namespace Tabsan.EduSphere.Domain.Settings;

/// <summary>
/// Stable string keys that identify the five standard reports seeded at startup.
/// Use these constants everywhere in the codebase instead of magic strings.
/// </summary>
public static class ReportKeys
{
    public const string AttendanceSummary    = "attendance_summary";
    public const string ResultSummary        = "result_summary";
    public const string GpaReport            = "gpa_report";
    public const string EnrollmentSummary    = "enrollment_summary";
    public const string SemesterResults      = "semester_results";
    public const string StudentTranscript    = "student_transcript";
    public const string LowAttendanceWarning = "low_attendance_warning";
    public const string FypStatus            = "fyp_status";
    public const string PaymentSummary       = "payment_summary";
}
