SET NOCOUNT ON;

/*
  Post-deployment validation script for Tabsan EduSphere.
  
  PREREQUISITE SCRIPTS - MUST RUN FIRST IN THIS ORDER:
  1. 01-Schema-Current.sql      - Creates all tables and schema
  2. 02-Seed-Core.sql           - Seeds core data (roles, institutions, departments, users)
  3. 03-FullDummyData.sql       - Adds comprehensive test data
  4. 04-Maintenance-Indexes-And-Views.sql - Creates indexes and views (optional)
  5. 05-PostDeployment-Checks.sql - This script: validates data integrity

  PURPOSE:
  - Verify database schema is complete
  - Validate core data has been seeded correctly
  - Check that all prerequisite scripts ran successfully
  - Provide summary statistics for verification

  NOTE:
  - This script is read-only; it only performs SELECT queries
  - It's safe to run repeatedly
  - Use this to verify deployment success
*/

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
	RAISERROR('Database [Tabsan-EduSphere] does not exist. Run 01-Schema-Current.sql first.', 16, 1);
	RETURN;
END;
GO

USE [Tabsan-EduSphere];
GO

IF DB_NAME() <> N'Tabsan-EduSphere'
BEGIN
	RAISERROR('Failed to switch context to [Tabsan-EduSphere]. Aborting check script.', 16, 1);
	RETURN;
END;
GO

PRINT 'Running post-deployment checks...';

SELECT 'SchemaVersionCount' AS [CheckName], COUNT(1) AS [Value]
FROM __EFMigrationsHistory;

SELECT 'RoleCount' AS [CheckName], COUNT(1) AS [Value]
FROM roles;

SELECT 'FinanceRoleCount' AS [CheckName], COUNT(1) AS [Value]
FROM roles
WHERE [Name] = N'Finance';

SELECT 'ModuleCount' AS [CheckName], COUNT(1) AS [Value]
FROM modules;

SELECT 'ModuleStatusCount' AS [CheckName], COUNT(1) AS [Value]
FROM module_status;

SELECT 'DepartmentCount' AS [CheckName], COUNT(1) AS [Value]
FROM departments;

SELECT 'ProgramCount' AS [CheckName], COUNT(1) AS [Value]
FROM academic_programs;

SELECT 'CourseCount' AS [CheckName], COUNT(1) AS [Value]
FROM courses;

SELECT 'CourseOfferingCount' AS [CheckName], COUNT(1) AS [Value]
FROM course_offerings;

SELECT 'StudentProfileCount' AS [CheckName], COUNT(1) AS [Value]
FROM student_profiles;

SELECT 'NotificationCount' AS [CheckName], COUNT(1) AS [Value]
FROM notifications;

IF OBJECT_ID(N'[Tabsan-EduSphere]') IS NOT NULL
BEGIN
	SELECT 'TabsanEduSphereMetaCount' AS [CheckName], COUNT(1) AS [Value]
	FROM [Tabsan-EduSphere];
END;

SELECT 'ResultCount' AS [CheckName], COUNT(1) AS [Value]
FROM results;

SELECT 'QuizCount' AS [CheckName], COUNT(1) AS [Value]
FROM quizzes;

SELECT 'QuizQuestionCount' AS [CheckName], COUNT(1) AS [Value]
FROM quiz_questions;

SELECT 'QuizAttemptCount' AS [CheckName], COUNT(1) AS [Value]
FROM quiz_attempts;

SELECT 'SupportTicketCount' AS [CheckName], COUNT(1) AS [Value]
FROM support_tickets;

SELECT 'DiscussionThreadCount' AS [CheckName], COUNT(1) AS [Value]
FROM discussion_threads;

SELECT 'PortalSettingsCount' AS [CheckName], COUNT(1) AS [Value]
FROM portal_settings;

SELECT
	'UsersInstitutionTypeColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('users', 'InstitutionType') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT
	'UsersPhoneNumberColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('users', 'PhoneNumber') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT
	'UsersMfaIsEnabledColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('users', 'MfaIsEnabled') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT
	'UsersMfaTotpSecretColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('users', 'MfaTotpSecret') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT
	'UsersMfaRecoveryCodesHashJsonColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('users', 'MfaRecoveryCodesHashJson') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT
	'UsersPhoneNumberMaxLength' AS [CheckName],
	ISNULL(COL_LENGTH('users', 'PhoneNumber'), 0) AS [Value];

SELECT 'PaymentSummaryReportCount' AS [CheckName], COUNT(1) AS [Value]
FROM report_definitions
WHERE [Key] = N'payment_summary';

SELECT 'PaymentSummaryReportFinanceRoleAssignmentCount' AS [CheckName], COUNT(1) AS [Value]
FROM report_definitions rd
INNER JOIN report_role_assignments rra ON rra.ReportDefinitionId = rd.Id
WHERE rd.[Key] = N'payment_summary'
  AND rra.[RoleName] = N'Finance';

SELECT 'UsersInstitutionTypeAssignedCount' AS [CheckName], COUNT(1) AS [Value]
FROM users
WHERE InstitutionType IS NOT NULL;

SELECT
	'DepartmentsInstitutionTypeColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('departments', 'InstitutionType') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT
	'EnrollmentsStatusMaxLength' AS [CheckName],
	ISNULL(COL_LENGTH('enrollments', 'Status'), 0) AS [Value];

SELECT 'IndexExists_IX_departments_institution_type' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_departments_institution_type' AND object_id = OBJECT_ID('departments')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_academic_programs_code_dept' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_academic_programs_code_dept' AND object_id = OBJECT_ID('academic_programs')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_enrollments_offering_status' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_enrollments_offering_status' AND object_id = OBJECT_ID('enrollments')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_enrollments_student_status' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_enrollments_student_status' AND object_id = OBJECT_ID('enrollments')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_users_active_phone' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_active_phone' AND object_id = OBJECT_ID('users')) THEN 1 ELSE 0 END AS [Value];

SELECT 'MigrationExists_Stage11_DepartmentInstitutionType' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260513121000_Phase1Stage11DepartmentInstitutionType') THEN 1 ELSE 0 END AS [Value];

SELECT 'MigrationExists_Stage12_ReferentialIntegrityAndIndexes' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes') THEN 1 ELSE 0 END AS [Value];

SELECT 'DepartmentInstitutionType_InvalidCount' AS [CheckName],
	COUNT(1) AS [Value]
FROM departments
WHERE InstitutionType NOT IN (0, 1, 2);

SELECT 'DepartmentInstitutionType_CoverageCount' AS [CheckName],
	COUNT(DISTINCT InstitutionType) AS [Value]
FROM departments
WHERE InstitutionType IN (0, 1, 2);

SELECT 'DepartmentInstitutionType_0_School_Count' AS [CheckName],
	COUNT(1) AS [Value]
FROM departments
WHERE InstitutionType = 0;

SELECT 'DepartmentInstitutionType_1_College_Count' AS [CheckName],
	COUNT(1) AS [Value]
FROM departments
WHERE InstitutionType = 1;

SELECT 'DepartmentInstitutionType_2_University_Count' AS [CheckName],
	COUNT(1) AS [Value]
FROM departments
WHERE InstitutionType = 2;

SELECT 'OrphanCount_AcademicPrograms_Department' AS [CheckName],
	COUNT(1) AS [Value]
FROM academic_programs ap
LEFT JOIN departments d ON d.Id = ap.DepartmentId
WHERE d.Id IS NULL;

SELECT 'OrphanCount_Courses_Department' AS [CheckName],
	COUNT(1) AS [Value]
FROM courses c
LEFT JOIN departments d ON d.Id = c.DepartmentId
WHERE d.Id IS NULL;

SELECT 'OrphanCount_StudentProfiles_Department' AS [CheckName],
	COUNT(1) AS [Value]
FROM student_profiles sp
LEFT JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.Id IS NULL;

SELECT 'OrphanCount_StudentProfiles_Program' AS [CheckName],
	COUNT(1) AS [Value]
FROM student_profiles sp
LEFT JOIN academic_programs ap ON ap.Id = sp.ProgramId
WHERE ap.Id IS NULL;

SELECT 'OrphanCount_CourseOfferings_Course' AS [CheckName],
	COUNT(1) AS [Value]
FROM course_offerings co
LEFT JOIN courses c ON c.Id = co.CourseId
WHERE c.Id IS NULL;

SELECT 'OrphanCount_CourseOfferings_Semester' AS [CheckName],
	COUNT(1) AS [Value]
FROM course_offerings co
LEFT JOIN semesters s ON s.Id = co.SemesterId
WHERE s.Id IS NULL;

SELECT 'OrphanCount_Enrollments_StudentProfile' AS [CheckName],
	COUNT(1) AS [Value]
FROM enrollments e
LEFT JOIN student_profiles sp ON sp.Id = e.StudentProfileId
WHERE sp.Id IS NULL;

SELECT 'OrphanCount_Enrollments_CourseOffering' AS [CheckName],
	COUNT(1) AS [Value]
FROM enrollments e
LEFT JOIN course_offerings co ON co.Id = e.CourseOfferingId
WHERE co.Id IS NULL;

SELECT 'OrphanCount_FacultyAssignments_Department' AS [CheckName],
	COUNT(1) AS [Value]
FROM faculty_department_assignments fda
LEFT JOIN departments d ON d.Id = fda.DepartmentId
WHERE d.Id IS NULL;

SELECT 'OrphanCount_AdminAssignments_Department' AS [CheckName],
	COUNT(1) AS [Value]
FROM admin_department_assignments ada
LEFT JOIN departments d ON d.Id = ada.DepartmentId
WHERE d.Id IS NULL;

/* Stage 5.3 parity data-quality and replay-safety checks */
SELECT 'DummySeed_DepartmentCoverageByInstitutionType' AS [CheckName],
	COUNT(DISTINCT InstitutionType) AS [Value]
FROM departments
WHERE InstitutionType IN (0, 1, 2);

SELECT 'DummySeed_UsersCount_InstitutionType_0_School' AS [CheckName], COUNT(1) AS [Value]
FROM users
WHERE InstitutionType = 0;

SELECT 'DummySeed_UsersCount_InstitutionType_1_College' AS [CheckName], COUNT(1) AS [Value]
FROM users
WHERE InstitutionType = 1;

SELECT 'DummySeed_UsersCount_InstitutionType_2_University' AS [CheckName], COUNT(1) AS [Value]
FROM users
WHERE InstitutionType = 2;

SELECT 'DummySeed_StudentProfilesByInstitutionType_0_School' AS [CheckName], COUNT(1) AS [Value]
FROM student_profiles sp
INNER JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.InstitutionType = 0;

SELECT 'DummySeed_StudentProfilesByInstitutionType_1_College' AS [CheckName], COUNT(1) AS [Value]
FROM student_profiles sp
INNER JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.InstitutionType = 1;

SELECT 'DummySeed_StudentProfilesByInstitutionType_2_University' AS [CheckName], COUNT(1) AS [Value]
FROM student_profiles sp
INNER JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.InstitutionType = 2;

SELECT 'DummySeed_TimetableCountByInstitutionType_0_School' AS [CheckName], COUNT(1) AS [Value]
FROM timetables t
INNER JOIN departments d ON d.Id = t.DepartmentId
WHERE d.InstitutionType = 0;

SELECT 'DummySeed_TimetableCountByInstitutionType_1_College' AS [CheckName], COUNT(1) AS [Value]
FROM timetables t
INNER JOIN departments d ON d.Id = t.DepartmentId
WHERE d.InstitutionType = 1;

SELECT 'DummySeed_TimetableCountByInstitutionType_2_University' AS [CheckName], COUNT(1) AS [Value]
FROM timetables t
INNER JOIN departments d ON d.Id = t.DepartmentId
WHERE d.InstitutionType = 2;

SELECT 'DummySeed_PaymentReceiptCountByInstitutionType_0_School' AS [CheckName], COUNT(1) AS [Value]
FROM payment_receipts pr
INNER JOIN student_profiles sp ON sp.Id = pr.StudentProfileId
INNER JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.InstitutionType = 0;

SELECT 'DummySeed_PaymentReceiptCountByInstitutionType_1_College' AS [CheckName], COUNT(1) AS [Value]
FROM payment_receipts pr
INNER JOIN student_profiles sp ON sp.Id = pr.StudentProfileId
INNER JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.InstitutionType = 1;

SELECT 'DummySeed_PaymentReceiptCountByInstitutionType_2_University' AS [CheckName], COUNT(1) AS [Value]
FROM payment_receipts pr
INNER JOIN student_profiles sp ON sp.Id = pr.StudentProfileId
INNER JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.InstitutionType = 2;

SELECT 'DummySeed_CriticalEntityCount_AdminAssignments' AS [CheckName], COUNT(1) AS [Value]
FROM admin_department_assignments;

SELECT 'DummySeed_CriticalEntityCount_FacultyAssignments' AS [CheckName], COUNT(1) AS [Value]
FROM faculty_department_assignments;

SELECT 'DummySeed_CriticalEntityCount_Buildings' AS [CheckName], COUNT(1) AS [Value]
FROM buildings;

SELECT 'DummySeed_CriticalEntityCount_Rooms' AS [CheckName], COUNT(1) AS [Value]
FROM rooms;

SELECT 'DummySeed_CriticalEntityCount_TimetableEntries' AS [CheckName], COUNT(1) AS [Value]
FROM timetable_entries;

SELECT 'DummySeed_CriticalEntityCount_TranscriptExportLogs' AS [CheckName], COUNT(1) AS [Value]
FROM transcript_export_logs;

SELECT 'DummySeed_CriticalEntityCount_BulkPromotionBatches' AS [CheckName], COUNT(1) AS [Value]
FROM bulk_promotion_batches;

SELECT 'DummySeed_CriticalEntityCount_BulkPromotionEntries' AS [CheckName], COUNT(1) AS [Value]
FROM bulk_promotion_entries;

SELECT 'DummySeed_CriticalEntityCount_GraduationApplications' AS [CheckName], COUNT(1) AS [Value]
FROM graduation_applications;

SELECT 'DummySeed_CriticalEntityCount_GraduationApprovals' AS [CheckName], COUNT(1) AS [Value]
FROM graduation_application_approvals;

SELECT 'DummySeed_CriticalEntityCount_StudentReportCards' AS [CheckName], COUNT(1) AS [Value]
FROM student_report_cards;

SELECT 'DummySeed_CriticalEntityCount_SchoolStreams' AS [CheckName], COUNT(1) AS [Value]
FROM school_streams;

SELECT 'DummySeed_CriticalEntityCount_StudentStreamAssignments' AS [CheckName], COUNT(1) AS [Value]
FROM student_stream_assignments;

SELECT 'DummySeed_CriticalEntityCount_LicenseState' AS [CheckName], COUNT(1) AS [Value]
FROM license_state;

SELECT 'DummySeed_CriticalEntityCount_ModuleRoleAssignments' AS [CheckName], COUNT(1) AS [Value]
FROM module_role_assignments;

SELECT 'DummySeed_CriticalEntityCount_RegistrationWhitelist' AS [CheckName], COUNT(1) AS [Value]
FROM registration_whitelist;

SELECT 'DummySeed_CriticalEntityCount_GpaScaleRules' AS [CheckName], COUNT(1) AS [Value]
FROM gpa_scale_rules;

SELECT 'DummySeed_CriticalEntityCount_ResultComponentRules' AS [CheckName], COUNT(1) AS [Value]
FROM result_component_rules;

SELECT 'DummySeed_CriticalEntityCount_AcademicDeadlines' AS [CheckName], COUNT(1) AS [Value]
FROM academic_deadlines;

SELECT 'DummySeed_CriticalEntityCount_CoursePrerequisites' AS [CheckName], COUNT(1) AS [Value]
FROM course_prerequisites;

SELECT 'DummySeed_CriticalEntityCount_DegreeRules' AS [CheckName], COUNT(1) AS [Value]
FROM degree_rules;

SELECT 'DummySeed_CriticalEntityCount_DegreeRuleRequiredCourses' AS [CheckName], COUNT(1) AS [Value]
FROM degree_rule_required_courses;

SELECT 'DummySeed_CriticalEntityCount_CourseGradingConfigs' AS [CheckName], COUNT(1) AS [Value]
FROM course_grading_configs;

SELECT 'DummySeed_CriticalEntityCount_StudyPlans' AS [CheckName], COUNT(1) AS [Value]
FROM study_plans;

SELECT 'DummySeed_CriticalEntityCount_StudyPlanCourses' AS [CheckName], COUNT(1) AS [Value]
FROM study_plan_courses;

SELECT 'DummySeed_CriticalEntityCount_CourseAnnouncements' AS [CheckName], COUNT(1) AS [Value]
FROM course_announcements;

SELECT 'DummySeed_CriticalEntityCount_CourseContentModules' AS [CheckName], COUNT(1) AS [Value]
FROM course_content_modules;

SELECT 'DummySeed_CriticalEntityCount_ContentVideos' AS [CheckName], COUNT(1) AS [Value]
FROM content_videos;

SELECT 'DummySeed_CriticalEntityCount_Rubrics' AS [CheckName], COUNT(1) AS [Value]
FROM rubrics;

SELECT 'DummySeed_CriticalEntityCount_RubricCriteria' AS [CheckName], COUNT(1) AS [Value]
FROM rubric_criteria;

SELECT 'DummySeed_CriticalEntityCount_RubricLevels' AS [CheckName], COUNT(1) AS [Value]
FROM rubric_levels;

SELECT 'DummySeed_CriticalEntityCount_RubricStudentGrades' AS [CheckName], COUNT(1) AS [Value]
FROM rubric_student_grades;

SELECT 'DummySeed_CriticalEntityCount_FypProjects' AS [CheckName], COUNT(1) AS [Value]
FROM fyp_projects;

SELECT 'DummySeed_CriticalEntityCount_FypMeetings' AS [CheckName], COUNT(1) AS [Value]
FROM fyp_meetings;

SELECT 'DummySeed_CriticalEntityCount_FypPanelMembers' AS [CheckName], COUNT(1) AS [Value]
FROM fyp_panel_members;

SELECT 'DummySeed_CriticalEntityCount_ChatConversations' AS [CheckName], COUNT(1) AS [Value]
FROM chat_conversations;

SELECT 'DummySeed_CriticalEntityCount_ChatMessages' AS [CheckName], COUNT(1) AS [Value]
FROM chat_messages;

SELECT 'DummySeed_CriticalEntityCount_UserSessions' AS [CheckName], COUNT(1) AS [Value]
FROM user_sessions;

SELECT 'DummySeed_CriticalEntityCount_ConsumedVerificationKeys' AS [CheckName], COUNT(1) AS [Value]
FROM consumed_verification_keys;

SELECT 'DummySeed_CriticalEntityCount_PasswordHistory' AS [CheckName], COUNT(1) AS [Value]
FROM password_history;

SELECT 'DummySeed_CriticalEntityCount_OutboundEmailLogs' AS [CheckName], COUNT(1) AS [Value]
FROM outbound_email_logs;

SELECT 'DummySeed_CriticalEntityCount_AdminChangeRequests' AS [CheckName], COUNT(1) AS [Value]
FROM admin_change_requests;

SELECT 'DummySeed_CriticalEntityCount_TeacherModificationRequests' AS [CheckName], COUNT(1) AS [Value]
FROM teacher_modification_requests;

SELECT 'DummySeed_CriticalEntityCount_AccreditationTemplates' AS [CheckName], COUNT(1) AS [Value]
FROM accreditation_templates;

SELECT 'DummySeed_CriticalEntityCount_InstitutionGradingProfiles' AS [CheckName], COUNT(1) AS [Value]
FROM institution_grading_profiles;

SELECT 'DummySeed_CriticalEntityCount_ParentStudentLinks' AS [CheckName], COUNT(1) AS [Value]
FROM parent_student_links;

SELECT 'DummySeed_CriticalEntityCount_AuditLogs' AS [CheckName], COUNT(1) AS [Value]
FROM audit_logs;

SELECT 'IndexExists_IX_course_content_modules_offering_week' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_course_content_modules_offering_week' AND object_id = OBJECT_ID('course_content_modules')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_parent_student_links_parent_active' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_parent_student_links_parent_active' AND object_id = OBJECT_ID('parent_student_links')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_registration_whitelist_value_department_program' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_registration_whitelist_value_department_program' AND object_id = OBJECT_ID('registration_whitelist')) THEN 1 ELSE 0 END AS [Value];

SELECT 'DummySeed_RegistrationNumberDuplicates' AS [CheckName], COUNT(1) AS [Value]
FROM (
	SELECT RegistrationNumber
	FROM student_profiles
	GROUP BY RegistrationNumber
	HAVING COUNT(1) > 1
) dup;

SELECT 'DummySeed_UsernameDuplicates' AS [CheckName], COUNT(1) AS [Value]
FROM (
	SELECT Username
	FROM users
	GROUP BY Username
	HAVING COUNT(1) > 1
) dup;

SELECT 'DummySeed_DemoDatasetVersionRowCount' AS [CheckName], COUNT(1) AS [Value]
FROM [Tabsan-EduSphere]
WHERE DemoKey = N'DemoDatasetVersion';

SELECT TOP 20 [MigrationId], [ProductVersion]
FROM __EFMigrationsHistory
ORDER BY [MigrationId] DESC;

PRINT 'Post-deployment checks completed.';
