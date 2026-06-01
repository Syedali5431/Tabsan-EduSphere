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

IF OBJECT_ID(N'[attendance_records]') IS NULL
BEGIN
    RAISERROR('University validation failed: table [attendance_records] is missing.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'[fyp_projects]') IS NULL
BEGIN
    RAISERROR('University validation failed: table [fyp_projects] is missing.', 16, 1);
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
    RAISERROR('University validation failed: expected Semester 1-8 report cards for all university students.', 16, 1);
    RETURN;
END;

IF @UniversityResults < (@UniversityStudentCount * 8)
BEGIN
    RAISERROR('University validation failed: expected Semester 1-8 results for all university students.', 16, 1);
    RETURN;
END;

IF @UniversityAttendance < (@UniversityStudentCount * 8)
BEGIN
    RAISERROR('University validation failed: expected attendance coverage across Semester 1-8 for all university students.', 16, 1);
    RETURN;
END;

IF @UniversityFypStudents < @UniversityStudentCount
BEGIN
    RAISERROR('University validation failed: expected FYP coverage for all university students.', 16, 1);
    RETURN;
END;

PRINT N'University validation checks passed.';
GO
