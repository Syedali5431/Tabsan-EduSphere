SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Tabsan EduSphere — Comprehensive Demo Data v3.0
  =================================================================
  3 institutions × 10 departments × 50 courses × 60+ offerings
  ~135 users, 70+ students, properly scoped enrollments.
  All INSERTs are idempotent (WHERE NOT EXISTS).

  Institution: University (InstitutionType=2): CS, Business, Engineering, Arts
  Institution: College    (InstitutionType=1): Science, Commerce, Humanities
  Institution: School     (InstitutionType=0): Primary, Middle, Secondary
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL BEGIN RAISERROR('Run 01-Schema-Current.sql first.',16,1); RETURN; END;
GO
USE [Tabsan-EduSphere];
GO

DECLARE @Now       DATETIME2      = SYSUTCDATETIME();
DECLARE @SAPwdHash NVARCHAR(512)  = N'argon2id:kot3aIW+GTcmK4Ji/jGD7BxrNOEh57PLaFMUZrZa5oM=:v+XYusZ0Eu9Xs8Sz/7Hi58z4SrS9KsJ/ynnr/iCkkSk=';
DECLARE @PwdHash   NVARCHAR(512)  = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';

/* ── Role IDs ── */
DECLARE @RoleSA      INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name]=N'SuperAdmin');
DECLARE @RoleAdmin   INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name]=N'Admin');
DECLARE @RoleFaculty INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name]=N'Faculty');
DECLARE @RoleStudent INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name]=N'Student');
DECLARE @RoleFinance INT = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name]=N'Finance');

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 0: TENANTS & CAMPUSES (3 each)
   ══════════════════════════════════════════════════════════════════════════════ */
DECLARE @UniTenantId UNIQUEIDENTIFIER = CAST('10000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @ColTenantId UNIQUEIDENTIFIER = CAST('10000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchTenantId UNIQUEIDENTIFIER = CAST('10000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);
DECLARE @UniCampusId UNIQUEIDENTIFIER = CAST('20000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @ColCampusId UNIQUEIDENTIFIER = CAST('20000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @SchCampusId UNIQUEIDENTIFIER = CAST('20000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);

INSERT INTO [tenants] ([Id],[Code],[Name],[IsActive],[IsDeleted],[CreatedAt])
SELECT t.Id,t.Code,t.Name,1,0,@Now FROM (VALUES
 (@UniTenantId,N'UNI',N'University Tenant'),
 (@ColTenantId,N'COL',N'College Tenant'),
 (@SchTenantId,N'SCH',N'School Tenant')) t(Id,Code,Name)
WHERE NOT EXISTS(SELECT 1 FROM [tenants] x WHERE x.[Id]=t.Id OR x.[Code]=t.Code);

INSERT INTO [campuses] ([Id],[TenantId],[Code],[Name],[IsActive],[IsDeleted],[CreatedAt])
SELECT c.Id,c.TenantId,c.Code,c.Name,1,0,@Now FROM (VALUES
 (@UniCampusId,@UniTenantId,N'UNI-MAIN',N'University Main Campus'),
 (@ColCampusId,@ColTenantId,N'COL-MAIN',N'College Main Campus'),
 (@SchCampusId,@SchTenantId,N'SCH-MAIN',N'School Main Campus')) c(Id,TenantId,Code,Name)
WHERE NOT EXISTS(SELECT 1 FROM [campuses] x WHERE x.[Id]=c.Id OR x.[Code]=c.Code);

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 1: DEPARTMENTS (10 across 3 institutions)
   ══════════════════════════════════════════════════════════════════════════════ */
DECLARE @UniCSDept   UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
DECLARE @UniBUSDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
DECLARE @UniENGDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);
DECLARE @UniARTSDept UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER);
DECLARE @ColSCIDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER);
DECLARE @ColCOMDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000006' AS UNIQUEIDENTIFIER);
DECLARE @ColHUMDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000007' AS UNIQUEIDENTIFIER);
DECLARE @SchPRIDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000008' AS UNIQUEIDENTIFIER);
DECLARE @SchMIDDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000009' AS UNIQUEIDENTIFIER);
DECLARE @SchSECDept  UNIQUEIDENTIFIER = CAST('31000000-0000-0000-0000-000000000010' AS UNIQUEIDENTIFIER);

INSERT INTO [departments] ([Id],[Name],[Code],[InstitutionType],[TenantId],[CampusId],[IsActive],[IsDeleted],[CreatedAt])
SELECT d.Id,d.Name,d.Code,d.InstitutionType,d.TenantId,d.CampusId,1,0,@Now FROM (VALUES
 (@UniCSDept,  N'School of Computer Science',     N'CS',   2,@UniTenantId,@UniCampusId),
 (@UniBUSDept, N'School of Business',             N'BUS',  2,@UniTenantId,@UniCampusId),
 (@UniENGDept, N'School of Engineering',          N'ENG',  2,@UniTenantId,@UniCampusId),
 (@UniARTSDept,N'School of Arts & Humanities',    N'ARTS', 2,@UniTenantId,@UniCampusId),
 (@ColSCIDept, N'Science College',                N'SCI',  1,@ColTenantId,@ColCampusId),
 (@ColCOMDept, N'Commerce College',               N'COM',  1,@ColTenantId,@ColCampusId),
 (@ColHUMDept, N'Humanities College',             N'HUM',  1,@ColTenantId,@ColCampusId),
 (@SchPRIDept, N'Primary Section (Class 1-5)',    N'PRI',  0,@SchTenantId,@SchCampusId),
 (@SchMIDDept, N'Middle Section (Class 6-8)',     N'MID',  0,@SchTenantId,@SchCampusId),
 (@SchSECDept, N'Secondary Section (Class 9-10)', N'SEC',  0,@SchTenantId,@SchCampusId))
 d(Id,Name,Code,InstitutionType,TenantId,CampusId)
WHERE NOT EXISTS(SELECT 1 FROM [departments] x WHERE x.[Id]=d.Id);

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 2: ACADEMIC PROGRAMS (1 per department)
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [academic_programs] ([Id],[Name],[Code],[DepartmentId],[TotalSemesters],[MaxCreditLoadPerSemester],[IsActive],[IsDeleted],[CreatedAt])
SELECT p.Id,p.Name,p.Code,p.DepartmentId,p.TotalSemesters,p.MaxCreditLoadPerSemester,1,0,@Now FROM (VALUES
 (CAST('41000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER),N'BS Computer Science',       N'BSCS',   @UniCSDept, 8,18),
 (CAST('41000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER),N'Bachelor of Business Admin',N'BBA',    @UniBUSDept,8,18),
 (CAST('41000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER),N'BS Electrical Engineering',  N'BSEE',   @UniENGDept,8,18),
 (CAST('41000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER),N'BA English Literature',     N'BA-ENG', @UniARTSDept,4,15),
 (CAST('41000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER),N'FSc Pre-Medical',           N'FSC-MED',@ColSCIDept,2,12),
 (CAST('41000000-0000-0000-0000-000000000006' AS UNIQUEIDENTIFIER),N'ICom',                      N'ICOM',   @ColCOMDept,2,12),
 (CAST('41000000-0000-0000-0000-000000000007' AS UNIQUEIDENTIFIER),N'FA General Arts',           N'FA',     @ColHUMDept,2,12),
 (CAST('41000000-0000-0000-0000-000000000008' AS UNIQUEIDENTIFIER),N'Primary School Certificate',N'SCH-PRI',@SchPRIDept,5,8),
 (CAST('41000000-0000-0000-0000-000000000009' AS UNIQUEIDENTIFIER),N'Middle School Certificate', N'SCH-MID',@SchMIDDept,3,8),
 (CAST('41000000-0000-0000-0000-000000000010' AS UNIQUEIDENTIFIER),N'Secondary School Certificate',N'SCH-SEC',@SchSECDept,2,8))
 p(Id,Name,Code,DepartmentId,TotalSemesters,MaxCreditLoadPerSemester)
WHERE NOT EXISTS(SELECT 1 FROM [academic_programs] x WHERE x.[Id]=p.Id);

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 3: SEMESTERS (8)
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [semesters] ([Id],[Name],[StartDate],[EndDate],[IsClosed],[IsDeleted],[CreatedAt])
SELECT s.Id,s.Name,s.StartDate,s.EndDate,s.IsClosed,0,@Now FROM (VALUES
 (CAST('51000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER),N'Fall 2025',  '2025-09-01','2026-01-15',1),
 (CAST('51000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER),N'Spring 2026','2026-02-01','2026-06-15',1),
 (CAST('51000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER),N'Fall 2026',  '2026-09-01','2027-01-15',0),
 (CAST('51000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER),N'Spring 2027','2027-02-01','2027-06-15',0),
 (CAST('51000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER),N'Fall 2027',  '2027-09-01','2028-01-15',0),
 (CAST('51000000-0000-0000-0000-000000000006' AS UNIQUEIDENTIFIER),N'Spring 2028','2028-02-01','2028-06-15',0),
 (CAST('51000000-0000-0000-0000-000000000007' AS UNIQUEIDENTIFIER),N'Fall 2028',  '2028-09-01','2029-01-15',0),
 (CAST('51000000-0000-0000-0000-000000000008' AS UNIQUEIDENTIFIER),N'Spring 2029','2029-02-01','2029-06-15',0))
 s(Id,Name,StartDate,EndDate,IsClosed)
WHERE NOT EXISTS(SELECT 1 FROM [semesters] x WHERE x.[Id]=s.Id);

DECLARE @SemFall2026  UNIQUEIDENTIFIER = CAST('51000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);
DECLARE @SemSpring2027 UNIQUEIDENTIFIER = CAST('51000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER);

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 4: COURSES (5 per department = 50)
   ══════════════════════════════════════════════════════════════════════════════ */
DECLARE @Crs TABLE (Id UNIQUEIDENTIFIER, Title NVARCHAR(200), Code NVARCHAR(20), Cr INT, DeptId UNIQUEIDENTIFIER, CT INT, GT NVARCHAR(20));
INSERT INTO @Crs VALUES
 (CAST('61000000-0000-0000-0000-000000001001' AS UNIQUEIDENTIFIER),N'Programming Fundamentals',   N'CS-101',3,@UniCSDept,  1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000001002' AS UNIQUEIDENTIFIER),N'Data Structures & Algorithms',N'CS-201',4,@UniCSDept,  1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000001003' AS UNIQUEIDENTIFIER),N'Database Systems',            N'CS-301',4,@UniCSDept,  1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000001004' AS UNIQUEIDENTIFIER),N'Software Engineering',        N'CS-401',3,@UniCSDept,  1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000001005' AS UNIQUEIDENTIFIER),N'Artificial Intelligence',     N'CS-501',3,@UniCSDept,  1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000002001' AS UNIQUEIDENTIFIER),N'Principles of Management',    N'BUS-101',3,@UniBUSDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000002002' AS UNIQUEIDENTIFIER),N'Financial Accounting',        N'BUS-201',4,@UniBUSDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000002003' AS UNIQUEIDENTIFIER),N'Marketing Management',        N'BUS-301',3,@UniBUSDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000002004' AS UNIQUEIDENTIFIER),N'Organizational Behavior',     N'BUS-401',3,@UniBUSDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000002005' AS UNIQUEIDENTIFIER),N'Business Analytics',          N'BUS-501',3,@UniBUSDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000003001' AS UNIQUEIDENTIFIER),N'Circuit Analysis',            N'ENG-101',4,@UniENGDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000003002' AS UNIQUEIDENTIFIER),N'Digital Logic Design',        N'ENG-201',4,@UniENGDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000003003' AS UNIQUEIDENTIFIER),N'Signals & Systems',           N'ENG-301',3,@UniENGDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000003004' AS UNIQUEIDENTIFIER),N'Microprocessors',             N'ENG-401',4,@UniENGDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000003005' AS UNIQUEIDENTIFIER),N'Power Systems',               N'ENG-501',3,@UniENGDept, 1,N'GPA'),
 (CAST('61000000-0000-0000-0000-000000004001' AS UNIQUEIDENTIFIER),N'English Literature I',        N'ARTS-101',3,@UniARTSDept,1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000004002' AS UNIQUEIDENTIFIER),N'Creative Writing',            N'ARTS-201',3,@UniARTSDept,2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000004003' AS UNIQUEIDENTIFIER),N'Linguistics',                 N'ARTS-301',3,@UniARTSDept,1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000004004' AS UNIQUEIDENTIFIER),N'Philosophy',                  N'ARTS-401',3,@UniARTSDept,2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000004005' AS UNIQUEIDENTIFIER),N'World History',               N'ARTS-501',3,@UniARTSDept,2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000005001' AS UNIQUEIDENTIFIER),N'Physics',                     N'SCI-101',4,@ColSCIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000005002' AS UNIQUEIDENTIFIER),N'Chemistry',                   N'SCI-201',4,@ColSCIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000005003' AS UNIQUEIDENTIFIER),N'Biology',                     N'SCI-301',4,@ColSCIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000005004' AS UNIQUEIDENTIFIER),N'Mathematics',                 N'SCI-401',3,@ColSCIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000005005' AS UNIQUEIDENTIFIER),N'Computer Science',            N'SCI-501',3,@ColSCIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000006001' AS UNIQUEIDENTIFIER),N'Accounting',                  N'COM-101',4,@ColCOMDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000006002' AS UNIQUEIDENTIFIER),N'Business Studies',            N'COM-201',4,@ColCOMDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000006003' AS UNIQUEIDENTIFIER),N'Economics',                   N'COM-301',4,@ColCOMDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000006004' AS UNIQUEIDENTIFIER),N'Banking & Finance',           N'COM-401',3,@ColCOMDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000006005' AS UNIQUEIDENTIFIER),N'Statistics',                  N'COM-501',3,@ColCOMDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000007001' AS UNIQUEIDENTIFIER),N'English Compulsory',          N'HUM-101',3,@ColHUMDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000007002' AS UNIQUEIDENTIFIER),N'Urdu Compulsory',             N'HUM-201',3,@ColHUMDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000007003' AS UNIQUEIDENTIFIER),N'Islamic Studies',             N'HUM-301',3,@ColHUMDept, 2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000007004' AS UNIQUEIDENTIFIER),N'Pakistan Studies',            N'HUM-401',3,@ColHUMDept, 2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000007005' AS UNIQUEIDENTIFIER),N'Sociology',                   N'HUM-501',3,@ColHUMDept, 2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000008001' AS UNIQUEIDENTIFIER),N'English (Primary)',           N'PRI-101',4,@SchPRIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000008002' AS UNIQUEIDENTIFIER),N'Mathematics (Primary)',       N'PRI-201',4,@SchPRIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000008003' AS UNIQUEIDENTIFIER),N'Science (Primary)',           N'PRI-301',4,@SchPRIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000008004' AS UNIQUEIDENTIFIER),N'Urdu (Primary)',              N'PRI-401',4,@SchPRIDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000008005' AS UNIQUEIDENTIFIER),N'General Knowledge',           N'PRI-501',3,@SchPRIDept, 2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000009001' AS UNIQUEIDENTIFIER),N'English (Middle)',            N'MID-101',4,@SchMIDDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000009002' AS UNIQUEIDENTIFIER),N'Mathematics (Middle)',        N'MID-201',4,@SchMIDDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000009003' AS UNIQUEIDENTIFIER),N'Science (Middle)',            N'MID-301',4,@SchMIDDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000009004' AS UNIQUEIDENTIFIER),N'Computer Science (Middle)',   N'MID-401',3,@SchMIDDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000009005' AS UNIQUEIDENTIFIER),N'History & Geography',         N'MID-501',3,@SchMIDDept, 2,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000010001' AS UNIQUEIDENTIFIER),N'English (Secondary)',         N'SEC-101',4,@SchSECDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000010002' AS UNIQUEIDENTIFIER),N'Mathematics (Secondary)',     N'SEC-201',4,@SchSECDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000010003' AS UNIQUEIDENTIFIER),N'Physics (Secondary)',         N'SEC-301',4,@SchSECDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000010004' AS UNIQUEIDENTIFIER),N'Chemistry (Secondary)',       N'SEC-401',4,@SchSECDept, 1,N'Percentage'),
 (CAST('61000000-0000-0000-0000-000000010005' AS UNIQUEIDENTIFIER),N'Biology (Secondary)',         N'SEC-501',4,@SchSECDept, 1,N'Percentage');

INSERT INTO [courses] ([Id],[Title],[Code],[CreditHours],[DepartmentId],[CourseType],[GradingType],[HasSemesters],[IsActive],[IsDeleted],[CreatedAt])
SELECT c.Id,c.Title,c.Code,c.Cr,c.DeptId,c.CT,c.GT,1,1,0,@Now FROM @Crs c
WHERE NOT EXISTS(SELECT 1 FROM [courses] x WHERE x.[Id]=c.Id);

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 5: USERS (~130 total: 1 SA + 10 admin + 3 finance + 35 faculty + 70 students + 6 parent)
   ══════════════════════════════════════════════════════════════════════════════ */
DECLARE @SAId  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);

-- Admin user IDs (1 per department)
DECLARE @AdmCS   UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001001' AS UNIQUEIDENTIFIER);
DECLARE @AdmBUS  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001002' AS UNIQUEIDENTIFIER);
DECLARE @AdmENG  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001003' AS UNIQUEIDENTIFIER);
DECLARE @AdmARTS UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001004' AS UNIQUEIDENTIFIER);
DECLARE @AdmSCI  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001005' AS UNIQUEIDENTIFIER);
DECLARE @AdmCOM  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001006' AS UNIQUEIDENTIFIER);
DECLARE @AdmHUM  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001007' AS UNIQUEIDENTIFIER);
DECLARE @AdmPRI  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001008' AS UNIQUEIDENTIFIER);
DECLARE @AdmMID  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001009' AS UNIQUEIDENTIFIER);
DECLARE @AdmSEC  UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000001010' AS UNIQUEIDENTIFIER);

-- Finance user IDs (1 per institution)
DECLARE @FinUNI UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000003001' AS UNIQUEIDENTIFIER);
DECLARE @FinCOL UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000003002' AS UNIQUEIDENTIFIER);
DECLARE @FinSCH UNIQUEIDENTIFIER = CAST('70000000-0000-0000-0000-000000003003' AS UNIQUEIDENTIFIER);

INSERT INTO [users] ([Id],[Username],[Email],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[FullName],[IsActive],[IsDeleted],[CreatedAt])
SELECT u.Id,u.Username,u.Email,u.Pwd,u.RoleId,u.DeptId,u.TenantId,u.CampusId,u.InstType,u.FullName,1,0,@Now FROM (VALUES
 (@SAId,     N'superadmin',  N'superadmin@tabsan.local',  @SAPwdHash,@RoleSA,     NULL,         NULL,         NULL,         NULL,N'System Super Administrator'),
 (@AdmCS,   N'admin.cs',    N'admin.cs@demo.local',      @PwdHash,  @RoleAdmin,  @UniCSDept,   @UniTenantId, @UniCampusId, 2,   N'CS Department Admin'),
 (@AdmBUS,  N'admin.bus',   N'admin.bus@demo.local',     @PwdHash,  @RoleAdmin,  @UniBUSDept,  @UniTenantId, @UniCampusId, 2,   N'Business Department Admin'),
 (@AdmENG,  N'admin.eng',   N'admin.eng@demo.local',     @PwdHash,  @RoleAdmin,  @UniENGDept,  @UniTenantId, @UniCampusId, 2,   N'Engineering Department Admin'),
 (@AdmARTS, N'admin.arts',  N'admin.arts@demo.local',    @PwdHash,  @RoleAdmin,  @UniARTSDept, @UniTenantId, @UniCampusId, 2,   N'Arts Department Admin'),
 (@AdmSCI,  N'admin.sci',   N'admin.sci@demo.local',     @PwdHash,  @RoleAdmin,  @ColSCIDept,  @ColTenantId, @ColCampusId, 1,   N'Science College Admin'),
 (@AdmCOM,  N'admin.com',   N'admin.com@demo.local',     @PwdHash,  @RoleAdmin,  @ColCOMDept,  @ColTenantId, @ColCampusId, 1,   N'Commerce College Admin'),
 (@AdmHUM,  N'admin.hum',   N'admin.hum@demo.local',     @PwdHash,  @RoleAdmin,  @ColHUMDept,  @ColTenantId, @ColCampusId, 1,   N'Humanities College Admin'),
 (@AdmPRI,  N'admin.pri',   N'admin.pri@demo.local',     @PwdHash,  @RoleAdmin,  @SchPRIDept,  @SchTenantId, @SchCampusId, 0,   N'Primary Section Admin'),
 (@AdmMID,  N'admin.mid',   N'admin.mid@demo.local',     @PwdHash,  @RoleAdmin,  @SchMIDDept,  @SchTenantId, @SchCampusId, 0,   N'Middle Section Admin'),
 (@AdmSEC,  N'admin.sec',   N'admin.sec@demo.local',     @PwdHash,  @RoleAdmin,  @SchSECDept,  @SchTenantId, @SchCampusId, 0,   N'Secondary Section Admin'),
 (@FinUNI,  N'finance.uni', N'finance.uni@demo.local',   @PwdHash,  @RoleFinance,@UniCSDept,   @UniTenantId, @UniCampusId, 2,   N'University Finance Officer'),
 (@FinCOL,  N'finance.col', N'finance.col@demo.local',   @PwdHash,  @RoleFinance,@ColSCIDept,  @ColTenantId, @ColCampusId, 1,   N'College Finance Officer'),
 (@FinSCH,  N'finance.sch', N'finance.sch@demo.local',   @PwdHash,  @RoleFinance,@SchPRIDept,  @SchTenantId, @SchCampusId, 0,   N'School Finance Officer'))
 u(Id,Username,Email,Pwd,RoleId,DeptId,TenantId,CampusId,InstType,FullName)
WHERE NOT EXISTS(SELECT 1 FROM [users] x WHERE x.[Id]=u.Id OR x.[Email]=u.Email);

/* Faculty users */
INSERT INTO [users] ([Id],[Username],[Email],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[FullName],[IsActive],[IsDeleted],[CreatedAt])
SELECT u.Id,u.Username,u.Email,@PwdHash,@RoleFaculty,u.DeptId,u.TenantId,u.CampusId,u.InstType,u.FullName,1,0,@Now FROM (VALUES
 (CAST('70000000-0000-0000-0000-000000002001' AS UNIQUEIDENTIFIER),N'fac.cs.1', N'fac.cs.1@demo.local',  @UniCSDept, @UniTenantId,@UniCampusId,2,N'Dr. Ahmad Khan'),
 (CAST('70000000-0000-0000-0000-000000002002' AS UNIQUEIDENTIFIER),N'fac.cs.2', N'fac.cs.2@demo.local',  @UniCSDept, @UniTenantId,@UniCampusId,2,N'Dr. Sara Ali'),
 (CAST('70000000-0000-0000-0000-000000002003' AS UNIQUEIDENTIFIER),N'fac.cs.3', N'fac.cs.3@demo.local',  @UniCSDept, @UniTenantId,@UniCampusId,2,N'Prof. Bilal Siddiqui'),
 (CAST('70000000-0000-0000-0000-000000002004' AS UNIQUEIDENTIFIER),N'fac.bus.1',N'fac.bus.1@demo.local', @UniBUSDept,@UniTenantId,@UniCampusId,2,N'Dr. Fatima Noor'),
 (CAST('70000000-0000-0000-0000-000000002005' AS UNIQUEIDENTIFIER),N'fac.bus.2',N'fac.bus.2@demo.local', @UniBUSDept,@UniTenantId,@UniCampusId,2,N'Prof. Imran Qureshi'),
 (CAST('70000000-0000-0000-0000-000000002006' AS UNIQUEIDENTIFIER),N'fac.bus.3',N'fac.bus.3@demo.local', @UniBUSDept,@UniTenantId,@UniCampusId,2,N'Dr. Zainab Haider'),
 (CAST('70000000-0000-0000-0000-000000002007' AS UNIQUEIDENTIFIER),N'fac.eng.1',N'fac.eng.1@demo.local', @UniENGDept,@UniTenantId,@UniCampusId,2,N'Engr. Hassan Raza'),
 (CAST('70000000-0000-0000-0000-000000002008' AS UNIQUEIDENTIFIER),N'fac.eng.2',N'fac.eng.2@demo.local', @UniENGDept,@UniTenantId,@UniCampusId,2,N'Engr. Nadia Sheikh'),
 (CAST('70000000-0000-0000-0000-000000002009' AS UNIQUEIDENTIFIER),N'fac.eng.3',N'fac.eng.3@demo.local', @UniENGDept,@UniTenantId,@UniCampusId,2,N'Dr. Tariq Mahmood'),
 (CAST('70000000-0000-0000-0000-000000002010' AS UNIQUEIDENTIFIER),N'fac.arts.1',N'fac.arts.1@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Prof. Ayesha Malik'),
 (CAST('70000000-0000-0000-0000-000000002011' AS UNIQUEIDENTIFIER),N'fac.arts.2',N'fac.arts.2@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Dr. Omar Farooq'),
 (CAST('70000000-0000-0000-0000-000000002012' AS UNIQUEIDENTIFIER),N'fac.arts.3',N'fac.arts.3@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Prof. Laila Hassan'),
 (CAST('70000000-0000-0000-0000-000000002013' AS UNIQUEIDENTIFIER),N'fac.sci.1',N'fac.sci.1@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Prof. Rashid Iqbal'),
 (CAST('70000000-0000-0000-0000-000000002014' AS UNIQUEIDENTIFIER),N'fac.sci.2',N'fac.sci.2@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Dr. Sana Tariq'),
 (CAST('70000000-0000-0000-0000-000000002015' AS UNIQUEIDENTIFIER),N'fac.sci.3',N'fac.sci.3@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Prof. Kamran Ali'),
 (CAST('70000000-0000-0000-0000-000000002016' AS UNIQUEIDENTIFIER),N'fac.com.1',N'fac.com.1@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Prof. Naveed Akram'),
 (CAST('70000000-0000-0000-0000-000000002017' AS UNIQUEIDENTIFIER),N'fac.com.2',N'fac.com.2@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Dr. Hina Riaz'),
 (CAST('70000000-0000-0000-0000-000000002018' AS UNIQUEIDENTIFIER),N'fac.com.3',N'fac.com.3@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Prof. Usman Ghani'),
 (CAST('70000000-0000-0000-0000-000000002019' AS UNIQUEIDENTIFIER),N'fac.hum.1',N'fac.hum.1@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Prof. Rabia Khan'),
 (CAST('70000000-0000-0000-0000-000000002020' AS UNIQUEIDENTIFIER),N'fac.hum.2',N'fac.hum.2@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Dr. Asif Mehmood'),
 (CAST('70000000-0000-0000-0000-000000002021' AS UNIQUEIDENTIFIER),N'fac.hum.3',N'fac.hum.3@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Prof. Maria Shah'),
 (CAST('70000000-0000-0000-0000-000000002022' AS UNIQUEIDENTIFIER),N'fac.pri.1',N'fac.pri.1@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Ms. Amna Javed'),
 (CAST('70000000-0000-0000-0000-000000002023' AS UNIQUEIDENTIFIER),N'fac.pri.2',N'fac.pri.2@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Ms. Saima Yousaf'),
 (CAST('70000000-0000-0000-0000-000000002024' AS UNIQUEIDENTIFIER),N'fac.pri.3',N'fac.pri.3@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Mr. Farhan Ali'),
 (CAST('70000000-0000-0000-0000-000000002025' AS UNIQUEIDENTIFIER),N'fac.pri.4',N'fac.pri.4@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Ms. Zara Ahmed'),
 (CAST('70000000-0000-0000-0000-000000002026' AS UNIQUEIDENTIFIER),N'fac.mid.1',N'fac.mid.1@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Mr. Talha Hussain'),
 (CAST('70000000-0000-0000-0000-000000002027' AS UNIQUEIDENTIFIER),N'fac.mid.2',N'fac.mid.2@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Ms. Komal Rizvi'),
 (CAST('70000000-0000-0000-0000-000000002028' AS UNIQUEIDENTIFIER),N'fac.mid.3',N'fac.mid.3@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Mr. Adeel Shah'),
 (CAST('70000000-0000-0000-0000-000000002029' AS UNIQUEIDENTIFIER),N'fac.mid.4',N'fac.mid.4@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Ms. Noreen Akhtar'),
 (CAST('70000000-0000-0000-0000-000000002030' AS UNIQUEIDENTIFIER),N'fac.sec.1',N'fac.sec.1@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Mr. Waqas Ahmed'),
 (CAST('70000000-0000-0000-0000-000000002031' AS UNIQUEIDENTIFIER),N'fac.sec.2',N'fac.sec.2@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Ms. Sadaf Batool'),
 (CAST('70000000-0000-0000-0000-000000002032' AS UNIQUEIDENTIFIER),N'fac.sec.3',N'fac.sec.3@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Mr. Hamza Siddique'),
 (CAST('70000000-0000-0000-0000-000000002033' AS UNIQUEIDENTIFIER),N'fac.sec.4',N'fac.sec.4@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Ms. Areeba Nasir'))
 u(Id,Username,Email,DeptId,TenantId,CampusId,InstType,FullName)
WHERE NOT EXISTS(SELECT 1 FROM [users] x WHERE x.[Id]=u.Id OR x.[Email]=u.Email);

/* ── Student users (6-8 per department = 70) ── */
INSERT INTO [users] ([Id],[Username],[Email],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[FullName],[IsActive],[IsDeleted],[CreatedAt])
SELECT u.Id,u.Username,u.Email,@PwdHash,@RoleStudent,u.DeptId,u.TenantId,u.CampusId,u.InstType,u.FullName,1,0,@Now FROM (VALUES
 (CAST('70000000-0000-0000-0100-000000000001' AS UNIQUEIDENTIFIER),N'stu.cs.1', N'stu.cs.1@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Ali Hassan'),
 (CAST('70000000-0000-0000-0100-000000000002' AS UNIQUEIDENTIFIER),N'stu.cs.2', N'stu.cs.2@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Sana Tariq'),
 (CAST('70000000-0000-0000-0100-000000000003' AS UNIQUEIDENTIFIER),N'stu.cs.3', N'stu.cs.3@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Bilal Ahmed'),
 (CAST('70000000-0000-0000-0100-000000000004' AS UNIQUEIDENTIFIER),N'stu.cs.4', N'stu.cs.4@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Ayesha Khan'),
 (CAST('70000000-0000-0000-0100-000000000005' AS UNIQUEIDENTIFIER),N'stu.cs.5', N'stu.cs.5@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Hamza Riaz'),
 (CAST('70000000-0000-0000-0100-000000000006' AS UNIQUEIDENTIFIER),N'stu.cs.6', N'stu.cs.6@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Fatima Zahra'),
 (CAST('70000000-0000-0000-0100-000000000007' AS UNIQUEIDENTIFIER),N'stu.cs.7', N'stu.cs.7@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Usman Khalid'),
 (CAST('70000000-0000-0000-0100-000000000008' AS UNIQUEIDENTIFIER),N'stu.cs.8', N'stu.cs.8@demo.local', @UniCSDept, @UniTenantId,@UniCampusId,2,N'Zainab Noor'),
 (CAST('70000000-0000-0000-0100-000000000009' AS UNIQUEIDENTIFIER),N'stu.bus.1',N'stu.bus.1@demo.local',@UniBUSDept,@UniTenantId,@UniCampusId,2,N'Kamran Saeed'),
 (CAST('70000000-0000-0000-0100-000000000010' AS UNIQUEIDENTIFIER),N'stu.bus.2',N'stu.bus.2@demo.local',@UniBUSDept,@UniTenantId,@UniCampusId,2,N'Nadia Amir'),
 (CAST('70000000-0000-0000-0100-000000000011' AS UNIQUEIDENTIFIER),N'stu.bus.3',N'stu.bus.3@demo.local',@UniBUSDept,@UniTenantId,@UniCampusId,2,N'Rizwan Haider'),
 (CAST('70000000-0000-0000-0100-000000000012' AS UNIQUEIDENTIFIER),N'stu.bus.4',N'stu.bus.4@demo.local',@UniBUSDept,@UniTenantId,@UniCampusId,2,N'Mehwish Iqbal'),
 (CAST('70000000-0000-0000-0100-000000000013' AS UNIQUEIDENTIFIER),N'stu.bus.5',N'stu.bus.5@demo.local',@UniBUSDept,@UniTenantId,@UniCampusId,2,N'Danish Latif'),
 (CAST('70000000-0000-0000-0100-000000000014' AS UNIQUEIDENTIFIER),N'stu.bus.6',N'stu.bus.6@demo.local',@UniBUSDept,@UniTenantId,@UniCampusId,2,N'Rabia Qureshi'),
 (CAST('70000000-0000-0000-0100-000000000015' AS UNIQUEIDENTIFIER),N'stu.bus.7',N'stu.bus.7@demo.local',@UniBUSDept,@UniTenantId,@UniCampusId,2,N'Faisal Mahmood'),
 (CAST('70000000-0000-0000-0100-000000000016' AS UNIQUEIDENTIFIER),N'stu.eng.1',N'stu.eng.1@demo.local',@UniENGDept,@UniTenantId,@UniCampusId,2,N'Adnan Yousaf'),
 (CAST('70000000-0000-0000-0100-000000000017' AS UNIQUEIDENTIFIER),N'stu.eng.2',N'stu.eng.2@demo.local',@UniENGDept,@UniTenantId,@UniCampusId,2,N'Hira Shahid'),
 (CAST('70000000-0000-0000-0100-000000000018' AS UNIQUEIDENTIFIER),N'stu.eng.3',N'stu.eng.3@demo.local',@UniENGDept,@UniTenantId,@UniCampusId,2,N'Taimoor Javed'),
 (CAST('70000000-0000-0000-0100-000000000019' AS UNIQUEIDENTIFIER),N'stu.eng.4',N'stu.eng.4@demo.local',@UniENGDept,@UniTenantId,@UniCampusId,2,N'Sidra Batool'),
 (CAST('70000000-0000-0000-0100-000000000020' AS UNIQUEIDENTIFIER),N'stu.eng.5',N'stu.eng.5@demo.local',@UniENGDept,@UniTenantId,@UniCampusId,2,N'Nasir Abbas'),
 (CAST('70000000-0000-0000-0100-000000000021' AS UNIQUEIDENTIFIER),N'stu.eng.6',N'stu.eng.6@demo.local',@UniENGDept,@UniTenantId,@UniCampusId,2,N'Kiran Aslam'),
 (CAST('70000000-0000-0000-0100-000000000022' AS UNIQUEIDENTIFIER),N'stu.eng.7',N'stu.eng.7@demo.local',@UniENGDept,@UniTenantId,@UniCampusId,2,N'Shahbaz Ali'),
 (CAST('70000000-0000-0000-0100-000000000023' AS UNIQUEIDENTIFIER),N'stu.arts.1',N'stu.arts.1@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Iram Shehzadi'),
 (CAST('70000000-0000-0000-0100-000000000024' AS UNIQUEIDENTIFIER),N'stu.arts.2',N'stu.arts.2@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Shahmeer Khan'),
 (CAST('70000000-0000-0000-0100-000000000025' AS UNIQUEIDENTIFIER),N'stu.arts.3',N'stu.arts.3@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Amber Rehman'),
 (CAST('70000000-0000-0000-0100-000000000026' AS UNIQUEIDENTIFIER),N'stu.arts.4',N'stu.arts.4@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Saad Farooq'),
 (CAST('70000000-0000-0000-0100-000000000027' AS UNIQUEIDENTIFIER),N'stu.arts.5',N'stu.arts.5@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Yumna Arif'),
 (CAST('70000000-0000-0000-0100-000000000028' AS UNIQUEIDENTIFIER),N'stu.arts.6',N'stu.arts.6@demo.local',@UniARTSDept,@UniTenantId,@UniCampusId,2,N'Zohaib Malik'),
 (CAST('70000000-0000-0000-0100-000000000029' AS UNIQUEIDENTIFIER),N'stu.sci.1',N'stu.sci.1@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Arslan Mehmood'),
 (CAST('70000000-0000-0000-0100-000000000030' AS UNIQUEIDENTIFIER),N'stu.sci.2',N'stu.sci.2@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Dua Fatima'),
 (CAST('70000000-0000-0000-0100-000000000031' AS UNIQUEIDENTIFIER),N'stu.sci.3',N'stu.sci.3@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Junaid Akhtar'),
 (CAST('70000000-0000-0000-0100-000000000032' AS UNIQUEIDENTIFIER),N'stu.sci.4',N'stu.sci.4@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Nimra Asif'),
 (CAST('70000000-0000-0000-0100-000000000033' AS UNIQUEIDENTIFIER),N'stu.sci.5',N'stu.sci.5@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Raheel Sharif'),
 (CAST('70000000-0000-0000-0100-000000000034' AS UNIQUEIDENTIFIER),N'stu.sci.6',N'stu.sci.6@demo.local', @ColSCIDept,@ColTenantId,@ColCampusId,1,N'Wardah Sultan'),
 (CAST('70000000-0000-0000-0100-000000000035' AS UNIQUEIDENTIFIER),N'stu.com.1',N'stu.com.1@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Ahsan Raza'),
 (CAST('70000000-0000-0000-0100-000000000036' AS UNIQUEIDENTIFIER),N'stu.com.2',N'stu.com.2@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Kashaf Idrees'),
 (CAST('70000000-0000-0000-0100-000000000037' AS UNIQUEIDENTIFIER),N'stu.com.3',N'stu.com.3@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Nabeel Tahir'),
 (CAST('70000000-0000-0000-0100-000000000038' AS UNIQUEIDENTIFIER),N'stu.com.4',N'stu.com.4@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Sania Abbas'),
 (CAST('70000000-0000-0000-0100-000000000039' AS UNIQUEIDENTIFIER),N'stu.com.5',N'stu.com.5@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Talha Mushtaq'),
 (CAST('70000000-0000-0000-0100-000000000040' AS UNIQUEIDENTIFIER),N'stu.com.6',N'stu.com.6@demo.local', @ColCOMDept,@ColTenantId,@ColCampusId,1,N'Zara Naeem'),
 (CAST('70000000-0000-0000-0100-000000000041' AS UNIQUEIDENTIFIER),N'stu.hum.1',N'stu.hum.1@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Abubakar Sadiq'),
 (CAST('70000000-0000-0000-0100-000000000042' AS UNIQUEIDENTIFIER),N'stu.hum.2',N'stu.hum.2@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Fajar Haroon'),
 (CAST('70000000-0000-0000-0100-000000000043' AS UNIQUEIDENTIFIER),N'stu.hum.3',N'stu.hum.3@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Ihtisham ul Haq'),
 (CAST('70000000-0000-0000-0100-000000000044' AS UNIQUEIDENTIFIER),N'stu.hum.4',N'stu.hum.4@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Laiba Amir'),
 (CAST('70000000-0000-0000-0100-000000000045' AS UNIQUEIDENTIFIER),N'stu.hum.5',N'stu.hum.5@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Mujtaba Hassan'),
 (CAST('70000000-0000-0000-0100-000000000046' AS UNIQUEIDENTIFIER),N'stu.hum.6',N'stu.hum.6@demo.local', @ColHUMDept,@ColTenantId,@ColCampusId,1,N'Nashra Javed'),
 (CAST('70000000-0000-0000-0100-000000000047' AS UNIQUEIDENTIFIER),N'stu.pri.1',N'stu.pri.1@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Ahmed Raza'),
 (CAST('70000000-0000-0000-0100-000000000048' AS UNIQUEIDENTIFIER),N'stu.pri.2',N'stu.pri.2@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Hania Amir'),
 (CAST('70000000-0000-0000-0100-000000000049' AS UNIQUEIDENTIFIER),N'stu.pri.3',N'stu.pri.3@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Rayyan Malik'),
 (CAST('70000000-0000-0000-0100-000000000050' AS UNIQUEIDENTIFIER),N'stu.pri.4',N'stu.pri.4@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Emaan Shah'),
 (CAST('70000000-0000-0000-0100-000000000051' AS UNIQUEIDENTIFIER),N'stu.pri.5',N'stu.pri.5@demo.local', @SchPRIDept,@SchTenantId,@SchCampusId,0,N'Zayan Qureshi'),
 (CAST('70000000-0000-0000-0100-000000000052' AS UNIQUEIDENTIFIER),N'stu.mid.1',N'stu.mid.1@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Aiman Tariq'),
 (CAST('70000000-0000-0000-0100-000000000053' AS UNIQUEIDENTIFIER),N'stu.mid.2',N'stu.mid.2@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Bilal Naeem'),
 (CAST('70000000-0000-0000-0100-000000000054' AS UNIQUEIDENTIFIER),N'stu.mid.3',N'stu.mid.3@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Hoorain Fatima'),
 (CAST('70000000-0000-0000-0100-000000000055' AS UNIQUEIDENTIFIER),N'stu.mid.4',N'stu.mid.4@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Ibrahim Khalid'),
 (CAST('70000000-0000-0000-0100-000000000056' AS UNIQUEIDENTIFIER),N'stu.mid.5',N'stu.mid.5@demo.local', @SchMIDDept,@SchTenantId,@SchCampusId,0,N'Javeria Shahid'),
 (CAST('70000000-0000-0000-0100-000000000057' AS UNIQUEIDENTIFIER),N'stu.sec.1',N'stu.sec.1@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Haris Ameen'),
 (CAST('70000000-0000-0000-0100-000000000058' AS UNIQUEIDENTIFIER),N'stu.sec.2',N'stu.sec.2@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Mahnoor Javed'),
 (CAST('70000000-0000-0000-0100-000000000059' AS UNIQUEIDENTIFIER),N'stu.sec.3',N'stu.sec.3@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Owais Akram'),
 (CAST('70000000-0000-0000-0100-000000000060' AS UNIQUEIDENTIFIER),N'stu.sec.4',N'stu.sec.4@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Saba Rehman'),
 (CAST('70000000-0000-0000-0100-000000000061' AS UNIQUEIDENTIFIER),N'stu.sec.5',N'stu.sec.5@demo.local', @SchSECDept,@SchTenantId,@SchCampusId,0,N'Yahya Khan'),
 (CAST('70000000-0000-0000-0100-000000000062' AS UNIQUEIDENTIFIER),N'parent.1',  N'parent.1@demo.local',  NULL,        NULL,         NULL,         1,N'Parent of Ahmed Raza'),
 (CAST('70000000-0000-0000-0100-000000000063' AS UNIQUEIDENTIFIER),N'parent.2',  N'parent.2@demo.local',  NULL,        NULL,         NULL,         1,N'Parent of Hania Amir'),
 (CAST('70000000-0000-0000-0100-000000000064' AS UNIQUEIDENTIFIER),N'parent.3',  N'parent.3@demo.local',  NULL,        NULL,         NULL,         1,N'Parent of Rayyan Malik'),
 (CAST('70000000-0000-0000-0100-000000000065' AS UNIQUEIDENTIFIER),N'parent.4',  N'parent.4@demo.local',  NULL,        NULL,         NULL,         1,N'Parent of Emaan Shah'),
 (CAST('70000000-0000-0000-0100-000000000066' AS UNIQUEIDENTIFIER),N'parent.5',  N'parent.5@demo.local',  NULL,        NULL,         NULL,         1,N'Parent of Zayan Qureshi'),
 (CAST('70000000-0000-0000-0100-000000000067' AS UNIQUEIDENTIFIER),N'parent.6',  N'parent.6@demo.local',  NULL,        NULL,         NULL,         1,N'Parent of Aiman Tariq'))
 u(Id,Username,Email,DeptId,TenantId,CampusId,InstType,FullName)
WHERE NOT EXISTS(SELECT 1 FROM [users] x WHERE x.[Id]=u.Id OR x.[Email]=u.Email);

PRINT 'Users seeded (~130).';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 6: STUDENT PROFILES (one per student user)
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[Cgpa],[AdmissionDate],[Status],[IsDeleted],[CreatedAt])
SELECT sp.Id, sp.UserId, sp.RegNo,
  (SELECT TOP 1 ap.[Id] FROM [academic_programs] ap WHERE ap.[DepartmentId]=sp.DeptId ORDER BY ap.[Code]),
  sp.DeptId, sp.Sem, sp.Cgpa, DATEADD(DAY,-365-sp.Sem*180,@Now), 0, 0, @Now
FROM (VALUES
 -- University CS (8),
(CAST('81000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000001' AS UNIQUEIDENTIFIER),N'UNI-CS-001',@UniCSDept, 3,3.5),
 (CAST('81000000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000002' AS UNIQUEIDENTIFIER),N'UNI-CS-002',@UniCSDept, 3,3.7),
 (CAST('81000000-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000003' AS UNIQUEIDENTIFIER),N'UNI-CS-003',@UniCSDept, 5,2.9),
 (CAST('81000000-0000-0000-0000-000000000004' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000004' AS UNIQUEIDENTIFIER),N'UNI-CS-004',@UniCSDept, 5,3.2),
 (CAST('81000000-0000-0000-0000-000000000005' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000005' AS UNIQUEIDENTIFIER),N'UNI-CS-005',@UniCSDept, 1,3.0),
 (CAST('81000000-0000-0000-0000-000000000006' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000006' AS UNIQUEIDENTIFIER),N'UNI-CS-006',@UniCSDept, 1,3.8),
 (CAST('81000000-0000-0000-0000-000000000007' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000007' AS UNIQUEIDENTIFIER),N'UNI-CS-007',@UniCSDept, 7,3.1),
 (CAST('81000000-0000-0000-0000-000000000008' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000008' AS UNIQUEIDENTIFIER),N'UNI-CS-008',@UniCSDept, 7,3.4),
 -- University BUS (7),
(CAST('81000000-0000-0000-0000-000000000009' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000009' AS UNIQUEIDENTIFIER),N'UNI-BUS-001',@UniBUSDept,3,3.2),
 (CAST('81000000-0000-0000-0000-000000000010' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000010' AS UNIQUEIDENTIFIER),N'UNI-BUS-002',@UniBUSDept,3,3.6),
 (CAST('81000000-0000-0000-0000-000000000011' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000011' AS UNIQUEIDENTIFIER),N'UNI-BUS-003',@UniBUSDept,5,2.8),
 (CAST('81000000-0000-0000-0000-000000000012' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000012' AS UNIQUEIDENTIFIER),N'UNI-BUS-004',@UniBUSDept,5,3.3),
 (CAST('81000000-0000-0000-0000-000000000013' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000013' AS UNIQUEIDENTIFIER),N'UNI-BUS-005',@UniBUSDept,1,3.0),
 (CAST('81000000-0000-0000-0000-000000000014' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000014' AS UNIQUEIDENTIFIER),N'UNI-BUS-006',@UniBUSDept,1,3.5),
 (CAST('81000000-0000-0000-0000-000000000015' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000015' AS UNIQUEIDENTIFIER),N'UNI-BUS-007',@UniBUSDept,7,3.1),
 -- University ENG (7),
(CAST('81000000-0000-0000-0000-000000000016' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000016' AS UNIQUEIDENTIFIER),N'UNI-ENG-001',@UniENGDept,3,3.0),
 (CAST('81000000-0000-0000-0000-000000000017' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000017' AS UNIQUEIDENTIFIER),N'UNI-ENG-002',@UniENGDept,3,3.4),
 (CAST('81000000-0000-0000-0000-000000000018' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000018' AS UNIQUEIDENTIFIER),N'UNI-ENG-003',@UniENGDept,5,2.7),
 (CAST('81000000-0000-0000-0000-000000000019' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000019' AS UNIQUEIDENTIFIER),N'UNI-ENG-004',@UniENGDept,5,3.2),
 (CAST('81000000-0000-0000-0000-000000000020' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000020' AS UNIQUEIDENTIFIER),N'UNI-ENG-005',@UniENGDept,1,3.3),
 (CAST('81000000-0000-0000-0000-000000000021' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000021' AS UNIQUEIDENTIFIER),N'UNI-ENG-006',@UniENGDept,1,3.6),
 (CAST('81000000-0000-0000-0000-000000000022' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000022' AS UNIQUEIDENTIFIER),N'UNI-ENG-007',@UniENGDept,7,3.0),
 -- University ARTS (6),
(CAST('81000000-0000-0000-0000-000000000023' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000023' AS UNIQUEIDENTIFIER),N'UNI-ARTS-001',@UniARTSDept,2,3.5),
 (CAST('81000000-0000-0000-0000-000000000024' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000024' AS UNIQUEIDENTIFIER),N'UNI-ARTS-002',@UniARTSDept,2,3.1),
 (CAST('81000000-0000-0000-0000-000000000025' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000025' AS UNIQUEIDENTIFIER),N'UNI-ARTS-003',@UniARTSDept,4,2.9),
 (CAST('81000000-0000-0000-0000-000000000026' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000026' AS UNIQUEIDENTIFIER),N'UNI-ARTS-004',@UniARTSDept,4,3.4),
 (CAST('81000000-0000-0000-0000-000000000027' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000027' AS UNIQUEIDENTIFIER),N'UNI-ARTS-005',@UniARTSDept,1,3.2),
 (CAST('81000000-0000-0000-0000-000000000028' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000028' AS UNIQUEIDENTIFIER),N'UNI-ARTS-006',@UniARTSDept,1,3.6),
 (CAST('81000000-0000-0000-0000-000000000029' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000029' AS UNIQUEIDENTIFIER),N'COL-SCI-001',@ColSCIDept,1,78.5),
 (CAST('81000000-0000-0000-0000-000000000030' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000030' AS UNIQUEIDENTIFIER),N'COL-SCI-002',@ColSCIDept,1,82.0),
 (CAST('81000000-0000-0000-0000-000000000031' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000031' AS UNIQUEIDENTIFIER),N'COL-SCI-003',@ColSCIDept,2,75.2),
 (CAST('81000000-0000-0000-0000-000000000032' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000032' AS UNIQUEIDENTIFIER),N'COL-SCI-004',@ColSCIDept,2,80.0),
 (CAST('81000000-0000-0000-0000-000000000033' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000033' AS UNIQUEIDENTIFIER),N'COL-SCI-005',@ColSCIDept,1,85.1),
 (CAST('81000000-0000-0000-0000-000000000034' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000034' AS UNIQUEIDENTIFIER),N'COL-SCI-006',@ColSCIDept,2,72.8),
 (CAST('81000000-0000-0000-0000-000000000035' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000035' AS UNIQUEIDENTIFIER),N'COL-COM-001',@ColCOMDept,1,70.0),
 (CAST('81000000-0000-0000-0000-000000000036' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000036' AS UNIQUEIDENTIFIER),N'COL-COM-002',@ColCOMDept,1,76.3),
 (CAST('81000000-0000-0000-0000-000000000037' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000037' AS UNIQUEIDENTIFIER),N'COL-COM-003',@ColCOMDept,2,68.5),
 (CAST('81000000-0000-0000-0000-000000000038' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000038' AS UNIQUEIDENTIFIER),N'COL-COM-004',@ColCOMDept,2,81.0),
 (CAST('81000000-0000-0000-0000-000000000039' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000039' AS UNIQUEIDENTIFIER),N'COL-COM-005',@ColCOMDept,1,74.2),
 (CAST('81000000-0000-0000-0000-000000000040' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000040' AS UNIQUEIDENTIFIER),N'COL-COM-006',@ColCOMDept,2,79.5),
 (CAST('81000000-0000-0000-0000-000000000041' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000041' AS UNIQUEIDENTIFIER),N'COL-HUM-001',@ColHUMDept,1,77.8),
 (CAST('81000000-0000-0000-0000-000000000042' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000042' AS UNIQUEIDENTIFIER),N'COL-HUM-002',@ColHUMDept,1,83.0),
 (CAST('81000000-0000-0000-0000-000000000043' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000043' AS UNIQUEIDENTIFIER),N'COL-HUM-003',@ColHUMDept,2,71.5),
 (CAST('81000000-0000-0000-0000-000000000044' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000044' AS UNIQUEIDENTIFIER),N'COL-HUM-004',@ColHUMDept,2,80.5),
 (CAST('81000000-0000-0000-0000-000000000045' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000045' AS UNIQUEIDENTIFIER),N'COL-HUM-005',@ColHUMDept,1,76.0),
 (CAST('81000000-0000-0000-0000-000000000046' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000046' AS UNIQUEIDENTIFIER),N'COL-HUM-006',@ColHUMDept,2,73.9),
 (CAST('81000000-0000-0000-0000-000000000047' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000047' AS UNIQUEIDENTIFIER),N'SCH-C3-001',@SchPRIDept,3,88.0),
 (CAST('81000000-0000-0000-0000-000000000048' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000048' AS UNIQUEIDENTIFIER),N'SCH-C4-002',@SchPRIDept,4,92.5),
 (CAST('81000000-0000-0000-0000-000000000049' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000049' AS UNIQUEIDENTIFIER),N'SCH-C5-003',@SchPRIDept,5,85.0),
 (CAST('81000000-0000-0000-0000-000000000050' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000050' AS UNIQUEIDENTIFIER),N'SCH-C1-004',@SchPRIDept,1,90.0),
 (CAST('81000000-0000-0000-0000-000000000051' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000051' AS UNIQUEIDENTIFIER),N'SCH-C2-005',@SchPRIDept,2,87.5),
 (CAST('81000000-0000-0000-0000-000000000052' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000052' AS UNIQUEIDENTIFIER),N'SCH-C6-006',@SchMIDDept,6,82.0),
 (CAST('81000000-0000-0000-0000-000000000053' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000053' AS UNIQUEIDENTIFIER),N'SCH-C7-007',@SchMIDDept,7,78.5),
 (CAST('81000000-0000-0000-0000-000000000054' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000054' AS UNIQUEIDENTIFIER),N'SCH-C8-008',@SchMIDDept,8,84.0),
 (CAST('81000000-0000-0000-0000-000000000055' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000055' AS UNIQUEIDENTIFIER),N'SCH-C6-009',@SchMIDDept,6,80.5),
 (CAST('81000000-0000-0000-0000-000000000056' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000056' AS UNIQUEIDENTIFIER),N'SCH-C7-010',@SchMIDDept,7,86.0),
 (CAST('81000000-0000-0000-0000-000000000057' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000057' AS UNIQUEIDENTIFIER),N'SCH-C9-011',@SchSECDept,9,75.0),
 (CAST('81000000-0000-0000-0000-000000000058' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000058' AS UNIQUEIDENTIFIER),N'SCH-C10-012',@SchSECDept,10,81.5),
 (CAST('81000000-0000-0000-0000-000000000059' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000059' AS UNIQUEIDENTIFIER),N'SCH-C9-013',@SchSECDept,9,79.0),
 (CAST('81000000-0000-0000-0000-000000000060' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000060' AS UNIQUEIDENTIFIER),N'SCH-C10-014',@SchSECDept,10,83.0),
 (CAST('81000000-0000-0000-0000-000000000061' AS UNIQUEIDENTIFIER),CAST('70000000-0000-0000-0100-000000000061' AS UNIQUEIDENTIFIER),N'SCH-C9-015',@SchSECDept,9,77.5))
 sp(Id,UserId,RegNo,DeptId,Sem,Cgpa)
WHERE NOT EXISTS(SELECT 1 FROM [student_profiles] x WHERE x.[Id]=sp.Id)
  AND EXISTS(SELECT 1 FROM [users] u WHERE u.[Id]=sp.UserId);

PRINT 'Student profiles seeded (61).';

PRINT '=== CORE DATA COMPLETE. ===';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 7: ADMIN & FACULTY DEPARTMENT ASSIGNMENTS
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [admin_department_assignments] ([Id],[AdminUserId],[DepartmentId],[AssignedAt],[CreatedAt])
SELECT NEWID(),a.AdminId,a.DeptId,@Now,@Now FROM (VALUES
 (@AdmCS,@UniCSDept),(@AdmBUS,@UniBUSDept),(@AdmENG,@UniENGDept),(@AdmARTS,@UniARTSDept),
 (@AdmSCI,@ColSCIDept),(@AdmCOM,@ColCOMDept),(@AdmHUM,@ColHUMDept),
 (@AdmPRI,@SchPRIDept),(@AdmMID,@SchMIDDept),(@AdmSEC,@SchSECDept))
 a(AdminId,DeptId)
WHERE NOT EXISTS(SELECT 1 FROM [admin_department_assignments] x WHERE x.[AdminUserId]=a.AdminId AND x.[DepartmentId]=a.DeptId);

INSERT INTO [faculty_department_assignments] ([Id],[FacultyUserId],[DepartmentId],[AssignedAt],[CreatedAt])
SELECT NEWID(),f.FacId,f.DeptId,@Now,@Now FROM (VALUES
 (CAST('70000000-0000-0000-0000-000000002001' AS UNIQUEIDENTIFIER),@UniCSDept),(CAST('70000000-0000-0000-0000-000000002002' AS UNIQUEIDENTIFIER),@UniCSDept),(CAST('70000000-0000-0000-0000-000000002003' AS UNIQUEIDENTIFIER),@UniCSDept),
 (CAST('70000000-0000-0000-0000-000000002004' AS UNIQUEIDENTIFIER),@UniBUSDept),(CAST('70000000-0000-0000-0000-000000002005' AS UNIQUEIDENTIFIER),@UniBUSDept),(CAST('70000000-0000-0000-0000-000000002006' AS UNIQUEIDENTIFIER),@UniBUSDept),
 (CAST('70000000-0000-0000-0000-000000002007' AS UNIQUEIDENTIFIER),@UniENGDept),(CAST('70000000-0000-0000-0000-000000002008' AS UNIQUEIDENTIFIER),@UniENGDept),(CAST('70000000-0000-0000-0000-000000002009' AS UNIQUEIDENTIFIER),@UniENGDept),
 (CAST('70000000-0000-0000-0000-000000002010' AS UNIQUEIDENTIFIER),@UniARTSDept),(CAST('70000000-0000-0000-0000-000000002011' AS UNIQUEIDENTIFIER),@UniARTSDept),(CAST('70000000-0000-0000-0000-000000002012' AS UNIQUEIDENTIFIER),@UniARTSDept),
 (CAST('70000000-0000-0000-0000-000000002013' AS UNIQUEIDENTIFIER),@ColSCIDept),(CAST('70000000-0000-0000-0000-000000002014' AS UNIQUEIDENTIFIER),@ColSCIDept),(CAST('70000000-0000-0000-0000-000000002015' AS UNIQUEIDENTIFIER),@ColSCIDept),
 (CAST('70000000-0000-0000-0000-000000002016' AS UNIQUEIDENTIFIER),@ColCOMDept),(CAST('70000000-0000-0000-0000-000000002017' AS UNIQUEIDENTIFIER),@ColCOMDept),(CAST('70000000-0000-0000-0000-000000002018' AS UNIQUEIDENTIFIER),@ColCOMDept),
 (CAST('70000000-0000-0000-0000-000000002019' AS UNIQUEIDENTIFIER),@ColHUMDept),(CAST('70000000-0000-0000-0000-000000002020' AS UNIQUEIDENTIFIER),@ColHUMDept),(CAST('70000000-0000-0000-0000-000000002021' AS UNIQUEIDENTIFIER),@ColHUMDept),
 (CAST('70000000-0000-0000-0000-000000002022' AS UNIQUEIDENTIFIER),@SchPRIDept),(CAST('70000000-0000-0000-0000-000000002023' AS UNIQUEIDENTIFIER),@SchPRIDept),(CAST('70000000-0000-0000-0000-000000002024' AS UNIQUEIDENTIFIER),@SchPRIDept),
 (CAST('70000000-0000-0000-0000-000000002026' AS UNIQUEIDENTIFIER),@SchMIDDept),(CAST('70000000-0000-0000-0000-000000002027' AS UNIQUEIDENTIFIER),@SchMIDDept),(CAST('70000000-0000-0000-0000-000000002028' AS UNIQUEIDENTIFIER),@SchMIDDept),
 (CAST('70000000-0000-0000-0000-000000002030' AS UNIQUEIDENTIFIER),@SchSECDept),(CAST('70000000-0000-0000-0000-000000002031' AS UNIQUEIDENTIFIER),@SchSECDept),(CAST('70000000-0000-0000-0000-000000002032' AS UNIQUEIDENTIFIER),@SchSECDept))
 f(FacId,DeptId)
WHERE NOT EXISTS(SELECT 1 FROM [faculty_department_assignments] x WHERE x.[FacultyUserId]=f.FacId AND x.[DepartmentId]=f.DeptId);

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 8: COURSE OFFERINGS (at least 2 per course, scoped by semester)
   ══════════════════════════════════════════════════════════════════════════════ */
DECLARE @Off TABLE (Id UNIQUEIDENTIFIER, CrsId UNIQUEIDENTIFIER, SemId UNIQUEIDENTIFIER, FacId UNIQUEIDENTIFIER, DeptId UNIQUEIDENTIFIER, TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER, InstType INT);
DECLARE @SemF26 UNIQUEIDENTIFIER = @SemFall2026, @SemS27 UNIQUEIDENTIFIER = @SemSpring2027;

INSERT INTO @Off
SELECT CAST(CONCAT('91000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY c.Id) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY c.Id) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 c.Id, o.SemId,
 (SELECT TOP 1 f.[Id] FROM [users] f WHERE f.[DepartmentId]=c.[DepartmentId] AND f.[RoleId]=@RoleFaculty AND f.[IsDeleted]=0 ORDER BY f.[Id]),
 c.[DepartmentId], d.[TenantId], d.[CampusId], d.[InstitutionType]
FROM [courses] c
JOIN [departments] d ON d.[Id]=c.[DepartmentId]
CROSS APPLY (VALUES (@SemF26),(@SemS27)) o(SemId)
WHERE c.[IsDeleted]=0 AND d.[IsDeleted]=0;

INSERT INTO [course_offerings] ([Id],[CourseId],[SemesterId],[FacultyUserId],[MaxEnrollment],[IsOpen],[TenantId],[CampusId],[InstitutionType],[IsDeleted],[CreatedAt])
SELECT o.Id,o.CrsId,o.SemId,o.FacId,40,1,o.TenantId,o.CampusId,o.InstType,0,@Now FROM @Off o
WHERE EXISTS(SELECT 1 FROM [courses] c WHERE c.[Id]=o.CrsId)
  AND NOT EXISTS(SELECT 1 FROM [course_offerings] x WHERE x.[Id]=o.Id);
PRINT 'Course offerings seeded.';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 9: ENROLLMENTS (properly scoped by DepartmentId + TenantId + CampusId)
   ══════════════════════════════════════════════════════════════════════════════ */
;WITH stu AS (
  SELECT sp.[Id] AS ProfileId, sp.[UserId], sp.[CurrentSemesterNumber], sp.[DepartmentId],
         u.[TenantId], u.[CampusId], d.[InstitutionType]
  FROM [student_profiles] sp
  JOIN [users] u ON u.[Id]=sp.[UserId]
  JOIN [departments] d ON d.[Id]=sp.[DepartmentId]
  WHERE sp.[IsDeleted]=0
),
offer AS (
  SELECT co.[Id] AS OfferingId, co.[CourseId], co.[SemesterId], co.[TenantId], co.[CampusId],
         co.[InstitutionType], c.[DepartmentId]
  FROM [course_offerings] co
  JOIN [courses] c ON c.[Id]=co.[CourseId]
  WHERE co.[IsDeleted]=0
)
INSERT INTO [enrollments] ([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
SELECT CAST(CONCAT('92000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY s.ProfileId,o.OfferingId) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY s.ProfileId,o.OfferingId) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 s.ProfileId, o.OfferingId, @Now, N'Active', @Now
FROM stu s
CROSS APPLY (SELECT TOP 4 o.* FROM offer o
  WHERE o.[DepartmentId]=s.[DepartmentId]
    AND o.[TenantId]=s.[TenantId]
    AND o.[CampusId]=s.[CampusId]
  ORDER BY o.OfferingId) o
WHERE NOT EXISTS(SELECT 1 FROM [enrollments] x WHERE x.[StudentProfileId]=s.ProfileId AND x.[CourseOfferingId]=o.OfferingId);
PRINT 'Enrollments seeded (scoped by Department+Tenant+Campus).';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 10: ATTENDANCE RECORDS (10 days per enrollment)
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [attendance_records] ([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt])
SELECT NEWID(),
 e.[StudentProfileId], e.[CourseOfferingId], d.RecDate,
 CASE WHEN d.n%10<7 THEN N'Present' WHEN d.n%10<9 THEN N'Late' ELSE N'Absent' END,
 (SELECT TOP 1 co.[FacultyUserId] FROM [course_offerings] co WHERE co.[Id]=e.[CourseOfferingId]),
 @Now
FROM [enrollments] e
CROSS APPLY (SELECT DATEADD(DAY,-n,@Now) AS RecDate, n FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) nums(n)) d
WHERE NOT EXISTS(SELECT 1 FROM [attendance_records] x WHERE x.[StudentProfileId]=e.[StudentProfileId] AND x.[CourseOfferingId]=e.[CourseOfferingId] AND x.[Date]=d.RecDate);
PRINT 'Attendance records seeded.';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 11: ASSIGNMENTS + SUBMISSIONS
   ══════════════════════════════════════════════════════════════════════════════ */
DECLARE @Assign TABLE (Id UNIQUEIDENTIFIER, OffId UNIQUEIDENTIFIER, Title NVARCHAR(300), DueDt DATETIME2, Mm DECIMAL(8,2));
INSERT INTO @Assign
SELECT CAST(CONCAT('94000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,n.n) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,n.n) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 co.Id, N'Assignment ' + CAST(n.n AS NVARCHAR) + N' — ' + c.Title,
 DATEADD(DAY,14*n.n,@Now), CAST(20+5*n.n AS DECIMAL(8,2))
FROM [course_offerings] co
JOIN [courses] c ON c.[Id]=co.[CourseId]
CROSS APPLY (VALUES(1),(2),(3)) n(n)
WHERE EXISTS(SELECT 1 FROM [enrollments] e WHERE e.[CourseOfferingId]=co.Id)
  AND co.[IsDeleted]=0;

INSERT INTO [assignments] ([Id],[CourseOfferingId],[Title],[DueDate],[MaxMarks],[IsPublished],[IsDeleted],[CreatedAt])
SELECT a.Id,a.OffId,a.Title,a.DueDt,a.Mm,1,0,@Now FROM @Assign a
WHERE NOT EXISTS(SELECT 1 FROM [assignments] x WHERE x.[Id]=a.Id);

INSERT INTO [assignment_submissions] ([Id],[AssignmentId],[StudentProfileId],[FileUrl],[TextContent],[SubmittedAt],[Status],[CreatedAt])
SELECT CAST(CONCAT('95000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY a.Id,e.StudentProfileId) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY a.Id,e.StudentProfileId) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 a.Id, e.StudentProfileId,
 N'/uploads/submission_' + CAST(ROW_NUMBER()OVER(ORDER BY a.Id,e.StudentProfileId) AS NVARCHAR) + N'.pdf',
 N'Submitted solution for ' + a.Title + N'.',
 DATEADD(DAY,-1,@Now), N'Submitted', @Now
FROM [assignments] a
JOIN [enrollments] e ON e.[CourseOfferingId]=a.[CourseOfferingId]
WHERE NOT EXISTS(SELECT 1 FROM [assignment_submissions] x WHERE x.[AssignmentId]=a.Id AND x.[StudentProfileId]=e.[StudentProfileId]);
PRINT 'Assignments & submissions seeded.';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 12: RESULTS (3 types per enrollment: Quiz, Midterm, Final)
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [results] ([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[GradePoint],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
SELECT CAST(CONCAT('96000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY e.StudentProfileId,e.CourseOfferingId,rt.ResultType) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY e.StudentProfileId,e.CourseOfferingId,rt.ResultType) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 e.StudentProfileId, e.CourseOfferingId,
 rt.ResultType, rt.MarksObtained, rt.MaxMarks,
 CASE WHEN d.InstitutionType=2 THEN CAST((rt.MarksObtained/NULLIF(rt.MaxMarks,0))*4.0 AS DECIMAL(4,2)) ELSE NULL END,
 1, @Now, @SAId, @Now
FROM [enrollments] e
JOIN [course_offerings] co ON co.Id=e.CourseOfferingId
JOIN [courses] c ON c.Id=co.CourseId
JOIN [departments] d ON d.Id=c.DepartmentId
CROSS APPLY (VALUES (N'Quiz',CAST(15+ABS(CHECKSUM(NEWID()))%11 AS DECIMAL(8,2)),CAST(20.00 AS DECIMAL(8,2))),
                     (N'Midterm',CAST(25+ABS(CHECKSUM(NEWID()))%16 AS DECIMAL(8,2)),CAST(35.00 AS DECIMAL(8,2))),
                     (N'Final',CAST(40+ABS(CHECKSUM(NEWID()))%31 AS DECIMAL(8,2)),CAST(45.00 AS DECIMAL(8,2)))) rt(ResultType,MarksObtained,MaxMarks)
WHERE NOT EXISTS(SELECT 1 FROM [results] x WHERE x.[StudentProfileId]=e.[StudentProfileId] AND x.[CourseOfferingId]=e.[CourseOfferingId] AND x.[ResultType]=rt.ResultType);
PRINT 'Results seeded.';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 13: QUIZZES + QUESTIONS + OPTIONS
   ══════════════════════════════════════════════════════════════════════════════ */
DECLARE @Qz TABLE (Id UNIQUEIDENTIFIER, OffId UNIQUEIDENTIFIER, Title NVARCHAR(300), FacId UNIQUEIDENTIFIER);
INSERT INTO @Qz
SELECT CAST(CONCAT('97000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 co.Id, N'Quiz — ' + c.Title, co.FacultyUserId
FROM [course_offerings] co
JOIN [courses] c ON c.[Id]=co.[CourseId]
WHERE EXISTS(SELECT 1 FROM [enrollments] e WHERE e.[CourseOfferingId]=co.Id) AND co.[IsDeleted]=0;

INSERT INTO [quizzes] ([Id],[CourseOfferingId],[Title],[MaxAttempts],[IsPublished],[IsActive],[CreatedByUserId],[CreatedAt])
SELECT q.Id,q.OffId,q.Title,2,1,1,q.FacId,@Now FROM @Qz q
WHERE NOT EXISTS(SELECT 1 FROM [quizzes] x WHERE x.[Id]=q.Id);

/* Quiz questions (3 per quiz) */
DECLARE @Qn TABLE (Id UNIQUEIDENTIFIER, QzId UNIQUEIDENTIFIER, Txt NVARCHAR(500), Marks INT, Ord INT);
INSERT INTO @Qn
SELECT CAST(CONCAT('97100000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY q.Id,n.n) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY q.Id,n.n) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 q.Id, CASE n.n%3 WHEN 0 THEN N'Explain the key concepts covered in this module.' WHEN 1 THEN N'What is the primary objective of the topic discussed?' ELSE N'Describe a real-world application of this subject.' END,
 10, n.n
FROM @Qz q CROSS APPLY (VALUES(1),(2),(3)) n(n);

INSERT INTO [quiz_questions] ([Id],[QuizId],[Text],[Type],[Marks],[OrderIndex],[CreatedAt])
SELECT qn.Id,qn.QzId,qn.Txt,N'ShortAnswer',qn.Marks,qn.Ord,@Now FROM @Qn qn
WHERE NOT EXISTS(SELECT 1 FROM [quiz_questions] x WHERE x.[Id]=qn.Id);

/* Quiz options (4 per question) */
INSERT INTO [quiz_options] ([Id],[QuizQuestionId],[Text],[IsCorrect],[OrderIndex],[CreatedAt])
SELECT CAST(CONCAT('97200000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY qn.Id,o.Ord) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY qn.Id,o.Ord) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 qn.Id, CASE o.Ord WHEN 1 THEN N'Option A: The primary approach' WHEN 2 THEN N'Option B: An alternative method' WHEN 3 THEN N'Option C: A hybrid solution' ELSE N'Option D: None of the above' END,
 CASE WHEN o.Ord=1 THEN 1 ELSE 0 END, o.Ord, @Now
FROM @Qn qn CROSS APPLY (VALUES(1),(2),(3),(4)) o(Ord)
WHERE NOT EXISTS(SELECT 1 FROM [quiz_options] x WHERE x.[QuizQuestionId]=qn.Id AND x.[OrderIndex]=o.Ord);
PRINT 'Quizzes, questions & options seeded.';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 14: COURSE CONTENT MODULES + ANNOUNCEMENTS + DISCUSSIONS
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [course_content_modules] ([Id],[OfferingId],[Title],[WeekNumber],[Body],[IsPublished],[PublishedAt],[IsDeleted],[CreatedAt])
SELECT CAST(CONCAT('98000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,w.wk) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,w.wk) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 co.Id, N'Week ' + CAST(w.wk AS NVARCHAR) + N' — ' + c.Title, w.wk,
 N'## Learning Objectives' + CHAR(13)+CHAR(10) + N'- Understand key concepts' + CHAR(13)+CHAR(10) + N'- Complete practice exercises' + CHAR(13)+CHAR(10) + N'- Review assigned readings',
 1, @Now, 0, @Now
FROM [course_offerings] co
JOIN [courses] c ON c.[Id]=co.[CourseId]
CROSS APPLY (VALUES(1),(2),(3),(4),(5),(6)) w(wk)
WHERE co.[IsDeleted]=0
  AND NOT EXISTS(SELECT 1 FROM [course_content_modules] x WHERE x.[OfferingId]=co.Id AND x.[WeekNumber]=w.wk);

/* Announcements (2 per offering) */
INSERT INTO [course_announcements] ([Id],[OfferingId],[AuthorId],[Title],[Body],[PostedAt],[IsDeleted],[CreatedAt])
SELECT CAST(CONCAT('98100000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,n.n) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,n.n) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 co.Id, co.FacultyUserId,
 CASE n.n WHEN 1 THEN N'Welcome to ' + c.Title ELSE N'Important: Upcoming assessment for ' + c.Title END,
 CASE n.n WHEN 1 THEN N'Welcome to this course! Please review the syllabus and Week 1 materials.' ELSE N'Please prepare for the upcoming quiz. Review chapters 1-4.' END,
 DATEADD(DAY,-n.n*5,@Now), 0, @Now
FROM [course_offerings] co
JOIN [courses] c ON c.[Id]=co.[CourseId]
CROSS APPLY (VALUES(1),(2)) n(n)
WHERE co.[IsDeleted]=0
  AND NOT EXISTS(SELECT 1 FROM [course_announcements] x WHERE x.[OfferingId]=co.Id AND x.[Title] LIKE '%' + c.Title + '%');

/* Discussion threads (2 per offering) */
DECLARE @Thread TABLE (Id UNIQUEIDENTIFIER, OffId UNIQUEIDENTIFIER, AuthorId UNIQUEIDENTIFIER, Title NVARCHAR(300));
INSERT INTO @Thread
SELECT CAST(CONCAT('98200000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,n.n) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY co.Id,n.n) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 co.Id, co.FacultyUserId,
 CASE n.n WHEN 1 THEN N'Discussion: Key topics in ' + c.Title ELSE N'Q&A: Questions about ' + c.Title END
FROM [course_offerings] co
JOIN [courses] c ON c.[Id]=co.[CourseId]
CROSS APPLY (VALUES(1),(2)) n(n)
WHERE co.[IsDeleted]=0;

INSERT INTO [discussion_threads] ([Id],[OfferingId],[AuthorId],[Title],[IsPinned],[IsClosed],[IsDeleted],[CreatedAt])
SELECT t.Id,t.OffId,t.AuthorId,t.Title,0,0,0,@Now FROM @Thread t
WHERE NOT EXISTS(SELECT 1 FROM [discussion_threads] x WHERE x.[Id]=t.Id);

/* Discussion replies (2-3 per thread) */
INSERT INTO [discussion_replies] ([Id],[ThreadId],[AuthorId],[Body],[IsDeleted],[CreatedAt])
SELECT CAST(CONCAT('98300000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY t.Id,r.n) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY t.Id,r.n) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 t.Id,
 ISNULL((SELECT TOP 1 e.[StudentProfileId] FROM [enrollments] e JOIN [course_offerings] co ON co.[Id]=e.[CourseOfferingId] WHERE co.[Id]=t.OffId ORDER BY NEWID()), CAST('81000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER)),
 CASE r.n WHEN 1 THEN N'Thank you for this discussion. Very helpful!' WHEN 2 THEN N'I have a question about this topic. Can you clarify?' ELSE N'Great points! Here is another perspective...' END,
 0, @Now
FROM @Thread t CROSS APPLY (VALUES(1),(2),(3)) r(n)
WHERE NOT EXISTS(SELECT 1 FROM [discussion_replies] x WHERE x.[ThreadId]=t.Id);
PRINT 'LMS content, announcements & discussions seeded.';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 15: PAYMENTS + NOTIFICATIONS + TICKETS
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [payment_receipts] ([Id],[StudentProfileId],[CreatedByUserId],[ReceiptNo],[Status],[Amount],[Description],[DueDate],[IsDeleted],[CreatedAt],[UpdatedAt])
SELECT CAST(CONCAT('99000000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY sp.Id) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY sp.Id) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 sp.Id, (SELECT TOP 1 u.[Id] FROM [users] u JOIN [departments] d ON d.[Id]=u.[DepartmentId] WHERE d.[TenantId]=(SELECT d2.[TenantId] FROM [departments] d2 WHERE d2.[Id]=sp.[DepartmentId]) AND u.[RoleId]=@RoleFinance AND u.[IsDeleted]=0),
 N'RCPT-' + CAST(ROW_NUMBER()OVER(ORDER BY sp.Id) AS NVARCHAR),
 1, 15000.00+ABS(CHECKSUM(NEWID()))%35000, N'Tuition fee — Semester ' + CAST(sp.[CurrentSemesterNumber] AS NVARCHAR),
 DATEADD(DAY,30,@Now), 0, @Now, @Now
FROM [student_profiles] sp WHERE sp.[IsDeleted]=0
  AND NOT EXISTS(SELECT 1 FROM [payment_receipts] x WHERE x.[StudentProfileId]=sp.Id);

INSERT INTO [notifications] ([Id],[Title],[Body],[Type],[SenderUserId],[IsSystemGenerated],[IsActive],[CreatedAt])
SELECT CAST(CONCAT('99100000-0000-0000-',RIGHT('0000'+CAST(n.Idx AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(n.Idx AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 n.Title, n.Body, n.Type, @SAId, 1, 1, @Now
FROM (VALUES
 (1,N'Welcome to Tabsan EduSphere',N'Your account is now active. Explore the dashboard!',N'System'),
 (2,N'New Assignment Posted',N'A new assignment has been posted for your enrolled course.',N'Academic'),
 (3,N'Payment Reminder',N'Your tuition fee is due in 30 days. Please make the payment.',N'Finance'),
 (4,N'Attendance Alert',N'Your attendance has dropped below 75%. Please take action.',N'Academic'),
 (5,N'Upcoming Holiday',N'The institution will remain closed next Monday.',N'General'))
 n(Idx,Title,Body,Type)
WHERE NOT EXISTS(SELECT 1 FROM [notifications] x WHERE x.[Title]=n.Title);

INSERT INTO [notification_recipients] ([Id],[NotificationId],[RecipientUserId],[IsRead],[ReadAt],[CreatedAt])
SELECT NEWID(), n.[Id], u.[Id],
 CASE WHEN ABS(CHECKSUM(NEWID()))%3=0 THEN 1 ELSE 0 END,
 CASE WHEN ABS(CHECKSUM(NEWID()))%3=0 THEN DATEADD(HOUR,-ABS(CHECKSUM(NEWID()))%48,@Now) ELSE NULL END, @Now
FROM [notifications] n
CROSS JOIN (SELECT TOP 8 [Id] FROM [users] WHERE [RoleId]=@RoleStudent ORDER BY [Id]) u
WHERE NOT EXISTS(SELECT 1 FROM [notification_recipients] x WHERE x.[NotificationId]=n.[Id] AND x.[RecipientUserId]=u.[Id]);

INSERT INTO [support_tickets] ([Id],[SubmitterId],[Subject],[Body],[Category],[Status],[ReopenWindowDays],[IsDeleted],[CreatedAt])
SELECT CAST(CONCAT('99200000-0000-0000-',RIGHT('0000'+CAST(t.Idx AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(t.Idx AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 t.SubmitterId, t.Subject, t.Body, t.Category, 1, 7, 0, @Now
FROM (VALUES
 (1,CAST('70000000-0000-0000-0100-000000000001' AS UNIQUEIDENTIFIER),N'Cannot access course materials',N'I am unable to view the uploaded PDF for my course.',2),
 (2,CAST('70000000-0000-0000-0100-000000000035' AS UNIQUEIDENTIFIER),N'Payment not reflecting',N'I made a payment yesterday but it still shows unpaid.',3),
 (3,CAST('70000000-0000-0000-0000-000000002013' AS UNIQUEIDENTIFIER),N'Error in attendance record',N'My attendance for last Monday is marked incorrectly.',1),
 (4,CAST('70000000-0000-0000-0100-000000000057' AS UNIQUEIDENTIFIER),N'Request for transcript',N'I need an official transcript for my university application.',3))
 t(Idx,SubmitterId,Subject,Body,Category)
WHERE NOT EXISTS(SELECT 1 FROM [support_tickets] x WHERE x.[Subject]=t.Subject);
PRINT 'Payments, notifications & support tickets seeded.';

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 16: GRADING CONFIGS, DEGREE RULES, REPORT CARDS
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [institution_grading_profiles] ([Id],[InstitutionType],[PassThreshold],[GradeRangesJson],[IsActive],[CreatedAt])
SELECT CAST(CONCAT('99300000-0000-0000-',RIGHT('0000'+CAST(g.InstType AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(g.InstType AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 g.InstType, g.Pass, g.Ranges, 1, @Now
FROM (VALUES
 (0,50.00,N'[{"grade":"A+","from":90,"to":100},{"grade":"A","from":80,"to":89},{"grade":"B","from":70,"to":79},{"grade":"C","from":60,"to":69},{"grade":"D","from":50,"to":59},{"grade":"F","from":0,"to":49}]'),
 (1,55.00,N'[{"grade":"A+","from":90,"to":100},{"grade":"A","from":80,"to":89},{"grade":"B","from":70,"to":79},{"grade":"C","from":60,"to":69},{"grade":"D","from":55,"to":59},{"grade":"F","from":0,"to":54}]'),
 (2,60.00,N'[{"grade":"A","from":85,"to":100},{"grade":"B","from":75,"to":84},{"grade":"C","from":65,"to":74},{"grade":"D","from":60,"to":64},{"grade":"F","from":0,"to":59}]'))
 g(InstType,Pass,Ranges)
WHERE NOT EXISTS(SELECT 1 FROM [institution_grading_profiles] x WHERE x.[InstitutionType]=g.InstType);

INSERT INTO [degree_rules] ([Id],[AcademicProgramId],[MinTotalCredits],[MinCoreCredits],[MinElectiveCredits],[MinGpa],[IsDeleted],[CreatedAt])
SELECT CAST(CONCAT('99400000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY ap.Id) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY ap.Id) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 ap.Id, CASE ap.TotalSemesters WHEN 2 THEN 36 WHEN 4 THEN 72 WHEN 5 THEN 40 ELSE 132 END,
 CASE ap.TotalSemesters WHEN 2 THEN 24 WHEN 4 THEN 48 WHEN 5 THEN 30 ELSE 96 END,
 CASE ap.TotalSemesters WHEN 2 THEN 6 WHEN 4 THEN 12 WHEN 5 THEN 5 ELSE 24 END,
 2.00, 0, @Now
FROM [academic_programs] ap
WHERE NOT EXISTS(SELECT 1 FROM [degree_rules] x WHERE x.[AcademicProgramId]=ap.[Id]);

INSERT INTO [student_report_cards] ([Id],[StudentProfileId],[InstitutionType],[PeriodLabel],[PayloadJson],[GeneratedByUserId],[GeneratedAt],[CreatedAt])
SELECT CAST(CONCAT('99500000-0000-0000-',RIGHT('0000'+CAST(ROW_NUMBER()OVER(ORDER BY sp.Id) AS NVARCHAR),4),'-',RIGHT('000000000000'+CAST(ROW_NUMBER()OVER(ORDER BY sp.Id) AS NVARCHAR),12)) AS UNIQUEIDENTIFIER),
 sp.Id, d.InstitutionType,
 CASE d.InstitutionType WHEN 0 THEN N'Class '+CAST(sp.CurrentSemesterNumber AS NVARCHAR) WHEN 1 THEN N'Year '+CAST(sp.CurrentSemesterNumber AS NVARCHAR) ELSE N'Semester '+CAST(sp.CurrentSemesterNumber AS NVARCHAR) END,
 N'{"results":[],"summary":{"total":500,"obtained":420,"percentage":84.0,"grade":"A"}}', @SAId, @Now, @Now
FROM [student_profiles] sp
JOIN [departments] d ON d.Id=sp.DepartmentId
WHERE sp.[IsDeleted]=0
  AND NOT EXISTS(SELECT 1 FROM [student_report_cards] x WHERE x.[StudentProfileId]=sp.Id);

/* ══════════════════════════════════════════════════════════════════════════════
   SECTION 17: ISO COMPLIANCE DATA
   ══════════════════════════════════════════════════════════════════════════════ */
INSERT INTO [audit_logs] ([ActorUserId],[Action],[EntityName],[EntityId],[OldValuesJson],[NewValuesJson],[OccurredAt],[IpAddress])
SELECT TOP 40 u.[Id], a.Act, a.Ent, CAST(NEWID() AS NVARCHAR(100)), N'{"old":"value"}', N'{"new":"value"}',
 DATEADD(MINUTE,-ABS(CHECKSUM(NEWID()))%43200,@Now), CONCAT(N'192.168.1.',ABS(CHECKSUM(NEWID()))%200)
FROM [users] u
CROSS JOIN (VALUES (N'Login',N'User'),(N'View',N'Course'),(N'Edit',N'Result'),(N'Create',N'Assignment'),(N'Publish',N'Attendance'),(N'Export',N'Report')) a(Act,Ent)
WHERE u.[IsDeleted]=0 ORDER BY NEWID();

INSERT INTO [login_activity_logs] ([Id],[UserId],[Username],[AttemptedAt],[IpAddress],[UserAgent],[IsSuccess],[FailureReason],[UserIsLockedOut])
SELECT NEWID(), (SELECT TOP 1 [Id] FROM [users] WHERE [Username] IN ('superadmin','admin.cs','fac.cs.1','stu.cs.1','finance.uni') ORDER BY NEWID()),
 la.UserName, la.AttAt, CONCAT(N'192.168.1.',ABS(CHECKSUM(NEWID()))%200),
 N'Mozilla/5.0 (Demo)', CASE la.IsOk WHEN 1 THEN 1 ELSE 0 END,
 CASE la.IsOk WHEN 0 THEN N'Invalid credentials' ELSE NULL END, 0
FROM (VALUES (N'superadmin',DATEADD(HOUR,-1,@Now),1),(N'admin.cs',DATEADD(HOUR,-2,@Now),1),
 (N'fac.cs.1',DATEADD(HOUR,-3,@Now),1),(N'stu.cs.1',DATEADD(HOUR,-4,@Now),0),
 (N'finance.uni',DATEADD(DAY,-1,@Now),1),(N'admin.sci',DATEADD(DAY,-1,@Now),1),
 (N'fac.mid.1',DATEADD(DAY,-2,@Now),1),(N'stu.sec.1',DATEADD(DAY,-2,@Now),0),
 (N'admin.hum',DATEADD(DAY,-3,@Now),1),(N'fac.eng.1',DATEADD(DAY,-3,@Now),0))
 la(UserName,AttAt,IsOk);

INSERT INTO [backup_logs] ([Id],[BackupType],[FileName],[FilePath],[FileSizeBytes],[DurationSeconds],[Status],[StartedAt],[CompletedAt],[Checksum],[InitiatedBy])
SELECT NEWID(), bt.[Type], bt.[File], CONCAT(N'D:\Backups\',bt.[File]), 1024000+ABS(CHECKSUM(NEWID()))%5000000, 30+ABS(CHECKSUM(NEWID()))%120,
 bt.[Status], bt.[Started], bt.[Completed], CONVERT(NVARCHAR(128),NEWID()), N'superadmin'
FROM (VALUES
 (N'Full',N'TabsanEduSphere_Full_20260605.bak',N'Success',DATEADD(DAY,-1,@Now),DATEADD(DAY,-1,DATEADD(MINUTE,45,@Now))),
 (N'Differential',N'TabsanEduSphere_Diff_20260605.bak',N'Success',DATEADD(DAY,-2,@Now),DATEADD(DAY,-2,DATEADD(MINUTE,15,@Now))),
 (N'Full',N'TabsanEduSphere_Full_20260603.bak',N'Success',DATEADD(DAY,-3,@Now),DATEADD(DAY,-3,DATEADD(MINUTE,52,@Now))),
 (N'TransactionLog',N'TabsanEduSphere_Log_20260605.trn',N'Failed',DATEADD(HOUR,-6,@Now),DATEADD(HOUR,-6,DATEADD(MINUTE,2,@Now))),
 (N'Full',N'TabsanEduSphere_Full_20260601.bak',N'Success',DATEADD(DAY,-5,@Now),DATEADD(DAY,-5,DATEADD(MINUTE,40,@Now))))
 bt([Type],[File],[Status],[Started],[Completed]);

INSERT INTO [policy_documents] ([Id],[Title],[Description],[Content],[Version],[Status],[Category],[AccessLevel],[PublishedAt],[CreatedAt])
SELECT NEWID(), p.[Title], p.[Description], N'# Policy Content', p.[Version], p.[Status], p.[Category], N'Internal', DATEADD(DAY,-p.[DaysAgo],@Now), DATEADD(DAY,-p.[DaysAgo]-30,@Now)
FROM (VALUES
 (N'Information Security Policy',N'ISO 27001 A.5.1 security directives',2,N'Published',N'Security',60),
 (N'Data Protection Policy',N'GDPR and PII data handling guidelines',1,N'Published',N'Compliance',45),
 (N'Acceptable Use Policy',N'IT resource usage terms for all users',3,N'Published',N'IT',90),
 (N'Incident Response Plan',N'ISO 27001 A.16 incident management',1,N'Draft',N'Security',15))
 p([Title],[Description],[Version],[Status],[Category],[DaysAgo]);

PRINT '=== ALL DEMO DATA SEEDED SUCCESSFULLY ===';
PRINT 'Tenants: 3 | Departments: 10 | Courses: 50 | Offerings: ~100 | Users: ~130 | Students: 61';
PRINT 'Every menu has 4-5+ records with proper Tenant→Campus→Department scoping.';

