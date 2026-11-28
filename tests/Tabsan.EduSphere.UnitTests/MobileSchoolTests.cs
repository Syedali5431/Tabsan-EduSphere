using System.Net;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// The app's School tab: news (new marks), events with replies and guests, and forms filled in for a child — choices ticked
/// one or several at a time, answers sent as the API expects, saved answers shown again, the API's message when refused.
/// </summary>
public class MobileSchoolTests
{
    private sealed class Store : ISecureStore
    {
        private readonly Dictionary<string, string> _values = new();
        public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { _values[key] = value; return Task.CompletedTask; }
        public void Remove(string key) => _values.Remove(key);
    }

    private sealed class Api : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _answers = new();
        public readonly List<(string Key, string? Body)> Calls = new();

        public Api On(string key, HttpStatusCode status, object body)
        {
            _answers[key] = (status, body as string ?? JsonSerializer.Serialize(body));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var key = $"{request.Method} {request.RequestUri!.PathAndQuery}";
            Calls.Add((key, request.Content is null ? null : await request.Content.ReadAsStringAsync(ct)));
            return _answers.TryGetValue(key, out var a)
                ? new HttpResponseMessage(a.Status) { Content = new StringContent(a.Body, Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    private static async Task<(EduSphereClient Client, Api Api)> SignedInAsync(string role = "Parent")
    {
        var api = new Api();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, new
        {
            accessToken = "a1", refreshToken = "r1", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(), username = "nadia"
        });
        var client = new EduSphereClient(new HttpClient(api) { BaseAddress = new Uri("https://api.test/") }, new Store(), new AppSettings("https://api.test", "1.2.0", "android"));
        await client.SignInAsync("nadia", "pw");
        return (client, api);
    }

    private static readonly Guid EventId = Guid.NewGuid(), FormId = Guid.NewGuid(), Ali = Guid.NewGuid(), GoField = Guid.NewGuid(), LunchField = Guid.NewGuid(),
        AllergyField = Guid.NewGuid(), NameField = Guid.NewGuid(), InfoField = Guid.NewGuid();

    private static object Event(string? answer = null, int guests = 0) => new
    {
        id = EventId, title = "Parents' evening", location = "Main hall", startsAt = "2026-12-05T13:00:00Z", endsAt = "2026-12-05T15:00:00Z", allDay = false,
        category = "Meeting", status = "Published", repliesOpen = true, capacity = 40, maxGuests = 2, myAnswer = answer, myGuests = guests,
        counts = new { going = answer == "Going" ? 1 : 0, notGoing = 0, maybe = 0, places = answer == "Going" ? 1 + guests : 0 }, canReply = true
    };

    private static object Form(Dictionary<Guid, string>? answers = null, string? by = null) => new
    {
        id = FormId, title = "Zoo trip consent", description = "Friday's trip.", dueAt = "2026-12-04T12:00:00Z", status = "Open", takesAnswers = true,
        studentProfileId = Ali, studentName = "Ali", answers = answers ?? new Dictionary<Guid, string>(), answeredBy = by,
        fields = new object[]
        {
            new { id = InfoField, type = "Information", label = "The bus leaves at 8:00.", required = false, options = Array.Empty<string>() },
            new { id = GoField, type = "YesNo", label = "May Ali go?", required = true, options = Array.Empty<string>() },
            new { id = LunchField, type = "SingleChoice", label = "Lunch", required = true, options = new[] { "Packed lunch", "School lunch" } },
            new { id = AllergyField, type = "MultipleChoice", label = "Allergies", required = false, options = new[] { "Nuts", "Dairy", "Eggs" } },
            new { id = NameField, type = "Signature", label = "Your full name", required = true, options = Array.Empty<string>() }
        }
    };

    [Fact]
    public async Task SchoolTab_LoadsNewsEventsAndForms_WithCounts_AndRepliesToAnEvent()
    {
        var (client, api) = await SignedInAsync();
        var post = Guid.NewGuid();
        api.On("GET /api/v1/community/news", HttpStatusCode.OK, new object[]
            {
                new { id = post, title = "Sports day", summary = "Friday on the big field.", publishAt = "2026-11-27T08:00:00Z", isPinned = true, isNew = true, authorName = "Mrs Admin" },
                new { id = Guid.NewGuid(), title = "Uniform", publishAt = "2026-11-20T08:00:00Z", isPinned = false, isNew = false, authorName = "Mrs Admin" }
            })
           .On("GET /api/v1/community/events", HttpStatusCode.OK, new[] { Event() })
           .On("GET /api/v1/community/forms", HttpStatusCode.OK, new object[]
            {
                new { formId = FormId, title = "Zoo trip consent", studentProfileId = Ali, studentName = "Ali", answered = false, open = true },
                new { formId = FormId, title = "Zoo trip consent", studentProfileId = Guid.NewGuid(), studentName = "Zoya", answered = true, answeredBy = "Mr Raza", open = true }
            })
           .On($"POST /api/v1/community/events/{EventId}/reply", HttpStatusCode.OK, Event("Going", 2));

        var school = new SchoolViewModel(client);
        await school.LoadAsync();
        Assert.Null(school.Error);
        Assert.Equal((2, 1, 2), (school.News.Count, school.Events.Count, school.Forms.Count));
        Assert.Equal((1, 1), (school.NewNews, school.FormsWaiting));
        Assert.Equal(["Ali", "Zoya"], school.Forms.Select(f => f.StudentName));   // still to answer first
        Assert.Equal(string.Format(AppText.Get("ForChild"), "Ali"), school.Forms[0].For);
        Assert.Contains("Main hall", school.Events[0].When);
        school.MarkRead(school.News[0]);
        Assert.Equal(0, school.NewNews);

        Assert.True(await school.ReplyAsync(school.Events[0], "Going", 5)); // more guests than allowed: at most two are sent
        using (var body = JsonDocument.Parse(api.Calls.Last().Body!))
            Assert.Equal(("Going", 2), (body.RootElement.GetProperty("answer").GetString(), body.RootElement.GetProperty("guests").GetInt32()));
        var replied = school.Events.Single();
        Assert.Equal($"{AppText.Get("YourReply")}: {AppText.Get("Going")} (+2)", replied.MyReply);
        Assert.Equal(string.Format(AppText.Get("RepliesSoFar"), 1, 0) + " · " + string.Format(AppText.Get("PlacesTaken"), 3, 40), replied.CountsText);
        Assert.Equal(AppText.Get("ReplySaved"), school.Notice);
    }

    [Fact]
    public async Task Form_TicksChoices_SendsTheAnswersForTheChild_AndShowsThemSaved()
    {
        var (client, api) = await SignedInAsync();
        api.On($"GET /api/v1/community/forms/{FormId}?studentProfileId={Ali}", HttpStatusCode.OK, Form(new() { [AllergyField] = "Nuts\nEggs" }));
        var form = new FormViewModel(client);
        await form.LoadAsync(FormId, Ali);
        Assert.Null(form.Error);
        Assert.True(form.CanSend);
        Assert.StartsWith(string.Format(AppText.Get("ForChild"), "Ali"), form.Subtitle);
        var q = form.Questions.ToDictionary(x => x.Field.Id);
        Assert.True(q[InfoField].IsInformation);
        Assert.Equal(["Nuts", "Eggs"], q[AllergyField].Choices.Where(c => c.IsChecked).Select(c => c.Value)); // earlier answers shown
        Assert.Equal("May Ali go? *", q[GoField].Label);

        q[GoField].Pick(q[GoField].Choices[0], true);                         // Yes
        q[LunchField].Pick(q[LunchField].Choices[0], true);
        q[LunchField].Pick(q[LunchField].Choices[1], true);                   // one choice: the second replaces the first
        q[AllergyField].Pick(q[AllergyField].Choices[1], true);               // several: Dairy joins Nuts and Eggs
        q[AllergyField].Pick(q[AllergyField].Choices[2], false);              // and Eggs goes
        q[NameField].Value = "  Nadia Raza ";

        api.On($"POST /api/v1/community/forms/{FormId}/responses", HttpStatusCode.OK,
            Form(new() { [GoField] = "Yes", [LunchField] = "School lunch", [AllergyField] = "Nuts\nDairy", [NameField] = "Nadia Raza" }, "Nadia Raza"));
        await form.SendAsync();
        Assert.Null(form.Error);
        using (var body = JsonDocument.Parse(api.Calls.Last().Body!))
        {
            Assert.Equal(Ali, body.RootElement.GetProperty("studentProfileId").GetGuid());
            var a = body.RootElement.GetProperty("answers");
            Assert.False(a.TryGetProperty(InfoField.ToString(), out _));     // nothing to answer
            Assert.Equal("Yes", a.GetProperty(GoField.ToString()).GetString());
            Assert.Equal("School lunch", a.GetProperty(LunchField.ToString()).GetString());
            Assert.Equal("Nuts\nDairy", a.GetProperty(AllergyField.ToString()).GetString());
            Assert.Equal("Nadia Raza", a.GetProperty(NameField.ToString()).GetString());
        }
        Assert.Equal(AppText.Get("AnswersSaved"), form.Notice);
        Assert.Contains("Nadia Raza", form.Subtitle);                          // answered by
        Assert.Equal("School lunch", form.Questions.Single(x => x.Field.Id == LunchField).Answer);

        api.On($"POST /api/v1/community/forms/{FormId}/responses", HttpStatusCode.Conflict, """{"message":"The form is closed."}""");
        await form.SendAsync();
        Assert.Equal("The form is closed.", form.Error);
    }
}
