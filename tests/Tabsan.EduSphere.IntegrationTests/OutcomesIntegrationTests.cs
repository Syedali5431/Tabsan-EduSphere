using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M6 — outcomes, CLO → PLO mapping, alignment to assignments, rubric criteria and quiz questions, evidence
/// from real grading, the mastery gradebook, the student's view, program attainment, exports and the Excel import.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class OutcomesIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public OutcomesIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Alignment_Grading_Mastery_StudentView_Attainment_EndToEnd()
    {
        await ActivateModuleAsync();
        var seed = await SeedAsync();
        var admin = Client("Admin");
        var faculty = Client("Faculty");

        // PLO (admin) and CLO (teacher) mapped to it; teachers cannot create program outcomes.
        var plo = await CreateOutcomeAsync(admin, new { level = "Program", programId = seed.ProgramId, code = "PLO1", title = "Solve computing problems" });
        Assert.Equal(HttpStatusCode.Forbidden, (await faculty.PostAsJsonAsync("api/v1/outcomes", new { level = "Program", programId = seed.ProgramId, code = "PLO9", title = "x" })).StatusCode);
        var clo = await CreateOutcomeAsync(faculty, new { level = "Course", courseId = seed.CourseId, code = "CLO1", title = "Write correct programs", calculationMethod = "Highest" });
        var mapped = await faculty.PutAsJsonAsync($"api/v1/outcomes/{clo}/mappings", new { programOutcomeIds = new[] { plo } });
        Assert.Equal(HttpStatusCode.OK, mapped.StatusCode);
        Assert.Equal(plo, (await mapped.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("programOutcomeIds")[0].GetGuid());

        // Assignment (20 marks, automatic posting) with a rubric; aligned: whole assignment → CLO1, "Argument" criterion → PLO1.
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, 20);
        await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/settings", new
        {
            allowedAttempts = 1, allowedFileTypes = (string[]?)null, maxFileSizeMb = 10, anonymousGrading = false, postingPolicy = "Automatic", moderatedGrading = false
        });
        var rubric = await faculty.PostAsJsonAsync("api/v1/rubric", new
        {
            assignmentId, title = "Rubric",
            criteria = new[]
            {
                new { name = "Argument", maxPoints = 6m, displayOrder = 1, levels = new[] { new { label = "Weak", pointsAwarded = 2m, displayOrder = 1 }, new { label = "Strong", pointsAwarded = 6m, displayOrder = 2 } } },
                new { name = "Style", maxPoints = 4m, displayOrder = 2, levels = new[] { new { label = "Poor", pointsAwarded = 1m, displayOrder = 1 }, new { label = "Clear", pointsAwarded = 4m, displayOrder = 2 } } }
            }
        });
        Assert.True(rubric.IsSuccessStatusCode, await rubric.Content.ReadAsStringAsync());

        var target = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/outcomes/alignments?targetType=Assignment&targetId={assignmentId}");
        var available = target.GetProperty("available").EnumerateArray().Select(o => o.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(clo, available);
        Assert.Contains(plo, available);
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync("api/v1/outcomes/alignments", new { targetType = "Assignment", targetId = assignmentId, outcomeIds = new[] { clo } })).StatusCode);

        var studentA = Client("Student", seed.StudentA);
        var submitted = await studentA.PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = "My answer" });
        Assert.True(submitted.IsSuccessStatusCode, await submitted.Content.ReadAsStringAsync());
        var submissionId = (await submitted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("submissionId").GetGuid();
        var detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}");
        var argument = detail.GetProperty("rubric").GetProperty("criteria").EnumerateArray().Single(c => c.GetProperty("name").GetString() == "Argument");
        var style = detail.GetProperty("rubric").GetProperty("criteria").EnumerateArray().Single(c => c.GetProperty("name").GetString() == "Style");
        Guid Level(JsonElement c, string label) => c.GetProperty("levels").EnumerateArray().Single(l => l.GetProperty("label").GetString() == label).GetProperty("levelId").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync("api/v1/outcomes/alignments", new
        {
            targetType = "RubricCriterion", targetId = argument.GetProperty("criterionId").GetGuid(), outcomeIds = new[] { plo }
        })).StatusCode);

        // Graded 16/20 = 80% → CLO1 3.2 of 4: mastered (mastery 3).
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = 16m, feedback = "Good" })).StatusCode);
        var cell = await CellAsync(faculty, seed.OfferingId, seed.StudentA, clo);
        Assert.Equal(3.2m, cell.GetProperty("score").GetDecimal());
        Assert.Equal("mastered", cell.GetProperty("status").GetString());
        Assert.Equal("Meets mastery", cell.GetProperty("rating").GetString());
        Assert.Equal("none", (await CellAsync(faculty, seed.OfferingId, seed.StudentB, clo)).GetProperty("status").GetString());

        // Rubric Strong + Poor = 7/10 → marks 14/20 = 70% → CLO1 2.8 (near); Argument 6/6 → PLO1 evidence 4.
        var scored = await faculty.PostAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/rubric", new
        {
            grades = new[]
            {
                new { criterionId = argument.GetProperty("criterionId").GetGuid(), levelId = Level(argument, "Strong") },
                new { criterionId = style.GetProperty("criterionId").GetGuid(), levelId = Level(style, "Poor") }
            },
            applyTotalToMarks = true
        });
        Assert.True(scored.IsSuccessStatusCode, await scored.Content.ReadAsStringAsync());
        cell = await CellAsync(faculty, seed.OfferingId, seed.StudentA, clo);
        Assert.Equal(2.8m, cell.GetProperty("score").GetDecimal());
        Assert.Equal("near", cell.GetProperty("status").GetString());
        Assert.Equal(4m, (await CellAsync(faculty, seed.OfferingId, seed.StudentA, plo)).GetProperty("score").GetDecimal());

        // A quiz question aligned to CLO1 and answered correctly → 4 → Highest = 4.
        var quizId = await CreateQuizAsync(faculty, seed.OfferingId);
        var questionId = await AddQuestionAsync(faculty, new
        {
            quizId, text = "2 + 2 = ?", type = "MultipleChoice", marks = 2, orderIndex = 1,
            options = new[] { new { text = "4", isCorrect = true, orderIndex = 1 }, new { text = "5", isCorrect = false, orderIndex = 2 } }
        });
        (await faculty.PostAsync($"api/v1/quiz/{quizId}/publish", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync("api/v1/outcomes/alignments", new { targetType = "QuizQuestion", targetId = questionId, outcomeIds = new[] { clo } })).StatusCode);
        var right = (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}")).GetProperty("questions")[0].GetProperty("options")
            .EnumerateArray().Single(o => o.GetProperty("isCorrect").GetBoolean()).GetProperty("optionId").GetGuid();
        await TakeQuizAsync(studentA, quizId, questionId, right);
        cell = await CellAsync(faculty, seed.OfferingId, seed.StudentA, clo);
        Assert.Equal(4m, cell.GetProperty("score").GetDecimal());
        Assert.Equal(2, cell.GetProperty("evidence").GetInt32());

        // The student's own view: CLO1 mastered with two pieces of evidence; the other student has none.
        var mine = await studentA.GetFromJsonAsync<JsonElement>("api/v1/outcomes/mastery/me");
        var course = mine.EnumerateArray().Single(c => c.GetProperty("courseOfferingId").GetGuid() == seed.OfferingId);
        var cloItem = course.GetProperty("outcomes").EnumerateArray().Single(o => o.GetProperty("outcomeId").GetGuid() == clo);
        Assert.True(cloItem.GetProperty("mastered").GetBoolean());
        Assert.Equal(2, cloItem.GetProperty("evidence").GetArrayLength());
        var theirs = await Client("Student", seed.StudentB).GetFromJsonAsync<JsonElement>("api/v1/outcomes/mastery/me");
        Assert.Equal("none", theirs.EnumerateArray().Single(c => c.GetProperty("courseOfferingId").GetGuid() == seed.OfferingId)
            .GetProperty("outcomes").EnumerateArray().Single(o => o.GetProperty("outcomeId").GetGuid() == clo).GetProperty("status").GetString());

        // Program attainment: PLO1 = direct rubric evidence + mapped CLO1 evidence (decaying average) → 1 of 1 attained.
        var report = await admin.GetFromJsonAsync<JsonElement>($"api/v1/outcomes/attainment/programs/{seed.ProgramId}?target=70");
        var row = report.GetProperty("outcomes").EnumerateArray().Single(o => o.GetProperty("outcomeId").GetGuid() == plo);
        Assert.Equal(1, row.GetProperty("studentsAssessed").GetInt32());
        Assert.Equal(1, row.GetProperty("studentsMastered").GetInt32());
        Assert.Equal(100m, row.GetProperty("attainmentPercent").GetDecimal());
        Assert.True(row.GetProperty("targetMet").GetBoolean());
        Assert.Contains(row.GetProperty("mappedCourseOutcomes").EnumerateArray(), m => m.GetString()!.StartsWith("CLO1"));
        Assert.Equal(seed.CourseCode, row.GetProperty("contributions")[0].GetProperty("courseCode").GetString());

        // Exports.
        var xlsx = await faculty.GetAsync($"api/v1/outcomes/mastery/offerings/{seed.OfferingId}/export");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", xlsx.Content.Headers.ContentType!.MediaType);
        using (var book = new XLWorkbook(await xlsx.Content.ReadAsStreamAsync()))
            Assert.Contains("CLO1", book.Worksheet(1).Row(3).CellsUsed().Select(c => c.GetString()).First(s => s.StartsWith("CLO1")));
        var pdf = await admin.GetByteArrayAsync($"api/v1/outcomes/attainment/programs/{seed.ProgramId}/export?format=pdf");
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        var attainmentXlsx = await admin.GetByteArrayAsync($"api/v1/outcomes/attainment/programs/{seed.ProgramId}/export?format=xlsx");
        Assert.Equal((byte)'P', attainmentXlsx[0]);

        // Removing the assignment alignment removes its evidence (the quiz remains).
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync("api/v1/outcomes/alignments", new { targetType = "Assignment", targetId = assignmentId, outcomeIds = Array.Empty<Guid>() })).StatusCode);
        Assert.Equal(1, (await CellAsync(faculty, seed.OfferingId, seed.StudentA, clo)).GetProperty("evidence").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsync($"api/v1/outcomes/mastery/offerings/{seed.OfferingId}/recalculate", null)).StatusCode);
        Assert.Equal(1, (await CellAsync(faculty, seed.OfferingId, seed.StudentA, clo)).GetProperty("evidence").GetInt32());

        // Students cannot reach staff data.
        Assert.Equal(HttpStatusCode.Forbidden, (await studentA.GetAsync($"api/v1/outcomes/mastery/offerings/{seed.OfferingId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await studentA.GetAsync("api/v1/outcomes")).StatusCode);
    }

    [Fact]
    public async Task Validation_Permissions_Scale_AndModuleGate()
    {
        await ActivateModuleAsync();
        var seed = await SeedAsync();
        var other = await SeedAsync();
        var admin = Client("Admin");
        var faculty = Client("Faculty");

        var clo = await CreateOutcomeAsync(faculty, new { level = "Course", courseId = seed.CourseId, code = "CLO1", title = "Outcome" });
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync("api/v1/outcomes", new { level = "Course", courseId = seed.CourseId, code = "CLO1", title = "Duplicate" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync("api/v1/outcomes", new { level = "Course", code = "X", title = "No course" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await faculty.PostAsJsonAsync("api/v1/outcomes", new { level = "Institution", code = "ILO", title = "x" })).StatusCode);

        // Custom scale: mastery must be on the scale; points distinct.
        var bad = await faculty.PutAsJsonAsync($"api/v1/outcomes/{clo}", new
        {
            code = "CLO1", title = "Outcome", ratings = new[] { new { description = "Good", points = 5m }, new { description = "Poor", points = 0m } }, masteryPoints = 6m
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var good = await faculty.PutAsJsonAsync($"api/v1/outcomes/{clo}", new
        {
            code = "CLO1", title = "Outcome", ratings = new[] { new { description = "Excellent", points = 10m }, new { description = "Pass", points = 6m }, new { description = "Fail", points = 0m } },
            masteryPoints = 6m, calculationMethod = "NMastery", calculationParameter = 2
        });
        Assert.True(good.StatusCode == HttpStatusCode.OK, await good.Content.ReadAsStringAsync());
        var saved = await good.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10m, saved.GetProperty("maxPoints").GetDecimal());
        Assert.Equal("NMastery", saved.GetProperty("calculationMethod").GetString());
        Assert.Equal(3, saved.GetProperty("ratings").GetArrayLength());

        // Mapping only to program outcomes; aligning only outcomes the course can use.
        var otherClo = await CreateOutcomeAsync(faculty, new { level = "Course", courseId = other.CourseId, code = "CLO1", title = "Other course" });
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PutAsJsonAsync($"api/v1/outcomes/{clo}/mappings", new { programOutcomeIds = new[] { otherClo } })).StatusCode);
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, 10);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PutAsJsonAsync("api/v1/outcomes/alignments", new { targetType = "Assignment", targetId = assignmentId, outcomeIds = new[] { otherClo } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.GetAsync($"api/v1/outcomes/alignments?targetType=Banana&targetId={assignmentId}")).StatusCode);

        // Deactivated outcomes leave the lists.
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsync($"api/v1/outcomes/{clo}/deactivate", null)).StatusCode);
        var list = await admin.GetFromJsonAsync<JsonElement>($"api/v1/outcomes?courseId={seed.CourseId}");
        Assert.DoesNotContain(list.EnumerateArray(), o => o.GetProperty("id").GetGuid() == clo);
        Assert.Contains((await admin.GetFromJsonAsync<JsonElement>($"api/v1/outcomes?courseId={seed.CourseId}&includeInactive=true")).EnumerateArray(), o => o.GetProperty("id").GetGuid() == clo);

        var superAdmin = Client("SuperAdmin");
        Assert.True((await superAdmin.PostAsync("api/v1/module/outcomes/deactivate", null)).IsSuccessStatusCode);
        try { Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("api/v1/outcomes")).StatusCode); }
        finally { await ActivateModuleAsync(); }
    }

    [Fact]
    public async Task Import_CreatesUpdatesAndMaps_AndReportsBadRows()
    {
        await ActivateModuleAsync();
        var seed = await SeedAsync();
        var admin = Client("Admin");

        var template = await admin.GetAsync("api/v1/outcomes/import-template");
        Assert.Equal(HttpStatusCode.OK, template.StatusCode);
        var templateBytes = await template.Content.ReadAsByteArrayAsync();

        // Each import starts from the downloaded template with its example rows replaced.
        byte[] Workbook(string cloTitle)
        {
            using var source = new MemoryStream(templateBytes);
            using var book = new XLWorkbook(source);
            var sheet = book.Worksheet("Outcomes");
            Assert.Equal("Level", sheet.Cell(1, 1).GetString());
            sheet.Rows(2, sheet.LastRowUsed()!.RowNumber()).Delete();
            object[][] rows =
            [
                ["Program", seed.ProgramCode, "", "PLO1", "Apply knowledge", "", 3, "DecayingAverage", 65, "", ""],
                ["Program", seed.ProgramCode, "", "PLO1.1", "Apply mathematics", "", "", "", "", "PLO1", ""],
                ["Course", "", seed.CourseCode, "CLO1", cloTitle, "Uses loops", 3, "Latest", "", "", "PLO1"],
                ["Course", "", "NOPE-404", "CLO2", "Missing course", "", "", "", "", "", ""],
                ["Planet", "", "", "X1", "Bad level", "", "", "", "", "", ""]
            ];
            for (var r = 0; r < rows.Length; r++)
                for (var c = 0; c < rows[r].Length; c++)
                    sheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
            using var output = new MemoryStream();
            book.SaveAs(output);
            return output.ToArray();
        }

        var first = await ImportAsync(admin, Workbook("Write programs"));
        Assert.Equal(3, first.GetProperty("created").GetInt32());
        Assert.Equal(1, first.GetProperty("mapped").GetInt32());
        var errors = first.GetProperty("errors").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.StartsWith("Row 5:") && e.Contains("NOPE-404"));
        Assert.Contains(errors, e => e.StartsWith("Row 6:"));

        var outcomes = await admin.GetFromJsonAsync<JsonElement>($"api/v1/outcomes?programId={seed.ProgramId}");
        var plo1 = outcomes.EnumerateArray().Single(o => o.GetProperty("code").GetString() == "PLO1").GetProperty("id").GetGuid();
        Assert.Equal(plo1, outcomes.EnumerateArray().Single(o => o.GetProperty("code").GetString() == "PLO1.1").GetProperty("parentId").GetGuid());
        var clo = (await admin.GetFromJsonAsync<JsonElement>($"api/v1/outcomes?courseId={seed.CourseId}")).EnumerateArray().Single();
        Assert.Equal("Latest", clo.GetProperty("calculationMethod").GetString());
        Assert.Equal(plo1, clo.GetProperty("programOutcomeIds")[0].GetGuid());

        // Importing again updates instead of duplicating.
        var second = await ImportAsync(admin, Workbook("Write and test programs"));
        Assert.Equal(0, second.GetProperty("created").GetInt32());
        Assert.Equal(3, second.GetProperty("updated").GetInt32());
        Assert.Equal("Write and test programs", (await admin.GetFromJsonAsync<JsonElement>($"api/v1/outcomes?courseId={seed.CourseId}"))[0].GetProperty("title").GetString());

        var notExcel = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "file", "x.xlsx" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/v1/outcomes/import", notExcel)).StatusCode);
    }

    // ── helpers ──

    private sealed record Seed(Guid OfferingId, Guid CourseId, string CourseCode, Guid ProgramId, string ProgramCode, Guid StudentA, Guid StudentB);

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var department = new Department($"M6 Dept {suffix}", $"M6D{suffix}", InstitutionType.University);
        var program = new AcademicProgram($"M6 Program {suffix}", $"M6P{suffix}", department.Id, 8);
        var semester = new Semester($"M6 Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"M6 Course {suffix}", $"M6C{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40);
        offering.SetInstitutionScope(InstitutionType.University, null, null);
        db.AddRange(department, program, semester, course, offering);
        var ids = new List<Guid>();
        for (var i = 0; i < 2; i++)
        {
            var user = new User($"m6_{i}_{suffix}", "integration-hash", studentRole.Id, $"m6_{i}_{suffix}@tabsan.local");
            user.UpdateFullName($"M6 Student {i} {suffix}");
            var profile = new StudentProfile(user.Id, $"M6-{i}-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile, new Enrollment(profile.Id, offering.Id));
            ids.Add(profile.Id);
        }
        await db.SaveChangesAsync();
        return new Seed(offering.Id, course.Id, course.Code, program.Id, program.Code, ids[0], ids[1]);
    }

    private HttpClient Client(string role, Guid? studentProfileId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, Guid.NewGuid().ToString(), studentProfileId: studentProfileId?.ToString()));
        return client;
    }

    private async Task ActivateModuleAsync()
    {
        var response = await Client("SuperAdmin").PostAsync("api/v1/module/outcomes/activate", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<Guid> CreateOutcomeAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("api/v1/outcomes", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> CellAsync(HttpClient faculty, Guid offeringId, Guid studentId, Guid outcomeId)
    {
        var grid = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/outcomes/mastery/offerings/{offeringId}");
        return grid.GetProperty("students").EnumerateArray().Single(s => s.GetProperty("studentProfileId").GetGuid() == studentId)
            .GetProperty("cells").EnumerateArray().Single(c => c.GetProperty("outcomeId").GetGuid() == outcomeId);
    }

    private static async Task<JsonElement> ImportAsync(HttpClient client, byte[] workbook)
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(workbook), "file", "outcomes.xlsx" } };
        var response = await client.PostAsync("api/v1/outcomes/import", form);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, text);
        return JsonDocument.Parse(text).RootElement;
    }

    private static async Task<Guid> CreateAssignmentAsync(HttpClient faculty, Guid offeringId, decimal maxMarks)
    {
        var created = await faculty.PostAsJsonAsync("api/v1/assignment", new
        {
            courseOfferingId = offeringId, title = "M6 task " + Guid.NewGuid().ToString("N")[..5], description = "Do it", dueDate = DateTime.UtcNow.AddDays(7), maxMarks
        });
        var body = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, body);
        var id = JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.True((await faculty.PostAsync($"api/v1/assignment/{id}/publish", null)).IsSuccessStatusCode);
        return id;
    }

    private static async Task<Guid> CreateQuizAsync(HttpClient faculty, Guid offeringId)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/quiz", new
        {
            courseOfferingId = offeringId, title = "M6 quiz " + Guid.NewGuid().ToString("N")[..5], instructions = "",
            timeLimitMinutes = (int?)null, maxAttempts = 1, availableFrom = (DateTime?)null, availableUntil = (DateTime?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("quizId").GetGuid();
    }

    private static async Task<Guid> AddQuestionAsync(HttpClient faculty, object body)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/quiz/question", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("questionId").GetGuid();
    }

    private static async Task TakeQuizAsync(HttpClient student, Guid quizId, Guid questionId, Guid optionId)
    {
        var start = await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { accessCode = (string?)null });
        var startBody = await start.Content.ReadAsStringAsync();
        Assert.True(start.IsSuccessStatusCode, startBody);
        var attemptId = JsonDocument.Parse(startBody).RootElement.GetProperty("attemptId").GetGuid();
        var submit = await student.PostAsJsonAsync("api/v1/quiz/attempt/submit", new
        {
            attemptId, answers = new object[] { new { quizQuestionId = questionId, selectedOptionIds = new[] { optionId } } }
        });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
    }
}
