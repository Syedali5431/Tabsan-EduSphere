using System.Collections.Concurrent;

namespace Tabsan.EduSphere.API.Services.PlanK;

/// <summary>
/// Plan K add-on service: orchestrates degree/transcript generation in an isolated pipeline.
/// This service intentionally does not alter existing generation/storage services.
/// </summary>
public sealed class DocumentGenerationService
{
    private readonly TemplateExportService _templateExport;
    private readonly TemplateProcessorService _templateProcessor;
    private readonly QRCodeService _qrCodeService;
    private readonly ILogger<DocumentGenerationService> _logger;
    private readonly ConcurrentDictionary<Guid, GeneratedDocumentInfo> _documents = new();

    public DocumentGenerationService(
        TemplateExportService templateExport,
        TemplateProcessorService templateProcessor,
        QRCodeService qrCodeService,
        ILogger<DocumentGenerationService> logger)
    {
        _templateExport = templateExport;
        _templateProcessor = templateProcessor;
        _qrCodeService = qrCodeService;
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
        _documents.TryGetValue(documentId, out var item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<GeneratedDocumentInfo>> ListByStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var list = _documents.Values
            .Where(d => d.StudentId == studentId)
            .OrderByDescending(d => d.GeneratedAtUtc)
            .ToList();
        return Task.FromResult((IReadOnlyList<GeneratedDocumentInfo>)list);
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

        var outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "Artifacts", "PlanK", "generated-documents");
        Directory.CreateDirectory(outputRoot);

        var baseName = $"{type.ToString().ToLowerInvariant()}-{serial}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var docxPath = Path.Combine(outputRoot, $"{baseName}.docx");
        await File.WriteAllBytesAsync(docxPath, processed, ct);

        var qrPath = Path.Combine(outputRoot, $"{baseName}.qr.png");
        await File.WriteAllBytesAsync(qrPath, qrBytes, ct);

        // Safe fallback policy: PDF is optional and unavailable until an explicit converter adapter is plugged in.
        string? pdfPath = null;

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
        _logger.LogInformation("PlanK generated {Type} document {DocumentId} for Student {StudentId}", type, documentId, studentId);
        return info;
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
