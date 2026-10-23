using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Outcomes;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Outcomes;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M6 — learning outcomes, alignments, Learning Mastery Gradebook, student mastery and program attainment.
/// Staff work inside their tenant/campus; students only see their own mastery. Module key "outcomes".
/// </summary>
[ApiController]
[Route("api/v1/outcomes")]
[Authorize]
public sealed class OutcomesController : ControllerBase
{
    private const long MaxImportBytes = 5 * 1024 * 1024;
    private readonly OutcomeService _outcomes;
    private readonly OutcomeReportService _reports;
    private readonly IOutcomeDocuments _documents;
    private readonly IOutcomeRepository _repo;
    private readonly IAccessScopeResolver _scope;
    private readonly IPortalBrandingService _branding;
    private readonly ApplicationDbContext _db;

    public OutcomesController(OutcomeService outcomes, OutcomeReportService reports, IOutcomeDocuments documents, IOutcomeRepository repo,
        IAccessScopeResolver scope, IPortalBrandingService branding, ApplicationDbContext db)
    {
        _outcomes = outcomes;
        _reports = reports;
        _documents = documents;
        _repo = repo;
        _scope = scope;
        _branding = branding;
        _db = db;
    }

    // ── outcomes ──

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> List([FromQuery] string? level, [FromQuery] Guid? programId, [FromQuery] Guid? courseId, [FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _outcomes.ListAsync(Actor(), level, programId, courseId, includeInactive, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => await _outcomes.GetAsync(Actor(), id, ct) is { } outcome ? Ok(outcome) : NotFound();

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Create([FromBody] SaveOutcomeRequest request, CancellationToken ct)
        => ToResult(await _outcomes.CreateAsync(Actor(), request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveOutcomeRequest request, CancellationToken ct)
        => ToResult(await _outcomes.UpdateAsync(Actor(), id, request, ct));

    [HttpPost("{id:guid}/{state:regex(^(activate|deactivate)$)}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> SetActive(Guid id, string state, CancellationToken ct)
    {
        var result = await _outcomes.SetActiveAsync(Actor(), id, state == "activate", ct);
        return result.Ok ? NoContent() : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpPut("{id:guid}/mappings")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> SetMappings(Guid id, [FromBody] SetOutcomeMappingsRequest request, CancellationToken ct)
        => ToResult(await _outcomes.SetMappingsAsync(Actor(), id, request, ct));

    [HttpGet("import-template")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public IActionResult ImportTemplate()
        => File(_documents.ImportTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "outcomes-import-template.xlsx");

    [HttpPost("import")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    [RequestSizeLimit(MaxImportBytes + 64 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose the Excel file to import." });
        if (file.Length > MaxImportBytes) return BadRequest(new { message = "The file is larger than 5 MB." });
        await using var stream = file.OpenReadStream();
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, ct);
        copy.Position = 0;
        var (rows, error) = _documents.ReadImport(copy);
        if (error is not null) return BadRequest(new { message = error });
        return Ok(await _outcomes.ImportAsync(Actor(), rows, ct));
    }

    [HttpGet("programs")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Programs(CancellationToken ct) => Ok(await _repo.GetProgramsAsync(null, null, ct));

    [HttpGet("available")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Available([FromQuery] Guid offeringId, CancellationToken ct)
    {
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _outcomes.AvailableForOfferingAsync(Actor(), offeringId, ct));
    }

    // ── alignments ──

    [HttpGet("alignments")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Alignments([FromQuery] string targetType, [FromQuery] Guid targetId, CancellationToken ct)
    {
        if (await TargetDeniedAsync(targetType, targetId, ct) is { } denied) return denied;
        return ToResult(await _outcomes.GetAlignmentsAsync(Actor(), targetType, targetId, ct));
    }

    [HttpPut("alignments")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> SetAlignments([FromBody] SetAlignmentsRequest request, CancellationToken ct)
    {
        if (await TargetDeniedAsync(request.TargetType, request.TargetId, ct) is { } denied) return denied;
        return ToResult(await _outcomes.SetAlignmentsAsync(Actor(), request, ct));
    }

    // ── mastery ──

    [HttpGet("mastery/offerings/{offeringId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Mastery(Guid offeringId, CancellationToken ct)
    {
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        return await _reports.MasteryGradebookAsync(Actor(), offeringId, ct) is { } gradebook ? Ok(gradebook) : NotFound();
    }

    [HttpGet("mastery/offerings/{offeringId:guid}/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportMastery(Guid offeringId, CancellationToken ct)
    {
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        if (await _reports.MasteryGradebookAsync(Actor(), offeringId, ct) is not { } gradebook) return NotFound();
        return File(_documents.MasteryWorkbook(gradebook), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"mastery-{SafeName(gradebook.CourseCode)}.xlsx");
    }

    [HttpPost("mastery/offerings/{offeringId:guid}/recalculate")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Recalculate(Guid offeringId, CancellationToken ct)
    {
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        await _outcomes.RecalculateOfferingAsync(offeringId, ct);
        return NoContent();
    }

    [HttpGet("mastery/me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyMastery([FromQuery] Guid? offeringId, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("studentProfileId"), out var studentId))
            studentId = await _db.StudentProfiles.AsNoTracking().Where(s => s.UserId == UserId()).Select(s => s.Id).FirstOrDefaultAsync(ct);
        if (studentId == Guid.Empty) return Ok(Array.Empty<StudentMasteryCourse>());
        return Ok(await _reports.StudentMasteryAsync(studentId, offeringId, ct));
    }

    // ── program attainment ──

    [HttpGet("attainment/programs/{programId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Attainment(Guid programId, [FromQuery] Guid? semesterId, [FromQuery] decimal? target, CancellationToken ct)
        => await _reports.ProgramAttainmentAsync(Actor(), programId, semesterId, target ?? 70, ct) is { } report ? Ok(report) : NotFound();

    [HttpGet("attainment/programs/{programId:guid}/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportAttainment(Guid programId, [FromQuery] string? format, [FromQuery] Guid? semesterId, [FromQuery] decimal? target, CancellationToken ct)
    {
        if (await _reports.ProgramAttainmentAsync(Actor(), programId, semesterId, target ?? 70, ct) is not { } report) return NotFound();
        var name = $"plo-attainment-{SafeName(report.ProgramCode)}";
        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var institution = (await _branding.GetAsync(ct)).UniversityName;
            return File(_documents.AttainmentPdf(report, institution), "application/pdf", name + ".pdf");
        }
        return File(_documents.AttainmentWorkbook(report), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name + ".xlsx");
    }

    // ── helpers ──

    private OutcomeActor Actor()
    {
        var role = User.IsInRole("SuperAdmin") ? "SuperAdmin" : User.IsInRole("Admin") ? "Admin" : User.IsInRole("Faculty") ? "Faculty"
            : User.FindFirstValue(ClaimTypes.Role) ?? "";
        return new OutcomeActor(UserId(), role, _scope.GetTenantId(), _scope.GetCampusId(), _scope.IsSuperAdmin());
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private async Task<IActionResult?> OfferingDeniedAsync(Guid offeringId, CancellationToken ct)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        if (offering is null) return NotFound();
        return Outside(offering.TenantId, offering.CampusId, checkCampus: true) ? Forbid() : null;
    }

    private async Task<IActionResult?> TargetDeniedAsync(string? targetType, Guid targetId, CancellationToken ct)
    {
        if (!Enum.TryParse<OutcomeTargetType>(targetType, true, out var type) || !Enum.IsDefined(type)) return BadRequest(new { message = "Unknown target type." });
        var target = await _repo.GetTargetAsync(type, targetId, ct);
        if (target is null) return NotFound();
        if (target.CourseOfferingId is { } offeringId) return await OfferingDeniedAsync(offeringId, ct);
        return Outside(target.TenantId, null, checkCampus: false) ? Forbid() : null; // question banks have no campus
    }

    private bool Outside(Guid? tenantId, Guid? campusId, bool checkCampus)
    {
        if (_scope.IsSuperAdmin()) return false;
        var tenant = _scope.GetTenantId();
        var campus = _scope.GetCampusId();
        return (tenant.HasValue && tenantId != tenant) || (checkCampus && campus.HasValue && campusId != campus);
    }

    private IActionResult ToResult<T>(OutcomeResult<T> result)
        => result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });

    private static string SafeName(string value) => new(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
}
