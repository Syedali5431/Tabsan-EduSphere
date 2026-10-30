using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Localization;
using Tabsan.EduSphere.Application.Mobile;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Mobile;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Infrastructure.Mobile;
using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Plan M12 — the mobile app's core (API client: sign-in incl. MFA/SSO hand-off, refresh, uploads; version policy; crash
/// reports; app texts; view models) and the server pieces (hand-off with PKCE, devices, crash reports, native push, FCM).
/// </summary>
public class MobileAppTests
{
    // ── fakes ──────────────────────────────────────────────────────────────────────────────────

    private sealed class Store : ISecureStore, IAppStorage
    {
        public readonly Dictionary<string, string> Values = new();
        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { Values[key] = value; return Task.CompletedTask; }
        public void Remove(string key) => Values.Remove(key);
        public Task<string?> ReadAsync(string name) => GetAsync(name);
        public Task WriteAsync(string name, string content) => SetAsync(name, content);
        public void Delete(string name) => Remove(name);
    }

    /// <summary>Answers by "METHOD path" (first match wins, queued answers are used in order) and records every request.</summary>
    private sealed class Api : HttpMessageHandler
    {
        private readonly Dictionary<string, Queue<(HttpStatusCode, string)>> _answers = new();
        public readonly List<(string Key, string? Body, HttpRequestMessage Request)> Calls = new();

        public Api On(string key, HttpStatusCode status, string body)
        {
            (_answers.TryGetValue(key, out var q) ? q : _answers[key] = new()).Enqueue((status, body));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((key, body, request));
            if (!_answers.TryGetValue(key, out var q) || q.Count == 0) return new HttpResponseMessage(HttpStatusCode.NotFound);
            var (status, text) = q.Count > 1 ? q.Dequeue() : q.Peek();
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    private static readonly AppSettings Settings = new("https://api.test", "1.2.0", "android");

    private static string Login(string access = "a1", string refresh = "r1", string role = "Student")
        => JsonSerializer.Serialize(new { accessToken = access, refreshToken = refresh, accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(), username = "ann" });

    private static (EduSphereClient Client, Api Api, Store Store) Client()
    {
        var api = new Api();
        var store = new Store();
        return (new EduSphereClient(new HttpClient(api) { BaseAddress = new Uri("https://api.test/") }, store, Settings) { Language = "ur" }, api, store);
    }

    // ── sign-in ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignIn_AsksForTheAuthenticatorCode_ThenSavesTheSession()
    {
        var (client, api, store) = Client();
        api.On("POST /api/v1/auth/login", HttpStatusCode.BadRequest, """{"error":"MFA_CODE_REQUIRED"}""")
           .On("POST /api/v1/auth/login", HttpStatusCode.OK, Login());
        Assert.Equal(SignInStatus.MfaRequired, (await client.SignInAsync("ann", "pw")).Status);
        var ok = await client.SignInAsync("ann", "pw", " 123456 ");
        Assert.Equal(SignInStatus.Success, ok.Status);
        Assert.Contains("\"mfaCode\":\"123456\"", api.Calls[1].Body);
        Assert.True(client.IsSignedIn);
        Assert.Contains("a1", store.Values["edusphere.session"]);
        // The language and the app version travel with every call.
        Assert.Equal("ur", api.Calls[0].Request.Headers.AcceptLanguage.Single().Value);
        Assert.Equal("1.2.0", api.Calls[0].Request.Headers.GetValues("X-App-Version").Single());

        var restored = new EduSphereClient(new HttpClient(new Api()) { BaseAddress = new Uri("https://api.test/") }, store, Settings);
        Assert.True(await restored.RestoreAsync());
        Assert.Equal("a1", restored.Session!.AccessToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, """{"error":"SSO_REQUIRED","message":"Use SSO"}""", SignInStatus.SsoRequired)]
    [InlineData(HttpStatusCode.Unauthorized, """{"message":"Invalid credentials or account inactive."}""", SignInStatus.InvalidCredentials)]
    [InlineData(HttpStatusCode.Unauthorized, """{"error":"INVALID_MFA_CODE"}""", SignInStatus.InvalidMfaCode)]
    [InlineData((HttpStatusCode)423, """{"message":"blocked"}""", SignInStatus.Blocked)]
    [InlineData(HttpStatusCode.InternalServerError, "", SignInStatus.Failed)]
    public async Task SignIn_TellsWhatWentWrong(HttpStatusCode status, string body, SignInStatus expected)
    {
        var (client, api, _) = Client();
        api.On("POST /api/v1/auth/login", status, body);
        Assert.Equal(expected, (await client.SignInAsync("ann", "pw")).Status);
        Assert.False(client.IsSignedIn);
    }

    [Fact]
    public async Task SingleSignOn_GoesThroughThePortal_AndRedeemsTheCodeWithTheVerifier()
    {
        var pkce = Pkce.Create();
        Assert.Equal(43, pkce.Challenge.Length);
        Assert.Equal(Pkce.Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(pkce.Verifier))), pkce.Challenge);
        var url = EduSphereClient.SingleSignOnUrl("https://portal.test/", "microsoft", pkce, "UNI");
        Assert.Equal($"https://portal.test/Login/Sso/microsoft?appChallenge={pkce.Challenge}&tenant=UNI", url);

        var (client, api, _) = Client();
        api.On("POST /api/v1/mobile/handoff/redeem", HttpStatusCode.OK, Login("sso-a", "sso-r", "Faculty"));
        var result = await client.CompleteSingleSignOnAsync(new Uri("edusphere://sso?code=abc%2Bdef"), pkce, "Pixel 8");
        Assert.Equal(SignInStatus.Success, result.Status);
        using var sent = JsonDocument.Parse(api.Calls.Single().Body!);
        Assert.Equal("abc+def", sent.RootElement.GetProperty("code").GetString());
        Assert.Equal(pkce.Verifier, sent.RootElement.GetProperty("codeVerifier").GetString());
        Assert.Equal("Faculty", client.Session!.Role);
        Assert.Equal(SignInStatus.Failed, (await client.CompleteSingleSignOnAsync(new Uri("edusphere://sso?error=cancelled"), pkce)).Status);
    }

    [Fact]
    public async Task ExpiredTokens_AreRefreshedOnce_AndAFailedRefreshSignsOut()
    {
        var (client, api, store) = Client();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login("old", "r1"));
        await client.SignInAsync("ann", "pw");
        api.On("GET /api/v1/mobile/home", HttpStatusCode.Unauthorized, "")
           .On("GET /api/v1/mobile/home", HttpStatusCode.OK, """{"role":"Student","displayName":"Ann","unreadNotifications":2}""")
           .On("POST /api/v1/auth/refresh", HttpStatusCode.OK, Login("new", "r2"));
        var home = await client.GetHomeAsync();
        Assert.Equal(2, home!.UnreadNotifications);
        Assert.Equal("new", client.Session!.AccessToken);
        Assert.Equal("Bearer new", api.Calls.Last().Request.Headers.Authorization!.ToString());
        Assert.Contains("\"refreshToken\":\"r1\"", api.Calls.Single(c => c.Key == "POST /api/v1/auth/refresh").Body);

        var (other, api2, store2) = Client();
        api2.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login()).On("GET /api/v1/mobile/home", HttpStatusCode.Unauthorized, "")
            .On("POST /api/v1/auth/refresh", HttpStatusCode.Unauthorized, "");
        await other.SignInAsync("ann", "pw");
        var signedOut = false;
        other.SignedOut += (_, _) => signedOut = true;
        await Assert.ThrowsAsync<ApiException>(() => other.GetHomeAsync());
        Assert.True(signedOut);
        Assert.False(other.IsSignedIn);
        Assert.Empty(store2.Values);
    }

    [Fact]
    public async Task ErrorAnswers_BecomeMessages_AndUploadsAreMultipart()
    {
        var (client, api, _) = Client();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login());
        await client.SignInAsync("ann", "pw");
        var id = Guid.NewGuid();
        api.On($"POST /api/v1/assignment/{id}/submissions", HttpStatusCode.OK, "{}");
        await client.SubmitAssignmentAsync(id, "my answer", [new UploadFile("photo.jpg", "image/jpeg", () => Task.FromResult<Stream>(new MemoryStream([1, 2, 3])))]);
        var upload = api.Calls.Last();
        Assert.StartsWith("multipart/form-data", upload.Request.Content!.Headers.ContentType!.MediaType);
        Assert.Contains("name=textContent", upload.Body);
        Assert.Contains("filename=photo.jpg", upload.Body);

        api.On("GET /api/v1/mobile/results", HttpStatusCode.Forbidden, """{"message":"لا يمكنك رؤية نتائج هذا الطالب."}""");
        var error = await Assert.ThrowsAsync<ApiException>(() => client.GetResultsAsync(Guid.NewGuid()));
        Assert.Equal("لا يمكنك رؤية نتائج هذا الطالب.", error.Message);
        Assert.Contains("studentProfileId=", api.Calls.Last().Request.RequestUri!.Query);

        var offering = Guid.NewGuid();
        api.On("POST /api/v1/attendance/bulk", HttpStatusCode.OK, "[]");
        await client.TakeAttendanceAsync(offering, new DateTime(2026, 10, 21, 15, 30, 0), [(Guid.Empty, 1), (Guid.Empty, 2)]);
        using var body = JsonDocument.Parse(api.Calls.Last().Body!);
        Assert.Equal(offering, body.RootElement.GetProperty("courseOfferingId").GetGuid());
        Assert.Equal(2, body.RootElement.GetProperty("entries").GetArrayLength());
        Assert.Equal(2, body.RootElement.GetProperty("entries")[1].GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Discussions_StartWithTheFirstMessage_AndRepliesReloadTheThread()
    {
        var (client, api, _) = Client();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login(role: "Faculty"));
        await client.SignInAsync("ann", "pw");
        var offering = Guid.NewGuid();
        var thread = Guid.NewGuid();
        var threadJson = JsonSerializer.Serialize(new { id = thread, offeringId = offering, title = "Lab moved", authorName = "ann", replyCount = 0, createdAt = DateTime.UtcNow });
        api.On("POST /api/v1/discussion/thread", HttpStatusCode.Created, threadJson)
           .On("POST /api/v1/discussion/reply", HttpStatusCode.OK, "{}")
           .On($"GET /api/v1/discussion/{offering}/threads", HttpStatusCode.OK, "[]");

        var list = new DiscussionsViewModel(client);
        await list.LoadAsync(offering);
        await list.StartAsync();
        Assert.Equal(AppText.Get("TitleAndMessage"), list.Error); // nothing sent without a title and a message
        Assert.DoesNotContain(api.Calls, c => c.Key == "POST /api/v1/discussion/thread");

        DiscussionThread? started = null;
        list.Started += (_, t) => started = t;
        list.Title = " Lab moved ";
        list.Message = "Room B12 on Thursday.";
        await list.StartAsync();
        Assert.Equal(thread, started?.Id);
        Assert.Equal(thread, list.Threads.Single().Id);
        using (var create = JsonDocument.Parse(api.Calls.Single(c => c.Key == "POST /api/v1/discussion/thread").Body!))
        {
            Assert.Equal(offering, create.RootElement.GetProperty("offeringId").GetGuid());
            Assert.Equal("Lab moved", create.RootElement.GetProperty("title").GetString());
        }
        using (var first = JsonDocument.Parse(api.Calls.Last().Body!))
        {
            Assert.Equal(thread, first.RootElement.GetProperty("threadId").GetGuid());
            Assert.Equal("Room B12 on Thursday.", first.RootElement.GetProperty("body").GetString());
        }

        var replies = new[]
        {
            new { id = Guid.NewGuid(), authorName = "bob", body = "Thanks", createdAt = DateTime.UtcNow.AddMinutes(-1) },
            new { id = Guid.NewGuid(), authorName = "ann", body = "Room B12 on Thursday.", createdAt = DateTime.UtcNow.AddMinutes(-5) }
        };
        api.On($"GET /api/v1/discussion/thread/{thread}", HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = thread, offeringId = offering, title = "Lab moved", isClosed = false, replies }));
        var one = new DiscussionViewModel(client);
        await one.LoadAsync(thread);
        Assert.True(one.CanReply);
        Assert.Equal(["ann", "bob"], one.Messages.Select(m => m.AuthorName)); // oldest first
        one.Reply = "See you there";
        await one.ReplyAsync();
        Assert.Equal("", one.Reply);
        Assert.Equal($"GET /api/v1/discussion/thread/{thread}", api.Calls.Last().Key); // reloaded after replying

        var old = Guid.NewGuid();
        api.On($"GET /api/v1/discussion/thread/{old}", HttpStatusCode.OK, JsonSerializer.Serialize(new { id = old, title = "Closed", isClosed = true }));
        var closed = new DiscussionViewModel(client);
        await closed.LoadAsync(old);
        Assert.False(closed.CanReply);
    }

    [Fact]
    public async Task ReportCards_ForAChild_AreShownWithTheirLines()
    {
        var (client, api, _) = Client();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login(role: "Parent"));
        await client.SignInAsync("ann", "pw");
        var child = Guid.NewGuid();
        api.On("GET /api/v1/mobile/report-cards", HttpStatusCode.OK, JsonSerializer.Serialize(new[]
        {
            new { id = Guid.NewGuid(), periodLabel = "Term 1", generatedAt = DateTime.UtcNow, lines = new[] { new { label = "GPA", value = "3.4", isHeading = false } } }
        }));
        var vm = new ReportCardsViewModel(client);
        await vm.LoadAsync(child);
        Assert.Equal("Term 1", vm.Cards.Single().PeriodLabel);
        Assert.Equal(("GPA", "3.4"), (vm.Cards[0].Lines[0].Label, vm.Cards[0].Lines[0].Value));
        Assert.Equal($"?studentProfileId={child}", api.Calls.Last().Request.RequestUri!.Query);
    }

    [Fact]
    public void ReportCardLines_TurnTheStoredDetailsIntoReadableLines()
    {
        var lines = ReportCardLines.From("""
            { "termGPA": 3.456, "position": 4, "promoted": true, "remarks": null,
              "subjects": [ { "subject": "Mathematics", "marks": 88, "grade": "A" }, { "code": "PHY", "total_marks": 70 } ],
              "teacherComments": { "classTeacher": "Works hard." }, "tags": ["Prefect"] }
            """);
        Assert.Contains(lines, l => l is { Label: "Term GPA", Value: "3.46", IsHeading: false });
        Assert.Contains(lines, l => l is { Label: "Promoted", Value: "Yes" });
        Assert.Contains(lines, l => l is { Label: "Remarks", Value: "—" });
        Assert.Contains(lines, l => l is { Label: "Subjects", IsHeading: true });
        Assert.Contains(lines, l => l is { Label: "Mathematics", Value: "Marks: 88 · Grade: A" });
        Assert.Contains(lines, l => l is { Label: "PHY", Value: "Total marks: 70" });
        Assert.Contains(lines, l => l is { Label: "Teacher comments", IsHeading: true });
        Assert.Contains(lines, l => l is { Label: "Class teacher", Value: "Works hard." });
        Assert.Contains(lines, l => l is { Label: "Prefect" });

        Assert.Equal("Details", ReportCardLines.From("not json").Single().Label);
        Assert.Empty(ReportCardLines.From(null));
        Assert.Equal("Mathematics", ReportCardLines.From("""[{"name":"Mathematics","grade":"B"}]""").Single().Label);
        Assert.Equal("CGPA", ReportCardLines.Humanize("CGPA"));
        Assert.Equal("GPA", ReportCardLines.Humanize("gpa"));
        Assert.Equal("CGPA score", ReportCardLines.Humanize("CGPAScore"));
        Assert.Equal("Term 1 marks", ReportCardLines.Humanize("term_1_marks"));
        Assert.True(ReportCardLines.From("{\"rows\":[" + string.Join(",", Enumerable.Range(0, 500).Select(i => $"{{\"name\":\"S{i}\"}}")) + "]}").Count <= 200);
    }

    // ── app pieces ─────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1.2.0", "1.0.0", "1.2.0", VersionPolicy.State.Current)]
    [InlineData("1.2.0", "1.0.0", "1.3.0", VersionPolicy.State.UpdateAvailable)]
    [InlineData("1.2.0", "2.0", "2.1.0", VersionPolicy.State.UpdateRequired)]
    [InlineData("v1.4.1-beta", "1.4.0", null, VersionPolicy.State.Current)]
    public void VersionPolicy_FollowsTheApi(string app, string? minimum, string? latest, VersionPolicy.State expected)
        => Assert.Equal(expected, VersionPolicy.Check(app, minimum, latest));

    [Fact]
    public async Task CrashReports_AreKeptOnTheDevice_AndSentAfterSignIn()
    {
        var store = new Store();
        var reporter = new CrashReporter(store, Settings, "Android 14", "Pixel 8") { CurrentScreen = "Courses" };
        try { throw new InvalidOperationException("boom"); }
        catch (Exception ex) { await reporter.RecordAsync(ex); }
        Assert.Contains("boom", store.Values["pending-crash.json"]);

        var (client, api, _) = Client();
        Assert.False(await reporter.SendPendingAsync(client)); // not signed in: kept
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login()).On("POST /api/v1/mobile/crash-reports", HttpStatusCode.OK, "{}");
        await client.SignInAsync("ann", "pw");
        Assert.True(await reporter.SendPendingAsync(client));
        Assert.False(store.Values.ContainsKey("pending-crash.json"));
        using var sent = JsonDocument.Parse(api.Calls.Last().Body!);
        Assert.Equal("System.InvalidOperationException", sent.RootElement.GetProperty("exceptionType").GetString());
        Assert.Equal("Courses", sent.RootElement.GetProperty("screen").GetString());
    }

    [Fact]
    public void AppTexts_AreTranslatedIntoUrduAndArabic_WithTheSamePlaceholders()
    {
        foreach (var key in AppText.Keys)
        {
            var en = AppText.Get(key, "en");
            foreach (var lang in new[] { "ur", "ar" })
            {
                var text = AppText.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(text), $"{lang}:{key}");
                Assert.Equal(en.Contains("{0}"), text.Contains("{0}"));
            }
        }
        Assert.Equal("Unknown", AppText.Get("Unknown"));
    }

    [Fact]
    public async Task SignInViewModel_HandlesMfa_UpdateRequired_AndSignsIn()
    {
        var (client, api, _) = Client();
        api.On("GET /api/v1/mobile/config", HttpStatusCode.OK, """{"institutionName":"Uni","minimumVersion":"1.0.0","latestVersion":"1.5.0","ssoProviders":[{"provider":"microsoft","displayName":"Microsoft"}]}""")
           .On("POST /api/v1/auth/login", HttpStatusCode.BadRequest, """{"error":"MFA_CODE_REQUIRED"}""")
           .On("POST /api/v1/auth/login", HttpStatusCode.OK, Login());
        var vm = new SignInViewModel(client, new NoBrowser(), Settings) { Username = "ann", Password = "pw" };
        Session? signedIn = null;
        vm.SignedIn += (_, s) => signedIn = s;
        await vm.LoadAsync();
        Assert.Equal(VersionPolicy.State.UpdateAvailable, vm.VersionState);
        Assert.Single(vm.Providers);
        await vm.SignInAsync();
        Assert.True(vm.NeedsMfa);
        vm.MfaCode = "654321";
        await vm.SignInAsync();
        Assert.NotNull(signedIn);
        Assert.Equal("", vm.Password);

        var (old, api2, _) = Client();
        api2.On("GET /api/v1/mobile/config", HttpStatusCode.OK, """{"minimumVersion":"9.0.0"}""");
        var outdated = new SignInViewModel(old, new NoBrowser(), Settings) { Username = "ann", Password = "pw" };
        await outdated.LoadAsync();
        await outdated.SignInAsync();
        Assert.Equal(AppText.Get("UpdateRequired"), outdated.Error);
        Assert.DoesNotContain(api2.Calls, c => c.Key.Contains("login"));
    }

    private sealed class NoBrowser : IBrowserAuthenticator
    {
        public Task<Uri?> AuthenticateAsync(Uri start, Uri callback) => Task.FromResult<Uri?>(null);
    }

    [Fact]
    public void Timetable_StartsToday_AndMarksCycle()
    {
        var slots = new[]
        {
            new ClassSlot { DayOfWeek = 1, Day = "Monday", Start = "10:00", Subject = "B" },
            new ClassSlot { DayOfWeek = 1, Day = "Monday", Start = "09:00", Subject = "A" },
            new ClassSlot { DayOfWeek = 3, Day = "Wednesday", Start = "08:00", Subject = "C" }
        };
        var days = TimetableViewModel.Group(slots, today: 3);
        Assert.Equal(["Wednesday", "Monday"], days.Select(d => d.Day));
        Assert.True(days[0].IsToday);
        Assert.Equal(["A", "B"], days[1].Classes.Select(c => c.Subject));

        var mark = new AttendanceMark(Guid.NewGuid(), "Ann", "R1");
        Assert.Equal(1, mark.Status);
        mark.Next(); mark.Next(); mark.Next();
        Assert.Equal(4, mark.Status);
        mark.Next();
        Assert.Equal(1, mark.Status);
        Assert.Equal("Overdue", HomeViewModel.DueLabel(new DueItem { DueAt = DateTime.UtcNow.AddHours(-1) }, DateTime.UtcNow));
    }

    // ── what School users see (checked against the School-only demo data) ─────────────────────

    [Fact]
    public async Task Home_IsForThePhonesDay_NotTheServersUtcDay()
    {
        var (client, api, _) = Client();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login())
           .On("GET /api/v1/mobile/home", HttpStatusCode.OK, """{"role":"Student","student":{"averagePercent":69.5}}""");
        await client.SignInAsync("ann", "pw");
        var home = await client.GetHomeAsync(DayOfWeek.Tuesday);
        Assert.Equal("?day=2", api.Calls.Last().Request.RequestUri!.Query);
        Assert.Null(home!.Student!.Cgpa);
        Assert.Equal(69.5m, home.Student.AveragePercent);

        await new HomeViewModel(client).LoadAsync();
        Assert.Equal($"?day={(int)DateTime.Now.DayOfWeek}", api.Calls.Last().Request.RequestUri!.Query);
    }

    [Fact]
    public async Task ImportedAccounts_ChooseTheirOwnPassword_BeforeTheAppOpens()
    {
        var (client, api, store) = Client();
        var temporary = JsonSerializer.Serialize(new
        {
            accessToken = "t1", refreshToken = "r1", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role = "Parent", userId = Guid.NewGuid(),
            username = "sadia", mustChangePassword = true
        });
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, temporary)
           .On("POST /api/v1/auth/login", HttpStatusCode.OK, Login("a2", "r2", "Parent"))
           .On("POST /api/v1/auth/force-change-password", HttpStatusCode.NoContent, "");
        var vm = new SignInViewModel(client, new NoBrowser(), Settings) { Username = "sadia", Password = "sadia" };
        Session? signedIn = null;
        vm.SignedIn += (_, s) => signedIn = s;

        await vm.SignInAsync();
        Assert.True(vm.NeedsNewPassword);
        Assert.Null(signedIn);
        Assert.Empty(store.Values); // nothing is kept on the phone while the password is still the temporary one

        vm.NewPassword = "Garden#Path42";
        vm.ConfirmPassword = "Garden#Path24";
        await vm.SignInAsync();
        Assert.Equal(AppText.Get("PasswordsDiffer"), vm.Error);
        Assert.DoesNotContain(api.Calls, c => c.Key.Contains("force-change"));

        vm.ConfirmPassword = "Garden#Path42";
        await vm.SignInAsync();
        var change = api.Calls.Single(c => c.Key == "POST /api/v1/auth/force-change-password");
        Assert.Contains("\"currentPassword\":\"sadia\"", change.Body);
        Assert.Equal("Bearer t1", change.Request.Headers.Authorization!.ToString());
        Assert.Contains("\"password\":\"Garden#Path42\"", api.Calls.Last(c => c.Key == "POST /api/v1/auth/login").Body);
        Assert.Equal("a2", signedIn!.AccessToken);
        Assert.False(vm.NeedsNewPassword);
        Assert.NotEmpty(store.Values);
    }

    [Fact]
    public async Task Grading_NamesStudentsFromTheRoster_AndShowsTheirAnswers()
    {
        var (client, api, _) = Client();
        var (offering, assignment, hira, bilal) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login(role: "Faculty"))
           .On($"GET /api/v1/assignment/{assignment}/submissions", HttpStatusCode.OK,
               $$"""[{"submissionId":"{{Guid.NewGuid()}}","studentProfileId":"{{hira}}","textContent":"Q1 = 12","marksAwarded":18},{"submissionId":"{{Guid.NewGuid()}}","studentProfileId":"{{bilal}}","textContent":"Q1 = 11"}]""")
           .On($"GET /api/v1/enrollment/roster/{offering}", HttpStatusCode.OK,
               $$"""[{"studentProfileId":"{{hira}}","studentName":"Hira Qureshi","registrationNumber":"TMS-GUL-09-03"},{"studentProfileId":"{{bilal}}","studentName":"Bilal Raza"}]""");
        await client.SignInAsync("ali", "pw");
        var vm = new GradingViewModel(client);
        await vm.LoadAsync(new CourseAssignment { Id = assignment, MaxMarks = 20 }, offering);
        Assert.Equal(["Bilal Raza", "Hira Qureshi"], vm.Submissions.Select(s => s.Student)); // work to grade first
        Assert.Equal(["Q1 = 11", "Q1 = 12"], vm.Submissions.Select(s => s.Answer));
    }

    [Fact]
    public async Task ChildAnnouncements_ComeFromTheMobileApi()
    {
        var (client, api, _) = Client();
        var child = Guid.NewGuid();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login(role: "Parent"))
           .On("GET /api/v1/mobile/announcements", HttpStatusCode.OK, """[{"title":"Sports day","body":"Friday","courseTitle":"Class 3"}]""");
        await client.SignInAsync("sadia", "pw");
        var items = await client.GetChildAnnouncementsAsync(child);
        Assert.Equal("Sports day", items.Single().Title);
        Assert.Equal($"?studentProfileId={child}", api.Calls.Last().Request.RequestUri!.Query);
    }

    [Fact]
    public async Task Notifications_ShowWhenTheyWereSent()
    {
        // The inbox API sends "sentAt"; the redesigned list shows it under each notification (it showed 1 Jan 0001).
        var (client, api, _) = Client();
        api.On("POST /api/v1/auth/login", HttpStatusCode.OK, Login())
           .On("GET /api/v1/notification/inbox", HttpStatusCode.OK,
               """[{"notificationId":"6f1c0a6e-1111-4d5e-9e0e-0a1b2c3d4e5f","title":"Results published","body":"Midterm","sentAt":"2026-10-30T10:04:00Z","isRead":false}]""");
        await client.SignInAsync("ann", "pw");
        var item = (await client.GetNotificationsAsync()).Single();
        Assert.Equal(new DateTime(2026, 10, 30, 10, 4, 0, DateTimeKind.Utc), item.When.ToUniversalTime());
    }

    [Fact]
    public void SchoolStudents_SeeTheirAverage_UniversityStudentsTheirCgpa()
    {
        Assert.Equal(("CGPA", 3.4m.ToString("0.00")), HomeViewModel.Standing(3.4m, 81m));
        Assert.Equal(("Average", $"{69.5m:0.#}%"), HomeViewModel.Standing(null, 69.5m));
        Assert.Equal(("Average", "—"), HomeViewModel.Standing(null, null));
    }

    [Fact]
    public void TimetableDays_AreNamedInTheAppsLanguage()
    {
        var slots = new[] { new ClassSlot { DayOfWeek = 2, Day = "Tuesday", Start = "08:00", Subject = "Urdu" } };
        Assert.Equal("منگل", TimetableViewModel.Group(slots, today: 2, language: "ur").Single().Day);
        Assert.Equal("الثلاثاء", TimetableViewModel.Group(slots, today: 2, language: "ar").Single().Day);
        Assert.Equal("Tuesday", TimetableViewModel.Group(slots, today: 2, language: "en").Single().Day);
        Assert.Equal("Odd", TimetableViewModel.Group([new ClassSlot { DayOfWeek = 9, Day = "Odd" }], today: 2, language: "ur").Single().Day);
    }

    [Fact]
    public void Teachers_SeeWhatIsLeftToGrade_AndLiveClassesAreNeverOverdue()
    {
        Assert.Equal("3 to grade", CourseViewModel.AssignmentState(new CourseAssignment { ToGrade = 3 }, isTeacher: true));
        Assert.Equal("Nothing to grade", CourseViewModel.AssignmentState(new CourseAssignment { ToGrade = 0 }, isTeacher: true));
        Assert.Equal("To grade", CourseViewModel.AssignmentState(new CourseAssignment(), isTeacher: true)); // an API without the count
        Assert.Equal("Not graded yet", CourseViewModel.AssignmentState(new CourseAssignment { Submitted = true, ToGrade = 0 }, isTeacher: false));
        Assert.Equal("Hand in", CourseViewModel.AssignmentState(new CourseAssignment(), isTeacher: false));

        Assert.Equal("TMS-GUL-MTH09 · Room 9 · Main Block", new ClassSlot { CourseCode = "TMS-GUL-MTH09", Room = "Room 9", Building = "Main Block" }.Details);
        var now = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);
        Assert.Contains("2027", HomeViewModel.ShortDate(new DateTime(2027, 10, 26, 13, 1, 0, DateTimeKind.Utc), now)); // not "Wed 27 Oct"
        Assert.DoesNotContain("2026", HomeViewModel.ShortDate(new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc), now));

        var started = new DueItem { Kind = "LiveClass", DueAt = DateTime.UtcNow.AddMinutes(-20) };
        Assert.Equal(started.DueAt.ToLocalTime().ToString("ddd d MMM HH:mm"), HomeViewModel.DueLabel(started, DateTime.UtcNow));
        Assert.Equal("Overdue", HomeViewModel.DueLabel(new DueItem { Kind = "Assignment", DueAt = started.DueAt }, DateTime.UtcNow));
    }

    // ── server: hand-off, devices, crash reports, push ─────────────────────────────────────────

    private sealed class Repo : IMobileRepository
    {
        public readonly List<MobileDevice> Devices = new();
        public readonly List<MobileCrashReport> Crashes = new();
        public HashSet<Guid> OptedOut { get; } = new();
        public Task<MobileDevice?> GetDeviceByTokenAsync(string token, CancellationToken ct = default) => Task.FromResult(Devices.FirstOrDefault(d => d.Token == token));
        public Task<IReadOnlyList<MobileDevice>> GetDevicesAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<MobileDevice>>(Devices.Where(d => d.UserId == userId).ToList());
        public Task<IReadOnlyList<MobileDevice>> GetDeliverableAsync(IReadOnlyCollection<Guid> userIds, NotificationType type, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MobileDevice>>(Devices.Where(d => d.IsActive && userIds.Contains(d.UserId) && !OptedOut.Contains(d.UserId)).ToList());
        public void Add(MobileDevice device) => Devices.Add(device);
        public void Remove(MobileDevice device) => Devices.Remove(device);
        public void Add(MobileCrashReport report) => Crashes.Add(report);
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class Auth : IAuthService
    {
        public readonly List<Guid> ExternalLogins = new();
        public Task<LoginResult> ExternalLoginAsync(Guid userId, string provider, string? mfaCode, bool mfaSatisfiedExternally, string? ipAddress, string? deviceInfo, CancellationToken ct = default)
        {
            ExternalLogins.Add(userId);
            return Task.FromResult(LoginResult.Ok(new LoginResponse("a", "r", DateTime.UtcNow.AddMinutes(15), "Student", userId, "ann")));
        }
        public Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AuthSecurityProfileResponse> GetSecurityProfileAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<LoginResponse?> RefreshAsync(string rawRefreshToken, string? ipAddress, CancellationToken ct = default) => throw new NotSupportedException();
        public Task LogoutAsync(string rawRefreshToken, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> ForceChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MfaSetupResponse?> BeginMfaSetupAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> EnableMfaAsync(Guid userId, EnableMfaRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MfaRecoveryCodesResponse?> RegenerateRecoveryCodesAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class Branding : IPortalBrandingService
    {
        public Task<PortalBrandingDto> GetAsync(CancellationToken ct = default) => Task.FromResult(new PortalBrandingDto("City University", "Campus Portal", "©", "/logo.png", null, null, null, null));
        public Task SaveAsync(SavePortalBrandingCommand cmd, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class Languages : IPortalLanguageService
    {
        public Task<string> GetDefaultAsync(CancellationToken ct = default) => Task.FromResult("ur");
        public Task SetDefaultAsync(string language, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string> ForRecipientAsync(string? preferredLanguage, CancellationToken ct = default) => Task.FromResult(preferredLanguage ?? "ur");
    }

    private sealed class Push(params NativePushResult[] results) : INativePushSender
    {
        private readonly Queue<NativePushResult> _results = new(results);
        public readonly List<(string Token, NativePushMessage Message)> Sent = new();
        public bool IsConfigured => true;
        public Task<NativePushResult> SendAsync(string token, NativePushMessage message, CancellationToken ct = default)
        {
            Sent.Add((token, message));
            return Task.FromResult(_results.Count > 0 ? _results.Dequeue() : NativePushResult.Delivered);
        }
    }

    private static (MobileService Service, Repo Repo, Auth Auth) Service(Push? push = null, bool fcm = true)
    {
        var repo = new Repo();
        var auth = new Auth();
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var options = Options.Create(new MobileOptions { MinimumVersion = "1.1.0", LatestVersion = "1.3.0", PortalBaseUrl = "https://portal.test/", Fcm = new FcmOptions { Enabled = fcm } });
        return (new MobileService(repo, cache, auth, new Branding(), new Languages(), push ?? new Push(), options), repo, auth);
    }

    [Fact]
    public async Task Config_CarriesBrandingVersionsAndLanguages()
    {
        var (service, _, _) = Service();
        var config = await service.GetConfigAsync(null);
        Assert.Equal("City University", config.InstitutionName);
        Assert.Equal("ur", config.DefaultLanguage);
        Assert.Equal(["en", "ur", "ar"], config.Languages.Select(l => l.Code));
        Assert.Equal("https://portal.test", config.PortalBaseUrl);
        Assert.True(config.PushEnabled);
        Assert.True(service.IsSupportedVersion("1.1.0"));
        Assert.True(service.IsSupportedVersion("v2"));
        Assert.False(service.IsSupportedVersion("1.0.9"));
        Assert.False(service.IsSupportedVersion("garbage"));
    }

    [Fact]
    public async Task Handoff_IsSingleUse_AndBoundToTheAppsVerifier()
    {
        var (service, _, auth) = Service();
        var user = Guid.NewGuid();
        var pkce = Pkce.Create();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateHandoffAsync(user, new HandoffRequest("short")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateHandoffAsync(Guid.Empty, new HandoffRequest(pkce.Challenge)));

        var handoff = await service.CreateHandoffAsync(user, new HandoffRequest(pkce.Challenge));
        Assert.Equal(120, handoff.ExpiresInSeconds);
        var stolen = Pkce.Create();
        Assert.Null(await service.RedeemHandoffAsync(new HandoffRedeemRequest(handoff.Code, stolen.Verifier, null), null)); // wrong verifier…
        Assert.Null(await service.RedeemHandoffAsync(new HandoffRedeemRequest(handoff.Code, pkce.Verifier, null), null)); // …and the code is gone

        var second = await service.CreateHandoffAsync(user, new HandoffRequest(pkce.Challenge));
        var login = await service.RedeemHandoffAsync(new HandoffRedeemRequest(second.Code, pkce.Verifier, "Pixel"), "10.0.0.1");
        Assert.True(login!.IsSuccess);
        Assert.Equal([user], auth.ExternalLogins);
        Assert.Null(await service.RedeemHandoffAsync(new HandoffRedeemRequest(second.Code, pkce.Verifier, null), null));
        Assert.Null(await service.RedeemHandoffAsync(new HandoffRedeemRequest("nope", pkce.Verifier, null), null));
    }

    [Fact]
    public async Task Devices_FollowTheSignedInUser_AndCrashReportsAreKept()
    {
        var (service, repo, _) = Service();
        var (ann, ben) = (Guid.NewGuid(), Guid.NewGuid());
        await service.RegisterDeviceAsync(ann, new RegisterDeviceRequest("android", "tok-1", "1.2.0", "Pixel", "ur-PK"));
        await service.RegisterDeviceAsync(ben, new RegisterDeviceRequest("Android", "tok-1", "1.2.1", "Pixel", "ar"));
        var device = Assert.Single(repo.Devices);
        Assert.Equal((ben, "1.2.1", "ar"), (device.UserId, device.AppVersion, device.Language));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterDeviceAsync(ann, new RegisterDeviceRequest("blackberry", "tok-2", null, null, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterDeviceAsync(ann, new RegisterDeviceRequest("ios", "has space", null, null, null)));
        Assert.False(await service.UnregisterDeviceAsync(ann, "tok-1")); // not Ann's any more
        Assert.True(await service.UnregisterDeviceAsync(ben, "tok-1"));
        Assert.Empty(repo.Devices);

        await service.ReportCrashAsync(ann, new CrashReportRequest("ios", "1.2.0", "17.4", "iPhone", "NullReferenceException", "boom", new string('x', 20_000), "Home", DateTime.UtcNow.AddYears(1)));
        var crash = Assert.Single(repo.Crashes);
        Assert.Equal(MobileCrashReport.MaxStackTraceLength, crash.StackTrace.Length);
        Assert.True(crash.OccurredAt <= DateTime.UtcNow); // a future time from a wrong phone clock is not trusted
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportCrashAsync(ann, new CrashReportRequest("ios", "1", null, null, "", "", "", null, DateTime.UtcNow)));
    }

    [Fact]
    public async Task NativePush_RespectsChoices_RemovesDeadTokens_AndStopsAfterFailures()
    {
        var push = new Push(NativePushResult.Delivered, NativePushResult.Gone, NativePushResult.Failed);
        var (service, repo, _) = Service(push);
        var (a, b, c, quiet) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        foreach (var (user, token) in new[] { (a, "t-a"), (b, "t-b"), (c, "t-c"), (quiet, "t-q") })
            await service.RegisterDeviceAsync(user, new RegisterDeviceRequest("android", token, "1.2.0", null, "en"));
        repo.OptedOut.Add(quiet);
        var delivered = await service.SendPushAsync(NotificationType.Assignment, "New assignment", "Essay due Friday", [a, b, c, quiet]);
        Assert.Equal(1, delivered);
        Assert.Equal(3, push.Sent.Count);
        Assert.DoesNotContain(push.Sent, s => s.Token == "t-q");
        Assert.Equal("assignments", push.Sent[0].Message.Data["screen"]);
        Assert.DoesNotContain(repo.Devices, d => d.Token == "t-b"); // unregistered token removed
        Assert.Equal(1, repo.Devices.Single(d => d.Token == "t-c").ConsecutiveFailures);
        for (var i = 0; i < 4; i++) repo.Devices.Single(d => d.Token == "t-c").RecordFailure(5);
        Assert.False(repo.Devices.Single(d => d.Token == "t-c").IsActive);

        var (off, _, _) = Service(new Push(), fcm: false);
        Assert.Equal(0, await off.SendPushAsync(NotificationType.General, "t", "b", [a]));
        Assert.EndsWith("…", MobileService.BuildMessage(NotificationType.General, new string('T', 200), "b").Title);
    }

    // ── FCM ────────────────────────────────────────────────────────────────────────────────────

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static string ServiceAccount(RSA rsa) => JsonSerializer.Serialize(new
    {
        type = "service_account", project_id = "edusphere-test", private_key_id = "k1", client_email = "push@edusphere-test.iam.gserviceaccount.com",
        private_key = rsa.ExportPkcs8PrivateKeyPem(), token_uri = "https://oauth2.googleapis.com/token"
    });

    [Fact]
    public void FcmAssertion_IsAGoogleJwtSignedWithTheServiceAccountKey()
    {
        using var rsa = RSA.Create(2048);
        var account = FcmPushSender.ParseAccount(ServiceAccount(rsa))!;
        var jwt = FcmPushSender.CreateAssertion(account, DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
        var parts = jwt.Split('.');
        static byte[] B64(string s) => Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + new string('=', (4 - s.Length % 4) % 4));
        Assert.True(rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), B64(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        using var claims = JsonDocument.Parse(B64(parts[1]));
        Assert.Equal("push@edusphere-test.iam.gserviceaccount.com", claims.RootElement.GetProperty("iss").GetString());
        Assert.Equal("https://www.googleapis.com/auth/firebase.messaging", claims.RootElement.GetProperty("scope").GetString());
        Assert.Equal(1_800_003_600, claims.RootElement.GetProperty("exp").GetInt64());
        Assert.Null(FcmPushSender.ParseAccount("""{"client_email":"x","private_key":"y","token_uri":"http://evil.test/token"}"""));
    }

    private sealed class Google : HttpMessageHandler
    {
        public readonly List<(string Url, string Body, string? Auth)> Calls = new();
        public HttpStatusCode SendStatus { get; set; } = HttpStatusCode.OK;
        public string SendBody { get; set; } = """{"name":"projects/x/messages/1"}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request.RequestUri!.ToString(), body, request.Headers.Authorization?.ToString()));
            return request.RequestUri!.Host == "oauth2.googleapis.com"
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"ya29.token","expires_in":3600}""") }
                : new HttpResponseMessage(SendStatus) { Content = new StringContent(SendBody) };
        }
    }

    [Fact]
    public async Task Fcm_SendsV1Messages_CachesTheToken_AndReportsDeadTokens()
    {
        using var rsa = RSA.Create(2048);
        var google = new Google();
        var sender = new FcmPushSender(new Factory(google), Options.Create(new MobileOptions { Fcm = new FcmOptions { Enabled = true, ServiceAccountJson = ServiceAccount(rsa) } }));
        Assert.True(sender.IsConfigured);
        var message = MobileService.BuildMessage(NotificationType.Result, "Results published", "Your Fall 2026 results are out.");
        Assert.Equal(NativePushResult.Delivered, await sender.SendAsync("device-token", message));
        Assert.Equal(NativePushResult.Delivered, await sender.SendAsync("device-token-2", message));
        Assert.Single(google.Calls, c => c.Url.StartsWith("https://oauth2.googleapis.com"));
        var send = google.Calls.Last();
        Assert.Equal("https://fcm.googleapis.com/v1/projects/edusphere-test/messages:send", send.Url);
        Assert.Equal("Bearer ya29.token", send.Auth);
        using var doc = JsonDocument.Parse(send.Body);
        var msg = doc.RootElement.GetProperty("message");
        Assert.Equal("device-token-2", msg.GetProperty("token").GetString());
        Assert.Equal("results", msg.GetProperty("data").GetProperty("screen").GetString());
        Assert.Equal("high", msg.GetProperty("android").GetProperty("priority").GetString());

        google.SendStatus = HttpStatusCode.NotFound;
        google.SendBody = """{"error":{"status":"NOT_FOUND","details":[{"errorCode":"UNREGISTERED"}]}}""";
        Assert.Equal(NativePushResult.Gone, await sender.SendAsync("old-token", message));
        google.SendStatus = HttpStatusCode.ServiceUnavailable;
        google.SendBody = "{}";
        Assert.Equal(NativePushResult.Failed, await sender.SendAsync("device-token", message));

        Assert.False(new FcmPushSender(new Factory(google), Options.Create(new MobileOptions())).IsConfigured);
    }
}
