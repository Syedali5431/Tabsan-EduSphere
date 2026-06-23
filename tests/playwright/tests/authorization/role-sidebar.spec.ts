/**
 * Authorization / Role-Based Sidebar Tests
 *
 * For each role, verifies that:
 *   - Expected sidebar menu items are visible
 *   - Restricted menu items are hidden
 *   - Direct URL access to unauthorized pages is blocked
 *
 * Tags: @authorization, @role-based, @regression
 */
import { test, expect } from '@fixtures/role-fixtures';
import { assertMenuVisible, assertMenuHidden, assertDirectUrlAccess } from '@helpers/navigation';

// ── Critical menu items to verify per role (subset for speed) ──────────────

const CRITICAL_CHECKS: Record<string, { visible: string[]; hidden: string[]; blockedUrls: string[] }> = {
  SuperAdmin: {
    visible: ['Dashboard', 'System Settings', 'User Management', 'Students', 'Reports', 'Payments'],
    hidden: [],
    blockedUrls: [],
  },
  Admin: {
    visible: ['Dashboard', 'Students', 'Faculty Management', 'Courses', 'Attendance', 'Results', 'Reports'],
    hidden: ['System Settings', 'License Management', 'Audit Logs'],
    blockedUrls: ['/Portal/Settings', '/Portal/License', '/Portal/AuditLogs'],
  },
  Faculty: {
    visible: ['Dashboard', 'Students', 'Enter Attendance', 'Enter Results', 'View Attendance', 'View Results'],
    hidden: ['System Settings', 'User Management', 'License Management', 'Faculty Management', 'Payments', 'Generate Invoice'],
    blockedUrls: ['/Portal/Settings', '/Portal/Users', '/Portal/Payments', '/Portal/Faculty'],
  },
  Student: {
    visible: ['Dashboard', 'View Attendance', 'View Results', 'Transcript', 'Profile', 'My Payments'],
    hidden: ['System Settings', 'User Management', 'Enter Attendance', 'Enter Results', 'Payments', 'Students'],
    blockedUrls: ['/Portal/Settings', '/Portal/Students', '/Portal/Attendance/Enter', '/Portal/Results/Enter'],
  },
  Finance: {
    visible: ['Dashboard', 'Payments', 'Generate Invoice', 'Profile'],
    hidden: ['System Settings', 'Students', 'Enter Attendance', 'Enter Results', 'Faculty Management'],
    blockedUrls: ['/Portal/Settings', '/Portal/Students', '/Portal/Attendance/Enter', '/Portal/Results/Enter'],
  },
};

// ═══════════════════════════════════════════════════════════════════════════
//  Explicit per-role test groups (no dynamic computed keys)
// ═══════════════════════════════════════════════════════════════════════════

test.describe('Authorization – Sidebar Menu Visibility', () => {
  // ── SuperAdmin ─────────────────────────────────────────────────────────
  test.describe('SuperAdmin Role', () => {
    for (const item of CRITICAL_CHECKS.SuperAdmin.visible) {
      test(`@role-based @regression should show "${item}" in sidebar for SuperAdmin`, async ({ superAdminPage }) => {
        await assertMenuVisible(superAdminPage, item);
      });
    }
  });

  // ── Admin ──────────────────────────────────────────────────────────────
  test.describe('Admin Role', () => {
    for (const item of CRITICAL_CHECKS.Admin.visible) {
      test(`@role-based @regression should show "${item}" in sidebar for Admin`, async ({ adminUniversityPage }) => {
        await assertMenuVisible(adminUniversityPage, item);
      });
    }
    for (const item of CRITICAL_CHECKS.Admin.hidden) {
      test(`@role-based @regression should hide "${item}" from sidebar for Admin`, async ({ adminUniversityPage }) => {
        await assertMenuHidden(adminUniversityPage, item);
      });
    }
    for (const url of CRITICAL_CHECKS.Admin.blockedUrls) {
      test(`@role-based @regression should block direct access to "${url}" for Admin`, async ({ adminUniversityPage }) => {
        await assertDirectUrlAccess(adminUniversityPage, url, true);
      });
    }
  });

  // ── Faculty ────────────────────────────────────────────────────────────
  test.describe('Faculty Role', () => {
    for (const item of CRITICAL_CHECKS.Faculty.visible) {
      test(`@role-based @regression should show "${item}" in sidebar for Faculty`, async ({ facultyUniversityPage }) => {
        await assertMenuVisible(facultyUniversityPage, item);
      });
    }
    for (const item of CRITICAL_CHECKS.Faculty.hidden) {
      test(`@role-based @regression should hide "${item}" from sidebar for Faculty`, async ({ facultyUniversityPage }) => {
        await assertMenuHidden(facultyUniversityPage, item);
      });
    }
    for (const url of CRITICAL_CHECKS.Faculty.blockedUrls) {
      test(`@role-based @regression should block direct access to "${url}" for Faculty`, async ({ facultyUniversityPage }) => {
        await assertDirectUrlAccess(facultyUniversityPage, url, true);
      });
    }
  });

  // ── Student ────────────────────────────────────────────────────────────
  test.describe('Student Role', () => {
    for (const item of CRITICAL_CHECKS.Student.visible) {
      test(`@role-based @regression should show "${item}" in sidebar for Student`, async ({ studentUniversityPage }) => {
        await assertMenuVisible(studentUniversityPage, item);
      });
    }
    for (const item of CRITICAL_CHECKS.Student.hidden) {
      test(`@role-based @regression should hide "${item}" from sidebar for Student`, async ({ studentUniversityPage }) => {
        await assertMenuHidden(studentUniversityPage, item);
      });
    }
    for (const url of CRITICAL_CHECKS.Student.blockedUrls) {
      test(`@role-based @regression should block direct access to "${url}" for Student`, async ({ studentUniversityPage }) => {
        await assertDirectUrlAccess(studentUniversityPage, url, true);
      });
    }
  });

  // ── Finance ────────────────────────────────────────────────────────────
  test.describe('Finance Role', () => {
    for (const item of CRITICAL_CHECKS.Finance.visible) {
      test(`@role-based @regression should show "${item}" in sidebar for Finance`, async ({ financePage }) => {
        await assertMenuVisible(financePage, item);
      });
    }
    for (const item of CRITICAL_CHECKS.Finance.hidden) {
      test(`@role-based @regression should hide "${item}" from sidebar for Finance`, async ({ financePage }) => {
        await assertMenuHidden(financePage, item);
      });
    }
    for (const url of CRITICAL_CHECKS.Finance.blockedUrls) {
      test(`@role-based @regression should block direct access to "${url}" for Finance`, async ({ financePage }) => {
        await assertDirectUrlAccess(financePage, url, true);
      });
    }
  });
});

test.describe('Authorization – Direct URL Access Control', () => {
  test('@role-based @smoke SuperAdmin should access all admin URLs', async ({ superAdminPage }) => {
    await assertDirectUrlAccess(superAdminPage, '/Portal/Settings', false);
    await assertDirectUrlAccess(superAdminPage, '/Portal/Users', false);
    await assertDirectUrlAccess(superAdminPage, '/Portal/Students', false);
  });

  test('@role-based @regression Student should be blocked from admin URLs', async ({ studentUniversityPage }) => {
    await assertDirectUrlAccess(studentUniversityPage, '/Portal/Settings', true);
    await assertDirectUrlAccess(studentUniversityPage, '/Portal/Users', true);
    await assertDirectUrlAccess(studentUniversityPage, '/Portal/Faculty', true);
  });

  test('@role-based @regression Faculty should be blocked from Settings', async ({ facultyUniversityPage }) => {
    await assertDirectUrlAccess(facultyUniversityPage, '/Portal/Settings', true);
    await assertDirectUrlAccess(facultyUniversityPage, '/Portal/License', true);
  });
});
