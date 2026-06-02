SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*
  University domain seed script.
  1) Validates baseline schema availability.
  2) Ensures deterministic baseline university entities for Semester 1-8 dummy data.
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
  PRINT N'University core seed note: database [Tabsan-EduSphere] does not exist. Run University Scripts/01-Schema-Current.sql first.';
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
    PRINT N'University core seed note: base schema tables are missing. Run Scripts/01-Schema-Current.sql before this script.';
    ROLLBACK TRANSACTION;
    RETURN;
  END;

  DECLARE @Now DATETIME2 = SYSUTCDATETIME();
  DECLARE @UniversityInstitutionType INT = 2;

  DECLARE @UniversityDepartmentId UNIQUEIDENTIFIER = CAST('12U01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityProgramId UNIQUEIDENTIFIER = CAST('22U01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityFacultyUserId UNIQUEIDENTIFIER = CAST('66U01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityStudentUserId UNIQUEIDENTIFIER = CAST('66U01000-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);
  DECLARE @UniversityStudentProfileId UNIQUEIDENTIFIER = CAST('99U01000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER);

  DECLARE @StudentRoleId INT = (SELECT TOP (1) [Id] FROM [roles] WHERE [Name] = N'Student');
  DECLARE @FacultyRoleId INT = (SELECT TOP (1) [Id] FROM [roles] WHERE [Name] = N'Faculty');

  IF @StudentRoleId IS NULL OR @FacultyRoleId IS NULL
  BEGIN
    PRINT N'University core seed note: required roles [Student]/[Faculty] are missing. Seed core roles first (Scripts/02-Seed-Core.sql).';
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

  IF NOT EXISTS (SELECT 1 FROM [departments] WHERE [Id] = @UniversityDepartmentId)
  BEGIN
    INSERT INTO [departments] ([Id], [Name], [Code], [InstitutionType], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@UniversityDepartmentId, N'University Computing Department', N'UNI-CSE', @UniversityInstitutionType, 1, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [academic_programs] WHERE [Id] = @UniversityProgramId)
  BEGIN
    INSERT INTO [academic_programs] ([Id], [Name], [Code], [DepartmentId], [TotalSemesters], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@UniversityProgramId, N'BS Computer Science (Semester 1-8)', N'UNI-BSCS-8', @UniversityDepartmentId, 8, 1, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id] = @UniversityFacultyUserId)
  BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@UniversityFacultyUserId, N'university.faculty.s1s8', N'university.faculty.s1s8@demo.local', @PwdHash, @FacultyRoleId, @UniversityDepartmentId, @UniversityInstitutionType, 1, NULL, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id] = @UniversityStudentUserId)
  BEGIN
    INSERT INTO [users] ([Id], [Username], [Email], [PasswordHash], [RoleId], [DepartmentId], [InstitutionType], [IsActive], [LastLoginAt], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@UniversityStudentUserId, N'university.student.s1s8', N'university.student.s1s8@demo.local', @PwdHash, @StudentRoleId, @UniversityDepartmentId, @UniversityInstitutionType, 1, NULL, @Now, NULL, 0, NULL);
  END;

  IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [Id] = @UniversityStudentProfileId)
  BEGIN
    INSERT INTO [student_profiles] ([Id], [UserId], [RegistrationNumber], [ProgramId], [DepartmentId], [AdmissionDate], [Cgpa], [CurrentSemesterNumber], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    VALUES (@UniversityStudentProfileId, @UniversityStudentUserId, N'UNI-S1S8-001', @UniversityProgramId, @UniversityDepartmentId, DATEADD(year, -2, @Now), 3.40, 1, @Now, NULL, 0, NULL);
  END;

  COMMIT TRANSACTION;
  PRINT N'University core seed script completed with deterministic Semester 1-8 baseline users/profiles.';
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
  PRINT CONCAT(N'University core seed warning: ', ERROR_MESSAGE());
END CATCH;
GO
