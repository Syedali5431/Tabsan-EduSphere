-- =====================================================================================
-- 01-School-Records.sql  —  Tabsan EduSphere
-- Demo records for the schools added by ../Schools-And-Campuses (run that folder first).
-- For every campus School section (<campus>-SCH, Class 1 to 10 programme <campus>-CLS):
--
--   People      an admin and a finance officer per campus, five teachers per campus,
--               six students per class (Class 1 to 10) and four new admissions (not enrolled)
--   Teaching    five subjects per class, an offering per subject taught by its teacher,
--               enrolments, prerequisites (Mathematics and English follow on class by class)
--   Records     25 school days of attendance, published Quiz and Midterm results, three
--               months of fee receipts (paid, proof submitted, pending)
--   Campus      a building with ten classrooms and two labs, a published weekly timetable
--               per class (five periods a day, room and teacher on every period)
--   Messages    a welcome announcement per subject, school notices, notifications, and a
--               study link per subject in Course Material
--
-- Every account's password is EduSphere147 (change them before real use).
-- Run against [Tabsan-EduSphere]. Safe to re-run: rows are keyed on usernames, codes,
-- registration and receipt numbers, and skipped when they exist.
-- Quizzes, assignments, modules, outcomes, discussions, live classes and helpdesk tickets
-- are added through the API by 02-Learning-Activity.mjs.
-- =====================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @DefPwd NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ='; -- EduSphere147
DECLARE @RoleAdmin INT   = (SELECT Id FROM roles WHERE Name = N'Admin');
DECLARE @RoleFaculty INT = (SELECT Id FROM roles WHERE Name = N'Faculty');
DECLARE @RoleStudent INT = (SELECT Id FROM roles WHERE Name = N'Student');
DECLARE @RoleFinance INT = (SELECT Id FROM roles WHERE Name = N'Finance');
DECLARE @School INT = 1; -- InstitutionType
DECLARE @YearStart DATE = DATEFROMPARTS(YEAR(@Now) - CASE WHEN MONTH(@Now) < 4 THEN 1 ELSE 0 END, 4, 1); -- school year from April

-- ── The campuses (from Schools-And-Campuses) ─────────────────────────────────────────
DECLARE @Campus TABLE (Seq INT PRIMARY KEY, TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER, DepartmentId UNIQUEIDENTIFIER,
                       ProgramId UNIQUEIDENTIFIER, SchoolCode NVARCHAR(64), SchoolName NVARCHAR(200), CampusCode NVARCHAR(16),
                       CampusKey NVARCHAR(16), CampusName NVARCHAR(200), SchoolSeq INT);
INSERT INTO @Campus
SELECT ROW_NUMBER() OVER (ORDER BY t.Code, c.Code), t.Id, c.Id, d.Id, p.Id, t.Code, t.Name, c.Code,
       LOWER(REPLACE(c.Code, N'-', N'')), c.Name, DENSE_RANK() OVER (ORDER BY t.Code)
FROM departments d
JOIN campuses c ON c.Id = d.CampusId AND c.IsDeleted = 0
JOIN tenants t ON t.Id = d.TenantId AND t.IsDeleted = 0
JOIN academic_programs p ON p.DepartmentId = d.Id AND p.Code = CONCAT(c.Code, N'-CLS') AND p.IsDeleted = 0
WHERE d.InstitutionType = @School AND d.IsDeleted = 0 AND d.IsActive = 1 AND d.Code = CONCAT(c.Code, N'-SCH');

IF NOT EXISTS (SELECT 1 FROM @Campus)
BEGIN
    RAISERROR(N'01-School-Records: no School sections found; run Scripts/Schools-And-Campuses first.', 16, 1);
    RETURN;
END

DECLARE @Class TABLE (Lvl INT PRIMARY KEY, SemesterId UNIQUEIDENTIFIER, Band CHAR(1), Fee DECIMAL(18,2));
INSERT INTO @Class
SELECT n.n, (SELECT TOP 1 s.Id FROM semesters s WHERE s.Name = CONCAT(N'Class ', n.n) AND s.IsDeleted = 0 ORDER BY s.CreatedAt),
       CASE WHEN n.n <= 5 THEN 'A' WHEN n.n <= 8 THEN 'B' ELSE 'C' END,
       CASE WHEN n.n <= 5 THEN 4500 WHEN n.n <= 8 THEN 5500 ELSE 6500 END
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) n(n);
IF EXISTS (SELECT 1 FROM @Class WHERE SemesterId IS NULL)
BEGIN
    RAISERROR(N'01-School-Records: the classes Class 1 to Class 10 are missing; run Scripts/Schools-And-Campuses/02-Add-School-Sections.sql.', 16, 1);
    RETURN;
END

-- Five subjects per class; slot n is taught by the campus teacher of slot n.
DECLARE @Subject TABLE (Band CHAR(1), Slot INT, Code NVARCHAR(3), Title NVARCHAR(100), Credit INT, Link NVARCHAR(300), LinkName NVARCHAR(200), PRIMARY KEY (Band, Slot));
INSERT INTO @Subject VALUES
 ('A',1,N'ENG',N'English',4,          N'https://learnenglishkids.britishcouncil.org/',          N'British Council LearnEnglish Kids'),
 ('A',2,N'URD',N'Urdu',4,             N'https://www.rekhta.org/',                               N'Rekhta: Urdu poetry and stories'),
 ('A',3,N'MTH',N'Mathematics',4,      N'https://www.khanacademy.org/math/early-math',           N'Khan Academy: early maths'),
 ('A',4,N'SCI',N'General Science',3,  N'https://www.bbc.co.uk/bitesize/subjects/z6svr82',       N'BBC Bitesize: science'),
 ('A',5,N'ISL',N'Islamiyat',2,        N'https://www.bbc.co.uk/bitesize/subjects/zh3rkqt',       N'BBC Bitesize: religious studies'),
 ('B',1,N'ENG',N'English',4,          N'https://learnenglishteens.britishcouncil.org/',         N'British Council LearnEnglish Teens'),
 ('B',2,N'URD',N'Urdu',4,             N'https://www.rekhta.org/',                               N'Rekhta: Urdu literature'),
 ('B',3,N'MTH',N'Mathematics',4,      N'https://www.khanacademy.org/math/pre-algebra',          N'Khan Academy: pre-algebra'),
 ('B',4,N'SCI',N'Science',3,          N'https://www.khanacademy.org/science',                   N'Khan Academy: science'),
 ('B',5,N'SST',N'Social Studies',3,   N'https://www.bbc.co.uk/bitesize/subjects/zk26n39',       N'BBC Bitesize: geography'),
 ('C',1,N'ENG',N'English',4,          N'https://learnenglishteens.britishcouncil.org/skills',  N'British Council: reading and writing skills'),
 ('C',2,N'URD',N'Urdu',4,             N'https://www.rekhta.org/',                               N'Rekhta: Urdu prose and poetry'),
 ('C',3,N'MTH',N'Mathematics',4,      N'https://www.khanacademy.org/math/algebra',              N'Khan Academy: algebra'),
 ('C',4,N'PHY',N'Physics',4,          N'https://www.khanacademy.org/science/physics',           N'Khan Academy: physics'),
 ('C',5,N'CSC',N'Computer Science',3, N'https://www.khanacademy.org/computing/computer-programming', N'Khan Academy: computer programming');

DECLARE @Slot TABLE (Slot INT PRIMARY KEY, KeyName NVARCHAR(20), Teaches NVARCHAR(100));
INSERT INTO @Slot VALUES (1,N'english',N'English'),(2,N'urdu',N'Urdu'),(3,N'maths',N'Mathematics'),
                         (4,N'science',N'Science and Physics'),(5,N'social',N'Islamiyat, Social Studies and Computer Science');

DECLARE @Female TABLE (n INT PRIMARY KEY, Name NVARCHAR(50));
INSERT INTO @Female VALUES (0,N'Ayesha'),(1,N'Fatima'),(2,N'Zainab'),(3,N'Maryam'),(4,N'Sana'),(5,N'Hira'),(6,N'Amna'),(7,N'Iqra'),(8,N'Mahnoor'),(9,N'Rabia'),(10,N'Noor'),(11,N'Khadija');
DECLARE @Male TABLE (n INT PRIMARY KEY, Name NVARCHAR(50));
INSERT INTO @Male VALUES (0,N'Ali'),(1,N'Hamza'),(2,N'Omar'),(3,N'Bilal'),(4,N'Hassan'),(5,N'Usman'),(6,N'Ahmed'),(7,N'Saad'),(8,N'Zain'),(9,N'Ibrahim'),(10,N'Hamid'),(11,N'Faisal');
DECLARE @Last TABLE (n INT PRIMARY KEY, Name NVARCHAR(50));
INSERT INTO @Last VALUES (0,N'Khan'),(1,N'Ahmed'),(2,N'Hussain'),(3,N'Malik'),(4,N'Sheikh'),(5,N'Chaudhry'),(6,N'Butt'),(7,N'Raza'),(8,N'Qureshi'),(9,N'Siddiqui'),(10,N'Javed'),(11,N'Iqbal');

BEGIN TRANSACTION;

-- ═════════════════════════════════════════════════════════════════════════════════════
-- 1. People
-- ═════════════════════════════════════════════════════════════════════════════════════
-- A campus admin (the campus office) and a finance officer per campus
DECLARE @CampusStaff TABLE (Username NVARCHAR(100) PRIMARY KEY, FullName NVARCHAR(200), RoleId INT, CampusSeq INT);
INSERT INTO @CampusStaff
SELECT CONCAT(N'admin.', c.CampusKey), CONCAT(f.Name, N' ', l.Name), @RoleAdmin, c.Seq
FROM @Campus c JOIN @Female f ON f.n = (c.Seq * 5 + 3) % 12 JOIN @Last l ON l.n = (c.Seq * 7 + 1) % 12
UNION ALL
SELECT CONCAT(N'finance.', c.CampusKey), CONCAT(m.Name, N' ', l.Name), @RoleFinance, c.Seq
FROM @Campus c JOIN @Male m ON m.n = (c.Seq * 3 + 1) % 12 JOIN @Last l ON l.n = (c.Seq * 5 + 2) % 12;

INSERT INTO users (Id, Username, Email, FullName, PasswordHash, RoleId, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType, MustChangePassword, PhoneNumber)
SELECT NEWID(), s.Username, CONCAT(s.Username, N'@tabsan.school'), s.FullName, @DefPwd, s.RoleId, c.DepartmentId, 1, 0, @Now, c.TenantId, c.CampusId, @School, 0,
       CONCAT(N'0300-55', RIGHT(CONCAT(N'00000', ABS(CHECKSUM(s.Username)) % 100000), 5))
FROM @CampusStaff s JOIN @Campus c ON c.Seq = s.CampusSeq
WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.Username = s.Username);
PRINT CONCAT(N'Campus admins and finance officers added: ', @@ROWCOUNT);

-- Teachers: five per campus (english, urdu, maths, science, social)
DECLARE @Teacher TABLE (Username NVARCHAR(100) PRIMARY KEY, FullName NVARCHAR(200), CampusSeq INT, Slot INT);
INSERT INTO @Teacher
SELECT CONCAT(c.CampusKey, N'.', s.KeyName),
       CASE WHEN (c.Seq + s.Slot) % 2 = 0 THEN CONCAT(N'Ms. ', f.Name, N' ', l.Name) ELSE CONCAT(N'Mr. ', m.Name, N' ', l.Name) END,
       c.Seq, s.Slot
FROM @Campus c CROSS JOIN @Slot s
JOIN @Female f ON f.n = (c.Seq * 5 + s.Slot) % 12
JOIN @Male m ON m.n = (c.Seq * 7 + s.Slot * 2) % 12
JOIN @Last l ON l.n = (c.Seq * 3 + s.Slot * 5) % 12;

INSERT INTO users (Id, Username, Email, FullName, PasswordHash, RoleId, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType, MustChangePassword, PhoneNumber)
SELECT NEWID(), t.Username, CONCAT(t.Username, N'@tabsan.school'), t.FullName, @DefPwd, @RoleFaculty, c.DepartmentId, 1, 0, @Now, c.TenantId, c.CampusId, @School, 0,
       CONCAT(N'0321-44', RIGHT(CONCAT(N'00000', ABS(CHECKSUM(t.Username)) % 100000), 5))
FROM @Teacher t JOIN @Campus c ON c.Seq = t.CampusSeq
WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.Username = t.Username);
PRINT CONCAT(N'Teachers added: ', @@ROWCOUNT);

INSERT INTO faculty_department_assignments (Id, FacultyUserId, DepartmentId, AssignedAt, CreatedAt)
SELECT NEWID(), u.Id, c.DepartmentId, @Now, @Now
FROM @Teacher t JOIN @Campus c ON c.Seq = t.CampusSeq JOIN users u ON u.Username = t.Username
WHERE NOT EXISTS (SELECT 1 FROM faculty_department_assignments f WHERE f.FacultyUserId = u.Id AND f.DepartmentId = c.DepartmentId AND f.RemovedAt IS NULL);

-- The campus admin manages the campus's School section
INSERT INTO admin_department_assignments (Id, AdminUserId, DepartmentId, AssignedAt, CreatedAt)
SELECT NEWID(), u.Id, c.DepartmentId, @Now, @Now
FROM @Campus c JOIN users u ON u.Username = CONCAT(N'admin.', c.CampusKey)
WHERE NOT EXISTS (SELECT 1 FROM admin_department_assignments a WHERE a.AdminUserId = u.Id AND a.DepartmentId = c.DepartmentId AND a.RemovedAt IS NULL);

-- Students: six per class per campus, and four new admissions per campus (Class 1, not enrolled yet)
DECLARE @Stu TABLE (Username NVARCHAR(100) PRIMARY KEY, RegNo NVARCHAR(50), FullName NVARCHAR(200), FatherName NVARCHAR(200),
                    CampusSeq INT, Lvl INT, n INT, IsNew BIT, Ability INT);
INSERT INTO @Stu
SELECT CONCAT(c.CampusKey, N'.c', cl.Lvl, N's', k.n),
       CONCAT(c.CampusCode, N'-', RIGHT(CONCAT(N'0', cl.Lvl), 2), N'-', RIGHT(CONCAT(N'0', k.n), 2)),
       CONCAT(CASE WHEN k.n % 2 = 1 THEN f.Name ELSE m.Name END, N' ', l.Name),
       CONCAT(fm.Name, N' ', l.Name),
       c.Seq, cl.Lvl, k.n, 0,
       55 + ABS(CHECKSUM(c.CampusCode, cl.Lvl, k.n)) % 40
FROM @Campus c CROSS JOIN @Class cl CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6)) k(n)
JOIN @Female f ON f.n = (c.Seq * 3 + cl.Lvl * 2 + k.n) % 12
JOIN @Male m ON m.n = (c.Seq * 5 + cl.Lvl + k.n * 3) % 12
JOIN @Male fm ON fm.n = (c.Seq + cl.Lvl * 3 + k.n * 5 + 4) % 12
JOIN @Last l ON l.n = (c.Seq * 7 + cl.Lvl * 5 + k.n) % 12
UNION ALL
SELECT CONCAT(c.CampusKey, N'.new', k.n),
       CONCAT(c.CampusCode, N'-01-N', RIGHT(CONCAT(N'0', k.n), 2)),
       CONCAT(CASE WHEN k.n % 2 = 1 THEN m.Name ELSE f.Name END, N' ', l.Name),
       CONCAT(fm.Name, N' ', l.Name),
       c.Seq, 1, k.n, 1, 70
FROM @Campus c CROSS JOIN (VALUES (1),(2),(3),(4)) k(n)
JOIN @Female f ON f.n = (c.Seq * 2 + k.n * 5) % 12
JOIN @Male m ON m.n = (c.Seq * 4 + k.n * 7) % 12
JOIN @Male fm ON fm.n = (c.Seq * 6 + k.n * 3 + 1) % 12
JOIN @Last l ON l.n = (c.Seq * 9 + k.n * 4) % 12;

INSERT INTO users (Id, Username, Email, FullName, FatherName, PasswordHash, RoleId, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId,
                   InstitutionType, MustChangePassword, PhoneNumber, Address)
SELECT NEWID(), s.Username, CONCAT(s.Username, N'@tabsan.school'), s.FullName, s.FatherName, @DefPwd, @RoleStudent, c.DepartmentId, 1, 0, @Now,
       c.TenantId, c.CampusId, @School, 0,
       CONCAT(N'0333-', RIGHT(CONCAT(N'0000000', ABS(CHECKSUM(s.RegNo)) % 10000000), 7)),
       CONCAT(N'House ', 10 + ABS(CHECKSUM(s.Username)) % 190, N', Street ', 1 + ABS(CHECKSUM(s.RegNo)) % 40, N', near ', c.CampusName)
FROM @Stu s JOIN @Campus c ON c.Seq = s.CampusSeq
WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.Username = s.Username);
PRINT CONCAT(N'Student accounts added: ', @@ROWCOUNT);

INSERT INTO student_profiles (Id, UserId, RegistrationNumber, ProgramId, DepartmentId, AdmissionDate, Cgpa, CurrentSemesterGpa, CurrentSemesterNumber, Status, CreatedAt, IsDeleted)
SELECT NEWID(), u.Id, s.RegNo, c.ProgramId, c.DepartmentId,
       CASE WHEN s.IsNew = 1 THEN DATEADD(MONTH, 6, @YearStart) ELSE DATEADD(YEAR, -(s.Lvl - 1), @YearStart) END,
       CASE WHEN s.IsNew = 1 THEN 0 ELSE s.Ability END, CASE WHEN s.IsNew = 1 THEN 0 ELSE s.Ability END, s.Lvl, 1, @Now, 0
FROM @Stu s JOIN @Campus c ON c.Seq = s.CampusSeq JOIN users u ON u.Username = s.Username
WHERE NOT EXISTS (SELECT 1 FROM student_profiles p WHERE p.UserId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM student_profiles p WHERE p.RegistrationNumber = s.RegNo);
PRINT CONCAT(N'Student profiles added: ', @@ROWCOUNT);

-- ═════════════════════════════════════════════════════════════════════════════════════
-- 2. Subjects, offerings, enrolments, prerequisites
-- ═════════════════════════════════════════════════════════════════════════════════════
UPDATE semesters SET IsClosed = 0, ClosedAt = NULL, UpdatedAt = @Now
WHERE Id IN (SELECT SemesterId FROM @Class) AND IsClosed = 1;

DECLARE @Course TABLE (Code NVARCHAR(20) PRIMARY KEY, CampusSeq INT, Lvl INT, Slot INT, Title NVARCHAR(200), Credit INT);
INSERT INTO @Course
SELECT CONCAT(c.CampusCode, N'-', s.Code, RIGHT(CONCAT(N'0', cl.Lvl), 2)), c.Seq, cl.Lvl, s.Slot, CONCAT(s.Title, N' (Class ', cl.Lvl, N')'), s.Credit
FROM @Campus c CROSS JOIN @Class cl JOIN @Subject s ON s.Band = cl.Band;

INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType, CourseType, GradingType, HasSemesters)
SELECT NEWID(), k.Title, k.Code, k.Credit, c.DepartmentId, 1, 0, @Now, c.TenantId, c.CampusId, @School, 1, N'Percentage', 1
FROM @Course k JOIN @Campus c ON c.Seq = k.CampusSeq
WHERE NOT EXISTS (SELECT 1 FROM courses x WHERE x.Code = k.Code AND x.IsDeleted = 0);
PRINT CONCAT(N'Subjects added: ', @@ROWCOUNT);

INSERT INTO course_offerings (Id, CourseId, SemesterId, FacultyUserId, MaxEnrollment, IsOpen, CreatedAt, IsDeleted, InstitutionType, TenantId, CampusId)
SELECT NEWID(), x.Id, cl.SemesterId, u.Id, 40, 1, @Now, 0, @School, c.TenantId, c.CampusId
FROM @Course k
JOIN @Campus c ON c.Seq = k.CampusSeq
JOIN @Class cl ON cl.Lvl = k.Lvl
JOIN courses x ON x.Code = k.Code AND x.IsDeleted = 0
JOIN @Teacher t ON t.CampusSeq = k.CampusSeq AND t.Slot = k.Slot
JOIN users u ON u.Username = t.Username
WHERE NOT EXISTS (SELECT 1 FROM course_offerings o WHERE o.CourseId = x.Id AND o.SemesterId = cl.SemesterId AND o.IsDeleted = 0);
PRINT CONCAT(N'Subject offerings added: ', @@ROWCOUNT);

-- Each student's subjects in their class this year
DECLARE @Enr TABLE (StudentProfileId UNIQUEIDENTIFIER, OfferingId UNIQUEIDENTIFIER, TeacherId UNIQUEIDENTIFIER, Ability INT, n INT, Slot INT, PRIMARY KEY (StudentProfileId, OfferingId));
INSERT INTO @Enr
SELECT p.Id, o.Id, o.FacultyUserId, s.Ability, s.n, k.Slot
FROM @Stu s
JOIN users u ON u.Username = s.Username
JOIN student_profiles p ON p.UserId = u.Id
JOIN @Course k ON k.CampusSeq = s.CampusSeq AND k.Lvl = s.Lvl
JOIN courses x ON x.Code = k.Code AND x.IsDeleted = 0
JOIN @Class cl ON cl.Lvl = s.Lvl
JOIN course_offerings o ON o.CourseId = x.Id AND o.SemesterId = cl.SemesterId AND o.IsDeleted = 0
WHERE s.IsNew = 0;

INSERT INTO enrollments (Id, StudentProfileId, CourseOfferingId, EnrolledAt, Status, CreatedAt)
SELECT NEWID(), e.StudentProfileId, e.OfferingId, @YearStart, N'Active', @Now
FROM @Enr e
WHERE NOT EXISTS (SELECT 1 FROM enrollments x WHERE x.StudentProfileId = e.StudentProfileId AND x.CourseOfferingId = e.OfferingId);
PRINT CONCAT(N'Enrolments added: ', @@ROWCOUNT);

-- Mathematics and English build on the previous class
INSERT INTO course_prerequisites (Id, CourseId, PrerequisiteCourseId, CreatedAt)
SELECT NEWID(), x.Id, prev.Id, @Now
FROM @Course k
JOIN @Subject s ON s.Slot = k.Slot AND s.Band = (SELECT Band FROM @Class WHERE Lvl = k.Lvl) AND s.Code IN (N'MTH', N'ENG')
JOIN courses x ON x.Code = k.Code AND x.IsDeleted = 0
JOIN @Campus c ON c.Seq = k.CampusSeq
JOIN courses prev ON prev.Code = CONCAT(c.CampusCode, N'-', s.Code, RIGHT(CONCAT(N'0', k.Lvl - 1), 2)) AND prev.IsDeleted = 0
WHERE k.Lvl > 1
  AND NOT EXISTS (SELECT 1 FROM course_prerequisites q WHERE q.CourseId = x.Id AND q.PrerequisiteCourseId = prev.Id);
PRINT CONCAT(N'Prerequisites added: ', @@ROWCOUNT);

-- ═════════════════════════════════════════════════════════════════════════════════════
-- 3. Attendance, results, fees
-- ═════════════════════════════════════════════════════════════════════════════════════
-- 25 school days from mid-August; the sixth student of each class misses about a third of them.
DECLARE @Day TABLE (d DATE PRIMARY KEY);
INSERT INTO @Day
SELECT TOP 25 x.d
FROM (SELECT DATEADD(DAY, n.n * 2, DATEFROMPARTS(YEAR(@YearStart), 8, 17)) AS d
      FROM (SELECT TOP 60 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n FROM sys.all_objects) n) x
WHERE DATENAME(WEEKDAY, x.d) NOT IN (N'Saturday', N'Sunday') AND x.d < CAST(@Now AS DATE)
ORDER BY x.d;

INSERT INTO attendance_records (Id, StudentProfileId, CourseOfferingId, Date, Status, MarkedByUserId, CreatedAt)
SELECT NEWID(), e.StudentProfileId, e.OfferingId, d.d,
       CASE WHEN ABS(CHECKSUM(e.StudentProfileId, e.OfferingId, d.d)) % 100 < CASE WHEN e.n = 6 THEN 62 ELSE 88 END THEN N'Present'
            WHEN ABS(CHECKSUM(e.StudentProfileId, e.OfferingId, d.d)) % 100 < CASE WHEN e.n = 6 THEN 70 ELSE 94 END THEN N'Late'
            ELSE N'Absent' END,
       e.TeacherId, @Now
FROM @Enr e CROSS JOIN @Day d
WHERE NOT EXISTS (SELECT 1 FROM attendance_records a WHERE a.StudentProfileId = e.StudentProfileId AND a.CourseOfferingId = e.OfferingId AND a.Date = d.d);
PRINT CONCAT(N'Attendance records added: ', @@ROWCOUNT);

-- Published Quiz (out of 20) and Midterm (out of 50) marks, around each student's ability
INSERT INTO results (Id, StudentProfileId, CourseOfferingId, ResultType, MarksObtained, MaxMarks, IsPublished, PublishedAt, PublishedByUserId, CreatedAt)
SELECT NEWID(), e.StudentProfileId, e.OfferingId, r.ResultType,
       ROUND(r.MaxMarks * (CASE WHEN v.p > 99 THEN 99 WHEN v.p < 30 THEN 30 ELSE v.p END) / 100.0, 0),
       r.MaxMarks, 1, DATEADD(DAY, r.DaysAgo, @Now), e.TeacherId, @Now
FROM @Enr e
CROSS JOIN (VALUES (N'Quiz', 20, -40), (N'Midterm', 50, -12)) r(ResultType, MaxMarks, DaysAgo)
CROSS APPLY (SELECT e.Ability + (ABS(CHECKSUM(e.StudentProfileId, e.OfferingId, r.ResultType)) % 21) - 10 AS p) v
WHERE NOT EXISTS (SELECT 1 FROM results x WHERE x.StudentProfileId = e.StudentProfileId AND x.CourseOfferingId = e.OfferingId AND x.ResultType = r.ResultType);
PRINT CONCAT(N'Results added: ', @@ROWCOUNT);

-- Monthly tuition (August and September paid; October paid, proof submitted or pending) and admission fees for new students
INSERT INTO payment_receipts (Id, StudentProfileId, CreatedByUserId, ReceiptNo, Status, Amount, Description, DueDate, ProofUploadedAt,
                              ConfirmedByUserId, ConfirmedAt, Notes, CreatedAt, UpdatedAt, IsDeleted)
SELECT NEWID(), p.Id, fin.Id, CONCAT(s.RegNo, N'-', FORMAT(m.DueDate, 'yyyyMM')),
       st.Status, cl.Fee, CONCAT(N'Tuition fee - ', FORMAT(m.DueDate, 'MMMM yyyy'), N' (Class ', s.Lvl, N')'), m.DueDate,
       CASE WHEN st.Status IN (2, 3) THEN DATEADD(DAY, -3, m.DueDate) END,
       CASE WHEN st.Status = 3 THEN fin.Id END, CASE WHEN st.Status = 3 THEN DATEADD(DAY, -1, m.DueDate) END,
       CASE WHEN st.Status = 2 THEN N'Bank deposit slip uploaded by parent' END,
       DATEADD(DAY, -20, m.DueDate), @Now, 0
FROM @Stu s
JOIN @Campus c ON c.Seq = s.CampusSeq
JOIN @Class cl ON cl.Lvl = s.Lvl
JOIN users u ON u.Username = s.Username
JOIN student_profiles p ON p.UserId = u.Id
JOIN users fin ON fin.Username = CONCAT(N'finance.', c.CampusKey)
CROSS JOIN (VALUES (8), (9), (10)) mm(m)
CROSS APPLY (SELECT DATEFROMPARTS(YEAR(@YearStart), mm.m, 10) AS DueDate) m
CROSS APPLY (SELECT CASE WHEN mm.m < 10 THEN 3 WHEN s.n IN (1, 2, 3) THEN 3 WHEN s.n = 4 THEN 2 ELSE 1 END AS Status) st
WHERE s.IsNew = 0
  AND NOT EXISTS (SELECT 1 FROM payment_receipts r WHERE r.ReceiptNo = CONCAT(s.RegNo, N'-', FORMAT(m.DueDate, 'yyyyMM')));
PRINT CONCAT(N'Tuition receipts added: ', @@ROWCOUNT);

INSERT INTO payment_receipts (Id, StudentProfileId, CreatedByUserId, ReceiptNo, Status, Amount, Description, DueDate, CreatedAt, UpdatedAt, IsDeleted)
SELECT NEWID(), p.Id, fin.Id, CONCAT(s.RegNo, N'-ADM'), 1, 15000, N'Admission and registration fee', DATEADD(DAY, 14, CAST(@Now AS DATE)), @Now, @Now, 0
FROM @Stu s
JOIN @Campus c ON c.Seq = s.CampusSeq
JOIN users u ON u.Username = s.Username
JOIN student_profiles p ON p.UserId = u.Id
JOIN users fin ON fin.Username = CONCAT(N'finance.', c.CampusKey)
WHERE s.IsNew = 1 AND NOT EXISTS (SELECT 1 FROM payment_receipts r WHERE r.ReceiptNo = CONCAT(s.RegNo, N'-ADM'));
PRINT CONCAT(N'Admission fee receipts added: ', @@ROWCOUNT);

-- ═════════════════════════════════════════════════════════════════════════════════════
-- 4. Buildings, rooms and timetables
-- ═════════════════════════════════════════════════════════════════════════════════════
INSERT INTO buildings (Id, Name, Code, IsActive, CreatedAt, IsDeleted, CampusId, TenantId)
SELECT NEWID(), CONCAT(N'Main Block - ', c.CampusName), CONCAT(c.CampusCode, N'-MB'), 1, @Now, 0, c.CampusId, c.TenantId
FROM @Campus c
WHERE NOT EXISTS (SELECT 1 FROM buildings b WHERE b.Code = CONCAT(c.CampusCode, N'-MB') AND b.IsDeleted = 0);
PRINT CONCAT(N'Buildings added: ', @@ROWCOUNT);

INSERT INTO rooms (Id, Number, BuildingId, Capacity, IsActive, CreatedAt, IsDeleted, CampusId, TenantId)
SELECT NEWID(), r.Number, b.Id, r.Capacity, 1, @Now, 0, c.CampusId, c.TenantId
FROM @Campus c
JOIN buildings b ON b.Code = CONCAT(c.CampusCode, N'-MB') AND b.IsDeleted = 0
CROSS JOIN (SELECT CONCAT(N'Room ', n.n) AS Number, 35 AS Capacity FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) n(n)
            UNION ALL SELECT N'Science Lab', 30 UNION ALL SELECT N'Computer Lab', 30) r
WHERE NOT EXISTS (SELECT 1 FROM rooms x WHERE x.BuildingId = b.Id AND x.Number = r.Number AND x.IsDeleted = 0);
PRINT CONCAT(N'Rooms added: ', @@ROWCOUNT);

INSERT INTO timetables (Id, DepartmentId, SemesterId, AcademicProgramId, EffectiveDate, SemesterNumber, IsPublished, PublishedAt, CreatedAt, IsDeleted)
SELECT NEWID(), c.DepartmentId, cl.SemesterId, c.ProgramId, @YearStart, cl.Lvl, 1, @Now, @Now, 0
FROM @Campus c CROSS JOIN @Class cl
WHERE NOT EXISTS (SELECT 1 FROM timetables t WHERE t.DepartmentId = c.DepartmentId AND t.SemesterId = cl.SemesterId AND t.IsDeleted = 0);
PRINT CONCAT(N'Timetables added: ', @@ROWCOUNT);

-- Monday to Friday (1-5), five periods; the subject of each period rotates through the week.
-- Science and Physics in the Science Lab, Computer Science in the Computer Lab, the rest in the class's own room.
INSERT INTO timetable_entries (Id, TimetableId, DayOfWeek, StartTime, EndTime, SubjectName, RoomNumber, FacultyName, RoomId, BuildingId, CourseId, FacultyUserId, CreatedAt)
SELECT NEWID(), tt.Id, d.d, p.StartTime, p.EndTime, s.Title, rm.Number, tu.FullName, rm.Id, b.Id, x.Id, tu.Id, @Now
FROM @Campus c
CROSS JOIN @Class cl
JOIN timetables tt ON tt.DepartmentId = c.DepartmentId AND tt.SemesterId = cl.SemesterId AND tt.IsDeleted = 0
CROSS JOIN (VALUES (1),(2),(3),(4),(5)) d(d)
CROSS JOIN (VALUES (1, CAST('08:00' AS TIME), CAST('08:40' AS TIME)), (2, '08:45', '09:25'), (3, '09:30', '10:10'), (4, '10:30', '11:10'), (5, '11:15', '11:55')) p(Period, StartTime, EndTime)
JOIN @Subject s ON s.Band = cl.Band AND s.Slot = ((p.Period + d.d - 2) % 5) + 1
JOIN courses x ON x.Code = CONCAT(c.CampusCode, N'-', s.Code, RIGHT(CONCAT(N'0', cl.Lvl), 2)) AND x.IsDeleted = 0
JOIN @Teacher t ON t.CampusSeq = c.Seq AND t.Slot = s.Slot
JOIN users tu ON tu.Username = t.Username
JOIN buildings b ON b.Code = CONCAT(c.CampusCode, N'-MB') AND b.IsDeleted = 0
JOIN rooms rm ON rm.BuildingId = b.Id AND rm.IsDeleted = 0
           AND rm.Number = CASE WHEN s.Code IN (N'SCI', N'PHY') AND cl.Lvl >= 6 THEN N'Science Lab' WHEN s.Code = N'CSC' THEN N'Computer Lab' ELSE CONCAT(N'Room ', cl.Lvl) END
WHERE NOT EXISTS (SELECT 1 FROM timetable_entries e WHERE e.TimetableId = tt.Id AND e.DayOfWeek = d.d AND e.StartTime = p.StartTime);
PRINT CONCAT(N'Timetable periods added: ', @@ROWCOUNT);

-- ═════════════════════════════════════════════════════════════════════════════════════
-- 5. Announcements, study links and notifications
-- ═════════════════════════════════════════════════════════════════════════════════════
INSERT INTO course_announcements (Id, OfferingId, AuthorId, Title, Body, PostedAt, CreatedAt, IsDeleted)
SELECT NEWID(), o.Id, o.FacultyUserId, CONCAT(N'Welcome to ', x.Title),
       CONCAT(N'Assalam-o-Alaikum! This term in ', x.Title, N' we follow the Punjab Textbook Board book chapter by chapter. ',
              N'Bring your notebook to every class; homework is set on Mondays and checked on Thursdays.'),
       DATEADD(DAY, -60, @Now), @Now, 0
FROM course_offerings o
JOIN courses x ON x.Id = o.CourseId
JOIN @Campus c ON c.CampusId = o.CampusId
WHERE o.IsDeleted = 0 AND x.Code LIKE CONCAT(c.CampusCode, N'-%')
  AND NOT EXISTS (SELECT 1 FROM course_announcements a WHERE a.OfferingId = o.Id AND a.Title = CONCAT(N'Welcome to ', x.Title));
PRINT CONCAT(N'Subject announcements added: ', @@ROWCOUNT);


INSERT INTO course_materials (Id, TenantId, CampusId, DepartmentId, AcademicProgramId, SemesterId, CourseId, Name, Description, LinkUrl, MaterialType,
                              CreatedByUserId, IsActive, CreatedAt, IsDeleted)
SELECT NEWID(), c.TenantId, c.CampusId, c.DepartmentId, c.ProgramId, cl.SemesterId, x.Id, m.Name, m.Description, m.LinkUrl, 2, tu.Id, 1, @Now, 0
FROM @Course k
JOIN @Campus c ON c.Seq = k.CampusSeq
JOIN @Class cl ON cl.Lvl = k.Lvl
JOIN @Subject s ON s.Band = cl.Band AND s.Slot = k.Slot
JOIN courses x ON x.Code = k.Code AND x.IsDeleted = 0
JOIN @Teacher t ON t.CampusSeq = k.CampusSeq AND t.Slot = k.Slot
JOIN users tu ON tu.Username = t.Username
CROSS APPLY (VALUES (s.LinkName, CONCAT(N'Practice and short lessons for ', k.Title, N'.'), s.Link)) m(Name, Description, LinkUrl)
WHERE NOT EXISTS (SELECT 1 FROM course_materials y WHERE y.CourseId = x.Id AND y.Name = m.Name AND y.IsDeleted = 0);
PRINT CONCAT(N'Course materials added: ', @@ROWCOUNT);

DECLARE @Notice TABLE (Seq INT PRIMARY KEY, Title NVARCHAR(300), Body NVARCHAR(2000), Type NVARCHAR(50), IsSystem BIT, DaysAgo INT);
INSERT INTO @Notice VALUES
 (1, N'Welcome to the new school year', N'Timetables, subjects and class teachers are published. Check your timetable for room changes.', N'General', 1, -70),
 (2, N'Midterm results published', N'Midterm marks for all subjects are now available under Results.', N'Result', 0, -12),
 (3, N'October fee reminder', N'The October tuition fee is due on the 10th. Upload the bank slip on the Payments page if you paid at the bank.', N'General', 0, -18),
 (4, N'Attendance reminder', N'Students need at least 75% attendance to sit the final examinations.', N'AttendanceAlert', 1, -8),
 (5, N'Parent-teacher meeting', N'The parent-teacher meeting is on Saturday from 9:00 to 12:00.', N'Announcement', 0, -5);

INSERT INTO notifications (Id, Title, Body, Type, SenderUserId, IsSystemGenerated, IsActive, CreatedAt)
SELECT NEWID(), CONCAT(n.Title, N' - ', sc.SchoolName), n.Body, n.Type, CASE WHEN n.IsSystem = 1 THEN NULL ELSE a.Id END, n.IsSystem, 1, DATEADD(DAY, n.DaysAgo, @Now)
FROM (SELECT SchoolCode, SchoolName, MIN(CampusKey) AS CampusKey FROM @Campus GROUP BY SchoolCode, SchoolName) sc
JOIN users a ON a.Username = CONCAT(N'admin.', sc.CampusKey)
CROSS JOIN @Notice n
WHERE NOT EXISTS (SELECT 1 FROM notifications x WHERE x.Title = CONCAT(n.Title, N' - ', sc.SchoolName));
PRINT CONCAT(N'Notifications added: ', @@ROWCOUNT);

-- Everyone at the school receives the school's notifications; older ones are mostly read.
INSERT INTO notification_recipients (Id, NotificationId, RecipientUserId, IsRead, ReadAt, CreatedAt)
SELECT NEWID(), x.Id, u.Id,
       CASE WHEN n.DaysAgo < -10 AND ABS(CHECKSUM(x.Id, u.Id)) % 100 < 70 THEN 1 ELSE 0 END,
       CASE WHEN n.DaysAgo < -10 AND ABS(CHECKSUM(x.Id, u.Id)) % 100 < 70 THEN DATEADD(DAY, n.DaysAgo + 1, @Now) END,
       x.CreatedAt
FROM (SELECT DISTINCT SchoolName, TenantId FROM @Campus) sc
CROSS JOIN @Notice n
JOIN notifications x ON x.Title = CONCAT(n.Title, N' - ', sc.SchoolName)
JOIN users u ON u.TenantId = sc.TenantId AND u.IsDeleted = 0 AND u.IsActive = 1
WHERE NOT EXISTS (SELECT 1 FROM notification_recipients r WHERE r.NotificationId = x.Id AND r.RecipientUserId = u.Id);
PRINT CONCAT(N'Notification deliveries added: ', @@ROWCOUNT);

COMMIT TRANSACTION;

-- ═════════════════════════════════════════════════════════════════════════════════════
-- Summary per school
-- ═════════════════════════════════════════════════════════════════════════════════════
SELECT t.Code AS School,
       (SELECT COUNT(*) FROM campuses c WHERE c.TenantId = t.Id AND c.IsDeleted = 0) AS Campuses,
       (SELECT COUNT(*) FROM users u WHERE u.TenantId = t.Id AND u.RoleId = @RoleFaculty AND u.IsDeleted = 0) AS Teachers,
       (SELECT COUNT(*) FROM users u WHERE u.TenantId = t.Id AND u.RoleId = @RoleStudent AND u.IsDeleted = 0) AS Students,
       (SELECT COUNT(*) FROM courses k WHERE k.TenantId = t.Id AND k.IsDeleted = 0) AS Subjects,
       (SELECT COUNT(*) FROM course_offerings o WHERE o.TenantId = t.Id AND o.IsDeleted = 0) AS Offerings,
       (SELECT COUNT(*) FROM enrollments e JOIN course_offerings o ON o.Id = e.CourseOfferingId WHERE o.TenantId = t.Id) AS Enrolments,
       (SELECT COUNT(*) FROM payment_receipts r JOIN student_profiles p ON p.Id = r.StudentProfileId JOIN users u ON u.Id = p.UserId WHERE u.TenantId = t.Id) AS FeeReceipts
FROM tenants t
WHERE t.IsDeleted = 0 AND EXISTS (SELECT 1 FROM departments d WHERE d.TenantId = t.Id AND d.InstitutionType = @School)
ORDER BY t.Code;
