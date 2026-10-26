using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Middleware;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

// Phase 27 — University Portal Parity and Student Experience — Stage 27.1

[ApiController]
[Route("api/v1/portal-capabilities")]
[Authorize]
public sealed class PortalCapabilitiesController : ControllerBase
{
    private readonly IPortalCapabilityMatrixService _service;

    public PortalCapabilitiesController(IPortalCapabilityMatrixService service)
    {
        _service = service;
    }

    [HttpGet("matrix")]
    public async Task<IActionResult> GetMatrix(CancellationToken ct)
    {
        var policy = HttpContext.GetInstitutionVisibility();
        var matrix = await _service.GetMatrixAsync(policy, ct);
        return Ok(matrix);
    }
}
