SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*
  School domain seed script.
  1) Validates baseline schema availability.
  2) Ensures deterministic baseline school entities for Class 1-10 dummy data.
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
  PRINT N'School core seed note: database [Tabsan-EduSphere] does not exist. Run School Scripts/01-Schema-Current.sql first.';
  RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
  BEGIN TRANSACTION;

  IF OBJECT_ID(N'[departments]') IS NULL
  OR OBJECT_ID(N'[academic_programs]') IS NULL
  OR OBJECT_ID(N'[users]') IS NULL
  OR OBJECT_ID(N'[roles]') IS NULL
  OR OBJECT_ID(N'[student_profiles]') IS NULL
  BEGIN
    PRINT N'School core seed note: base schema tables are missing. Run Scripts/01-Schema-Current.sql before this script.';
    ROLLBACK TRANSACTION;
    RETURN;
  END;

  DECLARE @Now DATETIME2 = SYSUTCDATETIME();
  DECLARE @SchoolInstitutionType INT = 0;

  DECLARE @SchoolDepartmentId UNIQUEIDENTIFIER = CAST('12S01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolProgramId UNIQUEIDENTIFIER = CAST('22S01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolFacultyUserId UNIQUEIDENTIFIER = CAST('66S01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolStudentUserId UNIQUEIDENTIFIER = CAST('66S01000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
  DECLARE @SchoolStudentProfileId UNIQUEIDENTIFIER = CAST('99S01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);

  DECLARE @StudentRoleId INT = (SELECT TOP (1) [Id] FROM [roles] WHERE [Name] = N'Student');
  DECLARE @FacultyRoleId INT = (SELECT TOP (1) [Id] FROM [roles] WHERE [Name] = N'Faculty');

  IF @StudentRoleId IS NULL OR @FacultyRoleId IS NULL
  BEGIN
    PRINT N'School core seed note: required roles [Student]/[Faculty] are missing. Seed core roles first (Scripts/02-Seed-Core.sql).';
    ROLLBACK TRANSACTION;
    RETURN;
  END;

  DECLARE @PwdHash NVARCHAR(512) =
  (
    SELECT TOP (1) [PasswordHash]
    FROM [users]
    WHERE [PasswordHash] IS NOT NULL
    ORDER BY [CreatedAt], [Id]
  );

  IF @PwdHash IS NULL
  BEGIN
    SET @PwdHash = N'EduSphere147';
  END;

  IF NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @SchoolDepartmentId)
  BEGIN
    INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@SchoolDepartmentId, N'School Academic Department', N'SCH-ACA', @SchoolInstitutionType, 1, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @SchoolProgramId)
  BEGIN
    INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@SchoolProgramId, N'School Foundation (Class 1-10)', N'SCH-C1-C10', @SchoolDepartmentId, 10, 1, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id] = @SchoolFacultyUserId)
  BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@SchoolFacultyUserId, N'school.faculty.c1c10', N'school.faculty.c1c10@demo.local', @PwdHash, @FacultyRoleId, @SchoolDepartmentId, @SchoolInstitutionType, 1, NULL, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id] = @SchoolStudentUserId)
  BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@SchoolStudentUserId, N'school.student.c1c10', N'school.student.c1c10@demo.local', @PwdHash, @StudentRoleId, @SchoolDepartmentId, @SchoolInstitutionType, 1, NULL, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [Id] = @SchoolStudentProfileId)
  BEGIN
    INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@SchoolStudentProfileId, @SchoolStudentUserId, N'SCH-C1C10-001', @SchoolProgramId, @SchoolDepartmentId, DATEADD(year, -3, @Now), 3.20, 1, @Now, NULL, 0, NULL);
  END;

  COMMIT TRANSACTION;
  PRINT N'School core seed script completed with deterministic Class 1-10 baseline users/profiles.';
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
  PRINT CONCAT(N'School core seed warning: ', ERROR_MESSAGE());
END CATCH;
GO
