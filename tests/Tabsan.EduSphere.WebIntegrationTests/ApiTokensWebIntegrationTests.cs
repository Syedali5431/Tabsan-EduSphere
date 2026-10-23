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

/// <summary>Plan M14.3 — the API Tokens page against a fake API: list, create (value shown once), revoke, administrators' list, access.</summary>
public class ApiTokensWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string TokenId = "aa000000-0000-0000-0000-0000000000a1";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public ApiTokensWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Admin_CreatesAToken_SeesItOnce_AndRevokesTokens()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var page = await client.GetStringAsync("/Portal/ApiTokens");
        Assert.Contains("Nightly SIS sync", page);
        Assert.Contains("esk_AbCdEfGh…", page);
        Assert.Contains("All tokens", page);
        Assert.Contains("someone.else", page);

        var created = await client.PostAsync("/Portal/CreateApiToken", Form(Token(page), ("name", "Reporting"), ("expiresInDays", "30"), ("readOnly", "true")));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        using (var doc = JsonDocument.Parse(api.Body("POST", "/api/v1/api-tokens")))
        {
            Assert.Equal("Reporting", doc.RootElement.GetProperty("name").GetString());
            Assert.Equal(30, doc.RootElement.GetProperty("expiresInDays").GetInt32());
            Assert.True(doc.RootElement.GetProperty("readOnly").GetBoolean());
        }
        var shown = await client.GetStringAsync("/Portal/ApiTokens");
        Assert.Contains("esk_the-new-secret-value", shown);
        Assert.Contains("https://api.test/api/v1/", shown);
        Assert.DoesNotContain("esk_the-new-secret-value", await client.GetStringAsync("/Portal/ApiTokens"));

        await client.PostAsync("/Portal/RevokeApiToken", Form(Token(shown), ("id", TokenId)));
        Assert.Contains(api.Writes, w => w == $"DELETE /api/v1/api-tokens/{TokenId}");
    }

    [Fact]
    public async Task Students_CannotOpenThePage()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var response = await client.GetAsync("/Portal/ApiTokens");
        Assert.DoesNotContain("Create token", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(api.Reads, r => r.StartsWith("/api/v1/api-tokens"));
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private static string Token(string html) => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Prepend(new("__RequestVerificationToken", token)));

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

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        private readonly List<(string Method, string Path, string Body)> _writes = new();
        public readonly List<string> Reads = new();

        public IEnumerable<string> Writes => _writes.Select(w => $"{w.Method} {w.Path}");
        public string Body(string method, string path) => _writes.Last(w => w.Method == method && w.Path == path).Body;

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private static string Item(string id, string user, string name) =>
            $$"""{"id":"{{id}}","userId":"{{Guid.NewGuid()}}","userName":"{{user}}","name":"{{name}}","hint":"esk_AbCdEfGh…","readOnly":false,"createdAt":"2026-10-01T00:00:00Z","expiresAt":"2026-12-30T00:00:00Z","active":true}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, path, body));
            else Reads.Add(path);

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000e1","key":"api_tokens","name":"API Tokens","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000e2","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/api-tokens" && request.Method == HttpMethod.Get) return Json("[" + Item(TokenId, "tester", "Nightly SIS sync") + "]");
            if (path == "/api/v1/api-tokens/all") return Json("[" + Item(TokenId, "tester", "Nightly SIS sync") + "," + Item(Guid.NewGuid().ToString(), "someone.else", "BI export") + "]");
            if (path == "/api/v1/api-tokens" && request.Method == HttpMethod.Post)
                return Json("{\"token\":" + Item(Guid.NewGuid().ToString(), "tester", "Reporting") + ",\"value\":\"esk_the-new-secret-value\"}", HttpStatusCode.Created);
            if (path.StartsWith("/api/v1/api-tokens/") && request.Method == HttpMethod.Delete) return new HttpResponseMessage(HttpStatusCode.NoContent);
            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]") : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
