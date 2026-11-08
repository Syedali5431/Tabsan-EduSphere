using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

// Final-Touches Phase 15 Stage 15.1 — PrerequisiteController: CRUD for course prerequisites
/// <summary>
/// Manages prerequisite links between courses.
/// Admin and SuperAdmin can add or remove prerequisites; all authenticated users can view them.
/// </summary>
[ApiController]
[Route("api/v1/prerequisite")]
[Authorize]
public class PrerequisiteController : ControllerBase
{
    private readonly IPrerequisiteRepository _repo;
    private readonly ICourseRepository _courseRepo;

    public PrerequisiteController(IPrerequisiteRepository repo, ICourseRepository courseRepo)
    {
        _repo = repo;
        _courseRepo = courseRepo;
    }

    // ── GET /api/v1/prerequisite/{courseId} ───────────────────────────────────

    /// <summary>Returns all prerequisites for the given course.</summary>
    [HttpGet("{courseId:guid}")]
    public async Task<IActionResult> GetByCourse(Guid courseId, CancellationToken ct)
        => Ok(ToDtos(await _repo.GetByCourseIdAsync(courseId, ct)));

    // ── GET /api/v1/prerequisite?departmentId= ────────────────────────────────

    /// <summary>
    /// Returns the prerequisites of every course in a department in one call. The portal's Prerequisites page used one call
    /// per course: with 600 courses a single page load used up the whole per-user request limit (600 a minute).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetByDepartment([FromQuery] Guid? departmentId, CancellationToken ct)
    {
        if (departmentId is not { } id || id == Guid.Empty)
            return BadRequest(new { message = "departmentId is required." });
        return Ok(ToDtos(await _repo.GetByDepartmentIdAsync(id, ct)));
    }

    private static IEnumerable<PrerequisiteDto> ToDtos(IEnumerable<CoursePrerequisite> prereqs)
        => prereqs.Select(p => new PrerequisiteDto(
            CourseId:               p.CourseId,
            CourseCode:             p.Course?.Code ?? "",
            CourseTitle:            p.Course?.Title ?? "",
            PrerequisiteCourseId:   p.PrerequisiteCourseId,
            PrerequisiteCourseCode: p.PrerequisiteCourse?.Code ?? "",
            PrerequisiteCourseTitle: p.PrerequisiteCourse?.Title ?? ""));

    // ── POST /api/v1/prerequisite ─────────────────────────────────────────────

    /// <summary>Adds a prerequisite link. Only Admin and SuperAdmin can call this.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Add([FromBody] AddPrerequisiteRequest request, CancellationToken ct)
    {
        if (request.CourseId == request.PrerequisiteCourseId)
            return BadRequest("A course cannot be its own prerequisite.");

        var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
        if (course is null) return NotFound("Course not found.");

        var prereqCourse = await _courseRepo.GetByIdAsync(request.PrerequisiteCourseId, ct);
        if (prereqCourse is null) return NotFound("Prerequisite course not found.");

        if (await _repo.ExistsAsync(request.CourseId, request.PrerequisiteCourseId, ct))
            return Conflict("This prerequisite link already exists.");

        var link = new CoursePrerequisite(request.CourseId, request.PrerequisiteCourseId);
        await _repo.AddAsync(link, ct);
        await _repo.SaveChangesAsync(ct);

        return Created($"api/v1/prerequisite/{request.CourseId}",
            new PrerequisiteDto(
                CourseId:               course.Id,
                CourseCode:             course.Code,
                CourseTitle:            course.Title,
                PrerequisiteCourseId:   prereqCourse.Id,
                PrerequisiteCourseCode: prereqCourse.Code,
                PrerequisiteCourseTitle: prereqCourse.Title));
    }

    // ── DELETE /api/v1/prerequisite/{courseId}/{prereqCourseId} ──────────────

    /// <summary>Removes a prerequisite link. Only Admin and SuperAdmin can call this.</summary>
    [HttpDelete("{courseId:guid}/{prereqCourseId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Remove(Guid courseId, Guid prereqCourseId, CancellationToken ct)
    {
        if (!await _repo.ExistsAsync(courseId, prereqCourseId, ct))
            return NotFound("Prerequisite link not found.");

        await _repo.RemoveAsync(courseId, prereqCourseId, ct);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }
}
