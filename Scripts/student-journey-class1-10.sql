-- ============================================================================
-- Student Journey: col11s6 (COL-REG-11-06) from Class 1 to Class 10
-- School student, Information Technology department
-- Creates: semesters, course offerings, enrollments, results, attendance, assignments
--
-- Certificate Eligibility Rules:
--   - Completion Certificate: Requires Class 10 completion + attendance >= 85%
--   - Report Card: Available for any student with results
--   - Transcript: Blocked for School/College (University only)
--   - Degree: Blocked for non-University institutions
--
-- Marks follow realistic progression: strong start (~92%) declining to ~74% by Class 10
-- ============================================================================
SET NOCOUNT ON;
GO

USE [Tabsan-EduSphere];
GO

DECLARE @StudentProfileId UNIQUEIDENTIFIER = 'F5D93FFE-38F1-49AC-A718-25F415E09DD9';
DECLARE @FacultyUserId UNIQUEIDENTIFIER = 'B56A573F-A01D-4691-AB15-112CABE163B5'; -- faculty.uni.it1
DECLARE @DeptId UNIQUEIDENTIFIER = 'D0000003-0000-0000-0000-000000000003';
DECLARE @TenantId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

PRINT '=== Student Journey: col11s6 Class 1-10 ===';
PRINT '';

-- Clean any previous journey data for this student (correct FK order)
DELETE FROM assignment_submissions WHERE AssignmentId IN (SELECT a.Id FROM assignments a INNER JOIN course_offerings co ON co.Id = a.CourseOfferingId INNER JOIN enrollments e ON e.CourseOfferingId = co.Id WHERE e.StudentProfileId = @StudentProfileId);
DELETE FROM assignments WHERE CourseOfferingId IN (SELECT co.Id FROM course_offerings co INNER JOIN enrollments e ON e.CourseOfferingId = co.Id WHERE e.StudentProfileId = @StudentProfileId);
DELETE FROM attendance_records WHERE StudentProfileId = @StudentProfileId;
DELETE FROM results WHERE StudentProfileId = @StudentProfileId;
DELETE FROM enrollments WHERE StudentProfileId = @StudentProfileId;
-- Must delete child tables before parents due to FK constraints
DELETE FROM course_offerings WHERE SemesterId IN (SELECT Id FROM semesters WHERE Name LIKE 'Class [0-9]%');
DELETE FROM timetable_entries WHERE TimetableId IN (SELECT Id FROM timetables WHERE SemesterId IN (SELECT Id FROM semesters WHERE Name LIKE 'Class [0-9]%'));
DELETE FROM timetables WHERE SemesterId IN (SELECT Id FROM semesters WHERE Name LIKE 'Class [0-9]%');
DELETE FROM semesters WHERE Name LIKE 'Class [0-9]%';
DELETE FROM courses WHERE DepartmentId = @DeptId AND (Code LIKE '%-G[0-9]' OR Code LIKE '%-G[0-9][0-9]');

PRINT 'Cleaned previous journey data.';
PRINT '';

-- ============================================================================
-- 1. Create courses for each class (School-style: single annual exam per class)
-- ============================================================================
PRINT '--- Creating courses ---';

-- Courses array: CourseId, Code, Title, CreditHours
DECLARE @Courses TABLE (RowId INT IDENTITY, CourseId UNIQUEIDENTIFIER, Code NVARCHAR(20), Title NVARCHAR(200), CreditHours INT);

-- Core school subjects (zero-padded grade numbers: G01, G02, ..., G10)
INSERT INTO @Courses (CourseId, Code, Title, CreditHours) VALUES
(NEWID(), N'ENG-G01', N'English Grade 1', 3),
(NEWID(), N'ENG-G02', N'English Grade 2', 3),
(NEWID(), N'ENG-G03', N'English Grade 3', 3),
(NEWID(), N'ENG-G04', N'English Grade 4', 3),
(NEWID(), N'ENG-G05', N'English Grade 5', 3),
(NEWID(), N'ENG-G06', N'English Grade 6', 3),
(NEWID(), N'ENG-G07', N'English Grade 7', 3),
(NEWID(), N'ENG-G08', N'English Grade 8', 3),
(NEWID(), N'ENG-G09', N'English Grade 9', 3),
(NEWID(), N'ENG-G10', N'English Grade 10', 3);

INSERT INTO @Courses (CourseId, Code, Title, CreditHours) VALUES
(NEWID(), N'MTH-G01', N'Mathematics Grade 1', 3),
(NEWID(), N'MTH-G02', N'Mathematics Grade 2', 3),
(NEWID(), N'MTH-G03', N'Mathematics Grade 3', 3),
(NEWID(), N'MTH-G04', N'Mathematics Grade 4', 3),
(NEWID(), N'MTH-G05', N'Mathematics Grade 5', 3),
(NEWID(), N'MTH-G06', N'Mathematics Grade 6', 3),
(NEWID(), N'MTH-G07', N'Mathematics Grade 7', 3),
(NEWID(), N'MTH-G08', N'Mathematics Grade 8', 3),
(NEWID(), N'MTH-G09', N'Mathematics Grade 9', 3),
(NEWID(), N'MTH-G10', N'Mathematics Grade 10', 3);

INSERT INTO @Courses (CourseId, Code, Title, CreditHours) VALUES
(NEWID(), N'SCI-G01', N'Science Grade 1', 3),
(NEWID(), N'SCI-G02', N'Science Grade 2', 3),
(NEWID(), N'SCI-G03', N'Science Grade 3', 3),
(NEWID(), N'SCI-G04', N'Science Grade 4', 3),
(NEWID(), N'SCI-G05', N'Science Grade 5', 3),
(NEWID(), N'SCI-G06', N'Science Grade 6', 3),
(NEWID(), N'SCI-G07', N'Science Grade 7', 3),
(NEWID(), N'SCI-G08', N'Science Grade 8', 3),
(NEWID(), N'SCI-G09', N'Science Grade 9', 3),
(NEWID(), N'SCI-G10', N'Science Grade 10', 3);

INSERT INTO @Courses (CourseId, Code, Title, CreditHours) VALUES
(NEWID(), N'SST-G01', N'Social Studies Grade 1', 2),
(NEWID(), N'SST-G02', N'Social Studies Grade 2', 2),
(NEWID(), N'SST-G03', N'Social Studies Grade 3', 2),
(NEWID(), N'SST-G04', N'Social Studies Grade 4', 2),
(NEWID(), N'SST-G05', N'Social Studies Grade 5', 2),
(NEWID(), N'SST-G06', N'Social Studies Grade 6', 2),
(NEWID(), N'SST-G07', N'Social Studies Grade 7', 2),
(NEWID(), N'SST-G08', N'Social Studies Grade 8', 2),
(NEWID(), N'SST-G09', N'Social Studies Grade 9', 2),
(NEWID(), N'SST-G10', N'Social Studies Grade 10', 2);

INSERT INTO @Courses (CourseId, Code, Title, CreditHours) VALUES
(NEWID(), N'URD-G01', N'Urdu Grade 1', 2),
(NEWID(), N'URD-G02', N'Urdu Grade 2', 2),
(NEWID(), N'URD-G03', N'Urdu Grade 3', 2),
(NEWID(), N'URD-G04', N'Urdu Grade 4', 2),
(NEWID(), N'URD-G05', N'Urdu Grade 5', 2),
(NEWID(), N'URD-G06', N'Urdu Grade 6', 2),
(NEWID(), N'URD-G07', N'Urdu Grade 7', 2),
(NEWID(), N'URD-G08', N'Urdu Grade 8', 2),
(NEWID(), N'URD-G09', N'Urdu Grade 9', 2),
(NEWID(), N'URD-G10', N'Urdu Grade 10', 2);

-- Insert courses, then sync @Courses with actual DB IDs
INSERT INTO [courses] ([Id],[Title],[Code],[CreditHours],[DepartmentId],[IsActive],[CreatedAt],[IsDeleted],[InstitutionType],[TenantId],[CampusId],[GradingType],[HasSemesters])
SELECT c.CourseId, c.Title, c.Code, c.CreditHours, @DeptId, 1, @Now, 0, 1, @TenantId, NULL, N'Percentage', 0
FROM @Courses c
WHERE NOT EXISTS (SELECT 1 FROM [courses] WHERE [Code] = c.Code);

-- Sync @Courses IDs with actual database IDs
UPDATE c
SET c.CourseId = db.Id
FROM @Courses c
INNER JOIN [courses] db ON db.Code = c.Code;

DECLARE @CourseVerifyCount INT = (SELECT COUNT(*) FROM @Courses);
PRINT 'Courses created/verified: ' + CAST(@CourseVerifyCount AS NVARCHAR);

-- ============================================================================
-- 2. Create semesters (one per class, named "Class 1" through "Class 10")
-- ============================================================================
PRINT '--- Creating semesters ---';

DECLARE @ClassNum INT = 1;
DECLARE @SemId UNIQUEIDENTIFIER;
DECLARE @SemIds TABLE (ClassNum INT, SemId UNIQUEIDENTIFIER);

WHILE @ClassNum <= 10
BEGIN
    SET @SemId = NEWID();
    INSERT INTO @SemIds (ClassNum, SemId) VALUES (@ClassNum, @SemId);
    
    INSERT INTO [semesters] ([Id],[Name],[StartDate],[EndDate],[IsClosed],[CreatedAt],[IsDeleted])
    VALUES (@SemId, CONCAT(N'Class ', @ClassNum),
        DATEFROMPARTS(2015 + @ClassNum, 4, 1),
        DATEFROMPARTS(2016 + @ClassNum, 3, 31),
        1, @Now, 0);

    SET @ClassNum += 1;
END

DECLARE @SemCreateCount INT = (SELECT COUNT(*) FROM @SemIds);
PRINT 'Semesters created: ' + CAST(@SemCreateCount AS NVARCHAR);

-- ============================================================================
-- 3. Create course offerings and enrollments for each class
-- ============================================================================
PRINT '--- Creating offerings and enrollments ---';

DECLARE @ClassCursor INT = 1;
DECLARE @SemCursor UNIQUEIDENTIFIER;
DECLARE @OffId UNIQUEIDENTIFIER;
DECLARE @CourseId UNIQUEIDENTIFIER;
DECLARE @CoCode NVARCHAR(20);
DECLARE @GradCodeSuffix NVARCHAR(10);

DECLARE @OfferingIds TABLE (ClassNum INT, OffId UNIQUEIDENTIFIER, CourseId UNIQUEIDENTIFIER, Code NVARCHAR(20));

DECLARE curClass CURSOR FOR SELECT ClassNum, SemId FROM @SemIds ORDER BY ClassNum;
OPEN curClass;
FETCH NEXT FROM curClass INTO @ClassCursor, @SemCursor;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @GradCodeSuffix = CONCAT('%-G', RIGHT(CONCAT('0',@ClassCursor),2));

    DECLARE curCourse CURSOR FOR 
        SELECT CourseId, Code FROM @Courses 
        WHERE Code LIKE @GradCodeSuffix
        ORDER BY Code;

    OPEN curCourse;
    FETCH NEXT FROM curCourse INTO @CourseId, @CoCode;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @OffId = NEWID();

        -- Course offering
        INSERT INTO [course_offerings] ([Id],[CourseId],[SemesterId],[FacultyUserId],[MaxEnrollment],[IsOpen],[CreatedAt],[IsDeleted],[InstitutionType],[TenantId],[CampusId])
        VALUES (@OffId, @CourseId, @SemCursor, @FacultyUserId, 50, 0, @Now, 0, 1, @TenantId, NULL);

        INSERT INTO @OfferingIds (ClassNum, OffId, CourseId, Code) VALUES (@ClassCursor, @OffId, @CourseId, @CoCode);

        -- Enrollment
        INSERT INTO [enrollments] ([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
        VALUES (NEWID(), @StudentProfileId, @OffId, DATEADD(DAY, -365*(11-@ClassCursor), @Now), N'Active', @Now);

        FETCH NEXT FROM curCourse INTO @CourseId, @CoCode;
    END
    CLOSE curCourse; DEALLOCATE curCourse;

    FETCH NEXT FROM curClass INTO @ClassCursor, @SemCursor;
END
CLOSE curClass; DEALLOCATE curClass;

DECLARE @OffCreateCount INT = (SELECT COUNT(*) FROM @OfferingIds);
PRINT 'Offerings created: ' + CAST(@OffCreateCount AS NVARCHAR);

-- ============================================================================
-- 4. Create results with realistic declining marks
-- ============================================================================
PRINT '--- Creating results ---';

-- 11-Tier Grading Scale (Percentage → Grade):
--   >=90%  A+    85-89%  A     80-84%  A-    75-79%  B+
--   70-74%  B     65-69%  B-    60-64%  C+    55-59%  C
--   50-54%  C-    40-49%  D     <40%    F
--
-- Target percentages per class (declining: A+ → A → A → A- → B+ → B → B- → C+ → C → C-):
DECLARE @Results TABLE (ClassNum INT, TargetPct DECIMAL(5,2), ExpectedGrade NVARCHAR(3));
-- All classes target ~92% for a strong student profile (Grade A/A+ across the board)
INSERT INTO @Results VALUES
(1, 94.0, N'A+'), (2, 93.0, N'A+'), (3, 93.0, N'A+'), (4, 92.0, N'A+'), (5, 92.0, N'A+'),
(6, 92.0, N'A+'), (7, 91.0, N'A+'), (8, 91.0, N'A+'), (9, 90.0, N'A+'), (10, 92.0, N'A+');

DECLARE @ResCursor INT = 1;
DECLARE @TargetPct DECIMAL(5,2);
DECLARE @MarksObtained DECIMAL(8,2);
DECLARE @MaxMarks DECIMAL(8,2) = 100.0;
DECLARE @ResultCount INT = 0;

DECLARE curRes CURSOR FOR SELECT ClassNum, TargetPct FROM @Results ORDER BY ClassNum;
OPEN curRes;
FETCH NEXT FROM curRes INTO @ResCursor, @TargetPct;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE curOff CURSOR FOR 
        SELECT OffId, Code FROM @OfferingIds WHERE ClassNum = @ResCursor ORDER BY Code;
    DECLARE @ROffId UNIQUEIDENTIFIER;
    DECLARE @RCode NVARCHAR(20);

    OPEN curOff;
    FETCH NEXT FROM curOff INTO @ROffId, @RCode;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Add small random variation (+-3%) to target percentage
        SET @MarksObtained = ROUND(@TargetPct + (ABS(CHECKSUM(NEWID())) % 7) - 3, 0);
        IF @MarksObtained > 100 SET @MarksObtained = 100;
        IF @MarksObtained < 33 SET @MarksObtained = 33;

        INSERT INTO [results] ([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
        VALUES (NEWID(), @StudentProfileId, @ROffId, N'Final', @MarksObtained, @MaxMarks, 1, @Now, @FacultyUserId, @Now);

        SET @ResultCount += 1;
        FETCH NEXT FROM curOff INTO @ROffId, @RCode;
    END
    CLOSE curOff; DEALLOCATE curOff;

    FETCH NEXT FROM curRes INTO @ResCursor, @TargetPct;
END
CLOSE curRes; DEALLOCATE curRes;

PRINT 'Results created: ' + CAST(@ResultCount AS NVARCHAR);

-- ============================================================================
-- 5. Create attendance records (90%+ attendance)
-- ============================================================================
PRINT '--- Creating attendance ---';

DECLARE @AttClassNum INT = 1;
DECLARE @AttOffId UNIQUEIDENTIFIER;
DECLARE @AttDay INT;
DECLARE @AttStatus NVARCHAR(10);
DECLARE @AttSemId UNIQUEIDENTIFIER;
DECLARE @AttCount INT = 0;

DECLARE curAttClass CURSOR FOR SELECT ClassNum, SemId FROM @SemIds ORDER BY ClassNum;
OPEN curAttClass;
FETCH NEXT FROM curAttClass INTO @AttClassNum, @AttSemId;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE curAttOff CURSOR FOR SELECT OffId FROM @OfferingIds WHERE ClassNum = @AttClassNum;
    OPEN curAttOff;
    FETCH NEXT FROM curAttOff INTO @AttOffId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- 180 school days per year, ~90% attendance = ~162 present days
        SET @AttDay = 1;
        WHILE @AttDay <= 180
        BEGIN
            SET @AttStatus = CASE WHEN ABS(CHECKSUM(NEWID())) % 100 < 90 THEN N'Present' ELSE N'Absent' END;

            INSERT INTO [attendance_records] ([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt])
            VALUES (NEWID(), @StudentProfileId, @AttOffId,
                DATEADD(DAY, @AttDay, DATEFROMPARTS(2015 + @AttClassNum, 4, 1)),
                @AttStatus, @FacultyUserId, @Now);
            SET @AttCount += 1;
            SET @AttDay += 1;
        END

        FETCH NEXT FROM curAttOff INTO @AttOffId;
    END
    CLOSE curAttOff; DEALLOCATE curAttOff;

    FETCH NEXT FROM curAttClass INTO @AttClassNum, @AttSemId;
END
CLOSE curAttClass; DEALLOCATE curAttClass;

PRINT 'Attendance records created: ' + CAST(@AttCount AS NVARCHAR);

-- ============================================================================
-- 6. Create assignments (4 per course per class)
-- ============================================================================
PRINT '--- Creating assignments ---';

DECLARE @AsgnOffId UNIQUEIDENTIFIER;
DECLARE @AsgnClassNum INT;
DECLARE @AsgnCount INT = 0;
DECLARE @AsgnTitles TABLE (Seq INT, Title NVARCHAR(300));
INSERT INTO @AsgnTitles VALUES (1, N'Homework Assignment'), (2, N'Mid-Term Project'), (3, N'Practical Exercise'), (4, N'Final Project');

DECLARE curAsgn CURSOR FOR SELECT OffId, ClassNum FROM @OfferingIds ORDER BY ClassNum;
OPEN curAsgn;
FETCH NEXT FROM curAsgn INTO @AsgnOffId, @AsgnClassNum;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @AsgnSeq INT = 1;
    WHILE @AsgnSeq <= 4
    BEGIN
        DECLARE @AsgnTitle NVARCHAR(300) = (SELECT Title FROM @AsgnTitles WHERE Seq = @AsgnSeq);
        INSERT INTO [assignments] ([Id],[CourseOfferingId],[Title],[Description],[DueDate],[MaxMarks],[IsPublished],[PublishedAt],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(), @AsgnOffId,
            CONCAT(@AsgnTitle, N' - Class ', @AsgnClassNum),
            CONCAT(N'Assignment ', @AsgnSeq, N' for Class ', @AsgnClassNum),
            DATEADD(DAY, 30*@AsgnSeq, DATEFROMPARTS(2015 + @AsgnClassNum, 4, 1)),
            50.0, 1, @Now, @Now, 0, NULL);
        SET @AsgnCount += 1;
        SET @AsgnSeq += 1;
    END

    FETCH NEXT FROM curAsgn INTO @AsgnOffId, @AsgnClassNum;
END
CLOSE curAsgn; DEALLOCATE curAsgn;

PRINT 'Assignments created: ' + CAST(@AsgnCount AS NVARCHAR);

-- ============================================================================
-- 7. Summary
-- ============================================================================
PRINT '';
PRINT '=== Journey Complete ===';
PRINT 'Student: col11s6 (COL-REG-11-06)';
PRINT 'Classes: 1 through 10';
PRINT 'Courses per class: 5';
DECLARE @TotalCourses INT = (SELECT COUNT(DISTINCT CourseId) FROM @OfferingIds);
PRINT 'Total courses: ' + CAST(@TotalCourses AS NVARCHAR);
DECLARE @EnrollCount INT = (SELECT COUNT(*) FROM enrollments WHERE StudentProfileId = @StudentProfileId);
PRINT 'Enrollments: ' + CAST(@EnrollCount AS NVARCHAR);
DECLARE @ResCountFinal INT = (SELECT COUNT(*) FROM results WHERE StudentProfileId = @StudentProfileId);
PRINT 'Results: ' + CAST(@ResCountFinal AS NVARCHAR);
DECLARE @AttCountFinal INT = (SELECT COUNT(*) FROM attendance_records WHERE StudentProfileId = @StudentProfileId);
PRINT 'Attendance: ' + CAST(@AttCountFinal AS NVARCHAR);
DECLARE @AsgnCountFinal INT = (SELECT COUNT(*) FROM assignments WHERE CourseOfferingId IN (SELECT CourseOfferingId FROM enrollments WHERE StudentProfileId = @StudentProfileId));
PRINT 'Assignments: ' + CAST(@AsgnCountFinal AS NVARCHAR);
PRINT '';
PRINT 'Ready for certificate generation!';
