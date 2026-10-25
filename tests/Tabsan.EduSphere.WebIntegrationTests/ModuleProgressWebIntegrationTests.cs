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
/// Plan M7 — module item editor, release settings, the student's course page, item pages, the previous/next bar and
/// the progress report, against a fake API.
/// </summary>
public class ModuleProgressWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string OfferingId = "44444444-0000-0000-0000-000000000001";
    private const string Week1 = "a1000000-0000-0000-0000-000000000001";
    private const string Week2 = "a1000000-0000-0000-0000-000000000002";
    private const string PageItem = "b1000000-0000-0000-0000-000000000001";
    private const string QuizItem = "b1000000-0000-0000-0000-000000000002";
    private const string LinkItem = "b1000000-0000-0000-0000-000000000003";
    private const string AssignmentItem = "b1000000-0000-0000-0000-000000000004";
    private const string AssignmentId = "c1000000-0000-0000-0000-000000000001";
    private const string QuizId = "c1000000-0000-0000-0000-000000000002";
    private const string StudentId = "d1000000-0000-0000-0000-000000000001";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public ModuleProgressWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task LmsManage_ShowsTheItemEditor_AndFormsReachTheApi()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/LmsManage?offeringId={OfferingId}");
        Assert.Contains($"data-module-editor=\"{Week1}\"", html);
        Assert.Contains("Syllabus", html);
        Assert.Contains("Score at least 60%", html);
        Assert.Contains("data-add-item-form", html);
        Assert.Contains($"value=\"{QuizId}\" data-type=\"Quiz\"", html); // linkable quiz
        Assert.Contains("module-editor.js", html);
        Assert.DoesNotContain("<h6>Videos</h6>", html); // videos are items now
        Assert.Contains("Student progress", html);

        var token = Token(html);
        var add = await client.PostAsync("/Portal/AddModuleItem", Form(token, ("moduleId", Week1), ("offeringId", OfferingId), ("itemType", "Quiz"),
            ("targetId", QuizId), ("title", ""), ("indentLevel", "1"), ("requirementType", "MustScoreAtLeast"), ("minScorePercent", "70")));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        var addCall = api.Calls.Single(c => c.Method == "POST" && c.Path == $"/api/v1/lms/modules/{Week1}/items");
        using (var doc = JsonDocument.Parse(addCall.Body))
        {
            Assert.Equal("Quiz", doc.RootElement.GetProperty("itemType").GetString());
            Assert.Equal(QuizId, doc.RootElement.GetProperty("targetId").GetString());
            Assert.Equal(70, doc.RootElement.GetProperty("minScorePercent").GetDecimal());
        }

        // Moving the second item up sends the new order.
        await client.PostAsync("/Portal/MoveModuleItem", Form(token, ("moduleId", Week1), ("itemId", AssignmentItem), ("direction", "-1"), ("offeringId", OfferingId)));
        var order = api.Calls.Single(c => c.Path == $"/api/v1/lms/modules/{Week1}/items/order").Body;
        Assert.Equal($"{{\"itemIds\":[\"{AssignmentItem}\",\"{PageItem}\"]}}", order);

        // Release: unlock date (UTC) and prerequisites.
        await client.PostAsync("/Portal/SaveModuleRelease", Form(token, ("moduleId", Week2), ("offeringId", OfferingId), ("unlockAt", "2026-11-01T09:30"),
            ("requireSequentialProgress", "true"), ("completionMode", "Any"), ("prerequisiteModuleIds", Week1)));
        var release = api.Calls.Single(c => c.Path == $"/api/v1/lms/modules/{Week2}/release").Body;
        Assert.Contains("\"unlockAt\":\"2026-11-01T09:30:00Z\"", release);
        Assert.Contains($"\"prerequisiteModuleIds\":[\"{Week1}\"]", release);
        Assert.Contains("\"completionMode\":\"Any\"", release);
    }

    [Fact]
    public async Task CourseLms_ShowsProgress_LockedItemsAsText_AndOpenItemsAsLinks()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/CourseLms?offeringId={OfferingId}");
        Assert.Contains("data-course-progress=\"50\"", html);
        Assert.Contains($"href=\"/Portal/ModuleItem/{PageItem}\"", html);
        Assert.DoesNotContain($"href=\"/Portal/ModuleItem/{QuizItem}\"", html);
        Assert.Contains("Complete “Week 1” first", WebUtility.HtmlDecode(html));
        Assert.Contains("data-status=\"done\"", html);
    }

    [Fact]
    public async Task CourseContent_OpensWithACourseChooser_AndShowsTheChosenCourse()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/CourseLms");
        Assert.Contains("href=\"/Portal/CourseLms\"", html); // the Course Content menu
        Assert.Contains("data-course-chooser", html);
        Assert.Contains("CS702 — Machine Learning (BSCS Semester 7)", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("Dropped course", html);
        Assert.Contains("Choose a course to see its modules.", html);
        Assert.DoesNotContain(api.Calls, c => c.Path.Contains(Guid.Empty.ToString()));

        var chosen = await client.GetStringAsync($"/Portal/CourseLms?offeringId={OfferingId}");
        Assert.Contains($"value=\"{OfferingId}\" selected=\"selected\"", chosen);
        Assert.Contains("data-course-progress=\"50\"", chosen);
    }

    [Fact]
    public async Task ModuleItem_RendersPages_LocksAndRedirects()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var page = await client.GetStringAsync($"/Portal/ModuleItem/{PageItem}");
        Assert.Contains("data-item-type=\"Page\"", page);
        Assert.Contains("Read me &lt;b&gt;carefully&lt;/b&gt;", page); // plain text, encoded
        Assert.Contains("data-module-nav", page);
        Assert.Contains($"href=\"/Portal/ModuleItem/{AssignmentItem}\"", page); // next

        var locked = await client.GetStringAsync($"/Portal/ModuleItem/{QuizItem}");
        Assert.Contains("data-module-item-locked", locked);
        Assert.Contains("Complete “Week 1” first", WebUtility.HtmlDecode(locked));

        var redirect = await client.GetAsync($"/Portal/ModuleItem/{AssignmentItem}");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal($"/Portal/AssignmentSubmission?assignmentId={AssignmentId}&moduleItem={AssignmentItem}", redirect.Headers.Location!.ToString());

        var link = await client.GetStringAsync($"/Portal/ModuleItem/{LinkItem}");
        Assert.Contains("href=\"https://example.org/reading\"", link);
        Assert.Contains("Mark as done", link);
    }

    [Fact]
    public async Task NavBar_AppearsOnPagesOpenedFromAModule_AndMarkDoneReturns()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/CourseLms?offeringId={OfferingId}&moduleItem={LinkItem}");
        Assert.Contains("data-module-nav", html);
        Assert.Contains("data-mark-done", html);
        Assert.Contains("name=\"done\" value=\"true\"", html); // a plain bool would render an empty value
        Assert.DoesNotContain("data-module-nav", await client.GetStringAsync($"/Portal/CourseLms?offeringId={OfferingId}"));

        var done = await client.PostAsync("/Portal/ModuleItemDone", Form(Token(html), ("itemId", LinkItem), ("done", "true"),
            ("returnUrl", $"/Portal/CourseLms?offeringId={OfferingId}&moduleItem={LinkItem}")));
        Assert.Equal(HttpStatusCode.Redirect, done.StatusCode);
        Assert.Equal($"/Portal/CourseLms?offeringId={OfferingId}&moduleItem={LinkItem}", done.Headers.Location!.ToString());
        Assert.Contains(api.Calls, c => c.Method == "POST" && c.Path == $"/api/v1/lms/module-items/{LinkItem}/done");

        var evil = await client.PostAsync("/Portal/ModuleItemDone", Form(Token(html), ("itemId", LinkItem), ("done", "false"), ("returnUrl", "https://evil.test/")));
        Assert.Equal($"/Portal/ModuleItem/{LinkItem}", evil.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ProgressReport_ShowsStudents_AndOneStudentsDetail()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/ModuleProgress?offeringId={OfferingId}&studentId={StudentId}");
        Assert.Contains("data-progress-table", html);
        Assert.Contains("Ayesha Malik", html);
        Assert.Contains("1/2", html);
        Assert.Contains("data-progress-detail", html);
        Assert.Contains("to do: Score at least 60%", html);

        var student = new FakeApi("Student");
        using var studentClient = await SignedInClientAsync(student);
        Assert.Equal(HttpStatusCode.Redirect, (await studentClient.GetAsync($"/Portal/ModuleProgress?offeringId={OfferingId}")).StatusCode);
    }

    // ── harness ──────────────────────────────────────────────────────────────

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

    private sealed record Call(string Method, string Path, string Body);

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        public List<Call> Calls { get; } = new();

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private const string AllowedAll = """["None","MustView","MustMarkDone","MustSubmit","MustScoreAtLeast"]""";

        private static readonly string Structure = $$"""
            {"courseOfferingId":"{{OfferingId}}","courseCode":"CS101","courseTitle":"Programming","modules":[
              {"id":"{{Week1}}","title":"Week 1","weekNumber":1,"body":"Intro","isPublished":true,"requireSequentialProgress":false,"completionMode":"All","prerequisiteModuleIds":[],
               "items":[{"id":"{{PageItem}}","moduleId":"{{Week1}}","itemType":"Page","title":"Syllabus","body":"Read","position":1,"indentLevel":0,"isPublished":true,"requirementType":"MustView","targetMissing":false,"allowedRequirements":["None","MustView","MustMarkDone"]},
                        {"id":"{{AssignmentItem}}","moduleId":"{{Week1}}","itemType":"Assignment","targetId":"{{AssignmentId}}","title":"Essay","position":2,"indentLevel":1,"isPublished":true,"requirementType":"MustScoreAtLeast","minScorePercent":60,"targetMissing":false,"allowedRequirements":{{AllowedAll}}}]},
              {"id":"{{Week2}}","title":"Week 2","weekNumber":2,"isPublished":true,"requireSequentialProgress":true,"completionMode":"All","prerequisiteModuleIds":["{{Week1}}"],"items":[]}]}
            """;

        private static readonly string MyModules = $$"""
            {"courseOfferingId":"{{OfferingId}}","courseCode":"CS101","courseTitle":"Programming","coursePercent":50,"modules":[
              {"id":"{{Week1}}","title":"Week 1","weekNumber":1,"locked":false,"completed":false,"required":2,"done":1,"percent":50,
               "items":[{"id":"{{PageItem}}","itemType":"Page","title":"Syllabus","indentLevel":0,"requirementType":"MustView","status":"done","canMarkDone":false},
                        {"id":"{{AssignmentItem}}","itemType":"Assignment","title":"Essay","indentLevel":0,"requirementType":"MustScoreAtLeast","minScorePercent":60,"status":"todo","canMarkDone":false}]},
              {"id":"{{Week2}}","title":"Week 2","weekNumber":2,"locked":true,"lockReason":"Complete “Week 1” first","completed":false,"required":1,"done":0,"percent":0,
               "items":[{"id":"{{QuizItem}}","itemType":"Quiz","title":"Quiz 1","indentLevel":0,"requirementType":"MustSubmit","status":"locked","lockReason":"Complete “Week 1” first","canMarkDone":false}]}]}
            """;

        private static string Open(string id, string type, string? target, string title, bool locked, string? reason, string? body, string? url, string requirement, string status, bool canMarkDone)
            => JsonSerializer.Serialize(new
            {
                itemId = id, itemType = type, targetId = target, title, courseOfferingId = OfferingId, locked, lockReason = reason, url, body,
                materialHasFile = false, requirementType = requirement, status, canMarkDone
            });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (path.StartsWith("/api/v1/lms", StringComparison.Ordinal)) Calls.Add(new Call(request.Method.Method, path, body));

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000b2","key":"lms_manage","name":"LMS","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000b4","key":"course_content","name":"Course Content","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/enrollment/my-courses")
                return Json($$"""[{"id":"{{Guid.NewGuid()}}","courseOfferingId":"{{OfferingId}}","courseTitle":"Machine Learning","courseCode":"CS702","semester":"BSCS Semester 7","status":"Active","enrolledAt":"2026-09-01T00:00:00Z"},{"id":"{{Guid.NewGuid()}}","courseOfferingId":"{{Guid.NewGuid()}}","courseTitle":"Dropped course","courseCode":"CS999","semester":"BSCS Semester 7","status":"Dropped","enrolledAt":"2026-09-01T00:00:00Z"}]""");
            if (path == $"/api/v1/lms/modules/{OfferingId}")
                return Json($$"""[{"id":"{{Week1}}","offeringId":"{{OfferingId}}","title":"Week 1","weekNumber":1,"body":"Intro","isPublished":true,"videos":[{"id":"{{Guid.NewGuid()}}","moduleId":"{{Week1}}","title":"Lecture","embedUrl":"https://v.example/1"}]}]""");
            if (path == $"/api/v1/lms/offerings/{OfferingId}/structure") return Json(Structure);
            if (path == $"/api/v1/lms/offerings/{OfferingId}/linkable")
                return Json($$"""[{"type":"Quiz","id":"{{QuizId}}","title":"Quiz 1"},{"type":"Assignment","id":"{{AssignmentId}}","title":"Essay","detail":"due 30 Oct 2026"}]""");
            if (path == $"/api/v1/lms/offerings/{OfferingId}/my-modules") return Json(MyModules);
            if (path == $"/api/v1/lms/offerings/{OfferingId}/progress")
                return Json($$"""
                    {"courseOfferingId":"{{OfferingId}}","courseCode":"CS101","courseTitle":"Programming",
                     "modules":[{"moduleId":"{{Week1}}","title":"Week 1","required":2},{"moduleId":"{{Week2}}","title":"Week 2","required":1}],
                     "students":[{"studentProfileId":"{{StudentId}}","name":"Ayesha Malik","registrationNumber":"R-1","coursePercent":33,
                                  "modules":[{"moduleId":"{{Week1}}","done":1,"required":2,"percent":50,"completed":false,"locked":false},
                                             {"moduleId":"{{Week2}}","done":0,"required":1,"percent":0,"completed":false,"locked":true}]}]}
                    """);
            if (path == $"/api/v1/lms/offerings/{OfferingId}/progress/{StudentId}") return Json(MyModules);
            if (path.EndsWith("/open"))
            {
                if (path.Contains(PageItem)) return Json(Open(PageItem, "Page", null, "Syllabus", false, null, "Read me <b>carefully</b>", null, "MustView", "done", false));
                if (path.Contains(QuizItem)) return Json(Open(QuizItem, "Quiz", QuizId, "Quiz 1", true, "Complete “Week 1” first", null, null, "MustSubmit", "locked", false));
                if (path.Contains(AssignmentItem)) return Json(Open(AssignmentItem, "Assignment", AssignmentId, "Essay", false, null, null, null, "MustScoreAtLeast", "todo", false));
                if (path.Contains(LinkItem)) return Json(Open(LinkItem, "ExternalUrl", null, "Reading", false, null, null, "https://example.org/reading", "MustMarkDone", "todo", true));
            }
            if (path.EndsWith("/nav"))
                return Json($$"""{"itemId":"{{LinkItem}}","moduleTitle":"Week 1","previous":{"id":"{{PageItem}}","title":"Syllabus","locked":false},"next":{"id":"{{AssignmentItem}}","title":"Essay","locked":false},"status":"todo","canMarkDone":true,"courseOfferingId":"{{OfferingId}}"}""");
            if (path.EndsWith("/done")) return Json(Open(LinkItem, "ExternalUrl", null, "Reading", false, null, null, "https://example.org/reading", "MustMarkDone", "done", true));
            if (path.StartsWith("/api/v1/lms/")) return request.Method == HttpMethod.Post && path.EndsWith("/items") ? Json("{}", HttpStatusCode.Created) : new HttpResponseMessage(HttpStatusCode.NoContent);

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
