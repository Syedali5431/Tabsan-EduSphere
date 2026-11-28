using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Community;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Community;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Community;

/// <summary>School news, events and forms in the database, and the people an audience reaches (see <see cref="CommunityService"/>).</summary>
public sealed class CommunityStore(ApplicationDbContext db) : ICommunityStore
{
    private static readonly string[] StaffRoles = ["Faculty", "Admin", "Finance"];

    // ═════════════════════════════════════════════════════════════════════════
    // People and audiences
    // ═════════════════════════════════════════════════════════════════════════

    private sealed record Place(Guid OfferingId, Guid? TenantId, Guid? CampusId, Guid SemesterId);

    private Task<List<Place>> StudyingAsync(Guid profileId, CancellationToken ct)
        => db.Enrollments.AsNoTracking().Where(e => e.StudentProfileId == profileId && e.Status == EnrollmentStatus.Active)
            .Select(e => new Place(e.CourseOfferingId, e.CourseOffering.TenantId, e.CourseOffering.CampusId, e.CourseOffering.SemesterId)).ToListAsync(ct);

    private static HashSet<Guid> Campuses(IEnumerable<Place> places, Guid? own)
        => places.Where(p => p.CampusId is not null).Select(p => p.CampusId!.Value).Concat(own is { } o ? [o] : []).ToHashSet();

    public async Task<CommunityMember?> MemberAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { Name = u.FullName ?? u.Username, u.TenantId, u.CampusId }).FirstOrDefaultAsync(ct);
        if (user is null) return null;
        var none = new HashSet<Guid>();
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var profile = await db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            var places = profile is { } pid ? await StudyingAsync(pid, ct) : [];
            return new CommunityMember(userId, role, user.Name, user.TenantId ?? places.FirstOrDefault(p => p.TenantId != null)?.TenantId, null,
                Campuses(places, user.CampusId), places.Select(p => p.SemesterId).ToHashSet(), places.Select(p => p.OfferingId).ToHashSet(), [], none);
        }
        if (string.Equals(role, "Parent", StringComparison.OrdinalIgnoreCase))
        {
            var links = await (from l in db.ParentStudentLinks.AsNoTracking()
                               join p in db.StudentProfiles.AsNoTracking() on l.StudentProfileId equals p.Id
                               join u in db.Users.IgnoreQueryFilters().AsNoTracking() on p.UserId equals u.Id
                               where l.ParentUserId == userId && l.IsActive
                               select new { ProfileId = p.Id, u.Id, Name = u.FullName ?? u.Username, u.TenantId, u.CampusId }).ToListAsync(ct);
            var children = new List<CommunityChild>();
            Guid? tenant = user.TenantId;
            foreach (var c in links.DistinctBy(c => c.ProfileId))
            {
                var places = await StudyingAsync(c.ProfileId, ct);
                tenant ??= c.TenantId ?? places.FirstOrDefault(p => p.TenantId != null)?.TenantId;
                children.Add(new CommunityChild(c.ProfileId, c.Id, c.Name, Campuses(places, c.CampusId), places.Select(p => p.SemesterId).ToHashSet(),
                    places.Select(p => p.OfferingId).ToHashSet()));
            }
            return new CommunityMember(userId, role, user.Name, tenant, null, children.SelectMany(c => c.Campuses).ToHashSet(),
                children.SelectMany(c => c.Semesters).ToHashSet(), children.SelectMany(c => c.Offerings).ToHashSet(), children, none);
        }
        var teaching = await db.CourseOfferings.AsNoTracking().Where(o => o.FacultyUserId == userId)
            .Select(o => new Place(o.Id, o.TenantId, o.CampusId, o.SemesterId)).ToListAsync(ct);
        var superAdmin = string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        // SuperAdmins look after every school, whatever school their own account is filed under.
        var tenantId = superAdmin ? null : user.TenantId ?? teaching.FirstOrDefault(p => p.TenantId != null)?.TenantId;
        HashSet<Guid>? adminCampuses = null;
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            // An admin looks after the campuses of their departments (else their own campus); all of them = the whole school.
            var departments = await (from a in db.AdminDepartmentAssignments.AsNoTracking()
                                     join d in db.Departments.AsNoTracking() on a.DepartmentId equals d.Id
                                     where a.AdminUserId == userId && a.RemovedAt == null
                                     select new { d.TenantId, d.CampusId }).ToListAsync(ct);
            tenantId ??= departments.FirstOrDefault(d => d.TenantId != null)?.TenantId;
            var campuses = departments.Where(d => d.CampusId != null).Select(d => d.CampusId!.Value).ToHashSet();
            if (campuses.Count == 0 && user.CampusId is { } own) campuses.Add(own);
            if (campuses.Count > 0 && tenantId is { } t)
            {
                var all = await db.Campuses.AsNoTracking().Where(c => c.TenantId == t && c.IsActive).Select(c => c.Id).ToListAsync(ct);
                adminCampuses = all.All(campuses.Contains) ? null : campuses;
            }
        }
        return new CommunityMember(userId, role, user.Name, tenantId, adminCampuses, Campuses(teaching, user.CampusId), teaching.Select(p => p.SemesterId).ToHashSet(),
            teaching.Select(p => p.OfferingId).ToHashSet(), [], teaching.Select(p => p.OfferingId).ToHashSet());
    }

    public async Task<AudienceOptionsDto> AudienceOptionsAsync(CommunityMember m, CancellationToken ct = default)
    {
        IQueryable<CourseOffering> offerings = db.CourseOfferings.AsNoTracking();
        if (m.IsManager)
        {
            if (m.TenantId is { } tenant) offerings = offerings.Where(o => o.TenantId == tenant);
            if (m.IsAdmin && m.AdminCampuses is { } own) { var ids = own.ToList(); offerings = offerings.Where(o => o.CampusId != null && ids.Contains(o.CampusId.Value)); }
        }
        else offerings = offerings.Where(o => o.FacultyUserId == m.UserId);
        var classes = await offerings.OrderBy(o => o.Course.Code)
            .Select(o => new { o.Id, o.Course.Code, o.Course.Title, o.CampusId, o.SemesterId, Semester = o.Semester.Name }).Take(2000).ToListAsync(ct);

        IQueryable<Domain.Tenancy.Campus> campuses = db.Campuses.AsNoTracking().Where(c => c.IsActive);
        if (m.TenantId is { } t) campuses = campuses.Where(c => c.TenantId == t);
        if (m.IsAdmin && m.AdminCampuses is { } ownCampuses) { var ids = ownCampuses.ToList(); campuses = campuses.Where(c => ids.Contains(c.Id)); }
        if (!m.IsManager)
        {
            var mine = classes.Where(c => c.CampusId != null).Select(c => c.CampusId!.Value).Distinct().ToList();
            campuses = campuses.Where(c => mine.Contains(c.Id));
        }
        var campusList = await campuses.OrderBy(c => c.Name).Select(c => new OptionDto(c.Id, c.Name, null)).ToListAsync(ct);
        var semesters = classes.GroupBy(c => c.SemesterId).Select(g => new OptionDto(g.Key, g.First().Semester)).OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return new AudienceOptionsDto(m.IsManager && !(m.IsAdmin && m.AdminCampuses is not null), campusList, semesters,
            classes.Select(c => new OptionDto(c.Id, $"{c.Code} — {c.Title}", c.CampusId)).ToList());
    }

    public async Task<AudienceNames> AudienceNamesAsync(IEnumerable<CommunityItem> items, CancellationToken ct = default)
    {
        var list = items.ToList();
        var campusIds = list.Where(i => i.CampusId != null).Select(i => i.CampusId!.Value).Distinct().ToList();
        var semesterIds = list.Where(i => i.SemesterId != null).Select(i => i.SemesterId!.Value).Distinct().ToList();
        var classIds = list.Where(i => i.CourseOfferingId != null).Select(i => i.CourseOfferingId!.Value).Distinct().ToList();
        var campuses = campusIds.Count == 0 ? [] : await db.Campuses.AsNoTracking().IgnoreQueryFilters().Where(c => campusIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var semesters = semesterIds.Count == 0 ? [] : await db.Semesters.AsNoTracking().IgnoreQueryFilters().Where(s => semesterIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var classes = classIds.Count == 0 ? [] : await db.CourseOfferings.AsNoTracking().IgnoreQueryFilters().Where(o => classIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Course.Code, o.Course.Title }).ToDictionaryAsync(o => o.Id, o => $"{o.Code} — {o.Title}", ct);
        return new AudienceNames(campuses, semesters, classes);
    }

    public Task<AudienceTarget?> CampusAsync(Guid id, CancellationToken ct = default)
        => db.Campuses.AsNoTracking().Where(c => c.Id == id).Select(c => new AudienceTarget(c.Id, c.TenantId, c.Id, null)).FirstOrDefaultAsync(ct);

    public Task<AudienceTarget?> SemesterAsync(Guid id, CancellationToken ct = default)
        => db.Semesters.AsNoTracking().Where(s => s.Id == id).Select(s => new AudienceTarget(s.Id, null, null, s.Id)).FirstOrDefaultAsync(ct);

    public Task<AudienceTarget?> OfferingAsync(Guid id, CancellationToken ct = default)
        => db.CourseOfferings.AsNoTracking().Where(o => o.Id == id).Select(o => new AudienceTarget(o.Id, o.TenantId, o.CampusId, o.SemesterId)).FirstOrDefaultAsync(ct);

    public async Task<AudiencePeople> PeopleAsync(CommunityItem item, CancellationToken ct = default)
    {
        var people = new AudiencePeople();
        var needStudents = item.Roles.HasFlag(AudienceRoles.Students) || item.Roles.HasFlag(AudienceRoles.Parents);

        if (needStudents)
        {
            var enrolments = db.Enrollments.AsNoTracking().Where(e => e.Status == EnrollmentStatus.Active);
            if (item.CourseOfferingId is { } offering) enrolments = enrolments.Where(e => e.CourseOfferingId == offering);
            if (item.SemesterId is { } semester) enrolments = enrolments.Where(e => e.CourseOffering.SemesterId == semester);
            if (item.CampusId is { } campus) enrolments = enrolments.Where(e => e.CourseOffering.CampusId == campus);
            if (item.TenantId is { } tenant) enrolments = enrolments.Where(e => e.CourseOffering.TenantId == tenant);
            var rows = await (from e in enrolments
                              join p in db.StudentProfiles.AsNoTracking() on e.StudentProfileId equals p.Id
                              join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                              where u.IsActive
                              select new { ProfileId = p.Id, UserId = u.Id, Name = u.FullName ?? u.Username, Class = e.CourseOffering.Semester.Name }).ToListAsync(ct);
            var students = rows.GroupBy(r => r.ProfileId).Select(g => new AudienceStudent(g.Key, g.First().UserId, g.First().Name, g.Select(r => r.Class).Min())).ToList();
            if (item.SemesterId is null && item.CourseOfferingId is null)
            {
                // The whole school or campus: students not (yet) in a class count too.
                var known = students.Select(s => s.StudentProfileId).ToHashSet();
                var loose = from p in db.StudentProfiles.AsNoTracking()
                            join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                            where u.IsActive && (item.TenantId == null || u.TenantId == item.TenantId) && (item.CampusId == null || u.CampusId == item.CampusId)
                            select new { p.Id, UserId = u.Id, Name = u.FullName ?? u.Username };
                students.AddRange((await loose.ToListAsync(ct)).Where(s => !known.Contains(s.Id)).Select(s => new AudienceStudent(s.Id, s.UserId, s.Name, null)));
            }
            people.Students.AddRange(students);

            if (item.Roles.HasFlag(AudienceRoles.Parents) && students.Count > 0)
            {
                var ids = students.Select(s => s.StudentProfileId).ToList();
                people.Parents.AddRange((await (from l in db.ParentStudentLinks.AsNoTracking()
                                                join u in db.Users.AsNoTracking() on l.ParentUserId equals u.Id
                                                where l.IsActive && u.IsActive && ids.Contains(l.StudentProfileId)
                                                select new { u.Id, Name = u.FullName ?? u.Username, l.StudentProfileId }).ToListAsync(ct))
                    .Select(p => (p.Id, p.Name, p.StudentProfileId)));
            }
        }

        if (item.Roles.HasFlag(AudienceRoles.Staff))
        {
            List<(Guid, string, string)> staff;
            if (item.CourseOfferingId is not null || item.SemesterId is not null)
            {
                // A class or year level: its teachers.
                var offerings = db.CourseOfferings.AsNoTracking().Where(o => o.FacultyUserId != null);
                if (item.CourseOfferingId is { } offering) offerings = offerings.Where(o => o.Id == offering);
                if (item.SemesterId is { } semester) offerings = offerings.Where(o => o.SemesterId == semester);
                if (item.CampusId is { } campus) offerings = offerings.Where(o => o.CampusId == campus);
                if (item.TenantId is { } tenant) offerings = offerings.Where(o => o.TenantId == tenant);
                staff = (await (from o in offerings join u in db.Users.AsNoTracking() on o.FacultyUserId equals u.Id
                                where u.IsActive select new { u.Id, Name = u.FullName ?? u.Username, Role = u.Role.Name }).Distinct().ToListAsync(ct))
                    .Select(s => (s.Id, s.Name, s.Role)).ToList();
            }
            else
            {
                staff = (await db.Users.AsNoTracking()
                        .Where(u => u.IsActive && StaffRoles.Contains(u.Role.Name) && (item.TenantId == null || u.TenantId == item.TenantId)
                                    && (item.CampusId == null || u.CampusId == null || u.CampusId == item.CampusId))
                        .Select(u => new { u.Id, Name = u.FullName ?? u.Username, Role = u.Role.Name }).ToListAsync(ct))
                    .Select(s => (s.Id, s.Name, s.Role)).ToList();
            }
            people.Staff.AddRange(staff);
        }
        return people;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
        => userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Username, ct);

    // ═════════════════════════════════════════════════════════════════════════
    // News
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<NewsPost>> NewsAsync(Guid? tenantId, CancellationToken ct = default)
        => await db.NewsPosts.AsNoTracking().Where(p => tenantId == null || p.TenantId == null || p.TenantId == tenantId).ToListAsync(ct);

    public Task<NewsPost?> NewsPostAsync(Guid id, CancellationToken ct = default) => db.NewsPosts.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(NewsPost post) => db.NewsPosts.Add(post);

    public void Remove(NewsPost post) => db.NewsPosts.Remove(post);

    public async Task<IReadOnlySet<Guid>> ReadPostsAsync(Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct = default)
        => (await db.NewsReads.AsNoTracking().Where(r => r.UserId == userId && postIds.Contains(r.PostId)).Select(r => r.PostId).ToListAsync(ct)).ToHashSet();

    public async Task<IReadOnlyDictionary<Guid, int>> SeenCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken ct = default)
        => postIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await db.NewsReads.AsNoTracking().Where(r => postIds.Contains(r.PostId)).GroupBy(r => r.PostId)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, ct);

    public void Add(NewsRead read) => db.NewsReads.Add(read);

    public async Task<IReadOnlyList<NewsPost>> NewsToAnnounceAsync(DateTime now, CancellationToken ct = default)
        => await db.NewsPosts.Where(p => p.Status == NewsStatus.Published && p.NotifiedAt == null && p.PublishAt <= now && (p.ExpiresAt == null || p.ExpiresAt > now))
            .Take(50).ToListAsync(ct);

    // ═════════════════════════════════════════════════════════════════════════
    // Events
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<SchoolEvent>> EventsAsync(Guid? tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await db.SchoolEvents.AsNoTracking()
            .Where(e => (tenantId == null || e.TenantId == null || e.TenantId == tenantId) && e.StartsAt < to && e.EndsAt >= from)
            .OrderBy(e => e.StartsAt).Take(1000).ToListAsync(ct);

    public Task<SchoolEvent?> EventAsync(Guid id, CancellationToken ct = default) => db.SchoolEvents.FirstOrDefaultAsync(e => e.Id == id, ct);

    public void Add(SchoolEvent schoolEvent) => db.SchoolEvents.Add(schoolEvent);

    public Task<Guid?> EventOfFormAsync(Guid formId, CancellationToken ct = default)
        => db.SchoolEvents.AsNoTracking().Where(e => e.FormId == formId && e.Status != EventStatus.Cancelled).OrderByDescending(e => e.StartsAt)
            .Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<EventReply>> RepliesAsync(IReadOnlyCollection<Guid> eventIds, CancellationToken ct = default)
        => eventIds.Count == 0 ? [] : await db.EventReplies.Where(r => eventIds.Contains(r.EventId)).ToListAsync(ct);

    public void Add(EventReply reply) => db.EventReplies.Add(reply);

    // ═════════════════════════════════════════════════════════════════════════
    // Forms
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<SchoolForm>> FormsAsync(Guid? tenantId, CancellationToken ct = default)
        => await db.SchoolForms.Where(f => tenantId == null || f.TenantId == null || f.TenantId == tenantId).ToListAsync(ct);

    public Task<SchoolForm?> FormAsync(Guid id, CancellationToken ct = default) => db.SchoolForms.FirstOrDefaultAsync(f => f.Id == id, ct);

    public void Add(SchoolForm form) => db.SchoolForms.Add(form);

    public void Remove(SchoolForm form) => db.SchoolForms.Remove(form);

    public async Task<IReadOnlyList<FormField>> FieldsAsync(Guid formId, CancellationToken ct = default)
        => await db.FormFields.Where(f => f.FormId == formId).OrderBy(f => f.Position).ToListAsync(ct);

    public void ReplaceFields(IReadOnlyList<FormField> old, IReadOnlyList<FormField> fields)
    {
        db.FormFields.RemoveRange(old);
        db.FormFields.AddRange(fields);
    }

    public async Task<IReadOnlyList<FormResponse>> ResponsesAsync(IReadOnlyCollection<Guid> formIds, CancellationToken ct = default)
        => formIds.Count == 0 ? [] : await db.FormResponses.Where(r => formIds.Contains(r.FormId)).ToListAsync(ct);

    public void Add(FormResponse response) => db.FormResponses.Add(response);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
