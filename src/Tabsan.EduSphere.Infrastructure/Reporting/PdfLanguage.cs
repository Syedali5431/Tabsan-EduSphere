using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Tabsan.EduSphere.Application.Localization;

namespace Tabsan.EduSphere.Infrastructure.Reporting;

/// <summary>
/// Plan M11 — PDFs in the language of the request (the portal forwards it as Accept-Language). For Urdu and Arabic the
/// page runs right to left and text uses fonts with Arabic-script glyphs, falling back through the list (Noto fonts on
/// servers that install them — see the Dockerfile — then Segoe UI / Arial, which carry Arabic script on Windows).
/// Official certificates keep their template language and layout.
/// </summary>
public static class PdfLanguage
{
    private static readonly TextLocalizer Localizer = new();

    public static string Language => PortalLanguages.Current;

    public static bool IsRtl => PortalLanguages.IsRtl(Language);

    /// <summary>The English label in the request language (unchanged when there is no translation).</summary>
    public static string Text(string english) => Localizer.Translate(english, Language);

    /// <summary>Right-to-left pages for Urdu and Arabic.</summary>
    public static void Apply(PageDescriptor page)
    {
        if (IsRtl) page.ContentFromRightToLeft();
    }

    /// <summary>Font families for the request language: the document's own font first for English, Arabic-script fonts first otherwise.</summary>
    public static string[] Fonts(string? latin = null)
    {
        var fallbacks = Language == "ur"
            ? new[] { "Noto Nastaliq Urdu", "Noto Naskh Arabic", "Noto Sans Arabic", "Segoe UI", "Arial" }
            : IsRtl
                ? new[] { "Noto Naskh Arabic", "Noto Sans Arabic", "Segoe UI", "Arial" }
                : Array.Empty<string>();
        return IsRtl
            ? fallbacks.Concat(latin is null ? [] : [latin]).ToArray()
            : latin is null ? ["Segoe UI", "Arial"] : [latin, "Segoe UI", "Arial"];
    }

    /// <summary>Applies the request language's fonts to a text style (keeps the style unchanged for English without a font).</summary>
    public static TextStyle WithFonts(this TextStyle style, string? latin = null)
        => IsRtl || latin is not null ? style.FontFamily(Fonts(latin)) : style;
}
