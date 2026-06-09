/*
  Maintenance Indexes & Views — Tabsan EduSphere v1.0
  
  Creates performance indexes and useful views for reporting.
*/

SET NOCOUNT ON;
GO

USE [Tabsan-EduSphere];
GO

-- ═══════════════════════════════════════════════════════════════════
-- INDEXES
-- ═══════════════════════════════════════════════════════════════════

-- Attendance lookup by student + date
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_attendance_records_student_date')
    CREATE INDEX [IX_attendance_records_student_date] ON [attendance_records] ([StudentId], [Date]);

-- Attendance by semester
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_attendance_records_semester')
    CREATE INDEX [IX_attendance_records_semester] ON [attendance_records] ([SemesterId]);

-- Results by student + semester
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_results_student_semester')
    CREATE INDEX [IX_results_student_semester] ON [results] ([StudentId], [SemesterId]);

-- Results by course
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_results_course')
    CREATE INDEX [IX_results_course] ON [results] ([CourseId]);

-- Student profiles by program
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_student_profiles_program')
    CREATE INDEX [IX_student_profiles_program] ON [student_profiles] ([ProgramId]);

-- Student profiles by department
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_student_profiles_department')
    CREATE INDEX [IX_student_profiles_department] ON [student_profiles] ([DepartmentId]);

-- Enrollments by student
IF COL_LENGTH('enrollments', 'StudentId') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_enrollments_student')
    CREATE INDEX [IX_enrollments_student] ON [enrollments] ([StudentId]);

-- Assignments by semester
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_assignments_semester')
    CREATE INDEX [IX_assignments_semester] ON [assignments] ([SemesterId]);

-- Submissions by assignment + student
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_assignment_submissions_lookup')
    CREATE INDEX [IX_assignment_submissions_lookup] ON [assignment_submissions] ([AssignmentId], [StudentId]);

-- Quizzes by semester
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_quizzes_semester')
    CREATE INDEX [IX_quizzes_semester] ON [quizzes] ([SemesterId]);

-- FYP by student
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_fyp_projects_student')
    CREATE INDEX [IX_fyp_projects_student] ON [fyp_projects] ([StudentId]);

-- Courses by department
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_courses_department')
    CREATE INDEX [IX_courses_department] ON [courses] ([DepartmentId]);

-- Users by tenant/campus
IF COL_LENGTH('users', 'TenantId') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_users_tenant_campus')
    CREATE INDEX [IX_users_tenant_campus] ON [users] ([TenantId], [CampusId]);

-- Users by institution type
IF COL_LENGTH('users', 'InstitutionType') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_users_institution_type')
    CREATE INDEX [IX_users_institution_type] ON [users] ([InstitutionType]);

-- ═══════════════════════════════════════════════════════════════════
-- USEFUL VIEWS
-- ═══════════════════════════════════════════════════════════════════

-- Student attendance summary
IF OBJECT_ID('vw_StudentAttendanceSummary') IS NULL
    EXEC(N'CREATE VIEW vw_StudentAttendanceSummary AS
    SELECT sp.Id AS StudentProfileId, sp.RegistrationNumber, u.FullName,
           s.[Name] AS SemesterName, s.Id AS SemesterId,
           COUNT(CASE WHEN ar.IsPresent=1 THEN 1 END) AS DaysPresent,
           COUNT(ar.Id) AS TotalDays,
           CASE WHEN COUNT(ar.Id)>0 THEN CAST(COUNT(CASE WHEN ar.IsPresent=1 THEN 1 END)*100.0/COUNT(ar.Id) AS DECIMAL(5,1)) ELSE 0 END AS AttendancePercentage
    FROM student_profiles sp
    JOIN users u ON u.Id=sp.UserId
    LEFT JOIN attendance_records ar ON ar.StudentId=sp.UserId
    LEFT JOIN semesters s ON s.Id=ar.SemesterId
    GROUP BY sp.Id, sp.RegistrationNumber, u.FullName, s.[Name], s.Id');

-- Student results summary
IF OBJECT_ID('vw_StudentResultsSummary') IS NULL
    EXEC(N'CREATE VIEW vw_StudentResultsSummary AS
    SELECT sp.Id AS StudentProfileId, sp.RegistrationNumber, u.FullName,
           s.[Name] AS SemesterName, COUNT(r.Id) AS SubjectsCount,
           AVG(CAST(r.MarksObtained AS DECIMAL(5,1))) AS AvgMarks,
           SUM(r.MarksObtained) AS TotalObtained, SUM(r.MaxMarks) AS TotalMax
    FROM student_profiles sp
    JOIN users u ON u.Id=sp.UserId
    LEFT JOIN results r ON r.StudentId=sp.UserId
    LEFT JOIN semesters s ON s.Id=r.SemesterId
    GROUP BY sp.Id, sp.RegistrationNumber, u.FullName, s.[Name]');

PRINT '04-Maintenance-Indexes-And-Views.sql completed.';
GO
