using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Distributed;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Application.DTOs.Reports;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Reporting;

/// <summary>
/// Generates report data and Excel exports for Phase 12 Reporting.
/// Lives in Infrastructure to access ClosedXML.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly IReportRepository _repo;
    private readonly IDistributedCache _distributedCache;

    private static readonly TimeSpan CatalogCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SummaryCacheTtl = TimeSpan.FromSeconds(45);

    public ReportService(IReportRepository repo, IDistributedCache distributedCache)
    {
        _repo = repo;
        _distributedCache = distributedCache;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Catalog ────────────────────────────────────────────────────────────────

    public async Task<ReportCatalogResponse> GetCatalogAsync(string roleName, CancellationToken ct = default)
    {
        var cacheKey = $"report_catalog:{roleName.Trim().ToLowerInvariant()}";
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedResponse = JsonSerializer.Deserialize<ReportCatalogResponse>(cached);
            if (cachedResponse is not null)
                return cachedResponse;
        }

        var defs = await _repo.GetCatalogForRoleAsync(roleName, ct);
        var items = defs.Select(d => new ReportCatalogItemResponse(
            d.Id,
            d.Key,
            d.Name,
            d.Purpose,
            d.IsActive,
            d.RoleAssignments.Select(ra => ra.RoleName).ToList()
        )).ToList();
        var response = new ReportCatalogResponse(items);
        await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CatalogCacheTtl
        }, ct);
        return response;
    }

    // ── Attendance Summary ─────────────────────────────────────────────────────

    public async Task<AttendanceSummaryReportResponse> GetAttendanceSummaryAsync(
        AttendanceSummaryRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildSummaryCacheKey(
            "attendance",
            request.SemesterId,
            request.CourseOfferingId,
            request.StudentProfileId,
            request.InstitutionType,
            null,
            null,
            BuildScopeKey(request.TenantId, request.CampusId));

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetAttendanceDataAsync(
                request.SemesterId, request.CourseOfferingId, request.StudentProfileId, request.InstitutionType, request.TenantId, request.CampusId, ct);

            var rows = raw.Select(r => new AttendanceSummaryRow(
                r.StudentProfileId, r.RegistrationNumber, r.StudentName,
                r.CourseOfferingId, r.CourseCode, r.CourseTitle,
                r.TotalSessions, r.AttendedSessions, r.AttendancePercentage)).ToList();

            return new AttendanceSummaryReportResponse(
                rows,
                rows.Select(r => r.StudentProfileId).Distinct().Count(),
                DateTime.UtcNow);
        }, ct);
    }

    // ── Result Summary ─────────────────────────────────────────────────────────

    public async Task<ResultSummaryReportResponse> GetResultSummaryAsync(
        ResultSummaryRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildSummaryCacheKey(
            "result",
            request.SemesterId,
            request.CourseOfferingId,
            request.StudentProfileId,
            request.InstitutionType,
            null,
            null,
            BuildScopeKey(request.TenantId, request.CampusId));

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetResultDataAsync(
                request.SemesterId, request.CourseOfferingId, request.StudentProfileId, request.InstitutionType, request.TenantId, request.CampusId, ct);

            var rows = raw.Select(r => new ResultSummaryRow(
                r.StudentProfileId, r.RegistrationNumber, r.StudentName,
                r.CourseCode, r.CourseTitle, r.ResultType,
                r.MarksObtained, r.MaxMarks, r.Percentage, r.PublishedAt)).ToList();

            return new ResultSummaryReportResponse(rows, rows.Count, DateTime.UtcNow);
        }, ct);
    }

    // ── Assignment Summary ───────────────────────────────────────────────────

    public async Task<AssignmentSummaryReportResponse> GetAssignmentSummaryAsync(
        AssignmentSummaryRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildSummaryCacheKey(
            "assignment",
            request.SemesterId,
            request.CourseOfferingId,
            request.StudentProfileId,
            request.InstitutionType,
            request.DepartmentId,
            null,
            BuildScopeKey(request.TenantId, request.CampusId));

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetAssignmentDataAsync(
                request.SemesterId, request.CourseOfferingId, request.StudentProfileId, request.InstitutionType, request.TenantId, request.CampusId, ct);

            if (request.DepartmentId.HasValue)
            {
                raw = raw.Where(r => r.DepartmentId == request.DepartmentId.Value)
                         .ToList();
            }

            var rows = raw.Select(r => new AssignmentSummaryRow(
                r.StudentProfileId, r.RegistrationNumber, r.StudentName,
                r.CourseCode, r.CourseTitle, r.AssignmentTitle,
                r.DueDate, r.SubmittedAt, r.Status, r.MarksAwarded)).ToList();

            return new AssignmentSummaryReportResponse(rows, rows.Count, DateTime.UtcNow);
        }, ct);
    }

    // ── Quiz Summary ─────────────────────────────────────────────────────────

    public async Task<QuizSummaryReportResponse> GetQuizSummaryAsync(
        QuizSummaryRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildSummaryCacheKey(
            "quiz",
            request.SemesterId,
            request.CourseOfferingId,
            request.StudentProfileId,
            request.InstitutionType,
            request.DepartmentId,
            null,
            BuildScopeKey(request.TenantId, request.CampusId));

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetQuizDataAsync(
                request.SemesterId, request.CourseOfferingId, request.StudentProfileId, request.InstitutionType, request.TenantId, request.CampusId, ct);

            if (request.DepartmentId.HasValue)
            {
                raw = raw.Where(r => r.DepartmentId == request.DepartmentId.Value)
                         .ToList();
            }

            var rows = raw.Select(r => new QuizSummaryRow(
                r.StudentProfileId, r.RegistrationNumber, r.StudentName,
                r.CourseCode, r.CourseTitle, r.QuizTitle,
                r.StartedAt, r.FinishedAt, r.AttemptStatus, r.TotalScore)).ToList();

            return new QuizSummaryReportResponse(rows, rows.Count, DateTime.UtcNow);
        }, ct);
    }

    // ── GPA Report ─────────────────────────────────────────────────────────────

    public async Task<GpaReportResponse> GetGpaReportAsync(
        GpaReportRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildSummaryCacheKey(
            "gpa",
            null,
            null,
            null,
            request.InstitutionType,
            request.DepartmentId,
            request.ProgramId,
            BuildScopeKey(request.TenantId, request.CampusId));

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetGpaDataAsync(request.DepartmentId, request.ProgramId, request.InstitutionType, request.TenantId, request.CampusId, ct);

            var rows = raw.Select(r => new Tabsan.EduSphere.Application.DTOs.Reports.GpaReportRow(
                r.StudentProfileId, r.RegistrationNumber, r.StudentName,
                r.ProgramName, r.DepartmentName,
                r.CurrentSemesterNumber, r.Cgpa, r.CurrentSemesterGpa)).ToList();

            var avgCgpa = rows.Any() ? Math.Round(rows.Average(r => r.Cgpa), 2) : 0m;
            return new GpaReportResponse(rows, avgCgpa, rows.Count, DateTime.UtcNow);
        }, ct);
    }

    // ── Enrollment Summary ─────────────────────────────────────────────────────

    public async Task<EnrollmentSummaryReportResponse> GetEnrollmentSummaryAsync(
        EnrollmentSummaryRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildSummaryCacheKey(
            "enrollment",
            request.SemesterId,
            null,
            null,
            request.InstitutionType,
            request.DepartmentId,
            null,
            BuildScopeKey(request.TenantId, request.CampusId));

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetEnrollmentDataAsync(request.SemesterId, request.DepartmentId, request.InstitutionType, request.TenantId, request.CampusId, ct);

            var rows = raw.Select(r => new EnrollmentSummaryRow(
                r.CourseOfferingId, r.CourseCode, r.CourseTitle, r.SemesterName,
                r.MaxEnrollment, r.EnrolledCount,
                r.MaxEnrollment - r.EnrolledCount)).ToList();

            return new EnrollmentSummaryReportResponse(rows, rows.Count, DateTime.UtcNow);
        }, ct);
    }

    // ── Semester Results ───────────────────────────────────────────────────────

    public async Task<SemesterResultsReportResponse> GetSemesterResultsAsync(
        SemesterResultsRequest request, CancellationToken ct = default)
    {
        var cacheKey = BuildSummaryCacheKey(
            "semester-results",
            request.SemesterId,
            null,
            null,
            request.InstitutionType,
            request.DepartmentId,
            null,
            BuildScopeKey(request.TenantId, request.CampusId));

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetSemesterResultDataAsync(request.SemesterId, request.DepartmentId, request.InstitutionType, request.TenantId, request.CampusId, ct);

            var rows = raw.Select(r => new SemesterResultsRow(
                r.StudentProfileId, r.RegistrationNumber, r.StudentName,
                r.CourseCode, r.CourseTitle, r.ResultType,
                r.MarksObtained, r.MaxMarks, r.Percentage)).ToList();

            return new SemesterResultsReportResponse(
                rows,
                rows.Select(r => r.StudentProfileId).Distinct().Count(),
                DateTime.UtcNow);
        }, ct);
    }

    private async Task<T> GetOrSetCachedSummaryAsync<T>(
        string cacheKey,
        Func<Task<T>> factory,
        CancellationToken ct) where T : class
    {
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedResponse = JsonSerializer.Deserialize<T>(cached);
            if (cachedResponse is not null)
            {
                return cachedResponse;
            }
        }

        var response = await factory();
        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = SummaryCacheTtl
            },
            ct);

        return response;
    }

    private static string BuildSummaryCacheKey(
        string reportType,
        Guid? semesterId,
        Guid? courseOfferingId,
        Guid? studentProfileId,
        int? institutionType,
        Guid? departmentId,
        Guid? programId,
        string? extra = null)
        => string.Join(
            ":",
            "report_summary",
            reportType,
            semesterId?.ToString("N") ?? "none",
            courseOfferingId?.ToString("N") ?? "none",
            studentProfileId?.ToString("N") ?? "none",
            institutionType?.ToString() ?? "none",
            departmentId?.ToString("N") ?? "none",
            programId?.ToString("N") ?? "none",
            extra ?? "none");

    private static string BuildScopeKey(Guid? tenantId, Guid? campusId)
        => $"{tenantId?.ToString("N") ?? "none"}:{campusId?.ToString("N") ?? "none"}";

    // ── Excel Exports ──────────────────────────────────────────────────────────

    public async Task<byte[]> ExportAttendanceSummaryExcelAsync(
        AttendanceSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetAttendanceSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Total Sessions", "Attended", "Attendance %" };
        var rows = report.Rows.Select(r => new object[]
        {
            r.RegistrationNumber, r.StudentName, r.CourseCode, r.CourseTitle,
            r.TotalSessions, r.AttendedSessions, r.AttendancePercentage
        }).ToList();
        return BuildExcelBytes("Attendance Summary", headers, rows);
    }

    public async Task<byte[]> ExportAttendanceSummaryCsvAsync(
        AttendanceSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetAttendanceSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Total Sessions", "Attended", "Attendance %" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.TotalSessions.ToString(),
            r.AttendedSessions.ToString(),
            r.AttendancePercentage.ToString("F2")
        });
        return BuildCsvBytes(headers, rows);
    }

    public async Task<byte[]> ExportAttendanceSummaryPdfAsync(
        AttendanceSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetAttendanceSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Total", "Attended", "Attendance %" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.TotalSessions.ToString(),
            r.AttendedSessions.ToString(),
            r.AttendancePercentage.ToString("F2")
        }).ToList();
        return BuildPdfBytes("Attendance Summary", headers, rows);
    }

    public async Task<byte[]> ExportResultSummaryExcelAsync(
        ResultSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetResultSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Component", "Marks", "Max Marks", "Percentage", "Published" };
        var rows = report.Rows.Select(r => new object[]
        {
            r.RegistrationNumber, r.StudentName, r.CourseCode, r.CourseTitle, r.ResultType,
            r.MarksObtained, r.MaxMarks, r.Percentage,
            r.PublishedAt.HasValue ? r.PublishedAt.Value.ToString("yyyy-MM-dd") : "-"
        }).ToList();
        return BuildExcelBytes("Result Summary", headers, rows);
    }

    public async Task<byte[]> ExportResultSummaryCsvAsync(
        ResultSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetResultSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Component", "Marks", "Max Marks", "Percentage", "Published" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.ResultType,
            r.MarksObtained.ToString("F2"),
            r.MaxMarks.ToString("F2"),
            r.Percentage.ToString("F2"),
            r.PublishedAt.HasValue ? r.PublishedAt.Value.ToString("yyyy-MM-dd") : "-"
        });
        return BuildCsvBytes(headers, rows);
    }

    public async Task<byte[]> ExportResultSummaryPdfAsync(
        ResultSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetResultSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course", "Title", "Type", "Marks", "Max", "%", "Published" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.ResultType,
            r.MarksObtained.ToString("F2"),
            r.MaxMarks.ToString("F2"),
            r.Percentage.ToString("F2"),
            r.PublishedAt.HasValue ? r.PublishedAt.Value.ToString("yyyy-MM-dd") : "-"
        }).ToList();
        return BuildPdfBytes("Result Summary", headers, rows);
    }

    public async Task<byte[]> ExportAssignmentSummaryExcelAsync(
        AssignmentSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetAssignmentSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Assignment", "Due Date", "Submitted At", "Status", "Marks Awarded" };
        var rows = report.Rows.Select(r => new object[]
        {
            r.RegistrationNumber, r.StudentName, r.CourseCode, r.CourseTitle,
            r.AssignmentTitle, r.DueDate.ToString("yyyy-MM-dd"), r.SubmittedAt.ToString("yyyy-MM-dd HH:mm"),
            r.Status, r.MarksAwarded.HasValue ? (object)r.MarksAwarded.Value : "-"
        }).ToList();
        return BuildExcelBytes("Assignment Summary", headers, rows);
    }

    public async Task<byte[]> ExportAssignmentSummaryCsvAsync(
        AssignmentSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetAssignmentSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Assignment", "Due Date", "Submitted At", "Status", "Marks Awarded" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.AssignmentTitle,
            r.DueDate.ToString("yyyy-MM-dd"),
            r.SubmittedAt.ToString("yyyy-MM-dd HH:mm"),
            r.Status,
            r.MarksAwarded.HasValue ? r.MarksAwarded.Value.ToString("F2") : "-"
        });
        return BuildCsvBytes(headers, rows);
    }

    public async Task<byte[]> ExportAssignmentSummaryPdfAsync(
        AssignmentSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetAssignmentSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course", "Title", "Assignment", "Due", "Submitted", "Status", "Marks" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.AssignmentTitle,
            r.DueDate.ToString("yyyy-MM-dd"),
            r.SubmittedAt.ToString("yyyy-MM-dd HH:mm"),
            r.Status,
            r.MarksAwarded.HasValue ? r.MarksAwarded.Value.ToString("F2") : "-"
        }).ToList();
        return BuildPdfBytes("Assignment Summary", headers, rows);
    }

    public async Task<byte[]> ExportQuizSummaryExcelAsync(
        QuizSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetQuizSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Quiz", "Started At", "Finished At", "Attempt Status", "Total Score" };
        var rows = report.Rows.Select(r => new object[]
        {
            r.RegistrationNumber, r.StudentName, r.CourseCode, r.CourseTitle,
            r.QuizTitle, r.StartedAt.ToString("yyyy-MM-dd HH:mm"),
            r.FinishedAt.HasValue ? r.FinishedAt.Value.ToString("yyyy-MM-dd HH:mm") : "-",
            r.AttemptStatus, r.TotalScore.HasValue ? (object)r.TotalScore.Value : "-"
        }).ToList();
        return BuildExcelBytes("Quiz Summary", headers, rows);
    }

    public async Task<byte[]> ExportQuizSummaryCsvAsync(
        QuizSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetQuizSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course Code", "Course Title", "Quiz", "Started At", "Finished At", "Attempt Status", "Total Score" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.QuizTitle,
            r.StartedAt.ToString("yyyy-MM-dd HH:mm"),
            r.FinishedAt.HasValue ? r.FinishedAt.Value.ToString("yyyy-MM-dd HH:mm") : "-",
            r.AttemptStatus,
            r.TotalScore.HasValue ? r.TotalScore.Value.ToString("F2") : "-"
        });
        return BuildCsvBytes(headers, rows);
    }

    public async Task<byte[]> ExportQuizSummaryPdfAsync(
        QuizSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetQuizSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Course", "Title", "Quiz", "Started", "Finished", "Status", "Score" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.CourseCode,
            r.CourseTitle,
            r.QuizTitle,
            r.StartedAt.ToString("yyyy-MM-dd HH:mm"),
            r.FinishedAt.HasValue ? r.FinishedAt.Value.ToString("yyyy-MM-dd HH:mm") : "-",
            r.AttemptStatus,
            r.TotalScore.HasValue ? r.TotalScore.Value.ToString("F2") : "-"
        }).ToList();
        return BuildPdfBytes("Quiz Summary", headers, rows);
    }

    public async Task<byte[]> ExportGpaReportExcelAsync(
        GpaReportRequest request, CancellationToken ct = default)
    {
        var report = await GetGpaReportAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Program", "Department", "Semester", "CGPA", "Semester GPA" };
        var rows = report.Rows.Select(r => new object[]
        {
            r.RegistrationNumber, r.StudentName, r.ProgramName, r.DepartmentName,
            r.CurrentSemester, r.Cgpa, r.CurrentSemesterGpa
        }).ToList();
        return BuildExcelBytes("GPA Report", headers, rows);
    }

    public async Task<byte[]> ExportPaymentSummaryExcelAsync(
        PaymentSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetPaymentSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Department", "Course", "Level", "Status", "Amount", "Due Date", "Paid Date" };
        var rows = report.Rows.Select(r => new object[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.DepartmentName,
            string.IsNullOrWhiteSpace(r.CourseCode) ? "-" : $"{r.CourseCode} - {r.CourseTitle}",
            r.LevelLabel,
            r.Status,
            r.Amount,
            r.DueDate.ToString("yyyy-MM-dd"),
            r.PaidDate?.ToString("yyyy-MM-dd") ?? "-"
        }).ToList();

        return BuildExcelBytes("Payment Summary", headers, rows);
    }

    public async Task<byte[]> ExportPaymentSummaryCsvAsync(
        PaymentSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetPaymentSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Department", "Course", "Level", "Status", "Amount", "Due Date", "Paid Date" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.DepartmentName,
            string.IsNullOrWhiteSpace(r.CourseCode) ? "-" : $"{r.CourseCode} - {r.CourseTitle}",
            r.LevelLabel,
            r.Status,
            r.Amount.ToString("F2"),
            r.DueDate.ToString("yyyy-MM-dd"),
            r.PaidDate?.ToString("yyyy-MM-dd") ?? "-"
        });

        return BuildCsvBytes(headers, rows);
    }

    public async Task<byte[]> ExportPaymentSummaryPdfAsync(
        PaymentSummaryRequest request, CancellationToken ct = default)
    {
        var report = await GetPaymentSummaryAsync(request, ct);
        var headers = new[] { "Reg No", "Student", "Dept", "Course", "Level", "Status", "Amount", "Due", "Paid" };
        var rows = report.Rows.Select(r => new[]
        {
            r.RegistrationNumber,
            r.StudentName,
            r.DepartmentName,
            string.IsNullOrWhiteSpace(r.CourseCode) ? "-" : r.CourseCode,
            r.LevelLabel,
            r.Status,
            r.Amount.ToString("F2"),
            r.DueDate.ToString("yyyy-MM-dd"),
            r.PaidDate?.ToString("yyyy-MM-dd") ?? "-"
        }).ToList();

        return BuildPdfBytes("Payment Summary", headers, rows);
    }

    // ── Stage 4.2: Student Transcript ─────────────────────────────────────────

    public async Task<TranscriptReportResponse?> GetStudentTranscriptAsync(
        TranscriptRequest request, CancellationToken ct = default)
    {
        var raw = await _repo.GetTranscriptDataAsync(request.StudentProfileId, ct);
        if (raw is null) return null;

        var rows = raw.Rows.Select(r => new TranscriptRow(
            r.CourseCode, r.CourseTitle, r.SemesterName, r.ResultType,
            r.MarksObtained, r.MaxMarks, r.Percentage, r.GradePoint, r.PublishedAt)).ToList();

        return new TranscriptReportResponse(
            raw.StudentProfileId, raw.RegistrationNumber, raw.StudentName,
            raw.ProgramName, raw.DepartmentName, raw.Cgpa,
            rows, DateTime.UtcNow);
    }

    public async Task<byte[]> ExportTranscriptExcelAsync(
        TranscriptRequest request, CancellationToken ct = default)
    {
        var report = await GetStudentTranscriptAsync(request, ct);
        if (report is null) return Array.Empty<byte>();

        var headers = new[] { "Course Code", "Course Title", "Semester", "Component", "Marks", "Max Marks", "%", "Grade Point", "Published" };
        var rows = report.Rows.Select(r => new object[]
        {
            r.CourseCode, r.CourseTitle, r.SemesterName, r.ResultType,
            r.MarksObtained, r.MaxMarks, r.Percentage,
            r.GradePoint.HasValue ? (object)r.GradePoint.Value : "-",
            r.PublishedAt.HasValue ? r.PublishedAt.Value.ToString("yyyy-MM-dd") : "-"
        }).ToList();
        return BuildExcelBytes($"Transcript_{report.RegistrationNumber}", headers, rows);
    }

    // ── Stage 4.2: Low Attendance Warning ─────────────────────────────────────

    public async Task<LowAttendanceReportResponse> GetLowAttendanceWarningAsync(
        LowAttendanceRequest request, CancellationToken ct = default)
    {
        var raw = await _repo.GetLowAttendanceDataAsync(
            request.ThresholdPercent, request.DepartmentId, request.CourseOfferingId, request.InstitutionType, request.TenantId, request.CampusId, ct);

        var rows = raw.Select(r => new LowAttendanceRow(
            r.StudentProfileId, r.RegistrationNumber, r.StudentName,
            r.CourseCode, r.CourseTitle, r.SemesterName, r.DepartmentName,
            r.TotalSessions, r.AttendedSessions, r.AttendancePercentage)).ToList();

        return new LowAttendanceReportResponse(rows, request.ThresholdPercent, rows.Select(r => r.StudentProfileId).Distinct().Count(), DateTime.UtcNow);
    }

    // ── Stage 4.2: FYP Status Report ──────────────────────────────────────────

    public async Task<FypStatusReportResponse> GetFypStatusReportAsync(
        FypStatusRequest request, CancellationToken ct = default)
    {
        var raw = await _repo.GetFypStatusDataAsync(request.DepartmentId, request.Status, request.InstitutionType, request.TenantId, request.CampusId, ct);

        var rows = raw.Select(r => new FypStatusRow(
            r.ProjectId, r.Title, r.StudentName, r.RegistrationNumber,
            r.DepartmentName, r.SupervisorName, r.Status, r.ProposedAt, r.MeetingCount)).ToList();

        return new FypStatusReportResponse(rows, rows.Count, DateTime.UtcNow);
    }

    public async Task<PaymentSummaryReportResponse> GetPaymentSummaryAsync(
        PaymentSummaryRequest request, CancellationToken ct = default)
    {
        var cacheScope = $"y:{request.Year?.ToString() ?? "none"}|m:{request.Month?.ToString() ?? "none"}|s:{request.SemesterId?.ToString("N") ?? "none"}|d:{request.DepartmentId?.ToString("N") ?? "none"}|c:{request.CourseId?.ToString("N") ?? "none"}|l:{request.LevelNumber?.ToString() ?? "none"}|t:{request.TenantId?.ToString("N") ?? "none"}|cp:{request.CampusId?.ToString("N") ?? "none"}";
        var cacheKey = BuildSummaryCacheKey(
            "payment-summary",
            request.SemesterId,
            null,
            null,
            request.InstitutionType,
            request.DepartmentId,
            request.CourseId,
            cacheScope);

        return await GetOrSetCachedSummaryAsync(cacheKey, async () =>
        {
            var raw = await _repo.GetPaymentSummaryDataAsync(
                request.Year,
                request.Month,
                request.SemesterId,
                request.DepartmentId,
                request.CourseId,
                request.LevelNumber,
                request.InstitutionType,
                request.TenantId,
                request.CampusId,
                ct);

            var rows = raw.Select(r => new PaymentSummaryRow(
                r.ReceiptId,
                r.StudentProfileId,
                r.RegistrationNumber,
                r.StudentName,
                r.Amount,
                r.Status,
                r.DueDate,
                r.PaidDate,
                r.DepartmentName,
                r.CourseCode,
                r.CourseTitle,
                r.SemesterName,
                r.CurrentLevel,
                r.LevelLabel)).ToList();

            var totalAmount = rows.Sum(r => r.Amount);
            var totalPaid = rows
                .Where(r => string.Equals(r.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.Amount);

            return new PaymentSummaryReportResponse(
                rows,
                totalAmount,
                totalPaid,
                totalAmount - totalPaid,
                rows.Count,
                DateTime.UtcNow);
        }, ct);
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private static byte[] BuildExcelBytes(string sheetName, string[] headers, IList<object[]> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var colIndex = 0; colIndex < row.Length; colIndex++)
                ws.Cell(rowIndex + 2, colIndex + 1).Value = XLCellValue.FromObject(row[colIndex]);
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] BuildCsvBytes(string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvCell)));

        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(EscapeCsvCell)));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsvCell(string? value)
    {
        var cell = value ?? string.Empty;
        if (cell.Contains('"') || cell.Contains(',') || cell.Contains('\n') || cell.Contains('\r'))
            return $"\"{cell.Replace("\"", "\"\"")}\"";

        return cell;
    }

    private static byte[] BuildPdfBytes(string title, string[] headers, IList<string[]> rows)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .Text($"{title} - Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                    .SemiBold()
                    .FontSize(12);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < headers.Length; i++)
                            columns.RelativeColumn();
                    });

                    foreach (var header in headers)
                    {
                        table.Cell().Background(Colors.Grey.Lighten2).Padding(4)
                            .Text(header).SemiBold();
                    }

                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(3).Text(cell ?? "-");
                        }
                    }
                });
            });
        });

        return doc.GeneratePdf();
    }
}
