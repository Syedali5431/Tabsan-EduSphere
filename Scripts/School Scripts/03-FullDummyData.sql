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
DECLARE @SchoolCanonicalCourseCode NVARCHAR(50) = N'SCH-CORE-1TO10';

IF OBJECT_ID(N'[departments]') IS NOT NULL
AND OBJECT_ID(N'[academic_programs]') IS NOT NULL
AND OBJECT_ID(N'[courses]') IS NOT NULL
AND OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND OBJECT_ID(N'[semesters]') IS NOT NULL
BEGIN
  DECLARE @SchoolCanonicalDepartmentId UNIQUEIDENTIFIER = CAST('13333333-3333-3333-3333-333333333331' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolCanonicalProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222431' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolCanonicalCourseId UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444428' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolCanonicalOfferingId UNIQUEIDENTIFIER = CAST('75555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolSeedFacultyUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) u.[Id]
    FROM [users] u
    INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
    WHERE r.[Name] = N'Faculty'
      AND ISNULL(u.[InstitutionType], @SchoolInstitutionType) = @SchoolInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
    ORDER BY u.[CreatedAt], u.[Id]
  );
  DECLARE @SchoolSemesterId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [Id]
    FROM [semesters]
    WHERE ISNULL([IsDeleted], 0) = 0
    ORDER BY [CreatedAt], [Id]
  );

  INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @SchoolCanonicalDepartmentId, N'School Core Department', N'SCHCORE', @SchoolInstitutionType, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @SchoolCanonicalDepartmentId);

  INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @SchoolCanonicalProgramId, N'School Core (Class 1-10)', N'CLS1_10', @SchoolCanonicalDepartmentId, 10, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @SchoolCanonicalProgramId);

  INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @SchoolCanonicalCourseId, N'School Core Studies (Class 1-10)', @SchoolCanonicalCourseCode, 3, @SchoolCanonicalDepartmentId, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [courses] WHERE [Id] = @SchoolCanonicalCourseId);

  UPDATE [courses]
  SET [Title] = N'School Core Studies (Class 1-10)',
    [Code] = @SchoolCanonicalCourseCode,
    [DepartmentId] = @SchoolCanonicalDepartmentId,
    [CreditHours] = 3,
    [IsActive] = 1,
    [IsDeleted] = 0,
    [DeletedAt] = NULL,
    [UpdatedAt] = @Now
  WHERE [Id] = @SchoolCanonicalCourseId;

  IF @SchoolSemesterId IS NOT NULL
  BEGIN
    INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @SchoolCanonicalOfferingId, @SchoolCanonicalCourseId, @SchoolSemesterId, @SchoolSeedFacultyUserId, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [course_offerings] WHERE [Id] = @SchoolCanonicalOfferingId);
  END;
END;

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
    ORDER BY CASE WHEN c.[Code] = @SchoolCanonicalCourseCode THEN 0 ELSE 1 END,
             co.[CreatedAt], co.[Id]
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
    ORDER BY CASE WHEN c.[Code] = @SchoolCanonicalCourseCode THEN 0 ELSE 1 END,
             co.[CreatedAt], co.[Id]
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

IF OBJECT_ID(N'[timetables]') IS NOT NULL
AND OBJECT_ID(N'[timetable_entries]') IS NOT NULL
AND OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND OBJECT_ID(N'[academic_programs]') IS NOT NULL
BEGIN
  DECLARE @SchoolTimetableId UNIQUEIDENTIFIER = CAST('91000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolDepartmentId UNIQUEIDENTIFIER = (
    SELECT TOP (1) sp.[DepartmentId]
    FROM @TargetStudents ts
    INNER JOIN [student_profiles] sp ON sp.[Id] = ts.[StudentProfileId]
    ORDER BY ts.[StudentRn]
  );
  DECLARE @SchoolSemesterId UNIQUEIDENTIFIER = (
    SELECT TOP (1) co.[SemesterId]
    FROM @TargetStudents ts
    INNER JOIN [course_offerings] co ON co.[Id] = ts.[CourseOfferingId]
    WHERE co.[SemesterId] IS NOT NULL
    ORDER BY ts.[StudentRn]
  );
  DECLARE @SchoolProgramId UNIQUEIDENTIFIER = (
    SELECT TOP (1) ap.[Id]
    FROM [academic_programs] ap
    WHERE ap.[DepartmentId] = @SchoolDepartmentId
      AND ISNULL(ap.[IsDeleted], 0) = 0
    ORDER BY ap.[CreatedAt], ap.[Id]
  );

  IF @SchoolSemesterId IS NULL
    SELECT TOP (1) @SchoolSemesterId = s.[Id] FROM [semesters] s WHERE ISNULL(s.[IsDeleted], 0) = 0 ORDER BY s.[CreatedAt], s.[Id];

  IF @SchoolDepartmentId IS NOT NULL AND @SchoolSemesterId IS NOT NULL AND @SchoolProgramId IS NOT NULL
  BEGIN
    INSERT INTO [timetables] ([Id], [DepartmentId], [AcademicProgramId], [SemesterId], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt], [EffectiveDate], [SemesterNumber])
    SELECT @SchoolTimetableId, @SchoolDepartmentId, @SchoolProgramId, @SchoolSemesterId, 1, @Now, @Now, NULL, 0, NULL, CAST(@Now AS date), 1
    WHERE NOT EXISTS (SELECT 1 FROM [timetables] t WHERE t.[Id] = @SchoolTimetableId);

    DECLARE @SchoolOfferingPool TABLE
    (
      [PoolRank] INT NOT NULL,
      [CourseId] UNIQUEIDENTIFIER NOT NULL,
      [FacultyUserId] UNIQUEIDENTIFIER NULL
    );

    INSERT INTO @SchoolOfferingPool ([PoolRank], [CourseId], [FacultyUserId])
    SELECT
      ROW_NUMBER() OVER (ORDER BY co.[CreatedAt], co.[Id]),
      co.[CourseId],
      co.[FacultyUserId]
    FROM
    (
      SELECT DISTINCT co.[Id], co.[CreatedAt], co.[CourseId], co.[FacultyUserId]
      FROM @TargetStudents ts
      INNER JOIN [course_offerings] co ON co.[Id] = ts.[CourseOfferingId]
      WHERE co.[CourseId] IS NOT NULL
        AND ISNULL(co.[IsDeleted], 0) = 0
    ) co;

    DECLARE @SchoolOfferingPoolCount INT = (SELECT COUNT(1) FROM @SchoolOfferingPool);

    IF @SchoolOfferingPoolCount > 0
    BEGIN
      ;WITH ClassEntries AS
      (
        SELECT
          cc.[ClassNo],
          ((cc.[ClassNo] - 1) % @SchoolOfferingPoolCount) + 1 AS [PoolRank]
        FROM @ClassCoverage cc
      )
      INSERT INTO [timetable_entries] ([Id], [TimetableId], [DayOfWeek], [StartTime], [EndTime], [SubjectName], [RoomNumber], [FacultyName], [RoomId], [CreatedAt], [UpdatedAt], [BuildingId], [CourseId], [FacultyUserId])
      SELECT
        NEWID(),
        @SchoolTimetableId,
        ((ce.[ClassNo] - 1) % 5) + 1,
        CAST('08:30:00' AS time),
        CAST('10:00:00' AS time),
        CONCAT(N'Class ', CAST(ce.[ClassNo] AS NVARCHAR(10)), N' - School Core'),
        N'S-101',
        COALESCE(fu.[Username], N'school.faculty'),
        NULL,
        @Now,
        NULL,
        NULL,
        op.[CourseId],
        op.[FacultyUserId]
      FROM ClassEntries ce
      INNER JOIN @SchoolOfferingPool op ON op.[PoolRank] = ce.[PoolRank]
      LEFT JOIN [users] fu ON fu.[Id] = op.[FacultyUserId]
      WHERE NOT EXISTS
      (
        SELECT 1
        FROM [timetable_entries] te
        WHERE te.[TimetableId] = @SchoolTimetableId
          AND te.[SubjectName] = CONCAT(N'Class ', CAST(ce.[ClassNo] AS NVARCHAR(10)), N' - School Core')
      );
    END;
  END;
END;

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

IF COL_LENGTH('student_profiles', 'Status') IS NOT NULL
AND COL_LENGTH('student_profiles', 'GraduatedDate') IS NOT NULL
AND EXISTS
(
  SELECT 1
  FROM sys.columns c
  INNER JOIN sys.types t ON t.[user_type_id] = c.[user_type_id]
  WHERE c.[object_id] = OBJECT_ID(N'student_profiles')
    AND c.[name] = N'Status'
    AND t.[name] IN (N'nvarchar', N'varchar', N'nchar', N'char')
)
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
