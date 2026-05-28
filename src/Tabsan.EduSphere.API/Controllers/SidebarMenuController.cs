using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages sidebar navigation menu items and their per-role visibility settings.
/// Super Admin always bypasses role restrictions — these settings apply only to
/// Admin, Faculty, and Student roles.
/// </summary>
[ApiController]
[Route("api/v1/sidebar-menu")]
public class SidebarMenuController : ControllerBase
{
    private readonly ISidebarMenuService _service;
    private readonly IModuleEntitlementResolver _moduleEntitlement;
    private readonly IInstitutionPolicyService _institutionPolicy;

    private static readonly HashSet<string> UniversityOnlyDegreeAuditMenuKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "degree_audit",
            "graduation_eligibility",
            "degree_rules",
            "generate_certificates"
        };

    private static readonly IReadOnlyDictionary<string, string> MenuModuleKeyMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["departments"] = "departments",
            ["courses"] = "courses",
            ["prerequisites"] = "courses",
            ["degree_audit"] = "courses",
            ["degree_rules"] = "courses",
            ["graduation_eligibility"] = "courses",
            ["graduation_apply"] = "courses",
            ["graduation_applications"] = "courses",
            ["generate_certificates"] = "courses",
            ["grading_config"] = "courses",
            ["study_plan"] = "courses",
            ["lms_manage"] = "courses",
            ["course_material"] = "courses",
            ["discussion"] = "courses",
            ["announcements"] = "courses",
            ["students"] = "sis",
            ["user_import"] = "sis",
            ["admin_users"] = "sis",
            ["student_lifecycle"] = "sis",
            ["payments"] = "sis",
            ["enrollments"] = "sis",
            ["programs"] = "courses",
            ["assignments"] = "assignments",
            ["rubric_manage"] = "assignments",
            ["attendance"] = "attendance",
            ["results"] = "results",
            ["enter_results"] = "results",
            ["gradebook"] = "results",
            ["result_calculation"] = "results",
            ["quizzes"] = "quizzes",
            ["fyp"] = "fyp",
            ["notifications"] = "notifications",
            ["helpdesk"] = "notifications",
            ["analytics"] = "reports",
            ["report_center"] = "reports",
            ["report_settings"] = "reports",
            ["ai_chat"] = "ai_chat",
            ["theme_settings"] = "themes"
        };

    public SidebarMenuController(
        ISidebarMenuService service,
        IModuleEntitlementResolver moduleEntitlement,
        IInstitutionPolicyService institutionPolicy)
    {
        _service = service;
        _moduleEntitlement = moduleEntitlement;
        _institutionPolicy = institutionPolicy;
    }

    // ── GET /api/v1/sidebar-menu/my-visible ───────────────────────────────────

    /// <summary>
    /// Returns sidebar menu items visible to the current authenticated user.
    /// SuperAdmin always sees all menu items regardless of role/status rules.
    /// </summary>
    [HttpGet("my-visible")]
    [Authorize]
    public async Task<IActionResult> GetVisibleForCurrentUser(CancellationToken ct)
    {
        var policy = await _institutionPolicy.GetPolicyAsync(ct);

        if (User.IsInRole("SuperAdmin"))
        {
            var allMenus = await _service.GetTopLevelMenusAsync(ct);
            var policyFiltered = ApplyInstitutionPolicyFilters(allMenus, policy);
            return Ok(policyFiltered.OrderBy(m => m.DisplayOrder));
        }

        var effectiveRoles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Keep all non-SuperAdmin roles from the token so roles like Finance/Parent
        // can receive sidebar menus when access is configured in the DB.
        var filteredRoles = effectiveRoles
            .Where(r => !string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filteredRoles.Count == 0)
            return Ok(Array.Empty<SidebarMenuItemDto>());

        var topLevel = await _service.GetTopLevelMenusAsync(ct);
        var visible = FilterVisible(topLevel, filteredRoles);
        visible = ApplyInstitutionPolicyFilters(visible, policy);
        var moduleFilteredVisible = await FilterByModuleActivationAsync(visible, ct);
        return Ok(moduleFilteredVisible);
    }

    // ── GET /api/v1/sidebar-menu ──────────────────────────────────────────────

    /// <summary>Returns all top-level menu items with sub-menus and role access lists.</summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetTopLevel(CancellationToken ct)
    {
        var items = await _service.GetTopLevelMenusAsync(ct);
        return Ok(items);
    }

    // ── GET /api/v1/sidebar-menu/{id} ─────────────────────────────────────────

    /// <summary>Returns a single menu item by ID with full role access detail.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── GET /api/v1/sidebar-menu/{id}/sub-menus ───────────────────────────────

    /// <summary>Returns the sub-menu items under a given top-level menu item.</summary>
    [HttpGet("{id:guid}/sub-menus")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetSubMenus(Guid id, CancellationToken ct)
    {
        var items = await _service.GetSubMenusAsync(id, ct);
        return Ok(items);
    }

    // ── PUT /api/v1/sidebar-menu/{id}/roles ───────────────────────────────────

    /// <summary>
    /// Replaces the role access assignments for a menu item.
    /// </summary>
    [HttpPut("{id:guid}/roles")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetRoles(Guid id, [FromBody] SetSidebarMenuRolesCommand cmd, CancellationToken ct)
    {
        try
        {
            await _service.SetRolesAsync(id, cmd, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── PUT /api/v1/sidebar-menu/{id}/status ──────────────────────────────────

    /// <summary>
    /// Activates or deactivates a menu item.
    /// System menus cannot be deactivated — a 409 Conflict is returned if attempted.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetSidebarMenuStatusCommand cmd, CancellationToken ct)
    {
        try
        {
            await _service.SetStatusAsync(id, cmd, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private static IList<SidebarMenuItemDto> FilterVisible(IEnumerable<SidebarMenuItemDto> topLevelMenus, IList<string> roles)
    {
        var result = new List<SidebarMenuItemDto>();

        foreach (var menu in topLevelMenus.OrderBy(m => m.DisplayOrder))
        {
            var visibleSubMenus = menu.SubMenus
                .Where(s => IsVisibleForRoles(s, roles))
                .OrderBy(s => s.DisplayOrder)
                .ToList();

            // Keep parent if parent itself is visible OR at least one child is visible.
            if (IsVisibleForRoles(menu, roles) || visibleSubMenus.Count > 0)
                result.Add(menu with { SubMenus = visibleSubMenus });
        }

        return result;
    }

    private static bool IsVisibleForRoles(SidebarMenuItemDto menu, IList<string> roles)
    {
        if (!menu.IsActive) return false;

        return menu.RoleAccesses.Any(a =>
            a.IsAllowed && roles.Contains(a.RoleName, StringComparer.OrdinalIgnoreCase));
    }

    private async Task<IList<SidebarMenuItemDto>> FilterByModuleActivationAsync(
        IEnumerable<SidebarMenuItemDto> menus,
        CancellationToken ct)
    {
        var result = new List<SidebarMenuItemDto>();

        foreach (var menu in menus.OrderBy(m => m.DisplayOrder))
        {
            var children = await FilterByModuleActivationAsync(menu.SubMenus, ct);
            var moduleVisible = await IsMenuModuleVisibleAsync(menu.Key, ct);

            if (moduleVisible || children.Count > 0)
                result.Add(menu with { SubMenus = children.ToList() });
        }

        return result;
    }

    private async Task<bool> IsMenuModuleVisibleAsync(string menuKey, CancellationToken ct)
    {
        if (!MenuModuleKeyMap.TryGetValue(menuKey, out var moduleKey))
            return true;

        return await _moduleEntitlement.IsActiveAsync(moduleKey, ct);
    }

    private static IList<SidebarMenuItemDto> ApplyInstitutionPolicyFilters(
        IEnumerable<SidebarMenuItemDto> menus,
        InstitutionPolicySnapshot policy)
    {
        var result = new List<SidebarMenuItemDto>();

        foreach (var menu in menus.OrderBy(m => m.DisplayOrder))
        {
            if (!policy.IncludeUniversity && UniversityOnlyDegreeAuditMenuKeys.Contains(menu.Key))
                continue;

            var children = ApplyInstitutionPolicyFilters(menu.SubMenus, policy);
            result.Add(menu with { SubMenus = children.ToList() });
        }

        return result;
    }
}
