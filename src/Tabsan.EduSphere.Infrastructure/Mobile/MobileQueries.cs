using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Mobile;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Mobile;

/// <summary>
/// Plan M12 — the app's home screen, timetable and children in one call each (small answers for phones).
/// Filters run before projecting (EF cannot translate filters on projected records).
/// </summary>
public sealed class MobileQueries : IMobileQueries
{
    private static readonly string[] DayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
    private readonly ApplicationDbContext _db;

    public MobileQueries(ApplicationDbContext db) => _db = db;

    public async Task<MobileHomeResponse> HomeAsync(Guid userId, string role, int? dayOfWeek = null, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new { u.FullName, u.Username }).FirstOrDefaultAsync(ct);
        var unread = await _db.NotificationRecipients.CountAsync(r => r.RecipientUserId == userId && !r.IsRead, ct);
        var today = dayOfWeek is >= 0 and <= 6 ? dayOfWeek.Value : (int)DateTime.UtcNow.DayOfWeek;
        var name = user?.FullName ?? user?.Username;

        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var profile = await _db.StudentProfiles.AsNoTracking().Where(p => p.UserId == userId)
                .Select(p => new { p.Id, p.RegistrationNumber, ProgramName = p.Program.Name, p.Cgpa, p.Department.InstitutionType }).FirstOrDefaultAsync(ct);
            if (profile is null) return Empty(role, name, unread);
            var offerings = await ActiveOfferingsAsync(profile.Id, ct);
            var timetable = (await StudentTimetableAsync(offerings, ct)).Where(c => c.DayOfWeek == today).ToList();
            var due = await StudentDueAsync(profile.Id, offerings, ct);
            var announcements = await AnnouncementsAsync(offerings.Select(o => o.OfferingId).ToList(), ct);
            var summary = new MobileStudentSummary(profile.Id, profile.RegistrationNumber, profile.ProgramName,
                await AttendancePercentAsync(profile.Id, ct), GpaOrNull(profile.InstitutionType, profile.Cgpa), offerings.Count,
                await OutstandingFeesAsync(profile.Id, ct), await AveragePercentAsync(profile.Id, ct));
            return new MobileHomeResponse("Student", name, unread, timetable, due, announcements, summary, null, null);
        }

        if (string.Equals(role, "Parent", StringComparison.OrdinalIgnoreCase))
        {
            var children = await ChildrenAsync(userId, ct);
            var childIds = children.Select(c => c.StudentProfileId).ToList();
            var offerings = await _db.Enrollments.AsNoTracking()
                .Where(e => childIds.Contains(e.StudentProfileId) && e.Status == EnrollmentStatus.Active)
                .Select(e => e.CourseOfferingId).Distinct().ToListAsync(ct);
            return new MobileHomeResponse("Parent", name, unread, [], [], await AnnouncementsAsync(offerings, ct), null, null, children);
        }

        if (string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase))
        {
            var teaching = await _db.CourseOfferings.AsNoTracking().Where(o => o.FacultyUserId == userId).Select(o => o.Id).ToListAsync(ct);
            var classes = (await TeacherTimetableAsync(userId, ct)).Where(c => c.DayOfWeek == today).ToList();
            var toGrade = await _db.AssignmentSubmissions.AsNoTracking()
                .CountAsync(s => teaching.Contains(s.Assignment.CourseOfferingId) && s.Status == SubmissionStatus.Submitted && s.MarksAwarded == null, ct);
            var now = DateTime.UtcNow;
            var live = await _db.LiveSessions.AsNoTracking()
                .Where(s => teaching.Contains(s.CourseOfferingId) && s.StartsAt >= now.AddHours(-1) && s.StartsAt <= now.AddDays(7)
                            && s.Status != Tabsan.EduSphere.Domain.LiveClasses.LiveSessionStatus.Cancelled)
                .OrderBy(s => s.StartsAt).Take(5)
                .Select(s => new { s.Id, OfferingId = s.CourseOfferingId, s.Title, s.StartsAt }).ToListAsync(ct);
            var courseTitles = await CourseTitlesAsync(teaching, ct);
            var due = live.Select(s => new MobileDue("LiveClass", s.Id, s.OfferingId, s.Title, courseTitles.GetValueOrDefault(s.OfferingId), s.StartsAt, false)).ToList();
            return new MobileHomeResponse("Faculty", name, unread, classes, due, await AnnouncementsAsync(teaching, ct), null,
                new MobileTeacherSummary(teaching.Count, toGrade, classes.Count), null);
        }

        return Empty(role, name, unread);
    }

    private static MobileHomeResponse Empty(string role, string? name, int unread) => new(role, name, unread, [], [], [], null, null, null);

    public async Task<IReadOnlyList<MobileClass>> TimetableAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase)) return await TeacherTimetableAsync(userId, ct);
        if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase)) return [];
        var profileId = await _db.StudentProfiles.Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
        return profileId is null ? [] : await StudentTimetableAsync(await ActiveOfferingsAsync(profileId.Value, ct), ct);
    }

    public async Task<IReadOnlyList<MobileChildSummary>> ChildrenAsync(Guid parentUserId, CancellationToken ct = default)
    {
        var children = await _db.ParentStudentLinks.AsNoTracking()
            .Where(l => l.ParentUserId == parentUserId && l.IsActive)
            .Join(_db.StudentProfiles, l => l.StudentProfileId, p => p.Id,
                (l, p) => new { p.Id, p.UserId, p.RegistrationNumber, ProgramName = p.Program.Name, p.Cgpa, p.Department.InstitutionType })
            .ToListAsync(ct);
        var result = new List<MobileChildSummary>();
        foreach (var c in children)
        {
            var name = await _db.Users.Where(u => u.Id == c.UserId).Select(u => u.FullName ?? u.Username).FirstOrDefaultAsync(ct) ?? c.RegistrationNumber;
            var published = await _db.Results.CountAsync(r => r.StudentProfileId == c.Id && r.IsPublished, ct);
            result.Add(new MobileChildSummary(c.Id, name, c.RegistrationNumber, c.ProgramName, await AttendancePercentAsync(c.Id, ct),
                GpaOrNull(c.InstitutionType, c.Cgpa), await OutstandingFeesAsync(c.Id, ct), published, await AveragePercentAsync(c.Id, ct)));
        }
        return result.OrderBy(c => c.Name).ToList();
    }

    public async Task<IReadOnlyList<MobileCourse>> CoursesAsync(Guid userId, string role, CancellationToken ct = default)
    {
        IQueryable<CourseOffering> offerings;
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var profileId = await _db.StudentProfiles.Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            if (profileId is null) return [];
            var ids = _db.Enrollments.Where(e => e.StudentProfileId == profileId && e.Status == EnrollmentStatus.Active).Select(e => e.CourseOfferingId);
            offerings = _db.CourseOfferings.Where(o => ids.Contains(o.Id));
        }
        else if (string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase))
            offerings = _db.CourseOfferings.Where(o => o.FacultyUserId == userId);
        else return [];
        var rows = await offerings.AsNoTracking()
            .Select(o => new { o.Id, o.Course.Code, o.Course.Title, Semester = o.Semester.Name, o.FacultyUserId })
            .ToListAsync(ct);
        var teacherIds = rows.Where(r => r.FacultyUserId != null).Select(r => r.FacultyUserId!.Value).Distinct().ToList();
        var teachers = await _db.Users.AsNoTracking().Where(u => teacherIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.Username }).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        return rows.Select(r => new MobileCourse(r.Id, r.Code, r.Title, r.Semester, r.FacultyUserId is { } f ? teachers.GetValueOrDefault(f) : null))
            .OrderBy(c => c.CourseCode, StringComparer.Ordinal).ToList();
    }

    public async Task<MobileCourseResponse?> CourseAsync(Guid userId, string role, Guid offeringId, CancellationToken ct = default)
    {
        var course = (await CoursesAsync(userId, role, ct)).FirstOrDefault(c => c.OfferingId == offeringId);
        if (course is null) return null;
        var isTeacher = string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase);
        var offering = await _db.CourseOfferings.AsNoTracking().Where(o => o.Id == offeringId).Select(o => new { o.CourseId, o.SemesterId }).FirstAsync(ct);
        var materials = await _db.CourseMaterials.AsNoTracking()
            .Where(m => m.CourseId == offering.CourseId && m.SemesterId == offering.SemesterId && m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new MobileMaterial(m.Id, m.Name, m.Description, m.MaterialType.ToString(), m.LinkUrl, m.FilePath != null))
            .ToListAsync(ct);
        var profileId = isTeacher ? null : await _db.StudentProfiles.Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
        var assignments = await _db.Assignments.AsNoTracking()
            .Where(a => a.CourseOfferingId == offeringId && (a.IsPublished || isTeacher))
            .OrderBy(a => a.DueDate)
            .Select(a => new
            {
                a.Id, a.Title, a.Description, a.DueDate, a.MaxMarks,
                Mine = _db.AssignmentSubmissions.Where(s => s.AssignmentId == a.Id && s.StudentProfileId == profileId && !s.IsMissing)
                    .Select(s => new { s.MarksAwarded, s.Feedback, s.Status, s.PostedAt }).FirstOrDefault(),
                ToGrade = isTeacher ? _db.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id && s.Status == SubmissionStatus.Submitted && s.MarksAwarded == null) : 0
            })
            .ToListAsync(ct);
        var quizzes = await _db.Quizzes.AsNoTracking()
            .Where(q => q.CourseOfferingId == offeringId && q.IsActive && (q.IsPublished || isTeacher))
            .OrderBy(q => q.AvailableUntil)
            .Select(q => new
            {
                q.Id, q.Title, q.AvailableFrom, q.AvailableUntil, q.TimeLimitMinutes, q.MaxAttempts,
                Used = _db.QuizAttempts.Count(t => t.QuizId == q.Id && t.StudentProfileId == profileId)
            })
            .ToListAsync(ct);
        return new MobileCourseResponse(course, materials,
            assignments.Select(a => new MobileAssignment(a.Id, a.Title, a.Description, a.DueDate, a.MaxMarks, a.Mine is not null,
                a.Mine?.PostedAt != null ? a.Mine.MarksAwarded : null, a.Mine?.PostedAt != null ? a.Mine.Feedback : null, a.Mine?.Status.ToString(),
                a.ToGrade)).ToList(),
            quizzes.Select(q => new MobileQuiz(q.Id, q.Title, q.AvailableFrom, q.AvailableUntil, q.TimeLimitMinutes, q.Used, q.MaxAttempts)).ToList(),
            await AnnouncementsAsync([offeringId], ct), isTeacher);
    }

    public async Task<IReadOnlyList<MobileResult>?> ResultsAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default)
    {
        if (await AllowedStudentAsync(userId, role, studentProfileId, ct) is not { } profileId) return null;
        var rows = await _db.Results.AsNoTracking()
            .Where(r => r.StudentProfileId == profileId && r.IsPublished)
            .Join(_db.CourseOfferings, r => r.CourseOfferingId, o => o.Id, (r, o) => new
            {
                r.CourseOfferingId, o.Course.Code, o.Course.Title, Semester = o.Semester.Name, r.ResultType, r.MarksObtained, r.MaxMarks, r.GradePoint, r.PublishedAt
            })
            .ToListAsync(ct);
        return rows.Select(r => new MobileResult(r.CourseOfferingId, r.Code, r.Title, r.Semester, r.ResultType, r.MarksObtained, r.MaxMarks,
                r.MaxMarks == 0 ? 0 : Math.Round(100m * r.MarksObtained / r.MaxMarks, 1), r.GradePoint, r.PublishedAt))
            .OrderByDescending(r => r.PublishedAt).ThenBy(r => r.CourseCode, StringComparer.Ordinal).ToList();
    }

    public async Task<IReadOnlyList<MobileReportCard>?> ReportCardsAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default)
    {
        if (await AllowedStudentAsync(userId, role, studentProfileId, ct) is not { } profileId) return null;
        var cards = await _db.StudentReportCards.AsNoTracking()
            .Where(c => c.StudentProfileId == profileId)
            .OrderByDescending(c => c.GeneratedAt).Take(20)
            .Select(c => new { c.Id, c.PeriodLabel, c.GeneratedAt, c.PayloadJson })
            .ToListAsync(ct);
        return cards.Select(c => new MobileReportCard(c.Id, c.PeriodLabel, c.GeneratedAt, ReportCardLines.From(c.PayloadJson))).ToList();
    }

    public async Task<IReadOnlyList<MobileAttendanceCourse>?> AttendanceAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default)
    {
        if (await AllowedStudentAsync(userId, role, studentProfileId, ct) is not { } profileId) return null;
        var rows = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.StudentProfileId == profileId)
            .GroupBy(a => a.CourseOfferingId)
            .Select(g => new
            {
                OfferingId = g.Key, Total = g.Count(),
                Present = g.Count(a => a.Status == AttendanceStatus.Present), Absent = g.Count(a => a.Status == AttendanceStatus.Absent),
                Late = g.Count(a => a.Status == AttendanceStatus.Late), Excused = g.Count(a => a.Status == AttendanceStatus.Excused),
                Last = g.Max(a => a.Date)
            })
            .ToListAsync(ct);
        var ids = rows.Select(r => r.OfferingId).ToList();
        var courses = await _db.CourseOfferings.AsNoTracking().Where(o => ids.Contains(o.Id))
            .Select(o => new { o.Id, o.Course.Code, o.Course.Title }).ToDictionaryAsync(o => o.Id, ct);
        return rows.Select(r =>
            {
                var attended = r.Present + r.Late;
                var course = courses.GetValueOrDefault(r.OfferingId);
                return new MobileAttendanceCourse(r.OfferingId, course?.Code ?? "", course?.Title ?? "", r.Total, attended, r.Absent, r.Late, r.Excused,
                    r.Total == 0 ? null : Math.Round(100m * attended / r.Total, 1), r.Last);
            })
            .OrderBy(r => r.Percent ?? 100).ToList();
    }

    public async Task<IReadOnlyList<MobileAnnouncement>?> StudentAnnouncementsAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default)
    {
        if (await AllowedStudentAsync(userId, role, studentProfileId, ct) is not { } profileId) return null;
        var offerings = await ActiveOfferingsAsync(profileId, ct);
        return await AnnouncementsAsync(offerings.Select(o => o.OfferingId).ToList(), ct, take: 20);
    }

    /// <summary>The student's own profile, or a parent's actively linked child; null otherwise.</summary>
    private async Task<Guid?> AllowedStudentAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct)
    {
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var own = await _db.StudentProfiles.Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            return studentProfileId is null || studentProfileId == own ? own : null;
        }
        if (string.Equals(role, "Parent", StringComparison.OrdinalIgnoreCase) && studentProfileId is { } child)
            return await _db.ParentStudentLinks.AnyAsync(l => l.ParentUserId == userId && l.StudentProfileId == child && l.IsActive, ct) ? child : null;
        return null;
    }

    // ── pieces ─────────────────────────────────────────────────────────────────────────────────

    private async Task<List<(Guid OfferingId, Guid CourseId, Guid SemesterId)>> ActiveOfferingsAsync(Guid studentProfileId, CancellationToken ct)
        => (await _db.Enrollments.AsNoTracking()
                .Where(e => e.StudentProfileId == studentProfileId && e.Status == EnrollmentStatus.Active)
                .Select(e => new { e.CourseOfferingId, e.CourseOffering.CourseId, e.CourseOffering.SemesterId })
                .ToListAsync(ct))
            .Select(x => (x.CourseOfferingId, x.CourseId, x.SemesterId)).ToList();

    private async Task<IReadOnlyList<MobileClass>> StudentTimetableAsync(List<(Guid OfferingId, Guid CourseId, Guid SemesterId)> offerings, CancellationToken ct)
    {
        if (offerings.Count == 0) return [];
        var courseIds = offerings.Select(o => o.CourseId).Distinct().ToList();
        var semesterIds = offerings.Select(o => o.SemesterId).Distinct().ToList();
        var entries = await _db.TimetableEntries.AsNoTracking()
            .Where(e => e.CourseId != null && courseIds.Contains(e.CourseId.Value) && e.Timetable.IsPublished && semesterIds.Contains(e.Timetable.SemesterId))
            .Select(e => new
            {
                e.DayOfWeek, e.StartTime, e.EndTime, e.SubjectName, e.CourseId, CourseCode = e.Course != null ? e.Course.Code : null,
                Room = e.Room != null ? e.Room.Number : e.RoomNumber, Building = e.Building != null ? e.Building.Name : null, e.FacultyName,
                e.Timetable.SemesterId
            })
            .ToListAsync(ct);
        return entries
            .Select(e => Class(e.DayOfWeek, e.StartTime, e.EndTime, e.SubjectName, e.CourseCode, e.Room, e.Building, e.FacultyName, e.CourseId,
                offerings.Where(o => o.CourseId == e.CourseId && o.SemesterId == e.SemesterId).Select(o => (Guid?)o.OfferingId).FirstOrDefault()))
            .OrderBy(c => c.DayOfWeek).ThenBy(c => c.Start, StringComparer.Ordinal).ToList();
    }

    private async Task<IReadOnlyList<MobileClass>> TeacherTimetableAsync(Guid userId, CancellationToken ct)
    {
        var entries = await _db.TimetableEntries.AsNoTracking()
            .Where(e => e.FacultyUserId == userId && e.Timetable.IsPublished)
            .Select(e => new
            {
                e.DayOfWeek, e.StartTime, e.EndTime, e.SubjectName, e.CourseId, CourseCode = e.Course != null ? e.Course.Code : null,
                Room = e.Room != null ? e.Room.Number : e.RoomNumber, Building = e.Building != null ? e.Building.Name : null, e.FacultyName,
                e.Timetable.SemesterId
            })
            .ToListAsync(ct);
        var offerings = await _db.CourseOfferings.AsNoTracking().Where(o => o.FacultyUserId == userId)
            .Select(o => new { o.Id, o.CourseId, o.SemesterId }).ToListAsync(ct);
        return entries
            .Select(e => Class(e.DayOfWeek, e.StartTime, e.EndTime, e.SubjectName, e.CourseCode, e.Room, e.Building, e.FacultyName, e.CourseId,
                offerings.Where(o => o.CourseId == e.CourseId && o.SemesterId == e.SemesterId).Select(o => (Guid?)o.Id).FirstOrDefault()))
            .OrderBy(c => c.DayOfWeek).ThenBy(c => c.Start, StringComparer.Ordinal).ToList();
    }

    private static MobileClass Class(int day, TimeOnly start, TimeOnly end, string subject, string? code, string? room, string? building,
        string? teacher, Guid? courseId, Guid? offeringId)
        => new(day, DayNames[Math.Clamp(day, 0, 6)], start.ToString("HH:mm"), end.ToString("HH:mm"), subject, code, room, building, teacher, courseId, offeringId);

    /// <summary>Assignments and quizzes due in the next two weeks (and overdue ones not handed in), soonest first.</summary>
    private async Task<IReadOnlyList<MobileDue>> StudentDueAsync(Guid studentProfileId, List<(Guid OfferingId, Guid CourseId, Guid SemesterId)> offerings, CancellationToken ct)
    {
        if (offerings.Count == 0) return [];
        var ids = offerings.Select(o => o.OfferingId).ToList();
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(14);
        var titles = await CourseTitlesAsync(ids, ct);
        var assignments = await _db.Assignments.AsNoTracking()
            .Where(a => ids.Contains(a.CourseOfferingId) && a.IsPublished && a.DueDate >= now.AddDays(-7) && a.DueDate <= horizon)
            .Select(a => new
            {
                a.Id, a.CourseOfferingId, a.Title, a.DueDate,
                Done = _db.AssignmentSubmissions.Any(s => s.AssignmentId == a.Id && s.StudentProfileId == studentProfileId && !s.IsMissing)
            })
            .ToListAsync(ct);
        var quizzes = await _db.Quizzes.AsNoTracking()
            .Where(q => ids.Contains(q.CourseOfferingId) && q.IsPublished && q.IsActive && q.AvailableUntil != null
                        && q.AvailableUntil >= now && q.AvailableUntil <= horizon)
            .Select(q => new
            {
                q.Id, q.CourseOfferingId, q.Title, Due = q.AvailableUntil!.Value,
                Done = _db.QuizAttempts.Any(t => t.QuizId == q.Id && t.StudentProfileId == studentProfileId && t.FinishedAt != null)
            })
            .ToListAsync(ct);
        return assignments.Where(a => a.DueDate >= now || !a.Done)
            .Select(a => new MobileDue("Assignment", a.Id, a.CourseOfferingId, a.Title, titles.GetValueOrDefault(a.CourseOfferingId), a.DueDate, a.Done))
            .Concat(quizzes.Select(q => new MobileDue("Quiz", q.Id, q.CourseOfferingId, q.Title, titles.GetValueOrDefault(q.CourseOfferingId), q.Due, q.Done)))
            .OrderBy(d => d.Done).ThenBy(d => d.DueAt).Take(20).ToList();
    }

    private async Task<IReadOnlyList<MobileAnnouncement>> AnnouncementsAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct, int take = 5)
    {
        if (offeringIds.Count == 0) return [];
        var ids = offeringIds.ToList();
        var titles = await CourseTitlesAsync(ids, ct);
        var rows = await _db.CourseAnnouncements.AsNoTracking()
            .Where(a => a.OfferingId != null && ids.Contains(a.OfferingId.Value))
            .OrderByDescending(a => a.PostedAt).Take(take)
            .Select(a => new { a.Id, a.Title, a.Body, a.PostedAt, a.OfferingId })
            .ToListAsync(ct);
        return rows.Select(a => new MobileAnnouncement(a.Id, a.Title, a.Body.Length > 280 ? a.Body[..279] + "…" : a.Body, a.PostedAt,
            a.OfferingId is { } o ? titles.GetValueOrDefault(o) : null)).ToList();
    }

    private async Task<Dictionary<Guid, string>> CourseTitlesAsync(List<Guid> offeringIds, CancellationToken ct)
        => offeringIds.Count == 0 ? [] : await _db.CourseOfferings.AsNoTracking().Where(o => offeringIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Course.Title }).ToDictionaryAsync(o => o.Id, o => o.Title, ct);

    /// <summary>Present or late over all marked classes (null when nothing is marked yet).</summary>
    private async Task<decimal?> AttendancePercentAsync(Guid studentProfileId, CancellationToken ct)
    {
        var counts = await _db.AttendanceRecords.AsNoTracking().Where(a => a.StudentProfileId == studentProfileId)
            .GroupBy(a => 1)
            .Select(g => new { Total = g.Count(), Attended = g.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) })
            .FirstOrDefaultAsync(ct);
        return counts is null || counts.Total == 0 ? null : Math.Round(100m * counts.Attended / counts.Total, 1);
    }

    /// <summary>GPA/CGPA is a University measure; School and College standing is a percentage (see <see cref="AveragePercentAsync"/>).</summary>
    private static decimal? GpaOrNull(InstitutionType type, decimal? cgpa) => type == InstitutionType.University ? cgpa : null;

    /// <summary>
    /// Published marks over the published maximum marks, in percent (null without results). The "Total" rows the result
    /// calculation keeps per course repeat their components, so they only count when a course has nothing else.
    /// </summary>
    private async Task<decimal?> AveragePercentAsync(Guid studentProfileId, CancellationToken ct)
    {
        var rows = await _db.Results.AsNoTracking()
            .Where(r => r.StudentProfileId == studentProfileId && r.IsPublished && r.MaxMarks > 0)
            .Select(r => new { r.CourseOfferingId, r.ResultType, r.MarksObtained, r.MaxMarks })
            .ToListAsync(ct);
        var counted = rows.GroupBy(r => r.CourseOfferingId)
            .SelectMany(g => g.Any(r => !IsTotal(r.ResultType)) ? g.Where(r => !IsTotal(r.ResultType)) : g)
            .ToList();
        var max = counted.Sum(r => r.MaxMarks);
        return max == 0 ? null : Math.Round(100m * counted.Sum(r => r.MarksObtained) / max, 1);
    }

    private static bool IsTotal(string? resultType) => string.Equals(resultType, "Total", StringComparison.OrdinalIgnoreCase);

    /// <summary>Unpaid fee receipts (pending or proof submitted).</summary>
    private async Task<decimal?> OutstandingFeesAsync(Guid studentProfileId, CancellationToken ct)
    {
        var receipts = _db.PaymentReceipts.AsNoTracking()
            .Where(r => r.StudentProfileId == studentProfileId && (r.Status == Tabsan.EduSphere.Domain.Enums.PaymentReceiptStatus.Pending
                                                                  || r.Status == Tabsan.EduSphere.Domain.Enums.PaymentReceiptStatus.Submitted));
        return await receipts.AnyAsync(ct) ? await receipts.SumAsync(r => r.Amount, ct) : 0m;
    }
}
