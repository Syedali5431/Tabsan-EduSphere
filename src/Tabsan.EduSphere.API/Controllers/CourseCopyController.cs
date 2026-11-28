using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Lms;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Course copy and templates (/api/v1/course-copy). Teachers and admins copy content into a course they look after, from
/// another course they look after or from a template shared with them; admins share courses as templates.
/// </summary>
[ApiController]
[Route("api/v1/course-copy")]
[Authorize(Roles = "SuperAdmin,Admin,Faculty")]
public sealed class CourseCopyController : ControllerBase
{
    private readonly CourseCopyService _copy;
    private readonly IAccessScopeResolver _scope;

    public CourseCopyController(CourseCopyService copy, IAccessScopeResolver scope)
    {
        _copy = copy;
        _scope = scope;
    }

    /// <summary>Courses and templates that can be copied into the course.</summary>
    [HttpGet("offerings/{offeringId:guid}/sources")]
    public async Task<IActionResult> Sources(Guid offeringId, CancellationToken ct) => Answer(await _copy.SourcesAsync(Actor(), offeringId, ct));

    /// <summary>What a source holds, part by part, what cannot be copied, and the suggested date shift.</summary>
    [HttpGet("offerings/{offeringId:guid}/preview")]
    public async Task<IActionResult> Preview(Guid offeringId, [FromQuery] Guid sourceId, CancellationToken ct)
        => Answer(await _copy.PreviewAsync(Actor(), offeringId, sourceId, ct));

    /// <summary>Copies the chosen content of the source into the course.</summary>
    [HttpPost("offerings/{offeringId:guid}/copy")]
    public async Task<IActionResult> Copy(Guid offeringId, [FromBody] CourseCopyRequest request, CancellationToken ct)
        => Answer(await _copy.CopyAsync(Actor(), offeringId, request, DateTime.UtcNow, ct));

    /// <summary>Copies made into the course, newest first.</summary>
    [HttpGet("offerings/{offeringId:guid}/history")]
    public async Task<IActionResult> History(Guid offeringId, CancellationToken ct) => Answer(await _copy.HistoryAsync(Actor(), offeringId, ct));

    /// <summary>The course's template settings (204 when it is not a template).</summary>
    [HttpGet("offerings/{offeringId:guid}/template")]
    public async Task<IActionResult> Template(Guid offeringId, CancellationToken ct)
    {
        var result = await _copy.TemplateOfAsync(Actor(), offeringId, ct);
        if (result.Ok && result.Value is null) return NoContent();
        return Answer(result);
    }

    /// <summary>Shares the course as a template, or changes the template (admins).</summary>
    [HttpPut("offerings/{offeringId:guid}/template")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SaveTemplate(Guid offeringId, [FromBody] SaveCourseTemplateRequest request, CancellationToken ct)
        => Answer(await _copy.SaveTemplateAsync(Actor(), offeringId, request, ct));

    /// <summary>Stops sharing the course as a template (admins).</summary>
    [HttpDelete("offerings/{offeringId:guid}/template")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveTemplate(Guid offeringId, CancellationToken ct)
    {
        var result = await _copy.RemoveTemplateAsync(Actor(), offeringId, ct);
        return result.Ok ? NoContent() : StatusCode(result.Status, new { message = result.Error });
    }

    private IActionResult Answer<T>(CourseCopyResult<T> result)
        => result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });

    private CourseCopyActor Actor()
    {
        var id = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId) ? userId : Guid.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
        return new CourseCopyActor(id, role, _scope.GetTenantId());
    }
}
