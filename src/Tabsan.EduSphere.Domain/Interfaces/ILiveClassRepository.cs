using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.LiveClasses;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Plan M10 — live classes.

public sealed record LiveOfferingInfo(Guid OfferingId, Guid CourseId, Guid SemesterId, string CourseCode, string CourseTitle, Guid? TenantId, Guid? CampusId, Guid? FacultyUserId);

public sealed record LiveStudent(Guid StudentProfileId, Guid UserId, string Name, string? Email, string? RegistrationNumber);

public sealed record LiveTimetableSlot(Guid EntryId, Guid OfferingId, int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string SubjectName, string? RoomNumber);

public interface ILiveClassRepository
{
    Task<ConferenceSetting?> GetSettingAsync(Guid? tenantId, CancellationToken ct = default);
    void Add(ConferenceSetting setting);

    Task<LiveOfferingInfo?> GetOfferingAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyList<LiveOfferingInfo>> GetOfferingsAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetStudentOfferingIdsAsync(Guid studentProfileId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetTeachingOfferingIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<LiveStudent>> GetEnrolledStudentsAsync(Guid offeringId, CancellationToken ct = default);
    Task<bool> IsEnrolledAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Timetable slots of the offering's course in the offering's semester.</summary>
    Task<IReadOnlyList<LiveTimetableSlot>> GetTimetableSlotsAsync(Guid offeringId, CancellationToken ct = default);
    Task<LiveTimetableSlot?> GetTimetableSlotAsync(Guid entryId, CancellationToken ct = default);

    Task<IReadOnlyList<LiveSession>> GetSessionsAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyList<LiveSession>> GetSessionsAsync(IReadOnlyCollection<Guid> offeringIds, DateTime from, DateTime to, CancellationToken ct = default);
    Task<LiveSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    /// <summary>Scheduled sessions starting between <paramref name="from"/> and <paramref name="to"/> with no reminder sent yet.</summary>
    Task<IReadOnlyList<LiveSession>> GetSessionsNeedingReminderAsync(DateTime from, DateTime to, CancellationToken ct = default);
    /// <summary>Ended sessions with automatic attendance not taken yet.</summary>
    Task<IReadOnlyList<LiveSession>> GetSessionsNeedingAttendanceAsync(DateTime endedBefore, CancellationToken ct = default);
    /// <summary>Sessions that ended within the last day and have no recording link yet.</summary>
    Task<IReadOnlyList<LiveSession>> GetSessionsWithoutRecordingAsync(DateTime endedAfter, DateTime endedBefore, CancellationToken ct = default);

    Task<IReadOnlyList<LiveSessionAttendance>> GetAttendanceAsync(Guid sessionId, CancellationToken ct = default);
    Task<LiveSessionAttendance?> GetAttendanceAsync(Guid sessionId, Guid userId, CancellationToken ct = default);

    /// <summary>Students who already have an attendance record for the offering on that date (teachers' marks are never overwritten).</summary>
    Task<IReadOnlySet<Guid>> GetStudentsWithAttendanceAsync(Guid offeringId, DateTime date, CancellationToken ct = default);
    void Add(AttendanceRecord record);

    void Add(LiveSession session);
    void Add(LiveSessionAttendance attendance);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
