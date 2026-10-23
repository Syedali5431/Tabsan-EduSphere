using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.LiveClasses;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M10 — conferencing settings, live sessions, joins, timetable slots and attendance marks.</summary>
public sealed class LiveClassRepository : ILiveClassRepository
{
    private readonly ApplicationDbContext _db;

    public LiveClassRepository(ApplicationDbContext db) => _db = db;

    public Task<ConferenceSetting?> GetSettingAsync(Guid? tenantId, CancellationToken ct = default)
        => _db.ConferenceSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    public void Add(ConferenceSetting setting) => _db.ConferenceSettings.Add(setting);

    private IQueryable<LiveOfferingInfo> Offerings(IQueryable<CourseOffering> offerings)
        => from o in offerings
           join c in _db.Courses.IgnoreQueryFilters() on o.CourseId equals c.Id
           select new LiveOfferingInfo(o.Id, o.CourseId, o.SemesterId, c.Code, c.Title, o.TenantId, o.CampusId, o.FacultyUserId);

    public Task<LiveOfferingInfo?> GetOfferingAsync(Guid offeringId, CancellationToken ct = default)
        => Offerings(_db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => o.Id == offeringId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<LiveOfferingInfo>> GetOfferingsAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct = default)
        => offeringIds.Count == 0 ? [] : await Offerings(_db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => offeringIds.Contains(o.Id))).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetStudentOfferingIdsAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.Enrollments.AsNoTracking().Where(e => e.StudentProfileId == studentProfileId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.CourseOfferingId).Distinct().ToListAsync(ct);

    /// <summary>Offerings the user teaches, plus offerings where they scheduled classes.</summary>
    public async Task<IReadOnlyList<Guid>> GetTeachingOfferingIdsAsync(Guid userId, CancellationToken ct = default)
        => await _db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => o.FacultyUserId == userId).Select(o => o.Id)
            .Union(_db.LiveSessions.AsNoTracking().Where(s => s.CreatedByUserId == userId).Select(s => s.CourseOfferingId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LiveStudent>> GetEnrolledStudentsAsync(Guid offeringId, CancellationToken ct = default)
        => await (from e in _db.Enrollments.AsNoTracking()
                  join sp in _db.StudentProfiles.IgnoreQueryFilters() on e.StudentProfileId equals sp.Id
                  join u in _db.Users.IgnoreQueryFilters() on sp.UserId equals u.Id
                  where e.CourseOfferingId == offeringId && e.Status == EnrollmentStatus.Active
                  select new LiveStudent(sp.Id, u.Id, u.FullName ?? u.Username, u.Email, sp.RegistrationNumber)).Distinct().ToListAsync(ct);

    public Task<bool> IsEnrolledAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default)
        => _db.Enrollments.AnyAsync(e => e.CourseOfferingId == offeringId && e.StudentProfileId == studentProfileId && e.Status == EnrollmentStatus.Active, ct);

    private IQueryable<LiveTimetableSlot> Slots(IQueryable<TimetableEntry> entries, Guid? offeringId = null)
        => from e in entries
           join t in _db.Timetables.IgnoreQueryFilters() on e.TimetableId equals t.Id
           from o in _db.CourseOfferings.IgnoreQueryFilters().Where(o => e.CourseId != null && o.CourseId == e.CourseId && o.SemesterId == t.SemesterId)
           where offeringId == null || o.Id == offeringId
           select new LiveTimetableSlot(e.Id, o.Id, e.DayOfWeek, e.StartTime, e.EndTime, e.SubjectName, e.RoomNumber);

    public async Task<IReadOnlyList<LiveTimetableSlot>> GetTimetableSlotsAsync(Guid offeringId, CancellationToken ct = default)
        => await Slots(_db.TimetableEntries.AsNoTracking(), offeringId).ToListAsync(ct);

    public Task<LiveTimetableSlot?> GetTimetableSlotAsync(Guid entryId, CancellationToken ct = default)
        => Slots(_db.TimetableEntries.AsNoTracking().Where(e => e.Id == entryId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<LiveSession>> GetSessionsAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.LiveSessions.Where(s => s.CourseOfferingId == offeringId).ToListAsync(ct);

    public async Task<IReadOnlyList<LiveSession>> GetSessionsAsync(IReadOnlyCollection<Guid> offeringIds, DateTime from, DateTime to, CancellationToken ct = default)
        => offeringIds.Count == 0 ? [] : await _db.LiveSessions.AsNoTracking()
            .Where(s => offeringIds.Contains(s.CourseOfferingId) && s.StartsAt >= from && s.StartsAt <= to).ToListAsync(ct);

    public Task<LiveSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
        => _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task<IReadOnlyList<LiveSession>> GetSessionsNeedingReminderAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.LiveSessions.Where(s => s.Status == LiveSessionStatus.Scheduled && s.ReminderSentAt == null && s.StartsAt >= from && s.StartsAt <= to).ToListAsync(ct);

    public async Task<IReadOnlyList<LiveSession>> GetSessionsNeedingAttendanceAsync(DateTime endedBefore, CancellationToken ct = default)
        => await _db.LiveSessions.Where(s => s.Status == LiveSessionStatus.Scheduled && s.AutoAttendance && s.AttendanceTakenAt == null
                && s.StartsAt.AddMinutes(s.DurationMinutes) <= endedBefore)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LiveSession>> GetSessionsWithoutRecordingAsync(DateTime endedAfter, DateTime endedBefore, CancellationToken ct = default)
        => await _db.LiveSessions.Where(s => s.Status == LiveSessionStatus.Scheduled && s.RecordingUrl == null && s.ExternalMeetingId != null
                && s.StartsAt.AddMinutes(s.DurationMinutes) >= endedAfter && s.StartsAt.AddMinutes(s.DurationMinutes) <= endedBefore)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LiveSessionAttendance>> GetAttendanceAsync(Guid sessionId, CancellationToken ct = default)
        => await _db.LiveSessionAttendances.Where(a => a.SessionId == sessionId).ToListAsync(ct);

    public Task<LiveSessionAttendance?> GetAttendanceAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
        => _db.LiveSessionAttendances.FirstOrDefaultAsync(a => a.SessionId == sessionId && a.UserId == userId, ct);

    public async Task<IReadOnlySet<Guid>> GetStudentsWithAttendanceAsync(Guid offeringId, DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        return (await _db.AttendanceRecords.AsNoTracking().Where(r => r.CourseOfferingId == offeringId && r.Date == day)
            .Select(r => r.StudentProfileId).ToListAsync(ct)).ToHashSet();
    }

    public void Add(AttendanceRecord record) => _db.AttendanceRecords.Add(record);
    public void Add(LiveSession session) => _db.LiveSessions.Add(session);
    public void Add(LiveSessionAttendance attendance) => _db.LiveSessionAttendances.Add(attendance);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
