SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
  PRINT N'University dummy data note: database [Tabsan-EduSphere] does not exist. Run University Scripts/01-Schema-Current.sql first.';
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

IF DB_NAME() <> N'Tabsan-EduSphere'
BEGIN
  PRINT N'University dummy data note: failed to switch context to [Tabsan-EduSphere]. Aborting.';
    RETURN;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  University-specific dummy data script.

  PREREQUISITES:
  1. 01-Schema-Current.sql
  2. 02-Seed-Core.sql

  PURPOSE:
  - Seeds deterministic university report-card coverage for all semesters (Semester 1 to Semester 8).
  - Seeds university results/marks rows for those semesters.
*/

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'[student_report_cards]') IS NULL OR OBJECT_ID(N'[results]') IS NULL
BEGIN
  PRINT N'University dummy data note: required tables [student_report_cards] and/or [results] were not found. Run Scripts/01-Schema-Current.sql first.';
  ROLLBACK TRANSACTION;
  RETURN;
END;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @UniversityInstitutionType INT = 2;
DECLARE @UniversityItCourseCode NVARCHAR(50) = N'UNI-IT-CORE';
DECLARE @UniversityBusinessCourseCode NVARCHAR(50) = N'UNI-BUS-CORE';
DECLARE @UniversityLanguageCourseCode NVARCHAR(50) = N'UNI-LANG-CORE';

IF OBJECT_ID(N'[departments]') IS NOT NULL
AND OBJECT_ID(N'[academic_programs]') IS NOT NULL
AND OBJECT_ID(N'[courses]') IS NOT NULL
AND OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND OBJECT_ID(N'[semesters]') IS NOT NULL
BEGIN
  DECLARE @UniversityItDepartmentId UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityBusinessDepartmentId UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111112' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityLanguageDepartmentId UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111114' AS UNIQUEIDENTIFIER);

  DECLARE @UniversityBscsProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222211' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityBsseProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222212' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityBbaProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222214' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityLanguageProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222218' AS UNIQUEIDENTIFIER);

  DECLARE @UniversityItCourseId UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444401' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityBusinessCourseId UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444408' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityLanguageCourseId UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444437' AS UNIQUEIDENTIFIER);

  DECLARE @UniversityItOfferingId UNIQUEIDENTIFIER = CAST('75555555-5555-5555-5555-555555555701' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityBusinessOfferingId UNIQUEIDENTIFIER = CAST('75555555-5555-5555-5555-555555555702' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityLanguageOfferingId UNIQUEIDENTIFIER = CAST('75555555-5555-5555-5555-555555555703' AS UNIQUEIDENTIFIER);

  DECLARE @UniversitySeedFacultyUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) u.[Id]
    FROM [users] u
    INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
    WHERE r.[Name] = N'Faculty'
      AND ISNULL(u.[InstitutionType], @UniversityInstitutionType) = @UniversityInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
    ORDER BY u.[CreatedAt], u.[Id]
  );
  DECLARE @UniversitySemesterId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [Id]
    FROM [semesters]
    WHERE ISNULL([IsDeleted], 0) = 0
    ORDER BY [CreatedAt], [Id]
  );

  INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityItDepartmentId, N'IT Department', N'IT', @UniversityInstitutionType, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @UniversityItDepartmentId);

  INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityBusinessDepartmentId, N'Business Department', N'BUS', @UniversityInstitutionType, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @UniversityBusinessDepartmentId);

  INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityLanguageDepartmentId, N'Language Department', N'LANG', @UniversityInstitutionType, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @UniversityLanguageDepartmentId);

  INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityBscsProgramId, N'BS Computer Science', N'BSCS', @UniversityItDepartmentId, 8, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @UniversityBscsProgramId);

  INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityBsseProgramId, N'BS Software Engineering', N'BSSE', @UniversityItDepartmentId, 8, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @UniversityBsseProgramId);

  INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityBbaProgramId, N'BBA', N'BBA', @UniversityBusinessDepartmentId, 8, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @UniversityBbaProgramId);

  INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityLanguageProgramId, N'Language Studies (1 Year)', N'LANG-1Y', @UniversityLanguageDepartmentId, 2, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @UniversityLanguageProgramId);

  UPDATE [academic_programs]
  SET [Name] = CASE [Id]
           WHEN @UniversityBscsProgramId THEN N'BS Computer Science'
           WHEN @UniversityBsseProgramId THEN N'BS Software Engineering'
           WHEN @UniversityBbaProgramId THEN N'BBA'
           WHEN @UniversityLanguageProgramId THEN N'Language Studies (1 Year)'
         END,
    [Code] = CASE [Id]
           WHEN @UniversityBscsProgramId THEN N'BSCS'
           WHEN @UniversityBsseProgramId THEN N'BSSE'
           WHEN @UniversityBbaProgramId THEN N'BBA'
           WHEN @UniversityLanguageProgramId THEN N'LANG-1Y'
         END,
    [DepartmentId] = CASE [Id]
               WHEN @UniversityBscsProgramId THEN @UniversityItDepartmentId
               WHEN @UniversityBsseProgramId THEN @UniversityItDepartmentId
               WHEN @UniversityBbaProgramId THEN @UniversityBusinessDepartmentId
               WHEN @UniversityLanguageProgramId THEN @UniversityLanguageDepartmentId
             END,
    [TotalSemesters] = CASE [Id]
                 WHEN @UniversityLanguageProgramId THEN 2
                 ELSE 8
               END,
    [IsActive] = 1,
    [IsDeleted] = 0,
    [DeletedAt] = NULL,
    [UpdatedAt] = @Now
  WHERE [Id] IN (@UniversityBscsProgramId, @UniversityBsseProgramId, @UniversityBbaProgramId, @UniversityLanguageProgramId);

  INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityItCourseId, N'IT Core Computing', @UniversityItCourseCode, 3, @UniversityItDepartmentId, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [courses] WHERE [Id] = @UniversityItCourseId);

  INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityBusinessCourseId, N'Business Core Management', @UniversityBusinessCourseCode, 3, @UniversityBusinessDepartmentId, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [courses] WHERE [Id] = @UniversityBusinessCourseId);

  INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @UniversityLanguageCourseId, N'Language Core Studies', @UniversityLanguageCourseCode, 3, @UniversityLanguageDepartmentId, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [courses] WHERE [Id] = @UniversityLanguageCourseId);

  UPDATE [courses]
  SET [Title] = CASE [Id]
            WHEN @UniversityItCourseId THEN N'IT Core Computing'
            WHEN @UniversityBusinessCourseId THEN N'Business Core Management'
            WHEN @UniversityLanguageCourseId THEN N'Language Core Studies'
          END,
    [Code] = CASE [Id]
           WHEN @UniversityItCourseId THEN @UniversityItCourseCode
           WHEN @UniversityBusinessCourseId THEN @UniversityBusinessCourseCode
           WHEN @UniversityLanguageCourseId THEN @UniversityLanguageCourseCode
         END,
    [DepartmentId] = CASE [Id]
               WHEN @UniversityItCourseId THEN @UniversityItDepartmentId
               WHEN @UniversityBusinessCourseId THEN @UniversityBusinessDepartmentId
               WHEN @UniversityLanguageCourseId THEN @UniversityLanguageDepartmentId
             END,
    [CreditHours] = 3,
    [IsActive] = 1,
    [IsDeleted] = 0,
    [DeletedAt] = NULL,
    [UpdatedAt] = @Now
  WHERE [Id] IN (@UniversityItCourseId, @UniversityBusinessCourseId, @UniversityLanguageCourseId);

  IF @UniversitySemesterId IS NOT NULL
  BEGIN
    INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @UniversityItOfferingId, @UniversityItCourseId, @UniversitySemesterId, @UniversitySeedFacultyUserId, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [course_offerings] WHERE [Id] = @UniversityItOfferingId);

    INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @UniversityBusinessOfferingId, @UniversityBusinessCourseId, @UniversitySemesterId, @UniversitySeedFacultyUserId, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [course_offerings] WHERE [Id] = @UniversityBusinessOfferingId);

    INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @UniversityLanguageOfferingId, @UniversityLanguageCourseId, @UniversitySemesterId, @UniversitySeedFacultyUserId, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [course_offerings] WHERE [Id] = @UniversityLanguageOfferingId);
  END;
END;

DECLARE @FallbackFacultyUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) u.[Id]
    FROM [users] u
    INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
    WHERE r.[Name] = N'Faculty'
      AND ISNULL(u.[InstitutionType], @UniversityInstitutionType) = @UniversityInstitutionType
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
    ORDER BY CASE
                 WHEN c.[Code] IN (@UniversityItCourseCode, @UniversityBusinessCourseCode, @UniversityLanguageCourseCode) THEN 0
                 ELSE 1
             END,
             co.[CreatedAt], co.[Id]
) depOffering
OUTER APPLY
(
    SELECT TOP (1) co.[Id] AS [CourseOfferingId], co.[FacultyUserId]
    FROM [course_offerings] co
    INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
    INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId]
    WHERE ISNULL(d.[InstitutionType], @UniversityInstitutionType) = @UniversityInstitutionType
      AND ISNULL(c.[IsDeleted], 0) = 0
      AND ISNULL(co.[IsDeleted], 0) = 0
      AND co.[FacultyUserId] IS NOT NULL
    ORDER BY CASE
                 WHEN c.[Code] IN (@UniversityItCourseCode, @UniversityBusinessCourseCode, @UniversityLanguageCourseCode) THEN 0
                 ELSE 1
             END,
             co.[CreatedAt], co.[Id]
) instOffering
WHERE ISNULL(su.[InstitutionType], @UniversityInstitutionType) = @UniversityInstitutionType
  AND ISNULL(su.[IsDeleted], 0) = 0
  AND COALESCE(depOffering.[CourseOfferingId], instOffering.[CourseOfferingId]) IS NOT NULL
  AND COALESCE(depOffering.[FacultyUserId], instOffering.[FacultyUserId], @FallbackFacultyUserId) IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM @TargetStudents)
BEGIN
  PRINT N'University dummy data note: prerequisites not satisfied (students, faculty, or course offerings missing). Run University Scripts/02-Seed-Core.sql and base seeds first.';
  ROLLBACK TRANSACTION;
  RETURN;
END;

DECLARE @SemesterCoverage TABLE ([SemesterNo] INT, [Marks] DECIMAL(8,2));
INSERT INTO @SemesterCoverage ([SemesterNo], [Marks]) VALUES
(1, 80.00),
(2, 82.00),
(3, 84.00),
(4, 85.00),
(5, 87.00),
(6, 88.00),
(7, 90.00),
(8, 92.00);

IF OBJECT_ID(N'[timetables]') IS NOT NULL
AND OBJECT_ID(N'[timetable_entries]') IS NOT NULL
AND OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND OBJECT_ID(N'[academic_programs]') IS NOT NULL
BEGIN
  DECLARE @UniversityTimetableId UNIQUEIDENTIFIER = CAST('93000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityDepartmentId UNIQUEIDENTIFIER = (
    SELECT TOP (1) sp.[DepartmentId]
    FROM @TargetStudents ts
    INNER JOIN [student_profiles] sp ON sp.[Id] = ts.[StudentProfileId]
    ORDER BY ts.[StudentRn]
  );
  DECLARE @UniversitySemesterId UNIQUEIDENTIFIER = (
    SELECT TOP (1) co.[SemesterId]
    FROM @TargetStudents ts
    INNER JOIN [course_offerings] co ON co.[Id] = ts.[CourseOfferingId]
    WHERE co.[SemesterId] IS NOT NULL
    ORDER BY ts.[StudentRn]
  );
  DECLARE @UniversityProgramId UNIQUEIDENTIFIER = (
    SELECT TOP (1) ap.[Id]
    FROM [academic_programs] ap
    WHERE ap.[DepartmentId] = @UniversityDepartmentId
      AND ISNULL(ap.[IsDeleted], 0) = 0
    ORDER BY ap.[CreatedAt], ap.[Id]
  );

  IF @UniversitySemesterId IS NULL
    SELECT TOP (1) @UniversitySemesterId = s.[Id] FROM [semesters] s WHERE ISNULL(s.[IsDeleted], 0) = 0 ORDER BY s.[CreatedAt], s.[Id];

  IF @UniversityDepartmentId IS NOT NULL AND @UniversitySemesterId IS NOT NULL AND @UniversityProgramId IS NOT NULL
  BEGIN
    INSERT INTO [timetables] ([Id], [DepartmentId], [AcademicProgramId], [SemesterId], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt], [EffectiveDate], [SemesterNumber])
    SELECT @UniversityTimetableId, @UniversityDepartmentId, @UniversityProgramId, @UniversitySemesterId, 1, @Now, @Now, NULL, 0, NULL, CAST(@Now AS date), 1
    WHERE NOT EXISTS (SELECT 1 FROM [timetables] t WHERE t.[Id] = @UniversityTimetableId);

    DECLARE @UniversityOfferingPool TABLE
    (
      [PoolRank] INT NOT NULL,
      [CourseId] UNIQUEIDENTIFIER NOT NULL,
      [FacultyUserId] UNIQUEIDENTIFIER NULL
    );

    INSERT INTO @UniversityOfferingPool ([PoolRank], [CourseId], [FacultyUserId])
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

    DECLARE @UniversityOfferingPoolCount INT = (SELECT COUNT(1) FROM @UniversityOfferingPool);

    IF @UniversityOfferingPoolCount > 0
    BEGIN
      ;WITH SemesterEntries AS
      (
        SELECT
          sc.[SemesterNo],
          ((sc.[SemesterNo] - 1) % @UniversityOfferingPoolCount) + 1 AS [PoolRank]
        FROM @SemesterCoverage sc
      )
      INSERT INTO [timetable_entries] ([Id], [TimetableId], [DayOfWeek], [StartTime], [EndTime], [SubjectName], [RoomNumber], [FacultyName], [RoomId], [CreatedAt], [UpdatedAt], [BuildingId], [CourseId], [FacultyUserId])
      SELECT
        NEWID(),
        @UniversityTimetableId,
        ((se.[SemesterNo] - 1) % 5) + 1,
        CAST('09:00:00' AS time),
        CAST('10:30:00' AS time),
        CONCAT(N'University Semester ', CAST(se.[SemesterNo] AS NVARCHAR(10)), N' - Core'),
        N'U-101',
        COALESCE(fu.[Username], N'university.faculty'),
        NULL,
        @Now,
        NULL,
        NULL,
        op.[CourseId],
        op.[FacultyUserId]
      FROM SemesterEntries se
      INNER JOIN @UniversityOfferingPool op ON op.[PoolRank] = se.[PoolRank]
      LEFT JOIN [users] fu ON fu.[Id] = op.[FacultyUserId]
      WHERE NOT EXISTS
      (
        SELECT 1
        FROM [timetable_entries] te
        WHERE te.[TimetableId] = @UniversityTimetableId
          AND te.[SubjectName] = CONCAT(N'University Semester ', CAST(se.[SemesterNo] AS NVARCHAR(10)), N' - Core')
      );
    END;
  END;
END;

INSERT INTO [student_report_cards] ([Id], [StudentProfileId], [InstitutionType], [PeriodLabel], [PayloadJson], [GeneratedByUserId], [GeneratedAt], [CreatedAt], [UpdatedAt])
SELECT NEWID(),
     ts.[StudentProfileId],
       @UniversityInstitutionType,
       CONCAT(N'Semester ', CAST(sc.[SemesterNo] AS NVARCHAR(10))),
     CONCAT(N'{"domain":"university","semester":', CAST(sc.[SemesterNo] AS NVARCHAR(10)), N',"score":', CAST(sc.[Marks] + ((ts.[StudentRn] - 1) % 5) AS NVARCHAR(20)), N',"studentIndex":', CAST(ts.[StudentRn] AS NVARCHAR(20)), N'}'),
     ts.[FacultyUserId],
     DATEADD(day, -(sc.[SemesterNo] + ts.[StudentRn]), @Now),
       @Now,
       NULL
FROM @SemesterCoverage sc
CROSS JOIN @TargetStudents ts
WHERE NOT EXISTS
(
    SELECT 1
    FROM [student_report_cards] src
  WHERE src.[StudentProfileId] = ts.[StudentProfileId]
      AND src.[InstitutionType] = @UniversityInstitutionType
      AND src.[PeriodLabel] = CONCAT(N'Semester ', CAST(sc.[SemesterNo] AS NVARCHAR(10)))
);

INSERT INTO [results] ([Id], [StudentProfileId], [CourseOfferingId], [ResultType], [MarksObtained], [MaxMarks], [IsPublished], [PublishedAt], [PublishedByUserId], [CreatedAt], [UpdatedAt])
SELECT NEWID(),
       ts.[StudentProfileId],
       ts.[CourseOfferingId],
       CONCAT(N'Semester ', CAST(sc.[SemesterNo] AS NVARCHAR(10))),
       sc.[Marks] + ((ts.[StudentRn] - 1) % 5),
       100.00,
       1,
       DATEADD(day, -(sc.[SemesterNo] + ts.[StudentRn]), @Now),
       ts.[FacultyUserId],
       @Now,
       NULL
FROM @SemesterCoverage sc
CROSS JOIN @TargetStudents ts
WHERE NOT EXISTS
(
    SELECT 1
    FROM [results] r
    WHERE r.[StudentProfileId] = ts.[StudentProfileId]
      AND r.[CourseOfferingId] = ts.[CourseOfferingId]
      AND r.[ResultType] = CONCAT(N'Semester ', CAST(sc.[SemesterNo] AS NVARCHAR(10)))
);

/*
  Mark university completion after Semester 8 where schema supports lifecycle columns.
*/
IF COL_LENGTH('student_profiles', 'CurrentSemesterNumber') IS NOT NULL
BEGIN
    UPDATE sp
    SET sp.[CurrentSemesterNumber] = CASE WHEN ISNULL(sp.[CurrentSemesterNumber], 0) < 8 THEN 8 ELSE sp.[CurrentSemesterNumber] END,
        sp.[UpdatedAt] = @Now
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE ISNULL(u.[InstitutionType], @UniversityInstitutionType) = @UniversityInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
      AND EXISTS
      (
          SELECT 1
          FROM [student_report_cards] src
          WHERE src.[StudentProfileId] = sp.[Id]
            AND src.[InstitutionType] = @UniversityInstitutionType
            AND src.[PeriodLabel] = N'Semester 8'
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
          WHERE ISNULL(u.[InstitutionType], @UniversityInstitutionType) = @UniversityInstitutionType
            AND ISNULL(u.[IsDeleted], 0) = 0
            AND EXISTS
            (
                SELECT 1
                FROM [student_report_cards] src
                WHERE src.[StudentProfileId] = sp.[Id]
                  AND src.[InstitutionType] = @UniversityInstitutionType
                  AND src.[PeriodLabel] = N''Semester 8''
            );',
        N'@Now DATETIME2, @UniversityInstitutionType INT',
        @Now = @Now,
        @UniversityInstitutionType = @UniversityInstitutionType;
END;

PRINT N'University lifecycle completion applied for students with Semester 8 coverage (where supported by schema).';

    IF OBJECT_ID(N'[enrollments]') IS NOT NULL
    BEGIN
      INSERT INTO [enrollments] ([Id], [StudentProfileId], [CourseOfferingId], [EnrolledAt], [DroppedAt], [Status], [CreatedAt], [UpdatedAt])
      SELECT NEWID(),
           ts.[StudentProfileId],
           ts.[CourseOfferingId],
           DATEADD(day, -(10 + ts.[StudentRn]), @Now),
           NULL,
           N'Enrolled',
           @Now,
           NULL
      FROM @TargetStudents ts
      WHERE NOT EXISTS
      (
        SELECT 1
        FROM [enrollments] e
        WHERE e.[StudentProfileId] = ts.[StudentProfileId]
          AND e.[CourseOfferingId] = ts.[CourseOfferingId]
      );
    END;

    IF OBJECT_ID(N'[attendance_records]') IS NOT NULL
    BEGIN
      INSERT INTO [attendance_records] ([Id], [StudentProfileId], [CourseOfferingId], [Date], [Status], [MarkedByUserId], [Remarks], [CreatedAt], [UpdatedAt])
      SELECT NEWID(),
           ts.[StudentProfileId],
           ts.[CourseOfferingId],
           DATEADD(day, -(30 + (sc.[SemesterNo] * 7) + ts.[StudentRn]), CAST(@Now AS date)),
           CASE WHEN ((ts.[StudentRn] + sc.[SemesterNo]) % 4) = 0 THEN N'Absent' ELSE N'Present' END,
           ts.[FacultyUserId],
           CONCAT(N'University semester ', CAST(sc.[SemesterNo] AS NVARCHAR(10)), N' attendance seed'),
           @Now,
           NULL
      FROM @SemesterCoverage sc
      CROSS JOIN @TargetStudents ts
      WHERE NOT EXISTS
      (
        SELECT 1
        FROM [attendance_records] ar
        WHERE ar.[StudentProfileId] = ts.[StudentProfileId]
          AND ar.[CourseOfferingId] = ts.[CourseOfferingId]
          AND ar.[Date] = DATEADD(day, -(30 + (sc.[SemesterNo] * 7) + ts.[StudentRn]), CAST(@Now AS date))
      );
    END;

    IF OBJECT_ID(N'[fyp_projects]') IS NOT NULL
    BEGIN
      INSERT INTO [fyp_projects] ([Id], [StudentProfileId], [DepartmentId], [Title], [Description], [Status], [SupervisorUserId], [CoordinatorRemarks], [CreatedAt], [UpdatedAt])
      SELECT NEWID(),
           ts.[StudentProfileId],
           sp.[DepartmentId],
           CONCAT(N'Capstone Project - Student ', CAST(ts.[StudentRn] AS NVARCHAR(20))),
           N'University semester 8 capstone and FYP coverage seeded for deterministic demo and validation.',
           N'InProgress',
           ts.[FacultyUserId],
           N'Seeded by University Scripts/03-FullDummyData.sql',
           @Now,
           NULL
      FROM @TargetStudents ts
      INNER JOIN [student_profiles] sp ON sp.[Id] = ts.[StudentProfileId]
      WHERE NOT EXISTS
      (
        SELECT 1
        FROM [fyp_projects] fp
        WHERE fp.[StudentProfileId] = ts.[StudentProfileId]
      );
    END;

COMMIT TRANSACTION;
  PRINT N'Phase 4 script sync: university semester-period write scope remains unchanged and backward compatible.';
  PRINT N'University dummy data seeded successfully (all university students, Semester 1 to Semester 8 with completion, results, attendance, enrollment, and FYP data).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

  PRINT CONCAT(N'University dummy data warning: ', ERROR_MESSAGE());
END CATCH;
GO
