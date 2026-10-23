using System.Text;
using Tabsan.EduSphere.Domain.LiveClasses;

namespace Tabsan.EduSphere.Application.LiveClasses;

// Plan M10 — live classes contracts and the conferencing provider abstraction.

/// <summary>What a provider needs from the tenant's settings (secret already decrypted).</summary>
public sealed record ConferenceConfig(string? BaseUrl, string? Secret, string? AccountId, string? ClientId, string? OrganizerUserId);

public sealed record ConferenceMeetingRequest(Guid SessionId, string Title, string? Description, DateTime StartsAt, int DurationMinutes);

/// <summary>A meeting created at the provider. HostUrl signs the host in (Zoom start link).</summary>
public sealed record ConferenceMeeting(string ExternalId, string? JoinUrl, string? HostUrl);

public sealed record ConferenceJoinRequest(string MeetingId, string MeetingName, string FullName, string UserId, bool Moderator,
    string ModeratorPassword, string AttendeePassword, int DurationMinutes);

public sealed record ConferenceParticipant(string? Email, string? Name, int Minutes);

/// <summary>Plan M10.1: one conferencing service (BigBlueButton, Zoom, Microsoft Teams).</summary>
public interface IConferenceProvider
{
    ConferenceProviderType Type { get; }

    /// <summary>Creates (or, for BigBlueButton, reserves) the meeting.</summary>
    Task<ConferenceMeeting> CreateAsync(ConferenceConfig config, ConferenceMeetingRequest request, CancellationToken ct = default);

    /// <summary>A personal join link, when the provider signs links per person (BigBlueButton); null to use the stored link.</summary>
    Task<string?> PersonalJoinUrlAsync(ConferenceConfig config, ConferenceJoinRequest request, CancellationToken ct = default);

    /// <summary>Who attended and for how long (null when the provider has no participant report).</summary>
    Task<IReadOnlyList<ConferenceParticipant>?> ParticipantsAsync(ConferenceConfig config, string externalId, CancellationToken ct = default);

    Task<string?> RecordingUrlAsync(ConferenceConfig config, string externalId, CancellationToken ct = default);

    Task CancelAsync(ConferenceConfig config, string externalId, CancellationToken ct = default);

    /// <summary>Checks the credentials; throws with a readable message when they do not work.</summary>
    Task TestAsync(ConferenceConfig config, CancellationToken ct = default);
}

public sealed record ConferenceSettingsDto(Guid? TenantId, string Provider, bool IsEnabled, string? BaseUrl, bool HasSecret, string? AccountId, string? ClientId,
    string? OrganizerUserId, int JoinEarlyMinutes, bool IsComplete, bool IsDefault);

public sealed record SaveConferenceSettingsRequest(string Provider, bool IsEnabled, string? BaseUrl, string? Secret, string? AccountId, string? ClientId,
    string? OrganizerUserId, int JoinEarlyMinutes);

/// <summary>
/// Schedule a class. <c>MeetingUrl</c>: paste a link (any provider) instead of creating a meeting with the institution's provider.
/// </summary>
public sealed record ScheduleLiveSessionRequest(string Title, string? Description, DateTime StartsAt, int DurationMinutes, bool AutoAttendance,
    int MinimumMinutes, string? MeetingUrl);

public sealed record ScheduleFromTimetableRequest(Guid EntryId, int Weeks, bool AutoAttendance, int MinimumMinutes, string? MeetingUrl);

public sealed record LiveSessionDto(Guid Id, Guid CourseOfferingId, string CourseCode, string CourseTitle, string Title, string? Description, string Provider,
    DateTime StartsAt, DateTime EndsAt, int DurationMinutes, string Status, string State, bool CanJoin, string? RecordingUrl, bool AutoAttendance,
    int MinimumMinutes, DateTime? AttendanceTakenAt, Guid? TimetableEntryId, int Joined);

public sealed record LiveTimetableSlotDto(Guid EntryId, string Day, string Start, string End, string SubjectName, string? RoomNumber);

public sealed record LiveAttendanceRowDto(Guid StudentProfileId, string Name, string? RegistrationNumber, bool Joined, DateTime? FirstJoinedAt, int? Minutes, bool Present);

public sealed record LiveAttendanceReportDto(Guid SessionId, string Title, DateTime StartsAt, DateTime? AttendanceTakenAt, IReadOnlyList<LiveAttendanceRowDto> Students);

public sealed record LiveJoinDto(string Url);

public sealed record LiveResult<T>(T? Value, int Status, string? Error)
{
    public bool Ok => Status is >= 200 and < 300;
    public static LiveResult<T> Success(T value, int status = 200) => new(value, status, null);
    public static LiveResult<T> Fail(int status, string error) => new(default, status, error);
}

/// <summary>Plan M10.2: an iCalendar (RFC 5545) event for "add to calendar".</summary>
public static class LiveCalendar
{
    public static string Event(LiveSession s, string courseLabel, string joinPageUrl)
    {
        static string Esc(string v) => v.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
        static string Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc).ToString("yyyyMMdd'T'HHmmss'Z'");
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Tabsan EduSphere//Live classes//EN\r\nCALSCALE:GREGORIAN\r\nMETHOD:PUBLISH\r\n");
        sb.Append("BEGIN:VEVENT\r\n");
        sb.Append($"UID:{s.Id}@edusphere\r\n");
        sb.Append($"DTSTAMP:{Utc(DateTime.UtcNow)}\r\n");
        sb.Append($"DTSTART:{Utc(s.StartsAt)}\r\nDTEND:{Utc(s.EndsAt)}\r\n");
        sb.Append($"SUMMARY:{Esc($"{s.Title} ({courseLabel})")}\r\n");
        sb.Append($"DESCRIPTION:{Esc((s.Description is null ? "" : s.Description + "\n\n") + "Join: " + joinPageUrl)}\r\n");
        sb.Append($"URL:{joinPageUrl}\r\n");
        if (s.Status == LiveSessionStatus.Cancelled) sb.Append("STATUS:CANCELLED\r\n");
        sb.Append("BEGIN:VALARM\r\nTRIGGER:-PT15M\r\nACTION:DISPLAY\r\nDESCRIPTION:Live class starts in 15 minutes\r\nEND:VALARM\r\n");
        sb.Append("END:VEVENT\r\nEND:VCALENDAR\r\n");
        return sb.ToString();
    }
}

public sealed class LiveClassOptions
{
    public const string SectionName = "LiveClasses";
    /// <summary>Public address of the portal, used in calendar files and notifications (e.g. https://portal.uni.edu).</summary>
    public string? PortalBaseUrl { get; set; }
    public int WorkerIntervalSeconds { get; set; } = 60;
    /// <summary>Minutes before the start when students are reminded.</summary>
    public int ReminderMinutes { get; set; } = 15;
}
