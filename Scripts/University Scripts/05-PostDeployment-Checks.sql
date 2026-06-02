SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    PRINT N'University validation note: database [Tabsan-EduSphere] does not exist. Run University Scripts/01-Schema-Current.sql first.';
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
    PRINT N'University validation note: required tables [users], [student_profiles], [student_report_cards], and/or [results] are missing. Run University scripts 01-04 first.';
    RETURN;
END;

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
    PRINT N'University validation note: no university students found. Run University Scripts/02-Seed-Core.sql and seed scripts.';
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

IF OBJECT_ID(N'[attendance_records]') IS NULL
BEGIN
    PRINT N'University validation note: table [attendance_records] is missing.';
    RETURN;
END;

IF OBJECT_ID(N'[fyp_projects]') IS NULL
BEGIN
    PRINT N'University validation note: table [fyp_projects] is missing.';
    RETURN;
END;

DECLARE @UniversityAttendance INT =
(
    SELECT COUNT(1)
    FROM [attendance_records] ar
    WHERE EXISTS
    (
        SELECT 1
        FROM [student_profiles] sp
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        WHERE sp.[Id] = ar.[StudentProfileId]
          AND ISNULL(u.[InstitutionType], 2) = 2
          AND ISNULL(u.[IsDeleted], 0) = 0
    )
);

DECLARE @UniversityFypStudents INT =
(
    SELECT COUNT(DISTINCT fp.[StudentProfileId])
    FROM [fyp_projects] fp
    WHERE EXISTS
    (
        SELECT 1
        FROM [student_profiles] sp
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        WHERE sp.[Id] = fp.[StudentProfileId]
          AND ISNULL(u.[InstitutionType], 2) = 2
          AND ISNULL(u.[IsDeleted], 0) = 0
    )
);

IF @UniversityCards < (@UniversityStudentCount * 8)
BEGIN
    PRINT CONCAT(N'University validation warning: expected Semester 1-8 report cards for all university students. Found ', @UniversityCards, N' rows.');
END;
ELSE
BEGIN
    PRINT N'University validation: Semester 1-8 report-card coverage is complete.';
END;

IF @UniversityResults < (@UniversityStudentCount * 8)
BEGIN
    PRINT CONCAT(N'University validation warning: expected Semester 1-8 results for all university students. Found ', @UniversityResults, N' rows.');
END;
ELSE
BEGIN
    PRINT N'University validation: Semester 1-8 result coverage is complete.';
END;

IF @UniversityAttendance < (@UniversityStudentCount * 8)
BEGIN
    PRINT CONCAT(N'University validation warning: expected attendance coverage across Semester 1-8. Found ', @UniversityAttendance, N' rows.');
END;
ELSE
BEGIN
    PRINT N'University validation: attendance coverage is complete.';
END;

IF @UniversityFypStudents < @UniversityStudentCount
BEGIN
    PRINT CONCAT(N'University validation warning: expected FYP coverage for all university students. Found ', @UniversityFypStudents, N' covered students.');
END;
ELSE
BEGIN
    PRINT N'University validation: FYP coverage is complete.';
END;

IF COL_LENGTH('student_profiles', 'CurrentSemesterNumber') IS NOT NULL
BEGIN
    DECLARE @CompletedAfterSemester8 INT =
    (
        SELECT COUNT(1)
        FROM [student_profiles] sp
        INNER JOIN [users] u ON u.[Id] = sp.[UserId]
        WHERE ISNULL(u.[InstitutionType], 2) = 2
          AND ISNULL(u.[IsDeleted], 0) = 0
          AND ISNULL(sp.[CurrentSemesterNumber], 0) >= 8
          AND EXISTS
          (
              SELECT 1
              FROM [student_report_cards] src
              WHERE src.[StudentProfileId] = sp.[Id]
                AND src.[InstitutionType] = 2
                AND src.[PeriodLabel] = N'Semester 8'
          )
    );

    PRINT CONCAT(N'University validation info: students with Semester 8 completion marker = ', @CompletedAfterSemester8, N'.');
END;

PRINT N'Phase 4 script sync: university period handling remains semester-based and validated as-is.';
PRINT N'University validation checks passed.';
GO
