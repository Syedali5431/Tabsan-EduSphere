using Microsoft.AspNetCore.Mvc;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M4.3 — relays for the "notifications on this device" card. The API works on the signed-in
/// user's own subscriptions only; these actions keep the token on the server.
/// </summary>
public partial class PortalController
{
    [HttpGet]
    public async Task<IActionResult> PushSummary(CancellationToken ct)
    {
        if (!_api.IsConnected()) return Unauthorized();
        return Relay(await _api.PushSendAsync(HttpMethod.Get, string.Empty, null, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PushSubscribe(CancellationToken ct)
        => await RelayPushJsonAsync(HttpMethod.Post, "subscriptions", ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PushUnsubscribe(string endpoint, CancellationToken ct)
    {
        if (!_api.IsConnected()) return Unauthorized();
        return Relay(await _api.PushSendAsync(HttpMethod.Delete, "subscriptions?endpoint=" + Uri.EscapeDataString(endpoint ?? string.Empty), null, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PushPreferences(CancellationToken ct)
        => await RelayPushJsonAsync(HttpMethod.Put, "preferences", ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PushTest(CancellationToken ct)
    {
        if (!_api.IsConnected()) return Unauthorized();
        return Relay(await _api.PushSendAsync(HttpMethod.Post, "test", "{}", ct));
    }

    private async Task<IActionResult> RelayPushJsonAsync(HttpMethod method, string path, CancellationToken ct)
    {
        if (!_api.IsConnected()) return Unauthorized();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        try { using var _ = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body); }
        catch (System.Text.Json.JsonException) { return BadRequest(new { message = "The request was not valid JSON." }); }
        return Relay(await _api.PushSendAsync(method, path, string.IsNullOrWhiteSpace(body) ? "{}" : body, ct));
    }
}
