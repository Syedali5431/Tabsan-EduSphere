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

SELECT 'FinanceUserCount' AS [CheckName], COUNT(1) AS [Value]
FROM users u
INNER JOIN roles r ON r.Id = u.RoleId
WHERE r.[Name] = N'Finance'
	AND u.[IsDeleted] = 0;

SELECT 'ModuleCount' AS [CheckName], COUNT(1) AS [Value]
FROM modules;

SELECT 'ModuleStatusCount' AS [CheckName], COUNT(1) AS [Value]
FROM module_status;

SELECT 'DepartmentCount' AS [CheckName], COUNT(1) AS [Value]
FROM departments;

SELECT 'TenantCount' AS [CheckName], COUNT(1) AS [Value]
FROM tenants;

SELECT 'CampusCount' AS [CheckName], COUNT(1) AS [Value]
FROM campuses;

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

IF COL_LENGTH('users', 'PhoneNumber') IS NOT NULL
BEGIN
	SELECT 'UsersWithPhoneNumberCount' AS [CheckName], COUNT(1) AS [Value]
	FROM users
	WHERE PhoneNumber IS NOT NULL
	  AND LTRIM(RTRIM(PhoneNumber)) <> N'';
END
ELSE
BEGIN
	SELECT 'UsersWithPhoneNumberCount' AS [CheckName], CAST(0 AS int) AS [Value];
END;

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

SELECT 'UsersTenantIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('users', 'TenantId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'UsersCampusIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('users', 'CampusId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'DepartmentsTenantIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('departments', 'TenantId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'DepartmentsCampusIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('departments', 'CampusId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'BuildingsTenantIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('buildings', 'TenantId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'BuildingsCampusIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('buildings', 'CampusId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'RoomsTenantIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('rooms', 'TenantId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'RoomsCampusIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('rooms', 'CampusId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CoursesTenantIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('courses', 'TenantId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CoursesCampusIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('courses', 'CampusId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CoursesInstitutionTypeColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('courses', 'InstitutionType') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CourseOfferingsTenantIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('course_offerings', 'TenantId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CourseOfferingsCampusIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('course_offerings', 'CampusId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CourseOfferingsInstitutionTypeColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('course_offerings', 'InstitutionType') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CourseMaterialsTableExists' AS [CheckName],
	CASE WHEN OBJECT_ID('course_materials') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CourseMaterialsTenantIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('course_materials', 'TenantId') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'CourseMaterialsCampusIdColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('course_materials', 'CampusId') IS NULL THEN 0 ELSE 1 END AS [Value];

IF OBJECT_ID(N'course_materials') IS NOT NULL
AND COL_LENGTH('course_materials', 'FilePath') IS NOT NULL
BEGIN
	SELECT 'CourseMaterialsUnsupportedFileExtensionCount' AS [CheckName],
		COUNT(1) AS [Value]
	FROM course_materials
	WHERE FilePath IS NOT NULL
	  AND LTRIM(RTRIM(FilePath)) <> N''
	  AND LOWER(
		CASE
			WHEN CHARINDEX(N'.', REVERSE(FilePath)) > 0
			THEN RIGHT(FilePath, CHARINDEX(N'.', REVERSE(FilePath)))
			ELSE N''
		END
	  ) NOT IN (N'.doc', N'.docx', N'.ppt', N'.pptx', N'.pdf', N'.txt', N'.xls', N'.xlsx', N'.jpg', N'.jpeg', N'.png');

	SELECT
		'CourseMaterialsAllowedFileExtensionCount_' + ext.[Extension] AS [CheckName],
		COUNT(1) AS [Value]
	FROM course_materials cm
	CROSS APPLY
	(
		SELECT LOWER(
			CASE
				WHEN cm.FilePath IS NULL OR LTRIM(RTRIM(cm.FilePath)) = N'' THEN N''
				WHEN CHARINDEX(N'.', REVERSE(cm.FilePath)) > 0
				THEN RIGHT(cm.FilePath, CHARINDEX(N'.', REVERSE(cm.FilePath)))
				ELSE N''
			END
		) AS [Extension]
	) ext
	WHERE ext.[Extension] IN (N'.doc', N'.docx', N'.ppt', N'.pptx', N'.pdf', N'.txt', N'.xls', N'.xlsx', N'.jpg', N'.jpeg', N'.png')
	GROUP BY ext.[Extension]
	ORDER BY ext.[Extension];
END;

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

SELECT 'IndexExists_IX_departments_scope_institution' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_departments_scope_institution' AND object_id = OBJECT_ID('departments')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_buildings_scope_code' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_buildings_scope_code' AND object_id = OBJECT_ID('buildings')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_rooms_scope_building_number' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_rooms_scope_building_number' AND object_id = OBJECT_ID('rooms')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_courses_scope_active' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_courses_scope_active' AND object_id = OBJECT_ID('courses')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_course_offerings_scope_open' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_course_offerings_scope_open' AND object_id = OBJECT_ID('course_offerings')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_course_materials_scope_lookup' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_course_materials_scope_lookup' AND object_id = OBJECT_ID('course_materials')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_course_materials_scope_active_sort' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_course_materials_scope_active_sort' AND object_id = OBJECT_ID('course_materials')) THEN 1 ELSE 0 END AS [Value];

SELECT 'CoreAdminAssignmentExists' AS [CheckName],
	CASE WHEN EXISTS (
		SELECT 1
		FROM admin_department_assignments a
		WHERE a.AdminUserId = '66666666-6666-6666-6666-666666666602'
		  AND a.DepartmentId = '21000000-0000-0000-0000-000000000001'
		  AND a.RemovedAt IS NULL) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_users_scope_role_department_active' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_scope_role_department_active' AND object_id = OBJECT_ID('users')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_attendance_offering_date' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_attendance_offering_date' AND object_id = OBJECT_ID('attendance_records')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_attendance_student_id' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_attendance_student_id' AND object_id = OBJECT_ID('attendance_records')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_attendance_student_offering_date' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_attendance_student_offering_date' AND object_id = OBJECT_ID('attendance_records')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_results_offering_publish_type' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_results_offering_publish_type' AND object_id = OBJECT_ID('results')) THEN 1 ELSE 0 END AS [Value];

SELECT 'IndexExists_IX_results_student_publish_type' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_results_student_publish_type' AND object_id = OBJECT_ID('results')) THEN 1 ELSE 0 END AS [Value];

SELECT 'ResultComponentRulesInstitutionTypeColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('result_component_rules', 'InstitutionType') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'IndexExists_IX_result_component_rules_institution_name' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_result_component_rules_institution_name' AND object_id = OBJECT_ID('result_component_rules')) THEN 1 ELSE 0 END AS [Value];

SELECT 'GpaScaleRulesInstitutionTypeColumnExists' AS [CheckName],
	CASE WHEN COL_LENGTH('gpa_scale_rules', 'InstitutionType') IS NULL THEN 0 ELSE 1 END AS [Value];

SELECT 'IndexExists_IX_gpa_scale_rules_institution_minimum_score' AS [CheckName],
	CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_gpa_scale_rules_institution_minimum_score' AND object_id = OBJECT_ID('gpa_scale_rules')) THEN 1 ELSE 0 END AS [Value];

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

SELECT 'TenantCampusPairInvalidCount_Departments' AS [CheckName],
	COUNT(1) AS [Value]
FROM departments
WHERE (TenantId IS NULL AND CampusId IS NOT NULL)
	 OR (TenantId IS NOT NULL AND CampusId IS NULL);

SELECT 'TenantCampusPairInvalidCount_Users' AS [CheckName],
	COUNT(1) AS [Value]
FROM users
WHERE (TenantId IS NULL AND CampusId IS NOT NULL)
	 OR (TenantId IS NOT NULL AND CampusId IS NULL);

SELECT 'NonSuperAdminUsersWithTenantScopeCount' AS [CheckName],
	COUNT(1) AS [Value]
FROM users u
INNER JOIN roles r ON r.Id = u.RoleId
WHERE r.[Name] <> N'SuperAdmin'
	AND u.[IsDeleted] = 0
	AND u.[TenantId] IS NOT NULL
	AND u.[CampusId] IS NOT NULL;

SELECT 'SuperAdminUsersScopedCount' AS [CheckName],
	COUNT(1) AS [Value]
FROM users u
INNER JOIN roles r ON r.Id = u.RoleId
WHERE r.[Name] = N'SuperAdmin'
	AND (u.[TenantId] IS NOT NULL OR u.[CampusId] IS NOT NULL);

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

SELECT 'DummySeed_StudentsFilterDemoProfiles_Total' AS [CheckName], COUNT(1) AS [Value]
FROM student_profiles
WHERE Id IN
(
		CAST('98989898-9898-9898-9898-989898989901' AS UNIQUEIDENTIFIER),
		CAST('98989898-9898-9898-9898-989898989902' AS UNIQUEIDENTIFIER),
		CAST('98989898-9898-9898-9898-989898989903' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_StudentsFilterDemoProfiles_CSDept' AS [CheckName], COUNT(1) AS [Value]
FROM student_profiles
WHERE DepartmentId = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER)
	AND Id IN
	(
			CAST('98989898-9898-9898-9898-989898989901' AS UNIQUEIDENTIFIER),
			CAST('98989898-9898-9898-9898-989898989902' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_StudentsFilterDemoProfiles_BUSDept' AS [CheckName], COUNT(1) AS [Value]
FROM student_profiles
WHERE DepartmentId = CAST('11111111-1111-1111-1111-111111111112' AS UNIQUEIDENTIFIER)
	AND Id = CAST('98989898-9898-9898-9898-989898989903' AS UNIQUEIDENTIFIER);

SELECT 'GraduationApply_InvalidRows_NotFinalSemesterOrFypIncomplete' AS [CheckName], COUNT(1) AS [Value]
FROM graduation_applications ga
INNER JOIN student_profiles sp ON sp.Id = ga.StudentProfileId
INNER JOIN academic_programs ap ON ap.Id = sp.ProgramId
INNER JOIN departments d ON d.Id = sp.DepartmentId
WHERE d.InstitutionType <> 2
	OR sp.Status <> 1
	OR sp.CurrentSemesterNumber < ap.TotalSemesters
	OR NOT EXISTS
	(
		 SELECT 1
		 FROM fyp_projects fp
		 WHERE fp.StudentProfileId = sp.Id
			AND fp.Status = 4
	);

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

SELECT 'DummySeed_DemoDatasetVersionIsV27' AS [CheckName], COUNT(1) AS [Value]
FROM [Tabsan-EduSphere]
WHERE DemoKey = N'DemoDatasetVersion'
	AND DemoValue = N'FullDummyData-v27';

SELECT 'Schema_DiscussionThreads_Phase31ColumnsPresent' AS [CheckName], COUNT(1) AS [Value]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'discussion_threads'
	AND COLUMN_NAME IN (N'ThreadType', N'IssueSubType', N'IsSolved', N'ResolvedBy', N'ResolvedAt', N'TicketNumber', N'IsVisibleToAll');

SELECT 'DummySeed_Discussion_ThreadTypePopulatedCount' AS [CheckName], COUNT(1) AS [Value]
FROM [discussion_threads]
WHERE [IsDeleted] = 0
	AND [ThreadType] IS NOT NULL
	AND [ThreadType] <> N'';

SELECT 'DummySeed_Discussion_TicketNumberPopulatedCount' AS [CheckName], COUNT(1) AS [Value]
FROM [discussion_threads]
WHERE [IsDeleted] = 0
	AND [TicketNumber] IS NOT NULL
	AND [TicketNumber] <> N'';

SELECT 'DummySeed_Discussion_ResolvedThreadCount' AS [CheckName], COUNT(1) AS [Value]
FROM [discussion_threads]
WHERE [IsDeleted] = 0
	AND [IsSolved] = 1;

SELECT 'DummySeed_Discussion_Offering501_ThreadCount' AS [CheckName], COUNT(1) AS [Value]
FROM [discussion_threads]
WHERE [IsDeleted] = 0
	AND [OfferingId] = CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER);

SELECT 'DummySeed_Discussion_Offering504_ThreadCount' AS [CheckName], COUNT(1) AS [Value]
FROM [discussion_threads]
WHERE [IsDeleted] = 0
	AND [OfferingId] = CAST('55555555-5555-5555-5555-555555555504' AS UNIQUEIDENTIFIER);

SELECT 'DummySeed_Discussion_RepliesCount' AS [CheckName], COUNT(1) AS [Value]
FROM [discussion_replies]
WHERE [IsDeleted] = 0
	AND [ThreadId] IN (
		CAST('20202020-2020-2020-2020-202020202001' AS UNIQUEIDENTIFIER),
		CAST('20202020-2020-2020-2020-202020202002' AS UNIQUEIDENTIFIER),
		CAST('20202020-2020-2020-2020-202020202003' AS UNIQUEIDENTIFIER),
		CAST('20202020-2020-2020-2020-202020202004' AS UNIQUEIDENTIFIER),
		CAST('20202020-2020-2020-2020-202020202005' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_Announcements_FilterDemoRowsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [course_announcements]
WHERE [Id] IN (
	CAST('A1010101-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER),
	CAST('A1010101-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER),
	CAST('A1010101-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_Announcements_Offering501_ActiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [course_announcements]
WHERE [IsDeleted] = 0
	AND [OfferingId] = CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER)
	AND [Id] IN (
		CAST('A1010101-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER),
		CAST('A1010101-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_Announcements_Offering501_InactiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [course_announcements]
WHERE [IsDeleted] = 1
	AND [OfferingId] = CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER)
	AND [Id] = CAST('A1010101-0000-0000-0000-000000000002' AS UNIQUEIDENTIFIER);

SELECT 'DummySeed_Announcements_Offering502_ActiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [course_announcements]
WHERE [IsDeleted] = 0
	AND [OfferingId] = CAST('55555555-5555-5555-5555-555555555502' AS UNIQUEIDENTIFIER)
	AND [Id] = CAST('A1010101-0000-0000-0000-000000000003' AS UNIQUEIDENTIFIER);

SELECT 'DummySeed_AttendanceFilterDemo_StudentProfilesCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [Id] IN (
	CAST('94949494-9494-9494-9494-949494949701' AS UNIQUEIDENTIFIER),
	CAST('94949494-9494-9494-9494-949494949702' AS UNIQUEIDENTIFIER),
	CAST('94949494-9494-9494-9494-949494949703' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_AttendanceFilterDemo_Enrollments_DS101' AS [CheckName], COUNT(1) AS [Value]
FROM [enrollments]
WHERE [CourseOfferingId] = CAST('66666666-6666-6666-6666-666666666661' AS UNIQUEIDENTIFIER)
	AND [StudentProfileId] IN (
		CAST('94949494-9494-9494-9494-949494949701' AS UNIQUEIDENTIFIER),
		CAST('94949494-9494-9494-9494-949494949702' AS UNIQUEIDENTIFIER),
		CAST('94949494-9494-9494-9494-949494949703' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_AttendanceFilterDemo_Enrollments_DB201' AS [CheckName], COUNT(1) AS [Value]
FROM [enrollments]
WHERE [CourseOfferingId] = CAST('66666666-6666-6666-6666-666666666662' AS UNIQUEIDENTIFIER)
	AND [StudentProfileId] IN (
		CAST('94949494-9494-9494-9494-949494949701' AS UNIQUEIDENTIFIER),
		CAST('94949494-9494-9494-9494-949494949702' AS UNIQUEIDENTIFIER),
		CAST('94949494-9494-9494-9494-949494949703' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_EnrollmentsFilterDemo_StudentProfilesCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [Id] IN (
	CAST('94949494-9494-9494-9494-949494949701' AS UNIQUEIDENTIFIER),
	CAST('94949494-9494-9494-9494-949494949702' AS UNIQUEIDENTIFIER),
	CAST('94949494-9494-9494-9494-949494949703' AS UNIQUEIDENTIFIER),
	CAST('94949494-9494-9494-9494-949494949704' AS UNIQUEIDENTIFIER),
	CAST('94949494-9494-9494-9494-949494949705' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_EnrollmentsFilterDemo_DS101_ActiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [enrollments]
WHERE [CourseOfferingId] = CAST('66666666-6666-6666-6666-666666666661' AS UNIQUEIDENTIFIER)
	AND [Status] = N'Active'
	AND [DroppedAt] IS NULL
	AND [Id] IN (
		CAST('95959595-9595-9595-9595-959595959711' AS UNIQUEIDENTIFIER),
		CAST('95959595-9595-9595-9595-959595959712' AS UNIQUEIDENTIFIER),
		CAST('95959595-9595-9595-9595-959595959713' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_EnrollmentsFilterDemo_DB201_ActiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [enrollments]
WHERE [CourseOfferingId] = CAST('66666666-6666-6666-6666-666666666662' AS UNIQUEIDENTIFIER)
	AND [Status] = N'Active'
	AND [DroppedAt] IS NULL
	AND [Id] IN (
		CAST('95959595-9595-9595-9595-959595959714' AS UNIQUEIDENTIFIER),
		CAST('95959595-9595-9595-9595-959595959715' AS UNIQUEIDENTIFIER),
		CAST('95959595-9595-9595-9595-959595959716' AS UNIQUEIDENTIFIER),
		CAST('95959595-9595-9595-9595-959595959717' AS UNIQUEIDENTIFIER),
		CAST('95959595-9595-9595-9595-959595959718' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_AttendanceFilterDemo_Attendance_DS101' AS [CheckName], COUNT(1) AS [Value]
FROM [attendance_records]
WHERE [CourseOfferingId] = CAST('66666666-6666-6666-6666-666666666661' AS UNIQUEIDENTIFIER)
	AND [Id] IN (
		CAST('96969696-9696-9696-9696-969696969801' AS UNIQUEIDENTIFIER),
		CAST('96969696-9696-9696-9696-969696969802' AS UNIQUEIDENTIFIER),
		CAST('96969696-9696-9696-9696-969696969803' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_AttendanceFilterDemo_Attendance_DB201' AS [CheckName], COUNT(1) AS [Value]
FROM [attendance_records]
WHERE [CourseOfferingId] = CAST('66666666-6666-6666-6666-666666666662' AS UNIQUEIDENTIFIER)
	AND [Id] IN (
		CAST('96969696-9696-9696-9696-969696969804' AS UNIQUEIDENTIFIER),
		CAST('96969696-9696-9696-9696-969696969805' AS UNIQUEIDENTIFIER),
		CAST('96969696-9696-9696-9696-969696969806' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_StudentTimetableDemo_TimetablesCount' AS [CheckName], COUNT(1) AS [Value]
FROM [timetables]
WHERE [Id] IN (
	CAST('25252525-2525-2525-2525-252525252901' AS UNIQUEIDENTIFIER),
	CAST('25252525-2525-2525-2525-252525252902' AS UNIQUEIDENTIFIER),
	CAST('25252525-2525-2525-2525-252525252903' AS UNIQUEIDENTIFIER)
)
	AND [DepartmentId] = CAST('33333333-3333-3333-3333-333333333333' AS UNIQUEIDENTIFIER)
	AND [IsPublished] = 1;

SELECT 'DummySeed_StudentTimetableDemo_EntriesCount' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [Id] IN (
	CAST('26262626-2626-2626-2626-262626262901' AS UNIQUEIDENTIFIER),
	CAST('26262626-2626-2626-2626-262626262902' AS UNIQUEIDENTIFIER),
	CAST('26262626-2626-2626-2626-262626262903' AS UNIQUEIDENTIFIER),
	CAST('26262626-2626-2626-2626-262626262904' AS UNIQUEIDENTIFIER),
	CAST('26262626-2626-2626-2626-262626262905' AS UNIQUEIDENTIFIER),
	CAST('26262626-2626-2626-2626-262626262906' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_StudentTimetableDemo_MondayRows_Timetable901' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [TimetableId] = CAST('25252525-2525-2525-2525-252525252901' AS UNIQUEIDENTIFIER)
	AND [DayOfWeek] = 1;

SELECT 'DummySeed_StudentTimetableDemo_WednesdayRows_Timetable901' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [TimetableId] = CAST('25252525-2525-2525-2525-252525252901' AS UNIQUEIDENTIFIER)
	AND [DayOfWeek] = 3;

SELECT 'DummySeed_StudentTimetableDemo_ThursdayRows_Timetable902' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [TimetableId] = CAST('25252525-2525-2525-2525-252525252902' AS UNIQUEIDENTIFIER)
	AND [DayOfWeek] = 4;

SELECT 'DummySeed_StudentTimetableDemo_TuesdayRows_Timetable903' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [TimetableId] = CAST('25252525-2525-2525-2525-252525252903' AS UNIQUEIDENTIFIER)
	AND [DayOfWeek] = 2;

SELECT 'DummySeed_StudentTimetableDemo_FridayRows_Timetable903' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [TimetableId] = CAST('25252525-2525-2525-2525-252525252903' AS UNIQUEIDENTIFIER)
	AND [DayOfWeek] = 5;

SELECT 'DummySeed_StudentTimetableDemo_SaturdayRows_Timetable903' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [TimetableId] = CAST('25252525-2525-2525-2525-252525252903' AS UNIQUEIDENTIFIER)
	AND [DayOfWeek] = 6;

SELECT 'DummySeed_Enrollments_ActiveForGradebook' AS [CheckName], COUNT(1) AS [Value]
FROM [enrollments]
WHERE [Status] = N'Active';

IF COL_LENGTH('result_component_rules', 'InstitutionType') IS NOT NULL
BEGIN
	EXEC(N'
		SELECT ''DummySeed_ResultComponentRules_UniversityCount'' AS [CheckName], COUNT(1) AS [Value]
		FROM [result_component_rules]
		WHERE [InstitutionType] = 0
			AND [IsActive] = 1;

		SELECT ''DummySeed_ResultComponentRules_SchoolCount'' AS [CheckName], COUNT(1) AS [Value]
		FROM [result_component_rules]
		WHERE [InstitutionType] = 1
			AND [IsActive] = 1;

		SELECT ''DummySeed_ResultComponentRules_CollegeCount'' AS [CheckName], COUNT(1) AS [Value]
		FROM [result_component_rules]
		WHERE [InstitutionType] = 2
			AND [IsActive] = 1;
	');
END
ELSE
BEGIN
	SELECT 'DummySeed_ResultComponentRules_UniversityCount' AS [CheckName], CAST(-1 AS INT) AS [Value];
	SELECT 'DummySeed_ResultComponentRules_SchoolCount' AS [CheckName], CAST(-1 AS INT) AS [Value];
	SELECT 'DummySeed_ResultComponentRules_CollegeCount' AS [CheckName], CAST(-1 AS INT) AS [Value];
END;

SELECT 'DummySeed_ResultRows_SchoolClassTestCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555517' AS UNIQUEIDENTIFIER)
	AND [ResultType] = N'ClassTest';

SELECT 'DummySeed_ResultRows_CollegeSessionalCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555516' AS UNIQUEIDENTIFIER)
	AND [ResultType] = N'Sessional';

SELECT 'DummySeed_RubricManage_Offering513_AssignmentCount' AS [CheckName], COUNT(1) AS [Value]
FROM [assignments]
WHERE [CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555513' AS UNIQUEIDENTIFIER)
	AND [IsDeleted] = 0;

SELECT 'DummySeed_RubricManage_Offering513_RubricCount' AS [CheckName], COUNT(1) AS [Value]
FROM [rubrics] r
INNER JOIN [assignments] a ON a.[Id] = r.[AssignmentId]
WHERE a.[CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555513' AS UNIQUEIDENTIFIER)
	AND r.[IsActive] = 1
	AND r.[IsDeleted] = 0;

SELECT 'DummySeed_RubricManage_Offering513_SubmissionCount' AS [CheckName], COUNT(1) AS [Value]
FROM [assignment_submissions] s
INNER JOIN [assignments] a ON a.[Id] = s.[AssignmentId]
WHERE a.[CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555513' AS UNIQUEIDENTIFIER)
    AND s.[Status] = N'Graded';

SELECT 'DummySeed_ResultRows_Offering513_PracticalCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555513' AS UNIQUEIDENTIFIER)
    AND [ResultType] = N'Practical';

SELECT 'DummySeed_ResultRows_InternalDemoRowCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [Id] IN
(
	CAST('cccccccc-cccc-cccc-cccc-cccccccccc40' AS UNIQUEIDENTIFIER),
	CAST('cccccccc-cccc-cccc-cccc-cccccccccc41' AS UNIQUEIDENTIFIER),
	CAST('cccccccc-cccc-cccc-cccc-cccccccccc42' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_ResultRows_Offering501_InternalCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER)
	AND [ResultType] = N'Internal';

SELECT 'DummySeed_ResultOffering501_ScopeAligned' AS [CheckName], COUNT(1) AS [Value]
FROM [course_offerings]
WHERE [Id] = CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER)
	AND [TenantId] = CAST('f1000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER)
	AND [CampusId] = CAST('f2000000-0000-0000-0000-000000000001' AS UNIQUEIDENTIFIER)
	AND [InstitutionType] = 2;

SELECT 'DummySeed_QuizRows_FilterDemoByIdCount' AS [CheckName], COUNT(1) AS [Value]
FROM [quizzes]
WHERE [Id] IN
(
	CAST('13131313-1313-1313-1313-131313131307' AS UNIQUEIDENTIFIER),
	CAST('13131313-1313-1313-1313-131313131308' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_QuizRows_Offering501_ActiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [quizzes]
WHERE [CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER)
	AND [IsActive] = 1;

SELECT 'DummySeed_QuizRows_Offering501_InactiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [quizzes]
WHERE [CourseOfferingId] = CAST('55555555-5555-5555-5555-555555555501' AS UNIQUEIDENTIFIER)
	AND [IsActive] = 0;

SELECT 'DummySeed_StudentLifecycleFilterDemo_ProfileCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [Id] IN
(
	CAST('98989898-9898-9898-9898-989898989811' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989812' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989813' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_StudentLifecycleFilterDemo_CSDeptCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [DepartmentId] = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER)
	AND [Id] IN
	(
		CAST('98989898-9898-9898-9898-989898989811' AS UNIQUEIDENTIFIER),
		CAST('98989898-9898-9898-9898-989898989812' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_StudentLifecycleFilterDemo_EngineeringDeptCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [DepartmentId] = CAST('11111111-1111-1111-1111-111111111113' AS UNIQUEIDENTIFIER)
	AND [Id] = CAST('98989898-9898-9898-9898-989898989813' AS UNIQUEIDENTIFIER);

SELECT 'DummySeed_StudentLifecycleFilterDemo_StatusActiveCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [Status] = 1
	AND [Id] IN
	(
		CAST('98989898-9898-9898-9898-989898989811' AS UNIQUEIDENTIFIER),
		CAST('98989898-9898-9898-9898-989898989812' AS UNIQUEIDENTIFIER),
		CAST('98989898-9898-9898-9898-989898989813' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_StudentLifecycleFilterDemo_GraduationCandidateCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles] sp
INNER JOIN [academic_programs] ap ON ap.[Id] = sp.[ProgramId]
WHERE sp.[Id] IN
(
	CAST('98989898-9898-9898-9898-989898989811' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989812' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989813' AS UNIQUEIDENTIFIER)
)
	AND sp.[Status] = 1
	AND sp.[CurrentSemesterNumber] >= ap.[TotalSemesters];

SELECT 'DummySeed_StudentLifecycleFilterDemo_Semester1Count' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [Id] IN
(
	CAST('98989898-9898-9898-9898-989898989811' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989812' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989813' AS UNIQUEIDENTIFIER)
)
	AND [CurrentSemesterNumber] = 1;

SELECT 'DummySeed_StudentLifecycleFilterDemo_Semester2Count' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [Id] IN
(
	CAST('98989898-9898-9898-9898-989898989811' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989812' AS UNIQUEIDENTIFIER),
	CAST('98989898-9898-9898-9898-989898989813' AS UNIQUEIDENTIFIER)
)
	AND [CurrentSemesterNumber] = 2;

SELECT 'StudentLifecycle_LegacyStatus0Count' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [Status] = 0;

SELECT 'DummySeed_LmsManage_Offering513_ModuleCount' AS [CheckName], COUNT(1) AS [Value]
FROM [course_content_modules]
WHERE [OfferingId] = CAST('55555555-5555-5555-5555-555555555513' AS UNIQUEIDENTIFIER)
	AND [IsDeleted] = 0;

SELECT 'DummySeed_LmsManage_Offering513_DraftModuleCount' AS [CheckName], COUNT(1) AS [Value]
FROM [course_content_modules]
WHERE [OfferingId] = CAST('55555555-5555-5555-5555-555555555513' AS UNIQUEIDENTIFIER)
	AND [WeekNumber] = 5
	AND [IsPublished] = 0
	AND [IsDeleted] = 0;

SELECT 'DummySeed_LmsManage_Week6VideoCount' AS [CheckName], COUNT(1) AS [Value]
FROM [content_videos]
WHERE [ModuleId] = CAST('58585858-5858-5858-5858-585858585852' AS UNIQUEIDENTIFIER)
	AND [IsDeleted] = 0;

SELECT 'DummySeed_ResultModificationRequests_ResultEntrySectionCount' AS [CheckName], COUNT(1) AS [Value]
FROM [teacher_modification_requests]
WHERE [ProposedData] LIKE N'%"section":"result-entry"%';

SELECT 'DummySeed_InstituteDemoUsers_University' AS [CheckName], COUNT(1) AS [Value]
FROM [users]
WHERE [Username] IN (N'demo.uni.admin', N'demo.uni.faculty', N'demo.uni.student')
	AND [InstitutionType] = 2;

SELECT 'DummySeed_InstituteDemoUsers_College' AS [CheckName], COUNT(1) AS [Value]
FROM [users]
WHERE [Username] IN (N'demo.col.admin', N'demo.col.faculty', N'demo.col.student')
	AND [InstitutionType] = 1;

SELECT 'DummySeed_InstituteDemoUsers_School' AS [CheckName], COUNT(1) AS [Value]
FROM [users]
WHERE [Username] IN (N'demo.sch.admin', N'demo.sch.faculty', N'demo.sch.student')
	AND [InstitutionType] = 0;

SELECT 'DummySeed_InstituteDemoProfiles_CoverageCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [RegistrationNumber] IN (N'DEMO-UNI-0001', N'DEMO-COL-0001', N'DEMO-SCH-0001');

SELECT 'SidebarMenu_GenerateCertificates_Count' AS [CheckName], COUNT(1) AS [Value]
FROM [sidebar_menu_items]
WHERE [Key] = N'generate_certificates';

SELECT 'SidebarMenu_CourseMaterial_Count' AS [CheckName], COUNT(1) AS [Value]
FROM [sidebar_menu_items]
WHERE [Key] = N'course_material';

SELECT 'SidebarMenu_TeacherTimetable_AdminAllowed_Count' AS [CheckName], COUNT(1) AS [Value]
FROM [sidebar_menu_role_accesses] sra
INNER JOIN [sidebar_menu_items] smi ON smi.[Id] = sra.[SidebarMenuItemId]
WHERE smi.[Key] = N'timetable_teacher'
	AND sra.[RoleName] = N'Admin'
	AND sra.[IsAllowed] = 1;

SELECT 'DummySeed_AllInstitutionTypes_HaveTimetableCoverage' AS [CheckName], COUNT(1) AS [Value]
FROM
(
	SELECT d.[InstitutionType]
	FROM [timetables] t
	INNER JOIN [departments] d ON d.[Id] = t.[DepartmentId]
	GROUP BY d.[InstitutionType]
) x;

SELECT 'DummySeed_AllInstitutionTypes_HavePaymentCoverage' AS [CheckName], COUNT(1) AS [Value]
FROM
(
	SELECT u.[InstitutionType]
	FROM [payment_receipts] pr
	INNER JOIN [student_profiles] sp ON sp.[Id] = pr.[StudentProfileId]
	INNER JOIN [users] u ON u.[Id] = sp.[UserId]
	GROUP BY u.[InstitutionType]
) x;

SELECT 'DummySeed_CourseOfferingsDistinctSemesterCount' AS [CheckName], COUNT(DISTINCT co.[SemesterId]) AS [Value]
FROM [course_offerings] co;

SELECT 'DummySeed_SemesterCoverage_AllSemestersHaveOfferings' AS [CheckName],
	COUNT(1) AS [Value]
FROM [semesters] s
WHERE EXISTS (SELECT 1 FROM [course_offerings] co WHERE co.[SemesterId] = s.[Id]);

SELECT 'DummySeed_SchoolClassSemesterCoverageCount' AS [CheckName],
	COUNT(DISTINCT t.[SemesterId]) AS [Value]
FROM [timetables] t
INNER JOIN [departments] d ON d.[Id] = t.[DepartmentId]
WHERE d.[InstitutionType] = 0;

SELECT 'DummySeed_CollegeClassSemesterCoverageCount' AS [CheckName],
	COUNT(DISTINCT t.[SemesterId]) AS [Value]
FROM [timetables] t
INNER JOIN [departments] d ON d.[Id] = t.[DepartmentId]
WHERE d.[InstitutionType] = 1;

SELECT 'DummySeed_SchoolClassEntryCount' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries] te
WHERE te.[SubjectName] LIKE N'Class %';

SELECT 'DummySeed_CollegeClassEntryCount' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries] te
WHERE te.[SubjectName] LIKE N'College Semester %';

SELECT 'DummySeed_TeacherTimetableFilterDemoCount' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries] te
WHERE te.[Id] IN
(
		CAST('26262626-2626-2626-2626-262626262804' AS UNIQUEIDENTIFIER),
		CAST('26262626-2626-2626-2626-262626262805' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_TargetSchoolScienceClassCount' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries] te
INNER JOIN [timetables] t ON t.[Id] = te.[TimetableId]
WHERE t.[AcademicProgramId] = CAST('22222222-2222-2222-2222-222222222432' AS UNIQUEIDENTIFIER)
	AND te.[SubjectName] LIKE N'Class [0-9]% - %';

SELECT 'DummySeed_TargetCollegeIcsClassCount' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries] te
INNER JOIN [timetables] t ON t.[Id] = te.[TimetableId]
WHERE t.[AcademicProgramId] = CAST('22222222-2222-2222-2222-222222222325' AS UNIQUEIDENTIFIER)
	AND te.[SubjectName] LIKE N'College Semester [0-9]% - %';

SELECT 'DummySeed_ExactSchoolScienceClassEntries_10' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [Id] BETWEEN CAST('26262626-2626-2626-2626-262626262701' AS UNIQUEIDENTIFIER)
								AND CAST('26262626-2626-2626-2626-262626262710' AS UNIQUEIDENTIFIER);

SELECT 'DummySeed_ExactCollegeIcsClassEntries_2' AS [CheckName], COUNT(1) AS [Value]
FROM [timetable_entries]
WHERE [Id] IN
(
		CAST('26262626-2626-2626-2626-262626262711' AS UNIQUEIDENTIFIER),
		CAST('26262626-2626-2626-2626-262626262712' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_TargetUniversityFocusCoursesDistinctCount' AS [CheckName], COUNT(DISTINCT co.[CourseId]) AS [Value]
FROM [course_offerings] co
WHERE co.[CourseId] IN
(
		CAST('44444444-4444-4444-4444-444444444401' AS UNIQUEIDENTIFIER), -- BSCS focus
		CAST('44444444-4444-4444-4444-444444444402' AS UNIQUEIDENTIFIER), -- MCS focus
		CAST('44444444-4444-4444-4444-444444444408' AS UNIQUEIDENTIFIER), -- BBA focus
		CAST('44444444-4444-4444-4444-444444444437' AS UNIQUEIDENTIFIER)  -- Language learning focus
);

SELECT 'DummySeed_TargetUniversityFocusCourseSemesterCoverage' AS [CheckName],
			 COUNT(1) AS [Value]
FROM
(
		SELECT co.[CourseId], COUNT(DISTINCT co.[SemesterId]) AS SemCount
		FROM [course_offerings] co
		WHERE co.[CourseId] IN
		(
				CAST('44444444-4444-4444-4444-444444444401' AS UNIQUEIDENTIFIER),
				CAST('44444444-4444-4444-4444-444444444402' AS UNIQUEIDENTIFIER),
				CAST('44444444-4444-4444-4444-444444444408' AS UNIQUEIDENTIFIER),
				CAST('44444444-4444-4444-4444-444444444437' AS UNIQUEIDENTIFIER)
		)
		GROUP BY co.[CourseId]
) fc
WHERE fc.[SemCount] = (SELECT COUNT(1) FROM [semesters]);

SELECT 'DummySeed_DepartmentsMissingAdminUser' AS [CheckName], COUNT(1) AS [Value]
FROM [departments] d
WHERE NOT EXISTS
(
	SELECT 1
	FROM [users] u
	WHERE u.[DepartmentId] = d.[Id]
	  AND u.[RoleId] = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Admin')
	  AND u.[IsDeleted] = 0
);

SELECT 'DummySeed_DepartmentsMissingFacultyUser' AS [CheckName], COUNT(1) AS [Value]
FROM [departments] d
WHERE NOT EXISTS
(
	SELECT 1
	FROM [users] u
	WHERE u.[DepartmentId] = d.[Id]
	  AND u.[RoleId] = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Faculty')
	  AND u.[IsDeleted] = 0
);

SELECT 'DummySeed_DepartmentsMissingFinanceUser' AS [CheckName], COUNT(1) AS [Value]
FROM [departments] d
WHERE NOT EXISTS
(
	SELECT 1
	FROM [users] u
	WHERE u.[DepartmentId] = d.[Id]
	  AND u.[RoleId] = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Finance')
	  AND u.[IsDeleted] = 0
);

SELECT 'DummySeed_DepartmentsMissingStudentUser' AS [CheckName], COUNT(1) AS [Value]
FROM [departments] d
WHERE NOT EXISTS
(
	SELECT 1
	FROM [users] u
	WHERE u.[DepartmentId] = d.[Id]
	  AND u.[RoleId] = (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'Student')
	  AND u.[IsDeleted] = 0
);

SELECT 'DummySeed_BulkUsersCount' AS [CheckName], COUNT(1) AS [Value]
FROM [users]
WHERE [Username] LIKE N'bulk.%';

SELECT 'DummySeed_BulkStudentProfilesCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles] sp
INNER JOIN [users] u ON u.[Id] = sp.[UserId]
WHERE u.[Username] LIKE N'bulk.%.student.%';

SELECT 'DummySeed_BulkEnrollmentsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [enrollments] e
INNER JOIN [student_profiles] sp ON sp.[Id] = e.[StudentProfileId]
INNER JOIN [users] u ON u.[Id] = sp.[UserId]
WHERE u.[Username] LIKE N'bulk.%.student.%';

SELECT 'DummySeed_BulkAttendanceCount' AS [CheckName], COUNT(1) AS [Value]
FROM [attendance_records] ar
INNER JOIN [student_profiles] sp ON sp.[Id] = ar.[StudentProfileId]
INNER JOIN [users] u ON u.[Id] = sp.[UserId]
WHERE u.[Username] LIKE N'bulk.%.student.%';

SELECT 'DummySeed_AttendanceStatusPresentCount' AS [CheckName], COUNT(1) AS [Value]
FROM [attendance_records]
WHERE [Status] = N'Present';

SELECT 'DummySeed_AttendanceStatusAbsentCount' AS [CheckName], COUNT(1) AS [Value]
FROM [attendance_records]
WHERE [Status] = N'Absent';

SELECT 'DummySeed_AttendanceStatusLateCount' AS [CheckName], COUNT(1) AS [Value]
FROM [attendance_records]
WHERE [Status] = N'Late';

SELECT 'DummySeed_BulkResultsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results] r
INNER JOIN [student_profiles] sp ON sp.[Id] = r.[StudentProfileId]
INNER JOIN [users] u ON u.[Id] = sp.[UserId]
WHERE u.[Username] LIKE N'bulk.%.student.%';

SELECT 'DummySeed_ResultsPublishedCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [IsPublished] = 1;

SELECT 'DummySeed_ResultsDraftCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [IsPublished] = 0;

SELECT 'DummySeed_ResultsMidtermCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [ResultType] = N'Midterm';

SELECT 'DummySeed_ResultsFinalReviewCount' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [ResultType] = N'FinalReview';

SELECT 'DummySeed_AttendanceInstitutionCoverageCount' AS [CheckName], COUNT(DISTINCT d.[InstitutionType]) AS [Value]
FROM [attendance_records] ar
INNER JOIN [course_offerings] co ON co.[Id] = ar.[CourseOfferingId]
INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId];

SELECT 'DummySeed_ResultsInstitutionCoverageCount' AS [CheckName], COUNT(DISTINCT d.[InstitutionType]) AS [Value]
FROM [results] r
INNER JOIN [course_offerings] co ON co.[Id] = r.[CourseOfferingId]
INNER JOIN [courses] c ON c.[Id] = co.[CourseId]
INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId];

SELECT 'DummySeed_BulkPaymentReceiptsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [payment_receipts] pr
INNER JOIN [student_profiles] sp ON sp.[Id] = pr.[StudentProfileId]
INNER JOIN [users] u ON u.[Id] = sp.[UserId]
WHERE u.[Username] LIKE N'bulk.%.student.%';

SELECT 'DummySeed_SemesterCyclePaymentReceiptCount' AS [CheckName], COUNT(1) AS [Value]
FROM [payment_receipts]
WHERE [Description] LIKE N'Semester Fee - %';

SELECT 'DummySeed_FypPanelLegacyRoleRowsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [fyp_panel_members]
WHERE [Role] IN (N'Internal', N'External');

SELECT 'DummySeed_FypFilterDemoRows_ByIdCount' AS [CheckName], COUNT(1) AS [Value]
FROM [fyp_projects]
WHERE [Id] IN
(
		CAST('61616161-6161-6161-6161-616161616101' AS UNIQUEIDENTIFIER),
		CAST('61616161-6161-6161-6161-616161616102' AS UNIQUEIDENTIFIER),
		CAST('61616161-6161-6161-6161-616161616103' AS UNIQUEIDENTIFIER)
);

SELECT 'DummySeed_FypFilterDemo_CS_DepartmentCount' AS [CheckName], COUNT(1) AS [Value]
FROM [fyp_projects]
WHERE [DepartmentId] = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER)
	AND [Id] IN
	(
		CAST('61616161-6161-6161-6161-616161616101' AS UNIQUEIDENTIFIER),
		CAST('61616161-6161-6161-6161-616161616102' AS UNIQUEIDENTIFIER)
	);

SELECT 'DummySeed_FypFilterDemo_Engineering_DepartmentCount' AS [CheckName], COUNT(1) AS [Value]
FROM [fyp_projects]
WHERE [DepartmentId] = CAST('11111111-1111-1111-1111-111111111113' AS UNIQUEIDENTIFIER)
	AND [Id] = CAST('61616161-6161-6161-6161-616161616103' AS UNIQUEIDENTIFIER);

SELECT 'DummySeed_FypFilterDemo_UpcomingMeetingCount' AS [CheckName], COUNT(1) AS [Value]
FROM [fyp_meetings]
WHERE [Id] = CAST('62626262-6262-6262-6262-626262626201' AS UNIQUEIDENTIFIER)
	AND [Status] = N'Scheduled';

SELECT 'Lifecycle_School_Class1To10_ProfileCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [RegistrationNumber] = N'LCYC-SCH-001'
	AND [CurrentSemesterNumber] >= 10;

SELECT 'Lifecycle_School_Class1To10_ReportCardsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_report_cards]
WHERE [StudentProfileId] = CAST('99991111-2222-3333-4444-000000000001' AS UNIQUEIDENTIFIER)
	AND [PeriodLabel] IN
	(
			N'Class 1', N'Class 2', N'Class 3', N'Class 4', N'Class 5',
			N'Class 6', N'Class 7', N'Class 8', N'Class 9', N'Class 10'
	);

SELECT 'Lifecycle_College_Class11To12_ProfileCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [RegistrationNumber] = N'LCYC-COL-001'
	AND [CurrentSemesterNumber] >= 12;

SELECT 'Lifecycle_College_Class11To12_ReportCardsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_report_cards]
WHERE [StudentProfileId] = CAST('99991111-2222-3333-4444-000000000002' AS UNIQUEIDENTIFIER)
	AND [PeriodLabel] IN (N'Class 11', N'Class 12');

SELECT 'Lifecycle_University_Sem1To8_ProfileCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_profiles]
WHERE [RegistrationNumber] = N'LCYC-UNI-001'
	AND [CurrentSemesterNumber] >= 8;

SELECT 'Lifecycle_University_Sem1To8_ReportCardsCount' AS [CheckName], COUNT(1) AS [Value]
FROM [student_report_cards]
WHERE [StudentProfileId] = CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER)
	AND [PeriodLabel] IN
	(
			N'Semester 1', N'Semester 2', N'Semester 3', N'Semester 4',
			N'Semester 5', N'Semester 6', N'Semester 7', N'Semester 8'
	);

SELECT 'Lifecycle_AttendanceRows_3Profiles' AS [CheckName], COUNT(1) AS [Value]
FROM [attendance_records]
WHERE [StudentProfileId] IN
(
		CAST('99991111-2222-3333-4444-000000000001' AS UNIQUEIDENTIFIER),
		CAST('99991111-2222-3333-4444-000000000002' AS UNIQUEIDENTIFIER),
		CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER)
);

SELECT 'Lifecycle_FinalResultsRows_3Profiles' AS [CheckName], COUNT(1) AS [Value]
FROM [results]
WHERE [StudentProfileId] IN
(
		CAST('99991111-2222-3333-4444-000000000001' AS UNIQUEIDENTIFIER),
		CAST('99991111-2222-3333-4444-000000000002' AS UNIQUEIDENTIFIER),
		CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER)
)
AND [ResultType] = N'Final';

SELECT 'Lifecycle_AssignmentSubmissionsRows_3Profiles' AS [CheckName], COUNT(1) AS [Value]
FROM [assignment_submissions]
WHERE [StudentProfileId] IN
(
		CAST('99991111-2222-3333-4444-000000000001' AS UNIQUEIDENTIFIER),
		CAST('99991111-2222-3333-4444-000000000002' AS UNIQUEIDENTIFIER),
		CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER)
);

SELECT 'Lifecycle_University_FypCompleteCount' AS [CheckName], COUNT(1) AS [Value]
FROM [fyp_projects]
WHERE [StudentProfileId] = CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER)
	AND
	(
			TRY_CONVERT(INT, [Status]) = 4
			OR CONVERT(NVARCHAR(50), [Status]) IN (N'Completed', N'Approved')
	);

SELECT 'Lifecycle_University_GraduationApprovedCount' AS [CheckName], COUNT(1) AS [Value]
FROM [graduation_applications]
WHERE [StudentProfileId] = CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER)
	AND [Status] = 2;

SELECT 'Lifecycle_University_GraduationApprovalStagesCount' AS [CheckName], COUNT(DISTINCT [Stage]) AS [Value]
FROM [graduation_application_approvals]
WHERE [GraduationApplicationId] IN
(
		SELECT [Id]
		FROM [graduation_applications]
		WHERE [StudentProfileId] = CAST('99991111-2222-3333-4444-000000000003' AS UNIQUEIDENTIFIER)
);

SELECT TOP 20 [MigrationId], [ProductVersion]
FROM __EFMigrationsHistory
ORDER BY [MigrationId] DESC;

IF OBJECT_ID(N'[Tabsan-EduSphere]') IS NOT NULL
BEGIN
		SELECT 'DummySeed_DemoDatasetVersion_v27' AS [CheckName], COUNT(1) AS [Value]
		FROM [Tabsan-EduSphere]
		WHERE [DemoKey] = N'DemoDatasetVersion'
			AND [DemoValue] = N'FullDummyData-v27';
END;

PRINT 'Post-deployment checks completed.';
