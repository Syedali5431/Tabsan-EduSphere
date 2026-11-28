using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lms;

// Course copy and templates: a course's content (modules, pages, assignments, quizzes, files, tools) copied into another
// course offering, e.g. last term's course into this term's, or a department's template into a new class.

/// <summary>Who may start from a template.</summary>
public enum CourseTemplateScope
{
    /// <summary>Teachers and admins of the course's department.</summary>
    Department = 0,
    /// <summary>Every teacher and admin of the school (tenant).</summary>
    Institution = 1
}

/// <summary>A course offering shared as a template: its content is what others copy. The offering stays an ordinary
/// course that its teachers edit with the usual tools.</summary>
public class CourseTemplate : BaseEntity
{
    public const int MaxNameLength = 150;
    public const int MaxDescriptionLength = 1000;

    public Guid OfferingId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public CourseTemplateScope Scope { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private CourseTemplate() { }

    public CourseTemplate(Guid offeringId, string name, string? description, CourseTemplateScope scope, Guid createdByUserId)
    {
        if (offeringId == Guid.Empty) throw new ArgumentException("Choose the course to share.", nameof(offeringId));
        OfferingId = offeringId;
        CreatedByUserId = createdByUserId;
        Update(name, description, scope);
    }

    public void Update(string name, string? description, CourseTemplateScope scope)
    {
        var n = (name ?? "").Trim();
        if (n.Length == 0) throw new ArgumentException("Enter a name for the template.", nameof(name));
        if (n.Length > MaxNameLength) throw new ArgumentException($"A template name has at most {MaxNameLength} characters.", nameof(name));
        var d = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (d is { Length: > MaxDescriptionLength }) throw new ArgumentException($"A description has at most {MaxDescriptionLength} characters.", nameof(description));
        if (!Enum.IsDefined(scope)) throw new ArgumentException("Choose who may use the template.", nameof(scope));
        Name = n;
        Description = d;
        Scope = scope;
        Touch();
    }
}

/// <summary>One copy of a course's content into another offering (kept as the target's copy history).</summary>
public class CourseCopyRun : BaseEntity
{
    public const int MaxSummaryLength = 8000;

    public Guid SourceOfferingId { get; private set; }
    public Guid TargetOfferingId { get; private set; }
    public Guid CopiedByUserId { get; private set; }
    /// <summary>Days added to every copied date (0 = dates kept).</summary>
    public int DateShiftDays { get; private set; }
    /// <summary>True when what was published in the source was published in the copy; otherwise everything arrived hidden.</summary>
    public bool KeptPublishedState { get; private set; }
    /// <summary>What was copied and what was left out (JSON).</summary>
    public string SummaryJson { get; private set; } = "{}";

    private CourseCopyRun() { }

    public CourseCopyRun(Guid sourceOfferingId, Guid targetOfferingId, Guid copiedByUserId, int dateShiftDays, bool keptPublishedState, string summaryJson, DateTime now)
    {
        if (sourceOfferingId == targetOfferingId) throw new ArgumentException("A course cannot be copied into itself.", nameof(targetOfferingId));
        SourceOfferingId = sourceOfferingId;
        TargetOfferingId = targetOfferingId;
        CopiedByUserId = copiedByUserId;
        DateShiftDays = dateShiftDays;
        KeptPublishedState = keptPublishedState;
        SummaryJson = summaryJson.Length > MaxSummaryLength ? summaryJson[..MaxSummaryLength] : summaryJson;
        CreatedAt = now;
    }
}
