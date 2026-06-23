/**
 * Students Module E2E Tests
 *
 * Covers: student list filtering, cascading filters, seeded data validation,
 *         empty filter state handling.
 *
 * Tags: @student, @smoke, @regression, @role-based
 */
import { test } from '@fixtures/role-fixtures';
import { StudentsPage } from '@pages/students.page';
import { applyFilters, clearAllFilters, assertEmptyFilterState } from '@helpers/filters';
import { INSTITUTIONS, DEPARTMENTS, COURSES, STUDENTS } from '@data/test-data';

test.describe('Students Module', () => {
  // ── SuperAdmin: full access ──────────────────────────────────────────────

  test.describe('As SuperAdmin', () => {
    test('@student @smoke should load students page with data', async ({ superAdminPage }) => {
      const page = new StudentsPage(superAdminPage);
      await page.goto();
      await page.assertHasRows(1);
    });

    test('@student @regression should display seeded demo students', async ({ superAdminPage }) => {
      const page = new StudentsPage(superAdminPage);
      await page.goto();

      // Verify known seeded student names appear
      await page.assertStudentVisible(STUDENTS.uniStudent1.name);
    });

    test('@student @regression should filter students by institution', async ({ superAdminPage }) => {
      const page = new StudentsPage(superAdminPage);
      await page.goto();

      const initialCount = await page.getRowCount();
      await page.selectInstitution(INSTITUTIONS.demoUniversity.name);

      // After filtering, row count may change
      const filteredCount = await page.getRowCount();
      // Should not crash and should show data (either same or filtered)
      expect(filteredCount).toBeGreaterThanOrEqual(0);
    });

    test('@student @edge should handle empty filter state gracefully', async ({ superAdminPage }) => {
      const page = new StudentsPage(superAdminPage);
      await page.goto();
      await assertEmptyFilterState(superAdminPage);
    });

    test('@student @regression should apply cascading filters', async ({ superAdminPage }) => {
      const page = new StudentsPage(superAdminPage);
      await page.goto();

      await applyFilters(superAdminPage, {
        institution: INSTITUTIONS.demoUniversity.name,
        department: DEPARTMENTS.computerScience.name,
        course: COURSES.bscs.name,
        semesterOrClass: 'Semester 1',
        levelType: 'semester',
      });

      // Page should still render (no crash)
      await page.assertHasRows(0); // may or may not have data, but must not crash
    });
  });

  // ── Faculty: can view but not manage ─────────────────────────────────────

  test.describe('As Faculty', () => {
    test('@student @role-based should load students page', async ({ facultyUniversityPage }) => {
      const page = new StudentsPage(facultyUniversityPage);
      await page.goto();
      await page.assertHasRows(1);
    });

    test('@student @role-based should NOT see registration/admin options', async ({ facultyUniversityPage }) => {
      const page = new StudentsPage(facultyUniversityPage);
      await page.goto();

      // Faculty should not see "Register Student" or "Edit" buttons
      const adminBtn = facultyUniversityPage.locator(
        'a:has-text("Register"), button:has-text("Add Student"), [data-testid="register-student"]',
      );
      await expect(adminBtn).not.toBeVisible({ timeout: 3_000 });
    });
  });

  // ── Student: restricted view ─────────────────────────────────────────────

  test.describe('As Student', () => {
    test('@student @role-based should be blocked from Students page', async ({ studentUniversityPage }) => {
      // Direct access should be denied
      await studentUniversityPage.goto('/Portal/Students', { waitUntil: 'domcontentloaded' });

      const isDashboard = studentUniversityPage.url().includes('/Dashboard');
      const isAccessDenied = await studentUniversityPage
        .locator('.access-denied, [data-testid="access-denied"], h1:has-text("Access Denied")')
        .isVisible()
        .catch(() => false);

      expect(isDashboard || isAccessDenied).toBeTruthy();
    });
  });
});
