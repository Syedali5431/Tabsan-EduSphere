| Function Name | Purpose | Location |
|--------------|--------|----------|
| AddHealthChecks | Registers database, memory, CPU, network, and error-rate checks for continuous runtime health monitoring. | src/Tabsan.EduSphere.API/Program.cs |
| AddOpenTelemetry | Publishes ASP.NET Core, HttpClient, runtime, and process metrics and exposes Prometheus scraping support. | src/Tabsan.EduSphere.API/Program.cs |
| AddResponseCompression (API) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | src/Tabsan.EduSphere.API/Program.cs |
| AddResponseCompression (Web) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | src/Tabsan.EduSphere.Web/Program.cs |
| AdminUserController.Create | Accepts optional `institutionType`, validates against active policy, persists to user, and returns assignment in response/list payloads. | src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs |
| AiChatService.GetConversationAsync | Returns a full conversation thread with message history for the requesting user. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AiChatService.GetConversationsAsync | Returns the requesting user's conversation list for the AI chat experience. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AiChatService.SendMessageAsync | Sends a user message to the AI provider and persists the response in conversation history. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AnalyticsController.GetPaymentStatus | Accepts and forwards course/semester filters for payment analytics requests. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| AnalyticsService.BuildAnalyticsCacheKey(...) | Enforces cache scope boundaries by keying shared analytics cache entries to report type and department scope. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAssignmentStatsAsync(...) | Adds short-TTL distributed cache policy for expensive assignment analytics reads. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAssignmentStatsAsync(..., courseId, semesterId) | Applies course/semester-scoped assignment filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAttendanceReportAsync(...) | Adds short-TTL distributed cache policy for expensive attendance analytics reads. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAttendanceReportAsync(..., courseId, semesterId) | Applies course/semester-scoped attendance filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPaymentStatusReportAsync | Aggregates scoped paid vs unpaid receipt counts/amounts for analytics consumption. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPaymentStatusReportAsync(..., courseId, semesterId) | Restricts payment status aggregation to students enrolled in matching course/semester scope. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPerformanceReportAsync(...) | Adds short-TTL distributed cache policy for expensive department/all performance analytics reads. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPerformanceReportAsync(..., courseId, semesterId) | Applies course/semester-scoped performance filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetQuizStatsAsync(...) | Adds short-TTL distributed cache policy for expensive quiz analytics reads. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| BuildAnalyticsCacheKey(..., accessScope, ...) | Partitions analytics cache entries by tenant/campus and caller scope profile to prevent cross-scope cache collisions. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| BuildingRoomRepository.GetAllBuildingsAsync(...) | Returns building lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| BuildingRoomRepository.GetAllRoomsAsync(...) | Returns room lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| BuildingRoomRepository.GetRoomsByBuildingAsync(...) | Returns building-scoped room lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| CanAccessExportJob | Enforces owner-or-superadmin and tenant/campus scope parity checks for export job access. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| CourseController.GetOfferings | Exposes department metadata for deterministic dependent filtering in web layer. | src/Tabsan.EduSphere.API/Controllers/CourseController.cs |
| CourseMaterial.EnsureMaterialLocation | Enforces material-type-specific file/link requirements before persistence. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.EnsureRequiredScope | Prevents creation of unscoped material records by rejecting empty scope identifiers. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.UpdateLocation | Updates material storage location for file/link and validates required location by material type. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.UpdateMetadata | Updates material title and description metadata. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterialController.BuildScopedStorageCategory | Builds tenant/campus-aware storage category path for Course Material upload isolation. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.Create | Creates scoped material records for authorized Faculty/Admin/SuperAdmin users using caller identity. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.DownloadFile | Streams stored material files with metadata-aware content type and file name for authorized scoped users. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.GetAll | Returns course materials with strict repository-enforced tenant/campus filtering. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.Upload | Uploads validated material files and persists them in scoped storage for Faculty/Admin/SuperAdmin users. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialRepository.ApplyTenantCampusScope | Enforces strict tenant/campus query isolation with SuperAdmin bypass behavior. | src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs |
| CourseMaterialRepository.IsScopeMissingForNonSuperAdmin | Short-circuits read operations early when tenant scope is missing for non-SuperAdmin callers. | src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs |
| DashboardCompositionController.GetContext | Provides role- and policy-aware module/vocabulary/widget composition used as evidence for menu/dashboard correctness by mode and role. | src/Tabsan.EduSphere.API/Controllers/DashboardCompositionController.cs |
| DashboardCompositionController.GetContext(...) | Aggregates visible modules, academic vocabulary, and widgets into one dashboard-context response for the portal ModuleComposition screen. | src/Tabsan.EduSphere.API/Controllers/DashboardCompositionController.cs |
| DashboardCompositionService.GetWidgets(...) | Adds short-TTL in-memory cache for dashboard widget composition keyed by role and institution policy state to reduce repeated composition cost. | src/Tabsan.EduSphere.Application/Services/DashboardCompositionService.cs |
| DatabaseConnectionResolver.ResolveDefaultConnection | Resolves DB connection string from prioritized environment/deployment keys with backward-compatible fallback to legacy connection-string key. | src/Tabsan.EduSphere.Application/Services/DatabaseConnectionResolver.cs |
| DatabaseSeeder.SeedRolesAsync | Seeds `Finance` role additively alongside existing system roles during startup bootstrap. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| DatabaseSeeder.SeedSidebarMenusAsync(...) | Makes sidebar role seeding self-healing by updating existing role-access values, ensuring corrected visibility rules are enforced on already-seeded databases. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| Department.SetTenantCampus(tenantId, campusId) | Assigns or clears tenant/campus ownership for department-level scoping while preserving InstitutionType behavior. | src/Tabsan.EduSphere.Domain/Academic/Department.cs |
| DepartmentRepository.ApplyTenantCampusScope | Applies tenant/campus query filtering for department reads with SuperAdmin bypass. | src/Tabsan.EduSphere.Infrastructure/Repositories/DepartmentRepository.cs |
| DeploymentTopologyResolver.Resolve | Resolves effective deployment mode, customer identity, domain, database name, and scaling settings from config and environment variables. | src/Tabsan.EduSphere.Application/Services/DeploymentTopologyResolver.cs |
| EduApiClient.BuildAnalyticsQuery | Builds analytics query string including optional `courseId` and `semesterId` filters. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetDashboardCompositionContextAsync(...) | Deserializes the aggregated dashboard-context endpoint response into a single Web client model. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetPaymentStatusAnalyticsAsync | Retrieves payment status analytics data for portal snapshots. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetPaymentSummaryReportAsync | Retrieves payment summary report data for the portal. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.MapPayment | Consumes payment payload with compatibility fallback (`PaidDate ?? ConfirmedAt`) and update trail mapping. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.UpdatePaymentAsync | Web client method that calls the finance payment edit endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EnsureDefaultTenantCampusAsync | Guarantees default tenant (`DEFAULT`) and default campus (`MAIN`) are present and active at startup. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| EnsureTenantCampusBackfillAsync | Performs startup safety backfill for users/departments missing tenant/campus assignments. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| GetAnalyticsAccessScope | Resolves effective analytics access scope from caller tenant/campus claims with superadmin bypass handling. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetAssignmentStatsAsync | Computes assignment statistics from grouped submission/enrollment snapshots instead of per-assignment query loops. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetComparativeSummaryAsync | Computes comparative summary metrics via department-grouped batched queries. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetCurrentTenantId | Resolves caller tenant/campus claim scope used for export-job ownership checks. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| GetPerformanceReportAsync | Builds performance report using batched results/submissions aggregation instead of per-student round-trips. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetQuizStatsAsync | Aggregates quiz attempt statistics in a grouped pass rather than per-quiz queries. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GradebookService.GetGradebookAsync(...) | Removes sync-over-async `.Result` consumption from the gradebook request path by awaiting completed tasks. | src/Tabsan.EduSphere.Application/Assignments/GradebookService.cs |
| HttpTenantScopeResolver.GetTenantScopeKey() | Resolves tenant scope from JWT claims or `X-Tenant-Code` request header for API-request-scoped operations. | src/Tabsan.EduSphere.API/Services/HttpTenantScopeResolver.cs |
| IAnalyticsService.GetPaymentStatusReportAsync(..., courseId, semesterId) | Exposes filter-aware payment analytics contract including course/semester dimensions. | src/Tabsan.EduSphere.Application/Interfaces/IAnalyticsService.cs |
| IEduApiClient.DownloadCourseMaterialFileAsync | Downloads Course Material files from API for portal-proxied file delivery. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GetCourseMaterialsAsync | Fetches scoped course materials for portal pages with optional active-only filtering. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GetDashboardCompositionContextAsync(...) | Fetches the aggregated dashboard-context payload in a single API request for the web portal. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.UploadCourseMaterialFileAsync | Uploads Course Material files from portal to API multipart endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEnrollmentRepository.GetWaitlistedByOfferingAsync | Returns waitlisted enrollments in queue order so promotion can be deterministic. | src/Tabsan.EduSphere.Domain/Interfaces/IEnrollmentRepository.cs |
| INotificationRepository.GetForUserAsync(..., asNoTracking, ...) | Adds opt-in no-tracking control for read-heavy inbox retrieval while preserving tracked reads for mark-all-read operations. | src/Tabsan.EduSphere.Domain/Interfaces/INotificationRepository.cs |
| InstitutionPolicyController.Save | Used by SuperAdmin matrix run to switch School/College/University modes and validate privileged access continuity. | src/Tabsan.EduSphere.API/Controllers/InstitutionPolicyController.cs |
| InstitutionPolicyService.SavePolicyAsync | Persists institution policy flags to `portal_settings` by committing settings repository changes, enabling mode switches from uploaded licenses to be retained. | src/Tabsan.EduSphere.Application/Services/InstitutionPolicyService.cs |
| IReportRepository.GetPaymentSummaryDataAsync | Queries filtered payment receipt reporting rows with tenant/campus-safe scope and academic dimensions. | src/Tabsan.EduSphere.Domain/Interfaces/IReportRepository.cs |
| IStudentLifecycleService.UpdatePaymentReceiptAsync | Exposes finance receipt edit capability through the application contract. | src/Tabsan.EduSphere.Application/Interfaces/IStudentLifecycleService.cs |
| ITenantScopeResolver.GetTenantScopeKey() | Provides a tenant-scope abstraction for application-layer tenant-aware key resolution. | src/Tabsan.EduSphere.Application/Interfaces/ITenantScopeResolver.cs |
| LibraryService.BuildLoanLookupCacheKey(...) | Keys external-call cache entries by student identifier and integration configuration fingerprint to avoid stale cross-config reuse. | src/Tabsan.EduSphere.Application/Services/LibraryService.cs |
| LibraryService.GetLoansAsync(...) | Adds short-TTL distributed cache for safe external loan lookup reads to reduce repeated dependency calls. | src/Tabsan.EduSphere.Application/Services/LibraryService.cs |
| NotificationRepository.GetActiveUserPhoneNumbersAsync | Resolves distinct active recipient phone numbers by user IDs for SMS dispatch. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationRepository.GetForUserAsync(..., asNoTracking, ...) | Applies optional AsNoTracking to notification recipient + parent notification read path. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationRepository.GetUnreadCountAsync(...) | Removes unnecessary Include from unread count query to avoid extra query shaping overhead. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationService.BumpCacheVersion() | Invalidates notification read-cache windows by incrementing cache version after notification mutations. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| NotificationService.GetBadgeAsync(...) | Adds short-TTL in-memory cache for unread badge counts with cache-version keying. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| NotificationService.GetInboxAsync(...) | Adds short-TTL in-memory cache for paged inbox reads with cache-version keying for mutation-safe freshness. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| PaymentReceipt.UpdateDetails | Updates actionable receipt fields while preserving audit trail and blocking edits on Paid/Cancelled receipts. | src/Tabsan.EduSphere.Domain/StudentLifecycle/PaymentReceipt.cs |
| PaymentReceiptController.Delete | Explicitly rejects receipt deletion requests with `405` to preserve immutable payment history. | src/Tabsan.EduSphere.API/Controllers/PaymentReceiptController.cs |
| PaymentReceiptController.Update | API endpoint for finance/admin users to edit actionable payment receipts. | src/Tabsan.EduSphere.API/Controllers/PaymentReceiptController.cs |
| PortalController.BuildAnalyticsPageModelAsync | Loads payment status analytics into analytics page/snapshot model and summary cards. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CreateCourseMaterial | Creates a course material from the portal manage page while preserving current filter state. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadCourseMaterialFile | Proxies material file download from API and preserves student/manage redirect context on failure. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.SetCourseMaterialActive | Toggles material active state from the portal manage page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdateCourseMaterial | Updates course material metadata/location from the portal manage page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdatePayment | Web action that posts finance payment edits from the payments page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UserImportTemplate(fileName) | Serves approved CSV template files from `User Import Sheets/` via filename allow-list with traversal-safe path resolution. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QuizRepository.GetAllAttemptsForStudentAsync(...) | Returns student quiz attempts with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| QuizRepository.GetAttemptsAsync(...) | Returns quiz attempts with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| QuizRepository.GetByOfferingAsync(...) | Returns quiz lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| refreshSnapshot | Fetches analytics snapshot and updates filters/cards/charts without full page reload. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderCourseTrend | Renders course trend line based on average assignment marks. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderDepartmentCounts | Renders department-wise student counts using a bar chart. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderPaymentStatus | Renders the Paid vs Unpaid interactive pie chart on the analytics page. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderSemesterTrend | Renders combined semester trend lines for marks and attendance where available. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderStudentDistribution | Renders semester-based student distribution using a pie chart. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| ReportController.ExportAssignmentSummary | Exports assignment summary report as Excel for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAssignmentSummaryCsv | Exports assignment summary report as CSV for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAssignmentSummaryPdf | Exports assignment summary report as PDF for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummary | Exports attendance summary report as Excel for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummaryCsv | Exports attendance summary report as CSV for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummaryPdf | Exports attendance summary report as PDF for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportGpaReport | Exports GPA report as Excel for selected scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummary | Exports payment summary report as Excel for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummaryCsv | Exports payment summary report as CSV for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummaryPdf | Exports payment summary report as PDF for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetAssignmentSummary | Returns assignment summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetAttendanceSummary | Returns attendance summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetCatalog | Returns report catalog entries available to the caller role. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetEnrollmentSummary | Returns enrollment summary report data with institution-aware filtering. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetFypStatusReport | Returns FYP status report data for scoped department and institution filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetGpaReport | Returns GPA report data for scoped program and department filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetInstitutionReportSections | Returns institution-aware report section visibility metadata. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetLowAttendanceWarning | Returns low-attendance warning report rows under scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetPaymentSummary | Exposes finance/admin/superadmin payment summary report endpoint with optional filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetQuizSummary | Returns quiz summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetResultSummary | Returns result summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetSemesterResults | Returns semester results report data for scoped semester filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetStudentTranscript | Returns transcript report data for a specific student profile. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportRepository.GetPaymentSummaryDataAsync | Materializes filtered payment summary rows (tenant/campus/course/semester/level aware) for finance reporting. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportRepository.cs |
| ReportService.ExportAssignmentSummaryCsvAsync | Builds CSV export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAssignmentSummaryExcelAsync | Builds Excel export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAssignmentSummaryPdfAsync | Builds PDF export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryCsvAsync | Builds CSV export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryExcelAsync | Builds Excel export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryPdfAsync | Builds PDF export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportGpaReportExcelAsync | Builds Excel export payload for GPA report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryCsvAsync | Builds CSV export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryExcelAsync | Builds Excel export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryPdfAsync | Builds PDF export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryCsvAsync | Builds CSV export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryExcelAsync | Builds Excel export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryPdfAsync | Builds PDF export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryCsvAsync | Builds CSV export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryExcelAsync | Builds Excel export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryPdfAsync | Builds PDF export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportTranscriptExcelAsync | Builds Excel export payload for student transcript report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetAssignmentSummaryAsync | Aggregates assignment report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetAttendanceSummaryAsync | Aggregates attendance report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetCatalogAsync | Builds role-filtered report catalog response for API consumers. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetEnrollmentSummaryAsync | Aggregates enrollment report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetFypStatusReportAsync | Aggregates FYP status report rows into response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetGpaReportAsync | Aggregates GPA report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetLowAttendanceWarningAsync | Aggregates low-attendance report rows into response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetPaymentSummaryAsync | Aggregates payment report rows into finance-ready totals and report response shape. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetQuizSummaryAsync | Aggregates quiz report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetResultSummaryAsync | Aggregates result report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetSemesterResultsAsync | Aggregates semester result rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetStudentTranscriptAsync | Builds transcript response payload for a target student profile. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| SettingsRepository.GetAllModuleRolesAsync(...) | Returns all module-role assignments with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetAllReportsAsync(...) | Returns report definitions with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetModuleRolesAsync(...) | Returns module-role assignments with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetSubMenusAsync(...) | Uses AsNoTracking for read-only submenu retrieval. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetTopLevelMenusAsync(...) | Uses AsNoTracking + AsSplitQuery for include-heavy top-level sidebar graph retrieval. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetVisibleMenusForRoleAsync(...) | Uses AsNoTracking + AsSplitQuery for include-heavy role-scoped sidebar visibility reads. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SidebarMenuService.GetTopLevelMenusAsync(...) | Adds short-TTL in-memory cache for top-level sidebar menu reads with versioned invalidation support. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| SidebarMenuService.GetVisibleForRoleAsync(...) | Adds short-TTL in-memory cache for role-scoped visible sidebar reads with versioned invalidation support. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| SidebarMenuService.InvalidateSidebarCache() | Bumps sidebar cache version after role/status mutations to force fresh reads on next request. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| StudyPlanService.AddCourseAsync | Adds a course to a study plan with validation for duplicates and plan state. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.AdvisePlanAsync | Applies advisor endorsement decision and notes for a study plan. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.CreatePlanAsync | Creates a new study plan for a student profile and planned semester. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.DeletePlanAsync | Deletes a study plan owned by the target student profile. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlanAsync | Returns a single study plan with associated planned courses. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlansAsync | Returns all study plans for a specific student profile. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlansByDepartmentAsync | Returns department-scoped study plans for advisor/admin workflows. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetRecommendationsAsync | Generates recommendation payload for missing requirements and sequencing guidance. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.RemoveCourseAsync | Removes a course from a study plan while preserving remaining entries. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StartupVisibilityReporter.DescribeDatabaseType | Classifies the resolved connection string into a safe database-type label for logging. | src/Tabsan.EduSphere.Application/Services/StartupVisibilityReporter.cs |
| StudentLifecycleRepository.ApplyPaymentAccessScope | Applies tenant/campus scope filtering to payment receipt lifecycle queries. | src/Tabsan.EduSphere.Infrastructure/Repositories/StudentLifecycleRepository.cs |
| StudentLifecycleRepository.ApplyStudentAccessScope | Applies tenant/campus scope filtering to student profile lifecycle queries. | src/Tabsan.EduSphere.Infrastructure/Repositories/StudentLifecycleRepository.cs |
| StudentLifecycleService.MapPaymentReceipt | Maps receipt state to output contract including `PaidDate` and `UpdatedAt` tracking fields. | src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs |
| StudentLifecycleService.UpdatePaymentReceiptAsync | Applies finance receipt edits, persists them, and notifies the student of the update. | src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs |
| TenantIsolationResolver.Resolve | Resolves tenant isolation mode, tenant code/name/domain/database, and tenant config path from config and environment variables. | src/Tabsan.EduSphere.Application/Services/TenantIsolationResolver.cs |
| TenantOperationsService.ReadSetting(all, rawKey, defaultValue) | Reads tenant-scoped values first and falls back to legacy unscoped keys for migration-safe compatibility. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TenantOperationsService.ScopedCacheKey(key) | Builds tenant-scoped distributed-cache keys to prevent cross-tenant cache collisions. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TenantOperationsService.ScopedSettingKey(key) | Builds tenant-scoped settings keys for onboarding/subscription/profile operations with default-tenant backward compatibility. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TimetableRepository.GetByDepartmentAsync(...) | Returns timetable lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetEntriesByCourseOfferingAsync(...) | Returns course-offering timetable entries with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetPublishedByDepartmentAsync(...) | Returns published timetable lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetTeacherEntriesAsync(...) | Returns teacher timetable entries with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| UserImportController.ImportCsv | Accepts CSV uploads for bulk user import with optional strict mode and role-based access enforcement. | src/Tabsan.EduSphere.API/Controllers/UserImportController.cs |
| UserImportService.ImportFromCsvAsync | Parses CSV imports with role/identity validation, strict-mode rollback support, and additive mobile/campus column compatibility. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.IsValidMobileNumber | Validates optional mobile/phone values to accepted character set before user import persistence. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.ResolveCampusAssignmentsIndex | Resolves optional campus-assignment header aliases (`CampusAssignments`/`CampusIds`) for backward-compatible CSV parsing. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.ResolvePhoneNumberIndex | Resolves optional mobile/phone header aliases (`MobileNumber`/`PhoneNumber`) for backward-compatible CSV parsing. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.TryValidateCampusAssignments | Validates optional campus assignments as pipe-separated GUID values during CSV import. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| User.SetTenantCampus(tenantId, campusId) | Assigns or clears tenant/campus ownership for user-level scoping without breaking existing identity flows. | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.ValidateTenantCampusPair | Prevents invalid user state by requiring TenantId and CampusId to be set/cleared together. | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| UserRepository.ApplyTenantCampusScope | Applies tenant/campus query filtering for user reads with SuperAdmin bypass. | src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs |
| DegreeController.Download | Downloads generated degree document and applies `.docx` fallback when requested PDF is unavailable. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.DownloadDefaultTemplate | Streams default degree `.docx` template from Plan K export service. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.Generate | Triggers Plan K degree document generation workflow for admin users. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.StudentDegree | Returns Plan K generated degree artifacts for the current student route `/student/degree`. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.ResolveCurrentUserId | Resolves current caller user-id from NameIdentifier/sub claims for student artifact filtering. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| TranscriptController.Download | Downloads generated transcript document and applies `.docx` fallback when requested PDF is unavailable. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.DownloadDefaultTemplate | Streams default transcript `.docx` template from Plan K export service. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.Generate | Triggers Plan K transcript document generation workflow for admin users. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.StudentTranscript | Returns Plan K generated transcript artifacts for the current student route `/student/transcript`. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.ResolveCurrentUserId | Resolves current caller user-id from NameIdentifier/sub claims for student artifact filtering. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TemplateExportService.GetDegreeTemplateAsync | Generates default Degree Word template bytes with Plan K placeholder contract. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService.GetTranscriptTemplateAsync | Generates default Transcript Word template bytes with Plan K placeholder contract including `{{COURSE_TABLE}}`. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService.BuildTemplateDocument | Constructs in-memory `.docx` payload from template text lines using OpenXML. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| QRCodeService.GeneratePng | Produces QR PNG byte array from verification payload using QRCoder. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/QRCodeService.cs |
| QRCodeService.GenerateDataUrl | Produces Base64 QR data URL for lightweight UI rendering support. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/QRCodeService.cs |
| TemplateProcessorService.PopulateTemplate | Applies Plan K placeholder replacement and transcript table rendering to `.docx` template bytes. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceInMainBody | Replaces mapped placeholder tokens in main document body text nodes. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceInHeadersAndFooters | Replaces mapped placeholder tokens in header and footer parts. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceCourseTable | Replaces `{{COURSE_TABLE}}` marker with generated OpenXML table or empty-data text. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildCourseTable | Builds transcript course table with required Plan K columns. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildRow | Creates table row instances for course-table header and data rows. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildCell | Creates formatted OpenXML table cell content (with bold header support). | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| DocumentGenerationService.GenerateDegreeAsync | Orchestrates Plan K degree generation using export, processing, and QR services. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GenerateTranscriptAsync | Orchestrates Plan K transcript generation including course-table population. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GetAsync | Returns generated Plan K document metadata by document id. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.ListByStudentAsync | Lists Plan K generated artifacts for a specific student id. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GenerateInternalAsync | Performs shared generation pipeline, invokes optional PDF adapter, persists outputs, and registers metadata. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DegreeGenerationRequest.ToPayload | Maps degree generation request data to Plan K template payload shape with default serial/date assignment. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| TranscriptGenerationRequest.ToPayload | Maps transcript generation request data to Plan K template payload shape with default serial/date assignment. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| IPdfConverterAdapter.TryConvertToPdfAsync | Defines optional PDF conversion adapter contract for Plan K generated documents. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| NoOpPdfConverterAdapter.TryConvertToPdfAsync | Default no-op adapter that returns null to preserve guaranteed `.docx` fallback behavior. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| CertificateGenerationController.GetGraduatedStudents | Returns university-only graduated students filtered by tenant, campus, department, and course with role scope enforcement. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GenerateDegreeCertificate | Generates degree certificate document for a scoped graduated student (Admin/SuperAdmin only). | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GenerateTranscriptCertificate | Generates transcript document for a scoped graduated student (Admin/SuperAdmin only). | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetStudentDocuments | Returns generated document history for a scoped student with role-aware read access checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.Download | Downloads generated certificate/transcript artifact with pdf/docx selection and scope validation. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetStudentInScopeAsync | Enforces university/license/tenant/campus/department scope before management operations. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.HasStudentReadScopeAsync | Enforces scoped read permissions for Admin, Faculty, and Student document access. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.ResolveStudentNameAsync | Resolves student display name from user identity with registration fallback. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.BuildTranscriptRowsAsync | Builds transcript course rows from enrollments/course offerings for transcript generation payloads. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| PortalController.GenerateCertificates | Renders Tenant/Campus/Department/Course-filtered Generate Certificates page with role-based manage mode. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.GenerateDegreeCertificate | Web action that triggers scoped degree generation and preserves current filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.GenerateTranscriptCertificate | Web action that triggers scoped transcript generation and preserves current filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadGeneratedCertificateDocument | Proxies generated document download/print for scoped portal users. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| IEduApiClient.GetGraduatedCertificateStudentsAsync | Client contract for graduated-student certificate listing with tenant/campus/department/course filters. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GenerateDegreeCertificateAsync | Client contract for scoped degree certificate generation. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GenerateTranscriptCertificateAsync | Client contract for scoped transcript generation. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DownloadGeneratedCertificateDocumentAsync | Client contract for generated document download/print retrieval. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetGraduatedCertificateStudentsAsync | Calls graduated-student listing endpoint with dynamic filter query composition. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GenerateDegreeCertificateAsync | Calls API endpoint to generate degree certificate for selected graduated student. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GenerateTranscriptCertificateAsync | Calls API endpoint to generate transcript for selected graduated student. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.DownloadGeneratedCertificateDocumentAsync | Downloads generated certificate/transcript files by document id and requested format. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |

