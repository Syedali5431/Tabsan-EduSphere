/**
 * attendance.spec.ts — Student attendance viewing.
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

test.describe('Attendance', () => {
  test('attendance page loads without error', async ({ page }) => {
    await doLogin(page);
    const response = await page.goto('/Portal/Attendance');
    // Page may redirect if filters not set — that's OK
    expect(response?.status()).toBeLessThan(500);
  });
});
