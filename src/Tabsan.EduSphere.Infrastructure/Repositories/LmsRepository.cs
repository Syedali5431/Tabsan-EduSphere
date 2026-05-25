using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

// Final-Touches Phase 20 Stage 20.1/20.2 — LMS content repository

/// <summary>EF Core implementation of ILmsRepository for course content modules and videos.</summary>
public sealed class LmsRepository : ILmsRepository
{
    private readonly ApplicationDbContext _db;
    public LmsRepository(ApplicationDbContext db) => _db = db;

    // ── Modules ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CourseContentModule>> GetModulesByOfferingAsync(
        Guid offeringId, bool publishedOnly, CancellationToken ct = default)
    {
        var query = _db.CourseContentModules
                       .Include(m => m.Videos)
                       .Where(m => m.OfferingId == offeringId);
        if (publishedOnly)
            query = query.Where(m => m.IsPublished);
        return await query.OrderBy(m => m.WeekNumber).ThenBy(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task<CourseContentModule?> GetModuleByIdAsync(Guid moduleId, CancellationToken ct = default)
        => await _db.CourseContentModules
                    .Include(m => m.Videos)
                    .FirstOrDefaultAsync(m => m.Id == moduleId, ct);

    public Task<bool> CourseOfferingExistsAsync(Guid offeringId, CancellationToken ct = default)
        => _db.CourseOfferings
            .AsNoTracking()
            .AnyAsync(o => o.Id == offeringId && !o.IsDeleted, ct);

    public async Task AddModuleAsync(CourseContentModule module, CancellationToken ct = default)
        => await _db.CourseContentModules.AddAsync(module, ct);

    public void UpdateModule(CourseContentModule module)
        => _db.CourseContentModules.Update(module);

    public void DeleteModule(CourseContentModule module)
        => _db.CourseContentModules.Remove(module);

    // ── Videos ─────────────────────────────────────────────────────────────────

    public async Task<ContentVideo?> GetVideoByIdAsync(Guid videoId, CancellationToken ct = default)
        => await _db.ContentVideos.FirstOrDefaultAsync(v => v.Id == videoId, ct);

    public async Task AddVideoAsync(ContentVideo video, CancellationToken ct = default)
        => await _db.ContentVideos.AddAsync(video, ct);

    public void UpdateVideo(ContentVideo video)
        => _db.ContentVideos.Update(video);

    public void DeleteVideo(ContentVideo video)
        => _db.ContentVideos.Remove(video);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
