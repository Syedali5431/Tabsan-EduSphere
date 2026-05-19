using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using Tabsan.EduSphere.Application.DTOs.Analytics;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Analytics;

/// <summary>
/// Computes analytics reports from the database and exports them to PDF / Excel.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _distributedCache;

    // Final-Touches Phase 34 Stage 4.1 — short-TTL distributed cache policy for expensive analytics read endpoints.
    private static readonly TimeSpan AnalyticsCacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>Initialises the service with the application DbContext.</summary>
    public AnalyticsService(ApplicationDbContext db, IDistributedCache distributedCache)
    {
        _db = db;
        _distributedCache = distributedCache;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // Performance report
    /// <summary>Returns a performance report for a department, or all departments if null.</summary>
    public async Task<DepartmentPerformanceReport?> GetPerformanceReportAsync(
        Guid? departmentId,
        int? institutionType = null,
        CancellationToken ct = default,
        Guid? courseId = null,
        Guid? semesterId = null)
    {
        // Final-Touches Phase 34 Stage 4.1 — cache expensive analytics report reads in shared distributed cache.
        var cacheKey = BuildAnalyticsCacheKey("performance", departmentId, institutionType, courseId, semesterId);
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedReport = JsonSerializer.Deserialize<DepartmentPerformanceReport>(cached);
            if (cachedReport is not null)
            {
                return cachedReport;
            }
        }

        var deptName = await ResolveDeptNameAsync(departmentId, institutionType, ct);
        var query =
            from sp in _db.StudentProfiles
            join u  in _db.Users           on sp.UserId           equals u.Id
            join e  in _db.Enrollments     on sp.Id               equals e.StudentProfileId
            join co in _db.CourseOfferings on e.CourseOfferingId  equals co.Id
            join c  in _db.Courses         on co.CourseId         equals c.Id
            join d  in _db.Departments     on c.DepartmentId      equals d.Id
            where (departmentId == null || c.DepartmentId == departmentId)
               && (!institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
                    && (!courseId.HasValue || c.Id == courseId.Value)
                    && (!semesterId.HasValue || co.SemesterId == semesterId.Value)
            select new { sp.Id, sp.RegistrationNumber, DisplayName = u.Username, sp.CurrentSemesterNumber, OfferingId = co.Id };

        var raw = await query.Distinct().ToListAsync(ct);
        if (!raw.Any()) return null;

        var students = new List<StudentPerformanceRow>();
        foreach (var g in raw.GroupBy(r => r.Id))
        {
            var first = g.First();
            var oids  = g.Select(x => x.OfferingId).Distinct().ToList();
            var results = await _db.Results
                .Where(r => r.StudentProfileId == first.Id && oids.Contains(r.CourseOfferingId))
                .ToListAsync(ct);
            var subs = await _db.AssignmentSubmissions.CountAsync(s => s.StudentProfileId == first.Id, ct);
            var avg  = results.Any() ? results.Average(r => (double)r.MarksObtained) : 0.0;
            students.Add(new StudentPerformanceRow(first.Id, first.RegistrationNumber, first.DisplayName,
                deptName, first.CurrentSemesterNumber, Math.Round(avg, 2), subs, subs));
        }
        students = students.OrderByDescending(s => s.AverageMarks).ToList();
        var report = new DepartmentPerformanceReport(departmentId ?? Guid.Empty, deptName,
            students.Any() ? Math.Round(students.Average(s => s.AverageMarks), 2) : 0,
            students.Count, students);

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(report),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AnalyticsCacheTtl
            },
            ct);

        return report;
    }

    // Attendance report
    /// <summary>Returns an attendance summary for a department, or all departments if null.</summary>
    public async Task<DepartmentAttendanceReport?> GetAttendanceReportAsync(
        Guid? departmentId,
        int? institutionType = null,
        CancellationToken ct = default,
        Guid? courseId = null,
        Guid? semesterId = null)
    {
        // Final-Touches Phase 34 Stage 4.1 — cache expensive analytics report reads in shared distributed cache.
        var cacheKey = BuildAnalyticsCacheKey("attendance", departmentId, institutionType, courseId, semesterId);
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedReport = JsonSerializer.Deserialize<DepartmentAttendanceReport>(cached);
            if (cachedReport is not null)
            {
                return cachedReport;
            }
        }

        var deptName = await ResolveDeptNameAsync(departmentId, institutionType, ct);
        var rows = await (
            from ar in _db.AttendanceRecords
            join sp in _db.StudentProfiles on ar.StudentProfileId equals sp.Id
            join u  in _db.Users           on sp.UserId           equals u.Id
            join co in _db.CourseOfferings on ar.CourseOfferingId equals co.Id
            join c  in _db.Courses         on co.CourseId         equals c.Id
            join d  in _db.Departments     on c.DepartmentId      equals d.Id
            where (departmentId == null || c.DepartmentId == departmentId)
               && (!institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
                    && (!courseId.HasValue || c.Id == courseId.Value)
                    && (!semesterId.HasValue || co.SemesterId == semesterId.Value)
            select new { sp.Id, DisplayName = u.Username, CourseName = c.Title, ar.Status }
        ).ToListAsync(ct);

        if (!rows.Any()) return null;

        var attendanceRows = rows
            .GroupBy(r => new { r.Id, r.DisplayName, r.CourseName })
            .Select(g =>
            {
                var total    = g.Count();
                var attended = g.Count(x => x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late);
                return new AttendanceRow(g.Key.Id, g.Key.DisplayName, g.Key.CourseName, total, attended,
                    total > 0 ? Math.Round((double)attended / total * 100, 1) : 0);
            })
            .OrderBy(r => r.AttendancePercentage).ToList();

        var overall = attendanceRows.Any() ? Math.Round(attendanceRows.Average(r => r.AttendancePercentage), 1) : 0;
        var report = new DepartmentAttendanceReport(departmentId ?? Guid.Empty, deptName, overall, attendanceRows);

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(report),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AnalyticsCacheTtl
            },
            ct);

        return report;
    }

    // Assignment stats
    /// <summary>Returns assignment statistics for a department, or all if null.</summary>
    public async Task<AssignmentStatsReport?> GetAssignmentStatsAsync(
        Guid? departmentId,
        int? institutionType = null,
        CancellationToken ct = default,
        Guid? courseId = null,
        Guid? semesterId = null)
    {
        // Final-Touches Phase 34 Stage 4.1 — cache expensive analytics report reads in shared distributed cache.
        var cacheKey = BuildAnalyticsCacheKey("assignments", departmentId, institutionType, courseId, semesterId);
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedReport = JsonSerializer.Deserialize<AssignmentStatsReport>(cached);
            if (cachedReport is not null)
            {
                return cachedReport;
            }
        }

        var deptName = await ResolveDeptNameAsync(departmentId, institutionType, ct);
        var assignments = await (
            from a  in _db.Assignments
            join co in _db.CourseOfferings on a.CourseOfferingId equals co.Id
            join c  in _db.Courses         on co.CourseId         equals c.Id
            join d  in _db.Departments     on c.DepartmentId      equals d.Id
            where (departmentId == null || c.DepartmentId == departmentId)
               && (!institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
                    && (!courseId.HasValue || c.Id == courseId.Value)
                    && (!semesterId.HasValue || co.SemesterId == semesterId.Value)
            select new { a.Id, a.Title, CourseName = c.Title, OfferingId = co.Id }
        ).ToListAsync(ct);

        if (!assignments.Any()) return null;

        var stats = new List<AssignmentStatsRow>();
        foreach (var a in assignments)
        {
            var subs     = await _db.AssignmentSubmissions.Where(s => s.AssignmentId == a.Id).ToListAsync(ct);
            var graded   = subs.Count(s => s.Status == SubmissionStatus.Graded);
            var avg      = graded > 0 ? subs.Where(s => s.Status == SubmissionStatus.Graded && s.MarksAwarded.HasValue)
                               .Average(s => (double)s.MarksAwarded!.Value) : 0.0;
            var enrolled = await _db.Enrollments.CountAsync(e => e.CourseOfferingId == a.OfferingId, ct);
            stats.Add(new AssignmentStatsRow(a.Id, a.Title, a.CourseName, enrolled, subs.Count, graded, Math.Round(avg, 2)));
        }
        var report = new AssignmentStatsReport(departmentId ?? Guid.Empty, deptName, stats);

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(report),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AnalyticsCacheTtl
            },
            ct);

        return report;
    }

    // Quiz stats
    /// <summary>Returns quiz statistics for a department, or all if null.</summary>
    public async Task<QuizStatsReport?> GetQuizStatsAsync(
        Guid? departmentId,
        int? institutionType = null,
        CancellationToken ct = default)
    {
        // Final-Touches Phase 34 Stage 4.1 — cache expensive analytics report reads in shared distributed cache.
        var cacheKey = BuildAnalyticsCacheKey("quizzes", departmentId, institutionType);
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedReport = JsonSerializer.Deserialize<QuizStatsReport>(cached);
            if (cachedReport is not null)
            {
                return cachedReport;
            }
        }

        var deptName = await ResolveDeptNameAsync(departmentId, institutionType, ct);
        var quizzes = await (
            from q  in _db.Quizzes.IgnoreQueryFilters()
            join co in _db.CourseOfferings on q.CourseOfferingId equals co.Id
            join c  in _db.Courses         on co.CourseId         equals c.Id
            join d  in _db.Departments     on c.DepartmentId      equals d.Id
            where (departmentId == null || c.DepartmentId == departmentId)
               && (!institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
            select new { q.Id, q.Title, CourseName = c.Title }
        ).ToListAsync(ct);

        if (!quizzes.Any()) return null;

        var stats = new List<QuizStatsRow>();
        foreach (var q in quizzes)
        {
            var attempts  = await _db.QuizAttempts.Where(a => a.QuizId == q.Id).ToListAsync(ct);
            var submitted = attempts.Where(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.TimedOut).ToList();
            var avg  = submitted.Any() ? submitted.Average(a => (double)(a.TotalScore ?? 0)) : 0.0;
            var high = attempts.Any()  ? (double)(attempts.Max(a => a.TotalScore) ?? 0)       : 0.0;
            var low  = submitted.Any() ? (double)(submitted.Min(a => a.TotalScore) ?? 0)      : 0.0;
            stats.Add(new QuizStatsRow(q.Id, q.Title, q.CourseName, attempts.Count, submitted.Count,
                Math.Round(avg, 2), Math.Round(high, 2), Math.Round(low, 2)));
        }
        var report = new QuizStatsReport(departmentId ?? Guid.Empty, deptName, stats);

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(report),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AnalyticsCacheTtl
            },
            ct);

        return report;
    }

    // Stage 31.2 - Top performers
    public async Task<TopPerformersReport?> GetTopPerformersAsync(
        Guid? departmentId,
        int? institutionType = null,
        int take = 10,
        CancellationToken ct = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 100);
        var cacheKey = BuildAnalyticsCacheKey($"top-performers:{normalizedTake}", departmentId, institutionType);
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedReport = JsonSerializer.Deserialize<TopPerformersReport>(cached);
            if (cachedReport is not null)
            {
                return cachedReport;
            }
        }

        var deptName = await ResolveDeptNameAsync(departmentId, institutionType, ct);
        var effectiveInstitutionType = institutionType ?? (int)InstitutionType.University;

        var raw = await (
            from r in _db.Results
            join sp in _db.StudentProfiles on r.StudentProfileId equals sp.Id
            join u  in _db.Users on sp.UserId equals u.Id
            join co in _db.CourseOfferings on r.CourseOfferingId equals co.Id
            join c  in _db.Courses on co.CourseId equals c.Id
            join d  in _db.Departments on c.DepartmentId equals d.Id
            where r.IsPublished
               && r.MaxMarks > 0
               && (departmentId == null || c.DepartmentId == departmentId)
               && (!institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
            select new
            {
                sp.Id,
                sp.RegistrationNumber,
                FullName = u.Username,
                DepartmentName = d.Name,
                Percentage = (r.MarksObtained / r.MaxMarks) * 100m,
                r.PublishedAt,
                DepartmentInstitutionType = (int)d.InstitutionType
            }
        ).ToListAsync(ct);

        if (!raw.Any()) return null;

        if (!institutionType.HasValue)
        {
            effectiveInstitutionType = raw
                .GroupBy(x => x.DepartmentInstitutionType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault((int)InstitutionType.University);
        }

        var rows = raw
            .GroupBy(x => new { x.Id, x.RegistrationNumber, x.FullName, x.DepartmentName })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.RegistrationNumber,
                g.Key.FullName,
                g.Key.DepartmentName,
                AveragePercentage = Math.Round(g.Average(x => x.Percentage), 2),
                ResultCount = g.Count(),
                LastPublishedAt = g.Max(x => x.PublishedAt)
            })
            .OrderByDescending(x => x.AveragePercentage)
            .ThenByDescending(x => x.ResultCount)
            .ThenBy(x => x.FullName)
            .Take(normalizedTake)
            .Select((x, idx) => new TopPerformerRow(
                idx + 1,
                x.Id,
                x.RegistrationNumber,
                x.FullName,
                x.DepartmentName,
                x.AveragePercentage,
                x.ResultCount,
                x.LastPublishedAt))
            .ToList();

        var report = new TopPerformersReport(
            departmentId ?? Guid.Empty,
            deptName,
            effectiveInstitutionType,
            normalizedTake,
            rows);

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(report),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AnalyticsCacheTtl
            },
            ct);

        return report;
    }

    // Stage 31.2 - Performance trends
    public async Task<PerformanceTrendReport?> GetPerformanceTrendsAsync(
        Guid? departmentId,
        int? institutionType = null,
        int windowDays = 30,
        CancellationToken ct = default)
    {
        var normalizedWindowDays = Math.Clamp(windowDays, 7, 180);
        var cacheKey = BuildAnalyticsCacheKey($"performance-trends:{normalizedWindowDays}", departmentId, institutionType);
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedReport = JsonSerializer.Deserialize<PerformanceTrendReport>(cached);
            if (cachedReport is not null)
            {
                return cachedReport;
            }
        }

        var deptName = await ResolveDeptNameAsync(departmentId, institutionType, ct);
        var effectiveInstitutionType = institutionType ?? (int)InstitutionType.University;
        var windowStartDateUtc = DateTime.UtcNow.Date.AddDays(-(normalizedWindowDays - 1));

        var raw = await (
            from r in _db.Results
            join co in _db.CourseOfferings on r.CourseOfferingId equals co.Id
            join c  in _db.Courses on co.CourseId equals c.Id
            join d  in _db.Departments on c.DepartmentId equals d.Id
            where r.IsPublished
               && r.MaxMarks > 0
               && (r.PublishedAt ?? r.CreatedAt) >= windowStartDateUtc
               && (departmentId == null || c.DepartmentId == departmentId)
               && (!institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
            select new
            {
                Day = (r.PublishedAt ?? r.CreatedAt).Date,
                Percentage = (r.MarksObtained / r.MaxMarks) * 100m,
                DepartmentInstitutionType = (int)d.InstitutionType
            }
        ).ToListAsync(ct);

        if (!raw.Any()) return null;

        if (!institutionType.HasValue)
        {
            effectiveInstitutionType = raw
                .GroupBy(x => x.DepartmentInstitutionType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault((int)InstitutionType.University);
        }

        var points = raw
            .GroupBy(x => x.Day)
            .OrderBy(g => g.Key)
            .Select(g => new PerformanceTrendPoint(
                DateOnly.FromDateTime(g.Key),
                Math.Round(g.Average(x => x.Percentage), 2),
                g.Count()))
            .ToList();

        var report = new PerformanceTrendReport(
            departmentId ?? Guid.Empty,
            deptName,
            effectiveInstitutionType,
            normalizedWindowDays,
            points);

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(report),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AnalyticsCacheTtl
            },
            ct);

        return report;
    }

    // Stage 31.2 - Comparative summary
    public async Task<ComparativeSummaryReport?> GetComparativeSummaryAsync(
        Guid? departmentId,
        int? institutionType = null,
        CancellationToken ct = default)
    {
        var cacheKey = BuildAnalyticsCacheKey("comparative-summary", departmentId, institutionType);
        var cached = await _distributedCache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedReport = JsonSerializer.Deserialize<ComparativeSummaryReport>(cached);
            if (cachedReport is not null)
            {
                return cachedReport;
            }
        }

        var scopedDepartments = await _db.Departments
            .Where(d => !departmentId.HasValue || d.Id == departmentId.Value)
            .Where(d => !institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
            .Select(d => new { d.Id, d.Name, InstitutionType = (int)d.InstitutionType })
            .ToListAsync(ct);

        if (!scopedDepartments.Any()) return null;

        var rows = new List<ComparativeSummaryRow>(scopedDepartments.Count);
        foreach (var department in scopedDepartments)
        {
            var resultAverage = await (
                from r in _db.Results
                join co in _db.CourseOfferings on r.CourseOfferingId equals co.Id
                join c  in _db.Courses on co.CourseId equals c.Id
                where c.DepartmentId == department.Id
                   && r.IsPublished
                   && r.MaxMarks > 0
                select (r.MarksObtained / r.MaxMarks) * 100m
            ).ToListAsync(ct);

            var attendanceSnapshot = await (
                from ar in _db.AttendanceRecords
                join co in _db.CourseOfferings on ar.CourseOfferingId equals co.Id
                join c  in _db.Courses on co.CourseId equals c.Id
                where c.DepartmentId == department.Id
                select ar.Status
            ).ToListAsync(ct);

            var assignmentMeta = await (
                from a in _db.Assignments
                join co in _db.CourseOfferings on a.CourseOfferingId equals co.Id
                join c  in _db.Courses on co.CourseId equals c.Id
                where c.DepartmentId == department.Id
                select new { a.Id, a.CourseOfferingId }
            ).ToListAsync(ct);

            var quizScores = await (
                from qa in _db.QuizAttempts
                join q in _db.Quizzes on qa.QuizId equals q.Id
                join co in _db.CourseOfferings on q.CourseOfferingId equals co.Id
                join c  in _db.Courses on co.CourseId equals c.Id
                where c.DepartmentId == department.Id
                   && (qa.Status == AttemptStatus.Submitted || qa.Status == AttemptStatus.TimedOut)
                   && qa.TotalScore.HasValue
                select qa.TotalScore!.Value
            ).ToListAsync(ct);

            var expectedSubmissions = 0;
            var actualSubmissions = 0;
            foreach (var assignment in assignmentMeta)
            {
                var enrolledCount = await _db.Enrollments
                    .CountAsync(e => e.CourseOfferingId == assignment.CourseOfferingId, ct);
                var submissionsCount = await _db.AssignmentSubmissions
                    .CountAsync(s => s.AssignmentId == assignment.Id, ct);
                expectedSubmissions += enrolledCount;
                actualSubmissions += submissionsCount;
            }

            var averageResultPercentage = resultAverage.Any() ? Math.Round(resultAverage.Average(), 2) : 0m;
            var averageAttendancePercentage = attendanceSnapshot.Any()
                ? Math.Round((decimal)attendanceSnapshot.Count(s => s == AttendanceStatus.Present || s == AttendanceStatus.Late)
                             / attendanceSnapshot.Count * 100m, 2)
                : 0m;
            var assignmentSubmissionRate = expectedSubmissions > 0
                ? Math.Round((decimal)actualSubmissions / expectedSubmissions * 100m, 2)
                : 0m;
            var quizAverageScore = quizScores.Any() ? Math.Round(quizScores.Average(), 2) : 0m;

            rows.Add(new ComparativeSummaryRow(
                department.Id,
                department.Name,
                department.InstitutionType,
                averageResultPercentage,
                averageAttendancePercentage,
                assignmentSubmissionRate,
                quizAverageScore));
        }

        var effectiveInstitutionType = institutionType
            ?? rows.GroupBy(r => r.InstitutionType)
                   .OrderByDescending(g => g.Count())
                   .Select(g => g.Key)
                   .FirstOrDefault((int)InstitutionType.University);

        var report = new ComparativeSummaryReport(
            effectiveInstitutionType,
            rows.OrderByDescending(r => r.AverageResultPercentage).ToList());

        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(report),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AnalyticsCacheTtl
            },
            ct);

        return report;
    }

    // PDF exports
    /// <summary>Exports the performance report to a PDF byte array.</summary>
    public async Task<byte[]> ExportPerformancePdfAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default)
    {
        var report = await GetPerformanceReportAsync(departmentId, institutionType, ct)
                     ?? new DepartmentPerformanceReport(Guid.Empty, "All Departments", 0, 0, []);
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.Content().Column(col =>
            {
                col.Item().Text($"Performance Report - {report.DepartmentName}").FontSize(16).Bold();
                col.Item().Text($"Overall Average: {report.AverageMarks:F1}  |  Students: {report.TotalStudents}").FontSize(10);
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1);
                    });
                    AddPdfHeader(table, "Reg No", "Name", "Department", "Semester", "Avg Marks", "Submitted");
                    foreach (var s in report.Students)
                        AddPdfRow(table, s.RegistrationNumber, s.FullName, s.Department,
                            s.CurrentSemester.ToString(), s.AverageMarks.ToString("F1"), s.SubmittedAssignments.ToString());
                });
            });
        })).GeneratePdf();
    }

    /// <summary>Exports the attendance report to a PDF byte array.</summary>
    public async Task<byte[]> ExportAttendancePdfAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default)
    {
        var report = await GetAttendanceReportAsync(departmentId, institutionType, ct)
                     ?? new DepartmentAttendanceReport(Guid.Empty, "All Departments", 0, []);
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.Content().Column(col =>
            {
                col.Item().Text($"Attendance Report - {report.DepartmentName}").FontSize(16).Bold();
                col.Item().Text($"Overall Attendance: {report.OverallAttendancePercentage:F1}%").FontSize(10);
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1);
                        c.RelativeColumn(1); c.RelativeColumn(1);
                    });
                    AddPdfHeader(table, "Name", "Course", "Total", "Attended", "Attendance %");
                    foreach (var r in report.Rows)
                        AddPdfRow(table, r.FullName, r.CourseName,
                            r.TotalClasses.ToString(), r.AttendedClasses.ToString(),
                            r.AttendancePercentage.ToString("F1") + "%");
                });
            });
        })).GeneratePdf();
    }

    // Excel exports
    /// <summary>Exports the performance report to an Excel byte array.</summary>
    public async Task<byte[]> ExportPerformanceExcelAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default)
    {
        var report = await GetPerformanceReportAsync(departmentId, institutionType, ct)
                     ?? new DepartmentPerformanceReport(Guid.Empty, "All Departments", 0, 0, []);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Performance");
        ws.Cell(1, 1).Value = $"Performance Report - {report.DepartmentName}";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
        string[] h1 = ["Reg No", "Name", "Department", "Semester", "Avg Marks", "Submitted"];
        for (int i = 0; i < h1.Length; i++) { ws.Cell(2, i + 1).Value = h1[i]; ws.Cell(2, i + 1).Style.Font.Bold = true; }
        int row = 3;
        foreach (var s in report.Students)
        {
            ws.Cell(row, 1).Value = s.RegistrationNumber; ws.Cell(row, 2).Value = s.FullName;
            ws.Cell(row, 3).Value = s.Department;         ws.Cell(row, 4).Value = s.CurrentSemester;
            ws.Cell(row, 5).Value = s.AverageMarks;       ws.Cell(row, 6).Value = s.SubmittedAssignments; row++;
        }
        ws.Columns().AdjustToContents();
        using var ms1 = new MemoryStream(); wb.SaveAs(ms1); return ms1.ToArray();
    }

    /// <summary>Exports the attendance report to an Excel byte array.</summary>
    public async Task<byte[]> ExportAttendanceExcelAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default)
    {
        var report = await GetAttendanceReportAsync(departmentId, institutionType, ct)
                     ?? new DepartmentAttendanceReport(Guid.Empty, "All Departments", 0, []);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Attendance");
        ws.Cell(1, 1).Value = $"Attendance Report - {report.DepartmentName}";
        ws.Range(1, 1, 1, 5).Merge().Style.Font.Bold = true;
        string[] h2 = ["Name", "Course", "Total Classes", "Attended", "Attendance %"];
        for (int i = 0; i < h2.Length; i++) { ws.Cell(2, i + 1).Value = h2[i]; ws.Cell(2, i + 1).Style.Font.Bold = true; }
        int row = 3;
        foreach (var r in report.Rows)
        {
            ws.Cell(row, 1).Value = r.FullName;          ws.Cell(row, 2).Value = r.CourseName;
            ws.Cell(row, 3).Value = r.TotalClasses;      ws.Cell(row, 4).Value = r.AttendedClasses;
            ws.Cell(row, 5).Value = r.AttendancePercentage; row++;
        }
        ws.Columns().AdjustToContents();
        using var ms2 = new MemoryStream(); wb.SaveAs(ms2); return ms2.ToArray();
    }

    public async Task<byte[]> ExportTopPerformersPdfAsync(Guid? departmentId, int? institutionType = null, int take = 10, CancellationToken ct = default)
    {
        var report = await GetTopPerformersAsync(departmentId, institutionType, take, ct)
                     ?? new TopPerformersReport(Guid.Empty, "All Departments", institutionType ?? (int)InstitutionType.University, take, []);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.Content().Column(col =>
            {
                col.Item().Text($"Top Performers - {report.DepartmentName}").FontSize(16).Bold();
                col.Item().Text($"Institution Type: {report.EffectiveInstitutionType} | Take: {report.Take}").FontSize(10);
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(45); c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(1);
                    });
                    AddPdfHeader(table, "Rank", "Reg No", "Name", "Department", "Avg %", "Results");
                    foreach (var r in report.Rows)
                        AddPdfRow(table, r.Rank.ToString(), r.RegistrationNumber, r.FullName, r.Department,
                            r.AveragePercentage.ToString("F2"), r.ResultCount.ToString());
                });
            });
        })).GeneratePdf();
    }

    public async Task<byte[]> ExportTopPerformersExcelAsync(Guid? departmentId, int? institutionType = null, int take = 10, CancellationToken ct = default)
    {
        var report = await GetTopPerformersAsync(departmentId, institutionType, take, ct)
                     ?? new TopPerformersReport(Guid.Empty, "All Departments", institutionType ?? (int)InstitutionType.University, take, []);

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Top Performers");
        ws.Cell(1, 1).Value = $"Top Performers - {report.DepartmentName}";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.Bold = true;
        string[] headers = ["Rank", "Reg No", "Name", "Department", "Avg %", "Results", "Last Published At"];
        for (int i = 0; i < headers.Length; i++) { ws.Cell(2, i + 1).Value = headers[i]; ws.Cell(2, i + 1).Style.Font.Bold = true; }

        var row = 3;
        foreach (var r in report.Rows)
        {
            ws.Cell(row, 1).Value = r.Rank;
            ws.Cell(row, 2).Value = r.RegistrationNumber;
            ws.Cell(row, 3).Value = r.FullName;
            ws.Cell(row, 4).Value = r.Department;
            ws.Cell(row, 5).Value = r.AveragePercentage;
            ws.Cell(row, 6).Value = r.ResultCount;
            ws.Cell(row, 7).Value = r.LastPublishedAt;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    public async Task<byte[]> ExportPerformanceTrendsPdfAsync(Guid? departmentId, int? institutionType = null, int windowDays = 30, CancellationToken ct = default)
    {
        var report = await GetPerformanceTrendsAsync(departmentId, institutionType, windowDays, ct)
                     ?? new PerformanceTrendReport(Guid.Empty, "All Departments", institutionType ?? (int)InstitutionType.University, windowDays, []);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.Content().Column(col =>
            {
                col.Item().Text($"Performance Trends - {report.DepartmentName}").FontSize(16).Bold();
                col.Item().Text($"Window: {report.WindowDays} days | Points: {report.Points.Count}").FontSize(10);
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1);
                    });
                    AddPdfHeader(table, "Date", "Average %", "Result Count");
                    foreach (var p in report.Points)
                        AddPdfRow(table, p.Date.ToString("yyyy-MM-dd"), p.AveragePercentage.ToString("F2"), p.ResultCount.ToString());
                });
            });
        })).GeneratePdf();
    }

    public async Task<byte[]> ExportPerformanceTrendsExcelAsync(Guid? departmentId, int? institutionType = null, int windowDays = 30, CancellationToken ct = default)
    {
        var report = await GetPerformanceTrendsAsync(departmentId, institutionType, windowDays, ct)
                     ?? new PerformanceTrendReport(Guid.Empty, "All Departments", institutionType ?? (int)InstitutionType.University, windowDays, []);

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Performance Trends");
        ws.Cell(1, 1).Value = $"Performance Trends - {report.DepartmentName}";
        ws.Range(1, 1, 1, 3).Merge().Style.Font.Bold = true;
        string[] headers = ["Date", "Average %", "Result Count"];
        for (int i = 0; i < headers.Length; i++) { ws.Cell(2, i + 1).Value = headers[i]; ws.Cell(2, i + 1).Style.Font.Bold = true; }

        var row = 3;
        foreach (var p in report.Points)
        {
            ws.Cell(row, 1).Value = p.Date.ToString("yyyy-MM-dd");
            ws.Cell(row, 2).Value = p.AveragePercentage;
            ws.Cell(row, 3).Value = p.ResultCount;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    public async Task<byte[]> ExportComparativeSummaryPdfAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default)
    {
        var report = await GetComparativeSummaryAsync(departmentId, institutionType, ct)
                     ?? new ComparativeSummaryReport(institutionType ?? (int)InstitutionType.University, []);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.Content().Column(col =>
            {
                col.Item().Text("Comparative Summary").FontSize(16).Bold();
                col.Item().Text($"Institution Type: {report.EffectiveInstitutionType}").FontSize(10);
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1);
                    });
                    AddPdfHeader(table, "Department", "Result %", "Attendance %", "Submission %", "Quiz Avg");
                    foreach (var r in report.Rows)
                        AddPdfRow(table, r.DepartmentName,
                            r.AverageResultPercentage.ToString("F2"),
                            r.AverageAttendancePercentage.ToString("F2"),
                            r.AssignmentSubmissionRate.ToString("F2"),
                            r.QuizAverageScore.ToString("F2"));
                });
            });
        })).GeneratePdf();
    }

    public async Task<byte[]> ExportComparativeSummaryExcelAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default)
    {
        var report = await GetComparativeSummaryAsync(departmentId, institutionType, ct)
                     ?? new ComparativeSummaryReport(institutionType ?? (int)InstitutionType.University, []);

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Comparative Summary");
        ws.Cell(1, 1).Value = "Comparative Summary";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
        string[] headers = ["Department", "Institution Type", "Result %", "Attendance %", "Submission %", "Quiz Avg"];
        for (int i = 0; i < headers.Length; i++) { ws.Cell(2, i + 1).Value = headers[i]; ws.Cell(2, i + 1).Style.Font.Bold = true; }

        var row = 3;
        foreach (var r in report.Rows)
        {
            ws.Cell(row, 1).Value = r.DepartmentName;
            ws.Cell(row, 2).Value = r.InstitutionType;
            ws.Cell(row, 3).Value = r.AverageResultPercentage;
            ws.Cell(row, 4).Value = r.AverageAttendancePercentage;
            ws.Cell(row, 5).Value = r.AssignmentSubmissionRate;
            ws.Cell(row, 6).Value = r.QuizAverageScore;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // Private helpers
    /// <summary>Resolves a department name from its ID; returns "All Departments" for null.</summary>
    private async Task<string> ResolveDeptNameAsync(Guid? departmentId, int? institutionType, CancellationToken ct)
    {
        if (departmentId is null)
            return institutionType.HasValue && Enum.IsDefined(typeof(InstitutionType), institutionType.Value)
                ? $"All {((InstitutionType)institutionType.Value)} Departments"
                : "All Departments";

        var dept = await _db.Departments.FindAsync([departmentId], ct);
        return dept?.Name ?? "Unknown Department";
    }

    private static string BuildAnalyticsCacheKey(string reportType, Guid? departmentId, int? institutionType, Guid? courseId = null, Guid? semesterId = null)
    {
        var departmentSegment = departmentId?.ToString("N") ?? "all";
        var institutionSegment = institutionType?.ToString() ?? "any";
        var courseSegment = courseId?.ToString("N") ?? "all-courses";
        var semesterSegment = semesterId?.ToString("N") ?? "all-semesters";
        return $"analytics:{reportType}:{departmentSegment}:{institutionSegment}:{courseSegment}:{semesterSegment}";
    }

    private static void AddPdfHeader(TableDescriptor table, params string[] headers)
    {
        table.Header(header =>
        {
            foreach (var h in headers)
            {
                header.Cell().Background("#2563EB").Padding(4).Text(h).FontColor("#FFFFFF").Bold();
            }
        });
    }

    private static void AddPdfRow(TableDescriptor table, params string[] values)
    {
        foreach (var v in values)
            table.Cell().BorderBottom(0.5f).Padding(4).Text(v).FontSize(9);
    }
}
