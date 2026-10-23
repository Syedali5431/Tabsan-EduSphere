namespace Tabsan.EduSphere.Mobile.Core;

// Plan M12 — what the app reads from the API (only the fields it shows). Property names follow the API's camelCase JSON.

public sealed record AppSettings(string ApiBaseUrl, string AppVersion, string Platform, string AppName = "EduSphere")
{
    /// <summary>White-label builds set the institution's API address and name; see mobile/README.md.</summary>
    public string ApiBase => ApiBaseUrl.TrimEnd('/');
}

public sealed record Session(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry, string Role, Guid UserId, string Username);

public sealed class LoginApiResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime AccessTokenExpiry { get; set; }
    public string Role { get; set; } = "";
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public bool MustChangePassword { get; set; }
}

public sealed class MobileLanguage
{
    public string Code { get; set; } = "en";
    public string Name { get; set; } = "English";
    public string NativeName { get; set; } = "English";
    public bool Rtl { get; set; }
}

public sealed class SsoProvider
{
    public string Provider { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public sealed class MobileConfig
{
    public string InstitutionName { get; set; } = "EduSphere";
    public string? Subtitle { get; set; }
    public string? LogoUrl { get; set; }
    public string MinimumVersion { get; set; } = "1.0.0";
    public string LatestVersion { get; set; } = "1.0.0";
    public string? AndroidStoreUrl { get; set; }
    public string? IosStoreUrl { get; set; }
    public string? PortalBaseUrl { get; set; }
    public string AppRedirectUri { get; set; } = "edusphere://sso";
    public string DefaultLanguage { get; set; } = "en";
    public List<MobileLanguage> Languages { get; set; } = [];
    public List<SsoProvider> SsoProviders { get; set; } = [];
    public bool PushEnabled { get; set; }
}

public sealed class ClassSlot
{
    public int DayOfWeek { get; set; }
    public string Day { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? CourseCode { get; set; }
    public string? Room { get; set; }
    public string? Building { get; set; }
    public string? Teacher { get; set; }
    public Guid? OfferingId { get; set; }
    public string Where => string.Join(" · ", new[] { Room, Building }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public sealed class DueItem
{
    public string Kind { get; set; } = "";
    public Guid Id { get; set; }
    public Guid OfferingId { get; set; }
    public string Title { get; set; } = "";
    public string? CourseTitle { get; set; }
    public DateTime DueAt { get; set; }
    public bool Done { get; set; }
}

public sealed class AnnouncementItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime PostedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CourseTitle { get; set; }
    public string? AuthorName { get; set; }
}

public sealed class StudentSummary
{
    public Guid StudentProfileId { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? ProgramName { get; set; }
    public decimal? AttendancePercent { get; set; }
    public decimal? Cgpa { get; set; }
    public int Courses { get; set; }
    public decimal? OutstandingFees { get; set; }
}

public sealed class TeacherSummary
{
    public int Courses { get; set; }
    public int SubmissionsToGrade { get; set; }
    public int ClassesToday { get; set; }
}

public sealed class ChildSummary
{
    public Guid StudentProfileId { get; set; }
    public string Name { get; set; } = "";
    public string? RegistrationNumber { get; set; }
    public string? ProgramName { get; set; }
    public decimal? AttendancePercent { get; set; }
    public decimal? Cgpa { get; set; }
    public decimal? OutstandingFees { get; set; }
    public int PublishedResults { get; set; }
}

public sealed class HomeData
{
    public string Role { get; set; } = "";
    public string? DisplayName { get; set; }
    public int UnreadNotifications { get; set; }
    public List<ClassSlot> Today { get; set; } = [];
    public List<DueItem> Due { get; set; } = [];
    public List<AnnouncementItem> Announcements { get; set; } = [];
    public StudentSummary? Student { get; set; }
    public TeacherSummary? Teacher { get; set; }
    public List<ChildSummary>? Children { get; set; }
}

public sealed class CourseOffering
{
    public Guid Id { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
    public string? SemesterName { get; set; }
    public string Title => string.IsNullOrWhiteSpace(CourseCode) ? CourseTitle ?? "" : $"{CourseCode} — {CourseTitle}";
}

public sealed class AssignmentItem
{
    public Guid Id { get; set; }
    public Guid CourseOfferingId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public decimal MaxMarks { get; set; }
    public bool IsPublished { get; set; }
}

public sealed class SubmissionItem
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public Guid StudentProfileId { get; set; }
    public string? StudentName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? MarksAwarded { get; set; }
    public string? Feedback { get; set; }
    public string? Status { get; set; }
    public Guid Key => SubmissionId != Guid.Empty ? SubmissionId : Id;
}

public sealed class ResultItem
{
    public Guid OfferingId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string? SemesterName { get; set; }
    public string ResultType { get; set; } = "";
    public decimal MarksObtained { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal Percentage { get; set; }
    public decimal? GradePoint { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class AttendanceCourse
{
    public Guid OfferingId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public int Total { get; set; }
    public int Attended { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Excused { get; set; }
    public decimal? Percent { get; set; }
    public DateTime? LastMarked { get; set; }
}

public sealed class NotificationItem
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid Key => NotificationId != Guid.Empty ? NotificationId : Id;
}

public sealed class MaterialItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ExternalUrl { get; set; }
    public string? FileName { get; set; }
}

public sealed class RosterStudent
{
    public Guid StudentProfileId { get; set; }
    public string? StudentName { get; set; }
    public string? RegistrationNumber { get; set; }
}

public sealed record UploadFile(string FileName, string ContentType, Func<Task<Stream>> Open);

public sealed record CrashReport(string Platform, string AppVersion, string? OsVersion, string? DeviceModel, string ExceptionType,
    string Message, string StackTrace, string? Screen, DateTime OccurredAt);

public sealed class CourseSummary
{
    public Guid OfferingId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string? SemesterName { get; set; }
    public string? Teacher { get; set; }
    public string Title => string.IsNullOrWhiteSpace(CourseCode) ? CourseTitle : $"{CourseCode} — {CourseTitle}";
}

public sealed class CourseMaterial
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public string? LinkUrl { get; set; }
    public bool HasFile { get; set; }
}

public sealed class CourseAssignment
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public decimal MaxMarks { get; set; }
    public bool Submitted { get; set; }
    public decimal? Marks { get; set; }
    public string? Feedback { get; set; }
    public string? Status { get; set; }
}

public sealed class CourseQuiz
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int AttemptsUsed { get; set; }
    public int MaxAttempts { get; set; }
}

public sealed class CourseDetail
{
    public CourseSummary Course { get; set; } = new();
    public List<CourseMaterial> Materials { get; set; } = [];
    public List<CourseAssignment> Assignments { get; set; } = [];
    public List<CourseQuiz> Quizzes { get; set; } = [];
    public List<AnnouncementItem> Announcements { get; set; } = [];
    public bool IsTeacher { get; set; }
}

public sealed class ReportCard
{
    public Guid Id { get; set; }
    public string PeriodLabel { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public List<ReportCardLine> Lines { get; set; } = [];
}

public sealed class ReportCardLine
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsHeading { get; set; }
}

/// <summary>A course discussion: how teachers and students message each other about a course.</summary>
public sealed class DiscussionThread
{
    public Guid Id { get; set; }
    public Guid OfferingId { get; set; }
    public string Title { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public bool IsPinned { get; set; }
    public bool IsClosed { get; set; }
    public bool IsSolved { get; set; }
    public int ReplyCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? GroupId { get; set; }
    public List<DiscussionReply> Replies { get; set; } = [];
}

public sealed class DiscussionReply
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}