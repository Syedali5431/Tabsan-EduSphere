using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Middleware;

/// <summary>
/// Phase 24 Stage 24.2 — centralized module-license enforcement.
/// Maps API route prefixes to module keys and blocks requests with 403 when a module is inactive.
/// </summary>
public sealed class ModuleLicenseEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public ModuleLicenseEnforcementMiddleware(RequestDelegate next)
        => _next = next;

    private static readonly string[] ExcludedPrefixes =
    [
        "/health",
        "/metrics",
        "/swagger",
        "/api/v1/auth",
        "/api/v1/license",
        "/api/v1/modules",
        "/api/v1/module-registry",
        "/api/v1/institution-policy",
        "/api/v1/labels",
        "/api/v1/dashboard",
        "/api/v1/portal-capabilities",
        "/api/v1/feature-flags",
        "/api/v1/sidebar-menu",
        "/api/v1/portal-settings",
        "/api/v1/tenant-operations"
    ];

    // Longest/specific prefixes first to avoid accidental broad matches.
    private static readonly (string Prefix, string ModuleKey)[] RouteToModule =
    [
        ("/api/v1/result-calculation", "results"),
        ("/api/v1/report-settings", "reports"),
        ("/api/v1/student-lifecycle", "sis"),
        ("/api/v1/registration-import", "sis"),
        ("/api/v1/user-import", "sis"),
        ("/api/v1/report-cards", "sis"),
        ("/api/v1/enrollment", "sis"),
        ("/api/v1/student", "sis"),
        ("/api/v1/payments", "sis"),
        ("/api/v1/parent-portal", "sis"),
        ("/api/v1/department", "departments"),
        ("/api/v1/prerequisite", "courses"),
        ("/api/v1/study-plan", "courses"),
        ("/api/v1/grading-config", "courses"),
        ("/api/v1/degree-audit", "courses"),
        ("/api/v1/program", "courses"),
        ("/api/v1/semester", "courses"),
        ("/api/v1/course", "courses"),
        ("/api/v1/rubric", "assignments"),
        ("/api/v1/assignment", "assignments"),
        ("/api/v1/quiz", "quizzes"),
        ("/api/v1/attendance", "attendance"),
        ("/api/v1/timetable", "attendance"),
        ("/api/v1/progression", "results"),
        ("/api/v1/gradebook", "results"),
        ("/api/v1/result", "results"),
        ("/api/v1/reports", "reports"),
        ("/api/v1/analytics", "reports"),
        ("/api/analytics", "reports"),
        ("/api/v1/notification", "notifications"),
        ("/api/v1/announcement", "notifications"),
        ("/api/v1/discussion", "notifications"),
        ("/api/v1/audit", "advanced_audit"),
        ("/api/v1/fyp", "fyp"),
        ("/api/v1/ai", "ai_chat"),
        ("/api/ai", "ai_chat"),
        ("/api/v1/theme", "themes")
    ];

    public async Task InvokeAsync(HttpContext context, IModuleEntitlementResolver entitlementResolver)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Length == 0 || !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (ExcludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var moduleKey = ResolveModuleKey(path);
        if (moduleKey is null)
        {
            await _next(context);
            return;
        }

        var isActive = await entitlementResolver.IsActiveAsync(moduleKey, context.RequestAborted);
        if (!isActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                $"{{\"message\":\"Module '{moduleKey}' is disabled by license or configuration.\"}}",
                context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private static string? ResolveModuleKey(string path)
    {
        foreach (var (prefix, moduleKey) in RouteToModule)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return moduleKey;
        }

        return null;
    }
}
