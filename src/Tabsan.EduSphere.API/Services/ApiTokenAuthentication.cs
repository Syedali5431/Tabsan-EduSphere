using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M14.3 — "Authorization: Bearer esk_…" signs a request in as the token's owner with the same claims a sign-in gives
/// (role, scope, student profile), plus <c>api_token_id</c> (rate limits, audit) and <c>api_token_readonly</c>.
/// </summary>
public sealed class ApiTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiToken";
    public const string ReadOnlyClaim = "api_token_readonly";

    private readonly ApiTokenService _tokens;
    private readonly ApplicationDbContext _db;

    public ApiTokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ApiTokenService tokens, ApplicationDbContext db) : base(options, logger, encoder)
    {
        _tokens = tokens;
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer " + ApiToken.Prefix, StringComparison.Ordinal)) return AuthenticateResult.NoResult();
        var identity = await _tokens.AuthenticateAsync(header["Bearer ".Length..].Trim(), Context.Connection.RemoteIpAddress?.ToString(), Context.RequestAborted);
        if (identity is null) return AuthenticateResult.Fail("The API token is not valid, expired or revoked.");

        var user = await _db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == identity.UserId, Context.RequestAborted);
        if (user is null || !user.IsActive) return AuthenticateResult.Fail("The token's user is not active.");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
            new("role_id", user.RoleId.ToString()),
            new(ApiRateLimiting.TokenIdClaim, identity.TokenId.ToString()),
            new(ReadOnlyClaim, identity.ReadOnly ? "true" : "false")
        };
        if (user.DepartmentId is { } department) claims.Add(new Claim("department_id", department.ToString()));
        if (user.InstitutionType is { } type) claims.Add(new Claim("institutionType", ((int)type).ToString()));
        if (user.TenantId is { } tenant) claims.Add(new Claim("tenant_id", tenant.ToString()));
        if (user.CampusId is { } campus) claims.Add(new Claim("campus_id", campus.ToString()));
        var profile = await _db.StudentProfiles.AsNoTracking().Where(p => p.UserId == user.Id).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(Context.RequestAborted);
        if (profile is { } p) claims.Add(new Claim("studentProfileId", p.ToString()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}

/// <summary>Plan M14.3 — read-only tokens may only read; tokens cannot manage tokens.</summary>
public sealed class ApiTokenGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.FindFirst(ApiRateLimiting.TokenIdClaim) is not null)
        {
            var reading = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method);
            if (!reading && context.User.FindFirst(ApiTokenAuthenticationHandler.ReadOnlyClaim)?.Value == "true")
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "This API token is read-only." });
                return;
            }
            if (context.Request.Path.StartsWithSegments("/api/v1/api-tokens"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "API tokens are managed after signing in, not with a token." });
                return;
            }
        }
        await next(context);
    }
}
