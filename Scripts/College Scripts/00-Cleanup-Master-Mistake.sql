-- ============================================================
-- CLEANUP: Remove EduSphere objects accidentally created in master
-- Run this against the MASTER database ONLY.
-- After running, execute 01-Schema-Current.sql from master to
-- create [Tabsan-EduSphere] properly.
-- ============================================================

USE master;
GO

PRINT 'Starting cleanup of EduSphere objects from master database...';
GO

-- ------------------------------------------------------------
-- Drop Views
-- ------------------------------------------------------------
DROP VIEW IF EXISTS [vw_course_enrollment_summary];
DROP VIEW IF EXISTS [vw_student_results_summary];
DROP VIEW IF EXISTS [vw_student_attendance_summary];
GO

-- ------------------------------------------------------------
-- Drop Stored Procedures
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS [sp_get_attendance_below_threshold];
DROP PROCEDURE IF EXISTS [sp_recalculate_student_cgpa];
GO

-- ------------------------------------------------------------
-- Drop Foreign Keys that block table drops
-- ------------------------------------------------------------
DECLARE @TargetTables TABLE (Name sysname PRIMARY KEY);

INSERT INTO @TargetTables (Name)
VALUES
	(N'__EFMigrationsHistory'),
	(N'academic_deadlines'),
	(N'academic_programs'),
	(N'accreditation_templates'),
	(N'admin_change_requests'),
	(N'admin_department_assignments'),
	(N'assignment_submissions'),
	(N'assignments'),
	(N'attendance_records'),
	(N'audit_logs'),
	(N'buildings'),
	(N'bulk_promotion_batches'),
	(N'bulk_promotion_entries'),
	(N'chat_conversations'),
	(N'chat_messages'),
	(N'consumed_verification_keys'),
	(N'content_videos'),
	(N'course_announcements'),
	(N'course_content_modules'),
	(N'course_grading_configs'),
	(N'course_offerings'),
	(N'course_prerequisites'),
	(N'courses'),
	(N'degree_rule_required_courses'),
	(N'degree_rules'),
	(N'departments'),
	(N'discussion_replies'),
	(N'discussion_threads'),
	(N'enrollments'),
	(N'faculty_department_assignments'),
	(N'fyp_meetings'),
	(N'fyp_panel_members'),
	(N'fyp_projects'),
	(N'gpa_scale_rules'),
	(N'graduation_application_approvals'),
	(N'graduation_applications'),
	(N'institution_grading_profiles'),
	(N'license_state'),
	(N'module_role_assignments'),
	(N'module_status'),
	(N'modules'),
	(N'notification_recipients'),
	(N'notifications'),
	(N'outbound_email_logs'),
	(N'parent_student_links'),
	(N'password_history'),
	(N'payment_receipts'),
	(N'portal_settings'),
	(N'quiz_answers'),
	(N'quiz_attempts'),
	(N'quiz_options'),
	(N'quiz_questions'),
	(N'quizzes'),
	(N'registration_whitelist'),
	(N'report_definitions'),
	(N'report_role_assignments'),
	(N'result_component_rules'),
	(N'results'),
	(N'roles'),
	(N'rooms'),
	(N'rubric_criteria'),
	(N'rubric_levels'),
	(N'rubric_student_grades'),
	(N'rubrics'),
	(N'school_streams'),
	(N'semesters'),
	(N'sidebar_menu_items'),
	(N'sidebar_menu_role_accesses'),
	(N'student_profiles'),
	(N'student_report_cards'),
	(N'student_stream_assignments'),
	(N'study_plan_courses'),
	(N'study_plans'),
	(N'support_ticket_messages'),
	(N'support_tickets'),
	(N'teacher_modification_requests'),
	(N'timetable_entries'),
	(N'timetables'),
	(N'transcript_export_logs'),
	(N'Tabsan-EduSphere'),
	(N'user_sessions'),
	(N'users');

DECLARE @DropFkSql nvarchar(max) = N'';

SELECT @DropFkSql = @DropFkSql +
	N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(pt.schema_id)) + N'.' + QUOTENAME(pt.name) +
	N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(13) + CHAR(10)
FROM sys.foreign_keys fk
INNER JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
INNER JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
LEFT JOIN @TargetTables t1 ON t1.Name = pt.name
LEFT JOIN @TargetTables t2 ON t2.Name = rt.name
WHERE t1.Name IS NOT NULL OR t2.Name IS NOT NULL;

IF LEN(@DropFkSql) > 0
BEGIN
	EXEC sp_executesql @DropFkSql;
	PRINT 'Dropped foreign key constraints related to EduSphere tables.';
END
ELSE
BEGIN
	PRINT 'No foreign keys found to drop.';
END
GO

-- ------------------------------------------------------------
-- Drop Tables (order respects foreign key dependencies)
-- ------------------------------------------------------------

-- Leaf / junction tables first
DROP TABLE IF EXISTS [rubric_student_grades];
DROP TABLE IF EXISTS [rubric_levels];
DROP TABLE IF EXISTS [rubric_criteria];
DROP TABLE IF EXISTS [rubrics];
DROP TABLE IF EXISTS [quiz_answers];
DROP TABLE IF EXISTS [quiz_attempts];
DROP TABLE IF EXISTS [quiz_options];
DROP TABLE IF EXISTS [quiz_questions];
DROP TABLE IF EXISTS [quizzes];
DROP TABLE IF EXISTS [assignment_submissions];
DROP TABLE IF EXISTS [assignments];
DROP TABLE IF EXISTS [attendance_records];
DROP TABLE IF EXISTS [results];
DROP TABLE IF EXISTS [result_component_rules];
DROP TABLE IF EXISTS [course_grading_configs];
DROP TABLE IF EXISTS [gpa_scale_rules];
DROP TABLE IF EXISTS [institution_grading_profiles];
DROP TABLE IF EXISTS [enrollments];
DROP TABLE IF EXISTS [timetable_entries];
DROP TABLE IF EXISTS [timetables];
DROP TABLE IF EXISTS [rooms];
DROP TABLE IF EXISTS [buildings];
DROP TABLE IF EXISTS [course_announcements];
DROP TABLE IF EXISTS [content_videos];
DROP TABLE IF EXISTS [course_content_modules];
DROP TABLE IF EXISTS [discussion_replies];
DROP TABLE IF EXISTS [discussion_threads];
DROP TABLE IF EXISTS [fyp_meetings];
DROP TABLE IF EXISTS [fyp_panel_members];
DROP TABLE IF EXISTS [fyp_projects];
DROP TABLE IF EXISTS [chat_messages];
DROP TABLE IF EXISTS [chat_conversations];
DROP TABLE IF EXISTS [support_ticket_messages];
DROP TABLE IF EXISTS [support_tickets];
DROP TABLE IF EXISTS [notification_recipients];
DROP TABLE IF EXISTS [notifications];
DROP TABLE IF EXISTS [outbound_email_logs];
DROP TABLE IF EXISTS [report_role_assignments];
DROP TABLE IF EXISTS [report_definitions];
DROP TABLE IF EXISTS [module_role_assignments];
DROP TABLE IF EXISTS [sidebar_menu_role_accesses];
DROP TABLE IF EXISTS [sidebar_menu_items];
DROP TABLE IF EXISTS [module_status];
DROP TABLE IF EXISTS [student_report_cards];
DROP TABLE IF EXISTS [transcript_export_logs];
DROP TABLE IF EXISTS [Tabsan-EduSphere];
DROP TABLE IF EXISTS [graduation_application_approvals];
DROP TABLE IF EXISTS [graduation_applications];
DROP TABLE IF EXISTS [bulk_promotion_entries];
DROP TABLE IF EXISTS [bulk_promotion_batches];
DROP TABLE IF EXISTS [teacher_modification_requests];
DROP TABLE IF EXISTS [admin_change_requests];
DROP TABLE IF EXISTS [accreditation_templates];
DROP TABLE IF EXISTS [degree_rule_required_courses];
DROP TABLE IF EXISTS [degree_rules];
DROP TABLE IF EXISTS [study_plan_courses];
DROP TABLE IF EXISTS [study_plans];
DROP TABLE IF EXISTS [student_stream_assignments];
DROP TABLE IF EXISTS [school_streams];
DROP TABLE IF EXISTS [registration_whitelist];
DROP TABLE IF EXISTS [payment_receipts];
DROP TABLE IF EXISTS [password_history];
DROP TABLE IF EXISTS [parent_student_links];
DROP TABLE IF EXISTS [consumed_verification_keys];
DROP TABLE IF EXISTS [academic_deadlines];
DROP TABLE IF EXISTS [course_prerequisites];
DROP TABLE IF EXISTS [course_offerings];
DROP TABLE IF EXISTS [semesters];
DROP TABLE IF EXISTS [courses];
DROP TABLE IF EXISTS [academic_programs];
DROP TABLE IF EXISTS [student_profiles];
DROP TABLE IF EXISTS [faculty_department_assignments];
DROP TABLE IF EXISTS [admin_department_assignments];
DROP TABLE IF EXISTS [user_sessions];
DROP TABLE IF EXISTS [audit_logs];
DROP TABLE IF EXISTS [license_state];
DROP TABLE IF EXISTS [portal_settings];
DROP TABLE IF EXISTS [modules];
DROP TABLE IF EXISTS [roles];
DROP TABLE IF EXISTS [users];
DROP TABLE IF EXISTS [departments];
DROP TABLE IF EXISTS [__EFMigrationsHistory];
GO

DECLARE @RemainingCount int;

SELECT @RemainingCount = COUNT(*)
FROM sys.tables t
WHERE t.name IN (
	N'__EFMigrationsHistory', N'academic_deadlines', N'academic_programs', N'accreditation_templates', N'admin_change_requests', N'admin_department_assignments',
	N'assignment_submissions', N'assignments', N'attendance_records', N'audit_logs', N'buildings', N'bulk_promotion_batches', N'bulk_promotion_entries',
	N'chat_conversations', N'chat_messages', N'consumed_verification_keys', N'content_videos', N'course_announcements', N'course_content_modules',
	N'course_grading_configs', N'course_offerings', N'course_prerequisites', N'courses', N'degree_rule_required_courses', N'degree_rules', N'departments',
	N'discussion_replies', N'discussion_threads', N'enrollments', N'faculty_department_assignments', N'fyp_meetings', N'fyp_panel_members', N'fyp_projects',
	N'gpa_scale_rules', N'graduation_application_approvals', N'graduation_applications', N'institution_grading_profiles', N'license_state', N'module_role_assignments',
	N'module_status', N'modules', N'notification_recipients', N'notifications', N'outbound_email_logs', N'parent_student_links', N'password_history', N'payment_receipts',
	N'portal_settings', N'quiz_answers', N'quiz_attempts', N'quiz_options', N'quiz_questions', N'quizzes', N'registration_whitelist', N'report_definitions',
	N'report_role_assignments', N'result_component_rules', N'results', N'roles', N'rooms', N'rubric_criteria', N'rubric_levels', N'rubric_student_grades', N'rubrics',
	N'school_streams', N'semesters', N'sidebar_menu_items', N'sidebar_menu_role_accesses', N'student_profiles', N'student_report_cards', N'student_stream_assignments',
	N'study_plan_courses', N'study_plans', N'support_ticket_messages', N'support_tickets', N'teacher_modification_requests', N'timetable_entries', N'timetables',
	N'transcript_export_logs', N'Tabsan-EduSphere', N'user_sessions', N'users'
);

IF @RemainingCount = 0
	PRINT 'Cleanup complete. All EduSphere objects removed from master.';
ELSE
BEGIN
	PRINT 'Cleanup completed with leftovers. Review remaining tables below:';
	SELECT t.name AS RemainingTable
	FROM sys.tables t
	WHERE t.name IN (
		N'__EFMigrationsHistory', N'academic_deadlines', N'academic_programs', N'accreditation_templates', N'admin_change_requests', N'admin_department_assignments',
		N'assignment_submissions', N'assignments', N'attendance_records', N'audit_logs', N'buildings', N'bulk_promotion_batches', N'bulk_promotion_entries',
		N'chat_conversations', N'chat_messages', N'consumed_verification_keys', N'content_videos', N'course_announcements', N'course_content_modules',
		N'course_grading_configs', N'course_offerings', N'course_prerequisites', N'courses', N'degree_rule_required_courses', N'degree_rules', N'departments',
		N'discussion_replies', N'discussion_threads', N'enrollments', N'faculty_department_assignments', N'fyp_meetings', N'fyp_panel_members', N'fyp_projects',
		N'gpa_scale_rules', N'graduation_application_approvals', N'graduation_applications', N'institution_grading_profiles', N'license_state', N'module_role_assignments',
		N'module_status', N'modules', N'notification_recipients', N'notifications', N'outbound_email_logs', N'parent_student_links', N'password_history', N'payment_receipts',
		N'portal_settings', N'quiz_answers', N'quiz_attempts', N'quiz_options', N'quiz_questions', N'quizzes', N'registration_whitelist', N'report_definitions',
		N'report_role_assignments', N'result_component_rules', N'results', N'roles', N'rooms', N'rubric_criteria', N'rubric_levels', N'rubric_student_grades', N'rubrics',
		N'school_streams', N'semesters', N'sidebar_menu_items', N'sidebar_menu_role_accesses', N'student_profiles', N'student_report_cards', N'student_stream_assignments',
		N'study_plan_courses', N'study_plans', N'support_ticket_messages', N'support_tickets', N'teacher_modification_requests', N'timetable_entries', N'timetables',
		N'transcript_export_logs', N'Tabsan-EduSphere', N'user_sessions', N'users'
	)
	ORDER BY t.name;
END
PRINT 'Now run 01-Schema-Current.sql (connected to master) to create [Tabsan-EduSphere] properly.';
GO
