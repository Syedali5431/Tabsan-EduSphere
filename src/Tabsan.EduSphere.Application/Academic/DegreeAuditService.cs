// Final-Touches Phase 17 Stage 17.1/17.2/17.3 — Degree Audit service implementation

using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Academic;

/// <summary>
/// Implements the Degree Audit System: credit aggregation, eligibility evaluation,
/// degree rule CRUD, and course-type tagging.
/// </summary>
public class DegreeAuditService : IDegreeAuditService
{
    private readonly IDegreeAuditRepository    _auditRepo;
    private readonly IStudentProfileRepository _studentRepo;
    private readonly ICourseRepository         _courseRepo;

    public DegreeAuditService(
        IDegreeAuditRepository    auditRepo,
        IStudentProfileRepository studentRepo,
        ICourseRepository         courseRepo)
    {
        _auditRepo   = auditRepo;
        _studentRepo = studentRepo;
        _courseRepo  = courseRepo;
    }

    // Final-Touches Phase 17 Stage 17.1 — full credit audit for a student
    public async Task<DegreeAuditResponse> GetAuditAsync(
        Guid studentProfileId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null)
    {
        var student = await _studentRepo.GetByIdAsync(studentProfileId, ct)
            ?? throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        if (student.Department?.InstitutionType != InstitutionType.University)
            throw new InvalidOperationException("Degree audit is available only for university institution type.");

        if (tenantId.HasValue && student.Department?.TenantId != tenantId.Value)
            throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");
        if (campusId.HasValue && student.Department?.CampusId != campusId.Value)
            throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        var username = await _auditRepo.GetUsernameAsync(student.UserId, ct);
        var credits = await _auditRepo.GetEarnedCreditsAsync(studentProfileId, ct, tenantId, campusId);
        var programId = student.ProgramId;

        DegreeRule? rule = null;
        if (programId != Guid.Empty)
            rule = await _auditRepo.GetRuleByProgramAsync(programId, ct, tenantId, campusId);

        // Deduplicate by CourseId — only count each course once (highest GradePoint wins)
        var deduplicated = credits
            .GroupBy(r => r.CourseId)
            .Select(g => g.OrderByDescending(r => r.GradePoint ?? 0m).First())
            .ToList();

        int totalCredits    = deduplicated.Sum(r => r.CreditHours);
        int coreCredits     = deduplicated.Where(r => r.CourseType == CourseType.Core).Sum(r => r.CreditHours);
        int electiveCredits = deduplicated.Where(r => r.CourseType == CourseType.Elective).Sum(r => r.CreditHours);

        var unmet = new List<string>();
        bool isEligible = true;

        if (rule is not null)
        {
            if (totalCredits < rule.MinTotalCredits)
            {
                unmet.Add($"Total credits: {totalCredits}/{rule.MinTotalCredits} required.");
                isEligible = false;
            }
            if (coreCredits < rule.MinCoreCredits)
            {
                unmet.Add($"Core credits: {coreCredits}/{rule.MinCoreCredits} required.");
                isEligible = false;
            }
            if (electiveCredits < rule.MinElectiveCredits)
            {
                unmet.Add($"Elective credits: {electiveCredits}/{rule.MinElectiveCredits} required.");
                isEligible = false;
            }
            if (student.Cgpa < rule.MinGpa)
            {
                unmet.Add($"Minimum GPA: {student.Cgpa:F2}/{rule.MinGpa:F2} required.");
                isEligible = false;
            }

            // Final-Touches Phase 17 Stage 17.2 — check required core courses
            var passedCourseIds = new HashSet<Guid>(deduplicated.Select(r => r.CourseId));
            foreach (var req in rule.RequiredCourses)
            {
                if (!passedCourseIds.Contains(req.CourseId))
                {
                    var name = req.Course?.Code ?? req.CourseId.ToString();
                    unmet.Add($"Required course not passed: {name}.");
                    isEligible = false;
                }
            }
        }
        else
        {
            // No rule defined — mark as ineligible with informational message
            isEligible = false;
            unmet.Add("No degree rule has been configured for this program.");
        }

        return new DegreeAuditResponse
        {
            StudentProfileId     = student.Id,
            StudentName          = username,
            RegistrationNumber   = student.RegistrationNumber,
            ProgramName          = student.Program?.Name ?? "",
            Cgpa                 = student.Cgpa,
            TotalCreditsEarned   = totalCredits,
            CoreCreditsEarned    = coreCredits,
            ElectiveCreditsEarned = electiveCredits,
            IsEligible           = isEligible,
            UnmetRequirements    = unmet,
            CompletedCourses     = deduplicated.Select(r => new EarnedCourseRow
            {
                CourseId    = r.CourseId,
                CourseCode  = r.CourseCode,
                CourseTitle = r.CourseTitle,
                CreditHours = r.CreditHours,
                CourseType  = r.CourseType.ToString(),
                GradePoint  = r.GradePoint
            }).OrderBy(r => r.CourseCode).ToList()
        };
    }

    // Final-Touches Phase 17 Stage 17.2 — eligibility list for admin
    public async Task<IReadOnlyList<EligibilityListItem>> GetEligibilityListAsync(
        Guid? departmentId,
        Guid? programId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null)
    {
        var students = await _studentRepo.GetAllAsync(departmentId, ct);

        students = students
            .Where(s => s.Department?.InstitutionType == InstitutionType.University)
            .Where(s => s.Status == StudentStatus.Active)
            .Where(s => s.Program is not null)
            .Where(s => s.Program!.TotalSemesters > 0)
            .Where(s => s.CurrentSemesterNumber >= s.Program!.TotalSemesters)
            .ToList();

        if (tenantId.HasValue)
            students = students.Where(s => s.Department?.TenantId == tenantId.Value).ToList();

        if (campusId.HasValue)
            students = students.Where(s => s.Department?.CampusId == campusId.Value).ToList();

        if (programId.HasValue)
            students = students.Where(s => s.ProgramId == programId.Value).ToList();

        var results = new List<EligibilityListItem>();
        foreach (var s in students)
        {
            var audit = await GetAuditAsync(s.Id, ct, tenantId, campusId);
            results.Add(new EligibilityListItem
            {
                StudentProfileId   = s.Id,
                StudentName        = audit.StudentName,
                RegistrationNumber = s.RegistrationNumber,
                Cgpa               = s.Cgpa,
                TotalCreditsEarned = audit.TotalCreditsEarned,
                IsEligible         = audit.IsEligible,
                UnmetCount         = audit.UnmetRequirements.Count
            });
        }
        return results;
    }

    // Final-Touches Phase 17 Stage 17.2 — degree rule CRUD
    public async Task<IReadOnlyList<DegreeRuleResponse>> GetAllRulesAsync(
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null)
    {
        var rules = await _auditRepo.GetAllRulesAsync(ct, tenantId, campusId);
        return rules.Select(MapRule).ToList();
    }

    public async Task<DegreeRuleResponse?> GetRuleByProgramAsync(
        Guid programId,
        CancellationToken ct = default,
        Guid? tenantId = null,
        Guid? campusId = null)
    {
        var rule = await _auditRepo.GetRuleByProgramAsync(programId, ct, tenantId, campusId);
        if (rule is null)
            return null;

        if (rule.AcademicProgram?.Department?.InstitutionType != InstitutionType.University)
            return null;

        return MapRule(rule);
    }

    public async Task<DegreeRuleResponse> CreateRuleAsync(CreateDegreeRuleRequest req, CancellationToken ct = default)
    {
        var rule = DegreeRule.Create(
            req.AcademicProgramId,
            req.MinTotalCredits,
            req.MinCoreCredits,
            req.MinElectiveCredits,
            req.MinGpa);

        foreach (var courseId in req.RequiredCourseIds.Distinct())
            rule.AddRequiredCourse(courseId);

        await _auditRepo.AddRuleAsync(rule, ct);
        await _auditRepo.SaveChangesAsync(ct);
        return await GetRuleByProgramAsync(req.AcademicProgramId, ct) ?? MapRule(rule);
    }

    public async Task<DegreeRuleResponse> UpdateRuleAsync(Guid ruleId, UpdateDegreeRuleRequest req, CancellationToken ct = default)
    {
        var rule = await _auditRepo.GetRuleByIdAsync(ruleId, ct)
            ?? throw new KeyNotFoundException($"Degree rule {ruleId} not found.");

        if (rule.AcademicProgram?.Department?.InstitutionType != InstitutionType.University)
            throw new InvalidOperationException("Degree rules are available only for university institution type.");

        rule.Update(req.MinTotalCredits, req.MinCoreCredits, req.MinElectiveCredits, req.MinGpa);

        // Sync required courses
        var existing = rule.RequiredCourses.Select(r => r.CourseId).ToHashSet();
        var desired  = req.RequiredCourseIds.ToHashSet();
        foreach (var id in desired.Except(existing)) rule.AddRequiredCourse(id);
        foreach (var id in existing.Except(desired)) rule.RemoveRequiredCourse(id);

        _auditRepo.UpdateRule(rule);
        await _auditRepo.SaveChangesAsync(ct);
        return MapRule(rule);
    }

    public async Task DeleteRuleAsync(Guid ruleId, CancellationToken ct = default)
    {
        var rule = await _auditRepo.GetRuleByIdAsync(ruleId, ct)
            ?? throw new KeyNotFoundException($"Degree rule {ruleId} not found.");
        if (rule.AcademicProgram?.Department?.InstitutionType != InstitutionType.University)
            throw new InvalidOperationException("Degree rules are available only for university institution type.");
        rule.SoftDelete();
        _auditRepo.UpdateRule(rule);
        await _auditRepo.SaveChangesAsync(ct);
    }

    // Final-Touches Phase 17 Stage 17.3 — set core/elective on a course
    public async Task SetCourseTypeAsync(Guid courseId, string courseType, CancellationToken ct = default)
    {
        var course = await _courseRepo.GetByIdAsync(courseId, ct)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        var type = Enum.TryParse<CourseType>(courseType, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid CourseType: {courseType}. Must be 'Core' or 'Elective'.");

        course.SetCourseType(type);
        _courseRepo.Update(course);
        await _courseRepo.SaveChangesAsync(ct);
    }

    private static DegreeRuleResponse MapRule(DegreeRule r) => new()
    {
        RuleId             = r.Id,
        AcademicProgramId  = r.AcademicProgramId,
        ProgramName        = r.AcademicProgram?.Name ?? "",
        MinTotalCredits    = r.MinTotalCredits,
        MinCoreCredits     = r.MinCoreCredits,
        MinElectiveCredits = r.MinElectiveCredits,
        MinGpa             = r.MinGpa,
        RequiredCourses    = r.RequiredCourses.Select(rc => new RequiredCourseItem
        {
            CourseId    = rc.CourseId,
            CourseCode  = rc.Course?.Code  ?? "",
            CourseTitle = rc.Course?.Title ?? ""
        }).ToList()
    };
}
