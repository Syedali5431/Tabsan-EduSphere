// Final-Touches Phase 16 Stage 16.1/16.2 — GradebookRepository and RubricRepository

using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>
/// Efficient EF query that joins enrollments, student profiles, and identity users
/// in a single round-trip to build the gradebook student list.
/// </summary>
public class GradebookRepository : IGradebookRepository
{
    // Final-Touches Phase 16 Stage 16.1 — single join to get student identity for gradebook
    private readonly ApplicationDbContext _db;

    public GradebookRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<GradebookStudentInfo>> GetStudentsForOfferingAsync(
        Guid courseOfferingId,
        CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.1 — join enrollments → student_profiles → users
        var rows = await _db.Enrollments
            .Where(e => e.CourseOfferingId == courseOfferingId
                     && e.Status == Domain.Academic.EnrollmentStatus.Active)
            .Join(_db.StudentProfiles,
                  e  => e.StudentProfileId,
                  sp => sp.Id,
                  (e, sp) => new { sp.Id, sp.RegistrationNumber, sp.UserId })
            .Join(_db.Users,
                  sp  => sp.UserId,
                  u   => u.Id,
                (sp, u) => new { sp.Id, sp.RegistrationNumber, u.Username })
            .OrderBy(r => r.RegistrationNumber)
            .Select(r => new GradebookStudentInfo(
                r.Id,
                r.RegistrationNumber,
                r.Username))
            .ToListAsync(ct);

        return rows;
    }
}

/// <summary>
/// EF Core implementation of IRubricRepository.
/// Loads the full rubric graph (criteria + levels) eagerly.
/// </summary>
public class RubricRepository : IRubricRepository
{
    // Final-Touches Phase 16 Stage 16.2 — rubric CRUD
    private readonly ApplicationDbContext _db;

    public RubricRepository(ApplicationDbContext db) => _db = db;

    public Task<Rubric?> GetByAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
        => _db.Rubrics
              .Include(r => r.Criteria)
              .ThenInclude(c => c.Levels)
              .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.IsActive, ct);

    public Task<Rubric?> GetByIdAsync(Guid rubricId, CancellationToken ct = default)
        => _db.Rubrics
              .Include(r => r.Criteria)
              .ThenInclude(c => c.Levels)
              .FirstOrDefaultAsync(r => r.Id == rubricId, ct);

    public async Task AddAsync(Rubric rubric, CancellationToken ct = default)
        => await _db.Rubrics.AddAsync(rubric, ct);

    public void Update(Rubric rubric) => _db.Rubrics.Update(rubric);

    public Task<RubricStudentGrade?> GetStudentGradeAsync(
        Guid submissionId,
        Guid criterionId,
        CancellationToken ct = default)
        => _db.RubricStudentGrades
              .FirstOrDefaultAsync(
                  g => g.AssignmentSubmissionId == submissionId
                    && g.RubricCriterionId == criterionId,
                  ct);

    public async Task<IReadOnlyList<RubricStudentGrade>> GetStudentGradesForSubmissionAsync(
        Guid submissionId,
        CancellationToken ct = default)
        => await _db.RubricStudentGrades
                    .Where(g => g.AssignmentSubmissionId == submissionId)
                    .ToListAsync(ct);

    public async Task AddStudentGradeAsync(RubricStudentGrade grade, CancellationToken ct = default)
        => await _db.RubricStudentGrades.AddAsync(grade, ct);

    public void UpdateStudentGrade(RubricStudentGrade grade)
        => _db.RubricStudentGrades.Update(grade);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
