using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M14.3 — personal access tokens.</summary>
public sealed class ApiTokenRepository(ApplicationDbContext db) : IApiTokenRepository
{
    public Task<ApiToken?> GetAsync(Guid id, CancellationToken ct = default) => db.ApiTokens.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<ApiToken?> GetByHashAsync(string hash, CancellationToken ct = default) => db.ApiTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task<IReadOnlyList<ApiToken>> GetForUserAsync(Guid userId, CancellationToken ct = default)
        => await db.ApiTokens.Where(t => t.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<ApiToken>> GetAllAsync(CancellationToken ct = default) => await db.ApiTokens.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
        => await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username, ct);

    public void Add(ApiToken token) => db.ApiTokens.Add(token);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

internal sealed class ApiTokenConfiguration : IEntityTypeConfiguration<ApiToken>
{
    public void Configure(EntityTypeBuilder<ApiToken> b)
    {
        b.ToTable("api_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(120);
        b.Property(x => x.Hint).IsRequired().HasMaxLength(16);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        b.Property(x => x.LastUsedIp).HasMaxLength(45);
        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("IX_api_tokens_hash");
        b.HasIndex(x => x.UserId).HasDatabaseName("IX_api_tokens_user");
    }
}
