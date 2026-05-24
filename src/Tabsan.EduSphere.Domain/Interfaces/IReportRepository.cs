using Tabsan.EduSphere.Domain.Settings;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository contract for Phase 12 report data queries.
/// All methods return lightweight raw data tuples — mapping to DTOs happens in the application layer.
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Returns all active <see cref="ReportDefinition"/> rows for which the given role
    /// has a <c>ReportRoleAssignment</c>.
    /// </summary>
    Task<IList<ReportDefinition>> GetCatalogForRoleAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Returns raw attendance aggregate rows, optionally filtered by semester, offering, or student.
    /// </summary>
    Task<IList<AttendanceReportRow>> GetAttendanceDataAsync(
        Guid? semesterId,
        Guid? courseOfferingId,
        Guid? studentProfileId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns raw published result rows, optionally filtered by semester, offering, or student.
    /// </summary>
    Task<IList<ResultReportRow>> GetResultDataAsync(
        Guid? semesterId,
        Guid? courseOfferingId,
        Guid? studentProfileId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns assignment submission rows, optionally filtered by semester, offering, or student.
    /// </summary>
    Task<IList<AssignmentReportRow>> GetAssignmentDataAsync(
        Guid? semesterId,
        Guid? courseOfferingId,
        Guid? studentProfileId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns quiz attempt rows, optionally filtered by semester, offering, or student.
    /// </summary>
    Task<IList<QuizReportRow>> GetQuizDataAsync(
        Guid? semesterId,
        Guid? courseOfferingId,
        Guid? studentProfileId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns student profile GPA rows, optionally filtered by department or program.
    /// </summary>
    Task<IList<GpaReportRow>> GetGpaDataAsync(
        Guid? departmentId,
        Guid? programId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns course offering enrollment rows, optionally filtered by semester or department.
    /// </summary>
    Task<IList<EnrollmentReportRow>> GetEnrollmentDataAsync(
        Guid? semesterId,
        Guid? departmentId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns published result rows for a specific semester, optionally filtered by department.
    /// </summary>
    Task<IList<ResultReportRow>> GetSemesterResultDataAsync(
        Guid semesterId,
        Guid? departmentId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all published results for a single student (transcript), ordered by semester then course.
    /// </summary>
    Task<TranscriptReportRepoResult?> GetTranscriptDataAsync(
        Guid studentProfileId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns attendance rows where the attendance percentage is below <paramref name="thresholdPercent"/>.
    /// </summary>
    Task<IList<LowAttendanceReportRow>> GetLowAttendanceDataAsync(
        decimal thresholdPercent,
        Guid? departmentId,
        Guid? courseOfferingId,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all FYP project rows, optionally filtered by department or status.
    /// </summary>
    Task<IList<FypStatusReportRow>> GetFypStatusDataAsync(
        Guid? departmentId,
        string? status,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns payment receipt rows with optional time and academic filters.
    /// </summary>
    Task<IList<PaymentSummaryReportRow>> GetPaymentSummaryDataAsync(
        int? year,
        int? month,
        Guid? semesterId,
        Guid? departmentId,
        Guid? courseId,
        int? levelNumber,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct = default);
}

// ── Raw data row types returned by the repository ─────────────────────────────

public sealed record AttendanceReportRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    Guid CourseOfferingId,
    string CourseCode,
    string CourseTitle,
    string SemesterName,
    string DepartmentName,
    int TotalSessions,
    int AttendedSessions,
    decimal AttendancePercentage);

public sealed record ResultReportRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string CourseCode,
    string CourseTitle,
    string ResultType,
    decimal MarksObtained,
    decimal MaxMarks,
    decimal Percentage,
    DateTime? PublishedAt,
    Guid SemesterId,
    string DepartmentName);

public sealed record AssignmentReportRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    Guid DepartmentId,
    string CourseCode,
    string CourseTitle,
    string AssignmentTitle,
    DateTime DueDate,
    DateTime SubmittedAt,
    string Status,
    decimal? MarksAwarded,
    Guid SemesterId,
    string DepartmentName);

public sealed record QuizReportRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    Guid DepartmentId,
    string CourseCode,
    string CourseTitle,
    string QuizTitle,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string AttemptStatus,
    decimal? TotalScore,
    Guid SemesterId,
    string DepartmentName);

public sealed record GpaReportRow(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string ProgramName,
    string DepartmentName,
    int CurrentSemesterNumber,
    decimal Cgpa,
    decimal CurrentSemesterGpa);

public sealed record EnrollmentReportRow(
    Guid CourseOfferingId,
    string CourseCode,
    string CourseTitle,
    string SemesterName,
    string DepartmentName,
    int MaxEnrollment,
    int EnrolledCount);

// ── New row types for Stage 4.2 additional reports ────────────────────────────

public sealed record TranscriptResultRow(
    string CourseCode,
    string CourseTitle,
    string SemesterName,
    string ResultType,
    decimal MarksObtained,
    decimal MaxMarks,
    decimal Percentage,
    decimal? GradePoint,
    DateTime? PublishedAt);

public sealed record TranscriptReportRepoResult(
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string ProgramName,
    string DepartmentName,
    decimal Cgpa,
    IList<TranscriptResultRow> Rows);

public sealed record LowAttendanceReportRow(
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

public sealed record FypStatusReportRow(
    Guid ProjectId,
    string Title,
    string StudentName,
    string RegistrationNumber,
    string DepartmentName,
    string? SupervisorName,
    string Status,
    DateTime ProposedAt,
    int MeetingCount);

public sealed record PaymentSummaryReportRow(
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
