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
/// Plan M13 — SCORM in the portal against a fake API: the teacher's package card (upload, edit, preview, remove, report),
/// the student's package page and launch, the player relay under /scorm/play/{token}/ (shell, content with byte ranges,
/// commits — all without a portal session) and the xAPI credentials page.
/// </summary>
public class ScormWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string OfferingId = "55555555-0000-0000-0000-000000000001";
    private const string PackageId = "c1000000-0000-0000-0000-0000000000c1";
    private const string CredentialId = "f1000000-0000-0000-0000-0000000000f1";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public ScormWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Teacher_UploadsEditsPreviewsAndRemovesPackages_OnTheContentPage()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var page = await client.GetStringAsync($"/Portal/LmsManage?offeringId={OfferingId}");
        Assert.Contains("id=\"scorm-packages\"", page);
        Assert.Contains("Fire safety", page);
        Assert.Contains("Upload a SCORM package", page);
        Assert.Contains("The package has 2 SCOs", page);
        var token = Token(page);

        var upload = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(OfferingId), "offeringId" },
            { new StringContent("true"), "graded" },
            { new StringContent("20"), "maxMarks" },
            { new StringContent("2"), "maxAttempts" },
            { new StringContent("true"), "publish" }
        };
        var zip = new ByteArrayContent([0x50, 0x4B, 0x03, 0x04]);
        zip.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        upload.Add(zip, "file", "fire.zip");
        var uploaded = await client.PostAsync("/Portal/UploadScormPackage", upload);
        Assert.Equal(HttpStatusCode.Redirect, uploaded.StatusCode);
        Assert.EndsWith("#scorm-packages", uploaded.Headers.Location!.ToString());
        var sent = api.Body("POST", $"/api/v1/scorm/offerings/{OfferingId}/packages");
        Assert.Contains("filename=fire.zip", sent);
        Assert.Contains("name=graded", sent);
        Assert.Contains("name=maxMarks", sent);

        await client.PostAsync("/Portal/UpdateScormPackage", Form(token, ("packageId", PackageId), ("offeringId", OfferingId), ("title", "Fire drill"),
            ("maxAttempts", "3"), ("isPublished", "true"), ("graded", "true"), ("maxMarks", "25")));
        using (var doc = JsonDocument.Parse(api.Body("PUT", $"/api/v1/scorm/packages/{PackageId}")))
        {
            Assert.Equal("Fire drill", doc.RootElement.GetProperty("title").GetString());
            Assert.Equal(25m, doc.RootElement.GetProperty("maxMarks").GetDecimal());
        }
        await client.PostAsync("/Portal/RemoveScormPackage", Form(token, ("packageId", PackageId), ("offeringId", OfferingId)));
        Assert.Contains(api.Writes, w => w == $"DELETE /api/v1/scorm/packages/{PackageId}");

        var preview = await client.PostAsync("/Portal/PreviewScorm", Form(token, ("packageId", PackageId), ("offeringId", OfferingId)));
        Assert.Equal(HttpStatusCode.Redirect, preview.StatusCode);
        var player = await client.GetStringAsync(preview.Headers.Location!.ToString());
        Assert.Contains("data-src=\"/scorm/play/prev-1/\"", player);
        Assert.Contains("Preview — nothing is recorded", player);
        // The play link is used once: a refresh goes back to the content page.
        var again = await client.GetAsync(preview.Headers.Location!.ToString());
        Assert.EndsWith("#scorm-packages", again.Headers.Location!.ToString());

        var report = await client.GetStringAsync($"/Portal/ScormReport?packageId={PackageId}");
        Assert.Contains("Ann Lee", report);
        Assert.Contains("85%", report);
        Assert.Contains("0:10:00", report);
    }

    [Fact]
    public async Task Student_SeesThePackage_LaunchesIt_AndPlaysItInTheFrame()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api, contentOrigin: "https://content.test");
        var page = await client.GetStringAsync($"/Portal/Scorm/{PackageId}");
        Assert.Contains("Fire safety", page);
        Assert.Contains(">Continue</button>", page);
        Assert.Contains("Start a new attempt", page);
        Assert.Contains("85%", page);

        var launch = await client.PostAsync("/Portal/LaunchScorm", Form(Token(page), ("packageId", PackageId), ("newAttempt", "true")));
        Assert.Equal(HttpStatusCode.Redirect, launch.StatusCode);
        Assert.Contains("\"newAttempt\":true", api.Body("POST", $"/api/v1/scorm/packages/{PackageId}/launch"));
        var player = await client.GetStringAsync(launch.Headers.Location!.ToString());
        Assert.Contains("data-src=\"https://content.test/scorm/play/tok-123/\"", player); // the separate content origin
        Assert.Contains("Attempt 2", player);
        Assert.Contains("data-scorm-exit", player);

        // Teachers' tools are not offered to students.
        Assert.DoesNotContain("Upload a SCORM package", await client.GetStringAsync($"/Portal/Scorm/{PackageId}"));
        var refused = await client.PostAsync("/Portal/PreviewScorm", Form(Token(page), ("packageId", PackageId), ("offeringId", OfferingId)));
        Assert.DoesNotContain(api.Writes, w => w.EndsWith("/preview"));
        Assert.DoesNotContain("ScormPlay", refused.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Player_RunsWithoutAPortalSession_AndRelaysContentAndCommits()
    {
        var api = new FakeApi("Student");
        using var player = Client(api, portalOrigin: "https://portal.test"); // no sign-in, no cookies

        var bare = await player.GetAsync("/scorm/play/tok-123");
        Assert.Equal(HttpStatusCode.Redirect, bare.StatusCode);
        Assert.Equal("/scorm/play/tok-123/", bare.Headers.Location!.ToString());

        var shell = await player.GetAsync("/scorm/play/tok-123/");
        var html = await shell.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, shell.StatusCode);
        Assert.Contains("/lib/scorm-again/scorm12.min.js", html);
        Assert.Contains("/js/scorm-player.js", html);
        Assert.Contains("\"launchUrl\":\"content/my%20course/index.html?x=1\"", html);
        Assert.Contains("student_id", html);
        Assert.DoesNotContain("<script>alert", html); // data is JSON-encoded inside the page
        Assert.Equal("no-referrer", shell.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("frame-ancestors 'self' https://portal.test", shell.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("no-store", shell.Headers.CacheControl!.ToString());

        Assert.Contains("This link has expired", await player.GetStringAsync("/scorm/play/expired/"));

        var content = await player.GetAsync("/scorm/play/tok-123/content/my%20course/index.html");
        Assert.Equal("text/html", content.Content.Headers.ContentType!.MediaType);
        Assert.Equal("<html>sco</html>", await content.Content.ReadAsStringAsync());
        Assert.Equal("nosniff", content.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("frame-ancestors 'self'", content.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("/api/v1/scorm/play/tok-123/content/my%20course/index.html", api.Reads);

        var ranged = new HttpRequestMessage(HttpMethod.Get, "/scorm/play/tok-123/content/media/clip.mp4");
        ranged.Headers.Range = new RangeHeaderValue(0, 3);
        var partial = await player.SendAsync(ranged);
        Assert.Equal(HttpStatusCode.PartialContent, partial.StatusCode);
        Assert.Equal("bytes 0-3/10", partial.Content.Headers.ContentRange!.ToString());
        Assert.Equal("bytes=0-3", api.LastRange);
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync("/scorm/play/tok-123/content/missing.js")).StatusCode);

        var commit = await player.PostAsync("/scorm/play/tok-123/commit?final=true",
            new StringContent("{\"cmi\":{\"core\":{\"lesson_status\":\"passed\"}}}", Encoding.UTF8, "text/plain"));
        Assert.Equal(HttpStatusCode.OK, commit.StatusCode);
        Assert.Contains("\"result\":true", await commit.Content.ReadAsStringAsync());
        Assert.Contains("lesson_status", api.Body("POST", "/api/v1/scorm/play/tok-123/commit"));
        Assert.Equal("?final=true", api.LastCommitQuery);
    }

    [Fact]
    public async Task Admin_CreatesLrsCredentials_SeesTheSecretOnce_AndSwitchesThemOff()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var page = await client.GetStringAsync("/Portal/XapiSettings");
        Assert.Contains("Learning Record Store (xAPI)", page);
        Assert.Contains("xapi_existing", page);
        Assert.Contains("https://api.test/api/v1/xapi/", page);
        Assert.Contains("verbs/completed", page);

        var created = await client.PostAsync("/Portal/CreateXapiCredential", Form(Token(page), ("name", "Articulate")));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Contains("\"name\":\"Articulate\"", api.Body("POST", "/api/v1/xapi/credentials"));
        var shown = await client.GetStringAsync("/Portal/XapiSettings");
        Assert.Contains("s3cr3t-once", shown);
        Assert.DoesNotContain("s3cr3t-once", await client.GetStringAsync("/Portal/XapiSettings"));

        await client.PostAsync("/Portal/DeactivateXapiCredential", Form(Token(shown), ("id", CredentialId)));
        Assert.Contains(api.Writes, w => w == $"DELETE /api/v1/xapi/credentials/{CredentialId}");

        using var student = await SignedInClientAsync(new FakeApi("Student"));
        Assert.DoesNotContain("Create credentials", await (await student.GetAsync("/Portal/XapiSettings")).Content.ReadAsStringAsync());
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private static string Token(string html) => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Prepend(new("__RequestVerificationToken", token)));

    private HttpClient Client(FakeApi api, string? contentOrigin = null, string? portalOrigin = null)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            if (contentOrigin is not null) b.UseSetting("Scorm:ContentOrigin", contentOrigin);
            if (portalOrigin is not null) b.UseSetting("Scorm:PortalOrigin", portalOrigin);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
    }

    private async Task<HttpClient> SignedInClientAsync(FakeApi api, string? contentOrigin = null)
    {
        var client = Client(api, contentOrigin);
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
        public string? LastRange { get; private set; }
        public string? LastCommitQuery { get; private set; }

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

        private static string Package(bool mine) => JsonSerializer.Serialize(new
        {
            id = PackageId, offeringId = OfferingId, title = "Fire safety", version = "SCORM 1.2", launchPath = "index.html", fileCount = 12, sizeBytes = 2_400_000,
            masteryScore = 80, maxAttempts = 3, isPublished = true, graded = true, maxMarks = 20, assignmentId = Guid.NewGuid(), createdAt = DateTime.UtcNow,
            scoCount = 2,
            mine = mine ? new { attemptsUsed = 1, status = "incomplete", bestScorePercent = 85, canStartNew = true, lastActivityAt = DateTime.UtcNow } : null
        });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri!;
            var path = uri.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, path, body));
            else Reads.Add(uri.AbsolutePath);

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000d1","key":"lms_manage","name":"LMS Manage","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000d2","key":"xapi_settings","name":"Learning Record Store","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000d3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");

            // SCORM
            if (path == $"/api/v1/scorm/offerings/{OfferingId}/packages" && request.Method == HttpMethod.Get) return Json("[" + Package(false) + "]");
            if (path == $"/api/v1/scorm/offerings/{OfferingId}/packages") return Json(Package(false), HttpStatusCode.Created);
            if (path == $"/api/v1/scorm/offerings/{OfferingId}/my-packages") return Json("[" + Package(true) + "]");
            if (path == $"/api/v1/scorm/packages/{PackageId}/mine") return Json(Package(true));
            if (path == $"/api/v1/scorm/packages/{PackageId}/launch")
                return Json("""{"token":"tok-123","packageId":"c1000000-0000-0000-0000-0000000000c1","title":"Fire safety","version":"SCORM 1.2","launchPath":"index.html","attemptNumber":2,"mode":"normal","attemptsUsed":2,"maxAttempts":3}""");
            if (path == $"/api/v1/scorm/packages/{PackageId}/preview")
                return Json("""{"token":"prev-1","packageId":"c1000000-0000-0000-0000-0000000000c1","title":"Fire safety","version":"SCORM 1.2","launchPath":"index.html","attemptNumber":0,"mode":"preview","attemptsUsed":0,"maxAttempts":3}""");
            if (path == $"/api/v1/scorm/packages/{PackageId}/report")
                return Json("{\"package\":" + Package(false) + ",\"learners\":[{\"studentProfileId\":\"" + Guid.NewGuid() + "\",\"name\":\"Ann Lee\",\"registrationNumber\":\"R-1\",\"attempts\":2,\"status\":\"completed\",\"successStatus\":\"passed\",\"bestScorePercent\":85,\"lastScorePercent\":40,\"totalSeconds\":600,\"lastActivityAt\":\"2026-10-22T10:00:00Z\"}]}");
            if (path == $"/api/v1/scorm/packages/{PackageId}" && request.Method == HttpMethod.Put) return Json(Package(false));
            if (path == $"/api/v1/scorm/packages/{PackageId}" && request.Method == HttpMethod.Delete) return new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path == "/api/v1/scorm/play/tok-123")
                return Json(JsonSerializer.Serialize(new
                {
                    packageId = PackageId, title = "Fire safety", version = "SCORM 1.2", launchPath = "my course/index.html?x=1", mode = "normal", attemptNumber = 2,
                    cmiJson = "{\"core\":{\"student_id\":\"R-1\",\"student_name\":\"<script>alert(1)</script>\"}}"
                }));
            if (path.StartsWith("/api/v1/scorm/play/expired")) return Json("{\"message\":\"This link has expired.\"}", HttpStatusCode.NotFound);
            if (path == "/api/v1/scorm/play/tok-123/content/my%20course/index.html")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>sco</html>", Encoding.UTF8, "text/html") };
            if (path == "/api/v1/scorm/play/tok-123/content/media/clip.mp4")
            {
                LastRange = request.Headers.TryGetValues("Range", out var r) ? string.Join(",", r) : null;
                var partial = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent([1, 2, 3, 4]) };
                partial.Content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
                partial.Content.Headers.ContentRange = new ContentRangeHeaderValue(0, 3, 10);
                return partial;
            }
            if (path == "/api/v1/scorm/play/tok-123/commit")
            {
                LastCommitQuery = uri.Query;
                return Json("{\"result\":true,\"errorCode\":0}");
            }
            if (path.StartsWith("/api/v1/scorm/play/")) return new HttpResponseMessage(HttpStatusCode.NotFound);

            // xAPI
            if (path == "/api/v1/xapi/credentials" && request.Method == HttpMethod.Get)
                return Json($$"""[{"id":"{{CredentialId}}","name":"Existing","key":"xapi_existing","offeringId":null,"isActive":true,"createdAt":"2026-10-01T00:00:00Z","lastUsedAt":null}]""");
            if (path == "/api/v1/xapi/credentials")
                return Json("""{"credential":{"id":"f2000000-0000-0000-0000-0000000000f2","name":"Articulate","key":"xapi_new","offeringId":null,"isActive":true,"createdAt":"2026-10-23T00:00:00Z","lastUsedAt":null},"secret":"s3cr3t-once"}""", HttpStatusCode.Created);
            if (path == $"/api/v1/xapi/credentials/{CredentialId}") return new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path == "/api/v1/xapi/recent")
                return Json("""[{"id":"11111111-0000-0000-0000-000000000001","actor":"mailto:ann@example.edu","verb":"http://adlnet.gov/expapi/verbs/completed","object":"https://x/activity","scoreScaled":0.9,"success":true,"completion":true,"timestamp":"2026-10-22T09:00:00Z","stored":"2026-10-22T09:00:01Z","voided":false,"credential":"Existing"}]""");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
