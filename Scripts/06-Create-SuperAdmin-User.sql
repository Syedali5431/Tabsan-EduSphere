/*
  Create SuperAdmin User — Tabsan EduSphere v1.0
  
  Creates or updates an additional SuperAdmin user.
  Default password: EduSphere147
*/

SET NOCOUNT ON;
GO

USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @UserId UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666699';
DECLARE @Username NVARCHAR(100) = N'superadmin2';
DECLARE @Email NVARCHAR(256) = N'superadmin2@tabsan.local';
DECLARE @FullName NVARCHAR(200) = N'Super Admin 2';
DECLARE @RoleId INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name]=N'SuperAdmin');
DECLARE @PasswordHash NVARCHAR(512) = N'argon2id:IC+ORGZ905PJHXorsasHdqZRTlTq+l6j5aMXLWduZO8=:42cW82PQ47be61NOshZkBebRNjeTL5C8NfE+VYx7+GA=';

IF @RoleId IS NULL
BEGIN
    RAISERROR('SuperAdmin role not found. Run 02-Seed-Core.sql first.', 16, 1);
    RETURN;
END

-- Update if exists, otherwise insert
IF EXISTS (SELECT 1 FROM [users] WHERE [Id]=@UserId OR [Username]=@Username OR [Email]=@Email)
BEGIN
    UPDATE [users] SET
        [Username] = @Username,
        [Email] = @Email,
        [FullName] = @FullName,
        [PasswordHash] = @PasswordHash,
        [RoleId] = @RoleId,
        [IsActive] = 1,
        [IsDeleted] = 0,
        [DeletedAt] = NULL,
        [UpdatedAt] = @Now
    WHERE [Id] = @UserId OR [Username] = @Username OR [Email] = @Email;
    PRINT 'SuperAdmin user updated.';
END
ELSE
BEGIN
    INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[IsActive],[CreatedAt],[IsDeleted])
    VALUES (@UserId, @Username, @Email, @FullName, @PasswordHash, @RoleId, 1, @Now, 0);
    PRINT 'SuperAdmin user created.';
END
GO
