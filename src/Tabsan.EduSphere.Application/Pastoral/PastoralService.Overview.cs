using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.Application.Pastoral;

// The staff overview (by campus, year level or class over a period), and what students and parents see.
public sealed partial class PastoralService
{
    /// <summary>The overview for staff: totals and categories for the period, every student with notes, open follow-ups
    /// or a running plan (teachers: the students they teach), check-ins waiting for someone, and the person's own
    /// follow-ups.</summary>
    public async Task<PastoralResult<OverviewDto>> OverviewAsync(PastoralViewer v, OverviewQuery q, DateTime now, CancellationToken ct = default)
    {
        if (!v.IsStaff) return PastoralResult<OverviewDto>.Forbidden("This page is for staff.");
        if (q.To <= q.From || q.To - q.From > TimeSpan.FromDays(400)) return PastoralResult<OverviewDto>.Invalid("Choose a period of up to 400 days.");
        var me = await MeAsync(v, ct);
        var tenant = v.TenantId;

        var records = await _store.RecordsAsync(tenant, q.From, q.To, ct);
        var followUps = await _store.OpenFollowUpsAsync(tenant, ct);
        var plans = await _store.ActivePlansAsync(tenant, ct);
        var checkIns = me.SeesCheckIns ? await _store.CheckInsAsync(tenant, q.From, ct) : [];
        var waiting = me.SeesCheckIns ? await _store.CheckInsNeedingAttentionAsync(tenant, ct) : [];

        var ids = records.Select(r => r.StudentProfileId).Concat(followUps.Select(r => r.StudentProfileId)).Concat(plans.Select(p => p.StudentProfileId))
            .Concat(checkIns.Select(c => c.StudentProfileId)).Concat(waiting.Select(c => c.StudentProfileId)).Distinct().ToList();
        var students = (await _store.StudentsAsync(ids, ct))
            .Where(s => (q.CampusId is null || s.Campuses.Contains(q.CampusId.Value)) && (q.SemesterId is null || s.Semesters.Contains(q.SemesterId.Value))
                        && (q.CourseOfferingId is null || s.Offerings.Contains(q.CourseOfferingId.Value)))
            .Select(s => (Student: s, Access: AccessTo(v, s))).Where(x => x.Access.IsStaff)
            .ToDictionary(x => x.Student.StudentProfileId);

        var seen = records.Where(r => students.TryGetValue(r.StudentProfileId, out var x) && CanSee(r, x.Access, v.UserId)).ToList();
        var open = followUps.Where(r => students.TryGetValue(r.StudentProfileId, out var x) && CanSee(r, x.Access, v.UserId)).ToList();
        var running = plans.Where(p => students.TryGetValue(p.StudentProfileId, out var x) && CanSee(p, x.Access, v.UserId)).ToList();
        var moods = checkIns.Where(c => students.TryGetValue(c.StudentProfileId, out var x) && x.Access.Manages).ToList();
        var attention = waiting.Where(c => students.TryGetValue(c.StudentProfileId, out var x) && x.Access.Manages).OrderByDescending(c => c.WantsToTalk)
            .ThenBy(c => c.Mood).ThenByDescending(c => c.CheckedInAt).ToList();

        var rows = students.Values.Select(x =>
        {
            var mine = seen.Where(r => r.StudentProfileId == x.Student.StudentProfileId).ToList();
            var lastMood = moods.Where(c => c.StudentProfileId == x.Student.StudentProfileId).OrderByDescending(c => c.CheckedInAt).FirstOrDefault();
            return new OverviewRowDto(Option(x.Student), mine.Count(r => r.Tone == PastoralTone.Positive), mine.Count(r => r.Tone == PastoralTone.Concern),
                mine.Count(r => r.Kind == PastoralKind.Wellbeing), mine.Count(r => r.Tone == PastoralTone.Neutral && r.Kind != PastoralKind.Wellbeing),
                open.Count(r => r.StudentProfileId == x.Student.StudentProfileId), running.Count(p => p.StudentProfileId == x.Student.StudentProfileId),
                lastMood?.Mood, attention.Any(c => c.StudentProfileId == x.Student.StudentProfileId),
                mine.Count == 0 ? null : mine.Max(r => r.OccurredAt));
        })
        .Where(r => r.Positive + r.Concerns + r.Wellbeing + r.Other + r.OpenFollowUps + r.ActivePlans > 0 || r.NeedsAttention || r.LastMood is not null)
        .OrderByDescending(r => r.NeedsAttention).ThenByDescending(r => r.Concerns).ThenByDescending(r => r.OpenFollowUps)
        .ThenBy(r => r.Student.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        var totals = new PastoralCountsDto(seen.Count(r => r.Tone == PastoralTone.Positive), seen.Count(r => r.Tone == PastoralTone.Concern),
            seen.Count(r => r.Kind == PastoralKind.Wellbeing), seen.Count(r => r.Tone == PastoralTone.Neutral && r.Kind != PastoralKind.Wellbeing),
            open.Count, running.Count);
        var categories = seen.GroupBy(r => (r.Kind, r.Tone, Category: r.Category ?? ""))
            .Select(g => new CategoryCountDto(g.Key.Kind.ToString(), g.Key.Tone.ToString(), g.Key.Category, g.Count()))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Category, StringComparer.CurrentCultureIgnoreCase).Take(30).ToList();

        var byId = students.ToDictionary(x => x.Key, x => x.Value.Student);
        var myFollowUps = await RecordDtosAsync(open.Where(r => r.FollowUpUserId == v.UserId).OrderBy(r => r.FollowUpBy ?? DateTime.MaxValue).ToList(), byId, v, ct);
        return PastoralResult<OverviewDto>.Success(new OverviewDto(q.From, q.To, totals, categories, rows, await CheckInDtosAsync(attention, byId, ct),
            myFollowUps, await FiltersAsync(v, ct), me));
    }

    /// <summary>A student's own page: the notes and plans shared with them, and their check-ins.</summary>
    public async Task<PastoralResult<MyWellbeingDto>> MineAsync(PastoralViewer v, CancellationToken ct = default)
    {
        if (!v.IsStudent || v.StudentProfileId is not { } profile) return PastoralResult<MyWellbeingDto>.Forbidden("This page is for students.");
        var s = (await _store.StudentsAsync([profile], ct)).FirstOrDefault();
        if (s is null) return PastoralResult<MyWellbeingDto>.NotFound("Student not found.");
        var a = AccessTo(v, s);
        var records = (await _store.RecordsOfAsync(profile, ct)).Where(r => !r.IsRemoved && CanSee(r, a, v.UserId)).OrderByDescending(r => r.OccurredAt).Take(100).ToList();
        var plans = (await _store.PlansOfAsync(profile, ct)).Where(p => CanSee(p, a, v.UserId)).ToList();
        var names = await _store.NamesAsync(records.Select(r => r.AuthorUserId).Concat(plans.Select(p => p.OwnerUserId)).Distinct().ToList(), ct);
        var history = await CheckInDtosAsync(await _store.CheckInsOfAsync(profile, 14, ct), new Dictionary<Guid, PastoralStudent> { [profile] = s }, ct, forStudent: true);
        return PastoralResult<MyWellbeingDto>.Success(new MyWellbeingDto(await CheckInsEnabledAsync(s.TenantId, ct), history.FirstOrDefault(), history,
            records.Select(r => Shared(r, names)).ToList(), plans.Select(p => SharedPlan(p, names)).ToList()));
    }

    /// <summary>For parents: each child's notes and plans shared with the family.</summary>
    public async Task<PastoralResult<IReadOnlyList<FamilyChildDto>>> FamilyAsync(PastoralViewer v, CancellationToken ct = default)
    {
        if (!v.IsParent) return PastoralResult<IReadOnlyList<FamilyChildDto>>.Forbidden("This page is for parents.");
        var students = await _store.StudentsAsync(v.Member.Children.Select(c => c.StudentProfileId).Distinct().ToList(), ct);
        var children = new List<FamilyChildDto>();
        foreach (var s in students.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var a = AccessTo(v, s);
            if (!a.IsParent) continue;
            var records = (await _store.RecordsOfAsync(s.StudentProfileId, ct)).Where(r => !r.IsRemoved && CanSee(r, a, v.UserId))
                .OrderByDescending(r => r.OccurredAt).Take(100).ToList();
            var plans = (await _store.PlansOfAsync(s.StudentProfileId, ct)).Where(p => CanSee(p, a, v.UserId)).ToList();
            var names = await _store.NamesAsync(records.Select(r => r.AuthorUserId).Concat(plans.Select(p => p.OwnerUserId)).Distinct().ToList(), ct);
            children.Add(new FamilyChildDto(s.StudentProfileId, s.Name, s.ClassName, records.Select(r => Shared(r, names)).ToList(),
                plans.Select(p => SharedPlan(p, names)).ToList()));
        }
        return PastoralResult<IReadOnlyList<FamilyChildDto>>.Success(children);
    }
}
