using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Outcomes;

// Plan M6 — learning outcomes, alignments and mastery results.

/// <summary>Where an outcome is defined: for the whole institution, one academic program (PLO) or one course (CLO).</summary>
public enum OutcomeLevel
{
    Institution = 0,
    Program = 1,
    Course = 2
}

/// <summary>How a student's scores on an outcome are combined into one mastery score.</summary>
public enum MasteryCalculationMethod
{
    /// <summary>The highest score.</summary>
    Highest = 0,
    /// <summary>The most recent score.</summary>
    Latest = 1,
    /// <summary>The most recent score weighted by the method parameter (%, default 65); the average of the others makes up the rest.</summary>
    DecayingAverage = 2,
    /// <summary>Mastery must be reached the parameter's number of times (default 5); the score is the average of the mastery scores.</summary>
    NMastery = 3,
    /// <summary>The plain average.</summary>
    Average = 4
}

/// <summary>What an outcome is aligned to.</summary>
public enum OutcomeTargetType
{
    /// <summary>A whole assignment (its mark out of its maximum; tool scores arrive here too).</summary>
    Assignment = 0,
    /// <summary>One rubric criterion (the chosen level's points out of the criterion's maximum).</summary>
    RubricCriterion = 1,
    /// <summary>A question written in a quiz.</summary>
    QuizQuestion = 2,
    /// <summary>A question bank question (counted wherever a quiz draws it).</summary>
    BankQuestion = 3
}

/// <summary>Which kind of graded work produced a result.</summary>
public enum OutcomeSourceType
{
    AssignmentSubmission = 0,
    QuizAttempt = 1
}

/// <summary>A learning outcome (institution outcome, program learning outcome or course learning outcome).</summary>
public class Outcome : BaseEntity
{
    public Guid? TenantId { get; private set; }
    public OutcomeLevel Level { get; private set; }
    /// <summary>The program of a program-level outcome.</summary>
    public Guid? ProgramId { get; private set; }
    /// <summary>The course of a course-level outcome.</summary>
    public Guid? CourseId { get; private set; }
    /// <summary>Optional parent outcome on the same level (e.g. sub-outcomes of a PLO).</summary>
    public Guid? ParentId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    /// <summary>Points on the rating scale that count as mastery.</summary>
    public decimal MasteryPoints { get; private set; }
    public MasteryCalculationMethod CalculationMethod { get; private set; }
    /// <summary>DecayingAverage: weight of the latest score in % (1–99); NMastery: required times (1–10).</summary>
    public int? CalculationParameter { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }

    private readonly List<OutcomeRatingLevel> _ratings = new();
    public IReadOnlyList<OutcomeRatingLevel> Ratings => _ratings;

    private Outcome() { }

    public Outcome(Guid? tenantId, OutcomeLevel level, Guid? programId, Guid? courseId, string code, string title, Guid createdByUserId)
    {
        if (level == OutcomeLevel.Program && programId is null) throw new ArgumentException("A program outcome needs its program.", nameof(programId));
        if (level == OutcomeLevel.Course && courseId is null) throw new ArgumentException("A course outcome needs its course.", nameof(courseId));
        TenantId = tenantId;
        Level = level;
        ProgramId = level == OutcomeLevel.Program ? programId : null;
        CourseId = level == OutcomeLevel.Course ? courseId : null;
        Code = code.Trim();
        Title = title.Trim();
        CreatedByUserId = createdByUserId;
        CalculationMethod = MasteryCalculationMethod.DecayingAverage;
        CalculationParameter = 65;
    }

    /// <summary>The top of the rating scale (scores are expressed on 0..MaxPoints).</summary>
    public decimal MaxPoints => _ratings.Count == 0 ? 0 : _ratings.Max(r => r.Points);

    public void Update(string code, string title, string? description, Guid? parentId)
    {
        if (parentId == Id) throw new ArgumentException("An outcome cannot be its own parent.", nameof(parentId));
        Code = code.Trim();
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ParentId = parentId;
        Touch();
    }

    /// <summary>Replaces the rating scale and the mastery rule. Levels need distinct, non-negative points; mastery must be on the scale.</summary>
    public void SetScale(IReadOnlyList<(string Description, decimal Points)> levels, decimal masteryPoints, MasteryCalculationMethod method, int? parameter)
    {
        if (levels.Count is < 2 or > 10) throw new ArgumentException("A rating scale needs 2 to 10 levels.", nameof(levels));
        if (levels.Any(l => l.Points < 0 || string.IsNullOrWhiteSpace(l.Description))) throw new ArgumentException("Every level needs a description and points of 0 or more.", nameof(levels));
        if (levels.Select(l => l.Points).Distinct().Count() != levels.Count) throw new ArgumentException("Each level needs different points.", nameof(levels));
        var max = levels.Max(l => l.Points);
        if (max <= 0) throw new ArgumentException("The top level must be worth more than 0 points.", nameof(levels));
        if (masteryPoints <= 0 || masteryPoints > max) throw new ArgumentException("Mastery must be above 0 and not above the top level.", nameof(masteryPoints));
        parameter = method switch
        {
            MasteryCalculationMethod.DecayingAverage => parameter is >= 1 and <= 99 ? parameter : throw new ArgumentException("The decaying-average weight must be 1–99%.", nameof(parameter)),
            MasteryCalculationMethod.NMastery => parameter is >= 1 and <= 10 ? parameter : throw new ArgumentException("The number of times must be 1–10.", nameof(parameter)),
            _ => null
        };

        _ratings.Clear();
        var order = 0;
        foreach (var (description, points) in levels.OrderByDescending(l => l.Points))
            _ratings.Add(new OutcomeRatingLevel(Id, description.Trim(), points, order++));
        MasteryPoints = masteryPoints;
        CalculationMethod = method;
        CalculationParameter = parameter;
        Touch();
    }

    /// <summary>The default 0–4 scale with mastery at 3 (Exceeds / Meets / Near / Below / No evidence).</summary>
    public void UseDefaultScale()
        => SetScale([("Exceeds mastery", 4), ("Meets mastery", 3), ("Near mastery", 2), ("Below mastery", 1), ("No evidence", 0)],
            3, MasteryCalculationMethod.DecayingAverage, 65);

    public void SetActive(bool active) { IsActive = active; Touch(); }
}

/// <summary>One level of an outcome's rating scale (e.g. "Meets mastery", 3 points).</summary>
public class OutcomeRatingLevel : BaseEntity
{
    public Guid OutcomeId { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Points { get; private set; }
    public int DisplayOrder { get; private set; }

    private OutcomeRatingLevel() { }

    internal OutcomeRatingLevel(Guid outcomeId, string description, decimal points, int displayOrder)
    {
        OutcomeId = outcomeId;
        Description = description;
        Points = points;
        DisplayOrder = displayOrder;
    }
}

/// <summary>A course outcome contributes to a program outcome (CLO → PLO).</summary>
public class OutcomeMapping : BaseEntity
{
    public Guid CourseOutcomeId { get; private set; }
    public Guid ProgramOutcomeId { get; private set; }

    private OutcomeMapping() { }

    public OutcomeMapping(Guid courseOutcomeId, Guid programOutcomeId)
    {
        CourseOutcomeId = courseOutcomeId;
        ProgramOutcomeId = programOutcomeId;
    }
}

/// <summary>An outcome assessed by an assignment, rubric criterion or question.</summary>
public class OutcomeAlignment : BaseEntity
{
    public Guid OutcomeId { get; private set; }
    public OutcomeTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    /// <summary>The course offering of the target (null for bank questions, which can be used in many courses).</summary>
    public Guid? CourseOfferingId { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private OutcomeAlignment() { }

    public OutcomeAlignment(Guid outcomeId, OutcomeTargetType targetType, Guid targetId, Guid? courseOfferingId, Guid createdByUserId)
    {
        OutcomeId = outcomeId;
        TargetType = targetType;
        TargetId = targetId;
        CourseOfferingId = courseOfferingId;
        CreatedByUserId = createdByUserId;
    }
}

/// <summary>
/// One piece of evidence: a student's score on an outcome from one graded piece of work (posted assignment grade,
/// rubric criterion or quiz question). Rows are recalculated whenever the work is graded, regraded, posted or hidden.
/// </summary>
public class StudentOutcomeResult : BaseEntity
{
    public Guid StudentProfileId { get; private set; }
    public Guid OutcomeId { get; private set; }
    public Guid CourseOfferingId { get; private set; }
    public Guid AlignmentId { get; private set; }
    public OutcomeSourceType SourceType { get; private set; }
    /// <summary>The submission or quiz attempt.</summary>
    public Guid SourceId { get; private set; }
    /// <summary>Earned share of the aligned work, 0..1.</summary>
    public decimal Percent { get; private set; }
    /// <summary>The share on the outcome's scale (Percent × top level points).</summary>
    public decimal Score { get; private set; }
    public DateTime AssessedAt { get; private set; }

    private StudentOutcomeResult() { }

    public StudentOutcomeResult(Guid studentProfileId, Guid outcomeId, Guid courseOfferingId, Guid alignmentId, OutcomeSourceType sourceType,
        Guid sourceId, decimal percent, decimal score, DateTime assessedAt)
    {
        StudentProfileId = studentProfileId;
        OutcomeId = outcomeId;
        CourseOfferingId = courseOfferingId;
        AlignmentId = alignmentId;
        SourceType = sourceType;
        SourceId = sourceId;
        Percent = Math.Clamp(percent, 0, 1);
        Score = score;
        AssessedAt = assessedAt;
    }

    public bool SameAs(StudentOutcomeResult other)
        => Percent == other.Percent && Score == other.Score && AssessedAt == other.AssessedAt;

    public void Refresh(decimal percent, decimal score, DateTime assessedAt)
    {
        Percent = Math.Clamp(percent, 0, 1);
        Score = score;
        AssessedAt = assessedAt;
        Touch();
    }
}
