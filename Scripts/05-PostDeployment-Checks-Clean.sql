/*
  Post-Deployment Checks (Clean) — Tabsan EduSphere v1.0
  Minimal version — only validates critical counts.
*/
SET NOCOUNT ON;
GO
USE [Tabsan-EduSphere];
GO

-- Verify required data exists
DECLARE @Errors INT = 0;

IF (SELECT COUNT(*) FROM [roles]) < 5 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [tenants]) < 3 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [departments]) < 4 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [academic_programs]) < 6 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [semesters]) < 15 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [users] WHERE [IsActive]=1 AND [IsDeleted]=0) < 10 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [student_profiles] WHERE [IsActive]=1) < 50 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [attendance_records]) < 100 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [results]) < 100 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [timetables]) < 5 SET @Errors += 1;
IF (SELECT COUNT(*) FROM [fyp_projects]) < 5 SET @Errors += 1;

IF @Errors = 0
    PRINT 'OK: All post-deployment checks passed.';
ELSE
    PRINT CONCAT('WARNING: ', @Errors, ' checks failed.');
GO
