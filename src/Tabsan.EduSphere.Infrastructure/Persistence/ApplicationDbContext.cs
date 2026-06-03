using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.AiChat;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Fyp;
using Tabsan.EduSphere.Domain.Helpdesk;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Licensing;
using Tabsan.EduSphere.Domain.Modules;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Domain.Settings;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.StudentLifecycle;
using Tabsan.EduSphere.Domain.StudyPlanner;
using Tabsan.EduSphere.Domain.Tenancy;

namespace Tabsan.EduSphere.Infrastructure.Persistence;

/// <summary>
/// Central EF Core DbContext for the University Portal application database.
/// All entity configurations are loaded from the Configurations sub-folder
/// using the fluent API (IEntityTypeConfiguration) rather than data annotations
/// to keep domain entities free of infrastructure concerns.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ── Identity ───────────────────────────────────────────────────────────
    /// <summary>All system users (students, faculty, admins, super admins).</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Top-level tenant organizations.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Tenant campuses used for data scoping within a tenant.</summary>
    public DbSet<Campus> Campuses => Set<Campus>();

    /// <summary>Predefined system roles seeded at startup.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Active refresh-token sessions per user.</summary>
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // ── Academic Core ──────────────────────────────────────────────────────
    /// <summary>University departments — the root organisational unit.</summary>
    public DbSet<Department> Departments => Set<Department>();

    // ── Licensing ──────────────────────────────────────────────────────────
    /// <summary>Single-row table holding the current validated license state.</summary>
    public DbSet<LicenseState> LicenseStates => Set<LicenseState>();

    /// <summary>Records VerificationKey hashes that have been consumed to prevent replay attacks.</summary>
    public DbSet<ConsumedVerificationKey> ConsumedVerificationKeys => Set<ConsumedVerificationKey>();

    // ── Modules ────────────────────────────────────────────────────────────
    /// <summary>Module definitions seeded at startup.</summary>
    public DbSet<Module> Modules => Set<Module>();

    /// <summary>Per-module activation state managed by Super Admin.</summary>
    public DbSet<ModuleStatus> ModuleStatuses => Set<ModuleStatus>();

    // ── Audit ──────────────────────────────────────────────────────────────
    /// <summary>Append-only audit log for privileged actions.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ── Phase 2: Academic Core ─────────────────────────────────────────────
    /// <summary>Degree programmes offered by departments.</summary>
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();

    /// <summary>Academic terms (semesters). Locked once closed.</summary>
    public DbSet<Semester> Semesters => Set<Semester>();

    /// <summary>Course catalogue definitions.</summary>
    public DbSet<Course> Courses => Set<Course>();

    /// <summary>Course offerings — a course scheduled for a specific semester and faculty.</summary>
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();

    // Final-Touches Phase 15 Stage 15.1 — CoursePrerequisite: prerequisite enrollment rules
    /// <summary>Prerequisite links — courses that must be passed before another course can be enrolled in.</summary>
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();

    /// <summary>Extended academic profile for students.</summary>
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    /// <summary>Student enrollment records — permanent academic history, never deleted.</summary>
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    /// <summary>Pre-approved registration entries for student self-registration.</summary>
    public DbSet<RegistrationWhitelist> RegistrationWhitelist => Set<RegistrationWhitelist>();

    /// <summary>Faculty ↔ department access assignments.</summary>
    public DbSet<FacultyDepartmentAssignment> FacultyDepartmentAssignments => Set<FacultyDepartmentAssignment>();

    /// <summary>Admin ↔ department access assignments.</summary>
    public DbSet<AdminDepartmentAssignment> AdminDepartmentAssignments => Set<AdminDepartmentAssignment>();

    // ── Phase 3: Assignments and Results ───────────────────────────────────────
    /// <summary>Faculty-created assignments for course offerings.</summary>
    public DbSet<Assignment> Assignments => Set<Assignment>();

    /// <summary>Student submissions for assignments — permanent academic evidence.</summary>
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();

    /// <summary>Published and draft result marks per student per course offering.</summary>
    public DbSet<Result> Results => Set<Result>();

    /// <summary>Configured GPA score thresholds used to derive grade points.</summary>
    public DbSet<GpaScaleRule> GpaScaleRules => Set<GpaScaleRule>();

    /// <summary>Configured assessment components and their score weightages.</summary>
    public DbSet<ResultComponentRule> ResultComponentRules => Set<ResultComponentRule>();

    /// <summary>Audit log of every transcript export request.</summary>
    public DbSet<TranscriptExportLog> TranscriptExportLogs => Set<TranscriptExportLog>();

    // ── Phase 2: Safe Data Storage Strategy ──────────────────────────────────
    /// <summary>Template metadata for degree/transcript document generation.</summary>
    public DbSet<AcademicDocumentTemplate> AcademicDocumentTemplates => Set<AcademicDocumentTemplate>();
    /// <summary>Persisted degree certificate generation records.</summary>
    public DbSet<DegreeDocumentRecord> DegreeDocuments => Set<DegreeDocumentRecord>();
    /// <summary>Persisted transcript generation records.</summary>
    public DbSet<TranscriptDocumentRecord> TranscriptDocuments => Set<TranscriptDocumentRecord>();

    // ── Phase 4: Notifications and Attendance ──────────────────────────────────
    /// <summary>Notification headers created by users or the system.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Per-user delivery and read-state records for notifications.</summary>
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();

    /// <summary>Daily per-student session attendance records.</summary>
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    // ── Phase 5: Quizzes ───────────────────────────────────────────────────────
    /// <summary>Quiz headers authored by faculty for course offerings.</summary>
    public DbSet<Quiz> Quizzes => Set<Quiz>();

    /// <summary>Questions belonging to quizzes.</summary>
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    /// <summary>Answer options for MCQ and TrueFalse questions.</summary>
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();

    /// <summary>Student attempt records for quizzes.</summary>
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    /// <summary>Per-question answers submitted by students within an attempt.</summary>
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();

    // ── Phase 5: FYP ──────────────────────────────────────────────────────────
    /// <summary>Final Year Projects proposed and managed by students.</summary>
    public DbSet<FypProject> FypProjects => Set<FypProject>();

    /// <summary>Faculty panel members (supervisors, co-supervisors, examiners) per project.</summary>
    public DbSet<FypPanelMember> FypPanelMembers => Set<FypPanelMember>();

    /// <summary>Supervision and review meetings scheduled for FYP projects.</summary>
    public DbSet<FypMeeting> FypMeetings => Set<FypMeeting>();

    // ── Phase 6: AI Chat ──────────────────────────────────────────────────────────
    /// <summary>AI chat conversations initiated by users.</summary>
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();

    /// <summary>Individual message turns (user and assistant) within a chat conversation.</summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // ── Phase 8: Student Lifecycle ─────────────────────────────────────────────────
    /// <summary>Requests from students/teachers to modify their profile information.</summary>
    public DbSet<AdminChangeRequest> AdminChangeRequests => Set<AdminChangeRequest>();

    /// <summary>Requests from teachers to modify attendance or result records.</summary>
    public DbSet<TeacherModificationRequest> TeacherModificationRequests => Set<TeacherModificationRequest>();

    /// <summary>Payment receipts issued to students for fee collection.</summary>
    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();

    // ── Phase 9: Timetable, Report Settings, Module Roles ─────────────────────────
    /// <summary>Department/semester timetables created by admins.</summary>
    public DbSet<Timetable> Timetables => Set<Timetable>();

    /// <summary>Individual scheduled slots within a timetable.</summary>
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    /// <summary>Physical campus buildings — managed by Admin/SuperAdmin, used in timetable dropdowns.</summary>
    public DbSet<Building> Buildings => Set<Building>();

    /// <summary>Rooms within buildings — managed by Admin/SuperAdmin, used in timetable dropdowns.</summary>
    public DbSet<Room> Rooms => Set<Room>();

    /// <summary>Report definitions configurable by Super Admin (name, purpose, active flag).</summary>
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();

    /// <summary>Per-role visibility assignments for each report definition.</summary>
    public DbSet<ReportRoleAssignment> ReportRoleAssignments => Set<ReportRoleAssignment>();

    /// <summary>Per-role access assignments for each module (beyond mandatory/licensed).</summary>
    public DbSet<ModuleRoleAssignment> ModuleRoleAssignments => Set<ModuleRoleAssignment>();

    /// <summary>Sidebar navigation menu items (top-level and sub-menus).</summary>
    public DbSet<SidebarMenuItem> SidebarMenuItems => Set<SidebarMenuItem>();

    /// <summary>Per-role visibility assignments for each sidebar menu item.</summary>
    public DbSet<SidebarMenuRoleAccess> SidebarMenuRoleAccesses => Set<SidebarMenuRoleAccess>();

    /// <summary>Key-value pairs for institution-wide portal branding (university name, brand initials, etc.).</summary>
    public DbSet<PortalSetting> PortalSettings => Set<PortalSetting>();

    // ── Phase 10: Security & Email Infrastructure ──────────────────────────────
    /// <summary>Append-only log of previous password hashes for reuse-prevention policy.</summary>
    public DbSet<PasswordHistoryEntry> PasswordHistory => Set<PasswordHistoryEntry>();

    /// <summary>Append-only log of all outbound email send attempts and their outcome.</summary>
    public DbSet<OutboundEmailLog> OutboundEmailLogs => Set<OutboundEmailLog>();

    // ── Phase 12: Academic Calendar ────────────────────────────────────────────
    /// <summary>Named academic deadlines attached to semesters (census dates, exam periods, etc.).</summary>
    public DbSet<AcademicDeadline> AcademicDeadlines => Set<AcademicDeadline>();

    // ── Phase 14: Helpdesk / Support Ticketing ─────────────────────────────────
    /// <summary>Support tickets submitted by Students and Faculty.</summary>
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    /// <summary>Thread messages on support tickets.</summary>
    public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();

    // ── Phase 16: Faculty Grading — Rubric-Based Grading ──────────────────────
    /// <summary>Rubrics attached to assignments for structured grading.</summary>
    public DbSet<Rubric> Rubrics => Set<Rubric>();
    /// <summary>Criteria rows within a rubric (e.g. Clarity, Depth).</summary>
    public DbSet<RubricCriterion> RubricCriteria => Set<RubricCriterion>();
    /// <summary>Performance levels for a rubric criterion (e.g. Excellent / Good / Poor).</summary>
    public DbSet<RubricLevel> RubricLevels => Set<RubricLevel>();
    /// <summary>Records which level was selected for each criterion per student submission.</summary>
    public DbSet<RubricStudentGrade> RubricStudentGrades => Set<RubricStudentGrade>();

    // ── Phase 17: Degree Audit System ─────────────────────────────────────────
    /// <summary>Graduation requirement rules defined per academic program.</summary>
    public DbSet<DegreeRule> DegreeRules => Set<DegreeRule>();
    /// <summary>Individual required courses within a degree rule.</summary>
    public DbSet<DegreeRuleRequiredCourse> DegreeRuleRequiredCourses => Set<DegreeRuleRequiredCourse>();

    // ── Phase 18: Graduation Workflow ──────────────────────────────────────────
    /// <summary>Student graduation applications subject to multi-stage approval.</summary>
    public DbSet<GraduationApplication> GraduationApplications => Set<GraduationApplication>();
    /// <summary>Individual approval records per stage within a graduation application.</summary>
    public DbSet<GraduationApplicationApproval> GraduationApplicationApprovals => Set<GraduationApplicationApproval>();

    // ── Phase 19: Advanced Course Creation & Grading Config ────────────────────
    /// <summary>Per-course grading configurations (pass threshold, grade ranges).</summary>
    public DbSet<CourseGradingConfig> CourseGradingConfigs => Set<CourseGradingConfig>();

    // ── Phase 20: Learning Management System (LMS) ─────────────────────────────
    /// <summary>Week-based course content modules for a course offering.</summary>
    public DbSet<CourseContentModule> CourseContentModules => Set<CourseContentModule>();
    /// <summary>Video attachments for a course content module.</summary>
    public DbSet<ContentVideo> ContentVideos => Set<ContentVideo>();
    /// <summary>Forum discussion threads per course offering.</summary>
    public DbSet<DiscussionThread> DiscussionThreads => Set<DiscussionThread>();
    /// <summary>Replies within a discussion thread.</summary>
    public DbSet<DiscussionReply> DiscussionReplies => Set<DiscussionReply>();
    /// <summary>Announcements for a course offering or department-wide.</summary>
    public DbSet<CourseAnnouncement> CourseAnnouncements => Set<CourseAnnouncement>();
    /// <summary>Tenant/campus scoped course materials (file/link) for department/program/semester/subject-course slices.</summary>
    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();

    // Final-Touches Phase 21 Stage 21.1 — Study Planner
    /// <summary>Student semester study plans.</summary>
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    /// <summary>Course line items within a student study plan.</summary>
    public DbSet<StudyPlanCourse> StudyPlanCourses => Set<StudyPlanCourse>();

    // ── Phase 22: External Integrations ───────────────────────────────────────
    /// <summary>Accreditation / government report templates defined by SuperAdmin.</summary>
    public DbSet<AccreditationTemplate> AccreditationTemplates => Set<AccreditationTemplate>();

    // ── Phase 25: Academic Engine Unification ──────────────────────────────────
    /// <summary>Institution-level grading profiles (pass threshold + grade bands) per institution type.</summary>
    public DbSet<InstitutionGradingProfile> InstitutionGradingProfiles => Set<InstitutionGradingProfile>();

    // ── Phase 26: School and College Functional Expansion ─────────────────────
    /// <summary>School stream master records (Science/Commerce/Arts etc.).</summary>
    public DbSet<SchoolStream> SchoolStreams => Set<SchoolStream>();
    /// <summary>Links students to streams for school-mode subject constraints.</summary>
    public DbSet<StudentStreamAssignment> StudentStreamAssignments => Set<StudentStreamAssignment>();
    /// <summary>Generated report-card snapshots per student and period.</summary>
    public DbSet<StudentReportCard> StudentReportCards => Set<StudentReportCard>();
    /// <summary>Bulk promotion batch headers.</summary>
    public DbSet<BulkPromotionBatch> BulkPromotionBatches => Set<BulkPromotionBatch>();
    /// <summary>Per-student bulk promotion batch entries.</summary>
    public DbSet<BulkPromotionEntry> BulkPromotionEntries => Set<BulkPromotionEntry>();
    /// <summary>Parent to student read-only access links.</summary>
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();

    /// <summary>
    /// Scans the current assembly for all IEntityTypeConfiguration implementations
    /// and applies them automatically. This keeps OnModelCreating clean as the
    /// schema grows across phases.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Discover and apply all entity configurations in this assembly automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    /// <summary>
    /// Intercepts SaveChanges to automatically update the UpdatedAt timestamp
    /// on any BaseEntity that has been modified.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceImmutableAuditLogs();
        SetAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void EnforceImmutableAuditLogs()
    {
        foreach (var entry in ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit logs are immutable and cannot be updated or deleted.");
            }
        }
    }

    /// <summary>
    /// Iterates all tracked entries that implement BaseEntity and updates
    /// their UpdatedAt timestamp before writing to the database.
    /// </summary>
    private void SetAuditTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                entry.Entity.Touch();
        }
    }
}
