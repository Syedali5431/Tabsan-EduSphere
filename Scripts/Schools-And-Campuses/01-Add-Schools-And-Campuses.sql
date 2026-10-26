-- =====================================================================================
-- 01-Add-Schools-And-Campuses.sql  —  Tabsan EduSphere
-- Adds several schools and their campuses. In EduSphere a school is a tenant; each
-- campus belongs to one school.
--
-- Edit the two lists below (@Schools, @Campuses) to match your schools, then run against
-- [Tabsan-EduSphere] after 02-Seed-Core.sql. Safe to re-run: schools are keyed on their
-- code, campuses on (school, campus code); rows that already exist are left unchanged.
--
-- A school shows in the portal once a campus has a School department:
-- run 02-Add-School-Sections.sql next.
-- =====================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Schools: code (unique, up to 64 characters) and name.
DECLARE @Schools TABLE (Code NVARCHAR(64) PRIMARY KEY, Name NVARCHAR(200) NOT NULL);
INSERT INTO @Schools (Code, Name) VALUES
 (N'TMS',  N'Tabsan Model School'),
 (N'CPS',  N'Crescent Public School'),
 (N'GVGS', N'Green Valley Grammar School');

-- Campuses: school code, campus code (unique within the school, up to 16 characters so the
-- department and class codes made from it fit) and name.
DECLARE @Campuses TABLE (SchoolCode NVARCHAR(64) NOT NULL, Code NVARCHAR(16) NOT NULL, Name NVARCHAR(200) NOT NULL, PRIMARY KEY (SchoolCode, Code));
INSERT INTO @Campuses (SchoolCode, Code, Name) VALUES
 (N'TMS',  N'TMS-GUL',  N'Gulberg Campus'),
 (N'TMS',  N'TMS-JT',   N'Johar Town Campus'),
 (N'TMS',  N'TMS-DHA',  N'DHA Campus'),
 (N'CPS',  N'CPS-MAIN', N'Main Campus'),
 (N'CPS',  N'CPS-NTH',  N'North Campus'),
 (N'GVGS', N'GVGS-CTY', N'City Campus'),
 (N'GVGS', N'GVGS-MT',  N'Model Town Campus');

-- A school code already used by a deleted tenant cannot be reused (the code is unique).
IF EXISTS (SELECT 1 FROM @Schools s JOIN tenants t ON t.Code = s.Code AND t.IsDeleted = 1)
BEGIN
    DECLARE @Blocked NVARCHAR(2000) =(SELECT STRING_AGG(s.Code, N', ') FROM @Schools s JOIN tenants t ON t.Code = s.Code AND t.IsDeleted = 1);
    RAISERROR(N'01-Add-Schools-And-Campuses: these school codes belong to deleted tenants; restore them or choose other codes: %s', 16, 1, @Blocked);
    RETURN;
END

IF EXISTS (SELECT 1 FROM @Campuses c WHERE NOT EXISTS (SELECT 1 FROM @Schools s WHERE s.Code = c.SchoolCode))
BEGIN
    RAISERROR(N'01-Add-Schools-And-Campuses: every campus must name a school listed in @Schools.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

-- Schools (tenants)
INSERT INTO tenants (Id, Code, Name, IsActive, IsDeleted, CreatedAt)
SELECT NEWID(), s.Code, s.Name, 1, 0, @Now
FROM @Schools s
WHERE NOT EXISTS (SELECT 1 FROM tenants t WHERE t.Code = s.Code);
PRINT CONCAT(N'Schools added: ', @@ROWCOUNT);

-- Campuses
INSERT INTO campuses (Id, TenantId, Code, Name, IsActive, IsDeleted, CreatedAt)
SELECT NEWID(), t.Id, c.Code, c.Name, 1, 0, @Now
FROM @Campuses c
JOIN tenants t ON t.Code = c.SchoolCode AND t.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM campuses x WHERE x.TenantId = t.Id AND x.Code = c.Code);
PRINT CONCAT(N'Campuses added: ', @@ROWCOUNT);

COMMIT TRANSACTION;

-- What the listed schools now have
SELECT t.Code AS SchoolCode, t.Name AS School, c.Code AS CampusCode, c.Name AS Campus, c.IsActive AS CampusActive
FROM @Schools s
JOIN tenants t ON t.Code = s.Code AND t.IsDeleted = 0
LEFT JOIN campuses c ON c.TenantId = t.Id AND c.IsDeleted = 0
ORDER BY t.Code, c.Code;
