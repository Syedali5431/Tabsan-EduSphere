using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Documents;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>Phase 7: Policy Document Management — ISO 9001 7.5. Admin/SuperAdmin for write, all roles for read.</summary>
[ApiController]
[Route("api/v1/policy-documents")]
public class PolicyDocumentController : ControllerBase
{
    private readonly IPolicyDocumentService _service;
    public PolicyDocumentController(IPolicyDocumentService service) => _service = service;

    private Guid GetUserId()
    {
        var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(val, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpGet("{id:guid}/versions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken ct) => Ok(await _service.GetVersionsAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePolicyDocumentRequest r, CancellationToken ct)
    {
        var uid = GetUserId(); if (uid == Guid.Empty) return Forbid();
        var doc = await _service.CreateAsync(r, uid, ct);
        return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePolicyDocumentRequest r, CancellationToken ct)
    {
        var uid = GetUserId(); if (uid == Guid.Empty) return Forbid();
        var doc = await _service.UpdateAsync(id, r, uid, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var doc = await _service.PublishAsync(id, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var doc = await _service.ArchiveAsync(id, ct);
        return doc is null ? NotFound() : Ok(doc);
    }
}
