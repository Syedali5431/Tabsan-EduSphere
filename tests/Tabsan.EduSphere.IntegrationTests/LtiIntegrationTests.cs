using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tabsan.EduSphere.Application.Notifications;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M5 — EduSphere as an LTI 1.3 platform, exercised end to end with an in-process tool (its own RSA key):
/// registration, resource-link launch (OIDC third-party login, signed id_token checked against the platform JWKS),
/// replay protection, client-credentials tokens, AGS line items / scores / results into EduSphere grades, NRPS,
/// Deep Linking 2.0, and the module licence gate.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class LtiIntegrationTests
{
    private const string Platform = "https://platform.test";
    private readonly EduSphereWebFactory _factory;

    public LtiIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Launch_Ags_Nrps_EndToEnd()
    {
        await ActivateModuleAsync();
        var seed = await SeedAsync();
        var tool = new FakeTool();
        var admin = Client("SuperAdmin", Guid.NewGuid());
        var (toolId, clientId, deploymentId) = await RegisterAsync(admin, tool);

        var faculty = Client("Faculty", seed.TeacherId);
        var link = await faculty.PostAsJsonAsync($"api/v1/lti/offerings/{seed.OfferingId}/links",
            new { toolId, title = "Week 3 practice", presentationTarget = "window", gradeMaximum = 10m });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var linkBody = await link.Content.ReadFromJsonAsync<JsonElement>();
        var linkId = linkBody.GetProperty("id").GetGuid();
        var assignmentId = linkBody.GetProperty("assignmentId").GetGuid();

        // Not enrolled → no launch.
        Assert.Equal(HttpStatusCode.Forbidden, (await StudentClient(seed.OutsiderUserId, seed.OutsiderProfileId).PostAsync($"api/v1/lti/links/{linkId}/launch", null)).StatusCode);

        // Launch: platform → tool login initiation.
        var student = StudentClient(seed.StudentUserId, seed.StudentProfileId);
        var init = await (await student.PostAsync($"api/v1/lti/links/{linkId}/launch?returnUrl=/Portal/CourseLms", null)).Content.ReadFromJsonAsync<JsonElement>();
        var p = init.GetProperty("parameters");
        Assert.Equal("https://tool.test/login", init.GetProperty("url").GetString());
        Assert.Equal(Platform, p.GetProperty("iss").GetString());
        Assert.Equal(clientId, p.GetProperty("client_id").GetString());
        Assert.Equal(deploymentId, p.GetProperty("lti_deployment_id").GetString());
        var loginHint = p.GetProperty("login_hint").GetString()!;

        // Tool → platform authorization (wrong redirect / wrong user / missing nonce are refused).
        Assert.Equal(HttpStatusCode.BadRequest, (await AuthorizeAsync(student, clientId, loginHint, "https://evil.test/cb", "n1")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AuthorizeAsync(faculty, clientId, loginHint, "https://tool.test/launch", "n2")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AuthorizeAsync(student, clientId, loginHint, "https://tool.test/launch", "")).StatusCode);
        var auth = await AuthorizeAsync(_factory.CreateClient(), clientId, loginHint, "https://tool.test/launch", "nonce-" + Guid.NewGuid());
        var authBody = await auth.Content.ReadAsStringAsync();
        Assert.True(auth.IsSuccessStatusCode, authBody);
        var authJson = JsonDocument.Parse(authBody).RootElement;
        Assert.Equal("state-1", authJson.GetProperty("state").GetString());

        // The id_token verifies against the platform JWKS and carries the LTI claims.
        var claims = await ValidatePlatformTokenAsync(authJson.GetProperty("idToken").GetString()!, clientId);
        Assert.Equal("LtiResourceLinkRequest", claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/message_type").GetString());
        Assert.Equal("1.3.0", claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/version").GetString());
        Assert.Equal(deploymentId, claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/deployment_id").GetString());
        Assert.Equal(seed.StudentUserId.ToString(), claims.GetProperty("sub").GetString());
        Assert.Contains("http://purl.imsglobal.org/vocab/lis/v2/membership#Learner",
            claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/roles").EnumerateArray().Select(r => r.GetString()));
        Assert.Equal(seed.OfferingId.ToString(), claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/context").GetProperty("id").GetString());
        Assert.Equal(linkId.ToString(), claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/resource_link").GetProperty("id").GetString());
        Assert.Equal("LTI Student", claims.GetProperty("name").GetString());
        Assert.Equal(Platform + "/Portal/CourseLms", claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/launch_presentation").GetProperty("return_url").GetString());
        var ags = claims.GetProperty("https://purl.imsglobal.org/spec/lti-ags/claim/endpoint");
        var lineItemUrl = ags.GetProperty("lineitem").GetString()!;
        Assert.StartsWith($"{Platform}/lti/ags/{seed.OfferingId}/lineitems/", lineItemUrl);
        Assert.Equal($"{Platform}/lti/nrps/{seed.OfferingId}/memberships",
            claims.GetProperty("https://purl.imsglobal.org/spec/lti-nrps/claim/namesroleservice").GetProperty("context_memberships_url").GetString());

        // The launch is single use.
        Assert.Equal(HttpStatusCode.BadRequest, (await AuthorizeAsync(_factory.CreateClient(), clientId, loginHint, "https://tool.test/launch", "nonce-" + Guid.NewGuid())).StatusCode);

        // Client-credentials token (assertion replay refused; scopes limited to what the tool may use).
        var assertion = tool.ClientAssertion(clientId, Platform + "/lti/token");
        var token = await TokenAsync(assertion, "https://purl.imsglobal.org/spec/lti-ags/scope/lineitem https://purl.imsglobal.org/spec/lti-ags/scope/score https://purl.imsglobal.org/spec/lti-ags/scope/result.readonly https://purl.imsglobal.org/spec/lti-nrps/scope/contextmembership.readonly https://example.test/other");
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);
        var tokenJson = await token.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain("example.test", tokenJson.GetProperty("scope").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await TokenAsync(assertion, "https://purl.imsglobal.org/spec/lti-ags/scope/score")).StatusCode);
        var service = ServiceClient(tokenJson.GetProperty("access_token").GetString()!);
        var roster = ServiceClient((await (await TokenAsync(tool.ClientAssertion(clientId, Platform + "/lti/token"), "https://purl.imsglobal.org/spec/lti-nrps/scope/contextmembership.readonly"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString()!);

        // AGS: line items, scores into EduSphere grades, results.
        var path = $"api/v1/lti/ags/{seed.OfferingId}/lineitems";
        var items = await service.GetAsync(path);
        Assert.Equal("application/vnd.ims.lis.v2.lineitemcontainer+json", items.Content.Headers.ContentType!.MediaType);
        Assert.Contains(lineItemUrl, await items.Content.ReadAsStringAsync());
        var lineItemPath = new Uri(lineItemUrl).AbsolutePath.Replace("/lti/", "api/v1/lti/").TrimStart('/');

        Assert.Equal(HttpStatusCode.Forbidden, (await roster.PostAsync(lineItemPath + "/scores", Score(seed.StudentUserId, 8, 10, DateTime.UtcNow))).StatusCode);
        Assert.Equal((HttpStatusCode)422, (await service.PostAsync(lineItemPath + "/scores", Score(seed.OutsiderUserId, 8, 10, DateTime.UtcNow))).StatusCode);
        var now = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.NoContent, (await service.PostAsync(lineItemPath + "/scores", Score(seed.StudentUserId, 16, 20, now, "Nice work"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await service.PostAsync(lineItemPath + "/scores", Score(seed.StudentUserId, 1, 20, now.AddMinutes(-5)))).StatusCode); // older: ignored

        var mySubmission = await (await student.GetAsync($"api/v1/assignment/{assignmentId}/student-view")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(8m, mySubmission.GetProperty("submission").GetProperty("marksAwarded").GetDecimal()); // 16/20 of 10 points
        Assert.Equal("Nice work", mySubmission.GetProperty("submission").GetProperty("feedback").GetString());

        var results = await (await service.GetAsync(lineItemPath + "/results")).Content.ReadFromJsonAsync<JsonElement>();
        var result = results.EnumerateArray().Single();
        Assert.Equal(seed.StudentUserId.ToString(), result.GetProperty("userId").GetString());
        Assert.Equal(8m, result.GetProperty("resultScore").GetDecimal());

        var created = await service.PostAsync(path, new StringContent("{\"label\":\"Quiz 2\",\"scoreMaximum\":20,\"tag\":\"quiz\"}", Encoding.UTF8, "application/vnd.ims.lis.v2.lineitem+json"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Contains("\"tag\":\"quiz\"", await created.Content.ReadAsStringAsync());
        var assignments = await faculty.GetStringAsync($"api/v1/assignment/by-offering/{seed.OfferingId}");
        Assert.Contains("Quiz 2", assignments);
        Assert.Contains("Week 3 practice", assignments);

        // NRPS with the tool's privacy level (Public → names and emails).
        var members = await roster.GetAsync($"api/v1/lti/nrps/{seed.OfferingId}/memberships");
        Assert.Equal("application/vnd.ims.lti-nrps.v2.membershipcontainer+json", members.Content.Headers.ContentType!.MediaType);
        var membership = await members.Content.ReadFromJsonAsync<JsonElement>();
        var studentMember = membership.GetProperty("members").EnumerateArray().Single(m => m.GetProperty("user_id").GetString() == seed.StudentUserId.ToString());
        Assert.StartsWith("lti.student.", studentMember.GetProperty("email").GetString());
        Assert.Contains(membership.GetProperty("members").EnumerateArray(),
            m => m.GetProperty("roles").EnumerateArray().Any(r => r.GetString()!.EndsWith("#Instructor")));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"api/v1/lti/nrps/{seed.OfferingId}/memberships")).StatusCode);
    }

    [Fact]
    public async Task DeepLinking_CreatesCourseItems_Once()
    {
        await ActivateModuleAsync();
        var seed = await SeedAsync();
        var tool = new FakeTool();
        var (toolId, clientId, deploymentId) = await RegisterAsync(Client("SuperAdmin", Guid.NewGuid()), tool);
        var faculty = Client("Faculty", seed.TeacherId);

        var start = await faculty.PostAsJsonAsync($"api/v1/lti/offerings/{seed.OfferingId}/deep-linking", new { toolId, returnUrl = "/Portal/LmsManage" });
        Assert.True(start.IsSuccessStatusCode, await start.Content.ReadAsStringAsync());
        var init = await start.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://tool.test/deeplink", init.GetProperty("parameters").GetProperty("target_link_uri").GetString());
        var hint = init.GetProperty("parameters").GetProperty("login_hint").GetString()!;

        var auth = await (await AuthorizeAsync(faculty, clientId, hint, "https://tool.test/launch", "dl-" + Guid.NewGuid())).Content.ReadFromJsonAsync<JsonElement>();
        var claims = await ValidatePlatformTokenAsync(auth.GetProperty("idToken").GetString()!, clientId);
        Assert.Equal("LtiDeepLinkingRequest", claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/message_type").GetString());
        Assert.Contains("#Instructor", claims.GetProperty("https://purl.imsglobal.org/spec/lti/claim/roles").GetRawText());
        var settings = claims.GetProperty("https://purl.imsglobal.org/spec/lti-dl/claim/deep_linking_settings");
        Assert.Equal(Platform + "/lti/deep-link-return", settings.GetProperty("deep_link_return_url").GetString());
        var data = settings.GetProperty("data").GetString()!;

        var response = tool.DeepLinkingResponse(clientId, deploymentId, data, Platform,
            """[{"type":"ltiResourceLink","title":"H5P: Photosynthesis","url":"https://tool.test/h5p/42","lineItem":{"scoreMaximum":5,"label":"H5P: Photosynthesis"}},{"type":"html","html":"<b>x</b>"}]""");
        Assert.Equal(HttpStatusCode.BadRequest, (await _factory.CreateClient().PostAsJsonAsync("api/v1/lti/deep-link-return",
            new { jwt = new FakeTool().DeepLinkingResponse(clientId, deploymentId, data, Platform, "[]") })).StatusCode); // wrong key
        var done = await _factory.CreateClient().PostAsJsonAsync("api/v1/lti/deep-link-return", new { jwt = response });
        var doneBody = await done.Content.ReadAsStringAsync();
        Assert.True(done.IsSuccessStatusCode, doneBody);
        var doneJson = JsonDocument.Parse(doneBody).RootElement;
        Assert.Equal(1, doneJson.GetProperty("created").GetInt32());
        Assert.Contains("skipped", doneJson.GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await _factory.CreateClient().PostAsJsonAsync("api/v1/lti/deep-link-return", new { jwt = response })).StatusCode); // replay

        var links = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/lti/offerings/{seed.OfferingId}/links");
        var item = links.EnumerateArray().Single(l => l.GetProperty("title").GetString() == "H5P: Photosynthesis");
        Assert.False(item.GetProperty("lineItemId").ValueKind == JsonValueKind.Null);
        // Students see it too.
        var studentLinks = await StudentClient(seed.StudentUserId, seed.StudentProfileId).GetStringAsync($"api/v1/lti/offerings/{seed.OfferingId}/links");
        Assert.Contains("H5P: Photosynthesis", studentLinks);
    }

    [Fact]
    public async Task Registry_Validation_Access_AndModuleGate()
    {
        await ActivateModuleAsync();
        var admin = Client("SuperAdmin", Guid.NewGuid());
        var tool = new FakeTool();
        var bad = await admin.PostAsJsonAsync("api/v1/lti/tools", ToolBody(tool) with { OidcInitiationUrl = "http://tool.test/login" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/v1/lti/tools", ToolBody(tool) with { PublicKeyPem = "not a key" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Faculty", Guid.NewGuid()).GetAsync("api/v1/lti/tools")).StatusCode);

        // The stored key is never shown again: an edit with a blank key keeps it; a JWKS URL replaces it.
        var created = await (await admin.PostAsJsonAsync("api/v1/lti/tools", ToolBody(tool))).Content.ReadFromJsonAsync<JsonElement>();
        var toolId = created.GetProperty("id").GetString();
        var kept = await (await admin.PutAsJsonAsync($"api/v1/lti/tools/{toolId}", ToolBody(tool) with { Name = "Renamed", PublicKeyPem = null }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Renamed", kept.GetProperty("name").GetString());
        Assert.True(kept.GetProperty("hasPublicKey").GetBoolean());
        var replaced = await (await admin.PutAsJsonAsync($"api/v1/lti/tools/{toolId}", ToolBody(tool) with { PublicKeyPem = null, JwksUrl = "https://tool.test/jwks" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(replaced.GetProperty("hasPublicKey").GetBoolean());
        Assert.Equal("https://tool.test/jwks", replaced.GetProperty("jwksUrl").GetString());

        var platform = await admin.GetFromJsonAsync<JsonElement>("api/v1/lti/platform");
        Assert.True(platform.GetProperty("configured").GetBoolean());
        Assert.Equal(Platform + "/lti/jwks", platform.GetProperty("jwksUrl").GetString());
        var before = (await _factory.CreateClient().GetFromJsonAsync<JsonElement>("api/v1/lti/jwks")).GetProperty("keys").GetArrayLength();
        await admin.PostAsync("api/v1/lti/platform/rotate-keys", null);
        Assert.Equal(before + 1, (await _factory.CreateClient().GetFromJsonAsync<JsonElement>("api/v1/lti/jwks")).GetProperty("keys").GetArrayLength()); // old key kept

        Assert.True((await admin.PostAsync("api/v1/module/lti/deactivate", null)).IsSuccessStatusCode);
        try { Assert.Equal(HttpStatusCode.Forbidden, (await _factory.CreateClient().GetAsync("api/v1/lti/jwks")).StatusCode); }
        finally { await ActivateModuleAsync(); }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Fake tool and helpers
    // ═════════════════════════════════════════════════════════════════════════

    private sealed class FakeTool
    {
        private readonly RSA _rsa = RSA.Create(2048);
        public string PublicPem => _rsa.ExportSubjectPublicKeyInfoPem();

        private string Sign(object payload)
        {
            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(JsonSerializer.Serialize(payload), new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = "tool-key" }, SecurityAlgorithms.RsaSha256));
        }

        public string ClientAssertion(string clientId, string audience)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Sign(new Dictionary<string, object> { ["iss"] = clientId, ["sub"] = clientId, ["aud"] = audience, ["iat"] = now, ["exp"] = now + 300, ["jti"] = Guid.NewGuid().ToString() });
        }

        public string DeepLinkingResponse(string clientId, string deploymentId, string data, string audience, string contentItemsJson)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Sign(new Dictionary<string, object>
            {
                ["iss"] = clientId, ["aud"] = audience, ["iat"] = now, ["exp"] = now + 300, ["nonce"] = Guid.NewGuid().ToString(),
                ["https://purl.imsglobal.org/spec/lti/claim/message_type"] = "LtiDeepLinkingResponse",
                ["https://purl.imsglobal.org/spec/lti/claim/version"] = "1.3.0",
                ["https://purl.imsglobal.org/spec/lti/claim/deployment_id"] = deploymentId,
                ["https://purl.imsglobal.org/spec/lti-dl/claim/data"] = data,
                ["https://purl.imsglobal.org/spec/lti-dl/claim/content_items"] = JsonDocument.Parse(contentItemsJson).RootElement
            });
        }
    }

    private sealed record ToolRequest(string Name, string? Description, string OidcInitiationUrl, string TargetLinkUri, string? DeepLinkingUrl,
        string[] RedirectUris, string? JwksUrl, string? PublicKeyPem, string? CustomParameters, string PrivacyLevel, bool AllowGradeService, bool AllowRosterService);

    private static ToolRequest ToolBody(FakeTool tool) => new("Test tool " + Guid.NewGuid().ToString("N")[..5], "H5P-like", "https://tool.test/login",
        "https://tool.test/launch", "https://tool.test/deeplink", ["https://tool.test/launch"], null, tool.PublicPem, "course_mode=practice", "Public", true, true);

    private static async Task<(Guid ToolId, string ClientId, string DeploymentId)> RegisterAsync(HttpClient admin, FakeTool tool)
    {
        var response = await admin.PostAsJsonAsync("api/v1/lti/tools", ToolBody(tool));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        var json = JsonDocument.Parse(body).RootElement;
        return (json.GetProperty("id").GetGuid(), json.GetProperty("clientId").GetString()!, json.GetProperty("deployments")[0].GetProperty("deploymentId").GetString()!);
    }

    private static Task<HttpResponseMessage> AuthorizeAsync(HttpClient client, string clientId, string loginHint, string redirectUri, string nonce)
        => client.PostAsJsonAsync("api/v1/lti/authorize", new
        {
            scope = "openid", responseType = "id_token", responseMode = "form_post", prompt = "none", clientId, redirectUri,
            loginHint, ltiMessageHint = loginHint, state = "state-1", nonce = nonce.Length == 0 ? null : nonce
        });

    private Task<HttpResponseMessage> TokenAsync(string assertion, string scope)
        => _factory.CreateClient().PostAsync("api/v1/lti/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
            ["client_assertion"] = assertion,
            ["scope"] = scope
        }));

    private static StringContent Score(Guid userId, decimal given, decimal max, DateTime timestamp, string? comment = null)
        => new(JsonSerializer.Serialize(new
        {
            userId = userId.ToString(), scoreGiven = given, scoreMaximum = max, comment, timestamp = timestamp.ToString("o"),
            activityProgress = "Completed", gradingProgress = "FullyGraded"
        }), Encoding.UTF8, "application/vnd.ims.lis.v1.score+json");

    private async Task<JsonElement> ValidatePlatformTokenAsync(string idToken, string clientId)
    {
        var jwks = await _factory.CreateClient().GetStringAsync("api/v1/lti/jwks");
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuer = Platform,
            ValidAudience = clientId,
            IssuerSigningKeys = new JsonWebKeySet(jwks).GetSigningKeys(),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        });
        Assert.True(result.IsValid, result.Exception?.Message);
        return JsonDocument.Parse(Base64Url.Decode(idToken.Split('.')[1])).RootElement.Clone();
    }

    private HttpClient Client(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private HttpClient StudentClient(Guid userId, Guid profileId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("Student", userId.ToString(), studentProfileId: profileId.ToString()));
        return client;
    }

    private HttpClient ServiceClient(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task ActivateModuleAsync()
    {
        var response = await Client("SuperAdmin", Guid.NewGuid()).PostAsync("api/v1/module/lti/activate", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private sealed record Seed(Guid OfferingId, Guid TeacherId, Guid StudentUserId, Guid StudentProfileId, Guid OutsiderUserId, Guid OutsiderProfileId);

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = await db.Roles.FirstAsync(r => r.Name == "Student");
        var facultyRole = await db.Roles.FirstAsync(r => r.Name == "Faculty");

        var teacher = new User($"lti_t_{suffix}", "integration-hash", facultyRole.Id, $"lti_t_{suffix}@uni.test");
        teacher.UpdateFullName("LTI Teacher");
        var department = new Department($"LTI Dept {suffix}", $"LD{suffix}", InstitutionType.University);
        var program = new AcademicProgram($"LTI Program {suffix}", $"LP{suffix}", department.Id, 8);
        var semester = new Semester($"LTI Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"LTI Course {suffix}", $"LC{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
        db.AddRange(teacher, department, program, semester, course, offering);

        var studentUser = new User($"lti_s_{suffix}", "integration-hash", studentRole.Id, $"lti.student.{suffix}@uni.test");
        studentUser.UpdateFullName("LTI Student");
        var profile = new StudentProfile(studentUser.Id, $"LS-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var outsiderUser = new User($"lti_o_{suffix}", "integration-hash", studentRole.Id, $"lti_o_{suffix}@uni.test");
        var outsider = new StudentProfile(outsiderUser.Id, $"LO-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        db.AddRange(studentUser, profile, outsiderUser, outsider, new Enrollment(profile.Id, offering.Id));
        await db.SaveChangesAsync();
        return new Seed(offering.Id, teacher.Id, studentUser.Id, profile.Id, outsiderUser.Id, outsider.Id);
    }
}
