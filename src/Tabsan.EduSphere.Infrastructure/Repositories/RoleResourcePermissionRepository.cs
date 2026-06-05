using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Permissions;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>
/// Read-only repository for role-resource permissions.
/// Permissions are global configuration — no data scoping applied.
/// </summary>
public class RoleResourcePermissionRepository : IRoleResourcePermissionRepository
{
    private readonly ApplicationDbContext _db;

    public RoleResourcePermissionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleResourcePermission>> GetByRoleNameAsync(
        string roleName, CancellationToken ct = default)
    {
        return await _db.RoleResourcePermissions
            .AsNoTracking()
            .Where(p => p.RoleName == roleName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RoleResourcePermission>> GetAllAsync(
        CancellationToken ct = default)
    {
        return await _db.RoleResourcePermissions
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
