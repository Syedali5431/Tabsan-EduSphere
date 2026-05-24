using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lms;

// Final-Touches Phase 20 Stage 20.4 — course-level announcement

/// <summary>
/// An announcement posted by faculty for a specific <see cref="CourseOffering"/>,
/// or by Admin for a department-wide audience.
/// Triggers a notification (NotificationType.Announcement = 6) to all enrolled students.
/// </summary>
public class CourseAnnouncement : AuditableEntity
{
    /// <summary>FK to the course offering (null for department-wide announcements).</summary>
    public Guid? OfferingId { get; private set; }

    /// <summary>UserId of the user who posted the announcement.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>Announcement headline.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Full announcement text (may contain Markdown).</summary>
    public string Body { get; private set; } = default!;

    /// <summary>UTC timestamp when the announcement was posted.</summary>
    public DateTime PostedAt { get; private set; } = DateTime.UtcNow;

    private CourseAnnouncement() { }

    /// <summary>Creates a new course announcement.</summary>
    public CourseAnnouncement(Guid? offeringId, Guid authorId, string title, string body)
    {
        OfferingId = offeringId;
        AuthorId   = authorId;
        Title      = title.Trim();
        Body       = body.Trim();
        PostedAt   = DateTime.UtcNow;
    }

    /// <summary>Updates the announcement content.</summary>
    public void Update(string title, string body)
    {
        Title = title.Trim();
        Body  = body.Trim();
        Touch();
    }

    public void Deactivate()
    {
        SoftDelete();
    }

    public void Activate()
    {
        Restore();
    }
}
