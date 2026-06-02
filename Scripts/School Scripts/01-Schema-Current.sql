SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*
  School schema bootstrap script (pure T-SQL).
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
  PRINT N'School schema note: base schema tables are missing.';
  PRINT N'Run shared schema script once: Scripts/01-Schema-Current.sql (prefer SQLCMD mode), then rerun this School pack.';
  RETURN;
END;

IF OBJECT_ID(N'[discussion_threads]') IS NOT NULL
BEGIN
  IF COL_LENGTH('discussion_threads', 'IsSolved') IS NULL
    ALTER TABLE [discussion_threads] ADD [IsSolved] BIT NOT NULL CONSTRAINT [DF_school_discussion_threads_IsSolved] DEFAULT ((0));

  IF COL_LENGTH('discussion_threads', 'IsVisibleToAll') IS NULL
    ALTER TABLE [discussion_threads] ADD [IsVisibleToAll] BIT NOT NULL CONSTRAINT [DF_school_discussion_threads_IsVisibleToAll] DEFAULT ((0));

  IF COL_LENGTH('discussion_threads', 'IssueSubType') IS NULL
    ALTER TABLE [discussion_threads] ADD [IssueSubType] NVARCHAR(100) NULL;

  IF COL_LENGTH('discussion_threads', 'ResolvedAt') IS NULL
    ALTER TABLE [discussion_threads] ADD [ResolvedAt] DATETIME2 NULL;

  IF COL_LENGTH('discussion_threads', 'ResolvedBy') IS NULL
    ALTER TABLE [discussion_threads] ADD [ResolvedBy] UNIQUEIDENTIFIER NULL;

  IF COL_LENGTH('discussion_threads', 'ThreadType') IS NULL
    ALTER TABLE [discussion_threads] ADD [ThreadType] NVARCHAR(30) NOT NULL CONSTRAINT [DF_school_discussion_threads_ThreadType] DEFAULT (N'Issue');

  IF COL_LENGTH('discussion_threads', 'TicketNumber') IS NULL
    ALTER TABLE [discussion_threads] ADD [TicketNumber] NVARCHAR(40) NOT NULL CONSTRAINT [DF_school_discussion_threads_TicketNumber] DEFAULT (N'');
END;

PRINT N'School schema wrapper validation passed.';
GO
