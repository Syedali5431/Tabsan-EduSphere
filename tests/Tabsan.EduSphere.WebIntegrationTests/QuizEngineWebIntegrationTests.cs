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
/// Plan M2 — the quiz builder, the student's paper (autosave, renderers) and the review,
/// rendered by the real portal against a fake API.
/// </summary>
public class QuizEngineWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string QuizId = "11111111-0000-0000-0000-000000000001";
    private const string AttemptId = "22222222-0000-0000-0000-000000000001";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public QuizEngineWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task QuizBuilder_RendersQuestionsGroupsAndEditors()
    {
        var api = new FakeApi("SuperAdmin");
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync($"/Portal/QuizBuilder?quizId={QuizId}");

        Assert.Contains("What is the capital of Australia?", html);
        Assert.Contains("Correct: Canberra", html);
        Assert.Contains("France &#x2192; Paris", html);
        Assert.Contains("Random geography", html);
        Assert.Contains("Only 2 question(s) available", html);
        Assert.True(Regex.Matches(html, "data-quiz-editor").Count >= 3, "each question gets an editor plus the add form");
        Assert.Contains("quiz-editor.js", html);
    }

    [Fact]
    public async Task AddQuizQuestion_TranslatesEditorJsonIntoApiRequest()
    {
        var api = new FakeApi("SuperAdmin");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/QuizBuilder?quizId={QuizId}");

        var post = await client.PostAsync("/Portal/AddQuizQuestion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html),
            ["quizId"] = QuizId,
            ["Type"] = "MultipleChoice",
            ["Text"] = "2 + 2 = ?",
            ["Marks"] = "1",
            ["OrderIndex"] = "3",
            ["OptionsJson"] = """[{"text":"4","isCorrect":true},{"text":"5","isCorrect":false},{"text":"  ","isCorrect":false}]""",
            ["ConfigJson"] = ""
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var sent = JsonSerializer.Deserialize<Tabsan.EduSphere.Application.DTOs.Quizzes.AddQuestionRequest>(
            api.Posts["/api/v1/quiz/question"], new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(Guid.Parse(QuizId), sent.QuizId);
        Assert.Equal("MultipleChoice", sent.Type);
        Assert.Equal(2, sent.Options!.Count); // the blank option is dropped
        Assert.True(sent.Options[0].IsCorrect);
        Assert.Equal("4", sent.Options[0].Text);
    }

    [Fact]
    public async Task QuizBuilder_IsNotAvailableToStudents()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var response = await client.GetAsync($"/Portal/QuizBuilder?quizId={QuizId}");

        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("data-quiz-editor", html);
        Assert.DoesNotContain("/api/v1/quiz/" + QuizId, api.Gets);
    }

    [Fact]
    public async Task Paper_RendersEveryQuestionTypeWithSavedAnswers()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync($"/Portal/TakeQuizAttempt?attemptId={AttemptId}");

        Assert.Contains("id=\"quiz-paper\"", html);
        Assert.Contains("take-quiz.js", html);
        Assert.Matches("type=\"radio\"[^>]*value=\"55555555-0000-0000-0000-000000000102\"[^>]*checked", html);
        Assert.Contains("data-match-id=\"b\"", html);
        Assert.Contains("data-item-id=\"3\"", html);
        Assert.Contains("data-blank-id=\"t\"", html);
        Assert.Matches("data-blank-id=\"t\"[^>]*value=\"100\"", html);
        Assert.Contains("data-numeric", html);
        Assert.Contains("data-word-limit=\"200\"", html);
        Assert.Contains("Uploaded: lab-report.pdf", html);
        Assert.Contains("data-deadline=\"20", html);
        Assert.DoesNotContain("[[t]]", html); // placeholders become inputs
    }

    [Fact]
    public async Task SaveQuizAnswers_ForwardsJsonWithAntiforgeryHeader()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/TakeQuizAttempt?attemptId={AttemptId}");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/Portal/SaveQuizAnswers?attemptId={AttemptId}")
        {
            Content = new StringContent("""[{"quizQuestionId":"55555555-0000-0000-0000-000000000006","responseJson":"{\"value\":\"3.14\"}"}]""", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("RequestVerificationToken", Token(html));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(result.RootElement.GetProperty("ok").GetBoolean());
        Assert.Contains("3.14", api.Posts[$"/api/v1/quiz/attempt/{AttemptId}/answers"]);
    }

    [Fact]
    public async Task SaveQuizAnswers_WithoutAntiforgeryToken_IsRejected()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var response = await client.PostAsync($"/Portal/SaveQuizAnswers?attemptId={AttemptId}",
            new StringContent("[]", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(api.Posts.ContainsKey($"/api/v1/quiz/attempt/{AttemptId}/answers"));
    }

    [Fact]
    public async Task ClosedAttempt_RedirectsToReview_WhichShowsCorrectAnswers()
    {
        var api = new FakeApi("Student") { PaperClosed = true };
        using var client = await SignedInClientAsync(api);

        var paper = await client.GetAsync($"/Portal/TakeQuizAttempt?attemptId={AttemptId}");
        Assert.Equal(HttpStatusCode.Redirect, paper.StatusCode);
        Assert.Contains("/Portal/QuizReview", paper.Headers.Location!.ToString());

        var html = await client.GetStringAsync($"/Portal/QuizReview?attemptId={AttemptId}");
        Assert.Contains("14.5", html);
        Assert.Contains("Correct answer:", html);
        Assert.Contains("Awaiting marking", html);
    }

    // ── harness ──────────────────────────────────────────────────────────────

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        var page = await client.GetStringAsync("/Login");
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(page),
            ["username"] = "tester",
            ["password"] = "Password#123"
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
        public bool PaperClosed { get; init; }
        public Dictionary<string, string> Posts { get; } = new();
        public List<string> Gets { get; } = new();

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                ["unique_name"] = "tester",
                ["role"] = role,
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private const string Q = "55555555-0000-0000-0000-0000000000";

        private static readonly string Detail = $$"""
            {"quizId":"{{QuizId}}","courseOfferingId":"44444444-0000-0000-0000-000000000001","title":"Week 6 check-in","timeLimitMinutes":30,"maxAttempts":2,"isPublished":true,"totalMarks":6,
             "settings":{"shuffleQuestions":true,"shuffleOptions":false,"oneQuestionAtATime":false,"lockAnsweredQuestions":false,"reviewVisibility":"CorrectAnswers","reviewAfterCloseOnly":false},
             "groups":[{"groupId":"{{Q}}90","questionBankId":"33333333-0000-0000-0000-000000000001","bankTitle":"Geography pool","title":"Random geography","pickCount":3,"marksPerQuestion":1,"orderIndex":5,"availableQuestions":2}],
             "questions":[
              {"questionId":"{{Q}}01","text":"What is the capital of Australia?","type":"MultipleChoice","marks":1,"orderIndex":1,
               "options":[{"optionId":"{{Q}}11","text":"Sydney","orderIndex":1,"isCorrect":false},{"optionId":"{{Q}}12","text":"Canberra","orderIndex":2,"isCorrect":true}]},
              {"questionId":"{{Q}}03","text":"Match each country to its capital.","type":"Matching","marks":2,"orderIndex":2,"options":[],
               "configJson":"{\"pairs\":[{\"id\":\"p1\",\"prompt\":\"France\",\"answer\":\"Paris\"},{\"id\":\"p2\",\"prompt\":\"Japan\",\"answer\":\"Tokyo\"}]}"}]}
            """;

        private const string Paper = $$$"""
            {"attemptId":"{{{AttemptId}}}","quizId":"{{{QuizId}}}","quizTitle":"Week 6 check-in","status":"InProgress","startedAt":"2026-10-19T00:00:00Z","deadlineAt":"2026-10-19T00:30:00Z","serverNow":"2026-10-19T00:10:00Z",
             "oneQuestionAtATime":false,"lockAnsweredQuestions":false,"totalMarks":13,
             "questions":[
              {"order":1,"questionId":"55555555-0000-0000-0000-000000000001","type":"MultipleChoice","text":"What is the capital of Australia?","marks":1,
               "options":[{"id":"55555555-0000-0000-0000-000000000102","text":"Canberra"},{"id":"55555555-0000-0000-0000-000000000101","text":"Sydney"}],
               "saved":{"selectedOptionIds":["55555555-0000-0000-0000-000000000102"]}},
              {"order":2,"questionId":"55555555-0000-0000-0000-000000000003","type":"Matching","text":"Match.","marks":2,
               "matchingPrompts":[{"id":"b","text":"Japan"},{"id":"a","text":"France"}],"matchingChoices":["Paris","Tokyo"]},
              {"order":3,"questionId":"55555555-0000-0000-0000-000000000004","type":"Ordering","text":"Order.","marks":2,
               "orderingItems":[{"id":"3","text":"Earth"},{"id":"1","text":"Mercury"}]},
              {"order":4,"questionId":"55555555-0000-0000-0000-000000000005","type":"FillInBlank","text":"Water boils at [[t]] C.","marks":1,"blankIds":["t"],
               "saved":{"selectedOptionIds":[],"responseJson":"{\"blanks\":{\"t\":\"100\"}}"}},
              {"order":5,"questionId":"55555555-0000-0000-0000-000000000006","type":"Numeric","text":"22/7?","marks":1},
              {"order":6,"questionId":"55555555-0000-0000-0000-000000000008","type":"Essay","text":"Explain.","marks":3,"wordLimit":200},
              {"order":7,"questionId":"55555555-0000-0000-0000-000000000009","type":"FileUpload","text":"Upload.","marks":3,"allowedExtensions":[".pdf"],"maxFileSizeMb":10,
               "saved":{"selectedOptionIds":[],"fileName":"lab-report.pdf"}}]}
            """;

        private const string Review = $$$"""
            {"attemptId":"{{{AttemptId}}}","quizId":"{{{QuizId}}}","studentProfileId":"55555555-0000-0000-0000-000000000700","status":"Submitted","startedAt":"2026-10-18T00:00:00Z","finishedAt":"2026-10-18T00:20:00Z",
             "totalScore":14.5,"maxScore":23,"reviewVisibility":"CorrectAnswers","answers":[],
             "items":[
              {"order":1,"question":{"order":1,"questionId":"55555555-0000-0000-0000-000000000001","type":"MultipleChoice","text":"What is the capital of Australia?","marks":1,
                "options":[{"id":"55555555-0000-0000-0000-000000000102","text":"Canberra"}],"saved":{"selectedOptionIds":["55555555-0000-0000-0000-000000000102"]}},
               "marks":1,"marksAwarded":1,"correctAnswer":"Canberra","pendingManualGrading":false},
              {"order":2,"question":{"order":2,"questionId":"55555555-0000-0000-0000-000000000008","type":"Essay","text":"Explain.","marks":3,"saved":{"selectedOptionIds":[],"textResponse":"Because."}},
               "marks":3,"pendingManualGrading":true}]}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            if (request.Method == HttpMethod.Post && body is not null) Posts[path] = body;
            if (request.Method == HttpMethod.Get) Gets.Add(path);

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                    role, userId = Guid.NewGuid(), username = "tester", mustChangePassword = false,
                    mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));

            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000b1","key":"quizzes","name":"Quizzes","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");

            if (path == $"/api/v1/quiz/{QuizId}" && request.Method == HttpMethod.Get) return Json(Detail);
            if (path == $"/api/v1/quiz/{QuizId}/info")
                return Json($$"""{"quizId":"{{QuizId}}","title":"Week 6 check-in","timeLimitMinutes":30,"maxAttempts":2,"requiresAccessCode":false,"questionCount":7}""");
            if (path == $"/api/v1/quiz/attempt/{AttemptId}/paper")
                return PaperClosed ? Json("""{"error":"ATTEMPT_CLOSED","message":"This attempt has already been submitted."}""", HttpStatusCode.Conflict) : Json(Paper);
            if (path == $"/api/v1/quiz/attempt/{AttemptId}/review") return Json(Review);
            if (path == $"/api/v1/quiz/attempt/{AttemptId}/answers") return Json("""{"saved":1,"lockedSkipped":0}""");
            if (path == "/api/v1/quiz/question" && request.Method == HttpMethod.Post)
                return Json("""{"questionId":"55555555-0000-0000-0000-000000000099"}""");
            if (path.StartsWith("/api/v1/quiz/banks") || path.EndsWith("/overrides") || path.Contains("/accommodations/") || path.StartsWith("/api/v1/enrollment/roster"))
                return Json("[]");

            // Everything else the layout touches (menus, notifications, branding...): empty but valid.
            return path.Contains("menu", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
