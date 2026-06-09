/*
  Seed Core Data — Tabsan EduSphere v1.0
  
  Populates minimal required data: roles, tenants, campuses, departments,
  academic programs, courses, semesters, and baseline users.
  
  Default password for ALL users: EduSphere147
  
  INSTITUTES (3):
    University (2): BSCS 8 sem, BBA 8 sem, MSE 4 sem, Spanish Language 1 yr
    College (1):     ICS (2 years, class 11-12)
    School (0):      Science (10 years, class 1-10)
*/

SET NOCOUNT ON;
GO

USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- ═══════════════════════════════════════════════════════════════════
-- 1. DATABASE VERSION
-- ═══════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [Tabsan-EduSphere] WHERE [DemoKey] = N'db.version')
    INSERT INTO [Tabsan-EduSphere] ([Id], [DemoKey], [DemoValue], [CreatedAt])
    VALUES (NEWID(), N'db.version', N'1.0', @Now);
ELSE
    UPDATE [Tabsan-EduSphere] SET [DemoValue] = N'1.0', [UpdatedAt] = @Now
    WHERE [DemoKey] = N'db.version';

-- ═══════════════════════════════════════════════════════════════════
-- 2. ROLES (5 — no Parent role)
-- ═══════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [roles])
BEGIN
    SET IDENTITY_INSERT [roles] ON;
    INSERT INTO [roles] ([Id], [Name], [Description], [IsSystemRole]) VALUES
    (1, N'SuperAdmin', N'Full platform access — manages license and all settings.', 1),
    (2, N'Admin',      N'Institution-level administrator.', 1),
    (3, N'Faculty',    N'Teaching staff with course-level access.', 1),
    (4, N'Student',    N'Learner with self-service access.', 1),
    (5, N'Finance',    N'Financial officer with payments and fee access.', 1);
    SET IDENTITY_INSERT [roles] OFF;
END

-- ═══════════════════════════════════════════════════════════════════
-- 3. PASSWORD HASHES (Argon2id — plain password: EduSphere147)
-- ═══════════════════════════════════════════════════════════════════
DECLARE @SuperAdminPwd NVARCHAR(512) = N'argon2id:kot3aIW+GTcmK4Ji/jGD7BxrNOEh57PLaFMUZrZa5oM=:v+XYusZ0Eu9Xs8Sz/7Hi58z4SrS9KsJ/ynnr/iCkkSk=';
DECLARE @DefaultPwd    NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';

-- ═══════════════════════════════════════════════════════════════════
-- 4. TENANTS & CAMPUSES (3 institutes → 3 tenants → 3 campuses)
-- ═══════════════════════════════════════════════════════════════════
DECLARE @T_Uni UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @T_Col UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @T_Sch UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

DECLARE @C_Uni UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
DECLARE @C_Col UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB';
DECLARE @C_Sch UNIQUEIDENTIFIER = 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC';

-- Tenants
INSERT INTO [tenants] ([Id], [Code], [Name], [IsDeleted], [CreatedAt])
SELECT v.Id, v.Code, v.Name, 0, @Now
FROM (VALUES
    (@T_Uni, N'TABSAN-UNI', N'Tabsan University'),
    (@T_Col, N'TABSAN-COL', N'Tabsan College'),
    (@T_Sch, N'TABSAN-SCH', N'Tabsan School')
) v(Id, Code, Name)
WHERE NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = v.Id);

-- Campuses
INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsDeleted], [CreatedAt])
SELECT v.Id, v.TenantId, v.Code, v.Name, 0, @Now
FROM (VALUES
    (@C_Uni, @T_Uni, N'MAIN-UNI', N'University Main Campus'),
    (@C_Col, @T_Col, N'MAIN-COL', N'College Main Campus'),
    (@C_Sch, @T_Sch, N'MAIN-SCH', N'School Main Campus')
) v(Id, TenantId, Code, Name)
WHERE NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = v.Id);

-- ═══════════════════════════════════════════════════════════════════
-- 5. DEPARTMENTS
-- ═══════════════════════════════════════════════════════════════════
DECLARE @D_IT   UNIQUEIDENTIFIER = 'D0000001-0000-0000-0000-000000000001';
DECLARE @D_BUS  UNIQUEIDENTIFIER = 'D0000002-0000-0000-0000-000000000002';

-- Insert with all columns including TenantId/CampusId/InstitutionType if they exist
IF COL_LENGTH('departments', 'TenantId') IS NOT NULL
BEGIN
    INSERT INTO [departments] ([Id], [Name], [Code], [IsActive], [IsDeleted], [CreatedAt], [TenantId], [CampusId], [InstitutionType])
    SELECT v.Id, v.Name, v.Code, 1, 0, @Now, v.TenantId, v.CampusId, v.InstitutionType
    FROM (VALUES
        (@D_IT,  N'Information Technology', N'IT',  @T_Uni, @C_Uni, 2),
        (@D_BUS, N'Business Administration', N'BUS', @T_Uni, @C_Uni, 2)
    ) v(Id, Name, Code, TenantId, CampusId, InstitutionType)
    WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = v.Id);
END
ELSE
BEGIN
    INSERT INTO [departments] ([Id], [Name], [Code], [IsActive], [IsDeleted], [CreatedAt])
    SELECT v.Id, v.Name, v.Code, 1, 0, @Now
    FROM (VALUES
        (@D_IT,  N'Information Technology', N'IT'),
        (@D_BUS, N'Business Administration', N'BUS')
    ) v(Id, Name, Code)
    WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = v.Id);
END

-- ═══════════════════════════════════════════════════════════════════
-- 6. ACADEMIC PROGRAMS
-- ═══════════════════════════════════════════════════════════════════
DECLARE @P_BSCS    UNIQUEIDENTIFIER = 'A0000001-0000-0000-0000-000000000001';
DECLARE @P_BBA     UNIQUEIDENTIFIER = 'A0000002-0000-0000-0000-000000000002';
DECLARE @P_MSE     UNIQUEIDENTIFIER = 'A0000003-0000-0000-0000-000000000003';
DECLARE @P_SPANISH UNIQUEIDENTIFIER = 'A0000004-0000-0000-0000-000000000004';
DECLARE @P_ICS     UNIQUEIDENTIFIER = 'A0000005-0000-0000-0000-000000000005';
DECLARE @P_SCIENCE UNIQUEIDENTIFIER = 'A0000006-0000-0000-0000-000000000006';

INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [IsDeleted], [CreatedAt])
SELECT v.Id, v.Name, v.Code, v.DeptId, v.TotalSemesters, 1, 0, @Now
FROM (VALUES
    (@P_BSCS,    N'Bachelors of Science in Computer Sciences', N'BSCS',    @D_IT,  8),
    (@P_BBA,     N'Bachelor of Business Administration',       N'BBA',     @D_BUS, 8),
    (@P_MSE,     N'Masters in Computer Engineering',           N'MSE',     @D_IT,  4),
    (@P_SPANISH, N'Spanish Language Course',                    N'SPANISH', @D_IT,  2),
    (@P_ICS,     N'Intermediate in Computer Science',           N'ICS',     @D_IT,  2),
    (@P_SCIENCE, N'Science',                                    N'SCIENCE', @D_IT,  10)
) v(Id, Name, Code, DeptId, TotalSemesters)
WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = v.Id);

-- ═══════════════════════════════════════════════════════════════════
-- 7. COURSES (subjects per program per semester)
-- ═══════════════════════════════════════════════════════════════════
DECLARE @CourseData TABLE (Id UNIQUEIDENTIFIER, Title NVARCHAR(200), Code NVARCHAR(20), CreditHours INT, DeptId UNIQUEIDENTIFIER);

-- BSCS Courses (8 semesters, ~5 per semester)
INSERT INTO @CourseData VALUES
(NEWID(), N'Introduction to Programming',      N'CS101', 3, @D_IT),
(NEWID(), N'Calculus I',                       N'MTH101', 3, @D_IT),
(NEWID(), N'English Composition',              N'ENG101', 3, @D_IT),
(NEWID(), N'Introduction to Computing',        N'CS102', 3, @D_IT),
(NEWID(), N'Physics I',                        N'PHY101', 4, @D_IT),
(NEWID(), N'Object Oriented Programming',      N'CS201', 4, @D_IT),
(NEWID(), N'Calculus II',                      N'MTH201', 3, @D_IT),
(NEWID(), N'Digital Logic Design',             N'CS202', 4, @D_IT),
(NEWID(), N'Communication Skills',             N'ENG201', 3, @D_IT),
(NEWID(), N'Discrete Mathematics',             N'MTH202', 3, @D_IT),
(NEWID(), N'Data Structures',                  N'CS301', 4, @D_IT),
(NEWID(), N'Database Systems',                 N'CS302', 4, @D_IT),
(NEWID(), N'Probability & Statistics',         N'MTH301', 3, @D_IT),
(NEWID(), N'Computer Networks',                N'CS303', 3, @D_IT),
(NEWID(), N'Linear Algebra',                   N'MTH302', 3, @D_IT),
(NEWID(), N'Operating Systems',                N'CS401', 4, @D_IT),
(NEWID(), N'Software Engineering',             N'CS402', 4, @D_IT),
(NEWID(), N'Theory of Automata',               N'CS403', 3, @D_IT),
(NEWID(), N'Web Technologies',                 N'CS404', 3, @D_IT),
(NEWID(), N'Numerical Computing',              N'MTH401', 3, @D_IT),
(NEWID(), N'Artificial Intelligence',          N'CS501', 4, @D_IT),
(NEWID(), N'Compiler Construction',            N'CS502', 4, @D_IT),
(NEWID(), N'Computer Architecture',            N'CS503', 3, @D_IT),
(NEWID(), N'Human Computer Interaction',       N'CS504', 3, @D_IT),
(NEWID(), N'Professional Practices',           N'CS505', 2, @D_IT),
(NEWID(), N'Information Security',             N'CS601', 4, @D_IT),
(NEWID(), N'Distributed Computing',            N'CS602', 3, @D_IT),
(NEWID(), N'Mobile App Development',           N'CS603', 4, @D_IT),
(NEWID(), N'Data Warehousing',                 N'CS604', 3, @D_IT),
(NEWID(), N'Technical Writing',                N'ENG601', 3, @D_IT),
(NEWID(), N'Final Year Project I',             N'CS701', 3, @D_IT),
(NEWID(), N'Machine Learning',                 N'CS702', 4, @D_IT),
(NEWID(), N'Cloud Computing',                  N'CS703', 3, @D_IT),
(NEWID(), N'Software Project Management',      N'CS704', 3, @D_IT),
(NEWID(), N'Ethics in Computing',              N'CS705', 2, @D_IT),
(NEWID(), N'Final Year Project II',            N'CS801', 4, @D_IT),
(NEWID(), N'Data Science',                     N'CS802', 3, @D_IT),
(NEWID(), N'Network Security',                 N'CS803', 3, @D_IT),
(NEWID(), N'Entrepreneurship',                 N'MGT801', 3, @D_IT),
(NEWID(), N'Emerging Technologies',            N'CS804', 3, @D_IT);

-- BBA Courses (8 semesters, ~5 per semester)
INSERT INTO @CourseData VALUES
(NEWID(), N'Principles of Management',         N'MGT101', 3, @D_BUS),
(NEWID(), N'Financial Accounting',             N'ACC101', 3, @D_BUS),
(NEWID(), N'Business Mathematics',             N'MTH101B', 3, @D_BUS),
(NEWID(), N'Micro Economics',                  N'ECO101', 3, @D_BUS),
(NEWID(), N'Business Communication',           N'ENG101B', 3, @D_BUS),
(NEWID(), N'Organizational Behavior',          N'MGT201', 3, @D_BUS),
(NEWID(), N'Cost Accounting',                  N'ACC201', 3, @D_BUS),
(NEWID(), N'Macro Economics',                  N'ECO201', 3, @D_BUS),
(NEWID(), N'Business Statistics',              N'STT201', 3, @D_BUS),
(NEWID(), N'Marketing Management',             N'MKT201', 3, @D_BUS),
(NEWID(), N'Human Resource Management',        N'MGT301', 3, @D_BUS),
(NEWID(), N'Financial Management',             N'FIN301', 3, @D_BUS),
(NEWID(), N'Business Law',                     N'LAW301', 3, @D_BUS),
(NEWID(), N'Management Accounting',            N'ACC301', 3, @D_BUS),
(NEWID(), N'Consumer Behavior',                N'MKT301', 3, @D_BUS),
(NEWID(), N'Operations Management',            N'MGT401', 3, @D_BUS),
(NEWID(), N'Corporate Finance',                N'FIN401', 3, @D_BUS),
(NEWID(), N'International Business',           N'INT401', 3, @D_BUS),
(NEWID(), N'Brand Management',                 N'MKT401', 3, @D_BUS),
(NEWID(), N'Taxation Management',              N'TAX401', 3, @D_BUS),
(NEWID(), N'Strategic Management',             N'MGT501', 3, @D_BUS),
(NEWID(), N'Investment Banking',               N'FIN501', 3, @D_BUS),
(NEWID(), N'Supply Chain Management',          N'SCM501', 3, @D_BUS),
(NEWID(), N'Digital Marketing',                N'MKT501', 3, @D_BUS),
(NEWID(), N'Business Research Methods',        N'RES501', 3, @D_BUS),
(NEWID(), N'Project Management',               N'MGT601', 3, @D_BUS),
(NEWID(), N'Islamic Banking',                  N'FIN601', 3, @D_BUS),
(NEWID(), N'Business Ethics',                  N'ETH601', 3, @D_BUS),
(NEWID(), N'E-Commerce',                       N'ECM601', 3, @D_BUS),
(NEWID(), N'Leadership',                       N'MGT602', 3, @D_BUS),
(NEWID(), N'Change Management',                N'MGT701', 3, @D_BUS),
(NEWID(), N'Risk Management',                  N'FIN701', 3, @D_BUS),
(NEWID(), N'Total Quality Management',         N'TQM701', 3, @D_BUS),
(NEWID(), N'Entrepreneurship',                 N'ENT701', 3, @D_BUS),
(NEWID(), N'Business Analytics',               N'BAN701', 3, @D_BUS),
(NEWID(), N'Capstone Project',                 N'CAP801', 4, @D_BUS),
(NEWID(), N'Corporate Governance',             N'CGV801', 3, @D_BUS),
(NEWID(), N'Global Business Strategy',         N'GBS801', 3, @D_BUS),
(NEWID(), N'Innovation Management',            N'INN801', 3, @D_BUS),
(NEWID(), N'Financial Derivatives',            N'FIN801', 3, @D_BUS);

-- MSE Courses (4 semesters, ~5 per semester)
INSERT INTO @CourseData VALUES
(NEWID(), N'Advanced Computer Architecture',   N'MSE501', 3, @D_IT),
(NEWID(), N'Advanced Algorithms',              N'MSE502', 4, @D_IT),
(NEWID(), N'Research Methodology',             N'MSE503', 3, @D_IT),
(NEWID(), N'Network Design & Analysis',        N'MSE504', 3, @D_IT),
(NEWID(), N'Advanced Databases',               N'MSE505', 3, @D_IT),
(NEWID(), N'Parallel Computing',               N'MSE601', 3, @D_IT),
(NEWID(), N'Embedded Systems',                 N'MSE602', 4, @D_IT),
(NEWID(), N'Software Quality Assurance',       N'MSE603', 3, @D_IT),
(NEWID(), N'Cyber Security',                   N'MSE604', 3, @D_IT),
(NEWID(), N'Big Data Analytics',               N'MSE605', 3, @D_IT),
(NEWID(), N'Internet of Things',               N'MSE701', 3, @D_IT),
(NEWID(), N'Computer Vision',                  N'MSE702', 3, @D_IT),
(NEWID(), N'Natural Language Processing',      N'MSE703', 3, @D_IT),
(NEWID(), N'Thesis I',                         N'MSE704', 3, @D_IT),
(NEWID(), N'Blockchain Technology',            N'MSE705', 3, @D_IT),
(NEWID(), N'Thesis II',                        N'MSE801', 4, @D_IT),
(NEWID(), N'Deep Learning',                    N'MSE802', 3, @D_IT),
(NEWID(), N'Cloud & Edge Computing',           N'MSE803', 3, @D_IT),
(NEWID(), N'Robotics',                         N'MSE804', 3, @D_IT),
(NEWID(), N'Seminar on Emerging Tech',         N'MSE805', 2, @D_IT);

-- Spanish Language (1 year / 2 semesters)
INSERT INTO @CourseData VALUES
(NEWID(), N'Spanish Grammar I',                N'SPN101', 3, @D_IT),
(NEWID(), N'Spanish Vocabulary I',             N'SPN102', 3, @D_IT),
(NEWID(), N'Spanish Conversation I',           N'SPN103', 2, @D_IT),
(NEWID(), N'Spanish Grammar II',               N'SPN201', 3, @D_IT),
(NEWID(), N'Spanish Vocabulary II',            N'SPN202', 3, @D_IT),
(NEWID(), N'Spanish Conversation II',          N'SPN203', 2, @D_IT);

-- ICS (College - 2 years)
INSERT INTO @CourseData VALUES
(NEWID(), N'Computer Fundamentals',            N'ICS111', 3, @D_IT),
(NEWID(), N'Programming Fundamentals',         N'ICS112', 4, @D_IT),
(NEWID(), N'English I',                        N'ENG111', 3, @D_IT),
(NEWID(), N'Mathematics I',                    N'MTH111', 3, @D_IT),
(NEWID(), N'Physics',                          N'PHY111', 3, @D_IT),
(NEWID(), N'Object Oriented Programming',      N'ICS121', 4, @D_IT),
(NEWID(), N'Database Fundamentals',            N'ICS122', 3, @D_IT),
(NEWID(), N'English II',                       N'ENG121', 3, @D_IT),
(NEWID(), N'Mathematics II',                   N'MTH121', 3, @D_IT),
(NEWID(), N'Web Development',                  N'ICS123', 3, @D_IT);

-- School Science (Classes 1-10)
INSERT INTO @CourseData VALUES
(NEWID(), N'English',                          N'ENG001', 3, @D_IT),
(NEWID(), N'Mathematics',                      N'MTH001', 3, @D_IT),
(NEWID(), N'Science',                          N'SCI001', 3, @D_IT),
(NEWID(), N'Social Studies',                   N'SST001', 2, @D_IT),
(NEWID(), N'Computer Science',                 N'CS001',  2, @D_IT),
(NEWID(), N'Urdu',                             N'URD001', 2, @D_IT),
(NEWID(), N'Islamiat',                         N'ISL001', 2, @D_IT),
(NEWID(), N'Physical Education',               N'PE001',  1, @D_IT);

INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [IsActive], [IsDeleted], [CreatedAt])
SELECT d.Id, d.Title, d.Code, d.CreditHours, d.DeptId, 1, 0, @Now
FROM @CourseData d
WHERE NOT EXISTS (SELECT 1 FROM [courses] WHERE [Code] = d.Code);

-- ═══════════════════════════════════════════════════════════════════
-- 8. SEMESTERS
-- ═══════════════════════════════════════════════════════════════════
DECLARE @SemData TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(100), StartDate DATETIME2, EndDate DATETIME2);

-- University: 8 semesters for BSCS/BBA, 4 for MSE, 2 for Spanish
INSERT INTO @SemData VALUES
(NEWID(), N'Semester 1  (2026)',  '2026-01-15', '2026-06-15'),
(NEWID(), N'Semester 2  (2026)',  '2026-07-01', '2026-12-15'),
(NEWID(), N'Semester 3  (2027)',  '2027-01-15', '2027-06-15'),
(NEWID(), N'Semester 4  (2027)',  '2027-07-01', '2027-12-15'),
(NEWID(), N'Semester 5  (2028)',  '2028-01-15', '2028-06-15'),
(NEWID(), N'Semester 6  (2028)',  '2028-07-01', '2028-12-15'),
(NEWID(), N'Semester 7  (2029)',  '2029-01-15', '2029-06-15'),
(NEWID(), N'Semester 8  (2029)',  '2029-07-01', '2029-12-15');

-- College: 2 years (ICS year 1, year 2)
INSERT INTO @SemData VALUES
(NEWID(), N'ICS Year 1 (2026)',   '2026-04-01', '2027-03-31'),
(NEWID(), N'ICS Year 2 (2027)',   '2027-04-01', '2028-03-31');

-- School: 10 classes
INSERT INTO @SemData VALUES
(NEWID(), N'Class 1  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 2  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 3  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 4  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 5  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 6  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 7  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 8  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 9  (2026)',     '2026-04-01', '2027-03-31'),
(NEWID(), N'Class 10 (2026)',     '2026-04-01', '2027-03-31');

INSERT INTO [semesters] ([Id], [Name], [StartDate], [EndDate], [IsClosed], [IsDeleted], [CreatedAt])
SELECT d.Id, d.Name, d.StartDate, d.EndDate, 0, 0, @Now
FROM @SemData d
WHERE NOT EXISTS (SELECT 1 FROM [semesters] WHERE [Name] = d.Name);

-- Store semester IDs for later use
DECLARE @SemUni1  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 1  (2026)');
DECLARE @SemUni2  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 2  (2026)');
DECLARE @SemUni3  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 3  (2027)');
DECLARE @SemUni4  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 4  (2027)');
DECLARE @SemUni5  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 5  (2028)');
DECLARE @SemUni6  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 6  (2028)');
DECLARE @SemUni7  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 7  (2029)');
DECLARE @SemUni8  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Semester 8  (2029)');
DECLARE @SemICS1  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'ICS Year 1 (2026)');
DECLARE @SemICS2  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'ICS Year 2 (2027)');
DECLARE @SemSch1  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Class 1  (2026)');
DECLARE @SemSch10 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] = N'Class 10 (2026)');

-- ═══════════════════════════════════════════════════════════════════
-- 9. CORE USERS (baseline for login)
-- ═══════════════════════════════════════════════════════════════════
DECLARE @R_SuperAdmin INT = 1, @R_Admin INT = 2, @R_Faculty INT = 3, @R_Student INT = 4, @R_Finance INT = 5;

DECLARE @CoreUsers TABLE (
    Id UNIQUEIDENTIFIER, Username NVARCHAR(100), Email NVARCHAR(256),
    FullName NVARCHAR(200), RoleId INT, DeptId UNIQUEIDENTIFIER NULL,
    TenantId UNIQUEIDENTIFIER NULL, CampusId UNIQUEIDENTIFIER NULL, InstitutionType INT NULL
);

-- SuperAdmin (global, no tenant/campus)
INSERT INTO @CoreUsers VALUES
('66666666-6666-6666-6666-666666666601', N'superadmin', N'superadmin@tabsan.local', N'Super Admin', @R_SuperAdmin, NULL, NULL, NULL, NULL);

-- University Admins
INSERT INTO @CoreUsers VALUES
('66666666-6666-6666-6666-666666666602', N'admin.uni', N'admin.uni@tabsan.local', N'University Admin', @R_Admin, @D_IT, @T_Uni, @C_Uni, 2);

-- College Admin
INSERT INTO @CoreUsers VALUES
('66666666-6666-6666-6666-666666666603', N'admin.col', N'admin.col@tabsan.local', N'College Admin', @R_Admin, @D_IT, @T_Col, @C_Col, 1);

-- School Admin
INSERT INTO @CoreUsers VALUES
('66666666-6666-6666-6666-666666666604', N'admin.sch', N'admin.sch@tabsan.local', N'School Admin', @R_Admin, @D_IT, @T_Sch, @C_Sch, 0);

-- Faculty (one per department)
INSERT INTO @CoreUsers VALUES
('66666666-6666-6666-6666-666666666605', N'faculty.uni', N'faculty.uni@tabsan.local', N'Dr. Ahmad Khan', @R_Faculty, @D_IT, @T_Uni, @C_Uni, 2),
('66666666-6666-6666-6666-666666666606', N'faculty.col', N'faculty.col@tabsan.local', N'Prof. Rashid Iqbal', @R_Faculty, @D_IT, @T_Col, @C_Col, 1),
('66666666-6666-6666-6666-666666666607', N'faculty.sch', N'faculty.sch@tabsan.local', N'Ms. Amna Javed', @R_Faculty, @D_IT, @T_Sch, @C_Sch, 0);

-- Students (one per program)
INSERT INTO @CoreUsers VALUES
('66666666-6666-6666-6666-666666666608', N'student.uni', N'student.uni@tabsan.local', N'Ali Hassan', @R_Student, @D_IT, @T_Uni, @C_Uni, 2),
('66666666-6666-6666-6666-666666666609', N'student.col', N'student.col@tabsan.local', N'Arslan Mehmood', @R_Student, @D_IT, @T_Col, @C_Col, 1),
('66666666-6666-6666-6666-666666666610', N'student.sch', N'student.sch@tabsan.local', N'Ahmed Raza', @R_Student, @D_IT, @T_Sch, @C_Sch, 0);

-- Finance Officers
INSERT INTO @CoreUsers VALUES
('66666666-6666-6666-6666-666666666611', N'finance.uni', N'finance.uni@tabsan.local', N'Finance Officer Uni', @R_Finance, @D_IT, @T_Uni, @C_Uni, 2),
('66666666-6666-6666-6666-666666666612', N'finance.col', N'finance.col@tabsan.local', N'Finance Officer Col', @R_Finance, @D_IT, @T_Col, @C_Col, 1),
('66666666-6666-6666-6666-666666666613', N'finance.sch', N'finance.sch@tabsan.local', N'Finance Officer Sch', @R_Finance, @D_IT, @T_Sch, @C_Sch, 0);

-- Insert/update users
MERGE [users] AS t
USING @CoreUsers AS s ON t.[Id] = s.[Id]
WHEN MATCHED THEN UPDATE SET
    t.[Username] = s.[Username], t.[Email] = s.[Email], t.[FullName] = s.[FullName],
    t.[RoleId] = s.[RoleId], t.[DepartmentId] = s.[DeptId],
    t.[TenantId] = s.[TenantId], t.[CampusId] = s.[CampusId],
    t.[InstitutionType] = ISNULL(s.[InstitutionType], t.[InstitutionType]),
    t.[PasswordHash] = CASE WHEN s.RoleId = @R_SuperAdmin THEN @SuperAdminPwd ELSE @DefaultPwd END,
    t.[IsActive] = 1, t.[IsDeleted] = 0, t.[DeletedAt] = NULL, t.[UpdatedAt] = @Now
WHEN NOT MATCHED THEN INSERT
    ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
    VALUES (s.[Id],s.[Username],s.[Email],s.[FullName],
            CASE WHEN s.RoleId = @R_SuperAdmin THEN @SuperAdminPwd ELSE @DefaultPwd END,
            s.[RoleId],s.[DeptId],s.[TenantId],s.[CampusId],s.[InstitutionType],1,@Now,0);

-- Department-specific IT dept for College and School
DECLARE @D_IT_Col UNIQUEIDENTIFIER = 'D0000003-0000-0000-0000-000000000003';
DECLARE @D_IT_Sch UNIQUEIDENTIFIER = 'D0000004-0000-0000-0000-000000000004';

IF COL_LENGTH('departments', 'TenantId') IS NOT NULL
BEGIN
    INSERT INTO [departments] ([Id], [Name], [Code], [IsActive], [IsDeleted], [CreatedAt], [TenantId], [CampusId], [InstitutionType])
    SELECT v.Id, v.Name, v.Code, 1, 0, @Now, v.TenantId, v.CampusId, v.InstitutionType
    FROM (VALUES
        (@D_IT_Col, N'Information Technology', N'IT-COL', @T_Col, @C_Col, 1),
        (@D_IT_Sch, N'Science Department',     N'SCI',   @T_Sch, @C_Sch, 0)
    ) v(Id, Name, Code, TenantId, CampusId, InstitutionType)
    WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = v.Id);
END
ELSE
BEGIN
    INSERT INTO [departments] ([Id], [Name], [Code], [IsActive], [IsDeleted], [CreatedAt])
    SELECT v.Id, v.Name, v.Code, 1, 0, @Now
    FROM (VALUES
        (@D_IT_Col, N'Information Technology', N'IT-COL'),
        (@D_IT_Sch, N'Science Department',     N'SCI')
    ) v(Id, Name, Code)
    WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = v.Id);
END

-- Update department TenantId/CampusId/InstitutionType if columns exist
IF COL_LENGTH('departments', 'TenantId') IS NOT NULL
BEGIN
    UPDATE [departments] SET [TenantId] = @T_Col WHERE [Id] = @D_IT_Col AND [TenantId] IS NULL;
    UPDATE [departments] SET [TenantId] = @T_Sch WHERE [Id] = @D_IT_Sch AND [TenantId] IS NULL;
END
IF COL_LENGTH('departments', 'CampusId') IS NOT NULL
BEGIN
    UPDATE [departments] SET [CampusId] = @C_Col WHERE [Id] = @D_IT_Col AND [CampusId] IS NULL;
    UPDATE [departments] SET [CampusId] = @C_Sch WHERE [Id] = @D_IT_Sch AND [CampusId] IS NULL;
END
IF COL_LENGTH('departments', 'InstitutionType') IS NOT NULL
BEGIN
    UPDATE [departments] SET [InstitutionType] = 1 WHERE [Id] = @D_IT_Col AND [InstitutionType] IS NULL;
    UPDATE [departments] SET [InstitutionType] = 0 WHERE [Id] = @D_IT_Sch AND [InstitutionType] IS NULL;
END

-- Update programs for College & School departments (they were inserted under @D_IT)
IF EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @P_ICS AND [DepartmentId] = @D_IT)
    UPDATE [academic_programs] SET [DepartmentId] = @D_IT_Col WHERE [Id] = @P_ICS;

IF EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @P_SCIENCE AND [DepartmentId] = @D_IT)
    UPDATE [academic_programs] SET [DepartmentId] = @D_IT_Sch WHERE [Id] = @P_SCIENCE;

-- Update faculty/student core users to correct departments
UPDATE [users] SET [DepartmentId] = @D_IT_Col WHERE [Username] IN (N'faculty.col', N'student.col');
UPDATE [users] SET [DepartmentId] = @D_IT_Sch WHERE [Username] IN (N'faculty.sch', N'student.sch');
UPDATE [users] SET [DepartmentId] = @D_IT_Col WHERE [Username] = N'admin.col';
UPDATE [users] SET [DepartmentId] = @D_IT_Sch WHERE [Username] = N'admin.sch';

-- ═══════════════════════════════════════════════════════════════════
-- 10. COURSE OFFERINGS (course + semester pairs)
-- ═══════════════════════════════════════════════════════════════════
INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [MaxEnrollment], [IsOpen], [IsDeleted], [CreatedAt])
SELECT NEWID(), c.Id,
    CASE
        -- School: all to Class 1
        WHEN c.[Code] LIKE N'ENG001' OR c.[Code] LIKE N'MTH001' OR c.[Code] LIKE N'SCI001'
          OR c.[Code] LIKE N'SST001' OR c.[Code] LIKE N'CS001'  OR c.[Code] LIKE N'URD001'
          OR c.[Code] LIKE N'ISL001' OR c.[Code] LIKE N'PE001'  THEN @SemSch1
        -- College ICS Year 1
        WHEN c.[Code] LIKE N'ICS11%' OR c.[Code] LIKE N'ENG111' OR c.[Code] LIKE N'MTH111' OR c.[Code] LIKE N'PHY111' THEN @SemICS1
        -- College ICS Year 2
        WHEN c.[Code] LIKE N'ICS12%' OR c.[Code] LIKE N'ENG121' OR c.[Code] LIKE N'MTH121' THEN @SemICS2
        -- Spanish Sem 1
        WHEN c.[Code] LIKE N'SPN1%' THEN @SemUni1
        -- Spanish Sem 2
        WHEN c.[Code] LIKE N'SPN2%' THEN @SemUni2
        -- MSE Semester mapping (MSE5xx→1, MSE6xx→2, MSE7xx→3, MSE8xx→4)
        WHEN c.[Code] LIKE N'MSE5%' THEN @SemUni1
        WHEN c.[Code] LIKE N'MSE6%' THEN @SemUni2
        WHEN c.[Code] LIKE N'MSE7%' THEN @SemUni3
        WHEN c.[Code] LIKE N'MSE8%' THEN @SemUni4
        -- BSCS/BBA: digit after letters determines semester
        WHEN c.[Code] LIKE N'CS1%' OR c.[Code] LIKE N'MTH1%' OR c.[Code] LIKE N'ENG1%' OR c.[Code] LIKE N'PHY1%'
          OR c.[Code] LIKE N'MGT1%' OR c.[Code] LIKE N'ACC1%' OR c.[Code] LIKE N'ECO1%' OR c.[Code] LIKE N'STT1%'
          OR c.[Code] LIKE N'MKT1%' THEN @SemUni1
        WHEN c.[Code] LIKE N'CS2%' OR c.[Code] LIKE N'MTH2%' OR c.[Code] LIKE N'ENG2%'
          OR c.[Code] LIKE N'MGT2%' OR c.[Code] LIKE N'ACC2%' OR c.[Code] LIKE N'ECO2%' OR c.[Code] LIKE N'STT2%'
          OR c.[Code] LIKE N'MKT2%' OR c.[Code] LIKE N'FIN2%' THEN @SemUni2
        WHEN c.[Code] LIKE N'CS3%' OR c.[Code] LIKE N'MTH3%'
          OR c.[Code] LIKE N'MGT3%' OR c.[Code] LIKE N'ACC3%' OR c.[Code] LIKE N'FIN3%' OR c.[Code] LIKE N'LAW3%'
          OR c.[Code] LIKE N'MKT3%' THEN @SemUni3
        WHEN c.[Code] LIKE N'CS4%' OR c.[Code] LIKE N'MTH4%'
          OR c.[Code] LIKE N'MGT4%' OR c.[Code] LIKE N'FIN4%' OR c.[Code] LIKE N'INT4%' OR c.[Code] LIKE N'MKT4%'
          OR c.[Code] LIKE N'TAX4%' THEN @SemUni4
        WHEN c.[Code] LIKE N'CS5%' OR c.[Code] LIKE N'ENG5%' OR c.[Code] LIKE N'MGT5%' OR c.[Code] LIKE N'FIN5%'
          OR c.[Code] LIKE N'SCM5%' OR c.[Code] LIKE N'MKT5%' OR c.[Code] LIKE N'RES5%' THEN @SemUni5
        WHEN c.[Code] LIKE N'CS6%' OR c.[Code] LIKE N'ENG6%' OR c.[Code] LIKE N'MGT6%' OR c.[Code] LIKE N'FIN6%'
          OR c.[Code] LIKE N'ETH6%' OR c.[Code] LIKE N'ECM6%' THEN @SemUni6
        WHEN c.[Code] LIKE N'CS7%' OR c.[Code] LIKE N'MGT7%' OR c.[Code] LIKE N'FIN7%' OR c.[Code] LIKE N'TQM7%'
          OR c.[Code] LIKE N'ENT7%' OR c.[Code] LIKE N'BAN7%' THEN @SemUni7
        WHEN c.[Code] LIKE N'CS8%' OR c.[Code] LIKE N'MGT8%' OR c.[Code] LIKE N'CAP8%' OR c.[Code] LIKE N'CGV8%'
          OR c.[Code] LIKE N'GBS8%' OR c.[Code] LIKE N'INN8%' OR c.[Code] LIKE N'FIN8%' THEN @SemUni8
        ELSE @SemUni1
    END,
    30, 1, 0, @Now
FROM [courses] c
WHERE NOT EXISTS (SELECT 1 FROM [course_offerings] WHERE [CourseId] = c.[Id]);

PRINT CONCAT('Course offerings created: ', @@ROWCOUNT);

PRINT '02-Seed-Core.sql completed successfully.';
GO
