using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;

namespace Tabsan.EduSphere.Application.Assignments;

public sealed partial class SubmissionWorkspaceService
{
    public async Task<WorkspaceResult<GradingWorkspaceResponse>> GetWorkspaceAsync(WorkspaceActor actor, Guid assignmentId, string? filter, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return WorkspaceResult<GradingWorkspaceResponse>.Fail(WorkspaceError.Forbidden);
        var assignment = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (assignment is null) return WorkspaceResult<GradingWorkspaceResponse>.Fail(WorkspaceError.NotFound);

        var submissions = (await _repo.GetSubmissionsAsync(assignmentId, false, ct)).ToList();
        var order = await OrderedSubmissionIdsAsync(assignment, submissions, ct);
        var numbers = AnonymousNumbers(submissions);
        var names = assignment.NamesHidden
            ? new Dictionary<Guid, (string Name, string? RegistrationNumber)>()
            : await _repo.GetStudentNamesAsync(submissions.Select(s => s.StudentProfileId).Distinct().ToList(), ct);
        var provisional = assignment.ModeratedGrading
            ? (await _repo.GetProvisionalGradesAsync(submissions.Select(s => s.Id).ToList(), ct)).GroupBy(g => g.SubmissionId).ToDictionary(g => g.Key, g => g.Count())
            : new Dictionary<Guid, int>();

        var byId = submissions.ToDictionary(s => s.Id);
        var items = order.Select(id => byId[id]).Select(s =>
        {
            var hidden = assignment.NamesHidden;
            var name = hidden ? $"Student {numbers[s.Id]}" : names.TryGetValue(s.StudentProfileId, out var n) ? n.Name : "Student";
            var reg = hidden ? null : names.TryGetValue(s.StudentProfileId, out var n2) ? n2.RegistrationNumber : null;
            var needsGrading = s.Status == SubmissionStatus.Submitted;
            return new WorkspaceSubmissionItem(s.Id, name, reg, hidden ? null : s.StudentProfileId, s.Status.ToString(), s.IsLate,
                s.CurrentAttemptNumber, s.SubmittedAt, s.MarksAwarded, s.PostedAt.HasValue, needsGrading, provisional.GetValueOrDefault(s.Id),
                s.IsMissing, s.LatePenalty, s.GroupId);
        });

        items = (filter ?? "all").ToLowerInvariant() switch
        {
            "ungraded" => items.Where(i => i.NeedsGrading),
            "late" => items.Where(i => i.IsLate),
            "unposted" => items.Where(i => i.MarksAwarded.HasValue && !i.IsPosted),
            _ => items
        };

        var hasRubric = await _rubrics.GetByAssignmentAsync(assignmentId, ct) is not null;
        return WorkspaceResult<GradingWorkspaceResponse>.Success(new GradingWorkspaceResponse(
            assignment.Id, assignment.Title, assignment.CourseOfferingId, assignment.MaxMarks, assignment.DueDate,
            await ToSettingsAsync(assignment, ct), items.ToList(),
            submissions.Count(s => s.MarksAwarded.HasValue),
            submissions.Count(s => s.PostedAt.HasValue),
            submissions.Count(s => s.MarksAwarded.HasValue && !s.PostedAt.HasValue),
            hasRubric, _converter?.IsEnabled == true, CanModerate(actor, assignment)));
    }

    public async Task<WorkspaceResult<SaveGradeResponse>> SaveGradeAsync(WorkspaceActor actor, Guid submissionId, SaveGradeRequest request, CancellationToken ct = default)
    {
        static WorkspaceResult<SaveGradeResponse> Fail(WorkspaceError e, string? m = null) => WorkspaceResult<SaveGradeResponse>.Fail(e, m);
        if (!actor.IsStaff) return Fail(WorkspaceError.Forbidden);
        var submission = await _repo.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null) return Fail(WorkspaceError.NotFound);
        var assignment = submission.Assignment;

        var marks = request.MarksAwarded ?? submission.MarksAwarded;
        if (marks is null) return Fail(WorkspaceError.Invalid, "Enter marks before saving.");
        if (marks < 0 || marks > assignment.MaxMarks) return Fail(WorkspaceError.Invalid, $"Marks must be between 0 and {assignment.MaxMarks:0.##}.");
        if (request.Feedback is { Length: > 2000 }) return Fail(WorkspaceError.Invalid, "Feedback is limited to 2000 characters.");
        if (submission.Status == SubmissionStatus.Rejected) return Fail(WorkspaceError.Conflict, "This submission was rejected.");

        // Moderated grading: everyone except the moderator records a provisional grade.
        if (assignment.ModeratedGrading && !CanModerate(actor, assignment))
        {
            var mine = (await _repo.GetProvisionalGradesAsync([submissionId], ct)).FirstOrDefault(g => g.GraderUserId == actor.UserId);
            if (mine is null)
            {
                mine = new ProvisionalGrade(submissionId, actor.UserId, marks.Value, request.Feedback);
                _repo.Add(mine);
            }
            else mine.Update(marks.Value, request.Feedback);
            await _repo.SaveChangesAsync(ct);
            await _audit.LogAsync(new AuditLog("ProvisionalGrade", "AssignmentSubmission", submission.Id.ToString(), actorUserId: actor.UserId,
                newValuesJson: $"{{\"marks\":{marks.Value}}}"), ct);
            return WorkspaceResult<SaveGradeResponse>.Success(new SaveGradeResponse("provisional", marks, submission.PostedAt.HasValue, true));
        }

        var wasPosted = submission.PostedAt.HasValue;
        await GradeWithPolicyAsync(submission, marks.Value, request.Feedback, actor.UserId, ct);
        // Plan M8.3: a group graded as one gets the same grade on every member's submission.
        var groupMates = await GroupMatesAsync(submission, ct);
        var refresh = new List<Guid> { submission.StudentProfileId };
        foreach (var mate in groupMates)
        {
            var mateWasPosted = mate.PostedAt.HasValue;
            await GradeWithPolicyAsync(mate, marks.Value, request.Feedback, actor.UserId, ct);
            if (mate.PostedAt.HasValue || mateWasPosted) refresh.Add(mate.StudentProfileId);
        }
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("GradeSubmission", "AssignmentSubmission", submission.Id.ToString(), actorUserId: actor.UserId,
            newValuesJson: $"{{\"marks\":{marks.Value},\"penalty\":{submission.LatePenalty},\"groupMembers\":{groupMates.Count},\"posted\":{(submission.PostedAt.HasValue ? "true" : "false")}}}"), ct);
        if (submission.PostedAt.HasValue || wasPosted || refresh.Count > 1)
            await RefreshGradebookAsync(assignment.CourseOfferingId, refresh, ct);

        return WorkspaceResult<SaveGradeResponse>.Success(new SaveGradeResponse(submission.PostedAt.HasValue ? "posted" : "saved",
            submission.MarksAwarded, submission.PostedAt.HasValue, false, submission.LatePenalty, groupMates.Count));
    }

    public async Task<WorkspaceResult<SubmissionDetailResponse>> ScoreRubricAsync(WorkspaceActor actor, Guid submissionId, RubricScoreRequest request, CancellationToken ct = default)
    {
        static WorkspaceResult<SubmissionDetailResponse> Fail(WorkspaceError e, string? m = null) => WorkspaceResult<SubmissionDetailResponse>.Fail(e, m);
        if (!actor.IsStaff) return Fail(WorkspaceError.Forbidden);
        var submission = await _repo.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null) return Fail(WorkspaceError.NotFound);
        if (assignmentModeratedForOthers(submission.Assignment)) return Fail(WorkspaceError.Conflict, "Under moderated grading only the moderator scores the rubric.");

        var rubric = await _rubrics.GetByAssignmentAsync(submission.AssignmentId, ct);
        if (rubric is null) return Fail(WorkspaceError.NotFound, "This assignment has no rubric.");
        if (request.Grades.Count == 0) return Fail(WorkspaceError.Invalid, "Choose a level for at least one criterion.");

        var criteria = rubric.Criteria.ToDictionary(c => c.Id);
        foreach (var g in request.Grades)
        {
            if (!criteria.TryGetValue(g.CriterionId, out var criterion)) return Fail(WorkspaceError.Invalid, "A criterion does not belong to this rubric.");
            var level = criterion.Levels.FirstOrDefault(l => l.Id == g.LevelId);
            if (level is null) return Fail(WorkspaceError.Invalid, $"That level does not belong to \"{criterion.Name}\".");

            var existing = await _rubrics.GetStudentGradeAsync(submissionId, criterion.Id, ct);
            if (existing is null)
                await _rubrics.AddStudentGradeAsync(RubricStudentGrade.Create(submissionId, criterion.Id, level.Id, level.PointsAwarded, actor.UserId), ct);
            else
            {
                existing.Update(level.Id, level.PointsAwarded, actor.UserId);
                _rubrics.UpdateStudentGrade(existing);
            }
        }
        await _rubrics.SaveChangesAsync(ct);

        if (request.ApplyTotalToMarks)
        {
            var view = await BuildRubricViewAsync(submission.AssignmentId, submissionId, ct);
            if (view is not null && view.MaxPoints > 0)
            {
                // The rubric total is scaled to the assignment's marks when the two differ.
                var marks = Math.Round(view.TotalPoints / view.MaxPoints * submission.Assignment.MaxMarks, 2, MidpointRounding.AwayFromZero);
                var saved = await SaveGradeAsync(actor, submissionId, new SaveGradeRequest(marks, submission.Feedback), ct);
                if (!saved.Ok) return Fail(saved.Error, saved.Message);
            }
        }

        return await GetSubmissionAsync(actor, submissionId, ct);

        bool assignmentModeratedForOthers(Assignment a) => a.ModeratedGrading && !CanModerate(actor, a);
    }

    public Task<WorkspaceResult<PostGradesResponse>> PostGradesAsync(WorkspaceActor actor, Guid assignmentId, PostGradesRequest request, CancellationToken ct = default)
        => ChangePostingAsync(actor, assignmentId, request, post: true, ct);

    public Task<WorkspaceResult<PostGradesResponse>> HideGradesAsync(WorkspaceActor actor, Guid assignmentId, PostGradesRequest request, CancellationToken ct = default)
        => ChangePostingAsync(actor, assignmentId, request, post: false, ct);

    private async Task<WorkspaceResult<PostGradesResponse>> ChangePostingAsync(WorkspaceActor actor, Guid assignmentId, PostGradesRequest request, bool post, CancellationToken ct)
    {
        if (!actor.IsStaff) return WorkspaceResult<PostGradesResponse>.Fail(WorkspaceError.Forbidden);
        var assignment = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (assignment is null) return WorkspaceResult<PostGradesResponse>.Fail(WorkspaceError.NotFound);
        if (assignment.ModeratedGrading && !CanModerate(actor, assignment))
            return WorkspaceResult<PostGradesResponse>.Fail(WorkspaceError.Forbidden, "Under moderated grading only the moderator posts grades.");

        var all = await _repo.GetSubmissionsAsync(assignmentId, false, ct);
        var wanted = request.SubmissionIds is { Count: > 0 } ids ? all.Where(s => ids.Contains(s.Id)).ToList() : all.ToList();
        var now = DateTime.UtcNow;
        var changed = wanted.Where(s => post ? s.Post(now) : s.Hide()).ToList();

        // Posting the grades of every submission lifts anonymity (as in Canvas): graders may now see who wrote what.
        // While some submissions are still ungraded, names stay hidden (use "Reveal names" to lift it early).
        var revealed = false;
        if (post && assignment.NamesHidden && all.Count > 0 && all.All(s => s.PostedAt.HasValue))
        {
            assignment.RevealNames(now);
            revealed = true;
        }

        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog(post ? "PostGrades" : "HideGrades", "Assignment", assignment.Id.ToString(), actorUserId: actor.UserId,
            newValuesJson: $"{{\"count\":{changed.Count},\"namesRevealed\":{(revealed ? "true" : "false")}}}"), ct);
        await RefreshGradebookAsync(assignment.CourseOfferingId, changed.Select(s => s.StudentProfileId), ct);
        return WorkspaceResult<PostGradesResponse>.Success(new PostGradesResponse(post ? changed.Count : 0, post ? 0 : changed.Count, revealed));
    }

    public async Task<WorkspaceResult<bool>> RevealNamesAsync(WorkspaceActor actor, Guid assignmentId, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return WorkspaceResult<bool>.Fail(WorkspaceError.Forbidden);
        var assignment = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (assignment is null) return WorkspaceResult<bool>.Fail(WorkspaceError.NotFound);
        if (!assignment.NamesHidden) return WorkspaceResult<bool>.Success(false);
        if (assignment.ModeratedGrading && !CanModerate(actor, assignment))
            return WorkspaceResult<bool>.Fail(WorkspaceError.Forbidden, "Under moderated grading only the moderator reveals names.");
        assignment.RevealNames(DateTime.UtcNow);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("RevealStudentNames", "Assignment", assignment.Id.ToString(), actorUserId: actor.UserId), ct);
        return WorkspaceResult<bool>.Success(true);
    }

    /// <summary>Plan M8.3: the other members' submissions of a group graded as one (empty otherwise).</summary>
    private async Task<List<AssignmentSubmission>> GroupMatesAsync(AssignmentSubmission submission, CancellationToken ct)
    {
        if (submission.GroupId is not { } groupId || submission.Assignment.GroupSetId is null || submission.Assignment.GradeGroupIndividually) return [];
        return (await _repo.GetSubmissionsAsync(submission.AssignmentId, false, ct))
            .Where(s => s.GroupId == groupId && s.Id != submission.Id && s.Status != SubmissionStatus.Rejected).ToList();
    }

    public async Task<WorkspaceResult<SubmissionDetailResponse>> ModerateAsync(WorkspaceActor actor, Guid submissionId, ModerateRequest request, CancellationToken ct = default)
    {
        static WorkspaceResult<SubmissionDetailResponse> Fail(WorkspaceError e, string? m = null) => WorkspaceResult<SubmissionDetailResponse>.Fail(e, m);
        var submission = await _repo.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null) return Fail(WorkspaceError.NotFound);
        var assignment = submission.Assignment;
        if (!assignment.ModeratedGrading) return Fail(WorkspaceError.Conflict, "This assignment is not moderated.");
        if (!CanModerate(actor, assignment)) return Fail(WorkspaceError.Forbidden, "Only the moderator chooses the final grade.");
        if (submission.Status == SubmissionStatus.Rejected) return Fail(WorkspaceError.Conflict, "This submission was rejected.");

        var provisional = (await _repo.GetProvisionalGradesAsync([submissionId], ct)).ToList();
        decimal marks;
        string? feedback;
        if (request.ProvisionalGradeId is { } chosenId)
        {
            var chosen = provisional.FirstOrDefault(g => g.Id == chosenId);
            if (chosen is null) return Fail(WorkspaceError.NotFound, "That provisional grade does not belong to this submission.");
            foreach (var g in provisional) { if (g.Id == chosenId) g.Select(); else if (g.IsSelected) g.Unselect(); }
            marks = chosen.Marks;
            feedback = request.Feedback ?? chosen.Feedback;
        }
        else if (request.MarksAwarded is { } own)
        {
            foreach (var g in provisional.Where(g => g.IsSelected)) g.Unselect();
            marks = own;
            feedback = request.Feedback;
        }
        else return Fail(WorkspaceError.Invalid, "Choose a provisional grade or enter the final marks.");

        if (marks < 0 || marks > assignment.MaxMarks) return Fail(WorkspaceError.Invalid, $"Marks must be between 0 and {assignment.MaxMarks:0.##}.");

        var wasPosted = submission.PostedAt.HasValue;
        await GradeWithPolicyAsync(submission, marks, feedback, actor.UserId, ct);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ModerateGrade", "AssignmentSubmission", submission.Id.ToString(), actorUserId: actor.UserId,
            newValuesJson: $"{{\"marks\":{marks},\"provisionalGradeId\":\"{request.ProvisionalGradeId}\"}}"), ct);
        if (submission.PostedAt.HasValue || wasPosted)
            await RefreshGradebookAsync(assignment.CourseOfferingId, [submission.StudentProfileId], ct);

        return await GetSubmissionAsync(actor, submissionId, ct);
    }
}
