using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Tabsan.EduSphere.Application.Mobile;

/// <summary>
/// Plan M12 — turns a report card's stored details (free-form JSON written by whoever generated it) into label/value lines:
/// plain values become "Label: value", lists become a heading with one line per entry (named by its subject/course/name),
/// nested objects become a heading with their own lines. Anything unreadable is shown as text.
/// </summary>
public static class ReportCardLines
{
    private const int MaxLines = 200;
    private const int MaxDepth = 4;
    private static readonly string[] NameKeys = ["subject", "subjectName", "course", "courseTitle", "courseName", "name", "title", "label"];

    public static IReadOnlyList<MobileReportCardLine> From(string? payloadJson)
    {
        var lines = new List<MobileReportCardLine>();
        if (string.IsNullOrWhiteSpace(payloadJson)) return lines;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            switch (root.ValueKind)
            {
                case JsonValueKind.Object: AddObject(lines, root, 0); break;
                case JsonValueKind.Array: AddItems(lines, root); break;
                default: lines.Add(new MobileReportCardLine("Details", Text(root))); break;
            }
        }
        catch (JsonException)
        {
            lines.Add(new MobileReportCardLine("Details", payloadJson.Length > 500 ? payloadJson[..500] : payloadJson));
        }
        return lines.Count > MaxLines ? lines.Take(MaxLines).ToList() : lines;
    }

    private static void AddObject(List<MobileReportCardLine> lines, JsonElement obj, int depth)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (lines.Count >= MaxLines) return;
            switch (p.Value.ValueKind)
            {
                case JsonValueKind.Array:
                    lines.Add(new MobileReportCardLine(Humanize(p.Name), "", IsHeading: true));
                    AddItems(lines, p.Value);
                    break;
                case JsonValueKind.Object when depth < MaxDepth:
                    lines.Add(new MobileReportCardLine(Humanize(p.Name), "", IsHeading: true));
                    AddObject(lines, p.Value, depth + 1);
                    break;
                case JsonValueKind.Object:
                    break;
                default:
                    lines.Add(new MobileReportCardLine(Humanize(p.Name), Text(p.Value)));
                    break;
            }
        }
    }

    private static void AddItems(List<MobileReportCardLine> lines, JsonElement array)
    {
        foreach (var item in array.EnumerateArray())
        {
            if (lines.Count >= MaxLines) return;
            if (item.ValueKind != JsonValueKind.Object)
            {
                lines.Add(new MobileReportCardLine(Text(item), ""));
                continue;
            }
            var values = item.EnumerateObject().Where(p => p.Value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array)).ToList();
            var name = values.FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.String && NameKeys.Contains(p.Name, StringComparer.OrdinalIgnoreCase));
            if (name.Value.ValueKind == JsonValueKind.Undefined)
                name = values.FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.String);
            var rest = values.Where(p => p.Name != name.Name || name.Value.ValueKind == JsonValueKind.Undefined)
                .Select(p => $"{Humanize(p.Name)}: {Text(p.Value)}");
            lines.Add(new MobileReportCardLine(name.Value.ValueKind == JsonValueKind.Undefined ? "" : Text(name.Value), string.Join(" · ", rest)));
        }
    }

    private static string Text(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.Number => value.TryGetDecimal(out var d) ? d.ToString("0.##", CultureInfo.InvariantCulture) : value.GetRawText(),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Null or JsonValueKind.Undefined => "—",
        _ => value.GetRawText()
    };

    /// <summary>"courseTitle" / "total_marks" → "Course title" / "Total marks".</summary>
    public static string Humanize(string key)
    {
        // Words: an all-capital run (termGPA → term, GPA), a capitalised or lower-case word, digits stay with their word.
        var words = Words.Matches(key ?? "").Select(m => m.Value).ToList();
        if (words.Count == 0) return (key ?? "").Trim();
        return string.Join(' ', words.Select((w, i) =>
            (w.Length >= 2 && w.All(c => !char.IsLetter(c) || char.IsUpper(c))) || Acronyms.Contains(w) ? w.ToUpperInvariant()
            : i == 0 ? char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()
            : w.ToLowerInvariant()));
    }

    private static readonly Regex Words = new("[A-Z]{2,}(?![a-z])|[A-Z]?[a-z0-9]+|[A-Z]|[0-9]+", RegexOptions.Compiled);
    private static readonly HashSet<string> Acronyms = new(StringComparer.OrdinalIgnoreCase) { "gpa", "cgpa", "sgpa", "id", "cnic" };
}
