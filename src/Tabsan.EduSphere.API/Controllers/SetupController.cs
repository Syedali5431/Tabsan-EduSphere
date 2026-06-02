using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Models.Setup;
using Tabsan.EduSphere.API.Services.Setup;

namespace Tabsan.EduSphere.API.Controllers;

[AllowAnonymous]
public sealed class SetupController : Controller
{
    private readonly IDatabaseSetupService _databaseSetupService;

    public SetupController(IDatabaseSetupService databaseSetupService)
    {
        _databaseSetupService = databaseSetupService;
    }

    [HttpGet("/setup")]
    public async Task<IActionResult> Index([FromQuery] string? returnUrl, CancellationToken ct)
    {
        await _databaseSetupService.ValidateCurrentConnectionAsync(force: true, ct);
        var model = _databaseSetupService.BuildViewModel(returnUrl);
        return View("~/Views/Setup/Index.cshtml", model);
    }

    [HttpPost("/setup/test-connection")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestConnection([FromForm] DatabaseSetupViewModel model, CancellationToken ct)
    {
        var result = await _databaseSetupService.TestConnectionAsync(model, ct);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost("/setup/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromForm] DatabaseSetupViewModel model, CancellationToken ct)
    {
        var save = await _databaseSetupService.SaveAsync(model, ct);
        if (!save.Success)
        {
            model.StatusMessage = save.Message;
            model.RequiresSetup = true;
            return View("~/Views/Setup/Index.cshtml", model);
        }

        TempData["SetupMessage"] = save.Message;
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return Redirect("/");
    }
}
