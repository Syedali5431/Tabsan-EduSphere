using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>Phase 9: Data Integrity — transaction safety and audit coverage verification. Admin/SuperAdmin only.</summary>
[ApiController]
[Route("api/v1/data-integrity")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class DataIntegrityController : ControllerBase
{
    private readonly IDataIntegrityService _service;
    public DataIntegrityController(IDataIntegrityService service) => _service = service;

    /// <summary>GET /api/v1/data-integrity/check — runs a full integrity check.</summary>
    [HttpGet("check")]
    public async Task<IActionResult> RunCheck(CancellationToken ct)
    {
        var report = await _service.RunIntegrityCheckAsync(ct);
        return Ok(report);
    }
}
