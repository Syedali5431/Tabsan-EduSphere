using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Tabsan.EduSphere.Web.Controllers;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.UnitTests;

public class PortalAttendanceCsvImportTests
{
    private static readonly Guid OfferingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000111");
    private static readonly Guid CampusId = Guid.Parse("00000000-0000-0000-0000-000000000222");
    private static readonly Guid DepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CourseId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const string SemesterName = "Semester 1";

    [Fact]
    public async Task BulkMarkAttendance_StudentOutsideRoster_DoesNotCallBulkMark()
    {
        var inRoster = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var outsideRoster = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = inRoster, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);

        var result = await sut.BulkMarkAttendance(
            OfferingId,
            date: DateTime.UtcNow.Date,
            studentIds: [outsideRoster],
            statuses: ["Present"],
            tenantId: TenantId,
            campusId: CampusId,
            departmentId: DepartmentId,
            courseId: CourseId,
            semesterName: SemesterName,
            entryPoint: "EnterAttendance",
            ct: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.BulkCalls.Should().BeEmpty();
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("outside the selected offering roster");
    }

    [Fact]
    public async Task CorrectAttendance_StudentOutsideRoster_DoesNotCallCorrectAttendance()
    {
        var inRoster = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var outsideRoster = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = inRoster, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);

        var result = await sut.CorrectAttendance(
            studentProfileId: outsideRoster,
            offeringId: OfferingId,
            date: DateTime.UtcNow.Date,
            newStatus: "Present",
            remarks: "test",
            tenantId: TenantId,
            campusId: CampusId,
            departmentId: DepartmentId,
            courseId: CourseId,
            semesterName: SemesterName,
            entryPoint: "EnterAttendance",
            ct: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.CorrectCalls.Should().BeEmpty();
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("not in the offering roster");
    }

    [Fact]
    public async Task ImportAttendanceCsv_ValidRows_CallsBulkMarkAndRedirectsToEnterAttendance()
    {
        var studentOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var studentTwo = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster:
            [
                new EnrollmentRosterItem { Id = studentOne, StudentName = "Student One" },
                new EnrollmentRosterItem { Id = studentTwo, StudentName = "Student Two" }
            ],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{studentOne},Student One,2026-05-28,true",
            $"{studentTwo},Student Two,2026-05-28,false"
        ]);

        var result = await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            "EnterAttendance",
            CreateCsvFile(csv),
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("EnterAttendance");
        proxy.BulkCalls.Should().HaveCount(1);
        proxy.BulkCalls[0].OfferingId.Should().Be(OfferingId);
        proxy.BulkCalls[0].Entries.Should().Contain(x => x.StudentProfileId == studentOne && x.Status == "Present");
        proxy.BulkCalls[0].Entries.Should().Contain(x => x.StudentProfileId == studentTwo && x.Status == "Absent");
    }

    [Fact]
    public async Task ImportAttendanceCsv_MissingFilterContext_DoesNotCallBulkMark()
    {
        var studentOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = studentOne, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{studentOne},Student One,2026-05-28,true"
        ]);

        var result = await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            departmentId: null,
            courseId: CourseId,
            semesterName: SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.BulkCalls.Should().BeEmpty();
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("Select department, course, and semester/class");
    }

    [Fact]
    public async Task ImportAttendanceCsv_InvalidHeader_DoesNotCallBulkMark()
    {
        var studentOne = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = studentOne, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,Name,Date,Present",
            $"{studentOne},Student One,2026-05-28,true"
        ]);

        var result = await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.BulkCalls.Should().BeEmpty();
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("header does not match");
    }

    [Fact]
    public async Task ImportAttendanceCsv_UnknownStudentId_DoesNotCallBulkMark()
    {
        var inRoster = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var unknownStudent = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = inRoster, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{unknownStudent},Unknown Student,2026-05-28,true"
        ]);

        var result = await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.BulkCalls.Should().BeEmpty();
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("strict mode");
        sut.TempData["PortalMessageDetails"]?.ToString().Should().ContainEquivalentOf("does not belong to the selected offering roster");
    }

    [Fact]
    public async Task ImportAttendanceCsv_InvalidDateOrPresent_DoesNotCallBulkMark()
    {
        var studentOne = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = studentOne, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{studentOne},Student One,not-a-date,maybe"
        ]);

        var result = await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.BulkCalls.Should().BeEmpty();
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("validation failed");
    }

    [Fact]
    public async Task ImportAttendanceCsv_DuplicateStudentAndDate_DoesNotCallBulkMark()
    {
        var studentOne = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = studentOne, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{studentOne},Student One,2026-05-28,true",
            $"{studentOne},Student One,2026-05-28,false"
        ]);

        var result = await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.BulkCalls.Should().BeEmpty();
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("strict mode");
        sut.TempData["PortalMessageDetails"]?.ToString().Should().ContainEquivalentOf("duplicate");
    }

    [Fact]
    public async Task ImportAttendanceCsv_NonStrictMode_ImportsValidRowsAndReturnsWarnings()
    {
        var inRoster = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var unknownStudent = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = inRoster, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{inRoster},Student One,2026-05-28,true",
            $"{unknownStudent},Unknown Student,2026-05-28,true"
        ]);

        var result = await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None,
            strictMode: false);

        result.Should().BeOfType<RedirectToActionResult>();
        proxy.BulkCalls.Should().HaveCount(1);
        proxy.BulkCalls[0].Entries.Should().ContainSingle(x => x.StudentProfileId == inRoster && x.Status == "Present");
        sut.TempData["PortalMessage"]?.ToString().Should().ContainEquivalentOf("completed with warnings");
        sut.TempData["PortalMessageDetails"]?.ToString().Should().ContainEquivalentOf("does not belong to the selected offering roster");
    }

    [Fact]
    public async Task ImportAttendanceCsv_StrictModeValidationFailure_WritesAuditTrail()
    {
        var unknownStudent = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var (api, _) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{unknownStudent},Unknown Student,2026-05-28,true"
        ]);

        await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None,
            strictMode: true);

        sut.TempData["PortalImportAudit"]?.ToString().Should().ContainEquivalentOf("strictMode=true");
        sut.TempData["PortalImportAudit"]?.ToString().Should().ContainEquivalentOf("importedRows=0");
        sut.TempData["PortalImportAudit"]?.ToString().Should().ContainEquivalentOf("skippedRows=1");
    }

    [Fact]
    public async Task ImportAttendanceCsv_NonStrictModeSuccessWithWarnings_WritesAuditTrail()
    {
        var inRoster = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var unknownStudent = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var (api, proxy) = CreateApiClient(
            isConnected: true,
            identity: CreateFacultyIdentity(),
            roster: [new EnrollmentRosterItem { Id = inRoster, StudentName = "Student One" }],
            offerings: [BuildOffering()]);

        var sut = CreateSut(api);
        var csv = string.Join('\n',
        [
            "StudentId,StudentName,Date,Present",
            $"{inRoster},Student One,2026-05-28,true",
            $"{unknownStudent},Unknown Student,2026-05-28,true"
        ]);

        await sut.ImportAttendanceCsv(
            OfferingId,
            TenantId,
            CampusId,
            DepartmentId,
            CourseId,
            SemesterName,
            entryPoint: "EnterAttendance",
            csvFile: CreateCsvFile(csv),
            ct: CancellationToken.None,
            strictMode: false);

        proxy.BulkCalls.Should().HaveCount(1);
        sut.TempData["PortalImportAudit"]?.ToString().Should().ContainEquivalentOf("strictMode=false");
        sut.TempData["PortalImportAudit"]?.ToString().Should().ContainEquivalentOf("importedRows=1");
        sut.TempData["PortalImportAudit"]?.ToString().Should().ContainEquivalentOf("skippedRows=1");
    }

    private static SessionIdentity CreateFacultyIdentity() => new()
    {
        Roles = ["Faculty"],
        TenantId = TenantId,
        CampusId = CampusId
    };

    private static CourseOfferingItem BuildOffering() => new()
    {
        Id = OfferingId,
        CourseId = CourseId,
        DepartmentId = DepartmentId,
        SemesterName = SemesterName,
        TenantId = TenantId,
        CampusId = CampusId,
        CourseCode = "CS101",
        CourseTitle = "Introduction to Computing"
    };

    private static PortalController CreateSut(IEduApiClient api)
    {
        var controller = new PortalController(api, new AttendanceTestWebHostEnvironment(), NullLogger<PortalController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new TestTempDataProvider());
        return controller;
    }

    private static (IEduApiClient Client, AttendanceCsvEduApiClientProxy Proxy) CreateApiClient(
        bool isConnected,
        SessionIdentity identity,
        List<EnrollmentRosterItem> roster,
        List<CourseOfferingItem> offerings)
    {
        var api = DispatchProxy.Create<IEduApiClient, AttendanceCsvEduApiClientProxy>();
        var proxy = (AttendanceCsvEduApiClientProxy)(object)api;
        proxy.IsConnectedValue = isConnected;
        proxy.Identity = identity;
        proxy.Roster = roster;
        proxy.Offerings = offerings;
        return (api, proxy);
    }

    private static IFormFile CreateCsvFile(string csv)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "csvFile", "attendance.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }
}

public class AttendanceCsvEduApiClientProxy : DispatchProxy
{
    public bool IsConnectedValue { get; set; }
    public SessionIdentity Identity { get; set; } = new();
    public List<EnrollmentRosterItem> Roster { get; set; } = [];
    public List<CourseOfferingItem> Offerings { get; set; } = [];
    public List<BulkCallRecord> BulkCalls { get; } = [];
    public List<CorrectCallRecord> CorrectCalls { get; } = [];

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
            throw new NotSupportedException();

        return targetMethod.Name switch
        {
            nameof(IEduApiClient.IsConnected) => IsConnectedValue,
            nameof(IEduApiClient.GetSessionIdentity) => Identity,
            nameof(IEduApiClient.GetEnrollmentRosterAsync) => Task.FromResult(Roster),
            nameof(IEduApiClient.GetCourseOfferingsAsync) => Task.FromResult(Offerings),
            nameof(IEduApiClient.BulkMarkAttendanceAsync) => HandleBulkMarkAttendanceAsync(args),
            nameof(IEduApiClient.CorrectAttendanceAsync) => HandleCorrectAttendanceAsync(args),
            _ => throw new NotSupportedException($"Method '{targetMethod.Name}' is not configured for this test proxy.")
        };
    }

    private Task HandleBulkMarkAttendanceAsync(object?[]? args)
    {
        if (args is null || args.Length < 3)
            throw new InvalidOperationException("Unexpected BulkMarkAttendanceAsync invocation arguments.");

        var offeringId = (Guid)args[0]!;
        var date = (DateTime)args[1]!;
        var entries = ((IEnumerable<(Guid StudentProfileId, string Status)>)args[2]!).ToList();

        BulkCalls.Add(new BulkCallRecord(offeringId, date, entries));
        return Task.CompletedTask;
    }

    private Task HandleCorrectAttendanceAsync(object?[]? args)
    {
        if (args is null || args.Length < 4)
            throw new InvalidOperationException("Unexpected CorrectAttendanceAsync invocation arguments.");

        var studentProfileId = (Guid)args[0]!;
        var offeringId = (Guid)args[1]!;
        var date = (DateTime)args[2]!;
        var newStatus = (string)args[3]!;

        CorrectCalls.Add(new CorrectCallRecord(studentProfileId, offeringId, date, newStatus));
        return Task.CompletedTask;
    }
}

public record BulkCallRecord(Guid OfferingId, DateTime Date, List<(Guid StudentProfileId, string Status)> Entries);
public record CorrectCallRecord(Guid StudentProfileId, Guid OfferingId, DateTime Date, string Status);

file sealed class AttendanceTestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Tabsan.EduSphere.Web.Tests";
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

file sealed class TestTempDataProvider : ITempDataProvider
{
    private IDictionary<string, object> _data = new Dictionary<string, object>();

    public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        _data = new Dictionary<string, object>(values);
    }
}
