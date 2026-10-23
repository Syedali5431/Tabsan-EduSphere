using Tabsan.EduSphere.Domain.Mobile;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Plan M12 — devices registered by the mobile app, and its crash reports.</summary>
public interface IMobileRepository
{
    Task<MobileDevice?> GetDeviceByTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<MobileDevice>> GetDevicesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Active devices of the users who have not turned this notification type off.</summary>
    Task<IReadOnlyList<MobileDevice>> GetDeliverableAsync(IReadOnlyCollection<Guid> userIds, NotificationType type, CancellationToken ct = default);

    void Add(MobileDevice device);
    void Remove(MobileDevice device);
    void Add(MobileCrashReport report);
    Task SaveChangesAsync(CancellationToken ct = default);
}
