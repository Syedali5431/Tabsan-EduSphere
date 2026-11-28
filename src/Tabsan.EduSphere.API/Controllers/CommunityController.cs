using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Community;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// School news, events and online forms (/api/v1/community). Everyone signed in reads what is for them, replies to events
/// and fills in forms (parents for each child); teachers write for their classes and admins for their school.
/// </summary>
[ApiController]
[Route("api/v1/community")]
[Authorize]
public sealed class CommunityController : ControllerBase
{
    private readonly CommunityService _community;

    public CommunityController(CommunityService community) => _community = community;

    /// <summary>The campuses, year levels and classes the user may write for.</summary>
    [HttpGet("audiences")]
    public Task<IActionResult> Audiences(CancellationToken ct) => As(m => _community.AudienceOptionsAsync(m, ct));

    // ── News ──

    [HttpGet("news")]
    public Task<IActionResult> News(CancellationToken ct) => AsList(m => _community.NewsFeedAsync(m, DateTime.UtcNow, ct));

    [HttpGet("news/manage")]
    public Task<IActionResult> ManageNews(CancellationToken ct) => As(m => _community.ManageNewsAsync(m, DateTime.UtcNow, ct));

    [HttpGet("news/{id:guid}")]
    public Task<IActionResult> NewsPost(Guid id, CancellationToken ct) => As(m => _community.NewsPostAsync(m, id, DateTime.UtcNow, ct));

    [HttpPost("news")]
    public Task<IActionResult> CreateNews([FromBody] SaveNewsRequest request, CancellationToken ct) => As(m => _community.SaveNewsAsync(m, null, request, DateTime.UtcNow, ct));

    [HttpPut("news/{id:guid}")]
    public Task<IActionResult> UpdateNews(Guid id, [FromBody] SaveNewsRequest request, CancellationToken ct) => As(m => _community.SaveNewsAsync(m, id, request, DateTime.UtcNow, ct));

    /// <summary>Takes a post down (a draft nobody has seen is deleted).</summary>
    [HttpPost("news/{id:guid}/withdraw")]
    public Task<IActionResult> WithdrawNews(Guid id, CancellationToken ct) => As(m => _community.WithdrawNewsAsync(m, id, ct));

    // ── Events ──

    /// <summary>Events for the user between ?from= and ?to= (default: yesterday to 120 days ahead).</summary>
    [HttpGet("events")]
    public Task<IActionResult> Events([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var start = from?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-1);
        var end = to?.ToUniversalTime() ?? start.AddDays(121);
        if (end <= start || end - start > TimeSpan.FromDays(400)) return Task.FromResult<IActionResult>(BadRequest(new { message = "Choose a period of up to 400 days." }));
        return AsList(m => _community.EventsAsync(m, start, end, DateTime.UtcNow, ct));
    }

    [HttpGet("events/manage")]
    public Task<IActionResult> ManageEvents(CancellationToken ct) => As(m => _community.ManageEventsAsync(m, DateTime.UtcNow, ct));

    [HttpGet("events/{id:guid}")]
    public Task<IActionResult> Event(Guid id, CancellationToken ct) => As(m => _community.EventAsync(m, id, DateTime.UtcNow, ct));

    [HttpPost("events")]
    public Task<IActionResult> CreateEvent([FromBody] SaveEventRequest request, CancellationToken ct) => As(m => _community.SaveEventAsync(m, null, request, DateTime.UtcNow, ct));

    [HttpPut("events/{id:guid}")]
    public Task<IActionResult> UpdateEvent(Guid id, [FromBody] SaveEventRequest request, CancellationToken ct) => As(m => _community.SaveEventAsync(m, id, request, DateTime.UtcNow, ct));

    [HttpPost("events/{id:guid}/cancel")]
    public Task<IActionResult> CancelEvent(Guid id, [FromBody] CancelEventRequest? request, CancellationToken ct)
        => As(m => _community.CancelEventAsync(m, id, request?.Reason, DateTime.UtcNow, ct));

    [HttpPost("events/{id:guid}/reply")]
    public Task<IActionResult> Reply(Guid id, [FromBody] EventReplyRequest request, CancellationToken ct) => As(m => _community.ReplyAsync(m, id, request, DateTime.UtcNow, ct));

    [HttpGet("events/{id:guid}/replies")]
    public Task<IActionResult> Replies(Guid id, CancellationToken ct) => As(m => _community.EventRepliesAsync(m, id, DateTime.UtcNow, ct));

    // ── Forms ──

    /// <summary>Open forms for the user (a parent: one row per child each form concerns).</summary>
    [HttpGet("forms")]
    public Task<IActionResult> Forms(CancellationToken ct) => AsList(m => _community.FormsToFillAsync(m, DateTime.UtcNow, ct));

    [HttpGet("forms/manage")]
    public Task<IActionResult> ManageForms(CancellationToken ct) => As(m => _community.ManageFormsAsync(m, ct));

    /// <summary>A form with the user's answers (?studentProfileId= for a parent's child).</summary>
    [HttpGet("forms/{id:guid}")]
    public Task<IActionResult> Form(Guid id, [FromQuery] Guid? studentProfileId, CancellationToken ct)
        => As(m => _community.FormAsync(m, id, studentProfileId, DateTime.UtcNow, ct));

    [HttpPost("forms")]
    public Task<IActionResult> CreateForm([FromBody] SaveFormRequest request, CancellationToken ct) => As(m => _community.SaveFormAsync(m, null, request, DateTime.UtcNow, ct));

    [HttpPut("forms/{id:guid}")]
    public Task<IActionResult> UpdateForm(Guid id, [FromBody] SaveFormRequest request, CancellationToken ct) => As(m => _community.SaveFormAsync(m, id, request, DateTime.UtcNow, ct));

    [HttpPost("forms/{id:guid}/open")]
    public Task<IActionResult> OpenForm(Guid id, CancellationToken ct) => As(m => _community.OpenFormAsync(m, id, true, DateTime.UtcNow, ct));

    [HttpPost("forms/{id:guid}/close")]
    public Task<IActionResult> CloseForm(Guid id, CancellationToken ct) => As(m => _community.OpenFormAsync(m, id, false, DateTime.UtcNow, ct));

    [HttpPost("forms/{id:guid}/responses")]
    public Task<IActionResult> Submit(Guid id, [FromBody] SubmitFormRequest request, CancellationToken ct) => As(m => _community.SubmitFormAsync(m, id, request, DateTime.UtcNow, ct));

    [HttpGet("forms/{id:guid}/tracking")]
    public Task<IActionResult> Tracking(Guid id, CancellationToken ct) => As(m => _community.FormTrackingAsync(m, id, DateTime.UtcNow, ct));

    /// <summary>Reminds everyone who has not answered; returns how many people were reminded.</summary>
    [HttpPost("forms/{id:guid}/remind")]
    public async Task<IActionResult> Remind(Guid id, CancellationToken ct)
    {
        if (await MemberAsync(ct) is not { } m) return Unauthorized();
        var result = await _community.RemindFormAsync(m, id, DateTime.UtcNow, ct);
        return result.Ok ? Ok(new { reminded = result.Value }) : StatusCode(result.Status, new { message = result.Error });
    }

    /// <summary>The answers as a CSV file.</summary>
    [HttpGet("forms/{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken ct)
    {
        if (await MemberAsync(ct) is not { } m) return Unauthorized();
        var result = await _community.ExportFormAsync(m, id, ct);
        return result.Ok ? File(result.Value.Content, "text/csv; charset=utf-8", result.Value.FileName) : StatusCode(result.Status, new { message = result.Error });
    }

    // ── helpers ──

    private async Task<CommunityMember?> MemberAsync(CancellationToken ct)
    {
        var id = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId) ? userId : Guid.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
        return id == Guid.Empty ? null : await _community.MemberAsync(id, role, ct);
    }

    private async Task<IActionResult> As<T>(Func<CommunityMember, Task<CommunityResult<T>>> action)
    {
        if (await MemberAsync(HttpContext.RequestAborted) is not { } m) return Unauthorized();
        var result = await action(m);
        return result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    private async Task<IActionResult> AsList<T>(Func<CommunityMember, Task<T>> action)
        => await MemberAsync(HttpContext.RequestAborted) is { } m ? Ok(await action(m)) : Unauthorized();
}

public sealed record CancelEventRequest(string? Reason);
