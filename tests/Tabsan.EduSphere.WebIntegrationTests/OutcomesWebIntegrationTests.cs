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
/// Plan M6 — outcome pages against a fake API: the outcomes page and form, the alignment relay, the mastery gradebook,
/// program attainment with exports, the student's page and an inactive module.
/// </summary>
public class OutcomesWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string OfferingId = "44444444-0000-0000-0000-000000000001";
    private const string ProgramId = "55555555-0000-0000-0000-000000000001";
    private const string CourseId = "66666666-0000-0000-0000-000000000001";
    private const string CloId = "77777777-0000-0000-0000-000000000001";
    private const string PloId = "77777777-0000-0000-0000-000000000002";
    private const string StudentId = "88888888-0000-0000-0000-000000000001";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public OutcomesWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task OutcomesPage_ListsOutcomes_AndAdminsGetEveryLevel()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/Outcomes?edit={CloId}");
        Assert.Contains("data-outcomes-table", html);
        Assert.Contains("CLO1", html);
        Assert.Contains("Write correct programs", html);
        Assert.Contains("Decaying average (65% latest)", html);
        Assert.Contains("value=\"Institution\"", html);
        Assert.Contains("Edit CLO1", html);
        Assert.Contains("data-outcome-mapping", html); // course outcome → program outcomes
        Assert.Contains($"value=\"{PloId}\"", html);
        Assert.Contains("checked=\"checked\"", html);
    }

    [Fact]
    public async Task OutcomesPage_TeachersOnlyCreateCourseOutcomes_StudentsAreKeptOut()
    {
        var faculty = new FakeApi("Faculty");
        using (var client = await SignedInClientAsync(faculty))
        {
            var html = await client.GetStringAsync("/Portal/Outcomes");
            var levelSelect = Regex.Match(html, @"id=""o-level""[\s\S]*?</select>").Value;
            Assert.DoesNotContain("Institution", levelSelect);
            Assert.Contains("Course (CLO)", html);
        }
        var student = new FakeApi("Student");
        using (var client = await SignedInClientAsync(student))
        {
            var response = await client.GetAsync("/Portal/Outcomes");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.DoesNotContain(student.Calls, c => c.Path == "/api/v1/outcomes");
        }
    }

    [Fact]
    public async Task SaveOutcome_SendsTheScaleWithoutEmptyRows()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/Outcomes");
        var form = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", Token(html)), new("Level", "Course"), new("CourseId", CourseId), new("Code", "CLO2"), new("Title", "Design algorithms"),
            new("RatingDescription", "Excellent"), new("RatingPoints", "10"), new("RatingDescription", "Pass"), new("RatingPoints", "6"),
            new("RatingDescription", ""), new("RatingPoints", ""), new("RatingDescription", "Fail"), new("RatingPoints", "0"),
            new("MasteryPoints", "6"), new("CalculationMethod", "NMastery"), new("CalculationParameter", "2")
        };
        var response = await client.PostAsync("/Portal/SaveOutcome", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"edit={CloId}", response.Headers.Location!.ToString());
        var body = api.Calls.Single(c => c.Method == "POST" && c.Path == "/api/v1/outcomes").Body;
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("Course", doc.RootElement.GetProperty("level").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("ratings").GetArrayLength());
        Assert.Equal("NMastery", doc.RootElement.GetProperty("calculationMethod").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("calculationParameter").GetInt32());
    }

    [Fact]
    public async Task AlignmentRelay_NeedsStaffAndTheAntiforgeryToken()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var get = await client.GetAsync($"/Portal/OutcomeAlignment?targetType=QuizQuestion&targetId={CloId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Contains("\"available\"", await get.Content.ReadAsStringAsync());
        Assert.Equal("?targetType=QuizQuestion&targetId=" + CloId, api.Calls.Last().Query);

        var json = $$"""{"targetType":"QuizQuestion","targetId":"{{CloId}}","outcomeIds":["{{CloId}}"]}""";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Portal/OutcomeAlignment", new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
        var page = await client.GetStringAsync("/Portal/Outcomes");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Portal/OutcomeAlignment") { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add("RequestVerificationToken", Token(page));
        var saved = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var put = api.Calls.Single(c => c.Method == "PUT" && c.Path == "/api/v1/outcomes/alignments");
        Assert.Contains(CloId, put.Body);

        var student = new FakeApi("Student");
        using var studentClient = await SignedInClientAsync(student);
        Assert.NotEqual(HttpStatusCode.OK, (await studentClient.GetAsync($"/Portal/OutcomeAlignment?targetType=Assignment&targetId={CloId}")).StatusCode);
    }

    [Fact]
    public async Task MasteryGradebook_ShowsStatuses_AndExports()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/OutcomeMastery?offeringId={OfferingId}");
        Assert.Contains("data-mastery-grid", html);
        Assert.Contains("data-status=\"mastered\"", html);
        Assert.Contains("data-status=\"none\"", html);
        Assert.Contains("1 of 1", html);
        Assert.Contains("Export to Excel", html);

        var export = await client.GetAsync($"/Portal/OutcomeMasteryExport?offeringId={OfferingId}");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.Content.Headers.ContentType!.MediaType);
        Assert.Equal("mastery-CS101.xlsx", export.Content.Headers.ContentDisposition!.FileNameStar ?? export.Content.Headers.ContentDisposition.FileName?.Trim('"'));
    }

    [Fact]
    public async Task Attainment_RendersTheReport_AndThePdf()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/OutcomeAttainment?programId={ProgramId}&target=80");
        Assert.Contains("data-attainment-table", html);
        Assert.Contains("PLO1", html);
        Assert.Contains("Met", html);
        Assert.Contains("<span class=\"badge bg-secondary\">No evidence</span>", html); // PLO2: the switch's default branch
        Assert.Contains("target=80", api.Calls.Last(c => c.Path.StartsWith("/api/v1/outcomes/attainment")).Query);
        var pdf = await client.GetAsync($"/Portal/OutcomeAttainmentExport?programId={ProgramId}&format=pdf&target=80");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.Contains("format=pdf", api.Calls.Last().Query);
    }

    [Fact]
    public async Task MyOutcomes_ShowsTheStudentsMastery_AndTheInactiveModule()
    {
        var api = new FakeApi("Student");
        using (var client = await SignedInClientAsync(api))
        {
            var html = await client.GetStringAsync("/Portal/MyOutcomes");
            Assert.Contains("data-outcome-item", html);
            Assert.Contains("Mastered", html);
            Assert.Contains("Evidence (1)", html);
            Assert.Contains("Essay", html);
        }
        var off = new FakeApi("Student") { ModuleOff = true };
        using (var client = await SignedInClientAsync(off))
            Assert.Contains("data-outcomes-module-off", await client.GetStringAsync("/Portal/MyOutcomes"));
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

    private sealed record Call(string Method, string Path, string Query, string Body);

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        public List<Call> Calls { get; } = new();
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

        private const string Ratings = """[{"description":"Exceeds mastery","points":4},{"description":"Meets mastery","points":3},{"description":"Near mastery","points":2},{"description":"Below mastery","points":1},{"description":"No evidence","points":0}]""";

        private static readonly string Clo = $$"""
            {"id":"{{CloId}}","level":"Course","courseId":"{{CourseId}}","courseName":"CS101 — Programming","code":"CLO1","title":"Write correct programs",
             "masteryPoints":3,"maxPoints":4,"calculationMethod":"DecayingAverage","calculationParameter":65,"isActive":true,"ratings":{{Ratings}},
             "programOutcomeIds":["{{PloId}}"],"alignmentCount":2,"isGlobal":true}
            """;

        private static readonly string Plo = $$"""
            {"id":"{{PloId}}","level":"Program","programId":"{{ProgramId}}","programName":"BSCS — Computer Science","code":"PLO1","title":"Solve computing problems",
             "masteryPoints":3,"maxPoints":4,"calculationMethod":"Highest","isActive":true,"ratings":{{Ratings}},"programOutcomeIds":[],"alignmentCount":1,"isGlobal":true}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (path.StartsWith("/api/v1/outcomes", StringComparison.Ordinal))
            {
                Calls.Add(new Call(request.Method.Method, path, request.RequestUri.Query, body));
                if (ModuleOff) return Json("""{"message":"Module 'outcomes' is disabled by license or configuration."}""", HttpStatusCode.Forbidden);
            }

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""
                    [{"id":"00000000-0000-0000-0000-0000000000b2","key":"outcomes","name":"Learning Outcomes","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},
                     {"id":"00000000-0000-0000-0000-0000000000b3","key":"my_outcomes","name":"My Outcomes","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]}]
                    """);

            if (path == "/api/v1/outcomes" && request.Method == HttpMethod.Get)
                return Json(request.RequestUri.Query.Contains("level=Program") ? $"[{Plo}]" : $"[{Clo},{Plo}]");
            if (path == "/api/v1/outcomes" && request.Method == HttpMethod.Post) return Json(Clo, HttpStatusCode.Created);
            if (path == $"/api/v1/outcomes/{CloId}") return Json(Clo);
            if (path == "/api/v1/outcomes/programs") return Json($$"""[{"id":"{{ProgramId}}","code":"BSCS","name":"Computer Science","departmentId":"{{ProgramId}}","departmentName":"Computing"}]""");
            if (path == "/api/v1/outcomes/alignments")
                return Json($$"""{"targetType":"QuizQuestion","targetId":"{{CloId}}","targetTitle":"Quiz — Q1","aligned":[{"id":"{{CloId}}","code":"CLO1","title":"Write correct programs","level":"Course"}],"available":[{"id":"{{CloId}}","code":"CLO1","title":"Write correct programs","level":"Course"}]}""");
            if (path == $"/api/v1/outcomes/mastery/offerings/{OfferingId}")
                return Json($$"""
                    {"courseOfferingId":"{{OfferingId}}","courseCode":"CS101","courseTitle":"Programming","semesterName":"Fall 2026",
                     "outcomes":[{"outcomeId":"{{CloId}}","code":"CLO1","title":"Write correct programs","level":"Course","masteryPoints":3,"maxPoints":4,"calculationMethod":"Highest","assessedCount":1,"masteredCount":1}],
                     "students":[{"studentProfileId":"{{StudentId}}","name":"Ayesha Khan","registrationNumber":"R-1","cells":[{"outcomeId":"{{CloId}}","score":3.5,"mastered":true,"evidence":2,"rating":"Meets mastery","status":"mastered"}]},
                                 {"studentProfileId":"88888888-0000-0000-0000-000000000002","name":"Bilal Ahmed","cells":[{"outcomeId":"{{CloId}}","mastered":false,"evidence":0,"status":"none"}]}]}
                    """);
            if (path == $"/api/v1/outcomes/mastery/offerings/{OfferingId}/export") return File("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "mastery-CS101.xlsx", "PK");
            if (path == $"/api/v1/outcomes/attainment/programs/{ProgramId}")
                return Json($$"""
                    {"programId":"{{ProgramId}}","programCode":"BSCS","programName":"Computer Science","departmentName":"Computing","generatedAt":"2026-10-19T00:00:00Z","targetPercent":80,"studentsInProgram":40,
                     "outcomes":[{"outcomeId":"{{PloId}}","code":"PLO1","title":"Solve computing problems","masteryPoints":3,"maxPoints":4,"calculationMethod":"Highest","studentsAssessed":30,"studentsMastered":27,
                                  "attainmentPercent":90,"averageScore":3.4,"targetMet":true,"mappedCourseOutcomes":["CLO1 Write correct programs"],
                                  "contributions":[{"courseCode":"CS101","courseTitle":"Programming","semesterName":"Fall 2026","students":30,"averagePercent":84.5}]},
                                 {"outcomeId":"77777777-0000-0000-0000-000000000003","code":"PLO2","title":"Communicate effectively","masteryPoints":3,"maxPoints":4,"calculationMethod":"Highest","studentsAssessed":0,"studentsMastered":0,
                                  "attainmentPercent":null,"averageScore":null,"targetMet":null,"mappedCourseOutcomes":[],"contributions":[]}]}
                    """);
            if (path == $"/api/v1/outcomes/attainment/programs/{ProgramId}/export") return File("application/pdf", "plo-attainment-BSCS.pdf", "%PDF-1.4");
            if (path == "/api/v1/outcomes/mastery/me")
                return Json($$"""
                    [{"courseOfferingId":"{{OfferingId}}","courseCode":"CS101","courseTitle":"Programming","semesterName":"Fall 2026",
                      "outcomes":[{"outcomeId":"{{CloId}}","code":"CLO1","title":"Write correct programs","level":"Course","score":3.5,"masteryPoints":3,"maxPoints":4,"mastered":true,"rating":"Meets mastery","status":"mastered",
                                   "evidence":[{"source":"Assignment","title":"Essay","score":3.5,"percent":0.875,"assessedAt":"2026-10-18T00:00:00Z"}]}]}]
                    """);

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("course", StringComparison.OrdinalIgnoreCase) || path.Contains("offering", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage File(string type, string name, string content)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.ASCII.GetBytes(content)) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(type);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = name };
            return response;
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
