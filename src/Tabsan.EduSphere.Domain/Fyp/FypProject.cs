using Tabsan.EduSphere.Domain.Common;
using System.Globalization;

namespace Tabsan.EduSphere.Domain.Fyp;

/// <summary>
/// Lifecycle stages of a Final Year Project.
/// </summary>
public enum FypProjectStatus
{
    /// <summary>Project title and description submitted by the student, awaiting coordinator review.</summary>
    Proposed = 1,

    /// <summary>Approved by the FYP coordinator — supervisor assignment pending.</summary>
    Approved = 2,

    /// <summary>Supervisor assigned and active work is in progress.</summary>
    InProgress = 3,

    /// <summary>Project completed and final submission accepted.</summary>
    Completed = 4,

    /// <summary>Proposal or submission rejected; student may resubmit.</summary>
    Rejected = 5
}

/// <summary>
/// Roles a faculty member can hold on an FYP project panel.
/// </summary>
public enum FypPanelRole
{
    /// <summary>Primary supervisor guiding the student throughout the project.</summary>
    Supervisor = 1,

    /// <summary>Co-supervisor providing additional domain expertise.</summary>
    CoSupervisor = 2,

    /// <summary>External or internal examiner evaluating the final submission.</summary>
    Examiner = 3
}

/// <summary>
/// A Final Year Project entity linking a student to a research or development topic.
/// Business rules:
///   - One active FYP project per student at a time.
///   - Status transitions: Proposed → Approved → InProgress → Completed or Rejected.
///   - Only admins/coordinators may approve or reject; supervisor assignment is separate.
/// </summary>
public class FypProject : BaseEntity
{
    /// <summary>FK to the student who owns this project.</summary>
    public Guid StudentProfileId { get; private set; }

    /// <summary>FK to the department this project is registered under.</summary>
    public Guid DepartmentId { get; private set; }

    /// <summary>Short project title shown in listings.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Full project description and objectives.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Current lifecycle status of the project.</summary>
    public FypProjectStatus Status { get; private set; }

    /// <summary>
    /// FK to the primary supervisor (faculty user).
    /// Null until a supervisor is assigned after approval.
    /// </summary>
    public Guid? SupervisorUserId { get; private set; }

    /// <summary>Optional remarks from the coordinator on approval or rejection.</summary>
    public string? CoordinatorRemarks { get; private set; }

    /// <summary>True when the student has requested completion and faculty approvals are pending.</summary>
    public bool IsCompletionRequested { get; private set; }

    /// <summary>UTC timestamp when completion was requested by the student.</summary>
    public DateTime? CompletionRequestedAt { get; private set; }

    /// <summary>Student profile ID that submitted the completion request.</summary>
    public Guid? CompletionRequestedByStudentProfileId { get; private set; }

    /// <summary>
    /// Comma-separated faculty user IDs who approved completion.
    /// Stored as CSV to avoid a separate join table for this workflow state.
    /// </summary>
    public string? CompletionApprovedByUserIdsCsv { get; private set; }

    /// <summary>Navigation collection of panel members (supervisors, co-supervisors, examiners).</summary>
    public IReadOnlyCollection<FypPanelMember> PanelMembers { get; private set; } = new List<FypPanelMember>();

    /// <summary>Navigation collection of scheduled meetings for this project.</summary>
    public IReadOnlyCollection<FypMeeting> Meetings { get; private set; } = new List<FypMeeting>();

    // ── EF constructor ─────────────────────────────────────────────────────────
    private FypProject() { }

    /// <summary>
    /// Creates a new FYP project proposal in the Proposed state.
    /// </summary>
    /// <param name="studentProfileId">FK to the student proposing the project.</param>
    /// <param name="departmentId">FK to the department this project is registered under.</param>
    /// <param name="title">Short descriptive title (max 500 characters).</param>
    /// <param name="description">Full description of the project scope and objectives.</param>
    public FypProject(Guid studentProfileId, Guid departmentId, string title, string description)
    {
        StudentProfileId = studentProfileId;
        DepartmentId     = departmentId;
        Title            = title;
        Description      = description;
        Status           = FypProjectStatus.Proposed;
    }

    /// <summary>
    /// Approves the project proposal and optionally adds coordinator remarks.
    /// Transitions status from Proposed → Approved.
    /// </summary>
    /// <param name="remarks">Optional coordinator feedback.</param>
    public void Approve(string? remarks = null)
    {
        Status              = FypProjectStatus.Approved;
        CoordinatorRemarks  = remarks;
        Touch();
    }

    /// <summary>
    /// Rejects the project proposal with mandatory feedback remarks.
    /// </summary>
    /// <param name="remarks">Reason for rejection shown to the student.</param>
    public void Reject(string remarks)
    {
        Status             = FypProjectStatus.Rejected;
        CoordinatorRemarks = remarks;
        Touch();
    }

    /// <summary>
    /// Assigns a primary supervisor and moves the project to InProgress.
    /// Can only be called after the project is Approved.
    /// </summary>
    /// <param name="supervisorUserId">FK to the faculty user acting as supervisor.</param>
    public void AssignSupervisor(Guid supervisorUserId)
    {
        SupervisorUserId = supervisorUserId;
        Status           = FypProjectStatus.InProgress;
        Touch();
    }

    /// <summary>
    /// Marks the project as successfully completed.
    /// </summary>
    public void Complete()
    {
        Status = FypProjectStatus.Completed;
        IsCompletionRequested = false;
        Touch();
    }

    /// <summary>
    /// Updates the project title and description (only while Proposed or Rejected).
    /// </summary>
    /// <param name="title">Updated title.</param>
    /// <param name="description">Updated description.</param>
    public void Update(string title, string description)
    {
        Title       = title;
        Description = description;
        Touch();
    }

    /// <summary>
    /// Marks the project as awaiting faculty completion approvals.
    /// </summary>
    public void RequestCompletion(Guid studentProfileId)
    {
        if (Status != FypProjectStatus.InProgress)
            throw new InvalidOperationException("Only in-progress projects can request completion.");

        if (StudentProfileId != studentProfileId)
            throw new InvalidOperationException("Only the project owner can request completion.");

        IsCompletionRequested = true;
        CompletionRequestedAt = DateTime.UtcNow;
        CompletionRequestedByStudentProfileId = studentProfileId;
        CompletionApprovedByUserIdsCsv = string.Empty;
        Touch();
    }

    /// <summary>
    /// Registers a faculty completion approval and auto-completes when all required approvers have approved.
    /// Returns true when the project reached Completed state in this call.
    /// </summary>
    public bool ApproveCompletion(Guid facultyUserId, IReadOnlyCollection<Guid> requiredApprovers)
    {
        if (Status != FypProjectStatus.InProgress)
            throw new InvalidOperationException("Only in-progress projects can be approved for completion.");

        if (!IsCompletionRequested)
            throw new InvalidOperationException("Student completion request is required before faculty approvals.");

        if (!requiredApprovers.Contains(facultyUserId))
            throw new InvalidOperationException("Only assigned faculty members can approve completion.");

        var approved = ParseApprovalUserIds();
        approved.Add(facultyUserId);
        CompletionApprovedByUserIdsCsv = string.Join(',', approved.Select(x => x.ToString("D", CultureInfo.InvariantCulture)));

        var allApproved = requiredApprovers.All(approved.Contains);
        if (allApproved)
        {
            Complete();
            return true;
        }

        Touch();
        return false;
    }

    /// <summary>
    /// Sets or updates the final result for a completed project.
    /// </summary>
    public void SetFinalResult(string result)
    {
        if (Status != FypProjectStatus.Completed)
            throw new InvalidOperationException("Result can only be entered after the project is completed.");

        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException("Result is required.");

        CoordinatorRemarks = result.Trim();
        Touch();
    }

    /// <summary>Returns faculty user IDs that already approved completion.</summary>
    public IReadOnlyCollection<Guid> GetCompletionApprovedUserIds() => ParseApprovalUserIds();

    /// <summary>Returns the final entered result once the project is completed.</summary>
    public string? GetFinalResult()
        => Status == FypProjectStatus.Completed ? CoordinatorRemarks : null;

    private HashSet<Guid> ParseApprovalUserIds()
    {
        var ids = new HashSet<Guid>();
        if (string.IsNullOrWhiteSpace(CompletionApprovedByUserIdsCsv))
            return ids;

        foreach (var raw in CompletionApprovedByUserIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(raw, out var id))
                ids.Add(id);
        }

        return ids;
    }
}

/// <summary>
/// A faculty panel member (supervisor, co-supervisor, or examiner) assigned to an FYP project.
/// </summary>
public class FypPanelMember : BaseEntity
{
    /// <summary>FK to the FYP project this member is assigned to.</summary>
    public Guid FypProjectId { get; private set; }

    /// <summary>Navigation property to the parent project.</summary>
    public FypProject Project { get; private set; } = default!;

    /// <summary>FK to the faculty user serving on the panel.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Role this member holds on the project panel.</summary>
    public FypPanelRole Role { get; private set; }

    // ── EF constructor ─────────────────────────────────────────────────────────
    private FypPanelMember() { }

    /// <summary>
    /// Assigns a faculty user to a project panel with the given role.
    /// </summary>
    /// <param name="fypProjectId">FK to the project.</param>
    /// <param name="userId">FK to the faculty user.</param>
    /// <param name="role">Panel role (Supervisor, CoSupervisor, or Examiner).</param>
    public FypPanelMember(Guid fypProjectId, Guid userId, FypPanelRole role)
    {
        FypProjectId = fypProjectId;
        UserId       = userId;
        Role         = role;
    }
}

/// <summary>
/// Lifecycle states of an FYP meeting.
/// </summary>
public enum MeetingStatus
{
    /// <summary>Meeting is confirmed and upcoming.</summary>
    Scheduled = 1,

    /// <summary>Meeting took place as scheduled.</summary>
    Completed = 2,

    /// <summary>Meeting was cancelled before it occurred.</summary>
    Cancelled = 3
}

/// <summary>
/// A supervision or review meeting scheduled for an FYP project.
/// </summary>
public class FypMeeting : BaseEntity
{
    /// <summary>FK to the FYP project this meeting relates to.</summary>
    public Guid FypProjectId { get; private set; }

    /// <summary>Navigation property to the parent project.</summary>
    public FypProject Project { get; private set; } = default!;

    /// <summary>UTC date-time the meeting is scheduled to begin.</summary>
    public DateTime ScheduledAt { get; private set; }

    /// <summary>Physical room name or online meeting URL.</summary>
    public string Venue { get; private set; } = default!;

    /// <summary>Brief agenda or topic list for the meeting.</summary>
    public string? Agenda { get; private set; }

    /// <summary>Current status of the meeting.</summary>
    public MeetingStatus Status { get; private set; }

    /// <summary>FK to the faculty user who created the meeting invitation.</summary>
    public Guid OrganiserUserId { get; private set; }

    /// <summary>Minutes or outcome notes recorded after the meeting completes.</summary>
    public string? Minutes { get; private set; }

    // ── EF constructor ─────────────────────────────────────────────────────────
    private FypMeeting() { }

    /// <summary>
    /// Schedules a new meeting for an FYP project.
    /// </summary>
    /// <param name="fypProjectId">FK to the project.</param>
    /// <param name="scheduledAt">UTC start time of the meeting.</param>
    /// <param name="venue">Room name or meeting URL.</param>
    /// <param name="organiserUserId">FK to the faculty user scheduling the meeting.</param>
    /// <param name="agenda">Optional meeting agenda.</param>
    public FypMeeting(Guid fypProjectId, DateTime scheduledAt, string venue, Guid organiserUserId, string? agenda = null)
    {
        FypProjectId     = fypProjectId;
        ScheduledAt      = scheduledAt;
        Venue            = venue;
        OrganiserUserId  = organiserUserId;
        Agenda           = agenda;
        Status           = MeetingStatus.Scheduled;
    }

    /// <summary>
    /// Marks the meeting as completed and records the outcome minutes.
    /// </summary>
    /// <param name="minutes">Summary or action items from the meeting.</param>
    public void Complete(string? minutes = null)
    {
        Status  = MeetingStatus.Completed;
        Minutes = minutes;
        Touch();
    }

    /// <summary>
    /// Cancels the meeting.
    /// Has no effect if the meeting is already completed.
    /// </summary>
    public void Cancel()
    {
        if (Status == MeetingStatus.Completed) return;
        Status = MeetingStatus.Cancelled;
        Touch();
    }

    /// <summary>
    /// Reschedules the meeting to a new time and venue.
    /// Only valid while the meeting is in Scheduled state.
    /// </summary>
    /// <param name="scheduledAt">New UTC start time.</param>
    /// <param name="venue">New room or meeting URL.</param>
    /// <param name="agenda">Updated agenda.</param>
    public void Reschedule(DateTime scheduledAt, string venue, string? agenda)
    {
        ScheduledAt = scheduledAt;
        Venue       = venue;
        Agenda      = agenda;
        Touch();
    }
}
