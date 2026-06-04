using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Documents;

/// <summary>
/// Phase 7: Immutable version history record for a policy document.
/// Each update creates a new version entry for audit trail.
/// </summary>
public class PolicyDocumentVersion : BaseEntity
{
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Content { get; private set; } = default!;
    public Guid? ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? ChangeNotes { get; private set; }

    private PolicyDocumentVersion() { }

    public PolicyDocumentVersion(Guid documentId, int versionNumber, string content, Guid? changedBy, string? changeNotes = null)
    {
        DocumentId = documentId; VersionNumber = versionNumber;
        Content = content; ChangedBy = changedBy;
        ChangedAt = DateTime.UtcNow; ChangeNotes = changeNotes;
    }
}
