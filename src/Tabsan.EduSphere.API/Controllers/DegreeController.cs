using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Degree/Transcript Generation add-on controller: isolated degree template download and generation endpoints.
/// Existing controllers and authentication flow remain unchanged.
/// </summary>
[ApiController]
[Route("api/v1/degree")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class DegreeController : ControllerBase
{
    private readonly TemplateExportService _templateExportService;
    private readonly DocumentGenerationService _documentGenerationService;

    public DegreeController(
        TemplateExportService templateExportService,
        DocumentGenerationService documentGenerationService)
    {
        _templateExportService = templateExportService;
        _documentGenerationService = documentGenerationService;
    }

    [HttpGet("template/default")]
    public async Task<IActionResult> DownloadDefaultTemplate(CancellationToken ct)
    {
        var template = await _templateExportService.GetDegreeTemplateAsync(ct);
        return File(template.Content, template.ContentType, template.FileName);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] DegreeGenerationRequest request, CancellationToken ct)
    {
        var document = await _documentGenerationService.GenerateDegreeAsync(request, ct);
        return Ok(document);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId, [FromQuery] string format = "docx", CancellationToken ct = default)
    {
        var doc = await _documentGenerationService.GetAsync(documentId, ct);
        if (doc is null || !System.IO.File.Exists(doc.DocxPath))
        {
            return NotFound();
        }

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(doc.PdfPath)
            && System.IO.File.Exists(doc.PdfPath))
        {
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(doc.PdfPath, ct);
            return File(pdfBytes, "application/pdf", Path.GetFileName(doc.PdfPath));
        }

        var docxBytes = await System.IO.File.ReadAllBytesAsync(doc.DocxPath, ct);
        return File(docxBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Path.GetFileName(doc.DocxPath));
    }

    // Required by Degree/Transcript Generation add-on: additive student endpoint only.
    [HttpGet("/student/degree")]
    [Authorize(Roles = "Student,Admin,SuperAdmin")]
    public async Task<IActionResult> StudentDegree(CancellationToken ct)
    {
        var userId = ResolveCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Forbid();
        }

        var docs = await _documentGenerationService.ListByStudentAsync(userId, ct);
        return Ok(docs.Where(d => d.Type == AcademicDocumentType.Degree));
    }

    private Guid ResolveCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
