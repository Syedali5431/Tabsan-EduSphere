using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lti;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.ViewComponents;

/// <summary>
/// Plan M5 — "External tools" card for a course: items students launch, and (for staff) adding items, choosing content
/// from a tool (Deep Linking) and hiding items. Renders nothing when the LTI module is off or the user has no access.
/// </summary>
public sealed class ExternalToolsViewComponent : ViewComponent
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly IEduApiClient _api;

    public ExternalToolsViewComponent(IEduApiClient api) => _api = api;

    public async Task<IViewComponentResult> InvokeAsync(Guid offeringId, bool manage)
    {
        if (offeringId == Guid.Empty || !_api.IsConnected()) return Content(string.Empty);
        try
        {
            var links = await _api.LtiSendAsync(HttpMethod.Get, $"offerings/{offeringId}/links", null, HttpContext.RequestAborted);
            if (!links.Ok) return Content(string.Empty);
            var model = new ExternalToolsCardModel
            {
                OfferingId = offeringId,
                Manage = manage,
                ReturnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString,
                Links = JsonSerializer.Deserialize<List<LtiLinkResponse>>(links.Body, Web) ?? []
            };
            if (manage)
            {
                var tools = await _api.LtiSendAsync(HttpMethod.Get, $"offerings/{offeringId}/tools", null, HttpContext.RequestAborted);
                if (tools.Ok) model.Tools = JsonSerializer.Deserialize<List<LtiCourseToolResponse>>(tools.Body, Web) ?? [];
            }
            if (!manage && model.Links.Count == 0) return Content(string.Empty);
            return View(model);
        }
        catch (Exception)
        {
            return Content(string.Empty);
        }
    }
}
