SET NOCOUNT ON;

/*
  Clean-seed validation script for Tabsan EduSphere.

  PREREQUISITE SCRIPTS (CLEAN PATH):
  1. 01-Schema-Current.sql
  2. Seed-Core-Clean.sql

  PURPOSE:
  - Validate clean startup baseline only (no dummy/demo data)
  - Verify roles, superadmin user, institution type baseline, modules,
    role/module permissions, report/sidebar access, and portal settings.
  - Fail fast when baseline is not clean.

  NOTE:
  - Read-only checks plus final RAISERROR when any check fails.
  - Safe to run repeatedly.
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
    RAISERROR('Failed to switch context to [Tabsan-EduSphere]. Aborting clean check script.', 16, 1);
    RETURN;
END;
GO

PRINT 'Running clean baseline checks...';

DECLARE @Results TABLE
(
    [CheckName] NVARCHAR(200) NOT NULL,
    [Passed] BIT NOT NULL,
    [Actual] NVARCHAR(4000) NULL,
    [Expected] NVARCHAR(4000) NULL
);

/* 1) Roles */
DECLARE @RequiredRoles TABLE ([Name] NVARCHAR(50) PRIMARY KEY);
INSERT INTO @RequiredRoles ([Name]) VALUES
(N'SuperAdmin'), (N'Admin'), (N'Faculty'), (N'Student'), (N'Finance'), (N'Parent');

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Roles.RequiredPresent',
    CASE WHEN NOT EXISTS (
        SELECT 1
        FROM @RequiredRoles r
        WHERE NOT EXISTS (SELECT 1 FROM [roles] x WHERE x.[Name] = r.[Name])
    ) THEN 1 ELSE 0 END,
    CAST((SELECT COUNT(1) FROM [roles] WHERE [Name] IN (SELECT [Name] FROM @RequiredRoles)) AS NVARCHAR(20)),
    N'6';

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Roles.FinancePresent',
    CASE WHEN EXISTS (SELECT 1 FROM [roles] WHERE [Name] = N'Finance') THEN 1 ELSE 0 END,
    CAST((SELECT COUNT(1) FROM [roles] WHERE [Name] = N'Finance') AS NVARCHAR(20)),
    N'1';

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
        N'Users.FinanceUserCleanBaselineCount',
        CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'0'
FROM [users] u
INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
WHERE r.[Name] = N'Finance'
    AND u.[IsDeleted] = 0;

/* 2) SuperAdmin-only startup user baseline */
INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Users.TotalCount',
    CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END,
    CAST(COUNT(1) AS NVARCHAR(20)),
    N'1'
FROM [users]
WHERE [IsDeleted] = 0;

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Users.SuperAdminAccountPresent',
    CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END,
    CAST(COUNT(1) AS NVARCHAR(20)),
    N'1'
FROM [users] u
INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
WHERE u.[Username] = N'superadmin'
  AND u.[Email] = N'superadmin@tabsan.local'
  AND r.[Name] = N'SuperAdmin'
  AND u.[IsActive] = 1
  AND u.[IsDeleted] = 0;

IF COL_LENGTH('users', 'PhoneNumber') IS NOT NULL
BEGIN
        INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
        SELECT
                N'Users.SuperAdminPhonePresent',
                CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END,
                CAST(COUNT(1) AS NVARCHAR(20)),
                N'1'
        FROM [users]
        WHERE [Id] = '66666666-6666-6666-6666-666666666601'
            AND [PhoneNumber] IS NOT NULL
            AND LTRIM(RTRIM([PhoneNumber])) <> N'';
END;

/* 3) Department institution-type baseline */
INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Departments.CoreOnlyCount',
    CASE WHEN COUNT(1) = 3 THEN 1 ELSE 0 END,
    CAST(COUNT(1) AS NVARCHAR(20)),
    N'3'
FROM [departments]
WHERE [IsDeleted] = 0;

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Departments.InstitutionTypeCoverage',
    CASE WHEN COUNT(DISTINCT [InstitutionType]) = 3 THEN 1 ELSE 0 END,
    CAST(COUNT(DISTINCT [InstitutionType]) AS NVARCHAR(20)),
    N'3 (0,1,2)'
FROM [departments]
WHERE [InstitutionType] IN (0, 1, 2)
  AND [IsDeleted] = 0;

IF OBJECT_ID(N'[tenants]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Tenants.DefaultPresent',
        CASE WHEN COUNT(1) >= 1 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'>=1'
    FROM [tenants]
    WHERE [Code] = N'DEFAULT'
      AND [IsDeleted] = 0;
END;

IF OBJECT_ID(N'[campuses]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Campuses.DefaultPresent',
        CASE WHEN COUNT(1) >= 1 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'>=1'
    FROM [campuses]
    WHERE [Code] = N'MAIN'
      AND [IsDeleted] = 0;
END;

IF COL_LENGTH('departments', 'TenantId') IS NOT NULL AND COL_LENGTH('departments', 'CampusId') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Departments.CoreScopedCount',
        CASE WHEN COUNT(1) = 3 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'3'
    FROM [departments]
    WHERE [Id] IN (
            '21000000-0000-0000-0000-000000000001',
            '21000000-0000-0000-0000-000000000002',
            '21000000-0000-0000-0000-000000000003'
    )
      AND [TenantId] IS NOT NULL
      AND [CampusId] IS NOT NULL;
END;

IF COL_LENGTH('buildings', 'TenantId') IS NOT NULL AND COL_LENGTH('buildings', 'CampusId') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Buildings.ScopeColumnsPresent',
        CASE WHEN COUNT(1) = 3 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'3'
    FROM [buildings]
    WHERE [Id] IN (
            '23232323-2323-2323-2323-232323232301',
            '23232323-2323-2323-2323-232323232302',
            '23232323-2323-2323-2323-232323232303'
    )
      AND [TenantId] IS NOT NULL
      AND [CampusId] IS NOT NULL;
END;

IF COL_LENGTH('rooms', 'TenantId') IS NOT NULL AND COL_LENGTH('rooms', 'CampusId') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Rooms.ScopeColumnsPresent',
        CASE WHEN COUNT(1) = 6 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'6'
    FROM [rooms]
    WHERE [Id] IN (
            '24242424-2424-2424-2424-242424242401',
            '24242424-2424-2424-2424-242424242402',
            '24242424-2424-2424-2424-242424242403',
            '24242424-2424-2424-2424-242424242404',
            '24242424-2424-2424-2424-242424242405',
            '24242424-2424-2424-2424-242424242406'
    )
      AND [TenantId] IS NOT NULL
      AND [CampusId] IS NOT NULL;
END;

IF COL_LENGTH('courses', 'TenantId') IS NOT NULL
   AND COL_LENGTH('courses', 'CampusId') IS NOT NULL
   AND COL_LENGTH('courses', 'InstitutionType') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Courses.ScopeColumnsPresent',
        CASE WHEN COUNT(1) = 3 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'3'
    FROM (VALUES
        (COL_LENGTH('courses', 'TenantId')),
        (COL_LENGTH('courses', 'CampusId')),
        (COL_LENGTH('courses', 'InstitutionType'))
    ) v([len])
    WHERE v.[len] IS NOT NULL;
END;

IF COL_LENGTH('course_offerings', 'TenantId') IS NOT NULL
   AND COL_LENGTH('course_offerings', 'CampusId') IS NOT NULL
   AND COL_LENGTH('course_offerings', 'InstitutionType') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'CourseOfferings.ScopeColumnsPresent',
        CASE WHEN COUNT(1) = 3 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'3'
    FROM (VALUES
        (COL_LENGTH('course_offerings', 'TenantId')),
        (COL_LENGTH('course_offerings', 'CampusId')),
        (COL_LENGTH('course_offerings', 'InstitutionType'))
    ) v([len])
    WHERE v.[len] IS NOT NULL;
END;

IF OBJECT_ID(N'[course_materials]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'CourseMaterials.TablePresent',
        1,
        N'1',
        N'1';

    IF COL_LENGTH('course_materials', 'TenantId') IS NOT NULL
       AND COL_LENGTH('course_materials', 'CampusId') IS NOT NULL
    BEGIN
        INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
        SELECT
            N'CourseMaterials.ScopeColumnsPresent',
            CASE WHEN COUNT(1) = 2 THEN 1 ELSE 0 END,
            CAST(COUNT(1) AS NVARCHAR(20)),
            N'2'
        FROM (VALUES
            (COL_LENGTH('course_materials', 'TenantId')),
            (COL_LENGTH('course_materials', 'CampusId'))
        ) v([len])
        WHERE v.[len] IS NOT NULL;
    END;

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'IndexExists.IX_course_materials_scope_lookup',
        CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_materials_scope_lookup' AND [object_id] = OBJECT_ID(N'course_materials')) THEN 1 ELSE 0 END,
        CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_materials_scope_lookup' AND [object_id] = OBJECT_ID(N'course_materials')) THEN N'1' ELSE N'0' END,
        N'1';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'IndexExists.IX_course_materials_scope_active_sort',
        CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_materials_scope_active_sort' AND [object_id] = OBJECT_ID(N'course_materials')) THEN 1 ELSE 0 END,
        CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_materials_scope_active_sort' AND [object_id] = OBJECT_ID(N'course_materials')) THEN N'1' ELSE N'0' END,
        N'1';

    IF COL_LENGTH('course_materials', 'FilePath') IS NOT NULL
    BEGIN
        INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
        SELECT
            N'CourseMaterials.UnsupportedFileExtensions',
            CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END,
            CAST(COUNT(1) AS NVARCHAR(20)),
            N'0'
        FROM [course_materials]
        WHERE [FilePath] IS NOT NULL
          AND LTRIM(RTRIM([FilePath])) <> N''
          AND LOWER(
                CASE
                    WHEN CHARINDEX(N'.', REVERSE([FilePath])) > 0
                    THEN RIGHT([FilePath], CHARINDEX(N'.', REVERSE([FilePath])))
                    ELSE N''
                END
              ) NOT IN (N'.doc', N'.docx', N'.ppt', N'.pptx', N'.pdf', N'.txt', N'.xls', N'.xlsx', N'.jpg', N'.jpeg', N'.png');
    END;
END;

IF OBJECT_ID(N'[admin_department_assignments]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'AdminAssignments.CoreAdminPresent',
        CASE WHEN COUNT(1) >= 1 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'>=1'
    FROM [admin_department_assignments] a
    WHERE a.[AdminUserId] = '66666666-6666-6666-6666-666666666602'
      AND a.[DepartmentId] = '21000000-0000-0000-0000-000000000001'
      AND a.[RemovedAt] IS NULL;
END;

/* 4) Modules + status */
DECLARE @RequiredModules TABLE ([Key] NVARCHAR(50) PRIMARY KEY);
INSERT INTO @RequiredModules ([Key]) VALUES
(N'authentication'), (N'departments'), (N'courses'), (N'sis'),
(N'assignments'), (N'attendance'), (N'results'), (N'quizzes'),
(N'fyp'), (N'notifications'), (N'ai_chat'), (N'reports'),
(N'themes'), (N'advanced_audit');

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Modules.RequiredPresent',
    CASE WHEN NOT EXISTS (
        SELECT 1
        FROM @RequiredModules r
        WHERE NOT EXISTS (SELECT 1 FROM [modules] m WHERE m.[Key] = r.[Key])
    ) THEN 1 ELSE 0 END,
    CAST((SELECT COUNT(1) FROM [modules] WHERE [Key] IN (SELECT [Key] FROM @RequiredModules)) AS NVARCHAR(20)),
    N'14';

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'Modules.LegacyKeysAbsent',
    CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END,
    CAST(COUNT(1) AS NVARCHAR(20)),
    N'0'
FROM [modules]
WHERE [Key] IN (N'ai-chat', N'theming', N'advanced-audit');

/* 4.1) Clean path must not contain full demo dataset marker */
IF OBJECT_ID(N'[Tabsan-EduSphere]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'CleanBaseline.FullDummyDatasetMarkerAbsent',
        CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'0'
    FROM [Tabsan-EduSphere]
    WHERE [DemoKey] = N'DemoDatasetVersion'
      AND [DemoValue] LIKE N'FullDummyData-v%';
END;

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'ModuleStatus.RequiredCoverage',
    CASE WHEN COUNT(1) = 14 THEN 1 ELSE 0 END,
    CAST(COUNT(1) AS NVARCHAR(20)),
    N'14'
FROM [module_status] ms
INNER JOIN [modules] m ON m.[Id] = ms.[ModuleId]
WHERE m.[Key] IN (SELECT [Key] FROM @RequiredModules);

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'IndexExists.IX_attendance_offering_date',
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_offering_date' AND [object_id] = OBJECT_ID(N'attendance_records')) THEN 1 ELSE 0 END,
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_offering_date' AND [object_id] = OBJECT_ID(N'attendance_records')) THEN N'1' ELSE N'0' END,
    N'1';

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'IndexExists.IX_attendance_student_id',
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_student_id' AND [object_id] = OBJECT_ID(N'attendance_records')) THEN 1 ELSE 0 END,
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_student_id' AND [object_id] = OBJECT_ID(N'attendance_records')) THEN N'1' ELSE N'0' END,
    N'1';

INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
SELECT
    N'IndexExists.IX_attendance_student_offering_date',
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_student_offering_date' AND [object_id] = OBJECT_ID(N'attendance_records')) THEN 1 ELSE 0 END,
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_student_offering_date' AND [object_id] = OBJECT_ID(N'attendance_records')) THEN N'1' ELSE N'0' END,
    N'1';

/* 5) Module role assignments */
IF OBJECT_ID(N'[module_role_assignments]') IS NOT NULL
BEGIN
    DECLARE @ExpectedModuleRoleCount INT = 48;

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'ModuleRoleAssignments.MinimumRequired',
        CASE WHEN COUNT(1) >= @ExpectedModuleRoleCount THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        CAST(@ExpectedModuleRoleCount AS NVARCHAR(20))
    FROM [module_role_assignments] mra
    INNER JOIN [modules] m ON m.[Id] = mra.[ModuleId]
    WHERE m.[Key] IN (SELECT [Key] FROM @RequiredModules);
END;

/* 6) Portal settings */
IF OBJECT_ID(N'[portal_settings]') IS NOT NULL
BEGIN
    DECLARE @RequiredPortalKeys TABLE ([Key] NVARCHAR(100) PRIMARY KEY);
    INSERT INTO @RequiredPortalKeys ([Key]) VALUES
    (N'portal.universityName'),
    (N'portal.brandInitials'),
    (N'portal.theme'),
    (N'portal.timeZone'),
    (N'institution_include_school'),
    (N'institution_include_college'),
    (N'institution_include_university');

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'PortalSettings.RequiredKeysPresent',
        CASE WHEN NOT EXISTS (
            SELECT 1
            FROM @RequiredPortalKeys p
            WHERE NOT EXISTS (SELECT 1 FROM [portal_settings] s WHERE s.[Key] = p.[Key])
        ) THEN 1 ELSE 0 END,
        CAST((SELECT COUNT(1) FROM [portal_settings] WHERE [Key] IN (SELECT [Key] FROM @RequiredPortalKeys)) AS NVARCHAR(20)),
        N'7';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'PaymentSummaryReportPresent',
        CASE WHEN EXISTS (SELECT 1 FROM [report_definitions] WHERE [Key] = N'payment_summary') THEN 1 ELSE 0 END,
        CAST((SELECT COUNT(1) FROM [report_definitions] WHERE [Key] = N'payment_summary') AS NVARCHAR(20)),
        N'1';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'PaymentSummaryReportFinanceAssignmentPresent',
        CASE WHEN EXISTS (
            SELECT 1
            FROM [report_definitions] rd
            INNER JOIN [report_role_assignments] rra ON rra.[ReportDefinitionId] = rd.[Id]
            WHERE rd.[Key] = N'payment_summary'
              AND rra.[RoleName] = N'Finance'
        ) THEN 1 ELSE 0 END,
        CAST((
            SELECT COUNT(1)
            FROM [report_definitions] rd
            INNER JOIN [report_role_assignments] rra ON rra.[ReportDefinitionId] = rd.[Id]
            WHERE rd.[Key] = N'payment_summary'
              AND rra.[RoleName] = N'Finance'
        ) AS NVARCHAR(20)),
        N'1';
END;

/* 7) Report functionality permissions */
IF OBJECT_ID(N'[report_definitions]') IS NOT NULL
BEGIN
    DECLARE @RequiredReportKeys TABLE ([Key] NVARCHAR(100) PRIMARY KEY);
    INSERT INTO @RequiredReportKeys ([Key]) VALUES
    (N'attendance_summary'),
    (N'result_summary'),
    (N'gpa_report'),
    (N'student_transcript');

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Reports.RequiredDefinitionsPresent',
        CASE WHEN COUNT(1) = 4 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'4'
    FROM [report_definitions]
    WHERE [Key] IN (SELECT [Key] FROM @RequiredReportKeys)
      AND [IsDeleted] = 0;

    IF OBJECT_ID(N'[report_role_assignments]') IS NOT NULL
    BEGIN
        INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
        SELECT
            N'Reports.RoleAssignmentMinimum',
            CASE WHEN COUNT(1) >= 13 THEN 1 ELSE 0 END,
            CAST(COUNT(1) AS NVARCHAR(20)),
            N'13'
        FROM [report_role_assignments] rra
        INNER JOIN [report_definitions] rd ON rd.[Id] = rra.[ReportDefinitionId]
        WHERE rd.[Key] IN (SELECT [Key] FROM @RequiredReportKeys)
          AND rd.[IsDeleted] = 0;
    END;
END;

/* 8) Sidebar functionality permissions */
IF OBJECT_ID(N'[sidebar_menu_items]') IS NOT NULL
BEGIN
    DECLARE @RequiredMenuKeys TABLE ([Key] NVARCHAR(100) PRIMARY KEY);
    INSERT INTO @RequiredMenuKeys ([Key]) VALUES
    (N'dashboard'), (N'academic'), (N'courses'), (N'attendance'),
    (N'programs'), (N'settings'), (N'admin_users'), (N'tenant_management'), (N'campus_management');

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Sidebar.RequiredMenuItemsPresent',
        CASE WHEN COUNT(1) = 9 THEN 1 ELSE 0 END,
        CAST(COUNT(1) AS NVARCHAR(20)),
        N'9'
    FROM [sidebar_menu_items]
    WHERE [Key] IN (SELECT [Key] FROM @RequiredMenuKeys)
      AND [IsDeleted] = 0;

    IF OBJECT_ID(N'[sidebar_menu_role_accesses]') IS NOT NULL
    BEGIN
        INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
        SELECT
            N'Sidebar.RoleAccessMinimum',
            CASE WHEN COUNT(1) >= 23 THEN 1 ELSE 0 END,
            CAST(COUNT(1) AS NVARCHAR(20)),
            N'23'
        FROM [sidebar_menu_role_accesses] sra
        INNER JOIN [sidebar_menu_items] smi ON smi.[Id] = sra.[SidebarMenuItemId]
        WHERE smi.[Key] IN (N'dashboard', N'academic', N'courses', N'attendance', N'programs', N'settings', N'admin_users', N'tenant_management', N'campus_management')
          AND smi.[IsDeleted] = 0;
    END;
END;

/* 9) User profile security/SMS columns exist in schema baseline */
IF OBJECT_ID(N'[users]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Users.ColumnExists.PhoneNumber',
        CASE WHEN COL_LENGTH('users', 'PhoneNumber') IS NULL THEN 0 ELSE 1 END,
        CASE WHEN COL_LENGTH('users', 'PhoneNumber') IS NULL THEN N'0' ELSE N'1' END,
        N'1';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Users.ColumnExists.TenantId',
        CASE WHEN COL_LENGTH('users', 'TenantId') IS NULL THEN 0 ELSE 1 END,
        CASE WHEN COL_LENGTH('users', 'TenantId') IS NULL THEN N'0' ELSE N'1' END,
        N'1';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Users.ColumnExists.CampusId',
        CASE WHEN COL_LENGTH('users', 'CampusId') IS NULL THEN 0 ELSE 1 END,
        CASE WHEN COL_LENGTH('users', 'CampusId') IS NULL THEN N'0' ELSE N'1' END,
        N'1';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Users.ColumnExists.MfaIsEnabled',
        CASE WHEN COL_LENGTH('users', 'MfaIsEnabled') IS NULL THEN 0 ELSE 1 END,
        CASE WHEN COL_LENGTH('users', 'MfaIsEnabled') IS NULL THEN N'0' ELSE N'1' END,
        N'1';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Users.ColumnExists.MfaTotpSecret',
        CASE WHEN COL_LENGTH('users', 'MfaTotpSecret') IS NULL THEN 0 ELSE 1 END,
        CASE WHEN COL_LENGTH('users', 'MfaTotpSecret') IS NULL THEN N'0' ELSE N'1' END,
        N'1';

    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT
        N'Users.ColumnExists.MfaRecoveryCodesHashJson',
        CASE WHEN COL_LENGTH('users', 'MfaRecoveryCodesHashJson') IS NULL THEN 0 ELSE 1 END,
        CASE WHEN COL_LENGTH('users', 'MfaRecoveryCodesHashJson') IS NULL THEN N'0' ELSE N'1' END,
        N'1';

    IF COL_LENGTH('users', 'MfaIsEnabled') IS NOT NULL
    BEGIN
        INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
        SELECT
            N'Users.CleanBaseline.MfaEnabledCount',
            CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END,
            CAST(COUNT(1) AS NVARCHAR(20)),
            N'0'
        FROM [users]
        WHERE [IsDeleted] = 0
          AND [MfaIsEnabled] = 1;
    END;

    IF COL_LENGTH('users', 'TenantId') IS NOT NULL AND COL_LENGTH('users', 'CampusId') IS NOT NULL
    BEGIN
        INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
        SELECT
            N'Users.SuperAdminUnscoped',
            CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END,
            CAST(COUNT(1) AS NVARCHAR(20)),
            N'1'
        FROM [users] u
        INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
        WHERE r.[Name] = N'SuperAdmin'
          AND u.[IsDeleted] = 0
          AND u.[TenantId] IS NULL
          AND u.[CampusId] IS NULL;
    END;
END;

/* 10) Clean means no dummy-domain rows */
IF OBJECT_ID(N'[academic_programs]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.AcademicPrograms', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [academic_programs];
END;

IF OBJECT_ID(N'[courses]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.Courses', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [courses];
END;

IF OBJECT_ID(N'[course_offerings]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.CourseOfferings', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [course_offerings];
END;

IF OBJECT_ID(N'[student_profiles]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.StudentProfiles', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [student_profiles];
END;

IF OBJECT_ID(N'[enrollments]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.Enrollments', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [enrollments];
END;

IF OBJECT_ID(N'[assignments]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.Assignments', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [assignments];
END;

IF OBJECT_ID(N'[results]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.Results', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [results];
END;

IF OBJECT_ID(N'[attendance_records]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.AttendanceRecords', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [attendance_records];
END;

IF OBJECT_ID(N'[quizzes]') IS NOT NULL
BEGIN
    INSERT INTO @Results ([CheckName], [Passed], [Actual], [Expected])
    SELECT N'NoDummy.Quizzes', CASE WHEN COUNT(1) = 0 THEN 1 ELSE 0 END, CAST(COUNT(1) AS NVARCHAR(20)), N'0'
    FROM [quizzes];
END;

SELECT [CheckName], [Passed], [Actual], [Expected]
FROM @Results
ORDER BY CASE WHEN [Passed] = 1 THEN 1 ELSE 0 END, [CheckName];

DECLARE @FailureCount INT = (SELECT COUNT(1) FROM @Results WHERE [Passed] = 0);

IF @FailureCount > 0
BEGIN
    RAISERROR('Clean baseline checks failed. Review result set for details.', 16, 1);
    RETURN;
END;

PRINT 'Clean baseline checks passed.';
