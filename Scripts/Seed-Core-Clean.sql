SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*
  Clean core seed for Tabsan EduSphere.

  Purpose:
  - Seed ONLY essential startup data (no demo/dummy rows)
  - Add baseline roles, institution-type departments, one SuperAdmin user,
    modules, functionality access, and permissions matrix.

  Execution order:
  1) 01-Schema-Current.sql
  2) Seed-Core-Clean.sql (this script)

  SuperAdmin login:
  - Username: superadmin
    - Password: EduSphere147
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
    RAISERROR('Failed to switch context to [Tabsan-EduSphere]. Aborting clean seed script.', 16, 1);
    RETURN;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'[roles]') IS NULL OR OBJECT_ID(N'[users]') IS NULL OR OBJECT_ID(N'[modules]') IS NULL OR OBJECT_ID(N'[module_status]') IS NULL OR OBJECT_ID(N'[departments]') IS NULL
BEGIN
    RAISERROR('Required core tables are missing. Run 01-Schema-Current.sql first.', 16, 1);
END;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @SuperAdminPasswordHash NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
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

/* 1) Roles (all baseline roles used by the platform) */
MERGE INTO [roles] AS tgt
USING (
    SELECT N'SuperAdmin' AS [Name], N'Full platform access.' AS [Description], CAST(1 AS bit) AS [IsSystemRole]
    UNION ALL SELECT N'Admin', N'Institution administrator access.', CAST(1 AS bit)
    UNION ALL SELECT N'Faculty', N'Faculty/staff teaching and grading access.', CAST(1 AS bit)
    UNION ALL SELECT N'Student', N'Student learning access.', CAST(1 AS bit)
    UNION ALL SELECT N'Finance', N'Finance and fee workflow access.', CAST(1 AS bit)
    UNION ALL SELECT N'Parent', N'Parent portal access.', CAST(1 AS bit)
) AS src
ON tgt.[Name] = src.[Name]
WHEN MATCHED THEN
    UPDATE SET tgt.[Description] = src.[Description], tgt.[IsSystemRole] = src.[IsSystemRole]
WHEN NOT MATCHED THEN
    INSERT ([Name], [Description], [IsSystemRole])
    VALUES (src.[Name], src.[Description], src.[IsSystemRole]);

/* 2) Institution type baseline (School=0, College=1, University=2) */
DECLARE @CoreDepartments TABLE ([Id] UNIQUEIDENTIFIER, [Name] NVARCHAR(200), [Code] NVARCHAR(20), [InstitutionType] INT);
INSERT INTO @CoreDepartments ([Id], [Name], [Code], [InstitutionType]) VALUES
(CAST('21000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'Core School Department', N'CORE-SCH', 0),
(CAST('21000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'Core College Department', N'CORE-COL', 1),
(CAST('21000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'Core University Department', N'CORE-UNI', 2);

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

/* 3) SuperAdmin user only */
DECLARE @SuperAdminRoleId INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'SuperAdmin');
DECLARE @SuperAdminUserId UNIQUEIDENTIFIER = CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER);

IF @SuperAdminRoleId IS NULL
BEGIN
    RAISERROR('Role [SuperAdmin] was not created. Aborting.', 16, 1);
END;

IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id] = @SuperAdminUserId)
BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@SuperAdminUserId, N'superadmin', N'superadmin@tabsan.local', @SuperAdminPasswordHash, @SuperAdminRoleId, NULL, NULL, 1, NULL, @Now, NULL, 0, NULL);
END;

UPDATE [users]
SET [Username] = N'superadmin',
    [Email] = N'superadmin@tabsan.local',
    [PasswordHash] = @SuperAdminPasswordHash,
    [RoleId] = @SuperAdminRoleId,
    [DepartmentId] = NULL,
    [InstitutionType] = NULL,
    [IsActive] = 1,
    [IsDeleted] = 0,
    [DeletedAt] = NULL,
    [UpdatedAt] = @Now
WHERE [Id] = @SuperAdminUserId;

IF COL_LENGTH('users', 'TenantId') IS NOT NULL AND COL_LENGTH('users', 'CampusId') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [TenantId] = NULL,
        [CampusId] = NULL,
        [UpdatedAt] = @Now
    WHERE [Id] = @SuperAdminUserId;
END;

IF COL_LENGTH('users', 'PhoneNumber') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [PhoneNumber] = N'+61490000001',
        [UpdatedAt] = @Now
    WHERE [Id] = @SuperAdminUserId;
END;

IF OBJECT_ID(N'[password_history]') IS NOT NULL
BEGIN
    INSERT INTO [password_history] ([Id], [UserId], [PasswordHash], [CreatedAt])
    SELECT NEWID(), @SuperAdminUserId, @SuperAdminPasswordHash, @Now
    WHERE NOT EXISTS (
        SELECT 1
        FROM [password_history] x
        WHERE x.[UserId] = @SuperAdminUserId
          AND x.[PasswordHash] = @SuperAdminPasswordHash
    );
END;

/* 4) Modules (normalized to current key format used by ModuleRegistry) */
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
(N'courses', N'Courses', 1),
(N'sis', N'Student Information', 1),
(N'assignments', N'Assignments', 0),
(N'attendance', N'Attendance', 0),
(N'results', N'Results / Grades', 0),
(N'quizzes', N'Quizzes', 0),
(N'fyp', N'Final Year Projects', 0),
(N'notifications', N'Notifications', 0),
(N'ai_chat', N'AI Chatbot', 0),
(N'reports', N'Reports', 0),
(N'themes', N'UI Themes', 0),
(N'advanced_audit', N'Advanced Audit Logging', 0);

MERGE INTO [modules] AS tgt
USING @Modules AS src
ON tgt.[Key] = src.[Key]
WHEN MATCHED THEN
    UPDATE SET tgt.[Name] = src.[Name], tgt.[IsMandatory] = src.[IsMandatory], tgt.[UpdatedAt] = @Now
WHEN NOT MATCHED THEN
    INSERT ([Id], [Key], [Name], [IsMandatory], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), src.[Key], src.[Name], src.[IsMandatory], @Now, NULL);

MERGE INTO [module_status] AS tgt
USING (
    SELECT m.[Id] AS [ModuleId],
           CASE WHEN m.[IsMandatory] = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS [IsActive],
           CASE WHEN m.[IsMandatory] = 1 THEN N'mandatory' ELSE N'seed' END AS [Source]
    FROM [modules] m
) AS src
ON tgt.[ModuleId] = src.[ModuleId]
WHEN MATCHED THEN
    UPDATE SET tgt.[IsActive] = src.[IsActive],
               tgt.[ActivatedAt] = CASE WHEN src.[IsActive] = 1 THEN COALESCE(tgt.[ActivatedAt], @Now) ELSE NULL END,
               tgt.[Source] = src.[Source],
               tgt.[UpdatedAt] = @Now
WHEN NOT MATCHED THEN
    INSERT ([Id], [ModuleId], [IsActive], [ActivatedAt], [Source], [ChangedBy], [CreatedAt], [UpdatedAt])
    VALUES (NEWID(), src.[ModuleId], src.[IsActive], CASE WHEN src.[IsActive] = 1 THEN @Now ELSE NULL END, src.[Source], NULL, @Now, NULL);

/* 5) Module permissions matrix */
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

    (N'courses', N'SuperAdmin'),
    (N'courses', N'Admin'),
    (N'courses', N'Faculty'),
    (N'courses', N'Student'),

    (N'sis', N'SuperAdmin'),
    (N'sis', N'Admin'),
    (N'sis', N'Faculty'),
    (N'sis', N'Student'),

    (N'assignments', N'SuperAdmin'),
    (N'assignments', N'Admin'),
    (N'assignments', N'Faculty'),
    (N'assignments', N'Student'),

    (N'attendance', N'SuperAdmin'),
    (N'attendance', N'Admin'),
    (N'attendance', N'Faculty'),
    (N'attendance', N'Student'),

    (N'results', N'SuperAdmin'),
    (N'results', N'Admin'),
    (N'results', N'Faculty'),
    (N'results', N'Student'),

    (N'quizzes', N'SuperAdmin'),
    (N'quizzes', N'Admin'),
    (N'quizzes', N'Faculty'),
    (N'quizzes', N'Student'),

    (N'fyp', N'SuperAdmin'),
    (N'fyp', N'Admin'),
    (N'fyp', N'Faculty'),
    (N'fyp', N'Student'),

    (N'notifications', N'SuperAdmin'),
    (N'notifications', N'Admin'),
    (N'notifications', N'Faculty'),
    (N'notifications', N'Student'),

    (N'ai_chat', N'SuperAdmin'),
    (N'ai_chat', N'Admin'),
    (N'ai_chat', N'Faculty'),
    (N'ai_chat', N'Student'),

    (N'reports', N'SuperAdmin'),
    (N'reports', N'Admin'),
    (N'reports', N'Faculty'),

    (N'themes', N'SuperAdmin'),
    (N'themes', N'Admin'),

    (N'advanced_audit', N'SuperAdmin');

    INSERT INTO [module_role_assignments] ([Id], [ModuleId], [RoleName], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), m.[Id], map.[RoleName], @Now, NULL
    FROM @ModuleRoleMatrix map
    INNER JOIN [modules] m ON m.[Key] = map.[ModuleKey]
    WHERE NOT EXISTS (
        SELECT 1
        FROM [module_role_assignments] x
        WHERE x.[ModuleId] = m.[Id]
          AND x.[RoleName] = map.[RoleName]
    );
END;

/* 6) Portal and institution-type settings */
IF OBJECT_ID(N'[portal_settings]') IS NOT NULL
BEGIN
    MERGE INTO [portal_settings] AS tgt
    USING (
        SELECT N'portal.universityName' AS [Key], N'Tabsan EduSphere' AS [Value]
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
END;

/* 7) Functionality baseline (reports + sidebar access permissions) */
IF OBJECT_ID(N'[report_definitions]') IS NOT NULL
BEGIN
    DECLARE @Reports TABLE ([Key] NVARCHAR(100), [Name] NVARCHAR(150), [Purpose] NVARCHAR(500));
    INSERT INTO @Reports ([Key], [Name], [Purpose]) VALUES
    (N'attendance_summary', N'Attendance Summary', N'Attendance metrics by student and class.'),
    (N'result_summary', N'Result Summary', N'Published results and grade summaries.'),
    (N'gpa_report', N'GPA & CGPA Report', N'GPA and CGPA analytics by cohort.'),
    (N'student_transcript', N'Student Transcript', N'Complete academic transcript per student.'),
    (N'payment_summary', N'Payment Summary', N'Payment receipt summary for finance workflows.');

    INSERT INTO [report_definitions] ([Id], [Name], [Purpose], [Key], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT NEWID(), r.[Name], r.[Purpose], r.[Key], 1, @Now, NULL, 0, NULL
    FROM @Reports r
    WHERE NOT EXISTS (SELECT 1 FROM [report_definitions] x WHERE x.[Key] = r.[Key]);

    IF OBJECT_ID(N'[report_role_assignments]') IS NOT NULL
    BEGIN
        INSERT INTO [report_role_assignments] ([Id], [ReportDefinitionId], [RoleName], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), d.[Id], rr.[RoleName], @Now, NULL
        FROM [report_definitions] d
        CROSS APPLY (VALUES (N'SuperAdmin'), (N'Admin'), (N'Faculty')) rr([RoleName])
        WHERE d.[Key] IN (N'attendance_summary', N'result_summary', N'gpa_report', N'student_transcript')
          AND NOT EXISTS (
              SELECT 1
              FROM [report_role_assignments] x
              WHERE x.[ReportDefinitionId] = d.[Id]
                AND x.[RoleName] = rr.[RoleName]
          );

        INSERT INTO [report_role_assignments] ([Id], [ReportDefinitionId], [RoleName], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), d.[Id], N'Student', @Now, NULL
        FROM [report_definitions] d
        WHERE d.[Key] = N'student_transcript'
          AND NOT EXISTS (
              SELECT 1
              FROM [report_role_assignments] x
              WHERE x.[ReportDefinitionId] = d.[Id]
                AND x.[RoleName] = N'Student'
          );

                INSERT INTO [report_role_assignments] ([Id], [ReportDefinitionId], [RoleName], [CreatedAt], [UpdatedAt])
                SELECT NEWID(), d.[Id], rr.[RoleName], @Now, NULL
                FROM [report_definitions] d
                CROSS APPLY (VALUES (N'SuperAdmin'), (N'Admin'), (N'Finance')) rr([RoleName])
                WHERE d.[Key] = N'payment_summary'
                    AND NOT EXISTS (
                            SELECT 1
                            FROM [report_role_assignments] x
                            WHERE x.[ReportDefinitionId] = d.[Id]
                                AND x.[RoleName] = rr.[RoleName]
                    );
    END;
END;

IF OBJECT_ID(N'[sidebar_menu_items]') IS NOT NULL
BEGIN
    DECLARE @DashboardId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'dashboard');
    IF @DashboardId IS NULL
    BEGIN
        SET @DashboardId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@DashboardId, N'Dashboard', N'Main landing page', N'dashboard', NULL, 1, 1, 1, @Now, NULL, 0, NULL);
    END;

    DECLARE @AcademicId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'academic');
    IF @AcademicId IS NULL
    BEGIN
        SET @AcademicId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@AcademicId, N'Academic', N'Core academic operations', N'academic', NULL, 2, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @CoursesMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'courses');
    IF @CoursesMenuId IS NULL
    BEGIN
        SET @CoursesMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@CoursesMenuId, N'Courses', N'Course catalog and offerings', N'courses', @AcademicId, 1, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @AttendanceMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'attendance');
    IF @AttendanceMenuId IS NULL
    BEGIN
        SET @AttendanceMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@AttendanceMenuId, N'Attendance', N'Attendance marking and views', N'attendance', @AcademicId, 2, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @ProgramsMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'programs');
    IF @ProgramsMenuId IS NULL
    BEGIN
        SET @ProgramsMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@ProgramsMenuId, N'Programs', N'Program catalog and semester structure management', N'programs', @AcademicId, 3, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @GenerateCertificatesMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'generate_certificates');
    IF @GenerateCertificatesMenuId IS NULL
    BEGIN
        SET @GenerateCertificatesMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@GenerateCertificatesMenuId, N'Generate Certificates', N'University degree and transcript workflow plus non-university additional certificates.', N'generate_certificates', @AcademicId, 4, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @CourseMaterialMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'course_material');
    IF @CourseMaterialMenuId IS NULL
    BEGIN
        SET @CourseMaterialMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@CourseMaterialMenuId, N'Course Material', N'Course material management and learner consumption surfaces.', N'course_material', @AcademicId, 5, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @SettingsMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'settings');
    IF @SettingsMenuId IS NULL
    BEGIN
        SET @SettingsMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@SettingsMenuId, N'Settings', N'Platform configuration and governance', N'settings', NULL, 3, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @AdminUsersMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'admin_users');
    IF @AdminUsersMenuId IS NULL
    BEGIN
        SET @AdminUsersMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@AdminUsersMenuId, N'Admin Users', N'Administrative user lifecycle management', N'admin_users', @SettingsMenuId, 1, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @TenantManagementMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'tenant_management');
    IF @TenantManagementMenuId IS NULL
    BEGIN
        SET @TenantManagementMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@TenantManagementMenuId, N'Tenant Management', N'Tenant registry and lifecycle operations', N'tenant_management', @SettingsMenuId, 2, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @CampusManagementMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'campus_management');
    IF @CampusManagementMenuId IS NULL
    BEGIN
        SET @CampusManagementMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@CampusManagementMenuId, N'Campus Management', N'Campus registry and campus-level governance', N'campus_management', @SettingsMenuId, 3, 1, 0, @Now, NULL, 0, NULL);
    END;

    IF OBJECT_ID(N'[sidebar_menu_role_accesses]') IS NOT NULL
    BEGIN
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
            (N'timetable_admin', N'Admin', 1),
            (N'timetable_teacher', N'Admin', 1),
            (N'timetable_teacher', N'Faculty', 1),
            (N'timetable_student', N'Student', 1),
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
            (N'assignments', N'SuperAdmin', 1),
            (N'assignments', N'Admin', 1),
            (N'assignments', N'Faculty', 1),
            (N'assignments', N'Student', 1),
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
            WHERE x.[SidebarMenuItemId] = m.[Id]
              AND x.[RoleName] = ra.[RoleName]
        );
    END;
END;

/* 8) Baseline license state (optional, seeded only when empty) */
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
END;

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
        SET [MaxUsers] = CASE WHEN [MaxUsers] IS NULL OR [MaxUsers] < 100000 THEN 100000 ELSE [MaxUsers] END;
    END

    IF COL_LENGTH('license_state', 'InstitutionScope') IS NOT NULL
    BEGIN
        UPDATE [license_state]
        SET [InstitutionScope] = COALESCE([InstitutionScope], N'University')
        WHERE [InstitutionScope] IS NULL;
    END

    IF COL_LENGTH('license_state', 'ExpiryType') IS NOT NULL
    BEGIN
        UPDATE [license_state]
        SET [ExpiryType] = COALESCE([ExpiryType], N'Yearly')
        WHERE [ExpiryType] IS NULL;
    END
END;

COMMIT TRANSACTION;
PRINT 'Seed-Core-Clean completed successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
