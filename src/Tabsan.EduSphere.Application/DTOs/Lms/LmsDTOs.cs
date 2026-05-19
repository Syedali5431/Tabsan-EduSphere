namespace Tabsan.EduSphere.Application.DTOs.Lms;

// Final-Touches Phase 20 — LMS Data Transfer Objects

// ── Stage 20.1 — Course Content Modules ──────────────────────────────────────

public sealed record CreateModuleRequest(
    Guid   OfferingId,
    string Title,
    int    WeekNumber,
    string? Body = null);

public sealed record UpdateModuleRequest(
    string  Title,
    int     WeekNumber,
    string? Body);

public sealed class CourseContentModuleDto
{
    public Guid     Id          { get; set; }
    public Guid     OfferingId  { get; set; }
    public string   Title       { get; set; } = "";
    public int      WeekNumber  { get; set; }
    public string?  Body        { get; set; }
    public bool     IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<ContentVideoDto> Videos { get; set; } = new();
}

// ── Stage 20.2 — Content Videos ──────────────────────────────────────────────

public sealed record AddVideoRequest(
    Guid    ModuleId,
    string  Title,
    string? StorageUrl,
    string? EmbedUrl,
    int?    DurationSeconds = null);

public sealed class ContentVideoDto
{
    public Guid    Id              { get; set; }
    public Guid    ModuleId        { get; set; }
    public string  Title           { get; set; } = "";
    public string? StorageUrl      { get; set; }
    public string? EmbedUrl        { get; set; }
    public int?    DurationSeconds { get; set; }
}

// ── Stage 20.3 — Discussion Threads & Replies ────────────────────────────────
// Phase 31 Stage 31.3 — Extended with type, sub-type, ticket system, and visibility

public sealed record CreateThreadRequest(
    Guid   OfferingId,
    Guid   AuthorId,
    string Title,
    string ThreadType = "Issue",
    string? IssueSubType = null);

public sealed record AddReplyRequest(
    Guid   ThreadId,
    Guid   AuthorId,
    string Body);

public sealed record MarkThreadSolvedRequest(
    Guid ThreadId,
    Guid ResolvedByUserId);

public sealed record MarkThreadVisibleRequest(
    Guid ThreadId,
    bool IsVisible);

public sealed class DiscussionThreadDto
{
    public Guid     Id         { get; set; }
    public Guid     OfferingId { get; set; }
    public string   Title      { get; set; } = "";
    public Guid     AuthorId   { get; set; }
    public string   AuthorName { get; set; } = "";
    public bool     IsPinned   { get; set; }
    public bool     IsClosed   { get; set; }
    public int      ReplyCount { get; set; }
    public DateTime CreatedAt  { get; set; }
    
    // Phase 31 Stage 31.3 — New Fields
    public string   ThreadType { get; set; } = "Issue";
    public string?  IssueSubType { get; set; }
    public bool     IsSolved   { get; set; }
    public Guid?    ResolvedBy { get; set; }
    public string?  ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string   TicketNumber { get; set; } = "";
    public bool     IsVisibleToAll { get; set; }
    
    public List<DiscussionReplyDto> Replies { get; set; } = new();
}

public sealed class DiscussionReplyDto
{
    public Guid     Id         { get; set; }
    public Guid     ThreadId   { get; set; }
    public Guid     AuthorId   { get; set; }
    public string   AuthorName { get; set; } = "";
    public string   Body       { get; set; } = "";
    public DateTime  CreatedAt  { get; set; }
    public DateTime? UpdatedAt  { get; set; }
}

// ── Stage 20.4 — Course Announcements ────────────────────────────────────────

public sealed record CreateAnnouncementRequest(
    Guid?  OfferingId,
    Guid   AuthorId,
    string Title,
    string Body);

public sealed class CourseAnnouncementDto
{
    public Guid     Id         { get; set; }
    public Guid?    OfferingId { get; set; }
    public Guid     AuthorId   { get; set; }
    public string   AuthorName { get; set; } = "";
    public string   Title      { get; set; } = "";
    public string   Body       { get; set; } = "";
    public DateTime PostedAt   { get; set; }
}

// ── Plan C Phase 3 — Course Materials Access & Isolation ───────────────────

public sealed record CreateCourseMaterialRequest(
    Guid    DepartmentId,
    Guid    AcademicProgramId,
    Guid    SemesterId,
    Guid    CourseId,
    string  MaterialType,
    string  Title,
    string? Description,
    string? ExternalUrl,
    string? BlobPath,
    string? FileName,
    long?   FileSizeBytes,
    bool    IsActive = true);

public sealed record UpdateCourseMaterialRequest(
    string  MaterialType,
    string  Title,
    string? Description,
    string? ExternalUrl,
    string? BlobPath,
    string? FileName,
    long?   FileSizeBytes,
    bool    IsActive);

public sealed class CourseMaterialDto
{
    public Guid     Id               { get; set; }
    public Guid     TenantId         { get; set; }
    public Guid     CampusId         { get; set; }
    public Guid     DepartmentId     { get; set; }
    public Guid     AcademicProgramId { get; set; }
    public Guid     SemesterId       { get; set; }
    public Guid     CourseId         { get; set; }
    public string   MaterialType     { get; set; } = "";
    public string   Title            { get; set; } = "";
    public string?  Description      { get; set; }
    public string?  ExternalUrl      { get; set; }
    public string?  BlobPath         { get; set; }
    public string?  FileName         { get; set; }
    public long?    FileSizeBytes    { get; set; }
    public bool     IsActive         { get; set; }
    public DateTime CreatedAt        { get; set; }
    public DateTime UpdatedAt        { get; set; }
}

public sealed class CourseMaterialUploadDto
{
    public string BlobPath       { get; set; } = string.Empty;
    public string FileUrl        { get; set; } = string.Empty;
    public string FileName       { get; set; } = string.Empty;
    public long   FileSizeBytes  { get; set; }
    public string ContentType    { get; set; } = string.Empty;
}
