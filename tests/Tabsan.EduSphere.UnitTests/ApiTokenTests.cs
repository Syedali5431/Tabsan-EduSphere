using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M14.3 — personal access tokens: who may create them, lifetime, hashing, caching, revocation and expiry.</summary>
public class ApiTokenTests
{
    private sealed class Repo : IApiTokenRepository
    {
        public readonly List<ApiToken> Tokens = [];
        public int HashLookups;
        public Task<ApiToken?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Tokens.FirstOrDefault(t => t.Id == id));
        public Task<ApiToken?> GetByHashAsync(string hash, CancellationToken ct = default) { HashLookups++; return Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == hash)); }
        public Task<IReadOnlyList<ApiToken>> GetForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ApiToken>>(Tokens.Where(t => t.UserId == userId).ToList());
        public Task<IReadOnlyList<ApiToken>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ApiToken>>(Tokens);
        public Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.ToDictionary(id => id, id => "user-" + id.ToString()[..4]));
        public void Add(ApiToken token) => Tokens.Add(token);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class Audit : IAuditService
    {
        public readonly List<string> Actions = [];
        public Task LogAsync(AuditLog entry, CancellationToken ct = default) { Actions.Add(entry.Action); return Task.CompletedTask; }
        public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(string? query = null, Guid? actorUserId = null, string? action = null,
            string? entityName = null, DateTime? fromUtc = null, DateTime? toUtc = null, int page = 1, int pageSize = 50, string? actorRole = null,
            string? severity = null, string? eventCategory = null, string? correlationId = null, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<AuditLog>, int)>(([], 0));
    }

    private static (ApiTokenService Service, Repo Repo, Audit Audit, Func<DateTime> Clock, Action<TimeSpan> Advance) Create(ApiTokenOptions? options = null)
    {
        var now = new DateTime(2026, 10, 23, 8, 0, 0, DateTimeKind.Utc);
        var repo = new Repo();
        var audit = new Audit();
        var service = new ApiTokenService(repo, audit, new MemoryCache(new MemoryCacheOptions()), Options.Create(options ?? new ApiTokenOptions()), () => now);
        return (service, repo, audit, () => now, d => now = now.Add(d));
    }

    [Fact]
    public async Task Admins_CreateTokens_ThatAreStoredOnlyAsAHash()
    {
        var (service, repo, audit, _, _) = Create();
        var user = Guid.NewGuid();
        var result = await service.CreateAsync(user, "Admin", new CreateApiTokenRequest(" SIS sync ", 30, ReadOnly: true));
        Assert.Equal(201, result.Status);
        var value = result.Value!.Value;
        Assert.Matches("^esk_[A-Za-z0-9_-]{43}$", value);
        var stored = repo.Tokens.Single();
        Assert.Equal("SIS sync", stored.Name);
        Assert.Equal(ApiTokenService.Hash(value), stored.TokenHash);
        Assert.DoesNotContain(value, stored.TokenHash);
        Assert.Equal(value[..12], stored.Hint);
        Assert.True(stored.ReadOnly);
        Assert.Contains("ApiTokenCreated", audit.Actions);

        Assert.Equal(403, (await service.CreateAsync(user, "Student", new CreateApiTokenRequest("x", 5, false))).Status);
        Assert.Equal(400, (await service.CreateAsync(user, "Admin", new CreateApiTokenRequest("x", null, false))).Status);
        Assert.Equal(400, (await service.CreateAsync(user, "Admin", new CreateApiTokenRequest("x", 366, false))).Status);
        Assert.Equal(400, (await service.CreateAsync(user, "Admin", new CreateApiTokenRequest("x", 0, false))).Status);
        Assert.Equal(400, (await service.CreateAsync(user, "Admin", new CreateApiTokenRequest("", 5, false))).Status);
    }

    [Fact]
    public async Task Tokens_Authenticate_AreCached_AndStopWhenRevokedOrExpired()
    {
        var (service, repo, _, _, advance) = Create();
        var user = Guid.NewGuid();
        var value = (await service.CreateAsync(user, "SuperAdmin", new CreateApiTokenRequest("Reports", 2, false))).Value!.Value;

        var identity = await service.AuthenticateAsync(value, "10.0.0.5");
        Assert.Equal((user, false), (identity!.UserId, identity.ReadOnly));
        Assert.Equal("10.0.0.5", repo.Tokens.Single().LastUsedIp);
        await service.AuthenticateAsync(value, "10.0.0.5");
        Assert.Equal(1, repo.HashLookups); // cached

        // Unknown and malformed tokens: refused (and the miss is cached too).
        Assert.Null(await service.AuthenticateAsync("esk_unknown", null));
        Assert.Null(await service.AuthenticateAsync("esk_unknown", null));
        Assert.Equal(2, repo.HashLookups);
        Assert.Null(await service.AuthenticateAsync("not-a-token", null));
        Assert.Null(await service.AuthenticateAsync("esk_" + new string('a', 200), null));

        Assert.True(await service.RevokeAsync(repo.Tokens.Single().Id, user, administrator: false));
        Assert.Null(await service.AuthenticateAsync(value, null)); // the cache entry went with the revocation

        var second = (await service.CreateAsync(user, "SuperAdmin", new CreateApiTokenRequest("Short", 1, false))).Value!.Value;
        Assert.NotNull(await service.AuthenticateAsync(second, null));
        advance(TimeSpan.FromDays(1).Add(TimeSpan.FromMinutes(2)));
        Assert.Null(await service.AuthenticateAsync(second, null)); // expired (the cache never outlives the token)
    }

    [Fact]
    public async Task Owners_RevokeTheirOwn_AdministratorsAny_AndUsersHaveACap()
    {
        var (service, repo, _, _, _) = Create(new ApiTokenOptions { MaxTokensPerUser = 2, AllowedRoles = ["Admin", "Faculty"] });
        var owner = Guid.NewGuid();
        await service.CreateAsync(owner, "Faculty", new CreateApiTokenRequest("a", 10, false));
        await service.CreateAsync(owner, "Faculty", new CreateApiTokenRequest("b", 10, false));
        Assert.Equal(409, (await service.CreateAsync(owner, "Faculty", new CreateApiTokenRequest("c", 10, false))).Status);

        var id = repo.Tokens[0].Id;
        Assert.False(await service.RevokeAsync(id, Guid.NewGuid(), administrator: false));
        Assert.True(await service.RevokeAsync(id, Guid.NewGuid(), administrator: true));
        Assert.Equal(201, (await service.CreateAsync(owner, "Faculty", new CreateApiTokenRequest("c", 10, false))).Status); // a slot is free again

        var mine = await service.MineAsync(owner);
        Assert.Equal(3, mine.Count);
        Assert.Single(mine, t => !t.Active);
        Assert.All(await service.AllAsync(), t => Assert.StartsWith("user-", t.UserName));
    }
}
