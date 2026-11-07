using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Mobile;

// Plan M12 — the EduSphere mobile app: API contracts (small, mobile-friendly answers), settings and native push.

/// <summary>Settings (section "Mobile").</summary>
public sealed class MobileOptions
{
    public const string SectionName = "Mobile";

    /// <summary>Oldest app version still allowed to sign in (older apps are asked to update).</summary>
    public string MinimumVersion { get; set; } = "1.0.0";

    /// <summary>Newest published version (older apps are told an update is available).</summary>
    public string LatestVersion { get; set; } = "1.0.0";

    public string? AndroidStoreUrl { get; set; }
    public string? IosStoreUrl { get; set; }

    /// <summary>The portal address the app opens for single sign-on and for pages it does not show itself (quizzes).</summary>
    public string? PortalBaseUrl { get; set; }

    /// <summary>Where the portal returns the one-time sign-in code to the app (its URL scheme).</summary>
    public string AppRedirectUri { get; set; } = "edusphere://sso";

    /// <summary>How long a sign-in hand-off code stays valid.</summary>
    public int HandoffSeconds { get; set; } = 120;

    /// <summary>Failed deliveries in a row after which a device stops receiving push.</summary>
    public int MaxConsecutiveFailures { get; set; } = 5;

    public FcmOptions Fcm { get; set; } = new();
}

/// <summary>Firebase Cloud Messaging (HTTP v1) — Android directly, iOS through APNs.</summary>
public sealed class FcmOptions
{
    public bool Enabled { get; set; }
    public string? ProjectId { get; set; }

    /// <summary>The service account key file (JSON) from the Firebase console, or its content in <see cref="ServiceAccountJson"/>.</summary>
    public string? ServiceAccountFile { get; set; }
    public string? ServiceAccountJson { get; set; }
}

public sealed record MobileLanguage(string Code, string Name, string NativeName, bool Rtl);

public sealed record MobileSsoProvider(string Provider, string DisplayName);

/// <summary>Everything the app needs before sign-in: branding (white-label), versions, languages and sign-in options.</summary>
public sealed record MobileConfigResponse(
    string InstitutionName, string? Subtitle, string? LogoUrl,
    string MinimumVersion, string LatestVersion, string? AndroidStoreUrl, string? IosStoreUrl,
    string? PortalBaseUrl, string AppRedirectUri,
    string DefaultLanguage, IReadOnlyList<MobileLanguage> Languages,
    IReadOnlyList<MobileSsoProvider> SsoProviders, bool PushEnabled);

public sealed record MobileClass(int DayOfWeek, string Day, string Start, string End, string Subject, string? CourseCode, string? Room,
    string? Building, string? Teacher, Guid? CourseId, Guid? OfferingId);

/// <summary>Something with a date: an assignment or quiz due, or a live class starting.</summary>
public sealed record MobileDue(string Kind, Guid Id, Guid OfferingId, string Title, string? CourseTitle, DateTime DueAt, bool Done);

public sealed record MobileAnnouncement(Guid Id, string Title, string Body, DateTime PostedAt, string? CourseTitle);

/// <summary>
/// Cgpa only for University students (GPA/CGPA is a University measure); School and College students get AveragePercent,
/// their published marks over the maximum marks.
/// </summary>
public sealed record MobileStudentSummary(Guid StudentProfileId, string? RegistrationNumber, string? ProgramName,
    decimal? AttendancePercent, decimal? Cgpa, int Courses, decimal? OutstandingFees, decimal? AveragePercent = null);

public sealed record MobileTeacherSummary(int Courses, int SubmissionsToGrade, int ClassesToday);

/// <summary>Cgpa and AveragePercent as in <see cref="MobileStudentSummary"/>.</summary>
public sealed record MobileChildSummary(Guid StudentProfileId, string Name, string? RegistrationNumber, string? ProgramName,
    decimal? AttendancePercent, decimal? Cgpa, decimal? OutstandingFees, int PublishedResults, decimal? AveragePercent = null);

/// <summary>The home screen in one call, by role.</summary>
public sealed record MobileHomeResponse(
    string Role, string? DisplayName, int UnreadNotifications,
    IReadOnlyList<MobileClass> Today, IReadOnlyList<MobileDue> Due, IReadOnlyList<MobileAnnouncement> Announcements,
    MobileStudentSummary? Student, MobileTeacherSummary? Teacher, IReadOnlyList<MobileChildSummary>? Children);

public sealed record RegisterDeviceRequest(string Platform, string Token, string? AppVersion, string? DeviceModel, string? Language);

public sealed record MobileDeviceResponse(Guid Id, string Platform, string? AppVersion, string? DeviceModel, bool IsActive, DateTime LastSeenAt);

public sealed record CrashReportRequest(string Platform, string AppVersion, string? OsVersion, string? DeviceModel,
    string ExceptionType, string Message, string StackTrace, string? Screen, DateTime OccurredAt);

/// <summary>The portal asks for a one-time code after single sign-on (the app sent the PKCE challenge).</summary>
public sealed record HandoffRequest(string CodeChallenge);

public sealed record HandoffResponse(string Code, int ExpiresInSeconds);

/// <summary>The app redeems the code with its PKCE verifier and gets its own tokens.</summary>
public sealed record HandoffRedeemRequest(string Code, string CodeVerifier, string? DeviceInfo);

public enum NativePushResult
{
    Delivered,
    /// <summary>The token is no longer valid (app uninstalled, token rotated).</summary>
    Gone,
    Failed
}

public sealed record NativePushMessage(string Title, string Body, NotificationType Type, IReadOnlyDictionary<string, string> Data);

/// <summary>Sends one native push message (Firebase Cloud Messaging).</summary>
public interface INativePushSender
{
    bool IsConfigured { get; }
    Task<NativePushResult> SendAsync(string token, NativePushMessage message, CancellationToken ct = default);
}

/// <summary>A course offering; SemesterName is the class (School/College) or semester (University). Campus and institute let staff with
/// several campuses choose one in the app.</summary>
public sealed record MobileCourse(Guid OfferingId, string CourseCode, string CourseTitle, string? SemesterName, string? Teacher,
    Guid? CampusId = null, string? CampusName = null, Guid? TenantId = null, string? TenantName = null);

/// <summary>Homework (an assignment) of a course: for students and parents with their own state, for teachers what is left to grade.</summary>
public sealed record MobileHomework(Guid AssignmentId, Guid OfferingId, string CourseCode, string CourseTitle, string? SemesterName,
    string Title, string? Description, DateTime DueDate, decimal MaxMarks, bool Submitted, decimal? Marks, string? Status, int ToGrade);

/// <summary>One marked day of attendance in one course.</summary>
public sealed record MobileAttendanceRecord(DateTime Date, Guid OfferingId, string CourseCode, string CourseTitle, string? SemesterName, string Status);

/// <summary>An institute (tenant) and campus the user works in.</summary>
public sealed record MobileScope(Guid TenantId, string TenantName, Guid CampusId, string CampusName);

/// <summary>An administrator's view of a campus (or of everything they manage when no campus is chosen).</summary>
public sealed record MobileCampusSummary(Guid? CampusId, string? CampusName, string? TenantName, int Students, int Teachers, int Courses,
    int AttendanceTodayMarked, decimal? AttendanceTodayPercent);

public sealed record MobileMaterial(Guid Id, string Title, string? Description, string Type, string? LinkUrl, bool HasFile);

/// <summary>ToGrade: for the course's teacher, the handed-in submissions without marks yet (0 for students).</summary>
public sealed record MobileAssignment(Guid Id, string Title, string? Description, DateTime DueDate, decimal MaxMarks, bool Submitted,
    decimal? Marks, string? Feedback, string? Status, int ToGrade = 0);

public sealed record MobileQuiz(Guid Id, string Title, DateTime? AvailableFrom, DateTime? AvailableUntil, int? TimeLimitMinutes, int AttemptsUsed, int MaxAttempts);

/// <summary>A course in one call: materials, assignments (with the student's own state), quizzes and announcements.</summary>
public sealed record MobileCourseResponse(MobileCourse Course, IReadOnlyList<MobileMaterial> Materials, IReadOnlyList<MobileAssignment> Assignments,
    IReadOnlyList<MobileQuiz> Quizzes, IReadOnlyList<MobileAnnouncement> Announcements, bool IsTeacher);

public sealed record MobileResult(Guid OfferingId, string CourseCode, string CourseTitle, string? SemesterName, string ResultType,
    decimal MarksObtained, decimal MaxMarks, decimal Percentage, decimal? GradePoint, DateTime? PublishedAt);

public sealed record MobileAttendanceCourse(Guid OfferingId, string CourseCode, string CourseTitle, int Total, int Attended, int Absent, int Late,
    int Excused, decimal? Percent, DateTime? LastMarked);

/// <summary>A report card; its stored details (free-form JSON) as label/value lines a phone can show.</summary>
public sealed record MobileReportCard(Guid Id, string PeriodLabel, DateTime GeneratedAt, IReadOnlyList<MobileReportCardLine> Lines);

/// <summary>One line of a report card; headings start a section (e.g. "Subjects").</summary>
public sealed record MobileReportCardLine(string Label, string Value, bool IsHeading = false);

/// <summary>Home screen, timetable, courses and children summaries (read models over several modules).</summary>
public interface IMobileQueries
{
    /// <summary>Report cards of the student (or of a parent's linked child), newest first; null when not allowed.</summary>
    Task<IReadOnlyList<MobileReportCard>?> ReportCardsAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default);

    /// <summary>Published results of the student (or of a parent's linked child); null when not allowed.</summary>
    Task<IReadOnlyList<MobileResult>?> ResultsAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default);

    /// <summary>Attendance per course of the student (or of a parent's linked child); null when not allowed.</summary>
    Task<IReadOnlyList<MobileAttendanceCourse>?> AttendanceAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default);

    /// <summary>
    /// Announcements of the student's courses (or of a parent's linked child), newest first; null when not allowed. Parents
    /// have no tenant scope of their own, so this does not go through the portal's tenant-scoped announcement service.
    /// </summary>
    Task<IReadOnlyList<MobileAnnouncement>?> StudentAnnouncementsAsync(Guid userId, string role, Guid? studentProfileId, CancellationToken ct = default);

    /// <summary>The courses the user studies or teaches (parents: of a linked child, ?studentProfileId=); null when not allowed.</summary>
    Task<IReadOnlyList<MobileCourse>?> CoursesAsync(Guid userId, string role, Guid? studentProfileId = null, CancellationToken ct = default);

    /// <summary>Null when the user neither studies nor teaches this course (parents: when it is not a linked child's course).</summary>
    Task<MobileCourseResponse?> CourseAsync(Guid userId, string role, Guid offeringId, Guid? studentProfileId = null, CancellationToken ct = default);

    /// <summary>Homework of all the user's courses (parents: of a linked child), soonest due first; null when not allowed.</summary>
    Task<IReadOnlyList<MobileHomework>?> HomeworkAsync(Guid userId, string role, Guid? studentProfileId = null, CancellationToken ct = default);

    /// <summary>Day-by-day attendance of the student (or a parent's linked child), newest first, optionally of one course; null when not allowed.</summary>
    Task<IReadOnlyList<MobileAttendanceRecord>?> AttendanceRecordsAsync(Guid userId, string role, Guid? studentProfileId, Guid? offeringId,
        int take = 100, CancellationToken ct = default);

    /// <summary>Institutes and campuses the user works in: teachers through their courses and departments, admins through their
    /// departments, SuperAdmins all active campuses.</summary>
    Task<IReadOnlyList<MobileScope>> ScopesAsync(Guid userId, string role, CancellationToken ct = default);

    /// <summary>Admins and SuperAdmins: students, teachers, courses and today's attendance of a campus they manage (all of them when
    /// <paramref name="campusId"/> is null); null for other roles or a campus outside their scope.</summary>
    Task<MobileCampusSummary?> CampusSummaryAsync(Guid userId, string role, Guid? campusId, DateTime today, CancellationToken ct = default);

    /// <summary>
    /// <paramref name="dayOfWeek"/> is today on the phone (0 Sunday … 6 Saturday): timetables hold the school's local times,
    /// so "today's classes" follow the user's day, not the server's UTC day (used when the app does not send it).
    /// </summary>
    Task<MobileHomeResponse> HomeAsync(Guid userId, string role, int? dayOfWeek = null, CancellationToken ct = default);
    /// <summary>The week's classes (parents: of a linked child, ?studentProfileId=); null when not allowed.</summary>
    Task<IReadOnlyList<MobileClass>?> TimetableAsync(Guid userId, string role, Guid? studentProfileId = null, CancellationToken ct = default);
    Task<IReadOnlyList<MobileChildSummary>> ChildrenAsync(Guid parentUserId, CancellationToken ct = default);
}

public interface IMobileService
{
    Task<MobileConfigResponse> GetConfigAsync(string? tenantCode, CancellationToken ct = default);

    Task<HandoffResponse> CreateHandoffAsync(Guid userId, HandoffRequest request, CancellationToken ct = default);

    /// <summary>Redeems a hand-off code (once, with the matching verifier); null when refused.</summary>
    Task<Tabsan.EduSphere.Application.DTOs.Auth.LoginResult?> RedeemHandoffAsync(HandoffRedeemRequest request, string? ipAddress, CancellationToken ct = default);

    Task<MobileDeviceResponse> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MobileDeviceResponse>> GetDevicesAsync(Guid userId, CancellationToken ct = default);
    Task<bool> UnregisterDeviceAsync(Guid userId, string token, CancellationToken ct = default);

    Task<Guid> ReportCrashAsync(Guid? userId, CrashReportRequest request, CancellationToken ct = default);

    /// <summary>Sends a notification to the users' phones (respecting their per-type choices). Returns how many were delivered.</summary>
    Task<int> SendPushAsync(NotificationType type, string title, string body, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Is <paramref name="appVersion"/> still allowed (at least the minimum version)?</summary>
    bool IsSupportedVersion(string? appVersion);
}
