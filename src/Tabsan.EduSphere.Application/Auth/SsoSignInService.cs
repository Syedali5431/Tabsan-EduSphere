using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Auth;

/// <summary>
/// Plan M1 Stage M1.2 — OpenID Connect sign-in (Microsoft Entra ID, Google, generic OIDC).
///
/// Flow: the web portal calls <see cref="StartAsync"/> and redirects the browser to the provider;
/// the provider redirects back to the portal, which relays code + state to <see cref="CompleteAsync"/>.
/// One-time state (nonce + PKCE verifier) and MFA tickets live in the distributed cache.
///
/// Account resolution: existing link → else (if AutoLinkByEmail) a verified email matching an
/// existing user of the same tenant → else refused. Users are never auto-created here.
/// </summary>
public sealed class SsoSignInService : ISsoSignInService
{
    internal const string CallbackPath = "/Login/SsoCallback";
    internal const string SamlAcsPath = "/Login/SamlAcs";
    internal const string SamlMetadataPath = "/Login/SamlMetadata";
    private const string StateKeyPrefix = "sso:state:";
    private const string MfaKeyPrefix = "sso:mfa:";
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MfaTicketTtl = TimeSpan.FromMinutes(5);
    private const int MaxMfaAttempts = 5;

    private readonly ISsoRepository _repo;
    private readonly IOidcClient _oidc;
    private readonly ISamlClient _saml;
    private readonly IEncryptionService _encryption;
    private readonly IUserRepository _users;
    private readonly IAuthService _auth;
    private readonly IAuditService _audit;
    private readonly IDistributedCache _cache;
    private readonly SsoSettings _settings;

    public SsoSignInService(
        ISsoRepository repo,
        IOidcClient oidc,
        ISamlClient saml,
        IEncryptionService encryption,
        IUserRepository users,
        IAuthService auth,
        IAuditService audit,
        IDistributedCache cache,
        IOptions<AuthSecurityOptions> security)
    {
        _repo = repo;
        _oidc = oidc;
        _saml = saml;
        _encryption = encryption;
        _users = users;
        _auth = auth;
        _audit = audit;
        _cache = cache;
        _settings = security.Value.Sso;
    }

    // ── Providers ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SsoProviderSummary>> GetProvidersAsync(string? tenantCode, CancellationToken ct = default)
    {
        var (tenantOk, tenantId) = await ResolveTenantAsync(tenantCode, ct);
        if (!tenantOk)
            return [];

        var settings = await _repo.GetEnabledSettingsAsync(tenantId, ct);
        return settings
            .Where(s => s.IsComplete)
            .OrderBy(s => s.Provider)
            .Select(s => new SsoProviderSummary(s.Provider.ToString(), s.DisplayName))
            .ToList();
    }

    // ── Start ────────────────────────────────────────────────────────────────

    public async Task<(SsoStartResponse? Response, SsoFailureReason? Failure)> StartAsync(SsoStartRequest request, CancellationToken ct = default)
    {
        var isSaml = string.Equals(request.Provider?.Trim(), nameof(SsoProvider.Saml), StringComparison.OrdinalIgnoreCase);
        if (!IsAllowedRedirectUri(request.RedirectUri, isSaml ? SamlAcsPath : CallbackPath))
            return (null, SsoFailureReason.InvalidRedirectUri);

        var setting = await LoadUsableSettingAsync(request.TenantCode, request.Provider ?? string.Empty, ct);
        if (setting is null)
            return (null, SsoFailureReason.ProviderUnavailable);

        return setting.IsOidc
            ? await StartOidcAsync(setting, request.RedirectUri, ct)
            : await StartSamlAsync(setting, request.RedirectUri, request.TenantCode, ct);
    }

    private async Task<(SsoStartResponse?, SsoFailureReason?)> StartOidcAsync(SsoProviderSetting setting, string redirectUri, CancellationToken ct)
    {
        var config = TryBuildConfig(setting);
        if (config is null)
            return (null, SsoFailureReason.ProviderUnavailable);

        var state = NewRandomToken();
        var nonce = NewRandomToken();
        var verifier = NewRandomToken();
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        string url;
        try
        {
            url = await _oidc.BuildAuthorizationUrlAsync(config, redirectUri, state, nonce, challenge, ct);
        }
        catch (SsoProtocolException)
        {
            return (null, SsoFailureReason.ProviderUnavailable);
        }

        await SaveStateAsync(state, new PendingState(setting.TenantId, setting.Provider, redirectUri, nonce, verifier), ct);
        return (new SsoStartResponse(url, state), null);
    }

    private async Task<(SsoStartResponse?, SsoFailureReason?)> StartSamlAsync(SsoProviderSetting setting, string acsUrl, string? tenantCode, CancellationToken ct)
    {
        var sp = BuildServiceProvider(acsUrl, tenantCode);
        var state = NewRandomToken(); // sent as RelayState

        SamlAuthnRequest authn;
        try
        {
            authn = await _saml.BuildAuthnRequestAsync(setting.MetadataUrl!, sp, state, ct);
        }
        catch (SsoProtocolException)
        {
            return (null, SsoFailureReason.ProviderUnavailable);
        }

        // Nonce carries the AuthnRequest ID that the assertion must answer (InResponseTo).
        await SaveStateAsync(state, new PendingState(setting.TenantId, setting.Provider, acsUrl, authn.RequestId, string.Empty, sp.EntityId), ct);
        return (new SsoStartResponse(authn.RedirectUrl, state), null);
    }

    // ── Complete ─────────────────────────────────────────────────────────────

    public async Task<SsoResult> CompleteAsync(SsoCompleteRequest request, string? ipAddress, CancellationToken ct = default)
    {
        // State is single-use: remove before doing anything else.
        var pending = await TakeAsync<PendingState>(StateKeyPrefix + request.State, ct);
        if (pending is null || pending.Provider == SsoProvider.Saml)
            return await FailAsync(SsoFailureReason.InvalidState, null, null, ipAddress, ct);

        var setting = await _repo.GetSettingAsync(pending.TenantId, pending.Provider, ct);
        var config = setting is { IsEnabled: true } ? TryBuildConfig(setting) : null;
        if (setting is null || config is null)
            return await FailAsync(SsoFailureReason.ProviderUnavailable, pending.Provider, null, ipAddress, ct);

        ExternalIdentity identity;
        try
        {
            identity = await _oidc.RedeemCodeAsync(config, request.Code, pending.RedirectUri, pending.CodeVerifier, pending.Nonce, ct);
        }
        catch (SsoProtocolException ex)
        {
            return await FailAsync(SsoFailureReason.TokenValidationFailed, pending.Provider, ex.Message, ipAddress, ct);
        }

        return await FinishSignInAsync(setting, identity, request.DeviceInfo, ipAddress, ct);
    }

    public async Task<SsoResult> CompleteSamlAsync(SsoSamlCompleteRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var pending = await TakeAsync<PendingState>(StateKeyPrefix + request.RelayState, ct);
        if (pending is null || pending.Provider != SsoProvider.Saml || string.IsNullOrEmpty(pending.SpEntityId))
            return await FailAsync(SsoFailureReason.InvalidState, SsoProvider.Saml, null, ipAddress, ct);

        var setting = await _repo.GetSettingAsync(pending.TenantId, SsoProvider.Saml, ct);
        if (setting is not { IsEnabled: true, IsComplete: true })
            return await FailAsync(SsoFailureReason.ProviderUnavailable, SsoProvider.Saml, null, ipAddress, ct);

        ExternalIdentity identity;
        try
        {
            identity = await _saml.ValidateResponseAsync(setting.MetadataUrl!,
                new SamlServiceProvider(pending.SpEntityId, pending.RedirectUri), request.SamlResponse, pending.Nonce, ct);
        }
        catch (SsoProtocolException ex)
        {
            return await FailAsync(SsoFailureReason.TokenValidationFailed, SsoProvider.Saml, ex.Message, ipAddress, ct);
        }

        return await FinishSignInAsync(setting, identity, request.DeviceInfo, ipAddress, ct);
    }

    public async Task<string?> GetSamlMetadataAsync(string acsUrl, string? tenantCode, CancellationToken ct = default)
    {
        if (!IsAllowedRedirectUri(acsUrl, SamlAcsPath))
            return null;

        var (tenantOk, _) = await ResolveTenantAsync(tenantCode, ct);
        return tenantOk ? _saml.BuildServiceProviderMetadata(BuildServiceProvider(acsUrl, tenantCode)) : null;
    }

    /// <summary>Shared tail for OIDC and SAML: domain policy, account linking, then the standard login gates.</summary>
    private async Task<SsoResult> FinishSignInAsync(
        SsoProviderSetting setting, ExternalIdentity identity, string? deviceInfo, string? ipAddress, CancellationToken ct)
    {
        if (!setting.IsEmailDomainAllowed(identity.Email))
            return await FailAsync(SsoFailureReason.EmailDomainNotAllowed, setting.Provider, identity.Email, ipAddress, ct);

        var link = await _repo.FindLinkAsync(setting.TenantId, setting.Provider, identity.Subject, ct);
        if (link is null)
        {
            link = await TryAutoLinkAsync(setting, identity, ipAddress, ct);
            if (link is null)
                return await FailAsync(SsoFailureReason.AccountNotLinked, setting.Provider, identity.Email, ipAddress, ct);
        }
        else
        {
            link.RecordLogin(identity.Email);
            await _repo.SaveChangesAsync(ct);
        }

        var mfaSatisfied = setting.TrustIdpMfa && identity.MfaAsserted;
        var login = await _auth.ExternalLoginAsync(link.UserId, setting.Provider.ToString(), mfaCode: null,
            mfaSatisfied, ipAddress, deviceInfo, ct);

        if (login.IsSuccess)
            return SsoResult.Ok(login.Response!);

        if (login.FailureReason == LoginFailureReason.MfaCodeRequired)
        {
            var ticket = NewRandomToken();
            await SaveMfaTicketAsync(ticket, new PendingMfa(link.UserId, setting.Provider, 0), ct);
            return SsoResult.Fail(SsoFailureReason.MfaCodeRequired, ticket);
        }

        return SsoResult.Rejected(login.FailureReason ?? LoginFailureReason.InvalidCredentials);
    }
    public async Task<SsoResult> CompleteMfaAsync(SsoMfaRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var pending = await TakeAsync<PendingMfa>(MfaKeyPrefix + request.Ticket, ct);
        if (pending is null)
            return SsoResult.Fail(SsoFailureReason.InvalidState);

        var login = await _auth.ExternalLoginAsync(pending.UserId, pending.Provider.ToString(), request.MfaCode,
            mfaSatisfiedExternally: false, ipAddress, request.DeviceInfo, ct);

        if (login.IsSuccess)
            return SsoResult.Ok(login.Response!);

        if (login.FailureReason is LoginFailureReason.InvalidMfaCode or LoginFailureReason.MfaCodeRequired)
        {
            var attempts = pending.Attempts + 1;
            if (attempts >= MaxMfaAttempts)
                return SsoResult.Fail(SsoFailureReason.InvalidState);

            // Re-issue under a fresh ticket so a leaked ticket cannot be replayed.
            var ticket = NewRandomToken();
            await SaveMfaTicketAsync(ticket, pending with { Attempts = attempts }, ct);
            return SsoResult.Fail(SsoFailureReason.InvalidMfaCode, ticket);
        }

        return SsoResult.Rejected(login.FailureReason ?? LoginFailureReason.InvalidCredentials);
    }

    // ── Linking ──────────────────────────────────────────────────────────────

    private async Task<ExternalLogin?> TryAutoLinkAsync(SsoProviderSetting setting, ExternalIdentity identity, string? ipAddress, CancellationToken ct)
    {
        if (!setting.AutoLinkByEmail || string.IsNullOrWhiteSpace(identity.Email))
            return null;

        // Microsoft: authority is pinned to the institution's own directory (see SsoProviderSetting),
        // so its email claim is institution-controlled. Others must assert email_verified.
        var emailTrusted = identity.EmailVerified || setting.Provider == SsoProvider.Microsoft;
        if (!emailTrusted)
            return null;

        var user = await _users.GetByEmailAsync(identity.Email.Trim(), ct);
        if (user is null || !user.IsActive)
            return null;

        if (setting.TenantId is not null && user.TenantId != setting.TenantId)
            return null;

        if (await _repo.UserHasLinkAsync(user.Id, setting.Provider, ct))
            return null; // already linked to a different external identity — never silently re-link

        var link = new ExternalLogin(setting.TenantId, user.Id, setting.Provider, identity.Subject, identity.Email);
        link.RecordLogin(identity.Email);
        await _repo.AddLinkAsync(link, ct);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog(
            "SsoAccountLinked", "User", user.Id.ToString(),
            actorUserId: user.Id,
            newValuesJson: JsonSerializer.Serialize(new { provider = setting.Provider.ToString(), method = "auto_email" }),
            ipAddress: ipAddress), ct);

        return link;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<SsoProviderSetting?> LoadUsableSettingAsync(string? tenantCode, string providerName, CancellationToken ct)
    {
        if (!Enum.TryParse<SsoProvider>(providerName, ignoreCase: true, out var provider) || !Enum.IsDefined(provider))
            return null;

        var (tenantOk, tenantId) = await ResolveTenantAsync(tenantCode, ct);
        if (!tenantOk)
            return null;

        var setting = await _repo.GetSettingAsync(tenantId, provider, ct);
        return setting is { IsEnabled: true, IsComplete: true } ? setting : null;
    }

    private async Task<(bool Ok, Guid? TenantId)> ResolveTenantAsync(string? tenantCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return (true, null);

        var id = await _repo.GetTenantIdByCodeAsync(tenantCode.Trim(), ct);
        return id is null ? (false, null) : (true, id);
    }

    private OidcProviderConfig? TryBuildConfig(SsoProviderSetting setting)
    {
        try
        {
            var secret = _encryption.Decrypt(setting.ClientSecretEncrypted!);
            return string.IsNullOrEmpty(secret)
                ? null
                : new OidcProviderConfig(setting.Authority!, setting.ClientId!, secret);
        }
        catch (Exception)
        {
            return null; // undecryptable secret (master key changed) — admin must re-enter it
        }
    }

    /// <summary>
    /// The portal endpoint the IdP returns to: <see cref="CallbackPath"/> for OIDC, <see cref="SamlAcsPath"/> for SAML.
    /// With AllowedRedirectUris configured the URL must be listed exactly; otherwise https (or loopback http) is required.
    /// </summary>
    internal bool IsAllowedRedirectUri(string? redirectUri, string expectedPath)
    {
        if (string.IsNullOrWhiteSpace(redirectUri)
            || !Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || !uri.AbsolutePath.TrimEnd('/').EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase))
            return false;

        if (_settings.AllowedRedirectUris.Length > 0)
            return _settings.AllowedRedirectUris.Any(a => string.Equals(a.TrimEnd('/'), redirectUri.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

        var schemeOk = uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
        return schemeOk && string.Equals(uri.AbsolutePath.TrimEnd('/'), expectedPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// SP entity ID = the portal's metadata URL (plus ?tenant=CODE on multi-tenant portals),
    /// derived from the validated ACS URL so a caller can never choose our audience.
    /// </summary>
    private static SamlServiceProvider BuildServiceProvider(string acsUrl, string? tenantCode)
    {
        var acs = acsUrl.TrimEnd('/');
        var portalBase = acs[..^SamlAcsPath.Length];
        var entityId = portalBase + SamlMetadataPath
                       + (string.IsNullOrWhiteSpace(tenantCode) ? string.Empty : "?tenant=" + Uri.EscapeDataString(tenantCode.Trim().ToUpperInvariant()));
        return new SamlServiceProvider(entityId, acs);
    }

    private Task SaveStateAsync(string state, PendingState pending, CancellationToken ct)
        => _cache.SetStringAsync(StateKeyPrefix + state, JsonSerializer.Serialize(pending),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = StateTtl }, ct);

    private async Task<SsoResult> FailAsync(SsoFailureReason reason, SsoProvider? provider, string? detail, string? ipAddress, CancellationToken ct)
    {
        await _audit.LogAsync(new AuditLog(
            "SsoLoginFailed", "User", entityId: null,
            newValuesJson: JsonSerializer.Serialize(new { reason = reason.ToString(), provider = provider?.ToString(), detail }),
            ipAddress: ipAddress), ct);
        return SsoResult.Fail(reason);
    }

    private Task SaveMfaTicketAsync(string ticket, PendingMfa pending, CancellationToken ct)
        => _cache.SetStringAsync(MfaKeyPrefix + ticket, JsonSerializer.Serialize(pending),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = MfaTicketTtl }, ct);

    private async Task<T?> TakeAsync<T>(string key, CancellationToken ct) where T : class
    {
        if (key.Length > 300)
            return null;

        var json = await _cache.GetStringAsync(key, ct);
        if (json is null)
            return null;

        await _cache.RemoveAsync(key, ct);
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NewRandomToken() => Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>OIDC: Nonce + PKCE verifier. SAML: Nonce = AuthnRequest ID, RedirectUri = ACS, SpEntityId set.</summary>
    private sealed record PendingState(Guid? TenantId, SsoProvider Provider, string RedirectUri, string Nonce, string CodeVerifier, string? SpEntityId = null);

    private sealed record PendingMfa(Guid UserId, SsoProvider Provider, int Attempts);
}
