-- =============================================================================
-- Fix sidebar menu role visibility per user specification
-- Run against: Tabsan-EduSphere (localdb)\MSSQLLocalDB
-- =============================================================================

-- Step 1: Ensure all 5 roles exist for every active menu item
INSERT INTO sidebar_menu_role_accesses (Id, SidebarMenuItemId, RoleName, IsAllowed, CreatedAt)
SELECT NEWID(), smi.Id, r.RoleName, 0, SYSUTCDATETIME()
FROM sidebar_menu_items smi
CROSS JOIN (VALUES ('SuperAdmin'),('Admin'),('Faculty'),('Student'),('Finance')) AS r(RoleName)
WHERE smi.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM sidebar_menu_role_accesses ra
    WHERE ra.SidebarMenuItemId = smi.Id AND ra.RoleName = r.RoleName
  );

-- Step 2: DISABLE everything for all roles first (then selectively enable)
UPDATE ra
SET ra.IsAllowed = 0
FROM sidebar_menu_role_accesses ra
JOIN sidebar_menu_items smi ON smi.Id = ra.SidebarMenuItemId
WHERE smi.IsDeleted = 0;

-- =============================================================================
-- Step 3: ENABLE specific menus per role
-- =============================================================================

-- ── SUPERADMIN (all menus) ──────────────────────────────────────────────────
UPDATE ra SET ra.IsAllowed = 1
FROM sidebar_menu_role_accesses ra
JOIN sidebar_menu_items smi ON smi.Id = ra.SidebarMenuItemId
WHERE smi.IsDeleted = 0
  AND ra.RoleName = 'SuperAdmin';

-- ── ALL ROLES (common menus) ─────────────────────────────────────────────────
UPDATE ra SET ra.IsAllowed = 1
FROM sidebar_menu_role_accesses ra
JOIN sidebar_menu_items smi ON smi.Id = ra.SidebarMenuItemId
WHERE smi.[Key] IN ('notifications','helpdesk','user_settings','theme_settings')
  AND ra.RoleName IN ('SuperAdmin','Admin','Faculty','Student','Finance');

-- ── ADMIN ────────────────────────────────────────────────────────────────────
UPDATE ra SET ra.IsAllowed = 1
FROM sidebar_menu_role_accesses ra
JOIN sidebar_menu_items smi ON smi.Id = ra.SidebarMenuItemId
WHERE smi.[Key] IN (
    -- Overview
    'dashboard',
    -- Setup Flow
    'departments','programs','courses','enrollments','students',
    -- Faculty Related
    'timetable_admin','timetable_teacher','assignments','enter_attendance','enter_results',
    'gradebook','rubric_manage','lms_manage','course_material','discussion','announcements',
    -- Student Related
    'results','student_lifecycle',
    -- Academic Related
    'result_calculation','prerequisites','generate_certificates','grading_config','study_plan',
    -- Settings Related
    'user_import','buildings','rooms','analytics','report_center',
    -- Financial
    'payments',
    -- General
    'privacy'
) AND ra.RoleName = 'Admin';

-- ── FACULTY ──────────────────────────────────────────────────────────────────
UPDATE ra SET ra.IsAllowed = 1
FROM sidebar_menu_role_accesses ra
JOIN sidebar_menu_items smi ON smi.Id = ra.SidebarMenuItemId
WHERE smi.[Key] IN (
    -- Faculty Related
    'timetable_admin','timetable_teacher','assignments','enter_attendance','enter_results',
    'gradebook','rubric_manage','lms_manage','course_material','discussion','announcements',
    -- Student Related
    'results','student_lifecycle',
    -- Academic Related
    'study_plan',
    -- Settings Related
    'analytics','report_center'
) AND ra.RoleName = 'Faculty';

-- ── STUDENT ──────────────────────────────────────────────────────────────────
UPDATE ra SET ra.IsAllowed = 1
FROM sidebar_menu_role_accesses ra
JOIN sidebar_menu_items smi ON smi.Id = ra.SidebarMenuItemId
WHERE smi.[Key] IN (
    -- Student Related
    'results','student_lifecycle',
    -- Academic Related
    'study_plan',
    -- Settings Related
    'analytics','report_center',
    -- Financial
    'payments'
) AND ra.RoleName = 'Student';

-- ── FINANCE ──────────────────────────────────────────────────────────────────
UPDATE ra SET ra.IsAllowed = 1
FROM sidebar_menu_role_accesses ra
JOIN sidebar_menu_items smi ON smi.Id = ra.SidebarMenuItemId
WHERE smi.[Key] IN (
    -- Settings Related
    'analytics','report_center',
    -- Financial
    'payments'
) AND ra.RoleName = 'Finance';

-- =============================================================================
-- Verification queries
-- =============================================================================
PRINT '--- SUPERADMIN visible menus ---';
SELECT smi.[Key], smi.Name
FROM sidebar_menu_items smi
JOIN sidebar_menu_role_accesses ra ON ra.SidebarMenuItemId = smi.Id
WHERE smi.IsDeleted = 0 AND smi.ParentId IS NULL AND ra.RoleName = 'SuperAdmin' AND ra.IsAllowed = 1
ORDER BY smi.DisplayOrder;

PRINT '--- ADMIN visible menus ---';
SELECT smi.[Key], smi.Name
FROM sidebar_menu_items smi
JOIN sidebar_menu_role_accesses ra ON ra.SidebarMenuItemId = smi.Id
WHERE smi.IsDeleted = 0 AND smi.ParentId IS NULL AND ra.RoleName = 'Admin' AND ra.IsAllowed = 1
ORDER BY smi.DisplayOrder;

PRINT '--- FACULTY visible menus ---';
SELECT smi.[Key], smi.Name
FROM sidebar_menu_items smi
JOIN sidebar_menu_role_accesses ra ON ra.SidebarMenuItemId = smi.Id
WHERE smi.IsDeleted = 0 AND smi.ParentId IS NULL AND ra.RoleName = 'Faculty' AND ra.IsAllowed = 1
ORDER BY smi.DisplayOrder;

PRINT '--- STUDENT visible menus ---';
SELECT smi.[Key], smi.Name
FROM sidebar_menu_items smi
JOIN sidebar_menu_role_accesses ra ON ra.SidebarMenuItemId = smi.Id
WHERE smi.IsDeleted = 0 AND smi.ParentId IS NULL AND ra.RoleName = 'Student' AND ra.IsAllowed = 1
ORDER BY smi.DisplayOrder;

PRINT '--- FINANCE visible menus ---';
SELECT smi.[Key], smi.Name
FROM sidebar_menu_items smi
JOIN sidebar_menu_role_accesses ra ON ra.SidebarMenuItemId = smi.Id
WHERE smi.IsDeleted = 0 AND smi.ParentId IS NULL AND ra.RoleName = 'Finance' AND ra.IsAllowed = 1
ORDER BY smi.DisplayOrder;
