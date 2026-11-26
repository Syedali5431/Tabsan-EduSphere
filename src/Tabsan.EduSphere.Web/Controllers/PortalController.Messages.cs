using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Messaging;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Messages: the inbox, a conversation with its reply box, and writing a new message to the people the user's role connects
/// them with. The API decides who may write to whom and who may read a conversation.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions MessagesJson = new(JsonSerializerDefaults.Web);

    private static T? ReadMessages<T>(ApiRaw raw) => raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonSerializer.Deserialize<T>(raw.Body, MessagesJson) : default;

    [HttpGet]
    public async Task<IActionResult> Messages(bool archived, CancellationToken ct)
    {
        ViewData["Title"] = "Messages";
        var model = new MessagesPageModel { IsConnected = _api.IsConnected(), Archived = archived, Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.MessagesSendAsync(HttpMethod.Get, $"conversations?archived={(archived ? "true" : "false")}", null, ct);
            if (raw.Ok) model.Conversations = ReadMessages<List<ConversationSummaryDto>>(raw) ?? [];
            else model.Message ??= raw.Message ?? "The messages could not be loaded.";
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MessageThread(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Messages";
        var model = new MessageThreadPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.MessagesSendAsync(HttpMethod.Get, $"conversations/{id}", null, ct);
            if (raw.Status == 404) { TempData["PortalMessage"] = "Conversation not found."; return RedirectToAction(nameof(Messages)); }
            if (raw.Ok) model.Conversation = ReadMessages<ConversationDto>(raw);
            else model.Message ??= raw.Message ?? "The conversation could not be loaded.";
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyMessage(Guid id, string? body, CancellationToken ct)
    {
        var raw = await _api.MessagesSendAsync(HttpMethod.Post, $"conversations/{id}/messages", new ReplyRequest(body ?? ""), ct);
        if (!raw.Ok) TempData["PortalMessage"] = raw.Message ?? "The message could not be sent.";
        return RedirectToAction(nameof(MessageThread), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveConversation(Guid id, bool archive, CancellationToken ct)
    {
        var raw = await _api.MessagesSendAsync(HttpMethod.Post, $"conversations/{id}/{(archive ? "archive" : "unarchive")}", null, ct);
        TempData["PortalMessage"] = raw.Ok
            ? archive ? "The conversation is archived. A new message brings it back to your inbox." : "The conversation is back in your inbox."
            : raw.Message ?? "The conversation could not be changed.";
        return RedirectToAction(nameof(Messages), new { archived = !archive });
    }

    /// <summary>?search= narrows the people; ?to= ticks someone (a link from another page).</summary>
    [HttpGet]
    public async Task<IActionResult> NewMessage(string? search, Guid? to, CancellationToken ct)
    {
        ViewData["Title"] = "New message";
        var model = new NewMessagePageModel { IsConnected = _api.IsConnected(), Search = search, Message = TempData["PortalMessage"]?.ToString() };
        if (to is { } person) model.Selected.Add(person);
        if (!model.IsConnected) return View(model);
        await LoadContactsAsync(model, ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(List<Guid>? recipientIds, string? subject, string? body, string? search, CancellationToken ct)
    {
        var recipients = recipientIds ?? [];
        var raw = await _api.MessagesSendAsync(HttpMethod.Post, "conversations", new StartConversationRequest(recipients, subject ?? "", body ?? ""), ct);
        if (raw.Ok && ReadMessages<StartConversationResult>(raw) is { } sent)
        {
            if (sent.ConversationIds.Count == 1) return RedirectToAction(nameof(MessageThread), new { id = sent.ConversationIds[0] });
            TempData["PortalMessage"] = $"Sent: {sent.ConversationIds.Count} conversations, one for each student or parent.";
            return RedirectToAction(nameof(Messages));
        }
        // Keep what was written so nothing is lost.
        var model = new NewMessagePageModel
        {
            IsConnected = true, Search = search, Subject = subject, Body = body, Selected = recipients.ToHashSet(),
            Message = raw.Message ?? "The message could not be sent."
        };
        ViewData["Title"] = "New message";
        await LoadContactsAsync(model, ct);
        return View(nameof(NewMessage), model);
    }

    private async Task LoadContactsAsync(NewMessagePageModel model, CancellationToken ct)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(model.Search) ? "contacts" : $"contacts?search={Uri.EscapeDataString(model.Search.Trim())}";
            var raw = await _api.MessagesSendAsync(HttpMethod.Get, path, null, ct);
            if (raw.Ok) model.Contacts = ReadMessages<List<MessageContactDto>>(raw) ?? [];
            else model.Message ??= raw.Message ?? "The list of people could not be loaded.";
            // Someone ticked earlier stays in the list even when the search no longer finds them.
            var missing = model.Selected.Where(id => model.Contacts.All(c => c.UserId != id)).ToList();
            if (missing.Count > 0 && !string.IsNullOrWhiteSpace(model.Search))
            {
                var all = ReadMessages<List<MessageContactDto>>(await _api.MessagesSendAsync(HttpMethod.Get, "contacts", null, ct)) ?? [];
                model.Contacts.InsertRange(0, all.Where(c => missing.Contains(c.UserId)));
            }
        }
        catch (Exception ex) { model.Message ??= ex.Message; }
    }
}
