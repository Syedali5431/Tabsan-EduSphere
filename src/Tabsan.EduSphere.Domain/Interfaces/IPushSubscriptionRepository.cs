using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Plan M4.3: push subscriptions and per-type notification preferences.</summary>
public interface IPushSubscriptionRepository
{
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct = default);
    Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Active subscriptions of the given users, leaving out users who switched push off for <paramref name="type"/>.</summary>
    Task<IReadOnlyList<PushSubscription>> GetDeliverableAsync(IReadOnlyCollection<Guid> userIds, NotificationType type, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(Guid userId, CancellationToken ct = default);

    void Add<T>(T entity) where T : BaseEntity;
    void Remove<T>(T entity) where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
