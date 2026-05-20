using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Tabsan.EduSphere.Application.DTOs.Analytics;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.Web.Services;

public interface IEduApiClient
{
    bool IsConnected();
    bool IsForcePasswordChangeRequired();
    void SetForcePasswordChangeRequired(bool required);
    ApiConnectionModel GetConnection();
    void SaveConnection(ApiConnectionModel model);
    SessionIdentity? GetSessionIdentity();
    Task ForceChangePasswordAsync(string newPassword, CancellationToken ct);
    Task<StudentProfileSummaryItem?> GetMyStudentProfileAsync(CancellationToken ct);

    Task<List<LookupItem>> GetDepartmentsAsync(CancellationToken ct);
    Task<List<LookupItem>> GetProgramsAsync(Guid? departmentId, CancellationToken ct);
    Task<List<ProgramItem>> GetProgramDetailsAsync(Guid? departmentId, CancellationToken ct);
    Task<List<LookupItem>> GetSemestersAsync(CancellationToken ct);
    Task<List<LookupItem>> GetCoursesAsync(Guid? departmentId, CancellationToken ct);
    Task<List<FacultyLookupItem>> GetFacultyAsync(CancellationToken ct);
    Task<List<LookupItem>> GetBuildingsAsync(CancellationToken ct);
    Task<List<RoomLookupItem>> GetRoomsAsync(CancellationToken ct);
    Task<List<RoomLookupItem>> GetRoomsByBuildingAsync(Guid buildingId, CancellationToken ct);

    Task<TimetableDetailsItem?> GetTimetableByIdAsync(Guid timetableId, CancellationToken ct);
    Task<List<TimetableSummaryItem>> GetTimetablesByDepartmentAsync(Guid departmentId, CancellationToken ct);
    Task<Guid> CreateTimetableAsync(CreateTimetableForm form, CancellationToken ct);
    Task AddTimetableEntryAsync(AddTimetableEntryForm form, CancellationToken ct);
    Task PublishTimetableAsync(Guid timetableId, CancellationToken ct);
    Task<List<TeacherTimetableEntryItem>> GetTeacherEntriesAsync(CancellationToken ct);

    // Buildings
    Task<List<BuildingItem>> GetAllBuildingsAsync(bool activeOnly, CancellationToken ct);
    Task<BuildingItem?> GetBuildingByIdAsync(Guid id, CancellationToken ct);
    Task<BuildingItem> CreateBuildingAsync(BuildingFormModel form, CancellationToken ct);
    Task<BuildingItem> UpdateBuildingAsync(Guid id, BuildingFormModel form, CancellationToken ct);
    Task ActivateBuildingAsync(Guid id, CancellationToken ct);
    Task DeactivateBuildingAsync(Guid id, CancellationToken ct);

    // Rooms
    Task<List<RoomItem>> GetAllRoomsAsync(bool activeOnly, CancellationToken ct);
    Task<List<RoomItem>> GetRoomsForBuildingAsync(Guid buildingId, bool activeOnly, CancellationToken ct);
    Task<RoomItem> CreateRoomAsync(RoomFormModel form, CancellationToken ct);
    Task<RoomItem> UpdateRoomAsync(Guid id, RoomFormModel form, CancellationToken ct);
    Task ActivateRoomAsync(Guid id, CancellationToken ct);
    Task DeactivateRoomAsync(Guid id, CancellationToken ct);

    // Sidebar Settings
    Task<List<SidebarMenuItemWebModel>> GetSidebarMenusAsync(CancellationToken ct);
    Task<List<SidebarMenuItemWebModel>> GetVisibleSidebarMenusForCurrentUserAsync(CancellationToken ct);
    Task SetSidebarMenuRolesAsync(Guid id, Dictionary<string, bool> roles, CancellationToken ct);
    Task SetSidebarMenuStatusAsync(Guid id, bool isActive, CancellationToken ct);

    // License
    Task<LicenseUpdatePageModel> GetLicenseDetailsAsync(CancellationToken ct);
    Task<string> UploadLicenseAsync(Stream fileStream, string fileName, CancellationToken ct);

    // Theme
    Task<string?> GetCurrentThemeAsync(CancellationToken ct);
    Task SetThemeAsync(string? themeKey, CancellationToken ct);

    // Report Settings
    Task<List<ReportDefinitionWebModel>> GetReportDefinitionsAsync(CancellationToken ct);
    Task CreateReportDefinitionAsync(CreateReportForm form, CancellationToken ct);
    Task SetReportActiveAsync(Guid id, bool activate, CancellationToken ct);
    Task SetReportRolesAsync(Guid id, List<string> roles, CancellationToken ct);

    // Module Settings
    Task<List<ModuleSettingsWebModel>> GetModuleSettingsAsync(CancellationToken ct);
    Task SetModuleActiveAsync(string key, bool activate, CancellationToken ct);
    Task SetModuleRolesAsync(string key, List<string> roles, CancellationToken ct);

    // Result Calculation
    Task<ResultCalculationSettingsPageModel> GetResultCalculationSettingsAsync(CancellationToken ct);
    Task SaveResultCalculationSettingsAsync(ResultCalculationSettingsPageModel model, CancellationToken ct);

    // Phase 12: Reports
    Task<List<ReportCatalogItem>> GetReportCatalogAsync(CancellationToken ct);
    Task<AttendanceSummaryWebModel?> GetAttendanceSummaryReportAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<ResultSummaryWebModel?> GetResultSummaryReportAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<AssignmentSummaryWebModel?> GetAssignmentSummaryReportAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<QuizSummaryWebModel?> GetQuizSummaryReportAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<GpaReportWebModel?> GetGpaReportAsync(Guid? departmentId, Guid? programId, int? institutionType, CancellationToken ct);
    Task<EnrollmentSummaryWebModel?> GetEnrollmentSummaryReportAsync(Guid? semesterId, Guid? departmentId, int? institutionType, CancellationToken ct);
    Task<SemesterResultsWebModel?> GetSemesterResultsReportAsync(Guid semesterId, Guid? departmentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportAttendanceSummaryAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportResultSummaryAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportAttendanceSummaryCsvAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportAttendanceSummaryPdfAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportResultSummaryCsvAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportResultSummaryPdfAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportAssignmentSummaryAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportQuizSummaryAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportAssignmentSummaryCsvAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportAssignmentSummaryPdfAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportQuizSummaryCsvAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportQuizSummaryPdfAsync(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportGpaReportAsync(Guid? departmentId, Guid? programId, int? institutionType, CancellationToken ct);

    // Stage 4.2: Additional Reports
    Task<TranscriptWebModel?> GetStudentTranscriptReportAsync(Guid studentProfileId, CancellationToken ct);
    Task<LowAttendanceWebModel?> GetLowAttendanceReportAsync(decimal threshold, Guid? departmentId, Guid? courseOfferingId, int? institutionType, CancellationToken ct);
    Task<FypStatusWebModel?> GetFypStatusReportAsync(Guid? departmentId, string? status, int? institutionType, CancellationToken ct);
    Task<byte[]> ExportStudentTranscriptAsync(Guid studentProfileId, CancellationToken ct);

    // Notifications
    Task<List<NotificationItem>> GetNotificationsAsync(CancellationToken ct);
    Task<int> GetUnreadNotificationCountAsync(CancellationToken ct);
    Task MarkNotificationReadAsync(Guid id, CancellationToken ct);
    Task MarkAllNotificationsReadAsync(CancellationToken ct);

    // Students
    Task<List<StudentItem>> GetStudentsAsync(Guid? departmentId, CancellationToken ct);

    // Departments
    Task<List<DepartmentItem>> GetDepartmentDetailsAsync(CancellationToken ct);
    Task CreateProgramAsync(string name, string code, Guid departmentId, int totalSemesters, CancellationToken ct);
    Task UpdateProgramAsync(Guid id, string newName, CancellationToken ct);
    Task DeactivateProgramAsync(Guid id, CancellationToken ct);
    Task CreateDepartmentAsync(string name, string code, int institutionType, CancellationToken ct);
    Task UpdateDepartmentAsync(Guid id, string newName, int? institutionType, CancellationToken ct);
    Task DeactivateDepartmentAsync(Guid id, CancellationToken ct);
    Task<UserImportResultItem> ImportUsersCsvAsync(Stream fileStream, string fileName, CancellationToken ct);
    Task<List<AdminUserLookupItem>> GetAdminUsersAsync(CancellationToken ct);
    Task<Guid> CreateAdminUserAsync(string username, string? email, string password, int? institutionType, CancellationToken ct);
    Task UpdateAdminUserAsync(Guid userId, string? email, bool isActive, string? newPassword, CancellationToken ct);
    Task<List<Guid>> GetAdminDepartmentIdsAsync(Guid adminUserId, CancellationToken ct);
    Task AssignAdminToDepartmentAsync(Guid adminUserId, Guid departmentId, CancellationToken ct);
    Task RemoveAdminFromDepartmentAsync(Guid adminUserId, Guid departmentId, CancellationToken ct);

    // Tenant and Campus Management (Phase 5)
    Task<List<TenantItem>> GetTenantsAsync(CancellationToken ct);
    Task CreateTenantAsync(string code, string name, CancellationToken ct);
    Task UpdateTenantAsync(Guid id, string newName, CancellationToken ct);
    Task SetTenantActiveAsync(Guid id, bool activate, CancellationToken ct);

    Task<List<CampusItem>> GetCampusesAsync(Guid? tenantId, CancellationToken ct);
    Task CreateCampusAsync(Guid tenantId, string code, string name, CancellationToken ct);
    Task UpdateCampusAsync(Guid id, string newName, CancellationToken ct);
    Task SetCampusActiveAsync(Guid id, bool activate, CancellationToken ct);

    // Courses / Offerings
    Task<List<CourseItem>> GetCourseDetailsAsync(Guid? departmentId, CancellationToken ct);
    // Final-Touches Phase 19 Stage 19.3 — filtered by hasSemesters
    Task<List<CourseItem>> GetCourseDetailsByTypeAsync(bool hasSemesters, CancellationToken ct);
    Task<List<CourseOfferingItem>> GetCourseOfferingsAsync(Guid? departmentId, CancellationToken ct);
    Task<List<LookupItem>> GetMyOfferingsAsync(CancellationToken ct);
    // Final-Touches Phase 19 Stage 19.1/19.2 — extended create with semester/duration/grading
    Task CreateCourseAsync(string code, string title, int creditHours, Guid departmentId, bool hasSemesters, int? totalSemesters, int? durationValue, string? durationUnit, string? gradingType, CancellationToken ct);
    Task CreateOfferingAsync(Guid courseId, Guid semesterId, int maxEnrollment, Guid? facultyUserId, CancellationToken ct);
    Task DeactivateCourseAsync(Guid id, CancellationToken ct);
    Task DeleteOfferingAsync(Guid id, CancellationToken ct);
    // Final-Touches Phase 19 Stage 19.4 — per-course grading config
    Task<GradingConfigApiModel?> GetCourseGradingConfigAsync(Guid courseId, CancellationToken ct);
    Task<GradingConfigApiModel?> SaveCourseGradingConfigAsync(Guid courseId, decimal passThreshold, string gradingType, string? gradeRangesJson, CancellationToken ct);
    Task<List<InstitutionGradingProfileApiModel>> GetInstitutionGradingProfilesAsync(CancellationToken ct);
    Task<InstitutionGradingProfileApiModel?> SaveInstitutionGradingProfileAsync(int institutionType, decimal passThreshold, string? gradeRangesJson, bool isActive, CancellationToken ct);

    // Final-Touches Phase 20 Stage 20.1/20.2 — LMS content modules and videos
    Task<List<LmsModuleApiModel>> GetLmsModulesAsync(Guid offeringId, bool publishedOnly, CancellationToken ct);
    Task<LmsModuleApiModel?> GetLmsModuleAsync(Guid moduleId, CancellationToken ct);
    Task<LmsModuleApiModel?> CreateLmsModuleAsync(Guid offeringId, string title, int weekNumber, string? body, CancellationToken ct);
    Task UpdateLmsModuleAsync(Guid moduleId, string title, int weekNumber, string? body, CancellationToken ct);
    Task PublishLmsModuleAsync(Guid moduleId, CancellationToken ct);
    Task UnpublishLmsModuleAsync(Guid moduleId, CancellationToken ct);
    Task DeleteLmsModuleAsync(Guid moduleId, CancellationToken ct);
    Task<LmsVideoApiModel?> AddLmsVideoAsync(Guid moduleId, string title, string? storageUrl, string? embedUrl, int? durationSeconds, CancellationToken ct);
    Task DeleteLmsVideoAsync(Guid videoId, CancellationToken ct);

    // Final-Touches Phase 20 Stage 20.3 — discussion forum
    Task<List<DiscussionThreadApiModel>> GetDiscussionThreadsAsync(Guid offeringId, CancellationToken ct);
    Task<DiscussionThreadApiModel?> GetDiscussionThreadAsync(Guid threadId, CancellationToken ct);
    Task<DiscussionThreadApiModel?> CreateDiscussionThreadAsync(Guid offeringId, Guid authorId, string title, CancellationToken ct);
    Task SetThreadPinnedAsync(Guid threadId, bool pinned, CancellationToken ct);
    Task CloseDiscussionThreadAsync(Guid threadId, CancellationToken ct);
    Task ReopenDiscussionThreadAsync(Guid threadId, CancellationToken ct);
    Task DeleteDiscussionThreadAsync(Guid threadId, CancellationToken ct);
    Task<DiscussionReplyApiModel?> AddDiscussionReplyAsync(Guid threadId, Guid authorId, string body, CancellationToken ct);
    Task DeleteDiscussionReplyAsync(Guid replyId, CancellationToken ct);

    // Final-Touches Phase 20 Stage 20.4 — announcements
    Task<List<AnnouncementApiModel>> GetAnnouncementsAsync(Guid offeringId, CancellationToken ct);
    Task<AnnouncementApiModel?> CreateAnnouncementAsync(Guid? offeringId, Guid authorId, string title, string body, CancellationToken ct);
    Task DeleteAnnouncementAsync(Guid announcementId, CancellationToken ct);

    // Plan C Phase 4 — course materials UI API bindings
    Task<List<CourseMaterialApiModel>> GetCourseMaterialsAsync(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        bool activeOnly,
        CancellationToken ct);
    Task<CourseMaterialApiModel?> GetCourseMaterialByIdAsync(Guid id, CancellationToken ct);
    Task<CourseMaterialFileDownloadApiModel?> DownloadCourseMaterialFileAsync(Guid id, CancellationToken ct);
    Task<CourseMaterialUploadApiModel?> UploadCourseMaterialFileAsync(Stream fileStream, string fileName, CancellationToken ct);
    Task<CourseMaterialApiModel?> CreateCourseMaterialAsync(
        Guid departmentId,
        Guid academicProgramId,
        Guid semesterId,
        Guid courseId,
        string materialType,
        string title,
        string? description,
        string? externalUrl,
        string? blobPath,
        string? fileName,
        long? fileSizeBytes,
        bool isActive,
        CancellationToken ct);
    Task<CourseMaterialApiModel?> UpdateCourseMaterialAsync(
        Guid id,
        string materialType,
        string title,
        string? description,
        string? externalUrl,
        string? blobPath,
        string? fileName,
        long? fileSizeBytes,
        bool isActive,
        CancellationToken ct);
    Task SetCourseMaterialActiveAsync(Guid id, bool isActive, CancellationToken ct);

    // Assignments
    Task<List<AssignmentItem>> GetMyAssignmentsAsync(CancellationToken ct);
    Task<List<MyAssignmentSubmissionItem>> GetMyAssignmentSubmissionsAsync(CancellationToken ct);
    Task<List<AssignmentItem>> GetAssignmentsByOfferingAsync(Guid offeringId, CancellationToken ct);
    Task<List<SubmissionItem>> GetSubmissionsForAssignmentAsync(Guid assignmentId, CancellationToken ct);
    Task SubmitAssignmentAsync(Guid assignmentId, string? fileUrl, string? textContent, CancellationToken ct);
    Task<Guid> CreateAssignmentAsync(Guid courseOfferingId, string title, string? description, DateTime dueDate, decimal maxMarks, CancellationToken ct);
    Task UpdateAssignmentAsync(Guid id, string title, string? description, DateTime dueDate, decimal maxMarks, CancellationToken ct);
    Task PublishAssignmentAsync(Guid id, CancellationToken ct);
    Task DeleteAssignmentAsync(Guid id, CancellationToken ct);
    Task GradeSubmissionAsync(Guid assignmentId, Guid studentProfileId, decimal marksAwarded, string? feedback, CancellationToken ct);

    // Attendance
    Task<List<AttendanceSummaryItem>> GetMyAttendanceSummaryAsync(CancellationToken ct);
    Task<List<AttendanceRecordItem>> GetAttendanceByOfferingAsync(Guid offeringId, CancellationToken ct);
    Task BulkMarkAttendanceAsync(Guid offeringId, DateTime date, IEnumerable<(Guid StudentProfileId, string Status)> entries, CancellationToken ct);
    Task CorrectAttendanceAsync(Guid studentProfileId, Guid courseOfferingId, DateTime date, string newStatus, string? remarks, CancellationToken ct);

    // Results
    Task<List<ResultItem>> GetMyResultsAsync(CancellationToken ct);
    Task<List<ResultItem>> GetResultsByOfferingAsync(Guid offeringId, CancellationToken ct);
    Task CreateResultAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, decimal marksObtained, decimal maxMarks, CancellationToken ct);
    Task CorrectResultAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, decimal newMarksObtained, decimal newMaxMarks, CancellationToken ct);
    Task PublishAllResultsAsync(Guid courseOfferingId, CancellationToken ct);

    // Quizzes
    Task<List<QuizItem>> GetQuizzesByOfferingAsync(Guid offeringId, CancellationToken ct);
    Task<List<QuizAttemptItem>> GetMyAttemptsAsync(CancellationToken ct);
    Task<Guid> CreateQuizAsync(Guid courseOfferingId, string title, string? instructions, int? timeLimitMinutes, int maxAttempts, CancellationToken ct);
    Task UpdateQuizAsync(Guid id, string title, string? instructions, int? timeLimitMinutes, int maxAttempts, CancellationToken ct);
    Task PublishQuizAsync(Guid id, CancellationToken ct);
    Task DeleteQuizAsync(Guid id, CancellationToken ct);

    // FYP
    Task<List<FypProjectItem>> GetMyFypProjectsAsync(CancellationToken ct);
    Task<List<FypProjectItem>> GetAllFypProjectsAsync(CancellationToken ct);
    Task<List<FypProjectItem>> GetFypByDepartmentAsync(Guid departmentId, CancellationToken ct);
    Task<List<FypProjectItem>> GetMySupervisedProjectsAsync(CancellationToken ct);
    Task<List<FypMeetingItem>> GetUpcomingMeetingsAsync(CancellationToken ct);
    Task<Guid> ProposeFypProjectAsync(Guid departmentId, string title, string description, CancellationToken ct);
    Task<Guid> CreateFypProjectAsync(Guid studentProfileId, Guid departmentId, string title, string description, CancellationToken ct);
    Task UpdateFypProjectAsync(Guid id, string title, string description, CancellationToken ct);
    Task ApproveFypProjectAsync(Guid id, string? remarks, CancellationToken ct);
    Task RejectFypProjectAsync(Guid id, string remarks, CancellationToken ct);
    Task AssignFypSupervisorAsync(Guid id, Guid supervisorUserId, CancellationToken ct);
    Task CompleteFypProjectAsync(Guid id, CancellationToken ct);
    Task RequestFypCompletionAsync(Guid id, CancellationToken ct);
    Task ApproveFypCompletionAsync(Guid id, CancellationToken ct);

    // Analytics — Final-Touches Phase 6 Stage 6.2: typed return instead of raw JSON strings
    Task<DepartmentPerformanceReport?> GetPerformanceAnalyticsAsync(Guid? departmentId, int? institutionType, Guid? courseId, Guid? semesterId, CancellationToken ct);
    Task<DepartmentAttendanceReport?> GetAttendanceAnalyticsAsync(Guid? departmentId, int? institutionType, Guid? courseId, Guid? semesterId, CancellationToken ct);
    Task<AssignmentStatsReport?> GetAssignmentAnalyticsAsync(Guid? departmentId, int? institutionType, Guid? courseId, Guid? semesterId, CancellationToken ct);

    // AI Chat
    Task<List<AiChatConversationItem>> GetChatConversationsAsync(CancellationToken ct);
    Task<List<AiChatMessageItem>> GetChatMessagesAsync(Guid conversationId, CancellationToken ct);
    Task<AiChatSendResultItem?> SendChatMessageAsync(Guid? conversationId, string message, CancellationToken ct);

    // Student Lifecycle
    Task<List<GraduationCandidateItem>> GetGraduationCandidatesAsync(Guid departmentId, CancellationToken ct);
    Task GraduateStudentAsync(Guid studentId, CancellationToken ct);
    Task GraduateStudentsBatchAsync(List<Guid> studentIds, CancellationToken ct);
    Task<List<StudentItem>> GetStudentsByAcademicLevelAsync(Guid departmentId, int levelNumber, CancellationToken ct);
    Task<List<StudentItem>> GetStudentsBySemesterAsync(Guid departmentId, int semesterNumber, CancellationToken ct);
    Task PromoteStudentAsync(Guid studentId, CancellationToken ct);

    // Payments
    Task<PaymentReceiptPageItem> GetPaymentsByStudentAsync(Guid studentId, int page, int pageSize, CancellationToken ct);
    // Final-Touches Phase 7 — admin all-receipts, student own, create, confirm, cancel, submit proof
    Task<PaymentReceiptPageItem> GetAllPaymentsAsync(int page, int pageSize, CancellationToken ct);
    Task<PaymentReceiptPageItem> GetMyPaymentsAsync(int page, int pageSize, CancellationToken ct);
    Task CreatePaymentAsync(Guid studentProfileId, decimal amount, string description, DateTime dueDate, CancellationToken ct);
    Task UpdatePaymentAsync(Guid receiptId, decimal amount, string description, DateTime dueDate, string? notes, CancellationToken ct);
    Task ConfirmPaymentAsync(Guid receiptId, CancellationToken ct);
    Task CancelPaymentAsync(Guid receiptId, CancellationToken ct);
    Task SubmitProofAsync(Guid receiptId, string proofNote, CancellationToken ct);

    // Enrollments
    Task<List<EnrollmentRosterItem>> GetEnrollmentRosterAsync(Guid offeringId, CancellationToken ct);
    // Final-Touches Phase 8 Stage 8.1+8.2 — student my-courses, admin enroll/drop, student enroll/drop
    Task<List<MyEnrollmentItem>> GetMyEnrollmentsAsync(CancellationToken ct);
    Task AdminEnrollStudentAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct);
    Task AdminDropEnrollmentAsync(Guid enrollmentId, CancellationToken ct);
    Task StudentEnrollAsync(Guid courseOfferingId, CancellationToken ct);
    Task StudentDropEnrollmentAsync(Guid courseOfferingId, CancellationToken ct);

    // Portal / Dashboard Settings
    Task<PortalBrandingWebModel> GetPortalBrandingAsync(CancellationToken ct);
    Task SavePortalBrandingAsync(PortalBrandingWebModel model, CancellationToken ct);
    Task<string?> UploadLogoAsync(Stream fileStream, string fileName, CancellationToken ct);

    // Phase 12: Academic Calendar & Deadlines
    Task<List<DeadlineWebItem>> GetCalendarDeadlinesAsync(Guid? semesterId, CancellationToken ct);
    Task<DeadlineWebItem?> GetCalendarDeadlineByIdAsync(Guid id, CancellationToken ct);
    Task CreateCalendarDeadlineAsync(DeadlineFormModel form, CancellationToken ct);
    Task UpdateCalendarDeadlineAsync(Guid id, DeadlineFormModel form, CancellationToken ct);
    Task DeleteCalendarDeadlineAsync(Guid id, CancellationToken ct);

    // Phase 13: Global Search
    Task<SearchWebResponse> SearchAsync(string term, int limit, CancellationToken ct);

    // Phase 14: Helpdesk
    Task<TicketSummaryPageItem> GetTicketsAsync(TicketStatusWeb? status, int page, int pageSize, CancellationToken ct);
    Task<TicketDetailItem?> GetTicketDetailAsync(Guid ticketId, CancellationToken ct);
    Task<Guid> CreateTicketAsync(Guid? departmentId, TicketCategoryWeb category, string subject, string body, CancellationToken ct);
    Task<Guid> AddTicketMessageAsync(Guid ticketId, string body, bool isInternalNote, CancellationToken ct);
    Task AssignTicketAsync(Guid ticketId, Guid assignedToId, CancellationToken ct);
    Task ResolveTicketAsync(Guid ticketId, CancellationToken ct);
    Task CloseTicketAsync(Guid ticketId, CancellationToken ct);
    Task ReopenTicketAsync(Guid ticketId, CancellationToken ct);

    // Phase 15: Enrollment Rules — Prerequisites
    Task<List<PrerequisiteWebItem>> GetPrerequisitesAsync(Guid courseId, CancellationToken ct);
    Task AddPrerequisiteAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct);
    Task RemovePrerequisiteAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct);

    // Phase 16: Faculty Grading System
    Task<GradebookGridWebModel?> GetGradebookAsync(Guid offeringId, CancellationToken ct);
    Task UpsertGradebookEntryAsync(Guid offeringId, Guid studentProfileId, string componentName, decimal marksObtained, decimal maxMarks, CancellationToken ct);
    Task PublishGradebookAllAsync(Guid offeringId, CancellationToken ct);
    Task<byte[]> GetGradebookCsvTemplateAsync(Guid offeringId, string componentName, CancellationToken ct);
    Task<BulkGradePreviewWebModel?> UploadBulkGradeCsvAsync(Guid offeringId, string componentName, Stream csvStream, string fileName, CancellationToken ct);
    Task ConfirmBulkGradeAsync(Guid offeringId, BulkGradeConfirmWebRequest request, CancellationToken ct);
    Task<RubricWebModel?> GetRubricByAssignmentAsync(Guid assignmentId, CancellationToken ct);
    Task<Guid> CreateRubricAsync(CreateRubricWebRequest request, CancellationToken ct);
    Task UpdateRubricAsync(Guid rubricId, string title, CancellationToken ct);
    Task DeleteRubricAsync(Guid rubricId, CancellationToken ct);
    Task<RubricGradeWebModel?> GetRubricGradeAsync(Guid rubricId, Guid submissionId, CancellationToken ct);
    Task<RubricGradeWebModel?> GradeRubricSubmissionAsync(Guid rubricId, RubricGradeWebRequest request, CancellationToken ct);

    // Phase 17: Degree Audit System
    // Final-Touches Phase 17 Stage 17.1 — student own audit
    Task<DegreeAuditWebModel?> GetMyDegreeAuditAsync(CancellationToken ct);
    // Final-Touches Phase 17 Stage 17.1 — admin/faculty audit for a student
    Task<DegreeAuditWebModel?> GetStudentDegreeAuditAsync(Guid studentProfileId, CancellationToken ct);
    // Final-Touches Phase 17 Stage 17.2 — eligibility list
    Task<List<EligibilityListWebItem>> GetEligibilityListAsync(Guid? departmentId, Guid? programId, CancellationToken ct);
    // Final-Touches Phase 17 Stage 17.2 — degree rules
    Task<List<DegreeRuleWebModel>> GetAllDegreeRulesAsync(CancellationToken ct);
    Task<DegreeRuleWebModel?> GetDegreeRuleByProgramAsync(Guid programId, CancellationToken ct);
    Task<DegreeRuleWebModel?> CreateDegreeRuleAsync(CreateDegreeRuleWebRequest request, CancellationToken ct);
    Task<DegreeRuleWebModel?> UpdateDegreeRuleAsync(Guid ruleId, UpdateDegreeRuleWebRequest request, CancellationToken ct);
    Task DeleteDegreeRuleAsync(Guid ruleId, CancellationToken ct);
    // Final-Touches Phase 17 Stage 17.3 — course type tagging
    Task SetCourseTypeAsync(Guid courseId, string courseType, CancellationToken ct);

    // Phase 18: Graduation Workflow
    // Final-Touches Phase 18 Stage 18.1 — student own applications
    Task<GraduationApplicationPageItem?> GetMyGraduationApplicationsAsync(int page, int pageSize, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.1 — application detail
    Task<GraduationApplicationDetailWebModel?> GetGraduationApplicationDetailAsync(Guid applicationId, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.1 — admin/superadmin list
    Task<GraduationApplicationPageItem?> GetGraduationApplicationsAsync(Guid? departmentId, string? status, int page, int pageSize, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.1 — student submit
    Task<GraduationApplicationWebModel?> SubmitGraduationApplicationAsync(string? studentNote, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.1 — faculty approve/reject
    Task<GraduationApplicationWebModel?> FacultyApproveApplicationAsync(Guid applicationId, bool isApproved, string? note, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.1 — admin approve/reject
    Task<GraduationApplicationWebModel?> AdminApproveApplicationAsync(Guid applicationId, bool isApproved, string? note, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.1 — superadmin final approve/reject
    Task<GraduationApplicationWebModel?> FinalApproveApplicationAsync(Guid applicationId, bool isApproved, string? note, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.1 — reject at caller's stage
    Task<GraduationApplicationWebModel?> RejectApplicationAsync(Guid applicationId, string? reason, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.2 — download certificate bytes
    Task<byte[]?> DownloadCertificateAsync(Guid applicationId, CancellationToken ct);
    // Final-Touches Phase 18 Stage 18.2 — regenerate certificate
    Task<string?> RegenerateCertificateAsync(Guid applicationId, CancellationToken ct);

    // Final-Touches Phase 21 Stage 21.1/21.2 — Study Planner
    Task<List<StudyPlanApiModel>> GetStudyPlansAsync(Guid studentProfileId, CancellationToken ct);
    Task<List<StudyPlanApiModel>> GetStudyPlansByDepartmentAsync(Guid departmentId, CancellationToken ct);
    Task<StudyPlanApiModel?> GetStudyPlanAsync(Guid planId, CancellationToken ct);
    Task<StudyPlanApiModel?> CreateStudyPlanAsync(Guid studentProfileId, string plannedSemesterName, string? notes, CancellationToken ct);
    Task<StudyPlanApiModel?> AddStudyPlanCourseAsync(Guid planId, Guid courseId, CancellationToken ct);
    Task RemoveStudyPlanCourseAsync(Guid planId, Guid courseId, CancellationToken ct);
    Task DeleteStudyPlanAsync(Guid planId, CancellationToken ct);
    Task AdvisePlanAsync(Guid planId, bool isEndorsed, string? advisorNotes, CancellationToken ct);
    Task<StudyPlanRecommendationApiModel?> GetStudyPlanRecommendationsAsync(Guid studentProfileId, string plannedSemesterName, CancellationToken ct);

    // Final-Touches Phase 22 — External Integrations
    Task<LibraryConfigApiModel?> GetLibraryConfigAsync(CancellationToken ct);
    Task SaveLibraryConfigAsync(LibraryConfigApiModel model, CancellationToken ct);
    Task<LibraryLoansApiModel?> GetMyLibraryLoansAsync(CancellationToken ct);
    Task<List<AccreditationTemplateApiModel>> GetAccreditationTemplatesAsync(CancellationToken ct);
    Task<AccreditationTemplateApiModel?> GetAccreditationTemplateAsync(Guid id, CancellationToken ct);
    Task<AccreditationTemplateApiModel?> CreateAccreditationTemplateAsync(CreateAccreditationTemplateForm form, CancellationToken ct);
    Task<AccreditationTemplateApiModel?> UpdateAccreditationTemplateAsync(Guid id, UpdateAccreditationTemplateForm form, CancellationToken ct);
    Task DeleteAccreditationTemplateAsync(Guid id, CancellationToken ct);
    Task<(byte[]? Content, string ContentType, string FileName)> GenerateAccreditationReportAsync(Guid id, CancellationToken ct);

    // Phase 23 — Institution Policy
    Task<InstitutionPolicyApiModel?> GetInstitutionPolicyAsync(CancellationToken ct);
    Task SaveInstitutionPolicyAsync(InstitutionPolicyApiModel model, CancellationToken ct);

    // Phase 24 — Dynamic Module & UI Composition
    Task<DashboardCompositionContextApiModel?> GetDashboardCompositionContextAsync(CancellationToken ct);
    Task<List<ModuleVisibilityApiModel>> GetVisibleModulesAsync(CancellationToken ct);
    Task<AcademicVocabularyApiModel?> GetVocabularyAsync(CancellationToken ct);
    Task<List<WidgetDescriptorApiModel>> GetDashboardWidgetsAsync(CancellationToken ct);
    // Phase 27 — Student Portal Capability Matrix
    Task<PortalCapabilityMatrixApiModel?> GetPortalCapabilityMatrixAsync(CancellationToken ct);
}

public class EduApiClient : IEduApiClient
{
    private const string ConnectionItemKey = "Tabsan.EduSphere.CurrentConnection";
    private const string ApiUrlKey    = "Tabsan.EduSphere.ApiBaseUrl";
    private const string ApiTokenKey  = "Tabsan.EduSphere.ApiAccessToken";
    private const string ApiRefreshTokenKey = "Tabsan.EduSphere.ApiRefreshToken";
    private const string DepartmentKey = "Tabsan.EduSphere.DefaultDepartmentId";
    private const string IdentityKey  = "Tabsan.EduSphere.SessionIdentity";
    private const string ForcePasswordChangeKey = "Tabsan.EduSphere.ForcePasswordChangeRequired";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _protector;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public EduApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IDataProtectionProvider dataProtectionProvider)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _protector = dataProtectionProvider.CreateProtector("Tabsan.EduSphere.Web.EduApiClientCookies.v1");
    }

    // â”€â”€ Connection â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public bool IsConnected()
    {
        var c = GetConnection();
        return !string.IsNullOrWhiteSpace(c.ApiBaseUrl) && !string.IsNullOrWhiteSpace(c.AccessToken);
    }

    public bool IsForcePasswordChangeRequired()
    {
        var raw = ReadCookie(ForcePasswordChangeKey);
        return bool.TryParse(raw, out var required) && required;
    }

    public void SetForcePasswordChangeRequired(bool required)
    {
        WriteCookie(ForcePasswordChangeKey, required.ToString());

        var identity = GetSessionIdentity() ?? new SessionIdentity();
        identity.MustChangePassword = required;
        WriteCookie(IdentityKey, JsonSerializer.Serialize(identity, _jsonOptions));
    }

    public ApiConnectionModel GetConnection()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(ConnectionItemKey, out var scopedConnection) == true
            && scopedConnection is ApiConnectionModel cached)
        {
            return cached;
        }

        var baseUrl = ReadCookie(ApiUrlKey) ?? string.Empty;
        var token   = ReadCookie(ApiTokenKey) ?? string.Empty;
        var refreshToken = ReadCookie(ApiRefreshTokenKey) ?? string.Empty;
        var rawDept = ReadCookie(DepartmentKey);
        Guid? dept = Guid.TryParse(rawDept, out var parsed) ? parsed : null;

        var connection = new ApiConnectionModel
        {
            ApiBaseUrl = baseUrl,
            AccessToken = token,
            RefreshToken = refreshToken,
            DefaultDepartmentId = dept
        };

        if (context is not null)
            context.Items[ConnectionItemKey] = connection;

        return connection;
    }

    public void SaveConnection(ApiConnectionModel model)
    {
        var context = _httpContextAccessor.HttpContext;

        if (string.IsNullOrWhiteSpace(model.ApiBaseUrl) || string.IsNullOrWhiteSpace(model.AccessToken))
        {
            DeleteCookie(ApiUrlKey);
            DeleteCookie(ApiTokenKey);
            DeleteCookie(ApiRefreshTokenKey);
            DeleteCookie(DepartmentKey);
            DeleteCookie(IdentityKey);
            DeleteCookie(ForcePasswordChangeKey);
            if (context is not null)
                context.Items[ConnectionItemKey] = new ApiConnectionModel();
            return;
        }

        WriteCookie(ApiUrlKey, model.ApiBaseUrl.TrimEnd('/'));
        WriteCookie(ApiTokenKey, model.AccessToken.Trim());
        var refreshTokenToStore = !string.IsNullOrWhiteSpace(model.RefreshToken)
            ? model.RefreshToken.Trim()
            : (ReadCookie(ApiRefreshTokenKey) ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(refreshTokenToStore))
            WriteCookie(ApiRefreshTokenKey, refreshTokenToStore);
        else
            DeleteCookie(ApiRefreshTokenKey);

        if (model.DefaultDepartmentId.HasValue)
            WriteCookie(DepartmentKey, model.DefaultDepartmentId.Value.ToString());
        else
            DeleteCookie(DepartmentKey);

        // Decode JWT and persist identity claims into session
        var identity = DecodeJwtIdentity(model.AccessToken.Trim());
        var json = JsonSerializer.Serialize(identity, _jsonOptions);
        WriteCookie(IdentityKey, json);
        DeleteCookie(ForcePasswordChangeKey);

        if (context is not null)
        {
            context.Items[ConnectionItemKey] = new ApiConnectionModel
            {
                ApiBaseUrl = model.ApiBaseUrl.TrimEnd('/'),
                AccessToken = model.AccessToken.Trim(),
                RefreshToken = refreshTokenToStore,
                DefaultDepartmentId = model.DefaultDepartmentId
            };
        }
    }

    public SessionIdentity? GetSessionIdentity()
    {
        var raw = ReadCookie(IdentityKey);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return JsonSerializer.Deserialize<SessionIdentity>(raw, _jsonOptions); }
        catch { return null; }
    }

    public async Task<StudentProfileSummaryItem?> GetMyStudentProfileAsync(CancellationToken ct)
    {
        var raw = await GetAsync<StudentProfileApiDto>("api/v1/student/profile", ct);
        if (raw is null) return null;

        return new StudentProfileSummaryItem
        {
            Id = raw.Id,
            DepartmentId = raw.DepartmentId,
            DepartmentName = raw.DeptName ?? "",
            CurrentSemesterNumber = raw.CurrentSemesterNumber
        };
    }

    public Task ForceChangePasswordAsync(string newPassword, CancellationToken ct)
        => PostAsync<object, object>("api/v1/auth/force-change-password", new { newPassword }, ct);

    // â”€â”€ Lookup GETs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<List<LookupItem>> GetDepartmentsAsync(CancellationToken ct)
        => await GetAsync<List<LookupItem>>("api/v1/department", ct) ?? new();

    public async Task<List<LookupItem>> GetProgramsAsync(Guid? departmentId, CancellationToken ct)
    {
        var path = departmentId.HasValue
            ? $"api/v1/program?departmentId={departmentId.Value}"
            : "api/v1/program";
        return await GetAsync<List<LookupItem>>(path, ct) ?? new();
    }

    public async Task<List<ProgramItem>> GetProgramDetailsAsync(Guid? departmentId, CancellationToken ct)
    {
        var path = departmentId.HasValue
            ? $"api/v1/program?departmentId={departmentId.Value}"
            : "api/v1/program";
        var raw = await GetAsync<List<ProgramDetailDto>>(path, ct) ?? new();
        return raw.Select(p => new ProgramItem
        {
            Id = p.Id,
            Name = p.Name ?? "",
            Code = p.Code ?? "",
            DepartmentId = p.DepartmentId,
            TotalSemesters = p.TotalSemesters,
            IsActive = p.IsActive
        }).ToList();
    }

    public async Task<List<LookupItem>> GetSemestersAsync(CancellationToken ct)
        => await GetAsync<List<LookupItem>>("api/v1/semester", ct) ?? new();

    public async Task<List<LookupItem>> GetCoursesAsync(Guid? departmentId, CancellationToken ct)
    {
        var path = departmentId.HasValue
            ? $"api/v1/course?departmentId={departmentId.Value}"
            : "api/v1/course";
        return await GetAsync<List<LookupItem>>(path, ct) ?? new();
    }

    public async Task<List<FacultyLookupItem>> GetFacultyAsync(CancellationToken ct)
        => await GetAsync<List<FacultyLookupItem>>("api/v1/timetable/faculty", ct) ?? new();

    public async Task<List<LookupItem>> GetBuildingsAsync(CancellationToken ct)
        => await GetAsync<List<LookupItem>>("api/v1/building", ct) ?? new();

    public async Task<List<RoomLookupItem>> GetRoomsAsync(CancellationToken ct)
        => await GetAsync<List<RoomLookupItem>>("api/v1/room", ct) ?? new();

    public async Task<List<RoomLookupItem>> GetRoomsByBuildingAsync(Guid buildingId, CancellationToken ct)
        => await GetAsync<List<RoomLookupItem>>($"api/v1/room/building/{buildingId}", ct) ?? new();

    // â”€â”€ Timetable â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public Task<TimetableDetailsItem?> GetTimetableByIdAsync(Guid timetableId, CancellationToken ct)
        => GetAsync<TimetableDetailsItem>($"api/v1/timetable/{timetableId}", ct);

    public async Task<List<TimetableSummaryItem>> GetTimetablesByDepartmentAsync(Guid departmentId, CancellationToken ct)
        => await GetAsync<List<TimetableSummaryItem>>($"api/v1/timetable/department/{departmentId}", ct) ?? new();

    public async Task<Guid> CreateTimetableAsync(CreateTimetableForm form, CancellationToken ct)
    {
        var result = await PostAsync<CreateTimetableForm, TimetableCreateResponse>("api/v1/timetable", form, ct)
            ?? throw new InvalidOperationException("Timetable create API returned no body.");
        return result.Id;
    }

    public async Task AddTimetableEntryAsync(AddTimetableEntryForm form, CancellationToken ct)
    {
        var payload = new
        {
            form.DayOfWeek,
            form.StartTime,
            form.EndTime,
            form.SubjectName,
            form.CourseId,
            form.FacultyUserId,
            form.FacultyName,
            form.RoomId,
            form.RoomNumber,
            form.BuildingId
        };
        await PostAsync<object, object>($"api/v1/timetable/{form.TimetableId}/entries", payload, ct);
    }

    public async Task PublishTimetableAsync(Guid timetableId, CancellationToken ct)
        => await PostAsync<object, object>($"api/v1/timetable/{timetableId}/publish", new { }, ct);

    public async Task<List<TeacherTimetableEntryItem>> GetTeacherEntriesAsync(CancellationToken ct)
        => await GetAsync<List<TeacherTimetableEntryItem>>("api/v1/timetable/mine/teacher", ct) ?? new();

    // â”€â”€ Buildings â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<List<BuildingItem>> GetAllBuildingsAsync(bool activeOnly, CancellationToken ct)
        => await GetAsync<List<BuildingItem>>($"api/v1/building?activeOnly={activeOnly}", ct) ?? new();

    public Task<BuildingItem?> GetBuildingByIdAsync(Guid id, CancellationToken ct)
        => GetAsync<BuildingItem>($"api/v1/building/{id}", ct);

    public async Task<BuildingItem> CreateBuildingAsync(BuildingFormModel form, CancellationToken ct)
        => await PostAsync<BuildingFormModel, BuildingItem>("api/v1/building", form, ct)
           ?? throw new InvalidOperationException("Building create returned no body.");

    public async Task<BuildingItem> UpdateBuildingAsync(Guid id, BuildingFormModel form, CancellationToken ct)
        => await PutAsync<BuildingFormModel, BuildingItem>($"api/v1/building/{id}", form, ct)
           ?? throw new InvalidOperationException("Building update returned no body.");

    public Task ActivateBuildingAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/building/{id}/activate", new { }, ct);

    public Task DeactivateBuildingAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/building/{id}/deactivate", new { }, ct);

    // â”€â”€ Rooms â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<List<RoomItem>> GetAllRoomsAsync(bool activeOnly, CancellationToken ct)
        => await GetAsync<List<RoomItem>>($"api/v1/room?activeOnly={activeOnly}", ct) ?? new();

    public async Task<List<RoomItem>> GetRoomsForBuildingAsync(Guid buildingId, bool activeOnly, CancellationToken ct)
        => await GetAsync<List<RoomItem>>($"api/v1/room/building/{buildingId}?activeOnly={activeOnly}", ct) ?? new();

    public async Task<RoomItem> CreateRoomAsync(RoomFormModel form, CancellationToken ct)
        => await PostAsync<RoomFormModel, RoomItem>("api/v1/room", form, ct)
           ?? throw new InvalidOperationException("Room create returned no body.");

    public async Task<RoomItem> UpdateRoomAsync(Guid id, RoomFormModel form, CancellationToken ct)
        => await PutAsync<RoomFormModel, RoomItem>($"api/v1/room/{id}", form, ct)
           ?? throw new InvalidOperationException("Room update returned no body.");

    public Task ActivateRoomAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/room/{id}/activate", new { }, ct);

    public Task DeactivateRoomAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/room/{id}/deactivate", new { }, ct);

    // â”€â”€ HTTP helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€


    // Phase 18: Graduation Workflow

    public async Task<GraduationApplicationPageItem?> GetMyGraduationApplicationsAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = $"api/v1/graduation/my?page={page}&pageSize={pageSize}";
        var raw = await GetAsync<GraduationApplicationPageApiDto>(query, ct);
        return raw is null ? null : MapGradPage(raw);
    }

    public async Task<GraduationApplicationDetailWebModel?> GetGraduationApplicationDetailAsync(Guid applicationId, CancellationToken ct)
    {
        var raw = await GetAsync<GraduationApplicationDetailApiDto>($"api/v1/graduation/{applicationId}", ct);
        if (raw is null) return null;
        return new GraduationApplicationDetailWebModel
        {
            Id = raw.Id, StudentProfileId = raw.StudentProfileId,
            StudentName = raw.StudentName ?? "", RegistrationNumber = raw.RegistrationNumber ?? "",
            ProgramName = raw.ProgramName ?? "", Status = raw.Status ?? "",
            StudentNote = raw.StudentNote, SubmittedAt = raw.SubmittedAt, UpdatedAt = raw.UpdatedAt,
            HasCertificate = raw.HasCertificate, CertificatePath = raw.CertificatePath,
            ApprovalHistory = raw.ApprovalHistory?.Select(a => new ApprovalHistoryWebItem
            { Stage = a.Stage ?? "", ApproverName = a.ApproverName ?? "", IsApproved = a.IsApproved, Note = a.Note, ActedAt = a.ActedAt }).ToList() ?? new()
        };
    }

    public async Task<GraduationApplicationPageItem?> GetGraduationApplicationsAsync(Guid? departmentId, string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = "api/v1/graduation";
        var parts = new System.Collections.Generic.List<string>();
        if (departmentId.HasValue) parts.Add($"departmentId={departmentId}");
        if (!string.IsNullOrEmpty(status)) parts.Add($"status={status}");
        parts.Add($"page={page}");
        parts.Add($"pageSize={pageSize}");
        if (parts.Count > 0) query += "?" + string.Join("&", parts);
        var raw = await GetAsync<GraduationApplicationPageApiDto>(query, ct);
        return raw is null ? null : MapGradPage(raw);
    }

    public async Task<GraduationApplicationWebModel?> SubmitGraduationApplicationAsync(string? studentNote, CancellationToken ct)
    {
        var payload = new { StudentNote = studentNote };
        var raw = await PostAsync<object, GraduationApplicationApiDto>("api/v1/graduation/submit", payload, ct);
        return raw is null ? null : MapGradApp(raw);
    }

    public async Task<GraduationApplicationWebModel?> FacultyApproveApplicationAsync(Guid applicationId, bool isApproved, string? note, CancellationToken ct)
    {
        var payload = new { IsApproved = isApproved, Note = note };
        var raw = await PostAsync<object, GraduationApplicationApiDto>($"api/v1/graduation/{applicationId}/faculty-approve", payload, ct);
        return raw is null ? null : MapGradApp(raw);
    }

    public async Task<GraduationApplicationWebModel?> AdminApproveApplicationAsync(Guid applicationId, bool isApproved, string? note, CancellationToken ct)
    {
        var payload = new { IsApproved = isApproved, Note = note };
        var raw = await PostAsync<object, GraduationApplicationApiDto>($"api/v1/graduation/{applicationId}/admin-approve", payload, ct);
        return raw is null ? null : MapGradApp(raw);
    }

    public async Task<GraduationApplicationWebModel?> FinalApproveApplicationAsync(Guid applicationId, bool isApproved, string? note, CancellationToken ct)
    {
        var payload = new { IsApproved = isApproved, Note = note };
        var raw = await PostAsync<object, GraduationApplicationApiDto>($"api/v1/graduation/{applicationId}/final-approve", payload, ct);
        return raw is null ? null : MapGradApp(raw);
    }

    public async Task<GraduationApplicationWebModel?> RejectApplicationAsync(Guid applicationId, string? reason, CancellationToken ct)
    {
        var payload = new { IsApproved = false, Note = reason };
        var raw = await PostAsync<object, GraduationApplicationApiDto>($"api/v1/graduation/{applicationId}/reject", payload, ct);
        return raw is null ? null : MapGradApp(raw);
    }

    public async Task<byte[]?> DownloadCertificateAsync(Guid applicationId, CancellationToken ct)
    {
        var connection = GetConnection();
        if (connection is null) return null;
        using var request = CreateRequest(System.Net.Http.HttpMethod.Get, $"api/v1/graduation/{applicationId}/certificate");
        using var response = await CreateClient().SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<string?> RegenerateCertificateAsync(Guid applicationId, CancellationToken ct)
    {
        var raw = await PostAsync<object, System.Collections.Generic.Dictionary<string, string>>($"api/v1/graduation/{applicationId}/regenerate-certificate", new { }, ct);
        return raw?.GetValueOrDefault("path");
    }

    private static GraduationApplicationWebModel MapGradApp(GraduationApplicationApiDto raw) => new()
    {
        Id = raw.Id, StudentProfileId = raw.StudentProfileId, StudentName = raw.StudentName ?? "",
        RegistrationNumber = raw.RegistrationNumber ?? "", ProgramName = raw.ProgramName ?? "",
        Status = raw.Status ?? "", SubmittedAt = raw.SubmittedAt, UpdatedAt = raw.UpdatedAt, HasCertificate = raw.HasCertificate
    };

    private static GraduationApplicationPageItem MapGradPage(GraduationApplicationPageApiDto raw) => new()
    {
        Items = raw.Items?.Select(MapGradApp).ToList() ?? new(),
        Page = raw.Page,
        PageSize = raw.PageSize,
        TotalCount = raw.TotalCount
    };

    private class GraduationApplicationApiDto
    {
        public Guid Id { get; set; } public Guid StudentProfileId { get; set; }
        public string? StudentName { get; set; } public string? RegistrationNumber { get; set; }
        public string? ProgramName { get; set; } public string? Status { get; set; }
        public DateTime? SubmittedAt { get; set; } public DateTime? UpdatedAt { get; set; }
        public bool HasCertificate { get; set; }
    }

    private sealed class GraduationApplicationPageApiDto
    {
        public System.Collections.Generic.List<GraduationApplicationApiDto>? Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    private sealed class GraduationApplicationDetailApiDto : GraduationApplicationApiDto
    {
        public string? StudentNote { get; set; } public string? CertificatePath { get; set; }
        public System.Collections.Generic.List<ApprovalHistoryApiDto>? ApprovalHistory { get; set; }
    }

    private sealed class ApprovalHistoryApiDto
    {
        public string? Stage { get; set; } public string? ApproverName { get; set; }
        public bool IsApproved { get; set; } public string? Note { get; set; } public DateTime ActedAt { get; set; }
    }
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, path), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);
        return string.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize<T>(body, _jsonOptions);
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, path);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);
        return string.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize<TResponse>(body, _jsonOptions);
    }

    private async Task<TResponse?> PutAsync<TRequest, TResponse>(string path, TRequest payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Put, path);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);
        return string.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize<TResponse>(body, _jsonOptions);
    }

    private async Task DeleteAsync(string path, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Delete, path), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);
    }

    private async Task DeleteWithBodyAsync(string path, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Delete, path);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);
    }

    // Dedicated byte-array downloader for binary responses (Excel exports).
    // Cannot reuse GetAsync<T> here because that method reads the body as JSON
    // and attempts to deserialize it, which corrupts binary xlsx content.
    private async Task<byte[]> GetBytesAsync(string path, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, path), ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw BuildException(response.StatusCode, body);
        }
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<HttpResponseMessage> SendWithAutoRefreshAsync(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var client = CreateClient();

        using var firstRequest = requestFactory();
        var firstResponse = await client.SendAsync(firstRequest, ct);
        if (firstResponse.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return firstResponse;

        firstResponse.Dispose();

        if (!await TryRefreshAccessTokenAsync(ct))
        {
            using var retryWithoutRefresh = requestFactory();
            return await client.SendAsync(retryWithoutRefresh, ct);
        }

        using var retryRequest = requestFactory();
        return await client.SendAsync(retryRequest, ct);
    }

    private async Task<bool> TryRefreshAccessTokenAsync(CancellationToken ct)
    {
        var connection = GetConnection();
        if (string.IsNullOrWhiteSpace(connection.ApiBaseUrl) || string.IsNullOrWhiteSpace(connection.RefreshToken))
            return false;

        await _refreshLock.WaitAsync(ct);
        try
        {
            var latest = GetConnection();
            if (string.IsNullOrWhiteSpace(latest.ApiBaseUrl) || string.IsNullOrWhiteSpace(latest.RefreshToken))
                return false;

            var refreshUri = new Uri(new Uri(latest.ApiBaseUrl.TrimEnd('/') + "/"), "api/v1/auth/refresh");
            var payload = JsonSerializer.Serialize(new { refreshToken = latest.RefreshToken }, _jsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, refreshUri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClientFactory.CreateClient().SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    SaveConnection(new ApiConnectionModel());
                    SetForcePasswordChangeRequired(false);
                }
                return false;
            }

            var refreshed = string.IsNullOrWhiteSpace(body)
                ? null
                : JsonSerializer.Deserialize<RefreshLoginApiResponse>(body, _jsonOptions);

            if (refreshed is null || string.IsNullOrWhiteSpace(refreshed.AccessToken))
                return false;

            SaveConnection(new ApiConnectionModel
            {
                ApiBaseUrl = latest.ApiBaseUrl,
                AccessToken = refreshed.AccessToken,
                RefreshToken = string.IsNullOrWhiteSpace(refreshed.RefreshToken) ? latest.RefreshToken : refreshed.RefreshToken,
                DefaultDepartmentId = latest.DefaultDepartmentId
            });
            SetForcePasswordChangeRequired(refreshed.MustChangePassword);
            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("EduApi");

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var connection = GetConnection();
        if (string.IsNullOrWhiteSpace(connection.ApiBaseUrl) || string.IsNullOrWhiteSpace(connection.AccessToken))
            throw new InvalidOperationException("API connection is not configured. Set base URL and access token first.");

        var uri     = new Uri(new Uri(connection.ApiBaseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        return request;
    }

    private static Exception BuildException(System.Net.HttpStatusCode statusCode, string body)
    {
        var message = string.IsNullOrWhiteSpace(body)
            ? $"API request failed with status {(int)statusCode}."
            : body;
        return new InvalidOperationException(message);
    }

    private sealed record RefreshLoginApiResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        string Role,
        Guid UserId,
        string Username,
        bool MustChangePassword,
        bool MfaEnabled,
        bool SsoEnabled,
        string? SsoProvider,
        string SessionRiskLevel);

    private string? ReadCookie(string key)
    {
        var value = _httpContextAccessor.HttpContext?.Request.Cookies[key];
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return _protector.Unprotect(value);
        }
        catch
        {
            return null;
        }
    }

    private void WriteCookie(string key, string value)
    {
        var response = _httpContextAccessor.HttpContext?.Response
            ?? throw new InvalidOperationException("No active HTTP response found.");
        var isHttpsRequest = _httpContextAccessor.HttpContext?.Request.IsHttps ?? false;

        response.Cookies.Append(key, _protector.Protect(value), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = isHttpsRequest,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });
    }

    private void DeleteCookie(string key)
    {
        var isHttpsRequest = _httpContextAccessor.HttpContext?.Request.IsHttps ?? false;
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(key, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = isHttpsRequest
        });
    }

    // â”€â”€ JWT identity decoding (no signature validation â€” display use only) â”€

    private static SessionIdentity DecodeJwtIdentity(string token)
    {
        var identity = new SessionIdentity();
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return identity;

            var payload = parts[1];
            // Pad base64 string
            payload = payload.Replace('-', '+').Replace('_', '/');
            payload += (payload.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("unique_name", out var un)) identity.UserName = un.GetString();
            else if (root.TryGetProperty("sub", out var sub)) identity.UserName = sub.GetString();

            if (root.TryGetProperty("email", out var em)) identity.Email = em.GetString();

            if (root.TryGetProperty("institutionType", out var it) && it.ValueKind == JsonValueKind.String)
            {
                if (int.TryParse(it.GetString(), out var parsed))
                    identity.InstitutionType = parsed;
            }
            else if (root.TryGetProperty("institutionType", out it) && it.ValueKind == JsonValueKind.Number)
            {
                identity.InstitutionType = it.GetInt32();
            }

            // Role claim may be emitted as `role` or the standard ClaimTypes.Role URI.
            if (TryReadRoleClaims(root, out var roles))
            {
                identity.Roles.AddRange(roles);
            }
        }
        catch { /* ignore decode errors â€“ identity stays default */ }

        static bool TryReadRoleClaims(JsonElement root, out List<string> roles)
        {
            roles = new List<string>();
            if (TryAppendRoles(root, "role", roles)) return true;
            return TryAppendRoles(root, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", roles);
        }

        static bool TryAppendRoles(JsonElement root, string propertyName, List<string> roles)
        {
            if (!root.TryGetProperty(propertyName, out var roleProp))
                return false;

            if (roleProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in roleProp.EnumerateArray())
                {
                    if (r.GetString() is string value && !string.IsNullOrWhiteSpace(value))
                        roles.Add(value);
                }
            }
            else if (roleProp.GetString() is string singleRole && !string.IsNullOrWhiteSpace(singleRole))
            {
                roles.Add(singleRole);
            }

            return roles.Count > 0;
        }

        return identity;
    }

    private sealed class TimetableCreateResponse { public Guid Id { get; set; } }

    // ── Sidebar Settings ──────────────────────────────────────────────────────

    public async Task<List<SidebarMenuItemWebModel>> GetSidebarMenusAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<SidebarMenuApiDto>>("api/v1/sidebar-menu", ct) ?? new();
        return raw.Select(MapSidebarItem).ToList();
    }

    public async Task<List<SidebarMenuItemWebModel>> GetVisibleSidebarMenusForCurrentUserAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<SidebarMenuApiDto>>("api/v1/sidebar-menu/my-visible", ct) ?? new();
        return raw.Select(MapSidebarItem).ToList();
    }

    public Task SetSidebarMenuRolesAsync(Guid id, Dictionary<string, bool> roles, CancellationToken ct)
    {
        var payload = new
        {
            entries = roles.Select(kv => new { roleName = kv.Key, isAllowed = kv.Value }).ToList()
        };
        return PutAsync<object, object>($"api/v1/sidebar-menu/{id}/roles", payload, ct);
    }

    public Task SetSidebarMenuStatusAsync(Guid id, bool isActive, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/sidebar-menu/{id}/status", new { isActive }, ct);

    public async Task<ResultCalculationSettingsPageModel> GetResultCalculationSettingsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<ResultCalculationSettingsApiDto>("api/v1/result-calculation", ct);
        var model = new ResultCalculationSettingsPageModel();
        if (raw is null) return model;

        model.GpaRules = raw.GpaScaleRules.Select((r, index) => new ResultCalculationGpaRuleItem
        {
            Id = r.Id,
            GradePoint = r.GradePoint,
            MinimumScore = r.MinimumScore,
            DisplayOrder = r.DisplayOrder == 0 ? index + 1 : r.DisplayOrder
        }).ToList();

        model.ComponentRules = raw.ComponentRules.Select((r, index) => new ResultCalculationComponentRuleItem
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty,
            Weightage = r.Weightage,
            DisplayOrder = r.DisplayOrder == 0 ? index + 1 : r.DisplayOrder,
            IsActive = r.IsActive
        }).ToList();

        return model;
    }

    public Task SaveResultCalculationSettingsAsync(ResultCalculationSettingsPageModel model, CancellationToken ct)
    {
        var payload = new
        {
            gpaScaleRules = model.GpaRules.Select((r, index) => new
            {
                r.Id,
                gradePoint = r.GradePoint,
                minimumScore = r.MinimumScore,
                displayOrder = index + 1
            }).ToList(),
            componentRules = model.ComponentRules.Select((r, index) => new
            {
                r.Id,
                name = r.Name,
                weightage = r.Weightage,
                displayOrder = index + 1,
                isActive = r.IsActive
            }).ToList()
        };

        return PostAsync<object, object>("api/v1/result-calculation", payload, ct);
    }

    private static SidebarMenuItemWebModel MapSidebarItem(SidebarMenuApiDto dto) => new()
    {
        Id           = dto.Id,
        Key          = dto.Key,
        Name         = dto.Name,
        Purpose      = dto.Purpose,
        ParentId     = dto.ParentId,
        DisplayOrder = dto.DisplayOrder,
        IsActive     = dto.IsActive,
        IsSystemMenu = dto.IsSystemMenu,
        RoleAccesses = dto.RoleAccesses?.ToDictionary(r => r.RoleName, r => r.IsAllowed) ?? new(),
        SubMenus     = dto.SubMenus?.Select(MapSidebarItem).ToList() ?? new()
    };

    private sealed class SidebarMenuApiDto
    {
        public Guid   Id           { get; set; }
        public string Key          { get; set; } = string.Empty;
        public string Name         { get; set; } = string.Empty;
        public string Purpose      { get; set; } = string.Empty;
        public Guid?  ParentId     { get; set; }
        public int    DisplayOrder { get; set; }
        public bool   IsActive     { get; set; }
        public bool   IsSystemMenu { get; set; }
        public List<SidebarRoleAccessApiDto> RoleAccesses { get; set; } = new();
        public List<SidebarMenuApiDto>       SubMenus     { get; set; } = new();
    }

    private sealed class SidebarRoleAccessApiDto
    {
        public string RoleName  { get; set; } = string.Empty;
        public bool   IsAllowed { get; set; }
    }

    private sealed class ResultCalculationSettingsApiDto
    {
        public List<ResultCalculationGpaRuleApiDto> GpaScaleRules { get; set; } = new();
        public List<ResultCalculationComponentRuleApiDto> ComponentRules { get; set; } = new();
    }

    private sealed class ResultCalculationGpaRuleApiDto
    {
        public Guid? Id { get; set; }
        public decimal GradePoint { get; set; }
        public decimal MinimumScore { get; set; }
        public int DisplayOrder { get; set; }
    }

    private sealed class ResultCalculationComponentRuleApiDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public decimal Weightage { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    // ── License ───────────────────────────────────────────────────────────────

    public async Task<LicenseUpdatePageModel> GetLicenseDetailsAsync(CancellationToken ct)
    {
        var req = CreateRequest(HttpMethod.Get, "api/v1/license/details");
        using var client = CreateClient();
        using var resp   = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return new LicenseUpdatePageModel { IsConnected = true, Message = $"API error: {(int)resp.StatusCode}" };

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var dto = await JsonSerializer.DeserializeAsync<LicenseDetailsDto>(stream, _jsonOptions, ct);
        return new LicenseUpdatePageModel
        {
            IsConnected  = true,
            Status       = dto?.Status,
            LicenseType  = dto?.LicenseType,
            ActivatedAt  = dto?.ActivatedAt,
            ExpiresAt    = dto?.ExpiresAt,
            UpdatedAt    = dto?.UpdatedAt,
            RemainingDays= dto?.RemainingDays,
        };
    }

    public async Task<string> UploadLicenseAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, ct);
        var fileBytes = buffer.ToArray();

        using var resp = await SendWithAutoRefreshAsync(() =>
        {
            var req = CreateRequest(HttpMethod.Post, "api/v1/license/upload");
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileBytes), "file", fileName);
            req.Content = content;
            return req;
        }, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        return resp.IsSuccessStatusCode ? "License uploaded successfully." : $"Upload failed: {body}";
    }

    private sealed class LicenseDetailsDto
    {
        public string?   Status        { get; set; }
        public string?   LicenseType   { get; set; }
        public DateTime? ActivatedAt   { get; set; }
        public DateTime? ExpiresAt     { get; set; }
        public DateTime? UpdatedAt     { get; set; }
        public int?      RemainingDays { get; set; }
    }

    // ── Theme ─────────────────────────────────────────────────────────────────

    public async Task<string?> GetCurrentThemeAsync(CancellationToken ct)
    {
        var req = CreateRequest(HttpMethod.Get, "api/v1/theme");
        using var client = CreateClient();
        using var resp   = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var dto = await JsonSerializer.DeserializeAsync<ThemeDto>(stream, _jsonOptions, ct);
        return dto?.ThemeKey;
    }

    public Task SetThemeAsync(string? themeKey, CancellationToken ct)
        => PutAsync<object, object>("api/v1/theme", new { themeKey }, ct);

    private sealed class ThemeDto { public string? ThemeKey { get; set; } }

    // ── Report Settings ───────────────────────────────────────────────────────

    public async Task<List<ReportDefinitionWebModel>> GetReportDefinitionsAsync(CancellationToken ct)
    {
        var req = CreateRequest(HttpMethod.Get, "api/v1/report-settings");
        using var client = CreateClient();
        using var resp   = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return new();
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var dtos = await JsonSerializer.DeserializeAsync<List<ReportDefinitionApiDto>>(stream, _jsonOptions, ct);
        return dtos?.Select(d => new ReportDefinitionWebModel
        {
            Id            = d.Id,
            Key           = d.Key ?? "",
            Name          = d.Name ?? "",
            Purpose       = d.Purpose ?? "",
            IsActive      = d.IsActive,
            AssignedRoles = d.AssignedRoles ?? new()
        }).ToList() ?? new();
    }

    public Task CreateReportDefinitionAsync(CreateReportForm form, CancellationToken ct)
        => PostAsync<object, object>("api/v1/report-settings", new { form.Key, form.Name, form.Purpose }, ct);

    public Task SetReportActiveAsync(Guid id, bool activate, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/report-settings/{id}/{(activate ? "activate" : "deactivate")}", new { }, ct);

    public Task SetReportRolesAsync(Guid id, List<string> roles, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/report-settings/{id}/roles", new { roleNames = roles }, ct);

    private sealed class ReportDefinitionApiDto
    {
        public Guid         Id            { get; set; }
        public string?      Key           { get; set; }
        public string?      Name          { get; set; }
        public string?      Purpose       { get; set; }
        public bool         IsActive      { get; set; }
        public List<string> AssignedRoles { get; set; } = new();
    }

    // ── Module Settings ───────────────────────────────────────────────────────

    public async Task<List<ModuleSettingsWebModel>> GetModuleSettingsAsync(CancellationToken ct)
    {
        var req = CreateRequest(HttpMethod.Get, "api/v1/modules/all-settings");
        using var client = CreateClient();
        using var resp   = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return new();
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var dtos = await JsonSerializer.DeserializeAsync<List<ModuleSettingsApiDto>>(stream, _jsonOptions, ct);
        return dtos?.Select(d => new ModuleSettingsWebModel
        {
            Id            = d.Id,
            Key           = d.Key ?? "",
            Name          = d.Name ?? "",
            IsMandatory   = d.IsMandatory,
            IsActive      = d.IsActive,
            AssignedRoles = d.AssignedRoles ?? new()
        }).ToList() ?? new();
    }

    public Task SetModuleActiveAsync(string key, bool activate, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/modules/{key}/{(activate ? "activate" : "deactivate")}", new { }, ct);

    public Task SetModuleRolesAsync(string key, List<string> roles, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/modules/{key}/roles", new { roleNames = roles }, ct);

    private sealed class ModuleSettingsApiDto
    {
        public Guid         Id            { get; set; }
        public string?      Key           { get; set; }
        public string?      Name          { get; set; }
        public bool         IsMandatory   { get; set; }
        public bool         IsActive      { get; set; }
        public List<string> AssignedRoles { get; set; } = new();
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    public async Task<List<NotificationItem>> GetNotificationsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<NotificationApiDto>>("api/v1/notification/inbox", ct) ?? new();
        return raw.Select(n => new NotificationItem
        {
            Id               = n.Id,
            Title            = n.Title ?? "",
            Body             = n.Body,
            NotificationType = n.NotificationType ?? "",
            IsRead           = n.IsRead,
            CreatedAt        = n.CreatedAt
        }).ToList();
    }

    public async Task<int> GetUnreadNotificationCountAsync(CancellationToken ct)
    {
        var dto = await GetAsync<BadgeDto>("api/v1/notification/badge", ct);
        return dto?.UnreadCount ?? 0;
    }

    public Task MarkNotificationReadAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/notification/{id}/read", new { }, ct);

    public Task MarkAllNotificationsReadAsync(CancellationToken ct)
        => PostAsync<object, object>("api/v1/notification/read-all", new { }, ct);

    private sealed class NotificationApiDto
    {
        public Guid     Id               { get; set; }
        public string?  Title            { get; set; }
        public string?  Body             { get; set; }
        public string?  NotificationType { get; set; }
        public bool     IsRead           { get; set; }
        public DateTime CreatedAt        { get; set; }
    }

    private sealed class BadgeDto { public int UnreadCount { get; set; } }

    // ── Students ──────────────────────────────────────────────────────────────

    public async Task<List<StudentItem>> GetStudentsAsync(Guid? departmentId, CancellationToken ct)
    {
        var path = departmentId.HasValue
            ? $"api/v1/student?departmentId={departmentId.Value}"
            : "api/v1/student";
        var raw = await GetAsync<List<StudentApiDto>>(path, ct) ?? new();
        return raw.Select(MapStudent).ToList();
    }

    private static StudentItem MapStudent(StudentApiDto s) => new()
    {
        // Final-Touches Phase 1 Stage 1.5 — semester-students endpoint returns StudentProfileId.
        Id                 = s.StudentProfileId != Guid.Empty ? s.StudentProfileId : s.Id,
        RegistrationNumber = s.RegistrationNumber ?? "",
        FullName           = s.FullName ?? s.StudentName ?? s.UserName ?? s.Email ?? s.RegistrationNumber ?? "Student",
        Email              = s.Email,
        DepartmentName     = s.DepartmentName ?? "",
        ProgramName        = s.ProgramName ?? "",
        SemesterNumber     = s.SemesterNumber > 0 ? s.SemesterNumber : s.CurrentSemesterNumber,
        Status             = s.Status ?? "Active"
    };

    private sealed class StudentApiDto
    {
        public Guid    Id                 { get; set; }
        public Guid    StudentProfileId   { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? FullName           { get; set; }
        public string? StudentName        { get; set; }
        public string? UserName           { get; set; }
        public string? Email              { get; set; }
        public string? DepartmentName     { get; set; }
        public string? ProgramName        { get; set; }
        public int     SemesterNumber     { get; set; }
        public int     CurrentSemesterNumber { get; set; }
        public string? Status             { get; set; }
    }

    // ── Departments (detail) ──────────────────────────────────────────────────

    public async Task<List<DepartmentItem>> GetDepartmentDetailsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<DeptDetailDto>>("api/v1/department", ct) ?? new();
        return raw.Select(d => new DepartmentItem
        {
            Id = d.Id,
            Name = d.Name ?? "",
            Code = d.Code ?? "",
            IsActive = d.IsActive,
            InstitutionType = d.InstitutionType
        }).ToList();
    }

    public async Task<List<TenantItem>> GetTenantsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<TenantApiDto>>("api/v1/tenant", ct) ?? new();
        return raw.Select(t => new TenantItem
        {
            Id = t.Id,
            Code = t.Code ?? string.Empty,
            Name = t.Name ?? string.Empty,
            IsActive = t.IsActive
        }).ToList();
    }

    public Task CreateTenantAsync(string code, string name, CancellationToken ct)
        => PostAsync<object, object>("api/v1/tenant", new { code, name }, ct);

    public Task UpdateTenantAsync(Guid id, string newName, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/tenant/{id}", new { newName }, ct);

    public Task SetTenantActiveAsync(Guid id, bool activate, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/tenant/{id}/{(activate ? "activate" : "deactivate")}", new { }, ct);

    public async Task<List<CampusItem>> GetCampusesAsync(Guid? tenantId, CancellationToken ct)
    {
        var path = tenantId.HasValue
            ? $"api/v1/campus?tenantId={tenantId.Value}"
            : "api/v1/campus";

        var raw = await GetAsync<List<CampusApiDto>>(path, ct) ?? new();
        return raw.Select(c => new CampusItem
        {
            Id = c.Id,
            TenantId = c.TenantId,
            Code = c.Code ?? string.Empty,
            Name = c.Name ?? string.Empty,
            IsActive = c.IsActive
        }).ToList();
    }

    public Task CreateCampusAsync(Guid tenantId, string code, string name, CancellationToken ct)
        => PostAsync<object, object>("api/v1/campus", new { tenantId, code, name }, ct);

    public Task UpdateCampusAsync(Guid id, string newName, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/campus/{id}", new { newName }, ct);

    public Task SetCampusActiveAsync(Guid id, bool activate, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/campus/{id}/{(activate ? "activate" : "deactivate")}", new { }, ct);

    private sealed class DeptDetailDto
    {
        public Guid    Id       { get; set; }
        public string? Name     { get; set; }
        public string? Code     { get; set; }
        public bool    IsActive { get; set; }
        public int     InstitutionType { get; set; }
    }

    private sealed class TenantApiDto
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class CampusApiDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class ProgramDetailDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public Guid DepartmentId { get; set; }
        public int TotalSemesters { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class AdminUserApiDto
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public int? InstitutionType { get; set; }
    }

    private sealed class AdminDepartmentAssignmentApiDto
    {
        public Guid AdminUserId { get; set; }
        public Guid DepartmentId { get; set; }
    }

    private sealed class UserImportResultApiDto
    {
        public int TotalRows { get; set; }
        public int Imported { get; set; }
        public int Duplicates { get; set; }
        public int Errors { get; set; }
        public List<string>? ErrorDetails { get; set; }
    }

    public Task CreateDepartmentAsync(string name, string code, int institutionType, CancellationToken ct)
        => PostAsync<object, object>("api/v1/department", new { name, code, institutionType }, ct);

    public Task CreateProgramAsync(string name, string code, Guid departmentId, int totalSemesters, CancellationToken ct)
        => PostAsync<object, object>("api/v1/program", new { name, code, departmentId, totalSemesters }, ct);

    public Task UpdateProgramAsync(Guid id, string newName, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/program/{id}", new { name = newName }, ct);

    public Task DeactivateProgramAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/program/{id}", ct);

    public Task UpdateDepartmentAsync(Guid id, string newName, int? institutionType, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/department/{id}", new { newName, institutionType }, ct);

    public Task DeactivateDepartmentAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/department/{id}", ct);

    public async Task<UserImportResultItem> ImportUsersCsvAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, ct);
        var fileBytes = buffer.ToArray();

        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, "api/v1/user-import/csv");
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileBytes), "file", fileName);
            request.Content = content;
            return request;
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);

        var raw = string.IsNullOrWhiteSpace(body)
            ? null
            : JsonSerializer.Deserialize<UserImportResultApiDto>(body, _jsonOptions);

        return new UserImportResultItem
        {
            TotalRows = raw?.TotalRows ?? 0,
            Imported = raw?.Imported ?? 0,
            Duplicates = raw?.Duplicates ?? 0,
            Errors = raw?.Errors ?? 0,
            ErrorDetails = raw?.ErrorDetails ?? new List<string>()
        };
    }

    public async Task<List<AdminUserLookupItem>> GetAdminUsersAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<AdminUserApiDto>>("api/v1/admin-user", ct) ?? new();
        return raw.Select(a => new AdminUserLookupItem
        {
            Id = a.Id,
            UserName = a.Username ?? string.Empty,
            Email = a.Email,
            IsActive = a.IsActive,
            InstitutionType = a.InstitutionType
        }).ToList();
    }

    public async Task<Guid> CreateAdminUserAsync(string username, string? email, string password, int? institutionType, CancellationToken ct)
    {
        var created = await PostAsync<object, AdminUserApiDto>("api/v1/admin-user", new { username, email, password, institutionType }, ct)
            ?? throw new InvalidOperationException("Admin create API returned no body.");
        return created.Id;
    }

    public Task UpdateAdminUserAsync(Guid userId, string? email, bool isActive, string? newPassword, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/admin-user/{userId}", new { email, isActive, newPassword }, ct);

    public async Task<List<Guid>> GetAdminDepartmentIdsAsync(Guid adminUserId, CancellationToken ct)
    {
        var raw = await GetAsync<List<AdminDepartmentAssignmentApiDto>>($"api/v1/department/admin-assignment/{adminUserId}", ct) ?? new();
        return raw.Select(x => x.DepartmentId).Distinct().ToList();
    }

    public Task AssignAdminToDepartmentAsync(Guid adminUserId, Guid departmentId, CancellationToken ct)
        => PostAsync<object, object>("api/v1/department/admin-assignment", new { adminUserId, departmentId }, ct);

    public Task RemoveAdminFromDepartmentAsync(Guid adminUserId, Guid departmentId, CancellationToken ct)
        => DeleteWithBodyAsync("api/v1/department/admin-assignment", new { adminUserId, departmentId }, ct);

    // ── Courses ───────────────────────────────────────────────────────────────

    public async Task<List<CourseItem>> GetCourseDetailsAsync(Guid? departmentId, CancellationToken ct)
    {
        var path = departmentId.HasValue
            ? $"api/v1/course?departmentId={departmentId.Value}"
            : "api/v1/course";
        var raw = await GetAsync<List<CourseDetailDto>>(path, ct) ?? new();
        return raw.Select(MapCourseItem).ToList();
    }

    // Final-Touches Phase 19 Stage 19.3 — get courses filtered by HasSemesters
    public async Task<List<CourseItem>> GetCourseDetailsByTypeAsync(bool hasSemesters, CancellationToken ct)
    {
        var raw = await GetAsync<List<CourseDetailDto>>($"api/v1/course?hasSemesters={hasSemesters}", ct) ?? new();
        return raw.Select(MapCourseItem).ToList();
    }

    public async Task<List<CourseOfferingItem>> GetCourseOfferingsAsync(Guid? departmentId, CancellationToken ct)
    {
        var path = departmentId.HasValue
            ? $"api/v1/course/offerings?departmentId={departmentId.Value}"
            : "api/v1/course/offerings";
        var raw = await GetAsync<List<OfferingApiDto>>(path, ct) ?? new();
        return raw.Select(o => new CourseOfferingItem
        {
            Id           = o.Id,
            CourseId     = o.CourseId,
            SemesterId   = o.SemesterId,
            DepartmentId = o.DepartmentId,
            CourseTitle  = o.CourseTitle ?? "",
            CourseCode   = o.CourseCode ?? "",
            FacultyName  = o.FacultyName ?? "",
            SemesterName = o.SemesterName ?? "",
            IsActive     = o.IsActive
        }).ToList();
    }

    public async Task<List<LookupItem>> GetMyOfferingsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<MyOfferingApiDto>>("api/v1/course/offerings/my", ct) ?? new();
        return raw.Select(o => new LookupItem
        {
            Id = o.Id,
            Name = string.IsNullOrWhiteSpace(o.SemesterName)
                ? o.CourseTitle ?? "Offering"
                : $"{o.CourseTitle ?? "Offering"} ({o.SemesterName})"
        }).ToList();
    }

    public Task CreateCourseAsync(string code, string title, int creditHours, Guid departmentId, bool hasSemesters, int? totalSemesters, int? durationValue, string? durationUnit, string? gradingType, CancellationToken ct)
        => PostAsync<object, object>("api/v1/course", new { code, title, creditHours, departmentId, hasSemesters, totalSemesters, durationValue, durationUnit, gradingType }, ct);

    // Final-Touches Phase 19 Stage 19.4 — get grading config
    public Task<GradingConfigApiModel?> GetCourseGradingConfigAsync(Guid courseId, CancellationToken ct)
        => GetAsync<GradingConfigApiModel>($"api/v1/grading-config/{courseId}", ct);

    // Final-Touches Phase 19 Stage 19.4 — save grading config
    public async Task<GradingConfigApiModel?> SaveCourseGradingConfigAsync(Guid courseId, decimal passThreshold, string gradingType, string? gradeRangesJson, CancellationToken ct)
    {
        await PutAsync<object, object>($"api/v1/grading-config/{courseId}", new { passThreshold, gradingType, gradeRangesJson }, ct);
        return await GetCourseGradingConfigAsync(courseId, ct);
    }

    public async Task<List<InstitutionGradingProfileApiModel>> GetInstitutionGradingProfilesAsync(CancellationToken ct)
        => await GetAsync<List<InstitutionGradingProfileApiModel>>("api/v1/institution-grading-profiles", ct) ?? new();

    public Task<InstitutionGradingProfileApiModel?> SaveInstitutionGradingProfileAsync(
        int institutionType,
        decimal passThreshold,
        string? gradeRangesJson,
        bool isActive,
        CancellationToken ct)
        => PutAsync<object, InstitutionGradingProfileApiModel>(
            $"api/v1/institution-grading-profiles/{institutionType}",
            new { passThreshold, gradeRangesJson, isActive },
            ct);

    // ── Phase 20: LMS content modules ──────────────────────────────────────────

    public async Task<List<LmsModuleApiModel>> GetLmsModulesAsync(Guid offeringId, bool publishedOnly, CancellationToken ct)
        => await GetAsync<List<LmsModuleApiModel>>($"api/v1/lms/modules/{offeringId}?publishedOnly={publishedOnly}", ct) ?? new();

    public Task<LmsModuleApiModel?> GetLmsModuleAsync(Guid moduleId, CancellationToken ct)
        => GetAsync<LmsModuleApiModel>($"api/v1/lms/module/{moduleId}", ct);

    public async Task<LmsModuleApiModel?> CreateLmsModuleAsync(Guid offeringId, string title, int weekNumber, string? body, CancellationToken ct)
    {
        await PostAsync<object, object>("api/v1/lms/module", new { offeringId, title, weekNumber, body }, ct);
        var modules = await GetLmsModulesAsync(offeringId, false, ct);
        return modules.OrderByDescending(m => m.WeekNumber).FirstOrDefault();
    }

    public Task UpdateLmsModuleAsync(Guid moduleId, string title, int weekNumber, string? body, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/lms/module/{moduleId}", new { title, weekNumber, body }, ct);

    public Task PublishLmsModuleAsync(Guid moduleId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/lms/module/{moduleId}/publish", new { }, ct);

    public Task UnpublishLmsModuleAsync(Guid moduleId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/lms/module/{moduleId}/unpublish", new { }, ct);

    public Task DeleteLmsModuleAsync(Guid moduleId, CancellationToken ct)
        => DeleteAsync($"api/v1/lms/module/{moduleId}", ct);

    public Task<LmsVideoApiModel?> AddLmsVideoAsync(Guid moduleId, string title, string? storageUrl, string? embedUrl, int? durationSeconds, CancellationToken ct)
        => PostAsync<object, LmsVideoApiModel>("api/v1/lms/video", new { moduleId, title, storageUrl, embedUrl, durationSeconds }, ct);

    public Task DeleteLmsVideoAsync(Guid videoId, CancellationToken ct)
        => DeleteAsync($"api/v1/lms/video/{videoId}", ct);

    // ── Phase 20: Discussion forum ──────────────────────────────────────────────

    public async Task<List<DiscussionThreadApiModel>> GetDiscussionThreadsAsync(Guid offeringId, CancellationToken ct)
        => await GetAsync<List<DiscussionThreadApiModel>>($"api/v1/discussion/{offeringId}/threads", ct) ?? new();

    public Task<DiscussionThreadApiModel?> GetDiscussionThreadAsync(Guid threadId, CancellationToken ct)
        => GetAsync<DiscussionThreadApiModel>($"api/v1/discussion/thread/{threadId}", ct);

    public Task<DiscussionThreadApiModel?> CreateDiscussionThreadAsync(Guid offeringId, Guid authorId, string title, CancellationToken ct)
        => PostAsync<object, DiscussionThreadApiModel>("api/v1/discussion/thread", new { offeringId, authorId, title }, ct);

    public Task SetThreadPinnedAsync(Guid threadId, bool pinned, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/discussion/thread/{threadId}/pin?pinned={pinned}", new { }, ct);

    public Task CloseDiscussionThreadAsync(Guid threadId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/discussion/thread/{threadId}/close", new { }, ct);

    public Task ReopenDiscussionThreadAsync(Guid threadId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/discussion/thread/{threadId}/reopen", new { }, ct);

    public Task DeleteDiscussionThreadAsync(Guid threadId, CancellationToken ct)
        => DeleteAsync($"api/v1/discussion/thread/{threadId}", ct);

    public Task<DiscussionReplyApiModel?> AddDiscussionReplyAsync(Guid threadId, Guid authorId, string body, CancellationToken ct)
        => PostAsync<object, DiscussionReplyApiModel>("api/v1/discussion/reply", new { threadId, authorId, body }, ct);

    public Task DeleteDiscussionReplyAsync(Guid replyId, CancellationToken ct)
        => DeleteAsync($"api/v1/discussion/reply/{replyId}", ct);

    // ── Phase 20: Announcements ─────────────────────────────────────────────────

    public async Task<List<AnnouncementApiModel>> GetAnnouncementsAsync(Guid offeringId, CancellationToken ct)
        => await GetAsync<List<AnnouncementApiModel>>($"api/v1/announcement/{offeringId}", ct) ?? new();

    public Task<AnnouncementApiModel?> CreateAnnouncementAsync(Guid? offeringId, Guid authorId, string title, string body, CancellationToken ct)
        => PostAsync<object, AnnouncementApiModel>("api/v1/announcement", new { offeringId, authorId, title, body }, ct);

    public Task DeleteAnnouncementAsync(Guid announcementId, CancellationToken ct)
        => DeleteAsync($"api/v1/announcement/{announcementId}", ct);

    // ── Plan C: Course Materials ──────────────────────────────────────────────

    public async Task<List<CourseMaterialApiModel>> GetCourseMaterialsAsync(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        bool activeOnly,
        CancellationToken ct)
    {
        var query = new List<string>();
        if (departmentId.HasValue) query.Add($"departmentId={departmentId.Value}");
        if (academicProgramId.HasValue) query.Add($"academicProgramId={academicProgramId.Value}");
        if (semesterId.HasValue) query.Add($"semesterId={semesterId.Value}");
        if (courseId.HasValue) query.Add($"courseId={courseId.Value}");
        query.Add($"activeOnly={activeOnly.ToString().ToLowerInvariant()}");

        var path = query.Count == 0
            ? "api/v1/course-materials"
            : $"api/v1/course-materials?{string.Join("&", query)}";

        return await GetAsync<List<CourseMaterialApiModel>>(path, ct) ?? new();
    }

    public Task<CourseMaterialApiModel?> GetCourseMaterialByIdAsync(Guid id, CancellationToken ct)
        => GetAsync<CourseMaterialApiModel>($"api/v1/course-materials/{id}", ct);

    public async Task<CourseMaterialFileDownloadApiModel?> DownloadCourseMaterialFileAsync(Guid id, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/v1/course-materials/{id}/file");
        using var response = await CreateClient().SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw BuildException(response.StatusCode, body);
        }

        var fileBytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "course-material";

        return new CourseMaterialFileDownloadApiModel
        {
            Content = fileBytes,
            ContentType = contentType,
            FileName = fileName.Trim('"')
        };
    }

    public async Task<CourseMaterialUploadApiModel?> UploadCourseMaterialFileAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        using var request = CreateRequest(HttpMethod.Post, "api/v1/course-materials/upload");
        request.Content = content;
        using var response = await CreateClient().SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);
        return JsonSerializer.Deserialize<CourseMaterialUploadApiModel>(body, _jsonOptions);
    }

    public Task<CourseMaterialApiModel?> CreateCourseMaterialAsync(
        Guid departmentId,
        Guid academicProgramId,
        Guid semesterId,
        Guid courseId,
        string materialType,
        string title,
        string? description,
        string? externalUrl,
        string? blobPath,
        string? fileName,
        long? fileSizeBytes,
        bool isActive,
        CancellationToken ct)
        => PostAsync<object, CourseMaterialApiModel>(
            "api/v1/course-materials",
            new
            {
                departmentId,
                academicProgramId,
                semesterId,
                courseId,
                materialType,
                title,
                description,
                externalUrl,
                blobPath,
                fileName,
                fileSizeBytes,
                isActive
            },
            ct);

    public Task<CourseMaterialApiModel?> UpdateCourseMaterialAsync(
        Guid id,
        string materialType,
        string title,
        string? description,
        string? externalUrl,
        string? blobPath,
        string? fileName,
        long? fileSizeBytes,
        bool isActive,
        CancellationToken ct)
        => PutAsync<object, CourseMaterialApiModel>(
            $"api/v1/course-materials/{id}",
            new
            {
                materialType,
                title,
                description,
                externalUrl,
                blobPath,
                fileName,
                fileSizeBytes,
                isActive
            },
            ct);

    public Task SetCourseMaterialActiveAsync(Guid id, bool isActive, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/course-materials/{id}/active?isActive={isActive.ToString().ToLowerInvariant()}", new { }, ct);

    // ── Phase 21: Study Planner ─────────────────────────────────────────────────

    public async Task<List<StudyPlanApiModel>> GetStudyPlansAsync(Guid studentProfileId, CancellationToken ct)
        => await GetAsync<List<StudyPlanApiModel>>($"api/v1/study-plan/plans/{studentProfileId}", ct) ?? new();

    public async Task<List<StudyPlanApiModel>> GetStudyPlansByDepartmentAsync(Guid departmentId, CancellationToken ct)
        => await GetAsync<List<StudyPlanApiModel>>($"api/v1/study-plan/plans/department/{departmentId}", ct) ?? new();

    public Task<StudyPlanApiModel?> GetStudyPlanAsync(Guid planId, CancellationToken ct)
        => GetAsync<StudyPlanApiModel>($"api/v1/study-plan/plan/{planId}", ct);

    public Task<StudyPlanApiModel?> CreateStudyPlanAsync(Guid studentProfileId, string plannedSemesterName, string? notes, CancellationToken ct)
        => PostAsync<object, StudyPlanApiModel>("api/v1/study-plan/plan",
            new { studentProfileId, plannedSemesterName, notes }, ct);

    public async Task<StudyPlanApiModel?> AddStudyPlanCourseAsync(Guid planId, Guid courseId, CancellationToken ct)
    {
        var client  = CreateClient();
        var json    = System.Text.Json.JsonSerializer.Serialize(courseId);
        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"api/v1/study-plan/plan/{planId}/course", content, ct);
        if (!response.IsSuccessStatusCode) return null;
        var body = await response.Content.ReadAsStringAsync(ct);
        return System.Text.Json.JsonSerializer.Deserialize<StudyPlanApiModel>(body, _jsonOptions);
    }

    public Task RemoveStudyPlanCourseAsync(Guid planId, Guid courseId, CancellationToken ct)
        => DeleteAsync($"api/v1/study-plan/plan/{planId}/course/{courseId}", ct);

    public Task DeleteStudyPlanAsync(Guid planId, CancellationToken ct)
        => DeleteAsync($"api/v1/study-plan/plan/{planId}", ct);

    public Task AdvisePlanAsync(Guid planId, bool isEndorsed, string? advisorNotes, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/study-plan/plan/{planId}/advise",
            new { planId, isEndorsed, advisorNotes }, ct);

    public async Task<StudyPlanRecommendationApiModel?> GetStudyPlanRecommendationsAsync(Guid studentProfileId, string plannedSemesterName, CancellationToken ct)
        => await GetAsync<StudyPlanRecommendationApiModel>(
            $"api/v1/study-plan/recommendations/{studentProfileId}?plannedSemesterName={Uri.EscapeDataString(plannedSemesterName)}", ct);

    public Task CreateOfferingAsync(Guid courseId, Guid semesterId, int maxEnrollment, Guid? facultyUserId, CancellationToken ct)
        => PostAsync<object, object>("api/v1/course/offerings", new { courseId, semesterId, maxEnrollment, facultyUserId }, ct);

    public Task DeactivateCourseAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/course/{id}", ct);

    public Task DeleteOfferingAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/course/offerings/{id}", ct);

    // ── Phase 22: External Integrations ─────────────────────────────────────────

    // Library config
    public Task<LibraryConfigApiModel?> GetLibraryConfigAsync(CancellationToken ct)
        => GetAsync<LibraryConfigApiModel>("api/v1/library/config", ct);

    public Task SaveLibraryConfigAsync(LibraryConfigApiModel model, CancellationToken ct)
        => PutAsync<LibraryConfigApiModel, object>("api/v1/library/config", model, ct);

    public async Task<LibraryLoansApiModel?> GetMyLibraryLoansAsync(CancellationToken ct)
        => await GetAsync<LibraryLoansApiModel>("api/v1/library/loans", ct);

    // Accreditation templates
    public Task<List<AccreditationTemplateApiModel>> GetAccreditationTemplatesAsync(CancellationToken ct)
        => GetAsync<List<AccreditationTemplateApiModel>>("api/v1/accreditation", ct)
           .ContinueWith(t => t.Result ?? new List<AccreditationTemplateApiModel>(), TaskScheduler.Default);

    public Task<AccreditationTemplateApiModel?> GetAccreditationTemplateAsync(Guid id, CancellationToken ct)
        => GetAsync<AccreditationTemplateApiModel>($"api/v1/accreditation/{id}", ct);

    public Task<AccreditationTemplateApiModel?> CreateAccreditationTemplateAsync(CreateAccreditationTemplateForm form, CancellationToken ct)
        => PostAsync<CreateAccreditationTemplateForm, AccreditationTemplateApiModel>("api/v1/accreditation", form, ct);

    public Task<AccreditationTemplateApiModel?> UpdateAccreditationTemplateAsync(Guid id, UpdateAccreditationTemplateForm form, CancellationToken ct)
        => PutAsync<UpdateAccreditationTemplateForm, AccreditationTemplateApiModel>($"api/v1/accreditation/{id}", form, ct);

    public Task DeleteAccreditationTemplateAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/accreditation/{id}", ct);

    public async Task<(byte[]? Content, string ContentType, string FileName)> GenerateAccreditationReportAsync(Guid id, CancellationToken ct)
    {
        var client   = CreateClient();
        var response = await client.GetAsync($"api/v1/accreditation/{id}/generate", ct);
        if (!response.IsSuccessStatusCode) return (null, "", "");
        var bytes       = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName    = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                          ?? "report.csv";
        return (bytes, contentType, fileName);
    }

    // Phase 23 — Institution Policy
    public async Task<InstitutionPolicyApiModel?> GetInstitutionPolicyAsync(CancellationToken ct)
        => await GetAsync<InstitutionPolicyApiModel>("api/v1/institution-policy", ct);

    public async Task SaveInstitutionPolicyAsync(InstitutionPolicyApiModel model, CancellationToken ct)
        => await PutAsync<InstitutionPolicyApiModel, object?>("api/v1/institution-policy", model, ct);

    // Phase 24 — Dynamic Module & UI Composition
    public async Task<DashboardCompositionContextApiModel?> GetDashboardCompositionContextAsync(CancellationToken ct)
        => await GetAsync<DashboardCompositionContextApiModel>("api/v1/dashboard/context", ct);

    public async Task<List<ModuleVisibilityApiModel>> GetVisibleModulesAsync(CancellationToken ct)
        => await GetAsync<List<ModuleVisibilityApiModel>>("api/v1/module-registry/visible", ct)
           ?? new();

    public async Task<AcademicVocabularyApiModel?> GetVocabularyAsync(CancellationToken ct)
        => await GetAsync<AcademicVocabularyApiModel>("api/v1/labels", ct);

    public async Task<List<WidgetDescriptorApiModel>> GetDashboardWidgetsAsync(CancellationToken ct)
        => await GetAsync<List<WidgetDescriptorApiModel>>("api/v1/dashboard/composition", ct)
           ?? new();

    // Phase 27 — Student Portal Capability Matrix
    public async Task<PortalCapabilityMatrixApiModel?> GetPortalCapabilityMatrixAsync(CancellationToken ct)
        => await GetAsync<PortalCapabilityMatrixApiModel>("api/v1/portal-capabilities/matrix", ct);

    private sealed class CourseDetailDto
    {
        public Guid    Id             { get; set; }
        public string? Title          { get; set; }
        public string? Code           { get; set; }
        public string? DepartmentName { get; set; }
        public int     CreditHours    { get; set; }
        // Final-Touches Phase 19 Stage 19.1/19.2 — extended fields
        public bool    HasSemesters   { get; set; } = true;
        public int?    TotalSemesters { get; set; }
        public int?    DurationValue  { get; set; }
        public string? DurationUnit   { get; set; }
        public string? GradingType    { get; set; }
    }

    // Final-Touches Phase 19 Stage 19.1/19.2 — shared mapper
    private static CourseItem MapCourseItem(CourseDetailDto c) => new()
    {
        Id             = c.Id,
        Title          = c.Title          ?? "",
        Code           = c.Code           ?? "",
        DepartmentName = c.DepartmentName ?? "",
        CreditHours    = c.CreditHours,
        HasSemesters   = c.HasSemesters,
        TotalSemesters = c.TotalSemesters,
        DurationValue  = c.DurationValue,
        DurationUnit   = c.DurationUnit,
        GradingType    = c.GradingType    ?? "GPA"
    };

    private sealed class OfferingApiDto
    {
        public Guid    Id           { get; set; }
        public Guid    CourseId     { get; set; }
        public Guid    SemesterId   { get; set; }
        public Guid    DepartmentId { get; set; }
        public string? CourseTitle  { get; set; }
        public string? CourseCode   { get; set; }
        public string? FacultyName  { get; set; }
        public string? SemesterName { get; set; }
        public bool    IsActive     { get; set; }
    }

    private sealed class MyOfferingApiDto
    {
        public Guid Id { get; set; }
        public string? CourseTitle { get; set; }
        public string? SemesterName { get; set; }
    }

    private sealed class StudentProfileApiDto
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public string? DeptName { get; set; }
        public int CurrentSemesterNumber { get; set; }
    }

    // ── Assignments ───────────────────────────────────────────────────────────

    public async Task<List<AssignmentItem>> GetMyAssignmentsAsync(CancellationToken ct)
    {
        // Student endpoint returns submissions, so map it into assignment-like rows for the default view.
        var raw = await GetAsync<List<MySubmissionApiDto>>("api/v1/assignment/my-submissions", ct) ?? new();
        return raw.Select(s => new AssignmentItem
        {
            Id                  = s.AssignmentId,
            Title               = s.AssignmentTitle ?? "",
            Description         = null,
            DueDate             = null,
            TotalMarks          = 0,
            IsPublished         = true,
            CourseOfferingTitle = "",
            SubmissionCount     = 0,
            IsSubmitted         = true,
            MarksAwarded        = s.MarksAwarded
        }).ToList();
    }

    public async Task<List<MyAssignmentSubmissionItem>> GetMyAssignmentSubmissionsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<MySubmissionApiDto>>("api/v1/assignment/my-submissions", ct) ?? new();
        return raw.Select(s => new MyAssignmentSubmissionItem
        {
            AssignmentId = s.AssignmentId,
            Status = s.Status ?? "",
            MarksAwarded = s.MarksAwarded,
            SubmittedAt = s.SubmittedAt
        }).ToList();
    }

    public async Task<List<AssignmentItem>> GetAssignmentsByOfferingAsync(Guid offeringId, CancellationToken ct)
    {
        var raw = await GetAsync<List<AssignmentApiDto>>($"api/v1/assignment/by-offering/{offeringId}", ct) ?? new();
        return raw.Select(MapAssignment).ToList();
    }

    private static AssignmentItem MapAssignment(AssignmentApiDto a) => new()
    {
        Id                  = a.Id,
        Title               = a.Title ?? "",
        Description         = a.Description,
        DueDate             = a.DueDate,
        TotalMarks          = a.MaxMarks,
        IsPublished         = a.IsPublished,
        CourseOfferingTitle = a.CourseOfferingTitle ?? "",
        SubmissionCount     = a.SubmissionCount,
        IsSubmitted         = a.IsSubmitted,
        MarksAwarded        = a.MarksAwarded
    };

    // ── Assignment write methods ──────────────────────────────────────────────

    public Task<Guid> CreateAssignmentAsync(Guid courseOfferingId, string title, string? description,
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        var payload = new { courseOfferingId, title, description, dueDate, maxMarks };
        return PostAsync<object, AssignmentCreateResponse>("api/v1/assignment", payload, ct)
            .ContinueWith(t => t.Result?.Id ?? Guid.Empty, ct);
    }

    public Task UpdateAssignmentAsync(Guid id, string title, string? description,
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        var payload = new { title, description, dueDate, maxMarks };
        return PutAsync<object, object>($"api/v1/assignment/{id}", payload, ct);
    }

    public Task PublishAssignmentAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/assignment/{id}/publish", new { }, ct);

    public Task DeleteAssignmentAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/assignment/{id}", ct);

    public Task GradeSubmissionAsync(Guid assignmentId, Guid studentProfileId, decimal marksAwarded,
        string? feedback, CancellationToken ct)
    {
        var payload = new { assignmentId, studentProfileId, marksAwarded, feedback };
        return PutAsync<object, object>("api/v1/assignment/submissions/grade", payload, ct);
    }

    private sealed class AssignmentCreateResponse { public Guid Id { get; set; } }

    public async Task<List<SubmissionItem>> GetSubmissionsForAssignmentAsync(Guid assignmentId, CancellationToken ct)
    {
        var raw = await GetAsync<List<SubmissionApiDto>>($"api/v1/assignment/{assignmentId}/submissions", ct) ?? new();
        return raw.Select(s => new SubmissionItem
        {
            Id                  = s.Id,
            StudentName         = s.StudentName ?? "",
            RegistrationNumber  = s.RegistrationNumber ?? "",
            Comments            = s.Comments,
            SubmittedAt         = s.SubmittedAt,
            IsGraded            = s.IsGraded,
            MarksAwarded        = s.MarksAwarded,
            FeedbackFromFaculty = s.FeedbackFromFaculty
        }).ToList();
    }

    public Task SubmitAssignmentAsync(Guid assignmentId, string? fileUrl, string? textContent, CancellationToken ct)
    {
        var payload = new { assignmentId, fileUrl, textContent };
        return PostAsync<object, object>("api/v1/assignment/submit", payload, ct);
    }

    private sealed class AssignmentApiDto
    {
        public Guid      Id                   { get; set; }
        public string?   Title                { get; set; }
        public string?   Description          { get; set; }
        public DateTime? DueDate              { get; set; }
        [JsonPropertyName("maxMarks")]
        public decimal   MaxMarks             { get; set; }
        public bool      IsPublished          { get; set; }
        public string?   CourseOfferingTitle  { get; set; }
        public int       SubmissionCount      { get; set; }
        public bool      IsSubmitted          { get; set; }
        public decimal?  MarksAwarded         { get; set; }
    }

    private sealed class SubmissionApiDto
    {
        public Guid     Id                   { get; set; }
        public string?  StudentName          { get; set; }
        public string?  RegistrationNumber   { get; set; }
        public string?  Comments             { get; set; }
        public DateTime SubmittedAt          { get; set; }
        public bool     IsGraded             { get; set; }
        public decimal? MarksAwarded         { get; set; }
        public string?  FeedbackFromFaculty  { get; set; }
    }

    private sealed class MySubmissionApiDto
    {
        public Guid      Id               { get; set; }
        public Guid      AssignmentId     { get; set; }
        public string?   AssignmentTitle  { get; set; }
        public Guid      StudentProfileId { get; set; }
        public string?   FileUrl          { get; set; }
        public string?   TextContent      { get; set; }
        public DateTime  SubmittedAt      { get; set; }
        public string?   Status           { get; set; }
        public decimal?  MarksAwarded     { get; set; }
        public string?   Feedback         { get; set; }
        public DateTime? GradedAt         { get; set; }
    }

    // ── Attendance ────────────────────────────────────────────────────────────

    public async Task<List<AttendanceSummaryItem>> GetMyAttendanceSummaryAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<MyAttendanceApiDto>>("api/v1/attendance/my-attendance", ct) ?? new();
        return raw.Select(s => new AttendanceSummaryItem
        {
            StudentId            = s.StudentId,
            StudentName          = s.StudentName ?? "",
            RegistrationNumber   = s.RegistrationNumber ?? "",
            CourseName           = s.CourseName ?? "",
            TotalClasses         = s.TotalClasses,
            PresentCount         = s.PresentCount,
            AttendancePercentage = s.AttendancePercentage
        }).ToList();
    }

    public async Task<List<AttendanceRecordItem>> GetAttendanceByOfferingAsync(Guid offeringId, CancellationToken ct)
    {
        var raw = await GetAsync<List<AttendanceRecordApiDto>>($"api/v1/attendance/by-offering/{offeringId}", ct) ?? new();
        return raw.Select(r => new AttendanceRecordItem
        {
            Id                 = r.Id,
            StudentProfileId   = r.StudentProfileId,
            StudentName        = r.StudentName ?? "",
            RegistrationNumber = r.RegistrationNumber ?? "",
            Date               = r.Date,
            Status             = r.Status ?? "",
            IsCorrected        = r.IsCorrected
        }).ToList();
    }

    private sealed class MyAttendanceApiDto
    {
        public Guid   StudentId            { get; set; }
        public string? StudentName         { get; set; }
        public string? RegistrationNumber  { get; set; }
        public string? CourseName          { get; set; }
        public int    TotalClasses         { get; set; }
        public int    PresentCount         { get; set; }
        public double AttendancePercentage { get; set; }
    }

    private sealed class AttendanceRecordApiDto
    {
        public Guid     Id                 { get; set; }
        public Guid     StudentProfileId   { get; set; }
        public string?  StudentName        { get; set; }
        public string?  RegistrationNumber { get; set; }
        public DateTime Date               { get; set; }
        public string?  Status             { get; set; }
        public bool     IsCorrected        { get; set; }
    }

    // ── Attendance write methods ──────────────────────────────────────────────

    public Task BulkMarkAttendanceAsync(Guid offeringId, DateTime date,
        IEnumerable<(Guid StudentProfileId, string Status)> entries, CancellationToken ct)
    {
        var payload = new
        {
            courseOfferingId = offeringId,
            date,
            entries = entries.Select(e => new { studentProfileId = e.StudentProfileId, status = e.Status }).ToList()
        };
        return PostAsync<object, object>("api/v1/attendance/bulk", payload, ct);
    }

    public Task CorrectAttendanceAsync(Guid studentProfileId, Guid courseOfferingId, DateTime date,
        string newStatus, string? remarks, CancellationToken ct)
    {
        var payload = new { studentProfileId, courseOfferingId, date, newStatus, remarks };
        return PutAsync<object, object>("api/v1/attendance/correct", payload, ct);
    }

    // ── Results ───────────────────────────────────────────────────────────────

    public async Task<List<ResultItem>> GetMyResultsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<ResultApiDto>>("api/v1/result/my-results", ct) ?? new();
        return raw.Select(MapResult).ToList();
    }

    public async Task<List<ResultItem>> GetResultsByOfferingAsync(Guid offeringId, CancellationToken ct)
    {
        var raw = await GetAsync<List<ResultApiDto>>($"api/v1/result/by-offering/{offeringId}", ct) ?? new();
        return raw.Select(MapResult).ToList();
    }

    // Maps the raw API DTO to the view-facing ResultItem.
    // StudentProfileId is included so the Results table can render
    // the per-row Promote button with the correct student identity.
    private static ResultItem MapResult(ResultApiDto r) => new()
    {
        Id                 = r.Id,
        StudentProfileId   = r.StudentProfileId,
        CourseOfferingId   = r.CourseOfferingId,
        ResultType         = r.ResultType ?? "",
        CourseName         = r.CourseName ?? "",
        CourseCode         = r.CourseCode ?? "",
        MarksObtained      = r.MarksObtained,
        TotalMarks         = r.TotalMarks,
        LetterGrade        = r.LetterGrade,
        IsPublished        = r.IsPublished,
        SemesterName       = r.SemesterName ?? "",
        StudentName        = r.StudentName ?? "",
        RegistrationNumber = r.RegistrationNumber ?? ""
    };

    private sealed class ResultApiDto
    {
        public Guid    Id                 { get; set; }
        public Guid    StudentProfileId   { get; set; }
        public Guid    CourseOfferingId   { get; set; }
        public string? ResultType         { get; set; }
        public string? CourseName         { get; set; }
        public string? CourseCode         { get; set; }
        public decimal? MarksObtained     { get; set; }
        public int     TotalMarks         { get; set; }
        public string? LetterGrade        { get; set; }
        public bool    IsPublished        { get; set; }
        public string? SemesterName       { get; set; }
        public string? StudentName        { get; set; }
        public string? RegistrationNumber { get; set; }
    }

    // ── Result write methods ──────────────────────────────────────────────────

    public Task CreateResultAsync(Guid studentProfileId, Guid courseOfferingId,
        string resultType, decimal marksObtained, decimal maxMarks, CancellationToken ct)
    {
        var payload = new { studentProfileId, courseOfferingId, resultType, marksObtained, maxMarks };
        return PostAsync<object, object>("api/v1/result", payload, ct);
    }

    public Task CorrectResultAsync(Guid studentProfileId, Guid courseOfferingId, string resultType,
        decimal newMarksObtained, decimal newMaxMarks, CancellationToken ct)
    {
        var payload = new { newMarksObtained, newMaxMarks };
        return PutAsync<object, object>($"api/v1/result/correct?studentProfileId={studentProfileId}&courseOfferingId={courseOfferingId}&resultType={Uri.EscapeDataString(resultType)}", payload, ct);
    }

    public Task PublishAllResultsAsync(Guid courseOfferingId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/result/publish-all?courseOfferingId={courseOfferingId}", new { }, ct);

    // ── Quizzes ───────────────────────────────────────────────────────────────

    public async Task<List<QuizItem>> GetQuizzesByOfferingAsync(Guid offeringId, CancellationToken ct)
    {
        var raw = await GetAsync<List<QuizApiDto>>($"api/v1/quiz/by-offering/{offeringId}", ct) ?? new();
        return raw.Select(q => new QuizItem
        {
            Id                  = q.Id,
            Title               = q.Title ?? "",
            Description         = q.Description,
            IsPublished         = q.IsPublished,
            IsActive            = q.IsActive,
            AvailableFrom       = q.AvailableFrom,
            AvailableTo         = q.AvailableTo,
            MaxAttempts         = q.MaxAttempts,
            TimeLimitMinutes    = q.TimeLimitMinutes,
            QuestionCount       = q.QuestionCount,
            CourseOfferingTitle = q.CourseOfferingTitle ?? ""
        }).ToList();
    }

    public async Task<List<QuizAttemptItem>> GetMyAttemptsAsync(CancellationToken ct)
    {
        // Fetch attempts across all quizzes student has accessed
        var raw = await GetAsync<List<QuizAttemptApiDto>>("api/v1/quiz/my-attempts", ct) ?? new();
        return raw.Select(a => new QuizAttemptItem
        {
            Id          = a.Id,
            QuizTitle   = a.QuizTitle ?? "",
            StartedAt   = a.StartedAt,
            SubmittedAt = a.SubmittedAt,
            Status      = a.Status ?? "",
            TotalScore  = a.TotalScore,
            MaxScore    = a.MaxScore
        }).ToList();
    }

    private sealed class QuizApiDto
    {
        public Guid      Id                  { get; set; }
        public string?   Title               { get; set; }
        public string?   Description         { get; set; }
        public bool      IsPublished         { get; set; }
        public bool      IsActive            { get; set; }
        public DateTime? AvailableFrom       { get; set; }
        public DateTime? AvailableTo         { get; set; }
        public int       MaxAttempts         { get; set; }
        public int?      TimeLimitMinutes    { get; set; }
        public int       QuestionCount       { get; set; }
        public string?   CourseOfferingTitle { get; set; }
    }

    private sealed class QuizAttemptApiDto
    {
        public Guid      Id          { get; set; }
        public string?   QuizTitle   { get; set; }
        public DateTime  StartedAt   { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string?   Status      { get; set; }
        public decimal?  TotalScore  { get; set; }
        public int       MaxScore    { get; set; }
    }

    // ── Quiz write methods ────────────────────────────────────────────────────

    public Task<Guid> CreateQuizAsync(Guid courseOfferingId, string title, string? instructions,
        int? timeLimitMinutes, int maxAttempts, CancellationToken ct)
    {
        var payload = new { courseOfferingId, title, instructions, timeLimitMinutes, maxAttempts };
        return PostAsync<object, QuizCreateResponse>("api/v1/quiz", payload, ct)
            .ContinueWith(t => t.Result?.QuizId ?? Guid.Empty, ct);
    }

    public Task UpdateQuizAsync(Guid id, string title, string? instructions,
        int? timeLimitMinutes, int maxAttempts, CancellationToken ct)
    {
        var payload = new
        {
            title,
            instructions,
            timeLimitMinutes,
            maxAttempts,
            availableFrom = (DateTime?)null,
            availableUntil = (DateTime?)null
        };
        return PutAsync<object, object>($"api/v1/quiz/{id}", payload, ct);
    }

    public Task PublishQuizAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/quiz/{id}/publish", new { }, ct);

    public Task DeleteQuizAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/quiz/{id}", ct);

    private sealed class QuizCreateResponse { public Guid QuizId { get; set; } }

    // ── FYP ───────────────────────────────────────────────────────────────────

    public async Task<List<FypProjectItem>> GetMyFypProjectsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<FypApiDto>>("api/v1/fyp/my-projects", ct) ?? new();
        return raw.Select(MapFyp).ToList();
    }

    public async Task<List<FypProjectItem>> GetAllFypProjectsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<FypApiDto>>("api/v1/fyp/all", ct) ?? new();
        return raw.Select(MapFyp).ToList();
    }

    public async Task<List<FypProjectItem>> GetFypByDepartmentAsync(Guid departmentId, CancellationToken ct)
    {
        var raw = await GetAsync<List<FypApiDto>>($"api/v1/fyp/by-department/{departmentId}", ct) ?? new();
        return raw.Select(MapFyp).ToList();
    }

    public async Task<List<FypProjectItem>> GetMySupervisedProjectsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<FypApiDto>>("api/v1/fyp/my-supervised", ct) ?? new();
        return raw.Select(MapFyp).ToList();
    }

    public async Task<List<FypMeetingItem>> GetUpcomingMeetingsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<FypMeetingApiDto>>("api/v1/fyp/meeting/upcoming", ct) ?? new();
        return raw.Select(m => new FypMeetingItem
        {
            Id           = m.Id,
            Title        = m.Title ?? "",
            ScheduledAt  = m.ScheduledAt,
            Status       = m.Status ?? "",
            Location     = m.Location,
            Notes        = m.Notes,
            ProjectTitle = m.ProjectTitle ?? ""
        }).ToList();
    }

    // ── FYP write methods ─────────────────────────────────────────────────────

    public Task<Guid> ProposeFypProjectAsync(Guid departmentId, string title, string description, CancellationToken ct)
    {
        var payload = new { departmentId, title, description };
        return PostAsync<object, FypCreateResponse>("api/v1/fyp", payload, ct)
            .ContinueWith(t => t.Result?.ProjectId ?? Guid.Empty, ct);
    }

    public Task<Guid> CreateFypProjectAsync(Guid studentProfileId, Guid departmentId, string title, string description, CancellationToken ct)
    {
        var payload = new { studentProfileId, departmentId, title, description };
        return PostAsync<object, FypCreateResponse>("api/v1/fyp/admin-create", payload, ct)
            .ContinueWith(t => t.Result?.ProjectId ?? Guid.Empty, ct);
    }

    public Task UpdateFypProjectAsync(Guid id, string title, string description, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/fyp/{id}", new { title, description }, ct);

    public Task ApproveFypProjectAsync(Guid id, string? remarks, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/fyp/{id}/approve", new { remarks }, ct);

    public Task RejectFypProjectAsync(Guid id, string remarks, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/fyp/{id}/reject", new { remarks }, ct);

    public Task AssignFypSupervisorAsync(Guid id, Guid supervisorUserId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/fyp/{id}/assign-supervisor", new { supervisorUserId }, ct);

    public Task CompleteFypProjectAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/fyp/{id}/complete", new { }, ct);

    public Task RequestFypCompletionAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/fyp/{id}/request-completion", new { }, ct);

    public Task ApproveFypCompletionAsync(Guid id, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/fyp/{id}/approve-completion", new { }, ct);

    private sealed class FypCreateResponse { public Guid ProjectId { get; set; } }

    private static FypProjectItem MapFyp(FypApiDto f) => new()
    {
        Id             = f.ProjectId != Guid.Empty ? f.ProjectId : f.Id,
        Title          = f.Title ?? "",
        Description    = f.Description,
        Status         = f.Status ?? "",
        StudentName    = f.StudentName ?? "",
        SupervisorName = f.SupervisorName,
        DepartmentName = f.DepartmentName ?? "",
        MeetingCount   = f.MeetingCount,
        IsCompletionRequested = f.IsCompletionRequested,
        CompletionApprovalCount = f.CompletionApprovalCount,
        RequiredApprovalCount = f.RequiredApprovalCount,
        CompletionApprovedByUserIds = f.CompletionApprovedByUserIds ?? new()
    };

    private sealed class FypApiDto
    {
        public Guid    ProjectId      { get; set; }
        public Guid    Id             { get; set; }
        public string? Title          { get; set; }
        public string? Description    { get; set; }
        public string? Status         { get; set; }
        public string? StudentName    { get; set; }
        public string? SupervisorName { get; set; }
        public string? DepartmentName { get; set; }
        public int     MeetingCount   { get; set; }
        public bool    IsCompletionRequested { get; set; }
        public int     CompletionApprovalCount { get; set; }
        public int     RequiredApprovalCount { get; set; }
        public List<Guid>? CompletionApprovedByUserIds { get; set; }
    }

    private sealed class FypMeetingApiDto
    {
        public Guid     Id           { get; set; }
        public string?  Title        { get; set; }
        public DateTime ScheduledAt  { get; set; }
        public string?  Status       { get; set; }
        public string?  Location     { get; set; }
        public string?  Notes        { get; set; }
        public string?  ProjectTitle { get; set; }
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    // Final-Touches Phase 6 Stage 6.2 — replaced raw JSON fetch with typed GetAsync<T> deserialization
    public Task<DepartmentPerformanceReport?> GetPerformanceAnalyticsAsync(Guid? departmentId, int? institutionType, Guid? courseId, Guid? semesterId, CancellationToken ct)
        => GetAsync<DepartmentPerformanceReport>($"api/v1/analytics/performance{BuildAnalyticsQuery(departmentId, institutionType, courseId, semesterId)}", ct);

    public Task<DepartmentAttendanceReport?> GetAttendanceAnalyticsAsync(Guid? departmentId, int? institutionType, Guid? courseId, Guid? semesterId, CancellationToken ct)
        => GetAsync<DepartmentAttendanceReport>($"api/v1/analytics/attendance{BuildAnalyticsQuery(departmentId, institutionType, courseId, semesterId)}", ct);

    public Task<AssignmentStatsReport?> GetAssignmentAnalyticsAsync(Guid? departmentId, int? institutionType, Guid? courseId, Guid? semesterId, CancellationToken ct)
        => GetAsync<AssignmentStatsReport>($"api/v1/analytics/assignments{BuildAnalyticsQuery(departmentId, institutionType, courseId, semesterId)}", ct);

    private static string BuildAnalyticsQuery(Guid? departmentId, int? institutionType, Guid? courseId, Guid? semesterId)
    {
        var parts = new List<string>();
        if (departmentId.HasValue)
            parts.Add($"departmentId={departmentId.Value}");
        if (institutionType.HasValue)
            parts.Add($"institutionType={institutionType.Value}");
        if (courseId.HasValue)
            parts.Add($"courseId={courseId.Value}");
        if (semesterId.HasValue)
            parts.Add($"semesterId={semesterId.Value}");

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    // ── AI Chat ───────────────────────────────────────────────────────────────

    public async Task<List<AiChatConversationItem>> GetChatConversationsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<ConversationApiDto>>("api/v1/ai/conversations", ct) ?? new();
        return raw.Select(c => new AiChatConversationItem
        {
            Id            = c.Id,
            Title         = BuildConversationTitle(c.UserRole, c.StartedAt),
            CreatedAt     = c.StartedAt,
            LastMessageAt = c.LastMessageAt
        }).ToList();
    }

    public async Task<List<AiChatMessageItem>> GetChatMessagesAsync(Guid conversationId, CancellationToken ct)
    {
        var raw = await GetAsync<ConversationDetailApiDto>($"api/v1/ai/conversations/{conversationId}", ct);
        return (raw?.Messages ?? new List<MessageApiDto>()).Select(m => new AiChatMessageItem
        {
            Id        = m.Id,
            Role      = m.Role ?? "user",
            Content   = m.Content ?? "",
            CreatedAt = m.SentAt
        }).ToList();
    }

    public async Task<AiChatSendResultItem?> SendChatMessageAsync(Guid? conversationId, string message, CancellationToken ct)
    {
        var payload = new { conversationId, message };
        var raw = await PostAsync<object, SendMessageApiDto>("api/v1/ai/message", payload, ct);
        if (raw is null) return null;
        return new AiChatSendResultItem
        {
            ConversationId = raw.ConversationId,
            AssistantMessage = new AiChatMessageItem
            {
                Id        = raw.AssistantMessage?.Id ?? Guid.Empty,
                Role      = raw.AssistantMessage?.Role ?? "assistant",
                Content   = raw.AssistantMessage?.Content ?? string.Empty,
                CreatedAt = raw.AssistantMessage?.SentAt ?? DateTime.UtcNow
            }
        };
    }

    private static string BuildConversationTitle(string? userRole, DateTime startedAt)
    {
        var roleLabel = string.IsNullOrWhiteSpace(userRole) ? "AI Chat" : $"{userRole} assistant";
        return $"{roleLabel} · {startedAt:dd MMM}";
    }

    private sealed class SendMessageApiDto
    {
        public Guid ConversationId { get; set; }
        public MessageApiDto? AssistantMessage { get; set; }
    }

    private sealed class ConversationApiDto
    {
        public Guid      Id            { get; set; }
        public string?   UserRole      { get; set; }
        public DateTime  StartedAt     { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }

    private sealed class ConversationDetailApiDto
    {
        public Guid Id { get; set; }
        public string? UserRole { get; set; }
        public DateTime StartedAt { get; set; }
        public List<MessageApiDto> Messages { get; set; } = new();
    }

    private sealed class MessageApiDto
    {
        public Guid     Id        { get; set; }
        public string?  Role      { get; set; }
        public string?  Content   { get; set; }
        public DateTime SentAt    { get; set; }
    }

    // ── Student Lifecycle ─────────────────────────────────────────────────────

    public async Task<List<GraduationCandidateItem>> GetGraduationCandidatesAsync(Guid departmentId, CancellationToken ct)
    {
        var raw = await GetAsync<List<GradCandidateApiDto>>($"api/v1/student-lifecycle/graduation-candidates/{departmentId}", ct) ?? new();
        return raw.Select(g => new GraduationCandidateItem
        {
            Id                 = g.Id,
            FullName           = g.FullName ?? "",
            RegistrationNumber = g.RegistrationNumber ?? "",
            ProgramName        = g.ProgramName ?? "",
            SemesterNumber     = g.SemesterNumber,
            Cgpa               = g.Cgpa
        }).ToList();
    }

    public Task GraduateStudentAsync(Guid studentId, CancellationToken ct)
        => PostAsync<object, object>("api/v1/student-lifecycle/graduate", new { studentProfileId = studentId }, ct);

    public Task GraduateStudentsBatchAsync(List<Guid> studentIds, CancellationToken ct)
        => PostAsync<object, object>("api/v1/student-lifecycle/graduate/batch", new { studentProfileIds = studentIds }, ct);

    public async Task<List<StudentItem>> GetStudentsByAcademicLevelAsync(Guid departmentId, int levelNumber, CancellationToken ct)
    {
        var raw = await GetAsync<List<StudentApiDto>>($"api/v1/student-lifecycle/academic-level-students/{departmentId}/{levelNumber}", ct) ?? new();
        return raw.Select(MapStudent).ToList();
    }

    public Task<List<StudentItem>> GetStudentsBySemesterAsync(Guid departmentId, int semesterNumber, CancellationToken ct)
        => GetStudentsByAcademicLevelAsync(departmentId, semesterNumber, ct);

    public Task PromoteStudentAsync(Guid studentId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/student-lifecycle/{studentId}/promote", new { }, ct);

    private sealed class GradCandidateApiDto
    {
        public Guid    Id                 { get; set; }
        public string? FullName           { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? ProgramName        { get; set; }
        public int     SemesterNumber     { get; set; }
        public double? Cgpa               { get; set; }
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    public async Task<PaymentReceiptPageItem> GetPaymentsByStudentAsync(Guid studentId, int page, int pageSize, CancellationToken ct)
    {
        var raw = await GetAsync<PaymentPageApiDto>($"api/v1/payments/student/{studentId}?page={page}&pageSize={pageSize}", ct);
        return raw is null ? new PaymentReceiptPageItem() : MapPaymentPage(raw);
    }

    private static PaymentReceiptItem MapPayment(PaymentApiDto p) => new()
    {
        Id                 = p.Id,
        StudentProfileId   = p.StudentProfileId,
        StudentName        = p.StudentName ?? "",
        RegistrationNumber = p.RegistrationNumber ?? "",
        Amount             = p.Amount,
        FeeType            = p.Description ?? p.FeeType ?? "",
        Status             = p.Status ?? "",
        DueDate            = p.DueDate,
        PaidDate           = p.PaidDate ?? p.ConfirmedAt,
        UpdatedAt          = p.UpdatedAt,
        ProofOfPaymentPath = p.ProofOfPaymentPath,
        Notes              = p.Notes
    };

    // Final-Touches Phase 7 — admin and student payment actions
    public async Task<PaymentReceiptPageItem> GetAllPaymentsAsync(int page, int pageSize, CancellationToken ct)
    {
        var raw = await GetAsync<PaymentPageApiDto>($"api/v1/payments?page={page}&pageSize={pageSize}", ct);
        return raw is null ? new PaymentReceiptPageItem() : MapPaymentPage(raw);
    }

    public async Task<PaymentReceiptPageItem> GetMyPaymentsAsync(int page, int pageSize, CancellationToken ct)
    {
        var raw = await GetAsync<PaymentPageApiDto>($"api/v1/payments/mine?page={page}&pageSize={pageSize}", ct);
        return raw is null ? new PaymentReceiptPageItem() : MapPaymentPage(raw);
    }

    private static PaymentReceiptPageItem MapPaymentPage(PaymentPageApiDto raw) => new()
    {
        Items = raw.Items?.Select(MapPayment).ToList() ?? new(),
        Page = raw.Page,
        PageSize = raw.PageSize,
        TotalCount = raw.TotalCount
    };

    public Task CreatePaymentAsync(Guid studentProfileId, decimal amount, string description, DateTime dueDate, CancellationToken ct)
        => PostAsync<object, object>("api/v1/payments", new { studentProfileId, amount, description, dueDate }, ct);

    public Task UpdatePaymentAsync(Guid receiptId, decimal amount, string description, DateTime dueDate, string? notes, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/payments/{receiptId}", new { amount, description, dueDate, notes }, ct);

    public Task ConfirmPaymentAsync(Guid receiptId, CancellationToken ct)
        => PostAsync<string, object>($"api/v1/payments/{receiptId}/confirm", string.Empty, ct);

    public Task CancelPaymentAsync(Guid receiptId, CancellationToken ct)
        => PostAsync<string, object>($"api/v1/payments/{receiptId}/cancel", string.Empty, ct);

    public Task SubmitProofAsync(Guid receiptId, string proofNote, CancellationToken ct)
        => PostAsync<string, object>($"api/v1/payments/{receiptId}/mark-submitted", proofNote, ct);

    private sealed class PaymentApiDto
    {
        public Guid     Id                 { get; set; }
        public Guid     StudentProfileId   { get; set; }
        public string?  StudentName        { get; set; }
        public string?  RegistrationNumber { get; set; }
        public decimal  Amount             { get; set; }
        public string?  FeeType            { get; set; }
        public string?  Description        { get; set; }
        public string?  Status             { get; set; }
        public DateTime DueDate            { get; set; }
        public DateTime? PaidDate          { get; set; }
        public DateTime? ConfirmedAt       { get; set; }
        public DateTime? UpdatedAt         { get; set; }
        public string?  ProofOfPaymentPath { get; set; }
        public string?  Notes              { get; set; }
    }

    private sealed class PaymentPageApiDto
    {
        public List<PaymentApiDto>? Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    // ── Enrollments ───────────────────────────────────────────────────────────

    public async Task<List<EnrollmentRosterItem>> GetEnrollmentRosterAsync(Guid offeringId, CancellationToken ct)
    {
        var raw = await GetAsync<List<RosterApiDto>>($"api/v1/enrollment/roster/{offeringId}", ct) ?? new();
        return raw.Select(r => new EnrollmentRosterItem
        {
            Id                 = r.Id,
            StudentName        = r.StudentName ?? "",
            RegistrationNumber = r.RegistrationNumber ?? "",
            ProgramName        = r.ProgramName ?? "",
            SemesterNumber     = r.SemesterNumber
        }).ToList();
    }

    // Final-Touches Phase 8 Stage 8.1+8.2 — student my-courses, admin enroll/drop, student enroll/drop
    public async Task<List<MyEnrollmentItem>> GetMyEnrollmentsAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<MyCourseApiDto>>("api/v1/enrollment/my-courses", ct) ?? new();
        return raw.Select(e => new MyEnrollmentItem
        {
            EnrollmentId     = e.Id,
            CourseOfferingId = e.CourseOfferingId,
            CourseCode       = e.CourseCode ?? "",
            CourseTitle      = e.CourseTitle ?? "",
            SemesterName     = e.Semester ?? "",
            Status           = e.Status ?? "",
            EnrolledAt       = e.EnrolledAt
        }).ToList();
    }

    public async Task AdminEnrollStudentAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct)
        => await PostAsync<object, object>("api/v1/enrollment/admin",
               new { StudentProfileId = studentProfileId, CourseOfferingId = courseOfferingId }, ct);

    public async Task AdminDropEnrollmentAsync(Guid enrollmentId, CancellationToken ct)
        => await DeleteAsync($"api/v1/enrollment/admin/{enrollmentId}", ct);

    public async Task StudentEnrollAsync(Guid courseOfferingId, CancellationToken ct)
        => await PostAsync<object, object>("api/v1/enrollment",
               new { CourseOfferingId = courseOfferingId }, ct);

    public async Task StudentDropEnrollmentAsync(Guid courseOfferingId, CancellationToken ct)
        => await DeleteAsync($"api/v1/enrollment/{courseOfferingId}", ct);

    private sealed class MyCourseApiDto
    {
        public Guid     Id               { get; set; }
        public Guid     CourseOfferingId { get; set; }
        public string?  CourseTitle      { get; set; }
        public string?  CourseCode       { get; set; }
        public string?  Semester         { get; set; }
        public string?  Status           { get; set; }
        public DateTime EnrolledAt       { get; set; }
    }

    private sealed class RosterApiDto
    {
        public Guid    Id                 { get; set; }
        public string? StudentName        { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? ProgramName        { get; set; }
        public int     SemesterNumber     { get; set; }
    }

    // ── Phase 12: Report methods ───────────────────────────────────────────────

    public async Task<List<ReportCatalogItem>> GetReportCatalogAsync(CancellationToken ct)
    {
        var raw = await GetAsync<ReportCatalogApiDto>("api/v1/reports", ct);
        if (raw?.Reports is null) return new();
        return raw.Reports.Select(r => new ReportCatalogItem
        {
            Id           = r.Id,
            Key          = r.Key ?? "",
            Name         = r.Name ?? "",
            Purpose      = r.Purpose ?? "",
            AllowedRoles = r.AllowedRoles ?? new()
        }).ToList();
    }

    public async Task<AttendanceSummaryWebModel?> GetAttendanceSummaryReportAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        var raw = await GetAsync<AttendanceSummaryApiDto>($"api/v1/reports/attendance-summary{qs}", ct);
        if (raw is null) return null;
        return new AttendanceSummaryWebModel
        {
            TotalStudents = raw.TotalStudents,
            GeneratedAt   = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new AttendanceSummaryRowItem
            {
                RegistrationNumber   = r.RegistrationNumber ?? "",
                StudentName          = r.StudentName ?? "",
                CourseCode           = r.CourseCode ?? "",
                CourseTitle          = r.CourseTitle ?? "",
                TotalSessions        = r.TotalSessions,
                AttendedSessions     = r.AttendedSessions,
                AttendancePercentage = r.AttendancePercentage
            }).ToList() ?? new()
        };
    }

    public async Task<ResultSummaryWebModel?> GetResultSummaryReportAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        var raw = await GetAsync<ResultSummaryApiDto>($"api/v1/reports/result-summary{qs}", ct);
        if (raw is null) return null;
        return new ResultSummaryWebModel
        {
            TotalRecords = raw.TotalRecords,
            GeneratedAt  = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new ResultSummaryRowItem
            {
                RegistrationNumber = r.RegistrationNumber ?? "",
                StudentName        = r.StudentName ?? "",
                CourseCode         = r.CourseCode ?? "",
                CourseTitle        = r.CourseTitle ?? "",
                ResultType         = r.ResultType ?? "",
                MarksObtained      = r.MarksObtained,
                MaxMarks           = r.MaxMarks,
                Percentage         = r.Percentage,
                PublishedAt        = r.PublishedAt
            }).ToList() ?? new()
        };
    }

    public async Task<AssignmentSummaryWebModel?> GetAssignmentSummaryReportAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        var raw = await GetAsync<AssignmentSummaryApiDto>($"api/v1/reports/assignment-summary{qs}", ct);
        if (raw is null) return null;
        return new AssignmentSummaryWebModel
        {
            TotalSubmissions = raw.TotalSubmissions,
            GeneratedAt      = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new AssignmentSummaryRowItem
            {
                RegistrationNumber = r.RegistrationNumber ?? "",
                StudentName        = r.StudentName ?? "",
                CourseCode         = r.CourseCode ?? "",
                CourseTitle        = r.CourseTitle ?? "",
                AssignmentTitle    = r.AssignmentTitle ?? "",
                DueDate            = r.DueDate,
                SubmittedAt        = r.SubmittedAt,
                Status             = r.Status ?? "",
                MarksAwarded       = r.MarksAwarded
            }).ToList() ?? new()
        };
    }

    public async Task<QuizSummaryWebModel?> GetQuizSummaryReportAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        var raw = await GetAsync<QuizSummaryApiDto>($"api/v1/reports/quiz-summary{qs}", ct);
        if (raw is null) return null;
        return new QuizSummaryWebModel
        {
            TotalAttempts = raw.TotalAttempts,
            GeneratedAt   = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new QuizSummaryRowItem
            {
                RegistrationNumber = r.RegistrationNumber ?? "",
                StudentName        = r.StudentName ?? "",
                CourseCode         = r.CourseCode ?? "",
                CourseTitle        = r.CourseTitle ?? "",
                QuizTitle          = r.QuizTitle ?? "",
                StartedAt          = r.StartedAt,
                FinishedAt         = r.FinishedAt,
                AttemptStatus      = r.AttemptStatus ?? "",
                TotalScore         = r.TotalScore
            }).ToList() ?? new()
        };
    }

    public async Task<GpaReportWebModel?> GetGpaReportAsync(
        Guid? departmentId, Guid? programId, int? institutionType, CancellationToken ct)
    {
        var parts = new List<string>();
        if (departmentId.HasValue) parts.Add($"departmentId={departmentId.Value}");
        if (programId.HasValue)    parts.Add($"programId={programId.Value}");
        if (institutionType.HasValue) parts.Add($"institutionType={institutionType.Value}");
        var qs = parts.Any() ? "?" + string.Join("&", parts) : "";
        var raw = await GetAsync<GpaReportApiDto>($"api/v1/reports/gpa-report{qs}", ct);
        if (raw is null) return null;
        return new GpaReportWebModel
        {
            AverageCgpa   = raw.AverageCgpa,
            TotalStudents = raw.TotalStudents,
            GeneratedAt   = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new GpaReportRowItem
            {
                RegistrationNumber = r.RegistrationNumber ?? "",
                StudentName        = r.StudentName ?? "",
                ProgramName        = r.ProgramName ?? "",
                DepartmentName     = r.DepartmentName ?? "",
                CurrentSemester    = r.CurrentSemester,
                Cgpa               = r.Cgpa,
                CurrentSemesterGpa = r.CurrentSemesterGpa
            }).ToList() ?? new()
        };
    }

    public async Task<EnrollmentSummaryWebModel?> GetEnrollmentSummaryReportAsync(
        Guid? semesterId, Guid? departmentId, int? institutionType, CancellationToken ct)
    {
        var parts = new List<string>();
        if (semesterId.HasValue)   parts.Add($"semesterId={semesterId.Value}");
        if (departmentId.HasValue) parts.Add($"departmentId={departmentId.Value}");
        if (institutionType.HasValue) parts.Add($"institutionType={institutionType.Value}");
        var qs = parts.Any() ? "?" + string.Join("&", parts) : "";
        var raw = await GetAsync<EnrollmentSummaryApiDto>($"api/v1/reports/enrollment-summary{qs}", ct);
        if (raw is null) return null;
        return new EnrollmentSummaryWebModel
        {
            TotalOfferings = raw.TotalOfferings,
            GeneratedAt    = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new EnrollmentSummaryRowItem
            {
                CourseCode      = r.CourseCode ?? "",
                CourseTitle     = r.CourseTitle ?? "",
                SemesterName    = r.SemesterName ?? "",
                MaxEnrollment   = r.MaxEnrollment,
                EnrolledCount   = r.EnrolledCount,
                AvailableSeats  = r.AvailableSeats
            }).ToList() ?? new()
        };
    }

    private static string BuildReportQuery(Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType)
    {
        var parts = new List<string>();
        if (semesterId.HasValue)   parts.Add($"semesterId={semesterId.Value}");
        if (departmentId.HasValue) parts.Add($"departmentId={departmentId.Value}");
        if (offeringId.HasValue)   parts.Add($"courseOfferingId={offeringId.Value}");
        if (studentId.HasValue)    parts.Add($"studentProfileId={studentId.Value}");
        if (institutionType.HasValue) parts.Add($"institutionType={institutionType.Value}");
        return parts.Any() ? "?" + string.Join("&", parts) : "";
    }

    // semesterId is required by the API; departmentId is optional and narrows the result set
    // to a single department when provided.
    public async Task<SemesterResultsWebModel?> GetSemesterResultsReportAsync(
        Guid semesterId, Guid? departmentId, int? institutionType, CancellationToken ct)
    {
        var parts = new List<string> { $"semesterId={semesterId}" };
        if (departmentId.HasValue) parts.Add($"departmentId={departmentId.Value}");
        if (institutionType.HasValue) parts.Add($"institutionType={institutionType.Value}");
        var raw = await GetAsync<SemesterResultsApiDto>($"api/v1/reports/semester-results?{string.Join("&", parts)}", ct);
        if (raw is null) return null;
        return new SemesterResultsWebModel
        {
            TotalStudents = raw.TotalStudents,
            GeneratedAt   = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new SemesterResultsRowItem
            {
                RegistrationNumber = r.RegistrationNumber ?? "",
                StudentName        = r.StudentName ?? "",
                CourseCode         = r.CourseCode ?? "",
                CourseTitle        = r.CourseTitle ?? "",
                ResultType         = r.ResultType ?? "",
                MarksObtained      = r.MarksObtained,
                MaxMarks           = r.MaxMarks,
                Percentage         = r.Percentage
            }).ToList() ?? new()
        };
    }

    // All three export methods delegate to GetBytesAsync because the API returns a
    // binary .xlsx file, not JSON. BuildReportQuery assembles the shared filter params.

    public Task<byte[]> ExportAttendanceSummaryAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/attendance-summary/export{qs}", ct);
    }

    public Task<byte[]> ExportAttendanceSummaryCsvAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/attendance-summary/export/csv{qs}", ct);
    }

    public Task<byte[]> ExportAttendanceSummaryPdfAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/attendance-summary/export/pdf{qs}", ct);
    }

    public Task<byte[]> ExportResultSummaryAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/result-summary/export{qs}", ct);
    }

    public Task<byte[]> ExportResultSummaryCsvAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/result-summary/export/csv{qs}", ct);
    }

    public Task<byte[]> ExportResultSummaryPdfAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/result-summary/export/pdf{qs}", ct);
    }

    public Task<byte[]> ExportAssignmentSummaryAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/assignment-summary/export{qs}", ct);
    }

    public Task<byte[]> ExportAssignmentSummaryCsvAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/assignment-summary/export/csv{qs}", ct);
    }

    public Task<byte[]> ExportAssignmentSummaryPdfAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/assignment-summary/export/pdf{qs}", ct);
    }

    public Task<byte[]> ExportQuizSummaryAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/quiz-summary/export{qs}", ct);
    }

    public Task<byte[]> ExportQuizSummaryCsvAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/quiz-summary/export/csv{qs}", ct);
    }

    public Task<byte[]> ExportQuizSummaryPdfAsync(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var qs = BuildReportQuery(semesterId, departmentId, offeringId, studentId, institutionType);
        return GetBytesAsync($"api/v1/reports/quiz-summary/export/pdf{qs}", ct);
    }

    // GPA report uses department + program filters only (no per-offering or per-student scope).
    public Task<byte[]> ExportGpaReportAsync(Guid? departmentId, Guid? programId, int? institutionType, CancellationToken ct)
    {
        var parts = new List<string>();
        if (departmentId.HasValue) parts.Add($"departmentId={departmentId.Value}");
        if (programId.HasValue)    parts.Add($"programId={programId.Value}");
        if (institutionType.HasValue) parts.Add($"institutionType={institutionType.Value}");
        var qs = parts.Any() ? "?" + string.Join("&", parts) : "";
        return GetBytesAsync($"api/v1/reports/gpa-report/export{qs}", ct);
    }

    // ── Stage 4.2: Additional Report Proxy Methods ─────────────────────────────

    public async Task<TranscriptWebModel?> GetStudentTranscriptReportAsync(
        Guid studentProfileId, CancellationToken ct)
    {
        var raw = await GetAsync<TranscriptApiDto>(
            $"api/v1/reports/student-transcript?studentProfileId={studentProfileId}", ct);
        if (raw is null) return null;
        return new TranscriptWebModel
        {
            StudentProfileId   = raw.StudentProfileId,
            RegistrationNumber = raw.RegistrationNumber ?? "",
            StudentName        = raw.StudentName ?? "",
            ProgramName        = raw.ProgramName ?? "",
            DepartmentName     = raw.DepartmentName ?? "",
            Cgpa               = raw.Cgpa,
            GeneratedAt        = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new TranscriptRowItem
            {
                CourseCode    = r.CourseCode ?? "",
                CourseTitle   = r.CourseTitle ?? "",
                SemesterName  = r.SemesterName ?? "",
                ResultType    = r.ResultType ?? "",
                MarksObtained = r.MarksObtained,
                MaxMarks      = r.MaxMarks,
                Percentage    = r.Percentage,
                GradePoint    = r.GradePoint,
                PublishedAt   = r.PublishedAt
            }).ToList() ?? new()
        };
    }

    public async Task<LowAttendanceWebModel?> GetLowAttendanceReportAsync(
        decimal threshold, Guid? departmentId, Guid? courseOfferingId, int? institutionType, CancellationToken ct)
    {
        var parts = new List<string> { $"threshold={threshold}" };
        if (departmentId.HasValue)     parts.Add($"departmentId={departmentId.Value}");
        if (courseOfferingId.HasValue) parts.Add($"courseOfferingId={courseOfferingId.Value}");
        if (institutionType.HasValue)  parts.Add($"institutionType={institutionType.Value}");
        var raw = await GetAsync<LowAttendanceApiDto>(
            $"api/v1/reports/low-attendance?{string.Join("&", parts)}", ct);
        if (raw is null) return null;
        return new LowAttendanceWebModel
        {
            ThresholdPercent    = raw.ThresholdPercent,
            TotalStudentsAtRisk = raw.TotalStudentsAtRisk,
            GeneratedAt         = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new LowAttendanceRowItem
            {
                RegistrationNumber   = r.RegistrationNumber ?? "",
                StudentName          = r.StudentName ?? "",
                CourseCode           = r.CourseCode ?? "",
                CourseTitle          = r.CourseTitle ?? "",
                SemesterName         = r.SemesterName ?? "",
                DepartmentName       = r.DepartmentName ?? "",
                TotalSessions        = r.TotalSessions,
                AttendedSessions     = r.AttendedSessions,
                AttendancePercentage = r.AttendancePercentage
            }).ToList() ?? new()
        };
    }

    public async Task<FypStatusWebModel?> GetFypStatusReportAsync(
        Guid? departmentId, string? status, int? institutionType, CancellationToken ct)
    {
        var parts = new List<string>();
        if (departmentId.HasValue)         parts.Add($"departmentId={departmentId.Value}");
        if (!string.IsNullOrEmpty(status)) parts.Add($"status={Uri.EscapeDataString(status)}");
        if (institutionType.HasValue)      parts.Add($"institutionType={institutionType.Value}");
        var qs = parts.Any() ? "?" + string.Join("&", parts) : "";
        var raw = await GetAsync<FypStatusApiDto>($"api/v1/reports/fyp-status{qs}", ct);
        if (raw is null) return null;
        return new FypStatusWebModel
        {
            TotalProjects = raw.TotalProjects,
            GeneratedAt   = raw.GeneratedAt,
            Rows = raw.Rows?.Select(r => new FypStatusRowItem
            {
                Title              = r.Title ?? "",
                StudentName        = r.StudentName ?? "",
                RegistrationNumber = r.RegistrationNumber ?? "",
                DepartmentName     = r.DepartmentName ?? "",
                SupervisorName     = r.SupervisorName,
                Status             = r.Status ?? "",
                ProposedAt         = r.ProposedAt,
                MeetingCount       = r.MeetingCount
            }).ToList() ?? new()
        };
    }

    public Task<byte[]> ExportStudentTranscriptAsync(Guid studentProfileId, CancellationToken ct)
        => GetBytesAsync($"api/v1/reports/student-transcript/export?studentProfileId={studentProfileId}", ct);

    // Private API DTOs for Phase 12
    private sealed class ReportCatalogApiDto
    {
        public List<ReportCatalogItemApiDto>? Reports { get; set; }
    }
    private sealed class ReportCatalogItemApiDto
    {
        public Guid    Id           { get; set; }
        public string? Key          { get; set; }
        public string? Name         { get; set; }
        public string? Purpose      { get; set; }
        public bool    IsActive     { get; set; }
        public List<string> AllowedRoles { get; set; } = new();
    }
    private sealed class AttendanceSummaryApiDto
    {
        public int      TotalStudents { get; set; }
        public DateTime GeneratedAt   { get; set; }
        public List<AttendanceSummaryRowApiDto>? Rows { get; set; }
    }
    private sealed class AttendanceSummaryRowApiDto
    {
        public string? RegistrationNumber   { get; set; }
        public string? StudentName          { get; set; }
        public string? CourseCode           { get; set; }
        public string? CourseTitle          { get; set; }
        public int     TotalSessions        { get; set; }
        public int     AttendedSessions     { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
    private sealed class ResultSummaryApiDto
    {
        public int      TotalRecords { get; set; }
        public DateTime GeneratedAt  { get; set; }
        public List<ResultSummaryRowApiDto>? Rows { get; set; }
    }
    private sealed class ResultSummaryRowApiDto
    {
        public string?   RegistrationNumber { get; set; }
        public string?   StudentName        { get; set; }
        public string?   CourseCode         { get; set; }
        public string?   CourseTitle        { get; set; }
        public string?   ResultType         { get; set; }
        public decimal   MarksObtained      { get; set; }
        public decimal   MaxMarks           { get; set; }
        public decimal   Percentage         { get; set; }
        public DateTime? PublishedAt        { get; set; }
    }
    private sealed class AssignmentSummaryApiDto
    {
        public int      TotalSubmissions { get; set; }
        public DateTime GeneratedAt      { get; set; }
        public List<AssignmentSummaryRowApiDto>? Rows { get; set; }
    }
    private sealed class AssignmentSummaryRowApiDto
    {
        public string?   RegistrationNumber { get; set; }
        public string?   StudentName        { get; set; }
        public string?   CourseCode         { get; set; }
        public string?   CourseTitle        { get; set; }
        public string?   AssignmentTitle    { get; set; }
        public DateTime  DueDate            { get; set; }
        public DateTime  SubmittedAt        { get; set; }
        public string?   Status             { get; set; }
        public decimal?  MarksAwarded       { get; set; }
    }
    private sealed class QuizSummaryApiDto
    {
        public int      TotalAttempts { get; set; }
        public DateTime GeneratedAt   { get; set; }
        public List<QuizSummaryRowApiDto>? Rows { get; set; }
    }
    private sealed class QuizSummaryRowApiDto
    {
        public string?   RegistrationNumber { get; set; }
        public string?   StudentName        { get; set; }
        public string?   CourseCode         { get; set; }
        public string?   CourseTitle        { get; set; }
        public string?   QuizTitle          { get; set; }
        public DateTime  StartedAt          { get; set; }
        public DateTime? FinishedAt         { get; set; }
        public string?   AttemptStatus      { get; set; }
        public decimal?  TotalScore         { get; set; }
    }
    private sealed class GpaReportApiDto
    {
        public decimal  AverageCgpa   { get; set; }
        public int      TotalStudents { get; set; }
        public DateTime GeneratedAt   { get; set; }
        public List<GpaReportRowApiDto>? Rows { get; set; }
    }
    private sealed class GpaReportRowApiDto
    {
        public string? RegistrationNumber { get; set; }
        public string? StudentName        { get; set; }
        public string? ProgramName        { get; set; }
        public string? DepartmentName     { get; set; }
        public int     CurrentSemester    { get; set; }
        public decimal Cgpa               { get; set; }
        public decimal CurrentSemesterGpa { get; set; }
    }
    private sealed class EnrollmentSummaryApiDto
    {
        public int      TotalOfferings { get; set; }
        public DateTime GeneratedAt    { get; set; }
        public List<EnrollmentSummaryRowApiDto>? Rows { get; set; }
    }
    private sealed class EnrollmentSummaryRowApiDto
    {
        public string? CourseCode     { get; set; }
        public string? CourseTitle    { get; set; }
        public string? SemesterName   { get; set; }
        public int     MaxEnrollment  { get; set; }
        public int     EnrolledCount  { get; set; }
        public int     AvailableSeats { get; set; }
    }
    // Internal DTOs for the Semester Results report endpoint.
    // These are private to EduApiClient; the web layer uses SemesterResultsWebModel instead.
    private sealed class SemesterResultsApiDto
    {
        public int      TotalStudents { get; set; }
        public DateTime GeneratedAt   { get; set; }
        public List<SemesterResultsRowApiDto>? Rows { get; set; }
    }
    private sealed class SemesterResultsRowApiDto
    {
        public string? RegistrationNumber { get; set; }
        public string? StudentName        { get; set; }
        public string? CourseCode         { get; set; }
        public string? CourseTitle        { get; set; }
        public string? ResultType         { get; set; }
        public decimal MarksObtained      { get; set; }
        public decimal MaxMarks           { get; set; }
        public decimal Percentage         { get; set; }
    }

    // Stage 4.2 private DTOs
    private sealed class TranscriptApiDto
    {
        public Guid    StudentProfileId   { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? StudentName        { get; set; }
        public string? ProgramName        { get; set; }
        public string? DepartmentName     { get; set; }
        public decimal Cgpa               { get; set; }
        public DateTime GeneratedAt       { get; set; }
        public List<TranscriptRowApiDto>? Rows { get; set; }
    }
    private sealed class TranscriptRowApiDto
    {
        public string?   CourseCode    { get; set; }
        public string?   CourseTitle   { get; set; }
        public string?   SemesterName  { get; set; }
        public string?   ResultType    { get; set; }
        public decimal   MarksObtained { get; set; }
        public decimal   MaxMarks      { get; set; }
        public decimal   Percentage    { get; set; }
        public decimal?  GradePoint    { get; set; }
        public DateTime? PublishedAt   { get; set; }
    }
    private sealed class LowAttendanceApiDto
    {
        public decimal  ThresholdPercent    { get; set; }
        public int      TotalStudentsAtRisk { get; set; }
        public DateTime GeneratedAt         { get; set; }
        public List<LowAttendanceRowApiDto>? Rows { get; set; }
    }
    private sealed class LowAttendanceRowApiDto
    {
        public string? RegistrationNumber   { get; set; }
        public string? StudentName          { get; set; }
        public string? CourseCode           { get; set; }
        public string? CourseTitle          { get; set; }
        public string? SemesterName         { get; set; }
        public string? DepartmentName       { get; set; }
        public int     TotalSessions        { get; set; }
        public int     AttendedSessions     { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
    private sealed class FypStatusApiDto
    {
        public int      TotalProjects { get; set; }
        public DateTime GeneratedAt   { get; set; }
        public List<FypStatusRowApiDto>? Rows { get; set; }
    }
    private sealed class FypStatusRowApiDto
    {
        public string?   Title              { get; set; }
        public string?   StudentName        { get; set; }
        public string?   RegistrationNumber { get; set; }
        public string?   DepartmentName     { get; set; }
        public string?   SupervisorName     { get; set; }
        public string?   Status             { get; set; }
        public DateTime  ProposedAt         { get; set; }
        public int       MeetingCount       { get; set; }
    }

    // ── Portal / Dashboard Settings ───────────────────────────────────────────

    public async Task<PortalBrandingWebModel> GetPortalBrandingAsync(CancellationToken ct)
    {
        var raw = await GetAsync<PortalBrandingApiDto>("api/v1/portal-settings", ct);
        return new PortalBrandingWebModel
        {
            UniversityName   = raw?.UniversityName   ?? "Tabsan EduSphere",
            PortalSubtitle   = raw?.PortalSubtitle   ?? "Campus Portal",
            FooterText       = raw?.FooterText       ?? "© 2026 Tabsan EduSphere",
            LogoImage        = raw?.LogoImage,
            PrivacyPolicyUrl = raw?.PrivacyPolicyUrl,
            PrivacyPolicyContent = raw?.PrivacyPolicyContent,
            FontFamily       = raw?.FontFamily,
            FontSize         = raw?.FontSize
        };
    }

    public async Task SavePortalBrandingAsync(PortalBrandingWebModel model, CancellationToken ct)
    {
        var payload = new
        {
            universityName   = model.UniversityName,
            portalSubtitle   = model.PortalSubtitle,
            footerText       = model.FooterText,
            logoImage        = model.LogoImage,
            privacyPolicyUrl = model.PrivacyPolicyUrl,
            privacyPolicyContent = model.PrivacyPolicyContent,
            fontFamily       = model.FontFamily,
            fontSize         = model.FontSize
        };
        await PostAsync<object, object>("api/v1/portal-settings", payload, ct);
    }

    public async Task<string?> UploadLogoAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        using var request  = CreateRequest(HttpMethod.Post, "api/v1/portal-settings/logo");
        request.Content    = content;
        using var response = await CreateClient().SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) return null;
        var json = JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body, _jsonOptions);
        return json.TryGetProperty("url", out var urlProp) ? NormalizeApiAssetUrl(urlProp.GetString()) : null;
    }

    private string? NormalizeApiAssetUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return rawUrl;
        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out _)) return rawUrl;

        var baseUrl = GetConnection().ApiBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return rawUrl;

        return rawUrl.StartsWith('/') ? $"{baseUrl}{rawUrl}" : $"{baseUrl}/{rawUrl}";
    }

    private sealed class PortalBrandingApiDto
    {
        public string? UniversityName   { get; set; }
        public string? PortalSubtitle   { get; set; }
        public string? FooterText       { get; set; }
        public string? LogoImage        { get; set; }
        public string? PrivacyPolicyUrl { get; set; }
        public string? PrivacyPolicyContent { get; set; }
        public string? FontFamily       { get; set; }
        public string? FontSize         { get; set; }
    }

    // ── Phase 12: Academic Calendar & Deadlines ──────────────────────────────

    public async Task<List<DeadlineWebItem>> GetCalendarDeadlinesAsync(Guid? semesterId, CancellationToken ct)
    {
        var path = semesterId.HasValue
            ? $"api/v1/calendar/deadlines/by-semester/{semesterId.Value}"
            : "api/v1/calendar/deadlines";
        return await GetAsync<List<DeadlineWebItem>>(path, ct) ?? new();
    }

    public Task<DeadlineWebItem?> GetCalendarDeadlineByIdAsync(Guid id, CancellationToken ct)
        => GetAsync<DeadlineWebItem>($"api/v1/calendar/deadlines/{id}", ct);

    public async Task CreateCalendarDeadlineAsync(DeadlineFormModel form, CancellationToken ct)
    {
        var payload = new
        {
            semesterId         = form.SemesterId,
            title              = form.Title,
            description        = form.Description,
            deadlineDate       = form.DeadlineDate,
            reminderDaysBefore = form.ReminderDaysBefore
        };
        await PostAsync<object, object>("api/v1/calendar/deadlines", payload, ct);
    }

    public async Task UpdateCalendarDeadlineAsync(Guid id, DeadlineFormModel form, CancellationToken ct)
    {
        var payload = new
        {
            title              = form.Title,
            description        = form.Description,
            deadlineDate       = form.DeadlineDate,
            reminderDaysBefore = form.ReminderDaysBefore,
            isActive           = form.IsActive
        };
        await PutAsync<object, object>($"api/v1/calendar/deadlines/{id}", payload, ct);
    }

    public Task DeleteCalendarDeadlineAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/calendar/deadlines/{id}", ct);

    // ── Phase 13: Global Search ───────────────────────────────────────────────

    public async Task<SearchWebResponse> SearchAsync(string term, int limit, CancellationToken ct)
    {
        var encoded = Uri.EscapeDataString(term);
        var raw = await GetAsync<SearchApiDto>($"api/v1/search?q={encoded}&limit={limit}", ct);
        if (raw is null) return new SearchWebResponse(term, 0, new());
        return new SearchWebResponse(
            raw.Term ?? term,
            raw.TotalHits,
            raw.Results?.Select(r => new SearchWebItem
            {
                Type     = r.Type     ?? "",
                Id       = r.Id,
                Label    = r.Label    ?? "",
                SubLabel = r.SubLabel ?? "",
                Url      = r.Url      ?? ""
            }).ToList() ?? new());
    }

    // ── Phase 13 API DTOs (private) ───────────────────────────────────────────
    private sealed class SearchApiDto
    {
        public string?                  Term      { get; set; }
        public int                      TotalHits { get; set; }
        public List<SearchResultApiDto>? Results  { get; set; }
    }
    private sealed class SearchResultApiDto
    {
        public string? Type     { get; set; }
        public Guid    Id       { get; set; }
        public string? Label    { get; set; }
        public string? SubLabel { get; set; }
        public string? Url      { get; set; }
    }

    // ── Phase 14: Helpdesk / Support Ticketing ────────────────────────────────

    public async Task<TicketSummaryPageItem> GetTicketsAsync(TicketStatusWeb? status, int page, int pageSize, CancellationToken ct)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (status.HasValue)
            query.Add($"status={(int)status.Value}");

        var raw = await GetAsync<TicketSummaryPageApiDto>($"api/v1/helpdesk/tickets?{string.Join("&", query)}", ct);
        return new TicketSummaryPageItem
        {
            Items = raw?.Items?.Select(MapSummary).ToList() ?? new(),
            Page = raw?.Page ?? page,
            PageSize = raw?.PageSize ?? pageSize,
            TotalCount = raw?.TotalCount ?? 0
        };
    }

    public async Task<TicketDetailItem?> GetTicketDetailAsync(Guid ticketId, CancellationToken ct)
    {
        var raw = await GetAsync<TicketDetailApiDto>($"api/v1/helpdesk/tickets/{ticketId}", ct);
        if (raw is null) return null;
        return new TicketDetailItem
        {
            Id               = raw.Id,
            Subject          = raw.Subject ?? "",
            Body             = raw.Body ?? "",
            Category         = (TicketCategoryWeb)raw.Category,
            Status           = (TicketStatusWeb)raw.Status,
            SubmitterId      = raw.SubmitterId,
            SubmitterName    = raw.SubmitterName ?? "",
            AssignedToId     = raw.AssignedToId,
            AssigneeName     = raw.AssigneeName,
            DepartmentId     = raw.DepartmentId,
            CreatedAt        = raw.CreatedAt,
            ResolvedAt       = raw.ResolvedAt,
            ReopenWindowDays = raw.ReopenWindowDays,
            CanReopen        = raw.CanReopen,
            Messages         = raw.Messages?.Select(m => new TicketMessageItem
            {
                Id             = m.Id,
                AuthorId       = m.AuthorId,
                AuthorName     = m.AuthorName ?? "",
                Body           = m.Body ?? "",
                IsInternalNote = m.IsInternalNote,
                CreatedAt      = m.CreatedAt
            }).ToList() ?? new()
        };
    }

    public async Task<Guid> CreateTicketAsync(Guid? departmentId, TicketCategoryWeb category,
        string subject, string body, CancellationToken ct)
    {
        var req = new { departmentId, category = (int)category, subject, body };
        var res = await PostAsync<object, TicketIdApiDto>("api/v1/helpdesk/tickets", req, ct);
        return res?.Id ?? Guid.Empty;
    }

    public async Task<Guid> AddTicketMessageAsync(Guid ticketId, string body, bool isInternalNote, CancellationToken ct)
    {
        var req = new { body, isInternalNote };
        var res = await PostAsync<object, TicketIdApiDto>($"api/v1/helpdesk/tickets/{ticketId}/messages", req, ct);
        return res?.Id ?? Guid.Empty;
    }

    public Task AssignTicketAsync(Guid ticketId, Guid assignedToId, CancellationToken ct)
    {
        var req = new { assignedToId };
        return PutAsync<object, object>($"api/v1/helpdesk/tickets/{ticketId}/assign", req, ct);
    }

    public Task ResolveTicketAsync(Guid ticketId, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/helpdesk/tickets/{ticketId}/resolve", new { }, ct);

    public Task CloseTicketAsync(Guid ticketId, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/helpdesk/tickets/{ticketId}/close", new { }, ct);

    public Task ReopenTicketAsync(Guid ticketId, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/helpdesk/tickets/{ticketId}/reopen", new { }, ct);

    // ── Phase 15: Enrollment Rules — Prerequisites ────────────────────────────

    public async Task<List<PrerequisiteWebItem>> GetPrerequisitesAsync(Guid courseId, CancellationToken ct)
    {
        var items = await GetAsync<List<PrerequisiteApiDto>>($"api/v1/prerequisite/{courseId}", ct)
                    ?? new List<PrerequisiteApiDto>();
        return items.Select(p => new PrerequisiteWebItem
        {
            CourseId                = p.CourseId,
            CourseCode              = p.CourseCode ?? "",
            CourseTitle             = p.CourseTitle ?? "",
            PrerequisiteCourseId    = p.PrerequisiteCourseId,
            PrerequisiteCourseCode  = p.PrerequisiteCourseCode ?? "",
            PrerequisiteCourseTitle = p.PrerequisiteCourseTitle ?? ""
        }).ToList();
    }

    public Task AddPrerequisiteAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct)
        => PostAsync<object, object>("api/v1/prerequisite",
               new { CourseId = courseId, PrerequisiteCourseId = prerequisiteCourseId }, ct);

    public Task RemovePrerequisiteAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct)
        => DeleteAsync($"api/v1/prerequisite/{courseId}/{prerequisiteCourseId}", ct);

    private sealed class PrerequisiteApiDto
    {
        public Guid    CourseId                { get; set; }
        public string? CourseCode              { get; set; }
        public string? CourseTitle             { get; set; }
        public Guid    PrerequisiteCourseId    { get; set; }
        public string? PrerequisiteCourseCode  { get; set; }
        public string? PrerequisiteCourseTitle { get; set; }
    }

    // ── Phase 16: Faculty Grading System ──────────────────────────────────────

    // Final-Touches Phase 16 Stage 16.1 — gradebook grid
    public async Task<GradebookGridWebModel?> GetGradebookAsync(Guid offeringId, CancellationToken ct)
    {
        var raw = await GetAsync<GradebookGridApiDto>($"api/v1/gradebook/{offeringId}", ct);
        if (raw is null) return null;
        return new GradebookGridWebModel
        {
            CourseOfferingId = raw.CourseOfferingId,
            Columns = raw.Columns?.Select(c => new GradebookColumnWebModel
            {
                ComponentName = c.ComponentName ?? "",
                Weightage     = c.Weightage
            }).ToList() ?? new(),
            Rows = raw.Rows?.Select(r => new GradebookStudentRowWeb
            {
                StudentProfileId   = r.StudentProfileId,
                RegistrationNumber = r.RegistrationNumber ?? "",
                StudentName        = r.StudentName ?? "",
                WeightedTotal      = r.WeightedTotal,
                Cells = r.Cells?.Select(cell => new GradebookCellWebModel
                {
                    ComponentName = cell.ComponentName ?? "",
                    MarksObtained = cell.MarksObtained,
                    MaxMarks      = cell.MaxMarks,
                    IsPublished   = cell.IsPublished
                }).ToList() ?? new()
            }).ToList() ?? new()
        };
    }

    // Final-Touches Phase 16 Stage 16.1 — upsert single result cell
    public Task UpsertGradebookEntryAsync(
        Guid offeringId,
        Guid studentProfileId,
        string componentName,
        decimal marksObtained,
        decimal maxMarks,
        CancellationToken ct)
        => PutAsync<object, object>(
            $"api/v1/gradebook/{offeringId}/entry",
            new { studentProfileId, courseOfferingId = offeringId, componentName, marksObtained, maxMarks },
            ct);

    // Final-Touches Phase 16 Stage 16.1 — publish all results for offering
    public Task PublishGradebookAllAsync(Guid offeringId, CancellationToken ct)
        => PostAsync<object, object>($"api/v1/gradebook/{offeringId}/publish-all", new { }, ct);

    // Final-Touches Phase 16 Stage 16.3 — download CSV template
    public Task<byte[]> GetGradebookCsvTemplateAsync(Guid offeringId, string componentName, CancellationToken ct)
        => GetBytesAsync($"api/v1/gradebook/{offeringId}/template?component={Uri.EscapeDataString(componentName)}", ct);

    // Final-Touches Phase 16 Stage 16.3 — upload CSV for preview
    public async Task<BulkGradePreviewWebModel?> UploadBulkGradeCsvAsync(
        Guid offeringId,
        string componentName,
        Stream csvStream,
        string fileName,
        CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(csvStream), "file", fileName);
        using var request = CreateRequest(HttpMethod.Post,
            $"api/v1/gradebook/{offeringId}/bulk-grade?component={Uri.EscapeDataString(componentName)}");
        request.Content = content;
        using var response = await CreateClient().SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);
        var raw = JsonSerializer.Deserialize<BulkGradePreviewApiDto>(body, _jsonOptions);
        if (raw is null) return null;
        return new BulkGradePreviewWebModel
        {
            ComponentName = raw.ComponentName ?? "",
            TotalRows     = raw.TotalRows,
            ValidRows     = raw.ValidRows,
            ErrorRows     = raw.ErrorRows,
            Rows = raw.Rows?.Select(r => new BulkGradeRowWeb
            {
                RegistrationNumber = r.RegistrationNumber ?? "",
                StudentName        = r.StudentName ?? "",
                StudentProfileId   = r.StudentProfileId,
                MarksObtained      = r.MarksObtained,
                MaxMarks           = r.MaxMarks,
                ValidationError    = r.ValidationError
            }).ToList() ?? new()
        };
    }

    // Final-Touches Phase 16 Stage 16.3 — confirm bulk grade
    public Task ConfirmBulkGradeAsync(Guid offeringId, BulkGradeConfirmWebRequest request, CancellationToken ct)
    {
        var payload = new
        {
            courseOfferingId = request.CourseOfferingId,
            componentName    = request.ComponentName,
            maxMarks         = request.MaxMarks,
            validRows        = request.ValidRows.Select(r => new
            {
                registrationNumber = r.RegistrationNumber,
                studentName        = r.StudentName,
                studentProfileId   = r.StudentProfileId,
                marksObtained      = r.MarksObtained,
                maxMarks           = r.MaxMarks
            }).ToList()
        };
        return PostAsync<object, object>($"api/v1/gradebook/{offeringId}/bulk-grade/confirm", payload, ct);
    }

    // Final-Touches Phase 16 Stage 16.2 — get rubric by assignment
    public async Task<RubricWebModel?> GetRubricByAssignmentAsync(Guid assignmentId, CancellationToken ct)
    {
        var raw = await GetAsync<RubricApiDto>($"api/v1/rubric/assignment/{assignmentId}", ct);
        return raw is null ? null : MapRubricApiDto(raw);
    }

    // Final-Touches Phase 16 Stage 16.2 — create rubric
    public async Task<Guid> CreateRubricAsync(CreateRubricWebRequest request, CancellationToken ct)
    {
        var payload = new
        {
            assignmentId = request.AssignmentId,
            title        = request.Title,
            criteria     = request.Criteria.Select(c => new
            {
                name         = c.Name,
                maxPoints    = c.MaxPoints,
                displayOrder = c.DisplayOrder,
                levels       = c.Levels.Select(l => new { label = l.Label, pointsAwarded = l.PointsAwarded, displayOrder = l.DisplayOrder })
            })
        };
        var result = await PostAsync<object, RubricCreateResponseApiDto>("api/v1/rubric", payload, ct);
        return result?.RubricId ?? Guid.Empty;
    }

    // Final-Touches Phase 16 Stage 16.2 — update rubric title
    public Task UpdateRubricAsync(Guid rubricId, string title, CancellationToken ct)
        => PutAsync<object, object>($"api/v1/rubric/{rubricId}", new { title }, ct);

    // Final-Touches Phase 16 Stage 16.2 — delete (deactivate) rubric
    public Task DeleteRubricAsync(Guid rubricId, CancellationToken ct)
        => DeleteAsync($"api/v1/rubric/{rubricId}", ct);

    // Final-Touches Phase 16 Stage 16.2 — get rubric grade for submission
    public async Task<RubricGradeWebModel?> GetRubricGradeAsync(Guid rubricId, Guid submissionId, CancellationToken ct)
    {
        var raw = await GetAsync<RubricGradeApiDto>($"api/v1/rubric/{rubricId}/grade/{submissionId}", ct);
        return raw is null ? null : MapRubricGradeDto(raw);
    }

    // Final-Touches Phase 16 Stage 16.2 — submit rubric grades for a submission
    public async Task<RubricGradeWebModel?> GradeRubricSubmissionAsync(
        Guid rubricId,
        RubricGradeWebRequest request,
        CancellationToken ct)
    {
        var payload = new
        {
            submissionId = request.SubmissionId,
            grades       = request.Grades.Select(g => new { criterionId = g.CriterionId, levelId = g.LevelId })
        };
        var raw = await PostAsync<object, RubricGradeApiDto>($"api/v1/rubric/{rubricId}/grade", payload, ct);
        return raw is null ? null : MapRubricGradeDto(raw);
    }

    // ── Phase 17: Degree Audit System ──────────────────────────────────────────

    // Final-Touches Phase 17 Stage 17.1 — student own degree audit
    public async Task<DegreeAuditWebModel?> GetMyDegreeAuditAsync(CancellationToken ct)
    {
        var raw = await GetAsync<DegreeAuditApiDto>("api/v1/degree-audit/me", ct);
        return raw is null ? null : MapDegreeAudit(raw);
    }

    // Final-Touches Phase 17 Stage 17.1 — admin/faculty fetch student audit
    public async Task<DegreeAuditWebModel?> GetStudentDegreeAuditAsync(Guid studentProfileId, CancellationToken ct)
    {
        var raw = await GetAsync<DegreeAuditApiDto>($"api/v1/degree-audit/{studentProfileId}", ct);
        return raw is null ? null : MapDegreeAudit(raw);
    }

    // Final-Touches Phase 17 Stage 17.2 — eligibility list
    public async Task<List<EligibilityListWebItem>> GetEligibilityListAsync(
        Guid? departmentId, Guid? programId, CancellationToken ct)
    {
        var qs = "";
        if (departmentId.HasValue) qs += $"?departmentId={departmentId}";
        if (programId.HasValue)    qs += (qs.Length > 0 ? "&" : "?") + $"programId={programId}";
        var raw = await GetAsync<List<EligibilityListApiDto>>($"api/v1/degree-audit/eligible{qs}", ct);
        return raw?.Select(r => new EligibilityListWebItem
        {
            StudentProfileId   = r.StudentProfileId,
            StudentName        = r.StudentName ?? "",
            RegistrationNumber = r.RegistrationNumber ?? "",
            Cgpa               = r.Cgpa,
            TotalCreditsEarned = r.TotalCreditsEarned,
            IsEligible         = r.IsEligible,
            UnmetCount         = r.UnmetCount
        }).ToList() ?? new();
    }

    // Final-Touches Phase 17 Stage 17.2 — list all degree rules
    public async Task<List<DegreeRuleWebModel>> GetAllDegreeRulesAsync(CancellationToken ct)
    {
        var raw = await GetAsync<List<DegreeRuleApiDto>>("api/v1/degree-audit/rule", ct);
        return raw?.Select(MapDegreeRule).ToList() ?? new();
    }

    // Final-Touches Phase 17 Stage 17.2 — rule by program
    public async Task<DegreeRuleWebModel?> GetDegreeRuleByProgramAsync(Guid programId, CancellationToken ct)
    {
        var raw = await GetAsync<DegreeRuleApiDto>($"api/v1/degree-audit/rule/{programId}", ct);
        return raw is null ? null : MapDegreeRule(raw);
    }

    // Final-Touches Phase 17 Stage 17.2 — create degree rule
    public async Task<DegreeRuleWebModel?> CreateDegreeRuleAsync(CreateDegreeRuleWebRequest request, CancellationToken ct)
    {
        var payload = new
        {
            academicProgramId  = request.AcademicProgramId,
            minTotalCredits    = request.MinTotalCredits,
            minCoreCredits     = request.MinCoreCredits,
            minElectiveCredits = request.MinElectiveCredits,
            minGpa             = request.MinGpa,
            requiredCourseIds  = request.RequiredCourseIds
        };
        var raw = await PostAsync<object, DegreeRuleApiDto>("api/v1/degree-audit/rule", payload, ct);
        return raw is null ? null : MapDegreeRule(raw);
    }

    // Final-Touches Phase 17 Stage 17.2 — update degree rule
    public async Task<DegreeRuleWebModel?> UpdateDegreeRuleAsync(Guid ruleId, UpdateDegreeRuleWebRequest request, CancellationToken ct)
    {
        var payload = new
        {
            minTotalCredits    = request.MinTotalCredits,
            minCoreCredits     = request.MinCoreCredits,
            minElectiveCredits = request.MinElectiveCredits,
            minGpa             = request.MinGpa,
            requiredCourseIds  = request.RequiredCourseIds
        };
        var raw = await PutAsync<object, DegreeRuleApiDto>($"api/v1/degree-audit/rule/{ruleId}", payload, ct);
        return raw is null ? null : MapDegreeRule(raw);
    }

    // Final-Touches Phase 17 Stage 17.2 — delete degree rule
    public async Task DeleteDegreeRuleAsync(Guid ruleId, CancellationToken ct)
        => await DeleteAsync($"api/v1/degree-audit/rule/{ruleId}", ct);

    // Final-Touches Phase 17 Stage 17.3 — set course type
    public async Task SetCourseTypeAsync(Guid courseId, string courseType, CancellationToken ct)
    {
        var payload = new { courseType };
        await PutAsync<object, object?>($"api/v1/degree-audit/course/{courseId}/type", payload, ct);
    }

    // Phase 17 API DTOs (private)
    private sealed class DegreeAuditApiDto
    {
        public Guid    StudentProfileId     { get; set; }
        public string? StudentName          { get; set; }
        public string? RegistrationNumber   { get; set; }
        public string? ProgramName          { get; set; }
        public decimal Cgpa                 { get; set; }
        public int     TotalCreditsEarned   { get; set; }
        public int     CoreCreditsEarned    { get; set; }
        public int     ElectiveCreditsEarned{ get; set; }
        public bool    IsEligible           { get; set; }
        public List<string>?        UnmetRequirements { get; set; }
        public List<EarnedCourseRowApiDto>? CompletedCourses { get; set; }
    }
    private sealed class EarnedCourseRowApiDto
    {
        public Guid    CourseId    { get; set; }
        public string? CourseCode  { get; set; }
        public string? CourseTitle { get; set; }
        public int     CreditHours { get; set; }
        public string? CourseType  { get; set; }
        public decimal? GradePoint { get; set; }
    }
    private sealed class DegreeRuleApiDto
    {
        public Guid    RuleId             { get; set; }
        public Guid    AcademicProgramId  { get; set; }
        public string? ProgramName        { get; set; }
        public int     MinTotalCredits    { get; set; }
        public int     MinCoreCredits     { get; set; }
        public int     MinElectiveCredits { get; set; }
        public decimal MinGpa             { get; set; }
        public List<RequiredCourseApiDto>? RequiredCourses { get; set; }
    }
    private sealed class RequiredCourseApiDto
    {
        public Guid    CourseId    { get; set; }
        public string? CourseCode  { get; set; }
        public string? CourseTitle { get; set; }
    }
    private sealed class EligibilityListApiDto
    {
        public Guid    StudentProfileId   { get; set; }
        public string? StudentName        { get; set; }
        public string? RegistrationNumber { get; set; }
        public decimal Cgpa               { get; set; }
        public int     TotalCreditsEarned { get; set; }
        public bool    IsEligible         { get; set; }
        public int     UnmetCount         { get; set; }
    }

    private static DegreeAuditWebModel MapDegreeAudit(DegreeAuditApiDto raw) => new()
    {
        StudentProfileId      = raw.StudentProfileId,
        StudentName           = raw.StudentName           ?? "",
        RegistrationNumber    = raw.RegistrationNumber    ?? "",
        ProgramName           = raw.ProgramName           ?? "",
        Cgpa                  = raw.Cgpa,
        TotalCreditsEarned    = raw.TotalCreditsEarned,
        CoreCreditsEarned     = raw.CoreCreditsEarned,
        ElectiveCreditsEarned = raw.ElectiveCreditsEarned,
        IsEligible            = raw.IsEligible,
        UnmetRequirements     = raw.UnmetRequirements ?? new(),
        CompletedCourses      = raw.CompletedCourses?.Select(r => new EarnedCourseRowWebItem
        {
            CourseId    = r.CourseId,
            CourseCode  = r.CourseCode  ?? "",
            CourseTitle = r.CourseTitle ?? "",
            CreditHours = r.CreditHours,
            CourseType  = r.CourseType  ?? "Core",
            GradePoint  = r.GradePoint
        }).ToList() ?? new()
    };

    private static DegreeRuleWebModel MapDegreeRule(DegreeRuleApiDto raw) => new()
    {
        RuleId             = raw.RuleId,
        AcademicProgramId  = raw.AcademicProgramId,
        ProgramName        = raw.ProgramName        ?? "",
        MinTotalCredits    = raw.MinTotalCredits,
        MinCoreCredits     = raw.MinCoreCredits,
        MinElectiveCredits = raw.MinElectiveCredits,
        MinGpa             = raw.MinGpa,
        RequiredCourses    = raw.RequiredCourses?.Select(r => new RequiredCourseWebItem
        {
            CourseId    = r.CourseId,
            CourseCode  = r.CourseCode  ?? "",
            CourseTitle = r.CourseTitle ?? ""
        }).ToList() ?? new()
    };

    // Phase 16 API DTOs (private)
    private sealed class GradebookGridApiDto
    {
        public Guid CourseOfferingId { get; set; }
        public List<GradebookColumnApiDto>?      Columns { get; set; }
        public List<GradebookStudentRowApiDto>?  Rows    { get; set; }
    }
    private sealed class GradebookColumnApiDto
    {
        public string?  ComponentName { get; set; }
        public decimal  Weightage     { get; set; }
    }
    private sealed class GradebookStudentRowApiDto
    {
        public Guid    StudentProfileId   { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? StudentName        { get; set; }
        public List<GradebookCellApiDto>? Cells { get; set; }
        public decimal? WeightedTotal { get; set; }
    }
    private sealed class GradebookCellApiDto
    {
        public string?  ComponentName { get; set; }
        public decimal? MarksObtained { get; set; }
        public decimal? MaxMarks      { get; set; }
        public bool     IsPublished   { get; set; }
    }
    private sealed class BulkGradePreviewApiDto
    {
        public string? ComponentName { get; set; }
        public int     TotalRows     { get; set; }
        public int     ValidRows     { get; set; }
        public int     ErrorRows     { get; set; }
        public List<BulkGradeRowApiDto>? Rows { get; set; }
    }
    private sealed class BulkGradeRowApiDto
    {
        public string?  RegistrationNumber { get; set; }
        public string?  StudentName        { get; set; }
        public Guid?    StudentProfileId   { get; set; }
        public decimal? MarksObtained      { get; set; }
        public decimal? MaxMarks           { get; set; }
        public string?  ValidationError    { get; set; }
    }
    private sealed class RubricApiDto
    {
        public Guid    RubricId     { get; set; }
        public Guid    AssignmentId { get; set; }
        public string? Title        { get; set; }
        public bool    IsActive     { get; set; }
        public List<RubricCriterionApiDto>? Criteria { get; set; }
    }
    private sealed class RubricCriterionApiDto
    {
        public Guid    CriterionId  { get; set; }
        public string? Name         { get; set; }
        public decimal MaxPoints    { get; set; }
        public int     DisplayOrder { get; set; }
        public List<RubricLevelApiDto>? Levels { get; set; }
    }
    private sealed class RubricLevelApiDto
    {
        public Guid    LevelId       { get; set; }
        public string? Label         { get; set; }
        public decimal PointsAwarded { get; set; }
        public int     DisplayOrder  { get; set; }
    }
    private sealed class RubricCreateResponseApiDto { public Guid RubricId { get; set; } }
    private sealed class RubricGradeApiDto
    {
        public Guid    SubmissionId   { get; set; }
        public Guid    RubricId       { get; set; }
        public string? RubricTitle    { get; set; }
        public decimal TotalPoints    { get; set; }
        public decimal MaxTotalPoints { get; set; }
        public List<RubricCriterionGradeApiDto>? CriteriaResults { get; set; }
    }
    private sealed class RubricCriterionGradeApiDto
    {
        public Guid    CriterionId   { get; set; }
        public string? CriterionName { get; set; }
        public decimal MaxPoints     { get; set; }
        public Guid?   ChosenLevelId { get; set; }
        public string? ChosenLabel   { get; set; }
        public decimal PointsAwarded { get; set; }
    }

    private static RubricWebModel MapRubricApiDto(RubricApiDto raw) => new()
    {
        RubricId     = raw.RubricId,
        AssignmentId = raw.AssignmentId,
        Title        = raw.Title ?? "",
        IsActive     = raw.IsActive,
        Criteria = raw.Criteria?.Select(c => new RubricCriterionWebModel
        {
            CriterionId  = c.CriterionId,
            Name         = c.Name ?? "",
            MaxPoints    = c.MaxPoints,
            DisplayOrder = c.DisplayOrder,
            Levels = c.Levels?.Select(l => new RubricLevelWebModel
            {
                LevelId       = l.LevelId,
                Label         = l.Label ?? "",
                PointsAwarded = l.PointsAwarded,
                DisplayOrder  = l.DisplayOrder
            }).ToList() ?? new()
        }).ToList() ?? new()
    };

    private static RubricGradeWebModel MapRubricGradeDto(RubricGradeApiDto raw) => new()
    {
        SubmissionId   = raw.SubmissionId,
        RubricId       = raw.RubricId,
        RubricTitle    = raw.RubricTitle ?? "",
        TotalPoints    = raw.TotalPoints,
        MaxTotalPoints = raw.MaxTotalPoints,
        CriteriaResults = raw.CriteriaResults?.Select(r => new RubricCriterionGradeWebModel
        {
            CriterionId   = r.CriterionId,
            CriterionName = r.CriterionName ?? "",
            MaxPoints     = r.MaxPoints,
            ChosenLevelId = r.ChosenLevelId,
            ChosenLabel   = r.ChosenLabel,
            PointsAwarded = r.PointsAwarded
        }).ToList() ?? new()
    };

    // ── Phase 14 API DTOs (private) ───────────────────────────────────────────

    private static TicketSummaryItem MapSummary(TicketSummaryApiDto d) => new()
    {
        Id            = d.Id,
        Subject       = d.Subject       ?? "",
        Category      = (TicketCategoryWeb)d.Category,
        Status        = (TicketStatusWeb)d.Status,
        SubmitterId   = d.SubmitterId,
        SubmitterName = d.SubmitterName ?? "",
        AssignedToId  = d.AssignedToId,
        AssigneeName  = d.AssigneeName,
        DepartmentId  = d.DepartmentId,
        CreatedAt     = d.CreatedAt,
        ResolvedAt    = d.ResolvedAt,
        MessageCount  = d.MessageCount
    };

    private sealed class TicketIdApiDto         { public Guid Id { get; set; } }
    private sealed class TicketSummaryPageApiDto
    {
        public List<TicketSummaryApiDto>? Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    private sealed class TicketSummaryApiDto
    {
        public Guid    Id            { get; set; }
        public string? Subject       { get; set; }
        public int     Category      { get; set; }
        public int     Status        { get; set; }
        public Guid    SubmitterId   { get; set; }
        public string? SubmitterName { get; set; }
        public Guid?   AssignedToId  { get; set; }
        public string? AssigneeName  { get; set; }
        public Guid?   DepartmentId  { get; set; }
        public DateTime CreatedAt   { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int     MessageCount  { get; set; }
    }
    private sealed class TicketDetailApiDto
    {
        public Guid    Id               { get; set; }
        public string? Subject          { get; set; }
        public string? Body             { get; set; }
        public int     Category         { get; set; }
        public int     Status           { get; set; }
        public Guid    SubmitterId      { get; set; }
        public string? SubmitterName    { get; set; }
        public Guid?   AssignedToId     { get; set; }
        public string? AssigneeName     { get; set; }
        public Guid?   DepartmentId     { get; set; }
        public DateTime  CreatedAt      { get; set; }
        public DateTime? ResolvedAt     { get; set; }
        public int     ReopenWindowDays { get; set; }
        public bool    CanReopen        { get; set; }
        public List<TicketMessageApiDto>? Messages { get; set; }
    }
    private sealed class TicketMessageApiDto
    {
        public Guid    Id             { get; set; }
        public Guid    AuthorId       { get; set; }
        public string? AuthorName     { get; set; }
        public string? Body           { get; set; }
        public bool    IsInternalNote { get; set; }
        public DateTime CreatedAt    { get; set; }
    }
}

// Final-Touches Phase 19 Stage 19.4 — grading config API response model (top-level for interface visibility)
public sealed class GradingConfigApiModel
{
    public Guid    Id              { get; set; }
    public Guid    CourseId        { get; set; }
    public decimal PassThreshold   { get; set; }
    public string? GradingType     { get; set; }
    public string? GradeRangesJson { get; set; }
}

public sealed class InstitutionGradingProfileApiModel
{
    public Guid    Id              { get; set; }
    public int     InstitutionType { get; set; }
    public decimal PassThreshold   { get; set; }
    public string? GradeRangesJson { get; set; }
    public bool    IsActive        { get; set; }
}

// ── Phase 20: LMS API models ───────────────────────────────────────────────────

public sealed class LmsVideoApiModel
{
    public Guid    Id              { get; set; }
    public Guid    ModuleId        { get; set; }
    public string  Title           { get; set; } = string.Empty;
    public string? StorageUrl      { get; set; }
    public string? EmbedUrl        { get; set; }
    public int?    DurationSeconds { get; set; }
}

public sealed class LmsModuleApiModel
{
    public Guid    Id          { get; set; }
    public Guid    OfferingId  { get; set; }
    public string  Title       { get; set; } = string.Empty;
    public int     WeekNumber  { get; set; }
    public string? Body        { get; set; }
    public bool    IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<LmsVideoApiModel> Videos { get; set; } = new();
}

public sealed class DiscussionReplyApiModel
{
    public Guid     Id         { get; set; }
    public Guid     ThreadId   { get; set; }
    public Guid     AuthorId   { get; set; }
    public string   AuthorName { get; set; } = string.Empty;
    public string   Body       { get; set; } = string.Empty;
    public DateTime CreatedAt  { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class DiscussionThreadApiModel
{
    public Guid     Id         { get; set; }
    public Guid     OfferingId { get; set; }
    public string   Title      { get; set; } = string.Empty;
    public Guid     AuthorId   { get; set; }
    public string   AuthorName { get; set; } = string.Empty;
    public bool     IsPinned   { get; set; }
    public bool     IsClosed   { get; set; }
    public int      ReplyCount { get; set; }
    public DateTime CreatedAt  { get; set; }
    public List<DiscussionReplyApiModel> Replies { get; set; } = new();
}

public sealed class AnnouncementApiModel
{
    public Guid     Id         { get; set; }
    public Guid?    OfferingId { get; set; }
    public Guid     AuthorId   { get; set; }
    public string   AuthorName { get; set; } = string.Empty;
    public string   Title      { get; set; } = string.Empty;
    public string   Body       { get; set; } = string.Empty;
    public DateTime PostedAt   { get; set; }
}

public sealed class CourseMaterialApiModel
{
    public Guid     Id                { get; set; }
    public Guid     TenantId          { get; set; }
    public Guid     CampusId          { get; set; }
    public Guid     DepartmentId      { get; set; }
    public Guid     AcademicProgramId { get; set; }
    public Guid     SemesterId        { get; set; }
    public Guid     CourseId          { get; set; }
    public string   MaterialType      { get; set; } = string.Empty;
    public string   Title             { get; set; } = string.Empty;
    public string?  Description       { get; set; }
    public string?  ExternalUrl       { get; set; }
    public string?  BlobPath          { get; set; }
    public string?  FileName          { get; set; }
    public long?    FileSizeBytes     { get; set; }
    public bool     IsActive          { get; set; }
    public DateTime CreatedAt         { get; set; }
    public DateTime UpdatedAt         { get; set; }
}

public sealed class CourseMaterialUploadApiModel
{
    public string BlobPath      { get; set; } = string.Empty;
    public string FileUrl       { get; set; } = string.Empty;
    public string FileName      { get; set; } = string.Empty;
    public long   FileSizeBytes { get; set; }
    public string ContentType   { get; set; } = string.Empty;
}

public sealed class CourseMaterialFileDownloadApiModel
{
    public byte[] Content      { get; set; } = Array.Empty<byte>();
    public string ContentType  { get; set; } = "application/octet-stream";
    public string FileName     { get; set; } = "course-material";
}

// ── Phase 21: Study Planner API models ──────────────────────────────────────

public sealed class StudyPlanCourseApiModel
{
    public Guid   CourseId    { get; set; }
    public string CourseCode  { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int    CreditHours { get; set; }
    public string CourseType  { get; set; } = string.Empty;
}

public sealed class StudyPlanApiModel
{
    public Guid                          Id                  { get; set; }
    public Guid                          StudentProfileId    { get; set; }
    public string                        PlannedSemesterName { get; set; } = string.Empty;
    public string?                       Notes               { get; set; }
    public string                        AdvisorStatus       { get; set; } = string.Empty;
    public string?                       AdvisorNotes        { get; set; }
    public Guid?                         ReviewedByUserId    { get; set; }
    public int                           TotalCreditHours    { get; set; }
    public List<StudyPlanCourseApiModel> Courses             { get; set; } = new();
    public DateTime                      CreatedAt           { get; set; }
    public DateTime?                     UpdatedAt           { get; set; }
}

public sealed class RecommendedCourseApiModel
{
    public Guid   CourseId    { get; set; }
    public string CourseCode  { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int    CreditHours { get; set; }
    public string CourseType  { get; set; } = string.Empty;
    public string Reason      { get; set; } = string.Empty;
}

public sealed class StudyPlanRecommendationApiModel
{
    public Guid                              StudentProfileId        { get; set; }
    public string                            PlannedSemesterName     { get; set; } = string.Empty;
    public int                               MaxCreditLoad           { get; set; }
    public int                               RecommendedTotalCredits { get; set; }
    public List<RecommendedCourseApiModel>   Recommendations         { get; set; } = new();
}

// ── Phase 22: External Integrations ─────────────────────────────────────────

public sealed class LibraryConfigApiModel
{
    public string? CatalogueUrl { get; set; }
    public string? ApiToken     { get; set; }
    public string? LoanApiUrl   { get; set; }
}

public sealed class LibraryLoanItemApiModel
{
    public string    Title     { get; set; } = string.Empty;
    public string?   Author    { get; set; }
    public DateTime? DueDate   { get; set; }
    public string    Status    { get; set; } = string.Empty;
    public bool      IsOverdue { get; set; }
}

public sealed class LibraryLoansApiModel
{
    public bool                          IsConfigured  { get; set; }
    public string?                       ErrorMessage  { get; set; }
    public List<LibraryLoanItemApiModel> Loans         { get; set; } = new();
}

public sealed class AccreditationTemplateApiModel
{
    public Guid      Id                { get; set; }
    public string    Name              { get; set; } = string.Empty;
    public string?   Description       { get; set; }
    public string    Format            { get; set; } = "CSV";
    public string?   FieldMappingsJson { get; set; }
    public bool      IsActive          { get; set; }
    public DateTime  CreatedAt         { get; set; }
}

public sealed class CreateAccreditationTemplateForm
{
    public string  Name              { get; set; } = string.Empty;
    public string? Description       { get; set; }
    public string  Format            { get; set; } = "CSV";
    public string? FieldMappingsJson { get; set; }
}

public sealed class UpdateAccreditationTemplateForm
{
    public string  Name              { get; set; } = string.Empty;
    public string? Description       { get; set; }
    public string  Format            { get; set; } = "CSV";
    public string? FieldMappingsJson { get; set; }
    public bool    IsActive          { get; set; }
}

// ── Phase 23 API models ───────────────────────────────────────────────────────
public sealed class InstitutionPolicyApiModel
{
    public bool IncludeSchool     { get; set; }
    public bool IncludeCollege    { get; set; }
    public bool IncludeUniversity { get; set; } = true;
    public bool IsValid           { get; set; }
}

// ── Phase 24 API models ───────────────────────────────────────────────────────
public sealed class ModuleVisibilityApiModel
{
    public string Key          { get; set; } = "";
    public string Name         { get; set; } = "";
    public bool   IsActive     { get; set; }
    public bool   IsAccessible { get; set; }
}

public sealed class AcademicVocabularyApiModel
{
    public string PeriodLabel       { get; set; } = "Semester";
    public string ProgressionLabel  { get; set; } = "Progression";
    public string GradingLabel      { get; set; } = "GPA/CGPA";
    public string CourseLabel       { get; set; } = "Course";
    public string StudentGroupLabel { get; set; } = "Batch";
}

public sealed class WidgetDescriptorApiModel
{
    public string Key   { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon  { get; set; } = "";
    public int    Order { get; set; }
}

public sealed class DashboardCompositionContextApiModel
{
    public List<ModuleVisibilityApiModel> Modules { get; set; } = new();
    public AcademicVocabularyApiModel? Vocabulary { get; set; }
    public List<WidgetDescriptorApiModel> Widgets { get; set; } = new();
}

// ── Phase 27 API models ───────────────────────────────────────────────────────
public sealed class PortalCapabilityMatrixApiModel
{
    public bool IncludeSchool { get; set; }
    public bool IncludeCollege { get; set; }
    public bool IncludeUniversity { get; set; }
    public List<PortalCapabilityMatrixRowApiModel> Rows { get; set; } = new();
}

public sealed class PortalCapabilityMatrixRowApiModel
{
    public string CapabilityKey { get; set; } = "";
    public string CapabilityName { get; set; } = "";
    public string ModuleKey { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public string Route { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsModuleActive { get; set; }
    public bool IsLicenseGated { get; set; }
    public bool Student { get; set; }
    public bool Faculty { get; set; }
    public bool Admin { get; set; }
    public bool SuperAdmin { get; set; }
    public bool University { get; set; }
    public bool School { get; set; }
    public bool College { get; set; }
}
