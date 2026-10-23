using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>Plan M14.3 — personal access tokens for the public REST API (create, shown once; revoke; administrators see all).</summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions TokenJson = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<IActionResult> ApiTokens(CancellationToken ct)
    {
        ViewData["Title"] = "API Tokens";
        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsAdmin || identity.IsSuperAdmin)) return AccessDenied("API tokens are available to Admin and Super Admin.");
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var model = new ApiTokensPageModel
        {
            ApiBaseUrl = (config["PublicApi:BaseUrl"] ?? config["EduApi:BaseUrl"] ?? "").TrimEnd('/'),
            Message = TempData["ErrorMessage"]?.ToString()
        };
        if (TempData["ApiTokenCreated"] is string created) model.Created = JsonSerializer.Deserialize<ApiTokenCreated>(created, TokenJson);
        var mine = await _api.ApiTokensSendAsync(HttpMethod.Get, "", null, ct);
        if (mine.Ok) model.Mine = JsonSerializer.Deserialize<List<ApiTokenResponse>>(mine.Body, TokenJson) ?? [];
        else model.Message ??= mine.Message ?? "Tokens could not be loaded.";
        var all = await _api.ApiTokensSendAsync(HttpMethod.Get, "all", null, ct);
        if (all.Ok) model.All = JsonSerializer.Deserialize<List<ApiTokenResponse>>(all.Body, TokenJson) ?? [];
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateApiToken(string? name, int? expiresInDays, bool readOnly, CancellationToken ct)
    {
        var request = new CreateApiTokenRequest(name ?? "", expiresInDays, readOnly);
        var result = await _api.ApiTokensSendAsync(HttpMethod.Post, "", JsonSerializer.Serialize(request, TokenJson), ct);
        if (result.Ok) TempData["ApiTokenCreated"] = result.Body;
        else TempData["ErrorMessage"] = result.Message ?? "The token could not be created.";
        return RedirectToAction(nameof(ApiTokens));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeApiToken(Guid id, CancellationToken ct)
    {
        var result = await _api.ApiTokensSendAsync(HttpMethod.Delete, id.ToString(), null, ct);
        TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Ok ? "Token revoked." : result.Message ?? "The token could not be revoked.";
        return RedirectToAction(nameof(ApiTokens));
    }
}
