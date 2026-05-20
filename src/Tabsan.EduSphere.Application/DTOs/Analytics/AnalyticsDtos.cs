namespace Tabsan.EduSphere.Application.DTOs.Analytics;

// ── Student performance ───────────────────────────────────────────────────────

/// <summary>Per-student performance summary aggregated across results.</summary>
public record StudentPerformanceRow(
    Guid   StudentProfileId,
    string RegistrationNumber,
    string FullName,
    string Department,
    int    CurrentSemester,
    double AverageMarks,
    int    TotalAssignments,
    int    SubmittedAssignments);

/// <summary>Department-level performance report.</summary>
public record DepartmentPerformanceReport(
    Guid   DepartmentId,
    string DepartmentName,
    double AverageMarks,
    int    TotalStudents,
    IReadOnlyList<StudentPerformanceRow> Students);

// ── Attendance ────────────────────────────────────────────────────────────────

/// <summary>Attendance summary per student for a department.</summary>
public record AttendanceRow(
    Guid   StudentProfileId,
    string FullName,
    string CourseName,
    int    TotalClasses,
    int    AttendedClasses,
    double AttendancePercentage);

/// <summary>Department-level attendance summary.</summary>
public record DepartmentAttendanceReport(
    Guid   DepartmentId,
    string DepartmentName,
    double OverallAttendancePercentage,
    IReadOnlyList<AttendanceRow> Rows);

// ── Assignment statistics ─────────────────────────────────────────────────────

/// <summary>Statistics for a single assignment.</summary>
public record AssignmentStatsRow(
    Guid   AssignmentId,
    string Title,
    string CourseName,
    int    TotalStudents,
    int    Submissions,
    int    Graded,
    double AverageMarks);

/// <summary>Assignment statistics report for a department.</summary>
public record AssignmentStatsReport(
    Guid   DepartmentId,
    string DepartmentName,
    IReadOnlyList<AssignmentStatsRow> Assignments);

// ── Quiz statistics ───────────────────────────────────────────────────────────

/// <summary>Statistics for a single quiz.</summary>
public record QuizStatsRow(
    Guid   QuizId,
    string Title,
    string CourseName,
    int    TotalAttempts,
    int    CompletedAttempts,
    double AverageScore,
    double HighestScore,
    double LowestScore);

/// <summary>Quiz statistics report for a department.</summary>
public record QuizStatsReport(
    Guid   DepartmentId,
    string DepartmentName,
    IReadOnlyList<QuizStatsRow> Quizzes);

// ── Stage 31.2: Advanced Analytics ─────────────────────────────────────────

/// <summary>Ranked top performer entry for a scoped analytics context.</summary>
public record TopPerformerRow(
    int Rank,
    Guid StudentProfileId,
    string RegistrationNumber,
    string FullName,
    string Department,
    decimal AveragePercentage,
    int ResultCount,
    DateTime? LastPublishedAt);

/// <summary>Top performers report for the selected analytics scope.</summary>
public record TopPerformersReport(
    Guid DepartmentId,
    string DepartmentName,
    int EffectiveInstitutionType,
    int Take,
    IReadOnlyList<TopPerformerRow> Rows);

/// <summary>Time-bucketed performance trend point.</summary>
public record PerformanceTrendPoint(
    DateOnly Date,
    decimal AveragePercentage,
    int ResultCount);

/// <summary>Performance trend report for the selected analytics scope.</summary>
public record PerformanceTrendReport(
    Guid DepartmentId,
    string DepartmentName,
    int EffectiveInstitutionType,
    int WindowDays,
    IReadOnlyList<PerformanceTrendPoint> Points);

/// <summary>Comparative analytics row for a single department.</summary>
public record ComparativeSummaryRow(
    Guid DepartmentId,
    string DepartmentName,
    int InstitutionType,
    decimal AverageResultPercentage,
    decimal AverageAttendancePercentage,
    decimal AssignmentSubmissionRate,
    decimal QuizAverageScore);

/// <summary>Comparative summary report across scoped departments.</summary>
public record ComparativeSummaryReport(
    int EffectiveInstitutionType,
    IReadOnlyList<ComparativeSummaryRow> Rows);

// ── Payment status analytics ────────────────────────────────────────────────

/// <summary>Single segment in payment status distribution.</summary>
public record PaymentStatusSlice(
    string Status,
    int Count,
    decimal TotalAmount);

/// <summary>Paid vs unpaid payment status summary for the selected analytics scope.</summary>
public record PaymentStatusReport(
    Guid DepartmentId,
    string DepartmentName,
    int EffectiveInstitutionType,
    int PaidCount,
    int UnpaidCount,
    decimal PaidAmount,
    decimal UnpaidAmount,
    IReadOnlyList<PaymentStatusSlice> Slices);
