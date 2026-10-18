using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M1 Stage M1.4 — a user's linked SSO accounts: list and unlink (self-service or by an admin).
/// Every unlink is written to the audit log. Gated by the "sso" module like the rest of /api/v1/sso.
/// </summary>
[ApiController]
[Route("api/v1/sso/links")]
[Authorize]
public sealed class SsoLinksController : ControllerBase
{
    private readonly ISsoAccountService _accounts;

    public SsoLinksController(ISsoAccountService accounts) => _accounts = accounts;

    /// <summary>GET /api/v1/sso/links — the signed-in user's linked accounts.</summary>
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => CurrentUserId() is { } userId ? Ok(await _accounts.GetLinksAsync(userId, ct)) : Forbid();

    /// <summary>DELETE /api/v1/sso/links/{provider} — unlink one of the signed-in user's accounts.</summary>
    [HttpDelete("{provider}")]
    public async Task<IActionResult> UnlinkMine(string provider, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Forbid();

        var result = await _accounts.UnlinkAsync(userId, provider, userId, isSelfService: true, ClientIp(), ct);
        return ToActionResult(result);
    }

    /// <summary>GET /api/v1/sso/links/users/{userId} — an admin views a user's linked accounts.</summary>
    [HttpGet("users/{userId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ForUser(Guid userId, CancellationToken ct)
        => Ok(await _accounts.GetLinksAsync(userId, ct));

    /// <summary>DELETE /api/v1/sso/links/users/{userId}/{provider} — an admin removes a user's link (e.g. wrong account linked).</summary>
    [HttpDelete("users/{userId:guid}/{provider}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UnlinkForUser(Guid userId, string provider, CancellationToken ct)
    {
        if (CurrentUserId() is not { } actorId)
            return Forbid();

        var result = await _accounts.UnlinkAsync(userId, provider, actorId, isSelfService: false, ClientIp(), ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(SsoUnlinkResult result) => result switch
    {
        SsoUnlinkResult.Unlinked => NoContent(),
        SsoUnlinkResult.WouldLockOut => Conflict(new
        {
            error = "WOULD_LOCK_OUT",
            message = "Your institution requires single sign-on and this is your only linked account. Ask an administrator to change it."
        }),
        _ => NotFound()
    };

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
