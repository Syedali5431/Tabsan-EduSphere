using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Domain.Interfaces;

public interface ICourseMaterialRepository
{
    Task<IReadOnlyList<CourseMaterial>> GetByFiltersAsync(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        bool activeOnly,
        CancellationToken ct = default);

    Task<CourseMaterial?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(CourseMaterial material, CancellationToken ct = default);
    void Update(CourseMaterial material);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}