using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.LiveClasses;

// Plan M10 — live classes / web conferencing.

public enum ConferenceProviderType
{
    /// <summary>Self-hosted BigBlueButton (free); meetings are created on demand with the server's shared secret.</summary>
    BigBlueButton = 0,
    /// <summary>Zoom through a Server-to-Server OAuth app.</summary>
    Zoom = 1,
    /// <summary>Microsoft Teams online meetings through Microsoft Graph (application permissions).</summary>
    MicrosoftTeams = 2,
    /// <summary>A meeting link pasted by the teacher (Google Meet, Jitsi, an existing Teams/Zoom link…).</summary>
    ExternalLink = 3
}

public enum LiveSessionStatus
{
    Scheduled = 0,
    Cancelled = 1
}

/// <summary>Plan M10.1: a tenant's conferencing provider (TenantId null = the installation default).</summary>
public class ConferenceSetting : BaseEntity
{
    public Guid? TenantId { get; private set; }
    public ConferenceProviderType Provider { get; private set; } = ConferenceProviderType.ExternalLink;
    public bool IsEnabled { get; private set; } = true;
    /// <summary>BigBlueButton API address (e.g. https://bbb.uni.edu/bigbluebutton/api).</summary>
    public string? BaseUrl { get; private set; }
    /// <summary>BigBlueButton shared secret, or the Zoom / Microsoft Entra client secret — encrypted at rest.</summary>
    public string? SecretEncrypted { get; private set; }
    /// <summary>Zoom account id, or the Microsoft Entra tenant id.</summary>
    public string? AccountId { get; private set; }
    /// <summary>Zoom / Microsoft Entra application (client) id.</summary>
    public string? ClientId { get; private set; }
    /// <summary>Teams: the user (object id or UPN) who organises the meetings. Zoom: the host user ("me" when empty).</summary>
    public string? OrganizerUserId { get; private set; }
    /// <summary>Minutes before the start when joining opens.</summary>
    public int JoinEarlyMinutes { get; private set; } = 15;

    private ConferenceSetting() { }

    public ConferenceSetting(Guid? tenantId) => TenantId = tenantId;

    public void Configure(ConferenceProviderType provider, bool enabled, string? baseUrl, string? accountId, string? clientId, string? organizerUserId, int joinEarlyMinutes)
    {
        if (!Enum.IsDefined(provider)) throw new ArgumentException("Unknown provider.", nameof(provider));
        if (joinEarlyMinutes is < 0 or > 120) throw new ArgumentException("Joining can open 0 to 120 minutes early.", nameof(joinEarlyMinutes));
        var url = Clean(baseUrl, 500);
        if (provider == ConferenceProviderType.BigBlueButton && url is not null
            && (!Uri.TryCreate(url, UriKind.Absolute, out var u) || (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp)))
            throw new ArgumentException("Enter the BigBlueButton API address (https://…/bigbluebutton/api).", nameof(baseUrl));
        Provider = provider;
        IsEnabled = enabled;
        BaseUrl = provider == ConferenceProviderType.BigBlueButton ? url?.TrimEnd('/') : null;
        AccountId = provider is ConferenceProviderType.Zoom or ConferenceProviderType.MicrosoftTeams ? Clean(accountId, 200) : null;
        ClientId = provider is ConferenceProviderType.Zoom or ConferenceProviderType.MicrosoftTeams ? Clean(clientId, 200) : null;
        OrganizerUserId = provider is ConferenceProviderType.Zoom or ConferenceProviderType.MicrosoftTeams ? Clean(organizerUserId, 200) : null;
        JoinEarlyMinutes = joinEarlyMinutes;
        Touch();
    }

    public void SetEncryptedSecret(string? cipher)
    {
        SecretEncrypted = string.IsNullOrWhiteSpace(cipher) ? null : cipher;
        Touch();
    }

    /// <summary>Whether the provider has everything it needs to create meetings.</summary>
    public bool IsComplete => Provider switch
    {
        ConferenceProviderType.BigBlueButton => BaseUrl is not null && SecretEncrypted is not null,
        ConferenceProviderType.Zoom => AccountId is not null && ClientId is not null && SecretEncrypted is not null,
        ConferenceProviderType.MicrosoftTeams => AccountId is not null && ClientId is not null && SecretEncrypted is not null && OrganizerUserId is not null,
        _ => true
    };

    private static string? Clean(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.Length > max ? throw new ArgumentException($"Values are limited to {max} characters.") : v;
    }
}

/// <summary>Plan M10.2: a scheduled live class of a course offering.</summary>
public class LiveSession : BaseEntity
{
    public const int MaxDurationMinutes = 600;

    public Guid CourseOfferingId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public ConferenceProviderType Provider { get; private set; }
    public DateTime StartsAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public LiveSessionStatus Status { get; private set; }
    /// <summary>The provider's meeting id (BigBlueButton: our own meeting id).</summary>
    public string? ExternalMeetingId { get; private set; }
    /// <summary>The link participants open (not used for BigBlueButton, whose links are signed per person).</summary>
    public string? JoinUrl { get; private set; }
    /// <summary>The host's start link (Zoom), encrypted at rest because it signs the host in.</summary>
    public string? HostUrlEncrypted { get; private set; }
    public string? RecordingUrl { get; private set; }
    /// <summary>BigBlueButton passwords (older servers); random per session.</summary>
    public string ModeratorPassword { get; private set; } = default!;
    public string AttendeePassword { get; private set; } = default!;
    public Guid? TimetableEntryId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    /// <summary>Mark attendance from who joined once the class has ended.</summary>
    public bool AutoAttendance { get; private set; }
    /// <summary>Minutes in the class needed to count as present, when the provider reports minutes (0 = any join).</summary>
    public int MinimumMinutes { get; private set; }
    public DateTime? ReminderSentAt { get; private set; }
    public DateTime? AttendanceTakenAt { get; private set; }

    private LiveSession() { }

    public LiveSession(Guid courseOfferingId, string title, string? description, ConferenceProviderType provider, DateTime startsAt, int durationMinutes,
        bool autoAttendance, int minimumMinutes, Guid? timetableEntryId, Guid createdBy, string moderatorPassword, string attendeePassword)
    {
        CourseOfferingId = courseOfferingId;
        Provider = provider;
        TimetableEntryId = timetableEntryId;
        CreatedByUserId = createdBy;
        ModeratorPassword = moderatorPassword;
        AttendeePassword = attendeePassword;
        Status = LiveSessionStatus.Scheduled;
        Reschedule(title, description, startsAt, durationMinutes, autoAttendance, minimumMinutes);
    }

    public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);

    public void Reschedule(string title, string? description, DateTime startsAt, int durationMinutes, bool autoAttendance, int minimumMinutes)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200) throw new ArgumentException("Enter a title (up to 200 characters).", nameof(title));
        if (durationMinutes is < 5 or > MaxDurationMinutes) throw new ArgumentException($"A class lasts 5 to {MaxDurationMinutes} minutes.", nameof(durationMinutes));
        if (minimumMinutes < 0 || minimumMinutes > durationMinutes) throw new ArgumentException("The minimum time must be between 0 and the class's length.", nameof(minimumMinutes));
        if (description is { Length: > 2000 }) throw new ArgumentException("The description is limited to 2000 characters.", nameof(description));
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (startsAt != StartsAt) ReminderSentAt = null;
        StartsAt = startsAt;
        DurationMinutes = durationMinutes;
        AutoAttendance = autoAttendance;
        MinimumMinutes = minimumMinutes;
        Touch();
    }

    public void SetMeeting(string? externalMeetingId, string? joinUrl, string? hostUrlEncrypted)
    {
        ExternalMeetingId = externalMeetingId;
        JoinUrl = joinUrl;
        HostUrlEncrypted = hostUrlEncrypted;
        Touch();
    }

    public void Cancel()
    {
        Status = LiveSessionStatus.Cancelled;
        Touch();
    }

    public void SetRecording(string url)
    {
        RecordingUrl = url;
        Touch();
    }

    public void MarkReminderSent(DateTime now) { ReminderSentAt = now; Touch(); }

    public void MarkAttendanceTaken(DateTime now) { AttendanceTakenAt = now; Touch(); }

    /// <summary>Joining opens <paramref name="earlyMinutes"/> before the start and closes 30 minutes after the end.</summary>
    public bool IsJoinable(DateTime now, int earlyMinutes)
        => Status == LiveSessionStatus.Scheduled && now >= StartsAt.AddMinutes(-earlyMinutes) && now <= EndsAt.AddMinutes(30);
}

/// <summary>Plan M10.3: a person's joins of a live class (recorded by the portal when they press Join).</summary>
public class LiveSessionAttendance : BaseEntity
{
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? StudentProfileId { get; private set; }
    public DateTime FirstJoinedAt { get; private set; }
    public DateTime LastJoinedAt { get; private set; }
    public int JoinCount { get; private set; }
    /// <summary>Minutes in the meeting from the provider's participant report (null when the provider gives none).</summary>
    public int? Minutes { get; private set; }

    private LiveSessionAttendance() { }

    public LiveSessionAttendance(Guid sessionId, Guid userId, Guid? studentProfileId, DateTime now)
    {
        SessionId = sessionId;
        UserId = userId;
        StudentProfileId = studentProfileId;
        FirstJoinedAt = now;
        LastJoinedAt = now;
        JoinCount = 1;
    }

    public void Joined(DateTime now)
    {
        LastJoinedAt = now;
        JoinCount++;
        Touch();
    }

    public void SetMinutes(int minutes)
    {
        Minutes = Math.Max(0, minutes);
        Touch();
    }
}
