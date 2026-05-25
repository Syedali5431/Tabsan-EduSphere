using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

// Final-Touches Phase 20 Stage 20.1/20.2 — LMS content REST API

/// <summary>
/// REST API for course content modules and video attachments (Phase 20).
/// Reading is open to all authenticated users.
/// Faculty/Admin/SuperAdmin can create, update, publish, and delete content.
/// </summary>
[ApiController]
[Route("api/v1/lms")]
[Authorize]
public class LmsController : ControllerBase
{
    private readonly ILmsService _lms;
    public LmsController(ILmsService lms) => _lms = lms;

    // ── Modules ────────────────────────────────────────────────────────────────

    /// <summary>Returns all content modules for the given offering.</summary>
    [HttpGet("modules/{offeringId:guid}")]
    public async Task<IActionResult> GetModules(Guid offeringId, [FromQuery] bool publishedOnly = false, CancellationToken ct = default)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        bool forcePublishedOnly = role is "Student" || publishedOnly;
        var modules = await _lms.GetModulesAsync(offeringId, forcePublishedOnly, ct);
        return Ok(modules);
    }

    /// <summary>Returns a single module with video list.</summary>
    [HttpGet("module/{moduleId:guid}")]
    public async Task<IActionResult> GetModule(Guid moduleId, CancellationToken ct = default)
    {
        var module = await _lms.GetModuleAsync(moduleId, ct);
        return module is null ? NotFound() : Ok(module);
    }

    /// <summary>Creates a new content module. Faculty/Admin/SuperAdmin only.</summary>
    [HttpPost("module")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request, CancellationToken ct = default)
    {
        try
        {
            var module = await _lms.CreateModuleAsync(request, ct);
            return CreatedAtAction(nameof(GetModule), new { moduleId = module.Id }, module);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Updates a content module's title, week, and body. Faculty/Admin/SuperAdmin only.</summary>
    [HttpPut("module/{moduleId:guid}")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateModule(Guid moduleId, [FromBody] UpdateModuleRequest request, CancellationToken ct = default)
    {
        await _lms.UpdateModuleAsync(moduleId, request, ct);
        return NoContent();
    }

    /// <summary>Publishes a module so students can see it. Faculty/Admin/SuperAdmin only.</summary>
    [HttpPost("module/{moduleId:guid}/publish")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> PublishModule(Guid moduleId, CancellationToken ct = default)
    {
        await _lms.PublishModuleAsync(moduleId, ct);
        return NoContent();
    }

    /// <summary>Unpublishes a module, hiding it from students. Faculty/Admin/SuperAdmin only.</summary>
    [HttpPost("module/{moduleId:guid}/unpublish")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> UnpublishModule(Guid moduleId, CancellationToken ct = default)
    {
        await _lms.UnpublishModuleAsync(moduleId, ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a content module. Faculty/Admin/SuperAdmin only.</summary>
    [HttpDelete("module/{moduleId:guid}")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteModule(Guid moduleId, CancellationToken ct = default)
    {
        await _lms.DeleteModuleAsync(moduleId, ct);
        return NoContent();
    }

    // ── Videos ─────────────────────────────────────────────────────────────────

    /// <summary>Attaches a video to a content module. Faculty/Admin/SuperAdmin only.</summary>
    [HttpPost("video")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> AddVideo([FromBody] AddVideoRequest request, CancellationToken ct = default)
    {
        var video = await _lms.AddVideoAsync(request, ct);
        return Ok(video);
    }

    /// <summary>Removes a video attachment. Faculty/Admin/SuperAdmin only.</summary>
    [HttpDelete("video/{videoId:guid}")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteVideo(Guid videoId, CancellationToken ct = default)
    {
        await _lms.DeleteVideoAsync(videoId, ct);
        return NoContent();
    }
}
