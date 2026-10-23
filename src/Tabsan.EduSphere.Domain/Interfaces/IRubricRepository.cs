// Final-Touches Phase 16 Stage 16.2 — repository interface for rubric CRUD

using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Handles persistence of Rubric, RubricCriterion, RubricLevel,
/// and RubricStudentGrade entities.
/// </summary>
public interface IRubricRepository
{
    // Final-Touches Phase 16 Stage 16.2 — query rubric with criteria and levels
    Task<Rubric?> GetByAssignmentAsync(Guid assignmentId, CancellationToken ct = default);
    Task<Rubric?> GetByIdAsync(Guid rubricId, CancellationToken ct = default);

    /// <summary>Plan M3 fix: the rubric (with criteria and levels) that owns a criterion.</summary>
    Task<Rubric?> GetByCriterionAsync(Guid criterionId, CancellationToken ct = default);

    // Final-Touches Phase 16 Stage 16.2 — persist a complete rubric graph
    Task AddAsync(Rubric rubric, CancellationToken ct = default);
    void Update(Rubric rubric);

    // Final-Touches Phase 16 Stage 16.2 — manage student grade entries per rubric
    Task<RubricStudentGrade?> GetStudentGradeAsync(
        Guid submissionId,
        Guid criterionId,
        CancellationToken ct = default);

    Task<IReadOnlyList<RubricStudentGrade>> GetStudentGradesForSubmissionAsync(
        Guid submissionId,
        CancellationToken ct = default);

    Task AddStudentGradeAsync(RubricStudentGrade grade, CancellationToken ct = default);
    void UpdateStudentGrade(RubricStudentGrade grade);

    Task SaveChangesAsync(CancellationToken ct = default);
}
