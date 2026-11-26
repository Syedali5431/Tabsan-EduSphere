using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Calendar;
using Tabsan.EduSphere.Application.DTOs.Notifications;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Calendar;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Infrastructure.Calendar;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// The calendar: entries in order and range checks, the private calendar link (created, shown again, replaced, turned off,
/// read without signing in), the iCalendar feed (UTC times, all-day key dates, escaping, folding, alarms, cancelled
/// classes), due-date reminder wording, sending (once per item, parents when asked, failures logged) and the effective
/// due date with "Assign to" dates.
/// </summary>
public class CalendarTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc);

    // ── fakes ──────────────────────────────────────────────────────────────────────────────────

    private sealed class Queries : ICalendarQueries
    {
        public IReadOnlyList<CalendarEventDto>? Events = [];
        public readonly List<(Guid UserId, string Role, Guid? ChildId, DateTime From, DateTime To, CalendarSources Sources)> EventCalls = [];
        public readonly Dictionary<Guid, CalendarUser> Users = [];
        public readonly List<CalendarFeedToken> Tokens = [];
        public List<DueReminderCandidate> Candidates = [];
        public CalendarSources? DueSoonSources;
        public (DateTime Now, DateTime Until)? DueSoonWindow;
        public readonly List<CalendarParentLink> Parents = [];
        public readonly List<DueReminderLog> Logs = [];
        public int Saves;

        public Task<IReadOnlyList<CalendarEventDto>?> EventsAsync(Guid userId, string role, Guid? childId, DateTime from, DateTime to, CalendarSources sources, DateTime now, CancellationToken ct = default)
        {
            EventCalls.Add((userId, role, childId, from, to, sources));
            return Task.FromResult(Events);
        }

        public Task<CalendarUser?> UserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(Users.GetValueOrDefault(userId));
        public Task<CalendarFeedToken?> FeedTokenForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(Tokens.FirstOrDefault(t => t.UserId == userId));
        public Task<CalendarFeedToken?> FeedTokenByHashAsync(string tokenHash, CancellationToken ct = default) => Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
        public void Add(CalendarFeedToken token) => Tokens.Add(token);
        public void Remove(CalendarFeedToken token) => Tokens.Remove(token);

        public Task<IReadOnlyList<DueReminderCandidate>> DueSoonAsync(DateTime now, DateTime until, CalendarSources sources, CancellationToken ct = default)
        {
            DueSoonSources = sources;
            DueSoonWindow = (now, until);
            return Task.FromResult<IReadOnlyList<DueReminderCandidate>>(Candidates);
        }

        public Task<IReadOnlyList<CalendarParentLink>> ParentsAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CalendarParentLink>>(Parents.Where(p => studentProfileIds.Contains(p.StudentProfileId)).ToList());

        public void Add(DueReminderLog log) => Logs.Add(log);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.FromResult(1); }
    }

    private sealed class PlainEncryption : IEncryptionService
    {
        public string Encrypt(string plaintext) => "enc:" + plaintext;
        public string Decrypt(string ciphertext) => ciphertext.StartsWith("enc:") ? ciphertext[4..] : throw new CryptographicException();
    }

    private sealed class Notifications : INotificationService
    {
        public readonly List<(string Title, string Body, NotificationType Type, IReadOnlyList<Guid> To)> Sent = [];
        public bool Fail;

        public Task<Guid> SendSystemAsync(string title, string body, NotificationType type, IReadOnlyList<Guid> recipientUserIds, CancellationToken ct = default)
        {
            if (Fail) throw new InvalidOperationException("mail server down");
            Sent.Add((title, body, type, recipientUserIds));
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<Guid> SendAsync(SendNotificationRequest request, Guid senderUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> DeactivateAsync(Guid notificationId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NotificationResponse>> GetInboxAsync(Guid userId, bool unreadOnly = false, int page = 0, int pageSize = 20, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<NotificationBadgeResponse> GetBadgeAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MarkAllReadAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class Modules(params string[] active) : IModuleEntitlementResolver
    {
        public Task<bool> IsActiveAsync(string moduleKey, CancellationToken ct = default) => Task.FromResult(active.Contains(moduleKey));
        public void InvalidateCache(string moduleKey) { }
    }

    private static CalendarService Service(Queries q, Notifications? n = null, IModuleEntitlementResolver? modules = null, Action<CalendarOptions>? configure = null)
    {
        var options = new CalendarOptions { PortalBaseUrl = "https://portal.school.test" };
        configure?.Invoke(options);
        return new CalendarService(q, new PlainEncryption(), Options.Create(options), modules, n);
    }

    private static CalendarEventDto Entry(string kind, string title, DateTime at, bool allDay = false, string? status = null, string? student = null, DateTime? end = null)
        => new($"{kind.ToLowerInvariant()}:{Guid.NewGuid()}", kind, title, at, end, allDay, Guid.NewGuid(), Guid.NewGuid(), "ENG-9", "English", status, null,
            "/Portal/Assignments", student is null ? null : Guid.NewGuid(), student);

    // ── entries ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Events_CheckTheRange_AndComeInOrder_AllDayFirst()
    {
        var q = new Queries();
        var later = Entry(CalendarKinds.Assignment, "Essay", Now.AddDays(1).AddHours(5));
        var early = Entry(CalendarKinds.LiveClass, "Revision", Now.AddDays(1).AddHours(1));
        var keyDate = Entry(CalendarKinds.KeyDate, "Exams begin", Now.Date.AddDays(1), allDay: true);
        q.Events = [later, early, keyDate];
        var service = Service(q);

        var (bad, error) = await service.EventsAsync(Guid.NewGuid(), "Student", null, Now, Now.AddDays(-1), Now);
        Assert.Null(bad);
        Assert.Equal("The end of the range must be after its start.", error);
        (_, error) = await service.EventsAsync(Guid.NewGuid(), "Student", null, Now, Now.AddDays(CalendarService.MaxRangeDays + 1), Now);
        Assert.Equal($"Ask for at most {CalendarService.MaxRangeDays} days at a time.", error);

        var (response, none) = await service.EventsAsync(Guid.NewGuid(), "Student", null, Now, Now.AddDays(30), Now);
        Assert.Null(none);
        Assert.Equal(new[] { "Exams begin", "Revision", "Essay" }, response!.Events.Select(e => e.Title));
        Assert.Equal(CalendarSources.All, q.EventCalls.Single().Sources); // no module resolver: everything
    }

    [Fact]
    public async Task Events_TakeLocalTimesAsUtc_FollowTheModules_AndRefuseAnotherParentsChild()
    {
        var q = new Queries { Events = null };
        var service = Service(q, modules: new Modules("assignments", "live_classes"));
        var local = new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Local);

        var (response, error) = await service.EventsAsync(Guid.NewGuid(), "Parent", Guid.NewGuid(), local, local.AddDays(7), Now);

        Assert.Null(response);
        Assert.Null(error); // the controller answers 403
        var call = q.EventCalls.Single();
        Assert.Equal(DateTimeKind.Utc, call.From.Kind);
        Assert.Equal(local.ToUniversalTime(), call.From);
        Assert.Equal(new CalendarSources(true, false, true), call.Sources);
    }

    // ── calendar link ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FeedLink_IsCreatedShownAgainReplacedAndTurnedOff_OnlyTheCurrentOneOpens()
    {
        var q = new Queries();
        var userId = Guid.NewGuid();
        q.Users[userId] = new CalendarUser(userId, "Student", "Ayesha Khan", true);
        q.Events = [Entry(CalendarKinds.Assignment, "Essay on rivers", Now.AddDays(2))];
        var service = Service(q);

        Assert.Null(await service.GetFeedAsync(userId));
        var first = await service.ResetFeedAsync(userId);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", first.Token);
        Assert.Equal($"/api/v1/calendar/feed/{first.Token}.ics", first.Path);
        var stored = q.Tokens.Single();
        Assert.Equal(CalendarService.Hash(first.Token), stored.TokenHash); // looked up by hash
        Assert.Equal("enc:" + first.Token, stored.TokenEncrypted);        // shown again from the encrypted copy
        Assert.Equal(first.Token, (await service.GetFeedAsync(userId))!.Token);

        var ics = await service.FeedAsync(first.Token, Now);
        Assert.NotNull(ics);
        Assert.Contains("X-WR-CALNAME:EduSphere — Ayesha Khan", ics);
        Assert.Contains("SUMMARY:Due: Essay on rivers (ENG-9)", ics);
        Assert.Equal(Now, stored.LastUsedAt);
        var saves = q.Saves;
        await service.FeedAsync(first.Token, Now.AddMinutes(20));
        Assert.Equal(saves, q.Saves); // calendar apps poll often: the use is recorded once an hour

        var second = await service.ResetFeedAsync(userId);
        Assert.NotEqual(first.Token, second.Token);
        Assert.Single(q.Tokens);
        Assert.Null(await service.FeedAsync(first.Token, Now)); // the old link stops working
        Assert.NotNull(await service.FeedAsync(second.Token, Now));

        Assert.Null(await service.FeedAsync("not-a-token", Now));
        Assert.Null(await service.FeedAsync(CalendarService.NewToken(), Now)); // well-formed but unknown

        q.Users[userId] = q.Users[userId] with { IsActive = false };
        Assert.Null(await service.FeedAsync(second.Token, Now)); // a deactivated account's calendar is not served
        q.Users[userId] = q.Users[userId] with { IsActive = true };

        Assert.True(await service.RemoveFeedAsync(userId));
        Assert.False(await service.RemoveFeedAsync(userId));
        Assert.Null(await service.FeedAsync(second.Token, Now));
    }

    [Fact]
    public async Task FeedLink_AnUnreadableCopy_IsNotShown_AndTheFeedCoversItsWindow()
    {
        var q = new Queries();
        var userId = Guid.NewGuid();
        q.Users[userId] = new CalendarUser(userId, "Parent", "Sara Ali", true);
        q.Tokens.Add(new CalendarFeedToken(userId, new string('a', 64), "garbled"));
        var service = Service(q, configure: o => { o.FeedPastDays = 30; o.FeedFutureDays = 90; });

        Assert.Null(await service.GetFeedAsync(userId)); // the application key changed: the person makes a new link

        var link = await service.ResetFeedAsync(userId);
        await service.FeedAsync(link.Token, Now);
        var call = q.EventCalls.Single();
        Assert.Equal(("Parent", (Guid?)null), (call.Role, call.ChildId)); // a parent's feed has every child
        Assert.Equal(Now.Date.AddDays(-30), call.From);
        Assert.Equal(Now.Date.AddDays(91), call.To);
    }

    // ── iCalendar ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Ics_HasUtcTimes_AllDayKeyDates_AlarmsForClasses_AndCancelledClasses()
    {
        var due = Entry(CalendarKinds.Assignment, "Essay", new DateTime(2026, 12, 1, 12, 0, 0), status: CalendarStatuses.Done);
        var keyDate = Entry(CalendarKinds.KeyDate, "Exams begin", new DateTime(2026, 12, 7), allDay: true);
        var live = Entry(CalendarKinds.LiveClass, "Revision", new DateTime(2026, 12, 2, 4, 30, 0), end: new DateTime(2026, 12, 2, 5, 15, 0));
        var cancelled = Entry(CalendarKinds.LiveClass, "Q&A", new DateTime(2026, 12, 3, 4, 30, 0), status: CalendarStatuses.Cancelled, end: new DateTime(2026, 12, 3, 5, 0, 0));

        var ics = CalendarIcs.Build("EduSphere — Ali", [due, keyDate, live, cancelled], "https://portal.school.test/", Now);
        var lines = Unfold(ics);

        Assert.StartsWith("BEGIN:VCALENDAR\r\nVERSION:2.0\r\n", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.DoesNotContain(ics.Replace("\r\n", ""), c => c == '\n'); // every line ends in CRLF
        Assert.Contains("DTSTART:20261201T120000Z", lines);
        Assert.Contains("DTEND:20261201T120000Z", lines); // a due date is a moment
        Assert.Contains("SUMMARY:Due: Essay (ENG-9) — handed in", lines);
        Assert.Contains("DTSTART;VALUE=DATE:20261207", lines);
        Assert.Contains("DTEND;VALUE=DATE:20261208", lines);
        Assert.Contains("DTSTART:20261202T043000Z", lines);
        Assert.Contains("DTEND:20261202T051500Z", lines);
        Assert.Contains("URL:https://portal.school.test/Portal/Assignments", lines);
        Assert.Contains("CATEGORIES:Key date", lines);
        Assert.Equal(1, lines.Count(l => l == "TRIGGER:-PT15M"));  // only the class still on gets a reminder
        Assert.Equal(1, lines.Count(l => l == "STATUS:CANCELLED"));
        Assert.Contains("SUMMARY:Live class: Q&A (ENG-9) — cancelled", lines);
        Assert.Contains($"UID:{due.Id.Replace(':', '-')}@edusphere", lines);
        Assert.Equal(4, lines.Count(l => l == "BEGIN:VEVENT"));
    }

    [Fact]
    public void Ics_EscapesText_FoldsLongLines_AndNamesTheChildForParents()
    {
        var title = "Essay; part 1, rivers\nand lakes — " + new string('ب', 60); // Urdu letters take two bytes each
        var e = Entry(CalendarKinds.Assignment, title, new DateTime(2026, 12, 1, 12, 0, 0), student: "Ali Raza");

        var ics = CalendarIcs.Build("EduSphere", [e], null, Now);

        foreach (var line in ics.Split("\r\n"))
            Assert.True(Encoding.UTF8.GetByteCount(line) <= 75, $"{Encoding.UTF8.GetByteCount(line)} bytes: {line}");
        var summary = Unfold(ics).Single(l => l.StartsWith("SUMMARY:", StringComparison.Ordinal));
        Assert.Equal("SUMMARY:Ali: Due: Essay\\; part 1\\, rivers\\nand lakes — " + new string('ب', 60) + " (ENG-9)", summary);
        Assert.DoesNotContain(Unfold(ics), l => l.StartsWith("URL:", StringComparison.Ordinal)); // no portal address: no links
        Assert.Equal("Ali: Quiz closes: Week 3 (ENG-9) — taken",
            CalendarIcs.Summary(Entry(CalendarKinds.Quiz, "Week 3", Now, status: CalendarStatuses.Done, student: "Ali Raza")));
        Assert.Equal("Own due date · 3 to mark · Some students have their own dates",
            CalendarIcs.Facts(Entry(CalendarKinds.Assignment, "x", Now) with { OwnDates = true, ToMark = 3, SomeHaveOwnDates = true }));
    }

    private static List<string> Unfold(string ics) => ics.Replace("\r\n ", "").Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

    // ── reminders ──────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(30, "in less than an hour")]
    [InlineData(61, "in 1 hour")]
    [InlineData(5 * 60 + 10, "in 5 hours")]
    [InlineData(24 * 60, "in 24 hours")]
    [InlineData(3 * 24 * 60, "in 3 days")]
    public void ReminderText_SaysHowLongIsLeft(int minutes, string expected)
        => Assert.Equal(expected, DueReminderText.When(Now.AddMinutes(minutes), Now));

    [Fact]
    public void ReminderText_ForStudentsAndParents()
    {
        Assert.Equal(("Due in 5 hours: Essay", "Essay (ENG-9) is due in 5 hours and has not been handed in yet."),
            DueReminderText.ForStudent(DueReminderKind.Assignment, "Essay", "ENG-9", Now.AddHours(5), Now));
        Assert.Equal(("Quiz closes in 5 hours: Week 3", "Week 3 closes in 5 hours and you have not taken it yet."),
            DueReminderText.ForStudent(DueReminderKind.Quiz, "Week 3", null, Now.AddHours(5), Now));
        Assert.Equal(("Ali Raza: homework due in 24 hours", "Ali Raza has not handed in Essay (ENG-9) yet. It is due in 24 hours."),
            DueReminderText.ForParent(DueReminderKind.Assignment, "Essay", "ENG-9", "Ali Raza", Now.AddHours(24), Now));
    }

    private static DueReminderCandidate Candidate(DueReminderKind kind, Guid item, string title, Guid? profile = null, Guid? user = null, int hours = 5)
        => new(kind, item, Guid.NewGuid(), title, "ENG-9", profile ?? Guid.NewGuid(), user ?? Guid.NewGuid(), "Student " + title, Now.AddHours(hours));

    [Fact]
    public async Task Reminders_GoOncePerItem_ToItsStudents_AndAreLogged()
    {
        var essay = Guid.NewGuid();
        var quiz = Guid.NewGuid();
        var q = new Queries
        {
            Candidates = [Candidate(DueReminderKind.Assignment, essay, "Essay"), Candidate(DueReminderKind.Assignment, essay, "Essay"), Candidate(DueReminderKind.Quiz, quiz, "Week 3")]
        };
        var n = new Notifications();
        var service = Service(q, n, configure: o => o.Reminders.HoursBefore = 12);

        Assert.Equal(3, await service.SendDueRemindersAsync(Now));

        Assert.Equal((Now, Now.AddHours(12)), q.DueSoonWindow);
        Assert.Equal(2, n.Sent.Count);
        var homework = n.Sent.Single(s => s.Title == "Due in 5 hours: Essay");
        Assert.Equal(NotificationType.DueReminder, homework.Type);
        Assert.Equal(2, homework.To.Count);
        Assert.Single(n.Sent, s => s.Title == "Quiz closes in 5 hours: Week 3");
        Assert.Equal(3, q.Logs.Count);
        Assert.All(q.Logs, l => Assert.Equal(Now.AddHours(5), l.DueAt));
        Assert.Equal(2, q.Logs.Count(l => l.Kind == DueReminderKind.Assignment && l.ItemId == essay));
    }

    [Fact]
    public async Task Reminders_TellParentsWhenAsked_AndLogEvenWhenSendingFails()
    {
        var profile = Guid.NewGuid();
        var parent = Guid.NewGuid();
        var item = Guid.NewGuid();
        var q = new Queries { Candidates = [Candidate(DueReminderKind.Assignment, item, "Essay", profile)] };
        q.Parents.Add(new CalendarParentLink(profile, parent));
        var n = new Notifications();

        await Service(q, n, configure: o => o.Reminders.IncludeParents = true).SendDueRemindersAsync(Now);
        Assert.Equal(2, n.Sent.Count);
        Assert.Equal(new[] { parent }, n.Sent[1].To);
        Assert.Equal("Student Essay: homework due in 5 hours", n.Sent[1].Title);

        var failing = new Queries { Candidates = [Candidate(DueReminderKind.Quiz, Guid.NewGuid(), "Week 3")] };
        Assert.Equal(1, await Service(failing, new Notifications { Fail = true }).SendDueRemindersAsync(Now));
        Assert.Single(failing.Logs); // not resent every few minutes while a channel is down
    }

    [Fact]
    public async Task Reminders_StayQuiet_WhenOff_WithoutNotifications_OrWithoutTheModules()
    {
        var q = new Queries { Candidates = [Candidate(DueReminderKind.Assignment, Guid.NewGuid(), "Essay")] };
        Assert.Equal(0, await Service(q, new Notifications(), configure: o => o.Reminders.Enabled = false).SendDueRemindersAsync(Now));
        Assert.Equal(0, await Service(q, null).SendDueRemindersAsync(Now));
        Assert.Equal(0, await Service(q, new Notifications(), new Modules("live_classes")).SendDueRemindersAsync(Now));
        Assert.Null(q.DueSoonWindow);

        Assert.Equal(1, await Service(q, new Notifications(), new Modules("quizzes")).SendDueRemindersAsync(Now));
        Assert.Equal(new CalendarSources(false, true, false), q.DueSoonSources);
    }

    // ── effective due date ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void EffectiveDue_StudentFirst_ThenTheLatestOfTheirGroups_ElseTheAssignments()
    {
        var assignment = Guid.NewGuid();
        var student = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var due = Now.AddDays(3);
        var forGroupA = new AssignmentOverride(assignment, null, groupA, Now.AddDays(4), null, null, null);
        var forGroupB = new AssignmentOverride(assignment, null, groupB, Now.AddDays(6), null, null, null);
        var forStudent = new AssignmentOverride(assignment, student, null, Now.AddDays(5), null, null, null);
        var someoneElse = new AssignmentOverride(assignment, Guid.NewGuid(), null, Now.AddDays(9), null, null, null);

        Assert.Equal((due, false), CalendarQueries.EffectiveDue(due, [someoneElse], student, new HashSet<Guid>()));
        Assert.Equal((Now.AddDays(6), true), CalendarQueries.EffectiveDue(due, [forGroupA, forGroupB], student, new HashSet<Guid> { groupA, groupB }));
        Assert.Equal((Now.AddDays(4), true), CalendarQueries.EffectiveDue(due, [forGroupA, forGroupB], student, new HashSet<Guid> { groupA }));
        Assert.Equal((Now.AddDays(5), true), CalendarQueries.EffectiveDue(due, [forGroupB, forStudent], student, new HashSet<Guid> { groupB }));
    }

    // ── domain ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FeedToken_NeedsASha256Hash_AndReplacingStartsAfresh()
    {
        Assert.Throws<ArgumentException>(() => new CalendarFeedToken(Guid.NewGuid(), "short", "x"));
        var token = new CalendarFeedToken(Guid.NewGuid(), new string('a', 64), "x");
        Assert.True(token.Used(Now));
        token.Replace(new string('b', 64), "y");
        Assert.Null(token.LastUsedAt);
        Assert.Equal(new string('b', 64), token.TokenHash);
    }
}
