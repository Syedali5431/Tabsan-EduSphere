using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Notifications;

namespace Tabsan.EduSphere.Infrastructure.Notifications;

/// <summary>
/// Plan M4.3: Web Push message encryption (RFC 8291, "aes128gcm" content coding, RFC 8188) with .NET's own
/// ECDH P-256, HKDF-SHA-256 and AES-128-GCM — no third-party crypto.
/// </summary>
public static class WebPushEncryption
{
    public const int RecordSize = 4096;

    /// <summary>
    /// Encrypts <paramref name="plaintext"/> for a browser subscription. <paramref name="serverKey"/> and
    /// <paramref name="salt"/> are only supplied by tests (RFC 8291 Appendix A); normally both are fresh per message.
    /// </summary>
    public static byte[] Encrypt(byte[] uaPublic, byte[] authSecret, byte[] plaintext, ECDiffieHellman? serverKey = null, byte[]? salt = null)
    {
        if (uaPublic.Length != 65 || uaPublic[0] != 0x04) throw new ArgumentException("The browser key must be an uncompressed P-256 point.", nameof(uaPublic));
        if (authSecret.Length != 16) throw new ArgumentException("The auth secret must be 16 bytes.", nameof(authSecret));
        if (plaintext.Length > RecordSize - 17 - 86) throw new ArgumentException("The push payload is too large.", nameof(plaintext));

        using var ownKey = serverKey is null ? ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256) : null;
        var asKey = serverKey ?? ownKey!;
        salt ??= RandomNumberGenerator.GetBytes(16);

        var asPublic = ExportPublic(asKey);
        using var uaKey = ImportPublic(uaPublic);
        var ecdhSecret = asKey.DeriveRawSecretAgreement(uaKey.PublicKey);

        // IKM = HKDF(auth_secret, ecdh_secret, "WebPush: info" || 0x00 || ua_public || as_public, 32)
        var keyInfo = Concat(Encoding.ASCII.GetBytes("WebPush: info\0"), uaPublic, asPublic);
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, ecdhSecret, 32, authSecret, keyInfo);

        // CEK / NONCE from the per-message salt.
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
        var cek = HKDF.Expand(HashAlgorithmName.SHA256, prk, 16, Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0"));
        var nonce = HKDF.Expand(HashAlgorithmName.SHA256, prk, 12, Encoding.ASCII.GetBytes("Content-Encoding: nonce\0"));

        // Single record: plaintext || 0x02 (last-record delimiter), no extra padding.
        var record = new byte[plaintext.Length + 1];
        plaintext.CopyTo(record, 0);
        record[^1] = 0x02;
        var cipher = new byte[record.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(cek, 16)) aes.Encrypt(nonce, record, cipher, tag);

        // Header: salt(16) || rs(4, big-endian) || idlen(1) || keyid(as_public)
        var header = new byte[16 + 4 + 1 + asPublic.Length];
        salt.CopyTo(header, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16, 4), RecordSize);
        header[20] = (byte)asPublic.Length;
        asPublic.CopyTo(header, 21);
        return Concat(header, cipher, tag);
    }

    /// <summary>Reverses <see cref="Encrypt"/> with the browser's private key (used by tests).</summary>
    public static byte[] Decrypt(ECDiffieHellman uaKey, byte[] authSecret, byte[] body)
    {
        var salt = body[..16];
        var idlen = body[20];
        var asPublic = body[21..(21 + idlen)];
        var payload = body[(21 + idlen)..];
        var uaPublic = ExportPublic(uaKey);
        using var asKey = ImportPublic(asPublic);
        var ecdhSecret = uaKey.DeriveRawSecretAgreement(asKey.PublicKey);
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, ecdhSecret, 32, authSecret, Concat(Encoding.ASCII.GetBytes("WebPush: info\0"), uaPublic, asPublic));
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
        var cek = HKDF.Expand(HashAlgorithmName.SHA256, prk, 16, Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0"));
        var nonce = HKDF.Expand(HashAlgorithmName.SHA256, prk, 12, Encoding.ASCII.GetBytes("Content-Encoding: nonce\0"));
        var plain = new byte[payload.Length - 16];
        using (var aes = new AesGcm(cek, 16)) aes.Decrypt(nonce, payload[..^16], payload[^16..], plain);
        var end = Array.LastIndexOf(plain, (byte)0x02);
        return plain[..end];
    }

    public static byte[] ExportPublic(ECDiffieHellman key)
    {
        var p = key.ExportParameters(false);
        return Concat([0x04], p.Q.X!, p.Q.Y!);
    }

    public static ECDiffieHellman ImportPublic(byte[] point)
        => ECDiffieHellman.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = point[1..33], Y = point[33..65] } });

    private static byte[] Concat(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)];
        var offset = 0;
        foreach (var p in parts) { p.CopyTo(result, offset); offset += p.Length; }
        return result;
    }
}

/// <summary>Plan M4.3: VAPID (RFC 8292) application server keys and request signing.</summary>
public sealed class VapidKeys
{
    public required byte[] PublicKey { get; init; }   // 65-byte uncompressed point
    public required byte[] PrivateKey { get; init; }  // 32-byte scalar

    public static VapidKeys Generate()
    {
        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ec.ExportParameters(true);
        return new VapidKeys { PublicKey = [0x04, .. p.Q.X!, .. p.Q.Y!], PrivateKey = p.D! };
    }

    /// <summary>"vapid t=…, k=…" for one push service origin (valid 12 hours).</summary>
    public string AuthorizationHeader(Uri endpoint, string subject, DateTimeOffset now)
    {
        var audience = endpoint.GetLeftPart(UriPartial.Authority);
        var header = Base64Url.Encode(Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"alg\":\"ES256\"}"));
        var claims = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { aud = audience, exp = now.AddHours(12).ToUnixTimeSeconds(), sub = subject }));
        using var ec = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = PrivateKey,
            Q = new ECPoint { X = PublicKey[1..33], Y = PublicKey[33..65] }
        });
        var signature = ec.SignData(Encoding.ASCII.GetBytes(header + "." + claims), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"vapid t={header}.{claims}.{Base64Url.Encode(signature)}, k={Base64Url.Encode(PublicKey)}";
    }
}

/// <summary>
/// Plan M4.3: where the VAPID key pair lives. Configuration (WebPush:PublicKey/PrivateKey) wins; otherwise a key file
/// outside the repository (WebPush:KeyFile, default &lt;app-data&gt;/Tabsan/vapid-keys.json) that is created on first use.
/// Every API/worker node must use the same keys, or browsers reject the messages.
/// </summary>
public sealed class VapidKeyStore
{
    private readonly WebPushOptions _options;
    private readonly ILogger<VapidKeyStore>? _logger;
    private readonly Lazy<VapidKeys?> _keys;

    public VapidKeyStore(IOptions<WebPushOptions> options, ILogger<VapidKeyStore>? logger = null)
    {
        _options = options.Value;
        _logger = logger;
        _keys = new Lazy<VapidKeys?>(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public VapidKeys? Keys => _keys.Value;

    public static string DefaultKeyFile()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Tabsan", "vapid-keys.json");

    private VapidKeys? Load()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_options.PublicKey) && !string.IsNullOrWhiteSpace(_options.PrivateKey))
                return Validate(Base64Url.Decode(_options.PublicKey), Base64Url.Decode(_options.PrivateKey));

            var path = string.IsNullOrWhiteSpace(_options.KeyFile) ? DefaultKeyFile() : _options.KeyFile!;
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                return Validate(Base64Url.Decode(doc.RootElement.GetProperty("publicKey").GetString()!),
                                Base64Url.Decode(doc.RootElement.GetProperty("privateKey").GetString()!));
            }

            var keys = VapidKeys.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // CreateNew: if another process won the race, use its file.
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(stream, new { publicKey = Base64Url.Encode(keys.PublicKey), privateKey = Base64Url.Encode(keys.PrivateKey), createdAt = DateTime.UtcNow });
                _logger?.LogInformation("Generated Web Push (VAPID) keys at {KeyFile}. Back this file up; changing the keys unsubscribes every browser.", path);
                return keys;
            }
            catch (IOException) when (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                return Validate(Base64Url.Decode(doc.RootElement.GetProperty("publicKey").GetString()!),
                                Base64Url.Decode(doc.RootElement.GetProperty("privateKey").GetString()!));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Web Push keys could not be loaded or created; push notifications are off.");
            return null;
        }
    }

    private static VapidKeys Validate(byte[] publicKey, byte[] privateKey)
    {
        if (publicKey.Length != 65 || publicKey[0] != 0x04 || privateKey.Length != 32) throw new CryptographicException("Invalid VAPID key pair.");
        return new VapidKeys { PublicKey = publicKey, PrivateKey = privateKey };
    }
}

/// <summary>Plan M4.3: sends one encrypted message to a push service.</summary>
public sealed class WebPushSender : IWebPushSender
{
    private readonly HttpClient _http;
    private readonly VapidKeyStore _keys;
    private readonly WebPushOptions _options;
    private readonly ILogger<WebPushSender>? _logger;

    public WebPushSender(HttpClient http, VapidKeyStore keys, IOptions<WebPushOptions> options, ILogger<WebPushSender>? logger = null)
    {
        _http = http;
        _keys = keys;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.Enabled && _keys.Keys is not null;

    public string? PublicKey => _keys.Keys is { } k ? Base64Url.Encode(k.PublicKey) : null;

    public async Task<WebPushSendResult> SendAsync(PushTarget target, byte[] payload, int ttlSeconds, CancellationToken ct = default)
    {
        var keys = _keys.Keys ?? throw new InvalidOperationException("Web Push keys are not available.");
        // The endpoint was validated when it was stored; check again before every request.
        if (PushNotificationService.ValidateEndpoint(target.Endpoint, _options.AllowedHosts) is { } error)
        {
            _logger?.LogWarning("Refusing to send a push message: {Reason}", error);
            return WebPushSendResult.Gone;
        }

        var endpoint = new Uri(target.Endpoint);
        var body = WebPushEncryption.Encrypt(Base64Url.Decode(target.P256dh), Base64Url.Decode(target.Auth), payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new ByteArrayContent(body) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Content.Headers.ContentEncoding.Add("aes128gcm");
        request.Headers.TryAddWithoutValidation("Authorization", keys.AuthorizationHeader(endpoint, _options.Subject, DateTimeOffset.UtcNow));
        request.Headers.TryAddWithoutValidation("TTL", Math.Clamp(ttlSeconds, 0, 2419200).ToString());
        request.Headers.TryAddWithoutValidation("Urgency", "normal");

        using var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return WebPushSendResult.Delivered;
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) return WebPushSendResult.Gone;
        _logger?.LogWarning("Push service {Host} answered {Status}.", endpoint.Host, (int)response.StatusCode);
        return WebPushSendResult.Failed;
    }
}
