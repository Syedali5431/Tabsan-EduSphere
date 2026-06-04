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
  Full dummy demo data for Tabsan EduSphere.
  Covers School (Science Class 1-10), College (ICS Class 11-12),
  University (BSCS, MCS, BBA, Arabic Language) with complete audit/ISO compliance data.

  PREREQUISITES: 01-Schema-Current.sql then 02-Seed-Core.sql
  Idempotent — safe to re-run. Uses stable GUIDs for all core entities.
  All 4 copies (Scripts/, School Scripts/, College Scripts/, University Scripts/) are identical.

  InstitutionType convention (DATABASE): School = 0, College = 1, University = 2
*/

BEGIN TRY
BEGIN TRANSACTION;

-- ==============================================================================
-- VARIABLES
-- ==============================================================================

DECLARE @Now        DATETIME2       = SYSUTCDATETIME();
DECLARE @PwdHash    NVARCHAR(512)   = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';

-- Tenant GUIDs
DECLARE @UniTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @ColTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchTenantId UNIQUEIDENTIFIER = CAST('f1000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);

-- Campus GUIDs
DECLARE @UniCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @ColCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchCampusId UNIQUEIDENTIFIER = CAST('f2000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);

-- Department GUIDs (stable)
DECLARE @UniCSDeptId    UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER); -- IT/CS
DECLARE @UniBUSDeptId   UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111112' AS UNIQUEIDENTIFIER); -- Business
DECLARE @UniLANGDeptId  UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111113' AS UNIQUEIDENTIFIER); -- Languages
DECLARE @ColSCIDeptId   UNIQUEIDENTIFIER = CAST('12222222-2222-2222-2222-222222222221' AS UNIQUEIDENTIFIER); -- Science College
DECLARE @SchSCIDeptId   UNIQUEIDENTIFIER = CAST('13333333-3333-3333-3333-333333333331' AS UNIQUEIDENTIFIER); -- School Science

-- Role IDs (from 02-Seed-Core.sql)
DECLARE @RoleSuperAdmin INT = 1;
DECLARE @RoleAdmin      INT = 2;
DECLARE @RoleFaculty    INT = 3;
DECLARE @RoleStudent    INT = 4;
DECLARE @RoleFinance    INT = 5;
DECLARE @RoleParent     INT = 6;

PRINT 'Variables initialized at ' + CONVERT(NVARCHAR, @Now, 121);

-- ==============================================================================
-- SECTION 0: DEMO METADATA
-- ==============================================================================

IF OBJECT_ID(N'[Tabsan-EduSphere]') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Tabsan-EduSphere] WHERE [DemoKey] = N'DemoDataVersion')
        INSERT INTO [Tabsan-EduSphere] ([Id], [DemoKey], [DemoValue], [CreatedAt]) VALUES (NEWID(), N'DemoDataVersion', N'3.0.0', @Now);
    ELSE
        UPDATE [Tabsan-EduSphere] SET [DemoValue] = N'3.0.0' WHERE [DemoKey] = N'DemoDataVersion';

    IF NOT EXISTS (SELECT 1 FROM [Tabsan-EduSphere] WHERE [DemoKey] = N'DemoDataCreatedAt')
        INSERT INTO [Tabsan-EduSphere] ([Id], [DemoKey], [DemoValue], [CreatedAt]) VALUES (NEWID(), N'DemoDataCreatedAt', CONVERT(NVARCHAR, @Now, 121), @Now);
END

-- ==============================================================================
-- SECTION 1: TENANTS
-- ==============================================================================

IF OBJECT_ID(N'[tenants]') IS NOT NULL
BEGIN
    DECLARE @Tenants TABLE (Id UNIQUEIDENTIFIER, Code NVARCHAR(64), Name NVARCHAR(200), IsActive BIT);
    INSERT INTO @Tenants VALUES
    (@UniTenantId, N'UNI', N'University Tenant', 1),
    (@ColTenantId, N'COL', N'College Tenant',   1),
    (@SchTenantId, N'SCH', N'School Tenant',    1);

    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT t.Id, t.Code, t.Name, t.IsActive, @Now, NULL
    FROM @Tenants t
    WHERE NOT EXISTS (SELECT 1 FROM [tenants] x WHERE x.[Id] = t.Id);

    UPDATE tgt SET tgt.[Name] = src.[Name], tgt.[IsActive] = src.[IsActive], tgt.[UpdatedAt] = @Now
    FROM [tenants] tgt INNER JOIN @Tenants src ON src.[Id] = tgt.[Id]
    WHERE tgt.[Name] <> src.[Name] OR tgt.[IsActive] <> src.[IsActive];
END

-- ==============================================================================
-- SECTION 2: CAMPUSES
-- ==============================================================================

IF OBJECT_ID(N'[campuses]') IS NOT NULL
BEGIN
    DECLARE @Campuses TABLE (Id UNIQUEIDENTIFIER, TenantId UNIQUEIDENTIFIER, Code NVARCHAR(64), Name NVARCHAR(200), IsActive BIT);
    INSERT INTO @Campuses VALUES
    (@UniCampusId, @UniTenantId, N'UNI-MAIN', N'University Main Campus', 1),
    (@ColCampusId, @ColTenantId, N'COL-MAIN', N'College Main Campus',   1),
    (@SchCampusId, @SchTenantId, N'SCH-MAIN', N'School Main Campus',    1);

    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT c.Id, c.TenantId, c.Code, c.Name, c.IsActive, @Now, NULL
    FROM @Campuses c
    WHERE NOT EXISTS (SELECT 1 FROM [campuses] x WHERE x.[Id] = c.Id);

    UPDATE tgt SET tgt.[Name] = src.[Name], tgt.[IsActive] = src.[IsActive], tgt.[UpdatedAt] = @Now
    FROM [campuses] tgt INNER JOIN @Campuses src ON src.[Id] = tgt.[Id]
    WHERE tgt.[Name] <> src.[Name] OR tgt.[IsActive] <> src.[IsActive];
END

-- ==============================================================================
-- SECTION 3: DEPARTMENTS (5 total)
-- ==============================================================================

IF OBJECT_ID(N'[departments]') IS NOT NULL
BEGIN
    DECLARE @Departments TABLE (
        Id UNIQUEIDENTIFIER, Name NVARCHAR(200), Code NVARCHAR(20),
        InstitutionType INT, TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER, IsActive BIT);

    INSERT INTO @Departments VALUES
    -- University departments (InstitutionType = 2)
    (@UniCSDeptId,   N'School of Computer Science',  N'CS',   2, @UniTenantId, @UniCampusId, 1),
    (@UniBUSDeptId,  N'School of Business Administration', N'BUS', 2, @UniTenantId, @UniCampusId, 1),
    (@UniLANGDeptId, N'School of Languages',          N'LANG', 2, @UniTenantId, @UniCampusId, 1),
    -- College department (InstitutionType = 1)
    (@ColSCIDeptId,  N'Science College',              N'COL-SCI', 1, @ColTenantId, @ColCampusId, 1),
    -- School department (InstitutionType = 0)
    (@SchSCIDeptId,  N'School Science Department',    N'SCH-SCI', 0, @SchTenantId, @SchCampusId, 1);

    INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [TenantId], [CampusId], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT d.Id, d.Name, d.Code, d.InstitutionType, d.TenantId, d.CampusId, d.IsActive, @Now, NULL
    FROM @Departments d
    WHERE NOT EXISTS (SELECT 1 FROM [departments] x WHERE x.[Id] = d.Id);

    UPDATE tgt SET tgt.[Name] = src.[Name], tgt.[Code] = src.[Code],
        tgt.[InstitutionType] = src.[InstitutionType],
        tgt.[TenantId] = src.[TenantId], tgt.[CampusId] = src.[CampusId],
        tgt.[IsActive] = src.[IsActive], tgt.[UpdatedAt] = @Now
    FROM [departments] tgt INNER JOIN @Departments src ON src.[Id] = tgt.[Id]
    WHERE tgt.[Name] <> src.[Name] OR tgt.[Code] <> src.[Code]
       OR tgt.[InstitutionType] <> src.[InstitutionType]
       OR ISNULL(tgt.[TenantId], CAST('00000000-0000-0000-0000-000000000000' AS UNIQUEIDENTIFIER)) <> ISNULL(src.[TenantId], CAST('00000000-0000-0000-0000-000000000000' AS UNIQUEIDENTIFIER));
END

PRINT 'Departments seeded.';

-- ==============================================================================
-- SECTION 4: ACADEMIC PROGRAMS (6 total)
-- ==============================================================================

IF OBJECT_ID(N'[academic_programs]') IS NOT NULL
BEGIN
    DECLARE @Programs TABLE (
        Id UNIQUEIDENTIFIER, Name NVARCHAR(200), Code NVARCHAR(20),
        DepartmentId UNIQUEIDENTIFIER, TotalSemesters INT, MaxCreditLoadPerSemester INT, IsActive BIT);

    INSERT INTO @Programs VALUES
    -- University
    (CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), N'BS Computer Science',         N'BSCS',    @UniCSDeptId,   8, 18, 1),
    (CAST('22222222-2222-2222-2222-222222222212' AS UNIQUEIDENTIFIER), N'MCS',                         N'MCS',     @UniCSDeptId,   4, 15, 1),
    (CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), N'Bachelor of Business Admin',  N'BBA',     @UniBUSDeptId,  8, 18, 1),
    (CAST('22222222-2222-2222-2222-222222222214' AS UNIQUEIDENTIFIER), N'Arabic Language Studies',     N'LANG-AR', @UniLANGDeptId, 4, 15, 1),
    -- College
    (CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), N'ICS',                         N'ICS',     @ColSCIDeptId,  2, 12, 1),
    -- School
    (CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), N'School Science Class 1-10',   N'SCH-SCI-C1TO10', @SchSCIDeptId, 10, 8, 1);

    INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [MaxCreditLoadPerSemester], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT p.Id, p.Name, p.Code, p.DepartmentId, p.TotalSemesters, p.MaxCreditLoadPerSemester, p.IsActive, @Now, NULL
    FROM @Programs p
    WHERE EXISTS (SELECT 1 FROM [departments] d WHERE d.[Id] = p.[DepartmentId])
      AND NOT EXISTS (SELECT 1 FROM [academic_programs] x WHERE x.[Id] = p.Id);

    -- Sync
    UPDATE tgt SET tgt.[Name] = src.[Name], tgt.[TotalSemesters] = src.[TotalSemesters],
        tgt.[MaxCreditLoadPerSemester] = src.[MaxCreditLoadPerSemester],
        tgt.[IsActive] = src.[IsActive], tgt.[UpdatedAt] = @Now
    FROM [academic_programs] tgt INNER JOIN @Programs src ON src.[Id] = tgt.[Id]
    WHERE tgt.[Name] <> src.[Name] OR tgt.[TotalSemesters] <> src.[TotalSemesters]
       OR tgt.[IsActive] <> src.[IsActive];
END

PRINT 'Academic programs seeded.';

-- ==============================================================================
-- SECTION 5: SEMESTERS (10 total)
-- ==============================================================================

IF OBJECT_ID(N'[semesters]') IS NOT NULL
BEGIN
    DECLARE @Semesters TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(100), StartDate DATETIME2, EndDate DATETIME2, IsClosed BIT);

    INSERT INTO @Semesters VALUES
    -- School & College: Academic Year 2026-2027 (Class 1 / Class 11) through 2027-2028 (Class 2 / Class 12)
    -- University: Semester 1-8 mapped to real semesters
    (CAST('33333333-3333-3333-3333-333333333301' AS UNIQUEIDENTIFIER), N'Fall 2026',   '2026-09-01', '2027-01-15', 0),
    (CAST('33333333-3333-3333-3333-333333333302' AS UNIQUEIDENTIFIER), N'Spring 2027', '2027-02-01', '2027-06-15', 0),
    (CAST('33333333-3333-3333-3333-333333333303' AS UNIQUEIDENTIFIER), N'Fall 2027',   '2027-09-01', '2028-01-15', 0),
    (CAST('33333333-3333-3333-3333-333333333304' AS UNIQUEIDENTIFIER), N'Spring 2028', '2028-02-01', '2028-06-15', 0),
    (CAST('33333333-3333-3333-3333-333333333305' AS UNIQUEIDENTIFIER), N'Fall 2028',   '2028-09-01', '2029-01-15', 0),
    (CAST('33333333-3333-3333-3333-333333333306' AS UNIQUEIDENTIFIER), N'Spring 2029', '2029-02-01', '2029-06-15', 0),
    (CAST('33333333-3333-3333-3333-333333333307' AS UNIQUEIDENTIFIER), N'Fall 2029',   '2029-09-01', '2030-01-15', 0),
    (CAST('33333333-3333-3333-3333-333333333308' AS UNIQUEIDENTIFIER), N'Spring 2030', '2030-02-01', '2030-06-15', 0),
    (CAST('33333333-3333-3333-3333-333333333309' AS UNIQUEIDENTIFIER), N'Past Fall 2025', '2025-09-01', '2026-01-15', 1),
    (CAST('33333333-3333-3333-3333-333333333310' AS UNIQUEIDENTIFIER), N'Past Spring 2026','2026-02-01', '2026-06-15', 1);

    INSERT INTO [semesters] ([Id], [Name], [StartDate], [EndDate], [IsClosed], [ClosedAt], [CreatedAt], [UpdatedAt])
    SELECT s.Id, s.Name, s.StartDate, s.EndDate, s.IsClosed, CASE WHEN s.IsClosed = 1 THEN @Now ELSE NULL END, @Now, NULL
    FROM @Semesters s
    WHERE NOT EXISTS (SELECT 1 FROM [semesters] x WHERE x.[Id] = s.Id);

    UPDATE tgt SET tgt.[Name] = src.[Name], tgt.[StartDate] = src.[StartDate],
        tgt.[EndDate] = src.[EndDate], tgt.[IsClosed] = src.[IsClosed], tgt.[UpdatedAt] = @Now
    FROM [semesters] tgt INNER JOIN @Semesters src ON src.[Id] = tgt.[Id]
    WHERE tgt.[Name] <> src.[Name] OR tgt.[IsClosed] <> src.[IsClosed];
END

PRINT 'Semesters seeded.';

-- ==============================================================================
-- SECTION 6: COURSES
-- ==============================================================================

IF OBJECT_ID(N'[courses]') IS NOT NULL
BEGIN
    DECLARE @Courses TABLE (
        Id UNIQUEIDENTIFIER, Title NVARCHAR(200), Code NVARCHAR(20), CreditHours INT,
        DepartmentId UNIQUEIDENTIFIER, CourseType INT, GradingType NVARCHAR(20),
        HasSemesters BIT, TotalSemesters INT, IsActive BIT);

    INSERT INTO @Courses VALUES
    -- ===== SCHOOL: Science subjects (Classes 1-10) =====
    -- GradingType = 'Percentage', TotalSemesters = 10
    (CAST('44444444-4444-4444-4444-444444444351' AS UNIQUEIDENTIFIER), N'Physics',          N'SCI-PHYS', 2, @SchSCIDeptId,  1, N'Percentage', 1, 10, 1),
    (CAST('44444444-4444-4444-4444-444444444352' AS UNIQUEIDENTIFIER), N'Chemistry',        N'SCI-CHEM', 2, @SchSCIDeptId,  1, N'Percentage', 1, 10, 1),
    (CAST('44444444-4444-4444-4444-444444444353' AS UNIQUEIDENTIFIER), N'Mathematics',      N'SCI-MATH', 2, @SchSCIDeptId,  1, N'Percentage', 1, 10, 1),
    (CAST('44444444-4444-4444-4444-444444444354' AS UNIQUEIDENTIFIER), N'Computer Science', N'SCI-COMP', 2, @SchSCIDeptId,  1, N'Percentage', 1, 10, 1),
    (CAST('44444444-4444-4444-4444-444444444355' AS UNIQUEIDENTIFIER), N'English',          N'SCI-ENG',  1, @SchSCIDeptId,  1, N'Percentage', 1, 10, 1),
    (CAST('44444444-4444-4444-4444-444444444356' AS UNIQUEIDENTIFIER), N'Urdu',             N'SCI-URDU', 1, @SchSCIDeptId,  1, N'Percentage', 1, 10, 1),

    -- ===== COLLEGE: ICS subjects (Classes 11-12) =====
    -- GradingType = 'Percentage', TotalSemesters = 2
    (CAST('44444444-4444-4444-4444-444444444231' AS UNIQUEIDENTIFIER), N'Physics',          N'COL-PHYS', 3, @ColSCIDeptId,  1, N'Percentage', 1, 2, 1),
    (CAST('44444444-4444-4444-4444-444444444232' AS UNIQUEIDENTIFIER), N'Mathematics',      N'COL-MATH', 3, @ColSCIDeptId,  1, N'Percentage', 1, 2, 1),
    (CAST('44444444-4444-4444-4444-444444444233' AS UNIQUEIDENTIFIER), N'Computer Science', N'COL-COMP', 3, @ColSCIDeptId,  1, N'Percentage', 1, 2, 1),
    (CAST('44444444-4444-4444-4444-444444444234' AS UNIQUEIDENTIFIER), N'English',          N'COL-ENG',  1, @ColSCIDeptId,  1, N'Percentage', 1, 2, 1),

    -- ===== UNIVERSITY: IT Department (BSCS + MCS) — GPA =====
    (CAST('44444444-4444-4444-4444-444444444101' AS UNIQUEIDENTIFIER), N'Programming Fundamentals', N'CS-101', 3, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444102' AS UNIQUEIDENTIFIER), N'Data Structures',          N'CS-201', 3, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444103' AS UNIQUEIDENTIFIER), N'Algorithms',               N'CS-202', 3, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444104' AS UNIQUEIDENTIFIER), N'Database Systems',         N'CS-301', 4, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444105' AS UNIQUEIDENTIFIER), N'Web Development',          N'CS-302', 3, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444106' AS UNIQUEIDENTIFIER), N'Software Engineering',     N'CS-401', 3, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444107' AS UNIQUEIDENTIFIER), N'Artificial Intelligence',  N'CS-402', 3, @UniCSDeptId,  2, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444108' AS UNIQUEIDENTIFIER), N'Capstone Project',         N'CS-499', 4, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    -- MCS advanced courses
    (CAST('44444444-4444-4444-4444-444444444109' AS UNIQUEIDENTIFIER), N'Advanced Databases',       N'CS-501', 3, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444110' AS UNIQUEIDENTIFIER), N'Machine Learning',         N'CS-502', 3, @UniCSDeptId,  2, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444111' AS UNIQUEIDENTIFIER), N'Thesis',                   N'CS-599', 6, @UniCSDeptId,  1, N'GPA', 1, NULL, 1),

    -- ===== UNIVERSITY: Business Department (BBA) — GPA =====
    (CAST('44444444-4444-4444-4444-444444444201' AS UNIQUEIDENTIFIER), N'Principles of Management',   N'BUS-101', 3, @UniBUSDeptId, 1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444202' AS UNIQUEIDENTIFIER), N'Marketing Fundamentals',     N'BUS-201', 3, @UniBUSDeptId, 1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444203' AS UNIQUEIDENTIFIER), N'Financial Accounting',       N'BUS-301', 4, @UniBUSDeptId, 1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444204' AS UNIQUEIDENTIFIER), N'Managerial Accounting',      N'BUS-302', 3, @UniBUSDeptId, 1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444205' AS UNIQUEIDENTIFIER), N'Economics',                  N'BUS-103', 3, @UniBUSDeptId, 1, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444206' AS UNIQUEIDENTIFIER), N'Human Resource Management',  N'BUS-401', 3, @UniBUSDeptId, 2, N'GPA', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444207' AS UNIQUEIDENTIFIER), N'Business Strategy',          N'BUS-402', 3, @UniBUSDeptId, 1, N'GPA', 1, NULL, 1),

    -- ===== UNIVERSITY: Languages Department (Arabic) — Percentage =====
    (CAST('44444444-4444-4444-4444-444444444301' AS UNIQUEIDENTIFIER), N'Arabic Language I',          N'LANG-101', 3, @UniLANGDeptId, 1, N'Percentage', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444302' AS UNIQUEIDENTIFIER), N'Arabic Literature',           N'LANG-102', 3, @UniLANGDeptId, 1, N'Percentage', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444303' AS UNIQUEIDENTIFIER), N'Arabic Grammar & Morphology', N'LANG-201', 3, @UniLANGDeptId, 1, N'Percentage', 1, NULL, 1),
    (CAST('44444444-4444-4444-4444-444444444304' AS UNIQUEIDENTIFIER), N'Arabic Conversation',         N'LANG-202', 2, @UniLANGDeptId, 2, N'Percentage', 1, NULL, 1);

    INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [CourseType], [GradingType], [HasSemesters], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT c.Id, c.Title, c.Code, c.CreditHours, c.DepartmentId, c.CourseType, c.GradingType, c.HasSemesters, c.TotalSemesters, c.IsActive, @Now, NULL
    FROM @Courses c
    WHERE EXISTS (SELECT 1 FROM [departments] d WHERE d.[Id] = c.[DepartmentId])
      AND NOT EXISTS (SELECT 1 FROM [courses] x WHERE x.[Id] = c.Id);

    -- Sync
    UPDATE tgt SET tgt.[Title] = src.[Title], tgt.[CreditHours] = src.[CreditHours],
        tgt.[GradingType] = src.[GradingType], tgt.[CourseType] = src.[CourseType],
        tgt.[HasSemesters] = src.[HasSemesters], tgt.[TotalSemesters] = src.[TotalSemesters],
        tgt.[IsActive] = src.[IsActive], tgt.[UpdatedAt] = @Now
    FROM [courses] tgt INNER JOIN @Courses src ON src.[Id] = tgt.[Id]
    WHERE tgt.[Title] <> src.[Title] OR tgt.[CreditHours] <> src.[CreditHours]
       OR ISNULL(tgt.[GradingType], N'') <> src.[GradingType];
END

PRINT 'Courses seeded (' + CAST(@@ROWCOUNT AS NVARCHAR) + ' courses).';

-- ==============================================================================
-- SECTION 7: USERS
-- ==============================================================================

IF OBJECT_ID(N'[users]') IS NOT NULL
BEGIN
    DECLARE @Users TABLE (
        Id UNIQUEIDENTIFIER, Username NVARCHAR(100), Email NVARCHAR(256),
        PasswordHash NVARCHAR(512), RoleId INT, DepartmentId UNIQUEIDENTIFIER,
        TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER, InstitutionType INT,
        IsActive BIT, FullName NVARCHAR(200));

    INSERT INTO @Users VALUES
    -- ===== SuperAdmin (cross-institution) =====
    (CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), N'superadmin', N'superadmin@demo.local',
     @PwdHash, @RoleSuperAdmin, NULL, NULL, NULL, NULL, 1, N'System Super Administrator'),

    -- ===== Finance (1 per institution) =====
    (CAST('66666666-6666-6666-6666-666666666610' AS UNIQUEIDENTIFIER), N'finance.uni', N'finance.uni@demo.local',
     @PwdHash, @RoleFinance, NULL, @UniTenantId, @UniCampusId, 2, 1, N'University Finance Officer'),
    (CAST('66666666-6666-6666-6666-666666666611' AS UNIQUEIDENTIFIER), N'finance.col', N'finance.col@demo.local',
     @PwdHash, @RoleFinance, NULL, @ColTenantId, @ColCampusId, 1, 1, N'College Finance Officer'),
    (CAST('66666666-6666-6666-6666-666666666612' AS UNIQUEIDENTIFIER), N'finance.sch', N'finance.sch@demo.local',
     @PwdHash, @RoleFinance, NULL, @SchTenantId, @SchCampusId, 0, 1, N'School Finance Officer'),

    -- ===== Admins (1 per department) =====
    (CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), N'admin.cs',   N'admin.cs@demo.local',   @PwdHash, @RoleAdmin, @UniCSDeptId,   @UniTenantId, @UniCampusId, 2, 1, N'CS Department Admin'),
    (CAST('66666666-6666-6666-6666-666666666622' AS UNIQUEIDENTIFIER), N'admin.bus',  N'admin.bus@demo.local',  @PwdHash, @RoleAdmin, @UniBUSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Business Department Admin'),
    (CAST('66666666-6666-6666-6666-666666666623' AS UNIQUEIDENTIFIER), N'admin.lang', N'admin.lang@demo.local', @PwdHash, @RoleAdmin, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Languages Department Admin'),
    (CAST('66666666-6666-6666-6666-666666666624' AS UNIQUEIDENTIFIER), N'admin.col',  N'admin.col@demo.local',  @PwdHash, @RoleAdmin, @ColSCIDeptId,  @ColTenantId, @ColCampusId, 1, 1, N'College Admin'),
    (CAST('66666666-6666-6666-6666-666666666625' AS UNIQUEIDENTIFIER), N'admin.sch',  N'admin.sch@demo.local',  @PwdHash, @RoleAdmin, @SchSCIDeptId,  @SchTenantId, @SchCampusId, 0, 1, N'School Admin'),

    -- ===== University IT Faculty =====
    (CAST('77777777-7777-7777-7777-777777777101' AS UNIQUEIDENTIFIER), N'faculty.cs.1', N'faculty.cs.1@demo.local', @PwdHash, @RoleFaculty, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Dr. Ahmad Khan'),
    (CAST('77777777-7777-7777-7777-777777777102' AS UNIQUEIDENTIFIER), N'faculty.cs.2', N'faculty.cs.2@demo.local', @PwdHash, @RoleFaculty, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Dr. Bilal Siddiqui'),
    (CAST('77777777-7777-7777-7777-777777777103' AS UNIQUEIDENTIFIER), N'faculty.cs.3', N'faculty.cs.3@demo.local', @PwdHash, @RoleFaculty, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Dr. Sarah Ahmed'),
    (CAST('77777777-7777-7777-7777-777777777104' AS UNIQUEIDENTIFIER), N'faculty.cs.4', N'faculty.cs.4@demo.local', @PwdHash, @RoleFaculty, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Prof. Kamran Ali'),

    -- ===== University Business Faculty =====
    (CAST('77777777-7777-7777-7777-777777777201' AS UNIQUEIDENTIFIER), N'faculty.bus.1', N'faculty.bus.1@demo.local', @PwdHash, @RoleFaculty, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Dr. Fatima Raza'),
    (CAST('77777777-7777-7777-7777-777777777202' AS UNIQUEIDENTIFIER), N'faculty.bus.2', N'faculty.bus.2@demo.local', @PwdHash, @RoleFaculty, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Dr. Usman Ghani'),
    (CAST('77777777-7777-7777-7777-777777777203' AS UNIQUEIDENTIFIER), N'faculty.bus.3', N'faculty.bus.3@demo.local', @PwdHash, @RoleFaculty, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Prof. Hira Tariq'),

    -- ===== University Languages Faculty =====
    (CAST('77777777-7777-7777-7777-777777777301' AS UNIQUEIDENTIFIER), N'faculty.lang.1', N'faculty.lang.1@demo.local', @PwdHash, @RoleFaculty, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Dr. Omar Farooq'),
    (CAST('77777777-7777-7777-7777-777777777302' AS UNIQUEIDENTIFIER), N'faculty.lang.2', N'faculty.lang.2@demo.local', @PwdHash, @RoleFaculty, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Prof. Aisha Noor'),

    -- ===== College Faculty =====
    (CAST('77777777-7777-7777-7777-777777777401' AS UNIQUEIDENTIFIER), N'faculty.col.1', N'faculty.col.1@demo.local', @PwdHash, @RoleFaculty, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Prof. Salman Khan'),
    (CAST('77777777-7777-7777-7777-777777777402' AS UNIQUEIDENTIFIER), N'faculty.col.2', N'faculty.col.2@demo.local', @PwdHash, @RoleFaculty, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Prof. Nida Malik'),
    (CAST('77777777-7777-7777-7777-777777777403' AS UNIQUEIDENTIFIER), N'faculty.col.3', N'faculty.col.3@demo.local', @PwdHash, @RoleFaculty, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Prof. Imran Shah'),

    -- ===== School Faculty =====
    (CAST('77777777-7777-7777-7777-777777777501' AS UNIQUEIDENTIFIER), N'faculty.sch.1', N'faculty.sch.1@demo.local', @PwdHash, @RoleFaculty, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Ms. Rabia Hassan'),
    (CAST('77777777-7777-7777-7777-777777777502' AS UNIQUEIDENTIFIER), N'faculty.sch.2', N'faculty.sch.2@demo.local', @PwdHash, @RoleFaculty, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Mr. Tariq Mehmood'),
    (CAST('77777777-7777-7777-7777-777777777503' AS UNIQUEIDENTIFIER), N'faculty.sch.3', N'faculty.sch.3@demo.local', @PwdHash, @RoleFaculty, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Ms. Sana Javed'),

    -- ===== Students — University BSCS (10) =====
    (CAST('88888888-8888-8888-8888-888888881101' AS UNIQUEIDENTIFIER), N'student.bscs.1',  N'student.bscs.1@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Ali Raza'),
    (CAST('88888888-8888-8888-8888-888888881102' AS UNIQUEIDENTIFIER), N'student.bscs.2',  N'student.bscs.2@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Hamza Shahid'),
    (CAST('88888888-8888-8888-8888-888888881103' AS UNIQUEIDENTIFIER), N'student.bscs.3',  N'student.bscs.3@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Hassan Riaz'),
    (CAST('88888888-8888-8888-8888-888888881104' AS UNIQUEIDENTIFIER), N'student.bscs.4',  N'student.bscs.4@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Mariam Noor'),
    (CAST('88888888-8888-8888-8888-888888881105' AS UNIQUEIDENTIFIER), N'student.bscs.5',  N'student.bscs.5@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Sara Tariq'),
    (CAST('88888888-8888-8888-8888-888888881106' AS UNIQUEIDENTIFIER), N'student.bscs.6',  N'student.bscs.6@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Zain Abbas'),
    (CAST('88888888-8888-8888-8888-888888881107' AS UNIQUEIDENTIFIER), N'student.bscs.7',  N'student.bscs.7@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Ayesha Khan'),
    (CAST('88888888-8888-8888-8888-888888881108' AS UNIQUEIDENTIFIER), N'student.bscs.8',  N'student.bscs.8@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Farhan Saeed'),
    (CAST('88888888-8888-8888-8888-888888881109' AS UNIQUEIDENTIFIER), N'student.bscs.9',  N'student.bscs.9@demo.local',  @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Nadia Ahmed'),
    (CAST('88888888-8888-8888-8888-888888881110' AS UNIQUEIDENTIFIER), N'student.bscs.10', N'student.bscs.10@demo.local', @PwdHash, @RoleStudent, @UniCSDeptId,  @UniTenantId, @UniCampusId, 2, 1, N'Usman Khalid'),

    -- ===== Students — University MCS (5) =====
    (CAST('88888888-8888-8888-8888-888888881201' AS UNIQUEIDENTIFIER), N'student.mcs.1', N'student.mcs.1@demo.local', @PwdHash, @RoleStudent, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Bilal Ahmed'),
    (CAST('88888888-8888-8888-8888-888888881202' AS UNIQUEIDENTIFIER), N'student.mcs.2', N'student.mcs.2@demo.local', @PwdHash, @RoleStudent, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Farah Iqbal'),
    (CAST('88888888-8888-8888-8888-888888881203' AS UNIQUEIDENTIFIER), N'student.mcs.3', N'student.mcs.3@demo.local', @PwdHash, @RoleStudent, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Kashif Mir'),
    (CAST('88888888-8888-8888-8888-888888881204' AS UNIQUEIDENTIFIER), N'student.mcs.4', N'student.mcs.4@demo.local', @PwdHash, @RoleStudent, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Rabia Latif'),
    (CAST('88888888-8888-8888-8888-888888881205' AS UNIQUEIDENTIFIER), N'student.mcs.5', N'student.mcs.5@demo.local', @PwdHash, @RoleStudent, @UniCSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Sohail Tanveer'),

    -- ===== Students — University BBA (8) =====
    (CAST('88888888-8888-8888-8888-888888882101' AS UNIQUEIDENTIFIER), N'student.bba.1', N'student.bba.1@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Ahmed Raza'),
    (CAST('88888888-8888-8888-8888-888888882102' AS UNIQUEIDENTIFIER), N'student.bba.2', N'student.bba.2@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Zainab Bukhari'),
    (CAST('88888888-8888-8888-8888-888888882103' AS UNIQUEIDENTIFIER), N'student.bba.3', N'student.bba.3@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Kamran Yusuf'),
    (CAST('88888888-8888-8888-8888-888888882104' AS UNIQUEIDENTIFIER), N'student.bba.4', N'student.bba.4@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Mehwish Alam'),
    (CAST('88888888-8888-8888-8888-888888882105' AS UNIQUEIDENTIFIER), N'student.bba.5', N'student.bba.5@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Haroon Rashid'),
    (CAST('88888888-8888-8888-8888-888888882106' AS UNIQUEIDENTIFIER), N'student.bba.6', N'student.bba.6@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Sania Mirza'),
    (CAST('88888888-8888-8888-8888-888888882107' AS UNIQUEIDENTIFIER), N'student.bba.7', N'student.bba.7@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Talha Mehmood'),
    (CAST('88888888-8888-8888-8888-888888882108' AS UNIQUEIDENTIFIER), N'student.bba.8', N'student.bba.8@demo.local', @PwdHash, @RoleStudent, @UniBUSDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Hira Sheikh'),

    -- ===== Students — University Arabic Language (5) =====
    (CAST('88888888-8888-8888-8888-888888883101' AS UNIQUEIDENTIFIER), N'student.lang.1', N'student.lang.1@demo.local', @PwdHash, @RoleStudent, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Ibrahim Hassan'),
    (CAST('88888888-8888-8888-8888-888888883102' AS UNIQUEIDENTIFIER), N'student.lang.2', N'student.lang.2@demo.local', @PwdHash, @RoleStudent, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Khadija Yusuf'),
    (CAST('88888888-8888-8888-8888-888888883103' AS UNIQUEIDENTIFIER), N'student.lang.3', N'student.lang.3@demo.local', @PwdHash, @RoleStudent, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Yusuf Ali'),
    (CAST('88888888-8888-8888-8888-888888883104' AS UNIQUEIDENTIFIER), N'student.lang.4', N'student.lang.4@demo.local', @PwdHash, @RoleStudent, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Amina Sadiq'),
    (CAST('88888888-8888-8888-8888-888888883105' AS UNIQUEIDENTIFIER), N'student.lang.5', N'student.lang.5@demo.local', @PwdHash, @RoleStudent, @UniLANGDeptId, @UniTenantId, @UniCampusId, 2, 1, N'Ismail Qureshi'),

    -- ===== Students — College ICS (8) =====
    (CAST('88888888-8888-8888-8888-888888884101' AS UNIQUEIDENTIFIER), N'student.ics.1', N'student.ics.1@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Ahsan Javed'),
    (CAST('88888888-8888-8888-8888-888888884102' AS UNIQUEIDENTIFIER), N'student.ics.2', N'student.ics.2@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Madiha Khan'),
    (CAST('88888888-8888-8888-8888-888888884103' AS UNIQUEIDENTIFIER), N'student.ics.3', N'student.ics.3@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Waqas Ali'),
    (CAST('88888888-8888-8888-8888-888888884104' AS UNIQUEIDENTIFIER), N'student.ics.4', N'student.ics.4@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Nimra Saeed'),
    (CAST('88888888-8888-8888-8888-888888884105' AS UNIQUEIDENTIFIER), N'student.ics.5', N'student.ics.5@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Fawad Ahmed'),
    (CAST('88888888-8888-8888-8888-888888884106' AS UNIQUEIDENTIFIER), N'student.ics.6', N'student.ics.6@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Komal Riaz'),
    (CAST('88888888-8888-8888-8888-888888884107' AS UNIQUEIDENTIFIER), N'student.ics.7', N'student.ics.7@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Saad Hassan'),
    (CAST('88888888-8888-8888-8888-888888884108' AS UNIQUEIDENTIFIER), N'student.ics.8', N'student.ics.8@demo.local', @PwdHash, @RoleStudent, @ColSCIDeptId, @ColTenantId, @ColCampusId, 1, 1, N'Anum Shahid'),

    -- ===== Students — School Science (12 across classes 1-10) =====
    (CAST('88888888-8888-8888-8888-888888885101' AS UNIQUEIDENTIFIER), N'student.sch.1',  N'student.sch.1@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Ayan Malik'),
    (CAST('88888888-8888-8888-8888-888888885102' AS UNIQUEIDENTIFIER), N'student.sch.2',  N'student.sch.2@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Esha Noor'),
    (CAST('88888888-8888-8888-8888-888888885103' AS UNIQUEIDENTIFIER), N'student.sch.3',  N'student.sch.3@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Rayyan Ahmed'),
    (CAST('88888888-8888-8888-8888-888888885104' AS UNIQUEIDENTIFIER), N'student.sch.4',  N'student.sch.4@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Zara Tariq'),
    (CAST('88888888-8888-8888-8888-888888885105' AS UNIQUEIDENTIFIER), N'student.sch.5',  N'student.sch.5@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Hadiya Khan'),
    (CAST('88888888-8888-8888-8888-888888885106' AS UNIQUEIDENTIFIER), N'student.sch.6',  N'student.sch.6@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Bilal Sheikh'),
    (CAST('88888888-8888-8888-8888-888888885107' AS UNIQUEIDENTIFIER), N'student.sch.7',  N'student.sch.7@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Minahil Abbas'),
    (CAST('88888888-8888-8888-8888-888888885108' AS UNIQUEIDENTIFIER), N'student.sch.8',  N'student.sch.8@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Adnan Riaz'),
    (CAST('88888888-8888-8888-8888-888888885109' AS UNIQUEIDENTIFIER), N'student.sch.9',  N'student.sch.9@demo.local',  @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Laiba Qadir'),
    (CAST('88888888-8888-8888-8888-888888885110' AS UNIQUEIDENTIFIER), N'student.sch.10', N'student.sch.10@demo.local', @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Shayan Ali'),
    (CAST('88888888-8888-8888-8888-888888885111' AS UNIQUEIDENTIFIER), N'student.sch.11', N'student.sch.11@demo.local', @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Dania Amir'),
    (CAST('88888888-8888-8888-8888-888888885112' AS UNIQUEIDENTIFIER), N'student.sch.12', N'student.sch.12@demo.local', @PwdHash, @RoleStudent, @SchSCIDeptId, @SchTenantId, @SchCampusId, 0, 1, N'Rohan Farooq');

    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId],
        [TenantId], [CampusId], [InstitutionType], [IsActive], [FullName], [LastPasswordChangedAt],
        [ConsentToMonitoring], [DataRetentionDate], [CreatedAt], [UpdatedAt])
    SELECT u.Id, u.Username, u.Email, u.PasswordHash, u.RoleId, u.DepartmentId,
        u.TenantId, u.CampusId, u.InstitutionType, u.IsActive, u.FullName,
        DATEADD(DAY, -30, @Now), 1, DATEADD(YEAR, 7, @Now), @Now, NULL
    FROM @Users u
    WHERE NOT EXISTS (SELECT 1 FROM [users] x WHERE x.[Id] = u.Id);

    -- Sync named users
    UPDATE tgt SET
        tgt.[Username] = src.[Username], tgt.[Email] = src.[Email],
        tgt.[PasswordHash] = src.[PasswordHash], tgt.[RoleId] = src.[RoleId],
        tgt.[DepartmentId] = src.[DepartmentId], tgt.[TenantId] = src.[TenantId],
        tgt.[CampusId] = src.[CampusId], tgt.[InstitutionType] = src.[InstitutionType],
        tgt.[IsActive] = src.[IsActive], tgt.[FullName] = src.[FullName],
        tgt.[UpdatedAt] = @Now
    FROM [users] tgt INNER JOIN @Users src ON src.[Id] = tgt.[Id]
    WHERE tgt.[Username] <> src.[Username]
       OR ISNULL(tgt.[Email], N'') <> ISNULL(src.[Email], N'')
       OR ISNULL(tgt.[FullName], N'') <> ISNULL(src.[FullName], N'')
       OR tgt.[IsActive] <> src.[IsActive];
END

PRINT 'Users seeded.';

-- ==============================================================================
-- SECTION 7.1: ADMIN / FACULTY DEPARTMENT ASSIGNMENTS
-- ==============================================================================

IF OBJECT_ID(N'[admin_department_assignments]') IS NOT NULL
BEGIN
    INSERT INTO [admin_department_assignments] ([Id], [AdminUserId], [DepartmentId], [AssignedAt], [CreatedAt])
    SELECT CAST(CONCAT('ADADADAD-ADAD-ADAD-ADAD-ADADADADAD', RIGHT('0' + CAST(u.RowNum AS NVARCHAR), 2)) AS UNIQUEIDENTIFIER),
           u.AdminUserId, u.DeptId, @Now, @Now
    FROM (
        SELECT CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER) AS AdminUserId, @UniCSDeptId   AS DeptId, 1 AS RowNum
        UNION ALL SELECT CAST('66666666-6666-6666-6666-666666666622' AS UNIQUEIDENTIFIER), @UniBUSDeptId,  2
        UNION ALL SELECT CAST('66666666-6666-6666-6666-666666666623' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 3
        UNION ALL SELECT CAST('66666666-6666-6666-6666-666666666624' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  4
        UNION ALL SELECT CAST('66666666-6666-6666-6666-666666666625' AS UNIQUEIDENTIFIER), @SchSCIDeptId,  5
    ) u
    WHERE NOT EXISTS (SELECT 1 FROM [admin_department_assignments] x
        WHERE x.[AdminUserId] = u.AdminUserId AND x.[DepartmentId] = u.DeptId AND x.[RemovedAt] IS NULL);
END

IF OBJECT_ID(N'[faculty_department_assignments]') IS NOT NULL
BEGIN
    INSERT INTO [faculty_department_assignments] ([Id], [FacultyUserId], [DepartmentId], [AssignedAt], [CreatedAt])
    SELECT CAST(CONCAT('FAFAFAFA-FAFA-FAFA-FAFA-FAFAFAFAFA', RIGHT('0' + CAST(f.RowNum AS NVARCHAR), 2)) AS UNIQUEIDENTIFIER),
           f.FacultyUserId, f.DeptId, @Now, @Now
    FROM (
        -- All faculty assigned to their departments
        SELECT CAST('77777777-7777-7777-7777-777777777101' AS UNIQUEIDENTIFIER) AS FacultyUserId, @UniCSDeptId   AS DeptId, 1 AS RowNum
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777102' AS UNIQUEIDENTIFIER), @UniCSDeptId,   2
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777103' AS UNIQUEIDENTIFIER), @UniCSDeptId,   3
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777104' AS UNIQUEIDENTIFIER), @UniCSDeptId,   4
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777201' AS UNIQUEIDENTIFIER), @UniBUSDeptId,  5
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777202' AS UNIQUEIDENTIFIER), @UniBUSDeptId,  6
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777203' AS UNIQUEIDENTIFIER), @UniBUSDeptId,  7
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777301' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 8
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777302' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 9
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777401' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  10
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777402' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  11
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777403' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  12
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777501' AS UNIQUEIDENTIFIER), @SchSCIDeptId,  13
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777502' AS UNIQUEIDENTIFIER), @SchSCIDeptId,  14
        UNION ALL SELECT CAST('77777777-7777-7777-7777-777777777503' AS UNIQUEIDENTIFIER), @SchSCIDeptId,  15
    ) f
    WHERE NOT EXISTS (SELECT 1 FROM [faculty_department_assignments] x
        WHERE x.[FacultyUserId] = f.FacultyUserId AND x.[DepartmentId] = f.DeptId AND x.[RemovedAt] IS NULL);
END

PRINT 'Department assignments seeded.';

-- ==============================================================================
-- SECTION 8: STUDENT PROFILES
-- ==============================================================================

IF OBJECT_ID(N'[student_profiles]') IS NOT NULL
BEGIN
    DECLARE @Profiles TABLE (Id UNIQUEIDENTIFIER, UserId UNIQUEIDENTIFIER, RegistrationNumber NVARCHAR(50),
        ProgramId UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER, CurrentSemesterNumber INT, Cgpa DECIMAL(4,2),
        AdmissionDate DATETIME2, Status INT);

    INSERT INTO @Profiles VALUES
    -- BSCS (semester 1-8 mixed)
    (CAST('99999999-9999-9999-9999-999999991101' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881101' AS UNIQUEIDENTIFIER), N'2026-CS-0001', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  1, 3.50, DATEADD(DAY, -60,  @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991102' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881102' AS UNIQUEIDENTIFIER), N'2026-CS-0002', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  2, 3.20, DATEADD(DAY, -200, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991103' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881103' AS UNIQUEIDENTIFIER), N'2026-CS-0003', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  3, 2.90, DATEADD(DAY, -400, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991104' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881104' AS UNIQUEIDENTIFIER), N'2026-CS-0004', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  4, 3.75, DATEADD(DAY, -580, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991105' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881105' AS UNIQUEIDENTIFIER), N'2026-CS-0005', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  5, 3.10, DATEADD(DAY, -760, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991106' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881106' AS UNIQUEIDENTIFIER), N'2026-CS-0006', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  6, 3.45, DATEADD(DAY, -940, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991107' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881107' AS UNIQUEIDENTIFIER), N'2026-CS-0007', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  7, 3.60, DATEADD(DAY, -1120,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999991108' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881108' AS UNIQUEIDENTIFIER), N'2026-CS-0008', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  8, 3.30, DATEADD(DAY, -1300,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999991109' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881109' AS UNIQUEIDENTIFIER), N'2026-CS-0009', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  3, 2.80, DATEADD(DAY, -420, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991110' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881110' AS UNIQUEIDENTIFIER), N'2026-CS-0010', CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER), @UniCSDeptId,  5, 3.00, DATEADD(DAY, -780, @Now), 0),

    -- MCS (semester 1-4)
    (CAST('99999999-9999-9999-9999-999999991201' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881201' AS UNIQUEIDENTIFIER), N'2026-CS-0011', CAST('22222222-2222-2222-2222-222222222212' AS UNIQUEIDENTIFIER), @UniCSDeptId,  1, 3.65, DATEADD(DAY, -50,  @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991202' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881202' AS UNIQUEIDENTIFIER), N'2026-CS-0012', CAST('22222222-2222-2222-2222-222222222212' AS UNIQUEIDENTIFIER), @UniCSDeptId,  2, 3.40, DATEADD(DAY, -200, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991203' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881203' AS UNIQUEIDENTIFIER), N'2026-CS-0013', CAST('22222222-2222-2222-2222-222222222212' AS UNIQUEIDENTIFIER), @UniCSDeptId,  3, 3.15, DATEADD(DAY, -380, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991204' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881204' AS UNIQUEIDENTIFIER), N'2026-CS-0014', CAST('22222222-2222-2222-2222-222222222212' AS UNIQUEIDENTIFIER), @UniCSDeptId,  4, 3.55, DATEADD(DAY, -560, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999991205' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881205' AS UNIQUEIDENTIFIER), N'2026-CS-0015', CAST('22222222-2222-2222-2222-222222222212' AS UNIQUEIDENTIFIER), @UniCSDeptId,  2, 3.25, DATEADD(DAY, -210, @Now), 0),

    -- BBA (semester 1-8)
    (CAST('99999999-9999-9999-9999-999999992101' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882101' AS UNIQUEIDENTIFIER), N'2026-BBA-0001', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 1, 3.30, DATEADD(DAY, -60,  @Now), 0),
    (CAST('99999999-9999-9999-9999-999999992102' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882102' AS UNIQUEIDENTIFIER), N'2026-BBA-0002', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 2, 3.85, DATEADD(DAY, -200, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999992103' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882103' AS UNIQUEIDENTIFIER), N'2026-BBA-0003', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 4, 3.15, DATEADD(DAY, -580, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999992104' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882104' AS UNIQUEIDENTIFIER), N'2026-BBA-0004', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 6, 2.95, DATEADD(DAY, -940, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999992105' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882105' AS UNIQUEIDENTIFIER), N'2026-BBA-0005', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 3, 3.50, DATEADD(DAY, -400, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999992106' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882106' AS UNIQUEIDENTIFIER), N'2026-BBA-0006', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 1, 3.20, DATEADD(DAY, -50,  @Now), 0),
    (CAST('99999999-9999-9999-9999-999999992107' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882107' AS UNIQUEIDENTIFIER), N'2026-BBA-0007', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 7, 3.40, DATEADD(DAY, -1120,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999992108' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888882108' AS UNIQUEIDENTIFIER), N'2026-BBA-0008', CAST('22222222-2222-2222-2222-222222222213' AS UNIQUEIDENTIFIER), @UniBUSDeptId, 8, 3.10, DATEADD(DAY, -1300,@Now), 0),

    -- Arabic Language (semester 1-4, percentage-based)
    (CAST('99999999-9999-9999-9999-999999993101' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888883101' AS UNIQUEIDENTIFIER), N'2026-LANG-0001', CAST('22222222-2222-2222-2222-222222222214' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 1, 0.00, DATEADD(DAY, -50,  @Now), 0),
    (CAST('99999999-9999-9999-9999-999999993102' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888883102' AS UNIQUEIDENTIFIER), N'2026-LANG-0002', CAST('22222222-2222-2222-2222-222222222214' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 2, 0.00, DATEADD(DAY, -200, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999993103' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888883103' AS UNIQUEIDENTIFIER), N'2026-LANG-0003', CAST('22222222-2222-2222-2222-222222222214' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 3, 0.00, DATEADD(DAY, -380, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999993104' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888883104' AS UNIQUEIDENTIFIER), N'2026-LANG-0004', CAST('22222222-2222-2222-2222-222222222214' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 4, 0.00, DATEADD(DAY, -560, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999993105' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888883105' AS UNIQUEIDENTIFIER), N'2026-LANG-0005', CAST('22222222-2222-2222-2222-222222222214' AS UNIQUEIDENTIFIER), @UniLANGDeptId, 1, 0.00, DATEADD(DAY, -45,  @Now), 0),

    -- College ICS (Class 11 = semester 1, Class 12 = semester 2)
    (CAST('99999999-9999-9999-9999-999999994101' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884101' AS UNIQUEIDENTIFIER), N'2026-ICS-0001', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  1, 0.00, DATEADD(DAY, -50, @Now),  0),
    (CAST('99999999-9999-9999-9999-999999994102' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884102' AS UNIQUEIDENTIFIER), N'2026-ICS-0002', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  1, 0.00, DATEADD(DAY, -55, @Now),  0),
    (CAST('99999999-9999-9999-9999-999999994103' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884103' AS UNIQUEIDENTIFIER), N'2026-ICS-0003', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  1, 0.00, DATEADD(DAY, -60, @Now),  0),
    (CAST('99999999-9999-9999-9999-999999994104' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884104' AS UNIQUEIDENTIFIER), N'2026-ICS-0004', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  2, 0.00, DATEADD(DAY, -210, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999994105' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884105' AS UNIQUEIDENTIFIER), N'2026-ICS-0005', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  2, 0.00, DATEADD(DAY, -220, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999994106' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884106' AS UNIQUEIDENTIFIER), N'2026-ICS-0006', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  1, 0.00, DATEADD(DAY, -65, @Now),  0),
    (CAST('99999999-9999-9999-9999-999999994107' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884107' AS UNIQUEIDENTIFIER), N'2026-ICS-0007', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  2, 0.00, DATEADD(DAY, -230, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999994108' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884108' AS UNIQUEIDENTIFIER), N'2026-ICS-0008', CAST('22222222-2222-2222-2222-222222222231' AS UNIQUEIDENTIFIER), @ColSCIDeptId,  2, 0.00, DATEADD(DAY, -240, @Now), 0),

    -- School Science (Class 1-10)
    (CAST('99999999-9999-9999-9999-999999995101' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885101' AS UNIQUEIDENTIFIER), N'2026-SCI-C01-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 1,  0.00, DATEADD(DAY, -5,   @Now), 0),
    (CAST('99999999-9999-9999-9999-999999995102' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885102' AS UNIQUEIDENTIFIER), N'2026-SCI-C02-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 2,  0.00, DATEADD(DAY, -370, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999995103' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885103' AS UNIQUEIDENTIFIER), N'2026-SCI-C03-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 3,  0.00, DATEADD(DAY, -735, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999995104' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885104' AS UNIQUEIDENTIFIER), N'2026-SCI-C04-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 4,  0.00, DATEADD(DAY, -1100,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999995105' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885105' AS UNIQUEIDENTIFIER), N'2026-SCI-C05-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 5,  0.00, DATEADD(DAY, -1465,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999995106' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885106' AS UNIQUEIDENTIFIER), N'2026-SCI-C06-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 6,  0.00, DATEADD(DAY, -1830,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999995107' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885107' AS UNIQUEIDENTIFIER), N'2026-SCI-C07-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 7,  0.00, DATEADD(DAY, -2195,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999995108' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885108' AS UNIQUEIDENTIFIER), N'2026-SCI-C08-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 8,  0.00, DATEADD(DAY, -2560,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999995109' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885109' AS UNIQUEIDENTIFIER), N'2026-SCI-C09-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 9,  0.00, DATEADD(DAY, -2925,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999995110' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885110' AS UNIQUEIDENTIFIER), N'2026-SCI-C10-001', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 10, 0.00, DATEADD(DAY, -3290,@Now), 0),
    (CAST('99999999-9999-9999-9999-999999995111' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885111' AS UNIQUEIDENTIFIER), N'2026-SCI-C03-002', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 3,  0.00, DATEADD(DAY, -740, @Now), 0),
    (CAST('99999999-9999-9999-9999-999999995112' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888885112' AS UNIQUEIDENTIFIER), N'2026-SCI-C07-002', CAST('22222222-2222-2222-2222-222222222251' AS UNIQUEIDENTIFIER), @SchSCIDeptId, 7,  0.00, DATEADD(DAY, -2200,@Now), 0);

    INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId],
        [CurrentSemesterNumber], [Cgpa], [AdmissionDate], [Status], [CreatedAt], [UpdatedAt])
    SELECT p.Id, p.UserId, p.RegistrationNumber, p.ProgramId, p.DepartmentId,
        p.CurrentSemesterNumber, p.Cgpa, p.AdmissionDate, p.Status, @Now, NULL
    FROM @Profiles p
    WHERE EXISTS (SELECT 1 FROM [users] u WHERE u.[Id] = p.[UserId])
      AND NOT EXISTS (SELECT 1 FROM [student_profiles] x WHERE x.[Id] = p.Id);

    UPDATE tgt SET tgt.[RegistrationNumber] = src.[RegistrationNumber],
        tgt.[CurrentSemesterNumber] = src.[CurrentSemesterNumber],
        tgt.[Cgpa] = src.[Cgpa], tgt.[UpdatedAt] = @Now
    FROM [student_profiles] tgt INNER JOIN @Profiles src ON src.[Id] = tgt.[Id]
    WHERE tgt.[RegistrationNumber] <> src.[RegistrationNumber]
       OR tgt.[CurrentSemesterNumber] <> src.[CurrentSemesterNumber];
END

PRINT 'Student profiles seeded.';

-- ==============================================================================
-- SECTION 9: COURSE OFFERINGS (per semester)
-- ==============================================================================

IF OBJECT_ID(N'[course_offerings]') IS NOT NULL
BEGIN
    DECLARE @Offerings TABLE (Id UNIQUEIDENTIFIER, CourseId UNIQUEIDENTIFIER, SemesterId UNIQUEIDENTIFIER,
        FacultyUserId UNIQUEIDENTIFIER, MaxEnrollment INT, IsOpen BIT,
        TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER, InstitutionType INT);

    DECLARE @Sem1 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333301' AS UNIQUEIDENTIFIER); -- Fall 2026
    DECLARE @Sem2 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333302' AS UNIQUEIDENTIFIER); -- Spring 2027
    DECLARE @Sem3 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333303' AS UNIQUEIDENTIFIER);
    DECLARE @Sem4 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333304' AS UNIQUEIDENTIFIER);
    DECLARE @Sem5 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333305' AS UNIQUEIDENTIFIER);
    DECLARE @Sem6 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333306' AS UNIQUEIDENTIFIER);
    DECLARE @Sem7 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333307' AS UNIQUEIDENTIFIER);
    DECLARE @Sem8 UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333308' AS UNIQUEIDENTIFIER);

    -- University CS courses (GPA)
    DECLARE @CS_PF   UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444101' AS UNIQUEIDENTIFIER);
    DECLARE @CS_DS   UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444102' AS UNIQUEIDENTIFIER);
    DECLARE @CS_ALGO UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444103' AS UNIQUEIDENTIFIER);
    DECLARE @CS_DB   UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444104' AS UNIQUEIDENTIFIER);
    DECLARE @CS_WEB  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444105' AS UNIQUEIDENTIFIER);
    DECLARE @CS_SE   UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444106' AS UNIQUEIDENTIFIER);
    DECLARE @CS_AI   UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444107' AS UNIQUEIDENTIFIER);
    DECLARE @CS_CAP  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444108' AS UNIQUEIDENTIFIER);
    DECLARE @CS_ADB  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444109' AS UNIQUEIDENTIFIER);
    DECLARE @CS_ML   UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444110' AS UNIQUEIDENTIFIER);
    DECLARE @CS_THESIS UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444111' AS UNIQUEIDENTIFIER);

    -- CS Faculty
    DECLARE @CS_F1 UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777101' AS UNIQUEIDENTIFIER);
    DECLARE @CS_F2 UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777102' AS UNIQUEIDENTIFIER);
    DECLARE @CS_F3 UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777103' AS UNIQUEIDENTIFIER);
    DECLARE @CS_F4 UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777104' AS UNIQUEIDENTIFIER);

    INSERT INTO @Offerings VALUES
    -- BSCS Semester 1 courses (Fall 2026)
    (CAST('55555555-5555-5555-5555-555555555101' AS UNIQUEIDENTIFIER), @CS_PF,  @Sem1, @CS_F1, 40, 1, @UniTenantId, @UniCampusId, 2),
    -- BSCS Sem 2
    (CAST('55555555-5555-5555-5555-555555555102' AS UNIQUEIDENTIFIER), @CS_DS,  @Sem2, @CS_F2, 35, 1, @UniTenantId, @UniCampusId, 2),
    -- BSCS Sem 3
    (CAST('55555555-5555-5555-5555-555555555103' AS UNIQUEIDENTIFIER), @CS_ALGO,@Sem3, @CS_F2, 35, 1, @UniTenantId, @UniCampusId, 2),
    -- BSCS Sem 4
    (CAST('55555555-5555-5555-5555-555555555104' AS UNIQUEIDENTIFIER), @CS_DB,  @Sem4, @CS_F3, 30, 1, @UniTenantId, @UniCampusId, 2),
    -- BSCS Sem 5
    (CAST('55555555-5555-5555-5555-555555555105' AS UNIQUEIDENTIFIER), @CS_WEB, @Sem5, @CS_F3, 30, 1, @UniTenantId, @UniCampusId, 2),
    -- BSCS Sem 6
    (CAST('55555555-5555-5555-5555-555555555106' AS UNIQUEIDENTIFIER), @CS_SE,  @Sem6, @CS_F4, 30, 1, @UniTenantId, @UniCampusId, 2),
    -- BSCS Sem 7
    (CAST('55555555-5555-5555-5555-555555555107' AS UNIQUEIDENTIFIER), @CS_AI,  @Sem7, @CS_F4, 30, 1, @UniTenantId, @UniCampusId, 2),
    -- BSCS Sem 8
    (CAST('55555555-5555-5555-5555-555555555108' AS UNIQUEIDENTIFIER), @CS_CAP, @Sem8, @CS_F1, 30, 1, @UniTenantId, @UniCampusId, 2),
    -- MCS Sem 1 (advanced, same semesters)
    (CAST('55555555-5555-5555-5555-555555555109' AS UNIQUEIDENTIFIER), @CS_ADB,  @Sem1, @CS_F1, 25, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555110' AS UNIQUEIDENTIFIER), @CS_ML,   @Sem3, @CS_F4, 25, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555111' AS UNIQUEIDENTIFIER), @CS_THESIS, @Sem4, @CS_F2, 20, 1, @UniTenantId, @UniCampusId, 2);

    -- University BUS courses (GPA)
    DECLARE @BUS_MGT UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444201' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_MKT UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444202' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_FA  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444203' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_MA  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444204' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_ECO UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444205' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_HR  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444206' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_STR UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444207' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_F1  UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777201' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_F2  UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777202' AS UNIQUEIDENTIFIER);
    DECLARE @BUS_F3  UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777203' AS UNIQUEIDENTIFIER);

    INSERT INTO @Offerings VALUES
    (CAST('55555555-5555-5555-5555-555555555201' AS UNIQUEIDENTIFIER), @BUS_MGT, @Sem1, @BUS_F1, 40, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555202' AS UNIQUEIDENTIFIER), @BUS_MKT, @Sem2, @BUS_F2, 40, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555203' AS UNIQUEIDENTIFIER), @BUS_FA,  @Sem3, @BUS_F3, 35, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555204' AS UNIQUEIDENTIFIER), @BUS_MA,  @Sem4, @BUS_F1, 35, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555205' AS UNIQUEIDENTIFIER), @BUS_ECO, @Sem5, @BUS_F2, 35, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555206' AS UNIQUEIDENTIFIER), @BUS_HR,  @Sem6, @BUS_F3, 30, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555207' AS UNIQUEIDENTIFIER), @BUS_STR, @Sem7, @BUS_F1, 30, 1, @UniTenantId, @UniCampusId, 2);

    -- University LANG courses (Percentage)
    DECLARE @LANG_F1 UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777301' AS UNIQUEIDENTIFIER);
    DECLARE @LANG_F2 UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777302' AS UNIQUEIDENTIFIER);
    DECLARE @LANG_101 UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444301' AS UNIQUEIDENTIFIER);
    DECLARE @LANG_102 UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444302' AS UNIQUEIDENTIFIER);
    DECLARE @LANG_201 UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444303' AS UNIQUEIDENTIFIER);
    DECLARE @LANG_202 UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444304' AS UNIQUEIDENTIFIER);

    INSERT INTO @Offerings VALUES
    (CAST('55555555-5555-5555-5555-555555555301' AS UNIQUEIDENTIFIER), @LANG_101, @Sem1, @LANG_F1, 30, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555302' AS UNIQUEIDENTIFIER), @LANG_102, @Sem2, @LANG_F1, 30, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555303' AS UNIQUEIDENTIFIER), @LANG_201, @Sem3, @LANG_F2, 25, 1, @UniTenantId, @UniCampusId, 2),
    (CAST('55555555-5555-5555-5555-555555555304' AS UNIQUEIDENTIFIER), @LANG_202, @Sem4, @LANG_F2, 25, 1, @UniTenantId, @UniCampusId, 2);

    -- College ICS (Percentage)
    DECLARE @COL_PHYS UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444231' AS UNIQUEIDENTIFIER);
    DECLARE @COL_MATH UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444232' AS UNIQUEIDENTIFIER);
    DECLARE @COL_COMP UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444233' AS UNIQUEIDENTIFIER);
    DECLARE @COL_ENG  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444234' AS UNIQUEIDENTIFIER);
    DECLARE @COL_F1   UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777401' AS UNIQUEIDENTIFIER);
    DECLARE @COL_F2   UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777402' AS UNIQUEIDENTIFIER);
    DECLARE @COL_F3   UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777403' AS UNIQUEIDENTIFIER);

    INSERT INTO @Offerings VALUES
    -- Class 11 (Sem 1)
    (CAST('55555555-5555-5555-5555-555555555401' AS UNIQUEIDENTIFIER), @COL_PHYS, @Sem1, @COL_F1, 35, 1, @ColTenantId, @ColCampusId, 1),
    (CAST('55555555-5555-5555-5555-555555555402' AS UNIQUEIDENTIFIER), @COL_MATH, @Sem1, @COL_F2, 35, 1, @ColTenantId, @ColCampusId, 1),
    (CAST('55555555-5555-5555-5555-555555555403' AS UNIQUEIDENTIFIER), @COL_COMP, @Sem1, @COL_F3, 35, 1, @ColTenantId, @ColCampusId, 1),
    (CAST('55555555-5555-5555-5555-555555555404' AS UNIQUEIDENTIFIER), @COL_ENG,  @Sem1, @COL_F2, 35, 1, @ColTenantId, @ColCampusId, 1),
    -- Class 12 (Sem 2)
    (CAST('55555555-5555-5555-5555-555555555405' AS UNIQUEIDENTIFIER), @COL_PHYS, @Sem2, @COL_F1, 35, 1, @ColTenantId, @ColCampusId, 1),
    (CAST('55555555-5555-5555-5555-555555555406' AS UNIQUEIDENTIFIER), @COL_MATH, @Sem2, @COL_F2, 35, 1, @ColTenantId, @ColCampusId, 1),
    (CAST('55555555-5555-5555-5555-555555555407' AS UNIQUEIDENTIFIER), @COL_COMP, @Sem2, @COL_F3, 35, 1, @ColTenantId, @ColCampusId, 1),
    (CAST('55555555-5555-5555-5555-555555555408' AS UNIQUEIDENTIFIER), @COL_ENG,  @Sem2, @COL_F2, 35, 1, @ColTenantId, @ColCampusId, 1);

    -- School Science (Percentage, Classes 1-10 map to Semesters 1-10)
    DECLARE @SCH_PHYS UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444351' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_CHEM UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444352' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_MATH UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444353' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_COMP UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444354' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_ENG  UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444355' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_URDU UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444356' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_F1   UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777501' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_F2   UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777502' AS UNIQUEIDENTIFIER);
    DECLARE @SCH_F3   UNIQUEIDENTIFIER = CAST('77777777-7777-7777-7777-777777777503' AS UNIQUEIDENTIFIER);

    -- Active school classes (Classes 1-10 across semesters)
    -- Using Sem1 (Fall 2026) for odd classes, Sem2 (Spring 2027) for even
    INSERT INTO @Offerings VALUES
    -- Class 1 (Sem1)
    (CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER), @SCH_PHYS, @Sem1, @SCH_F1, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555502' AS UNIQUEIDENTIFIER), @SCH_CHEM, @Sem1, @SCH_F2, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555503' AS UNIQUEIDENTIFIER), @SCH_MATH, @Sem1, @SCH_F3, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555504' AS UNIQUEIDENTIFIER), @SCH_COMP, @Sem1, @SCH_F1, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555505' AS UNIQUEIDENTIFIER), @SCH_ENG,  @Sem1, @SCH_F2, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555506' AS UNIQUEIDENTIFIER), @SCH_URDU, @Sem1, @SCH_F3, 40, 1, @SchTenantId, @SchCampusId, 0),
    -- Class 10 (Sem2)
    (CAST('55555555-5555-5555-5555-555555555507' AS UNIQUEIDENTIFIER), @SCH_PHYS, @Sem2, @SCH_F1, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555508' AS UNIQUEIDENTIFIER), @SCH_CHEM, @Sem2, @SCH_F2, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555509' AS UNIQUEIDENTIFIER), @SCH_MATH, @Sem2, @SCH_F3, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555510' AS UNIQUEIDENTIFIER), @SCH_COMP, @Sem2, @SCH_F1, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555511' AS UNIQUEIDENTIFIER), @SCH_ENG,  @Sem2, @SCH_F2, 40, 1, @SchTenantId, @SchCampusId, 0),
    (CAST('55555555-5555-5555-5555-555555555512' AS UNIQUEIDENTIFIER), @SCH_URDU, @Sem2, @SCH_F3, 40, 1, @SchTenantId, @SchCampusId, 0);

    INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [MaxEnrollment], [IsOpen],
        [TenantId], [CampusId], [InstitutionType], [CreatedAt], [UpdatedAt])
    SELECT o.Id, o.CourseId, o.SemesterId, o.FacultyUserId, o.MaxEnrollment, o.IsOpen,
        o.TenantId, o.CampusId, o.InstitutionType, @Now, NULL
    FROM @Offerings o
    WHERE EXISTS (SELECT 1 FROM [courses] c WHERE c.[Id] = o.[CourseId])
      AND NOT EXISTS (SELECT 1 FROM [course_offerings] x WHERE x.[Id] = o.Id);
END

PRINT 'Course offerings seeded.';

-- ==============================================================================
-- SECTION 10: ENROLLMENTS
-- ==============================================================================

IF OBJECT_ID(N'[enrollments]') IS NOT NULL
BEGIN
    -- Enroll students in offerings matching their semester level
    ;WITH eligible AS (
        SELECT sp.Id AS ProfileId, sp.UserId, sp.CurrentSemesterNumber, sp.ProgramId,
               d.InstitutionType, d.Id AS DeptId, d.TenantId, d.CampusId
        FROM [student_profiles] sp
        JOIN [departments] d ON d.Id = sp.DepartmentId
        WHERE sp.IsDeleted = 0
    ),
    offerings AS (
        SELECT co.Id AS OfferingId, co.CourseId, co.SemesterId, co.TenantId, co.CampusId, co.InstitutionType
        FROM [course_offerings] co
    )
    INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [Status])
    SELECT CONVERT(uniqueidentifier, CONCAT('DDDDDDDD-DDDD-DDDD-DDDD-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY e.ProfileId, o.OfferingId) AS VARCHAR(12)), 12))),
           e.ProfileId, o.OfferingId, @Now, N'Active'
    FROM eligible e
    CROSS APPLY (SELECT TOP 5 o.* FROM offerings o WHERE o.InstitutionType = e.InstitutionType ORDER BY o.OfferingId) o
    WHERE NOT EXISTS (
        SELECT 1 FROM [enrollments] x
        WHERE x.[StudentProfileId] = e.ProfileId AND x.[CourseOfferingId] = o.OfferingId
    );
END

PRINT 'Enrollments seeded.';

-- ==============================================================================
-- SECTION 11: ASSIGNMENTS & SUBMISSIONS
-- ==============================================================================

IF OBJECT_ID(N'[assignments]') IS NOT NULL
BEGIN
    DECLARE @Assignments TABLE (Id UNIQUEIDENTIFIER, CourseOfferingId UNIQUEIDENTIFIER,
        Title NVARCHAR(300), Description NVARCHAR(4000), DueDate DATETIME2, MaxMarks DECIMAL(8,2), IsPublished BIT);

    INSERT INTO @Assignments
    SELECT CAST(CONCAT('EEEEEEEE-EEEE-EEEE-EEEE-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY co.Id) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           co.Id,
           N'Assignment ' + CAST(ROW_NUMBER() OVER (ORDER BY co.Id) AS NVARCHAR),
           N'Complete all problems. Show your work.',
           DATEADD(DAY, 14, @Now), 100.00, 1
    FROM [course_offerings] co
    WHERE EXISTS (SELECT 1 FROM [enrollments] e WHERE e.[CourseOfferingId] = co.Id)
      AND NOT EXISTS (SELECT TOP 1 1 FROM [assignments] a WHERE a.[CourseOfferingId] = co.Id);

    INSERT INTO [assignments] ([Id], [CourseOfferingId], [Title], [Description], [DueDate], [MaxMarks], [IsPublished], [CreatedAt], [UpdatedAt])
    SELECT a.Id, a.CourseOfferingId, a.Title, a.Description, a.DueDate, a.MaxMarks, a.IsPublished, @Now, NULL
    FROM @Assignments a
    WHERE NOT EXISTS (SELECT 1 FROM [assignments] x WHERE x.[Id] = a.Id);
END

-- Submissions
IF OBJECT_ID(N'[assignment_submissions]') IS NOT NULL
BEGIN
    INSERT INTO [assignment_submissions] ([Id], [AssignmentId], [StudentProfileId], [FileUrl], [TextContent], [SubmittedAt], [Status])
    SELECT CAST(CONCAT('AAAAAAAA-AAAA-AAAA-AAAA-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY a.Id, e.StudentProfileId) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           a.Id, e.StudentProfileId,
           N'/uploads/submission_' + CAST(ROW_NUMBER() OVER (ORDER BY a.Id, e.StudentProfileId) AS NVARCHAR) + N'.pdf',
           N'Submitted assignment solution.',
           DATEADD(DAY, -1, @Now), N'Submitted'
    FROM [assignments] a
    JOIN [enrollments] e ON e.[CourseOfferingId] = a.[CourseOfferingId]
    WHERE NOT EXISTS (
        SELECT 1 FROM [assignment_submissions] x
        WHERE x.[AssignmentId] = a.Id AND x.[StudentProfileId] = e.[StudentProfileId]
    );
END

PRINT 'Assignments and submissions seeded.';

-- ==============================================================================
-- SECTION 12: RESULTS (complete grade data)
-- ==============================================================================

IF OBJECT_ID(N'[results]') IS NOT NULL
BEGIN
    -- For each enrollment, create 3 result types: Quiz, Midterm, Final
    INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType],
        [MarksObtained], [MaxMarks], [GradePoint], [IsPublished], [PublishedAt], [PublishedByUserId])
    SELECT CAST(CONCAT('CCCCCCCC-CCCC-CCCC-CCCC-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY e.StudentProfileId, e.CourseOfferingId) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           e.StudentProfileId, e.CourseOfferingId,
           rt.ResultType, rt.MarksObtained, rt.MaxMarks,
           -- GPA for University IT/Business, NULL for percentage-based
           CASE WHEN d.InstitutionType = 2 AND d.Code IN ('CS','BUS') THEN CAST((rt.MarksObtained / NULLIF(rt.MaxMarks, 0)) * 4.0 AS DECIMAL(4,2)) ELSE NULL END,
           1, @Now, CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER)
    FROM [enrollments] e
    JOIN [course_offerings] co ON co.Id = e.CourseOfferingId
    JOIN [courses] c ON c.Id = co.CourseId
    JOIN [departments] d ON d.Id = c.DepartmentId
    CROSS APPLY (VALUES
        (N'Quiz',    CAST(20 + (ABS(CHECKSUM(NEWID())) % 16) AS DECIMAL(8,2)), CAST(25.00 AS DECIMAL(8,2))),
        (N'Midterm', CAST(30 + (ABS(CHECKSUM(NEWID())) % 26) AS DECIMAL(8,2)), CAST(40.00 AS DECIMAL(8,2))),
        (N'Final',   CAST(50 + (ABS(CHECKSUM(NEWID())) % 41) AS DECIMAL(8,2)), CAST(60.00 AS DECIMAL(8,2)))
    ) rt (ResultType, MarksObtained, MaxMarks)
    WHERE NOT EXISTS (
        SELECT 1 FROM [results] x
        WHERE x.[StudentProfileId] = e.[StudentProfileId]
          AND x.[CourseOfferingId] = e.[CourseOfferingId]
          AND x.[ResultType] = rt.ResultType
    );
END

PRINT 'Results seeded.';

-- ==============================================================================
-- SECTION 13: ATTENDANCE
-- ==============================================================================

IF OBJECT_ID(N'[attendance_records]') IS NOT NULL
BEGIN
    -- 30 days of attendance per enrollment
    ;WITH dates AS (
        SELECT DATEADD(DAY, n, DATEADD(DAY, -30, @Now)) AS RecDate
        FROM (SELECT TOP 30 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n FROM sys.all_objects) nums
    )
    INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId])
    SELECT CAST(CONCAT('BBBBBBBB-BBBB-BBBB-BBBB-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY e.StudentProfileId, e.CourseOfferingId, d.RecDate) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           e.StudentProfileId, e.CourseOfferingId, d.RecDate,
           CASE WHEN ABS(CHECKSUM(NEWID())) % 10 < 7 THEN N'Present'
                WHEN ABS(CHECKSUM(NEWID())) % 10 < 9 THEN N'Late'
                ELSE N'Absent' END,
           CAST('77777777-7777-7777-7777-777777777101' AS UNIQUEIDENTIFIER)
    FROM [enrollments] e
    CROSS JOIN dates d
    WHERE NOT EXISTS (
        SELECT 1 FROM [attendance_records] x
        WHERE x.[StudentProfileId] = e.[StudentProfileId]
          AND x.[CourseOfferingId] = e.[CourseOfferingId]
          AND x.[Date] = d.RecDate
    );
END

PRINT 'Attendance records seeded.';

-- ==============================================================================
-- SECTION 14: GRADING CONFIGURATION
-- ==============================================================================

-- institution_grading_profiles
IF OBJECT_ID(N'[institution_grading_profiles]') IS NOT NULL
BEGIN
    INSERT INTO [institution_grading_profiles] ([Id], [InstitutionType], [PassThreshold], [GradeRangesJson], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT * FROM (VALUES
        (CAST('43434343-4343-4343-4343-434343434301' AS UNIQUEIDENTIFIER), 0, 50.00, N'[{"grade":"A+","from":90,"to":100},{"grade":"A","from":80,"to":89},{"grade":"B","from":70,"to":79},{"grade":"C","from":60,"to":69},{"grade":"D","from":50,"to":59},{"grade":"F","from":0,"to":49}]', 1),
        (CAST('43434343-4343-4343-4343-434343434302' AS UNIQUEIDENTIFIER), 1, 55.00, N'[{"grade":"A+","from":90,"to":100},{"grade":"A","from":80,"to":89},{"grade":"B","from":70,"to":79},{"grade":"C","from":60,"to":69},{"grade":"D","from":55,"to":59},{"grade":"F","from":0,"to":54}]', 1),
        (CAST('43434343-4343-4343-4343-434343434303' AS UNIQUEIDENTIFIER), 2, 60.00, N'[{"grade":"A","from":85,"to":100},{"grade":"B","from":75,"to":84},{"grade":"C","from":65,"to":74},{"grade":"D","from":60,"to":64},{"grade":"F","from":0,"to":59}]', 1)
    ) v (Id, InstitutionType, PassThreshold, GradeRangesJson, IsActive)
    WHERE NOT EXISTS (SELECT 1 FROM [institution_grading_profiles] x WHERE x.[Id] = v.Id);
END

-- gpa_scale_rules (for University GPA-based departments)
IF OBJECT_ID(N'[gpa_scale_rules]') IS NOT NULL
BEGIN
    INSERT INTO [gpa_scale_rules] ([Id], [InstitutionType], [GradePoint], [MinimumScore], [DisplayOrder], [CreatedAt], [UpdatedAt])
    SELECT * FROM (VALUES
        (CAST('41414141-4141-4141-4141-414141414101' AS UNIQUEIDENTIFIER), 2, 4.00, 85.00, 1),
        (CAST('41414141-4141-4141-4141-414141414102' AS UNIQUEIDENTIFIER), 2, 3.70, 80.00, 2),
        (CAST('41414141-4141-4141-4141-414141414103' AS UNIQUEIDENTIFIER), 2, 3.30, 75.00, 3),
        (CAST('41414141-4141-4141-4141-414141414104' AS UNIQUEIDENTIFIER), 2, 3.00, 70.00, 4),
        (CAST('41414141-4141-4141-4141-414141414105' AS UNIQUEIDENTIFIER), 2, 2.70, 65.00, 5),
        (CAST('41414141-4141-4141-4141-414141414106' AS UNIQUEIDENTIFIER), 2, 2.30, 60.00, 6),
        (CAST('41414141-4141-4141-4141-414141414107' AS UNIQUEIDENTIFIER), 2, 2.00, 55.00, 7),
        (CAST('41414141-4141-4141-4141-414141414108' AS UNIQUEIDENTIFIER), 2, 1.70, 50.00, 8),
        (CAST('41414141-4141-4141-4141-414141414109' AS UNIQUEIDENTIFIER), 2, 0.00,  0.00, 9)
    ) v (Id, InstitutionType, GradePoint, MinimumScore, DisplayOrder)
    WHERE NOT EXISTS (SELECT 1 FROM [gpa_scale_rules] x WHERE x.[Id] = v.Id);
END

-- result_component_rules
IF OBJECT_ID(N'[result_component_rules]') IS NOT NULL
BEGIN
    INSERT INTO [result_component_rules] ([Id], [InstitutionType], [Name], [Weightage], [DisplayOrder], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT * FROM (VALUES
        -- School (0): Assignments + Quizzes + Midterm + Final
        (CAST('42424242-4242-4242-4242-424242424201' AS UNIQUEIDENTIFIER), 0, N'Assignments', 25.00, 1, 1),
        (CAST('42424242-4242-4242-4242-424242424202' AS UNIQUEIDENTIFIER), 0, N'Quizzes',     15.00, 2, 1),
        (CAST('42424242-4242-4242-4242-424242424203' AS UNIQUEIDENTIFIER), 0, N'Midterm',     25.00, 3, 1),
        (CAST('42424242-4242-4242-4242-424242424204' AS UNIQUEIDENTIFIER), 0, N'Final',       35.00, 4, 1),
        -- College (1): ClassTests + Final
        (CAST('42424242-4242-4242-4242-424242424211' AS UNIQUEIDENTIFIER), 1, N'ClassTests',  40.00, 1, 1),
        (CAST('42424242-4242-4242-4242-424242424212' AS UNIQUEIDENTIFIER), 1, N'Final',       60.00, 2, 1),
        -- University (2): Sessional + Final
        (CAST('42424242-4242-4242-4242-424242424221' AS UNIQUEIDENTIFIER), 2, N'Sessional',   30.00, 1, 1),
        (CAST('42424242-4242-4242-4242-424242424222' AS UNIQUEIDENTIFIER), 2, N'Final',       70.00, 2, 1)
    ) v (Id, InstitutionType, Name, Weightage, DisplayOrder, IsActive)
    WHERE NOT EXISTS (SELECT 1 FROM [result_component_rules] x WHERE x.[Id] = v.Id);
END

-- course_grading_configs
IF OBJECT_ID(N'[course_grading_configs]') IS NOT NULL
BEGIN
    INSERT INTO [course_grading_configs] ([Id], [CourseId], [PassThreshold], [GradingType], [GradeRangesJson], [CreatedAt], [UpdatedAt])
    SELECT CAST(CONCAT('45454545-4545-4545-4545-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY c.Id) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           c.Id,
           CASE WHEN c.GradingType = N'Percentage' THEN 50.00 ELSE 60.00 END,
           c.GradingType,
           CASE WHEN c.GradingType = N'Percentage'
                THEN N'[{"grade":"A+","from":90,"to":100},{"grade":"A","from":80,"to":89},{"grade":"B","from":70,"to":79},{"grade":"C","from":60,"to":69},{"grade":"D","from":50,"to":59},{"grade":"F","from":0,"to":49}]'
                ELSE N'[{"grade":"A","from":85,"to":100},{"grade":"B","from":75,"to":84},{"grade":"C","from":65,"to":74},{"grade":"D","from":60,"to":64},{"grade":"F","from":0,"to":59}]' END,
           @Now, NULL
    FROM [courses] c
    WHERE NOT EXISTS (SELECT 1 FROM [course_grading_configs] x WHERE x.[CourseId] = c.[Id]);
END

-- degree_rules
IF OBJECT_ID(N'[degree_rules]') IS NOT NULL
BEGIN
    INSERT INTO [degree_rules] ([Id], [AcademicProgramId], [MinTotalCredits], [MinCoreCredits], [MinElectiveCredits], [MinGpa], [CreatedAt], [UpdatedAt])
    SELECT CAST(CONCAT('46464646-4646-4646-4646-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY p.Id) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           p.Id,
           CASE WHEN p.TotalSemesters <= 2 THEN 40 WHEN p.TotalSemesters <= 4 THEN 72 ELSE 132 END,
           CASE WHEN p.TotalSemesters <= 2 THEN 24 WHEN p.TotalSemesters <= 4 THEN 48 ELSE 96 END,
           CASE WHEN p.TotalSemesters <= 2 THEN 8  WHEN p.TotalSemesters <= 4 THEN 12 ELSE 24 END,
           2.00, @Now, NULL
    FROM [academic_programs] p
    WHERE NOT EXISTS (SELECT 1 FROM [degree_rules] x WHERE x.[AcademicProgramId] = p.[Id]);
END

PRINT 'Grading configuration seeded.';

-- ==============================================================================
-- SECTION 15: QUIZZES
-- ==============================================================================

IF OBJECT_ID(N'[quizzes]') IS NOT NULL
BEGIN
    INSERT INTO [quizzes] ([Id], [CourseOfferingId], [Title], [Instructions], [TimeLimitMinutes], [MaxAttempts], [IsPublished], [IsActive], [CreatedByUserId], [CreatedAt], [UpdatedAt])
    SELECT CAST(CONCAT('13131313-1313-1313-1313-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY co.Id) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           co.Id, N'Quiz - ' + c.Title, N'Answer all questions. Time limit applies.', 30, 2, 1, 1,
           co.FacultyUserId, @Now, NULL
    FROM [course_offerings] co
    JOIN [courses] c ON c.Id = co.CourseId
    WHERE EXISTS (SELECT 1 FROM [enrollments] e WHERE e.[CourseOfferingId] = co.Id)
      AND NOT EXISTS (SELECT TOP 1 1 FROM [quizzes] q WHERE q.[CourseOfferingId] = co.Id);
END

PRINT 'Quizzes seeded.';

-- ==============================================================================
-- SECTION 16: LMS CONTENT
-- ==============================================================================

IF OBJECT_ID(N'[course_content_modules]') IS NOT NULL
BEGIN
    INSERT INTO [course_content_modules] ([Id], [OfferingId], [Title], [WeekNumber], [Body], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt])
    SELECT CAST(CONCAT('50505050-5050-5050-5050-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY co.Id, w.WeekNum) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           co.Id, N'Week ' + CAST(w.WeekNum AS NVARCHAR) + N' — ' + c.Title,
           w.WeekNum, N'## Overview' + CHAR(13) + CHAR(10) + N'This module covers key concepts and provides reading materials, lecture notes, and exercises.',
           1, @Now, @Now, NULL
    FROM [course_offerings] co
    JOIN [courses] c ON c.Id = co.CourseId
    CROSS APPLY (VALUES (1),(2),(3),(4),(5),(6)) w(WeekNum)
    WHERE NOT EXISTS (SELECT TOP 1 1 FROM [course_content_modules] x WHERE x.[OfferingId] = co.Id AND x.[WeekNumber] = w.WeekNum);
END

PRINT 'LMS modules seeded.';

-- ==============================================================================
-- SECTION 17: SCHOOL STREAMS & STUDENT STREAM ASSIGNMENTS
-- ==============================================================================

IF OBJECT_ID(N'[school_streams]') IS NOT NULL
BEGIN
    INSERT INTO [school_streams] ([Id], [Name], [Description], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT * FROM (VALUES
        (CAST('34343434-3434-3434-3434-343434343401' AS UNIQUEIDENTIFIER), N'Science Stream', N'Physics, Chemistry, Maths, Computer Science for Grades 1-10', 1)
    ) v (Id, Name, Description, IsActive)
    WHERE NOT EXISTS (SELECT 1 FROM [school_streams] x WHERE x.[Id] = v.Id);
END

IF OBJECT_ID(N'[student_stream_assignments]') IS NOT NULL
BEGIN
    INSERT INTO [student_stream_assignments] ([Id], [StudentProfileId], [SchoolStreamId], [AssignedAt], [AssignedByUserId])
    SELECT CAST(CONCAT('35353535-3535-3535-3535-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY sp.Id) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           sp.Id, CAST('34343434-3434-3434-3434-343434343401' AS UNIQUEIDENTIFIER),
           @Now, CAST('66666666-6666-6666-6666-666666666625' AS UNIQUEIDENTIFIER)
    FROM [student_profiles] sp
    WHERE sp.[DepartmentId] = @SchSCIDeptId
      AND NOT EXISTS (SELECT 1 FROM [student_stream_assignments] x WHERE x.[StudentProfileId] = sp.[Id]);
END

PRINT 'School streams seeded.';

-- ==============================================================================
-- SECTION 18: STUDENT REPORT CARDS
-- ==============================================================================

IF OBJECT_ID(N'[student_report_cards]') IS NOT NULL
BEGIN
    INSERT INTO [student_report_cards] ([Id], [StudentProfileId], [InstitutionType], [PeriodLabel], [PayloadJson], [GeneratedByUserId], [GeneratedAt])
    SELECT CAST(CONCAT('39393939-3939-3939-3939-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY sp.Id) AS VARCHAR(12)), 12)) AS UNIQUEIDENTIFIER),
           sp.Id,
           d.InstitutionType,
           CASE WHEN d.InstitutionType = 0 THEN N'Class ' + CAST(sp.CurrentSemesterNumber AS NVARCHAR)
                WHEN d.InstitutionType = 1 THEN N'Year ' + CAST(sp.CurrentSemesterNumber AS NVARCHAR)
                ELSE N'Semester ' + CAST(sp.CurrentSemesterNumber AS NVARCHAR) END,
           N'{"results":[],"summary":{"total":500,"obtained":420,"percentage":84.0,"grade":"A"}}',
           CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), @Now
    FROM [student_profiles] sp
    JOIN [departments] d ON d.Id = sp.DepartmentId
    WHERE NOT EXISTS (SELECT 1 FROM [student_report_cards] x WHERE x.[StudentProfileId] = sp.[Id]);
END

PRINT 'Student report cards seeded.';

-- ==============================================================================
-- SECTION 19: PAYMENTS
-- ==============================================================================

IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
BEGIN
    INSERT INTO [payment_receipts] ([Id], [StudentProfileId], [CreatedByUserId], [ReceiptNo], [Status], [Amount], [Description], [DueDate], [CreatedAt], [UpdatedAt], [IsDeleted])
    SELECT CONVERT(uniqueidentifier, CONCAT('18181818-1818-1818-1818-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY sp.Id) AS VARCHAR(12)), 12))),
           sp.Id, CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER),
           N'RCPT-' + CAST(ROW_NUMBER() OVER (ORDER BY sp.Id) AS NVARCHAR),
           1, 15000.00 + (ABS(CHECKSUM(NEWID())) % 35000),
           N'Tuition fee payment', DATEADD(DAY, 30, @Now), @Now, @Now, 0
    FROM [student_profiles] sp
    WHERE NOT EXISTS (SELECT 1 FROM [payment_receipts] x WHERE x.[StudentProfileId] = sp.[Id]);
END

PRINT 'Payments seeded.';

-- ==============================================================================
-- SECTION 20: NOTIFICATIONS
-- ==============================================================================

IF OBJECT_ID(N'[notifications]') IS NOT NULL
BEGIN
    INSERT INTO [notifications] ([Id], [Title], [Body], [Type], [SenderUserId], [IsSystemGenerated], [IsActive], [CreatedAt], [UpdatedAt])
    SELECT v.Id, v.Title, v.Body, v.Type, v.SenderUserId, v.IsSystemGenerated, v.IsActive, @Now, NULL
    FROM (VALUES
        (CAST('19191919-1919-1919-1919-191919191901' AS UNIQUEIDENTIFIER), N'Welcome to Tabsan EduSphere', N'Your account has been created. Please update your profile.', N'General', NULL, 1, 1),
        (CAST('19191919-1919-1919-1919-191919191902' AS UNIQUEIDENTIFIER), N'Assignment Deadline', N'Your assignment is due in 3 days.', N'Assignment', CAST('77777777-7777-7777-7777-777777777101' AS UNIQUEIDENTIFIER), 1, 1),
        (CAST('19191919-1919-1919-1919-191919191903' AS UNIQUEIDENTIFIER), N'Results Published', N'Your semester results have been published.', N'Result', NULL, 1, 1),
        (CAST('19191919-1919-1919-1919-191919191904' AS UNIQUEIDENTIFIER), N'Attendance Alert', N'Your attendance has dropped below 75%.', N'AttendanceAlert', NULL, 1, 1)
    ) v (Id, Title, Body, Type, SenderUserId, IsSystemGenerated, IsActive)
    WHERE NOT EXISTS (SELECT 1 FROM [notifications] x WHERE x.[Id] = v.Id);
END

-- notification_recipients
IF OBJECT_ID(N'[notification_recipients]') IS NOT NULL
BEGIN
    INSERT INTO [notification_recipients] ([Id], [NotificationId], [RecipientUserId], [IsRead], [ReadAt], [CreatedAt])
    SELECT CONVERT(uniqueidentifier, CONCAT('19191919-1919-1919-1919-', RIGHT('000000000000' + CAST(ROW_NUMBER() OVER (ORDER BY n.Id, u.Id) AS VARCHAR(12)), 12))),
           n.Id, u.Id, CASE WHEN ABS(CHECKSUM(NEWID())) % 3 = 0 THEN 1 ELSE 0 END,
           CASE WHEN ABS(CHECKSUM(NEWID())) % 3 = 0 THEN DATEADD(HOUR, -ABS(CHECKSUM(NEWID())) % 48, @Now) ELSE NULL END, @Now
    FROM [notifications] n
    CROSS JOIN (SELECT TOP 10 Id FROM [users] WHERE [RoleId] = @RoleStudent ORDER BY Id) u
    WHERE NOT EXISTS (SELECT 1 FROM [notification_recipients] x WHERE x.[NotificationId] = n.Id AND x.[RecipientUserId] = u.Id);
END

PRINT 'Notifications seeded.';

-- ==============================================================================
-- SECTION 21: SUPPORT TICKETS
-- ==============================================================================

IF OBJECT_ID(N'[support_tickets]') IS NOT NULL
BEGIN
    INSERT INTO [support_tickets] ([Id], [SubmitterId], [Subject], [Body], [Category], [Status], [ReopenWindowDays], [IsDeleted], [CreatedAt])
    SELECT v.Id, v.SubmitterId, v.Subject, v.Body, v.Category, v.Status, v.ReopenWindowDays, v.IsDeleted, @Now
    FROM (VALUES
        (CAST('40404040-4040-4040-4040-404040404001' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881101' AS UNIQUEIDENTIFIER), N'Unable to view results', N'I cannot see my semester results after they were published.', 1, 0, 7, 0),
        (CAST('40404040-4040-4040-4040-404040404002' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888884101' AS UNIQUEIDENTIFIER), N'Password reset request', N'I forgot my password and need assistance.', 2, 2, 7, 0)
    ) v (Id, SubmitterId, Subject, Body, Category, Status, ReopenWindowDays, IsDeleted)
    WHERE NOT EXISTS (SELECT 1 FROM [support_tickets] x WHERE x.[Id] = v.Id);
END

PRINT 'Support tickets seeded.';

-- ==============================================================================
-- SECTION 22: AUDIT LOGS (200 rows)
-- ==============================================================================

IF OBJECT_ID(N'[audit_logs]') IS NOT NULL AND (SELECT COUNT(1) FROM [audit_logs]) < 50
BEGIN
    ;WITH all_users AS (SELECT TOP 40 Id, ROW_NUMBER() OVER (ORDER BY Id) AS RowNum FROM [users] WHERE [IsDeleted] = 0),
    actions AS (SELECT a.Action, a.EntityName FROM (VALUES
        (N'Create', N'Course'), (N'Update', N'Result'), (N'Publish', N'Result'),
        (N'Login', N'User'), (N'Create', N'Assignment'), (N'Submit', N'Submission')
    ) a(Action, EntityName))
    INSERT INTO [audit_logs] ([ActorUserId], [Action], [EntityName], [EntityId], [OldValuesJson], [NewValuesJson], [OccurredAt], [IpAddress])
    SELECT DISTINCT u.Id, a.Action, a.EntityName,
           CAST(NEWID() AS NVARCHAR(100)),
           N'{"old":"value"}', N'{"new":"value"}',
           DATEADD(MINUTE, -ABS(CHECKSUM(NEWID())) % 43200, @Now),
           CONCAT(N'192.168.1.', ABS(CHECKSUM(NEWID())) % 200)
    FROM all_users u
    CROSS JOIN actions a;
END

PRINT 'Audit logs seeded.';

-- ==============================================================================
-- SECTION 23: ISO 27001 + ISO 9001 COMPLIANCE DATA
-- ==============================================================================

-- login_activity_logs
IF OBJECT_ID(N'[login_activity_logs]') IS NOT NULL AND NOT EXISTS (SELECT TOP 1 1 FROM [login_activity_logs])
BEGIN
    INSERT INTO [login_activity_logs] ([Id], [UserId], [Username], [AttemptedAt], [IpAddress], [UserAgent], [DeviceInfo], [IsSuccess], [FailureReason], [RiskLevel], [UserIsLockedOut])
    VALUES
    (CAST('A1000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), N'superadmin', DATEADD(HOUR, -1, @Now), N'192.168.1.100', N'Mozilla/5.0 Chrome/125.0', N'Windows 11 Pro', 1, NULL, N'low', 0),
    (CAST('A1000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), N'admin.cs',    DATEADD(HOUR, -2, @Now), N'192.168.1.101', N'Mozilla/5.0 Safari/17.5', N'macOS 14.5', 1, NULL, N'low', 0),
    (CAST('A1000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), CAST('77777777-7777-7777-7777-777777777101' AS UNIQUEIDENTIFIER), N'faculty.cs.1',DATEADD(HOUR, -3, @Now), N'192.168.1.102', N'Mozilla/5.0 Firefox/127.0', N'Ubuntu 24.04', 1, NULL, N'low', 0),
    (CAST('A1000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881101' AS UNIQUEIDENTIFIER), N'student.bscs.1',DATEADD(HOUR, -4, @Now), N'192.168.1.103', N'Mozilla/5.0 Mobile/15E148', N'iPhone 16', 1, NULL, N'low', 0),
    (CAST('A1000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER), NULL, N'hacker',     DATEADD(HOUR, -5, @Now), N'10.0.0.55',   N'python-requests/2.31.0', N'Unknown', 0, N'InvalidCredentials', N'high', 0),
    (CAST('A1000000-0000-0000-0000-000000000006' AS UNIQUEIDENTIFIER), CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), N'admin.cs',    DATEADD(HOUR, -6, @Now), N'185.220.101.34', N'Mozilla/5.0 Gecko', N'Windows 10', 0, N'InvalidCredentials', N'high', 0),
    (CAST('A1000000-0000-0000-0000-000000000007' AS UNIQUEIDENTIFIER), CAST('77777777-7777-7777-7777-777777777101' AS UNIQUEIDENTIFIER), N'faculty.cs.1',DATEADD(HOUR, -8, @Now), N'192.168.1.102', N'Mozilla/5.0 Firefox/127.0', N'Ubuntu 24.04', 0, N'InvalidCredentials', N'medium', 0),
    (CAST('A1000000-0000-0000-0000-000000000008' AS UNIQUEIDENTIFIER), CAST('88888888-8888-8888-8888-888888881101' AS UNIQUEIDENTIFIER), N'student.bscs.1',DATEADD(HOUR, -12, @Now), N'192.168.1.103', N'Mozilla/5.0 Mobile/15E148', N'iPhone 16', 0, N'MfaRequired', N'low', 0),
    (CAST('A1000000-0000-0000-0000-000000000009' AS UNIQUEIDENTIFIER), NULL, N'bruteforce',  DATEADD(DAY, -1, @Now), N'45.33.32.156', N'curl/8.6.0', N'Unknown', 0, N'ConcurrencyLimitReached', N'high', 0),
    (CAST('A1000000-0000-0000-0000-000000000010' AS UNIQUEIDENTIFIER), CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), N'admin.cs',    DATEADD(DAY, -2, @Now), N'192.168.1.101', N'Mozilla/5.0 Safari/17.5', N'macOS 14.5', 1, NULL, N'low', 0);
END

-- backup_logs
IF OBJECT_ID(N'[backup_logs]') IS NOT NULL AND NOT EXISTS (SELECT TOP 1 1 FROM [backup_logs])
BEGIN
    INSERT INTO [backup_logs] ([Id], [BackupType], [FileName], [FilePath], [FileSizeBytes], [DurationSeconds], [Status], [StartedAt], [CompletedAt], [ErrorMessage], [Checksum], [InitiatedBy])
    VALUES
    (CAST('B2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'Full', N'Tabsan_FULL_20260604.bak', N'D:\Backups\Daily\', 524288000, 120, N'Completed', DATEADD(DAY, -3, @Now), DATEADD(DAY, -3, DATEADD(SECOND, 120, @Now)), NULL, N'SHA256:A1B2C3D4E5F6...', N'superadmin'),
    (CAST('B2000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'Differential', N'Tabsan_DIFF_20260604.bak', N'D:\Backups\Daily\', 104857600, 45, N'Completed', DATEADD(DAY, -2, @Now), DATEADD(DAY, -2, DATEADD(SECOND, 45, @Now)), NULL, N'SHA256:B2C3D4E5F6A7...', N'admin.cs'),
    (CAST('B2000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'Log', N'Tabsan_LOG_20260604.trn', N'D:\Backups\Hourly\', 20971520, 15, N'Completed', DATEADD(DAY, -1, @Now), DATEADD(DAY, -1, DATEADD(SECOND, 15, @Now)), NULL, N'SHA256:C3D4E5F6A7B8...', N'SQLAgent'),
    (CAST('B2000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), N'Full', N'Tabsan_FULL_20260603.bak', N'D:\Backups\Daily\', 524288000, 118, N'Completed', DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, DATEADD(SECOND, 118, @Now)), NULL, N'SHA256:D4E5F6A7B8C9...', N'SQLAgent'),
    (CAST('B2000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER), N'Full', N'Tabsan_FULL_20260528.bak', N'D:\Backups\Daily\', 524288000, NULL, N'Failed', DATEADD(DAY, -7, @Now), NULL, N'Insufficient disk space. Required: 512MB, Available: 200MB.', NULL, N'SQLAgent');
END

-- data_classification_entries
IF OBJECT_ID(N'[data_classification_entries]') IS NOT NULL AND NOT EXISTS (SELECT TOP 1 1 FROM [data_classification_entries])
BEGIN
    INSERT INTO [data_classification_entries] ([Id], [EntityName], [EntityId], [ClassificationLevel], [ClassifiedBy], [ClassifiedAt], [Justification])
    VALUES
    (CAST('D3000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'StudentProfile', N'*', N'Confidential', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -90, @Now), N'Contains PII per GDPR Article 4(1)'),
    (CAST('D3000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'Result', N'*', N'Confidential', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -90, @Now), N'Academic records with performance data'),
    (CAST('D3000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'PaymentReceipt', N'*', N'Restricted', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -90, @Now), N'Financial transaction data'),
    (CAST('D3000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), N'Course', N'*', N'Internal', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -90, @Now), N'Course materials for internal use'),
    (CAST('D3000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER), N'Department', N'*', N'Public', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -90, @Now), N'Organizational structure — public'),
    (CAST('D3000000-0000-0000-0000-000000000006' AS UNIQUEIDENTIFIER), N'Assignment', N'*', N'Internal', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -90, @Now), N'Assignment content — internal');
END

-- incident_logs
IF OBJECT_ID(N'[incident_logs]') IS NOT NULL AND NOT EXISTS (SELECT TOP 1 1 FROM [incident_logs])
BEGIN
    INSERT INTO [incident_logs] ([Id], [Title], [Description], [Severity], [Category], [Status], [ReportedBy], [ReportedAt], [AssignedTo], [ResolvedAt], [Resolution])
    VALUES
    (CAST('E4000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'Multiple failed logins from suspicious IP', N'5 failed login attempts from IP 185.220.101.34 targeting admin account within 10 minutes.', N'High', N'AccessViolation', N'Resolved', CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), DATEADD(DAY, -5, @Now), CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -4, @Now), N'IP blocked. Rate limiting configured.'),
    (CAST('E4000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'Backup failure — insufficient disk space', N'Full backup failed due to insufficient space.', N'Medium', N'System', N'Investigating', CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), DATEADD(DAY, -7, @Now), CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), NULL, NULL),
    (CAST('E4000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'Suspicious data export by faculty', N'Faculty user exported full student roster with PII outside business hours.', N'High', N'DataLoss', N'Open', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -1, @Now), NULL, NULL, NULL),
    (CAST('E4000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), N'SSL certificate expiring', N'SSL certificate expires in 14 days. Auto-renewal failed.', N'Low', N'Security', N'Resolved', CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), DATEADD(DAY, -14, @Now), CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -12, @Now), N'Manual DNS validation completed. SSL renewed.'),
    (CAST('E4000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER), N'Brute force attack on student accounts', N'Automated attack on 50+ student accounts from IP range 45.33.32.0/24.', N'Critical', N'Breach', N'Resolved', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -10, @Now), CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -10, @Now), N'IP range blocked. All accounts locked and forced password reset.');
END

-- policy_documents
IF OBJECT_ID(N'[policy_documents]') IS NOT NULL AND NOT EXISTS (SELECT TOP 1 1 FROM [policy_documents])
BEGIN
    INSERT INTO [policy_documents] ([Id], [Title], [Description], [Content], [Version], [Status], [Category], [AccessLevel], [PublishedAt], [CreatedAt], [UpdatedAt])
    VALUES
    (CAST('F5000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'Information Security Policy', N'ISO 27001:2022 Annex A controls for Tabsan EduSphere.', N'# Information Security Policy' + CHAR(13)+CHAR(10) + '## Access Control' + CHAR(13)+CHAR(10) + '- RBAC enforced' + CHAR(13)+CHAR(10) + '- MFA required for admin accounts' + CHAR(13)+CHAR(10) + '- Quarterly access reviews', 3, N'Published', N'Security', N'Internal', DATEADD(DAY, -60, @Now), DATEADD(DAY, -90, @Now), DATEADD(DAY, -60, @Now)),
    (CAST('F5000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'Data Protection & Privacy Policy', N'GDPR-compliant data handling procedures.', N'# Data Protection Policy' + CHAR(13)+CHAR(10) + '## Data Retention' + CHAR(13)+CHAR(10) + '- Student records: 7 years post-graduation' + CHAR(13)+CHAR(10) + '- Financial records: 10 years', 2, N'Published', N'Compliance', N'Internal', DATEADD(DAY, -45, @Now), DATEADD(DAY, -80, @Now), DATEADD(DAY, -45, @Now)),
    (CAST('F5000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'Backup & Disaster Recovery Plan', N'DR plan per ISO 27001 A.17.', N'# Backup & DR Plan' + CHAR(13)+CHAR(10) + '## RPO/RTO' + CHAR(13)+CHAR(10) + '- RPO: 15 minutes' + CHAR(13)+CHAR(10) + '- RTO: 4 hours', 1, N'Draft', N'Operations', N'Restricted', NULL, DATEADD(DAY, -30, @Now), DATEADD(DAY, -30, @Now)),
    (CAST('F5000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), N'Academic Integrity Policy', N'Policy for academic honesty per ISO 9001 clause 8.5.', N'# Academic Integrity Policy' + CHAR(13)+CHAR(10) + '## Prohibited Conduct' + CHAR(13)+CHAR(10) + '- Plagiarism' + CHAR(13)+CHAR(10) + '- Cheating on examinations', 1, N'Published', N'Academic', N'Public', DATEADD(DAY, -120, @Now), DATEADD(DAY, -150, @Now), DATEADD(DAY, -120, @Now));
END

-- policy_document_versions
IF OBJECT_ID(N'[policy_document_versions]') IS NOT NULL AND NOT EXISTS (SELECT TOP 1 1 FROM [policy_document_versions])
BEGIN
    INSERT INTO [policy_document_versions] ([Id], [DocumentId], [VersionNumber], [Content], [ChangedBy], [ChangedAt], [ChangeNotes])
    VALUES
    (CAST('F5100000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), CAST('F5000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), 1, N'Initial draft based on ISO 27001:2013.', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -90, @Now), N'Initial draft'),
    (CAST('F5100000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), CAST('F5000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), 2, N'Updated to ISO 27001:2022 controls. Added MFA.', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -75, @Now), N'Updated to ISO 27001:2022'),
    (CAST('F5100000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), CAST('F5000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), 3, N'Added incident response and data classification.', CAST('66666666-6666-6666-6666-666666666621' AS UNIQUEIDENTIFIER), DATEADD(DAY, -60, @Now), N'Added incident response (A.16)'),
    (CAST('F5100000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), CAST('F5000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), 1, N'Initial data protection framework.', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -80, @Now), N'Initial draft'),
    (CAST('F5100000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER), CAST('F5000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), 2, N'Added data retention schedules and breach notification.', CAST('66666666-6666-6666-6666-666666666601' AS UNIQUEIDENTIFIER), DATEADD(DAY, -45, @Now), N'Added retention schedules');
END

-- backup_verification_logs
IF OBJECT_ID(N'[backup_verification_logs]') IS NOT NULL AND NOT EXISTS (SELECT TOP 1 1 FROM [backup_verification_logs])
BEGIN
    INSERT INTO [backup_verification_logs] ([Id], [BackupLogId], [VerificationType], [VerifiedAt], [VerifiedBy], [IsSuccessful], [DurationSeconds], [Issues], [VerifiedChecksum])
    VALUES
    (CAST('06000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), CAST('B2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'IntegrityCheck', DATEADD(DAY, -3, DATEADD(MINUTE, 30, @Now)), N'superadmin', 1, 45, NULL, N'SHA256:A1B2C3D4E5F6...'),
    (CAST('06000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), CAST('B2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER), N'RestoreTest',     DATEADD(DAY, -3, DATEADD(HOUR, 2, @Now)), N'superadmin', 1, 300, NULL, N'SHA256:A1B2C3D4E5F6...'),
    (CAST('06000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), CAST('B2000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER), N'IntegrityCheck', DATEADD(DAY, -2, DATEADD(MINUTE, 15, @Now)), N'admin.cs', 1, 20, NULL, N'SHA256:B2C3D4E5F6A7...'),
    (CAST('06000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), CAST('B2000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER), N'IntegrityCheck', DATEADD(DAY, -1, DATEADD(MINUTE, 5, @Now)), N'SQLAgent', 1, 8, NULL, N'SHA256:C3D4E5F6A7B8...'),
    (CAST('06000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER), CAST('B2000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER), N'IntegrityCheck', DATEADD(DAY, -4, DATEADD(MINUTE, 30, @Now)), N'SQLAgent', 0, 60, N'Checksum mismatch. Backup file may be corrupted.', NULL);
END

PRINT 'ISO compliance data seeded.';

-- ==============================================================================
-- SECTION 24: FINAL SYNC PASSES
-- ==============================================================================

-- Sync TenantId/CampusId on users who have DepartmentId
UPDATE u SET u.[TenantId] = d.[TenantId], u.[CampusId] = d.[CampusId],
    u.[InstitutionType] = d.[InstitutionType], u.[UpdatedAt] = @Now
FROM [users] u
JOIN [departments] d ON d.[Id] = u.[DepartmentId]
WHERE u.[DepartmentId] IS NOT NULL
  AND (u.[TenantId] IS NULL OR u.[TenantId] <> d.[TenantId]);

-- Reactivate all demo users and reset password
UPDATE [users] SET [PasswordHash] = @PwdHash, [IsActive] = 1, [IsLockedOut] = 0,
    [FailedLoginAttempts] = 0, [MustChangePassword] = 0, [UpdatedAt] = @Now
WHERE [IsDeleted] = 0;

-- Update ISO columns on users if schema supports them
IF COL_LENGTH('users', 'LastPasswordChangedAt') IS NOT NULL
    UPDATE [users] SET [LastPasswordChangedAt] = DATEADD(DAY, -30, @Now), [UpdatedAt] = @Now
    WHERE [IsDeleted] = 0 AND [LastPasswordChangedAt] IS NULL;

IF COL_LENGTH('users', 'ConsentToMonitoring') IS NOT NULL
    UPDATE [users] SET [ConsentToMonitoring] = 1, [UpdatedAt] = @Now
    WHERE [IsDeleted] = 0 AND [ConsentToMonitoring] IS NULL;

IF COL_LENGTH('users', 'DataRetentionDate') IS NOT NULL
    UPDATE [users] SET [DataRetentionDate] = DATEADD(YEAR, 7, @Now), [UpdatedAt] = @Now
    WHERE [IsDeleted] = 0 AND [DataRetentionDate] IS NULL;

PRINT 'Final sync passes completed.';

-- ==============================================================================
-- COMMIT
-- ==============================================================================

PRINT 'Full dummy demo data seeding completed successfully.';
COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    DECLARE @ErrorLine INT = ERROR_LINE();

    RAISERROR (N'Line %d: %s', @ErrorSeverity, @ErrorState, @ErrorLine, @ErrorMessage);
END CATCH;
