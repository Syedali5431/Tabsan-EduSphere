using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

/// <summary>
/// Degree/Transcript Generation add-on service: orchestrates document generation in an isolated pipeline.
/// This service intentionally does not alter existing generation/storage services.
/// </summary>
public sealed class DocumentGenerationService
{
    private readonly TemplateExportService _templateExport;
    private readonly TemplateProcessorService _templateProcessor;
    private readonly QRCodeService _qrCodeService;
    private readonly IPdfConverterAdapter _pdfConverterAdapter;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DocumentGenerationService> _logger;
    private readonly ConcurrentDictionary<Guid, GeneratedDocumentInfo> _documents = new();

    public DocumentGenerationService(
        TemplateExportService templateExport,
        TemplateProcessorService templateProcessor,
        QRCodeService qrCodeService,
        IPdfConverterAdapter pdfConverterAdapter,
        ApplicationDbContext dbContext,
        ILogger<DocumentGenerationService> logger)
    {
        _templateExport = templateExport;
        _templateProcessor = templateProcessor;
        _qrCodeService = qrCodeService;
        _pdfConverterAdapter = pdfConverterAdapter;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GeneratedDocumentInfo> GenerateDegreeAsync(DegreeGenerationRequest request, CancellationToken ct = default)
    {
        var template = await _templateExport.GetDegreeTemplateAsync(ct);
        return await GenerateInternalAsync(AcademicDocumentType.Degree, template, request.ToPayload(), null, request.StudentId, ct);
    }

    public async Task<GeneratedDocumentInfo> GenerateTranscriptAsync(TranscriptGenerationRequest request, CancellationToken ct = default)
    {
        var template = await _templateExport.GetTranscriptTemplateAsync(ct);
        return await GenerateInternalAsync(AcademicDocumentType.Transcript, template, request.ToPayload(), request.Courses, request.StudentId, ct);
    }

    public Task<GeneratedDocumentInfo?> GetAsync(Guid documentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (_documents.TryGetValue(documentId, out var item))
        {
            return Task.FromResult<GeneratedDocumentInfo?>(item);
        }

        return GetFromDatabaseAsync(documentId, ct);
    }

    public Task<IReadOnlyList<GeneratedDocumentInfo>> ListByStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return ListByStudentFromDatabaseAsync(studentId, ct);
    }

    private async Task<GeneratedDocumentInfo> GenerateInternalAsync(
        AcademicDocumentType type,
        TemplateExportResult template,
        DocumentTemplatePayload payload,
        IReadOnlyList<TranscriptCourseRow>? courses,
        Guid studentId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var documentId = Guid.NewGuid();
        var serial = payload.SerialNumber;
        var verificationUrl = $"https://verify.tabsan-edusphere.local/document/{documentId:N}";

        var qrBytes = _qrCodeService.GeneratePng(verificationUrl);
        var finalPayload = payload with { VerificationUrl = verificationUrl };
        var processed = _templateProcessor.PopulateTemplate(template.Content, finalPayload, courses);

        var outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "Artifacts", "Degree-Transcript-Generation", "generated-documents");
        Directory.CreateDirectory(outputRoot);

        var baseName = $"{type.ToString().ToLowerInvariant()}-{serial}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var docxPath = Path.Combine(outputRoot, $"{baseName}.docx");
        await File.WriteAllBytesAsync(docxPath, processed, ct);

        var qrPath = Path.Combine(outputRoot, $"{baseName}.qr.png");
        await File.WriteAllBytesAsync(qrPath, qrBytes, ct);

        string? pdfPath = null;
        try
        {
            pdfPath = await _pdfConverterAdapter.TryConvertToPdfAsync(docxPath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Degree/Transcript PDF conversion adapter failed for {DocxPath}. Falling back to .docx.",
                docxPath);
            pdfPath = null;
        }

        var info = new GeneratedDocumentInfo(
            DocumentId: documentId,
            Type: type,
            StudentId: studentId,
            SerialNumber: serial,
            IssueDate: finalPayload.IssueDate,
            GeneratedAtUtc: DateTime.UtcNow,
            DocxPath: docxPath,
            PdfPath: pdfPath,
            QrImagePath: qrPath,
            VerificationUrl: verificationUrl);

        _documents[documentId] = info;

        try
        {
            await PersistGeneratedDocumentAsync(info, courses, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Degree/Transcript storage persistence failed for {Type} document {DocumentId}; in-memory and file outputs remain available.",
                type,
                documentId);
        }

        _logger.LogInformation("DegreeTranscriptGeneration generated {Type} document {DocumentId} for Student {StudentId}", type, documentId, studentId);
        return info;
    }

    private async Task PersistGeneratedDocumentAsync(
        GeneratedDocumentInfo info,
        IReadOnlyList<TranscriptCourseRow>? courses,
        CancellationToken ct)
    {
        var templateType = info.Type == AcademicDocumentType.Degree
            ? AcademicDocumentTemplateType.Degree
            : AcademicDocumentTemplateType.Transcript;

        var templateId = await _dbContext.AcademicDocumentTemplates
            .AsNoTracking()
            .Where(t => t.TemplateType == templateType && t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (info.Type == AcademicDocumentType.Degree)
        {
            _dbContext.DegreeDocuments.Add(DegreeDocumentRecord.Create(
                studentProfileId: info.StudentId,
                serialNumber: info.SerialNumber,
                issueDate: info.IssueDate,
                docxPath: info.DocxPath,
                pdfPath: info.PdfPath,
                verificationUrl: info.VerificationUrl,
                academicDocumentTemplateId: templateId == Guid.Empty ? null : templateId));
        }
        else
        {
            var courseSnapshotJson = courses is null
                ? null
                : JsonSerializer.Serialize(courses.Select(c => new
                {
                    c.CourseName,
                    c.CreditHours,
                    c.Grade,
                    c.SgpaOrMarks
                }));

            _dbContext.TranscriptDocuments.Add(TranscriptDocumentRecord.Create(
                studentProfileId: info.StudentId,
                serialNumber: info.SerialNumber,
                issueDate: info.IssueDate,
                docxPath: info.DocxPath,
                pdfPath: info.PdfPath,
                verificationUrl: info.VerificationUrl,
                courseSnapshotJson: courseSnapshotJson,
                academicDocumentTemplateId: templateId == Guid.Empty ? null : templateId));
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<GeneratedDocumentInfo?> GetFromDatabaseAsync(Guid documentId, CancellationToken ct)
    {
        var degree = await _dbContext.DegreeDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (degree is not null)
        {
            return new GeneratedDocumentInfo(
                DocumentId: degree.Id,
                Type: AcademicDocumentType.Degree,
                StudentId: degree.StudentProfileId,
                SerialNumber: degree.SerialNumber,
                IssueDate: degree.IssueDate,
                GeneratedAtUtc: degree.GeneratedAtUtc,
                DocxPath: degree.DocxPath,
                PdfPath: degree.PdfPath,
                QrImagePath: string.Empty,
                VerificationUrl: degree.VerificationUrl);
        }

        var transcript = await _dbContext.TranscriptDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (transcript is not null)
        {
            return new GeneratedDocumentInfo(
                DocumentId: transcript.Id,
                Type: AcademicDocumentType.Transcript,
                StudentId: transcript.StudentProfileId,
                SerialNumber: transcript.SerialNumber,
                IssueDate: transcript.IssueDate,
                GeneratedAtUtc: transcript.GeneratedAtUtc,
                DocxPath: transcript.DocxPath,
                PdfPath: transcript.PdfPath,
                QrImagePath: string.Empty,
                VerificationUrl: transcript.VerificationUrl);
        }

        return null;
    }

    private async Task<IReadOnlyList<GeneratedDocumentInfo>> ListByStudentFromDatabaseAsync(Guid studentId, CancellationToken ct)
    {
        var documents = new List<GeneratedDocumentInfo>();

        documents.AddRange(_documents.Values.Where(d => d.StudentId == studentId));

        var degreeDocs = await _dbContext.DegreeDocuments.AsNoTracking()
            .Where(d => d.StudentProfileId == studentId)
            .Select(d => new GeneratedDocumentInfo(
                d.Id,
                AcademicDocumentType.Degree,
                d.StudentProfileId,
                d.SerialNumber,
                d.IssueDate,
                d.GeneratedAtUtc,
                d.DocxPath,
                d.PdfPath,
                string.Empty,
                d.VerificationUrl))
            .ToListAsync(ct);

        var transcriptDocs = await _dbContext.TranscriptDocuments.AsNoTracking()
            .Where(d => d.StudentProfileId == studentId)
            .Select(d => new GeneratedDocumentInfo(
                d.Id,
                AcademicDocumentType.Transcript,
                d.StudentProfileId,
                d.SerialNumber,
                d.IssueDate,
                d.GeneratedAtUtc,
                d.DocxPath,
                d.PdfPath,
                string.Empty,
                d.VerificationUrl))
            .ToListAsync(ct);

        documents.AddRange(degreeDocs);
        documents.AddRange(transcriptDocs);

        return documents
            .GroupBy(d => d.DocumentId)
            .Select(g => g.OrderByDescending(d => d.GeneratedAtUtc).First())
            .OrderByDescending(d => d.GeneratedAtUtc)
            .ToList();
    }
}

public enum AcademicDocumentType
{
    Degree = 1,
    Transcript = 2
}

public sealed record GeneratedDocumentInfo(
    Guid DocumentId,
    AcademicDocumentType Type,
    Guid StudentId,
    string SerialNumber,
    string IssueDate,
    DateTime GeneratedAtUtc,
    string DocxPath,
    string? PdfPath,
    string QrImagePath,
    string VerificationUrl);

public sealed record DegreeGenerationRequest(
    Guid StudentId,
    string StudentName,
    string FatherName,
    string DegreeTitle,
    string Cgpa,
    string? IssueDate = null,
    string? SerialNumber = null)
{
    public DocumentTemplatePayload ToPayload() => new(
        StudentName,
        FatherName,
        DegreeTitle,
        Cgpa,
        IssueDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
        SerialNumber ?? $"DEG-{DateTime.UtcNow:yyyyMMddHHmmss}",
        VerificationUrl: "{{QR_CODE}}");
}

public sealed record TranscriptGenerationRequest(
    Guid StudentId,
    string StudentName,
    string FatherName,
    string DegreeTitle,
    string Cgpa,
    IReadOnlyList<TranscriptCourseRow> Courses,
    string? IssueDate = null,
    string? SerialNumber = null)
{
    public DocumentTemplatePayload ToPayload() => new(
        StudentName,
        FatherName,
        DegreeTitle,
        Cgpa,
        IssueDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
        SerialNumber ?? $"TRN-{DateTime.UtcNow:yyyyMMddHHmmss}",
        VerificationUrl: "{{QR_CODE}}");
}
