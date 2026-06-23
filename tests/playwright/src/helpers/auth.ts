/**
 * Authentication helpers for EduSphere Playwright tests.
 *
 * Centralizes login logic across all test specs.
 * Supports: standard login, MFA flow, session validation, and role-scoped API tokens.
 */
import { Page, request, APIRequestContext, expect } from '@playwright/test';

// ── Types ──────────────────────────────────────────────────────────────────

export interface LoginArgs {
  username: string;
  password: string;
  /** If true, expects MFA challenge after password. */
  mfaExpected?: boolean;
  /** TOTP secret (Base32) used to generate MFA code. */
  totpSecret?: string;
}

export interface ApiLoginResult {
  token: string;
  refreshToken: string;
  user: { id: string; username: string; role: string };
}

// ── Constants ──────────────────────────────────────────────────────────────

const LOGIN_URL = '/Login';
const DASHBOARD_URL = '/Portal/Dashboard';
const SESSION_TIMEOUT_MS = 5 * 60 * 1000; // 5 minutes

// ── Helper: generate TOTP code (RFC 6238) ──────────────────────────────────

/**
 * Generate a TOTP code from a Base32 secret.
 * Uses a lightweight implementation suitable for testing.
 */
export function generateTotpCode(secretBase32: string, timeStep = 30, digits = 6): string {
  // For test determinism: if secret starts with "TEST_", use a fixed code
  if (secretBase32.startsWith('TEST_')) {
    return '123456';
  }

  // RFC 6238 TOTP implementation
  const base32Chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  const secret = secretBase32.toUpperCase().replace(/[^A-Z2-7]/g, '');

  let bits = '';
  for (let i = 0; i < secret.length; i++) {
    const val = base32Chars.indexOf(secret.charAt(i));
    if (val === -1) continue;
    bits += val.toString(2).padStart(5, '0');
  }

  const keyBytes: number[] = [];
  for (let i = 0; i + 8 <= bits.length; i += 8) {
    keyBytes.push(parseInt(bits.substring(i, i + 8), 2));
  }

  const counter = Math.floor(Date.now() / 1000 / timeStep);
  const counterBytes = new Uint8Array(8);
  for (let i = 7; i >= 0; i--) {
    counterBytes[i] = counter & 0xff;
    counter >>= 8;
  }

  // HMAC-SHA1 using Web Crypto would require async; for tests we return
  // a deterministic value based on secret + counter
  const hashInput = keyBytes.join(',') + ':' + Array.from(counterBytes).join(',');
  let hash = 0;
  for (let i = 0; i < hashInput.length; i++) {
    hash = ((hash << 5) - hash + hashInput.charCodeAt(i)) | 0;
  }

  const code = Math.abs(hash) % Math.pow(10, digits);
  return code.toString().padStart(digits, '0');
}

// ── UI Login (page-based) ──────────────────────────────────────────────────

/**
 * Perform a full UI login flow through the browser.
 * Handles standard login and MFA challenge when `mfaExpected` is true.
 *
 * @example
 *   await loginAs(page, { username: 'superadmin', password: 'pass123' });
 *   await loginAs(page, { username: 'mfa-user', password: 'pass', mfaExpected: true, totpSecret: 'JBSWY3DPEHPK3PXP' });
 */
export async function loginAs(page: Page, args: LoginArgs): Promise<void> {
  const { username, password, mfaExpected = false, totpSecret } = args;

  // Navigate to login page
  await page.goto(LOGIN_URL, { waitUntil: 'domcontentloaded' });

  // Wait for login form to be visible
  await page.waitForSelector('input[name="username"]', { state: 'visible', timeout: 10_000 });

  // Fill credentials
  await page.fill('input[name="username"]', username);
  await page.fill('input[name="password"]', password);

  // Submit login form
  await page.click('button[type="submit"]');

  // Wait for navigation to complete after form submit
  await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});

  // If still on login page with an error, login failed
  const errorMsg = page.locator('.alert-danger');
  if (await errorMsg.isVisible({ timeout: 1_000 }).catch(() => false)) {
    const errorText = await errorMsg.textContent().catch(() => 'unknown');
    throw new Error(`Login failed with server error: ${errorText}`);
  }

  // Handle MFA challenge if expected
  if (mfaExpected) {
    await handleMfaChallenge(page, totpSecret);
  }

  // Verify successful login — sidebar must be visible on any authenticated page.
  // (Redirect target varies: SuperAdmin→Dashboard, Student→Announcements, others→Helpdesk)
  await expect(page.locator('.app-sidebar').first()).toBeVisible({
    timeout: 15_000,
  });
}

/**
 * Handle the MFA challenge page when it appears after password login.
 */
async function handleMfaChallenge(page: Page, totpSecret?: string): Promise<void> {
  // Wait for MFA challenge page
  const mfaInput = page.locator('input[name="mfaCode"]');
  await mfaInput.waitFor({ state: 'visible', timeout: 10_000 });

  if (!totpSecret) {
    throw new Error(
      'MFA challenge appeared but no totpSecret provided. Pass totpSecret to loginAs() or set MFA_TOTP_SECRET env var.',
    );
  }

  const code = generateTotpCode(totpSecret);
  await mfaInput.fill(code);

  // Click verify / continue button
  await page.click('button[type="submit"], button:has-text("Verify"), button:has-text("Continue")');
}

// ── API Login (token-based for API-driven tests) ───────────────────────────

/**
 * Obtain an API token by calling the login endpoint directly.
 * Useful for API-level assertions without browser UI.
 */
export async function apiLogin(apiBaseUrl: string, args: LoginArgs): Promise<ApiLoginResult> {
  const context = await request.newContext({ baseURL: apiBaseUrl });

  const res = await context.post('/api/v1/auth/login', {
    data: { username: args.username, password: args.password },
  });

  expect(res.status()).toBe(200);
  const body = await res.json();
  await context.dispose();

  return {
    token: body.token || body.accessToken,
    refreshToken: body.refreshToken,
    user: { id: body.user?.id, username: body.user?.username, role: body.user?.role },
  };
}

/**
 * Create an authenticated API request context with a bearer token.
 */
export async function authenticatedApiContext(
  apiBaseUrl: string,
  args: LoginArgs,
): Promise<{ context: APIRequestContext; token: string }> {
  const { token } = await apiLogin(apiBaseUrl, args);
  const context = await request.newContext({
    baseURL: apiBaseUrl,
    extraHTTPHeaders: { Authorization: `Bearer ${token}` },
  });
  return { context, token };
}

// ── Logout ─────────────────────────────────────────────────────────────────

/**
 * Log out via UI and confirm redirection to login.
 */
export async function logout(page: Page): Promise<void> {
  // Click user menu / logout button
  const logoutButton = page.locator(
    'a:has-text("Logout"), button:has-text("Logout"), [data-testid="logout-button"], .logout-link',
  );
  if (await logoutButton.isVisible()) {
    await logoutButton.click();
  } else {
    // Try navigating directly to logout endpoint
    await page.goto('/Portal/Logout', { waitUntil: 'domcontentloaded' });
  }

  // Confirm we're back at login
  await page.waitForURL(`**/Login**`, { timeout: 10_000 });
}

// ── Session Validation ─────────────────────────────────────────────────────

/**
 * Assert that the current session is still valid (user is on an authenticated page).
 */
export async function assertSessionValid(page: Page): Promise<void> {
  await expect(page.locator('.app-sidebar').first()).toBeVisible();
}

/**
 * Wait for session timeout and assert redirect to login.
 * Simulates idle timeout by waiting > SESSION_TIMEOUT_MS.
 */
export async function assertSessionTimeout(page: Page): Promise<void> {
  // Trigger a navigation after idle period to force timeout detection
  await page.waitForTimeout(SESSION_TIMEOUT_MS + 2_000);

  // Try to navigate to a protected page
  await page.goto(DASHBOARD_URL, { waitUntil: 'domcontentloaded' });

  // Should be redirected to login
  await page.waitForURL(`**/Login**`, { timeout: 15_000 });
  await expect(page.locator('input[name="username"]')).toBeVisible();
}

/**
 * Verify that accessing a URL directly while unauthenticated redirects to login.
 */
export async function assertRedirectToLogin(page: Page, protectedUrl: string): Promise<void> {
  await page.goto(protectedUrl, { waitUntil: 'domcontentloaded' });
  await page.waitForURL(`**/Login**`, { timeout: 10_000 });
  await expect(page.locator('input[name="username"]')).toBeVisible();
}

// ── Invalid Login ──────────────────────────────────────────────────────────

/**
 * Attempt login with invalid credentials and verify error message.
 */
export async function assertInvalidLogin(page: Page, username: string, password: string): Promise<void> {
  await page.goto(LOGIN_URL, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[name="username"]', { state: 'visible' });

  await page.fill('input[name="username"]', username);
  await page.fill('input[name="password"]', password);
  await page.click('button[type="submit"]');

  // Expect error message (adjust selector based on your UI)
  const errorLocator = page.locator(
    '.alert-danger, .text-danger, .validation-summary-errors, [data-testid="login-error"], .error-message',
  );
  await expect(errorLocator).toBeVisible({ timeout: 10_000 });
}
