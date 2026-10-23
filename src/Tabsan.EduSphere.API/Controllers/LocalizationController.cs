using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Localization;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M11 — portal languages: the list, the institution default (readable before signing in, so the login page can
/// use it) and each user's own choice.
/// </summary>
[ApiController]
[Route("api/v1/localization")]
[Authorize]
public class LocalizationController : ControllerBase
{
    private readonly IPortalLanguageService _languages;
    private readonly IUserRepository _users;
    private readonly IAuditService _audit;

    public LocalizationController(IPortalLanguageService languages, IUserRepository users, IAuditService audit)
    {
        _languages = languages;
        _users = users;
        _audit = audit;
    }

    public sealed record LanguageRequest(string? Language);

    /// <summary>The languages and the institution default ("auto" = the browser's language). No sign-in needed.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(new
        {
            defaultLanguage = await _languages.GetDefaultAsync(ct),
            languages = PortalLanguages.All.Select(l => new
            {
                code = l,
                name = PortalLanguages.EnglishName(l),
                nativeName = PortalLanguages.NativeName(l),
                rtl = PortalLanguages.IsRtl(l)
            })
        });

    /// <summary>Sets the institution default language. SuperAdmin only.</summary>
    [HttpPut("default")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetDefault([FromBody] LanguageRequest request, CancellationToken ct)
    {
        var value = request.Language?.Trim().ToLowerInvariant();
        if (value != PortalLanguages.Auto && !PortalLanguages.IsSupported(value))
            return BadRequest(new { message = "Choose English, Urdu, Arabic or the browser's language." });

        var before = await _languages.GetDefaultAsync(ct);
        await _languages.SetDefaultAsync(value!, ct);
        await _audit.LogAsync(new AuditLog("SetDefaultLanguage", "PortalSetting", PortalLanguageService.SettingKey, CurrentUserId(),
            $"{{\"language\":\"{before}\"}}", $"{{\"language\":\"{value}\"}}", null), ct);
        return Ok(new { defaultLanguage = value });
    }

    /// <summary>The signed-in user's own language (null = the institution default).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(CurrentUserId(), ct);
        if (user is null) return NotFound(new { message = "Current user was not found." });
        return Ok(new { language = user.PreferredLanguage, defaultLanguage = await _languages.GetDefaultAsync(ct) });
    }

    /// <summary>Saves (or clears with null) the signed-in user's language.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> SetMine([FromBody] LanguageRequest request, CancellationToken ct)
    {
        var language = PortalLanguages.Normalize(request.Language);
        if (language is null && !string.IsNullOrWhiteSpace(request.Language))
            return BadRequest(new { message = "Choose English, Urdu or Arabic." });

        var user = await _users.GetByIdAsync(CurrentUserId(), ct);
        if (user is null) return NotFound(new { message = "Current user was not found." });
        user.SetPreferredLanguage(language);
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
        return Ok(new { language });
    }

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
