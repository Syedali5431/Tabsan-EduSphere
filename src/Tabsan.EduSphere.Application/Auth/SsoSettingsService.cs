using System.Text.Json;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Auth;

/// <summary>
/// Plan M1 Stage M1.5 — SuperAdmin management of SSO providers.
/// Client secrets are encrypted on the way in and never returned; every change is audited
/// (without secret values). Domain rules (https URLs, single-directory Microsoft authority,
/// "cannot enable while incomplete") surface to the admin as plain error messages.
/// </summary>
public sealed class SsoSettingsService : ISsoSettingsService
{
    // Placeholders used only to exercise discovery / metadata during "Test connection".
    private const string TestCallback = "https://sso-test.invalid/Login/SsoCallback";
    private static readonly SamlServiceProvider TestSp = new("https://sso-test.invalid/Login/SamlMetadata", "https://sso-test.invalid/Login/SamlAcs");

    private readonly ISsoRepository _repo;
    private readonly IEncryptionService _encryption;
    private readonly IOidcClient _oidc;
    private readonly ISamlClient _saml;
    private readonly IAuditService _audit;

    public SsoSettingsService(ISsoRepository repo, IEncryptionService encryption, IOidcClient oidc, ISamlClient saml, IAuditService audit)
    {
        _repo = repo;
        _encryption = encryption;
        _oidc = oidc;
        _saml = saml;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SsoProviderSettingDto>> ListAsync(CancellationToken ct = default)
        => (await _repo.GetAllSettingsAsync(ct)).Select(r => ToDto(r.Setting, r.TenantCode)).ToList();

    public async Task<(SsoProviderSettingDto? Saved, string? Error)> SaveAsync(
        string provider, string? tenantCode, SaveSsoProviderSettingRequest request, Guid actorUserId, string? ipAddress, CancellationToken ct = default)
    {
        if (!TryParseProvider(provider, out var parsed))
            return (null, "Unknown SSO provider.");

        var (tenantOk, tenantId, code) = await ResolveTenantAsync(tenantCode, ct);
        if (!tenantOk)
            return (null, $"Tenant '{tenantCode}' was not found or is inactive.");

        var setting = await _repo.GetSettingAsync(tenantId, parsed, ct);
        var isNew = setting is null;
        var secretChanged = false;

        try
        {
            setting ??= new SsoProviderSetting(tenantId, parsed, request.DisplayName);
            setting.Rename(request.DisplayName);

            if (setting.IsOidc)
            {
                setting.ConfigureOidc(request.ClientId ?? string.Empty, request.Authority);

                if (!string.IsNullOrWhiteSpace(request.ClientSecret))
                {
                    setting.SetEncryptedClientSecret(_encryption.Encrypt(request.ClientSecret.Trim()));
                    secretChanged = true;
                }
            }
            else
            {
                setting.ConfigureSaml(request.MetadataUrl ?? string.Empty);
            }

            setting.SetPolicy(request.AutoLinkByEmail, request.EnforceSsoOnly, request.TrustIdpMfa, request.AllowedEmailDomains);
            setting.SetAttributeMapping(request.IdClaim, request.LinkById, request.RoleClaim, request.RoleMappings, request.RequireRoleMatch);
            setting.SetSamlSecurity(!setting.IsOidc && request.SignAuthnRequests, !setting.IsOidc && request.RequireEncryptedAssertions);

            if (request.Enabled)
                setting.Enable();
            else
                setting.Disable();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return (null, FriendlyError(ex));
        }

        if (isNew)
            await _repo.AddSettingAsync(setting, ct);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog(
            isNew ? "SsoSettingsCreated" : "SsoSettingsUpdated", "SsoProviderSetting", setting.Id.ToString(),
            actorUserId: actorUserId,
            newValuesJson: JsonSerializer.Serialize(new
            {
                tenant = code,
                provider = parsed.ToString(),
                setting.DisplayName,
                setting.IsEnabled,
                setting.ClientId,
                setting.Authority,
                setting.MetadataUrl,
                setting.AllowedEmailDomains,
                setting.AutoLinkByEmail,
                setting.EnforceSsoOnly,
                setting.TrustIdpMfa,
                setting.IdClaim,
                setting.LinkById,
                setting.RoleClaim,
                setting.RoleMappings,
                setting.RequireRoleMatch,
                setting.SignAuthnRequests,
                setting.RequireEncryptedAssertions,
                secretChanged
            }),
            ipAddress: ipAddress), ct);

        return (ToDto(setting, code), null);
    }

    public async Task<bool> DeleteAsync(string provider, string? tenantCode, Guid actorUserId, string? ipAddress, CancellationToken ct = default)
    {
        if (!TryParseProvider(provider, out var parsed))
            return false;

        var (tenantOk, tenantId, code) = await ResolveTenantAsync(tenantCode, ct);
        if (!tenantOk)
            return false;

        var setting = await _repo.GetSettingAsync(tenantId, parsed, ct);
        if (setting is null)
            return false;

        setting.Disable();
        setting.SoftDelete();
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog(
            "SsoSettingsDeleted", "SsoProviderSetting", setting.Id.ToString(),
            actorUserId: actorUserId,
            oldValuesJson: JsonSerializer.Serialize(new { tenant = code, provider = parsed.ToString(), setting.DisplayName }),
            ipAddress: ipAddress), ct);
        return true;
    }

    public async Task<SsoConnectionTestResult> TestAsync(string provider, string? tenantCode, CancellationToken ct = default)
    {
        if (!TryParseProvider(provider, out var parsed))
            return new(false, "Unknown SSO provider.");

        var (tenantOk, tenantId, _) = await ResolveTenantAsync(tenantCode, ct);
        var setting = tenantOk ? await _repo.GetSettingAsync(tenantId, parsed, ct) : null;
        if (setting is null)
            return new(false, "Save the provider settings before testing the connection.");
        if (!setting.IsComplete)
            return new(false, setting.IsOidc
                ? "Client ID, client secret and authority are required."
                : "The IdP metadata URL is required.");

        try
        {
            if (setting.IsOidc)
            {
                string secret;
                try { secret = _encryption.Decrypt(setting.ClientSecretEncrypted!); }
                catch (Exception) { return new(false, "The stored client secret cannot be decrypted (the encryption key changed). Enter the secret again."); }

                await _oidc.BuildAuthorizationUrlAsync(new OidcProviderConfig(setting.Authority!, setting.ClientId!, secret),
                    TestCallback, "test", "test", "test", ct);
                return new(true, $"Connected: the OpenID configuration at {setting.Authority} was loaded. Complete a real sign-in to confirm the client ID and secret.");
            }

            await _saml.BuildAuthnRequestAsync(setting.MetadataUrl!, TestSp, "test", setting.SignAuthnRequests, ct);
            return new(true, "Connected: the IdP metadata was loaded and contains a signing certificate and a single sign-on endpoint.");
        }
        catch (SsoProtocolException ex)
        {
            return new(false, ex.Message);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static bool TryParseProvider(string? provider, out SsoProvider parsed)
        => Enum.TryParse(provider?.Trim(), ignoreCase: true, out parsed) && Enum.IsDefined(parsed);

    private async Task<(bool Ok, Guid? TenantId, string? Code)> ResolveTenantAsync(string? tenantCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return (true, null, null);

        var code = tenantCode.Trim().ToUpperInvariant();
        var id = await _repo.GetTenantIdByCodeAsync(code, ct);
        return id is null ? (false, null, null) : (true, id, code);
    }

    private static string FriendlyError(Exception ex)
    {
        // Domain messages carry " (Parameter 'x')" suffixes from ArgumentException.
        var message = ex.Message;
        var idx = message.IndexOf(" (Parameter", StringComparison.Ordinal);
        var field = ex is ArgumentException { ParamName: { } p } ? p switch
        {
            "displayName" => "Display name",
            "clientId" => "Client ID",
            "authority" => "Authority",
            "metadataUrl" => "Metadata URL",
            "idClaim" => "Institution ID claim",
            "roleClaim" => "Role claim",
            "roleMappings" => "Role mapping",
            _ => p
        } : null;
        var text = idx > 0 ? message[..idx] : message;
        return field is null ? text : $"{field}: {text}";
    }

    private static SsoProviderSettingDto ToDto(SsoProviderSetting s, string? tenantCode) => new(
        s.Id, tenantCode, s.Provider.ToString(), s.DisplayName, s.IsEnabled, s.IsComplete,
        s.ClientId, !string.IsNullOrEmpty(s.ClientSecretEncrypted), s.Authority, s.MetadataUrl,
        s.GetAllowedEmailDomains(), s.AutoLinkByEmail, s.EnforceSsoOnly, s.TrustIdpMfa, s.UpdatedAt,
        s.IdClaim, s.LinkById, s.RoleClaim, s.RoleMappings, s.RequireRoleMatch, s.SignAuthnRequests, s.RequireEncryptedAssertions);
}
