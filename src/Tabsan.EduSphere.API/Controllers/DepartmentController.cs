using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages academic departments.
/// Admin and SuperAdmin can create and modify departments.
/// All authenticated users can read department data.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentRepository _deptRepo;
    private readonly IFacultyAssignmentRepository _facultyAssignments;
    private readonly IAdminAssignmentRepository _adminAssignments;
    private readonly IUserRepository _users;
    private readonly IAuditService _audit;
    private readonly IInstitutionPolicyService _institutionPolicyService;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public DepartmentController(
        IDepartmentRepository deptRepo,
        IFacultyAssignmentRepository facultyAssignments,
        IAdminAssignmentRepository adminAssignments,
        IUserRepository users,
        IAuditService audit,
        IInstitutionPolicyService institutionPolicyService,
        IAccessScopeResolver accessScope,
        ApplicationDbContext db)
    {
        _deptRepo = deptRepo;
        _facultyAssignments = facultyAssignments;
        _adminAssignments = adminAssignments;
        _users = users;
        _audit = audit;
        _institutionPolicyService = institutionPolicyService;
        _accessScope = accessScope;
        _db = db;
    }

    // ── GET /api/v1/department ─────────────────────────────────────────────────

    /// <summary>Returns all active departments ordered by name.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] InstitutionType? institutionType,
        CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var depts = (await _deptRepo.GetAllAsync(scope.TenantId, scope.CampusId, ct)).AsEnumerable();

        if (User.IsInRole("Faculty") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(GetUserId(), ct);
            if (allowedDepartmentIds.Count > 0)
                depts = depts.Where(d => allowedDepartmentIds.Contains(d.Id));
        }

        if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(GetUserId(), ct);
            depts = depts.Where(d => allowedDepartmentIds.Contains(d.Id));
        }

        if (institutionType.HasValue)
            depts = depts.Where(d => d.InstitutionType == institutionType.Value);

        return Ok(depts.Select(d => new { d.Id, d.Name, d.Code, d.IsActive, d.InstitutionType, d.TenantId, d.CampusId }));
    }

    // ── GET /api/v1/department/{id} ────────────────────────────────────────────

    /// <summary>Returns a single department by its GUID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var scopeResult = await EnsureDepartmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (scopeResult is not null)
            return scopeResult;

        var dept = await _deptRepo.GetByIdAsync(id, ct);
        return dept is null ? NotFound() : Ok(new { dept.Id, dept.Name, dept.Code, dept.IsActive, dept.InstitutionType, dept.TenantId, dept.CampusId });
    }

    // ── POST /api/v1/department ────────────────────────────────────────────────

    /// <summary>Creates a new department. Admin and SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        if (!scope.TenantId.HasValue || !scope.CampusId.HasValue)
            return BadRequest(new { message = "Department creation requires both tenantId and campusId." });

        if (await _deptRepo.CodeExistsAsync(request.Code, scope.TenantId, scope.CampusId, ct))
            return Conflict($"Department code '{request.Code}' is already in use.");

        var policy = await _institutionPolicyService.GetPolicyAsync(ct);
        if (!policy.IsEnabled(request.InstitutionType))
            return BadRequest($"Institution type '{request.InstitutionType}' is not enabled by the current license policy.");

        var dept = new Department(request.Name, request.Code, request.InstitutionType);
        dept.SetTenantCampus(scope.TenantId, scope.CampusId);
        await _deptRepo.AddAsync(dept, ct);
        await _deptRepo.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = dept.Id }, new { dept.Id });
    }

    // ── PUT /api/v1/department/{id} ────────────────────────────────────────────

    /// <summary>Updates the department name. Admin and SuperAdmin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var scopeResult = await EnsureDepartmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (scopeResult is not null)
            return scopeResult;

        var dept = await _deptRepo.GetByIdAsync(id, ct);
        if (dept is null) return NotFound();

        if (request.InstitutionType is not null)
        {
            var policy = await _institutionPolicyService.GetPolicyAsync(ct);
            if (!policy.IsEnabled(request.InstitutionType.Value))
                return BadRequest($"Institution type '{request.InstitutionType}' is not enabled by the current license policy.");

            dept.SetInstitutionType(request.InstitutionType.Value);
        }

        dept.Rename(request.NewName);
        _deptRepo.Update(dept);
        await _deptRepo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── DELETE /api/v1/department/{id} (soft-delete) ──────────────────────────

    /// <summary>Soft-deletes (deactivates) a department. Admin and SuperAdmin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Deactivate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var scopeResult = await EnsureDepartmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (scopeResult is not null)
            return scopeResult;

        var dept = await _deptRepo.GetByIdAsync(id, ct);
        if (dept is null) return NotFound();

        dept.Deactivate();
        _deptRepo.Update(dept);
        await _deptRepo.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Activate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var scopeResult = await EnsureDepartmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (scopeResult is not null)
            return scopeResult;

        var dept = await _deptRepo.GetByIdAsync(id, ct);
        if (dept is null) return NotFound();

        dept.Activate();
        _deptRepo.Update(dept);
        await _deptRepo.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public Task<IActionResult> DeactivateAlias(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
        => Deactivate(id, tenantId, campusId, ct);

    // ── POST /api/v1/department/admin-assignment ────────────────────────────

    /// <summary>Assigns an Admin user to a department. SuperAdmin only.</summary>
    [HttpPost("admin-assignment")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AssignAdminToDepartment([FromBody] AssignAdminToDepartmentRequest request, CancellationToken ct)
    {
        if (request.AdminUserId == Guid.Empty || request.DepartmentId == Guid.Empty)
            return BadRequest("AdminUserId and DepartmentId are required.");

        var adminUser = await _users.GetByIdAsync(request.AdminUserId, ct);
        if (adminUser is null)
            return NotFound("Admin user not found.");

        if (!string.Equals(adminUser.Role?.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only users with Admin role can be assigned.");

        var dept = await _deptRepo.GetByIdAsync(request.DepartmentId, ct);
        if (dept is null)
            return NotFound("Department not found.");

        if (adminUser.InstitutionType is not null && adminUser.InstitutionType.Value != dept.InstitutionType)
            return BadRequest("Admin user's institution type does not match the target department institution type.");

        var existing = await _adminAssignments.GetAsync(request.AdminUserId, request.DepartmentId, ct);
        if (existing is not null)
            return NoContent();

        await _adminAssignments.AddAsync(new AdminDepartmentAssignment(request.AdminUserId, request.DepartmentId), ct);
        await _adminAssignments.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── DELETE /api/v1/department/admin-assignment ──────────────────────────

    /// <summary>Revokes an Admin user's department assignment. SuperAdmin only.</summary>
    [HttpDelete("admin-assignment")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RemoveAdminFromDepartment([FromBody] RemoveAdminFromDepartmentRequest request, CancellationToken ct)
    {
        if (request.AdminUserId == Guid.Empty || request.DepartmentId == Guid.Empty)
            return BadRequest("AdminUserId and DepartmentId are required.");

        var existing = await _adminAssignments.GetAsync(request.AdminUserId, request.DepartmentId, ct);
        if (existing is null)
            return NotFound("Assignment not found.");

        existing.Remove();
        _adminAssignments.Update(existing);
        await _adminAssignments.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── GET /api/v1/department/admin-assignment/{adminUserId} ───────────────

    /// <summary>Lists active department assignments for an Admin user. SuperAdmin only.</summary>
    [HttpGet("admin-assignment/{adminUserId:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAdminDepartmentAssignments(Guid adminUserId, CancellationToken ct)
    {
        if (adminUserId == Guid.Empty)
            return BadRequest("adminUserId is required.");

        var assignments = await _adminAssignments.GetByAdminAsync(adminUserId, ct);
        return Ok(assignments.Select(a => new { a.AdminUserId, a.DepartmentId, DepartmentName = a.Department.Name, a.AssignedAt }));
    }

    // ── GET /api/v1/department/admin-users ──────────────────────────────────

    /// <summary>Returns active users in Admin role for department assignment management. SuperAdmin only.</summary>
    [HttpGet("admin-users")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAdminUsers(CancellationToken ct)
    {
        var users = await _users.GetActiveUsersByRolesAsync(new[] { "Admin" }, ct);
        return Ok(users.Select(u => new { u.Id, u.Username, u.Email, u.InstitutionType }));
    }

    // ── POST /api/v1/department/faculty-assignment ───────────────────────────

    /// <summary>Assigns a Faculty user to a department. SuperAdmin only.</summary>
    [HttpPost("faculty-assignment")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AssignFacultyToDepartment([FromBody] AssignFacultyToDepartmentRequest request, CancellationToken ct)
    {
        if (request.FacultyUserId == Guid.Empty || request.DepartmentId == Guid.Empty)
            return BadRequest("FacultyUserId and DepartmentId are required.");

        var facultyUser = await _users.GetByIdAsync(request.FacultyUserId, ct);
        if (facultyUser is null)
            return NotFound("Faculty user not found.");

        if (!string.Equals(facultyUser.Role?.Name, "Faculty", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only users with Faculty role can be assigned.");

        var dept = await _deptRepo.GetByIdAsync(request.DepartmentId, ct);
        if (dept is null)
            return NotFound("Department not found.");

        if (facultyUser.InstitutionType is not null && facultyUser.InstitutionType.Value != dept.InstitutionType)
            return BadRequest("Faculty user's institution type does not match the target department institution type.");

        var existing = await _facultyAssignments.GetAsync(request.FacultyUserId, request.DepartmentId, ct);
        if (existing is not null)
            return NoContent();

        await _facultyAssignments.AddAsync(new FacultyDepartmentAssignment(request.FacultyUserId, request.DepartmentId), ct);
        await _facultyAssignments.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── DELETE /api/v1/department/faculty-assignment ─────────────────────────

    /// <summary>Revokes a Faculty user's department assignment. SuperAdmin only.</summary>
    [HttpDelete("faculty-assignment")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RemoveFacultyFromDepartment([FromBody] RemoveFacultyFromDepartmentRequest request, CancellationToken ct)
    {
        if (request.FacultyUserId == Guid.Empty || request.DepartmentId == Guid.Empty)
            return BadRequest("FacultyUserId and DepartmentId are required.");

        var existing = await _facultyAssignments.GetAsync(request.FacultyUserId, request.DepartmentId, ct);
        if (existing is null)
            return NotFound("Assignment not found.");

        existing.Remove();
        _facultyAssignments.Update(existing);
        await _facultyAssignments.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── GET /api/v1/department/faculty-assignment/{facultyUserId} ───────────

    /// <summary>Lists active department assignments for a Faculty user. SuperAdmin only.</summary>
    [HttpGet("faculty-assignment/{facultyUserId:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetFacultyDepartmentAssignments(Guid facultyUserId, CancellationToken ct)
    {
        if (facultyUserId == Guid.Empty)
            return BadRequest("facultyUserId is required.");

        var assignments = await _facultyAssignments.GetByFacultyAsync(facultyUserId, ct);
        return Ok(assignments.Select(a => new { a.FacultyUserId, a.DepartmentId, DepartmentName = a.Department.Name, a.AssignedAt }));
    }

    // ── GET /api/v1/department/faculty-users ─────────────────────────────────

    /// <summary>Returns active users in Faculty role for department assignment management. SuperAdmin only.</summary>
    [HttpGet("faculty-users")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetFacultyUsers(CancellationToken ct)
    {
        var users = await _users.GetActiveUsersByRolesAsync(new[] { "Faculty" }, ct);
        return Ok(users.Select(u => new { u.Id, u.Username, u.Email, u.InstitutionType }));
    }

    // ── Helper ─────────────────────────────────────────────────────────────────
    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
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

    private async Task<IActionResult?> EnsureDepartmentIsInScopeAsync(Guid departmentId, Guid? effectiveTenantId, Guid? effectiveCampusId, CancellationToken ct)
    {
        if (!effectiveTenantId.HasValue && !effectiveCampusId.HasValue)
            return null;

        var deptScope = await _db.Departments
            .AsNoTracking()
            .Where(d => d.Id == departmentId)
            .Select(d => new { d.TenantId, d.CampusId })
            .FirstOrDefaultAsync(ct);

        if (deptScope is null)
            return null;

        if (effectiveTenantId.HasValue && deptScope.TenantId != effectiveTenantId.Value)
            return Forbid();

        if (effectiveCampusId.HasValue && deptScope.CampusId != effectiveCampusId.Value)
            return Forbid();

        return null;
    }
}

// ── Inline request records (simple enough to keep co-located) ─────────────────

/// <summary>Request body for creating a department.</summary>
public sealed record CreateDepartmentRequest(string Name, string Code, InstitutionType InstitutionType = InstitutionType.University);

/// <summary>Request body for updating a department's display name.</summary>
public sealed record UpdateDepartmentRequest(string NewName, InstitutionType? InstitutionType = null);
