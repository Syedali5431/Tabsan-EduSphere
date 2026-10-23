using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Localization;

// Plan M11 — languages shared by the API and the portal, API text translation (messages, validation, email subjects,
// PDF labels) and the institution's default language.

/// <summary>The portal's languages: English (default), Urdu and Arabic (both right-to-left).</summary>
public static class PortalLanguages
{
    public const string English = "en";

    /// <summary>Institution default meaning "use the browser's language".</summary>
    public const string Auto = "auto";

    public static readonly IReadOnlyList<string> All = ["en", "ur", "ar"];

    public static bool IsSupported(string? lang) => lang is not null && All.Contains(lang);

    public static bool IsRtl(string? lang) => lang is "ur" or "ar";

    public static string NativeName(string lang) => lang switch { "ur" => "اردو", "ar" => "العربية", _ => "English" };

    public static string EnglishName(string lang) => lang switch { "ur" => "Urdu", "ar" => "Arabic", _ => "English" };

    /// <summary>Normalises "ar-SA", "ur_PK", "UR" … to a supported language (null when none).</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var two = value.Trim().Replace('_', '-').Split('-')[0].ToLowerInvariant();
        return IsSupported(two) ? two : null;
    }

    /// <summary>The first supported language of an Accept-Language header, by quality (null when none).</summary>
    public static string? FromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return null;
        var ranked = header.Split(',').Select((part, index) =>
        {
            var pieces = part.Split(';');
            var q = 1.0;
            foreach (var p in pieces.Skip(1))
            {
                var kv = p.Trim();
                if (kv.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(kv[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) q = parsed;
            }
            return (Lang: Normalize(pieces[0]), Q: q, Index: index);
        });
        return ranked.Where(r => r.Lang is not null && r.Q > 0).OrderByDescending(r => r.Q).ThenBy(r => r.Index).Select(r => r.Lang).FirstOrDefault();
    }

    /// <summary>The language of the current request (the UI culture set by the localisation middleware).</summary>
    public static string Current => Normalize(CultureInfo.CurrentUICulture.Name) ?? English;
}

/// <summary>Translates the API's English text (messages, validation errors, email subjects, PDF labels).</summary>
public interface ITextLocalizer
{
    /// <summary>The English text in <paramref name="language"/> (default: the current request's language); unchanged when there is no translation.</summary>
    string Translate(string english, string? language = null);

    /// <summary>Whether a translation exists (exactly or through a "{0}" pattern).</summary>
    bool Has(string english, string language);
}

/// <summary>
/// Translations shipped as embedded JSON (<c>Localization/Resources/api-{lang}.json</c>; not "api.ur.json", which MSBuild would treat as an Urdu satellite resource), keyed by the English text.
/// Keys with <c>{0}</c>, <c>{1}</c> … are patterns: "User {0} not found." also translates "User 42 not found.".
/// </summary>
public sealed class TextLocalizer : ITextLocalizer
{
    private static readonly ConcurrentDictionary<string, Catalog> Catalogs = new();
    private static readonly Regex Placeholder = new(@"\{(\d+)\}", RegexOptions.Compiled);

    public string Translate(string english, string? language = null)
    {
        if (string.IsNullOrEmpty(english)) return english;
        var lang = PortalLanguages.Normalize(language) ?? PortalLanguages.Current;
        if (lang == PortalLanguages.English) return english;
        return For(lang).Translate(english) ?? english;
    }

    public bool Has(string english, string language) => For(language).Translate(english) is not null;

    /// <summary>Every English key of the API catalogue (for the translation export and the missing-key test).</summary>
    public static IReadOnlyCollection<string> Keys() => For("ur").Keys.Union(For("ar").Keys).ToList();

    private static Catalog For(string lang) => Catalogs.GetOrAdd(lang, l => new Catalog(Load(l)));

    private static Dictionary<string, string> Load(string lang)
    {
        var assembly = typeof(TextLocalizer).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith($".api-{lang}.json", StringComparison.OrdinalIgnoreCase));
        if (name is null) return [];
        using var stream = assembly.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
    }

    private sealed class Catalog
    {
        private readonly Dictionary<string, string> _exact = new(StringComparer.Ordinal);
        private readonly List<(Regex Pattern, string Template)> _patterns = [];

        public Catalog(Dictionary<string, string> values)
        {
            foreach (var (key, value) in values)
            {
                Keys.Add(key);
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (Placeholder.IsMatch(key))
                {
                    var regex = "^" + Placeholder.Replace(Regex.Escape(key).Replace(@"\{", "{"), m => $"(?<p{m.Groups[1].Value}>.+?)") + "$";
                    _patterns.Add((new Regex(regex, RegexOptions.Singleline | RegexOptions.CultureInvariant), value));
                }
                else _exact[key] = value;
            }
            // Longer (more specific) patterns first.
            _patterns.Sort((a, b) => b.Pattern.ToString().Length.CompareTo(a.Pattern.ToString().Length));
        }

        public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);

        public string? Translate(string english)
        {
            var trimmed = english.Trim();
            if (_exact.TryGetValue(trimmed, out var exact)) return exact;
            foreach (var (pattern, template) in _patterns)
            {
                var match = pattern.Match(trimmed);
                if (!match.Success) continue;
                return Placeholder.Replace(template, m => match.Groups["p" + m.Groups[1].Value] is { Success: true } g ? g.Value : m.Value);
            }
            return null;
        }
    }
}

/// <summary>The institution's default portal language (portal setting <c>default_language</c>).</summary>
public interface IPortalLanguageService
{
    /// <summary>"en", "ur", "ar" or "auto" (the browser's language).</summary>
    Task<string> GetDefaultAsync(CancellationToken ct = default);

    Task SetDefaultAsync(string language, CancellationToken ct = default);

    /// <summary>The language to write to someone in (emails): their own choice, else the institution default (English for "auto").</summary>
    Task<string> ForRecipientAsync(string? preferredLanguage, CancellationToken ct = default);
}

public sealed class PortalLanguageService : IPortalLanguageService
{
    public const string SettingKey = "default_language";
    private readonly ISettingsRepository _settings;

    public PortalLanguageService(ISettingsRepository settings) => _settings = settings;

    public async Task<string> GetDefaultAsync(CancellationToken ct = default)
    {
        var stored = (await _settings.GetPortalSettingAsync(SettingKey, ct))?.Trim().ToLowerInvariant();
        return stored == PortalLanguages.Auto ? PortalLanguages.Auto : PortalLanguages.Normalize(stored) ?? PortalLanguages.English;
    }

    public async Task SetDefaultAsync(string language, CancellationToken ct = default)
    {
        var value = language?.Trim().ToLowerInvariant();
        if (value != PortalLanguages.Auto && !PortalLanguages.IsSupported(value))
            throw new ArgumentException($"Unsupported language '{language}'. Use one of: {string.Join(", ", PortalLanguages.All)}, auto.");
        await _settings.UpsertPortalSettingAsync(SettingKey, value!, ct);
        await _settings.SaveChangesAsync(ct);
    }

    public async Task<string> ForRecipientAsync(string? preferredLanguage, CancellationToken ct = default)
    {
        if (PortalLanguages.Normalize(preferredLanguage) is { } own) return own;
        var fallback = await GetDefaultAsync(ct);
        return fallback == PortalLanguages.Auto ? PortalLanguages.English : fallback;
    }
}
