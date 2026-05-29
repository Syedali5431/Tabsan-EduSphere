SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Seed core data script for Tabsan EduSphere.
  
  PREREQUISITES - MUST RUN FIRST IN THIS ORDER:
  1. 01-Schema-Current.sql      - Creates all tables and schema (RUN THIS FIRST)
  2. 02-Seed-Core.sql           - This script: seeds core data (roles, institutions, departments, users)
  3. 03-FullDummyData.sql       - Adds comprehensive test data
  
  After all three scripts complete successfully:
  4. 04-Maintenance-Indexes-And-Views.sql - Creates indexes and views
  5. 05-PostDeployment-Checks.sql         - Validates data integrity

  NOTE:
  - This script must run AFTER 01-Schema-Current.sql
  - This script must run BEFORE 03-FullDummyData.sql
  - Includes comprehensive error handling with TRY-CATCH
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    RAISERROR('Database [Tabsan-EduSphere] does not exist. Run 01-Schema-Current.sql first.', 16, 1);
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

IF DB_NAME() <> N'Tabsan-EduSphere'
BEGIN
    RAISERROR('Failed to switch context to [Tabsan-EduSphere]. Aborting seed script.', 16, 1);
    RETURN;
END;
GO

BEGIN TRY
BEGIN TRANSACTION;

-- VALIDATE CRITICAL PREREQUISITES - Check table existence FIRST before any queries
IF OBJECT_ID(N'[roles]') IS NULL
BEGIN
    RAISERROR('ERROR: Table [roles] does not exist. You MUST run 01-Schema-Current.sql FIRST before running this script.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'[departments]') IS NULL
BEGIN
    RAISERROR('ERROR: Table [departments] does not exist. You MUST run 01-Schema-Current.sql FIRST before running this script.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'[users]') IS NULL
BEGIN
    RAISERROR('ERROR: Table [users] does not exist. You MUST run 01-Schema-Current.sql FIRST before running this script.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @DefaultPasswordHash NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @DefaultTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @DefaultCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);

IF OBJECT_ID(N'[tenants]') IS NOT NULL
BEGIN
    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @DefaultTenantId, N'DEFAULT', N'Default Tenant', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = @DefaultTenantId);
END;

IF OBJECT_ID(N'[campuses]') IS NOT NULL
BEGIN
    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @DefaultCampusId, @DefaultTenantId, N'MAIN', N'Main Campus', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = @DefaultCampusId);
END;

/* 1) System roles */
MERGE INTO [roles] AS tgt
USING (
    SELECT N'SuperAdmin' AS [Name], N'Full platform access - manages license and all settings.' AS [Description], CAST(1 AS bit) AS [IsSystemRole]
    UNION ALL SELECT N'Admin', N'Department-level admin - manages users and courses.', CAST(1 AS bit)
    UNION ALL SELECT N'Faculty', N'Teaches courses and manages academic content.', CAST(1 AS bit)
    UNION ALL SELECT N'Student', N'Enrolled student - accesses course and academic content.', CAST(1 AS bit)
    UNION ALL SELECT N'Finance', N'Finance and fee workflow access.', CAST(1 AS bit)
    UNION ALL SELECT N'Parent', N'Parent portal read-only learner progress visibility.', CAST(1 AS bit)
) AS src
ON tgt.[Name] = src.[Name]
WHEN MATCHED THEN
    UPDATE SET tgt.[Description] = src.[Description], tgt.[IsSystemRole] = src.[IsSystemRole]
WHEN NOT MATCHED THEN
    INSERT ([Name], [Description], [IsSystemRole])
    VALUES (src.[Name], src.[Description], src.[IsSystemRole]);

/* 2) Modules + default module_status */
UPDATE [modules]
SET [Key] = N'ai_chat', [UpdatedAt] = @Now
WHERE [Key] = N'ai-chat'
    AND NOT EXISTS (SELECT 1 FROM [modules] x WHERE x.[Key] = N'ai_chat');

UPDATE [modules]
SET [Key] = N'themes', [UpdatedAt] = @Now
WHERE [Key] = N'theming'
    AND NOT EXISTS (SELECT 1 FROM [modules] x WHERE x.[Key] = N'themes');

UPDATE [modules]
SET [Key] = N'advanced_audit', [UpdatedAt] = @Now
WHERE [Key] = N'advanced-audit'
    AND NOT EXISTS (SELECT 1 FROM [modules] x WHERE x.[Key] = N'advanced_audit');

DECLARE @Modules TABLE ([Key] NVARCHAR(50), [Name] NVARCHAR(100), [IsMandatory] bit);
INSERT INTO @Modules ([Key], [Name], [IsMandatory]) VALUES
(N'authentication', N'Authentication', 1),
(N'departments', N'Departments', 1),
(N'sis', N'Student Information', 1),
(N'courses', N'Courses', 0),
(N'assignments', N'Assignments', 0),
(N'quizzes', N'Quizzes', 0),
(N'attendance', N'Attendance', 0),
(N'results', N'Results / Grades', 0),
(N'notifications', N'Notifications', 0),
(N'fyp', N'Final Year Projects', 0),
(N'ai_chat', N'AI Chatbot', 0),
(N'reports', N'Reports', 0),
(N'themes', N'UI Themes', 0),
(N'advanced_audit', N'Advanced Audit Logging', 0);

INSERT INTO [modules] ([Id], [Key], [Name], [IsMandatory], [CreatedAt], [UpdatedAt])
SELECT NEWID(), m.[Key], m.[Name], m.[IsMandatory], @Now, NULL
FROM @Modules m
WHERE NOT EXISTS (SELECT 1 FROM [modules] x WHERE x.[Key] = m.[Key]);

INSERT INTO [module_status] ([Id], [ModuleId], [IsActive], [ActivatedAt], [Source], [ChangedBy], [CreatedAt], [UpdatedAt])
SELECT NEWID(), m.[Id], CASE WHEN m.[IsMandatory] = 1 THEN 1 ELSE 0 END,
       CASE WHEN m.[IsMandatory] = 1 THEN @Now ELSE NULL END,
       CASE WHEN m.[IsMandatory] = 1 THEN N'mandatory' ELSE N'seed' END,
       NULL,
       @Now,
       NULL
FROM [modules] m
WHERE NOT EXISTS (SELECT 1 FROM [module_status] s WHERE s.[ModuleId] = m.[Id]);

/* 3) Portal settings */
MERGE INTO [portal_settings] AS tgt
USING (
    SELECT N'portal.universityName' AS [Key], N'Tabsan EduSphere University' AS [Value]
    UNION ALL SELECT N'portal.brandInitials', N'TE'
    UNION ALL SELECT N'portal.theme', N'default'
    UNION ALL SELECT N'portal.timeZone', N'UTC'
    UNION ALL SELECT N'institution_include_school', N'true'
    UNION ALL SELECT N'institution_include_college', N'true'
    UNION ALL SELECT N'institution_include_university', N'true'
) AS src
ON tgt.[Key] = src.[Key]
WHEN MATCHED THEN
    UPDATE SET tgt.[Value] = src.[Value], tgt.[UpdatedAt] = @Now
WHEN NOT MATCHED THEN
    INSERT ([Id], [Key], [Value], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), src.[Key], src.[Value], @Now, NULL);

/* 4) Baseline institute-aware departments (School, College, University) */
DECLARE @CoreDepartments TABLE ([Id] UNIQUEIDENTIFIER, [Name] NVARCHAR(200), [Code] NVARCHAR(20), [InstitutionType] INT);
INSERT INTO @CoreDepartments ([Id], [Name], [Code], [InstitutionType]) VALUES
(CAST('21000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'Core University Department', N'CORE-UNI', 2),
(CAST('21000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'Core College Department', N'CORE-COL', 1),
(CAST('21000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'Core School Department', N'CORE-SCH', 0);

INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT d.[Id], d.[Name], d.[Code], d.[InstitutionType], 1, @Now, NULL, 0, NULL
FROM @CoreDepartments d
WHERE NOT EXISTS (SELECT 1 FROM [departments] x WHERE x.[Id] = d.[Id]);

IF COL_LENGTH('departments', 'TenantId') IS NOT NULL AND COL_LENGTH('departments', 'CampusId') IS NOT NULL
BEGIN
    UPDATE d
    SET d.[TenantId] = @DefaultTenantId,
        d.[CampusId] = @DefaultCampusId,
        d.[UpdatedAt] = @Now
    FROM [departments] d
    WHERE d.[Id] IN (SELECT [Id] FROM @CoreDepartments)
      AND (d.[TenantId] IS NULL OR d.[CampusId] IS NULL);
END;

/* 4.1) Baseline users (default password: EduSphere147) */
DECLARE @RoleSuperAdminId INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'SuperAdmin');
DECLARE @RoleAdminId INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Admin');
DECLARE @RoleFacultyId INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Faculty');
DECLARE @RoleStudentId INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Student');

DECLARE @CoreUsers TABLE
(
    [Id] UNIQUEIDENTIFIER,
    [Username] NVARCHAR(100),
    [Email] NVARCHAR(256),
    [RoleId] INT,
    [DepartmentId] UNIQUEIDENTIFIER NULL,
    [InstitutionType] INT NULL
);

INSERT INTO @CoreUsers ([Id], [Username], [Email], [RoleId], [DepartmentId], [InstitutionType])
VALUES
    (CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), N'superadmin', N'superadmin@tabsan.local', @RoleSuperAdminId, NULL, NULL),
    (CAST('66666666-6666-6666-6666-666666666602' AS UNIQUEIDENTIFIER), N'admin',      N'admin@tabsan.local',      @RoleAdminId,      CAST('21000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), 2),
    (CAST('66666666-6666-6666-6666-666666666603' AS UNIQUEIDENTIFIER), N'faculty',    N'faculty@tabsan.local',    @RoleFacultyId,    CAST('21000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), 2),
    (CAST('66666666-6666-6666-6666-666666666604' AS UNIQUEIDENTIFIER), N'student',    N'student@tabsan.local',    @RoleStudentId,    CAST('21000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), 2);

INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT u.[Id], u.[Username], u.[Email], @DefaultPasswordHash, u.[RoleId], u.[DepartmentId], u.[InstitutionType], 1, NULL, @Now, NULL, 0, NULL
FROM @CoreUsers u
WHERE NOT EXISTS (SELECT 1 FROM [users] x WHERE x.[Id] = u.[Id]);

UPDATE u
SET u.[Username] = src.[Username],
    u.[Email] = src.[Email],
    u.[PasswordHash] = @DefaultPasswordHash,
    u.[RoleId] = src.[RoleId],
    u.[DepartmentId] = src.[DepartmentId],
    u.[InstitutionType] = src.[InstitutionType],
    u.[IsActive] = 1,
    u.[IsDeleted] = 0,
    u.[DeletedAt] = NULL,
    u.[UpdatedAt] = @Now
FROM [users] u
INNER JOIN @CoreUsers src ON src.[Id] = u.[Id];

IF COL_LENGTH('users', 'TenantId') IS NOT NULL AND COL_LENGTH('users', 'CampusId') IS NOT NULL
BEGIN
    UPDATE u
    SET u.[TenantId] = CASE WHEN r.[Name] = N'SuperAdmin' THEN NULL ELSE @DefaultTenantId END,
        u.[CampusId] = CASE WHEN r.[Name] = N'SuperAdmin' THEN NULL ELSE @DefaultCampusId END,
        u.[UpdatedAt] = @Now
    FROM [users] u
    INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
    WHERE u.[Id] IN (SELECT [Id] FROM @CoreUsers);
END;

IF COL_LENGTH('users', 'PhoneNumber') IS NOT NULL
BEGIN
    UPDATE u
    SET u.[PhoneNumber] = CASE u.[Username]
        WHEN N'superadmin' THEN N'+61490000001'
        WHEN N'admin' THEN N'+61490000002'
        WHEN N'faculty' THEN N'+61490000003'
        WHEN N'student' THEN N'+61490000004'
        ELSE u.[PhoneNumber]
    END,
    u.[UpdatedAt] = @Now
    FROM [users] u
    WHERE u.[Id] IN (SELECT [Id] FROM @CoreUsers);
END;

IF OBJECT_ID(N'[password_history]') IS NOT NULL
BEGIN
    INSERT INTO [password_history] ([Id], [UserId], [PasswordHash], [CreatedAt])
    SELECT NEWID(), u.[Id], @DefaultPasswordHash, @Now
    FROM @CoreUsers u
    WHERE NOT EXISTS (
        SELECT 1
        FROM [password_history] ph
        WHERE ph.[UserId] = u.[Id]
          AND ph.[PasswordHash] = @DefaultPasswordHash
    );
END

/* 4.2) Baseline admin-to-department assignment for menu-scoped pages */
IF OBJECT_ID(N'[admin_department_assignments]') IS NOT NULL
BEGIN
    DECLARE @CoreAdminUserId UNIQUEIDENTIFIER = CAST('66666666-6666-6666-6666-666666666602' AS UNIQUEIDENTIFIER);
    DECLARE @CoreAdminDepartmentId UNIQUEIDENTIFIER = CAST('21000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);

    IF EXISTS (SELECT 1 FROM [users] WHERE [Id] = @CoreAdminUserId)
       AND EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @CoreAdminDepartmentId)
       AND NOT EXISTS (
           SELECT 1
           FROM [admin_department_assignments] a
           WHERE a.[AdminUserId] = @CoreAdminUserId
             AND a.[DepartmentId] = @CoreAdminDepartmentId
             AND a.[RemovedAt] IS NULL)
    BEGIN
        INSERT INTO [admin_department_assignments] ([Id], [AdminUserId], [DepartmentId], [AssignedAt], [RemovedAt], [CreatedAt], [UpdatedAt])
        VALUES (NEWID(), @CoreAdminUserId, @CoreAdminDepartmentId, @Now, NULL, @Now, NULL);
    END
END

/* 5) Baseline report definitions + role assignments */
-- Normalize legacy report keys from older seed scripts to current underscore keys.
UPDATE [report_definitions]
SET [Key] = N'academic_transcript', [UpdatedAt] = @Now
WHERE [Key] = N'academic-transcript'
    AND NOT EXISTS (SELECT 1 FROM [report_definitions] x WHERE x.[Key] = N'academic_transcript');

UPDATE [report_definitions]
SET [Key] = N'attendance_summary', [UpdatedAt] = @Now
WHERE [Key] = N'attendance-summary'
    AND NOT EXISTS (SELECT 1 FROM [report_definitions] x WHERE x.[Key] = N'attendance_summary');

UPDATE [report_definitions]
SET [Key] = N'result_summary', [UpdatedAt] = @Now
WHERE [Key] = N'result-sheet'
    AND NOT EXISTS (SELECT 1 FROM [report_definitions] x WHERE x.[Key] = N'result_summary');

DECLARE @Reports TABLE ([Key] NVARCHAR(100), [Name] NVARCHAR(150), [Purpose] NVARCHAR(500));
INSERT INTO @Reports ([Key], [Name], [Purpose]) VALUES
(N'attendance_summary', N'Attendance Summary', N'Per-student attendance percentage per course offering, filterable by semester and department.'),
(N'result_summary', N'Result Summary', N'All published result entries with marks and percentage, filterable by semester, offering, or student.'),
(N'gpa_report', N'GPA & CGPA Report', N'Per-student current semester GPA and cumulative CGPA, filterable by department and program.'),
(N'enrollment_summary', N'Enrollment Summary', N'Course offering seat utilisation showing enrolled count versus maximum capacity.'),
(N'semester_results', N'Semester Results', N'Full published result set for a selected semester with optional department filter.'),
(N'student_transcript', N'Student Transcript', N'Full academic record for a selected student including all result components.'),
(N'low_attendance_warning', N'Low Attendance Warning', N'Students whose attendance falls below a configurable threshold.'),
(N'fyp_status', N'FYP Status Report', N'Final Year Project status overview filterable by department and project status.'),
(N'payment_summary', N'Payment Summary', N'Payment receipt summary for finance workflows with campus, department, course, class, semester, year, and month filters.');

INSERT INTO [report_definitions] ([Id], [Name], [Purpose], [Key], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT NEWID(), r.[Name], r.[Purpose], r.[Key], 1, @Now, NULL, 0, NULL
FROM @Reports r
WHERE NOT EXISTS (SELECT 1 FROM [report_definitions] d WHERE d.[Key] = r.[Key]);

INSERT INTO [report_role_assignments] ([Id], [ReportDefinitionId], [RoleName], [CreatedAt], [UpdatedAt])
SELECT NEWID(), d.[Id], rr.[RoleName], @Now, NULL
FROM [report_definitions] d
CROSS APPLY (VALUES (N'SuperAdmin'), (N'Admin'), (N'Faculty')) rr([RoleName])
WHERE d.[Key] IN (
        N'attendance_summary',
        N'result_summary',
        N'gpa_report',
        N'enrollment_summary',
        N'semester_results',
        N'student_transcript',
        N'low_attendance_warning',
        N'fyp_status')
  AND NOT EXISTS (
      SELECT 1
      FROM [report_role_assignments] x
      WHERE x.[ReportDefinitionId] = d.[Id] AND x.[RoleName] = rr.[RoleName]
  );

INSERT INTO [report_role_assignments] ([Id], [ReportDefinitionId], [RoleName], [CreatedAt], [UpdatedAt])
SELECT NEWID(), d.[Id], rr.[RoleName], @Now, NULL
FROM [report_definitions] d
CROSS APPLY (VALUES (N'SuperAdmin'), (N'Admin'), (N'Finance')) rr([RoleName])
WHERE d.[Key] = N'payment_summary'
    AND NOT EXISTS (
            SELECT 1
            FROM [report_role_assignments] x
            WHERE x.[ReportDefinitionId] = d.[Id] AND x.[RoleName] = rr.[RoleName]
    );

INSERT INTO [report_role_assignments] ([Id], [ReportDefinitionId], [RoleName], [CreatedAt], [UpdatedAt])
SELECT NEWID(), d.[Id], N'Student', @Now, NULL
FROM [report_definitions] d
WHERE d.[Key] = N'student_transcript'
    AND NOT EXISTS (
            SELECT 1
            FROM [report_role_assignments] x
            WHERE x.[ReportDefinitionId] = d.[Id] AND x.[RoleName] = N'Student'
    );

/* 6) Baseline sidebar menus */
DECLARE @DashboardId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'dashboard');
IF @DashboardId IS NULL
BEGIN
    SET @DashboardId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@DashboardId, N'Dashboard', N'Main landing page', N'dashboard', NULL, 1, 1, 1, @Now, NULL, 0, NULL);
END

DECLARE @AcademicId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'academic');
IF @AcademicId IS NULL
BEGIN
    SET @AcademicId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@AcademicId, N'Academic', N'Core academic operations', N'academic', NULL, 2, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @CoursesMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'courses');
IF @CoursesMenuId IS NULL
BEGIN
    SET @CoursesMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CoursesMenuId, N'Courses', N'Course catalog and offerings', N'courses', @AcademicId, 1, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @AttendanceMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'attendance');
IF @AttendanceMenuId IS NULL
BEGIN
    SET @AttendanceMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@AttendanceMenuId, N'Attendance', N'Attendance marking and views', N'attendance', @AcademicId, 2, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @ProgramsMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'programs');
IF @ProgramsMenuId IS NULL
BEGIN
    SET @ProgramsMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@ProgramsMenuId, N'Programs', N'Program catalog and semester structure management', N'programs', @AcademicId, 3, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @GenerateCertificatesMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'generate_certificates');
IF @GenerateCertificatesMenuId IS NULL
BEGIN
    SET @GenerateCertificatesMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@GenerateCertificatesMenuId, N'Generate Certificates', N'University degree and transcript workflow plus non-university additional certificates.', N'generate_certificates', @AcademicId, 4, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @CourseMaterialMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'course_material');
IF @CourseMaterialMenuId IS NULL
BEGIN
    SET @CourseMaterialMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CourseMaterialMenuId, N'Course Material', N'Course material management and learner consumption surfaces.', N'course_material', @AcademicId, 5, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @SettingsMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'settings');
IF @SettingsMenuId IS NULL
BEGIN
    SET @SettingsMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@SettingsMenuId, N'Settings', N'Platform configuration and governance', N'settings', NULL, 3, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @AdminUsersMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'admin_users');
IF @AdminUsersMenuId IS NULL
BEGIN
    SET @AdminUsersMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@AdminUsersMenuId, N'Admin Users', N'Administrative user lifecycle management', N'admin_users', @SettingsMenuId, 1, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @TenantManagementMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'tenant_management');
IF @TenantManagementMenuId IS NULL
BEGIN
    SET @TenantManagementMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@TenantManagementMenuId, N'Tenant Management', N'Tenant registry and lifecycle operations', N'tenant_management', @SettingsMenuId, 2, 1, 0, @Now, NULL, 0, NULL);
END

DECLARE @CampusManagementMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'campus_management');
IF @CampusManagementMenuId IS NULL
BEGIN
    SET @CampusManagementMenuId = NEWID();
    INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CampusManagementMenuId, N'Campus Management', N'Campus registry and campus-level governance', N'campus_management', @SettingsMenuId, 3, 1, 0, @Now, NULL, 0, NULL);
END

INSERT INTO [sidebar_menu_role_accesses] ([Id], [SidebarMenuItemId], [RoleName], [IsAllowed], [CreatedAt], [UpdatedAt])
SELECT NEWID(), m.[Id], ra.[RoleName], ra.[IsAllowed], @Now, NULL
FROM [sidebar_menu_items] m
JOIN (VALUES
    (N'dashboard', N'SuperAdmin', 1),
    (N'dashboard', N'Admin', 1),
    (N'dashboard', N'Faculty', 1),
    (N'dashboard', N'Student', 1),
    (N'academic', N'SuperAdmin', 1),
    (N'academic', N'Admin', 1),
    (N'courses', N'SuperAdmin', 1),
    (N'courses', N'Admin', 1),
    (N'courses', N'Faculty', 1),
    (N'programs', N'SuperAdmin', 1),
    (N'programs', N'Admin', 1),
    (N'programs', N'Faculty', 1),
    (N'generate_certificates', N'SuperAdmin', 1),
    (N'generate_certificates', N'Admin', 1),
    (N'generate_certificates', N'Faculty', 1),
    (N'generate_certificates', N'Student', 1),
    (N'course_material', N'SuperAdmin', 1),
    (N'course_material', N'Admin', 1),
    (N'course_material', N'Faculty', 1),
    (N'course_material', N'Student', 1),
    (N'attendance', N'SuperAdmin', 1),
    (N'attendance', N'Admin', 1),
    (N'attendance', N'Faculty', 1),
    (N'attendance', N'Student', 1),
    (N'settings', N'SuperAdmin', 1),
    (N'settings', N'Admin', 1),
    (N'admin_users', N'SuperAdmin', 1),
    (N'admin_users', N'Admin', 1),
    (N'tenant_management', N'SuperAdmin', 1),
    (N'campus_management', N'SuperAdmin', 1),
    (N'campus_management', N'Admin', 1)
) ra([MenuKey], [RoleName], [IsAllowed]) ON ra.[MenuKey] = m.[Key]
WHERE NOT EXISTS (
    SELECT 1
    FROM [sidebar_menu_role_accesses] x
    WHERE x.[SidebarMenuItemId] = m.[Id] AND x.[RoleName] = ra.[RoleName]
);

/* 7) License baseline state */
IF OBJECT_ID(N'[license_state]') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [license_state])
BEGIN
    INSERT INTO [license_state]
        ([Id], [LicenseHash], [LicenseType], [Status], [ActivatedAt], [ExpiresAt], [CreatedAt], [UpdatedAt])
    VALUES
        ('0f0f0f0f-0f0f-0f0f-0f0f-0f0f0f0f0f01',
         N'SEED-LICENSE-HASH-CHANGE-IN-PROD',
         N'Education',
         N'Active',
         @Now,
         DATEADD(year, 1, @Now),
         @Now,
         NULL);
END

IF OBJECT_ID(N'[license_state]') IS NOT NULL
BEGIN
    IF COL_LENGTH('license_state', 'ActivatedDomain') IS NOT NULL
    BEGIN
        UPDATE [license_state]
        SET [ActivatedDomain] = COALESCE([ActivatedDomain], N'tabsan.local')
        WHERE [ActivatedDomain] IS NULL;
    END

    IF COL_LENGTH('license_state', 'MaxUsers') IS NOT NULL
    BEGIN
        UPDATE [license_state]
        SET [MaxUsers] = CASE WHEN [MaxUsers] < 100000 THEN 100000 ELSE [MaxUsers] END;
    END
END

/* 8) Module role access matrix */
IF OBJECT_ID(N'[module_role_assignments]') IS NOT NULL
BEGIN
    DECLARE @ModuleRoleMatrix TABLE ([ModuleKey] NVARCHAR(50), [RoleName] NVARCHAR(50));
    INSERT INTO @ModuleRoleMatrix ([ModuleKey], [RoleName]) VALUES
    (N'authentication', N'SuperAdmin'),
    (N'authentication', N'Admin'),
    (N'authentication', N'Faculty'),
    (N'authentication', N'Student'),
    (N'departments', N'SuperAdmin'),
    (N'departments', N'Admin'),
    (N'sis', N'SuperAdmin'),
    (N'sis', N'Admin'),
    (N'sis', N'Faculty'),
    (N'courses', N'SuperAdmin'),
    (N'courses', N'Admin'),
    (N'courses', N'Faculty'),
    (N'courses', N'Student'),
    (N'assignments', N'SuperAdmin'),
    (N'assignments', N'Admin'),
    (N'assignments', N'Faculty'),
    (N'assignments', N'Student'),
    (N'quizzes', N'SuperAdmin'),
    (N'quizzes', N'Admin'),
    (N'quizzes', N'Faculty'),
    (N'quizzes', N'Student'),
    (N'attendance', N'SuperAdmin'),
    (N'attendance', N'Admin'),
    (N'attendance', N'Faculty'),
    (N'attendance', N'Student'),
    (N'results', N'SuperAdmin'),
    (N'results', N'Admin'),
    (N'results', N'Faculty'),
    (N'results', N'Student'),
    (N'notifications', N'SuperAdmin'),
    (N'notifications', N'Admin'),
    (N'notifications', N'Faculty'),
    (N'notifications', N'Student'),
    (N'fyp', N'SuperAdmin'),
    (N'fyp', N'Admin'),
    (N'fyp', N'Faculty'),
    (N'fyp', N'Student'),
    (N'ai_chat', N'SuperAdmin'),
    (N'ai_chat', N'Admin'),
    (N'ai_chat', N'Faculty'),
    (N'ai_chat', N'Student'),
    (N'reports', N'SuperAdmin'),
    (N'reports', N'Admin'),
    (N'reports', N'Faculty'),
    (N'reports', N'Student'),
    (N'themes', N'SuperAdmin'),
    (N'themes', N'Admin'),
    (N'themes', N'Faculty'),
    (N'themes', N'Student'),
    (N'advanced_audit', N'SuperAdmin'),
    (N'advanced_audit', N'Admin');

    INSERT INTO [module_role_assignments] ([Id], [ModuleId], [RoleName], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), m.[Id], matrix.[RoleName], @Now, NULL
    FROM @ModuleRoleMatrix matrix
    INNER JOIN [modules] m ON m.[Key] = matrix.[ModuleKey]
    WHERE NOT EXISTS (
        SELECT 1
        FROM [module_role_assignments] x
        WHERE x.[ModuleId] = m.[Id] AND x.[RoleName] = matrix.[RoleName]
    );
END

COMMIT TRANSACTION;
PRINT 'Core seed data completed successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
