using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Localization;
using Tabsan.EduSphere.Web.Localization;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>Plan M11 — the language switcher (top bar and login page).</summary>
public class LanguageController : Controller
{
    private readonly IEduApiClient _api;

    public LanguageController(IEduApiClient api) => _api = api;

    /// <summary>
    /// Switches the portal language: remembered in a cookie (also before signing in) and, when signed in, saved as the user's
    /// own language so it follows them to other devices. An empty <paramref name="lang"/> goes back to the institution default.
    /// </summary>
    [HttpPost("/Language/Set")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Set(string? lang, string? returnUrl, CancellationToken ct)
    {
        var language = PortalLanguages.Normalize(lang);
        if (language is null && !string.IsNullOrWhiteSpace(lang))
            return BadRequest();

        if (language is null)
            Response.Cookies.Delete(PortalLanguageResolver.CookieName);
        else
            Response.Cookies.Append(PortalLanguageResolver.CookieName, language, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        if (_api.IsConnected() && _api.GetSessionIdentity()?.UserName is { Length: > 0 } user)
        {
            try { await _api.SetMyLanguageAsync(language, ct); }
            catch { /* this session still switches; the saved choice is updated next time the user switches */ }
            PortalLanguageResolver.RememberPreference(HttpContext, user, language);
        }

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
