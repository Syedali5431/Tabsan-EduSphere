/*
  College Student Lifecycle Demo — Tabsan EduSphere
  Creates a single college student and walks through their entire
  academic lifecycle from Class 11 to Class 12 (ICS Program), including:
  - Enrollments, Attendance, Results, Quizzes, Assignments
  - Payments, Notifications, Discussion threads
  - Certificate generation at completion
  
  Run after: 02-Seed-Core.sql, 03-FullDummyData.sql
*/
SET NOCOUNT ON;
GO
USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @DefaultPwd NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @R_Student INT = 4;

-- College tenant/campus/department
DECLARE @T_Col   UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @C_Col   UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB';
DECLARE @D_Col   UNIQUEIDENTIFIER = 'D0000003-0000-0000-0000-000000000003';
DECLARE @P_ICS   UNIQUEIDENTIFIER = 'A0000005-0000-0000-0000-000000000005';
DECLARE @Fac_Col UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=1);

PRINT '=== College Student Lifecycle Demo — Zara Malik ===';

-- ════════════════════════════════════════════════
-- 1. CREATE STUDENT USER + PROFILE
-- ════════════════════════════════════════════════
DECLARE @uid UNIQUEIDENTIFIER = 'E0000011-0000-0000-0000-000000000001';
DECLARE @spId UNIQUEIDENTIFIER = 'E0000012-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id]=@uid)
    INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
    VALUES (@uid, N'democollege', N'zara.malik@college.local', N'Zara Malik', @DefaultPwd, @R_Student, @D_Col, @T_Col, @C_Col, 1, 1, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [Id]=@spId)
    INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
    VALUES (@spId, @uid, N'COL-DEMO-001', @P_ICS, @D_Col, 12, DATEADD(YEAR,-2,@Now), NULL, @Now, 0);

PRINT 'Student created: Zara Malik (COL-DEMO-001)';

-- ════════════════════════════════════════════════
-- 2. COMPLETE CLASS 11 & 12
-- ════════════════════════════════════════════════
DECLARE @class INT = 11;
DECLARE @semName NVARCHAR(100);
DECLARE @semId UNIQUEIDENTIFIER;

WHILE @class <= 12
BEGIN
    SET @semName = CONCAT(N'ICS Year ',@class-10,N' (2026)');
    -- Try different naming patterns
    SET @semId = (SELECT Id FROM [semesters] WHERE [Name]=@semName);
    IF @semId IS NULL
        SET @semId = (SELECT Id FROM [semesters] WHERE [Name] LIKE CONCAT(N'Class ',@class,N'%') AND [IsActive]=1);
    IF @semId IS NULL
        SET @semId = (SELECT Id FROM [semesters] WHERE [Name] LIKE CONCAT(N'ICS%') AND [IsActive]=1);
    
    PRINT CONCAT(N'  Processing Class ',@class,N' (',@semName,N')...');

    -- 2a. Enroll in 5 ICS subjects for this class
    INSERT INTO [enrollments] ([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
    SELECT NEWID(), @spId, co.Id, DATEADD(YEAR,-13+@class,@Now), N'Active', @Now
    FROM [course_offerings] co
    JOIN [courses] c ON c.Id = co.CourseId
    JOIN [semesters] s ON s.Id = co.SemesterId
    WHERE (s.[Name] = @semName OR s.[Name] LIKE CONCAT(N'Class ',@class,N'%') OR s.[Name] LIKE N'ICS%')
      AND c.[Code] IN (N'ENG001',N'MTH001',N'SCI001',N'CS001',N'URD001')
      AND NOT EXISTS (SELECT 1 FROM [enrollments] e WHERE e.StudentProfileId=@spId AND e.CourseOfferingId=co.Id);

    -- 2b. Attendance: 30 days, ~90% present
    DECLARE @offId UNIQUEIDENTIFIER;
    DECLARE offCur CURSOR FOR 
        SELECT co.Id FROM [course_offerings] co
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE (s.[Name] = @semName OR s.[Name] LIKE CONCAT(N'Class ',@class,N'%') OR s.[Name] LIKE N'ICS%');
    OPEN offCur; FETCH NEXT FROM offCur INTO @offId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @day INT = 1;
        WHILE @day <= 30
        BEGIN
            DECLARE @stat NVARCHAR(10) = CASE WHEN @day % 12 = 0 THEN N'Absent' ELSE N'Present' END;
            DECLARE @attDate DATE = DATEADD(DAY,-@day,DATEADD(YEAR,-13+@class,@Now));
            IF NOT EXISTS (SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spId AND [CourseOfferingId]=@offId AND [Date]=@attDate)
                INSERT INTO [attendance_records] ([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt])
                VALUES (NEWID(), @spId, @offId, @attDate, @stat, @Fac_Col, @Now);
            SET @day += 1;
        END
        FETCH NEXT FROM offCur INTO @offId;
    END
    CLOSE offCur; DEALLOCATE offCur;

    -- 2c. Results: 5 subjects, passing marks (65-98%)
    INSERT INTO [results] ([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
    SELECT NEWID(), @spId, co.Id, N'Final', 65+ABS(CHECKSUM(NEWID()))%34, 100, 1, @Now
    FROM [course_offerings] co
    JOIN [semesters] s ON s.Id = co.SemesterId
    WHERE (s.[Name] = @semName OR s.[Name] LIKE CONCAT(N'Class ',@class,N'%') OR s.[Name] LIKE N'ICS%')
      AND NOT EXISTS (SELECT 1 FROM [results] r WHERE r.StudentProfileId=@spId AND r.CourseOfferingId=co.Id);

    -- 2d. Quiz attempts (score 75-100%)
    DECLARE @qId UNIQUEIDENTIFIER;
    DECLARE quizCur CURSOR FOR 
        SELECT q.Id FROM [quizzes] q
        JOIN [course_offerings] co ON co.Id = q.CourseOfferingId
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE (s.[Name] = @semName OR s.[Name] LIKE CONCAT(N'Class ',@class,N'%') OR s.[Name] LIKE N'ICS%');
    OPEN quizCur; FETCH NEXT FROM quizCur INTO @qId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [quiz_attempts] WHERE [QuizId]=@qId AND [StudentProfileId]=@spId)
            INSERT INTO [quiz_attempts] ([Id],[QuizId],[StudentProfileId],[StartedAt],[FinishedAt],[AttemptStatus],[TotalScore],[CreatedAt])
            VALUES (NEWID(), @qId, @spId, DATEADD(DAY,-5,DATEADD(YEAR,-13+@class,@Now)), DATEADD(MINUTE,25,DATEADD(DAY,-5,DATEADD(YEAR,-13+@class,@Now))), N'Completed', 75+ABS(CHECKSUM(NEWID()))%26, @Now);
        FETCH NEXT FROM quizCur INTO @qId;
    END
    CLOSE quizCur; DEALLOCATE quizCur;

    -- 2e. Assignment submissions
    DECLARE @aId UNIQUEIDENTIFIER;
    DECLARE assignCur CURSOR FOR 
        SELECT a.Id FROM [assignments] a
        JOIN [course_offerings] co ON co.Id = a.CourseOfferingId
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE (s.[Name] = @semName OR s.[Name] LIKE CONCAT(N'Class ',@class,N'%') OR s.[Name] LIKE N'ICS%');
    OPEN assignCur; FETCH NEXT FROM assignCur INTO @aId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [assignment_submissions] WHERE [AssignmentId]=@aId AND [StudentProfileId]=@spId)
            INSERT INTO [assignment_submissions] ([Id],[AssignmentId],[StudentProfileId],[SubmittedAt],[Status],[MarksAwarded],[CreatedAt])
            VALUES (NEWID(), @aId, @spId, DATEADD(DAY,-2,DATEADD(YEAR,-13+@class,@Now)), N'Submitted', 65+ABS(CHECKSUM(NEWID()))%36, @Now);
        FETCH NEXT FROM assignCur INTO @aId;
    END
    CLOSE assignCur; DEALLOCATE assignCur;

    -- 2f. Payment receipt (paid in full)
    INSERT INTO [payment_receipts] ([Id],[StudentProfileId],[CreatedByUserId],[ReceiptNo],[Status],[Amount],[Description],[DueDate],[ConfirmedByUserId],[ConfirmedAt],[Notes],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(), @spId, ISNULL(@Fac_Col,(SELECT TOP 1 Id FROM [users] WHERE RoleId=2)),
            CONCAT(N'RCP-COLDEMO-C',@class), 2, 8000.00,
            CONCAT(N'Class ',@class,N' Tuition Fee'),
            DATEADD(MONTH,1,DATEADD(YEAR,-13+@class,@Now)),
            ISNULL(@Fac_Col,(SELECT TOP 1 Id FROM [users] WHERE RoleId=2)),
            DATEADD(YEAR,-13+@class,@Now),
            N'Paid in full', @Now, @Now, 0, NULL);

    SET @class += 1;
END

-- ════════════════════════════════════════════════
-- 3. UPDATE PROFILE: ICS completed
-- ════════════════════════════════════════════════
UPDATE [student_profiles] SET [CurrentSemesterNumber]=12, [Cgpa]=NULL WHERE [Id]=@spId;
PRINT 'Profile updated: ICS Year 2 (Class 12) completed.';

-- ════════════════════════════════════════════════
-- 4. COURSE ANNOUNCEMENT
-- ════════════════════════════════════════════════
DECLARE @c12SemId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] LIKE N'Class 12%' OR [Name] LIKE N'ICS Year 2%' ORDER BY [Name]);
DECLARE @c12OffId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [course_offerings] WHERE [SemesterId]=@c12SemId);

IF @c12OffId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [course_announcements] WHERE [OfferingId]=@c12OffId AND [AuthorId]=@uid)
    INSERT INTO [course_announcements] ([Id],[OfferingId],[AuthorId],[Title],[Body],[PostedAt],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@c12OffId,@uid,N'Final Project Submission',N'Please submit your final year programming project by the deadline. Best of luck!',@Now,@Now,0,NULL);

-- ════════════════════════════════════════════════
-- 5. DISCUSSION THREAD
-- ════════════════════════════════════════════════
IF @c12OffId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [discussion_threads] WHERE [OfferingId]=@c12OffId AND [AuthorId]=@uid)
BEGIN
    DECLARE @dtId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [discussion_threads] ([Id],[OfferingId],[Title],[AuthorId],[IsPinned],[IsClosed],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (@dtId,@c12OffId,N'Career guidance after ICS',@uid,1,0,@Now,0,NULL);
    INSERT INTO [discussion_replies] ([Id],[ThreadId],[AuthorId],[Body],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@dtId,@Fac_Col,N'Consider BSCS or Software Engineering at university. Your programming skills are excellent!',@Now,0,NULL);
END

-- ════════════════════════════════════════════════
-- 6. SUPPORT TICKET
-- ════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [support_tickets] WHERE [SubmitterId]=@uid)
    INSERT INTO [support_tickets] ([Id],[SubmitterId],[DepartmentId],[Category],[Subject],[Body],[Status],[ReopenWindowDays],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@uid,@D_Col,1,N'College transcript request',N'I have completed ICS Year 2. Please issue my Completion Certificate and Report Card for university admission.',2,7,@Now,0,NULL);

-- ════════════════════════════════════════════════
-- 7. STUDY PLAN for university
-- ════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [study_plans] WHERE [StudentProfileId]=@spId)
BEGIN
    DECLARE @planId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [study_plans] ([Id],[StudentProfileId],[PlannedSemesterName],[Notes],[AdvisorStatus],[ReviewedByUserId],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (@planId,@spId,N'University Semester 1',N'Applying to BSCS program. Need to prepare for entrance exam.',1,@Fac_Col,@Now,0,NULL);
    
    INSERT INTO [study_plan_courses] ([Id],[StudyPlanId],[CourseId],[CreatedAt])
    SELECT NEWID(), @planId, c.Id, @Now
    FROM [courses] c WHERE c.[Code] IN (N'CS101',N'MTH001',N'ENG001')
      AND NOT EXISTS (SELECT 1 FROM [study_plan_courses] spc WHERE spc.StudyPlanId=@planId AND spc.CourseId=c.Id);
END

-- ════════════════════════════════════════════════
-- 8. NOTIFICATION
-- ════════════════════════════════════════════════
DECLARE @notifId UNIQUEIDENTIFIER;
IF NOT EXISTS (SELECT 1 FROM [notifications] WHERE [Title]=N'Congratulations Zara! ICS Completed')
BEGIN
    SET @notifId = NEWID();
    INSERT INTO [notifications] ([Id],[Title],[Body],[Type],[SenderUserId],[IsSystemGenerated],[IsActive],[CreatedAt])
    VALUES (@notifId,N'Congratulations Zara! ICS Completed',N'Dear Zara, you have successfully completed ICS (Class 11-12). Your certificates are ready. Best wishes for university!',N'Academic',@Fac_Col,0,1,@Now);
END
ELSE
    SET @notifId = (SELECT Id FROM [notifications] WHERE [Title]=N'Congratulations Zara! ICS Completed');

IF NOT EXISTS (SELECT 1 FROM [notification_recipients] WHERE [NotificationId]=@notifId AND [RecipientUserId]=@uid)
    INSERT INTO [notification_recipients] ([Id],[NotificationId],[RecipientUserId],[IsRead],[ReadAt],[CreatedAt])
    VALUES (NEWID(),@notifId,@uid,0,NULL,@Now);

-- ════════════════════════════════════════════════
-- 9. SUMMARY
-- ════════════════════════════════════════════════
PRINT '';
PRINT '=== COLLEGE LIFECYCLE DEMO COMPLETE ===';
PRINT 'Student:    Zara Malik (COL-DEMO-001)';
PRINT 'Username:   democollege';
PRINT 'Password:   (default demo password)';
PRINT 'College:    ICS Program, Classes 11-12';
PRINT 'Grading:    Percentage-based';
PRINT '';
PRINT 'Enrollments: ' + CAST((SELECT COUNT(*) FROM [enrollments] WHERE [StudentProfileId]=@spId) AS NVARCHAR);
PRINT 'Attendance:  ' + CAST((SELECT COUNT(*) FROM [attendance_records] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' records';
PRINT 'Results:     ' + CAST((SELECT COUNT(*) FROM [results] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' published';
PRINT 'Quiz attempts: ' + CAST((SELECT COUNT(*) FROM [quiz_attempts] WHERE [StudentProfileId]=@spId) AS NVARCHAR);
PRINT 'Assignments:  ' + CAST((SELECT COUNT(*) FROM [assignment_submissions] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' submitted';
PRINT 'Payments:     ' + CAST((SELECT COUNT(*) FROM [payment_receipts] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' receipts';
PRINT 'Login as:     democollege to view the complete lifecycle.';
PRINT 'Certificates: Use Admin portal → Generate Certificates → Select Zara → Completion Certificate + Report Card';
GO
