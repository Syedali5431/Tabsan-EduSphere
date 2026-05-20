using Tabsan.EduSphere.Application.DTOs.Reports;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Application service contract for Phase 12 report generation and Excel export.
/// </summary>
public interface IReportService
{
    /// <summary>Returns the list of reports the given role is permitted to view.</summary>
    Task<ReportCatalogResponse> GetCatalogAsync(string roleName, CancellationToken ct = default);

    Task<AttendanceSummaryReportResponse> GetAttendanceSummaryAsync(AttendanceSummaryRequest request, CancellationToken ct = default);
    Task<ResultSummaryReportResponse>     GetResultSummaryAsync(ResultSummaryRequest request, CancellationToken ct = default);
    Task<AssignmentSummaryReportResponse> GetAssignmentSummaryAsync(AssignmentSummaryRequest request, CancellationToken ct = default);
    Task<QuizSummaryReportResponse>       GetQuizSummaryAsync(QuizSummaryRequest request, CancellationToken ct = default);
    Task<GpaReportResponse>               GetGpaReportAsync(GpaReportRequest request, CancellationToken ct = default);
    Task<EnrollmentSummaryReportResponse> GetEnrollmentSummaryAsync(EnrollmentSummaryRequest request, CancellationToken ct = default);
    Task<SemesterResultsReportResponse>   GetSemesterResultsAsync(SemesterResultsRequest request, CancellationToken ct = default);

    /// <summary>Returns an Excel workbook (.xlsx) byte array for the attendance summary report.</summary>
    Task<byte[]> ExportAttendanceSummaryExcelAsync(AttendanceSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a CSV byte array for the attendance summary report.</summary>
    Task<byte[]> ExportAttendanceSummaryCsvAsync(AttendanceSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a PDF byte array for the attendance summary report.</summary>
    Task<byte[]> ExportAttendanceSummaryPdfAsync(AttendanceSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns an Excel workbook (.xlsx) byte array for the result summary report.</summary>
    Task<byte[]> ExportResultSummaryExcelAsync(ResultSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a CSV byte array for the result summary report.</summary>
    Task<byte[]> ExportResultSummaryCsvAsync(ResultSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a PDF byte array for the result summary report.</summary>
    Task<byte[]> ExportResultSummaryPdfAsync(ResultSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns an Excel workbook (.xlsx) byte array for the assignment summary report.</summary>
    Task<byte[]> ExportAssignmentSummaryExcelAsync(AssignmentSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a CSV byte array for the assignment summary report.</summary>
    Task<byte[]> ExportAssignmentSummaryCsvAsync(AssignmentSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a PDF byte array for the assignment summary report.</summary>
    Task<byte[]> ExportAssignmentSummaryPdfAsync(AssignmentSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns an Excel workbook (.xlsx) byte array for the quiz summary report.</summary>
    Task<byte[]> ExportQuizSummaryExcelAsync(QuizSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a CSV byte array for the quiz summary report.</summary>
    Task<byte[]> ExportQuizSummaryCsvAsync(QuizSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns a PDF byte array for the quiz summary report.</summary>
    Task<byte[]> ExportQuizSummaryPdfAsync(QuizSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns an Excel workbook (.xlsx) byte array for the GPA report.</summary>
    Task<byte[]> ExportGpaReportExcelAsync(GpaReportRequest request, CancellationToken ct = default);

    // ── Stage 4.2: Additional reports ─────────────────────────────────────────
    Task<TranscriptReportResponse?>    GetStudentTranscriptAsync(TranscriptRequest request, CancellationToken ct = default);
    Task<LowAttendanceReportResponse>  GetLowAttendanceWarningAsync(LowAttendanceRequest request, CancellationToken ct = default);
    Task<FypStatusReportResponse>      GetFypStatusReportAsync(FypStatusRequest request, CancellationToken ct = default);
    Task<PaymentSummaryReportResponse> GetPaymentSummaryAsync(PaymentSummaryRequest request, CancellationToken ct = default);

    /// <summary>Returns an Excel workbook (.xlsx) byte array for the student transcript.</summary>
    Task<byte[]> ExportTranscriptExcelAsync(TranscriptRequest request, CancellationToken ct = default);
    Task<byte[]> ExportPaymentSummaryExcelAsync(PaymentSummaryRequest request, CancellationToken ct = default);
    Task<byte[]> ExportPaymentSummaryCsvAsync(PaymentSummaryRequest request, CancellationToken ct = default);
    Task<byte[]> ExportPaymentSummaryPdfAsync(PaymentSummaryRequest request, CancellationToken ct = default);
}
