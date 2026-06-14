using System.Reflection;
using FluentAssertions;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.UnitTests;

public sealed class EnrollmentServiceWaitlistTests
{
    [Fact]
    public async Task EnrollAsync_WhenOfferingIsFull_CreatesWaitlistedEnrollment()
    {
        var studentId = Guid.NewGuid();
        var offering = CreateOffering(maxEnrollment: 1, isOpen: true);
        var repo = new StubEnrollmentRepository();
        var service = CreateService(offering, repo, currentEnrollmentCount: 1);

        var response = await service.EnrollAsync(studentId, new EnrollRequest(offering.Id));

        response.Should().NotBeNull();
        response!.Status.Should().Be(EnrollmentStatus.Waitlisted.ToString());
        repo.Enrollments.Should().ContainSingle(e => e.StudentProfileId == studentId && e.Status == EnrollmentStatus.Waitlisted);
    }

    [Fact]
    public async Task DropAsync_PromotesOldestWaitlistedEnrollment()
    {
        var offering = CreateOffering(maxEnrollment: 1, isOpen: true);
        var activeStudentId = Guid.NewGuid();
        var firstWaitlistedStudentId = Guid.NewGuid();
        var secondWaitlistedStudentId = Guid.NewGuid();

        var activeEnrollment = new Enrollment(activeStudentId, offering.Id);
        var firstWaitlisted = new Enrollment(firstWaitlistedStudentId, offering.Id);
        firstWaitlisted.Waitlist();
        Set(firstWaitlisted, nameof(Enrollment.EnrolledAt), DateTime.UtcNow.AddMinutes(-10));

        var secondWaitlisted = new Enrollment(secondWaitlistedStudentId, offering.Id);
        secondWaitlisted.Waitlist();
        Set(secondWaitlisted, nameof(Enrollment.EnrolledAt), DateTime.UtcNow.AddMinutes(-5));

        var repo = new StubEnrollmentRepository([activeEnrollment, firstWaitlisted, secondWaitlisted]);
        var service = CreateService(offering, repo, currentEnrollmentCount: 1);

        var dropped = await service.DropAsync(activeStudentId, offering.Id);

        dropped.Should().BeTrue();
        activeEnrollment.Status.Should().Be(EnrollmentStatus.Dropped);
        firstWaitlisted.Status.Should().Be(EnrollmentStatus.Active);
        secondWaitlisted.Status.Should().Be(EnrollmentStatus.Waitlisted);
    }

    private static EnrollmentService CreateService(CourseOffering offering, IEnrollmentRepository enrollmentRepo, int currentEnrollmentCount)
    {
        var courseRepo = new StubCourseRepository(offering, currentEnrollmentCount);
        return new EnrollmentService(
            enrollmentRepo,
            courseRepo,
            new StubSemesterRepository(),
            new StubAuditService(),
            new StubPrerequisiteRepository(),
            new StubResultRepository(),
            new StubTimetableRepository(),
            new StubGradingProfileRepository());
    }

    private static CourseOffering CreateOffering(int maxEnrollment, bool isOpen)
    {
        var department = new Department($"Dept-{Guid.NewGuid():N}"[..9], $"D{Guid.NewGuid():N}"[..5], InstitutionType.University);
        var course = new Course($"Course-{Guid.NewGuid():N}"[..10], $"CRS-{Guid.NewGuid():N}"[..8], 3, department.Id);
        var semester = new Semester("Fall 2026", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddMonths(4));
        var offering = new CourseOffering(course.Id, semester.Id, maxEnrollment);
        Set(offering, nameof(CourseOffering.Course), course);
        Set(offering, nameof(CourseOffering.Semester), semester);
        if (!isOpen)
        {
            offering.Close();
        }

        return offering;
    }

    private static void Set<T>(object target, string propertyName, T value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop?.SetValue(target, value);
    }
}

file sealed class StubEnrollmentRepository : IEnrollmentRepository
{
    private readonly List<Enrollment> _enrollments;

    public StubEnrollmentRepository(IEnumerable<Enrollment>? enrollments = null)
    {
        _enrollments = enrollments?.ToList() ?? [];
    }

    public IReadOnlyList<Enrollment> Enrollments => _enrollments;

    public Task<Enrollment?> GetAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
        => Task.FromResult(_enrollments.FirstOrDefault(e => e.StudentProfileId == studentProfileId && e.CourseOfferingId == courseOfferingId));

    public Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Enrollment>>(_enrollments.Where(e => e.StudentProfileId == studentProfileId).ToList());

    public Task<IReadOnlyList<Enrollment>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Enrollment>>(_enrollments.Where(e => e.CourseOfferingId == courseOfferingId && e.Status == EnrollmentStatus.Active).ToList());

    public Task<IReadOnlyList<Enrollment>> GetWaitlistedByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Enrollment>>(_enrollments.Where(e => e.CourseOfferingId == courseOfferingId && e.Status == EnrollmentStatus.Waitlisted).OrderBy(e => e.EnrolledAt).ToList());

    public Task<bool> IsEnrolledAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
        => Task.FromResult(_enrollments.Any(e => e.StudentProfileId == studentProfileId && e.CourseOfferingId == courseOfferingId && e.Status == EnrollmentStatus.Active));

    public Task AddAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        _enrollments.Add(enrollment);
        return Task.CompletedTask;
    }

    public void Update(Enrollment enrollment)
    {
        var index = _enrollments.FindIndex(e => e.Id == enrollment.Id);
        if (index >= 0)
            _enrollments[index] = enrollment;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_enrollments.FirstOrDefault(e => e.Id == id));
}

file sealed class StubCourseRepository : ICourseRepository
{
    private readonly CourseOffering _offering;
    private readonly int _currentEnrollmentCount;

    public StubCourseRepository(CourseOffering offering, int currentEnrollmentCount)
    {
        _offering = offering;
        _currentEnrollmentCount = currentEnrollmentCount;
    }

    public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Course?>(null);
    public Task<bool> CodeExistsAsync(string code, Guid departmentId, CancellationToken ct = default) => Task.FromResult(false);
    public Task AddAsync(Course course, CancellationToken ct = default) => Task.CompletedTask;
    public void Update(Course course) { }
    public Task<IReadOnlyList<Course>> GetAllAsync(Guid? departmentId = null, bool? hasSemesters = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Course>>([]);
    public Task<IReadOnlyList<CourseOffering>> GetAllOfferingsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CourseOffering>>([_offering]);
    public Task<IReadOnlyList<CourseOffering>> GetOfferingsBySemesterAsync(Guid semesterId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CourseOffering>>([_offering]);
    public Task<IReadOnlyList<CourseOffering>> GetOfferingsByDepartmentAsync(Guid departmentId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CourseOffering>>([_offering]);
    public Task<IReadOnlyList<CourseOffering>> GetOfferingsByFacultyAsync(Guid facultyUserId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CourseOffering>>([_offering]);
    public Task<CourseOffering?> GetOfferingByIdAsync(Guid offeringId, CancellationToken ct = default) => Task.FromResult<CourseOffering?>(offeringId == _offering.Id ? _offering : null);
    public Task<int> GetEnrollmentCountAsync(Guid offeringId, CancellationToken ct = default) => Task.FromResult(offeringId == _offering.Id ? _currentEnrollmentCount : 0);
    public Task AddOfferingAsync(CourseOffering offering, CancellationToken ct = default) => Task.CompletedTask;
    public void UpdateOffering(CourseOffering offering) { }
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}

file sealed class StubSemesterRepository : ISemesterRepository
{
    public Task<IReadOnlyList<Semester>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Semester>>([]);
    public Task<Semester?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Semester?>(null);
    public Task<Semester?> GetCurrentOpenAsync(CancellationToken ct = default) => Task.FromResult<Semester?>(null);
    public Task AddAsync(Semester semester, CancellationToken ct = default) => Task.CompletedTask;
    public void Update(Semester semester) { }
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}

file sealed class StubAuditService : IAuditService
{
    public Task LogAsync(AuditLog entry, CancellationToken ct = default) => Task.CompletedTask;
    public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        string? query = null,
        Guid? actorUserId = null,
        string? action = null,
        string? entityName = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        string? actorRole = null,
        string? severity = null,
        string? eventCategory = null,
        string? correlationId = null,
        CancellationToken ct = default)
        => Task.FromResult(((IReadOnlyList<AuditLog>)[], 0));
}

file sealed class StubPrerequisiteRepository : IPrerequisiteRepository
{
    public Task<IReadOnlyList<CoursePrerequisite>> GetByCourseIdAsync(Guid courseId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CoursePrerequisite>>([]);
    public Task<bool> ExistsAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct = default) => Task.FromResult(false);
    public Task AddAsync(CoursePrerequisite prerequisite, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}

file sealed class StubResultRepository : IResultRepository
{
    public Task<Result?> GetAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default) => Task.FromResult<Result?>(null);
    public Task<IReadOnlyList<Result>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Result>>([]);
    public Task<IReadOnlyList<Result>> GetPublishedByStudentAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Result>>([]);
    public Task<IReadOnlyList<Result>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Result>>([]);
    public Task<IReadOnlyList<Result>> GetByStudentAndOfferingAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Result>>([]);
    public Task<IReadOnlyList<Result>> GetByStudentAndSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Result>>([]);
    public Task<bool> ExistsAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default) => Task.FromResult(false);
    public Task AddAsync(Result result, CancellationToken ct = default) => Task.CompletedTask;
    public Task AddRangeAsync(IEnumerable<Result> results, CancellationToken ct = default) => Task.CompletedTask;
    public void Update(Result result) { }
    public Task<IReadOnlyList<ResultComponentRule>> GetActiveComponentRulesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ResultComponentRule>>([]);
    public Task<IReadOnlyList<ResultComponentRule>> GetActiveComponentRulesAsync(InstitutionType institutionType, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ResultComponentRule>>([]);
    public Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ResultComponentRule>>([]);
    public Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(InstitutionType institutionType, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ResultComponentRule>>([]);
    public Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GpaScaleRule>>([]);
    public Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(InstitutionType institutionType, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GpaScaleRule>>([]);
    public Task ReplaceCalculationRulesAsync(IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default) => Task.CompletedTask;
    public Task ReplaceCalculationRulesAsync(InstitutionType institutionType, IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default) => Task.CompletedTask;
    public Task<StudentProfile?> GetStudentProfileAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<StudentProfile?>(null);
    public Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Enrollment>>([]);
    public Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForStudentAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Enrollment>>([]);
    public Task<Guid?> GetSemesterIdForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
    public Task<int?> GetCreditHoursForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult<int?>(null);
    public Task<InstitutionType> GetInstitutionTypeForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult(InstitutionType.University);
    public void UpdateStudentProfile(StudentProfile studentProfile) { }
    public Task<IReadOnlyList<TranscriptExportLog>> GetExportLogsAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TranscriptExportLog>>([]);
    public Task AddExportLogAsync(TranscriptExportLog log, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> HasPassedCourseAsync(Guid studentProfileId, Guid courseId, decimal passThresholdPercentage, CancellationToken ct = default) => Task.FromResult(false);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}

file sealed class StubTimetableRepository : ITimetableRepository
{
    public Task<IList<Timetable>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default) => Task.FromResult<IList<Timetable>>([]);
    public Task<IList<Timetable>> GetPublishedByDepartmentAsync(Guid departmentId, CancellationToken ct = default) => Task.FromResult<IList<Timetable>>([]);
    public Task<Timetable?> GetByIdWithEntriesAsync(Guid timetableId, CancellationToken ct = default) => Task.FromResult<Timetable?>(null);
    public Task<Timetable?> GetByIdAsync(Guid timetableId, CancellationToken ct = default) => Task.FromResult<Timetable?>(null);
    public Task AddAsync(Timetable timetable, CancellationToken ct = default) => Task.CompletedTask;
    public Task AddEntryAsync(TimetableEntry entry, CancellationToken ct = default) => Task.CompletedTask;
    public void Update(Timetable timetable) { }
    public void UpdateEntry(TimetableEntry entry) { }
    public void RemoveEntry(TimetableEntry entry) { }
    public Task<TimetableEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default) => Task.FromResult<TimetableEntry?>(null);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<IList<TimetableEntry>> GetTeacherEntriesAsync(Guid facultyUserId, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct = default) => Task.FromResult<IList<TimetableEntry>>([]);
    public Task<IList<TimetableEntry>> GetEntriesByCourseOfferingAsync(Guid courseId, Guid semesterId, CancellationToken ct = default) => Task.FromResult<IList<TimetableEntry>>([]);
}

file sealed class StubGradingProfileRepository : IInstitutionGradingProfileRepository
{
    public Task<IReadOnlyList<InstitutionGradingProfile>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<InstitutionGradingProfile>>([]);
    public Task<InstitutionGradingProfile?> GetByTypeAsync(InstitutionType institutionType, CancellationToken ct = default) => Task.FromResult<InstitutionGradingProfile?>(null);
    public Task<InstitutionGradingProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<InstitutionGradingProfile?>(null);
    public Task AddAsync(InstitutionGradingProfile profile, CancellationToken ct = default) => Task.CompletedTask;
    public void Update(InstitutionGradingProfile profile) { }
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}