using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Quizzes;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.Application.Outcomes;

/// <summary>
/// Plan M6.3 — turns graded work into outcome evidence. Only grades students can see count: posted assignment grades
/// (with their rubric criteria; tool scores arrive as assignment grades) and graded answers of finished quiz attempts.
/// The rows of the affected students are rebuilt each time, so regrades, re-posts, hidden grades and new alignments
/// are all reflected.
/// </summary>
public sealed class OutcomeResultSync : IOutcomeResultSync
{
    private readonly IOutcomeRepository _repo;
    private readonly ILogger<OutcomeResultSync>? _logger;

    public OutcomeResultSync(IOutcomeRepository repo, ILogger<OutcomeResultSync>? logger = null)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task RefreshAsync(Guid courseOfferingId, IReadOnlyCollection<Guid>? studentProfileIds, OutcomeSourceType? source, CancellationToken ct = default)
    {
        try
        {
            var existing = await _repo.GetResultsAsync(courseOfferingId, studentProfileIds, null, source, ct);
            if (existing.Count == 0 && !await _repo.AnyAlignmentsAsync(ct)) return; // nothing aligned anywhere yet

            var desired = await BuildAsync(courseOfferingId, studentProfileIds, source, ct);
            var current = existing.ToDictionary(r => (r.StudentProfileId, r.AlignmentId, r.SourceId));
            foreach (var row in desired)
            {
                if (current.Remove((row.StudentProfileId, row.AlignmentId, row.SourceId), out var old))
                {
                    if (!old.SameAs(row)) old.Refresh(row.Percent, row.Score, row.AssessedAt);
                }
                else _repo.Add(row);
            }
            foreach (var stale in current.Values) _repo.Remove(stale);
            await _repo.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Outcome results could not be refreshed for offering {CourseOfferingId}.", courseOfferingId);
        }
    }

    /// <summary>The evidence rows the graded work justifies.</summary>
    private async Task<List<StudentOutcomeResult>> BuildAsync(Guid offeringId, IReadOnlyCollection<Guid>? studentIds, OutcomeSourceType? source, CancellationToken ct)
    {
        var assignments = source is null or OutcomeSourceType.AssignmentSubmission
            ? await _repo.GetAssignmentEvidenceAsync(offeringId, studentIds, ct) : [];
        var rubric = assignments.Count == 0 ? [] : await _repo.GetRubricEvidenceAsync(assignments.Select(a => a.SubmissionId).ToList(), ct);
        var quiz = source is null or OutcomeSourceType.QuizAttempt
            ? await _repo.GetQuizEvidenceAsync(offeringId, studentIds, ct) : [];

        var targets = new List<(OutcomeTargetType, Guid)>();
        targets.AddRange(assignments.Select(a => a.AssignmentId).Distinct().Select(id => (OutcomeTargetType.Assignment, id)));
        targets.AddRange(rubric.Select(r => r.CriterionId).Distinct().Select(id => (OutcomeTargetType.RubricCriterion, id)));
        foreach (var q in quiz.Select(q => q.QuestionId).Distinct())
        {
            targets.Add((OutcomeTargetType.QuizQuestion, q));
            targets.Add((OutcomeTargetType.BankQuestion, q));
        }
        if (targets.Count == 0) return [];

        var alignments = await _repo.GetAlignmentsAsync(targets, ct);
        if (alignments.Count == 0) return [];
        var outcomes = (await _repo.GetManyAsync(alignments.Select(a => a.OutcomeId).Distinct().ToList(), ct))
            .Where(o => o.MaxPoints > 0).ToDictionary(o => o.Id);

        var rows = new List<StudentOutcomeResult>();
        void AddRow(OutcomeAlignment alignment, Guid student, OutcomeSourceType kind, Guid sourceId, decimal percent, DateTime at)
        {
            if (!outcomes.TryGetValue(alignment.OutcomeId, out var outcome)) return;
            rows.Add(new StudentOutcomeResult(student, outcome.Id, offeringId, alignment.Id, kind, sourceId, percent,
                MasteryCalculator.ToScale(percent, outcome.MaxPoints), at));
        }

        var byTarget = alignments.ToLookup(a => (a.TargetType, a.TargetId));
        var submissions = assignments.ToDictionary(a => a.SubmissionId);
        foreach (var a in assignments)
            foreach (var alignment in byTarget[(OutcomeTargetType.Assignment, a.AssignmentId)])
                AddRow(alignment, a.StudentProfileId, OutcomeSourceType.AssignmentSubmission, a.SubmissionId, MasteryCalculator.Percent(a.Marks, a.MaxMarks), a.AssessedAt);
        foreach (var r in rubric)
        {
            if (!submissions.TryGetValue(r.SubmissionId, out var sub)) continue;
            foreach (var alignment in byTarget[(OutcomeTargetType.RubricCriterion, r.CriterionId)])
                AddRow(alignment, sub.StudentProfileId, OutcomeSourceType.AssignmentSubmission, r.SubmissionId, MasteryCalculator.Percent(r.Points, r.MaxPoints), sub.AssessedAt);
        }
        foreach (var q in quiz)
            foreach (var alignment in byTarget[(OutcomeTargetType.QuizQuestion, q.QuestionId)].Concat(byTarget[(OutcomeTargetType.BankQuestion, q.QuestionId)]))
                AddRow(alignment, q.StudentProfileId, OutcomeSourceType.QuizAttempt, q.AttemptId, MasteryCalculator.Percent(q.MarksAwarded, q.MaxMarks), q.AssessedAt);

        // One row per student, alignment and piece of work.
        return rows.GroupBy(r => (r.StudentProfileId, r.AlignmentId, r.SourceId)).Select(g => g.First()).ToList();
    }
}

/// <summary>Plan M6.3: every assignment gradebook refresh (grade, post, hide, tool score) also refreshes outcome evidence.</summary>
public sealed class OutcomeAwareAssignmentGradebookSync : IAssignmentGradebookSync
{
    private readonly AssignmentGradebookSync _inner;
    private readonly IOutcomeResultSync _outcomes;

    public OutcomeAwareAssignmentGradebookSync(AssignmentGradebookSync inner, IOutcomeResultSync outcomes)
    {
        _inner = inner;
        _outcomes = outcomes;
    }

    public async Task<SystemResultSyncOutcome?> RefreshAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default)
    {
        var result = await _inner.RefreshAsync(courseOfferingId, studentProfileId, ct);
        await _outcomes.RefreshAsync(courseOfferingId, [studentProfileId], OutcomeSourceType.AssignmentSubmission, ct);
        return result;
    }
}

/// <summary>Plan M6.3: every quiz gradebook refresh (finish, time-out, grade, regrade) also refreshes outcome evidence.</summary>
public sealed class OutcomeAwareQuizGradebookSync : IQuizGradebookSync
{
    private readonly QuizGradebookSync _inner;
    private readonly IOutcomeResultSync _outcomes;

    public OutcomeAwareQuizGradebookSync(QuizGradebookSync inner, IOutcomeResultSync outcomes)
    {
        _inner = inner;
        _outcomes = outcomes;
    }

    public async Task<SystemResultSyncOutcome?> RefreshAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default)
    {
        var result = await _inner.RefreshAsync(courseOfferingId, studentProfileId, ct);
        await _outcomes.RefreshAsync(courseOfferingId, [studentProfileId], OutcomeSourceType.QuizAttempt, ct);
        return result;
    }
}
