using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.Web;

namespace Tabsan.EduSphere.WebIntegrationTests;

/// <summary>
/// Plan M1 Stage M1.5 — the SSO admin page and the linked-accounts card, rendered by the real
/// portal against a fake API (the portal signs in with a SuperAdmin or Student token).
/// </summary>
public class SsoAdminWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public SsoAdminWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task SsoSettingsPage_RendersProvidersAndIdpValues()
    {
        var api = new FakeApi("SuperAdmin");
        using var client = await SignedInClientAsync(api);

        var response = await client.GetAsync("/Portal/SsoSettings");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var provider in new[] { "Microsoft", "Google", "GenericOidc", "Saml" })
            Assert.Contains($"data-sso-card=\"{provider}\"", html);
        Assert.Contains("https://localhost/Login/SsoCallback", html);
        Assert.Contains("https://localhost/Login/SamlAcs", html);
        Assert.Contains("https://localhost/Login/SamlMetadata", html);
        // Configured Microsoft provider shows its state, but never a secret value.
        Assert.Contains("leave blank to keep", html);
        Assert.Contains("SSO only", html);
        Assert.DoesNotContain("the-secret", html);
    }

    [Fact]
    public async Task SaveForm_SendsSettingsToApi()
    {
        var api = new FakeApi("SuperAdmin");
        using var client = await SignedInClientAsync(api);
        var page = await client.GetStringAsync("/Portal/SsoSettings");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var post = await client.PostAsync("/Portal/SaveSsoSetting", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Provider"] = "Google",
            ["TenantCode"] = "",
            ["DisplayName"] = "Sign in with Google",
            ["ClientId"] = "gid",
            ["ClientSecret"] = "gsecret",
            ["Authority"] = "https://accounts.google.com",
            ["AllowedEmailDomains"] = "uni.edu, staff.uni.edu",
            ["Enabled"] = "true",
            ["AutoLinkByEmail"] = "true"
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("/api/v1/sso/settings/Google", api.LastSavePath);
        using var saved = JsonDocument.Parse(api.LastSaveBody!);
        JsonElement Prop(string name) => saved.RootElement.EnumerateObject()
            .First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;
        Assert.True(Prop("enabled").GetBoolean());
        Assert.True(Prop("autoLinkByEmail").GetBoolean());
        Assert.False(Prop("enforceSsoOnly").GetBoolean());
        Assert.Equal(2, Prop("allowedEmailDomains").GetArrayLength());
        Assert.Equal("gsecret", Prop("clientSecret").GetString());
    }

    [Fact]
    public async Task SsoSettingsPage_IsSuperAdminOnly()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var response = await client.GetAsync("/Portal/SsoSettings");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Null(api.LastSavePath);
    }

    [Fact]
    public async Task SecurityPage_ListsLinkedAccounts_AndUnlinkCallsApi()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync("/Portal/TwoFactorSettings");
        Assert.Contains("data-sso-links", html);
        Assert.Contains("ali@uni.edu", html);

        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var post = await client.PostAsync("/Portal/UnlinkSsoAccount", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["provider"] = "Google"
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("/api/v1/sso/links/Google", api.LastDeletePath);
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        var page = await client.GetStringAsync("/Login");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["username"] = "tester",
            ["password"] = "Password#123"
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri(FakeApi.Base + "/") };
    }

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        public string? LastSavePath, LastSaveBody, LastDeletePath;

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                ["unique_name"] = "tester",
                ["role"] = role,
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                    role, userId = Guid.NewGuid(), username = "tester", mustChangePassword = false,
                    mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));

            if (path == "/api/v1/sso/settings" && request.Method == HttpMethod.Get)
                return Json("""
                    [{"id":"5d0b1a9e-1111-4c1e-9a55-000000000001","tenantCode":null,"provider":"Microsoft","displayName":"Sign in with Microsoft",
                      "isEnabled":true,"isComplete":true,"clientId":"ms-client","hasClientSecret":true,
                      "authority":"https://login.microsoftonline.com/uni.edu/v2.0","metadataUrl":null,"allowedEmailDomains":["uni.edu"],
                      "autoLinkByEmail":true,"enforceSsoOnly":true,"trustIdpMfa":false,"updatedAt":"2026-10-18T08:00:00Z"}]
                    """);

            if (path.StartsWith("/api/v1/sso/settings/") && request.Method == HttpMethod.Put)
            {
                LastSavePath = path;
                LastSaveBody = body;
                return Json("{}");
            }

            if (path == "/api/v1/sso/links" && request.Method == HttpMethod.Get)
                return Json("""[{"provider":"Google","email":"ali@uni.edu","linkedAt":"2026-10-01T00:00:00Z","lastLoginAt":null}]""");

            if (path.StartsWith("/api/v1/sso/links/") && request.Method == HttpMethod.Delete)
            {
                LastDeletePath = path;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path == "/api/v1/sso/providers")
                return Json("[]");

            // Everything else the layout touches (menus, notifications, branding...): empty but valid.
            return path.Contains("menu", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
