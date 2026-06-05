using Microsoft.AspNetCore.Authorization;

namespace Tabsan.EduSphere.API.Authorization;

/// <summary>
/// Declarative permission check for controller actions.
/// Usage: [RequirePermission("courses", "Add")]
/// The resourceKey matches sidebar_menu_items.[Key].
/// The action is one of: View, Add, Edit, Deactivate, Export, Import.
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    private const string POLICY_PREFIX = "Permission_";

    public RequirePermissionAttribute(string resourceKey, string action)
    {
        ResourceKey = resourceKey;
        Action = action;
        Policy = $"{POLICY_PREFIX}{resourceKey}_{action}";
    }

    public string ResourceKey { get; }
    public string Action { get; }
}
