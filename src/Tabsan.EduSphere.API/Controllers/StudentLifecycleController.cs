using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages student graduation and active/inactive status lifecycle.
/// All endpoints require Admin or SuperAdmin role.
/// Routes: /api/v1/student-lifecycle
/// </summary>
[ApiController]
[Route("api/v1/student-lifecycle")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class StudentLifecycleController : ControllerBase
{
    private readonly IStudentLifecycleService _service;
    private readonly IDepartmentRepository _departments;
    private readonly IStudentLifecycleRepository _studentLifecycle;
    private readonly IAdminAssignmentRepository _adminAssignments;
    private readonly IAccessScopeResolver _accessScope;

    public StudentLifecycleController(
        IStudentLifecycleService service,
        IDepartmentRepository departments,
        IStudentLifecycleRepository studentLifecycle,
        IAdminAssignmentRepository adminAssignments,
        IAccessScopeResolver accessScope)
    {
        _service = service;
        _departments = departments;
        _studentLifecycle = studentLifecycle;
        _adminAssignments = adminAssignments;
        _accessScope = accessScope;
    }

    // ── GET /api/v1/student-lifecycle/graduation-candidates/{departmentId} ────

    /// <summary>Returns all active students in a department eligible for graduation.</summary>
    [HttpGet("graduation-candidates/{departmentId:guid}")]
    public async Task<IActionResult> GetGraduationCandidates(Guid departmentId, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await EnforceDepartmentScopeAsync(departmentId, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        var candidates = await _service.GetGraduationCandidatesByDepartmentAsync(departmentId, ct);
        return Ok(candidates);
    }

    // ── POST /api/v1/student-lifecycle/graduate ───────────────────────────────

    /// <summary>Marks a single student as Graduated. Student dashboard becomes read-only.</summary>
    [HttpPost("graduate")]
    public async Task<IActionResult> GraduateStudent([FromBody] GraduateStudentRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        try
        {
            var scope = await EnforceStudentScopeAsync(request.StudentProfileId, tenantId, campusId, ct);
            if (scope is not null)
                return scope;

            await _service.GraduateStudentAsync(request.StudentProfileId, ct);
            return NoContent();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
    }

    // ── POST /api/v1/student-lifecycle/graduate/batch ─────────────────────────

    /// <summary>Marks multiple students as Graduated in one request.</summary>
    [HttpPost("graduate/batch")]
    public async Task<IActionResult> GraduateStudentsBatch(
        [FromBody] IList<Guid> studentProfileIds,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            foreach (var studentProfileId in studentProfileIds)
            {
                var scope = await EnforceStudentScopeAsync(studentProfileId, tenantId, campusId, ct);
                if (scope is not null)
                    return scope;
            }

            await _service.GraduateStudentsBatchAsync(studentProfileIds, ct);
            return NoContent();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
    }

    // ── POST /api/v1/student-lifecycle/{id}/deactivate ────────────────────────

    /// <summary>Marks a student as Inactive (dropout/leave). Blocks login; preserves all academic data.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        try
        {
            var scope = await EnforceStudentScopeAsync(id, tenantId, campusId, ct);
            if (scope is not null)
                return scope;

            await _service.DeactivateStudentAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
    }

    // ── POST /api/v1/student-lifecycle/{id}/reactivate ────────────────────────

    /// <summary>Re-activates a previously deactivated student account.</summary>
    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        try
        {
            var scope = await EnforceStudentScopeAsync(id, tenantId, campusId, ct);
            if (scope is not null)
                return scope;

            await _service.ReactivateStudentAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
    }

    // ── GET /api/v1/student-lifecycle/academic-level-students/{departmentId}/{level} ──

    /// <summary>Returns all Active students in a department currently in the given academic level number.</summary>
    [HttpGet("academic-level-students/{departmentId:guid}/{levelNumber:int}")]
    public async Task<IActionResult> GetStudentsByAcademicLevel(
        Guid departmentId,
        int levelNumber,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var scope = await EnforceDepartmentScopeAsync(departmentId, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        var students = await _service.GetStudentsByAcademicLevelAsync(departmentId, levelNumber, ct);
        return Ok(students);
    }

    // ── GET /api/v1/student-lifecycle/semester-students/{departmentId}/{semester} ──
    // Backward-compatible alias for legacy portal/API clients.
    [HttpGet("semester-students/{departmentId:guid}/{semesterNumber:int}")]
    public Task<IActionResult> GetStudentsBySemester(Guid departmentId, int semesterNumber, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
        => GetStudentsByAcademicLevel(departmentId, semesterNumber, tenantId, campusId, ct);

    // ── POST /api/v1/student-lifecycle/{id}/promote ───────────────────────────

    /// <summary>Advances a single Active student to the next semester (increments CurrentSemesterNumber).</summary>
    [HttpPost("{id:guid}/promote")]
    public async Task<IActionResult> PromoteStudent(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        try
        {
            var scope = await EnforceStudentScopeAsync(id, tenantId, campusId, ct);
            if (scope is not null)
                return scope;

            await _service.PromoteStudentAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // ── POST /api/v1/student-lifecycle/promote/batch ──────────────────────────

    /// <summary>
    /// Advances multiple Active students to the next semester in one request.
    /// Returns a result with promoted count and per-student errors.
    /// </summary>
    [HttpPost("promote/batch")]
    public async Task<IActionResult> PromoteStudentsBatch(
        [FromBody] PromoteStudentsBatchRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        foreach (var studentProfileId in request.StudentProfileIds)
        {
            var scope = await EnforceStudentScopeAsync(studentProfileId, tenantId, campusId, ct);
            if (scope is not null)
                return scope;
        }

        var result = await _service.PromoteStudentsBatchAsync(request.StudentProfileIds, ct);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private int? GetCurrentInstitutionType()
    {
        var raw = User.FindFirst("institutionType")?.Value;
        return int.TryParse(raw, out var value) ? value : null;
    }

    private async Task<IActionResult?> EnforceDepartmentScopeAsync(Guid departmentId, Guid? requestedTenantId, Guid? requestedCampusId, CancellationToken ct)
    {
        if (requestedTenantId.HasValue != requestedCampusId.HasValue)
            return BadRequest(new { message = "TenantId and CampusId must be provided together." });

        var department = await _departments.GetByIdAsync(departmentId, ct);
        if (department is null)
            return NotFound("Department not found.");

        if (_accessScope.IsSuperAdmin())
        {
            if (requestedTenantId.HasValue && department.TenantId != requestedTenantId.Value)
                return Forbid();
            if (requestedCampusId.HasValue && department.CampusId != requestedCampusId.Value)
                return Forbid();
            return null;
        }

        var callerTenantId = _accessScope.GetTenantId();
        var callerCampusId = _accessScope.GetCampusId();

        if (callerTenantId.HasValue && department.TenantId != callerTenantId.Value)
            return Forbid();
        if (callerCampusId.HasValue && department.CampusId != callerCampusId.Value)
            return Forbid();

        if (requestedTenantId.HasValue && department.TenantId != requestedTenantId.Value)
            return Forbid();
        if (requestedCampusId.HasValue && department.CampusId != requestedCampusId.Value)
            return Forbid();

        if (User.IsInRole("Admin"))
        {
            var adminUserId = GetCurrentUserId();
            if (adminUserId == Guid.Empty)
                return Forbid();

            var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(adminUserId, ct);
            if (!allowedDepartmentIds.Contains(departmentId))
                return Forbid();
        }

        var callerInstitutionType = GetCurrentInstitutionType();
        if (!callerInstitutionType.HasValue)
            return null;

        return (int)department.InstitutionType == callerInstitutionType.Value ? null : Forbid();
    }

    private async Task<IActionResult?> EnforceStudentScopeAsync(Guid studentProfileId, Guid? requestedTenantId, Guid? requestedCampusId, CancellationToken ct)
    {
        var student = await _studentLifecycle.GetByIdAsync(studentProfileId, ct);
        if (student is null)
            return NotFound(new { message = $"Student profile {studentProfileId} not found." });

        return await EnforceDepartmentScopeAsync(student.DepartmentId, requestedTenantId, requestedCampusId, ct);
    }
}
