using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M3 — EF implementation of <see cref="ISubmissionWorkspaceRepository"/>.</summary>
public sealed class SubmissionWorkspaceRepository : ISubmissionWorkspaceRepository
{
    private readonly ApplicationDbContext _db;

    public SubmissionWorkspaceRepository(ApplicationDbContext db) => _db = db;

    public Task<Assignment?> GetAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
        => _db.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

    private IQueryable<AssignmentSubmission> Submissions(bool withHistory)
    {
        var query = _db.AssignmentSubmissions.Include(s => s.Assignment).AsQueryable();
        return withHistory
            ? query.Include(s => s.Attempts).ThenInclude(a => a.Files).AsSplitQuery()
            : query;
    }

    public Task<AssignmentSubmission?> GetSubmissionAsync(Guid submissionId, bool withHistory, CancellationToken ct = default)
        => Submissions(withHistory).FirstOrDefaultAsync(s => s.Id == submissionId, ct);

    public Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid studentProfileId, bool withHistory, CancellationToken ct = default)
        => Submissions(withHistory).FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentProfileId == studentProfileId, ct);

    public async Task<IReadOnlyList<AssignmentSubmission>> GetSubmissionsAsync(Guid assignmentId, bool withHistory, CancellationToken ct = default)
        => await Submissions(withHistory).Where(s => s.AssignmentId == assignmentId).ToListAsync(ct);

    public async Task<(SubmissionFile File, AssignmentSubmission Submission)?> GetFileAsync(Guid fileId, CancellationToken ct = default)
    {
        var file = await _db.SubmissionFiles.FirstOrDefaultAsync(f => f.Id == fileId, ct);
        if (file is null) return null;
        var submissionId = await _db.SubmissionAttempts.Where(a => a.Id == file.AttemptId).Select(a => (Guid?)a.SubmissionId).FirstOrDefaultAsync(ct);
        if (submissionId is null) return null;
        var submission = await Submissions(false).FirstOrDefaultAsync(s => s.Id == submissionId.Value, ct);
        return submission is null ? null : (file, submission);
    }

    public Task<SubmissionFile?> GetFileForUpdateAsync(Guid fileId, CancellationToken ct = default)
        => _db.SubmissionFiles.FirstOrDefaultAsync(f => f.Id == fileId, ct);

    public async Task<IReadOnlyList<SubmissionComment>> GetCommentsAsync(Guid submissionId, CancellationToken ct = default)
        => await _db.SubmissionComments.Where(c => c.SubmissionId == submissionId).OrderBy(c => c.CreatedAt).ToListAsync(ct);

    public Task<SubmissionComment?> GetCommentAsync(Guid commentId, CancellationToken ct = default)
        => _db.SubmissionComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);

    public async Task<IReadOnlyList<SubmissionAnnotation>> GetAnnotationsAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken ct = default)
        => fileIds.Count == 0 ? [] : await _db.SubmissionAnnotations.Where(a => fileIds.Contains(a.SubmissionFileId)).ToListAsync(ct);

    public Task<SubmissionAnnotation?> GetAnnotationAsync(Guid annotationId, CancellationToken ct = default)
        => _db.SubmissionAnnotations.FirstOrDefaultAsync(a => a.Id == annotationId, ct);

    public async Task<IReadOnlyList<ProvisionalGrade>> GetProvisionalGradesAsync(IReadOnlyCollection<Guid> submissionIds, CancellationToken ct = default)
        => submissionIds.Count == 0 ? [] : await _db.ProvisionalGrades.Where(g => submissionIds.Contains(g.SubmissionId)).ToListAsync(ct);

    public async Task<IReadOnlyList<SubmissionFile>> GetConversionCandidatesAsync(DateTime staleBefore, int take, CancellationToken ct = default)
        => await _db.SubmissionFiles
            .Where(f => f.StorageKey != null
                && (f.ConversionStatus == FileConversionStatus.Pending
                    || (f.ConversionStatus == FileConversionStatus.Converting && f.ConversionStartedAt < staleBefore)))
            .OrderBy(f => f.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(decimal Marks, decimal MaxMarks, Guid AssignmentId)>> GetPostedGradesAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default)
    {
        var rows = await _db.AssignmentSubmissions
            .Where(s => s.StudentProfileId == studentProfileId
                && s.Assignment.CourseOfferingId == courseOfferingId
                && s.PostedAt != null && s.MarksAwarded != null
                && s.Status == SubmissionStatus.Graded)
            .Select(s => new { Marks = s.MarksAwarded!.Value, s.Assignment.MaxMarks, s.AssignmentId })
            .ToListAsync(ct);
        return rows.Select(r => (r.Marks, r.MaxMarks, r.AssignmentId)).ToList();
    }

    public Task<bool> IsEnrolledAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default)
        => _db.Enrollments.AnyAsync(e => e.CourseOfferingId == courseOfferingId && e.StudentProfileId == studentProfileId
            && e.Status == EnrollmentStatus.Active, ct);

    public async Task<IReadOnlyDictionary<Guid, (string Name, string? RegistrationNumber)>> GetStudentNamesAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default)
    {
        if (studentProfileIds.Count == 0) return new Dictionary<Guid, (string, string?)>();
        var rows = await (from sp in _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking()
                          join u in _db.Users.IgnoreQueryFilters().AsNoTracking() on sp.UserId equals u.Id
                          where studentProfileIds.Contains(sp.Id)
                          select new { sp.Id, Name = u.FullName ?? u.Username, sp.RegistrationNumber })
                         .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => (r.Name, (string?)r.RegistrationNumber));
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, string>();
        return await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Username, ct);
    }

    public Task<Guid?> GetStudentUserIdAsync(Guid studentProfileId, CancellationToken ct = default)
        => _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking()
            .Where(sp => sp.Id == studentProfileId).Select(sp => (Guid?)sp.UserId).FirstOrDefaultAsync(ct);

    public void Add<T>(T entity) where T : BaseEntity
    {
        // New children reached through a tracked parent may already be tracked as Added
        // (their keys are not store-generated, so EF treats them as new).
        if (_db.Entry(entity).State == EntityState.Detached) _db.Add(entity);
    }

    public void Remove<T>(T entity) where T : BaseEntity => _db.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<bool> TrySaveClaimAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries) entry.State = EntityState.Detached;
            return false;
        }
    }
}
