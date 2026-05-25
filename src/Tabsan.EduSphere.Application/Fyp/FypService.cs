using Tabsan.EduSphere.Application.DTOs.Fyp;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Fyp;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Fyp;

/// <summary>
/// Implements <see cref="IFypService"/> — FYP project proposal lifecycle,
/// panel assignments, and supervision meeting management.
/// </summary>
public sealed class FypService : IFypService
{
    private readonly IFypRepository _repo;

    /// <summary>Initialises the service with the FYP repository.</summary>
    public FypService(IFypRepository repo) => _repo = repo;

    // ── Projects ──────────────────────────────────────────────────────────────

    /// <summary>Submits a new FYP project proposal. Returns the project ID.</summary>
    public async Task<Guid> ProposeAsync(ProposeProjectRequest request, Guid studentProfileId, CancellationToken ct = default)
    {
        var eligibility = await EnsureStudentEligibilityAsync(studentProfileId, request.DepartmentId, ct);

        var project = new FypProject(
            studentProfileId,
            eligibility.DepartmentId,
            request.Title,
            request.Description);

        await _repo.AddAsync(project, ct);
        await _repo.SaveChangesAsync(ct);
        return project.Id;
    }

    /// <summary>Creates a new FYP project directly for a student. Returns the project ID.</summary>
    public async Task<Guid> CreateForStudentAsync(CreateProjectForStudentRequest request, CancellationToken ct = default)
    {
        var eligibility = await EnsureStudentEligibilityAsync(request.StudentProfileId, request.DepartmentId, ct);

        var project = new FypProject(
            request.StudentProfileId,
            eligibility.DepartmentId,
            request.Title,
            request.Description);

        await _repo.AddAsync(project, ct);
        await _repo.SaveChangesAsync(ct);
        return project.Id;
    }

    /// <summary>Updates a project's title and description. Returns false if not found.</summary>
    public async Task<bool> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        project.Update(request.Title, request.Description);
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Approves a project proposal. Returns false if not found.</summary>
    public async Task<bool> ApproveAsync(Guid projectId, ApproveProjectRequest request, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        project.Approve(request.Remarks);
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Rejects a project proposal. Returns false if not found.</summary>
    public async Task<bool> RejectAsync(Guid projectId, RejectProjectRequest request, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        project.Reject(request.Remarks);
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Assigns a supervisor and moves the project to InProgress. Returns false if not found.</summary>
    public async Task<bool> AssignSupervisorAsync(Guid projectId, AssignSupervisorRequest request, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        project.AssignSupervisor(request.SupervisorUserId);
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Marks a project as completed. Returns false if not found.</summary>
    public async Task<bool> CompleteAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        project.Complete();
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Student requests completion review for an in-progress project.</summary>
    public async Task<bool> RequestCompletionAsync(Guid projectId, Guid studentProfileId, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        project.RequestCompletion(studentProfileId);
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Assigned faculty approves completion review for a project.</summary>
    public async Task<ApproveCompletionResponse?> ApproveCompletionAsync(Guid projectId, Guid facultyUserId, CancellationToken ct = default)
    {
        var project = await _repo.GetWithDetailsAsync(projectId, ct);
        if (project is null) return null;

        var requiredApprovers = BuildRequiredApproverIds(project);
        if (requiredApprovers.Count == 0)
            throw new InvalidOperationException("Assign supervisor or panel members before approving completion.");

        var isCompleted = project.ApproveCompletion(facultyUserId, requiredApprovers);
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);

        var approvedCount = project.GetCompletionApprovedUserIds().Count;
        return new ApproveCompletionResponse(isCompleted, approvedCount, requiredApprovers.Count);
    }

    /// <summary>Enters or updates the final result for a completed project.</summary>
    public async Task<bool> EnterResultAsync(Guid projectId, EnterFypResultRequest request, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        project.SetFinalResult(request.Result);
        _repo.Update(project);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all FYP projects for a student.</summary>
    public async Task<IReadOnlyList<FypProjectSummaryResponse>> GetByStudentAsync(Guid studentProfileId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
    {
        var projects = await _repo.GetByStudentAsync(studentProfileId, tenantId, campusId, ct);
        return projects.Select(ToSummary).ToList();
    }

    /// <summary>Returns projects in a department, optionally filtered by status string.</summary>
    public async Task<IReadOnlyList<FypProjectSummaryResponse>> GetByDepartmentAsync(Guid departmentId, string? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
    {
        FypProjectStatus? statusEnum = null;
        if (status is not null && Enum.TryParse<FypProjectStatus>(status, ignoreCase: true, out var parsed))
            statusEnum = parsed;

        var projects = await _repo.GetByDepartmentAsync(departmentId, statusEnum, tenantId, campusId, ct);
        return projects.Select(ToSummary).ToList();
    }

    /// <summary>Returns all projects across all departments, optionally filtered by status string.</summary>
    public async Task<IReadOnlyList<FypProjectSummaryResponse>> GetAllAsync(string? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
    {
        FypProjectStatus? statusEnum = null;
        if (status is not null && Enum.TryParse<FypProjectStatus>(status, ignoreCase: true, out var parsed))
            statusEnum = parsed;

        var projects = await _repo.GetAllAsync(statusEnum, tenantId, campusId, ct);
        return projects.Select(ToSummary).ToList();
    }

    /// <summary>Returns projects supervised by a faculty user.</summary>
    public async Task<IReadOnlyList<FypProjectSummaryResponse>> GetBySupervisorAsync(Guid supervisorUserId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
    {
        var projects = await _repo.GetBySupervisorAsync(supervisorUserId, tenantId, campusId, ct);
        return projects.Select(ToSummary).ToList();
    }

    /// <summary>Returns full project detail with panel members and meeting history. Returns null if not found.</summary>
    public async Task<FypProjectDetailResponse?> GetDetailAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _repo.GetWithDetailsAsync(projectId, ct);
        return project is null ? null : ToDetail(project);
    }

    // ── Panel members ─────────────────────────────────────────────────────────

    /// <summary>Adds a faculty user to a project panel. Returns false if the project is not found.</summary>
    public async Task<bool> AddPanelMemberAsync(Guid projectId, AddPanelMemberRequest request, CancellationToken ct = default)
    {
        var project = await _repo.GetByIdAsync(projectId, ct);
        if (project is null) return false;

        if (!Enum.TryParse<FypPanelRole>(request.Role, ignoreCase: true, out var role))
            role = FypPanelRole.Examiner;

        var member = new FypPanelMember(projectId, request.UserId, role);
        await _repo.AddPanelMemberAsync(member, ct);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Removes a panel member from a project. Returns false if not found.</summary>
    public async Task<bool> RemovePanelMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var member = await _repo.GetPanelMemberAsync(projectId, userId, ct);
        if (member is null) return false;

        _repo.RemovePanelMember(member);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    // ── Meetings ──────────────────────────────────────────────────────────────

    /// <summary>Schedules a meeting for an FYP project. Returns the meeting ID.</summary>
    public async Task<Guid> ScheduleMeetingAsync(ScheduleMeetingRequest request, Guid organiserUserId, CancellationToken ct = default)
    {
        var meeting = new FypMeeting(
            request.FypProjectId,
            request.ScheduledAt,
            request.Venue,
            organiserUserId,
            request.Agenda);

        await _repo.AddMeetingAsync(meeting, ct);
        await _repo.SaveChangesAsync(ct);
        return meeting.Id;
    }

    /// <summary>Reschedules an existing meeting. Returns false if not found.</summary>
    public async Task<bool> RescheduleMeetingAsync(Guid meetingId, RescheduleMeetingRequest request, CancellationToken ct = default)
    {
        var meeting = await _repo.GetMeetingByIdAsync(meetingId, ct);
        if (meeting is null) return false;

        meeting.Reschedule(request.ScheduledAt, request.Venue, request.Agenda);
        _repo.UpdateMeeting(meeting);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Marks a meeting as completed with optional minutes. Returns false if not found.</summary>
    public async Task<bool> CompleteMeetingAsync(Guid meetingId, CompleteMeetingRequest request, CancellationToken ct = default)
    {
        var meeting = await _repo.GetMeetingByIdAsync(meetingId, ct);
        if (meeting is null) return false;

        meeting.Complete(request.Minutes);
        _repo.UpdateMeeting(meeting);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Cancels a meeting. Returns false if not found.</summary>
    public async Task<bool> CancelMeetingAsync(Guid meetingId, CancellationToken ct = default)
    {
        var meeting = await _repo.GetMeetingByIdAsync(meetingId, ct);
        if (meeting is null) return false;

        meeting.Cancel();
        _repo.UpdateMeeting(meeting);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Returns all meetings for a project.</summary>
    public async Task<IReadOnlyList<MeetingResponse>> GetMeetingsByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var meetings = await _repo.GetMeetingsByProjectAsync(projectId, ct);
        return meetings.Select(ToMeetingResponse).ToList();
    }

    /// <summary>Returns upcoming scheduled meetings for a supervisor.</summary>
    public async Task<IReadOnlyList<MeetingResponse>> GetUpcomingMeetingsAsync(Guid supervisorUserId, CancellationToken ct = default)
    {
        var meetings = await _repo.GetUpcomingMeetingsAsync(supervisorUserId, ct);
        return meetings.Select(ToMeetingResponse).ToList();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="FypProject"/> to a <see cref="FypProjectSummaryResponse"/>.</summary>
    private static FypProjectSummaryResponse ToSummary(FypProject p) => new(
        p.Id, p.StudentProfileId, p.DepartmentId, p.Title,
        p.Status.ToString(), p.GetFinalResult(), p.SupervisorUserId,
        p.IsCompletionRequested,
        p.GetCompletionApprovedUserIds().Count,
        BuildRequiredApproverIds(p).Count,
        p.GetCompletionApprovedUserIds().ToList());

    /// <summary>Maps a <see cref="FypProject"/> (with details loaded) to a <see cref="FypProjectDetailResponse"/>.</summary>
    private static FypProjectDetailResponse ToDetail(FypProject p) => new(
        p.Id, p.StudentProfileId, p.DepartmentId, p.Title, p.Description,
        p.Status.ToString(), p.GetFinalResult(), p.SupervisorUserId, p.CoordinatorRemarks,
        p.PanelMembers.Select(m => new PanelMemberResponse(m.Id, m.UserId, m.Role.ToString())).ToList(),
        p.Meetings.Select(ToMeetingResponse).ToList());

    /// <summary>Maps a <see cref="FypMeeting"/> to a <see cref="MeetingResponse"/>.</summary>
    private static MeetingResponse ToMeetingResponse(FypMeeting m) => new(
        m.Id, m.FypProjectId, m.ScheduledAt, m.Venue, m.Agenda,
        m.Status.ToString(), m.OrganiserUserId, m.Minutes);

    private static IReadOnlyList<Guid> BuildRequiredApproverIds(FypProject project)
    {
        var ids = new HashSet<Guid>();
        if (project.SupervisorUserId.HasValue)
            ids.Add(project.SupervisorUserId.Value);

        foreach (var panelUserId in project.PanelMembers.Select(x => x.UserId))
            ids.Add(panelUserId);

        return ids.ToList();
    }

    private async Task<FypStudentEligibility> EnsureStudentEligibilityAsync(Guid studentProfileId, Guid requestedDepartmentId, CancellationToken ct)
    {
        var eligibility = await _repo.GetStudentEligibilityAsync(studentProfileId, ct)
            ?? throw new InvalidOperationException("Student profile not found.");

        if (eligibility.DepartmentId != requestedDepartmentId)
            throw new InvalidOperationException("Student does not belong to the selected department.");

        if (!eligibility.IsUniversity)
            throw new InvalidOperationException("FYP is available for University students only.");

        if (eligibility.TotalSemesters <= 0 || eligibility.CurrentSemesterNumber != eligibility.TotalSemesters)
            throw new InvalidOperationException("FYP is available only in the student's last semester.");

        return eligibility;
    }
}
