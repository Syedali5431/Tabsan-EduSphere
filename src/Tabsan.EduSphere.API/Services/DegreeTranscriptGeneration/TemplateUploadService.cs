using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

/// <summary>
/// Degree/Transcript Generation add-on service: stores uploaded templates in isolated storage.
/// This service only handles the dedicated degree/transcript template upload path.
/// </summary>
public sealed class TemplateUploadService
{
    private const long MaxTemplateSizeBytes = 5 * 1024 * 1024;
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly IMediaStorageService _mediaStorage;
    private readonly ApplicationDbContext _dbContext;

    public TemplateUploadService(IMediaStorageService mediaStorage, ApplicationDbContext dbContext)
    {
        _mediaStorage = mediaStorage;
        _dbContext = dbContext;
    }

    public Task<TemplateUploadResult> UploadDegreeTemplateAsync(IFormFile file, CancellationToken ct = default)
        => UploadAsync(AcademicDocumentTemplateType.Degree, file, ct);

    public Task<TemplateUploadResult> UploadTranscriptTemplateAsync(IFormFile file, CancellationToken ct = default)
        => UploadAsync(AcademicDocumentTemplateType.Transcript, file, ct);

    private async Task<TemplateUploadResult> UploadAsync(AcademicDocumentTemplateType templateType, IFormFile file, CancellationToken ct)
    {
        var error = await ValidateDocxAsync(file);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var category = templateType == AcademicDocumentTemplateType.Degree
            ? "academic-document-templates/degree"
            : "academic-document-templates/transcript";

        await using var read = file.OpenReadStream();
        var stored = await _mediaStorage.SaveAsync(read, category, ".docx", file.ContentType, file.FileName, ct);

        var version = BuildVersion(stored.ContentHashSha256);
        var activeName = templateType == AcademicDocumentTemplateType.Degree
            ? "Uploaded Degree Template"
            : "Uploaded Transcript Template";

        var template = AcademicDocumentTemplate.Create(
            templateType,
            activeName,
            version,
            stored.StorageKey,
            stored.DownloadFileName ?? file.FileName,
            stored.ContentType,
            isActive: true);

        _dbContext.AcademicDocumentTemplates.Add(template);
        await _dbContext.SaveChangesAsync(ct);

        return new TemplateUploadResult(
            template.Id,
            template.TemplateType,
            template.Name,
            template.Version,
            stored.StorageKey,
            stored.Reference,
            stored.ContentType,
            stored.Length,
            stored.DownloadFileName);
    }

    private static async Task<string?> ValidateDocxAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return "No file uploaded.";

        if (file.Length > MaxTemplateSizeBytes)
            return "File exceeds the maximum allowed size of 5 MB.";

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".docx", StringComparison.OrdinalIgnoreCase))
            return "Template file must be a .docx file.";

        if (!string.Equals(file.ContentType, DocxContentType, StringComparison.OrdinalIgnoreCase))
            return "MIME type must be application/vnd.openxmlformats-officedocument.wordprocessingml.document.";

        await using var stream = file.OpenReadStream();
        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));

        if (read < header.Length || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
            return "File content does not match the expected .docx format.";

        return null;
    }

    private static string BuildVersion(string? contentHashSha256)
    {
        var suffix = !string.IsNullOrWhiteSpace(contentHashSha256)
            ? contentHashSha256[..Math.Min(12, contentHashSha256.Length)]
            : Guid.NewGuid().ToString("N")[..12];

        return $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{suffix}";
    }
}

public sealed record TemplateUploadResult(
    Guid TemplateId,
    AcademicDocumentTemplateType TemplateType,
    string Name,
    string Version,
    string StorageKey,
    string Reference,
    string ContentType,
    long Length,
    string? DownloadFileName);