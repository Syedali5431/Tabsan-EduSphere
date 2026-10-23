using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.LiveClasses;
using Tabsan.EduSphere.Infrastructure.LiveClasses;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M10 — BigBlueButton signing/joining, Zoom and Teams calls, live session rules, timetable dates, calendar files.</summary>
public class LiveClassTests
{
    private static readonly ConferenceConfig Bbb = new("https://bbb.test/bigbluebutton/api", "s3cret", null, null, null);

    [Fact]
    public void BigBlueButton_SignsCallsWithSha1OfCallQueryAndSecret()
    {
        var url = BigBlueButtonProvider.SignedUrl(Bbb.BaseUrl!, "s3cret", "join", [("fullName", "Ann Lee"), ("meetingID", "abc")]);
        const string query = "fullName=Ann%20Lee&meetingID=abc";
        var expected = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes("join" + query + "s3cret"))).ToLowerInvariant();
        Assert.Equal($"https://bbb.test/bigbluebutton/api/join?{query}&checksum={expected}", url);
        Assert.EndsWith("getMeetings?checksum=" + Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes("getMeetingss3cret"))).ToLowerInvariant(),
            BigBlueButtonProvider.SignedUrl(Bbb.BaseUrl!, "s3cret", "getMeetings", []));
    }

    [Fact]
    public async Task BigBlueButton_CreatesOnJoin_AndSignsViewerAndModeratorLinks()
    {
        var handler = new FakeHttp(_ => Xml("<response><returncode>SUCCESS</returncode><meetingID>m1</meetingID></response>"));
        var bbb = new BigBlueButtonProvider(new HttpClient(handler));
        var meeting = await bbb.CreateAsync(Bbb, new ConferenceMeetingRequest(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Week 1", null, DateTime.UtcNow, 60));
        Assert.Equal("11111111222233334444555555555555", meeting.ExternalId);
        Assert.Empty(handler.Requests); // nothing is created at the server until someone joins

        var join = new ConferenceJoinRequest("m1", "CS101: Week 1", "Ann Lee", "u1", false, "modpw", "attpw", 60);
        var viewer = await bbb.PersonalJoinUrlAsync(Bbb, join);
        Assert.StartsWith("https://bbb.test/bigbluebutton/api/create?name=CS101%3A%20Week%201&meetingID=m1&attendeePW=attpw&moderatorPW=modpw", handler.Requests.Single());
        Assert.Contains("duration=90", handler.Requests.Single());
        Assert.Contains("password=attpw&role=VIEWER", viewer);
        var moderator = await bbb.PersonalJoinUrlAsync(Bbb, join with { Moderator = true });
        Assert.Contains("password=modpw&role=MODERATOR", moderator);

        var wrong = new BigBlueButtonProvider(new HttpClient(new FakeHttp(_ => Xml("<response><returncode>FAILED</returncode><messageKey>checksumError</messageKey></response>"))));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => wrong.TestAsync(Bbb));
        Assert.Equal("the shared secret is wrong", ex.Message);

        var recordings = new BigBlueButtonProvider(new HttpClient(new FakeHttp(_ => Xml(
            "<response><returncode>SUCCESS</returncode><recordings><recording><playback><format><type>presentation</type><url>https://bbb.test/playback/1</url></format></playback></recording></recordings></response>"))));
        Assert.Equal("https://bbb.test/playback/1", await recordings.RecordingUrlAsync(Bbb, "m1"));
        Assert.Null(await bbb.ParticipantsAsync(Bbb, "m1")); // attendance comes from the portal's join log
    }

    [Fact]
    public async Task Zoom_UsesAccountCredentials_CreatesMeetings_AndSumsParticipantMinutes()
    {
        var config = new ConferenceConfig(null, "zsecret", $"acct-{Guid.NewGuid():N}", "zclient", null);
        var handler = new FakeHttp(r => r.RequestUri!.AbsoluteUri switch
        {
            var u when u.StartsWith(ZoomProvider.OAuthUrl) => Json("""{"access_token":"ztok","expires_in":3600}"""),
            var u when u.EndsWith("/users/me/meetings") => Json("""{"id":987654321,"join_url":"https://zoom.us/j/987654321","start_url":"https://zoom.us/s/987654321?zak=x"}"""),
            var u when u.Contains("/report/meetings/") => Json("""{"participants":[{"user_email":"ann@uni.edu","name":"Ann","duration":1500},{"user_email":"ANN@uni.edu","name":"Ann","duration":300},{"user_email":"ben@uni.edu","name":"Ben","duration":240}]}"""),
            _ => Json("{}")
        });
        var zoom = new ZoomProvider(new HttpClient(handler));
        var meeting = await zoom.CreateAsync(config, new ConferenceMeetingRequest(Guid.NewGuid(), "Week 1", "Intro", new DateTime(2026, 11, 2, 9, 30, 0, DateTimeKind.Utc), 45));
        Assert.Equal(("987654321", "https://zoom.us/j/987654321"), (meeting.ExternalId, meeting.JoinUrl));
        Assert.StartsWith("https://zoom.us/s/", meeting.HostUrl);
        var token = handler.Messages.First();
        Assert.Contains("grant_type=account_credentials&account_id=" + config.AccountId, token.Url);
        Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("zclient:zsecret")), token.Authorization);
        using (var body = JsonDocument.Parse(handler.Messages[1].Body))
        {
            Assert.Equal("2026-11-02T09:30:00Z", body.RootElement.GetProperty("start_time").GetString());
            Assert.Equal(45, body.RootElement.GetProperty("duration").GetInt32());
            Assert.Equal("Bearer ztok", handler.Messages[1].Authorization);
        }
        var participants = await zoom.ParticipantsAsync(config, meeting.ExternalId);
        Assert.Equal(30, participants!.Single(p => p.Email == "ann@uni.edu").Minutes); // rejoins add up
        Assert.Equal(4, participants!.Single(p => p.Email == "ben@uni.edu").Minutes);
        Assert.Single(handler.Messages, m => m.Url.StartsWith(ZoomProvider.OAuthUrl)); // token cached
    }

    [Fact]
    public async Task Teams_CreatesOnlineMeetings_AndReadsAttendanceReports()
    {
        var config = new ConferenceConfig(null, "tsecret", $"tenant-{Guid.NewGuid():N}", "tclient", "organiser@uni.edu");
        var handler = new FakeHttp(r => r.RequestUri!.AbsoluteUri switch
        {
            var u when u.Contains("login.microsoftonline.com") => Json("""{"access_token":"gtok","expires_in":3600}"""),
            var u when u.EndsWith("/onlineMeetings") => Json("""{"id":"MSo1","joinWebUrl":"https://teams.microsoft.com/l/meetup-join/abc"}"""),
            var u when u.EndsWith("/attendanceReports") => Json("""{"value":[{"id":"r1"}]}"""),
            var u when u.EndsWith("/attendanceRecords") => Json("""{"value":[{"emailAddress":"ann@uni.edu","totalAttendanceInSeconds":2700,"identity":{"displayName":"Ann"}}]}"""),
            _ => Json("{}")
        });
        var teams = new TeamsProvider(new HttpClient(handler));
        var meeting = await teams.CreateAsync(config, new ConferenceMeetingRequest(Guid.NewGuid(), "Week 1", null, DateTime.UtcNow.AddDays(1), 60));
        Assert.Equal(("MSo1", "https://teams.microsoft.com/l/meetup-join/abc"), (meeting.ExternalId, meeting.JoinUrl));
        Assert.Contains("grant_type=client_credentials", handler.Messages[0].Body);
        Assert.Contains("scope=https%3A%2F%2Fgraph.microsoft.com%2F.default", handler.Messages[0].Body);
        Assert.Equal($"{TeamsProvider.GraphUrl}/users/organiser%40uni.edu/onlineMeetings", handler.Messages[1].Url);
        var participants = await teams.ParticipantsAsync(config, "MSo1");
        Assert.Equal(45, Assert.Single(participants!).Minutes);
    }

    [Fact]
    public void Session_ValidatesAndOpensForJoiningInItsWindow()
    {
        var start = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
        var s = new LiveSession(Guid.NewGuid(), " Week 1 ", null, ConferenceProviderType.ExternalLink, start, 60, true, 10, null, Guid.NewGuid(), "m", "a");
        Assert.Equal("Week 1", s.Title);
        Assert.Equal(start.AddHours(1), s.EndsAt);
        Assert.False(s.IsJoinable(start.AddMinutes(-16), 15));
        Assert.True(s.IsJoinable(start.AddMinutes(-15), 15));
        Assert.True(s.IsJoinable(s.EndsAt.AddMinutes(30), 15));
        Assert.False(s.IsJoinable(s.EndsAt.AddMinutes(31), 15));
        s.MarkReminderSent(start);
        s.Reschedule("Week 1", null, start.AddDays(1), 60, true, 10);
        Assert.Null(s.ReminderSentAt); // moving the class reminds again
        s.Cancel();
        Assert.False(s.IsJoinable(start.AddDays(1), 15));
        Assert.Throws<ArgumentException>(() => s.Reschedule("x", null, start, 4, false, 0));
        Assert.Throws<ArgumentException>(() => s.Reschedule("x", null, start, 30, false, 31));
        Assert.Throws<ArgumentException>(() => s.Reschedule(" ", null, start, 30, false, 0));
    }

    [Fact]
    public void Settings_KnowWhatEachProviderNeeds()
    {
        var s = new ConferenceSetting(null);
        Assert.True(s.IsComplete); // meeting links need nothing
        s.Configure(ConferenceProviderType.BigBlueButton, true, "https://bbb.test/bigbluebutton/api/", null, null, null, 10);
        Assert.Equal("https://bbb.test/bigbluebutton/api", s.BaseUrl);
        Assert.False(s.IsComplete);
        s.SetEncryptedSecret("cipher");
        Assert.True(s.IsComplete);
        s.Configure(ConferenceProviderType.MicrosoftTeams, true, null, "tenant", "client", null, 10);
        Assert.Null(s.BaseUrl);
        Assert.False(s.IsComplete); // Teams also needs the organiser
        Assert.Throws<ArgumentException>(() => s.Configure(ConferenceProviderType.BigBlueButton, true, "ftp://bbb", null, null, null, 10));
        Assert.Throws<ArgumentException>(() => s.Configure(ConferenceProviderType.Zoom, true, null, "a", "c", null, 500));
    }

    [Fact]
    public void Timetable_NextOccurrences_FallOnTheSlotsWeekdayAndTime()
    {
        var slot = new LiveTimetableSlot(Guid.NewGuid(), Guid.NewGuid(), (int)DayOfWeek.Wednesday, new TimeOnly(10, 30), new TimeOnly(12, 0), "Algorithms", "B12");
        var now = new DateTime(2026, 10, 21, 11, 0, 0, DateTimeKind.Utc); // a Wednesday, after the slot started
        var dates = LiveClassService.NextOccurrences(slot, now, 3).ToList();
        Assert.Equal([new DateTime(2026, 10, 28, 10, 30, 0), new DateTime(2026, 11, 4, 10, 30, 0), new DateTime(2026, 11, 11, 10, 30, 0)], dates);
        Assert.All(dates, d => Assert.Equal(DateTimeKind.Utc, d.Kind));
        Assert.Equal(new DateTime(2026, 10, 21, 10, 30, 0), LiveClassService.NextOccurrences(slot, now.AddHours(-1), 1).Single()); // same day, still ahead
    }

    [Fact]
    public void Calendar_EventHasUtcTimes_EscapedText_AndAReminder()
    {
        var s = new LiveSession(Guid.NewGuid(), "Week 1; intro, part 2", "Bring notes\nand questions", ConferenceProviderType.Zoom,
            new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc), 90, false, 0, null, Guid.NewGuid(), "m", "a");
        var ics = LiveCalendar.Event(s, "CS101 Programming", "https://portal.uni.edu/Portal/LiveClasses?offeringId=x");
        Assert.Contains("DTSTART:20261102T090000Z\r\n", ics);
        Assert.Contains("DTEND:20261102T103000Z\r\n", ics);
        Assert.Contains(@"SUMMARY:Week 1\; intro\, part 2 (CS101 Programming)", ics);
        Assert.Contains(@"DESCRIPTION:Bring notes\nand questions\n\nJoin: https://portal.uni.edu", ics);
        Assert.Contains("TRIGGER:-PT15M", ics);
        Assert.DoesNotContain("STATUS:CANCELLED", ics);
        s.Cancel();
        Assert.Contains("STATUS:CANCELLED", LiveCalendar.Event(s, "CS101", "https://x"));
    }

    private static HttpResponseMessage Xml(string xml) => new(HttpStatusCode.OK) { Content = new StringContent(xml, Encoding.UTF8, "text/xml") };
    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeHttp(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        public List<(string Url, string? Authorization, string Body)> Messages { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            Messages.Add((request.RequestUri!.AbsoluteUri, request.Headers.Authorization?.ToString(), request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return respond(request);
        }
    }
}
