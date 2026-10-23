using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M3 — submission history, comments, annotations and the grading workspace.
/// Lives under /api/v1/assignment so the "assignments" module licence gates it.
/// Students reach only their own submission (checked by the service); staff are limited to
/// their tenant/campus (checked here through the assignment's course offering).
/// Files are always returned as attachments with nosniff, so uploaded content never runs in a page.
/// </summary>
[ApiController]
[Route("api/v1/assignment")]
[Authorize]
public sealed class SubmissionWorkspaceController : ControllerBase
{
    private const long MaxRequestBytes = (Assignment.MaxAttemptTotalMb + 5) * 1024L * 1024L;

    private readonly ISubmissionWorkspaceService _service;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public SubmissionWorkspaceController(ISubmissionWorkspaceService service, IAccessScopeResolver accessScope, ApplicationDbContext db)
    {
        _service = service;
        _accessScope = accessScope;
        _db = db;
    }

    // ── Settings ────────────────────────────────────────────────────────────

    [HttpGet("{assignmentId:guid}/settings")]
    public async Task<IActionResult> GetSettings(Guid assignmentId, CancellationToken ct)
    {
        if (await DeniedAsync(assignmentId: assignmentId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.GetSettingsAsync(assignmentId, ct));
    }

    [HttpPut("{assignmentId:guid}/settings")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> SaveSettings(Guid assignmentId, [FromBody] AssignmentSettingsRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(assignmentId: assignmentId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.SaveSettingsAsync(Actor(), assignmentId, request, ct));
    }

    // ── Students ────────────────────────────────────────────────────────────

    [HttpGet("{assignmentId:guid}/student-view")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StudentView(Guid assignmentId, CancellationToken ct)
        => ToResult(await _service.GetStudentViewAsync(Actor(), assignmentId, ct));

    /// <summary>Submits (or resubmits) with files and/or text. Multipart: <c>textContent</c>, <c>files</c>.</summary>
    [HttpPost("{assignmentId:guid}/submissions")]
    [Authorize(Roles = "Student")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Submit(Guid assignmentId, [FromForm] string? textContent, List<IFormFile>? files, CancellationToken ct)
    {
        // Plan M7: an assignment placed in a locked module cannot be submitted yet.
        if (await Services.ModuleGate.LockReasonAsync(HttpContext, Actor().StudentProfileId, Domain.Lms.ModuleItemType.Assignment, assignmentId, ct) is { } locked)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = locked });
        var incoming = new List<IncomingFile>();
        var streams = new List<Stream>();
        try
        {
            foreach (var f in files ?? [])
            {
                var s = f.OpenReadStream();
                streams.Add(s);
                incoming.Add(new IncomingFile(f.FileName, f.ContentType, f.Length, s));
            }
            var result = await _service.SubmitAsync(Actor(), assignmentId, textContent, incoming, ct);
            return result.Ok ? Ok(result.Value) : ToResult(result);
        }
        finally
        {
            foreach (var s in streams) await s.DisposeAsync();
        }
    }

    // ── Shared ──────────────────────────────────────────────────────────────

    [HttpGet("submissions/{submissionId:guid}")]
    public async Task<IActionResult> GetSubmission(Guid submissionId, CancellationToken ct)
    {
        if (await DeniedAsync(submissionId: submissionId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.GetSubmissionAsync(Actor(), submissionId, ct));
    }

    [HttpGet("submission-files/{fileId:guid}")]
    public async Task<IActionResult> DownloadFile(Guid fileId, [FromQuery] bool pdf, CancellationToken ct)
    {
        if (await DeniedAsync(fileId: fileId, ct: ct) is { } denied) return denied;
        return ToFile(await _service.DownloadFileAsync(Actor(), fileId, pdf, ct));
    }

    [HttpGet("submission-files/{fileId:guid}/annotated")]
    public async Task<IActionResult> DownloadAnnotated(Guid fileId, CancellationToken ct)
    {
        if (await DeniedAsync(fileId: fileId, ct: ct) is { } denied) return denied;
        return ToFile(await _service.DownloadAnnotatedPdfAsync(Actor(), fileId, ct));
    }

    [HttpGet("submission-files/{fileId:guid}/annotations")]
    public async Task<IActionResult> GetAnnotations(Guid fileId, CancellationToken ct)
    {
        if (await DeniedAsync(fileId: fileId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.GetAnnotationsAsync(Actor(), fileId, ct));
    }

    /// <summary>Adds a comment. Multipart: <c>body</c>, <c>attemptNumber</c>, <c>isPrivate</c> (staff), <c>media</c> (audio/video).</summary>
    [HttpPost("submissions/{submissionId:guid}/comments")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024)]
    public async Task<IActionResult> AddComment(Guid submissionId, [FromForm] string? body, [FromForm] int? attemptNumber,
        [FromForm] bool isPrivate, IFormFile? media, CancellationToken ct)
    {
        if (await DeniedAsync(submissionId: submissionId, ct: ct) is { } denied) return denied;
        await using var stream = media?.OpenReadStream();
        var incoming = media is null ? null : new IncomingFile(media.FileName, media.ContentType, media.Length, stream!);
        return ToResult(await _service.AddCommentAsync(Actor(), submissionId, attemptNumber, body, isPrivate, incoming, ct));
    }

    [HttpDelete("submission-comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct)
    {
        if (await DeniedAsync(commentId: commentId, ct: ct) is { } denied) return denied;
        var result = await _service.DeleteCommentAsync(Actor(), commentId, ct);
        return result.Ok ? NoContent() : ToResult(result);
    }

    [HttpGet("submission-comments/{commentId:guid}/media")]
    public async Task<IActionResult> CommentMedia(Guid commentId, CancellationToken ct)
    {
        if (await DeniedAsync(commentId: commentId, ct: ct) is { } denied) return denied;
        return ToFile(await _service.DownloadCommentMediaAsync(Actor(), commentId, ct));
    }

    // ── Graders ─────────────────────────────────────────────────────────────

    [HttpGet("{assignmentId:guid}/workspace")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Workspace(Guid assignmentId, [FromQuery] string? filter, CancellationToken ct)
    {
        if (await DeniedAsync(assignmentId: assignmentId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.GetWorkspaceAsync(Actor(), assignmentId, filter, ct));
    }

    [HttpPut("submissions/{submissionId:guid}/grade")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> SaveGrade(Guid submissionId, [FromBody] SaveGradeRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(submissionId: submissionId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.SaveGradeAsync(Actor(), submissionId, request, ct));
    }

    [HttpPost("submissions/{submissionId:guid}/rubric")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ScoreRubric(Guid submissionId, [FromBody] RubricScoreRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(submissionId: submissionId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.ScoreRubricAsync(Actor(), submissionId, request, ct));
    }

    [HttpPost("submissions/{submissionId:guid}/moderate")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Moderate(Guid submissionId, [FromBody] ModerateRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(submissionId: submissionId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.ModerateAsync(Actor(), submissionId, request, ct));
    }

    [HttpPost("submission-files/{fileId:guid}/annotations")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> AddAnnotation(Guid fileId, [FromBody] SaveAnnotationRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(fileId: fileId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.AddAnnotationAsync(Actor(), fileId, request, ct));
    }

    [HttpPut("submission-annotations/{annotationId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> UpdateAnnotation(Guid annotationId, [FromBody] SaveAnnotationRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(annotationId: annotationId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.UpdateAnnotationAsync(Actor(), annotationId, request, ct));
    }

    [HttpDelete("submission-annotations/{annotationId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> DeleteAnnotation(Guid annotationId, CancellationToken ct)
    {
        if (await DeniedAsync(annotationId: annotationId, ct: ct) is { } denied) return denied;
        var result = await _service.DeleteAnnotationAsync(Actor(), annotationId, ct);
        return result.Ok ? NoContent() : ToResult(result);
    }

    [HttpPost("submission-files/{fileId:guid}/reconvert")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Reconvert(Guid fileId, CancellationToken ct)
    {
        if (await DeniedAsync(fileId: fileId, ct: ct) is { } denied) return denied;
        var result = await _service.RequeueConversionAsync(Actor(), fileId, ct);
        return result.Ok ? NoContent() : ToResult(result);
    }

    [HttpPost("{assignmentId:guid}/post-grades")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> PostGrades(Guid assignmentId, [FromBody] PostGradesRequest? request, CancellationToken ct)
    {
        if (await DeniedAsync(assignmentId: assignmentId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.PostGradesAsync(Actor(), assignmentId, request ?? new PostGradesRequest(null), ct));
    }

    [HttpPost("{assignmentId:guid}/hide-grades")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> HideGrades(Guid assignmentId, [FromBody] PostGradesRequest? request, CancellationToken ct)
    {
        if (await DeniedAsync(assignmentId: assignmentId, ct: ct) is { } denied) return denied;
        return ToResult(await _service.HideGradesAsync(Actor(), assignmentId, request ?? new PostGradesRequest(null), ct));
    }

    [HttpPost("{assignmentId:guid}/reveal-names")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> RevealNames(Guid assignmentId, CancellationToken ct)
    {
        if (await DeniedAsync(assignmentId: assignmentId, ct: ct) is { } denied) return denied;
        var result = await _service.RevealNamesAsync(Actor(), assignmentId, ct);
        return result.Ok ? Ok(new { revealed = result.Value }) : ToResult(result);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private bool IsStaff() => User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    private WorkspaceActor Actor()
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        Guid? studentProfileId = Guid.TryParse(User.FindFirstValue("studentProfileId"), out var sp) && User.IsInRole("Student") ? sp : null;
        return new WorkspaceActor(userId, studentProfileId, IsStaff(), User.IsInRole("Admin") || User.IsInRole("SuperAdmin"));
    }

    /// <summary>Staff may only act inside their tenant/campus. Students are checked for ownership by the service.</summary>
    private async Task<IActionResult?> DeniedAsync(Guid? assignmentId = null, Guid? submissionId = null, Guid? fileId = null,
        Guid? commentId = null, Guid? annotationId = null, CancellationToken ct = default)
    {
        var offeringId = await _service.ResolveOfferingAsync(assignmentId, submissionId, fileId, commentId, annotationId, ct);
        if (offeringId is null) return NotFound();
        if (!IsStaff() || _accessScope.IsSuperAdmin()) return null;

        var scope = await _db.CourseOfferings.AsNoTracking().Where(o => o.Id == offeringId.Value)
            .Select(o => new { o.TenantId, o.CampusId }).FirstOrDefaultAsync(ct);
        if (scope is null) return NotFound();
        var tenant = _accessScope.GetTenantId();
        var campus = _accessScope.GetCampusId();
        if (tenant.HasValue && scope.TenantId != tenant) return Forbid();
        if (campus.HasValue && scope.CampusId != campus) return Forbid();
        return null;
    }

    private IActionResult ToResult<T>(WorkspaceResult<T> result) => result.Error switch
    {
        WorkspaceError.None => Ok(result.Value),
        WorkspaceError.NotFound => NotFound(new { message = result.Message ?? "Not found." }),
        WorkspaceError.Forbidden => result.Message is null ? Forbid() : StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
        WorkspaceError.Conflict => Conflict(new { message = result.Message }),
        _ => BadRequest(new { message = result.Message })
    };

    private IActionResult ToFile(WorkspaceResult<FileDownload> result)
    {
        if (!result.Ok) return ToResult(result);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
}
