using System.Text.Json;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>One piece of work to review: a student's submission, or one per group for group work.</summary>
public sealed record PeerWork(Guid SubmissionId, Guid OwnerKey);

/// <summary>A reviewer and the work-owner key they must not review (themselves or their group).</summary>
public sealed record PeerReviewer(Guid StudentProfileId, Guid OwnerKey);

/// <summary>
/// Plan M8.4 allocation (pure, so it can be unit tested): each reviewer gets <c>count</c> pieces of work that are not their
/// own (nor their group's). Reviews are handed out one round at a time, least-reviewed work first (ties random), and then
/// moved from over-reviewed to under-reviewed work wherever a reviewer can take the other piece — for individual work this
/// always ends with every piece reviewed within one review of every other.
/// </summary>
public static class PeerReviewAllocator
{
    public static IReadOnlyList<(Guid SubmissionId, Guid ReviewerId)> Allocate(IReadOnlyList<PeerWork> work, IReadOnlyList<PeerReviewer> reviewers, int count, Random random)
    {
        var load = work.ToDictionary(w => w.SubmissionId, _ => 0);
        var owner = work.ToDictionary(w => w.SubmissionId, w => w.OwnerKey);
        var assigned = reviewers.ToDictionary(r => r.StudentProfileId, _ => new HashSet<Guid>());
        var order = reviewers.OrderBy(_ => random.Next()).ToList();

        for (var round = 0; round < count; round++)
            foreach (var reviewer in order)
            {
                var pick = work.Where(w => w.OwnerKey != reviewer.OwnerKey && !assigned[reviewer.StudentProfileId].Contains(w.SubmissionId))
                    .OrderBy(w => load[w.SubmissionId]).ThenBy(_ => random.Next()).FirstOrDefault();
                if (pick is null) continue;
                assigned[reviewer.StudentProfileId].Add(pick.SubmissionId);
                load[pick.SubmissionId]++;
            }

        // Rebalance: every move lowers the spread of the loads, so this ends.
        var moved = true;
        while (moved)
        {
            moved = false;
            foreach (var heavy in work.OrderByDescending(w => load[w.SubmissionId]))
            {
                foreach (var light in work.Where(w => load[heavy.SubmissionId] - load[w.SubmissionId] >= 2).OrderBy(w => load[w.SubmissionId]))
                {
                    var mover = order.FirstOrDefault(r => assigned[r.StudentProfileId].Contains(heavy.SubmissionId)
                        && !assigned[r.StudentProfileId].Contains(light.SubmissionId) && r.OwnerKey != owner[light.SubmissionId]);
                    if (mover is null) continue;
                    assigned[mover.StudentProfileId].Remove(heavy.SubmissionId);
                    assigned[mover.StudentProfileId].Add(light.SubmissionId);
                    load[heavy.SubmissionId]--;
                    load[light.SubmissionId]++;
                    moved = true;
                    break;
                }
                if (moved) break;
            }
        }

        return order.SelectMany(r => assigned[r.StudentProfileId].Select(s => (s, r.StudentProfileId))).ToList();
    }
}

/// <summary>
/// Plan M8.4 — peer review: allocation (by the teacher, or automatically after the due date), the reviewer's task
/// (the work, the rubric, their review), the reviews a student received, and the teacher's overview with the average
/// score to use in grading.
/// </summary>
public sealed class PeerReviewService
{
    private readonly IAssignmentPolicyRepository _repo;
    private readonly ISubmissionWorkspaceRepository _submissions;
    private readonly IRubricRepository _rubrics;
    private readonly IAuditService _audit;

    public PeerReviewService(IAssignmentPolicyRepository repo, ISubmissionWorkspaceRepository submissions, IRubricRepository rubrics, IAuditService audit)
    {
        _repo = repo;
        _submissions = submissions;
        _rubrics = rubrics;
        _audit = audit;
    }

    /// <summary>Allocates reviews among students who handed in work. Returns how many reviews were created (0 when already allocated).</summary>
    public async Task<PolicyResult<int>> AllocateAsync(Guid assignmentId, Guid? actorUserId, Random? random = null, CancellationToken ct = default)
    {
        var a = await _submissions.GetAssignmentAsync(assignmentId, ct);
        if (a is null) return PolicyResult<int>.Fail(404, "Assignment not found.");
        if (a.PeerReviewCount <= 0) return PolicyResult<int>.Fail(400, "Peer review is not turned on for this assignment.");
        if ((await _repo.GetPeerReviewsAsync(assignmentId, ct)).Count > 0) return PolicyResult<int>.Fail(409, "Peer reviews have already been assigned.");

        var handedIn = (await _submissions.GetSubmissionsAsync(assignmentId, false, ct))
            .Where(s => s.CurrentAttemptNumber > 0 && s.Status != SubmissionStatus.Rejected && !s.IsMissing).ToList();
        // Group work is reviewed once per group; every member reviews.
        var work = handedIn.GroupBy(s => s.GroupId ?? s.StudentProfileId).Select(g => new PeerWork(g.OrderBy(s => s.SubmittedAt).First().Id, g.Key)).ToList();
        var reviewers = handedIn.Select(s => new PeerReviewer(s.StudentProfileId, s.GroupId ?? s.StudentProfileId)).ToList();
        if (work.Count < 2) return PolicyResult<int>.Fail(409, "At least two students (or groups) must hand in work before reviews can be assigned.");

        var pairs = PeerReviewAllocator.Allocate(work, reviewers, Math.Min(a.PeerReviewCount, work.Count - 1), random ?? Random.Shared);
        foreach (var (submissionId, reviewerId) in pairs) _repo.Add(new PeerReview(assignmentId, submissionId, reviewerId));
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("PeerReviewsAllocated", "Assignment", assignmentId.ToString(), actorUserId: actorUserId,
            newValuesJson: $"{{\"reviews\":{pairs.Count},\"work\":{work.Count}}}"), ct);
        return PolicyResult<int>.Success(pairs.Count);
    }

    /// <summary>Allocates reviews for every peer-review assignment past its due date that has none yet.</summary>
    public async Task<int> AllocateDueAsync(DateTime now, CancellationToken ct = default)
    {
        var total = 0;
        foreach (var a in await _repo.GetAssignmentsAwaitingPeerAllocationAsync(now, ct))
        {
            var result = await AllocateAsync(a.Id, null, null, ct);
            if (result.Ok) total += result.Value;
        }
        return total;
    }

    public async Task<PeerReviewOverviewDto?> OverviewAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var a = await _submissions.GetAssignmentAsync(assignmentId, ct);
        if (a is null) return null;
        var reviews = await _repo.GetPeerReviewsAsync(assignmentId, ct);
        var submissions = (await _submissions.GetSubmissionsAsync(assignmentId, false, ct)).ToDictionary(s => s.Id);
        var names = await _submissions.GetStudentNamesAsync(reviews.Select(r => r.ReviewerStudentProfileId)
            .Concat(submissions.Values.Select(s => s.StudentProfileId)).Distinct().ToList(), ct);
        string Name(Guid id) => names.TryGetValue(id, out var n) ? n.Name : "Student";
        return new PeerReviewOverviewDto(a.Id, a.PeerReviewCount, a.PeerReviewDueDate, a.PeerReviewAnonymous,
            reviews.Count, reviews.Count(r => r.Status == PeerReviewStatus.Completed),
            reviews.OrderBy(r => r.Status).Select(r => new PeerReviewSummaryDto(r.Id, r.SubmissionId,
                submissions.TryGetValue(r.SubmissionId, out var s) ? Name(s.StudentProfileId) : "Student", Name(r.ReviewerStudentProfileId),
                r.Status.ToString(), r.Score, r.CompletedAt)).ToList());
    }

    /// <summary>Completed reviews of a submission (and of the rest of its group), for the grader.</summary>
    public async Task<(decimal? Average, IReadOnlyList<PeerReviewSummaryDto> Reviews)> ForSubmissionAsync(Guid submissionId, CancellationToken ct = default)
    {
        var submission = await _submissions.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null) return (null, []);
        var ids = new HashSet<Guid> { submissionId };
        if (submission.GroupId is { } groupId)
            foreach (var s in await _submissions.GetSubmissionsAsync(submission.AssignmentId, false, ct))
                if (s.GroupId == groupId) ids.Add(s.Id);
        var reviews = (await _repo.GetPeerReviewsAsync(submission.AssignmentId, ct)).Where(r => ids.Contains(r.SubmissionId)).ToList();
        var names = await _submissions.GetStudentNamesAsync(reviews.Select(r => r.ReviewerStudentProfileId).Distinct().ToList(), ct);
        var scored = reviews.Where(r => r.Status == PeerReviewStatus.Completed && r.Score is not null).Select(r => r.Score!.Value).ToList();
        return (scored.Count == 0 ? null : Math.Round(scored.Average(), 2, MidpointRounding.AwayFromZero),
            reviews.Select(r => new PeerReviewSummaryDto(r.Id, r.SubmissionId, "", names.TryGetValue(r.ReviewerStudentProfileId, out var n) ? n.Name : "Student",
                r.Status.ToString(), r.Score, r.CompletedAt)).ToList());
    }

    /// <summary>The reviewer's task. Null when the review does not belong to this student.</summary>
    public async Task<PeerReviewTaskDto?> TaskAsync(Guid peerReviewId, Guid reviewerStudentProfileId, CancellationToken ct = default)
    {
        var review = await _repo.GetPeerReviewAsync(peerReviewId, ct);
        if (review is null || review.ReviewerStudentProfileId != reviewerStudentProfileId) return null;
        var submission = await _submissions.GetSubmissionAsync(review.SubmissionId, true, ct);
        if (submission is null) return null;
        var a = submission.Assignment;
        var latest = submission.Attempts.OrderByDescending(x => x.AttemptNumber).FirstOrDefault();
        var label = "Peer submission";
        if (!a.PeerReviewAnonymous)
            label = (await _submissions.GetStudentNamesAsync([submission.StudentProfileId], ct)).TryGetValue(submission.StudentProfileId, out var n) ? n.Name : label;

        var chosen = string.IsNullOrWhiteSpace(review.RubricJson) ? new Dictionary<Guid, Guid>()
            : (JsonSerializer.Deserialize<List<PeerRubricChoice>>(review.RubricJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [])
                .ToDictionary(c => c.CriterionId, c => c.LevelId);
        var rubric = await _rubrics.GetByAssignmentAsync(a.Id, ct);
        var criteria = rubric is null ? [] : rubric.Criteria.OrderBy(c => c.DisplayOrder).Select(c => new PeerRubricCriterionDto(c.Id, c.Name, c.MaxPoints,
            c.Levels.OrderBy(l => l.DisplayOrder).Select(l => new PeerRubricLevelDto(l.Id, l.Label, l.PointsAwarded)).ToList(),
            chosen.TryGetValue(c.Id, out var levelId) ? levelId : null)).ToList();

        return new PeerReviewTaskDto(review.Id, a.Id, a.Title, label, a.MaxMarks, a.PeerReviewDueDate, review.Status.ToString(), latest?.TextContent,
            (latest?.Files ?? []).Where(f => f.StorageKey is not null).Select(f => new PeerFileDto(f.Id, f.FileName, f.ContentType, f.SizeBytes)).ToList(),
            criteria, review.Score, review.Comment);
    }

    public async Task<PolicyResult<PeerReviewTaskDto>> SubmitAsync(Guid peerReviewId, Guid reviewerStudentProfileId, SubmitPeerReviewRequest r, CancellationToken ct = default)
    {
        var review = await _repo.GetPeerReviewAsync(peerReviewId, ct);
        if (review is null || review.ReviewerStudentProfileId != reviewerStudentProfileId) return PolicyResult<PeerReviewTaskDto>.Fail(404, "Review not found.");
        var submission = await _submissions.GetSubmissionAsync(review.SubmissionId, false, ct);
        if (submission is null) return PolicyResult<PeerReviewTaskDto>.Fail(404, "Review not found.");
        var a = submission.Assignment;

        decimal? score = r.Score;
        string? rubricJson = null;
        var rubric = await _rubrics.GetByAssignmentAsync(a.Id, ct);
        if (rubric is not null && r.Rubric is { Count: > 0 })
        {
            var criteria = rubric.Criteria.ToDictionary(c => c.Id);
            decimal total = 0;
            foreach (var choice in r.Rubric)
            {
                if (!criteria.TryGetValue(choice.CriterionId, out var criterion)) return PolicyResult<PeerReviewTaskDto>.Fail(400, "A criterion does not belong to this rubric.");
                var level = criterion.Levels.FirstOrDefault(l => l.Id == choice.LevelId);
                if (level is null) return PolicyResult<PeerReviewTaskDto>.Fail(400, $"That level does not belong to \"{criterion.Name}\".");
                total += level.PointsAwarded;
            }
            var max = rubric.Criteria.Sum(c => c.MaxPoints);
            score = max > 0 ? Math.Round(total / max * a.MaxMarks, 2, MidpointRounding.AwayFromZero) : null;
            rubricJson = JsonSerializer.Serialize(r.Rubric, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        if (score is { } s && (s < 0 || s > a.MaxMarks)) return PolicyResult<PeerReviewTaskDto>.Fail(400, $"The score must be between 0 and {a.MaxMarks:0.##}.");
        try { review.Complete(score, r.Comment, rubricJson, DateTime.UtcNow); }
        catch (ArgumentException ex) { return PolicyResult<PeerReviewTaskDto>.Fail(400, ex.Message); }
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("PeerReviewCompleted", "PeerReview", review.Id.ToString(),
            newValuesJson: $"{{\"score\":{(score?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null")}}}"), ct);
        return PolicyResult<PeerReviewTaskDto>.Success((await TaskAsync(peerReviewId, reviewerStudentProfileId, ct))!);
    }

    public async Task<Guid?> OfferingOfReviewAsync(Guid peerReviewId, CancellationToken ct = default)
        => await _repo.GetPeerReviewAsync(peerReviewId, ct) is { } r ? (await _submissions.GetAssignmentAsync(r.AssignmentId, ct))?.CourseOfferingId : null;
}
