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

    // A course in parts (like the Golf Explained "one file per SCO" samples): a group with a lesson and notes, then a quiz.
    private const string MultiPartManifest = """
        <?xml version="1.0" encoding="UTF-8"?>
        <manifest identifier="com.example.golf" version="1" xmlns="http://www.imsproject.org/xsd/imscp_rootv1p1p2" xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
          <metadata><schema>ADL SCORM</schema><schemaversion>1.2</schemaversion></metadata>
          <organizations default="o"><organization identifier="o"><title>Golf explained</title>
            <item identifier="unit"><title>Playing the game</title>
              <item identifier="lesson" identifierref="r1" parameters="?page=1"><title>How to play</title></item>
              <item identifier="notes" identifierref="r2"><title>Notes</title></item>
            </item>
            <item identifier="quiz" identifierref="r3"><title>Quiz</title><adlcp:masteryscore>80</adlcp:masteryscore></item>
          </organization></organizations>
          <resources>
            <resource identifier="r1" type="webcontent" adlcp:scormtype="sco" href="lesson.html"/>
            <resource identifier="r2" type="webcontent" adlcp:scormtype="asset" href="notes.html"/>
            <resource identifier="r3" type="webcontent" adlcp:scormtype="sco" href="quiz.html"/>
          </resources>
        </manifest>
        """;

    [Fact]
    public async Task MultiPartPackage_OpensEachPart_SavesEachPartsData_AndRollsThePartsUp()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherUserId);
        var student = Client("Student", seed.StudentUserId, seed.StudentProfileId);
        var player = _factory.CreateClient();
        var zip = Zip(("imsmanifest.xml", MultiPartManifest), ("lesson.html", "Lesson"), ("notes.html", "Notes"), ("quiz.html", "Quiz"));

        var package = await (await teacher.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(zip))).Content.ReadFromJsonAsync<JsonElement>();
        var packageId = package.GetProperty("id").GetGuid();
        var assignmentId = package.GetProperty("assignmentId").GetGuid();
        Assert.Equal((3, 2), (package.GetProperty("partCount").GetInt32(), package.GetProperty("scoCount").GetInt32()));

        var launch = await LaunchAsync(student, packageId);
        var token = launch.GetProperty("token").GetString()!;
        var parts = launch.GetProperty("parts").EnumerateArray().ToList();
        Assert.Equal(["How to play", "Notes", "Quiz"], parts.Select(p => p.GetProperty("title").GetString()));
        Assert.All(parts, p => Assert.Equal("not attempted", p.GetProperty("status").GetString()));

        // Without a part the player starts at the first part not done yet.
        var first = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}");
        Assert.Equal(("lesson", "lesson.html?page=1", true), (first.GetProperty("partId").GetString(), first.GetProperty("launchPath").GetString(), first.GetProperty("isSco").GetBoolean()));

        // A part's commit goes to that part; a commit that names no part (or an unknown one) is refused.
        var progress = Cmi(new { core = new { lesson_status = "incomplete", lesson_location = "p3", exit = "suspend" }, suspend_data = "lesson=3" });
        Assert.True((await (await player.PostAsync($"api/v1/scorm/play/{token}/commit/lesson", progress)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("result").GetBoolean());
        foreach (var wrong in new[] { "commit", "commit/nope" })
        {
            var refused = await (await player.PostAsync($"api/v1/scorm/play/{token}/{wrong}", Cmi(new { core = new { lesson_status = "passed" } }))).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((false, 101), (refused.GetProperty("result").GetBoolean(), refused.GetProperty("errorCode").GetInt32()));
        }

        // Opening the notes (an asset: no runtime) counts as done; reopening the lesson resumes it.
        var notes = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}?part=notes");
        Assert.Equal(("notes.html", false), (notes.GetProperty("launchPath").GetString(), notes.GetProperty("isSco").GetBoolean()));
        var lesson = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}?part=lesson");
        var lessonCmi = JsonDocument.Parse(lesson.GetProperty("cmiJson").GetString()!).RootElement;
        Assert.Equal(("resume", "lesson=3"), (lessonCmi.GetProperty("core").GetProperty("entry").GetString(), lessonCmi.GetProperty("suspend_data").GetString()));
        // Each part starts from its own data: the quiz is new, with its own passing score.
        var quiz = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}?part=quiz");
        var quizCmi = JsonDocument.Parse(quiz.GetProperty("cmiJson").GetString()!).RootElement;
        Assert.Equal(("ab-initio", "80"), (quizCmi.GetProperty("core").GetProperty("entry").GetString(), quizCmi.GetProperty("student_data").GetProperty("mastery_score").GetString()));

        await player.PostAsync($"api/v1/scorm/play/{token}/commit/lesson?final=true", Cmi(new { core = new { lesson_status = "passed", score = new { raw = "90" }, session_time = "00:04:00" } }));
        var mid = (await student.GetFromJsonAsync<JsonElement>($"api/v1/scorm/packages/{packageId}/mine")).GetProperty("mine");
        Assert.Equal("incomplete", mid.GetProperty("status").GetString()); // the quiz is still open
        await player.PostAsync($"api/v1/scorm/play/{token}/commit/quiz?final=true", Cmi(new { core = new { lesson_status = "completed", score = new { raw = "70" }, session_time = "00:06:00" } }));

        // Every part done: failed (the quiz needs 80), the score the average of the parts, the time their sum.
        var statuses = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}/parts");
        Assert.Equal(["passed", "completed", "failed"], statuses.EnumerateArray().Select(p => p.GetProperty("status").GetString()));
        var summary = (await student.GetFromJsonAsync<JsonElement>($"api/v1/scorm/packages/{packageId}/mine")).GetProperty("mine");
        Assert.Equal(("failed", 80m), (summary.GetProperty("status").GetString(), summary.GetProperty("bestScorePercent").GetDecimal()));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var attempt = await db.ScormAttempts.AsNoTracking().SingleAsync(a => a.PackageId == packageId);
            Assert.Equal(600, attempt.TotalSeconds);
            Assert.NotNull(attempt.FinishedAt);
            Assert.Equal(16m, (await db.AssignmentSubmissions.AsNoTracking().SingleAsync(s => s.AssignmentId == assignmentId && s.StudentProfileId == seed.StudentProfileId)).MarksAwarded);
        }

        // The finished attempt reopens in review at the first part; staff preview lists the parts too.
        var review = await LaunchAsync(student, packageId);
        Assert.Equal("review", review.GetProperty("mode").GetString());
        Assert.Equal("lesson", (await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{review.GetProperty("token").GetString()}")).GetProperty("partId").GetString());
        var preview = await (await teacher.PostAsJsonAsync($"api/v1/scorm/packages/{packageId}/preview", new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, preview.GetProperty("parts").GetArrayLength());
    }

    // SCORM 2004 sequencing: B opens only once A is satisfied (a shared objective), like the "forced sequential" examples.
    private const string SequencedManifest = """
        <?xml version="1.0" encoding="UTF-8"?>
        <manifest identifier="com.example.forced" version="1" xmlns="http://www.imsglobal.org/xsd/imscp_v1p1" xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_v1p3"
                  xmlns:imsss="http://www.imsglobal.org/xsd/imsss" xmlns:adlseq="http://www.adlnet.org/xsd/adlseq_v1p3">
          <metadata><schema>ADL SCORM</schema><schemaversion>2004 3rd Edition</schemaversion></metadata>
          <organizations default="o"><organization identifier="o" adlseq:objectivesGlobalToSystem="false"><title>Forced order</title>
            <item identifier="a" identifierref="ra"><title>A</title>
              <imsss:sequencing><imsss:objectives><imsss:primaryObjective objectiveID="a_sat"><imsss:mapInfo targetObjectiveID="g.a" writeSatisfiedStatus="true"/></imsss:primaryObjective></imsss:objectives>
                <imsss:deliveryControls completionSetByContent="true" objectiveSetByContent="true"/></imsss:sequencing>
            </item>
            <item identifier="b" identifierref="rb"><title>B</title>
              <imsss:sequencing>
                <imsss:sequencingRules><imsss:preConditionRule><imsss:ruleConditions conditionCombination="any">
                  <imsss:ruleCondition referencedObjective="prev" operator="not" condition="satisfied"/><imsss:ruleCondition referencedObjective="prev" operator="not" condition="objectiveStatusKnown"/>
                </imsss:ruleConditions><imsss:ruleAction action="disabled"/></imsss:preConditionRule></imsss:sequencingRules>
                <imsss:objectives><imsss:primaryObjective objectiveID="b_sat"/><imsss:objective objectiveID="prev"><imsss:mapInfo targetObjectiveID="g.a" readSatisfiedStatus="true"/></imsss:objective></imsss:objectives>
                <imsss:deliveryControls completionSetByContent="true" objectiveSetByContent="true"/>
              </imsss:sequencing>
            </item>
            <imsss:sequencing><imsss:controlMode choice="true" flow="true"/></imsss:sequencing>
          </organization></organizations>
          <resources>
            <resource identifier="ra" type="webcontent" adlcp:scormType="sco" href="a.html"/>
            <resource identifier="rb" type="webcontent" adlcp:scormType="sco" href="b.html"/>
          </resources>
        </manifest>
        """;

    private static StringContent Cmi2004(string completion, string success, decimal? scaled = null)
        => new(JsonSerializer.Serialize(new { cmi = new { completion_status = completion, success_status = success, score = new { scaled }, exit = "" } }), Encoding.UTF8, "application/json");

    [Fact]
    public async Task SequencedPackage_DecidesWhatOpens_IgnoresStaleCommits_AndOlderPackagesAreReadAgain()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherUserId);
        var student = Client("Student", seed.StudentUserId, seed.StudentProfileId);
        var player = _factory.CreateClient();
        var zip = Zip(("imsmanifest.xml", SequencedManifest), ("a.html", "A"), ("b.html", "B"));

        var package = await (await teacher.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(zip))).Content.ReadFromJsonAsync<JsonElement>();
        var packageId = package.GetProperty("id").GetGuid();
        Assert.True(package.GetProperty("sequenced").GetBoolean());

        var launch = await LaunchAsync(student, packageId);
        Assert.True(launch.GetProperty("sequenced").GetBoolean());
        var b = launch.GetProperty("parts").EnumerateArray().Single(p => p.GetProperty("id").GetString() == "b");
        Assert.False(b.GetProperty("available").GetBoolean());
        Assert.Equal(("\"{0}\" is not available yet.", "B"), (b.GetProperty("reason").GetString(), b.GetProperty("reasonArg").GetString()));
        var token = launch.GetProperty("token").GetString()!;

        async Task<JsonElement> Navigate(string request, string? target = null)
            => await (await player.PostAsJsonAsync($"api/v1/scorm/play/{token}/navigate", new { request, target })).Content.ReadFromJsonAsync<JsonElement>();
        static string? Part(JsonElement e) => e.TryGetProperty("partId", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

        // The package decides: A opens, and asking for B (by address or by choice) does not open it yet.
        var play = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}?part=b");
        Assert.Equal(("a", 1, true), (play.GetProperty("partId").GetString(), play.GetProperty("partAttempt").GetInt32(), play.GetProperty("sequenced").GetBoolean()));
        var refused = await Navigate("choice", "b");
        Assert.Null(Part(refused));
        Assert.Equal("B", refused.GetProperty("messageArg").GetString());

        await player.PostAsync($"api/v1/scorm/play/{token}/commit/a/1?final=true", Cmi2004("completed", "passed", 0.8m));
        Assert.Equal("b", Part(await Navigate("continue")));
        // Back to A: a new attempt on it; a late commit from A's first attempt is ignored.
        var back = await Navigate("previous");
        Assert.Equal("a", Part(back));
        Assert.Equal("not attempted", back.GetProperty("parts").EnumerateArray().Single(p => p.GetProperty("id").GetString() == "a").GetProperty("status").GetString());
        await player.PostAsync($"api/v1/scorm/play/{token}/commit/a/1?final=true", Cmi2004("completed", "failed", 0.1m));
        Assert.Equal("not attempted", (await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}/parts")).EnumerateArray()
            .Single(p => p.GetProperty("id").GetString() == "a").GetProperty("status").GetString());
        await player.PostAsync($"api/v1/scorm/play/{token}/commit/a/2?final=true", Cmi2004("completed", "passed", 0.9m));
        Assert.Equal("b", Part(await Navigate("continue")));
        // B opened again after going back: this is its second attempt.
        await player.PostAsync($"api/v1/scorm/play/{token}/commit/b/2?final=true", Cmi2004("completed", "passed", 0.7m));
        var end = await Navigate("continue");
        Assert.True(end.GetProperty("ended").GetBoolean());
        Assert.Equal("You have reached the end of this package.", end.GetProperty("message").GetString());

        var mine = (await student.GetFromJsonAsync<JsonElement>($"api/v1/scorm/packages/{packageId}/mine")).GetProperty("mine");
        Assert.Equal(("passed", 80m), (mine.GetProperty("status").GetString(), mine.GetProperty("bestScorePercent").GetDecimal())); // measure: (0.9 + 0.7) / 2
        var ended = await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}");
        Assert.Equal(("", true), (ended.GetProperty("launchPath").GetString(), ended.GetProperty("ended").GetBoolean()));

        // A package uploaded under the older rules (no sequencing, one part) with an attempt in progress is read again.
        var old = await (await teacher.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(zip, title: "Older upload"))).Content.ReadFromJsonAsync<JsonElement>();
        var oldId = old.GetProperty("id").GetGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE scorm_packages SET SequencingJson = NULL, PartsJson = NULL, StructureVersion = 0 WHERE Id = {oldId}");
        }
        var oldLaunch = await LaunchAsync(student, oldId);
        Assert.False(oldLaunch.GetProperty("sequenced").GetBoolean());
        var oldToken = oldLaunch.GetProperty("token").GetString()!;
        await player.PostAsync($"api/v1/scorm/play/{oldToken}/commit", Cmi2004("completed", "passed", 1m));
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<Tabsan.EduSphere.Application.Lms.Scorm.ScormService>();
            Assert.True(await service.UpgradeStructuresAsync() >= 1);
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var upgraded = await db.ScormPackages.AsNoTracking().SingleAsync(p => p.Id == oldId);
            Assert.True(upgraded.IsSequenced);
            Assert.Equal(ScormPackage.CurrentStructureVersion, upgraded.StructureVersion);
            Assert.Contains("\"a\"", (await db.ScormAttempts.AsNoTracking().SingleAsync(a => a.PackageId == oldId)).PartsStateJson);
        }
        // The attempt's earlier progress counts: A is passed, so B can be chosen straight away.
        var relaunch = await LaunchAsync(student, oldId);
        var relaunchToken = relaunch.GetProperty("token").GetString()!;
        Assert.True(relaunch.GetProperty("parts").EnumerateArray().Single(p => p.GetProperty("id").GetString() == "b").GetProperty("available").GetBoolean());
        Assert.Equal("b", Part(await (await player.PostAsJsonAsync($"api/v1/scorm/play/{relaunchToken}/navigate", new { request = "choice", target = "b" }))
            .Content.ReadFromJsonAsync<JsonElement>()));
    }

    [Fact]
    public async Task SequencedPackage_NextPartOpening_AndThePreviousPartsLastCommit_ArriveTogether_AndBothSucceed()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherUserId);
        var student = Client("Student", seed.StudentUserId, seed.StudentProfileId);
        var player = _factory.CreateClient();
        var zip = Zip(("imsmanifest.xml", SequencedManifest), ("a.html", "A"), ("b.html", "B"));
        var package = await (await teacher.PostAsync($"api/v1/scorm/offerings/{seed.OfferingId}/packages", Upload(zip))).Content.ReadFromJsonAsync<JsonElement>();
        var token = (await LaunchAsync(student, package.GetProperty("id").GetGuid())).GetProperty("token").GetString()!;
        await player.GetAsync($"api/v1/scorm/play/{token}");
        await player.PostAsync($"api/v1/scorm/play/{token}/commit/a/1", Cmi2004("completed", "passed", 0.8m));
        await player.PostAsJsonAsync($"api/v1/scorm/play/{token}/navigate", new { request = "continue" });

        // Next replaced A with B: A's frame sends its last commit while B's frame opens (and starts committing).
        for (var round = 0; round < 6; round++)
        {
            var responses = await Task.WhenAll(
                player.GetAsync($"api/v1/scorm/play/{token}"),
                player.PostAsync($"api/v1/scorm/play/{token}/commit/a/1?final=true", Cmi2004("completed", "passed", 0.8m)),
                player.GetAsync($"api/v1/scorm/play/{token}"),
                player.PostAsync($"api/v1/scorm/play/{token}/commit/b/1", Cmi2004("incomplete", "unknown")));
            Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
            Assert.Equal("b", (await responses[0].Content.ReadFromJsonAsync<JsonElement>()).GetProperty("partId").GetString());
        }
        var parts = (await player.GetFromJsonAsync<JsonElement>($"api/v1/scorm/play/{token}/parts")).EnumerateArray().ToList();
        Assert.Equal(("passed", "incomplete"), (parts.Single(p => p.GetProperty("id").GetString() == "a").GetProperty("status").GetString(),
            parts.Single(p => p.GetProperty("id").GetString() == "b").GetProperty("status").GetString()));
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
