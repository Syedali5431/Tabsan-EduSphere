using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M13 — SCORM packages and the xAPI store through the real API and database: upload checks, the student's launch,
/// content behind the play link, commits (resume data, scores, time), the best score in the gradebook, module progress,
/// attempt limits and review, staff preview and the report; LRS credentials, statements, voiding and queries.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class ScormIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public ScormIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid TeacherUserId, Guid StudentUserId, Guid StudentProfileId, string RegistrationNumber, Guid OutsiderUserId,
        Guid OutsiderProfileId, Guid OfferingId, Guid ModuleId);

    private HttpClient Client(string role, Guid userId, Guid? studentProfileId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, userId.ToString(), studentProfileId: studentProfileId?.ToString()));
        return client;
    }

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var facultyRole = db.Roles.First(r => r.Name == "Faculty");
        var tenant = new Tenant($"SCT{suffix}", $"Scorm Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"SCK{suffix}", $"Scorm Campus {suffix}");
        var department = new Department($"Scorm Dept {suffix}", $"SCD{suffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"Scorm Program {suffix}", $"SCP{suffix}", department.Id, 8);
        var semester = new Semester($"Scorm Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(120));
        var course = new Course($"Scorm Course {suffix}", $"SC{suffix}", 3, department.Id);
        var teacher = new User($"sc_t_{suffix}", "integration-hash", facultyRole.Id, $"sc_t_{suffix}@tabsan.local");
        var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
        offering.SetInstitutionScope(InstitutionType.University, tenant.Id, campus.Id);
        var student = new User($"sc_s_{suffix}", "integration-hash", studentRole.Id, $"sc_s_{suffix}@tabsan.local");
        student.UpdateFullName($"Learner {suffix}");
        var registration = $"SC-1-{suffix}";
        var profile = new StudentProfile(student.Id, registration, program.Id, department.Id, DateTime.UtcNow.Date);
        var outsider = new User($"sc_o_{suffix}", "integration-hash", studentRole.Id, $"sc_o_{suffix}@tabsan.local");
        var outsiderProfile = new StudentProfile(outsider.Id, $"SC-2-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var module = new CourseContentModule(offering.Id, "Safety week", 1);
        module.Publish();
        db.AddRange(tenant, campus, department, program, semester, course, teacher, offering, student, profile, outsider, outsiderProfile,
            new Enrollment(profile.Id, offering.Id), module);
        await db.SaveChangesAsync();
        return new Seed(teacher.Id, student.Id, profile.Id, registration, outsider.Id, outsiderProfile.Id, offering.Id, module.Id);
    }

    private const string Manifest = """
        <?xml version="1.0" encoding="UTF-8"?>
        <manifest identifier="com.example.fire" version="1" xmlns="http://www.imsproject.org/xsd/imscp_rootv1p1p2" xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
          <metadata><schema>ADL SCORM</schema><schemaversion>1.2</schemaversion></metadata>
          <organizations default="o"><organization identifier="o"><title>Fire safety</title>
            <item identifier="i" identifierref="r"><title>Lesson</title><adlcp:masteryscore>80</adlcp:masteryscore></item>
          </organization></organizations>
          <resources><resource identifier="r" type="webcontent" adlcp:scormtype="sco" href="index.html"/></resources>
        </manifest>
        """;

    private static byte[] Zip(params (string Name, string Content)[] files)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, content) in files)
            {
                using var w = new StreamWriter(zip.CreateEntry(name).Open());
                w.Write(content);
            }
        return stream.ToArray();
    }

    private static byte[] Package() => Zip(("imsmanifest.xml", Manifest), ("index.html", "<html><body>Lesson</body></html>"), ("img/a b.png", "PNG"));

    private static MultipartFormDataContent Upload(byte[] zip, bool graded = true, int maxAttempts = 2, bool publish = true, string? title = null)
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(zip), "file", "fire.zip" } };
        form.Add(new StringContent(graded ? "true" : "false"), "graded");
        form.Add(new StringContent("20"), "maxMarks");
        form.Add(new StringContent(maxAttempts.ToString()), "maxAttempts");
        form.Add(new StringContent(publish ? "true" : "false"), "publish");
        if (title is not null) form.Add(new StringContent(title), "title");
        return form;
    }

    private static StringContent Cmi(object cmi) => new(JsonSerializer.Serialize(new { cmi }), Encoding.UTF8, "application/json");

    private async Task<JsonElement> LaunchAsync(HttpClient student, Guid packageId, bool newAttempt = false, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var response = await student.PostAsJsonAsync($"api/v1/scorm/packages/{packageId}/launch", new { newAttempt });
        Assert.Equal(expected, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Package_IsLaunched_Resumed_Scored_Graded_AndCountsForModuleProgress()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherUserId);
        var student = Client("Student", seed.StudentUserId, seed.StudentProfileId);
        var player = _factory.CreateClient(); // the play link needs no sign-in

        var uploaded = await teacher.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(Package()));
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var package = await uploaded.Content.ReadFromJsonAsync<JsonElement>();
        var packageId = package.GetProperty("id").GetGuid();
        var assignmentId = package.GetProperty("assignmentId").GetGuid();
        Assert.Equal("Fire safety", package.GetProperty("title").GetString());
        Assert.Equal("SCORM 1.2", package.GetProperty("version").GetString());
        Assert.Equal(80m, package.GetProperty("masteryScore").GetDecimal());
        Assert.Equal(3, package.GetProperty("fileCount").GetInt32());

        // A module item that needs 80% in the package.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = new ModuleItem(seed.ModuleId, seed.OfferingId, ModuleItemType.ScormPackage, packageId, "Fire safety", null, null, 1, seed.TeacherUserId);
            item.SetRequirement(ModuleRequirementType.MustScoreAtLeast, 80);
            db.Add(item);
            await db.SaveChangesAsync();
        }

        var mine = (await student.GetFromJsonAsync<JsonElement>($"api/v1/scorm/offerings/{seed.OfferingId}/my-packages")).EnumerateArray().Single();
        Assert.Equal(0, mine.GetProperty("mine").GetProperty("attemptsUsed").GetInt32());

        // First launch: a new attempt with the learner's details.
        var launch = await LaunchAsync(student, packageId);
        Assert.Equal((1, "normal"), (launch.GetProperty("attemptNumber").GetInt32(), launch.GetProperty("mode").GetString()));
        var token = launch.GetProperty("token").GetString()!;
        var play = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}");
        Assert.Equal("index.html", play.GetProperty("launchPath").GetString());
        var cmi = JsonDocument.Parse(play.GetProperty("cmiJson").GetString()!).RootElement;
        Assert.Equal(seed.RegistrationNumber, cmi.GetProperty("core").GetProperty("student_id").GetString());
        Assert.Equal("ab-initio", cmi.GetProperty("core").GetProperty("entry").GetString());
        Assert.Equal("80", cmi.GetProperty("student_data").GetProperty("mastery_score").GetString());

        // The package's files behind the play link.
        var page = await player.GetAsync($"api/v1/scorm/play/{token}/content/index.html");
        Assert.Equal("text/html", page.Content.Headers.ContentType!.MediaType);
        Assert.Contains("Lesson", await page.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await player.GetAsync($"api/v1/scorm/play/{token}/content/img/a%20b.png")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync($"api/v1/scorm/play/{token}/content/missing.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync($"api/v1/scorm/play/{token}/content/%2E%2E%2Fimsmanifest.xml")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync($"api/v1/scorm/play/not-a-token/content/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync($"api/v1/scorm/play/{token}x")).StatusCode);

        // Suspend half-way, then resume where the learner left off.
        var commit = await player.PostAsync($"api/v1/scorm/play/{token}/commit", Cmi(new
        {
            core = new { lesson_status = "incomplete", lesson_location = "p2", exit = "suspend", session_time = "00:05:00" },
            suspend_data = "page=2"
        }));
        Assert.True((await commit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("result").GetBoolean());
        var resumed = await LaunchAsync(student, packageId);
        Assert.Equal(1, resumed.GetProperty("attemptNumber").GetInt32());
        var resumeCmi = JsonDocument.Parse((await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{resumed.GetProperty("token").GetString()}"))
            .GetProperty("cmiJson").GetString()!).RootElement;
        Assert.Equal("resume", resumeCmi.GetProperty("core").GetProperty("entry").GetString());
        Assert.Equal("page=2", resumeCmi.GetProperty("suspend_data").GetString());
        Assert.Equal("p2", resumeCmi.GetProperty("core").GetProperty("lesson_location").GetString());

        // Finish with 85% (mastery 80: passed). The final commit (sendBeacon) arrives as text/plain.
        var final = new StringContent(JsonSerializer.Serialize(new { cmi = new { core = new { lesson_status = "completed", score = new { raw = "85", min = "0", max = "100" }, session_time = "00:10:00", exit = "" } } }),
            Encoding.UTF8, "text/plain");
        Assert.True((await (await player.PostAsync($"api/v1/scorm/play/{token}/commit?final=true", final)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("result").GetBoolean());

        var summary = (await student.GetFromJsonAsync<JsonElement>($"api/v1/scorm/packages/{packageId}/mine")).GetProperty("mine");
        Assert.Equal(("passed", 85m), (summary.GetProperty("status").GetString(), summary.GetProperty("bestScorePercent").GetDecimal()));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var graded = await db.AssignmentSubmissions.AsNoTracking().SingleAsync(s => s.AssignmentId == assignmentId && s.StudentProfileId == seed.StudentProfileId);
            Assert.Equal(17m, graded.MarksAwarded); // 85% of 20
            Assert.NotNull(graded.PostedAt);
            var attempt = await db.ScormAttempts.AsNoTracking().SingleAsync(a => a.PackageId == packageId);
            Assert.Equal(600, attempt.TotalSeconds); // only the final commit's session counts
        }
        var modules = await student.GetFromJsonAsync<JsonElement>($"api/v1/lms/offerings/{seed.OfferingId}/my-modules");
        var moduleItem = modules.GetProperty("modules").EnumerateArray().Single().GetProperty("items").EnumerateArray().Single();
        Assert.Equal(("ScormPackage", "done"), (moduleItem.GetProperty("itemType").GetString(), moduleItem.GetProperty("status").GetString()));

        // Reopening a finished package reviews it; a second attempt has to be asked for.
        var review = await LaunchAsync(student, packageId);
        Assert.Equal(("review", 1), (review.GetProperty("mode").GetString(), review.GetProperty("attemptNumber").GetInt32()));
        var second = await LaunchAsync(student, packageId, newAttempt: true);
        Assert.Equal(2, second.GetProperty("attemptNumber").GetInt32());
        await player.PostAsync($"api/v1/scorm/play/{second.GetProperty("token").GetString()}/commit?final=true", Cmi(new { core = new { lesson_status = "failed", score = new { raw = "40" } } }));
        await LaunchAsync(student, packageId, newAttempt: true, expected: HttpStatusCode.Conflict); // 2 attempts allowed

        // Review commits change nothing; the best score (85) stays in the gradebook.
        var lastReview = await LaunchAsync(student, packageId);
        Assert.Equal(("review", 2), (lastReview.GetProperty("mode").GetString(), lastReview.GetProperty("attemptNumber").GetInt32()));
        await player.PostAsync($"api/v1/scorm/play/{lastReview.GetProperty("token").GetString()}/commit", Cmi(new { core = new { lesson_status = "passed", score = new { raw = "100" } } }));
        var report = await teacher.GetFromJsonAsync<JsonElement>($"api/v1/scorm/packages/{packageId}/report");
        var row = report.GetProperty("learners").EnumerateArray().Single();
        Assert.Equal((2, 85m, 40m), (row.GetProperty("attempts").GetInt32(), row.GetProperty("bestScorePercent").GetDecimal(), row.GetProperty("lastScorePercent").GetDecimal()));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(17m, (await db.AssignmentSubmissions.AsNoTracking().SingleAsync(s => s.AssignmentId == assignmentId && s.StudentProfileId == seed.StudentProfileId)).MarksAwarded);
        }

        // Students do not upload work to the package's assignment.
        var handIn = new MultipartFormDataContent { { new StringContent("my work"), "textContent" } };
        Assert.Equal(HttpStatusCode.Conflict, (await student.PostAsync($"api/v1/assignment/{assignmentId}/submissions", handIn)).StatusCode);
    }

    [Fact]
    public async Task Packages_AreChecked_Scoped_Previewed_Hidden_AndRemoved()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherUserId);
        var student = Client("Student", seed.StudentUserId, seed.StudentProfileId);
        var outsider = Client("Student", seed.OutsiderUserId, seed.OutsiderProfileId);
        var player = _factory.CreateClient();

        async Task<string?> Rejected(byte[] zip)
        {
            var r = await teacher.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(zip));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString();
        }
        Assert.Contains("imsmanifest.xml is missing", await Rejected(Zip(("index.html", "x"))));
        Assert.Contains("not a zip", await Rejected(Encoding.UTF8.GetBytes("plain text, not a zip")));
        Assert.Contains("outside its folder", await Rejected(Zip(("imsmanifest.xml", Manifest), ("index.html", "x"), ("../evil.js", "x"))));
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(Package()))).StatusCode);

        var created = await (await teacher.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(Package(), graded: false, title: "Drill"))).Content.ReadFromJsonAsync<JsonElement>();
        var packageId = created.GetProperty("id").GetGuid();
        Assert.False(created.GetProperty("graded").GetBoolean());
        Assert.Equal("Drill", created.GetProperty("title").GetString());

        // Not enrolled: nothing to see or launch.
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"api/v1/scorm/offerings/{seed.OfferingId}/my-packages")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PostAsJsonAsync($"api/v1/scorm/packages/{packageId}/launch", new { newAttempt = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync($"api/v1/scorm/packages/{packageId}/preview", new { })).StatusCode);

        // Staff preview: plays, records nothing.
        var preview = await (await teacher.PostAsync($"api/v1/scorm/packages/{packageId}/preview", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("preview", preview.GetProperty("mode").GetString());
        var previewToken = preview.GetProperty("token").GetString()!;
        var previewPlay = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{previewToken}");
        Assert.Equal("review", previewPlay.GetProperty("mode").GetString());
        Assert.Equal(HttpStatusCode.OK, (await player.GetAsync($"api/v1/scorm/play/{previewToken}/content/index.html")).StatusCode);
        await player.PostAsync($"api/v1/scorm/play/{previewToken}/commit", Cmi(new { core = new { lesson_status = "passed" } }));
        using (var scope = _factory.Services.CreateScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ScormAttempts.AnyAsync(a => a.PackageId == packageId));

        // Hidden from students, then graded later (an assignment appears), then removed.
        var hidden = await teacher.PutAsJsonAsync($"api/v1/scorm/packages/{packageId}", new { title = "Drill 2", maxAttempts = 0, isPublished = false, graded = true, maxMarks = 25 });
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
        var updated = await hidden.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(updated.GetProperty("graded").GetBoolean());
        Assert.Empty((await student.GetFromJsonAsync<JsonElement>($"api/v1/scorm/offerings/{seed.OfferingId}/my-packages")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await student.PostAsJsonAsync($"api/v1/scorm/packages/{packageId}/launch", new { newAttempt = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await teacher.PutAsJsonAsync($"api/v1/scorm/packages/{packageId}",
            new { title = "Drill 2", maxAttempts = 0, isPublished = true, graded = false, maxMarks = 25 })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await teacher.DeleteAsync($"api/v1/scorm/packages/{packageId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync($"api/v1/scorm/play/{previewToken}/content/index.html")).StatusCode); // files deleted
        Assert.DoesNotContain((await teacher.GetFromJsonAsync<JsonElement>($"api/v1/scorm/offerings/{seed.OfferingId}/packages")).EnumerateArray(),
            p => p.GetProperty("id").GetGuid() == packageId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var assignment = await db.Assignments.IgnoreQueryFilters().AsNoTracking().SingleAsync(a => a.ScormPackageId == packageId);
            Assert.True(assignment.IsDeleted); // no grades yet, so it goes too
            Assert.Equal(("Drill 2", 25m), (assignment.Title, assignment.MaxMarks));
        }
    }

    [Fact]
    public async Task Lrs_AcceptsStatementsWithCredentials_AndAnswersQueries()
    {
        var admin = Client("Admin", Guid.NewGuid());
        var created = await (await admin.PostAsJsonAsync("api/v1/xapi/credentials", new { name = "Safety course" })).Content.ReadFromJsonAsync<JsonElement>();
        var key = created.GetProperty("credential").GetProperty("key").GetString()!;
        var secret = created.GetProperty("secret").GetString()!;
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Faculty", Guid.NewGuid()).PostAsJsonAsync("api/v1/xapi/credentials", new { name = "x" })).StatusCode);

        var lrs = _factory.CreateClient();
        lrs.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}")));
        var verb = "http://adlnet.gov/expapi/verbs/completed";
        var activity = $"https://edusphere.test/activities/{Guid.NewGuid():N}";
        string Statement(Guid? id = null, double scaled = 0.9) => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = id?.ToString(),
            ["actor"] = new { mbox = "mailto:learner@edusphere.test" },
            ["verb"] = new { id = verb },
            ["object"] = new { id = activity },
            ["result"] = new { score = new { scaled }, success = true }
        }.Where(p => p.Value is not null).ToDictionary(p => p.Key, p => p.Value));

        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().GetAsync("api/v1/xapi/about")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await lrs.PostAsync("api/v1/xapi/statements", new StringContent(Statement(), Encoding.UTF8, "application/json"))).StatusCode);
        lrs.DefaultRequestHeaders.Add("X-Experience-API-Version", "1.0.3");
        var wrong = _factory.CreateClient();
        wrong.DefaultRequestHeaders.Add("X-Experience-API-Version", "1.0.3");
        wrong.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:nope")));
        var refused = await wrong.PostAsync("api/v1/xapi/statements", new StringContent(Statement(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.Contains("Basic", refused.Headers.WwwAuthenticate.ToString());

        var posted = await lrs.PostAsync("api/v1/xapi/statements", new StringContent(Statement(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var id = Guid.Parse((await posted.Content.ReadFromJsonAsync<string[]>())!.Single());
        var one = await lrs.GetFromJsonAsync<JsonElement>($"api/v1/xapi/statements?statementId={id}");
        Assert.Equal(key, one.GetProperty("authority").GetProperty("account").GetProperty("name").GetString());
        Assert.True(one.TryGetProperty("stored", out _));

        var putId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await lrs.PutAsync($"api/v1/xapi/statements?statementId={putId}", new StringContent(Statement(putId), Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await lrs.PutAsync($"api/v1/xapi/statements?statementId={putId}", new StringContent(Statement(putId, 0.1), Encoding.UTF8, "application/json"))).StatusCode);

        var list = await lrs.GetFromJsonAsync<JsonElement>($"api/v1/xapi/statements?activity={Uri.EscapeDataString(activity)}&limit=1");
        Assert.Single(list.GetProperty("statements").EnumerateArray());
        Assert.StartsWith("/api/v1/xapi/statements?", list.GetProperty("more").GetString());
        var agent = Uri.EscapeDataString("{\"mbox\":\"mailto:LEARNER@edusphere.test\"}");
        Assert.Equal(2, (await lrs.GetFromJsonAsync<JsonElement>($"api/v1/xapi/statements?agent={agent}&activity={Uri.EscapeDataString(activity)}")).GetProperty("statements").GetArrayLength());

        // Administrators read with their sign-in; others cannot.
        admin.DefaultRequestHeaders.Add("X-Experience-API-Version", "1.0.3");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"api/v1/xapi/statements?statementId={id}")).StatusCode);
        var studentClient = Client("Student", Guid.NewGuid(), Guid.NewGuid());
        studentClient.DefaultRequestHeaders.Add("X-Experience-API-Version", "1.0.3");
        Assert.Equal(HttpStatusCode.Unauthorized, (await studentClient.GetAsync("api/v1/xapi/statements")).StatusCode);
        Assert.Contains((await admin.GetFromJsonAsync<JsonElement>("api/v1/xapi/recent?take=20")).EnumerateArray(), s => s.GetProperty("id").GetGuid() == id);

        // Voiding hides the statement from lists; switched-off credentials are refused.
        var voiding = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["actor"] = new { mbox = "mailto:admin@edusphere.test" },
            ["verb"] = new { id = "http://adlnet.gov/expapi/verbs/voided" },
            ["object"] = new { objectType = "StatementRef", id = id.ToString() }
        });
        Assert.Equal(HttpStatusCode.OK, (await lrs.PostAsync("api/v1/xapi/statements", new StringContent(voiding, Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await lrs.GetAsync($"api/v1/xapi/statements?statementId={id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await lrs.GetAsync($"api/v1/xapi/statements?voidedStatementId={id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/v1/xapi/credentials/{created.GetProperty("credential").GetProperty("id").GetGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await lrs.PostAsync("api/v1/xapi/statements", new StringContent(Statement(), Encoding.UTF8, "application/json"))).StatusCode);
    }
}
