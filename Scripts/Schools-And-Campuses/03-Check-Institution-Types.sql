-- =====================================================================================
-- 03-Check-Institution-Types.sql  —  Tabsan EduSphere
-- Read-only. Shows which institution types the portal displays and why.
--
-- The portal shows an institution type (School, College, University) only when it is
-- switched on in the institution policy AND has at least one active department. With
-- School departments only, College and University menus, modules, filters, dashboard
-- cards and reports are hidden; they come back by themselves (within a minute, or at
-- once when added in the portal) when a College or University department exists.
-- If none of the enabled types has a department yet, the portal shows the policy as is.
-- =====================================================================================
SET NOCOUNT ON;

DECLARE @Types TABLE (InstitutionType INT PRIMARY KEY, Name NVARCHAR(20), PolicyKey NVARCHAR(64), PolicyDefault NVARCHAR(5));
INSERT INTO @Types VALUES
 (0, N'University', N'institution_include_university', N'true'),
 (1, N'School',     N'institution_include_school',     N'false'),
 (2, N'College',    N'institution_include_college',    N'false');

DECLARE @Report TABLE (InstitutionType INT, Name NVARCHAR(20), EnabledInPolicy BIT, ActiveDepartments INT);
INSERT INTO @Report
SELECT t.InstitutionType, t.Name,
       CASE WHEN COALESCE((SELECT TOP 1 s.[Value] FROM portal_settings s WHERE s.[Key] = t.PolicyKey), t.PolicyDefault) = N'true' THEN 1 ELSE 0 END,
       (SELECT COUNT(*) FROM departments d WHERE d.InstitutionType = t.InstitutionType AND d.IsActive = 1 AND d.IsDeleted = 0)
FROM @Types t;

DECLARE @AnyShown BIT = CASE WHEN EXISTS (SELECT 1 FROM @Report WHERE EnabledInPolicy = 1 AND ActiveDepartments > 0) THEN 1 ELSE 0 END;

SELECT Name AS InstitutionType, EnabledInPolicy, ActiveDepartments,
       CASE WHEN EnabledInPolicy = 0 THEN N'Hidden (switched off in the institution policy)'
            WHEN @AnyShown = 0 THEN N'Shown (no enabled type has departments yet, so the policy applies as is)'
            WHEN ActiveDepartments > 0 THEN N'Shown'
            ELSE N'Hidden until its first active department is added' END AS InThePortal
FROM @Report r ORDER BY r.InstitutionType;

DECLARE @Shown NVARCHAR(200) = (SELECT STRING_AGG(Name, N', ') WITHIN GROUP (ORDER BY InstitutionType) FROM @Report
                                WHERE EnabledInPolicy = 1 AND (ActiveDepartments > 0 OR @AnyShown = 0));
PRINT CONCAT(N'The portal shows: ', @Shown);

-- The departments that keep College or University visible (e.g. seeded by 02-Seed-Core.sql).
-- To run School-only, deactivate them on the Departments page; activate them again to bring the types back.
SELECT CASE d.InstitutionType WHEN 0 THEN N'University' WHEN 2 THEN N'College' END AS InstitutionType,
       d.Code, d.Name, t.Code AS Tenant, c.Code AS Campus
FROM departments d
LEFT JOIN tenants t ON t.Id = d.TenantId
LEFT JOIN campuses c ON c.Id = d.CampusId
WHERE d.InstitutionType IN (0, 2) AND d.IsActive = 1 AND d.IsDeleted = 0
ORDER BY d.InstitutionType, d.Code;

-- Schools (tenants with School sections) and their campuses
SELECT t.Code AS SchoolCode, t.Name AS School, c.Code AS CampusCode, c.Name AS Campus,
       (SELECT COUNT(*) FROM departments d WHERE d.CampusId = c.Id AND d.InstitutionType = 1 AND d.IsActive = 1 AND d.IsDeleted = 0) AS SchoolSections
FROM tenants t
JOIN campuses c ON c.TenantId = t.Id AND c.IsDeleted = 0
WHERE t.IsDeleted = 0
  AND EXISTS (SELECT 1 FROM departments d WHERE d.TenantId = t.Id AND d.InstitutionType = 1 AND d.IsDeleted = 0)
ORDER BY t.Code, c.Code;
