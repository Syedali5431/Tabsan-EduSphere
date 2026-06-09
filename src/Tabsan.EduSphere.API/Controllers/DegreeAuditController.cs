// Final-Touches Phase 17 Stage 17.1/17.2/17.3 — Degree Audit API controller

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Degree Audit endpoints: credit-completion audits, graduation eligibility checks,
/// degree rule CRUD, and course-type tagging.
/// </summary>
[ApiController]
[Route("api/v1/degree-audit")]
[Authorize]
public class DegreeAuditController : ControllerBase
{
    private readonly IDegreeAuditService      _degreeAudit;
    private readonly IStudentProfileRepository _studentRepo;
    private readonly IAccessScopeResolver _accessScope;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IAcademicProgramRepository _programRepo;
    private readonly IInstitutionPolicyService _institutionPolicy;

    public DegreeAuditController(
        IDegreeAuditService       degreeAudit,
        IStudentProfileRepository studentRepo,
        IAccessScopeResolver accessScope,
        IDepartmentRepository departmentRepo,
        IAcademicProgramRepository programRepo,
        IInstitutionPolicyService institutionPolicy)
    {
        _degreeAudit = degreeAudit;
        _studentRepo = studentRepo;
        _accessScope = accessScope;
        _departmentRepo = departmentRepo;
        _programRepo = programRepo;
        _institutionPolicy = institutionPolicy;
    }

    // ── GET /api/v1/degree-audit/me ───────────────────────────────────────────

    // Final-Touches Phase 17 Stage 17.1 — student retrieves own audit
    /// <summary>Returns the degree audit for the currently authenticated student.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyAudit(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        // Per-student department check below enforces university-only;
        // caller-level gating is unnecessary here.
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var userId  = GetUserId();
        var profile = await _studentRepo.GetByUserIdAsync(userId, ct);
        if (profile is null) return NotFound("Student profile not found.");

        if (profile.Department is { InstitutionType: not InstitutionType.University })
            return BadRequest("Degree audit is available only for university institution type.");

        if (scope.TenantId.HasValue && profile.Department?.TenantId != scope.TenantId.Value)
            return Forbid();
        if (scope.CampusId.HasValue && profile.Department?.CampusId != scope.CampusId.Value)
            return Forbid();

        var audit = await _degreeAudit.GetAuditAsync(profile.Id, ct, scope.TenantId, scope.CampusId);
        return Ok(audit);
    }

    // ── GET /api/v1/degree-audit/{studentProfileId} ───────────────────────────

    // Final-Touches Phase 17 Stage 17.1 — admin/faculty retrieves audit for any student
    /// <summary>Returns the degree audit for the specified student profile (Admin/Faculty/SuperAdmin).</summary>
    [HttpGet("{studentProfileId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,Faculty")]
    public async Task<IActionResult> GetStudentAudit(
        Guid studentProfileId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        // Caller institution-type gating is enforced per-student below,
        // so admins/faculty on non-university setups can still look up
        // audits for students who belong to university departments.
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var student = await _studentRepo.GetByIdAsync(studentProfileId, ct);
        if (student is null)
            return NotFound($"Student profile {studentProfileId} not found.");

        if (student.Department is { InstitutionType: not InstitutionType.University })
            return BadRequest("Degree audit is available only for university institution type.");

        try
        {
            var audit = await _degreeAudit.GetAuditAsync(studentProfileId, ct, scope.TenantId, scope.CampusId);
            return Ok(audit);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── GET /api/v1/degree-audit/eligible ────────────────────────────────────

    // Final-Touches Phase 17 Stage 17.2 — eligibility list for admin
    /// <summary>Returns a graduation eligibility summary for all students (Admin/SuperAdmin).</summary>
    [HttpGet("eligible")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetEligibilityList(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? programId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        var callerResult = EnsureCallerUniversityOrSuperAdmin();
        if (callerResult is not null)
            return callerResult;

        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        if (departmentId.HasValue)
        {
            var dept = await _departmentRepo.GetByIdAsync(departmentId.Value, ct);
            if (dept is null)
                return NotFound("Department not found.");
            if (dept.InstitutionType != InstitutionType.University)
                return BadRequest("Degree audit is available only for university institution type.");
        }

        if (programId.HasValue)
        {
            var prog = await _programRepo.GetByIdAsync(programId.Value, scope.TenantId, scope.CampusId, ct);
            if (prog is null)
                return NotFound("Program not found.");
            if (prog.Department?.InstitutionType != InstitutionType.University)
                return BadRequest("Degree audit is available only for university institution type.");
        }

        var list = await _degreeAudit.GetEligibilityListAsync(
            departmentId,
            programId,
            ct,
            scope.TenantId,
            scope.CampusId);
        return Ok(list);
    }

    // ── GET /api/v1/degree-audit/rule ────────────────────────────────────────

    // Final-Touches Phase 17 Stage 17.2 — list all rules for SuperAdmin
    /// <summary>Returns all configured degree rules (SuperAdmin).</summary>
    [HttpGet("rule")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllRules(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var rules = await _degreeAudit.GetAllRulesAsync(ct, scope.TenantId, scope.CampusId);
        return Ok(rules);
    }

    // ── GET /api/v1/degree-audit/rule/{programId} ─────────────────────────────

    // Final-Touches Phase 17 Stage 17.2 — any authenticated user can view their program's rule
    /// <summary>Returns the degree rule for the specified academic program.</summary>
    [HttpGet("rule/{programId:guid}")]
    public async Task<IActionResult> GetRuleByProgram(
        Guid programId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        var callerResult = EnsureCallerUniversityOrSuperAdmin();
        if (callerResult is not null)
            return callerResult;

        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var program = await _programRepo.GetByIdAsync(programId, scope.TenantId, scope.CampusId, ct);
        if (program is null)
            return NotFound("Program not found.");
        if (program.Department?.InstitutionType != InstitutionType.University)
            return BadRequest("Degree audit is available only for university institution type.");

        var rule = await _degreeAudit.GetRuleByProgramAsync(programId, ct, scope.TenantId, scope.CampusId);
        return rule is null ? NotFound("No degree rule is configured for this program.") : Ok(rule);
    }

    // ── POST /api/v1/degree-audit/rule ───────────────────────────────────────

    // Final-Touches Phase 17 Stage 17.2 — SuperAdmin creates a new degree rule
    /// <summary>Creates a new degree rule for an academic program (SuperAdmin).</summary>
    [HttpPost("rule")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateRule([FromBody] CreateDegreeRuleRequest request, CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        var program = await _programRepo.GetByIdAsync(request.AcademicProgramId, null, null, ct);
        if (program is null)
            return NotFound("Program not found.");
        if (program.Department?.InstitutionType != InstitutionType.University)
            return BadRequest("Degree rules are available only for university institution type.");

        var rule = await _degreeAudit.CreateRuleAsync(request, ct);
        return CreatedAtAction(nameof(GetRuleByProgram), new { programId = rule.AcademicProgramId }, rule);
    }

    // ── PUT /api/v1/degree-audit/rule/{ruleId} ────────────────────────────────

    // Final-Touches Phase 17 Stage 17.2 — SuperAdmin updates an existing degree rule
    /// <summary>Updates an existing degree rule (SuperAdmin).</summary>
    [HttpPut("rule/{ruleId:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateRule(Guid ruleId, [FromBody] UpdateDegreeRuleRequest request, CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        try
        {
            var rule = await _degreeAudit.UpdateRuleAsync(ruleId, request, ct);
            return Ok(rule);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── DELETE /api/v1/degree-audit/rule/{ruleId} ─────────────────────────────

    // Final-Touches Phase 17 Stage 17.2 — SuperAdmin deletes a degree rule
    /// <summary>Deletes (soft) a degree rule (SuperAdmin).</summary>
    [HttpDelete("rule/{ruleId:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteRule(Guid ruleId, CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        try
        {
            await _degreeAudit.DeleteRuleAsync(ruleId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── PUT /api/v1/degree-audit/course/{courseId}/type ──────────────────────

    // Final-Touches Phase 17 Stage 17.3 — Admin/SuperAdmin tags a course as Core or Elective
    /// <summary>Sets the CourseType (Core or Elective) on a course (Admin/SuperAdmin).</summary>
    [HttpPut("course/{courseId:guid}/type")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SetCourseType(Guid courseId, [FromBody] SetCourseTypeRequest request, CancellationToken ct)
    {
        var policyResult = await EnsureUniversityEnabledAsync(ct);
        if (policyResult is not null)
            return policyResult;

        var callerResult = EnsureCallerUniversityOrSuperAdmin();
        if (callerResult is not null)
            return callerResult;

        try
        {
            await _degreeAudit.SetCourseTypeAsync(courseId, request.CourseType, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private IActionResult? EnsureCallerUniversityOrSuperAdmin()
    {
        if (User.IsInRole("SuperAdmin"))
            return null;

        var raw = User.FindFirst("institutionType")?.Value;
        if (!int.TryParse(raw, out var institutionType))
            return null;

        return institutionType == (int)InstitutionType.University ? null : Forbid();
    }

    private async Task<IActionResult?> EnsureUniversityEnabledAsync(CancellationToken ct)
    {
        var policy = await _institutionPolicy.GetPolicyAsync(ct);
        if (policy.IncludeUniversity)
            return null;

        return BadRequest("University institution type is disabled by the current license policy.");
    }

    private (Guid? TenantId, Guid? CampusId, IActionResult? Error) ResolveEffectiveScope(Guid? requestedTenantId, Guid? requestedCampusId)
    {
        Guid? effectiveTenantId = requestedTenantId;
        Guid? effectiveCampusId = requestedCampusId;

        if (!_accessScope.IsSuperAdmin())
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue)
            {
                if (requestedTenantId.HasValue && requestedTenantId.Value != callerTenantId.Value)
                    return (null, null, Forbid());

                effectiveTenantId = callerTenantId.Value;
            }

            if (callerCampusId.HasValue)
            {
                if (requestedCampusId.HasValue && requestedCampusId.Value != callerCampusId.Value)
                    return (null, null, Forbid());

                effectiveCampusId = callerCampusId.Value;
            }
        }

        if (effectiveTenantId.HasValue != effectiveCampusId.HasValue)
            return (null, null, BadRequest(new { message = "TenantId and CampusId must be provided together." }));

        return (effectiveTenantId, effectiveCampusId, null);
    }
}
