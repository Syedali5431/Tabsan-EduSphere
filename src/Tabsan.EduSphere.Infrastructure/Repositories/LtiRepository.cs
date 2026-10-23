using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lti;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M5 — EF implementation of <see cref="ILtiRepository"/>.</summary>
public sealed class LtiRepository : ILtiRepository
{
    private readonly ApplicationDbContext _db;

    public LtiRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<LtiTool>> GetToolsAsync(CancellationToken ct = default)
        => await _db.LtiTools.Include(t => t.Deployments).OrderBy(t => t.Name).ToListAsync(ct);

    public Task<LtiTool?> GetToolAsync(Guid toolId, CancellationToken ct = default)
        => _db.LtiTools.Include(t => t.Deployments).FirstOrDefaultAsync(t => t.Id == toolId, ct);

    public Task<LtiTool?> GetToolByClientIdAsync(string clientId, CancellationToken ct = default)
        => _db.LtiTools.Include(t => t.Deployments).FirstOrDefaultAsync(t => t.ClientId == clientId, ct);

    public Task<LtiToolDeployment?> GetDeploymentAsync(Guid deploymentRowId, CancellationToken ct = default)
        => _db.LtiToolDeployments.FirstOrDefaultAsync(d => d.Id == deploymentRowId, ct);

    public async Task<LtiToolDeployment?> FindDeploymentForOfferingAsync(Guid toolId, Guid offeringId, CancellationToken ct = default)
    {
        var departmentId = await DepartmentOfAsync(offeringId, ct);
        var candidates = await _db.LtiToolDeployments
            .Where(d => d.ToolId == toolId && d.IsActive
                && (d.Scope == LtiDeploymentScope.Installation
                    || (d.Scope == LtiDeploymentScope.CourseOffering && d.ScopeId == offeringId)
                    || (d.Scope == LtiDeploymentScope.Department && d.ScopeId == departmentId)))
            .ToListAsync(ct);
        return candidates.OrderByDescending(d => d.Scope).FirstOrDefault();
    }

    public async Task<IReadOnlyList<LtiTool>> GetToolsForOfferingAsync(Guid offeringId, CancellationToken ct = default)
    {
        var departmentId = await DepartmentOfAsync(offeringId, ct);
        return await _db.LtiTools.Include(t => t.Deployments)
            .Where(t => t.IsActive && t.Deployments.Any(d => d.IsActive
                && (d.Scope == LtiDeploymentScope.Installation
                    || (d.Scope == LtiDeploymentScope.CourseOffering && d.ScopeId == offeringId)
                    || (d.Scope == LtiDeploymentScope.Department && d.ScopeId == departmentId))))
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    private Task<Guid?> DepartmentOfAsync(Guid offeringId, CancellationToken ct)
        => _db.CourseOfferings.IgnoreQueryFilters().Where(o => o.Id == offeringId).Select(o => (Guid?)o.Course.DepartmentId).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<LtiResourceLink>> GetLinksForOfferingAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.LtiResourceLinks.Where(l => l.CourseOfferingId == offeringId).OrderBy(l => l.CreatedAt).ToListAsync(ct);

    public Task<LtiResourceLink?> GetLinkAsync(Guid linkId, CancellationToken ct = default)
        => _db.LtiResourceLinks.FirstOrDefaultAsync(l => l.Id == linkId, ct);

    public async Task<IReadOnlyList<LtiLineItem>> GetLineItemsAsync(Guid toolId, Guid offeringId, CancellationToken ct = default)
        => await _db.LtiLineItems.Where(l => l.ToolId == toolId && l.CourseOfferingId == offeringId).OrderBy(l => l.CreatedAt).ToListAsync(ct);

    public Task<LtiLineItem?> GetLineItemAsync(Guid lineItemId, CancellationToken ct = default)
        => _db.LtiLineItems.FirstOrDefaultAsync(l => l.Id == lineItemId, ct);

    public async Task<IReadOnlyList<LtiScore>> GetScoresAsync(Guid lineItemId, CancellationToken ct = default)
        => await _db.LtiScores.Where(s => s.LineItemId == lineItemId).OrderBy(s => s.Timestamp).ToListAsync(ct);

    public Task<LtiLaunch?> GetLaunchAsync(Guid launchId, CancellationToken ct = default)
        => _db.LtiLaunches.FirstOrDefaultAsync(l => l.Id == launchId, ct);

    public async Task<bool> TryUseNonceAsync(Guid toolId, string value, DateTime expiresAt, CancellationToken ct = default)
    {
        var trimmed = value.Length > 200 ? value[..200] : value;
        if (await _db.LtiUsedNonces.AnyAsync(n => n.ToolId == toolId && n.Value == trimmed, ct)) return false;
        var nonce = new LtiUsedNonce(toolId, trimmed, expiresAt);
        _db.LtiUsedNonces.Add(nonce);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique index: another request used it at the same moment.
            _db.Entry(nonce).State = EntityState.Detached;
            return false;
        }
    }

    public Task<LtiAccessToken?> GetAccessTokenAsync(string tokenHash, CancellationToken ct = default)
        => _db.LtiAccessTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<LtiOfferingContext?> GetOfferingContextAsync(Guid offeringId, CancellationToken ct = default)
        => _db.CourseOfferings.IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.Id == offeringId)
            .Select(o => new LtiOfferingContext(o.Id, o.Course.Code, o.Course.Title, o.Semester.Name, (Guid?)o.Course.DepartmentId, o.TenantId, o.CampusId))
            .FirstOrDefaultAsync(ct);

    public async Task<LtiPerson?> GetPersonAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await (from u in _db.Users.IgnoreQueryFilters().AsNoTracking()
                         where u.Id == userId
                         select new { u.Id, u.FullName, u.Email, Role = u.Role.Name, u.IsActive }).FirstOrDefaultAsync(ct);
        if (row is null) return null;
        var profile = await _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking().Where(sp => sp.UserId == userId).Select(sp => (Guid?)sp.Id).FirstOrDefaultAsync(ct);
        return new LtiPerson(row.Id, row.FullName, row.Email, row.Role, profile, row.IsActive);
    }

    public async Task<IReadOnlyList<(LtiPerson Person, bool IsInstructor)>> GetOfferingMembersAsync(Guid offeringId, CancellationToken ct = default)
    {
        var students = await (from e in _db.Enrollments.AsNoTracking()
                              join sp in _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking() on e.StudentProfileId equals sp.Id
                              join u in _db.Users.IgnoreQueryFilters().AsNoTracking() on sp.UserId equals u.Id
                              where e.CourseOfferingId == offeringId && e.Status == EnrollmentStatus.Active
                              select new LtiPerson(u.Id, u.FullName, u.Email, "Student", sp.Id, u.IsActive))
                             .ToListAsync(ct);
        var result = students.Select(s => (s, false)).ToList();
        var facultyId = await _db.CourseOfferings.IgnoreQueryFilters().Where(o => o.Id == offeringId).Select(o => o.FacultyUserId).FirstOrDefaultAsync(ct);
        if (facultyId is { } fid && await GetPersonAsync(fid, ct) is { } teacher) result.Add((teacher, true));
        return result;
    }

    public async Task PurgeExpiredAsync(DateTime now, CancellationToken ct = default)
    {
        await _db.LtiUsedNonces.Where(n => n.ExpiresAt < now).ExecuteDeleteAsync(ct);
        await _db.LtiAccessTokens.Where(t => t.ExpiresAt < now).ExecuteDeleteAsync(ct);
        var launchCutoff = now.AddDays(-1);
        await _db.LtiLaunches.Where(l => l.ExpiresAt < launchCutoff).ExecuteDeleteAsync(ct);
    }

    public void Add<T>(T entity) where T : BaseEntity
    {
        if (_db.Entry(entity).State == EntityState.Detached) _db.Add(entity);
    }

    public void Remove<T>(T entity) where T : BaseEntity => _db.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
