using System.Globalization;
using System.Text;
using Tabsan.EduSphere.Domain.Calendar;

namespace Tabsan.EduSphere.Application.Calendar;

/// <summary>What a calendar entry is.</summary>
public static class CalendarKinds
{
    public const string Assignment = "Assignment";
    public const string Quiz = "Quiz";
    public const string LiveClass = "LiveClass";
    public const string KeyDate = "KeyDate";
    /// <summary>A school event (news, events and forms) whose audience includes the person.</summary>
    public const string Event = "Event";
}

/// <summary>An entry's state for the person looking at it.</summary>
public static class CalendarStatuses
{
    /// <summary>Handed in (homework) or taken (quiz).</summary>
    public const string Done = "Done";
    /// <summary>Past its date and not handed in or taken.</summary>
    public const string Overdue = "Overdue";
    /// <summary>A live class that was called off.</summary>
    public const string Cancelled = "Cancelled";
    /// <summary>Teachers: not published to students yet.</summary>
    public const string Draft = "Draft";
}

/// <summary>
/// One entry of a person's calendar. Times are UTC; an all-day entry (key dates) uses the date of <see cref="StartsAt"/>.
/// Homework is shown at its due time and quizzes at their closing time, with the student's own dates when the teacher gave
/// them different ones. <see cref="Url"/> is the portal page (a path such as /Portal/Assignments?…).
/// <see cref="Note"/> is a key date's description; <see cref="OwnDates"/>: the student has their own date. For teachers,
/// <see cref="ToMark"/> counts hand-ins waiting for marks and <see cref="SomeHaveOwnDates"/> tells that some students have
/// their own dates. Key dates carry the semester in <see cref="CourseTitle"/>. Parents' entries name the child.
/// </summary>
public sealed record CalendarEventDto(
    string Id,
    string Kind,
    string Title,
    DateTime StartsAt,
    DateTime? EndsAt,
    bool AllDay,
    Guid? ItemId,
    Guid? OfferingId,
    string? CourseCode,
    string? CourseTitle,
    string? Status,
    string? Note,
    string? Url,
    Guid? StudentProfileId = null,
    string? StudentName = null,
    bool OwnDates = false,
    int ToMark = 0,
    bool SomeHaveOwnDates = false);

public sealed record CalendarEventsResponse(DateTime From, DateTime To, IReadOnlyList<CalendarEventDto> Events);

/// <summary>The person's calendar link. <see cref="Path"/> is on the API server (/api/v1/calendar/feed/&lt;token&gt;.ics).</summary>
public sealed record CalendarFeedDto(string Token, string Path, DateTime CreatedAt, DateTime? LastUsedAt);

/// <summary>Which kinds the calendar shows: homework, quizzes and live classes follow their licensed modules.</summary>
public sealed record CalendarSources(bool Assignments, bool Quizzes, bool LiveClasses, bool KeyDates = true)
{
    public static readonly CalendarSources All = new(true, true, true);
}

/// <summary>A signed-in person as the calendar feed needs them (the feed is opened without signing in).</summary>
public sealed record CalendarUser(Guid UserId, string Role, string DisplayName, bool IsActive);

/// <summary>A student who has homework or a quiz due soon, not handed in or taken, and has not been reminded of this date.</summary>
public sealed record DueReminderCandidate(
    DueReminderKind Kind,
    Guid ItemId,
    Guid OfferingId,
    string Title,
    string? CourseCode,
    Guid StudentProfileId,
    Guid StudentUserId,
    string StudentName,
    DateTime DueAt);

/// <summary>A parent of a student (for reminders that also go to parents).</summary>
public sealed record CalendarParentLink(Guid StudentProfileId, Guid ParentUserId);

/// <summary>Settings (appsettings "Calendar").</summary>
public sealed class CalendarOptions
{
    public const string SectionName = "Calendar";

    /// <summary>Public address of the portal, for links in calendar entries (falls back to Mobile:PortalBaseUrl).</summary>
    public string? PortalBaseUrl { get; set; }

    /// <summary>How far back the calendar feed reaches.</summary>
    public int FeedPastDays { get; set; } = 60;

    /// <summary>How far ahead the calendar feed reaches.</summary>
    public int FeedFutureDays { get; set; } = 365;

    public DueReminderOptions Reminders { get; set; } = new();
}

public sealed class DueReminderOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Students are reminded this many hours before homework is due or a quiz closes.</summary>
    public int HoursBefore { get; set; } = 24;

    /// <summary>How often the reminder worker looks.</summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>Also remind the student's linked parents (School).</summary>
    public bool IncludeParents { get; set; }
}

/// <summary>Reads calendars and keeps feed links and sent reminders (Infrastructure: the database).</summary>
public interface ICalendarQueries
{
    /// <summary>
    /// The person's entries between <paramref name="from"/> and <paramref name="to"/>: a student's own work; a parent's
    /// children's (one with <paramref name="childId"/>, all without); a teacher's courses; key dates for everyone (of their
    /// semesters; all of them for admins). Null when a parent asks for a child who is not theirs.
    /// </summary>
    Task<IReadOnlyList<CalendarEventDto>?> EventsAsync(Guid userId, string role, Guid? childId, DateTime from, DateTime to,
        CalendarSources sources, DateTime now, CancellationToken ct = default);

    Task<CalendarUser?> UserAsync(Guid userId, CancellationToken ct = default);

    Task<CalendarFeedToken?> FeedTokenForUserAsync(Guid userId, CancellationToken ct = default);
    Task<CalendarFeedToken?> FeedTokenByHashAsync(string tokenHash, CancellationToken ct = default);
    void Add(CalendarFeedToken token);
    void Remove(CalendarFeedToken token);

    /// <summary>Students whose homework is due, or whose quiz closes, after <paramref name="now"/> and by <paramref name="until"/>.</summary>
    Task<IReadOnlyList<DueReminderCandidate>> DueSoonAsync(DateTime now, DateTime until, CalendarSources sources, CancellationToken ct = default);

    Task<IReadOnlyList<CalendarParentLink>> ParentsAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default);
    void Add(DueReminderLog log);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>The words of a due-date reminder. Relative ("in 5 hours") so they read right in every time zone.</summary>
public static class DueReminderText
{
    public static string When(DateTime dueAt, DateTime now)
    {
        var left = dueAt - now;
        if (left <= TimeSpan.FromMinutes(60)) return "in less than an hour";
        var hours = (int)Math.Round(left.TotalHours, MidpointRounding.AwayFromZero);
        if (hours < 36) return hours == 1 ? "in 1 hour" : $"in {hours} hours";
        var days = (int)Math.Round(left.TotalDays, MidpointRounding.AwayFromZero);
        return $"in {days} days";
    }

    public static (string Title, string Body) ForStudent(DueReminderKind kind, string title, string? courseCode, DateTime dueAt, DateTime now)
    {
        var when = When(dueAt, now);
        var course = string.IsNullOrWhiteSpace(courseCode) ? "" : $" ({courseCode})";
        return kind == DueReminderKind.Quiz
            ? ($"Quiz closes {when}: {title}", $"{title}{course} closes {when} and you have not taken it yet.")
            : ($"Due {when}: {title}", $"{title}{course} is due {when} and has not been handed in yet.");
    }

    public static (string Title, string Body) ForParent(DueReminderKind kind, string title, string? courseCode, string studentName, DateTime dueAt, DateTime now)
    {
        var when = When(dueAt, now);
        var course = string.IsNullOrWhiteSpace(courseCode) ? "" : $" ({courseCode})";
        return kind == DueReminderKind.Quiz
            ? ($"{studentName}: quiz closes {when}", $"{studentName} has not taken {title}{course} yet. It closes {when}.")
            : ($"{studentName}: homework due {when}", $"{studentName} has not handed in {title}{course} yet. It is due {when}.");
    }
}

/// <summary>
/// The calendar as an iCalendar (RFC 5545) feed for Google Calendar, Outlook and phones. Times are UTC; key dates are
/// all-day; cancelled live classes say so; long lines are folded at 75 bytes without splitting a character.
/// </summary>
public static class CalendarIcs
{
    public static string Build(string calendarName, IEnumerable<CalendarEventDto> events, string? portalBaseUrl, DateTime now)
    {
        var sb = new StringBuilder();
        void Line(string text) => sb.Append(Fold(text)).Append("\r\n");

        Line("BEGIN:VCALENDAR");
        Line("VERSION:2.0");
        Line("PRODID:-//Tabsan EduSphere//Calendar//EN");
        Line("CALSCALE:GREGORIAN");
        Line("METHOD:PUBLISH");
        Line($"X-WR-CALNAME:{Escape(calendarName)}");
        Line("X-WR-CALDESC:Due dates\\, quizzes\\, live classes and key dates from EduSphere");
        Line("REFRESH-INTERVAL;VALUE=DURATION:PT1H");
        Line("X-PUBLISHED-TTL:PT1H");
        foreach (var e in events)
        {
            Line("BEGIN:VEVENT");
            Line($"UID:{e.Id.Replace(':', '-')}@edusphere");
            Line($"DTSTAMP:{Utc(now)}");
            if (e.AllDay)
            {
                Line($"DTSTART;VALUE=DATE:{e.StartsAt:yyyyMMdd}");
                var lastDay = e.EndsAt is { } last && last.Date > e.StartsAt.Date ? last.Date : e.StartsAt.Date;
                Line($"DTEND;VALUE=DATE:{lastDay.AddDays(1):yyyyMMdd}");
            }
            else
            {
                Line($"DTSTART:{Utc(e.StartsAt)}");
                Line($"DTEND:{Utc(e.EndsAt is { } end && end > e.StartsAt ? end : e.StartsAt)}");
            }
            Line($"SUMMARY:{Escape(Summary(e))}");
            var url = Link(portalBaseUrl, e.Url);
            var description = string.Join("\n", new[] { e.CourseTitle is null ? null : $"{e.CourseCode} {e.CourseTitle}".Trim(), e.Note, Facts(e), url }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            if (description.Length > 0) Line($"DESCRIPTION:{Escape(description)}");
            if (url is not null) Line($"URL:{url}");
            Line($"CATEGORIES:{Category(e.Kind)}");
            if (e.Status == CalendarStatuses.Cancelled) Line("STATUS:CANCELLED");
            if (e.Kind == CalendarKinds.LiveClass && e.Status != CalendarStatuses.Cancelled)
            {
                Line("BEGIN:VALARM");
                Line("TRIGGER:-PT15M");
                Line("ACTION:DISPLAY");
                Line("DESCRIPTION:Live class starts in 15 minutes");
                Line("END:VALARM");
            }
            Line("END:VEVENT");
        }
        Line("END:VCALENDAR");
        return sb.ToString();
    }

    /// <summary>"Due: Essay (ENG-9)", "Quiz closes: …", "Live class: …"; done work says so; parents' entries start with the child.</summary>
    public static string Summary(CalendarEventDto e)
    {
        var course = string.IsNullOrWhiteSpace(e.CourseCode) ? "" : $" ({e.CourseCode})";
        var text = e.Kind switch
        {
            CalendarKinds.Assignment => $"Due: {e.Title}{course}",
            CalendarKinds.Quiz => $"Quiz closes: {e.Title}{course}",
            CalendarKinds.LiveClass => $"Live class: {e.Title}{course}",
            _ => e.Title
        };
        text += e.Status switch
        {
            CalendarStatuses.Done => e.Kind == CalendarKinds.Quiz ? " — taken" : " — handed in",
            CalendarStatuses.Cancelled => " — cancelled",
            CalendarStatuses.Draft => " — not published",
            _ => ""
        };
        return string.IsNullOrWhiteSpace(e.StudentName) ? text : $"{FirstName(e.StudentName)}: {text}";
    }

    /// <summary>"Own due date", "3 to mark · Some students have their own dates".</summary>
    public static string? Facts(CalendarEventDto e)
    {
        var parts = new List<string>();
        if (e.OwnDates) parts.Add(e.Kind == CalendarKinds.Quiz ? "Own closing time" : "Own due date");
        if (e.ToMark > 0) parts.Add(e.ToMark == 1 ? "1 to mark" : $"{e.ToMark} to mark");
        if (e.SomeHaveOwnDates) parts.Add("Some students have their own dates");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    public static string? Link(string? portalBaseUrl, string? path)
        => string.IsNullOrWhiteSpace(portalBaseUrl) || string.IsNullOrWhiteSpace(path) ? null : portalBaseUrl.TrimEnd('/') + path;

    private static string Category(string kind) => kind switch
    {
        CalendarKinds.Assignment => "Homework",
        CalendarKinds.Quiz => "Quiz",
        CalendarKinds.LiveClass => "Live class",
        _ => "Key date"
    };

    private static string FirstName(string name) => name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;

    private static string Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    public static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");

    /// <summary>RFC 5545 3.1: lines of at most 75 bytes, continued on lines that start with a space.</summary>
    public static string Fold(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= 75) return line;
        var sb = new StringBuilder();
        var bytes = 0;
        var limit = 75;
        var e = StringInfo.GetTextElementEnumerator(line);
        while (e.MoveNext())
        {
            var element = e.GetTextElement();
            var size = Encoding.UTF8.GetByteCount(element);
            if (bytes + size > limit)
            {
                sb.Append("\r\n ");
                bytes = 0;
                limit = 74; // the leading space counts
            }
            sb.Append(element);
            bytes += size;
        }
        return sb.ToString();
    }
}
