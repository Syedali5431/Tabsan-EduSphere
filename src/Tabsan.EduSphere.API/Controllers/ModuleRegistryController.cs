using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Middleware;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Returns the module visibility list for the authenticated user, combining the static
/// module registry with live activation status and the current institution policy.
/// </summary>
[ApiController]
[Route("api/v1/module-registry")]
[Authorize]
public sealed class ModuleRegistryController : ControllerBase
{
    private readonly IModuleRegistryService _registry;

    public ModuleRegistryController(IModuleRegistryService registry)
        => _registry = registry;

    /// <summary>Returns all modules with visibility resolved for the current user.</summary>
    [HttpGet("visible")]
    public async Task<IActionResult> GetVisible(CancellationToken ct)
    {
        var role   = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                     ?? User.FindFirst("role")?.Value ?? "";
        var policy = HttpContext.GetInstitutionVisibility();

        var results = await _registry.GetVisibleModulesAsync(role, policy, ct);

        return Ok(results.Select(r => new
        {
            r.Key,
            r.Name,
            r.IsActive,
            r.IsAccessible
        }));
    }
}
