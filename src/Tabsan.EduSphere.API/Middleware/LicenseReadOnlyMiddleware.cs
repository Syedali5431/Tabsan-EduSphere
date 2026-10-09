using Tabsan.EduSphere.Domain.Licensing;
using Tabsan.EduSphere.Infrastructure.Licensing;

namespace Tabsan.EduSphere.API.Middleware;

/// <summary>
/// Puts the API into read-only mode when there is no active license (missing, expired, or
/// failed verification/tamper checks). GET/HEAD/OPTIONS requests continue to work so users can
/// still view their data; every write is rejected with 403 until a valid license is uploaded.
/// Authentication and license endpoints stay open so a Super Admin can always sign in and
/// upload a renewal.
/// </summary>
public sealed class LicenseReadOnlyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _enabled;

    private static readonly string[] _alwaysAllowedPrefixes =
    [
        "/api/v1/auth",
        "/api/v1/license",
        "/setup",
        "/health",
        "/metrics",
        "/swagger"
    ];

    public LicenseReadOnlyMiddleware(RequestDelegate next, IConfiguration configuration, IWebHostEnvironment env)
    {
        _next = next;
        _enabled = configuration.GetValue("Licensing:EnforceReadOnlyWhenUnlicensed", true)
                   && !env.IsEnvironment("Testing");
    }

    public async Task InvokeAsync(HttpContext context, LicenseStatusCache cache, IServiceProvider services)
    {
        if (!_enabled || HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
            _alwaysAllowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var status = cache.TryGet();
        if (status is null)
        {
            var validator = services.GetRequiredService<LicenseValidationService>();
            status = await validator.ValidateCurrentAsync(context.RequestAborted);
            if (status == LicenseStatus.Active)
                status = cache.TryGet() ?? status;
        }

        if (status == LicenseStatus.Active)
        {
            await _next(context);
            return;
        }

        var reason = status == LicenseStatus.Expired
            ? "The product license has expired."
            : "No valid product license is active.";

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            message = $"{reason} The portal is in read-only mode. A Super Admin must upload a valid license under Settings → License Update.",
            licenseStatus = status.ToString()
        }, context.RequestAborted);
    }
}
