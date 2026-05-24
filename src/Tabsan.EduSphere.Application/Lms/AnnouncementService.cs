using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms;

// Final-Touches Phase 20 Stage 20.4 — announcement service implementation

/// <summary>
/// Application service for Phase 20 — course announcements.
/// Dispatches in-app notifications (NotificationType.Announcement = 6) to enrolled students.
/// </summary>
public sealed class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _repo;
    private readonly IUserRepository         _users;
    private readonly ICourseRepository       _courses;
    private readonly IAccessScopeResolver    _accessScope;
    private readonly IAnnouncementBroadcastProvider _broadcastProvider;

    public AnnouncementService(
        IAnnouncementRepository repo,
        IUserRepository         users,
        ICourseRepository       courses,
        IAccessScopeResolver    accessScope,
        IAnnouncementBroadcastProvider broadcastProvider)
    {
        _repo          = repo;
        _users         = users;
        _courses       = courses;
        _accessScope   = accessScope;
        _broadcastProvider = broadcastProvider;
    }

    public async Task<List<CourseAnnouncementDto>> GetByOfferingAsync(
        Guid offeringId,
        bool includeInactive = false,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var (effectiveTenantId, effectiveCampusId) = ResolveScope(tenantId, campusId);
        var items  = await _repo.GetByOfferingAsync(offeringId, includeInactive, effectiveTenantId, effectiveCampusId, ct);
        var result = new List<CourseAnnouncementDto>(items.Count);
        foreach (var a in items)
        {
            var author = await _users.GetByIdAsync(a.AuthorId, ct);
            result.Add(MapAnnouncement(a, author?.Username ?? "Unknown"));
        }
        return result;
    }

    public async Task<CourseAnnouncementDto> CreateAsync(
        CreateAnnouncementRequest request,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var (effectiveTenantId, effectiveCampusId) = ResolveScope(tenantId, campusId);

        if (!request.OfferingId.HasValue)
            throw new InvalidOperationException("Announcement offering is required.");

        var offering = await _courses.GetOfferingByIdAsync(request.OfferingId.Value, ct)
            ?? throw new InvalidOperationException($"Course offering {request.OfferingId.Value} not found.");

        if (effectiveTenantId.HasValue && offering.TenantId != effectiveTenantId.Value)
            throw new UnauthorizedAccessException("You are not authorized to post announcements for this tenant.");

        if (effectiveCampusId.HasValue && offering.CampusId != effectiveCampusId.Value)
            throw new UnauthorizedAccessException("You are not authorized to post announcements for this campus.");

        var announcement = new CourseAnnouncement(
            request.OfferingId, request.AuthorId, request.Title, request.Body);

        await _repo.AddAsync(announcement, ct);
        await _repo.SaveChangesAsync(ct);

        await _broadcastProvider.BroadcastAsync(request.OfferingId, request.Title, request.Body, ct);

        var author = await _users.GetByIdAsync(announcement.AuthorId, ct);
        return MapAnnouncement(announcement, author?.Username ?? "Unknown");
    }

    public async Task SetActiveAsync(
        Guid announcementId,
        bool isActive,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var (effectiveTenantId, effectiveCampusId) = ResolveScope(tenantId, campusId);

        var announcement = await _repo.GetByIdAsync(announcementId, includeInactive: true, effectiveTenantId, effectiveCampusId, ct)
            ?? throw new InvalidOperationException($"Announcement {announcementId} not found.");

        if (isActive)
            announcement.Activate();
        else
            announcement.Deactivate();

        _repo.Update(announcement);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(
        Guid announcementId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var (effectiveTenantId, effectiveCampusId) = ResolveScope(tenantId, campusId);

        var a = await _repo.GetByIdAsync(announcementId, includeInactive: true, effectiveTenantId, effectiveCampusId, ct)
            ?? throw new InvalidOperationException($"Announcement {announcementId} not found.");
        a.Deactivate();
        _repo.Update(a);
        await _repo.SaveChangesAsync(ct);
    }

    private (Guid? TenantId, Guid? CampusId) ResolveScope(Guid? requestedTenantId, Guid? requestedCampusId)
    {
        if (_accessScope.IsSuperAdmin())
            return (requestedTenantId, requestedCampusId);

        var tenantId = _accessScope.GetTenantId();
        var campusId = _accessScope.GetCampusId();

        if (!tenantId.HasValue)
            throw new UnauthorizedAccessException("A valid tenant scope is required.");

        return (tenantId, campusId);
    }

    // ── Mapper ─────────────────────────────────────────────────────────────────

    private static CourseAnnouncementDto MapAnnouncement(CourseAnnouncement a, string authorName) => new()
    {
        Id         = a.Id,
        OfferingId = a.OfferingId,
        AuthorId   = a.AuthorId,
        AuthorName = authorName,
        Title      = a.Title,
        Body       = a.Body,
        IsActive   = !a.IsDeleted,
        PostedAt   = a.PostedAt
    };
}
