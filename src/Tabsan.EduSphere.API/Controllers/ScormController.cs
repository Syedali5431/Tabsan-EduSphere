using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Lms.Scorm;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M13.1 — SCORM packages. Staff upload and manage packages in their tenant/campus; enrolled students launch them. The
/// player (served by the portal) talks to the <c>play/{token}</c> endpoints: they need no sign-in because the play link
/// itself carries the attempt (sealed, time-limited), so the content can run on a separate, cookie-less content origin.
/// </summary>
[ApiController]
[Route("api/v1/scorm")]
[Authorize]
public sealed class ScormController : ControllerBase
{
    /// <summary>Upper bound for an upload request; the configured Scorm:MaxPackageMb is checked below it.</summary>
    private const long MaxRequestBytes = 1_100_000_000;

    private static readonly FileExtensionContentTypeProvider ContentTypes = CreateContentTypes();

    private readonly ScormService _service;
    private readonly IScormRepository _repo;
    private readonly IAccessScopeResolver _scope;

    public ScormController(ScormService service, IScormRepository repo, IAccessScopeResolver scope)
    {
        _service = service;
        _repo = repo;
        _scope = scope;
    }

    // ── staff ──

    [HttpGet("offerings/{offeringId:guid}/packages")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Packages(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _service.ListAsync(offeringId, null, ct));
    }

    /// <summary>Uploads a SCORM zip (form fields: file, title, graded, maxMarks, maxAttempts, publish).</summary>
    [HttpPost("offerings/{offeringId:guid}/packages")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Upload(Guid offeringId, IFormFile? file, [FromForm] string? title, [FromForm] bool graded, [FromForm] decimal? maxMarks,
        [FromForm] int maxAttempts, [FromForm] bool publish, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose the package's zip file." });
        if (file.Length > _service.MaxUploadBytes) return StatusCode(StatusCodes.Status413PayloadTooLarge, new { message = "The package is too large." });

        // ZipArchive needs a seekable stream; work from a temporary copy that is deleted when closed.
        var temp = Path.Combine(Path.GetTempPath(), $"scorm-{Guid.NewGuid():N}.zip");
        await using var zip = new FileStream(temp, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await file.CopyToAsync(zip, ct);
        zip.Position = 0;
        var result = await _service.ImportAsync(offeringId, zip, file.FileName, new ScormImportRequest(title, graded, maxMarks, maxAttempts, publish), UserId(), ct);
        return result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpPut("packages/{packageId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Update(Guid packageId, [FromBody] ScormUpdateRequest request, CancellationToken ct)
    {
        if (await PackageDeniedAsync(packageId, ct) is { } denied) return denied;
        var result = await _service.UpdateAsync(packageId, request, UserId(), ct);
        return result.Ok ? Ok(result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpDelete("packages/{packageId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Remove(Guid packageId, CancellationToken ct)
    {
        if (await PackageDeniedAsync(packageId, ct) is { } denied) return denied;
        var result = await _service.RemoveAsync(packageId, UserId(), ct);
        return result.Ok ? NoContent() : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpGet("packages/{packageId:guid}/report")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Report(Guid packageId, CancellationToken ct)
    {
        if (await PackageDeniedAsync(packageId, ct) is { } denied) return denied;
        return await _service.ReportAsync(packageId, ct) is { } report ? Ok(report) : NotFound(new { message = "Package not found." });
    }

    /// <summary>A play link for staff to try the package (review mode: nothing is recorded).</summary>
    [HttpPost("packages/{packageId:guid}/preview")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Preview(Guid packageId, CancellationToken ct)
    {
        if (await PackageDeniedAsync(packageId, ct) is { } denied) return denied;
        var result = await _service.PreviewAsync(packageId, ct);
        return result.Ok ? Ok(result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    // ── students ──

    [HttpGet("offerings/{offeringId:guid}/my-packages")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyPackages(Guid offeringId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } student) return Forbid();
        if (!await _repo.IsEnrolledAsync(offeringId, student, ct)) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not enrolled in this course." });
        return Ok(await _service.ListAsync(offeringId, student, ct));
    }

    [HttpGet("packages/{packageId:guid}/mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Mine(Guid packageId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } student) return Forbid();
        var package = await _service.GetAsync(packageId, student, ct);
        if (package is null || !await _repo.IsEnrolledAsync(package.OfferingId, student, ct)) return NotFound(new { message = "This package is not available." });
        return Ok(package);
    }

    [HttpPost("packages/{packageId:guid}/launch")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Launch(Guid packageId, [FromBody] ScormLaunchRequest? request, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } student) return Forbid();
        var result = await _service.LaunchAsync(packageId, student, request?.NewAttempt ?? false, ct);
        return result.Ok ? Ok(result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    // ── player (play link instead of sign-in) ──

    [HttpGet("play/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> Play(string token, [FromQuery] string? part, CancellationToken ct)
    {
        var result = await _service.PlayAsync(token, ct, part);
        return result.Ok ? Ok(result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    /// <summary>Multi-part packages: each part's status for the player's contents list.</summary>
    [HttpGet("play/{token}/parts")]
    [AllowAnonymous]
    public async Task<IActionResult> Parts(string token, CancellationToken ct)
    {
        var result = await _service.PartsAsync(token, ct);
        return result.Ok ? Ok(result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    /// <summary>The runtime's commit: the CMI object as JSON (sendBeacon sends it as text/plain). Answers as scorm-again expects.</summary>
    [HttpPost("play/{token}/commit")]
    [HttpPost("play/{token}/commit/{part}")]
    [AllowAnonymous]
    [RequestSizeLimit(4_500_000)]
    public async Task<IActionResult> Commit(string token, string? part, [FromQuery] bool final, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);
        var result = await _service.CommitAsync(token, body, final, ct, part);
        return result.Ok ? Ok(result.Value) : StatusCode(result.Status, new { result = false, errorCode = 101, message = result.Error });
    }

    [HttpGet("play/{token}/content/{**path}")]
    [AllowAnonymous]
    public async Task<IActionResult> Content(string token, string path, CancellationToken ct)
    {
        if (await _service.OpenContentAsync(token, path, ct) is not { } file) return NotFound();
        if (!ContentTypes.TryGetContentType(file.Path, out var type)) type = "application/octet-stream";
        Response.Headers.CacheControl = "private, max-age=3600";
        return File(file.Content, type, enableRangeProcessing: true);
    }

    // ── pieces ──

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<Guid?> StudentAsync(CancellationToken ct)
        => Guid.TryParse(User.FindFirstValue("studentProfileId"), out var id) ? id : await _repo.GetStudentProfileIdAsync(UserId(), ct);

    private async Task<IActionResult?> DeniedAsync(Guid offeringId, CancellationToken ct)
    {
        var scope = await _repo.GetOfferingScopeAsync(offeringId, ct);
        if (scope is null) return NotFound(new { message = "Course offering not found." });
        if (_scope.IsSuperAdmin()) return null;
        var tenant = _scope.GetTenantId();
        var campus = _scope.GetCampusId();
        if (tenant.HasValue && scope.Value.TenantId != tenant) return Forbid();
        if (campus.HasValue && scope.Value.CampusId != campus) return Forbid();
        return null;
    }

    private async Task<IActionResult?> PackageDeniedAsync(Guid packageId, CancellationToken ct)
    {
        var package = await _repo.GetPackageAsync(packageId, ct);
        if (package is null || package.IsRemoved) return NotFound(new { message = "Package not found." });
        return await DeniedAsync(package.OfferingId, ct);
    }

    private static FileExtensionContentTypeProvider CreateContentTypes()
    {
        var provider = new FileExtensionContentTypeProvider();
        // Types SCORM authoring tools use that the defaults miss or get wrong.
        provider.Mappings[".js"] = "text/javascript";
        provider.Mappings[".mjs"] = "text/javascript";
        provider.Mappings[".json"] = "application/json";
        provider.Mappings[".xml"] = "application/xml";
        provider.Mappings[".xsd"] = "application/xml";
        provider.Mappings[".woff"] = "font/woff";
        provider.Mappings[".woff2"] = "font/woff2";
        provider.Mappings[".vtt"] = "text/vtt";
        provider.Mappings[".swf"] = "application/x-shockwave-flash";
        return provider;
    }
}
