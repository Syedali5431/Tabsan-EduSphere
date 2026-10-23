using System.Net;
using System.Net.Http.Headers;
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
/// Plan M5 — the portal's LTI endpoints against a fake API: launches (new tab and iframe), the tool's OIDC request
/// without a portal session, the deep-linking return, the service relays, the course card and the admin registry page.
/// </summary>
public class LtiWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string LinkId = "88888888-0000-0000-0000-000000000001";
    private const string ToolId = "88888888-0000-0000-0000-0000000000a1";
    private const string OfferingId = "44444444-0000-0000-0000-000000000001";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public LtiWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Launch_NewTab_AutoPostsTheLoginInitiation_AndKeepsOnlyLocalReturnUrls()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync($"/lti/launch/{LinkId}?returnUrl=%2FPortal%2FCourseLms%3FofferingId%3D{OfferingId}");
        Assert.Contains("action=\"https://tool.test/login\"", html);
        Assert.Contains("name=\"login_hint\" value=\"launch-123\"", html);
        Assert.Contains("name=\"iss\" value=\"https://platform.test\"", html);
        var call = api.Calls.Single(c => c.Path == $"/api/v1/lti/links/{LinkId}/launch");
        Assert.Equal("Bearer", call.Authorization?.Split(' ')[0]);
        Assert.Contains("returnUrl=%2FPortal%2FCourseLms", call.Query);

        await client.GetStringAsync($"/lti/launch/{LinkId}?returnUrl=https%3A%2F%2Fevil.test%2F");
        Assert.DoesNotContain("evil", api.Calls.Last(c => c.Path.EndsWith("/launch")).Query);
    }

    [Fact]
    public async Task Launch_Iframe_ShowsTheFrame_ThenTheFrameAutoPosts()
    {
        var api = new FakeApi("Student") { PresentationTarget = "iframe" };
        using var client = await SignedInClientAsync(api);

        var page = await client.GetStringAsync($"/lti/launch/{LinkId}");
        var src = Regex.Match(page, "<iframe src=\"([^\"]+)\"").Groups[1].Value;
        Assert.Contains("embedded=True", WebUtility.HtmlDecode(src), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("action=\"https://tool.test/login\"", page);

        var frame = await client.GetStringAsync(WebUtility.HtmlDecode(src));
        Assert.Contains("action=\"https://tool.test/login\"", frame);
    }

    [Fact]
    public async Task Launch_WithoutASession_GoesToLogin()
    {
        using var client = AnonymousClient(new FakeApi("Student"));
        var response = await client.GetAsync($"/lti/launch/{LinkId}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Login", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Authorize_WorksWithoutAPortalSession_AndAutoPostsTheIdToken(string method)
    {
        var api = new FakeApi("Student");
        using var client = AnonymousClient(api);
        var fields = new Dictionary<string, string>
        {
            ["scope"] = "openid", ["response_type"] = "id_token", ["response_mode"] = "form_post", ["prompt"] = "none",
            ["client_id"] = "edusphere-abc", ["redirect_uri"] = "https://tool.test/launch", ["login_hint"] = "launch-123",
            ["lti_message_hint"] = "m", ["state"] = "state-1", ["nonce"] = "nonce-1"
        };
        var response = method == "GET"
            ? await client.GetAsync("/lti/authorize?" + string.Join("&", fields.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}")))
            : await client.PostAsync("/lti/authorize", new FormUrlEncodedContent(fields)); // cross-site post: no antiforgery token

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("action=\"https://tool.test/launch\"", html);
        Assert.Contains("name=\"id_token\" value=\"header.payload.signature\"", html);
        Assert.Contains("name=\"state\" value=\"state-1\"", html);

        var call = api.Calls.Single(c => c.Path == "/api/v1/lti/authorize");
        Assert.Null(call.Authorization);
        Assert.Contains("\"loginHint\":\"launch-123\"", call.Body);
        Assert.Contains("\"nonce\":\"nonce-1\"", call.Body);
    }

    [Fact]
    public async Task Authorize_Refused_ShowsTheReason()
    {
        var api = new FakeApi("Student") { RefuseAuthorize = true };
        using var client = AnonymousClient(api);
        var response = await client.GetAsync("/lti/authorize?login_hint=old");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("This launch has expired.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeepLinkReturn_ForwardsTheToolsJwt_AndShowsWhatWasAdded()
    {
        var api = new FakeApi("Faculty");
        using var client = AnonymousClient(api);
        var response = await client.PostAsync("/lti/deep-link-return", new FormUrlEncodedContent(new Dictionary<string, string> { ["JWT"] = "tool.jwt.here" }));
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("2 items added to the course", html);
        Assert.Contains("Chapter 1", html);
        Assert.Contains($"/Portal/LmsManage?offeringId={OfferingId}", html);
        Assert.Contains("\"jwt\":\"tool.jwt.here\"", api.Calls.Single(c => c.Path == "/api/v1/lti/deep-link-return").Body);

        var empty = await client.PostAsync("/lti/deep-link-return", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task ServiceRelays_PassTheToolsTokenBodyStatusAndMediaTypes()
    {
        var api = new FakeApi("Student");
        using var client = AnonymousClient(api);

        var jwks = await client.GetAsync("/lti/jwks");
        Assert.Equal("application/json", jwks.Content.Headers.ContentType!.MediaType);
        Assert.Contains("\"keys\"", await jwks.Content.ReadAsStringAsync());

        var token = await client.PostAsync("/lti/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["client_assertion"] = "a.b.c"
        }));
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);
        Assert.Equal("no-store", token.Headers.CacheControl!.ToString());
        var tokenCall = api.Calls.Single(c => c.Path == "/api/v1/lti/token");
        Assert.Equal("application/x-www-form-urlencoded", tokenCall.ContentType);
        Assert.Equal("grant_type=client_credentials&client_assertion=a.b.c", tokenCall.Body);

        using var lineItems = new HttpRequestMessage(HttpMethod.Get, $"/lti/ags/{OfferingId}/lineitems?limit=5");
        lineItems.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "tool-access-token");
        lineItems.Headers.Accept.ParseAdd("application/vnd.ims.lis.v2.lineitemcontainer+json");
        var items = await client.SendAsync(lineItems);
        Assert.Equal("application/vnd.ims.lis.v2.lineitemcontainer+json", items.Content.Headers.ContentType!.MediaType);
        var agsCall = api.Calls.Single(c => c.Path == $"/api/v1/lti/ags/{OfferingId}/lineitems");
        Assert.Equal("Bearer tool-access-token", agsCall.Authorization);
        Assert.Equal("?limit=5", agsCall.Query);

        var denied = await client.GetAsync($"/lti/nrps/{OfferingId}/memberships");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Contains("Bearer", denied.Headers.WwwAuthenticate.ToString());

        var score = await client.PostAsync($"/lti/ags/{OfferingId}/lineitems/{LinkId}/scores",
            new StringContent("{\"scoreGiven\":8}", Encoding.UTF8, "application/vnd.ims.lis.v1.score+json"));
        Assert.Equal(HttpStatusCode.NoContent, score.StatusCode);
        Assert.Equal("application/vnd.ims.lis.v1.score+json", api.Calls.Last().ContentType);
    }

    [Fact]
    public async Task CourseCard_ShowsItemsToStudents_AndAddFormsToStaff()
    {
        var student = new FakeApi("Student");
        using (var client = await SignedInClientAsync(student))
        {
            var html = await client.GetStringAsync($"/Portal/CourseLms?offeringId={OfferingId}");
            Assert.Contains("id=\"external-tools\"", html);
            Assert.Contains("Reading quiz", html);
            Assert.Contains($"/lti/launch/{LinkId}?returnUrl=", html);
            Assert.DoesNotContain("Add an external tool", html);
        }

        var faculty = new FakeApi("Faculty");
        using (var client = await SignedInClientAsync(faculty))
        {
            var html = await client.GetStringAsync($"/Portal/LmsManage?offeringId={OfferingId}");
            Assert.Contains("Add an external tool", html);
            Assert.Contains("Choose content from H5P", html);
            Assert.Contains("name=\"visible\" value=\"false\"", html); // hide; a plain bool would render an empty value
            Assert.Contains($"action=\"/lti/deep-link/{OfferingId}\"", html);

            var post = await client.PostAsync("/Portal/CreateLtiLink", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = Token(html), ["offeringId"] = OfferingId, ["toolId"] = ToolId, ["title"] = "Week 2 quiz",
                ["presentationTarget"] = "iframe", ["gradeMaximum"] = "10", ["returnUrl"] = $"/Portal/LmsManage?offeringId={OfferingId}"
            }));
            Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
            Assert.Equal($"/Portal/LmsManage?offeringId={OfferingId}", post.Headers.Location!.ToString());
            var body = faculty.Calls.Single(c => c.Method == "POST" && c.Path == $"/api/v1/lti/offerings/{OfferingId}/links").Body;
            Assert.Contains("\"title\":\"Week 2 quiz\"", body);
            Assert.Contains("\"presentationTarget\":\"iframe\"", body);
            Assert.Contains("\"gradeMaximum\":10", body);

            var deepLink = await client.PostAsync($"/lti/deep-link/{OfferingId}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = Token(html), ["toolId"] = ToolId, ["returnUrl"] = "https://evil.test/"
            }));
            Assert.Contains("action=\"https://tool.test/login\"", await deepLink.Content.ReadAsStringAsync());
            var dl = faculty.Calls.Single(c => c.Path == $"/api/v1/lti/offerings/{OfferingId}/deep-linking").Body;
            Assert.Contains(ToolId, dl);
            Assert.DoesNotContain("evil", dl);
        }
    }

    [Fact]
    public async Task AdminPage_ListsPlatformValuesAndTools_AndSavesTools()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/LtiTools?edit={ToolId}");
        Assert.Contains("https://platform.test/lti/authorize", html);
        Assert.Contains("https://platform.test/lti/jwks", html);
        Assert.Contains("edusphere-abc", html);
        Assert.Contains("dep-0001", html);
        Assert.Contains("Edit H5P", html);
        Assert.DoesNotContain("Rotate the signing key", html); // Super Admins only

        var save = await client.PostAsync("/Portal/SaveLtiTool", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html), ["Id"] = ToolId, ["Name"] = "H5P", ["OidcInitiationUrl"] = "https://tool.test/login",
            ["TargetLinkUri"] = "https://tool.test/launch", ["RedirectUris"] = "https://tool.test/launch\r\nhttps://tool.test/launch2",
            ["JwksUrl"] = "https://tool.test/jwks", ["PrivacyLevel"] = "NameOnly", ["AllowGradeService"] = "true"
        }));
        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
        var body = api.Calls.Single(c => c.Method == "PUT" && c.Path == $"/api/v1/lti/tools/{ToolId}").Body;
        Assert.Contains("\"redirectUris\":[\"https://tool.test/launch\",\"https://tool.test/launch2\"]", body);
        Assert.Contains("\"privacyLevel\":\"NameOnly\"", body);
        Assert.Contains("\"allowGradeService\":true", body);
        Assert.Contains("\"allowRosterService\":false", body);
    }

    [Fact]
    public async Task AdminPage_IsNotForStudents_AndExplainsAnInactiveModule()
    {
        var student = new FakeApi("Student");
        using (var client = await SignedInClientAsync(student))
        {
            var response = await client.GetAsync("/Portal/LtiTools");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.DoesNotContain(student.Calls, c => c.Path == "/api/v1/lti/platform");
        }

        var admin = new FakeApi("SuperAdmin") { ModuleOff = true };
        using (var client = await SignedInClientAsync(admin))
        {
            var html = await client.GetStringAsync("/Portal/LtiTools");
            Assert.Contains("data-lti-module-off", html);
        }
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private WebApplicationFactory<WebEntryPointMarker> Factory(FakeApi api)
        => _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });

    private HttpClient AnonymousClient(FakeApi api)
        => Factory(api).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var client = AnonymousClient(api);
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

    private sealed record Call(string Method, string Path, string Query, string? Authorization, string? ContentType, string Body);

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        public List<Call> Calls { get; } = new();
        public string PresentationTarget { get; init; } = "window";
        public bool RefuseAuthorize { get; init; }
        public bool ModuleOff { get; init; }

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private const string Tool = $$"""
            {"id":"{{ToolId}}","name":"H5P","description":"Interactive content","clientId":"edusphere-abc","oidcInitiationUrl":"https://tool.test/login",
             "targetLinkUri":"https://tool.test/launch","deepLinkingUrl":"https://tool.test/deep","redirectUris":["https://tool.test/launch"],
             "jwksUrl":"https://tool.test/jwks","hasPublicKey":false,"privacyLevel":"Anonymous","allowGradeService":true,"allowRosterService":false,"isActive":true,
             "deployments":[{"id":"99999999-0000-0000-0000-000000000001","deploymentId":"dep-0001","scope":"Installation","isActive":true}]}
            """;

        private string Init => $$"""
            {"url":"https://tool.test/login","parameters":{"iss":"https://platform.test","login_hint":"launch-123","target_link_uri":"https://tool.test/launch","client_id":"edusphere-abc"},
             "presentationTarget":"{{PresentationTarget}}"}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (path.StartsWith("/api/v1/lti", StringComparison.Ordinal))
                Calls.Add(new Call(request.Method.Method, path, request.RequestUri.Query,
                    request.Headers.Authorization?.ToString(), request.Content?.Headers.ContentType?.MediaType, body));

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""
                    [{"id":"00000000-0000-0000-0000-0000000000b2","key":"lms_manage","name":"LMS","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},
                     {"id":"00000000-0000-0000-0000-0000000000b3","key":"external_tools","name":"External Tools","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]
                    """);
            if (path.StartsWith("/api/v1/lms/modules/")) return Json("[]");

            // LTI: course
            if (path == $"/api/v1/lti/links/{LinkId}/launch") return Json(Init);
            if (path == $"/api/v1/lti/offerings/{OfferingId}/deep-linking") return Json(Init);
            if (path == $"/api/v1/lti/offerings/{OfferingId}/links" && request.Method == HttpMethod.Get)
                return Json($$"""[{"id":"{{LinkId}}","toolId":"{{ToolId}}","toolName":"H5P","title":"Reading quiz","presentationTarget":"window","isVisible":true}]""");
            if (path == $"/api/v1/lti/offerings/{OfferingId}/links") return Json("{}", HttpStatusCode.Created);
            if (path == $"/api/v1/lti/offerings/{OfferingId}/tools")
                return Json($$"""[{"toolId":"{{ToolId}}","name":"H5P","supportsDeepLinking":true}]""");

            // LTI: protocol
            if (path == "/api/v1/lti/authorize")
                return RefuseAuthorize
                    ? Json("""{"message":"This launch has expired."}""", HttpStatusCode.BadRequest)
                    : Json("""{"redirectUri":"https://tool.test/launch","idToken":"header.payload.signature","state":"state-1"}""");
            if (path == "/api/v1/lti/deep-link-return")
                return Json($$"""{"courseOfferingId":"{{OfferingId}}","created":2,"titles":["Chapter 1","Chapter 2"],"message":"Done"}""");
            if (path == "/api/v1/lti/jwks") return Json("""{"keys":[]}""");
            if (path == "/api/v1/lti/token")
            {
                var response = Json("""{"access_token":"t","token_type":"Bearer","expires_in":3600,"scope":"x"}""");
                response.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true };
                return response;
            }
            if (path == $"/api/v1/lti/ags/{OfferingId}/lineitems")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.ims.lis.v2.lineitemcontainer+json");
                return response;
            }
            if (path.EndsWith("/scores")) return new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path.StartsWith("/api/v1/lti/nrps/"))
            {
                var response = Json("""{"message":"A valid access token is required."}""", HttpStatusCode.Unauthorized);
                response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer"));
                return response;
            }

            // LTI: admin
            if (path == "/api/v1/lti/platform")
                return ModuleOff
                    ? Json("""{"message":"Module 'lti' is disabled by license or configuration."}""", HttpStatusCode.Forbidden)
                    : Json("""
                        {"configured":true,"issuer":"https://platform.test","authorizationUrl":"https://platform.test/lti/authorize","tokenUrl":"https://platform.test/lti/token",
                         "jwksUrl":"https://platform.test/lti/jwks","deepLinkReturnUrl":"https://platform.test/lti/deep-link-return",
                         "keys":[{"kid":"k1","createdAt":"2026-10-01T00:00:00Z","active":true}]}
                        """);
            if (path == "/api/v1/lti/tools" && request.Method == HttpMethod.Get) return Json($"[{Tool}]");
            if (path.StartsWith("/api/v1/lti/tools")) return Json(Tool);

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("departments", StringComparison.OrdinalIgnoreCase) || path.Contains("offering", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
