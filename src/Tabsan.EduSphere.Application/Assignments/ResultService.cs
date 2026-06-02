using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Orchestrates result entry, publication, Admin correction, and transcript export.
/// Business invariants:
///   - Results are per (student, offering, type) — no duplicates.
///   - Publication is one-way; corrections require an Admin and are always audited.
///   - Transcript export creates a TranscriptExportLog row for every request.
///   - Faculty can only publish results for their own course offerings (enforced at controller layer).
/// </summary>
public class ResultService : IResultService
{
    private readonly IResultRepository _repo;
    private readonly IAuditService _audit;
    private readonly INotificationService? _notifications;
    private readonly IParentStudentLinkRepository? _parentLinks;

    public ResultService(
        IResultRepository repo,
        IAuditService audit,
        INotificationService? notifications = null,
        IParentStudentLinkRepository? parentLinks = null)
    {
        _repo = repo;
        _audit = audit;
        _notifications = notifications;
        _parentLinks = parentLinks;
    }

    // ── Result entry ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a draft result entry for a single student.
    /// Throws when a result already exists for the same (student, offering, type) combination.
    /// </summary>
    public async Task<ResultResponse> CreateAsync(CreateResultRequest request, CancellationToken ct = default)
    {
        var institutionType = await _repo.GetInstitutionTypeForOfferingAsync(request.CourseOfferingId, ct);
        var resultType = await ValidateComponentResultTypeAsync(request.ResultType, institutionType, ct);

        if (await _repo.ExistsAsync(request.StudentProfileId, request.CourseOfferingId, resultType, ct))
            throw new InvalidOperationException("A result entry already exists for this student / offering / type.");

        var result = new Result(request.StudentProfileId, request.CourseOfferingId, resultType,
                                request.MarksObtained, request.MaxMarks);
        result.SetGradePoint(await ResolveGradePointForInstitutionAsync(
            institutionType,
            request.MarksObtained,
            request.MaxMarks,
            ct));

        await _repo.AddAsync(result, ct);
        await _repo.SaveChangesAsync(ct);

        await RecalculateOfferingStandingAsync(request.StudentProfileId, request.CourseOfferingId, ct);
        return ToResponse(result);
    }

    /// <summary>
    /// Bulk-creates draft result entries for an entire class.
    /// Skips any entries that already exist rather than throwing.
    /// Returns the count of newly inserted rows.
    /// </summary>
    public async Task<int> BulkCreateAsync(BulkCreateResultsRequest request, CancellationToken ct = default)
    {
        var toInsert = new List<Result>();
        var institutionByOffering = new Dictionary<Guid, InstitutionType>();
        var validTypesByInstitution = new Dictionary<InstitutionType, HashSet<string>>();
        var gpaRulesByInstitution = new Dictionary<InstitutionType, IReadOnlyList<GpaScaleRule>>();

        var affectedPairs = new HashSet<(Guid StudentProfileId, Guid CourseOfferingId)>();

        foreach (var r in request.Results)
        {
            if (!institutionByOffering.TryGetValue(r.CourseOfferingId, out var institutionType))
            {
                institutionType = await _repo.GetInstitutionTypeForOfferingAsync(r.CourseOfferingId, ct);
                institutionByOffering[r.CourseOfferingId] = institutionType;
            }

            if (!validTypesByInstitution.TryGetValue(institutionType, out var validTypes))
            {
                validTypes = (await _repo.GetActiveComponentRulesAsync(institutionType, ct))
                    .Select(x => x.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                validTypesByInstitution[institutionType] = validTypes;
            }

            var resultType = NormalizeResultType(r.ResultType);
            if (!validTypes.Contains(resultType))
                continue;

            // Skip existing — do not overwrite.
            if (await _repo.ExistsAsync(r.StudentProfileId, r.CourseOfferingId, resultType, ct))
                continue;

            var result = new Result(r.StudentProfileId, r.CourseOfferingId, resultType, r.MarksObtained, r.MaxMarks);

            IReadOnlyList<GpaScaleRule> gpaRules = Array.Empty<GpaScaleRule>();
            if (institutionType == InstitutionType.University)
            {
                if (!gpaRulesByInstitution.TryGetValue(institutionType, out gpaRules))
                {
                    gpaRules = await _repo.GetGpaScaleRulesAsync(institutionType, ct);
                    gpaRulesByInstitution[institutionType] = gpaRules;
                }
            }

            result.SetGradePoint(ResolveGradePoint(r.MarksObtained, r.MaxMarks, gpaRules));
            toInsert.Add(result);
            affectedPairs.Add((r.StudentProfileId, r.CourseOfferingId));
        }

        if (toInsert.Count == 0) return 0;

        await _repo.AddRangeAsync(toInsert, ct);
        await _repo.SaveChangesAsync(ct);

        foreach (var pair in affectedPairs)
            await RecalculateOfferingStandingAsync(pair.StudentProfileId, pair.CourseOfferingId, ct);

        return toInsert.Count;
    }

    // ── Publication ───────────────────────────────────────────────────────────

    /// <summary>
    /// Publishes a single result, making it visible to the student.
    /// Returns false when the result does not exist or is already published.
    /// </summary>
    public async Task<bool> PublishAsync(Guid studentProfileId, Guid courseOfferingId, string resultType,
                                         Guid publishedByUserId, CancellationToken ct = default)
    {
        var result = await _repo.GetAsync(studentProfileId, courseOfferingId, NormalizeResultType(resultType), ct);
        if (result is null) return false;

        try { result.Publish(publishedByUserId); }
        catch (InvalidOperationException) { return false; }

        _repo.Update(result);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog("PublishResult", "Result", result.Id.ToString(),
            actorUserId: publishedByUserId), ct);

        await NotifyParentsForPublishedResultsAsync(
            [studentProfileId],
            courseOfferingId,
            normalizedResultType: NormalizeResultType(resultType),
            ct);

        return true;
    }

    /// <summary>
    /// Publishes all draft results for a course offering in one batch.
    /// Already-published results are silently skipped.
    /// Returns the number of results newly published.
    /// </summary>
    public async Task<int> PublishAllForOfferingAsync(Guid courseOfferingId, Guid publishedByUserId, CancellationToken ct = default)
    {
        var results = await _repo.GetByOfferingAsync(courseOfferingId, ct);
        var unpublished = results.Where(r => !r.IsPublished).ToList();

        foreach (var result in unpublished)
            result.Publish(publishedByUserId);

        foreach (var result in unpublished)
            _repo.Update(result);

        if (unpublished.Count > 0)
        {
            await _repo.SaveChangesAsync(ct);
            await _audit.LogAsync(new AuditLog("BulkPublishResults", "CourseOffering", courseOfferingId.ToString(),
                actorUserId: publishedByUserId,
                newValuesJson: $"{{\"count\":{unpublished.Count}}}"), ct);

            var affectedStudentIds = unpublished
                .Select(r => r.StudentProfileId)
                .Distinct()
                .ToList();

            await NotifyParentsForPublishedResultsAsync(
                affectedStudentIds,
                courseOfferingId,
                normalizedResultType: null,
                ct);
        }

        return unpublished.Count;
    }

    // ── Corrections (Admin only) ──────────────────────────────────────────────

    /// <summary>
    /// Applies an Admin-authorised correction to a published result.
    /// Captures the old values in the audit log before overwriting.
    /// Returns false when the result does not exist.
    /// </summary>
    public async Task<bool> CorrectAsync(Guid studentProfileId, Guid courseOfferingId, string resultType,
                                          CorrectResultRequest request, Guid correctedByUserId, CancellationToken ct = default)
    {
        var institutionType = await _repo.GetInstitutionTypeForOfferingAsync(courseOfferingId, ct);
        var normalizedType = NormalizeResultType(resultType);
        if (IsAggregateType(normalizedType))
            throw new InvalidOperationException("The Total row is system-calculated and cannot be corrected directly.");

        var result = await _repo.GetAsync(studentProfileId, courseOfferingId, normalizedType, ct);
        if (result is null) return false;
        if (!result.IsPublished)
            throw new InvalidOperationException("Only published results can be corrected. Save draft changes through result entry.");

        var oldJson = $"{{\"marks\":{result.MarksObtained},\"max\":{result.MaxMarks}}}";

        result.CorrectMarks(request.NewMarksObtained, request.NewMaxMarks);
        result.SetGradePoint(await ResolveGradePointForInstitutionAsync(
            institutionType,
            request.NewMarksObtained,
            request.NewMaxMarks,
            ct));
        _repo.Update(result);
        await _repo.SaveChangesAsync(ct);

        await RecalculateOfferingStandingAsync(studentProfileId, courseOfferingId, ct);

        await _audit.LogAsync(new AuditLog("CorrectResult", "Result", result.Id.ToString(),
            actorUserId: correctedByUserId,
            oldValuesJson: oldJson,
            newValuesJson: $"{{\"marks\":{request.NewMarksObtained},\"max\":{request.NewMaxMarks},\"reason\":{System.Text.Json.JsonSerializer.Serialize(request.Reason ?? string.Empty)}}}"), ct);

        return true;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all results for a student (draft + published) for faculty/admin views.</summary>
    public async Task<IReadOnlyList<ResultResponse>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var results = await _repo.GetByStudentAsync(studentProfileId, ct);
        return results.Select(ToResponse).ToList();
    }

    /// <summary>Returns only published results for a student (what the student sees).</summary>
    public async Task<IReadOnlyList<ResultResponse>> GetPublishedByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var results = await _repo.GetPublishedByStudentAsync(studentProfileId, ct);
        return results.Select(ToResponse).ToList();
    }

    /// <summary>Returns all results (draft + published) for a course offering.</summary>
    public async Task<IReadOnlyList<ResultResponse>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
    {
        var results = await _repo.GetByOfferingAsync(courseOfferingId, ct);
        return results.Select(ToResponse).ToList();
    }

    // ── Transcript ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gathers all published results for the student, logs the export request,
    /// and returns the results along with the new log entry ID.
    /// </summary>
    public async Task<(IReadOnlyList<ResultResponse> Results, Guid LogId)> ExportTranscriptAsync(
        TranscriptExportRequest request, Guid requestedByUserId, string? ipAddress, CancellationToken ct = default)
    {
        var results = await _repo.GetPublishedByStudentAsync(request.StudentProfileId, ct);
        var resultDtos = results.Select(ToResponse).ToList();

        var log = new TranscriptExportLog(request.StudentProfileId, requestedByUserId, request.Format, ipAddress: ipAddress);
        await _repo.AddExportLogAsync(log, ct);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog("ExportTranscript", "StudentProfile", request.StudentProfileId.ToString(),
            actorUserId: requestedByUserId, ipAddress: ipAddress), ct);

        return (resultDtos, log.Id);
    }

    /// <summary>Returns the transcript export history for a student, newest first.</summary>
    public async Task<IReadOnlyList<TranscriptExportLogResponse>> GetExportHistoryAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var logs = await _repo.GetExportLogsAsync(studentProfileId, ct);
        return logs.Select(l => new TranscriptExportLogResponse(l.Id, l.ExportedAt, l.Format, l.DocumentUrl)).ToList();
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    /// <summary>Maps a domain Result to a ResultResponse DTO.</summary>
    private static ResultResponse ToResponse(Result r) =>
        new(r.Id, r.StudentProfileId, r.CourseOfferingId,
            r.ResultType,
            r.MarksObtained, r.MaxMarks,
            r.MaxMarks > 0 ? Math.Round(r.MarksObtained / r.MaxMarks * 100, 2) : 0,
            r.GradePoint,
            r.IsPublished, r.PublishedAt);

    private async Task<string> ValidateComponentResultTypeAsync(string rawResultType, InstitutionType institutionType, CancellationToken ct)
    {
        var resultType = NormalizeResultType(rawResultType);
        if (IsAggregateType(resultType))
            throw new ArgumentException("Total is system-calculated and cannot be entered manually.");

        var allowedTypes = await _repo.GetActiveComponentRulesAsync(institutionType, ct);
        if (!allowedTypes.Any(x => string.Equals(x.Name, resultType, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Invalid result type '{rawResultType}'. Configure the component in Result Calculation first.");

        return resultType;
    }

    private async Task RecalculateOfferingStandingAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct)
    {
        var institutionType = await _repo.GetInstitutionTypeForOfferingAsync(courseOfferingId, ct);
        var componentRules = (await _repo.GetActiveComponentRulesAsync(institutionType, ct))
            .OrderBy(x => x.DisplayOrder)
            .ToList();
        if (componentRules.Count == 0)
            return;

        var gpaRules = institutionType == InstitutionType.University
            ? await _repo.GetGpaScaleRulesAsync(institutionType, ct)
            : Array.Empty<GpaScaleRule>();
        var results = (await _repo.GetByStudentAndOfferingAsync(studentProfileId, courseOfferingId, ct)).ToList();

        foreach (var componentResult in results.Where(r => !IsAggregateType(r.ResultType)))
        {
            componentResult.SetGradePoint(
                institutionType == InstitutionType.University
                    ? ResolveGradePoint(componentResult.MarksObtained, componentResult.MaxMarks, gpaRules)
                    : null);
            _repo.Update(componentResult);
        }

        var configuredByName = componentRules.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var enteredComponents = results
            .Where(r => !IsAggregateType(r.ResultType) && configuredByName.ContainsKey(r.ResultType))
            .ToList();

        if (enteredComponents.Count == 0)
        {
            await _repo.SaveChangesAsync(ct);
            return;
        }

        decimal currentMarks = 0;
        decimal currentMax = 0;

        foreach (var component in enteredComponents)
        {
            var rule = configuredByName[component.ResultType];
            if (component.MaxMarks <= 0) continue;

            currentMarks += Math.Round((component.MarksObtained / component.MaxMarks) * rule.Weightage, 2);
            currentMax += rule.Weightage;
        }

        var totalGradePoint = institutionType == InstitutionType.University
            ? ResolveGradePoint(currentMarks, currentMax, gpaRules)
            : null;
        var totalRow = results.FirstOrDefault(r => IsAggregateType(r.ResultType));
        if (totalRow is null)
        {
            totalRow = new Result(studentProfileId, courseOfferingId, TotalResultType, currentMarks, currentMax);
            totalRow.SetGradePoint(totalGradePoint);
            await _repo.AddAsync(totalRow, ct);
        }
        else
        {
            totalRow.CorrectMarks(currentMarks, currentMax);
            totalRow.SetGradePoint(totalGradePoint);
            _repo.Update(totalRow);
        }

        await _repo.SaveChangesAsync(ct);

        if (institutionType is InstitutionType.School or InstitutionType.College)
        {
            await RecalculateNonUniversityStandingAsync(studentProfileId, ct);
            return;
        }

        var semesterId = await _repo.GetSemesterIdForOfferingAsync(courseOfferingId, ct);
        if (!semesterId.HasValue)
            return;

        var student = await _repo.GetStudentProfileAsync(studentProfileId, ct);
        if (student is null)
            return;

        var configuredTotalWeight = componentRules.Sum(x => x.Weightage);
        var semesterEnrollments = await _repo.GetActiveEnrollmentsForSemesterAsync(studentProfileId, semesterId.Value, ct);
        var semesterResults = await _repo.GetByStudentAndSemesterAsync(studentProfileId, semesterId.Value, ct);
        var completeSemesterTotals = semesterResults
            .Where(r => IsAggregateType(r.ResultType)
                     && r.MaxMarks == configuredTotalWeight
                     && r.GradePoint.HasValue)
            .ToDictionary(r => r.CourseOfferingId, r => r, EqualityComparer<Guid>.Default);

        var semesterIsComplete = semesterEnrollments.Count > 0
            && semesterEnrollments.All(e => completeSemesterTotals.ContainsKey(e.CourseOfferingId));

        var semesterGpa = student.CurrentSemesterGpa;
        if (semesterIsComplete)
        {
            semesterGpa = CalculateWeightedGpa(
                semesterEnrollments
                    .Select(e => (e.CourseOfferingId, CreditHours: e.CourseOffering.Course.CreditHours))
                    .Where(x => completeSemesterTotals.ContainsKey(x.CourseOfferingId))
                    .Select(x => (x.CreditHours, completeSemesterTotals[x.CourseOfferingId].GradePoint!.Value)));
        }

        var allEnrollments = await _repo.GetActiveEnrollmentsForStudentAsync(studentProfileId, ct);
        var allResults = await _repo.GetByStudentAsync(studentProfileId, ct);
        var completeTotals = allResults
            .Where(r => IsAggregateType(r.ResultType)
                     && r.MaxMarks == configuredTotalWeight
                     && r.GradePoint.HasValue)
            .ToDictionary(r => r.CourseOfferingId, r => r, EqualityComparer<Guid>.Default);

        var cgpa = completeTotals.Count == 0
            ? 0m
            : CalculateWeightedGpa(
                allEnrollments
                    .Where(e => completeTotals.ContainsKey(e.CourseOfferingId))
                    .Select(e => (e.CourseOffering.Course.CreditHours, completeTotals[e.CourseOfferingId].GradePoint!.Value)));

        student.UpdateAcademicStanding(semesterGpa, cgpa);
        _repo.UpdateStudentProfile(student);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task RecalculateNonUniversityStandingAsync(Guid studentProfileId, CancellationToken ct)
    {
        var student = await _repo.GetStudentProfileAsync(studentProfileId, ct);
        if (student is null)
            return;

        var allResults = await _repo.GetByStudentAsync(studentProfileId, ct);
        var aggregateRows = allResults
            .Where(r => IsAggregateType(r.ResultType) && r.MaxMarks > 0)
            .ToList();

        var semesterPercentage = aggregateRows.Count == 0
            ? 0m
            : Math.Round(aggregateRows.Last().MarksObtained / aggregateRows.Last().MaxMarks * 100m, 2, MidpointRounding.AwayFromZero);

        decimal cumulativePercentage;
        if (aggregateRows.Count == 0)
        {
            cumulativePercentage = 0m;
        }
        else
        {
            var totalMarks = aggregateRows.Sum(x => x.MarksObtained);
            var totalMax = aggregateRows.Sum(x => x.MaxMarks);
            cumulativePercentage = totalMax <= 0
                ? 0m
                : Math.Round(totalMarks / totalMax * 100m, 2, MidpointRounding.AwayFromZero);
        }

        student.UpdateAcademicStanding(
            ToGpaScaleEquivalent(semesterPercentage),
            ToGpaScaleEquivalent(cumulativePercentage));

        _repo.UpdateStudentProfile(student);
        await _repo.SaveChangesAsync(ct);
    }

    private static decimal ToGpaScaleEquivalent(decimal percentage)
        => Math.Round(Math.Clamp(percentage, 0m, 100m) / 25m, 2, MidpointRounding.AwayFromZero);

    private async Task<decimal?> ResolveGradePointForInstitutionAsync(
        InstitutionType institutionType,
        decimal marksObtained,
        decimal maxMarks,
        CancellationToken ct)
    {
        if (institutionType != InstitutionType.University)
            return null;

        return ResolveGradePoint(marksObtained, maxMarks, await _repo.GetGpaScaleRulesAsync(institutionType, ct));
    }

    private static decimal? ResolveGradePoint(decimal marksObtained, decimal maxMarks, IReadOnlyList<GpaScaleRule> gpaRules)
    {
        if (maxMarks <= 0 || gpaRules.Count == 0)
            return null;

        var percentage = (marksObtained / maxMarks) * 100m;
        return gpaRules
            .OrderByDescending(r => r.MinimumScore)
            .FirstOrDefault(r => percentage >= r.MinimumScore)
            ?.GradePoint;
    }

    private static decimal CalculateWeightedGpa(IEnumerable<(int CreditHours, decimal GradePoint)> rows)
    {
        var materialized = rows.Where(x => x.CreditHours > 0).ToList();
        var totalCredits = materialized.Sum(x => x.CreditHours);
        if (totalCredits <= 0) return 0m;

        var weighted = materialized.Sum(x => x.CreditHours * x.GradePoint);
        return Math.Round(weighted / totalCredits, 2, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeResultType(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static bool IsAggregateType(string value)
        => string.Equals(value, TotalResultType, StringComparison.OrdinalIgnoreCase);

    private async Task NotifyParentsForPublishedResultsAsync(
        IReadOnlyList<Guid> studentProfileIds,
        Guid courseOfferingId,
        string? normalizedResultType,
        CancellationToken ct)
    {
        if (_notifications is null || _parentLinks is null || studentProfileIds.Count == 0)
            return;

        var parentUserIds = await _parentLinks.GetActiveParentUserIdsByStudentsAsync(studentProfileIds, ct);
        if (parentUserIds.Count == 0)
            return;

        var title = "Result Published";
        var body = string.IsNullOrWhiteSpace(normalizedResultType)
            ? $"New results have been published for your child in course offering {courseOfferingId}."
            : $"A {normalizedResultType} result has been published for your child in course offering {courseOfferingId}.";

        await _notifications.SendSystemAsync(title, body, NotificationType.Result, parentUserIds, ct);
    }

    private const string TotalResultType = "Total";
}
