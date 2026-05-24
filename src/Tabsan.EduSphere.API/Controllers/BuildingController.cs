using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages the building catalogue used as timetable entry dropdown source.
/// GET endpoints are available to all authenticated users (dropdown population).
/// Write endpoints are restricted to Admin and SuperAdmin.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BuildingController : ControllerBase
{
    private readonly IBuildingRoomService _service;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public BuildingController(IBuildingRoomService service, IAccessScopeResolver accessScope, ApplicationDbContext db)
    {
        _service = service;
        _accessScope = accessScope;
        _db = db;
    }

    // ── GET /api/v1/building ──────────────────────────────────────────────────

    /// <summary>Returns all active buildings (for dropdown population).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool activeOnly = true,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var list = await _service.GetAllBuildingsAsync(activeOnly, scope.TenantId, scope.CampusId, ct);
        return Ok(list);
    }

    // ── GET /api/v1/building/{id} ─────────────────────────────────────────────

    /// <summary>Returns a single building by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = ResolveEffectiveScope(tenantId, campusId);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureBuildingIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var dto = await _service.GetBuildingByIdAsync(id, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/building ─────────────────────────────────────────────────

    /// <summary>Creates a new building.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateBuildingCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        if (!scope.TenantId.HasValue || !scope.CampusId.HasValue)
            return BadRequest(new { message = "Building creation requires both tenantId and campusId." });

        var dto = await _service.CreateBuildingAsync(cmd, scope.TenantId, scope.CampusId, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // ── PUT /api/v1/building/{id} ─────────────────────────────────────────────

    /// <summary>Updates a building's name and code.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBuildingCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = ResolveEffectiveScope(tenantId, campusId);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureBuildingIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var dto = await _service.UpdateBuildingAsync(id, cmd, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/building/{id}/activate ──────────────────────────────────

    /// <summary>Activates a building so it appears in timetable dropdowns.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Activate(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = ResolveEffectiveScope(tenantId, campusId);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureBuildingIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.ActivateBuildingAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/building/{id}/deactivate ─────────────────────────────────

    /// <summary>Deactivates a building so it is hidden from timetable dropdowns.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = ResolveEffectiveScope(tenantId, campusId);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureBuildingIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.DeactivateBuildingAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
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

    private async Task<IActionResult?> EnsureBuildingIsInScopeAsync(Guid buildingId, Guid? effectiveTenantId, Guid? effectiveCampusId, CancellationToken ct)
    {
        if (!effectiveTenantId.HasValue && !effectiveCampusId.HasValue)
            return null;

        var buildingScope = await _db.Buildings
            .AsNoTracking()
            .Where(b => b.Id == buildingId)
            .Select(b => new { b.TenantId, b.CampusId })
            .FirstOrDefaultAsync(ct);

        if (buildingScope is null)
            return null;

        if (effectiveTenantId.HasValue && buildingScope.TenantId != effectiveTenantId.Value)
            return Forbid();

        if (effectiveCampusId.HasValue && buildingScope.CampusId != effectiveCampusId.Value)
            return Forbid();

        return null;
    }
}
