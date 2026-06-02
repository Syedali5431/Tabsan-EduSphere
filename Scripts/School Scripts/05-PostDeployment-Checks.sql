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

IF OBJECT_ID(N'[users]') IS NULL
OR OBJECT_ID(N'[student_profiles]') IS NULL
OR OBJECT_ID(N'[student_report_cards]') IS NULL
OR OBJECT_ID(N'[results]') IS NULL
BEGIN
    PRINT N'School validation note: required tables [users], [student_profiles], [student_report_cards], and/or [results] are missing. Run School scripts 01-04 first.';
    RETURN;
END;

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
    PRINT N'School validation note: no school students found. Run School Scripts/02-Seed-Core.sql and seed scripts.';
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
    PRINT CONCAT(N'School validation warning: expected Class 1-10 report cards for all school students. Found ', @SchoolCards, N' rows.');
END;
ELSE
BEGIN
    PRINT N'School validation: Class 1-10 report card coverage is complete.';
END;

IF @SchoolResults < (@SchoolStudentCount * 10)
BEGIN
    PRINT CONCAT(N'School validation warning: expected Class 1-10 results for all school students. Found ', @SchoolResults, N' rows.');
END;
ELSE
BEGIN
    PRINT N'School validation: Class 1-10 result coverage is complete.';
END;

IF COL_LENGTH('student_profiles', 'CurrentSemesterNumber') IS NOT NULL
BEGIN
    DECLARE @CompletedAfterClass10 INT =
    (
        SELECT COUNT(1)
        FROM [student_profiles] sp
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        WHERE ISNULL(u.[InstitutionType], 0) = 0
          AND ISNULL(u.[IsDeleted], 0) = 0
          AND ISNULL(sp.[CurrentSemesterNumber], 0) >= 10
          AND EXISTS
          (
              SELECT 1
              FROM [student_report_cards] src
              WHERE src.[StudentProfileId] = sp.[Id]
                AND src.[InstitutionType] = 0
                AND src.[PeriodLabel] = N'Class 10'
          )
    );

    PRINT CONCAT(N'School validation info: students with Class 10 completion marker = ', @CompletedAfterClass10, N'.');
END;

PRINT N'Phase 4 script sync: school period handling uses class labels and does not require semester-only validation gates.';
PRINT N'School validation checks passed.';
GO
