using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M4.3 — EF implementation of <see cref="IPushSubscriptionRepository"/>.</summary>
public sealed class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly ApplicationDbContext _db;

    public PushSubscriptionRepository(ApplicationDbContext db) => _db = db;

    public Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct = default)
        => _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);

    public async Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<PushSubscription>> GetDeliverableAsync(IReadOnlyCollection<Guid> userIds, NotificationType type, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return [];
        var results = new List<PushSubscription>();
        // Chunk large recipient lists so the IN clause stays small.
        foreach (var chunk in userIds.Distinct().Chunk(500))
        {
            var optedOut = _db.NotificationPreferences.Where(p => p.Type == type && !p.PushEnabled).Select(p => p.UserId);
            results.AddRange(await _db.PushSubscriptions
                .Where(s => s.IsActive && chunk.Contains(s.UserId) && !optedOut.Contains(s.UserId))
                .ToListAsync(ct));
        }
        return results;
    }

    public async Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(Guid userId, CancellationToken ct = default)
        => await _db.NotificationPreferences.Where(p => p.UserId == userId).ToListAsync(ct);

    public void Add<T>(T entity) where T : BaseEntity
    {
        if (_db.Entry(entity).State == EntityState.Detached) _db.Add(entity);
    }

    public void Remove<T>(T entity) where T : BaseEntity => _db.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
