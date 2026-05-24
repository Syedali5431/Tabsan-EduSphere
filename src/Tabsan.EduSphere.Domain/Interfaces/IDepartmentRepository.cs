using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Repository interface for Department aggregate operations.</summary>
public interface IDepartmentRepository
{
    /// <summary>Returns all departments ordered by name (soft-delete filter applied).</summary>
    Task<IReadOnlyList<Department>> GetAllAsync(Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns the department with the given ID, or null.</summary>
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns true when the uppercase code is already in use.</summary>
    Task<bool> CodeExistsAsync(string code, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Queues the department for insertion.</summary>
    Task AddAsync(Department department, CancellationToken ct = default);

    /// <summary>Marks the department as modified.</summary>
    void Update(Department department);

    /// <summary>Commits changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
