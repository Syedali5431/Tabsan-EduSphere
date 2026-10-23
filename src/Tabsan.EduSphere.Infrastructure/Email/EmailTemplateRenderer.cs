using Microsoft.Extensions.Logging;
using System.Net;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Email;

/// <summary>
/// Loads HTML email templates from the <c>Email/Templates/</c> sub-directory
/// of the application's base directory (<see cref="AppContext.BaseDirectory"/>).
/// Token values are HTML-encoded before substitution to prevent XSS in email clients.
/// </summary>
public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly string TemplatesDir =
        Path.Combine(AppContext.BaseDirectory, "Email", "Templates");

    private readonly ILogger<EmailTemplateRenderer> _logger;

    public EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger)
        => _logger = logger;

    public string Render(string templateName, IDictionary<string, string> tokens)
    {
        var filePath = Path.Combine(TemplatesDir, $"{templateName}.html");
        // Plan M11 — the recipient's language (token LANGUAGE) picks a translated template when there is one.
        if (tokens.TryGetValue("LANGUAGE", out var language) && !string.IsNullOrWhiteSpace(language)
            && language.All(char.IsAsciiLetterLower) && language != "en")
        {
            var translated = Path.Combine(TemplatesDir, $"{templateName}.{language}.html");
            if (File.Exists(translated)) filePath = translated;
        }

        string html;
        if (File.Exists(filePath))
        {
            html = File.ReadAllText(filePath);
        }
        else
        {
            _logger.LogWarning(
                "Email template '{Template}' not found at {Path}. Using inline fallback.",
                templateName, filePath);
            html = $"<p>This is an automated message from <strong>Tabsan EduSphere</strong>.</p>";
        }

        foreach (var (key, value) in tokens)
        {
            var encoded = WebUtility.HtmlEncode(value);
            html = html.Replace($"{{{{{key}}}}}", encoded, StringComparison.Ordinal);
        }

        return html;
    }
}
