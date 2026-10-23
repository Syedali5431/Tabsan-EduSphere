using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Auth;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M14.3 — personal access tokens for the public REST API: users in the allowed roles (ApiTokens:AllowedRoles) create
/// and revoke their own; administrators see and revoke all. A token is shown once. Managed only after signing in.
/// </summary>
[ApiController]
[Route("api/v1/api-tokens")]
[Authorize]
public sealed class ApiTokensController(ApiTokenService tokens) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await tokens.MineAsync(UserId(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiTokenRequest request, CancellationToken ct)
    {
        var result = await tokens.CreateAsync(UserId(), User.FindFirstValue(ClaimTypes.Role), request, ct);
        return result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
        => await tokens.RevokeAsync(id, UserId(), Administrator(), ct) ? NoContent() : NotFound(new { message = "Token not found." });

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> All(CancellationToken ct) => Ok(await tokens.AllAsync(ct));

    private bool Administrator() => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
