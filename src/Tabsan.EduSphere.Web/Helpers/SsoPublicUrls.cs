namespace Tabsan.EduSphere.Web.Helpers;

/// <summary>
/// Plan M1: the portal URLs an identity provider must be configured with.
/// Behind a tunnel / reverse proxy without forwarded headers set <c>Sso:PublicBaseUrl</c>
/// (e.g. https://portal.uni.edu) so these match what is registered at the IdP.
/// </summary>
public static class SsoPublicUrls
{
    public const string OidcCallbackPath = "/Login/SsoCallback";
    public const string SamlAcsPath = "/Login/SamlAcs";
    public const string SamlMetadataPath = "/Login/SamlMetadata";

    public static string BaseUrl(HttpRequest request, IConfiguration config)
    {
        var configured = config["Sso:PublicBaseUrl"];
        return !string.IsNullOrWhiteSpace(configured)
            ? configured.TrimEnd('/')
            : $"{request.Scheme}://{request.Host}{request.PathBase}";
    }

    public static string Build(HttpRequest request, IConfiguration config, string path) => BaseUrl(request, config) + path;

    /// <summary>SAML SP entity ID — must match what the API derives from the ACS URL.</summary>
    public static string SamlEntityId(HttpRequest request, IConfiguration config, string? tenantCode)
        => Build(request, config, SamlMetadataPath)
           + (string.IsNullOrWhiteSpace(tenantCode) ? string.Empty : "?tenant=" + Uri.EscapeDataString(tenantCode.Trim().ToUpperInvariant()));
}
