using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Integrity;

/// <summary>
/// Phase 9: Data integrity verification — transaction safety and audit coverage.
/// Checks orphaned records, audit coverage gaps, and referential integrity.
/// </summary>
public class DataIntegrityService : IDataIntegrityService
{
    private readonly ApplicationDbContext _db;

    public DataIntegrityService(ApplicationDbContext db) => _db = db;

    public async Task<DataIntegrityReport> RunIntegrityCheckAsync(CancellationToken ct = default)
    {
        var report = new DataIntegrityReport { CheckedAt = DateTime.UtcNow, IsHealthy = true };
        var findings = report.Findings;

        // 1. Audit coverage — verify financial & academic actions are logged
        var auditActions = await _db.AuditLogs.Select(x => x.EntityName).Distinct().AsNoTracking().ToListAsync(ct);
        var criticalEntities = new[] { "User", "Result", "Enrollment", "PaymentReceipt", "StudentProfile", "Course", "Assignment" };
        foreach (var entity in criticalEntities)
        {
            var hasCoverage = auditActions.Any(a => string.Equals(a, entity, StringComparison.OrdinalIgnoreCase));
            findings.Add(new DataIntegrityFinding
            {
                Area = $"AuditCoverage:{entity}",
                Status = hasCoverage ? "OK" : "Warning",
                Message = hasCoverage ? $"Audit log entries exist for {entity}." : $"No audit log entries found for {entity}."
            });
            if (!hasCoverage) report.IsHealthy = false;
        }

        // 2. Orphaned users — active but no role
        var orphanedUsers = await _db.Users.CountAsync(x => x.IsActive && x.RoleId == 0, ct);
        findings.Add(new DataIntegrityFinding
        {
            Area = "OrphanedUsers",
            Status = orphanedUsers == 0 ? "OK" : "Error",
            Message = orphanedUsers == 0 ? "No orphaned active users." : $"{orphanedUsers} active user(s) with no role assignment."
        });
        if (orphanedUsers > 0) report.IsHealthy = false;

        // 3. Students without profiles
        var studentsWithoutProfiles = await _db.Users.CountAsync(x => x.IsActive && x.Role != null && x.Role.Name == "Student"
            && !_db.StudentProfiles.Any(s => s.UserId == x.Id), ct);
        findings.Add(new DataIntegrityFinding
        {
            Area = "StudentsWithoutProfiles",
            Status = studentsWithoutProfiles == 0 ? "OK" : "Warning",
            Message = studentsWithoutProfiles == 0 ? "All students have profiles." : $"{studentsWithoutProfiles} student(s) without profiles."
        });

        // 4. Course offerings in closed semesters
        var orphanedOfferings = await _db.CourseOfferings.CountAsync(x => x.IsOpen && x.Semester != null && x.Semester.IsClosed, ct);
        findings.Add(new DataIntegrityFinding
        {
            Area = "OpenOfferingsInClosedSemesters",
            Status = orphanedOfferings == 0 ? "OK" : "Warning",
            Message = orphanedOfferings == 0 ? "No open offerings in closed semesters." : $"{orphanedOfferings} open offering(s) in closed semesters."
        });

        // 5. Unpublished results with marks
        var draftResults = await _db.Results.CountAsync(x => !x.IsPublished && x.MarksObtained > 0, ct);
        findings.Add(new DataIntegrityFinding
        {
            Area = "DraftResults",
            Status = draftResults == 0 ? "OK" : "Info",
            Message = $"{draftResults} unpublished result(s) with marks (pending publish)."
        });

        // 6. Enrollments in dropped state
        var droppedEnrollments = await _db.Enrollments.CountAsync(x => x.Status == EnrollmentStatus.Dropped, ct);
        findings.Add(new DataIntegrityFinding
        {
            Area = "DroppedEnrollments",
            Status = "OK",
            Message = $"{droppedEnrollments} dropped enrollment record(s) (historical, consistent)."
        });

        // 7. Pending modification requests
        var pendingMods = await _db.TeacherModificationRequests.CountAsync(x => x.Status == ModificationRequestStatus.Pending, ct);
        findings.Add(new DataIntegrityFinding
        {
            Area = "PendingModificationRequests",
            Status = pendingMods == 0 ? "OK" : "Info",
            Message = pendingMods == 0 ? "No pending modification requests." : $"{pendingMods} pending modification request(s) requiring review."
        });

        return report;
    }
}
