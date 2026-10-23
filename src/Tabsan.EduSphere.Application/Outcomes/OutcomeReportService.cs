using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.Application.Outcomes;

/// <summary>
/// Plan M6.4 — Learning Mastery Gradebook (students × outcomes for a course offering), a student's own mastery,
/// and program outcome attainment for accreditation. Scores are always taken from the stored share of the work
/// (Percent) on the outcome's current scale, so a changed rating scale applies to old evidence too.
/// </summary>
public sealed class OutcomeReportService
{
    private readonly IOutcomeRepository _repo;

    public OutcomeReportService(IOutcomeRepository repo) => _repo = repo;

    public async Task<MasteryGradebookResponse?> MasteryGradebookAsync(OutcomeActor actor, Guid offeringId, CancellationToken ct = default)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        if (offering is null) return null;
        var results = await _repo.GetResultsAsync(offeringId, null, null, null, ct);
        var alignments = await _repo.GetAlignmentsForOfferingAsync(offeringId, ct);
        var courseOutcomes = await _repo.ListAsync(actor.TenantId, actor.IsSuperAdmin, OutcomeLevel.Course, null, offering.CourseId, false, ct);
        var ids = results.Select(r => r.OutcomeId).Concat(alignments.Select(a => a.OutcomeId)).Concat(courseOutcomes.Select(o => o.Id)).Distinct().ToList();
        var outcomes = (await _repo.GetManyAsync(ids, ct)).Where(o => o.IsActive && o.MaxPoints > 0 && OutcomeService.Visible(actor, o))
            .OrderByDescending(o => o.Level).ThenBy(o => o.Code, StringComparer.OrdinalIgnoreCase).ToList();

        var enrolled = await _repo.GetEnrolledStudentsAsync(offeringId, ct);
        var extraIds = results.Select(r => r.StudentProfileId).Distinct().Where(id => enrolled.All(s => s.StudentProfileId != id)).ToList();
        var students = enrolled.Concat(extraIds.Count == 0 ? [] : await _repo.GetStudentsAsync(extraIds, ct))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var byStudentOutcome = results.ToLookup(r => (r.StudentProfileId, r.OutcomeId));

        var rows = students.Select(s => new MasteryStudentRow(s.StudentProfileId, s.Name, s.RegistrationNumber,
            outcomes.Select(o => Cell(o, byStudentOutcome[(s.StudentProfileId, o.Id)])).ToList())).ToList();
        var columns = outcomes.Select((o, i) => new MasteryOutcomeColumn(o.Id, o.Code, o.Title, o.Level.ToString(), o.MasteryPoints, o.MaxPoints,
            o.CalculationMethod.ToString(), rows.Count(r => r.Cells[i].Score is not null), rows.Count(r => r.Cells[i].Mastered))).ToList();
        return new MasteryGradebookResponse(offeringId, offering.CourseCode, offering.CourseTitle, offering.SemesterName, columns, rows);
    }

    /// <summary>The student's own mastery, per course they are enrolled in (courses without outcomes are left out).</summary>
    public async Task<IReadOnlyList<StudentMasteryCourse>> StudentMasteryAsync(Guid studentProfileId, Guid? offeringId, CancellationToken ct = default)
    {
        var results = await _repo.GetResultsAsync(offeringId, [studentProfileId], null, null, ct);
        var offeringIds = offeringId is { } only ? [only] : (await _repo.GetStudentOfferingsAsync(studentProfileId, ct)).Union(results.Select(r => r.CourseOfferingId)).ToList();
        var offerings = await _repo.GetOfferingsAsync(offeringIds, ct);
        var titles = await _repo.GetSourceTitlesAsync(
            results.Where(r => r.SourceType == OutcomeSourceType.AssignmentSubmission).Select(r => r.SourceId).Distinct().ToList(),
            results.Where(r => r.SourceType == OutcomeSourceType.QuizAttempt).Select(r => r.SourceId).Distinct().ToList(), ct);

        var courses = new List<StudentMasteryCourse>();
        foreach (var offering in offerings.OrderByDescending(o => o.SemesterName).ThenBy(o => o.CourseCode))
        {
            var mine = results.Where(r => r.CourseOfferingId == offering.OfferingId).ToList();
            var alignments = await _repo.GetAlignmentsForOfferingAsync(offering.OfferingId, ct);
            var ids = mine.Select(r => r.OutcomeId).Concat(alignments.Select(a => a.OutcomeId)).Distinct().ToList();
            if (ids.Count == 0) continue;
            var outcomes = (await _repo.GetManyAsync(ids, ct)).Where(o => o.IsActive && o.MaxPoints > 0)
                .OrderByDescending(o => o.Level).ThenBy(o => o.Code, StringComparer.OrdinalIgnoreCase);
            var items = outcomes.Select(o =>
            {
                var evidence = mine.Where(r => r.OutcomeId == o.Id).OrderByDescending(r => r.AssessedAt).ToList();
                var cell = Cell(o, evidence);
                return new StudentMasteryItem(o.Id, o.Code, o.Title, o.Description, o.Level.ToString(), cell.Score, o.MasteryPoints, o.MaxPoints,
                    cell.Mastered, cell.Rating, cell.Status,
                    evidence.Select(r => new StudentEvidenceItem(r.SourceType == OutcomeSourceType.QuizAttempt ? "Quiz" : "Assignment",
                        titles.TryGetValue(r.SourceId, out var t) ? t : "Graded work", MasteryCalculator.ToScale(r.Percent, o.MaxPoints), r.Percent, r.AssessedAt)).ToList());
            }).ToList();
            courses.Add(new StudentMasteryCourse(offering.OfferingId, offering.CourseCode, offering.CourseTitle, offering.SemesterName, items));
        }
        return courses;
    }

    /// <summary>
    /// Program outcome attainment: for each program outcome, the students of the program with evidence (direct results
    /// plus results of mapped course outcomes, on the program outcome's scale), how many mastered it, and the courses
    /// the evidence came from. <paramref name="targetPercent"/> is the share of students expected to attain each outcome.
    /// </summary>
    public async Task<ProgramAttainmentReport?> ProgramAttainmentAsync(OutcomeActor actor, Guid programId, Guid? semesterId, decimal targetPercent, CancellationToken ct = default)
    {
        var program = await _repo.GetProgramAsync(programId, ct);
        if (program is null) return null;
        targetPercent = Math.Clamp(targetPercent, 1, 100);
        var plos = (await _repo.ListAsync(actor.TenantId, actor.IsSuperAdmin, OutcomeLevel.Program, programId, null, false, ct))
            .Where(o => o.MaxPoints > 0).OrderBy(o => o.Code, StringComparer.OrdinalIgnoreCase).ToList();
        var students = (await _repo.GetProgramStudentIdsAsync(programId, ct)).ToHashSet();
        var mappings = await _repo.GetMappingsAsync(null, plos.Select(p => p.Id).ToList(), ct);
        var clos = (await _repo.GetManyAsync(mappings.Select(m => m.CourseOutcomeId).Distinct().ToList(), ct)).Where(o => o.IsActive).ToDictionary(o => o.Id);
        var evidenceOutcomeIds = plos.Select(p => p.Id).Concat(clos.Keys).ToList();
        var results = students.Count == 0 || evidenceOutcomeIds.Count == 0 ? []
            : (await _repo.GetResultsAsync(null, students, evidenceOutcomeIds, null, ct)).ToList();
        var offerings = (await _repo.GetOfferingsAsync(results.Select(r => r.CourseOfferingId).Distinct().ToList(), ct)).ToDictionary(o => o.OfferingId);
        if (semesterId is { } semester) results = results.Where(r => offerings.TryGetValue(r.CourseOfferingId, out var o) && o.SemesterId == semester).ToList();

        var rows = new List<ProgramOutcomeAttainment>();
        foreach (var plo in plos)
        {
            var mappedIds = mappings.Where(m => m.ProgramOutcomeId == plo.Id && clos.ContainsKey(m.CourseOutcomeId)).Select(m => m.CourseOutcomeId).ToHashSet();
            var evidence = results.Where(r => r.OutcomeId == plo.Id || mappedIds.Contains(r.OutcomeId)).ToList();
            var perStudent = evidence.GroupBy(r => r.StudentProfileId)
                .Select(g => MasteryCalculator.Calculate(g.Select(r => (MasteryCalculator.ToScale(r.Percent, plo.MaxPoints), r.AssessedAt)),
                    plo.CalculationMethod, plo.CalculationParameter, plo.MasteryPoints))
                .ToList();
            var assessed = perStudent.Count;
            var mastered = perStudent.Count(v => v.Mastered);
            decimal? attainment = assessed == 0 ? null : Math.Round(mastered * 100m / assessed, 1, MidpointRounding.AwayFromZero);
            var contributions = evidence.GroupBy(r => r.CourseOfferingId)
                .Select(g => offerings.TryGetValue(g.Key, out var o)
                    ? new AttainmentContribution(o.CourseCode, o.CourseTitle, o.SemesterName, g.Select(r => r.StudentProfileId).Distinct().Count(),
                        Math.Round(g.Average(r => r.Percent) * 100, 1, MidpointRounding.AwayFromZero))
                    : null)
                .Where(c => c is not null).Select(c => c!).OrderBy(c => c.CourseCode).ToList();
            rows.Add(new ProgramOutcomeAttainment(plo.Id, plo.Code, plo.Title, plo.Description, plo.MasteryPoints, plo.MaxPoints, plo.CalculationMethod.ToString(),
                assessed, mastered, attainment,
                assessed == 0 ? null : Math.Round(perStudent.Average(v => v.Score ?? 0), 2, MidpointRounding.AwayFromZero),
                attainment is null ? null : attainment >= targetPercent,
                mappedIds.Select(id => clos[id]).OrderBy(c => c.Code).Select(c => c.Code + " " + c.Title).ToList(),
                contributions));
        }
        return new ProgramAttainmentReport(program.Id, program.Code, program.Name, program.DepartmentName, DateTime.UtcNow, targetPercent, semesterId,
            students.Count, rows);
    }

    private static MasteryCell Cell(Outcome outcome, IEnumerable<StudentOutcomeResult> evidence)
    {
        var list = evidence.ToList();
        var value = MasteryCalculator.Calculate(list.Select(r => (MasteryCalculator.ToScale(r.Percent, outcome.MaxPoints), r.AssessedAt)),
            outcome.CalculationMethod, outcome.CalculationParameter, outcome.MasteryPoints);
        var ratings = outcome.Ratings.Select(x => (x.Description, x.Points)).ToList();
        return new MasteryCell(outcome.Id, value.Score, value.Mastered, value.Count, MasteryCalculator.RatingFor(value.Score, ratings),
            MasteryCalculator.Status(value, outcome.MasteryPoints, ratings.Select(x => x.Points)));
    }
}
