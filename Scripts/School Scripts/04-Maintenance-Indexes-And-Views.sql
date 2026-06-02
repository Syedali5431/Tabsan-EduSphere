SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*
  School maintenance/index script.
  Adds school-focused query indexes for Class 1-10 coverage access patterns.
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    PRINT N'School maintenance note: database [Tabsan-EduSphere] does not exist. Run School Scripts/01-Schema-Current.sql first.';
    RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'[student_report_cards]') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_SRC_School_Class1_10'
          AND [object_id] = OBJECT_ID(N'[student_report_cards]')
    )
    BEGIN
        CREATE INDEX [IX_SRC_School_Class1_10]
            ON [student_report_cards] ([InstitutionType], [PeriodLabel], [StudentProfileId]);
    END;
END;

IF OBJECT_ID(N'[results]') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_Results_School_Class1_10'
          AND [object_id] = OBJECT_ID(N'[results]')
    )
    BEGIN
        CREATE INDEX [IX_Results_School_Class1_10]
            ON [results] ([ResultType], [StudentProfileId], [CourseOfferingId]);
    END;
END;

IF OBJECT_ID(N'[courses]') IS NOT NULL
AND COL_LENGTH('courses', 'TenantId') IS NOT NULL
AND COL_LENGTH('courses', 'CampusId') IS NOT NULL
AND COL_LENGTH('courses', 'InstitutionType') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_courses_scope_active'
          AND [object_id] = OBJECT_ID(N'[courses]')
    )
    BEGIN
        CREATE INDEX [IX_courses_scope_active]
            ON [courses] ([TenantId], [CampusId], [InstitutionType], [IsActive]);
    END;
END;

IF OBJECT_ID(N'[course_offerings]') IS NOT NULL
AND COL_LENGTH('course_offerings', 'TenantId') IS NOT NULL
AND COL_LENGTH('course_offerings', 'CampusId') IS NOT NULL
AND COL_LENGTH('course_offerings', 'InstitutionType') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_course_offerings_scope_open'
          AND [object_id] = OBJECT_ID(N'[course_offerings]')
    )
    BEGIN
        CREATE INDEX [IX_course_offerings_scope_open]
            ON [course_offerings] ([TenantId], [CampusId], [InstitutionType], [IsOpen]);
    END;
END;

PRINT N'School maintenance script completed.';
GO
