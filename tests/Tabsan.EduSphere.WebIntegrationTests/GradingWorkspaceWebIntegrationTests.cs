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
/// Plan M3 — grading workspace and student submission pages, rendered by the real portal against a fake API:
/// page rendering, JSON relays (antiforgery), multipart forwarding and file relays.
/// </summary>
public class GradingWorkspaceWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string AssignmentId = "66666666-0000-0000-0000-000000000001";
    private const string SubmissionId = "77777777-0000-0000-0000-000000000001";
    private const string FileId = "77777777-0000-0000-0000-000000000501";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public GradingWorkspaceWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Workspace_RendersConfigAndViewerScripts()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync($"/Portal/GradingWorkspace?assignmentId={AssignmentId}");

        Assert.Contains("id=\"grading-workspace\"", html);
        Assert.Contains("lib/pdfjs/pdf.min.js", html);
        Assert.Contains("grading-workspace.js", html);
        var config = Regex.Match(html, "<script type=\"application/json\" id=\"workspace-config\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
        using var doc = JsonDocument.Parse(config);
        Assert.Equal(SubmissionId, doc.RootElement.GetProperty("selected").GetString());
        Assert.Equal("Student 1", doc.RootElement.GetProperty("submissions")[0].GetProperty("studentLabel").GetString());
        Assert.Contains("submissionId=__id__", doc.RootElement.GetProperty("urls").GetProperty("grade").GetString());
        Assert.Contains("Post grades (1)", html);
        Assert.Contains("Anonymous", html);
    }

    [Fact]
    public async Task Workspace_IsNotForStudents()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var response = await client.GetAsync($"/Portal/GradingWorkspace?assignmentId={AssignmentId}");
        Assert.DoesNotContain("id=\"grading-workspace\"", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(api.Requests, r => r.EndsWith("/workspace"));
    }

    [Fact]
    public async Task GradeRelay_NeedsAntiforgery_AndForwardsJson()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/GradingWorkspace?assignmentId={AssignmentId}");

        var withoutToken = await client.PostAsync($"/Portal/WorkspaceGrade?submissionId={SubmissionId}",
            new StringContent("{\"marksAwarded\":15}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/Portal/WorkspaceGrade?submissionId={SubmissionId}")
        {
            Content = new StringContent("{\"marksAwarded\":15,\"feedback\":\"Good\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("RequestVerificationToken", Token(html));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"outcome\":\"saved\"", await response.Content.ReadAsStringAsync());
        Assert.Equal("PUT /api/v1/assignment/submissions/" + SubmissionId + "/grade", api.LastWrite);
        Assert.Contains("\"marksAwarded\":15", api.LastBody);

        using var bad = new HttpRequestMessage(HttpMethod.Post, $"/Portal/WorkspaceGrade?submissionId={SubmissionId}")
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json")
        };
        bad.Headers.Add("RequestVerificationToken", Token(html));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(bad)).StatusCode);
    }

    [Fact]
    public async Task StudentPage_RendersAttempts_AndSubmitForwardsFilesToTheApi()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync($"/Portal/AssignmentSubmission?assignmentId={AssignmentId}");
        Assert.Contains("Attempt 1", html);
        Assert.Contains("essay.pdf", html);
        Assert.Contains("data-kind=\"pdf\"", html);
        Assert.Contains("Submit again", html);
        Assert.Contains("17", html); // posted grade

        var form = new MultipartFormDataContent
        {
            { new StringContent(Token(html)), "__RequestVerificationToken" },
            { new StringContent(AssignmentId), "assignmentId" },
            { new StringContent("My answer"), "textContent" }
        };
        var file = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4 test"));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "files", "essay-v2.pdf");
        var post = await client.PostAsync("/Portal/SubmitAssignmentFiles", form);

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal($"POST /api/v1/assignment/{AssignmentId}/submissions", api.LastWrite);
        Assert.Contains("essay-v2.pdf", api.LastBody);
        Assert.Contains("My answer", api.LastBody);
    }

    [Fact]
    public async Task FileRelay_SendsAttachmentWithNoSniff()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var response = await client.GetAsync($"/Portal/SubmissionFile?fileId={FileId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(await response.Content.ReadAsByteArrayAsync()));
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

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        public List<string> Requests { get; } = new();
        public string? LastWrite { get; private set; }
        public string LastBody { get; private set; } = string.Empty;

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
            {"assignmentId":"{{AssignmentId}}","allowedAttempts":2,"allowedFileTypes":[".pdf",".docx"],"fileTypesRestricted":true,"maxFileSizeMb":10,
             "anonymousGrading":true,"namesHidden":true,"postingPolicy":"Manual","moderatedGrading":false}
            """;

        private static readonly string Workspace = $$"""
            {"assignmentId":"{{AssignmentId}}","title":"Essay","courseOfferingId":"44444444-0000-0000-0000-000000000001","maxMarks":20,"dueDate":"2026-10-30T00:00:00Z",
             "settings":{{Settings}},
             "submissions":[{"submissionId":"{{SubmissionId}}","studentLabel":"Student 1","status":"Submitted","isLate":false,"attemptNumber":1,"submittedAt":"2026-10-18T00:00:00Z","isPosted":false,"needsGrading":true,"provisionalGradeCount":0},
                            {"submissionId":"77777777-0000-0000-0000-000000000002","studentLabel":"Student 2","status":"Graded","isLate":true,"attemptNumber":1,"submittedAt":"2026-10-18T00:00:00Z","marksAwarded":14,"isPosted":false,"needsGrading":false,"provisionalGradeCount":0}],
             "gradedCount":1,"postedCount":0,"gradedNotPostedCount":1,"hasRubric":false,"conversionEnabled":false,"canModerate":false}
            """;

        private static readonly string StudentView = $$"""
            {"assignmentId":"{{AssignmentId}}","title":"Essay","description":"Write it","dueDate":"2030-10-30T00:00:00Z","maxMarks":20,"isOpen":true,"settings":{{Settings}},
             "submission":{"submissionId":"{{SubmissionId}}","assignmentId":"{{AssignmentId}}","assignmentTitle":"Essay","courseOfferingId":"44444444-0000-0000-0000-000000000001",
               "maxMarks":20,"dueDate":"2030-10-30T00:00:00Z","studentLabel":"Me","status":"Graded","currentAttemptNumber":1,"isLate":false,"submittedAt":"2026-10-18T00:00:00Z",
               "marksAwarded":17,"feedback":"Excellent","isPosted":true,"gradeHiddenFromStudent":false,
               "attempts":[{"attemptNumber":1,"submittedAt":"2026-10-18T00:00:00Z","isLate":false,"textContent":"Hello",
                 "files":[{"fileId":"{{FileId}}","fileName":"essay.pdf","contentType":"application/pdf","sizeBytes":10,"viewerKind":"pdf","conversionStatus":"NotRequired","isLegacy":false,"annotationCount":0}]}],
               "comments":[],"settings":{{Settings}},"canResubmit":true,"attemptsRemaining":1,"canModerate":false} }
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add(path);
            if (request.Method != HttpMethod.Get && path.StartsWith("/api/v1/assignment"))
            {
                LastWrite = $"{request.Method} {path}";
                LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            }

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000b2","key":"assignments","name":"Assignments","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == $"/api/v1/assignment/{AssignmentId}/workspace") return Json(Workspace);
            if (path == $"/api/v1/assignment/{AssignmentId}/student-view") return Json(StudentView);
            if (path == $"/api/v1/assignment/{AssignmentId}/submissions") return Json("{}");
            if (path == $"/api/v1/assignment/submissions/{SubmissionId}/grade")
                return Json("""{"outcome":"saved","marksAwarded":15,"isPosted":false,"provisional":false}""");
            if (path == $"/api/v1/assignment/submission-files/{FileId}")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4 fake")) };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "essay.pdf" };
                return response;
            }
            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
