using Tabsan.EduSphere.Application.DTOs.Fyp;
using Tabsan.EduSphere.Domain.Fyp;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Application service contract for FYP project proposals, panel management, and meeting scheduling.
/// </summary>
public interface IFypService
{
    // ── Projects ──────────────────────────────────────────────────────────────

    /// <summary>Submits a new FYP project proposal for the given student. Returns the project ID.</summary>
    Task<Guid> ProposeAsync(ProposeProjectRequest request, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Creates a new FYP project directly for a student under admin control. Returns the project ID.</summary>
    Task<Guid> CreateForStudentAsync(CreateProjectForStudentRequest request, CancellationToken ct = default);

    /// <summary>Updates a project's title and description. Returns false if not found.</summary>
    Task<bool> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default);

    /// <summary>Approves a project proposal. Returns false if not found.</summary>
    Task<bool> ApproveAsync(Guid projectId, ApproveProjectRequest request, CancellationToken ct = default);

    /// <summary>Rejects a project proposal. Returns false if not found.</summary>
    Task<bool> RejectAsync(Guid projectId, RejectProjectRequest request, CancellationToken ct = default);

    /// <summary>Assigns a supervisor and moves the project to InProgress. Returns false if not found.</summary>
    Task<bool> AssignSupervisorAsync(Guid projectId, AssignSupervisorRequest request, CancellationToken ct = default);

    /// <summary>Marks a project as completed. Returns false if not found.</summary>
    Task<bool> CompleteAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Student requests completion review for an in-progress project. Returns false if not found.</summary>
    Task<bool> RequestCompletionAsync(Guid projectId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Assigned faculty approves completion review for a project. Returns null if project is not found.</summary>
    Task<ApproveCompletionResponse?> ApproveCompletionAsync(Guid projectId, Guid facultyUserId, CancellationToken ct = default);

    /// <summary>Enters or updates the final result for a completed project. Returns false if not found.</summary>
    Task<bool> EnterResultAsync(Guid projectId, EnterFypResultRequest request, CancellationToken ct = default);

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all FYP projects for a student.</summary>
    Task<IReadOnlyList<FypProjectSummaryResponse>> GetByStudentAsync(Guid studentProfileId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns projects in a department, optionally filtered by status string.</summary>
    Task<IReadOnlyList<FypProjectSummaryResponse>> GetByDepartmentAsync(Guid departmentId, string? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns all projects across all departments, optionally filtered by status string.</summary>
    Task<IReadOnlyList<FypProjectSummaryResponse>> GetAllAsync(string? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns projects supervised by a faculty user.</summary>
    Task<IReadOnlyList<FypProjectSummaryResponse>> GetBySupervisorAsync(Guid supervisorUserId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns full project detail with panel members and meeting history. Returns null if not found.</summary>
    Task<FypProjectDetailResponse?> GetDetailAsync(Guid projectId, CancellationToken ct = default);

    // ── Panel members ─────────────────────────────────────────────────────────

    /// <summary>Adds a faculty user to a project panel. Returns false if the project is not found.</summary>
    Task<bool> AddPanelMemberAsync(Guid projectId, AddPanelMemberRequest request, CancellationToken ct = default);

    /// <summary>Removes a panel member from a project. Returns false if not found.</summary>
    Task<bool> RemovePanelMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);

    // ── Meetings ──────────────────────────────────────────────────────────────

    /// <summary>Schedules a meeting for an FYP project. Returns the meeting ID.</summary>
    Task<Guid> ScheduleMeetingAsync(ScheduleMeetingRequest request, Guid organiserUserId, CancellationToken ct = default);

    /// <summary>Reschedules an existing meeting. Returns false if not found.</summary>
    Task<bool> RescheduleMeetingAsync(Guid meetingId, RescheduleMeetingRequest request, CancellationToken ct = default);

    /// <summary>Marks a meeting as completed with optional minutes. Returns false if not found.</summary>
    Task<bool> CompleteMeetingAsync(Guid meetingId, CompleteMeetingRequest request, CancellationToken ct = default);

    /// <summary>Cancels a meeting. Returns false if not found.</summary>
    Task<bool> CancelMeetingAsync(Guid meetingId, CancellationToken ct = default);

    /// <summary>Returns all meetings for a project.</summary>
    Task<IReadOnlyList<MeetingResponse>> GetMeetingsByProjectAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Returns upcoming meetings for a supervisor.</summary>
    Task<IReadOnlyList<MeetingResponse>> GetUpcomingMeetingsAsync(Guid supervisorUserId, CancellationToken ct = default);
}
