using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
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

    [HttpGet("/setup/export")]
    public IActionResult ExportProfile()
    {
        var json = _databaseSetupService.ExportProfileJson();
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", "edusphere-db-setup-profile.json");
    }

    [HttpPost("/setup/import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportProfile([FromForm] IFormFile? profileFile, CancellationToken ct)
    {
        if (profileFile is null || profileFile.Length == 0)
        {
            var emptyModel = _databaseSetupService.BuildViewModel();
            emptyModel.StatusMessage = "Please select a valid JSON profile file.";
            emptyModel.RequiresSetup = true;
            return View("~/Views/Setup/Index.cshtml", emptyModel);
        }

        try
        {
            await using var stream = profileFile.OpenReadStream();
            var profile = await JsonSerializer.DeserializeAsync<DatabaseSetupProfileModel>(stream, cancellationToken: ct);
            if (profile is null)
            {
                var invalidModel = _databaseSetupService.BuildViewModel();
                invalidModel.StatusMessage = "Could not read setup profile JSON.";
                invalidModel.RequiresSetup = true;
                return View("~/Views/Setup/Index.cshtml", invalidModel);
            }

            var save = await _databaseSetupService.ImportProfileAsync(profile, ct);
            if (!save.Success)
            {
                var failedModel = _databaseSetupService.BuildViewModel();
                failedModel.StatusMessage = save.Message;
                failedModel.RequiresSetup = true;
                return View("~/Views/Setup/Index.cshtml", failedModel);
            }

            TempData["SetupMessage"] = "Setup profile imported and applied successfully.";
            return Redirect("/setup");
        }
        catch (Exception ex)
        {
            var errorModel = _databaseSetupService.BuildViewModel();
            errorModel.StatusMessage = $"Import failed: {ex.Message}";
            errorModel.RequiresSetup = true;
            return View("~/Views/Setup/Index.cshtml", errorModel);
        }
    }

    [HttpGet("/setup/docker-env")]
    public IActionResult DownloadDockerEnvTemplate()
    {
        var content = _databaseSetupService.BuildDockerEnvTemplate();
        var bytes = Encoding.UTF8.GetBytes(content);
        return File(bytes, "text/plain", "edusphere-docker.env");
    }
}
