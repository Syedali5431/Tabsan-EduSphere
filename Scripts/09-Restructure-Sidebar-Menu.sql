/*
  Sidebar Menu Structure — Tabsan EduSphere v1.0
  
  Schema: sidebar_menu_items (Name, Purpose, Key, ParentId, DisplayOrder, IsActive, IsSystemMenu)
          sidebar_menu_role_accesses (SidebarMenuItemId, RoleName, IsAllowed)
*/

SET NOCOUNT ON;
GO
USE [Tabsan-EduSphere];
GO
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

DELETE FROM [sidebar_menu_role_accesses];
DELETE FROM [sidebar_menu_items];

-- ═══════ ALL MENU ITEMS ═══════
DECLARE @mDashboard     UNIQUEIDENTIFIER = NEWID();
DECLARE @mTenants       UNIQUEIDENTIFIER = NEWID();
DECLARE @mCampuses      UNIQUEIDENTIFIER = NEWID();
DECLARE @mDepts         UNIQUEIDENTIFIER = NEWID();
DECLARE @mPrograms      UNIQUEIDENTIFIER = NEWID();
DECLARE @mCourses       UNIQUEIDENTIFIER = NEWID();
DECLARE @mEnroll        UNIQUEIDENTIFIER = NEWID();
DECLARE @mStudents      UNIQUEIDENTIFIER = NEWID();
DECLARE @mTimetable     UNIQUEIDENTIFIER = NEWID();
DECLARE @mTeacherTT     UNIQUEIDENTIFIER = NEWID();
DECLARE @mAssign        UNIQUEIDENTIFIER = NEWID();
DECLARE @mEnterAtt      UNIQUEIDENTIFIER = NEWID();
DECLARE @mEnterRes      UNIQUEIDENTIFIER = NEWID();
DECLARE @mGradebook     UNIQUEIDENTIFIER = NEWID();
DECLARE @mRubric        UNIQUEIDENTIFIER = NEWID();
DECLARE @mLookups       UNIQUEIDENTIFIER = NEWID();
DECLARE @mPayments      UNIQUEIDENTIFIER = NEWID();
DECLARE @mReportCenter  UNIQUEIDENTIFIER = NEWID();
DECLARE @mHelpdesk      UNIQUEIDENTIFIER = NEWID();
DECLARE @mAiChat        UNIQUEIDENTIFIER = NEWID();
DECLARE @mAnalytics     UNIQUEIDENTIFIER = NEWID();
DECLARE @mSystemSet     UNIQUEIDENTIFIER = NEWID();
DECLARE @mAdminUsers    UNIQUEIDENTIFIER = NEWID();
DECLARE @mQuizzes       UNIQUEIDENTIFIER = NEWID();
DECLARE @mLms           UNIQUEIDENTIFIER = NEWID();
DECLARE @mCourseMat     UNIQUEIDENTIFIER = NEWID();
DECLARE @mDiscuss       UNIQUEIDENTIFIER = NEWID();
DECLARE @mAnnounce      UNIQUEIDENTIFIER = NEWID();
DECLARE @mDegreeAudit   UNIQUEIDENTIFIER = NEWID();
DECLARE @mGradElig      UNIQUEIDENTIFIER = NEWID();
DECLARE @mDegreeRules   UNIQUEIDENTIFIER = NEWID();
DECLARE @mGenCert       UNIQUEIDENTIFIER = NEWID();
DECLARE @mResultCalc    UNIQUEIDENTIFIER = NEWID();
DECLARE @mPrereq        UNIQUEIDENTIFIER = NEWID();
DECLARE @mGradApply     UNIQUEIDENTIFIER = NEWID();
DECLARE @mGradApps      UNIQUEIDENTIFIER = NEWID();
DECLARE @mGradingCfg    UNIQUEIDENTIFIER = NEWID();
DECLARE @mStudyPlan     UNIQUEIDENTIFIER = NEWID();
DECLARE @mStuTT         UNIQUEIDENTIFIER = NEWID();
DECLARE @mAttendance    UNIQUEIDENTIFIER = NEWID();
DECLARE @mResults       UNIQUEIDENTIFIER = NEWID();
DECLARE @mFyp           UNIQUEIDENTIFIER = NEWID();
DECLARE @mLifecycle     UNIQUEIDENTIFIER = NEWID();
DECLARE @m2FA           UNIQUEIDENTIFIER = NEWID();
DECLARE @mBuildings     UNIQUEIDENTIFIER = NEWID();
DECLARE @mRooms         UNIQUEIDENTIFIER = NEWID();
DECLARE @mReportSet     UNIQUEIDENTIFIER = NEWID();
DECLARE @mModuleComp    UNIQUEIDENTIFIER = NEWID();
DECLARE @mSidebar       UNIQUEIDENTIFIER = NEWID();
DECLARE @mTheme         UNIQUEIDENTIFIER = NEWID();
DECLARE @mLicense       UNIQUEIDENTIFIER = NEWID();
DECLARE @mDashSet       UNIQUEIDENTIFIER = NEWID();
DECLARE @mPolicy        UNIQUEIDENTIFIER = NEWID();
DECLARE @mLibrary       UNIQUEIDENTIFIER = NEWID();
DECLARE @mAccred        UNIQUEIDENTIFIER = NEWID();
DECLARE @mAudit         UNIQUEIDENTIFIER = NEWID();
DECLARE @mNotif         UNIQUEIDENTIFIER = NEWID();
DECLARE @mUserImp       UNIQUEIDENTIFIER = NEWID();

INSERT INTO [sidebar_menu_items] ([Id],[Name],[Purpose],[Key],[ParentId],[DisplayOrder],[IsActive],[IsSystemMenu],[IsDeleted],[CreatedAt])
VALUES
(@mDashboard, N'Dashboard',              N'Main dashboard overview',          N'dashboard',                NULL,  1, 1, 1, 0, @Now),
(@mTenants,   N'Tenant Management',      N'Manage tenants',                   N'tenant_management',        NULL, 10, 1, 0, 0, @Now),
(@mCampuses,  N'Campus Management',      N'Manage campuses',                  N'campus_management',        NULL, 11, 1, 0, 0, @Now),
(@mDepts,     N'Departments',            N'Manage departments',               N'departments',              NULL, 12, 1, 0, 0, @Now),
(@mPrograms,  N'Programs',               N'Manage academic programs',         N'programs',                 NULL, 13, 1, 0, 0, @Now),
(@mCourses,   N'Courses',                N'Manage courses',                   N'courses',                  NULL, 14, 1, 0, 0, @Now),
(@mEnroll,    N'Enrollments',            N'Manage course enrollments',        N'enrollments',              NULL, 15, 1, 0, 0, @Now),
(@mStudents,  N'Students',               N'Manage student records',           N'students',                 NULL, 16, 1, 0, 0, @Now),
(@mTimetable, N'Timetable Admin',        N'Manage timetables',                N'timetable_admin',          NULL, 20, 1, 0, 0, @Now),
(@mTeacherTT, N'Teacher Timetable',      N'View teacher schedule',            N'timetable_teacher',        NULL, 21, 1, 0, 0, @Now),
(@mAssign,    N'Assignments',            N'Manage assignments',               N'assignments',              NULL, 22, 1, 0, 0, @Now),
(@mEnterAtt,  N'Enter Attendance',       N'Mark student attendance',          N'enter_attendance',         NULL, 23, 1, 0, 0, @Now),
(@mEnterRes,  N'Enter Results',          N'Enter student results',            N'enter_results',            NULL, 24, 1, 0, 0, @Now),
(@mGradebook, N'Gradebook',              N'View and manage gradebook',        N'gradebook',                NULL, 25, 1, 0, 0, @Now),
(@mLookups,   N'Lookups',               N'Reference data and lookup masters', N'lookups',                  NULL, 17, 1, 0, 0, @Now),
(@mRubric,    N'Rubric Management',      N'Manage assessment rubrics',        N'rubric_manage',            NULL, 26, 1, 0, 0, @Now),
(@mQuizzes,   N'Quizzes',                N'Manage quizzes and tests',         N'quizzes',                  NULL, 27, 1, 0, 0, @Now),
(@mLms,       N'LMS Manage',             N'Learning management system',       N'lms_manage',               NULL, 28, 1, 0, 0, @Now),
(@mCourseMat, N'Course Material',        N'Upload and manage course content', N'course_material',           NULL, 29, 1, 0, 0, @Now),
(@mDiscuss,   N'Discussion',             N'Course discussion forums',         N'discussion',               NULL, 30, 1, 0, 0, @Now),
(@mAnnounce,  N'Announcements',          N'Course and system announcements',  N'announcements',            NULL, 31, 1, 0, 0, @Now),
(@mDegreeAudit,N'Degree Audit',          N'Audit degree progress',            N'degree_audit',             NULL, 40, 1, 0, 0, @Now),
(@mGradElig,  N'Graduation Eligibility', N'Check graduation requirements',    N'graduation_eligibility',   NULL, 41, 1, 0, 0, @Now),
(@mDegreeRules,N'Degree Rules',          N'Configure degree requirements',    N'degree_rules',             NULL, 42, 1, 0, 0, @Now),
(@mGenCert,   N'Generate Certificates',  N'Generate academic certificates',   N'generate_certificates',    NULL, 43, 1, 0, 0, @Now),
(@mResultCalc,N'Result Calculation',     N'Configure result calculation',     N'result_calculation',       NULL, 44, 1, 0, 0, @Now),
(@mPrereq,    N'Prerequisites',          N'Manage course prerequisites',      N'prerequisites',            NULL, 45, 1, 0, 0, @Now),
(@mGradApply, N'Graduation Apply',       N'Apply for graduation',             N'graduation_apply',         NULL, 46, 1, 0, 0, @Now),
(@mGradApps,  N'Graduation Applications',N'Review graduation applications',   N'graduation_applications',  NULL, 47, 1, 0, 0, @Now),
(@mGradingCfg,N'Grading Config',         N'Configure grading system',         N'grading_config',           NULL, 48, 1, 0, 0, @Now),
(@mStudyPlan, N'Study Plan',             N'Plan course schedule',             N'study_plan',               NULL, 49, 1, 0, 0, @Now),
(@mStuTT,     N'Student Timetable',      N'View student schedule',            N'timetable_student',        NULL, 60, 1, 0, 0, @Now),
(@mAttendance,N'Attendance',             N'View attendance records',          N'attendance',               NULL, 61, 1, 0, 0, @Now),
(@mResults,   N'Results',                N'View exam results',                N'results',                  NULL, 62, 1, 0, 0, @Now),
(@mFyp,       N'FYP',                    N'Final Year Projects',              N'fyp',                      NULL, 63, 1, 0, 0, @Now),
(@mLifecycle, N'Student Lifecycle',      N'View student progression',         N'student_lifecycle',        NULL, 64, 1, 0, 0, @Now),
(@m2FA,       N'Two-Factor Authentication',N'MFA security settings',          N'two_factor_auth',          NULL, 80, 1, 0, 0, @Now),
(@mBuildings, N'Buildings',              N'Manage campus buildings',          N'buildings',                NULL, 81, 1, 0, 0, @Now),
(@mRooms,     N'Rooms',                  N'Manage classrooms and rooms',      N'rooms',                    NULL, 82, 1, 0, 0, @Now),
(@mReportSet, N'Report Settings',        N'Configure report definitions',     N'report_settings',          NULL, 83, 1, 0, 0, @Now),
(@mModuleComp,N'Module Composition',     N'Configure module visibility',      N'module_composition',       NULL, 84, 1, 0, 0, @Now),
(@mSidebar,   N'Sidebar Settings',       N'Configure sidebar navigation',     N'sidebar_settings',         NULL, 85, 1, 0, 0, @Now),
(@mTheme,     N'Theme Settings',         N'Configure portal theme',           N'theme_settings',           NULL, 86, 1, 0, 0, @Now),
(@mLicense,   N'License Update',         N'Upload and manage license',        N'license_update',           NULL, 87, 1, 0, 0, @Now),
(@mDashSet,   N'Dashboard Settings',     N'Configure dashboard widgets',      N'dashboard_settings',       NULL, 88, 1, 0, 0, @Now),
(@mPolicy,    N'Institution Policy',     N'Configure institution policies',   N'institution_policy',       NULL, 89, 1, 0, 0, @Now),
(@mLibrary,   N'Library Config',         N'Configure library integration',    N'library_config',           NULL, 90, 1, 0, 0, @Now),
(@mAccred,    N'Accreditation',          N'Manage accreditation templates',   N'accreditation',            NULL, 91, 1, 0, 0, @Now),
(@mAudit,     N'Advanced Audit',         N'View detailed audit logs',         N'audit_logs',               NULL, 92, 1, 0, 0, @Now),
(@mNotif,     N'Notifications',          N'View system notifications',        N'notifications',            NULL, 93, 1, 0, 0, @Now),
(@mUserImp,   N'User Import',            N'Import users from CSV',            N'user_import',              NULL, 94, 1, 0, 0, @Now),
(@mPayments,  N'Payments',               N'Track and manage payment receipts', N'payments',                 NULL, 65, 1, 0, 0, @Now),
(@mReportCenter,N'Report Center',         N'Run filter and export reports',     N'report_center',            NULL, 66, 1, 0, 0, @Now),
(@mHelpdesk,  N'Helpdesk',               N'Ticket management and support',     N'helpdesk',                 NULL, 67, 1, 0, 0, @Now),
(@mAiChat,    N'AI Chat',                N'AI assistant for contextual help',  N'ai_chat',                  NULL, 68, 1, 0, 0, @Now),
(@mAnalytics, N'Analytics',              N'Performance and trends dashboards', N'analytics',                NULL, 69, 1, 0, 0, @Now),
(@mSystemSet, N'System Settings',        N'Platform-level system configuration',N'system_settings',         NULL, 95, 1, 0, 0, @Now),
(@mAdminUsers,N'Admin Users',            N'Manage admin account lifecycle',    N'admin_users',              NULL, 96, 1, 0, 0, @Now);

-- ═══════ ROLE-BASED ACCESS ═══════
-- SuperAdmin: all menus
INSERT INTO [sidebar_menu_role_accesses] ([Id],[SidebarMenuItemId],[RoleName],[IsAllowed],[CreatedAt])
SELECT NEWID(), m.Id, N'SuperAdmin', 1, @Now FROM [sidebar_menu_items] m;

-- Admin: all except dashboard and SuperAdmin-only settings
INSERT INTO [sidebar_menu_role_accesses] ([Id],[SidebarMenuItemId],[RoleName],[IsAllowed],[CreatedAt])
SELECT NEWID(), m.Id, N'Admin', 1, @Now FROM [sidebar_menu_items] m
WHERE m.[Key] NOT IN (N'dashboard',N'module_composition',N'sidebar_settings',N'license_update',
    N'dashboard_settings',N'institution_policy',N'library_config',N'system_settings',
    N'report_settings',N'admin_users',N'tenant_management',N'campus_management');

-- Faculty: teaching + academic + personal (no admin settings, no finance)
INSERT INTO [sidebar_menu_role_accesses] ([Id],[SidebarMenuItemId],[RoleName],[IsAllowed],[CreatedAt])
SELECT NEWID(), m.Id, N'Faculty', 1, @Now FROM [sidebar_menu_items] m
WHERE m.[Key] IN (N'dashboard',N'timetable_teacher',N'timetable_student',N'assignments',
    N'enter_attendance',N'attendance',N'enter_results',N'results',N'gradebook',
    N'rubric_manage',N'quizzes',N'course_material',N'discussion',N'announcements',
    N'degree_audit',N'graduation_apply',N'study_plan',N'fyp',
    N'helpdesk',N'report_center',N'ai_chat',N'analytics',N'notifications',
    N'theme_settings',N'courses');

-- Student: self-service (view/submit only, no CRUD)
INSERT INTO [sidebar_menu_role_accesses] ([Id],[SidebarMenuItemId],[RoleName],[IsAllowed],[CreatedAt])
SELECT NEWID(), m.Id, N'Student', 1, @Now FROM [sidebar_menu_items] m
WHERE m.[Key] IN (N'dashboard',N'timetable_student',N'assignments',N'quizzes',
    N'course_material',N'discussion',N'announcements',N'attendance',
    N'results',N'fyp',N'study_plan',N'degree_audit',N'generate_certificates',
    N'graduation_apply',N'payments',N'helpdesk',N'ai_chat',N'notifications',
    N'theme_settings',N'two_factor_auth');

-- Finance: payments, payment reports, payment analytics, personal settings
INSERT INTO [sidebar_menu_role_accesses] ([Id],[SidebarMenuItemId],[RoleName],[IsAllowed],[CreatedAt])
SELECT NEWID(), m.Id, N'Finance', 1, @Now FROM [sidebar_menu_items] m
WHERE m.[Key] IN (N'dashboard',N'payments',N'report_center',N'analytics',
    N'theme_settings',N'notifications');

PRINT 'Sidebar menu structure created successfully.';
GO
