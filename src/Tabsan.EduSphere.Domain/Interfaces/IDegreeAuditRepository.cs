// Final-Touches Phase 17 Stage 17.1/17.2 — Degree audit repository interface

using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository interface for DegreeRule persistence and degree-audit credit queries.
/// </summary>
public interface IDegreeAuditRepository
{
    // ── DegreeRule CRUD ───────────────────────────────────────────────────────

    /// <summary>Returns the active (non-deleted) degree rule for the given academic program, or null.</summary>
    Task<DegreeRule?> GetRuleByProgramAsync(
        Guid academicProgramId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null);

    /// <summary>Returns all degree rules (SuperAdmin view).</summary>
    Task<IReadOnlyList<DegreeRule>> GetAllRulesAsync(
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null);

    /// <summary>Returns the degree rule by its own ID, or null.</summary>
    Task<DegreeRule?> GetRuleByIdAsync(Guid ruleId, CancellationToken ct = default);

    /// <summary>Queues a new rule for insertion.</summary>
    Task AddRuleAsync(DegreeRule rule, CancellationToken ct = default);

    /// <summary>Marks an existing rule as modified.</summary>
    void UpdateRule(DegreeRule rule);

    // ── Degree Audit Queries ─────────────────────────────────────────────────

    /// <summary>
    /// Returns a flat list of credit rows for a student.
    /// Each row represents one Result that has a GradePoint of at least 1.0 (D grade pass),
    /// joined with CourseOffering → Course for CreditHours and CourseType.
    /// </summary>
    Task<IReadOnlyList<CreditRow>> GetEarnedCreditsAsync(
        Guid studentProfileId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null);

    /// <summary>Returns the academic program ID linked to the student's profile, or null.</summary>
    Task<Guid?> GetStudentProgramIdAsync(Guid studentProfileId, CancellationToken ct = default);

    // Final-Touches Phase 17 Stage 17.1 — fetch username for audit response display
    /// <summary>Returns the Username of the user with the given ID, or an empty string.</summary>
    Task<string> GetUsernameAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Commits all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Final-Touches Phase 17 Stage 17.1 — lightweight credit row DTO returned by the join query
/// <summary>Represents one earned credit row for a student (one distinct passing course).</summary>
public sealed record CreditRow(
    Guid       CourseOfferingId,
    Guid       CourseId,
    string     CourseCode,
    string     CourseTitle,
    int        CreditHours,
    CourseType CourseType,
    string     ResultType,
    decimal?   GradePoint
);
