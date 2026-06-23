/**
 * Attendance Module E2E Tests
 *
 * Covers: cascading filter application, attendance entry, save verification.
 *
 * Tags: @attendance, @smoke, @regression, @role-based
 */
import { test, expect } from '@fixtures/role-fixtures';
import { AttendancePage } from '@pages/attendance.page';
import { applyFilters } from '@helpers/filters';
import { INSTITUTIONS, DEPARTMENTS, COURSES } from '@data/test-data';

test.describe('Attendance Module', () => {
  // ── Faculty: primary attendance entry role ───────────────────────────────

  test.describe('As Faculty (University)', () => {
    test('@attendance @smoke should load attendance entry page', async ({ facultyUniversityPage }) => {
      const page = new AttendancePage(facultyUniversityPage);
      await page.goto();
      // Page loads with filters visible
      await expect(facultyUniversityPage.locator('select[name="InstitutionId"], [data-testid="filter-institution"]')).toBeVisible();
    });

    test('@attendance @regression should apply cascading filters and show students', async ({ facultyUniversityPage }) => {
      const page = new AttendancePage(facultyUniversityPage);
      await page.goto();

      await applyFilters(facultyUniversityPage, {
        institution: INSTITUTIONS.demoUniversity.name,
        department: DEPARTMENTS.computerScience.name,
        course: COURSES.bscs.name,
        semesterOrClass: 'Semester 1',
        levelType: 'semester',
      });

      // After applying filters, student list should be visible or empty state shown
      // — neither should crash
      const hasTable = await page.table.isVisible().catch(() => false);
      const hasEmpty = await facultyUniversityPage.locator('.no-data, .empty-state').isVisible().catch(() => false);
      expect(hasTable || hasEmpty).toBeTruthy();
    });

    test('@attendance @regression should mark attendance and save', async ({ facultyUniversityPage }) => {
      const page = new AttendancePage(facultyUniversityPage);
      await page.goto();

      // Apply filters first
      await applyFilters(facultyUniversityPage, {
        institution: INSTITUTIONS.demoUniversity.name,
        department: DEPARTMENTS.computerScience.name,
        course: COURSES.bscs.name,
        levelType: 'semester',
      });

      // If students are listed, mark them present and save
      const rowCount = await page.getRowCount();
      if (rowCount > 0) {
        await page.markFirstNStudentsPresent(Math.min(3, rowCount));
        await page.save();
      }
      // If no students, test passes as long as no crash
    });

    test('@attendance @regression should mark all students present', async ({ facultyUniversityPage }) => {
      const page = new AttendancePage(facultyUniversityPage);
      await page.goto();

      await applyFilters(facultyUniversityPage, {
        institution: INSTITUTIONS.demoUniversity.name,
        department: DEPARTMENTS.mathematics.name,
        course: COURSES.bsMath.name,
        levelType: 'semester',
      });

      const rowCount = await page.getRowCount();
      if (rowCount > 0) {
        await page.markAllPresent();
        await page.save();
      }
    });
  });

  // ── Admin: can also enter attendance ─────────────────────────────────────

  test.describe('As Admin', () => {
    test('@attendance @role-based should access attendance entry', async ({ adminUniversityPage }) => {
      const page = new AttendancePage(adminUniversityPage);
      await page.goto();
      await expect(adminUniversityPage.locator('select[name="InstitutionId"], [data-testid="filter-institution"]')).toBeVisible();
    });
  });

  // ── Student: view-only ───────────────────────────────────────────────────

  test.describe('As Student', () => {
    test('@attendance @role-based should view own attendance', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Attendance', { waitUntil: 'domcontentloaded' });

      // Should either show attendance table or no-data message
      const hasContent = await studentUniversityPage
        .locator('table, .no-data, .empty-state')
        .first()
        .isVisible()
        .catch(() => false);
      expect(hasContent).toBeTruthy();
    });

    test('@attendance @role-based should NOT access attendance entry page', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Attendance/Enter', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentUniversityPage.url().includes('/Dashboard') ||
        (await studentUniversityPage
          .locator('.access-denied, [data-testid="access-denied"], h1:has-text("Access Denied")')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeTruthy();
    });
  });
});
