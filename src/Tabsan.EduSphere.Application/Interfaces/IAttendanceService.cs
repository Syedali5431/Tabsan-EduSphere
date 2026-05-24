using Tabsan.EduSphere.Application.DTOs.Attendance;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Contract for recording, querying, and correcting student attendance.
/// Faculty marks daily attendance; students view their own records.
/// Admins can correct records and trigger threshold checks.
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Records attendance for a single student for one session.
    /// Returns false when a record already exists for the combination.
    /// </summary>
    Task<bool> MarkAsync(
        MarkAttendanceRequest request,
        Guid markedByUserId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Bulk-marks attendance for a full class for one session.
    /// Skips entries where a record already exists.
    /// Returns the count of newly inserted records.
    /// </summary>
    Task<int> BulkMarkAsync(
        BulkMarkAttendanceRequest request,
        Guid markedByUserId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Corrects an existing attendance record.
    /// Returns false when the record is not found.
    /// </summary>
    Task<bool> CorrectAsync(
        CorrectAttendanceRequest request,
        Guid correctedByUserId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns all attendance records for a course offering, optionally filtered by date range.</summary>
    Task<IReadOnlyList<AttendanceResponse>> GetByOfferingAsync(
        Guid courseOfferingId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns all attendance records for a student, optionally scoped to a single offering.</summary>
    Task<IReadOnlyList<AttendanceResponse>> GetByStudentAsync(
        Guid studentProfileId,
        Guid? courseOfferingId = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns the attendance percentage summary for a student in a course offering.</summary>
    Task<AttendanceSummaryResponse> GetSummaryAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all (studentProfileId, courseOfferingId, percent) tuples
    /// where the student's attendance is below the given threshold.
    /// Used by the alert job and admin reports.
    /// </summary>
    Task<IReadOnlyList<(Guid StudentProfileId, Guid CourseOfferingId, double AttendancePercent)>>
        GetBelowThresholdAsync(
            double thresholdPercent,
            Guid? tenantId = null,
            Guid? campusId = null,
            CancellationToken ct = default);
}
