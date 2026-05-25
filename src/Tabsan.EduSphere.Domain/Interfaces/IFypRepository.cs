using Tabsan.EduSphere.Domain.Fyp;

namespace Tabsan.EduSphere.Domain.Interfaces;

public sealed record FypStudentEligibility(
    Guid StudentProfileId,
    Guid DepartmentId,
    bool IsUniversity,
    int CurrentSemesterNumber,
    int TotalSemesters,
    Guid? TenantId,
    Guid? CampusId);

/// <summary>
/// Repository contract for FYP project, panel, and meeting management.
/// </summary>
public interface IFypRepository
{
    // ── Projects ──────────────────────────────────────────────────────────────

    /// <summary>Returns an FYP project by its ID, or null if not found.</summary>
    Task<FypProject?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a project with its panel members and meetings loaded.</summary>
    Task<FypProject?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all projects for a student.</summary>
    Task<IReadOnlyList<FypProject>> GetByStudentAsync(Guid studentProfileId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns all projects in a department, optionally filtered by status.</summary>
    Task<IReadOnlyList<FypProject>> GetByDepartmentAsync(Guid departmentId, FypProjectStatus? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns all projects across all departments, optionally filtered by status.</summary>
    Task<IReadOnlyList<FypProject>> GetAllAsync(FypProjectStatus? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns all projects supervised by a specific faculty user.</summary>
    Task<IReadOnlyList<FypProject>> GetBySupervisorAsync(Guid supervisorUserId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns eligibility and scope details for a student FYP workflow, or null if student does not exist.</summary>
    Task<FypStudentEligibility?> GetStudentEligibilityAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns tenant/campus scope for a project, or null when project does not exist.</summary>
    Task<(Guid? TenantId, Guid? CampusId)?> GetProjectScopeAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Returns tenant/campus scope for a department, or null when department does not exist.</summary>
    Task<(Guid? TenantId, Guid? CampusId)?> GetDepartmentScopeAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Returns tenant/campus scope for a student profile, or null when student does not exist.</summary>
    Task<(Guid? TenantId, Guid? CampusId)?> GetStudentScopeAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Queues a new project for insertion.</summary>
    Task AddAsync(FypProject project, CancellationToken ct = default);

    /// <summary>Marks a project entity as modified.</summary>
    void Update(FypProject project);

    // ── Panel Members ─────────────────────────────────────────────────────────

    /// <summary>Returns panel members for a given project.</summary>
    Task<IReadOnlyList<FypPanelMember>> GetPanelMembersAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Returns a panel member by project and user IDs, or null.</summary>
    Task<FypPanelMember?> GetPanelMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);

    /// <summary>Queues a panel member for insertion.</summary>
    Task AddPanelMemberAsync(FypPanelMember member, CancellationToken ct = default);

    /// <summary>Removes a panel member from the context.</summary>
    void RemovePanelMember(FypPanelMember member);

    // ── Meetings ──────────────────────────────────────────────────────────────

    /// <summary>Returns a meeting by its ID, or null.</summary>
    Task<FypMeeting?> GetMeetingByIdAsync(Guid meetingId, CancellationToken ct = default);

    /// <summary>Returns all meetings for a project, ordered by scheduled time.</summary>
    Task<IReadOnlyList<FypMeeting>> GetMeetingsByProjectAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Returns upcoming scheduled meetings for a supervisor.</summary>
    Task<IReadOnlyList<FypMeeting>> GetUpcomingMeetingsAsync(Guid supervisorUserId, CancellationToken ct = default);

    /// <summary>Queues a new meeting for insertion.</summary>
    Task AddMeetingAsync(FypMeeting meeting, CancellationToken ct = default);

    /// <summary>Marks a meeting entity as modified.</summary>
    void UpdateMeeting(FypMeeting meeting);

    /// <summary>Commits all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
