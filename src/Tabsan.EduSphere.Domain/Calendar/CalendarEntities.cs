using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Calendar;

/// <summary>
/// A person's private calendar feed: the secret link that Google Calendar, Outlook or a phone subscribes to, so due dates,
/// quizzes, live classes and key dates appear in their own calendar. One per user; resetting replaces the token, which stops
/// the old link. The token is looked up by its SHA-256 hash and kept encrypted so the person can copy the link again.
/// </summary>
public class CalendarFeedToken : BaseEntity
{
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 of the token (hex, lower case).</summary>
    public string TokenHash { get; private set; } = default!;

    /// <summary>The token, encrypted with the application key (shown again on the calendar page).</summary>
    public string TokenEncrypted { get; private set; } = default!;

    public DateTime? LastUsedAt { get; private set; }

    private CalendarFeedToken() { }

    public CalendarFeedToken(Guid userId, string tokenHash, string tokenEncrypted)
    {
        UserId = userId;
        Replace(tokenHash, tokenEncrypted);
    }

    /// <summary>A new token for the same person (the old link stops working).</summary>
    public void Replace(string tokenHash, string tokenEncrypted)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != 64) throw new ArgumentException("A SHA-256 hash is 64 hex characters.", nameof(tokenHash));
        TokenHash = tokenHash;
        TokenEncrypted = tokenEncrypted;
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = null;
        Touch();
    }

    /// <summary>Calendar apps poll every few minutes to hours; recording once an hour is enough.</summary>
    public bool Used(DateTime now)
    {
        if (LastUsedAt is { } last && now - last < TimeSpan.FromHours(1)) return false;
        LastUsedAt = now;
        return true;
    }
}

/// <summary>What a due-date reminder was about.</summary>
public enum DueReminderKind
{
    Assignment = 1,
    Quiz = 2
}

/// <summary>
/// A due-date reminder already sent to one student, so the reminder worker sends each one once. Keyed by the due date it
/// announced: when a teacher moves the date (or gives the student their own), the student is reminded of the new date.
/// </summary>
public class DueReminderLog : BaseEntity
{
    public DueReminderKind Kind { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public DateTime DueAt { get; private set; }

    private DueReminderLog() { }

    public DueReminderLog(DueReminderKind kind, Guid itemId, Guid studentProfileId, DateTime dueAt)
    {
        Kind = kind;
        ItemId = itemId;
        StudentProfileId = studentProfileId;
        DueAt = dueAt;
    }
}
