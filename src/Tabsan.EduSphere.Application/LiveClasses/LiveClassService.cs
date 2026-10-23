using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.LiveClasses;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.LiveClasses;

/// <summary>
/// Plan M10 — live classes. Teachers schedule classes (ad hoc or from timetable slots) with the institution's conferencing
/// provider or a pasted link; students are notified and reminded, join from the dashboard or course, and attendance is
/// marked from who joined once the class ends (never overwriting a teacher's own marks). Recording links are collected
/// from the provider after the class.
/// </summary>
public sealed class LiveClassService
{
    private readonly ILiveClassRepository _repo;
    private readonly IReadOnlyDictionary<ConferenceProviderType, IConferenceProvider> _providers;
    private readonly IEncryptionService _encryption;
    private readonly IAuditService _audit;
    private readonly INotificationService? _notifications;
    private readonly LiveClassOptions _options;
    private readonly ILogger<LiveClassService> _logger;

    public LiveClassService(ILiveClassRepository repo, IEnumerable<IConferenceProvider> providers, IEncryptionService encryption, IAuditService audit,
        IOptions<LiveClassOptions> options, INotificationService? notifications = null, ILogger<LiveClassService>? logger = null)
    {
        _repo = repo;
        // The last registration of a type wins (as with DI itself), so a deployment or test can replace a provider.
        _providers = providers.GroupBy(p => p.Type).ToDictionary(g => g.Key, g => g.Last());
        _encryption = encryption;
        _audit = audit;
        _options = options.Value;
        _notifications = notifications;
        _logger = logger ?? NullLogger<LiveClassService>.Instance;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Settings
    // ═════════════════════════════════════════════════════════════════════════

    private async Task<ConferenceSetting> EffectiveSettingAsync(Guid? tenantId, CancellationToken ct)
        => (tenantId is not null ? await _repo.GetSettingAsync(tenantId, ct) : null) ?? await _repo.GetSettingAsync(null, ct) ?? new ConferenceSetting(null);

    private ConferenceConfig Config(ConferenceSetting s)
        => new(s.BaseUrl, s.SecretEncrypted is null ? null : _encryption.Decrypt(s.SecretEncrypted), s.AccountId, s.ClientId, s.OrganizerUserId);

    public async Task<ConferenceSettingsDto> GetSettingsAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var own = await _repo.GetSettingAsync(tenantId, ct);
        var s = own ?? await EffectiveSettingAsync(tenantId, ct);
        return new ConferenceSettingsDto(tenantId, s.Provider.ToString(), s.IsEnabled, s.BaseUrl, s.SecretEncrypted is not null, s.AccountId, s.ClientId,
            s.OrganizerUserId, s.JoinEarlyMinutes, s.IsComplete, own is null);
    }

    public async Task<LiveResult<ConferenceSettingsDto>> SaveSettingsAsync(Guid? tenantId, SaveConferenceSettingsRequest r, Guid actor, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ConferenceProviderType>(r.Provider, true, out var provider) || !Enum.IsDefined(provider))
            return LiveResult<ConferenceSettingsDto>.Fail(400, "Choose BigBlueButton, Zoom, Microsoft Teams or meeting links.");
        var setting = await _repo.GetSettingAsync(tenantId, ct);
        var isNew = setting is null;
        var providerChanged = setting is not null && setting.Provider != provider;
        setting ??= new ConferenceSetting(tenantId);
        try { setting.Configure(provider, r.IsEnabled, r.BaseUrl, r.AccountId, r.ClientId, r.OrganizerUserId, r.JoinEarlyMinutes); }
        catch (ArgumentException ex) { return LiveResult<ConferenceSettingsDto>.Fail(400, ex.Message); }
        if (!string.IsNullOrWhiteSpace(r.Secret)) setting.SetEncryptedSecret(_encryption.Encrypt(r.Secret.Trim()));
        else if (providerChanged || provider == ConferenceProviderType.ExternalLink) setting.SetEncryptedSecret(null); // never reuse one service's secret for another
        if (r.IsEnabled && !setting.IsComplete)
            return LiveResult<ConferenceSettingsDto>.Fail(400, provider switch
            {
                ConferenceProviderType.BigBlueButton => "Enter the BigBlueButton API address and shared secret.",
                ConferenceProviderType.Zoom => "Enter the Zoom account id, client id and client secret.",
                _ => "Enter the Microsoft Entra tenant id, client id, client secret and the organiser's user id."
            });
        if (isNew) _repo.Add(setting);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ConferenceSettingsChanged", "ConferenceSetting", setting.Id.ToString(), actorUserId: actor,
            newValuesJson: JsonSerializer.Serialize(new { r.Provider, r.IsEnabled, r.BaseUrl, r.AccountId, r.ClientId, r.OrganizerUserId, secretChanged = !string.IsNullOrWhiteSpace(r.Secret) })), ct);
        return LiveResult<ConferenceSettingsDto>.Success(await GetSettingsAsync(tenantId, ct));
    }

    public async Task<LiveResult<string>> TestAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var s = await EffectiveSettingAsync(tenantId, ct);
        if (s.Provider == ConferenceProviderType.ExternalLink) return LiveResult<string>.Success("Teachers paste a meeting link for each class — nothing to connect.");
        if (!s.IsComplete || !_providers.TryGetValue(s.Provider, out var provider)) return LiveResult<string>.Fail(400, "Fill in the provider's details first.");
        try
        {
            await provider.TestAsync(Config(s), ct);
            return LiveResult<string>.Success($"Connected to {Label(s.Provider)}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return LiveResult<string>.Fail(502, $"{Label(s.Provider)} refused: {ex.Message}");
        }
    }

    public static string Label(ConferenceProviderType p) => p switch
    {
        ConferenceProviderType.BigBlueButton => "BigBlueButton",
        ConferenceProviderType.Zoom => "Zoom",
        ConferenceProviderType.MicrosoftTeams => "Microsoft Teams",
        _ => "Meeting link"
    };

    // ═════════════════════════════════════════════════════════════════════════
    // Scheduling
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<LiveTimetableSlotDto>> TimetableSlotsAsync(Guid offeringId, CancellationToken ct = default)
        => (await _repo.GetTimetableSlotsAsync(offeringId, ct))
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new LiveTimetableSlotDto(s.EntryId, ((DayOfWeek)s.DayOfWeek).ToString(), s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"), s.SubjectName, s.RoomNumber))
            .ToList();

    public async Task<LiveResult<LiveSessionDto>> ScheduleAsync(Guid offeringId, ScheduleLiveSessionRequest r, Guid actor, CancellationToken ct = default)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        if (offering is null) return LiveResult<LiveSessionDto>.Fail(404, "Course not found.");
        var startsAt = Utc(r.StartsAt);
        if (startsAt < DateTime.UtcNow.AddHours(-1)) return LiveResult<LiveSessionDto>.Fail(400, "Choose a start time in the future.");
        var result = await CreateSessionAsync(offering, r.Title, r.Description, startsAt, r.DurationMinutes, r.AutoAttendance, r.MinimumMinutes, r.MeetingUrl, null, actor, ct);
        if (!result.Ok) return LiveResult<LiveSessionDto>.Fail(result.Status, result.Error!);
        await NotifyAsync(offering, $"Live class: {result.Value!.Title}",
            $"{offering.CourseCode} — {result.Value.StartsAt:ddd dd MMM yyyy HH:mm} UTC. Join from your dashboard or the course page.", ct);
        return LiveResult<LiveSessionDto>.Success(await ToDtoAsync(result.Value, offering, null, ct), 201);
    }

    /// <summary>Creates a class for the next <c>Weeks</c> occurrences of a timetable slot (dates already scheduled from that slot are skipped).</summary>
    public async Task<LiveResult<IReadOnlyList<LiveSessionDto>>> ScheduleFromTimetableAsync(Guid offeringId, ScheduleFromTimetableRequest r, Guid actor, DateTime now, CancellationToken ct = default)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        var slot = await _repo.GetTimetableSlotAsync(r.EntryId, ct);
        if (offering is null || slot is null || slot.OfferingId != offeringId) return LiveResult<IReadOnlyList<LiveSessionDto>>.Fail(404, "Choose a timetable slot of this course.");
        if (r.Weeks is < 1 or > 20) return LiveResult<IReadOnlyList<LiveSessionDto>>.Fail(400, "Schedule 1 to 20 weeks at a time.");
        var duration = (int)(slot.EndTime - slot.StartTime).TotalMinutes;
        if (duration < 5) return LiveResult<IReadOnlyList<LiveSessionDto>>.Fail(400, "This timetable slot has no length.");
        var existing = (await _repo.GetSessionsAsync(offeringId, ct)).Where(s => s.TimetableEntryId == slot.EntryId && s.Status == LiveSessionStatus.Scheduled)
            .Select(s => s.StartsAt).ToHashSet();
        var created = new List<LiveSession>();
        foreach (var start in NextOccurrences(slot, now, r.Weeks))
        {
            if (existing.Contains(start)) continue;
            var result = await CreateSessionAsync(offering, slot.SubjectName, slot.RoomNumber is null ? null : $"Timetable slot (room {slot.RoomNumber})",
                start, Math.Min(duration, LiveSession.MaxDurationMinutes), r.AutoAttendance, Math.Min(r.MinimumMinutes, duration), r.MeetingUrl, slot.EntryId, actor, ct);
            if (!result.Ok) return LiveResult<IReadOnlyList<LiveSessionDto>>.Fail(result.Status, result.Error!);
            created.Add(result.Value!);
        }
        if (created.Count > 0)
            await NotifyAsync(offering, $"Live classes scheduled: {offering.CourseCode}",
                $"{created.Count} live class(es) from {created[0].StartsAt:dd MMM} — {((DayOfWeek)slot.DayOfWeek)} {slot.StartTime:HH\\:mm} UTC.", ct);
        var dtos = new List<LiveSessionDto>();
        foreach (var s in created) dtos.Add(await ToDtoAsync(s, offering, null, ct));
        return LiveResult<IReadOnlyList<LiveSessionDto>>.Success(dtos, 201);
    }

    /// <summary>The next <paramref name="weeks"/> starts of a weekly slot (times are UTC, like other scheduled dates).</summary>
    public static IEnumerable<DateTime> NextOccurrences(LiveTimetableSlot slot, DateTime now, int weeks)
    {
        var first = now.Date.AddDays(((slot.DayOfWeek - (int)now.DayOfWeek) + 7) % 7).Add(slot.StartTime.ToTimeSpan());
        if (first <= now) first = first.AddDays(7);
        for (var i = 0; i < weeks; i++) yield return DateTime.SpecifyKind(first.AddDays(7 * i), DateTimeKind.Utc);
    }

    private async Task<LiveResult<LiveSession>> CreateSessionAsync(LiveOfferingInfo offering, string title, string? description, DateTime startsAt, int duration,
        bool autoAttendance, int minimumMinutes, string? meetingUrl, Guid? timetableEntryId, Guid actor, CancellationToken ct)
    {
        var setting = await EffectiveSettingAsync(offering.TenantId, ct);
        var pasted = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim();
        if (pasted is not null && (!Uri.TryCreate(pasted, UriKind.Absolute, out var u) || u.Scheme != Uri.UriSchemeHttps || pasted.Length > 2000))
            return LiveResult<LiveSession>.Fail(400, "Paste the meeting's https link.");
        var provider = pasted is not null ? ConferenceProviderType.ExternalLink : setting.Provider;
        if (pasted is null && (!setting.IsEnabled || provider == ConferenceProviderType.ExternalLink))
            return LiveResult<LiveSession>.Fail(400, "Paste the meeting link (no conferencing service is set up for this institution).");
        if (pasted is null && !setting.IsComplete) return LiveResult<LiveSession>.Fail(409, $"{Label(provider)} is not fully set up. Ask an administrator.");

        LiveSession session;
        try
        {
            session = new LiveSession(offering.OfferingId, title, description, provider, startsAt, duration, autoAttendance, minimumMinutes, timetableEntryId, actor,
                Password(), Password());
        }
        catch (ArgumentException ex) { return LiveResult<LiveSession>.Fail(400, ex.Message); }

        if (pasted is not null) session.SetMeeting(null, pasted, null);
        else
        {
            if (!_providers.TryGetValue(provider, out var p)) return LiveResult<LiveSession>.Fail(409, $"{Label(provider)} is not available.");
            try
            {
                var meeting = await p.CreateAsync(Config(setting), new ConferenceMeetingRequest(session.Id, session.Title, session.Description, session.StartsAt, session.DurationMinutes), ct);
                session.SetMeeting(meeting.ExternalId, meeting.JoinUrl, meeting.HostUrl is null ? null : _encryption.Encrypt(meeting.HostUrl));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Creating a {Provider} meeting failed.", provider);
                return LiveResult<LiveSession>.Fail(502, $"{Label(provider)} could not create the meeting: {ex.Message}");
            }
        }
        _repo.Add(session);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LiveSessionScheduled", "LiveSession", session.Id.ToString(), actorUserId: actor,
            newValuesJson: JsonSerializer.Serialize(new { session.CourseOfferingId, session.Title, session.StartsAt, session.DurationMinutes, Provider = provider.ToString() })), ct);
        return LiveResult<LiveSession>.Success(session);
    }

    public async Task<LiveResult<LiveSessionDto>> UpdateAsync(Guid sessionId, ScheduleLiveSessionRequest r, Guid actor, CancellationToken ct = default)
    {
        var s = await _repo.GetSessionAsync(sessionId, ct);
        if (s is null) return LiveResult<LiveSessionDto>.Fail(404, "Live class not found.");
        if (s.Status == LiveSessionStatus.Cancelled) return LiveResult<LiveSessionDto>.Fail(409, "This class was cancelled.");
        var moved = Utc(r.StartsAt) != s.StartsAt;
        try { s.Reschedule(r.Title, r.Description, Utc(r.StartsAt), r.DurationMinutes, r.AutoAttendance, r.MinimumMinutes); }
        catch (ArgumentException ex) { return LiveResult<LiveSessionDto>.Fail(400, ex.Message); }
        if (s.Provider == ConferenceProviderType.ExternalLink && !string.IsNullOrWhiteSpace(r.MeetingUrl))
        {
            if (!Uri.TryCreate(r.MeetingUrl.Trim(), UriKind.Absolute, out var u) || u.Scheme != Uri.UriSchemeHttps) return LiveResult<LiveSessionDto>.Fail(400, "Paste the meeting's https link.");
            s.SetMeeting(null, r.MeetingUrl.Trim(), null);
        }
        await _repo.SaveChangesAsync(ct);
        var offering = (await _repo.GetOfferingAsync(s.CourseOfferingId, ct))!;
        if (moved) await NotifyAsync(offering, $"Live class moved: {s.Title}", $"{offering.CourseCode} — now {s.StartsAt:ddd dd MMM yyyy HH:mm} UTC.", ct);
        await _audit.LogAsync(new AuditLog("LiveSessionUpdated", "LiveSession", s.Id.ToString(), actorUserId: actor), ct);
        return LiveResult<LiveSessionDto>.Success(await ToDtoAsync(s, offering, null, ct));
    }

    public async Task<LiveResult<bool>> CancelAsync(Guid sessionId, Guid actor, CancellationToken ct = default)
    {
        var s = await _repo.GetSessionAsync(sessionId, ct);
        if (s is null) return LiveResult<bool>.Fail(404, "Live class not found.");
        if (s.Status == LiveSessionStatus.Cancelled) return LiveResult<bool>.Success(true);
        s.Cancel();
        await _repo.SaveChangesAsync(ct);
        var offering = (await _repo.GetOfferingAsync(s.CourseOfferingId, ct))!;
        if (s.ExternalMeetingId is not null && _providers.TryGetValue(s.Provider, out var p))
        {
            try { await p.CancelAsync(Config(await EffectiveSettingAsync(offering.TenantId, ct)), s.ExternalMeetingId, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogWarning(ex, "Cancelling meeting {Id} at the provider failed.", s.ExternalMeetingId); }
        }
        if (s.StartsAt > DateTime.UtcNow) await NotifyAsync(offering, $"Live class cancelled: {s.Title}", $"{offering.CourseCode} — {s.StartsAt:ddd dd MMM yyyy HH:mm} UTC is cancelled.", ct);
        await _audit.LogAsync(new AuditLog("LiveSessionCancelled", "LiveSession", s.Id.ToString(), actorUserId: actor), ct);
        return LiveResult<bool>.Success(true);
    }

    private static string Password() => Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();

    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

    // ═════════════════════════════════════════════════════════════════════════
    // Reading and joining
    // ═════════════════════════════════════════════════════════════════════════

    private async Task<LiveSessionDto> ToDtoAsync(LiveSession s, LiveOfferingInfo offering, int? earlyMinutes, CancellationToken ct)
    {
        var early = earlyMinutes ?? (await EffectiveSettingAsync(offering.TenantId, ct)).JoinEarlyMinutes;
        var now = DateTime.UtcNow;
        var state = s.Status == LiveSessionStatus.Cancelled ? "Cancelled" : now < s.StartsAt ? "Upcoming" : now <= s.EndsAt ? "Live" : "Ended";
        var joined = (await _repo.GetAttendanceAsync(s.Id, ct)).Count(a => a.StudentProfileId is not null);
        return new LiveSessionDto(s.Id, s.CourseOfferingId, offering.CourseCode, offering.CourseTitle, s.Title, s.Description, Label(s.Provider),
            s.StartsAt, s.EndsAt, s.DurationMinutes, s.Status.ToString(), state, s.IsJoinable(now, early), s.RecordingUrl, s.AutoAttendance, s.MinimumMinutes,
            s.AttendanceTakenAt, s.TimetableEntryId, joined);
    }

    public async Task<IReadOnlyList<LiveSessionDto>> SessionsAsync(Guid offeringId, CancellationToken ct = default)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        if (offering is null) return [];
        var early = (await EffectiveSettingAsync(offering.TenantId, ct)).JoinEarlyMinutes;
        var result = new List<LiveSessionDto>();
        foreach (var s in (await _repo.GetSessionsAsync(offeringId, ct)).OrderBy(s => s.StartsAt)) result.Add(await ToDtoAsync(s, offering, early, ct));
        return result;
    }

    /// <summary>The next two weeks' classes (and those on now) of the user's courses: enrolled ones for students, taught ones for teachers.</summary>
    public async Task<IReadOnlyList<LiveSessionDto>> UpcomingAsync(Guid userId, Guid? studentProfileId, DateTime now, CancellationToken ct = default)
    {
        var offeringIds = studentProfileId is { } sp ? await _repo.GetStudentOfferingIdsAsync(sp, ct) : await _repo.GetTeachingOfferingIdsAsync(userId, ct);
        if (offeringIds.Count == 0) return [];
        var offerings = (await _repo.GetOfferingsAsync(offeringIds, ct)).ToDictionary(o => o.OfferingId);
        var sessions = await _repo.GetSessionsAsync(offeringIds, now.AddMinutes(-LiveSession.MaxDurationMinutes - 30), now.AddDays(14), ct);
        var result = new List<LiveSessionDto>();
        foreach (var s in sessions.Where(s => s.Status == LiveSessionStatus.Scheduled && s.EndsAt.AddMinutes(30) >= now).OrderBy(s => s.StartsAt))
            if (offerings.TryGetValue(s.CourseOfferingId, out var o)) result.Add(await ToDtoAsync(s, o, null, ct));
        return result;
    }

    public async Task<Guid?> OfferingOfSessionAsync(Guid sessionId, CancellationToken ct = default) => (await _repo.GetSessionAsync(sessionId, ct))?.CourseOfferingId;

    /// <summary>
    /// The link to open. Students must be enrolled and are recorded as joined; staff join as moderators (BigBlueButton) or
    /// hosts (Zoom start link). Joining opens shortly before the start and closes 30 minutes after the end.
    /// </summary>
    public async Task<LiveResult<LiveJoinDto>> JoinAsync(Guid sessionId, Guid userId, Guid? studentProfileId, string displayName, bool isStaff, DateTime now, CancellationToken ct = default)
    {
        var s = await _repo.GetSessionAsync(sessionId, ct);
        if (s is null) return LiveResult<LiveJoinDto>.Fail(404, "Live class not found.");
        if (s.Status == LiveSessionStatus.Cancelled) return LiveResult<LiveJoinDto>.Fail(409, "This class was cancelled.");
        var offering = (await _repo.GetOfferingAsync(s.CourseOfferingId, ct))!;
        if (!isStaff && (studentProfileId is not { } sp || !await _repo.IsEnrolledAsync(s.CourseOfferingId, sp, ct)))
            return LiveResult<LiveJoinDto>.Fail(403, "You are not enrolled in this course.");
        var setting = await EffectiveSettingAsync(offering.TenantId, ct);
        if (!s.IsJoinable(now, setting.JoinEarlyMinutes))
            return LiveResult<LiveJoinDto>.Fail(409, now < s.StartsAt
                ? $"Joining opens {setting.JoinEarlyMinutes} minutes before the start ({s.StartsAt:dd MMM HH:mm} UTC)."
                : "This class has ended.");

        string? url = null;
        if (isStaff && s.HostUrlEncrypted is not null) url = _encryption.Decrypt(s.HostUrlEncrypted);
        if (url is null && s.ExternalMeetingId is not null && _providers.TryGetValue(s.Provider, out var p))
        {
            try
            {
                url = await p.PersonalJoinUrlAsync(Config(setting), new ConferenceJoinRequest(s.ExternalMeetingId, $"{offering.CourseCode}: {s.Title}", displayName,
                    userId.ToString(), isStaff, s.ModeratorPassword, s.AttendeePassword, s.DurationMinutes), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Opening the {Provider} meeting failed.", s.Provider);
                return LiveResult<LiveJoinDto>.Fail(502, $"{Label(s.Provider)} could not open the meeting: {ex.Message}");
            }
        }
        url ??= s.JoinUrl;
        if (url is null) return LiveResult<LiveJoinDto>.Fail(409, "This class has no meeting link.");

        if (!isStaff)
        {
            var attendance = await _repo.GetAttendanceAsync(s.Id, userId, ct);
            if (attendance is null) _repo.Add(new LiveSessionAttendance(s.Id, userId, studentProfileId, now));
            else attendance.Joined(now);
            await _repo.SaveChangesAsync(ct);
        }
        return LiveResult<LiveJoinDto>.Success(new LiveJoinDto(url));
    }

    public async Task<LiveResult<string>> CalendarAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await _repo.GetSessionAsync(sessionId, ct);
        if (s is null) return LiveResult<string>.Fail(404, "Live class not found.");
        var o = (await _repo.GetOfferingAsync(s.CourseOfferingId, ct))!;
        var portal = string.IsNullOrWhiteSpace(_options.PortalBaseUrl) ? "" : _options.PortalBaseUrl.TrimEnd('/');
        return LiveResult<string>.Success(LiveCalendar.Event(s, $"{o.CourseCode} {o.CourseTitle}", $"{portal}/Portal/LiveClasses?offeringId={o.OfferingId}"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Attendance (Plan M10.3)
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<LiveAttendanceReportDto?> AttendanceReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await _repo.GetSessionAsync(sessionId, ct);
        if (s is null) return null;
        var joins = (await _repo.GetAttendanceAsync(sessionId, ct)).Where(a => a.StudentProfileId is not null).ToDictionary(a => a.StudentProfileId!.Value);
        var rows = (await _repo.GetEnrolledStudentsAsync(s.CourseOfferingId, ct)).OrderBy(st => st.Name).Select(st =>
        {
            var j = joins.GetValueOrDefault(st.StudentProfileId);
            return new LiveAttendanceRowDto(st.StudentProfileId, st.Name, st.RegistrationNumber, j is not null, j?.FirstJoinedAt, j?.Minutes, IsPresent(s, j));
        }).ToList();
        return new LiveAttendanceReportDto(s.Id, s.Title, s.StartsAt, s.AttendanceTakenAt, rows);
    }

    private static bool IsPresent(LiveSession s, LiveSessionAttendance? join)
        => join is not null && (s.MinimumMinutes == 0 || join.Minutes is null || join.Minutes >= s.MinimumMinutes);

    /// <summary>
    /// Marks attendance for the class date: present when the student joined (and stayed the minimum time when the provider
    /// reports minutes), absent otherwise. Students who already have a mark for that date keep it.
    /// </summary>
    public async Task<LiveResult<int>> TakeAttendanceAsync(Guid sessionId, Guid? actor, DateTime now, CancellationToken ct = default)
    {
        var s = await _repo.GetSessionAsync(sessionId, ct);
        if (s is null) return LiveResult<int>.Fail(404, "Live class not found.");
        if (s.Status == LiveSessionStatus.Cancelled) return LiveResult<int>.Fail(409, "This class was cancelled.");
        if (now < s.StartsAt) return LiveResult<int>.Fail(409, "The class has not started yet.");
        var offering = (await _repo.GetOfferingAsync(s.CourseOfferingId, ct))!;
        var students = await _repo.GetEnrolledStudentsAsync(s.CourseOfferingId, ct);
        var joins = (await _repo.GetAttendanceAsync(s.Id, ct)).Where(a => a.StudentProfileId is not null).ToDictionary(a => a.StudentProfileId!.Value);

        // The provider's report adds minutes (and catches students who joined with the link directly).
        if (s.ExternalMeetingId is not null && _providers.TryGetValue(s.Provider, out var p))
        {
            try
            {
                var participants = await p.ParticipantsAsync(Config(await EffectiveSettingAsync(offering.TenantId, ct)), s.ExternalMeetingId, ct);
                foreach (var part in participants ?? [])
                {
                    var st = students.FirstOrDefault(x => x.Email is not null && string.Equals(x.Email, part.Email, StringComparison.OrdinalIgnoreCase));
                    if (st is null) continue;
                    if (!joins.TryGetValue(st.StudentProfileId, out var j))
                    {
                        j = new LiveSessionAttendance(s.Id, st.UserId, st.StudentProfileId, s.StartsAt);
                        _repo.Add(j);
                        joins[st.StudentProfileId] = j;
                    }
                    j.SetMinutes((j.Minutes ?? 0) + part.Minutes);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Reading the participant report of live class {Id} failed; using portal joins only.", s.Id);
            }
        }

        var already = await _repo.GetStudentsWithAttendanceAsync(s.CourseOfferingId, s.StartsAt.Date, ct);
        var marked = 0;
        foreach (var st in students.Where(st => !already.Contains(st.StudentProfileId)))
        {
            joins.TryGetValue(st.StudentProfileId, out var j);
            var present = IsPresent(s, j);
            _repo.Add(new AttendanceRecord(st.StudentProfileId, s.CourseOfferingId, s.StartsAt, present ? AttendanceStatus.Present : AttendanceStatus.Absent,
                actor ?? s.CreatedByUserId, present
                    ? $"Live class “{s.Title}”: joined{(j!.Minutes is { } m ? $", {m} min" : "")}"
                    : $"Live class “{s.Title}”: did not join"));
            marked++;
        }
        s.MarkAttendanceTaken(now);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LiveSessionAttendance", "LiveSession", s.Id.ToString(), actorUserId: actor,
            newValuesJson: $"{{\"marked\":{marked},\"kept\":{already.Count}}}"), ct);
        return LiveResult<int>.Success(marked);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Worker
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Reminders before classes, automatic attendance after them, and recording links.</summary>
    public async Task<(int Reminded, int Attendance, int Recordings)> RunAsync(DateTime now, CancellationToken ct = default)
    {
        var reminded = 0;
        foreach (var s in await _repo.GetSessionsNeedingReminderAsync(now, now.AddMinutes(_options.ReminderMinutes), ct))
        {
            var o = await _repo.GetOfferingAsync(s.CourseOfferingId, ct);
            if (o is null) continue;
            await NotifyAsync(o, $"Starting soon: {s.Title}", $"{o.CourseCode} live class starts at {s.StartsAt:HH:mm} UTC. Join from your dashboard.", ct);
            s.MarkReminderSent(now);
            await _repo.SaveChangesAsync(ct);
            reminded++;
        }

        var attendance = 0;
        foreach (var s in await _repo.GetSessionsNeedingAttendanceAsync(now.AddMinutes(-10), ct))
            if ((await TakeAttendanceAsync(s.Id, null, now, ct)).Ok) attendance++;

        var recordings = 0;
        foreach (var s in await _repo.GetSessionsWithoutRecordingAsync(now.AddDays(-1), now.AddMinutes(-5), ct))
        {
            if (s.ExternalMeetingId is null || !_providers.TryGetValue(s.Provider, out var p)) continue;
            var o = await _repo.GetOfferingAsync(s.CourseOfferingId, ct);
            if (o is null) continue;
            try
            {
                if (await p.RecordingUrlAsync(Config(await EffectiveSettingAsync(o.TenantId, ct)), s.ExternalMeetingId, ct) is { } url
                    && Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps)
                {
                    s.SetRecording(url);
                    await _repo.SaveChangesAsync(ct);
                    recordings++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogDebug(ex, "No recording yet for {Id}.", s.Id); }
        }
        return (reminded, attendance, recordings);
    }

    private async Task NotifyAsync(LiveOfferingInfo offering, string title, string body, CancellationToken ct)
    {
        if (_notifications is null) return;
        var users = (await _repo.GetEnrolledStudentsAsync(offering.OfferingId, ct)).Select(s => s.UserId).Distinct().ToList();
        if (users.Count == 0) return;
        try { await _notifications.SendSystemAsync(title, body, NotificationType.LiveClass, users, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogWarning(ex, "Live class notification failed."); }
    }
}
