using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Plan M3.3: annotation geometry, in page-relative coordinates (0–1 of the page's width and height).
/// <list type="bullet">
///   <item>Highlight, Strikeout, Box: <c>{"x":…,"y":…,"w":…,"h":…}</c> (top-left corner, size).</item>
///   <item>Text, Pin: <c>{"x":…,"y":…}</c>; Text needs the text in the comment.</item>
///   <item>Freehand: <c>{"points":[[x,y],…]}</c>, 2–2000 points, optional <c>"width"</c> (0.001–0.05).</item>
/// </list>
/// The stored JSON is re-serialised from the parsed numbers (rounded to 5 decimals), never echoed.
/// </summary>
public static partial class AnnotationGeometry
{
    public const int MaxPage = 2000;
    public const int MaxFreehandPoints = 2000;

    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColor();

    public static (AnnotationType Type, string? Json, string? Color, string? Error) Validate(SaveAnnotationRequest request)
    {
        if (!Enum.TryParse<AnnotationType>(request.Type, true, out var type) || !Enum.IsDefined(type))
            return (default, null, null, "Unknown annotation type.");
        if (request.PageNumber is < 1 or > MaxPage) return (type, null, null, "Page number is out of range.");
        if (request.Color is { Length: > 0 } c && !HexColor().IsMatch(c)) return (type, null, null, "Colour must be #rgb or #rrggbb.");
        if (type == AnnotationType.Text && string.IsNullOrWhiteSpace(request.Comment)) return (type, null, null, "Text annotations need text.");

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(request.GeometryJson) ? "{}" : request.GeometryJson);
            root = doc.RootElement.Clone();
        }
        catch (JsonException) { return (type, null, null, "Geometry is not valid JSON."); }
        if (root.ValueKind != JsonValueKind.Object) return (type, null, null, "Geometry must be an object.");

        string? error;
        string? json;
        switch (type)
        {
            case AnnotationType.Highlight:
            case AnnotationType.Strikeout:
            case AnnotationType.Box:
                (json, error) = Rect(root);
                break;
            case AnnotationType.Text:
            case AnnotationType.Pin:
                (json, error) = Point(root);
                break;
            default:
                (json, error) = Freehand(root);
                break;
        }
        return (type, json, string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.ToLowerInvariant(), error);
    }

    public static bool TryReadRect(string json, out double x, out double y, out double w, out double h)
    {
        x = y = w = h = 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            x = r.GetProperty("x").GetDouble(); y = r.GetProperty("y").GetDouble();
            w = r.GetProperty("w").GetDouble(); h = r.GetProperty("h").GetDouble();
            return true;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { return false; }
    }

    public static bool TryReadPoint(string json, out double x, out double y)
    {
        x = y = 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            x = doc.RootElement.GetProperty("x").GetDouble();
            y = doc.RootElement.GetProperty("y").GetDouble();
            return true;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { return false; }
    }

    public static bool TryReadPath(string json, out List<(double X, double Y)> points, out double width)
    {
        points = new();
        width = 0.004;
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var p in doc.RootElement.GetProperty("points").EnumerateArray())
                points.Add((p[0].GetDouble(), p[1].GetDouble()));
            if (doc.RootElement.TryGetProperty("width", out var wEl)) width = wEl.GetDouble();
            return points.Count >= 2;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException) { return false; }
    }

    private static (string?, string?) Rect(JsonElement r)
    {
        if (!Num(r, "x", out var x) || !Num(r, "y", out var y) || !Num(r, "w", out var w) || !Num(r, "h", out var h))
            return (null, "A rectangle needs x, y, w and h.");
        if (w <= 0 || h <= 0 || x + w > 1.0001 || y + h > 1.0001) return (null, "The rectangle must lie inside the page.");
        return ($"{{\"x\":{F(x)},\"y\":{F(y)},\"w\":{F(w)},\"h\":{F(h)}}}", null);
    }

    private static (string?, string?) Point(JsonElement r)
        => Num(r, "x", out var x) && Num(r, "y", out var y)
            ? ($"{{\"x\":{F(x)},\"y\":{F(y)}}}", null)
            : (null, "A point needs x and y inside the page.");

    private static (string?, string?) Freehand(JsonElement r)
    {
        if (!r.TryGetProperty("points", out var pts) || pts.ValueKind != JsonValueKind.Array) return (null, "A drawing needs points.");
        var count = pts.GetArrayLength();
        if (count < 2 || count > MaxFreehandPoints) return (null, $"A drawing needs 2–{MaxFreehandPoints} points.");
        var parts = new List<string>(count);
        foreach (var p in pts.EnumerateArray())
        {
            double px = 0, py = 0;
            if (p.ValueKind != JsonValueKind.Array || p.GetArrayLength() != 2
                || p[0].ValueKind != JsonValueKind.Number || p[1].ValueKind != JsonValueKind.Number
                || !p[0].TryGetDouble(out px) || !p[1].TryGetDouble(out py) || !InPage(px) || !InPage(py))
                return (null, "Every point must be [x, y] inside the page.");
            parts.Add($"[{F(px)},{F(py)}]");
        }
        var width = 0.004;
        if (r.TryGetProperty("width", out var wEl)
            && (wEl.ValueKind != JsonValueKind.Number || !wEl.TryGetDouble(out width) || width is < 0.001 or > 0.05))
            return (null, "Line width must be between 0.001 and 0.05.");
        return ($"{{\"points\":[{string.Join(",", parts)}],\"width\":{F(width)}}}", null);
    }

    private static bool Num(JsonElement r, string name, out double value)
    {
        value = 0;
        return r.TryGetProperty(name, out var el)
            && el.ValueKind == JsonValueKind.Number
            && el.TryGetDouble(out value)
            && InPage(value);
    }

    private static bool InPage(double v) => !double.IsNaN(v) && v >= 0 && v <= 1;

    private static string F(double v) => Math.Round(v, 5).ToString(CultureInfo.InvariantCulture);
}
