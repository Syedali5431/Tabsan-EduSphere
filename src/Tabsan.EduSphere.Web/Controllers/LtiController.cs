using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lti;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M5 — the platform's public LTI endpoints live on the portal address (the platform issuer):
/// <list type="bullet">
///   <item><c>/lti/launch/{id}</c> and <c>/lti/deep-link/{offering}</c>: a signed-in user starts a launch; the browser posts to the tool.</item>
///   <item><c>/lti/authorize</c>: the tool's OIDC authentication request; answered with an auto-posted id_token.
///         It works without the portal cookie (tools post cross-site or run in iframes) — the single-use launch is the proof.</item>
///   <item><c>/lti/deep-link-return</c>: the tool's signed content selection.</item>
///   <item><c>/lti/jwks</c>, <c>/lti/token</c>, <c>/lti/ags/…</c>, <c>/lti/nrps/…</c>: relayed to the API unchanged; tools
///         authenticate with LTI tokens there, never with a portal session.</item>
/// </list>
/// </summary>
[Route("lti")]
public sealed class LtiController : Controller
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly IEduApiClient _api;
    private readonly IHttpClientFactory _http;
    private readonly string _apiBase;

    public LtiController(IEduApiClient api, IHttpClientFactory http, IConfiguration config)
    {
        _api = api;
        _http = http;
        _apiBase = (config["EduApi:BaseUrl"] ?? throw new InvalidOperationException("EduApi:BaseUrl is required.")).TrimEnd('/') + "/";
    }

    // ── Launch (signed-in user) ─────────────────────────────────────────────

    [HttpGet("launch/{linkId:guid}")]
    public async Task<IActionResult> Launch(Guid linkId, bool embedded, string? returnUrl, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Index", "Login", new { returnUrl = Request.Path + Request.QueryString });
        var back = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        var result = await _api.LtiSendAsync(HttpMethod.Post, $"links/{linkId}/launch" + (back is null ? "" : "?returnUrl=" + Uri.EscapeDataString(back)), null, ct);
        if (!result.Ok) return ErrorPage(result.Message ?? "This tool could not be opened.");
        var init = JsonSerializer.Deserialize<LtiLoginInitiation>(result.Body, Web)!;

        // An iframe item opens inside the portal; the frame then starts the launch itself.
        if (init.PresentationTarget == "iframe" && !embedded)
        {
            ViewData["Title"] = "External tool";
            return View("Frame", new LtiFramePageModel { FrameUrl = Url.Action(nameof(Launch), new { linkId, embedded = true, returnUrl = back })!, ReturnUrl = back });
        }
        return View("AutoPost", new LtiAutoPostModel { Action = init.Url, Fields = init.Parameters, Title = "Opening the tool…" });
    }

    [HttpPost("deep-link/{offeringId:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeepLink(Guid offeringId, Guid toolId, Guid? moduleId, string? returnUrl, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Index", "Login");
        var body = JsonSerializer.Serialize(new { toolId, moduleId, returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null }, Web);
        var result = await _api.LtiSendAsync(HttpMethod.Post, $"offerings/{offeringId}/deep-linking", body, ct);
        if (!result.Ok) return ErrorPage(result.Message ?? "The tool could not be opened.");
        var init = JsonSerializer.Deserialize<LtiLoginInitiation>(result.Body, Web)!;
        return View("AutoPost", new LtiAutoPostModel { Action = init.Url, Fields = init.Parameters, Title = "Opening the tool…" });
    }

    // ── OIDC authentication request from the tool ───────────────────────────

    [HttpGet("authorize"), HttpPost("authorize"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        string? P(string name) => Request.HasFormContentType && Request.Form.TryGetValue(name, out var f) ? f.ToString()
            : Request.Query.TryGetValue(name, out var q) ? q.ToString() : null;
        var request = new LtiAuthorizationRequest(P("scope"), P("response_type"), P("response_mode"), P("prompt"), P("client_id"),
            P("redirect_uri"), P("login_hint"), P("lti_message_hint"), P("state"), P("nonce"));
        var json = JsonSerializer.Serialize(request, Web);

        // With a portal session the API also checks that it is the launching user.
        var result = _api.IsConnected()
            ? await _api.LtiSendAsync(HttpMethod.Post, "authorize", json, ct)
            : await AnonymousAsync(HttpMethod.Post, "api/v1/lti/authorize", new StringContent(json, Encoding.UTF8, "application/json"), null, ct);
        if (!result.Ok) return ErrorPage(result.Message ?? "The tool's sign-in request was refused.");

        var answer = JsonSerializer.Deserialize<LtiAuthorizationResponse>(result.Body, Web)!;
        var fields = new Dictionary<string, string> { ["id_token"] = answer.IdToken };
        if (answer.State is not null) fields["state"] = answer.State;
        return View("AutoPost", new LtiAutoPostModel { Action = answer.RedirectUri, Fields = fields, Title = "Signing you in to the tool…" });
    }

    // ── Deep Linking response from the tool ─────────────────────────────────

    [HttpPost("deep-link-return"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeepLinkReturn(CancellationToken ct)
    {
        var jwt = Request.HasFormContentType ? Request.Form["JWT"].ToString() : string.Empty;
        if (string.IsNullOrWhiteSpace(jwt)) return ErrorPage("The tool did not send a selection.");
        var result = await AnonymousAsync(HttpMethod.Post, "api/v1/lti/deep-link-return",
            new StringContent(JsonSerializer.Serialize(new { jwt }), Encoding.UTF8, "application/json"), null, ct);
        if (!result.Ok) return ErrorPage(result.Message ?? "The selection could not be added.");
        var done = JsonSerializer.Deserialize<LtiDeepLinkResult>(result.Body, Web)!;
        ViewData["Title"] = "Content added";
        return View("DeepLinkDone", done);
    }

    // ── Service relays (tools call these server-to-server) ──────────────────

    [HttpGet("jwks")]
    public Task<IActionResult> Jwks(CancellationToken ct) => RelayAsync("api/v1/lti/jwks", ct);

    // LTI Dynamic Registration: the tool reads the platform configuration and posts its registration (bearer registration token).
    [HttpGet("openid-configuration")]
    public Task<IActionResult> OpenIdConfiguration(CancellationToken ct) => RelayAsync("api/v1/lti/openid-configuration", ct);

    [HttpPost("register"), IgnoreAntiforgeryToken]
    public Task<IActionResult> Register(CancellationToken ct) => RelayAsync("api/v1/lti/register", ct);

    [HttpPost("token"), IgnoreAntiforgeryToken]
    public Task<IActionResult> Token(CancellationToken ct) => RelayAsync("api/v1/lti/token", ct);

    [AcceptVerbs("GET", "POST", "PUT", "DELETE", Route = "ags/{**rest}"), IgnoreAntiforgeryToken]
    public Task<IActionResult> Ags(string rest, CancellationToken ct) => RelayAsync("api/v1/lti/ags/" + rest, ct);

    [AcceptVerbs("GET", Route = "nrps/{**rest}"), IgnoreAntiforgeryToken]
    public Task<IActionResult> Nrps(string rest, CancellationToken ct) => RelayAsync("api/v1/lti/nrps/" + rest, ct);

    private async Task<IActionResult> RelayAsync(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), new Uri(new Uri(_apiBase), path + Request.QueryString));
        if (Request.HasFormContentType)
        {
            // MVC's form value provider has already read the body (the token request), so send the parsed form on.
            var form = await Request.ReadFormAsync(ct);
            request.Content = new FormUrlEncodedContent(form.SelectMany(f => f.Value.Select(v => new KeyValuePair<string, string>(f.Key, v ?? string.Empty))));
        }
        else if (Request.ContentLength is > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            using var buffer = new MemoryStream();
            await Request.Body.CopyToAsync(buffer, ct);
            if (buffer.Length > 1_000_000) return StatusCode(413);
            request.Content = new ByteArrayContent(buffer.ToArray());
            if (MediaTypeHeaderValue.TryParse(Request.ContentType, out var type)) request.Content.Headers.ContentType = type;
        }
        if (Request.Headers.Authorization.Count > 0) request.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
        if (Request.Headers.Accept.Count > 0) request.Headers.TryAddWithoutValidation("Accept", Request.Headers.Accept.ToString());

        using var response = await _http.CreateClient("EduApi").SendAsync(request, ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (response.Headers.CacheControl is { } cache) Response.Headers.CacheControl = cache.ToString();
        if (response.Headers.WwwAuthenticate.Count > 0) Response.Headers.WWWAuthenticate = response.Headers.WwwAuthenticate.ToString();
        if (response.Headers.TryGetValues("Link", out var link)) Response.Headers.Link = string.Join(", ", link); // AGS/NRPS paging
        return new RawResult((int)response.StatusCode, response.Content.Headers.ContentType?.ToString(), bytes);
    }

    /// <summary>Writes a relayed API answer as it came: status, content type and body.</summary>
    private sealed class RawResult(int status, string? contentType, byte[] body) : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = status;
            if (status is 204 or 304 || body.Length == 0) return;
            response.ContentType = contentType ?? "application/octet-stream";
            response.ContentLength = body.Length;
            await response.Body.WriteAsync(body);
        }
    }

    private async Task<ApiRaw> AnonymousAsync(HttpMethod method, string path, HttpContent? content, string? authorization, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, new Uri(new Uri(_apiBase), path)) { Content = content };
        if (authorization is not null) request.Headers.TryAddWithoutValidation("Authorization", authorization);
        using var response = await _http.CreateClient("EduApi").SendAsync(request, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    private IActionResult ErrorPage(string message)
    {
        ViewData["Title"] = "External tool";
        Response.StatusCode = 400;
        return View("Error", message);
    }
}
