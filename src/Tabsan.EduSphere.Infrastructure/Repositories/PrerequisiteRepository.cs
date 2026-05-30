using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

// Final-Touches Phase 15 Stage 15.1 — PrerequisiteRepository: prerequisite CRUD
/// <summary>EF Core implementation of IPrerequisiteRepository.</summary>
public class PrerequisiteRepository : IPrerequisiteRepository
{
    private readonly ApplicationDbContext _db;

    public PrerequisiteRepository(ApplicationDbContext db) => _db = db;

    /// <summary>Returns all prerequisites for the given course with PrerequisiteCourse navigation loaded.</summary>
    public async Task<IReadOnlyList<CoursePrerequisite>> GetByCourseIdAsync(Guid courseId, CancellationToken ct = default)
        => await _db.CoursePrerequisites
                    .Include(p => p.Course)
                    .Include(p => p.PrerequisiteCourse)
                    .Where(p => p.CourseId == courseId)
                    .OrderBy(p => p.PrerequisiteCourse.Code)
                    .ToListAsync(ct);

    /// <summary>Returns true when the prerequisite link already exists.</summary>
    public Task<bool> ExistsAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct = default)
        => _db.CoursePrerequisites.AnyAsync(
               p => p.CourseId == courseId && p.PrerequisiteCourseId == prerequisiteCourseId, ct);

    /// <summary>Queues the prerequisite for insertion.</summary>
    public async Task AddAsync(CoursePrerequisite prerequisite, CancellationToken ct = default)
        => await _db.CoursePrerequisites.AddAsync(prerequisite, ct);

    /// <summary>Removes the prerequisite link if it exists.</summary>
    public async Task RemoveAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct = default)
    {
        var link = await _db.CoursePrerequisites.FirstOrDefaultAsync(
                       p => p.CourseId == courseId && p.PrerequisiteCourseId == prerequisiteCourseId, ct);
        if (link is not null)
            _db.CoursePrerequisites.Remove(link);
    }

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
