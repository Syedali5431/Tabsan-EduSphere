using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>Phase 10: Compliance Dashboard — aggregated ISO 27001/9001 posture. Admin/SuperAdmin only.</summary>
[ApiController]
[Route("api/v1/compliance")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class ComplianceDashboardController : ControllerBase
{
    private readonly IComplianceDashboardService _service;
    public ComplianceDashboardController(IComplianceDashboardService service) => _service = service;

    /// <summary>GET /api/v1/compliance/dashboard — full compliance posture overview.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _service.GetDashboardAsync(ct);
        return Ok(dashboard);
    }
}
