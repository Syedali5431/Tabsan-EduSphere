using System.Text.Json;
using Tabsan.EduSphere.API.Services.Setup;

namespace Tabsan.EduSphere.API.Middleware;

public sealed class DatabaseSetupGateMiddleware
{
    private const string SetupPath = "/setup";

    private static readonly string[] BypassPrefixes =
    [
        "/health",
        "/metrics",
        "/swagger",
        "/favicon",
        "/portal-uploads"
    ];

    private readonly RequestDelegate _next;

    public DatabaseSetupGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDatabaseSetupService setupService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsSetupPath(path))
        {
            // The Database Setup page (SetupController) asks for no sign-in: it must work on a new server, where there is
            // no database and so no users. Its actions change the database the API uses (save, import), make the API
            // connect to any server typed in (test-connection) and show the connection settings (the page, export,
            // docker-env). So it is open only while the API has no working database (DatabaseSetupRuntimeState.RequiresSetup)
            // and answers 404 once the database works, for everyone.
            // Not "SuperAdmin only": a browser opening this page on the API carries no sign-in token (the portal keeps it).
            // Not "the server itself (loopback)": behind nginx or out-of-process hosting on the same computer every request
            // comes from 127.0.0.1, and forwarded headers can be forged when no known proxy is configured.
            // To change the database of a working server, change EDUSPHERE_DB_CONNECTION or the settings file, delete
            // App_Data/database-setup.json if this page saved one, and restart (Installation and Connection Guide, 2.4).
            var current = await setupService.ValidateCurrentConnectionAsync(force: false, context.RequestAborted);
            if (current.Success)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await _next(context);
            return;
        }

        if (BypassPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var validation = await setupService.ValidateCurrentConnectionAsync(force: false, context.RequestAborted);
        if (validation.Success)
        {
            await _next(context);
            return;
        }

        if (AcceptsHtml(context.Request))
        {
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"/setup?returnUrl={returnUrl}");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";
        var payload = new
        {
            error = "database_setup_required",
            message = validation.Message,
            setupUrl = "/setup"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// <c>/setup</c> and every address under it (<c>/setup/save</c>, <c>/setup/import</c>, …). Only addresses that
    /// <em>start</em> with <c>/setup</c>: two-factor sign-in uses <c>/api/v1/auth/mfa/setup</c> and <c>/api/v1/2fa/setup</c>.
    /// </summary>
    internal static bool IsSetupPath(string path) =>
        path.Equals(SetupPath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(SetupPath + "/", StringComparison.OrdinalIgnoreCase);

    private static bool AcceptsHtml(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        if (string.IsNullOrWhiteSpace(accept))
            return true;

        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
               || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
    }
}
