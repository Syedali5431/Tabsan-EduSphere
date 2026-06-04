SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Resets all active user passwords to the canonical demo password.

  Plain password: EduSphere147
  Hash format:    argon2id (matches Tabsan.EduSphere.Infrastructure.Auth.Argon2idPasswordHasher)

  Use when users were created with placeholder hashes (e.g. REPLACE_WITH_VALID_HASH)
  or after partial seed runs that skipped password updates.
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

DECLARE @Updated INT = @@ROWCOUNT;

IF COL_LENGTH('users', 'MustChangePassword') IS NOT NULL
BEGIN
    UPDATE [users]
    SET [MustChangePassword] = 0,
        [UpdatedAt] = @Now
    WHERE [IsDeleted] = 0
      AND [MustChangePassword] = 1;
END

COMMIT TRANSACTION;

PRINT CONCAT(N'Password reset completed. Users updated: ', @Updated, N'. Login password: EduSphere147');

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
