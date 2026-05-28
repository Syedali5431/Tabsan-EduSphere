using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Assignments;

/// <summary>
/// An officially published mark for a student in a <see cref="CourseOffering"/>.
/// Once published, a result is immutable — corrections require an Admin-initiated override
/// and must be audited. There is at most one result row per (StudentProfile, CourseOffering, ResultType) tuple.
/// </summary>
public class Result : BaseEntity
{
    /// <summary>FK to the student profile this result belongs to.</summary>
    public Guid StudentProfileId { get; private set; }

    /// <summary>FK to the course offering the mark relates to.</summary>
    public Guid CourseOfferingId { get; private set; }

    /// <summary>
    /// The category of mark (for example: Quiz, Midterm, Final, Total).
    /// Stored as a string so the admin-configured result components can flow through without code changes.
    /// </summary>
    public string ResultType { get; private set; } = default!;

    /// <summary>Marks obtained by the student. Between 0 and MaxMarks for this result type.</summary>
    public decimal MarksObtained { get; private set; }

    /// <summary>Maximum marks possible for this result type (e.g. 50 for midterm, 100 for final).</summary>
    public decimal MaxMarks { get; private set; }

    /// <summary>
    /// Current GPA derived from the entered marks and the configured GPA scale.
    /// For component rows this reflects the component-only GPA; for the Total row it reflects the current subject GPA.
    /// </summary>
    public decimal? GradePoint { get; private set; }

    /// <summary>True once the result has been officially published and becomes visible to the student.</summary>
    public bool IsPublished { get; private set; }

    /// <summary>UTC timestamp of publication. Null while still a draft.</summary>
    public DateTime? PublishedAt { get; private set; }

    /// <summary>FK to the faculty/admin user who published this result.</summary>
    public Guid? PublishedByUserId { get; private set; }

    private Result() { }

    /// <summary>
    /// Creates a draft result. Call <see cref="Publish"/> to make it visible to students.
    /// </summary>
    public Result(Guid studentProfileId, Guid courseOfferingId, string resultType,
                  decimal marksObtained, decimal maxMarks)
    {
        if (string.IsNullOrWhiteSpace(resultType))
            throw new ArgumentException("Result type is required.", nameof(resultType));

        if (marksObtained < 0 || marksObtained > maxMarks)
            throw new ArgumentOutOfRangeException(nameof(marksObtained),
                $"Marks obtained ({marksObtained}) must be between 0 and {maxMarks}.");

        StudentProfileId = studentProfileId;
        CourseOfferingId = courseOfferingId;
        ResultType = resultType.Trim();
        MarksObtained = marksObtained;
        MaxMarks = maxMarks;
    }

    /// <summary>
    /// Officially publishes this result, making it visible to the student.
    /// Once published, the result is locked — further edits require an Admin override.
    /// </summary>
    public void Publish(Guid publishedByUserId)
    {
        if (IsPublished)
            throw new InvalidOperationException("Result is already published.");

        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        PublishedByUserId = publishedByUserId;
        Touch();
    }

    /// <summary>
    /// Admin-only override to correct a published result.
    /// The corrected marks replace the original; the publish state is preserved.
    /// All corrections must be audited at the service layer.
    /// </summary>
    public void CorrectMarks(decimal newMarksObtained, decimal newMaxMarks)
    {
        if (!IsPublished)
            throw new InvalidOperationException("Only published results can be corrected.");

        if (newMarksObtained < 0 || newMarksObtained > newMaxMarks)
            throw new ArgumentOutOfRangeException(nameof(newMarksObtained),
                $"Corrected marks ({newMarksObtained}) must be between 0 and {newMaxMarks}.");

        MarksObtained = newMarksObtained;
        MaxMarks = newMaxMarks;
        Touch();
    }

    /// <summary>
    /// Updates the GPA value derived from the configured grading scale.
    /// </summary>
    public void SetGradePoint(decimal? gradePoint)
    {
        if (gradePoint.HasValue && (gradePoint.Value < 0 || gradePoint.Value > 4.0m))
            throw new ArgumentOutOfRangeException(nameof(gradePoint), "Grade point must be between 0.0 and 4.0.");

        GradePoint = gradePoint;
        Touch();
    }
}
