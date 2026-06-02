/*
  College schema bootstrap script (pure T-SQL).
  SQLCMD-only directives were removed so this script can run in standard SSMS/Azure Data Studio mode.
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
  PRINT N'Database [Tabsan-EduSphere] does not exist. Creating it now...';
  CREATE DATABASE [Tabsan-EduSphere];
END;

GO

USE [Tabsan-EduSphere];
GO

IF OBJECT_ID(N'[departments]') IS NULL
OR OBJECT_ID(N'[academic_programs]') IS NULL
OR OBJECT_ID(N'[users]') IS NULL
OR OBJECT_ID(N'[student_profiles]') IS NULL
BEGIN
  PRINT N'College schema note: base schema tables are missing.';
  PRINT N'Run shared schema script once: Scripts/01-Schema-Current.sql (prefer SQLCMD mode), then rerun this College pack.';
    RETURN;
END;

/*
  College compatibility self-heal:
  Ensure current API-required scope columns exist on courses and course_offerings.
*/
IF OBJECT_ID(N'[courses]') IS NOT NULL
AND OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND OBJECT_ID(N'[departments]') IS NOT NULL
BEGIN
  IF COL_LENGTH('courses', 'CampusId') IS NULL
    ALTER TABLE [courses] ADD [CampusId] UNIQUEIDENTIFIER NULL;

  IF COL_LENGTH('courses', 'InstitutionType') IS NULL
    ALTER TABLE [courses] ADD [InstitutionType] INT NOT NULL CONSTRAINT [DF_college_courses_InstitutionType] DEFAULT ((3));

  IF COL_LENGTH('courses', 'TenantId') IS NULL
    ALTER TABLE [courses] ADD [TenantId] UNIQUEIDENTIFIER NULL;

  IF COL_LENGTH('course_offerings', 'CampusId') IS NULL
    ALTER TABLE [course_offerings] ADD [CampusId] UNIQUEIDENTIFIER NULL;

  IF COL_LENGTH('course_offerings', 'InstitutionType') IS NULL
    ALTER TABLE [course_offerings] ADD [InstitutionType] INT NOT NULL CONSTRAINT [DF_college_course_offerings_InstitutionType] DEFAULT ((3));

  IF COL_LENGTH('course_offerings', 'TenantId') IS NULL
    ALTER TABLE [course_offerings] ADD [TenantId] UNIQUEIDENTIFIER NULL;

  IF COL_LENGTH('departments', 'TenantId') IS NOT NULL
  AND COL_LENGTH('departments', 'CampusId') IS NOT NULL
  AND COL_LENGTH('departments', 'InstitutionType') IS NOT NULL
  BEGIN
    EXEC(N'
      UPDATE c
      SET
        c.[TenantId] = d.[TenantId],
        c.[CampusId] = d.[CampusId],
        c.[InstitutionType] = CAST(d.[InstitutionType] AS int)
      FROM [courses] c
      INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId]
      WHERE c.[TenantId] IS NULL OR c.[CampusId] IS NULL OR c.[InstitutionType] = 3;
    ');
  END;

  EXEC(N'
    UPDATE o
    SET
      o.[TenantId] = c.[TenantId],
      o.[CampusId] = c.[CampusId],
      o.[InstitutionType] = c.[InstitutionType]
    FROM [course_offerings] o
    INNER JOIN [courses] c ON c.[Id] = o.[CourseId]
    WHERE o.[TenantId] IS NULL OR o.[CampusId] IS NULL OR o.[InstitutionType] = 3;
  ');
END;

  IF OBJECT_ID(N'[discussion_threads]') IS NOT NULL
  BEGIN
    IF COL_LENGTH('discussion_threads', 'IsSolved') IS NULL
      ALTER TABLE [discussion_threads] ADD [IsSolved] BIT NOT NULL CONSTRAINT [DF_college_discussion_threads_IsSolved] DEFAULT ((0));

    IF COL_LENGTH('discussion_threads', 'IsVisibleToAll') IS NULL
      ALTER TABLE [discussion_threads] ADD [IsVisibleToAll] BIT NOT NULL CONSTRAINT [DF_college_discussion_threads_IsVisibleToAll] DEFAULT ((0));

    IF COL_LENGTH('discussion_threads', 'IssueSubType') IS NULL
      ALTER TABLE [discussion_threads] ADD [IssueSubType] NVARCHAR(100) NULL;

    IF COL_LENGTH('discussion_threads', 'ResolvedAt') IS NULL
      ALTER TABLE [discussion_threads] ADD [ResolvedAt] DATETIME2 NULL;

    IF COL_LENGTH('discussion_threads', 'ResolvedBy') IS NULL
      ALTER TABLE [discussion_threads] ADD [ResolvedBy] UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH('discussion_threads', 'ThreadType') IS NULL
      ALTER TABLE [discussion_threads] ADD [ThreadType] NVARCHAR(30) NOT NULL CONSTRAINT [DF_college_discussion_threads_ThreadType] DEFAULT (N'Issue');

    IF COL_LENGTH('discussion_threads', 'TicketNumber') IS NULL
      ALTER TABLE [discussion_threads] ADD [TicketNumber] NVARCHAR(40) NOT NULL CONSTRAINT [DF_college_discussion_threads_TicketNumber] DEFAULT (N'');
  END;

PRINT N'College schema wrapper validation passed.';
GO
