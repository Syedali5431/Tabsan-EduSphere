using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/course-materials")]
[Authorize]
public sealed class CourseMaterialController : ControllerBase
{
    private readonly ICourseMaterialService _materials;

    public CourseMaterialController(ICourseMaterialService materials)
    {
        _materials = materials;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? academicProgramId,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? courseId,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var items = await _materials.GetAllAsync(departmentId, academicProgramId, semesterId, courseId, activeOnly, ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var item = await _materials.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCourseMaterialRequest request, CancellationToken ct = default)
    {
        var callerIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (!Guid.TryParse(callerIdRaw, out var callerId) || callerId == Guid.Empty)
            return Unauthorized();

        var created = await _materials.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseMaterialRequest request, CancellationToken ct = default)
    {
        var updated = await _materials.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/active")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive, CancellationToken ct = default)
    {
        await _materials.SetActiveAsync(id, isActive, ct);
        return NoContent();
    }
}