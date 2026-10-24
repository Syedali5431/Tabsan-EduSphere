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

    // ── Attribute mapping ────────────────────────────────────────────────────

    /// <summary>EduSphere roles an identity-provider role value can map to (SuperAdmin keeps password + 2FA sign-in).</summary>
    public static readonly string[] MappableRoles = ["Student", "Faculty", "Admin", "Finance", "Parent"];

    /// <summary>
    /// Claim (OIDC) or attribute (SAML) carrying the person's institution ID: a student's registration number,
    /// otherwise the EduSphere username (e.g. a staff employee number).
    /// </summary>
    public string? IdClaim { get; private set; }

    /// <summary>When true a first-time sign-in is linked to the one active user of the tenant with that institution ID.</summary>
    public bool LinkById { get; private set; }

    /// <summary>Claim or attribute carrying role / affiliation values (e.g. eduPersonAffiliation, Entra app roles).</summary>
    public string? RoleClaim { get; private set; }

    /// <summary>Normalised "value=Role|Role; value=Role" pairs translating role values into EduSphere roles.</summary>
    public string? RoleMappings { get; private set; }

    /// <summary>When true a sign-in is refused unless an asserted role value maps to the account's EduSphere role.</summary>
    public bool RequireRoleMatch { get; private set; }

    // ── SAML security ────────────────────────────────────────────────────────

    /// <summary>SAML: sign AuthnRequests with the service-provider certificate (for IdPs that require it).</summary>
    public bool SignAuthnRequests { get; private set; }

    /// <summary>SAML: refuse responses whose assertion is not encrypted to the service-provider certificate.</summary>
    public bool RequireEncryptedAssertions { get; private set; }

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

    /// <summary>
    /// Sets the institution-ID and role attribute mapping. <paramref name="roleMappings"/> holds "value=Role" pairs
    /// separated by ";", "," or new lines; several roles for one value are separated by "|".
    /// </summary>
    public void SetAttributeMapping(string? idClaim, bool linkById, string? roleClaim, string? roleMappings, bool requireRoleMatch)
    {
        var id = OptionalText(idClaim, 256, nameof(idClaim));
        var role = OptionalText(roleClaim, 256, nameof(roleClaim));
        if (linkById && id is null)
            throw new ArgumentException("Enter the claim or attribute that carries the institution ID.", nameof(idClaim));

        var mappings = ParseRoleMappings(roleMappings);
        if (requireRoleMatch && (role is null || mappings.Count == 0))
            throw new ArgumentException("Enter the role claim or attribute and at least one role mapping.", nameof(roleMappings));

        var text = string.Join("; ", mappings.Select(m => m.Key + "=" + string.Join('|', m.Value)));
        if (text.Length > 1000)
            throw new ArgumentException("The role mapping is too long (1,000 characters at most).", nameof(roleMappings));

        IdClaim = id;
        LinkById = linkById;
        RoleClaim = role;
        RoleMappings = text.Length == 0 ? null : text;
        RequireRoleMatch = requireRoleMatch;
        Touch();
    }

    /// <summary>SAML only: signed AuthnRequests and required assertion encryption.</summary>
    public void SetSamlSecurity(bool signAuthnRequests, bool requireEncryptedAssertions)
    {
        if (Provider != SsoProvider.Saml && (signAuthnRequests || requireEncryptedAssertions))
            throw new InvalidOperationException($"{Provider} is not a SAML provider.");

        SignAuthnRequests = signAuthnRequests;
        RequireEncryptedAssertions = requireEncryptedAssertions;
        Touch();
    }

    /// <summary>The role mapping as value → EduSphere roles (values compared case-insensitively).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetRoleMappings() => ParseRoleMappings(RoleMappings);

    /// <summary>
    /// EduSphere roles for the asserted role values. Scoped values ("student@uni.edu", eduPersonScopedAffiliation)
    /// also match on the part before "@".
    /// </summary>
    public IReadOnlyList<string> MapRoles(IEnumerable<string> assertedValues)
    {
        var mappings = GetRoleMappings();
        var roles = new List<string>();
        foreach (var raw in assertedValues)
        {
            var value = raw?.Trim() ?? string.Empty;
            if (value.Length == 0) continue;
            var at = value.IndexOf('@');
            if (mappings.TryGetValue(value, out var mapped) || (at > 0 && mappings.TryGetValue(value[..at], out mapped)))
                roles.AddRange(mapped);
        }
        return roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static Dictionary<string, IReadOnlyList<string>> ParseRoleMappings(string? text)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in (text ?? string.Empty).Split([';', ',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = pair.IndexOf('=');
            var value = eq > 0 ? pair[..eq].Trim() : string.Empty;
            if (value.Length == 0 || eq == pair.Length - 1)
                throw new ArgumentException($"\"{pair}\" is not a role mapping. Use value=Role, e.g. student=Student.", "roleMappings");

            var roles = new List<string>();
            foreach (var name in pair[(eq + 1)..].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var role = MappableRoles.FirstOrDefault(r => r.Equals(name, StringComparison.OrdinalIgnoreCase))
                           ?? throw new ArgumentException($"\"{name}\" is not a role that can be mapped. Use {string.Join(", ", MappableRoles)}.", "roleMappings");
                if (!roles.Contains(role)) roles.Add(role);
            }
            result[value] = result.TryGetValue(value, out var existing) ? existing.Union(roles).ToList() : roles;
        }
        return result;
    }

    private static string? OptionalText(string? value, int maxLength, string paramName)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return text.Length <= maxLength ? text : throw new ArgumentException($"At most {maxLength} characters.", paramName);
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
