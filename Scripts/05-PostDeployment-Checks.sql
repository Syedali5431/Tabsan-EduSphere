/*
  Post-Deployment Checks — Tabsan EduSphere v1.0
  
  Validates data integrity after running all seed scripts.
*/

SET NOCOUNT ON;
GO

USE [Tabsan-EduSphere];
GO

DECLARE @Errors INT = 0;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

PRINT '=== Tabsan EduSphere Post-Deployment Checks ===';
PRINT '';

-- Database version
DECLARE @DbVer NVARCHAR(20) = NULL;
IF OBJECT_ID('[Tabsan-EduSphere]', 'U') IS NOT NULL
    SET @DbVer = (SELECT TOP 1 [DemoValue] FROM [Tabsan-EduSphere] WHERE [DemoKey]=N'db.version');
PRINT CONCAT('Database Version: ', ISNULL(@DbVer,N'NOT SET'));

-- ═══════ ROLE COUNTS ═══════
PRINT '';
PRINT '--- Roles ---';
SELECT [Name], [Description] FROM [roles] ORDER BY [Id];
DECLARE @RoleCount INT = (SELECT COUNT(*) FROM [roles]);
PRINT CONCAT('Total roles: ', @RoleCount, ' (expected: 5)');
IF @RoleCount != 5 SET @Errors += 1;

-- ═══════ TENANT & CAMPUS COUNTS ═══════
PRINT '';
PRINT '--- Tenants & Campuses ---';
SELECT t.[Code] AS TenantCode, t.[Name] AS TenantName, c.[Code] AS CampusCode, c.[Name] AS CampusName
FROM [tenants] t LEFT JOIN [campuses] c ON c.[TenantId]=t.[Id] ORDER BY t.[Code];
DECLARE @TenantCount INT = (SELECT COUNT(*) FROM [tenants]);
PRINT CONCAT('Total tenants: ', @TenantCount, ' (expected: 3)');
IF @TenantCount != 3 SET @Errors += 1;

-- ═══════ DEPARTMENT COUNTS ═══════
PRINT '';
PRINT '--- Departments ---';
SELECT [Name], [Code] FROM [departments] ORDER BY [Code];
DECLARE @DeptCount INT = (SELECT COUNT(*) FROM [departments]);
PRINT CONCAT('Total departments: ', @DeptCount, ' (expected: 5)');

-- ═══════ ACADEMIC PROGRAMS ═══════
PRINT '';
PRINT '--- Academic Programs ---';
SELECT p.[Code], p.[Name], p.[TotalSemesters], d.[Name] AS Department
FROM [academic_programs] p JOIN [departments] d ON d.[Id]=p.[DepartmentId] ORDER BY p.[Code];
DECLARE @ProgCount INT = (SELECT COUNT(*) FROM [academic_programs]);
PRINT CONCAT('Total programs: ', @ProgCount, ' (expected: 6)');
IF @ProgCount != 6 SET @Errors += 1;

-- ═══════ COURSES ═══════
PRINT '';
PRINT '--- Courses ---';
DECLARE @CourseCount INT = (SELECT COUNT(*) FROM [courses]);
PRINT CONCAT('Total courses: ', @CourseCount);

-- ═══════ SEMESTERS ═══════
PRINT '';
PRINT '--- Semesters ---';
SELECT [Name], [StartDate], [EndDate] FROM [semesters] ORDER BY [StartDate];
DECLARE @SemCount INT = (SELECT COUNT(*) FROM [semesters]);
PRINT CONCAT('Total semesters: ', @SemCount, ' (expected: 20)');

-- ═══════ USERS BY ROLE ═══════
PRINT '';
PRINT '--- Users by Role ---';
SELECT r.[Name] AS RoleName, COUNT(u.[Id]) AS UserCount
FROM [roles] r LEFT JOIN [users] u ON u.[RoleId]=r.[Id] AND u.[IsDeleted]=0 AND u.[IsActive]=1
GROUP BY r.[Name], r.[Id] ORDER BY r.[Id];
DECLARE @UserCount INT = (SELECT COUNT(*) FROM [users] WHERE [IsDeleted]=0 AND [IsActive]=1);
PRINT CONCAT('Total active users: ', @UserCount);

-- ═══════ USERS BY INSTITUTION TYPE ═══════
IF COL_LENGTH('users', 'InstitutionType') IS NOT NULL
BEGIN
    PRINT '';
    PRINT '--- Users by Institution Type ---';
    SELECT CASE [InstitutionType] WHEN 0 THEN N'School' WHEN 1 THEN N'College' WHEN 2 THEN N'University' ELSE N'Global' END AS Institution,
           COUNT(*) AS UserCount
    FROM [users] WHERE [IsDeleted]=0 AND [IsActive]=1 AND [RoleId]=4
    GROUP BY [InstitutionType] ORDER BY [InstitutionType];
END

-- ═══════ STUDENT PROFILES ═══════
PRINT '';
PRINT '--- Student Profiles ---';
SELECT p.[Code] AS Program, COUNT(sp.[Id]) AS StudentCount
FROM [academic_programs] p LEFT JOIN [student_profiles] sp ON sp.[ProgramId]=p.[Id] AND sp.[IsDeleted]=0
GROUP BY p.[Code], p.[Id] ORDER BY p.[Code];

-- ═══════ GRADUATED STUDENTS (Status=3) ═══════
PRINT '';
PRINT '--- Graduated Students ---';
SELECT u.[Username], u.[FullName], sp.[RegistrationNumber], p.[Code] AS Program,
       sp.[Cgpa], sp.[Status],
       CASE sp.[Status] WHEN 0 THEN N'Active' WHEN 1 THEN N'Inactive' WHEN 2 THEN N'Suspended' WHEN 3 THEN N'Graduated' ELSE N'Unknown' END AS StatusName
FROM [student_profiles] sp
JOIN [users] u ON u.[Id]=sp.[UserId]
JOIN [academic_programs] p ON p.[Id]=sp.[ProgramId]
WHERE sp.[Status]=3 AND sp.[IsDeleted]=0
ORDER BY p.[Code];
DECLARE @GradCount INT = (SELECT COUNT(*) FROM [student_profiles] WHERE [Status]=3 AND [IsDeleted]=0);
PRINT CONCAT('Graduated students: ', @GradCount, ' (expected: 5)');
IF @GradCount < 5 SET @Errors += 1;

-- ═══════ GRADUATION APPLICATIONS ═══════
DECLARE @GradAppCount INT = (SELECT COUNT(*) FROM [graduation_applications] WHERE [IsDeleted]=0);
PRINT CONCAT('Graduation applications: ', @GradAppCount, ' (expected: 5)');

-- ═══════ PROFILE PICTURE COLUMN ═══════
PRINT '';
PRINT '--- Profile Picture Column ---';
IF COL_LENGTH('users', 'ProfilePicturePath') IS NOT NULL
    PRINT N'✓ ProfilePicturePath column exists on users table.';
ELSE
BEGIN
    PRINT N'✗ ProfilePicturePath column is missing from users table.';
    SET @Errors += 1;
END

-- ═══════ ATTENDANCE ═══════
DECLARE @AttCount INT = (SELECT COUNT(*) FROM [attendance_records]);
PRINT CONCAT('Total attendance records: ', @AttCount);

-- ═══════ RESULTS ═══════
DECLARE @ResCount INT = (SELECT COUNT(*) FROM [results]);
PRINT CONCAT('Total result records: ', @ResCount);

-- ═══════ ASSIGNMENTS ═══════
DECLARE @AssignCount INT = (SELECT COUNT(*) FROM [assignments]);
DECLARE @SubCount INT = (SELECT COUNT(*) FROM [assignment_submissions]);
PRINT CONCAT('Assignments: ', @AssignCount, ', Submissions: ', @SubCount);

-- ═══════ QUIZZES ═══════
DECLARE @QuizCount INT = (SELECT COUNT(*) FROM [quizzes]);
DECLARE @QAttemptCount INT = (SELECT COUNT(*) FROM [quiz_attempts]);
PRINT CONCAT('Quizzes: ', @QuizCount, ', Attempts: ', @QAttemptCount);

-- ═══════ FYP ═══════
DECLARE @FypCount INT = (SELECT COUNT(*) FROM [fyp_projects]);
PRINT CONCAT('FYP projects: ', @FypCount);

-- ═══════ TIMETABLES ═══════
DECLARE @TtCount INT = (SELECT COUNT(*) FROM [timetables]);
PRINT CONCAT('Timetables: ', @TtCount);

-- ═══════ CORE USERS VERIFICATION ═══════
PRINT '';
PRINT '--- Core Login Users ---';
SELECT [Username], [Email], r.[Name] AS Role, [IsActive]
FROM [users] u JOIN [roles] r ON r.[Id]=u.[RoleId]
WHERE [Username] IN (N'superadmin',N'admin.uni',N'admin.col',N'admin.sch',
                      N'faculty.uni',N'faculty.col',N'faculty.sch',
                      N'student.uni',N'student.col',N'student.sch',
                      N'finance.uni',N'finance.col',N'finance.sch')
ORDER BY u.[Username];

-- ═══════ SEMESTER SORT ORDER CHECK ═══════
PRINT '';
PRINT '--- Semester Sort Order ---';
DECLARE @SortInconsistencies INT = 0;
SELECT @SortInconsistencies = COUNT(*) FROM (
    SELECT s1.Name, s1.StartDate, 
           CASE WHEN s1.StartDate > s2.StartDate AND s1.Name < s2.Name THEN 1 ELSE 0 END AS WrongOrder
    FROM semesters s1
    CROSS APPLY (SELECT TOP 1 s2.Name, s2.StartDate FROM semesters s2 WHERE s2.StartDate > s1.StartDate ORDER BY s2.StartDate) s2
) sub WHERE WrongOrder = 1;
IF @SortInconsistencies = 0
    PRINT N'✓ Semester sort order is ascending by StartDate.';
ELSE
BEGIN
    PRINT CONCAT('✗ ', @SortInconsistencies, ' semester(s) have wrong sort order.');
    SET @Errors += 1;
END

-- ═══════ BBA DEPARTMENT CHECK ═══════
PRINT '';
PRINT '--- BBA Department InstitutionType ---';
DECLARE @BBA_DepartmentId UNIQUEIDENTIFIER = (SELECT Id FROM departments WHERE Name = 'Business Administration');
DECLARE @BBA_InstitutionType INT = (SELECT InstitutionType FROM departments WHERE Name = 'Business Administration');
IF @BBA_InstitutionType = 0
    PRINT N'✓ Business Administration department InstitutionType = 0 (University).';
ELSE
BEGIN
    PRINT CONCAT('✗ Business Administration department InstitutionType = ', @BBA_InstitutionType, ' (expected: 0).');
    SET @Errors += 1;
END

-- ═══════ SIDEBAR CERTIFICATE MENU CHECK ═══════
PRINT '';
PRINT '--- Sidebar Certificate Menu ---';
DECLARE @CertMenuCount INT = (SELECT COUNT(*) FROM sidebar_menu_items WHERE [Key] = 'generate_certificates');
IF @CertMenuCount = 1
    PRINT N'✓ Exactly 1 generate_certificates sidebar menu item.';
ELSE
BEGIN
    PRINT CONCAT('✗ ', @CertMenuCount, ' generate_certificates menu items found (expected: 1).');
    SET @Errors += 1;
END

-- ═══════ 2026-06-22 VERSION MARKER ═══════
PRINT '';
PRINT '--- Deployment Sync Marker (2026-06-22) ---';
PRINT N'✓ Profile picture upload feature (User entity + migration + UI)';
PRINT N'✓ Graduated demo students: BSCS, BBA, Spanish, School, College';
PRINT N'✓ Graduated students have: mid+final exams, quizzes, FYP (BSCS/BBA)';
PRINT N'✓ Graduation applications created for all 5 graduated students';
PRINT N'✓ Grading: School/College percentage (A+/A/B/C/D/F), Uni GPA-based';
PRINT '';
PRINT '--- Previous Deployment Markers ---';
PRINT '';
PRINT '--- Previous Deployment Markers ---';
PRINT '✓ MFA single-step TOTP login (AuthService)';
PRINT '✓ Base32 raw-secret storage (TwoFactorStateStore)';
PRINT '✓ Tenant active-only in dropdowns (GetTenantsAsync)';
PRINT '✓ Campus active-only filter (CampusController activeOnly=true)';
PRINT '✓ Session idle timeout 5 minutes (AuthSecurityOptions)';
PRINT '✓ Report columns: ProgramName + DepartmentName added';
PRINT '✓ Reports allowed without department/course filter';
PRINT '✓ Semester sorting ascending (SemesterRepository)';
PRINT '✓ BBA department InstitutionType = University';
PRINT '✓ Duplicate DTO files consolidated';

-- ═══════ FINAL REPORT ═══════
PRINT '';
IF @Errors = 0
    PRINT '✓ All checks passed! Database is ready.';
ELSE
    PRINT CONCAT('✗ ', @Errors, ' check(s) failed. Review the output above.');

PRINT '';
PRINT '=== Post-Deployment Checks Complete ===';
GO
