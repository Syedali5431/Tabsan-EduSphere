using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M7 — module items, progress, prerequisites and the course content they point to.</summary>
public sealed class ModuleItemRepository : IModuleItemRepository
{
    private readonly ApplicationDbContext _db;

    public ModuleItemRepository(ApplicationDbContext db) => _db = db;

    public Task<ModuleOfferingInfo?> GetOfferingAsync(Guid offeringId, CancellationToken ct = default)
        => _db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => o.Id == offeringId)
            .Select(o => new ModuleOfferingInfo(o.Id, o.CourseId, o.Course.Code, o.Course.Title, o.TenantId, o.CampusId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CourseContentModule>> GetModulesAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.CourseContentModules.Where(m => m.OfferingId == offeringId).ToListAsync(ct);

    public Task<CourseContentModule?> GetModuleAsync(Guid moduleId, CancellationToken ct = default)
        => _db.CourseContentModules.FirstOrDefaultAsync(m => m.Id == moduleId, ct);

    public async Task<IReadOnlyList<ModuleItem>> GetItemsAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.ModuleItems.Where(i => i.OfferingId == offeringId).OrderBy(i => i.Position).ToListAsync(ct);

    public Task<ModuleItem?> GetItemAsync(Guid itemId, CancellationToken ct = default)
        => _db.ModuleItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);

    public async Task<IReadOnlyList<ModuleItem>> GetItemsByTargetAsync(ModuleItemType type, Guid targetId, CancellationToken ct = default)
        => await _db.ModuleItems.Where(i => i.ItemType == type && i.TargetId == targetId).ToListAsync(ct);

    public async Task<IReadOnlyList<ModulePrerequisite>> GetPrerequisitesAsync(IReadOnlyCollection<Guid> moduleIds, CancellationToken ct = default)
        => moduleIds.Count == 0 ? [] : await _db.ModulePrerequisites.Where(p => moduleIds.Contains(p.ModuleId)).ToListAsync(ct);

    public async Task<IReadOnlyList<ModuleItemProgress>> GetProgressAsync(Guid offeringId, IReadOnlyCollection<Guid>? studentIds, CancellationToken ct = default)
    {
        var q = _db.ModuleItemProgress.Where(p => p.OfferingId == offeringId);
        if (studentIds is not null) q = q.Where(p => studentIds.Contains(p.StudentProfileId));
        return await q.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ModuleItemProgress>> GetProgressForItemAsync(Guid itemId, CancellationToken ct = default)
        => await _db.ModuleItemProgress.Where(p => p.ModuleItemId == itemId).ToListAsync(ct);

    private IQueryable<ModuleStudentInfo> Students(IQueryable<Guid> ids)
        => from sp in _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking()
           join u in _db.Users.IgnoreQueryFilters().AsNoTracking() on sp.UserId equals u.Id
           where ids.Contains(sp.Id)
           select new ModuleStudentInfo(sp.Id, u.Id, u.FullName ?? u.Username, sp.RegistrationNumber);

    public async Task<IReadOnlyList<ModuleStudentInfo>> GetEnrolledStudentsAsync(Guid offeringId, CancellationToken ct = default)
        => await Students(_db.Enrollments.Where(e => e.CourseOfferingId == offeringId && e.Status == EnrollmentStatus.Active).Select(e => e.StudentProfileId))
            .ToListAsync(ct);

    public Task<ModuleStudentInfo?> GetStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => Students(_db.StudentProfiles.IgnoreQueryFilters().Where(s => s.Id == studentProfileId).Select(s => s.Id)).FirstOrDefaultAsync(ct);

    public Task<bool> IsEnrolledAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default)
        => _db.Enrollments.AnyAsync(e => e.CourseOfferingId == offeringId && e.StudentProfileId == studentProfileId && e.Status == EnrollmentStatus.Active, ct);

    public async Task<IReadOnlyList<ModuleLinkable>> GetLinkablesAsync(Guid offeringId, CancellationToken ct = default)
    {
        var result = new List<ModuleLinkable>();
        result.AddRange((await _db.Assignments.AsNoTracking().Where(a => a.CourseOfferingId == offeringId)
                .Select(a => new { a.Id, a.Title, a.IsPublished, a.DueDate }).ToListAsync(ct))
            .Select(a => new ModuleLinkable(ModuleItemType.Assignment, a.Id, a.Title, a.IsPublished ? $"due {a.DueDate:dd MMM yyyy}" : "not published")));
        result.AddRange((await _db.Quizzes.AsNoTracking().Where(q => q.CourseOfferingId == offeringId && q.IsActive)
                .Select(q => new { q.Id, q.Title, q.IsPublished }).ToListAsync(ct))
            .Select(q => new ModuleLinkable(ModuleItemType.Quiz, q.Id, q.Title, q.IsPublished ? null : "not published")));
        result.AddRange((await _db.DiscussionThreads.AsNoTracking().Where(t => t.OfferingId == offeringId)
                .Select(t => new { t.Id, t.Title }).ToListAsync(ct))
            .Select(t => new ModuleLinkable(ModuleItemType.Discussion, t.Id, t.Title, null)));
        result.AddRange((await _db.LtiResourceLinks.AsNoTracking().Where(l => l.CourseOfferingId == offeringId)
                .Select(l => new { l.Id, l.Title, l.LineItemId }).ToListAsync(ct))
            .Select(l => new ModuleLinkable(ModuleItemType.ExternalTool, l.Id, l.Title, l.LineItemId is null ? null : "graded")));
        result.AddRange((await (from v in _db.ContentVideos.AsNoTracking()
                                join m in _db.CourseContentModules.AsNoTracking() on v.ModuleId equals m.Id
                                where m.OfferingId == offeringId
                                select new { v.Id, v.Title, Module = m.Title }).ToListAsync(ct))
            .Select(v => new ModuleLinkable(ModuleItemType.Video, v.Id, v.Title, v.Module)));
        result.AddRange((await _db.ScormPackages.AsNoTracking().Where(p => p.OfferingId == offeringId && !p.IsRemoved)
                .Select(p => new { p.Id, p.Title, p.IsPublished, Graded = p.AssignmentId != null }).ToListAsync(ct))
            .Select(p => new ModuleLinkable(ModuleItemType.ScormPackage, p.Id, p.Title, !p.IsPublished ? "not published" : p.Graded ? "graded" : null)));
        var courseId = await _db.CourseOfferings.IgnoreQueryFilters().Where(o => o.Id == offeringId).Select(o => o.CourseId).FirstOrDefaultAsync(ct);
        result.AddRange((await _db.CourseMaterials.AsNoTracking().Where(m => m.CourseId == courseId && m.IsActive)
                .Select(m => new { m.Id, m.Name, m.MaterialType }).ToListAsync(ct))
            .Select(m => new ModuleLinkable(ModuleItemType.Material, m.Id, m.Name, m.MaterialType.ToString())));
        return result;
    }

    public async Task<IReadOnlyList<ModuleWorkEvidence>> GetAssignmentEvidenceAsync(IReadOnlyCollection<Guid> assignmentIds, IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default)
    {
        var rows = await _db.AssignmentSubmissions.AsNoTracking()
            .Where(s => assignmentIds.Contains(s.AssignmentId) && studentIds.Contains(s.StudentProfileId))
            .Select(s => new { s.AssignmentId, s.StudentProfileId, s.Status, s.MarksAwarded, s.PostedAt, s.Assignment.MaxMarks })
            .ToListAsync(ct);
        return rows.Select(r => new ModuleWorkEvidence(r.AssignmentId, r.StudentProfileId, r.Status != SubmissionStatus.Rejected,
            r.PostedAt is not null && r.MarksAwarded is { } marks && r.MaxMarks > 0 ? Math.Round(marks / r.MaxMarks * 100, 2) : null)).ToList();
    }

    public async Task<IReadOnlyList<ModuleWorkEvidence>> GetQuizEvidenceAsync(IReadOnlyCollection<Guid> quizIds, IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default)
    {
        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => quizIds.Contains(a.QuizId) && studentIds.Contains(a.StudentProfileId)
                && (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.TimedOut))
            .Select(a => new
            {
                a.QuizId, a.StudentProfileId, a.TotalScore,
                ItemMax = a.Items.Sum(i => (decimal?)i.Marks) ?? 0,
                QuizMax = a.Quiz.Questions.Where(q => !q.IsArchived).Sum(q => (decimal?)q.Marks) ?? 0
            })
            .ToListAsync(ct);
        return attempts.GroupBy(a => (a.QuizId, a.StudentProfileId)).Select(g =>
        {
            decimal? best = g.Select(a =>
            {
                var max = a.ItemMax > 0 ? a.ItemMax : a.QuizMax;
                return a.TotalScore is { } score && max > 0 ? Math.Round(Math.Clamp(score / max, 0, 1) * 100, 2) : (decimal?)null;
            }).Max();
            return new ModuleWorkEvidence(g.Key.QuizId, g.Key.StudentProfileId, true, best);
        }).ToList();
    }

    public async Task<IReadOnlyList<(Guid ThreadId, Guid UserId)>> GetDiscussionContributionsAsync(IReadOnlyCollection<Guid> threadIds, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        var replies = await _db.DiscussionReplies.AsNoTracking().Where(r => threadIds.Contains(r.ThreadId) && userIds.Contains(r.AuthorId))
            .Select(r => new { r.ThreadId, r.AuthorId }).Distinct().ToListAsync(ct);
        var started = await _db.DiscussionThreads.AsNoTracking().Where(t => threadIds.Contains(t.Id) && userIds.Contains(t.AuthorId))
            .Select(t => new { ThreadId = t.Id, t.AuthorId }).ToListAsync(ct);
        return replies.Concat(started).Select(x => (x.ThreadId, x.AuthorId)).Distinct().ToList();
    }

    public async Task<IReadOnlyList<ModuleWorkEvidence>> GetScormEvidenceAsync(IReadOnlyCollection<Guid> packageIds, IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default)
    {
        var rows = await _db.ScormAttempts.AsNoTracking()
            .Where(a => packageIds.Contains(a.PackageId) && studentIds.Contains(a.StudentProfileId) && a.FinishedAt != null)
            .Select(a => new { a.PackageId, a.StudentProfileId, a.ScorePercent })
            .ToListAsync(ct);
        return rows.GroupBy(r => (r.PackageId, r.StudentProfileId))
            .Select(g => new ModuleWorkEvidence(g.Key.PackageId, g.Key.StudentProfileId, true, g.Max(r => r.ScorePercent))).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetLtiAssignmentsAsync(IReadOnlyCollection<Guid> linkIds, CancellationToken ct = default)
        => await (from l in _db.LtiResourceLinks.AsNoTracking()
                  join li in _db.LtiLineItems.AsNoTracking() on l.LineItemId equals li.Id
                  where linkIds.Contains(l.Id)
                  select new { l.Id, li.AssignmentId }).ToDictionaryAsync(x => x.Id, x => x.AssignmentId, ct);

    public async Task<(string? EmbedUrl, string? StorageUrl)?> GetVideoAsync(Guid videoId, CancellationToken ct = default)
    {
        var v = await _db.ContentVideos.AsNoTracking().Where(x => x.Id == videoId).Select(x => new { x.EmbedUrl, x.StorageUrl }).FirstOrDefaultAsync(ct);
        return v is null ? null : (v.EmbedUrl, v.StorageUrl);
    }

    public async Task<(string? LinkUrl, bool HasFile)?> GetMaterialAsync(Guid materialId, CancellationToken ct = default)
    {
        var m = await _db.CourseMaterials.AsNoTracking().Where(x => x.Id == materialId && x.IsActive).Select(x => new { x.LinkUrl, x.FilePath }).FirstOrDefaultAsync(ct);
        return m is null ? null : (m.LinkUrl, !string.IsNullOrWhiteSpace(m.FilePath));
    }

    public void Add(ModuleItem item) => _db.ModuleItems.Add(item);
    public void Remove(ModuleItem item) => _db.ModuleItems.Remove(item);
    public void Add(ModulePrerequisite prerequisite) => _db.ModulePrerequisites.Add(prerequisite);
    public void Remove(ModulePrerequisite prerequisite) => _db.ModulePrerequisites.Remove(prerequisite);
    public void Add(ModuleItemProgress progress) => _db.ModuleItemProgress.Add(progress);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
