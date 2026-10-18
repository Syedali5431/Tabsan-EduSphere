using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M1 Stage M1.5 — SuperAdmin configuration of SSO providers (installation-wide or per tenant
/// via ?tenant=CODE). Secrets are write-only. Gated by the "sso" module like the rest of /api/v1/sso.
/// </summary>
[ApiController]
[Route("api/v1/sso/settings")]
[Authorize(Roles = "SuperAdmin")]
public sealed class SsoSettingsController : ControllerBase
{
    private readonly ISsoSettingsService _settings;

    public SsoSettingsController(ISsoSettingsService settings) => _settings = settings;

    /// <summary>GET /api/v1/sso/settings — every configured provider.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _settings.ListAsync(ct));

    /// <summary>PUT /api/v1/sso/settings/{provider}?tenant=CODE — create or update.</summary>
    [HttpPut("{provider}")]
    public async Task<IActionResult> Save(string provider, [FromQuery] string? tenant, [FromBody] SaveSsoProviderSettingRequest request, CancellationToken ct)
    {
        var (saved, error) = await _settings.SaveAsync(provider, tenant, request, ActorId(), ClientIp(), ct);
        return saved is not null ? Ok(saved) : BadRequest(new { message = error });
    }

    /// <summary>DELETE /api/v1/sso/settings/{provider}?tenant=CODE — remove a provider.</summary>
    [HttpDelete("{provider}")]
    public async Task<IActionResult> Delete(string provider, [FromQuery] string? tenant, CancellationToken ct)
        => await _settings.DeleteAsync(provider, tenant, ActorId(), ClientIp(), ct) ? NoContent() : NotFound();

    /// <summary>POST /api/v1/sso/settings/{provider}/test?tenant=CODE — check the saved configuration can reach the IdP.</summary>
    [HttpPost("{provider}/test")]
    public async Task<IActionResult> Test(string provider, [FromQuery] string? tenant, CancellationToken ct)
        => Ok(await _settings.TestAsync(provider, tenant, ct));

    private Guid ActorId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
