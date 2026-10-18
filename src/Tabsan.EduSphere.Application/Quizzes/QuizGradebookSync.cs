using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes;

/// <summary>One finished attempt as the gradebook sees it. <see cref="Score"/> is null while marks are pending.</summary>
public sealed record QuizAttemptScore(Guid QuizId, decimal? Score, decimal MaxMarks);

/// <summary>The quiz component for one student: marks out of the component's weight.</summary>
public sealed record QuizComponentScore(decimal MarksObtained, decimal MaxMarks, int QuizzesCounted);

/// <summary>
/// Plan M2 gradebook rules for quizzes (pure, so they can be unit tested):
/// <list type="number">
///   <item>The component is the active result component named "Quiz" (or else "Quizzes") for the
///         offering's institution type. Without one, nothing is synced.</item>
///   <item>Only finished attempts with a score count. An attempt whose essays / file uploads still
///         wait for marks has no score, so it never posts a partial result.</item>
///   <item>Per quiz, the best attempt counts (highest percentage).</item>
///   <item>Quizzes are combined by points: sum of best scores / sum of their max marks. A quiz the
///         student has no scored attempt on is left out (not counted as zero); quizzes worth 0 marks are ignored.</item>
///   <item>The result is scaled to the component's weightage, so MaxMarks = weightage (2 dp).</item>
/// </list>
/// </summary>
public static class QuizGradebookCalculator
{
    /// <summary>Component names that receive quiz scores, in order of preference.</summary>
    public static readonly IReadOnlyList<string> ComponentNames = ["Quiz", "Quizzes"];

    public static ResultComponentRule? FindComponent(IEnumerable<ResultComponentRule> activeRules)
    {
        var rules = activeRules.ToList();
        return ComponentNames
            .Select(name => rules.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(r => r is not null);
    }

    public static QuizComponentScore? Calculate(IEnumerable<QuizAttemptScore> attempts, decimal componentMax)
    {
        if (componentMax <= 0) return null;

        var best = attempts
            .Where(a => a.Score.HasValue && a.MaxMarks > 0)
            .GroupBy(a => a.QuizId)
            .Select(g => g.OrderByDescending(a => a.Score!.Value / a.MaxMarks).ThenByDescending(a => a.Score).First())
            .ToList();
        if (best.Count == 0) return null;

        var earned = best.Sum(a => Math.Clamp(a.Score!.Value, 0, a.MaxMarks));
        var possible = best.Sum(a => a.MaxMarks);
        var marks = Math.Round(earned / possible * componentMax, 2, MidpointRounding.AwayFromZero);
        return new QuizComponentScore(Math.Min(marks, componentMax), componentMax, best.Count);
    }

    /// <summary>Max marks of an attempt: its item snapshot, or the quiz's questions for pre-M2 attempts.</summary>
    public static QuizAttemptScore ToScore(QuizAttempt attempt)
    {
        var max = attempt.Items.Count > 0
            ? attempt.Items.Sum(i => i.Marks)
            : attempt.Quiz?.Questions.Sum(q => q.Marks) ?? 0;
        return new QuizAttemptScore(attempt.QuizId, attempt.Status == AttemptStatus.InProgress ? null : attempt.TotalScore, max);
    }
}

/// <inheritdoc />
public sealed class QuizGradebookSync : IQuizGradebookSync
{
    private readonly IQuizRepository _quizzes;
    private readonly IResultRepository _results;
    private readonly IResultService _resultService;
    private readonly ILogger<QuizGradebookSync>? _logger;

    public QuizGradebookSync(IQuizRepository quizzes, IResultRepository results, IResultService resultService,
        ILogger<QuizGradebookSync>? logger = null)
    {
        _quizzes = quizzes;
        _results = results;
        _resultService = resultService;
        _logger = logger;
    }

    public async Task<SystemResultSyncOutcome?> RefreshAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default)
    {
        try
        {
            var institutionType = await _results.GetInstitutionTypeForOfferingAsync(courseOfferingId, ct);
            var rule = QuizGradebookCalculator.FindComponent(await _results.GetActiveComponentRulesAsync(institutionType, ct));
            if (rule is null) return SystemResultSyncOutcome.SkippedNotConfigured;

            var attempts = await _quizzes.GetFinishedAttemptsForGradebookAsync(courseOfferingId, studentProfileId, ct);
            var score = QuizGradebookCalculator.Calculate(attempts.Select(QuizGradebookCalculator.ToScore), rule.Weightage);
            if (score is null) return null; // nothing graded yet

            var outcome = await _resultService.SyncSystemComponentAsync(studentProfileId, courseOfferingId, rule.Name,
                score.MarksObtained, score.MaxMarks, ResultSource.QuizSync, ct);
            if (outcome is SystemResultSyncOutcome.SkippedPublished)
                _logger?.LogInformation("Quiz gradebook sync skipped for student {StudentProfileId} in offering {CourseOfferingId}: the {Component} result is published (new value {Marks}/{Max} needs an admin correction).",
                    studentProfileId, courseOfferingId, rule.Name, score.MarksObtained, score.MaxMarks);
            return outcome;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Quiz gradebook sync failed for student {StudentProfileId} in offering {CourseOfferingId}.",
                studentProfileId, courseOfferingId);
            return null;
        }
    }
}
