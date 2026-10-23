using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Auth;

// Plan M14.3 — personal access tokens for integrations with the public REST API.

/// <summary>Settings (section "ApiTokens").</summary>
public sealed class ApiTokenOptions
{
    public const string SectionName = "ApiTokens";

    /// <summary>Roles that may create tokens for themselves.</summary>
    public string[] AllowedRoles { get; set; } = ["SuperAdmin", "Admin"];

    /// <summary>Longest lifetime a token may have (days); 0 = tokens may be created without an expiry.</summary>
    public int MaxLifetimeDays { get; set; } = 365;

    /// <summary>Most active tokens one user may hold.</summary>
    public int MaxTokensPerUser { get; set; } = 20;
}

public sealed record CreateApiTokenRequest(string Name, int? ExpiresInDays, bool ReadOnly);

public sealed record ApiTokenResponse(Guid Id, Guid UserId, string? UserName, string Name, string Hint, bool ReadOnly, DateTime CreatedAt,
    DateTime? ExpiresAt, DateTime? LastUsedAt, string? LastUsedIp, DateTime? RevokedAt, bool Active);

/// <summary>Returned once, when the token is created.</summary>
public sealed record ApiTokenCreated(ApiTokenResponse Token, string Value);

/// <summary>What a valid token stands for (used to build the request's identity).</summary>
public sealed record ApiTokenIdentity(Guid TokenId, Guid UserId, bool ReadOnly, DateTime? ExpiresAt = null);

public interface IApiTokenRepository
{
    Task<ApiToken?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ApiToken?> GetByHashAsync(string hash, CancellationToken ct = default);
    Task<IReadOnlyList<ApiToken>> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ApiToken>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    void Add(ApiToken token);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public sealed class ApiTokenResult<T>
{
    public bool Ok { get; private init; }
    public int Status { get; private init; }
    public string? Error { get; private init; }
    public T? Value { get; private init; }
    public static ApiTokenResult<T> Success(T value, int status = 200) => new() { Ok = true, Status = status, Value = value };
    public static ApiTokenResult<T> Fail(int status, string error) => new() { Status = status, Error = error };
}

public sealed class ApiTokenService
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(60);
    private readonly IApiTokenRepository _repo;
    private readonly IAuditService _audit;
    private readonly IMemoryCache _cache;
    private readonly ApiTokenOptions _options;
    private readonly Func<DateTime> _now;

    public ApiTokenService(IApiTokenRepository repo, IAuditService audit, IMemoryCache cache, IOptions<ApiTokenOptions> options)
        : this(repo, audit, cache, options, () => DateTime.UtcNow) { }

    public ApiTokenService(IApiTokenRepository repo, IAuditService audit, IMemoryCache cache, IOptions<ApiTokenOptions> options, Func<DateTime> now)
    {
        _repo = repo;
        _audit = audit;
        _cache = cache;
        _options = options.Value;
        _now = now;
    }

    public bool MayCreate(string? role) => role is not null && _options.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public async Task<ApiTokenResult<ApiTokenCreated>> CreateAsync(Guid userId, string? role, CreateApiTokenRequest r, CancellationToken ct = default)
    {
        if (!MayCreate(role)) return ApiTokenResult<ApiTokenCreated>.Fail(403, "Your role cannot create API tokens.");
        if (string.IsNullOrWhiteSpace(r.Name)) return ApiTokenResult<ApiTokenCreated>.Fail(400, "Enter a name for the token.");
        if (r.ExpiresInDays is < 1) return ApiTokenResult<ApiTokenCreated>.Fail(400, "A token must be valid for at least one day.");
        if (_options.MaxLifetimeDays > 0 && (r.ExpiresInDays is null || r.ExpiresInDays > _options.MaxLifetimeDays))
            return ApiTokenResult<ApiTokenCreated>.Fail(400, $"Choose an expiry of at most {_options.MaxLifetimeDays} days.");
        var mine = await _repo.GetForUserAsync(userId, ct);
        if (mine.Count(t => t.IsActive(_now())) >= _options.MaxTokensPerUser)
            return ApiTokenResult<ApiTokenCreated>.Fail(409, $"You already have {_options.MaxTokensPerUser} active tokens. Revoke one first.");

        var value = ApiToken.Prefix + Base64Url(RandomNumberGenerator.GetBytes(32));
        var token = new ApiToken(userId, r.Name, value[..12], Hash(value), r.ReadOnly, r.ExpiresInDays is { } days ? _now().AddDays(days) : null);
        _repo.Add(token);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ApiTokenCreated", "ApiToken", token.Id.ToString(), actorUserId: userId,
            newValuesJson: $"{{\"name\":{System.Text.Json.JsonSerializer.Serialize(token.Name)},\"readOnly\":{(token.ReadOnly ? "true" : "false")}}}"), ct);
        return ApiTokenResult<ApiTokenCreated>.Success(new ApiTokenCreated(ToResponse(token, null), value), 201);
    }

    public async Task<IReadOnlyList<ApiTokenResponse>> MineAsync(Guid userId, CancellationToken ct = default)
        => (await _repo.GetForUserAsync(userId, ct)).OrderByDescending(t => t.CreatedAt).Select(t => ToResponse(t, null)).ToList();

    public async Task<IReadOnlyList<ApiTokenResponse>> AllAsync(CancellationToken ct = default)
    {
        var tokens = await _repo.GetAllAsync(ct);
        var names = await _repo.GetUserNamesAsync(tokens.Select(t => t.UserId).Distinct().ToList(), ct);
        return tokens.OrderByDescending(t => t.CreatedAt).Select(t => ToResponse(t, names.GetValueOrDefault(t.UserId))).ToList();
    }

    /// <summary>Revokes a token: the owner's own, or any token for administrators.</summary>
    public async Task<bool> RevokeAsync(Guid tokenId, Guid actorUserId, bool administrator, CancellationToken ct = default)
    {
        var token = await _repo.GetAsync(tokenId, ct);
        if (token is null || (!administrator && token.UserId != actorUserId)) return false;
        token.Revoke(actorUserId, _now());
        await _repo.SaveChangesAsync(ct);
        _cache.Remove(CacheKey(token.TokenHash));
        await _audit.LogAsync(new AuditLog("ApiTokenRevoked", "ApiToken", token.Id.ToString(), actorUserId: actorUserId), ct);
        return true;
    }

    /// <summary>The identity behind a presented token; null when unknown, revoked or expired. Results are cached briefly.</summary>
    public async Task<ApiTokenIdentity?> AuthenticateAsync(string presented, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(presented) || !presented.StartsWith(ApiToken.Prefix, StringComparison.Ordinal) || presented.Length > 100) return null;
        var hash = Hash(presented);
        if (_cache.TryGetValue(CacheKey(hash), out ApiTokenIdentity? cached))
        {
            if (cached?.ExpiresAt is not { } until || until > _now()) return cached;
            _cache.Remove(CacheKey(hash));
            return null;
        }

        var token = await _repo.GetByHashAsync(hash, ct);
        var now = _now();
        ApiTokenIdentity? identity = token is not null && token.IsActive(now) ? new ApiTokenIdentity(token.Id, token.UserId, token.ReadOnly, token.ExpiresAt) : null;
        if (token is not null && identity is not null && (token.LastUsedAt is null || token.LastUsedAt < now.AddMinutes(-1)))
        {
            token.Used(now, ip);
            await _repo.SaveChangesAsync(ct);
        }
        var lifetime = token?.ExpiresAt is { } expires && expires - now < CacheFor ? expires - now : CacheFor;
        if (lifetime > TimeSpan.Zero) _cache.Set(CacheKey(hash), identity, lifetime);
        return identity;
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string CacheKey(string hash) => "api-token:" + hash;

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private ApiTokenResponse ToResponse(ApiToken t, string? userName)
        => new(t.Id, t.UserId, userName, t.Name, t.Hint + "…", t.ReadOnly, t.CreatedAt, t.ExpiresAt, t.LastUsedAt, t.LastUsedIp, t.RevokedAt, t.IsActive(_now()));
}
