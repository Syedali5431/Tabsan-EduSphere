using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

// Final-Touches Phase 20 Stage 20.3 — discussion repository
// Phase 31 Stage 31.3 — Extended with ticket generation support

/// <summary>EF Core implementation of IDiscussionRepository.</summary>
public sealed class DiscussionRepository : IDiscussionRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IAccessScopeResolver? _accessScope;

    public DiscussionRepository(ApplicationDbContext db, IAccessScopeResolver? accessScope = null)
    {
        _db = db;
        _accessScope = accessScope;
    }

    private IQueryable<DiscussionThread> ApplyTenantCampusScope(IQueryable<DiscussionThread> query)
    {
        if (_accessScope?.IsSuperAdmin() == true)
            return query;

        var tenantId = _accessScope?.GetTenantId();
        var campusId = _accessScope?.GetCampusId();

        if (!tenantId.HasValue)
            return query.Where(_ => false);

        var scopedOfferingIds = _db.CourseOfferings
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId.Value && (!campusId.HasValue || o.CampusId == campusId.Value))
            .Select(o => o.Id);

        return query.Where(t => scopedOfferingIds.Contains(t.OfferingId));
    }

    public async Task<IReadOnlyList<DiscussionThread>> GetThreadsByOfferingAsync(
        Guid offeringId, CancellationToken ct = default)
        => await ApplyTenantCampusScope(_db.DiscussionThreads)
                    .Where(t => t.OfferingId == offeringId)
                    .OrderByDescending(t => t.IsPinned)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync(ct);

    public async Task<DiscussionThread?> GetThreadByIdAsync(Guid threadId, CancellationToken ct = default)
        => await ApplyTenantCampusScope(_db.DiscussionThreads)
                    .Include(t => t.Replies)
                    .FirstOrDefaultAsync(t => t.Id == threadId, ct);

    /// <summary>Counts all threads created by a user (identified by username) for ticket number generation.</summary>
    public async Task<int> CountThreadsByAuthorUsernameAsync(string authorUsername, CancellationToken ct = default)
        => await ApplyTenantCampusScope(_db.DiscussionThreads)
                    .Join(_db.Users,
                        t => t.AuthorId,
                        u => u.Id,
                        (t, u) => new { t, u })
                    .Where(x => x.u.Username == authorUsername && !x.t.IsDeleted)
                    .CountAsync(ct);

    public async Task AddThreadAsync(DiscussionThread thread, CancellationToken ct = default)
        => await _db.DiscussionThreads.AddAsync(thread, ct);

    public void UpdateThread(DiscussionThread thread)
        => _db.DiscussionThreads.Update(thread);

    public void DeleteThread(DiscussionThread thread)
        => _db.DiscussionThreads.Remove(thread);

    public async Task<DiscussionReply?> GetReplyByIdAsync(Guid replyId, CancellationToken ct = default)
        => await _db.DiscussionReplies
            .Join(
                ApplyTenantCampusScope(_db.DiscussionThreads),
                r => r.ThreadId,
                t => t.Id,
                (r, t) => r)
            .FirstOrDefaultAsync(r => r.Id == replyId, ct);

    public async Task AddReplyAsync(DiscussionReply reply, CancellationToken ct = default)
        => await _db.DiscussionReplies.AddAsync(reply, ct);

    public void UpdateReply(DiscussionReply reply)
        => _db.DiscussionReplies.Update(reply);

    public void DeleteReply(DiscussionReply reply)
        => _db.DiscussionReplies.Remove(reply);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
