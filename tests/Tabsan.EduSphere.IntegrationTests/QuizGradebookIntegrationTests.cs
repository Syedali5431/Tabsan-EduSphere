using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M2 acceptance: a submitted and graded quiz shows in the gradebook for that student.
/// Runs through the real API and database: pending essays post nothing, grading and regrade
/// update the "Quiz" component (best attempt, scaled to its weight), and a hand-entered cell is
/// never overwritten.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class QuizGradebookIntegrationTests
{
    private const decimal QuizWeight = 20m;
    private readonly EduSphereWebFactory _factory;

    public QuizGradebookIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task SubmittedAndGradedQuiz_ShowsInGradebook_WithoutOverwritingHandEntries()
    {
        var restoreRules = await UseCollegeComponentsAsync(("Quiz", QuizWeight), ("Final", 80m));
        try
        {
            var (offeringId, studentId, otherStudentId) = await SeedCollegeOfferingAsync();
            using var faculty = Client("Faculty");

            // Quiz: one multiple-choice (2) + one essay (4) = 6 marks, two attempts.
            var quizId = await CreateQuizAsync(faculty, offeringId);
            var mcId = await AddQuestionAsync(faculty, quizId, new
            {
                quizId, text = "2 + 2 = ?", type = "MultipleChoice", marks = 2, orderIndex = 1,
                options = new[] { new { text = "4", isCorrect = true, orderIndex = 1 }, new { text = "5", isCorrect = false, orderIndex = 2 } }
            });
            await AddQuestionAsync(faculty, quizId, new { quizId, text = "Explain addition.", type = "Essay", marks = 4, orderIndex = 2 });
            (await faculty.PostAsync($"api/v1/quiz/{quizId}/publish", null)).EnsureSuccessStatusCode();
            var options = (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}")).GetProperty("questions")
                .EnumerateArray().Single(q => q.GetProperty("questionId").GetGuid() == mcId).GetProperty("options").EnumerateArray().ToList();
            var right = options.Single(o => o.GetProperty("isCorrect").GetBoolean()).GetProperty("optionId").GetGuid();
            var wrong = options.Single(o => !o.GetProperty("isCorrect").GetBoolean()).GetProperty("optionId").GetGuid();

            // Attempt 1: wrong MC + essay → essay pending → nothing posted (no partial score).
            using var student = Client("Student", studentId);
            var attempt1 = await TakeQuizAsync(student, quizId, mcId, wrong);
            Assert.Null((await QuizCellAsync(faculty, offeringId, studentId)).Marks);

            // Essay graded 3/4 → attempt 3/6 → 50% of 20 = 10.
            await GradeEssayAsync(faculty, quizId, attempt1, 3);
            var cell = await QuizCellAsync(faculty, offeringId, studentId);
            Assert.Equal(10m, cell.Marks);
            Assert.Equal(QuizWeight, cell.Max);
            Assert.Equal("QuizSync", cell.Source);
            Assert.False(cell.Published);

            // Regrade the MC with full credit → 5/6 → 16.67.
            var regrade = await faculty.PostAsJsonAsync($"api/v1/quiz/{quizId}/questions/{mcId}/regrade", new { mode = "FullCredit" });
            Assert.Equal(HttpStatusCode.OK, regrade.StatusCode);
            Assert.Equal(16.67m, (await QuizCellAsync(faculty, offeringId, studentId)).Marks);

            // Attempt 2 (right MC, essay pending) does not disturb the posted best score until graded.
            var attempt2 = await TakeQuizAsync(student, quizId, mcId, right);
            Assert.Equal(16.67m, (await QuizCellAsync(faculty, offeringId, studentId)).Marks);
            await GradeEssayAsync(faculty, quizId, attempt2, 4);
            Assert.Equal(20m, (await QuizCellAsync(faculty, offeringId, studentId)).Marks);

            // The Total aggregate follows the synced component.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var total = await db.Results.AsNoTracking().SingleAsync(r => r.StudentProfileId == studentId && r.CourseOfferingId == offeringId && r.ResultType == "Total");
                Assert.Equal(20m, total.MarksObtained);
                Assert.Equal(QuizWeight, total.MaxMarks);
            }

            // A teacher's hand-entered cell is never overwritten by quiz scores.
            var entry = await faculty.PutAsJsonAsync($"api/v1/gradebook/{offeringId}/entry", new
            {
                studentProfileId = otherStudentId, courseOfferingId = offeringId, componentName = "Quiz", marksObtained = 3m, maxMarks = 20m
            });
            Assert.True(entry.IsSuccessStatusCode, await entry.Content.ReadAsStringAsync());
            using var other = Client("Student", otherStudentId);
            var otherAttempt = await TakeQuizAsync(other, quizId, mcId, right);
            await GradeEssayAsync(faculty, quizId, otherAttempt, 4);
            var manual = await QuizCellAsync(faculty, offeringId, otherStudentId);
            Assert.Equal(3m, manual.Marks);
            Assert.Equal("Manual", manual.Source);

            // ...and a teacher edit of a synced cell takes it over: later grading leaves it alone.
            (await faculty.PutAsJsonAsync($"api/v1/gradebook/{offeringId}/entry", new
            {
                studentProfileId = studentId, courseOfferingId = offeringId, componentName = "Quiz", marksObtained = 18m, maxMarks = 20m
            })).EnsureSuccessStatusCode();
            await faculty.PostAsJsonAsync($"api/v1/quiz/{quizId}/questions/{mcId}/regrade", new { mode = "FullCredit" });
            var taken = await QuizCellAsync(faculty, offeringId, studentId);
            Assert.Equal(18m, taken.Marks);
            Assert.Equal("Manual", taken.Source);
        }
        finally
        {
            await restoreRules();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed record Cell(decimal? Marks, decimal? Max, string? Source, bool Published);

    private static async Task<Cell> QuizCellAsync(HttpClient faculty, Guid offeringId, Guid studentId)
    {
        var grid = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/gradebook/{offeringId}");
        var row = grid.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("studentProfileId").GetGuid() == studentId);
        var cell = row.GetProperty("cells").EnumerateArray().Single(c => c.GetProperty("componentName").GetString() == "Quiz");
        decimal? Num(string name) => cell.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;
        return new Cell(Num("marksObtained"), Num("maxMarks"),
            cell.TryGetProperty("source", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null,
            cell.GetProperty("isPublished").GetBoolean());
    }

    private static async Task<Guid> TakeQuizAsync(HttpClient student, Guid quizId, Guid mcId, Guid optionId)
    {
        var start = await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { accessCode = (string?)null });
        var startBody = await start.Content.ReadAsStringAsync();
        Assert.True(start.IsSuccessStatusCode, startBody);
        var attemptId = JsonDocument.Parse(startBody).RootElement.GetProperty("attemptId").GetGuid();

        var paper = await student.GetFromJsonAsync<JsonElement>($"api/v1/quiz/attempt/{attemptId}/paper");
        var essayId = paper.GetProperty("questions").EnumerateArray()
            .Single(q => q.GetProperty("type").GetString() == "Essay").GetProperty("questionId").GetGuid();
        var answers = new object[]
        {
            new { quizQuestionId = mcId, selectedOptionIds = new[] { optionId } },
            new { quizQuestionId = essayId, textResponse = "You count on." }
        };
        var submit = await student.PostAsJsonAsync("api/v1/quiz/attempt/submit", new { attemptId, answers });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        return attemptId;
    }

    private static async Task GradeEssayAsync(HttpClient faculty, Guid quizId, Guid attemptId, decimal marks)
    {
        var queue = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}/grading-queue");
        var answerId = queue.EnumerateArray().Single(q => q.GetProperty("attemptId").GetGuid() == attemptId).GetProperty("answerId").GetGuid();
        var grade = await faculty.PostAsJsonAsync("api/v1/quiz/attempt/grade-answer", new { answerId, marksAwarded = marks, feedback = "ok" });
        Assert.Equal(HttpStatusCode.NoContent, grade.StatusCode);
    }

    private static async Task<Guid> CreateQuizAsync(HttpClient faculty, Guid offeringId)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/quiz", new
        {
            courseOfferingId = offeringId, title = "Gradebook quiz " + Guid.NewGuid().ToString("N")[..5], instructions = "",
            timeLimitMinutes = (int?)null, maxAttempts = 2, availableFrom = (DateTime?)null, availableUntil = (DateTime?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("quizId").GetGuid();
    }

    private static async Task<Guid> AddQuestionAsync(HttpClient faculty, Guid quizId, object body)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/quiz/question", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("questionId").GetGuid();
    }

    private HttpClient Client(string role, Guid? studentProfileId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, Guid.NewGuid().ToString(), studentProfileId: studentProfileId?.ToString()));
        return client;
    }

    /// <summary>College offering with two enrolled students (gradebook rows come from enrolments).</summary>
    private async Task<(Guid OfferingId, Guid StudentId, Guid OtherStudentId)> SeedCollegeOfferingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");

        var department = new Department($"QGB Dept {suffix}", $"QGB{suffix}", InstitutionType.College);
        var program = new AcademicProgram($"QGB Program {suffix}", $"QGBP{suffix}", department.Id, 4);
        var semester = new Semester($"QGB Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"QGB Course {suffix}", $"QGBC{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40);
        offering.SetInstitutionScope(InstitutionType.College, null, null);
        db.AddRange(department, program, semester, course, offering);

        var profiles = new List<StudentProfile>();
        foreach (var tag in new[] { "a", "b" })
        {
            var user = new User($"qgb_{tag}_{suffix}", "integration-hash", studentRole.Id, $"qgb_{tag}_{suffix}@tabsan.local");
            var profile = new StudentProfile(user.Id, $"QGB-{tag}-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile, new Enrollment(profile.Id, offering.Id));
            profiles.Add(profile);
        }

        await db.SaveChangesAsync();
        return (offering.Id, profiles[0].Id, profiles[1].Id);
    }

    /// <summary>
    /// Result components are global per institution type, so the test swaps in its own College
    /// set and puts the previous one back afterwards.
    /// </summary>
    private async Task<Func<Task>> UseCollegeComponentsAsync(params (string Name, decimal Weight)[] components)
    {
        List<(string Name, decimal Weight, int Order, bool Active)> previous;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existing = await db.ResultComponentRules.Where(r => r.InstitutionType == InstitutionType.College).ToListAsync();
            previous = existing.Select(r => (r.Name, r.Weightage, r.DisplayOrder, r.IsActive)).ToList();
            db.ResultComponentRules.RemoveRange(existing);
            db.ResultComponentRules.AddRange(components.Select((c, i) =>
                new ResultComponentRule(c.Name, c.Weight, i + 1, true, InstitutionType.College)));
            await db.SaveChangesAsync();
        }

        return async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ResultComponentRules.RemoveRange(await db.ResultComponentRules.Where(r => r.InstitutionType == InstitutionType.College).ToListAsync());
            db.ResultComponentRules.AddRange(previous.Select(p => new ResultComponentRule(p.Name, p.Weight, p.Order, p.Active, InstitutionType.College)));
            await db.SaveChangesAsync();
        };
    }
}
