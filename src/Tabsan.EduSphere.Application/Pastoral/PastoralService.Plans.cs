using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.Application.Pastoral;

// Support plans (learning adjustments, behaviour, wellbeing and health plans) and students' wellbeing check-ins.
public sealed partial class PastoralService
{
    private static SupportPlanDto PlanDto(SupportPlan p, PastoralAccess a, PastoralViewer v, IReadOnlyDictionary<Guid, string> names)
        => new(p.Id, p.StudentProfileId, p.Kind.ToString(), p.Title, p.Needs, p.Strategies, p.StartsOn, p.ReviewOn, p.Status.ToString(), p.Visibility.ToString(),
            p.SharedWithStudent, p.SharedWithParents, names.GetValueOrDefault(p.OwnerUserId) ?? "", CanManage(p, a, v.UserId), p.EndedAt);

    private static SharedPlanDto SharedPlan(SupportPlan p, IReadOnlyDictionary<Guid, string> names)
        => new(p.Id, p.Kind.ToString(), p.Title, p.Strategies, p.StartsOn, p.ReviewOn, names.GetValueOrDefault(p.OwnerUserId) ?? "");

    private async Task<(SupportPlan? Plan, PastoralStudent? Student, PastoralAccess Access)> LoadPlanAsync(PastoralViewer v, Guid id, CancellationToken ct)
    {
        var plan = await _store.PlanAsync(id, ct);
        if (plan is null) return (null, null, PastoralAccess.None);
        var s = (await _store.StudentsAsync([plan.StudentProfileId], ct)).FirstOrDefault();
        if (s is null) return (null, null, PastoralAccess.None);
        var a = AccessTo(v, s);
        return CanSee(plan, a, v.UserId) && (a.IsStaff || plan.OwnerUserId == v.UserId) ? (plan, s, a) : (null, null, PastoralAccess.None);
    }

    /// <summary>Writes a plan (the wellbeing team and admins) or changes one (its owner, the team and admins). A new plan
    /// staff can see is announced to the student's teachers; one shared with the student or family is announced to them.</summary>
    public async Task<PastoralResult<SupportPlanDto>> SavePlanAsync(PastoralViewer v, Guid? id, SavePlanRequest req, DateTime now, CancellationToken ct = default)
    {
        SupportPlan? plan = null;
        PastoralStudent? s;
        PastoralAccess a;
        if (id is { } existing)
        {
            (plan, s, a) = await LoadPlanAsync(v, existing, ct);
            if (plan is null || s is null) return PastoralResult<SupportPlanDto>.NotFound("Support plan not found.");
            if (!CanManage(plan, a, v.UserId)) return PastoralResult<SupportPlanDto>.Forbidden("Only the plan's owner, the wellbeing team and admins change it.");
        }
        else
        {
            s = (await _store.StudentsAsync([req.StudentProfileId], ct)).FirstOrDefault();
            if (s is null) return PastoralResult<SupportPlanDto>.NotFound("Student not found.");
            a = AccessTo(v, s);
            if (!a.Manages) return PastoralResult<SupportPlanDto>.Forbidden("Support plans are written by the wellbeing team and admins.");
        }
        if (!TryParse(req.Kind, SupportPlanKind.LearningAdjustments, out var kind)) return PastoralResult<SupportPlanDto>.Invalid($"Unknown kind of plan \"{req.Kind}\".");

        var isNew = plan is null;
        plan ??= new SupportPlan(s.TenantId, s.CampusId, s.StudentProfileId, v.UserId);
        try
        {
            plan.Edit(kind, req.Title, req.Needs, req.Strategies, req.StartsOn ?? now.Date, req.ReviewOn);
            plan.SetSharing(VisibilityOf(req.Confidential), req.ShareWithStudent, req.ShareWithParents);
        }
        catch (ArgumentException ex) { return PastoralResult<SupportPlanDto>.Invalid(ex.Plain()); }
        if (isNew) _store.Add(plan);
        await _store.SaveChangesAsync(ct);
        await AuditAsync(isNew ? "SupportPlanWritten" : "SupportPlanChanged", "SupportPlan", plan.Id, v, ct);

        var names = await _store.NamesAsync([plan.OwnerUserId, v.UserId], ct);
        var by = names.GetValueOrDefault(v.UserId) ?? v.Member.Name;
        if (isNew && plan.Visibility == PastoralVisibility.Staff)
            await NotifyAsync("New support plan", $"{by} wrote a support plan for {Who(s)}: {Short(plan.Title, 80)}. Open their pastoral page to see what to do.",
                s.TeacherUserIds, v.UserId, ct);
        if (plan.IsShared && plan.SharedNotifiedAt is null)
        {
            if (plan.SharedWithStudent) await NotifyAsync("A support plan from school", $"{by}: {Short(plan.Title, 80)}", [s.UserId], v.UserId, ct);
            if (plan.SharedWithParents) await NotifyAsync("A support plan from school", $"For {s.Name}, from {by}: {Short(plan.Title, 80)}", s.ParentUserIds, v.UserId, ct);
            plan.MarkSharedNotified(now);
            await _store.SaveChangesAsync(ct);
        }
        return PastoralResult<SupportPlanDto>.Success(PlanDto(plan, a, v, names), isNew ? 201 : 200);
    }

    /// <param name="ended">True ends the plan, false starts it again.</param>
    public async Task<PastoralResult<SupportPlanDto>> SetPlanEndedAsync(PastoralViewer v, Guid id, bool ended, DateTime now, CancellationToken ct = default)
    {
        var (plan, s, a) = await LoadPlanAsync(v, id, ct);
        if (plan is null || s is null) return PastoralResult<SupportPlanDto>.NotFound("Support plan not found.");
        if (!CanManage(plan, a, v.UserId)) return PastoralResult<SupportPlanDto>.Forbidden("Only the plan's owner, the wellbeing team and admins change it.");
        try
        {
            if (ended) plan.End(now);
            else plan.Restart();
        }
        catch (InvalidOperationException ex) { return PastoralResult<SupportPlanDto>.Invalid(ex.Message); }
        await _store.SaveChangesAsync(ct);
        await AuditAsync(ended ? "SupportPlanEnded" : "SupportPlanRestarted", "SupportPlan", plan.Id, v, ct);
        return PastoralResult<SupportPlanDto>.Success(PlanDto(plan, a, v, await _store.NamesAsync([plan.OwnerUserId], ct)));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Check-ins
    // ═════════════════════════════════════════════════════════════════════════

    /// <param name="forStudent">The student's own view: the team's note is left out.</param>
    private async Task<List<CheckInDto>> CheckInDtosAsync(IReadOnlyList<WellbeingCheckIn> list, IReadOnlyDictionary<Guid, PastoralStudent> students,
        CancellationToken ct, bool forStudent = false)
    {
        if (list.Count == 0) return [];
        var names = await _store.NamesAsync(list.Where(c => c.AcknowledgedByUserId != null).Select(c => c.AcknowledgedByUserId!.Value).Distinct().ToList(), ct);
        return list.Select(c =>
        {
            var s = students.GetValueOrDefault(c.StudentProfileId);
            return new CheckInDto(c.Id, c.StudentProfileId, s?.Name ?? "", s?.ClassName, c.Mood, c.Note, c.WantsToTalk, c.CheckedInAt, c.NeedsAttention,
                c.AcknowledgedAt, c.AcknowledgedByUserId is { } by ? names.GetValueOrDefault(by) : null, forStudent ? null : c.AcknowledgeNote);
        }).ToList();
    }

    /// <summary>A student says how they are (a second answer within six hours changes the first). Asking to talk, or a very
    /// low mood, tells the wellbeing team at once.</summary>
    public async Task<PastoralResult<CheckInDto>> CheckInAsync(PastoralViewer v, CheckInRequest req, DateTime now, CancellationToken ct = default)
    {
        if (!v.IsStudent || v.StudentProfileId is not { } profile) return PastoralResult<CheckInDto>.Forbidden("Only students check in.");
        var s = (await _store.StudentsAsync([profile], ct)).FirstOrDefault();
        if (s is null) return PastoralResult<CheckInDto>.NotFound("Student not found.");
        if (!await CheckInsEnabledAsync(s.TenantId, ct)) return PastoralResult<CheckInDto>.Forbidden("Your school has not switched check-ins on.");

        var latest = (await _store.CheckInsOfAsync(profile, 1, ct)).FirstOrDefault();
        var isNew = latest is null || now - latest.CheckedInAt > WellbeingCheckIn.SameCheckIn;
        var checkIn = isNew ? new WellbeingCheckIn(s.TenantId, s.CampusId, profile, v.UserId) : latest!;
        try { checkIn.Answer(req.Mood, req.Note, req.WantsToTalk, now); }
        catch (ArgumentException ex) { return PastoralResult<CheckInDto>.Invalid(ex.Plain()); }
        if (isNew) _store.Add(checkIn);
        await _store.SaveChangesAsync(ct);

        if (checkIn.Urgent && checkIn.AlertedAt is null)
        {
            var body = checkIn.WantsToTalk
                ? $"{Who(s)} would like to talk to someone. Open the pastoral page to follow it up."
                : $"{Who(s)} checked in feeling very low. Open the pastoral page to follow it up.";
            await NotifyAsync(checkIn.WantsToTalk ? "A student would like to talk" : "A student is feeling very low", body, await TeamForAsync(s, false, ct), null, ct);
            checkIn.MarkAlerted(now);
            await _store.SaveChangesAsync(ct);
        }
        var students = new Dictionary<Guid, PastoralStudent> { [profile] = s };
        return PastoralResult<CheckInDto>.Success((await CheckInDtosAsync([checkIn], students, ct, forStudent: true)).Single(), isNew ? 201 : 200);
    }

    /// <summary>Someone on the wellbeing team (or an admin) has seen to a check-in.</summary>
    public async Task<PastoralResult<CheckInDto>> AcknowledgeCheckInAsync(PastoralViewer v, Guid id, string? note, DateTime now, CancellationToken ct = default)
    {
        var checkIn = await _store.CheckInAsync(id, ct);
        var s = checkIn is null ? null : (await _store.StudentsAsync([checkIn.StudentProfileId], ct)).FirstOrDefault();
        if (checkIn is null || s is null || !AccessTo(v, s).Manages) return PastoralResult<CheckInDto>.NotFound("Check-in not found.");
        try { checkIn.Acknowledge(v.UserId, note, now); }
        catch (ArgumentException ex) { return PastoralResult<CheckInDto>.Invalid(ex.Plain()); }
        await _store.SaveChangesAsync(ct);
        await AuditAsync("WellbeingCheckInSeen", "WellbeingCheckIn", checkIn.Id, v, ct);
        return PastoralResult<CheckInDto>.Success((await CheckInDtosAsync([checkIn], new Dictionary<Guid, PastoralStudent> { [s.StudentProfileId] = s }, ct)).Single());
    }
}
