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
/// Two faults found by opening every portal page as every role (2026-11-08), checked against the real portal and a fake API:
/// a parent's sign-in looped between Dashboard and Helpdesk until the browser gave up (parents have no portal pages), and
/// the Prerequisites page asked the API course by course, so 600 courses used up the per-user request limit in one load.
/// </summary>
public class PortalLandingWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public PortalLandingWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Parent_LandsOnStart_ThatPointsToTheApp_AndNoPageLoops()
    {
        var api = new FakeApi("Parent", visibleMenus: "[]");
        var (client, landing) = await SignInAsync(api);

        Assert.Equal("/Portal/Start", landing);
        var start = await client.GetAsync("/Portal/Start");
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var html = WebUtility.HtmlDecode(await start.Content.ReadAsStringAsync());
        Assert.Contains("Parents use the EduSphere mobile app", html);
        Assert.Contains("type the server address your school gave you", html);
        // the menu offers no staff pages, which would only answer "access denied"
        Assert.Contains("href=\"/Portal/Start\"", html);
        Assert.DoesNotContain("href=\"/Portal/Analytics\"", html);
        Assert.DoesNotContain("href=\"/Portal/ReportCenter\"", html);
        Assert.DoesNotContain("href=\"/Portal/Assignments\"", html);

        // Helpdesk is refused (no menu) and the Dashboard is SuperAdmin-only: both end on Start, in at most two steps.
        Assert.Equal("/Portal/Start", await FollowAsync(client, "/Portal/Helpdesk"));
        Assert.Equal("/Portal/Start", await FollowAsync(client, "/Portal/Dashboard"));
    }

    [Fact]
    public async Task StudentWithoutHelpdesk_IsSentToStart_WithTheReason_InsteadOfLooping()
    {
        var api = new FakeApi("Student", visibleMenus: Menu("results"));
        var (client, _) = await SignInAsync(api);

        // one step, so the reason (a one-time message) is still there when Start opens
        var refused = await client.GetAsync("/Portal/Helpdesk");
        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.Equal("/Portal/Start", refused.Headers.Location!.OriginalString.Split('?')[0]);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Start"));
        Assert.Contains("Access denied for this section", html);
        Assert.Contains("Choose a page from the menu.", html);
        Assert.DoesNotContain("Parents use the EduSphere mobile app", html);
    }

    [Fact]
    public async Task Prerequisites_AskOncePerDepartment_NotOncePerCourse()
    {
        var api = new FakeApi("SuperAdmin", visibleMenus: Menu("prerequisites"));
        var (client, _) = await SignInAsync(api);

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Prerequisites"));

        Assert.Contains("MTH-101", html); // MTH-201's prerequisite, from the department call
        var prerequisiteCalls = api.Requests.Where(r => r.StartsWith("/api/v1/prerequisite", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, prerequisiteCalls.Count); // two departments
        Assert.All(prerequisiteCalls, r => Assert.Contains("?departmentId=", r));
        Assert.DoesNotContain(prerequisiteCalls, r => Regex.IsMatch(r, @"^/api/v1/prerequisite/[0-9a-f-]{36}$"));
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private static string Menu(string key)
        => $$"""[{"id":"{{Guid.NewGuid()}}","key":"{{key}}","name":"{{key}}","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""";

    /// <summary>Follows the portal's redirects (at most five) and returns where they end.</summary>
    private static async Task<string> FollowAsync(HttpClient client, string path)
    {
        for (var hop = 0; hop < 5; hop++)
        {
            var response = await client.GetAsync(path);
            if (response.StatusCode != HttpStatusCode.Redirect && response.StatusCode != HttpStatusCode.Found)
                return path;
            path = response.Headers.Location!.IsAbsoluteUri ? response.Headers.Location.AbsolutePath : response.Headers.Location.OriginalString.Split('?')[0];
        }
        throw new Xunit.Sdk.XunitException($"Still redirecting after five steps (last: {path}) — a redirect loop.");
    }

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<(HttpClient Client, string Landing)> SignInAsync(FakeApi api)
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
        return (client, login.Headers.Location!.OriginalString.Split('?')[0]);
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri(FakeApi.Base + "/") };
    }

    private sealed class FakeApi(string role, string visibleMenus) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        private const string MathsDept = "d0000000-0000-0000-0000-000000000001";
        private const string ScienceDept = "d0000000-0000-0000-0000-000000000002";
        private const string Mth101 = "c0000000-0000-0000-0000-000000000101";
        private const string Mth201 = "c0000000-0000-0000-0000-000000000201";
        private const string Phy101 = "c0000000-0000-0000-0000-000000000301";

        /// <summary>Paths with their query strings, in order.</summary>
        public List<string> Requests { get; } = new();

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private static string Course(string id, string code, string title, string dept)
            => $$"""{"id":"{{id}}","title":"{{title}}","code":"{{code}}","creditHours":3,"departmentId":"{{dept}}","institutionType":0,"departmentName":"Dept","isActive":true,"hasSemesters":true,"gradingType":"GPA"}""";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add(path + request.RequestUri.Query);

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible") return Json(visibleMenus);
            if (path == "/api/v1/course")
                return Json($"[{Course(Mth101, "MTH-101", "Algebra", MathsDept)},{Course(Mth201, "MTH-201", "Calculus", MathsDept)},{Course(Phy101, "PHY-101", "Mechanics", ScienceDept)}]");
            if (path == "/api/v1/prerequisite")
                return Json(request.RequestUri.Query.Contains(MathsDept)
                    ? $$"""[{"courseId":"{{Mth201}}","courseCode":"MTH-201","courseTitle":"Calculus","prerequisiteCourseId":"{{Mth101}}","prerequisiteCourseCode":"MTH-101","prerequisiteCourseTitle":"Algebra"}]"""
                    : "[]");
            if (path.StartsWith("/api/v1/prerequisite/", StringComparison.Ordinal)) return Json("[]");
            if (path is "/api/v1/department" or "/api/v1/helpdesk/tickets" or "/api/v1/helpdesk/tickets/my") return Json("[]");
            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static Task<HttpResponseMessage> Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
