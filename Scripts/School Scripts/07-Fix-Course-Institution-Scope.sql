SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Fixes missing institution-scope columns on courses and course_offerings.

  This script is safe and idempotent.
  It applies the same shape expected by Phase47_CourseInstitutionScope:
  - courses: TenantId, CampusId, InstitutionType
  - course_offerings: TenantId, CampusId, InstitutionType
  - Backfills from departments/courses
  - Creates scope indexes if missing
  - Records migration marker in __EFMigrationsHistory
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    RAISERROR('Database [Tabsan-EduSphere] does not exist.', 16, 1);
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'[courses]') IS NULL
   OR OBJECT_ID(N'[course_offerings]') IS NULL
   OR OBJECT_ID(N'[departments]') IS NULL
BEGIN
    RAISERROR('Required tables are missing ([courses], [course_offerings], [departments]). Run schema script first.', 16, 1);
    RETURN;
END;

IF COL_LENGTH('courses', 'CampusId') IS NULL
    ALTER TABLE [courses] ADD [CampusId] uniqueidentifier NULL;

IF COL_LENGTH('courses', 'InstitutionType') IS NULL
    ALTER TABLE [courses] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_courses_InstitutionType] DEFAULT(3);

IF COL_LENGTH('courses', 'TenantId') IS NULL
    ALTER TABLE [courses] ADD [TenantId] uniqueidentifier NULL;

IF COL_LENGTH('course_offerings', 'CampusId') IS NULL
    ALTER TABLE [course_offerings] ADD [CampusId] uniqueidentifier NULL;

IF COL_LENGTH('course_offerings', 'InstitutionType') IS NULL
    ALTER TABLE [course_offerings] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_course_offerings_InstitutionType] DEFAULT(3);

IF COL_LENGTH('course_offerings', 'TenantId') IS NULL
    ALTER TABLE [course_offerings] ADD [TenantId] uniqueidentifier NULL;

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

EXEC(N'
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N''IX_courses_scope_active'' AND [object_id] = OBJECT_ID(N''[courses]''))
        CREATE INDEX [IX_courses_scope_active] ON [courses] ([TenantId], [CampusId], [InstitutionType], [IsActive]);
');

EXEC(N'
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N''IX_course_offerings_scope_open'' AND [object_id] = OBJECT_ID(N''[course_offerings]''))
        CREATE INDEX [IX_course_offerings_scope_open] ON [course_offerings] ([TenantId], [CampusId], [InstitutionType], [IsOpen]);
');

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523021000_Phase47_CourseInstitutionScope', N'8.0.4');
END;

COMMIT TRANSACTION;

PRINT N'07-Fix-Course-Institution-Scope.sql applied successfully.';

SELECT 'courses' AS [TableName], [name] AS [ColumnName]
FROM sys.columns
WHERE [object_id] = OBJECT_ID('courses')
  AND [name] IN ('TenantId', 'CampusId', 'InstitutionType')
UNION ALL
SELECT 'course_offerings' AS [TableName], [name] AS [ColumnName]
FROM sys.columns
WHERE [object_id] = OBJECT_ID('course_offerings')
  AND [name] IN ('TenantId', 'CampusId', 'InstitutionType')
ORDER BY [TableName], [ColumnName];

END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();

    RAISERROR('07-Fix-Course-Institution-Scope.sql failed at line %d: %s', @ErrSeverity, @ErrState, @ErrLine, @ErrMsg);
END CATCH;
GO
