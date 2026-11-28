/*
  Demo sign-in accounts (one login for demos, the role chosen at sign-in) — run after 01-School-Records.sql.

  The API's DemoLogin settings (off by default) turn the username "demo" into a shared sign-in: the app asks which role to
  open and the session belongs to that role's account. This script adds the accounts 01/02 do not create:
    demo         the shared sign-in itself (password EduSphere147); a Parent with no children, so on its own it opens nothing
    demo.admin   Admin of Tabsan Model School, assigned to all of its campuses (Gulberg, Johar Town, DHA)
    demo.parent  Parent of tmsgul.c9s1 (Gulberg, Class 9) and tmsjt.c4s2 (Johar Town, Class 4)
  The other roles open existing accounts: SuperAdmin superadmin, teacher tmsgul.maths, student tmsgul.c9s1.
  API settings (environment variables of the API, or the DemoLogin section of its appsettings):
    DemoLogin__Enabled=true
    DemoLogin__Accounts__0__Role=SuperAdmin  DemoLogin__Accounts__0__Username=superadmin
    DemoLogin__Accounts__1__Role=Admin       DemoLogin__Accounts__1__Username=demo.admin
    DemoLogin__Accounts__2__Role=Faculty     DemoLogin__Accounts__2__Username=tmsgul.maths
    DemoLogin__Accounts__3__Role=Student     DemoLogin__Accounts__3__Username=tmsgul.c9s1
    DemoLogin__Accounts__4__Role=Parent      DemoLogin__Accounts__4__Username=demo.parent
  Demo databases only: delete these accounts and leave DemoLogin off for a real institution. Safe to run more than once.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
-- The demo password EduSphere147, as every account of 01-School-Records.sql.
DECLARE @Hash NVARCHAR(512) = (SELECT TOP 1 [PasswordHash] FROM [users] WHERE [Username] = N'admin.tmsgul');
DECLARE @Tenant UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [tenants] WHERE [Code] = N'TMS');
DECLARE @Campus UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [campuses] WHERE [Code] = N'TMS-GUL');
DECLARE @AdminRole INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Admin');
DECLARE @ParentRole INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Parent');
DECLARE @School INT = 1; -- InstitutionType.School

IF @Hash IS NULL OR @Tenant IS NULL OR @Campus IS NULL OR @AdminRole IS NULL OR @ParentRole IS NULL
BEGIN
    RAISERROR('Run ../Schools-And-Campuses and 01-School-Records.sql first, and start the API once (it adds the Parent role).', 16, 1);
    RETURN;
END

DECLARE @Accounts TABLE ([Username] NVARCHAR(100), [Email] NVARCHAR(256), [FullName] NVARCHAR(200), [RoleId] INT);
INSERT INTO @Accounts VALUES
    (N'demo',        N'demo@tabsan.school',        N'Demo (choose a role)', @ParentRole),
    (N'demo.admin',  N'demo.admin@tabsan.school',  N'Demo School Admin',    @AdminRole),
    (N'demo.parent', N'demo.parent@tabsan.school', N'Demo Parent',          @ParentRole);

INSERT INTO [users] ([Id], [Username], [Email], [FullName], [PasswordHash], [RoleId], [TenantId], [CampusId], [InstitutionType],
                     [IsActive], [CreatedAt], [IsDeleted])
SELECT NEWID(), a.[Username], a.[Email], a.[FullName], @Hash, a.[RoleId], @Tenant, @Campus, @School, 1, @Now, 0
FROM @Accounts a
WHERE NOT EXISTS (SELECT 1 FROM [users] u WHERE u.[Username] = a.[Username]);
PRINT CONCAT(N'Demo accounts added: ', @@ROWCOUNT);

-- Existing rows: back to the demo state (active, demo password, no forced password change, no lockout).
UPDATE u SET [PasswordHash] = @Hash, [RoleId] = a.[RoleId], [IsActive] = 1, [IsDeleted] = 0, [MustChangePassword] = 0,
             [FailedLoginAttempts] = 0, [IsLockedOut] = 0, [LockedOutUntil] = NULL, [UpdatedAt] = @Now
FROM [users] u JOIN @Accounts a ON a.[Username] = u.[Username];

-- demo.admin: every active department (campus School section) of Tabsan Model School.
DECLARE @DemoAdmin UNIQUEIDENTIFIER = (SELECT [Id] FROM [users] WHERE [Username] = N'demo.admin');
INSERT INTO [admin_department_assignments] ([Id], [AdminUserId], [DepartmentId], [AssignedAt], [CreatedAt])
SELECT NEWID(), @DemoAdmin, d.[Id], @Now, @Now
FROM [departments] d
WHERE d.[TenantId] = @Tenant AND d.[IsActive] = 1 AND d.[IsDeleted] = 0
  AND NOT EXISTS (SELECT 1 FROM [admin_department_assignments] x
                  WHERE x.[AdminUserId] = @DemoAdmin AND x.[DepartmentId] = d.[Id] AND x.[RemovedAt] IS NULL);
DECLARE @AdminCampuses INT = (SELECT COUNT(*) FROM [admin_department_assignments] WHERE [AdminUserId] = @DemoAdmin AND [RemovedAt] IS NULL);
PRINT CONCAT(N'demo.admin campuses: ', @AdminCampuses);

-- demo.parent: two children on different campuses and in different classes.
DECLARE @DemoParent UNIQUEIDENTIFIER = (SELECT [Id] FROM [users] WHERE [Username] = N'demo.parent');
INSERT INTO [parent_student_links] ([Id], [ParentUserId], [StudentProfileId], [Relationship], [IsActive], [CreatedAt], [IsDeleted])
SELECT NEWID(), @DemoParent, p.[Id], N'Guardian', 1, @Now, 0
FROM [student_profiles] p JOIN [users] s ON s.[Id] = p.[UserId]
WHERE s.[Username] IN (N'tmsgul.c9s1', N'tmsjt.c4s2')
  AND NOT EXISTS (SELECT 1 FROM [parent_student_links] l WHERE l.[ParentUserId] = @DemoParent AND l.[StudentProfileId] = p.[Id]);
UPDATE [parent_student_links] SET [IsActive] = 1, [IsDeleted] = 0 WHERE [ParentUserId] = @DemoParent;
DECLARE @Children INT = (SELECT COUNT(*) FROM [parent_student_links] WHERE [ParentUserId] = @DemoParent AND [IsActive] = 1);
PRINT CONCAT(N'demo.parent children: ', @Children);
