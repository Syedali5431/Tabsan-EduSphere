using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Modules;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Exposes module activation controls to Super Admin users only.
/// All endpoints are under /api/v1/modules and require the SuperAdmin role.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// The portal client calls the plural form; keep both so neither path 404s.
[Route("api/v1/modules")]
[Authorize(Roles = "SuperAdmin")]
public class ModuleController : ControllerBase
{
    private readonly IModuleService _modules;
    private readonly IModuleEntitlementResolver _resolver;
    private readonly IModuleRolesService _moduleRoles;

    public ModuleController(
        IModuleService modules,
        IModuleEntitlementResolver resolver,
        IModuleRolesService moduleRoles)
    {
        _modules = modules;
        _resolver = resolver;
        _moduleRoles = moduleRoles;
    }

    // ── GET /api/v1/modules/all-settings ─────────────────────────────────────

    /// <summary>
    /// Returns every module with its activation state and assigned roles combined
    /// into a single list. Used by the Super Admin Module Settings view.
    /// </summary>
    [HttpGet("all-settings")]
    public async Task<IActionResult> AllSettings(CancellationToken ct)
    {
        var modules   = await _modules.GetAllAsync(ct);
        var rolesList = await _moduleRoles.GetAllAsync(ct);
        var rolesMap  = rolesList.ToDictionary(r => r.ModuleId);

        var result = new List<ModuleSettingsDto>(modules.Count);
        foreach (var m in modules)
        {
            var isActive = await _resolver.IsActiveAsync(m.Key, ct);
            rolesMap.TryGetValue(m.Id, out var roles);
            result.Add(new ModuleSettingsDto(
                m.Id,
                m.Key,
                m.Name,
                m.IsMandatory,
                isActive,
                roles?.AssignedRoles ?? new List<string>()
            ));
        }
        return Ok(result);
    }

    // ── GET /api/v1/modules ────────────────────────────────────────────────────

    /// <summary>Returns all modules with their current active/inactive status.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var modules = await _modules.GetAllAsync(ct);
        return Ok(modules);
    }

    // ── POST /api/v1/modules/{key}/activate ───────────────────────────────────

    /// <summary>Activates the named module if it is currently inactive.</summary>
    [HttpPost("{key}/activate")]
    public async Task<IActionResult> Activate(string key, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        try
        {
            await _modules.ActivateAsync(key, userId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST /api/v1/modules/{key}/deactivate ─────────────────────────────────

    /// <summary>Deactivates the named module. Returns 400 when the module is mandatory.</summary>
    [HttpPost("{key}/deactivate")]
    public async Task<IActionResult> Deactivate(string key, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        try
        {
            await _modules.DeactivateAsync(key, userId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── GET /api/v1/modules/{key}/status ──────────────────────────────────────

    /// <summary>Returns whether a single named module is currently active. Uses the cache.</summary>
    [HttpGet("{key}/status")]
    public async Task<IActionResult> Status(string key, CancellationToken ct)
    {
        var isActive = await _resolver.IsActiveAsync(key, ct);
        return Ok(new { key, isActive });
    }

    // ── GET /api/v1/modules/{key}/roles ───────────────────────────────────────

    /// <summary>Returns the roles currently assigned to access the named module.</summary>
    [HttpGet("{key}/roles")]
    public async Task<IActionResult> GetRoles(string key, CancellationToken ct)
    {
        try
        {
            var dto = await _moduleRoles.GetByModuleKeyAsync(key, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── PUT /api/v1/modules/{key}/roles ───────────────────────────────────────

    /// <summary>Replaces all role assignments for the named module. Pass an empty array to clear all.</summary>
    [HttpPut("{key}/roles")]
    public async Task<IActionResult> SetRoles(string key, [FromBody] SetRolesCommand cmd, CancellationToken ct)
    {
        try
        {
            await _moduleRoles.SetRolesAsync(key, cmd, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Extracts the authenticated user's GUID from the JWT sub claim.</summary>
    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
