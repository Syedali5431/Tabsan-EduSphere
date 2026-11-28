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
/// The calendar page against a fake API: the month asked for (with the weeks around it), entries listed with their state,
/// the menu link and its guard, the calendar link (create, the Google/Outlook/phone buttons, replace, turn off), the link
/// served at the portal's address without signing in, and the page in Arabic.
/// </summary>
public class CalendarWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string Token = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ"; // 43 characters, like a real link
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public CalendarWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Student_SeesTheMonthsEntries_AndCreatesTheirCalendarLink()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var html = await client.GetStringAsync("/Portal/AcademicCalendar?month=2026-11");

        Assert.Contains("data-calendar", html);
        Assert.Contains("?from=2026-10-24T00:00:00Z&to=2026-12-16T00:00:00Z", api.Reads.Single(r => r.StartsWith("/api/v1/calendar/events", StringComparison.Ordinal)));
        Assert.Contains("id=\"calendar-data\"", html);
        Assert.Contains("\"month\":\"2026-11\"", html);
        var list = WebUtility.HtmlDecode(html);
        Assert.Contains("Essay on rivers", list);
        Assert.Contains(">Overdue</span>", list);
        Assert.Contains(">Handed in</span>", list);
        Assert.Contains("12:00 UTC", list); // without scripts the list says the zone; the script shows local times
        var monthList = list[list.IndexOf("data-calendar-list", StringComparison.Ordinal)..list.IndexOf("id=\"calendar-data\"", StringComparison.Ordinal)];
        Assert.DoesNotContain("Old news", monthList); // October's entries are only in the data, for the grid's first days
        Assert.Contains("\"title\":\"Old news\"", html);
        Assert.Contains("href=\"/Portal/AcademicCalendar?month=2026-10\"", html);
        Assert.Contains("href=\"/Portal/AcademicCalendar?month=2026-12\"", html);
        Assert.DoesNotContain("Manage key dates", html); // students do not edit key dates
        Assert.Contains("href=\"/Portal/AcademicCalendar\"", html); // the sidebar's Calendar link
        Assert.Contains("Create my calendar link", html);
        Assert.Contains("/js/calendar.js", html);

        var created = await client.PostAsync("/Portal/ResetCalendarFeed", Form(TokenOf(html), ("month", "2026-11")));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Equal("/Portal/AcademicCalendar?month=2026-11", created.Headers.Location!.ToString());
        Assert.Contains("POST /api/v1/calendar/feed", api.Writes);
        api.HasFeed = true;

        var withLink = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/AcademicCalendar?month=2026-11"));
        Assert.Contains("Your calendar link is ready.", withLink);
        Assert.Contains($"value=\"https://localhost/calendar/{Token}.ics\"", withLink);
        Assert.Contains($"href=\"webcal://localhost/calendar/{Token}.ics\"", withLink);
        Assert.Contains("https://calendar.google.com/calendar/render?cid=" + Uri.EscapeDataString($"webcal://localhost/calendar/{Token}.ics"), withLink);
        Assert.Contains("https://outlook.live.com/calendar/0/addfromweb?url=" + Uri.EscapeDataString($"https://localhost/calendar/{Token}.ics"), withLink);

        await client.PostAsync("/Portal/RemoveCalendarFeed", Form(TokenOf(withLink), ("month", "2026-11")));
        Assert.Contains("DELETE /api/v1/calendar/feed", api.Writes);
    }

    [Fact]
    public async Task CalendarLink_IsServedAtThePortalsAddress_WithoutSigningIn()
    {
        var api = new FakeApi("Student");
        var factory = Factory(api);
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        var feed = await anonymous.GetAsync($"/calendar/{Token}.ics");
        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
        Assert.Equal("text/calendar", feed.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("BEGIN:VCALENDAR", await feed.Content.ReadAsStringAsync());
        Assert.Equal("no-referrer", feed.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains($"/api/v1/calendar/feed/{Token}.ics", api.Reads);

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/calendar/short.ics")).StatusCode); // not even asked
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/calendar/{new string('z', 43)}.ics")).StatusCode); // unknown to the API
    }

    [Fact]
    public async Task Admin_ManagesKeyDates_AndAMenuWithoutTheCalendarClosesThePage()
    {
        var admin = new FakeApi("Admin");
        using (var client = await SignedInClientAsync(admin))
            Assert.Contains("Manage key dates", await client.GetStringAsync("/Portal/AcademicCalendar"));

        var finance = new FakeApi("Finance") { MenuHasCalendar = false };
        using var financeClient = await SignedInClientAsync(finance);
        var page = await financeClient.GetAsync("/Portal/AcademicCalendar");
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.DoesNotContain(finance.Reads, r => r.StartsWith("/api/v1/calendar/events", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Arabic_CalendarIsRightToLeft()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/AcademicCalendar?month=2026-11");
        await client.PostAsync("/Language/Set", Form(TokenOf(html), ("lang", "ar"), ("returnUrl", "/")));
        var arabic = await client.GetStringAsync("/Portal/AcademicCalendar?month=2026-11");
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", arabic);
        Assert.Contains("أضِف إلى تقويمك", WebUtility.HtmlDecode(arabic));
        Assert.Contains("متأخر", WebUtility.HtmlDecode(arabic)); // Overdue
        Assert.Contains("\"month\":\"2026-11\"", arabic);        // the data stays as it is
    }

    // ── helpers ──

    private static string TokenOf(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }.Concat(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))));

    private WebApplicationFactory<WebEntryPointMarker> Factory(FakeApi api)
        => _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var client = Factory(api).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var page = await client.GetStringAsync("/Login");
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = TokenOf(page), ["username"] = "tester", ["password"] = "Password#123"
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
        private readonly List<string> _writes = new();
        private readonly List<string> _reads = new();
        public bool HasFeed { get; set; }
        public bool MenuHasCalendar { get; set; } = true;
        public IReadOnlyList<string> Writes => _writes;
        public IReadOnlyList<string> Reads => _reads;

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private const string Events = """
            {"from":"2026-10-24T00:00:00Z","to":"2026-12-16T00:00:00Z","events":[
              {"id":"assignment:1","kind":"Assignment","title":"Old news","startsAt":"2026-10-30T12:00:00","allDay":false,"courseCode":"ENG-9","status":"Done"},
              {"id":"assignment:2","kind":"Assignment","title":"Essay on rivers","startsAt":"2026-11-03T12:00:00","allDay":false,"courseCode":"ENG-9","status":"Overdue","url":"/Portal/Assignments?offeringId=1&selectedAssignmentId=2"},
              {"id":"assignment:3","kind":"Assignment","title":"Map work","startsAt":"2026-11-05T09:30:00","allDay":false,"courseCode":"GEO-9","status":"Done"},
              {"id":"quiz:4","kind":"Quiz","title":"Week 3 quiz","startsAt":"2026-11-20T17:00:00","allDay":false,"courseCode":"MATH-9","ownDates":true},
              {"id":"keydate:5","kind":"KeyDate","title":"Sports day","startsAt":"2026-11-27T00:00:00","allDay":true,"courseTitle":"Term 1","note":"Wear house colours"}
            ]}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Content is not null) await request.Content.ReadAsStringAsync(ct);
            if (request.Method == HttpMethod.Get) _reads.Add(request.RequestUri.PathAndQuery);
            else if (path != "/api/v1/auth/login") _writes.Add($"{request.Method.Method} {path}");

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("[" + (MenuHasCalendar ? """{"id":"00000000-0000-0000-0000-0000000000d1","key":"calendar","name":"Calendar","purpose":"","parentId":null,"displayOrder":45,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},""" : "")
                    + """{"id":"00000000-0000-0000-0000-0000000000d3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/calendar/events") return Json(Events);
            if (path == "/api/v1/calendar/feed" && request.Method == HttpMethod.Get)
                return HasFeed ? Json($$"""{"token":"{{Token}}","path":"/api/v1/calendar/feed/{{Token}}.ics","createdAt":"2026-11-26T08:00:00Z","lastUsedAt":null}""") : new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path == "/api/v1/calendar/feed" && request.Method == HttpMethod.Post)
                return Json($$"""{"token":"{{Token}}","path":"/api/v1/calendar/feed/{{Token}}.ics","createdAt":"2026-11-26T08:00:00Z","lastUsedAt":null}""");
            if (path == "/api/v1/calendar/feed" && request.Method == HttpMethod.Delete) return new HttpResponseMessage(HttpStatusCode.NoContent);
            if (path == $"/api/v1/calendar/feed/{Token}.ics")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nEND:VCALENDAR\r\n", Encoding.UTF8, "text/calendar") };
            if (path.StartsWith("/api/v1/calendar/feed/", StringComparison.Ordinal)) return new HttpResponseMessage(HttpStatusCode.NotFound);
            if (path == "/api/v1/live/upcoming") return Json("[]");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
