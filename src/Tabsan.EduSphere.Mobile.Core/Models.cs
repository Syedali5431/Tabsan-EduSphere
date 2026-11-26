namespace Tabsan.EduSphere.Mobile.Core;

// Plan M12 — what the app reads from the API (only the fields it shows). Property names follow the API's camelCase JSON.

/// <summary>
/// What the build says. <paramref name="ApiBaseUrl"/> is the server the app starts with: empty in the standard build, where
/// each person enters their institute's server on first start (see <see cref="ServerAddress"/>); an institution's own build
/// sets it, and with <paramref name="ServerLocked"/> nobody can switch it (see mobile/README.md).
/// </summary>
public sealed record AppSettings(string ApiBaseUrl, string AppVersion, string Platform, string AppName = "EduSphere", bool ServerLocked = false)
{
    public string ApiBase => (ApiBaseUrl ?? "").Trim().TrimEnd('/');

    /// <summary>People may choose another server (not on a build locked to its institution's server).</summary>
    public bool CanChangeServer => !(ServerLocked && ApiBase.Length > 0);
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
    /// <summary>Demo installations only: the shared demo username; typing it lets people choose the role to open.</summary>
    public string? DemoUsername { get; set; }
    /// <summary>Demo installations only: the roles the demo sign-in can open, in order (Admin, Faculty, Student, Parent).</summary>
    public List<string>? DemoRoles { get; set; }
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
    /// <summary>Course code and place: a teacher's day lists the same subject for several classes, told apart by the course.</summary>
    public string Details => string.Join(" · ", new[] { CourseCode, Where }.Where(s => !string.IsNullOrWhiteSpace(s)));
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
    /// <summary>University students only (GPA/CGPA is a University measure).</summary>
    public decimal? Cgpa { get; set; }
    public int Courses { get; set; }
    public decimal? OutstandingFees { get; set; }
    /// <summary>Published marks over maximum marks, in percent: how School and College students stand.</summary>
    public decimal? AveragePercent { get; set; }
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
    public decimal? AveragePercent { get; set; }
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
    public string? TextContent { get; set; }
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
    /// <summary>When it was sent (the inbox API's "sentAt").</summary>
    public DateTime SentAt { get; set; }
    public DateTime When => SentAt != default ? SentAt : CreatedAt;
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
    /// <summary>The class (School/College) or semester (University).</summary>
    public string? SemesterName { get; set; }
    public string? Teacher { get; set; }
    public Guid? CampusId { get; set; }
    public string? CampusName { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string Title => string.IsNullOrWhiteSpace(CourseCode) ? CourseTitle : $"{CourseCode} — {CourseTitle}";
}

/// <summary>Someone the user may write to; Role is the role name, Detail data (a course, a child), Group how the list is grouped.</summary>
public sealed class MessageContact : ObservableObject
{
    private bool _selected;
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Detail { get; set; }
    public string? Group { get; set; }
    /// <summary>Ticked on the new-message screen.</summary>
    public bool IsSelected { get => _selected; set => Set(ref _selected, value); }

    /// <summary>"Teacher · Mathematics (Class 9)", "Parent of Fatima Sheikh", "Student · TMS-GUL-09-01".</summary>
    public string Description => Role == "Parent" && !string.IsNullOrWhiteSpace(Detail)
        ? string.Format(AppText.Get("ParentOf"), Detail)
        : string.IsNullOrWhiteSpace(Detail) ? MessageText.Role(Role) : $"{MessageText.Role(Role)} · {Detail}";

    /// <summary>The group's heading: a course title as it is, or Teachers, Office, Students, Parents, Staff in the app's language.</summary>
    public string GroupTitle => Group is "Teachers" or "Office" or "Students" or "Parents" or "Staff" ? AppText.Get("Group" + Group) : Group ?? "";
}

public sealed class ConversationPerson
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}

public sealed class ConversationSummary
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = "";
    public List<ConversationPerson> With { get; set; } = [];
    public DateTime LastMessageAt { get; set; }
    public string? LastMessage { get; set; }
    public Guid? LastSenderId { get; set; }
    public string? LastSenderName { get; set; }
    public int Unread { get; set; }
    public bool Archived { get; set; }
    public bool LastFromMe { get; set; }

    public string People => string.Join(", ", With.Select(p => p.Name));
    /// <summary>"Ms Khan: See you Friday", "You: Thank you".</summary>
    public string Preview => LastMessage is null ? ""
        : LastFromMe ? $"{AppText.Get("You")}: {LastMessage}"
        : string.IsNullOrWhiteSpace(LastSenderName) ? LastMessage : $"{LastSenderName}: {LastMessage}";
    public bool HasUnread => Unread > 0;
}

public sealed class ConversationMessageItem
{
    public Guid Id { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderName { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; }
    public bool Mine { get; set; }
    public string Who => Mine ? AppText.Get("You") : SenderName;
}

public sealed class ConversationDetail
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = "";
    public List<ConversationPerson> People { get; set; } = [];
    public List<ConversationMessageItem> Messages { get; set; } = [];
    public bool Archived { get; set; }
}

public sealed class StartedConversations
{
    public List<Guid> ConversationIds { get; set; } = [];
}

public sealed class UnreadMessages
{
    public int Conversations { get; set; }
}

/// <summary>How the app names roles in messages.</summary>
public static class MessageText
{
    public static string Role(string? role) => role switch
    {
        "Faculty" => AppText.Get("Teacher"),
        "Student" => AppText.Get("Student"),
        "Parent" => AppText.Get("Parent"),
        "Finance" => AppText.Get("FinanceOffice"),
        "Admin" or "SuperAdmin" => AppText.Get("SchoolOffice"),
        _ => role ?? ""
    };
}

/// <summary>
/// A calendar entry (times UTC): "Assignment" (homework due), "Quiz" (closes), "LiveClass" or "KeyDate" (all day).
/// Status: Done, Overdue, Cancelled or Draft (teachers); parents' entries name the child.
/// </summary>
public sealed class CalendarEntry
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool AllDay { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? OfferingId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
    public string? Status { get; set; }
    public string? Note { get; set; }
    public string? StudentName { get; set; }
    public bool OwnDates { get; set; }
    public int ToMark { get; set; }
    public bool SomeHaveOwnDates { get; set; }

    /// <summary>The day it is on in the phone's time (key dates stay on their date everywhere).</summary>
    public DateTime LocalDay => AllDay ? StartsAt.Date : DateTime.SpecifyKind(StartsAt, DateTimeKind.Utc).ToLocalTime().Date;

    /// <summary>"17:00" in the phone's time, "17:00–17:45" for a live class, or "All day".</summary>
    public string Time
    {
        get
        {
            if (AllDay) return AppText.Get("AllDay");
            var start = DateTime.SpecifyKind(StartsAt, DateTimeKind.Utc).ToLocalTime().ToString("HH:mm");
            return EndsAt is { } end && end > StartsAt ? $"{start}–{DateTime.SpecifyKind(end, DateTimeKind.Utc).ToLocalTime():HH:mm}" : start;
        }
    }

    /// <summary>"Due: Essay", "Quiz closes: Week 3", "Ali: Due: Essay" (parents).</summary>
    public string What
    {
        get
        {
            var prefix = Kind switch
            {
                "Assignment" => AppText.Get("Due"),
                "Quiz" => AppText.Get("QuizCloses"),
                "LiveClass" => AppText.Get("LiveClass"),
                _ => AppText.Get("KeyDate")
            };
            var text = $"{prefix}: {Title}";
            var child = StudentName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrEmpty(child) ? text : $"{child}: {text}";
        }
    }

    /// <summary>Course, the student's own date, what is left to mark, a key date's description.</summary>
    public string Details => string.Join(" · ", new[]
    {
        Kind == "KeyDate" ? CourseTitle : CourseCode ?? CourseTitle,
        OwnDates ? AppText.Get(Kind == "Quiz" ? "OwnClosingTime" : "OwnDueDate") : null,
        ToMark > 0 ? string.Format(AppText.Get("ToGradeCount"), ToMark) : null,
        SomeHaveOwnDates ? AppText.Get("SomeOwnDates") : null,
        Note
    }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>The state to show, or null.</summary>
    public string? StatusText => Status switch
    {
        "Done" => AppText.Get(Kind == "Quiz" ? "Taken" : "HandedIn"),
        "Overdue" => AppText.Get("Overdue"),
        "Cancelled" => AppText.Get("Cancelled"),
        "Draft" => AppText.Get("NotPublished"),
        _ => null
    };
}

public sealed class CalendarEvents
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<CalendarEntry> Events { get; set; } = [];
}

/// <summary>The person's private calendar link; <see cref="Path"/> is on the API server.</summary>
public sealed class CalendarFeed
{
    public string Token { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>Homework (an assignment) of one of the user's courses — or a child's for parents.</summary>
public sealed class HomeworkItem
{
    public Guid AssignmentId { get; set; }
    public Guid OfferingId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string? SemesterName { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public decimal MaxMarks { get; set; }
    public bool Submitted { get; set; }
    public decimal? Marks { get; set; }
    public string? Status { get; set; }
    /// <summary>Teachers: handed-in work without marks yet.</summary>
    public int ToGrade { get; set; }

    /// <summary>The same homework as the course page's assignment (to open hand-in or grading).</summary>
    public CourseAssignment ToAssignment() => new()
    {
        Id = AssignmentId, Title = Title, Description = Description, DueDate = DueDate, MaxMarks = MaxMarks,
        Submitted = Submitted, Marks = Marks, Status = Status, ToGrade = ToGrade
    };
}

/// <summary>One marked day of attendance in one course.</summary>
public sealed class AttendanceDay
{
    public DateTime Date { get; set; }
    public Guid OfferingId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string? SemesterName { get; set; }
    /// <summary>Present, Absent, Late or Excused.</summary>
    public string Status { get; set; } = "";
}

/// <summary>An institute and campus the user works in.</summary>
public sealed class ScopeItem
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public Guid CampusId { get; set; }
    public string CampusName { get; set; } = "";
    public string Label => $"{TenantName} — {CampusName}";
}

/// <summary>A campus at a glance, for admins.</summary>
public sealed class CampusSummary
{
    public Guid? CampusId { get; set; }
    public string? CampusName { get; set; }
    public string? TenantName { get; set; }
    public int Students { get; set; }
    public int Teachers { get; set; }
    public int Courses { get; set; }
    public int AttendanceTodayMarked { get; set; }
    public decimal? AttendanceTodayPercent { get; set; }
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
    /// <summary>Teachers: handed-in work without marks yet (null from an API that does not count it).</summary>
    public int? ToGrade { get; set; }
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