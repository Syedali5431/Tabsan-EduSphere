SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

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
    RAISERROR('Failed to switch context to [Tabsan-EduSphere]. Aborting dummy data script.', 16, 1);
    RETURN;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Full dummy data script for demos.
  
  PREREQUISITES - MUST RUN FIRST IN THIS ORDER:
  1. 01-Schema-Current.sql      - Creates all tables and schema
  2. 02-Seed-Core.sql           - Seeds core data (roles, institutions, departments, users)
  3. 03-FullDummyData.sql       - This script: adds comprehensive test data
  
  After all three scripts complete successfully:
  4. 04-Maintenance-Indexes-And-Views.sql - Creates indexes and views
  5. 05-PostDeployment-Checks.sql         - Validates data integrity

    NOTE:
    - Seeded user password is EduSphere147 (stored as Argon2id hash in @PwdHash).
  - This script is idempotent (NOT EXISTS checks + stable GUID keys).
  - Requires database [Tabsan-EduSphere] to exist and schema to be created.
  - Requires core seed data (roles, institutions, etc.) to be inserted first.
*/

BEGIN TRY
BEGIN TRANSACTION;

-- VALIDATE CRITICAL PREREQUISITES - Check table existence FIRST
IF OBJECT_ID(N'[academic_programs]') IS NULL
BEGIN
    RAISERROR('ERROR: Table [academic_programs] does not exist. You MUST run scripts in this order: 01-Schema, 02-Seed, 03-DummyData', 16, 1);
    RETURN;
END;

-- Only check row count if table exists
DECLARE @DepartmentCount INT = (SELECT COUNT(1) FROM [departments]);
IF @DepartmentCount = 0
BEGIN
    RAISERROR('ERROR: Table [departments] is empty. You MUST run 02-Seed-Core.sql BEFORE running this script.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @PwdHash NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @UniTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @ColTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);
DECLARE @UniCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @ColCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);

IF OBJECT_ID(N'[tenants]') IS NOT NULL
BEGIN
    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @UniTenantId, N'UNI', N'University Tenant', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = @UniTenantId);

    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @ColTenantId, N'COL', N'College Tenant', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = @ColTenantId);

    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @SchTenantId, N'SCH', N'School Tenant', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = @SchTenantId);
END;

IF OBJECT_ID(N'[campuses]') IS NOT NULL
BEGIN
    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @UniCampusId, @UniTenantId, N'UNI-MAIN', N'University Main Campus', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = @UniCampusId);

    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @ColCampusId, @ColTenantId, N'COL-MAIN', N'College Main Campus', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = @ColCampusId);

    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @SchCampusId, @SchTenantId, N'SCH-MAIN', N'School Main Campus', 1, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = @SchCampusId);
END;

DECLARE @RoleSuperAdmin INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'SuperAdmin');
DECLARE @RoleAdmin INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Admin');
DECLARE @RoleFaculty INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Faculty');
DECLARE @RoleStudent INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Student');
DECLARE @RoleFinance INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Finance');
DECLARE @RoleParent INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Parent');
DECLARE @SuperAdminUserId UNIQUEIDENTIFIER = (
    SELECT TOP 1 [Id]
    FROM [users]
    WHERE [Username] = N'superadmin' OR [Email] = N'superadmin@demo.local'
    ORDER BY CASE WHEN [Username] = N'superadmin' THEN 0 ELSE 1 END
);

IF @SuperAdminUserId IS NULL
BEGIN
    SET @SuperAdminUserId = '66666666-6666-6666-6666-666666666601';
END;

DECLARE @RoleCount INT = (SELECT COUNT(1) FROM [roles] WHERE [Id] IN (@RoleSuperAdmin, @RoleAdmin, @RoleFaculty, @RoleStudent, @RoleFinance));
IF @RoleCount < 5
BEGIN
    RAISERROR('ERROR: Not all required roles found (including Finance). You MUST run 02-Seed-Core.sql BEFORE running this script.', 16, 1);
    RETURN;
END;

/* 0) Demo metadata table (custom object requested for demos) */
IF OBJECT_ID(N'[Tabsan-EduSphere]') IS NOT NULL
BEGIN
    INSERT INTO [Tabsan-EduSphere] ([Id], [DemoKey], [DemoValue], [CreatedAt], [UpdatedAt])
    SELECT '10101010-1010-1010-1010-101010101010', N'DemoDatasetVersion', N'FullDummyData-v13', @Now, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [Tabsan-EduSphere] x WHERE x.[DemoKey] = N'DemoDatasetVersion');

    INSERT INTO [Tabsan-EduSphere] ([Id], [DemoKey], [DemoValue], [CreatedAt], [UpdatedAt])
    SELECT '10101010-1010-1010-1010-101010101011', N'DemoSeededAtUtc', CONVERT(NVARCHAR(40), @Now, 127), @Now, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [Tabsan-EduSphere] x WHERE x.[DemoKey] = N'DemoSeededAtUtc');
    UPDATE [Tabsan-EduSphere]
    SET [DemoValue] = N'FullDummyData-v13',
        [UpdatedAt] = @Now
    WHERE [DemoKey] = N'DemoDatasetVersion';
END

/* 1) Departments - Expanded for School/College/University contexts */
DECLARE @Departments TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(200), Code NVARCHAR(20), InstitutionType INT);
INSERT INTO @Departments (Id, Name, Code, InstitutionType) VALUES
-- University Level (InstitutionType = 2)
('11111111-1111-1111-1111-111111111111', N'School of Computer Science', N'CS', 2),
('11111111-1111-1111-1111-111111111112', N'School of Business Administration', N'BUS', 2),
('11111111-1111-1111-1111-111111111113', N'School of Engineering', N'UENG', 2),
('11111111-1111-1111-1111-111111111114', N'School of Liberal Arts', N'ARTS', 2),
('11111111-1111-1111-1111-111111111115', N'School of Sciences', N'USCI', 2),
-- College Level (InstitutionType = 1)
('12222222-2222-2222-2222-222222222221', N'Commerce College', N'COMM', 1),
('12222222-2222-2222-2222-222222222222', N'Arts College', N'AC', 1),
('12222222-2222-2222-2222-222222222223', N'Science College', N'SC', 1),
-- School Level (InstitutionType = 0)
('13333333-3333-3333-3333-333333333331', N'Mathematics Department', N'MATH', 0),
('13333333-3333-3333-3333-333333333332', N'Science Department', N'SCI', 0),
('13333333-3333-3333-3333-333333333333', N'English Department', N'SENG', 0),
('13333333-3333-3333-3333-333333333334', N'Social Studies', N'SS', 0);

INSERT INTO [departments] ([Id], [Name], [Code], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT d.Id, d.Name, d.Code, 1, @Now, NULL, 0, NULL
FROM @Departments d
WHERE NOT EXISTS (SELECT 1 FROM [departments] x WHERE x.[Id] = d.Id);

IF COL_LENGTH('departments', 'InstitutionType') IS NOT NULL
BEGIN
    UPDATE d
    SET d.[Name] = src.[Name],
        d.[Code] = src.[Code],
        d.[IsActive] = 1,
        d.[InstitutionType] = src.[InstitutionType],
        d.[UpdatedAt] = @Now
    FROM [departments] d
    INNER JOIN @Departments src ON src.[Id] = d.[Id]
    WHERE d.[Name] <> src.[Name]
       OR d.[Code] <> src.[Code]
       OR d.[IsActive] = 0
       OR d.[InstitutionType] <> src.[InstitutionType];
END;

IF COL_LENGTH('departments', 'TenantId') IS NOT NULL AND COL_LENGTH('departments', 'CampusId') IS NOT NULL
BEGIN
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
    INNER JOIN @Departments src ON src.[Id] = d.[Id]
    WHERE d.[TenantId] IS NULL
       OR d.[CampusId] IS NULL
       OR d.[TenantId] <> CASE d.[InstitutionType] WHEN 2 THEN @UniTenantId WHEN 1 THEN @ColTenantId ELSE @SchTenantId END
       OR d.[CampusId] <> CASE d.[InstitutionType] WHEN 2 THEN @UniCampusId WHEN 1 THEN @ColCampusId ELSE @SchCampusId END;
END;

/* 2) Programs - Expanded across all institution types */
DECLARE @Programs TABLE (Id UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER, Name NVARCHAR(200), Code NVARCHAR(20), TotalSemesters INT);
INSERT INTO @Programs VALUES
-- University Programs
('22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111111', N'BS Computer Science', N'BSCS', 8),
('22222222-2222-2222-2222-222222222212', '11111111-1111-1111-1111-111111111111', N'BS Information Technology', N'BSIT', 8),
('22222222-2222-2222-2222-222222222213', '11111111-1111-1111-1111-111111111111', N'MCS', N'MCS', 4),
('22222222-2222-2222-2222-222222222214', '11111111-1111-1111-1111-111111111112', N'BBA', N'BBA', 8),
('22222222-2222-2222-2222-222222222215', '11111111-1111-1111-1111-111111111112', N'MBA', N'MBA', 4),
('22222222-2222-2222-2222-222222222216', '11111111-1111-1111-1111-111111111113', N'BS Mechanical Engineering', N'BSME', 8),
('22222222-2222-2222-2222-222222222217', '11111111-1111-1111-1111-111111111113', N'BS Civil Engineering', N'BSCE', 8),
('22222222-2222-2222-2222-222222222218', '11111111-1111-1111-1111-111111111114', N'English Diploma (1 Year)', N'ENG-1Y', 2),
('22222222-2222-2222-2222-222222222219', '11111111-1111-1111-1111-111111111114', N'BA History', N'BAHST', 8),
('22222222-2222-2222-2222-222222222220', '11111111-1111-1111-1111-111111111115', N'BS Biology', N'BSBIO', 8),
-- College Programs  
('22222222-2222-2222-2222-222222222321', '12222222-2222-2222-2222-222222222221', N'D Commerce', N'DCOM', 6),
('22222222-2222-2222-2222-222222222322', '12222222-2222-2222-2222-222222222221', N'A Accounting', N'AAC', 6),
('22222222-2222-2222-2222-222222222323', '12222222-2222-2222-2222-222222222222', N'D Arts', N'DART', 6),
('22222222-2222-2222-2222-222222222324', '12222222-2222-2222-2222-222222222223', N'FSC Pre-Engineering', N'FSC-ENG', 4),
('22222222-2222-2222-2222-222222222325', '12222222-2222-2222-2222-222222222223', N'ICS', N'ICS', 4),
('22222222-2222-2222-2222-222222222326', '12222222-2222-2222-2222-222222222223', N'FSC-Bio', N'FSC-BIO', 4),
-- School Programs
('22222222-2222-2222-2222-222222222431', '13333333-3333-3333-3333-333333333331', N'School Mathematics (Class 1-12)', N'SCH-MATH', 12),
('22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', N'School Science (Class 1-12)', N'SCH-SCI', 12),
('22222222-2222-2222-2222-222222222433', '13333333-3333-3333-3333-333333333333', N'School English (Class 1-12)', N'SCH-ENG', 12),
('22222222-2222-2222-2222-222222222434', '13333333-3333-3333-3333-333333333334', N'School Social Studies (Class 1-12)', N'SCH-SS', 12);

INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT p.Id, p.Name, p.Code, p.DepartmentId, p.TotalSemesters, 1, @Now, NULL, 0, NULL
FROM @Programs p
WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] x WHERE x.[Id] = p.Id);

/* 3) Semesters - Expanded time range */
DECLARE @Semesters TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(100), StartDate DATETIME2, EndDate DATETIME2, IsClosed BIT);
INSERT INTO @Semesters VALUES
('33333333-3333-3333-3333-333333333331', N'Fall 2024', '2024-08-15', '2024-12-31', 1),
('33333333-3333-3333-3333-333333333332', N'Spring 2025', '2025-01-15', '2025-06-15', 1),
('33333333-3333-3333-3333-333333333333', N'Fall 2025', '2025-08-15', '2025-12-31', 1),
('33333333-3333-3333-3333-333333333334', N'Spring 2026', '2026-01-15', '2026-06-15', 0),
('33333333-3333-3333-3333-333333333335', N'Fall 2026', '2026-08-15', '2026-12-31', 0),
('33333333-3333-3333-3333-333333333336', N'Spring 2027', '2027-01-15', '2027-06-15', 0);

INSERT INTO [semesters] ([Id], [Name], [StartDate], [EndDate], [IsClosed], [ClosedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT s.Id, s.Name, s.StartDate, s.EndDate, s.IsClosed,
       CASE WHEN s.IsClosed = 1 THEN DATEADD(day, 1, s.EndDate) ELSE NULL END,
       @Now, NULL, 0, NULL
FROM @Semesters s
WHERE NOT EXISTS (SELECT 1 FROM [semesters] x WHERE x.[Id] = s.Id);

/* 4) Users - Significantly expanded for all roles and institutions */
DECLARE @Users TABLE (
    Id UNIQUEIDENTIFIER,
    Username NVARCHAR(100),
    Email NVARCHAR(256),
    RoleId INT,
    DepartmentId UNIQUEIDENTIFIER NULL,
    InstitutionType INT NULL
);

-- SuperAdmins
INSERT INTO @Users VALUES
(@SuperAdminUserId, N'superadmin', N'superadmin@demo.local', @RoleSuperAdmin, NULL, NULL),

-- Finance users
('99999999-9999-9999-9999-999999999901', N'finance.uni.1', N'finance.uni.1@demo.local', @RoleFinance, '11111111-1111-1111-1111-111111111111', 2),
('99999999-9999-9999-9999-999999999902', N'finance.col.1', N'finance.col.1@demo.local', @RoleFinance, '12222222-2222-2222-2222-222222222221', 1),
('99999999-9999-9999-9999-999999999903', N'finance.sch.1', N'finance.sch.1@demo.local', @RoleFinance, '13333333-3333-3333-3333-333333333331', 0),

-- Admins - University departments
('66666666-6666-6666-6666-666666666611', N'admin.cs', N'admin.cs@demo.local', @RoleAdmin, '11111111-1111-1111-1111-111111111111', 2),
('66666666-6666-6666-6666-666666666612', N'admin.bus', N'admin.bus@demo.local', @RoleAdmin, '11111111-1111-1111-1111-111111111112', 2),
('66666666-6666-6666-6666-666666666613', N'admin.eng', N'admin.eng@demo.local', @RoleAdmin, '11111111-1111-1111-1111-111111111113', 2),
('66666666-6666-6666-6666-666666666614', N'admin.arts', N'admin.arts@demo.local', @RoleAdmin, '11111111-1111-1111-1111-111111111114', 2),
('66666666-6666-6666-6666-666666666615', N'admin.sci', N'admin.sci@demo.local', @RoleAdmin, '11111111-1111-1111-1111-111111111115', 2),
-- Admins - College departments
('66666666-6666-6666-6666-666666666621', N'admin.comm', N'admin.comm@demo.local', @RoleAdmin, '12222222-2222-2222-2222-222222222221', 1),
('66666666-6666-6666-6666-666666666622', N'admin.artcol', N'admin.artcol@demo.local', @RoleAdmin, '12222222-2222-2222-2222-222222222222', 1),
('66666666-6666-6666-6666-666666666623', N'admin.scicol', N'admin.scicol@demo.local', @RoleAdmin, '12222222-2222-2222-2222-222222222223', 1),
-- Admins - School departments
('66666666-6666-6666-6666-666666666631', N'admin.math', N'admin.math@demo.local', @RoleAdmin, '13333333-3333-3333-3333-333333333331', 0),
('66666666-6666-6666-6666-666666666632', N'admin.scisch', N'admin.scisch@demo.local', @RoleAdmin, '13333333-3333-3333-3333-333333333332', 0),

-- Faculty - University
('77777777-7777-7777-7777-777777777711', N'faculty.cs.1', N'faculty.cs.1@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111111', 2),
('77777777-7777-7777-7777-777777777712', N'faculty.cs.2', N'faculty.cs.2@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111111', 2),
('77777777-7777-7777-7777-777777777713', N'faculty.cs.3', N'faculty.cs.3@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111111', 2),
('77777777-7777-7777-7777-777777777714', N'faculty.bus.1', N'faculty.bus.1@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111112', 2),
('77777777-7777-7777-7777-777777777715', N'faculty.bus.2', N'faculty.bus.2@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111112', 2),
('77777777-7777-7777-7777-777777777716', N'faculty.eng.1', N'faculty.eng.1@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111113', 2),
('77777777-7777-7777-7777-777777777717', N'faculty.eng.2', N'faculty.eng.2@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111113', 2),
('77777777-7777-7777-7777-777777777718', N'faculty.arts.1', N'faculty.arts.1@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111114', 2),
('77777777-7777-7777-7777-777777777719', N'faculty.sci.1', N'faculty.sci.1@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111115', 2),
-- Faculty - College
('77777777-7777-7777-7777-777777777721', N'faculty.comm.1', N'faculty.comm.1@demo.local', @RoleFaculty, '12222222-2222-2222-2222-222222222221', 1),
('77777777-7777-7777-7777-777777777722', N'faculty.comm.2', N'faculty.comm.2@demo.local', @RoleFaculty, '12222222-2222-2222-2222-222222222221', 1),
('77777777-7777-7777-7777-777777777723', N'faculty.artcol.1', N'faculty.artcol.1@demo.local', @RoleFaculty, '12222222-2222-2222-2222-222222222222', 1),
-- Faculty - School
('77777777-7777-7777-7777-777777777731', N'faculty.math.1', N'faculty.math.1@demo.local', @RoleFaculty, '13333333-3333-3333-3333-333333333331', 0),
('77777777-7777-7777-7777-777777777732', N'faculty.scisch.1', N'faculty.scisch.1@demo.local', @RoleFaculty, '13333333-3333-3333-3333-333333333332', 0),

-- Students - 60+ students across all institutions
('88888888-8888-8888-8888-888888888811', N'student.cs.1', N'student.cs.1@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
('88888888-8888-8888-8888-888888888812', N'student.cs.2', N'student.cs.2@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
('88888888-8888-8888-8888-888888888813', N'student.cs.3', N'student.cs.3@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
('88888888-8888-8888-8888-888888888814', N'student.cs.4', N'student.cs.4@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
('88888888-8888-8888-8888-888888888815', N'student.cs.5', N'student.cs.5@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
('88888888-8888-8888-8888-888888888816', N'student.it.1', N'student.it.1@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
('88888888-8888-8888-8888-888888888817', N'student.it.2', N'student.it.2@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
('88888888-8888-8888-8888-888888888818', N'student.bus.1', N'student.bus.1@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111112', 2),
('88888888-8888-8888-8888-888888888819', N'student.bus.2', N'student.bus.2@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111112', 2),
('88888888-8888-8888-8888-888888888820', N'student.bus.3', N'student.bus.3@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111112', 2),
('88888888-8888-8888-8888-888888888821', N'student.eng.1', N'student.eng.1@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111113', 2),
('88888888-8888-8888-8888-888888888822', N'student.eng.2', N'student.eng.2@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111113', 2),
('88888888-8888-8888-8888-888888888823', N'student.arts.1', N'student.arts.1@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111114', 2),
('88888888-8888-8888-8888-888888888824', N'student.sci.1', N'student.sci.1@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111115', 2),
('88888888-8888-8888-8888-888888888825', N'student.lang.1', N'student.lang.1@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111114', 2),
-- College Students
('88888888-8888-8888-8888-888888888831', N'student.comm.1', N'student.comm.1@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222221', 1),
('88888888-8888-8888-8888-888888888832', N'student.comm.2', N'student.comm.2@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222221', 1),
('88888888-8888-8888-8888-888888888833', N'student.comm.3', N'student.comm.3@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222221', 1),
('88888888-8888-8888-8888-888888888834', N'student.artcol.1', N'student.artcol.1@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222222', 1),
('88888888-8888-8888-8888-888888888835', N'student.artcol.2', N'student.artcol.2@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222222', 1),
('88888888-8888-8888-8888-888888888851', N'student.ics.1', N'student.ics.1@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222223', 1),
('88888888-8888-8888-8888-888888888852', N'student.ics.2', N'student.ics.2@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222223', 1),
-- School Students
('88888888-8888-8888-8888-888888888841', N'student.math.1', N'student.math.1@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333331', 0),
('88888888-8888-8888-8888-888888888842', N'student.math.2', N'student.math.2@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333331', 0),
('88888888-8888-8888-8888-888888888843', N'student.scisch.1', N'student.scisch.1@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888844', N'student.scisch.2', N'student.scisch.2@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888845', N'student.scisch.3', N'student.scisch.3@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888846', N'student.scisch.4', N'student.scisch.4@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888847', N'student.scisch.5', N'student.scisch.5@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888848', N'student.scisch.6', N'student.scisch.6@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888849', N'student.scisch.7', N'student.scisch.7@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888850', N'student.scisch.8', N'student.scisch.8@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888856', N'student.scisch.9', N'student.scisch.9@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0),
('88888888-8888-8888-8888-888888888857', N'student.scisch.10', N'student.scisch.10@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333332', 0);

INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT u.Id, u.Username, u.Email, @PwdHash, u.RoleId, u.DepartmentId, u.InstitutionType, 1, NULL, @Now, NULL, 0, NULL
FROM @Users u
WHERE NOT EXISTS (SELECT 1 FROM [users] x WHERE x.[Id] = u.Id);

UPDATE u
SET u.[Username] = src.[Username],
    u.[Email] = src.[Email],
    u.[PasswordHash] = @PwdHash,
    u.[RoleId] = src.[RoleId],
    u.[DepartmentId] = src.[DepartmentId],
    u.[InstitutionType] = src.[InstitutionType],
    u.[IsActive] = 1,
    u.[UpdatedAt] = @Now
FROM [users] u
INNER JOIN @Users src ON src.[Id] = u.[Id]
WHERE u.[Username] <> src.[Username]
   OR u.[Email] <> src.[Email]
   OR u.[RoleId] <> src.[RoleId]
   OR ISNULL(CAST(u.[DepartmentId] AS NVARCHAR(36)), N'') <> ISNULL(CAST(src.[DepartmentId] AS NVARCHAR(36)), N'')
   OR ISNULL(u.[InstitutionType], -1) <> ISNULL(src.[InstitutionType], -1)
   OR u.[IsActive] = 0;

/* 4.0.1) Ensure each institute department has role coverage users */
INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT
    NEWID(),
    CONCAT(N'auto.admin.', LOWER(d.[Code])),
    CONCAT(N'auto.admin.', LOWER(d.[Code]), N'@demo.local'),
    @PwdHash,
    @RoleAdmin,
    d.[Id],
    d.[InstitutionType],
    1,
    NULL,
    @Now,
    NULL,
    0,
    NULL
FROM @Departments d
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] u
    WHERE u.[RoleId] = @RoleAdmin
      AND u.[DepartmentId] = d.[Id]
      AND u.[IsDeleted] = 0
);

INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT
    NEWID(),
    CONCAT(N'auto.faculty.', LOWER(d.[Code])),
    CONCAT(N'auto.faculty.', LOWER(d.[Code]), N'@demo.local'),
    @PwdHash,
    @RoleFaculty,
    d.[Id],
    d.[InstitutionType],
    1,
    NULL,
    @Now,
    NULL,
    0,
    NULL
FROM @Departments d
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] u
    WHERE u.[RoleId] = @RoleFaculty
      AND u.[DepartmentId] = d.[Id]
      AND u.[IsDeleted] = 0
);

INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT
    NEWID(),
    CONCAT(N'auto.finance.', LOWER(d.[Code])),
    CONCAT(N'auto.finance.', LOWER(d.[Code]), N'@demo.local'),
    @PwdHash,
    @RoleFinance,
    d.[Id],
    d.[InstitutionType],
    1,
    NULL,
    @Now,
    NULL,
    0,
    NULL
FROM @Departments d
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] u
    WHERE u.[RoleId] = @RoleFinance
      AND u.[DepartmentId] = d.[Id]
      AND u.[IsDeleted] = 0
);

;WITH N AS
(
    SELECT TOP (6) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
)
INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT
    NEWID(),
    CONCAT(N'auto.', LOWER(d.[Code]), N'.student.', RIGHT(N'00' + CAST(n.n AS NVARCHAR(10)), 2)),
    CONCAT(N'auto.', LOWER(d.[Code]), N'.student.', RIGHT(N'00' + CAST(n.n AS NVARCHAR(10)), 2), N'@demo.local'),
    @PwdHash,
    @RoleStudent,
    d.[Id],
    d.[InstitutionType],
    1,
    NULL,
    @Now,
    NULL,
    0,
    NULL
FROM @Departments d
INNER JOIN N ON 1 = 1
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] u
    WHERE u.[Username] = CONCAT(N'auto.', LOWER(d.[Code]), N'.student.', RIGHT(N'00' + CAST(n.n AS NVARCHAR(10)), 2))
);

IF COL_LENGTH('users', 'PhoneNumber') IS NOT NULL
BEGIN
    ;WITH OrderedUsers AS (
        SELECT
            u.[Id],
            rn = ROW_NUMBER() OVER (ORDER BY u.[Username])
        FROM [users] u
        WHERE u.[Id] IN (SELECT [Id] FROM @Users)
    )
    UPDATE u
    SET u.[PhoneNumber] = CONCAT(N'+614', RIGHT(CONCAT(N'0000000', CAST(1000000 + o.rn AS NVARCHAR(16))), 7)),
        u.[UpdatedAt] = @Now
    FROM [users] u
    INNER JOIN OrderedUsers o ON o.[Id] = u.[Id];
END;

/* 4.1) Admin and faculty department assignments */
IF OBJECT_ID(N'[admin_department_assignments]') IS NOT NULL
BEGIN
    DECLARE @AdminDepartmentAssignments TABLE (Id UNIQUEIDENTIFIER, AdminUserId UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER);
    INSERT INTO @AdminDepartmentAssignments VALUES
    ('26111111-1111-1111-1111-111111111611', '66666666-6666-6666-6666-666666666611', '11111111-1111-1111-1111-111111111111'),
    ('26111111-1111-1111-1111-111111111612', '66666666-6666-6666-6666-666666666612', '11111111-1111-1111-1111-111111111112'),
    ('26111111-1111-1111-1111-111111111613', '66666666-6666-6666-6666-666666666613', '11111111-1111-1111-1111-111111111113');

    INSERT INTO [admin_department_assignments] ([Id], [AdminUserId], [DepartmentId], [AssignedAt], [RemovedAt], [CreatedAt], [UpdatedAt])
    SELECT a.[Id], a.[AdminUserId], a.[DepartmentId], @Now, NULL, @Now, NULL
    FROM @AdminDepartmentAssignments a
    WHERE NOT EXISTS (SELECT 1 FROM [admin_department_assignments] x WHERE x.[Id] = a.[Id]);
END

IF OBJECT_ID(N'[faculty_department_assignments]') IS NOT NULL
BEGIN
    DECLARE @FacultyDepartmentAssignments TABLE (Id UNIQUEIDENTIFIER, FacultyUserId UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER);
    INSERT INTO @FacultyDepartmentAssignments VALUES
    ('27111111-1111-1111-1111-111111111621', '66666666-6666-6666-6666-666666666621', '11111111-1111-1111-1111-111111111111'),
    ('27111111-1111-1111-1111-111111111622', '66666666-6666-6666-6666-666666666622', '11111111-1111-1111-1111-111111111111'),
    ('27111111-1111-1111-1111-111111111623', '66666666-6666-6666-6666-666666666623', '11111111-1111-1111-1111-111111111112'),
    ('27111111-1111-1111-1111-111111111624', '66666666-6666-6666-6666-666666666624', '11111111-1111-1111-1111-111111111113');

    INSERT INTO [faculty_department_assignments] ([Id], [FacultyUserId], [DepartmentId], [AssignedAt], [RemovedAt], [CreatedAt], [UpdatedAt])
    SELECT a.[Id], a.[FacultyUserId], a.[DepartmentId], @Now, NULL, @Now, NULL
    FROM @FacultyDepartmentAssignments a
    WHERE NOT EXISTS (SELECT 1 FROM [faculty_department_assignments] x WHERE x.[Id] = a.[Id]);
END

/* 5) Student profiles - Massive expansion (60+ profiles) */
DECLARE @StudentProfiles TABLE (
    Id UNIQUEIDENTIFIER,
    UserId UNIQUEIDENTIFIER,
    RegistrationNumber NVARCHAR(50),
    ProgramId UNIQUEIDENTIFIER,
    DepartmentId UNIQUEIDENTIFIER,
    CurrentSemesterNumber INT
);

-- University CS Students
INSERT INTO @StudentProfiles VALUES
('99999999-9999-9999-9999-999999999911', '88888888-8888-8888-8888-888888888811', N'2026-CS-0001', '22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111111', 2),
('99999999-9999-9999-9999-999999999912', '88888888-8888-8888-8888-888888888812', N'2026-CS-0002', '22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111111', 2),
('99999999-9999-9999-9999-999999999913', '88888888-8888-8888-8888-888888888813', N'2026-CS-0003', '22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111111', 2),
('99999999-9999-9999-9999-999999999914', '88888888-8888-8888-8888-888888888814', N'2026-CS-0004', '22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111111', 4),
('99999999-9999-9999-9999-999999999915', '88888888-8888-8888-8888-888888888815', N'2026-CS-0005', '22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111111', 3),
('99999999-9999-9999-9999-999999999916', '88888888-8888-8888-8888-888888888816', N'2026-IT-0001', '22222222-2222-2222-2222-222222222212', '11111111-1111-1111-1111-111111111111', 2),
('99999999-9999-9999-9999-999999999917', '88888888-8888-8888-8888-888888888817', N'2026-IT-0002', '22222222-2222-2222-2222-222222222212', '11111111-1111-1111-1111-111111111111', 2),
-- University BUS Students
('99999999-9999-9999-9999-999999999918', '88888888-8888-8888-8888-888888888818', N'2026-BBA-0001', '22222222-2222-2222-2222-222222222214', '11111111-1111-1111-1111-111111111112', 2),
('99999999-9999-9999-9999-999999999919', '88888888-8888-8888-8888-888888888819', N'2026-BBA-0002', '22222222-2222-2222-2222-222222222214', '11111111-1111-1111-1111-111111111112', 2),
('99999999-9999-9999-9999-999999999920', '88888888-8888-8888-8888-888888888820', N'2026-BBA-0003', '22222222-2222-2222-2222-222222222214', '11111111-1111-1111-1111-111111111112', 3),
-- University ENG Students
('99999999-9999-9999-9999-999999999921', '88888888-8888-8888-8888-888888888821', N'2026-ME-0001', '22222222-2222-2222-2222-222222222216', '11111111-1111-1111-1111-111111111113', 2),
('99999999-9999-9999-9999-999999999922', '88888888-8888-8888-8888-888888888822', N'2026-CE-0001', '22222222-2222-2222-2222-222222222217', '11111111-1111-1111-1111-111111111113', 3),
-- University ARTS Students
('99999999-9999-9999-9999-999999999923', '88888888-8888-8888-8888-888888888823', N'2026-BAENG-0001', '22222222-2222-2222-2222-222222222218', '11111111-1111-1111-1111-111111111114', 2),
-- University SCI Students
('99999999-9999-9999-9999-999999999924', '88888888-8888-8888-8888-888888888824', N'2026-BIO-0001', '22222222-2222-2222-2222-222222222220', '11111111-1111-1111-1111-111111111115', 2),
('99999999-9999-9999-9999-999999999925', '88888888-8888-8888-8888-888888888825', N'2026-LANG-0001', '22222222-2222-2222-2222-222222222218', '11111111-1111-1111-1111-111111111114', 2),
-- College COMM Students
('99999999-9999-9999-9999-999999999931', '88888888-8888-8888-8888-888888888831', N'2026-COMM-0001', '22222222-2222-2222-2222-222222222321', '12222222-2222-2222-2222-222222222221', 2),
('99999999-9999-9999-9999-999999999932', '88888888-8888-8888-8888-888888888832', N'2026-COMM-0002', '22222222-2222-2222-2222-222222222321', '12222222-2222-2222-2222-222222222221', 2),
('99999999-9999-9999-9999-999999999933', '88888888-8888-8888-8888-888888888833', N'2026-ACC-0001', '22222222-2222-2222-2222-222222222322', '12222222-2222-2222-2222-222222222221', 1),
-- College ARTS Students
('99999999-9999-9999-9999-999999999934', '88888888-8888-8888-8888-888888888834', N'2026-DART-0001', '22222222-2222-2222-2222-222222222323', '12222222-2222-2222-2222-222222222222', 2),
('99999999-9999-9999-9999-999999999935', '88888888-8888-8888-8888-888888888835', N'2026-DART-0002', '22222222-2222-2222-2222-222222222323', '12222222-2222-2222-2222-222222222222', 1),
('99999999-9999-9999-9999-999999999953', '88888888-8888-8888-8888-888888888851', N'2026-ICS-0001', '22222222-2222-2222-2222-222222222325', '12222222-2222-2222-2222-222222222223', 1),
('99999999-9999-9999-9999-999999999954', '88888888-8888-8888-8888-888888888852', N'2026-ICS-0002', '22222222-2222-2222-2222-222222222325', '12222222-2222-2222-2222-222222222223', 2),
-- School MATH Students
('99999999-9999-9999-9999-999999999941', '88888888-8888-8888-8888-888888888841', N'2026-MAT-M-001', '22222222-2222-2222-2222-222222222431', '13333333-3333-3333-3333-333333333331', 1),
('99999999-9999-9999-9999-999999999942', '88888888-8888-8888-8888-888888888842', N'2026-MAT-M-002', '22222222-2222-2222-2222-222222222431', '13333333-3333-3333-3333-333333333331', 2),
-- School SCI Students
('99999999-9999-9999-9999-999999999943', '88888888-8888-8888-8888-888888888843', N'2026-MAT-S-001', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 1),
('99999999-9999-9999-9999-999999999944', '88888888-8888-8888-8888-888888888844', N'2026-MAT-S-002', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 2),
('99999999-9999-9999-9999-999999999945', '88888888-8888-8888-8888-888888888845', N'2026-SCI-S-003', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 3),
('99999999-9999-9999-9999-999999999946', '88888888-8888-8888-8888-888888888846', N'2026-SCI-S-004', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 4),
('99999999-9999-9999-9999-999999999947', '88888888-8888-8888-8888-888888888847', N'2026-SCI-S-005', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 5),
('99999999-9999-9999-9999-999999999948', '88888888-8888-8888-8888-888888888848', N'2026-SCI-S-006', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 6),
('99999999-9999-9999-9999-999999999949', '88888888-8888-8888-8888-888888888849', N'2026-SCI-S-007', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 7),
('99999999-9999-9999-9999-999999999950', '88888888-8888-8888-8888-888888888850', N'2026-SCI-S-008', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 8),
('99999999-9999-9999-9999-999999999951', '88888888-8888-8888-8888-888888888856', N'2026-SCI-S-009', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 9),
('99999999-9999-9999-9999-999999999952', '88888888-8888-8888-8888-888888888857', N'2026-SCI-S-010', '22222222-2222-2222-2222-222222222432', '13333333-3333-3333-3333-333333333332', 10);

INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT sp.Id, sp.UserId, sp.RegistrationNumber, sp.ProgramId, sp.DepartmentId, '2026-01-15', 3.20, sp.CurrentSemesterNumber, @Now, NULL, 0, NULL
FROM @StudentProfiles sp
WHERE NOT EXISTS (SELECT 1 FROM [student_profiles] x WHERE x.[Id] = sp.Id);

/* 5.1) High-volume multi-institute dummy expansion */
DECLARE @BulkTarget TABLE
(
    InstitutionCode NVARCHAR(10),
    InstitutionType INT,
    DepartmentId UNIQUEIDENTIFIER,
    DepartmentCode NVARCHAR(20),
    ProgramId UNIQUEIDENTIFIER,
    ProgramCode NVARCHAR(20),
    StudentCount INT,
    FacultyCount INT,
    AdminCount INT
);

INSERT INTO @BulkTarget (InstitutionCode, InstitutionType, DepartmentId, DepartmentCode, ProgramId, ProgramCode, StudentCount, FacultyCount, AdminCount)
SELECT
    CASE d.[InstitutionType] WHEN 2 THEN N'UNI' WHEN 1 THEN N'COL' ELSE N'SCH' END,
    d.[InstitutionType],
    d.[Id],
    d.[Code],
    p.[Id],
    p.[Code],
    CASE d.[InstitutionType] WHEN 2 THEN 80 WHEN 1 THEN 64 ELSE 56 END,
    CASE d.[InstitutionType] WHEN 2 THEN 10 WHEN 1 THEN 8 ELSE 6 END,
    CASE d.[InstitutionType] WHEN 2 THEN 4 WHEN 1 THEN 3 ELSE 2 END
FROM @Programs p
INNER JOIN @Departments d ON d.[Id] = p.[DepartmentId];

;WITH N AS
(
    SELECT TOP (120) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b
)
INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT NEWID(),
       CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.student.', RIGHT(N'0000' + CAST(n.n AS NVARCHAR(10)), 4)),
       CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.student.', RIGHT(N'0000' + CAST(n.n AS NVARCHAR(10)), 4), N'@demo.local'),
       @PwdHash,
       @RoleStudent,
       t.DepartmentId,
       t.InstitutionType,
       1,
       NULL,
       @Now,
       NULL,
       0,
       NULL
FROM @BulkTarget t
INNER JOIN N ON N.n <= t.StudentCount
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] x
    WHERE x.[Username] = CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.student.', RIGHT(N'0000' + CAST(n.n AS NVARCHAR(10)), 4))
);

;WITH N AS
(
    SELECT TOP (20) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a
)
INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT NEWID(),
       CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.faculty.', RIGHT(N'000' + CAST(n.n AS NVARCHAR(10)), 3)),
       CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.faculty.', RIGHT(N'000' + CAST(n.n AS NVARCHAR(10)), 3), N'@demo.local'),
       @PwdHash,
       @RoleFaculty,
       t.DepartmentId,
       t.InstitutionType,
       1,
       NULL,
       @Now,
       NULL,
       0,
       NULL
FROM @BulkTarget t
INNER JOIN N ON N.n <= t.FacultyCount
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] x
    WHERE x.[Username] = CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.faculty.', RIGHT(N'000' + CAST(n.n AS NVARCHAR(10)), 3))
);

;WITH N AS
(
    SELECT TOP (10) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
)
INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT NEWID(),
       CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.admin.', RIGHT(N'00' + CAST(n.n AS NVARCHAR(10)), 2)),
       CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.admin.', RIGHT(N'00' + CAST(n.n AS NVARCHAR(10)), 2), N'@demo.local'),
       @PwdHash,
       @RoleAdmin,
       t.DepartmentId,
       t.InstitutionType,
       1,
       NULL,
       @Now,
       NULL,
       0,
       NULL
FROM @BulkTarget t
INNER JOIN N ON N.n <= t.AdminCount
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] x
    WHERE x.[Username] = CONCAT(N'bulk.', LOWER(t.InstitutionCode), N'.', LOWER(t.DepartmentCode), N'.', LOWER(t.ProgramCode), N'.admin.', RIGHT(N'00' + CAST(n.n AS NVARCHAR(10)), 2))
);

INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT NEWID(),
       u.[Id],
       CONCAT(N'BULK-', t.InstitutionCode, N'-', t.DepartmentCode, N'-', t.ProgramCode, N'-', RIGHT(N'0000' + CAST(ROW_NUMBER() OVER (PARTITION BY t.InstitutionCode, t.DepartmentCode, t.ProgramCode ORDER BY u.[Username]) AS NVARCHAR(10)), 4)),
       t.ProgramId,
       t.DepartmentId,
       DATEADD(day, -ABS(CHECKSUM(u.[Id])) % 540, @Now),
       CAST(2.50 + (ABS(CHECKSUM(u.[Id])) % 150) / 100.0 AS DECIMAL(4,2)),
      ((ROW_NUMBER() OVER (PARTITION BY t.ProgramId ORDER BY u.[Username]) - 1)
          % CASE
               WHEN t.InstitutionType = 0 THEN 12
               WHEN p.[TotalSemesters] > 0 THEN p.[TotalSemesters]
               ELSE 1
            END) + 1,
       @Now,
       NULL,
       0,
       NULL
FROM [users] u
INNER JOIN @BulkTarget t
        ON t.DepartmentId = u.[DepartmentId]
     AND u.[Username] LIKE CONCAT(N'bulk.%.', LOWER(t.[ProgramCode]), N'.student.%')
INNER JOIN @Programs p ON p.[Id] = t.[ProgramId]
WHERE u.[Username] LIKE N'bulk.%.student.%'
  AND NOT EXISTS (SELECT 1 FROM [student_profiles] sp WHERE sp.[UserId] = u.[Id]);

/* 5.2) Auto student profiles for department-wide institute users */
;WITH DepartmentPrograms AS
(
    SELECT
        ap.[DepartmentId],
        ap.[Id] AS ProgramId,
        ap.[TotalSemesters],
        ProgramOrdinal = ROW_NUMBER() OVER (PARTITION BY ap.[DepartmentId] ORDER BY ap.[Code], ap.[Id]),
        ProgramCount = COUNT(*) OVER (PARTITION BY ap.[DepartmentId])
    FROM [academic_programs] ap
), AutoStudents AS
(
    SELECT
        u.[Id] AS UserId,
        u.[DepartmentId],
        StudentOrdinal = ROW_NUMBER() OVER (PARTITION BY u.[DepartmentId] ORDER BY u.[Username])
    FROM [users] u
    WHERE u.[Username] LIKE N'auto.%.student.%'
      AND NOT EXISTS (SELECT 1 FROM [student_profiles] sp WHERE sp.[UserId] = u.[Id])
)
INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT
    NEWID(),
    s.[UserId],
    CONCAT(N'AUTO-', d.[Code], N'-', RIGHT(N'0000' + CAST(s.[StudentOrdinal] AS NVARCHAR(10)), 4)),
    p.[ProgramId],
    s.[DepartmentId],
    DATEADD(day, -((s.[StudentOrdinal] * 17) % 480), @Now),
    CAST(2.20 + ((s.[StudentOrdinal] % 180) / 100.0) AS DECIMAL(4,2)),
    ((s.[StudentOrdinal] - 1) % CASE WHEN p.[TotalSemesters] > 0 THEN p.[TotalSemesters] ELSE 1 END) + 1,
    @Now,
    NULL,
    0,
    NULL
FROM AutoStudents s
INNER JOIN @Departments d ON d.[Id] = s.[DepartmentId]
INNER JOIN DepartmentPrograms p
    ON p.[DepartmentId] = s.[DepartmentId]
   AND p.[ProgramOrdinal] = ((s.[StudentOrdinal] - 1) % p.[ProgramCount]) + 1;

/* 6) Courses - Massive expansion (50+ courses) */
DECLARE @Courses TABLE (Id UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER, Title NVARCHAR(200), Code NVARCHAR(20), CreditHours INT);
INSERT INTO @Courses VALUES
-- University CS Courses
('44444444-4444-4444-4444-444444444401', '11111111-1111-1111-1111-111111111111', N'Programming Fundamentals', N'CS-101', 3),
('44444444-4444-4444-4444-444444444402', '11111111-1111-1111-1111-111111111111', N'Data Structures', N'CS-201', 3),
('44444444-4444-4444-4444-444444444403', '11111111-1111-1111-1111-111111111111', N'Algorithms', N'CS-202', 3),
('44444444-4444-4444-4444-444444444404', '11111111-1111-1111-1111-111111111111', N'Database Systems', N'CS-301', 4),
('44444444-4444-4444-4444-444444444405', '11111111-1111-1111-1111-111111111111', N'Web Development', N'CS-302', 3),
('44444444-4444-4444-4444-444444444406', '11111111-1111-1111-1111-111111111111', N'Software Engineering', N'CS-401', 3),
('44444444-4444-4444-4444-444444444407', '11111111-1111-1111-1111-111111111111', N'Artificial Intelligence', N'CS-402', 3),
-- University BUS Courses
('44444444-4444-4444-4444-444444444408', '11111111-1111-1111-1111-111111111112', N'Principles of Management', N'BUS-101', 3),
('44444444-4444-4444-4444-444444444409', '11111111-1111-1111-1111-111111111112', N'Marketing Fundamentals', N'BUS-201', 3),
('44444444-4444-4444-4444-444444444410', '11111111-1111-1111-1111-111111111112', N'Financial Accounting', N'BUS-301', 4),
('44444444-4444-4444-4444-444444444411', '11111111-1111-1111-1111-111111111112', N'Managerial Accounting', N'BUS-302', 3),
('44444444-4444-4444-4444-444444444412', '11111111-1111-1111-1111-111111111112', N'Economics', N'BUS-103', 3),
-- University ENG Courses
('44444444-4444-4444-4444-444444444413', '11111111-1111-1111-1111-111111111113', N'Mechanical Engineering I', N'ENG-101', 4),
('44444444-4444-4444-4444-444444444414', '11111111-1111-1111-1111-111111111113', N'Thermodynamics', N'ENG-201', 3),
('44444444-4444-4444-4444-444444444415', '11111111-1111-1111-1111-111111111113', N'Fluid Mechanics', N'ENG-202', 3),
-- University ARTS Courses
('44444444-4444-4444-4444-444444444416', '11111111-1111-1111-1111-111111111114', N'English Composition', N'ENG-101', 3),
('44444444-4444-4444-4444-444444444417', '11111111-1111-1111-1111-111111111114', N'World Literature', N'ENG-201', 3),
('44444444-4444-4444-4444-444444444418', '11111111-1111-1111-1111-111111111114', N'Critical Analysis', N'ENG-301', 3),
('44444444-4444-4444-4444-444444444437', '11111111-1111-1111-1111-111111111114', N'Language Learning', N'LANG-101', 3),
-- University SCI Courses
('44444444-4444-4444-4444-444444444419', '11111111-1111-1111-1111-111111111115', N'Biology I', N'BIO-101', 4),
('44444444-4444-4444-4444-444444444420', '11111111-1111-1111-1111-111111111115', N'Organic Chemistry', N'CHEM-201', 4),
('44444444-4444-4444-4444-444444444421', '11111111-1111-1111-1111-111111111115', N'Physics I', N'PHYS-101', 4),
-- College COMM Courses
('44444444-4444-4444-4444-444444444422', '12222222-2222-2222-2222-222222222221', N'Commerce Fundamentals', N'COM-101', 3),
('44444444-4444-4444-4444-444444444423', '12222222-2222-2222-2222-222222222221', N'Business Law', N'COM-201', 3),
('44444444-4444-4444-4444-444444444424', '12222222-2222-2222-2222-222222222221', N'Accounting Basics', N'COM-202', 3),
-- College ARTS Courses
('44444444-4444-4444-4444-444444444425', '12222222-2222-2222-2222-222222222222', N'History of India', N'ART-101', 3),
('44444444-4444-4444-4444-444444444426', '12222222-2222-2222-2222-222222222222', N'Indian Philosophy', N'ART-201', 3),
('44444444-4444-4444-4444-444444444427', '12222222-2222-2222-2222-222222222222', N'Classical Literature', N'ART-202', 3),
-- College SCI Courses (FSC/ICS tracks)
('44444444-4444-4444-4444-444444444434', '12222222-2222-2222-2222-222222222223', N'Applied Mathematics', N'SCI-COL-101', 3),
('44444444-4444-4444-4444-444444444435', '12222222-2222-2222-2222-222222222223', N'Computer Fundamentals', N'SCI-COL-102', 3),
('44444444-4444-4444-4444-444444444436', '12222222-2222-2222-2222-222222222223', N'Biology Fundamentals', N'SCI-COL-103', 3),
-- School MATH Courses
('44444444-4444-4444-4444-444444444428', '13333333-3333-3333-3333-333333333331', N'Algebra', N'MATH-101', 2),
('44444444-4444-4444-4444-444444444429', '13333333-3333-3333-3333-333333333331', N'Geometry', N'MATH-102', 2),
('44444444-4444-4444-4444-444444444430', '13333333-3333-3333-3333-333333333331', N'Trigonometry', N'MATH-103', 2),
-- School SCI Courses
('44444444-4444-4444-4444-444444444431', '13333333-3333-3333-3333-333333333332', N'General Science', N'SCI-101', 3),
('44444444-4444-4444-4444-444444444432', '13333333-3333-3333-3333-333333333332', N'Physics Basics', N'SCI-102', 2),
('44444444-4444-4444-4444-444444444433', '13333333-3333-3333-3333-333333333332', N'Chemistry Basics', N'SCI-103', 2);

INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT c.Id, c.Title, c.Code, c.CreditHours, c.DepartmentId, 1, @Now, NULL, 0, NULL
FROM @Courses c
WHERE NOT EXISTS (SELECT 1 FROM [courses] x WHERE x.[Id] = c.Id);

/* 6.1) Buildings, rooms, and timetable parity coverage */
IF OBJECT_ID(N'[buildings]') IS NOT NULL
BEGIN
    DECLARE @Buildings TABLE (Id UNIQUEIDENTIFIER, [Name] NVARCHAR(100), [Code] NVARCHAR(20), InstitutionType INT, TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER);
    INSERT INTO @Buildings VALUES
    ('23232323-2323-2323-2323-232323232301', N'University Block A', N'BLD-U1', 2, @UniTenantId, @UniCampusId),
    ('23232323-2323-2323-2323-232323232302', N'College Commerce Block', N'BLD-C1', 1, @ColTenantId, @ColCampusId),
    ('23232323-2323-2323-2323-232323232303', N'School Language Block', N'BLD-S1', 0, @SchTenantId, @SchCampusId);

    INSERT INTO [buildings] ([Id], [TenantId], [CampusId], [Name], [Code], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT b.[Id], b.[TenantId], b.[CampusId], b.[Name], b.[Code], 1, @Now, NULL, 0, NULL
    FROM @Buildings b
    WHERE NOT EXISTS (SELECT 1 FROM [buildings] x WHERE x.[Id] = b.[Id]);
END

IF OBJECT_ID(N'[rooms]') IS NOT NULL
BEGIN
    DECLARE @Rooms TABLE (Id UNIQUEIDENTIFIER, [Number] NVARCHAR(50), BuildingId UNIQUEIDENTIFIER, Capacity INT, TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER);
    INSERT INTO @Rooms VALUES
    ('24242424-2424-2424-2424-242424242401', N'U-101', '23232323-2323-2323-2323-232323232301', 70, @UniTenantId, @UniCampusId),
    ('24242424-2424-2424-2424-242424242402', N'U-201', '23232323-2323-2323-2323-232323232301', 55, @UniTenantId, @UniCampusId),
    ('24242424-2424-2424-2424-242424242403', N'C-101', '23232323-2323-2323-2323-232323232302', 60, @ColTenantId, @ColCampusId),
    ('24242424-2424-2424-2424-242424242404', N'C-202', '23232323-2323-2323-2323-232323232302', 45, @ColTenantId, @ColCampusId),
    ('24242424-2424-2424-2424-242424242405', N'S-101', '23232323-2323-2323-2323-232323232303', 40, @SchTenantId, @SchCampusId),
    ('24242424-2424-2424-2424-242424242406', N'S-102', '23232323-2323-2323-2323-232323232303', 35, @SchTenantId, @SchCampusId);

    INSERT INTO [rooms] ([Id], [TenantId], [CampusId], [Number], [BuildingId], [Capacity], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT r.[Id], r.[TenantId], r.[CampusId], r.[Number], r.[BuildingId], r.[Capacity], 1, @Now, NULL, 0, NULL
    FROM @Rooms r
    WHERE NOT EXISTS (SELECT 1 FROM [rooms] x WHERE x.[Id] = r.[Id]);
END

IF OBJECT_ID(N'[timetables]') IS NOT NULL
BEGIN
    DECLARE @Timetables TABLE (Id UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER, AcademicProgramId UNIQUEIDENTIFIER, SemesterId UNIQUEIDENTIFIER, EffectiveDate DATE, SemesterNumber INT);
    INSERT INTO @Timetables VALUES
    ('25252525-2525-2525-2525-252525252501', '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333334', '2026-01-15', 2),
    ('25252525-2525-2525-2525-252525252502', '11111111-1111-1111-1111-111111111112', '22222222-2222-2222-2222-222222222214', '33333333-3333-3333-3333-333333333334', '2026-01-15', 2),
    ('25252525-2525-2525-2525-252525252503', '11111111-1111-1111-1111-111111111113', '22222222-2222-2222-2222-222222222216', '33333333-3333-3333-3333-333333333334', '2026-01-15', 2),
    ('25252525-2525-2525-2525-252525252504', '11111111-1111-1111-1111-111111111114', '22222222-2222-2222-2222-222222222218', '33333333-3333-3333-3333-333333333334', '2026-01-15', 2);

    INSERT INTO [timetables] ([Id], [DepartmentId], [AcademicProgramId], [SemesterId], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt], [EffectiveDate], [SemesterNumber])
    SELECT t.[Id], t.[DepartmentId], t.[AcademicProgramId], t.[SemesterId], 1, @Now, @Now, NULL, 0, NULL, t.[EffectiveDate], t.[SemesterNumber]
    FROM @Timetables t
    WHERE NOT EXISTS (SELECT 1 FROM [timetables] x WHERE x.[Id] = t.[Id]);
END

IF OBJECT_ID(N'[timetable_entries]') IS NOT NULL
BEGIN
    DECLARE @TimetableEntries TABLE (
        Id UNIQUEIDENTIFIER,
        TimetableId UNIQUEIDENTIFIER,
        DayOfWeek INT,
        StartTime TIME,
        EndTime TIME,
        SubjectName NVARCHAR(200),
        RoomNumber NVARCHAR(50),
        FacultyName NVARCHAR(200),
        RoomId UNIQUEIDENTIFIER,
        BuildingId UNIQUEIDENTIFIER,
        CourseId UNIQUEIDENTIFIER,
        FacultyUserId UNIQUEIDENTIFIER
    );

    INSERT INTO @TimetableEntries VALUES
    ('26262626-2626-2626-2626-262626262601', '25252525-2525-2525-2525-252525252501', 1, '09:00:00', '10:30:00', N'Programming Fundamentals', N'U-101', N'Faculty IT 1', '24242424-2424-2424-2424-242424242401', '23232323-2323-2323-2323-232323232301', '44444444-4444-4444-4444-444444444401', '77777777-7777-7777-7777-777777777711'),
    ('26262626-2626-2626-2626-262626262602', '25252525-2525-2525-2525-252525252502', 2, '10:45:00', '12:15:00', N'Principles of Management', N'C-101', N'Faculty BUS 1', '24242424-2424-2424-2424-242424242403', '23232323-2323-2323-2323-232323232302', '44444444-4444-4444-4444-444444444404', '77777777-7777-7777-7777-777777777714'),
    ('26262626-2626-2626-2626-262626262603', '25252525-2525-2525-2525-252525252503', 3, '08:30:00', '10:00:00', N'English Composition', N'S-101', N'Faculty LANG 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444416', '77777777-7777-7777-7777-777777777718'),
    ('26262626-2626-2626-2626-262626262604', '25252525-2525-2525-2525-252525252504', 4, '11:00:00', '12:30:00', N'Language Learning', N'U-201', N'Faculty LANG 2', '24242424-2424-2424-2424-242424242402', '23232323-2323-2323-2323-232323232301', '44444444-4444-4444-4444-444444444437', '77777777-7777-7777-7777-777777777718');

    INSERT INTO [timetable_entries] ([Id], [TimetableId], [DayOfWeek], [StartTime], [EndTime], [SubjectName], [RoomNumber], [FacultyName], [RoomId], [CreatedAt], [UpdatedAt], [BuildingId], [CourseId], [FacultyUserId])
    SELECT te.[Id], te.[TimetableId], te.[DayOfWeek], te.[StartTime], te.[EndTime], te.[SubjectName], te.[RoomNumber], te.[FacultyName], te.[RoomId], @Now, NULL, te.[BuildingId], te.[CourseId], te.[FacultyUserId]
    FROM @TimetableEntries te
    WHERE NOT EXISTS (SELECT 1 FROM [timetable_entries] x WHERE x.[Id] = te.[Id]);
END

/* 6.2) School and college classes across all seeded semesters */
IF OBJECT_ID(N'[timetables]') IS NOT NULL
BEGIN
    ;WITH SemesterBase AS
    (
        SELECT
            s.[Id],
            s.[StartDate],
            SemesterOrdinal = ROW_NUMBER() OVER (ORDER BY s.[StartDate], s.[Id])
        FROM @Semesters s
    ), ClassPrograms AS
    (
        SELECT
            p.[Id] AS ProgramId,
            p.[DepartmentId],
            p.[TotalSemesters],
            d.[InstitutionType]
        FROM @Programs p
        INNER JOIN @Departments d ON d.[Id] = p.[DepartmentId]
        WHERE d.[InstitutionType] IN (0, 1)
    )
    INSERT INTO [timetables] ([Id], [DepartmentId], [AcademicProgramId], [SemesterId], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt], [EffectiveDate], [SemesterNumber])
    SELECT
        NEWID(),
        cp.[DepartmentId],
        cp.[ProgramId],
        sb.[Id],
        1,
        @Now,
        @Now,
        NULL,
        0,
        NULL,
        CAST(sb.[StartDate] AS DATE),
        ((sb.[SemesterOrdinal] - 1) % CASE WHEN cp.[TotalSemesters] > 0 THEN cp.[TotalSemesters] ELSE 1 END) + 1
    FROM ClassPrograms cp
    CROSS JOIN SemesterBase sb
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [timetables] t
        WHERE t.[DepartmentId] = cp.[DepartmentId]
          AND t.[AcademicProgramId] = cp.[ProgramId]
          AND t.[SemesterId] = sb.[Id]
    );
END

IF OBJECT_ID(N'[timetable_entries]') IS NOT NULL
BEGIN
    ;WITH ClassPrograms AS
    (
        SELECT
            p.[Id] AS ProgramId,
            p.[DepartmentId],
            d.[InstitutionType],
            d.[Name] AS DepartmentName,
            c.[Id] AS CourseId,
            c.[Title] AS CourseTitle,
            CourseOrdinal = ROW_NUMBER() OVER (PARTITION BY p.[DepartmentId] ORDER BY c.[Code], c.[Id])
        FROM @Programs p
        INNER JOIN @Departments d ON d.[Id] = p.[DepartmentId]
        INNER JOIN [courses] c ON c.[DepartmentId] = p.[DepartmentId]
        WHERE d.[InstitutionType] IN (0, 1)
    ), CandidateClassTimetables AS
    (
        SELECT
            t.[Id] AS TimetableId,
            t.[DepartmentId],
            t.[SemesterNumber],
            cp.[InstitutionType],
            cp.[DepartmentName],
            cp.[CourseId],
            cp.[CourseTitle],
            cp.[CourseOrdinal]
        FROM [timetables] t
        INNER JOIN ClassPrograms cp ON cp.[ProgramId] = t.[AcademicProgramId]
        WHERE cp.[CourseOrdinal] <= 2
    ), FacultyByDepartment AS
    (
        SELECT
            u.[DepartmentId],
            u.[Id] AS FacultyUserId,
            u.[Username],
            FacultyOrdinal = ROW_NUMBER() OVER (PARTITION BY u.[DepartmentId] ORDER BY u.[Username])
        FROM [users] u
        WHERE u.[RoleId] = @RoleFaculty
          AND u.[IsDeleted] = 0
    )
    INSERT INTO [timetable_entries] ([Id], [TimetableId], [DayOfWeek], [StartTime], [EndTime], [SubjectName], [RoomNumber], [FacultyName], [RoomId], [CreatedAt], [UpdatedAt], [BuildingId], [CourseId], [FacultyUserId])
    SELECT
        NEWID(),
        ct.[TimetableId],
        CASE WHEN ct.[CourseOrdinal] = 1 THEN 1 ELSE 3 END,
        CASE WHEN ct.[CourseOrdinal] = 1 THEN CAST('08:30:00' AS TIME) ELSE CAST('10:45:00' AS TIME) END,
        CASE WHEN ct.[CourseOrdinal] = 1 THEN CAST('10:00:00' AS TIME) ELSE CAST('12:15:00' AS TIME) END,
        CONCAT(
            CASE
                WHEN ct.[InstitutionType] = 0 THEN CONCAT(N'School Cycle ', CAST(ct.[SemesterNumber] AS NVARCHAR(10)))
                ELSE CONCAT(N'College Cycle ', CAST(ct.[SemesterNumber] AS NVARCHAR(10)))
            END,
            N' - ',
            ct.[CourseTitle]
        ),
        CASE WHEN ct.[InstitutionType] = 0 THEN N'S-101' ELSE N'C-101' END,
        COALESCE(f.[Username], N'auto.faculty'),
        CASE WHEN ct.[InstitutionType] = 0 THEN CAST('24242424-2424-2424-2424-242424242405' AS UNIQUEIDENTIFIER) ELSE CAST('24242424-2424-2424-2424-242424242403' AS UNIQUEIDENTIFIER) END,
        @Now,
        NULL,
        CASE WHEN ct.[InstitutionType] = 0 THEN CAST('23232323-2323-2323-2323-232323232303' AS UNIQUEIDENTIFIER) ELSE CAST('23232323-2323-2323-2323-232323232302' AS UNIQUEIDENTIFIER) END,
        ct.[CourseId],
        f.[FacultyUserId]
    FROM CandidateClassTimetables ct
    LEFT JOIN FacultyByDepartment f ON f.[DepartmentId] = ct.[DepartmentId] AND f.[FacultyOrdinal] = 1
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [timetable_entries] te
        WHERE te.[TimetableId] = ct.[TimetableId]
          AND te.[CourseId] = ct.[CourseId]
    );
END

/* 6.3) Explicit class matrix for requested testing coverage
       - School Science: 10 classes (Class 1 to Class 10)
       - College ICS: 2 classes (College Semester 1 to College Semester 2)
*/
IF OBJECT_ID(N'[timetables]') IS NOT NULL
BEGIN
    DECLARE @TargetClassTimetables TABLE
    (
        Id UNIQUEIDENTIFIER,
        DepartmentId UNIQUEIDENTIFIER,
        AcademicProgramId UNIQUEIDENTIFIER,
        SemesterId UNIQUEIDENTIFIER,
        EffectiveDate DATE,
        SemesterNumber INT
    );

    INSERT INTO @TargetClassTimetables VALUES
    ('25252525-2525-2525-2525-252525252601', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 1),
    ('25252525-2525-2525-2525-252525252602', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 2),
    ('25252525-2525-2525-2525-252525252603', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 3),
    ('25252525-2525-2525-2525-252525252604', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 4),
    ('25252525-2525-2525-2525-252525252605', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 5),
    ('25252525-2525-2525-2525-252525252606', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 6),
    ('25252525-2525-2525-2525-252525252607', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 7),
    ('25252525-2525-2525-2525-252525252608', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 8),
    ('25252525-2525-2525-2525-252525252609', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 9),
    ('25252525-2525-2525-2525-252525252610', '13333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222432', '33333333-3333-3333-3333-333333333334', '2026-01-15', 10),
    ('25252525-2525-2525-2525-252525252611', '12222222-2222-2222-2222-222222222223', '22222222-2222-2222-2222-222222222325', '33333333-3333-3333-3333-333333333334', '2026-01-15', 1),
    ('25252525-2525-2525-2525-252525252612', '12222222-2222-2222-2222-222222222223', '22222222-2222-2222-2222-222222222325', '33333333-3333-3333-3333-333333333334', '2026-01-15', 2);

    INSERT INTO [timetables] ([Id], [DepartmentId], [AcademicProgramId], [SemesterId], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt], [EffectiveDate], [SemesterNumber])
    SELECT t.[Id], t.[DepartmentId], t.[AcademicProgramId], t.[SemesterId], 1, @Now, @Now, NULL, 0, NULL, t.[EffectiveDate], t.[SemesterNumber]
    FROM @TargetClassTimetables t
    WHERE NOT EXISTS (SELECT 1 FROM [timetables] x WHERE x.[Id] = t.[Id]);
END

IF OBJECT_ID(N'[timetable_entries]') IS NOT NULL
BEGIN
    DECLARE @TargetClassEntries TABLE
    (
        Id UNIQUEIDENTIFIER,
        TimetableId UNIQUEIDENTIFIER,
        DayOfWeek INT,
        StartTime TIME,
        EndTime TIME,
        SubjectName NVARCHAR(200),
        RoomNumber NVARCHAR(50),
        FacultyName NVARCHAR(200),
        RoomId UNIQUEIDENTIFIER,
        BuildingId UNIQUEIDENTIFIER,
        CourseId UNIQUEIDENTIFIER,
        FacultyUserId UNIQUEIDENTIFIER
    );

    INSERT INTO @TargetClassEntries VALUES
    ('26262626-2626-2626-2626-262626262701', '25252525-2525-2525-2525-252525252601', 1, '08:30:00', '10:00:00', N'Class 1 - General Science', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444431', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262702', '25252525-2525-2525-2525-252525252602', 1, '08:30:00', '10:00:00', N'Class 2 - Physics Basics', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444432', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262703', '25252525-2525-2525-2525-252525252603', 1, '08:30:00', '10:00:00', N'Class 3 - Chemistry Basics', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444433', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262704', '25252525-2525-2525-2525-252525252604', 1, '08:30:00', '10:00:00', N'Class 4 - General Science', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444431', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262705', '25252525-2525-2525-2525-252525252605', 1, '08:30:00', '10:00:00', N'Class 5 - Physics Basics', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444432', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262706', '25252525-2525-2525-2525-252525252606', 1, '08:30:00', '10:00:00', N'Class 6 - Chemistry Basics', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444433', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262707', '25252525-2525-2525-2525-252525252607', 1, '08:30:00', '10:00:00', N'Class 7 - General Science', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444431', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262708', '25252525-2525-2525-2525-252525252608', 1, '08:30:00', '10:00:00', N'Class 8 - Physics Basics', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444432', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262709', '25252525-2525-2525-2525-252525252609', 1, '08:30:00', '10:00:00', N'Class 9 - Chemistry Basics', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444433', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262710', '25252525-2525-2525-2525-252525252610', 1, '08:30:00', '10:00:00', N'Class 10 - General Science', N'S-101', N'Faculty School Sci 1', '24242424-2424-2424-2424-242424242405', '23232323-2323-2323-2323-232323232303', '44444444-4444-4444-4444-444444444431', '77777777-7777-7777-7777-777777777732'),
    ('26262626-2626-2626-2626-262626262711', '25252525-2525-2525-2525-252525252611', 2, '10:45:00', '12:15:00', N'College Semester 1 - Computer Fundamentals', N'C-101', N'Faculty College Sci 1', '24242424-2424-2424-2424-242424242403', '23232323-2323-2323-2323-232323232302', '44444444-4444-4444-4444-444444444435', '77777777-7777-7777-7777-777777777723'),
    ('26262626-2626-2626-2626-262626262712', '25252525-2525-2525-2525-252525252612', 2, '10:45:00', '12:15:00', N'College Semester 2 - Applied Mathematics', N'C-101', N'Faculty College Sci 1', '24242424-2424-2424-2424-242424242403', '23232323-2323-2323-2323-232323232302', '44444444-4444-4444-4444-444444444434', '77777777-7777-7777-7777-777777777723');

    INSERT INTO [timetable_entries] ([Id], [TimetableId], [DayOfWeek], [StartTime], [EndTime], [SubjectName], [RoomNumber], [FacultyName], [RoomId], [CreatedAt], [UpdatedAt], [BuildingId], [CourseId], [FacultyUserId])
    SELECT te.[Id], te.[TimetableId], te.[DayOfWeek], te.[StartTime], te.[EndTime], te.[SubjectName], te.[RoomNumber], te.[FacultyName], te.[RoomId], @Now, NULL, te.[BuildingId], te.[CourseId], te.[FacultyUserId]
    FROM @TargetClassEntries te
    WHERE NOT EXISTS (SELECT 1 FROM [timetable_entries] x WHERE x.[Id] = te.[Id]);
END

/* 6.4) Timetable Admin-focused demo pack (draft + publish workflow friendly) */
IF OBJECT_ID(N'[timetables]') IS NOT NULL
BEGIN
    DECLARE @TimetableAdminDemo TABLE
    (
        Id UNIQUEIDENTIFIER,
        DepartmentId UNIQUEIDENTIFIER,
        AcademicProgramId UNIQUEIDENTIFIER,
        SemesterId UNIQUEIDENTIFIER,
        EffectiveDate DATE,
        SemesterNumber INT,
        IsPublished BIT
    );

    INSERT INTO @TimetableAdminDemo VALUES
    ('25252525-2525-2525-2525-252525252701', '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333335', '2026-08-15', 3, 0),
    ('25252525-2525-2525-2525-252525252702', '11111111-1111-1111-1111-111111111112', '22222222-2222-2222-2222-222222222214', '33333333-3333-3333-3333-333333333335', '2026-08-15', 3, 0);

    INSERT INTO [timetables] ([Id], [DepartmentId], [AcademicProgramId], [SemesterId], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt], [EffectiveDate], [SemesterNumber])
    SELECT
        t.[Id],
        t.[DepartmentId],
        t.[AcademicProgramId],
        t.[SemesterId],
        t.[IsPublished],
        CASE WHEN t.[IsPublished] = 1 THEN @Now ELSE NULL END,
        @Now,
        NULL,
        0,
        NULL,
        t.[EffectiveDate],
        t.[SemesterNumber]
    FROM @TimetableAdminDemo t
    WHERE NOT EXISTS (SELECT 1 FROM [timetables] x WHERE x.[Id] = t.[Id]);
END

IF OBJECT_ID(N'[timetable_entries]') IS NOT NULL
BEGIN
    DECLARE @TimetableAdminDemoEntries TABLE
    (
        Id UNIQUEIDENTIFIER,
        TimetableId UNIQUEIDENTIFIER,
        DayOfWeek INT,
        StartTime TIME,
        EndTime TIME,
        SubjectName NVARCHAR(200),
        RoomNumber NVARCHAR(50),
        FacultyName NVARCHAR(200),
        RoomId UNIQUEIDENTIFIER,
        BuildingId UNIQUEIDENTIFIER,
        CourseId UNIQUEIDENTIFIER,
        FacultyUserId UNIQUEIDENTIFIER
    );

    INSERT INTO @TimetableAdminDemoEntries VALUES
    ('26262626-2626-2626-2626-262626262801', '25252525-2525-2525-2525-252525252701', 1, '09:00:00', '10:30:00', N'Timetable Admin Demo - Programming Fundamentals', N'U-101', N'faculty.cs.1', '24242424-2424-2424-2424-242424242401', '23232323-2323-2323-2323-232323232301', '44444444-4444-4444-4444-444444444401', '77777777-7777-7777-7777-777777777711'),
    ('26262626-2626-2626-2626-262626262802', '25252525-2525-2525-2525-252525252701', 3, '10:45:00', '12:15:00', N'Timetable Admin Demo - Data Structures', N'U-201', N'faculty.cs.2', '24242424-2424-2424-2424-242424242402', '23232323-2323-2323-2323-232323232301', '44444444-4444-4444-4444-444444444402', '77777777-7777-7777-7777-777777777712'),
    ('26262626-2626-2626-2626-262626262803', '25252525-2525-2525-2525-252525252702', 2, '11:00:00', '12:30:00', N'Timetable Admin Demo - Principles of Management', N'C-101', N'faculty.bus.1', '24242424-2424-2424-2424-242424242403', '23232323-2323-2323-2323-232323232302', '44444444-4444-4444-4444-444444444408', '77777777-7777-7777-7777-777777777714'),
    ('26262626-2626-2626-2626-262626262804', '25252525-2525-2525-2525-252525252701', 2, '13:00:00', '14:00:00', N'Teacher Timetable Demo - Admin Filter CS1', N'U-101', N'faculty.cs.1', '24242424-2424-2424-2424-242424242401', '23232323-2323-2323-2323-232323232301', '44444444-4444-4444-4444-444444444404', '77777777-7777-7777-7777-777777777711'),
    ('26262626-2626-2626-2626-262626262805', '25252525-2525-2525-2525-252525252701', 4, '14:15:00', '15:15:00', N'Teacher Timetable Demo - Admin Filter CS2', N'U-201', N'faculty.cs.2', '24242424-2424-2424-2424-242424242402', '23232323-2323-2323-2323-232323232301', '44444444-4444-4444-4444-444444444405', '77777777-7777-7777-7777-777777777712');

    INSERT INTO [timetable_entries] ([Id], [TimetableId], [DayOfWeek], [StartTime], [EndTime], [SubjectName], [RoomNumber], [FacultyName], [RoomId], [CreatedAt], [UpdatedAt], [BuildingId], [CourseId], [FacultyUserId])
    SELECT te.[Id], te.[TimetableId], te.[DayOfWeek], te.[StartTime], te.[EndTime], te.[SubjectName], te.[RoomNumber], te.[FacultyName], te.[RoomId], @Now, NULL, te.[BuildingId], te.[CourseId], te.[FacultyUserId]
    FROM @TimetableAdminDemoEntries te
    WHERE NOT EXISTS (SELECT 1 FROM [timetable_entries] x WHERE x.[Id] = te.[Id]);
END

/* 6.5) Course Materials demo pack for portal testing */
IF OBJECT_ID(N'[course_materials]') IS NOT NULL
BEGIN
    DECLARE @CourseMaterialsDemo TABLE
    (
        Id UNIQUEIDENTIFIER,
        TenantId UNIQUEIDENTIFIER,
        CampusId UNIQUEIDENTIFIER,
        DepartmentId UNIQUEIDENTIFIER,
        AcademicProgramId UNIQUEIDENTIFIER,
        SemesterId UNIQUEIDENTIFIER,
        CourseId UNIQUEIDENTIFIER,
        [Name] NVARCHAR(300),
        [Description] NVARCHAR(MAX),
        LinkUrl NVARCHAR(1024),
        FilePath NVARCHAR(1024),
        MaterialType INT,
        CreatedByUserId UNIQUEIDENTIFIER,
        IsActive BIT
    );

    INSERT INTO @CourseMaterialsDemo VALUES
    ('27272727-2727-2727-2727-272727272701', @UniTenantId, @UniCampusId, '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333335', '44444444-4444-4444-4444-444444444401', N'Programming Fundamentals - Demo Outline', N'Demo handout for Course Material page tests.', N'https://demo.tabsan.local/materials/pf-outline', N'dev/course-materials/demo/pf-outline.pdf', 3, '66666666-6666-6666-6666-666666666611', 1),
    ('27272727-2727-2727-2727-272727272702', @UniTenantId, @UniCampusId, '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333335', '44444444-4444-4444-4444-444444444402', N'Data Structures - Demo Slides', N'Slides file for demo download checks.', NULL, N'dev/course-materials/demo/data-structures-slides.pptx', 1, '66666666-6666-6666-6666-666666666611', 1),
    ('27272727-2727-2727-2727-272727272703', @UniTenantId, @UniCampusId, '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333335', '44444444-4444-4444-4444-444444444403', N'Algorithms - External Reference', N'Link-only material used to validate Link mode rendering.', N'https://demo.tabsan.local/materials/algorithms-reference', NULL, 2, '66666666-6666-6666-6666-666666666611', 1),
    ('27272727-2727-2727-2727-272727272704', @UniTenantId, @UniCampusId, '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333336', '44444444-4444-4444-4444-444444444404', N'Database Systems - Lab Sheet', N'Lab sheet file for semester filter scenarios.', NULL, N'dev/course-materials/demo/database-lab-sheet.docx', 1, '66666666-6666-6666-6666-666666666611', 1),
    ('27272727-2727-2727-2727-272727272705', @UniTenantId, @UniCampusId, '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222211', '33333333-3333-3333-3333-333333333334', '44444444-4444-4444-4444-444444444405', N'Web Development - Archive Sample', N'Inactive material used for ActiveOnly filter checks.', N'https://demo.tabsan.local/materials/web-archive', N'dev/course-materials/demo/web-archive.txt', 3, '66666666-6666-6666-6666-666666666611', 0);

    INSERT INTO [course_materials] ([Id], [TenantId], [CampusId], [DepartmentId], [AcademicProgramId], [SemesterId], [CourseId], [Name], [Description], [LinkUrl], [FilePath], [MaterialType], [CreatedByUserId], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        cm.[Id],
        cm.[TenantId],
        cm.[CampusId],
        cm.[DepartmentId],
        cm.[AcademicProgramId],
        cm.[SemesterId],
        cm.[CourseId],
        cm.[Name],
        cm.[Description],
        NULLIF(cm.[LinkUrl], N''),
        NULLIF(cm.[FilePath], N''),
        cm.[MaterialType],
        cm.[CreatedByUserId],
        cm.[IsActive],
        @Now,
        NULL,
        0,
        NULL
    FROM @CourseMaterialsDemo cm
    WHERE NOT EXISTS (SELECT 1 FROM [course_materials] x WHERE x.[Id] = cm.[Id]);
END

/* 7) Course offerings (Spring 2026 and beyond) - Massively expanded */
DECLARE @SpringSemester UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333334';
DECLARE @FallSemester UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @Offerings TABLE (Id UNIQUEIDENTIFIER, CourseId UNIQUEIDENTIFIER, SemesterId UNIQUEIDENTIFIER, FacultyUserId UNIQUEIDENTIFIER, MaxEnrollment INT);
INSERT INTO @Offerings VALUES
-- Spring 2026 University CS offerings
('55555555-5555-5555-5555-555555555501', '44444444-4444-4444-4444-444444444401', @SpringSemester, '77777777-7777-7777-7777-777777777711', 60),
('55555555-5555-5555-5555-555555555502', '44444444-4444-4444-4444-444444444402', @SpringSemester, '77777777-7777-7777-7777-777777777712', 50),
('55555555-5555-5555-5555-555555555503', '44444444-4444-4444-4444-444444444403', @SpringSemester, '77777777-7777-7777-7777-777777777713', 55),
('55555555-5555-5555-5555-555555555504', '44444444-4444-4444-4444-444444444404', @SpringSemester, '77777777-7777-7777-7777-777777777711', 45),
('55555555-5555-5555-5555-555555555505', '44444444-4444-4444-4444-444444444405', @SpringSemester, '77777777-7777-7777-7777-777777777712', 55),
('55555555-5555-5555-5555-555555555506', '44444444-4444-4444-4444-444444444406', @SpringSemester, '77777777-7777-7777-7777-777777777713', 40),
-- Spring 2026 University BUS offerings
('55555555-5555-5555-5555-555555555507', '44444444-4444-4444-4444-444444444408', @SpringSemester, '77777777-7777-7777-7777-777777777714', 65),
('55555555-5555-5555-5555-555555555508', '44444444-4444-4444-4444-444444444409', @SpringSemester, '77777777-7777-7777-7777-777777777715', 60),
('55555555-5555-5555-5555-555555555509', '44444444-4444-4444-4444-444444444410', @SpringSemester, '77777777-7777-7777-7777-777777777714', 50),
-- Spring 2026 University ENG offerings
('55555555-5555-5555-5555-555555555510', '44444444-4444-4444-4444-444444444413', @SpringSemester, '77777777-7777-7777-7777-777777777716', 55),
('55555555-5555-5555-5555-555555555511', '44444444-4444-4444-4444-444444444414', @SpringSemester, '77777777-7777-7777-7777-777777777717', 45),
-- Spring 2026 University ARTS offerings
('55555555-5555-5555-5555-555555555512', '44444444-4444-4444-4444-444444444416', @SpringSemester, '77777777-7777-7777-7777-777777777718', 40),
('55555555-5555-5555-5555-555555555519', '44444444-4444-4444-4444-444444444437', @SpringSemester, '77777777-7777-7777-7777-777777777718', 40),
-- Spring 2026 University SCI offerings
('55555555-5555-5555-5555-555555555513', '44444444-4444-4444-4444-444444444419', @SpringSemester, '77777777-7777-7777-7777-777777777719', 50),
-- Spring 2026 College offerings
('55555555-5555-5555-5555-555555555514', '44444444-4444-4444-4444-444444444422', @SpringSemester, '77777777-7777-7777-7777-777777777721', 45),
('55555555-5555-5555-5555-555555555515', '44444444-4444-4444-4444-444444444423', @SpringSemester, '77777777-7777-7777-7777-777777777722', 40),
('55555555-5555-5555-5555-555555555516', '44444444-4444-4444-4444-444444444425', @SpringSemester, '77777777-7777-7777-7777-777777777723', 35),
-- Spring 2026 School offerings
('55555555-5555-5555-5555-555555555517', '44444444-4444-4444-4444-444444444428', @SpringSemester, '77777777-7777-7777-7777-777777777731', 30),
('55555555-5555-5555-5555-555555555518', '44444444-4444-4444-4444-444444444431', @SpringSemester, '77777777-7777-7777-7777-777777777732', 35);

INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [MaxEnrollment], [IsOpen], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT o.Id, o.CourseId, o.SemesterId, o.FacultyUserId, o.MaxEnrollment, 1, @Now, NULL, 0, NULL
FROM @Offerings o
WHERE NOT EXISTS (SELECT 1 FROM [course_offerings] x WHERE x.[Id] = o.Id);

/* 7.1) Ensure detailed offering coverage for all semesters and institutes */
;WITH SemesterBase AS
(
    SELECT
        s.[Id],
        SemesterOrdinal = ROW_NUMBER() OVER (ORDER BY s.[StartDate], s.[Id])
    FROM @Semesters s
), DeptCourses AS
(
    SELECT
        c.[DepartmentId],
        c.[Id] AS CourseId,
        CourseOrdinal = ROW_NUMBER() OVER (PARTITION BY c.[DepartmentId] ORDER BY c.[Code], c.[Id])
    FROM [courses] c
), DeptFaculty AS
(
    SELECT
        u.[DepartmentId],
        u.[Id] AS FacultyUserId,
        FacultyOrdinal = ROW_NUMBER() OVER (PARTITION BY u.[DepartmentId] ORDER BY u.[Username]),
        FacultyCount = COUNT(*) OVER (PARTITION BY u.[DepartmentId])
    FROM [users] u
    WHERE u.[RoleId] = @RoleFaculty
      AND u.[IsDeleted] = 0
)
INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [MaxEnrollment], [IsOpen], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT
    NEWID(),
    dc.[CourseId],
    sb.[Id],
    df.[FacultyUserId],
    CASE d.[InstitutionType] WHEN 2 THEN 70 WHEN 1 THEN 55 ELSE 40 END,
    1,
    @Now,
    NULL,
    0,
    NULL
FROM @Departments d
INNER JOIN SemesterBase sb ON 1 = 1
INNER JOIN DeptCourses dc
    ON dc.[DepartmentId] = d.[Id]
   AND dc.[CourseOrdinal] <= CASE d.[InstitutionType] WHEN 2 THEN 3 WHEN 1 THEN 2 ELSE 2 END
INNER JOIN DeptFaculty df
    ON df.[DepartmentId] = d.[Id]
   AND df.[FacultyOrdinal] = ((sb.[SemesterOrdinal] + dc.[CourseOrdinal] - 2) % df.[FacultyCount]) + 1
WHERE NOT EXISTS
(
    SELECT 1
    FROM [course_offerings] co
    WHERE co.[CourseId] = dc.[CourseId]
      AND co.[SemesterId] = sb.[Id]
);

/* 7.2) Explicit university focus coverage
       Ensure one offering per semester for requested focus courses:
       BSCS, MCS, BBA, and Language Learning. */
;WITH SemesterBase AS
(
    SELECT s.[Id]
    FROM @Semesters s
), DepartmentLeadFaculty AS
(
    SELECT
        u.[DepartmentId],
        u.[Id] AS FacultyUserId,
        ROW_NUMBER() OVER (PARTITION BY u.[DepartmentId] ORDER BY u.[Username], u.[Id]) AS rn
    FROM [users] u
    WHERE u.[RoleId] = @RoleFaculty
      AND u.[IsDeleted] = 0
), FocusCourses AS
(
    SELECT CAST('44444444-4444-4444-4444-444444444401' AS UNIQUEIDENTIFIER) AS CourseId, CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER) AS DepartmentId, 65 AS MaxEnrollment -- BSCS focus
    UNION ALL
    SELECT CAST('44444444-4444-4444-4444-444444444402' AS UNIQUEIDENTIFIER), CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER), 55 -- MCS focus
    UNION ALL
    SELECT CAST('44444444-4444-4444-4444-444444444408' AS UNIQUEIDENTIFIER), CAST('11111111-1111-1111-1111-111111111112' AS UNIQUEIDENTIFIER), 60 -- BBA focus
    UNION ALL
    SELECT CAST('44444444-4444-4444-4444-444444444437' AS UNIQUEIDENTIFIER), CAST('11111111-1111-1111-1111-111111111114' AS UNIQUEIDENTIFIER), 45 -- Language learning focus
)
INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [MaxEnrollment], [IsOpen], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT
    NEWID(),
    fc.[CourseId],
    sb.[Id],
    dlf.[FacultyUserId],
    fc.[MaxEnrollment],
    1,
    @Now,
    NULL,
    0,
    NULL
FROM FocusCourses fc
INNER JOIN SemesterBase sb ON 1 = 1
INNER JOIN DepartmentLeadFaculty dlf
    ON dlf.[DepartmentId] = fc.[DepartmentId]
   AND dlf.[rn] = 1
WHERE NOT EXISTS
(
    SELECT 1
    FROM [course_offerings] co
    WHERE co.[CourseId] = fc.[CourseId]
      AND co.[SemesterId] = sb.[Id]
);

/* 8) Enrollments - Massive expansion (100+ enrollments) */
DECLARE @Enrollments TABLE (Id UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, CourseOfferingId UNIQUEIDENTIFIER, Status NVARCHAR(50));
INSERT INTO @Enrollments VALUES
-- Spring 2026 CS Enrollments
('88888888-8888-8888-8888-888888888101', '99999999-9999-9999-9999-999999999911', '55555555-5555-5555-5555-555555555501', N'Enrolled'),
('88888888-8888-8888-8888-888888888102', '99999999-9999-9999-9999-999999999911', '55555555-5555-5555-5555-555555555502', N'Enrolled'),
('88888888-8888-8888-8888-888888888103', '99999999-9999-9999-9999-999999999912', '55555555-5555-5555-5555-555555555501', N'Enrolled'),
('88888888-8888-8888-8888-888888888104', '99999999-9999-9999-9999-999999999912', '55555555-5555-5555-5555-555555555503', N'Enrolled'),
('88888888-8888-8888-8888-888888888105', '99999999-9999-9999-9999-999999999913', '55555555-5555-5555-5555-555555555502', N'Enrolled'),
('88888888-8888-8888-8888-888888888106', '99999999-9999-9999-9999-999999999913', '55555555-5555-5555-5555-555555555503', N'Enrolled'),
('88888888-8888-8888-8888-888888888107', '99999999-9999-9999-9999-999999999914', '55555555-5555-5555-5555-555555555501', N'Enrolled'),
('88888888-8888-8888-8888-888888888108', '99999999-9999-9999-9999-999999999914', '55555555-5555-5555-5555-555555555504', N'Enrolled'),
('88888888-8888-8888-8888-888888888109', '99999999-9999-9999-9999-999999999915', '55555555-5555-5555-5555-555555555502', N'Enrolled'),
('88888888-8888-8888-8888-888888888110', '99999999-9999-9999-9999-999999999916', '55555555-5555-5555-5555-555555555505', N'Enrolled'),
-- Spring 2026 IT Enrollments
('88888888-8888-8888-8888-888888888111', '99999999-9999-9999-9999-999999999916', '55555555-5555-5555-5555-555555555501', N'Enrolled'),
('88888888-8888-8888-8888-888888888112', '99999999-9999-9999-9999-999999999917', '55555555-5555-5555-5555-555555555501', N'Enrolled'),
('88888888-8888-8888-8888-888888888113', '99999999-9999-9999-9999-999999999917', '55555555-5555-5555-5555-555555555506', N'Enrolled'),
-- Spring 2026 BUS Enrollments
('88888888-8888-8888-8888-888888888114', '99999999-9999-9999-9999-999999999918', '55555555-5555-5555-5555-555555555507', N'Enrolled'),
('88888888-8888-8888-8888-888888888115', '99999999-9999-9999-9999-999999999918', '55555555-5555-5555-5555-555555555508', N'Enrolled'),
('88888888-8888-8888-8888-888888888116', '99999999-9999-9999-9999-999999999919', '55555555-5555-5555-5555-555555555507', N'Enrolled'),
('88888888-8888-8888-8888-888888888117', '99999999-9999-9999-9999-999999999919', '55555555-5555-5555-5555-555555555509', N'Enrolled'),
('88888888-8888-8888-8888-888888888118', '99999999-9999-9999-9999-999999999920', '55555555-5555-5555-5555-555555555508', N'Enrolled'),
-- Spring 2026 ENG Enrollments
('88888888-8888-8888-8888-888888888119', '99999999-9999-9999-9999-999999999921', '55555555-5555-5555-5555-555555555510', N'Enrolled'),
('88888888-8888-8888-8888-888888888120', '99999999-9999-9999-9999-999999999922', '55555555-5555-5555-5555-555555555511', N'Enrolled'),
-- Spring 2026 ARTS Enrollments
('88888888-8888-8888-8888-888888888121', '99999999-9999-9999-9999-999999999923', '55555555-5555-5555-5555-555555555512', N'Enrolled'),
-- Spring 2026 SCI Enrollments
('88888888-8888-8888-8888-888888888122', '99999999-9999-9999-9999-999999999924', '55555555-5555-5555-5555-555555555513', N'Enrolled'),
('88888888-8888-8888-8888-888888888133', '99999999-9999-9999-9999-999999999925', '55555555-5555-5555-5555-555555555519', N'Enrolled'),
-- Spring 2026 COLLEGE Enrollments
('88888888-8888-8888-8888-888888888123', '99999999-9999-9999-9999-999999999931', '55555555-5555-5555-5555-555555555514', N'Enrolled'),
('88888888-8888-8888-8888-888888888124', '99999999-9999-9999-9999-999999999931', '55555555-5555-5555-5555-555555555515', N'Enrolled'),
('88888888-8888-8888-8888-888888888125', '99999999-9999-9999-9999-999999999932', '55555555-5555-5555-5555-555555555514', N'Enrolled'),
('88888888-8888-8888-8888-888888888126', '99999999-9999-9999-9999-999999999933', '55555555-5555-5555-5555-555555555515', N'Enrolled'),
('88888888-8888-8888-8888-888888888127', '99999999-9999-9999-9999-999999999934', '55555555-5555-5555-5555-555555555516', N'Enrolled'),
('88888888-8888-8888-8888-888888888128', '99999999-9999-9999-9999-999999999935', '55555555-5555-5555-5555-555555555516', N'Enrolled'),
-- Spring 2026 SCHOOL Enrollments
('88888888-8888-8888-8888-888888888129', '99999999-9999-9999-9999-999999999941', '55555555-5555-5555-5555-555555555517', N'Enrolled'),
('88888888-8888-8888-8888-888888888130', '99999999-9999-9999-9999-999999999942', '55555555-5555-5555-5555-555555555517', N'Enrolled'),
('88888888-8888-8888-8888-888888888131', '99999999-9999-9999-9999-999999999943', '55555555-5555-5555-5555-555555555518', N'Enrolled'),
('88888888-8888-8888-8888-888888888132', '99999999-9999-9999-9999-999999999944', '55555555-5555-5555-5555-555555555518', N'Enrolled');

INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
SELECT e.Id, e.StudentProfileId, e.CourseOfferingId, @Now, NULL, e.Status, @Now, NULL
FROM @Enrollments e
WHERE NOT EXISTS (SELECT 1 FROM [enrollments] x WHERE x.[Id] = e.Id);

/* 8.1) High-volume enrollments for bulk institute students */
;WITH BulkStudents AS (
    SELECT
        sp.[Id] AS StudentProfileId,
        sp.[DepartmentId],
        StudentOrdinal = ROW_NUMBER() OVER (PARTITION BY sp.[DepartmentId] ORDER BY sp.[RegistrationNumber], sp.[Id])
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE u.[Username] LIKE N'bulk.%.student.%'
), DeptOfferings AS (
    SELECT
        co.[Id] AS CourseOfferingId,
        c.[DepartmentId],
        OfferingOrdinal = ROW_NUMBER() OVER (PARTITION BY c.[DepartmentId] ORDER BY co.[Id])
    FROM [course_offerings] co
    INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
)
INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
SELECT
    NEWID(),
    bs.[StudentProfileId],
    dof.[CourseOfferingId],
    DATEADD(day, -((bs.[StudentOrdinal] + dof.[OfferingOrdinal]) % 30), @Now),
    NULL,
    N'Enrolled',
    @Now,
    NULL
FROM BulkStudents bs
INNER JOIN DeptOfferings dof ON dof.[DepartmentId] = bs.[DepartmentId] AND dof.[OfferingOrdinal] <= 2
WHERE NOT EXISTS (
    SELECT 1
    FROM [enrollments] e
    WHERE e.[StudentProfileId] = bs.[StudentProfileId]
      AND e.[CourseOfferingId] = dof.[CourseOfferingId]
);

/* 8.2) Semester-level enrollment coverage for all departments and semesters */
;WITH DepartmentStudents AS
(
    SELECT
        sp.[Id] AS StudentProfileId,
        sp.[DepartmentId],
        StudentOrdinal = ROW_NUMBER() OVER (PARTITION BY sp.[DepartmentId] ORDER BY sp.[RegistrationNumber], sp.[Id])
    FROM [student_profiles] sp
), SemesterOfferings AS
(
    SELECT
        co.[Id] AS CourseOfferingId,
        c.[DepartmentId],
        co.[SemesterId],
        OfferingOrdinal = ROW_NUMBER() OVER (PARTITION BY c.[DepartmentId], co.[SemesterId] ORDER BY co.[Id])
    FROM [course_offerings] co
    INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
)
INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
SELECT
    NEWID(),
    ds.[StudentProfileId],
    so.[CourseOfferingId],
    DATEADD(day, -((ds.[StudentOrdinal] + so.[OfferingOrdinal]) % 28), @Now),
    NULL,
    N'Enrolled',
    @Now,
    NULL
FROM SemesterOfferings so
INNER JOIN DepartmentStudents ds
    ON ds.[DepartmentId] = so.[DepartmentId]
   AND ds.[StudentOrdinal] <= CASE WHEN so.[OfferingOrdinal] = 1 THEN 18 ELSE 12 END
WHERE so.[OfferingOrdinal] <= 2
  AND NOT EXISTS
(
    SELECT 1
    FROM [enrollments] e
    WHERE e.[StudentProfileId] = ds.[StudentProfileId]
      AND e.[CourseOfferingId] = so.[CourseOfferingId]
);

/* 9) Assignments - Expanded (20+ assignments) */
DECLARE @Assignments TABLE (Id UNIQUEIDENTIFIER, CourseOfferingId UNIQUEIDENTIFIER, Title NVARCHAR(300), DueDate DATETIME2, MaxMarks DECIMAL(8,2));
INSERT INTO @Assignments VALUES
('99999999-9999-9999-9999-999999999901', '55555555-5555-5555-5555-555555555501', N'Programming Assignment 1', DATEADD(day, 10, @Now), 20.00),
('99999999-9999-9999-9999-999999999902', '55555555-5555-5555-5555-555555555502', N'Data Structures Assignment 1', DATEADD(day, 12, @Now), 25.00),
('99999999-9999-9999-9999-999999999903', '55555555-5555-5555-5555-555555555503', N'Algorithms Assignment 1', DATEADD(day, 14, @Now), 25.00),
('99999999-9999-9999-9999-999999999904', '55555555-5555-5555-5555-555555555504', N'Database Project', DATEADD(day, 21, @Now), 30.00),
('99999999-9999-9999-9999-999999999905', '55555555-5555-5555-5555-555555555505', N'Web Development Project', DATEADD(day, 28, @Now), 40.00),
('99999999-9999-9999-9999-999999999906', '55555555-5555-5555-5555-555555555507', N'Management Case Study', DATEADD(day, 14, @Now), 30.00),
('99999999-9999-9999-9999-999999999907', '55555555-5555-5555-5555-555555555508', N'Marketing Plan', DATEADD(day, 16, @Now), 35.00),
('99999999-9999-9999-9999-999999999908', '55555555-5555-5555-5555-555555555514', N'Commerce Assignment', DATEADD(day, 12, @Now), 20.00),
('99999999-9999-9999-9999-999999999909', '55555555-5555-5555-5555-555555555517', N'Mathematics Problem Set', DATEADD(day, 8, @Now), 15.00),
('99999999-9999-9999-9999-999999999910', '55555555-5555-5555-5555-555555555518', N'Science Experiment Report', DATEADD(day, 18, @Now), 20.00);

INSERT INTO [assignments] ([Id], [CourseOfferingId], [Title], [Description], [DueDate], [MaxMarks], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT a.Id, a.CourseOfferingId, a.Title, N'Demo assignment data', a.DueDate, a.MaxMarks, 1, @Now, @Now, NULL, 0, NULL
FROM @Assignments a
WHERE NOT EXISTS (SELECT 1 FROM [assignments] x WHERE x.[Id] = a.Id);

/* 10) Assignment submissions - Expanded (40+ submissions) */
DECLARE @Submissions TABLE (Id UNIQUEIDENTIFIER, AssignmentId UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, Marks DECIMAL(8,2), GradedByUserId UNIQUEIDENTIFIER);
INSERT INTO @Submissions VALUES
-- Assignment 1 submissions
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01', '99999999-9999-9999-9999-999999999901', '99999999-9999-9999-9999-999999999911', 18.00, '77777777-7777-7777-7777-777777777711'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02', '99999999-9999-9999-9999-999999999901', '99999999-9999-9999-9999-999999999912', 19.00, '77777777-7777-7777-7777-777777777711'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03', '99999999-9999-9999-9999-999999999901', '99999999-9999-9999-9999-999999999913', 17.00, '77777777-7777-7777-7777-777777777711'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa04', '99999999-9999-9999-9999-999999999901', '99999999-9999-9999-9999-999999999914', 20.00, '77777777-7777-7777-7777-777777777711'),
-- Assignment 2 submissions
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa05', '99999999-9999-9999-9999-999999999902', '99999999-9999-9999-9999-999999999911', 22.00, '77777777-7777-7777-7777-777777777712'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa06', '99999999-9999-9999-9999-999999999902', '99999999-9999-9999-9999-999999999912', 21.00, '77777777-7777-7777-7777-777777777712'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa07', '99999999-9999-9999-9999-999999999902', '99999999-9999-9999-9999-999999999913', 23.00, '77777777-7777-7777-7777-777777777712'),
-- Assignment 3 submissions
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa08', '99999999-9999-9999-9999-999999999903', '99999999-9999-9999-9999-999999999914', 24.00, '77777777-7777-7777-7777-777777777713'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa09', '99999999-9999-9999-9999-999999999903', '99999999-9999-9999-9999-999999999915', 22.00, '77777777-7777-7777-7777-777777777713'),
-- BUS assignments
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10', '99999999-9999-9999-9999-999999999906', '99999999-9999-9999-9999-999999999918', 28.00, '77777777-7777-7777-7777-777777777714'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa11', '99999999-9999-9999-9999-999999999906', '99999999-9999-9999-9999-999999999919', 26.00, '77777777-7777-7777-7777-777777777714'),
-- COLLEGE assignments
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa12', '99999999-9999-9999-9999-999999999908', '99999999-9999-9999-9999-999999999931', 19.00, '77777777-7777-7777-7777-777777777721'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa13', '99999999-9999-9999-9999-999999999908', '99999999-9999-9999-9999-999999999932', 18.00, '77777777-7777-7777-7777-777777777721'),
-- SCHOOL assignments
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa14', '99999999-9999-9999-9999-999999999909', '99999999-9999-9999-9999-999999999941', 14.00, '77777777-7777-7777-7777-777777777731'),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa15', '99999999-9999-9999-9999-999999999909', '99999999-9999-9999-9999-999999999942', 13.00, '77777777-7777-7777-7777-777777777731');

INSERT INTO [assignment_submissions] ([Id], [AssignmentId], [StudentProfileId], [FileUrl], [TextContent], [SubmittedAt], [MarksAwarded], [Feedback], [GradedAt], [GradedByUserId], [Status], [CreatedAt], [UpdatedAt])
SELECT s.Id, s.AssignmentId, s.StudentProfileId, NULL, N'Demo answer content', @Now, s.Marks, N'Good work', @Now, s.GradedByUserId, N'Graded', @Now, NULL
FROM @Submissions s
WHERE NOT EXISTS (SELECT 1 FROM [assignment_submissions] x WHERE x.[Id] = s.Id);

/* 11) Attendance records - Expanded (50+ records) */
DECLARE @Attendance TABLE (Id UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, CourseOfferingId UNIQUEIDENTIFIER, [Date] DATETIME2, [Status] NVARCHAR(20), MarkedByUserId UNIQUEIDENTIFIER);
INSERT INTO @Attendance VALUES
-- CS Offering 1 attendance
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01', '99999999-9999-9999-9999-999999999911', '55555555-5555-5555-5555-555555555501', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777711'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb02', '99999999-9999-9999-9999-999999999912', '55555555-5555-5555-5555-555555555501', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777711'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb03', '99999999-9999-9999-9999-999999999913', '55555555-5555-5555-5555-555555555501', CAST(@Now AS date), N'Late', '77777777-7777-7777-7777-777777777711'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb04', '99999999-9999-9999-9999-999999999914', '55555555-5555-5555-5555-555555555501', CAST(@Now AS date), N'Absent', '77777777-7777-7777-7777-777777777711'),
-- CS Offering 2 attendance
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb05', '99999999-9999-9999-9999-999999999911', '55555555-5555-5555-5555-555555555502', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777712'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb06', '99999999-9999-9999-9999-999999999912', '55555555-5555-5555-5555-555555555502', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777712'),
-- BUS Offering attendance
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb07', '99999999-9999-9999-9999-999999999918', '55555555-5555-5555-5555-555555555507', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777714'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb08', '99999999-9999-9999-9999-999999999919', '55555555-5555-5555-5555-555555555507', CAST(@Now AS date), N'Late', '77777777-7777-7777-7777-777777777714'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb09', '99999999-9999-9999-9999-999999999920', '55555555-5555-5555-5555-555555555507', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777714'),
-- COLLEGE attendance
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb10', '99999999-9999-9999-9999-999999999931', '55555555-5555-5555-5555-555555555514', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777721'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb11', '99999999-9999-9999-9999-999999999932', '55555555-5555-5555-5555-555555555514', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777721'),
-- SCHOOL attendance
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb12', '99999999-9999-9999-9999-999999999941', '55555555-5555-5555-5555-555555555517', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777731'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb13', '99999999-9999-9999-9999-999999999942', '55555555-5555-5555-5555-555555555517', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777731'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb14', '99999999-9999-9999-9999-999999999943', '55555555-5555-5555-5555-555555555518', CAST(@Now AS date), N'Present', '77777777-7777-7777-7777-777777777732'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb15', '99999999-9999-9999-9999-999999999944', '55555555-5555-5555-5555-555555555518', CAST(@Now AS date), N'Late', '77777777-7777-7777-7777-777777777732');

INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId], [Remarks], [CreatedAt], [UpdatedAt])
SELECT a.Id, a.StudentProfileId, a.CourseOfferingId, a.[Date], a.[Status], a.MarkedByUserId, N'Demo attendance', @Now, NULL
FROM @Attendance a
WHERE NOT EXISTS (SELECT 1 FROM [attendance_records] x WHERE x.[Id] = a.Id);

/* 11.2) Additional attendance timeline coverage for non-bulk students */
INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId], [Remarks], [CreatedAt], [UpdatedAt])
SELECT
    NEWID(),
    e.[StudentProfileId],
    e.[CourseOfferingId],
    DATEADD(day, -1, CAST(@Now AS date)),
    CASE ABS(CHECKSUM(e.[StudentProfileId], e.[CourseOfferingId])) % 5
        WHEN 0 THEN N'Absent'
        WHEN 1 THEN N'Late'
        ELSE N'Present'
    END,
    COALESCE(co.[FacultyUserId], @SuperAdminUserId),
    N'Additional non-bulk attendance for trend scenarios.',
    @Now,
    NULL
FROM [enrollments] e
INNER JOIN [student_profiles] sp ON sp.[Id] = e.[StudentProfileId]
INNER JOIN [users] u ON u.[Id] = sp.[UserId]
INNER JOIN [course_offerings] co ON co.[Id] = e.[CourseOfferingId]
WHERE u.[Username] NOT LIKE N'bulk.%.student.%'
    AND NOT EXISTS (
        SELECT 1
        FROM [attendance_records] ar
        WHERE ar.[StudentProfileId] = e.[StudentProfileId]
        AND ar.[CourseOfferingId] = e.[CourseOfferingId]
        AND CAST(ar.[Date] AS date) = DATEADD(day, -1, CAST(@Now AS date))
    );

/* 11.1) High-volume attendance for bulk enrollments */
INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId], [Remarks], [CreatedAt], [UpdatedAt])
SELECT
        NEWID(),
        e.[StudentProfileId],
        e.[CourseOfferingId],
        DATEADD(day, -(ABS(CHECKSUM(e.[Id])) % 10), CAST(@Now AS date)),
        CASE ABS(CHECKSUM(e.[Id])) % 10
                WHEN 0 THEN N'Absent'
                WHEN 1 THEN N'Late'
                ELSE N'Present'
        END,
        COALESCE(co.[FacultyUserId], @SuperAdminUserId),
        N'Auto-generated bulk attendance record.',
        @Now,
        NULL
FROM [enrollments] e
INNER JOIN [student_profiles] sp ON sp.[Id] = e.[StudentProfileId]
INNER JOIN [users] u ON u.[Id] = sp.[UserId]
INNER JOIN [course_offerings] co ON co.[Id] = e.[CourseOfferingId]
WHERE u.[Username] LIKE N'bulk.%.student.%'
    AND NOT EXISTS (
            SELECT 1
            FROM [attendance_records] ar
            WHERE ar.[StudentProfileId] = e.[StudentProfileId]
                AND ar.[CourseOfferingId] = e.[CourseOfferingId]
                AND CAST(ar.[Date] AS date) = DATEADD(day, -(ABS(CHECKSUM(e.[Id])) % 10), CAST(@Now AS date))
    );

/* 12) Results - Massively Expanded */
IF OBJECT_ID(N'[results]') IS NOT NULL
BEGIN
    DECLARE @Results TABLE (
        Id UNIQUEIDENTIFIER,
        StudentProfileId UNIQUEIDENTIFIER,
        CourseOfferingId UNIQUEIDENTIFIER,
        ResultType NVARCHAR(450),
        MarksObtained DECIMAL(8,2),
        MaxMarks DECIMAL(8,2),
        PublishedByUserId UNIQUEIDENTIFIER
    );

    INSERT INTO @Results VALUES
    -- CS Offering 1 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc01', '99999999-9999-9999-9999-999999999911', '55555555-5555-5555-5555-555555555501', N'Final', 92.00, 100.00, '77777777-7777-7777-7777-777777777711'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc02', '99999999-9999-9999-9999-999999999912', '55555555-5555-5555-5555-555555555501', N'Final', 88.00, 100.00, '77777777-7777-7777-7777-777777777711'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc03', '99999999-9999-9999-9999-999999999913', '55555555-5555-5555-5555-555555555501', N'Final', 85.00, 100.00, '77777777-7777-7777-7777-777777777711'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc04', '99999999-9999-9999-9999-999999999914', '55555555-5555-5555-5555-555555555501', N'Final', 95.00, 100.00, '77777777-7777-7777-7777-777777777711'),
    -- CS Offering 2 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc05', '99999999-9999-9999-9999-999999999911', '55555555-5555-5555-5555-555555555502', N'Final', 90.00, 100.00, '77777777-7777-7777-7777-777777777712'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc06', '99999999-9999-9999-9999-999999999912', '55555555-5555-5555-5555-555555555502', N'Final', 87.00, 100.00, '77777777-7777-7777-7777-777777777712'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc07', '99999999-9999-9999-9999-999999999913', '55555555-5555-5555-5555-555555555502', N'Final', 82.00, 100.00, '77777777-7777-7777-7777-777777777712'),
    -- CS Offering 3 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc08', '99999999-9999-9999-9999-999999999912', '55555555-5555-5555-5555-555555555503', N'Final', 91.00, 100.00, '77777777-7777-7777-7777-777777777713'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc09', '99999999-9999-9999-9999-999999999913', '55555555-5555-5555-5555-555555555503', N'Final', 86.00, 100.00, '77777777-7777-7777-7777-777777777713'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc10', '99999999-9999-9999-9999-999999999915', '55555555-5555-5555-5555-555555555503', N'Final', 80.00, 100.00, '77777777-7777-7777-7777-777777777713'),
    -- CS Offering 4 (Database) results
    ('cccccccc-cccc-cccc-cccc-cccccccccc11', '99999999-9999-9999-9999-999999999914', '55555555-5555-5555-5555-555555555504', N'Final', 89.00, 100.00, '77777777-7777-7777-7777-777777777711'),
    -- CS Offering 5 (Web Dev) results
    ('cccccccc-cccc-cccc-cccc-cccccccccc12', '99999999-9999-9999-9999-999999999916', '55555555-5555-5555-5555-555555555505', N'Final', 93.00, 100.00, '77777777-7777-7777-7777-777777777712'),
    -- CS Offering 6 (Software Engineering) results
    ('cccccccc-cccc-cccc-cccc-cccccccccc13', '99999999-9999-9999-9999-999999999917', '55555555-5555-5555-5555-555555555506', N'Final', 84.00, 100.00, '77777777-7777-7777-7777-777777777713'),
    -- BUS Offering 1 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc14', '99999999-9999-9999-9999-999999999918', '55555555-5555-5555-5555-555555555507', N'Final', 89.00, 100.00, '77777777-7777-7777-7777-777777777714'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc15', '99999999-9999-9999-9999-999999999919', '55555555-5555-5555-5555-555555555507', N'Final', 84.00, 100.00, '77777777-7777-7777-7777-777777777714'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc16', '99999999-9999-9999-9999-999999999920', '55555555-5555-5555-5555-555555555507', N'Final', 91.00, 100.00, '77777777-7777-7777-7777-777777777714'),
    -- BUS Offering 2 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc17', '99999999-9999-9999-9999-999999999918', '55555555-5555-5555-5555-555555555508', N'Final', 87.00, 100.00, '77777777-7777-7777-7777-777777777715'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc18', '99999999-9999-9999-9999-999999999919', '55555555-5555-5555-5555-555555555508', N'Final', 88.00, 100.00, '77777777-7777-7777-7777-777777777715'),
    -- BUS Offering 3 (Financial Accounting) results
    ('cccccccc-cccc-cccc-cccc-cccccccccc19', '99999999-9999-9999-9999-999999999920', '55555555-5555-5555-5555-555555555509', N'Final', 85.00, 100.00, '77777777-7777-7777-7777-777777777714'),
    -- ENG Offering 1 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc20', '99999999-9999-9999-9999-999999999921', '55555555-5555-5555-5555-555555555510', N'Final', 92.00, 100.00, '77777777-7777-7777-7777-777777777716'),
    -- ENG Offering 2 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc21', '99999999-9999-9999-9999-999999999922', '55555555-5555-5555-5555-555555555511', N'Final', 88.00, 100.00, '77777777-7777-7777-7777-777777777717'),
    -- ARTS Offering 1 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc22', '99999999-9999-9999-9999-999999999923', '55555555-5555-5555-5555-555555555512', N'Final', 86.00, 100.00, '77777777-7777-7777-7777-777777777718'),
    -- SCI Offering 1 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc23', '99999999-9999-9999-9999-999999999924', '55555555-5555-5555-5555-555555555513', N'Final', 83.00, 100.00, '77777777-7777-7777-7777-777777777719'),
    -- COLLEGE Offering 1 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc24', '99999999-9999-9999-9999-999999999931', '55555555-5555-5555-5555-555555555514', N'Final', 86.00, 100.00, '77777777-7777-7777-7777-777777777721'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc25', '99999999-9999-9999-9999-999999999932', '55555555-5555-5555-5555-555555555514', N'Final', 84.00, 100.00, '77777777-7777-7777-7777-777777777721'),
    -- COLLEGE Offering 2 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc26', '99999999-9999-9999-9999-999999999931', '55555555-5555-5555-5555-555555555515', N'Final', 89.00, 100.00, '77777777-7777-7777-7777-777777777722'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc27', '99999999-9999-9999-9999-999999999933', '55555555-5555-5555-5555-555555555515', N'Final', 85.00, 100.00, '77777777-7777-7777-7777-777777777722'),
    -- COLLEGE Offering 3 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc28', '99999999-9999-9999-9999-999999999934', '55555555-5555-5555-5555-555555555516', N'Final', 82.00, 100.00, '77777777-7777-7777-7777-777777777723'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc29', '99999999-9999-9999-9999-999999999935', '55555555-5555-5555-5555-555555555516', N'Final', 87.00, 100.00, '77777777-7777-7777-7777-777777777723'),
    -- SCHOOL Offering 1 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc30', '99999999-9999-9999-9999-999999999941', '55555555-5555-5555-5555-555555555517', N'Final', 80.00, 100.00, '77777777-7777-7777-7777-777777777731'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc31', '99999999-9999-9999-9999-999999999942', '55555555-5555-5555-5555-555555555517', N'Final', 82.00, 100.00, '77777777-7777-7777-7777-777777777731'),
    -- SCHOOL Offering 2 results
    ('cccccccc-cccc-cccc-cccc-cccccccccc32', '99999999-9999-9999-9999-999999999943', '55555555-5555-5555-5555-555555555518', N'Final', 85.00, 100.00, '77777777-7777-7777-7777-777777777732'),
    ('cccccccc-cccc-cccc-cccc-cccccccccc33', '99999999-9999-9999-9999-999999999944', '55555555-5555-5555-5555-555555555518', N'Final', 78.00, 100.00, '77777777-7777-7777-7777-777777777732');

    INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
    SELECT r.Id, r.StudentProfileId, r.CourseOfferingId, r.ResultType, r.MarksObtained, r.MaxMarks, 1, @Now, r.PublishedByUserId, @Now, NULL
    FROM @Results r
    WHERE NOT EXISTS (SELECT 1 FROM [results] x WHERE x.[Id] = r.Id);

        /* 12.0.1) High-volume results for bulk enrollments */
        INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
        SELECT
                NEWID(),
                e.[StudentProfileId],
                e.[CourseOfferingId],
                N'Final',
                CAST(55 + (ABS(CHECKSUM(e.[Id])) % 46) AS DECIMAL(8,2)),
                CAST(100 AS DECIMAL(8,2)),
                1,
                @Now,
                COALESCE(co.[FacultyUserId], @SuperAdminUserId),
                @Now,
                NULL
        FROM [enrollments] e
        INNER JOIN [student_profiles] sp ON sp.[Id] = e.[StudentProfileId]
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        INNER JOIN [course_offerings] co ON co.[Id] = e.[CourseOfferingId]
        WHERE u.[Username] LIKE N'bulk.%.student.%'
            AND NOT EXISTS (
                    SELECT 1
                    FROM [results] x
                    WHERE x.[StudentProfileId] = e.[StudentProfileId]
                        AND x.[CourseOfferingId] = e.[CourseOfferingId]
                        AND x.[ResultType] = N'Final'
            );

        /* 12.0.2) Midterm published data and draft final-review data for lifecycle testing */
        INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
        SELECT
            NEWID(),
            r.[StudentProfileId],
            r.[CourseOfferingId],
            N'Midterm',
            CAST(CASE
                WHEN r.[MarksObtained] > 12 THEN r.[MarksObtained] - 12
                ELSE r.[MarksObtained]
            END AS DECIMAL(8,2)),
            r.[MaxMarks],
            1,
            DATEADD(day, -30, @Now),
            COALESCE(r.[PublishedByUserId], @SuperAdminUserId),
            @Now,
            NULL
        FROM [results] r
        WHERE r.[ResultType] = N'Final'
            AND r.[IsPublished] = 1
            AND NOT EXISTS (
                SELECT 1
                FROM [results] x
                WHERE x.[StudentProfileId] = r.[StudentProfileId]
                AND x.[CourseOfferingId] = r.[CourseOfferingId]
                AND x.[ResultType] = N'Midterm'
            );

        INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
        SELECT
            NEWID(),
            r.[StudentProfileId],
            r.[CourseOfferingId],
            N'FinalReview',
            r.[MarksObtained],
            r.[MaxMarks],
            0,
            NULL,
            NULL,
            @Now,
            NULL
        FROM [results] r
        WHERE r.[ResultType] = N'Final'
            AND r.[IsPublished] = 1
            AND r.[StudentProfileId] IN (
                '99999999-9999-9999-9999-999999999911',
                '99999999-9999-9999-9999-999999999918',
                '99999999-9999-9999-9999-999999999931',
                '99999999-9999-9999-9999-999999999941'
            )
            AND NOT EXISTS (
                SELECT 1
                FROM [results] x
                WHERE x.[StudentProfileId] = r.[StudentProfileId]
                AND x.[CourseOfferingId] = r.[CourseOfferingId]
                AND x.[ResultType] = N'FinalReview'
            );

        /* 12.0.3) Assignment-derived published results for demo/testing lifecycle */
        IF OBJECT_ID(N'[assignment_submissions]') IS NOT NULL AND OBJECT_ID(N'[assignments]') IS NOT NULL
        BEGIN
            ;WITH RankedAssignmentSubmissions AS
            (
                SELECT
                    s.[StudentProfileId],
                    a.[CourseOfferingId],
                    CAST(COALESCE(s.[MarksAwarded], a.[MaxMarks] * CAST(0.80 AS DECIMAL(8,2))) AS DECIMAL(8,2)) AS [MarksObtained],
                    CAST(a.[MaxMarks] AS DECIMAL(8,2)) AS [MaxMarks],
                    COALESCE(s.[GradedByUserId], co.[FacultyUserId], @SuperAdminUserId) AS [PublishedByUserId],
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY s.[StudentProfileId], a.[CourseOfferingId]
                        ORDER BY COALESCE(s.[GradedAt], s.[SubmittedAt]) DESC, s.[Id] DESC
                    ) AS [RowNum]
                FROM [assignment_submissions] s
                INNER JOIN [assignments] a ON a.[Id] = s.[AssignmentId]
                INNER JOIN [course_offerings] co ON co.[Id] = a.[CourseOfferingId]
                WHERE s.[Status] = N'Graded'
            )
            INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
            SELECT
                NEWID(),
                ras.[StudentProfileId],
                ras.[CourseOfferingId],
                N'Assignments',
                ras.[MarksObtained],
                ras.[MaxMarks],
                1,
                @Now,
                ras.[PublishedByUserId],
                @Now,
                NULL
            FROM RankedAssignmentSubmissions ras
            WHERE ras.[RowNum] = 1
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM [results] x
                    WHERE x.[StudentProfileId] = ras.[StudentProfileId]
                      AND x.[CourseOfferingId] = ras.[CourseOfferingId]
                      AND x.[ResultType] = N'Assignments'
                );
        END
END

/* 12.1) Payments - Expanded */
IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
BEGIN
    DECLARE @PaymentReceipts TABLE (
        Id UNIQUEIDENTIFIER,
        StudentProfileId UNIQUEIDENTIFIER,
        CreatedByUserId UNIQUEIDENTIFIER,
        [Status] INT,
        Amount DECIMAL(10,2),
        [Description] NVARCHAR(500),
        DueDate DATETIME2,
        ConfirmedByUserId UNIQUEIDENTIFIER NULL,
        ConfirmedAt DATETIME2 NULL,
        Notes NVARCHAR(2000) NULL
    );

    INSERT INTO @PaymentReceipts VALUES
    ('27272727-2727-2727-2727-272727272701', '99999999-9999-9999-9999-999999999911', '77777777-7777-7777-7777-777777777711', 1, 15000.00, N'Spring 2026 semester tuition installment 1', DATEADD(day, 14, @Now), '77777777-7777-7777-7777-777777777711', DATEADD(day, -2, @Now), N'Paid at campus counter.'),
    ('27272727-2727-2727-2727-272727272702', '99999999-9999-9999-9999-999999999912', '77777777-7777-7777-7777-777777777711', 1, 15000.00, N'Spring 2026 semester tuition installment 1', DATEADD(day, 14, @Now), '77777777-7777-7777-7777-777777777711', DATEADD(day, -2, @Now), N'Paid at campus counter.'),
    ('27272727-2727-2727-2727-272727272703', '99999999-9999-9999-9999-999999999913', '77777777-7777-7777-7777-777777777712', 0, 15000.00, N'Spring 2026 semester tuition', DATEADD(day, 21, @Now), NULL, NULL, N'Awaiting proof of payment.'),
    ('27272727-2727-2727-2727-272727272704', '99999999-9999-9999-9999-999999999914', '77777777-7777-7777-7777-777777777712', 1, 15000.00, N'Spring 2026 semester tuition installment 1', DATEADD(day, 14, @Now), '77777777-7777-7777-7777-777777777712', DATEADD(day, -1, @Now), N'Online payment confirmed.'),
    ('27272727-2727-2727-2727-272727272705', '99999999-9999-9999-9999-999999999931', '77777777-7777-7777-7777-777777777721', 1, 12000.00, N'College Spring 2026 semester fee', DATEADD(day, 21, @Now), '77777777-7777-7777-7777-777777777721', DATEADD(day, -3, @Now), N'Paid at college office.'),
    ('27272727-2727-2727-2727-272727272706', '99999999-9999-9999-9999-999999999932', '77777777-7777-7777-7777-777777777721', 0, 12000.00, N'College Spring 2026 semester fee', DATEADD(day, 21, @Now), NULL, NULL, N'Pending student submission.'),
    ('27272727-2727-2727-2727-272727272707', '99999999-9999-9999-9999-999999999933', '77777777-7777-7777-7777-777777777722', 1, 12000.00, N'College Spring 2026 semester fee', DATEADD(day, 21, @Now), '77777777-7777-7777-7777-777777777722', DATEADD(day, -1, @Now), N'Confirmed by college.'),
    ('27272727-2727-2727-2727-272727272708', '99999999-9999-9999-9999-999999999941', '77777777-7777-7777-7777-777777777731', 1, 9500.00, N'School Spring 2026 term fee', DATEADD(day, 10, @Now), '77777777-7777-7777-7777-777777777731', DATEADD(day, -2, @Now), N'Confirmed by school admin.'),
    ('27272727-2727-2727-2727-272727272709', '99999999-9999-9999-9999-999999999942', '77777777-7777-7777-7777-777777777732', 1, 9500.00, N'School Spring 2026 term fee', DATEADD(day, 10, @Now), '77777777-7777-7777-7777-777777777732', DATEADD(day, -2, @Now), N'Confirmed by school admin.'),
    ('27272727-2727-2727-2727-272727272710', '99999999-9999-9999-9999-999999999943', '77777777-7777-7777-7777-777777777732', 0, 9500.00, N'School Spring 2026 term fee', DATEADD(day, 10, @Now), NULL, NULL, N'Awaiting payment.');

    INSERT INTO [payment_receipts] ([Id], [StudentProfileId], [CreatedByUserId], [Status], [Amount], [Description], [DueDate], [ProofOfPaymentPath], [ProofUploadedAt], [ConfirmedByUserId], [ConfirmedAt], [Notes], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT p.[Id], p.[StudentProfileId], p.[CreatedByUserId], p.[Status], p.[Amount], p.[Description], p.[DueDate], NULL, NULL, p.[ConfirmedByUserId], p.[ConfirmedAt], p.[Notes], @Now, @Now, 0, NULL
    FROM @PaymentReceipts p
    WHERE NOT EXISTS (SELECT 1 FROM [payment_receipts] x WHERE x.[Id] = p.[Id]);

    /* 12.1.1) High-volume payment receipts for bulk students by institution type */
    DECLARE @FinanceUniUserId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [users] WHERE [Username] = N'finance.uni.1');
    DECLARE @FinanceColUserId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [users] WHERE [Username] = N'finance.col.1');
    DECLARE @FinanceSchUserId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [users] WHERE [Username] = N'finance.sch.1');

    INSERT INTO [payment_receipts] ([Id], [StudentProfileId], [CreatedByUserId], [Status], [Amount], [Description], [DueDate], [ProofOfPaymentPath], [ProofUploadedAt], [ConfirmedByUserId], [ConfirmedAt], [Notes], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        NEWID(),
        sp.[Id],
        CASE u.[InstitutionType]
            WHEN 2 THEN COALESCE(@FinanceUniUserId, @SuperAdminUserId)
            WHEN 1 THEN COALESCE(@FinanceColUserId, @SuperAdminUserId)
            ELSE COALESCE(@FinanceSchUserId, @SuperAdminUserId)
        END,
        CASE WHEN ABS(CHECKSUM(sp.[Id])) % 4 = 0 THEN 0 ELSE 1 END,
        CASE u.[InstitutionType]
            WHEN 2 THEN CAST(15000 + (ABS(CHECKSUM(sp.[Id])) % 6000) AS DECIMAL(10,2))
            WHEN 1 THEN CAST(12000 + (ABS(CHECKSUM(sp.[Id])) % 4500) AS DECIMAL(10,2))
            ELSE CAST(9000 + (ABS(CHECKSUM(sp.[Id])) % 3000) AS DECIMAL(10,2))
        END,
        N'Bulk Tuition Auto Seed - Spring Cycle',
        DATEADD(day, 10 + (ABS(CHECKSUM(sp.[Id])) % 21), @Now),
        NULL,
        NULL,
        CASE WHEN ABS(CHECKSUM(sp.[Id])) % 4 = 0 THEN NULL ELSE
            CASE u.[InstitutionType]
                WHEN 2 THEN COALESCE(@FinanceUniUserId, @SuperAdminUserId)
                WHEN 1 THEN COALESCE(@FinanceColUserId, @SuperAdminUserId)
                ELSE COALESCE(@FinanceSchUserId, @SuperAdminUserId)
            END
        END,
        CASE WHEN ABS(CHECKSUM(sp.[Id])) % 4 = 0 THEN NULL ELSE DATEADD(day, -1, @Now) END,
        CASE WHEN ABS(CHECKSUM(sp.[Id])) % 4 = 0 THEN N'Pending student payment proof.' ELSE N'Auto-confirmed bulk finance seed receipt.' END,
        @Now,
        @Now,
        0,
        NULL
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE u.[Username] LIKE N'bulk.%.student.%'
      AND NOT EXISTS (
          SELECT 1
          FROM [payment_receipts] pr
          WHERE pr.[StudentProfileId] = sp.[Id]
            AND pr.[Description] = N'Bulk Tuition Auto Seed - Spring Cycle'
      );
END

/* 12.2) Report export artifacts - Expanded */
IF OBJECT_ID(N'[transcript_export_logs]') IS NOT NULL
BEGIN
    DECLARE @TranscriptExports TABLE (Id UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, RequestedByUserId UNIQUEIDENTIFIER, ExportedAt DATETIME2, DocumentUrl NVARCHAR(2048), [Format] NVARCHAR(10), IpAddress NVARCHAR(45));
    INSERT INTO @TranscriptExports VALUES
    ('28282828-2828-2828-2828-282828282801', '99999999-9999-9999-9999-999999999911', '77777777-7777-7777-7777-777777777711', DATEADD(day, -5, @Now), N'https://demo.local/transcripts/2026-CS-0001.pdf', N'PDF', N'10.10.1.25'),
    ('28282828-2828-2828-2828-282828282802', '99999999-9999-9999-9999-999999999912', '77777777-7777-7777-7777-777777777712', DATEADD(day, -4, @Now), N'https://demo.local/transcripts/2026-CS-0002.pdf', N'PDF', N'10.10.1.26'),
    ('28282828-2828-2828-2828-282828282803', '99999999-9999-9999-9999-999999999913', '77777777-7777-7777-7777-777777777713', DATEADD(day, -3, @Now), N'https://demo.local/transcripts/2026-CS-0003.pdf', N'PDF', N'10.10.2.40'),
    ('28282828-2828-2828-2828-282828282804', '99999999-9999-9999-9999-999999999931', '77777777-7777-7777-7777-777777777721', DATEADD(day, -2, @Now), N'https://demo.local/transcripts/2026-COMM-0001.pdf', N'PDF', N'10.10.3.50'),
    ('28282828-2828-2828-2828-282828282805', '99999999-9999-9999-9999-999999999941', '77777777-7777-7777-7777-777777777731', DATEADD(day, -1, @Now), N'https://demo.local/transcripts/2026-SCHOOL-0001.pdf', N'PDF', N'10.10.3.56'),
    ('28282828-2828-2828-2828-282828282806', '99999999-9999-9999-9999-999999999918', '77777777-7777-7777-7777-777777777714', DATEADD(day, -1, @Now), N'https://demo.local/transcripts/2026-BUS-0001.pdf', N'PDF', N'10.10.4.10');

    INSERT INTO [transcript_export_logs] ([Id], [StudentProfileId], [RequestedByUserId], [ExportedAt], [DocumentUrl], [Format], [IpAddress], [CreatedAt], [UpdatedAt])
    SELECT t.[Id], t.[StudentProfileId], t.[RequestedByUserId], t.[ExportedAt], t.[DocumentUrl], t.[Format], t.[IpAddress], @Now, NULL
    FROM @TranscriptExports t
    WHERE NOT EXISTS (SELECT 1 FROM [transcript_export_logs] x WHERE x.[Id] = t.[Id]);
END

/* 13) Quizzes + questions + attempts + answers - Expanded */
IF OBJECT_ID(N'[quizzes]') IS NOT NULL
BEGIN
    DECLARE @Quizzes TABLE (Id UNIQUEIDENTIFIER, CourseOfferingId UNIQUEIDENTIFIER, Title NVARCHAR(300), CreatedByUserId UNIQUEIDENTIFIER);
    INSERT INTO @Quizzes VALUES
    ('13131313-1313-1313-1313-131313131301', '55555555-5555-5555-5555-555555555501', N'Programming Fundamentals Quiz 1', '77777777-7777-7777-7777-777777777711'),
    ('13131313-1313-1313-1313-131313131302', '55555555-5555-5555-5555-555555555502', N'Data Structures Quiz 1', '77777777-7777-7777-7777-777777777712'),
    ('13131313-1313-1313-1313-131313131303', '55555555-5555-5555-5555-555555555504', N'Database Systems Quiz 1', '77777777-7777-7777-7777-777777777711'),
    ('13131313-1313-1313-1313-131313131304', '55555555-5555-5555-5555-555555555507', N'Management Principles Quiz 1', '77777777-7777-7777-7777-777777777714'),
    ('13131313-1313-1313-1313-131313131305', '55555555-5555-5555-5555-555555555514', N'Commerce Fundamentals Quiz 1', '77777777-7777-7777-7777-777777777721'),
    ('13131313-1313-1313-1313-131313131306', '55555555-5555-5555-5555-555555555517', N'Mathematics Basics Quiz 1', '77777777-7777-7777-7777-777777777731');

    INSERT INTO [quizzes] ([Id], [CourseOfferingId], [Title], [Instructions], [TimeLimitMinutes], [MaxAttempts], [AvailableFrom], [AvailableUntil], [IsPublished], [IsActive], [CreatedByUserId], [CreatedAt], [UpdatedAt])
    SELECT q.Id, q.CourseOfferingId, q.Title, N'Complete all questions. Select best answer for MCQ.', 20, 1, DATEADD(day, -2, @Now), DATEADD(day, 7, @Now), 1, 1, q.CreatedByUserId, @Now, NULL
    FROM @Quizzes q
    WHERE NOT EXISTS (SELECT 1 FROM [quizzes] x WHERE x.[Id] = q.Id);

    DECLARE @QuizQuestions TABLE (Id UNIQUEIDENTIFIER, QuizId UNIQUEIDENTIFIER, [Text] NVARCHAR(2000), [Type] NVARCHAR(20), Marks DECIMAL(8,2), OrderIndex INT);
    INSERT INTO @QuizQuestions VALUES
    -- PF Quiz 1 questions
    ('14141414-1414-1414-1414-141414141401', '13131313-1313-1313-1313-131313131301', N'Which keyword declares a variable in C#?', N'MCQ', 5.00, 1),
    ('14141414-1414-1414-1414-141414141402', '13131313-1313-1313-1313-131313131301', N'What is a method?', N'Short', 5.00, 2),
    -- DS Quiz 1 questions
    ('14141414-1414-1414-1414-141414141403', '13131313-1313-1313-1313-131313131302', N'What is a linked list?', N'Short', 5.00, 1),
    ('14141414-1414-1414-1414-141414141404', '13131313-1313-1313-1313-131313131302', N'Best time complexity for searching in array?', N'MCQ', 5.00, 2),
    -- Database Quiz 1 questions
    ('14141414-1414-1414-1414-141414141405', '13131313-1313-1313-1313-131313131303', N'What is a primary key?', N'Short', 5.00, 1),
    -- Management Quiz 1 questions
    ('14141414-1414-1414-1414-141414141406', '13131313-1313-1313-1313-131313131304', N'Management is both science and?', N'MCQ', 5.00, 1),
    -- Commerce Quiz 1 questions
    ('14141414-1414-1414-1414-141414141407', '13131313-1313-1313-1313-131313131305', N'What is accounting?', N'Short', 5.00, 1),
    -- Math Quiz 1 questions
    ('14141414-1414-1414-1414-141414141408', '13131313-1313-1313-1313-131313131306', N'Solve: 2 + 2 * 3 = ?', N'MCQ', 5.00, 1);

    INSERT INTO [quiz_questions] ([Id], [QuizId], [Text], [Type], [Marks], [OrderIndex], [CreatedAt], [UpdatedAt])
    SELECT qq.Id, qq.QuizId, qq.[Text], qq.[Type], qq.Marks, qq.OrderIndex, @Now, NULL
    FROM @QuizQuestions qq
    WHERE NOT EXISTS (SELECT 1 FROM [quiz_questions] x WHERE x.[Id] = qq.Id);

    DECLARE @QuizOptions TABLE (Id UNIQUEIDENTIFIER, QuizQuestionId UNIQUEIDENTIFIER, [Text] NVARCHAR(1000), IsCorrect BIT, OrderIndex INT);
    INSERT INTO @QuizOptions VALUES
    -- PF Quiz Q1
    ('15151515-1515-1515-1515-151515151501', '14141414-1414-1414-1414-141414141401', N'var', 1, 1),
    ('15151515-1515-1515-1515-151515151502', '14141414-1414-1414-1414-141414141401', N'int', 0, 2),
    ('15151515-1515-1515-1515-151515151503', '14141414-1414-1414-1414-141414141401', N'variable', 0, 3),
    -- DS Quiz Q2
    ('15151515-1515-1515-1515-151515151504', '14141414-1414-1414-1414-141414141404', N'O(log n)', 0, 1),
    ('15151515-1515-1515-1515-151515151505', '14141414-1414-1414-1414-141414141404', N'O(n)', 1, 2),
    ('15151515-1515-1515-1515-151515151506', '14141414-1414-1414-1414-141414141404', N'O(n^2)', 0, 3),
    -- Management Quiz Q1
    ('15151515-1515-1515-1515-151515151507', '14141414-1414-1414-1414-141414141406', N'Art', 1, 1),
    ('15151515-1515-1515-1515-151515151508', '14141414-1414-1414-1414-141414141406', N'Luck', 0, 2),
    -- Math Quiz Q1
    ('15151515-1515-1515-1515-151515151509', '14141414-1414-1414-1414-141414141408', N'8', 1, 1),
    ('15151515-1515-1515-1515-151515151510', '14141414-1414-1414-1414-141414141408', N'6', 0, 2),
    ('15151515-1515-1515-1515-151515151511', '14141414-1414-1414-1414-141414141408', N'10', 0, 3);

    INSERT INTO [quiz_options] ([Id], [QuizQuestionId], [Text], [IsCorrect], [OrderIndex], [CreatedAt], [UpdatedAt])
    SELECT qo.Id, qo.QuizQuestionId, qo.[Text], qo.IsCorrect, qo.OrderIndex, @Now, NULL
    FROM @QuizOptions qo
    WHERE NOT EXISTS (SELECT 1 FROM [quiz_options] x WHERE x.[Id] = qo.Id);

    DECLARE @QuizAttempts TABLE (Id UNIQUEIDENTIFIER, QuizId UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, TotalScore DECIMAL(10,2));
    INSERT INTO @QuizAttempts VALUES
    -- PF Quiz 1 attempts
    ('16161616-1616-1616-1616-161616161601', '13131313-1313-1313-1313-131313131301', '99999999-9999-9999-9999-999999999911', 8.00),
    ('16161616-1616-1616-1616-161616161602', '13131313-1313-1313-1313-131313131301', '99999999-9999-9999-9999-999999999912', 10.00),
    -- DS Quiz 1 attempts
    ('16161616-1616-1616-1616-161616161603', '13131313-1313-1313-1313-131313131302', '99999999-9999-9999-9999-999999999912', 9.00),
    -- Database Quiz 1 attempts
    ('16161616-1616-1616-1616-161616161604', '13131313-1313-1313-1313-131313131303', '99999999-9999-9999-9999-999999999914', 7.00),
    -- Management Quiz 1 attempts
    ('16161616-1616-1616-1616-161616161605', '13131313-1313-1313-1313-131313131304', '99999999-9999-9999-9999-999999999918', 8.00),
    -- Commerce Quiz 1 attempts
    ('16161616-1616-1616-1616-161616161606', '13131313-1313-1313-1313-131313131305', '99999999-9999-9999-9999-999999999931', 9.00),
    -- Math Quiz 1 attempts
    ('16161616-1616-1616-1616-161616161607', '13131313-1313-1313-1313-131313131306', '99999999-9999-9999-9999-999999999941', 6.00);

    INSERT INTO [quiz_attempts] ([Id], [QuizId], [StudentProfileId], [StartedAt], [FinishedAt], [Status], [TotalScore], [CreatedAt], [UpdatedAt])
    SELECT qa.Id, qa.QuizId, qa.StudentProfileId, DATEADD(minute, -20, @Now), @Now, N'Completed', qa.TotalScore, @Now, NULL
    FROM @QuizAttempts qa
    WHERE NOT EXISTS (SELECT 1 FROM [quiz_attempts] x WHERE x.[Id] = qa.Id);

    DECLARE @QuizAnswers TABLE (Id UNIQUEIDENTIFIER, QuizAttemptId UNIQUEIDENTIFIER, QuizQuestionId UNIQUEIDENTIFIER, SelectedOptionId UNIQUEIDENTIFIER NULL, TextResponse NVARCHAR(4000) NULL, MarksAwarded DECIMAL(8,2));
    INSERT INTO @QuizAnswers VALUES
    -- PF Quiz 1 Attempt 1 answers
    ('17171717-1717-1717-1717-171717171701', '16161616-1616-1616-1616-161616161601', '14141414-1414-1414-1414-141414141401', '15151515-1515-1515-1515-151515151501', NULL, 5.00),
    ('17171717-1717-1717-1717-171717171702', '16161616-1616-1616-1616-161616161601', '14141414-1414-1414-1414-141414141402', NULL, N'A method is a function in a class.', 3.00),
    -- PF Quiz 1 Attempt 2 answers
    ('17171717-1717-1717-1717-171717171703', '16161616-1616-1616-1616-161616161602', '14141414-1414-1414-1414-141414141401', '15151515-1515-1515-1515-151515151501', NULL, 5.00),
    ('17171717-1717-1717-1717-171717171704', '16161616-1616-1616-1616-161616161602', '14141414-1414-1414-1414-141414141402', NULL, N'A reusable block of code.', 5.00),
    -- DS Quiz 1 Attempt 1 answers
    ('17171717-1717-1717-1717-171717171705', '16161616-1616-1616-1616-161616161603', '14141414-1414-1414-1414-141414141403', NULL, N'A data structure with nodes linked by pointers.', 5.00),
    ('17171717-1717-1717-1717-171717171706', '16161616-1616-1616-1616-161616161603', '14141414-1414-1414-1414-141414141404', '15151515-1515-1515-1515-151515151505', NULL, 4.00),
    -- Management Quiz 1 Attempt 1 answers
    ('17171717-1717-1717-1717-171717171707', '16161616-1616-1616-1616-161616161605', '14141414-1414-1414-1414-141414141406', '15151515-1515-1515-1515-151515151507', NULL, 5.00),
    -- Math Quiz 1 Attempt 1 answers
    ('17171717-1717-1717-1717-171717171708', '16161616-1616-1616-1616-161616161607', '14141414-1414-1414-1414-141414141408', '15151515-1515-1515-1515-151515151509', NULL, 5.00);

    INSERT INTO [quiz_answers] ([Id], [QuizAttemptId], [QuizQuestionId], [SelectedOptionId], [TextResponse], [MarksAwarded], [CreatedAt], [UpdatedAt])
    SELECT qa.Id, qa.QuizAttemptId, qa.QuizQuestionId, qa.SelectedOptionId, qa.TextResponse, qa.MarksAwarded, @Now, NULL
    FROM @QuizAnswers qa
    WHERE NOT EXISTS (SELECT 1 FROM [quiz_answers] x WHERE x.[Id] = qa.Id);
END

/* 13.1) Lifecycle parity artifacts */
IF OBJECT_ID(N'[bulk_promotion_batches]') IS NOT NULL
BEGIN
    DECLARE @PromotionBatches TABLE (Id UNIQUEIDENTIFIER, Title NVARCHAR(180), [Status] INT, CreatedByUserId UNIQUEIDENTIFIER, ApprovedByUserId UNIQUEIDENTIFIER NULL, ReviewedAt DATETIME2 NULL, AppliedAt DATETIME2 NULL, ReviewNote NVARCHAR(1000) NULL);
    INSERT INTO @PromotionBatches VALUES
    ('29292929-2929-2929-2929-292929292901', N'Institute Parity Cycle 2026', 2, @SuperAdminUserId, @SuperAdminUserId, DATEADD(day, -2, @Now), DATEADD(day, -1, @Now), N'Applied for school, college, and university demo students.');

    INSERT INTO [bulk_promotion_batches] ([Id], [Title], [Status], [CreatedByUserId], [ApprovedByUserId], [ReviewedAt], [AppliedAt], [ReviewNote], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT b.[Id], b.[Title], b.[Status], b.[CreatedByUserId], b.[ApprovedByUserId], b.[ReviewedAt], b.[AppliedAt], b.[ReviewNote], @Now, NULL, 0, NULL
    FROM @PromotionBatches b
    WHERE NOT EXISTS (SELECT 1 FROM [bulk_promotion_batches] x WHERE x.[Id] = b.[Id]);
END

IF OBJECT_ID(N'[bulk_promotion_entries]') IS NOT NULL
BEGIN
    DECLARE @PromotionEntries TABLE (Id UNIQUEIDENTIFIER, BatchId UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, Decision INT, Reason NVARCHAR(500), IsApplied BIT, AppliedAt DATETIME2 NULL);
    INSERT INTO @PromotionEntries VALUES
    ('30303030-3030-3030-3030-303030303001', '29292929-2929-2929-2929-292929292901', '99999999-9999-9999-9999-999999999941', 0, N'Promoted to next term.', 1, DATEADD(day, -1, @Now)),
    ('30303030-3030-3030-3030-303030303002', '29292929-2929-2929-2929-292929292901', '99999999-9999-9999-9999-999999999943', 0, N'Promoted to next year.', 1, DATEADD(day, -1, @Now)),
    ('30303030-3030-3030-3030-303030303003', '29292929-2929-2929-2929-292929292901', '99999999-9999-9999-9999-999999999944', 0, N'Promoted to next class.', 1, DATEADD(day, -1, @Now));

    INSERT INTO [bulk_promotion_entries] ([Id], [BatchId], [StudentProfileId], [Decision], [Reason], [IsApplied], [AppliedAt], [CreatedAt], [UpdatedAt])
    SELECT e.[Id], e.[BatchId], e.[StudentProfileId], e.[Decision], e.[Reason], e.[IsApplied], e.[AppliedAt], @Now, NULL
    FROM @PromotionEntries e
    WHERE NOT EXISTS (SELECT 1 FROM [bulk_promotion_entries] x WHERE x.[Id] = e.[Id]);
END

IF OBJECT_ID(N'[graduation_applications]') IS NOT NULL
BEGIN
        DECLARE @EligibleGraduationStudentId UNIQUEIDENTIFIER;
        SELECT TOP (1) @EligibleGraduationStudentId = sp.Id
        FROM [student_profiles] sp
        INNER JOIN [academic_programs] ap ON ap.Id = sp.ProgramId
        INNER JOIN [departments] d ON d.Id = sp.DepartmentId
        WHERE d.InstitutionType = 2
            AND sp.Status = 1
            AND sp.CurrentSemesterNumber >= ap.TotalSemesters
            AND EXISTS
            (
                    SELECT 1
                    FROM [fyp_projects] fp
                    WHERE fp.StudentProfileId = sp.Id
                        AND (
                            TRY_CONVERT(INT, fp.[Status]) = 4
                            OR CONVERT(NVARCHAR(50), fp.[Status]) IN (N'Completed', N'Approved')
                        )
            )
        ORDER BY sp.CreatedAt;

        DECLARE @GraduationApplications TABLE (Id UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, [Status] INT, StudentNote NVARCHAR(2000), SubmittedAt DATETIME2, CertificatePath NVARCHAR(500) NULL, CertificateGeneratedAt DATETIME2 NULL);

        IF @EligibleGraduationStudentId IS NOT NULL
        BEGIN
                INSERT INTO @GraduationApplications VALUES
                ('31313131-3131-3131-3131-313131313101', @EligibleGraduationStudentId, 2, N'All degree requirements completed.', DATEADD(day, -20, @Now), N'https://demo.local/certificates/2026-IT-0001.pdf', DATEADD(day, -2, @Now));
        END

    INSERT INTO [graduation_applications] ([Id], [StudentProfileId], [Status], [StudentNote], [SubmittedAt], [CertificatePath], [CertificateGeneratedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT g.[Id], g.[StudentProfileId], g.[Status], g.[StudentNote], g.[SubmittedAt], g.[CertificatePath], g.[CertificateGeneratedAt], @Now, NULL, 0, NULL
    FROM @GraduationApplications g
    WHERE NOT EXISTS (SELECT 1 FROM [graduation_applications] x WHERE x.[Id] = g.[Id]);
END

IF OBJECT_ID(N'[graduation_application_approvals]') IS NOT NULL
BEGIN
    DECLARE @GraduationApprovals TABLE (Id UNIQUEIDENTIFIER, GraduationApplicationId UNIQUEIDENTIFIER, Stage INT, ApproverUserId UNIQUEIDENTIFIER, IsApproved BIT, Note NVARCHAR(1000), ActedAt DATETIME2);
    INSERT INTO @GraduationApprovals VALUES
    ('32323232-3232-3232-3232-323232323201', '31313131-3131-3131-3131-313131313101', 0, '66666666-6666-6666-6666-666666666611', 1, N'Department verification approved.', DATEADD(day, -10, @Now)),
    ('32323232-3232-3232-3232-323232323202', '31313131-3131-3131-3131-313131313101', 1, @SuperAdminUserId, 1, N'Final approval granted.', DATEADD(day, -2, @Now));

    INSERT INTO [graduation_application_approvals] ([Id], [GraduationApplicationId], [Stage], [ApproverUserId], [IsApproved], [Note], [ActedAt], [CreatedAt], [UpdatedAt])
    SELECT a.[Id], a.[GraduationApplicationId], a.[Stage], a.[ApproverUserId], a.[IsApproved], a.[Note], a.[ActedAt], @Now, NULL
    FROM @GraduationApprovals a
        WHERE EXISTS (SELECT 1 FROM [graduation_applications] ga WHERE ga.[Id] = a.[GraduationApplicationId])
            AND NOT EXISTS (SELECT 1 FROM [graduation_application_approvals] x WHERE x.[Id] = a.[Id]);
END

IF OBJECT_ID(N'[student_report_cards]') IS NOT NULL
BEGIN
    DECLARE @StudentReportCards TABLE (Id UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, InstitutionType INT, PeriodLabel NVARCHAR(80), PayloadJson NVARCHAR(MAX), GeneratedByUserId UNIQUEIDENTIFIER, GeneratedAt DATETIME2);
    INSERT INTO @StudentReportCards VALUES
    ('33333333-3333-3333-3333-333333333901', '99999999-9999-9999-9999-999999999916', 2, N'Spring 2026 Semester 2', N'{"summary":"University parity card","gpa":3.40}', '66666666-6666-6666-6666-666666666611', DATEADD(day, -2, @Now)),
    ('33333333-3333-3333-3333-333333333902', '99999999-9999-9999-9999-999999999933', 1, N'2026 Year 2', N'{"summary":"College parity card","percentage":78.5}', '66666666-6666-6666-6666-666666666612', DATEADD(day, -2, @Now)),
    ('33333333-3333-3333-3333-333333333903', '99999999-9999-9999-9999-999999999944', 0, N'Class 10 Term 1', N'{"summary":"School parity card","percentage":82.0}', '66666666-6666-6666-6666-666666666613', DATEADD(day, -2, @Now));

    INSERT INTO [student_report_cards] ([Id], [StudentProfileId], [InstitutionType], [PeriodLabel], [PayloadJson], [GeneratedByUserId], [GeneratedAt], [CreatedAt], [UpdatedAt])
    SELECT rc.[Id], rc.[StudentProfileId], rc.[InstitutionType], rc.[PeriodLabel], rc.[PayloadJson], rc.[GeneratedByUserId], rc.[GeneratedAt], @Now, NULL
    FROM @StudentReportCards rc
    WHERE NOT EXISTS (SELECT 1 FROM [student_report_cards] x WHERE x.[Id] = rc.[Id]);
END

IF OBJECT_ID(N'[school_streams]') IS NOT NULL
BEGIN
    DECLARE @SchoolStreams TABLE (Id UNIQUEIDENTIFIER, [Name] NVARCHAR(120), [Description] NVARCHAR(500));
    INSERT INTO @SchoolStreams VALUES
    ('34343434-3434-3434-3434-343434343401', N'Science', N'School-level science stream for parity data.'),
    ('34343434-3434-3434-3434-343434343402', N'Commerce', N'School-level commerce stream for parity data.');

    INSERT INTO [school_streams] ([Id], [Name], [Description], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT s.[Id], s.[Name], s.[Description], 1, @Now, NULL, 0, NULL
    FROM @SchoolStreams s
    WHERE NOT EXISTS (SELECT 1 FROM [school_streams] x WHERE x.[Id] = s.[Id]);
END

IF OBJECT_ID(N'[student_stream_assignments]') IS NOT NULL
BEGIN
    DECLARE @StudentStreamAssignments TABLE (Id UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, SchoolStreamId UNIQUEIDENTIFIER, AssignedByUserId UNIQUEIDENTIFIER);
    INSERT INTO @StudentStreamAssignments VALUES
    ('35353535-3535-3535-3535-353535353500', '99999999-9999-9999-9999-999999999943', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353501', '99999999-9999-9999-9999-999999999944', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666613'),
    ('35353535-3535-3535-3535-353535353502', '99999999-9999-9999-9999-999999999945', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353503', '99999999-9999-9999-9999-999999999946', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353504', '99999999-9999-9999-9999-999999999947', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353505', '99999999-9999-9999-9999-999999999948', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353506', '99999999-9999-9999-9999-999999999949', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353507', '99999999-9999-9999-9999-999999999950', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353508', '99999999-9999-9999-9999-999999999951', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632'),
    ('35353535-3535-3535-3535-353535353509', '99999999-9999-9999-9999-999999999952', '34343434-3434-3434-3434-343434343401', '66666666-6666-6666-6666-666666666632');

    INSERT INTO [student_stream_assignments] ([Id], [StudentProfileId], [SchoolStreamId], [AssignedAt], [AssignedByUserId], [CreatedAt], [UpdatedAt])
    SELECT a.[Id], a.[StudentProfileId], a.[SchoolStreamId], @Now, a.[AssignedByUserId], @Now, NULL
    FROM @StudentStreamAssignments a
    WHERE NOT EXISTS (SELECT 1 FROM [student_stream_assignments] x WHERE x.[Id] = a.[Id]);
END

/* 14) Helpdesk demo tickets */
IF OBJECT_ID(N'[support_tickets]') IS NOT NULL
BEGIN
    DECLARE @SupportTickets TABLE (Id UNIQUEIDENTIFIER, SubmitterId UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER, Category INT, Subject NVARCHAR(300), Body NVARCHAR(4000), Status INT, AssignedToId UNIQUEIDENTIFIER NULL);
    INSERT INTO @SupportTickets VALUES
    ('18181818-1818-1818-1818-181818181801', '66666666-6666-6666-6666-666666666631', '11111111-1111-1111-1111-111111111111', 0, N'Cannot open assignment portal', N'Assignment page is not loading from student dashboard.', 0, '66666666-6666-6666-6666-666666666611'),
    ('18181818-1818-1818-1818-181818181802', '66666666-6666-6666-6666-666666666633', '11111111-1111-1111-1111-111111111112', 1, N'Result discrepancy query', N'Requesting review of posted marks.', 1, '66666666-6666-6666-6666-666666666612');

    INSERT INTO [support_tickets] ([Id], [SubmitterId], [DepartmentId], [Category], [Subject], [Body], [Status], [AssignedToId], [ResolvedAt], [ReopenWindowDays], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT t.Id, t.SubmitterId, t.DepartmentId, t.Category, t.Subject, t.Body, t.Status, t.AssignedToId,
           CASE WHEN t.Status = 1 THEN @Now ELSE NULL END,
           7, @Now, NULL, 0, NULL
    FROM @SupportTickets t
    WHERE NOT EXISTS (SELECT 1 FROM [support_tickets] x WHERE x.[Id] = t.Id);

    IF OBJECT_ID(N'[support_ticket_messages]') IS NOT NULL
    BEGIN
        DECLARE @SupportMessages TABLE (Id UNIQUEIDENTIFIER, TicketId UNIQUEIDENTIFIER, AuthorId UNIQUEIDENTIFIER, Body NVARCHAR(4000), IsInternalNote BIT);
        INSERT INTO @SupportMessages VALUES
        ('19191919-1919-1919-1919-191919191901', '18181818-1818-1818-1818-181818181801', '66666666-6666-6666-6666-666666666631', N'Issue started after latest portal login.', 0),
        ('19191919-1919-1919-1919-191919191902', '18181818-1818-1818-1818-181818181801', '66666666-6666-6666-6666-666666666611', N'Investigating logs and routing.', 1),
        ('19191919-1919-1919-1919-191919191903', '18181818-1818-1818-1818-181818181802', '66666666-6666-6666-6666-666666666612', N'Please share screenshot for result discrepancy.', 0);

        INSERT INTO [support_ticket_messages] ([Id], [TicketId], [AuthorId], [Body], [IsInternalNote], [CreatedAt], [UpdatedAt])
        SELECT m.Id, m.TicketId, m.AuthorId, m.Body, m.IsInternalNote, @Now, NULL
        FROM @SupportMessages m
        WHERE NOT EXISTS (SELECT 1 FROM [support_ticket_messages] x WHERE x.[Id] = m.Id);
    END
END

/* 15) LMS discussions */
IF OBJECT_ID(N'[discussion_threads]') IS NOT NULL
BEGIN
    DECLARE @DiscussionThreads TABLE (Id UNIQUEIDENTIFIER, OfferingId UNIQUEIDENTIFIER, Title NVARCHAR(500), AuthorId UNIQUEIDENTIFIER, IsPinned BIT, IsClosed BIT);
    INSERT INTO @DiscussionThreads VALUES
    ('20202020-2020-2020-2020-202020202001', '55555555-5555-5555-5555-555555555501', N'Week 2 Lab Preparation', '66666666-6666-6666-6666-666666666621', 1, 0),
    ('20202020-2020-2020-2020-202020202002', '55555555-5555-5555-5555-555555555504', N'Case Study References', '66666666-6666-6666-6666-666666666623', 0, 0);

    INSERT INTO [discussion_threads] ([Id], [OfferingId], [Title], [AuthorId], [IsPinned], [IsClosed], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT t.Id, t.OfferingId, t.Title, t.AuthorId, t.IsPinned, t.IsClosed, @Now, NULL, 0, NULL
    FROM @DiscussionThreads t
    WHERE NOT EXISTS (SELECT 1 FROM [discussion_threads] x WHERE x.[Id] = t.Id);

    IF OBJECT_ID(N'[discussion_replies]') IS NOT NULL
    BEGIN
        DECLARE @DiscussionReplies TABLE (Id UNIQUEIDENTIFIER, ThreadId UNIQUEIDENTIFIER, AuthorId UNIQUEIDENTIFIER, Body NVARCHAR(MAX));
        INSERT INTO @DiscussionReplies VALUES
        ('21212121-2121-2121-2121-212121212101', '20202020-2020-2020-2020-202020202001', '66666666-6666-6666-6666-666666666631', N'Should we revise loops before the lab?'),
        ('21212121-2121-2121-2121-212121212102', '20202020-2020-2020-2020-202020202001', '66666666-6666-6666-6666-666666666621', N'Yes, focus on arrays and loops.'),
        ('21212121-2121-2121-2121-212121212103', '20202020-2020-2020-2020-202020202002', '66666666-6666-6666-6666-666666666633', N'I found a useful Harvard case study on strategy.');

        INSERT INTO [discussion_replies] ([Id], [ThreadId], [AuthorId], [Body], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT r.Id, r.ThreadId, r.AuthorId, r.Body, @Now, NULL, 0, NULL
        FROM @DiscussionReplies r
        WHERE NOT EXISTS (SELECT 1 FROM [discussion_replies] x WHERE x.[Id] = r.Id);
    END
END

/* 16) Notifications and recipients - Expanded */
DECLARE @Notifications TABLE (Id UNIQUEIDENTIFIER, Title NVARCHAR(300), Body NVARCHAR(4000), [Type] NVARCHAR(50), SenderUserId UNIQUEIDENTIFIER);
INSERT INTO @Notifications VALUES
('cccccccc-cccc-cccc-cccc-cccccccccc01', N'Welcome to Spring 2026 Semester', N'Classes are now active in the portal. Please review course schedule.', N'Academic', '77777777-7777-7777-7777-777777777711'),
('cccccccc-cccc-cccc-cccc-cccccccccc02', N'Assignment Due Reminder', N'Programming Assignment 1 is due in 3 days. Submit before deadline.', N'Reminder', '77777777-7777-7777-7777-777777777711'),
('cccccccc-cccc-cccc-cccc-cccccccccc03', N'Attendance Policy Update', N'Attendance below 75% will be flagged. Current attendance: 88%.', N'Policy', '77777777-7777-7777-7777-777777777712'),
('cccccccc-cccc-cccc-cccc-cccccccccc04', N'Quiz Available Now', N'Programming Fundamentals Quiz 1 is now available for 7 days.', N'Academic', '77777777-7777-7777-7777-777777777711'),
('cccccccc-cccc-cccc-cccc-cccccccccc05', N'Results Published', N'Your semester results have been published. Check grades in portal.', N'Academic', '77777777-7777-7777-7777-777777777714'),
('cccccccc-cccc-cccc-cccc-cccccccccc06', N'Payment Reminder', N'Spring 2026 semester fee is due on the scheduled date.', N'Reminder', '77777777-7777-7777-7777-777777777721'),
('cccccccc-cccc-cccc-cccc-cccccccccc07', N'Support Ticket Update', N'Your support ticket has been resolved. Please review response.', N'System', '77777777-7777-7777-7777-777777777711'),
('cccccccc-cccc-cccc-cccc-cccccccccc08', N'Course Material Posted', N'New lecture notes have been uploaded to Week 2.', N'Academic', '77777777-7777-7777-7777-777777777712');

INSERT INTO [notifications] ([Id], [Title], [Body], [Type], [SenderUserId], [IsSystemGenerated], [IsActive], [CreatedAt], [UpdatedAt])
SELECT n.Id, n.Title, n.Body, n.[Type], n.SenderUserId, 0, 1, @Now, NULL
FROM @Notifications n
WHERE NOT EXISTS (SELECT 1 FROM [notifications] x WHERE x.[Id] = n.Id);

DECLARE @Recipients TABLE (Id UNIQUEIDENTIFIER, NotificationId UNIQUEIDENTIFIER, RecipientUserId UNIQUEIDENTIFIER, IsRead BIT);
INSERT INTO @Recipients VALUES
-- Welcome notification to all students
('dddddddd-dddd-dddd-dddd-dddddddddd01', 'cccccccc-cccc-cccc-cccc-cccccccccc01', '99999999-9999-9999-9999-999999999911', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd02', 'cccccccc-cccc-cccc-cccc-cccccccccc01', '99999999-9999-9999-9999-999999999912', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd03', 'cccccccc-cccc-cccc-cccc-cccccccccc01', '99999999-9999-9999-9999-999999999931', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd04', 'cccccccc-cccc-cccc-cccc-cccccccccc01', '99999999-9999-9999-9999-999999999941', 0),
-- Assignment reminder to CS students
('dddddddd-dddd-dddd-dddd-dddddddddd05', 'cccccccc-cccc-cccc-cccc-cccccccccc02', '99999999-9999-9999-9999-999999999911', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd06', 'cccccccc-cccc-cccc-cccc-cccccccccc02', '99999999-9999-9999-9999-999999999912', 0),
-- Attendance notification to all students
('dddddddd-dddd-dddd-dddd-dddddddddd07', 'cccccccc-cccc-cccc-cccc-cccccccccc03', '99999999-9999-9999-9999-999999999911', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd08', 'cccccccc-cccc-cccc-cccc-cccccccccc03', '99999999-9999-9999-9999-999999999918', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd09', 'cccccccc-cccc-cccc-cccc-cccccccccc03', '99999999-9999-9999-9999-999999999931', 0),
-- Quiz notification
('dddddddd-dddd-dddd-dddd-dddddddddd10', 'cccccccc-cccc-cccc-cccc-cccccccccc04', '99999999-9999-9999-9999-999999999911', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd11', 'cccccccc-cccc-cccc-cccc-cccccccccc04', '99999999-9999-9999-9999-999999999912', 1),
-- Results notification
('dddddddd-dddd-dddd-dddd-dddddddddd12', 'cccccccc-cccc-cccc-cccc-cccccccccc05', '99999999-9999-9999-9999-999999999918', 1),
('dddddddd-dddd-dddd-dddd-dddddddddd13', 'cccccccc-cccc-cccc-cccc-cccccccccc05', '99999999-9999-9999-9999-999999999919', 1),
-- Payment notification
('dddddddd-dddd-dddd-dddd-dddddddddd14', 'cccccccc-cccc-cccc-cccc-cccccccccc06', '99999999-9999-9999-9999-999999999931', 0),
('dddddddd-dddd-dddd-dddd-dddddddddd15', 'cccccccc-cccc-cccc-cccc-cccccccccc06', '99999999-9999-9999-9999-999999999941', 0);

INSERT INTO [notification_recipients] ([Id], [NotificationId], [RecipientUserId], [IsRead], [ReadAt], [CreatedAt], [UpdatedAt])
SELECT r.Id, r.NotificationId, r.RecipientUserId, r.IsRead,
       CASE WHEN r.IsRead = 1 THEN DATEADD(minute, -30, @Now) ELSE NULL END,
       @Now, NULL
FROM @Recipients r
WHERE NOT EXISTS (SELECT 1 FROM [notification_recipients] x WHERE x.[Id] = r.Id);

/* 17) Extended coverage for remaining domains + high-volume dummy data */

IF OBJECT_ID(N'[gpa_scale_rules]') IS NOT NULL
BEGIN
    DECLARE @GpaScaleSql NVARCHAR(MAX);
    IF COL_LENGTH('gpa_scale_rules', 'InstitutionType') IS NOT NULL
    BEGIN
        SET @GpaScaleSql = N'
            INSERT INTO [gpa_scale_rules] ([Id], [InstitutionType], [GradePoint], [MinimumScore], [DisplayOrder], [CreatedAt], [UpdatedAt])
            SELECT src.[Id], 0, src.[GradePoint], src.[MinimumScore], src.[DisplayOrder], @Now, NULL
            FROM (VALUES
                (''41414141-4141-4141-4141-414141414101'', CAST(4.00 AS DECIMAL(4,2)), CAST(85.00 AS DECIMAL(5,2)), 1),
                (''41414141-4141-4141-4141-414141414102'', CAST(3.70 AS DECIMAL(4,2)), CAST(80.00 AS DECIMAL(5,2)), 2),
                (''41414141-4141-4141-4141-414141414103'', CAST(3.30 AS DECIMAL(4,2)), CAST(75.00 AS DECIMAL(5,2)), 3),
                (''41414141-4141-4141-4141-414141414104'', CAST(3.00 AS DECIMAL(4,2)), CAST(70.00 AS DECIMAL(5,2)), 4),
                (''41414141-4141-4141-4141-414141414105'', CAST(2.70 AS DECIMAL(4,2)), CAST(65.00 AS DECIMAL(5,2)), 5),
                (''41414141-4141-4141-4141-414141414106'', CAST(2.30 AS DECIMAL(4,2)), CAST(60.00 AS DECIMAL(5,2)), 6),
                (''41414141-4141-4141-4141-414141414107'', CAST(2.00 AS DECIMAL(4,2)), CAST(55.00 AS DECIMAL(5,2)), 7),
                (''41414141-4141-4141-4141-414141414108'', CAST(1.70 AS DECIMAL(4,2)), CAST(50.00 AS DECIMAL(5,2)), 8),
                (''41414141-4141-4141-4141-414141414109'', CAST(0.00 AS DECIMAL(4,2)), CAST(0.00 AS DECIMAL(5,2)), 9)
            ) src([Id], [GradePoint], [MinimumScore], [DisplayOrder])
            WHERE NOT EXISTS (SELECT 1 FROM [gpa_scale_rules] x WHERE x.[Id] = src.[Id]);';
    END
    ELSE
    BEGIN
        SET @GpaScaleSql = N'
            INSERT INTO [gpa_scale_rules] ([Id], [GradePoint], [MinimumScore], [DisplayOrder], [CreatedAt], [UpdatedAt])
            SELECT src.[Id], src.[GradePoint], src.[MinimumScore], src.[DisplayOrder], @Now, NULL
            FROM (VALUES
                (''41414141-4141-4141-4141-414141414101'', CAST(4.00 AS DECIMAL(4,2)), CAST(85.00 AS DECIMAL(5,2)), 1),
                (''41414141-4141-4141-4141-414141414102'', CAST(3.70 AS DECIMAL(4,2)), CAST(80.00 AS DECIMAL(5,2)), 2),
                (''41414141-4141-4141-4141-414141414103'', CAST(3.30 AS DECIMAL(4,2)), CAST(75.00 AS DECIMAL(5,2)), 3),
                (''41414141-4141-4141-4141-414141414104'', CAST(3.00 AS DECIMAL(4,2)), CAST(70.00 AS DECIMAL(5,2)), 4),
                (''41414141-4141-4141-4141-414141414105'', CAST(2.70 AS DECIMAL(4,2)), CAST(65.00 AS DECIMAL(5,2)), 5),
                (''41414141-4141-4141-4141-414141414106'', CAST(2.30 AS DECIMAL(4,2)), CAST(60.00 AS DECIMAL(5,2)), 6),
                (''41414141-4141-4141-4141-414141414107'', CAST(2.00 AS DECIMAL(4,2)), CAST(55.00 AS DECIMAL(5,2)), 7),
                (''41414141-4141-4141-4141-414141414108'', CAST(1.70 AS DECIMAL(4,2)), CAST(50.00 AS DECIMAL(5,2)), 8),
                (''41414141-4141-4141-4141-414141414109'', CAST(0.00 AS DECIMAL(4,2)), CAST(0.00 AS DECIMAL(5,2)), 9)
            ) src([Id], [GradePoint], [MinimumScore], [DisplayOrder])
            WHERE NOT EXISTS (SELECT 1 FROM [gpa_scale_rules] x WHERE x.[Id] = src.[Id]);';
    END

    EXEC sp_executesql @GpaScaleSql, N'@Now datetime2', @Now = @Now;
END

IF OBJECT_ID(N'[result_component_rules]') IS NOT NULL
BEGIN
    DECLARE @ResultComponentSql NVARCHAR(MAX);
    IF COL_LENGTH('result_component_rules', 'InstitutionType') IS NOT NULL
    BEGIN
        SET @ResultComponentSql = N'
            INSERT INTO [result_component_rules] ([Id], [InstitutionType], [Name], [Weightage], [DisplayOrder], [IsActive], [CreatedAt], [UpdatedAt])
            SELECT src.[Id], 0, src.[Name], src.[Weightage], src.[DisplayOrder], 1, @Now, NULL
            FROM (VALUES
                (''42424242-4242-4242-4242-424242424201'', N''Assignments'', CAST(25.00 AS DECIMAL(5,2)), 1),
                (''42424242-4242-4242-4242-424242424202'', N''Quizzes'', CAST(15.00 AS DECIMAL(5,2)), 2),
                (''42424242-4242-4242-4242-424242424203'', N''Midterm'', CAST(25.00 AS DECIMAL(5,2)), 3),
                (''42424242-4242-4242-4242-424242424204'', N''Final'', CAST(35.00 AS DECIMAL(5,2)), 4)
            ) src([Id], [Name], [Weightage], [DisplayOrder])
            WHERE NOT EXISTS (SELECT 1 FROM [result_component_rules] x WHERE x.[Id] = src.[Id]);';
    END
    ELSE
    BEGIN
        SET @ResultComponentSql = N'
            INSERT INTO [result_component_rules] ([Id], [Name], [Weightage], [DisplayOrder], [IsActive], [CreatedAt], [UpdatedAt])
            SELECT src.[Id], src.[Name], src.[Weightage], src.[DisplayOrder], 1, @Now, NULL
            FROM (VALUES
                (''42424242-4242-4242-4242-424242424201'', N''Assignments'', CAST(25.00 AS DECIMAL(5,2)), 1),
                (''42424242-4242-4242-4242-424242424202'', N''Quizzes'', CAST(15.00 AS DECIMAL(5,2)), 2),
                (''42424242-4242-4242-4242-424242424203'', N''Midterm'', CAST(25.00 AS DECIMAL(5,2)), 3),
                (''42424242-4242-4242-4242-424242424204'', N''Final'', CAST(35.00 AS DECIMAL(5,2)), 4)
            ) src([Id], [Name], [Weightage], [DisplayOrder])
            WHERE NOT EXISTS (SELECT 1 FROM [result_component_rules] x WHERE x.[Id] = src.[Id]);';
    END

    EXEC sp_executesql @ResultComponentSql, N'@Now datetime2', @Now = @Now;
END

IF OBJECT_ID(N'[institution_grading_profiles]') IS NOT NULL
BEGIN
    INSERT INTO [institution_grading_profiles] ([Id], [InstitutionType], [PassThreshold], [GradeRangesJson], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT src.[Id], src.[InstitutionType], src.[PassThreshold], src.[GradeRangesJson], 1, @Now, NULL
    FROM (VALUES
        ('43434343-4343-4343-4343-434343434301', 0, CAST(50.00 AS DECIMAL(5,2)), N'[{"grade":"A","min":85},{"grade":"B","min":70},{"grade":"C","min":55},{"grade":"D","min":50}]'),
        ('43434343-4343-4343-4343-434343434302', 1, CAST(55.00 AS DECIMAL(5,2)), N'[{"grade":"A","min":85},{"grade":"B","min":72},{"grade":"C","min":60},{"grade":"D","min":55}]'),
        ('43434343-4343-4343-4343-434343434303', 2, CAST(60.00 AS DECIMAL(5,2)), N'[{"grade":"A","min":85},{"grade":"B","min":75},{"grade":"C","min":65},{"grade":"D","min":60}]')
    ) src([Id], [InstitutionType], [PassThreshold], [GradeRangesJson])
    WHERE NOT EXISTS (SELECT 1 FROM [institution_grading_profiles] x WHERE x.[InstitutionType] = src.[InstitutionType]);
END

IF OBJECT_ID(N'[accreditation_templates]') IS NOT NULL
BEGIN
    INSERT INTO [accreditation_templates] ([Id], [Name], [Description], [Format], [FieldMappingsJson], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT src.[Id], src.[Name], src.[Description], src.[Format], src.[FieldMappingsJson], 1, @Now, NULL
    FROM (VALUES
        ('44444444-4444-4444-4444-444444444401', N'HEC Program Template', N'Program-level accreditation mapping for HEC style submissions.', N'JSON', N'{"program":"academic_programs.Name","courses":"courses.Code","faculty":"users.Username"}'),
        ('44444444-4444-4444-4444-444444444402', N'NAAC Department Template', N'Department-level quality assurance data extract.', N'CSV', N'{"department":"departments.Name","students":"student_profiles.RegistrationNumber"}'),
        ('44444444-4444-4444-4444-444444444403', N'ISO Academic Ops Template', N'Operational process compliance and evidence template.', N'JSON', N'{"tickets":"support_tickets.Subject","audit":"audit_logs.Action"}')
    ) src([Id], [Name], [Description], [Format], [FieldMappingsJson])
    WHERE NOT EXISTS (SELECT 1 FROM [accreditation_templates] x WHERE x.[Id] = src.[Id]);
END

IF OBJECT_ID(N'[registration_whitelist]') IS NOT NULL
BEGIN
    INSERT INTO [registration_whitelist] ([Id], [IdentifierType], [IdentifierValue], [DepartmentId], [ProgramId], [IsUsed], [UsedAt], [CreatedUserId], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), N'RegistrationNumber', sp.[RegistrationNumber], sp.[DepartmentId], sp.[ProgramId], 1, DATEADD(day, -7, @Now), @SuperAdminUserId, @Now, NULL
    FROM [student_profiles] sp
    WHERE NOT EXISTS (
        SELECT 1 FROM [registration_whitelist] x
        WHERE x.[IdentifierType] = N'RegistrationNumber' AND x.[IdentifierValue] = sp.[RegistrationNumber]
    );

    INSERT INTO [registration_whitelist] ([Id], [IdentifierType], [IdentifierValue], [DepartmentId], [ProgramId], [IsUsed], [UsedAt], [CreatedUserId], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), N'Email', u.[Email], sp.[DepartmentId], sp.[ProgramId], 1, DATEADD(day, -6, @Now), @SuperAdminUserId, @Now, NULL
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE u.[Email] IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM [registration_whitelist] x
          WHERE x.[IdentifierType] = N'Email' AND x.[IdentifierValue] = u.[Email]
      );
END

IF OBJECT_ID(N'[course_prerequisites]') IS NOT NULL
BEGIN
    ;WITH RankedCourses AS (
        SELECT c.[Id], c.[DepartmentId], ROW_NUMBER() OVER (PARTITION BY c.[DepartmentId] ORDER BY c.[Code], c.[Id]) AS rn
        FROM [courses] c
    )
    INSERT INTO [course_prerequisites] ([Id], [CourseId], [PrerequisiteCourseId], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), c2.[Id], c1.[Id], @Now, NULL
    FROM RankedCourses c1
    INNER JOIN RankedCourses c2 ON c2.[DepartmentId] = c1.[DepartmentId] AND c2.rn = c1.rn + 1
    WHERE NOT EXISTS (
        SELECT 1
        FROM [course_prerequisites] x
        WHERE x.[CourseId] = c2.[Id] AND x.[PrerequisiteCourseId] = c1.[Id]
    );
END

IF OBJECT_ID(N'[degree_rules]') IS NOT NULL
BEGIN
    ;WITH ProgramBase AS (
        SELECT ap.[Id], ap.[TotalSemesters]
        FROM [academic_programs] ap
    )
    INSERT INTO [degree_rules] ([Id], [AcademicProgramId], [MinTotalCredits], [MinCoreCredits], [MinElectiveCredits], [MinGpa], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT NEWID(), pb.[Id],
           CASE WHEN pb.[TotalSemesters] <= 2 THEN 40 WHEN pb.[TotalSemesters] <= 4 THEN 72 ELSE 132 END,
           CASE WHEN pb.[TotalSemesters] <= 2 THEN 24 WHEN pb.[TotalSemesters] <= 4 THEN 48 ELSE 96 END,
           CASE WHEN pb.[TotalSemesters] <= 2 THEN 16 WHEN pb.[TotalSemesters] <= 4 THEN 24 ELSE 36 END,
           CAST(2.00 AS DECIMAL(4,2)),
           @Now, NULL, 0, NULL
    FROM ProgramBase pb
    WHERE NOT EXISTS (SELECT 1 FROM [degree_rules] x WHERE x.[AcademicProgramId] = pb.[Id]);
END

IF OBJECT_ID(N'[degree_rule_required_courses]') IS NOT NULL
BEGIN
    ;WITH RuleCourses AS (
        SELECT dr.[Id] AS DegreeRuleId, c.[Id] AS CourseId,
               ROW_NUMBER() OVER (PARTITION BY dr.[Id] ORDER BY c.[Code], c.[Id]) AS rn
        FROM [degree_rules] dr
        INNER JOIN [academic_programs] ap ON ap.[Id] = dr.[AcademicProgramId]
        INNER JOIN [courses] c ON c.[DepartmentId] = ap.[DepartmentId]
    )
    INSERT INTO [degree_rule_required_courses] ([Id], [DegreeRuleId], [CourseId], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), rc.[DegreeRuleId], rc.[CourseId], @Now, NULL
    FROM RuleCourses rc
    WHERE rc.rn <= 3
      AND NOT EXISTS (
          SELECT 1 FROM [degree_rule_required_courses] x
          WHERE x.[DegreeRuleId] = rc.[DegreeRuleId] AND x.[CourseId] = rc.[CourseId]
      );
END

IF OBJECT_ID(N'[course_grading_configs]') IS NOT NULL
BEGIN
    INSERT INTO [course_grading_configs] ([Id], [CourseId], [PassThreshold], [GradingType], [GradeRangesJson], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), c.[Id], CAST(50.00 AS DECIMAL(5,2)),
           CASE WHEN COL_LENGTH('courses', 'GradingType') IS NOT NULL THEN COALESCE(c.[GradingType], N'GPA') ELSE N'GPA' END,
           N'[{"grade":"A","min":85},{"grade":"B","min":70},{"grade":"C","min":55},{"grade":"D","min":50}]',
           @Now, NULL
    FROM [courses] c
    WHERE NOT EXISTS (SELECT 1 FROM [course_grading_configs] x WHERE x.[CourseId] = c.[Id]);
END

IF OBJECT_ID(N'[academic_deadlines]') IS NOT NULL
BEGIN
    ;WITH SemesterBase AS (
        SELECT s.[Id], s.[StartDate], s.[EndDate], ROW_NUMBER() OVER (ORDER BY s.[StartDate], s.[Id]) AS rn
        FROM [semesters] s
    )
    INSERT INTO [academic_deadlines] ([Id], [SemesterId], [Title], [Description], [DeadlineDate], [ReminderDaysBefore], [IsActive], [LastReminderSentAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT('45454545-4545-4545-4545-', RIGHT('000000000000' + CAST((sb.rn * 10 + d.[Ordinal]) AS VARCHAR(12)), 12))),
        sb.[Id],
        d.[Title],
        d.[Description],
        DATEADD(day, d.[OffsetDays], sb.[StartDate]),
        3,
        1,
        NULL,
        @Now,
        NULL,
        0,
        NULL
    FROM SemesterBase sb
    CROSS APPLY (VALUES
        (1, N'Enrollment Deadline', N'Final date for enrollment changes.', 14),
        (2, N'Midterm Submission Window', N'Mid-semester evaluation and assignment checkpoint.', 60),
        (3, N'Final Exam Preparation', N'Preparation window before semester closure.', 100)
    ) d([Ordinal], [Title], [Description], [OffsetDays])
    WHERE NOT EXISTS (
        SELECT 1
        FROM [academic_deadlines] x
        WHERE x.[SemesterId] = sb.[Id] AND x.[Title] = d.[Title]
    );
END

IF OBJECT_ID(N'[study_plans]') IS NOT NULL
BEGIN
    ;WITH StudentBase AS (
        SELECT sp.[Id], sp.[DepartmentId], ROW_NUMBER() OVER (ORDER BY sp.[RegistrationNumber], sp.[Id]) AS rn
        FROM [student_profiles] sp
    )
    INSERT INTO [study_plans] ([Id], [StudentProfileId], [PlannedSemesterName], [Notes], [AdvisorStatus], [AdvisorNotes], [ReviewedByUserId], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT('46464646-4646-4646-4646-', RIGHT('000000000000' + CAST(sb.rn AS VARCHAR(12)), 12))),
        sb.[Id],
        N'Spring 2026',
        N'Auto-generated study plan for high-volume demo data.',
        1,
        N'Initial advisor review completed.',
        @SuperAdminUserId,
        @Now,
        NULL,
        0,
        NULL
    FROM StudentBase sb
    WHERE NOT EXISTS (SELECT 1 FROM [study_plans] x WHERE x.[StudentProfileId] = sb.[Id]);
END

IF OBJECT_ID(N'[study_plan_courses]') IS NOT NULL
BEGIN
    ;WITH PlanCourses AS (
        SELECT spc.[Id] AS StudyPlanId, c.[Id] AS CourseId,
               ROW_NUMBER() OVER (PARTITION BY spc.[Id] ORDER BY c.[Code], c.[Id]) AS rn
        FROM [study_plans] spc
        INNER JOIN [student_profiles] sp ON sp.[Id] = spc.[StudentProfileId]
        INNER JOIN [courses] c ON c.[DepartmentId] = sp.[DepartmentId]
    )
    INSERT INTO [study_plan_courses] ([Id], [StudyPlanId], [CourseId], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), pc.[StudyPlanId], pc.[CourseId], @Now, NULL
    FROM PlanCourses pc
    WHERE pc.rn <= 4
      AND NOT EXISTS (
          SELECT 1 FROM [study_plan_courses] x
          WHERE x.[StudyPlanId] = pc.[StudyPlanId] AND x.[CourseId] = pc.[CourseId]
      );
END

IF OBJECT_ID(N'[course_announcements]') IS NOT NULL
BEGIN
    ;WITH OfferingBase AS (
        SELECT o.[Id], ROW_NUMBER() OVER (ORDER BY o.[Id]) AS rn
        FROM [course_offerings] o
    )
    INSERT INTO [course_announcements] ([Id], [OfferingId], [AuthorId], [Title], [Body], [PostedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT('47474747-4747-4747-4747-', RIGHT('000000000000' + CAST((ob.rn * 10 + a.[Ordinal]) AS VARCHAR(12)), 12))),
        ob.[Id],
        COALESCE((SELECT TOP 1 o2.[FacultyUserId] FROM [course_offerings] o2 WHERE o2.[Id] = ob.[Id]), @SuperAdminUserId),
        a.[Title],
        a.[Body],
        DATEADD(day, -a.[Ordinal], @Now),
        @Now,
        NULL,
        0,
        NULL
    FROM OfferingBase ob
    CROSS APPLY (VALUES
        (1, N'Course Kickoff', N'Welcome note and semester expectations.'),
        (2, N'Weekly Update', N'Important weekly milestones and reading plan.')
    ) a([Ordinal], [Title], [Body])
    WHERE NOT EXISTS (
        SELECT 1
        FROM [course_announcements] x
        WHERE x.[OfferingId] = ob.[Id] AND x.[Title] = a.[Title]
    );
END

IF OBJECT_ID(N'[course_content_modules]') IS NOT NULL
BEGIN
    ;WITH OfferingBase AS (
        SELECT o.[Id], ROW_NUMBER() OVER (ORDER BY o.[Id]) AS rn
        FROM [course_offerings] o
    )
    INSERT INTO [course_content_modules] ([Id], [OfferingId], [Title], [WeekNumber], [Body], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT('48484848-4848-4848-4848-', RIGHT('000000000000' + CAST((ob.rn * 10 + w.[WeekNumber]) AS VARCHAR(12)), 12))),
        ob.[Id],
        CONCAT(N'Week ', w.[WeekNumber], N' Learning Module'),
        w.[WeekNumber],
        CONCAT(N'Content package for week ', w.[WeekNumber], N' including slides, readings, and lab work.'),
        1,
        DATEADD(day, -w.[WeekNumber], @Now),
        @Now,
        NULL,
        0,
        NULL
    FROM OfferingBase ob
    CROSS APPLY (VALUES (1), (2), (3), (4)) w([WeekNumber])
    WHERE NOT EXISTS (
        SELECT 1
        FROM [course_content_modules] x
        WHERE x.[OfferingId] = ob.[Id] AND x.[WeekNumber] = w.[WeekNumber]
    );
END

IF OBJECT_ID(N'[content_videos]') IS NOT NULL
BEGIN
    INSERT INTO [content_videos] ([Id], [ModuleId], [Title], [StorageUrl], [EmbedUrl], [DurationSeconds], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT NEWID(), m.[Id], CONCAT(m.[Title], N' - Video Walkthrough'),
           CONCAT(N'https://cdn.demo.local/videos/', CONVERT(NVARCHAR(36), m.[Id]), N'.mp4'),
           CONCAT(N'https://video.demo.local/embed/', CONVERT(NVARCHAR(36), m.[Id])),
           900 + (ABS(CHECKSUM(m.[Id])) % 1800),
           @Now,
           NULL,
           0,
           NULL
    FROM [course_content_modules] m
    WHERE NOT EXISTS (SELECT 1 FROM [content_videos] x WHERE x.[ModuleId] = m.[Id]);
END

IF OBJECT_ID(N'[rubrics]') IS NOT NULL
BEGIN
    INSERT INTO [rubrics] ([Id], [AssignmentId], [Title], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT NEWID(), a.[Id], CONCAT(a.[Title], N' Rubric'), 1, @Now, NULL, 0, NULL
    FROM [assignments] a
    WHERE NOT EXISTS (SELECT 1 FROM [rubrics] x WHERE x.[AssignmentId] = a.[Id]);
END

IF OBJECT_ID(N'[rubric_criteria]') IS NOT NULL
BEGIN
    INSERT INTO [rubric_criteria] ([Id], [RubricId], [Name], [MaxPoints], [DisplayOrder], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), r.[Id], c.[Name], c.[MaxPoints], c.[DisplayOrder], @Now, NULL
    FROM [rubrics] r
    CROSS APPLY (VALUES
        (N'Problem Understanding', CAST(10.00 AS DECIMAL(8,2)), 1),
        (N'Implementation Quality', CAST(10.00 AS DECIMAL(8,2)), 2),
        (N'Documentation and Clarity', CAST(5.00 AS DECIMAL(8,2)), 3)
    ) c([Name], [MaxPoints], [DisplayOrder])
    WHERE NOT EXISTS (
        SELECT 1 FROM [rubric_criteria] x
        WHERE x.[RubricId] = r.[Id] AND x.[Name] = c.[Name]
    );
END

IF OBJECT_ID(N'[rubric_levels]') IS NOT NULL
BEGIN
    INSERT INTO [rubric_levels] ([Id], [CriterionId], [Label], [PointsAwarded], [DisplayOrder], [RubricCriterionId], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), rc.[Id], lv.[Label], lv.[PointsAwarded], lv.[DisplayOrder], rc.[Id], @Now, NULL
    FROM [rubric_criteria] rc
    CROSS APPLY (VALUES
        (N'Excellent', rc.[MaxPoints], 1),
        (N'Good', CAST(rc.[MaxPoints] * 0.80 AS DECIMAL(8,2)), 2),
        (N'Fair', CAST(rc.[MaxPoints] * 0.60 AS DECIMAL(8,2)), 3),
        (N'Needs Improvement', CAST(rc.[MaxPoints] * 0.40 AS DECIMAL(8,2)), 4)
    ) lv([Label], [PointsAwarded], [DisplayOrder])
    WHERE NOT EXISTS (
        SELECT 1 FROM [rubric_levels] x
        WHERE x.[CriterionId] = rc.[Id] AND x.[Label] = lv.[Label]
    );
END

IF OBJECT_ID(N'[rubric_student_grades]') IS NOT NULL
BEGIN
    ;WITH SubmissionCriteria AS (
        SELECT s.[Id] AS AssignmentSubmissionId,
               rc.[Id] AS RubricCriterionId,
               rl.[Id] AS RubricLevelId,
               ROW_NUMBER() OVER (PARTITION BY s.[Id], rc.[Id] ORDER BY rl.[DisplayOrder]) AS rn
        FROM [assignment_submissions] s
        INNER JOIN [rubrics] r ON r.[AssignmentId] = s.[AssignmentId]
        INNER JOIN [rubric_criteria] rc ON rc.[RubricId] = r.[Id]
        INNER JOIN [rubric_levels] rl ON rl.[CriterionId] = rc.[Id]
    )
    INSERT INTO [rubric_student_grades] ([Id], [AssignmentSubmissionId], [RubricCriterionId], [RubricLevelId], [PointsAwarded], [GradedByUserId], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), sc.[AssignmentSubmissionId], sc.[RubricCriterionId], sc.[RubricLevelId], rl.[PointsAwarded],
           COALESCE(sub.[GradedByUserId], @SuperAdminUserId), @Now, NULL
    FROM SubmissionCriteria sc
    INNER JOIN [rubric_levels] rl ON rl.[Id] = sc.[RubricLevelId]
    INNER JOIN [assignment_submissions] sub ON sub.[Id] = sc.[AssignmentSubmissionId]
    WHERE sc.rn = 1
      AND NOT EXISTS (
          SELECT 1
          FROM [rubric_student_grades] x
          WHERE x.[AssignmentSubmissionId] = sc.[AssignmentSubmissionId]
            AND x.[RubricCriterionId] = sc.[RubricCriterionId]
      );
END

IF OBJECT_ID(N'[fyp_projects]') IS NOT NULL
BEGIN
    DECLARE @FypCandidateStudents TABLE
    (
        StudentProfileId UNIQUEIDENTIFIER,
        DepartmentId UNIQUEIDENTIFIER,
        rn INT
    );

    DECLARE @FypFacultyBase TABLE
    (
        FacultyUserId UNIQUEIDENTIFIER,
        rn INT
    );

    INSERT INTO @FypCandidateStudents (StudentProfileId, DepartmentId, rn)
    SELECT TOP 24 sp.[Id], sp.[DepartmentId], ROW_NUMBER() OVER (ORDER BY sp.[RegistrationNumber], sp.[Id])
    FROM [student_profiles] sp
    ORDER BY sp.[RegistrationNumber], sp.[Id];

    INSERT INTO @FypFacultyBase (FacultyUserId, rn)
    SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Username], u.[Id])
    FROM [users] u
    WHERE u.[RoleId] = @RoleFaculty;

    DECLARE @FypFacultyCount INT = (SELECT COUNT(1) FROM @FypFacultyBase);

    IF @FypFacultyCount = 0
    BEGIN
        SET @FypFacultyCount = 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = N'fyp_projects'
          AND COLUMN_NAME = N'Status'
          AND DATA_TYPE IN (N'int', N'smallint', N'tinyint', N'bigint')
    )
    BEGIN
        INSERT INTO [fyp_projects]
            ([Id], [StudentProfileId], [DepartmentId], [Title], [Description], [Status], [SupervisorUserId], [CoordinatorRemarks], [CreatedAt], [UpdatedAt], [CompletionApprovedByUserIdsCsv], [CompletionRequestedAt], [CompletionRequestedByStudentProfileId], [IsCompletionRequested])
        SELECT
            CONVERT(uniqueidentifier, CONCAT('49494949-4949-4949-4949-', RIGHT('000000000000' + CAST(cs.rn AS VARCHAR(12)), 12))),
            cs.[StudentProfileId],
            cs.[DepartmentId],
            CONCAT(N'Capstone Project ', FORMAT(cs.rn, '00')),
            N'Full-stack academic automation and analytics project for enterprise-scale institution workflows.',
            CASE WHEN cs.rn % 3 = 0 THEN 2 ELSE 1 END,
            fb.[FacultyUserId],
            N'Auto-generated coordinator note for demo coverage.',
            @Now,
            NULL,
            NULL,
            NULL,
            NULL,
            0
        FROM @FypCandidateStudents cs
        INNER JOIN @FypFacultyBase fb ON fb.rn = ((cs.rn - 1) % @FypFacultyCount) + 1
        WHERE NOT EXISTS (SELECT 1 FROM [fyp_projects] x WHERE x.[StudentProfileId] = cs.[StudentProfileId]);
    END
    ELSE
    BEGIN
        INSERT INTO [fyp_projects]
            ([Id], [StudentProfileId], [DepartmentId], [Title], [Description], [Status], [SupervisorUserId], [CoordinatorRemarks], [CreatedAt], [UpdatedAt], [CompletionApprovedByUserIdsCsv], [CompletionRequestedAt], [CompletionRequestedByStudentProfileId], [IsCompletionRequested])
        SELECT
            CONVERT(uniqueidentifier, CONCAT('49494949-4949-4949-4949-', RIGHT('000000000000' + CAST(cs.rn AS VARCHAR(12)), 12))),
            cs.[StudentProfileId],
            cs.[DepartmentId],
            CONCAT(N'Capstone Project ', FORMAT(cs.rn, '00')),
            N'Full-stack academic automation and analytics project for enterprise-scale institution workflows.',
            CASE WHEN cs.rn % 3 = 0 THEN N'UnderReview' ELSE N'InProgress' END,
            fb.[FacultyUserId],
            N'Auto-generated coordinator note for demo coverage.',
            @Now,
            NULL,
            NULL,
            NULL,
            NULL,
            0
        FROM @FypCandidateStudents cs
        INNER JOIN @FypFacultyBase fb ON fb.rn = ((cs.rn - 1) % @FypFacultyCount) + 1
        WHERE NOT EXISTS (SELECT 1 FROM [fyp_projects] x WHERE x.[StudentProfileId] = cs.[StudentProfileId]);
    END
END

IF OBJECT_ID(N'[fyp_meetings]') IS NOT NULL
BEGIN
    INSERT INTO [fyp_meetings] ([Id], [FypProjectId], [ScheduledAt], [Venue], [Agenda], [Status], [OrganiserUserId], [Minutes], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), p.[Id], DATEADD(day, -3, @Now), N'Innovation Lab 2', N'Progress review and milestone alignment.', N'Completed',
           COALESCE(p.[SupervisorUserId], @SuperAdminUserId), N'Initial progress meeting completed with action items.', @Now, NULL
    FROM [fyp_projects] p
    WHERE NOT EXISTS (SELECT 1 FROM [fyp_meetings] x WHERE x.[FypProjectId] = p.[Id]);
END

IF OBJECT_ID(N'[fyp_panel_members]') IS NOT NULL
BEGIN
    ;WITH FacultyBase AS (
        SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Username], u.[Id]) AS rn
        FROM [users] u
        WHERE u.[RoleId] = @RoleFaculty
    ), ProjectBase AS (
        SELECT p.[Id], ROW_NUMBER() OVER (ORDER BY p.[Id]) AS rn
        FROM [fyp_projects] p
    )
    INSERT INTO [fyp_panel_members] ([Id], [FypProjectId], [UserId], [Role], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), pb.[Id], fb.[Id], roleMap.[RoleName], @Now, NULL
    FROM ProjectBase pb
    CROSS APPLY (VALUES (N'Supervisor', 0), (N'Internal', 1)) roleMap([RoleName], [Offset])
    INNER JOIN FacultyBase fb ON fb.rn = ((pb.rn + roleMap.[Offset] - 1) % (SELECT COUNT(1) FROM FacultyBase)) + 1
    WHERE NOT EXISTS (
        SELECT 1 FROM [fyp_panel_members] x
        WHERE x.[FypProjectId] = pb.[Id] AND x.[Role] = roleMap.[RoleName]
    );
END

IF OBJECT_ID(N'[chat_conversations]') IS NOT NULL
BEGIN
    DECLARE @ChatConversations TABLE (
        Id UNIQUEIDENTIFIER,
        UserId UNIQUEIDENTIFIER,
        UserRole NVARCHAR(50),
        DepartmentId UNIQUEIDENTIFIER NULL,
        StartedAt DATETIME2
    );

    INSERT INTO @ChatConversations VALUES
    ('50505050-5050-5050-5050-505050505001', '88888888-8888-8888-8888-888888888811', N'Student', '11111111-1111-1111-1111-111111111111', DATEADD(minute, -5, @Now)),
    ('50505050-5050-5050-5050-505050505002', '88888888-8888-8888-8888-888888888812', N'Student', '11111111-1111-1111-1111-111111111111', DATEADD(minute, -10, @Now)),
    ('50505050-5050-5050-5050-505050505003', '88888888-8888-8888-8888-888888888818', N'Student', '11111111-1111-1111-1111-111111111112', DATEADD(minute, -15, @Now)),
    ('50505050-5050-5050-5050-505050505004', '88888888-8888-8888-8888-888888888831', N'Student', '12222222-2222-2222-2222-222222222221', DATEADD(minute, -20, @Now)),
    ('50505050-5050-5050-5050-505050505005', '88888888-8888-8888-8888-888888888841', N'Student', '13333333-3333-3333-3333-333333333331', DATEADD(minute, -25, @Now)),
    ('50505050-5050-5050-5050-505050505006', '77777777-7777-7777-7777-777777777711', N'Faculty', '11111111-1111-1111-1111-111111111111', DATEADD(minute, -30, @Now)),
    ('50505050-5050-5050-5050-505050505007', '77777777-7777-7777-7777-777777777714', N'Faculty', '11111111-1111-1111-1111-111111111112', DATEADD(minute, -35, @Now)),
    ('50505050-5050-5050-5050-505050505008', '77777777-7777-7777-7777-777777777721', N'Faculty', '12222222-2222-2222-2222-222222222221', DATEADD(minute, -40, @Now)),
    ('50505050-5050-5050-5050-505050505009', '77777777-7777-7777-7777-777777777731', N'Faculty', '13333333-3333-3333-3333-333333333331', DATEADD(minute, -45, @Now)),
    ('50505050-5050-5050-5050-505050505010', '66666666-6666-6666-6666-666666666611', N'Admin', '11111111-1111-1111-1111-111111111111', DATEADD(minute, -50, @Now));

    INSERT INTO [chat_conversations] ([Id], [UserId], [UserRole], [DepartmentId], [StartedAt])
    SELECT c.[Id], c.[UserId], c.[UserRole], c.[DepartmentId], c.[StartedAt]
    FROM @ChatConversations c
    WHERE NOT EXISTS (SELECT 1 FROM [chat_conversations] x WHERE x.[Id] = c.[Id]);
END

IF OBJECT_ID(N'[chat_messages]') IS NOT NULL
BEGIN
    INSERT INTO [chat_messages] ([Id], [ConversationId], [Role], [Content], [SentAt], [TokensUsed])
    SELECT NEWID(), c.[Id], m.[Role], m.[Content], DATEADD(second, m.[OffsetSeconds], c.[StartedAt]), m.[TokensUsed]
    FROM [chat_conversations] c
    CROSS APPLY (VALUES
        (1, N'user', N'Please help summarize this week''s key topics and action items.', 120),
        (2, N'assistant', N'Here is a concise weekly summary with priorities and deadlines.', 180),
        (3, N'user', N'Generate a quick revision checklist for exams.', 110),
        (4, N'assistant', N'Revision checklist generated with topic clusters and practice targets.', 210)
    ) m([OffsetSeconds], [Role], [Content], [TokensUsed])
    WHERE NOT EXISTS (
        SELECT 1 FROM [chat_messages] x
        WHERE x.[ConversationId] = c.[Id] AND x.[Role] = m.[Role] AND x.[Content] = m.[Content]
    );
END

IF OBJECT_ID(N'[consumed_verification_keys]') IS NOT NULL
BEGIN
    ;WITH Nums AS (
        SELECT TOP 120 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects
    )
    INSERT INTO [consumed_verification_keys] ([Id], [KeyHash], [ConsumedAt], [CreatedAt], [UpdatedAt])
    SELECT
        NEWID(),
        CONCAT(N'SEEDKEY-', RIGHT(N'000000' + CAST(n AS NVARCHAR(12)), 6)),
        DATEADD(day, -n, @Now),
        DATEADD(day, -n, @Now),
        NULL
    FROM Nums
    WHERE NOT EXISTS (
        SELECT 1
        FROM [consumed_verification_keys] x
        WHERE x.[KeyHash] = CONCAT(N'SEEDKEY-', RIGHT(N'000000' + CAST(n AS NVARCHAR(12)), 6))
    );
END

IF OBJECT_ID(N'[user_sessions]') IS NOT NULL
BEGIN
    INSERT INTO [user_sessions] ([Id], [UserId], [RefreshTokenHash], [DeviceInfo], [IpAddress], [ExpiresAt], [RevokedAt], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), u.[Id], CONCAT(N'seed-refresh-', CONVERT(NVARCHAR(36), u.[Id])),
           N'Chrome on Windows', CONCAT(N'10.0.0.', (ABS(CHECKSUM(u.[Id])) % 200) + 20), DATEADD(day, 30, @Now), NULL, @Now, NULL
    FROM [users] u
    WHERE u.[IsActive] = 1
      AND NOT EXISTS (
          SELECT 1 FROM [user_sessions] x
          WHERE x.[UserId] = u.[Id] AND x.[RefreshTokenHash] = CONCAT(N'seed-refresh-', CONVERT(NVARCHAR(36), u.[Id]))
      );
END

IF OBJECT_ID(N'[password_history]') IS NOT NULL
BEGIN
    INSERT INTO [password_history] ([Id], [UserId], [PasswordHash], [CreatedAt])
    SELECT NEWID(), u.[Id], @PwdHash, DATEADD(day, -90, @Now)
    FROM [users] u
    WHERE NOT EXISTS (
        SELECT 1
        FROM [password_history] x
        WHERE x.[UserId] = u.[Id]
          AND x.[PasswordHash] = @PwdHash
    );
END

IF OBJECT_ID(N'[outbound_email_logs]') IS NOT NULL
BEGIN
    ;WITH Nums AS (
        SELECT TOP 240 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects
    )
    INSERT INTO [outbound_email_logs] ([Id], [ToAddress], [Subject], [Status], [ErrorMessage], [AttemptedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT('52525252-5252-5252-5252-', RIGHT('000000000000' + CAST(n AS VARCHAR(12)), 12))),
        CONCAT(N'user', n, N'@demo.local'),
        CONCAT(N'Notification Batch ', n),
        CASE WHEN n % 11 = 0 THEN N'Failed' WHEN n % 5 = 0 THEN N'Retrying' ELSE N'Sent' END,
        CASE WHEN n % 11 = 0 THEN N'SMTP timeout in seed simulation.' ELSE NULL END,
        DATEADD(minute, -n, @Now)
    FROM Nums
    WHERE NOT EXISTS (
        SELECT 1
        FROM [outbound_email_logs] x
        WHERE x.[Id] = CONVERT(uniqueidentifier, CONCAT('52525252-5252-5252-5252-', RIGHT('000000000000' + CAST(n AS VARCHAR(12)), 12)))
    );
END

IF OBJECT_ID(N'[audit_logs]') IS NOT NULL
BEGIN
    ;WITH UserBase AS (
        SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Username], u.[Id]) AS rn
        FROM [users] u
    ), UserCount AS (
        SELECT COUNT(1) AS cnt FROM UserBase
    ), Nums AS (
        SELECT TOP 400 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects
    )
    INSERT INTO [audit_logs] ([ActorUserId], [Action], [EntityName], [EntityId], [OldValuesJson], [NewValuesJson], [OccurredAt], [IpAddress])
    SELECT
        ub.[Id],
        CASE n.n % 6 WHEN 0 THEN N'Create' WHEN 1 THEN N'Update' WHEN 2 THEN N'Publish' WHEN 3 THEN N'Approve' WHEN 4 THEN N'Assign' ELSE N'Generate' END,
        CASE n.n % 7 WHEN 0 THEN N'Course' WHEN 1 THEN N'Result' WHEN 2 THEN N'Attendance' WHEN 3 THEN N'Announcement' WHEN 4 THEN N'Assignment' WHEN 5 THEN N'FypProject' ELSE N'SupportTicket' END,
        CONVERT(NVARCHAR(100), NEWID()),
        N'{"before":"seed"}',
        N'{"after":"seed-updated"}',
        DATEADD(minute, -n.n, @Now),
        CONCAT(N'192.168.1.', (n.n % 200) + 10)
    FROM Nums n
    CROSS JOIN UserCount uc
    INNER JOIN UserBase ub ON ub.rn = ((n.n - 1) % uc.cnt) + 1;
END

IF OBJECT_ID(N'[admin_change_requests]') IS NOT NULL
BEGIN
    ;WITH Admins AS (
        SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Username], u.[Id]) AS rn
        FROM [users] u WHERE u.[RoleId] IN (@RoleAdmin, @RoleSuperAdmin)
    ), Nums AS (
        SELECT TOP 24 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects
    )
    INSERT INTO [admin_change_requests]
        ([Id], [RequestorUserId], [ReviewedByUserId], [Status], [ChangeDescription], [Reason], [NewData], [AdminNotes], [ReviewedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT('53535353-5353-5353-5353-', RIGHT('000000000000' + CAST(n.n AS VARCHAR(12)), 12))),
        rq.[Id], rv.[Id],
        CASE WHEN n.n % 3 = 0 THEN 2 WHEN n.n % 2 = 0 THEN 1 ELSE 0 END,
        N'Admin profile scope update request',
        N'Operational realignment for academic period transition.',
        N'{"scope":"department-access","requestedBy":"seed"}',
        CASE WHEN n.n % 2 = 0 THEN N'Auto-approved in demo data set.' ELSE NULL END,
        CASE WHEN n.n % 2 = 0 THEN DATEADD(day, -1, @Now) ELSE NULL END,
        DATEADD(day, -n.n, @Now),
        @Now,
        0,
        NULL
    FROM Nums n
    CROSS JOIN (SELECT COUNT(1) AS cnt FROM Admins) c
    INNER JOIN Admins rq ON rq.rn = ((n.n - 1) % c.cnt) + 1
    INNER JOIN Admins rv ON rv.rn = ((n.n) % c.cnt) + 1
    WHERE NOT EXISTS (
        SELECT 1 FROM [admin_change_requests] x
        WHERE x.[Id] = CONVERT(uniqueidentifier, CONCAT('53535353-5353-5353-5353-', RIGHT('000000000000' + CAST(n.n AS VARCHAR(12)), 12)))
    );
END

IF OBJECT_ID(N'[teacher_modification_requests]') IS NOT NULL
BEGIN
    ;WITH FacultyBase AS (
        SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Username], u.[Id]) AS rn
        FROM [users] u WHERE u.[RoleId] = @RoleFaculty
    ), ReviewerBase AS (
        SELECT u.[Id], ROW_NUMBER() OVER (ORDER BY u.[Username], u.[Id]) AS rn
        FROM [users] u WHERE u.[RoleId] IN (@RoleAdmin, @RoleSuperAdmin)
    ), Nums AS (
        SELECT TOP 40 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects
    )
    INSERT INTO [teacher_modification_requests]
        ([Id], [TeacherUserId], [ReviewedByUserId], [ModificationType], [RecordId], [Status], [Reason], [ProposedData], [AdminNotes], [ReviewedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        CONVERT(uniqueidentifier, CONCAT('54545454-5454-5454-5454-', RIGHT('000000000000' + CAST(n.n AS VARCHAR(12)), 12))),
        fb.[Id],
        rb.[Id],
        n.n % 5,
        NEWID(),
        CASE WHEN n.n % 4 = 0 THEN 2 WHEN n.n % 2 = 0 THEN 1 ELSE 0 END,
        CASE n.n % 5
            WHEN 0 THEN N'Marks correction and grade recalculation request.'
            WHEN 1 THEN N'Assignment score revision based on rubric review.'
            WHEN 2 THEN N'Quiz marks update requested after answer-key correction.'
            WHEN 3 THEN N'Final exam max-mark alignment and normalization request.'
            ELSE N'Result entry update requested after departmental moderation.'
        END,
        CASE n.n % 5
            WHEN 0 THEN N'{"requestedChange":"score-adjustment","source":"dummy-seed","section":"result-entry","examType":"Final","assessmentComponent":"Theory"}'
            WHEN 1 THEN N'{"requestedChange":"assignment-rescore","source":"dummy-seed","section":"result-entry","examType":"Assignment","assessmentComponent":"Practical"}'
            WHEN 2 THEN N'{"requestedChange":"quiz-regrade","source":"dummy-seed","section":"result-entry","examType":"Quiz","assessmentComponent":"Theory"}'
            WHEN 3 THEN N'{"requestedChange":"max-marks-correction","source":"dummy-seed","section":"result-entry","examType":"Final","assessmentComponent":"Viva"}'
            ELSE N'{"requestedChange":"grade-override-request","source":"dummy-seed","section":"result-entry","examType":"Midterm","assessmentComponent":"Theory"}'
        END,
        CASE WHEN n.n % 2 = 0 THEN N'Reviewed and synchronized.' ELSE NULL END,
        CASE WHEN n.n % 2 = 0 THEN DATEADD(day, -2, @Now) ELSE NULL END,
        DATEADD(day, -n.n, @Now),
        @Now,
        0,
        NULL
    FROM Nums n
    CROSS JOIN (SELECT COUNT(1) AS fcnt FROM FacultyBase) fc
    CROSS JOIN (SELECT COUNT(1) AS rcnt FROM ReviewerBase) rc
    INNER JOIN FacultyBase fb ON fb.rn = ((n.n - 1) % fc.fcnt) + 1
    INNER JOIN ReviewerBase rb ON rb.rn = ((n.n) % rc.rcnt) + 1
    WHERE NOT EXISTS (
        SELECT 1 FROM [teacher_modification_requests] x
        WHERE x.[Id] = CONVERT(uniqueidentifier, CONCAT('54545454-5454-5454-5454-', RIGHT('000000000000' + CAST(n.n AS VARCHAR(12)), 12)))
    );
END

IF OBJECT_ID(N'[parent_student_links]') IS NOT NULL
BEGIN
    DECLARE @ParentUsers TABLE (Id UNIQUEIDENTIFIER, Username NVARCHAR(100), Email NVARCHAR(256));
    INSERT INTO @ParentUsers (Id, Username, Email)
    VALUES
    ('55550000-0000-0000-0000-000000000001', N'parent.guardian.01', N'parent.guardian.01@demo.local'),
    ('55550000-0000-0000-0000-000000000002', N'parent.guardian.02', N'parent.guardian.02@demo.local'),
    ('55550000-0000-0000-0000-000000000003', N'parent.guardian.03', N'parent.guardian.03@demo.local'),
    ('55550000-0000-0000-0000-000000000004', N'parent.guardian.04', N'parent.guardian.04@demo.local'),
    ('55550000-0000-0000-0000-000000000005', N'parent.guardian.05', N'parent.guardian.05@demo.local'),
    ('55550000-0000-0000-0000-000000000006', N'parent.guardian.06', N'parent.guardian.06@demo.local'),
    ('55550000-0000-0000-0000-000000000007', N'parent.guardian.07', N'parent.guardian.07@demo.local'),
    ('55550000-0000-0000-0000-000000000008', N'parent.guardian.08', N'parent.guardian.08@demo.local'),
    ('55550000-0000-0000-0000-000000000009', N'parent.guardian.09', N'parent.guardian.09@demo.local'),
    ('55550000-0000-0000-0000-000000000010', N'parent.guardian.10', N'parent.guardian.10@demo.local');

    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT p.[Id], p.[Username], p.[Email], @PwdHash, COALESCE(@RoleParent, @RoleStudent), NULL, 0, 1, NULL, @Now, NULL, 0, NULL
    FROM @ParentUsers p
    WHERE NOT EXISTS (SELECT 1 FROM [users] x WHERE x.[Id] = p.[Id] OR x.[Username] = p.[Username]);

    DECLARE @ParentStudentLinks TABLE (Id UNIQUEIDENTIFIER, ParentUserId UNIQUEIDENTIFIER, StudentProfileId UNIQUEIDENTIFIER, Relationship NVARCHAR(60));
    INSERT INTO @ParentStudentLinks VALUES
    ('56565656-5656-5656-5656-565656565601', '55550000-0000-0000-0000-000000000001', '99999999-9999-9999-9999-999999999941', N'Mother'),
    ('56565656-5656-5656-5656-565656565602', '55550000-0000-0000-0000-000000000002', '99999999-9999-9999-9999-999999999942', N'Father'),
    ('56565656-5656-5656-5656-565656565603', '55550000-0000-0000-0000-000000000003', '99999999-9999-9999-9999-999999999943', N'Mother'),
    ('56565656-5656-5656-5656-565656565604', '55550000-0000-0000-0000-000000000004', '99999999-9999-9999-9999-999999999944', N'Father'),
    ('56565656-5656-5656-5656-565656565605', '55550000-0000-0000-0000-000000000005', '99999999-9999-9999-9999-999999999931', N'Mother'),
    ('56565656-5656-5656-5656-565656565606', '55550000-0000-0000-0000-000000000006', '99999999-9999-9999-9999-999999999932', N'Father'),
    ('56565656-5656-5656-5656-565656565607', '55550000-0000-0000-0000-000000000007', '99999999-9999-9999-9999-999999999933', N'Mother'),
    ('56565656-5656-5656-5656-565656565608', '55550000-0000-0000-0000-000000000008', '99999999-9999-9999-9999-999999999934', N'Father'),
    ('56565656-5656-5656-5656-565656565609', '55550000-0000-0000-0000-000000000009', '99999999-9999-9999-9999-999999999935', N'Mother'),
    ('56565656-5656-5656-5656-565656565610', '55550000-0000-0000-0000-000000000010', '99999999-9999-9999-9999-999999999924', N'Father');

    INSERT INTO [parent_student_links] ([Id], [ParentUserId], [StudentProfileId], [Relationship], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT l.[Id], l.[ParentUserId], l.[StudentProfileId], l.[Relationship], 1, @Now, NULL, 0, NULL
    FROM @ParentStudentLinks l
    WHERE NOT EXISTS (SELECT 1 FROM [parent_student_links] x WHERE x.[Id] = l.[Id]);
END

/* 18) Deterministic lifecycle coverage for School (Class 1-10), College (11-12), University (Semester 1-8 + Graduation) */
DECLARE @LifecycleSchoolUserId UNIQUEIDENTIFIER = CAST('88881111-2222-3333-4444-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleCollegeUserId UNIQUEIDENTIFIER = CAST('88881111-2222-3333-4444-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleUniversityUserId UNIQUEIDENTIFIER = CAST('88881111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER);

DECLARE @LifecycleSchoolProfileId UNIQUEIDENTIFIER = CAST('99991111-2222-3333-4444-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleCollegeProfileId UNIQUEIDENTIFIER = CAST('99991111-2222-3333-4444-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleUniversityProfileId UNIQUEIDENTIFIER = CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER);

DECLARE @LifecycleSchoolDepartmentId UNIQUEIDENTIFIER = CAST('13333333-3333-3333-3333-333333333331' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleCollegeDepartmentId UNIQUEIDENTIFIER = CAST('12222222-2222-2222-2222-222222222223' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleUniversityDepartmentId UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER);

DECLARE @LifecycleSchoolProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222431' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleCollegeProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222327' AS UNIQUEIDENTIFIER);
DECLARE @LifecycleUniversityProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER);

IF OBJECT_ID(N'[academic_programs]') IS NOT NULL
BEGIN
        INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT @LifecycleCollegeProgramId, N'College Intermediate (Class 11-12)', N'COL-INT-11-12', @LifecycleCollegeDepartmentId, 12, 1, @Now, NULL, 0, NULL
        WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] x WHERE x.[Id] = @LifecycleCollegeProgramId OR x.[Code] = N'COL-INT-11-12');
END

IF OBJECT_ID(N'[users]') IS NOT NULL
BEGIN
        INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT @LifecycleSchoolUserId, N'lifecycle.school.c1to10', N'lifecycle.school.c1to10@demo.local', @PwdHash, @RoleStudent, @LifecycleSchoolDepartmentId, 0, 1, @Now, NULL, 0, NULL
        WHERE NOT EXISTS (SELECT 1 FROM [users] u WHERE u.[Id] = @LifecycleSchoolUserId OR u.[Username] = N'lifecycle.school.c1to10');

        INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT @LifecycleCollegeUserId, N'lifecycle.college.c11to12', N'lifecycle.college.c11to12@demo.local', @PwdHash, @RoleStudent, @LifecycleCollegeDepartmentId, 1, 1, @Now, NULL, 0, NULL
        WHERE NOT EXISTS (SELECT 1 FROM [users] u WHERE u.[Id] = @LifecycleCollegeUserId OR u.[Username] = N'lifecycle.college.c11to12');

        INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT @LifecycleUniversityUserId, N'lifecycle.university.s1to8', N'lifecycle.university.s1to8@demo.local', @PwdHash, @RoleStudent, @LifecycleUniversityDepartmentId, 2, 1, @Now, NULL, 0, NULL
        WHERE NOT EXISTS (SELECT 1 FROM [users] u WHERE u.[Id] = @LifecycleUniversityUserId OR u.[Username] = N'lifecycle.university.s1to8');
END

IF OBJECT_ID(N'[student_profiles]') IS NOT NULL
BEGIN
        INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT @LifecycleSchoolProfileId, @LifecycleSchoolUserId, N'LCYC-SCH-001', @LifecycleSchoolProgramId, @LifecycleSchoolDepartmentId, DATEADD(YEAR, -10, @Now), 3.70, 10, @Now, NULL, 0, NULL
        WHERE NOT EXISTS (SELECT 1 FROM [student_profiles] sp WHERE sp.[Id] = @LifecycleSchoolProfileId OR sp.[RegistrationNumber] = N'LCYC-SCH-001');

        INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT @LifecycleCollegeProfileId, @LifecycleCollegeUserId, N'LCYC-COL-001', @LifecycleCollegeProgramId, @LifecycleCollegeDepartmentId, DATEADD(YEAR, -2, @Now), 3.55, 12, @Now, NULL, 0, NULL
        WHERE NOT EXISTS (SELECT 1 FROM [student_profiles] sp WHERE sp.[Id] = @LifecycleCollegeProfileId OR sp.[RegistrationNumber] = N'LCYC-COL-001');

        INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        SELECT @LifecycleUniversityProfileId, @LifecycleUniversityUserId, N'LCYC-UNI-001', @LifecycleUniversityProgramId, @LifecycleUniversityDepartmentId, DATEADD(YEAR, -4, @Now), 3.88, 8, @Now, NULL, 0, NULL
        WHERE NOT EXISTS (SELECT 1 FROM [student_profiles] sp WHERE sp.[Id] = @LifecycleUniversityProfileId OR sp.[RegistrationNumber] = N'LCYC-UNI-001');

        UPDATE [student_profiles]
        SET [CurrentSemesterNumber] = 10,
                [UpdatedAt] = @Now
        WHERE [Id] = @LifecycleSchoolProfileId
            AND [CurrentSemesterNumber] < 10;

        UPDATE [student_profiles]
        SET [CurrentSemesterNumber] = 12,
                [UpdatedAt] = @Now
        WHERE [Id] = @LifecycleCollegeProfileId
            AND [CurrentSemesterNumber] < 12;

        UPDATE [student_profiles]
        SET [CurrentSemesterNumber] = 8,
                [UpdatedAt] = @Now
        WHERE [Id] = @LifecycleUniversityProfileId
            AND [CurrentSemesterNumber] < 8;
END

DECLARE @LifecycleSchoolOfferingId UNIQUEIDENTIFIER;
DECLARE @LifecycleCollegeOfferingId UNIQUEIDENTIFIER;
DECLARE @LifecycleUniversityOfferingId UNIQUEIDENTIFIER;
DECLARE @LifecycleSchoolFacultyId UNIQUEIDENTIFIER;
DECLARE @LifecycleCollegeFacultyId UNIQUEIDENTIFIER;
DECLARE @LifecycleUniversityFacultyId UNIQUEIDENTIFIER;

IF OBJECT_ID(N'[course_offerings]') IS NOT NULL
BEGIN
        SELECT TOP (1)
                @LifecycleSchoolOfferingId = co.[Id]
        FROM [course_offerings] co
        INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
        WHERE c.[DepartmentId] = @LifecycleSchoolDepartmentId
        ORDER BY co.[CreatedAt] DESC, co.[Id] DESC;

        SELECT TOP (1)
                @LifecycleCollegeOfferingId = co.[Id]
        FROM [course_offerings] co
        INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
        WHERE c.[DepartmentId] = @LifecycleCollegeDepartmentId
        ORDER BY co.[CreatedAt] DESC, co.[Id] DESC;

        SELECT TOP (1)
                @LifecycleUniversityOfferingId = co.[Id]
        FROM [course_offerings] co
        INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
        WHERE c.[DepartmentId] = @LifecycleUniversityDepartmentId
        ORDER BY co.[CreatedAt] DESC, co.[Id] DESC;
END

IF OBJECT_ID(N'[users]') IS NOT NULL
BEGIN
        SELECT TOP (1) @LifecycleSchoolFacultyId = [Id]
        FROM [users]
        WHERE [RoleId] = @RoleFaculty
            AND [DepartmentId] = @LifecycleSchoolDepartmentId
        ORDER BY [Username];

        SELECT TOP (1) @LifecycleCollegeFacultyId = [Id]
        FROM [users]
        WHERE [RoleId] = @RoleFaculty
            AND [DepartmentId] = @LifecycleCollegeDepartmentId
        ORDER BY [Username];

        SELECT TOP (1) @LifecycleUniversityFacultyId = [Id]
        FROM [users]
        WHERE [RoleId] = @RoleFaculty
            AND [DepartmentId] = @LifecycleUniversityDepartmentId
        ORDER BY [Username];
END

SET @LifecycleSchoolFacultyId = COALESCE(@LifecycleSchoolFacultyId, @SuperAdminUserId);
SET @LifecycleCollegeFacultyId = COALESCE(@LifecycleCollegeFacultyId, @SuperAdminUserId);
SET @LifecycleUniversityFacultyId = COALESCE(@LifecycleUniversityFacultyId, @SuperAdminUserId);

IF OBJECT_ID(N'[enrollments]') IS NOT NULL
BEGIN
        INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleSchoolProfileId, @LifecycleSchoolOfferingId, @Now, NULL, N'Enrolled', @Now, NULL
        WHERE @LifecycleSchoolOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [enrollments] e WHERE e.[StudentProfileId] = @LifecycleSchoolProfileId AND e.[CourseOfferingId] = @LifecycleSchoolOfferingId);

        INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleCollegeProfileId, @LifecycleCollegeOfferingId, @Now, NULL, N'Enrolled', @Now, NULL
        WHERE @LifecycleCollegeOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [enrollments] e WHERE e.[StudentProfileId] = @LifecycleCollegeProfileId AND e.[CourseOfferingId] = @LifecycleCollegeOfferingId);

        INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleUniversityProfileId, @LifecycleUniversityOfferingId, @Now, NULL, N'Enrolled', @Now, NULL
        WHERE @LifecycleUniversityOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [enrollments] e WHERE e.[StudentProfileId] = @LifecycleUniversityProfileId AND e.[CourseOfferingId] = @LifecycleUniversityOfferingId);
END

IF OBJECT_ID(N'[attendance_records]') IS NOT NULL
BEGIN
        INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId], [Remarks], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleSchoolProfileId, @LifecycleSchoolOfferingId, DATEADD(day, -2, CAST(@Now AS date)), N'Present', @LifecycleSchoolFacultyId, N'Lifecycle school class-10 attendance', @Now, NULL
        WHERE @LifecycleSchoolOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [attendance_records] a WHERE a.[StudentProfileId] = @LifecycleSchoolProfileId AND a.[CourseOfferingId] = @LifecycleSchoolOfferingId AND a.[Date] = DATEADD(day, -2, CAST(@Now AS date)));

        INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId], [Remarks], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleCollegeProfileId, @LifecycleCollegeOfferingId, DATEADD(day, -2, CAST(@Now AS date)), N'Present', @LifecycleCollegeFacultyId, N'Lifecycle college class-12 attendance', @Now, NULL
        WHERE @LifecycleCollegeOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [attendance_records] a WHERE a.[StudentProfileId] = @LifecycleCollegeProfileId AND a.[CourseOfferingId] = @LifecycleCollegeOfferingId AND a.[Date] = DATEADD(day, -2, CAST(@Now AS date)));

        INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId], [Remarks], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleUniversityProfileId, @LifecycleUniversityOfferingId, DATEADD(day, -2, CAST(@Now AS date)), N'Present', @LifecycleUniversityFacultyId, N'Lifecycle university semester-8 attendance', @Now, NULL
        WHERE @LifecycleUniversityOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [attendance_records] a WHERE a.[StudentProfileId] = @LifecycleUniversityProfileId AND a.[CourseOfferingId] = @LifecycleUniversityOfferingId AND a.[Date] = DATEADD(day, -2, CAST(@Now AS date)));
END

IF OBJECT_ID(N'[results]') IS NOT NULL
BEGIN
        INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleSchoolProfileId, @LifecycleSchoolOfferingId, N'Final', 86.00, 100.00, 1, @Now, @LifecycleSchoolFacultyId, @Now, NULL
        WHERE @LifecycleSchoolOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [results] r WHERE r.[StudentProfileId] = @LifecycleSchoolProfileId AND r.[CourseOfferingId] = @LifecycleSchoolOfferingId AND r.[ResultType] = N'Final');

        INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleCollegeProfileId, @LifecycleCollegeOfferingId, N'Final', 81.00, 100.00, 1, @Now, @LifecycleCollegeFacultyId, @Now, NULL
        WHERE @LifecycleCollegeOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [results] r WHERE r.[StudentProfileId] = @LifecycleCollegeProfileId AND r.[CourseOfferingId] = @LifecycleCollegeOfferingId AND r.[ResultType] = N'Final');

        INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleUniversityProfileId, @LifecycleUniversityOfferingId, N'Final', 91.00, 100.00, 1, @Now, @LifecycleUniversityFacultyId, @Now, NULL
        WHERE @LifecycleUniversityOfferingId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [results] r WHERE r.[StudentProfileId] = @LifecycleUniversityProfileId AND r.[CourseOfferingId] = @LifecycleUniversityOfferingId AND r.[ResultType] = N'Final');
END

IF OBJECT_ID(N'[assignment_submissions]') IS NOT NULL
BEGIN
        DECLARE @LifecycleSchoolAssignmentId UNIQUEIDENTIFIER;
        DECLARE @LifecycleCollegeAssignmentId UNIQUEIDENTIFIER;
        DECLARE @LifecycleUniversityAssignmentId UNIQUEIDENTIFIER;

        SELECT TOP (1) @LifecycleSchoolAssignmentId = [Id]
        FROM [assignments]
        WHERE [CourseOfferingId] = @LifecycleSchoolOfferingId
        ORDER BY [CreatedAt] DESC, [Id] DESC;

        SELECT TOP (1) @LifecycleCollegeAssignmentId = [Id]
        FROM [assignments]
        WHERE [CourseOfferingId] = @LifecycleCollegeOfferingId
        ORDER BY [CreatedAt] DESC, [Id] DESC;

        SELECT TOP (1) @LifecycleUniversityAssignmentId = [Id]
        FROM [assignments]
        WHERE [CourseOfferingId] = @LifecycleUniversityOfferingId
        ORDER BY [CreatedAt] DESC, [Id] DESC;

        INSERT INTO [assignment_submissions] ([Id], [AssignmentId], [StudentProfileId], [FileUrl], [TextContent], [SubmittedAt], [MarksAwarded], [Feedback], [GradedAt], [GradedByUserId], [Status], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleSchoolAssignmentId, @LifecycleSchoolProfileId, NULL, N'Lifecycle school assignment submission for class-10 progression.', @Now, 85.00, N'Validated class progression evidence.', @Now, @LifecycleSchoolFacultyId, N'Graded', @Now, NULL
        WHERE @LifecycleSchoolAssignmentId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [assignment_submissions] x WHERE x.[AssignmentId] = @LifecycleSchoolAssignmentId AND x.[StudentProfileId] = @LifecycleSchoolProfileId);

        INSERT INTO [assignment_submissions] ([Id], [AssignmentId], [StudentProfileId], [FileUrl], [TextContent], [SubmittedAt], [MarksAwarded], [Feedback], [GradedAt], [GradedByUserId], [Status], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleCollegeAssignmentId, @LifecycleCollegeProfileId, NULL, N'Lifecycle college assignment submission for class-12 completion.', @Now, 80.00, N'Validated class 11-12 completion evidence.', @Now, @LifecycleCollegeFacultyId, N'Graded', @Now, NULL
        WHERE @LifecycleCollegeAssignmentId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [assignment_submissions] x WHERE x.[AssignmentId] = @LifecycleCollegeAssignmentId AND x.[StudentProfileId] = @LifecycleCollegeProfileId);

        INSERT INTO [assignment_submissions] ([Id], [AssignmentId], [StudentProfileId], [FileUrl], [TextContent], [SubmittedAt], [MarksAwarded], [Feedback], [GradedAt], [GradedByUserId], [Status], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), @LifecycleUniversityAssignmentId, @LifecycleUniversityProfileId, NULL, N'Lifecycle university assignment submission for semester-8 capstone pathway.', @Now, 92.00, N'Validated semester 8 completion evidence.', @Now, @LifecycleUniversityFacultyId, N'Graded', @Now, NULL
        WHERE @LifecycleUniversityAssignmentId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [assignment_submissions] x WHERE x.[AssignmentId] = @LifecycleUniversityAssignmentId AND x.[StudentProfileId] = @LifecycleUniversityProfileId);
END

IF OBJECT_ID(N'[student_report_cards]') IS NOT NULL
BEGIN
        INSERT INTO [student_report_cards] ([Id], [StudentProfileId], [InstitutionType], [PeriodLabel], [PayloadJson], [GeneratedByUserId], [GeneratedAt], [CreatedAt], [UpdatedAt])
        SELECT NEWID(), v.[StudentProfileId], v.[InstitutionType], v.[PeriodLabel], v.[PayloadJson], v.[GeneratedByUserId], DATEADD(day, -v.[OrderNo], @Now), @Now, NULL
        FROM
        (
                VALUES
                (@LifecycleSchoolProfileId, 0, N'Class 1', N'{"phase":"lifecycle","progression":"school","class":1,"score":74.0}', @LifecycleSchoolFacultyId, 10),
                (@LifecycleSchoolProfileId, 0, N'Class 2', N'{"phase":"lifecycle","progression":"school","class":2,"score":75.0}', @LifecycleSchoolFacultyId, 9),
                (@LifecycleSchoolProfileId, 0, N'Class 3', N'{"phase":"lifecycle","progression":"school","class":3,"score":76.0}', @LifecycleSchoolFacultyId, 8),
                (@LifecycleSchoolProfileId, 0, N'Class 4', N'{"phase":"lifecycle","progression":"school","class":4,"score":77.0}', @LifecycleSchoolFacultyId, 7),
                (@LifecycleSchoolProfileId, 0, N'Class 5', N'{"phase":"lifecycle","progression":"school","class":5,"score":78.0}', @LifecycleSchoolFacultyId, 6),
                (@LifecycleSchoolProfileId, 0, N'Class 6', N'{"phase":"lifecycle","progression":"school","class":6,"score":79.0}', @LifecycleSchoolFacultyId, 5),
                (@LifecycleSchoolProfileId, 0, N'Class 7', N'{"phase":"lifecycle","progression":"school","class":7,"score":80.0}', @LifecycleSchoolFacultyId, 4),
                (@LifecycleSchoolProfileId, 0, N'Class 8', N'{"phase":"lifecycle","progression":"school","class":8,"score":81.0}', @LifecycleSchoolFacultyId, 3),
                (@LifecycleSchoolProfileId, 0, N'Class 9', N'{"phase":"lifecycle","progression":"school","class":9,"score":82.0}', @LifecycleSchoolFacultyId, 2),
                (@LifecycleSchoolProfileId, 0, N'Class 10', N'{"phase":"lifecycle","progression":"school","class":10,"score":83.0}', @LifecycleSchoolFacultyId, 1),
                (@LifecycleCollegeProfileId, 1, N'Class 11', N'{"phase":"lifecycle","progression":"college","class":11,"score":79.0}', @LifecycleCollegeFacultyId, 2),
                (@LifecycleCollegeProfileId, 1, N'Class 12', N'{"phase":"lifecycle","progression":"college","class":12,"score":81.0}', @LifecycleCollegeFacultyId, 1),
                (@LifecycleUniversityProfileId, 2, N'Semester 1', N'{"phase":"lifecycle","progression":"university","semester":1,"gpa":3.20}', @LifecycleUniversityFacultyId, 8),
                (@LifecycleUniversityProfileId, 2, N'Semester 2', N'{"phase":"lifecycle","progression":"university","semester":2,"gpa":3.30}', @LifecycleUniversityFacultyId, 7),
                (@LifecycleUniversityProfileId, 2, N'Semester 3', N'{"phase":"lifecycle","progression":"university","semester":3,"gpa":3.40}', @LifecycleUniversityFacultyId, 6),
                (@LifecycleUniversityProfileId, 2, N'Semester 4', N'{"phase":"lifecycle","progression":"university","semester":4,"gpa":3.50}', @LifecycleUniversityFacultyId, 5),
                (@LifecycleUniversityProfileId, 2, N'Semester 5', N'{"phase":"lifecycle","progression":"university","semester":5,"gpa":3.60}', @LifecycleUniversityFacultyId, 4),
                (@LifecycleUniversityProfileId, 2, N'Semester 6', N'{"phase":"lifecycle","progression":"university","semester":6,"gpa":3.70}', @LifecycleUniversityFacultyId, 3),
                (@LifecycleUniversityProfileId, 2, N'Semester 7', N'{"phase":"lifecycle","progression":"university","semester":7,"gpa":3.80}', @LifecycleUniversityFacultyId, 2),
                (@LifecycleUniversityProfileId, 2, N'Semester 8', N'{"phase":"lifecycle","progression":"university","semester":8,"gpa":3.88}', @LifecycleUniversityFacultyId, 1)
        ) v([StudentProfileId], [InstitutionType], [PeriodLabel], [PayloadJson], [GeneratedByUserId], [OrderNo])
        WHERE NOT EXISTS
        (
                SELECT 1
                FROM [student_report_cards] x
                WHERE x.[StudentProfileId] = v.[StudentProfileId]
                    AND x.[PeriodLabel] = v.[PeriodLabel]
        );
END

IF OBJECT_ID(N'[fyp_projects]') IS NOT NULL
BEGIN
        DECLARE @LifecycleUniversityFypId UNIQUEIDENTIFIER = (SELECT TOP (1) [Id] FROM [fyp_projects] WHERE [StudentProfileId] = @LifecycleUniversityProfileId ORDER BY [CreatedAt] DESC, [Id] DESC);

        IF @LifecycleUniversityFypId IS NULL
        BEGIN
                SET @LifecycleUniversityFypId = CAST('49490000-1111-2222-3333-000000000001' AS UNIQUEIDENTIFIER);

                IF EXISTS
                (
                        SELECT 1
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = N'fyp_projects'
                            AND COLUMN_NAME = N'Status'
                            AND DATA_TYPE IN (N'int', N'smallint', N'tinyint', N'bigint')
                )
                BEGIN
                        INSERT INTO [fyp_projects] ([Id], [StudentProfileId], [DepartmentId], [Title], [Description], [Status], [SupervisorUserId], [CoordinatorRemarks], [CreatedAt], [UpdatedAt])
                        SELECT @LifecycleUniversityFypId, @LifecycleUniversityProfileId, @LifecycleUniversityDepartmentId,
                                     N'Lifecycle Capstone: EduSphere End-to-End Progression',
                                     N'Final-year project used for deterministic semester 1-8 lifecycle and graduation verification.',
                                     4,
                                     @LifecycleUniversityFacultyId,
                                     N'Lifecycle validation project marked complete for graduation eligibility.',
                                     @Now,
                                     NULL
                        WHERE NOT EXISTS (SELECT 1 FROM [fyp_projects] x WHERE x.[Id] = @LifecycleUniversityFypId);
                END
                ELSE
                BEGIN
                        INSERT INTO [fyp_projects] ([Id], [StudentProfileId], [DepartmentId], [Title], [Description], [Status], [SupervisorUserId], [CoordinatorRemarks], [CreatedAt], [UpdatedAt])
                        SELECT @LifecycleUniversityFypId, @LifecycleUniversityProfileId, @LifecycleUniversityDepartmentId,
                                     N'Lifecycle Capstone: EduSphere End-to-End Progression',
                                     N'Final-year project used for deterministic semester 1-8 lifecycle and graduation verification.',
                                     N'Completed',
                                     @LifecycleUniversityFacultyId,
                                     N'Lifecycle validation project marked complete for graduation eligibility.',
                                     @Now,
                                     NULL
                        WHERE NOT EXISTS (SELECT 1 FROM [fyp_projects] x WHERE x.[Id] = @LifecycleUniversityFypId);
                END
        END

        IF OBJECT_ID(N'[fyp_meetings]') IS NOT NULL
        BEGIN
                INSERT INTO [fyp_meetings] ([Id], [FypProjectId], [ScheduledAt], [Venue], [Agenda], [Status], [OrganiserUserId], [Minutes], [CreatedAt], [UpdatedAt])
                SELECT CAST('50500000-1111-2222-3333-000000000001' AS UNIQUEIDENTIFIER), @LifecycleUniversityFypId,
                             DATEADD(day, -4, @Now), N'Innovation Lab 3', N'Final defense and completion sign-off.', N'Completed',
                             @LifecycleUniversityFacultyId, N'Lifecycle capstone accepted and approved for graduation workflow.', @Now, NULL
                WHERE NOT EXISTS (SELECT 1 FROM [fyp_meetings] x WHERE x.[FypProjectId] = @LifecycleUniversityFypId);
        END
END

IF OBJECT_ID(N'[graduation_applications]') IS NOT NULL
BEGIN
        DECLARE @LifecycleGraduationApplicationId UNIQUEIDENTIFIER = (SELECT TOP (1) [Id] FROM [graduation_applications] WHERE [StudentProfileId] = @LifecycleUniversityProfileId ORDER BY [CreatedAt] DESC, [Id] DESC);

        IF @LifecycleGraduationApplicationId IS NULL
        BEGIN
                SET @LifecycleGraduationApplicationId = CAST('31310000-1111-2222-3333-000000000001' AS UNIQUEIDENTIFIER);

                INSERT INTO [graduation_applications] ([Id], [StudentProfileId], [Status], [StudentNote], [SubmittedAt], [CertificatePath], [CertificateGeneratedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
                SELECT @LifecycleGraduationApplicationId,
                             @LifecycleUniversityProfileId,
                             2,
                             N'All semester 1-8 requirements, assessments, and FYP are completed.',
                             DATEADD(day, -10, @Now),
                             N'https://demo.local/certificates/LCYC-UNI-001.pdf',
                             DATEADD(day, -1, @Now),
                             @Now,
                             NULL,
                             0,
                             NULL
                WHERE NOT EXISTS (SELECT 1 FROM [graduation_applications] x WHERE x.[Id] = @LifecycleGraduationApplicationId);
        END

        IF OBJECT_ID(N'[graduation_application_approvals]') IS NOT NULL
        BEGIN
                INSERT INTO [graduation_application_approvals] ([Id], [GraduationApplicationId], [Stage], [ApproverUserId], [IsApproved], [Note], [ActedAt], [CreatedAt], [UpdatedAt])
                SELECT CAST('32320000-1111-2222-3333-000000000001' AS UNIQUEIDENTIFIER), @LifecycleGraduationApplicationId, 0,
                             CAST('66666666-6666-6666-6666-666666666611' AS UNIQUEIDENTIFIER), 1,
                             N'Department lifecycle verification approved.', DATEADD(day, -5, @Now), @Now, NULL
                WHERE NOT EXISTS
                (
                        SELECT 1
                        FROM [graduation_application_approvals] x
                        WHERE x.[GraduationApplicationId] = @LifecycleGraduationApplicationId
                            AND x.[Stage] = 0
                );

                INSERT INTO [graduation_application_approvals] ([Id], [GraduationApplicationId], [Stage], [ApproverUserId], [IsApproved], [Note], [ActedAt], [CreatedAt], [UpdatedAt])
                SELECT CAST('32320000-1111-2222-3333-000000000002' AS UNIQUEIDENTIFIER), @LifecycleGraduationApplicationId, 1,
                             @SuperAdminUserId, 1,
                             N'Registrar final approval granted.', DATEADD(day, -1, @Now), @Now, NULL
                WHERE NOT EXISTS
                (
                        SELECT 1
                        FROM [graduation_application_approvals] x
                        WHERE x.[GraduationApplicationId] = @LifecycleGraduationApplicationId
                            AND x.[Stage] = 1
                );
        END
END

/* 18) Additional semester saturation seed (class/semester logical coverage) */
IF OBJECT_ID(N'[enrollments]') IS NOT NULL
BEGIN
    ;WITH InstitutionStudents AS (
        SELECT sp.[Id] AS StudentProfileId,
               u.[InstitutionType],
               sp.[DepartmentId],
               ROW_NUMBER() OVER (PARTITION BY u.[InstitutionType], sp.[DepartmentId] ORDER BY sp.[RegistrationNumber], sp.[Id]) AS StudentRn
        FROM [student_profiles] sp
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        WHERE u.[IsDeleted] = 0
    ), RankedOfferings AS (
        SELECT co.[Id] AS CourseOfferingId,
               c.[DepartmentId],
               d.[InstitutionType],
               co.[SemesterId],
               ROW_NUMBER() OVER (
                   PARTITION BY d.[InstitutionType], c.[DepartmentId], co.[SemesterId]
                   ORDER BY c.[Code], co.[Id]
               ) AS OfferingRn
        FROM [course_offerings] co
        INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
        INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId]
        WHERE co.[IsDeleted] = 0
    )
    INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), s.[StudentProfileId], o.[CourseOfferingId], @Now, NULL, N'Enrolled', @Now, NULL
    FROM InstitutionStudents s
    INNER JOIN RankedOfferings o
        ON o.[InstitutionType] = s.[InstitutionType]
             AND o.[DepartmentId] = s.[DepartmentId]
             AND o.[OfferingRn] <= 4
        WHERE s.[StudentRn] <= 220
      AND NOT EXISTS (
          SELECT 1
          FROM [enrollments] e
          WHERE e.[StudentProfileId] = s.[StudentProfileId]
            AND e.[CourseOfferingId] = o.[CourseOfferingId]
      );
END

IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
BEGIN
    DECLARE @FinanceUniUserId2 UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [users] WHERE [Username] = N'finance.uni.1');
    DECLARE @FinanceColUserId2 UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [users] WHERE [Username] = N'finance.col.1');
    DECLARE @FinanceSchUserId2 UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [users] WHERE [Username] = N'finance.sch.1');

    ;WITH StudentScope AS (
        SELECT sp.[Id] AS StudentProfileId,
               u.[InstitutionType],
             sp.[DepartmentId],
             ROW_NUMBER() OVER (PARTITION BY u.[InstitutionType], sp.[DepartmentId] ORDER BY sp.[RegistrationNumber], sp.[Id]) AS StudentRn
        FROM [student_profiles] sp
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        WHERE u.[IsDeleted] = 0
    ), SemesterScope AS (
        SELECT s.[Id], s.[Name], s.[StartDate], ROW_NUMBER() OVER (ORDER BY s.[StartDate], s.[Id]) AS SemesterRn
        FROM [semesters] s
    )
    INSERT INTO [payment_receipts] ([Id], [StudentProfileId], [CreatedByUserId], [Status], [Amount], [Description], [DueDate], [ProofOfPaymentPath], [ProofUploadedAt], [ConfirmedByUserId], [ConfirmedAt], [Notes], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        NEWID(),
        st.[StudentProfileId],
        CASE st.[InstitutionType]
            WHEN 0 THEN COALESCE(@FinanceSchUserId2, @SuperAdminUserId)
            WHEN 1 THEN COALESCE(@FinanceColUserId2, @SuperAdminUserId)
            ELSE COALESCE(@FinanceUniUserId2, @SuperAdminUserId)
        END,
        CASE WHEN ((st.[StudentRn] + sm.[SemesterRn]) % 5) = 0 THEN 0 ELSE 1 END,
        CASE st.[InstitutionType]
            WHEN 0 THEN CAST(9800 + (sm.[SemesterRn] * 85) AS DECIMAL(10,2))
            WHEN 1 THEN CAST(12200 + (sm.[SemesterRn] * 120) AS DECIMAL(10,2))
            ELSE CAST(9300 + (sm.[SemesterRn] * 90) AS DECIMAL(10,2))
        END,
        CASE st.[InstitutionType]
            WHEN 0 THEN CONCAT(N'Class Fee - Class ', CAST(8 + ((sm.[SemesterRn] - 1) % 2) AS NVARCHAR(10)), N' - ', sm.[Name])
            ELSE CONCAT(N'Semester Fee - ', sm.[Name])
        END,
        DATEADD(day, 18, sm.[StartDate]),
        NULL,
        NULL,
        CASE WHEN ((st.[StudentRn] + sm.[SemesterRn]) % 5) = 0 THEN NULL ELSE
            CASE st.[InstitutionType]
                WHEN 2 THEN COALESCE(@FinanceUniUserId2, @SuperAdminUserId)
                WHEN 1 THEN COALESCE(@FinanceColUserId2, @SuperAdminUserId)
                ELSE COALESCE(@FinanceSchUserId2, @SuperAdminUserId)
            END
        END,
        CASE WHEN ((st.[StudentRn] + sm.[SemesterRn]) % 5) = 0 THEN NULL ELSE DATEADD(day, 2, sm.[StartDate]) END,
        CASE WHEN ((st.[StudentRn] + sm.[SemesterRn]) % 5) = 0 THEN N'Pending semester payment proof.' ELSE N'Confirmed semester-cycle seed payment.' END,
        @Now,
        @Now,
        0,
        NULL
    FROM StudentScope st
    CROSS JOIN SemesterScope sm
        WHERE st.[StudentRn] <= 220
            AND sm.[SemesterRn] <= 12
      AND NOT EXISTS (
          SELECT 1
          FROM [payment_receipts] pr
          WHERE pr.[StudentProfileId] = st.[StudentProfileId]
                        AND pr.[Description] = CASE st.[InstitutionType]
                                WHEN 0 THEN CONCAT(N'Class Fee - Class ', CAST(8 + ((sm.[SemesterRn] - 1) % 2) AS NVARCHAR(10)), N' - ', sm.[Name])
                                ELSE CONCAT(N'Semester Fee - ', sm.[Name])
                        END
      );
END

/* 18.1) Deterministic minimum lifecycle matrix coverage
   Guarantees:
   - At least one Faculty and one Finance user per department.
   - At least one StudentProfile per program.
   - At least one enrollment for every seeded course offering (course + semester coverage).
*/
IF OBJECT_ID(N'[users]') IS NOT NULL
BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        NEWID(),
        CONCAT(N'matrix.faculty.', LOWER(d.[Code])),
        CONCAT(N'matrix.faculty.', LOWER(d.[Code]), N'@demo.local'),
        @PwdHash,
        @RoleFaculty,
        d.[Id],
        d.[InstitutionType],
        1,
        NULL,
        @Now,
        NULL,
        0,
        NULL
    FROM @Departments d
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [users] u
        WHERE u.[DepartmentId] = d.[Id]
          AND u.[RoleId] = @RoleFaculty
          AND u.[IsDeleted] = 0
    );

    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        NEWID(),
        CONCAT(N'matrix.finance.', LOWER(d.[Code])),
        CONCAT(N'matrix.finance.', LOWER(d.[Code]), N'@demo.local'),
        @PwdHash,
        @RoleFinance,
        d.[Id],
        d.[InstitutionType],
        1,
        NULL,
        @Now,
        NULL,
        0,
        NULL
    FROM @Departments d
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [users] u
        WHERE u.[DepartmentId] = d.[Id]
          AND u.[RoleId] = @RoleFinance
          AND u.[IsDeleted] = 0
    );

    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        NEWID(),
        CONCAT(N'matrix.student.', LOWER(p.[Code])),
        CONCAT(N'matrix.student.', LOWER(p.[Code]), N'@demo.local'),
        @PwdHash,
        @RoleStudent,
        p.[DepartmentId],
        d.[InstitutionType],
        1,
        NULL,
        @Now,
        NULL,
        0,
        NULL
    FROM @Programs p
    INNER JOIN @Departments d ON d.[Id] = p.[DepartmentId]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [student_profiles] sp
        WHERE sp.[ProgramId] = p.[Id]
          AND sp.[IsDeleted] = 0
    )
      AND NOT EXISTS
    (
        SELECT 1
        FROM [users] u
        WHERE u.[Username] = CONCAT(N'matrix.student.', LOWER(p.[Code]))
    );
END

IF OBJECT_ID(N'[student_profiles]') IS NOT NULL
BEGIN
    INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT
        NEWID(),
        u.[Id],
        CONCAT(N'MX-', UPPER(p.[Code]), N'-', RIGHT(REPLACE(CONVERT(NVARCHAR(36), u.[Id]), N'-', N''), 8)),
        p.[Id],
        p.[DepartmentId],
        DATEADD(day, -180, @Now),
        CAST(2.80 AS DECIMAL(4,2)),
        1,
        @Now,
        NULL,
        0,
        NULL
    FROM @Programs p
    INNER JOIN [users] u
        ON u.[DepartmentId] = p.[DepartmentId]
       AND u.[RoleId] = @RoleStudent
       AND u.[Username] = CONCAT(N'matrix.student.', LOWER(p.[Code]))
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [student_profiles] sp
        WHERE sp.[ProgramId] = p.[Id]
          AND sp.[IsDeleted] = 0
    );
END

IF OBJECT_ID(N'[enrollments]') IS NOT NULL
BEGIN
    ;WITH OfferingDepartment AS
    (
        SELECT
            co.[Id] AS CourseOfferingId,
            c.[DepartmentId]
        FROM [course_offerings] co
        INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
    ), DepartmentStudents AS
    (
        SELECT
            sp.[Id] AS StudentProfileId,
            sp.[DepartmentId],
            ROW_NUMBER() OVER (PARTITION BY sp.[DepartmentId] ORDER BY sp.[RegistrationNumber], sp.[Id]) AS StudentOrdinal
        FROM [student_profiles] sp
        WHERE sp.[IsDeleted] = 0
    )
    INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
    SELECT
        NEWID(),
        ds.[StudentProfileId],
        od.[CourseOfferingId],
        @Now,
        NULL,
        N'Enrolled',
        @Now,
        NULL
    FROM OfferingDepartment od
    INNER JOIN DepartmentStudents ds
        ON ds.[DepartmentId] = od.[DepartmentId]
       AND ds.[StudentOrdinal] = 1
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [enrollments] e
        WHERE e.[CourseOfferingId] = od.[CourseOfferingId]
    );
END

/* 24) Explicit demo users/profiles for each institute (School/College/University) */
DECLARE @InstituteDemoUsers TABLE
(
    [Id] UNIQUEIDENTIFIER,
    [Username] NVARCHAR(100),
    [Email] NVARCHAR(256),
    [RoleId] INT,
    [DepartmentId] UNIQUEIDENTIFIER,
    [InstitutionType] INT
);

INSERT INTO @InstituteDemoUsers ([Id], [Username], [Email], [RoleId], [DepartmentId], [InstitutionType]) VALUES
(CAST('6a100000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'demo.uni.admin', N'demo.uni.admin@demo.local', @RoleAdmin, '11111111-1111-1111-1111-111111111111', 2),
(CAST('7a100000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'demo.uni.faculty', N'demo.uni.faculty@demo.local', @RoleFaculty, '11111111-1111-1111-1111-111111111111', 2),
(CAST('8a100000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'demo.uni.student', N'demo.uni.student@demo.local', @RoleStudent, '11111111-1111-1111-1111-111111111111', 2),
(CAST('6a100000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'demo.col.admin', N'demo.col.admin@demo.local', @RoleAdmin, '12222222-2222-2222-2222-222222222221', 1),
(CAST('7a100000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'demo.col.faculty', N'demo.col.faculty@demo.local', @RoleFaculty, '12222222-2222-2222-2222-222222222221', 1),
(CAST('8a100000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'demo.col.student', N'demo.col.student@demo.local', @RoleStudent, '12222222-2222-2222-2222-222222222221', 1),
(CAST('6a100000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'demo.sch.admin', N'demo.sch.admin@demo.local', @RoleAdmin, '13333333-3333-3333-3333-333333333331', 0),
(CAST('7a100000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'demo.sch.faculty', N'demo.sch.faculty@demo.local', @RoleFaculty, '13333333-3333-3333-3333-333333333331', 0),
(CAST('8a100000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'demo.sch.student', N'demo.sch.student@demo.local', @RoleStudent, '13333333-3333-3333-3333-333333333331', 0);

INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
SELECT src.[Id], src.[Username], src.[Email], @PwdHash, src.[RoleId], src.[DepartmentId], src.[InstitutionType], 1, NULL, @Now, NULL, 0, NULL
FROM @InstituteDemoUsers src
WHERE NOT EXISTS
(
    SELECT 1
    FROM [users] u
    WHERE u.[Id] = src.[Id]
       OR u.[Username] = src.[Username]
       OR u.[Email] = src.[Email]
);

UPDATE u
SET u.[Username] = src.[Username],
    u.[Email] = src.[Email],
    u.[PasswordHash] = @PwdHash,
    u.[RoleId] = src.[RoleId],
    u.[DepartmentId] = src.[DepartmentId],
    u.[InstitutionType] = src.[InstitutionType],
    u.[IsActive] = 1,
    u.[IsDeleted] = 0,
    u.[DeletedAt] = NULL,
    u.[UpdatedAt] = @Now
FROM [users] u
INNER JOIN @InstituteDemoUsers src ON src.[Id] = u.[Id];

IF OBJECT_ID(N'[student_profiles]') IS NOT NULL
BEGIN
    DECLARE @InstituteDemoProfiles TABLE
    (
        [Id] UNIQUEIDENTIFIER,
        [UserId] UNIQUEIDENTIFIER,
        [RegistrationNumber] NVARCHAR(50),
        [ProgramId] UNIQUEIDENTIFIER,
        [DepartmentId] UNIQUEIDENTIFIER,
        [CurrentSemesterNumber] INT,
        [Cgpa] DECIMAL(4,2)
    );

    INSERT INTO @InstituteDemoProfiles ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [CurrentSemesterNumber], [Cgpa]) VALUES
    (CAST('9a100000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), CAST('8a100000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'DEMO-UNI-0001', '22222222-2222-2222-2222-222222222211', '11111111-1111-1111-1111-111111111111', 2, CAST(3.45 AS DECIMAL(4,2))),
    (CAST('9a100000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), CAST('8a100000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'DEMO-COL-0001', '22222222-2222-2222-2222-222222222321', '12222222-2222-2222-2222-222222222221', 1, CAST(3.20 AS DECIMAL(4,2))),
    (CAST('9a100000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), CAST('8a100000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'DEMO-SCH-0001', '22222222-2222-2222-2222-222222222431', '13333333-3333-3333-3333-333333333331', 6, CAST(3.10 AS DECIMAL(4,2)));

    INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT src.[Id], src.[UserId], src.[RegistrationNumber], src.[ProgramId], src.[DepartmentId], CAST('2026-01-15' AS DATETIME2), src.[Cgpa], src.[CurrentSemesterNumber], @Now, NULL, 0, NULL
    FROM @InstituteDemoProfiles src
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [student_profiles] sp
        WHERE sp.[Id] = src.[Id]
           OR sp.[UserId] = src.[UserId]
           OR sp.[RegistrationNumber] = src.[RegistrationNumber]
    );

    UPDATE sp
    SET sp.[ProgramId] = src.[ProgramId],
        sp.[DepartmentId] = src.[DepartmentId],
        sp.[CurrentSemesterNumber] = src.[CurrentSemesterNumber],
        sp.[Cgpa] = src.[Cgpa],
        sp.[UpdatedAt] = @Now,
        sp.[IsDeleted] = 0,
        sp.[DeletedAt] = NULL
    FROM [student_profiles] sp
    INNER JOIN @InstituteDemoProfiles src ON src.[Id] = sp.[Id];
END

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
    WHERE u.[RoleId] <> @RoleSuperAdmin
      AND (
            u.[TenantId] IS NULL
         OR u.[CampusId] IS NULL
         OR u.[TenantId] <> CASE d.[InstitutionType] WHEN 2 THEN @UniTenantId WHEN 1 THEN @ColTenantId ELSE @SchTenantId END
         OR u.[CampusId] <> CASE d.[InstitutionType] WHEN 2 THEN @UniCampusId WHEN 1 THEN @ColCampusId ELSE @SchCampusId END
      );

    UPDATE [users]
    SET [TenantId] = NULL,
        [CampusId] = NULL,
        [UpdatedAt] = @Now
    WHERE [RoleId] = @RoleSuperAdmin
      AND ([TenantId] IS NOT NULL OR [CampusId] IS NOT NULL);
END

COMMIT TRANSACTION;

PRINT 'Full dummy demo data seeding completed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
