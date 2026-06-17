using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Academic;

// Phase 25 — Academic Engine Unification — Stage 25.3

/// <summary>
/// Evaluates and applies student progression decisions across institution types.
///
/// University: progression is based on CGPA ≥ pass threshold (GPA scale 0.0–4.0).
/// School / College: progression is based on latest percentage ≥ pass threshold.
///
/// The <see cref="IStudentProfileRepository"/> is used to read and advance the
/// student's current semester/year/grade counter. The active grading profile for
/// the institution type supplies the pass threshold.
/// </summary>
public class ProgressionService : IProgressionService
{
    private readonly IStudentProfileRepository _studentRepo;
    private readonly IInstitutionGradingProfileRepository _gradingRepo;
    private readonly IResultRepository? _resultRepo;

    public ProgressionService(
        IStudentProfileRepository studentRepo,
        IInstitutionGradingProfileRepository gradingRepo,
        IResultRepository? resultRepo = null)
    {
        _studentRepo = studentRepo;
        _gradingRepo = gradingRepo;
        _resultRepo = resultRepo;
    }

    public async Task<ProgressionDecision> EvaluateAsync(
        ProgressionEvaluationRequest request,
        CancellationToken ct = default)
    {
        var student = await _studentRepo.GetByIdAsync(request.StudentProfileId, ct)
            ?? throw new KeyNotFoundException($"Student profile {request.StudentProfileId} not found.");

        var profile = await _gradingRepo.GetByTypeAsync(request.InstitutionType, ct);
        var passThreshold = profile?.PassThreshold ?? DefaultPassThreshold(request.InstitutionType);

        return request.InstitutionType switch
        {
            InstitutionType.University => BuildUniversityDecision(student, passThreshold),
            InstitutionType.School     => BuildSchoolDecision(student, passThreshold, await ResolveProgressionPercentageAsync(student, ct)),
            InstitutionType.College    => BuildCollegeDecision(student, passThreshold, await ResolveProgressionPercentageAsync(student, ct)),
            _                          => BuildUniversityDecision(student, passThreshold)
        };
    }

    public async Task<ProgressionDecision> PromoteAsync(
        ProgressionEvaluationRequest request,
        CancellationToken ct = default)
    {
        var decision = await EvaluateAsync(request, ct);
        if (!decision.CanProgress)
            throw new InvalidOperationException(
                $"Student {request.StudentProfileId} does not meet the progression criteria. {decision.Remarks}");

        var student = await _studentRepo.GetByIdAsync(request.StudentProfileId, ct)!;
        student!.AdvanceSemester();
        if (request.InstitutionType == InstitutionType.College && student.Status != StudentStatus.Graduated)
            student.AdvanceSemester();
        _studentRepo.Update(student);
        await _studentRepo.SaveChangesAsync(ct);

        // Return a refreshed decision reflecting the new semester number.
        var profile = await _gradingRepo.GetByTypeAsync(request.InstitutionType, ct);
        var passThreshold = profile?.PassThreshold ?? DefaultPassThreshold(request.InstitutionType);
        return request.InstitutionType switch
        {
            InstitutionType.University => BuildUniversityDecision(student, passThreshold),
            InstitutionType.School     => BuildSchoolDecision(student, passThreshold, await ResolveProgressionPercentageAsync(student, ct)),
            InstitutionType.College    => BuildCollegeDecision(student, passThreshold, await ResolveProgressionPercentageAsync(student, ct)),
            _                          => BuildUniversityDecision(student, passThreshold)
        };
    }

    // ── Strategy-specific decision builders ──────────────────────────────────

    private static ProgressionDecision BuildUniversityDecision(
        Domain.Academic.StudentProfile student, decimal passThreshold)
    {
        var cgpa = student.Cgpa;
        var canProgress = cgpa >= passThreshold;
        var currentLabel = $"Semester {student.CurrentSemesterNumber}";
        var nextLabel    = $"Semester {student.CurrentSemesterNumber + 1}";
        var remarks = canProgress
            ? $"CGPA {cgpa:F2} ≥ {passThreshold:F2} — eligible for semester promotion."
            : $"CGPA {cgpa:F2} < {passThreshold:F2} — does not meet the minimum GPA requirement.";

        return new ProgressionDecision(student.Id, InstitutionType.University,
            canProgress, currentLabel, nextLabel, cgpa, passThreshold, remarks);
    }

    private static ProgressionDecision BuildSchoolDecision(
        Domain.Academic.StudentProfile student, decimal passThreshold, decimal percentage)
    {
        var canProgress = percentage >= passThreshold;
        var currentLabel = $"Grade {student.CurrentSemesterNumber}";
        var nextLabel    = $"Grade {student.CurrentSemesterNumber + 1}";
        var remarks = canProgress
            ? $"Percentage {percentage:F2}% ≥ {passThreshold:F2}% — eligible for grade promotion."
            : $"Percentage {percentage:F2}% < {passThreshold:F2}% — does not meet the pass requirement.";

        return new ProgressionDecision(student.Id, InstitutionType.School,
            canProgress, currentLabel, nextLabel, percentage, passThreshold, remarks);
    }

    private static ProgressionDecision BuildCollegeDecision(
        Domain.Academic.StudentProfile student, decimal passThreshold, decimal percentage)
    {
        var year = (student.CurrentSemesterNumber + 1) / 2; // semesters → years
        var canProgress = percentage >= passThreshold;
        var currentLabel = $"Year {year}";
        var nextLabel    = $"Year {year + 1}";
        var remarks = canProgress
            ? $"Percentage {percentage:F2}% ≥ {passThreshold:F2}% — eligible for year promotion."
            : $"Percentage {percentage:F2}% < {passThreshold:F2}% — does not meet the pass requirement.";

        return new ProgressionDecision(student.Id, InstitutionType.College,
            canProgress, currentLabel, nextLabel, percentage, passThreshold, remarks);
    }

    private async Task<decimal> ResolveProgressionPercentageAsync(
        Domain.Academic.StudentProfile student,
        CancellationToken ct)
    {
        var storedPercentage = NormalizeToPercentage(student.CurrentSemesterGpa);
        if (storedPercentage > 0m || _resultRepo is null)
            return storedPercentage;

        var allResults = await _resultRepo.GetByStudentAsync(student.Id, ct);
        if (allResults.Count == 0)
            return storedPercentage;

        var latestTotal = allResults
            .Where(r => IsAggregateResultType(r.ResultType) && r.MaxMarks > 0)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latestTotal is not null)
            return Math.Round(latestTotal.MarksObtained / latestTotal.MaxMarks * 100m, 2, MidpointRounding.AwayFromZero);

        var latestOfferingId = allResults
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.CourseOfferingId)
            .FirstOrDefault();

        if (latestOfferingId == Guid.Empty)
            return storedPercentage;

        var latestOfferingComponents = allResults
            .Where(r => r.CourseOfferingId == latestOfferingId
                        && !IsAggregateResultType(r.ResultType)
                        && r.MaxMarks > 0)
            .ToList();

        if (latestOfferingComponents.Count == 0)
            return storedPercentage;

        var totalMarks = latestOfferingComponents.Sum(r => r.MarksObtained);
        var totalMax = latestOfferingComponents.Sum(r => r.MaxMarks);
        if (totalMax <= 0)
            return storedPercentage;

        return Math.Round(totalMarks / totalMax * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static bool IsAggregateResultType(string? resultType)
        => string.Equals(resultType, "Total", StringComparison.OrdinalIgnoreCase);

    // Legacy academic standing stores GPA-scale values (0-4); School/College progression
    // needs percentage semantics, so normalize when values are in GPA range.
    private static decimal NormalizeToPercentage(decimal score)
        => score <= 4.0m ? score * 25m : score;

    private static decimal DefaultPassThreshold(InstitutionType type)
        => type == InstitutionType.University ? 2.0m : 40m;
}
