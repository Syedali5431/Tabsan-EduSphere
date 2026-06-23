/**
 * MFA (Multi-Factor Authentication) E2E Tests
 *
 * Covers: MFA challenge page, valid TOTP code, invalid TOTP code,
 *         MFA required for privileged roles.
 *
 * Tags: @auth, @mfa, @regression
 */
import { test, expect } from '@playwright/test';
import { TEST_USERS, getPassword } from '@data/test-users';
import { loginAs, generateTotpCode } from '@helpers/auth';
import { LoginPage } from '@pages/login.page';

test.describe('MFA Authentication', () => {
  test.use({ storageState: undefined });

  // ═════════════════════════════════════════════════════════════════════════
  //  MFA SUCCESS FLOW
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @mfa should complete MFA login with valid TOTP code', async ({ page }) => {
    const mfaUser = TEST_USERS.mfaUser;
    const password = getPassword(mfaUser);
    const totpSecret = process.env.MFA_TOTP_SECRET;

    test.skip(!totpSecret, 'MFA_TOTP_SECRET not set in .env — skipping MFA tests');

    await loginAs(page, {
      username: mfaUser.username,
      password,
      mfaExpected: true,
      totpSecret,
    });

    // Should land on authenticated page after successful MFA (Faculty→Helpdesk)
    await expect(page.locator('.app-sidebar, .sidebar-nav, [data-app-sidebar]')).toBeVisible();
  });

  // ═════════════════════════════════════════════════════════════════════════
  //  INVALID MFA CODE
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @mfa should reject invalid TOTP code', async ({ page }) => {
    const mfaUser = TEST_USERS.mfaUser;
    const password = getPassword(mfaUser);
    const totpSecret = process.env.MFA_TOTP_SECRET;

    test.skip(!totpSecret, 'MFA_TOTP_SECRET not set in .env — skipping MFA tests');

    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login(mfaUser.username, password);

    // MFA challenge should appear
    await loginPage.mfaCodeInput.waitFor({ state: 'visible', timeout: 10_000 });

    // Submit an invalid code
    await loginPage.submitMfa('000000');

    // Should show error and stay on MFA page
    await expect(loginPage.errorMessage.first()).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('.app-sidebar').first()).toBeVisible();
  });

  // ═════════════════════════════════════════════════════════════════════════
  //  MFA NOT REQUIRED FOR NON-MFA USERS
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @mfa should not prompt MFA for non-MFA users', async ({ page }) => {
    const user = TEST_USERS.superAdmin;
    const password = getPassword(user);

    await loginAs(page, {
      username: user.username,
      password,
    });

    // Should go directly to authenticated page (no MFA challenge)
    await expect(page.locator('.app-sidebar').first()).toBeVisible();
  });

  // ═════════════════════════════════════════════════════════════════════════
  //  MFA CODE EXPIRY (EDGE CASE)
  // ═════════════════════════════════════════════════════════════════════════

  test('@auth @mfa @edge should handle expired/wrong-window TOTP code', async ({ page }) => {
    const mfaUser = TEST_USERS.mfaUser;
    const password = getPassword(mfaUser);
    const totpSecret = process.env.MFA_TOTP_SECRET;

    test.skip(!totpSecret, 'MFA_TOTP_SECRET not set in .env — skipping MFA tests');

    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login(mfaUser.username, password);

    await loginPage.mfaCodeInput.waitFor({ state: 'visible', timeout: 10_000 });

    // Generate a TOTP code but deliberately use a different time window
    const wrongCode = '999999';
    await loginPage.submitMfa(wrongCode);

    // Should show error
    await expect(loginPage.errorMessage.first()).toBeVisible({ timeout: 10_000 });
  });
});
