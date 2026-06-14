/*
  Student Lifecycle Demo — Tabsan EduSphere
  Creates a single school student and walks through their entire
  academic lifecycle from Class 1 to Class 10, including:
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

-- School tenant/campus/department
DECLARE @T_Sch   UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @C_Sch   UNIQUEIDENTIFIER = 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC';
DECLARE @D_Sch   UNIQUEIDENTIFIER = 'D0000004-0000-0000-0000-000000000004';
DECLARE @P_SCI   UNIQUEIDENTIFIER = 'A0000006-0000-0000-0000-000000000006';
DECLARE @Fac_Sch UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=0);

PRINT '=== Student Lifecycle Demo — Ahmad Hassan ===';

-- ════════════════════════════════════════════════
-- 1. CREATE STUDENT USER + PROFILE
-- ════════════════════════════════════════════════
DECLARE @uid UNIQUEIDENTIFIER = 'E0000001-0000-0000-0000-000000000001';
DECLARE @spId UNIQUEIDENTIFIER = 'E0000002-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Id]=@uid)
    INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
    VALUES (@uid, N'demostudent', N'ahmad.hassan@school.local', N'Ahmad Hassan', @DefaultPwd, @R_Student, @D_Sch, @T_Sch, @C_Sch, 0, 1, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [Id]=@spId)
    INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
    VALUES (@spId, @uid, N'SCH-DEMO-001', @P_SCI, @D_Sch, 10, DATEADD(YEAR,-10,@Now), 0, @Now, 0);

PRINT 'Student created: Ahmad Hassan (SCH-DEMO-001)';

-- ════════════════════════════════════════════════
-- 2. COMPLETE EACH CLASS (1 → 10)
-- ════════════════════════════════════════════════
DECLARE @class INT = 1;
DECLARE @semName NVARCHAR(100);
DECLARE @semId UNIQUEIDENTIFIER;

WHILE @class <= 10
BEGIN
    SET @semName = CONCAT(N'Class ',@class,N'  (2026)');
    SET @semId = (SELECT Id FROM [semesters] WHERE [Name]=@semName AND [IsActive]=1);
    
    IF @semId IS NULL
        SET @semId = (SELECT TOP 1 Id FROM [semesters] WHERE [Name] LIKE CONCAT(N'Class ',@class,N'%') ORDER BY [Name]);
    
    PRINT CONCAT(N'  Processing Class ',@class,N' (',@semName,N')...');

    -- 2a. Enroll in 5 school subjects for this class
    INSERT INTO [enrollments] ([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
    SELECT NEWID(), @spId, co.Id, DATEADD(YEAR,-10+@class,@Now), N'Active', @Now
    FROM [course_offerings] co
    JOIN [courses] c ON c.Id = co.CourseId
    JOIN [semesters] s ON s.Id = co.SemesterId
    WHERE s.[Name] = @semName
      AND c.[Code] IN (N'ENG001',N'MTH001',N'SCI001',N'SST001',N'URD001')
      AND NOT EXISTS (SELECT 1 FROM [enrollments] e WHERE e.StudentProfileId=@spId AND e.CourseOfferingId=co.Id);

    -- 2b. Attendance: 30 days, ~90% present (27/30)
    DECLARE @offId UNIQUEIDENTIFIER;
    DECLARE offCur CURSOR FOR 
        SELECT co.Id FROM [course_offerings] co
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE s.[Name] = @semName;
    OPEN offCur; FETCH NEXT FROM offCur INTO @offId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @day INT = 1;
        WHILE @day <= 30
        BEGIN
            DECLARE @stat NVARCHAR(10) = CASE WHEN @day % 10 = 0 THEN N'Absent' ELSE N'Present' END;
            DECLARE @attDate DATE = DATEADD(DAY,-@day,DATEADD(YEAR,-10+@class,@Now));
            IF NOT EXISTS (SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spId AND [CourseOfferingId]=@offId AND [Date]=@attDate)
                INSERT INTO [attendance_records] ([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt])
                VALUES (NEWID(), @spId, @offId, @attDate, @stat, @Fac_Sch, @Now);
            SET @day += 1;
        END
        FETCH NEXT FROM offCur INTO @offId;
    END
    CLOSE offCur; DEALLOCATE offCur;

    -- 2c. Results: 5 subjects, passing marks (60-95%)
    INSERT INTO [results] ([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
    SELECT NEWID(), @spId, co.Id, N'Final', 60+ABS(CHECKSUM(NEWID()))%36, 100, 1, @Now
    FROM [course_offerings] co
    JOIN [semesters] s ON s.Id = co.SemesterId
    WHERE s.[Name] = @semName
      AND NOT EXISTS (SELECT 1 FROM [results] r WHERE r.StudentProfileId=@spId AND r.CourseOfferingId=co.Id);

    -- 2d. Quiz attempts (1 per subject, score 70-100%)
    DECLARE @qId UNIQUEIDENTIFIER;
    DECLARE quizCur CURSOR FOR 
        SELECT q.Id FROM [quizzes] q
        JOIN [course_offerings] co ON co.Id = q.CourseOfferingId
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE s.[Name] = @semName;
    OPEN quizCur; FETCH NEXT FROM quizCur INTO @qId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [quiz_attempts] WHERE [QuizId]=@qId AND [StudentProfileId]=@spId)
            INSERT INTO [quiz_attempts] ([Id],[QuizId],[StudentProfileId],[StartedAt],[FinishedAt],[AttemptStatus],[TotalScore],[CreatedAt])
            VALUES (NEWID(), @qId, @spId, DATEADD(DAY,-5,DATEADD(YEAR,-10+@class,@Now)), DATEADD(MINUTE,25,DATEADD(DAY,-5,DATEADD(YEAR,-10+@class,@Now))), N'Completed', 70+ABS(CHECKSUM(NEWID()))%31, @Now);
        FETCH NEXT FROM quizCur INTO @qId;
    END
    CLOSE quizCur; DEALLOCATE quizCur;

    -- 2e. Assignment submissions (2 per offering, with marks)
    DECLARE @aId UNIQUEIDENTIFIER;
    DECLARE assignCur CURSOR FOR 
        SELECT a.Id FROM [assignments] a
        JOIN [course_offerings] co ON co.Id = a.CourseOfferingId
        JOIN [semesters] s ON s.Id = co.SemesterId
        WHERE s.[Name] = @semName;
    OPEN assignCur; FETCH NEXT FROM assignCur INTO @aId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [assignment_submissions] WHERE [AssignmentId]=@aId AND [StudentProfileId]=@spId)
            INSERT INTO [assignment_submissions] ([Id],[AssignmentId],[StudentProfileId],[SubmittedAt],[Status],[MarksAwarded],[CreatedAt])
            VALUES (NEWID(), @aId, @spId, DATEADD(DAY,-2,DATEADD(YEAR,-10+@class,@Now)), N'Submitted', 60+ABS(CHECKSUM(NEWID()))%41, @Now);
        FETCH NEXT FROM assignCur INTO @aId;
    END
    CLOSE assignCur; DEALLOCATE assignCur;

    -- 2f. Payment receipt (paid in full)
    INSERT INTO [payment_receipts] ([Id],[StudentProfileId],[CreatedByUserId],[ReceiptNo],[Status],[Amount],[Description],[DueDate],[ConfirmedByUserId],[ConfirmedAt],[Notes],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(), @spId, ISNULL(@Fac_Sch,(SELECT TOP 1 Id FROM [users] WHERE RoleId=2)),
            CONCAT(N'RCP-DEMO-C',@class), 2, 5000.00,
            CONCAT(N'Class ',@class,N' Tuition Fee'),
            DATEADD(MONTH,1,DATEADD(YEAR,-10+@class,@Now)),
            ISNULL(@Fac_Sch,(SELECT TOP 1 Id FROM [users] WHERE RoleId=2)),
            DATEADD(YEAR,-10+@class,@Now),
            N'Paid in full', @Now, @Now, 0, NULL);

    SET @class += 1;
END

-- ════════════════════════════════════════════════
-- 3. UPDATE PROFILE: Mark as Class 10 completed
-- ════════════════════════════════════════════════
UPDATE [student_profiles] SET [CurrentSemesterNumber]=10, [Cgpa]=3.45 WHERE [Id]=@spId;
PRINT 'Profile updated: CGPA 3.45, Class 10 completed.';

-- ════════════════════════════════════════════════
-- 4. COURSE ANNOUNCEMENTS for Class 10
-- ════════════════════════════════════════════════
DECLARE @c10SemId UNIQUEIDENTIFIER = (SELECT Id FROM [semesters] WHERE [Name]=N'Class 10 (2026)');
DECLARE @c10OffId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [course_offerings] WHERE [SemesterId]=@c10SemId);

IF @c10OffId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [course_announcements] WHERE [OfferingId]=@c10OffId AND [AuthorId]=@uid)
    INSERT INTO [course_announcements] ([Id],[OfferingId],[AuthorId],[Title],[Body],[PostedAt],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@c10OffId,@uid,N'Final Exam Preparation',N'Please review chapters 1-10 for the final examination. Good luck!',@Now,@Now,0,NULL);

-- ════════════════════════════════════════════════
-- 5. DISCUSSION THREAD by Ahmad
-- ════════════════════════════════════════════════
IF @c10OffId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [discussion_threads] WHERE [OfferingId]=@c10OffId AND [AuthorId]=@uid)
BEGIN
    DECLARE @dtId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [discussion_threads] ([Id],[OfferingId],[Title],[AuthorId],[IsPinned],[IsClosed],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (@dtId,@c10OffId,N'Study tips for final exam',@uid,0,0,@Now,0,NULL);
    INSERT INTO [discussion_replies] ([Id],[ThreadId],[AuthorId],[Body],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@dtId,@Fac_Sch,N'Focus on past papers and practice problems. You will do great!',@Now,0,NULL);
END

-- ════════════════════════════════════════════════
-- 6. SUPPORT TICKET
-- ════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [support_tickets] WHERE [SubmitterId]=@uid)
    INSERT INTO [support_tickets] ([Id],[SubmitterId],[DepartmentId],[Category],[Subject],[Body],[Status],[ReopenWindowDays],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (NEWID(),@uid,@D_Sch,1,N'Certificate request',N'I have completed Class 10. Please generate my Completion Certificate and Report Card.',2,7,@Now,0,NULL);

-- ════════════════════════════════════════════════
-- 7. STUDY PLAN for next academic year
-- ════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM [study_plans] WHERE [StudentProfileId]=@spId)
BEGIN
    DECLARE @planId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [study_plans] ([Id],[StudentProfileId],[PlannedSemesterName],[Notes],[AdvisorStatus],[ReviewedByUserId],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES (@planId,@spId,N'College Year 1',N'Planning to enroll in ICS program at college.',1,@Fac_Sch,@Now,0,NULL);
    
    INSERT INTO [study_plan_courses] ([Id],[StudyPlanId],[CourseId],[CreatedAt])
    SELECT NEWID(), @planId, c.Id, @Now
    FROM [courses] c WHERE c.[Code] IN (N'ENG001',N'MTH001',N'SCI001')
      AND NOT EXISTS (SELECT 1 FROM [study_plan_courses] spc WHERE spc.StudyPlanId=@planId AND spc.CourseId=c.Id);
END

-- ════════════════════════════════════════════════
-- 8. NOTIFICATION for Ahmad
-- ════════════════════════════════════════════════
DECLARE @notifId UNIQUEIDENTIFIER = NEWID();
IF NOT EXISTS (SELECT 1 FROM [notifications] WHERE [Title]=N'Congratulations on Completing Class 10!')
BEGIN
    INSERT INTO [notifications] ([Id],[Title],[Body],[Type],[SenderUserId],[IsSystemGenerated],[IsActive],[CreatedAt])
    VALUES (@notifId,N'Congratulations on Completing Class 10!',N'Dear Ahmad, you have successfully completed all 10 classes. Your certificates are ready for download.',N'Academic',@Fac_Sch,0,1,@Now);
END
ELSE
    SET @notifId = (SELECT Id FROM [notifications] WHERE [Title]=N'Congratulations on Completing Class 10!');

IF NOT EXISTS (SELECT 1 FROM [notification_recipients] WHERE [NotificationId]=@notifId AND [RecipientUserId]=@uid)
    INSERT INTO [notification_recipients] ([Id],[NotificationId],[RecipientUserId],[IsRead],[ReadAt],[CreatedAt])
    VALUES (NEWID(),@notifId,@uid,0,NULL,@Now);

-- ════════════════════════════════════════════════
-- 9. SUMMARY
-- ════════════════════════════════════════════════
PRINT '';
PRINT '=== LIFECYCLE DEMO COMPLETE ===';
PRINT 'Student:    Ahmad Hassan (SCH-DEMO-001)';
PRINT 'Username:   demostudent';
PRINT 'Password:   (default demo password)';
PRINT 'School:     Science Program, Classes 1-10';
PRINT 'CGPA:       3.45';
PRINT '';
PRINT 'Enrollments: ' + CAST((SELECT COUNT(*) FROM [enrollments] WHERE [StudentProfileId]=@spId) AS NVARCHAR);
PRINT 'Attendance:  ' + CAST((SELECT COUNT(*) FROM [attendance_records] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' records';
PRINT 'Results:     ' + CAST((SELECT COUNT(*) FROM [results] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' published';
PRINT 'Quiz attempts: ' + CAST((SELECT COUNT(*) FROM [quiz_attempts] WHERE [StudentProfileId]=@spId) AS NVARCHAR);
PRINT 'Assignments:  ' + CAST((SELECT COUNT(*) FROM [assignment_submissions] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' submitted';
PRINT 'Payments:     ' + CAST((SELECT COUNT(*) FROM [payment_receipts] WHERE [StudentProfileId]=@spId) AS NVARCHAR) + ' receipts';
PRINT 'Login as:     demostudent to view the complete lifecycle.';
PRINT 'Certificates: Use Admin portal → Generate Certificates → Select Ahmad → Completion Certificate + Report Card';
GO
