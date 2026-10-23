using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Lms.Scorm;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M13 — SCORM packages and attempts, xAPI credentials and statements.</summary>
public sealed class ScormRepository : IScormRepository
{
    private readonly ApplicationDbContext _db;

    public ScormRepository(ApplicationDbContext db) => _db = db;

    public Task<ScormPackage?> GetPackageAsync(Guid packageId, CancellationToken ct = default)
        => _db.ScormPackages.FirstOrDefaultAsync(p => p.Id == packageId, ct);

    public async Task<IReadOnlyList<ScormPackage>> GetPackagesAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.ScormPackages.Where(p => p.OfferingId == offeringId).OrderBy(p => p.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<ScormAttempt>> GetAttemptsAsync(Guid packageId, Guid? studentProfileId, CancellationToken ct = default)
        => await _db.ScormAttempts.Where(a => a.PackageId == packageId && (studentProfileId == null || a.StudentProfileId == studentProfileId))
            .OrderBy(a => a.Number).ToListAsync(ct);

    public async Task<IReadOnlyList<ScormAttempt>> GetAttemptsForStudentAsync(IReadOnlyCollection<Guid> packageIds, Guid studentProfileId, CancellationToken ct = default)
        => packageIds.Count == 0 ? []
            : await _db.ScormAttempts.AsNoTracking().Where(a => a.StudentProfileId == studentProfileId && packageIds.Contains(a.PackageId)).ToListAsync(ct);

    public Task<ScormAttempt?> GetAttemptAsync(Guid attemptId, CancellationToken ct = default)
        => _db.ScormAttempts.FirstOrDefaultAsync(a => a.Id == attemptId, ct);

    public Task<bool> IsEnrolledAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default)
        => _db.Enrollments.AnyAsync(e => e.CourseOfferingId == offeringId && e.StudentProfileId == studentProfileId && e.Status == EnrollmentStatus.Active, ct);

    public async Task<ScormLearnerInfo?> GetLearnerAsync(Guid studentProfileId, CancellationToken ct = default)
        => await (from p in _db.StudentProfiles.AsNoTracking()
                  join u in _db.Users.AsNoTracking() on p.UserId equals u.Id
                  where p.Id == studentProfileId
                  select new ScormLearnerInfo(p.Id, u.Id, u.FullName ?? u.Username, p.RegistrationNumber)).FirstOrDefaultAsync(ct);

    public Task<Guid?> GetStudentProfileIdAsync(Guid userId, CancellationToken ct = default)
        => _db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ScormLearnerInfo>> GetEnrolledLearnersAsync(Guid offeringId, CancellationToken ct = default)
        => await (from e in _db.Enrollments.AsNoTracking()
                  join p in _db.StudentProfiles.AsNoTracking() on e.StudentProfileId equals p.Id
                  join u in _db.Users.AsNoTracking() on p.UserId equals u.Id
                  where e.CourseOfferingId == offeringId && e.Status == EnrollmentStatus.Active
                  select new ScormLearnerInfo(p.Id, u.Id, u.FullName ?? u.Username, p.RegistrationNumber)).ToListAsync(ct);

    public async Task<(Guid? TenantId, Guid? CampusId)?> GetOfferingScopeAsync(Guid offeringId, CancellationToken ct = default)
    {
        var o = await _db.CourseOfferings.AsNoTracking().Where(x => x.Id == offeringId).Select(x => new { x.TenantId, x.CampusId }).FirstOrDefaultAsync(ct);
        return o is null ? null : (o.TenantId, o.CampusId);
    }

    public Task<XapiCredential?> GetCredentialByKeyAsync(string key, CancellationToken ct = default)
        => _db.XapiCredentials.FirstOrDefaultAsync(c => c.Key == key, ct);

    public Task<XapiCredential?> GetCredentialAsync(Guid id, CancellationToken ct = default)
        => _db.XapiCredentials.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<XapiCredential>> GetCredentialsAsync(CancellationToken ct = default)
        => await _db.XapiCredentials.AsNoTracking().ToListAsync(ct);

    public Task<XapiStatement?> GetStatementAsync(Guid statementId, CancellationToken ct = default)
        => _db.XapiStatements.FirstOrDefaultAsync(s => s.StatementId == statementId, ct);

    public async Task<IReadOnlyList<XapiStatement>> QueryStatementsAsync(XapiQuery q, CancellationToken ct = default)
    {
        var query = _db.XapiStatements.AsNoTracking().AsQueryable();
        if (!q.IncludeVoided) query = query.Where(s => !s.IsVoided);
        if (q.ActorKey is not null) query = query.Where(s => s.ActorKey == q.ActorKey);
        if (q.VerbId is not null) query = query.Where(s => s.VerbId == q.VerbId);
        if (q.ActivityId is not null) query = query.Where(s => s.ObjectId == q.ActivityId);
        if (q.Registration is { } r) query = query.Where(s => s.Registration == r);
        if (q.Since is { } since) query = query.Where(s => s.Stored > since);
        if (q.Until is { } until) query = query.Where(s => s.Stored <= until);
        if (q.CredentialId is { } c) query = query.Where(s => s.CredentialId == c);
        query = q.Ascending ? query.OrderBy(s => s.Stored).ThenBy(s => s.Id) : query.OrderByDescending(s => s.Stored).ThenByDescending(s => s.Id);
        return await query.Take(Math.Clamp(q.Limit, 1, 501)).ToListAsync(ct);
    }

    public void Add(ScormPackage package) => _db.ScormPackages.Add(package);
    public void Add(ScormAttempt attempt) => _db.ScormAttempts.Add(attempt);
    public void Add(XapiCredential credential) => _db.XapiCredentials.Add(credential);
    public void Add(XapiStatement statement) => _db.XapiStatements.Add(statement);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
