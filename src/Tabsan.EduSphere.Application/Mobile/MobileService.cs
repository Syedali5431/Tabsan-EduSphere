using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Localization;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Mobile;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Mobile;

/// <summary>
/// Plan M12 — app configuration (white-label branding, versions, languages, sign-in options), the single sign-on hand-off
/// from the portal to the app (one-time code bound to the app's PKCE challenge), device registration for native push,
/// crash reports and native push delivery.
/// </summary>
public sealed partial class MobileService : IMobileService
{
    private const string HandoffKeyPrefix = "mobile:handoff:";
    private static readonly Regex Base64Url43 = Base64UrlRegex();

    private readonly IMobileRepository _repo;
    private readonly IDistributedCache _cache;
    private readonly IAuthService _auth;
    private readonly IPortalBrandingService _branding;
    private readonly IPortalLanguageService _languages;
    private readonly ISsoSignInService? _sso;
    private readonly INativePushSender _push;
    private readonly MobileOptions _options;
    private readonly ILogger<MobileService>? _logger;

    public MobileService(IMobileRepository repo, IDistributedCache cache, IAuthService auth, IPortalBrandingService branding,
        IPortalLanguageService languages, INativePushSender push, IOptions<MobileOptions> options,
        ISsoSignInService? sso = null, ILogger<MobileService>? logger = null)
    {
        _repo = repo;
        _cache = cache;
        _auth = auth;
        _branding = branding;
        _languages = languages;
        _push = push;
        _options = options.Value;
        _sso = sso;
        _logger = logger;
    }

    // ── configuration ──────────────────────────────────────────────────────────────────────────

    public async Task<MobileConfigResponse> GetConfigAsync(string? tenantCode, CancellationToken ct = default)
    {
        var brand = await _branding.GetAsync(ct);
        IReadOnlyList<MobileSsoProvider> providers = [];
        if (_sso is not null)
        {
            try { providers = (await _sso.GetProvidersAsync(tenantCode, ct)).Select(p => new MobileSsoProvider(p.Provider, p.DisplayName)).ToList(); }
            catch (Exception ex) when (ex is not OperationCanceledException) { _logger?.LogDebug(ex, "SSO providers unavailable for the app."); }
        }
        var defaultLanguage = await _languages.GetDefaultAsync(ct);
        return new MobileConfigResponse(
            brand.UniversityName, brand.PortalSubtitle, brand.LogoImage,
            _options.MinimumVersion, _options.LatestVersion, _options.AndroidStoreUrl, _options.IosStoreUrl,
            _options.PortalBaseUrl?.TrimEnd('/'), _options.AppRedirectUri,
            defaultLanguage,
            PortalLanguages.All.Select(l => new MobileLanguage(l, PortalLanguages.EnglishName(l), PortalLanguages.NativeName(l), PortalLanguages.IsRtl(l))).ToList(),
            providers, _options.Fcm.Enabled && _push.IsConfigured);
    }

    public bool IsSupportedVersion(string? appVersion)
        => !Version.TryParse(Normalize(_options.MinimumVersion), out var minimum)
           || (Version.TryParse(Normalize(appVersion), out var version) && version >= minimum);

    private static string? Normalize(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var core = v.Trim().TrimStart('v', 'V').Split('-', '+')[0];
        return core.Count(c => c == '.') == 0 ? core + ".0" : core;
    }

    // ── single sign-on hand-off ────────────────────────────────────────────────────────────────

    public async Task<HandoffResponse> CreateHandoffAsync(Guid userId, HandoffRequest request, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new UnauthorizedAccessException("Sign in first.");
        if (!Base64Url43.IsMatch(request.CodeChallenge ?? string.Empty))
            throw new ArgumentException("The code challenge must be the base64url SHA-256 of the app's verifier (43 characters).");
        var code = Base64Url(RandomNumberGenerator.GetBytes(32));
        var ttl = TimeSpan.FromSeconds(Math.Clamp(_options.HandoffSeconds, 30, 600));
        await _cache.SetStringAsync(HandoffKeyPrefix + Hash(code), JsonSerializer.Serialize(new Pending(userId, request.CodeChallenge!)),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        return new HandoffResponse(code, (int)ttl.TotalSeconds);
    }

    public async Task<LoginResult?> RedeemHandoffAsync(HandoffRedeemRequest request, string? ipAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.CodeVerifier)
            || request.CodeVerifier.Length is < 43 or > 128) return null;
        var key = HandoffKeyPrefix + Hash(request.Code.Trim());
        var json = await _cache.GetStringAsync(key, ct);
        if (json is null) return null;
        await _cache.RemoveAsync(key, ct); // single use, even when the verifier is wrong
        var pending = JsonSerializer.Deserialize<Pending>(json);
        if (pending is null) return null;
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(request.CodeVerifier.Trim())));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(challenge), Encoding.ASCII.GetBytes(pending.Challenge))) return null;
        // MFA (when required) was completed in the portal's single sign-on, so this is not asked again.
        return await _auth.ExternalLoginAsync(pending.UserId, "MobileApp", mfaCode: null, mfaSatisfiedExternally: true, ipAddress,
            Cut(request.DeviceInfo, 300) ?? "EduSphere app", ct);
    }

    private sealed record Pending(Guid UserId, string Challenge);

    // ── devices ────────────────────────────────────────────────────────────────────────────────

    public async Task<MobileDeviceResponse> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request, CancellationToken ct = default)
    {
        var platform = ParsePlatform(request.Platform);
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > MobileDevice.MaxTokenLength || token.Any(char.IsWhiteSpace))
            throw new ArgumentException("The push token is not valid.");
        var language = PortalLanguages.Normalize(request.Language);
        var device = await _repo.GetDeviceByTokenAsync(token, ct);
        if (device is null)
        {
            device = new MobileDevice(userId, platform, token, request.AppVersion, request.DeviceModel, language);
            _repo.Add(device);
        }
        else device.Refresh(userId, platform, request.AppVersion, request.DeviceModel, language);
        await _repo.SaveChangesAsync(ct);
        return ToResponse(device);
    }

    public async Task<IReadOnlyList<MobileDeviceResponse>> GetDevicesAsync(Guid userId, CancellationToken ct = default)
        => (await _repo.GetDevicesAsync(userId, ct)).Select(ToResponse).ToList();

    public async Task<bool> UnregisterDeviceAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var device = await _repo.GetDeviceByTokenAsync((token ?? string.Empty).Trim(), ct);
        if (device is null || device.UserId != userId) return false;
        _repo.Remove(device);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    private static MobileDeviceResponse ToResponse(MobileDevice d) => new(d.Id, d.Platform.ToString(), d.AppVersion, d.DeviceModel, d.IsActive, d.LastSeenAt);

    private static MobilePlatform ParsePlatform(string? platform) => (platform ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "android" => MobilePlatform.Android,
        "ios" or "iphone" or "ipad" => MobilePlatform.Ios,
        _ => throw new ArgumentException("Platform must be android or ios.")
    };

    // ── crash reports ──────────────────────────────────────────────────────────────────────────

    public async Task<Guid> ReportCrashAsync(Guid? userId, CrashReportRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExceptionType) && string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Describe the error.");
        var report = new MobileCrashReport(userId, ParsePlatform(request.Platform), request.AppVersion, request.OsVersion, request.DeviceModel,
            request.ExceptionType, request.Message, request.StackTrace, request.Screen, request.OccurredAt.Kind == DateTimeKind.Utc ? request.OccurredAt : request.OccurredAt.ToUniversalTime());
        _repo.Add(report);
        await _repo.SaveChangesAsync(ct);
        return report.Id;
    }

    // ── native push ────────────────────────────────────────────────────────────────────────────

    public async Task<int> SendPushAsync(NotificationType type, string title, string body, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (!_options.Fcm.Enabled || !_push.IsConfigured || userIds.Count == 0) return 0;
        var devices = await _repo.GetDeliverableAsync(userIds, type, ct);
        if (devices.Count == 0) return 0;
        var message = BuildMessage(type, title, body);
        var delivered = 0;
        var now = DateTime.UtcNow;
        foreach (var device in devices)
        {
            NativePushResult result;
            try { result = await _push.SendAsync(device.Token, message, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "Native push to device {DeviceId} failed.", device.Id);
                result = NativePushResult.Failed;
            }
            switch (result)
            {
                case NativePushResult.Delivered: device.RecordSuccess(now); delivered++; break;
                case NativePushResult.Gone: _repo.Remove(device); break;
                default: device.RecordFailure(_options.MaxConsecutiveFailures); break;
            }
        }
        await _repo.SaveChangesAsync(ct);
        return delivered;
    }

    /// <summary>A short notification with the screen to open (the app maps "type" to a tab).</summary>
    public static NativePushMessage BuildMessage(NotificationType type, string title, string body)
    {
        var text = (body ?? string.Empty).Trim();
        if (text.Length > 240) text = text[..239] + "…";
        var heading = (title ?? "EduSphere").Trim();
        if (heading.Length > 120) heading = heading[..119] + "…";
        return new NativePushMessage(heading, text, type, new Dictionary<string, string>
        {
            ["type"] = type.ToString(),
            ["screen"] = type switch
            {
                NotificationType.Assignment => "assignments",
                NotificationType.Result => "results",
                NotificationType.AttendanceAlert => "attendance",
                NotificationType.Announcement => "announcements",
                NotificationType.LiveClass => "home",
                _ => "notifications"
            }
        });
    }

    // ── helpers ────────────────────────────────────────────────────────────────────────────────

    public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static string? Cut(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Length > max ? value[..max] : value;

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$")]
    private static partial Regex Base64UrlRegex();
}
