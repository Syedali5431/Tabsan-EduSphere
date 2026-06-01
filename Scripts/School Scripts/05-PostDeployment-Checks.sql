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

DECLARE @SchoolStudentCount INT =
(
    SELECT COUNT(1)
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE ISNULL(u.[InstitutionType], 0) = 0
      AND ISNULL(u.[IsDeleted], 0) = 0
);

IF @SchoolStudentCount = 0
BEGIN
    RAISERROR('School validation failed: no school students found.', 16, 1);
    RETURN;
END;

DECLARE @SchoolCards INT =
(
    SELECT COUNT(1)
    FROM [student_report_cards]
    WHERE [InstitutionType] = 0
      AND [PeriodLabel] LIKE N'Class %'
);

DECLARE @SchoolResults INT =
(
    SELECT COUNT(1)
    FROM [results]
    WHERE [ResultType] LIKE N'Class %'
      AND EXISTS
      (
          SELECT 1
          FROM [student_profiles] sp
          INNER JOIN [users] u ON u.[Id] = sp.[UserId]
          WHERE sp.[Id] = [results].[StudentProfileId]
            AND ISNULL(u.[InstitutionType], 0) = 0
            AND ISNULL(u.[IsDeleted], 0) = 0
      )
);

IF @SchoolCards < (@SchoolStudentCount * 10)
BEGIN
    RAISERROR('School validation failed: expected Class 1-10 report cards for all school students.', 16, 1);
    RETURN;
END;

IF @SchoolResults < (@SchoolStudentCount * 10)
BEGIN
    RAISERROR('School validation failed: expected Class 1-10 results for all school students.', 16, 1);
    RETURN;
END;

PRINT N'School validation checks passed.';
GO
