// Final-Touches Phase 16 Stage 16.2 — Rubric domain entities for rubric-based grading

using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Assignments;

/// <summary>
/// A grading rubric attached to an assignment.
/// </summary>
public class Rubric : AuditableEntity
{
    // Final-Touches Phase 16 Stage 16.2 — assignment FK and basic rubric metadata
    public Guid   AssignmentId { get; private set; }
    public string Title        { get; private set; } = string.Empty;
    public bool   IsActive     { get; private set; } = true;

    private readonly List<RubricCriterion> _criteria = new();
    public IReadOnlyList<RubricCriterion> Criteria => _criteria.AsReadOnly();

    protected Rubric() { }

    public static Rubric Create(Guid assignmentId, string title, Guid createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new Rubric
        {
            Id           = Guid.NewGuid(),
            AssignmentId = assignmentId,
            Title        = title.Trim(),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
    }

    public void Update(string title, Guid updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
        Touch();
    }

    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Plan M3 fix: adds a criterion to this rubric. (Creating a criterion on its own never attached it,
    /// so rubrics were saved without criteria.)
    /// </summary>
    public RubricCriterion AddCriterion(string name, decimal maxPoints, int displayOrder)
    {
        var criterion = RubricCriterion.Create(Id, name, maxPoints, displayOrder);
        _criteria.Add(criterion);
        return criterion;
    }
}

/// <summary>
/// A single grading criterion (row) within a rubric.
/// </summary>
public class RubricCriterion : BaseEntity
{
    // Final-Touches Phase 16 Stage 16.2 — criterion definition
    public Guid    RubricId     { get; private set; }
    public string  Name         { get; private set; } = string.Empty;
    public decimal MaxPoints    { get; private set; }
    public int     DisplayOrder { get; private set; }

    private readonly List<RubricLevel> _levels = new();
    public IReadOnlyList<RubricLevel> Levels => _levels.AsReadOnly();

    protected RubricCriterion() { }

    public static RubricCriterion Create(Guid rubricId, string name, decimal maxPoints, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (maxPoints <= 0) throw new ArgumentException("MaxPoints must be positive.", nameof(maxPoints));
        return new RubricCriterion
        {
            Id           = Guid.NewGuid(),
            RubricId     = rubricId,
            Name         = name.Trim(),
            MaxPoints    = maxPoints,
            DisplayOrder = displayOrder,
            CreatedAt    = DateTime.UtcNow
        };
    }

    /// <summary>Plan M3 fix: adds a performance level to this criterion. Points may not exceed the criterion's maximum.</summary>
    public RubricLevel AddLevel(string label, decimal pointsAwarded, int displayOrder)
    {
        if (pointsAwarded > MaxPoints)
            throw new ArgumentException($"Level \"{label}\" awards more than the criterion's {MaxPoints} points.", nameof(pointsAwarded));
        var level = RubricLevel.Create(Id, label, pointsAwarded, displayOrder);
        _levels.Add(level);
        return level;
    }
}

/// <summary>
/// A performance level (column) for a rubric criterion.
/// </summary>
public class RubricLevel : BaseEntity
{
    // Final-Touches Phase 16 Stage 16.2 — rubric level (e.g. Excellent / Good / Poor)
    public Guid    CriterionId    { get; private set; }
    public string  Label          { get; private set; } = string.Empty;
    public decimal PointsAwarded  { get; private set; }
    public int     DisplayOrder   { get; private set; }

    protected RubricLevel() { }

    public static RubricLevel Create(Guid criterionId, string label, decimal pointsAwarded, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (pointsAwarded < 0) throw new ArgumentException("PointsAwarded cannot be negative.", nameof(pointsAwarded));
        return new RubricLevel
        {
            Id             = Guid.NewGuid(),
            CriterionId    = criterionId,
            Label          = label.Trim(),
            PointsAwarded  = pointsAwarded,
            DisplayOrder   = displayOrder,
            CreatedAt      = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Records which rubric level was selected for a student's submission criterion.
/// </summary>
public class RubricStudentGrade : BaseEntity
{
    // Final-Touches Phase 16 Stage 16.2 — links a submission criterion to a chosen level
    public Guid    AssignmentSubmissionId { get; private set; }
    public Guid    RubricCriterionId      { get; private set; }
    public Guid    RubricLevelId          { get; private set; }
    public decimal PointsAwarded          { get; private set; }
    public Guid    GradedByUserId         { get; private set; }

    protected RubricStudentGrade() { }

    public static RubricStudentGrade Create(
        Guid submissionId,
        Guid criterionId,
        Guid levelId,
        decimal points,
        Guid gradedByUserId)
    {
        if (points < 0) throw new ArgumentException("PointsAwarded cannot be negative.", nameof(points));
        return new RubricStudentGrade
        {
            Id                     = Guid.NewGuid(),
            AssignmentSubmissionId = submissionId,
            RubricCriterionId      = criterionId,
            RubricLevelId          = levelId,
            PointsAwarded          = points,
            GradedByUserId         = gradedByUserId,
            CreatedAt              = DateTime.UtcNow
        };
    }

    public void Update(Guid levelId, decimal points, Guid updatedByUserId)
    {
        if (points < 0) throw new ArgumentException("PointsAwarded cannot be negative.", nameof(points));
        RubricLevelId  = levelId;
        PointsAwarded  = points;
        GradedByUserId = updatedByUserId;
        Touch();
    }
}
