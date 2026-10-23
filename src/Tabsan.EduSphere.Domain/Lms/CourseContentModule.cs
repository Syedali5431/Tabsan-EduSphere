using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lms;

// Final-Touches Phase 20 Stage 20.1 — structured course content module

/// <summary>
/// A weekly content module belonging to a <see cref="CourseOffering"/>.
/// Faculty create and publish modules; enrolled students access published ones in order.
/// </summary>
public class CourseContentModule : AuditableEntity
{
    /// <summary>FK to the course offering this module belongs to.</summary>
    public Guid OfferingId { get; private set; }

    /// <summary>Module title displayed in the student portal (e.g. "Week 3 — Sorting Algorithms").</summary>
    public string Title { get; private set; } = default!;

    /// <summary>1-based week number used for ordering.</summary>
    public int WeekNumber { get; private set; }

    /// <summary>Rich-text body (Markdown/HTML) containing lecture notes, instructions, etc.</summary>
    public string? Body { get; private set; }

    /// <summary>True once the module has been published and is visible to enrolled students.</summary>
    public bool IsPublished { get; private set; }

    /// <summary>UTC timestamp when the module was published.</summary>
    public DateTime? PublishedAt { get; private set; }

    /// <summary>Plan M7: the module (and its items) open on this date.</summary>
    public DateTime? UnlockAt { get; private set; }

    /// <summary>Plan M7: items must be completed in order.</summary>
    public bool RequireSequentialProgress { get; private set; }

    /// <summary>Plan M7: whether all or any item requirements complete the module.</summary>
    public ModuleCompletionMode CompletionMode { get; private set; }

    // Navigation
    public ICollection<ContentVideo> Videos { get; private set; } = new List<ContentVideo>();

    private CourseContentModule() { }

    /// <summary>Creates a new unpublished course content module.</summary>
    public CourseContentModule(Guid offeringId, string title, int weekNumber, string? body = null)
    {
        OfferingId = offeringId;
        Title      = title.Trim();
        WeekNumber = weekNumber;
        Body       = body;
    }

    /// <summary>Updates module content. Only allowed while unpublished or for minor corrections.</summary>
    public void Update(string title, int weekNumber, string? body)
    {
        Title      = title.Trim();
        WeekNumber = weekNumber;
        Body       = body;
        Touch();
    }

    /// <summary>Publishes the module — it becomes visible to enrolled students.</summary>
    public void Publish()
    {
        if (IsPublished) return;
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Plan M7: release conditions (prerequisite modules are stored separately).</summary>
    public void ConfigureRelease(DateTime? unlockAt, bool requireSequentialProgress, ModuleCompletionMode completionMode)
    {
        UnlockAt = unlockAt;
        RequireSequentialProgress = requireSequentialProgress;
        CompletionMode = completionMode;
        Touch();
    }

    /// <summary>Unpublishes the module — hides it from students without deleting content.</summary>
    public void Unpublish()
    {
        if (!IsPublished) return;
        IsPublished = false;
        Touch();
    }
}
