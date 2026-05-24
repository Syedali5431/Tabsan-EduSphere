using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

// Final-Touches Phase 20 Stage 20.4 — announcement repository

/// <summary>EF Core implementation of IAnnouncementRepository.</summary>
public sealed class AnnouncementRepository : IAnnouncementRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IAccessScopeResolver? _accessScope;

    public AnnouncementRepository(ApplicationDbContext db, IAccessScopeResolver? accessScope = null)
    {
        _db = db;
        _accessScope = accessScope;
    }

    private IQueryable<CourseAnnouncement> BaseQuery(bool includeInactive)
    {
        var query = includeInactive
            ? _db.CourseAnnouncements.IgnoreQueryFilters().AsQueryable()
            : _db.CourseAnnouncements.AsQueryable();

        if (_accessScope?.IsSuperAdmin() == true)
            return query;

        var tenantId = _accessScope?.GetTenantId();
        var campusId = _accessScope?.GetCampusId();

        if (!tenantId.HasValue)
            return query.Where(_ => false);

        return query.Where(a => a.OfferingId.HasValue && _db.CourseOfferings
            .Any(o => o.Id == a.OfferingId.Value
                && o.TenantId == tenantId.Value
                && (!campusId.HasValue || o.CampusId == campusId.Value)));
    }

    public async Task<IReadOnlyList<CourseAnnouncement>> GetByOfferingAsync(
        Guid offeringId,
        bool includeInactive = false,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
        => await BaseQuery(includeInactive)
                    .Where(a => a.OfferingId == offeringId)
                    .Where(a => !tenantId.HasValue || _db.CourseOfferings
                        .Any(o => o.Id == offeringId && o.TenantId == tenantId.Value))
                    .Where(a => !campusId.HasValue || _db.CourseOfferings
                        .Any(o => o.Id == offeringId && o.CampusId == campusId.Value))
                    .OrderByDescending(a => a.PostedAt)
                    .ToListAsync(ct);

    public async Task<CourseAnnouncement?> GetByIdAsync(
        Guid id,
        bool includeInactive = false,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
        => await BaseQuery(includeInactive)
            .Where(a => a.Id == id)
            .Where(a => !tenantId.HasValue || (a.OfferingId.HasValue && _db.CourseOfferings
                .Any(o => o.Id == a.OfferingId.Value && o.TenantId == tenantId.Value)))
            .Where(a => !campusId.HasValue || (a.OfferingId.HasValue && _db.CourseOfferings
                .Any(o => o.Id == a.OfferingId.Value && o.CampusId == campusId.Value)))
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(CourseAnnouncement announcement, CancellationToken ct = default)
        => await _db.CourseAnnouncements.AddAsync(announcement, ct);

    public void Update(CourseAnnouncement announcement)
        => _db.CourseAnnouncements.Update(announcement);

    public void Delete(CourseAnnouncement announcement)
        => _db.CourseAnnouncements.Remove(announcement);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
