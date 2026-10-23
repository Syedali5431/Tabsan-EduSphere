using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M7 — module items, requirements, prerequisites, sequential progress and unlock dates through the real API:
/// what students see, the gate on quizzes/assignments, completion from real work, the teacher report and validation.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class ModuleProgressIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public ModuleProgressIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Release_Gate_Completion_AndTeacherReport()
    {
        var seed = await SeedAsync();
        var faculty = Client("Faculty");
        var a = Client("Student", seed.StudentA);

        // Week 1: syllabus page (view) + assignment (score ≥ 60%). Week 2: needs Week 1, sequential: quiz (submit) then a link (mark done).
        var week1 = await CreateModuleAsync(faculty, seed.OfferingId, "Week 1", 1);
        var week2 = await CreateModuleAsync(faculty, seed.OfferingId, "Week 2", 2);
        var week3 = await CreateModuleAsync(faculty, seed.OfferingId, "Week 3", 3);
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId);
        var quizId = await CreateQuizAsync(faculty, seed.OfferingId);

        var linkable = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/lms/offerings/{seed.OfferingId}/linkable");
        Assert.Contains(linkable.EnumerateArray(), l => l.GetProperty("type").GetString() == "Quiz" && l.GetProperty("id").GetGuid() == quizId);

        await AddItemAsync(faculty, week1, new { itemType = "Header", title = "Start here" });
        var syllabus = await AddItemAsync(faculty, week1, new { itemType = "Page", title = "Syllabus", body = "Read me.\nTwice.", requirementType = "MustView" });
        var essay = await AddItemAsync(faculty, week1, new { itemType = "Assignment", targetId = assignmentId, requirementType = "MustScoreAtLeast", minScorePercent = 60 });
        var quizItem = await AddItemAsync(faculty, week2, new { itemType = "Quiz", targetId = quizId, requirementType = "MustSubmit" });
        var reading = await AddItemAsync(faculty, week2, new { itemType = "ExternalUrl", title = "Reading", url = "https://example.org/reading", requirementType = "MustMarkDone" });
        await AddItemAsync(faculty, week3, new { itemType = "Page", title = "Later", body = "x" });
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PutAsJsonAsync($"api/v1/lms/modules/{week2}/release",
            new { requireSequentialProgress = true, completionMode = "All", prerequisiteModuleIds = new[] { week1 } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PutAsJsonAsync($"api/v1/lms/modules/{week3}/release",
            new { unlockAt = DateTime.UtcNow.AddDays(3), requireSequentialProgress = false })).StatusCode);

        // A loop is refused.
        var loop = await faculty.PutAsJsonAsync($"api/v1/lms/modules/{week1}/release", new { requireSequentialProgress = false, prerequisiteModuleIds = new[] { week2 } });
        Assert.Equal(HttpStatusCode.BadRequest, loop.StatusCode);

        // Student view: Week 2 waits for Week 1, Week 3 for its date.
        var view = await MyModulesAsync(a, seed.OfferingId);
        Assert.False(Mod(view, week1).GetProperty("locked").GetBoolean());
        Assert.Equal("Complete “Week 1” first", Mod(view, week2).GetProperty("lockReason").GetString());
        Assert.StartsWith("Opens", Mod(view, week3).GetProperty("lockReason").GetString());
        Assert.Equal("locked", ItemOf(view, quizItem).GetProperty("status").GetString());
        Assert.Equal("none", Mod(view, week1).GetProperty("items")[0].GetProperty("status").GetString()); // header

        // The quiz is gated; opening a locked item records nothing.
        var blocked = await a.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { accessCode = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Contains("Week 1", await blocked.Content.ReadAsStringAsync());
        var lockedOpen = await (await a.PostAsync($"api/v1/lms/module-items/{quizItem}/open", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(lockedOpen.GetProperty("locked").GetBoolean());

        // Viewing the syllabus completes it; the page body comes back.
        var opened = await (await a.PostAsync($"api/v1/lms/module-items/{syllabus}/open", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Read me.\nTwice.", opened.GetProperty("body").GetString());
        Assert.Equal("done", opened.GetProperty("status").GetString());

        // Assignment: submitted and graded 15/20 = 75% → Week 1 complete → Week 2 opens.
        var submitted = await a.PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = "Essay" });
        Assert.True(submitted.IsSuccessStatusCode, await submitted.Content.ReadAsStringAsync());
        var submissionId = (await submitted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("submissionId").GetGuid();
        Assert.Equal("todo", ItemOf(await MyModulesAsync(a, seed.OfferingId), essay).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = 15m, feedback = "ok" })).StatusCode);
        view = await MyModulesAsync(a, seed.OfferingId);
        Assert.True(Mod(view, week1).GetProperty("completed").GetBoolean());
        Assert.False(Mod(view, week2).GetProperty("locked").GetBoolean());
        Assert.Equal("locked", ItemOf(view, reading).GetProperty("status").GetString()); // sequential: the quiz first
        Assert.Equal("Complete “M7 quiz” first", ItemOf(view, reading).GetProperty("lockReason").GetString());

        // Quiz submitted → the reading opens → marked done → Week 2 complete.
        await TakeQuizAsync(a, faculty, quizId);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsync($"api/v1/lms/module-items/{syllabus}/done", null)).StatusCode); // viewed, not marked by hand
        var done = await a.PostAsync($"api/v1/lms/module-items/{reading}/done", null);
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal("done", (await done.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        view = await MyModulesAsync(a, seed.OfferingId);
        Assert.True(Mod(view, week2).GetProperty("completed").GetBoolean());
        Assert.Equal(100, view.GetProperty("coursePercent").GetInt32()); // Week 3's page has no requirement

        // Undo keeps it honest.
        Assert.Equal(HttpStatusCode.OK, (await a.DeleteAsync($"api/v1/lms/module-items/{reading}/done")).StatusCode);
        Assert.False(Mod(await MyModulesAsync(a, seed.OfferingId), week2).GetProperty("completed").GetBoolean());
        await a.PostAsync($"api/v1/lms/module-items/{reading}/done", null);

        // Teacher report: A finished both weeks, B nothing.
        var report = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/lms/offerings/{seed.OfferingId}/progress");
        var rowA = report.GetProperty("students").EnumerateArray().Single(s => s.GetProperty("studentProfileId").GetGuid() == seed.StudentA);
        var rowB = report.GetProperty("students").EnumerateArray().Single(s => s.GetProperty("studentProfileId").GetGuid() == seed.StudentB);
        Assert.Equal(100, rowA.GetProperty("coursePercent").GetInt32());
        Assert.Equal(0, rowB.GetProperty("coursePercent").GetInt32());
        var detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/lms/offerings/{seed.OfferingId}/progress/{seed.StudentA}");
        Assert.True(Mod(detail, week1).GetProperty("completed").GetBoolean());

        // Changing a requirement re-evaluates it: the syllabus now needs "mark as done".
        var structure = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/lms/offerings/{seed.OfferingId}/structure");
        Assert.Equal(3, structure.GetProperty("modules").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/lms/module-items/{syllabus}", new
        {
            title = "Syllabus", body = "Read me.", indentLevel = 1, requirementType = "MustMarkDone", isPublished = true
        })).StatusCode);
        Assert.Equal("todo", ItemOf(await MyModulesAsync(a, seed.OfferingId), syllabus).GetProperty("status").GetString());

        // Outsiders and other students' details are refused.
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", seed.Outsider).GetAsync($"api/v1/lms/offerings/{seed.OfferingId}/my-modules")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.GetAsync($"api/v1/lms/offerings/{seed.OfferingId}/progress")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await faculty.GetAsync($"api/v1/lms/offerings/{seed.OfferingId}/progress/{seed.Outsider}")).StatusCode);
    }

    [Fact]
    public async Task Items_Validation_Order_Videos_AndUnpublished()
    {
        var seed = await SeedAsync();
        var other = await SeedAsync();
        var faculty = Client("Faculty");
        var week1 = await CreateModuleAsync(faculty, seed.OfferingId, "Week 1", 1);
        var foreignAssignment = await CreateAssignmentAsync(faculty, other.OfferingId);

        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/lms/modules/{week1}/items", new { itemType = "Assignment", targetId = foreignAssignment })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/lms/modules/{week1}/items", new { itemType = "ExternalUrl", title = "x", url = "ftp://x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/lms/modules/{week1}/items", new { itemType = "Header", title = "H", requirementType = "MustView" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/lms/modules/{week1}/items", new { itemType = "Nope", title = "H" })).StatusCode);

        // Adding a video through the existing endpoint adds a video item; deleting it removes the item.
        var video = await faculty.PostAsJsonAsync("api/v1/lms/video", new { moduleId = week1, title = "Lecture 1", embedUrl = "https://video.example.org/embed/1" });
        Assert.True(video.IsSuccessStatusCode, await video.Content.ReadAsStringAsync());
        var videoId = (await video.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var page = await AddItemAsync(faculty, week1, new { itemType = "Page", title = "Notes", body = "n" });
        var hidden = await AddItemAsync(faculty, week1, new { itemType = "Page", title = "Draft", body = "d" });
        var structure = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/lms/offerings/{seed.OfferingId}/structure");
        var items = structure.GetProperty("modules")[0].GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("Video", items[0].GetProperty("itemType").GetString());
        Assert.Equal(videoId, items[0].GetProperty("targetId").GetGuid());

        // Reorder (must list every item once) and hide one item.
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/lms/modules/{week1}/items/order", new { itemIds = new[] { page } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsJsonAsync($"api/v1/lms/modules/{week1}/items/order",
            new { itemIds = new[] { page, items[0].GetProperty("id").GetGuid(), hidden } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/lms/module-items/{hidden}", new { title = "Draft", body = "d", indentLevel = 0, requirementType = "None", isPublished = false })).StatusCode);

        var student = Client("Student", seed.StudentA);
        var view = await MyModulesAsync(student, seed.OfferingId);
        var titles = Mod(view, week1).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).ToList();
        Assert.Equal(["Notes", "Lecture 1"], titles);
        var opened = await (await student.PostAsync($"api/v1/lms/module-items/{items[0].GetProperty("id").GetGuid()}/open", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://video.example.org/embed/1", opened.GetProperty("embedUrl").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await student.PostAsync($"api/v1/lms/module-items/{hidden}/open", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await faculty.DeleteAsync($"api/v1/lms/video/{videoId}")).StatusCode);
        structure = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/lms/offerings/{seed.OfferingId}/structure");
        Assert.DoesNotContain(structure.GetProperty("modules")[0].GetProperty("items").EnumerateArray(), i => i.GetProperty("itemType").GetString() == "Video");

        // Unpublished modules are invisible to students.
        var draft = await CreateModuleAsync(faculty, seed.OfferingId, "Draft week", 9, publish: false);
        Assert.DoesNotContain((await MyModulesAsync(student, seed.OfferingId)).GetProperty("modules").EnumerateArray(), m => m.GetProperty("id").GetGuid() == draft);
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.DeleteAsync($"api/v1/lms/module-items/{page}")).StatusCode);
    }

    // ── helpers ──

    private sealed record Seed(Guid OfferingId, Guid StudentA, Guid StudentB, Guid Outsider);

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var department = new Department($"M7 Dept {suffix}", $"M7D{suffix}", InstitutionType.University);
        var program = new AcademicProgram($"M7 Program {suffix}", $"M7P{suffix}", department.Id, 8);
        var semester = new Semester($"M7 Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"M7 Course {suffix}", $"M7C{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40);
        offering.SetInstitutionScope(InstitutionType.University, null, null);
        db.AddRange(department, program, semester, course, offering);
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var user = new User($"m7_{i}_{suffix}", "integration-hash", studentRole.Id, $"m7_{i}_{suffix}@tabsan.local");
            user.UpdateFullName($"M7 Student {i} {suffix}");
            var profile = new StudentProfile(user.Id, $"M7-{i}-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile);
            if (i < 2) db.Add(new Enrollment(profile.Id, offering.Id));
            ids.Add(profile.Id);
        }
        await db.SaveChangesAsync();
        return new Seed(offering.Id, ids[0], ids[1], ids[2]);
    }

    private HttpClient Client(string role, Guid? studentProfileId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, Guid.NewGuid().ToString(), studentProfileId: studentProfileId?.ToString()));
        return client;
    }

    private static JsonElement Mod(JsonElement view, Guid moduleId)
        => view.GetProperty("modules").EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == moduleId);

    private static JsonElement ItemOf(JsonElement view, Guid itemId)
        => view.GetProperty("modules").EnumerateArray().SelectMany(m => m.GetProperty("items").EnumerateArray()).Single(i => i.GetProperty("id").GetGuid() == itemId);

    private static async Task<JsonElement> MyModulesAsync(HttpClient student, Guid offeringId)
    {
        var response = await student.GetAsync($"api/v1/lms/offerings/{offeringId}/my-modules");
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, text);
        return JsonDocument.Parse(text).RootElement;
    }

    private static async Task<Guid> CreateModuleAsync(HttpClient faculty, Guid offeringId, string title, int week, bool publish = true)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/lms/module", new { offeringId, title, weekNumber = week, body = "Intro" });
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, text);
        var id = JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
        if (publish) Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsync($"api/v1/lms/module/{id}/publish", null)).StatusCode);
        return id;
    }

    private static async Task<Guid> AddItemAsync(HttpClient faculty, Guid moduleId, object body)
    {
        var response = await faculty.PostAsJsonAsync($"api/v1/lms/modules/{moduleId}/items", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateAssignmentAsync(HttpClient faculty, Guid offeringId)
    {
        var created = await faculty.PostAsJsonAsync("api/v1/assignment", new
        {
            courseOfferingId = offeringId, title = "M7 essay", description = "Write", dueDate = DateTime.UtcNow.AddDays(7), maxMarks = 20m
        });
        var body = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, body);
        var id = JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.True((await faculty.PostAsync($"api/v1/assignment/{id}/publish", null)).IsSuccessStatusCode);
        await faculty.PutAsJsonAsync($"api/v1/assignment/{id}/settings", new
        {
            allowedAttempts = 1, allowedFileTypes = (string[]?)null, maxFileSizeMb = 10, anonymousGrading = false, postingPolicy = "Automatic", moderatedGrading = false
        });
        return id;
    }

    private static async Task<Guid> CreateQuizAsync(HttpClient faculty, Guid offeringId)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/quiz", new
        {
            courseOfferingId = offeringId, title = "M7 quiz", instructions = "", timeLimitMinutes = (int?)null, maxAttempts = 1,
            availableFrom = (DateTime?)null, availableUntil = (DateTime?)null
        });
        response.EnsureSuccessStatusCode();
        var quizId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("quizId").GetGuid();
        var question = await faculty.PostAsJsonAsync("api/v1/quiz/question", new
        {
            quizId, text = "1 + 1 = ?", type = "MultipleChoice", marks = 1, orderIndex = 1,
            options = new[] { new { text = "2", isCorrect = true, orderIndex = 1 }, new { text = "3", isCorrect = false, orderIndex = 2 } }
        });
        Assert.True(question.IsSuccessStatusCode, await question.Content.ReadAsStringAsync());
        (await faculty.PostAsync($"api/v1/quiz/{quizId}/publish", null)).EnsureSuccessStatusCode();
        return quizId;
    }

    private static async Task TakeQuizAsync(HttpClient student, HttpClient faculty, Guid quizId)
    {
        var start = await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { accessCode = (string?)null });
        var startBody = await start.Content.ReadAsStringAsync();
        Assert.True(start.IsSuccessStatusCode, startBody);
        var attemptId = JsonDocument.Parse(startBody).RootElement.GetProperty("attemptId").GetGuid();
        var question = (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}")).GetProperty("questions")[0];
        var right = question.GetProperty("options").EnumerateArray().Single(o => o.GetProperty("isCorrect").GetBoolean()).GetProperty("optionId").GetGuid();
        var submit = await student.PostAsJsonAsync("api/v1/quiz/attempt/submit", new
        {
            attemptId, answers = new object[] { new { quizQuestionId = question.GetProperty("questionId").GetGuid(), selectedOptionIds = new[] { right } } }
        });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
    }
}
