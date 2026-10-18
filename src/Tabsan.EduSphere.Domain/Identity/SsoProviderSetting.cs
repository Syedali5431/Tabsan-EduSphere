using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Domain.Identity;

/// <summary>
/// Plan M1 (SSO): per-tenant configuration of one external identity provider.
/// At most one active row exists per (TenantId, Provider). The client secret is stored
/// encrypted by the Infrastructure layer — this entity only ever holds ciphertext.
/// Rows start disabled and can only be enabled once the provider details are complete.
/// </summary>
public class SsoProviderSetting : AuditableEntity
{
    /// <summary>Well-known Google OpenID Connect issuer.</summary>
    public const string GoogleAuthority = "https://accounts.google.com";

    /// <summary>Tenant that owns this configuration. Null on single-tenant installs.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Provider type this row configures.</summary>
    public SsoProvider Provider { get; private set; }

    /// <summary>Label shown on the login button (e.g. "Sign in with University Account").</summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>When false the provider is hidden from the login page and its endpoints refuse sign-in.</summary>
    public bool IsEnabled { get; private set; }

    // ── OpenID Connect ───────────────────────────────────────────────────────

    /// <summary>OIDC client / application id issued by the provider.</summary>
    public string? ClientId { get; private set; }

    /// <summary>OIDC client secret, encrypted at rest (ciphertext only).</summary>
    public string? ClientSecretEncrypted { get; private set; }

    /// <summary>OIDC issuer / authority URL (e.g. https://login.microsoftonline.com/{tenant}/v2.0).</summary>
    public string? Authority { get; private set; }

    // ── SAML 2.0 ─────────────────────────────────────────────────────────────

    /// <summary>URL of the SAML identity provider's metadata document.</summary>
    public string? MetadataUrl { get; private set; }

    // ── Policy ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Comma-separated list of email domains accepted from this provider (e.g. "uni.edu,staff.uni.edu").
    /// Null or empty means any domain is accepted.
    /// </summary>
    public string? AllowedEmailDomains { get; private set; }

    /// <summary>
    /// When true a first-time external sign-in is linked to an existing user whose email matches
    /// the provider-asserted (verified) email. When false links must be created explicitly.
    /// </summary>
    public bool AutoLinkByEmail { get; private set; }

    /// <summary>
    /// When true password login is refused for this tenant's users (SuperAdmin break-glass excepted).
    /// </summary>
    public bool EnforceSsoOnly { get; private set; }

    /// <summary>
    /// When true and the provider asserts multi-factor authentication (e.g. OIDC <c>amr</c> = "mfa"),
    /// EduSphere skips its own TOTP challenge for that sign-in.
    /// </summary>
    public bool TrustIdpMfa { get; private set; }

    private SsoProviderSetting() { }

    public SsoProviderSetting(Guid? tenantId, SsoProvider provider, string displayName)
    {
        TenantId = tenantId;
        Provider = provider;
        DisplayName = RequireText(displayName, nameof(displayName));
        if (provider == SsoProvider.Google)
            Authority = GoogleAuthority;
    }

    /// <summary>True for providers configured through OpenID Connect.</summary>
    public bool IsOidc => Provider is SsoProvider.Microsoft or SsoProvider.Google or SsoProvider.GenericOidc;

    /// <summary>True when every field required by the provider type is present.</summary>
    public bool IsComplete => IsOidc
        ? !string.IsNullOrWhiteSpace(ClientId)
          && !string.IsNullOrWhiteSpace(ClientSecretEncrypted)
          && !string.IsNullOrWhiteSpace(Authority)
        : !string.IsNullOrWhiteSpace(MetadataUrl);

    public void Rename(string displayName)
    {
        DisplayName = RequireText(displayName, nameof(displayName));
        Touch();
    }

    /// <summary>Sets the OIDC client id and authority. Google always uses its fixed issuer.</summary>
    public void ConfigureOidc(string clientId, string? authority)
    {
        if (!IsOidc)
            throw new InvalidOperationException($"{Provider} is not an OpenID Connect provider.");

        ClientId = RequireText(clientId, nameof(clientId));
        Authority = Provider switch
        {
            SsoProvider.Google    => GoogleAuthority,
            SsoProvider.Microsoft => RequireSingleTenantMicrosoftAuthority(authority, nameof(authority)),
            _                     => RequireAbsoluteHttpsUrl(authority, nameof(authority))
        };
        Touch();
    }

    /// <summary>
    /// Microsoft multi-tenant endpoints (common / organizations / consumers) would accept accounts
    /// from any directory, whose email claims the institution does not control. Only the
    /// institution's own directory (tenant id or verified domain) is allowed.
    /// </summary>
    private static string RequireSingleTenantMicrosoftAuthority(string? value, string paramName)
    {
        var text = RequireAbsoluteHttpsUrl(value, paramName);
        var segments = new Uri(text).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var directory = segments.FirstOrDefault() ?? string.Empty;
        if (directory.Length == 0
            || directory.Equals("common", StringComparison.OrdinalIgnoreCase)
            || directory.Equals("organizations", StringComparison.OrdinalIgnoreCase)
            || directory.Equals("consumers", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Use your institution's own Entra ID directory, e.g. https://login.microsoftonline.com/{tenant-id}/v2.0.",
                paramName);
        }
        return text;
    }

    /// <summary>Stores an already-encrypted client secret. Pass null to clear it (disables the provider).</summary>
    public void SetEncryptedClientSecret(string? ciphertext)
    {
        if (!IsOidc)
            throw new InvalidOperationException($"{Provider} does not use a client secret.");

        ClientSecretEncrypted = string.IsNullOrWhiteSpace(ciphertext) ? null : ciphertext;
        if (ClientSecretEncrypted is null)
            IsEnabled = false;
        Touch();
    }

    /// <summary>Sets the SAML identity provider metadata URL.</summary>
    public void ConfigureSaml(string metadataUrl)
    {
        if (Provider != SsoProvider.Saml)
            throw new InvalidOperationException($"{Provider} is not a SAML provider.");

        MetadataUrl = RequireAbsoluteHttpsUrl(metadataUrl, nameof(metadataUrl));
        Touch();
    }

    /// <summary>Updates the sign-in policy flags and the allowed email domain list.</summary>
    public void SetPolicy(bool autoLinkByEmail, bool enforceSsoOnly, bool trustIdpMfa, IEnumerable<string>? allowedEmailDomains)
    {
        AutoLinkByEmail = autoLinkByEmail;
        EnforceSsoOnly = enforceSsoOnly;
        TrustIdpMfa = trustIdpMfa;

        var domains = (allowedEmailDomains ?? [])
            .Select(d => d.Trim().TrimStart('@').ToLowerInvariant())
            .Where(d => d.Length > 0)
            .Distinct()
            .ToList();
        AllowedEmailDomains = domains.Count == 0 ? null : string.Join(',', domains);
        Touch();
    }

    /// <summary>Returns the allowed email domains as a list (empty = any domain).</summary>
    public IReadOnlyList<string> GetAllowedEmailDomains()
        => string.IsNullOrWhiteSpace(AllowedEmailDomains)
            ? []
            : AllowedEmailDomains.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>True when <paramref name="email"/> belongs to an allowed domain (or no restriction is set).</summary>
    public bool IsEmailDomainAllowed(string? email)
    {
        var domains = GetAllowedEmailDomains();
        if (domains.Count == 0)
            return true;
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1)
            return false;

        var domain = email[(at + 1)..].Trim().ToLowerInvariant();
        return domains.Contains(domain, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Enables the provider. Throws when required provider details are missing.</summary>
    public void Enable()
    {
        if (!IsComplete)
            throw new InvalidOperationException($"{Provider} SSO configuration is incomplete and cannot be enabled.");

        IsEnabled = true;
        Touch();
    }

    public void Disable()
    {
        IsEnabled = false;
        Touch();
    }

    private static string RequireText(string? value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", paramName)
            : value.Trim();

    private static string RequireAbsoluteHttpsUrl(string? value, string paramName)
    {
        var text = RequireText(value, paramName);
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("An absolute https:// URL is required.", paramName);
        return text;
    }
}
