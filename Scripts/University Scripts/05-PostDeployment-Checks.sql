SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    RAISERROR('Database [Tabsan-EduSphere] does not exist. Run schema script first.', 16, 1);
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

SET NOCOUNT ON;

DECLARE @UniversityStudentCount INT =
(
    SELECT COUNT(1)
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE ISNULL(u.[InstitutionType], 2) = 2
      AND ISNULL(u.[IsDeleted], 0) = 0
);

IF @UniversityStudentCount = 0
BEGIN
    RAISERROR('University validation failed: no university students found.', 16, 1);
    RETURN;
END;

DECLARE @UniversityCards INT =
(
    SELECT COUNT(1)
    FROM [student_report_cards]
    WHERE [InstitutionType] = 2
      AND [PeriodLabel] LIKE N'Semester %'
);

DECLARE @UniversityResults INT =
(
    SELECT COUNT(1)
    FROM [results]
    WHERE [ResultType] LIKE N'Semester %'
      AND EXISTS
      (
          SELECT 1
          FROM [student_profiles] sp
          INNER JOIN [users] u ON u.[Id] = sp.[UserId]
          WHERE sp.[Id] = [results].[StudentProfileId]
            AND ISNULL(u.[InstitutionType], 2) = 2
            AND ISNULL(u.[IsDeleted], 0) = 0
      )
);

IF @UniversityCards < (@UniversityStudentCount * 8)
BEGIN
    RAISERROR('University validation failed: expected Semester 1-8 report cards for all university students.', 16, 1);
    RETURN;
END;

IF @UniversityResults < (@UniversityStudentCount * 8)
BEGIN
    RAISERROR('University validation failed: expected Semester 1-8 results for all university students.', 16, 1);
    RETURN;
END;

PRINT N'University validation checks passed.';
GO
