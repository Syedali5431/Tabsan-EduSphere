using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// All database access for the User aggregate goes through this class.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IAccessScopeResolver? _accessScope;

    public UserRepository(ApplicationDbContext db, IAccessScopeResolver? accessScope = null)
    {
        _db = db;
        _accessScope = accessScope;
    }

    private IQueryable<User> ApplyTenantCampusScope(IQueryable<User> query)
    {
        if (_accessScope?.IsSuperAdmin() == true)
            return query;

        var tenantId = _accessScope?.GetTenantId();
        var campusId = _accessScope?.GetCampusId();

        if (tenantId.HasValue && campusId.HasValue)
            return query.Where(u => u.TenantId == tenantId && u.CampusId == campusId);

        if (tenantId.HasValue)
            return query.Where(u => u.TenantId == tenantId);

        return query;
    }

    /// <summary>Finds a user by their GUID primary key. Returns null if not found.</summary>
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => ApplyTenantCampusScope(_db.Users)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>
    /// Finds a user by username using a case-insensitive comparison.
    /// Used during login to locate the account before password verification.
    /// </summary>
    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => ApplyTenantCampusScope(_db.Users)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), ct);

    /// <summary>Finds a user by email address. Returns null when no match exists.</summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => ApplyTenantCampusScope(_db.Users)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower(), ct);

    /// <summary>Returns true when the username string is already taken by another account.</summary>
    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
        => ApplyTenantCampusScope(_db.Users)
            .AnyAsync(u => u.Username.ToLower() == username.ToLower(), ct);

    /// <summary>
    /// Returns all non-admin accounts that are currently locked out.
    /// Excludes Admin and SuperAdmin roles (those cannot be locked via automated policy).
    /// </summary>
    public async Task<IList<User>> GetLockedAccountsAsync(CancellationToken ct = default)
        => await ApplyTenantCampusScope(_db.Users)
            .Include(u => u.Role)
            .Where(u => u.IsLockedOut && u.Role.Name != "Admin" && u.Role.Name != "SuperAdmin")
            .ToListAsync(ct);

    /// <summary>Returns all active users in Faculty role. Used for timetable teacher dropdowns.</summary>
    public async Task<IList<User>> GetFacultyUsersAsync(CancellationToken ct = default)
        => await ApplyTenantCampusScope(_db.Users)
            .Include(u => u.Role)
            .Where(u => u.IsActive && u.Role.Name == "Faculty")
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

    /// <summary>Returns active users that belong to any role in <paramref name="roleNames"/>.</summary>
    public async Task<IList<User>> GetActiveUsersByRolesAsync(IReadOnlyList<string> roleNames, CancellationToken ct = default)
    {
        return await GetUsersByRolesAsync(roleNames, includeInactive: false, ct);
    }

    /// <summary>Returns users in the provided roles, optionally including inactive accounts.</summary>
    public async Task<IList<User>> GetUsersByRolesAsync(IReadOnlyList<string> roleNames, bool includeInactive = false, CancellationToken ct = default)
    {
        if (roleNames.Count == 0)
            return new List<User>();

        var normalized = roleNames
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
            return new List<User>();

        var query = ApplyTenantCampusScope(_db.Users)
            .Include(u => u.Role)
            .Where(u => normalized.Contains(u.Role.Name));

        if (!includeInactive)
            query = query.Where(u => u.IsActive);

        return await query
            .OrderBy(u => u.Username)
            .ToListAsync(ct);
    }

    /// <summary>Queues the new user entity for insertion on the next SaveChanges call.</summary>
    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    /// <summary>Bulk-queues multiple new user entities for insertion (P4-S1-01 CSV import).</summary>
    public async Task AddRangeAsync(IEnumerable<User> users, CancellationToken ct = default)
        => await _db.Users.AddRangeAsync(users, ct);

    /// <summary>Returns the role matching the given name (case-insensitive), or null if not found.</summary>
    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        => _db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower(), ct);

    /// <summary>Marks the user entity as Modified so EF Core generates an UPDATE statement.</summary>
    public void Update(User user) => _db.Users.Update(user);

    /// <summary>Flushes all tracked changes to the database in a single transaction.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
