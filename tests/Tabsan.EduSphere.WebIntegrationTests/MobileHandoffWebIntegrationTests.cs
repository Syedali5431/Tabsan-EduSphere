using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.Web;

namespace Tabsan.EduSphere.WebIntegrationTests;

/// <summary>
/// Plan M12 — the portal side of single sign-on for the mobile app: the app opens /Login/Sso/{provider} with its PKCE
/// challenge in the system browser; after the identity provider the portal asks the API for a one-time code, ends its own
/// session and sends the code to the app's URL scheme. Runs against a fake API.
/// </summary>
public class MobileHandoffWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string ApiBase = "https://api.test";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"; // 43 base64url characters
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public MobileHandoffWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task AppSignIn_EndsInTheAppWithAOneTimeCode_AndLeavesNoPortalSession()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);

        var start = await client.GetAsync($"/Login/Sso/google?appChallenge={Challenge}");
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        Assert.StartsWith("https://idp.test/authorize", start.Headers.Location!.ToString());
        Assert.Contains(start.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".Tabsan.EduSphere.AppSso=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal(new Uri("edusphere://sso?code=one-time-code"), callback.Headers.Location);
        Assert.Equal(Challenge, api.LastHandoffChallenge);
        Assert.Equal("Bearer header.payload.sig", api.LastHandoffAuthorization);
        Assert.Equal("refresh", api.LastLogoutRefreshToken); // the browser's own session is ended

        // The browser is not signed in to the portal: starting a portal sign-in goes to the identity provider again.
        var again = await client.GetAsync("/Login/Sso/google");
        Assert.StartsWith("https://idp.test/authorize", again.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("too-short")]
    [InlineData("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw+cM")]
    public async Task MalformedChallenge_IsRejectedBeforeTheIdentityProvider(string challenge)
    {
        var api = new FakeApi();
        using var client = CreateClient(api);

        var start = await client.GetAsync($"/Login/Sso/google?appChallenge={Uri.EscapeDataString(challenge)}");

        Assert.Equal(HttpStatusCode.BadRequest, start.StatusCode);
        Assert.Null(api.LastStartRedirectUri);
    }

    [Fact]
    public async Task PortalSignIn_AfterAnAbandonedAppSignIn_StaysInThePortal()
    {
        var api = new FakeApi();
        using var client = CreateClient(api);
        await client.GetAsync($"/Login/Sso/google?appChallenge={Challenge}");

        await client.GetAsync("/Login/Sso/google?returnUrl=%2FPortal%2FCourses");
        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");

        Assert.Equal("/Portal/Courses", callback.Headers.Location!.ToString());
        Assert.Null(api.LastHandoffChallenge);
    }

    [Fact]
    public async Task HandoffRefused_ShowsAnErrorInsteadOfOpeningTheApp()
    {
        var api = new FakeApi { HandoffStatus = HttpStatusCode.BadRequest };
        using var client = CreateClient(api);
        await client.GetAsync($"/Login/Sso/google?appChallenge={Challenge}");

        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");

        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        Assert.Contains("Single sign-on failed", await callback.Content.ReadAsStringAsync());
        Assert.Equal("refresh", api.LastLogoutRefreshToken);
    }

    [Theory]
    [InlineData("cityuni://sso", "cityuni://sso?code=one-time-code")]
    [InlineData("https://evil.test/steal", "edusphere://sso?code=one-time-code")]
    [InlineData("javascript:alert(1)", "edusphere://sso?code=one-time-code")]
    public async Task TheAppAddress_ComesFromConfiguration_AndIsNeverAWebAddress(string configured, string expected)
    {
        var api = new FakeApi();
        using var client = CreateClient(api, configured);
        await client.GetAsync($"/Login/Sso/google?appChallenge={Challenge}");

        var callback = await client.GetAsync($"/Login/SsoCallback?code=abc&state={FakeApi.State}");

        Assert.Equal(new Uri(expected), callback.Headers.Location);
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private HttpClient CreateClient(FakeApi api, string? appRedirectUri = null)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", ApiBase);
            if (appRedirectUri is not null) b.UseSetting("Mobile:AppRedirectUri", appRedirectUri);
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
        public HttpStatusCode HandoffStatus { get; init; } = HttpStatusCode.OK;
        public string? LastStartRedirectUri, LastHandoffChallenge, LastHandoffAuthorization, LastLogoutRefreshToken;

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
                    return Json("[{\"provider\":\"Google\",\"displayName\":\"Sign in with Google\"}]");

                case "/api/v1/sso/start":
                    LastStartRedirectUri = JsonDocument.Parse(body!).RootElement.GetProperty("redirectUri").GetString();
                    return Json($"{{\"authorizationUrl\":\"https://idp.test/authorize?state={State}\",\"state\":\"{State}\"}}");

                case "/api/v1/sso/complete":
                    return Json(LoginJson);

                case "/api/v1/mobile/handoff":
                    LastHandoffChallenge = JsonDocument.Parse(body!).RootElement.GetProperty("codeChallenge").GetString();
                    LastHandoffAuthorization = request.Headers.Authorization?.ToString();
                    return HandoffStatus == HttpStatusCode.OK
                        ? Json("{\"code\":\"one-time-code\",\"expiresInSeconds\":120}")
                        : Json("{\"error\":\"INVALID_CHALLENGE\"}", HandoffStatus);

                case "/api/v1/auth/logout":
                    LastLogoutRefreshToken = JsonDocument.Parse(body!).RootElement.GetProperty("refreshToken").GetString();
                    return new HttpResponseMessage(HttpStatusCode.NoContent);

                default:
                    return Json("{}");
            }
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
