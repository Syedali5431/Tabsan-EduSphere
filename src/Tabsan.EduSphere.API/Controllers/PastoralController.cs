using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Pastoral;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Pastoral care and wellbeing (/api/v1/pastoral). Staff write notes about the students they teach or look after and see
/// a student's pastoral page; the wellbeing team and admins write support plans and follow up check-ins; students check in
/// and see what is shared with them; parents see what is shared about their children; admins set up the team.
/// </summary>
[ApiController]
[Route("api/v1/pastoral")]
[Authorize]
public sealed class PastoralController : ControllerBase
{
    private readonly PastoralService _pastoral;

    public PastoralController(PastoralService pastoral) => _pastoral = pastoral;

    /// <summary>What the user may do here.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => await ViewerAsync(ct) is { } v ? Ok(await _pastoral.MeAsync(v, ct)) : Unauthorized();

    /// <summary>Totals, students with notes, check-ins waiting and the user's follow-ups (?campusId= ?semesterId= ?courseOfferingId=
    /// ?from= ?to=; default: the last 30 days).</summary>
    [HttpGet("overview")]
    public Task<IActionResult> Overview([FromQuery] Guid? campusId, [FromQuery] Guid? semesterId, [FromQuery] Guid? courseOfferingId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var end = to?.ToUniversalTime() ?? DateTime.UtcNow;
        var start = from?.ToUniversalTime() ?? end.AddDays(-30);
        return As(v => _pastoral.OverviewAsync(v, new OverviewQuery(campusId, semesterId, courseOfferingId, start, end), DateTime.UtcNow, ct));
    }

    /// <summary>The campuses, year levels and classes the user can choose from.</summary>
    [HttpGet("filters")]
    public Task<IActionResult> Filters(CancellationToken ct) => As(v => _pastoral.FilterOptionsAsync(v, ct));

    /// <summary>The school's categories for notes.</summary>
    [HttpGet("categories")]
    public Task<IActionResult> Categories(CancellationToken ct) => As(v => _pastoral.CategoriesAsync(v, ct));

    /// <summary>Students the user may write about (?q= name or number, or ?courseOfferingId=).</summary>
    [HttpGet("students")]
    public Task<IActionResult> Students([FromQuery] string? q, [FromQuery] Guid? courseOfferingId, CancellationToken ct)
        => As(v => _pastoral.FindStudentsAsync(v, q, courseOfferingId, ct));

    [HttpGet("students/{id:guid}")]
    public Task<IActionResult> Student(Guid id, CancellationToken ct) => As(v => _pastoral.StudentAsync(v, id, DateTime.UtcNow, ct));

    // ── Notes ──

    /// <summary>Writes the same note for one or more students.</summary>
    [HttpPost("records")]
    public Task<IActionResult> CreateRecords([FromBody] SaveRecordRequest request, CancellationToken ct)
        => As(v => _pastoral.CreateRecordsAsync(v, request, DateTime.UtcNow, ct));

    [HttpPut("records/{id:guid}")]
    public Task<IActionResult> UpdateRecord(Guid id, [FromBody] SaveRecordRequest request, CancellationToken ct)
        => As(v => _pastoral.UpdateRecordAsync(v, id, request, DateTime.UtcNow, ct));

    [HttpPost("records/{id:guid}/notes")]
    public Task<IActionResult> AddNote(Guid id, [FromBody] PastoralTextRequest request, CancellationToken ct)
        => As(v => _pastoral.AddNoteAsync(v, id, request.Text, DateTime.UtcNow, ct));

    [HttpPost("records/{id:guid}/follow-up/done")]
    public Task<IActionResult> FollowUpDone(Guid id, CancellationToken ct) => As(v => _pastoral.SetFollowUpDoneAsync(v, id, true, DateTime.UtcNow, ct));

    [HttpPost("records/{id:guid}/follow-up/reopen")]
    public Task<IActionResult> FollowUpReopen(Guid id, CancellationToken ct) => As(v => _pastoral.SetFollowUpDoneAsync(v, id, false, DateTime.UtcNow, ct));

    /// <summary>Removes a note written in error (kept, with the reason, for the wellbeing team).</summary>
    [HttpPost("records/{id:guid}/remove")]
    public Task<IActionResult> RemoveRecord(Guid id, [FromBody] PastoralTextRequest request, CancellationToken ct)
        => As(v => _pastoral.RemoveRecordAsync(v, id, request.Text, DateTime.UtcNow, ct));

    // ── Support plans ──

    [HttpPost("plans")]
    public Task<IActionResult> CreatePlan([FromBody] SavePlanRequest request, CancellationToken ct) => As(v => _pastoral.SavePlanAsync(v, null, request, DateTime.UtcNow, ct));

    [HttpPut("plans/{id:guid}")]
    public Task<IActionResult> UpdatePlan(Guid id, [FromBody] SavePlanRequest request, CancellationToken ct)
        => As(v => _pastoral.SavePlanAsync(v, id, request, DateTime.UtcNow, ct));

    [HttpPost("plans/{id:guid}/end")]
    public Task<IActionResult> EndPlan(Guid id, CancellationToken ct) => As(v => _pastoral.SetPlanEndedAsync(v, id, true, DateTime.UtcNow, ct));

    [HttpPost("plans/{id:guid}/restart")]
    public Task<IActionResult> RestartPlan(Guid id, CancellationToken ct) => As(v => _pastoral.SetPlanEndedAsync(v, id, false, DateTime.UtcNow, ct));

    // ── Students and families ──

    /// <summary>A student's own page: shared notes and plans, and their check-ins.</summary>
    [HttpGet("mine")]
    public Task<IActionResult> Mine(CancellationToken ct) => As(v => _pastoral.MineAsync(v, ct));

    [HttpPost("check-ins")]
    public Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken ct) => As(v => _pastoral.CheckInAsync(v, request, DateTime.UtcNow, ct));

    [HttpPost("check-ins/{id:guid}/seen")]
    public Task<IActionResult> CheckInSeen(Guid id, [FromBody] PastoralTextRequest? request, CancellationToken ct)
        => As(v => _pastoral.AcknowledgeCheckInAsync(v, id, request?.Text, DateTime.UtcNow, ct));

    /// <summary>For parents: each child's notes and plans shared with the family.</summary>
    [HttpGet("family")]
    public Task<IActionResult> Family(CancellationToken ct) => As(v => _pastoral.FamilyAsync(v, ct));

    // ── Set-up ──

    [HttpGet("setup")]
    public Task<IActionResult> Setup([FromQuery] Guid? tenantId, CancellationToken ct) => As(v => _pastoral.SetupAsync(v, tenantId, ct));

    [HttpPost("team")]
    public Task<IActionResult> AddTeamMember([FromBody] AddTeamMemberRequest request, CancellationToken ct) => As(v => _pastoral.AddTeamMemberAsync(v, request, ct));

    [HttpDelete("team/{id:guid}")]
    public Task<IActionResult> RemoveTeamMember(Guid id, CancellationToken ct) => As(v => _pastoral.RemoveTeamMemberAsync(v, id, ct));

    [HttpPut("settings")]
    public Task<IActionResult> SaveSettings([FromBody] SaveSettingsRequest request, CancellationToken ct) => As(v => _pastoral.SaveSettingsAsync(v, request, ct));

    [HttpPost("categories")]
    public Task<IActionResult> AddCategory([FromBody] SaveCategoryRequest request, CancellationToken ct) => As(v => _pastoral.SaveCategoryAsync(v, null, request, ct));

    [HttpPut("categories/{id:guid}")]
    public Task<IActionResult> UpdateCategory(Guid id, [FromBody] SaveCategoryRequest request, CancellationToken ct)
        => As(v => _pastoral.SaveCategoryAsync(v, id, request, ct));

    // ── helpers ──

    private async Task<PastoralViewer?> ViewerAsync(CancellationToken ct)
    {
        var id = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId) ? userId : Guid.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
        return id == Guid.Empty ? null : await _pastoral.ViewerAsync(id, role, ct);
    }

    private async Task<IActionResult> As<T>(Func<PastoralViewer, Task<PastoralResult<T>>> action)
    {
        if (await ViewerAsync(HttpContext.RequestAborted) is not { } v) return Unauthorized();
        var result = await action(v);
        return result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
    }
}
