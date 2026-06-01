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

DECLARE @CollegeStudentCount INT =
(
    SELECT COUNT(1)
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE ISNULL(u.[InstitutionType], 1) = 1
      AND ISNULL(u.[IsDeleted], 0) = 0
);

IF @CollegeStudentCount = 0
BEGIN
    RAISERROR('College validation failed: no college students found.', 16, 1);
    RETURN;
END;

DECLARE @CollegeCards INT =
(
    SELECT COUNT(1)
    FROM [student_report_cards]
    WHERE [InstitutionType] = 1
      AND [PeriodLabel] IN (N'Class 11', N'Class 12')
);

DECLARE @CollegeResults INT =
(
    SELECT COUNT(1)
    FROM [results]
    WHERE [ResultType] IN (N'Class 11', N'Class 12')
      AND EXISTS
      (
          SELECT 1
          FROM [student_profiles] sp
          INNER JOIN [users] u ON u.[Id] = sp.[UserId]
          WHERE sp.[Id] = [results].[StudentProfileId]
            AND ISNULL(u.[InstitutionType], 1) = 1
            AND ISNULL(u.[IsDeleted], 0) = 0
      )
);

IF @CollegeCards < (@CollegeStudentCount * 2)
BEGIN
    RAISERROR('College validation failed: expected Class 11-12 report cards for all college students.', 16, 1);
    RETURN;
END;

IF @CollegeResults < (@CollegeStudentCount * 2)
BEGIN
    RAISERROR('College validation failed: expected Class 11-12 results for all college students.', 16, 1);
    RETURN;
END;

PRINT N'College validation checks passed.';
GO
