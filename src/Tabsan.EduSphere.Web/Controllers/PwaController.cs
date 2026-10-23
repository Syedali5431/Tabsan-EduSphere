using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M4.1 — the installable app's manifest. Name and logo come from the institution's portal branding
/// (each customer deployment has its own); without a session the last branding seen in this browser session,
/// or the EduSphere defaults, are used.
/// </summary>
public sealed class PwaController : Controller
{
    private readonly IEduApiClient _api;

    public PwaController(IEduApiClient api) => _api = api;

    [HttpGet("/manifest.webmanifest")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Manifest(CancellationToken ct)
    {
        var branding = await BrandingAsync(ct);
        var name = string.IsNullOrWhiteSpace(branding.UniversityName) ? "Tabsan EduSphere" : branding.UniversityName.Trim();
        var shortName = ShortName(name);

        var icons = new List<object>();
        if (!string.IsNullOrWhiteSpace(branding.LogoImage) && Uri.TryCreate(branding.LogoImage, UriKind.RelativeOrAbsolute, out _))
        {
            // The institution's own logo first; browsers that need an exact size fall back to the sized icons below.
            icons.Add(new { src = branding.LogoImage, sizes = "any", purpose = "any" });
        }
        icons.Add(new { src = Url.Content("~/images/pwa/icon-192.png"), sizes = "192x192", type = "image/png", purpose = "any" });
        icons.Add(new { src = Url.Content("~/images/pwa/icon-512.png"), sizes = "512x512", type = "image/png", purpose = "any" });
        icons.Add(new { src = Url.Content("~/images/pwa/maskable-512.png"), sizes = "512x512", type = "image/png", purpose = "maskable" });

        var shortcutIcon = new[] { new { src = Url.Content("~/images/pwa/shortcut-96.png"), sizes = "96x96", type = "image/png" } };
        var manifest = new
        {
            id = "/?app=edusphere",
            name,
            short_name = shortName,
            description = $"{name} — {(string.IsNullOrWhiteSpace(branding.PortalSubtitle) ? "Campus Portal" : branding.PortalSubtitle)}",
            start_url = "/Portal/Dashboard?source=pwa",
            scope = "/",
            display = "standalone",
            orientation = "any",
            background_color = "#ffffff",
            theme_color = "#0d3b66",
            lang = "en",
            categories = new[] { "education", "productivity" },
            icons,
            shortcuts = new object[]
            {
                new { name = "Timetable", url = "/Portal/TimetableStudent?source=pwa", icons = shortcutIcon },
                new { name = "Assignments", url = "/Portal/Assignments?source=pwa", icons = shortcutIcon },
                new { name = "Notifications", url = "/Portal/Notifications?source=pwa", icons = shortcutIcon }
            }
        };
        return Content(JsonSerializer.Serialize(manifest), "application/manifest+json");
    }

    /// <summary>
    /// A short name for the home screen (≤ 12 characters): the name itself when it fits, initials for names of
    /// three or more words ("University of Lahore Engineering" → "ULE"), otherwise the first word.
    /// </summary>
    public static string ShortName(string name)
    {
        if (name.Length <= 12) return name;
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 3)
        {
            var initials = string.Concat(words
                .Where(w => char.IsLetter(w[0]) && !new[] { "of", "and", "the", "for" }.Contains(w.ToLowerInvariant()))
                .Select(w => char.ToUpperInvariant(w[0])));
            if (initials.Length is >= 2 and <= 12) return initials;
        }
        return words[0].Length <= 12 ? words[0] : words[0][..12];
    }

    private async Task<PortalBrandingWebModel> BrandingAsync(CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { return await _api.GetPortalBrandingAsync(ct); }
            catch (Exception) { /* fall back to the cached copy */ }
        }
        var cached = HttpContext.Session.GetString("PortalBrandingCache");
        if (!string.IsNullOrWhiteSpace(cached))
        {
            try { return JsonSerializer.Deserialize<PortalBrandingWebModel>(cached) ?? new(); }
            catch (JsonException) { }
        }
        return new PortalBrandingWebModel();
    }
}
