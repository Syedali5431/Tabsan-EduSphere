using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Plan M8.1/M8.2 — late and missing policy, availability window, per-student/group dates ("Assign to"), waiving
/// late deductions and the student's assignment extras. Dates and deductions are also used by the submission and grading
/// paths through <see cref="IAssignmentPolicyService"/>.
/// </summary>
public sealed class AssignmentPolicyService : IAssignmentPolicyService
{
    private readonly IAssignmentPolicyRepository _repo;
    private readonly ISubmissionWorkspaceRepository _submissions;
    private readonly IAuditService _audit;
    private readonly IAssignmentGradebookSync? _gradebook;

    public AssignmentPolicyService(IAssignmentPolicyRepository repo, ISubmissionWorkspaceRepository submissions, IAuditService audit,
        IAssignmentGradebookSync? gradebook = null)
    {
        _repo = repo;
        _submissions = submissions;
        _audit = audit;
        _gradebook = gradebook;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Dates, deductions, groups (used by submission and grading)
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<EffectiveDates> DatesForAsync(Assignment assignment, Guid studentProfileId, CancellationToken ct = default)
    {
        var overrides = await _repo.GetOverridesAsync(assignment.Id, ct);
        var mine = overrides.FirstOrDefault(o => o.StudentProfileId == studentProfileId);
        var source = "student";
        if (mine is null && overrides.Any(o => o.GroupId is not null))
        {
            var groups = (await _repo.GetMembershipsAsync(studentProfileId, assignment.CourseOfferingId, ct)).Select(m => m.GroupId).ToHashSet();
            mine = overrides.Where(o => o.GroupId is { } g && groups.Contains(g)).OrderByDescending(o => o.DueDate).FirstOrDefault();
            source = "group";
        }
        return mine is null
            ? new EffectiveDates(assignment.DueDate, assignment.AvailableFrom, assignment.ClosesAt, "assignment")
            : new EffectiveDates(mine.DueDate, mine.AvailableFrom, mine.AvailableUntil ?? mine.DueDate, source);
    }

    /// <summary>Plan M8.2: the student's own due dates for assignments whose dates differ from the assignment's.</summary>
    public async Task<IReadOnlyDictionary<Guid, DateTime>> DueDatesForAsync(IReadOnlyCollection<Guid> assignmentIds, Guid studentProfileId, CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, DateTime>();
        foreach (var id in assignmentIds)
        {
            if (await _repo.GetOverridesAsync(id, ct) is not { Count: > 0 }) continue;
            if (await _submissions.GetAssignmentAsync(id, ct) is not { } a) continue;
            var dates = await DatesForAsync(a, studentProfileId, ct);
            if (dates.Source != "assignment") result[id] = dates.DueDate;
        }
        return result;
    }

    public async Task<decimal> LatePenaltyAsync(Assignment assignment, AssignmentSubmission submission, decimal rawMarks, CancellationToken ct = default)
    {
        if (submission.IsMissing || submission.CurrentAttemptNumber == 0 || submission.LatePenaltyWaived) return 0;
        var policy = await _repo.GetLatePolicyAsync(assignment.CourseOfferingId, ct);
        if (policy is not { LateDeductionEnabled: true }) return 0;
        var dates = await DatesForAsync(assignment, submission.StudentProfileId, ct);
        return policy.PenaltyFor(dates.DueDate, submission.SubmittedAt, assignment.MaxMarks, rawMarks);
    }

    public async Task<(Guid GroupId, IReadOnlyList<Guid> MemberIds)?> GroupForAsync(Assignment assignment, Guid studentProfileId, CancellationToken ct = default)
    {
        if (assignment.GroupSetId is not { } setId) return null;
        var membership = (await _repo.GetMembershipsAsync(studentProfileId, assignment.CourseOfferingId, ct)).FirstOrDefault(m => m.GroupSetId == setId);
        if (membership is null) return null;
        var members = (await _repo.GetMembersAsync([setId], ct)).Where(m => m.GroupId == membership.GroupId).Select(m => m.StudentProfileId).ToList();
        return (membership.GroupId, members);
    }

    public Task<bool> IsPeerReviewerAsync(Guid submissionId, Guid studentProfileId, CancellationToken ct = default)
        => _repo.IsReviewerOfAsync(submissionId, studentProfileId, ct);

    public async Task<string?> GroupNameAsync(Guid groupId, CancellationToken ct = default) => (await _repo.GetGroupAsync(groupId, ct))?.Name;

    public async Task<(decimal? Average, int Completed)> PeerSummaryAsync(AssignmentSubmission submission, CancellationToken ct = default)
    {
        if (submission.Assignment is not { PeerReviewCount: > 0 }) return (null, 0);
        var ids = new HashSet<Guid> { submission.Id };
        if (submission.GroupId is { } groupId)
            foreach (var s in await _submissions.GetSubmissionsAsync(submission.AssignmentId, false, ct))
                if (s.GroupId == groupId) ids.Add(s.Id);
        var done = (await _repo.GetPeerReviewsAsync(submission.AssignmentId, ct))
            .Where(r => ids.Contains(r.SubmissionId) && r.Status == PeerReviewStatus.Completed).ToList();
        var scores = done.Where(r => r.Score is not null).Select(r => r.Score!.Value).ToList();
        return (scores.Count == 0 ? null : Math.Round(scores.Average(), 2, MidpointRounding.AwayFromZero), done.Count);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Late policy
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<LatePolicyDto> GetLatePolicyAsync(Guid offeringId, CancellationToken ct = default)
        => ToDto(await _repo.GetLatePolicyAsync(offeringId, ct)) ?? new LatePolicyDto(false, 10, "Day", 0, false, 0);

    public async Task<PolicyResult<LatePolicyDto>> SaveLatePolicyAsync(Guid offeringId, LatePolicyDto r, Guid actorUserId, CancellationToken ct = default)
    {
        if (!Enum.TryParse<LateInterval>(r.Interval, true, out var interval) || !Enum.IsDefined(interval))
            return PolicyResult<LatePolicyDto>.Fail(400, "The interval must be Hour or Day.");
        var policy = await _repo.GetLatePolicyAsync(offeringId, ct);
        var isNew = policy is null;
        policy ??= new LatePolicy(offeringId);
        try { policy.Configure(r.LateDeductionEnabled, r.DeductionPercent, interval, r.LowestGradePercent, r.MissingEnabled, r.MissingGradePercent); }
        catch (ArgumentException ex) { return PolicyResult<LatePolicyDto>.Fail(400, ex.Message); }
        if (isNew) _repo.Add(policy);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LatePolicyChanged", "CourseOffering", offeringId.ToString(), actorUserId: actorUserId,
            newValuesJson: System.Text.Json.JsonSerializer.Serialize(r)), ct);
        return PolicyResult<LatePolicyDto>.Success(ToDto(policy)!);
    }

    private static LatePolicyDto? ToDto(LatePolicy? p) => p is null ? null
        : new LatePolicyDto(p.LateDeductionEnabled, p.DeductionPercent, p.Interval.ToString(), p.LowestGradePercent, p.MissingEnabled, p.MissingGradePercent);

    // ═════════════════════════════════════════════════════════════════════════
    // Assignment options (availability, groups, peer review)
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<AssignmentOptionsDto?> GetOptionsAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var a = await _submissions.GetAssignmentAsync(assignmentId, ct);
        return a is null ? null : await ToOptionsAsync(a, ct);
    }

    public async Task<PolicyResult<AssignmentOptionsDto>> SaveOptionsAsync(Guid assignmentId, SaveAssignmentOptionsRequest r, Guid actorUserId, CancellationToken ct = default)
    {
        var a = await _submissions.GetAssignmentAsync(assignmentId, ct);
        if (a is null) return PolicyResult<AssignmentOptionsDto>.Fail(404, "Assignment not found.");
        if (r.GroupSetId is { } setId && (await _repo.GetGroupSetAsync(setId, ct))?.CourseOfferingId != a.CourseOfferingId)
            return PolicyResult<AssignmentOptionsDto>.Fail(400, "Choose a group set of this course.");
        if (r.GroupSetId != a.GroupSetId && (await _submissions.GetSubmissionsAsync(assignmentId, false, ct)).Any(s => s.CurrentAttemptNumber > 0))
            return PolicyResult<AssignmentOptionsDto>.Fail(409, "Work has already been handed in, so the grouping cannot change.");
        try
        {
            a.ConfigureAvailability(r.AvailableFrom, r.AvailableUntil);
            a.ConfigureGroups(r.GroupSetId, r.GradeGroupIndividually);
            a.ConfigurePeerReview(r.PeerReviewCount, r.PeerReviewDueDate, r.PeerReviewAnonymous);
        }
        catch (ArgumentException ex) { return PolicyResult<AssignmentOptionsDto>.Fail(400, ex.Message); }
        await _submissions.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("AssignmentOptionsChanged", "Assignment", assignmentId.ToString(), actorUserId: actorUserId,
            newValuesJson: System.Text.Json.JsonSerializer.Serialize(r)), ct);
        return PolicyResult<AssignmentOptionsDto>.Success(await ToOptionsAsync(a, ct));
    }

    private async Task<AssignmentOptionsDto> ToOptionsAsync(Assignment a, CancellationToken ct)
        => new(a.Id, a.DueDate, a.AvailableFrom, a.AvailableUntil, a.GroupSetId, a.GradeGroupIndividually, a.PeerReviewCount, a.PeerReviewDueDate,
            a.PeerReviewAnonymous, (await _submissions.GetSubmissionsAsync(a.Id, false, ct)).Any(s => s.CurrentAttemptNumber > 0));

    // ═════════════════════════════════════════════════════════════════════════
    // Overrides
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<AssignmentOverrideDto>> ListOverridesAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var overrides = await _repo.GetOverridesAsync(assignmentId, ct);
        var students = await _submissions.GetStudentNamesAsync(overrides.Where(o => o.StudentProfileId is not null).Select(o => o.StudentProfileId!.Value).ToList(), ct);
        var groups = new Dictionary<Guid, string>();
        foreach (var id in overrides.Where(o => o.GroupId is not null).Select(o => o.GroupId!.Value).Distinct())
            if (await _repo.GetGroupAsync(id, ct) is { } g) groups[id] = g.Name;
        return overrides.OrderBy(o => o.DueDate).Select(o => new AssignmentOverrideDto(o.Id, o.StudentProfileId is not null ? "Student" : "Group",
            o.StudentProfileId, o.GroupId,
            o.StudentProfileId is { } s ? students.TryGetValue(s, out var n) ? n.Name : "Student" : o.GroupId is { } gid && groups.TryGetValue(gid, out var gn) ? gn : "Group",
            o.DueDate, o.AvailableFrom, o.AvailableUntil, o.Note)).ToList();
    }

    public async Task<PolicyResult<AssignmentOverrideDto>> AddOverrideAsync(Guid assignmentId, AddOverrideRequest r, Guid actorUserId, CancellationToken ct = default)
    {
        var a = await _submissions.GetAssignmentAsync(assignmentId, ct);
        if (a is null) return PolicyResult<AssignmentOverrideDto>.Fail(404, "Assignment not found.");
        if (r.StudentProfileId is { } studentId && !await _submissions.IsEnrolledAsync(a.CourseOfferingId, studentId, ct))
            return PolicyResult<AssignmentOverrideDto>.Fail(400, "The student is not enrolled in this course.");
        if (r.GroupId is { } groupId)
        {
            var group = await _repo.GetGroupAsync(groupId, ct);
            var set = group is null ? null : await _repo.GetGroupSetAsync(group.GroupSetId, ct);
            if (set?.CourseOfferingId != a.CourseOfferingId) return PolicyResult<AssignmentOverrideDto>.Fail(400, "Choose a group of this course.");
        }
        AssignmentOverride item;
        try { item = new AssignmentOverride(assignmentId, r.StudentProfileId, r.GroupId, Utc(r.DueDate), Utc(r.AvailableFrom), Utc(r.AvailableUntil), r.Note); }
        catch (ArgumentException ex) { return PolicyResult<AssignmentOverrideDto>.Fail(400, ex.Message); }
        // One override per student or group: a new one replaces the old.
        foreach (var old in (await _repo.GetOverridesAsync(assignmentId, ct)).Where(o => o.StudentProfileId == r.StudentProfileId && o.GroupId == r.GroupId))
            _repo.Remove(old);
        _repo.Add(item);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("AssignmentOverrideAdded", "Assignment", assignmentId.ToString(), actorUserId: actorUserId,
            newValuesJson: $"{{\"student\":\"{r.StudentProfileId}\",\"group\":\"{r.GroupId}\",\"due\":\"{item.DueDate:O}\"}}"), ct);
        return PolicyResult<AssignmentOverrideDto>.Success((await ListOverridesAsync(assignmentId, ct)).First(o => o.Id == item.Id), 201);
    }

    public async Task<PolicyResult<bool>> RemoveOverrideAsync(Guid overrideId, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await _repo.GetOverrideAsync(overrideId, ct);
        if (item is null) return PolicyResult<bool>.Fail(404, "Override not found.");
        _repo.Remove(item);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("AssignmentOverrideRemoved", "Assignment", item.AssignmentId.ToString(), actorUserId: actorUserId), ct);
        return PolicyResult<bool>.Success(true);
    }

    public async Task<Guid?> AssignmentOfOverrideAsync(Guid overrideId, CancellationToken ct = default) => (await _repo.GetOverrideAsync(overrideId, ct))?.AssignmentId;

    // ═════════════════════════════════════════════════════════════════════════
    // Missing submissions and waivers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gives the policy's missing grade to students with no work after their (overridden) due date. A student who later
    /// hands in late work replaces the missing grade.
    /// </summary>
    public async Task<MissingRunResult> ApplyMissingAsync(Guid offeringId, Guid? onlyAssignmentId, DateTime now, CancellationToken ct = default)
    {
        var policy = await _repo.GetLatePolicyAsync(offeringId, ct);
        if (policy is not { MissingEnabled: true }) return new MissingRunResult(0, 0);
        var assignments = (await _repo.GetGradableAssignmentsAsync(offeringId, ct)).Where(a => onlyAssignmentId is null || a.Id == onlyAssignmentId).ToList();
        var students = await _repo.GetEnrolledStudentIdsAsync(offeringId, ct);
        var changed = new HashSet<Guid>();
        var graded = 0;
        foreach (var assignment in assignments)
        {
            var submissions = (await _submissions.GetSubmissionsAsync(assignment.Id, false, ct)).ToDictionary(s => s.StudentProfileId);
            foreach (var studentId in students)
            {
                if (submissions.TryGetValue(studentId, out var existing) && (existing.CurrentAttemptNumber > 0 || existing.Status == SubmissionStatus.Rejected)) continue;
                var dates = await DatesForAsync(assignment, studentId, ct);
                if (now <= dates.DueDate) continue;
                if (existing is null)
                {
                    existing = AssignmentSubmission.Start(assignment.Id, studentId);
                    _submissions.Add(existing);
                }
                if (existing.MarkMissing(policy.MissingMarks(assignment.MaxMarks), now, assignment.PostingPolicy == GradePostingPolicy.Automatic))
                {
                    graded++;
                    changed.Add(studentId);
                }
            }
        }
        if (graded > 0)
        {
            await _submissions.SaveChangesAsync(ct);
            await _audit.LogAsync(new AuditLog("MissingGradesApplied", "CourseOffering", offeringId.ToString(),
                newValuesJson: $"{{\"graded\":{graded},\"assignments\":{assignments.Count}}}"), ct);
            if (_gradebook is not null) foreach (var s in changed) await _gradebook.RefreshAsync(offeringId, s, ct);
        }
        return new MissingRunResult(graded, assignments.Count);
    }

    /// <summary>Runs the missing-submission policy for every course that uses it.</summary>
    public async Task<int> ApplyMissingEverywhereAsync(DateTime now, CancellationToken ct = default)
    {
        var total = 0;
        foreach (var policy in await _repo.GetMissingPoliciesAsync(ct))
            total += (await ApplyMissingAsync(policy.CourseOfferingId, null, now, ct)).Graded;
        return total;
    }

    public async Task<PolicyResult<bool>> WaiveLatePenaltyAsync(Guid submissionId, bool waived, Guid actorUserId, CancellationToken ct = default)
    {
        var submission = await _submissions.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null) return PolicyResult<bool>.Fail(404, "Submission not found.");
        if (submission.RawMarks is not { } raw) return PolicyResult<bool>.Fail(409, "Grade the submission first.");
        var penalty = waived ? 0 : await PenaltyIgnoringWaiverAsync(submission, raw, ct);
        submission.SetLatePenaltyWaived(waived, penalty);
        await _submissions.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog(waived ? "LatePenaltyWaived" : "LatePenaltyRestored", "AssignmentSubmission", submissionId.ToString(),
            actorUserId: actorUserId, newValuesJson: $"{{\"raw\":{raw},\"penalty\":{submission.LatePenalty}}}"), ct);
        if (submission.PostedAt.HasValue && _gradebook is not null)
            await _gradebook.RefreshAsync(submission.Assignment.CourseOfferingId, submission.StudentProfileId, ct);
        return PolicyResult<bool>.Success(true);
    }

    private async Task<decimal> PenaltyIgnoringWaiverAsync(AssignmentSubmission s, decimal raw, CancellationToken ct)
    {
        if (s.IsMissing || s.CurrentAttemptNumber == 0) return 0;
        var policy = await _repo.GetLatePolicyAsync(s.Assignment.CourseOfferingId, ct);
        if (policy is not { LateDeductionEnabled: true }) return 0;
        var dates = await DatesForAsync(s.Assignment, s.StudentProfileId, ct);
        return policy.PenaltyFor(dates.DueDate, s.SubmittedAt, s.Assignment.MaxMarks, raw);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Student extras
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<StudentAssignmentExtrasDto?> StudentExtrasAsync(Guid assignmentId, Guid studentProfileId, CancellationToken ct = default)
    {
        var a = await _submissions.GetAssignmentAsync(assignmentId, ct);
        if (a is null || !a.IsPublished) return null;
        var dates = await DatesForAsync(a, studentProfileId, ct);
        var policy = ToDto(await _repo.GetLatePolicyAsync(a.CourseOfferingId, ct));

        string? groupName = null;
        var memberNames = new List<string>();
        var needsGroup = false;
        if (await GroupForAsync(a, studentProfileId, ct) is { } group)
        {
            groupName = (await _repo.GetGroupAsync(group.GroupId, ct))?.Name;
            var names = await _submissions.GetStudentNamesAsync(group.MemberIds, ct);
            memberNames = group.MemberIds.Select(id => names.TryGetValue(id, out var n) ? n.Name : "Student").OrderBy(n => n).ToList();
        }
        else needsGroup = a.GroupSetId is not null;

        var reviews = a.PeerReviewCount > 0 ? await _repo.GetPeerReviewsAsync(assignmentId, ct) : [];
        var mySubmission = await _submissions.GetSubmissionAsync(assignmentId, studentProfileId, false, ct);
        var toDo = reviews.Where(r => r.ReviewerStudentProfileId == studentProfileId)
            .Select((r, i) => new PeerReviewSummaryDto(r.Id, r.SubmissionId, $"Peer submission {i + 1}", "You", r.Status.ToString(), r.Score, r.CompletedAt)).ToList();
        var receivedFor = new HashSet<Guid>();
        if (mySubmission is not null) receivedFor.Add(mySubmission.Id);
        var reviewerNames = a.PeerReviewAnonymous ? new Dictionary<Guid, (string Name, string? RegistrationNumber)>()
            : await _submissions.GetStudentNamesAsync(reviews.Select(r => r.ReviewerStudentProfileId).Distinct().ToList(), ct);
        // Group work: reviews of any member's submission count for the whole group.
        if (mySubmission?.GroupId is { } myGroup)
            foreach (var s in await _submissions.GetSubmissionsAsync(assignmentId, false, ct))
                if (s.GroupId == myGroup) receivedFor.Add(s.Id);
        var received = reviews.Where(r => receivedFor.Contains(r.SubmissionId) && r.Status == PeerReviewStatus.Completed)
            .Select((r, i) => new ReceivedPeerReviewDto(a.PeerReviewAnonymous ? $"Peer {i + 1}" : reviewerNames.TryGetValue(r.ReviewerStudentProfileId, out var n) ? n.Name : "Peer",
                r.Score, r.Comment, r.CompletedAt)).ToList();

        var visible = mySubmission?.IsGradeVisibleToStudent == true;
        return new StudentAssignmentExtrasDto(a.Id, a.CourseOfferingId, dates.DueDate, dates.AvailableFrom, dates.ClosesAt, dates.Source != "assignment", policy,
            groupName, memberNames, needsGroup, a.PeerReviewCount, a.PeerReviewDueDate, toDo, received,
            visible ? mySubmission!.RawMarks : null, visible ? mySubmission!.LatePenalty : 0, visible && mySubmission!.IsMissing);
    }

    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
    private static DateTime? Utc(DateTime? value) => value is { } v ? Utc(v) : null;
}
