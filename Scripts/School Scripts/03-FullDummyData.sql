SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
  PRINT N'School dummy data note: database [Tabsan-EduSphere] does not exist. Run School Scripts/01-Schema-Current.sql first.';
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

IF DB_NAME() <> N'Tabsan-EduSphere'
BEGIN
  PRINT N'School dummy data note: failed to switch context to [Tabsan-EduSphere]. Aborting.';
    RETURN;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  School-specific dummy data script.

  PREREQUISITES:
  1. 01-Schema-Current.sql
  2. 02-Seed-Core.sql

  PURPOSE:
  - Seeds deterministic school report-card coverage for all classes (Class 1 to Class 10).
  - Seeds school results/marks rows for those classes.
*/

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'[student_report_cards]') IS NULL OR OBJECT_ID(N'[results]') IS NULL
BEGIN
  PRINT N'School dummy data note: required tables [student_report_cards] and/or [results] were not found. Run Scripts/01-Schema-Current.sql first.';
  ROLLBACK TRANSACTION;
  RETURN;
END;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @SchoolInstitutionType INT = 0;

DECLARE @FallbackFacultyUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) u.[Id]
    FROM [users] u
    INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
    WHERE r.[Name] = N'Faculty'
      AND ISNULL(u.[InstitutionType], @SchoolInstitutionType) = @SchoolInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
    ORDER BY u.[CreatedAt], u.[Id]
);

DECLARE @TargetStudents TABLE
(
    [StudentProfileId] UNIQUEIDENTIFIER NOT NULL,
    [CourseOfferingId] UNIQUEIDENTIFIER NOT NULL,
    [FacultyUserId] UNIQUEIDENTIFIER NOT NULL,
    [StudentRn] INT NOT NULL
);

INSERT INTO @TargetStudents ([StudentProfileId], [CourseOfferingId], [FacultyUserId], [StudentRn])
SELECT sp.[Id],
       COALESCE(depOffering.[CourseOfferingId], instOffering.[CourseOfferingId]),
       COALESCE(depOffering.[FacultyUserId], instOffering.[FacultyUserId], @FallbackFacultyUserId),
       ROW_NUMBER() OVER (ORDER BY sp.[CreatedAt], sp.[Id])
FROM [student_profiles] sp
INNER JOIN [users] su ON su.[Id] = sp.[UserId]
OUTER APPLY
(
    SELECT TOP (1) co.[Id] AS [CourseOfferingId], co.[FacultyUserId]
    FROM [course_offerings] co
    INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
    WHERE c.[DepartmentId] = sp.[DepartmentId]
      AND ISNULL(c.[IsDeleted], 0) = 0
      AND ISNULL(co.[IsDeleted], 0) = 0
      AND co.[FacultyUserId] IS NOT NULL
    ORDER BY co.[CreatedAt], co.[Id]
) depOffering
OUTER APPLY
(
    SELECT TOP (1) co.[Id] AS [CourseOfferingId], co.[FacultyUserId]
    FROM [course_offerings] co
    INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
    INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId]
    WHERE ISNULL(d.[InstitutionType], @SchoolInstitutionType) = @SchoolInstitutionType
      AND ISNULL(c.[IsDeleted], 0) = 0
      AND ISNULL(co.[IsDeleted], 0) = 0
      AND co.[FacultyUserId] IS NOT NULL
    ORDER BY co.[CreatedAt], co.[Id]
) instOffering
WHERE ISNULL(su.[InstitutionType], @SchoolInstitutionType) = @SchoolInstitutionType
  AND ISNULL(su.[IsDeleted], 0) = 0
  AND COALESCE(depOffering.[CourseOfferingId], instOffering.[CourseOfferingId]) IS NOT NULL
  AND COALESCE(depOffering.[FacultyUserId], instOffering.[FacultyUserId], @FallbackFacultyUserId) IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM @TargetStudents)
BEGIN
  PRINT N'School dummy data note: prerequisites not satisfied (students, faculty, or course offerings missing). Run School Scripts/02-Seed-Core.sql and base seeds first.';
  ROLLBACK TRANSACTION;
  RETURN;
END;

DECLARE @ClassCoverage TABLE ([ClassNo] INT, [Marks] DECIMAL(8,2));
INSERT INTO @ClassCoverage ([ClassNo], [Marks]) VALUES
(1, 74.00),
(2, 75.00),
(3, 76.00),
(4, 77.00),
(5, 78.00),
(6, 79.00),
(7, 80.00),
(8, 81.00),
(9, 82.00),
(10, 83.00);

INSERT INTO [student_report_cards] ([Id], [StudentProfileId], [InstitutionType], [PeriodLabel], [PayloadJson], [GeneratedByUserId], [GeneratedAt], [CreatedAt], [UpdatedAt])
SELECT NEWID(),
     ts.[StudentProfileId],
       @SchoolInstitutionType,
       CONCAT(N'Class ', CAST(cc.[ClassNo] AS NVARCHAR(10))),
     CONCAT(N'{"domain":"school","class":', CAST(cc.[ClassNo] AS NVARCHAR(10)), N',"score":', CAST(cc.[Marks] + ((ts.[StudentRn] - 1) % 5) AS NVARCHAR(20)), N',"studentIndex":', CAST(ts.[StudentRn] AS NVARCHAR(20)), N'}'),
     ts.[FacultyUserId],
     DATEADD(day, -(cc.[ClassNo] + ts.[StudentRn]), @Now),
       @Now,
       NULL
FROM @ClassCoverage cc
CROSS JOIN @TargetStudents ts
WHERE NOT EXISTS
(
    SELECT 1
    FROM [student_report_cards] src
  WHERE src.[StudentProfileId] = ts.[StudentProfileId]
      AND src.[InstitutionType] = @SchoolInstitutionType
      AND src.[PeriodLabel] = CONCAT(N'Class ', CAST(cc.[ClassNo] AS NVARCHAR(10)))
);

INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
SELECT NEWID(),
       ts.[StudentProfileId],
       ts.[CourseOfferingId],
       CONCAT(N'Class ', CAST(cc.[ClassNo] AS NVARCHAR(10))),
       cc.[Marks] + ((ts.[StudentRn] - 1) % 5),
       100.00,
       1,
       DATEADD(day, -(cc.[ClassNo] + ts.[StudentRn]), @Now),
       ts.[FacultyUserId],
       @Now,
       NULL
FROM @ClassCoverage cc
CROSS JOIN @TargetStudents ts
WHERE NOT EXISTS
(
    SELECT 1
    FROM [results] r
    WHERE r.[StudentProfileId] = ts.[StudentProfileId]
      AND r.[CourseOfferingId] = ts.[CourseOfferingId]
      AND r.[ResultType] = CONCAT(N'Class ', CAST(cc.[ClassNo] AS NVARCHAR(10)))
);

/*
  Mark school completion after Class 10 where schema supports lifecycle columns.
*/
IF COL_LENGTH('student_profiles', 'CurrentSemesterNumber') IS NOT NULL
BEGIN
    UPDATE sp
    SET sp.[CurrentSemesterNumber] = CASE WHEN ISNULL(sp.[CurrentSemesterNumber], 0) < 10 THEN 10 ELSE sp.[CurrentSemesterNumber] END,
        sp.[UpdatedAt] = @Now
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE ISNULL(u.[InstitutionType], @SchoolInstitutionType) = @SchoolInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
      AND EXISTS
      (
          SELECT 1
          FROM [student_report_cards] src
          WHERE src.[StudentProfileId] = sp.[Id]
            AND src.[InstitutionType] = @SchoolInstitutionType
            AND src.[PeriodLabel] = N'Class 10'
      );
END;

IF COL_LENGTH('student_profiles', 'Status') IS NOT NULL AND COL_LENGTH('student_profiles', 'GraduatedDate') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql
        N'UPDATE sp
          SET sp.[Status] = N''Graduated'',
              sp.[GraduatedDate] = COALESCE(sp.[GraduatedDate], @Now),
              sp.[UpdatedAt] = @Now
          FROM [student_profiles] sp
          INNER JOIN [users] u ON u.[Id] = sp.[UserId]
          WHERE ISNULL(u.[InstitutionType], @SchoolInstitutionType) = @SchoolInstitutionType
            AND ISNULL(u.[IsDeleted], 0) = 0
            AND EXISTS
            (
                SELECT 1
                FROM [student_report_cards] src
                WHERE src.[StudentProfileId] = sp.[Id]
                  AND src.[InstitutionType] = @SchoolInstitutionType
                  AND src.[PeriodLabel] = N''Class 10''
            );',
        N'@Now DATETIME2, @SchoolInstitutionType INT',
        @Now = @Now,
        @SchoolInstitutionType = @SchoolInstitutionType;
END;

PRINT N'School lifecycle completion applied for students with Class 10 coverage (where supported by schema).';

COMMIT TRANSACTION;
PRINT N'Phase 4 script sync: school class-period write scope is web-driven; SQL seed remains class coverage aligned.';
PRINT N'School dummy data seeded successfully (all school students, Class 1 to Class 10 with completion after Class 10).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT CONCAT(N'School dummy data warning: ', ERROR_MESSAGE());
END CATCH;
GO
