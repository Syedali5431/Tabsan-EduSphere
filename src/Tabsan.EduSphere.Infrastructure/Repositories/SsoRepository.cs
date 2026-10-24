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

    public async Task<IReadOnlyList<Guid>> FindUserIdsByInstitutionIdAsync(Guid? tenantId, string institutionId, CancellationToken ct = default)
    {
        var id = institutionId.Trim();
        var students = await (
                from p in _db.StudentProfiles.AsNoTracking()
                join u in _db.Users.AsNoTracking() on p.UserId equals u.Id
                where p.RegistrationNumber == id && (tenantId == null || u.TenantId == tenantId)
                select p.UserId)
            .ToListAsync(ct);
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.Username == id && (tenantId == null || u.TenantId == tenantId))
            .Select(u => u.Id)
            .ToListAsync(ct);
        return students.Concat(users).Distinct().ToList();
    }

    public async Task<IReadOnlyList<SsoProviderSetting>> GetEnforcingSettingsAsync(Guid? tenantId, CancellationToken ct = default)
        => await _db.SsoProviderSettings.AsNoTracking()
            .Where(s => s.IsEnabled && s.EnforceSsoOnly && (s.TenantId == null || s.TenantId == tenantId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ExternalLogin>> GetLinksForUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.ExternalLogins.Where(l => l.UserId == userId).OrderBy(l => l.Provider).ToListAsync(ct);

    public void RemoveLink(ExternalLogin link) => _db.ExternalLogins.Remove(link);

    public async Task<IReadOnlyList<(SsoProviderSetting Setting, string? TenantCode)>> GetAllSettingsAsync(CancellationToken ct = default)
    {
        var rows = await (
                from s in _db.SsoProviderSettings.AsNoTracking()
                join t in _db.Tenants.AsNoTracking() on s.TenantId equals (Guid?)t.Id into tenants
                from t in tenants.DefaultIfEmpty()
                orderby s.TenantId, s.Provider
                select new { Setting = s, TenantCode = t == null ? null : t.Code })
            .ToListAsync(ct);
        return rows.Select(r => (r.Setting, r.TenantCode)).ToList();
    }

    public async Task AddSettingAsync(SsoProviderSetting setting, CancellationToken ct = default)
        => await _db.SsoProviderSettings.AddAsync(setting, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
