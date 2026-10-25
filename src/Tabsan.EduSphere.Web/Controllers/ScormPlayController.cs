using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lms.Scorm;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M13 — the SCORM player: a shell page with the SCORM runtime (scorm-again) and the package in a frame below it, both
/// under /scorm/play/{token}/ so the package finds the runtime as a same-origin parent. Everything here is authorised by the
/// play link alone (no portal session or cookies), relayed to the API, so it can run on a separate content origin
/// (Scorm:ContentOrigin) that cannot read the portal's pages or cookies.
/// </summary>
[Route("scorm/play/{token}")]
public sealed class ScormPlayController : Controller
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> CopiedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Range", "Accept-Ranges", "Last-Modified", "ETag", "Cache-Control"
    };

    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public ScormPlayController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    /// <summary>The shell. Always served with a trailing slash so the package's relative links stay under the play link.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Shell(string token, [FromQuery] string? part, CancellationToken ct)
    {
        if (!(Request.Path.Value ?? "").EndsWith('/')) return Redirect(Request.PathBase + Request.Path + "/" + Request.QueryString);
        Isolate(shell: true);
        using var response = await Client().GetAsync(ApiPath(token, string.IsNullOrEmpty(part) ? "" : "?part=" + Uri.EscapeDataString(part)), ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return View("Expired");
        if (!response.IsSuccessStatusCode) return View("Unavailable");
        var play = await response.Content.ReadFromJsonAsync<ScormPlayResponse>(Json, ct);
        if (play is null) return View("Expired");
        // A sequenced package with nothing to open now (it has ended, or the learner has to choose).
        if (play.LaunchPath.Length == 0) return View("Ended", new ScormEndedModel { Title = play.Title, Message = play.Message, MessageArg = play.MessageArg, Ended = play.Ended });
        return View("Shell", new ScormShellModel
        {
            Title = play.Title,
            Version = play.Version,
            Mode = play.Mode,
            LaunchUrl = "content/" + LaunchUrl(play.LaunchPath),
            CmiJson = play.CmiJson,
            // Multi-part packages: each part commits to its own address (the part is not in the query, which the runtime extends);
            // sequenced parts add their attempt number, so a late commit from a frame being replaced is ignored.
            CommitUrl = play.PartId is { } partId && (play.Parts is { Count: > 1 } || play.Sequenced)
                ? "commit/" + Uri.EscapeDataString(partId) + (play.Sequenced ? "/" + play.PartAttempt : "")
                : "commit",
            PartId = play.PartId,
            Sequenced = play.Sequenced,
            CanContinue = play.CanContinue,
            CanPrevious = play.CanPrevious
        });
    }

    /// <summary>A file of the package (streamed; byte ranges pass through for audio and video).</summary>
    [HttpGet("content/{**path}")]
    public async Task Content(string token, string? path, CancellationToken ct)
    {
        Isolate(shell: false);
        var relative = (Request.Path.Value ?? "")[(Request.Path.Value!.IndexOf("/content/", StringComparison.Ordinal) + "/content/".Length)..];
        var encoded = string.Join('/', relative.Split('/').Select(Uri.EscapeDataString));
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiPath(token, "/content/" + encoded));
        if (Request.Headers.Range.Count > 0) request.Headers.TryAddWithoutValidation("Range", Request.Headers.Range.ToString());
        using var response = await Client().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        Response.StatusCode = (int)response.StatusCode;
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.RequestedRangeNotSatisfiable) return;
        foreach (var header in response.Headers.Concat(response.Content.Headers).Where(h => CopiedHeaders.Contains(h.Key)))
            Response.Headers[header.Key] = header.Value.ToArray();
        Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        if (response.Content.Headers.ContentLength is { } length) Response.ContentLength = length;
        await using var body = await response.Content.ReadAsStreamAsync(ct);
        await body.CopyToAsync(Response.Body, ct);
    }

    /// <summary>The runtime's commit (JSON, or text/plain from sendBeacon) relayed to the API; answers { result, errorCode }.</summary>
    [HttpPost("commit")]
    [HttpPost("commit/{part}")]
    [HttpPost("commit/{part}/{n:int}")]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(4_500_000)]
    public async Task<IActionResult> Commit(string token, string? part, int? n, [FromQuery] bool final, CancellationToken ct)
    {
        Isolate(shell: false);
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);
        var path = "/commit" + (string.IsNullOrEmpty(part) ? "" : "/" + Uri.EscapeDataString(part)) + (n is { } attempt ? "/" + attempt : "") + (final ? "?final=true" : "");
        using var response = await Client().PostAsync(ApiPath(token, path), new StringContent(body, Encoding.UTF8, "application/json"), ct);
        var answer = await response.Content.ReadAsStringAsync(ct);
        return new ContentResult { StatusCode = (int)response.StatusCode, Content = answer, ContentType = "application/json" };
    }

    /// <summary>Sequenced packages: a navigation request (Next, Previous, a part from the contents, or a SCO's request) relayed to the API.</summary>
    [HttpPost("navigate")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Navigate(string token, CancellationToken ct)
    {
        Isolate(shell: false);
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);
        if (body.Length > 4000) return BadRequest();
        using var response = await Client().PostAsync(ApiPath(token, "/navigate"), new StringContent(body, Encoding.UTF8, "application/json"), ct);
        return new ContentResult { StatusCode = (int)response.StatusCode, Content = await response.Content.ReadAsStringAsync(ct), ContentType = "application/json" };
    }

    /// <summary>Multi-part packages: each part's status (the player page refreshes its contents list with it).</summary>
    [HttpGet("parts")]
    public async Task<IActionResult> Parts(string token, CancellationToken ct)
    {
        Isolate(shell: false);
        Response.Headers.CacheControl = "no-store";
        using var response = await Client().GetAsync(ApiPath(token, "/parts"), ct);
        return new ContentResult { StatusCode = (int)response.StatusCode, Content = await response.Content.ReadAsStringAsync(ct), ContentType = "application/json" };
    }

    // ── pieces ──

    private HttpClient Client() => _http.CreateClient("EduApi");

    private string ApiPath(string token, string rest)
        => $"{(_config["EduApi:BaseUrl"] ?? "").TrimEnd('/')}/api/v1/scorm/play/{Uri.EscapeDataString(token)}{rest}";

    /// <summary>No referrer (the play link must not leak to sites the package loads from), no sniffing, framed only by the portal.</summary>
    private void Isolate(bool shell)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var ancestors = shell ? $"'self' {_config["Scorm:PortalOrigin"]}".Trim() : "'self'";
        Response.Headers["Content-Security-Policy"] = $"frame-ancestors {ancestors}";
        if (shell) Response.Headers.CacheControl = "no-store";
    }

    /// <summary>The manifest's launch path with each path segment encoded once (its query/fragment kept).</summary>
    public static string LaunchUrl(string launchPath)
    {
        var cut = launchPath.IndexOfAny(['?', '#']);
        var (path, rest) = cut >= 0 ? (launchPath[..cut], launchPath[cut..]) : (launchPath, "");
        return string.Join('/', path.Split('/').Select(s => Uri.EscapeDataString(Uri.UnescapeDataString(s)))) + rest;
    }
}
