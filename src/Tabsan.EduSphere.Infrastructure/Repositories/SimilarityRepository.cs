using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Similarity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M9 — similarity settings, checks (the queue), extracted documents and fingerprints.</summary>
public sealed class SimilarityRepository : ISimilarityRepository
{
    private readonly ApplicationDbContext _db;

    public SimilarityRepository(ApplicationDbContext db) => _db = db;

    public Task<SimilaritySetting?> GetSettingAsync(Guid? tenantId, CancellationToken ct = default)
        => _db.SimilaritySettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    public void Add(SimilaritySetting setting) => _db.SimilaritySettings.Add(setting);

    public Task<Assignment?> GetAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
        => _db.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

    public Task<Guid?> GetOfferingTenantAsync(Guid offeringId, CancellationToken ct = default)
        => _db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => o.Id == offeringId).Select(o => o.TenantId).FirstOrDefaultAsync(ct);

    public async Task<(Guid? TenantId, Guid? CampusId)?> GetOfferingScopeAsync(Guid offeringId, CancellationToken ct = default)
    {
        var o = await _db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == offeringId)
            .Select(x => new { x.TenantId, x.CampusId }).FirstOrDefaultAsync(ct);
        return o is null ? null : (o.TenantId, o.CampusId);
    }

    private IQueryable<SimilarityCandidate> Candidates(IQueryable<AssignmentSubmission> submissions)
        => from s in submissions
           join a in _db.Assignments.IgnoreQueryFilters() on s.AssignmentId equals a.Id
           join o in _db.CourseOfferings.IgnoreQueryFilters() on a.CourseOfferingId equals o.Id
           select new SimilarityCandidate(s.Id, s.AssignmentId, o.Id, o.TenantId, s.StudentProfileId, s.GroupId, s.CurrentAttemptNumber, s.SubmittedAt);

    public async Task<IReadOnlyList<SimilarityCandidate>> GetUncheckedSubmissionsAsync(int take, CancellationToken ct = default)
    {
        var enabled = _db.Assignments.Where(a => a.SimilarityCheckEnabled).Select(a => a.Id);
        var due = _db.AssignmentSubmissions.AsNoTracking().Where(s =>
            enabled.Contains(s.AssignmentId) && s.CurrentAttemptNumber > 0 && !s.IsMissing && s.Status != SubmissionStatus.Rejected
            && !_db.SimilarityChecks.Any(c => c.SubmissionId == s.Id && c.AttemptNumber == s.CurrentAttemptNumber)
            // Group work: one check per hand-in, made on any member's submission.
            && (s.GroupId == null || !_db.AssignmentSubmissions.Any(m => m.AssignmentId == s.AssignmentId && m.GroupId == s.GroupId && m.Id != s.Id
                && _db.SimilarityChecks.Any(c => c.SubmissionId == m.Id && c.CreatedAt >= s.SubmittedAt))));
        return await Candidates(due.OrderBy(s => s.SubmittedAt).Take(take)).ToListAsync(ct);
    }

    public Task<SimilarityCandidate?> GetSubmissionAsync(Guid submissionId, CancellationToken ct = default)
        => Candidates(_db.AssignmentSubmissions.AsNoTracking().Where(s => s.Id == submissionId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SimilarityCandidate>> GetAssignmentSubmissionsAsync(Guid assignmentId, CancellationToken ct = default)
        => await Candidates(_db.AssignmentSubmissions.AsNoTracking().Where(s => s.AssignmentId == assignmentId)).ToListAsync(ct);

    public async Task<(string? Text, IReadOnlyList<SimilarityAttemptFile> Files)?> GetAttemptContentAsync(Guid submissionId, int attemptNumber, CancellationToken ct = default)
    {
        var attempt = await _db.SubmissionAttempts.AsNoTracking().Where(a => a.SubmissionId == submissionId && a.AttemptNumber == attemptNumber)
            .Select(a => new { a.Id, a.TextContent }).FirstOrDefaultAsync(ct);
        if (attempt is null) return null;
        var files = await _db.SubmissionFiles.AsNoTracking().Where(f => f.AttemptId == attempt.Id && f.StorageKey != null)
            .Select(f => new SimilarityAttemptFile(f.FileName, f.ContentType, f.StorageKey!)).ToListAsync(ct);
        return (attempt.TextContent, files);
    }

    public async Task<IReadOnlyList<SimilaritySubmissionLabel>> GetSubmissionLabelsAsync(IReadOnlyCollection<Guid> submissionIds, CancellationToken ct = default)
    {
        if (submissionIds.Count == 0) return [];
        var rows = await (from s in _db.AssignmentSubmissions.IgnoreQueryFilters().AsNoTracking()
                          join a in _db.Assignments.IgnoreQueryFilters() on s.AssignmentId equals a.Id
                          join sp in _db.StudentProfiles.IgnoreQueryFilters() on s.StudentProfileId equals sp.Id
                          join u in _db.Users.IgnoreQueryFilters() on sp.UserId equals u.Id
                          where submissionIds.Contains(s.Id)
                          select new { s.Id, Name = u.FullName ?? u.Username, a.Title, a.AnonymousGrading, a.NamesRevealedAt }).ToListAsync(ct);
        return rows.Select(r => new SimilaritySubmissionLabel(r.Id, r.Name, r.Title, r.AnonymousGrading && r.NamesRevealedAt == null)).ToList();
    }

    public async Task<IReadOnlyList<SimilarityCheck>> GetDueChecksAsync(DateTime now, int take, CancellationToken ct = default)
        => await _db.SimilarityChecks.Where(c => c.Status == SimilarityCheckStatus.Queued && (c.NextAttemptAt == null || c.NextAttemptAt <= now))
            .OrderBy(c => c.CreatedAt).Take(take).ToListAsync(ct);

    public Task<SimilarityCheck?> GetCheckAsync(Guid checkId, CancellationToken ct = default)
        => _db.SimilarityChecks.FirstOrDefaultAsync(c => c.Id == checkId, ct);

    public async Task<IReadOnlyList<SimilarityCheck>> GetChecksForSubmissionAsync(Guid submissionId, CancellationToken ct = default)
        => await _db.SimilarityChecks.Where(c => c.SubmissionId == submissionId).ToListAsync(ct);

    public async Task<IReadOnlyList<SimilarityCheck>> GetChecksForAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
        => await _db.SimilarityChecks.AsNoTracking().Where(c => c.AssignmentId == assignmentId).ToListAsync(ct);

    public Task<SimilarityDocument?> GetDocumentAsync(Guid submissionId, int attemptNumber, CancellationToken ct = default)
        => _db.SimilarityDocuments.FirstOrDefaultAsync(d => d.SubmissionId == submissionId && d.AttemptNumber == attemptNumber, ct);

    public async Task<IReadOnlyList<SimilarityDocument>> GetDocumentsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken ct = default)
        => documentIds.Count == 0 ? [] : await _db.SimilarityDocuments.AsNoTracking().Where(d => documentIds.Contains(d.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<SimilarityFingerprint>> GetFingerprintsAsync(Guid documentId, CancellationToken ct = default)
        => await _db.SimilarityFingerprints.AsNoTracking().Where(f => f.DocumentId == documentId).ToListAsync(ct);

    public async Task<IReadOnlyList<SimilarityMatch>> FindMatchesAsync(IReadOnlyCollection<long> hashes, SimilarityScope scope, SimilarityDocument document, CancellationToken ct = default)
    {
        if (hashes.Count == 0) return [];
        var docs = _db.SimilarityDocuments.AsNoTracking().Where(d => d.Id != document.Id && d.SubmissionId != document.SubmissionId
            && d.StudentProfileId != document.StudentProfileId
            && !(document.GroupId != null && d.GroupId == document.GroupId && d.AssignmentId == document.AssignmentId));
        docs = scope switch
        {
            SimilarityScope.Assignment => docs.Where(d => d.AssignmentId == document.AssignmentId),
            SimilarityScope.Course => docs.Where(d => d.CourseOfferingId == document.CourseOfferingId),
            _ => docs.Where(d => d.TenantId == document.TenantId)
        };
        var list = hashes.ToList();
        return await (from f in _db.SimilarityFingerprints.AsNoTracking()
                      join d in docs on f.DocumentId equals d.Id
                      where list.Contains(f.Hash)
                      select new SimilarityMatch(f.DocumentId, f.Position, f.Hash)).ToListAsync(ct);
    }

    public void Add(SimilarityCheck check) => _db.SimilarityChecks.Add(check);
    public void Add(SimilarityDocument document) => _db.SimilarityDocuments.Add(document);
    public void AddRange(IEnumerable<SimilarityFingerprint> fingerprints) => _db.SimilarityFingerprints.AddRange(fingerprints);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
