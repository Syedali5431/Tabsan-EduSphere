using Tabsan.EduSphere.Domain.Permissions;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Read-only repository for role-resource action permissions.
/// Permissions are global configuration — no scoping needed.
/// </summary>
public interface IRoleResourcePermissionRepository
{
    /// <summary>Returns all permission records for a given role name.</summary>
    Task<IReadOnlyList<RoleResourcePermission>> GetByRoleNameAsync(string roleName, CancellationToken ct = default);

    /// <summary>Returns all permission records (for cache priming).</summary>
    Task<IReadOnlyList<RoleResourcePermission>> GetAllAsync(CancellationToken ct = default);
}
