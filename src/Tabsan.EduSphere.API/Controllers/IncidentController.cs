using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Incidents;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>Phase 6: Incident Management — ISO 27001 A.16.1.5. Admin/SuperAdmin only.</summary>
[ApiController]
[Route("api/v1/incidents")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _service;
    public IncidentController(IIncidentService service) => _service = service;

    private Guid GetUserId()
    {
        var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(val, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentRequest request, CancellationToken ct)
    {
        var uid = GetUserId(); if (uid == Guid.Empty) return Forbid();
        var r = await _service.CreateAsync(request, uid, ct);
        return CreatedAtAction(nameof(GetById), new { id = r.Id }, r);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateIncidentStatusRequest request, CancellationToken ct)
    {
        var r = await _service.UpdateStatusAsync(id, request, ct);
        return r is null ? NotFound(new { message = "Incident not found or invalid action." }) : Ok(r);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct) => Ok(await _service.GetSummaryAsync(ct));
}
