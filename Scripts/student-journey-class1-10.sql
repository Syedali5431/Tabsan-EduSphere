-- ============================================================================
-- Student Journey: col11s6 (COL-REG-11-06) from Class 1 to Class 10
-- Creates: enrollments, results, attendance, assignments
-- ============================================================================
SET NOCOUNT ON;
GO

DECLARE @StudentProfileId UNIQUEIDENTIFIER = 'F5D93FFE-38F1-49AC-A718-25F415E09DD9';
DECLARE @FacultyUserId UNIQUEIDENTIFIER = '6D6AB857-A27A-4784-B7A8-000AD147A7E0';
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- ============================================================================
-- 1. Create basic courses if they don't exist
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM courses WHERE Id = 'CCCCCC01-0000-0000-0000-000000000001')
    INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, CreatedAt, IsDeleted, InstitutionType, TenantId)
    VALUES ('CCCCCC01-0000-0000-0000-000000000001', 'English', 'ENG-01', 3, 'D0000003-0000-0000-0000-000000000003', 1, @Now, 0, 1, '11111111-1111-1111-1111-111111111111');
IF NOT EXISTS (SELECT 1 FROM courses WHERE Id = 'CCCCCC02-0000-0000-0000-000000000002')
    INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, CreatedAt, IsDeleted, InstitutionType, TenantId)
    VALUES ('CCCCCC02-0000-0000-0000-000000000002', 'Mathematics', 'MATH-01', 3, 'D0000003-0000-0000-0000-000000000003', 1, @Now, 0, 1, '11111111-1111-1111-1111-111111111111');
IF NOT EXISTS (SELECT 1 FROM courses WHERE Id = 'CCCCCC03-0000-0000-0000-000000000003')
    INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, CreatedAt, IsDeleted, InstitutionType, TenantId)
    VALUES ('CCCCCC03-0000-0000-0000-000000000003', 'Science', 'SCI-01', 3, 'D0000003-0000-0000-0000-000000000003', 1, @Now, 0, 1, '11111111-1111-1111-1111-111111111111');
IF NOT EXISTS (SELECT 1 FROM courses WHERE Id = 'CCCCCC04-0000-0000-0000-000000000004')
    INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, CreatedAt, IsDeleted, InstitutionType, TenantId)
    VALUES ('CCCCCC04-0000-0000-0000-000000000004', 'Social Studies', 'SS-01', 2, 'D0000003-0000-0000-0000-000000000003', 1, @Now, 0, 1, '11111111-1111-1111-1111-111111111111');
IF NOT EXISTS (SELECT 1 FROM courses WHERE Id = 'CCCCCC05-0000-0000-0000-000000000005')
    INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, CreatedAt, IsDeleted, InstitutionType, TenantId)
    VALUES ('CCCCCC05-0000-0000-0000-000000000005', 'Urdu', 'URD-01', 2, 'D0000003-0000-0000-0000-000000000003', 1, @Now, 0, 1, '11111111-1111-1111-1111-111111111111');
IF NOT EXISTS (SELECT 1 FROM courses WHERE Id = 'CCCCCC06-0000-0000-0000-000000000006')
    INSERT INTO courses (Id, Title, Code, CreditHours, DepartmentId, IsActive, CreatedAt, IsDeleted, InstitutionType, TenantId)
    VALUES ('CCCCCC06-0000-0000-0000-000000000006', 'Islamiat', 'ISL-01', 2, 'D0000003-0000-0000-0000-000000000003', 1, @Now, 0, 1, '11111111-1111-1111-1111-111111111111');

-- ============================================================================
-- 2. Class semesters mapping
-- ============================================================================
DECLARE @Classes TABLE (ClassNum INT, SemesterId UNIQUEIDENTIFIER);
INSERT INTO @Classes VALUES
(1,  'A868353C-19A4-4581-8FED-51C7D4027168'),
(2,  '3A287690-E173-43B2-8FBE-5EBFE147FDD9'),
(3,  'E679DDCD-49A0-40AE-8637-C3F57CCB8057'),
(4,  '9F7A9466-75C7-4B68-87CC-EA82186C941F'),
(5,  'CDA82342-0266-4789-A82B-D9A97225DC6F'),
(6,  'C883B765-4467-461F-887D-F241619A9CEB'),
(7,  '021FEAA9-29B6-470E-86B1-B71FCFAB475A'),
(8,  '9EA0ED24-BDB5-4CAA-90BE-5628C01F7DE2'),
(9,  '806214B5-806B-4DC7-AE39-86C74242FB7E'),
(10, '97426850-2A47-4F45-A599-3342A41CE039');

DECLARE @CourseIds TABLE (Idx INT, CourseId UNIQUEIDENTIFIER);
INSERT INTO @CourseIds VALUES
(0, 'CCCCCC01-0000-0000-0000-000000000001'),
(1, 'CCCCCC02-0000-0000-0000-000000000002'),
(2, 'CCCCCC03-0000-0000-0000-000000000003'),
(3, 'CCCCCC04-0000-0000-0000-000000000004'),
(4, 'CCCCCC05-0000-0000-0000-000000000005'),
(5, 'CCCCCC06-0000-0000-0000-000000000006');

-- ============================================================================
-- 3. Create course offerings for classes that don't have them
-- ============================================================================
DECLARE @SemesterId UNIQUEIDENTIFIER;
DECLARE @ClassNum INT;
DECLARE @OfferingId UNIQUEIDENTIFIER;
DECLARE @cid UNIQUEIDENTIFIER;
DECLARE @offIdx INT;

DECLARE class_cursor CURSOR FOR SELECT ClassNum, SemesterId FROM @Classes ORDER BY ClassNum;
OPEN class_cursor;
FETCH NEXT FROM class_cursor INTO @ClassNum, @SemesterId;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM course_offerings WHERE SemesterId = @SemesterId AND IsDeleted = 0)
    BEGIN
        SET @offIdx = 0;
        WHILE @offIdx < 3
        BEGIN
            SET @OfferingId = NEWID();
            SELECT @cid = CourseId FROM @CourseIds WHERE Idx = (@ClassNum + @offIdx) % 6;

            INSERT INTO course_offerings (Id, CourseId, SemesterId, FacultyUserId, MaxEnrollment, IsOpen, CreatedAt, IsDeleted, InstitutionType, TenantId, CampusId)
            VALUES (@OfferingId, @cid, @SemesterId, @FacultyUserId, 50, 1, @Now, 0, 1, '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222');

            SET @offIdx = @offIdx + 1;
        END
    END
    FETCH NEXT FROM class_cursor INTO @ClassNum, @SemesterId;
END
CLOSE class_cursor;
DEALLOCATE class_cursor;

-- ============================================================================
-- 4. ENROLL student in each offering
-- ============================================================================
DECLARE enroll_cursor CURSOR FOR 
    SELECT co.Id
    FROM course_offerings co
    JOIN @Classes cl ON cl.SemesterId = co.SemesterId
    WHERE co.IsDeleted = 0
      AND NOT EXISTS (
        SELECT 1 FROM enrollments e 
        WHERE e.StudentProfileId = @StudentProfileId AND e.CourseOfferingId = co.Id
    )
    ORDER BY cl.ClassNum;

OPEN enroll_cursor;
FETCH NEXT FROM enroll_cursor INTO @OfferingId;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO enrollments (Id, StudentProfileId, CourseOfferingId, EnrolledAt, Status, CreatedAt)
    VALUES (NEWID(), @StudentProfileId, @OfferingId, DATEADD(DAY, -180, @Now), 'Enrolled', @Now);
    FETCH NEXT FROM enroll_cursor INTO @OfferingId;
END
CLOSE enroll_cursor;
DEALLOCATE enroll_cursor;

-- ============================================================================
-- 5. RESULTS: Create results for each enrollment
-- ============================================================================
DECLARE @MarksObtained INT;
DECLARE @GradePoint DECIMAL(3,2);

DECLARE result_cursor CURSOR FOR 
    SELECT co.Id, cl.ClassNum
    FROM enrollments e
    JOIN course_offerings co ON co.Id = e.CourseOfferingId
    JOIN @Classes cl ON cl.SemesterId = co.SemesterId
    WHERE e.StudentProfileId = @StudentProfileId
      AND NOT EXISTS (SELECT 1 FROM results r WHERE r.StudentProfileId = @StudentProfileId AND r.CourseOfferingId = co.Id AND r.ResultType = 'Final')
    ORDER BY cl.ClassNum;

OPEN result_cursor;
FETCH NEXT FROM result_cursor INTO @OfferingId, @ClassNum;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @MarksObtained = 65 + (10 - @ClassNum) * 2 + (ABS(CHECKSUM(NEWID())) % 15);
    IF @MarksObtained > 100 SET @MarksObtained = 100;
    
    SET @GradePoint = CASE 
        WHEN @MarksObtained >= 90 THEN 4.0
        WHEN @MarksObtained >= 80 THEN 3.5
        WHEN @MarksObtained >= 70 THEN 3.0
        WHEN @MarksObtained >= 60 THEN 2.5
        WHEN @MarksObtained >= 50 THEN 2.0
        ELSE 0.0
    END;

    INSERT INTO results (Id, StudentProfileId, CourseOfferingId, ResultType, MarksObtained, MaxMarks, GradePoint, IsPublished, PublishedAt, PublishedByUserId, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @StudentProfileId, @OfferingId, 'Final', @MarksObtained, 100, @GradePoint, 1, @Now, @FacultyUserId, @Now, @Now);

    FETCH NEXT FROM result_cursor INTO @OfferingId, @ClassNum;
END
CLOSE result_cursor;
DEALLOCATE result_cursor;

-- ============================================================================
-- 6. ATTENDANCE: Create attendance for 60 school days per class
-- ============================================================================
DECLARE @AttDay INT;
DECLARE @AttDate DATE;
DECLARE @AttStatus NVARCHAR(20);

DECLARE att_cursor CURSOR FOR 
    SELECT co.Id, cl.ClassNum
    FROM course_offerings co
    JOIN @Classes cl ON cl.SemesterId = co.SemesterId
    WHERE co.IsDeleted = 0
      AND EXISTS (SELECT 1 FROM enrollments e WHERE e.StudentProfileId = @StudentProfileId AND e.CourseOfferingId = co.Id)
    ORDER BY cl.ClassNum;

OPEN att_cursor;
FETCH NEXT FROM att_cursor INTO @OfferingId, @ClassNum;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @AttDate = DATEADD(DAY, -365 + (@ClassNum - 1) * 30, CAST(@Now AS DATE));
    SET @AttDay = 0;
    
    WHILE @AttDay < 60
    BEGIN
        SET @AttDate = DATEADD(DAY, 1, @AttDate);
        IF DATEPART(WEEKDAY, @AttDate) IN (1, 7) CONTINUE;
        
        SET @AttStatus = CASE WHEN (ABS(CHECKSUM(NEWID())) % 100) < 88 THEN 'Present' 
                              WHEN (ABS(CHECKSUM(NEWID())) % 100) < 50 THEN 'Absent'
                              ELSE 'Late' END;

        IF NOT EXISTS (SELECT 1 FROM attendance_records a WHERE a.StudentProfileId = @StudentProfileId AND a.CourseOfferingId = @OfferingId AND a.Date = @AttDate)
        BEGIN
            INSERT INTO attendance_records (Id, StudentProfileId, CourseOfferingId, Date, Status, MarkedByUserId, CreatedAt)
            VALUES (NEWID(), @StudentProfileId, @OfferingId, @AttDate, @AttStatus, @FacultyUserId, @Now);
        END
        
        SET @AttDay = @AttDay + 1;
    END

    FETCH NEXT FROM att_cursor INTO @OfferingId, @ClassNum;
END
CLOSE att_cursor;
DEALLOCATE att_cursor;

-- ============================================================================
-- 7. ASSIGNMENTS: Create 3 assignments per class with graded submissions
-- ============================================================================
DECLARE @AssignId UNIQUEIDENTIFIER;
DECLARE @AssignNum INT;
DECLARE @AssignTitle NVARCHAR(200);
DECLARE @DueDate DATETIME2;
DECLARE @MaxAssignMarks DECIMAL(5,2);
DECLARE @MarksAwarded DECIMAL(5,2);

DECLARE assign_cursor CURSOR FOR 
    SELECT co.Id, cl.ClassNum
    FROM course_offerings co
    JOIN @Classes cl ON cl.SemesterId = co.SemesterId
    WHERE co.IsDeleted = 0
      AND EXISTS (SELECT 1 FROM enrollments e WHERE e.StudentProfileId = @StudentProfileId AND e.CourseOfferingId = co.Id)
    ORDER BY cl.ClassNum;

OPEN assign_cursor;
FETCH NEXT FROM assign_cursor INTO @OfferingId, @ClassNum;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @AssignNum = 1;
    WHILE @AssignNum <= 3
    BEGIN
        SET @AssignTitle = CONCAT('Assignment ', @AssignNum, ' - Class ', @ClassNum);
        SET @DueDate = DATEADD(DAY, @AssignNum * 20, DATEADD(DAY, -365 + (@ClassNum - 1) * 30, CAST(@Now AS DATETIME2)));
        SET @MaxAssignMarks = 20 + (@ClassNum * 5);
        SET @MarksAwarded = @MaxAssignMarks * (0.60 + (ABS(CHECKSUM(NEWID())) % 35) / 100.0);
        IF @MarksAwarded > @MaxAssignMarks SET @MarksAwarded = @MaxAssignMarks;
        
        SET @AssignId = NEWID();

        IF NOT EXISTS (SELECT 1 FROM assignments a WHERE a.CourseOfferingId = @OfferingId AND a.Title = @AssignTitle AND a.IsDeleted = 0)
        BEGIN
            INSERT INTO assignments (Id, CourseOfferingId, Title, Description, DueDate, MaxMarks, IsPublished, PublishedAt, CreatedAt, IsDeleted)
            VALUES (@AssignId, @OfferingId, @AssignTitle, CONCAT('Assignment for Class ', @ClassNum, ' - Topic ', @AssignNum), @DueDate, @MaxAssignMarks, 1, @Now, @Now, 0);

            INSERT INTO assignment_submissions (Id, AssignmentId, StudentProfileId, TextContent, SubmittedAt, MarksAwarded, Feedback, Status, GradedAt, GradedByUserId, CreatedAt)
            VALUES (NEWID(), @AssignId, @StudentProfileId, CONCAT('Completed submission for ', @AssignTitle), DATEADD(DAY, -1, @DueDate), @MarksAwarded, 'Good work!', 'Graded', @DueDate, @FacultyUserId, @Now);
        END

        SET @AssignNum = @AssignNum + 1;
    END

    FETCH NEXT FROM assign_cursor INTO @OfferingId, @ClassNum;
END
CLOSE assign_cursor;
DEALLOCATE assign_cursor;

-- ============================================================================
-- 8. Update student profile: CGPA and current semester
-- ============================================================================
UPDATE student_profiles
SET Cgpa = ISNULL((SELECT AVG(CAST(r.GradePoint AS DECIMAL(4,2))) FROM results r WHERE r.StudentProfileId = @StudentProfileId AND r.IsPublished = 1), 0),
    CurrentSemesterNumber = 10,
    UpdatedAt = @Now
WHERE Id = @StudentProfileId;

PRINT '========================================';
PRINT 'Student journey Class 1-10 created!';
PRINT CONCAT('Student: COL-REG-11-06', ' (col11s6)');
PRINT '========================================';
GO
