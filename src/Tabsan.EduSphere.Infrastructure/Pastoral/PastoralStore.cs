using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Community;
using Tabsan.EduSphere.Application.Pastoral;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Pastoral;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Pastoral;

/// <summary>Pastoral notes, plans, check-ins and the wellbeing team in the database, and the students they are about
/// (see <see cref="PastoralService"/>).</summary>
public sealed class PastoralStore(ApplicationDbContext db, ICommunityStore community) : IPastoralStore
{
    private static readonly string[] StaffRoles = ["Faculty", "Admin"];

    // ═════════════════════════════════════════════════════════════════════════
    // People
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<PastoralViewer?> ViewerAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var member = await community.MemberAsync(userId, role, ct);
        if (member is null) return null;
        Guid? profile = member.IsStudent
            ? await db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct)
            : null;
        var team = member.IsStudent || member.IsParent
            ? []
            : await db.PastoralTeamMembers.AsNoTracking().Where(t => t.UserId == userId)
                .Select(t => new TeamScope(t.Id, t.TenantId, t.CampusId, t.SemesterId, t.SeesConfidential, t.Title)).ToListAsync(ct);
        return new PastoralViewer(member, profile, team);
    }

    // A class with settable members (not a positional record) so queries can keep filtering after the projection.
    private sealed class ProfileRow
    {
        public Guid ProfileId { get; init; }
        public Guid UserId { get; init; }
        public string Name { get; init; } = "";
        public string RegistrationNumber { get; init; } = "";
        public Guid? TenantId { get; init; }
        public Guid? CampusId { get; init; }
    }

    private async Task<IReadOnlyList<PastoralStudent>> BuildAsync(List<ProfileRow> profiles, CancellationToken ct)
    {
        if (profiles.Count == 0) return [];
        var ids = profiles.Select(p => p.ProfileId).ToList();
        var places = await db.Enrollments.AsNoTracking().Where(e => ids.Contains(e.StudentProfileId) && e.Status == EnrollmentStatus.Active)
            .Select(e => new
            {
                e.StudentProfileId, e.CourseOfferingId, e.CourseOffering.TenantId, e.CourseOffering.CampusId, e.CourseOffering.SemesterId,
                Semester = e.CourseOffering.Semester.Name, e.CourseOffering.FacultyUserId
            }).ToListAsync(ct);
        var parents = await db.ParentStudentLinks.AsNoTracking().Where(l => l.IsActive && ids.Contains(l.StudentProfileId))
            .Join(db.Users.AsNoTracking().Where(u => u.IsActive), l => l.ParentUserId, u => u.Id, (l, u) => new { l.StudentProfileId, u.Id }).ToListAsync(ct);
        return profiles.Select(p =>
        {
            var mine = places.Where(x => x.StudentProfileId == p.ProfileId).ToList();
            var campuses = mine.Where(x => x.CampusId != null).Select(x => x.CampusId!.Value).ToHashSet();
            if (p.CampusId is { } own) campuses.Add(own);
            return new PastoralStudent(p.ProfileId, p.UserId, p.Name, p.RegistrationNumber, p.TenantId ?? mine.FirstOrDefault(x => x.TenantId != null)?.TenantId,
                p.CampusId ?? mine.FirstOrDefault(x => x.CampusId != null)?.CampusId, campuses, mine.Select(x => x.SemesterId).ToHashSet(),
                mine.Select(x => x.CourseOfferingId).ToHashSet(), mine.Select(x => x.Semester).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).FirstOrDefault(),
                mine.Where(x => x.FacultyUserId != null).Select(x => x.FacultyUserId!.Value).ToHashSet(),
                parents.Where(x => x.StudentProfileId == p.ProfileId).Select(x => x.Id).Distinct().ToList());
        }).ToList();
    }

    private IQueryable<ProfileRow> Profiles()
        => from p in db.StudentProfiles.AsNoTracking()
           join u in db.Users.AsNoTracking() on p.UserId equals u.Id
           select new ProfileRow { ProfileId = p.Id, UserId = u.Id, Name = u.FullName ?? u.Username, RegistrationNumber = p.RegistrationNumber, TenantId = u.TenantId, CampusId = u.CampusId };

    public async Task<IReadOnlyList<PastoralStudent>> StudentsAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default)
    {
        if (studentProfileIds.Count == 0) return [];
        var ids = studentProfileIds.Distinct().ToList();
        var profiles = new List<ProfileRow>();
        foreach (var chunk in ids.Chunk(500))
            profiles.AddRange(await Profiles().Where(p => chunk.Contains(p.ProfileId)).ToListAsync(ct));
        return await BuildAsync(profiles, ct);
    }

    public async Task<IReadOnlyList<PastoralStudent>> FindStudentsAsync(Guid? tenantId, IReadOnlyCollection<Guid>? offerings, string? search, int take,
        CancellationToken ct = default)
    {
        IQueryable<ProfileRow> q = Profiles();
        if (tenantId is { } t)
        {
            // Students filed under the school, or studying in one of its classes.
            var inSchool = db.Enrollments.Where(e => e.Status == EnrollmentStatus.Active && e.CourseOffering.TenantId == t).Select(e => e.StudentProfileId);
            q = q.Where(p => p.TenantId == t || (p.TenantId == null && inSchool.Contains(p.ProfileId)));
        }
        if (offerings is not null)
        {
            var ids = offerings.ToList();
            var enrolled = db.Enrollments.Where(e => e.Status == EnrollmentStatus.Active && ids.Contains(e.CourseOfferingId)).Select(e => e.StudentProfileId);
            q = q.Where(p => enrolled.Contains(p.ProfileId));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.RegistrationNumber.Contains(s));
        }
        return await BuildAsync(await q.OrderBy(p => p.Name).Take(take).ToListAsync(ct), ct);
    }

    public async Task<FilterOptionsDto> FilterOptionsAsync(Guid? tenantId, IReadOnlySet<Guid>? campuses, IReadOnlySet<Guid>? offerings, CancellationToken ct = default)
    {
        IQueryable<CourseOffering> classes = db.CourseOfferings.AsNoTracking();
        if (tenantId is { } t) classes = classes.Where(o => o.TenantId == t);
        if (campuses is not null) { var ids = campuses.ToList(); classes = classes.Where(o => o.CampusId != null && ids.Contains(o.CampusId.Value)); }
        if (offerings is not null) { var ids = offerings.ToList(); classes = classes.Where(o => ids.Contains(o.Id)); }
        var list = await classes.OrderBy(o => o.Course.Code)
            .Select(o => new { o.Id, o.Course.Code, o.Course.Title, o.CampusId, o.SemesterId, Semester = o.Semester.Name }).Take(3000).ToListAsync(ct);

        IQueryable<Domain.Tenancy.Campus> campusQuery = db.Campuses.AsNoTracking().Where(c => c.IsActive);
        if (tenantId is { } tenant) campusQuery = campusQuery.Where(c => c.TenantId == tenant);
        if (campuses is not null) { var ids = campuses.ToList(); campusQuery = campusQuery.Where(c => ids.Contains(c.Id)); }
        if (offerings is not null) { var ids = list.Where(c => c.CampusId != null).Select(c => c.CampusId!.Value).Distinct().ToList(); campusQuery = campusQuery.Where(c => ids.Contains(c.Id)); }
        var campusList = await campusQuery.OrderBy(c => c.Name).Select(c => new OptionDto(c.Id, c.Name, null)).ToListAsync(ct);
        var semesters = list.GroupBy(c => c.SemesterId).Select(g => new OptionDto(g.Key, g.First().Semester))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return new FilterOptionsDto(campusList, semesters, list.Select(c => new OptionDto(c.Id, $"{c.Code} — {c.Title}", c.CampusId)).ToList());
    }

    public async Task<IReadOnlyList<OptionDto>> SchoolsAsync(CancellationToken ct = default)
        => await db.Tenants.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Name).Select(t => new OptionDto(t.Id, t.Name, null)).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
        => userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Username, ct);

    public async Task<IReadOnlyDictionary<Guid, string>> PlaceNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        var names = new Dictionary<Guid, string>();
        foreach (var c in await db.Campuses.AsNoTracking().IgnoreQueryFilters().Where(c => ids.Contains(c.Id)).Select(c => new { c.Id, c.Name }).ToListAsync(ct))
            names[c.Id] = c.Name;
        foreach (var s in await db.Semesters.AsNoTracking().IgnoreQueryFilters().Where(s => ids.Contains(s.Id)).Select(s => new { s.Id, s.Name }).ToListAsync(ct))
            names[s.Id] = s.Name;
        foreach (var o in await db.CourseOfferings.AsNoTracking().IgnoreQueryFilters().Where(o => ids.Contains(o.Id))
                     .Select(o => new { o.Id, o.Course.Code, o.Course.Title }).ToListAsync(ct))
            names[o.Id] = $"{o.Code} — {o.Title}";
        return names;
    }

    public async Task<IReadOnlyList<Guid>> AdminsAsync(Guid? tenantId, IReadOnlySet<Guid> campuses, CancellationToken ct = default)
    {
        var ids = campuses.ToList();
        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role.Name == "Admin" && (tenantId == null || u.TenantId == tenantId)
                        && (ids.Count == 0 || u.CampusId == null || ids.Contains(u.CampusId.Value)))
            .Select(u => u.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PersonOptionDto>> StaffAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Users.AsNoTracking().Where(u => u.IsActive && u.TenantId == tenantId && StaffRoles.Contains(u.Role.Name))
            .OrderBy(u => u.FullName ?? u.Username).Take(2000)
            .Select(u => new PersonOptionDto(u.Id, u.FullName ?? u.Username, u.Role.Name == "Faculty" ? "Teacher" : u.Role.Name, false)).ToListAsync(ct);

    // ═════════════════════════════════════════════════════════════════════════
    // Notes
    // ═════════════════════════════════════════════════════════════════════════

    private IQueryable<PastoralRecord> InSchool(Guid? tenantId) => db.PastoralRecords.Where(r => tenantId == null || r.TenantId == tenantId);

    public async Task<IReadOnlyList<PastoralRecord>> RecordsOfAsync(Guid studentProfileId, CancellationToken ct = default)
        => await db.PastoralRecords.AsNoTracking().Where(r => r.StudentProfileId == studentProfileId).OrderByDescending(r => r.OccurredAt).Take(1000).ToListAsync(ct);

    public async Task<IReadOnlyList<PastoralRecord>> RecordsAsync(Guid? tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await InSchool(tenantId).AsNoTracking().Where(r => r.RemovedAt == null && r.OccurredAt >= from && r.OccurredAt < to).Take(20_000).ToListAsync(ct);

    public async Task<IReadOnlyList<PastoralRecord>> OpenFollowUpsAsync(Guid? tenantId, CancellationToken ct = default)
        => await InSchool(tenantId).AsNoTracking().Where(r => r.RemovedAt == null && r.FollowUp == FollowUpStatus.Open).Take(5000).ToListAsync(ct);

    public Task<PastoralRecord?> RecordAsync(Guid id, CancellationToken ct = default) => db.PastoralRecords.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<int> ConcernCountAsync(Guid studentProfileId, DateTime since, CancellationToken ct = default)
        => db.PastoralRecords.CountAsync(r => r.StudentProfileId == studentProfileId && r.RemovedAt == null && r.Tone == PastoralTone.Concern
                                              && (r.Kind == PastoralKind.Behaviour || r.Kind == PastoralKind.Wellbeing) && r.OccurredAt >= since, ct);

    public void Add(PastoralRecord record) => db.PastoralRecords.Add(record);

    public async Task<IReadOnlyList<PastoralNote>> NotesAsync(IReadOnlyCollection<Guid> recordIds, CancellationToken ct = default)
    {
        if (recordIds.Count == 0) return [];
        var ids = recordIds.ToList();
        var notes = new List<PastoralNote>();
        foreach (var chunk in ids.Chunk(500))
            notes.AddRange(await db.PastoralNotes.AsNoTracking().Where(n => chunk.Contains(n.RecordId)).ToListAsync(ct));
        return notes;
    }

    public void Add(PastoralNote note) => db.PastoralNotes.Add(note);

    public async Task<IReadOnlyList<PastoralRecord>> FollowUpsDueAsync(DateTime now, CancellationToken ct = default)
        => await db.PastoralRecords.Where(r => r.RemovedAt == null && r.FollowUp == FollowUpStatus.Open && r.FollowUpBy != null && r.FollowUpBy <= now
                                               && r.FollowUpRemindedAt == null).Take(100).ToListAsync(ct);

    // ═════════════════════════════════════════════════════════════════════════
    // Plans
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<SupportPlan>> PlansOfAsync(Guid studentProfileId, CancellationToken ct = default)
        => await db.SupportPlans.AsNoTracking().Where(p => p.StudentProfileId == studentProfileId).ToListAsync(ct);

    public async Task<IReadOnlyList<SupportPlan>> ActivePlansAsync(Guid? tenantId, CancellationToken ct = default)
        => await db.SupportPlans.AsNoTracking().Where(p => p.Status == SupportPlanStatus.Active && (tenantId == null || p.TenantId == tenantId)).Take(5000).ToListAsync(ct);

    public Task<SupportPlan?> PlanAsync(Guid id, CancellationToken ct = default) => db.SupportPlans.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(SupportPlan plan) => db.SupportPlans.Add(plan);

    public async Task<IReadOnlyList<SupportPlan>> PlansToReviewAsync(DateTime now, CancellationToken ct = default)
        => await db.SupportPlans.Where(p => p.Status == SupportPlanStatus.Active && p.ReviewOn != null && p.ReviewOn <= now && p.ReviewRemindedAt == null)
            .Take(100).ToListAsync(ct);

    // ═════════════════════════════════════════════════════════════════════════
    // Check-ins
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<WellbeingCheckIn>> CheckInsOfAsync(Guid studentProfileId, int take, CancellationToken ct = default)
        => await db.WellbeingCheckIns.Where(c => c.StudentProfileId == studentProfileId).OrderByDescending(c => c.CheckedInAt).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<WellbeingCheckIn>> CheckInsAsync(Guid? tenantId, DateTime since, CancellationToken ct = default)
        => await db.WellbeingCheckIns.AsNoTracking().Where(c => (tenantId == null || c.TenantId == tenantId) && c.CheckedInAt >= since).Take(20_000).ToListAsync(ct);

    public async Task<IReadOnlyList<WellbeingCheckIn>> CheckInsNeedingAttentionAsync(Guid? tenantId, CancellationToken ct = default)
        => await db.WellbeingCheckIns.AsNoTracking()
            .Where(c => (tenantId == null || c.TenantId == tenantId) && c.AcknowledgedAt == null && (c.WantsToTalk || c.Mood <= 2))
            .OrderByDescending(c => c.CheckedInAt).Take(500).ToListAsync(ct);

    public Task<WellbeingCheckIn?> CheckInAsync(Guid id, CancellationToken ct = default) => db.WellbeingCheckIns.FirstOrDefaultAsync(c => c.Id == id, ct);

    public void Add(WellbeingCheckIn checkIn) => db.WellbeingCheckIns.Add(checkIn);

    // ═════════════════════════════════════════════════════════════════════════
    // Team, categories and settings
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<TeamMemberRow>> TeamAsync(Guid? tenantId, CancellationToken ct = default)
        => (await (from t in db.PastoralTeamMembers.AsNoTracking()
                   join u in db.Users.AsNoTracking() on t.UserId equals u.Id
                   where (tenantId == null || t.TenantId == tenantId) && u.IsActive
                   select new { t, Name = u.FullName ?? u.Username }).ToListAsync(ct))
            .Select(x => new TeamMemberRow(x.t, x.Name)).ToList();

    public Task<PastoralTeamMember?> TeamMemberAsync(Guid id, CancellationToken ct = default) => db.PastoralTeamMembers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public void Add(PastoralTeamMember member) => db.PastoralTeamMembers.Add(member);

    public void Remove(PastoralTeamMember member) => db.PastoralTeamMembers.Remove(member);

    public async Task<IReadOnlyList<PastoralCategory>> CategoriesAsync(Guid? tenantId, CancellationToken ct = default)
        => await db.PastoralCategories.Where(c => c.TenantId == tenantId).OrderBy(c => c.Position).ToListAsync(ct);

    public Task<PastoralCategory?> CategoryAsync(Guid id, CancellationToken ct = default) => db.PastoralCategories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public void Add(PastoralCategory category) => db.PastoralCategories.Add(category);

    public Task<PastoralSettings?> SettingsAsync(Guid tenantId, CancellationToken ct = default)
        => db.PastoralSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    public void Add(PastoralSettings settings) => db.PastoralSettings.Add(settings);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
