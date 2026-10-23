using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Html;
using Tabsan.EduSphere.Application.Localization;

namespace Tabsan.EduSphere.Web.Localization;

// Plan M11 — portal localisation. Views write English text as the key (@T["Save"]); other languages come from
// translation files. English output is therefore exactly the markup that was there before.

/// <summary>
/// Translations by language, keyed by the plain English text. Shipped files live in <c>Localization/translations</c>;
/// files uploaded through the Translations page are written to an overrides folder (default <c>App_Data/translations</c>)
/// and win over the shipped ones, so translators can work without a new release.
/// </summary>
public sealed class TranslationStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
    private readonly string _shippedFolder;
    private readonly string _overridesFolder;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _byLanguage = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<(System.Text.RegularExpressions.Regex Pattern, string Template)>> _patterns = new();
    private static readonly System.Text.RegularExpressions.Regex Placeholder = new(@"\{(\d+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);

    public TranslationStore(IWebHostEnvironment env, IConfiguration configuration)
    {
        _shippedFolder = Path.Combine(env.ContentRootPath, "Localization", "translations");
        var configured = configuration["Localization:OverridesPath"];
        _overridesFolder = string.IsNullOrWhiteSpace(configured) ? Path.Combine(env.ContentRootPath, "App_Data", "translations")
            : Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
    }

    public string ShippedFolder => _shippedFolder;

    public IReadOnlyDictionary<string, string> For(string lang) => _byLanguage.GetOrAdd(lang, Load);

    public string? Get(string lang, string key) => For(lang).TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    /// <summary>
    /// Translates a text with values in it through the "{0}" keys (e.g. "Attendance marked for 12 student(s)." with the key
    /// "Attendance marked for {0} student(s)."); null when no key matches.
    /// </summary>
    public string? GetByPattern(string lang, string text)
    {
        var patterns = _patterns.GetOrAdd(lang, l => For(l)
            .Where(p => Placeholder.IsMatch(p.Key))
            .Select(p => (new System.Text.RegularExpressions.Regex(
                "^" + Placeholder.Replace(System.Text.RegularExpressions.Regex.Escape(p.Key).Replace(@"\{", "{"), m => $"(?<p{m.Groups[1].Value}>.+?)") + "$",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant), p.Value))
            .OrderByDescending(p => p.Item1.ToString().Length)
            .ToList());
        foreach (var (pattern, template) in patterns)
        {
            var match = pattern.Match(text);
            if (!match.Success) continue;
            return Placeholder.Replace(template, m => match.Groups["p" + m.Groups[1].Value] is { Success: true } g ? g.Value : m.Value);
        }
        return null;
    }

    private readonly ConcurrentDictionary<string, string> _scriptJson = new();

    /// <summary>
    /// The translations used by the portal scripts (keys whose source is a .js file), as JSON for the page's
    /// <c>portal-text</c> block. Characters such as &lt; are escaped, so the JSON is safe inside a script element.
    /// </summary>
    public string ScriptJson(string lang) => _scriptJson.GetOrAdd(lang, l =>
    {
        var texts = Keys().Where(k => k.Value.Any(f => f.EndsWith(".js", StringComparison.OrdinalIgnoreCase)))
            .Select(k => (k.Key, Value: Get(l, k.Key)))
            .Where(k => k.Value is not null)
            .ToDictionary(k => k.Key, k => k.Value!);
        return JsonSerializer.Serialize(texts);
    });

    /// <summary>Every key the portal uses (generated from the views with the translations, see keys.json).</summary>
    public IReadOnlyDictionary<string, string[]> Keys() => _keys.Value;

    private Lazy<IReadOnlyDictionary<string, string[]>> _keys => _keysCache ??= new(() =>
    {
        var path = Path.Combine(_shippedFolder, "keys.json");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(path)) ?? new Dictionary<string, string[]>()
            : new Dictionary<string, string[]>();
    });
    private Lazy<IReadOnlyDictionary<string, string[]>>? _keysCache;

    /// <summary>The translations uploaded for a language (the overrides file), without the shipped ones.</summary>
    public IReadOnlyDictionary<string, string> ReadOverrides(string lang)
    {
        var path = Path.Combine(_overridesFolder, $"{lang}.json");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();
    }

    public int OverrideCount(string lang) => ReadOverrides(lang).Count;

    private IReadOnlyDictionary<string, string> Load(string lang)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var folder in new[] { _shippedFolder, _overridesFolder })
        {
            var path = Path.Combine(folder, $"{lang}.json");
            if (!File.Exists(path)) continue;
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];
            foreach (var (k, v) in values) if (!string.IsNullOrWhiteSpace(v)) result[Normalize(k)] = v;
        }
        return result;
    }

    /// <summary>Saves translations uploaded by a translator (only values that differ from the shipped file are kept).</summary>
    public int SaveOverrides(string lang, IReadOnlyDictionary<string, string> translations)
    {
        var shippedPath = Path.Combine(_shippedFolder, $"{lang}.json");
        var shipped = File.Exists(shippedPath) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(shippedPath)) ?? [] : [];
        var overrides = translations.Where(t => !string.IsNullOrWhiteSpace(t.Value) && (!shipped.TryGetValue(t.Key, out var s) || s != t.Value))
            .ToDictionary(t => Normalize(t.Key), t => t.Value.Trim());
        Directory.CreateDirectory(_overridesFolder);
        File.WriteAllText(Path.Combine(_overridesFolder, $"{lang}.json"), JsonSerializer.Serialize(new SortedDictionary<string, string>(overrides, StringComparer.Ordinal), Json), new UTF8Encoding(false));
        _byLanguage.TryRemove(lang, out _);
        _patterns.TryRemove(lang, out _);
        _scriptJson.TryRemove(lang, out _);
        return overrides.Count;
    }

    public static string Normalize(string key) => key.Trim();
}

/// <summary>Localised text for views: <c>@T["Save"]</c>, <c>@T.Format("Due {0}", date)</c>, <c>T.Text("Title")</c>.</summary>
public interface IPortalText
{
    /// <summary>Static markup text (the key is the English markup, entities included); returns HTML.</summary>
    IHtmlContent this[string htmlKey] { get; }

    /// <summary>Markup text with values: <c>{0}</c>, <c>{1}</c> … are replaced by the encoded arguments (HTML arguments are kept).</summary>
    IHtmlContent Format(string htmlKey, params object?[] args);

    /// <summary>Plain text (page titles, labels from data such as the institution's vocabulary).</summary>
    string Text(string? key);

    /// <summary>Translated text escaped for a single- or double-quoted JavaScript string (e.g. confirm('…') in an onclick).</summary>
    string Js(string key, params object?[] args);

    string Language { get; }
    bool IsRtl { get; }
}

public sealed class PortalText : IPortalText
{
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);
    private readonly TranslationStore _store;

    public PortalText(TranslationStore store) => _store = store;

    public string Language => PortalLanguages.Normalize(CultureInfo.CurrentUICulture.Name) ?? PortalLanguages.English;

    public bool IsRtl => PortalLanguages.IsRtl(Language);

    private string? Translation(string plainKey) => Language == PortalLanguages.English ? null : _store.Get(Language, TranslationStore.Normalize(plainKey));

    public IHtmlContent this[string htmlKey]
        => Translation(WebUtility.HtmlDecode(htmlKey)) is { } t ? new HtmlString(Encoder.Encode(t)) : new HtmlString(htmlKey);

    public IHtmlContent Format(string htmlKey, params object?[] args)
    {
        var translated = Translation(WebUtility.HtmlDecode(htmlKey));
        var template = translated ?? htmlKey;
        var sb = new StringBuilder();
        var i = 0;
        while (i < template.Length)
        {
            var open = template.IndexOf('{', i);
            var close = open < 0 ? -1 : template.IndexOf('}', open);
            if (open < 0 || close < 0 || !int.TryParse(template.AsSpan(open + 1, close - open - 1), out var index) || index < 0 || index >= args.Length)
            {
                var rest = open < 0 || close < 0 ? template[i..] : template[i..(close + 1)];
                sb.Append(translated is null ? rest : Encoder.Encode(rest));
                i += rest.Length;
                continue;
            }
            var literal = template[i..open];
            sb.Append(translated is null ? literal : Encoder.Encode(literal));
            sb.Append(Render(args[index]));
            i = close + 1;
        }
        return new HtmlString(sb.ToString());
    }

    private static string Render(object? value)
    {
        if (value is IHtmlContent html)
        {
            using var writer = new StringWriter();
            html.WriteTo(writer, Encoder);
            return writer.ToString();
        }
        return Encoder.Encode(Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty);
    }

    private static readonly JavaScriptEncoder ScriptEncoder = JavaScriptEncoder.Create(UnicodeRanges.All);

    public string Js(string key, params object?[] args)
    {
        var text = Text(key);
        if (args.Length > 0)
        {
            var values = args.Select(a => Convert.ToString(a, CultureInfo.CurrentCulture) ?? string.Empty).ToArray();
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\{(\d+)\}",
                m => int.TryParse(m.Groups[1].Value, out var i) && i < values.Length ? values[i] : m.Value);
        }
        return ScriptEncoder.Encode(text);
    }

    public string Text(string? key)
    {
        if (string.IsNullOrEmpty(key) || Language == PortalLanguages.English) return key ?? string.Empty;
        return Translation(key) ?? _store.GetByPattern(Language, key.Trim()) ?? key;
    }
}
