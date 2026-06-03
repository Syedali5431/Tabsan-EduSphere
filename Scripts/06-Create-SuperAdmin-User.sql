SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Creates (or updates) a SuperAdmin user in Tabsan-EduSphere.

  Usage:
  1) Set input variables in the INPUT section.
  2) Execute this script in SSMS/Azure Data Studio.

  Notes:
  - Default password hash below corresponds to plain password: EduSphere147
  - If a user already exists (by Id, Username, or Email), this script updates it to SuperAdmin.
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

/* ------------------------- INPUT ------------------------- */
DECLARE @UserId UNIQUEIDENTIFIER = NEWID();
DECLARE @Username NVARCHAR(100) = N'superadmin2';
DECLARE @Email NVARCHAR(256) = N'superadmin2@tabsan.local';
DECLARE @Address NVARCHAR(500) = N'Head Office';
DECLARE @PasswordHash NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @PhoneNumber NVARCHAR(32) = N'+61490000999';
/* --------------------------------------------------------- */

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

IF OBJECT_ID(N'[roles]') IS NULL OR OBJECT_ID(N'[users]') IS NULL
BEGIN
    RAISERROR('Required tables [roles] or [users] are missing. Run 01-Schema-Current.sql first.', 16, 1);
    RETURN;
END;

DECLARE @RoleSuperAdminId INT = (
    SELECT TOP 1 [Id]
    FROM [roles]
    WHERE [Name] = N'SuperAdmin'
);

IF @RoleSuperAdminId IS NULL
BEGIN
    RAISERROR('Role [SuperAdmin] was not found. Run 02-Seed-Core.sql first.', 16, 1);
    RETURN;
END;

DECLARE @ExistingId UNIQUEIDENTIFIER = (
    SELECT TOP 1 u.[Id]
    FROM [users] u
    WHERE u.[Id] = @UserId
       OR u.[Username] = @Username
       OR (u.[Email] IS NOT NULL AND u.[Email] = @Email)
    ORDER BY CASE WHEN u.[Id] = @UserId THEN 0 ELSE 1 END, u.[CreatedAt]
);

IF @ExistingId IS NULL
BEGIN
    INSERT INTO [users]
    (
        [Id],
        [Username],
        [Email],
        [Address],
        [PasswordHash],
        [RoleId],
        [DepartmentId],
        [InstitutionType],
        [IsActive],
        [LastLoginAt],
        [CreatedAt],
        [UpdatedAt],
        [IsDeleted],
        [DeletedAt]
    )
    VALUES
    (
        @UserId,
        @Username,
        @Email,
        @Address,
        @PasswordHash,
        @RoleSuperAdminId,
        NULL,
        NULL,
        1,
        NULL,
        @Now,
        NULL,
        0,
        NULL
    );

    SET @ExistingId = @UserId;
END
ELSE
BEGIN
    UPDATE [users]
    SET [Username] = @Username,
        [Email] = @Email,
        [Address] = @Address,
        [PasswordHash] = @PasswordHash,
        [RoleId] = @RoleSuperAdminId,
        [DepartmentId] = NULL,
        [InstitutionType] = NULL,
        [IsActive] = 1,
        [IsDeleted] = 0,
        [DeletedAt] = NULL,
        [UpdatedAt] = @Now
    WHERE [Id] = @ExistingId;
END;

IF COL_LENGTH('users', 'TenantId') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [TenantId] = NULL,
        [UpdatedAt] = @Now
    WHERE [Id] = @ExistingId;
END;

IF COL_LENGTH('users', 'CampusId') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [CampusId] = NULL,
        [UpdatedAt] = @Now
    WHERE [Id] = @ExistingId;
END;

IF COL_LENGTH('users', 'PhoneNumber') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [PhoneNumber] = @PhoneNumber,
        [UpdatedAt] = @Now
    WHERE [Id] = @ExistingId;
END;

IF COL_LENGTH('users', 'Address') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [Address] = @Address,
        [UpdatedAt] = @Now
    WHERE [Id] = @ExistingId;
END;

IF OBJECT_ID(N'[password_history]') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM [password_history]
        WHERE [UserId] = @ExistingId
          AND [PasswordHash] = @PasswordHash
    )
    BEGIN
        INSERT INTO [password_history] ([Id], [UserId], [PasswordHash], [CreatedAt])
        VALUES (NEWID(), @ExistingId, @PasswordHash, @Now);
    END;
END;

COMMIT TRANSACTION;

PRINT N'SuperAdmin user ready.';
PRINT N'Username: ' + @Username;
PRINT N'Email: ' + @Email;
PRINT N'UserId: ' + CONVERT(NVARCHAR(36), @ExistingId);
PRINT N'Password (for this default hash): EduSphere147';

SELECT
    u.[Id],
    u.[Username],
    u.[Email],
    r.[Name] AS [RoleName],
    u.[IsActive],
    u.[IsDeleted],
    u.[CreatedAt],
    u.[UpdatedAt]
FROM [users] u
INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
WHERE u.[Id] = @ExistingId;

END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();

    RAISERROR('06-Create-SuperAdmin-User.sql failed at line %d: %s', @ErrSeverity, @ErrState, @ErrLine, @ErrMsg);
END CATCH;
GO
