using Tabsan.EduSphere.Application.Outcomes;
using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M6 — mastery calculation methods, rating/status rules and the outcome rating scale.</summary>
public class OutcomeMasteryTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    private static IEnumerable<(decimal, DateTime)> Scores(params decimal[] scores) => scores.Select((s, i) => (s, T0.AddDays(i)));

    [Fact]
    public void NoEvidence_HasNoScore()
    {
        var v = MasteryCalculator.Calculate([], MasteryCalculationMethod.Highest, null, 3);
        Assert.Null(v.Score);
        Assert.False(v.Mastered);
        Assert.Equal("none", MasteryCalculator.Status(v, 3, [4, 3, 2, 1, 0]));
    }

    [Theory]
    [InlineData(MasteryCalculationMethod.Highest, 3.5)]
    [InlineData(MasteryCalculationMethod.Latest, 2)]
    [InlineData(MasteryCalculationMethod.Average, 2.5)]
    public void SimpleMethods(MasteryCalculationMethod method, double expected)
        => Assert.Equal((decimal)expected, MasteryCalculator.Calculate(Scores(2, 3.5m, 2), method, null, 3).Score);

    [Fact]
    public void Latest_UsesTheAssessmentDate_NotTheListOrder()
    {
        var evidence = new[] { (4m, T0.AddDays(5)), (1m, T0) };
        Assert.Equal(4m, MasteryCalculator.Calculate(evidence, MasteryCalculationMethod.Latest, null, 3).Score);
    }

    [Fact]
    public void DecayingAverage_WeightsTheLatestScore()
    {
        // 4 × 65% + average(3, 1) × 35% = 2.6 + 0.7.
        var v = MasteryCalculator.Calculate(Scores(3, 1, 4), MasteryCalculationMethod.DecayingAverage, 65, 3);
        Assert.Equal(3.3m, v.Score);
        Assert.True(v.Mastered);
        Assert.Equal(2.5m, MasteryCalculator.Calculate(Scores(2.5m), MasteryCalculationMethod.DecayingAverage, 65, 3).Score);
        // Weight 50% → 2 × 0.5 + 4 × 0.5.
        Assert.Equal(3m, MasteryCalculator.Calculate(Scores(4, 2), MasteryCalculationMethod.DecayingAverage, 50, 3).Score);
    }

    [Fact]
    public void NMastery_NeedsTheNumberOfMasteryScores()
    {
        var enough = MasteryCalculator.Calculate(Scores(3.5m, 2, 3.2m), MasteryCalculationMethod.NMastery, 2, 3);
        Assert.True(enough.Mastered);
        Assert.Equal(3.35m, enough.Score); // average of the mastery scores
        Assert.Equal(2, enough.MasteryCount);

        var notYet = MasteryCalculator.Calculate(Scores(4, 2), MasteryCalculationMethod.NMastery, 2, 3);
        Assert.False(notYet.Mastered);
        Assert.Equal(3m, notYet.Score); // the average — even at the mastery points it is not mastered yet
        Assert.Equal("near", MasteryCalculator.Status(notYet, 3, [4, 3, 2, 1, 0]));
    }

    [Fact]
    public void RatingAndStatus_FollowTheScale()
    {
        (string, decimal)[] scale = [("Exceeds", 4), ("Meets", 3), ("Near", 2), ("Below", 1), ("No evidence", 0)];
        Assert.Equal("Meets", MasteryCalculator.RatingFor(3.2m, scale));
        Assert.Equal("Near", MasteryCalculator.RatingFor(2.99m, scale));
        Assert.Equal("No evidence", MasteryCalculator.RatingFor(0.5m, scale));
        Assert.Null(MasteryCalculator.RatingFor(null, scale));

        MasteryValue V(decimal s) => MasteryCalculator.Calculate(Scores(s), MasteryCalculationMethod.Latest, null, 3);
        Assert.Equal("mastered", MasteryCalculator.Status(V(3), 3, scale.Select(x => x.Item2)));
        Assert.Equal("near", MasteryCalculator.Status(V(2), 3, scale.Select(x => x.Item2)));
        Assert.Equal("below", MasteryCalculator.Status(V(1.9m), 3, scale.Select(x => x.Item2)));
        // A two-level scale has no "near".
        Assert.Equal("below", MasteryCalculator.Status(V(2), 3, [3, 0]));
    }

    [Fact]
    public void Shares_AreClampedAndScaled()
    {
        Assert.Equal(0.8m, MasteryCalculator.Percent(16, 20));
        Assert.Equal(1m, MasteryCalculator.Percent(25, 20));
        Assert.Equal(0m, MasteryCalculator.Percent(5, 0));
        Assert.Equal(3.2m, MasteryCalculator.ToScale(0.8m, 4));
        Assert.Equal(4m, MasteryCalculator.ToScale(1.5m, 4));
    }

    [Fact]
    public void Outcome_LevelNeedsItsScope_AndGetsTheDefaultScale()
    {
        Assert.Throws<ArgumentException>(() => new Outcome(null, OutcomeLevel.Program, null, null, "PLO1", "x", Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Outcome(null, OutcomeLevel.Course, null, null, "CLO1", "x", Guid.NewGuid()));
        var course = new Outcome(null, OutcomeLevel.Course, Guid.NewGuid(), Guid.NewGuid(), " CLO1 ", " Title ", Guid.NewGuid());
        Assert.Null(course.ProgramId); // only the course is kept for a course outcome
        Assert.Equal("CLO1", course.Code);
        course.UseDefaultScale();
        Assert.Equal(4m, course.MaxPoints);
        Assert.Equal(3m, course.MasteryPoints);
        Assert.Equal(5, course.Ratings.Count);
        Assert.Equal(MasteryCalculationMethod.DecayingAverage, course.CalculationMethod);
        Assert.Equal(65, course.CalculationParameter);
        Assert.Throws<ArgumentException>(() => course.Update("CLO1", "x", null, course.Id));
    }

    [Fact]
    public void Outcome_ScaleIsValidated()
    {
        var o = new Outcome(null, OutcomeLevel.Institution, null, null, "ILO1", "x", Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => o.SetScale([("Only", 3)], 3, MasteryCalculationMethod.Highest, null));
        Assert.Throws<ArgumentException>(() => o.SetScale([("A", 3), ("B", 3)], 3, MasteryCalculationMethod.Highest, null));
        Assert.Throws<ArgumentException>(() => o.SetScale([("A", 3), ("B", 0)], 4, MasteryCalculationMethod.Highest, null));
        Assert.Throws<ArgumentException>(() => o.SetScale([("A", 3), ("B", 0)], 3, MasteryCalculationMethod.DecayingAverage, 100));
        Assert.Throws<ArgumentException>(() => o.SetScale([("A", 3), ("B", 0)], 3, MasteryCalculationMethod.NMastery, 0));
        Assert.Throws<ArgumentException>(() => o.SetScale([("A", 3), ("B", -1)], 3, MasteryCalculationMethod.Highest, null));
        Assert.Throws<ArgumentException>(() => o.SetScale([("A", 3), ("", 1)], 3, MasteryCalculationMethod.Highest, null));

        o.SetScale([("Fail", 0), ("Excellent", 10), ("Pass", 6)], 6, MasteryCalculationMethod.Highest, 7);
        Assert.Equal(["Excellent", "Pass", "Fail"], o.Ratings.Select(r => r.Description));
        Assert.Null(o.CalculationParameter); // only decaying average and n-times keep a parameter
        Assert.Equal(10m, o.MaxPoints);
    }
}
