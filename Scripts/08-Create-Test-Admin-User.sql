/*
  Create Test Admin User — Tabsan EduSphere v1.0

  Creates or updates a testing account that has every right except SuperAdmin:
    Username : testadmin
    Password : EduSphere147
    Role     : Admin (highest role below SuperAdmin)

  Every non-SuperAdmin account belongs to exactly one tenant (only SuperAdmin spans
  tenants), so the account is placed in the University tenant/campus (TABSAN-UNI / UNI-1),
  which holds the richest demo data (FYP, degree audit, graduation). It is not limited to
  an institution type and is assigned to every active department of that tenant, so it
  can manage all academic, student, finance and reporting areas there.
  SuperAdmin-only governance (licenses, tenants/campuses, module composition, sidebar,
  institution policy, admin-user management) stays unavailable by design.

  Idempotent: safe to run more than once. Run after 02-Seed-Core.sql / 03-FullDummyData.sql.
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @UserId UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666688';
DECLARE @Username NVARCHAR(100) = N'testadmin';
DECLARE @Email NVARCHAR(256) = N'testadmin@tabsan.local';
DECLARE @FullName NVARCHAR(200) = N'Test Administrator';
DECLARE @RoleId INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name]=N'Admin');
-- Argon2id hash of EduSphere147 (same default password as all seeded accounts)
DECLARE @TenantId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [tenants] WHERE [Code]=N'TABSAN-UNI');
DECLARE @CampusId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [campuses] WHERE [Code]=N'UNI-1');
DECLARE @PasswordHash NVARCHAR(512) = N'argon2id:IC+ORGZ905PJHXorsasHdqZRTlTq+l6j5aMXLWduZO8=:42cW82PQ47be61NOshZkBebRNjeTL5C8NfE+VYx7+GA=';

IF @RoleId IS NULL OR @TenantId IS NULL OR @CampusId IS NULL
BEGIN
    RAISERROR('Admin role or University tenant/campus not found. Run 02-Seed-Core.sql first.', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM [users] WHERE [Id]=@UserId OR [Username]=@Username OR [Email]=@Email)
BEGIN
    UPDATE [users] SET
        [Username] = @Username,
        [Email] = @Email,
        [FullName] = @FullName,
        [PasswordHash] = @PasswordHash,
        [RoleId] = @RoleId,
        [TenantId] = @TenantId,
        [CampusId] = @CampusId,
        [InstitutionType] = NULL,
        [DepartmentId] = NULL,
        [IsActive] = 1,
        [IsDeleted] = 0,
        [DeletedAt] = NULL,
        [UpdatedAt] = @Now
    WHERE [Id] = @UserId OR [Username] = @Username OR [Email] = @Email;
    SELECT @UserId = [Id] FROM [users] WHERE [Username] = @Username;
    PRINT 'Test admin user updated.';
END
ELSE
BEGIN
    INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[TenantId],[CampusId],[IsActive],[CreatedAt],[IsDeleted])
    VALUES (@UserId, @Username, @Email, @FullName, @PasswordHash, @RoleId, @TenantId, @CampusId, 1, @Now, 0);
    PRINT 'Test admin user created.';
END

-- Assign every active department of the tenant; drop any assignment outside it.
UPDATE [admin_department_assignments] SET [RemovedAt] = @Now
WHERE [AdminUserId] = @UserId AND [RemovedAt] IS NULL
  AND [DepartmentId] NOT IN (SELECT [Id] FROM [departments] WHERE [TenantId] = @TenantId);

INSERT INTO [admin_department_assignments]([Id],[AdminUserId],[DepartmentId],[AssignedAt],[CreatedAt])
SELECT NEWID(), @UserId, d.[Id], @Now, @Now
FROM [departments] d
WHERE d.[IsActive] = 1 AND d.[TenantId] = @TenantId
  AND NOT EXISTS (SELECT 1 FROM [admin_department_assignments] a
                  WHERE a.[AdminUserId] = @UserId AND a.[DepartmentId] = d.[Id] AND a.[RemovedAt] IS NULL);

DECLARE @AssignedCount INT = (SELECT COUNT(*) FROM [admin_department_assignments] WHERE [AdminUserId] = @UserId AND [RemovedAt] IS NULL);
PRINT CONCAT('Department assignments: ', @AssignedCount);
GO
