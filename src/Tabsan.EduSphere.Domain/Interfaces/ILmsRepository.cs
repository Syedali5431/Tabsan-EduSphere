using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Final-Touches Phase 20 Stage 20.1/20.2 — repository contract for LMS course content

/// <summary>Repository for course content modules and their video attachments.</summary>
public interface ILmsRepository
{
    // ── Modules ────────────────────────────────────────────────────────────────

    /// <summary>Returns all modules for an offering, ordered by WeekNumber.</summary>
    Task<IReadOnlyList<CourseContentModule>> GetModulesByOfferingAsync(Guid offeringId, bool publishedOnly = false, CancellationToken ct = default);

    /// <summary>Returns a single module including its videos.</summary>
    Task<CourseContentModule?> GetModuleByIdAsync(Guid moduleId, CancellationToken ct = default);
    Task<bool> CourseOfferingExistsAsync(Guid offeringId, CancellationToken ct = default);

    Task AddModuleAsync(CourseContentModule module, CancellationToken ct = default);
    void UpdateModule(CourseContentModule module);
    void DeleteModule(CourseContentModule module);

    // ── Videos ─────────────────────────────────────────────────────────────────

    Task<ContentVideo?> GetVideoByIdAsync(Guid videoId, CancellationToken ct = default);
    Task AddVideoAsync(ContentVideo video, CancellationToken ct = default);
    void UpdateVideo(ContentVideo video);
    void DeleteVideo(ContentVideo video);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
