using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lms.Scorm;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.ViewComponents;

/// <summary>
/// Plan M13 — "SCORM packages" card of a course: students see the published packages with their status; on the content
/// page teachers upload, edit, preview and remove packages. Renders nothing for students when the course has none.
/// </summary>
public sealed class ScormPackagesViewComponent : ViewComponent
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly IEduApiClient _api;
    private readonly IConfiguration _config;

    public ScormPackagesViewComponent(IEduApiClient api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid offeringId, bool manage)
    {
        if (offeringId == Guid.Empty || !_api.IsConnected()) return Content(string.Empty);
        var identity = _api.GetSessionIdentity();
        if (!manage && identity?.IsStudent != true) return Content(string.Empty);
        try
        {
            var raw = await _api.ScormSendAsync(HttpMethod.Get, manage ? $"offerings/{offeringId}/packages" : $"offerings/{offeringId}/my-packages", null,
                HttpContext.RequestAborted);
            if (!raw.Ok) return Content(string.Empty);
            var model = new ScormCardModel
            {
                OfferingId = offeringId,
                Manage = manage,
                Packages = JsonSerializer.Deserialize<List<ScormPackageResponse>>(raw.Body, Web) ?? [],
                MaxUploadMb = int.TryParse(_config["Scorm:MaxPackageMb"], out var mb) ? mb : 500
            };
            if (!manage && model.Packages.Count == 0) return Content(string.Empty);
            return View(model);
        }
        catch (Exception)
        {
            return Content(string.Empty);
        }
    }
}
