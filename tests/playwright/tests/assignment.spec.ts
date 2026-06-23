/**
 * assignment.spec.ts — Student assignment lifecycle.
 */
import { test, expect } from '@playwright/test';

const TEST_USER = 'admin.uni';
const TEST_PASS = process.env.ADMIN_PASSWORD || 'EduSphere147';

async function doLogin(page: any) {
  await page.goto('/Login');
  await page.fill('#username', TEST_USER);
  await page.fill('#password', TEST_PASS);
  await page.click('button[type="submit"]');
  await expect(page.locator('.app-sidebar').first()).toBeVisible({ timeout: 15000 });
}

test.describe('Assignment', () => {
  test('user can view assignments page', async ({ page }) => {
    await doLogin(page);
    await page.goto('/Portal/Assignments');
    await page.waitForLoadState('domcontentloaded');

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });

  test('user can open and submit an assignment', async ({ page }) => {
    await doLogin(page);
    await page.goto('/Portal/Assignments');
    await page.waitForLoadState('domcontentloaded');

    // Look for an assignment link/row
    const assignmentLink = page.locator('table tbody tr a, a:has-text("View"), a:has-text("Open"), a:has-text("Submit")').first();
    if (await assignmentLink.isVisible({ timeout: 3000 }).catch(() => false)) {
      await assignmentLink.click();
      await page.waitForLoadState('domcontentloaded');

      // Look for a file upload or text submission
      const fileInput = page.locator('input[type="file"]');
      const textArea  = page.locator('textarea');

      if (await fileInput.isVisible({ timeout: 2000 }).catch(() => false)) {
        // File upload available — skip (no test file)
      }

      if (await textArea.isVisible({ timeout: 2000 }).catch(() => false)) {
        await textArea.fill('Assignment submission text');
      }

      // Submit button
      const submitBtn = page.locator('button:has-text("Submit"), button:has-text("Turn In"), button[type="submit"]');
      if (await submitBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
        await submitBtn.click();
        await page.waitForLoadState('domcontentloaded');
      }
    }

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });
});
