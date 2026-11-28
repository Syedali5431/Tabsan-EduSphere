using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Messaging;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Messaging;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Messaging;

/// <summary>
/// Messaging from the database: who each role may write to (from enrolments, teaching, parent links and department
/// assignments), inboxes with unread counts, conversations and their messages.
/// </summary>
public sealed class MessagingQueries : IMessagingQueries
{
    private const string Teachers = "Teachers", Office = "Office", Students = "Students", Parents = "Parents", StaffGroup = "Staff";
    private readonly ApplicationDbContext _db;

    public MessagingQueries(ApplicationDbContext db) => _db = db;

    private static bool Is(string role, string name) => string.Equals(role, name, StringComparison.OrdinalIgnoreCase);

    // ═════════════════════════════════════════════════════════════════════════
    // Who may write to whom
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<MessageContactDto>> ContactsAsync(Guid userId, string role, string? search, int limit, CancellationToken ct = default)
    {
        var found = new List<MessageContactDto>();
        if (Is(role, "SuperAdmin"))
        {
            var people = _db.Users.AsNoTracking().Where(u => u.IsActive && u.Id != userId);
            if (search is not null) people = people.Where(u => (u.FullName ?? "").Contains(search) || u.Username.Contains(search));
            found.AddRange(await people.OrderBy(u => u.FullName ?? u.Username).Take(limit)
                .Select(u => new MessageContactDto(u.Id, u.FullName ?? u.Username, u.Role.Name, u.Username, u.Role.Name)).ToListAsync(ct));
            return found;
        }

        if (Is(role, "Student"))
        {
            var profile = await _db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => new { p.Id, p.DepartmentId }).FirstOrDefaultAsync(ct);
            if (profile is not null)
            {
                found.AddRange(await TeachersOfAsync([profile.Id], null, ct));
                found.AddRange(await AdminsOfAsync([profile.DepartmentId], Office, ct));
            }
        }
        else if (Is(role, "Parent"))
        {
            var children = await _db.ParentStudentLinks.AsNoTracking().Where(l => l.ParentUserId == userId && l.IsActive)
                .Join(_db.StudentProfiles, l => l.StudentProfileId, p => p.Id, (l, p) => new { p.Id, p.DepartmentId, p.UserId })
                .Join(_db.Users, p => p.UserId, u => u.Id, (p, u) => new { p.Id, p.DepartmentId, Name = u.FullName ?? u.Username })
                .ToListAsync(ct);
            var names = children.ToDictionary(c => c.Id, c => c.Name);
            found.AddRange(await TeachersOfAsync(children.Select(c => c.Id).ToList(), names, ct));
            var departments = children.Select(c => c.DepartmentId).Distinct().ToList();
            found.AddRange(await AdminsOfAsync(departments, Office, ct));
            found.AddRange(await FinanceOfAsync(departments, ct));
        }
        else if (Is(role, "Faculty"))
        {
            var teaching = await _db.CourseOfferings.AsNoTracking().Where(o => o.FacultyUserId == userId)
                .Select(o => new { o.Id, o.Course.Title, o.Course.DepartmentId }).ToListAsync(ct);
            var titles = teaching.ToDictionary(o => o.Id, o => o.Title);
            var ids = teaching.Select(o => o.Id).ToList();
            var students = await (from e in _db.Enrollments.AsNoTracking()
                                  join p in _db.StudentProfiles on e.StudentProfileId equals p.Id
                                  join u in _db.Users on p.UserId equals u.Id
                                  where ids.Contains(e.CourseOfferingId) && e.Status == EnrollmentStatus.Active && u.IsActive
                                  select new { e.CourseOfferingId, ProfileId = p.Id, u.Id, Name = u.FullName ?? u.Username, p.RegistrationNumber }).ToListAsync(ct);
            found.AddRange(students.Select(s => new MessageContactDto(s.Id, s.Name, "Student", s.RegistrationNumber, titles.GetValueOrDefault(s.CourseOfferingId))));
            var profileIds = students.Select(s => s.ProfileId).Distinct().ToList();
            var childNames = students.DistinctBy(s => s.ProfileId).ToDictionary(s => s.ProfileId, s => s.Name);
            var courseOf = students.GroupBy(s => s.ProfileId).ToDictionary(g => g.Key, g => titles.GetValueOrDefault(g.First().CourseOfferingId));
            found.AddRange((await ParentsOfAsync(profileIds, ct)).Select(p => new MessageContactDto(p.UserId, p.Name, "Parent", childNames.GetValueOrDefault(p.ChildId), courseOf.GetValueOrDefault(p.ChildId))));
            var departments = (await _db.FacultyDepartmentAssignments.AsNoTracking().Where(a => a.FacultyUserId == userId && a.RemovedAt == null)
                    .Select(a => a.DepartmentId).ToListAsync(ct))
                .Union(teaching.Select(o => o.DepartmentId)).Distinct().ToList();
            found.AddRange(await TeachersInAsync(departments, ct));
            found.AddRange(await AdminsOfAsync(departments, StaffGroup, ct));
        }
        else if (Is(role, "Admin") || Is(role, "Finance"))
        {
            List<Guid> departments;
            if (Is(role, "Admin"))
                departments = await _db.AdminDepartmentAssignments.AsNoTracking().Where(a => a.AdminUserId == userId && a.RemovedAt == null)
                    .Select(a => a.DepartmentId).Distinct().ToListAsync(ct);
            else
            {
                var me = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new { u.CampusId, u.DepartmentId }).FirstOrDefaultAsync(ct);
                departments = me?.CampusId is { } campus
                    ? await _db.Departments.AsNoTracking().Where(d => d.CampusId == campus).Select(d => d.Id).ToListAsync(ct)
                    : me?.DepartmentId is { } department ? [department] : [];
            }
            var students = await (from p in _db.StudentProfiles.AsNoTracking()
                                  join u in _db.Users on p.UserId equals u.Id
                                  where departments.Contains(p.DepartmentId) && u.IsActive
                                  select new { ProfileId = p.Id, u.Id, Name = u.FullName ?? u.Username, p.RegistrationNumber }).ToListAsync(ct);
            found.AddRange(students.Select(s => new MessageContactDto(s.Id, s.Name, "Student", s.RegistrationNumber, Students)));
            var childNames = students.DistinctBy(s => s.ProfileId).ToDictionary(s => s.ProfileId, s => s.Name);
            found.AddRange((await ParentsOfAsync(childNames.Keys.ToList(), ct)).Select(p => new MessageContactDto(p.UserId, p.Name, "Parent", childNames.GetValueOrDefault(p.ChildId), Parents)));
            if (Is(role, "Admin")) found.AddRange(await TeachersInAsync(departments, ct));
            found.AddRange(await AdminsOfAsync(departments, StaffGroup, ct));
            found.AddRange(await FinanceOfAsync(departments, ct));
        }

        // Each person once (the first group they appear in), never the user themself.
        IEnumerable<MessageContactDto> result = found.Where(c => c.UserId != userId).GroupBy(c => c.UserId).Select(g => g.First());
        if (search is not null)
            result = result.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || (c.Detail?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                                       || (c.Group?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        return result.OrderBy(c => GroupOrder(c.Group)).ThenBy(c => c.Group, StringComparer.CurrentCulture).ThenBy(c => c.Name, StringComparer.CurrentCulture)
            .Take(limit).ToList();
    }

    /// <summary>Courses first (a teacher's classes), then teachers, the office, students, parents and staff.</summary>
    private static int GroupOrder(string? group) => group switch
    {
        Teachers => 1,
        Office => 2,
        Students => 3,
        Parents => 4,
        StaffGroup => 5,
        _ => 0
    };

    /// <summary>The teachers of these students' courses (detail: the course, or the child for parents).</summary>
    private async Task<IEnumerable<MessageContactDto>> TeachersOfAsync(List<Guid> profileIds, Dictionary<Guid, string>? childNames, CancellationToken ct)
    {
        if (profileIds.Count == 0) return [];
        var rows = await (from e in _db.Enrollments.AsNoTracking()
                          join o in _db.CourseOfferings on e.CourseOfferingId equals o.Id
                          join u in _db.Users on o.FacultyUserId equals (Guid?)u.Id
                          where profileIds.Contains(e.StudentProfileId) && e.Status == EnrollmentStatus.Active && u.IsActive
                          select new { u.Id, Name = u.FullName ?? u.Username, o.Course.Title, e.StudentProfileId }).ToListAsync(ct);
        return rows.GroupBy(r => r.Id).Select(g =>
        {
            var courses = string.Join(", ", g.Select(r => r.Title).Distinct().OrderBy(t => t, StringComparer.CurrentCulture));
            var children = childNames is null ? null : string.Join(", ", g.Select(r => childNames.GetValueOrDefault(r.StudentProfileId)).Where(n => n is not null).Distinct());
            return new MessageContactDto(g.Key, g.First().Name, "Faculty", string.IsNullOrEmpty(children) ? courses : $"{courses} · {children}", Teachers);
        }).ToList();
    }

    private async Task<IEnumerable<MessageContactDto>> TeachersInAsync(List<Guid> departmentIds, CancellationToken ct)
    {
        if (departmentIds.Count == 0) return [];
        var assigned = _db.FacultyDepartmentAssignments.Where(a => departmentIds.Contains(a.DepartmentId) && a.RemovedAt == null).Select(a => a.FacultyUserId);
        var teaching = _db.CourseOfferings.Where(o => o.FacultyUserId != null && departmentIds.Contains(o.Course.DepartmentId)).Select(o => o.FacultyUserId!.Value);
        var ids = assigned.Union(teaching);
        return await _db.Users.AsNoTracking().Where(u => ids.Contains(u.Id) && u.IsActive && u.Role.Name == "Faculty")
            .Select(u => new MessageContactDto(u.Id, u.FullName ?? u.Username, "Faculty", null, StaffGroup)).ToListAsync(ct);
    }

    private async Task<IEnumerable<MessageContactDto>> AdminsOfAsync(List<Guid> departmentIds, string group, CancellationToken ct)
    {
        if (departmentIds.Count == 0) return [];
        var ids = _db.AdminDepartmentAssignments.Where(a => departmentIds.Contains(a.DepartmentId) && a.RemovedAt == null).Select(a => a.AdminUserId);
        return await _db.Users.AsNoTracking().Where(u => ids.Contains(u.Id) && u.IsActive && u.Role.Name == "Admin")
            .Select(u => new MessageContactDto(u.Id, u.FullName ?? u.Username, "Admin", null, group)).ToListAsync(ct);
    }

    /// <summary>Finance officers of these departments or of their campuses.</summary>
    private async Task<IEnumerable<MessageContactDto>> FinanceOfAsync(List<Guid> departmentIds, CancellationToken ct)
    {
        if (departmentIds.Count == 0) return [];
        var campuses = _db.Departments.Where(d => departmentIds.Contains(d.Id) && d.CampusId != null).Select(d => d.CampusId);
        return await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role.Name == "Finance" && ((u.DepartmentId != null && departmentIds.Contains(u.DepartmentId.Value)) || campuses.Contains(u.CampusId)))
            .Select(u => new MessageContactDto(u.Id, u.FullName ?? u.Username, "Finance", null, Office)).ToListAsync(ct);
    }

    private async Task<List<(Guid UserId, string Name, Guid ChildId)>> ParentsOfAsync(List<Guid> profileIds, CancellationToken ct)
    {
        if (profileIds.Count == 0) return [];
        return (await _db.ParentStudentLinks.AsNoTracking().Where(l => l.IsActive && profileIds.Contains(l.StudentProfileId))
                .Join(_db.Users.Where(u => u.IsActive), l => l.ParentUserId, u => u.Id, (l, u) => new { u.Id, Name = u.FullName ?? u.Username, l.StudentProfileId })
                .ToListAsync(ct))
            .Select(p => (p.Id, p.Name, p.StudentProfileId)).ToList();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // People, inboxes, conversations
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyDictionary<Guid, ConversationPersonDto>> PeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, ConversationPersonDto>();
        var ids = userIds.Distinct().ToList();
        return await _db.Users.AsNoTracking().Where(u => ids.Contains(u.Id))
            .Select(u => new ConversationPersonDto(u.Id, u.FullName ?? u.Username, u.Role.Name))
            .ToDictionaryAsync(p => p.UserId, ct);
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> InboxAsync(Guid userId, bool archived, int skip, int take, CancellationToken ct = default)
    {
        var rows = await (from p in _db.ConversationParticipants.AsNoTracking()
                          join c in _db.Conversations on p.ConversationId equals c.Id
                          where p.UserId == userId && (archived ? p.ArchivedAt != null : p.ArchivedAt == null)
                          orderby c.LastMessageAt descending
                          select new
                          {
                              c.Id, c.Subject, c.LastMessageAt, Archived = p.ArchivedAt != null,
                              Unread = _db.ConversationMessages.Count(m => m.ConversationId == c.Id && m.SenderUserId != userId && (p.LastReadAt == null || m.CreatedAt > p.LastReadAt)),
                              Last = _db.ConversationMessages.Where(m => m.ConversationId == c.Id).OrderByDescending(m => m.CreatedAt)
                                  .Select(m => new { m.Body, m.SenderUserId }).FirstOrDefault()
                          }).Skip(skip).Take(take).ToListAsync(ct);
        if (rows.Count == 0) return [];
        var ids = rows.Select(r => r.Id).ToList();
        var others = await _db.ConversationParticipants.AsNoTracking().Where(p => ids.Contains(p.ConversationId) && p.UserId != userId)
            .Join(_db.Users, p => p.UserId, u => u.Id, (p, u) => new { p.ConversationId, Person = new ConversationPersonDto(u.Id, u.FullName ?? u.Username, u.Role.Name) })
            .ToListAsync(ct);
        var senders = await PeopleAsync(rows.Where(r => r.Last is not null).Select(r => r.Last!.SenderUserId).Distinct().ToList(), ct);
        return rows.Select(r => new ConversationSummaryDto(r.Id, r.Subject,
                others.Where(o => o.ConversationId == r.Id).Select(o => o.Person).OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList(),
                r.LastMessageAt, r.Last is null ? null : Preview(r.Last.Body), r.Last?.SenderUserId,
                r.Last is null ? null : senders.GetValueOrDefault(r.Last.SenderUserId)?.Name, r.Unread, r.Archived, r.Last?.SenderUserId == userId))
            .ToList();
    }

    private static string Preview(string body)
    {
        var text = body.ReplaceLineEndings(" ").Trim();
        return text.Length > 140 ? text[..139].TrimEnd() + "…" : text;
    }

    public Task<int> UnreadConversationsAsync(Guid userId, CancellationToken ct = default)
        => _db.ConversationParticipants.AsNoTracking()
            .Where(p => p.UserId == userId)
            .CountAsync(p => _db.ConversationMessages.Any(m => m.ConversationId == p.ConversationId && m.SenderUserId != userId
                                                                && (p.LastReadAt == null || m.CreatedAt > p.LastReadAt)), ct);

    public Task<Conversation?> ConversationAsync(Guid conversationId, CancellationToken ct = default)
        => _db.Conversations.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == conversationId, ct);

    public async Task<IReadOnlyList<ConversationMessage>> MessagesAsync(Guid conversationId, int take, CancellationToken ct = default)
        => (await _db.ConversationMessages.AsNoTracking().Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt).Take(take).ToListAsync(ct))
            .OrderBy(m => m.CreatedAt).ToList();

    public void Add(Conversation conversation) => _db.Conversations.Add(conversation);

    public void Add(ConversationMessage message) => _db.ConversationMessages.Add(message);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
