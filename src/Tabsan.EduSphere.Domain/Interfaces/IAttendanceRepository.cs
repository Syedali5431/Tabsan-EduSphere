using Tabsan.EduSphere.Domain.Attendance;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Persistence contract for attendance records.
/// </summary>
public interface IAttendanceRepository
{
    /// <summary>
    /// Returns the attendance record for a specific student / offering / date, or null.
    /// </summary>
    Task<AttendanceRecord?> GetAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        DateTime date,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns true when an attendance record already exists for the combination.</summary>
    Task<bool> ExistsAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        DateTime date,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns all attendance records for a course offering, optionally filtered to a date range.</summary>
    Task<IReadOnlyList<AttendanceRecord>> GetByOfferingAsync(
        Guid courseOfferingId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns all attendance records for a student across all offerings, optionally filtered by offering.</summary>
    Task<IReadOnlyList<AttendanceRecord>> GetByStudentAsync(
        Guid studentProfileId,
        Guid? courseOfferingId = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the total number of sessions and number of sessions attended (Present or Late)
    /// for a student in a specific offering, used for percentage calculation.
    /// </summary>
    Task<(int TotalSessions, int AttendedSessions)> GetAttendanceSummaryAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all students with attendance below <paramref name="thresholdPercent"/> for any offering.
    /// Used by the <see cref="AttendanceAlertJob"/>.
    /// Returns tuples of (studentProfileId, courseOfferingId, attendancePercent).
    /// </summary>
    Task<IReadOnlyList<(Guid StudentProfileId, Guid CourseOfferingId, double AttendancePercent)>> GetBelowThresholdAsync(
        double thresholdPercent,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Queues an attendance record for insertion.</summary>
    Task AddAsync(AttendanceRecord record, CancellationToken ct = default);

    /// <summary>Queues multiple records for bulk insertion (marking a full class for one session).</summary>
    Task AddRangeAsync(IEnumerable<AttendanceRecord> records, CancellationToken ct = default);

    /// <summary>Marks an attendance record as modified (correction).</summary>
    void Update(AttendanceRecord record);

    /// <summary>Commits pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
