/*
  03-FullDummyData.sql — Tabsan EduSphere v2.0
  =============================================
  Clean, structured dummy data for all three institutes.
  Idempotent — safe to re-run. Depends on 01-Schema + 02-Seed-Core.

  INSTITUTES:
    University (TABSAN-UNI): IT/BSCS(8 sem) + BUS/BBA(8 sem) + SPA/Spanish(1 yr)
    School     (TABSAN-SCH): SCI (Class 1-10, 10 students/class)
    College    (TABSAN-COL): ICS (Class 11-12, 10 students/class)

  RESULTS RULES:
    - No results for Semester 1 / Class 1 / Class 11
    - Semester N → results for semesters 1..N-1 (cumulative)

  PASSWORD: EduSphere147 (Argon2id)
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [Tabsan-EduSphere];
GO

DECLARE @Now    DATETIME2 = SYSUTCDATETIME();
DECLARE @DefPwd NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';

-- ═══════════════════════════════════════════════════════════════════
-- 0. CONSTANTS
-- ═══════════════════════════════════════════════════════════════════
DECLARE @T_Uni UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @T_Col UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @T_Sch UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @C_Uni UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
DECLARE @C_Col UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB';
DECLARE @C_Sch UNIQUEIDENTIFIER = 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC';
DECLARE @RF INT = 3; -- Role Faculty
DECLARE @RS INT = 4; -- Role Student

-- ═══════════════════════════════════════════════════════════════════
-- 1. FIX DEPARTMENT InstitutionType (correct 02-Seed-Core values)
--    University=0  School=1  College=2
-- ═══════════════════════════════════════════════════════════════════
UPDATE [departments] SET [InstitutionType] = 0, [TenantId] = @T_Uni, [CampusId] = @C_Uni WHERE [Code] IN ('IT','BUS');
UPDATE [departments] SET [InstitutionType] = 1, [TenantId] = @T_Sch, [CampusId] = @C_Sch WHERE [Code] = 'SCI';
UPDATE [departments] SET [InstitutionType] = 2, [TenantId] = @T_Col, [CampusId] = @C_Col WHERE [Code] = 'IT-COL';

-- Add Spanish department (separate from SCI which uses D0000004)
IF NOT EXISTS (SELECT 1 FROM [departments] WHERE [Code] = 'SPA')
    INSERT INTO [departments] ([Id],[Name],[Code],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType])
    VALUES ('D0000005-0000-0000-0000-000000000005', N'Spanish Language', N'SPA', 1, 0, @Now, @T_Uni, @C_Uni, 0);

DECLARE @D_IT  UNIQUEIDENTIFIER = 'D0000001-0000-0000-0000-000000000001';
DECLARE @D_BUS UNIQUEIDENTIFIER = 'D0000002-0000-0000-0000-000000000002';
DECLARE @D_SCH UNIQUEIDENTIFIER = (SELECT Id FROM [departments] WHERE [Code]='SCI');
DECLARE @D_COL UNIQUEIDENTIFIER = (SELECT Id FROM [departments] WHERE [Code]='IT-COL');
DECLARE @D_SPA UNIQUEIDENTIFIER = (SELECT Id FROM [departments] WHERE [Code]='SPA');

-- Fix Spanish program department
UPDATE [academic_programs] SET [DepartmentId] = @D_SPA WHERE [Code] = 'SPANISH';

DECLARE @P_BSCS    UNIQUEIDENTIFIER = 'A0000001-0000-0000-0000-000000000001';
DECLARE @P_BBA     UNIQUEIDENTIFIER = 'A0000002-0000-0000-0000-000000000002';
DECLARE @P_SPANISH UNIQUEIDENTIFIER = 'A0000004-0000-0000-0000-000000000004';
DECLARE @P_ICS     UNIQUEIDENTIFIER = 'A0000005-0000-0000-0000-000000000005';
DECLARE @P_SCIENCE UNIQUEIDENTIFIER = 'A0000006-0000-0000-0000-000000000006';

-- ═══════════════════════════════════════════════════════════════════
-- 2. CLEAN EXISTING DUMMY DATA (safe cascade)
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Cleaning existing dummy data...';
DELETE FROM [rubric_student_grades]; DELETE FROM [rubric_levels]; DELETE FROM [rubric_criteria]; DELETE FROM [rubrics];
DELETE FROM [quiz_answers]; DELETE FROM [quiz_options]; DELETE FROM [quiz_attempts]; DELETE FROM [quiz_questions]; DELETE FROM [quizzes];
DELETE FROM [assignment_submissions]; DELETE FROM [assignments];
DELETE FROM [fyp_panel_members]; DELETE FROM [fyp_meetings]; DELETE FROM [fyp_projects];
DELETE FROM [transcript_export_logs]; DELETE FROM [results]; DELETE FROM [attendance_records]; DELETE FROM [enrollments];
DELETE FROM [course_offerings]; DELETE FROM [student_stream_assignments]; DELETE FROM [student_report_cards]; DELETE FROM [parent_student_links];
DELETE FROM [graduation_application_approvals]; DELETE FROM [graduation_applications];
DELETE FROM [study_plan_courses]; DELETE FROM [study_plans];
DELETE FROM [timetable_entries]; DELETE FROM [timetables];
DELETE FROM [bulk_promotion_entries]; DELETE FROM [bulk_promotion_batches];
DELETE FROM [student_profiles] WHERE [RegistrationNumber] NOT LIKE 'SUPER%';
DELETE FROM [users] WHERE [RoleId] IN (@RF, @RS);
DELETE FROM [faculty_department_assignments]; DELETE FROM [admin_department_assignments];
DELETE FROM [notification_recipients]; DELETE FROM [notifications]; DELETE FROM [academic_deadlines];
DELETE FROM [semesters] WHERE [Name] LIKE 'BSCS %' OR [Name] LIKE 'BBA %' OR [Name] LIKE 'Class %' OR [Name] LIKE 'Spanish %' OR [Name] = N'Spanish Language Program';
PRINT 'Clean done.';

-- ═══════════════════════════════════════════════════════════════════
-- 3. SEMESTERS / CLASSES
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating semesters...';
DECLARE @BscsSem TABLE (Num INT, Id UNIQUEIDENTIFIER);
DECLARE @BbaSem  TABLE (Num INT, Id UNIQUEIDENTIFIER);
DECLARE @SchClass TABLE (Num INT, Id UNIQUEIDENTIFIER);
DECLARE @ColClass TABLE (Num INT, Id UNIQUEIDENTIFIER);
DECLARE @i INT, @sid UNIQUEIDENTIFIER;

SET @i=1; WHILE @i<=8 BEGIN SET @sid=NEWID(); INSERT INTO @BscsSem VALUES(@i,@sid); INSERT INTO [semesters]([Id],[Name],[StartDate],[EndDate],[IsClosed],[CreatedAt],[IsDeleted]) VALUES(@sid,CONCAT(N'BSCS Semester ',@i),DATEFROMPARTS(2014+@i,9,1),DATEFROMPARTS(2015+@i,6,30),1,@Now,0); SET @i+=1; END
SET @i=1; WHILE @i<=8 BEGIN SET @sid=NEWID(); INSERT INTO @BbaSem VALUES(@i,@sid); INSERT INTO [semesters]([Id],[Name],[StartDate],[EndDate],[IsClosed],[CreatedAt],[IsDeleted]) VALUES(@sid,CONCAT(N'BBA Semester ',@i),DATEFROMPARTS(2014+@i,9,1),DATEFROMPARTS(2015+@i,6,30),1,@Now,0); SET @i+=1; END
SET @i=1; WHILE @i<=10 BEGIN SET @sid=NEWID(); INSERT INTO @SchClass VALUES(@i,@sid); INSERT INTO [semesters]([Id],[Name],[StartDate],[EndDate],[IsClosed],[CreatedAt],[IsDeleted]) VALUES(@sid,CONCAT(N'Class ',@i),DATEFROMPARTS(2013+@i,4,1),DATEFROMPARTS(2014+@i,3,31),1,@Now,0); SET @i+=1; END
SET @i=11; WHILE @i<=12 BEGIN SET @sid=NEWID(); INSERT INTO @ColClass VALUES(@i,@sid); INSERT INTO [semesters]([Id],[Name],[StartDate],[EndDate],[IsClosed],[CreatedAt],[IsDeleted]) VALUES(@sid,CONCAT(N'Class ',@i),DATEFROMPARTS(2013+@i,4,1),DATEFROMPARTS(2014+@i,3,31),1,@Now,0); SET @i+=1; END
IF NOT EXISTS(SELECT 1 FROM [semesters] WHERE [Name]=N'Spanish Language Program')
    INSERT INTO [semesters]([Id],[Name],[StartDate],[EndDate],[IsClosed],[CreatedAt],[IsDeleted]) VALUES('5A000000-0000-0000-0000-000000000000',N'Spanish Language Program','2020-09-01','2021-08-31',1,@Now,0);
DECLARE @SemSpa UNIQUEIDENTIFIER = '5A000000-0000-0000-0000-000000000000';
PRINT 'Semesters done.';

-- ═══════════════════════════════════════════════════════════════════
-- 4. COURSE OFFERINGS (bind existing courses → semesters)
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating course offerings...';
DECLARE @CO TABLE (OffId UNIQUEIDENTIFIER, CrsId UNIQUEIDENTIFIER, SemId UNIQUEIDENTIFIER, DeptId UNIQUEIDENTIFIER, ProgId UNIQUEIDENTIFIER, Code NVARCHAR(20));
DECLARE @oid UNIQUEIDENTIFIER, @cid UNIQUEIDENTIFIER, @ccode NVARCHAR(20);

-- BSCS: CSx0x pattern per semester
SET @i=1; WHILE @i<=8
BEGIN
    DECLARE crsB CURSOR FOR SELECT TOP 5 Id,Code FROM [courses] WHERE DepartmentId=@D_IT AND Code LIKE CONCAT(N'CS',@i,N'0%') AND IsActive=1 ORDER BY Code;
    OPEN crsB; FETCH NEXT FROM crsB INTO @cid,@ccode; WHILE @@FETCH_STATUS=0 BEGIN SET @oid=NEWID(); INSERT INTO [course_offerings]([Id],[CourseId],[SemesterId],[MaxEnrollment],[IsOpen],[CreatedAt],[IsDeleted],[InstitutionType],[TenantId],[CampusId]) VALUES(@oid,@cid,(SELECT Id FROM @BscsSem WHERE Num=@i),50,0,@Now,0,0,@T_Uni,@C_Uni); INSERT INTO @CO VALUES(@oid,@cid,(SELECT Id FROM @BscsSem WHERE Num=@i),@D_IT,@P_BSCS,@ccode); FETCH NEXT FROM crsB INTO @cid,@ccode; END
    CLOSE crsB; DEALLOCATE crsB; SET @i+=1;
END

-- BBA: MGT/ACC pattern
SET @i=1; WHILE @i<=8
BEGIN
    DECLARE crsBa CURSOR FOR SELECT TOP 5 Id,Code FROM [courses] WHERE DepartmentId=@D_BUS AND IsActive=1 AND Code NOT IN (SELECT Code FROM @CO) ORDER BY Code;
    OPEN crsBa; FETCH NEXT FROM crsBa INTO @cid,@ccode; WHILE @@FETCH_STATUS=0 BEGIN SET @oid=NEWID(); INSERT INTO [course_offerings]([Id],[CourseId],[SemesterId],[MaxEnrollment],[IsOpen],[CreatedAt],[IsDeleted],[InstitutionType],[TenantId],[CampusId]) VALUES(@oid,@cid,(SELECT Id FROM @BbaSem WHERE Num=@i),50,0,@Now,0,0,@T_Uni,@C_Uni); INSERT INTO @CO VALUES(@oid,@cid,(SELECT Id FROM @BbaSem WHERE Num=@i),@D_BUS,@P_BBA,@ccode); FETCH NEXT FROM crsBa INTO @cid,@ccode; END
    CLOSE crsBa; DEALLOCATE crsBa; SET @i+=1;
END

-- Spanish
DECLARE crsSp CURSOR FOR SELECT TOP 3 Id,Code FROM [courses] WHERE DepartmentId=@D_SPA AND IsActive=1 ORDER BY Code;
OPEN crsSp; FETCH NEXT FROM crsSp INTO @cid,@ccode; WHILE @@FETCH_STATUS=0 BEGIN SET @oid=NEWID(); INSERT INTO [course_offerings]([Id],[CourseId],[SemesterId],[MaxEnrollment],[IsOpen],[CreatedAt],[IsDeleted],[InstitutionType],[TenantId],[CampusId]) VALUES(@oid,@cid,@SemSpa,20,0,@Now,0,0,@T_Uni,@C_Uni); INSERT INTO @CO VALUES(@oid,@cid,@SemSpa,@D_SPA,@P_SPANISH,@ccode); FETCH NEXT FROM crsSp INTO @cid,@ccode; END
CLOSE crsSp; DEALLOCATE crsSp;

-- School (all SCI department courses per class)
SET @i=1; WHILE @i<=10
BEGIN
    DECLARE crsSc CURSOR FOR SELECT Id,Code FROM [courses] WHERE DepartmentId=@D_SCH AND IsActive=1 ORDER BY Code;
    OPEN crsSc; FETCH NEXT FROM crsSc INTO @cid,@ccode; WHILE @@FETCH_STATUS=0 BEGIN SET @oid=NEWID(); INSERT INTO [course_offerings]([Id],[CourseId],[SemesterId],[MaxEnrollment],[IsOpen],[CreatedAt],[IsDeleted],[InstitutionType],[TenantId],[CampusId]) VALUES(@oid,@cid,(SELECT Id FROM @SchClass WHERE Num=@i),30,0,@Now,0,1,@T_Sch,@C_Sch); INSERT INTO @CO VALUES(@oid,@cid,(SELECT Id FROM @SchClass WHERE Num=@i),@D_SCH,@P_SCIENCE,@ccode); FETCH NEXT FROM crsSc INTO @cid,@ccode; END
    CLOSE crsSc; DEALLOCATE crsSc; SET @i+=1;
END

-- College
SET @i=11; WHILE @i<=12
BEGIN
    DECLARE crsCl CURSOR FOR SELECT TOP 5 Id,Code FROM [courses] WHERE DepartmentId=@D_COL AND IsActive=1 ORDER BY Code;
    OPEN crsCl; FETCH NEXT FROM crsCl INTO @cid,@ccode; WHILE @@FETCH_STATUS=0 BEGIN SET @oid=NEWID(); INSERT INTO [course_offerings]([Id],[CourseId],[SemesterId],[MaxEnrollment],[IsOpen],[CreatedAt],[IsDeleted],[InstitutionType],[TenantId],[CampusId]) VALUES(@oid,@cid,(SELECT Id FROM @ColClass WHERE Num=@i),30,0,@Now,0,2,@T_Col,@C_Col); INSERT INTO @CO VALUES(@oid,@cid,(SELECT Id FROM @ColClass WHERE Num=@i),@D_COL,@P_ICS,@ccode); FETCH NEXT FROM crsCl INTO @cid,@ccode; END
    CLOSE crsCl; DEALLOCATE crsCl; SET @i+=1;
END
PRINT 'Offerings done.';

-- ═══════════════════════════════════════════════════════════════════
-- 5. FACULTY
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating faculty...';
DECLARE @fid UNIQUEIDENTIFIER, @fidx INT=1, @fb BINARY(16)=CONVERT(BINARY(16),NEWID());
DECLARE @fUniIT1 UNIQUEIDENTIFIER,@fUniIT2 UNIQUEIDENTIFIER,@fUniBUS1 UNIQUEIDENTIFIER,@fUniBUS2 UNIQUEIDENTIFIER,@fUniSPA1 UNIQUEIDENTIFIER;
DECLARE @fSch1 UNIQUEIDENTIFIER, @fSch2 UNIQUEIDENTIFIER, @fCol1 UNIQUEIDENTIFIER, @fCol2 UNIQUEIDENTIFIER;

-- Helper: create faculty user + dept assignment
DECLARE @createFac TABLE (UId UNIQUEIDENTIFIER, Uname NVARCHAR(100), FName NVARCHAR(200), DeptId UNIQUEIDENTIFIER, TId UNIQUEIDENTIFIER, CId UNIQUEIDENTIFIER, Inst INT);
INSERT INTO @createFac VALUES
(NEWID(),N'faculty.it1',N'IT Faculty 1',@D_IT,@T_Uni,@C_Uni,0),(NEWID(),N'faculty.it2',N'IT Faculty 2',@D_IT,@T_Uni,@C_Uni,0),
(NEWID(),N'faculty.bus1',N'BUS Faculty 1',@D_BUS,@T_Uni,@C_Uni,0),(NEWID(),N'faculty.bus2',N'BUS Faculty 2',@D_BUS,@T_Uni,@C_Uni,0),
(NEWID(),N'faculty.spa1',N'SPA Faculty 1',@D_SPA,@T_Uni,@C_Uni,0),
(NEWID(),N'faculty.sch1',N'School Faculty 1',@D_SCH,@T_Sch,@C_Sch,1),(NEWID(),N'faculty.sch2',N'School Faculty 2',@D_SCH,@T_Sch,@C_Sch,1),
(NEWID(),N'faculty.col1',N'College Faculty 1',@D_COL,@T_Col,@C_Col,2),(NEWID(),N'faculty.col2',N'College Faculty 2',@D_COL,@T_Col,@C_Col,2);

INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType])
SELECT UId,Uname,CONCAT(Uname,N'@tabsan.edu'),FName,@DefPwd,@RF,DeptId,1,0,@Now,TId,CId,Inst FROM @createFac;
INSERT INTO [faculty_department_assignments]([Id],[FacultyUserId],[DepartmentId],[AssignedAt],[CreatedAt])
SELECT NEWID(),UId,DeptId,@Now,@Now FROM @createFac;

SELECT @fUniIT1=UId FROM @createFac WHERE Uname='faculty.it1'; SELECT @fUniIT2=UId FROM @createFac WHERE Uname='faculty.it2';
SELECT @fUniBUS1=UId FROM @createFac WHERE Uname='faculty.bus1'; SELECT @fUniBUS2=UId FROM @createFac WHERE Uname='faculty.bus2';
SELECT @fUniSPA1=UId FROM @createFac WHERE Uname='faculty.spa1';
SELECT @fSch1=UId FROM @createFac WHERE Uname='faculty.sch1'; SELECT @fSch2=UId FROM @createFac WHERE Uname='faculty.sch2';
SELECT @fCol1=UId FROM @createFac WHERE Uname='faculty.col1'; SELECT @fCol2=UId FROM @createFac WHERE Uname='faculty.col2';

UPDATE [course_offerings] SET [FacultyUserId]=@fUniIT1 WHERE [CourseId] IN(SELECT Id FROM [courses] WHERE DepartmentId=@D_IT AND Code LIKE 'CS[1357]%');
UPDATE [course_offerings] SET [FacultyUserId]=@fUniIT2 WHERE [CourseId] IN(SELECT Id FROM [courses] WHERE DepartmentId=@D_IT AND Code LIKE 'CS[2468]%');
UPDATE [course_offerings] SET [FacultyUserId]=@fUniBUS1 WHERE [CourseId] IN(SELECT Id FROM [courses] WHERE DepartmentId=@D_BUS);
UPDATE [course_offerings] SET [FacultyUserId]=@fUniSPA1 WHERE [CourseId] IN(SELECT Id FROM [courses] WHERE DepartmentId=@D_SPA);
PRINT 'Faculty done.';

-- ═══════════════════════════════════════════════════════════════════
-- 6. STUDENTS
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating students...';
DECLARE @sid2 UNIQUEIDENTIFIER, @sc INT;
-- BSCS 10/8 = 80
SET @i=1; WHILE @i<=8 BEGIN SET @sc=1; WHILE @sc<=10 BEGIN SET @sid2=NEWID(); INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType]) VALUES(@sid2,CONCAT(N'bscs',@i,N's',@sc),CONCAT(N'bscs',@i,N's',@sc,N'@tabsan.edu'),CONCAT(N'BSCS S',@i,N' Stu ',@sc),@DefPwd,@RS,@D_IT,1,0,@Now,@T_Uni,@C_Uni,0); INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted]) VALUES(NEWID(),@sid2,CONCAT(N'BSCS-',@i,N'-',RIGHT(CONCAT(N'0',@sc),2)),@P_BSCS,@D_IT,DATEFROMPARTS(2014+@i,9,1),3.0+@i*0.05,3.0+@i*0.05,@i,0,@Now,0); SET @sc+=1; END; SET @i+=1; END
-- BBA 10/8 = 80
SET @i=1; WHILE @i<=8 BEGIN SET @sc=1; WHILE @sc<=10 BEGIN SET @sid2=NEWID(); INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType]) VALUES(@sid2,CONCAT(N'bba',@i,N's',@sc),CONCAT(N'bba',@i,N's',@sc,N'@tabsan.edu'),CONCAT(N'BBA S',@i,N' Stu ',@sc),@DefPwd,@RS,@D_BUS,1,0,@Now,@T_Uni,@C_Uni,0); INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted]) VALUES(NEWID(),@sid2,CONCAT(N'BBA-',@i,N'-',RIGHT(CONCAT(N'0',@sc),2)),@P_BBA,@D_BUS,DATEFROMPARTS(2014+@i,9,1),3.0+@i*0.04,3.0+@i*0.04,@i,0,@Now,0); SET @sc+=1; END; SET @i+=1; END
-- Spanish 10
SET @sc=1; WHILE @sc<=10 BEGIN SET @sid2=NEWID(); INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType]) VALUES(@sid2,CONCAT(N'spa',@sc),CONCAT(N'spa',@sc,N'@tabsan.edu'),CONCAT(N'Spanish Stu ',@sc),@DefPwd,@RS,@D_SPA,1,0,@Now,@T_Uni,@C_Uni,0); INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted]) VALUES(NEWID(),@sid2,CONCAT(N'SPA-1-',RIGHT(CONCAT(N'0',@sc),2)),@P_SPANISH,@D_SPA,'2020-09-01',0,0,1,0,@Now,0); SET @sc+=1; END
-- School 10/class × 10 = 100
SET @i=1; WHILE @i<=10 BEGIN SET @sc=1; WHILE @sc<=10 BEGIN SET @sid2=NEWID(); INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType]) VALUES(@sid2,CONCAT(N'sch',@i,N's',@sc),CONCAT(N'sch',@i,N's',@sc,N'@tabsan.edu'),CONCAT(N'Class ',@i,N' Stu ',@sc),@DefPwd,@RS,@D_SCH,1,0,@Now,@T_Sch,@C_Sch,1); INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted]) VALUES(NEWID(),@sid2,CONCAT(N'SCH-',@i,N'-',RIGHT(CONCAT(N'0',@sc),2)),@P_SCIENCE,@D_SCH,DATEFROMPARTS(2013+@i,4,1),80.0-@i*2.0,80.0-@i*2.0,@i,0,@Now,0); SET @sc+=1; END; SET @i+=1; END
-- College 10/class × 2 = 20
SET @i=11; WHILE @i<=12 BEGIN SET @sc=1; WHILE @sc<=10 BEGIN SET @sid2=NEWID(); INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType]) VALUES(@sid2,CONCAT(N'col',@i,N's',@sc),CONCAT(N'col',@i,N's',@sc,N'@tabsan.edu'),CONCAT(N'Class ',@i,N' Stu ',@sc),@DefPwd,@RS,@D_COL,1,0,@Now,@T_Col,@C_Col,2); INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted]) VALUES(NEWID(),@sid2,CONCAT(N'COL-',@i,N'-',RIGHT(CONCAT(N'0',@sc),2)),@P_ICS,@D_COL,DATEFROMPARTS(2013+@i,4,1),75.0,75.0,@i,0,@Now,0); SET @sc+=1; END; SET @i+=1; END
PRINT 'Students: BSCS:80 BBA:80 SPA:10 School:100 College:20 = 290';

-- ═══════════════════════════════════════════════════════════════════
-- 7. ENROLLMENTS
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating enrollments...';
DECLARE @ec INT=0, @espid UNIQUEIDENTIFIER, @esem INT, @epid UNIQUEIDENTIFIER, @edid UNIQUEIDENTIFIER;
-- Build unified semester map for enrollment logic
DECLARE @AllSem TABLE (ProgId UNIQUEIDENTIFIER, SemNum INT, SemId UNIQUEIDENTIFIER);
INSERT INTO @AllSem SELECT @P_BSCS, Num, Id FROM @BscsSem;
INSERT INTO @AllSem SELECT @P_BBA, Num, Id FROM @BbaSem;
INSERT INTO @AllSem SELECT @P_SCIENCE, Num, Id FROM @SchClass;
INSERT INTO @AllSem SELECT @P_ICS, Num, Id FROM @ColClass;
INSERT INTO @AllSem VALUES (@P_SPANISH, 1, @SemSpa);

DECLARE curE CURSOR FOR SELECT sp.Id,sp.CurrentSemesterNumber,sp.ProgramId,sp.DepartmentId FROM [student_profiles] sp WHERE sp.ProgramId IN(@P_BSCS,@P_BBA,@P_SPANISH,@P_SCIENCE,@P_ICS);
OPEN curE; FETCH NEXT FROM curE INTO @espid,@esem,@epid,@edid;
WHILE @@FETCH_STATUS=0
BEGIN
    -- Enroll in ALL semesters up to current (enables cumulative results)
    DECLARE curSem CURSOR FOR SELECT SemId FROM @AllSem WHERE ProgId=@epid AND SemNum <= @esem;
    DECLARE @cursemId UNIQUEIDENTIFIER;
    OPEN curSem; FETCH NEXT FROM curSem INTO @cursemId;
    WHILE @@FETCH_STATUS=0
    BEGIN
        DECLARE curEnrO CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@epid AND SemId=@cursemId;
        OPEN curEnrO; FETCH NEXT FROM curEnrO INTO @oid;
        WHILE @@FETCH_STATUS=0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM [enrollments] WHERE StudentProfileId=@espid AND CourseOfferingId=@oid)
            BEGIN
                INSERT INTO [enrollments]([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt]) VALUES(NEWID(),@espid,@oid,@Now,N'Active',@Now);
                SET @ec+=1;
            END
            FETCH NEXT FROM curEnrO INTO @oid;
        END
        CLOSE curEnrO; DEALLOCATE curEnrO;
        FETCH NEXT FROM curSem INTO @cursemId;
    END
    CLOSE curSem; DEALLOCATE curSem;
    FETCH NEXT FROM curE INTO @espid,@esem,@epid,@edid;
END
CLOSE curE; DEALLOCATE curE;
PRINT CONCAT('Enrollments: ',@ec);

-- ═══════════════════════════════════════════════════════════════════
-- 8. RESULTS (cumulative: Sem N → results of Sem 1..N-1)
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating results...';
DECLARE @rc INT=0, @resp UNIQUEIDENTIFIER, @reoff UNIQUEIDENTIFIER, @marks DECIMAL(8,2);

-- BSCS: Sem 2→Sem1, Sem3→Sem1-2, ... Sem8→Sem1-7
SET @i=2; WHILE @i<=8
BEGIN
    DECLARE curR CURSOR FOR SELECT e.StudentProfileId,e.CourseOfferingId FROM [enrollments] e JOIN [course_offerings] co ON co.Id=e.CourseOfferingId JOIN @BscsSem s ON s.Id=co.SemesterId WHERE s.Num<@i AND e.Status=N'Active' AND EXISTS(SELECT 1 FROM [student_profiles] sp WHERE sp.Id=e.StudentProfileId AND sp.ProgramId=@P_BSCS AND sp.CurrentSemesterNumber=@i);
    OPEN curR; FETCH NEXT FROM curR INTO @resp,@reoff; WHILE @@FETCH_STATUS=0 BEGIN SET @marks=65+ABS(CHECKSUM(NEWID()))%31; INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt]) VALUES(NEWID(),@resp,@reoff,N'Final',@marks,100.0,1,@Now,@fUniIT1,@Now); SET @rc+=1; FETCH NEXT FROM curR INTO @resp,@reoff; END
    CLOSE curR; DEALLOCATE curR; SET @i+=1;
END

-- BBA: same logic
SET @i=2; WHILE @i<=8
BEGIN
    DECLARE curR2 CURSOR FOR SELECT e.StudentProfileId,e.CourseOfferingId FROM [enrollments] e JOIN [course_offerings] co ON co.Id=e.CourseOfferingId JOIN @BbaSem s ON s.Id=co.SemesterId WHERE s.Num<@i AND e.Status=N'Active' AND EXISTS(SELECT 1 FROM [student_profiles] sp WHERE sp.Id=e.StudentProfileId AND sp.ProgramId=@P_BBA AND sp.CurrentSemesterNumber=@i);
    OPEN curR2; FETCH NEXT FROM curR2 INTO @resp,@reoff; WHILE @@FETCH_STATUS=0 BEGIN SET @marks=65+ABS(CHECKSUM(NEWID()))%31; INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt]) VALUES(NEWID(),@resp,@reoff,N'Final',@marks,100.0,1,@Now,@fUniBUS1,@Now); SET @rc+=1; FETCH NEXT FROM curR2 INTO @resp,@reoff; END
    CLOSE curR2; DEALLOCATE curR2; SET @i+=1;
END

-- School: Class 2→Class1, Class3→Class1-2, ... Class10→Class1-9
SET @i=2; WHILE @i<=10
BEGIN
    DECLARE curR3 CURSOR FOR SELECT e.StudentProfileId,e.CourseOfferingId FROM [enrollments] e JOIN [course_offerings] co ON co.Id=e.CourseOfferingId JOIN @SchClass s ON s.Id=co.SemesterId WHERE s.Num<@i AND e.Status=N'Active' AND EXISTS(SELECT 1 FROM [student_profiles] sp WHERE sp.Id=e.StudentProfileId AND sp.ProgramId=@P_SCIENCE AND sp.CurrentSemesterNumber=@i);
    OPEN curR3; FETCH NEXT FROM curR3 INTO @resp,@reoff; WHILE @@FETCH_STATUS=0 BEGIN SET @marks=60+ABS(CHECKSUM(NEWID()))%36; INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt]) VALUES(NEWID(),@resp,@reoff,N'Final',@marks,100.0,1,@Now,@fSch1,@Now); SET @rc+=1; FETCH NEXT FROM curR3 INTO @resp,@reoff; END
    CLOSE curR3; DEALLOCATE curR3; SET @i+=1;
END

-- College: Class 12→Class 11
DECLARE curR4 CURSOR FOR SELECT e.StudentProfileId,e.CourseOfferingId FROM [enrollments] e JOIN [course_offerings] co ON co.Id=e.CourseOfferingId JOIN @ColClass s ON s.Id=co.SemesterId WHERE s.Num=11 AND e.Status=N'Active' AND EXISTS(SELECT 1 FROM [student_profiles] sp WHERE sp.Id=e.StudentProfileId AND sp.ProgramId=@P_ICS AND sp.CurrentSemesterNumber=12);
OPEN curR4; FETCH NEXT FROM curR4 INTO @resp,@reoff; WHILE @@FETCH_STATUS=0 BEGIN SET @marks=60+ABS(CHECKSUM(NEWID()))%36; INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt]) VALUES(NEWID(),@resp,@reoff,N'Final',@marks,100.0,1,@Now,@fCol1,@Now); SET @rc+=1; FETCH NEXT FROM curR4 INTO @resp,@reoff; END
CLOSE curR4; DEALLOCATE curR4;
PRINT CONCAT('Results: ',@rc);

-- ═══════════════════════════════════════════════════════════════════
-- 9. ATTENDANCE (~90 days per enrollment, 85-95% present)
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating attendance...';
DECLARE @ac INT=0, @asp UNIQUEIDENTIFIER, @aoff UNIQUEIDENTIFIER, @d INT, @st NVARCHAR(10);
DECLARE curA CURSOR FOR SELECT StudentProfileId,CourseOfferingId FROM [enrollments] WHERE Status=N'Active';
OPEN curA; FETCH NEXT FROM curA INTO @asp,@aoff;
WHILE @@FETCH_STATUS=0 BEGIN SET @d=1; WHILE @d<=15 BEGIN SET @st=CASE WHEN ABS(CHECKSUM(NEWID()))%100<90 THEN N'Present' ELSE N'Absent' END; INSERT INTO [attendance_records]([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt]) VALUES(NEWID(),@asp,@aoff,DATEADD(DAY,@d,'2025-01-01'),@st,@fUniIT1,@Now); SET @ac+=1; SET @d+=1; END; FETCH NEXT FROM curA INTO @asp,@aoff; END
CLOSE curA; DEALLOCATE curA;
PRINT CONCAT('Attendance: ',@ac);

-- ═══════════════════════════════════════════════════════════════════
-- 10. ASSIGNMENTS (2 per offering)
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating assignments...';
DECLARE @asc INT=0;
DECLARE curAs CURSOR FOR SELECT OffId FROM @CO;
OPEN curAs; FETCH NEXT FROM curAs INTO @oid;
WHILE @@FETCH_STATUS=0 BEGIN INSERT INTO [assignments]([Id],[CourseOfferingId],[Title],[Description],[DueDate],[MaxMarks],[IsPublished],[PublishedAt],[CreatedAt],[IsDeleted]) VALUES(NEWID(),@oid,N'Assignment 1',N'First assignment',DATEADD(DAY,45,@Now),50,1,@Now,@Now,0),(NEWID(),@oid,N'Assignment 2',N'Second assignment',DATEADD(DAY,90,@Now),50,1,@Now,@Now,0); SET @asc+=2; FETCH NEXT FROM curAs INTO @oid; END
CLOSE curAs; DEALLOCATE curAs;
PRINT CONCAT('Assignments: ',@asc);

-- ═══════════════════════════════════════════════════════════════════
-- 11. FYP (BSCS & BBA Semester 8 only)
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating FYP...';
DECLARE @fc INT=0;
DECLARE curF CURSOR FOR SELECT sp.Id,sp.DepartmentId FROM [student_profiles] sp WHERE sp.ProgramId IN(@P_BSCS,@P_BBA) AND sp.CurrentSemesterNumber=8;
OPEN curF; FETCH NEXT FROM curF INTO @espid,@edid;
WHILE @@FETCH_STATUS=0 BEGIN DECLARE @fyid UNIQUEIDENTIFIER=NEWID(); INSERT INTO [fyp_projects]([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[CreatedAt]) VALUES(@fyid,@espid,@edid,CONCAT(N'Research - ',LEFT(CONVERT(NVARCHAR(36),NEWID()),8)),N'FYP',N'Active',CASE WHEN @edid=@D_IT THEN @fUniIT1 ELSE @fUniBUS1 END,@Now); INSERT INTO [fyp_meetings]([Id],[FypProjectId],[ScheduledAt],[Venue],[Status],[OrganiserUserId],[CreatedAt]) VALUES(NEWID(),@fyid,DATEADD(DAY,30,@Now),N'Room 101',N'Scheduled',CASE WHEN @edid=@D_IT THEN @fUniIT1 ELSE @fUniBUS1 END,@Now); SET @fc+=1; FETCH NEXT FROM curF INTO @espid,@edid; END
CLOSE curF; DEALLOCATE curF;
PRINT CONCAT('FYP: ',@fc);

-- ═══════════════════════════════════════════════════════════════════
-- 12. TIMETABLES
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating timetables...';
DECLARE @ttid UNIQUEIDENTIFIER;
DECLARE @ttDays TABLE(D INT,S TIME,E TIME); INSERT INTO @ttDays VALUES(1,'09:00','10:30'),(1,'11:00','12:30'),(3,'09:00','10:30'),(3,'11:00','12:30'),(5,'09:00','10:30');

-- BSCS
SET @i=1; WHILE @i<=8 BEGIN SET @ttid=NEWID(); INSERT INTO [timetables]([Id],[DepartmentId],[SemesterId],[AcademicProgramId],[EffectiveDate],[SemesterNumber],[IsPublished],[CreatedAt],[IsDeleted]) VALUES(@ttid,@D_IT,(SELECT Id FROM @BscsSem WHERE Num=@i),@P_BSCS,DATEFROMPARTS(2014+@i,9,1),@i,1,@Now,0); INSERT INTO [timetable_entries]([Id],[TimetableId],[DayOfWeek],[StartTime],[EndTime],[SubjectName],[RoomNumber],[FacultyName],[CreatedAt]) SELECT NEWID(),@ttid,D,S,E,CONCAT(N'BSCS S',@i,N' Sub'),CONCAT(N'R',ROW_NUMBER()OVER(ORDER BY D)),N'Prof. IT',@Now FROM @ttDays; SET @i+=1; END
-- BBA
SET @i=1; WHILE @i<=8 BEGIN SET @ttid=NEWID(); INSERT INTO [timetables]([Id],[DepartmentId],[SemesterId],[AcademicProgramId],[EffectiveDate],[SemesterNumber],[IsPublished],[CreatedAt],[IsDeleted]) VALUES(@ttid,@D_BUS,(SELECT Id FROM @BbaSem WHERE Num=@i),@P_BBA,DATEFROMPARTS(2014+@i,9,1),@i,1,@Now,0); INSERT INTO [timetable_entries]([Id],[TimetableId],[DayOfWeek],[StartTime],[EndTime],[SubjectName],[RoomNumber],[FacultyName],[CreatedAt]) SELECT NEWID(),@ttid,D,S,E,CONCAT(N'BBA S',@i,N' Sub'),CONCAT(N'R',ROW_NUMBER()OVER(ORDER BY D)),N'Prof. BUS',@Now FROM @ttDays; SET @i+=1; END
-- School
SET @i=1; WHILE @i<=10 BEGIN SET @ttid=NEWID(); INSERT INTO [timetables]([Id],[DepartmentId],[SemesterId],[AcademicProgramId],[EffectiveDate],[SemesterNumber],[IsPublished],[CreatedAt],[IsDeleted]) VALUES(@ttid,@D_SCH,(SELECT Id FROM @SchClass WHERE Num=@i),@P_SCIENCE,DATEFROMPARTS(2013+@i,4,1),@i,1,@Now,0); INSERT INTO [timetable_entries]([Id],[TimetableId],[DayOfWeek],[StartTime],[EndTime],[SubjectName],[RoomNumber],[FacultyName],[CreatedAt]) SELECT NEWID(),@ttid,D,S,E,CONCAT(N'Class ',@i,N' Sub'),CONCAT(N'R',ROW_NUMBER()OVER(ORDER BY D)),N'School Teacher',@Now FROM @ttDays; SET @i+=1; END
-- College
SET @i=11; WHILE @i<=12 BEGIN SET @ttid=NEWID(); INSERT INTO [timetables]([Id],[DepartmentId],[SemesterId],[AcademicProgramId],[EffectiveDate],[SemesterNumber],[IsPublished],[CreatedAt],[IsDeleted]) VALUES(@ttid,@D_COL,(SELECT Id FROM @ColClass WHERE Num=@i),@P_ICS,DATEFROMPARTS(2013+@i,4,1),@i,1,@Now,0); INSERT INTO [timetable_entries]([Id],[TimetableId],[DayOfWeek],[StartTime],[EndTime],[SubjectName],[RoomNumber],[FacultyName],[CreatedAt]) SELECT NEWID(),@ttid,D,S,E,CONCAT(N'ICS Sub ',@i),CONCAT(N'R',ROW_NUMBER()OVER(ORDER BY D)),N'College Teacher',@Now FROM @ttDays; SET @i+=1; END
PRINT 'Timetables done.';

-- ═══════════════════════════════════════════════════════════════════
-- 13. GRADUATED STUDENTS (1 per program with complete marks)
--     Purpose: enable certificate generation for demos.
--     Grading rules:
--       School & College: 90+ = A+, 80+ = A, 70+ = B, 60+ = C, 50+ = D, <50 = F
--       University semester-based (BSCS/BBA): GPA scale (4.0 base)
--       University non-semester (Spanish): percentage with letter grades
-- ═══════════════════════════════════════════════════════════════════
PRINT 'Creating graduated students...';
DECLARE @gid UNIQUEIDENTIFIER, @gpid UNIQUEIDENTIFIER, @gidx INT=0;

-- Helper: percentage → letter grade
DECLARE @gradeFn TABLE (MinPct INT, Letter NVARCHAR(5));
INSERT INTO @gradeFn VALUES (90,N'A+'),(80,N'A'),(70,N'B'),(60,N'C'),(50,N'D'),(0,N'F');

-- ===== 13a. BSCS Graduate (completed all 8 semesters) =====
SET @gid=NEWID(); SET @gpid=NEWID();
INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType])
VALUES(@gid,N'bscs.grad',N'bscs.grad@tabsan.edu',N'BSCS Graduate Demo',@DefPwd,@RS,@D_IT,1,0,@Now,@T_Uni,@C_Uni,0);
INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted])
VALUES(@gpid,@gid,N'BSCS-GRAD-01',@P_BSCS,@D_IT,'2018-09-01',3.75,3.75,8,3,@Now,0); -- Status 3 = Graduated

-- Enroll BSCS grad in ALL 8 semesters
INSERT INTO [enrollments]([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
SELECT NEWID(),@gpid,OffId,@Now,N'Completed',@Now FROM @CO WHERE ProgId=@P_BSCS;

-- BSCS grad results: GPA-based (marks between 75-98, giving strong GPA)
DECLARE @bresOff UNIQUEIDENTIFIER, @bmarks DECIMAL(8,2);
DECLARE curBg CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_BSCS;
OPEN curBg; FETCH NEXT FROM curBg INTO @bresOff;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @bmarks=75+ABS(CHECKSUM(NEWID()))%24;
    -- Mid-term (weighted ~30%)
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@bresOff,N'Mid',ROUND(@bmarks*0.30,2),30.0,1,@Now,@fUniIT1,@Now);
    -- Final exam (weighted ~70%)
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@bresOff,N'Final',ROUND(@bmarks*0.70,2),70.0,1,@Now,@fUniIT1,@Now);
    FETCH NEXT FROM curBg INTO @bresOff;
END
CLOSE curBg; DEALLOCATE curBg;

-- Quiz attempts for BSCS grad
DECLARE @bquizId UNIQUEIDENTIFIER, @bqScore DECIMAL(10,2);
DECLARE curBq CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_BSCS;
OPEN curBq; FETCH NEXT FROM curBq INTO @oid;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @bquizId=NEWID();
    INSERT INTO [quizzes]([Id],[CourseOfferingId],[Title],[Instructions],[MaxAttempts],[IsPublished],[IsActive],[CreatedByUserId],[CreatedAt])
    VALUES(@bquizId,@oid,N'Course Quiz',N'Complete all questions.',1,1,1,@fUniIT1,@Now);
    SET @bqScore=7.0+ABS(CHECKSUM(NEWID()))%3;
    INSERT INTO [quiz_attempts]([Id],[QuizId],[StudentProfileId],[StartedAt],[FinishedAt],[Status],[TotalScore],[CreatedAt])
    VALUES(NEWID(),@bquizId,@gpid,DATEADD(DAY,-30,@Now),DATEADD(DAY,-30,@Now),N'Completed',@bqScore,@Now);
    FETCH NEXT FROM curBq INTO @oid;
END
CLOSE curBq; DEALLOCATE curBq;

-- FYP for BSCS grad (with marks if columns exist, else without)
SET @fyid=NEWID();
IF COL_LENGTH('fyp_projects', 'FypMarks') IS NOT NULL
    INSERT INTO [fyp_projects]([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[FypMarks],[FypMaxMarks],[FypGradePoint],[CreatedAt])
    VALUES(@fyid,@gpid,@D_IT,N'AI-Based Academic Performance Prediction',N'Research on ML models for student performance.',N'Completed',@fUniIT1,88.0,100.0,3.8,@Now);
ELSE
    INSERT INTO [fyp_projects]([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[CreatedAt])
    VALUES(@fyid,@gpid,@D_IT,N'AI-Based Academic Performance Prediction',N'Research on ML models for student performance.',N'Completed',@fUniIT1,@Now);
INSERT INTO [fyp_meetings]([Id],[FypProjectId],[ScheduledAt],[Venue],[Status],[OrganiserUserId],[CreatedAt])
VALUES(NEWID(),@fyid,DATEADD(DAY,-90,@Now),N'Room 301',N'Completed',@fUniIT1,@Now);

-- Graduation application for BSCS
INSERT INTO [graduation_applications]([Id],[StudentProfileId],[Status],[SubmittedAt],[CreatedAt],[IsDeleted])
VALUES(NEWID(),@gpid,2,DATEADD(DAY,-10,@Now),@Now,0); -- Status 2 = Approved
SET @gidx+=1;

-- ===== 13b. BBA Graduate =====
SET @gid=NEWID(); SET @gpid=NEWID();
INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType])
VALUES(@gid,N'bba.grad',N'bba.grad@tabsan.edu',N'BBA Graduate Demo',@DefPwd,@RS,@D_BUS,1,0,@Now,@T_Uni,@C_Uni,0);
INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted])
VALUES(@gpid,@gid,N'BBA-GRAD-01',@P_BBA,@D_BUS,'2018-09-01',3.60,3.60,8,3,@Now,0);

INSERT INTO [enrollments]([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
SELECT NEWID(),@gpid,OffId,@Now,N'Completed',@Now FROM @CO WHERE ProgId=@P_BBA;

DECLARE curBba CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_BBA;
OPEN curBba; FETCH NEXT FROM curBba INTO @bresOff;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @bmarks=72+ABS(CHECKSUM(NEWID()))%24;
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@bresOff,N'Mid',ROUND(@bmarks*0.30,2),30.0,1,@Now,@fUniBUS1,@Now);
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@bresOff,N'Final',ROUND(@bmarks*0.70,2),70.0,1,@Now,@fUniBUS1,@Now);
    FETCH NEXT FROM curBba INTO @bresOff;
END
CLOSE curBba; DEALLOCATE curBba;

-- Quizzes BBA
DECLARE curBbaQ CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_BBA;
OPEN curBbaQ; FETCH NEXT FROM curBbaQ INTO @oid;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @bquizId=NEWID();
    INSERT INTO [quizzes]([Id],[CourseOfferingId],[Title],[Instructions],[MaxAttempts],[IsPublished],[IsActive],[CreatedByUserId],[CreatedAt])
    VALUES(@bquizId,@oid,N'Course Quiz',N'Complete all questions.',1,1,1,@fUniBUS1,@Now);
    SET @bqScore=7.0+ABS(CHECKSUM(NEWID()))%3;
    INSERT INTO [quiz_attempts]([Id],[QuizId],[StudentProfileId],[StartedAt],[FinishedAt],[Status],[TotalScore],[CreatedAt])
    VALUES(NEWID(),@bquizId,@gpid,DATEADD(DAY,-30,@Now),DATEADD(DAY,-30,@Now),N'Completed',@bqScore,@Now);
    FETCH NEXT FROM curBbaQ INTO @oid;
END
CLOSE curBbaQ; DEALLOCATE curBbaQ;

-- FYP BBA (with marks if columns exist, else without)
SET @fyid=NEWID();
IF COL_LENGTH('fyp_projects', 'FypMarks') IS NOT NULL
    INSERT INTO [fyp_projects]([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[FypMarks],[FypMaxMarks],[FypGradePoint],[CreatedAt])
    VALUES(@fyid,@gpid,@D_BUS,N'Strategic Management in Digital Era',N'Research on digital transformation.',N'Completed',@fUniBUS1,85.0,100.0,3.5,@Now);
ELSE
    INSERT INTO [fyp_projects]([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[CreatedAt])
    VALUES(@fyid,@gpid,@D_BUS,N'Strategic Management in Digital Era',N'Research on digital transformation.',N'Completed',@fUniBUS1,@Now);
INSERT INTO [fyp_meetings]([Id],[FypProjectId],[ScheduledAt],[Venue],[Status],[OrganiserUserId],[CreatedAt])
VALUES(NEWID(),@fyid,DATEADD(DAY,-90,@Now),N'Room 202',N'Completed',@fUniBUS1,@Now);

INSERT INTO [graduation_applications]([Id],[StudentProfileId],[Status],[SubmittedAt],[CreatedAt],[IsDeleted])
VALUES(NEWID(),@gpid,2,DATEADD(DAY,-10,@Now),@Now,0);
SET @gidx+=1;

-- ===== 13c. Spanish Graduate (non-semester, percentage-based grading) =====
SET @gid=NEWID(); SET @gpid=NEWID();
INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType])
VALUES(@gid,N'spa.grad',N'spa.grad@tabsan.edu',N'Spanish Graduate Demo',@DefPwd,@RS,@D_SPA,1,0,@Now,@T_Uni,@C_Uni,0);
INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted])
VALUES(@gpid,@gid,N'SPA-GRAD-01',@P_SPANISH,@D_SPA,'2020-09-01',88.0,88.0,1,3,@Now,0);

INSERT INTO [enrollments]([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
SELECT NEWID(),@gpid,OffId,@Now,N'Completed',@Now FROM @CO WHERE ProgId=@P_SPANISH;

-- Spanish: percentage-based (90+ = A+, 80-89 = A, etc.)
DECLARE @spMarks DECIMAL(8,2)=92.0, @spOff UNIQUEIDENTIFIER, @spFlag INT=0;
DECLARE curSp CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_SPANISH ORDER BY OffId;
OPEN curSp; FETCH NEXT FROM curSp INTO @spOff;
WHILE @@FETCH_STATUS=0
BEGIN
    -- Vary marks slightly per course: 92, 88, 95
    SET @spMarks = CASE @spFlag WHEN 0 THEN 92.0 WHEN 1 THEN 88.0 ELSE 95.0 END;
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@spOff,N'Mid',ROUND(@spMarks*0.30,2),30.0,1,@Now,@fUniSPA1,@Now);
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@spOff,N'Final',ROUND(@spMarks*0.70,2),70.0,1,@Now,@fUniSPA1,@Now);
    SET @spFlag+=1; FETCH NEXT FROM curSp INTO @spOff;
END
CLOSE curSp; DEALLOCATE curSp;

INSERT INTO [graduation_applications]([Id],[StudentProfileId],[Status],[SubmittedAt],[CreatedAt],[IsDeleted])
VALUES(NEWID(),@gpid,2,DATEADD(DAY,-10,@Now),@Now,0);
SET @gidx+=1;

-- ===== 13d. School Graduate (Class 10, percentage-based grading) =====
SET @gid=NEWID(); SET @gpid=NEWID();
INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType])
VALUES(@gid,N'sch.grad',N'sch.grad@tabsan.edu',N'School Graduate Demo',@DefPwd,@RS,@D_SCH,1,0,@Now,@T_Sch,@C_Sch,1);
INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted])
VALUES(@gpid,@gid,N'SCH-GRAD-01',@P_SCIENCE,@D_SCH,'2014-04-01',92.0,92.0,10,3,@Now,0);

INSERT INTO [enrollments]([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
SELECT NEWID(),@gpid,OffId,@Now,N'Completed',@Now FROM @CO WHERE ProgId=@P_SCIENCE;

-- School: percentage-based with specific letter grades (A+, A, B, C, D, F)
DECLARE @schMarksList TABLE (Seq INT, Marks DECIMAL(8,2));
INSERT INTO @schMarksList VALUES (1,94),(2,91),(3,87),(4,83),(5,78),(6,75),(7,72),(8,68);
DECLARE @schFlag INT=0, @schOff UNIQUEIDENTIFIER;
DECLARE curSch CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_SCIENCE ORDER BY OffId;
OPEN curSch; FETCH NEXT FROM curSch INTO @schOff;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @bmarks=ISNULL((SELECT Marks FROM @schMarksList WHERE Seq=(@schFlag%8)+1),85.0);
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@schOff,N'Mid',ROUND(@bmarks*0.30,2),30.0,1,@Now,@fSch1,@Now);
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@schOff,N'Final',ROUND(@bmarks*0.70,2),70.0,1,@Now,@fSch1,@Now);
    SET @schFlag+=1; FETCH NEXT FROM curSch INTO @schOff;
END
CLOSE curSch; DEALLOCATE curSch;

-- Quizzes for School grad
DECLARE curSchQ CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_SCIENCE;
OPEN curSchQ; FETCH NEXT FROM curSchQ INTO @oid;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @bquizId=NEWID();
    INSERT INTO [quizzes]([Id],[CourseOfferingId],[Title],[Instructions],[MaxAttempts],[IsPublished],[IsActive],[CreatedByUserId],[CreatedAt])
    VALUES(@bquizId,@oid,N'Class Quiz',N'Answer all questions.',1,1,1,@fSch1,@Now);
    SET @bqScore=8.0+ABS(CHECKSUM(NEWID()))%3;
    INSERT INTO [quiz_attempts]([Id],[QuizId],[StudentProfileId],[StartedAt],[FinishedAt],[Status],[TotalScore],[CreatedAt])
    VALUES(NEWID(),@bquizId,@gpid,DATEADD(DAY,-30,@Now),DATEADD(DAY,-30,@Now),N'Completed',@bqScore,@Now);
    FETCH NEXT FROM curSchQ INTO @oid;
END
CLOSE curSchQ; DEALLOCATE curSchQ;

INSERT INTO [graduation_applications]([Id],[StudentProfileId],[Status],[SubmittedAt],[CreatedAt],[IsDeleted])
VALUES(NEWID(),@gpid,2,DATEADD(DAY,-10,@Now),@Now,0);
SET @gidx+=1;

-- ===== 13e. College ICS Graduate (Class 12, percentage-based grading) =====
SET @gid=NEWID(); SET @gpid=NEWID();
INSERT INTO [users]([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[IsActive],[IsDeleted],[CreatedAt],[TenantId],[CampusId],[InstitutionType])
VALUES(@gid,N'col.grad',N'col.grad@tabsan.edu',N'College ICS Graduate Demo',@DefPwd,@RS,@D_COL,1,0,@Now,@T_Col,@C_Col,2);
INSERT INTO [student_profiles]([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[AdmissionDate],[Cgpa],[CurrentSemesterGpa],[CurrentSemesterNumber],[Status],[CreatedAt],[IsDeleted])
VALUES(@gpid,@gid,N'COL-GRAD-01',@P_ICS,@D_COL,'2015-04-01',88.0,88.0,12,3,@Now,0);

INSERT INTO [enrollments]([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
SELECT NEWID(),@gpid,OffId,@Now,N'Completed',@Now FROM @CO WHERE ProgId=@P_ICS;

-- College: percentage-based grades
DECLARE @colFlag INT=0, @colOff UNIQUEIDENTIFIER;
DECLARE curCol CURSOR FOR SELECT OffId FROM @CO WHERE ProgId=@P_ICS ORDER BY OffId;
OPEN curCol; FETCH NEXT FROM curCol INTO @colOff;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @bmarks=CASE @colFlag%5 WHEN 0 THEN 93 WHEN 1 THEN 86 WHEN 2 THEN 78 WHEN 3 THEN 82 WHEN 4 THEN 74 END;
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@colOff,N'Mid',ROUND(@bmarks*0.30,2),30.0,1,@Now,@fCol1,@Now);
    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[PublishedAt],[PublishedByUserId],[CreatedAt])
    VALUES(NEWID(),@gpid,@colOff,N'Final',ROUND(@bmarks*0.70,2),70.0,1,@Now,@fCol1,@Now);
    SET @colFlag+=1; FETCH NEXT FROM curCol INTO @colOff;
END
CLOSE curCol; DEALLOCATE curCol;

INSERT INTO [graduation_applications]([Id],[StudentProfileId],[Status],[SubmittedAt],[CreatedAt],[IsDeleted])
VALUES(NEWID(),@gpid,2,DATEADD(DAY,-10,@Now),@Now,0);
SET @gidx+=1;

PRINT CONCAT('Graduated students: ', @gidx);

-- ═══════════════════════════════════════════════════════════════════
-- 14. VERSION
-- ═══════════════════════════════════════════════════════════════════
UPDATE [Tabsan-EduSphere] SET [DemoValue]=N'2.1',[UpdatedAt]=@Now WHERE [DemoKey]=N'db.version';

PRINT '';
PRINT '=== 03-FullDummyData.sql v2.1 complete ===';
PRINT 'Uni: IT(80 BSCS) + BUS(80 BBA) + SPA(10) = 170';
PRINT 'School: SCI (Class 1-10, 100)';
PRINT 'College: ICS (Class 11-12, 20)';
PRINT 'Graduated: BSCS(1) + BBA(1) + Spanish(1) + School(1) + College(1) = 5';
PRINT 'Total: 295 students';
PRINT 'Rules: no results for first semester/class; cumulative thereafter';
PRINT 'Graduated students have: mid+final exams, quizzes, FYP (BSCS/BBA), graduation application';
GO
