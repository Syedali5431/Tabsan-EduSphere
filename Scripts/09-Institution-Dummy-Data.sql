-- =====================================================================================
-- 09-Institution-Dummy-Data.sql  —  Tabsan EduSphere
-- Balanced demo data for all three institution types + institution consistency fixes.
--
-- Run against [Tabsan-EduSphere] AFTER 03-FullDummyData.sql (and the optional
-- student-journey / 08 scripts). Safe to re-run: every insert is keyed on a code,
-- username, registration number or receipt number and is skipped if already present.
--
-- Part A  Consistency fixes
--   A1  Student accounts take the institution type / tenant / campus / department of
--       their current student profile (the lifecycle-demo students were still tagged
--       School while studying in a University department).
--   A2  Every course offering gets a teacher from its own department (or, failing that,
--       its own institution type) so Gradebook / Enter Attendance / Enter Results work.
--   A3  The demo institutes' classes / semesters and offerings are opened for enrollment
--       (03 seeds them closed, so every enrollment attempt was rejected).
--
-- Part B  New demo data (default password for every new account: EduSphere147)
--   School   (TABSAN-SCH)  Arts & Humanities, Commerce        Class 9-10
--   College  (TABSAN-COL)  Pre-Medical, Pre-Engineering, Commerce  Class 11-12
--   University (TABSAN-UNI) Economics                         Semester 1-2
--   For each: department, program, courses per level, offerings, a teacher, 10 students
--   per level, enrollments, published results, attendance and fee receipts, plus 5 newly
--   admitted students per department who are not enrolled yet (to try bulk enrollment).
-- =====================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

DECLARE @Now    DATETIME2 = SYSUTCDATETIME();
DECLARE @DefPwd NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @RoleFaculty INT = 3, @RoleStudent INT = 4;

DECLARE @Tenants TABLE (InstitutionType INT PRIMARY KEY, TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER);
INSERT INTO @Tenants
SELECT v.it, t.Id, (SELECT TOP 1 c.Id FROM campuses c WHERE c.TenantId = t.Id AND c.IsDeleted = 0 ORDER BY c.Code)
FROM (VALUES (0, N'TABSAN-UNI'), (1, N'TABSAN-SCH'), (2, N'TABSAN-COL')) v(it, code)
JOIN tenants t ON t.Code = v.code AND t.IsDeleted = 0;

IF (SELECT COUNT(*) FROM @Tenants WHERE CampusId IS NOT NULL) <> 3
BEGIN
    RAISERROR('09-Institution-Dummy-Data: tenants TABSAN-UNI/SCH/COL with a campus are required (run 02 and 03 first).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

DECLARE @FinanceUserId UNIQUEIDENTIFIER =
    (SELECT TOP 1 u.Id FROM users u JOIN roles r ON r.Id = u.RoleId WHERE r.Name IN (N'Finance', N'SuperAdmin') AND u.IsDeleted = 0 ORDER BY CASE r.Name WHEN N'Finance' THEN 0 ELSE 1 END, u.Username);

-- =====================================================================================
-- Part A1: student accounts follow their student profile's department
-- =====================================================================================
UPDATE u
SET u.InstitutionType = d.InstitutionType,
    u.TenantId        = d.TenantId,
    u.CampusId        = d.CampusId,
    u.DepartmentId    = d.Id,
    u.UpdatedAt       = @Now
FROM users u
JOIN student_profiles s ON s.UserId = u.Id AND s.IsDeleted = 0
JOIN departments d ON d.Id = s.DepartmentId
WHERE u.InstitutionType IS NULL OR u.InstitutionType <> d.InstitutionType
   OR u.TenantId IS NULL OR u.TenantId <> d.TenantId
   OR u.CampusId IS NULL OR u.CampusId <> d.CampusId
   OR u.DepartmentId IS NULL OR u.DepartmentId <> d.Id;
PRINT CONCAT('A1 student accounts aligned with their department: ', @@ROWCOUNT);

-- =====================================================================================
-- Part B: new departments, programs, courses, offerings, teachers, students and activity
-- =====================================================================================
DECLARE @Dept TABLE (
    Code NVARCHAR(50) PRIMARY KEY, Name NVARCHAR(200), InstitutionType INT,
    ProgCode NVARCHAR(50), ProgName NVARCHAR(200), TotalSemesters INT,
    Prefix NVARCHAR(10), StudentPrefix NVARCHAR(10), RegPrefix NVARCHAR(10),
    FirstLevel INT, LastLevel INT, LevelLabel NVARCHAR(20),
    FacultyUser NVARCHAR(100), FacultyName NVARCHAR(200), Fee DECIMAL(18,2));
INSERT INTO @Dept VALUES
 (N'ARTS-SCH', N'Arts & Humanities',   1, N'ARTS',     N'Arts',                         10, N'ART', N'arts',   N'ARTS',   9, 10, N'Class',            N'faculty.sch3', N'Ayesha Malik',   1350),
 (N'COM-SCH',  N'Commerce',            1, N'COMMERCE', N'Commerce',                     10, N'COM', N'comm',   N'COMM',   9, 10, N'Class',            N'faculty.sch4', N'Bilal Ahmed',    1400),
 (N'PMED-COL', N'Pre-Medical',         2, N'FSC-PM',   N'FSc Pre-Medical',               2, N'PMD', N'fscpm',  N'FSCPM', 11, 12, N'Class',            N'faculty.col3', N'Sara Qureshi',   2800),
 (N'PENG-COL', N'Pre-Engineering',     2, N'FSC-PE',   N'FSc Pre-Engineering',           2, N'PEN', N'fscpe',  N'FSCPE', 11, 12, N'Class',            N'faculty.col4', N'Usman Tariq',    2800),
 (N'ICOM-COL', N'Commerce (College)',  2, N'ICOM',     N'Intermediate in Commerce',      2, N'ICM', N'icom',   N'ICOM',  11, 12, N'Class',            N'faculty.col5', N'Hina Raza',      2600),
 (N'ECO',      N'Economics',           0, N'BSECO',    N'BS Economics',                  8, N'ECO', N'bseco',  N'BSECO',  1,  2, N'BSECO Semester',   N'faculty.eco1', N'Kamran Siddiqui',4500);

DECLARE @Subject TABLE (Prefix NVARCHAR(10), Seq INT, Title NVARCHAR(200), Credit INT);
INSERT INTO @Subject VALUES
 (N'ART',1,N'English Literature',3),(N'ART',2,N'History',3),(N'ART',3,N'Geography',3),(N'ART',4,N'Civics',2),
 (N'COM',1,N'Principles of Commerce',3),(N'COM',2,N'Book Keeping',3),(N'COM',3,N'Business Mathematics',3),(N'COM',4,N'Economics',2),
 (N'PMD',1,N'Biology',4),(N'PMD',2,N'Chemistry',4),(N'PMD',3,N'Physics',4),(N'PMD',4,N'English',3),
 (N'PEN',1,N'Mathematics',4),(N'PEN',2,N'Physics',4),(N'PEN',3,N'Chemistry',4),(N'PEN',4,N'English',3),
 (N'ICM',1,N'Principles of Accounting',4),(N'ICM',2,N'Economics',3),(N'ICM',3,N'Business Mathematics',3),(N'ICM',4,N'English',3),
 (N'ECO',1,N'Microeconomics',3),(N'ECO',2,N'Macroeconomics',3),(N'ECO',3,N'Statistics for Economics',3),(N'ECO',4,N'Pakistan Economy',3);

-- B1 departments
INSERT INTO departments (Id, Name, Code, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType)
SELECT NEWID(), d.Name, d.Code, 1, 0, @Now, t.TenantId, t.CampusId, d.InstitutionType
FROM @Dept d JOIN @Tenants t ON t.InstitutionType = d.InstitutionType
WHERE NOT EXISTS (SELECT 1 FROM departments x WHERE x.Code = d.Code);
PRINT CONCAT('B1 departments added: ', @@ROWCOUNT);

-- B2 programs
INSERT INTO academic_programs (Id, Name, Code, DepartmentId, TotalSemesters, IsActive, CreatedAt, IsDeleted, MaxCreditLoadPerSemester)
SELECT NEWID(), d.ProgName, d.ProgCode, x.Id, d.TotalSemesters, 1, @Now, 0, 18
FROM @Dept d JOIN departments x ON x.Code = d.Code
WHERE NOT EXISTS (SELECT 1 FROM academic_programs p WHERE p.Code = d.ProgCode);
PRINT CONCAT('B2 programs added: ', @@ROWCOUNT);

-- B3 semesters for the new University program (School/College reuse their existing Class rows)
INSERT INTO semesters (Id, Name, StartDate, EndDate, IsClosed, CreatedAt, IsDeleted)
SELECT NEWID(), CONCAT(d.LevelLabel, N' ', lv.n), DATEFROMPARTS(2025, CASE lv.n WHEN 1 THEN 2 ELSE 9 END, 1), DATEFROMPARTS(2025 + lv.n - 1, CASE lv.n WHEN 1 THEN 6 ELSE 1 END, 30), 0, @Now, 0
FROM @Dept d CROSS APPLY (VALUES (d.FirstLevel), (d.LastLevel)) lv(n)
WHERE d.InstitutionType = 0
  AND NOT EXISTS (SELECT 1 FROM semesters s WHERE s.Name = CONCAT(d.LevelLabel, N' ', lv.n) AND s.IsDeleted = 0);

-- Level -> semester row. School/College classes: the "Class N" row their institution's offerings already use.
DECLARE @LevelSem TABLE (Code NVARCHAR(50), Lvl INT, SemesterId UNIQUEIDENTIFIER, PRIMARY KEY (Code, Lvl));
INSERT INTO @LevelSem
SELECT d.Code, lv.n,
       COALESCE(
         (SELECT TOP 1 s.Id FROM semesters s
           WHERE s.Name = CONCAT(d.LevelLabel, N' ', lv.n) AND s.IsDeleted = 0
           ORDER BY (SELECT COUNT(*) FROM course_offerings o WHERE o.SemesterId = s.Id AND o.InstitutionType = d.InstitutionType) DESC, s.CreatedAt),
         NULL)
FROM @Dept d CROSS APPLY (VALUES (d.FirstLevel), (d.LastLevel)) lv(n);

IF EXISTS (SELECT 1 FROM @LevelSem WHERE SemesterId IS NULL)
BEGIN
    RAISERROR('09-Institution-Dummy-Data: a Class/Semester row is missing (expected Class 9-12 from 03-FullDummyData.sql).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

-- B4 courses: one course per subject per level, e.g. ART-901 "English Literature (Class 9)"
DECLARE @Course TABLE (Code NVARCHAR(50) PRIMARY KEY, DeptCode NVARCHAR(50), Lvl INT, Title NVARCHAR(200), Credit INT);
INSERT INTO @Course
SELECT CONCAT(d.Prefix, N'-', lv.n, RIGHT(CONCAT(N'0', s.Seq), 2)), d.Code, lv.n,
       CONCAT(s.Title, N' (', d.LevelLabel, N' ', lv.n, N')'), s.Credit
FROM @Dept d
JOIN @Subject s ON s.Prefix = d.Prefix
CROSS APPLY (VALUES (d.FirstLevel), (d.LastLevel)) lv(n);

INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType, CourseType, GradingType, HasSemesters)
SELECT NEWID(), c.Title, c.Code, c.Credit, x.Id, 1, 0, @Now, x.TenantId, x.CampusId, x.InstitutionType, 1, N'GPA', 1
FROM @Course c JOIN departments x ON x.Code = c.DeptCode
WHERE NOT EXISTS (SELECT 1 FROM courses k WHERE k.Code = c.Code AND k.IsDeleted = 0);
PRINT CONCAT('B4 courses added: ', @@ROWCOUNT);

-- B5 teachers (one per new department) + department assignment
INSERT INTO users (Id, Username, Email, FullName, PasswordHash, RoleId, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType, MustChangePassword)
SELECT NEWID(), d.FacultyUser, CONCAT(d.FacultyUser, N'@tabsan.edu'), d.FacultyName, @DefPwd, @RoleFaculty, x.Id, 1, 0, @Now, x.TenantId, x.CampusId, x.InstitutionType, 0
FROM @Dept d JOIN departments x ON x.Code = d.Code
WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.Username = d.FacultyUser);
PRINT CONCAT('B5 teachers added: ', @@ROWCOUNT);

INSERT INTO faculty_department_assignments (Id, FacultyUserId, DepartmentId, AssignedAt, CreatedAt)
SELECT NEWID(), u.Id, x.Id, @Now, @Now
FROM @Dept d JOIN departments x ON x.Code = d.Code JOIN users u ON u.Username = d.FacultyUser
WHERE NOT EXISTS (SELECT 1 FROM faculty_department_assignments f WHERE f.FacultyUserId = u.Id AND f.DepartmentId = x.Id AND f.RemovedAt IS NULL);

-- Institution admins manage the new departments of their own institution type.
INSERT INTO admin_department_assignments (Id, AdminUserId, DepartmentId, AssignedAt, CreatedAt)
SELECT NEWID(), u.Id, x.Id, @Now, @Now
FROM @Dept d
JOIN departments x ON x.Code = d.Code
JOIN users u ON u.Username = CASE d.InstitutionType WHEN 0 THEN N'admin.uni' WHEN 1 THEN N'admin.sch' ELSE N'admin.col' END
WHERE NOT EXISTS (SELECT 1 FROM admin_department_assignments a WHERE a.AdminUserId = u.Id AND a.DepartmentId = x.Id AND a.RemovedAt IS NULL);

-- testadmin manages every University department (see 08-Create-Test-Admin-User.sql).
INSERT INTO admin_department_assignments (Id, AdminUserId, DepartmentId, AssignedAt, CreatedAt)
SELECT NEWID(), u.Id, x.Id, @Now, @Now
FROM @Dept d JOIN departments x ON x.Code = d.Code AND x.InstitutionType = 0
JOIN users u ON u.Username = N'testadmin'
WHERE NOT EXISTS (SELECT 1 FROM admin_department_assignments a WHERE a.AdminUserId = u.Id AND a.DepartmentId = x.Id AND a.RemovedAt IS NULL);

-- B6 offerings: each new course in its level's semester/class, taught by the department's teacher
INSERT INTO course_offerings (Id, CourseId, SemesterId, FacultyUserId, MaxEnrollment, IsOpen, CreatedAt, IsDeleted, InstitutionType, TenantId, CampusId)
SELECT NEWID(), k.Id, ls.SemesterId, u.Id, 40, CASE WHEN c.Lvl = d.LastLevel THEN 1 ELSE 0 END, @Now, 0, k.InstitutionType, k.TenantId, k.CampusId
FROM @Course c
JOIN @Dept d ON d.Code = c.DeptCode
JOIN courses k ON k.Code = c.Code AND k.IsDeleted = 0
JOIN @LevelSem ls ON ls.Code = c.DeptCode AND ls.Lvl = c.Lvl
JOIN users u ON u.Username = d.FacultyUser
WHERE NOT EXISTS (SELECT 1 FROM course_offerings o WHERE o.CourseId = k.Id AND o.SemesterId = ls.SemesterId AND o.IsDeleted = 0);
PRINT CONCAT('B6 offerings added: ', @@ROWCOUNT);

-- B7 students: 10 per level per new program, with realistic names
DECLARE @First TABLE (n INT PRIMARY KEY, Name NVARCHAR(50));
INSERT INTO @First VALUES (1,N'Ali'),(2,N'Fatima'),(3,N'Hamza'),(4,N'Zainab'),(5,N'Omar'),(6,N'Maryam'),(7,N'Bilal'),(8,N'Ayesha'),(9,N'Hassan'),(10,N'Sana');
DECLARE @Last TABLE (n INT PRIMARY KEY, Name NVARCHAR(50));
INSERT INTO @Last VALUES (0,N'Khan'),(1,N'Ahmed'),(2,N'Hussain'),(3,N'Malik'),(4,N'Sheikh'),(5,N'Chaudhry'),(6,N'Butt'),(7,N'Raza');

DECLARE @Stu TABLE (Username NVARCHAR(100) PRIMARY KEY, RegNo NVARCHAR(50), FullName NVARCHAR(200), DeptCode NVARCHAR(50), Lvl INT, Seq INT);
INSERT INTO @Stu
SELECT CONCAT(d.StudentPrefix, lv.n, N's', s.n),
       CONCAT(d.RegPrefix, N'-', lv.n, N'-', RIGHT(CONCAT(N'0', s.n), 2)),
       CONCAT(f.Name, N' ', l.Name),
       d.Code, lv.n, s.n
FROM @Dept d
CROSS APPLY (VALUES (d.FirstLevel), (d.LastLevel)) lv(n)
CROSS JOIN @First s
JOIN @First f ON f.n = s.n
JOIN @Last l ON l.n = (s.n + lv.n + LEN(d.Code)) % 8;

INSERT INTO users (Id, Username, Email, FullName, PasswordHash, RoleId, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType, MustChangePassword)
SELECT NEWID(), s.Username, CONCAT(s.Username, N'@tabsan.edu'), s.FullName, @DefPwd, @RoleStudent, x.Id, 1, 0, @Now, x.TenantId, x.CampusId, x.InstitutionType, 0
FROM @Stu s JOIN departments x ON x.Code = s.DeptCode
WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.Username = s.Username);
PRINT CONCAT('B7 student accounts added: ', @@ROWCOUNT);

INSERT INTO student_profiles (Id, UserId, RegistrationNumber, ProgramId, DepartmentId, AdmissionDate, Cgpa, CurrentSemesterGpa, CurrentSemesterNumber, Status, CreatedAt, IsDeleted)
SELECT NEWID(), u.Id, s.RegNo, p.Id, x.Id,
       DATEFROMPARTS(2025 - (s.Lvl - d.FirstLevel), CASE WHEN d.InstitutionType = 0 THEN 2 ELSE 4 END, 1),
       CASE WHEN d.InstitutionType = 0 THEN 2.6 + (s.Seq % 5) * 0.25 ELSE 62 + (s.Seq * 3) % 33 END,
       CASE WHEN d.InstitutionType = 0 THEN 2.5 + (s.Seq % 6) * 0.25 ELSE 60 + (s.Seq * 7) % 36 END,
       s.Lvl, 1, @Now, 0
FROM @Stu s
JOIN @Dept d ON d.Code = s.DeptCode
JOIN departments x ON x.Code = s.DeptCode
JOIN academic_programs p ON p.Code = d.ProgCode
JOIN users u ON u.Username = s.Username
WHERE NOT EXISTS (SELECT 1 FROM student_profiles sp WHERE sp.UserId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM student_profiles sp WHERE sp.RegistrationNumber = s.RegNo);
PRINT CONCAT('B7 student profiles added: ', @@ROWCOUNT);

-- Students and their offerings: current level + (for the upper level) the completed lower level.
DECLARE @Enr TABLE (StudentProfileId UNIQUEIDENTIFIER, OfferingId UNIQUEIDENTIFIER, IsCurrent BIT, FacultyUserId UNIQUEIDENTIFIER, PRIMARY KEY (StudentProfileId, OfferingId));
INSERT INTO @Enr
SELECT sp.Id, o.Id, CASE WHEN c.Lvl = s.Lvl THEN 1 ELSE 0 END, o.FacultyUserId
FROM @Stu s
JOIN users u ON u.Username = s.Username
JOIN student_profiles sp ON sp.UserId = u.Id
JOIN @Course c ON c.DeptCode = s.DeptCode AND c.Lvl <= s.Lvl
JOIN courses k ON k.Code = c.Code AND k.IsDeleted = 0
JOIN @LevelSem ls ON ls.Code = c.DeptCode AND ls.Lvl = c.Lvl
JOIN course_offerings o ON o.CourseId = k.Id AND o.SemesterId = ls.SemesterId AND o.IsDeleted = 0;

-- B8 enrollments
INSERT INTO enrollments (Id, StudentProfileId, CourseOfferingId, EnrolledAt, Status, CreatedAt)
SELECT NEWID(), e.StudentProfileId, e.OfferingId, @Now, N'Active', @Now
FROM @Enr e
WHERE NOT EXISTS (SELECT 1 FROM enrollments x WHERE x.StudentProfileId = e.StudentProfileId AND x.CourseOfferingId = e.OfferingId);
PRINT CONCAT('B8 enrollments added: ', @@ROWCOUNT);

-- B9 results: completed level -> published Final; current level -> published Midterm
INSERT INTO results (Id, StudentProfileId, CourseOfferingId, ResultType, MarksObtained, MaxMarks, IsPublished, PublishedAt, PublishedByUserId, CreatedAt)
SELECT NEWID(), e.StudentProfileId, e.OfferingId,
       CASE WHEN e.IsCurrent = 1 THEN N'Midterm' ELSE N'Final' END,
       CASE WHEN e.IsCurrent = 1 THEN 18 + ABS(CHECKSUM(e.StudentProfileId, e.OfferingId)) % 13
            ELSE 52 + ABS(CHECKSUM(e.StudentProfileId, e.OfferingId)) % 46 END,
       CASE WHEN e.IsCurrent = 1 THEN 30 ELSE 100 END,
       1, @Now, e.FacultyUserId, @Now
FROM @Enr e
WHERE NOT EXISTS (SELECT 1 FROM results r WHERE r.StudentProfileId = e.StudentProfileId AND r.CourseOfferingId = e.OfferingId
                    AND r.ResultType = CASE WHEN e.IsCurrent = 1 THEN N'Midterm' ELSE N'Final' END);
PRINT CONCAT('B9 results added: ', @@ROWCOUNT);

-- B10 attendance: 20 class days for current-level offerings (about 88% present)
INSERT INTO attendance_records (Id, StudentProfileId, CourseOfferingId, Date, Status, MarkedByUserId, CreatedAt)
SELECT NEWID(), e.StudentProfileId, e.OfferingId, DATEADD(DAY, dd.n * 2, CAST('2026-08-03' AS DATE)),
       CASE WHEN ABS(CHECKSUM(e.StudentProfileId, e.OfferingId, dd.n)) % 100 < 85 THEN N'Present'
            WHEN ABS(CHECKSUM(e.StudentProfileId, e.OfferingId, dd.n)) % 100 < 92 THEN N'Late'
            ELSE N'Absent' END,
       e.FacultyUserId, @Now
FROM @Enr e
CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19)) dd(n)
WHERE e.IsCurrent = 1 AND e.FacultyUserId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM attendance_records a WHERE a.StudentProfileId = e.StudentProfileId AND a.CourseOfferingId = e.OfferingId
                    AND a.Date = DATEADD(DAY, dd.n * 2, CAST('2026-08-03' AS DATE)));
PRINT CONCAT('B10 attendance records added: ', @@ROWCOUNT);

-- B11 fee receipts: one per student (mostly Paid, some Pending / Submitted)
IF @FinanceUserId IS NOT NULL
BEGIN
    INSERT INTO payment_receipts (Id, StudentProfileId, CreatedByUserId, ReceiptNo, Status, Amount, Description, DueDate, ConfirmedByUserId, ConfirmedAt, CreatedAt, UpdatedAt, IsDeleted)
    SELECT NEWID(), sp.Id, @FinanceUserId, CONCAT(N'REC-', s.RegNo, N'-2026'),
           CASE s.Seq % 5 WHEN 0 THEN 1 WHEN 4 THEN 2 ELSE 3 END,
           d.Fee + (s.Seq % 4) * 50,
           CONCAT(N'Tuition fee - ', p.Name, N' ', d.LevelLabel, N' ', s.Lvl),
           CAST('2026-09-01' AS DATE),
           CASE WHEN s.Seq % 5 IN (0, 4) THEN NULL ELSE @FinanceUserId END,
           CASE WHEN s.Seq % 5 IN (0, 4) THEN NULL ELSE @Now END,
           @Now, @Now, 0
    FROM @Stu s
    JOIN @Dept d ON d.Code = s.DeptCode
    JOIN academic_programs p ON p.Code = d.ProgCode
    JOIN users u ON u.Username = s.Username
    JOIN student_profiles sp ON sp.UserId = u.Id
    WHERE NOT EXISTS (SELECT 1 FROM payment_receipts r WHERE r.ReceiptNo = CONCAT(N'REC-', s.RegNo, N'-2026'));
    PRINT CONCAT('B11 fee receipts added: ', @@ROWCOUNT);
END

-- B12 new admissions: 5 students per new department at its first level, NOT yet enrolled,
--     so bulk enrollment (Enrollments page) has real candidates to work with.
DECLARE @Admit TABLE (Username NVARCHAR(100) PRIMARY KEY, RegNo NVARCHAR(50), FullName NVARCHAR(200), DeptCode NVARCHAR(50), Lvl INT, Seq INT);
INSERT INTO @Admit
SELECT CONCAT(d.StudentPrefix, d.FirstLevel, N'new', s.n),
       CONCAT(d.RegPrefix, N'-', d.FirstLevel, N'-N', RIGHT(CONCAT(N'0', s.n), 2)),
       CONCAT(f.Name, N' ', l.Name),
       d.Code, d.FirstLevel, s.n
FROM @Dept d
CROSS JOIN (VALUES (1),(2),(3),(4),(5)) s(n)
JOIN @First f ON f.n = 11 - s.n
JOIN @Last l ON l.n = (s.n * 3 + LEN(d.Code)) % 8;

INSERT INTO users (Id, Username, Email, FullName, PasswordHash, RoleId, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType, MustChangePassword)
SELECT NEWID(), a.Username, CONCAT(a.Username, N'@tabsan.edu'), a.FullName, @DefPwd, @RoleStudent, x.Id, 1, 0, @Now, x.TenantId, x.CampusId, x.InstitutionType, 0
FROM @Admit a JOIN departments x ON x.Code = a.DeptCode
WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.Username = a.Username);

INSERT INTO student_profiles (Id, UserId, RegistrationNumber, ProgramId, DepartmentId, AdmissionDate, Cgpa, CurrentSemesterGpa, CurrentSemesterNumber, Status, CreatedAt, IsDeleted)
SELECT NEWID(), u.Id, a.RegNo, p.Id, x.Id, CAST('2026-09-01' AS DATE), 0, 0, a.Lvl, 1, @Now, 0
FROM @Admit a
JOIN @Dept d ON d.Code = a.DeptCode
JOIN departments x ON x.Code = a.DeptCode
JOIN academic_programs p ON p.Code = d.ProgCode
JOIN users u ON u.Username = a.Username
WHERE NOT EXISTS (SELECT 1 FROM student_profiles sp WHERE sp.UserId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM student_profiles sp WHERE sp.RegistrationNumber = a.RegNo);
PRINT CONCAT('B12 new admissions (not yet enrolled) added: ', @@ROWCOUNT);

-- =====================================================================================
-- Part A3: demo terms are open for enrollment
--   Every class / semester in the demo data has a cohort currently studying in it, but 03 seeds
--   them closed, which makes every enrollment attempt fail ("Semester is closed" / "Enrollment is
--   closed for this offering"). Re-open the demo institutes' terms and offerings.
-- =====================================================================================
UPDATE s SET s.IsClosed = 0, s.ClosedAt = NULL, s.UpdatedAt = @Now
FROM semesters s
WHERE s.IsDeleted = 0 AND s.IsClosed = 1
  AND EXISTS (SELECT 1 FROM course_offerings o JOIN @Tenants t ON t.TenantId = o.TenantId WHERE o.SemesterId = s.Id AND o.IsDeleted = 0);
PRINT CONCAT('A3 demo semesters/classes re-opened: ', @@ROWCOUNT);

UPDATE o SET o.IsOpen = 1, o.UpdatedAt = @Now
FROM course_offerings o JOIN @Tenants t ON t.TenantId = o.TenantId
WHERE o.IsDeleted = 0 AND o.IsOpen = 0;
PRINT CONCAT('A3 demo course offerings opened for enrollment: ', @@ROWCOUNT);

-- =====================================================================================
-- Part A2: every offering has a teacher of its own department / institution type
-- =====================================================================================
UPDATE o
SET o.FacultyUserId = COALESCE(
        (SELECT TOP 1 f.FacultyUserId
           FROM faculty_department_assignments f
           JOIN users fu ON fu.Id = f.FacultyUserId AND fu.IsDeleted = 0 AND fu.IsActive = 1
          WHERE f.DepartmentId = c.DepartmentId AND f.RemovedAt IS NULL
          ORDER BY ABS(CHECKSUM(o.Id, f.FacultyUserId))),
        (SELECT TOP 1 fu.Id
           FROM users fu JOIN roles r ON r.Id = fu.RoleId AND r.Name = N'Faculty'
          WHERE fu.InstitutionType = o.InstitutionType AND fu.IsDeleted = 0 AND fu.IsActive = 1
          ORDER BY ABS(CHECKSUM(o.Id, fu.Id)))),
    o.UpdatedAt = @Now
FROM course_offerings o
JOIN courses c ON c.Id = o.CourseId
WHERE o.IsDeleted = 0
  AND (o.FacultyUserId IS NULL
       OR NOT EXISTS (SELECT 1 FROM users fu WHERE fu.Id = o.FacultyUserId AND fu.InstitutionType = o.InstitutionType));
PRINT CONCAT('A2 offerings given a teacher of their own institution: ', @@ROWCOUNT);

COMMIT TRANSACTION;

-- =====================================================================================
-- Summary by institution type
-- =====================================================================================
SELECT CASE d.InstitutionType WHEN 0 THEN 'University' WHEN 1 THEN 'School' ELSE 'College' END AS Institute,
       COUNT(DISTINCT d.Id) AS Departments,
       (SELECT COUNT(*) FROM academic_programs p JOIN departments x ON x.Id = p.DepartmentId WHERE x.InstitutionType = d.InstitutionType AND p.IsDeleted = 0) AS Programs,
       (SELECT COUNT(*) FROM courses c WHERE c.InstitutionType = d.InstitutionType AND c.IsDeleted = 0) AS Courses,
       (SELECT COUNT(*) FROM course_offerings o WHERE o.InstitutionType = d.InstitutionType AND o.IsDeleted = 0) AS Offerings,
       (SELECT COUNT(*) FROM student_profiles s JOIN departments x ON x.Id = s.DepartmentId WHERE x.InstitutionType = d.InstitutionType AND s.IsDeleted = 0) AS Students,
       (SELECT COUNT(*) FROM users u JOIN roles r ON r.Id = u.RoleId AND r.Name = 'Faculty' WHERE u.InstitutionType = d.InstitutionType AND u.IsDeleted = 0) AS Teachers
FROM departments d
WHERE d.IsDeleted = 0
GROUP BY d.InstitutionType
ORDER BY d.InstitutionType;
