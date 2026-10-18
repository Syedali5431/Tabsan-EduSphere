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
/// Plan M1 Stage M1.2 — portal side of OIDC sign-in: provider buttons, the browser-bound
/// state cookie, the IdP callback and the authenticator-code step, against a fake API.
/// </summary>
public class SsoLoginWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string ApiBase = "https://api.test";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public SsoLoginWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task LoginPage_ShowsEnabledProviderButtons()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);

        var html = await client.GetStringAsync("/Login");

        Assert.Contains("data-sso-provider=\"Google\"", html);
        Assert.Contains("Sign in with Google", html);
        Assert.Contains("/Login/Sso/google", html);
    }

    [Fact]
    public async Task LoginPage_WithoutSsoModule_ShowsNoButtons()
    {
        var api = new FakeApi { ProvidersStatus = HttpStatusCode.Forbidden };
        using var client = CreateClient(api);

        var html = await client.GetStringAsync("/Login");

        Assert.DoesNotContain("data-sso-provider", html);
        Assert.Contains("name=\"password\"", html);
    }

    [Fact]
    public async Task FullFlow_RedirectsToIdp_ThenCallbackSignsIn()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);

        var start = await client.GetAsync("/Login/Sso/google?returnUrl=%2FPortal%2FCourses");
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        Assert.StartsWith("https://idp.test/authorize", start.Headers.Location!.ToString());
        Assert.Equal("https://localhost/Login/SsoCallback", api.LastStartRedirectUri);
        Assert.Contains(start.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".Tabsan.EduSphere.Sso=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/Portal/Courses", callback.Headers.Location!.ToString());
        Assert.Equal("abc", api.LastCompleteCode);
    }

    [Fact]
    public async Task Callback_WithoutBrowserCookie_IsRejected()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);

        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");
        var html = await callback.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        Assert.Contains("expired or is invalid", html);
        Assert.Null(api.LastCompleteCode); // never relayed to the API
    }

    [Fact]
    public async Task Callback_WithMismatchedState_IsRejected()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);
        await client.GetAsync("/Login/Sso/google");

        var callback = await client.GetAsync("/Login/SsoCallback?code=abc&state=attacker-state");

        Assert.Contains("expired or is invalid", await callback.Content.ReadAsStringAsync());
        Assert.Null(api.LastCompleteCode);
    }

    [Fact]
    public async Task Callback_AccountNotLinked_ShowsFriendlyError()
    {
        var api = new FakeApi { CompleteStatus = HttpStatusCode.Forbidden, CompleteBody = "{\"error\":\"AccountNotLinked\"}" };
        using var client = CreateClient(api);
        await client.GetAsync("/Login/Sso/google");

        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");

        Assert.Contains("not linked to an EduSphere user", await callback.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Callback_WhenTotpRequired_ShowsCodeStep_ThenSignsIn()
    {
        var api = new FakeApi { CompleteStatus = HttpStatusCode.Unauthorized, CompleteBody = "{\"error\":\"MfaCodeRequired\",\"ticket\":\"tkt-1\"}" };
        using var client = CreateClient(api);
        await client.GetAsync("/Login/Sso/google");

        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");
        var html = await callback.Content.ReadAsStringAsync();
        Assert.Contains("name=\"ticket\" value=\"tkt-1\"", html);
        Assert.DoesNotContain("name=\"password\"", html);

        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var post = await client.PostAsync("/Login/SsoMfa", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ticket"] = "tkt-1",
            ["mfaCode"] = "123456"
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("123456", api.LastMfaCode);
    }

    // ── SAML 2.0 (Stage M1.3) ────────────────────────────────────────────────

    [Fact]
    public async Task SamlFlow_UsesAcsAndCrossSiteCookie_ThenAcsPostSignsIn()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);

        var start = await client.GetAsync("/Login/Sso/saml?returnUrl=%2FPortal%2FCourses");
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        Assert.Equal("https://localhost/Login/SamlAcs", api.LastStartRedirectUri);
        var setCookie = start.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(".Tabsan.EduSphere.Sso="));
        Assert.Contains("samesite=none", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);

        // The IdP posts back cross-site: no antiforgery token, just SAMLResponse + RelayState.
        var acs = await client.PostAsync("/Login/SamlAcs", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["SAMLResponse"] = "PHNhbWxwOlJlc3BvbnNlLz4=",
            ["RelayState"] = FakeApi.State
        }));

        Assert.Equal(HttpStatusCode.Redirect, acs.StatusCode);
        Assert.Equal("/Portal/Courses", acs.Headers.Location!.ToString());
        Assert.Equal("PHNhbWxwOlJlc3BvbnNlLz4=", api.LastSamlResponse);
    }

    [Fact]
    public async Task SamlAcs_WithForeignRelayState_IsRejected()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);
        await client.GetAsync("/Login/Sso/saml");

        var acs = await client.PostAsync("/Login/SamlAcs", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["SAMLResponse"] = "PHNhbWxwOlJlc3BvbnNlLz4=",
            ["RelayState"] = "attacker-state"
        }));

        Assert.Contains("expired or is invalid", await acs.Content.ReadAsStringAsync());
        Assert.Null(api.LastSamlResponse);
    }

    [Fact]
    public async Task SamlMetadata_ProxiesSpDescriptorForThisPortal()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);

        var response = await client.GetAsync("/Login/SamlMetadata");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/samlmetadata+xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://localhost/Login/SamlAcs", api.LastMetadataAcs);
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private HttpClient CreateClient(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", ApiBase);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeApi : HttpMessageHandler
    {
        public const string State = "state-123";
        public HttpStatusCode ProvidersStatus { get; init; } = HttpStatusCode.OK;
        public HttpStatusCode CompleteStatus { get; init; } = HttpStatusCode.OK;
        public string? CompleteBody { get; init; }
        public string? LastStartRedirectUri, LastCompleteCode, LastMfaCode, LastSamlResponse, LastMetadataAcs;

        private static readonly string LoginJson = JsonSerializer.Serialize(new
        {
            accessToken = "header.payload.sig",
            refreshToken = "refresh",
            accessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
            role = "Student",
            userId = Guid.NewGuid(),
            username = "ali",
            mustChangePassword = false,
            mfaEnabled = false,
            ssoEnabled = false,
            ssoProvider = (string?)null,
            sessionRiskLevel = "low"
        });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);

            switch (path)
            {
                case "/api/v1/sso/providers":
                    return ProvidersStatus == HttpStatusCode.OK
                        ? Json("[{\"provider\":\"Google\",\"displayName\":\"Sign in with Google\"}]")
                        : new HttpResponseMessage(ProvidersStatus);

                case "/api/v1/sso/start":
                    LastStartRedirectUri = JsonDocument.Parse(body!).RootElement.GetProperty("redirectUri").GetString();
                    return Json($"{{\"authorizationUrl\":\"https://idp.test/authorize?state={State}\",\"state\":\"{State}\"}}");

                case "/api/v1/sso/complete":
                    LastCompleteCode = JsonDocument.Parse(body!).RootElement.GetProperty("code").GetString();
                    return CompleteStatus == HttpStatusCode.OK
                        ? Json(LoginJson)
                        : Json(CompleteBody ?? "{}", CompleteStatus);

                case "/api/v1/sso/saml/complete":
                    LastSamlResponse = JsonDocument.Parse(body!).RootElement.GetProperty("samlResponse").GetString();
                    return Json(LoginJson);

                case "/api/v1/sso/saml/metadata":
                    LastMetadataAcs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(request.RequestUri.Query)["acs"].ToString();
                    return Json("<EntityDescriptor/>");

                case "/api/v1/sso/complete-mfa":
                    LastMfaCode = JsonDocument.Parse(body!).RootElement.GetProperty("mfaCode").GetString();
                    return Json(LoginJson);

                default:
                    return Json("{}"); // security-profile and anything else the page touches
            }
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
