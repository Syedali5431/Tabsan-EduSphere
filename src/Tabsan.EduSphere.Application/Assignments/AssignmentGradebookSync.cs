using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Plan M3 gradebook rules for assignments (pure, so they can be unit tested). They mirror the quiz rules:
/// <list type="number">
///   <item>The component is the active result component named "Assignment" (or else "Assignments").
///         Without one, nothing is synced.</item>
///   <item>Only posted grades count — a hidden grade never reaches the gradebook.</item>
///   <item>Assignments are combined by points (sum of marks / sum of max marks), scaled to the component's weightage.
///         An assignment without a posted grade is left out, not counted as zero.</item>
/// </list>
/// Hiding every grade again leaves the last synced draft row as it was (it is not deleted).
/// </summary>
public static class AssignmentGradebookCalculator
{
    public static readonly IReadOnlyList<string> ComponentNames = ["Assignment", "Assignments"];

    public static ResultComponentRule? FindComponent(IEnumerable<ResultComponentRule> activeRules)
    {
        var rules = activeRules.ToList();
        return ComponentNames
            .Select(name => rules.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(r => r is not null);
    }

    public static (decimal Marks, decimal Max)? Calculate(IEnumerable<(decimal Marks, decimal MaxMarks)> posted, decimal componentMax)
    {
        if (componentMax <= 0) return null;
        var list = posted.Where(p => p.MaxMarks > 0).ToList();
        if (list.Count == 0) return null;
        var earned = list.Sum(p => Math.Clamp(p.Marks, 0, p.MaxMarks));
        var possible = list.Sum(p => p.MaxMarks);
        var marks = Math.Round(earned / possible * componentMax, 2, MidpointRounding.AwayFromZero);
        return (Math.Min(marks, componentMax), componentMax);
    }
}

/// <inheritdoc />
public sealed class AssignmentGradebookSync : IAssignmentGradebookSync
{
    private readonly ISubmissionWorkspaceRepository _submissions;
    private readonly IResultRepository _results;
    private readonly IResultService _resultService;
    private readonly ILogger<AssignmentGradebookSync>? _logger;

    public AssignmentGradebookSync(ISubmissionWorkspaceRepository submissions, IResultRepository results, IResultService resultService,
        ILogger<AssignmentGradebookSync>? logger = null)
    {
        _submissions = submissions;
        _results = results;
        _resultService = resultService;
        _logger = logger;
    }

    public async Task<SystemResultSyncOutcome?> RefreshAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default)
    {
        try
        {
            var institutionType = await _results.GetInstitutionTypeForOfferingAsync(courseOfferingId, ct);
            var rule = AssignmentGradebookCalculator.FindComponent(await _results.GetActiveComponentRulesAsync(institutionType, ct));
            if (rule is null) return SystemResultSyncOutcome.SkippedNotConfigured;

            var posted = await _submissions.GetPostedGradesAsync(courseOfferingId, studentProfileId, ct);
            var score = AssignmentGradebookCalculator.Calculate(posted.Select(p => (p.Marks, p.MaxMarks)), rule.Weightage);
            if (score is null) return null;

            var outcome = await _resultService.SyncSystemComponentAsync(studentProfileId, courseOfferingId, rule.Name,
                score.Value.Marks, score.Value.Max, ResultSource.AssignmentSync, ct);
            if (outcome is SystemResultSyncOutcome.SkippedPublished)
                _logger?.LogInformation("Assignment gradebook sync skipped for student {StudentProfileId} in offering {CourseOfferingId}: the {Component} result is published.",
                    studentProfileId, courseOfferingId, rule.Name);
            return outcome;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Assignment gradebook sync failed for student {StudentProfileId} in offering {CourseOfferingId}.", studentProfileId, courseOfferingId);
            return null;
        }
    }
}
