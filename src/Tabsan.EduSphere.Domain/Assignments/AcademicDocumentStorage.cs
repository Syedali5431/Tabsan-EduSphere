using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Assignments;

public enum AcademicDocumentTemplateType
{
    Degree = 1,
    Transcript = 2
}

/// <summary>
/// Stores isolated template metadata for degree and transcript documents.
/// </summary>
public class AcademicDocumentTemplate : BaseEntity
{
    public AcademicDocumentTemplateType TemplateType { get; private set; }
    public string Name { get; private set; } = default!;
    public string Version { get; private set; } = default!;
    public string? StoragePath { get; private set; }
    public string? FileName { get; private set; }
    public string? ContentType { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? CampusId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? CourseId { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected AcademicDocumentTemplate() { }

    public static AcademicDocumentTemplate Create(
        AcademicDocumentTemplateType templateType,
        string name,
        string version,
        string? storagePath = null,
        string? fileName = null,
        string? contentType = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        Guid? departmentId = null,
        Guid? courseId = null,
        bool isActive = true)
    {
        return new AcademicDocumentTemplate
        {
            Id = Guid.NewGuid(),
            TemplateType = templateType,
            Name = name.Trim(),
            Version = version.Trim(),
            StoragePath = string.IsNullOrWhiteSpace(storagePath) ? null : storagePath.Trim(),
            FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim(),
            TenantId = tenantId,
            CampusId = campusId,
            DepartmentId = departmentId,
            CourseId = courseId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Persists generated degree certificate metadata in a dedicated table.
/// </summary>
public class DegreeDocumentRecord : BaseEntity
{
    public Guid StudentProfileId { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public Guid? AcademicDocumentTemplateId { get; private set; }
    public string SerialNumber { get; private set; } = default!;
    public string IssueDate { get; private set; } = default!;
    public DateTime GeneratedAtUtc { get; private set; }
    public string DocxPath { get; private set; } = default!;
    public string? PdfPath { get; private set; }
    public string VerificationUrl { get; private set; } = default!;

    protected DegreeDocumentRecord() { }

    public static DegreeDocumentRecord Create(
        Guid studentProfileId,
        string serialNumber,
        string issueDate,
        string docxPath,
        string? pdfPath,
        string verificationUrl,
        Guid? requestedByUserId = null,
        Guid? academicDocumentTemplateId = null)
    {
        return new DegreeDocumentRecord
        {
            Id = Guid.NewGuid(),
            StudentProfileId = studentProfileId,
            RequestedByUserId = requestedByUserId,
            AcademicDocumentTemplateId = academicDocumentTemplateId,
            SerialNumber = serialNumber.Trim(),
            IssueDate = issueDate.Trim(),
            GeneratedAtUtc = DateTime.UtcNow,
            DocxPath = docxPath.Trim(),
            PdfPath = string.IsNullOrWhiteSpace(pdfPath) ? null : pdfPath.Trim(),
            VerificationUrl = verificationUrl.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Persists generated transcript metadata in a dedicated table.
/// </summary>
public class TranscriptDocumentRecord : BaseEntity
{
    public Guid StudentProfileId { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public Guid? AcademicDocumentTemplateId { get; private set; }
    public string SerialNumber { get; private set; } = default!;
    public string IssueDate { get; private set; } = default!;
    public DateTime GeneratedAtUtc { get; private set; }
    public string DocxPath { get; private set; } = default!;
    public string? PdfPath { get; private set; }
    public string VerificationUrl { get; private set; } = default!;
    public string? CourseSnapshotJson { get; private set; }

    protected TranscriptDocumentRecord() { }

    public static TranscriptDocumentRecord Create(
        Guid studentProfileId,
        string serialNumber,
        string issueDate,
        string docxPath,
        string? pdfPath,
        string verificationUrl,
        string? courseSnapshotJson = null,
        Guid? requestedByUserId = null,
        Guid? academicDocumentTemplateId = null)
    {
        return new TranscriptDocumentRecord
        {
            Id = Guid.NewGuid(),
            StudentProfileId = studentProfileId,
            RequestedByUserId = requestedByUserId,
            AcademicDocumentTemplateId = academicDocumentTemplateId,
            SerialNumber = serialNumber.Trim(),
            IssueDate = issueDate.Trim(),
            GeneratedAtUtc = DateTime.UtcNow,
            DocxPath = docxPath.Trim(),
            PdfPath = string.IsNullOrWhiteSpace(pdfPath) ? null : pdfPath.Trim(),
            VerificationUrl = verificationUrl.Trim(),
            CourseSnapshotJson = string.IsNullOrWhiteSpace(courseSnapshotJson) ? null : courseSnapshotJson,
            CreatedAt = DateTime.UtcNow
        };
    }
}