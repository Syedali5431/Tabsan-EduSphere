using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

[Route("Portal/Login")]
[Route("Login")]
[Route("Home/Login")]
public class LoginController : Controller
{
    private readonly IEduApiClient _api;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _http;
    private readonly IDataProtector _ssoProtector;
    private readonly string _configuredApiBaseUrl;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // Plan M1: binds an in-flight SSO sign-in to this browser (login-CSRF protection).
    private const string SsoCookieName = ".Tabsan.EduSphere.Sso";
    private const string SsoCallbackPath = "/Login/SsoCallback";
    private const string SamlAcsPath = "/Login/SamlAcs";

    public LoginController(IEduApiClient api, IConfiguration config, IHttpClientFactory http, IDataProtectionProvider dataProtection)
    {
        _api    = api;
        _config = config;
        _http   = http;
        _ssoProtector = dataProtection.CreateProtector("Tabsan.EduSphere.Web.SsoState.v1");

        var configured = _config["EduApi:BaseUrl"] ?? throw new InvalidOperationException("EduApi:BaseUrl is required.");
        if (!TryNormalizeApiBaseUrl(configured, out var normalized))
            throw new InvalidOperationException("EduApi:BaseUrl is invalid. Use a full URL such as https://localhost:7061.");

        _configuredApiBaseUrl = normalized;
    }

    // GET /Portal/Login
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null, CancellationToken ct = default)
    {
        if (_api.IsConnected())
            return RedirectToAction(ResolveHomeAction(_api.GetSessionIdentity()?.Roles.FirstOrDefault()), "Portal");

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
        var apiBaseCandidate = string.IsNullOrWhiteSpace(existingConnection.ApiBaseUrl)
            ? _configuredApiBaseUrl
            : existingConnection.ApiBaseUrl;

        if (!TryNormalizeApiBaseUrl(apiBaseCandidate, out var apiBase))
        {
            ViewData["Error"] = "API base URL is invalid. Use a full URL such as https://localhost:7061.";
            ViewData["ReturnUrl"] = returnUrl;
            await PopulateSecurityProfileAsync(_configuredApiBaseUrl, ct);
            return View();
        }

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
                // Read once and reuse — the account may not have MFA enabled at all,
                // in which case an invalid password looks the same as an invalid MFA code (401)
                // and we must not reveal the MFA field for accounts that don't use it.
                var isInvalidMfaCode = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    && await IsInvalidMfaCodeResponseAsync(response);
                var mfaCodeNeeded = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    || response.StatusCode == System.Net.HttpStatusCode.PreconditionRequired
                    || isInvalidMfaCode;
                // Plan M1.4: correct password, but the institution requires single sign-on for this account.
                var ssoRequired = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    && (await response.Content.ReadAsStringAsync(ct)).Contains("SSO_REQUIRED", StringComparison.Ordinal);

                ViewData["Error"] = ssoRequired
                    ? "Your institution requires single sign-on. Use your organization's sign-in button below."
                    : response.StatusCode switch
                {
                    System.Net.HttpStatusCode.BadRequest
                        => "This account requires an authenticator code. Enter it below and sign in again.",

                    System.Net.HttpStatusCode.Unauthorized
                        => isInvalidMfaCode
                            ? "Invalid authenticator code. Please try again."
                            : "Invalid username or password.",

                    System.Net.HttpStatusCode.PreconditionRequired
                        => "Multi-factor authentication is required. Enter your authenticator code and sign in again.",

                    System.Net.HttpStatusCode.Locked
                        => "Login blocked by session risk controls. Retry from a trusted network or contact support.",

                    _
                        => $"Login failed (HTTP {(int)response.StatusCode})."
                };
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["Username"] = username; // Preserve so user doesn't re-type.
                await PopulateSecurityProfileAsync(apiBase, ct);

                // The API only reveals MFA requirement after verifying credentials, so the
                // deployment-wide security profile alone can under-report it for accounts that
                // individually enabled MFA. Force the field to show once the API has told us
                // this specific sign-in attempt needs a code.
                if (mfaCodeNeeded)
                {
                    ViewData["MfaEnabled"] = true;
                }

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

            return CompleteSignIn(result, apiBase, returnUrl);
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = $"Cannot reach the API at {apiBase}. Make sure the API is running.";
            ViewData["ReturnUrl"] = returnUrl;
            await PopulateSecurityProfileAsync(apiBase, ct);
            return View();
        }
    }

    /// <summary>Stores the API session for a successful login (password or SSO) and redirects.</summary>
    private IActionResult CompleteSignIn(LoginApiResponse result, string apiBase, string? returnUrl)
    {
        var existingConnection = _api.GetConnection();

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

        var redirect = Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(ResolveHomeAction(result.Role), "Portal")!;
        return Redirect(redirect);
    }

    // ── Plan M1 Stage M1.2: OpenID Connect single sign-on ─────────────────────

    // GET /Login/Sso/{provider}?tenant=CODE&returnUrl=...
    [HttpGet("Sso/{provider}")]
    public async Task<IActionResult> Sso(string provider, string? tenant = null, string? returnUrl = null, CancellationToken ct = default)
    {
        if (_api.IsConnected())
            return RedirectToAction(nameof(Index));

        // SAML returns via an HTTP-POST to the ACS; OIDC via a GET to the callback.
        var isSaml = string.Equals(provider, "saml", StringComparison.OrdinalIgnoreCase);
        var redirectUri = BuildPublicUrl(isSaml ? SamlAcsPath : SsoCallbackPath);
        try
        {
            var client = _http.CreateClient();
            var payload = JsonSerializer.Serialize(new { provider, redirectUri, tenantCode = tenant });
            using var response = await client.PostAsync($"{_configuredApiBaseUrl}/api/v1/sso/start",
                new StringContent(payload, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
                return await RenderLoginErrorAsync("Single sign-on is not available for this provider right now.", returnUrl, ct);

            var start = JsonSerializer.Deserialize<SsoStartApiResponse>(await response.Content.ReadAsStringAsync(ct), _json);
            if (start is null
                || string.IsNullOrWhiteSpace(start.State)
                || !Uri.TryCreate(start.AuthorizationUrl, UriKind.Absolute, out var authorizeUri)
                || authorizeUri.Scheme != Uri.UriSchemeHttps)
                return await RenderLoginErrorAsync("Unexpected response while starting single sign-on.", returnUrl, ct);

            var cookie = new SsoCookie(start.State, Url.IsLocalUrl(returnUrl) ? returnUrl : null, tenant);
            Response.Cookies.Append(SsoCookieName, _ssoProtector.Protect(JsonSerializer.Serialize(cookie)), new CookieOptions
            {
                HttpOnly = true,
                // OIDC returns with a top-level GET (Lax is enough); SAML returns with a cross-site
                // POST from the IdP, which browsers only send SameSite=None (Secure) cookies with.
                Secure = isSaml || Request.IsHttps,
                SameSite = isSaml ? SameSiteMode.None : SameSiteMode.Lax,
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromMinutes(10)
            });

            return Redirect(authorizeUri.ToString());
        }
        catch (HttpRequestException)
        {
            return await RenderLoginErrorAsync($"Cannot reach the API at {_configuredApiBaseUrl}. Make sure the API is running.", returnUrl, ct);
        }
    }

    // GET /Login/SsoCallback?code=...&state=... (registered as the redirect URI at the IdP)
    [HttpGet("SsoCallback")]
    public async Task<IActionResult> SsoCallback(string? code, string? state, string? error, CancellationToken ct = default)
    {
        var cookie = ReadSsoCookie();
        Response.Cookies.Delete(SsoCookieName, new CookieOptions { Path = "/" });
        var returnUrl = cookie?.ReturnUrl;

        if (cookie is null || string.IsNullOrEmpty(state) || !FixedTimeEquals(cookie.State, state))
            return await RenderLoginErrorAsync("Your single sign-on session expired or is invalid. Please try again.", returnUrl, ct);

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            return await RenderLoginErrorAsync("Sign-in was cancelled or failed at the identity provider.", returnUrl, ct);

        var payload = new { state, code, deviceInfo = Request.Headers.UserAgent.ToString() };
        return await PostSsoAsync("complete", payload, returnUrl, cookie.Tenant, ct);
    }

    // POST /Login/SamlAcs — SAML assertion consumer service (the IdP posts SAMLResponse + RelayState here)
    [HttpPost("SamlAcs")]
    [IgnoreAntiforgeryToken] // cross-site POST from the IdP; protected by the signed assertion + browser-bound RelayState
    [RequestSizeLimit(512_000)]
    public async Task<IActionResult> SamlAcs(string? samlResponse, string? relayState, CancellationToken ct = default)
    {
        var cookie = ReadSsoCookie();
        Response.Cookies.Delete(SsoCookieName, new CookieOptions { Path = "/", Secure = true, SameSite = SameSiteMode.None });
        var returnUrl = cookie?.ReturnUrl;

        if (cookie is null || string.IsNullOrEmpty(relayState) || !FixedTimeEquals(cookie.State, relayState))
            return await RenderLoginErrorAsync("Your single sign-on session expired or is invalid. Please try again.", returnUrl, ct);

        if (string.IsNullOrWhiteSpace(samlResponse))
            return await RenderLoginErrorAsync("Sign-in was cancelled or failed at the identity provider.", returnUrl, ct);

        var payload = new { samlResponse, relayState, deviceInfo = Request.Headers.UserAgent.ToString() };
        return await PostSsoAsync("saml/complete", payload, returnUrl, cookie.Tenant, ct);
    }

    // GET /Login/SamlMetadata?tenant=CODE — service-provider metadata (also the SP entity ID) for the IdP administrator
    [HttpGet("SamlMetadata")]
    public async Task<IActionResult> SamlMetadata(string? tenant = null, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient();
            var url = $"{_configuredApiBaseUrl}/api/v1/sso/saml/metadata?acs={Uri.EscapeDataString(BuildPublicUrl(SamlAcsPath))}"
                      + (string.IsNullOrWhiteSpace(tenant) ? string.Empty : "&tenant=" + Uri.EscapeDataString(tenant.Trim()));
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return NotFound();

            return Content(await response.Content.ReadAsStringAsync(ct), "application/samlmetadata+xml");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }
    // POST /Login/SsoMfa — authenticator code for SSO users with TOTP enabled
    [HttpPost("SsoMfa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SsoMfa(string ticket, string mfaCode, string? returnUrl = null, string? tenant = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return await RenderLoginErrorAsync("Your single sign-on session expired. Please try again.", returnUrl, ct);

        if (string.IsNullOrWhiteSpace(mfaCode))
            return await RenderSsoMfaAsync(ticket, "Enter the code from your authenticator app.", returnUrl, tenant, ct);

        var payload = new { ticket, mfaCode = mfaCode.Trim(), deviceInfo = Request.Headers.UserAgent.ToString() };
        return await PostSsoAsync("complete-mfa", payload, returnUrl, tenant, ct);
    }

    private async Task<IActionResult> PostSsoAsync(string action, object payload, string? returnUrl, string? tenant, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient();
            using var response = await client.PostAsync($"{_configuredApiBaseUrl}/api/v1/sso/{action}",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<LoginApiResponse>(body, _json);
                return result is null || string.IsNullOrWhiteSpace(result.AccessToken)
                    ? await RenderLoginErrorAsync("Unexpected response from API.", returnUrl, ct)
                    : CompleteSignIn(result, _configuredApiBaseUrl, returnUrl);
            }

            var failure = TryDeserialize<SsoErrorApiResponse>(body);
            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(failure?.Ticket))
            {
                var message = failure.Error == "InvalidMfaCode" ? "Invalid authenticator code. Please try again." : null;
                return await RenderSsoMfaAsync(failure.Ticket, message, returnUrl, tenant, ct);
            }

            return await RenderLoginErrorAsync(DescribeSsoFailure(failure), returnUrl, ct);
        }
        catch (HttpRequestException)
        {
            return await RenderLoginErrorAsync($"Cannot reach the API at {_configuredApiBaseUrl}. Make sure the API is running.", returnUrl, ct);
        }
    }

    private static string DescribeSsoFailure(SsoErrorApiResponse? failure) => failure?.Error switch
    {
        "AccountNotLinked"      => "This account is not linked to an EduSphere user. Sign in with your username and password, or ask your administrator to link it.",
        "EmailDomainNotAllowed" => "This account's email domain is not allowed for this institution.",
        "InvalidState"          => "Your single sign-on session expired or is invalid. Please try again.",
        "LoginRejected"         => failure.LoginFailure switch
        {
            "ConcurrencyLimitReached" => "Login limit reached. The maximum number of concurrent users allowed by the current license has been reached.",
            "SessionRiskBlocked"      => "Login blocked by session risk controls. Retry from a trusted network or contact support.",
            _                         => "Your EduSphere account is inactive or locked. Contact your administrator."
        },
        _ => "Single sign-on failed. Please try again or sign in with your username and password."
    };

    private async Task<IActionResult> RenderLoginErrorAsync(string error, string? returnUrl, CancellationToken ct)
    {
        ViewData["Error"] = error;
        ViewData["ReturnUrl"] = returnUrl;
        await PopulateSecurityProfileAsync(_configuredApiBaseUrl, ct);
        return View(nameof(Index));
    }

    private async Task<IActionResult> RenderSsoMfaAsync(string ticket, string? error, string? returnUrl, string? tenant, CancellationToken ct)
    {
        ViewData["SsoMfaTicket"] = ticket;
        ViewData["Tenant"] = tenant;
        ViewData["Error"] = error;
        ViewData["ReturnUrl"] = returnUrl;
        await PopulateSecurityProfileAsync(_configuredApiBaseUrl, ct);
        return View(nameof(Index));
    }

    private string BuildPublicUrl(string path) => Helpers.SsoPublicUrls.Build(Request, _config, path);

    private SsoCookie? ReadSsoCookie()
    {
        if (!Request.Cookies.TryGetValue(SsoCookieName, out var raw) || string.IsNullOrEmpty(raw))
            return null;
        try
        {
            return JsonSerializer.Deserialize<SsoCookie>(_ssoProtector.Unprotect(raw));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            return null;
        }
    }

    private static bool FixedTimeEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private T? TryDeserialize<T>(string body) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(body, _json); }
        catch (JsonException) { return null; }
    }

    private sealed record SsoCookie(string State, string? ReturnUrl, string? Tenant);
    private sealed record SsoStartApiResponse(string AuthorizationUrl, string State);
    private sealed record SsoErrorApiResponse(string? Error, string? Ticket, string? LoginFailure);
    private sealed record SsoProviderApiResponse(string Provider, string DisplayName);

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

        await PopulateSsoProvidersAsync(apiBase, ct);
    }

    /// <summary>
    /// Plan M1: loads enabled SSO providers for the login buttons. The tenant comes from
    /// ?tenant=CODE (multi-tenant portals) and is carried through the login form.
    /// A 403 here simply means the "sso" module is not licensed/active — no buttons.
    /// </summary>
    private async Task PopulateSsoProvidersAsync(string apiBase, CancellationToken ct)
    {
        var tenant = ViewData["Tenant"] as string ?? Request.Query["tenant"].FirstOrDefault();
        ViewData["Tenant"] = string.IsNullOrWhiteSpace(tenant) ? null : tenant.Trim();

        try
        {
            var client = _http.CreateClient();
            var url = $"{apiBase}/api/v1/sso/providers"
                      + (string.IsNullOrWhiteSpace(tenant) ? string.Empty : "?tenant=" + Uri.EscapeDataString(tenant.Trim()));
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return;

            var providers = JsonSerializer.Deserialize<List<SsoProviderApiResponse>>(await response.Content.ReadAsStringAsync(ct), _json);
            ViewData["SsoProviders"] = providers?
                .Select(p => new SsoProviderButton(p.Provider, p.DisplayName))
                .ToList();
        }
        catch
        {
            // SSO buttons are optional — never block the password login page.
        }
    }

    private static string ResolveHomeAction(string? role)
        => role?.ToLowerInvariant() switch
        {
            "superadmin" => nameof(PortalController.Dashboard),
            "student" => nameof(PortalController.Announcements),
            _ => nameof(PortalController.Helpdesk)
        };

    private static bool TryNormalizeApiBaseUrl(string rawBaseUrl, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(rawBaseUrl))
            return false;

        var candidate = rawBaseUrl.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            var isLocal = candidate.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                          || candidate.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                          || candidate.StartsWith("[::1]", StringComparison.OrdinalIgnoreCase);
            candidate = (isLocal ? "http://" : "https://") + candidate;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        normalized = uri.ToString().TrimEnd('/');
        return true;
    }

    private static async Task<bool> IsInvalidMfaCodeResponseAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            return body.Contains("INVALID_MFA_CODE", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
