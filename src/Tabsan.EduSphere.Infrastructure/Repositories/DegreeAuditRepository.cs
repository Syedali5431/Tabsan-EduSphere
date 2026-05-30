// Final-Touches Phase 17 Stage 17.1/17.2 — Degree Audit repository implementation

using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>EF Core implementation of IDegreeAuditRepository.</summary>
public class DegreeAuditRepository : IDegreeAuditRepository
{
    private readonly ApplicationDbContext _db;

    public DegreeAuditRepository(ApplicationDbContext db) => _db = db;

    // ── DegreeRule CRUD ───────────────────────────────────────────────────────

    public Task<DegreeRule?> GetRuleByProgramAsync(
        Guid academicProgramId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null)
    {
        var query = _db.DegreeRules
            .Include(r => r.AcademicProgram)
                .ThenInclude(p => p.Department)
            .Include(r => r.RequiredCourses)
                .ThenInclude(rc => rc.Course)
            .Where(r => r.AcademicProgramId == academicProgramId);

        query = ApplyTenantCampusScope(query, tenantId, campusId);
        return query.FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<DegreeRule>> GetAllRulesAsync(
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null)
    {
        var query = _db.DegreeRules
            .Include(r => r.AcademicProgram)
                .ThenInclude(p => p.Department)
            .Include(r => r.RequiredCourses)
                .ThenInclude(rc => rc.Course)
            .AsQueryable();

        query = ApplyTenantCampusScope(query, tenantId, campusId);
        return await query
            .OrderBy(r => r.AcademicProgram.Name)
            .ToListAsync(ct);
    }

    public Task<DegreeRule?> GetRuleByIdAsync(Guid ruleId, CancellationToken ct = default)
        => _db.DegreeRules
              .Include(r => r.AcademicProgram)
                  .ThenInclude(p => p.Department)
              .Include(r => r.RequiredCourses)
                  .ThenInclude(rc => rc.Course)
              .FirstOrDefaultAsync(r => r.Id == ruleId, ct);

    public async Task AddRuleAsync(DegreeRule rule, CancellationToken ct = default)
        => await _db.DegreeRules.AddAsync(rule, ct);

    public void UpdateRule(DegreeRule rule) => _db.DegreeRules.Update(rule);

    // ── Degree Audit Queries ─────────────────────────────────────────────────

    // Final-Touches Phase 17 Stage 17.1 — join Results → CourseOffering → Course for credit aggregation
    public async Task<IReadOnlyList<CreditRow>> GetEarnedCreditsAsync(
        Guid studentProfileId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null)
    {
        var rows = await _db.Set<Domain.Assignments.Result>()
            .Where(r => r.StudentProfileId == studentProfileId
                     && r.IsPublished
                     && r.GradePoint.HasValue
                     && r.GradePoint.Value >= 1.0m)
            .Join(
                _db.Set<CourseOffering>(),
                r  => r.CourseOfferingId,
                co => co.Id,
                (r, co) => new { r, co })
            .Join(
                _db.Set<Course>(),
                rco => rco.co.CourseId,
                c   => c.Id,
                (rco, c) => new { rco.r, rco.co, c })
            .Join(
                _db.Set<Department>(),
                rcoc => rcoc.c.DepartmentId,
                d => d.Id,
                (rcoc, d) => new { rcoc.r, rcoc.co, rcoc.c, d })
            .Where(x => !tenantId.HasValue || (x.co.TenantId ?? x.c.TenantId ?? x.d.TenantId) == tenantId.Value)
            .Where(x => !campusId.HasValue || (x.co.CampusId ?? x.c.CampusId ?? x.d.CampusId) == campusId.Value)
            .Select(x => new CreditRow(
                x.co.Id,
                x.c.Id,
                x.c.Code,
                x.c.Title,
                x.c.CreditHours,
                x.c.CourseType,
                x.r.ResultType,
                x.r.GradePoint))
            .ToListAsync(ct);

        return rows;
    }

    private static IQueryable<DegreeRule> ApplyTenantCampusScope(
        IQueryable<DegreeRule> query,
        Guid? tenantId,
        Guid? campusId)
    {
        if (tenantId.HasValue)
            query = query.Where(r => r.AcademicProgram.Department.TenantId == tenantId.Value);

        if (campusId.HasValue)
            query = query.Where(r => r.AcademicProgram.Department.CampusId == campusId.Value);

        return query;
    }

    public async Task<Guid?> GetStudentProgramIdAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.StudentProfiles
                    .Where(sp => sp.Id == studentProfileId)
                    .Select(sp => (Guid?)sp.ProgramId)
                    .FirstOrDefaultAsync(ct);

    // Final-Touches Phase 17 Stage 17.1 — lookup username for audit display
    public async Task<string> GetUsernameAsync(Guid userId, CancellationToken ct = default)
        => await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Username)
                    .FirstOrDefaultAsync(ct) ?? "";

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
