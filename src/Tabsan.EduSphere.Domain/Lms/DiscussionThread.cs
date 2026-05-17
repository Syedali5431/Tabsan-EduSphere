using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lms;

// Final-Touches Phase 20 Stage 20.3 — discussion thread for a course offering
// Phase 31 Stage 31.3 — extended with type, issue classification, ticket system, and visibility controls

/// <summary>
/// A discussion thread opened by a student or faculty member within a <see cref="CourseOffering"/>.
/// Faculty can pin and close threads; any participant can reply.
/// Supports categorization (Issue, FAQ), sub-classification (Technical, Result, Assignment), 
/// resolution tracking (Solved), and visibility controls (VisibleToAll).
/// </summary>
public class DiscussionThread : AuditableEntity
{
    /// <summary>FK to the course offering this thread belongs to.</summary>
    public Guid OfferingId { get; private set; }

    /// <summary>Thread subject line shown in the listing.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>UserId of the user who opened the thread.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>Pinned threads appear at the top of the forum listing.</summary>
    public bool IsPinned { get; private set; }

    /// <summary>Closed threads accept no new replies (deprecated in favor of IsSolved).</summary>
    public bool IsClosed { get; private set; }

    // ── Phase 31 Stage 31.3 — Discussion Type & Classification ────────────────

    /// <summary>Type of discussion: "Issue" or "FAQ".</summary>
    public string ThreadType { get; private set; } = "Issue";

    /// <summary>Sub-classification for Issue types: "Technical", "Result", "Assignment", or custom.</summary>
    public string? IssueSubType { get; private set; }

    /// <summary>Whether this discussion has been resolved/solved.</summary>
    public bool IsSolved { get; private set; }

    /// <summary>UserId of the person who marked this as solved (faculty/admin).</summary>
    public Guid? ResolvedBy { get; private set; }

    /// <summary>When this discussion was marked as solved.</summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>Unique ticket number for tracking (e.g., "DISCUSS-superadmin-001").</summary>
    public string TicketNumber { get; private set; } = default!;

    /// <summary>Whether this discussion is visible to all users in the department (used for FAQ and shared solutions).</summary>
    public bool IsVisibleToAll { get; private set; }

    // Navigation
    public ICollection<DiscussionReply> Replies { get; private set; } = new List<DiscussionReply>();

    private DiscussionThread() { }

    /// <summary>Creates a new open, unpinned discussion thread with type classification.</summary>
    public DiscussionThread(Guid offeringId, Guid authorId, string title, string threadType = "Issue", string? issueSubType = null, string ticketNumber = "")
    {
        OfferingId = offeringId;
        AuthorId   = authorId;
        Title      = title.Trim();
        ThreadType = threadType;
        IssueSubType = issueSubType;
        TicketNumber = ticketNumber;
        IsSolved = false;
        IsVisibleToAll = threadType == "FAQ";
    }

    /// <summary>Faculty toggles the pinned state of a thread.</summary>
    public void SetPinned(bool pinned)
    {
        IsPinned = pinned;
        Touch();
    }

    /// <summary>Faculty closes a thread to prevent further replies (deprecated, use MarkSolved instead).</summary>
    public void Close()
    {
        IsClosed = true;
        Touch();
    }

    /// <summary>Faculty re-opens a previously closed thread (deprecated, use MarkUnresolved instead).</summary>
    public void Reopen()
    {
        IsClosed = false;
        Touch();
    }

    /// <summary>Faculty/Admin marks this discussion as solved, preventing further replies from the student.</summary>
    public void MarkSolved(Guid facultyAdminUserId)
    {
        IsSolved = true;
        IsClosed = true;
        ResolvedBy = facultyAdminUserId;
        ResolvedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Faculty/Admin marks a solved discussion as unresolved, allowing replies again.</summary>
    public void MarkUnresolved()
    {
        IsSolved = false;
        IsClosed = false;
        ResolvedBy = null;
        ResolvedAt = null;
        Touch();
    }

    /// <summary>Faculty/Admin marks this discussion as visible to all department users.</summary>
    public void MarkVisibleToAll()
    {
        IsVisibleToAll = true;
        Touch();
    }

    /// <summary>Faculty/Admin marks this discussion as visible only to involved parties.</summary>
    public void MarkPrivate()
    {
        IsVisibleToAll = false;
        Touch();
    }
}
