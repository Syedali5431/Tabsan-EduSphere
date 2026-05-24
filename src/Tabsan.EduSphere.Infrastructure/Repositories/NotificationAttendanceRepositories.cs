using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>EF Core implementation of INotificationRepository.</summary>
public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _db;
    public NotificationRepository(ApplicationDbContext db) => _db = db;

    // ── Notifications ─────────────────────────────────────────────────────────

    /// <summary>Returns the notification by ID, or null.</summary>
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    /// <summary>Queues a notification for insertion.</summary>
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await _db.Notifications.AddAsync(notification, ct);

    /// <summary>Marks the notification as modified.</summary>
    public void Update(Notification notification) => _db.Notifications.Update(notification);

    // ── Recipients ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns paged notifications for a user, newest first.
    /// Only returns records whose parent notification is still active.
    /// </summary>
    public async Task<IReadOnlyList<NotificationRecipient>> GetForUserAsync(
        Guid userId, bool unreadOnly = false, int skip = 0, int take = 20, bool asNoTracking = false, CancellationToken ct = default)
    {
        IQueryable<NotificationRecipient> query = _db.NotificationRecipients
            .Include(r => r.Notification)
            .Where(r => r.RecipientUserId == userId && r.Notification.IsActive);

        if (asNoTracking)
            query = query.AsNoTracking();

        if (unreadOnly)
            query = query.Where(r => !r.IsRead);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    /// <summary>Returns the count of unread, active notifications for a user.</summary>
    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => _db.NotificationRecipients
              .CountAsync(r => r.RecipientUserId == userId && !r.IsRead && r.Notification.IsActive, ct);

    /// <summary>Returns the delivery record for a specific user+notification pair, or null.</summary>
    public Task<NotificationRecipient?> GetRecipientAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
        => _db.NotificationRecipients.FirstOrDefaultAsync(
            r => r.NotificationId == notificationId && r.RecipientUserId == userId, ct);

    /// <summary>Returns active recipient email addresses for the provided user IDs.</summary>
    public async Task<IReadOnlyList<string>> GetActiveUserEmailsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return Array.Empty<string>();

        var normalizedIds = userIds.Distinct().ToList();
        return await _db.Users
            .AsNoTracking()
            .Where(u => normalizedIds.Contains(u.Id) && u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => u.Email!)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>Returns active recipient phone numbers for the provided user IDs (Phase 32.3).</summary>
    public async Task<IReadOnlyList<string>> GetActiveUserPhoneNumbersAsync(IReadOnlyList<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return Array.Empty<string>();

        var normalizedIds = userIds.Distinct().ToList();
        return await _db.Users
            .AsNoTracking()
            .Where(u => normalizedIds.Contains(u.Id) && u.IsActive && !string.IsNullOrWhiteSpace(u.PhoneNumber))
            .Select(u => u.PhoneNumber!)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>Queues multiple recipient rows for bulk insertion (fan-out on dispatch).</summary>
    public async Task AddRecipientsAsync(IEnumerable<NotificationRecipient> recipients, CancellationToken ct = default)
        => await _db.NotificationRecipients.AddRangeAsync(recipients, ct);

    /// <summary>Marks a recipient row as modified.</summary>
    public void UpdateRecipient(NotificationRecipient recipient)
        => _db.NotificationRecipients.Update(recipient);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>EF Core implementation of IAttendanceRepository.</summary>
public class AttendanceRepository : IAttendanceRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IAccessScopeResolver? _accessScope;

    public AttendanceRepository(ApplicationDbContext db, IAccessScopeResolver? accessScope = null)
    {
        _db = db;
        _accessScope = accessScope;
    }

    private IQueryable<AttendanceRecord> ApplyTenantCampusScope(
        IQueryable<AttendanceRecord> query,
        Guid? tenantId,
        Guid? campusId)
    {
        var effectiveTenantId = tenantId ?? _accessScope?.GetTenantId();
        var effectiveCampusId = campusId ?? _accessScope?.GetCampusId();
        var hasExplicitScope = tenantId.HasValue || campusId.HasValue;

        if (!effectiveTenantId.HasValue)
        {
            if (_accessScope?.IsSuperAdmin() == true && !hasExplicitScope)
                return query;

            return query.Where(_ => false);
        }

        var scopedOfferingIds = _db.CourseOfferings
            .AsNoTracking()
            .Where(o => o.TenantId == effectiveTenantId.Value && (!effectiveCampusId.HasValue || o.CampusId == effectiveCampusId.Value))
            .Select(o => o.Id);

        return query.Where(a => scopedOfferingIds.Contains(a.CourseOfferingId));
    }

    /// <summary>Returns the attendance record for a student / offering / date, or null.</summary>
    public Task<AttendanceRecord?> GetAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        DateTime date,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
        => ApplyTenantCampusScope(_db.AttendanceRecords, tenantId, campusId).FirstOrDefaultAsync(a =>
            a.StudentProfileId == studentProfileId &&
            a.CourseOfferingId == courseOfferingId &&
            a.Date == date.Date, ct);

    /// <summary>Returns true when a record already exists for the combination.</summary>
    public Task<bool> ExistsAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        DateTime date,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
        => ApplyTenantCampusScope(_db.AttendanceRecords, tenantId, campusId).AnyAsync(a =>
            a.StudentProfileId == studentProfileId &&
            a.CourseOfferingId == courseOfferingId &&
            a.Date == date.Date, ct);

    /// <summary>Returns all records for a course offering, optionally filtered by date range.</summary>
    public async Task<IReadOnlyList<AttendanceRecord>> GetByOfferingAsync(
        Guid courseOfferingId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var query = ApplyTenantCampusScope(_db.AttendanceRecords, tenantId, campusId)
            .Where(a => a.CourseOfferingId == courseOfferingId);

        if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
        if (to.HasValue)   query = query.Where(a => a.Date <= to.Value.Date);

        return await query.OrderBy(a => a.Date).ThenBy(a => a.StudentProfileId).ToListAsync(ct);
    }

    /// <summary>Returns all records for a student, optionally filtered to a single offering.</summary>
    public async Task<IReadOnlyList<AttendanceRecord>> GetByStudentAsync(
        Guid studentProfileId,
        Guid? courseOfferingId = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var query = ApplyTenantCampusScope(_db.AttendanceRecords, tenantId, campusId)
            .Where(a => a.StudentProfileId == studentProfileId);

        if (courseOfferingId.HasValue)
            query = query.Where(a => a.CourseOfferingId == courseOfferingId.Value);

        return await query.OrderBy(a => a.CourseOfferingId).ThenBy(a => a.Date).ToListAsync(ct);
    }

    /// <summary>
    /// Returns (TotalSessions, AttendedSessions) for a student in an offering.
    /// Attended = Present or Late.
    /// </summary>
    public async Task<(int TotalSessions, int AttendedSessions)> GetAttendanceSummaryAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var records = await ApplyTenantCampusScope(_db.AttendanceRecords, tenantId, campusId)
            .Where(a => a.StudentProfileId == studentProfileId && a.CourseOfferingId == courseOfferingId)
            .Select(a => a.Status)
            .ToListAsync(ct);

        int total    = records.Count;
        int attended = records.Count(s => s == AttendanceStatus.Present || s == AttendanceStatus.Late);
        return (total, attended);
    }

    /// <summary>
    /// Returns all (studentProfileId, courseOfferingId, attendancePercent) tuples
    /// where the student's attendance is below <paramref name="thresholdPercent"/>.
    /// Only students with at least one recorded session are considered.
    /// </summary>
    public async Task<IReadOnlyList<(Guid StudentProfileId, Guid CourseOfferingId, double AttendancePercent)>> GetBelowThresholdAsync(
        double thresholdPercent,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var grouped = await ApplyTenantCampusScope(_db.AttendanceRecords, tenantId, campusId)
            .GroupBy(a => new { a.StudentProfileId, a.CourseOfferingId })
            .Select(g => new
            {
                g.Key.StudentProfileId,
                g.Key.CourseOfferingId,
                Total    = g.Count(),
                Attended = g.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)
            })
            .ToListAsync(ct);

        return grouped
            .Select(g => (
                g.StudentProfileId,
                g.CourseOfferingId,
                AttendancePercent: g.Total > 0 ? (double)g.Attended / g.Total * 100.0 : 0.0))
            .Where(x => x.AttendancePercent < thresholdPercent)
            .ToList();
    }

    /// <summary>Queues a single attendance record for insertion.</summary>
    public async Task AddAsync(AttendanceRecord record, CancellationToken ct = default)
        => await _db.AttendanceRecords.AddAsync(record, ct);

    /// <summary>Queues multiple records for bulk insertion (full-class session).</summary>
    public async Task AddRangeAsync(IEnumerable<AttendanceRecord> records, CancellationToken ct = default)
        => await _db.AttendanceRecords.AddRangeAsync(records, ct);

    /// <summary>Marks a record as modified (correction).</summary>
    public void Update(AttendanceRecord record) => _db.AttendanceRecords.Update(record);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
