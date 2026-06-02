SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    PRINT N'College validation note: database [Tabsan-EduSphere] does not exist. Run College Scripts/01-Schema-Current.sql first.';
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
    PRINT N'College validation note: required tables are missing. Run schema/core seed scripts first.';
    RETURN;
END;

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
    PRINT N'College validation note: no college students found. Nothing to validate.';
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

DECLARE @HasAnyOfferings BIT = 0;
IF OBJECT_ID(N'[course_offerings]') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM [course_offerings])
        SET @HasAnyOfferings = 1;
END;

IF @CollegeCards < (@CollegeStudentCount * 2)
BEGIN
    PRINT N'College validation warning: Class 11-12 report card coverage is below expectation.';
    RETURN;
END;

IF @HasAnyOfferings = 1
AND @CollegeResults < (@CollegeStudentCount * 2)
BEGIN
    PRINT N'College validation warning: Class 11-12 result coverage is below expectation.';
    RETURN;
END;

IF @HasAnyOfferings = 0
BEGIN
    PRINT N'College validation note: no course offerings found, so result coverage check was skipped.';
END;

IF COL_LENGTH('student_profiles', 'CurrentSemesterNumber') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM [student_profiles] sp
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        WHERE ISNULL(u.[InstitutionType], 1) = 1
          AND ISNULL(u.[IsDeleted], 0) = 0
          AND EXISTS
          (
              SELECT 1
              FROM [student_report_cards] src
              WHERE src.[StudentProfileId] = sp.[Id]
                AND src.[InstitutionType] = 1
                AND src.[PeriodLabel] = N'Class 12'
          )
          AND ISNULL(sp.[CurrentSemesterNumber], 0) < 12
    )
    BEGIN
        PRINT N'College validation warning: completion-level progression after Class 12 is below expectation.';
        RETURN;
    END;
END;

PRINT N'Phase 4 script sync: college period handling uses class labels and does not require semester-only validation gates.';
PRINT N'College validation checks passed.';
GO
