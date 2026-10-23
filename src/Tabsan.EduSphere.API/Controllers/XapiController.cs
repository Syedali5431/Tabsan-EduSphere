using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lms.Scorm;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M13.2 — a minimal xAPI 1.0.3 Learning Record Store (statements and "about"). Activity providers authenticate with
/// HTTP Basic credentials an administrator creates here; administrators can also read statements with their own sign-in.
/// </summary>
[ApiController]
[Route("api/v1/xapi")]
public sealed class XapiController : ControllerBase
{
    private const string VersionHeader = "X-Experience-API-Version";
    private readonly XapiService _service;

    public XapiController(XapiService service) => _service = service;

    [HttpGet("about")]
    [AllowAnonymous]
    public IActionResult About()
    {
        Response.Headers[VersionHeader] = XapiService.Version;
        return Ok(new { version = new[] { XapiService.Version } });
    }

    [HttpPost("statements")]
    [AllowAnonymous]
    [RequestSizeLimit(7_000_000)]
    public Task<IActionResult> Post(CancellationToken ct) => StoreAsync(null, ct);

    [HttpPut("statements")]
    [AllowAnonymous]
    [RequestSizeLimit(100_000)]
    public async Task<IActionResult> Put([FromQuery] string? statementId, CancellationToken ct)
    {
        if (!Guid.TryParse(statementId, out var id)) return Error(400, "PUT needs the statementId parameter.");
        var result = await StoreAsync(id, ct);
        return result is OkObjectResult ? NoContent() : result;
    }

    [HttpGet("statements")]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] string? statementId, [FromQuery] string? voidedStatementId, [FromQuery] string? agent,
        [FromQuery] string? verb, [FromQuery] string? activity, [FromQuery] string? registration, [FromQuery] string? since, [FromQuery] string? until,
        [FromQuery] int limit, [FromQuery] bool ascending, CancellationToken ct)
    {
        Response.Headers[VersionHeader] = XapiService.Version;
        if (!HasVersionHeader()) return Error(400, $"The {VersionHeader} header is required.");
        var credential = await _service.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);
        var admin = credential is null && (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"));
        if (credential is null && !admin) return BasicChallenge();

        if (statementId is not null || voidedStatementId is not null)
        {
            var voided = voidedStatementId is not null;
            if (!Guid.TryParse(voided ? voidedStatementId : statementId, out var id)) return Error(400, "Invalid statement id.");
            var statement = await _service.GetAsync(id, voided, ct);
            if (statement is null || (credential is not null && statement.CredentialId != credential.Id)) return NotFound();
            return Content(statement.Json, "application/json");
        }

        string? actorKey = null;
        if (agent is not null)
        {
            try { actorKey = XapiService.ActorKey(JsonNode.Parse(agent)); }
            catch (System.Text.Json.JsonException) { }
            if (actorKey is null) return Error(400, "\"agent\" must be an Agent with one identifier.");
        }
        Guid? reg = null;
        if (registration is not null)
        {
            if (!Guid.TryParse(registration, out var r)) return Error(400, "\"registration\" must be a UUID.");
            reg = r;
        }
        if (!TryDate(since, out var sinceDate) || !TryDate(until, out var untilDate)) return Error(400, "\"since\" and \"until\" must be ISO 8601 dates.");

        var query = new XapiQuery(actorKey, verb, activity, reg, sinceDate, untilDate, limit, ascending, credential?.Id);
        var (statements, next) = await _service.QueryAsync(query, ct);
        var more = "";
        if (next is { } cursor)
        {
            var parameters = Request.Query.Where(q => q.Key is not ("since" or "until")).ToDictionary(q => q.Key, q => q.Value.ToString());
            parameters[ascending ? "since" : "until"] = (ascending ? cursor : cursor.AddTicks(-1)).ToString("o", CultureInfo.InvariantCulture);
            more = "/api/v1/xapi/statements?" + string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }
        Response.Headers["X-Experience-API-Consistent-Through"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        var json = "{\"statements\":[" + string.Join(",", statements.Select(s => s.Json)) + "],\"more\":" + System.Text.Json.JsonSerializer.Serialize(more) + "}";
        return Content(json, "application/json");
    }

    // ── administration (signed-in Admin / SuperAdmin) ──

    [HttpGet("credentials")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Credentials(CancellationToken ct) => Ok(await _service.CredentialsAsync(ct));

    [HttpPost("credentials")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateCredential([FromBody] XapiCredentialRequest request, CancellationToken ct)
    {
        var result = await _service.CreateCredentialAsync(request, UserId(), ct);
        return result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpDelete("credentials/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => await _service.DeactivateAsync(id, UserId(), ct) ? NoContent() : NotFound(new { message = "Credentials not found." });

    [HttpGet("recent")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Recent([FromQuery] int take, CancellationToken ct) => Ok(await _service.RecentAsync(take <= 0 ? 50 : take, ct));

    // ── pieces ──

    private async Task<IActionResult> StoreAsync(Guid? statementId, CancellationToken ct)
    {
        Response.Headers[VersionHeader] = XapiService.Version;
        if (!HasVersionHeader()) return Error(400, $"The {VersionHeader} header is required.");
        var credential = await _service.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);
        if (credential is null) return BasicChallenge();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);
        var result = await _service.StoreAsync(credential, body, statementId, $"{Request.Scheme}://{Request.Host}/api/v1/xapi", ct);
        return result.Ok ? Ok(result.Value!.Select(id => id.ToString())) : Error(result.Status, result.Error!);
    }

    private UnauthorizedResult BasicChallenge()
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"EduSphere LRS\"";
        return Unauthorized();
    }

    private bool HasVersionHeader() => Request.Headers[VersionHeader].ToString() is { Length: > 0 } v && v.StartsWith("1.0", StringComparison.Ordinal);

    private static bool TryDate(string? value, out DateTime? date)
    {
        date = null;
        if (value is null) return true;
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d)) return false;
        date = d.UtcDateTime;
        return true;
    }

    private ObjectResult Error(int status, string message) => StatusCode(status, new { message });

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
