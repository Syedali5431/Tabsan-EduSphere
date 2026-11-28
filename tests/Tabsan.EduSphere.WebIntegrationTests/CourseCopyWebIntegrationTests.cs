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
/// The Copy course content page against a fake API: the courses and templates to copy from, a source's content part by
/// part with what is not copied, copying the ticked items with the date shift, the result and history, template sharing
/// for admins only, the menu guard and Urdu.
/// </summary>
public class CourseCopyWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string TargetId = "c1000000-0000-0000-0000-000000000001";
    private const string SourceId = "c1000000-0000-0000-0000-000000000002";
    private const string TemplateId = "c1000000-0000-0000-0000-000000000003";
    private const string ModuleId = "d1000000-0000-0000-0000-000000000001";
    private const string EssayId = "d1000000-0000-0000-0000-000000000002";
    private const string PoemId = "d1000000-0000-0000-0000-000000000003";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public CourseCopyWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Teacher_ChoosesASource_SeesItsContent_AndCopiesTheTickedItems()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);

        var start = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/CourseCopy?offeringId={TargetId}"));
        Assert.Contains("ENG7 — English 7</strong> · Term 2", start);
        Assert.Contains("<optgroup label=\"Templates\">", start);
        Assert.Contains($"<option value=\"{TemplateId}\">English starter (ENG7 · Term 0)</option>", start);
        Assert.Matches($"<optgroup label=\"Your courses\">\\s*<option value=\"{SourceId}\">ENG7 — English 7 · Term 1 · Ms Khan</option>", start);
        Assert.DoesNotContain("data-copy-form", start);       // no source chosen yet
        Assert.DoesNotContain("data-course-template", start); // teachers do not share templates
        Assert.Contains("No content has been copied into this course yet.", start);
        Assert.Contains("/js/course-copy.js", start);

        var page = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/CourseCopy?offeringId={TargetId}&sourceId={SourceId}"));
        Assert.Contains("data-copy-part=\"modules\"", page);
        Assert.Contains("Week 1 · 5 items", page);
        Assert.Contains("Due 16 Feb 2026", page);
        Assert.Contains("Hidden from students", page);   // the poem is not published
        Assert.Contains("1 of 1 chosen", page);
        Assert.Contains("2 of 2 chosen", page);
        Assert.Contains("value=\"182\"", page);           // the suggested shift
        Assert.Contains("data-not-copied=\"scorm\"", page);
        Assert.Contains("River tour", page);
        Assert.Contains("SCORM packages aren't copied", page);
        Assert.Contains("Late and missing work policy", page);

        var copied = await client.PostAsync("/Portal/CopyCourseContent", Form(TokenOf(page), ("offeringId", TargetId), ("sourceId", SourceId),
            ("items", $"modules:{ModuleId}"), ("items", $"assignments:{EssayId}"), ("items", "junk"), ("shiftDays", "180"), ("keepPublished", "true")));
        Assert.Equal(HttpStatusCode.Redirect, copied.StatusCode);
        Assert.Equal($"/Portal/CourseCopy?offeringId={TargetId}", copied.Headers.Location!.ToString());
        using (var body = JsonDocument.Parse(api.Body("POST", $"/api/v1/course-copy/offerings/{TargetId}/copy")))
        {
            var r = body.RootElement;
            Assert.Equal(SourceId, r.GetProperty("sourceOfferingId").GetString());
            Assert.Equal(["modules", "assignments"], r.GetProperty("parts").EnumerateArray().Select(p => p.GetString()));
            Assert.Equal([ModuleId, EssayId], r.GetProperty("itemIds").EnumerateArray().Select(p => p.GetString()));
            Assert.Equal((180, true), (r.GetProperty("shiftDays").GetInt32(), r.GetProperty("keepPublished").GetBoolean()));
        }

        api.Copied = true;
        var done = WebUtility.HtmlDecode(await client.GetStringAsync(copied.Headers.Location!.ToString()));
        Assert.Contains("data-copy-result", done);
        Assert.Contains("The content was copied. What was published in the original is published here too.", done);
        Assert.Contains("Modules: 1", done);
        Assert.Contains("Dates moved by 180 days.", done);
        Assert.Contains("data-copy-run", done);
        Assert.Contains("180 days", done);

        var nothing = await client.PostAsync("/Portal/CopyCourseContent", Form(TokenOf(page), ("offeringId", TargetId), ("sourceId", SourceId), ("shiftDays", "0")));
        Assert.Equal($"/Portal/CourseCopy?offeringId={TargetId}&sourceId={SourceId}", nothing.Headers.Location!.ToString());
        Assert.Contains("Choose something to copy.", WebUtility.HtmlDecode(await client.GetStringAsync(nothing.Headers.Location!.ToString())));
        Assert.Single(api.Writes, w => w.StartsWith("POST", StringComparison.Ordinal)); // nothing sent the second time

        api.Refuse = true;
        var refused = await client.PostAsync("/Portal/CopyCourseContent", Form(TokenOf(page), ("offeringId", TargetId), ("sourceId", SourceId),
            ("items", $"assignments:{EssayId}"), ("shiftDays", "0")));
        Assert.Contains("You cannot copy from that course.", WebUtility.HtmlDecode(await client.GetStringAsync(refused.Headers.Location!.ToString())));
    }

    [Fact]
    public async Task Admin_SharesTheCourseAsATemplate_AndStopsSharing()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var page = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/CourseCopy?offeringId={TargetId}"));
        Assert.Contains("data-course-template=\"none\"", page);
        Assert.Contains("value=\"English 7\"", page); // the course title suggested as the name
        Assert.DoesNotContain("data-remove-template", page);

        var saved = await client.PostAsync("/Portal/SaveCourseTemplate", Form(TokenOf(page), ("offeringId", TargetId), ("name", "English starter"),
            ("description", "Weeks 1-10"), ("scope", "Institution")));
        Assert.Equal($"/Portal/CourseCopy?offeringId={TargetId}", saved.Headers.Location!.ToString());
        Assert.Equal("""{"name":"English starter","description":"Weeks 1-10","scope":"Institution"}""", api.Body("PUT", $"/api/v1/course-copy/offerings/{TargetId}/template"));

        api.IsTemplate = true;
        var shared = WebUtility.HtmlDecode(await client.GetStringAsync(saved.Headers.Location!.ToString()));
        Assert.Contains("The course is shared as a template.", shared);
        Assert.Contains("data-course-template=\"Institution\"", shared);
        Assert.Contains("Every teacher in the school", shared);
        Assert.Matches("id=\"template-scope-institution\" value=\"Institution\" checked=\"checked\"", shared);

        var removed = await client.PostAsync("/Portal/RemoveCourseTemplate", Form(TokenOf(shared), ("offeringId", TargetId)));
        Assert.Contains($"DELETE /api/v1/course-copy/offerings/{TargetId}/template", api.Writes);
        Assert.Contains("The course is no longer shared as a template.", WebUtility.HtmlDecode(await client.GetStringAsync(removed.Headers.Location!.ToString())));
    }

    [Fact]
    public async Task MenuWithoutCourseContent_ClosesThePage_AndUrduIsTranslated()
    {
        var closed = new FakeApi("Faculty") { MenuHasCourseContent = false };
        using (var client = await SignedInClientAsync(closed))
            Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync($"/Portal/CourseCopy?offeringId={TargetId}")).StatusCode);

        var api = new FakeApi("Faculty");
        using var urduClient = await SignedInClientAsync(api);
        var html = await urduClient.GetStringAsync($"/Portal/CourseCopy?offeringId={TargetId}");
        await urduClient.PostAsync("/Language/Set", Form(TokenOf(html), ("lang", "ur"), ("returnUrl", "/")));
        var urdu = WebUtility.HtmlDecode(await urduClient.GetStringAsync($"/Portal/CourseCopy?offeringId={TargetId}&sourceId={SourceId}"));
        Assert.Contains("<html lang=\"ur\" dir=\"rtl\"", urdu);
        Assert.Contains("کورس کا مواد کاپی کریں", urdu); // Copy course content
        Assert.Contains("1 میں سے 1 منتخب", urdu);       // 1 of 1 chosen
    }

    // ── helpers ──

    private static string TokenOf(string html)
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
            ["__RequestVerificationToken"] = TokenOf(page), ["username"] = "tester", ["password"] = "Password#123"
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
        public bool MenuHasCourseContent { get; set; } = true;
        public bool Copied { get; set; }
        public bool Refuse { get; set; }
        public bool IsTemplate { get; set; }
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

        private static string Offering(string id, string semester, string? teacher, string? template = null, bool mine = false)
            => $$"""{"offeringId":"{{id}}","courseCode":"ENG7","courseTitle":"English 7","semesterName":"{{semester}}","semesterStart":"2026-02-02T00:00:00","teacherName":{{(teacher is null ? "null" : $"\"{teacher}\"")}},"isTemplate":{{(template is null ? "false" : "true")}},"templateName":{{(template is null ? "null" : $"\"{template}\"")}},"templateScope":{{(template is null ? "null" : "\"Institution\"")}},"taughtByYou":{{(mine ? "true" : "false")}}}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, path, body));

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("[" + (MenuHasCourseContent ? """{"id":"00000000-0000-0000-0000-0000000000f1","key":"lms_manage","name":"Course Content","purpose":"","parentId":null,"displayOrder":20,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},""" : "")
                    + """{"id":"00000000-0000-0000-0000-0000000000e3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");

            const string copy = "/api/v1/course-copy/offerings/" + TargetId;
            if (path == copy + "/sources")
                return Json($$"""{"target":{{Offering(TargetId, "Term 2", "Ms Khan")}},"sources":[{{Offering(TemplateId, "Term 0", "Mr Shah", "English starter")}},{{Offering(SourceId, "Term 1", "Ms Khan", mine: true)}}]}""");
            if (path == copy + "/preview")
                return Json($$"""
                    {"source":{{Offering(SourceId, "Term 1", "Ms Khan")}},"target":{{Offering(TargetId, "Term 2", "Ms Khan")}},"suggestedShiftDays":182,
                     "parts":[
                       {"part":"modules","count":1,"items":[{"id":"{{ModuleId}}","title":"Week 1 — Rivers","week":1,"count":5,"published":true}]},
                       {"part":"assignments","count":2,"items":[{"id":"{{EssayId}}","title":"Essay on rivers","dueDate":"2026-02-16T00:00:00","published":true},{"id":"{{PoemId}}","title":"Poem","dueDate":"2026-03-04T00:00:00","published":false}]},
                       {"part":"settings","count":1,"items":[{"id":"d1000000-0000-0000-0000-000000000009","title":"Late and missing work policy","published":true}]}],
                     "notCopied":[{"reason":"scorm","title":"River tour","count":1}]}
                    """);
            if (path == copy + "/history")
                return Json(Copied
                    ? $$"""[{"id":"e1000000-0000-0000-0000-000000000001","sourceOfferingId":"{{SourceId}}","sourceTitle":"ENG7 — English 7","sourceSemester":"Term 1","copiedBy":"Ms Khan","copiedAt":"2026-11-27T09:00:00","shiftDays":180,"keptPublished":true,"copied":{"modules":1,"assignments":1},"notCopiedCount":0}]"""
                    : "[]");
            if (path == copy + "/template" && request.Method == HttpMethod.Get)
                return IsTemplate
                    ? Json($$"""{"offeringId":"{{TargetId}}","name":"English starter","description":"Weeks 1-10","scope":"Institution","courseCode":"ENG7","courseTitle":"English 7","semesterName":"Term 2"}""")
                    : new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path == copy + "/template" && request.Method == HttpMethod.Put)
                return Json($$"""{"offeringId":"{{TargetId}}","name":"English starter","scope":"Institution","courseCode":"ENG7","courseTitle":"English 7","semesterName":"Term 2"}""", HttpStatusCode.Created);
            if (path == copy + "/template" && request.Method == HttpMethod.Delete)
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path == copy + "/copy")
                return Refuse
                    ? Json("""{"message":"You cannot copy from that course."}""", HttpStatusCode.Forbidden)
                    : Json("""{"runId":"e1000000-0000-0000-0000-000000000001","copied":{"modules":1,"items":5,"assignments":1},"notCopied":[],"shiftDays":180,"keptPublished":true}""", HttpStatusCode.Created);
            if (path == "/api/v1/live/upcoming") return Json("[]");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
