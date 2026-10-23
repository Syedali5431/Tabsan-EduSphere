using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.ViewComponents;

/// <summary>
/// Plan M7 — when a page was opened from a course module (<c>?moduleItem=…</c>), shows the module's previous/next items
/// and, for "mark as done" items, the button. Renders nothing otherwise.
/// </summary>
public sealed class ModuleNavViewComponent : ViewComponent
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly IEduApiClient _api;

    public ModuleNavViewComponent(IEduApiClient api) => _api = api;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!Guid.TryParse(HttpContext.Request.Query["moduleItem"], out var itemId) || !_api.IsConnected()) return Content(string.Empty);
        try
        {
            var raw = await _api.ModuleSendAsync(HttpMethod.Get, $"module-items/{itemId}/nav", null, HttpContext.RequestAborted);
            if (!raw.Ok) return Content(string.Empty);
            var nav = JsonSerializer.Deserialize<ModuleItemNavResponse>(raw.Body, Web)!;
            return View(new ModuleNavModel
            {
                Nav = nav,
                ReturnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString,
                IsStudent = _api.GetSessionIdentity()?.IsStudent == true
            });
        }
        catch (Exception)
        {
            return Content(string.Empty);
        }
    }
}
