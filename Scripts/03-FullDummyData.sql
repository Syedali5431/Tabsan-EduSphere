/*
  Full Dummy Data — Tabsan EduSphere v1.0
  Generates 210+ students and full academic data across 3 institutes.
  Uses correct EF Core schema column names.

  --- 2026-06-10 Deployment Sync ---
  BBA department InstitutionType = 0 (University) — corrected from College(2).
  Semester sort order: ascending by StartDate.
  Campus active-only filter: activeOnly=true default in CampusController.
  Tenant active-only filter: .Where(t => t.IsActive) in GetTenantsAsync.
  Reports: ProgramName + DepartmentName columns added to all summaries.
*/
SET NOCOUNT ON;
GO
USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @DefaultPwd NVARCHAR(512) = N'argon2id:S7KBqFYDtoQ/+936WKnRGrfaizX10wKV9mIYdhbsO7M=:ncFDYnCu/jEm22iNzYCxdtkxnIZWWyRHRe7StVKmpvQ=';
DECLARE @R_Student INT = 4, @R_Faculty INT = 3;

-- Institute refs
DECLARE @T_Uni UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @T_Col UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @T_Sch UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @C_Uni UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
DECLARE @C_Col UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB';
DECLARE @C_Sch UNIQUEIDENTIFIER = 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC';
DECLARE @D_IT_Uni UNIQUEIDENTIFIER = 'D0000001-0000-0000-0000-000000000001';
DECLARE @D_BUS    UNIQUEIDENTIFIER = 'D0000002-0000-0000-0000-000000000002';
DECLARE @D_IT_Col UNIQUEIDENTIFIER = 'D0000003-0000-0000-0000-000000000003';
DECLARE @D_IT_Sch UNIQUEIDENTIFIER = 'D0000004-0000-0000-0000-000000000004';
DECLARE @P_BSCS    UNIQUEIDENTIFIER = 'A0000001-0000-0000-0000-000000000001';
DECLARE @P_BBA     UNIQUEIDENTIFIER = 'A0000002-0000-0000-0000-000000000002';
DECLARE @P_MSE     UNIQUEIDENTIFIER = 'A0000003-0000-0000-0000-000000000003';
DECLARE @P_SPANISH UNIQUEIDENTIFIER = 'A0000004-0000-0000-0000-000000000004';
DECLARE @P_ICS     UNIQUEIDENTIFIER = 'A0000005-0000-0000-0000-000000000005';
DECLARE @P_SCIENCE UNIQUEIDENTIFIER = 'A0000006-0000-0000-0000-000000000006';

-- Faculty user lookups (for MarkedByUserId in attendance)
DECLARE @Fac_Uni UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=2);
DECLARE @Fac_Col UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=1);
DECLARE @Fac_Sch UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=0);

PRINT 'Starting demo data generation...';

-- ════════════════════════════════════════════════
-- SCHOOL: Classes 1-10, 10 students each = 100
-- ════════════════════════════════════════════════
PRINT 'Generating School students...';
DECLARE @cls INT = 1;
WHILE @cls <= 10
BEGIN
    DECLARE @stuNum INT = 1;
    WHILE @stuNum <= 10
    BEGIN
        DECLARE @uidSch UNIQUEIDENTIFIER = NEWID();
        DECLARE @unSch NVARCHAR(20) = CONCAT(N'sch', @cls, N's', @stuNum);
        DECLARE @regSch NVARCHAR(50) = CONCAT(N'SCH-REG-', @cls, N'-', RIGHT(N'00'+CAST(@stuNum AS NVARCHAR),2));
        DECLARE @fnSch NVARCHAR(100) = CASE (@stuNum % 10)
            WHEN 1 THEN N'Ahmed Raza' WHEN 2 THEN N'Hania Amir' WHEN 3 THEN N'Rayyan Malik'
            WHEN 4 THEN N'Emaan Shah' WHEN 5 THEN N'Zayan Qureshi' WHEN 6 THEN N'Ayesha Noor'
            WHEN 7 THEN N'Bilal Khan' WHEN 8 THEN N'Fatima Zahra' WHEN 9 THEN N'Daniyal Ahmed'
            WHEN 0 THEN N'Kiran Shahid' END + N' C' + CAST(@cls AS NVARCHAR);

        IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Username]=@unSch)
            INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
            VALUES (@uidSch, @unSch, CONCAT(@unSch,N'@sch.local'), @fnSch, @DefaultPwd, @R_Student, @D_IT_Sch, @T_Sch, @C_Sch, 0, 1, @Now, 0);

        IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [UserId]=@uidSch)
            INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
            VALUES (NEWID(), @uidSch, @regSch, @P_SCIENCE, @D_IT_Sch, @cls, DATEADD(YEAR,-@cls+1,@Now), 2.0 + CAST(ABS(CHECKSUM(NEWID()))%20 AS FLOAT)/10, @Now, 0);

        -- Attendance: 30 days, Status = Present/Absent
        DECLARE @daySch INT = 1;
        WHILE @daySch <= 30
        BEGIN
            DECLARE @statSch NVARCHAR(10) = CASE WHEN ABS(CHECKSUM(NEWID()))%10<8 THEN N'Present' ELSE N'Absent' END;
            DECLARE @spIdSch UNIQUEIDENTIFIER = (SELECT Id FROM [student_profiles] WHERE [UserId]=@uidSch);
            IF NOT EXISTS (SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spIdSch AND [Date]=DATEADD(DAY,-@daySch,CAST(@Now AS DATE)))
                INSERT INTO [attendance_records] ([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt])
                SELECT NEWID(), @spIdSch, (SELECT TOP 1 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'ENG001'), DATEADD(DAY,-@daySch,CAST(@Now AS DATE)), @statSch, @Fac_Sch, @Now;
            SET @daySch += 1;
        END

        -- Results: 5 school subjects
        INSERT INTO [results] ([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
        SELECT NEWID(), sp.Id, co.Id, N'Final', 40+ABS(CHECKSUM(NEWID()))%61, 100, 1, @Now
        FROM [student_profiles] sp
        CROSS APPLY (SELECT TOP 5 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code IN (N'ENG001',N'MTH001',N'SCI001',N'SST001',N'URD001') ORDER BY c.Code) co
        WHERE sp.[UserId]=@uidSch;

        SET @stuNum += 1;
    END
    SET @cls += 1;
END
PRINT 'School data done.';

-- ════════════════════════════════════════════════
-- COLLEGE: ICS Year 11-12, 10 each = 20
-- ════════════════════════════════════════════════
PRINT 'Generating College students...';
DECLARE @yr INT = 1;
WHILE @yr <= 2
BEGIN
    DECLARE @classYr INT = 10 + @yr;
    DECLARE @j INT = 1;
    WHILE @j <= 10
    BEGIN
        DECLARE @uidCol UNIQUEIDENTIFIER = NEWID();
        DECLARE @unCol NVARCHAR(20) = CONCAT(N'col', @classYr, N's', @j);
        DECLARE @regCol NVARCHAR(50) = CONCAT(N'COL-REG-', @classYr, N'-', RIGHT(N'00'+CAST(@j AS NVARCHAR),2));
        DECLARE @fnCol NVARCHAR(100) = CASE (@j%10)
            WHEN 1 THEN N'Arslan Mehmood' WHEN 2 THEN N'Dua Fatima' WHEN 3 THEN N'Junaid Akhtar'
            WHEN 4 THEN N'Nimra Asif' WHEN 5 THEN N'Raheel Sharif' WHEN 6 THEN N'Wardah Sultan'
            WHEN 7 THEN N'Taimoor Javed' WHEN 8 THEN N'Sidra Batool' WHEN 9 THEN N'Ahsan Raza'
            WHEN 0 THEN N'Kashaf Idrees' END + N' Y' + CAST(@classYr AS NVARCHAR);

        IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Username]=@unCol)
            INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
            VALUES (@uidCol, @unCol, CONCAT(@unCol,N'@col.local'), @fnCol, @DefaultPwd, @R_Student, @D_IT_Col, @T_Col, @C_Col, 1, 1, @Now, 0);

        IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [UserId]=@uidCol)
            INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
            VALUES (NEWID(), @uidCol, @regCol, @P_ICS, @D_IT_Col, @yr, DATEADD(YEAR,-@yr,@Now), 2.0+CAST(ABS(CHECKSUM(NEWID()))%20 AS FLOAT)/10, @Now, 0);

        -- Attendance
        DECLARE @dayCol INT = 1;
        WHILE @dayCol <= 30
        BEGIN
            DECLARE @statCol NVARCHAR(10) = CASE WHEN ABS(CHECKSUM(NEWID()))%10<8 THEN N'Present' ELSE N'Absent' END;
            DECLARE @spIdCol UNIQUEIDENTIFIER = (SELECT Id FROM [student_profiles] WHERE [UserId]=@uidCol);
            IF NOT EXISTS (SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spIdCol AND [Date]=DATEADD(DAY,-@dayCol,CAST(@Now AS DATE)))
                INSERT INTO [attendance_records] ([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt])
                SELECT NEWID(), @spIdCol, (SELECT TOP 1 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE CASE WHEN @yr=1 THEN N'ICS11%' ELSE N'ICS12%' END), DATEADD(DAY,-@dayCol,CAST(@Now AS DATE)), @statCol, @Fac_Col, @Now;
            SET @dayCol += 1;
        END

        -- Results
        INSERT INTO [results] ([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
        SELECT NEWID(), sp.Id, co.Id, N'Final', 50+ABS(CHECKSUM(NEWID()))%51, 100, 1, @Now
        FROM [student_profiles] sp
        CROSS APPLY (SELECT TOP 5 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE CASE WHEN @yr=1 THEN N'ICS11%' WHEN @yr=2 THEN N'ICS12%' ELSE N'ENG1%' END ORDER BY c.Code) co
        WHERE sp.[UserId]=@uidCol;

        SET @j += 1;
    END
    SET @yr += 1;
END
PRINT 'College data done.';

-- ════════════════════════════════════════════════
-- UNIVERSITY: BSCS 8 sem x 10 = 80, BBA 8x10=80, MSE 4x10=40, Spanish 10
-- ════════════════════════════════════════════════
PRINT 'Generating University students...';

-- BSCS
DECLARE @bscsSem INT = 1;
WHILE @bscsSem <= 8
BEGIN
    DECLARE @bsJ INT = 1;
    WHILE @bsJ <= 10
    BEGIN
        DECLARE @uidB UNIQUEIDENTIFIER = NEWID();
        DECLARE @unB NVARCHAR(20) = CONCAT(N'bscs', @bscsSem, N's', @bsJ);
        DECLARE @regB NVARCHAR(50) = CONCAT(N'BSCS-REG-', @bscsSem, N'-', RIGHT(N'00'+CAST(@bsJ AS NVARCHAR),2));
        DECLARE @fnB NVARCHAR(100) = CASE (@bsJ%10) WHEN 1 THEN N'Ali Hassan' WHEN 2 THEN N'Sana Tariq' WHEN 3 THEN N'Bilal Ahmed' WHEN 4 THEN N'Ayesha Khan' WHEN 5 THEN N'Hamza Riaz' WHEN 6 THEN N'Fatima Zahra' WHEN 7 THEN N'Usman Khalid' WHEN 8 THEN N'Sara Noor' WHEN 9 THEN N'Hassan Ali' WHEN 0 THEN N'Zainab Malik' END + N' S' + CAST(@bscsSem AS NVARCHAR);

        IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Username]=@unB)
            INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
            VALUES (@uidB, @unB, CONCAT(@unB,N'@uni.local'), @fnB, @DefaultPwd, @R_Student, @D_IT_Uni, @T_Uni, @C_Uni, 2, 1, @Now, 0);

        IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [UserId]=@uidB)
            INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
            VALUES (NEWID(), @uidB, @regB, @P_BSCS, @D_IT_Uni, @bscsSem, DATEADD(YEAR,-@bscsSem/2,@Now), 2.5+CAST(ABS(CHECKSUM(NEWID()))%15 AS FLOAT)/10, @Now, 0);

        DECLARE @dayB INT = 1; WHILE @dayB <= 20 BEGIN DECLARE @statB NVARCHAR(10)=CASE WHEN ABS(CHECKSUM(NEWID()))%10<8 THEN N'Present' ELSE N'Absent' END; DECLARE @spIdB UNIQUEIDENTIFIER=(SELECT Id FROM [student_profiles] WHERE [UserId]=@uidB); IF NOT EXISTS(SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spIdB AND [Date]=DATEADD(DAY,-@dayB,CAST(@Now AS DATE))) INSERT INTO [attendance_records]([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt]) SELECT NEWID(),@spIdB,(SELECT TOP 1 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'CS'+CAST(@bscsSem AS NVARCHAR)+N'%'),DATEADD(DAY,-@dayB,CAST(@Now AS DATE)),@statB,@Fac_Uni,@Now; SET @dayB+=1; END

        INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
        SELECT NEWID(),sp.Id,co.Id,CASE(ABS(CHECKSUM(NEWID()))%3)WHEN 0 THEN N'Final' WHEN 1 THEN N'Midterm' ELSE N'Sessional' END,50+ABS(CHECKSUM(NEWID()))%51,100,1,@Now FROM [student_profiles] sp CROSS APPLY(SELECT TOP 5 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'CS'+CAST(@bscsSem AS NVARCHAR)+N'%' OR c.Code LIKE N'MTH'+CAST(@bscsSem AS NVARCHAR)+N'%' ORDER BY c.Code)co WHERE sp.[UserId]=@uidB;

        IF @bscsSem IN(7,8) AND @bsJ<=5 AND NOT EXISTS(SELECT 1 FROM [fyp_projects] WHERE [StudentProfileId]=(SELECT Id FROM [student_profiles] WHERE [UserId]=@uidB))
            INSERT INTO [fyp_projects]([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[CreatedAt])
            SELECT NEWID(),sp.Id,@D_IT_Uni,CONCAT(N'AI Learning Platform ',@regB),N'Adaptive ML system.',CASE WHEN @bscsSem=7 THEN N'InProgress' ELSE N'Completed' END,(SELECT TOP 1 u.Id FROM [users] u WHERE u.RoleId=@R_Faculty AND u.InstitutionType=2 ORDER BY NEWID()),@Now FROM [student_profiles] sp WHERE sp.[UserId]=@uidB;

        SET @bsJ+=1; END; SET @bscsSem+=1; END;
PRINT 'BSCS done.';

-- BBA
DECLARE @bbaSem INT = 1;
WHILE @bbaSem <= 8
BEGIN
    DECLARE @bbJ INT = 1;
    WHILE @bbJ <= 10
    BEGIN
        DECLARE @uidBba UNIQUEIDENTIFIER = NEWID();
        DECLARE @unBba NVARCHAR(20) = CONCAT(N'bba', @bbaSem, N's', @bbJ);
        DECLARE @regBba NVARCHAR(50) = CONCAT(N'BBA-REG-', @bbaSem, N'-', RIGHT(N'00'+CAST(@bbJ AS NVARCHAR),2));
        DECLARE @fnBba NVARCHAR(100) = CASE (@bbJ%10) WHEN 1 THEN N'Kamran Saeed' WHEN 2 THEN N'Nadia Amir' WHEN 3 THEN N'Rizwan Haider' WHEN 4 THEN N'Mehwish Iqbal' WHEN 5 THEN N'Danish Latif' WHEN 6 THEN N'Rabia Qureshi' WHEN 7 THEN N'Faisal Mahmood' WHEN 8 THEN N'Ayesha Siddiqui' WHEN 9 THEN N'Imran Khan' WHEN 0 THEN N'Sadia Noor' END + N' S' + CAST(@bbaSem AS NVARCHAR);

        IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Username]=@unBba)
            INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
            VALUES (@uidBba, @unBba, CONCAT(@unBba,N'@uni.local'), @fnBba, @DefaultPwd, @R_Student, @D_BUS, @T_Uni, @C_Uni, 2, 1, @Now, 0);

        IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [UserId]=@uidBba)
            INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
            VALUES (NEWID(), @uidBba, @regBba, @P_BBA, @D_BUS, @bbaSem, DATEADD(YEAR,-@bbaSem/2,@Now), 2.5+CAST(ABS(CHECKSUM(NEWID()))%15 AS FLOAT)/10, @Now, 0);

        DECLARE @dayBB INT = 1; WHILE @dayBB <= 15 BEGIN DECLARE @statBB NVARCHAR(10)=CASE WHEN ABS(CHECKSUM(NEWID()))%10<8 THEN N'Present' ELSE N'Absent' END; DECLARE @spIdBB UNIQUEIDENTIFIER=(SELECT Id FROM [student_profiles] WHERE [UserId]=@uidBba); IF NOT EXISTS(SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spIdBB AND [Date]=DATEADD(DAY,-@dayBB,CAST(@Now AS DATE))) INSERT INTO [attendance_records]([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt]) SELECT NEWID(),@spIdBB,(SELECT TOP 1 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'MGT'+CAST(@bbaSem AS NVARCHAR)+N'%'),DATEADD(DAY,-@dayBB,CAST(@Now AS DATE)),@statBB,@Fac_Uni,@Now; SET @dayBB+=1; END

        INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
        SELECT NEWID(),sp.Id,co.Id,CASE(ABS(CHECKSUM(NEWID()))%3)WHEN 0 THEN N'Final' WHEN 1 THEN N'Midterm' ELSE N'Sessional' END,50+ABS(CHECKSUM(NEWID()))%51,100,1,@Now FROM [student_profiles] sp CROSS APPLY(SELECT TOP 4 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'MGT'+CAST(@bbaSem AS NVARCHAR)+N'%' OR c.Code LIKE N'ACC'+CAST(@bbaSem AS NVARCHAR)+N'%' ORDER BY c.Code)co WHERE sp.[UserId]=@uidBba;

        SET @bbJ+=1; END; SET @bbaSem+=1; END;
PRINT 'BBA done.';

-- MSE
DECLARE @mseSem INT = 1;
WHILE @mseSem <= 4
BEGIN
    DECLARE @msJ INT = 1;
    WHILE @msJ <= 10
    BEGIN
        DECLARE @uidM UNIQUEIDENTIFIER = NEWID();
        DECLARE @unM NVARCHAR(20) = CONCAT(N'mse', @mseSem, N's', @msJ);
        DECLARE @regM NVARCHAR(50) = CONCAT(N'MSE-REG-', @mseSem, N'-', RIGHT(N'00'+CAST(@msJ AS NVARCHAR),2));
        DECLARE @fnM NVARCHAR(100) = CASE (@msJ%10) WHEN 1 THEN N'Adnan Yousaf' WHEN 2 THEN N'Hira Shahid' WHEN 3 THEN N'Taimoor Javed' WHEN 4 THEN N'Sidra Batool' WHEN 5 THEN N'Nasir Abbas' WHEN 6 THEN N'Kiran Aslam' WHEN 7 THEN N'Shahbaz Ali' WHEN 8 THEN N'Amber Rehman' WHEN 9 THEN N'Saad Farooq' WHEN 0 THEN N'Yumna Arif' END + N' S' + CAST(@mseSem AS NVARCHAR);

        IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Username]=@unM)
            INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
            VALUES (@uidM, @unM, CONCAT(@unM,N'@uni.local'), @fnM, @DefaultPwd, @R_Student, @D_IT_Uni, @T_Uni, @C_Uni, 2, 1, @Now, 0);

        IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [UserId]=@uidM)
            INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
            VALUES (NEWID(), @uidM, @regM, @P_MSE, @D_IT_Uni, @mseSem, DATEADD(YEAR,-@mseSem/2,@Now), 3.0+CAST(ABS(CHECKSUM(NEWID()))%10 AS FLOAT)/10, @Now, 0);

        DECLARE @dayM INT = 1; WHILE @dayM <= 15 BEGIN DECLARE @statM NVARCHAR(10)=CASE WHEN ABS(CHECKSUM(NEWID()))%10<8 THEN N'Present' ELSE N'Absent' END; DECLARE @spIdM UNIQUEIDENTIFIER=(SELECT Id FROM [student_profiles] WHERE [UserId]=@uidM); DECLARE @coIdM UNIQUEIDENTIFIER=(SELECT TOP 1 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'MSE'+CAST(@mseSem AS NVARCHAR)+N'%'); IF @coIdM IS NULL SET @coIdM=(SELECT TOP 1 Id FROM [course_offerings]); IF NOT EXISTS(SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spIdM AND [Date]=DATEADD(DAY,-@dayM,CAST(@Now AS DATE))) INSERT INTO [attendance_records]([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt]) SELECT NEWID(),@spIdM,@coIdM,DATEADD(DAY,-@dayM,CAST(@Now AS DATE)),@statM,@Fac_Uni,@Now; SET @dayM+=1; END

        INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
        SELECT NEWID(),sp.Id,co.Id,CASE(ABS(CHECKSUM(NEWID()))%3)WHEN 0 THEN N'Final' WHEN 1 THEN N'Midterm' ELSE N'Sessional' END,60+ABS(CHECKSUM(NEWID()))%41,100,1,@Now FROM [student_profiles] sp CROSS APPLY(SELECT TOP 3 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'MSE'+CAST(@mseSem AS NVARCHAR)+N'%' ORDER BY c.Code)co WHERE sp.[UserId]=@uidM;

        IF @mseSem IN(3,4) AND @msJ<=5 AND NOT EXISTS(SELECT 1 FROM [fyp_projects] WHERE [StudentProfileId]=(SELECT Id FROM [student_profiles] WHERE [UserId]=@uidM))
            INSERT INTO [fyp_projects]([Id],[StudentProfileId],[DepartmentId],[Title],[Description],[Status],[SupervisorUserId],[CreatedAt])
            SELECT NEWID(),sp.Id,@D_IT_Uni,CONCAT(N'Deep Learning Research ',@regM),N'Advanced ML thesis.',CASE WHEN @mseSem=3 THEN N'InProgress' ELSE N'Completed' END,(SELECT TOP 1 u.Id FROM [users] u WHERE u.RoleId=@R_Faculty AND u.InstitutionType=2 ORDER BY NEWID()),@Now FROM [student_profiles] sp WHERE sp.[UserId]=@uidM;

        SET @msJ+=1; END; SET @mseSem+=1; END;
PRINT 'MSE done.';

-- Spanish Language
DECLARE @spJ INT = 1;
WHILE @spJ <= 10
BEGIN
    DECLARE @uidSp UNIQUEIDENTIFIER = NEWID();
    DECLARE @unSp NVARCHAR(20) = CONCAT(N'spanish', @spJ);
    DECLARE @regSp NVARCHAR(50) = CONCAT(N'SPN-REG-', RIGHT(N'00'+CAST(@spJ AS NVARCHAR),2));
    DECLARE @fnSp NVARCHAR(100) = CASE (@spJ%10) WHEN 1 THEN N'Maria Garcia' WHEN 2 THEN N'Juan Martinez' WHEN 3 THEN N'Elena Lopez' WHEN 4 THEN N'Carlos Ruiz' WHEN 5 THEN N'Isabella Torres' WHEN 6 THEN N'Miguel Diaz' WHEN 7 THEN N'Sofia Ramirez' WHEN 8 THEN N'Diego Flores' WHEN 9 THEN N'Lucia Morales' WHEN 0 THEN N'Pablo Gutierrez' END;

    IF NOT EXISTS (SELECT 1 FROM [users] WHERE [Username]=@unSp)
        INSERT INTO [users] ([Id],[Username],[Email],[FullName],[PasswordHash],[RoleId],[DepartmentId],[TenantId],[CampusId],[InstitutionType],[IsActive],[CreatedAt],[IsDeleted])
        VALUES (@uidSp, @unSp, CONCAT(@unSp,N'@uni.local'), @fnSp, @DefaultPwd, @R_Student, @D_IT_Uni, @T_Uni, @C_Uni, 2, 1, @Now, 0);

    IF NOT EXISTS (SELECT 1 FROM [student_profiles] WHERE [UserId]=@uidSp)
        INSERT INTO [student_profiles] ([Id],[UserId],[RegistrationNumber],[ProgramId],[DepartmentId],[CurrentSemesterNumber],[AdmissionDate],[Cgpa],[CreatedAt],[IsDeleted])
        VALUES (NEWID(), @uidSp, @regSp, @P_SPANISH, @D_IT_Uni, 1, @Now, 3.0+CAST(ABS(CHECKSUM(NEWID()))%10 AS FLOAT)/10, @Now, 0);

    DECLARE @daySp INT = 1; WHILE @daySp <= 30 BEGIN DECLARE @statSp NVARCHAR(10)=CASE WHEN ABS(CHECKSUM(NEWID()))%10<9 THEN N'Present' ELSE N'Absent' END; DECLARE @spIdSp UNIQUEIDENTIFIER=(SELECT Id FROM [student_profiles] WHERE [UserId]=@uidSp); IF NOT EXISTS(SELECT 1 FROM [attendance_records] WHERE [StudentProfileId]=@spIdSp AND [Date]=DATEADD(DAY,-@daySp,CAST(@Now AS DATE))) INSERT INTO [attendance_records]([Id],[StudentProfileId],[CourseOfferingId],[Date],[Status],[MarkedByUserId],[CreatedAt]) SELECT NEWID(),@spIdSp,(SELECT TOP 1 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'SPN1%'),DATEADD(DAY,-@daySp,CAST(@Now AS DATE)),@statSp,@Fac_Uni,@Now; SET @daySp+=1; END

    INSERT INTO [results]([Id],[StudentProfileId],[CourseOfferingId],[ResultType],[MarksObtained],[MaxMarks],[IsPublished],[CreatedAt])
    SELECT NEWID(),sp.Id,co.Id,N'Final',60+ABS(CHECKSUM(NEWID()))%41,100,1,@Now FROM [student_profiles] sp CROSS APPLY(SELECT TOP 3 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId WHERE c.Code LIKE N'SPN%' ORDER BY c.Code)co WHERE sp.[UserId]=@uidSp;

    SET @spJ += 1;
END
PRINT 'Spanish done.';

-- ════════════════════════════════════════════════
-- TIMETABLES
-- ════════════════════════════════════════════════
PRINT 'Generating timetables...';
DECLARE @semTId UNIQUEIDENTIFIER;
DECLARE semCur CURSOR FOR SELECT Id FROM [semesters];
OPEN semCur; FETCH NEXT FROM semCur INTO @semTId;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @semNameT NVARCHAR(100) = (SELECT [Name] FROM [semesters] WHERE Id=@semTId);
    IF @semNameT LIKE N'Semester%' AND NOT EXISTS (SELECT 1 FROM [timetables] WHERE [SemesterId]=@semTId AND [DepartmentId]=@D_IT_Uni)
        INSERT INTO [timetables] ([Id],[DepartmentId],[SemesterId],[AcademicProgramId],[EffectiveDate],[SemesterNumber],[IsPublished],[IsDeleted],[CreatedAt])
        VALUES (NEWID(), @D_IT_Uni, @semTId, @P_BSCS, CAST(@Now AS DATE), 1, 1, 0, @Now);
    ELSE IF @semNameT LIKE N'Class%' AND NOT EXISTS (SELECT 1 FROM [timetables] WHERE [SemesterId]=@semTId AND [DepartmentId]=@D_IT_Sch)
        INSERT INTO [timetables] ([Id],[DepartmentId],[SemesterId],[AcademicProgramId],[EffectiveDate],[SemesterNumber],[IsPublished],[IsDeleted],[CreatedAt])
        VALUES (NEWID(), @D_IT_Sch, @semTId, @P_SCIENCE, CAST(@Now AS DATE), 1, 1, 0, @Now);
    ELSE IF @semNameT LIKE N'ICS%' AND NOT EXISTS (SELECT 1 FROM [timetables] WHERE [SemesterId]=@semTId AND [DepartmentId]=@D_IT_Col)
        INSERT INTO [timetables] ([Id],[DepartmentId],[SemesterId],[AcademicProgramId],[EffectiveDate],[SemesterNumber],[IsPublished],[IsDeleted],[CreatedAt])
        VALUES (NEWID(), @D_IT_Col, @semTId, @P_ICS, CAST(@Now AS DATE), 1, 1, 0, @Now);
    FETCH NEXT FROM semCur INTO @semTId;
END
CLOSE semCur; DEALLOCATE semCur;

PRINT '03-FullDummyData.sql completed successfully.';
GO

-- ════════════════════════════════════════════════
-- PHASE 4 SUPPLEMENT: Demo data for 14 empty tables
-- ════════════════════════════════════════════════
SET NOCOUNT ON;
GO
USE [Tabsan-EduSphere];
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @Fac_Uni UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=2);
DECLARE @Fac_Col UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=1);
DECLARE @Fac_Sch UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=3 AND InstitutionType=0);
DECLARE @Adm_Uni UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=2 AND InstitutionType=2);
DECLARE @Stu_Uni UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=4 AND InstitutionType=2);
DECLARE @Stu_Col UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=4 AND InstitutionType=1);
DECLARE @Stu_Sch UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=4 AND InstitutionType=0);
DECLARE @Fin_User UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [users] WHERE RoleId=5);
DECLARE @D_IT_Uni UNIQUEIDENTIFIER = 'D0000001-0000-0000-0000-000000000001';
DECLARE @D_BUS    UNIQUEIDENTIFIER = 'D0000002-0000-0000-0000-000000000002';
DECLARE @D_IT_Col UNIQUEIDENTIFIER = 'D0000003-0000-0000-0000-000000000003';
DECLARE @D_IT_Sch UNIQUEIDENTIFIER = 'D0000004-0000-0000-0000-000000000004';
DECLARE @T_Uni UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @T_Col UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @T_Sch UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @C_Uni UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
DECLARE @C_Col UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB';
DECLARE @C_Sch UNIQUEIDENTIFIER = 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC';
DECLARE @P_BSCS UNIQUEIDENTIFIER = 'A0000001-0000-0000-0000-000000000001';
DECLARE @P_BBA  UNIQUEIDENTIFIER = 'A0000002-0000-0000-0000-000000000002';
DECLARE @P_SCIENCE UNIQUEIDENTIFIER = 'A0000006-0000-0000-0000-000000000006';

PRINT '--- Phase 4 Supplement: Seeding 14 empty tables ---';

-- ════════════════════════════════════════════════
-- 1. BUILDINGS (3 per campus = 9 total)
-- ════════════════════════════════════════════════
PRINT '1. Buildings...';
IF NOT EXISTS (SELECT 1 FROM [buildings])
BEGIN
    INSERT INTO [buildings] ([Id],[TenantId],[CampusId],[Name],[Code],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES
    (NEWID(),@T_Uni,@C_Uni,N'Main Academic Block',N'UNI-MAIN',1,@Now,0,NULL),
    (NEWID(),@T_Uni,@C_Uni,N'Science & Technology Wing',N'UNI-SCI',1,@Now,0,NULL),
    (NEWID(),@T_Uni,@C_Uni,N'Business & Management Centre',N'UNI-BIZ',1,@Now,0,NULL),
    (NEWID(),@T_Col,@C_Col,N'College Main Building',N'COL-MAIN',1,@Now,0,NULL),
    (NEWID(),@T_Col,@C_Col,N'College IT Block',N'COL-IT',1,@Now,0,NULL),
    (NEWID(),@T_Col,@C_Col,N'College Library Block',N'COL-LIB',1,@Now,0,NULL),
    (NEWID(),@T_Sch,@C_Sch,N'School Main Building',N'SCH-MAIN',1,@Now,0,NULL),
    (NEWID(),@T_Sch,@C_Sch,N'School Junior Wing',N'SCH-JR',1,@Now,0,NULL),
    (NEWID(),@T_Sch,@C_Sch,N'School Senior Wing',N'SCH-SR',1,@Now,0,NULL);
END

-- ════════════════════════════════════════════════
-- 2. ROOMS (3 per building = 27 total)
-- ════════════════════════════════════════════════
PRINT '2. Rooms...';
IF NOT EXISTS (SELECT 1 FROM [rooms])
BEGIN
    DECLARE @bldId UNIQUEIDENTIFIER;
    DECLARE bldCur CURSOR FOR SELECT Id FROM [buildings] ORDER BY [Name];
    OPEN bldCur; FETCH NEXT FROM bldCur INTO @bldId;
    DECLARE @roomNum INT = 1;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO [rooms] ([Id],[TenantId],[CampusId],[Number],[BuildingId],[Capacity],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(),NULL,NULL,CONCAT(N'R',@roomNum),@bldId,30+ABS(CHECKSUM(NEWID()))%50,1,@Now,0,NULL);
        INSERT INTO [rooms] ([Id],[TenantId],[CampusId],[Number],[BuildingId],[Capacity],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(),NULL,NULL,CONCAT(N'R',@roomNum+1),@bldId,20+ABS(CHECKSUM(NEWID()))%40,1,@Now,0,NULL);
        INSERT INTO [rooms] ([Id],[TenantId],[CampusId],[Number],[BuildingId],[Capacity],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(),NULL,NULL,CONCAT(N'R',@roomNum+2),@bldId,10+ABS(CHECKSUM(NEWID()))%30,1,@Now,0,NULL);
        SET @roomNum += 3;
        FETCH NEXT FROM bldCur INTO @bldId;
    END
    CLOSE bldCur; DEALLOCATE bldCur;
END

-- ════════════════════════════════════════════════
-- 3. TIMETABLE ENTRIES (5 entries per timetable)
-- ════════════════════════════════════════════════
PRINT '3. Timetable entries...';
IF NOT EXISTS (SELECT 1 FROM [timetable_entries])
BEGIN
    DECLARE @ttId UNIQUEIDENTIFIER;
    DECLARE ttCur CURSOR FOR SELECT Id FROM [timetables];
    OPEN ttCur; FETCH NEXT FROM ttCur INTO @ttId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO [timetable_entries] ([Id],[TimetableId],[DayOfWeek],[StartTime],[EndTime],[SubjectName],[RoomNumber],[FacultyName],[CourseOfferingId],[CreatedAt])
        VALUES
        (NEWID(),@ttId,1,N'09:00',N'10:30',N'Introduction to Computing',N'R1',N'Dr. Ahmed Khan',NULL,@Now),
        (NEWID(),@ttId,1,N'11:00',N'12:30',N'Data Structures',N'R2',N'Dr. Sarah Ali',NULL,@Now),
        (NEWID(),@ttId,3,N'09:00',N'10:30',N'Calculus I',N'R3',N'Prof. Bilal Hussain',NULL,@Now),
        (NEWID(),@ttId,3,N'11:00',N'12:30',N'English Composition',N'R1',N'Ms. Fatima Noor',NULL,@Now),
        (NEWID(),@ttId,5,N'09:00',N'10:30',N'Programming Lab',N'R2',N'Dr. Ahmed Khan',NULL,@Now);
        FETCH NEXT FROM ttCur INTO @ttId;
    END
    CLOSE ttCur; DEALLOCATE ttCur;
END

-- ════════════════════════════════════════════════
-- 4. ENROLLMENTS (2-3 per student with course offerings)
-- ════════════════════════════════════════════════
PRINT '4. Enrollments...';
IF NOT EXISTS (SELECT 1 FROM [enrollments])
BEGIN
    INSERT INTO [enrollments] ([Id],[StudentProfileId],[CourseOfferingId],[EnrolledAt],[Status],[CreatedAt])
    SELECT NEWID(), sp.Id, co.Id, @Now, N'Active', @Now
    FROM [student_profiles] sp
    CROSS APPLY (SELECT TOP 2 co.Id FROM [course_offerings] co JOIN [courses] c ON c.Id=co.CourseId ORDER BY NEWID()) co
    WHERE NOT EXISTS (SELECT 1 FROM [enrollments] e WHERE e.StudentProfileId=sp.Id AND e.CourseOfferingId=co.Id);
END

-- ════════════════════════════════════════════════
-- 5. ASSIGNMENTS (2 per course offering)
-- ════════════════════════════════════════════════
PRINT '5. Assignments...';
IF NOT EXISTS (SELECT 1 FROM [assignments])
BEGIN
    DECLARE @coId UNIQUEIDENTIFIER;
    DECLARE coCur CURSOR FOR SELECT TOP 30 Id FROM [course_offerings] ORDER BY NEWID();
    OPEN coCur; FETCH NEXT FROM coCur INTO @coId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO [assignments] ([Id],[CourseOfferingId],[Title],[Description],[DueDate],[MaxMarks],[IsPublished],[PublishedAt],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES
        (NEWID(),@coId,N'Assignment 1: Research Paper',N'Write a 2000-word research paper on the course topic.',DATEADD(DAY,14,@Now),100,1,@Now,@Now,0,NULL);
        INSERT INTO [assignments] ([Id],[CourseOfferingId],[Title],[Description],[DueDate],[MaxMarks],[IsPublished],[PublishedAt],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES
        (NEWID(),@coId,N'Assignment 2: Problem Set',N'Complete all problems from Chapters 3-5.',DATEADD(DAY,21,@Now),50,1,@Now,@Now,0,NULL);
        FETCH NEXT FROM coCur INTO @coId;
    END
    CLOSE coCur; DEALLOCATE coCur;
END

-- ════════════════════════════════════════════════
-- 6. QUIZZES (1 per course offering)
-- ════════════════════════════════════════════════
PRINT '6. Quizzes...';
IF NOT EXISTS (SELECT 1 FROM [quizzes])
BEGIN
    DECLARE @qcoId UNIQUEIDENTIFIER;
    DECLARE qcoCur CURSOR FOR SELECT TOP 30 Id FROM [course_offerings] ORDER BY NEWID();
    OPEN qcoCur; FETCH NEXT FROM qcoCur INTO @qcoId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO [quizzes] ([Id],[CourseOfferingId],[Title],[Instructions],[TimeLimitMinutes],[MaxAttempts],[IsPublished],[IsActive],[CreatedByUserId],[CreatedAt])
        VALUES (NEWID(),@qcoId,N'Quiz: Mid-Term Review',N'Answer all questions. No external materials allowed.',30,2,1,1,ISNULL(@Fac_Uni,@Adm_Uni),@Now);
        FETCH NEXT FROM qcoCur INTO @qcoId;
    END
    CLOSE qcoCur; DEALLOCATE qcoCur;
END

-- ════════════════════════════════════════════════
-- 7. NOTIFICATIONS (system + user-generated)
-- ════════════════════════════════════════════════
PRINT '7. Notifications...';
IF NOT EXISTS (SELECT 1 FROM [notifications])
BEGIN
    DECLARE @nId1 UNIQUEIDENTIFIER = NEWID(), @nId2 UNIQUEIDENTIFIER = NEWID(), @nId3 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [notifications] ([Id],[Title],[Body],[Type],[SenderUserId],[IsSystemGenerated],[IsActive],[CreatedAt])
    VALUES
    (@nId1,N'Welcome to EduSphere',N'Your account has been created successfully. Please complete your profile.',N'System',NULL,1,1,@Now),
    (@nId2,N'New Assignment Posted',N'A new assignment has been posted for your enrolled course.',N'Academic',@Fac_Uni,0,1,@Now),
    (@nId3,N'Fee Payment Reminder',N'Please pay your semester fees by the due date to avoid late charges.',N'Finance',@Fin_User,0,1,@Now);

    -- Send to all students
    INSERT INTO [notification_recipients] ([Id],[NotificationId],[RecipientUserId],[IsRead],[ReadAt],[CreatedAt])
    SELECT NEWID(), n.Id, u.Id, 0, NULL, @Now
    FROM [notifications] n
    CROSS JOIN (SELECT Id FROM [users] WHERE RoleId=4) u
    WHERE NOT EXISTS (SELECT 1 FROM [notification_recipients] nr WHERE nr.NotificationId=n.Id AND nr.RecipientUserId=u.Id);
END

-- ════════════════════════════════════════════════
-- 8. PAYMENT RECEIPTS (3 per student)
-- ════════════════════════════════════════════════
PRINT '8. Payment receipts...';
IF NOT EXISTS (SELECT 1 FROM [payment_receipts])
BEGIN
    DECLARE @pIdx INT = 1;
    DECLARE @spId UNIQUEIDENTIFIER;
    DECLARE spCur CURSOR FOR SELECT TOP 50 Id FROM [student_profiles] ORDER BY [RegistrationNumber];
    OPEN spCur; FETCH NEXT FROM spCur INTO @spId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO [payment_receipts] ([Id],[StudentProfileId],[CreatedByUserId],[ReceiptNo],[Status],[Amount],[Description],[DueDate],[Notes],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt])
        VALUES
        (NEWID(),@spId,ISNULL(@Fin_User,@Adm_Uni),CONCAT(N'RCP-',FORMAT(@pIdx,N'00000')),2,5000.00,N'Semester Tuition Fee',DATEADD(DAY,30,@Now),N'Paid in full',@Now,@Now,0,NULL);
        SET @pIdx += 1;
        INSERT INTO [payment_receipts] ([Id],[StudentProfileId],[CreatedByUserId],[ReceiptNo],[Status],[Amount],[Description],[DueDate],[Notes],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt])
        VALUES
        (NEWID(),@spId,ISNULL(@Fin_User,@Adm_Uni),CONCAT(N'RCP-',FORMAT(@pIdx,N'00000')),1,2500.00,N'Library & Lab Fee',DATEADD(DAY,15,@Now),N'Pending',@Now,@Now,0,NULL);
        SET @pIdx += 1;
        INSERT INTO [payment_receipts] ([Id],[StudentProfileId],[CreatedByUserId],[ReceiptNo],[Status],[Amount],[Description],[DueDate],[Notes],[CreatedAt],[UpdatedAt],[IsDeleted],[DeletedAt])
        VALUES
        (NEWID(),@spId,ISNULL(@Fin_User,@Adm_Uni),CONCAT(N'RCP-',FORMAT(@pIdx,N'00000')),0,1000.00,N'Sports & Activity Fee',DATEADD(DAY,-10,@Now),N'Overdue',@Now,@Now,0,NULL);
        SET @pIdx += 1;
        FETCH NEXT FROM spCur INTO @spId;
    END
    CLOSE spCur; DEALLOCATE spCur;
END

-- ════════════════════════════════════════════════
-- 9. STUDY PLANS (University students only)
-- ════════════════════════════════════════════════
PRINT '9. Study plans...';
IF NOT EXISTS (SELECT 1 FROM [study_plans])
BEGIN
    INSERT INTO [study_plans] ([Id],[StudentProfileId],[PlannedSemesterName],[Notes],[AdvisorStatus],[ReviewedByUserId],[CreatedAt],[IsDeleted],[DeletedAt])
    SELECT NEWID(), sp.Id, CONCAT(N'Semester ',sp.CurrentSemesterNumber+1),
           CONCAT(N'Plan to take 5 courses in Semester ',sp.CurrentSemesterNumber+1),
           1, @Fac_Uni, @Now, 0, NULL
    FROM [student_profiles] sp
    JOIN [departments] d ON d.Id=sp.DepartmentId
    WHERE d.InstitutionType=0  -- University only
      AND NOT EXISTS (SELECT 1 FROM [study_plans] s WHERE s.StudentProfileId=sp.Id);

    -- Add courses to study plans
    DECLARE @planId UNIQUEIDENTIFIER;
    DECLARE planCur CURSOR FOR SELECT Id FROM [study_plans];
    OPEN planCur; FETCH NEXT FROM planCur INTO @planId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO [study_plan_courses] ([Id],[StudyPlanId],[CourseId],[CreatedAt])
        SELECT NEWID(), @planId, c.Id, @Now
        FROM [courses] c WHERE c.DepartmentId=@D_IT_Uni
        ORDER BY NEWID() OFFSET 0 ROWS FETCH NEXT 3 ROWS ONLY;
        FETCH NEXT FROM planCur INTO @planId;
    END
    CLOSE planCur; DEALLOCATE planCur;
END

-- ════════════════════════════════════════════════
-- 10. COURSE ANNOUNCEMENTS (2 per offering)
-- ════════════════════════════════════════════════
PRINT '10. Course announcements...';
IF NOT EXISTS (SELECT 1 FROM [course_announcements])
BEGIN
    DECLARE @acoId UNIQUEIDENTIFIER;
    DECLARE acoCur CURSOR FOR SELECT TOP 20 Id FROM [course_offerings] ORDER BY NEWID();
    OPEN acoCur; FETCH NEXT FROM acoCur INTO @acoId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO [course_announcements] ([Id],[OfferingId],[AuthorId],[Title],[Body],[PostedAt],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(),@acoId,ISNULL(@Fac_Uni,@Adm_Uni),N'Welcome to the Course',N'Welcome everyone! Please review the syllabus and course schedule.',@Now,@Now,0,NULL);
        INSERT INTO [course_announcements] ([Id],[OfferingId],[AuthorId],[Title],[Body],[PostedAt],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(),@acoId,ISNULL(@Fac_Uni,@Adm_Uni),N'Mid-Term Exam Schedule',N'The mid-term exam will be held in Week 8. Covering chapters 1-6.',DATEADD(DAY,7,@Now),@Now,0,NULL);
        FETCH NEXT FROM acoCur INTO @acoId;
    END
    CLOSE acoCur; DEALLOCATE acoCur;
END

-- ════════════════════════════════════════════════
-- 11. DISCUSSION THREADS + REPLIES (2 per offering)
-- ════════════════════════════════════════════════
PRINT '11. Discussion threads...';
IF NOT EXISTS (SELECT 1 FROM [discussion_threads])
BEGIN
    DECLARE @dcoId UNIQUEIDENTIFIER;
    DECLARE dcoCur CURSOR FOR SELECT TOP 15 Id FROM [course_offerings] ORDER BY NEWID();
    OPEN dcoCur; FETCH NEXT FROM dcoCur INTO @dcoId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @dtId1 UNIQUEIDENTIFIER = NEWID();
        INSERT INTO [discussion_threads] ([Id],[OfferingId],[Title],[AuthorId],[IsPinned],[IsClosed],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (@dtId1,@dcoId,N'Questions about Assignment 1',ISNULL(@Stu_Uni,@Adm_Uni),0,0,@Now,0,NULL);
        -- Add reply
        INSERT INTO [discussion_replies] ([Id],[ThreadId],[AuthorId],[Body],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(),@dtId1,ISNULL(@Fac_Uni,@Adm_Uni),N'Great question! Please refer to the assignment guidelines posted in the announcements.',@Now,0,NULL);

        DECLARE @dtId2 UNIQUEIDENTIFIER = NEWID();
        INSERT INTO [discussion_threads] ([Id],[OfferingId],[Title],[AuthorId],[IsPinned],[IsClosed],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (@dtId2,@dcoId,N'Study Group Formation',ISNULL(@Stu_Uni,@Adm_Uni),1,0,@Now,0,NULL);
        INSERT INTO [discussion_replies] ([Id],[ThreadId],[AuthorId],[Body],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (NEWID(),@dtId2,ISNULL(@Stu_Col,@Stu_Uni),N'I am interested! Let us form a group for the final exam preparation.',@Now,0,NULL);
        FETCH NEXT FROM dcoCur INTO @dcoId;
    END
    CLOSE dcoCur; DEALLOCATE dcoCur;
END

-- ════════════════════════════════════════════════
-- 12. SUPPORT TICKETS (5 sample tickets)
-- ════════════════════════════════════════════════
PRINT '12. Support tickets...';
IF NOT EXISTS (SELECT 1 FROM [support_tickets])
BEGIN
    INSERT INTO [support_tickets] ([Id],[SubmitterId],[DepartmentId],[Category],[Subject],[Body],[Status],[AssignedToId],[ReopenWindowDays],[CreatedAt],[IsDeleted],[DeletedAt])
    VALUES
    (NEWID(),ISNULL(@Stu_Uni,@Adm_Uni),@D_IT_Uni,1,N'Cannot access course materials',N'I am unable to download the lecture slides for CS101.',0,NULL,7,@Now,0,NULL),
    (NEWID(),ISNULL(@Stu_Col,@Stu_Uni),@D_IT_Col,2,N'Payment not reflected',N'I paid my fees yesterday but the portal still shows pending.',0,NULL,7,@Now,0,NULL),
    (NEWID(),ISNULL(@Fac_Uni,@Adm_Uni),@D_IT_Uni,3,N'Attendance sync issue',N'Attendance records for my class are not syncing properly.',1,@Adm_Uni,7,@Now,0,NULL),
    (NEWID(),ISNULL(@Stu_Sch,@Adm_Uni),@D_IT_Sch,1,N'Login issue',N'I forgot my password and the reset link is not working.',0,NULL,14,@Now,0,NULL),
    (NEWID(),ISNULL(@Fac_Col,@Adm_Uni),@D_IT_Col,4,N'Feature request: Batch grading',N'It would be helpful to have batch grading for assignments.',2,@Adm_Uni,30,@Now,0,NULL);
END

-- ════════════════════════════════════════════════
-- 13. RUBRICS (1 per assignment with criteria + levels)
-- ════════════════════════════════════════════════
PRINT '13. Rubrics...';
IF NOT EXISTS (SELECT 1 FROM [rubrics])
BEGIN
    DECLARE @aId UNIQUEIDENTIFIER;
    DECLARE aCur CURSOR FOR SELECT TOP 10 Id FROM [assignments] ORDER BY NEWID();
    OPEN aCur; FETCH NEXT FROM aCur INTO @aId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @rId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO [rubrics] ([Id],[AssignmentId],[Title],[IsActive],[CreatedAt],[IsDeleted],[DeletedAt])
        VALUES (@rId,@aId,N'Grading Rubric',1,@Now,0,NULL);

        -- Criteria 1
        DECLARE @c1 UNIQUEIDENTIFIER = NEWID();
        INSERT INTO [rubric_criteria] ([Id],[RubricId],[Name],[MaxPoints],[DisplayOrder],[CreatedAt])
        VALUES (@c1,@rId,N'Content Quality',40,1,@Now);
        INSERT INTO [rubric_levels] ([Id],[CriterionId],[Label],[PointsAwarded],[DisplayOrder],[CreatedAt])
        VALUES
        (NEWID(),@c1,N'Excellent',40,1,@Now),
        (NEWID(),@c1,N'Good',30,2,@Now),
        (NEWID(),@c1,N'Satisfactory',20,3,@Now),
        (NEWID(),@c1,N'Needs Improvement',10,4,@Now);

        -- Criteria 2
        DECLARE @c2 UNIQUEIDENTIFIER = NEWID();
        INSERT INTO [rubric_criteria] ([Id],[RubricId],[Name],[MaxPoints],[DisplayOrder],[CreatedAt])
        VALUES (@c2,@rId,N'Organization & Structure',30,2,@Now);
        INSERT INTO [rubric_levels] ([Id],[CriterionId],[Label],[PointsAwarded],[DisplayOrder],[CreatedAt])
        VALUES
        (NEWID(),@c2,N'Well-structured',30,1,@Now),
        (NEWID(),@c2,N'Mostly organized',20,2,@Now),
        (NEWID(),@c2,N'Somewhat disorganized',10,3,@Now);

        -- Criteria 3
        DECLARE @c3 UNIQUEIDENTIFIER = NEWID();
        INSERT INTO [rubric_criteria] ([Id],[RubricId],[Name],[MaxPoints],[DisplayOrder],[CreatedAt])
        VALUES (@c3,@rId,N'Grammar & Presentation',30,3,@Now);
        INSERT INTO [rubric_levels] ([Id],[CriterionId],[Label],[PointsAwarded],[DisplayOrder],[CreatedAt])
        VALUES
        (NEWID(),@c3,N'Error-free',30,1,@Now),
        (NEWID(),@c3,N'Few minor errors',20,2,@Now),
        (NEWID(),@c3,N'Several errors',10,3,@Now);

        FETCH NEXT FROM aCur INTO @aId;
    END
    CLOSE aCur; DEALLOCATE aCur;
END

-- ════════════════════════════════════════════════
-- 14. COURSE PREREQUISITES (chain pattern)
-- ════════════════════════════════════════════════
PRINT '14. Course prerequisites...';
IF NOT EXISTS (SELECT 1 FROM [course_prerequisites])
BEGIN
    DECLARE @c101 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [courses] WHERE [Code]=N'CS101' AND [DepartmentId]=@D_IT_Uni);
    DECLARE @c201 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [courses] WHERE [Code]=N'CS201' AND [DepartmentId]=@D_IT_Uni);
    DECLARE @c301 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [courses] WHERE [Code]=N'CS301' AND [DepartmentId]=@D_IT_Uni);
    DECLARE @eng UNIQUEIDENTIFIER  = (SELECT TOP 1 Id FROM [courses] WHERE [Code]=N'ENG001');
    DECLARE @mth UNIQUEIDENTIFIER  = (SELECT TOP 1 Id FROM [courses] WHERE [Code]=N'MTH001');

    IF @c101 IS NOT NULL AND @c201 IS NOT NULL
        INSERT INTO [course_prerequisites] ([Id],[CourseId],[PrerequisiteCourseId],[CreatedAt])
        VALUES (NEWID(),@c201,@c101,@Now);
    IF @c201 IS NOT NULL AND @c301 IS NOT NULL
        INSERT INTO [course_prerequisites] ([Id],[CourseId],[PrerequisiteCourseId],[CreatedAt])
        VALUES (NEWID(),@c301,@c201,@Now);
    IF @eng IS NOT NULL AND @c101 IS NOT NULL
        INSERT INTO [course_prerequisites] ([Id],[CourseId],[PrerequisiteCourseId],[CreatedAt])
        VALUES (NEWID(),@c101,@eng,@Now);
    IF @mth IS NOT NULL AND @c201 IS NOT NULL
        INSERT INTO [course_prerequisites] ([Id],[CourseId],[PrerequisiteCourseId],[CreatedAt])
        VALUES (NEWID(),@c201,@mth,@Now);
END

PRINT '--- Phase 4 Supplement completed ---';
GO
