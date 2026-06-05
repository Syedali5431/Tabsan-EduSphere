using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Tabsan.EduSphere.Application.Authorization;

namespace Tabsan.EduSphere.API.Authorization;

/// <summary>
/// Evaluates Permission_* policies by checking the user's role against
/// the IPermissionService. SuperAdmin always succeeds.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissions;

    public PermissionAuthorizationHandler(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)
                     ?? context.User.FindFirst("role");

        if (roleClaim is null)
            return Task.CompletedTask; // no role → fail

        var role = roleClaim.Value;

        // SuperAdmin bypasses all permission checks
        if (_permissions.IsSuperAdmin(role))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var allowed = requirement.Action switch
        {
            "View"        => _permissions.CanView(role, requirement.ResourceKey),
            "Add"         => _permissions.CanAdd(role, requirement.ResourceKey),
            "Edit"        => _permissions.CanEdit(role, requirement.ResourceKey),
            "Deactivate"  => _permissions.CanDeactivate(role, requirement.ResourceKey),
            "Export"      => _permissions.CanExport(role, requirement.ResourceKey),
            "Import"      => _permissions.CanImport(role, requirement.ResourceKey),
            _             => false
        };

        if (allowed)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>Simple requirement carrying the resource key and action being checked.</summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string ResourceKey { get; }
    public string Action { get; }

    public PermissionRequirement(string resourceKey, string action)
    {
        ResourceKey = resourceKey;
        Action = action;
    }
}
