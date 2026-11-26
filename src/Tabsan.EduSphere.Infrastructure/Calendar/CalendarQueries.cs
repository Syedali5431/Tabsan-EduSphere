using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Calendar;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Calendar;
using Tabsan.EduSphere.Domain.LiveClasses;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Calendar;

/// <summary>
/// The calendar's entries from the database: homework (with "Assign to" dates of the student or their group), quizzes (with a
/// student's own window), live classes and key dates; the people due a reminder; calendar links and sent reminders.
/// Filters run before projecting (EF cannot translate filters on projected records).
/// </summary>
public sealed class CalendarQueries : ICalendarQueries
{
    /// <summary>Dates further than this from the range are not looked at (an "Assign to" date can move a due date).</summary>
    private static readonly TimeSpan OverrideReach = TimeSpan.FromDays(366);

    private readonly ApplicationDbContext _db;

    public CalendarQueries(ApplicationDbContext db) => _db = db;

    private static bool Is(string role, string name) => string.Equals(role, name, StringComparison.OrdinalIgnoreCase);

    // ═════════════════════════════════════════════════════════════════════════
    // Entries
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<CalendarEventDto>?> EventsAsync(Guid userId, string role, Guid? childId, DateTime from, DateTime to,
        CalendarSources sources, DateTime now, CancellationToken ct = default)
    {
        var events = new List<CalendarEventDto>();
        if (Is(role, "Student") || Is(role, "Parent"))
        {
            var students = await StudentsInViewAsync(userId, role, childId, ct);
            if (students is null) return null;
            var isParent = Is(role, "Parent");
            var semesters = new HashSet<Guid>();
            foreach (var (profileId, name) in students)
            {
                var offerings = await _db.Enrollments.AsNoTracking()
                    .Where(e => e.StudentProfileId == profileId && e.Status == EnrollmentStatus.Active)
                    .Select(e => new { e.CourseOfferingId, e.CourseOffering.SemesterId }).Distinct().ToListAsync(ct);
                semesters.UnionWith(offerings.Select(o => o.SemesterId));
                var ids = offerings.Select(o => o.CourseOfferingId).Distinct().ToList();
                if (ids.Count == 0) continue;
                // Parents' entries carry the child (their feed has every child; UIDs stay the same when one is chosen).
                var owner = isParent ? (profileId, name) : ((Guid, string)?)null;
                if (sources.Assignments) events.AddRange(await StudentAssignmentsAsync(profileId, ids, from, to, now, owner, ct));
                if (sources.Quizzes) events.AddRange(await StudentQuizzesAsync(profileId, ids, from, to, now, owner, ct));
                if (sources.LiveClasses) events.AddRange(await LiveClassesAsync(ids, from, to, owner, ct));
            }
            events.AddRange(await KeyDatesAsync(semesters, from, to, ct));
            return events;
        }

        var teaching = await _db.CourseOfferings.AsNoTracking().Where(o => o.FacultyUserId == userId)
            .Select(o => new { o.Id, o.SemesterId }).ToListAsync(ct);
        var teachingIds = teaching.Select(o => o.Id).ToList();
        if (teachingIds.Count > 0)
        {
            if (sources.Assignments) events.AddRange(await TeacherAssignmentsAsync(teachingIds, from, to, ct));
            if (sources.Quizzes) events.AddRange(await TeacherQuizzesAsync(teachingIds, from, to, ct));
            if (sources.LiveClasses) events.AddRange(await LiveClassesAsync(teachingIds, from, to, null, ct));
        }
        // Teachers see their semesters' key dates; admins and other staff the institution's.
        events.AddRange(await KeyDatesAsync(Is(role, "Faculty") ? teaching.Select(o => o.SemesterId).ToHashSet() : null, from, to, ct));
        return events;
    }

    /// <summary>The student themself, or a parent's actively linked children (one when asked for); null when the child is not theirs.</summary>
    private async Task<List<(Guid ProfileId, string Name)>?> StudentsInViewAsync(Guid userId, string role, Guid? childId, CancellationToken ct)
    {
        if (Is(role, "Student"))
        {
            var own = await _db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            if (childId is not null && childId != own) return null;
            if (own is null) return [];
            return [(own.Value, "")];
        }
        var children = await _db.ParentStudentLinks.AsNoTracking()
            .Where(l => l.ParentUserId == userId && l.IsActive && (childId == null || l.StudentProfileId == childId))
            .Join(_db.StudentProfiles, l => l.StudentProfileId, p => p.Id, (l, p) => new { p.Id, p.UserId, p.RegistrationNumber })
            .Join(_db.Users, p => p.UserId, u => u.Id, (p, u) => new { p.Id, Name = u.FullName ?? u.Username })
            .ToListAsync(ct);
        if (childId is not null && children.Count == 0) return null;
        return children.DistinctBy(c => c.Id).Select(c => (c.Id, c.Name)).ToList();
    }

    private async Task<Dictionary<Guid, (string Code, string Title)>> CoursesAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct)
        => (await _db.CourseOfferings.AsNoTracking().Where(o => offeringIds.Contains(o.Id))
                .Select(o => new { o.Id, o.Course.Code, o.Course.Title }).ToListAsync(ct))
            .ToDictionary(o => o.Id, o => (o.Code, o.Title));

    private static string EventId(string kind, Guid id, (Guid ProfileId, string Name)? owner)
        => owner is { } o ? $"{kind.ToLowerInvariant()}:{id}:{o.ProfileId}" : $"{kind.ToLowerInvariant()}:{id}";

    private async Task<IEnumerable<CalendarEventDto>> StudentAssignmentsAsync(Guid profileId, List<Guid> offeringIds, DateTime from, DateTime to, DateTime now,
        (Guid ProfileId, string Name)? owner, CancellationToken ct)
    {
        var lower = from - OverrideReach;
        var upper = to + OverrideReach;
        var rows = await _db.Assignments.AsNoTracking()
            .Where(a => offeringIds.Contains(a.CourseOfferingId) && a.IsPublished && a.DueDate >= lower && a.DueDate <= upper)
            .Select(a => new
            {
                a.Id, a.CourseOfferingId, a.Title, a.DueDate,
                Done = _db.AssignmentSubmissions.Any(s => s.AssignmentId == a.Id && s.StudentProfileId == profileId && !s.IsMissing)
            })
            .ToListAsync(ct);
        if (rows.Count == 0) return [];
        var ids = rows.Select(r => r.Id).ToList();
        var overrides = await _db.AssignmentOverrides.AsNoTracking()
            .Where(o => ids.Contains(o.AssignmentId) && (o.StudentProfileId == profileId || o.GroupId != null)).ToListAsync(ct);
        var groups = overrides.Any(o => o.GroupId != null)
            ? (await _db.StudentGroupMembers.AsNoTracking().Where(m => m.StudentProfileId == profileId).Select(m => m.GroupId).ToListAsync(ct)).ToHashSet()
            : [];
        var courses = await CoursesAsync(offeringIds, ct);
        var result = new List<CalendarEventDto>();
        foreach (var a in rows)
        {
            var (due, own) = EffectiveDue(a.DueDate, overrides.Where(o => o.AssignmentId == a.Id), profileId, groups);
            if (due < from || due > to) continue;
            var course = courses.GetValueOrDefault(a.CourseOfferingId);
            result.Add(new CalendarEventDto(EventId(CalendarKinds.Assignment, a.Id, owner), CalendarKinds.Assignment, a.Title, due, null, false,
                a.Id, a.CourseOfferingId, course.Code, course.Title,
                a.Done ? CalendarStatuses.Done : due < now ? CalendarStatuses.Overdue : null, null,
                $"/Portal/Assignments?offeringId={a.CourseOfferingId}&selectedAssignmentId={a.Id}",
                owner?.ProfileId, owner?.Name, OwnDates: own));
        }
        return result;
    }

    /// <summary>The student's own date first, then their group's (the latest when in several), else the assignment's (as the policy service).</summary>
    public static (DateTime Due, bool Own) EffectiveDue(DateTime assignmentDue, IEnumerable<AssignmentOverride> overrides, Guid profileId, IReadOnlySet<Guid> groupIds)
    {
        var list = overrides.ToList();
        if (list.FirstOrDefault(o => o.StudentProfileId == profileId) is { } mine) return (mine.DueDate, true);
        var group = list.Where(o => o.GroupId is { } g && groupIds.Contains(g)).OrderByDescending(o => o.DueDate).FirstOrDefault();
        return group is null ? (assignmentDue, false) : (group.DueDate, true);
    }

    private async Task<IEnumerable<CalendarEventDto>> StudentQuizzesAsync(Guid profileId, List<Guid> offeringIds, DateTime from, DateTime to, DateTime now,
        (Guid ProfileId, string Name)? owner, CancellationToken ct)
    {
        var rows = await _db.Quizzes.AsNoTracking()
            .Where(q => offeringIds.Contains(q.CourseOfferingId) && q.IsPublished && q.IsActive)
            .Select(q => new
            {
                q.Id, q.CourseOfferingId, q.Title, q.AvailableUntil,
                OwnUntil = _db.QuizAvailabilityOverrides.Where(o => o.QuizId == q.Id && o.StudentProfileId == profileId).Select(o => o.AvailableUntil).FirstOrDefault(),
                Done = _db.QuizAttempts.Any(t => t.QuizId == q.Id && t.StudentProfileId == profileId && t.FinishedAt != null)
            })
            .ToListAsync(ct);
        var courses = await CoursesAsync(offeringIds, ct);
        var result = new List<CalendarEventDto>();
        foreach (var q in rows)
        {
            // A student's window replaces the quiz's dates it sets (the quiz's closing time when it leaves that open).
            if ((q.OwnUntil ?? q.AvailableUntil) is not { } until || until < from || until > to) continue;
            var course = courses.GetValueOrDefault(q.CourseOfferingId);
            result.Add(new CalendarEventDto(EventId(CalendarKinds.Quiz, q.Id, owner), CalendarKinds.Quiz, q.Title, until, null, false,
                q.Id, q.CourseOfferingId, course.Code, course.Title,
                q.Done ? CalendarStatuses.Done : until < now ? CalendarStatuses.Overdue : null, null,
                $"/Portal/TakeQuiz?quizId={q.Id}", owner?.ProfileId, owner?.Name, OwnDates: q.OwnUntil is not null));
        }
        return result;
    }

    private async Task<IEnumerable<CalendarEventDto>> TeacherAssignmentsAsync(List<Guid> offeringIds, DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await _db.Assignments.AsNoTracking()
            .Where(a => offeringIds.Contains(a.CourseOfferingId) && a.DueDate >= from && a.DueDate <= to)
            .Select(a => new
            {
                a.Id, a.CourseOfferingId, a.Title, a.DueDate, a.IsPublished,
                ToMark = _db.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id && s.Status == SubmissionStatus.Submitted && s.MarksAwarded == null),
                OwnDates = _db.AssignmentOverrides.Any(o => o.AssignmentId == a.Id)
            })
            .ToListAsync(ct);
        var courses = await CoursesAsync(offeringIds, ct);
        return rows.Select(a =>
        {
            var course = courses.GetValueOrDefault(a.CourseOfferingId);
            return new CalendarEventDto(EventId(CalendarKinds.Assignment, a.Id, null), CalendarKinds.Assignment, a.Title, a.DueDate, null, false,
                a.Id, a.CourseOfferingId, course.Code, course.Title, a.IsPublished ? null : CalendarStatuses.Draft, null,
                $"/Portal/Assignments?offeringId={a.CourseOfferingId}&selectedAssignmentId={a.Id}", ToMark: a.ToMark, SomeHaveOwnDates: a.OwnDates);
        }).ToList();
    }

    private async Task<IEnumerable<CalendarEventDto>> TeacherQuizzesAsync(List<Guid> offeringIds, DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await _db.Quizzes.AsNoTracking()
            .Where(q => offeringIds.Contains(q.CourseOfferingId) && q.IsActive && q.AvailableUntil != null && q.AvailableUntil >= from && q.AvailableUntil <= to)
            .Select(q => new { q.Id, q.CourseOfferingId, q.Title, Until = q.AvailableUntil!.Value, q.IsPublished })
            .ToListAsync(ct);
        var courses = await CoursesAsync(offeringIds, ct);
        return rows.Select(q =>
        {
            var course = courses.GetValueOrDefault(q.CourseOfferingId);
            return new CalendarEventDto(EventId(CalendarKinds.Quiz, q.Id, null), CalendarKinds.Quiz, q.Title, q.Until, null, false,
                q.Id, q.CourseOfferingId, course.Code, course.Title, q.IsPublished ? null : CalendarStatuses.Draft, null,
                $"/Portal/Quizzes?offeringId={q.CourseOfferingId}");
        }).ToList();
    }

    private async Task<IEnumerable<CalendarEventDto>> LiveClassesAsync(List<Guid> offeringIds, DateTime from, DateTime to,
        (Guid ProfileId, string Name)? owner, CancellationToken ct)
    {
        var earliest = from.AddMinutes(-LiveSession.MaxDurationMinutes);
        var rows = await _db.LiveSessions.AsNoTracking()
            .Where(s => offeringIds.Contains(s.CourseOfferingId) && s.StartsAt >= earliest && s.StartsAt <= to)
            .Select(s => new { s.Id, s.CourseOfferingId, s.Title, s.StartsAt, s.DurationMinutes, s.Status })
            .ToListAsync(ct);
        var courses = await CoursesAsync(offeringIds, ct);
        return rows.Where(s => s.StartsAt.AddMinutes(s.DurationMinutes) >= from).Select(s =>
        {
            var course = courses.GetValueOrDefault(s.CourseOfferingId);
            return new CalendarEventDto(EventId(CalendarKinds.LiveClass, s.Id, owner), CalendarKinds.LiveClass, s.Title, s.StartsAt,
                s.StartsAt.AddMinutes(s.DurationMinutes), false, s.Id, s.CourseOfferingId, course.Code, course.Title,
                s.Status == LiveSessionStatus.Cancelled ? CalendarStatuses.Cancelled : null, null,
                $"/Portal/LiveClasses?offeringId={s.CourseOfferingId}", owner?.ProfileId, owner?.Name);
        }).ToList();
    }

    /// <summary>Active key dates in the range: of the given semesters, or all of them (null).</summary>
    private async Task<IEnumerable<CalendarEventDto>> KeyDatesAsync(IReadOnlySet<Guid>? semesterIds, DateTime from, DateTime to, CancellationToken ct)
    {
        if (semesterIds is { Count: 0 }) return [];
        var ids = semesterIds?.ToList();
        var start = from.Date;
        var rows = await _db.AcademicDeadlines.AsNoTracking()
            .Where(d => d.IsActive && d.DeadlineDate >= start && d.DeadlineDate <= to && (ids == null || ids.Contains(d.SemesterId)))
            .Select(d => new { d.Id, d.Title, d.Description, d.DeadlineDate, Semester = d.Semester.Name })
            .ToListAsync(ct);
        return rows.Select(d => new CalendarEventDto(EventId(CalendarKinds.KeyDate, d.Id, null), CalendarKinds.KeyDate, d.Title,
            DateTime.SpecifyKind(d.DeadlineDate.Date, DateTimeKind.Utc), null, true, d.Id, null, null, d.Semester, null,
            string.IsNullOrWhiteSpace(d.Description) ? null : d.Description, "/Portal/AcademicCalendar")).ToList();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Reminders
    // ═════════════════════════════════════════════════════════════════════════

    private sealed record Enrolled(Guid OfferingId, Guid ProfileId, Guid UserId, string Name);

    private async Task<List<Enrolled>> EnrolledAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct)
        => (await (from e in _db.Enrollments.AsNoTracking()
                   join p in _db.StudentProfiles on e.StudentProfileId equals p.Id
                   join u in _db.Users on p.UserId equals u.Id
                   where offeringIds.Contains(e.CourseOfferingId) && e.Status == EnrollmentStatus.Active && u.IsActive
                   select new { e.CourseOfferingId, ProfileId = p.Id, UserId = u.Id, Name = u.FullName ?? u.Username })
                .ToListAsync(ct))
            .DistinctBy(x => (x.CourseOfferingId, x.ProfileId))
            .Select(x => new Enrolled(x.CourseOfferingId, x.ProfileId, x.UserId, x.Name)).ToList();

    public async Task<IReadOnlyList<DueReminderCandidate>> DueSoonAsync(DateTime now, DateTime until, CalendarSources sources, CancellationToken ct = default)
    {
        var result = new List<DueReminderCandidate>();
        if (sources.Assignments) result.AddRange(await AssignmentsDueSoonAsync(now, until, ct));
        if (sources.Quizzes) result.AddRange(await QuizzesClosingSoonAsync(now, until, ct));
        return result;
    }

    private async Task<List<DueReminderCandidate>> AssignmentsDueSoonAsync(DateTime now, DateTime until, CancellationToken ct)
    {
        var ids = await _db.Assignments.AsNoTracking().Where(a => a.IsPublished && a.DueDate > now && a.DueDate <= until).Select(a => a.Id)
            .Union(_db.AssignmentOverrides.AsNoTracking().Where(o => o.DueDate > now && o.DueDate <= until).Select(o => o.AssignmentId))
            .ToListAsync(ct);
        if (ids.Count == 0) return [];
        var assignments = await _db.Assignments.AsNoTracking().Where(a => ids.Contains(a.Id) && a.IsPublished)
            .Join(_db.CourseOfferings, a => a.CourseOfferingId, o => o.Id, (a, o) => new { a.Id, a.CourseOfferingId, a.Title, a.DueDate, o.Course.Code })
            .ToListAsync(ct);
        if (assignments.Count == 0) return [];
        ids = assignments.Select(a => a.Id).ToList();
        var overrides = await _db.AssignmentOverrides.AsNoTracking().Where(o => ids.Contains(o.AssignmentId)).ToListAsync(ct);
        var students = await EnrolledAsync(assignments.Select(a => a.CourseOfferingId).Distinct().ToList(), ct);
        var profileIds = students.Select(s => s.ProfileId).Distinct().ToList();
        var groups = overrides.Any(o => o.GroupId != null)
            ? (await _db.StudentGroupMembers.AsNoTracking().Where(m => profileIds.Contains(m.StudentProfileId)).Select(m => new { m.StudentProfileId, m.GroupId }).ToListAsync(ct))
                .GroupBy(m => m.StudentProfileId).ToDictionary(g => g.Key, g => (IReadOnlySet<Guid>)g.Select(m => m.GroupId).ToHashSet())
            : [];
        var handedIn = (await _db.AssignmentSubmissions.AsNoTracking().Where(s => ids.Contains(s.AssignmentId) && !s.IsMissing)
            .Select(s => new { s.AssignmentId, s.StudentProfileId }).ToListAsync(ct)).Select(s => (s.AssignmentId, s.StudentProfileId)).ToHashSet();
        var sent = await SentAsync(DueReminderKind.Assignment, ids, ct);
        var none = new HashSet<Guid>();
        var result = new List<DueReminderCandidate>();
        foreach (var a in assignments)
        {
            var own = overrides.Where(o => o.AssignmentId == a.Id).ToList();
            foreach (var s in students.Where(s => s.OfferingId == a.CourseOfferingId))
            {
                var (due, _) = EffectiveDue(a.DueDate, own, s.ProfileId, groups.GetValueOrDefault(s.ProfileId) ?? none);
                if (due <= now || due > until || handedIn.Contains((a.Id, s.ProfileId)) || sent.Contains((a.Id, s.ProfileId, due))) continue;
                result.Add(new DueReminderCandidate(DueReminderKind.Assignment, a.Id, a.CourseOfferingId, a.Title, a.Code, s.ProfileId, s.UserId, s.Name, due));
            }
        }
        return result;
    }

    private async Task<List<DueReminderCandidate>> QuizzesClosingSoonAsync(DateTime now, DateTime until, CancellationToken ct)
    {
        var ids = await _db.Quizzes.AsNoTracking()
            .Where(q => q.IsPublished && q.IsActive && q.AvailableUntil != null && q.AvailableUntil > now && q.AvailableUntil <= until).Select(q => q.Id)
            .Union(_db.QuizAvailabilityOverrides.AsNoTracking().Where(o => o.AvailableUntil != null && o.AvailableUntil > now && o.AvailableUntil <= until).Select(o => o.QuizId))
            .ToListAsync(ct);
        if (ids.Count == 0) return [];
        var quizzes = await _db.Quizzes.AsNoTracking().Where(q => ids.Contains(q.Id) && q.IsPublished && q.IsActive)
            .Join(_db.CourseOfferings, q => q.CourseOfferingId, o => o.Id, (q, o) => new { q.Id, q.CourseOfferingId, q.Title, q.AvailableFrom, q.AvailableUntil, o.Course.Code })
            .ToListAsync(ct);
        if (quizzes.Count == 0) return [];
        ids = quizzes.Select(q => q.Id).ToList();
        var windows = (await _db.QuizAvailabilityOverrides.AsNoTracking().Where(o => ids.Contains(o.QuizId)).ToListAsync(ct))
            .ToDictionary(o => (o.QuizId, o.StudentProfileId));
        var students = await EnrolledAsync(quizzes.Select(q => q.CourseOfferingId).Distinct().ToList(), ct);
        var taken = (await _db.QuizAttempts.AsNoTracking().Where(t => ids.Contains(t.QuizId) && t.FinishedAt != null)
            .Select(t => new { t.QuizId, t.StudentProfileId }).ToListAsync(ct)).Select(t => (t.QuizId, t.StudentProfileId)).ToHashSet();
        var sent = await SentAsync(DueReminderKind.Quiz, ids, ct);
        var result = new List<DueReminderCandidate>();
        foreach (var q in quizzes)
            foreach (var s in students.Where(s => s.OfferingId == q.CourseOfferingId))
            {
                var window = windows.GetValueOrDefault((q.Id, s.ProfileId));
                var opens = window?.AvailableFrom ?? q.AvailableFrom;
                if ((window?.AvailableUntil ?? q.AvailableUntil) is not { } closes) continue;
                if (closes <= now || closes > until || (opens is { } o && o > now)) continue; // not open yet: nothing to take
                if (taken.Contains((q.Id, s.ProfileId)) || sent.Contains((q.Id, s.ProfileId, closes))) continue;
                result.Add(new DueReminderCandidate(DueReminderKind.Quiz, q.Id, q.CourseOfferingId, q.Title, q.Code, s.ProfileId, s.UserId, s.Name, closes));
            }
        return result;
    }

    private async Task<HashSet<(Guid ItemId, Guid ProfileId, DateTime DueAt)>> SentAsync(DueReminderKind kind, List<Guid> ids, CancellationToken ct)
        => (await _db.DueReminderLogs.AsNoTracking().Where(l => l.Kind == kind && ids.Contains(l.ItemId))
                .Select(l => new { l.ItemId, l.StudentProfileId, l.DueAt }).ToListAsync(ct))
            .Select(l => (l.ItemId, l.StudentProfileId, l.DueAt)).ToHashSet();

    public async Task<IReadOnlyList<CalendarParentLink>> ParentsAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default)
    {
        if (studentProfileIds.Count == 0) return [];
        var ids = studentProfileIds.ToList();
        return await _db.ParentStudentLinks.AsNoTracking()
            .Where(l => l.IsActive && ids.Contains(l.StudentProfileId))
            .Join(_db.Users.Where(u => u.IsActive), l => l.ParentUserId, u => u.Id, (l, u) => new CalendarParentLink(l.StudentProfileId, l.ParentUserId))
            .ToListAsync(ct);
    }

    public void Add(DueReminderLog log) => _db.DueReminderLogs.Add(log);

    // ═════════════════════════════════════════════════════════════════════════
    // People and links
    // ═════════════════════════════════════════════════════════════════════════

    public Task<CalendarUser?> UserAsync(Guid userId, CancellationToken ct = default)
        => _db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new CalendarUser(u.Id, u.Role.Name, u.FullName ?? u.Username, u.IsActive)).FirstOrDefaultAsync(ct);

    public Task<CalendarFeedToken?> FeedTokenForUserAsync(Guid userId, CancellationToken ct = default)
        => _db.CalendarFeedTokens.FirstOrDefaultAsync(t => t.UserId == userId, ct);

    public Task<CalendarFeedToken?> FeedTokenByHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.CalendarFeedTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public void Add(CalendarFeedToken token) => _db.CalendarFeedTokens.Add(token);

    public void Remove(CalendarFeedToken token) => _db.CalendarFeedTokens.Remove(token);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
