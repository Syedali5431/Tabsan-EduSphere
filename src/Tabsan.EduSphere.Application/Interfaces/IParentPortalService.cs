using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.DTOs.Attendance;
using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Dtos;

namespace Tabsan.EduSphere.Application.Interfaces;

// Phase 26 — Stage 26.3

public interface IParentPortalService
{
    Task<IReadOnlyList<ParentLinkedStudentDto>> GetLinkedStudentsAsync(Guid parentUserId, CancellationToken ct = default);

    Task<IReadOnlyList<ResultResponse>> GetLinkedStudentResultsAsync(Guid parentUserId, Guid studentProfileId, CancellationToken ct = default);

    Task<IReadOnlyList<AttendanceResponse>> GetLinkedStudentAttendanceAsync(Guid parentUserId, Guid studentProfileId, Guid? courseOfferingId = null, CancellationToken ct = default);

    Task<IReadOnlyList<CourseAnnouncementDto>> GetLinkedStudentAnnouncementsAsync(Guid parentUserId, Guid studentProfileId, Guid? courseOfferingId = null, CancellationToken ct = default);

    Task<TimetableDto?> GetLinkedStudentTimetableAsync(Guid parentUserId, Guid studentProfileId, Guid? timetableId = null, CancellationToken ct = default);

    Task<IReadOnlyList<ParentStudentLinkDto>> GetLinksByParentAsync(Guid parentUserId, CancellationToken ct = default);

    Task<ParentStudentLinkDto> UpsertLinkAsync(UpsertParentStudentLinkRequest request, CancellationToken ct = default);

    Task<bool> DeactivateLinkAsync(Guid parentUserId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Removes the link (soft delete, also inactive); false when there is none. Linking again restores it.</summary>
    Task<bool> RemoveLinkAsync(Guid parentUserId, Guid studentProfileId, CancellationToken ct = default);
}
