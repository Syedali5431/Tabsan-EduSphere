using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Domain.Assignments;

/// <summary>
/// GPA scale row mapping a minimum score threshold to a grade point.
/// Example: 60 => 2.0, 65 => 2.5.
/// </summary>
public class GpaScaleRule : BaseEntity
{
    public InstitutionType InstitutionType { get; private set; } = InstitutionType.University;
    public decimal GradePoint { get; private set; }
    public decimal MinimumScore { get; private set; }
    public int DisplayOrder { get; private set; }

    private GpaScaleRule() { }

    public GpaScaleRule(decimal gradePoint, decimal minimumScore, int displayOrder, InstitutionType institutionType = InstitutionType.University)
    {
        InstitutionType = institutionType;
        Update(gradePoint, minimumScore, displayOrder);
    }

    public void Update(decimal gradePoint, decimal minimumScore, int displayOrder)
    {
        if (gradePoint < 0 || gradePoint > 4.0m)
            throw new ArgumentOutOfRangeException(nameof(gradePoint), "GPA must be between 0.0 and 4.0.");
        if (minimumScore < 0 || minimumScore > 100)
            throw new ArgumentOutOfRangeException(nameof(minimumScore), "Minimum score must be between 0 and 100.");

        GradePoint = gradePoint;
        MinimumScore = minimumScore;
        DisplayOrder = displayOrder;
        Touch();
    }
}

/// <summary>
/// Configurable assessment component and its contribution to the subject total.
/// Example: Quiz = 20, Midterm = 30, Final = 50.
/// </summary>
public class ResultComponentRule : BaseEntity
{
    public InstitutionType InstitutionType { get; private set; } = InstitutionType.University;
    public string Name { get; private set; } = default!;
    public decimal Weightage { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private ResultComponentRule() { }

    public ResultComponentRule(string name, decimal weightage, int displayOrder, bool isActive = true, InstitutionType institutionType = InstitutionType.University)
    {
        InstitutionType = institutionType;
        Update(name, weightage, displayOrder, isActive);
    }

    public void Update(string name, decimal weightage, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Component name is required.", nameof(name));
        if (weightage <= 0 || weightage > 100)
            throw new ArgumentOutOfRangeException(nameof(weightage), "Weightage must be greater than 0 and at most 100.");

        Name = name.Trim();
        Weightage = weightage;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        Touch();
    }
}