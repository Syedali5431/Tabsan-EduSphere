SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Repairs demo user role assignments and tenant/campus scope.

  Fixes common seed issues:
  - Users pointing at department/semester GUIDs used as TenantId/CampusId
  - SuperAdmin incorrectly scoped to a tenant/campus
  - RoleId drift for named demo accounts
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    RAISERROR('Database [Tabsan-EduSphere] does not exist.', 16, 1);
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

BEGIN TRY
BEGIN TRANSACTION;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @PwdHash NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @UniTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @UniCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @ColTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);
DECLARE @ColCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);
DECLARE @LegacyTenantId UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER);
DECLARE @LegacyCampusId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222222' AS UNIQUEIDENTIFIER);

DECLARE @RoleSuperAdmin INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'SuperAdmin');
DECLARE @RoleAdmin INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Admin');
DECLARE @RoleFaculty INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Faculty');
DECLARE @RoleStudent INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Student');
DECLARE @RoleFinance INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Finance');
DECLARE @RoleParent INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Parent');

IF OBJECT_ID(N'[tenants]') IS NOT NULL
BEGIN
    /* Avoid unique-index collisions when legacy rows reused canonical tenant codes. */
    UPDATE t
    SET t.[Code] = CONCAT(N'LEGACY-', LEFT(REPLACE(CAST(t.[Id] AS NVARCHAR(36)), N'-', N''), 12)),
        t.[UpdatedAt] = @Now
    FROM [tenants] t
    WHERE t.[Id] NOT IN (@UniTenantId, @ColTenantId, @SchTenantId)
      AND t.[Code] IN (N'DEFAULT', N'UNI', N'COL', N'SCH');

    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT v.[Id], v.[Code], v.[Name], 1, @Now, NULL, 0, NULL
    FROM (VALUES
        (@UniTenantId, N'UNI', N'University Tenant'),
        (@ColTenantId, N'COL', N'College Tenant'),
        (@SchTenantId, N'SCH', N'School Tenant')
    ) v([Id], [Code], [Name])
    WHERE NOT EXISTS (SELECT 1 FROM [tenants] t WHERE t.[Id] = v.[Id]);
END;

IF OBJECT_ID(N'[campuses]') IS NOT NULL
BEGIN
    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT v.[Id], v.[TenantId], v.[Code], v.[Name], 1, @Now, NULL, 0, NULL
    FROM (VALUES
        (@UniCampusId, @UniTenantId, N'UNI-MAIN', N'University Main Campus'),
        (@ColCampusId, @ColTenantId, N'COL-MAIN', N'College Main Campus'),
        (@SchCampusId, @SchTenantId, N'SCH-MAIN', N'School Main Campus')
    ) v([Id], [TenantId], [Code], [Name])
    WHERE NOT EXISTS (SELECT 1 FROM [campuses] c WHERE c.[Id] = v.[Id]);
END;

IF COL_LENGTH('users', 'TenantId') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [TenantId] = @UniTenantId,
        [CampusId] = @UniCampusId,
        [UpdatedAt] = @Now
    WHERE [TenantId] = @LegacyTenantId;

    UPDATE [users]
    SET [CampusId] = @UniCampusId,
        [UpdatedAt] = @Now
    WHERE [CampusId] = @LegacyCampusId
      AND [TenantId] = @UniTenantId;
END;

IF COL_LENGTH('departments', 'TenantId') IS NOT NULL AND COL_LENGTH('departments', 'CampusId') IS NOT NULL
BEGIN
    UPDATE d
    SET d.[TenantId] = @UniTenantId,
        d.[CampusId] = @UniCampusId,
        d.[UpdatedAt] = @Now
    FROM [departments] d
    WHERE d.[TenantId] = @LegacyTenantId;

    UPDATE d
    SET d.[CampusId] = @UniCampusId,
        d.[UpdatedAt] = @Now
    FROM [departments] d
    WHERE d.[CampusId] = @LegacyCampusId
      AND d.[TenantId] = @UniTenantId;

    UPDATE d
    SET d.[TenantId] = CASE d.[InstitutionType]
                          WHEN 2 THEN @UniTenantId
                          WHEN 1 THEN @ColTenantId
                          ELSE @SchTenantId
                      END,
        d.[CampusId] = CASE d.[InstitutionType]
                          WHEN 2 THEN @UniCampusId
                          WHEN 1 THEN @ColCampusId
                          ELSE @SchCampusId
                      END,
        d.[UpdatedAt] = @Now
    FROM [departments] d
    WHERE d.[IsDeleted] = 0;
END;

IF COL_LENGTH('users', 'TenantId') IS NOT NULL AND COL_LENGTH('users', 'CampusId') IS NOT NULL
BEGIN
    UPDATE u
    SET u.[TenantId] = CASE d.[InstitutionType]
                          WHEN 2 THEN @UniTenantId
                          WHEN 1 THEN @ColTenantId
                          ELSE @SchTenantId
                      END,
        u.[CampusId] = CASE d.[InstitutionType]
                          WHEN 2 THEN @UniCampusId
                          WHEN 1 THEN @ColCampusId
                          ELSE @SchCampusId
                      END,
        u.[UpdatedAt] = @Now
    FROM [users] u
    INNER JOIN [departments] d ON d.[Id] = u.[DepartmentId]
    WHERE u.[IsDeleted] = 0
      AND u.[RoleId] <> @RoleSuperAdmin;

    UPDATE [users]
    SET [TenantId] = NULL,
        [CampusId] = NULL,
        [UpdatedAt] = @Now
    WHERE [IsDeleted] = 0
      AND [RoleId] = @RoleSuperAdmin;
END;

/* Re-sync named demo account roles by username convention. */
UPDATE u
SET u.[RoleId] = CASE
        WHEN u.[Username] = N'superadmin' THEN @RoleSuperAdmin
        WHEN u.[Username] LIKE N'finance.%' OR u.[Username] LIKE N'auto.finance.%' THEN @RoleFinance
        WHEN u.[Username] LIKE N'parent.guardian.%' THEN COALESCE(@RoleParent, @RoleStudent)
        WHEN u.[Username] LIKE N'admin.%'
          OR u.[Username] LIKE N'auto.admin.%'
          OR u.[Username] LIKE N'demo.%.admin'
          OR u.[Username] LIKE N'bulk.%.admin.%' THEN @RoleAdmin
        WHEN u.[Username] LIKE N'faculty.%'
          OR u.[Username] LIKE N'auto.faculty.%'
          OR u.[Username] LIKE N'demo.%.faculty'
          OR u.[Username] LIKE N'bulk.%.faculty.%' THEN @RoleFaculty
        WHEN u.[Username] LIKE N'student.%'
          OR u.[Username] LIKE N'auto.%.student.%'
          OR u.[Username] LIKE N'demo.%.student%'
          OR u.[Username] LIKE N'bulk.%.student.%'
          OR u.[Username] LIKE N'lifecycle.%' THEN @RoleStudent
        ELSE u.[RoleId]
    END,
    u.[UpdatedAt] = @Now
FROM [users] u
WHERE u.[IsDeleted] = 0;

UPDATE [users]
SET [PasswordHash] = @PwdHash,
    [UpdatedAt] = @Now,
    [FailedLoginAttempts] = 0,
    [IsLockedOut] = 0,
    [LockedOutUntil] = NULL
WHERE [IsDeleted] = 0
  AND (
        [PasswordHash] IS NULL
     OR [PasswordHash] NOT LIKE N'argon2id:%'
     OR [PasswordHash] = N'REPLACE_WITH_VALID_HASH'
     OR [PasswordHash] <> @PwdHash
  );

IF COL_LENGTH('users', 'MustChangePassword') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [MustChangePassword] = 0,
        [UpdatedAt] = @Now
    WHERE [IsDeleted] = 0
      AND [MustChangePassword] = 1;
END;

COMMIT TRANSACTION;

PRINT N'Role and tenant/campus scope repair completed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO
