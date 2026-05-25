using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;
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
    private readonly TemplateUploadService _templateUploadService;
    private readonly DocumentGenerationService _documentGenerationService;
    private readonly IFeatureFlagService _featureFlags;

    public DegreeController(
        TemplateExportService templateExportService,
        TemplateUploadService templateUploadService,
        DocumentGenerationService documentGenerationService,
        IFeatureFlagService featureFlags)
    {
        _templateExportService = templateExportService;
        _templateUploadService = templateUploadService;
        _documentGenerationService = documentGenerationService;
        _featureFlags = featureFlags;
    }

    [HttpGet("template/default")]
    public async Task<IActionResult> DownloadDefaultTemplate(CancellationToken ct)
    {
        var gate = await EnsureDegreeTranscriptGenerationEnabledAsync(ct);
        if (gate is not null)
            return gate;

        var template = await _templateExportService.GetDegreeTemplateAsync(ct);
        return File(template.Content, template.ContentType, template.FileName);
    }

    [HttpPost("template/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTemplate([FromForm] IFormFile file, CancellationToken ct)
    {
        var gate = await EnsureDegreeTranscriptGenerationEnabledAsync(ct);
        if (gate is not null)
            return gate;

        try
        {
            var result = await _templateUploadService.UploadDegreeTemplateAsync(file, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] DegreeGenerationRequest request, CancellationToken ct)
    {
        var gate = await EnsureDegreeTranscriptGenerationEnabledAsync(ct);
        if (gate is not null)
            return gate;

        var document = await _documentGenerationService.GenerateDegreeAsync(request, ct);
        return Ok(document);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId, [FromQuery] string format = "docx", CancellationToken ct = default)
    {
        var gate = await EnsureDegreeTranscriptGenerationEnabledAsync(ct);
        if (gate is not null)
            return gate;

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
        var gate = await EnsureDegreeTranscriptGenerationEnabledAsync(ct);
        if (gate is not null)
            return gate;

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

    private async Task<IActionResult?> EnsureDegreeTranscriptGenerationEnabledAsync(CancellationToken ct)
    {
        var flag = await _featureFlags.GetAsync("degree-transcript-generation.enabled", ct);
        if (!flag.IsEnabled)
        {
            return StatusCode(StatusCodes.Status423Locked, new { message = "Degree/Transcript generation is currently disabled by feature flag." });
        }

        return null;
    }
}
