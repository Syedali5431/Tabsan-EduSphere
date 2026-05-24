using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms;

public sealed class CourseMaterialService : ICourseMaterialService
{
    private readonly ICourseMaterialRepository _materials;
    private readonly IAccessScopeResolver _accessScope;

    public CourseMaterialService(ICourseMaterialRepository materials, IAccessScopeResolver accessScope)
    {
        _materials = materials;
        _accessScope = accessScope;
    }

    public async Task<IReadOnlyList<CourseMaterialDto>> GetAllAsync(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        Guid? tenantId,
        Guid? campusId,
        bool activeOnly,
        CancellationToken ct = default)
    {
        var items = await _materials.GetByFiltersAsync(departmentId, academicProgramId, semesterId, courseId, tenantId, campusId, activeOnly, ct);
        return items.Select(Map).ToList();
    }

    public async Task<CourseMaterialDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _materials.GetByIdAsync(id, ct);
        return item is null ? null : Map(item);
    }

    public async Task<CourseMaterialDto> CreateAsync(CreateCourseMaterialRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var tenantId = _accessScope.GetTenantId();
        var campusId = _accessScope.GetCampusId();

        if (!tenantId.HasValue || !campusId.HasValue)
            throw new UnauthorizedAccessException("A valid tenant and campus scope is required.");

        var materialType = ParseMaterialType(request.MaterialType);

        var entity = new CourseMaterial(
            tenantId.Value,
            campusId.Value,
            request.DepartmentId,
            request.AcademicProgramId,
            request.SemesterId,
            request.CourseId,
            request.Title,
            createdByUserId,
            materialType,
            request.ExternalUrl,
            request.BlobPath,
            request.Description);

        if (!request.IsActive)
            entity.Deactivate();

        await _materials.AddAsync(entity, ct);
        await _materials.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<CourseMaterialDto> UpdateAsync(Guid id, UpdateCourseMaterialRequest request, CancellationToken ct = default)
    {
        var entity = await _materials.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Course material {id} not found.");

        var materialType = ParseMaterialType(request.MaterialType);

        entity.UpdateMetadata(
            request.Title,
            request.Description);
        entity.UpdateLocation(materialType, request.ExternalUrl, request.BlobPath);

        if (request.IsActive)
            entity.Activate();
        else
            entity.Deactivate();

        _materials.Update(entity);
        await _materials.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var entity = await _materials.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Course material {id} not found.");

        if (isActive)
            entity.Activate();
        else
            entity.Deactivate();

        _materials.Update(entity);
        await _materials.SaveChangesAsync(ct);
    }

    private static CourseMaterialType ParseMaterialType(string materialType)
    {
        if (Enum.TryParse<CourseMaterialType>(materialType, true, out var parsed))
            return parsed;

        throw new InvalidOperationException($"Unsupported material type '{materialType}'.");
    }

    private static CourseMaterialDto Map(CourseMaterial item) => new()
    {
        Id = item.Id,
        TenantId = item.TenantId,
        CampusId = item.CampusId,
        DepartmentId = item.DepartmentId,
        AcademicProgramId = item.AcademicProgramId,
        SemesterId = item.SemesterId,
        CourseId = item.CourseId,
        MaterialType = item.MaterialType.ToString(),
        Title = item.Name,
        Description = item.Description,
        ExternalUrl = item.LinkUrl,
        BlobPath = item.FilePath,
        FileName = null,
        FileSizeBytes = null,
        IsActive = item.IsActive,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt ?? item.CreatedAt
    };
}