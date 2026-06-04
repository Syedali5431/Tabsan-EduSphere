using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.LoginActivity;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Phase 3: Login activity monitoring — query login attempts and dashboard trends.
/// ISO 27001 A.12.4.1 — structured event logging for security monitoring.
/// Admin and SuperAdmin only.
/// </summary>
[ApiController]
[Route("api/v1/login-activity")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class LoginActivityController : ControllerBase
{
    private readonly ILoginActivityService _activityService;

    public LoginActivityController(ILoginActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>
    /// GET /api/v1/login-activity
    /// Returns paged login activity with optional filters for user, status, risk level, failure reason, and date range.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActivity([FromQuery] LoginActivityFilterDto filter, CancellationToken ct)
    {
        if (filter.ToUtc.HasValue && filter.FromUtc.HasValue && filter.ToUtc.Value < filter.FromUtc.Value)
            return BadRequest(new { message = "toUtc must be greater than or equal to fromUtc." });

        var result = await _activityService.GetActivityAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/login-activity/summary
    /// Returns aggregated login activity summary with daily breakdown, top failure reasons, and top IPs.
    /// Default range: last 30 days.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
    {
        if (toUtc.HasValue && fromUtc.HasValue && toUtc.Value < fromUtc.Value)
            return BadRequest(new { message = "toUtc must be greater than or equal to fromUtc." });

        var summary = await _activityService.GetSummaryAsync(fromUtc, toUtc, ct);
        return Ok(summary);
    }
}
