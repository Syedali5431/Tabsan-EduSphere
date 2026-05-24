using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.DTOs.Attendance;
using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Academic;

// Phase 26 — Stage 26.3

public class ParentPortalService : IParentPortalService
{
    private readonly IParentStudentLinkRepository _linkRepo;
    private readonly IStudentProfileRepository _studentRepo;
    private readonly IUserRepository _userRepo;
    private readonly IResultService? _resultService;
    private readonly IAttendanceService? _attendanceService;
    private readonly IAnnouncementService? _announcementService;
    private readonly ITimetableService? _timetableService;
    private readonly IEnrollmentRepository? _enrollmentRepo;

    public ParentPortalService(
        IParentStudentLinkRepository linkRepo,
        IStudentProfileRepository studentRepo,
        IUserRepository userRepo,
        IResultService? resultService = null,
        IAttendanceService? attendanceService = null,
        IAnnouncementService? announcementService = null,
        ITimetableService? timetableService = null,
        IEnrollmentRepository? enrollmentRepo = null)
    {
        _linkRepo = linkRepo;
        _studentRepo = studentRepo;
        _userRepo = userRepo;
        _resultService = resultService;
        _attendanceService = attendanceService;
        _announcementService = announcementService;
        _timetableService = timetableService;
        _enrollmentRepo = enrollmentRepo;
    }

    public async Task<IReadOnlyList<ParentLinkedStudentDto>> GetLinkedStudentsAsync(
        Guid parentUserId,
        CancellationToken ct = default)
    {
        var links = await _linkRepo.GetByParentUserIdAsync(parentUserId, ct);

        var result = new List<ParentLinkedStudentDto>(links.Count);
        foreach (var link in links.Where(l => l.IsActive))
        {
            var student = await _studentRepo.GetByIdAsync(link.StudentProfileId, ct);
            if (student is null)
                continue;

            result.Add(new ParentLinkedStudentDto(
                student.Id,
                student.RegistrationNumber,
                student.ProgramId,
                student.DepartmentId,
                student.CurrentSemesterNumber,
                student.Cgpa,
                student.CurrentSemesterGpa,
                link.Relationship));
        }

        return result;
    }

    public async Task<IReadOnlyList<ResultResponse>> GetLinkedStudentResultsAsync(
        Guid parentUserId,
        Guid studentProfileId,
        CancellationToken ct = default)
    {
        var service = _resultService
            ?? throw new InvalidOperationException("Result service is not configured.");

        await EnsureLinkedStudentAccessAsync(parentUserId, studentProfileId, ct);
        return await service.GetPublishedByStudentAsync(studentProfileId, ct);
    }

    public async Task<IReadOnlyList<AttendanceResponse>> GetLinkedStudentAttendanceAsync(
        Guid parentUserId,
        Guid studentProfileId,
        Guid? courseOfferingId = null,
        CancellationToken ct = default)
    {
        var service = _attendanceService
            ?? throw new InvalidOperationException("Attendance service is not configured.");

        await EnsureLinkedStudentAccessAsync(parentUserId, studentProfileId, ct);
        return await service.GetByStudentAsync(studentProfileId, courseOfferingId, ct: ct);
    }

    public async Task<IReadOnlyList<CourseAnnouncementDto>> GetLinkedStudentAnnouncementsAsync(
        Guid parentUserId,
        Guid studentProfileId,
        Guid? courseOfferingId = null,
        CancellationToken ct = default)
    {
        var announcementService = _announcementService
            ?? throw new InvalidOperationException("Announcement service is not configured.");
        var enrollmentRepo = _enrollmentRepo
            ?? throw new InvalidOperationException("Enrollment repository is not configured.");

        await EnsureLinkedStudentAccessAsync(parentUserId, studentProfileId, ct);

        var activeOfferingIds = (await enrollmentRepo.GetByStudentAsync(studentProfileId, ct))
            .Where(e => e.Status == EnrollmentStatus.Active)
            .Select(e => e.CourseOfferingId)
            .Distinct()
            .ToHashSet();

        if (activeOfferingIds.Count == 0)
            return [];

        if (courseOfferingId.HasValue && !activeOfferingIds.Contains(courseOfferingId.Value))
            return [];

        var targetOfferingIds = courseOfferingId.HasValue
            ? [courseOfferingId.Value]
            : activeOfferingIds;

        var items = new List<CourseAnnouncementDto>();
        foreach (var offeringId in targetOfferingIds)
        {
            var offeringItems = await announcementService.GetByOfferingAsync(offeringId, includeInactive: false, tenantId: null, campusId: null, ct);
            items.AddRange(offeringItems);
        }

        return items
            .OrderByDescending(a => a.PostedAt)
            .ToList();
    }

    public async Task<TimetableDto?> GetLinkedStudentTimetableAsync(
        Guid parentUserId,
        Guid studentProfileId,
        Guid? timetableId = null,
        CancellationToken ct = default)
    {
        var timetableService = _timetableService
            ?? throw new InvalidOperationException("Timetable service is not configured.");

        var student = await GetLinkedStudentAsync(parentUserId, studentProfileId, ct);
        var published = await timetableService.GetByDepartmentAsync(student.DepartmentId, publishedOnly: true, ct);
        if (published.Count == 0)
            return null;

        var selected = timetableId.HasValue
            ? published.FirstOrDefault(t => t.Id == timetableId.Value)
            : published
                .OrderByDescending(t => t.PublishedAt ?? DateTime.MinValue)
                .ThenByDescending(t => t.EffectiveDate)
                .First();

        if (selected is null)
            throw new KeyNotFoundException($"Timetable {timetableId} not found for this student.");

        return await timetableService.GetByIdAsync(selected.Id, ct);
    }

    public async Task<IReadOnlyList<ParentStudentLinkDto>> GetLinksByParentAsync(
        Guid parentUserId,
        CancellationToken ct = default)
    {
        var links = await _linkRepo.GetByParentUserIdAsync(parentUserId, ct);
        return links
            .Select(l => new ParentStudentLinkDto(l.ParentUserId, l.StudentProfileId, l.Relationship, l.IsActive))
            .ToList();
    }

    public async Task<ParentStudentLinkDto> UpsertLinkAsync(
        UpsertParentStudentLinkRequest request,
        CancellationToken ct = default)
    {
        var parentUser = await _userRepo.GetByIdAsync(request.ParentUserId, ct)
            ?? throw new KeyNotFoundException($"Parent user {request.ParentUserId} not found.");

        if (!string.Equals(parentUser.Role?.Name, "Parent", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Target user must have Parent role.");

        var student = await _studentRepo.GetByIdAsync(request.StudentProfileId, ct)
            ?? throw new KeyNotFoundException($"Student profile {request.StudentProfileId} not found.");

        if (student.Department?.InstitutionType != InstitutionType.School)
            throw new InvalidOperationException("Parent-portal linking is only allowed for School students.");

        var existing = await _linkRepo.GetByParentAndStudentAsync(request.ParentUserId, request.StudentProfileId, ct);
        if (existing is null)
        {
            var created = new Domain.Academic.ParentStudentLink(
                request.ParentUserId,
                request.StudentProfileId,
                request.Relationship);
            if (!request.IsActive)
                created.Update(request.Relationship, false);

            await _linkRepo.AddAsync(created, ct);
            await _linkRepo.SaveChangesAsync(ct);
            return new ParentStudentLinkDto(created.ParentUserId, created.StudentProfileId, created.Relationship, created.IsActive);
        }

        existing.Update(request.Relationship, request.IsActive);
        _linkRepo.Update(existing);
        await _linkRepo.SaveChangesAsync(ct);
        return new ParentStudentLinkDto(existing.ParentUserId, existing.StudentProfileId, existing.Relationship, existing.IsActive);
    }

    public async Task<bool> DeactivateLinkAsync(Guid parentUserId, Guid studentProfileId, CancellationToken ct = default)
    {
        var existing = await _linkRepo.GetByParentAndStudentAsync(parentUserId, studentProfileId, ct);
        if (existing is null)
            return false;

        existing.Update(existing.Relationship, false);
        _linkRepo.Update(existing);
        await _linkRepo.SaveChangesAsync(ct);
        return true;
    }

    private async Task EnsureLinkedStudentAccessAsync(Guid parentUserId, Guid studentProfileId, CancellationToken ct)
        => _ = await GetLinkedStudentAsync(parentUserId, studentProfileId, ct);

    private async Task<Domain.Academic.StudentProfile> GetLinkedStudentAsync(Guid parentUserId, Guid studentProfileId, CancellationToken ct)
    {
        var link = await _linkRepo.GetByParentAndStudentAsync(parentUserId, studentProfileId, ct);
        if (link is null || !link.IsActive)
            throw new UnauthorizedAccessException("Parent does not have an active link for the requested student.");

        var student = await _studentRepo.GetByIdAsync(studentProfileId, ct)
            ?? throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        return student;
    }
}
