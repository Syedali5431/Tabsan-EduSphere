using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M4.3 — the signed-in user's Web Push devices and per-type preferences.
/// Every action works on the caller's own subscriptions only.
/// </summary>
[ApiController]
[Route("api/v1/push")]
[Authorize]
public sealed class PushController : ControllerBase
{
    private readonly IPushNotificationService _push;

    public PushController(IPushNotificationService push) => _push = push;

    /// <summary>Whether push is on, the application server key, the caller's devices and preferences.</summary>
    [HttpGet]
    public async Task<IActionResult> Summary(CancellationToken ct)
        => UserId() is { } id ? Ok(await _push.GetSummaryAsync(id, ct)) : Unauthorized();

    [HttpPost("subscriptions")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request, CancellationToken ct)
    {
        if (UserId() is not { } id) return Unauthorized();
        var error = await _push.SubscribeAsync(id, request, ct);
        return error is null ? NoContent() : BadRequest(new { message = error });
    }

    [HttpDelete("subscriptions")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string endpoint, CancellationToken ct)
    {
        if (UserId() is not { } id) return Unauthorized();
        return await _push.UnsubscribeAsync(id, endpoint, ct) ? NoContent() : NotFound();
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> SavePreferences([FromBody] PushPreferencesRequest request, CancellationToken ct)
    {
        if (UserId() is not { } id) return Unauthorized();
        await _push.SavePreferencesAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        if (UserId() is not { } id) return Unauthorized();
        return Ok(new { delivered = await _push.SendTestAsync(id, ct) });
    }

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
