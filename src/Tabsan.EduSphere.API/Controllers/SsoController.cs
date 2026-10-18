using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M1 Stage M1.2 — OpenID Connect single sign-on.
/// These endpoints are called server-to-server by the web portal, which owns the browser
/// redirect to the identity provider and the /Login/SsoCallback page.
/// The whole route is gated by the "sso" module (ModuleLicenseEnforcementMiddleware).
/// </summary>
[ApiController]
[Route("api/v1/sso")]
[AllowAnonymous]
public sealed class SsoController : ControllerBase
{
    private readonly ISsoSignInService _sso;

    public SsoController(ISsoSignInService sso) => _sso = sso;

    /// <summary>GET /api/v1/sso/providers?tenant=CODE — enabled providers for the login page.</summary>
    [HttpGet("providers")]
    public async Task<IActionResult> Providers([FromQuery] string? tenant, CancellationToken ct)
        => Ok(await _sso.GetProvidersAsync(tenant, ct));

    /// <summary>POST /api/v1/sso/start — returns the provider authorize URL and the state to bind to the browser.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] SsoStartRequest request, CancellationToken ct)
    {
        var (response, failure) = await _sso.StartAsync(request, ct);
        return response is not null
            ? Ok(response)
            : BadRequest(new { error = failure.ToString() });
    }

    /// <summary>POST /api/v1/sso/complete — redeems the code and returns the standard login payload.</summary>
    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] SsoCompleteRequest request, CancellationToken ct)
        => ToActionResult(await _sso.CompleteAsync(request, ClientIp(), ct));

    /// <summary>POST /api/v1/sso/complete-mfa — second step for users with an authenticator app.</summary>
    [HttpPost("complete-mfa")]
    public async Task<IActionResult> CompleteMfa([FromBody] SsoMfaRequest request, CancellationToken ct)
        => ToActionResult(await _sso.CompleteMfaAsync(request, ClientIp(), ct));

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private IActionResult ToActionResult(SsoResult result)
    {
        if (result.IsSuccess)
            return Ok(result.Response);

        var error = result.FailureReason.ToString();
        return result.FailureReason switch
        {
            SsoFailureReason.MfaCodeRequired or SsoFailureReason.InvalidMfaCode
                => Unauthorized(new { error, ticket = result.MfaTicket }),

            SsoFailureReason.LoginRejected
                => StatusCode(StatusCodes.Status403Forbidden, new { error, loginFailure = result.LoginFailure?.ToString() }),

            SsoFailureReason.AccountNotLinked or SsoFailureReason.EmailDomainNotAllowed
                => StatusCode(StatusCodes.Status403Forbidden, new { error }),

            _ => BadRequest(new { error })
        };
    }
}
