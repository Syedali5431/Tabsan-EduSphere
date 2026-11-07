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

/// <summary>Parent Links (2026-10-30) against a fake API: find a parent, see their children, link, pause, remove; errors; access; Urdu.</summary>
public class ParentLinksWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string ParentId = "aa000000-0000-0000-0000-0000000000b1";
    private const string LinkedStudentId = "aa000000-0000-0000-0000-0000000000c1";
    private const string OtherStudentId = "aa000000-0000-0000-0000-0000000000c2";
    private const string OutsideStudentId = "aa000000-0000-0000-0000-0000000000c3";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public ParentLinksWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Admin_FindsAParent_LinksPausesAndRemovesChildren()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);

        var list = await client.GetStringAsync("/Portal/ParentLinks?parentSearch=imran");
        Assert.Contains("Imran Sheikh", list);
        Assert.Contains("/api/v1/parent-portal/parents?search=imran&take=25", api.Reads);
        Assert.Contains("Choose a parent from the list", list);

        var page = await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}&parentSearch=imran&studentSearch=Sheikh");
        Assert.Contains("Children of Imran Sheikh", page);
        Assert.Contains("TMS-GUL-09-01", page);
        Assert.Matches("<option value=\"Mother\" selected=\"selected\">Mother</option>", page);
        Assert.Contains("Already linked to this parent", page); // Fatima: already a child of this parent
        Assert.Contains("Hassan Sheikh", page);
        Assert.Contains("/api/v1/parent-portal/students?search=Sheikh&take=25", api.Reads);

        var linked = await client.PostAsync("/Portal/LinkParentStudent", Form(Token(page),
            ("parentId", ParentId), ("studentProfileId", OtherStudentId), ("relationship", " Father "), ("parentSearch", "imran")));
        Assert.Equal(HttpStatusCode.Redirect, linked.StatusCode);
        Assert.Contains($"parentId={ParentId}", linked.Headers.Location!.OriginalString);
        using (var doc = JsonDocument.Parse(api.Body("PUT", "/api/v1/parent-portal/links")))
        {
            Assert.Equal(ParentId, doc.RootElement.GetProperty("parentUserId").GetString());
            Assert.Equal(OtherStudentId, doc.RootElement.GetProperty("studentProfileId").GetString());
            Assert.Equal("Father", doc.RootElement.GetProperty("relationship").GetString());
            Assert.True(doc.RootElement.GetProperty("isActive").GetBoolean());
        }
        var after = await client.GetStringAsync(linked.Headers.Location!.OriginalString);
        Assert.Contains("Student linked.", after);

        await client.PostAsync("/Portal/UpdateParentLink", Form(Token(after),
            ("parentId", ParentId), ("studentProfileId", LinkedStudentId), ("relationship", "Mother"), ("active", "false"), ("change", "pause")));
        using (var doc = JsonDocument.Parse(api.Body("PUT", "/api/v1/parent-portal/links")))
        {
            Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
            Assert.Equal("Mother", doc.RootElement.GetProperty("relationship").GetString());
        }
        var paused = await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}");
        Assert.Contains("Link paused.", paused);

        await client.PostAsync("/Portal/UpdateParentLink", Form(Token(paused),
            ("parentId", ParentId), ("studentProfileId", LinkedStudentId), ("relationship", ""), ("active", "true"), ("change", "relationship")));
        using (var doc = JsonDocument.Parse(api.Body("PUT", "/api/v1/parent-portal/links")))
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("relationship").ValueKind); // "Not specified"
        Assert.Contains("Relationship saved.", await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}"));

        var removePage = await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}");
        await client.PostAsync("/Portal/RemoveParentLink", Form(Token(removePage), ("parentId", ParentId), ("studentProfileId", LinkedStudentId)));
        Assert.Contains($"DELETE /api/v1/parent-portal/links/{ParentId}/{LinkedStudentId}?remove=true", api.Writes);
        Assert.Contains("Link removed.", await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}"));
    }

    [Fact]
    public async Task ApiRefusals_AreShown_WithTheDepartmentMessageFor403()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);
        var page = await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}");

        await client.PostAsync("/Portal/LinkParentStudent", Form(Token(page), ("parentId", ParentId), ("studentProfileId", OutsideStudentId), ("relationship", "Father")));
        Assert.Contains("You can only manage links of students in your assigned departments.", await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}"));

        await client.PostAsync("/Portal/LinkParentStudent", Form(Token(page), ("parentId", ParentId), ("studentProfileId", OtherStudentId), ("relationship", new string('x', 61))));
        Assert.Contains("Relationship must be 60 characters or fewer.", await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}"));

        var unknown = await client.GetStringAsync($"/Portal/ParentLinks?parentId={Guid.NewGuid()}");
        Assert.Contains("Parent account not found.", unknown);
        Assert.Contains("Choose a parent from the list", unknown);
    }

    [Fact]
    public async Task UrduPage_IsTranslated()
    {
        var api = new FakeApi("SuperAdmin");
        using var client = await SignedInClientAsync(api);
        var page = await client.GetStringAsync($"/Portal/ParentLinks?parentId={ParentId}&lang=ur");
        Assert.Contains("والدین کے روابط", page);
        Assert.Contains("رشتہ", page);
        Assert.Contains("والدہ", page); // the saved relationship, Mother
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("Faculty")]
    [InlineData("Parent")]
    public async Task OtherRoles_CannotOpenThePage(string role)
    {
        var api = new FakeApi(role);
        using var client = await SignedInClientAsync(api);
        var response = await client.GetAsync("/Portal/ParentLinks");
        Assert.DoesNotContain("1. Find the parent", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(api.Reads, r => r.StartsWith("/api/v1/parent-portal/parents"));
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private static string Token(string html) => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Prepend(new("__RequestVerificationToken", token)));

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
        public readonly List<string> Reads = new();

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

        private const string Parent = $$"""{"userId":"{{ParentId}}","username":"parent.imran","fullName":"Imran Sheikh","email":"imran@example.test","phoneNumber":null,"isActive":true,"linkedStudents":1}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var pathAndQuery = request.RequestUri.PathAndQuery;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, pathAndQuery, body));
            else Reads.Add(pathAndQuery);

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000e1","key":"parent_links","name":"Parent Links","purpose":"","parentId":null,"displayOrder":10,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000e2","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/parent-portal/parents") return Json("[" + Parent + "]");
            if (path == $"/api/v1/parent-portal/parents/{ParentId}") return Json(Parent);
            if (path.StartsWith("/api/v1/parent-portal/parents/") && path.EndsWith("/links"))
                return Json($$"""[{"parentUserId":"{{ParentId}}","studentProfileId":"{{LinkedStudentId}}","registrationNumber":"TMS-GUL-09-01","studentName":"Fatima Sheikh","departmentName":"Gulberg Campus","programName":"Secondary","classNumber":9,"relationship":"Mother","isActive":true,"linkedAt":"2026-10-30T10:00:00Z"}]""");
            if (path.StartsWith("/api/v1/parent-portal/parents/"))
                return Json("""{"message":"Parent account not found."}""", HttpStatusCode.NotFound);
            if (path == "/api/v1/parent-portal/students")
                return Json($$"""[{"studentProfileId":"{{LinkedStudentId}}","registrationNumber":"TMS-GUL-09-01","name":"Fatima Sheikh","departmentName":"Gulberg Campus","programName":"Secondary","classNumber":9,"status":"Active","linkedParents":1},{"studentProfileId":"{{OtherStudentId}}","registrationNumber":"TMS-GUL-04-02","name":"Hassan Sheikh","departmentName":"Gulberg Campus","programName":"Primary","classNumber":4,"status":"Active","linkedParents":0}]""");
            if (path == "/api/v1/parent-portal/links" && request.Method == HttpMethod.Put)
            {
                if (body.Contains(OutsideStudentId)) return new HttpResponseMessage(HttpStatusCode.Forbidden);
                if (body.Contains(new string('x', 61))) return Json("""{"message":"Relationship must be 60 characters or fewer."}""", HttpStatusCode.BadRequest);
                return Json(body);
            }
            if (path.StartsWith("/api/v1/parent-portal/links/") && request.Method == HttpMethod.Delete) return new HttpResponseMessage(HttpStatusCode.NoContent);
            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]") : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
