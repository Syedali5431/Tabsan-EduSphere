/**
 * Reports Module E2E Tests
 *
 * Covers: report generation, export formats (Excel, CSV, PDF).
 *
 * Tags: @reports, @smoke, @regression, @role-based
 */
import { test, expect } from '@fixtures/role-fixtures';
import { navigateMenu, assertPageHeading } from '@helpers/navigation';
import { authenticatedApiContext } from '@helpers/auth';
import {
  assertExcelExport,
  assertCsvExport,
  assertPdfExport,
} from '@helpers/api';
import { TEST_USERS, getPassword } from '@data/test-users';

const API_BASE = process.env.API_BASE_URL || 'http://localhost:5181';

test.describe('Reports Module', () => {
  // ── SuperAdmin: full report access ───────────────────────────────────────

  test.describe('As SuperAdmin', () => {
    test('@reports @smoke should load reports page', async ({ superAdminPage }) => {
      await navigateMenu(superAdminPage, 'Reports', '/Portal/Reports');
      await assertPageHeading(superAdminPage, 'Reports');
    });

    test('@reports @regression should access export reports page', async ({ superAdminPage }) => {
      await navigateMenu(superAdminPage, 'Export Reports', '/Portal/Reports/Export');
      // Page should load without error
      const isError = await superAdminPage
        .locator('.alert-danger, .error-page')
        .isVisible()
        .catch(() => false);
      expect(isError).toBeFalsy();
    });
  });

  // ── API-level export tests ───────────────────────────────────────────────

  test.describe('Export Endpoints (API)', () => {
    test('@reports @smoke should export attendance report as PDF', async () => {
      const user = TEST_USERS.superAdmin;
      const { context } = await authenticatedApiContext(API_BASE, {
        username: user.username,
        password: getPassword(user),
      });

      await assertPdfExport(context, '/api/reports/attendance/pdf');
      await context.dispose();
    });

    test('@reports @smoke should export attendance report as Excel', async () => {
      const user = TEST_USERS.superAdmin;
      const { context } = await authenticatedApiContext(API_BASE, {
        username: user.username,
        password: getPassword(user),
      });

      await assertExcelExport(context, '/api/reports/attendance/excel');
      await context.dispose();
    });

    test('@reports @smoke should export attendance report as CSV', async () => {
      const user = TEST_USERS.superAdmin;
      const { context } = await authenticatedApiContext(API_BASE, {
        username: user.username,
        password: getPassword(user),
      });

      await assertCsvExport(context, '/api/reports/attendance/csv');
      await context.dispose();
    });

    test('@reports @regression should export results report as PDF', async () => {
      const user = TEST_USERS.superAdmin;
      const { context } = await authenticatedApiContext(API_BASE, {
        username: user.username,
        password: getPassword(user),
      });

      await assertPdfExport(context, '/api/reports/results/pdf');
      await context.dispose();
    });

    test('@reports @regression should export results report as Excel', async () => {
      const user = TEST_USERS.superAdmin;
      const { context } = await authenticatedApiContext(API_BASE, {
        username: user.username,
        password: getPassword(user),
      });

      await assertExcelExport(context, '/api/reports/results/excel');
      await context.dispose();
    });

    test('@reports @regression should export students report as CSV', async () => {
      const user = TEST_USERS.superAdmin;
      const { context } = await authenticatedApiContext(API_BASE, {
        username: user.username,
        password: getPassword(user),
      });

      await assertCsvExport(context, '/api/reports/students/csv');
      await context.dispose();
    });
  });

  // ── Faculty: can view reports ────────────────────────────────────────────

  test.describe('As Faculty', () => {
    test('@reports @role-based should access reports page', async ({ facultyUniversityPage }) => {
      await facultyUniversityPage.goto('/Portal/Reports', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        facultyUniversityPage.url().includes('/Dashboard') ||
        (await facultyUniversityPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeFalsy();
    });
  });

  // ── Student: blocked from reports ────────────────────────────────────────

  test.describe('As Student', () => {
    test('@reports @role-based should NOT access reports page', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Reports', { waitUntil: 'domcontentloaded' });

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
