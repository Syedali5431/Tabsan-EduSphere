using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Permissions;

/// <summary>
/// Stores action-level permissions (View, Add, Edit, Deactivate, Export, Import)
/// per role per resource. Follows the existing RoleName-string pattern (no FK to Roles table).
/// SuperAdmin bypasses this table — they always have full access.
/// </summary>
public class RoleResourcePermission : BaseEntity
{
    /// <summary>
    /// Role name this permission applies to (e.g. "Admin", "Faculty", "Student").
    /// Stored as a string to avoid FK coupling to the Roles table.
    /// </summary>
    public string RoleName { get; private set; } = default!;

    /// <summary>
    /// Resource identifier — matches sidebar_menu_items.[Key].
    /// </summary>
    public string ResourceKey { get; private set; } = default!;

    public bool CanView { get; private set; }
    public bool CanAdd { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanDeactivate { get; private set; }
    public bool CanExport { get; private set; }
    public bool CanImport { get; private set; }

    private RoleResourcePermission() { }

    public RoleResourcePermission(
        string roleName,
        string resourceKey,
        bool canView = false,
        bool canAdd = false,
        bool canEdit = false,
        bool canDeactivate = false,
        bool canExport = false,
        bool canImport = false)
    {
        RoleName = roleName.Trim();
        ResourceKey = resourceKey.Trim();
        CanView = canView;
        CanAdd = canAdd;
        CanEdit = canEdit;
        CanDeactivate = canDeactivate;
        CanExport = canExport;
        CanImport = canImport;
    }
}
