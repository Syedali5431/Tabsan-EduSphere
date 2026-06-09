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
PRINT CONCAT('Total departments: ', @DeptCount, ' (expected: 4)');

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

-- ═══════ FINAL REPORT ═══════
PRINT '';
IF @Errors = 0
    PRINT '✓ All checks passed! Database is ready.';
ELSE
    PRINT CONCAT('✗ ', @Errors, ' check(s) failed. Review the output above.');

PRINT '';
PRINT '=== Post-Deployment Checks Complete ===';
GO
