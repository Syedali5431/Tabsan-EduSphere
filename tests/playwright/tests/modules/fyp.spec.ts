/**
 * FYP (Final Year Project) Module E2E Tests
 *
 * University-only module. Covers: project submission, result entry,
 * transcript integration.
 *
 * Tags: @fyp, @regression, @role-based, @university-only
 */
import { test, expect } from '@fixtures/role-fixtures';
import { navigateMenu, assertPageHeading } from '@helpers/navigation';

test.describe('FYP Module (University Only)', () => {
  // ── SuperAdmin ───────────────────────────────────────────────────────────

  test.describe('As SuperAdmin', () => {
    test('@fyp @regression should access FYP Management', async ({ superAdminPage }) => {
      await navigateMenu(superAdminPage, 'FYP', '/Portal/FYP');
      await assertPageHeading(superAdminPage, 'FYP');
    });

    test('@fyp @regression should load FYP page without error', async ({ superAdminPage }) => {
      await superAdminPage.goto('/Portal/FYP', { waitUntil: 'domcontentloaded' });

      // Should load (no access denied)
      const isBlocked =
        superAdminPage.url().includes('/Dashboard') ||
        (await superAdminPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeFalsy();
    });
  });

  // ── Faculty (University): can manage FYP ─────────────────────────────────

  test.describe('As Faculty (University)', () => {
    test('@fyp @role-based should access FYP Management', async ({ facultyUniversityPage }) => {
      await facultyUniversityPage.goto('/Portal/FYP', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        facultyUniversityPage.url().includes('/Dashboard') ||
        (await facultyUniversityPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeFalsy();
    });
  });

  // ── Student (University): can submit FYP ─────────────────────────────────

  test.describe('As Student (University)', () => {
    test('@fyp @role-based should access FYP Submission', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/FYP/Submit', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentUniversityPage.url().includes('/Dashboard') ||
        (await studentUniversityPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeFalsy();
    });
  });

  // ── School Faculty: FYP should NOT be available ──────────────────────────

  test.describe('As Faculty (School)', () => {
    test('@fyp @role-based should NOT access FYP (School institution)', async ({ facultySchoolPage }) => {
      await facultySchoolPage.goto('/Portal/FYP', { waitUntil: 'domcontentloaded' });

      // Should be blocked — FYP is University-only
      const isBlocked =
        facultySchoolPage.url().includes('/Dashboard') ||
        (await facultySchoolPage
          .locator('.access-denied, [data-testid="access-denied"], h1:has-text("Access Denied")')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeTruthy();
    });
  });

  // ── School Student: FYP should NOT be available ──────────────────────────

  test.describe('As School Student', () => {
    test('@fyp @role-based should NOT access FYP Submission (School)', async ({ studentSchoolPage }) => {
      await studentSchoolPage.goto('/Portal/FYP/Submit', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentSchoolPage.url().includes('/Dashboard') ||
        (await studentSchoolPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeTruthy();
    });
  });
});
