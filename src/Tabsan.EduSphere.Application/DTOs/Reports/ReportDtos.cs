namespace Tabsan.EduSphere.Application.DTOs.Reports;

// ── Report Catalog ─────────────────────────────────────────────────────────────

public record ReportCatalogItemResponse(
    Guid Id,
    string Key,
    string Name,
    string Purpose,
    bool IsActive,
    IReadOnlyList<string> AllowedRoles);

public record ReportCatalogResponse(IReadOnlyList<ReportCatalogItemResponse> Reports);

public record ReportSectionItemResponse(
    string Key,
    string Name,
    string Purpose);

public record ReportSectionResponse(
    string SectionKey,
    string SectionName,
    IReadOnlyList<ReportSectionItemResponse> Reports);

public record InstitutionReportSectionsResponse(
    int EffectiveInstitutionType,
    string InstitutionModel,
    IReadOnlyList<ReportSectionResponse> Sections);

// ── Attendance Summary ─────────────────────────────────────────────────────────

public record AttendanceSummaryRequest(
    Guid? SemesterId,
    Guid? DepartmentId,
    Guid? CourseOfferingId,
    Guid? StudentProfileId,
    int? InstitutionType);

public record AttendanceSummaryRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    Guid CourseOfferingId,
    string CourseCode,
    string CourseTitle,
    int TotalSessions,
    int AttendedSessions,
    decimal AttendancePercentage);

public record AttendanceSummaryReportResponse(
    IReadOnlyList<AttendanceSummaryRow> Rows,
    int TotalStudents,
    DateTime GeneratedAt);

// ── Result Summary ─────────────────────────────────────────────────────────────

public record ResultSummaryRequest(
    Guid? SemesterId,
    Guid? DepartmentId,
    Guid? CourseOfferingId,
    Guid? StudentProfileId,
    int? InstitutionType);

public record ResultSummaryRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string CourseCode,
    string CourseTitle,
    string ResultType,
    decimal MarksObtained,
    decimal MaxMarks,
    decimal Percentage,
    DateTime? PublishedAt);

public record ResultSummaryReportResponse(
    IReadOnlyList<ResultSummaryRow> Rows,
    int TotalRecords,
    DateTime GeneratedAt);

// ── Assignment Summary ───────────────────────────────────────────────────────

public record AssignmentSummaryRequest(
    Guid? SemesterId,
    Guid? DepartmentId,
    Guid? CourseOfferingId,
    Guid? StudentProfileId,
    int? InstitutionType);

public record AssignmentSummaryRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string CourseCode,
    string CourseTitle,
    string AssignmentTitle,
    DateTime DueDate,
    DateTime SubmittedAt,
    string Status,
    decimal? MarksAwarded);

public record AssignmentSummaryReportResponse(
    IReadOnlyList<AssignmentSummaryRow> Rows,
    int TotalSubmissions,
    DateTime GeneratedAt);

// ── Quiz Summary ─────────────────────────────────────────────────────────────

public record QuizSummaryRequest(
    Guid? SemesterId,
    Guid? DepartmentId,
    Guid? CourseOfferingId,
    Guid? StudentProfileId,
    int? InstitutionType);

public record QuizSummaryRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string CourseCode,
    string CourseTitle,
    string QuizTitle,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string AttemptStatus,
    decimal? TotalScore);

public record QuizSummaryReportResponse(
    IReadOnlyList<QuizSummaryRow> Rows,
    int TotalAttempts,
    DateTime GeneratedAt);

// ── GPA Report ─────────────────────────────────────────────────────────────────

public record GpaReportRequest(Guid? DepartmentId, Guid? ProgramId, int? InstitutionType);

public record GpaReportRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string ProgramName,
    string DepartmentName,
    int CurrentSemester,
    decimal Cgpa,
    decimal CurrentSemesterGpa);

public record GpaReportResponse(
    IReadOnlyList<GpaReportRow> Rows,
    decimal AverageCgpa,
    int TotalStudents,
    DateTime GeneratedAt);

// ── Enrollment Summary ─────────────────────────────────────────────────────────

public record EnrollmentSummaryRequest(Guid? SemesterId, Guid? DepartmentId, int? InstitutionType);

public record EnrollmentSummaryRow(
    Guid CourseOfferingId,
    string CourseCode,
    string CourseTitle,
    string SemesterName,
    int MaxEnrollment,
    int EnrolledCount,
    int AvailableSeats);

public record EnrollmentSummaryReportResponse(
    IReadOnlyList<EnrollmentSummaryRow> Rows,
    int TotalOfferings,
    DateTime GeneratedAt);

// ── Semester Results ───────────────────────────────────────────────────────────

public record SemesterResultsRequest(Guid SemesterId, Guid? DepartmentId, int? InstitutionType);

public record SemesterResultsRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string CourseCode,
    string CourseTitle,
    string ResultType,
    decimal MarksObtained,
    decimal MaxMarks,
    decimal Percentage);

public record SemesterResultsReportResponse(
    IReadOnlyList<SemesterResultsRow> Rows,
    int TotalStudents,
    DateTime GeneratedAt);

// ── Student Transcript ─────────────────────────────────────────────────────────

public record TranscriptRequest(Guid StudentProfileId);

public record TranscriptRow(
    string CourseCode,
    string CourseTitle,
    string SemesterName,
    string ResultType,
    decimal MarksObtained,
    decimal MaxMarks,
    decimal Percentage,
    decimal? GradePoint,
    DateTime? PublishedAt);

public record TranscriptReportResponse(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string ProgramName,
    string DepartmentName,
    decimal Cgpa,
    IReadOnlyList<TranscriptRow> Rows,
    DateTime GeneratedAt);

// ── Low Attendance Warning ──────────────────────────────────────────────────────

public record LowAttendanceRequest(
    decimal ThresholdPercent,
    Guid? DepartmentId,
    Guid? CourseOfferingId,
    int? InstitutionType);

public record LowAttendanceRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string CourseCode,
    string CourseTitle,
    string SemesterName,
    string DepartmentName,
    int TotalSessions,
    int AttendedSessions,
    decimal AttendancePercentage);

public record LowAttendanceReportResponse(
    IReadOnlyList<LowAttendanceRow> Rows,
    decimal ThresholdPercent,
    int TotalStudentsAtRisk,
    DateTime GeneratedAt);

// ── FYP Status Report ──────────────────────────────────────────────────────────

public record FypStatusRequest(Guid? DepartmentId, string? Status, int? InstitutionType);

public record FypStatusRow(
    Guid ProjectId,
    string Title,
    string StudentName,
    string RegistrationNumber,
    string DepartmentName,
    string? SupervisorName,
    string Status,
    DateTime ProposedAt,
    int MeetingCount);

public record FypStatusReportResponse(
    IReadOnlyList<FypStatusRow> Rows,
    int TotalProjects,
    DateTime GeneratedAt);

// ── Plan F Phase 4: Payment Summary Report ───────────────────────────────────

public record PaymentSummaryRequest(
    int? Year,
    int? Month,
    Guid? SemesterId,
    Guid? DepartmentId,
    Guid? CourseId,
    int? LevelNumber,
    int? InstitutionType,
    Guid? TenantId,
    Guid? CampusId);

public record PaymentSummaryRow(
    Guid ReceiptId,
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    decimal Amount,
    string Status,
    DateTime DueDate,
    DateTime? PaidDate,
    string DepartmentName,
    string? CourseCode,
    string? CourseTitle,
    string? SemesterName,
    int CurrentLevel,
    string LevelLabel);

public record PaymentSummaryReportResponse(
    IReadOnlyList<PaymentSummaryRow> Rows,
    decimal TotalAmount,
    decimal TotalPaid,
    decimal TotalPending,
    int TotalReceipts,
    DateTime GeneratedAt);
