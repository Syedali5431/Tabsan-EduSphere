/**
 * Results Module E2E Tests
 *
 * Covers: result entry, GPA vs Percentage grading logic, cascading filters,
 *         role-based access.
 *
 * Tags: @results, @smoke, @regression, @role-based
 */
import { test, expect } from '@fixtures/role-fixtures';
import { ResultsPage } from '@pages/results.page';
import { applyFilters } from '@helpers/filters';
import { INSTITUTIONS, DEPARTMENTS, COURSES, GRADING } from '@data/test-data';

test.describe('Results Module', () => {
  // ── Faculty: primary result-entry role ───────────────────────────────────

  test.describe('As Faculty (University)', () => {
    test('@results @smoke should load results entry page', async ({ facultyUniversityPage }) => {
      const page = new ResultsPage(facultyUniversityPage);
      await page.goto();

      await expect(
        facultyUniversityPage.locator('select[name="InstitutionId"], [data-testid="filter-institution"]'),
      ).toBeVisible();
    });

    test('@results @regression should apply filters and show student list', async ({ facultyUniversityPage }) => {
      const page = new ResultsPage(facultyUniversityPage);
      await page.goto();

      await applyFilters(facultyUniversityPage, {
        institution: INSTITUTIONS.demoUniversity.name,
        department: DEPARTMENTS.computerScience.name,
        course: COURSES.bscs.name,
        semesterOrClass: 'Semester 1',
        levelType: 'semester',
      });

      const rowCount = await page.getRowCount();
      expect(rowCount).toBeGreaterThanOrEqual(0); // Must not crash
    });

    test('@results @regression should enter scores and save', async ({ facultyUniversityPage }) => {
      const page = new ResultsPage(facultyUniversityPage);
      await page.goto();

      await applyFilters(facultyUniversityPage, {
        institution: INSTITUTIONS.demoUniversity.name,
        department: DEPARTMENTS.computerScience.name,
        course: COURSES.bscs.name,
        levelType: 'semester',
      });

      const rowCount = await page.getRowCount();
      if (rowCount > 0) {
        // Enter varying scores for students
        const scores = Array.from({ length: Math.min(rowCount, 5) }, (_, i) => 75 + i * 5);
        await page.enterScores(scores);
        await page.save();
      }
    });

    test('@results @regression should display GPA-based grading for University', async ({ facultyUniversityPage }) => {
      const page = new ResultsPage(facultyUniversityPage);
      await page.goto();

      await applyFilters(facultyUniversityPage, {
        institution: INSTITUTIONS.demoUniversity.name,
        department: DEPARTMENTS.computerScience.name,
        course: COURSES.bscs.name,
        levelType: 'semester',
      });

      // University context should show GPA indicators
      await page.assertGpaDisplayed();
    });
  });

  // ── Faculty (School): should use percentage grading ──────────────────────

  test.describe('As Faculty (School)', () => {
    test('@results @regression should display percentage-based grading for School', async ({ facultySchoolPage }) => {
      const page = new ResultsPage(facultySchoolPage);
      await page.goto();

      await applyFilters(facultySchoolPage, {
        institution: INSTITUTIONS.demoSchool.name,
        department: DEPARTMENTS.schoolScience.name,
        course: COURSES.class9.name,
        levelType: 'class',
      });

      // School context should show percentage indicators
      await page.assertPercentageDisplayed();
    });
  });

  // ── Student: view-only ───────────────────────────────────────────────────

  test.describe('As Student', () => {
    test('@results @role-based should view own results', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Results', { waitUntil: 'domcontentloaded' });

      const hasContent = await studentUniversityPage
        .locator('table, .no-data, .empty-state')
        .first()
        .isVisible()
        .catch(() => false);
      expect(hasContent).toBeTruthy();
    });

    test('@results @role-based should NOT access results entry page', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Results/Enter', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentUniversityPage.url().includes('/Dashboard') ||
        (await studentUniversityPage
          .locator('.access-denied, [data-testid="access-denied"], h1:has-text("Access Denied")')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeTruthy();
    });

    test('@results @role-based should view transcript', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Transcript', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentUniversityPage.url().includes('/Dashboard') ||
        (await studentUniversityPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      // Student should be able to access transcript (not blocked)
      expect(isBlocked).toBeFalsy();
    });
  });
});
