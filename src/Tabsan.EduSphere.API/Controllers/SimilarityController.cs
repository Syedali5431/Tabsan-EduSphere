using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Similarity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M9 — similarity (plagiarism) checking under /api/v1/similarity, gated by the licensed "similarity" module.
/// Settings: Admin/SuperAdmin of the tenant (SuperAdmin may also set the installation default). Reports: staff of the
/// course's tenant/campus. Students: their own score when the assignment shows it. The provider webhook is anonymous and
/// authenticated by a one-time token per check.
/// </summary>
[ApiController]
[Route("api/v1/similarity")]
[Authorize]
public sealed class SimilarityController : ControllerBase
{
    private const string Staff = "SuperAdmin,Admin,Faculty";

    private readonly SimilarityService _service;
    private readonly ISimilarityRepository _repo;
    private readonly IAccessScopeResolver _scope;
    private readonly ApplicationDbContext _db;

    public SimilarityController(SimilarityService service, ISimilarityRepository repo, IAccessScopeResolver scope, ApplicationDbContext db)
    {
        _service = service;
        _repo = repo;
        _scope = scope;
        _db = db;
    }

    // ── settings ──

    [HttpGet("settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetSettings([FromQuery] bool installationDefault, CancellationToken ct)
    {
        var (tenant, denied) = TenantFor(installationDefault);
        if (denied is not null) return denied;
        return Ok(await _service.GetSettingsAsync(tenant, ct));
    }

    [HttpPut("settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SaveSettings([FromBody] SaveSimilaritySettingsRequest request, [FromQuery] bool installationDefault, CancellationToken ct)
    {
        var (tenant, denied) = TenantFor(installationDefault);
        if (denied is not null) return denied;
        return ToResult(await _service.SaveSettingsAsync(tenant, request, UserId(), ct));
    }

    [HttpPost("settings/test")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Test([FromQuery] bool installationDefault, CancellationToken ct)
    {
        var (tenant, denied) = TenantFor(installationDefault);
        if (denied is not null) return denied;
        var result = await _service.TestAsync(tenant, ct);
        return result.Ok ? Ok(new { message = result.Value }) : StatusCode(result.Status, new { message = result.Error });
    }

    /// <summary>The tenant whose settings are edited: the caller's; SuperAdmin without a tenant (or asking for it) edits the installation default.</summary>
    private (Guid? Tenant, IActionResult? Denied) TenantFor(bool installationDefault)
    {
        var tenant = _scope.GetTenantId();
        if (_scope.IsSuperAdmin()) return (installationDefault ? null : tenant, null);
        if (installationDefault) return (null, Forbid());
        return (tenant, null);
    }

    // ── assignments ──

    [HttpGet("assignments/{assignmentId:guid}/settings")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> GetAssignmentSettings(Guid assignmentId, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return await _service.GetAssignmentSettingsAsync(assignmentId, ct) is { } s ? Ok(s) : NotFound();
    }

    [HttpPut("assignments/{assignmentId:guid}/settings")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> SaveAssignmentSettings(Guid assignmentId, [FromBody] SaveAssignmentSimilarityRequest request, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return ToResult(await _service.SaveAssignmentSettingsAsync(assignmentId, request, UserId(), ct));
    }

    [HttpGet("assignments/{assignmentId:guid}/scores")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Scores(Guid assignmentId, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return Ok(await _service.AssignmentScoresAsync(assignmentId, ct));
    }

    // ── submissions and reports ──

    [HttpGet("submissions/{submissionId:guid}/checks")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Checks(Guid submissionId, CancellationToken ct)
    {
        if (await SubmissionDeniedAsync(submissionId, ct) is { } denied) return denied;
        return Ok(await _service.SubmissionChecksAsync(submissionId, ct));
    }

    [HttpPost("submissions/{submissionId:guid}/recheck")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Recheck(Guid submissionId, CancellationToken ct)
    {
        if (await SubmissionDeniedAsync(submissionId, ct) is { } denied) return denied;
        return ToResult(await _service.RecheckAsync(submissionId, UserId(), ct));
    }

    [HttpGet("checks/{checkId:guid}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Report(Guid checkId, CancellationToken ct)
    {
        if (await _service.OfferingOfCheckAsync(checkId, ct) is not { } offeringId) return NotFound();
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        return await _service.ReportAsync(checkId, ct) is { } report ? Ok(report) : NotFound();
    }

    [HttpGet("my/submissions/{submissionId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Mine(Guid submissionId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId) return Forbid();
        return await _service.StudentViewAsync(submissionId, studentId, ct) is { } view ? Ok(view) : NotFound();
    }

    // ── provider webhook ──

    /// <summary>Copyleaks status webhook (completed / error / creditsChecked / indexed).</summary>
    [HttpPost("webhooks/copyleaks/{status}")]
    [AllowAnonymous]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> CopyleaksWebhook(string status, [FromQuery] Guid check, [FromQuery] string? token, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        return await _service.HandleCopyleaksWebhookAsync(check, token, status, body, DateTime.UtcNow, ct) ? Ok() : NotFound();
    }

    // ── helpers ──

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private async Task<Guid?> StudentAsync(CancellationToken ct)
    {
        if (Guid.TryParse(User.FindFirstValue("studentProfileId"), out var id)) return id;
        var userId = UserId();
        return await _db.StudentProfiles.AsNoTracking().Where(s => s.UserId == userId).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
    }

    private async Task<IActionResult?> AssignmentDeniedAsync(Guid assignmentId, CancellationToken ct)
        => await _repo.GetAssignmentAsync(assignmentId, ct) is { } a ? await OfferingDeniedAsync(a.CourseOfferingId, ct) : NotFound();

    private async Task<IActionResult?> SubmissionDeniedAsync(Guid submissionId, CancellationToken ct)
        => await _repo.GetSubmissionAsync(submissionId, ct) is { } s ? await OfferingDeniedAsync(s.CourseOfferingId, ct) : NotFound();

    private async Task<IActionResult?> OfferingDeniedAsync(Guid offeringId, CancellationToken ct)
    {
        if (await _repo.GetOfferingScopeAsync(offeringId, ct) is not { } scope) return NotFound();
        if (_scope.IsSuperAdmin()) return null;
        var tenant = _scope.GetTenantId();
        var campus = _scope.GetCampusId();
        if (tenant.HasValue && scope.TenantId != tenant) return Forbid();
        if (campus.HasValue && scope.CampusId != campus) return Forbid();
        return null;
    }

    private IActionResult ToResult<T>(SimilarityResult<T> result)
        => result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
}
