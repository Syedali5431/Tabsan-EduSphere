namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Renders a named HTML email template with caller-supplied token values.
/// Templates are stored as .html files and use {{TOKEN_NAME}} placeholders.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Returns the rendered HTML string for <paramref name="templateName"/>.html
    /// after substituting every <c>{{KEY}}</c> occurrence with the matching value from
    /// <paramref name="tokens"/>. Token values are HTML-encoded automatically.
    /// Plan M11: a <c>LANGUAGE</c> token ("ur", "ar") selects <c>{templateName}.{language}.html</c> when that file exists.
    /// </summary>
    string Render(string templateName, IDictionary<string, string> tokens);
}
