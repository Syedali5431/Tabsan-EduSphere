using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M2 — quiz engine through the real API pipeline and database: every question type,
/// banks and draw groups, shuffling snapshot, no answer leakage, ownership, access code,
/// IP allow-list, locking, accommodations, overrides, server-side time limit, review options,
/// manual grading, regrade, statistics and Excel round trip.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class QuizEngineIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public QuizEngineIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    // ═════════════════════════════════════════════════════════════════════════
    // Full lifecycle with every auto-graded type + essay
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllQuestionTypes_EndToEnd_AutoGrade_ManualGrade_Statistics_Regrade()
    {
        var offeringId = await SeedOfferingAsync();
        using var faculty = Client("Faculty");
        var quizId = await CreateQuizAsync(faculty, offeringId, timeLimit: 30, maxAttempts: 2);

        // Invalid question is refused with a readable message.
        var bad = await faculty.PostAsJsonAsync("api/v1/quiz/question", new
        {
            quizId, text = "Pick one", type = "MultipleChoice", marks = 1, orderIndex = 1,
            options = new[] { new { text = "a", isCorrect = true, orderIndex = 1 }, new { text = "b", isCorrect = true, orderIndex = 2 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Contains("exactly one", await bad.Content.ReadAsStringAsync());

        await AddAsync(faculty, quizId, "Capital of France?", "MultipleChoice", 2, 1, options: [("Paris", true), ("Rome", false), ("Madrid", false)]);
        await AddAsync(faculty, quizId, "Prime numbers?", "MultipleAnswers", 2, 2, options: [("2", true), ("3", true), ("4", false)]);
        await AddAsync(faculty, quizId, "Match countries", "Matching", 2, 3,
            config: """{"pairs":[{"prompt":"France","answer":"Paris"},{"prompt":"Italy","answer":"Rome"}],"distractors":["Berlin"]}""");
        await AddAsync(faculty, quizId, "Order the numbers", "Ordering", 2, 4, config: """{"items":[{"text":"one"},{"text":"two"},{"text":"three"}]}""");
        await AddAsync(faculty, quizId, "The capital of Italy is [[1]].", "FillInBlank", 2, 5, config: """{"blanks":[{"id":"1","answers":["Rome"]}]}""");
        await AddAsync(faculty, quizId, "Value of pi to 2dp?", "Numeric", 2, 6, config: """{"answer":3.14,"tolerance":0.005}""");
        await AddAsync(faculty, quizId, "Multiply [a] by [b].", "Formula", 2, 7,
            config: """{"expression":"a*b","variables":[{"name":"a","min":2,"max":9},{"name":"b","min":2,"max":9}],"decimals":0}""");
        await AddAsync(faculty, quizId, "Explain photosynthesis.", "Essay", 4, 8);

        await SettingsAsync(faculty, quizId, shuffleQuestions: true, shuffleOptions: true, review: "CorrectAnswers");
        (await faculty.PostAsync($"api/v1/quiz/{quizId}/publish", null)).EnsureSuccessStatusCode();
        var detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}");
        Assert.Equal(18m, detail.GetProperty("totalMarks").GetDecimal());

        // Student sees quiz info only — never questions or correctness.
        var studentId = Guid.NewGuid();
        using var student = Client("Student", studentId);
        var info = await student.GetStringAsync($"api/v1/quiz/{quizId}");
        Assert.DoesNotContain("isCorrect", info, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Paris", info);

        var attempt = await StartAsync(student, quizId);
        var deadline = attempt.GetProperty("deadlineAt").GetDateTime();
        Assert.InRange(deadline, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
        var attemptId = attempt.GetProperty("attemptId").GetGuid();

        var paperJson = await student.GetStringAsync($"api/v1/quiz/attempt/{attemptId}/paper");
        Assert.DoesNotContain("isCorrect", paperJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tolerance", paperJson);
        var paper = JsonDocument.Parse(paperJson).RootElement;
        var questions = paper.GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(8, questions.Count);

        // Answer everything correctly (correct option ids come from the faculty view).
        var answers = BuildCorrectAnswers(questions, detail);
        var save = await student.PostAsJsonAsync($"api/v1/quiz/attempt/{attemptId}/answers", new { answers });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        // Another student cannot see or submit this attempt.
        using var intruder = Client("Student", Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"api/v1/quiz/attempt/{attemptId}/paper")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsJsonAsync("api/v1/quiz/attempt/submit", new { attemptId, answers = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"api/v1/quiz/attempt/{attemptId}")).StatusCode);

        var submit = await student.PostAsJsonAsync("api/v1/quiz/attempt/submit", new { attemptId, answers = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var review = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(IsMissing(review, "totalScore")); // essay pending
        var items = review.GetProperty("items").EnumerateArray().ToList();
        Assert.All(items.Where(i => i.GetProperty("question").GetProperty("type").GetString() != "Essay"),
            i => Assert.Equal(i.GetProperty("marks").GetDecimal(), i.GetProperty("marksAwarded").GetDecimal()));
        Assert.Contains(items, i => !IsMissing(i, "correctAnswer") && i.GetProperty("correctAnswer").GetString()!.Contains("Paris"));

        // Grading queue → grade the essay (over-max refused) → total completes.
        var queue = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}/grading-queue");
        var essay = queue.EnumerateArray().Single(q => q.GetProperty("attemptId").GetGuid() == attemptId);
        var essayAnswerId = essay.GetProperty("answerId").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync("api/v1/quiz/attempt/grade-answer", new { answerId = essayAnswerId, marksAwarded = 5, feedback = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsJsonAsync("api/v1/quiz/attempt/grade-answer", new { answerId = essayAnswerId, marksAwarded = 4, feedback = "Excellent" })).StatusCode);

        var graded = await student.GetFromJsonAsync<JsonElement>($"api/v1/quiz/attempt/{attemptId}/review");
        Assert.Equal(18m, graded.GetProperty("totalScore").GetDecimal());
        Assert.Contains(graded.GetProperty("items").EnumerateArray(), i => !IsMissing(i, "feedback"));

        // Statistics: one completed attempt, everyone right.
        var stats = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}/statistics");
        Assert.Equal(1, stats.GetProperty("completedAttempts").GetInt32());
        Assert.Equal(18m, stats.GetProperty("meanScore").GetDecimal());
        var mcStats = stats.GetProperty("questions").EnumerateArray().Single(q => q.GetProperty("type").GetString() == "MultipleChoice");
        Assert.Equal(1m, mcStats.GetProperty("difficultyIndex").GetDecimal());

        // Fix the "wrong" key: make Rome correct instead of Paris, recalculate → MC drops to 0.
        var mc = detail.GetProperty("questions").EnumerateArray().Single(q => q.GetProperty("type").GetString() == "MultipleChoice");
        var edit = await faculty.PutAsJsonAsync($"api/v1/quiz/question/{mc.GetProperty("questionId").GetGuid()}/full", new
        {
            text = "Capital of France?", marks = 2, orderIndex = 1, configJson = (string?)null,
            options = new[] { new { text = "Paris", isCorrect = false, orderIndex = 1 }, new { text = "Rome", isCorrect = true, orderIndex = 2 }, new { text = "Madrid", isCorrect = false, orderIndex = 3 } }
        });
        Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
        var regrade = await faculty.PostAsJsonAsync($"api/v1/quiz/{quizId}/questions/{mc.GetProperty("questionId").GetGuid()}/regrade", new { mode = "Recalculate" });
        Assert.Equal(1, (await regrade.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attemptsUpdated").GetInt32());
        Assert.Equal(16m, (await student.GetFromJsonAsync<JsonElement>($"api/v1/quiz/attempt/{attemptId}/review")).GetProperty("totalScore").GetDecimal());

        // Full credit restores it; questions with answers cannot be deleted or lose options.
        await faculty.PostAsJsonAsync($"api/v1/quiz/{quizId}/questions/{mc.GetProperty("questionId").GetGuid()}/regrade", new { mode = "FullCredit" });
        Assert.Equal(18m, (await student.GetFromJsonAsync<JsonElement>($"api/v1/quiz/attempt/{attemptId}/review")).GetProperty("totalScore").GetDecimal());
        Assert.Equal(HttpStatusCode.Conflict, (await faculty.DeleteAsync($"api/v1/quiz/question/{mc.GetProperty("questionId").GetGuid()}")).StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Banks, draw groups, copying, Excel round trip
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task QuestionBank_DrawGroup_Copy_ExcelRoundTrip()
    {
        var offeringId = await SeedOfferingAsync();
        using var faculty = Client("Faculty");

        var bankId = (await (await faculty.PostAsJsonAsync("api/v1/quiz/banks", new { title = "Algebra pool", courseOfferingId = offeringId, isShared = false }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("bankId").GetGuid();
        for (var i = 1; i <= 4; i++)
        {
            var add = await faculty.PostAsJsonAsync($"api/v1/quiz/banks/{bankId}/questions", new
            {
                text = $"What is {i} + {i}?", type = "Numeric", marks = 1, configJson = $$"""{"answer":{{i * 2}}}""",
                tags = i % 2 == 0 ? "even" : "odd", difficulty = i
            });
            Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        }

        var quizId = await CreateQuizAsync(faculty, offeringId);
        await AddAsync(faculty, quizId, "Own question: 1+1?", "Numeric", 1, 1, config: """{"answer":2}""");
        var group = await faculty.PostAsJsonAsync($"api/v1/quiz/{quizId}/groups",
            new { questionBankId = bankId, title = "Two random sums", pickCount = 2, marksPerQuestion = 5, orderIndex = 2, tagFilter = (string?)null });
        Assert.Equal(HttpStatusCode.OK, group.StatusCode);
        (await faculty.PostAsync($"api/v1/quiz/{quizId}/publish", null)).EnsureSuccessStatusCode();

        var detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/{quizId}");
        Assert.Equal(11m, detail.GetProperty("totalMarks").GetDecimal());

        using var student = Client("Student", Guid.NewGuid());
        var attemptId = (await StartAsync(student, quizId)).GetProperty("attemptId").GetGuid();
        var paper = await student.GetFromJsonAsync<JsonElement>($"api/v1/quiz/attempt/{attemptId}/paper");
        var qs = paper.GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(3, qs.Count);
        Assert.Equal(2, qs.Count(q => q.GetProperty("marks").GetDecimal() == 5));
        Assert.Equal(11m, paper.GetProperty("totalMarks").GetDecimal());

        // Answer the drawn bank questions using the sum shown in their text.
        var answers = qs.Select(q =>
        {
            var m = Regex.Match(q.GetProperty("text").GetString()!, @"(\d+) \+ (\d+)");
            var value = m.Success ? int.Parse(m.Groups[1].Value) * 2 : 2;
            return new { quizQuestionId = q.GetProperty("questionId").GetGuid(), responseJson = $$"""{"value":"{{value}}"}""" };
        }).ToList();
        var submit = await student.PostAsJsonAsync("api/v1/quiz/attempt/submit", new { attemptId, answers });
        Assert.Equal(11m, (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalScore").GetDecimal()); // default review = ScoreOnly

        // A bank used by a quiz cannot be deleted; copying bank questions into a quiz works.
        Assert.Equal(HttpStatusCode.Conflict, (await faculty.DeleteAsync($"api/v1/quiz/banks/{bankId}")).StatusCode);
        var bank = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/banks/{bankId}");
        var firstBankQuestion = bank.GetProperty("questions")[0].GetProperty("questionId").GetGuid();
        var quiz2 = await CreateQuizAsync(faculty, offeringId);
        var copy = await faculty.PostAsJsonAsync($"api/v1/quiz/{quiz2}/copy-from-bank", new { questionIds = new[] { firstBankQuestion } });
        Assert.Equal(1, (await copy.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("copied").GetInt32());

        // Excel export → import into a new bank reproduces every question.
        var xlsx = await faculty.GetByteArrayAsync($"api/v1/quiz/banks/{bankId}/export");
        var bank2 = (await (await faculty.PostAsJsonAsync("api/v1/quiz/banks", new { title = "Imported", isShared = true }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("bankId").GetGuid();
        using var form = new MultipartFormDataContent { { new ByteArrayContent(xlsx), "file", "bank.xlsx" } };
        var import = await (await faculty.PostAsync($"api/v1/quiz/banks/{bank2}/import", form)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, import.GetProperty("imported").GetInt32());
        Assert.Equal(0, import.GetProperty("errors").GetArrayLength());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Access code, IP allow-list, locking, accommodations, overrides, timing, review
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Security_AccessCode_IpAllowList_Locking_ReviewVisibility()
    {
        var offeringId = await SeedOfferingAsync();
        using var faculty = Client("Faculty");
        var quizId = await CreateQuizAsync(faculty, offeringId, maxAttempts: 0);
        var qid = await AddAsync(faculty, quizId, "2+2?", "Numeric", 1, 1, config: """{"answer":4}""");
        await SettingsAsync(faculty, quizId, accessCode: "OPEN-SESAME", lockAnswered: true, review: "None");
        (await faculty.PostAsync($"api/v1/quiz/{quizId}/publish", null)).EnsureSuccessStatusCode();

        using var student = Client("Student", Guid.NewGuid());
        Assert.Contains("ACCESS_CODE_REQUIRED", await (await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { })).Content.ReadAsStringAsync());
        Assert.Contains("ACCESS_CODE_INVALID", await (await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { accessCode = "nope" })).Content.ReadAsStringAsync());
        var attemptId = (await StartAsync(student, quizId, "OPEN-SESAME")).GetProperty("attemptId").GetGuid();

        // Lock after answering: the second save of the same question is skipped.
        var first = await (await student.PostAsJsonAsync($"api/v1/quiz/attempt/{attemptId}/answers",
            new { answers = new[] { new { quizQuestionId = qid, responseJson = """{"value":"5"}""" } } })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, first.GetProperty("saved").GetInt32());
        var second = await (await student.PostAsJsonAsync($"api/v1/quiz/attempt/{attemptId}/answers",
            new { answers = new[] { new { quizQuestionId = qid, responseJson = """{"value":"4"}""" } } })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, second.GetProperty("lockedSkipped").GetInt32());

        // Review "None": no score, no items.
        var review = await (await student.PostAsJsonAsync("api/v1/quiz/attempt/submit", new { attemptId, answers = Array.Empty<object>() })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(IsMissing(review, "totalScore"));
        Assert.True(IsMissing(review, "items"));
        // ...but faculty see the real (locked first) answer: 0 marks.
        var facultyView = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/quiz/attempt/{attemptId}");
        Assert.Equal(0m, facultyView.GetProperty("totalScore").GetDecimal());

        // IP allow-list: the test server has no client address, so it is refused.
        await SettingsAsync(faculty, quizId, ipRanges: "10.0.0.0/8", review: "ScoreOnly");
        Assert.Contains("IP_NOT_ALLOWED", await (await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { })).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PutAsJsonAsync($"api/v1/quiz/{quizId}/settings", SettingsBody(ipRanges: "999.1.1.1/8"))).StatusCode);
    }

    [Fact]
    public async Task Accommodations_Overrides_AndServerSideTimeLimit()
    {
        var offeringId = await SeedOfferingAsync();
        using var faculty = Client("Faculty");
        var quizId = await CreateQuizAsync(faculty, offeringId, timeLimit: 20, maxAttempts: 1,
            availableUntil: DateTime.UtcNow.AddMinutes(-5)); // already closed
        var qid = await AddAsync(faculty, quizId, "3+3?", "Numeric", 2, 1, config: """{"answer":6}""");
        (await faculty.PostAsync($"api/v1/quiz/{quizId}/publish", null)).EnsureSuccessStatusCode();

        var studentId = Guid.NewGuid();
        using var student = Client("Student", studentId);
        Assert.Contains("CLOSED", await (await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { })).Content.ReadAsStringAsync());

        // Override re-opens it for this student; accommodation gives 1.5× + 5 min and one extra attempt.
        Assert.Equal(HttpStatusCode.OK, (await faculty.PostAsJsonAsync($"api/v1/quiz/{quizId}/overrides",
            new { studentProfileId = studentId, availableFrom = (DateTime?)null, availableUntil = DateTime.UtcNow.AddDays(1), note = "make-up" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await faculty.PostAsJsonAsync("api/v1/quiz/accommodations",
            new { courseOfferingId = offeringId, quizId = (Guid?)null, studentProfileId = studentId, extraTimeMinutes = 5, timeMultiplier = 1.5m, extraAttempts = 1, note = "LSP" })).StatusCode);

        var attempt = await StartAsync(student, quizId);
        var deadline = attempt.GetProperty("deadlineAt").GetDateTime();
        Assert.InRange(deadline, DateTime.UtcNow.AddMinutes(34), DateTime.UtcNow.AddMinutes(36)); // 20 × 1.5 + 5
        var attemptId = attempt.GetProperty("attemptId").GetGuid();

        // Starting again resumes the same attempt.
        Assert.Equal(attemptId, (await StartAsync(student, quizId)).GetProperty("attemptId").GetGuid());

        await student.PostAsJsonAsync($"api/v1/quiz/attempt/{attemptId}/answers",
            new { answers = new[] { new { quizQuestionId = qid, responseJson = """{"value":"6"}""" } } });

        // Simulate the clock running out: the next save is refused and the attempt is auto-submitted and graded.
        await SetDeadlineAsync(attemptId, DateTime.UtcNow.AddMinutes(-5));
        var late = await student.PostAsJsonAsync($"api/v1/quiz/attempt/{attemptId}/answers",
            new { answers = new[] { new { quizQuestionId = qid, responseJson = """{"value":"7"}""" } } });
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Contains("TIME_EXPIRED", await late.Content.ReadAsStringAsync());
        var review = await student.GetFromJsonAsync<JsonElement>($"api/v1/quiz/attempt/{attemptId}/review");
        Assert.Equal("TimedOut", review.GetProperty("status").GetString());
        Assert.Equal(2m, review.GetProperty("totalScore").GetDecimal()); // the saved (correct) answer counts

        // The extra attempt from the accommodation allows one more; then attempts run out.
        var second = await StartAsync(student, quizId);
        await student.PostAsJsonAsync("api/v1/quiz/attempt/submit", new { attemptId = second.GetProperty("attemptId").GetGuid(), answers = Array.Empty<object>() });
        Assert.Contains("NO_ATTEMPTS_LEFT", await (await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { })).Content.ReadAsStringAsync());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private static List<object> BuildCorrectAnswers(List<JsonElement> paperQuestions, JsonElement facultyDetail)
    {
        var key = facultyDetail.GetProperty("questions").EnumerateArray().ToDictionary(q => q.GetProperty("questionId").GetGuid());
        var result = new List<object>();
        foreach (var q in paperQuestions)
        {
            var id = q.GetProperty("questionId").GetGuid();
            var type = q.GetProperty("type").GetString();
            key[id].TryGetProperty("configJson", out var config);
            var correctOptions = key[id].GetProperty("options").EnumerateArray()
                .Where(o => o.GetProperty("isCorrect").GetBoolean()).Select(o => o.GetProperty("optionId").GetGuid()).ToList();

            switch (type)
            {
                case "MultipleChoice":
                case "MultipleAnswers":
                    result.Add(new { quizQuestionId = id, selectedOptionIds = correctOptions });
                    break;
                case "Matching":
                {
                    var pairs = JsonDocument.Parse(config.GetString()!).RootElement.GetProperty("pairs").EnumerateArray()
                        .ToDictionary(p => p.GetProperty("id").GetString()!, p => p.GetProperty("answer").GetString()!);
                    result.Add(new { quizQuestionId = id, responseJson = JsonSerializer.Serialize(new { matches = pairs }) });
                    break;
                }
                case "Ordering":
                {
                    var order = JsonDocument.Parse(config.GetString()!).RootElement.GetProperty("items").EnumerateArray()
                        .Select(i => i.GetProperty("id").GetString()).ToList();
                    result.Add(new { quizQuestionId = id, responseJson = JsonSerializer.Serialize(new { order }) });
                    break;
                }
                case "FillInBlank":
                    result.Add(new { quizQuestionId = id, responseJson = """{"blanks":{"1":"rome"}}""" });
                    break;
                case "Numeric":
                    result.Add(new { quizQuestionId = id, responseJson = """{"value":"3.14"}""" });
                    break;
                case "Formula":
                {
                    var m = Regex.Match(q.GetProperty("text").GetString()!, @"Multiply (\d+) by (\d+)");
                    var product = int.Parse(m.Groups[1].Value) * int.Parse(m.Groups[2].Value);
                    result.Add(new { quizQuestionId = id, responseJson = $$"""{"value":"{{product}}"}""" });
                    break;
                }
                case "Essay":
                    result.Add(new { quizQuestionId = id, textResponse = "Plants turn light into chemical energy." });
                    break;
            }
        }
        return result;
    }

    /// <summary>The API omits null properties, so "null" means absent or JSON null.</summary>
    private static bool IsMissing(JsonElement e, string name)
        => !e.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null;

    private HttpClient Client(string role, Guid? studentProfileId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, Guid.NewGuid().ToString(), studentProfileId: studentProfileId?.ToString()));
        return client;
    }

    private async Task<Guid> SeedOfferingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var department = new Department($"Quiz Dept {suffix}", $"QD{suffix}", InstitutionType.University);
        var semester = new Semester($"Quiz Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"Quiz Course {suffix}", $"QC{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40);
        db.Departments.Add(department);
        db.Semesters.Add(semester);
        db.Courses.Add(course);
        db.CourseOfferings.Add(offering);
        await db.SaveChangesAsync();
        return offering.Id;
    }

    private static async Task<Guid> CreateQuizAsync(HttpClient faculty, Guid offeringId, int? timeLimit = null, int maxAttempts = 1, DateTime? availableUntil = null)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/quiz", new
        {
            courseOfferingId = offeringId, title = "M2 quiz " + Guid.NewGuid().ToString("N")[..5], instructions = "Do your best",
            timeLimitMinutes = timeLimit, maxAttempts, availableFrom = (DateTime?)null, availableUntil
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("quizId").GetGuid();
    }

    private static async Task<Guid> AddAsync(HttpClient faculty, Guid quizId, string text, string type, decimal marks, int order,
        (string Text, bool Correct)[]? options = null, string? config = null)
    {
        var response = await faculty.PostAsJsonAsync("api/v1/quiz/question", new
        {
            quizId, text, type, marks, orderIndex = order, configJson = config,
            options = options?.Select((o, i) => new { text = o.Text, isCorrect = o.Correct, orderIndex = i + 1 }).ToArray()
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{type}: {body}");
        return JsonDocument.Parse(body).RootElement.GetProperty("questionId").GetGuid();
    }

    private static object SettingsBody(bool shuffleQuestions = false, bool shuffleOptions = false, string? accessCode = null,
        string? ipRanges = null, bool lockAnswered = false, string review = "ScoreOnly")
        => new
        {
            shuffleQuestions, shuffleOptions, oneQuestionAtATime = false, lockAnsweredQuestions = lockAnswered,
            accessCode, allowedIpRanges = ipRanges, reviewVisibility = review, reviewAfterCloseOnly = false
        };

    private static async Task SettingsAsync(HttpClient faculty, Guid quizId, bool shuffleQuestions = false, bool shuffleOptions = false,
        string? accessCode = null, string? ipRanges = null, bool lockAnswered = false, string review = "ScoreOnly")
    {
        var response = await faculty.PutAsJsonAsync($"api/v1/quiz/{quizId}/settings",
            SettingsBody(shuffleQuestions, shuffleOptions, accessCode, ipRanges, lockAnswered, review));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<JsonElement> StartAsync(HttpClient student, Guid quizId, string? accessCode = null)
    {
        var response = await student.PostAsJsonAsync($"api/v1/quiz/{quizId}/start", new { accessCode });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement;
    }

    private async Task SetDeadlineAsync(Guid attemptId, DateTime deadline)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE quiz_attempts SET DeadlineAt = {deadline} WHERE Id = {attemptId}");
    }
}
