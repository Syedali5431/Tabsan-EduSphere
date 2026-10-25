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
using Tabsan.EduSphere.Web.Controllers;

namespace Tabsan.EduSphere.WebIntegrationTests;

/// <summary>Plan M4 — manifest, service worker delivery, offline page and push relays in the real portal.</summary>
public class PwaWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public PwaWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Manifest_IsInstallable_AndUsesTheInstitutionsBranding()
    {
        var api = new FakeApi();
        using var client = await SignedInClientAsync(api);

        var response = await client.GetAsync("/manifest.webmanifest");
        Assert.Equal("application/manifest+json", response.Content.Headers.ContentType!.MediaType);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var m = doc.RootElement;
        Assert.Equal("University of Lahore Engineering", m.GetProperty("name").GetString());
        Assert.Equal("ULE", m.GetProperty("short_name").GetString());
        Assert.Equal("standalone", m.GetProperty("display").GetString());
        Assert.StartsWith("/Portal/Dashboard", m.GetProperty("start_url").GetString());
        var icons = m.GetProperty("icons").EnumerateArray().ToList();
        // The institution's logo first (the portal points logo URLs at the API that serves them).
        Assert.EndsWith("/api/v1/portal-settings/logo-files/logo.png", icons[0].GetProperty("src").GetString());
        Assert.Contains(icons, i => i.GetProperty("sizes").GetString() == "192x192");
        Assert.Contains(icons, i => i.GetProperty("sizes").GetString() == "512x512" && i.GetProperty("purpose").GetString() == "maskable");
        foreach (var icon in icons.Skip(1))
        {
            var png = await client.GetAsync(icon.GetProperty("src").GetString());
            Assert.Equal(HttpStatusCode.OK, png.StatusCode);
            Assert.Equal("image/png", png.Content.Headers.ContentType!.MediaType);
        }
    }

    [Fact]
    public async Task Manifest_WithoutASession_FallsBackToDefaults()
    {
        var client = _baseFactory.WithWebHostBuilder(b => b.UseEnvironment("Testing")).CreateClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/manifest.webmanifest"));
        Assert.Equal("Tabsan EduSphere", doc.RootElement.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("Tabsan EduSphere", "Tabsan")]
    [InlineData("University of the Punjab", "UP")]
    [InlineData("Short Name", "Short Name")]
    [InlineData("Internationalisation Institute", "Internationa")]
    public void ShortName_FitsTheHomeScreen(string name, string expected)
        => Assert.Equal(expected, PwaController.ShortName(name));

    [Fact]
    public async Task ServiceWorker_IsAlwaysRevalidated_AndOfflinePageExists()
    {
        var client = _baseFactory.WithWebHostBuilder(b => b.UseEnvironment("Testing")).CreateClient();
        var sw = await client.GetAsync("/sw.js");
        Assert.Equal(HttpStatusCode.OK, sw.StatusCode);
        Assert.Equal("no-cache", sw.Headers.CacheControl!.ToString());
        Assert.Contains("isLoginResponse", await sw.Content.ReadAsStringAsync());
        Assert.Contains("You are offline", await client.GetStringAsync("/offline.html"));
    }

    [Fact]
    public async Task Layout_LinksTheManifest_AndPushRelaysForwardToTheApi()
    {
        var api = new FakeApi();
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/Notifications");
        Assert.Contains("rel=\"manifest\" href=\"/manifest.webmanifest\"", html);
        Assert.Contains("js/pwa.js", html);
        Assert.Contains("id=\"push-device-card\"", html);

        // iPhone "Add to Home Screen" works the same from the sign-in page and the portal: an app named EduSphere with its icon.
        var signInPage = await _baseFactory.WithWebHostBuilder(b => b.UseEnvironment("Testing")).CreateClient().GetStringAsync("/Login");
        foreach (var page in new[] { html, signInPage })
        {
            Assert.Contains("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\" />", page);
            Assert.Contains("<meta name=\"apple-mobile-web-app-title\" content=\"EduSphere\" />", page);
            Assert.Contains("rel=\"apple-touch-icon\"", page);
        }

        var summary = await client.GetStringAsync("/Portal/PushSummary");
        Assert.Contains("\"publicKey\"", summary);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Portal/PushSubscribe")
        {
            Content = new StringContent("{\"endpoint\":\"https://fcm.googleapis.com/x\",\"p256dh\":\"k\",\"auth\":\"a\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("RequestVerificationToken", Token(html));
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(request)).StatusCode);
        Assert.Equal("POST /api/v1/push/subscriptions", api.LastWrite);

        var noToken = await client.PostAsync("/Portal/PushTest", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
    }

    [Fact]
    public async Task NotificationInbox_ShowsTheSentDateAndType_AndMarksTheRightNotificationRead()
    {
        var api = new FakeApi();
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/Notifications");
        Assert.Contains("Live class: Week 1", html);
        Assert.Contains("09 Oct 2026, 17:32", html); // the API's sentAt, not an empty date
        Assert.DoesNotContain("0001", html);
        Assert.Contains(">Announcement</span>", html);
        Assert.Contains($"name=\"id\" value=\"{FakeApi.NotificationId}\"", html);

        var read = await client.PostAsync("/Portal/MarkNotificationRead", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html), ["id"] = FakeApi.NotificationId
        }));
        Assert.Equal(HttpStatusCode.Redirect, read.StatusCode);
        Assert.Equal($"POST /api/v1/notification/{FakeApi.NotificationId}/read", api.LastNotificationWrite);
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var page = await client.GetStringAsync("/Login");
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(page), ["username"] = "tester", ["password"] = "Password#123"
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri(FakeApi.Base + "/") };
    }

    private sealed class FakeApi : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        public const string NotificationId = "5a080752-5ad8-46dc-88f0-1ca18d98e42b";
        public string? LastWrite { get; private set; }
        public string? LastNotificationWrite { get; private set; }

        private static string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = "Student", ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method != HttpMethod.Get && path.StartsWith("/api/v1/push")) LastWrite = $"{request.Method} {path}";
            if (request.Method != HttpMethod.Get && path.StartsWith("/api/v1/notification")) LastNotificationWrite = $"{request.Method} {path}";
            // The inbox as the API sends it (NotificationResponse).
            if (path == "/api/v1/notification/inbox")
                return Task.FromResult(Json($$"""[{"notificationId":"{{NotificationId}}","title":"Live class: Week 1","body":"CS702 Mon 26 Oct","type":"Announcement","isSystemGenerated":true,"sentAt":"2026-10-09T17:32:28.94","isRead":false,"readAt":null}]"""));

            if (path == "/api/v1/auth/login")
                return Task.FromResult(Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role = "Student", userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                })));
            if (path == "/api/v1/portal-settings")
                return Task.FromResult(Json("""{"universityName":"University of Lahore Engineering","portalSubtitle":"Student Portal","logoImage":"/api/v1/portal-settings/logo-files/logo.png"}"""));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Task.FromResult(Json("""[{"id":"00000000-0000-0000-0000-0000000000b3","key":"notifications","name":"Notifications","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]"""));
            if (path == "/api/v1/push" && request.Method == HttpMethod.Get)
                return Task.FromResult(Json("""{"enabled":true,"publicKey":"BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8","devices":[],"preferences":[]}"""));
            if (path.StartsWith("/api/v1/push")) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            return Task.FromResult(path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}"));
        }

        private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
