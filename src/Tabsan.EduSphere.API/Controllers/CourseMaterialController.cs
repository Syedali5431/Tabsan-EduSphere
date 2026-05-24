using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.API.Services;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/course-materials")]
[Authorize]
public sealed class CourseMaterialController : ControllerBase
{
    private readonly ICourseMaterialService _materials;
    private readonly IMediaStorageService _mediaStorage;
    private readonly IAccessScopeResolver _accessScope;

    public CourseMaterialController(
        ICourseMaterialService materials,
        IMediaStorageService mediaStorage,
        IAccessScopeResolver accessScope)
    {
        _materials = materials;
        _mediaStorage = mediaStorage;
        _accessScope = accessScope;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? academicProgramId,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? courseId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var items = await _materials.GetAllAsync(departmentId, academicProgramId, semesterId, courseId, tenantId, campusId, activeOnly, ct);
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

    [HttpPost("upload")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var uploadError = await FileUploadValidator.ValidateAsync(file);
        if (!string.IsNullOrWhiteSpace(uploadError))
            return BadRequest(new { message = uploadError });

        var extension = Path.GetExtension(file.FileName);

        var category = BuildScopedStorageCategory();

        await using var stream = file.OpenReadStream();
        var stored = await _mediaStorage.SaveAsync(
            stream,
            category,
            extension,
            file.ContentType,
            file.FileName,
            ct);

        return Ok(new CourseMaterialUploadDto
        {
            BlobPath = stored.StorageKey,
            FileUrl = stored.Reference,
            FileName = stored.DownloadFileName ?? Path.GetFileName(file.FileName),
            FileSizeBytes = stored.Length,
            ContentType = stored.ContentType
        });
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken ct = default)
    {
        var material = await _materials.GetByIdAsync(id, ct);
        if (material is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(material.BlobPath))
            return NotFound(new { message = "Material does not have an uploaded file." });

        var fileBytes = await _mediaStorage.ReadAsBytesAsync(material.BlobPath, ct);
        if (fileBytes is null || fileBytes.Length == 0)
            return NotFound(new { message = "Stored file was not found." });

        var metadata = await _mediaStorage.GetMetadataAsync(material.BlobPath, ct);
        var contentType = metadata?.ContentType ?? "application/octet-stream";
        var downloadFileName = metadata?.DownloadFileName;

        if (string.IsNullOrWhiteSpace(downloadFileName))
            downloadFileName = !string.IsNullOrWhiteSpace(material.FileName)
                ? material.FileName
                : Path.GetFileName(material.BlobPath);

        return File(fileBytes, contentType, downloadFileName);
    }

    [HttpPost("{id:guid}/active")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive, CancellationToken ct = default)
    {
        await _materials.SetActiveAsync(id, isActive, ct);
        return NoContent();
    }

    private string BuildScopedStorageCategory()
    {
        var tenantId = _accessScope.GetTenantId();
        var campusId = _accessScope.GetCampusId();

        if (tenantId.HasValue && campusId.HasValue)
            return $"course-materials/{tenantId.Value:N}/{campusId.Value:N}";

        if (_accessScope.IsSuperAdmin())
            return "course-materials/superadmin";

        throw new UnauthorizedAccessException("A valid tenant and campus scope is required for material upload.");
    }
}