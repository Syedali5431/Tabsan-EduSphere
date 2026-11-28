using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Course copy: bring another course's content (last term's, or a template's) into this course, choosing what to copy,
/// moving its dates and deciding whether students see it straight away. Admins share courses as templates here too.
/// The API decides who may copy from and into which course.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions CourseCopyJson = new(JsonSerializerDefaults.Web);

    private static T? ReadCourseCopy<T>(ApiRaw raw) => raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonSerializer.Deserialize<T>(raw.Body, CourseCopyJson) : default;

    [HttpGet]
    public async Task<IActionResult> CourseCopy(Guid offeringId, Guid? sourceId, CancellationToken ct)
    {
        ViewData["Title"] = "Copy course content";
        var model = new CourseCopyPageModel
        {
            IsConnected = _api.IsConnected(), OfferingId = offeringId, SourceId = sourceId,
            Message = TempData["PortalMessage"]?.ToString(),
            Result = TempData["CourseCopyResult"] is string done ? JsonSerializer.Deserialize<CourseCopyResultDto>(done, CourseCopyJson) : null,
            CanShareTemplate = _api.GetSessionIdentity()?.Roles.Any(r => r is "Admin" or "SuperAdmin") == true
        };
        if (!model.IsConnected || offeringId == Guid.Empty) return View(model);
        try
        {
            var sources = await _api.CourseCopySendAsync(HttpMethod.Get, $"offerings/{offeringId}/sources", null, ct);
            if (!sources.Ok) { model.Message ??= sources.Message ?? "The courses to copy from could not be loaded."; return View(model); }
            var list = ReadCourseCopy<CourseCopySourcesDto>(sources);
            model.Target = list?.Target;
            model.Sources = list?.Sources.ToList() ?? [];

            if (sourceId is { } source)
            {
                var preview = await _api.CourseCopySendAsync(HttpMethod.Get, $"offerings/{offeringId}/preview?sourceId={source}", null, ct);
                if (preview.Ok) model.Preview = ReadCourseCopy<CourseCopyPreviewDto>(preview);
                else model.Message ??= preview.Message ?? "The course's content could not be loaded.";
            }
            model.History = ReadCourseCopy<List<CourseCopyRunDto>>(await _api.CourseCopySendAsync(HttpMethod.Get, $"offerings/{offeringId}/history", null, ct)) ?? [];
            if (model.CanShareTemplate)
                model.Template = ReadCourseCopy<CourseTemplateDto>(await _api.CourseCopySendAsync(HttpMethod.Get, $"offerings/{offeringId}/template", null, ct));
        }
        catch (Exception ex) { model.Message ??= ex.Message; }
        return View(model);
    }

    /// <param name="items">Ticked items as "part:id" (parts and ids of <see cref="CourseCopyParts"/>).</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyCourseContent(Guid offeringId, Guid sourceId, List<string>? items, int shiftDays, bool keepPublished, CancellationToken ct)
    {
        var parts = new HashSet<string>(StringComparer.Ordinal);
        var ids = new List<Guid>();
        foreach (var value in items ?? [])
        {
            var cut = value.IndexOf(':');
            if (cut <= 0 || !Guid.TryParse(value[(cut + 1)..], out var id)) continue;
            parts.Add(value[..cut]);
            ids.Add(id);
        }
        if (ids.Count == 0)
        {
            TempData["PortalMessage"] = "Choose something to copy.";
            return RedirectToAction(nameof(CourseCopy), new { offeringId, sourceId });
        }

        var raw = await _api.CourseCopySendAsync(HttpMethod.Post, $"offerings/{offeringId}/copy",
            new CourseCopyRequest(sourceId, parts.ToList(), ids, shiftDays, keepPublished), ct);
        if (raw.Ok)
        {
            TempData["CourseCopyResult"] = raw.Body;
            return RedirectToAction(nameof(CourseCopy), new { offeringId });
        }
        TempData["PortalMessage"] = raw.Message ?? "The content could not be copied.";
        return RedirectToAction(nameof(CourseCopy), new { offeringId, sourceId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCourseTemplate(Guid offeringId, string? name, string? description, string? scope, CancellationToken ct)
    {
        var raw = await _api.CourseCopySendAsync(HttpMethod.Put, $"offerings/{offeringId}/template", new SaveCourseTemplateRequest(name, description, scope), ct);
        TempData["PortalMessage"] = raw.Ok ? "The course is shared as a template." : raw.Message ?? "The template could not be saved.";
        return RedirectToAction(nameof(CourseCopy), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCourseTemplate(Guid offeringId, CancellationToken ct)
    {
        var raw = await _api.CourseCopySendAsync(HttpMethod.Delete, $"offerings/{offeringId}/template", null, ct);
        TempData["PortalMessage"] = raw.Ok ? "The course is no longer shared as a template." : raw.Message ?? "The template could not be removed.";
        return RedirectToAction(nameof(CourseCopy), new { offeringId });
    }
}
