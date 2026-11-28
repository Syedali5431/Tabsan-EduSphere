using System.Net;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// The app's Wellbeing section on the School tab: a student checks in (asking to talk) and sees what the school shared;
/// a parent sees each child's shared notes and plans; staff have no such section; a school without pastoral care leaves
/// the rest of the tab working.
/// </summary>
public class MobileWellbeingTests
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

    private static async Task<(EduSphereClient Client, Api Api)> SignedInAsync(string role)
    {
        var api = new Api();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, new
        {
            accessToken = "a1", refreshToken = "r1", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(), username = "ali"
        });
        foreach (var list in new[] { "news", "events", "forms" }) api.On($"GET /api/v1/community/{list}", HttpStatusCode.OK, Array.Empty<object>());
        var client = new EduSphereClient(new HttpClient(api) { BaseAddress = new Uri("https://api.test/") }, new Store(), new AppSettings("https://api.test", "1.2.0", "android"));
        await client.SignInAsync("ali", "pw");
        return (client, api);
    }

    private static object Shared(string summary, string tone, string? category, string at) => new
    {
        id = Guid.NewGuid(), kind = "Behaviour", tone, category, summary, occurredAt = at, authorName = "Mr Siddiqui"
    };

    [Fact]
    public async Task A_student_checks_in_asking_to_talk_and_sees_what_the_school_shared()
    {
        var (client, api) = await SignedInAsync("Student");
        api.On("GET /api/v1/pastoral/mine", HttpStatusCode.OK, new
        {
            checkInsEnabled = true, history = Array.Empty<object>(),
            records = new[] { Shared("Top marks in the test", "Positive", "Outstanding work", "2026-11-27T05:00:00Z") },
            plans = new[] { new { id = Guid.NewGuid(), kind = "LearningAdjustments", title = "Extra time in tests", strategies = "25% extra time.", ownerName = "Mrs Counsellor" } }
        });
        var vm = new SchoolViewModel(client);
        await vm.LoadAsync();

        Assert.True(vm.ShowsWellbeing);
        Assert.True(vm.CheckInsEnabled);
        var note = Assert.Single(vm.WellbeingNotes);
        Assert.True(note.IsPraise);
        Assert.Equal("Outstanding work", note.Heading);
        Assert.Equal("Extra time in tests", Assert.Single(vm.WellbeingPlans).Title);
        Assert.Null(vm.LatestCheckInText);
        vm.Section = SchoolViewModel.WellbeingSection;
        Assert.Equal(SchoolViewModel.WellbeingSection, vm.Section);

        api.On("POST /api/v1/pastoral/check-ins", HttpStatusCode.Created, new { id = Guid.NewGuid(), mood = 2, wantsToTalk = true, checkedInAt = DateTime.UtcNow });
        Assert.True(await vm.CheckInAsync(2, "  Worried about exams  ", true));
        Assert.Equal("""{"mood":2,"note":"Worried about exams","wantsToTalk":true}""", api.Calls.Last(c => c.Key == "POST /api/v1/pastoral/check-ins").Body);
        Assert.Equal(AppText.Get("CheckInTalkSaved"), vm.Notice);
        Assert.Contains(AppText.Get("Mood2"), vm.LatestCheckInText);
    }

    [Fact]
    public async Task Parents_see_each_childs_shared_notes_staff_have_no_section_and_a_school_without_it_still_works()
    {
        var (client, api) = await SignedInAsync("Parent");
        api.On("GET /api/v1/pastoral/family", HttpStatusCode.OK, new object[]
        {
            new { studentProfileId = Guid.NewGuid(), name = "Ali", records = new[] { Shared("Helped a classmate", "Positive", null, "2026-11-20T05:00:00Z") },
                plans = new[] { new { id = Guid.NewGuid(), kind = "LearningAdjustments", title = "Extra time", strategies = "25%.", ownerName = "Mrs Counsellor" } } },
            new { studentProfileId = Guid.NewGuid(), name = "Zoya", records = new[] { Shared("Late three times", "Concern", "Late to class", "2026-11-26T05:00:00Z") },
                plans = Array.Empty<object>() }
        });
        var vm = new SchoolViewModel(client);
        await vm.LoadAsync();
        Assert.Equal(["Zoya", "Ali"], vm.WellbeingNotes.Select(n => n.ChildName));
        Assert.StartsWith("Zoya · Mr Siddiqui", vm.WellbeingNotes[0].Meta);
        Assert.False(vm.WellbeingNotes[0].IsPraise);
        Assert.Equal("Helped a classmate", vm.WellbeingNotes[1].Heading);
        Assert.Equal("Ali: Extra time", Assert.Single(vm.WellbeingPlans).Title);
        Assert.False(vm.CheckInsEnabled);

        // Staff: no wellbeing section and no calls for it.
        var (teacherClient, teacherApi) = await SignedInAsync("Faculty");
        var teacher = new SchoolViewModel(teacherClient);
        await teacher.LoadAsync();
        Assert.False(teacher.ShowsWellbeing);
        teacher.Section = SchoolViewModel.WellbeingSection;
        Assert.Equal(SchoolViewModel.NewsSection, teacher.Section);
        Assert.DoesNotContain(teacherApi.Calls, c => c.Key.Contains("pastoral"));

        // A school without pastoral care: the rest of the tab loads without an error.
        var (otherClient, otherApi) = await SignedInAsync("Parent");
        otherApi.On("GET /api/v1/pastoral/family", HttpStatusCode.Forbidden, new { message = "Module not licensed." });
        var other = new SchoolViewModel(otherClient);
        Assert.True(await RunAsync(other));
        Assert.Null(other.Error);
        Assert.Empty(other.WellbeingNotes);
    }

    private static async Task<bool> RunAsync(SchoolViewModel vm)
    {
        await vm.LoadAsync();
        return vm.Error is null;
    }
}
