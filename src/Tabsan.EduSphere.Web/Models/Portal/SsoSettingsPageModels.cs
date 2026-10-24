using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M1 Stage M1.5 — SSO administration page.

public sealed class SsoSettingsPageModel
{
    /// <summary>Providers offered on the page, in display order.</summary>
    public static readonly (string Key, string Title, string Kind, string DefaultLabel)[] ProviderCatalog =
    [
        ("Microsoft",   "Microsoft Entra ID",       "oidc", "Sign in with Microsoft"),
        ("Google",      "Google Workspace",         "oidc", "Sign in with Google"),
        ("GenericOidc", "Other OpenID Connect IdP", "oidc", "Sign in with your institution"),
        ("Saml",        "SAML 2.0 IdP",             "saml", "Sign in with your institution")
    ];

    public bool IsConnected { get; set; }
    public bool ModuleUnavailable { get; set; }
    public string? Message { get; set; }

    /// <summary>Tenant being configured; null = installation-wide.</summary>
    public string? TenantCode { get; set; }

    public List<SsoProviderSettingApiModel> Settings { get; set; } = new();

    // Values to register at the identity provider.
    public string OidcRedirectUri { get; set; } = string.Empty;
    public string SamlAcsUrl { get; set; } = string.Empty;
    public string SamlEntityId { get; set; } = string.Empty;
    public string SamlMetadataUrl { get; set; } = string.Empty;

    public SsoProviderSettingApiModel? For(string provider)
        => Settings.FirstOrDefault(s =>
            string.Equals(s.Provider, provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.TenantCode ?? string.Empty, TenantCode ?? string.Empty, StringComparison.OrdinalIgnoreCase));

    /// <summary>Tenant codes that already have SSO configured (for quick switching).</summary>
    public IEnumerable<string?> ConfiguredTenants
        => Settings.Select(s => s.TenantCode).Distinct(StringComparer.OrdinalIgnoreCase);
}

public sealed class SsoSettingFormModel
{
    public string Provider { get; set; } = string.Empty;
    public string? TenantCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Authority { get; set; }
    public string? MetadataUrl { get; set; }
    /// <summary>Comma, space or newline separated.</summary>
    public string? AllowedEmailDomains { get; set; }
    public bool AutoLinkByEmail { get; set; }
    public bool EnforceSsoOnly { get; set; }
    public bool TrustIdpMfa { get; set; }
    public bool Enabled { get; set; }
    public string? IdClaim { get; set; }
    public bool LinkById { get; set; }
    public string? RoleClaim { get; set; }
    public string? RoleMappings { get; set; }
    public bool RequireRoleMatch { get; set; }
    public bool SignAuthnRequests { get; set; }
    public bool RequireEncryptedAssertions { get; set; }
}
