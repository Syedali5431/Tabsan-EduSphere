using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public sealed class TenantController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TenantController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenants = await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .Select(t => new
            {
                t.Id,
                t.Code,
                t.Name,
                t.IsActive
            })
            .ToListAsync(ct);

        return Ok(tenants);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Code and Name are required.");

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _db.Tenants.AnyAsync(t => t.Code == code, ct);
        if (exists)
            return Conflict($"Tenant code '{code}' is already in use.");

        var tenant = new Tenant(code, request.Name);
        await _db.Tenants.AddAsync(tenant, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new { tenant.Id, tenant.Code, tenant.Name, tenant.IsActive });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.NewName))
            tenant.Rename(request.NewName);

        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null)
            return NotFound();

        tenant.Activate();
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null)
            return NotFound();

        tenant.Deactivate();
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record CreateTenantRequest(string Code, string Name);
    public sealed record UpdateTenantRequest(string NewName);
}
