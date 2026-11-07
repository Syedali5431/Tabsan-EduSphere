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
/// The student Results page, rendered by the real portal against a fake API that answers with the API's own result
/// shape (resultId, maxMarks, gradePoint and the course/student labels). The page used to read fields the API never
/// sends, so every row showed Course "—", Student "—", "33.00/0" and no grade.
/// </summary>
public class ResultsPageWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string SchoolResultId = "88888888-0000-0000-0000-000000000001";
    private const string UniversityTotalId = "88888888-0000-0000-0000-000000000002";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public ResultsPageWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task StudentResults_ShowCourseStudentMarksOutOfTheMaximumAndGradePoint()
    {
        var api = new FakeApi();
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync("/Portal/Results");
        var rows = Regex.Matches(html, "<tr>\\s*<td>(.*?)</td>\\s*<td>(.*?)</td>\\s*<td>.*?</td>\\s*<td>(.*?)</td>\\s*<td>(.*?)</td>", RegexOptions.Singleline)
            .Select(m => (Course: Decode(m.Groups[1].Value), Student: Decode(m.Groups[2].Value), Marks: Decode(m.Groups[3].Value), Grade: Decode(Regex.Replace(m.Groups[4].Value, "<.*?>", ""))))
            .ToList();

        Assert.Contains("/api/v1/result/my-results", api.Requests);
        Assert.Equal(2, rows.Count);

        // A School component result: no grade point, so the grade cell stays empty.
        Assert.Contains(rows, r => r.Course == "TMS-GUL-CSC09 — Computer Science (Class 9)"
                                   && r.Student == "Fatima Sheikh (TMS-GUL-09-01)"
                                   && r.Marks == "33/50"
                                   && r.Grade == "");
        // A University "Total" row: weighted marks out of the configured weightage, with its grade point.
        Assert.Contains(rows, r => r.Course == "CS-101 — Programming Fundamentals"
                                   && r.Marks == "72.5/100"
                                   && r.Grade == "3.33");

        // The re-check button now carries the real result id (it sent an empty id before).
        Assert.Contains($"data-resultid=\"{SchoolResultId}\"", html);
        Assert.Contains($"data-resultid=\"{UniversityTotalId}\"", html);
        Assert.DoesNotContain("data-resultid=\"00000000-0000-0000-0000-000000000000\"", html);
    }

    private static string Decode(string value) => WebUtility.HtmlDecode(value).Trim();

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
        public List<string> Requests { get; } = new();

        private static string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = "Student", ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        // The API's ResultResponse as api/v1/result/my-results serializes it (null gradePoint is omitted).
        private static readonly string MyResults = $$"""
            [{"resultId":"{{SchoolResultId}}","studentProfileId":"99999999-0000-0000-0000-000000000001","courseOfferingId":"44444444-0000-0000-0000-000000000001",
              "resultType":"Midterm","marksObtained":33.00,"maxMarks":50.00,"percentage":66.00,"isPublished":true,"publishedAt":"2026-10-14T12:35:54",
              "courseCode":"TMS-GUL-CSC09","courseTitle":"Computer Science (Class 9)","semesterName":"Academic Year 2026-27",
              "studentName":"Fatima Sheikh","registrationNumber":"TMS-GUL-09-01"},
             {"resultId":"{{UniversityTotalId}}","studentProfileId":"99999999-0000-0000-0000-000000000001","courseOfferingId":"44444444-0000-0000-0000-000000000002",
              "resultType":"Total","marksObtained":72.50,"maxMarks":100.00,"percentage":72.50,"gradePoint":3.33,"isPublished":true,"publishedAt":"2026-10-14T12:35:54",
              "courseCode":"CS-101","courseTitle":"Programming Fundamentals","semesterName":"Semester 1",
              "studentName":"Fatima Sheikh","registrationNumber":"TMS-GUL-09-01"}]
            """;

        private static readonly string[] ListPaths =
        [
            "/api/v1/course/offerings/my", "/api/v1/course/offerings", "/api/v1/department",
            "/api/v1/fyp/my-projects", "/api/v1/change-requests/my"
        ];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add(path);

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role = "Student", userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000b3","key":"results","name":"Results","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/result/my-results") return Json(MyResults);
            if (ListPaths.Contains(path)) return Json("[]");
            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static Task<HttpResponseMessage> Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
