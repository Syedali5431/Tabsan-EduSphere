/**
 * Authentication E2E Tests
 *
 * Covers: login success, invalid credentials, session timeout, logout.
 *
 * Tags: @auth, @smoke, @regression
 */
import { test, expect } from '@playwright/test';
import { TEST_USERS, getPassword } from '@data/test-users';
import { loginAs, logout, assertInvalidLogin, assertRedirectToLogin, assertSessionTimeout } from '@helpers/auth';

// ── Helpers: lazy password resolution (NOT at module level) ────────────────

function getValidPassword(): string {
  return getPassword(TEST_USERS.superAdmin);
}

function getValidUsername(): string {
  return TEST_USERS.superAdmin.username;
}

test.describe('Authentication', () => {
  // ── Clean state for each test ────────────────────────────────────────────
  test.use({ storageState: undefined }); // No saved session

  // ═════════════════════════════════════════════════════════════════════════
  //  LOGIN SUCCESS
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @smoke should login successfully with valid credentials', async ({ page }) => {
    await loginAs(page, {
      username: getValidUsername(),
      password: getValidPassword(),
    });

    // Verify dashboard loaded
    await expect(page.locator('h1, .page-title, [data-testid="page-heading"]')).toBeVisible();
  });

  // ═════════════════════════════════════════════════════════════════════════
  //  INVALID LOGIN
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @smoke should show error for invalid credentials', async ({ page }) => {
    await assertInvalidLogin(page, 'invalid-user', 'wrong-password-12345');
  });

  test('@auth should show error for empty username', async ({ page }) => {
    await page.goto('/Login', { waitUntil: 'domcontentloaded' });
    await page.fill('input[name="password"]', 'somepass');
    await page.click('button[type="submit"]');

    // Form validation or server error should appear
    const error = page.locator('.alert-danger');
    await expect(error.first()).toBeVisible({ timeout: 10_000 });
  });

  test('@auth should show error for empty password', async ({ page }) => {
    await page.goto('/Login', { waitUntil: 'domcontentloaded' });
    await page.fill('input[name="username"]', getValidUsername());
    await page.click('button[type="submit"]');

    const error = page.locator('.alert-danger');
    await expect(error.first()).toBeVisible({ timeout: 10_000 });
  });

  // ═════════════════════════════════════════════════════════════════════════
  //  PROTECTED ROUTE REDIRECT
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @regression should redirect to login when accessing protected page unauthenticated', async ({ page }) => {
    await assertRedirectToLogin(page, '/Portal/Students');
  });

  test('@auth @regression should redirect to login when accessing dashboard unauthenticated', async ({ page }) => {
    await assertRedirectToLogin(page, '/Portal/Dashboard');
  });

  // ═════════════════════════════════════════════════════════════════════════
  //  LOGOUT
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @regression should logout successfully and redirect to login', async ({ page }) => {
    await loginAs(page, {
      username: getValidUsername(),
      password: getValidPassword(),
    });

    await logout(page);

    // Verify we're on the login page
    await expect(page.locator('input[name="username"]')).toBeVisible();
  });

  // ═════════════════════════════════════════════════════════════════════════
  //  SESSION TIMEOUT
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @edge should timeout session after idle period', async ({ page }) => {
    test.skip(
      !process.env.CI && !process.env.RUN_SLOW_TESTS,
      'Session timeout test takes 5+ minutes. Set RUN_SLOW_TESTS=true to enable.',
    );

    await loginAs(page, {
      username: getValidUsername(),
      password: getValidPassword(),
    });

    // Wait for session to expire (5 min idle + buffer)
    await assertSessionTimeout(page);
  });
});
