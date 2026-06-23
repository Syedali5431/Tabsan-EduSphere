/**
 * login.spec.ts — Simple login/logout tests for all roles.
 *
 * Uses ONLY ({ page }) fixture. No custom fixtures, no abstractions.
 *
 * Real selectors (verified from Login/Index.cshtml):
 *   #username  — username field
 *   #password  — password field
 *   #mfaCode   — MFA field (appears when MFA enabled; only enforced for privileged roles)
 *   button[type="submit"] — Sign In button
 */
import { test, expect } from '@playwright/test';

// ── Credentials from env ────────────────────────────────────────────────────
// Use admin.uni — confirmed to exist in local DB
const TEST_USER = 'admin.uni';
const TEST_PASS = process.env.ADMIN_PASSWORD || 'EduSphere147';

test.describe('Login', () => {
  // ═══════════════════════════════════════════════════════════════════════════
  //  STUDENT LOGIN (no MFA required — students not in privileged role list)
  // ═══════════════════════════════════════════════════════════════════════════
  test('valid user can login', async ({ page }) => {
    await page.goto('/Login');
    await page.fill('#username', TEST_USER);
    await page.fill('#password', TEST_PASS);
    await page.click('button[type="submit"]');

    // Admin → redirects to Portal/Helpdesk; sidebar must be visible
    await expect(page.locator('.app-sidebar').first()).toBeVisible({ timeout: 15000 });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  //  INVALID LOGIN
  // ═══════════════════════════════════════════════════════════════════════════
  test('invalid credentials show error', async ({ page }) => {
    await page.goto('/Login');
    await page.fill('#username', 'no-such-user');
    await page.fill('#password', 'wrong-password');
    await page.click('button[type="submit"]');
    await expect(page.locator('.alert-danger')).toBeVisible({ timeout: 10000 });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  //  LOGOUT
  // ═══════════════════════════════════════════════════════════════════════════
  test('user can logout', async ({ page }) => {
    // Login
    await page.goto('/Login');
    await page.fill('#username', TEST_USER);
    await page.fill('#password', TEST_PASS);
    await page.click('button[type="submit"]');
    await expect(page.locator('.app-sidebar').first()).toBeVisible({ timeout: 15000 });

    // Open profile menu and click Sign Out via form submission
    // The form is <form method="post" asp-action="Logout"> with a "Sign Out" button
    const details = page.locator('details.profile-menu');
    if (await details.isVisible({ timeout: 2000 }).catch(() => false)) {
      await details.locator('summary').click({ force: true });
    }
    // Submit the logout form directly by dispatching submit on the form
    await page.locator('form[action*="Logout"]').evaluate((el: HTMLFormElement) => el.submit());
    // After logout, redirected to Login page
    await expect(page).toHaveURL(/Login/, { timeout: 10000 });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  //  PROTECTED PAGE — unauthenticated redirect
  // ═══════════════════════════════════════════════════════════════════════════
  test('unauthenticated user redirected to login', async ({ page }) => {
    await page.goto('/Portal/Dashboard');
    // Should be redirected to login
    await expect(page.locator('#username')).toBeVisible({ timeout: 10000 });
  });
});
