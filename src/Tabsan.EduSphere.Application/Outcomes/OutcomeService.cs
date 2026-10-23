using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.Application.Outcomes;

/// <summary>
/// Plan M6.1/M6.2 — outcomes (institution, program, course), rating scales, CLO → PLO mappings, Excel import and
/// alignments. Admins manage every level; teachers manage course outcomes and alignments. Outcomes are kept per tenant;
/// installation-wide ones (no tenant) are managed by Super Admins.
/// </summary>
public sealed class OutcomeService
{
    private readonly IOutcomeRepository _repo;
    private readonly IOutcomeResultSync _sync;
    private readonly IAuditService _audit;

    public OutcomeService(IOutcomeRepository repo, IOutcomeResultSync sync, IAuditService audit)
    {
        _repo = repo;
        _sync = sync;
        _audit = audit;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Outcomes
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<OutcomeResponse>> ListAsync(OutcomeActor actor, string? level, Guid? programId, Guid? courseId, bool includeInactive, CancellationToken ct = default)
    {
        OutcomeLevel? lvl = Enum.TryParse<OutcomeLevel>(level, true, out var l) && Enum.IsDefined(l) ? l : null;
        var outcomes = await _repo.ListAsync(actor.TenantId, actor.IsSuperAdmin, lvl, programId, courseId, includeInactive, ct);
        return await ToResponsesAsync(outcomes, ct);
    }

    public async Task<OutcomeResponse?> GetAsync(OutcomeActor actor, Guid id, CancellationToken ct = default)
    {
        var outcome = await _repo.GetAsync(id, ct);
        return outcome is not null && Visible(actor, outcome) ? (await ToResponsesAsync([outcome], ct))[0] : null;
    }

    public async Task<OutcomeResult<OutcomeResponse>> CreateAsync(OutcomeActor actor, SaveOutcomeRequest r, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OutcomeLevel>(r.Level, true, out var level) || !Enum.IsDefined(level))
            return OutcomeResult<OutcomeResponse>.Fail(400, "Level must be Institution, Program or Course.");
        if (!CanManageLevel(actor, level)) return OutcomeResult<OutcomeResponse>.Fail(403, "Only administrators manage institution and program outcomes.");
        if (await ValidateAsync(actor, level, r, null, ct) is { } error) return OutcomeResult<OutcomeResponse>.Fail(400, error);

        Outcome outcome;
        try
        {
            outcome = new Outcome(actor.TenantId, level, r.ProgramId, r.CourseId, r.Code, r.Title, actor.UserId);
            outcome.Update(r.Code, r.Title, r.Description, r.ParentId);
            ApplyScale(outcome, r);
        }
        catch (ArgumentException ex) { return OutcomeResult<OutcomeResponse>.Fail(400, ex.Message); }
        _repo.Add(outcome);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("OutcomeCreated", "Outcome", outcome.Id.ToString(), actorUserId: actor.UserId,
            newValuesJson: $"{{\"level\":\"{level}\",\"code\":\"{Escape(outcome.Code)}\"}}"), ct);
        return OutcomeResult<OutcomeResponse>.Success((await ToResponsesAsync([outcome], ct))[0], 201);
    }

    public async Task<OutcomeResult<OutcomeResponse>> UpdateAsync(OutcomeActor actor, Guid id, SaveOutcomeRequest r, CancellationToken ct = default)
    {
        var outcome = await _repo.GetAsync(id, ct);
        if (outcome is null || !Visible(actor, outcome)) return OutcomeResult<OutcomeResponse>.Fail(404, "Outcome not found.");
        if (!CanEdit(actor, outcome)) return OutcomeResult<OutcomeResponse>.Fail(403, "You cannot change this outcome.");
        // Level, program and course are fixed once created (evidence and mappings depend on them).
        var fixedRequest = r with { Level = outcome.Level.ToString(), ProgramId = outcome.ProgramId, CourseId = outcome.CourseId };
        if (await ValidateAsync(actor, outcome.Level, fixedRequest, outcome, ct) is { } error) return OutcomeResult<OutcomeResponse>.Fail(400, error);
        try
        {
            outcome.Update(r.Code, r.Title, r.Description, r.ParentId);
            if (r.Ratings is not null || r.MasteryPoints is not null || r.CalculationMethod is not null) ApplyScale(outcome, r);
        }
        catch (ArgumentException ex) { return OutcomeResult<OutcomeResponse>.Fail(400, ex.Message); }
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("OutcomeUpdated", "Outcome", outcome.Id.ToString(), actorUserId: actor.UserId), ct);
        return OutcomeResult<OutcomeResponse>.Success((await ToResponsesAsync([outcome], ct))[0]);
    }

    public async Task<OutcomeResult<bool>> SetActiveAsync(OutcomeActor actor, Guid id, bool active, CancellationToken ct = default)
    {
        var outcome = await _repo.GetAsync(id, ct);
        if (outcome is null || !Visible(actor, outcome)) return OutcomeResult<bool>.Fail(404, "Outcome not found.");
        if (!CanEdit(actor, outcome)) return OutcomeResult<bool>.Fail(403, "You cannot change this outcome.");
        outcome.SetActive(active);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog(active ? "OutcomeActivated" : "OutcomeDeactivated", "Outcome", outcome.Id.ToString(), actorUserId: actor.UserId), ct);
        return OutcomeResult<bool>.Success(true);
    }

    public async Task<OutcomeResult<OutcomeResponse>> SetMappingsAsync(OutcomeActor actor, Guid courseOutcomeId, SetOutcomeMappingsRequest r, CancellationToken ct = default)
    {
        var outcome = await _repo.GetAsync(courseOutcomeId, ct);
        if (outcome is null || !Visible(actor, outcome)) return OutcomeResult<OutcomeResponse>.Fail(404, "Outcome not found.");
        if (outcome.Level != OutcomeLevel.Course) return OutcomeResult<OutcomeResponse>.Fail(400, "Only course outcomes are mapped to program outcomes.");
        if (!CanEdit(actor, outcome)) return OutcomeResult<OutcomeResponse>.Fail(403, "You cannot change this outcome.");
        var wanted = (r.ProgramOutcomeIds ?? []).Distinct().ToList();
        var targets = await _repo.GetManyAsync(wanted, ct);
        if (targets.Count != wanted.Count || targets.Any(t => t.Level != OutcomeLevel.Program || !Visible(actor, t)))
            return OutcomeResult<OutcomeResponse>.Fail(400, "Map to program outcomes only.");

        var current = await _repo.GetMappingsAsync([courseOutcomeId], null, ct);
        foreach (var m in current.Where(m => !wanted.Contains(m.ProgramOutcomeId))) _repo.Remove(m);
        foreach (var id in wanted.Where(id => current.All(m => m.ProgramOutcomeId != id))) _repo.Add(new OutcomeMapping(courseOutcomeId, id));
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("OutcomeMappingsChanged", "Outcome", outcome.Id.ToString(), actorUserId: actor.UserId,
            newValuesJson: $"{{\"programOutcomes\":{wanted.Count}}}"), ct);
        return OutcomeResult<OutcomeResponse>.Success((await ToResponsesAsync([outcome], ct))[0]);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Import
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Creates or updates outcomes from the workbook rows (matched on level + program/course + code), then maps CLOs to PLOs.</summary>
    public async Task<OutcomeImportResult> ImportAsync(OutcomeActor actor, IReadOnlyList<OutcomeImportRow> rows, CancellationToken ct = default)
    {
        var errors = new List<string>();
        int created = 0, updated = 0, mapped = 0;
        var imported = new List<(OutcomeImportRow Row, Outcome Outcome)>();

        foreach (var row in rows.Take(2000))
        {
            string Err(string text) { errors.Add($"Row {row.RowNumber}: {text}"); return text; }
            if (!Enum.TryParse<OutcomeLevel>(row.Level, true, out var level) || !Enum.IsDefined(level)) { Err("level must be Institution, Program or Course."); continue; }
            if (!CanManageLevel(actor, level)) { Err("only administrators import institution and program outcomes."); continue; }

            Guid? programId = null, courseId = null;
            if (level == OutcomeLevel.Program)
            {
                var program = string.IsNullOrWhiteSpace(row.ProgramCode) ? null : await _repo.FindProgramByCodeAsync(row.ProgramCode.Trim(), ct);
                if (program is null) { Err($"program \"{row.ProgramCode}\" was not found."); continue; }
                programId = program.Id;
            }
            if (level == OutcomeLevel.Course)
            {
                var courses = string.IsNullOrWhiteSpace(row.CourseCode) ? [] : await _repo.FindCoursesByCodeAsync(row.CourseCode.Trim(), actor.IsSuperAdmin ? null : actor.TenantId, ct);
                if (courses.Count != 1) { Err(courses.Count == 0 ? $"course \"{row.CourseCode}\" was not found." : $"course code \"{row.CourseCode}\" is ambiguous."); continue; }
                courseId = courses[0].Id;
            }

            var request = new SaveOutcomeRequest(level.ToString(), programId, courseId, null, row.Code, row.Title, row.Description, null,
                row.MasteryPoints, row.Method, row.Parameter);
            var existing = await _repo.FindByCodeAsync(actor.TenantId, level, programId, courseId, row.Code.Trim(), ct);
            if (existing is not null && !CanEdit(actor, existing)) { Err($"outcome {row.Code} cannot be changed by you."); continue; }
            if (await ValidateAsync(actor, level, request, existing, ct) is { } invalid) { Err(invalid); continue; }
            try
            {
                if (existing is null)
                {
                    existing = new Outcome(actor.TenantId, level, programId, courseId, row.Code, row.Title, actor.UserId);
                    existing.Update(row.Code, row.Title, row.Description, null);
                    ApplyScale(existing, request);
                    _repo.Add(existing);
                    created++;
                }
                else
                {
                    existing.Update(row.Code, row.Title, row.Description, existing.ParentId);
                    if (row.MasteryPoints is not null || row.Method is not null) ApplyScale(existing, request);
                    updated++;
                }
            }
            catch (ArgumentException ex) { Err(ex.Message); continue; }
            imported.Add((row, existing));
        }
        await _repo.SaveChangesAsync(ct);

        // Parents and CLO → PLO mappings, once every row exists.
        foreach (var (row, outcome) in imported)
        {
            if (!string.IsNullOrWhiteSpace(row.ParentCode))
            {
                var parent = await _repo.FindByCodeAsync(actor.TenantId, outcome.Level, outcome.ProgramId, outcome.CourseId, row.ParentCode.Trim(), ct);
                if (parent is null || parent.Id == outcome.Id) errors.Add($"Row {row.RowNumber}: parent \"{row.ParentCode}\" was not found on the same level.");
                else outcome.Update(outcome.Code, outcome.Title, outcome.Description, parent.Id);
            }
            if (outcome.Level != OutcomeLevel.Course || string.IsNullOrWhiteSpace(row.MapsTo)) continue;
            var current = await _repo.GetMappingsAsync([outcome.Id], null, ct);
            foreach (var code in row.MapsTo.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                // "PROGRAM:PLO1", or just "PLO1": first among the programs of the course's department, then among all visible ones.
                var parts = code.Split(':', 2, StringSplitOptions.TrimEntries);
                var candidates = (await _repo.ListAsync(actor.TenantId, actor.IsSuperAdmin, OutcomeLevel.Program, null, null, false, ct))
                    .Where(o => string.Equals(o.Code, parts[^1], StringComparison.OrdinalIgnoreCase)).ToList();
                if (parts.Length == 2)
                {
                    var program = await _repo.FindProgramByCodeAsync(parts[0], ct);
                    candidates = candidates.Where(o => o.ProgramId == program?.Id).ToList();
                }
                else if (candidates.Count > 1 && (await _repo.GetCoursesAsync([outcome.CourseId!.Value], ct)).FirstOrDefault() is { } course)
                {
                    var departmentPrograms = (await _repo.GetProgramsAsync(null, course.DepartmentId, ct)).Select(p => p.Id).ToHashSet();
                    var local = candidates.Where(o => o.ProgramId is { } p && departmentPrograms.Contains(p)).ToList();
                    if (local.Count > 0) candidates = local;
                }
                if (candidates.Count != 1) { errors.Add($"Row {row.RowNumber}: program outcome \"{code}\" {(candidates.Count == 0 ? "was not found" : "is ambiguous — write PROGRAM:CODE")}."); continue; }
                if (current.Any(m => m.ProgramOutcomeId == candidates[0].Id)) continue;
                _repo.Add(new OutcomeMapping(outcome.Id, candidates[0].Id));
                mapped++;
            }
        }
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("OutcomesImported", "Outcome", null, actorUserId: actor.UserId,
            newValuesJson: $"{{\"created\":{created},\"updated\":{updated},\"mapped\":{mapped},\"errors\":{errors.Count}}}"), ct);
        return new OutcomeImportResult(created, updated, mapped, errors);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Alignments
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<OutcomeResult<AlignmentTargetResponse>> GetAlignmentsAsync(OutcomeActor actor, string targetType, Guid targetId, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return OutcomeResult<AlignmentTargetResponse>.Fail(403, "Only staff align outcomes.");
        if (!TryTarget(targetType, out var type)) return OutcomeResult<AlignmentTargetResponse>.Fail(400, "Unknown target type.");
        var target = await _repo.GetTargetAsync(type, targetId, ct);
        if (target is null) return OutcomeResult<AlignmentTargetResponse>.Fail(404, "Not found.");
        return OutcomeResult<AlignmentTargetResponse>.Success(await BuildTargetAsync(actor, target, ct));
    }

    public async Task<OutcomeResult<AlignmentTargetResponse>> SetAlignmentsAsync(OutcomeActor actor, SetAlignmentsRequest r, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return OutcomeResult<AlignmentTargetResponse>.Fail(403, "Only staff align outcomes.");
        if (!TryTarget(r.TargetType, out var type)) return OutcomeResult<AlignmentTargetResponse>.Fail(400, "Unknown target type.");
        var target = await _repo.GetTargetAsync(type, r.TargetId, ct);
        if (target is null) return OutcomeResult<AlignmentTargetResponse>.Fail(404, "Not found.");

        var wanted = (r.OutcomeIds ?? []).Distinct().ToList();
        var current = await _repo.GetAlignmentsAsync([(type, r.TargetId)], ct);
        var available = (await AvailableForAsync(actor, target, ct)).Select(o => o.Id).ToHashSet();
        if (wanted.Any(id => !available.Contains(id) && current.All(a => a.OutcomeId != id)))
            return OutcomeResult<AlignmentTargetResponse>.Fail(400, "One of the outcomes cannot be used here.");

        var changed = false;
        foreach (var a in current.Where(a => !wanted.Contains(a.OutcomeId))) { _repo.Remove(a); changed = true; }
        foreach (var id in wanted.Where(id => current.All(a => a.OutcomeId != id)))
        {
            _repo.Add(new OutcomeAlignment(id, type, r.TargetId, target.CourseOfferingId, actor.UserId));
            changed = true;
        }
        if (changed)
        {
            await _repo.SaveChangesAsync(ct);
            await _audit.LogAsync(new AuditLog("OutcomeAlignmentsChanged", type.ToString(), r.TargetId.ToString(), actorUserId: actor.UserId,
                newValuesJson: $"{{\"outcomes\":{wanted.Count}}}"), ct);
            // Work graded before the alignment counts too.
            var source = type is OutcomeTargetType.Assignment or OutcomeTargetType.RubricCriterion ? OutcomeSourceType.AssignmentSubmission : OutcomeSourceType.QuizAttempt;
            var offerings = target.CourseOfferingId is { } o ? [o] : await _repo.GetOfferingsUsingQuestionAsync(r.TargetId, ct);
            foreach (var offering in offerings) await _sync.RefreshAsync(offering, null, source, ct);
        }
        return OutcomeResult<AlignmentTargetResponse>.Success(await BuildTargetAsync(actor, target, ct));
    }

    /// <summary>Outcomes a course can use: institution outcomes, outcomes of programs in the course's department or mapped from its CLOs, and the course's own.</summary>
    public async Task<IReadOnlyList<OutcomeSummary>> AvailableForOfferingAsync(OutcomeActor actor, Guid offeringId, CancellationToken ct = default)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        return offering is null ? [] : (await AvailableAsync(actor, offering.CourseId, offering.DepartmentId, ct)).Select(Summary).ToList();
    }

    public Task RecalculateOfferingAsync(Guid offeringId, CancellationToken ct = default) => _sync.RefreshAsync(offeringId, null, null, ct);

    private async Task<AlignmentTargetResponse> BuildTargetAsync(OutcomeActor actor, OutcomeTargetInfo target, CancellationToken ct)
    {
        var aligned = await _repo.GetAlignmentsAsync([(target.Type, target.Id)], ct);
        var alignedOutcomes = await _repo.GetManyAsync(aligned.Select(a => a.OutcomeId).ToList(), ct);
        var available = await AvailableForAsync(actor, target, ct);
        return new AlignmentTargetResponse(target.Type.ToString(), target.Id, target.Title, target.CourseOfferingId,
            alignedOutcomes.OrderBy(o => o.Level).ThenBy(o => o.Code).Select(Summary).ToList(),
            available.Select(Summary).ToList());
    }

    private async Task<IReadOnlyList<Outcome>> AvailableForAsync(OutcomeActor actor, OutcomeTargetInfo target, CancellationToken ct)
    {
        if (target.CourseOfferingId is { } offeringId && await _repo.GetOfferingAsync(offeringId, ct) is { } offering)
            return await AvailableAsync(actor, offering.CourseId, offering.DepartmentId, ct);
        // A bank without a course: institution outcomes and those of the bank's department programs.
        return await AvailableAsync(actor, null, target.DepartmentId, ct);
    }

    private async Task<IReadOnlyList<Outcome>> AvailableAsync(OutcomeActor actor, Guid? courseId, Guid? departmentId, CancellationToken ct)
    {
        var all = await _repo.ListAsync(actor.TenantId, actor.IsSuperAdmin, null, null, null, false, ct);
        var programIds = departmentId is { } dept ? (await _repo.GetProgramsAsync(null, dept, ct)).Select(p => p.Id).ToHashSet() : [];
        var courseOutcomes = all.Where(o => o.Level == OutcomeLevel.Course && o.CourseId == courseId && courseId is not null).ToList();
        if (courseOutcomes.Count > 0)
        {
            var mapped = await _repo.GetMappingsAsync(courseOutcomes.Select(o => o.Id).ToList(), null, ct);
            var mappedPlos = mapped.Select(m => m.ProgramOutcomeId).ToHashSet();
            foreach (var p in all.Where(o => mappedPlos.Contains(o.Id) && o.ProgramId is not null)) programIds.Add(p.ProgramId!.Value);
        }
        return all.Where(o => o.Level == OutcomeLevel.Institution
                              || (o.Level == OutcomeLevel.Program && o.ProgramId is { } p && (departmentId is null || programIds.Contains(p)))
                              || (o.Level == OutcomeLevel.Course && courseId is not null && o.CourseId == courseId))
            .OrderByDescending(o => o.Level).ThenBy(o => o.Code).ToList();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    public static bool Visible(OutcomeActor actor, Outcome o) => actor.IsSuperAdmin || o.TenantId is null || o.TenantId == actor.TenantId;

    private static bool CanManageLevel(OutcomeActor actor, OutcomeLevel level) => level == OutcomeLevel.Course ? actor.IsStaff : actor.IsAdmin;

    private static bool CanEdit(OutcomeActor actor, Outcome o)
        => CanManageLevel(actor, o.Level) && (actor.IsSuperAdmin || (o.TenantId is not null && o.TenantId == actor.TenantId) || (o.TenantId is null && actor.TenantId is null));

    private static bool TryTarget(string? value, out OutcomeTargetType type)
        => Enum.TryParse(value, true, out type) && Enum.IsDefined(type);

    private async Task<string?> ValidateAsync(OutcomeActor actor, OutcomeLevel level, SaveOutcomeRequest r, Outcome? existing, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Code) || r.Code.Trim().Length > 40) return "Enter a code (up to 40 characters).";
        if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Trim().Length > 300) return "Enter a title (up to 300 characters).";
        if (r.Description is { Length: > 4000 }) return "The description is limited to 4000 characters.";
        if (level == OutcomeLevel.Program)
        {
            if (r.ProgramId is not { } programId || await _repo.GetProgramAsync(programId, ct) is null) return "Choose the program.";
        }
        if (level == OutcomeLevel.Course)
        {
            if (r.CourseId is not { } courseId) return "Choose the course.";
            var course = (await _repo.GetCoursesAsync([courseId], ct)).FirstOrDefault();
            if (course is null) return "Choose the course.";
            if (!actor.IsSuperAdmin && course.TenantId is not null && actor.TenantId is not null && course.TenantId != actor.TenantId) return "Choose a course of your institution.";
        }
        var clash = await _repo.FindByCodeAsync(actor.TenantId, level, r.ProgramId, r.CourseId, r.Code.Trim(), ct);
        if (clash is not null && clash.Id != existing?.Id) return $"The code {r.Code.Trim()} is already used here.";
        if (r.ParentId is { } parentId)
        {
            var parent = await _repo.GetAsync(parentId, ct);
            if (parent is null || parent.Level != level || parent.ProgramId != r.ProgramId || parent.CourseId != r.CourseId || parent.Id == existing?.Id)
                return "The parent must be another outcome on the same level.";
        }
        return null;
    }

    private static void ApplyScale(Outcome outcome, SaveOutcomeRequest r)
    {
        var method = Enum.TryParse<MasteryCalculationMethod>(r.CalculationMethod, true, out var m) && Enum.IsDefined(m) ? m : outcome.CalculationMethod;
        if (r.CalculationMethod is not null && !Enum.TryParse<MasteryCalculationMethod>(r.CalculationMethod, true, out _))
            throw new ArgumentException("The calculation method must be Highest, Latest, Average, DecayingAverage or NMastery.");
        var levels = r.Ratings is { Count: > 0 }
            ? r.Ratings.Select(x => (x.Description ?? "", x.Points)).ToList()
            : outcome.Ratings.Count > 0
                ? outcome.Ratings.Select(x => (x.Description, x.Points)).ToList()
                : [("Exceeds mastery", 4m), ("Meets mastery", 3m), ("Near mastery", 2m), ("Below mastery", 1m), ("No evidence", 0m)];
        var mastery = r.MasteryPoints ?? (outcome.MasteryPoints > 0 ? outcome.MasteryPoints : 3m);
        var parameter = r.CalculationParameter ?? (method == outcome.CalculationMethod ? outcome.CalculationParameter : null)
            ?? method switch { MasteryCalculationMethod.DecayingAverage => 65, MasteryCalculationMethod.NMastery => 5, _ => (int?)null };
        outcome.SetScale(levels, mastery, method, parameter);
    }

    private async Task<IReadOnlyList<OutcomeResponse>> ToResponsesAsync(IReadOnlyList<Outcome> outcomes, CancellationToken ct)
    {
        if (outcomes.Count == 0) return [];
        var ids = outcomes.Select(o => o.Id).ToList();
        var mappings = (await _repo.GetMappingsAsync(ids, null, ct)).ToLookup(m => m.CourseOutcomeId, m => m.ProgramOutcomeId);
        var counts = await _repo.CountAlignmentsAsync(ids, ct);
        var programs = (await _repo.GetProgramsAsync(outcomes.Where(o => o.ProgramId is not null).Select(o => o.ProgramId!.Value).Distinct().ToList(), null, ct))
            .ToDictionary(p => p.Id);
        var courses = (await _repo.GetCoursesAsync(outcomes.Where(o => o.CourseId is not null).Select(o => o.CourseId!.Value).Distinct().ToList(), ct))
            .ToDictionary(c => c.Id);
        return outcomes.Select(o => new OutcomeResponse(o.Id, o.Level.ToString(), o.ProgramId,
            o.ProgramId is { } p && programs.TryGetValue(p, out var prog) ? $"{prog.Code} — {prog.Name}" : null,
            o.CourseId, o.CourseId is { } c && courses.TryGetValue(c, out var course) ? $"{course.Code} — {course.Title}" : null,
            o.ParentId, o.Code, o.Title, o.Description, o.MasteryPoints, o.MaxPoints, o.CalculationMethod.ToString(), o.CalculationParameter, o.IsActive,
            o.Ratings.OrderByDescending(x => x.Points).Select(x => new OutcomeRatingDto(x.Description, x.Points)).ToList(),
            mappings[o.Id].ToList(), counts.TryGetValue(o.Id, out var n) ? n : 0, o.TenantId is null)).ToList();
    }

    private static OutcomeSummary Summary(Outcome o) => new(o.Id, o.Code, o.Title, o.Level.ToString());

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
