using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

// Final-Touches Phase 20 Stage 20.4 — course announcement REST API

/// <summary>
/// REST API for course announcements (Phase 20).
/// All authenticated users can read announcements.
/// Faculty/Admin/SuperAdmin can create and delete.
/// </summary>
[ApiController]
[Route("api/v1/announcement")]
[Authorize]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementService _announcements;
    public AnnouncementController(IAnnouncementService announcements) => _announcements = announcements;

    /// <summary>Returns all announcements for the given offering.</summary>
    [HttpGet("{offeringId:guid}")]
    public async Task<IActionResult> GetAnnouncements(
        Guid offeringId,
        [FromQuery] bool includeInactive = true,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        var items = await _announcements.GetByOfferingAsync(offeringId, includeInactive, tenantId, campusId, ct);
        return Ok(items);
    }

    /// <summary>Posts an announcement (notifies enrolled students). Faculty/Admin/SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> CreateAnnouncement(
        [FromBody] CreateAnnouncementRequest request,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        if (!request.OfferingId.HasValue || request.OfferingId.Value == Guid.Empty)
            return BadRequest(new { message = "OfferingId is required." });

        var idStr    = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (!Guid.TryParse(idStr, out var authorId)) return Unauthorized();
        var actualRequest = request with { AuthorId = authorId };
        try
        {
            var item = await _announcements.CreateAsync(actualRequest, tenantId, campusId, ct);
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Activates/deactivates an announcement. Faculty/Admin/SuperAdmin only.</summary>
    [HttpPost("{announcementId:guid}/active")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> SetActive(
        Guid announcementId,
        [FromQuery] bool isActive,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        await _announcements.SetActiveAsync(announcementId, isActive, tenantId, campusId, ct);
        return NoContent();
    }

    /// <summary>Soft-deletes an announcement. Faculty/Admin/SuperAdmin only.</summary>
    [HttpDelete("{announcementId:guid}")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteAnnouncement(
        Guid announcementId,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? campusId = null,
        CancellationToken ct = default)
    {
        await _announcements.DeleteAsync(announcementId, tenantId, campusId, ct);
        return NoContent();
    }
}
