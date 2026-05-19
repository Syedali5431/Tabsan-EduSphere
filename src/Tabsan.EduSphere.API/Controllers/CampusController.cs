using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public sealed class CampusController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CampusController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? tenantId, CancellationToken ct)
    {
        var query = _db.Campuses.AsNoTracking();
        if (tenantId.HasValue)
            query = query.Where(c => c.TenantId == tenantId.Value);

        var campuses = await query
            .OrderBy(c => c.Code)
            .Select(c => new
            {
                c.Id,
                c.TenantId,
                c.Code,
                c.Name,
                c.IsActive
            })
            .ToListAsync(ct);

        return Ok(campuses);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCampusRequest request, CancellationToken ct)
    {
        if (request.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("TenantId, Code and Name are required.");

        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == request.TenantId, ct);
        if (!tenantExists)
            return NotFound("Tenant not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _db.Campuses.AnyAsync(c => c.TenantId == request.TenantId && c.Code == code, ct);
        if (exists)
            return Conflict($"Campus code '{code}' is already in use for this tenant.");

        var campus = new Campus(request.TenantId, code, request.Name);
        await _db.Campuses.AddAsync(campus, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new { campus.Id, campus.TenantId, campus.Code, campus.Name, campus.IsActive });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampusRequest request, CancellationToken ct)
    {
        var campus = await _db.Campuses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (campus is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.NewName))
            campus.Rename(request.NewName);

        _db.Campuses.Update(campus);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var campus = await _db.Campuses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (campus is null)
            return NotFound();

        campus.Activate();
        _db.Campuses.Update(campus);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var campus = await _db.Campuses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (campus is null)
            return NotFound();

        campus.Deactivate();
        _db.Campuses.Update(campus);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record CreateCampusRequest(Guid TenantId, string Code, string Name);
    public sealed record UpdateCampusRequest(string NewName);
}
