using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>EF Core implementation of IEnrollmentRepository.</summary>
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _db;
    public EnrollmentRepository(ApplicationDbContext db) => _db = db;

    /// <summary>Returns all enrollments for a student, including offering and course details.</summary>
    public async Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.Enrollments
                    .Include(e => e.CourseOffering).ThenInclude(o => o.Course)
                    .Include(e => e.CourseOffering).ThenInclude(o => o.Semester)
                    .Where(e => e.StudentProfileId == studentProfileId)
                    .OrderByDescending(e => e.EnrolledAt)
                    .ToListAsync(ct);

    /// <summary>Returns all active enrollments in a course offering, including student profile + program.</summary>
    // Final-Touches Phase 8 Stage 8.1 — added Program include so ProgramName can be returned in roster
    public async Task<IReadOnlyList<Enrollment>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => await _db.Enrollments
                    .Include(e => e.StudentProfile).ThenInclude(sp => sp.Program)
                    .Where(e => e.CourseOfferingId == courseOfferingId && e.Status == EnrollmentStatus.Active)
                    .ToListAsync(ct);

    /// <summary>Returns all waitlisted enrollments in a course offering, in queue order.</summary>
    public async Task<IReadOnlyList<Enrollment>> GetWaitlistedByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => await _db.Enrollments
                    .Include(e => e.StudentProfile).ThenInclude(sp => sp.Program)
                    .Where(e => e.CourseOfferingId == courseOfferingId && e.Status == EnrollmentStatus.Waitlisted)
                    .OrderBy(e => e.EnrolledAt)
                    .ToListAsync(ct);

    // Final-Touches Phase 8 Stage 8.2 — look up enrollment by its own ID for admin drop
    /// <summary>Returns the enrollment with the given ID, or null.</summary>
    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>Returns the enrollment for the given student + offering pair, or null.</summary>
    public Task<Enrollment?> GetAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
        => _db.Enrollments.FirstOrDefaultAsync(
               e => e.StudentProfileId == studentProfileId && e.CourseOfferingId == courseOfferingId, ct);

    /// <summary>Returns true when an active enrollment already exists for this student/offering pair.</summary>
    public Task<bool> IsEnrolledAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
        => _db.Enrollments.AnyAsync(
               e => e.StudentProfileId == studentProfileId &&
                    e.CourseOfferingId == courseOfferingId &&
                    e.Status == EnrollmentStatus.Active, ct);

    /// <summary>Queues the enrollment for insertion.</summary>
    public async Task AddAsync(Enrollment enrollment, CancellationToken ct = default)
        => await _db.Enrollments.AddAsync(enrollment, ct);

    /// <summary>Marks the enrollment as modified (status change).</summary>
    public void Update(Enrollment enrollment) => _db.Enrollments.Update(enrollment);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>EF Core implementation of IStudentProfileRepository.</summary>
public class StudentProfileRepository : IStudentProfileRepository
{
    private readonly ApplicationDbContext _db;
    public StudentProfileRepository(ApplicationDbContext db) => _db = db;

    /// <summary>Returns the profile linked to the given User ID, with Program and Department loaded.</summary>
    public Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.StudentProfiles
              .Include(sp => sp.Program)
              .Include(sp => sp.Department)
              .FirstOrDefaultAsync(sp => sp.UserId == userId, ct);

    /// <summary>Returns the profile with the given profile ID.</summary>
    public Task<StudentProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StudentProfiles
              .Include(sp => sp.Program)
              .Include(sp => sp.Department)
              .FirstOrDefaultAsync(sp => sp.Id == id, ct);

    /// <summary>Returns the profile matching the given registration number.</summary>
    public Task<StudentProfile?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken ct = default)
        => _db.StudentProfiles
              .FirstOrDefaultAsync(sp => sp.RegistrationNumber == registrationNumber, ct);

    /// <summary>Returns all student profiles, optionally scoped to a department.</summary>
    public async Task<IReadOnlyList<StudentProfile>> GetAllAsync(Guid? departmentId = null, CancellationToken ct = default)
    {
        var query = _db.StudentProfiles
            .Include(sp => sp.Program)
            .Include(sp => sp.Department)
            .AsQueryable();
        if (departmentId.HasValue)
            query = query.Where(sp => sp.DepartmentId == departmentId.Value);
        return await query.OrderBy(sp => sp.RegistrationNumber).ToListAsync(ct);
    }

    /// <summary>Returns true when the registration number is already in use.</summary>
    public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken ct = default)
        => _db.StudentProfiles.AnyAsync(sp => sp.RegistrationNumber == registrationNumber, ct);

    /// <summary>Queues the profile for insertion.</summary>
    public async Task AddAsync(StudentProfile profile, CancellationToken ct = default)
        => await _db.StudentProfiles.AddAsync(profile, ct);

    /// <summary>Marks the profile as modified.</summary>
    public void Update(StudentProfile profile) => _db.StudentProfiles.Update(profile);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>EF Core implementation of IRegistrationWhitelistRepository.</summary>
public class RegistrationWhitelistRepository : IRegistrationWhitelistRepository
{
    private readonly ApplicationDbContext _db;
    public RegistrationWhitelistRepository(ApplicationDbContext db) => _db = db;

    /// <summary>Returns an unused whitelist entry matching the identifier value (case-insensitive).</summary>
    public Task<RegistrationWhitelist?> FindUnusedAsync(string identifierValue, CancellationToken ct = default)
        => _db.RegistrationWhitelist
              .FirstOrDefaultAsync(rw => rw.IdentifierValue == identifierValue.Trim().ToLowerInvariant()
                                         && !rw.IsUsed, ct);

    /// <summary>Queues a new entry for insertion.</summary>
    public async Task AddAsync(RegistrationWhitelist entry, CancellationToken ct = default)
        => await _db.RegistrationWhitelist.AddAsync(entry, ct);

    /// <summary>Queues multiple entries for bulk insertion.</summary>
    public async Task AddRangeAsync(IEnumerable<RegistrationWhitelist> entries, CancellationToken ct = default)
        => await _db.RegistrationWhitelist.AddRangeAsync(entries, ct);

    /// <summary>Marks an entry as modified (consumed after registration).</summary>
    public void Update(RegistrationWhitelist entry) => _db.RegistrationWhitelist.Update(entry);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>EF Core implementation of IFacultyAssignmentRepository.</summary>
public class FacultyAssignmentRepository : IFacultyAssignmentRepository
{
    private readonly ApplicationDbContext _db;
    public FacultyAssignmentRepository(ApplicationDbContext db) => _db = db;

    /// <summary>Returns all active department assignments for the given faculty user.</summary>
    public async Task<IReadOnlyList<FacultyDepartmentAssignment>> GetByFacultyAsync(Guid facultyUserId, CancellationToken ct = default)
        => await _db.FacultyDepartmentAssignments
                    .Include(a => a.Department)
                    .Where(a => a.FacultyUserId == facultyUserId && a.RemovedAt == null)
                    .ToListAsync(ct);

    /// <summary>Returns all active faculty assignments for the given department.</summary>
    public async Task<IReadOnlyList<FacultyDepartmentAssignment>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
        => await _db.FacultyDepartmentAssignments
                    .Where(a => a.DepartmentId == departmentId && a.RemovedAt == null)
                    .ToListAsync(ct);

    /// <summary>Returns the active assignment for the given faculty + department pair, or null.</summary>
    public Task<FacultyDepartmentAssignment?> GetAsync(Guid facultyUserId, Guid departmentId, CancellationToken ct = default)
        => _db.FacultyDepartmentAssignments
              .FirstOrDefaultAsync(a => a.FacultyUserId == facultyUserId &&
                                        a.DepartmentId == departmentId &&
                                        a.RemovedAt == null, ct);

    /// <summary>Returns the list of department IDs the faculty user is actively assigned to.</summary>
    public async Task<IReadOnlyList<Guid>> GetDepartmentIdsForFacultyAsync(Guid facultyUserId, CancellationToken ct = default)
        => await _db.FacultyDepartmentAssignments
                    .Where(a => a.FacultyUserId == facultyUserId && a.RemovedAt == null)
                    .Select(a => a.DepartmentId)
                    .ToListAsync(ct);

    /// <summary>Queues the assignment for insertion.</summary>
    public async Task AddAsync(FacultyDepartmentAssignment assignment, CancellationToken ct = default)
        => await _db.FacultyDepartmentAssignments.AddAsync(assignment, ct);

    /// <summary>Marks the assignment as modified.</summary>
    public void Update(FacultyDepartmentAssignment assignment)
        => _db.FacultyDepartmentAssignments.Update(assignment);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>EF Core implementation of IAdminAssignmentRepository.</summary>
public class AdminAssignmentRepository : IAdminAssignmentRepository
{
    private readonly ApplicationDbContext _db;
    public AdminAssignmentRepository(ApplicationDbContext db) => _db = db;

    /// <summary>Returns all active department assignments for the given admin user.</summary>
    public async Task<IReadOnlyList<AdminDepartmentAssignment>> GetByAdminAsync(Guid adminUserId, CancellationToken ct = default)
        => await _db.AdminDepartmentAssignments
                    .Include(a => a.Department)
                    .Where(a => a.AdminUserId == adminUserId && a.RemovedAt == null)
                    .ToListAsync(ct);

    /// <summary>Returns all active admin assignments for the given department.</summary>
    public async Task<IReadOnlyList<AdminDepartmentAssignment>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
        => await _db.AdminDepartmentAssignments
                    .Where(a => a.DepartmentId == departmentId && a.RemovedAt == null)
                    .ToListAsync(ct);

    /// <summary>Returns the active assignment for the given admin + department pair, or null.</summary>
    public Task<AdminDepartmentAssignment?> GetAsync(Guid adminUserId, Guid departmentId, CancellationToken ct = default)
        => _db.AdminDepartmentAssignments
              .FirstOrDefaultAsync(a => a.AdminUserId == adminUserId &&
                                        a.DepartmentId == departmentId &&
                                        a.RemovedAt == null, ct);

    /// <summary>Returns the list of department IDs the admin user is actively assigned to.</summary>
    public async Task<IReadOnlyList<Guid>> GetDepartmentIdsForAdminAsync(Guid adminUserId, CancellationToken ct = default)
        => await _db.AdminDepartmentAssignments
                    .Where(a => a.AdminUserId == adminUserId && a.RemovedAt == null)
                    .Select(a => a.DepartmentId)
                    .ToListAsync(ct);

    /// <summary>Queues the assignment for insertion.</summary>
    public async Task AddAsync(AdminDepartmentAssignment assignment, CancellationToken ct = default)
        => await _db.AdminDepartmentAssignments.AddAsync(assignment, ct);

    /// <summary>Marks the assignment as modified.</summary>
    public void Update(AdminDepartmentAssignment assignment)
        => _db.AdminDepartmentAssignments.Update(assignment);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
