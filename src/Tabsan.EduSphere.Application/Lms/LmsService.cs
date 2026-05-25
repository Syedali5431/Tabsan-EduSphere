using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms;

// Final-Touches Phase 20 Stage 20.1/20.2 — LMS content service implementation

/// <summary>
/// Application service for Phase 20 — LMS course content modules and videos.
/// </summary>
public sealed class LmsService : ILmsService
{
    private readonly ILmsRepository _repo;

    public LmsService(ILmsRepository repo) => _repo = repo;

    // ── Modules ────────────────────────────────────────────────────────────────

    public async Task<List<CourseContentModuleDto>> GetModulesAsync(
        Guid offeringId, bool publishedOnly, CancellationToken ct = default)
    {
        var modules = await _repo.GetModulesByOfferingAsync(offeringId, publishedOnly, ct);
        return modules.Select(MapModule).ToList();
    }

    public async Task<CourseContentModuleDto?> GetModuleAsync(Guid moduleId, CancellationToken ct = default)
    {
        var m = await _repo.GetModuleByIdAsync(moduleId, ct);
        return m is null ? null : MapModule(m);
    }

    public async Task<CourseContentModuleDto> CreateModuleAsync(
        CreateModuleRequest request, CancellationToken ct = default)
    {
        if (request.OfferingId == Guid.Empty)
            throw new InvalidOperationException("OfferingId is required.");

        var offeringExists = await _repo.CourseOfferingExistsAsync(request.OfferingId, ct);
        if (!offeringExists)
            throw new InvalidOperationException($"Course offering {request.OfferingId} not found.");

        var module = new CourseContentModule(request.OfferingId, request.Title, request.WeekNumber, request.Body);
        await _repo.AddModuleAsync(module, ct);
        try
        {
            await _repo.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (string.Equals(ex.GetType().Name, "DbUpdateException", StringComparison.Ordinal))
        {
            if (ex.InnerException?.Message.Contains("FK_course_content_modules_course_offerings_OfferingId", StringComparison.OrdinalIgnoreCase) == true)
                throw new InvalidOperationException($"Course offering {request.OfferingId} not found.");

            throw;
        }

        return MapModule(module);
    }

    public async Task UpdateModuleAsync(Guid moduleId, UpdateModuleRequest request, CancellationToken ct = default)
    {
        var module = await _repo.GetModuleByIdAsync(moduleId, ct)
            ?? throw new InvalidOperationException($"Module {moduleId} not found.");
        module.Update(request.Title, request.WeekNumber, request.Body);
        _repo.UpdateModule(module);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task PublishModuleAsync(Guid moduleId, CancellationToken ct = default)
    {
        var module = await _repo.GetModuleByIdAsync(moduleId, ct)
            ?? throw new InvalidOperationException($"Module {moduleId} not found.");
        module.Publish();
        _repo.UpdateModule(module);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task UnpublishModuleAsync(Guid moduleId, CancellationToken ct = default)
    {
        var module = await _repo.GetModuleByIdAsync(moduleId, ct)
            ?? throw new InvalidOperationException($"Module {moduleId} not found.");
        module.Unpublish();
        _repo.UpdateModule(module);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteModuleAsync(Guid moduleId, CancellationToken ct = default)
    {
        var module = await _repo.GetModuleByIdAsync(moduleId, ct)
            ?? throw new InvalidOperationException($"Module {moduleId} not found.");
        module.SoftDelete();
        _repo.UpdateModule(module);
        await _repo.SaveChangesAsync(ct);
    }

    // ── Videos ─────────────────────────────────────────────────────────────────

    public async Task<ContentVideoDto> AddVideoAsync(AddVideoRequest request, CancellationToken ct = default)
    {
        var video = new ContentVideo(
            request.ModuleId, request.Title, request.StorageUrl, request.EmbedUrl, request.DurationSeconds);
        await _repo.AddVideoAsync(video, ct);
        await _repo.SaveChangesAsync(ct);
        return MapVideo(video);
    }

    public async Task DeleteVideoAsync(Guid videoId, CancellationToken ct = default)
    {
        var video = await _repo.GetVideoByIdAsync(videoId, ct)
            ?? throw new InvalidOperationException($"Video {videoId} not found.");
        video.SoftDelete();
        _repo.UpdateVideo(video);
        await _repo.SaveChangesAsync(ct);
    }

    // ── Mappers ────────────────────────────────────────────────────────────────

    private static CourseContentModuleDto MapModule(CourseContentModule m) => new()
    {
        Id          = m.Id,
        OfferingId  = m.OfferingId,
        Title       = m.Title,
        WeekNumber  = m.WeekNumber,
        Body        = m.Body,
        IsPublished = m.IsPublished,
        PublishedAt = m.PublishedAt,
        Videos      = m.Videos.Where(v => !v.IsDeleted).Select(MapVideo).ToList()
    };

    private static ContentVideoDto MapVideo(ContentVideo v) => new()
    {
        Id              = v.Id,
        ModuleId        = v.ModuleId,
        Title           = v.Title,
        StorageUrl      = v.StorageUrl,
        EmbedUrl        = v.EmbedUrl,
        DurationSeconds = v.DurationSeconds
    };
}
