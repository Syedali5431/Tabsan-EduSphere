SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*
  College domain seed wrapper.
  1) Validates baseline schema availability.
  2) Ensures deterministic baseline college entities for Class 11-12 dummy data.
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    PRINT N'College core seed note: database [Tabsan-EduSphere] does not exist. Run College Scripts/01-Schema-Current.sql first.';
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
    PRINT N'College core seed note: base schema tables are missing. Run Scripts/01-Schema-Current.sql before this script.';
    ROLLBACK TRANSACTION;
    RETURN;
  END;

  DECLARE @Now DATETIME2 = SYSUTCDATETIME();
  DECLARE @CollegeInstitutionType INT = 1;

  DECLARE @CollegeDepartmentId UNIQUEIDENTIFIER = CAST('12C01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeProgramId UNIQUEIDENTIFIER = CAST('22C01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeFacultyUserId UNIQUEIDENTIFIER = CAST('66C01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeStudentUserId UNIQUEIDENTIFIER = CAST('66C01000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
  DECLARE @CollegeStudentProfileId UNIQUEIDENTIFIER = CAST('99C01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);

  DECLARE @StudentRoleId INT = (SELECT TOP (1) [Id] FROM [roles] WHERE [Name] = N'Student');
  DECLARE @FacultyRoleId INT = (SELECT TOP (1) [Id] FROM [roles] WHERE [Name] = N'Faculty');

  IF @StudentRoleId IS NULL OR @FacultyRoleId IS NULL
  BEGIN
    PRINT N'College core seed note: required roles [Student]/[Faculty] are missing. Seed core roles first (Scripts/02-Seed-Core.sql).';
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

  IF NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @CollegeDepartmentId)
  BEGIN
    INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CollegeDepartmentId, N'College Intermediate Department', N'COL-INT', @CollegeInstitutionType, 1, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @CollegeProgramId)
  BEGIN
    INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CollegeProgramId, N'College Intermediate (Class 11-12)', N'COL-INT-11-12', @CollegeDepartmentId, 12, 1, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id] = @CollegeFacultyUserId)
  BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CollegeFacultyUserId, N'college.faculty.c11c12', N'college.faculty.c11c12@demo.local', @PwdHash, @FacultyRoleId, @CollegeDepartmentId, @CollegeInstitutionType, 1, NULL, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id] = @CollegeStudentUserId)
  BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CollegeStudentUserId, N'college.student.c11c12', N'college.student.c11c12@demo.local', @PwdHash, @StudentRoleId, @CollegeDepartmentId, @CollegeInstitutionType, 1, NULL, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [Id] = @CollegeStudentProfileId)
  BEGIN
    INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@CollegeStudentProfileId, @CollegeStudentUserId, N'COL-C11C12-001', @CollegeProgramId, @CollegeDepartmentId, DATEADD(year, -1, @Now), 3.40, 11, @Now, NULL, 0, NULL);
  END;

  COMMIT TRANSACTION;
  PRINT N'College core seed wrapper completed with deterministic Class 11-12 baseline users/profiles.';
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
  PRINT CONCAT(N'College core seed warning: ', ERROR_MESSAGE());
END CATCH;
GO
