using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages the room catalogue within buildings.
/// GET endpoints are available to all authenticated users (dropdown population).
/// Write endpoints are restricted to Admin and SuperAdmin.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RoomController : ControllerBase
{
    private readonly IBuildingRoomService _service;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public RoomController(IBuildingRoomService service, IAccessScopeResolver accessScope, ApplicationDbContext db)
    {
        _service = service;
        _accessScope = accessScope;
        _db = db;
    }

    // ── GET /api/v1/room ──────────────────────────────────────────────────────

    /// <summary>Returns all active rooms across all buildings (for dropdown population).</summary>
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

        var list = await _service.GetAllRoomsAsync(activeOnly, scope.TenantId, scope.CampusId, ct);
        return Ok(list);
    }

    // ── GET /api/v1/room/building/{buildingId} ────────────────────────────────

    /// <summary>Returns all active rooms within a specific building.</summary>
    [HttpGet("building/{buildingId:guid}")]
    public async Task<IActionResult> GetByBuilding(
        Guid buildingId,
        [FromQuery] bool activeOnly = true,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var buildingScopeResult = await EnsureBuildingIsInScopeAsync(buildingId, scope.TenantId, scope.CampusId, ct);
        if (buildingScopeResult is not null)
            return buildingScopeResult;

        var list = await _service.GetRoomsByBuildingAsync(buildingId, activeOnly, scope.TenantId, scope.CampusId, ct);
        return Ok(list);
    }

    // ── GET /api/v1/room/{id} ─────────────────────────────────────────────────

    /// <summary>Returns a single room by ID.</summary>
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

            var scopeResult = await EnsureRoomIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var dto = await _service.GetRoomByIdAsync(id, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/room ─────────────────────────────────────────────────────

    /// <summary>Creates a new room within a building.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoomCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = ResolveEffectiveScope(tenantId, campusId);
            if (scope.Error is not null)
                return scope.Error;

            if (!scope.TenantId.HasValue || !scope.CampusId.HasValue)
                return BadRequest(new { message = "Room creation requires both tenantId and campusId." });

            var buildingScopeResult = await EnsureBuildingIsInScopeAsync(cmd.BuildingId, scope.TenantId, scope.CampusId, ct);
            if (buildingScopeResult is not null)
                return buildingScopeResult;

            var dto = await _service.CreateRoomAsync(cmd, scope.TenantId, scope.CampusId, ct);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── PUT /api/v1/room/{id} ─────────────────────────────────────────────────

    /// <summary>Updates a room's number and capacity.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoomCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = ResolveEffectiveScope(tenantId, campusId);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureRoomIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var dto = await _service.UpdateRoomAsync(id, cmd, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/room/{id}/activate ───────────────────────────────────────

    /// <summary>Activates a room so it appears in timetable dropdowns.</summary>
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

            var scopeResult = await EnsureRoomIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.ActivateRoomAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/room/{id}/deactivate ─────────────────────────────────────

    /// <summary>Deactivates a room so it is hidden from timetable dropdowns.</summary>
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

            var scopeResult = await EnsureRoomIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.DeactivateRoomAsync(id, ct);
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

    private async Task<IActionResult?> EnsureRoomIsInScopeAsync(Guid roomId, Guid? effectiveTenantId, Guid? effectiveCampusId, CancellationToken ct)
    {
        if (!effectiveTenantId.HasValue && !effectiveCampusId.HasValue)
            return null;

        var roomScope = await _db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new { r.TenantId, r.CampusId })
            .FirstOrDefaultAsync(ct);

        if (roomScope is null)
            return null;

        if (effectiveTenantId.HasValue && roomScope.TenantId != effectiveTenantId.Value)
            return Forbid();

        if (effectiveCampusId.HasValue && roomScope.CampusId != effectiveCampusId.Value)
            return Forbid();

        return null;
    }
}
