-- =====================================================================================
-- 02-Add-School-Sections.sql  —  Tabsan EduSphere
-- Gives every campus of the schools added by 01-Add-Schools-And-Campuses.sql a School
-- section (a department of institution type School) with a Class 1 to 10 programme, so
-- the schools, their campuses and classes appear throughout the portal.
--
--   * One department per campus: code <campus code>-SCH, e.g. TMS-GUL-SCH
--   * One programme per department: code <campus code>-CLS, Class 1 to 10
--   * The classes "Class 1" … "Class 10" are added if the database has none yet
--   * School is switched on in the institution policy if it is off. On a database where
--     College or University has never been set (a new one), they are switched on too, so
--     their first departments can be added later and they appear by themselves; until
--     then the portal hides them. A choice already made on Institution Policy is kept.
--
-- Run against [Tabsan-EduSphere] after 01. Keep @Schools in step with 01. Safe to re-run:
-- departments are keyed on (school, campus, code), programmes on their code.
-- =====================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @School INT = 1; -- InstitutionType: 0 University, 1 School, 2 College

-- The schools whose campuses get a School section (the codes from 01).
DECLARE @Schools TABLE (Code NVARCHAR(64) PRIMARY KEY);
INSERT INTO @Schools (Code) VALUES (N'TMS'), (N'CPS'), (N'GVGS');

DECLARE @Targets TABLE (TenantId UNIQUEIDENTIFIER, CampusId UNIQUEIDENTIFIER, CampusCode NVARCHAR(64), CampusName NVARCHAR(200));
INSERT INTO @Targets
SELECT t.Id, c.Id, c.Code, c.Name
FROM @Schools s
JOIN tenants t ON t.Code = s.Code AND t.IsDeleted = 0
JOIN campuses c ON c.TenantId = t.Id AND c.IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM @Targets)
BEGIN
    RAISERROR(N'02-Add-School-Sections: none of the listed schools has a campus; run 01-Add-Schools-And-Campuses.sql first.', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM @Targets WHERE LEN(CampusCode) > 16)
BEGIN
    RAISERROR(N'02-Add-School-Sections: campus codes must be at most 16 characters (department and programme codes are built from them).', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

-- School sections (departments of institution type School), one per campus
INSERT INTO departments (Id, Name, Code, IsActive, IsDeleted, CreatedAt, TenantId, CampusId, InstitutionType)
SELECT NEWID(), CONCAT(x.CampusName, N' - Class 1 to 10'), CONCAT(x.CampusCode, N'-SCH'), 1, 0, @Now, x.TenantId, x.CampusId, @School
FROM @Targets x
WHERE NOT EXISTS (SELECT 1 FROM departments d WHERE d.TenantId = x.TenantId AND d.CampusId = x.CampusId AND d.Code = CONCAT(x.CampusCode, N'-SCH'));
PRINT CONCAT(N'School sections added: ', @@ROWCOUNT);

-- Class 1 to 10 programme in each section
INSERT INTO academic_programs (Id, Name, Code, DepartmentId, TotalSemesters, IsActive, CreatedAt, IsDeleted, MaxCreditLoadPerSemester)
SELECT NEWID(), CONCAT(N'Class 1 to 10 (', x.CampusName, N')'), CONCAT(x.CampusCode, N'-CLS'), d.Id, 10, 1, @Now, 0, 18
FROM @Targets x
JOIN departments d ON d.TenantId = x.TenantId AND d.CampusId = x.CampusId AND d.Code = CONCAT(x.CampusCode, N'-SCH') AND d.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM academic_programs p WHERE p.Code = CONCAT(x.CampusCode, N'-CLS'));
PRINT CONCAT(N'Class programmes added: ', @@ROWCOUNT);

-- Classes 1 to 10 (shared by every school), for a database that has none yet; the school year runs April to March
DECLARE @YearStart DATETIME2 = DATEFROMPARTS(YEAR(@Now) - CASE WHEN MONTH(@Now) < 4 THEN 1 ELSE 0 END, 4, 1);
INSERT INTO semesters (Id, Name, StartDate, EndDate, IsClosed, CreatedAt, IsDeleted)
SELECT NEWID(), CONCAT(N'Class ', n.n), @YearStart, DATEADD(DAY, -1, DATEADD(YEAR, 1, @YearStart)), 0, @Now, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) n(n)
WHERE NOT EXISTS (SELECT 1 FROM semesters s WHERE s.Name = CONCAT(N'Class ', n.n) AND s.IsDeleted = 0);
PRINT CONCAT(N'Classes added: ', @@ROWCOUNT);

-- School must be allowed by the institution policy (otherwise the portal refuses School departments).
IF EXISTS (SELECT 1 FROM portal_settings WHERE [Key] = N'institution_include_school')
    UPDATE portal_settings SET [Value] = N'true', UpdatedAt = @Now WHERE [Key] = N'institution_include_school' AND [Value] <> N'true';
ELSE
    INSERT INTO portal_settings (Id, [Key], [Value], CreatedAt) VALUES (NEWID(), N'institution_include_school', N'true', @Now);
PRINT CONCAT(N'School enabled in the institution policy (rows changed: ', @@ROWCOUNT, N'). The API reads the policy every 10 minutes; restart it to apply at once.');

-- College and University: allowed unless a choice has been made (a new database has no setting, which means
-- College off), so adding their first department later is accepted and shows them without changing the policy.
INSERT INTO portal_settings (Id, [Key], [Value], CreatedAt)
SELECT NEWID(), k.[Key], N'true', @Now
FROM (VALUES (N'institution_include_college'), (N'institution_include_university')) k([Key])
WHERE NOT EXISTS (SELECT 1 FROM portal_settings p WHERE p.[Key] = k.[Key]);
PRINT CONCAT(N'College/University policy settings added (only where none existed): ', @@ROWCOUNT);

COMMIT TRANSACTION;

-- Sections and programmes of the listed schools
SELECT t.Code AS SchoolCode, c.Code AS CampusCode, d.Code AS SectionCode, d.Name AS Section, p.Code AS ProgrammeCode, p.TotalSemesters AS Classes
FROM @Targets x
JOIN tenants t ON t.Id = x.TenantId
JOIN campuses c ON c.Id = x.CampusId
LEFT JOIN departments d ON d.CampusId = x.CampusId AND d.TenantId = x.TenantId AND d.IsDeleted = 0
LEFT JOIN academic_programs p ON p.DepartmentId = d.Id AND p.IsDeleted = 0
ORDER BY t.Code, c.Code, d.Code;
