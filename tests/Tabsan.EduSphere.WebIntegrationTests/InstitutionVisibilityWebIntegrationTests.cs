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
/// A School-only installation in the portal: the API's capability matrix shows School only, so the dashboard, the
/// institution choices and the defaults leave College and University out; with every type shown, nothing changes.
/// </summary>
public class InstitutionVisibilityWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public InstitutionVisibilityWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task SchoolOnly_Dashboard_ShowsTheSchoolCardOnly_AndPolicyPageSaysWhatIsHidden()
    {
        var api = new FakeApi { School = true, College = false, University = false };
        using var client = await SignedInClientAsync(api);

        var dashboard = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Dashboard"));
        Assert.Contains("School mode", dashboard);
        Assert.DoesNotContain("College mode", dashboard);
        Assert.DoesNotContain("University mode", dashboard);
        Assert.DoesNotContain("School / College / University", dashboard);
        Assert.DoesNotContain("/Portal/StudyPlan", dashboard); // University-only, also among the SuperAdmin's own links
        Assert.DoesNotContain("/Portal/DegreeAudit", dashboard);

        var policy = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/InstitutionPolicy"));
        Assert.Contains("data-institution-hidden", policy);
        Assert.Contains("Hidden until their first department is added: University, College.", policy);
    }

    [Fact]
    public async Task SchoolOnly_InstitutionChoices_ListSchoolOnly_AndDefaultToSchool()
    {
        var api = new FakeApi { School = true, College = false, University = false };
        using var client = await SignedInClientAsync(api);

        var grading = await client.GetStringAsync("/Portal/GradingConfig");
        var options = Options(grading, "institutionType");
        Assert.Equal(["1"], options.Select(o => o.Value));
        Assert.True(options.Single().Selected); // School chosen, not University

        await client.GetStringAsync("/Portal/ResultCalculation");
        Assert.Contains(api.Queries("/api/v1/result-calculation"), q => q.Contains("institutionType=1"));
        Assert.Equal(["1"], Options(await client.GetStringAsync("/Portal/ResultCalculation"), "selectedInstitutionType").Select(o => o.Value));

        var lifecycle = Options(await client.GetStringAsync("/Portal/StudentLifecycle"), "institutionType").Select(o => o.Value);
        Assert.Equal(["", "1"], lifecycle); // "All" and School
    }

    [Fact]
    public async Task SchoolOnly_Payments_AsksTheApiForSchoolReceipts()
    {
        var api = new FakeApi { School = true, College = false, University = false };
        using var client = await SignedInClientAsync(api);

        var payments = await client.GetStringAsync("/Portal/Payments");
        Assert.Equal(["", "1"], Options(payments, "institutionType").Select(o => o.Value)); // "All" and School
        Assert.Contains(api.Queries("/api/v1/payments"), q => q.Contains("institutionType=1")); // School, not University (0)
    }

    [Fact]
    public async Task EveryTypeShown_NothingIsHidden()
    {
        var api = new FakeApi { School = true, College = true, University = true };
        using var client = await SignedInClientAsync(api);

        var dashboard = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Dashboard"));
        Assert.Contains("School mode", dashboard);
        Assert.Contains("College mode", dashboard);
        Assert.Contains("University mode", dashboard);
        Assert.Contains("School / College / University", dashboard);
        Assert.Contains("/Portal/StudyPlan", dashboard);
        Assert.DoesNotContain("data-institution-hidden", await client.GetStringAsync("/Portal/InstitutionPolicy"));

        await client.GetStringAsync("/Portal/ResultCalculation");
        Assert.Contains(api.Queries("/api/v1/result-calculation"), q => q.Contains("institutionType=0")); // University, as before
        Assert.Equal(["1", "2", "0"], Options(await client.GetStringAsync("/Portal/GradingConfig"), "institutionType").Select(o => o.Value));
    }

    [Fact]
    public async Task Departments_ShowTheInstitutionBadge_ForEachType()
    {
        var api = new FakeApi
        {
            School = true, College = true, University = true,
            DepartmentsJson = """
                [{"id":"99999999-0000-0000-0000-000000000001","name":"Primary","code":"PRI","isActive":true,"institutionType":1},
                 {"id":"99999999-0000-0000-0000-000000000002","name":"Pre-Medical","code":"PMED","isActive":true,"institutionType":2},
                 {"id":"99999999-0000-0000-0000-000000000003","name":"Computer Science","code":"CS","isActive":true,"institutionType":0}]
                """
        };
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync("/Portal/Departments");
        Assert.Contains("<code>CS</code>", html);
        Assert.Contains("<span class=\"badge bg-info\">School</span>", html);
        Assert.Contains("<span class=\"badge bg-warning text-dark\">College</span>", html);
        Assert.Contains("<span class=\"badge bg-primary\">University</span>", html); // the switch's default branch
    }

    private static List<(string Value, bool Selected)> Options(string html, string selectName)
    {
        var select = Regex.Match(html, $@"<select[^>]*name=""{selectName}""[^>]*>(.*?)</select>", RegexOptions.Singleline);
        Assert.True(select.Success, $"no select named {selectName}");
        return Regex.Matches(select.Groups[1].Value, @"<option value=""([^""]*)""([^>]*)>")
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value.Contains("selected"))).ToList();
    }

    private static string Token(string html) => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

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

    /// <summary>A SuperAdmin's API; the capability matrix reports the institution types the portal shows.</summary>
    private sealed class FakeApi : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        public bool School { get; init; }
        public bool College { get; init; }
        public bool University { get; init; }
        public string DepartmentsJson { get; init; } = "[]";
        private readonly List<(string Path, string Query)> _gets = new();

        public IEnumerable<string> Queries(string path) => _gets.Where(g => g.Path == path).Select(g => g.Query);

        private static string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = "SuperAdmin", ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get) lock (_gets) _gets.Add((path, request.RequestUri.Query));

            if (path == "/api/v1/auth/login")
                return Task.FromResult(Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role = "SuperAdmin", userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                })));
            if (path == "/api/v1/portal-capabilities/matrix")
                return Task.FromResult(Json(JsonSerializer.Serialize(new { includeSchool = School, includeCollege = College, includeUniversity = University, rows = Array.Empty<object>() })));
            // The configured policy allows every type; what is shown comes from the matrix.
            if (path == "/api/v1/institution-policy")
                return Task.FromResult(Json("""{"includeSchool":true,"includeCollege":true,"includeUniversity":true}"""));
            if (path == "/api/v1/result-calculation")
            {
                var type = request.RequestUri.Query.Contains("institutionType=1") ? 1 : request.RequestUri.Query.Contains("institutionType=2") ? 2 : 0;
                return Task.FromResult(Json($$"""{"institutionType":{{type}},"gpaScaleRules":[],"componentRules":[]}"""));
            }
            if (path == "/api/v1/department") return Task.FromResult(Json(DepartmentsJson));
            if (path == "/api/v1/admin-user") return Task.FromResult(Json("[]"));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Task.FromResult(Json("""[{"id":"00000000-0000-0000-0000-0000000000c3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]"""));

            return Task.FromResult(path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                || path.Contains("tenant", StringComparison.OrdinalIgnoreCase) || path.Contains("campus", StringComparison.OrdinalIgnoreCase)
                || path.Contains("department", StringComparison.OrdinalIgnoreCase) || path.Contains("semester", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}"));
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
