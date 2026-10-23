using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Localization;
using Tabsan.EduSphere.Web.Localization;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M11 — the institution's default language and the translation workflow (export to Excel for translators,
/// import the translated workbook as overrides). SuperAdmin only.
/// </summary>
public partial class PortalController
{
    private const string LanguageSettingsDenied = "Languages and translations are managed by the SuperAdmin.";

    private TranslationStore Translations => HttpContext.RequestServices.GetRequiredService<TranslationStore>();

    [HttpGet]
    public async Task<IActionResult> LanguageSettings(CancellationToken ct)
    {
        ViewData["Title"] = "Language & Translations";
        if (_api.GetSessionIdentity() is not { IsSuperAdmin: true }) return AccessDenied(LanguageSettingsDenied);
        var model = new LanguageSettingsPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString(),
            Coverage = TranslationWorkbook.Coverage(Translations)
        };
        try { model.DefaultLanguage = await _api.GetDefaultLanguageAsync(ct) ?? PortalLanguages.English; }
        catch (Exception ex) { model.Message ??= ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDefaultLanguage(string? language, CancellationToken ct)
    {
        if (_api.GetSessionIdentity() is not { IsSuperAdmin: true }) return AccessDenied(LanguageSettingsDenied);
        var raw = await _api.SetDefaultLanguageAsync(language ?? PortalLanguages.English, ct);
        PortalLanguageResolver.ForgetInstitutionDefault(HttpContext);
        TempData["PortalMessage"] = raw.Ok ? "Default language saved." : raw.Message ?? "The settings were not saved.";
        return RedirectToAction(nameof(LanguageSettings));
    }

    [HttpGet]
    public IActionResult ExportTranslations()
    {
        if (_api.GetSessionIdentity() is not { IsSuperAdmin: true }) return AccessDenied(LanguageSettingsDenied);
        return File(TranslationWorkbook.Export(Translations), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"edusphere-translations-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public IActionResult ImportTranslations(IFormFile? file)
    {
        if (_api.GetSessionIdentity() is not { IsSuperAdmin: true }) return AccessDenied(LanguageSettingsDenied);
        if (file is null || file.Length == 0 || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["PortalMessage"] = "Choose an .xlsx file to import.";
            return RedirectToAction(nameof(LanguageSettings));
        }
        try
        {
            using var stream = file.OpenReadStream();
            var result = TranslationWorkbook.Import(Translations, stream);
            var parts = result.Saved.Select(s => $"{PortalLanguages.EnglishName(s.Key)}: {s.Value}");
            TempData["PortalMessage"] = $"Translations imported — changed texts kept per language: {string.Join(", ", parts)}."
                + (result.UnknownKeys > 0 ? $" {result.UnknownKeys} row(s) were not portal texts and were skipped." : "")
                + (result.Problems.Count > 0 ? " " + string.Join(" ", result.Problems) : "");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
        {
            TempData["PortalMessage"] = $"The file could not be imported: {ex.Message}";
        }
        return RedirectToAction(nameof(LanguageSettings));
    }
}
