using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M1: EF-backed store for SSO provider settings and external login links.</summary>
public sealed class SsoRepository : ISsoRepository
{
    private readonly ApplicationDbContext _db;

    public SsoRepository(ApplicationDbContext db) => _db = db;

    public async Task<Guid?> GetTenantIdByCodeAsync(string tenantCode, CancellationToken ct = default)
    {
        var code = tenantCode.Trim().ToUpperInvariant();
        return await _db.Tenants.AsNoTracking()
            .Where(t => t.Code == code && t.IsActive)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);
    }

    public Task<SsoProviderSetting?> GetSettingAsync(Guid? tenantId, SsoProvider provider, CancellationToken ct = default)
        => _db.SsoProviderSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Provider == provider, ct);

    public async Task<IReadOnlyList<SsoProviderSetting>> GetEnabledSettingsAsync(Guid? tenantId, CancellationToken ct = default)
        => await _db.SsoProviderSettings.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsEnabled)
            .ToListAsync(ct);

    public Task<ExternalLogin?> FindLinkAsync(Guid? tenantId, SsoProvider provider, string externalSubjectId, CancellationToken ct = default)
        => _db.ExternalLogins.FirstOrDefaultAsync(
            l => l.TenantId == tenantId && l.Provider == provider && l.ExternalSubjectId == externalSubjectId, ct);

    public Task<bool> UserHasLinkAsync(Guid userId, SsoProvider provider, CancellationToken ct = default)
        => _db.ExternalLogins.AnyAsync(l => l.UserId == userId && l.Provider == provider, ct);

    public async Task AddLinkAsync(ExternalLogin link, CancellationToken ct = default)
        => await _db.ExternalLogins.AddAsync(link, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
