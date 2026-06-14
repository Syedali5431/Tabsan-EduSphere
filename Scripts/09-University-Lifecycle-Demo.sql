/*
  University Student Lifecycle Demo — Tabsan EduSphere
  Creates a single university student and walks through their entire
  academic lifecycle from Semester 1 to Semester 8 (BSCS Program), including:
  - Enrollments, Attendance, Results, Quizzes, Assignments
  - Payments, Notifications, FYP project, Discussion threads
  - Degree and Transcript generation at completion
  
  Run after: 02-Seed-Core.sql, 03-FullDummyData.sql
*/
SET NOCOUNT ON;
GO
USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @DefaultPwd NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @R_Student INT = 4;

-- University tenant/campus/department
DECLARE @T_Uni   UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @C_Uni   UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
DECLARE @D_IT    UNIQUEIDENTIFIER = 'D0000001-0000-0000-0000-000000000001';
DECLARE @P_BSCS  UNIQUEIDENTIFIER = 'A0000001-0000-0000-0000-000000000001';
DECLARE @Fac_Uni UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=2);

PRINT '=== University Student Lifecycle Demo — Bilal Ahmed ===';

-- ════════════════════════════════════════════════
-- 1. CREATE STUDENT USER + PROFILE
-- ════════════════════════════════════════════════
DECLARE @uid UNIQUEIDENTIFIER = 'E0000021-0000-0000-0000-000000000001';
DECLARE @spId UNIQUEIDENTIFIER = 'E0000022-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id]=@uid)
    INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
    VALUES (@uid, N'demouni', N'bilal.ahmed@uni.local', N'Bilal Ahmed', @DefaultPwd, @R_Student, @D_IT, @T_Uni, @C_Uni, 2, 1, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [Id]=@spId)
    INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
    VALUES (@spId, @uid, N'UNI-DEMO-001', @P_BSCS, @D_IT, 8, DATEADD(YEAR,-4,@Now), 0, @Now, 0);

PRINT 'Student created: Bilal Ahmed (UNI-DEMO-001)';

-- ════════════════════════════════════════════════
-- 2. COMPLETE SEMESTERS 1-8
-- ════════════════════════════════════════════════
DECLARE @sem INT = 1;
DECLARE @semName NVARCHAR(100);
DECLARE @semId UNIQUEIDENTIFIER;

WHILE @sem <= 8
BEGIN
    SET @semName = CONCAT(N'Semester ',@sem);
    SET @semId = (SELECT Id FROM [semesters] WHERE [Name] LIKE CONCAT(@semName,N'%') AND [IsActive]=1);

    PRINT CONCAT(N'  Processing Semester ',@sem,N'...');

    -- 2a. Enroll in 5 BSCS courses for this semester
    INSERT INTO [enrollments] ([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
    SELECT NEWID(), @spId, co.Id, DATEADD(YEAR,-(8-@sem)/2,@Now), N'Active', @Now
    FROM [course_offerings] co
    JOIN [semesters] s ON s.Id = co.SemesterId
    WHERE s.[Name] LIKE CONCAT(@semName,N'%')
      AND NOT EXISTS (SELECT 1 FROM [enrollments] e WHERE e.StudentProfileId=@spId AND e.CourseOfferingId=co.Id);

    -- 2b. Attendance: 28 days, ~90% present
    DECLARE @offId UNIQUEIDENTIFIER;
    DECLARE offCur CURSOR FOR 
        SELECT co.Id FROM [course_offerings] co
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE s.[Name] LIKE CONCAT(@semName,N'%');
    OPEN offCur; FETCH NEXT FROM offCur INTO @offId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @day INT = 1;
        WHILE @day <= 28
        BEGIN
            DECLARE @stat NVARCHAR(10) = CASE WHEN @day % 14 = 0 THEN N'Absent' ELSE N'Present' END;
            DECLARE @attDate DATE = DATEADD(DAY,-@day,DATEADD(YEAR,-(8-@sem)/2,@Now));
            IF NOT EXISTS (SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spId AND [CourseOfferingId]=@offId AND [Date]=@attDate)
                INSERT INTO [attendance_records] ([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt])
                VALUES (NEWID(), @spId, @offId, @attDate, @stat, @Fac_Uni, @Now);
            SET @day += 1;
        END
        FETCH NEXT FROM offCur INTO @offId;
    END
    CLOSE offCur; DEALLOCATE offCur;

    -- 2c. Results: published with grade points
    INSERT INTO [results] ([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[GradePoint],[IsPublished],[CreatedAt])
    SELECT NEWID(), @spId, co.Id, N'Final', 50+ABS(CHECKSUM(NEWID()))%51, 100,
           CASE 
               WHEN ABS(CHECKSUM(NEWID()))%100 < 20 THEN 4.0
               WHEN ABS(CHECKSUM(NEWID()))%100 < 40 THEN 3.7
               WHEN ABS(CHECKSUM(NEWID()))%100 < 60 THEN 3.3
               WHEN ABS(CHECKSUM(NEWID()))%100 < 80 THEN 3.0
               ELSE 2.7
           END,
           1, @Now
    FROM [course_offerings] co
    JOIN [semesters] s ON s.Id = co.SemesterId
    WHERE s.[Name] LIKE CONCAT(@semName,N'%')
      AND NOT EXISTS (SELECT 1 FROM [results] r WHERE r.StudentProfileId=@spId AND r.CourseOfferingId=co.Id);

    -- 2d. Quiz attempts
    DECLARE @qId UNIQUEIDENTIFIER;
    DECLARE quizCur CURSOR FOR 
        SELECT q.Id FROM [quizzes] q
        JOIN [course_offerings] co ON co.Id = q.CourseOfferingId
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE s.[Name] LIKE CONCAT(@semName,N'%');
    OPEN quizCur; FETCH NEXT FROM quizCur INTO @qId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [quiz_attempts] WHERE [QuizId]=@qId AND [StudentProfileId]=@spId)
            INSERT INTO [quiz_attempts] ([Id],[QuizId],[StudentProfileId],[StartedAt],[FinishedAt],[AttemptStatus],[TotalScore],[CreatedAt])
            VALUES (NEWID(), @qId, @spId, DATEADD(DAY,-5,DATEADD(YEAR,-(8-@sem)/2,@Now)), DATEADD(MINUTE,25,DATEADD(DAY,-5,DATEADD(YEAR,-(8-@sem)/2,@Now))), N'Completed', 70+ABS(CHECKSUM(NEWID()))%31, @Now);
        FETCH NEXT FROM quizCur INTO @qId;
    END
    CLOSE quizCur; DEALLOCATE quizCur;

    -- 2e. Assignment submissions
    DECLARE @aId UNIQUEIDENTIFIER;
    DECLARE assignCur CURSOR FOR 
        SELECT a.Id FROM [assignments] a
        JOIN [course_offerings] co ON co.Id = a.CourseOfferingId
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE s.[Name] LIKE CONCAT(@semName,N'%');
    OPEN assignCur; FETCH NEXT FROM assignCur INTO @aId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [assignment_submissions] WHERE [AssignmentId]=@aId AND [StudentProfileId]=@spId)
            INSERT INTO [assignment_submissions] ([Id],[AssignmentId],[StudentProfileId],[SubmittedAt],[Status],[MarksAwarded],[CreatedAt])
            VALUES (NEWID(), @aId, @spId, DATEADD(DAY,-2,DATEADD(YEAR,-(8-@sem)/2,@Now)), N'Submitted', 60+ABS(CHECKSUM(NEWID()))%41, @Now);
        FETCH NEXT FROM assignCur INTO @aId;
    END
    CLOSE assignCur; DEALLOCATE assignCur;

    -- 2f. Payment receipt
    INSERT INTO [payment_receipts] ([Id],[StudentProfileId],[CreatedByUserId],[ReceiptNo],[Status],[Amount],[Description],[DueDate],[ConfirmedByUserId],[ConfirmedAt],[Notes],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(), @spId, ISNULL(@Fac_Uni,(SELECT TOP 1 Id FROM [users] WHERE RoleId=2)),
            CONCAT(N'RCP-UNIDEMO-S',@sem), 2, 15000.00,
            CONCAT(N'Semester ',@sem,N' Tuition Fee'),
            DATEADD(MONTH,1,DATEADD(YEAR,-(8-@sem)/2,@Now)),
            ISNULL(@Fac_Uni,(SELECT TOP 1 Id FROM [users] WHERE RoleId=2)),
            DATEADD(YEAR,-(8-@sem)/2,@Now),
            N'Paid in full', @Now, @Now, 0, NULL);

    SET @sem += 1;
END

-- ════════════════════════════════════════════════
-- 3. UPDATE PROFILE: All 8 semesters completed
-- ════════════════════════════════════════════════
UPDATE [student_profiles] SET [CurrentSemesterNumber]=8, [Cgpa]=3.42 WHERE [Id]=@spId;
PRINT 'Profile updated: CGPA 3.42, Semester 8 completed.';

-- ════════════════════════════════════════════════
-- 4. FYP PROJECT
-- ════════════════════════════════════════════════
DECLARE @fypId UNIQUEIDENTIFIER = 'E0000030-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM [fyp_projects] WHERE [StudentProfileId]=@spId)
BEGIN
    INSERT INTO [fyp_projects] ([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[CoordinatorRemarks],[FypGradePoint],[FypMarks],[FypMaxMarks],[CreatedAt])
    VALUES (@fypId,@spId,@D_IT,N'AI-Powered Campus Navigation System',
            N'A mobile application using machine learning to help students navigate the university campus with real-time routing and event notifications.',
            N'Completed',@Fac_Uni,N'Outstanding project with excellent implementation and presentation.',3.7,92,100,@Now);
    PRINT 'FYP project created: AI-Powered Campus Navigation System (Grade: A-, 92%)';
END

-- ════════════════════════════════════════════════
-- 5. COURSE ANNOUNCEMENT for final semester
-- ════════════════════════════════════════════════
DECLARE @s8SemId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] LIKE N'Semester 8%' ORDER BY [Name]);
DECLARE @s8OffId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [course_offerings] WHERE [SemesterId]=@s8SemId);

IF @s8OffId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [course_announcements] WHERE [OfferingId]=@s8OffId AND [AuthorId]=@uid)
    INSERT INTO [course_announcements] ([Id],[OfferingId],[AuthorId],[Title],[Body],[PostedAt],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@s8OffId,@uid,N'Final Semester Project Submission',N'All final projects and FYP documentation must be submitted by the deadline. Congratulations on reaching your final semester!',@Now,@Now,0,NULL);

-- ════════════════════════════════════════════════
-- 6. DISCUSSION THREAD
-- ════════════════════════════════════════════════
IF @s8OffId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [discussion_threads] WHERE [OfferingId]=@s8OffId AND [AuthorId]=@uid)
BEGIN
    DECLARE @dtId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [discussion_threads] ([Id],[OfferingId],[Title],[AuthorId],[IsPinned],[IsClosed],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (@dtId,@s8OffId,N'Career opportunities after BSCS',@uid,1,0,@Now,0,NULL);
    INSERT INTO [discussion_replies] ([Id],[ThreadId],[AuthorId],[Body],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@dtId,@Fac_Uni,N'With your CGPA and FYP project, you are well-positioned for top tech companies. Consider applying for the graduate program!',@Now,0,NULL);
END

-- ════════════════════════════════════════════════
-- 7. SUPPORT TICKET
-- ════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [support_tickets] WHERE [SubmitterId]=@uid)
    INSERT INTO [support_tickets] ([Id],[SubmitterId],[DepartmentId],[Category],[Subject],[Body],[Status],[ReopenWindowDays],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@uid,@D_IT,1,N'Degree and transcript request',N'I have completed all 8 semesters of BSCS with CGPA 3.42. Please process my degree and official transcript.',2,7,@Now,0,NULL);

-- ════════════════════════════════════════════════
-- 8. STUDY PLAN for Masters
-- ════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [study_plans] WHERE [StudentProfileId]=@spId)
BEGIN
    DECLARE @planId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [study_plans] ([Id],[StudentProfileId],[PlannedSemesterName],[Notes],[AdvisorStatus],[ReviewedByUserId],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (@planId,@spId,N'MSCS Semester 1',N'Planning to pursue MSCS with specialization in Artificial Intelligence.',1,@Fac_Uni,@Now,0,NULL);
END

-- ════════════════════════════════════════════════
-- 9. NOTIFICATION
-- ════════════════════════════════════════════════
DECLARE @notifId UNIQUEIDENTIFIER = NEWID();
IF NOT EXISTS (SELECT 1 FROM [notifications] WHERE [Title]=N'Congratulations Bilal! BSCS Completed')
    INSERT INTO [notifications] ([Id],[Title],[Body],[Type],[SenderUserId],[IsSystemGenerated],[IsActive],[CreatedAt])
    VALUES (@notifId,N'Congratulations Bilal! BSCS Completed',N'Dear Bilal, you have successfully completed all 8 semesters of BSCS with CGPA 3.42. Your degree and transcript are ready. Best wishes for your future!',N'Academic',@Fac_Uni,0,1,@Now);

IF NOT EXISTS (SELECT 1 FROM [notification_recipients] WHERE [NotificationId]=@notifId AND [RecipientUserId]=@uid)
    INSERT INTO [notification_recipients] ([Id],[NotificationId],[RecipientUserId],[IsRead],[ReadAt],[CreatedAt])
    VALUES (NEWID(),@notifId,@uid,0,NULL,@Now);

-- ════════════════════════════════════════════════
-- 10. SUMMARY
-- ════════════════════════════════════════════════
PRINT '';
PRINT '=== UNIVERSITY LIFECYCLE DEMO COMPLETE ===';
PRINT 'Student:    Bilal Ahmed (UNI-DEMO-001)';
PRINT 'Username:   demouni';
PRINT 'Password:   (default demo password)';
PRINT 'Program:    BSCS, Semesters 1-8';
PRINT 'CGPA:       3.42';
PRINT 'FYP:        AI-Powered Campus Navigation System (A-, 92%)';
PRINT '';
PRINT 'Enrollments: ' + CAST((SELECT COUNT(*) FROM [enrollments] WHERE [StudentProfileId]=@spId) AS NVARCHAR);
PRINT 'Attendance:  ' + CAST((SELECT COUNT(*) FROM [attendance_records] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' records';
PRINT 'Results:     ' + CAST((SELECT COUNT(*) FROM [results] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' published';
PRINT 'Quiz attempts: ' + CAST((SELECT COUNT(*) FROM [quiz_attempts] WHERE [StudentProfileId]=@spId) AS NVARCHAR);
PRINT 'Assignments:  ' + CAST((SELECT COUNT(*) FROM [assignment_submissions] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' submitted';
PRINT 'Payments:     ' + CAST((SELECT COUNT(*) FROM [payment_receipts] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' receipts';
PRINT 'Login as:     demouni to view the complete lifecycle.';
PRINT 'Transcript:   Use Admin portal → Report Center → Student Transcript';
PRINT 'Degree:       Use Admin portal → Generate Certificates → Degree';
GO
