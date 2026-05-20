using Tabsan.EduSphere.Application.DTOs.Analytics;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Application service for analytics reports and export.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>Returns a performance report for a department, or all departments if null.</summary>
    Task<DepartmentPerformanceReport?> GetPerformanceReportAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default, Guid? courseId = null, Guid? semesterId = null);

    /// <summary>Returns an attendance summary report for a department, or all departments if null.</summary>
    Task<DepartmentAttendanceReport?> GetAttendanceReportAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default, Guid? courseId = null, Guid? semesterId = null);

    /// <summary>Returns assignment statistics for a department, or all departments if null.</summary>
    Task<AssignmentStatsReport?> GetAssignmentStatsAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default, Guid? courseId = null, Guid? semesterId = null);

    /// <summary>Returns quiz statistics for a department, or all departments if null.</summary>
    Task<QuizStatsReport?> GetQuizStatsAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Exports the performance report for a department to a PDF byte array.</summary>
    Task<byte[]> ExportPerformancePdfAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Exports the performance report for a department to an Excel byte array.</summary>
    Task<byte[]> ExportPerformanceExcelAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Exports the attendance report for a department to a PDF byte array.</summary>
    Task<byte[]> ExportAttendancePdfAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Exports the attendance report for a department to an Excel byte array.</summary>
    Task<byte[]> ExportAttendanceExcelAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Exports the top performers report for a department to a PDF byte array.</summary>
    Task<byte[]> ExportTopPerformersPdfAsync(Guid? departmentId, int? institutionType = null, int take = 10, CancellationToken ct = default);

    /// <summary>Exports the top performers report for a department to an Excel byte array.</summary>
    Task<byte[]> ExportTopPerformersExcelAsync(Guid? departmentId, int? institutionType = null, int take = 10, CancellationToken ct = default);

    /// <summary>Exports the performance trends report for a department to a PDF byte array.</summary>
    Task<byte[]> ExportPerformanceTrendsPdfAsync(Guid? departmentId, int? institutionType = null, int windowDays = 30, CancellationToken ct = default);

    /// <summary>Exports the performance trends report for a department to an Excel byte array.</summary>
    Task<byte[]> ExportPerformanceTrendsExcelAsync(Guid? departmentId, int? institutionType = null, int windowDays = 30, CancellationToken ct = default);

    /// <summary>Exports the comparative summary report for a department to a PDF byte array.</summary>
    Task<byte[]> ExportComparativeSummaryPdfAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Exports the comparative summary report for a department to an Excel byte array.</summary>
    Task<byte[]> ExportComparativeSummaryExcelAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Returns ranked top performers for a scoped analytics context.</summary>
    Task<TopPerformersReport?> GetTopPerformersAsync(Guid? departmentId, int? institutionType = null, int take = 10, CancellationToken ct = default);

    /// <summary>Returns daily performance trends for a scoped analytics context.</summary>
    Task<PerformanceTrendReport?> GetPerformanceTrendsAsync(Guid? departmentId, int? institutionType = null, int windowDays = 30, CancellationToken ct = default);

    /// <summary>Returns comparative summary metrics across scoped departments.</summary>
    Task<ComparativeSummaryReport?> GetComparativeSummaryAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default);

    /// <summary>Returns paid vs unpaid payment status summary for a scoped analytics context.</summary>
    Task<PaymentStatusReport?> GetPaymentStatusReportAsync(Guid? departmentId, int? institutionType = null, CancellationToken ct = default, Guid? courseId = null, Guid? semesterId = null);
}
