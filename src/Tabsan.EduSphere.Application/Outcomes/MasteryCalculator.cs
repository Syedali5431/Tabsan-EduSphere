using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.Application.Outcomes;

/// <summary>A student's mastery of one outcome.</summary>
public sealed record MasteryValue(decimal? Score, bool Mastered, int Count, int MasteryCount);

/// <summary>
/// Plan M6.3 mastery rules (pure, so they can be unit tested). Scores are on the outcome's rating scale.
/// <list type="bullet">
///   <item>Highest / Latest / Average: as named.</item>
///   <item>Decaying average (weight w%): latest × w% + average of the earlier scores × (100 − w)%; a single score counts as is.</item>
///   <item>n times: mastered only when at least n scores reach mastery; the score is then the average of those scores,
///         otherwise the average of all scores (not mastered, whatever it is).</item>
/// </list>
/// Otherwise an outcome is mastered when the score reaches the mastery points.
/// </summary>
public static class MasteryCalculator
{
    public static MasteryValue Calculate(IEnumerable<(decimal Score, DateTime At)> evidence, MasteryCalculationMethod method, int? parameter, decimal masteryPoints)
    {
        var list = evidence.OrderBy(e => e.At).ToList();
        if (list.Count == 0) return new MasteryValue(null, false, 0, 0);
        var masteryCount = list.Count(e => e.Score >= masteryPoints);
        decimal score;
        switch (method)
        {
            case MasteryCalculationMethod.Highest:
                score = list.Max(e => e.Score);
                break;
            case MasteryCalculationMethod.Latest:
                score = list[^1].Score;
                break;
            case MasteryCalculationMethod.Average:
                score = list.Average(e => e.Score);
                break;
            case MasteryCalculationMethod.NMastery:
            {
                var n = Math.Clamp(parameter ?? 5, 1, 10);
                if (masteryCount >= n)
                {
                    var mastered = list.Where(e => e.Score >= masteryPoints).Average(e => e.Score);
                    return new MasteryValue(Round(mastered), true, list.Count, masteryCount);
                }
                return new MasteryValue(Round(list.Average(e => e.Score)), false, list.Count, masteryCount);
            }
            default: // DecayingAverage
            {
                if (list.Count == 1) { score = list[0].Score; break; }
                var weight = Math.Clamp(parameter ?? 65, 1, 99) / 100m;
                score = list[^1].Score * weight + list.Take(list.Count - 1).Average(e => e.Score) * (1 - weight);
                break;
            }
        }
        score = Round(score);
        return new MasteryValue(score, score >= masteryPoints, list.Count, masteryCount);
    }

    /// <summary>The highest rating level the score reaches.</summary>
    public static string? RatingFor(decimal? score, IEnumerable<(string Description, decimal Points)> ratings)
        => score is null ? null : ratings.Where(r => r.Points <= score).OrderByDescending(r => r.Points).Select(r => r.Description).FirstOrDefault();

    /// <summary>"mastered", "near" (at or above the level just below mastery), "below", or "none" without evidence.</summary>
    public static string Status(MasteryValue value, decimal masteryPoints, IEnumerable<decimal> ratingPoints)
    {
        if (value.Score is not { } score) return "none";
        if (value.Mastered) return "mastered";
        var below = ratingPoints.Where(p => p < masteryPoints).DefaultIfEmpty(-1).Max();
        return below >= 0 && score >= below && below > 0 ? "near" : "below";
    }

    /// <summary>Converts an earned share (0..1) to the outcome's scale.</summary>
    public static decimal ToScale(decimal percent, decimal maxPoints) => Round(Math.Clamp(percent, 0, 1) * maxPoints);

    public static decimal Percent(decimal earned, decimal possible) => possible <= 0 ? 0 : Math.Round(Math.Clamp(earned / possible, 0, 1), 4, MidpointRounding.AwayFromZero);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
