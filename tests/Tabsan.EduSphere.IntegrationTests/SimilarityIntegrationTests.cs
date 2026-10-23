using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.Application.Similarity;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M9 — similarity checking through the real API and database: the built-in comparison (typed text and a .docx),
/// rechecks, reports, student visibility and scope; Copyleaks settings, submission and its webhook with a stand-in client.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class SimilarityIntegrationTests
{
    private const string Essay =
        "Sunlight reaches the atmosphere as a mixture of every colour. Molecules of nitrogen and oxygen scatter short wavelengths far more " +
        "strongly than long ones, a process known as Rayleigh scattering. Blue light is therefore sent in every direction across the sky, " +
        "while red and orange light travel on almost undisturbed. At sunset the light crosses much more air, so most of the blue is scattered " +
        "away before it reaches us and the sky glows red.";

    private const string Unrelated =
        "Photosynthesis lets plants turn carbon dioxide and water into glucose using the energy of light captured by chlorophyll in their " +
        "leaves, releasing oxygen that animals breathe. Without it there would be no food chains on land and far less oxygen in the air.";

    private readonly EduSphereWebFactory _factory;
    private Guid _tenantId;
    private Guid _campusId;

    public SimilarityIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task BuiltIn_FindsCopiedWork_InTextAndFiles_WithReportsAndStudentVisibility()
    {
        await ActivateModuleAsync(_factory);
        var seed = await SeedAsync(4);
        var faculty = Client(_factory, "Faculty");
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId);
        var options = await faculty.PutAsJsonAsync($"api/v1/similarity/assignments/{assignmentId}/settings", new { enabled = true, visibleToStudents = true });
        Assert.Equal(HttpStatusCode.OK, options.StatusCode);
        Assert.Equal("Internal", (await options.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("provider").GetString());

        var (a, b, c, d) = (seed.Students[0], seed.Students[1], seed.Students[2], seed.Students[3]);
        var subA = await SubmitTextAsync(Client(_factory, "Student", a), assignmentId, Essay);
        var subB = await SubmitTextAsync(Client(_factory, "Student", b), assignmentId, Essay.Replace("At sunset", "In the evening") + " I found this topic interesting.");
        var subC = await SubmitTextAsync(Client(_factory, "Student", c), assignmentId, Unrelated);
        var subD = await SubmitFileAsync(Client(_factory, "Student", d), assignmentId, "essay.docx", Docx(Essay));

        var run = await RunAsync(_factory);
        Assert.Equal(4, run.Queued);
        Assert.Equal(0, (await RunAsync(_factory)).Queued); // nothing new

        var scores = await ScoresAsync(faculty, assignmentId);
        Assert.Equal(0m, scores[subA].Score); // the first to hand in had nothing to compare with yet
        Assert.InRange(scores[subB].Score!.Value, 70m, 100m);
        Assert.Equal(0m, scores[subC].Score);
        Assert.Equal(100m, scores[subD].Score); // the .docx text was read

        // Checking again compares with everything handed in since.
        var recheck = await faculty.PostAsync($"api/v1/similarity/submissions/{subA}/recheck", null);
        Assert.Equal(HttpStatusCode.Accepted, recheck.StatusCode);
        Assert.Equal("Queued", (await recheck.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        await RunAsync(_factory);
        Assert.Equal(100m, (await ScoresAsync(faculty, assignmentId))[subA].Score);
        Assert.Equal(2, (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/similarity/submissions/{subA}/checks")).GetArrayLength());

        // The report names the sources and shows the matching passages.
        var report = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/similarity/checks/{scores[subB].CheckId}");
        Assert.Equal("Completed", report.GetProperty("status").GetString());
        var sources = report.GetProperty("sources").EnumerateArray().ToList();
        Assert.Contains(sources, s => s.GetProperty("label").GetString()!.StartsWith($"M9 Student 0 {seed.Suffix}"));
        Assert.DoesNotContain(sources, s => s.GetProperty("label").GetString()!.StartsWith($"M9 Student 2 {seed.Suffix}"));
        var passage = sources[0].GetProperty("passages")[0];
        Assert.StartsWith("Sunlight reaches", passage.GetProperty("text").GetString());
        Assert.StartsWith("Sunlight reaches", passage.GetProperty("sourceText").GetString());

        // Students: their own score only, while the assignment shows it; never the report.
        var studentB = Client(_factory, "Student", b);
        var mine = await studentB.GetFromJsonAsync<JsonElement>($"api/v1/similarity/my/submissions/{subB}");
        Assert.Equal(scores[subB].Score, mine.GetProperty("score").GetDecimal());
        Assert.Equal(HttpStatusCode.NotFound, (await Client(_factory, "Student", c).GetAsync($"api/v1/similarity/my/submissions/{subB}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await studentB.GetAsync($"api/v1/similarity/checks/{scores[subB].CheckId}")).StatusCode);
        await faculty.PutAsJsonAsync($"api/v1/similarity/assignments/{assignmentId}/settings", new { enabled = true, visibleToStudents = false });
        Assert.Equal(HttpStatusCode.NotFound, (await studentB.GetAsync($"api/v1/similarity/my/submissions/{subB}")).StatusCode);

        // Other institutions' staff are refused; unknown webhooks reveal nothing.
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(_factory, "Faculty", tenantId: Guid.NewGuid()).GetAsync($"api/v1/similarity/assignments/{assignmentId}/scores")).StatusCode);
        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsync($"api/v1/similarity/webhooks/copyleaks/completed?check={Guid.NewGuid()}&token=x",
            new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
    }

    [Fact]
    public async Task Copyleaks_Settings_Submission_AndWebhook()
    {
        // The shared host never calls Copyleaks: it records sign-ins and scans (and has Similarity:PublicApiBaseUrl set).
        var copyleaks = _factory.Services.GetRequiredService<SwitchableCopyleaksClient>();
        var factory = _factory;
        await ActivateModuleAsync(factory);
        var seed = await SeedAsync(2);
        var admin = Client(factory, "Admin");

        // Copyleaks needs an account email and key; the key is stored but never returned.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("api/v1/similarity/settings",
            new { provider = "Copyleaks", isEnabled = true, scope = "Course", accountEmail = "checker@uni.edu", apiKey = (string?)null, sandbox = true })).StatusCode);
        var saved = await admin.PutAsJsonAsync("api/v1/similarity/settings",
            new { provider = "Copyleaks", isEnabled = true, scope = "Course", accountEmail = "checker@uni.edu", apiKey = "secret-key", sandbox = true });
        var savedBody = await saved.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.DoesNotContain("secret-key", savedBody);
        Assert.True(JsonDocument.Parse(savedBody).RootElement.GetProperty("hasApiKey").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("api/v1/similarity/settings?installationDefault=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(factory, "Faculty").GetAsync("api/v1/similarity/settings")).StatusCode);

        var test = await admin.PostAsync("api/v1/similarity/settings/test", null);
        Assert.Equal(HttpStatusCode.OK, test.StatusCode);
        Assert.Equal(("checker@uni.edu", "secret-key"), copyleaks.Logins.Last());

        var faculty = Client(factory, "Faculty");
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId);
        await faculty.PutAsJsonAsync($"api/v1/similarity/assignments/{assignmentId}/settings", new { enabled = true, visibleToStudents = false });
        var first = await SubmitTextAsync(Client(factory, "Student", seed.Students[0]), assignmentId, Essay);
        await RunAsync(factory);

        var submitted = copyleaks.Scans.Single(s => s.Text.Contains(Essay));
        Assert.Equal(copyleaks.Scans.Last(), submitted);
        Assert.Equal(Essay, submitted.Text.Trim());
        Assert.True(submitted.Sandbox);
        Assert.StartsWith("https://api.example.test/api/v1/similarity/webhooks/copyleaks/{STATUS}?check=", submitted.Webhook);
        Assert.Equal("Submitted", (await ScoresAsync(faculty, assignmentId))[first].Status);

        // The webhook needs the check's own token.
        var path = submitted.Webhook.Replace("https://api.example.test/", "").Replace("{STATUS}", "completed");
        var completed = """
            {"scannedDocument":{"totalWords":80},"results":{"score":{"aggregatedScore":37.5},
             "internet":[{"title":"Why is the sky blue","url":"https://example.org/sky","matchedWords":30,"introduction":"Blue light is scattered"}]}}
            """;
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsync(path[..(path.IndexOf("token=", StringComparison.Ordinal) + 6)] + "wrong",
            new StringContent(completed, Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsync(path, new StringContent(completed, Encoding.UTF8, "application/json"))).StatusCode);

        var score = (await ScoresAsync(faculty, assignmentId))[first];
        Assert.Equal(("Completed", (decimal?)37.5m), (score.Status, score.Score));
        var report = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/similarity/checks/{score.CheckId}");
        var source = report.GetProperty("sources")[0];
        Assert.Equal(("Why is the sky blue", "https://example.org/sky"), (source.GetProperty("label").GetString(), source.GetProperty("url").GetString()));
        Assert.Equal("Copyleaks", report.GetProperty("provider").GetString());

        // An error webhook fails the check.
        var second = await SubmitTextAsync(Client(factory, "Student", seed.Students[1]), assignmentId, Unrelated);
        await RunAsync(factory);
        var errorPath = copyleaks.Scans.Last().Webhook.Replace("https://api.example.test/", "").Replace("{STATUS}", "error");
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsync(errorPath,
            new StringContent("""{"error":{"code":5,"message":"Not enough credits"}}""", Encoding.UTF8, "application/json"))).StatusCode);
        var failed = (await ScoresAsync(faculty, assignmentId))[second];
        Assert.Equal("Failed", failed.Status);
        Assert.Equal("Not enough credits", (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/similarity/checks/{failed.CheckId}")).GetProperty("error").GetString());
    }

    // ── helpers ──


    private sealed record Seed(Guid OfferingId, IReadOnlyList<Guid> Students, string Suffix);

    private async Task<Seed> SeedAsync(int enrolled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var tenant = new Tenant($"M9T{suffix}", $"M9 Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"M9K{suffix}", $"M9 Campus {suffix}");
        db.AddRange(tenant, campus);
        (_tenantId, _campusId) = (tenant.Id, campus.Id);
        var department = new Department($"M9 Dept {suffix}", $"M9D{suffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"M9 Program {suffix}", $"M9P{suffix}", department.Id, 8);
        var semester = new Semester($"M9 Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"M9 Course {suffix}", $"M9C{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40);
        offering.SetInstitutionScope(InstitutionType.University, tenant.Id, campus.Id);
        db.AddRange(department, program, semester, course, offering);
        var ids = new List<Guid>();
        for (var i = 0; i < enrolled; i++)
        {
            var user = new User($"m9_{i}_{suffix}", "integration-hash", studentRole.Id, $"m9_{i}_{suffix}@tabsan.local");
            user.UpdateFullName($"M9 Student {i} {suffix}");
            var profile = new StudentProfile(user.Id, $"M9-{i}-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile, new Enrollment(profile.Id, offering.Id));
            ids.Add(profile.Id);
        }
        await db.SaveChangesAsync();
        return new Seed(offering.Id, ids, suffix);
    }

    private HttpClient Client(WebApplicationFactory<Program> factory, string role, Guid? studentProfileId = null, Guid? tenantId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, Guid.NewGuid().ToString(), studentProfileId: studentProfileId?.ToString(),
                tenantId: (tenantId ?? _tenantId).ToString(), campusId: (tenantId is null ? _campusId : Guid.NewGuid()).ToString()));
        return client;
    }

    private static async Task ActivateModuleAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("SuperAdmin", Guid.NewGuid().ToString()));
        var response = await client.PostAsync("api/v1/module/similarity/activate", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<SimilarityRunResult> RunAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SimilarityService>().RunAsync(DateTime.UtcNow);
    }

    private static async Task<Dictionary<Guid, SimilarityScoreDto>> ScoresAsync(HttpClient faculty, Guid assignmentId)
        => (await faculty.GetFromJsonAsync<List<SimilarityScoreDto>>($"api/v1/similarity/assignments/{assignmentId}/scores"))!.ToDictionary(s => s.SubmissionId);

    private static async Task<Guid> CreateAssignmentAsync(HttpClient faculty, Guid offeringId)
    {
        var created = await faculty.PostAsJsonAsync("api/v1/assignment", new { courseOfferingId = offeringId, title = "M9 essay", description = "Write", dueDate = DateTime.UtcNow.AddDays(7), maxMarks = 20m });
        var body = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, body);
        var id = JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.True((await faculty.PostAsync($"api/v1/assignment/{id}/publish", null)).IsSuccessStatusCode);
        return id;
    }

    private static async Task<Guid> SubmitTextAsync(HttpClient student, Guid assignmentId, string text)
    {
        var response = await student.PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = text });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("submissionId").GetGuid();
    }

    private static async Task<Guid> SubmitFileAsync(HttpClient student, Guid assignmentId, string name, byte[] bytes)
    {
        var form = new MultipartFormDataContent();
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        form.Add(part, "files", name);
        var response = await student.PostAsync($"api/v1/assignment/{assignmentId}/submissions", form);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("submissionId").GetGuid();
    }

    private static byte[] Docx(string text)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
        }
        return stream.ToArray();
    }
}
