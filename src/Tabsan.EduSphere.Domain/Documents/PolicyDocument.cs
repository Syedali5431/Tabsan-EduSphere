using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Documents;

/// <summary>
/// Phase 7: Versioned policy document for ISO 9001 7.5 documented information compliance.
/// Supports Draft → Published → Archived lifecycle with version tracking.
/// </summary>
public class PolicyDocument : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string Content { get; private set; } = default!;
    public int Version { get; private set; } = 1;
    public string Status { get; private set; } = "Draft"; // Draft, Published, Archived
    public string Category { get; private set; } = "General";
    public string AccessLevel { get; private set; } = "Internal"; // Public, Internal, Confidential, Restricted
    public Guid? CreatedBy { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    private PolicyDocument() { }

    public PolicyDocument(string title, string content, string category, string accessLevel, Guid? createdBy, string? description = null)
    {
        Title = title; Content = content; Category = category;
        AccessLevel = accessLevel; CreatedBy = createdBy; Description = description;
    }

    public void UpdateContent(string newContent, int newVersion)
    {
        Content = newContent; Version = newVersion; Touch();
    }

    public void Publish()
    {
        Status = "Published"; PublishedAt = DateTime.UtcNow; Touch();
    }

    public void Archive()
    {
        Status = "Archived"; ArchivedAt = DateTime.UtcNow; Touch();
    }
}
