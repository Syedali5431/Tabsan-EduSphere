using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Academic;

// Final-Touches Phase 19 Stage 19.1 — auto-semester generation service

/// <summary>Application service for advanced course operations (Phase 19).</summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepo;
    private readonly ISemesterRepository _semesterRepo;

    public CourseService(ICourseRepository courseRepo, ISemesterRepository semesterRepo)
    {
        _courseRepo = courseRepo;
        _semesterRepo = semesterRepo;
    }

    // Final-Touches Phase 19 Stage 19.1 — idempotent auto-creation of Semester rows
    /// <inheritdoc/>
    public async Task<AutoCreateSemestersResult> AutoCreateSemestersAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _courseRepo.GetByIdAsync(courseId, ct)
            ?? throw new InvalidOperationException($"Course '{courseId}' not found.");

        if (!course.HasSemesters || course.TotalSemesters is null or <= 0)
            return new AutoCreateSemestersResult(0, Array.Empty<Guid>());

        // Load existing semesters to avoid duplicates
        var existing = await _semesterRepo.GetAllAsync(ct);
        var existingNames = new HashSet<string>(existing.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);

        var createdIds = new List<Guid>();
        var baseDate = DateTime.UtcNow.Date;

        for (int i = 1; i <= course.TotalSemesters.Value; i++)
        {
            var name = $"{course.Code} — Semester {i}";
            if (existingNames.Contains(name))
                continue;

            // Generate back-to-back six-month teaching windows for each semester slot.
            var start = baseDate.AddMonths((i - 1) * 6);
            var end   = start.AddMonths(6).AddDays(-1);

            var semester = new Semester(name, start, end);
            await _semesterRepo.AddAsync(semester, ct);
            createdIds.Add(semester.Id);
        }

        if (createdIds.Count > 0)
            await _semesterRepo.SaveChangesAsync(ct);

        return new AutoCreateSemestersResult(createdIds.Count, createdIds);
    }
}
