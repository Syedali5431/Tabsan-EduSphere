using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Tabsan.EduSphere.Application.Localization;

namespace Tabsan.EduSphere.Web.Localization;

/// <summary>
/// Plan M11.4 — translation files for translators: an Excel workbook with one row per English text and a column per
/// language (export), and reading the translated workbook back (import, saved as overrides).
/// </summary>
public static class TranslationWorkbook
{
    private static readonly Regex Placeholder = new(@"\{\d+\}", RegexOptions.Compiled);
    private static readonly string[] Translated = PortalLanguages.All.Where(l => l != PortalLanguages.English).ToArray();

    public sealed record LanguageCoverage(string Language, string Name, int Keys, int Translated, int Overrides, IReadOnlyList<string> MissingSample);

    public sealed record ImportResult(IReadOnlyDictionary<string, int> Saved, int UnknownKeys, IReadOnlyList<string> Problems);

    /// <summary>How much of each language is translated (shipped files plus overrides).</summary>
    public static IReadOnlyList<LanguageCoverage> Coverage(TranslationStore store)
    {
        var keys = store.Keys().Keys.ToList();
        return Translated.Select(lang =>
        {
            var missing = keys.Where(k => store.Get(lang, k) is null).ToList();
            return new LanguageCoverage(lang, PortalLanguages.EnglishName(lang), keys.Count, keys.Count - missing.Count, store.OverrideCount(lang), missing.Take(25).ToList());
        }).ToList();
    }

    public static byte[] Export(TranslationStore store)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Translations");
        var headers = new List<string> { "English (key)" };
        headers.AddRange(Translated.Select(l => $"{PortalLanguages.EnglishName(l)} ({l})"));
        headers.Add("Used in");
        for (var c = 0; c < headers.Count; c++) sheet.Cell(1, c + 1).Value = headers[c];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);

        var row = 2;
        foreach (var (key, sources) in store.Keys().OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sheet.Cell(row, 1).Value = key;
            for (var i = 0; i < Translated.Length; i++)
            {
                var cell = sheet.Cell(row, i + 2);
                cell.Value = store.Get(Translated[i], key) ?? string.Empty;
                cell.Style.Alignment.SetReadingOrder(XLAlignmentReadingOrderValues.RightToLeft);
            }
            sheet.Cell(row, Translated.Length + 2).Value = string.Join(", ", sources.Take(5)) + (sources.Length > 5 ? $" (+{sources.Length - 5})" : "");
            row++;
        }
        sheet.Column(1).Width = 60;
        for (var i = 0; i < Translated.Length; i++) sheet.Column(i + 2).Width = 60;
        sheet.Column(Translated.Length + 2).Width = 40;
        sheet.Columns().Style.Alignment.WrapText = true;

        var help = workbook.Worksheets.Add("Help");
        var lines = new[]
        {
            "How to translate",
            "1. Fill in or correct the language columns. Do not change the English column — it is the key the portal looks up.",
            "2. Keep {0}, {1} … exactly as they are: they are replaced by names, dates and numbers (their order may change).",
            "3. Leave a cell empty to keep the current translation.",
            "4. Save as .xlsx and import it on the Language & Translations page. Only changed texts are stored; the portal uses them at once.",
        };
        for (var i = 0; i < lines.Length; i++) help.Cell(i + 1, 1).Value = lines[i];
        help.Cell(1, 1).Style.Font.Bold = true;
        help.Column(1).Width = 120;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Reads a translated workbook and saves each language column as overrides.</summary>
    public static ImportResult Import(TranslationStore store, Stream file)
    {
        using var workbook = new XLWorkbook(file);
        var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Translations", StringComparison.OrdinalIgnoreCase)) ?? workbook.Worksheet(1);
        var header = sheet.Row(1);
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var columns = new Dictionary<string, int>();
        for (var c = 2; c <= lastColumn; c++)
        {
            var title = header.Cell(c).GetString();
            var lang = Translated.FirstOrDefault(l => title.Contains($"({l})", StringComparison.OrdinalIgnoreCase));
            if (lang is not null) columns[lang] = c;
        }
        if (columns.Count == 0)
            throw new InvalidOperationException("No language columns were found. Export the workbook first and keep its header row (e.g. \"Urdu (ur)\").");

        var known = store.Keys();
        var values = columns.Keys.ToDictionary(l => l, _ => new Dictionary<string, string>(StringComparer.Ordinal));
        var problems = new List<string>();
        var unknown = 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var key = TranslationStore.Normalize(sheet.Cell(r, 1).GetString());
            if (key.Length == 0) continue;
            if (!known.ContainsKey(key)) { unknown++; continue; }
            var holes = Holes(key);
            foreach (var (lang, column) in columns)
            {
                var text = sheet.Cell(r, column).GetString().Trim();
                if (text.Length == 0) continue;
                if (Holes(text) != holes)
                {
                    if (problems.Count < 20) problems.Add($"Row {r} ({lang}): keep the placeholders {holes} — “{key}”.");
                    continue;
                }
                values[lang][key] = text;
            }
        }

        var saved = new Dictionary<string, int>();
        foreach (var (lang, texts) in values)
        {
            // Keep the existing overrides that this file does not mention.
            var merged = new Dictionary<string, string>(store.ReadOverrides(lang), StringComparer.Ordinal);
            foreach (var (k, v) in texts) merged[k] = v;
            saved[lang] = store.SaveOverrides(lang, merged);
        }
        return new ImportResult(saved, unknown, problems);
    }

    private static string Holes(string text) => string.Join(",", Placeholder.Matches(text).Select(m => m.Value).OrderBy(v => v, StringComparer.Ordinal));
}
