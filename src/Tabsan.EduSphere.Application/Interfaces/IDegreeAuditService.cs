// Final-Touches Phase 17 Stage 17.1/17.2/17.3 — Degree audit service interface

using Tabsan.EduSphere.Application.DTOs.Academic;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Application service interface for the Degree Audit System.
/// </summary>
public interface IDegreeAuditService
{
    // Final-Touches Phase 17 Stage 17.1 — credit completion audit
    /// <summary>Returns the full credit audit and eligibility check for a student.</summary>
    Task<DegreeAuditResponse> GetAuditAsync(
        Guid studentProfileId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null);

    // Final-Touches Phase 17 Stage 17.2 — eligibility list for admin
    /// <summary>Returns an eligibility summary list for all students in a department/program.</summary>
    Task<IReadOnlyList<EligibilityListItem>> GetEligibilityListAsync(
        Guid? departmentId,
        Guid? programId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null);

    // Final-Touches Phase 17 Stage 17.2 — degree rule CRUD
    /// <summary>Returns all degree rules.</summary>
    Task<IReadOnlyList<DegreeRuleResponse>> GetAllRulesAsync(
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null);

    /// <summary>Returns the degree rule for a specific program, or null.</summary>
    Task<DegreeRuleResponse?> GetRuleByProgramAsync(
        Guid programId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null);

    /// <summary>Creates a new degree rule for a program.</summary>
    Task<DegreeRuleResponse> CreateRuleAsync(CreateDegreeRuleRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing degree rule.</summary>
    Task<DegreeRuleResponse> UpdateRuleAsync(Guid ruleId, UpdateDegreeRuleRequest request, CancellationToken ct = default);

    /// <summary>Soft-deletes a degree rule.</summary>
    Task DeleteRuleAsync(Guid ruleId, CancellationToken ct = default);

    // Final-Touches Phase 17 Stage 17.3 — course type tagging
    /// <summary>Sets the CourseType (Core/Elective) on a course.</summary>
    Task SetCourseTypeAsync(Guid courseId, string courseType, CancellationToken ct = default);
}
