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
DECLARE @SuperAdminPasswordHash NVARCHAR(512) = N'argon2id:kot3aIW+GTcmK4Ji/jGD7BxrNOEh57PLaFMUZrZa5oM=:v+XYusZ0Eu9Xs8Sz/7Hi58z4SrS9KsJ/ynnr/iCkkSk=';
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

IF COL_LENGTH('users', 'FullName') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [FullName] = N'Tabsan Super Admin',
        [UpdatedAt] = @Now
    WHERE [Id] = @SuperAdminUserId;
END;

IF COL_LENGTH('users', 'FatherName') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [FatherName] = N'Founding Father',
        [UpdatedAt] = @Now
    WHERE [Id] = @SuperAdminUserId;
END;

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

    DECLARE @DegreeAuditMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'degree_audit');
    IF @DegreeAuditMenuId IS NULL
    BEGIN
        SET @DegreeAuditMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@DegreeAuditMenuId, N'Degree Audit', N'Evaluate completion progress against degree requirements.', N'degree_audit', @AcademicId, 4, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @GenerateCertificatesMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'generate_certificates');
    IF @GenerateCertificatesMenuId IS NULL
    BEGIN
        SET @GenerateCertificatesMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@GenerateCertificatesMenuId, N'Generate Certificates', N'University degree and transcript workflow plus non-university additional certificates.', N'generate_certificates', @AcademicId, 5, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @CourseMaterialMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'course_material');
    IF @CourseMaterialMenuId IS NULL
    BEGIN
        SET @CourseMaterialMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@CourseMaterialMenuId, N'Course Material', N'Course material management and learner consumption surfaces.', N'course_material', @AcademicId, 6, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @SettingsMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'settings');
    IF @SettingsMenuId IS NULL
    BEGIN
        SET @SettingsMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@SettingsMenuId, N'Settings', N'Platform configuration and governance', N'settings', NULL, 3, 1, 0, @Now, NULL, 0, NULL);
    END;

    DECLARE @UserSettingsMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [sidebar_menu_items] WHERE [Key] = N'user_settings');
    IF @UserSettingsMenuId IS NULL
    BEGIN
        SET @UserSettingsMenuId = NEWID();
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (@UserSettingsMenuId, N'User Settings', N'Update personal details and account password', N'user_settings', @SettingsMenuId, 1, 1, 0, @Now, NULL, 0, NULL);
    END

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
            (N'degree_audit', N'SuperAdmin', 1),
            (N'degree_audit', N'Admin', 1),
            (N'degree_audit', N'Faculty', 1),
            (N'degree_audit', N'Student', 1),
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

-- ==============================================================================
-- APPENDIX: Sidebar menu full role-access matrix + action permissions
-- ==============================================================================

/* ---- A1. Ensure all sidebar menu items exist ---- */
DECLARE @items TABLE (MenuKey NVARCHAR(100), DisplayName NVARCHAR(150), Purpose NVARCHAR(500), ParentKey NVARCHAR(100) NULL, DisplayOrder INT);
INSERT INTO @items VALUES
(N'timetable_admin',N'Timetable Admin',N'Create, edit, publish and retire timetables.',NULL,1),(N'timetable_teacher',N'Teacher Timetable',N'Faculty-facing teaching schedule with room, time and section context.',NULL,2),(N'timetable_student',N'Student Timetable',N'Student class schedule by active program, semester and enrollment.',NULL,3),(N'students',N'Students',N'Manage student profile lifecycle, status and related records.',NULL,4),(N'departments',N'Departments',N'Manage department masters and scoped ownership metadata.',NULL,5),(N'enrollments',N'Enrollments',N'Manage roster enrollment, drops and status updates by offering.',NULL,6),(N'enter_attendance',N'Enter Attendance',N'Faculty workflow for manual attendance and CSV import with validation feedback.',NULL,7),(N'enter_results',N'Enter Results',N'Faculty workflow for scoped result entry, correction, import and publish actions.',NULL,8),(N'results',N'Results',N'Review and consume published results with transcript-related visibility.',NULL,9),(N'quizzes',N'Quizzes',N'Create, attempt and evaluate quiz activities.',NULL,10),(N'analytics',N'Analytics',N'View performance, attendance and finance trends with filters.',NULL,11),(N'student_lifecycle',N'Student Lifecycle',N'Manage progression, graduation activation and student-state transitions.',NULL,12),(N'payments',N'Payments',N'Track, edit and confirm payment receipt lifecycle.',NULL,13),(N'report_center',N'Report Center',N'Run, filter and export assigned reports by role and scope.',NULL,14),(N'notifications',N'Notifications',N'Unified inbox for system, academic and workflow notifications.',NULL,15),(N'buildings',N'Buildings',N'Manage building master records for campus infrastructure.',NULL,16),(N'rooms',N'Rooms',N'Manage room inventory, capacity and usage metadata by building.',NULL,17),(N'helpdesk',N'Helpdesk',N'Ticket management for academic, technical and administrative support.',NULL,18),(N'programs',N'Programs',N'Manage academic programs, activation and scope metadata.',N'academic',2),(N'gradebook',N'Gradebook',N'Manage component scores and aggregate performance by offering.',N'academic',5),(N'rubric_manage',N'Rubric Management',N'Create and manage assignment rubrics, criteria and level scoring matrices.',N'academic',6),(N'lms_manage',N'LMS Manage',N'Manage LMS integration and learning content governance settings.',N'academic',10),(N'discussion',N'Discussion',N'Threaded discussions for course collaboration and Q&A.',N'academic',11),(N'announcements',N'Announcements',N'Create and consume scoped institutional and course announcements.',N'academic',12),(N'study_plan',N'Study Plan',N'Plan future course path with advisor review workflow.',N'academic',13),(N'prerequisites',N'Prerequisites',N'Define prerequisite relationships for course eligibility.',N'academic',14),(N'grading_config',N'Grading Config',N'Maintain grading profiles, pass thresholds and ranges.',N'academic',15),(N'result_calculation',N'Result Calculation',N'Configure institution-scoped GPA rules and weighted components.',N'academic',16),(N'accreditation',N'Accreditation',N'Manage accreditation templates, mappings and evidence structures.',N'academic',17),(N'user_import',N'User Import',N'Bulk onboarding with validation, mapping and scoped controls.',N'academic',18),(N'theme_settings',N'Theme Settings',N'Manage visual theme and appearance preferences.',N'settings',5),(N'report_settings',N'Report Settings',N'Configure report definitions, visibility and access rules.',N'settings',6);

DECLARE @ik NVARCHAR(100), @in NVARCHAR(150), @ip NVARCHAR(500), @ipk NVARCHAR(100), @io INT;
DECLARE ic CURSOR LOCAL FAST_FORWARD FOR SELECT MenuKey, DisplayName, Purpose, ParentKey, DisplayOrder FROM @items;
OPEN ic; FETCH NEXT FROM ic INTO @ik, @in, @ip, @ipk, @io;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [sidebar_menu_items] WHERE [Key] = @ik)
    BEGIN
        DECLARE @pid UNIQUEIDENTIFIER = NULL;
        IF @ipk IS NOT NULL SELECT @pid = [Id] FROM [sidebar_menu_items] WHERE [Key] = @ipk;
        INSERT INTO [sidebar_menu_items] ([Id], [Name], [Purpose], [Key], [ParentId], [DisplayOrder], [IsActive], [IsSystemMenu], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES (NEWID(), @in, @ip, @ik, @pid, @io, 1, 0, @Now, NULL, 0, NULL);
    END
    FETCH NEXT FROM ic INTO @ik, @in, @ip, @ipk, @io;
END;
CLOSE ic; DEALLOCATE ic;

/* ---- A2. Full sidebar role-access matrix ---- */
DELETE FROM [sidebar_menu_role_accesses] WHERE [RoleName] IN (N'Admin', N'Faculty', N'Student', N'Finance');

INSERT INTO [sidebar_menu_role_accesses] ([Id], [SidebarMenuItemId], [RoleName], [IsAllowed], [CreatedAt], [UpdatedAt])
SELECT NEWID(), m.[Id], ra.[RoleName], 1, @Now, NULL FROM [sidebar_menu_items] m JOIN (VALUES
(N'dashboard',N'Admin'),(N'dashboard',N'Faculty'),(N'dashboard',N'Student'),(N'dashboard',N'Finance'),
(N'tenant_management',N'Admin'),(N'campus_management',N'Admin'),(N'departments',N'Admin'),(N'programs',N'Admin'),
(N'courses',N'Admin'),(N'enrollments',N'Admin'),(N'students',N'Admin'),(N'timetable_admin',N'Admin'),
(N'timetable_teacher',N'Admin'),(N'timetable_student',N'Admin'),(N'assignments',N'Admin'),(N'enter_attendance',N'Admin'),
(N'enter_results',N'Admin'),(N'gradebook',N'Admin'),(N'rubric_manage',N'Admin'),(N'quizzes',N'Admin'),
(N'lms_manage',N'Admin'),(N'course_material',N'Admin'),(N'discussion',N'Admin'),(N'announcements',N'Admin'),
(N'generate_certificates',N'Admin'),(N'result_calculation',N'Admin'),(N'prerequisites',N'Admin'),(N'grading_config',N'Admin'),
(N'study_plan',N'Admin'),(N'attendance',N'Admin'),(N'results',N'Admin'),(N'student_lifecycle',N'Admin'),
(N'buildings',N'Admin'),(N'rooms',N'Admin'),(N'report_settings',N'Admin'),(N'theme_settings',N'Admin'),
(N'accreditation',N'Admin'),(N'notifications',N'Admin'),(N'user_import',N'Admin'),(N'user_settings',N'Admin'),
(N'analytics',N'Admin'),(N'helpdesk',N'Admin'),(N'report_center',N'Admin'),(N'admin_users',N'Admin'),(N'payments',N'Admin'),
(N'students',N'Faculty'),(N'timetable_teacher',N'Faculty'),(N'assignments',N'Faculty'),(N'enter_attendance',N'Faculty'),
(N'enter_results',N'Faculty'),(N'gradebook',N'Faculty'),(N'quizzes',N'Faculty'),(N'lms_manage',N'Faculty'),
(N'course_material',N'Faculty'),(N'discussion',N'Faculty'),(N'announcements',N'Faculty'),(N'result_calculation',N'Faculty'),
(N'study_plan',N'Faculty'),(N'attendance',N'Faculty'),(N'timetable_student',N'Faculty'),(N'results',N'Faculty'),
(N'theme_settings',N'Faculty'),(N'notifications',N'Faculty'),(N'user_settings',N'Faculty'),(N'analytics',N'Faculty'),
(N'helpdesk',N'Faculty'),(N'report_center',N'Faculty'),
(N'quizzes',N'Student'),(N'lms_manage',N'Student'),(N'course_material',N'Student'),(N'discussion',N'Student'),
(N'announcements',N'Student'),(N'study_plan',N'Student'),(N'attendance',N'Student'),(N'timetable_student',N'Student'),
(N'results',N'Student'),(N'theme_settings',N'Student'),(N'notifications',N'Student'),(N'user_settings',N'Student'),
(N'analytics',N'Student'),(N'helpdesk',N'Student'),(N'report_center',N'Student'),
(N'payments',N'Finance'),(N'theme_settings',N'Finance'),(N'notifications',N'Finance'),(N'user_settings',N'Finance'),
(N'analytics',N'Finance'),(N'helpdesk',N'Finance'),(N'report_center',N'Finance')
) ra([MenuKey], [RoleName]) ON ra.[MenuKey] = m.[Key]
WHERE NOT EXISTS (SELECT 1 FROM [sidebar_menu_role_accesses] x WHERE x.[SidebarMenuItemId] = m.[Id] AND x.[RoleName] = ra.[RoleName]);

/* ---- A3. Full role-resource action permissions ---- */
IF OBJECT_ID(N'[role_resource_permissions]') IS NOT NULL
BEGIN
    DELETE FROM [role_resource_permissions] WHERE [RoleName] IN (N'SuperAdmin',N'Admin',N'Faculty',N'Student',N'Finance');

    DECLARE @pm TABLE (MenuKey NVARCHAR(100), RoleName NVARCHAR(100), V BIT, A BIT, E BIT, D BIT, X BIT, I BIT);
    INSERT INTO @pm VALUES (N'dashboard',N'SuperAdmin',1,0,0,0,0,0),(N'timetable_admin',N'SuperAdmin',1,1,1,1,1,1),(N'timetable_teacher',N'SuperAdmin',1,1,1,1,1,1),(N'timetable_student',N'SuperAdmin',1,1,1,1,1,1),(N'students',N'SuperAdmin',1,1,1,1,1,1),(N'departments',N'SuperAdmin',1,1,1,1,1,1),(N'enrollments',N'SuperAdmin',1,1,1,1,1,1),(N'enter_attendance',N'SuperAdmin',1,1,1,1,1,1),(N'enter_results',N'SuperAdmin',1,1,1,1,1,1),(N'results',N'SuperAdmin',1,1,1,1,1,1),(N'quizzes',N'SuperAdmin',1,1,1,1,1,1),(N'analytics',N'SuperAdmin',1,1,1,1,1,1),(N'student_lifecycle',N'SuperAdmin',1,1,1,1,1,1),(N'payments',N'SuperAdmin',1,1,1,1,1,1),(N'report_center',N'SuperAdmin',1,1,1,1,1,1),(N'notifications',N'SuperAdmin',1,1,1,1,1,1),(N'buildings',N'SuperAdmin',1,1,1,1,1,1),(N'rooms',N'SuperAdmin',1,1,1,1,1,1),(N'helpdesk',N'SuperAdmin',1,1,1,1,1,1),(N'courses',N'SuperAdmin',1,1,1,1,1,1),(N'programs',N'SuperAdmin',1,1,1,1,1,1),(N'attendance',N'SuperAdmin',1,1,1,1,1,1),(N'assignments',N'SuperAdmin',1,1,1,1,1,1),(N'gradebook',N'SuperAdmin',1,1,1,1,1,1),(N'rubric_manage',N'SuperAdmin',1,1,1,1,1,1),(N'degree_audit',N'SuperAdmin',1,1,1,1,1,1),(N'generate_certificates',N'SuperAdmin',1,1,1,1,1,1),(N'course_material',N'SuperAdmin',1,1,1,1,1,1),(N'lms_manage',N'SuperAdmin',1,1,1,1,1,1),(N'discussion',N'SuperAdmin',1,1,1,1,1,1),(N'announcements',N'SuperAdmin',1,1,1,1,1,1),(N'study_plan',N'SuperAdmin',1,1,1,1,1,1),(N'prerequisites',N'SuperAdmin',1,1,1,1,1,1),(N'grading_config',N'SuperAdmin',1,1,1,1,1,1),(N'result_calculation',N'SuperAdmin',1,1,1,1,1,1),(N'accreditation',N'SuperAdmin',1,1,1,1,1,1),(N'user_import',N'SuperAdmin',1,1,1,1,1,1),(N'user_settings',N'SuperAdmin',1,1,1,1,1,1),(N'admin_users',N'SuperAdmin',1,1,1,1,1,1),(N'tenant_management',N'SuperAdmin',1,1,1,1,1,1),(N'campus_management',N'SuperAdmin',1,1,1,1,1,1),(N'theme_settings',N'SuperAdmin',1,1,1,1,1,1),(N'report_settings',N'SuperAdmin',1,1,1,1,1,1);
    INSERT INTO @pm VALUES (N'dashboard',N'Admin',1,0,0,0,0,0),(N'timetable_admin',N'Admin',1,1,1,1,1,1),(N'timetable_teacher',N'Admin',1,1,1,1,1,1),(N'timetable_student',N'Admin',1,1,1,1,1,1),(N'students',N'Admin',1,1,1,1,1,1),(N'departments',N'Admin',1,1,1,1,1,1),(N'enrollments',N'Admin',1,1,1,1,1,1),(N'enter_attendance',N'Admin',1,1,1,1,1,1),(N'enter_results',N'Admin',1,1,1,1,1,1),(N'results',N'Admin',1,1,1,1,1,1),(N'quizzes',N'Admin',1,1,1,1,1,1),(N'analytics',N'Admin',1,1,1,1,1,1),(N'student_lifecycle',N'Admin',1,1,1,1,1,1),(N'payments',N'Admin',1,1,1,1,1,1),(N'report_center',N'Admin',1,1,1,1,1,1),(N'notifications',N'Admin',1,1,1,1,1,1),(N'buildings',N'Admin',1,1,1,1,1,1),(N'rooms',N'Admin',1,1,1,1,1,1),(N'helpdesk',N'Admin',1,1,1,1,1,1),(N'courses',N'Admin',1,1,1,1,1,1),(N'programs',N'Admin',1,1,1,1,1,1),(N'attendance',N'Admin',1,1,1,1,1,1),(N'assignments',N'Admin',1,1,1,1,1,1),(N'gradebook',N'Admin',1,1,1,1,1,1),(N'rubric_manage',N'Admin',1,1,1,1,1,1),(N'degree_audit',N'Admin',1,1,1,1,1,1),(N'generate_certificates',N'Admin',1,1,1,1,1,1),(N'course_material',N'Admin',1,1,1,1,1,1),(N'lms_manage',N'Admin',1,1,1,1,1,1),(N'discussion',N'Admin',1,1,1,1,1,1),(N'announcements',N'Admin',1,1,1,1,1,1),(N'study_plan',N'Admin',1,1,1,1,1,1),(N'prerequisites',N'Admin',1,1,1,1,1,1),(N'grading_config',N'Admin',1,1,1,1,1,1),(N'result_calculation',N'Admin',1,1,1,1,1,1),(N'accreditation',N'Admin',1,1,1,1,1,1),(N'user_import',N'Admin',1,1,1,1,1,1),(N'user_settings',N'Admin',1,1,1,1,1,1),(N'admin_users',N'Admin',1,1,1,1,1,1),(N'tenant_management',N'Admin',1,1,1,1,1,1),(N'campus_management',N'Admin',1,1,1,1,1,1),(N'theme_settings',N'Admin',1,1,1,1,1,1),(N'report_settings',N'Admin',1,1,1,1,1,1);
    INSERT INTO @pm VALUES (N'dashboard',N'Faculty',1,0,0,0,0,0),(N'assignments',N'Faculty',1,1,1,0,1,1),(N'enter_attendance',N'Faculty',1,1,1,0,1,1),(N'enter_results',N'Faculty',1,1,1,0,1,1),(N'gradebook',N'Faculty',1,1,1,0,1,1),(N'quizzes',N'Faculty',1,1,1,0,1,1),(N'lms_manage',N'Faculty',1,1,1,0,1,1),(N'course_material',N'Faculty',1,1,1,0,1,1),(N'discussion',N'Faculty',1,1,1,0,1,1),(N'announcements',N'Faculty',1,1,1,0,1,1),(N'attendance',N'Faculty',1,1,1,0,1,1),(N'results',N'Faculty',1,1,1,0,1,1),(N'result_calculation',N'Faculty',1,0,1,0,0,0),(N'study_plan',N'Faculty',1,0,1,0,0,0),(N'students',N'Faculty',1,0,0,0,1,0),(N'timetable_teacher',N'Faculty',1,0,0,0,1,0),(N'timetable_student',N'Faculty',1,0,0,0,1,0),(N'analytics',N'Faculty',1,0,0,0,1,0),(N'theme_settings',N'Faculty',1,0,0,0,0,0),(N'notifications',N'Faculty',1,0,0,0,0,0),(N'helpdesk',N'Faculty',1,1,1,0,1,0),(N'report_center',N'Faculty',1,0,0,0,1,0),(N'user_settings',N'Faculty',1,0,1,0,0,0);
    INSERT INTO @pm VALUES (N'dashboard',N'Student',1,0,0,0,0,0),(N'quizzes',N'Student',1,0,0,0,0,0),(N'lms_manage',N'Student',1,0,0,0,0,0),(N'course_material',N'Student',1,0,0,0,1,0),(N'discussion',N'Student',1,1,0,0,0,0),(N'announcements',N'Student',1,0,0,0,0,0),(N'study_plan',N'Student',1,0,0,0,0,0),(N'attendance',N'Student',1,0,0,0,0,0),(N'timetable_student',N'Student',1,0,0,0,0,0),(N'notifications',N'Student',1,0,0,0,0,0),(N'analytics',N'Student',1,0,0,0,1,0),(N'helpdesk',N'Student',1,1,0,0,0,0),(N'results',N'Student',1,0,0,0,1,0),(N'report_center',N'Student',1,0,0,0,1,0),(N'user_settings',N'Student',0,0,1,0,0,0),(N'theme_settings',N'Student',0,0,0,0,0,0);
    INSERT INTO @pm VALUES (N'dashboard',N'Finance',1,0,0,0,0,0),(N'payments',N'Finance',1,1,1,1,1,1),(N'notifications',N'Finance',1,0,0,0,0,0),(N'analytics',N'Finance',1,0,0,0,1,0),(N'helpdesk',N'Finance',1,0,0,0,0,0),(N'report_center',N'Finance',1,0,0,0,1,0),(N'user_settings',N'Finance',0,0,1,0,0,0),(N'theme_settings',N'Finance',0,0,0,0,0,0);

    INSERT INTO [role_resource_permissions] ([Id], [RoleName], [ResourceKey], [CanView], [CanAdd], [CanEdit], [CanDeactivate], [CanExport], [CanImport], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), pm.RoleName, pm.MenuKey, pm.V, pm.A, pm.E, pm.D, pm.X, pm.I, @Now, NULL FROM @pm pm;
END;

COMMIT TRANSACTION;
PRINT 'Seed-Core-Clean completed successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage2 NVARCHAR(MAX) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity2 INT = ERROR_SEVERITY();
    DECLARE @ErrorState2 INT = ERROR_STATE();

    RAISERROR (@ErrorMessage2, @ErrorSeverity2, @ErrorState2);
END CATCH;
