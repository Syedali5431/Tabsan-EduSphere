using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Repository interface for AcademicProgram aggregate operations.</summary>
public interface IAcademicProgramRepository
{
    /// <summary>Returns all programmes optionally filtered by department and tenant/campus scope.</summary>
    Task<IReadOnlyList<AcademicProgram>> GetAllAsync(
        Guid? departmentId = null,
        Guid? tenantId = null,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns the programme with the given ID, optionally constrained by tenant/campus scope.</summary>
    Task<AcademicProgram?> GetByIdAsync(Guid id, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns true when the code is already taken inside the given department/scope.</summary>
    Task<bool> CodeExistsAsync(string code, Guid departmentId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Queues the programme for insertion.</summary>
    Task AddAsync(AcademicProgram program, CancellationToken ct = default);

    /// <summary>Marks the programme as modified.</summary>
    void Update(AcademicProgram program);

    /// <summary>Commits changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
