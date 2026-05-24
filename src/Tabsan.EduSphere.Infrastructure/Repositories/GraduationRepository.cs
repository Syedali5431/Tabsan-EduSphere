// Final-Touches Phase 18 Stage 18.1 — Graduation repository EF implementation

using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Fyp;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>EF Core implementation of IGraduationRepository.</summary>
public class GraduationRepository : IGraduationRepository
{
    private readonly ApplicationDbContext _db;

    public GraduationRepository(ApplicationDbContext db) => _db = db;

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<GraduationApplication>> GetByStudentAsync(
        Guid studentProfileId, CancellationToken ct = default)
        => await _db.GraduationApplications
                    .Include(a => a.Approvals)
                    .Where(a => a.StudentProfileId == studentProfileId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<GraduationApplication>> GetByStudentPagedAsync(
        Guid studentProfileId, int skip, int take, CancellationToken ct = default)
        => await _db.GraduationApplications
                    .Include(a => a.Approvals)
                    .Where(a => a.StudentProfileId == studentProfileId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);

    public Task<int> CountByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => _db.GraduationApplications
              .Where(a => a.StudentProfileId == studentProfileId)
              .CountAsync(ct);

    public Task<GraduationApplication?> GetActiveByStudentAsync(
        Guid studentProfileId, CancellationToken ct = default)
        => _db.GraduationApplications
              .Include(a => a.Approvals)
              .Where(a => a.StudentProfileId == studentProfileId
                       && a.Status != GraduationApplicationStatus.Rejected)
              .OrderByDescending(a => a.CreatedAt)
              .FirstOrDefaultAsync(ct);

    public Task<GraduationApplication?> GetByIdAsync(
        Guid applicationId, CancellationToken ct = default)
        => _db.GraduationApplications
              .Include(a => a.Approvals)
              .Include(a => a.StudentProfile)
              .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

    public async Task<IReadOnlyList<GraduationApplication>> GetByStatusAsync(
        GraduationApplicationStatus status, CancellationToken ct = default)
        => await _db.GraduationApplications
                    .Include(a => a.Approvals)
                    .Where(a => a.Status == status)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync(ct);

    // Final-Touches Phase 18 Stage 18.1 — filter by department via StudentProfile join
    public async Task<IReadOnlyList<GraduationApplication>> GetByDepartmentAsync(
        Guid departmentId, GraduationApplicationStatus? status, CancellationToken ct = default)
        => await _db.GraduationApplications
                    .Include(a => a.Approvals)
                    .Include(a => a.StudentProfile)
                    .Where(a => a.StudentProfile.DepartmentId == departmentId
                             && (status == null || a.Status == status))
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<GraduationApplication>> GetByDepartmentPagedAsync(
        Guid departmentId, GraduationApplicationStatus? status, int skip, int take, CancellationToken ct = default)
        => await _db.GraduationApplications
                    .Include(a => a.Approvals)
                    .Include(a => a.StudentProfile)
                    .Where(a => a.StudentProfile.DepartmentId == departmentId
                             && (status == null || a.Status == status))
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);

    public Task<int> CountByDepartmentAsync(Guid departmentId, GraduationApplicationStatus? status, CancellationToken ct = default)
        => _db.GraduationApplications
              .Include(a => a.StudentProfile)
              .Where(a => a.StudentProfile.DepartmentId == departmentId
                       && (status == null || a.Status == status))
              .CountAsync(ct);

    public async Task<IReadOnlyList<GraduationApplication>> GetAllAsync(
        GraduationApplicationStatus? status, CancellationToken ct = default)
        => await _db.GraduationApplications
                    .Include(a => a.Approvals)
                    .Include(a => a.StudentProfile)
                    .Where(a => status == null || a.Status == status)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<GraduationApplication>> GetAllPagedAsync(
        GraduationApplicationStatus? status, int skip, int take, CancellationToken ct = default)
        => await _db.GraduationApplications
                    .Include(a => a.Approvals)
                    .Include(a => a.StudentProfile)
                    .Where(a => status == null || a.Status == status)
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);

    public Task<int> CountAllAsync(GraduationApplicationStatus? status, CancellationToken ct = default)
        => _db.GraduationApplications
              .Where(a => status == null || a.Status == status)
              .CountAsync(ct);

    // ── Lookup helpers ────────────────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — return Username as display name
    public async Task<string> GetStudentDisplayNameAsync(
        Guid studentProfileId, CancellationToken ct = default)
    {
        var name = await _db.StudentProfiles
            .Where(sp => sp.Id == studentProfileId)
            .Join(_db.Set<Domain.Identity.User>(),
                  sp => sp.UserId,
                  u  => u.Id,
                  (sp, u) => u.Username)
            .FirstOrDefaultAsync(ct);
        return name ?? "Unknown";
    }

    public async Task<string> GetStudentRegistrationNumberAsync(
        Guid studentProfileId, CancellationToken ct = default)
    {
        var regNo = await _db.StudentProfiles
            .Where(sp => sp.Id == studentProfileId)
            .Select(sp => sp.RegistrationNumber)
            .FirstOrDefaultAsync(ct);
        return regNo ?? string.Empty;
    }

    public async Task<string> GetStudentProgramNameAsync(
        Guid studentProfileId, CancellationToken ct = default)
    {
        var name = await _db.StudentProfiles
            .Include(sp => sp.Program)
            .Where(sp => sp.Id == studentProfileId)
            .Select(sp => sp.Program.Name)
            .FirstOrDefaultAsync(ct);
        return name ?? string.Empty;
    }

    public Task<bool> HasCompletedFypProjectAsync(Guid studentProfileId, CancellationToken ct = default)
        => _db.FypProjects
              .AnyAsync(p => p.StudentProfileId == studentProfileId
                          && p.Status == FypProjectStatus.Completed, ct);

    // Final-Touches Phase 18 Stage 18.1 — faculty user IDs for a department
    public async Task<IReadOnlyList<Guid>> GetFacultyUserIdsByDepartmentAsync(
        Guid departmentId, CancellationToken ct = default)
        => await _db.FacultyDepartmentAssignments
                    .Where(a => a.DepartmentId == departmentId && a.IsActive)
                    .Select(a => a.FacultyUserId)
                    .Distinct()
                    .ToListAsync(ct);

    // Final-Touches Phase 18 Stage 18.1 — admin user IDs for a department
    public async Task<IReadOnlyList<Guid>> GetAdminUserIdsByDepartmentAsync(
        Guid departmentId, CancellationToken ct = default)
        => await _db.AdminDepartmentAssignments
                    .Where(a => a.DepartmentId == departmentId && a.IsActive)
                    .Select(a => a.AdminUserId)
                    .Distinct()
                    .ToListAsync(ct);

    // Final-Touches Phase 18 Stage 18.1 — all SuperAdmin user IDs (by role name)
    public async Task<IReadOnlyList<Guid>> GetSuperAdminUserIdsAsync(CancellationToken ct = default)
        => await _db.Set<Domain.Identity.User>()
                    .Include(u => u.Role)
                    .Where(u => u.Role.Name == "SuperAdmin" && u.IsActive && !u.IsDeleted)
                    .Select(u => u.Id)
                    .ToListAsync(ct);

    // ── Mutations ─────────────────────────────────────────────────────────────

    public async Task AddAsync(GraduationApplication application, CancellationToken ct = default)
        => await _db.GraduationApplications.AddAsync(application, ct);

    public void Update(GraduationApplication application)
        => _db.GraduationApplications.Update(application);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
