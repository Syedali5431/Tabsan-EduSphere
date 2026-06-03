SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
  PRINT N'College dummy data note: database [Tabsan-EduSphere] does not exist. Run College Scripts/01-Schema-Current.sql first.';
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

IF DB_NAME() <> N'Tabsan-EduSphere'
BEGIN
  PRINT N'College dummy data note: failed to switch context to [Tabsan-EduSphere].';
    RETURN;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  College-specific dummy data script.

  PREREQUISITES:
  1. 01-Schema-Current.sql
  2. 02-Seed-Core.sql

  PURPOSE:
  - Seeds deterministic college report-card coverage for classes (Class 11 and Class 12).
  - Seeds college results/marks rows for those classes.
*/

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'[users]') IS NULL
OR OBJECT_ID(N'[roles]') IS NULL
OR OBJECT_ID(N'[student_profiles]') IS NULL
OR OBJECT_ID(N'[student_report_cards]') IS NULL
OR OBJECT_ID(N'[results]') IS NULL
OR OBJECT_ID(N'[course_offerings]') IS NULL
OR OBJECT_ID(N'[courses]') IS NULL
OR OBJECT_ID(N'[departments]') IS NULL
BEGIN
  PRINT N'College dummy data note: required tables are missing. Run schema/core seed scripts first.';
  ROLLBACK TRANSACTION;
  RETURN;
END;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @CollegeInstitutionType INT = 1;
DECLARE @CollegeCanonicalCourseCode NVARCHAR(50) = N'COL-CORE-11TO12';

IF OBJECT_ID(N'[departments]') IS NOT NULL
AND OBJECT_ID(N'[academic_programs]') IS NOT NULL
AND OBJECT_ID(N'[courses]') IS NOT NULL
AND OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND OBJECT_ID(N'[semesters]') IS NOT NULL
BEGIN
  DECLARE @CollegeCanonicalDepartmentId UNIQUEIDENTIFIER = CAST('12222222-2222-2222-2222-222222222223' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeCanonicalProgramId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222324' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeCanonicalCourseId UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444434' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeCanonicalOfferingId UNIQUEIDENTIFIER = CAST('75555555-5555-5555-5555-555555555601' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeSeedFacultyUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) u.[Id]
    FROM [users] u
    INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
    WHERE r.[Name] = N'Faculty'
      AND ISNULL(u.[InstitutionType], @CollegeInstitutionType) = @CollegeInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
    ORDER BY u.[CreatedAt], u.[Id]
  );
  DECLARE @CollegeSemesterId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [Id]
    FROM [semesters]
    WHERE ISNULL([IsDeleted], 0) = 0
    ORDER BY [CreatedAt], [Id]
  );

  INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @CollegeCanonicalDepartmentId, N'College Core Department', N'COLCORE', @CollegeInstitutionType, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @CollegeCanonicalDepartmentId);

  INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @CollegeCanonicalProgramId, N'Intermediate Core (Class 11-12)', N'CLS11_12', @CollegeCanonicalDepartmentId, 2, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @CollegeCanonicalProgramId);

  INSERT INTO [courses] ([Id], [Title], [Code], [CreditHours], [DepartmentId], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
  SELECT @CollegeCanonicalCourseId, N'College Core Studies (Class 11-12)', @CollegeCanonicalCourseCode, 3, @CollegeCanonicalDepartmentId, 1, @Now, NULL, 0, NULL
  WHERE NOT EXISTS (SELECT 1 FROM [courses] WHERE [Id] = @CollegeCanonicalCourseId);

  UPDATE [courses]
  SET [Title] = N'College Core Studies (Class 11-12)',
    [Code] = @CollegeCanonicalCourseCode,
    [DepartmentId] = @CollegeCanonicalDepartmentId,
    [CreditHours] = 3,
    [IsActive] = 1,
    [IsDeleted] = 0,
    [DeletedAt] = NULL,
    [UpdatedAt] = @Now
  WHERE [Id] = @CollegeCanonicalCourseId;

  IF @CollegeSemesterId IS NOT NULL
  BEGIN
    INSERT INTO [course_offerings] ([Id], [CourseId], [SemesterId], [FacultyUserId], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @CollegeCanonicalOfferingId, @CollegeCanonicalCourseId, @CollegeSemesterId, @CollegeSeedFacultyUserId, @Now, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [course_offerings] WHERE [Id] = @CollegeCanonicalOfferingId);
  END;
END;

DECLARE @FallbackFacultyUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) u.[Id]
    FROM [users] u
    INNER JOIN [roles] r ON r.[Id] = u.[RoleId]
    WHERE r.[Name] = N'Faculty'
      AND ISNULL(u.[InstitutionType], @CollegeInstitutionType) = @CollegeInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
    ORDER BY u.[CreatedAt], u.[Id]
);

  IF @FallbackFacultyUserId IS NULL
  BEGIN
    SELECT TOP (1) @FallbackFacultyUserId = u.[Id]
    FROM [users] u
    WHERE ISNULL(u.[IsDeleted], 0) = 0
    ORDER BY u.[CreatedAt], u.[Id];
  END;

DECLARE @TargetStudents TABLE
(
    [StudentProfileId] UNIQUEIDENTIFIER NOT NULL,
    [CourseOfferingId] UNIQUEIDENTIFIER NULL,
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
    ORDER BY CASE WHEN c.[Code] = @CollegeCanonicalCourseCode THEN 0 ELSE 1 END,
             co.[CreatedAt], co.[Id]
) depOffering
OUTER APPLY
(
    SELECT TOP (1) co.[Id] AS [CourseOfferingId], co.[FacultyUserId]
    FROM [course_offerings] co
    INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
    INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId]
    WHERE ISNULL(d.[InstitutionType], @CollegeInstitutionType) = @CollegeInstitutionType
      AND ISNULL(c.[IsDeleted], 0) = 0
      AND ISNULL(co.[IsDeleted], 0) = 0
      AND co.[FacultyUserId] IS NOT NULL
    ORDER BY CASE WHEN c.[Code] = @CollegeCanonicalCourseCode THEN 0 ELSE 1 END,
             co.[CreatedAt], co.[Id]
) instOffering
WHERE ISNULL(su.[InstitutionType], @CollegeInstitutionType) = @CollegeInstitutionType
  AND ISNULL(su.[IsDeleted], 0) = 0
  AND COALESCE(depOffering.[FacultyUserId], instOffering.[FacultyUserId], @FallbackFacultyUserId) IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM @TargetStudents)
BEGIN
  PRINT N'College dummy data note: no eligible college students/faculty were found. Nothing to seed.';
  ROLLBACK TRANSACTION;
  RETURN;
END;

DECLARE @ClassCoverage TABLE ([ClassNo] INT, [Marks] DECIMAL(8,2));
INSERT INTO @ClassCoverage ([ClassNo], [Marks]) VALUES
(11, 79.00),
(12, 81.00);

IF OBJECT_ID(N'[timetables]') IS NOT NULL
AND OBJECT_ID(N'[timetable_entries]') IS NOT NULL
AND OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND OBJECT_ID(N'[academic_programs]') IS NOT NULL
BEGIN
  DECLARE @CollegeTimetableId UNIQUEIDENTIFIER = CAST('92000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeDepartmentId UNIQUEIDENTIFIER = (
    SELECT TOP (1) sp.[DepartmentId]
    FROM @TargetStudents ts
    INNER JOIN [student_profiles] sp ON sp.[Id] = ts.[StudentProfileId]
    ORDER BY ts.[StudentRn]
  );
  DECLARE @CollegeSemesterId UNIQUEIDENTIFIER = (
    SELECT TOP (1) co.[SemesterId]
    FROM @TargetStudents ts
    INNER JOIN [course_offerings] co ON co.[Id] = ts.[CourseOfferingId]
    WHERE co.[SemesterId] IS NOT NULL
    ORDER BY ts.[StudentRn]
  );
  DECLARE @CollegeProgramId UNIQUEIDENTIFIER = (
    SELECT TOP (1) ap.[Id]
    FROM [academic_programs] ap
    WHERE ap.[DepartmentId] = @CollegeDepartmentId
      AND ISNULL(ap.[IsDeleted], 0) = 0
    ORDER BY ap.[CreatedAt], ap.[Id]
  );

  IF @CollegeSemesterId IS NULL
    SELECT TOP (1) @CollegeSemesterId = s.[Id] FROM [semesters] s WHERE ISNULL(s.[IsDeleted], 0) = 0 ORDER BY s.[CreatedAt], s.[Id];

  IF @CollegeDepartmentId IS NOT NULL AND @CollegeSemesterId IS NOT NULL AND @CollegeProgramId IS NOT NULL
  BEGIN
    INSERT INTO [timetables] ([Id], [DepartmentId], [AcademicProgramId], [SemesterId], [IsPublished], [PublishedAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt], [EffectiveDate], [SemesterNumber])
    SELECT @CollegeTimetableId, @CollegeDepartmentId, @CollegeProgramId, @CollegeSemesterId, 1, @Now, @Now, NULL, 0, NULL, CAST(@Now AS date), 11
    WHERE NOT EXISTS (SELECT 1 FROM [timetables] t WHERE t.[Id] = @CollegeTimetableId);

    DECLARE @CollegeOfferingPool TABLE
    (
      [PoolRank] INT NOT NULL,
      [CourseId] UNIQUEIDENTIFIER NOT NULL,
      [FacultyUserId] UNIQUEIDENTIFIER NULL
    );

    INSERT INTO @CollegeOfferingPool ([PoolRank], [CourseId], [FacultyUserId])
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

    DECLARE @CollegeOfferingPoolCount INT = (SELECT COUNT(1) FROM @CollegeOfferingPool);

    IF @CollegeOfferingPoolCount > 0
    BEGIN
      ;WITH ClassEntries AS
      (
        SELECT
          cc.[ClassNo],
          ((cc.[ClassNo] - 11) % @CollegeOfferingPoolCount) + 1 AS [PoolRank]
        FROM @ClassCoverage cc
      )
      INSERT INTO [timetable_entries] ([Id], [TimetableId], [DayOfWeek], [StartTime], [EndTime], [SubjectName], [RoomNumber], [FacultyName], [RoomId], [CreatedAt], [UpdatedAt], [BuildingId], [CourseId], [FacultyUserId])
      SELECT
        NEWID(),
        @CollegeTimetableId,
        ((ce.[ClassNo] - 11) % 5) + 1,
        CAST('10:45:00' AS time),
        CAST('12:15:00' AS time),
        CONCAT(N'Class ', CAST(ce.[ClassNo] AS NVARCHAR(10)), N' - College Core'),
        N'C-101',
        COALESCE(fu.[Username], N'college.faculty'),
        NULL,
        @Now,
        NULL,
        NULL,
        op.[CourseId],
        op.[FacultyUserId]
      FROM ClassEntries ce
      INNER JOIN @CollegeOfferingPool op ON op.[PoolRank] = ce.[PoolRank]
      LEFT JOIN [users] fu ON fu.[Id] = op.[FacultyUserId]
      WHERE NOT EXISTS
      (
        SELECT 1
        FROM [timetable_entries] te
        WHERE te.[TimetableId] = @CollegeTimetableId
          AND te.[SubjectName] = CONCAT(N'Class ', CAST(ce.[ClassNo] AS NVARCHAR(10)), N' - College Core')
      );
    END;
  END;
END;

INSERT INTO [student_report_cards] ([Id], [StudentProfileId], [InstitutionType], [PeriodLabel], [PayloadJson], [GeneratedByUserId], [GeneratedAt], [CreatedAt], [UpdatedAt])
SELECT NEWID(),
     ts.[StudentProfileId],
       @CollegeInstitutionType,
       CONCAT(N'Class ', CAST(cc.[ClassNo] AS NVARCHAR(10))),
     CONCAT(N'{"domain":"college","class":', CAST(cc.[ClassNo] AS NVARCHAR(10)), N',"score":', CAST(cc.[Marks] + ((ts.[StudentRn] - 1) % 5) AS NVARCHAR(20)), N',"studentIndex":', CAST(ts.[StudentRn] AS NVARCHAR(20)), N'}'),
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
      AND src.[InstitutionType] = @CollegeInstitutionType
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
WHERE ts.[CourseOfferingId] IS NOT NULL
  AND NOT EXISTS
(
    SELECT 1
    FROM [results] r
    WHERE r.[StudentProfileId] = ts.[StudentProfileId]
      AND r.[CourseOfferingId] = ts.[CourseOfferingId]
      AND r.[ResultType] = CONCAT(N'Class ', CAST(cc.[ClassNo] AS NVARCHAR(10)))
);

/*
  Mark college completion after Class 12 where schema supports lifecycle columns.
*/
IF COL_LENGTH('student_profiles', 'CurrentSemesterNumber') IS NOT NULL
BEGIN
    UPDATE sp
    SET sp.[CurrentSemesterNumber] = CASE WHEN ISNULL(sp.[CurrentSemesterNumber], 0) < 12 THEN 12 ELSE sp.[CurrentSemesterNumber] END,
        sp.[UpdatedAt] = @Now
    FROM [student_profiles] sp
    INNER JOIN [users] u ON u.[Id] = sp.[UserId]
    WHERE ISNULL(u.[InstitutionType], @CollegeInstitutionType) = @CollegeInstitutionType
      AND ISNULL(u.[IsDeleted], 0) = 0
      AND EXISTS
      (
          SELECT 1
          FROM [student_report_cards] src
          WHERE src.[StudentProfileId] = sp.[Id]
            AND src.[InstitutionType] = @CollegeInstitutionType
            AND src.[PeriodLabel] = N'Class 12'
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
          WHERE ISNULL(u.[InstitutionType], @CollegeInstitutionType) = @CollegeInstitutionType
            AND ISNULL(u.[IsDeleted], 0) = 0
            AND EXISTS
            (
                SELECT 1
                FROM [student_report_cards] src
                WHERE src.[StudentProfileId] = sp.[Id]
                  AND src.[InstitutionType] = @CollegeInstitutionType
                  AND src.[PeriodLabel] = N''Class 12''
            );',
        N'@Now DATETIME2, @CollegeInstitutionType INT',
        @Now = @Now,
        @CollegeInstitutionType = @CollegeInstitutionType;
END;

PRINT N'College lifecycle completion applied for students with Class 12 coverage (where supported by schema).';

IF NOT EXISTS (SELECT 1 FROM @TargetStudents WHERE [CourseOfferingId] IS NOT NULL)
BEGIN
    PRINT N'College note: no course offerings resolved for college students, so Class 11/12 results were skipped; report cards were still seeded.';
END;

COMMIT TRANSACTION;
PRINT N'Phase 4 script sync: college class-period write scope is web-driven; SQL seed remains class coverage aligned.';
PRINT N'College dummy data seeded successfully (all college students, Class 11 and Class 12 with completion after Class 12).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
  PRINT CONCAT(N'College dummy data warning: ', ERROR_MESSAGE());
END CATCH;
GO
