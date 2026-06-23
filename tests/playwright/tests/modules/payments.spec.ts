/**
 * Payments Module E2E Tests
 *
 * Covers: payment filtering, scoped records, role-based access.
 *
 * Tags: @payments, @smoke, @regression, @role-based
 */
import { test, expect } from '@fixtures/role-fixtures';
import { PaymentsPage } from '@pages/payments.page';
import { INSTITUTIONS } from '@data/test-data';

test.describe('Payments Module', () => {
  // ── Finance role: primary payments access ────────────────────────────────

  test.describe('As Finance', () => {
    test('@payments @smoke should load payments page', async ({ financePage }) => {
      const page = new PaymentsPage(financePage);
      await page.goto();
      await expect(page.table).toBeVisible({ timeout: 15_000 });
    });

    test('@payments @regression should filter payments by institution', async ({ financePage }) => {
      const page = new PaymentsPage(financePage);
      await page.goto();

      await page.filterByInstitution(INSTITUTIONS.demoUniversity.name);

      // Table should refresh without error
      await expect(page.table).toBeVisible({ timeout: 10_000 });
    });

    test('@payments @regression should scope payments to selected institution', async ({ financePage }) => {
      const page = new PaymentsPage(financePage);
      await page.goto();

      await page.filterByInstitution(INSTITUTIONS.demoUniversity.name);

      const rowCount = await page.getRowCount();
      if (rowCount > 0) {
        // All visible payments should belong to the filtered institution
        await page.assertAllPaymentsScoped(INSTITUTIONS.demoUniversity.name);
      }
    });
  });

  // ── SuperAdmin: full payment access ──────────────────────────────────────

  test.describe('As SuperAdmin', () => {
    test('@payments @role-based should access payments page', async ({ superAdminPage }) => {
      const page = new PaymentsPage(superAdminPage);
      await page.goto();
      await expect(page.table).toBeVisible({ timeout: 15_000 });
    });

    test('@payments @role-based should see all institution payments', async ({ superAdminPage }) => {
      const page = new PaymentsPage(superAdminPage);
      await page.goto();
      await page.assertHasRecords(0);
    });
  });

  // ── Admin: can view payments ─────────────────────────────────────────────

  test.describe('As Admin', () => {
    test('@payments @role-based should access payments page', async ({ adminUniversityPage }) => {
      const page = new PaymentsPage(adminUniversityPage);
      await page.goto();
      await expect(page.table).toBeVisible({ timeout: 15_000 });
    });
  });

  // ── Student: can view own payments ───────────────────────────────────────

  test.describe('As Student', () => {
    test('@payments @role-based should view own payments', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Payments/My', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentUniversityPage.url().includes('/Dashboard') ||
        (await studentUniversityPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeFalsy();
    });

    test('@payments @role-based should NOT access admin payments page', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Payments', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentUniversityPage.url().includes('/Dashboard') ||
        (await studentUniversityPage
          .locator('.access-denied, [data-testid="access-denied"], h1:has-text("Access Denied")')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeTruthy();
    });
  });

  // ── Faculty: should NOT access payments ──────────────────────────────────

  test.describe('As Faculty', () => {
    test('@payments @role-based should NOT access payments page', async ({ facultyUniversityPage }) => {
      await facultyUniversityPage.goto('/Portal/Payments', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        facultyUniversityPage.url().includes('/Dashboard') ||
        (await facultyUniversityPage
          .locator('.access-denied, [data-testid="access-denied"], h1:has-text("Access Denied")')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeTruthy();
    });
  });
});
