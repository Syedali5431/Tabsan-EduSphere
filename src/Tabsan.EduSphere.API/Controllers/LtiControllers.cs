using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Lti;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

// Plan M5 — LTI 1.3 platform. All routes are under /api/v1/lti and gated by the licensed "lti" module.

/// <summary>Tool registry, deployments, platform details and key rotation (Admin/SuperAdmin).</summary>
[ApiController]
[Route("api/v1/lti")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class LtiAdminController : ControllerBase
{
    private readonly LtiAdminService _admin;

    public LtiAdminController(LtiAdminService admin) => _admin = admin;

    [HttpGet("platform")]
    public IActionResult Platform() => Ok(_admin.PlatformInfo());

    [HttpPost("platform/rotate-keys")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RotateKeys(CancellationToken ct)
    {
        await _admin.RotateKeysAsync(UserId(), ct);
        return Ok(_admin.PlatformInfo());
    }

    [HttpGet("tools")]
    public async Task<IActionResult> Tools(CancellationToken ct) => Ok(await _admin.ListAsync(ct));

    [HttpGet("tools/{toolId:guid}")]
    public async Task<IActionResult> Tool(Guid toolId, CancellationToken ct)
        => await _admin.GetAsync(toolId, ct) is { } tool ? Ok(tool) : NotFound();

    [HttpPost("tools")]
    public async Task<IActionResult> Create([FromBody] SaveLtiToolRequest request, CancellationToken ct)
        => ToResult(await _admin.CreateAsync(request, UserId(), ct));

    [HttpPut("tools/{toolId:guid}")]
    public async Task<IActionResult> Update(Guid toolId, [FromBody] SaveLtiToolRequest request, CancellationToken ct)
        => ToResult(await _admin.UpdateAsync(toolId, request, UserId(), ct));

    [HttpPost("tools/{toolId:guid}/{state:regex(^(enable|disable)$)}")]
    public async Task<IActionResult> SetActive(Guid toolId, string state, CancellationToken ct)
        => await _admin.SetActiveAsync(toolId, state == "enable", UserId(), ct) ? NoContent() : NotFound();

    [HttpPost("tools/{toolId:guid}/deployments")]
    public async Task<IActionResult> AddDeployment(Guid toolId, [FromBody] AddLtiDeploymentRequest request, CancellationToken ct)
        => ToResult(await _admin.AddDeploymentAsync(toolId, request, UserId(), ct));

    [HttpPost("tools/{toolId:guid}/deployments/{deploymentId:guid}/{state:regex(^(enable|disable)$)}")]
    public async Task<IActionResult> SetDeployment(Guid toolId, Guid deploymentId, string state, CancellationToken ct)
        => ToResult(await _admin.SetDeploymentActiveAsync(toolId, deploymentId, state == "enable", UserId(), ct));

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private IActionResult ToResult<T>(LtiResult<T> r) => r.Ok ? StatusCode(r.Status, r.Value) : StatusCode(r.Status, new { message = r.Error });
}

/// <summary>External tools in a course: available tools, course items, launching and content selection.</summary>
[ApiController]
[Route("api/v1/lti")]
[Authorize]
public sealed class LtiCourseController : ControllerBase
{
    private readonly LtiLaunchService _launch;
    private readonly IAccessScopeResolver _scope;
    private readonly ApplicationDbContext _db;

    public LtiCourseController(LtiLaunchService launch, IAccessScopeResolver scope, ApplicationDbContext db)
    {
        _launch = launch;
        _scope = scope;
        _db = db;
    }

    [HttpGet("offerings/{offeringId:guid}/tools")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Tools(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _launch.ToolsForOfferingAsync(offeringId, ct));
    }

    [HttpGet("offerings/{offeringId:guid}/links")]
    public async Task<IActionResult> Links(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _launch.LinksAsync(offeringId, IsStaff(), ct));
    }

    [HttpPost("offerings/{offeringId:guid}/links")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> CreateLink(Guid offeringId, [FromBody] CreateLtiLinkRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        var r = await _launch.CreateLinkAsync(offeringId, request, UserId(), ct);
        return r.Ok ? StatusCode(r.Status, r.Value) : StatusCode(r.Status, new { message = r.Error });
    }

    [HttpPost("links/{linkId:guid}/{state:regex(^(show|hide)$)}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> SetVisible(Guid linkId, string state, CancellationToken ct)
    {
        var offeringId = await _db.LtiResourceLinks.Where(l => l.Id == linkId).Select(l => (Guid?)l.CourseOfferingId).FirstOrDefaultAsync(ct);
        if (offeringId is null) return NotFound();
        if (await DeniedAsync(offeringId.Value, ct) is { } denied) return denied;
        return await _launch.SetLinkVisibleAsync(linkId, state == "show", UserId(), ct) ? NoContent() : NotFound();
    }

    /// <summary>Starts a launch; the portal posts the returned parameters to the tool's login initiation URL.</summary>
    [HttpPost("links/{linkId:guid}/launch")]
    public async Task<IActionResult> Launch(Guid linkId, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        var offeringId = await _db.LtiResourceLinks.Where(l => l.Id == linkId).Select(l => (Guid?)l.CourseOfferingId).FirstOrDefaultAsync(ct);
        if (offeringId is null) return NotFound();
        if (await DeniedAsync(offeringId.Value, ct) is { } denied) return denied;
        // Plan M7: a tool placed in a locked module cannot be opened yet.
        if (!IsStaff() && Guid.TryParse(User.FindFirstValue("studentProfileId"), out var studentId)
            && await Services.ModuleGate.LockReasonAsync(HttpContext, studentId, Domain.Lms.ModuleItemType.ExternalTool, linkId, ct) is { } locked)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = locked });
        var r = await _launch.StartLaunchAsync(UserId(), linkId, IsStaff(), LocalPath(returnUrl), ct);
        return r.Ok ? Ok(r.Value) : StatusCode(r.Status, new { message = r.Error });
    }

    public sealed record DeepLinkStart(Guid ToolId, Guid? ModuleId, string? ReturnUrl);

    [HttpPost("offerings/{offeringId:guid}/deep-linking")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> DeepLinking(Guid offeringId, [FromBody] DeepLinkStart request, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        var r = await _launch.StartDeepLinkingAsync(UserId(), offeringId, request.ToolId, request.ModuleId, LocalPath(request.ReturnUrl), ct);
        return r.Ok ? Ok(r.Value) : StatusCode(r.Status, new { message = r.Error });
    }

    private static string? LocalPath(string? url) => url is not null && url.StartsWith('/') && !url.StartsWith("//") && url.Length <= 500 ? url : null;

    private bool IsStaff() => User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    /// <summary>Students must be enrolled; staff must be in the course's tenant/campus.</summary>
    private async Task<IActionResult?> DeniedAsync(Guid offeringId, CancellationToken ct)
    {
        var scope = await _db.CourseOfferings.AsNoTracking().Where(o => o.Id == offeringId).Select(o => new { o.TenantId, o.CampusId }).FirstOrDefaultAsync(ct);
        if (scope is null) return NotFound();
        if (!IsStaff())
        {
            var studentId = Guid.TryParse(User.FindFirstValue("studentProfileId"), out var sp) ? sp : Guid.Empty;
            var enrolled = await _db.Enrollments.AnyAsync(e => e.CourseOfferingId == offeringId && e.StudentProfileId == studentId
                && e.Status == Domain.Academic.EnrollmentStatus.Active, ct);
            return enrolled ? null : Forbid();
        }
        if (_scope.IsSuperAdmin()) return null;
        var tenant = _scope.GetTenantId();
        var campus = _scope.GetCampusId();
        if (tenant.HasValue && scope.TenantId != tenant) return Forbid();
        if (campus.HasValue && scope.CampusId != campus) return Forbid();
        return null;
    }
}

/// <summary>
/// Protocol and service endpoints called by the portal on a tool's behalf (authorize, deep-link return) or by tools
/// directly (JWKS, token, AGS, NRPS). They authenticate with LTI mechanisms, not the portal's tokens.
/// </summary>
[ApiController]
[Route("api/v1/lti")]
[AllowAnonymous]
public sealed class LtiProtocolController : ControllerBase
{
    private readonly LtiLaunchService _launch;
    private readonly LtiTokenService _tokens;
    private readonly LtiAgsNrpsService _services;
    private readonly ILtiPlatformKeys _keys;

    public LtiProtocolController(LtiLaunchService launch, LtiTokenService tokens, LtiAgsNrpsService services, ILtiPlatformKeys keys)
    {
        _launch = launch;
        _tokens = tokens;
        _services = services;
        _keys = keys;
    }

    [HttpGet("jwks")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public IActionResult Jwks() => Content(_keys.JwksJson(), "application/json");

    /// <summary>The OIDC authentication request (relayed by the portal). The caller's portal token, when present, must belong to the launching user.</summary>
    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize([FromBody] LtiAuthorizationRequest request, CancellationToken ct)
    {
        Guid? signedIn = User.Identity?.IsAuthenticated == true && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        var r = await _launch.AuthorizeAsync(request, signedIn, ct);
        return r.Ok ? Ok(r.Value) : StatusCode(r.Status, new { message = r.Error });
    }

    public sealed record DeepLinkReturn(string Jwt);

    [HttpPost("deep-link-return")]
    public async Task<IActionResult> DeepLinkReturnAsync([FromBody] DeepLinkReturn request, CancellationToken ct)
    {
        var r = await _launch.CompleteDeepLinkingAsync(request.Jwt ?? string.Empty, ct);
        return r.Ok ? Ok(r.Value) : StatusCode(r.Status, new { message = r.Error });
    }

    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm(Name = "grant_type")] string? grantType, [FromForm(Name = "client_assertion_type")] string? assertionType,
        [FromForm(Name = "client_assertion")] string? assertion, [FromForm(Name = "scope")] string? scope, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var r = await _tokens.IssueAsync(new LtiTokenRequest(grantType, assertionType, assertion, scope), ct);
        if (r.Ok) return Ok(new { access_token = r.Value!.AccessToken, token_type = r.Value.TokenType, expires_in = r.Value.ExpiresIn, scope = r.Value.Scope });
        var i = r.Error!.IndexOf(':');
        return StatusCode(r.Status, new { error = r.Error[..i], error_description = r.Error[(i + 1)..].Trim() });
    }

    // ── Assignment and Grade Services ───────────────────────────────────────

    [HttpGet("ags/{offeringId:guid}/lineitems")]
    public Task<IActionResult> LineItems(Guid offeringId, [FromQuery(Name = "resource_link_id")] string? resourceLinkId, [FromQuery(Name = "resource_id")] string? resourceId,
        [FromQuery] string? tag, [FromQuery] int? limit, CancellationToken ct)
        => Serve(c => _services.ListLineItemsAsync(c, offeringId, resourceLinkId, resourceId, tag, limit, ct), LtiAgsNrpsService.LineItemContainerType, ct);

    [HttpPost("ags/{offeringId:guid}/lineitems")]
    public async Task<IActionResult> CreateLineItem(Guid offeringId, CancellationToken ct)
    {
        var body = await ReadBodyAsync(ct);
        return await Serve(c => _services.SaveLineItemAsync(c, offeringId, null, body, ct), LtiAgsNrpsService.LineItemType, ct);
    }

    [HttpGet("ags/{offeringId:guid}/lineitems/{lineItemId:guid}")]
    public Task<IActionResult> LineItem(Guid offeringId, Guid lineItemId, CancellationToken ct)
        => Serve(c => _services.GetLineItemAsync(c, offeringId, lineItemId, ct), LtiAgsNrpsService.LineItemType, ct);

    [HttpPut("ags/{offeringId:guid}/lineitems/{lineItemId:guid}")]
    public async Task<IActionResult> UpdateLineItem(Guid offeringId, Guid lineItemId, CancellationToken ct)
    {
        var body = await ReadBodyAsync(ct);
        return await Serve(c => _services.SaveLineItemAsync(c, offeringId, lineItemId, body, ct), LtiAgsNrpsService.LineItemType, ct);
    }

    [HttpDelete("ags/{offeringId:guid}/lineitems/{lineItemId:guid}")]
    public async Task<IActionResult> DeleteLineItem(Guid offeringId, Guid lineItemId, CancellationToken ct)
    {
        var caller = await _tokens.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);
        if (caller is null) return Unauthorized(new { error = "invalid_token" });
        var r = await _services.DeleteLineItemAsync(caller, offeringId, lineItemId, ct);
        return r.Ok ? NoContent() : StatusCode(r.Status, new { error = r.Error });
    }

    [HttpPost("ags/{offeringId:guid}/lineitems/{lineItemId:guid}/scores")]
    public async Task<IActionResult> Score(Guid offeringId, Guid lineItemId, CancellationToken ct)
    {
        var caller = await _tokens.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);
        if (caller is null) return Unauthorized(new { error = "invalid_token" });
        var r = await _services.PostScoreAsync(caller, offeringId, lineItemId, await ReadBodyAsync(ct), ct);
        return r.Ok ? NoContent() : StatusCode(r.Status, new { error = r.Error });
    }

    [HttpGet("ags/{offeringId:guid}/lineitems/{lineItemId:guid}/results")]
    public Task<IActionResult> Results(Guid offeringId, Guid lineItemId, [FromQuery(Name = "user_id")] string? userId, [FromQuery] int? limit, CancellationToken ct)
        => Serve(c => _services.ResultsAsync(c, offeringId, lineItemId, userId, limit, ct), LtiAgsNrpsService.ResultContainerType, ct);

    // ── Names and Role Provisioning Services ────────────────────────────────

    [HttpGet("nrps/{offeringId:guid}/memberships")]
    public Task<IActionResult> Memberships(Guid offeringId, [FromQuery] string? role, [FromQuery] int? limit, CancellationToken ct)
        => Serve(c => _services.MembershipsAsync(c, offeringId, role, limit, ct), LtiAgsNrpsService.MembershipType, ct);

    private async Task<IActionResult> Serve(Func<LtiServiceCaller, Task<LtiResult<JsonNode>>> action, string mediaType, CancellationToken ct)
    {
        var caller = await _tokens.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);
        if (caller is null) return Unauthorized(new { error = "invalid_token" });
        var r = await action(caller);
        if (!r.Ok) return StatusCode(r.Status, new { error = r.Error });
        return new ContentResult { StatusCode = r.Status, Content = r.Value!.ToJsonString(), ContentType = mediaType };
    }

    private async Task<string> ReadBodyAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        return body.Length > 100_000 ? string.Empty : body;
    }
}
