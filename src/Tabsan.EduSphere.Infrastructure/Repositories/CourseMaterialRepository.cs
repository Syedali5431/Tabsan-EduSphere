using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

public sealed class CourseMaterialRepository : ICourseMaterialRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IAccessScopeResolver? _accessScope;

    public CourseMaterialRepository(ApplicationDbContext db, IAccessScopeResolver? accessScope = null)
    {
        _db = db;
        _accessScope = accessScope;
    }

    private IQueryable<CourseMaterial> ApplyTenantCampusScope(IQueryable<CourseMaterial> query)
    {
        if (_accessScope?.IsSuperAdmin() == true)
            return query;

        var tenantId = _accessScope?.GetTenantId();
        var campusId = _accessScope?.GetCampusId();

        if (!tenantId.HasValue)
            return query.Where(_ => false);

        query = query.Where(m => m.TenantId == tenantId.Value);

        if (campusId.HasValue)
            query = query.Where(m => m.CampusId == campusId.Value);

        return query;
    }

    private bool IsScopeMissingForNonSuperAdmin()
    {
        if (_accessScope?.IsSuperAdmin() == true)
            return false;

        var tenantId = _accessScope?.GetTenantId();
        return !tenantId.HasValue;
    }

    public async Task<IReadOnlyList<CourseMaterial>> GetByFiltersAsync(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        Guid? tenantId,
        Guid? campusId,
        bool activeOnly,
        CancellationToken ct = default)
    {
        if (IsScopeMissingForNonSuperAdmin())
            return Array.Empty<CourseMaterial>();

        var query = ApplyTenantCampusScope(_db.CourseMaterials.AsNoTracking());

        if (departmentId.HasValue)
            query = query.Where(m => m.DepartmentId == departmentId.Value);

        if (academicProgramId.HasValue)
            query = query.Where(m => m.AcademicProgramId == academicProgramId.Value);

        if (semesterId.HasValue)
            query = query.Where(m => m.SemesterId == semesterId.Value);

        if (courseId.HasValue)
            query = query.Where(m => m.CourseId == courseId.Value);

        if (tenantId.HasValue)
            query = query.Where(m => m.TenantId == tenantId.Value);

        if (campusId.HasValue)
            query = query.Where(m => m.CampusId == campusId.Value);

        if (activeOnly)
            query = query.Where(m => m.IsActive);

        return await query
            .OrderBy(m => m.Name)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<CourseMaterial?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (IsScopeMissingForNonSuperAdmin())
            return Task.FromResult<CourseMaterial?>(null);

        return ApplyTenantCampusScope(_db.CourseMaterials.AsNoTracking())
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task AddAsync(CourseMaterial material, CancellationToken ct = default)
        => await _db.CourseMaterials.AddAsync(material, ct);

    public void Update(CourseMaterial material)
        => _db.CourseMaterials.Update(material);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}