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

/// <summary>Plan M9 — similarity settings, report page, assignment option, workspace scores/recheck and the student's score, against a fake API.</summary>
public class SimilarityWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string OfferingId = "44444444-0000-0000-0000-000000000001";
    private const string AssignmentId = "c9000000-0000-0000-0000-000000000001";
    private const string SubmissionId = "79000000-0000-0000-0000-000000000001";
    private const string CheckId = "e9000000-0000-0000-0000-000000000001";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public SimilarityWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Settings_ShowTheProvider_AndSaveWithoutEchoingTheKey()
    {
        var api = new FakeApi("SuperAdmin");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/SimilaritySettings");
        Assert.Contains("data-similarity-settings", html);
        Assert.Contains("id=\"sim-copyleaks\" value=\"Copyleaks\" checked=\"checked\"", html);
        Assert.Contains("Saved — leave empty to keep", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("secret", html);
        Assert.Contains("data-webhook-missing", html);
        Assert.Contains("name=\"isEnabled\" value=\"false\"", html);

        var token = Token(html);
        var save = await client.PostAsync("/Portal/SaveSimilaritySettings", Form(token, ("provider", "Copyleaks"), ("isEnabled", "true"), ("isEnabled", "false"),
            ("scope", "Institution"), ("accountEmail", "checker@uni.edu"), ("apiKey", "new-key"), ("sandbox", "false"), ("installationDefault", "false")));
        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
        using (var doc = JsonDocument.Parse(api.Body("PUT", "/api/v1/similarity/settings")))
        {
            Assert.Equal("Copyleaks", doc.RootElement.GetProperty("provider").GetString());
            Assert.True(doc.RootElement.GetProperty("isEnabled").GetBoolean());
            Assert.Equal("Institution", doc.RootElement.GetProperty("scope").GetString());
            Assert.Equal("new-key", doc.RootElement.GetProperty("apiKey").GetString());
            Assert.False(doc.RootElement.GetProperty("sandbox").GetBoolean());
        }
        Assert.Equal("installationDefault=false", api.LastQuery);

        await client.PostAsync("/Portal/TestSimilaritySettings", Form(token, ("installationDefault", "false")));
        Assert.Contains(api.Writes, w => w == "POST /api/v1/similarity/settings/test");

        api.ModuleOff = true;
        Assert.Contains("data-module-off", await client.GetStringAsync("/Portal/SimilaritySettings"));
    }

    [Fact]
    public async Task Report_ShowsSourcesAndPassages_Encoded_ForStaffOnly()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/SimilarityReport/{CheckId}");
        var text = WebUtility.HtmlDecode(html);
        Assert.Contains("data-similarity-score", html);
        Assert.Contains("62.5%", html);
        Assert.Contains("data-source=\"Ann Lee — Essay\"", text);
        Assert.Contains("&lt;b&gt;Sunlight&lt;/b&gt; reaches", html); // passages are text, never markup
        Assert.Contains("href=\"https://example.org/sky\" target=\"_blank\" rel=\"noopener noreferrer nofollow\"", html);
        Assert.Contains("not proof of plagiarism", text);

        using var student = await SignedInClientAsync(new FakeApi("Student"));
        Assert.DoesNotContain("data-similarity-score", await (await student.GetAsync($"/Portal/SimilarityReport/{CheckId}")).Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Workspace_Options_AndStudentPage_ShowSimilarity()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var workspace = await client.GetStringAsync($"/Portal/GradingWorkspace?assignmentId={AssignmentId}");
        var config = JsonDocument.Parse(Regex.Match(workspace, "<script type=\"application/json\" id=\"workspace-config\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value).RootElement;
        var score = config.GetProperty("similarity")[0];
        Assert.Equal((SubmissionId, 62.5m), (score.GetProperty("submissionId").GetString(), score.GetProperty("score").GetDecimal()));
        Assert.Equal($"/Portal/SimilarityReport/__id__", config.GetProperty("urls").GetProperty("similarityReport").GetString());
        var recheck = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/Portal/WorkspaceSimilarityRecheck?submissionId={SubmissionId}")
        {
            Headers = { { "RequestVerificationToken", Token(workspace) } }
        });
        Assert.Equal(HttpStatusCode.Accepted, recheck.StatusCode);
        Assert.Contains(api.Writes, w => w == $"POST /api/v1/similarity/submissions/{SubmissionId}/recheck");

        var options = await client.GetStringAsync($"/Portal/AssignmentOptions?assignmentId={AssignmentId}");
        Assert.Contains("data-assignment-similarity", options);
        Assert.Contains("id=\"sim-on\" name=\"enabled\" value=\"true\" checked=\"checked\"", options);
        await client.PostAsync("/Portal/SaveAssignmentSimilarity", Form(Token(options), ("assignmentId", AssignmentId), ("enabled", "true"), ("enabled", "false"),
            ("visibleToStudents", "false")));
        Assert.Equal("{\"enabled\":true,\"visibleToStudents\":false}", api.Body("PUT", $"/api/v1/similarity/assignments/{AssignmentId}/settings"));

        using var student = await SignedInClientAsync(new FakeApi("Student"));
        var page = await student.GetStringAsync($"/Portal/AssignmentSubmission?assignmentId={AssignmentId}");
        Assert.Contains("data-my-similarity", page);
        Assert.Contains("62.5%", page);
    }

    // ── helpers ──

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }.Concat(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))));

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
        public bool ModuleOff { get; set; }
        public string? LastQuery { get; private set; }

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

        private static readonly string Settings = $$"""
            {"assignmentId":"{{AssignmentId}}","allowedAttempts":1,"allowedFileTypes":[".pdf"],"fileTypesRestricted":true,"maxFileSizeMb":10,
             "anonymousGrading":false,"namesHidden":false,"postingPolicy":"Automatic","moderatedGrading":false}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") { _writes.Add((request.Method.Method, path, body)); LastQuery = request.RequestUri.Query.TrimStart('?'); }

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000b2","key":"assignments","name":"Assignments","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000b4","key":"similarity_settings","name":"Similarity Checking","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path.StartsWith("/api/v1/similarity/") && ModuleOff)
                return Json("""{"message":"Module 'similarity' is not active."}""", HttpStatusCode.Forbidden);
            if (path == "/api/v1/similarity/settings" && request.Method == HttpMethod.Get)
                return Json("""{"tenantId":null,"provider":"Copyleaks","isEnabled":true,"scope":"Course","accountEmail":"checker@uni.edu","hasApiKey":true,"sandbox":true,"isComplete":true,"webhookReady":false,"isDefault":false}""");
            if (path == "/api/v1/similarity/settings/test") return Json("""{"message":"Connected to Copyleaks."}""");
            if (path == $"/api/v1/similarity/checks/{CheckId}")
                return role == "Student" ? Json("{}", HttpStatusCode.Forbidden) : Json($$"""
                    {"checkId":"{{CheckId}}","submissionId":"{{SubmissionId}}","assignmentId":"{{AssignmentId}}","assignmentTitle":"Essay","studentLabel":"Ben Ng","attemptNumber":1,
                     "provider":"Built-in comparison","status":"Completed","score":62.5,"error":null,"createdAt":"2026-10-19T00:00:00Z","completedAt":"2026-10-19T00:01:00Z","reportUrl":null,
                     "sources":[{"kind":"submission","label":"Ann Lee — Essay","url":null,"percent":62.5,"matchedWords":null,
                                 "passages":[{"text":"<b>Sunlight</b> reaches the atmosphere","sourceText":"Sunlight reaches the atmosphere"}]},
                                {"kind":"internet","label":"Why is the sky blue","url":"https://example.org/sky","percent":12,"matchedWords":30,"passages":[]}]}
                    """);
            if (path == $"/api/v1/similarity/assignments/{AssignmentId}/scores")
                return Json($$"""[{"submissionId":"{{SubmissionId}}","checkId":"{{CheckId}}","status":"Completed","score":62.5,"attemptNumber":1}]""");
            if (path == $"/api/v1/similarity/submissions/{SubmissionId}/recheck")
                return Json($$"""{"submissionId":"{{SubmissionId}}","checkId":"{{Guid.NewGuid()}}","status":"Queued","score":null,"attemptNumber":1}""", HttpStatusCode.Accepted);
            if (path == $"/api/v1/similarity/assignments/{AssignmentId}/settings")
                return Json($$"""{"assignmentId":"{{AssignmentId}}","enabled":true,"visibleToStudents":true,"provider":"Internal","providerEnabled":true}""");
            if (path == $"/api/v1/similarity/my/submissions/{SubmissionId}")
                return Json($$"""{"submissionId":"{{SubmissionId}}","status":"Completed","score":62.5}""");
            if (path == $"/api/v1/assignment/{AssignmentId}/workspace")
                return Json($$"""
                    {"assignmentId":"{{AssignmentId}}","title":"Essay","courseOfferingId":"{{OfferingId}}","maxMarks":20,"dueDate":"2026-10-30T00:00:00Z","settings":{{Settings}},
                     "submissions":[{"submissionId":"{{SubmissionId}}","studentLabel":"Ben Ng","status":"Submitted","isLate":false,"attemptNumber":1,"submittedAt":"2026-10-19T00:00:00Z","isPosted":false,"needsGrading":true,"provisionalGradeCount":0}],
                     "gradedCount":0,"postedCount":0,"gradedNotPostedCount":0,"hasRubric":false,"conversionEnabled":false,"canModerate":false}
                    """);
            if (path == $"/api/v1/assignment/{AssignmentId}/options")
                return Json($$"""{"assignmentId":"{{AssignmentId}}","dueDate":"2026-10-30T00:00:00Z","availableFrom":null,"availableUntil":null,"groupSetId":null,"gradeGroupIndividually":false,"peerReviewCount":0,"peerReviewDueDate":null,"peerReviewAnonymous":true,"hasSubmissions":true}""");
            if (path == $"/api/v1/assignment/{AssignmentId}/student-view")
                return Json($$"""
                    {"assignmentId":"{{AssignmentId}}","title":"Essay","description":"Write","dueDate":"2030-10-30T00:00:00Z","maxMarks":20,"isOpen":true,"settings":{{Settings}},
                     "submission":{"submissionId":"{{SubmissionId}}","assignmentId":"{{AssignmentId}}","assignmentTitle":"Essay","courseOfferingId":"{{OfferingId}}",
                       "maxMarks":20,"dueDate":"2030-10-30T00:00:00Z","studentLabel":"Me","status":"Submitted","currentAttemptNumber":1,"isLate":false,"submittedAt":"2026-10-19T00:00:00Z",
                       "isPosted":false,"gradeHiddenFromStudent":false,"attempts":[{"attemptNumber":1,"submittedAt":"2026-10-19T00:00:00Z","isLate":false,"textContent":"Hello","files":[]}],
                       "comments":[],"settings":{{Settings}},"canResubmit":false,"attemptsRemaining":0,"canModerate":false} }
                    """);
            if (path.StartsWith("/api/v1/similarity/") && request.Method != HttpMethod.Get) return Json("{}");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/overrides") || path.EndsWith("/group-sets") || path.EndsWith("/roster")
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
