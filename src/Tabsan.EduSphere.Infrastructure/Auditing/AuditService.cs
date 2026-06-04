using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Auditing;

/// <summary>
/// Writes audit log entries to the database.
/// The AuditLog table is append-only — entries are never updated or deleted.
/// Uses a separate DbContext scope so audit writes do not interfere with
/// the main request's unit of work.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Appends a new audit log entry to the database.
    /// Runs asynchronously and does not block the caller's response pipeline.
    /// </summary>
    public async Task LogAsync(AuditLog entry, CancellationToken ct = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var actorUserId = entry.ActorUserId ?? TryResolveActorUserId(httpContext?.User);
        var actorRole = string.IsNullOrWhiteSpace(entry.ActorRole)
            ? TryResolveActorRole(httpContext?.User)
            : entry.ActorRole;
        var ipAddress = string.IsNullOrWhiteSpace(entry.IpAddress)
            ? TryResolveIpAddress(httpContext)
            : entry.IpAddress;
                var userAgent = string.IsNullOrWhiteSpace(entry.UserAgent)
            ? TryResolveUserAgent(httpContext)
            : entry.UserAgent;
        var deviceInfo = string.IsNullOrWhiteSpace(entry.DeviceInfo)
            ? TryResolveDeviceInfo(httpContext)
            : entry.DeviceInfo;

        var enriched = new AuditLog(
            action: entry.Action,
            entityName: entry.EntityName,
            entityId: entry.EntityId,
            actorUserId: actorUserId,
            oldValuesJson: entry.OldValuesJson,
            newValuesJson: entry.NewValuesJson,
            ipAddress: ipAddress,
            actorRole: actorRole,
            userAgent: userAgent,
            deviceInfo: deviceInfo);

        await _db.AuditLogs.AddAsync(enriched, ct);
        await _db.SaveChangesAsync(ct);
    }

    private static Guid? TryResolveActorUserId(ClaimsPrincipal? user)
    {
        var raw = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub")
            ?? user?.FindFirstValue("userId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? TryResolveActorRole(ClaimsPrincipal? user)
    {
        return user?.FindFirst(ClaimTypes.Role)?.Value
            ?? user?.FindFirst("role")?.Value
            ?? user?.Claims.FirstOrDefault(c => c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private static string? TryResolveIpAddress(HttpContext? context)
    {
        if (context is null)
            return null;

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

        private static string? TryResolveUserAgent(HttpContext? context)
        => context?.Request.Headers["User-Agent"].ToString();

    private static string? TryResolveDeviceInfo(HttpContext? context)
    {
        if (context is null) return null;
        
        // Construct a concise device-info string from available HTTP headers.
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var platform = context.Request.Headers["Sec-CH-UA-Platform"].ToString();
        var mobile = context.Request.Headers["Sec-CH-UA-Mobile"].ToString();
        
        if (!string.IsNullOrWhiteSpace(platform))
            return string.IsNullOrWhiteSpace(mobile) ? $"{platform}; {userAgent}" : $"{platform} (Mobile: {mobile}); {userAgent}";
        
        return userAgent;
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        string? query = null,
        Guid? actorUserId = null,
        string? action = null,
        string? entityName = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<AuditLog> auditQuery = _db.AuditLogs.AsNoTracking();

        if (actorUserId.HasValue)
            auditQuery = auditQuery.Where(x => x.ActorUserId == actorUserId.Value);

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionFilter = action.Trim();
            auditQuery = auditQuery.Where(x => x.Action == actionFilter);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var entityFilter = entityName.Trim();
            auditQuery = auditQuery.Where(x => x.EntityName == entityFilter);
        }

        if (fromUtc.HasValue)
            auditQuery = auditQuery.Where(x => x.OccurredAt >= fromUtc.Value);

        if (toUtc.HasValue)
            auditQuery = auditQuery.Where(x => x.OccurredAt <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            var like = $"%{q}%";
            auditQuery = auditQuery.Where(x =>
                EF.Functions.Like(x.Action, like)
                || EF.Functions.Like(x.EntityName, like)
                || (x.EntityId != null && EF.Functions.Like(x.EntityId, like))
                || (x.IpAddress != null && EF.Functions.Like(x.IpAddress, like)));
        }

        var totalCount = await auditQuery.CountAsync(ct);
        var items = await auditQuery
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
