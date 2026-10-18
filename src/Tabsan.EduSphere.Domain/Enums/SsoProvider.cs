namespace Tabsan.EduSphere.Domain.Enums;

/// <summary>Plan M1: external identity provider types supported for Single Sign-On.</summary>
public enum SsoProvider
{
    /// <summary>Microsoft Entra ID (Azure AD) via OpenID Connect.</summary>
    Microsoft   = 1,

    /// <summary>Google Workspace / Google accounts via OpenID Connect.</summary>
    Google      = 2,

    /// <summary>Any standards-compliant OpenID Connect provider (Okta, Keycloak, Auth0, ...).</summary>
    GenericOidc = 3,

    /// <summary>SAML 2.0 identity provider (Shibboleth, ADFS, ...).</summary>
    Saml        = 4
}
