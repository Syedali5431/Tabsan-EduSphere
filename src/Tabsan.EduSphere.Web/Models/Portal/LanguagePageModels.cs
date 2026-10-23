using Tabsan.EduSphere.Web.Localization;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M11 — Language & Translations page.

public sealed class LanguageSettingsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }

    /// <summary>"en", "ur", "ar" or "auto" (the browser's language).</summary>
    public string DefaultLanguage { get; set; } = "en";

    public IReadOnlyList<TranslationWorkbook.LanguageCoverage> Coverage { get; set; } = [];
}
