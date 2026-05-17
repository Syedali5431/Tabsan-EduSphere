using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

[Route("Portal/Login")]
public class LoginController : Controller
{
    private readonly IEduApiClient _api;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _http;
    private readonly string _configuredApiBaseUrl;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public LoginController(IEduApiClient api, IConfiguration config, IHttpClientFactory http)
    {
        _api    = api;
        _config = config;
        _http   = http;
        _configuredApiBaseUrl = (_config["EduApi:BaseUrl"] ?? throw new InvalidOperationException("EduApi:BaseUrl is required."))
            .TrimEnd('/');
    }

    // GET /Portal/Login
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null, CancellationToken ct = default)
    {
        if (_api.IsConnected())
            return RedirectToAction("Dashboard", "Portal");

        var apiBase = _configuredApiBaseUrl;
        await PopulateSecurityProfileAsync(apiBase, ct);

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST /Portal/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string username, string password, string? mfaCode = null, string? returnUrl = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewData["Error"] = "Username and password are required.";
            ViewData["ReturnUrl"] = returnUrl;
            await PopulateSecurityProfileAsync(_configuredApiBaseUrl, ct);
            return View();
        }

        var existingConnection = _api.GetConnection();
        var apiBase = (string.IsNullOrWhiteSpace(existingConnection.ApiBaseUrl)
            ? _configuredApiBaseUrl
            : existingConnection.ApiBaseUrl).TrimEnd('/');

        try
        {
            var client  = _http.CreateClient();
            var payload = JsonSerializer.Serialize(new
            {
                username,
                password,
                mfaCode,
                deviceInfo = Request.Headers.UserAgent.ToString()
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{apiBase}/api/v1/auth/login", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                ViewData["Error"] = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized
                        => "Invalid username or password.",
                    System.Net.HttpStatusCode.PreconditionRequired
                        => "MFA is required. Enter your MFA code and sign in again.",
                    System.Net.HttpStatusCode.Locked
                        => "Login blocked by session risk controls. Retry from a trusted network or contact support.",
                    _
                        => $"Login failed (HTTP {(int)response.StatusCode})."
                };
                ViewData["ReturnUrl"] = returnUrl;
                await PopulateSecurityProfileAsync(apiBase, ct);
                return View();
            }

            var body   = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<LoginApiResponse>(body, _json);

            if (result is null || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                ViewData["Error"] = "Unexpected response from API.";
                ViewData["ReturnUrl"] = returnUrl;
                await PopulateSecurityProfileAsync(apiBase, ct);
                return View();
            }

            ViewData["SessionRiskLevel"] = result.SessionRiskLevel;
            ViewData["MfaEnabled"] = result.MfaEnabled;
            ViewData["SsoEnabled"] = result.SsoEnabled;
            ViewData["SsoProvider"] = result.SsoProvider;

            // Reuse existing connection machinery — stores token + identity in session
            _api.SaveConnection(new ApiConnectionModel
            {
                ApiBaseUrl  = apiBase,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                DefaultDepartmentId = existingConnection.DefaultDepartmentId
            });

            _api.SetForcePasswordChangeRequired(result.MustChangePassword);

            if (result.MustChangePassword)
                return RedirectToAction("ForceChangePassword", "Portal");

            var redirect = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Dashboard", "Portal")!;
            return Redirect(redirect);
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = $"Cannot reach the API at {apiBase}. Make sure the API is running.";
            ViewData["ReturnUrl"] = returnUrl;
            await PopulateSecurityProfileAsync(apiBase, ct);
            return View();
        }
    }

    // POST /Portal/Login/Logout
    [HttpPost]
    [Route("Logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _api.SaveConnection(new ApiConnectionModel());
        _api.SetForcePasswordChangeRequired(false);
        return RedirectToAction(nameof(Index));
    }

    // ── Private DTO ───────────────────────────────────────────────────────────
    private sealed record LoginApiResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        string Role,
        System.Guid UserId,
        string Username,
        bool MustChangePassword,
        bool MfaEnabled,
        bool SsoEnabled,
        string? SsoProvider,
        string SessionRiskLevel);

    private sealed record SecurityProfileApiResponse(
        bool MfaEnabled,
        bool RequireMfaForPasswordLogin,
        bool RequireMfaForPrivilegedRolesOnly,
        string[]? PrivilegedMfaRoles,
        bool SsoEnabled,
        string? SsoProvider,
        string? SsoLoginUrl,
        bool SessionRiskEnabled,
        bool BlockHighRiskLogin);

    private async Task PopulateSecurityProfileAsync(string apiBase, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient();
            using var response = await client.GetAsync($"{apiBase}/api/v1/auth/security-profile", ct);
            if (!response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync(ct);
            var profile = JsonSerializer.Deserialize<SecurityProfileApiResponse>(body, _json);
            if (profile is null)
                return;

            ViewData["MfaEnabled"] = profile.MfaEnabled && profile.RequireMfaForPasswordLogin;
            ViewData["MfaPrivilegedOnly"] = profile.RequireMfaForPrivilegedRolesOnly;
            ViewData["MfaPrivilegedRoles"] = profile.PrivilegedMfaRoles;
            ViewData["SsoEnabled"] = profile.SsoEnabled;
            ViewData["SsoProvider"] = profile.SsoProvider;
            ViewData["SsoLoginUrl"] = profile.SsoLoginUrl;
            ViewData["SessionRiskEnabled"] = profile.SessionRiskEnabled;
            ViewData["SessionRiskBlocking"] = profile.BlockHighRiskLogin;
        }
        catch
        {
            // Login page should still render even when security profile endpoint is unreachable.
        }
    }
}
