using Tabsan.EduSphere.Application.DTOs.Lms;

namespace Tabsan.EduSphere.Application.Interfaces;

public interface ICourseMaterialService
{
    Task<IReadOnlyList<CourseMaterialDto>> GetAllAsync(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        bool activeOnly,
        CancellationToken ct = default);

    Task<CourseMaterialDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<CourseMaterialDto> CreateAsync(CreateCourseMaterialRequest request, Guid createdByUserId, CancellationToken ct = default);

    Task<CourseMaterialDto> UpdateAsync(Guid id, UpdateCourseMaterialRequest request, CancellationToken ct = default);

    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
}