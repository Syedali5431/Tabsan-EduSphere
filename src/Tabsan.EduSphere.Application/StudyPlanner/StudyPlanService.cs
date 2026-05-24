using Tabsan.EduSphere.Application.DTOs.StudyPlanner;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.StudyPlanner;

namespace Tabsan.EduSphere.Application.StudyPlanner;

// Final-Touches Phase 21 Stage 21.1/21.2 — Study Planner service implementation

/// <summary>
/// Implements <see cref="IStudyPlanService"/> to manage student semester study plans,
/// validate prerequisites and credit-load limits, handle advisor endorsements,
/// and generate course recommendations (Stage 21.2).
/// </summary>
public sealed class StudyPlanService : IStudyPlanService
{
    private readonly IStudyPlanRepository        _plans;
    private readonly ICourseRepository           _courses;
    private readonly IPrerequisiteRepository     _prereqs;
    private readonly IDegreeAuditRepository      _audit;
    private readonly IStudentProfileRepository   _students;
    private readonly IAcademicProgramRepository  _programs;

    public StudyPlanService(
        IStudyPlanRepository       plans,
        ICourseRepository          courses,
        IPrerequisiteRepository    prereqs,
        IDegreeAuditRepository     audit,
        IStudentProfileRepository  students,
        IAcademicProgramRepository programs)
    {
        _plans    = plans;
        _courses  = courses;
        _prereqs  = prereqs;
        _audit    = audit;
        _students = students;
        _programs = programs;
    }

    // ── Stage 21.1: Plan CRUD ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<List<StudyPlanDto>> GetPlansAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var planList = await _plans.GetByStudentAsync(studentProfileId, ct);
        return planList.Select(MapDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<StudyPlanDto>> GetPlansByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
    {
        var planList = await _plans.GetByDepartmentAsync(departmentId, ct);
        return planList.Select(MapDto).ToList();
    }

    /// <inheritdoc />
    public async Task<StudyPlanDto?> GetPlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(planId, ct);
        return plan is null ? null : MapDto(plan);
    }

    /// <inheritdoc />
    public async Task<StudyPlanDto> CreatePlanAsync(CreateStudyPlanRequest request, CancellationToken ct = default)
    {
        // Verify student profile exists
        var profile = await _students.GetByIdAsync(request.StudentProfileId, ct)
            ?? throw new KeyNotFoundException($"Student profile {request.StudentProfileId} not found.");

        // Check for a pre-existing plan with the same semester label
        var existing = await _plans.GetByStudentAsync(request.StudentProfileId, ct);
        if (existing.Any(p => string.Equals(p.PlannedSemesterName, request.PlannedSemesterName.Trim(),
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A study plan for '{request.PlannedSemesterName}' already exists for this student.");
        }

        var plan = new StudyPlan(request.StudentProfileId, request.PlannedSemesterName, request.Notes);
        await _plans.AddAsync(plan, ct);
        await _plans.SaveChangesAsync(ct);

        // Re-load so EF populates navigation properties
        var saved = await _plans.GetByIdAsync(plan.Id, ct) ?? plan;
        return MapDto(saved);
    }

    /// <inheritdoc />
    public async Task<StudyPlanDto> AddCourseAsync(AddPlanCourseRequest request, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(request.PlanId, ct)
            ?? throw new KeyNotFoundException($"Study plan {request.PlanId} not found.");

        // 1. Validate course exists and is semester-based
        var course = await _courses.GetByIdAsync(request.CourseId, ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} not found.");

        if (!course.HasSemesters)
            throw new InvalidOperationException(
                $"Course '{course.Code}' is not a semester-based course and cannot be added to a study plan.");

        if (!course.IsActive)
            throw new InvalidOperationException($"Course '{course.Code}' is not active.");

        // 2. Validate prerequisites — student must have passed all prerequisite courses
        var prereqList = await _prereqs.GetByCourseIdAsync(request.CourseId, ct);
        if (prereqList.Count > 0)
        {
            var earnedCredits = await _audit.GetEarnedCreditsAsync(plan.StudentProfileId, ct);
            var passedCourseIds = earnedCredits.Select(c => c.CourseId).ToHashSet();

            var unmet = prereqList
                .Where(p => !passedCourseIds.Contains(p.PrerequisiteCourseId))
                .Select(p => p.PrerequisiteCourse?.Code ?? p.PrerequisiteCourseId.ToString())
                .ToList();

            if (unmet.Count > 0)
                throw new InvalidOperationException(
                    $"Prerequisite(s) not met for '{course.Code}': {string.Join(", ", unmet)}.");
        }

        // 3. Validate credit-load limit from academic programme
        var profile = await _students.GetByIdAsync(plan.StudentProfileId, ct)
            ?? throw new InvalidOperationException("Associated student profile not found.");

        var program = await _programs.GetByIdAsync(profile.ProgramId, ct: ct);
        int maxLoad = program?.MaxCreditLoadPerSemester ?? 18;

        // Sum credits already in this plan + proposed course
        int currentLoad = plan.Courses.Sum(c => c.Course?.CreditHours ?? 0);
        if (currentLoad + course.CreditHours > maxLoad)
            throw new InvalidOperationException(
                $"Adding '{course.Code}' ({course.CreditHours} cr) would exceed the maximum credit load of {maxLoad} for this programme.");

        plan.AddCourse(request.CourseId);
        plan.ResetAdvisorStatus();
        _plans.Update(plan);
        await _plans.SaveChangesAsync(ct);

        var updated = await _plans.GetByIdAsync(plan.Id, ct) ?? plan;
        return MapDto(updated);
    }

    /// <inheritdoc />
    public async Task RemoveCourseAsync(Guid planId, Guid courseId, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Study plan {planId} not found.");

        plan.RemoveCourse(courseId);
        plan.ResetAdvisorStatus();
        _plans.Update(plan);
        await _plans.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeletePlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Study plan {planId} not found.");

        plan.SoftDelete();
        _plans.Update(plan);
        await _plans.SaveChangesAsync(ct);
    }

    // ── Stage 21.1: Advisor workflow ──────────────────────────────────────────

    /// <inheritdoc />
    public async Task AdvisePlanAsync(AdvisePlanRequest request, Guid advisorUserId, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(request.PlanId, ct)
            ?? throw new KeyNotFoundException($"Study plan {request.PlanId} not found.");

        if (request.IsEndorsed)
        {
            plan.Endorse(advisorUserId, request.AdvisorNotes);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.AdvisorNotes))
                throw new ArgumentException("Advisor notes are required when rejecting a plan.", nameof(request.AdvisorNotes));
            plan.Reject(advisorUserId, request.AdvisorNotes!);
        }

        _plans.Update(plan);
        await _plans.SaveChangesAsync(ct);
    }

    // ── Stage 21.2: Recommendations ──────────────────────────────────────────

    /// <inheritdoc />
    public async Task<StudyPlanRecommendationDto> GetRecommendationsAsync(
        Guid   studentProfileId,
        string plannedSemesterName,
        CancellationToken ct = default)
    {
        // 1. Student profile + program
        var profile = await _students.GetByIdAsync(studentProfileId, ct)
            ?? throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        var program = await _programs.GetByIdAsync(profile.ProgramId, ct: ct);
        int maxLoad = program?.MaxCreditLoadPerSemester ?? 18;

        // 2. Degree rule for the student's programme (required courses)
        var degreeRule = await _audit.GetRuleByProgramAsync(profile.ProgramId, ct);
        var requiredCourseIds = degreeRule?.RequiredCourses
            .Select(r => r.CourseId)
            .ToHashSet() ?? new HashSet<Guid>();

        // 3. Already passed courses
        var earnedCredits  = await _audit.GetEarnedCreditsAsync(studentProfileId, ct);
        var passedIds      = earnedCredits.Select(c => c.CourseId).ToHashSet();

        // 4. Already planned courses (all plans for this student)
        var allPlans       = await _plans.GetByStudentAsync(studentProfileId, ct);
        var plannedIds     = allPlans
            .SelectMany(p => p.Courses)
            .Select(c => c.CourseId)
            .ToHashSet();

        // 5. Available semester-based courses in the student's department
        var available = await _courses.GetAllAsync(
            departmentId:  profile.DepartmentId,
            hasSemesters:  true,
            ct:            ct);

        // 6. Filter out already passed or planned courses
        var candidates = available
            .Where(c => c.IsActive && !passedIds.Contains(c.Id) && !plannedIds.Contains(c.Id))
            .ToList();

        // 7. Check prerequisites for each candidate
        var recommendations = new List<RecommendedCourseDto>();
        int accumulatedCredits = 0;

        // Required-first order: degree-rule gaps → electives
        var requiredFirst = candidates
            .OrderByDescending(c => requiredCourseIds.Contains(c.Id))
            .ThenBy(c => c.CreditHours)
            .ToList();

        foreach (var c in requiredFirst)
        {
            if (accumulatedCredits >= maxLoad) break;

            // Skip if credit overshoot
            if (accumulatedCredits + c.CreditHours > maxLoad) continue;

            // Prerequisite gate
            var prereqs = await _prereqs.GetByCourseIdAsync(c.Id, ct);
            bool prereqsMet = prereqs.All(p => passedIds.Contains(p.PrerequisiteCourseId));
            if (!prereqsMet) continue;

            string reason = requiredCourseIds.Contains(c.Id)
                ? "Required by your degree plan"
                : "Elective available in your department";

            recommendations.Add(new RecommendedCourseDto(c.Id, c.Code, c.Title, c.CreditHours, c.CourseType, reason));
            accumulatedCredits += c.CreditHours;
        }

        return new StudyPlanRecommendationDto(
            StudentProfileId:         studentProfileId,
            PlannedSemesterName:      plannedSemesterName,
            MaxCreditLoad:            maxLoad,
            RecommendedTotalCredits:  accumulatedCredits,
            Recommendations:          recommendations);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static StudyPlanDto MapDto(StudyPlan p) => new(
        Id:                  p.Id,
        StudentProfileId:    p.StudentProfileId,
        PlannedSemesterName: p.PlannedSemesterName,
        Notes:               p.Notes,
        AdvisorStatus:       p.AdvisorStatus,
        AdvisorNotes:        p.AdvisorNotes,
        ReviewedByUserId:    p.ReviewedByUserId,
        TotalCreditHours:    p.Courses.Sum(c => c.Course?.CreditHours ?? 0),
        Courses:             p.Courses.Select(MapCourseDto).ToList(),
        CreatedAt:           p.CreatedAt,
        UpdatedAt:           p.UpdatedAt);

    private static StudyPlanCourseDto MapCourseDto(StudyPlanCourse c) => new(
        CourseId:    c.CourseId,
        CourseCode:  c.Course?.Code        ?? string.Empty,
        CourseTitle: c.Course?.Title       ?? string.Empty,
        CreditHours: c.Course?.CreditHours ?? 0,
        CourseType:  c.Course?.CourseType  ?? CourseType.Core);
}
