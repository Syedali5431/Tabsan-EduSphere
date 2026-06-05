SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Restructures the sidebar menu with labeled parent groups.
  Creates new parent containers, reassigns children, adds new leaf items.
  Idempotent — safe to re-run.
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    RAISERROR('Database [Tabsan-EduSphere] does not exist.', 16, 1);
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

BEGIN TRY
BEGIN TRANSACTION;

-- ==============================================================================
-- STEP 1: Create parent label menus (if not exist)
-- ==============================================================================

DECLARE @FoundationSetupId       UNIQUEIDENTIFIER;
DECLARE @StudentMgmtId           UNIQUEIDENTIFIER;
DECLARE @TimetableAttendanceId   UNIQUEIDENTIFIER;
DECLARE @LmsId                   UNIQUEIDENTIFIER;
DECLARE @AssessmentResultsId     UNIQUEIDENTIFIER;
DECLARE @InstitutionReportsId    UNIQUEIDENTIFIER;
DECLARE @CommunicationSupportId  UNIQUEIDENTIFIER;
DECLARE @FinanceDeptId           UNIQUEIDENTIFIER;
DECLARE @SecurityPolicyId        UNIQUEIDENTIFIER;

-- foundation_setup
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'foundation_setup')
BEGIN
    SET @FoundationSetupId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@FoundationSetupId, N'Foundation Setup', N'Core institutional structure: tenants, campuses, buildings, departments, programs, courses, prerequisites.', N'foundation_setup', NULL, 2, 1, 0, 0, @Now);
END
ELSE SELECT @FoundationSetupId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'foundation_setup';

-- student_management
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'student_management')
BEGIN
    SET @StudentMgmtId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@StudentMgmtId, N'Student Management', N'Student lifecycle, enrollments, study plans, and results tracking.', N'student_management', NULL, 3, 1, 0, 0, @Now);
END
ELSE SELECT @StudentMgmtId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'student_management';

-- timetable_attendance
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'timetable_attendance')
BEGIN
    SET @TimetableAttendanceId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@TimetableAttendanceId, N'Timetable & Attendance', N'Scheduling and attendance tracking for teachers, admins, and students.', N'timetable_attendance', NULL, 4, 1, 0, 0, @Now);
END
ELSE SELECT @TimetableAttendanceId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'timetable_attendance';

-- lms
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'lms')
BEGIN
    SET @LmsId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@LmsId, N'LMS (Learning Management System)', N'Course materials, assignments, quizzes, discussions, and announcements.', N'lms', NULL, 5, 1, 0, 0, @Now);
END
ELSE SELECT @LmsId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'lms';

-- assessment_results
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'assessment_results')
BEGIN
    SET @AssessmentResultsId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@AssessmentResultsId, N'Assessment & Results', N'Grading, rubrics, result calculation, certificates, and academic evaluation.', N'assessment_results', NULL, 6, 1, 0, 0, @Now);
END
ELSE SELECT @AssessmentResultsId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'assessment_results';

-- institution_reports
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'institution_reports')
BEGIN
    SET @InstitutionReportsId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@InstitutionReportsId, N'Institution & Reports', N'Accreditation, audit, analytics, and reporting.', N'institution_reports', NULL, 7, 1, 0, 0, @Now);
END
ELSE SELECT @InstitutionReportsId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'institution_reports';

-- communication_support
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'communication_support')
BEGIN
    SET @CommunicationSupportId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@CommunicationSupportId, N'Communication & Support', N'Notifications and helpdesk ticketing.', N'communication_support', NULL, 8, 1, 0, 0, @Now);
END
ELSE SELECT @CommunicationSupportId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'communication_support';

-- finance_department
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'finance_department')
BEGIN
    SET @FinanceDeptId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@FinanceDeptId, N'Finance Department', N'Payment tracking and financial operations.', N'finance_department', NULL, 10, 1, 0, 0, @Now);
END
ELSE SELECT @FinanceDeptId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'finance_department';

-- security_policy
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'security_policy')
BEGIN
    SET @SecurityPolicyId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@SecurityPolicyId, N'Security & Policy', N'Two-factor authentication and security settings.', N'security_policy', NULL, 11, 1, 0, 0, @Now);
END
ELSE SELECT @SecurityPolicyId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'security_policy';

-- Ensure settings parent exists (may already exist)
DECLARE @SettingsId UNIQUEIDENTIFIER;
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'settings')
BEGIN
    SET @SettingsId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (@SettingsId, N'Settings', N'User, report, and theme configuration.', N'settings', NULL, 9, 1, 0, 0, @Now);
END
ELSE SELECT @SettingsId = [Id] FROM [sidebar_menu_items] WHERE [Key] = N'settings';

-- ==============================================================================
-- STEP 2: Create new leaf menu items (if not exist)
-- ==============================================================================

-- view_results (Student read-only results)
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'view_results')
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (NEWID(), N'View Results', N'View personal academic results and transcripts.', N'view_results', @StudentMgmtId, 5, 1, 0, 0, @Now);

-- view_attendance (Student read-only attendance)
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'view_attendance')
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (NEWID(), N'View Attendance', N'View personal attendance records.', N'view_attendance', @StudentMgmtId, 6, 1, 0, 0, @Now);

-- view_payments (Student / Finance view payments)
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'view_payments')
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (NEWID(), N'View Payments', N'View payment receipts and fee status.', N'view_payments', @StudentMgmtId, 7, 1, 0, 0, @Now);

-- advanced_audit
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'advanced_audit')
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (NEWID(), N'Advanced Audit', N'Comprehensive audit logs and compliance reporting.', N'advanced_audit', @InstitutionReportsId, 2, 1, 0, 0, @Now);

-- two_factor_auth
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'two_factor_auth')
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (NEWID(), N'Two-Factor Authentication', N'Manage MFA / 2FA security settings.', N'two_factor_auth', @SecurityPolicyId, 1, 1, 0, 0, @Now);

-- ==============================================================================
-- STEP 3: Reassign existing menus to new parents
-- ==============================================================================

-- Foundation Setup children
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 1  WHERE [Key] = N'tenant_management';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 2  WHERE [Key] = N'admin_users';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 3  WHERE [Key] = N'user_import';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 4  WHERE [Key] = N'campus_management';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 5  WHERE [Key] = N'buildings';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 6  WHERE [Key] = N'rooms';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 7  WHERE [Key] = N'departments';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 8  WHERE [Key] = N'programs';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 9  WHERE [Key] = N'courses';
UPDATE [sidebar_menu_items] SET [ParentId] = @FoundationSetupId, [DisplayOrder] = 10 WHERE [Key] = N'prerequisites';

-- Student Management children
UPDATE [sidebar_menu_items] SET [ParentId] = @StudentMgmtId, [DisplayOrder] = 1 WHERE [Key] = N'students';
UPDATE [sidebar_menu_items] SET [ParentId] = @StudentMgmtId, [DisplayOrder] = 2 WHERE [Key] = N'enrollments';
UPDATE [sidebar_menu_items] SET [ParentId] = @StudentMgmtId, [DisplayOrder] = 3 WHERE [Key] = N'student_lifecycle';
UPDATE [sidebar_menu_items] SET [ParentId] = @StudentMgmtId, [DisplayOrder] = 4 WHERE [Key] = N'study_plan';

-- Timetable & Attendance children
UPDATE [sidebar_menu_items] SET [ParentId] = @TimetableAttendanceId, [DisplayOrder] = 1 WHERE [Key] = N'timetable_teacher';
UPDATE [sidebar_menu_items] SET [ParentId] = @TimetableAttendanceId, [DisplayOrder] = 2 WHERE [Key] = N'timetable_admin';
UPDATE [sidebar_menu_items] SET [ParentId] = @TimetableAttendanceId, [DisplayOrder] = 3 WHERE [Key] = N'attendance';
UPDATE [sidebar_menu_items] SET [ParentId] = @TimetableAttendanceId, [DisplayOrder] = 4 WHERE [Key] = N'enter_attendance';
UPDATE [sidebar_menu_items] SET [ParentId] = @TimetableAttendanceId, [DisplayOrder] = 5 WHERE [Key] = N'timetable_student';

-- LMS children
UPDATE [sidebar_menu_items] SET [ParentId] = @LmsId, [DisplayOrder] = 1 WHERE [Key] = N'lms_manage';
UPDATE [sidebar_menu_items] SET [ParentId] = @LmsId, [DisplayOrder] = 2 WHERE [Key] = N'course_material';
UPDATE [sidebar_menu_items] SET [ParentId] = @LmsId, [DisplayOrder] = 3 WHERE [Key] = N'assignments';
UPDATE [sidebar_menu_items] SET [ParentId] = @LmsId, [DisplayOrder] = 4 WHERE [Key] = N'quizzes';
UPDATE [sidebar_menu_items] SET [ParentId] = @LmsId, [DisplayOrder] = 5 WHERE [Key] = N'discussion';
UPDATE [sidebar_menu_items] SET [ParentId] = @LmsId, [DisplayOrder] = 6 WHERE [Key] = N'announcements';

-- Assessment & Results children
UPDATE [sidebar_menu_items] SET [ParentId] = @AssessmentResultsId, [DisplayOrder] = 1 WHERE [Key] = N'enter_results';
UPDATE [sidebar_menu_items] SET [ParentId] = @AssessmentResultsId, [DisplayOrder] = 2 WHERE [Key] = N'gradebook';
UPDATE [sidebar_menu_items] SET [ParentId] = @AssessmentResultsId, [DisplayOrder] = 3 WHERE [Key] = N'grading_config';
UPDATE [sidebar_menu_items] SET [ParentId] = @AssessmentResultsId, [DisplayOrder] = 4 WHERE [Key] = N'rubric_manage';
UPDATE [sidebar_menu_items] SET [ParentId] = @AssessmentResultsId, [DisplayOrder] = 5 WHERE [Key] = N'result_calculation';
UPDATE [sidebar_menu_items] SET [ParentId] = @AssessmentResultsId, [DisplayOrder] = 6 WHERE [Key] = N'generate_certificates';

-- Institution & Reports children
UPDATE [sidebar_menu_items] SET [ParentId] = @InstitutionReportsId, [DisplayOrder] = 1 WHERE [Key] = N'accreditation';
UPDATE [sidebar_menu_items] SET [ParentId] = @InstitutionReportsId, [DisplayOrder] = 3 WHERE [Key] = N'analytics';
UPDATE [sidebar_menu_items] SET [ParentId] = @InstitutionReportsId, [DisplayOrder] = 4 WHERE [Key] = N'report_center';

-- Communication & Support children
UPDATE [sidebar_menu_items] SET [ParentId] = @CommunicationSupportId, [DisplayOrder] = 1 WHERE [Key] = N'notifications';
UPDATE [sidebar_menu_items] SET [ParentId] = @CommunicationSupportId, [DisplayOrder] = 2 WHERE [Key] = N'helpdesk';

-- Settings children (keep only user_settings, report_settings, theme_settings)
UPDATE [sidebar_menu_items] SET [ParentId] = @SettingsId, [DisplayOrder] = 1 WHERE [Key] = N'user_settings';
UPDATE [sidebar_menu_items] SET [ParentId] = @SettingsId, [DisplayOrder] = 2 WHERE [Key] = N'report_settings';
UPDATE [sidebar_menu_items] SET [ParentId] = @SettingsId, [DisplayOrder] = 3 WHERE [Key] = N'theme_settings';

-- Finance Department children
UPDATE [sidebar_menu_items] SET [ParentId] = @FinanceDeptId, [DisplayOrder] = 1 WHERE [Key] = N'payments';

-- ==============================================================================
-- STEP 4: Move student view_payments under Finance Department too
-- ==============================================================================
-- view_payments also appears under Finance Department
IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = N'view_payments_finance')
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [IsDeleted], [CreatedAt])
    VALUES (NEWID(), N'View Payments', N'View and reconcile payment receipts.', N'view_payments_finance', @FinanceDeptId, 2, 1, 0, 0, @Now);

-- ==============================================================================
-- STEP 5: Remove orphaned parent containers (academic, lookups, system_settings)
-- Deactivate the old parent containers that are no longer used
-- ==============================================================================
UPDATE [sidebar_menu_items] SET [IsActive] = 0, [ParentId] = NULL WHERE [Key] = N'academic';
UPDATE [sidebar_menu_items] SET [IsActive] = 0, [ParentId] = NULL WHERE [Key] = N'lookups';
UPDATE [sidebar_menu_items] SET [IsActive] = 0, [ParentId] = NULL WHERE [Key] = N'system_settings';

-- Fix settings DisplayOrder to 9 (between communication_support=8 and finance_department=10)
UPDATE [sidebar_menu_items] SET [DisplayOrder] = 9 WHERE [Key] = N'settings';

-- Fix orphaned standalone menus display orders
UPDATE [sidebar_menu_items] SET [DisplayOrder] = 12 WHERE [Key] = N'results';
UPDATE [sidebar_menu_items] SET [DisplayOrder] = 20 WHERE [Key] = N'fyp';
UPDATE [sidebar_menu_items] SET [DisplayOrder] = 21 WHERE [Key] = N'ai_chat';

-- ==============================================================================
-- STEP 6: Full sidebar role-access matrix for ALL menus
-- ==============================================================================
DELETE FROM [sidebar_menu_role_accesses] WHERE [RoleName] IN (N'Admin', N'Faculty', N'Student', N'Finance');

INSERT INTO [sidebar_menu_role_accesses] ([Id], [SidebarMenuItemId], [RoleName], [IsAllowed], [CreatedAt], [UpdatedAt])
SELECT NEWID(), m.[Id], ra.[RoleName], 1, @Now, NULL FROM [sidebar_menu_items] m JOIN (VALUES
-- Dashboard: all roles
(N'dashboard',N'Admin'),(N'dashboard',N'Faculty'),(N'dashboard',N'Student'),(N'dashboard',N'Finance'),

-- Foundation Setup: Admin only (full CRUD)
(N'tenant_management',N'Admin'),(N'admin_users',N'Admin'),(N'user_import',N'Admin'),
(N'campus_management',N'Admin'),(N'buildings',N'Admin'),(N'rooms',N'Admin'),
(N'departments',N'Admin'),(N'programs',N'Admin'),(N'courses',N'Admin'),(N'prerequisites',N'Admin'),

-- Student Management: Admin full, Faculty view
(N'students',N'Admin'),(N'enrollments',N'Admin'),(N'student_lifecycle',N'Admin'),(N'study_plan',N'Admin'),
(N'view_results',N'Admin'),(N'view_attendance',N'Admin'),(N'view_payments',N'Admin'),
(N'students',N'Faculty'),(N'study_plan',N'Faculty'),
(N'students',N'Student'),(N'study_plan',N'Student'),(N'view_results',N'Student'),(N'view_attendance',N'Student'),(N'view_payments',N'Student'),

-- Timetable & Attendance: Admin + Faculty (enter/manage), Student (view own)
(N'timetable_admin',N'Admin'),(N'timetable_teacher',N'Admin'),(N'timetable_student',N'Admin'),
(N'attendance',N'Admin'),(N'enter_attendance',N'Admin'),
(N'timetable_teacher',N'Faculty'),(N'timetable_student',N'Faculty'),
(N'attendance',N'Faculty'),(N'enter_attendance',N'Faculty'),
(N'timetable_student',N'Student'),

-- LMS: all roles (Faculty full, Student view/interact)
(N'lms_manage',N'Admin'),(N'course_material',N'Admin'),(N'assignments',N'Admin'),(N'quizzes',N'Admin'),
(N'discussion',N'Admin'),(N'announcements',N'Admin'),
(N'lms_manage',N'Faculty'),(N'course_material',N'Faculty'),(N'assignments',N'Faculty'),(N'quizzes',N'Faculty'),
(N'discussion',N'Faculty'),(N'announcements',N'Faculty'),
(N'lms_manage',N'Student'),(N'course_material',N'Student'),(N'assignments',N'Student'),(N'quizzes',N'Student'),
(N'discussion',N'Student'),(N'announcements',N'Student'),

-- Assessment & Results: Admin + Faculty, Student view
(N'enter_results',N'Admin'),(N'gradebook',N'Admin'),(N'grading_config',N'Admin'),
(N'rubric_manage',N'Admin'),(N'result_calculation',N'Admin'),(N'generate_certificates',N'Admin'),
(N'enter_results',N'Faculty'),(N'gradebook',N'Faculty'),(N'result_calculation',N'Faculty'),(N'generate_certificates',N'Faculty'),
(N'results',N'Student'),

-- Institution & Reports: Admin + Faculty (view), Student (view)
(N'accreditation',N'Admin'),(N'advanced_audit',N'Admin'),(N'analytics',N'Admin'),(N'report_center',N'Admin'),
(N'analytics',N'Faculty'),(N'report_center',N'Faculty'),
(N'analytics',N'Student'),(N'report_center',N'Student'),

-- Communication & Support: all roles
(N'notifications',N'Admin'),(N'helpdesk',N'Admin'),
(N'notifications',N'Faculty'),(N'helpdesk',N'Faculty'),
(N'notifications',N'Student'),(N'helpdesk',N'Student'),
(N'notifications',N'Finance'),(N'helpdesk',N'Finance'),

-- Settings: all roles (scoped)
(N'user_settings',N'Admin'),(N'report_settings',N'Admin'),(N'theme_settings',N'Admin'),
(N'user_settings',N'Faculty'),(N'theme_settings',N'Faculty'),
(N'user_settings',N'Student'),(N'theme_settings',N'Student'),
(N'user_settings',N'Finance'),(N'theme_settings',N'Finance'),

-- Finance Department: Admin + Finance
(N'payments',N'Admin'),(N'view_payments_finance',N'Admin'),
(N'payments',N'Finance'),(N'view_payments_finance',N'Finance'),

-- Security & Policy: all roles
(N'two_factor_auth',N'Admin'),(N'two_factor_auth',N'Faculty'),
(N'two_factor_auth',N'Student'),(N'two_factor_auth',N'Finance'),

-- Parent containers: grant access to roles that see any child
(N'foundation_setup',N'Admin'),
(N'student_management',N'Admin'),(N'student_management',N'Faculty'),(N'student_management',N'Student'),
(N'timetable_attendance',N'Admin'),(N'timetable_attendance',N'Faculty'),(N'timetable_attendance',N'Student'),
(N'lms',N'Admin'),(N'lms',N'Faculty'),(N'lms',N'Student'),
(N'assessment_results',N'Admin'),(N'assessment_results',N'Faculty'),(N'assessment_results',N'Student'),
(N'institution_reports',N'Admin'),(N'institution_reports',N'Faculty'),(N'institution_reports',N'Student'),
(N'communication_support',N'Admin'),(N'communication_support',N'Faculty'),(N'communication_support',N'Student'),(N'communication_support',N'Finance'),
(N'settings',N'Admin'),(N'settings',N'Faculty'),(N'settings',N'Student'),(N'settings',N'Finance'),
(N'finance_department',N'Admin'),(N'finance_department',N'Finance'),
(N'security_policy',N'Admin'),(N'security_policy',N'Faculty'),(N'security_policy',N'Student'),(N'security_policy',N'Finance')
) ra([MenuKey], [RoleName]) ON ra.[MenuKey] = m.[Key]
WHERE NOT EXISTS (SELECT 1 FROM [sidebar_menu_role_accesses] x WHERE x.[SidebarMenuItemId] = m.[Id] AND x.[RoleName] = ra.[RoleName]);

-- ==============================================================================
-- STEP 7: Full action permissions for all menus
-- ==============================================================================
IF OBJECT_ID(N'[role_resource_permissions]') IS NOT NULL
BEGIN
    DELETE FROM [role_resource_permissions] WHERE [RoleName] IN (N'SuperAdmin',N'Admin',N'Faculty',N'Student',N'Finance');

    DECLARE @pm TABLE (MenuKey NVARCHAR(100), RoleName NVARCHAR(100), V BIT, A BIT, E BIT, D BIT, X BIT, I BIT);

    -- SuperAdmin: full access on all
    DECLARE @sa_menus TABLE (k NVARCHAR(100));
    INSERT INTO @sa_menus VALUES (N'dashboard'),(N'foundation_setup'),(N'tenant_management'),(N'admin_users'),(N'user_import'),(N'campus_management'),(N'buildings'),(N'rooms'),(N'departments'),(N'programs'),(N'courses'),(N'prerequisites'),(N'student_management'),(N'students'),(N'enrollments'),(N'student_lifecycle'),(N'study_plan'),(N'view_results'),(N'view_attendance'),(N'view_payments'),(N'timetable_attendance'),(N'timetable_teacher'),(N'timetable_admin'),(N'attendance'),(N'enter_attendance'),(N'timetable_student'),(N'lms'),(N'lms_manage'),(N'course_material'),(N'assignments'),(N'quizzes'),(N'discussion'),(N'announcements'),(N'assessment_results'),(N'enter_results'),(N'gradebook'),(N'grading_config'),(N'rubric_manage'),(N'result_calculation'),(N'generate_certificates'),(N'institution_reports'),(N'accreditation'),(N'advanced_audit'),(N'analytics'),(N'report_center'),(N'communication_support'),(N'notifications'),(N'helpdesk'),(N'settings'),(N'user_settings'),(N'report_settings'),(N'theme_settings'),(N'finance_department'),(N'payments'),(N'view_payments_finance'),(N'security_policy'),(N'two_factor_auth'),(N'results'),(N'degree_audit');
    INSERT INTO @pm SELECT k, N'SuperAdmin', 1,1,1,1,1,1 FROM @sa_menus;
    -- Dashboard: view only
    UPDATE @pm SET V=1,A=0,E=0,D=0,X=0,I=0 WHERE MenuKey=N'dashboard' AND RoleName=N'SuperAdmin';

    -- Admin: full CRUD on operational menus
    INSERT INTO @pm VALUES
    (N'dashboard',N'Admin',1,0,0,0,0,0),
    (N'tenant_management',N'Admin',1,1,1,1,1,1),(N'admin_users',N'Admin',1,1,1,1,1,1),(N'user_import',N'Admin',1,1,1,1,1,1),
    (N'campus_management',N'Admin',1,1,1,1,1,1),(N'buildings',N'Admin',1,1,1,1,1,1),(N'rooms',N'Admin',1,1,1,1,1,1),
    (N'departments',N'Admin',1,1,1,1,1,1),(N'programs',N'Admin',1,1,1,1,1,1),(N'courses',N'Admin',1,1,1,1,1,1),
    (N'prerequisites',N'Admin',1,1,1,1,1,1),
    (N'students',N'Admin',1,1,1,1,1,1),(N'enrollments',N'Admin',1,1,1,1,1,1),(N'student_lifecycle',N'Admin',1,1,1,1,1,1),
    (N'study_plan',N'Admin',1,1,1,1,1,1),(N'view_results',N'Admin',1,1,1,1,1,1),(N'view_attendance',N'Admin',1,1,1,1,1,1),
    (N'view_payments',N'Admin',1,1,1,1,1,1),
    (N'timetable_admin',N'Admin',1,1,1,1,1,1),(N'timetable_teacher',N'Admin',1,1,1,1,1,1),(N'timetable_student',N'Admin',1,1,1,1,1,1),
    (N'attendance',N'Admin',1,1,1,1,1,1),(N'enter_attendance',N'Admin',1,1,1,1,1,1),
    (N'lms_manage',N'Admin',1,1,1,1,1,1),(N'course_material',N'Admin',1,1,1,1,1,1),(N'assignments',N'Admin',1,1,1,1,1,1),
    (N'quizzes',N'Admin',1,1,1,1,1,1),(N'discussion',N'Admin',1,1,1,1,1,1),(N'announcements',N'Admin',1,1,1,1,1,1),
    (N'enter_results',N'Admin',1,1,1,1,1,1),(N'gradebook',N'Admin',1,1,1,1,1,1),(N'grading_config',N'Admin',1,1,1,1,1,1),
    (N'rubric_manage',N'Admin',1,1,1,1,1,1),(N'result_calculation',N'Admin',1,1,1,1,1,1),(N'generate_certificates',N'Admin',1,1,1,1,1,1),
    (N'accreditation',N'Admin',1,1,1,1,1,1),(N'advanced_audit',N'Admin',1,1,1,1,1,1),(N'analytics',N'Admin',1,1,1,1,1,1),
    (N'report_center',N'Admin',1,1,1,1,1,1),
    (N'notifications',N'Admin',1,1,1,1,1,1),(N'helpdesk',N'Admin',1,1,1,1,1,1),
    (N'user_settings',N'Admin',1,1,1,1,1,1),(N'report_settings',N'Admin',1,1,1,1,1,1),(N'theme_settings',N'Admin',1,1,1,1,1,1),
    (N'payments',N'Admin',1,1,1,1,1,1),(N'view_payments_finance',N'Admin',1,1,1,1,1,1),
    (N'two_factor_auth',N'Admin',1,0,1,0,0,0);

    -- Faculty: Add/Edit on teaching, View on student data
    INSERT INTO @pm VALUES
    (N'dashboard',N'Faculty',1,0,0,0,0,0),
    (N'students',N'Faculty',1,0,0,0,1,0),(N'study_plan',N'Faculty',1,0,1,0,0,0),
    (N'timetable_teacher',N'Faculty',1,0,0,0,1,0),(N'attendance',N'Faculty',1,1,1,0,1,1),
    (N'enter_attendance',N'Faculty',1,1,1,0,1,1),(N'timetable_student',N'Faculty',1,0,0,0,1,0),
    (N'lms_manage',N'Faculty',1,1,1,0,1,1),(N'course_material',N'Faculty',1,1,1,0,1,1),
    (N'assignments',N'Faculty',1,1,1,0,1,1),(N'quizzes',N'Faculty',1,1,1,0,1,1),
    (N'discussion',N'Faculty',1,1,1,0,1,1),(N'announcements',N'Faculty',1,1,1,0,1,1),
    (N'enter_results',N'Faculty',1,1,1,0,1,1),(N'gradebook',N'Faculty',1,1,1,0,1,1),
    (N'result_calculation',N'Faculty',1,0,1,0,0,0),(N'generate_certificates',N'Faculty',1,0,0,0,1,0),
    (N'analytics',N'Faculty',1,0,0,0,1,0),(N'report_center',N'Faculty',1,0,0,0,1,0),
    (N'notifications',N'Faculty',1,0,0,0,0,0),(N'helpdesk',N'Faculty',1,1,1,0,1,0),
    (N'user_settings',N'Faculty',1,0,1,0,0,0),(N'theme_settings',N'Faculty',1,0,0,0,0,0),
    (N'two_factor_auth',N'Faculty',1,0,0,0,0,0);

    -- Student: View + Export, own data only
    INSERT INTO @pm VALUES
    (N'dashboard',N'Student',1,0,0,0,0,0),
    (N'study_plan',N'Student',1,0,0,0,0,0),(N'view_results',N'Student',1,0,0,0,1,0),
    (N'view_attendance',N'Student',1,0,0,0,0,0),(N'view_payments',N'Student',1,0,0,0,0,0),
    (N'timetable_student',N'Student',1,0,0,0,0,0),
    (N'lms_manage',N'Student',1,0,0,0,0,0),(N'course_material',N'Student',1,0,0,0,1,0),
    (N'assignments',N'Student',1,0,0,0,0,0),(N'quizzes',N'Student',1,0,0,0,0,0),
    (N'discussion',N'Student',1,1,0,0,0,0),(N'announcements',N'Student',1,0,0,0,0,0),
    (N'analytics',N'Student',1,0,0,0,1,0),(N'report_center',N'Student',1,0,0,0,1,0),
    (N'notifications',N'Student',1,0,0,0,0,0),(N'helpdesk',N'Student',1,1,0,0,0,0),
    (N'user_settings',N'Student',0,0,1,0,0,0),(N'theme_settings',N'Student',0,0,0,0,0,0),
    (N'two_factor_auth',N'Student',1,0,0,0,0,0),(N'results',N'Student',1,0,0,0,1,0);

    -- Finance: Full CRUD on Payments, View on others
    INSERT INTO @pm VALUES
    (N'dashboard',N'Finance',1,0,0,0,0,0),
    (N'payments',N'Finance',1,1,1,1,1,1),(N'view_payments_finance',N'Finance',1,1,1,1,1,1),
    (N'notifications',N'Finance',1,0,0,0,0,0),(N'helpdesk',N'Finance',1,0,0,0,0,0),
    (N'analytics',N'Finance',1,0,0,0,1,0),(N'report_center',N'Finance',1,0,0,0,1,0),
    (N'user_settings',N'Finance',0,0,1,0,0,0),(N'theme_settings',N'Finance',0,0,0,0,0,0),
    (N'two_factor_auth',N'Finance',1,0,0,0,0,0);

    INSERT INTO [role_resource_permissions] ([Id], [RoleName], [ResourceKey], [CanView], [CanAdd], [CanEdit], [CanDeactivate], [CanExport], [CanImport], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), pm.RoleName, pm.MenuKey, pm.V, pm.A, pm.E, pm.D, pm.X, pm.I, @Now, NULL FROM @pm pm;
END;

COMMIT;
PRINT 'Sidebar menu restructured successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
