using Tabsan.EduSphere.Application.DTOs.Attendance;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Attendance;

/// <summary>
/// Orchestrates attendance marking, correction, and querying.
/// Business rules enforced here:
///   - At most one record per (student, offering, date) — skip duplicates on bulk insert.
///   - Date is normalised to UTC date (time stripped) before persistence.
///   - Corrections are allowed by faculty and admins; the correcting user is recorded.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _repo;
    public AttendanceService(IAttendanceRepository repo) => _repo = repo;

    /// <summary>
    /// Records attendance for a single student for one session.
    /// Returns false when a record already exists for the (student, offering, date) combination.
    /// </summary>
    public async Task<bool> MarkAsync(
        MarkAttendanceRequest request,
        Guid markedByUserId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        if (await _repo.ExistsAsync(request.StudentProfileId, request.CourseOfferingId, request.Date, tenantId, campusId, ct))
            return false;

        var record = new AttendanceRecord(
            request.StudentProfileId,
            request.CourseOfferingId,
            request.Date,
            request.Status,
            markedByUserId,
            request.Remarks);

        await _repo.AddAsync(record, ct);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Bulk-marks attendance for an entire class for one session.
    /// Entries with existing records are silently skipped.
    /// Returns the number of records successfully inserted.
    /// </summary>
    public async Task<int> BulkMarkAsync(
        BulkMarkAttendanceRequest request,
        Guid markedByUserId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var toInsert = new List<AttendanceRecord>();

        foreach (var entry in request.Entries)
        {
            if (await _repo.ExistsAsync(entry.StudentProfileId, request.CourseOfferingId, request.Date, tenantId, campusId, ct))
                continue;

            toInsert.Add(new AttendanceRecord(
                entry.StudentProfileId,
                request.CourseOfferingId,
                request.Date,
                entry.Status,
                markedByUserId,
                entry.Remarks));
        }

        if (toInsert.Count == 0) return 0;

        await _repo.AddRangeAsync(toInsert, ct);
        await _repo.SaveChangesAsync(ct);
        return toInsert.Count;
    }

    /// <summary>
    /// Corrects an existing attendance record.
    /// Returns false when the record is not found.
    /// </summary>
    public async Task<bool> CorrectAsync(
        CorrectAttendanceRequest request,
        Guid correctedByUserId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var record = await _repo.GetAsync(request.StudentProfileId, request.CourseOfferingId, request.Date, tenantId, campusId, ct);
        if (record is null) return false;

        record.Correct(request.NewStatus, correctedByUserId, request.Remarks);
        _repo.Update(record);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Returns all attendance records for a course offering, optionally filtered by date range.</summary>
    public async Task<IReadOnlyList<AttendanceResponse>> GetByOfferingAsync(
        Guid courseOfferingId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var records = await _repo.GetByOfferingAsync(courseOfferingId, from, to, tenantId, campusId, ct);
        return records.Select(ToResponse).ToList();
    }

    /// <summary>Returns all attendance records for a student, optionally scoped to one offering.</summary>
    public async Task<IReadOnlyList<AttendanceResponse>> GetByStudentAsync(
        Guid studentProfileId,
        Guid? courseOfferingId = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var records = await _repo.GetByStudentAsync(studentProfileId, courseOfferingId, tenantId, campusId, ct);
        return records.Select(ToResponse).ToList();
    }

    /// <summary>Returns the attendance percentage summary for a student in a course offering.</summary>
    public async Task<AttendanceSummaryResponse> GetSummaryAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var (total, attended) = await _repo.GetAttendanceSummaryAsync(studentProfileId, courseOfferingId, tenantId, campusId, ct);
        var percent = total > 0 ? Math.Round((double)attended / total * 100, 2) : 0.0;
        return new AttendanceSummaryResponse(studentProfileId, courseOfferingId, total, attended, percent);
    }

    /// <summary>Returns all students below the attendance threshold — delegates to the repository query.</summary>
    public Task<IReadOnlyList<(Guid StudentProfileId, Guid CourseOfferingId, double AttendancePercent)>>
        GetBelowThresholdAsync(double thresholdPercent, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => _repo.GetBelowThresholdAsync(thresholdPercent, tenantId, campusId, ct);

    // ── Mapping helpers ───────────────────────────────────────────────────────

    /// <summary>Maps a domain AttendanceRecord to an AttendanceResponse DTO.</summary>
    private static AttendanceResponse ToResponse(AttendanceRecord r) =>
        new(r.Id, r.StudentProfileId, r.CourseOfferingId, r.Date, r.Status.ToString(), r.Remarks, r.CreatedAt);
}
