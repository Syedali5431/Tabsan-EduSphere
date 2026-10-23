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

/// <summary>Plan M10 — live classes page, scheduling forms, join redirect, dashboard card, calendar file, attendance and settings, against a fake API.</summary>
public class LiveClassesWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string OfferingId = "44444444-0000-0000-0000-000000000001";
    private const string LiveId = "a1000000-0000-0000-0000-0000000000a1";
    private const string LaterId = "a1000000-0000-0000-0000-0000000000a2";
    private const string EntryId = "e1000000-0000-0000-0000-0000000000e1";
    private const string StudentId = "d1000000-0000-0000-0000-0000000000d1";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public LiveClassesWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Teacher_SeesClasses_AndFormsSendUtcTimesToTheApi()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/LiveClasses?offeringId={OfferingId}");
        Assert.Contains("data-session=\"Week 6 lab\"", html);
        Assert.Contains("aria-label=\"Join Week 6 lab\"", html);
        Assert.Contains("data-schedule-form", html);
        Assert.Contains($"<option value=\"{EntryId}\">Wednesday 10:30", html);
        Assert.Contains("name=\"autoAttendance\" value=\"false\"", html);

        var token = Token(html);
        await client.PostAsync("/Portal/ScheduleLiveSession", Form(token, ("offeringId", OfferingId), ("title", "Revision"), ("startsAt", "2026-11-03T14:00"),
            ("durationMinutes", "45"), ("autoAttendance", "true"), ("autoAttendance", "false"), ("minimumMinutes", "10"), ("meetingUrl", "")));
        using (var doc = JsonDocument.Parse(api.Body("POST", $"/api/v1/live/offerings/{OfferingId}/sessions")))
        {
            Assert.Equal("2026-11-03T14:00:00Z", doc.RootElement.GetProperty("startsAt").GetString());
            Assert.True(doc.RootElement.GetProperty("autoAttendance").GetBoolean());
            Assert.Equal(45, doc.RootElement.GetProperty("durationMinutes").GetInt32());
        }
        await client.PostAsync("/Portal/ScheduleLiveFromTimetable", Form(token, ("offeringId", OfferingId), ("entryId", EntryId), ("weeks", "4"),
            ("autoAttendance", "false"), ("minimumMinutes", "0"), ("meetingUrl", "https://meet.example.test/x")));
        Assert.Contains($"\"entryId\":\"{EntryId}\"", api.Body("POST", $"/api/v1/live/offerings/{OfferingId}/sessions/from-timetable"));
        Assert.Contains("\"weeks\":4", api.Body("POST", $"/api/v1/live/offerings/{OfferingId}/sessions/from-timetable"));

        var edit = await client.GetStringAsync($"/Portal/LiveClasses?offeringId={OfferingId}&edit={LaterId}");
        Assert.Contains("value=\"2026-11-10T09:00\"", edit);
        await client.PostAsync("/Portal/UpdateLiveSession", Form(Token(edit), ("sessionId", LaterId), ("offeringId", OfferingId), ("title", "Week 7"),
            ("startsAt", "2026-11-11T09:00"), ("durationMinutes", "60"), ("autoAttendance", "false"), ("minimumMinutes", "0")));
        Assert.Contains("\"startsAt\":\"2026-11-11T09:00:00Z\"", api.Body("PUT", $"/api/v1/live/sessions/{LaterId}"));
        await client.PostAsync("/Portal/CancelLiveSession", Form(token, ("sessionId", LaterId), ("offeringId", OfferingId)));
        Assert.Contains(api.Writes, w => w == $"POST /api/v1/live/sessions/{LaterId}/cancel");

        var attendance = await client.GetStringAsync($"/Portal/LiveSessionAttendance/{LiveId}?offeringId={OfferingId}");
        Assert.Contains("data-student=\"Ann Lee\"", attendance);
        Assert.Contains(">Present</span>", attendance);
        await client.PostAsync("/Portal/TakeLiveAttendance", Form(Token(attendance), ("sessionId", LiveId), ("offeringId", OfferingId)));
        Assert.Contains(api.Writes, w => w == $"POST /api/v1/live/sessions/{LiveId}/attendance");
    }

    [Fact]
    public async Task Join_RedirectsOnlyToWebLinks_AndDashboardShowsTheCard()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/LiveClasses?offeringId={OfferingId}");
        Assert.DoesNotContain("data-schedule-form", html); // students only join
        Assert.Contains("data-live-banner", html); // the class open now is offered on every page
        Assert.Contains("aria-label=\"Join Week 6 lab now\"", html);
        Assert.DoesNotContain("Join Week 7 now", html); // not open yet
        var token = Token(html);

        var join = await client.PostAsync("/Portal/JoinLiveSession", Form(token, ("sessionId", LiveId), ("returnUrl", "/Portal/Dashboard")));
        Assert.Equal(HttpStatusCode.Redirect, join.StatusCode);
        Assert.Equal("https://meet.example.test/j/1", join.Headers.Location!.ToString());

        api.JoinUrl = "javascript:alert(1)";
        var unsafeJoin = await client.PostAsync("/Portal/JoinLiveSession", Form(token, ("sessionId", LiveId), ("returnUrl", "https://evil.example/")));
        Assert.Equal("/Portal/LiveClasses", unsafeJoin.Headers.Location!.ToString()); // neither the script link nor the foreign return URL

        api.JoinStatus = HttpStatusCode.Conflict;
        var refused = await client.PostAsync("/Portal/JoinLiveSession", Form(token, ("sessionId", LiveId), ("returnUrl", "/Portal/Dashboard")));
        Assert.Equal("/Portal/Dashboard", refused.Headers.Location!.ToString());

        var ics = await client.GetAsync($"/Portal/LiveSessionCalendar?sessionId={LiveId}");
        Assert.Equal("text/calendar", ics.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Settings_SaveWithoutEchoingTheSecret()
    {
        var api = new FakeApi("SuperAdmin");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/LiveClassSettings");
        Assert.Contains("data-conference-settings", html);
        Assert.Contains("<option value=\"BigBlueButton\" selected=\"selected\">", html);
        Assert.Contains("Saved — leave empty to keep", WebUtility.HtmlDecode(html));
        var dashboard = await client.GetStringAsync("/Portal/Dashboard"); // the SuperAdmin dashboard lists the classes
        Assert.Contains("data-upcoming-live", dashboard);
        Assert.Contains("data-upcoming-session=\"Week 7\"", dashboard);
        await client.PostAsync("/Portal/SaveLiveClassSettings", Form(Token(html), ("provider", "Zoom"), ("isEnabled", "true"), ("isEnabled", "false"),
            ("baseUrl", ""), ("secret", "zs"), ("accountId", "acct"), ("clientId", "cid"), ("organizerUserId", ""), ("joinEarlyMinutes", "10"), ("installationDefault", "false")));
        using var doc = JsonDocument.Parse(api.Body("PUT", "/api/v1/live/settings"));
        Assert.Equal(("Zoom", "zs", 10), (doc.RootElement.GetProperty("provider").GetString(), doc.RootElement.GetProperty("secret").GetString(), doc.RootElement.GetProperty("joinEarlyMinutes").GetInt32()));
    }

    [Fact]
    public async Task Arabic_PageIsRightToLeft_AndFormsStillSendUtcTimes()
    {
        // Plan M11 — dates typed in an Arabic page must mean the same as in English (Gregorian calendar, "." decimals).
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync($"/Portal/LiveClasses?offeringId={OfferingId}");
        await client.PostAsync("/Language/Set", Form(Token(html), ("lang", "ar"), ("returnUrl", "/")));
        var arabic = await client.GetStringAsync($"/Portal/LiveClasses?offeringId={OfferingId}");
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", arabic);
        Assert.Contains("الحصص المباشرة", arabic);
        Assert.Contains("data-session=\"Week 6 lab\"", arabic); // data attributes stay as they are
        await client.PostAsync("/Portal/ScheduleLiveSession", Form(Token(arabic), ("offeringId", OfferingId), ("title", "مراجعة"), ("startsAt", "2026-11-03T14:00"),
            ("durationMinutes", "45"), ("autoAttendance", "false"), ("minimumMinutes", "0"), ("meetingUrl", "")));
        using var doc = JsonDocument.Parse(api.Body("POST", $"/api/v1/live/offerings/{OfferingId}/sessions"));
        Assert.Equal("2026-11-03T14:00:00Z", doc.RootElement.GetProperty("startsAt").GetString());
        Assert.Equal("مراجعة", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(45, doc.RootElement.GetProperty("durationMinutes").GetInt32());
    }

    // ── helpers ──

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }.Concat(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))));

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var page = await client.GetStringAsync("/Login");
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(page), ["username"] = "tester", ["password"] = "Password#123"
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
        private readonly List<(string Method, string Path, string Body)> _writes = new();
        public string JoinUrl { get; set; } = "https://meet.example.test/j/1";
        public HttpStatusCode JoinStatus { get; set; } = HttpStatusCode.OK;

        public IEnumerable<string> Writes => _writes.Select(w => $"{w.Method} {w.Path}");
        public string Body(string method, string path) => _writes.Last(w => w.Method == method && w.Path == path).Body;

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private static string Session(string id, string title, string start, string end, string state, bool canJoin)
            => $$"""{"id":"{{id}}","courseOfferingId":"{{OfferingId}}","courseCode":"CS101","courseTitle":"Programming","title":"{{title}}","description":null,"provider":"Zoom","startsAt":"{{start}}","endsAt":"{{end}}","durationMinutes":60,"status":"Scheduled","state":"{{state}}","canJoin":{{(canJoin ? "true" : "false")}},"recordingUrl":null,"autoAttendance":true,"minimumMinutes":0,"attendanceTakenAt":null,"timetableEntryId":null,"joined":3}""";

        private static readonly string Sessions = "[" + Session(LiveId, "Week 6 lab", "2026-10-20T09:00:00Z", "2026-10-20T10:00:00Z", "Live", true) + ","
            + Session(LaterId, "Week 7", "2026-11-10T09:00:00Z", "2026-11-10T10:00:00Z", "Upcoming", false) + "]";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, path, body));

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000c1","key":"live_classes","name":"Live Classes","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000c2","key":"live_class_settings","name":"Live Class Settings","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000c3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/course/offerings/my") return Json($$"""[{"id":"{{OfferingId}}","courseTitle":"Programming","semesterName":"Fall 2026"}]""");
            if (path == $"/api/v1/live/offerings/{OfferingId}/sessions" && request.Method == HttpMethod.Get) return Json(Sessions);
            if (path == $"/api/v1/live/offerings/{OfferingId}/timetable")
                return Json($$"""[{"entryId":"{{EntryId}}","day":"Wednesday","start":"10:30","end":"12:00","subjectName":"Programming","roomNumber":"B12"}]""");
            if (path == "/api/v1/live/upcoming") return Json(Sessions);
            if (path == $"/api/v1/live/sessions/{LiveId}/join")
                return JoinStatus == HttpStatusCode.OK ? Json(JsonSerializer.Serialize(new { url = JoinUrl })) : Json("""{"message":"This class has ended."}""", JoinStatus);
            if (path == $"/api/v1/live/sessions/{LiveId}/calendar")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("BEGIN:VCALENDAR\r\nEND:VCALENDAR\r\n", Encoding.UTF8, "text/calendar") };
            if (path == $"/api/v1/live/sessions/{LiveId}/attendance" && request.Method == HttpMethod.Get)
                return Json($$"""{"sessionId":"{{LiveId}}","title":"Week 6 lab","startsAt":"2026-10-20T09:00:00Z","attendanceTakenAt":null,"students":[{"studentProfileId":"{{StudentId}}","name":"Ann Lee","registrationNumber":"R-1","joined":true,"firstJoinedAt":"2026-10-20T09:02:00Z","minutes":55,"present":true}]}""");
            if (path == $"/api/v1/live/sessions/{LiveId}/attendance") return Json("""{"marked":1}""");
            if (path == "/api/v1/live/settings" && request.Method == HttpMethod.Get)
                return Json("""{"tenantId":null,"provider":"BigBlueButton","isEnabled":true,"baseUrl":"https://bbb.example.test/bigbluebutton/api","hasSecret":true,"accountId":null,"clientId":null,"organizerUserId":null,"joinEarlyMinutes":15,"isComplete":true,"isDefault":false}""");
            if (path.EndsWith("/from-timetable")) return Json("[]", HttpStatusCode.Created);
            if (path.StartsWith("/api/v1/live/") && request.Method == HttpMethod.Post && path.EndsWith("/cancel")) return new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path.StartsWith("/api/v1/live/") && request.Method != HttpMethod.Get) return Json("{}");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
