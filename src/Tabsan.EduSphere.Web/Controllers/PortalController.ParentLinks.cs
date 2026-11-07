using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Parent Links (2026-10-30) — Admins and Super Admins find a Parent-role account and School students by name, then link,
/// pause, resume and remove links. The API enforces the rules (Parent role, School student, the Admin's departments).
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions ParentLinkJson = new(JsonSerializerDefaults.Web);
    private const int ParentLinkSearchSize = 25;

    private IActionResult? RequireParentLinkAdmin()
    {
        var identity = _api.GetSessionIdentity();
        return identity is not null && (identity.IsAdmin || identity.IsSuperAdmin)
            ? null : AccessDenied("Parent links are managed by Admins and Super Admins.");
    }

    [HttpGet]
    public async Task<IActionResult> ParentLinks(string? parentSearch, Guid? parentId, string? studentSearch, CancellationToken ct)
    {
        ViewData["Title"] = "Parent Links";
        if (RequireParentLinkAdmin() is { } denied) return denied;
        var model = new ParentLinksPageModel
        {
            IsConnected = _api.IsConnected(),
            ParentSearch = ParentLinkRelationships.Normalize(parentSearch),
            StudentSearch = ParentLinkRelationships.Normalize(studentSearch),
            Message = TempData["ParentLinksMessage"]?.ToString(),
            MessageIsError = TempData["ParentLinksError"] is true
        };
        if (!model.IsConnected) return View(model);

        var parents = await _api.ParentPortalSendAsync(HttpMethod.Get, $"parents?search={Uri.EscapeDataString(model.ParentSearch ?? "")}&take={ParentLinkSearchSize}", null, ct);
        if (parents.Ok) model.Parents = JsonSerializer.Deserialize<List<ParentAccountLookupDto>>(parents.Body, ParentLinkJson) ?? [];
        else SetParentLinkError(model, parents, "Parent accounts could not be loaded.");

        if (parentId is not { } id) return View(model);

        var parent = await _api.ParentPortalSendAsync(HttpMethod.Get, $"parents/{id}", null, ct);
        if (!parent.Ok)
        {
            SetParentLinkError(model, parent, "The parent account could not be found.");
            return View(model);
        }
        model.Parent = JsonSerializer.Deserialize<ParentAccountLookupDto>(parent.Body, ParentLinkJson);

        var links = await _api.ParentPortalSendAsync(HttpMethod.Get, $"parents/{id}/links", null, ct);
        if (links.Ok) model.Links = JsonSerializer.Deserialize<List<ParentStudentLinkDetailDto>>(links.Body, ParentLinkJson) ?? [];
        else SetParentLinkError(model, links, "The parent's links could not be loaded.");

        if (model.StudentSearch is { } term)
        {
            model.StudentSearchRan = true;
            var students = await _api.ParentPortalSendAsync(HttpMethod.Get, $"students?search={Uri.EscapeDataString(term)}&take={ParentLinkSearchSize}", null, ct);
            if (students.Ok) model.Students = JsonSerializer.Deserialize<List<SchoolStudentLookupDto>>(students.Body, ParentLinkJson) ?? [];
            else SetParentLinkError(model, students, "Students could not be searched.");
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkParentStudent(Guid parentId, Guid studentProfileId, string? relationship, string? parentSearch, CancellationToken ct)
    {
        if (RequireParentLinkAdmin() is { } denied) return denied;
        var result = await SaveParentLinkAsync(parentId, studentProfileId, relationship, active: true, ct);
        FlashParentLink(result, "Student linked. The parent sees this child in the app the next time it refreshes.", "The student could not be linked.");
        return RedirectToAction(nameof(ParentLinks), new { parentId, parentSearch });
    }

    /// <summary>Saves the relationship, or pauses/resumes the link (<paramref name="change"/> = relationship, pause or resume).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateParentLink(Guid parentId, Guid studentProfileId, string? relationship, bool active, string? change, string? parentSearch, CancellationToken ct)
    {
        if (RequireParentLinkAdmin() is { } denied) return denied;
        var result = await SaveParentLinkAsync(parentId, studentProfileId, relationship, active, ct);
        var done = change switch
        {
            "pause" => "Link paused. The parent no longer sees this child until the link is resumed.",
            "resume" => "Link resumed.",
            _ => "Relationship saved."
        };
        FlashParentLink(result, done, "The link could not be changed.");
        return RedirectToAction(nameof(ParentLinks), new { parentId, parentSearch });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveParentLink(Guid parentId, Guid studentProfileId, string? parentSearch, CancellationToken ct)
    {
        if (RequireParentLinkAdmin() is { } denied) return denied;
        var result = await _api.ParentPortalSendAsync(HttpMethod.Delete, $"links/{parentId}/{studentProfileId}?remove=true", null, ct);
        FlashParentLink(result, "Link removed.", "The link could not be removed.");
        return RedirectToAction(nameof(ParentLinks), new { parentId, parentSearch });
    }

    private Task<ApiRaw> SaveParentLinkAsync(Guid parentId, Guid studentProfileId, string? relationship, bool active, CancellationToken ct)
        => _api.ParentPortalSendAsync(HttpMethod.Put, "links", JsonSerializer.Serialize(
            new UpsertParentStudentLinkRequest(parentId, studentProfileId, ParentLinkRelationships.Normalize(relationship), active), ParentLinkJson), ct);

    private void FlashParentLink(ApiRaw result, string done, string failed)
    {
        TempData["ParentLinksMessage"] = result.Ok ? done : ParentLinkFailure(result, failed);
        TempData["ParentLinksError"] = !result.Ok;
    }

    private static void SetParentLinkError(ParentLinksPageModel model, ApiRaw result, string fallback)
    {
        model.Message ??= ParentLinkFailure(result, fallback);
        model.MessageIsError = true;
    }

    private static string ParentLinkFailure(ApiRaw result, string fallback)
        => result.Status == 403 ? "You can only manage links of students in your assigned departments." : result.Message ?? fallback;
}
