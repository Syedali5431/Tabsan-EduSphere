using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Calendar;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Calendar;

/// <summary>
/// One calendar of everything with a date: homework due dates and quiz closing times (with a student's own dates), live
/// classes and the institution's key dates. Shown in the portal and the app, subscribed to from Google Calendar, Outlook or a
/// phone through a private link, and the source of due-date reminders for students who have not handed in yet.
/// </summary>
public sealed partial class CalendarService
{
    /// <summary>The longest range one request may ask for.</summary>
    public const int MaxRangeDays = 400;

    /// <summary>Reminders sent per run at most (the rest follow on the next run).</summary>
    public const int MaxRemindersPerRun = 5000;

    public const string FeedPathPrefix = "/api/v1/calendar/feed/";

    private readonly ICalendarQueries _queries;
    private readonly IEncryptionService _encryption;
    private readonly CalendarOptions _options;
    private readonly IModuleEntitlementResolver? _modules;
    private readonly INotificationService? _notifications;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(ICalendarQueries queries, IEncryptionService encryption, IOptions<CalendarOptions> options,
        IModuleEntitlementResolver? modules = null, INotificationService? notifications = null, ILogger<CalendarService>? logger = null)
    {
        _queries = queries;
        _encryption = encryption;
        _options = options.Value;
        _modules = modules;
        _notifications = notifications;
        _logger = logger ?? NullLogger<CalendarService>.Instance;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Entries
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>The person's entries in a range (at most <see cref="MaxRangeDays"/> days). Null data: the child is not theirs.</summary>
    public async Task<(CalendarEventsResponse? Response, string? Error)> EventsAsync(Guid userId, string role, Guid? childId, DateTime from, DateTime to,
        DateTime now, CancellationToken ct = default)
    {
        from = Utc(from);
        to = Utc(to);
        if (to <= from) return (null, "The end of the range must be after its start.");
        if ((to - from).TotalDays > MaxRangeDays) return (null, $"Ask for at most {MaxRangeDays} days at a time.");
        var events = await _queries.EventsAsync(userId, role, childId, from, to, await SourcesAsync(ct), now, ct);
        return events is null
            ? (null, null)
            : (new CalendarEventsResponse(from, to, Order(events)), null);
    }

    public static IReadOnlyList<CalendarEventDto> Order(IEnumerable<CalendarEventDto> events)
        => events.OrderBy(e => e.AllDay ? e.StartsAt.Date : e.StartsAt).ThenBy(e => e.AllDay ? 0 : 1).ThenBy(e => e.Title, StringComparer.CurrentCulture).ToList();

    private async Task<CalendarSources> SourcesAsync(CancellationToken ct)
    {
        if (_modules is null) return CalendarSources.All;
        return new CalendarSources(
            await _modules.IsActiveAsync("assignments", ct),
            await _modules.IsActiveAsync("quizzes", ct),
            await _modules.IsActiveAsync("live_classes", ct));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Calendar link (feed)
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<CalendarFeedDto?> GetFeedAsync(Guid userId, CancellationToken ct = default)
    {
        var token = await _queries.FeedTokenForUserAsync(userId, ct);
        if (token is null) return null;
        string plain;
        try { plain = _encryption.Decrypt(token.TokenEncrypted); }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // The application key changed: the old link cannot be shown (or trusted) any more.
            _logger.LogWarning("A calendar link could not be decrypted; the person needs a new one.");
            return null;
        }
        return Dto(plain, token);
    }

    /// <summary>A new private link (any earlier link of the person stops working).</summary>
    public async Task<CalendarFeedDto> ResetFeedAsync(Guid userId, CancellationToken ct = default)
    {
        var plain = NewToken();
        var hash = Hash(plain);
        var encrypted = _encryption.Encrypt(plain);
        var token = await _queries.FeedTokenForUserAsync(userId, ct);
        if (token is null) _queries.Add(token = new CalendarFeedToken(userId, hash, encrypted));
        else token.Replace(hash, encrypted);
        await _queries.SaveChangesAsync(ct);
        return Dto(plain, token);
    }

    public async Task<bool> RemoveFeedAsync(Guid userId, CancellationToken ct = default)
    {
        var token = await _queries.FeedTokenForUserAsync(userId, ct);
        if (token is null) return false;
        _queries.Remove(token);
        await _queries.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>The iCalendar text for a link, or null when the link is unknown, reset or its owner is deactivated.</summary>
    public async Task<string?> FeedAsync(string token, DateTime now, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(token) || !TokenPattern().IsMatch(token)) return null;
        var row = await _queries.FeedTokenByHashAsync(Hash(token), ct);
        if (row is null) return null;
        var user = await _queries.UserAsync(row.UserId, ct);
        if (user is null || !user.IsActive) return null;
        var from = now.Date.AddDays(-Math.Clamp(_options.FeedPastDays, 0, 365));
        var to = now.Date.AddDays(Math.Clamp(_options.FeedFutureDays, 1, MaxRangeDays - 1) + 1);
        var events = await _queries.EventsAsync(user.UserId, user.Role, null, from, to, await SourcesAsync(ct), now, ct) ?? [];
        if (row.Used(now)) await _queries.SaveChangesAsync(ct);
        return CalendarIcs.Build($"EduSphere — {user.DisplayName}", Order(events), _options.PortalBaseUrl, now);
    }

    private static CalendarFeedDto Dto(string plain, CalendarFeedToken token)
        => new(plain, $"{FeedPathPrefix}{plain}.ics", token.CreatedAt, token.LastUsedAt);

    public static string NewToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$")]
    private static partial Regex TokenPattern();

    // ═════════════════════════════════════════════════════════════════════════
    // Due-date reminders
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reminds students whose homework is due, or whose quiz closes, within <see cref="DueReminderOptions.HoursBefore"/> hours
    /// and who have not handed it in or taken it: once per due date (a moved date is announced again), with the parents when
    /// asked to. Returns how many students were reminded.
    /// </summary>
    public async Task<int> SendDueRemindersAsync(DateTime now, CancellationToken ct = default)
    {
        var options = _options.Reminders;
        if (!options.Enabled || _notifications is null) return 0;
        var sources = await SourcesAsync(ct);
        if (!sources.Assignments && !sources.Quizzes) return 0;
        var candidates = (await _queries.DueSoonAsync(now, now.AddHours(Math.Clamp(options.HoursBefore, 1, 24 * 14)), sources, ct))
            .Take(MaxRemindersPerRun).ToList();
        if (candidates.Count == 0) return 0;

        var parents = options.IncludeParents
            ? (await _queries.ParentsAsync(candidates.Select(c => c.StudentProfileId).Distinct().ToList(), ct))
                .GroupBy(p => p.StudentProfileId).ToDictionary(g => g.Key, g => g.Select(p => p.ParentUserId).Distinct().ToList())
            : [];
        var reminded = 0;
        foreach (var group in candidates.GroupBy(c => (c.Kind, c.ItemId, c.DueAt)))
        {
            var first = group.First();
            var students = group.DistinctBy(c => c.StudentProfileId).ToList();
            try
            {
                var (title, body) = DueReminderText.ForStudent(first.Kind, first.Title, first.CourseCode, first.DueAt, now);
                await _notifications.SendSystemAsync(title, body, NotificationType.DueReminder, students.Select(s => s.StudentUserId).Distinct().ToList(), ct);
                foreach (var s in students)
                {
                    if (!parents.TryGetValue(s.StudentProfileId, out var parentIds) || parentIds.Count == 0) continue;
                    var (parentTitle, parentBody) = DueReminderText.ForParent(first.Kind, first.Title, first.CourseCode, s.StudentName, first.DueAt, now);
                    await _notifications.SendSystemAsync(parentTitle, parentBody, NotificationType.DueReminder, parentIds, ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Logged as sent all the same: a failing channel must not resend the reminder every few minutes.
                _logger.LogWarning(ex, "A due-date reminder for {Kind} {ItemId} could not be sent.", first.Kind, first.ItemId);
            }
            foreach (var s in students) _queries.Add(new DueReminderLog(first.Kind, first.ItemId, s.StudentProfileId, first.DueAt));
            await _queries.SaveChangesAsync(ct);
            reminded += students.Count;
        }
        return reminded;
    }

    private static DateTime Utc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };
}
