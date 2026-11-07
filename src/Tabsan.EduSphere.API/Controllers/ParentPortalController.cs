using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

// Phase 26 — Stage 26.3
// 2026-10-30 — lookups for the portal's Parent Links screen; Admins manage links of their assigned departments' students.

[ApiController]
[Route("api/v1/parent-portal")]
[Authorize]
public class ParentPortalController : ControllerBase
{
    private readonly IParentPortalService _service;
    private readonly IParentLinkDirectory _directory;
    private readonly IAdminAssignmentRepository _adminAssignments;
    private readonly IAccessScopeResolver _accessScope;

    public ParentPortalController(
        IParentPortalService service,
        IParentLinkDirectory directory,
        IAdminAssignmentRepository adminAssignments,
        IAccessScopeResolver accessScope)
    {
        _service = service;
        _directory = directory;
        _adminAssignments = adminAssignments;
        _accessScope = accessScope;
    }

    [HttpGet("me/students")]
    [Authorize(Roles = "Parent,Admin,SuperAdmin")]
    public async Task<IActionResult> GetMyLinkedStudents(CancellationToken ct)
    {
        var parentUserId = GetCurrentUserId();
        if (parentUserId == Guid.Empty)
            return BadRequest("Missing user_id claim.");

        var students = await _service.GetLinkedStudentsAsync(parentUserId, ct);
        return Ok(students);
    }

    [HttpGet("me/students/{studentProfileId:guid}/results")]
    [Authorize(Roles = "Parent,Admin,SuperAdmin")]
    public async Task<IActionResult> GetLinkedStudentResults(Guid studentProfileId, CancellationToken ct)
    {
        var parentUserId = GetCurrentUserId();
        if (parentUserId == Guid.Empty)
            return BadRequest("Missing user_id claim.");

        try
        {
            var items = await _service.GetLinkedStudentResultsAsync(parentUserId, studentProfileId, ct);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("me/students/{studentProfileId:guid}/attendance")]
    [Authorize(Roles = "Parent,Admin,SuperAdmin")]
    public async Task<IActionResult> GetLinkedStudentAttendance(Guid studentProfileId, [FromQuery] Guid? courseOfferingId, CancellationToken ct)
    {
        var parentUserId = GetCurrentUserId();
        if (parentUserId == Guid.Empty)
            return BadRequest("Missing user_id claim.");

        try
        {
            var items = await _service.GetLinkedStudentAttendanceAsync(parentUserId, studentProfileId, courseOfferingId, ct);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("me/students/{studentProfileId:guid}/announcements")]
    [Authorize(Roles = "Parent,Admin,SuperAdmin")]
    public async Task<IActionResult> GetLinkedStudentAnnouncements(Guid studentProfileId, [FromQuery] Guid? courseOfferingId, CancellationToken ct)
    {
        var parentUserId = GetCurrentUserId();
        if (parentUserId == Guid.Empty)
            return BadRequest("Missing user_id claim.");

        try
        {
            var items = await _service.GetLinkedStudentAnnouncementsAsync(parentUserId, studentProfileId, courseOfferingId, ct);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("me/students/{studentProfileId:guid}/timetable")]
    [Authorize(Roles = "Parent,Admin,SuperAdmin")]
    public async Task<IActionResult> GetLinkedStudentTimetable(Guid studentProfileId, [FromQuery] Guid? timetableId, CancellationToken ct)
    {
        var parentUserId = GetCurrentUserId();
        if (parentUserId == Guid.Empty)
            return BadRequest("Missing user_id claim.");

        try
        {
            var item = await _service.GetLinkedStudentTimetableAsync(parentUserId, studentProfileId, timetableId, ct);
            return Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Lookups for the Parent Links screen (Admin: own departments' students, own tenant's or tenant-less parents) ──

    /// <summary>Parent-role accounts whose username, name, email or phone contains <paramref name="search"/>.</summary>
    [HttpGet("parents")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> SearchParents([FromQuery] string? search, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var scope = await GetScopeAsync(ct);
        return scope is null ? Forbid() : Ok(await _directory.SearchParentsAsync(search, scope, take, ct));
    }

    [HttpGet("parents/{parentUserId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetParent(Guid parentUserId, CancellationToken ct)
    {
        var scope = await GetScopeAsync(ct);
        if (scope is null) return Forbid();
        var parent = await _directory.GetParentAsync(parentUserId, scope, ct);
        return parent is null ? NotFound(new { message = "Parent account not found." }) : Ok(parent);
    }

    /// <summary>The parent's links with each child's name and class (only children the caller may manage).</summary>
    [HttpGet("parents/{parentUserId:guid}/links")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetParentLinkDetails(Guid parentUserId, CancellationToken ct)
    {
        var scope = await GetScopeAsync(ct);
        return scope is null ? Forbid() : Ok(await _directory.GetParentLinksAsync(parentUserId, scope, ct));
    }

    /// <summary>School students whose registration number, name or username contains <paramref name="search"/>.</summary>
    [HttpGet("students")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> SearchStudents([FromQuery] string? search, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var scope = await GetScopeAsync(ct);
        return scope is null ? Forbid() : Ok(await _directory.SearchSchoolStudentsAsync(search, scope, take, ct));
    }

    [HttpGet("links/{parentUserId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetLinksByParent(Guid parentUserId, CancellationToken ct)
        => Ok(await _service.GetLinksByParentAsync(parentUserId, ct));

    [HttpPut("links")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> UpsertLink([FromBody] UpsertParentStudentLinkRequest request, CancellationToken ct)
    {
        if (await ForbidStudentOutsideScopeAsync(request.StudentProfileId, ct) is { } denied)
            return denied;

        try
        {
            var dto = await _service.UpsertLinkAsync(request, ct);
            return Ok(dto);
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

    /// <summary>Pauses the link (inactive); with <c>remove=true</c> removes it (linking again restores it).</summary>
    [HttpDelete("links/{parentUserId:guid}/{studentProfileId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeactivateLink(Guid parentUserId, Guid studentProfileId, [FromQuery] bool remove = false, CancellationToken ct = default)
    {
        if (await ForbidStudentOutsideScopeAsync(studentProfileId, ct) is { } denied)
            return denied;

        var changed = remove
            ? await _service.RemoveLinkAsync(parentUserId, studentProfileId, ct)
            : await _service.DeactivateLinkAsync(parentUserId, studentProfileId, ct);
        return changed ? NoContent() : NotFound();
    }

    /// <summary>SuperAdmin: everything. Admin: assigned departments and own tenant. Null when the caller cannot be identified.</summary>
    private async Task<ParentLinkScope?> GetScopeAsync(CancellationToken ct)
    {
        if (User.IsInRole("SuperAdmin"))
            return ParentLinkScope.Unrestricted;

        var adminUserId = GetCurrentUserId();
        if (adminUserId == Guid.Empty)
            return null;

        var departmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(adminUserId, ct);
        return new ParentLinkScope(departmentIds, _accessScope.GetTenantId());
    }

    /// <summary>403 when an Admin changes a link of a student outside their departments (a missing student is the service's 404).</summary>
    private async Task<IActionResult?> ForbidStudentOutsideScopeAsync(Guid studentProfileId, CancellationToken ct)
    {
        var scope = await GetScopeAsync(ct);
        if (scope is null)
            return Forbid();
        if (scope.DepartmentIds is null)
            return null;

        var departmentId = await _directory.GetStudentDepartmentIdAsync(studentProfileId, ct);
        return departmentId is { } id && !scope.AllowsDepartment(id) ? Forbid() : null;
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirst("user_id")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
