using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Repository interface for Result and TranscriptExportLog operations.</summary>
public interface IResultRepository
{
    // ── Results ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the result for the given student, offering, and type combination, or null.
    /// </summary>
    Task<Result?> GetAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default);

    /// <summary>Returns all results for the given student (published and draft).</summary>
    Task<IReadOnlyList<Result>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns published results only for the given student (student-visible view).</summary>
    Task<IReadOnlyList<Result>> GetPublishedByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns all results for a course offering (faculty/admin view, published and draft).</summary>
    Task<IReadOnlyList<Result>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns all results for a specific student and course offering.</summary>
    Task<IReadOnlyList<Result>> GetByStudentAndOfferingAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns all results for a student in a specific semester.</summary>
    Task<IReadOnlyList<Result>> GetByStudentAndSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default);

    /// <summary>Returns true when a result row already exists for the given combination.</summary>
    Task<bool> ExistsAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default);

    /// <summary>Queues a new result for insertion.</summary>
    Task AddAsync(Result result, CancellationToken ct = default);

    /// <summary>Queues multiple results for bulk insertion (batch entry for an entire class).</summary>
    Task AddRangeAsync(IEnumerable<Result> results, CancellationToken ct = default);

    /// <summary>Marks a result as modified (publish or correction).</summary>
    void Update(Result result);

    /// <summary>Returns the currently active assessment component rules.</summary>
    Task<IReadOnlyList<ResultComponentRule>> GetActiveComponentRulesAsync(CancellationToken ct = default);

    /// <summary>Returns all result calculation component rules.</summary>
    Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(InstitutionType institutionType, CancellationToken ct = default);

    /// <summary>Returns the configured GPA scale rules.</summary>
    Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(InstitutionType institutionType, CancellationToken ct = default);

    /// <summary>Replaces the result calculation configuration in one unit of work.</summary>
    Task ReplaceCalculationRulesAsync(IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default);
    Task ReplaceCalculationRulesAsync(InstitutionType institutionType, IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default);

    /// <summary>Returns the student profile for GPA updates, or null when not found.</summary>
    Task<StudentProfile?> GetStudentProfileAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns active enrollments for the student in the given semester.</summary>
    Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default);

    /// <summary>Returns active enrollments for the student across all semesters.</summary>
    Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the semester ID for a course offering.</summary>
    Task<Guid?> GetSemesterIdForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns the credit hours for a course offering.</summary>
    Task<int?> GetCreditHoursForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Marks the student profile as modified for standing updates.</summary>
    void UpdateStudentProfile(StudentProfile studentProfile);

    // ── Transcript export logs ────────────────────────────────────────────────

    /// <summary>Returns all export log entries for the given student, ordered by export date descending.</summary>
    Task<IReadOnlyList<TranscriptExportLog>> GetExportLogsAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Queues a new transcript export log entry for insertion.</summary>
    Task AddExportLogAsync(TranscriptExportLog log, CancellationToken ct = default);

    // Final-Touches Phase 15 Stage 15.1 — HasPassedCourseAsync: prerequisite check
    /// <summary>
    /// Returns true when the student has a published 'Total' result for any offering of the given course
    /// with a passing mark (marks obtained percentage >= passThresholdPercentage).
    /// </summary>
    Task<bool> HasPassedCourseAsync(
        Guid studentProfileId,
        Guid courseId,
        decimal passThresholdPercentage,
        CancellationToken ct = default);

    /// <summary>Commits pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
