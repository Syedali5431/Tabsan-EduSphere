using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Permissions;

namespace Tabsan.EduSphere.Application.Authorization;

/// <summary>
/// Provides fast in-memory permission lookups for the RBAC system.
/// Caches all permissions on first access and refreshes on-demand.
/// SuperAdmin always returns true for every permission check.
/// </summary>
public interface IPermissionService
{
    /// <summary>Returns true if the given role has View permission on the resource.</summary>
    bool CanView(string role, string resourceKey);

    /// <summary>Returns true if the given role has Add/Create permission on the resource.</summary>
    bool CanAdd(string role, string resourceKey);

    /// <summary>Returns true if the given role has Edit/Update permission on the resource.</summary>
    bool CanEdit(string role, string resourceKey);

    /// <summary>Returns true if the given role has Deactivate/Delete permission on the resource.</summary>
    bool CanDeactivate(string role, string resourceKey);

    /// <summary>Returns true if the given role has Export permission on the resource.</summary>
    bool CanExport(string role, string resourceKey);

    /// <summary>Returns true if the given role has Import permission on the resource.</summary>
    bool CanImport(string role, string resourceKey);

    /// <summary>Returns true if the role is SuperAdmin (always bypasses permission checks).</summary>
    bool IsSuperAdmin(string role);

    /// <summary>
    /// Returns the full permission flags for a role on a resource.
    /// Returns all-false if no record exists.
    /// </summary>
    PermissionFlags GetPermissions(string role, string resourceKey);

    /// <summary>Reloads the permission cache from the database.</summary>
    Task RefreshAsync(CancellationToken ct = default);
}

/// <summary>Lightweight DTO for permission flags returned to the frontend.</summary>
public sealed record PermissionFlags(
    bool CanView,
    bool CanAdd,
    bool CanEdit,
    bool CanDeactivate,
    bool CanExport,
    bool CanImport
)
{
    public static readonly PermissionFlags None = new(false, false, false, false, false, false);
    public static readonly PermissionFlags All = new(true, true, true, true, true, true);
}

public class PermissionService : IPermissionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PermissionService> _logger;

    // Key: $"{RoleName}::{ResourceKey}" — stored lowercase for case-insensitive lookup
    private ConcurrentDictionary<string, PermissionFlags> _cache = new();

    private bool _isLoaded;

    private static string MakeKey(string role, string resource) =>
        $"{role.ToUpperInvariant()}::{resource.ToUpperInvariant()}";

    public PermissionService(IServiceProvider serviceProvider, ILogger<PermissionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // ── Public API ──────────────────────────────────────────────────────

    public bool IsSuperAdmin(string role) =>
        string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    public bool CanView(string role, string resourceKey)
    {
        if (IsSuperAdmin(role)) return true;
        EnsureLoaded();
        return _cache.TryGetValue(MakeKey(role, resourceKey), out var flags) && flags.CanView;
    }

    public bool CanAdd(string role, string resourceKey)
    {
        if (IsSuperAdmin(role)) return true;
        EnsureLoaded();
        return _cache.TryGetValue(MakeKey(role, resourceKey), out var flags) && flags.CanAdd;
    }

    public bool CanEdit(string role, string resourceKey)
    {
        if (IsSuperAdmin(role)) return true;
        EnsureLoaded();
        return _cache.TryGetValue(MakeKey(role, resourceKey), out var flags) && flags.CanEdit;
    }

    public bool CanDeactivate(string role, string resourceKey)
    {
        if (IsSuperAdmin(role)) return true;
        EnsureLoaded();
        return _cache.TryGetValue(MakeKey(role, resourceKey), out var flags) && flags.CanDeactivate;
    }

    public bool CanExport(string role, string resourceKey)
    {
        if (IsSuperAdmin(role)) return true;
        EnsureLoaded();
        return _cache.TryGetValue(MakeKey(role, resourceKey), out var flags) && flags.CanExport;
    }

    public bool CanImport(string role, string resourceKey)
    {
        if (IsSuperAdmin(role)) return true;
        EnsureLoaded();
        return _cache.TryGetValue(MakeKey(role, resourceKey), out var flags) && flags.CanImport;
    }

    public PermissionFlags GetPermissions(string role, string resourceKey)
    {
        if (IsSuperAdmin(role)) return PermissionFlags.All;
        EnsureLoaded();
        return _cache.TryGetValue(MakeKey(role, resourceKey), out var flags) ? flags : PermissionFlags.None;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await LoadFromDatabaseAsync(ct);
    }

    // ── Internal ────────────────────────────────────────────────────────

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        // Fire-and-forget on first access — synchronous fallback uses empty cache
        _ = Task.Run(async () =>
        {
            try { await LoadFromDatabaseAsync(CancellationToken.None); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to load permission cache"); }
        });

        // Block briefly for first load (avoids returning wrong results on startup)
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRoleResourcePermissionRepository>();
            var all = repo.GetAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            PopulateCache(all);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Synchronous permission cache load failed — will retry on next request");
        }
    }

    private async Task LoadFromDatabaseAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRoleResourcePermissionRepository>();
        var all = await repo.GetAllAsync(ct);
        PopulateCache(all);
        _logger.LogInformation("Permission cache loaded: {Count} records", all.Count);
    }

    private void PopulateCache(IReadOnlyList<RoleResourcePermission> all)
    {
        var newCache = new ConcurrentDictionary<string, PermissionFlags>();

        foreach (var p in all)
        {
            var key = MakeKey(p.RoleName, p.ResourceKey);
            newCache[key] = new PermissionFlags(p.CanView, p.CanAdd, p.CanEdit, p.CanDeactivate, p.CanExport, p.CanImport);
        }

        Interlocked.Exchange(ref _cache, newCache);
        _isLoaded = true;
    }
}
