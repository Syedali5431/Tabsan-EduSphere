// P2-S3-02: Domain binding enforcement middleware
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Middleware;

/// <summary>
/// P2-S3-02: Enforces that the application is accessed from the same domain
/// on which the license was originally activated.
///
/// When the stored <see cref="Domain.Licensing.LicenseState.ActivatedDomain"/>
/// does not match the current request host, all requests except the license upload
/// and login endpoints are rejected with HTTP 403.
///
/// This prevents a single license being reused across multiple deployments:
/// once activated on domain A, the portal will not serve requests from domain B
/// until a new license (with domain B in its AllowedDomain) is uploaded.
/// </summary>
public class LicenseDomainMiddleware
{
    private readonly RequestDelegate _next;

    // Endpoints that must remain accessible so an admin can fix the situation.
    private static readonly string[] _allowedPrefixes =
    [
        "/api/v1/auth/login",
        "/api/v1/license/upload",
        "/api/v1/license/status"
    ];

    public LicenseDomainMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ILicenseRepository licenseRepo, IWebHostEnvironment env)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Always allow the auth / license endpoints so the admin is never fully locked out. The Database Setup page too:
        // DatabaseSetupGateMiddleware lets it through only while there is no working database, so the licence cannot be read.
        if (_allowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || DatabaseSetupGateMiddleware.IsSetupPath(path))
        {
            await _next(context);
            return;
        }

        // Allow localhost in Development mode for testing
        var requestHost = context.Request.Host.Host;
        if (env.IsDevelopment() && (requestHost == "localhost" || requestHost == "127.0.0.1"))
        {
            await _next(context);
            return;
        }

        var state = await licenseRepo.GetCurrentAsync(context.RequestAborted);

        if (state?.ActivatedDomain is { Length: > 0 } activatedDomain)
        {
            if (!string.Equals(activatedDomain, requestHost, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $"{{\"message\":\"This license is bound to domain '{activatedDomain}'. " +
                    $"Access from '{requestHost}' is not permitted. " +
                    "Please upload a valid license for this domain via /api/v1/license/upload.\"}}",
                    context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }
}
