/*
  Schema creation script for Tabsan EduSphere.
  
  SCRIPT EXECUTION ORDER (IMPORTANT):
  1. 01-Schema-Current.sql      - This script: creates all tables and schema (RUN THIS FIRST)
  2. 02-Seed-Core.sql           - Seeds core data (roles, institutions, departments, users)
  3. 03-FullDummyData.sql       - Adds comprehensive test data
  4. 04-Maintenance-Indexes-And-Views.sql - Creates indexes and views (optional)
  5. 05-PostDeployment-Checks.sql - Validates data integrity (optional)

  PURPOSE:
  - Creates the complete database schema
  - Creates all tables with proper relationships
  - Defines all foreign keys and constraints
  - Establishes indexes for performance

  NOTE:
  - Must run FIRST before any other scripts
  - This script is safe to run repeatedly (uses IF NOT EXISTS checks)
  - Everything after the [Tabsan-EduSphere] metadata table is generated from the EF migrations; regenerate it with
      dotnet ef migrations script --idempotent --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
    whenever a migration is added, or the API's start-up migration fails on a database built by this script.
*/

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    CREATE DATABASE [Tabsan-EduSphere];
END;
GO

USE [Tabsan-EduSphere];
GO

IF DB_NAME() <> N'Tabsan-EduSphere'
BEGIN
    RAISERROR('Failed to switch context to [Tabsan-EduSphere]. Aborting schema script.', 16, 1);
    RETURN;
END;
GO

IF OBJECT_ID(N'[Tabsan-EduSphere]') IS NULL
BEGIN
    CREATE TABLE [Tabsan-EduSphere] (
        [Id] uniqueidentifier NOT NULL,
        [DemoKey] nvarchar(100) NOT NULL,
        [DemoValue] nvarchar(4000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Tabsan-EduSphere] PRIMARY KEY ([Id])
    );

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Tabsan-EduSphere_DemoKey' AND [object_id] = OBJECT_ID(N'[Tabsan-EduSphere]'))

    BEGIN

        CREATE UNIQUE INDEX [IX_Tabsan-EduSphere_DemoKey] ON [Tabsan-EduSphere] ([DemoKey]);

    END;
END;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [audit_logs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [OccurredAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        CONSTRAINT [PK_audit_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [departments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [license_state] (
        [Id] uniqueidentifier NOT NULL,
        [LicenseHash] nvarchar(128) NOT NULL,
        [LicenseType] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ActivatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_license_state] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [modules] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsMandatory] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_modules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(256) NULL,
        [IsSystemRole] bit NOT NULL,
        CONSTRAINT [PK_roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [module_status] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [ActivatedAt] datetime2 NULL,
        [Source] nvarchar(20) NOT NULL,
        [ChangedBy] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_module_status] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_module_status_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [users] (
        [Id] uniqueidentifier NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [RoleId] int NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_users_roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [roles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [user_sessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RefreshTokenHash] nvarchar(512) NOT NULL,
        [DeviceInfo] nvarchar(512) NULL,
        [IpAddress] nvarchar(64) NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_user_sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_user_sessions_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_actor] ON [audit_logs] ([ActorUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_occurred_at] ON [audit_logs] ([OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_departments_code] ON [departments] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_module_status_module_id] ON [module_status] ([ModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_modules_key] ON [modules] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_roles_name] ON [roles] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_sessions_token_hash] ON [user_sessions] ([RefreshTokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_sessions_user_id] ON [user_sessions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_users_email] ON [users] ([Email]) WHERE [email] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_users_RoleId] ON [users] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_users_username] ON [users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429002542_InitialCreate', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [academic_programs] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [TotalSemesters] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_academic_programs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_academic_programs_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [courses] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [CreditHours] int NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_courses_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [faculty_department_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [FacultyUserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [RemovedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_faculty_department_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_faculty_department_assignments_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [registration_whitelist] (
        [Id] uniqueidentifier NOT NULL,
        [IdentifierType] nvarchar(max) NOT NULL,
        [IdentifierValue] nvarchar(256) NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [ProgramId] uniqueidentifier NOT NULL,
        [IsUsed] bit NOT NULL,
        [UsedAt] datetime2 NULL,
        [CreatedUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_registration_whitelist] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [semesters] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [IsClosed] bit NOT NULL,
        [ClosedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_semesters] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [student_profiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RegistrationNumber] nvarchar(50) NOT NULL,
        [ProgramId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AdmissionDate] datetime2 NOT NULL,
        [Cgpa] decimal(4,2) NOT NULL,
        [CurrentSemesterNumber] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_student_profiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_student_profiles_academic_programs_ProgramId] FOREIGN KEY ([ProgramId]) REFERENCES [academic_programs] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_student_profiles_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [course_offerings] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [SemesterId] uniqueidentifier NOT NULL,
        [FacultyUserId] uniqueidentifier NULL,
        [MaxEnrollment] int NOT NULL,
        [IsOpen] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_course_offerings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_offerings_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_offerings_semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [semesters] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [enrollments] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [EnrolledAt] datetime2 NOT NULL,
        [DroppedAt] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_enrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_enrollments_course_offerings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_enrollments_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_academic_programs_code] ON [academic_programs] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_academic_programs_DepartmentId] ON [academic_programs] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_course_offerings_course_semester] ON [course_offerings] ([CourseId], [SemesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_course_offerings_SemesterId] ON [course_offerings] ([SemesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_courses_code_dept] ON [courses] ([Code], [DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_courses_DepartmentId] ON [courses] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_enrollments_CourseOfferingId] ON [enrollments] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_enrollments_student_offering] ON [enrollments] ([StudentProfileId], [CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_faculty_department_assignments_DepartmentId] ON [faculty_department_assignments] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_faculty_dept_assignments_faculty_dept] ON [faculty_department_assignments] ([FacultyUserId], [DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_registration_whitelist_identifier] ON [registration_whitelist] ([IdentifierValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_student_profiles_DepartmentId] ON [student_profiles] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE INDEX [IX_student_profiles_ProgramId] ON [student_profiles] ([ProgramId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_student_profiles_reg_no] ON [student_profiles] ([RegistrationNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_student_profiles_user_id] ON [student_profiles] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429004340_AcademicCore', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [assignments] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [DueDate] datetime2 NOT NULL,
        [MaxMarks] decimal(8,2) NOT NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_assignments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [results] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [ResultType] nvarchar(450) NOT NULL,
        [MarksObtained] decimal(8,2) NOT NULL,
        [MaxMarks] decimal(8,2) NOT NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [PublishedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_results] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [transcript_export_logs] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [ExportedAt] datetime2 NOT NULL,
        [DocumentUrl] nvarchar(2048) NULL,
        [Format] nvarchar(10) NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_transcript_export_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [assignment_submissions] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [FileUrl] nvarchar(2048) NULL,
        [TextContent] nvarchar(max) NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [MarksAwarded] decimal(8,2) NULL,
        [Feedback] nvarchar(2000) NULL,
        [GradedAt] datetime2 NULL,
        [GradedByUserId] uniqueidentifier NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_assignment_submissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_assignment_submissions_assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [assignments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE UNIQUE INDEX [IX_assignment_submissions_assignment_student] ON [assignment_submissions] ([AssignmentId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE INDEX [IX_assignments_offering_id] ON [assignments] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE INDEX [IX_results_offering_id] ON [results] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE UNIQUE INDEX [IX_results_student_offering_type] ON [results] ([StudentProfileId], [CourseOfferingId], [ResultType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE INDEX [IX_transcript_export_logs_student_id] ON [transcript_export_logs] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429005740_AssignmentsAndResults', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE TABLE [attendance_records] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Date] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [MarkedByUserId] uniqueidentifier NOT NULL,
        [Remarks] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_attendance_records] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE TABLE [notifications] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [SenderUserId] uniqueidentifier NULL,
        [IsSystemGenerated] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_notifications] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE TABLE [notification_recipients] (
        [Id] uniqueidentifier NOT NULL,
        [NotificationId] uniqueidentifier NOT NULL,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_notification_recipients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_notification_recipients_notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [notifications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE INDEX [IX_attendance_offering_date] ON [attendance_records] ([CourseOfferingId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE INDEX [IX_attendance_student_id] ON [attendance_records] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE UNIQUE INDEX [IX_attendance_student_offering_date] ON [attendance_records] ([StudentProfileId], [CourseOfferingId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_recipients_notification_user] ON [notification_recipients] ([NotificationId], [RecipientUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE INDEX [IX_notification_recipients_user_read] ON [notification_recipients] ([RecipientUserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE INDEX [IX_notifications_sender_id] ON [notifications] ([SenderUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE INDEX [IX_notifications_type] ON [notifications] ([Type]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429011542_NotificationsAndAttendance', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [fyp_projects] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [SupervisorUserId] uniqueidentifier NULL,
        [CoordinatorRemarks] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fyp_projects] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quizzes] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Instructions] nvarchar(4000) NULL,
        [TimeLimitMinutes] int NULL,
        [MaxAttempts] int NOT NULL,
        [AvailableFrom] datetime2 NULL,
        [AvailableUntil] datetime2 NULL,
        [IsPublished] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quizzes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [fyp_meetings] (
        [Id] uniqueidentifier NOT NULL,
        [FypProjectId] uniqueidentifier NOT NULL,
        [ScheduledAt] datetime2 NOT NULL,
        [Venue] nvarchar(500) NOT NULL,
        [Agenda] nvarchar(4000) NULL,
        [Status] nvarchar(20) NOT NULL,
        [OrganiserUserId] uniqueidentifier NOT NULL,
        [Minutes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fyp_meetings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_fyp_meetings_fyp_projects_FypProjectId] FOREIGN KEY ([FypProjectId]) REFERENCES [fyp_projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [fyp_panel_members] (
        [Id] uniqueidentifier NOT NULL,
        [FypProjectId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fyp_panel_members] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_fyp_panel_members_fyp_projects_FypProjectId] FOREIGN KEY ([FypProjectId]) REFERENCES [fyp_projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_attempts] (
        [Id] uniqueidentifier NOT NULL,
        [QuizId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [FinishedAt] datetime2 NULL,
        [Status] nvarchar(20) NOT NULL,
        [TotalScore] decimal(10,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_attempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_attempts_quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [quizzes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_questions] (
        [Id] uniqueidentifier NOT NULL,
        [QuizId] uniqueidentifier NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Marks] decimal(8,2) NOT NULL,
        [OrderIndex] int NOT NULL,
        [QuizId1] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_questions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_questions_quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [quizzes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_quiz_questions_quizzes_QuizId1] FOREIGN KEY ([QuizId1]) REFERENCES [quizzes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_answers] (
        [Id] uniqueidentifier NOT NULL,
        [QuizAttemptId] uniqueidentifier NOT NULL,
        [QuizQuestionId] uniqueidentifier NOT NULL,
        [SelectedOptionId] uniqueidentifier NULL,
        [TextResponse] nvarchar(4000) NULL,
        [MarksAwarded] decimal(8,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_answers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_answers_quiz_attempts_QuizAttemptId] FOREIGN KEY ([QuizAttemptId]) REFERENCES [quiz_attempts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_quiz_answers_quiz_questions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [quiz_questions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_options] (
        [Id] uniqueidentifier NOT NULL,
        [QuizQuestionId] uniqueidentifier NOT NULL,
        [Text] nvarchar(1000) NOT NULL,
        [IsCorrect] bit NOT NULL,
        [OrderIndex] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_options] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_options_quiz_questions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [quiz_questions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_fyp_meetings_FypProjectId_ScheduledAt] ON [fyp_meetings] ([FypProjectId], [ScheduledAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_fyp_meetings_OrganiserUserId_Status] ON [fyp_meetings] ([OrganiserUserId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fyp_panel_members_FypProjectId_UserId_Role] ON [fyp_panel_members] ([FypProjectId], [UserId], [Role]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_fyp_panel_members_UserId] ON [fyp_panel_members] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_fyp_projects_DepartmentId_Status] ON [fyp_projects] ([DepartmentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_fyp_projects_StudentProfileId] ON [fyp_projects] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_fyp_projects_SupervisorUserId] ON [fyp_projects] ([SupervisorUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quiz_answers_QuizAttemptId_QuizQuestionId] ON [quiz_answers] ([QuizAttemptId], [QuizQuestionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quiz_answers_QuizQuestionId] ON [quiz_answers] ([QuizQuestionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quiz_attempts_QuizId_StudentProfileId_Status] ON [quiz_attempts] ([QuizId], [StudentProfileId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quiz_attempts_StudentProfileId] ON [quiz_attempts] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quiz_options_QuizQuestionId_OrderIndex] ON [quiz_options] ([QuizQuestionId], [OrderIndex]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quiz_questions_QuizId_OrderIndex] ON [quiz_questions] ([QuizId], [OrderIndex]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quiz_questions_QuizId1] ON [quiz_questions] ([QuizId1]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quizzes_CourseOfferingId] ON [quizzes] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE INDEX [IX_quizzes_CourseOfferingId_IsPublished] ON [quizzes] ([CourseOfferingId], [IsPublished]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429013621_QuizzesAndFyp', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE TABLE [chat_conversations] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [UserRole] nvarchar(50) NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [StartedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_chat_conversations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE TABLE [chat_messages] (
        [Id] uniqueidentifier NOT NULL,
        [ConversationId] uniqueidentifier NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [TokensUsed] int NOT NULL,
        CONSTRAINT [PK_chat_messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_chat_messages_chat_conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [chat_conversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE INDEX [IX_chat_conversations_UserId] ON [chat_conversations] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE INDEX [IX_chat_conversations_UserId_StartedAt] ON [chat_conversations] ([UserId], [StartedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE INDEX [IX_chat_messages_ConversationId] ON [chat_messages] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE INDEX [IX_chat_messages_ConversationId_SentAt] ON [chat_messages] ([ConversationId], [SentAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429035351_AiAndAnalytics', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429041941_VerificationKeys'
)
BEGIN
    CREATE TABLE [consumed_verification_keys] (
        [Id] uniqueidentifier NOT NULL,
        [KeyHash] nvarchar(64) NOT NULL,
        [ConsumedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_consumed_verification_keys] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429041941_VerificationKeys'
)
BEGIN
    CREATE UNIQUE INDEX [IX_consumed_verification_keys_KeyHash] ON [consumed_verification_keys] ([KeyHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429041941_VerificationKeys'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429041941_VerificationKeys', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [users] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [users] ADD [IsLockedOut] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [users] ADD [LockedOutUntil] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [student_profiles] ADD [GraduatedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [student_profiles] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE TABLE [admin_change_requests] (
        [Id] uniqueidentifier NOT NULL,
        [RequestorUserId] uniqueidentifier NOT NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [ChangeDescription] nvarchar(500) NOT NULL,
        [Reason] nvarchar(2000) NULL,
        [NewData] NVARCHAR(MAX) NOT NULL,
        [AdminNotes] nvarchar(2000) NULL,
        [ReviewedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_admin_change_requests] PRIMARY KEY ([Id]),
        CONSTRAINT [fk_acr_requestor_user] FOREIGN KEY ([RequestorUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_acr_reviewer_user] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE TABLE [payment_receipts] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [ProofOfPaymentPath] nvarchar(500) NULL,
        [ProofUploadedAt] datetime2 NULL,
        [ConfirmedByUserId] uniqueidentifier NULL,
        [ConfirmedAt] datetime2 NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_payment_receipts] PRIMARY KEY ([Id]),
        CONSTRAINT [fk_pr_confirmed_by_user] FOREIGN KEY ([ConfirmedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_pr_created_by_user] FOREIGN KEY ([CreatedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_pr_student_profile] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE TABLE [teacher_modification_requests] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherUserId] uniqueidentifier NOT NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [ModificationType] int NOT NULL,
        [RecordId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ProposedData] NVARCHAR(MAX) NOT NULL,
        [AdminNotes] nvarchar(2000) NULL,
        [ReviewedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_teacher_modification_requests] PRIMARY KEY ([Id]),
        CONSTRAINT [fk_tmr_reviewer_user] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_tmr_teacher_user] FOREIGN KEY ([TeacherUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_acr_requestor_status] ON [admin_change_requests] ([RequestorUserId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_acr_requestor_user_id] ON [admin_change_requests] ([RequestorUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_acr_status] ON [admin_change_requests] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [IX_admin_change_requests_ReviewedByUserId] ON [admin_change_requests] ([ReviewedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [IX_payment_receipts_ConfirmedByUserId] ON [payment_receipts] ([ConfirmedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [IX_payment_receipts_CreatedByUserId] ON [payment_receipts] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_pr_due_date] ON [payment_receipts] ([DueDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_pr_status] ON [payment_receipts] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_pr_student_profile_id] ON [payment_receipts] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_pr_student_status] ON [payment_receipts] ([StudentProfileId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [IX_teacher_modification_requests_ReviewedByUserId] ON [teacher_modification_requests] ([ReviewedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_tmr_modification_type] ON [teacher_modification_requests] ([ModificationType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_tmr_record_id] ON [teacher_modification_requests] ([RecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_tmr_status] ON [teacher_modification_requests] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_tmr_teacher_status] ON [teacher_modification_requests] ([TeacherUserId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE INDEX [ix_tmr_teacher_user_id] ON [teacher_modification_requests] ([TeacherUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429043652_StudentLifecycle', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'IsLockedOut');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [users] ADD DEFAULT CAST(0 AS bit) FOR [IsLockedOut];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'FailedLoginAttempts');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [users] ADD DEFAULT 0 FOR [FailedLoginAttempts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_users_is_locked_out] ON [users] ([IsLockedOut]) WHERE [IsLockedOut] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429045706_AccountLockout', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    ALTER TABLE [users] ADD [ThemeKey] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [module_role_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_module_role_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_module_role_assignments_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [report_definitions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Purpose] nvarchar(500) NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_report_definitions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [timetables] (
        [Id] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [SemesterId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit),
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_timetables] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_timetables_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_timetables_semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [semesters] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [report_role_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [ReportDefinitionId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_report_role_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_report_role_assignments_report_definitions_ReportDefinitionId] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [report_definitions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [timetable_entries] (
        [Id] uniqueidentifier NOT NULL,
        [TimetableId] uniqueidentifier NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [SubjectName] nvarchar(200) NOT NULL,
        [RoomNumber] nvarchar(50) NULL,
        [FacultyName] nvarchar(200) NULL,
        [CourseOfferingId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_timetable_entries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_timetable_entries_timetables_TimetableId] FOREIGN KEY ([TimetableId]) REFERENCES [timetables] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_module_role_assignments_unique] ON [module_role_assignments] ([ModuleId], [RoleName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_definitions_key] ON [report_definitions] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_role_assignments_unique] ON [report_role_assignments] ([ReportDefinitionId], [RoleName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE INDEX [IX_timetable_entries_timetable_day] ON [timetable_entries] ([TimetableId], [DayOfWeek]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE INDEX [IX_timetables_dept_semester] ON [timetables] ([DepartmentId], [SemesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE INDEX [IX_timetables_SemesterId] ON [timetables] ([SemesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429223425_Phase9DashboardSettings', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    DROP INDEX [IX_timetables_dept_semester] ON [timetables];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[timetables]') AND [c].[name] = N'Title');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [timetables] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [timetables] DROP COLUMN [Title];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    EXEC sp_rename N'[timetable_entries].[CourseOfferingId]', N'RoomId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD [AcademicProgramId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD [EffectiveDate] date NOT NULL DEFAULT '0001-01-01';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD [SemesterNumber] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD [BuildingId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD [CourseId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD [FacultyUserId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE TABLE [buildings] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_buildings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE TABLE [rooms] (
        [Id] uniqueidentifier NOT NULL,
        [Number] nvarchar(50) NOT NULL,
        [BuildingId] uniqueidentifier NOT NULL,
        [Capacity] int NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_rooms_buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [buildings] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE INDEX [IX_timetables_AcademicProgramId] ON [timetables] ([AcademicProgramId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE INDEX [IX_timetables_dept_program_semester] ON [timetables] ([DepartmentId], [AcademicProgramId], [SemesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE INDEX [IX_timetable_entries_BuildingId] ON [timetable_entries] ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE INDEX [IX_timetable_entries_CourseId] ON [timetable_entries] ([CourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE INDEX [IX_timetable_entries_faculty_user] ON [timetable_entries] ([FacultyUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE INDEX [IX_timetable_entries_RoomId] ON [timetable_entries] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE UNIQUE INDEX [IX_buildings_code] ON [buildings] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE UNIQUE INDEX [IX_rooms_building_number] ON [rooms] ([BuildingId], [Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [buildings] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [rooms] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_users_FacultyUserId] FOREIGN KEY ([FacultyUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD CONSTRAINT [FK_timetables_academic_programs_AcademicProgramId] FOREIGN KEY ([AcademicProgramId]) REFERENCES [academic_programs] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429230253_Phase9TimetableRedesign', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    CREATE TABLE [sidebar_menu_items] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Purpose] nvarchar(500) NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsSystemMenu] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_sidebar_menu_items] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sidebar_menu_items_sidebar_menu_items_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [sidebar_menu_items] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    CREATE TABLE [sidebar_menu_role_accesses] (
        [Id] uniqueidentifier NOT NULL,
        [SidebarMenuItemId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(100) NOT NULL,
        [IsAllowed] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_sidebar_menu_role_accesses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sidebar_menu_role_accesses_sidebar_menu_items_SidebarMenuItemId] FOREIGN KEY ([SidebarMenuItemId]) REFERENCES [sidebar_menu_items] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sidebar_menu_items_key] ON [sidebar_menu_items] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    CREATE INDEX [IX_sidebar_menu_items_ParentId] ON [sidebar_menu_items] ([ParentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sidebar_menu_role_accesses_item_role] ON [sidebar_menu_role_accesses] ([SidebarMenuItemId], [RoleName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430000234_Phase9SidebarSettings', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430045628_Phase10PerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_audit_logs_entity_occurred_at] ON [audit_logs] ([EntityName], [OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430045628_Phase10PerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_assignments_offering_published] ON [assignments] ([CourseOfferingId], [IsPublished]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430045628_Phase10PerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430045628_Phase10PerformanceIndexes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    CREATE TABLE [outbound_email_logs] (
        [Id] uniqueidentifier NOT NULL,
        [ToAddress] nvarchar(256) NOT NULL,
        [Subject] nvarchar(500) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [AttemptedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_outbound_email_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    CREATE TABLE [password_history] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_password_history] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    CREATE INDEX [IX_outbound_email_logs_status_attempted] ON [outbound_email_logs] ([Status], [AttemptedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    CREATE INDEX [IX_password_history_user_created] ON [password_history] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430141918_Phase10SecurityTables', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430142338_Phase10StoredProcedures'
)
BEGIN
    EXEC(N'
    CREATE OR ALTER PROCEDURE sp_get_attendance_below_threshold
        @ThresholdPercent DECIMAL(5,2) = 75.0,
        @CourseOfferingId UNIQUEIDENTIFIER = NULL
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT
            ar.StudentProfileId,
            ar.CourseOfferingId,
            COUNT(*) AS TotalSessions,
            SUM(CASE WHEN ar.Status = ''Present'' THEN 1 ELSE 0 END) AS AttendedSessions,
            CAST(
                CASE WHEN COUNT(*) = 0 THEN 0.0
                     ELSE (SUM(CASE WHEN ar.Status = ''Present'' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
                END AS DECIMAL(5,2)
            ) AS AttendancePercentage
        FROM attendance_records ar
        WHERE (@CourseOfferingId IS NULL OR ar.CourseOfferingId = @CourseOfferingId)
        GROUP BY ar.StudentProfileId, ar.CourseOfferingId
        HAVING
            CASE WHEN COUNT(*) = 0 THEN 0.0
                 ELSE (SUM(CASE WHEN ar.Status = ''Present'' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
            END < @ThresholdPercent;
    END;
    ');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430142338_Phase10StoredProcedures'
)
BEGIN
    EXEC(N'
    CREATE OR ALTER PROCEDURE sp_recalculate_student_cgpa
        @StudentProfileId UNIQUEIDENTIFIER
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @TotalWeightedMarks DECIMAL(18,4) = 0;
        DECLARE @TotalMaxMarks DECIMAL(18,4) = 0;
        DECLARE @NewCgpa DECIMAL(4,2) = 0;

        SELECT
            @TotalWeightedMarks = SUM(CAST(r.MarksObtained AS DECIMAL(18,4))),
            @TotalMaxMarks = SUM(CAST(r.MaxMarks AS DECIMAL(18,4)))
        FROM results r
        WHERE r.StudentProfileId = @StudentProfileId
          AND r.IsPublished = 1
          AND r.MaxMarks > 0;

        IF @TotalMaxMarks > 0
        BEGIN
            -- Convert percentage to 4.0 GPA scale (proportional mapping: 100% -> 4.0)
            SET @NewCgpa = CAST((@TotalWeightedMarks / @TotalMaxMarks) * 4.0 AS DECIMAL(4,2));
            IF @NewCgpa > 4.0 SET @NewCgpa = 4.0;
        END

        UPDATE student_profiles
        SET Cgpa = @NewCgpa
        WHERE Id = @StudentProfileId;

        SELECT @NewCgpa AS NewCgpa;
    END;
    ');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430142338_Phase10StoredProcedures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430142338_Phase10StoredProcedures', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    EXEC(N'
    CREATE VIEW vw_student_attendance_summary AS
    SELECT
        ar.StudentProfileId,
        ar.CourseOfferingId,
        COUNT(*) AS TotalSessions,
        SUM(CASE WHEN ar.Status = ''Present'' THEN 1 ELSE 0 END) AS AttendedSessions,
        CAST(
            CASE WHEN COUNT(*) = 0 THEN 0.0
                 ELSE (SUM(CASE WHEN ar.Status = ''Present'' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
            END AS decimal(5,2)
        ) AS AttendancePercentage
    FROM attendance_records ar
    GROUP BY ar.StudentProfileId, ar.CourseOfferingId;
    ');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    EXEC(N'
    CREATE VIEW vw_student_results_summary AS
    SELECT
        r.StudentProfileId,
        r.CourseOfferingId,
        r.ResultType,
        r.MarksObtained,
        r.MaxMarks,
        CAST(
            CASE WHEN r.MaxMarks = 0 THEN 0.0
                 ELSE (CAST(r.MarksObtained AS decimal(10,2)) / r.MaxMarks) * 100.0
            END AS decimal(5,2)
        ) AS Percentage,
        r.PublishedAt,
        co.CourseId,
        c.Code AS CourseCode,
        c.Title AS CourseTitle,
        co.SemesterId
    FROM results r
    INNER JOIN course_offerings co ON co.Id = r.CourseOfferingId
    INNER JOIN courses c ON c.Id = co.CourseId
    WHERE r.IsPublished = 1;
    ');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    EXEC(N'
    CREATE VIEW vw_course_enrollment_summary AS
    SELECT
        co.Id AS CourseOfferingId,
        co.CourseId,
        c.Code AS CourseCode,
        c.Title AS CourseTitle,
        co.SemesterId,
        co.MaxEnrollment,
        COUNT(e.Id) AS EnrolledCount,
        co.MaxEnrollment - COUNT(e.Id) AS AvailableSeats
    FROM course_offerings co
    INNER JOIN courses c ON c.Id = co.CourseId
    LEFT JOIN enrollments e ON e.CourseOfferingId = co.Id AND e.Status = ''Active''
    WHERE co.IsOpen = 1
    GROUP BY co.Id, co.CourseId, c.Code, c.Title, co.SemesterId, co.MaxEnrollment;
    ');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430143000_Phase10SqlViews', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    ALTER TABLE [student_profiles] ADD [CurrentSemesterGpa] decimal(4,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    DROP INDEX [IX_results_student_offering_type] ON [results];
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[results]') AND [c].[name] = N'ResultType');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [results] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [results] ALTER COLUMN [ResultType] nvarchar(100) NOT NULL;
    CREATE UNIQUE INDEX [IX_results_student_offering_type] ON [results] ([StudentProfileId], [CourseOfferingId], [ResultType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    ALTER TABLE [results] ADD [GradePoint] decimal(4,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    CREATE TABLE [gpa_scale_rules] (
        [Id] uniqueidentifier NOT NULL,
        [GradePoint] decimal(4,2) NOT NULL,
        [MinimumScore] decimal(5,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_gpa_scale_rules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    CREATE TABLE [result_component_rules] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Weightage] decimal(5,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_result_component_rules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_gpa_scale_rules_minimum_score] ON [gpa_scale_rules] ([MinimumScore]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_result_component_rules_name] ON [result_component_rules] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260502134611_Phase11ResultCalculation', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503210356_Phase1DashboardBranding'
)
BEGIN
    CREATE TABLE [portal_settings] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_portal_settings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503210356_Phase1DashboardBranding'
)
BEGIN
    CREATE UNIQUE INDEX [IX_portal_settings_key] ON [portal_settings] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503210356_Phase1DashboardBranding'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503210356_Phase1DashboardBranding', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505_Phase2LicenseConcurrency'
)
BEGIN
    ALTER TABLE [license_state] ADD [ActivatedDomain] nvarchar(253) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505_Phase2LicenseConcurrency'
)
BEGIN
    ALTER TABLE [license_state] ADD [MaxUsers] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505_Phase2LicenseConcurrency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505_Phase2LicenseConcurrency', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506_Phase4UserImport'
)
BEGIN
    ALTER TABLE [users] ADD [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506_Phase4UserImport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506_Phase4UserImport', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN

    IF COL_LENGTH('license_state', 'ActivatedDomain') IS NULL
    BEGIN
        ALTER TABLE [license_state] ADD [ActivatedDomain] nvarchar(253) NULL;
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN

    IF COL_LENGTH('license_state', 'MaxUsers') IS NULL
    BEGIN
        ALTER TABLE [license_state] ADD [MaxUsers] int NOT NULL CONSTRAINT [DF_license_state_MaxUsers] DEFAULT (0);
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [CompletionApprovedByUserIdsCsv] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [CompletionRequestedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [CompletionRequestedByStudentProfileId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [IsCompletionRequested] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506035554_Phase4FypCompletionApprovalFlow', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    CREATE TABLE [admin_department_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [AdminUserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [RemovedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_admin_department_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_admin_department_assignments_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    CREATE INDEX [IX_admin_department_assignments_DepartmentId] ON [admin_department_assignments] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    CREATE INDEX [IX_admin_dept_assignments_admin_dept] ON [admin_department_assignments] ([AdminUserId], [DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506044806_20260506_Phase6AdminDepartmentAssignments', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    CREATE TABLE [academic_deadlines] (
        [Id] uniqueidentifier NOT NULL,
        [SemesterId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DeadlineDate] datetime2 NOT NULL,
        [ReminderDaysBefore] int NOT NULL,
        [IsActive] bit NOT NULL,
        [LastReminderSentAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_academic_deadlines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_academic_deadlines_semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [semesters] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    CREATE INDEX [IX_academic_deadlines_date_active] ON [academic_deadlines] ([DeadlineDate], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    CREATE INDEX [IX_academic_deadlines_semester] ON [academic_deadlines] ([SemesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507044625_20260507_Phase12AcademicCalendar', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE TABLE [support_tickets] (
        [Id] uniqueidentifier NOT NULL,
        [SubmitterId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [Category] int NOT NULL,
        [Subject] nvarchar(300) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Status] int NOT NULL,
        [AssignedToId] uniqueidentifier NULL,
        [ResolvedAt] datetime2 NULL,
        [ReopenWindowDays] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_support_tickets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE TABLE [support_ticket_messages] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [IsInternalNote] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_support_ticket_messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_support_ticket_messages_support_tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [support_tickets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE INDEX [IX_support_ticket_messages_author] ON [support_ticket_messages] ([AuthorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE INDEX [IX_support_ticket_messages_ticket] ON [support_ticket_messages] ([TicketId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE INDEX [IX_support_tickets_assigned] ON [support_tickets] ([AssignedToId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE INDEX [IX_support_tickets_department] ON [support_tickets] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE INDEX [IX_support_tickets_status] ON [support_tickets] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE INDEX [IX_support_tickets_submitter] ON [support_tickets] ([SubmitterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507055009_Phase14_Helpdesk', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507103000_PortalBrandingLogoValueMaxLength'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[portal_settings]') AND [c].[name] = N'Value');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [portal_settings] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [portal_settings] ALTER COLUMN [Value] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507103000_PortalBrandingLogoValueMaxLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507103000_PortalBrandingLogoValueMaxLength', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    CREATE TABLE [course_prerequisites] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [PrerequisiteCourseId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_course_prerequisites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_prerequisites_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_course_prerequisites_courses_PrerequisiteCourseId] FOREIGN KEY ([PrerequisiteCourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    CREATE UNIQUE INDEX [IX_course_prerequisites_course_prereq] ON [course_prerequisites] ([CourseId], [PrerequisiteCourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    CREATE INDEX [IX_course_prerequisites_PrerequisiteCourseId] ON [course_prerequisites] ([PrerequisiteCourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507133254_Phase15_EnrollmentRules', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubric_student_grades] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentSubmissionId] uniqueidentifier NOT NULL,
        [RubricCriterionId] uniqueidentifier NOT NULL,
        [RubricLevelId] uniqueidentifier NOT NULL,
        [PointsAwarded] decimal(8,2) NOT NULL,
        [GradedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_rubric_student_grades] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubrics] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_rubrics] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubric_criteria] (
        [Id] uniqueidentifier NOT NULL,
        [RubricId] uniqueidentifier NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [MaxPoints] decimal(8,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_rubric_criteria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_rubric_criteria_rubrics_RubricId] FOREIGN KEY ([RubricId]) REFERENCES [rubrics] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubric_levels] (
        [Id] uniqueidentifier NOT NULL,
        [CriterionId] uniqueidentifier NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [PointsAwarded] decimal(8,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [RubricCriterionId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_rubric_levels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_rubric_levels_rubric_criteria_RubricCriterionId] FOREIGN KEY ([RubricCriterionId]) REFERENCES [rubric_criteria] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE INDEX [IX_rubric_criteria_rubric_id] ON [rubric_criteria] ([RubricId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE INDEX [IX_rubric_levels_criterion_id] ON [rubric_levels] ([CriterionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE INDEX [IX_rubric_levels_RubricCriterionId] ON [rubric_levels] ([RubricCriterionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE UNIQUE INDEX [IX_rubric_student_grades_submission_criterion] ON [rubric_student_grades] ([AssignmentSubmissionId], [RubricCriterionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE INDEX [IX_rubric_student_grades_submission_id] ON [rubric_student_grades] ([AssignmentSubmissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE INDEX [IX_rubrics_assignment_active] ON [rubrics] ([AssignmentId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507223356_Phase16_FacultyGrading', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    ALTER TABLE [courses] ADD [CourseType] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    CREATE TABLE [degree_rules] (
        [Id] uniqueidentifier NOT NULL,
        [AcademicProgramId] uniqueidentifier NOT NULL,
        [MinTotalCredits] int NOT NULL,
        [MinCoreCredits] int NOT NULL,
        [MinElectiveCredits] int NOT NULL,
        [MinGpa] decimal(4,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_degree_rules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_degree_rules_academic_programs_AcademicProgramId] FOREIGN KEY ([AcademicProgramId]) REFERENCES [academic_programs] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    CREATE TABLE [degree_rule_required_courses] (
        [Id] uniqueidentifier NOT NULL,
        [DegreeRuleId] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_degree_rule_required_courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_degree_rule_required_courses_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_degree_rule_required_courses_degree_rules_DegreeRuleId] FOREIGN KEY ([DegreeRuleId]) REFERENCES [degree_rules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    CREATE INDEX [IX_degree_rule_required_courses_CourseId] ON [degree_rule_required_courses] ([CourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_degree_rule_required_courses_rule_course] ON [degree_rule_required_courses] ([DegreeRuleId], [CourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_degree_rules_program] ON [degree_rules] ([AcademicProgramId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507231326_Phase17_DegreeAudit', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    CREATE TABLE [graduation_applications] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [StudentNote] nvarchar(2000) NULL,
        [SubmittedAt] datetime2 NULL,
        [CertificatePath] nvarchar(500) NULL,
        [CertificateGeneratedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_graduation_applications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_graduation_applications_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    CREATE TABLE [graduation_application_approvals] (
        [Id] uniqueidentifier NOT NULL,
        [GraduationApplicationId] uniqueidentifier NOT NULL,
        [Stage] int NOT NULL,
        [ApproverUserId] uniqueidentifier NOT NULL,
        [IsApproved] bit NOT NULL,
        [Note] nvarchar(1000) NULL,
        [ActedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_graduation_application_approvals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_graduation_application_approvals_graduation_applications_GraduationApplicationId] FOREIGN KEY ([GraduationApplicationId]) REFERENCES [graduation_applications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    CREATE INDEX [IX_graduation_application_approvals_GraduationApplicationId] ON [graduation_application_approvals] ([GraduationApplicationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    CREATE INDEX [IX_graduation_applications_StudentProfileId] ON [graduation_applications] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508003259_Phase18_GraduationWorkflow', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [DurationUnit] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [DurationValue] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [GradingType] nvarchar(20) NOT NULL DEFAULT N'GPA';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [HasSemesters] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [TotalSemesters] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    CREATE TABLE [course_grading_configs] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [PassThreshold] decimal(5,2) NOT NULL,
        [GradingType] nvarchar(20) NOT NULL,
        [GradeRangesJson] nvarchar(4000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_course_grading_configs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_grading_configs_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    CREATE UNIQUE INDEX [IX_course_grading_configs_courseId] ON [course_grading_configs] ([CourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508043559_Phase19_CourseTypeAndGrading', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [course_announcements] (
        [Id] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [PostedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_course_announcements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_announcements_course_offerings_OfferingId] FOREIGN KEY ([OfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [course_content_modules] (
        [Id] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [WeekNumber] int NOT NULL,
        [Body] nvarchar(max) NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_course_content_modules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_content_modules_course_offerings_OfferingId] FOREIGN KEY ([OfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [discussion_threads] (
        [Id] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [IsPinned] bit NOT NULL,
        [IsClosed] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_discussion_threads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_discussion_threads_course_offerings_OfferingId] FOREIGN KEY ([OfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [content_videos] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [StorageUrl] nvarchar(1000) NULL,
        [EmbedUrl] nvarchar(1000) NULL,
        [DurationSeconds] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_content_videos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_content_videos_course_content_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [course_content_modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [discussion_replies] (
        [Id] uniqueidentifier NOT NULL,
        [ThreadId] uniqueidentifier NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_discussion_replies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_discussion_replies_discussion_threads_ThreadId] FOREIGN KEY ([ThreadId]) REFERENCES [discussion_threads] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE INDEX [IX_content_videos_ModuleId] ON [content_videos] ([ModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE INDEX [IX_course_announcements_OfferingId] ON [course_announcements] ([OfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE INDEX [IX_course_content_modules_OfferingId] ON [course_content_modules] ([OfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE INDEX [IX_discussion_replies_ThreadId] ON [discussion_replies] ([ThreadId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE INDEX [IX_discussion_threads_OfferingId] ON [discussion_threads] ([OfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508050712_Phase20_LMS', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    ALTER TABLE [academic_programs] ADD [MaxCreditLoadPerSemester] int NOT NULL DEFAULT 18;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    CREATE TABLE [study_plans] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [PlannedSemesterName] nvarchar(100) NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [AdvisorStatus] int NOT NULL,
        [AdvisorNotes] nvarchar(2000) NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_study_plans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_study_plans_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    CREATE TABLE [study_plan_courses] (
        [Id] uniqueidentifier NOT NULL,
        [StudyPlanId] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_study_plan_courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_study_plan_courses_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_study_plan_courses_study_plans_StudyPlanId] FOREIGN KEY ([StudyPlanId]) REFERENCES [study_plans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    CREATE INDEX [IX_study_plan_courses_CourseId] ON [study_plan_courses] ([CourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_study_plan_courses_plan_course] ON [study_plan_courses] ([StudyPlanId], [CourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    CREATE INDEX [IX_study_plans_StudentProfileId] ON [study_plans] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508054215_Phase21_StudyPlanner', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508132355_Phase22_ExternalIntegrations'
)
BEGIN
    CREATE TABLE [accreditation_templates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Format] nvarchar(10) NOT NULL,
        [FieldMappingsJson] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_accreditation_templates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508132355_Phase22_ExternalIntegrations'
)
BEGIN
    CREATE INDEX [IX_accreditation_templates_name] ON [accreditation_templates] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508132355_Phase22_ExternalIntegrations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508132355_Phase22_ExternalIntegrations', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508152906_Phase25_AcademicEngineUnification'
)
BEGIN
    CREATE TABLE [institution_grading_profiles] (
        [Id] uniqueidentifier NOT NULL,
        [InstitutionType] int NOT NULL,
        [PassThreshold] decimal(5,2) NOT NULL,
        [GradeRangesJson] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_institution_grading_profiles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508152906_Phase25_AcademicEngineUnification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_institution_grading_profiles_type] ON [institution_grading_profiles] ([InstitutionType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508152906_Phase25_AcademicEngineUnification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508152906_Phase25_AcademicEngineUnification', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [bulk_promotion_batches] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(180) NOT NULL,
        [Status] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ReviewedAt] datetime2 NULL,
        [AppliedAt] datetime2 NULL,
        [ReviewNote] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_bulk_promotion_batches] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [bulk_promotion_entries] (
        [Id] uniqueidentifier NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Decision] int NOT NULL,
        [Reason] nvarchar(500) NULL,
        [IsApplied] bit NOT NULL,
        [AppliedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_bulk_promotion_entries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [parent_student_links] (
        [Id] uniqueidentifier NOT NULL,
        [ParentUserId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Relationship] nvarchar(60) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_parent_student_links] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [school_streams] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_school_streams] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [student_report_cards] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [InstitutionType] int NOT NULL,
        [PeriodLabel] nvarchar(80) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [GeneratedByUserId] uniqueidentifier NOT NULL,
        [GeneratedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_student_report_cards] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [student_stream_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [SchoolStreamId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_student_stream_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_student_stream_assignments_school_streams_SchoolStreamId] FOREIGN KEY ([SchoolStreamId]) REFERENCES [school_streams] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE INDEX [IX_bulk_promotion_batches_status_created] ON [bulk_promotion_batches] ([Status], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE INDEX [IX_bulk_promotion_entries_batch] ON [bulk_promotion_entries] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE UNIQUE INDEX [IX_bulk_promotion_entries_batch_student] ON [bulk_promotion_entries] ([BatchId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE UNIQUE INDEX [IX_parent_student_links_parent_student] ON [parent_student_links] ([ParentUserId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE UNIQUE INDEX [IX_school_streams_name] ON [school_streams] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE INDEX [IX_student_report_cards_student_generated] ON [student_report_cards] ([StudentProfileId], [GeneratedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE INDEX [IX_student_stream_assignments_SchoolStreamId] ON [student_stream_assignments] ([SchoolStreamId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE UNIQUE INDEX [IX_student_stream_assignments_student] ON [student_stream_assignments] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260509044437_Phase26_SchoolCollegeExpansion', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    DROP INDEX [IX_graduation_applications_StudentProfileId] ON [graduation_applications];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_user_sessions_user_created_at] ON [user_sessions] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_support_tickets_assigned_created_at] ON [support_tickets] ([AssignedToId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_support_tickets_department_status_created_at] ON [support_tickets] ([DepartmentId], [Status], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_support_tickets_submitter_created_at] ON [support_tickets] ([SubmitterId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_quiz_attempts_quiz_student_started_at] ON [quiz_attempts] ([QuizId], [StudentProfileId], [StartedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_quiz_attempts_student_started_at] ON [quiz_attempts] ([StudentProfileId], [StartedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [ix_pr_status_due_date] ON [payment_receipts] ([Status], [DueDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [ix_pr_student_created_at] ON [payment_receipts] ([StudentProfileId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_notification_recipients_user_created_at] ON [notification_recipients] ([RecipientUserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_graduation_applications_status_created_at] ON [graduation_applications] ([Status], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    CREATE INDEX [IX_graduation_applications_student_created_at] ON [graduation_applications] ([StudentProfileId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260509155457_20260510_Phase29_IndexBaseline', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512043929_AddUserInstitutionTypeAssignment'
)
BEGIN
    ALTER TABLE [users] ADD [InstitutionType] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512043929_AddUserInstitutionTypeAssignment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512043929_AddUserInstitutionTypeAssignment', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513121000_Phase1Stage11DepartmentInstitutionType'
)
BEGIN
    ALTER TABLE [departments] ADD [InstitutionType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513121000_Phase1Stage11DepartmentInstitutionType'
)
BEGIN
    CREATE INDEX [IX_departments_institution_type] ON [departments] ([InstitutionType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513121000_Phase1Stage11DepartmentInstitutionType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513121000_Phase1Stage11DepartmentInstitutionType', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[enrollments]') AND [c].[name] = N'Status');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [enrollments] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [enrollments] ALTER COLUMN [Status] nvarchar(32) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    DROP INDEX [IX_academic_programs_code] ON [academic_programs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_academic_programs_code_dept] ON [academic_programs] ([Code], [DepartmentId]) WHERE [Code] IS NOT NULL AND [DepartmentId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_academic_programs_dept_active] ON [academic_programs] ([DepartmentId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_courses_dept_active] ON [courses] ([DepartmentId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_course_offerings_semester_open] ON [course_offerings] ([SemesterId], [IsOpen]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_course_offerings_faculty_open] ON [course_offerings] ([FacultyUserId], [IsOpen]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_student_profiles_dept_status] ON [student_profiles] ([DepartmentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_student_profiles_program_status] ON [student_profiles] ([ProgramId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_enrollments_offering_status] ON [enrollments] ([CourseOfferingId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_enrollments_student_status] ON [enrollments] ([StudentProfileId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_faculty_dept_assignments_active_lookup] ON [faculty_department_assignments] ([FacultyUserId], [RemovedAt], [DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    CREATE INDEX [IX_admin_dept_assignments_active_lookup] ON [admin_department_assignments] ([AdminUserId], [RemovedAt], [DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518091500_Phase39_MfaTotpRecoveryCodes'
)
BEGIN
    ALTER TABLE [users] ADD [MfaIsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518091500_Phase39_MfaTotpRecoveryCodes'
)
BEGIN
    ALTER TABLE [users] ADD [MfaRecoveryCodesHashJson] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518091500_Phase39_MfaTotpRecoveryCodes'
)
BEGIN
    ALTER TABLE [users] ADD [MfaTotpSecret] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518091500_Phase39_MfaTotpRecoveryCodes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260518091500_Phase39_MfaTotpRecoveryCodes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518104000_Phase40_AddUserPhoneNumber'
)
BEGIN
    ALTER TABLE [users] ADD [PhoneNumber] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260518104000_Phase40_AddUserPhoneNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260518104000_Phase40_AddUserPhoneNumber', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [users] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [users] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [departments] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [departments] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE TABLE [tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_tenants] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE TABLE [campuses] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_campuses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_campuses_tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE INDEX [IX_users_campus_id] ON [users] ([CampusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE INDEX [IX_users_tenant_id] ON [users] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE INDEX [IX_departments_campus_id] ON [departments] ([CampusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE INDEX [IX_departments_tenant_id] ON [departments] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_campuses_tenant_code] ON [campuses] ([TenantId], [Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE INDEX [IX_campuses_tenant_id] ON [campuses] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tenants_code] ON [tenants] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [FK_departments_campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [campuses] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [FK_departments_tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [FK_users_campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [campuses] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [FK_users_tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519031636_Phase41_TenantCampusFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519031636_Phase41_TenantCampusFoundation', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519032844_Phase42_DefaultTenantCampusBackfill'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = '11111111-1111-1111-1111-111111111111')
    BEGIN
        INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES ('11111111-1111-1111-1111-111111111111', 'DEFAULT', 'Default Tenant', 1, SYSUTCDATETIME(), NULL, 0, NULL)
    END

    IF NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = '22222222-2222-2222-2222-222222222222')
    BEGIN
        INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
        VALUES ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'MAIN', 'Main Campus', 1, SYSUTCDATETIME(), NULL, 0, NULL)
    END

    UPDATE [users]
    SET [TenantId] = '11111111-1111-1111-1111-111111111111'
    WHERE [TenantId] IS NULL;

    UPDATE [users]
    SET [CampusId] = '22222222-2222-2222-2222-222222222222'
    WHERE [CampusId] IS NULL;

    UPDATE [departments]
    SET [TenantId] = '11111111-1111-1111-1111-111111111111'
    WHERE [TenantId] IS NULL;

    UPDATE [departments]
    SET [CampusId] = '22222222-2222-2222-2222-222222222222'
    WHERE [CampusId] IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519032844_Phase42_DefaultTenantCampusBackfill'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519032844_Phase42_DefaultTenantCampusBackfill', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    ALTER TABLE [departments] DROP CONSTRAINT [FK_departments_campuses_CampusId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    ALTER TABLE [users] DROP CONSTRAINT [FK_users_campuses_CampusId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    ALTER TABLE [campuses] ADD CONSTRAINT [AK_campuses_Id_TenantId] UNIQUE ([Id], [TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    CREATE INDEX [IX_users_CampusId_TenantId] ON [users] ([CampusId], [TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    EXEC(N'ALTER TABLE [users] ADD CONSTRAINT [CK_users_tenant_campus_pair] CHECK (([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    CREATE INDEX [IX_departments_CampusId_TenantId] ON [departments] ([CampusId], [TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    EXEC(N'ALTER TABLE [departments] ADD CONSTRAINT [CK_departments_tenant_campus_pair] CHECK (([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [FK_departments_campuses_CampusId_TenantId] FOREIGN KEY ([CampusId], [TenantId]) REFERENCES [campuses] ([Id], [TenantId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [FK_users_campuses_CampusId_TenantId] FOREIGN KEY ([CampusId], [TenantId]) REFERENCES [campuses] ([Id], [TenantId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519034517_Phase43_TenantCampusCompatibilitySafety'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519034517_Phase43_TenantCampusCompatibilitySafety', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519040540_Phase46_TenantCampusQueryOptimization'
)
BEGIN
    CREATE INDEX [IX_users_tenant_campus_active_role] ON [users] ([TenantId], [CampusId], [IsActive], [RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519040540_Phase46_TenantCampusQueryOptimization'
)
BEGIN
    CREATE INDEX [IX_users_tenant_campus_username] ON [users] ([TenantId], [CampusId], [Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519040540_Phase46_TenantCampusQueryOptimization'
)
BEGIN
    CREATE INDEX [IX_departments_tenant_campus_name] ON [departments] ([TenantId], [CampusId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519040540_Phase46_TenantCampusQueryOptimization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519040540_Phase46_TenantCampusQueryOptimization', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE TABLE [course_materials] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CampusId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AcademicProgramId] uniqueidentifier NOT NULL,
        [SemesterId] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [LinkUrl] nvarchar(1000) NULL,
        [FilePath] nvarchar(1000) NULL,
        [MaterialType] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_course_materials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_materials_academic_programs_AcademicProgramId] FOREIGN KEY ([AcademicProgramId]) REFERENCES [academic_programs] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_materials_campuses_CampusId_TenantId] FOREIGN KEY ([CampusId], [TenantId]) REFERENCES [campuses] ([Id], [TenantId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_materials_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_materials_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_materials_semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [semesters] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_materials_tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_materials_users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_AcademicProgramId] ON [course_materials] ([AcademicProgramId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_campus_id] ON [course_materials] ([CampusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_CampusId_TenantId] ON [course_materials] ([CampusId], [TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_course_semester_active] ON [course_materials] ([CourseId], [SemesterId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_CreatedByUserId] ON [course_materials] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_DepartmentId] ON [course_materials] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_scope_lookup] ON [course_materials] ([TenantId], [CampusId], [DepartmentId], [AcademicProgramId], [SemesterId], [CourseId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_SemesterId] ON [course_materials] ([SemesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    CREATE INDEX [IX_course_materials_tenant_id] ON [course_materials] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519054518_PlanCPhase1CourseMaterialFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519054518_PlanCPhase1CourseMaterialFoundation', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519055118_PlanCPhase2DataSafetyScopeGuard'
)
BEGIN
    EXEC(N'ALTER TABLE [course_materials] ADD CONSTRAINT [CK_course_materials_location_by_type] CHECK (([MaterialType] = 1 AND [FilePath] IS NOT NULL AND LTRIM(RTRIM([FilePath])) <> '''') OR ([MaterialType] = 2 AND [LinkUrl] IS NOT NULL AND LTRIM(RTRIM([LinkUrl])) <> '''') OR ([MaterialType] = 3 AND (([FilePath] IS NOT NULL AND LTRIM(RTRIM([FilePath])) <> '''') OR ([LinkUrl] IS NOT NULL AND LTRIM(RTRIM([LinkUrl])) <> ''''))))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519055118_PlanCPhase2DataSafetyScopeGuard'
)
BEGIN
    EXEC(N'ALTER TABLE [course_materials] ADD CONSTRAINT [CK_course_materials_material_type] CHECK ([MaterialType] IN (1,2,3))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519055118_PlanCPhase2DataSafetyScopeGuard'
)
BEGIN
    EXEC(N'ALTER TABLE [course_materials] ADD CONSTRAINT [CK_course_materials_scope_required] CHECK ([TenantId] <> ''00000000-0000-0000-0000-000000000000'' AND [CampusId] <> ''00000000-0000-0000-0000-000000000000'' AND [DepartmentId] <> ''00000000-0000-0000-0000-000000000000'' AND [AcademicProgramId] <> ''00000000-0000-0000-0000-000000000000'' AND [SemesterId] <> ''00000000-0000-0000-0000-000000000000'' AND [CourseId] <> ''00000000-0000-0000-0000-000000000000'' AND [CreatedByUserId] <> ''00000000-0000-0000-0000-000000000000'')');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519055118_PlanCPhase2DataSafetyScopeGuard'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519055118_PlanCPhase2DataSafetyScopeGuard', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519215715_PlanCPhase6Stage2CourseMaterialIndexTuning'
)
BEGIN
    CREATE INDEX [IX_course_materials_scope_active_sort] ON [course_materials] ([TenantId], [CampusId], [IsActive], [Name], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260519215715_PlanCPhase6Stage2CourseMaterialIndexTuning'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519215715_PlanCPhase6Stage2CourseMaterialIndexTuning', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[assignment_submissions]') AND [c].[name] = N'Status');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [assignment_submissions] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [assignment_submissions] ALTER COLUMN [Status] nvarchar(32) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes'
)
BEGIN
    CREATE INDEX [IX_results_offering_published] ON [results] ([CourseOfferingId], [IsPublished]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes'
)
BEGIN
    CREATE INDEX [IX_results_published_published_at] ON [results] ([IsPublished], [PublishedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes'
)
BEGIN
    CREATE INDEX [IX_quiz_attempts_quiz_status] ON [quiz_attempts] ([QuizId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes'
)
BEGIN
    CREATE INDEX [IX_assignment_submissions_assignment_status] ON [assignment_submissions] ([AssignmentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes'
)
BEGIN
    CREATE INDEX [IX_assignment_submissions_student_id] ON [assignment_submissions] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260520002652_PlanDPhase5Stage52AnalyticsIndexes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    ALTER TABLE [courses] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    ALTER TABLE [courses] ADD [InstitutionType] int NOT NULL DEFAULT 3;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    ALTER TABLE [courses] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    ALTER TABLE [course_offerings] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    ALTER TABLE [course_offerings] ADD [InstitutionType] int NOT NULL DEFAULT 3;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    ALTER TABLE [course_offerings] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN

    UPDATE c
    SET
        c.TenantId = d.TenantId,
        c.CampusId = d.CampusId,
        c.InstitutionType = CAST(d.InstitutionType AS int)
    FROM courses c
    INNER JOIN departments d ON d.Id = c.DepartmentId;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN

    UPDATE o
    SET
        o.TenantId = c.TenantId,
        o.CampusId = c.CampusId,
        o.InstitutionType = c.InstitutionType
    FROM course_offerings o
    INNER JOIN courses c ON c.Id = o.CourseId;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    CREATE INDEX [IX_courses_scope_active] ON [courses] ([TenantId], [CampusId], [InstitutionType], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    CREATE INDEX [IX_course_offerings_scope_open] ON [course_offerings] ([TenantId], [CampusId], [InstitutionType], [IsOpen]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523021000_Phase47_CourseInstitutionScope', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524093000_Phase48_BuildingTenantCampusScope'
)
BEGIN
    DROP INDEX [IX_buildings_code] ON [buildings];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524093000_Phase48_BuildingTenantCampusScope'
)
BEGIN
    ALTER TABLE [buildings] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524093000_Phase48_BuildingTenantCampusScope'
)
BEGIN
    ALTER TABLE [buildings] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524093000_Phase48_BuildingTenantCampusScope'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_buildings_scope_code] ON [buildings] ([TenantId], [CampusId], [Code]) WHERE [TenantId] IS NOT NULL AND [CampusId] IS NOT NULL AND [Code] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524093000_Phase48_BuildingTenantCampusScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524093000_Phase48_BuildingTenantCampusScope', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524141843_Phase49_RoomTenantCampusScope'
)
BEGIN
    DROP INDEX [IX_rooms_building_number] ON [rooms];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524141843_Phase49_RoomTenantCampusScope'
)
BEGIN
    ALTER TABLE [rooms] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524141843_Phase49_RoomTenantCampusScope'
)
BEGIN
    ALTER TABLE [rooms] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524141843_Phase49_RoomTenantCampusScope'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_rooms_scope_building_number] ON [rooms] ([TenantId], [CampusId], [BuildingId], [Number]) WHERE [TenantId] IS NOT NULL AND [CampusId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524141843_Phase49_RoomTenantCampusScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524141843_Phase49_RoomTenantCampusScope', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524142409_Phase50_DepartmentTenantCampusScope'
)
BEGIN
    DROP INDEX [IX_departments_code] ON [departments];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524142409_Phase50_DepartmentTenantCampusScope'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_departments_scope_code] ON [departments] ([TenantId], [CampusId], [Code]) WHERE [TenantId] IS NOT NULL AND [CampusId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524142409_Phase50_DepartmentTenantCampusScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524142409_Phase50_DepartmentTenantCampusScope', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525001000_PlanL_TwoFactorSecretColumnWidening'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'MfaTotpSecret');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [users] ALTER COLUMN [MfaTotpSecret] nvarchar(512) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525001000_PlanL_TwoFactorSecretColumnWidening'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260525001000_PlanL_TwoFactorSecretColumnWidening', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602083000_Phase51_PaymentReceiptNoCompatibility'
)
BEGIN
    IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
       AND COL_LENGTH(N'payment_receipts', N'ReceiptNo') IS NULL
    BEGIN
        EXEC(N'ALTER TABLE [payment_receipts] ADD [ReceiptNo] nvarchar(64) NULL;');
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602083000_Phase51_PaymentReceiptNoCompatibility'
)
BEGIN
    IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
       AND COL_LENGTH(N'payment_receipts', N'ReceiptNo') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE [payment_receipts]
            SET [ReceiptNo] = CONCAT(N''RCPT-'', REPLACE(CONVERT(nvarchar(36), [Id]), N''-'', N''''))
            WHERE [ReceiptNo] IS NULL;
        ');

        EXEC(N'ALTER TABLE [payment_receipts] ALTER COLUMN [ReceiptNo] nvarchar(64) NOT NULL;');
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602083000_Phase51_PaymentReceiptNoCompatibility'
)
BEGIN
    IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
       AND COL_LENGTH(N'payment_receipts', N'ReceiptNo') IS NOT NULL
       AND NOT EXISTS (
           SELECT 1
           FROM sys.indexes
           WHERE [name] = N'ix_pr_receipt_no'
             AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
                            EXEC(N'CREATE UNIQUE INDEX [ix_pr_receipt_no] ON [payment_receipts] ([ReceiptNo]);');
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260602083000_Phase51_PaymentReceiptNoCompatibility'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260602083000_Phase51_PaymentReceiptNoCompatibility', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[result_component_rules]') AND name = N'IX_result_component_rules_name')
                                      DROP INDEX [IX_result_component_rules_name] ON [dbo].[result_component_rules];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[gpa_scale_rules]') AND name = N'IX_gpa_scale_rules_minimum_score')
                                      DROP INDEX [IX_gpa_scale_rules_minimum_score] ON [dbo].[gpa_scale_rules];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'MfaTotpSecret');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [users] ALTER COLUMN [MfaTotpSecret] nvarchar(512) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[users]', N'Address') IS NULL
                                      ALTER TABLE [dbo].[users] ADD [Address] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[users]', N'FullName') IS NULL
                                      ALTER TABLE [dbo].[users] ADD [FullName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[users]', N'FatherName') IS NULL
                                      ALTER TABLE [dbo].[users] ADD [FatherName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[result_component_rules]', N'InstitutionType') IS NULL
                                      ALTER TABLE [dbo].[result_component_rules] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_result_component_rules_InstitutionType] DEFAULT (0);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[payment_receipts]', N'ReceiptNo') IS NULL
                                      ALTER TABLE [dbo].[payment_receipts] ADD [ReceiptNo] nvarchar(64) NOT NULL CONSTRAINT [DF_payment_receipts_ReceiptNo] DEFAULT (N'');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[gpa_scale_rules]', N'InstitutionType') IS NULL
                                      ALTER TABLE [dbo].[gpa_scale_rules] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_gpa_scale_rules_InstitutionType] DEFAULT (0);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE TABLE [academic_document_templates] (
        [Id] uniqueidentifier NOT NULL,
        [TemplateType] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Version] nvarchar(50) NOT NULL,
        [StoragePath] nvarchar(1000) NULL,
        [FileName] nvarchar(260) NULL,
        [ContentType] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NULL,
        [CampusId] uniqueidentifier NULL,
        [DepartmentId] uniqueidentifier NULL,
        [CourseId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_academic_document_templates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE TABLE [degree_documents] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NULL,
        [AcademicDocumentTemplateId] uniqueidentifier NULL,
        [SerialNumber] nvarchar(50) NOT NULL,
        [IssueDate] nvarchar(20) NOT NULL,
        [GeneratedAtUtc] datetime2 NOT NULL,
        [DocxPath] nvarchar(1000) NOT NULL,
        [PdfPath] nvarchar(1000) NULL,
        [VerificationUrl] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_degree_documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_degree_documents_academic_document_templates_AcademicDocumentTemplateId] FOREIGN KEY ([AcademicDocumentTemplateId]) REFERENCES [academic_document_templates] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_degree_documents_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE TABLE [transcript_documents] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NULL,
        [AcademicDocumentTemplateId] uniqueidentifier NULL,
        [SerialNumber] nvarchar(50) NOT NULL,
        [IssueDate] nvarchar(20) NOT NULL,
        [GeneratedAtUtc] datetime2 NOT NULL,
        [DocxPath] nvarchar(1000) NOT NULL,
        [PdfPath] nvarchar(1000) NULL,
        [VerificationUrl] nvarchar(1000) NOT NULL,
        [CourseSnapshotJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_transcript_documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_transcript_documents_academic_document_templates_AcademicDocumentTemplateId] FOREIGN KEY ([AcademicDocumentTemplateId]) REFERENCES [academic_document_templates] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_transcript_documents_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[result_component_rules]') AND name = N'IX_result_component_rules_institution_name')
                                      CREATE UNIQUE INDEX [IX_result_component_rules_institution_name] ON [dbo].[result_component_rules] ([InstitutionType], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[payment_receipts]') AND name = N'ix_pr_receipt_no')
                                      CREATE UNIQUE INDEX [ix_pr_receipt_no] ON [dbo].[payment_receipts] ([ReceiptNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[gpa_scale_rules]') AND name = N'IX_gpa_scale_rules_institution_minimum_score')
                                      CREATE UNIQUE INDEX [IX_gpa_scale_rules_institution_minimum_score] ON [dbo].[gpa_scale_rules] ([InstitutionType], [MinimumScore]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE INDEX [IX_academic_document_templates_type_active] ON [academic_document_templates] ([TemplateType], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE UNIQUE INDEX [IX_academic_document_templates_type_version] ON [academic_document_templates] ([TemplateType], [Version]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE INDEX [IX_degree_documents_AcademicDocumentTemplateId] ON [degree_documents] ([AcademicDocumentTemplateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE INDEX [IX_degree_documents_student_generated_at] ON [degree_documents] ([StudentProfileId], [GeneratedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE INDEX [IX_degree_documents_student_id] ON [degree_documents] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE INDEX [IX_transcript_documents_AcademicDocumentTemplateId] ON [transcript_documents] ([AcademicDocumentTemplateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE INDEX [IX_transcript_documents_student_generated_at] ON [transcript_documents] ([StudentProfileId], [GeneratedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    CREATE INDEX [IX_transcript_documents_student_id] ON [transcript_documents] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603045033_AddUserAddress'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603045033_AddUserAddress', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603080000_PatchUsersFullNameFatherNameSafe'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[users]', N'FullName') IS NULL
                                      ALTER TABLE [dbo].[users] ADD [FullName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603080000_PatchUsersFullNameFatherNameSafe'
)
BEGIN
    IF COL_LENGTH(N'[dbo].[users]', N'FatherName') IS NULL
                                      ALTER TABLE [dbo].[users] ADD [FatherName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603080000_PatchUsersFullNameFatherNameSafe'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603080000_PatchUsersFullNameFatherNameSafe', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603134857_PhaseISO1AuditLoggingEnhancements'
)
BEGIN
    ALTER TABLE [audit_logs] ADD [ActorRole] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603134857_PhaseISO1AuditLoggingEnhancements'
)
BEGIN
    ALTER TABLE [audit_logs] ADD [UserAgent] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603134857_PhaseISO1AuditLoggingEnhancements'
)
BEGIN
    CREATE INDEX [IX_audit_logs_actor_role] ON [audit_logs] ([ActorRole]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603134857_PhaseISO1AuditLoggingEnhancements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603134857_PhaseISO1AuditLoggingEnhancements', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    ALTER TABLE [audit_logs] ADD [CorrelationId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    ALTER TABLE [audit_logs] ADD [DeviceInfo] nvarchar(1024) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    ALTER TABLE [audit_logs] ADD [EventCategory] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    ALTER TABLE [audit_logs] ADD [Severity] nvarchar(20) NOT NULL DEFAULT N'Info';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_audit_logs_actor_role_occurred_at] ON [audit_logs] ([ActorRole], [OccurredAt]) WHERE [ActorRole] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_audit_logs_correlation_id] ON [audit_logs] ([CorrelationId]) WHERE [CorrelationId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_audit_logs_event_category] ON [audit_logs] ([EventCategory]) WHERE [EventCategory] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_audit_logs_severity_occurred_at] ON [audit_logs] ([Severity], [OccurredAt]) WHERE [Severity] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604043644_PhaseISO1AuditLoggingPart2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604043644_PhaseISO1AuditLoggingPart2', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604051851_PhaseISO2Security'
)
BEGIN
    DROP INDEX [IX_user_sessions_user_id] ON [user_sessions];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604051851_PhaseISO2Security'
)
BEGIN
    ALTER TABLE [users] ADD [LastPasswordChangedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604051851_PhaseISO2Security'
)
BEGIN
    ALTER TABLE [user_sessions] ADD [LastActivityAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604051851_PhaseISO2Security'
)
BEGIN
    ALTER TABLE [password_history] ADD [ExpiresAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604051851_PhaseISO2Security'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_user_sessions_active] ON [user_sessions] ([UserId]) INCLUDE ([ExpiresAt], [RevokedAt]) WHERE [RevokedAt] IS NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604051851_PhaseISO2Security'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604051851_PhaseISO2Security', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604053626_PhaseISO3LoginActivity'
)
BEGIN
    CREATE TABLE [login_activity_logs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Username] nvarchar(100) NOT NULL,
        [AttemptedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(1024) NULL,
        [DeviceInfo] nvarchar(1024) NULL,
        [IsSuccess] bit NOT NULL,
        [FailureReason] nvarchar(50) NULL,
        [RiskLevel] nvarchar(20) NULL,
        [UserIsLockedOut] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_login_activity_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604053626_PhaseISO3LoginActivity'
)
BEGIN
    CREATE INDEX [IX_login_activity_attempted_at] ON [login_activity_logs] ([AttemptedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604053626_PhaseISO3LoginActivity'
)
BEGIN
    CREATE INDEX [IX_login_activity_ip_attempted] ON [login_activity_logs] ([IpAddress], [AttemptedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604053626_PhaseISO3LoginActivity'
)
BEGIN
    CREATE INDEX [IX_login_activity_success_attempted] ON [login_activity_logs] ([IsSuccess], [AttemptedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604053626_PhaseISO3LoginActivity'
)
BEGIN
    CREATE INDEX [IX_login_activity_user_id] ON [login_activity_logs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604053626_PhaseISO3LoginActivity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604053626_PhaseISO3LoginActivity', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604054539_PhaseISO4BackupDR'
)
BEGIN
    CREATE TABLE [backup_logs] (
        [Id] uniqueidentifier NOT NULL,
        [BackupType] nvarchar(20) NOT NULL,
        [FileName] nvarchar(500) NOT NULL,
        [FilePath] nvarchar(1000) NULL,
        [FileSizeBytes] bigint NULL,
        [DurationSeconds] int NULL,
        [Status] nvarchar(20) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [Checksum] nvarchar(128) NULL,
        [InitiatedBy] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_backup_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604054539_PhaseISO4BackupDR'
)
BEGIN
    CREATE INDEX [IX_backup_logs_status_started] ON [backup_logs] ([Status], [StartedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604054539_PhaseISO4BackupDR'
)
BEGIN
    CREATE INDEX [IX_backup_logs_type_started] ON [backup_logs] ([BackupType], [StartedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604054539_PhaseISO4BackupDR'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604054539_PhaseISO4BackupDR', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055147_PhaseISO5DataProtection'
)
BEGIN
    ALTER TABLE [users] ADD [ConsentToMonitoring] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055147_PhaseISO5DataProtection'
)
BEGIN
    ALTER TABLE [users] ADD [DataRetentionDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055147_PhaseISO5DataProtection'
)
BEGIN
    CREATE TABLE [data_classification_entries] (
        [Id] uniqueidentifier NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [ClassificationLevel] nvarchar(20) NOT NULL,
        [ClassifiedBy] uniqueidentifier NOT NULL,
        [ClassifiedAt] datetime2 NOT NULL,
        [Justification] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_data_classification_entries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055147_PhaseISO5DataProtection'
)
BEGIN
    CREATE INDEX [IX_data_classification_entity] ON [data_classification_entries] ([EntityName], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055147_PhaseISO5DataProtection'
)
BEGIN
    CREATE INDEX [IX_data_classification_level_classified] ON [data_classification_entries] ([ClassificationLevel], [ClassifiedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055147_PhaseISO5DataProtection'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604055147_PhaseISO5DataProtection', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055534_PhaseISO6IncidentManagement'
)
BEGIN
    CREATE TABLE [incident_logs] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [Severity] nvarchar(20) NOT NULL,
        [Category] nvarchar(30) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ReportedBy] uniqueidentifier NULL,
        [ReportedAt] datetime2 NOT NULL,
        [AssignedTo] uniqueidentifier NULL,
        [ResolvedAt] datetime2 NULL,
        [Resolution] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_incident_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055534_PhaseISO6IncidentManagement'
)
BEGIN
    CREATE INDEX [IX_incident_logs_reported_by] ON [incident_logs] ([ReportedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055534_PhaseISO6IncidentManagement'
)
BEGIN
    CREATE INDEX [IX_incident_logs_severity_status] ON [incident_logs] ([Severity], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055534_PhaseISO6IncidentManagement'
)
BEGIN
    CREATE INDEX [IX_incident_logs_status_reported] ON [incident_logs] ([Status], [ReportedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055534_PhaseISO6IncidentManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604055534_PhaseISO6IncidentManagement', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055911_PhaseISO7DocumentManagement'
)
BEGIN
    CREATE TABLE [policy_document_versions] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [ChangedBy] uniqueidentifier NULL,
        [ChangedAt] datetime2 NOT NULL,
        [ChangeNotes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_policy_document_versions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055911_PhaseISO7DocumentManagement'
)
BEGIN
    CREATE TABLE [policy_documents] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Content] nvarchar(max) NOT NULL,
        [Version] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [AccessLevel] nvarchar(20) NOT NULL,
        [CreatedBy] uniqueidentifier NULL,
        [PublishedAt] datetime2 NULL,
        [ArchivedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_policy_documents] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055911_PhaseISO7DocumentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_policy_doc_versions_doc_version] ON [policy_document_versions] ([DocumentId], [VersionNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055911_PhaseISO7DocumentManagement'
)
BEGIN
    CREATE INDEX [IX_policy_documents_access_level] ON [policy_documents] ([AccessLevel]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055911_PhaseISO7DocumentManagement'
)
BEGIN
    CREATE INDEX [IX_policy_documents_status_category] ON [policy_documents] ([Status], [Category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604055911_PhaseISO7DocumentManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604055911_PhaseISO7DocumentManagement', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604060147_PhaseISO8BackupValidation'
)
BEGIN
    CREATE TABLE [backup_verification_logs] (
        [Id] uniqueidentifier NOT NULL,
        [BackupLogId] uniqueidentifier NOT NULL,
        [VerificationType] nvarchar(20) NOT NULL,
        [VerifiedAt] datetime2 NOT NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [IsSuccessful] bit NOT NULL,
        [DurationSeconds] int NULL,
        [Issues] nvarchar(2000) NULL,
        [VerifiedChecksum] nvarchar(128) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_backup_verification_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604060147_PhaseISO8BackupValidation'
)
BEGIN
    CREATE INDEX [IX_backup_verification_backup_verified] ON [backup_verification_logs] ([BackupLogId], [VerifiedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604060147_PhaseISO8BackupValidation'
)
BEGIN
    CREATE INDEX [IX_backup_verification_success_verified] ON [backup_verification_logs] ([IsSuccessful], [VerifiedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260604060147_PhaseISO8BackupValidation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260604060147_PhaseISO8BackupValidation', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621232659_AddUserProfilePicture'
)
BEGIN
    ALTER TABLE [users] ADD [ProfilePicturePath] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621232659_AddUserProfilePicture'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [FypGradePoint] decimal(5,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621232659_AddUserProfilePicture'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [FypMarks] decimal(7,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621232659_AddUserProfilePicture'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [FypMaxMarks] decimal(7,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621232659_AddUserProfilePicture'
)
BEGIN
    CREATE TABLE [role_resource_permissions] (
        [Id] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(100) NOT NULL,
        [ResourceKey] nvarchar(100) NOT NULL,
        [CanView] bit NOT NULL,
        [CanAdd] bit NOT NULL,
        [CanEdit] bit NOT NULL,
        [CanDeactivate] bit NOT NULL,
        [CanExport] bit NOT NULL,
        [CanImport] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_role_resource_permissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621232659_AddUserProfilePicture'
)
BEGIN
    CREATE UNIQUE INDEX [IX_rrp_role_resource] ON [role_resource_permissions] ([RoleName], [ResourceKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621232659_AddUserProfilePicture'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260621232659_AddUserProfilePicture', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009043827_LicenseTamperProtection'
)
BEGIN
    ALTER TABLE [license_state] ADD [IntegritySeal] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009043827_LicenseTamperProtection'
)
BEGIN
    ALTER TABLE [license_state] ADD [LastValidatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009043827_LicenseTamperProtection'
)
BEGIN
    ALTER TABLE [license_state] ADD [LicenseBlob] varbinary(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009043827_LicenseTamperProtection'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009043827_LicenseTamperProtection', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018064107_PlanMPhase1SsoFoundation'
)
BEGIN
    CREATE TABLE [external_logins] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Provider] nvarchar(32) NOT NULL,
        [ExternalSubjectId] nvarchar(256) NOT NULL,
        [Email] nvarchar(256) NULL,
        [LinkedAt] datetime2 NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_external_logins] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_external_logins_tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_external_logins_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018064107_PlanMPhase1SsoFoundation'
)
BEGIN
    CREATE TABLE [sso_provider_settings] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Provider] nvarchar(32) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit),
        [ClientId] nvarchar(256) NULL,
        [ClientSecretEncrypted] nvarchar(2048) NULL,
        [Authority] nvarchar(500) NULL,
        [MetadataUrl] nvarchar(500) NULL,
        [AllowedEmailDomains] nvarchar(1000) NULL,
        [AutoLinkByEmail] bit NOT NULL DEFAULT CAST(0 AS bit),
        [EnforceSsoOnly] bit NOT NULL DEFAULT CAST(0 AS bit),
        [TrustIdpMfa] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_sso_provider_settings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sso_provider_settings_tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018064107_PlanMPhase1SsoFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_external_logins_tenant_provider_subject] ON [external_logins] ([TenantId], [Provider], [ExternalSubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018064107_PlanMPhase1SsoFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_external_logins_user_provider] ON [external_logins] ([UserId], [Provider]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018064107_PlanMPhase1SsoFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_sso_provider_settings_tenant_provider] ON [sso_provider_settings] ([TenantId], [Provider]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018064107_PlanMPhase1SsoFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018064107_PlanMPhase1SsoFoundation', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018080924_PlanMPhase1SsoLoginActivityMethod'
)
BEGIN
    ALTER TABLE [login_activity_logs] ADD [AuthMethod] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018080924_PlanMPhase1SsoLoginActivityMethod'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018080924_PlanMPhase1SsoLoginActivityMethod', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [AccessCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [AllowedIpRanges] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [LockAnsweredQuestions] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [OneQuestionAtATime] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [ReviewAfterCloseOnly] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [ReviewVisibility] nvarchar(30) NOT NULL DEFAULT N'ScoreOnly';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [ShuffleOptions] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quizzes] ADD [ShuffleQuestions] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[quiz_questions]') AND [c].[name] = N'QuizId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [quiz_questions] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [quiz_questions] ALTER COLUMN [QuizId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_questions] ADD [ConfigJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_questions] ADD [Difficulty] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_questions] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_questions] ADD [QuestionBankId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_questions] ADD [Tags] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_attempts] ADD [DeadlineAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_attempts] ADD [StartedFromIp] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[quiz_answers]') AND [c].[name] = N'TextResponse');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [quiz_answers] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [quiz_answers] ALTER COLUMN [TextResponse] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_answers] ADD [Feedback] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_answers] ADD [GradedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_answers] ADD [GradedByUserId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_answers] ADD [ResponseJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_answers] ADD [SavedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE TABLE [question_banks] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [CourseOfferingId] uniqueidentifier NULL,
        [DepartmentId] uniqueidentifier NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [IsShared] bit NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_question_banks] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE TABLE [quiz_accommodations] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [QuizId] uniqueidentifier NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [ExtraTimeMinutes] int NOT NULL,
        [TimeMultiplier] decimal(4,2) NOT NULL DEFAULT 1.0,
        [ExtraAttempts] int NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_accommodations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE TABLE [quiz_answer_selections] (
        [QuizAnswerId] uniqueidentifier NOT NULL,
        [QuizOptionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_quiz_answer_selections] PRIMARY KEY ([QuizAnswerId], [QuizOptionId]),
        CONSTRAINT [FK_quiz_answer_selections_quiz_answers_QuizAnswerId] FOREIGN KEY ([QuizAnswerId]) REFERENCES [quiz_answers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_quiz_answer_selections_quiz_options_QuizOptionId] FOREIGN KEY ([QuizOptionId]) REFERENCES [quiz_options] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE TABLE [quiz_attempt_items] (
        [Id] uniqueidentifier NOT NULL,
        [QuizAttemptId] uniqueidentifier NOT NULL,
        [QuizQuestionId] uniqueidentifier NOT NULL,
        [OrderIndex] int NOT NULL,
        [Marks] decimal(8,2) NOT NULL,
        [QuizQuestionGroupId] uniqueidentifier NULL,
        [PresentationJson] nvarchar(max) NULL,
        [GeneratedValuesJson] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_attempt_items] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_attempt_items_quiz_attempts_QuizAttemptId] FOREIGN KEY ([QuizAttemptId]) REFERENCES [quiz_attempts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_quiz_attempt_items_quiz_questions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [quiz_questions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE TABLE [quiz_availability_overrides] (
        [Id] uniqueidentifier NOT NULL,
        [QuizId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [AvailableFrom] datetime2 NULL,
        [AvailableUntil] datetime2 NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_availability_overrides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_availability_overrides_quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [quizzes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE TABLE [quiz_question_groups] (
        [Id] uniqueidentifier NOT NULL,
        [QuizId] uniqueidentifier NOT NULL,
        [QuestionBankId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [PickCount] int NOT NULL,
        [MarksPerQuestion] decimal(8,2) NOT NULL,
        [OrderIndex] int NOT NULL,
        [TagFilter] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_question_groups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_question_groups_question_banks_QuestionBankId] FOREIGN KEY ([QuestionBankId]) REFERENCES [question_banks] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_quiz_question_groups_quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [quizzes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_quiz_questions_QuestionBankId_IsArchived] ON [quiz_questions] ([QuestionBankId], [IsArchived]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    EXEC(N'ALTER TABLE [quiz_questions] ADD CONSTRAINT [CK_quiz_questions_owner] CHECK (([QuizId] IS NOT NULL AND [QuestionBankId] IS NULL) OR ([QuizId] IS NULL AND [QuestionBankId] IS NOT NULL))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_question_banks_TenantId_CourseOfferingId] ON [question_banks] ([TenantId], [CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_question_banks_TenantId_DepartmentId] ON [question_banks] ([TenantId], [DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quiz_accommodations_CourseOfferingId_QuizId_StudentProfileId] ON [quiz_accommodations] ([CourseOfferingId], [QuizId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_quiz_accommodations_StudentProfileId] ON [quiz_accommodations] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_quiz_answer_selections_QuizOptionId] ON [quiz_answer_selections] ([QuizOptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quiz_attempt_items_QuizAttemptId_QuizQuestionId] ON [quiz_attempt_items] ([QuizAttemptId], [QuizQuestionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_quiz_attempt_items_QuizQuestionId] ON [quiz_attempt_items] ([QuizQuestionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quiz_availability_overrides_QuizId_StudentProfileId] ON [quiz_availability_overrides] ([QuizId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_quiz_question_groups_QuestionBankId] ON [quiz_question_groups] ([QuestionBankId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    CREATE INDEX [IX_quiz_question_groups_QuizId] ON [quiz_question_groups] ([QuizId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    ALTER TABLE [quiz_questions] ADD CONSTRAINT [FK_quiz_questions_question_banks_QuestionBankId] FOREIGN KEY ([QuestionBankId]) REFERENCES [question_banks] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN

    INSERT INTO [quiz_answer_selections] ([QuizAnswerId], [QuizOptionId])
    SELECT a.[Id], a.[SelectedOptionId]
    FROM [quiz_answers] a
    INNER JOIN [quiz_options] o ON o.[Id] = a.[SelectedOptionId]
    WHERE a.[SelectedOptionId] IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM [quiz_answer_selections] s WHERE s.[QuizAnswerId] = a.[Id] AND s.[QuizOptionId] = a.[SelectedOptionId]);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018132603_PlanMPhase2QuizEngine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018132603_PlanMPhase2QuizEngine', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018141418_PlanMPhase2QuizGradebookSync'
)
BEGIN
    ALTER TABLE [results] ADD [Source] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018141418_PlanMPhase2QuizGradebookSync'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018141418_PlanMPhase2QuizGradebookSync', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [AllowedAttempts] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [AllowedFileTypes] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [AnonymousGrading] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [MaxFileSizeMb] int NOT NULL DEFAULT 10;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [ModeratedGrading] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [ModeratorUserId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [NamesRevealedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignments] ADD [PostingPolicy] nvarchar(20) NOT NULL DEFAULT N'Automatic';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [CurrentAttemptNumber] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [IsLate] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [PostedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE TABLE [submission_attempts] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [AttemptNumber] int NOT NULL,
        [TextContent] nvarchar(max) NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [IsLate] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_submission_attempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_submission_attempts_assignment_submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [assignment_submissions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE TABLE [submission_comments] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [AttemptNumber] int NULL,
        [AuthorUserId] uniqueidentifier NOT NULL,
        [AuthorIsStaff] bit NOT NULL,
        [Body] nvarchar(4000) NULL,
        [MediaStorageKey] nvarchar(500) NULL,
        [MediaContentType] nvarchar(200) NULL,
        [MediaFileName] nvarchar(260) NULL,
        [IsPrivateToGraders] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_submission_comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_submission_comments_assignment_submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [assignment_submissions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE TABLE [submission_provisional_grades] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [GraderUserId] uniqueidentifier NOT NULL,
        [Marks] decimal(8,2) NOT NULL,
        [Feedback] nvarchar(2000) NULL,
        [IsSelected] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_submission_provisional_grades] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_submission_provisional_grades_assignment_submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [assignment_submissions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE TABLE [submission_files] (
        [Id] uniqueidentifier NOT NULL,
        [AttemptId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [ContentType] nvarchar(200) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [StorageKey] nvarchar(500) NULL,
        [LegacyUrl] nvarchar(2048) NULL,
        [Sha256] nvarchar(64) NULL,
        [ConversionStatus] nvarchar(20) NOT NULL,
        [PdfStorageKey] nvarchar(500) NULL,
        [ConversionError] nvarchar(1000) NULL,
        [ConversionAttempts] int NOT NULL,
        [ConversionStartedAt] datetime2 NULL,
        [ConvertedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_submission_files] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_submission_files_submission_attempts_AttemptId] FOREIGN KEY ([AttemptId]) REFERENCES [submission_attempts] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE TABLE [submission_annotations] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionFileId] uniqueidentifier NOT NULL,
        [PageNumber] int NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [GeometryJson] nvarchar(max) NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [Color] nvarchar(9) NOT NULL,
        [AuthorUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_submission_annotations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_submission_annotations_submission_files_SubmissionFileId] FOREIGN KEY ([SubmissionFileId]) REFERENCES [submission_files] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE INDEX [IX_submission_annotations_file_page] ON [submission_annotations] ([SubmissionFileId], [PageNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE UNIQUE INDEX [IX_submission_attempts_submission_number] ON [submission_attempts] ([SubmissionId], [AttemptNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE INDEX [IX_submission_comments_submission] ON [submission_comments] ([SubmissionId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE INDEX [IX_submission_files_attempt] ON [submission_files] ([AttemptId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE INDEX [IX_submission_files_conversion] ON [submission_files] ([ConversionStatus], [ConversionStartedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    CREATE UNIQUE INDEX [IX_submission_provisional_grades_grader] ON [submission_provisional_grades] ([SubmissionId], [GraderUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN

    UPDATE s SET s.IsLate = CASE WHEN s.SubmittedAt > a.DueDate THEN 1 ELSE 0 END
    FROM assignment_submissions s JOIN assignments a ON a.Id = s.AssignmentId;

    UPDATE assignment_submissions SET PostedAt = COALESCE(GradedAt, SubmittedAt)
    WHERE MarksAwarded IS NOT NULL AND Status = 'Graded' AND PostedAt IS NULL;

    INSERT INTO submission_attempts (Id, SubmissionId, AttemptNumber, TextContent, SubmittedAt, IsLate, CreatedAt)
    SELECT NEWID(), s.Id, 1, s.TextContent, s.SubmittedAt, s.IsLate, s.SubmittedAt
    FROM assignment_submissions s
    WHERE NOT EXISTS (SELECT 1 FROM submission_attempts x WHERE x.SubmissionId = s.Id);

    INSERT INTO submission_files (Id, AttemptId, FileName, ContentType, SizeBytes, StorageKey, LegacyUrl, Sha256,
                                  ConversionStatus, PdfStorageKey, ConversionError, ConversionAttempts, CreatedAt)
    SELECT NEWID(), t.Id,
           LEFT(COALESCE(NULLIF(RIGHT(s.FileUrl, CHARINDEX('/', REVERSE(s.FileUrl) + '/') - 1), ''), 'submission'), 260),
           'application/octet-stream', 0, NULL, s.FileUrl, NULL, 'NotRequired', NULL, NULL, 0, s.SubmittedAt
    FROM assignment_submissions s
    JOIN submission_attempts t ON t.SubmissionId = s.Id AND t.AttemptNumber = 1
    WHERE s.FileUrl IS NOT NULL AND LTRIM(RTRIM(s.FileUrl)) <> ''
      AND NOT EXISTS (SELECT 1 FROM submission_files f WHERE f.AttemptId = t.Id);

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018180947_PlanMPhase3GradingWorkspace'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018180947_PlanMPhase3GradingWorkspace', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184752_PlanMPhase4WebPush'
)
BEGIN
    CREATE TABLE [notification_preferences] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Type] nvarchar(32) NOT NULL,
        [PushEnabled] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_notification_preferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_notification_preferences_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184752_PlanMPhase4WebPush'
)
BEGIN
    CREATE TABLE [push_subscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Endpoint] nvarchar(1000) NOT NULL,
        [P256dh] nvarchar(200) NOT NULL,
        [Auth] nvarchar(100) NOT NULL,
        [DeviceLabel] nvarchar(120) NULL,
        [IsActive] bit NOT NULL,
        [LastSuccessAt] datetime2 NULL,
        [LastFailureAt] datetime2 NULL,
        [ConsecutiveFailures] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_push_subscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_push_subscriptions_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184752_PlanMPhase4WebPush'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_preferences_user_type] ON [notification_preferences] ([UserId], [Type]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184752_PlanMPhase4WebPush'
)
BEGIN
    CREATE UNIQUE INDEX [IX_push_subscriptions_endpoint] ON [push_subscriptions] ([Endpoint]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184752_PlanMPhase4WebPush'
)
BEGIN
    CREATE INDEX [IX_push_subscriptions_user_active] ON [push_subscriptions] ([UserId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018184752_PlanMPhase4WebPush'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018184752_PlanMPhase4WebPush', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    ALTER TABLE [assignments] ADD [ExternalToolId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_access_tokens] (
        [Id] uniqueidentifier NOT NULL,
        [ToolId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [Scopes] nvarchar(1000) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_access_tokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_launches] (
        [Id] uniqueidentifier NOT NULL,
        [ToolId] uniqueidentifier NOT NULL,
        [DeploymentRowId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [MessageType] nvarchar(50) NOT NULL,
        [ResourceLinkId] uniqueidentifier NULL,
        [CourseOfferingId] uniqueidentifier NULL,
        [ModuleId] uniqueidentifier NULL,
        [ReturnUrl] nvarchar(1000) NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [AuthorizedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_launches] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_tools] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [ClientId] nvarchar(100) NOT NULL,
        [OidcInitiationUrl] nvarchar(1000) NOT NULL,
        [TargetLinkUri] nvarchar(1000) NOT NULL,
        [DeepLinkingUrl] nvarchar(1000) NULL,
        [RedirectUris] nvarchar(4000) NOT NULL,
        [JwksUrl] nvarchar(1000) NULL,
        [PublicKeyPem] nvarchar(4000) NULL,
        [CustomParameters] nvarchar(4000) NULL,
        [PrivacyLevel] nvarchar(20) NOT NULL,
        [AllowGradeService] bit NOT NULL,
        [AllowRosterService] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_tools] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_used_nonces] (
        [Id] uniqueidentifier NOT NULL,
        [ToolId] uniqueidentifier NOT NULL,
        [Value] nvarchar(200) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_used_nonces] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_line_items] (
        [Id] uniqueidentifier NOT NULL,
        [ToolId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [ResourceLinkId] uniqueidentifier NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [ScoreMaximum] decimal(10,2) NOT NULL,
        [Tag] nvarchar(200) NULL,
        [ResourceId] nvarchar(200) NULL,
        [StartDateTime] datetime2 NULL,
        [EndDateTime] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_line_items] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_lti_line_items_lti_tools_ToolId] FOREIGN KEY ([ToolId]) REFERENCES [lti_tools] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_resource_links] (
        [Id] uniqueidentifier NOT NULL,
        [ToolId] uniqueidentifier NOT NULL,
        [DeploymentRowId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Url] nvarchar(1000) NULL,
        [CustomParameters] nvarchar(4000) NULL,
        [PresentationTarget] nvarchar(10) NOT NULL,
        [LineItemId] uniqueidentifier NULL,
        [IsVisible] bit NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_resource_links] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_lti_resource_links_lti_tools_ToolId] FOREIGN KEY ([ToolId]) REFERENCES [lti_tools] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_tool_deployments] (
        [Id] uniqueidentifier NOT NULL,
        [ToolId] uniqueidentifier NOT NULL,
        [DeploymentId] nvarchar(100) NOT NULL,
        [Scope] nvarchar(20) NOT NULL,
        [ScopeId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_tool_deployments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_lti_tool_deployments_lti_tools_ToolId] FOREIGN KEY ([ToolId]) REFERENCES [lti_tools] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE TABLE [lti_scores] (
        [Id] uniqueidentifier NOT NULL,
        [LineItemId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [ScoreGiven] decimal(10,2) NULL,
        [ScoreMaximum] decimal(10,2) NULL,
        [ActivityProgress] nvarchar(20) NOT NULL,
        [GradingProgress] nvarchar(20) NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [Timestamp] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_lti_scores] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_lti_scores_lti_line_items_LineItemId] FOREIGN KEY ([LineItemId]) REFERENCES [lti_line_items] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE UNIQUE INDEX [IX_lti_access_tokens_hash] ON [lti_access_tokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE INDEX [IX_lti_launches_expires] ON [lti_launches] ([ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE INDEX [IX_lti_line_items_tool_offering] ON [lti_line_items] ([ToolId], [CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE INDEX [IX_lti_resource_links_offering] ON [lti_resource_links] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE INDEX [IX_lti_resource_links_ToolId] ON [lti_resource_links] ([ToolId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE INDEX [IX_lti_scores_line_item_student] ON [lti_scores] ([LineItemId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE UNIQUE INDEX [IX_lti_tool_deployments_deployment_id] ON [lti_tool_deployments] ([DeploymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE INDEX [IX_lti_tool_deployments_ToolId] ON [lti_tool_deployments] ([ToolId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE UNIQUE INDEX [IX_lti_tools_client_id] ON [lti_tools] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE INDEX [IX_lti_used_nonces_expires] ON [lti_used_nonces] ([ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    CREATE UNIQUE INDEX [IX_lti_used_nonces_tool_value] ON [lti_used_nonces] ([ToolId], [Value]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018225808_PlanMPhase5Lti'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018225808_PlanMPhase5Lti', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE TABLE [outcomes] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Level] nvarchar(20) NOT NULL,
        [ProgramId] uniqueidentifier NULL,
        [CourseId] uniqueidentifier NULL,
        [ParentId] uniqueidentifier NULL,
        [Code] nvarchar(40) NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [MasteryPoints] decimal(9,2) NOT NULL,
        [CalculationMethod] nvarchar(20) NOT NULL,
        [CalculationParameter] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_outcomes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE TABLE [outcome_alignments] (
        [Id] uniqueidentifier NOT NULL,
        [OutcomeId] uniqueidentifier NOT NULL,
        [TargetType] nvarchar(20) NOT NULL,
        [TargetId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_outcome_alignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_outcome_alignments_outcomes_OutcomeId] FOREIGN KEY ([OutcomeId]) REFERENCES [outcomes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE TABLE [outcome_mappings] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOutcomeId] uniqueidentifier NOT NULL,
        [ProgramOutcomeId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_outcome_mappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_outcome_mappings_outcomes_CourseOutcomeId] FOREIGN KEY ([CourseOutcomeId]) REFERENCES [outcomes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_outcome_mappings_outcomes_ProgramOutcomeId] FOREIGN KEY ([ProgramOutcomeId]) REFERENCES [outcomes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE TABLE [outcome_rating_levels] (
        [Id] uniqueidentifier NOT NULL,
        [OutcomeId] uniqueidentifier NOT NULL,
        [Description] nvarchar(100) NOT NULL,
        [Points] decimal(9,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_outcome_rating_levels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_outcome_rating_levels_outcomes_OutcomeId] FOREIGN KEY ([OutcomeId]) REFERENCES [outcomes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE TABLE [student_outcome_results] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [OutcomeId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [AlignmentId] uniqueidentifier NOT NULL,
        [SourceType] nvarchar(30) NOT NULL,
        [SourceId] uniqueidentifier NOT NULL,
        [Percent] decimal(9,4) NOT NULL,
        [Score] decimal(9,2) NOT NULL,
        [AssessedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_student_outcome_results] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_student_outcome_results_outcome_alignments_AlignmentId] FOREIGN KEY ([AlignmentId]) REFERENCES [outcome_alignments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_outcome_alignments_offering] ON [outcome_alignments] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_outcome_alignments_outcome_target] ON [outcome_alignments] ([OutcomeId], [TargetType], [TargetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_outcome_alignments_target] ON [outcome_alignments] ([TargetType], [TargetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_outcome_mappings_pair] ON [outcome_mappings] ([CourseOutcomeId], [ProgramOutcomeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_outcome_mappings_program_outcome] ON [outcome_mappings] ([ProgramOutcomeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_outcome_rating_levels_OutcomeId] ON [outcome_rating_levels] ([OutcomeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_outcomes_course] ON [outcomes] ([CourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_outcomes_program] ON [outcomes] ([ProgramId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_outcomes_scope_code] ON [outcomes] ([TenantId], [Level], [ProgramId], [CourseId], [Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_student_outcome_results_AlignmentId] ON [student_outcome_results] ([AlignmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_student_outcome_results_offering_student] ON [student_outcome_results] ([CourseOfferingId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE INDEX [IX_student_outcome_results_outcome_student] ON [student_outcome_results] ([OutcomeId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_student_outcome_results_unique] ON [student_outcome_results] ([StudentProfileId], [AlignmentId], [SourceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261018233743_PlanMPhase6Outcomes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018233743_PlanMPhase6Outcomes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    ALTER TABLE [course_content_modules] ADD [CompletionMode] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    ALTER TABLE [course_content_modules] ADD [RequireSequentialProgress] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    ALTER TABLE [course_content_modules] ADD [UnlockAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE TABLE [module_items] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NOT NULL,
        [ItemType] nvarchar(20) NOT NULL,
        [TargetId] uniqueidentifier NULL,
        [Title] nvarchar(300) NOT NULL,
        [Url] nvarchar(2000) NULL,
        [Body] nvarchar(max) NULL,
        [Position] int NOT NULL,
        [IndentLevel] int NOT NULL,
        [IsPublished] bit NOT NULL,
        [RequirementType] nvarchar(20) NOT NULL,
        [MinScorePercent] decimal(5,2) NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_module_items] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_module_items_course_content_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [course_content_modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE TABLE [module_prerequisites] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [PrerequisiteModuleId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_module_prerequisites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_module_prerequisites_course_content_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [course_content_modules] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_module_prerequisites_course_content_modules_PrerequisiteModuleId] FOREIGN KEY ([PrerequisiteModuleId]) REFERENCES [course_content_modules] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE TABLE [module_item_progress] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [ModuleItemId] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NOT NULL,
        [ViewedAt] datetime2 NULL,
        [MarkedDoneAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_module_item_progress] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_module_item_progress_module_items_ModuleItemId] FOREIGN KEY ([ModuleItemId]) REFERENCES [module_items] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE INDEX [IX_module_item_progress_ModuleItemId] ON [module_item_progress] ([ModuleItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE INDEX [IX_module_item_progress_offering_student] ON [module_item_progress] ([OfferingId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE UNIQUE INDEX [IX_module_item_progress_student_item] ON [module_item_progress] ([StudentProfileId], [ModuleItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE INDEX [IX_module_items_module_position] ON [module_items] ([ModuleId], [Position]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE INDEX [IX_module_items_offering] ON [module_items] ([OfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE INDEX [IX_module_items_target] ON [module_items] ([ItemType], [TargetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE UNIQUE INDEX [IX_module_prerequisites_pair] ON [module_prerequisites] ([ModuleId], [PrerequisiteModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    CREATE INDEX [IX_module_prerequisites_PrerequisiteModuleId] ON [module_prerequisites] ([PrerequisiteModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN

    INSERT INTO module_items (Id, ModuleId, OfferingId, ItemType, TargetId, Title, Url, Body, Position, IndentLevel, IsPublished,
                              RequirementType, MinScorePercent, CreatedByUserId, CreatedAt)
    SELECT NEWID(), v.ModuleId, m.OfferingId, 'Video', v.Id, v.Title, NULL, NULL,
           ROW_NUMBER() OVER (PARTITION BY v.ModuleId ORDER BY v.CreatedAt, v.Id), 0, 1, 'None', NULL,
           '00000000-0000-0000-0000-000000000000', SYSUTCDATETIME()
    FROM content_videos v
    JOIN course_content_modules m ON m.Id = v.ModuleId
    WHERE v.IsDeleted = 0 AND m.IsDeleted = 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019035915_PlanMPhase7ModuleProgress'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019035915_PlanMPhase7ModuleProgress', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [discussion_threads] ADD [GroupId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignments] ADD [AvailableFrom] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignments] ADD [AvailableUntil] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignments] ADD [GradeGroupIndividually] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignments] ADD [GroupSetId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignments] ADD [PeerReviewAnonymous] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignments] ADD [PeerReviewCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignments] ADD [PeerReviewDueDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [GroupId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [IsMissing] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [LatePenalty] decimal(8,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [LatePenaltyWaived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    ALTER TABLE [assignment_submissions] ADD [RawMarks] decimal(8,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    UPDATE assignment_submissions SET RawMarks = MarksAwarded WHERE MarksAwarded IS NOT NULL AND RawMarks IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE TABLE [assignment_overrides] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NULL,
        [GroupId] uniqueidentifier NULL,
        [DueDate] datetime2 NOT NULL,
        [AvailableFrom] datetime2 NULL,
        [AvailableUntil] datetime2 NULL,
        [Note] nvarchar(300) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_assignment_overrides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_assignment_overrides_assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [assignments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE TABLE [group_sets] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [SelfSignup] bit NOT NULL,
        [MaxSize] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_group_sets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_group_sets_course_offerings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE TABLE [late_policies] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [LateDeductionEnabled] bit NOT NULL,
        [DeductionPercent] decimal(5,2) NOT NULL,
        [Interval] nvarchar(10) NOT NULL,
        [LowestGradePercent] decimal(5,2) NOT NULL,
        [MissingEnabled] bit NOT NULL,
        [MissingGradePercent] decimal(5,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_late_policies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_late_policies_course_offerings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE TABLE [peer_reviews] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [ReviewerStudentProfileId] uniqueidentifier NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Score] decimal(8,2) NULL,
        [Comment] nvarchar(4000) NULL,
        [RubricJson] nvarchar(max) NULL,
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_peer_reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_peer_reviews_assignment_submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [assignment_submissions] ([Id]),
        CONSTRAINT [FK_peer_reviews_assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [assignments] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE TABLE [student_groups] (
        [Id] uniqueidentifier NOT NULL,
        [GroupSetId] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_student_groups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_student_groups_group_sets_GroupSetId] FOREIGN KEY ([GroupSetId]) REFERENCES [group_sets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE TABLE [student_group_members] (
        [Id] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        [GroupSetId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_student_group_members] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_student_group_members_student_groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [student_groups] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_discussion_threads_group] ON [discussion_threads] ([GroupId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_assignments_group_set] ON [assignments] ([GroupSetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_assignment_submissions_group] ON [assignment_submissions] ([GroupId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_assignment_overrides_assignment] ON [assignment_overrides] ([AssignmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_group_sets_offering] ON [group_sets] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_late_policies_offering] ON [late_policies] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_peer_reviews_assignment_reviewer] ON [peer_reviews] ([AssignmentId], [ReviewerStudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_peer_reviews_submission_reviewer] ON [peer_reviews] ([SubmissionId], [ReviewerStudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_student_group_members_group] ON [student_group_members] ([GroupId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_student_group_members_set_student] ON [student_group_members] ([GroupSetId], [StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    CREATE INDEX [IX_student_groups_set] ON [student_groups] ([GroupSetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019043258_PlanMPhase8AssignmentEnhancements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019043258_PlanMPhase8AssignmentEnhancements', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    ALTER TABLE [assignments] ADD [SimilarityCheckEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    ALTER TABLE [assignments] ADD [SimilarityVisibleToStudents] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE TABLE [similarity_checks] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [AttemptNumber] int NOT NULL,
        [Provider] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Score] decimal(5,1) NULL,
        [SourcesJson] nvarchar(max) NULL,
        [ExternalId] nvarchar(100) NULL,
        [WebhookTokenHash] nvarchar(64) NULL,
        [ReportUrl] nvarchar(2000) NULL,
        [Error] nvarchar(1000) NULL,
        [Tries] int NOT NULL,
        [NextAttemptAt] datetime2 NULL,
        [SubmittedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [RequestedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_similarity_checks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_similarity_checks_assignment_submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [assignment_submissions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE TABLE [similarity_documents] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NULL,
        [AttemptNumber] int NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [WordCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_similarity_documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_similarity_documents_assignment_submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [assignment_submissions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE TABLE [similarity_settings] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Provider] nvarchar(20) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [Scope] nvarchar(20) NOT NULL,
        [AccountEmail] nvarchar(256) NULL,
        [ApiKeyEncrypted] nvarchar(1000) NULL,
        [Sandbox] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_similarity_settings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE TABLE [similarity_fingerprints] (
        [DocumentId] uniqueidentifier NOT NULL,
        [Position] int NOT NULL,
        [Hash] bigint NOT NULL,
        CONSTRAINT [PK_similarity_fingerprints] PRIMARY KEY ([DocumentId], [Position]),
        CONSTRAINT [FK_similarity_fingerprints_similarity_documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [similarity_documents] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE INDEX [IX_similarity_checks_assignment] ON [similarity_checks] ([AssignmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE INDEX [IX_similarity_checks_queue] ON [similarity_checks] ([Status], [NextAttemptAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE INDEX [IX_similarity_checks_submission] ON [similarity_checks] ([SubmissionId], [AttemptNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE INDEX [IX_similarity_documents_assignment] ON [similarity_documents] ([AssignmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE INDEX [IX_similarity_documents_offering] ON [similarity_documents] ([CourseOfferingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_similarity_documents_submission_attempt] ON [similarity_documents] ([SubmissionId], [AttemptNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE INDEX [IX_similarity_documents_tenant] ON [similarity_documents] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    CREATE INDEX [IX_similarity_fingerprints_hash] ON [similarity_fingerprints] ([Hash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_similarity_settings_tenant] ON [similarity_settings] ([TenantId]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019142907_PlanMPhase9Similarity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019142907_PlanMPhase9Similarity', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    CREATE TABLE [conference_settings] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Provider] nvarchar(20) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [BaseUrl] nvarchar(500) NULL,
        [SecretEncrypted] nvarchar(2000) NULL,
        [AccountId] nvarchar(200) NULL,
        [ClientId] nvarchar(200) NULL,
        [OrganizerUserId] nvarchar(200) NULL,
        [JoinEarlyMinutes] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_conference_settings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    CREATE TABLE [live_sessions] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Provider] nvarchar(20) NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [DurationMinutes] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ExternalMeetingId] nvarchar(300) NULL,
        [JoinUrl] nvarchar(2000) NULL,
        [HostUrlEncrypted] nvarchar(4000) NULL,
        [RecordingUrl] nvarchar(2000) NULL,
        [ModeratorPassword] nvarchar(64) NOT NULL,
        [AttendeePassword] nvarchar(64) NOT NULL,
        [TimetableEntryId] uniqueidentifier NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [AutoAttendance] bit NOT NULL,
        [MinimumMinutes] int NOT NULL,
        [ReminderSentAt] datetime2 NULL,
        [AttendanceTakenAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_live_sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_live_sessions_course_offerings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    CREATE TABLE [live_session_attendance] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NULL,
        [FirstJoinedAt] datetime2 NOT NULL,
        [LastJoinedAt] datetime2 NOT NULL,
        [JoinCount] int NOT NULL,
        [Minutes] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_live_session_attendance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_live_session_attendance_live_sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [live_sessions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_conference_settings_tenant] ON [conference_settings] ([TenantId]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    CREATE UNIQUE INDEX [IX_live_session_attendance_session_user] ON [live_session_attendance] ([SessionId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    CREATE INDEX [IX_live_sessions_offering_start] ON [live_sessions] ([CourseOfferingId], [StartsAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    CREATE INDEX [IX_live_sessions_status_start] ON [live_sessions] ([Status], [StartsAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019150024_PlanMPhase10LiveClasses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019150024_PlanMPhase10LiveClasses', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019191719_PlanMPhase11Localisation'
)
BEGIN
    ALTER TABLE [users] ADD [PreferredLanguage] nvarchar(8) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019191719_PlanMPhase11Localisation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019191719_PlanMPhase11Localisation', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020100346_PlanMPhase12Mobile'
)
BEGIN
    CREATE TABLE [mobile_crash_reports] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Platform] nvarchar(10) NOT NULL,
        [AppVersion] nvarchar(32) NOT NULL,
        [OsVersion] nvarchar(64) NULL,
        [DeviceModel] nvarchar(100) NULL,
        [ExceptionType] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [StackTrace] nvarchar(max) NOT NULL,
        [Screen] nvarchar(200) NULL,
        [OccurredAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_mobile_crash_reports] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020100346_PlanMPhase12Mobile'
)
BEGIN
    CREATE TABLE [mobile_devices] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Platform] nvarchar(10) NOT NULL,
        [Token] nvarchar(512) NOT NULL,
        [AppVersion] nvarchar(32) NULL,
        [DeviceModel] nvarchar(100) NULL,
        [Language] nvarchar(8) NULL,
        [IsActive] bit NOT NULL,
        [LastSeenAt] datetime2 NOT NULL,
        [LastSuccessAt] datetime2 NULL,
        [ConsecutiveFailures] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_mobile_devices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020100346_PlanMPhase12Mobile'
)
BEGIN
    CREATE INDEX [IX_mobile_crash_reports_occurred] ON [mobile_crash_reports] ([OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020100346_PlanMPhase12Mobile'
)
BEGIN
    CREATE INDEX [IX_mobile_crash_reports_version_type] ON [mobile_crash_reports] ([AppVersion], [ExceptionType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020100346_PlanMPhase12Mobile'
)
BEGIN
    CREATE UNIQUE INDEX [IX_mobile_devices_token] ON [mobile_devices] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020100346_PlanMPhase12Mobile'
)
BEGIN
    CREATE INDEX [IX_mobile_devices_user_active] ON [mobile_devices] ([UserId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020100346_PlanMPhase12Mobile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020100346_PlanMPhase12Mobile', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    ALTER TABLE [assignments] ADD [ScormPackageId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE TABLE [scorm_attempts] (
        [Id] uniqueidentifier NOT NULL,
        [PackageId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [CompletionStatus] nvarchar(20) NOT NULL,
        [SuccessStatus] nvarchar(20) NOT NULL,
        [ScoreRaw] decimal(12,4) NULL,
        [ScoreMin] decimal(12,4) NULL,
        [ScoreMax] decimal(12,4) NULL,
        [ScoreScaled] decimal(7,6) NULL,
        [ScorePercent] decimal(5,2) NULL,
        [Location] nvarchar(1000) NULL,
        [SuspendData] nvarchar(max) NULL,
        [Exit] nvarchar(20) NULL,
        [CmiJson] nvarchar(max) NULL,
        [TotalSeconds] int NOT NULL,
        [CommitCount] int NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [LastCommitAt] datetime2 NULL,
        [FinishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_scorm_attempts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE TABLE [scorm_packages] (
        [Id] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Version] nvarchar(12) NOT NULL,
        [Identifier] nvarchar(200) NULL,
        [LaunchPath] nvarchar(1000) NOT NULL,
        [FileCount] int NOT NULL,
        [ScoCount] int NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [MasteryScore] decimal(5,2) NULL,
        [MaxAttempts] int NOT NULL,
        [IsPublished] bit NOT NULL,
        [AssignmentId] uniqueidentifier NULL,
        [MaxMarks] decimal(7,2) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [IsRemoved] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_scorm_packages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE TABLE [xapi_credentials] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Key] nvarchar(64) NOT NULL,
        [SecretHash] nvarchar(64) NOT NULL,
        [OfferingId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [LastUsedAt] datetime2 NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_xapi_credentials] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE TABLE [xapi_statements] (
        [Id] uniqueidentifier NOT NULL,
        [StatementId] uniqueidentifier NOT NULL,
        [CredentialId] uniqueidentifier NOT NULL,
        [ActorKey] nvarchar(300) NOT NULL,
        [VerbId] nvarchar(1000) NOT NULL,
        [ObjectId] nvarchar(1000) NOT NULL,
        [Registration] uniqueidentifier NULL,
        [ScoreScaled] decimal(7,6) NULL,
        [Success] bit NULL,
        [Completion] bit NULL,
        [Timestamp] datetime2 NOT NULL,
        [Stored] datetime2 NOT NULL,
        [IsVoided] bit NOT NULL,
        [Json] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_xapi_statements] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE UNIQUE INDEX [IX_scorm_attempts_package_student_number] ON [scorm_attempts] ([PackageId], [StudentProfileId], [Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE INDEX [IX_scorm_attempts_student] ON [scorm_attempts] ([StudentProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE INDEX [IX_scorm_packages_offering] ON [scorm_packages] ([OfferingId], [IsRemoved]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE UNIQUE INDEX [IX_xapi_credentials_key] ON [xapi_credentials] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE INDEX [IX_xapi_statements_actor_stored] ON [xapi_statements] ([ActorKey], [Stored]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE UNIQUE INDEX [IX_xapi_statements_statement_id] ON [xapi_statements] ([StatementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    CREATE INDEX [IX_xapi_statements_stored] ON [xapi_statements] ([Stored]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023084839_PlanMPhase13Scorm'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023084839_PlanMPhase13Scorm', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023125753_PlanMPhase14ApiTokensIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rooms_BuildingId' AND object_id = OBJECT_ID(N'rooms')) CREATE INDEX [IX_rooms_BuildingId] ON [rooms] ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023125753_PlanMPhase14ApiTokensIndexes'
)
BEGIN
    CREATE TABLE [api_tokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Hint] nvarchar(16) NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [ReadOnly] bit NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [LastUsedAt] datetime2 NULL,
        [LastUsedIp] nvarchar(45) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_api_tokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023125753_PlanMPhase14ApiTokensIndexes'
)
BEGIN
    CREATE INDEX [IX_incident_logs_assigned_to] ON [incident_logs] ([AssignedTo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023125753_PlanMPhase14ApiTokensIndexes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_api_tokens_hash] ON [api_tokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023125753_PlanMPhase14ApiTokensIndexes'
)
BEGIN
    CREATE INDEX [IX_api_tokens_user] ON [api_tokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023125753_PlanMPhase14ApiTokensIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023125753_PlanMPhase14ApiTokensIndexes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    ALTER TABLE [sso_provider_settings] ADD [IdClaim] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    ALTER TABLE [sso_provider_settings] ADD [LinkById] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    ALTER TABLE [sso_provider_settings] ADD [RequireEncryptedAssertions] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    ALTER TABLE [sso_provider_settings] ADD [RequireRoleMatch] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    ALTER TABLE [sso_provider_settings] ADD [RoleClaim] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    ALTER TABLE [sso_provider_settings] ADD [RoleMappings] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    ALTER TABLE [sso_provider_settings] ADD [SignAuthnRequests] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024052458_PlanMSsoAttributeMapping'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024052458_PlanMSsoAttributeMapping', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024053625_PlanMLtiDynamicRegistration'
)
BEGIN
    ALTER TABLE [lti_tools] ADD [RegisteredDynamicallyAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024053625_PlanMLtiDynamicRegistration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024053625_PlanMLtiDynamicRegistration', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025004703_PlanMScormMultiPart'
)
BEGIN
    ALTER TABLE [scorm_packages] ADD [PartsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025004703_PlanMScormMultiPart'
)
BEGIN
    ALTER TABLE [scorm_attempts] ADD [PartsStateJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025004703_PlanMScormMultiPart'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025004703_PlanMScormMultiPart', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025104259_PlanMScormSequencing'
)
BEGIN
    ALTER TABLE [scorm_packages] ADD [SequencingJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025104259_PlanMScormSequencing'
)
BEGIN
    ALTER TABLE [scorm_packages] ADD [StructureVersion] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025104259_PlanMScormSequencing'
)
BEGIN
    ALTER TABLE [scorm_attempts] ADD [SequencingStateJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025104259_PlanMScormSequencing'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025104259_PlanMScormSequencing', N'8.0.8');
END;
GO

COMMIT;
GO

