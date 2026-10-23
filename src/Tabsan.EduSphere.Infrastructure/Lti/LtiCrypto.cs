using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tabsan.EduSphere.Application.Lti;
using Tabsan.EduSphere.Application.Notifications;

namespace Tabsan.EduSphere.Infrastructure.Lti;

/// <summary>
/// Plan M5.1: the platform's RS256 signing keys, kept outside the repository (<c>Lti:KeyFile</c>, default
/// &lt;app-data&gt;/Tabsan/lti-platform-keys.json) and created on first use. Rotation adds a new active key; the old one
/// stays in the JWKS for <see cref="LtiOptions.RetiredKeyDays"/> so tokens signed just before rotation still verify.
/// All nodes must share the file.
/// </summary>
public sealed class LtiPlatformKeyStore : ILtiPlatformKeys
{
    private readonly LtiOptions _options;
    private readonly ILogger<LtiPlatformKeyStore>? _logger;
    private readonly object _gate = new();
    private List<StoredKey>? _keys;

    public LtiPlatformKeyStore(IOptions<LtiOptions> options, ILogger<LtiPlatformKeyStore>? logger = null)
    {
        _options = options.Value;
        _logger = logger;
    }

    private sealed class StoredKey
    {
        public string Kid { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string N { get; set; } = default!;
        public string E { get; set; } = default!;
        public string D { get; set; } = default!;
        public string P { get; set; } = default!;
        public string Q { get; set; } = default!;
        public string DP { get; set; } = default!;
        public string DQ { get; set; } = default!;
        public string QI { get; set; } = default!;

        public RSAParameters Parameters(bool includePrivate) => new()
        {
            Modulus = Base64Url.Decode(N), Exponent = Base64Url.Decode(E),
            D = includePrivate ? Base64Url.Decode(D) : null, P = includePrivate ? Base64Url.Decode(P) : null, Q = includePrivate ? Base64Url.Decode(Q) : null,
            DP = includePrivate ? Base64Url.Decode(DP) : null, DQ = includePrivate ? Base64Url.Decode(DQ) : null, InverseQ = includePrivate ? Base64Url.Decode(QI) : null
        };

        public static StoredKey Generate()
        {
            using var rsa = RSA.Create(2048);
            var p = rsa.ExportParameters(true);
            return new StoredKey
            {
                Kid = Base64Url.Encode(RandomNumberGenerator.GetBytes(12)), CreatedAt = DateTime.UtcNow,
                N = Base64Url.Encode(p.Modulus), E = Base64Url.Encode(p.Exponent), D = Base64Url.Encode(p.D), P = Base64Url.Encode(p.P),
                Q = Base64Url.Encode(p.Q), DP = Base64Url.Encode(p.DP), DQ = Base64Url.Encode(p.DQ), QI = Base64Url.Encode(p.InverseQ)
            };
        }
    }

    public static string DefaultKeyFile()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Tabsan", "lti-platform-keys.json");

    private string KeyFile => string.IsNullOrWhiteSpace(_options.KeyFile) ? DefaultKeyFile() : _options.KeyFile!;

    private List<StoredKey> Load()
    {
        lock (_gate)
        {
            if (_keys is not null) return _keys;
            if (File.Exists(KeyFile))
            {
                _keys = JsonSerializer.Deserialize<List<StoredKey>>(File.ReadAllText(KeyFile)) ?? new();
            }
            if (_keys is null || _keys.Count == 0 || _keys.All(k => k.RetiredAt is not null))
            {
                _keys ??= new();
                _keys.Add(StoredKey.Generate());
                Save(_keys);
                _logger?.LogInformation("Generated an LTI platform signing key in {KeyFile}. Back this file up.", KeyFile);
            }
            return _keys;
        }
    }

    private void Save(List<StoredKey> keys)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(KeyFile)!);
        var temp = KeyFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(keys));
        File.Move(temp, KeyFile, overwrite: true);
    }

    private StoredKey Active => Load().Where(k => k.RetiredAt is null).OrderByDescending(k => k.CreatedAt).First();

    public string Sign(string payloadJson)
    {
        var key = Active;
        var header = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT", kid = key.Kid }));
        var payload = Base64Url.Encode(Encoding.UTF8.GetBytes(payloadJson));
        using var rsa = RSA.Create(key.Parameters(true));
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(header + "." + payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{header}.{payload}.{Base64Url.Encode(signature)}";
    }

    public string JwksJson()
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(0, _options.RetiredKeyDays));
        var keys = Load().Where(k => k.RetiredAt is null || k.RetiredAt > cutoff)
            .Select(k => new { kty = "RSA", alg = "RS256", use = "sig", kid = k.Kid, n = k.N, e = k.E });
        return JsonSerializer.Serialize(new { keys });
    }

    public IReadOnlyList<LtiKeyInfo> Keys
    {
        get
        {
            var active = Active.Kid;
            return Load().OrderByDescending(k => k.CreatedAt).Select(k => new LtiKeyInfo(k.Kid, k.CreatedAt, k.RetiredAt, k.Kid == active)).ToList();
        }
    }

    public void Rotate()
    {
        lock (_gate)
        {
            var keys = Load();
            var now = DateTime.UtcNow;
            foreach (var k in keys.Where(k => k.RetiredAt is null)) k.RetiredAt = now;
            keys.Add(StoredKey.Generate());
            // Drop keys retired long ago.
            keys.RemoveAll(k => k.RetiredAt is { } r && r < now.AddDays(-Math.Max(0, _options.RetiredKeyDays) - 30));
            Save(keys);
        }
    }
}

/// <summary>
/// Plan M5: validates RS256 JWTs signed by a tool, with the keys from its JWKS URL (https only, cached 10 minutes)
/// or its registered PEM public key.
/// </summary>
public sealed class LtiToolJwtValidator : ILtiToolJwtValidator
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LtiToolJwtValidator>? _logger;

    public LtiToolJwtValidator(HttpClient http, IMemoryCache cache, ILogger<LtiToolJwtValidator>? logger = null)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(string? PayloadJson, string? Error)> ValidateAsync(string jwt, string? jwksUrl, string? publicKeyPem, string expectedIssuer,
        IReadOnlyCollection<string> validAudiences, CancellationToken ct = default)
    {
        IList<SecurityKey> keys;
        try { keys = await KeysAsync(jwksUrl, publicKeyPem, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogWarning(ex, "Could not load the keys of LTI tool {Issuer}.", expectedIssuer);
            return (null, "The tool's public keys could not be loaded.");
        }
        if (keys.Count == 0) return (null, "The tool has no public key registered.");

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            ValidIssuer = expectedIssuer,
            ValidAudiences = validAudiences,
            IssuerSigningKeys = keys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            TryAllIssuerSigningKeys = true
        });
        if (!result.IsValid) return (null, "The tool's token is not valid: " + (result.Exception?.Message ?? "validation failed"));
        var token = (JsonWebToken)result.SecurityToken;
        return (Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(token.EncodedPayload)), null);
    }

    private async Task<IList<SecurityKey>> KeysAsync(string? jwksUrl, string? pem, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(pem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return [new RsaSecurityKey(rsa)];
        }
        if (string.IsNullOrWhiteSpace(jwksUrl)) return [];
        if (!Uri.TryCreate(jwksUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback))
            throw new InvalidOperationException("The tool's JWKS URL must be https.");
        return (await _cache.GetOrCreateAsync("lti-jwks:" + jwksUrl, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            using var response = await _http.GetAsync(uri, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            if (json.Length > 100_000) throw new InvalidOperationException("The tool's JWKS is too large.");
            return new JsonWebKeySet(json).GetSigningKeys();
        }))!;
    }
}
