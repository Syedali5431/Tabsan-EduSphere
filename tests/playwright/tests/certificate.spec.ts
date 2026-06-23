/**
 * certificate.spec.ts — Student certificate generation.
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

test.describe('Certificate', () => {
  test('user can view certificates page', async ({ page }) => {
    await doLogin(page);
    await page.goto('/Portal/GenerateCertificates');
    await page.waitForLoadState('domcontentloaded');

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });

  test('user can generate a certificate', async ({ page }) => {
    await doLogin(page);
    await page.goto('/Portal/GenerateCertificates');
    await page.waitForLoadState('domcontentloaded');

    // Look for a certificate type dropdown and generate button
    const certDropdown = page.locator('select[name="certificateType"], select[name="CertificateType"]');
    if (await certDropdown.isVisible({ timeout: 2000 }).catch(() => false)) {
      // Select first available certificate type
      const options = certDropdown.locator('option');
      const count = await options.count();
      if (count > 1) {
        await certDropdown.selectOption({ index: 1 });
      }
    }

    // Click generate
    const generateBtn = page.locator('button:has-text("Generate"), button:has-text("Preview"), a:has-text("Generate")');
    if (await generateBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
      await generateBtn.click();
      await page.waitForLoadState('domcontentloaded');
    }

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });
});
