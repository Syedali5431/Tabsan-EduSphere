using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>EF Core implementation of IAssignmentRepository.</summary>
public class AssignmentRepository : IAssignmentRepository
{
    private readonly ApplicationDbContext _db;
    public AssignmentRepository(ApplicationDbContext db) => _db = db;

    // ── Assignments ───────────────────────────────────────────────────────────

    /// <summary>Returns all non-deleted assignments for the offering, ordered by due date ascending.</summary>
    public async Task<IReadOnlyList<Assignment>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = includeInactive
            ? _db.Assignments.IgnoreQueryFilters()
            : _db.Assignments;

        return await query
            .Where(a => a.CourseOfferingId == courseOfferingId)
            .OrderBy(a => a.DueDate)
            .ToListAsync(ct);
    }

    /// <summary>Returns the assignment by ID (respecting soft-delete filter), or null.</summary>
    public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Assignment?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken ct = default)
        => _db.Assignments
              .IgnoreQueryFilters()
              .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<(Guid? TenantId, Guid? CampusId)?> GetOfferingScopeAsync(Guid courseOfferingId, CancellationToken ct = default)
        => _db.CourseOfferings
            .Where(o => o.Id == courseOfferingId)
            .Select(o => ((Guid? TenantId, Guid? CampusId)?)new ValueTuple<Guid?, Guid?>(o.TenantId, o.CampusId))
            .FirstOrDefaultAsync(ct);

    public Task<(Guid? TenantId, Guid? CampusId)?> GetAssignmentScopeAsync(Guid assignmentId, CancellationToken ct = default)
        => _db.Assignments
            .IgnoreQueryFilters()
            .Where(a => a.Id == assignmentId)
            .Join(
                _db.CourseOfferings,
                a => a.CourseOfferingId,
                o => o.Id,
                (_, o) => ((Guid? TenantId, Guid? CampusId)?)new ValueTuple<Guid?, Guid?>(o.TenantId, o.CampusId))
            .FirstOrDefaultAsync(ct);

    /// <summary>Returns true when the offering already has an assignment with the same title.</summary>
    public Task<bool> TitleExistsAsync(Guid courseOfferingId, string title, CancellationToken ct = default)
        => _db.Assignments.AnyAsync(a => a.CourseOfferingId == courseOfferingId && a.Title == title.Trim(), ct);

    /// <summary>Queues the assignment for insertion.</summary>
    public async Task AddAsync(Assignment assignment, CancellationToken ct = default)
        => await _db.Assignments.AddAsync(assignment, ct);

    /// <summary>Marks the assignment as modified.</summary>
    public void Update(Assignment assignment) => _db.Assignments.Update(assignment);

    // ── Submissions ───────────────────────────────────────────────────────────

    /// <summary>Returns the submission for the student+assignment pair, or null.</summary>
    public Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid studentProfileId, CancellationToken ct = default)
        => _db.AssignmentSubmissions
              .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentProfileId == studentProfileId, ct);

    /// <summary>Returns all submissions for an assignment with student profile loaded.</summary>
    public async Task<IReadOnlyList<AssignmentSubmission>> GetSubmissionsByAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
        => await _db.AssignmentSubmissions
                    .Where(s => s.AssignmentId == assignmentId)
                    .OrderBy(s => s.SubmittedAt)
                    .ToListAsync(ct);

    /// <summary>Returns all submissions made by a student, ordered by submission date.</summary>
    public async Task<IReadOnlyList<AssignmentSubmission>> GetSubmissionsByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.AssignmentSubmissions
                    .Include(s => s.Assignment)
                    .Where(s => s.StudentProfileId == studentProfileId)
                    .OrderByDescending(s => s.SubmittedAt)
                    .ToListAsync(ct);

    /// <summary>Returns true when the student has already submitted for this assignment.</summary>
    public Task<bool> HasSubmittedAsync(Guid assignmentId, Guid studentProfileId, CancellationToken ct = default)
        => _db.AssignmentSubmissions.AnyAsync(s => s.AssignmentId == assignmentId && s.StudentProfileId == studentProfileId, ct);

    /// <summary>Returns the total number of submissions received for an assignment.</summary>
    public Task<int> GetSubmissionCountAsync(Guid assignmentId, CancellationToken ct = default)
        => _db.AssignmentSubmissions.CountAsync(s => s.AssignmentId == assignmentId, ct);

    /// <summary>Queues the submission for insertion.</summary>
    public async Task AddSubmissionAsync(AssignmentSubmission submission, CancellationToken ct = default)
        => await _db.AssignmentSubmissions.AddAsync(submission, ct);

    /// <summary>Marks the submission as modified (graded/rejected).</summary>
    public void UpdateSubmission(AssignmentSubmission submission) => _db.AssignmentSubmissions.Update(submission);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>EF Core implementation of IResultRepository.</summary>
public class ResultRepository : IResultRepository
{
    private readonly ApplicationDbContext _db;
    public ResultRepository(ApplicationDbContext db) => _db = db;

    // ── Results ───────────────────────────────────────────────────────────────

    /// <summary>Returns the specific result row for student+offering+type, or null.</summary>
    public Task<Result?> GetAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default)
        => _db.Results.FirstOrDefaultAsync(r =>
            r.StudentProfileId == studentProfileId &&
            r.CourseOfferingId == courseOfferingId &&
            r.ResultType == resultType, ct);

    /// <summary>Returns all result rows for the student (draft and published), ordered by offering then type.</summary>
    public async Task<IReadOnlyList<Result>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.Results
                    .Where(r => r.StudentProfileId == studentProfileId)
                    .OrderBy(r => r.CourseOfferingId)
                    .ThenBy(r => r.ResultType)
                    .ToListAsync(ct);

    /// <summary>Returns only published results for the student (what the student sees).</summary>
    public async Task<IReadOnlyList<Result>> GetPublishedByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.Results
                    .Where(r => r.StudentProfileId == studentProfileId && r.IsPublished)
                    .OrderBy(r => r.CourseOfferingId)
                    .ThenBy(r => r.ResultType)
                    .ToListAsync(ct);

    /// <summary>Returns all results for a course offering (published and draft) — faculty/admin view.</summary>
    public async Task<IReadOnlyList<Result>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => await _db.Results
                    .Where(r => r.CourseOfferingId == courseOfferingId)
                    .OrderBy(r => r.StudentProfileId)
                    .ThenBy(r => r.ResultType)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<Result>> GetByStudentAndOfferingAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
        => await _db.Results
                    .Where(r => r.StudentProfileId == studentProfileId && r.CourseOfferingId == courseOfferingId)
                    .OrderBy(r => r.ResultType)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<Result>> GetByStudentAndSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default)
        => await _db.Results
                    .Where(r => r.StudentProfileId == studentProfileId
                             && _db.CourseOfferings.Any(o => o.Id == r.CourseOfferingId && o.SemesterId == semesterId))
                    .OrderBy(r => r.CourseOfferingId)
                    .ThenBy(r => r.ResultType)
                    .ToListAsync(ct);

    /// <summary>Returns true when a result row already exists for the combination.</summary>
    public Task<bool> ExistsAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default)
        => _db.Results.AnyAsync(r =>
            r.StudentProfileId == studentProfileId &&
            r.CourseOfferingId == courseOfferingId &&
            r.ResultType == resultType, ct);

    /// <summary>Queues a new result for insertion.</summary>
    public async Task AddAsync(Result result, CancellationToken ct = default)
        => await _db.Results.AddAsync(result, ct);

    /// <summary>Queues multiple result rows for bulk insertion.</summary>
    public async Task AddRangeAsync(IEnumerable<Result> results, CancellationToken ct = default)
        => await _db.Results.AddRangeAsync(results, ct);

    /// <summary>Marks a result as modified (publish or Admin correction).</summary>
    public void Update(Result result) => _db.Results.Update(result);

    public async Task<IReadOnlyList<ResultComponentRule>> GetActiveComponentRulesAsync(CancellationToken ct = default)
        => await _db.ResultComponentRules
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.DisplayOrder)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(CancellationToken ct = default)
        => await _db.ResultComponentRules
                    .OrderBy(r => r.DisplayOrder)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(CancellationToken ct = default)
        => await _db.GpaScaleRules
                    .OrderBy(r => r.DisplayOrder)
                    .ToListAsync(ct);

    public async Task ReplaceCalculationRulesAsync(IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default)
    {
        var existingGpaRules = await _db.GpaScaleRules.ToListAsync(ct);
        var existingComponentRules = await _db.ResultComponentRules.ToListAsync(ct);

        _db.GpaScaleRules.RemoveRange(existingGpaRules);
        _db.ResultComponentRules.RemoveRange(existingComponentRules);

        await _db.GpaScaleRules.AddRangeAsync(gpaScaleRules, ct);
        await _db.ResultComponentRules.AddRangeAsync(componentRules, ct);
    }

    public Task<StudentProfile?> GetStudentProfileAsync(Guid studentProfileId, CancellationToken ct = default)
        => _db.StudentProfiles
              .Include(s => s.Department)
              .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

    public async Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default)
        => await _db.Enrollments
                    .Include(e => e.CourseOffering)
                        .ThenInclude(o => o.Course)
                    .Where(e => e.StudentProfileId == studentProfileId
                             && e.Status == EnrollmentStatus.Active
                             && e.CourseOffering.SemesterId == semesterId)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.Enrollments
                    .Include(e => e.CourseOffering)
                        .ThenInclude(o => o.Course)
                    .Where(e => e.StudentProfileId == studentProfileId
                             && e.Status == EnrollmentStatus.Active)
                    .ToListAsync(ct);

    public Task<Guid?> GetSemesterIdForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => _db.CourseOfferings
              .Where(o => o.Id == courseOfferingId)
              .Select(o => (Guid?)o.SemesterId)
              .FirstOrDefaultAsync(ct);

    public Task<int?> GetCreditHoursForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => _db.CourseOfferings
              .Where(o => o.Id == courseOfferingId)
              .Select(o => (int?)o.Course.CreditHours)
              .FirstOrDefaultAsync(ct);

    public void UpdateStudentProfile(StudentProfile studentProfile) => _db.StudentProfiles.Update(studentProfile);

    // ── Transcript export logs ────────────────────────────────────────────────

    /// <summary>Returns all export log entries for the student, newest first.</summary>
    public async Task<IReadOnlyList<TranscriptExportLog>> GetExportLogsAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.TranscriptExportLogs
                    .Where(l => l.StudentProfileId == studentProfileId)
                    .OrderByDescending(l => l.ExportedAt)
                    .ToListAsync(ct);

    /// <summary>Queues a new transcript export log entry for insertion.</summary>
    public async Task AddExportLogAsync(TranscriptExportLog log, CancellationToken ct = default)
        => await _db.TranscriptExportLogs.AddAsync(log, ct);

    // Final-Touches Phase 15 Stage 15.1 — HasPassedCourseAsync: prerequisite pass check
    /// <summary>
    /// Returns true when the student has a published 'Total' result for any offering of the given course
    /// with marks obtained percentage >= configured threshold.
    /// </summary>
    public Task<bool> HasPassedCourseAsync(
        Guid studentProfileId,
        Guid courseId,
        decimal passThresholdPercentage,
        CancellationToken ct = default)
        => _db.Results.AnyAsync(r =>
               r.StudentProfileId == studentProfileId
               && r.IsPublished
               && r.ResultType == "Total"
               && r.MaxMarks > 0
               && r.MarksObtained * 100m >= r.MaxMarks * passThresholdPercentage
               && _db.CourseOfferings.Any(o => o.Id == r.CourseOfferingId && o.CourseId == courseId), ct);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
