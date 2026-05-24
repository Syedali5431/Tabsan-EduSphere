using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages degree programmes.
/// Admin+ can perform full CRUD; all authenticated users can read.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProgramController : ControllerBase
{
    private readonly IAcademicProgramRepository _repo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IAccessScopeResolver _accessScope;

    public ProgramController(IAcademicProgramRepository repo, IDepartmentRepository deptRepo, IAccessScopeResolver accessScope)
    {
        _repo = repo;
        _deptRepo = deptRepo;
        _accessScope = accessScope;
    }

    // ── GET /api/v1/program ────────────────────────────────────────────────────

    /// <summary>Returns all programmes, optionally filtered by departmentId query parameter.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        if (departmentId.HasValue)
        {
            var scopeResult = await EnsureDepartmentIsInScopeAsync(departmentId.Value, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;
        }

        var programs = await _repo.GetAllAsync(departmentId, scope.TenantId, scope.CampusId, ct);
        return Ok(programs.Select(p => new
        {
            p.Id,
            p.Name,
            p.Code,
            p.DepartmentId,
            TenantId = p.Department.TenantId,
            CampusId = p.Department.CampusId,
            p.TotalSemesters,
            p.IsActive
        }));
    }

    // ── GET /api/v1/program/{id} ───────────────────────────────────────────────

    /// <summary>Returns a single programme by its GUID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var prog = await _repo.GetByIdAsync(id, scope.TenantId, scope.CampusId, ct);
        return prog is null ? NotFound()
            : Ok(new
            {
                prog.Id,
                prog.Name,
                prog.Code,
                prog.DepartmentId,
                TenantId = prog.Department.TenantId,
                CampusId = prog.Department.CampusId,
                prog.TotalSemesters,
                prog.IsActive
            });
    }

    // ── POST /api/v1/program ───────────────────────────────────────────────────

    /// <summary>Creates a new degree programme. Admin and SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProgramRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var dept = await _deptRepo.GetByIdAsync(request.DepartmentId, ct);
        if (dept is null)
            return BadRequest("Department not found.");

        if (scope.TenantId.HasValue && dept.TenantId != scope.TenantId.Value)
            return Forbid();
        if (scope.CampusId.HasValue && dept.CampusId != scope.CampusId.Value)
            return Forbid();

        if (await _repo.CodeExistsAsync(request.Code, request.DepartmentId, scope.TenantId, scope.CampusId, ct))
            return Conflict($"Programme code '{request.Code}' is already in use.");

        var prog = new AcademicProgram(request.Name, request.Code, request.DepartmentId, request.TotalSemesters);
        await _repo.AddAsync(prog, ct);
        await _repo.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = prog.Id }, new { prog.Id });
    }

    // ── PUT /api/v1/program/{id} ───────────────────────────────────────────────

    /// <summary>Renames an existing programme. Admin and SuperAdmin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProgramRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var prog = await _repo.GetByIdAsync(id, scope.TenantId, scope.CampusId, ct);
        if (prog is null) return NotFound();

        prog.Rename(request.Name);
        _repo.Update(prog);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── DELETE /api/v1/program/{id} ────────────────────────────────────────────

    /// <summary>Soft-deactivates a programme. SuperAdmin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public Task<IActionResult> Deactivate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
        => DeactivateInternal(id, tenantId, campusId, ct);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "SuperAdmin")]
    public Task<IActionResult> DeactivateAlias(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
        => DeactivateInternal(id, tenantId, campusId, ct);

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Activate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var prog = await _repo.GetByIdAsync(id, scope.TenantId, scope.CampusId, ct);
        if (prog is null) return NotFound();

        prog.Activate();
        _repo.Update(prog);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<IActionResult> DeactivateInternal(Guid id, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var prog = await _repo.GetByIdAsync(id, scope.TenantId, scope.CampusId, ct);
        if (prog is null) return NotFound();

        prog.Deactivate();
        _repo.Update(prog);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
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

    private async Task<IActionResult?> EnsureDepartmentIsInScopeAsync(Guid departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        var department = await _deptRepo.GetByIdAsync(departmentId, ct);
        if (department is null || !department.IsActive)
            return NotFound();

        if (tenantId.HasValue && department.TenantId != tenantId.Value)
            return Forbid();
        if (campusId.HasValue && department.CampusId != campusId.Value)
            return Forbid();

        return null;
    }
}
