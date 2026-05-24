using Tabsan.EduSphere.Application.DTOs.Lms;

namespace Tabsan.EduSphere.Application.Interfaces;

// Final-Touches Phase 20 Stage 20.4 — announcement service contract

/// <summary>Service for managing course announcements and dispatching notifications.</summary>
public interface IAnnouncementService
{
    Task<List<CourseAnnouncementDto>> GetByOfferingAsync(
        Guid offeringId,
        bool includeInactive = false,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    Task<CourseAnnouncementDto> CreateAsync(
        CreateAnnouncementRequest request,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    Task SetActiveAsync(
        Guid announcementId,
        bool isActive,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    Task DeleteAsync(
        Guid announcementId,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);
}
