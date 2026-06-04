namespace Tabsan.EduSphere.Application.DTOs.Documents;

public sealed class CreatePolicyDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string AccessLevel { get; set; } = "Internal";
    public string? Description { get; set; }
}

public sealed class UpdatePolicyDocumentRequest
{
    public string Content { get; set; } = string.Empty;
    public string? ChangeNotes { get; set; }
}

public sealed class PolicyDocumentItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class PolicyDocumentVersionItemDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangeNotes { get; set; }
}
