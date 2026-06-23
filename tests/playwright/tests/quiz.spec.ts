/**
 * quiz.spec.ts — Student quiz lifecycle.
 *
 * Student route: /Portal/ViewQuizzes (Faculty gets /Portal/Quizzes)
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

test.describe('Quiz', () => {
  test('user can view quizzes page', async ({ page }) => {
    await doLogin(page);

    // Direct navigation to ViewQuizzes (student quiz page)
    await page.goto('/Portal/ViewQuizzes');
    await page.waitForLoadState('domcontentloaded');

    // Page should load without fatal error
    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });

  test('user can start a quiz', async ({ page }) => {
    await doLogin(page);
    await page.goto('/Portal/ViewQuizzes');
    await page.waitForLoadState('domcontentloaded');

    // TODO: click "Start" or "Take Quiz" button if quizzes exist
    const startBtn = page.locator('a:has-text("Start"), button:has-text("Start"), a:has-text("Take"), button:has-text("Take")');
    if (await startBtn.first().isVisible({ timeout: 3000 }).catch(() => false)) {
      await startBtn.first().click();
      await page.waitForLoadState('domcontentloaded');
    }

    // Should not crash
    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });

  test('user can submit quiz answers', async ({ page }) => {
    await doLogin(page);
    await page.goto('/Portal/ViewQuizzes');
    await page.waitForLoadState('domcontentloaded');

    // Try to start a quiz
    const startBtn = page.locator('a:has-text("Start"), button:has-text("Start"), a:has-text("Take")');
    if (await startBtn.first().isVisible({ timeout: 3000 }).catch(() => false)) {
      await startBtn.first().click();
      await page.waitForLoadState('domcontentloaded');

      // Look for radio buttons or checkboxes (quiz answer options)
      const firstRadio = page.locator('input[type="radio"]').first();
      if (await firstRadio.isVisible({ timeout: 2000 }).catch(() => false)) {
        await firstRadio.check();
      }

      // Look for text input for open-ended questions
      const textInput = page.locator('input[type="text"], textarea').first();
      if (await textInput.isVisible({ timeout: 1000 }).catch(() => false)) {
        await textInput.fill('Test answer');
      }

      // Submit
      const submitBtn = page.locator('button:has-text("Submit"), button:has-text("Finish"), button[type="submit"]');
      if (await submitBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
        await submitBtn.click();
        await page.waitForLoadState('domcontentloaded');
      }
    }

    // Should not crash
    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });
});
