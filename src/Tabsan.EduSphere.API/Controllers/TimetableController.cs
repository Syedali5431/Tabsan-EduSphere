using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages department timetables.
/// Admins/SuperAdmins can create, edit, publish, and delete timetables.
/// All authenticated users can read published timetables and download exports.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TimetableController : ControllerBase
{
    private readonly ITimetableService _service;
    private readonly Tabsan.EduSphere.Domain.Interfaces.IUserRepository _users;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public TimetableController(
        ITimetableService service,
        Tabsan.EduSphere.Domain.Interfaces.IUserRepository users,
        IAccessScopeResolver accessScope,
        ApplicationDbContext db)
    {
        _service = service;
        _users = users;
        _accessScope = accessScope;
        _db = db;
    }

    // ── GET /api/v1/timetable/department/{departmentId} ───────────────────────

    /// <summary>
    /// Returns all timetables for a department.
    /// Admins/SuperAdmins see both draft and published; other roles see published only.
    /// </summary>
    [HttpGet("department/{departmentId:guid}")]
    public async Task<IActionResult> GetByDepartment(
        Guid departmentId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, departmentId, ct);
        if (scope.Error is not null)
            return scope.Error;

        bool publishedOnly = !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin");
        var list = await _service.GetByDepartmentAsync(departmentId, publishedOnly, ct);
        return Ok(list);
    }

    // ── GET /api/v1/timetable/{id} ────────────────────────────────────────────

    /// <summary>Returns a full timetable with all entries by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var dto = await _service.GetByIdAsync(id, ct);
            // Non-admin users can only see published timetables
            if (!dto.IsPublished && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                return Forbid();

            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/timetable ────────────────────────────────────────────────

    /// <summary>Creates a new draft timetable.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTimetableCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, cmd.DepartmentId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var dto = await _service.CreateAsync(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // ── PUT /api/v1/timetable/{id} ────────────────────────────────────────────

    /// <summary>Updates the timetable title.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTimetableCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var dto = await _service.UpdateAsync(id, cmd, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/timetable/{id}/entries ───────────────────────────────────

    /// <summary>Adds a new scheduled slot to the timetable.</summary>
    [HttpPost("{id:guid}/entries")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AddEntry(
        Guid id,
        [FromBody] UpsertTimetableEntryCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var entry = await _service.AddEntryAsync(id, cmd, ct);
            return Ok(entry);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── PUT /api/v1/timetable/{id}/entries/{entryId} ──────────────────────────

    /// <summary>Updates an existing timetable entry.</summary>
    [HttpPut("{id:guid}/entries/{entryId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateEntry(
        Guid id,
        Guid entryId,
        [FromBody] UpsertTimetableEntryCommand cmd,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var entry = await _service.UpdateEntryAsync(id, entryId, cmd, ct);
            return Ok(entry);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── DELETE /api/v1/timetable/{id}/entries/{entryId} ───────────────────────

    /// <summary>Removes a scheduled slot from the timetable.</summary>
    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteEntry(
        Guid id,
        Guid entryId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.DeleteEntryAsync(id, entryId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/timetable/{id}/publish ───────────────────────────────────

    /// <summary>Publishes the timetable so it is visible to all department members.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.PublishAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/timetable/{id}/unpublish ─────────────────────────────────

    /// <summary>Unpublishes the timetable (returns it to draft for editing).</summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Unpublish(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.UnpublishAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── DELETE /api/v1/timetable/{id} ─────────────────────────────────────────

    /// <summary>Soft-deletes the timetable. Data is preserved in the database.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── GET /api/v1/timetable/{id}/export/excel ───────────────────────────────

    /// <summary>Downloads the timetable as an Excel (.xlsx) file.</summary>
    [HttpGet("{id:guid}/export/excel")]
    public async Task<IActionResult> ExportExcel(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var bytes = await _service.ExportExcelAsync(id, ct);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"timetable-{id:N}.xlsx");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── GET /api/v1/timetable/{id}/export/pdf ─────────────────────────────────

    /// <summary>Downloads the timetable as a PDF file.</summary>
    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, ct);
            if (scope.Error is not null)
                return scope.Error;

            var scopeResult = await EnsureTimetableIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
            if (scopeResult is not null)
                return scopeResult;

            var bytes = await _service.ExportPdfAsync(id, ct);
            return File(bytes, "application/pdf", $"timetable-{id:N}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── GET /api/v1/timetable/mine/teacher ────────────────────────────────────

    /// <summary>
    /// Returns all published timetable slots assigned to the currently authenticated faculty member.
    /// Accessible to Faculty, Admin, and SuperAdmin roles.
    /// </summary>
    [HttpGet("mine/teacher")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> GetMyTeacherTimetable(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User identity could not be resolved." });

        var entries = await _service.GetForTeacherAsync(userId, ct);
        return Ok(entries);
    }

    // ── GET /api/v1/timetable/faculty ─────────────────────────────────────────

    /// <summary>
    /// Returns active faculty users for timetable teacher dropdown selection.
    /// Admin and SuperAdmin only.
    /// </summary>
    [HttpGet("faculty")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetFacultyUsers(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] Guid? departmentId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, departmentId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var users = await _users.GetFacultyUsersAsync(ct);
        var filteredUsers = users.AsEnumerable();

        if (scope.TenantId.HasValue)
            filteredUsers = filteredUsers.Where(u => u.TenantId == scope.TenantId.Value);

        if (scope.CampusId.HasValue)
            filteredUsers = filteredUsers.Where(u => u.CampusId == scope.CampusId.Value);

        if (departmentId.HasValue)
            filteredUsers = filteredUsers.Where(u => u.DepartmentId == departmentId.Value);

        return Ok(filteredUsers.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.DepartmentId,
            DisplayName = string.IsNullOrWhiteSpace(u.Email)
                ? u.Username
                : $"{u.Username} ({u.Email})"
        }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private async Task<(Guid? TenantId, Guid? CampusId, IActionResult? Error)> ResolveEffectiveScopeAsync(
        Guid? requestedTenantId,
        Guid? requestedCampusId,
        Guid? requestedDepartmentId,
        CancellationToken ct)
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
        {
            return (null, null, BadRequest(new
            {
                message = "TenantId and CampusId must be provided together."
            }));
        }

        if (requestedDepartmentId.HasValue)
        {
            var departmentScope = await _db.Departments
                .AsNoTracking()
                .Where(d => d.Id == requestedDepartmentId.Value)
                .Select(d => new { d.TenantId, d.CampusId })
                .FirstOrDefaultAsync(ct);

            if (departmentScope is null)
            {
                return (null, null, NotFound(new
                {
                    message = $"Department {requestedDepartmentId} not found."
                }));
            }

            if (effectiveTenantId.HasValue && departmentScope.TenantId != effectiveTenantId.Value)
            {
                return (null, null, BadRequest(new
                {
                    message = "Department tenant scope does not match the requested tenant."
                }));
            }

            if (effectiveCampusId.HasValue && departmentScope.CampusId != effectiveCampusId.Value)
            {
                return (null, null, BadRequest(new
                {
                    message = "Department campus scope does not match the requested campus."
                }));
            }

            effectiveTenantId ??= departmentScope.TenantId;
            effectiveCampusId ??= departmentScope.CampusId;
        }

        return (effectiveTenantId, effectiveCampusId, null);
    }

    private async Task<IActionResult?> EnsureTimetableIsInScopeAsync(
        Guid timetableId,
        Guid? effectiveTenantId,
        Guid? effectiveCampusId,
        CancellationToken ct)
    {
        if (!effectiveTenantId.HasValue && !effectiveCampusId.HasValue)
            return null;

        var timetableScope = await _db.Timetables
            .AsNoTracking()
            .Where(t => t.Id == timetableId)
            .Select(t => new
            {
                DepartmentTenantId = t.Department.TenantId,
                DepartmentCampusId = t.Department.CampusId
            })
            .FirstOrDefaultAsync(ct);

        if (timetableScope is null)
            return null;

        if (effectiveTenantId.HasValue && timetableScope.DepartmentTenantId != effectiveTenantId.Value)
            return Forbid();

        if (effectiveCampusId.HasValue && timetableScope.DepartmentCampusId != effectiveCampusId.Value)
            return Forbid();

        return null;
    }
}
