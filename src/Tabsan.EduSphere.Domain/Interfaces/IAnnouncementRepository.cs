using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Final-Touches Phase 20 Stage 20.4 — repository contract for course announcements

/// <summary>Repository for course and department-wide announcements.</summary>
public interface IAnnouncementRepository
{
    /// <summary>Returns announcements for a specific offering, newest first.</summary>
    Task<IReadOnlyList<CourseAnnouncement>> GetByOfferingAsync(
        Guid offeringId,
        bool includeInactive = false,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns a single announcement by id.</summary>
    Task<CourseAnnouncement?> GetByIdAsync(
        Guid id,
        bool includeInactive = false,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    Task AddAsync(CourseAnnouncement announcement, CancellationToken ct = default);
    void Update(CourseAnnouncement announcement);
    void Delete(CourseAnnouncement announcement);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
