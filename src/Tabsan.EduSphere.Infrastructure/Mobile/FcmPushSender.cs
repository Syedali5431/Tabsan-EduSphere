using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Mobile;

namespace Tabsan.EduSphere.Infrastructure.Mobile;

/// <summary>
/// Plan M12 — native push through Firebase Cloud Messaging HTTP v1 (Android directly, iOS through APNs). Signs in with the
/// Firebase service account (RS256 JWT bearer grant, token cached until shortly before it expires) and sends one message per
/// device token. An unknown or unregistered token comes back as <see cref="NativePushResult.Gone"/> so the device is removed.
/// </summary>
public sealed class FcmPushSender : INativePushSender
{
    public const string HttpClientName = "fcm";
    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";

    private readonly IHttpClientFactory _http;
    private readonly FcmOptions _options;
    private readonly ILogger<FcmPushSender>? _logger;
    private readonly Lazy<ServiceAccount?> _account;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private (string Token, DateTime ExpiresAt)? _accessToken;

    public FcmPushSender(IHttpClientFactory http, IOptions<MobileOptions> options, ILogger<FcmPushSender>? logger = null)
    {
        _http = http;
        _options = options.Value.Fcm;
        _logger = logger;
        _account = new Lazy<ServiceAccount?>(LoadAccount);
    }

    public bool IsConfigured => _options.Enabled && _account.Value is not null && !string.IsNullOrWhiteSpace(ProjectId);

    private string? ProjectId => string.IsNullOrWhiteSpace(_options.ProjectId) ? _account.Value?.ProjectId : _options.ProjectId;

    public async Task<NativePushResult> SendAsync(string token, NativePushMessage message, CancellationToken ct = default)
    {
        if (!IsConfigured) return NativePushResult.Failed;
        var accessToken = await GetAccessTokenAsync(ct);
        if (accessToken is null) return NativePushResult.Failed;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(ProjectId!)}/messages:send")
        {
            Content = new StringContent(BuildBody(token, message), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.CreateClient(HttpClientName).SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return NativePushResult.Delivered;

        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == HttpStatusCode.NotFound || body.Contains("UNREGISTERED", StringComparison.Ordinal)
            || (response.StatusCode == HttpStatusCode.BadRequest && body.Contains("registration token", StringComparison.OrdinalIgnoreCase)))
            return NativePushResult.Gone;
        if (response.StatusCode == HttpStatusCode.Unauthorized) _accessToken = null;
        _logger?.LogWarning("FCM refused a message ({Status}): {Body}", (int)response.StatusCode, body.Length > 300 ? body[..300] : body);
        return NativePushResult.Failed;
    }

    /// <summary>The FCM v1 message: a visible notification plus data the app uses to open the right screen.</summary>
    public static string BuildBody(string token, NativePushMessage message) => JsonSerializer.Serialize(new
    {
        message = new
        {
            token,
            notification = new { title = message.Title, body = message.Body },
            data = message.Data,
            android = new { priority = "high", notification = new { channel_id = "edusphere", tag = "edusphere-" + message.Type.ToString().ToLowerInvariant() } },
            apns = new { payload = new { aps = new { sound = "default", thread_id = "edusphere-" + message.Type.ToString().ToLowerInvariant() } } }
        }
    });

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken is { } cached && cached.ExpiresAt > DateTime.UtcNow.AddMinutes(1)) return cached.Token;
        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_accessToken is { } again && again.ExpiresAt > DateTime.UtcNow.AddMinutes(1)) return again.Token;
            var account = _account.Value!;
            var assertion = CreateAssertion(account, DateTimeOffset.UtcNow);
            using var response = await _http.CreateClient(HttpClientName).PostAsync(account.TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion
            }), ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Firebase sign-in failed ({Status}).", (int)response.StatusCode);
                return null;
            }
            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var seconds = doc.RootElement.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 3600;
            if (string.IsNullOrEmpty(token)) return null;
            _accessToken = (token, DateTime.UtcNow.AddSeconds(seconds));
            return token;
        }
        finally { _tokenLock.Release(); }
    }

    /// <summary>The signed JWT for Google's OAuth token endpoint (RFC 7523).</summary>
    public static string CreateAssertion(ServiceAccount account, DateTimeOffset now)
    {
        static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = B64(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT", kid = account.PrivateKeyId }));
        var claims = B64(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = account.ClientEmail, scope = Scope, aud = account.TokenUri,
            iat = now.ToUnixTimeSeconds(), exp = now.AddHours(1).ToUnixTimeSeconds()
        }));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(account.PrivateKeyPem);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(header + "." + claims), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return header + "." + claims + "." + B64(signature);
    }

    public sealed record ServiceAccount(string ClientEmail, string PrivateKeyPem, string? PrivateKeyId, string TokenUri, string? ProjectId);

    /// <summary>Reads the service account key (file or JSON); null when FCM is not configured or the key is unusable.</summary>
    public static ServiceAccount? ParseAccount(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string? Get(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var email = Get("client_email");
        var key = Get("private_key");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(key)) return null;
        var tokenUri = Get("token_uri") ?? "https://oauth2.googleapis.com/token";
        if (!Uri.TryCreate(tokenUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return null;
        return new ServiceAccount(email, key, Get("private_key_id"), tokenUri, Get("project_id"));
    }

    private ServiceAccount? LoadAccount()
    {
        if (!_options.Enabled) return null;
        try
        {
            var json = !string.IsNullOrWhiteSpace(_options.ServiceAccountJson) ? _options.ServiceAccountJson
                : !string.IsNullOrWhiteSpace(_options.ServiceAccountFile) && File.Exists(_options.ServiceAccountFile) ? File.ReadAllText(_options.ServiceAccountFile)
                : null;
            return json is null ? null : ParseAccount(json);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger?.LogWarning(ex, "The Firebase service account key could not be read.");
            return null;
        }
    }
}
