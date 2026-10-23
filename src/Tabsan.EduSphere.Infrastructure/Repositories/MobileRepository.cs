using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Mobile;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M12 — mobile devices and crash reports.</summary>
public sealed class MobileRepository : IMobileRepository
{
    private readonly ApplicationDbContext _db;

    public MobileRepository(ApplicationDbContext db) => _db = db;

    public Task<MobileDevice?> GetDeviceByTokenAsync(string token, CancellationToken ct = default)
        => _db.MobileDevices.FirstOrDefaultAsync(d => d.Token == token, ct);

    public async Task<IReadOnlyList<MobileDevice>> GetDevicesAsync(Guid userId, CancellationToken ct = default)
        => await _db.MobileDevices.Where(d => d.UserId == userId).OrderByDescending(d => d.LastSeenAt).ToListAsync(ct);

    public async Task<IReadOnlyList<MobileDevice>> GetDeliverableAsync(IReadOnlyCollection<Guid> userIds, NotificationType type, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return [];
        var results = new List<MobileDevice>();
        // The same per-type choices as browser push (Notification settings); chunked so the IN list stays small.
        foreach (var chunk in userIds.Distinct().Chunk(500))
        {
            var optedOut = _db.NotificationPreferences.Where(p => p.Type == type && !p.PushEnabled).Select(p => p.UserId);
            results.AddRange(await _db.MobileDevices
                .Where(d => d.IsActive && chunk.Contains(d.UserId) && !optedOut.Contains(d.UserId))
                .ToListAsync(ct));
        }
        return results;
    }

    public void Add(MobileDevice device) => _db.MobileDevices.Add(device);

    public void Remove(MobileDevice device) => _db.MobileDevices.Remove(device);

    public void Add(MobileCrashReport report) => _db.MobileCrashReports.Add(report);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
