// Final-Touches Phase 16 Stage 16.2 — RubricService implementation

using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Manages rubric lifecycle (create/update/delete) and student rubric grading.
/// </summary>
public class RubricService : IRubricService
{
    // Final-Touches Phase 16 Stage 16.2 — dependencies
    private readonly IRubricRepository _rubricRepo;
    private readonly ISubmissionWorkspaceRepository? _submissions;
    private readonly IAssignmentGradebookSync? _gradebook;

    public RubricService(IRubricRepository rubricRepo, ISubmissionWorkspaceRepository? submissions = null, IAssignmentGradebookSync? gradebook = null)
    {
        _rubricRepo = rubricRepo;
        _submissions = submissions;
        _gradebook = gradebook;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<RubricResponse?> GetByAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.2 — fetch rubric with criteria + levels
        var rubric = await _rubricRepo.GetByAssignmentAsync(assignmentId, ct);
        return rubric is null ? null : MapRubric(rubric);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Guid> CreateAsync(CreateRubricRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.2 — build complete rubric graph
        var rubric = Rubric.Create(request.AssignmentId, request.Title, createdByUserId);

        // Plan M3 fix: criteria and levels are attached to the rubric (they were created but never saved).
        foreach (var cReq in request.Criteria.OrderBy(c => c.DisplayOrder))
        {
            var criterion = rubric.AddCriterion(cReq.Name, cReq.MaxPoints, cReq.DisplayOrder);
            foreach (var lReq in cReq.Levels.OrderBy(l => l.DisplayOrder))
            {
                criterion.AddLevel(lReq.Label, lReq.PointsAwarded, lReq.DisplayOrder);
            }
        }

        await _rubricRepo.AddAsync(rubric, ct);
        await _rubricRepo.SaveChangesAsync(ct);
        return rubric.Id;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task UpdateAsync(Guid rubricId, UpdateRubricRequest request, Guid updatedByUserId, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.2 — update rubric title only
        var rubric = await _rubricRepo.GetByIdAsync(rubricId, ct)
            ?? throw new KeyNotFoundException($"Rubric {rubricId} not found.");
        rubric.Update(request.Title, updatedByUserId);
        _rubricRepo.Update(rubric);
        await _rubricRepo.SaveChangesAsync(ct);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(Guid rubricId, Guid updatedByUserId, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.2 — soft-delete via deactivate
        var rubric = await _rubricRepo.GetByIdAsync(rubricId, ct)
            ?? throw new KeyNotFoundException($"Rubric {rubricId} not found.");
        rubric.Deactivate(updatedByUserId);
        _rubricRepo.Update(rubric);
        await _rubricRepo.SaveChangesAsync(ct);
    }

    // ── Grade submission ──────────────────────────────────────────────────────

    public async Task<RubricGradeResponse> GradeSubmissionAsync(
        RubricGradeRequest request,
        Guid gradedByUserId,
        CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.2 — apply per-criterion level selections
        // Rubric must be loaded; we find it via the first criterion in the request
        if (request.Grades.Count == 0)
            throw new InvalidOperationException("At least one criterion grade is required.");

        var criterionId = request.Grades[0].CriterionId;
        // We look up the rubric that owns this criterion by traversing through IRubricRepository
        // by getting any existing grade for the submission (or by loading the rubric linked to the assignment).
        // Use the rubric loaded from the repo for validation.

        // Plan M3 fix: points come from the chosen level of the rubric that owns the criterion
        // (they were always stored as 0), and a level must belong to its criterion.
        var rubric = await _rubricRepo.GetByCriterionAsync(criterionId, ct)
            ?? throw new KeyNotFoundException("The rubric for this criterion was not found.");

        foreach (var gradeReq in request.Grades)
        {
            var points = ResolveLevelPoints(rubric, gradeReq.CriterionId, gradeReq.LevelId);
            var existing = await _rubricRepo.GetStudentGradeAsync(request.SubmissionId, gradeReq.CriterionId, ct);
            if (existing is not null)
            {
                existing.Update(gradeReq.LevelId, points, gradedByUserId);
                _rubricRepo.UpdateStudentGrade(existing);
            }
            else
            {
                var grade = RubricStudentGrade.Create(
                    request.SubmissionId,
                    gradeReq.CriterionId,
                    gradeReq.LevelId,
                    points,
                    gradedByUserId);
                await _rubricRepo.AddStudentGradeAsync(grade, ct);
            }
        }

        await _rubricRepo.SaveChangesAsync(ct);

        // Plan M6: rubric criteria can be aligned to outcomes, so refresh the submission's evidence.
        if (_submissions is not null && _gradebook is not null
            && await _submissions.GetSubmissionAsync(request.SubmissionId, false, ct) is { } submission
            && await _submissions.GetAssignmentAsync(submission.AssignmentId, ct) is { } assignment)
            await _gradebook.RefreshAsync(assignment.CourseOfferingId, submission.StudentProfileId, ct);
        return (await GetSubmissionGradeAsync(Guid.Empty, request.SubmissionId, ct))!;
    }

    // ── Get grade ─────────────────────────────────────────────────────────────

    public async Task<RubricGradeResponse?> GetSubmissionGradeAsync(
        Guid rubricId,
        Guid submissionId,
        CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.2 — build rubric grade response for a submission
        var grades = await _rubricRepo.GetStudentGradesForSubmissionAsync(submissionId, ct);
        if (grades.Count == 0) return null;

        // Load the rubric that the first grade's criterion belongs to
        // We need the rubric; find it by loading via any criterion
        // (The rubric is identified by rubricId arg if nonzero, otherwise we build a basic response)
        var criteriaResults = grades.Select(g => new RubricCriterionGradeResult
        {
            CriterionId   = g.RubricCriterionId,
            CriterionName = "",  // populated by controller from rubric data
            MaxPoints     = 0,
            ChosenLevelId = g.RubricLevelId,
            ChosenLabel   = "",
            PointsAwarded = g.PointsAwarded
        }).ToList();

        return new RubricGradeResponse
        {
            SubmissionId   = submissionId,
            RubricId       = rubricId,
            RubricTitle    = "",
            TotalPoints    = grades.Sum(g => g.PointsAwarded),
            MaxTotalPoints = 0,
            CriteriaResults = criteriaResults
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static decimal ResolveLevelPoints(Rubric rubric, Guid criterionId, Guid levelId)
    {
        var criterion = rubric.Criteria.FirstOrDefault(c => c.Id == criterionId)
            ?? throw new InvalidOperationException("A criterion does not belong to this rubric.");
        var level = criterion.Levels.FirstOrDefault(l => l.Id == levelId)
            ?? throw new InvalidOperationException($"That level does not belong to \"{criterion.Name}\".");
        return level.PointsAwarded;
    }

    private static RubricResponse MapRubric(Rubric rubric) => new()
    {
        RubricId     = rubric.Id,
        AssignmentId = rubric.AssignmentId,
        Title        = rubric.Title,
        IsActive     = rubric.IsActive,
        Criteria = rubric.Criteria.OrderBy(c => c.DisplayOrder).Select(c => new RubricCriterionDto
        {
            CriterionId  = c.Id,
            Name         = c.Name,
            MaxPoints    = c.MaxPoints,
            DisplayOrder = c.DisplayOrder,
            Levels = c.Levels.OrderBy(l => l.DisplayOrder).Select(l => new RubricLevelDto
            {
                LevelId       = l.Id,
                Label         = l.Label,
                PointsAwarded = l.PointsAwarded,
                DisplayOrder  = l.DisplayOrder
            }).ToList()
        }).ToList()
    };
}
