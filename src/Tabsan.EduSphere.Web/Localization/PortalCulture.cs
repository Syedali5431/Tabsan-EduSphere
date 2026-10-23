using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Caching.Memory;
using Tabsan.EduSphere.Application.Localization;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Localization;

// Plan M11 — which language a request is shown in, and the cultures used to format dates and numbers.

/// <summary>
/// Cultures per language. English keeps the server's culture, so English pages format exactly as before. Urdu (ur-PK) and
/// Arabic (ar-SA) always use the Gregorian calendar and "." / "," number separators: dates and decimals typed into forms and
/// sent to the API must mean the same in every language (Saudi Arabic would otherwise default to the Umm al-Qura calendar).
/// </summary>
public static class PortalCultures
{
    private static readonly CultureInfo EnglishCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName is "ur" or "ar"
        ? CultureInfo.ReadOnly(new CultureInfo("en-US")) : CultureInfo.ReadOnly(CultureInfo.CurrentCulture);
    private static readonly CultureInfo UrduCulture = Build("ur-PK");
    private static readonly CultureInfo ArabicCulture = Build("ar-SA");

    public static CultureInfo English => EnglishCulture;

    public static IList<CultureInfo> All => [EnglishCulture, UrduCulture, ArabicCulture];

    public static CultureInfo For(string? lang) => lang switch { "ur" => UrduCulture, "ar" => ArabicCulture, _ => EnglishCulture };

    private static CultureInfo Build(string name)
    {
        var culture = (CultureInfo)new CultureInfo(name).Clone();
        var gregorian = culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
        if (gregorian is not null) culture.DateTimeFormat.Calendar = gregorian;
        else culture.DateTimeFormat.Calendar = new GregorianCalendar();
        var invariant = CultureInfo.InvariantCulture.NumberFormat;
        var numbers = culture.NumberFormat;
        numbers.NumberDecimalSeparator = numbers.CurrencyDecimalSeparator = numbers.PercentDecimalSeparator = invariant.NumberDecimalSeparator;
        numbers.NumberGroupSeparator = numbers.CurrencyGroupSeparator = numbers.PercentGroupSeparator = invariant.NumberGroupSeparator;
        numbers.NegativeSign = invariant.NegativeSign;
        numbers.PositiveSign = invariant.PositiveSign;
        numbers.PercentSymbol = invariant.PercentSymbol;
        numbers.DigitSubstitution = DigitShapes.None;
        return CultureInfo.ReadOnly(culture);
    }
}

/// <summary>
/// Picks the language: <c>?lang=</c> → the signed-in user's saved choice → the language cookie → the institution default →
/// the browser's Accept-Language (only when the institution default is "browser") → English.
/// </summary>
public sealed class PortalLanguageResolver
{
    public const string CookieName = ".Tabsan.EduSphere.Lang";
    public const string QueryKey = "lang";
    private const string DefaultsCacheKey = "portal.lang.defaults";
    private const string PreferenceSessionPrefix = "portal.lang.pref:";
    private const string NoPreference = "-";

    public static async Task<string> ResolveAsync(HttpContext context)
    {
        if (PortalLanguages.Normalize(context.Request.Query[QueryKey]) is { } fromQuery) return fromQuery;
        if (await UserPreferenceAsync(context) is { } own) return own;
        if (PortalLanguages.Normalize(context.Request.Cookies[CookieName]) is { } fromCookie) return fromCookie;
        var fallback = await InstitutionDefaultAsync(context);
        if (fallback != PortalLanguages.Auto) return fallback;
        return PortalLanguages.FromAcceptLanguage(context.Request.Headers.AcceptLanguage) ?? PortalLanguages.English;
    }

    /// <summary>The signed-in user's saved language (read from the API once per session).</summary>
    private static async Task<string?> UserPreferenceAsync(HttpContext context)
    {
        var api = context.RequestServices.GetService<IEduApiClient>();
        if (api is null || !api.IsConnected()) return null;
        var user = api.GetSessionIdentity()?.UserName;
        if (string.IsNullOrWhiteSpace(user)) return null;
        var key = PreferenceSessionPrefix + user.ToLowerInvariant();
        string? stored;
        try { stored = context.Session.GetString(key); }
        catch (InvalidOperationException) { return null; } // no session (e.g. static files)
        if (stored is null)
        {
            string? fromApi = null;
            try { fromApi = await api.GetMyLanguageAsync(context.RequestAborted); }
            catch { /* the API is unavailable — fall back to the other sources for this session */ }
            stored = PortalLanguages.Normalize(fromApi) ?? NoPreference;
            context.Session.SetString(key, stored);
        }
        return stored == NoPreference ? null : stored;
    }

    /// <summary>Remembers the user's new choice for this session (after it was saved to the API).</summary>
    public static void RememberPreference(HttpContext context, string userName, string? language)
    {
        try { context.Session.SetString(PreferenceSessionPrefix + userName.ToLowerInvariant(), PortalLanguages.Normalize(language) ?? NoPreference); }
        catch (InvalidOperationException) { }
    }

    /// <summary>The institution default ("en", "ur", "ar" or "auto"), cached for two minutes.</summary>
    public static async Task<string> InstitutionDefaultAsync(HttpContext context)
    {
        var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
        if (cache.TryGetValue(DefaultsCacheKey, out string? cached) && cached is not null) return cached;
        string value;
        try
        {
            var api = context.RequestServices.GetRequiredService<IEduApiClient>();
            var raw = await api.GetDefaultLanguageAsync(context.RequestAborted);
            value = raw == PortalLanguages.Auto ? PortalLanguages.Auto : PortalLanguages.Normalize(raw) ?? PortalLanguages.English;
            cache.Set(DefaultsCacheKey, value, TimeSpan.FromMinutes(2));
        }
        catch
        {
            value = PortalLanguages.English;
            cache.Set(DefaultsCacheKey, value, TimeSpan.FromSeconds(30));
        }
        return value;
    }

    public static void ForgetInstitutionDefault(HttpContext context)
        => context.RequestServices.GetRequiredService<IMemoryCache>().Remove(DefaultsCacheKey);
}

/// <summary>The request-culture provider used by <c>UseRequestLocalization</c>.</summary>
public sealed class PortalRequestCultureProvider : RequestCultureProvider
{
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var lang = await PortalLanguageResolver.ResolveAsync(httpContext);
        if (lang == PortalLanguages.English) return null; // the default request culture (the server's English)
        var name = PortalCultures.For(lang).Name;
        return new ProviderCultureResult(name, name);
    }
}
