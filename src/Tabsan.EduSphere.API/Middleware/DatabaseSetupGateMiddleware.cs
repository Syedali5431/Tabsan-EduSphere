using System.Text.Json;
using Tabsan.EduSphere.API.Services.Setup;

namespace Tabsan.EduSphere.API.Middleware;

public sealed class DatabaseSetupGateMiddleware
{
    private static readonly string[] BypassPrefixes =
    [
        "/setup",
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

    private static bool AcceptsHtml(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        if (string.IsNullOrWhiteSpace(accept))
            return true;

        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
               || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
    }
}
