/**
 * Navigation helpers – sidebar menu traversal, breadcrumbs, and URL-based routing.
 *
 * The EduSphere sidebar is dynamic: items are filtered by role, institution type,
 * and license status. This module provides safe navigation that asserts visibility
 * before attempting clicks.
 */
import { Page, expect, Locator } from '@playwright/test';

// ── Types ──────────────────────────────────────────────────────────────────

export interface MenuItem {
  /** Display label of the menu item (case-insensitive partial match). */
  label: string;
  /** Expected URL pattern after clicking (e.g., '/Portal/Students'). */
  urlPattern?: string;
  /** Roles that should see this item. */
  visibleToRoles?: string[];
  /** Institution types where applicable (School, College, University). */
  institutionTypes?: string[];
  /** Whether this item requires a specific license feature. */
  licenseFeature?: string;
}

// ── Sidebar selectors ──────────────────────────────────────────────────────

const SIDEBAR_SELECTOR = '.app-sidebar';
const SIDEBAR_LINK_SELECTOR = `.sidebar-nav a, .sidebar-nav .nav-link`;
const MENU_TOGGLE_SELECTOR = `.sidebar-nav .dropdown-toggle, .sidebar-nav [data-bs-toggle="collapse"]`;

// ── Navigate by menu label ─────────────────────────────────────────────────

/**
 * Click a sidebar menu item by its visible label.
 * Expands parent dropdown groups automatically if the item is nested.
 *
 * @param page        - Playwright Page
 * @param label       - Human-readable menu label (case-insensitive partial)
 * @param urlPattern  - If provided, asserts the final URL matches this pattern
 *
 * @example
 *   await navigateMenu(page, 'Students');
 *   await navigateMenu(page, 'Enter Attendance', '/Portal/Attendance/Enter');
 */
export async function navigateMenu(
  page: Page,
  label: string,
  urlPattern?: string,
): Promise<void> {
  // Wait for sidebar to be fully loaded
  await page.waitForSelector(SIDEBAR_SELECTOR, { state: 'visible', timeout: 15_000 });

  // Try to expand any collapsed parent groups that might contain the target
  await expandSidebarGroups(page);

  // Locate the menu item by its visible text
  const link = findMenuItem(page, label);
  await expect(link, `Sidebar menu item "${label}" should be visible`).toBeVisible({
    timeout: 10_000,
  });

  await link.click();

  // Optionally wait for URL
  if (urlPattern) {
    await page.waitForURL(`**${urlPattern}**`, { timeout: 15_000 });
  }
}

/**
 * Find a sidebar menu item by label (case-insensitive, partial match).
 */
function findMenuItem(page: Page, label: string): Locator {
  const lowerLabel = label.toLowerCase();
  return page
    .locator(SIDEBAR_LINK_SELECTOR)
    .filter({ hasText: new RegExp(lowerLabel, 'i') })
    .first();
}

/**
 * Expand all collapsed sidebar groups so nested items become visible.
 */
async function expandSidebarGroups(page: Page): Promise<void> {
  const toggles = page.locator(`${MENU_TOGGLE_SELECTOR}[aria-expanded="false"]`);
  const count = await toggles.count();

  for (let i = 0; i < count; i++) {
    const toggle = toggles.nth(i);
    if (await toggle.isVisible().catch(() => false)) {
      await toggle.click();
      // Small delay for collapse animation
      await page.waitForTimeout(300);
    }
  }
}

// ── Assert menu visibility ─────────────────────────────────────────────────

/**
 * Assert that a sidebar menu item IS visible to the current role.
 */
export async function assertMenuVisible(page: Page, label: string): Promise<void> {
  await page.waitForSelector(SIDEBAR_SELECTOR, { state: 'visible' });
  await expandSidebarGroups(page);

  const link = findMenuItem(page, label);
  await expect(
    link,
    `Menu item "${label}" should be visible for the current role`,
  ).toBeVisible({ timeout: 5_000 });
}

/**
 * Assert that a sidebar menu item is NOT visible (access denied by role).
 */
export async function assertMenuHidden(page: Page, label: string): Promise<void> {
  await page.waitForSelector(SIDEBAR_SELECTOR, { state: 'visible' });
  await expandSidebarGroups(page);

  const link = findMenuItem(page, label);
  await expect(
    link,
    `Menu item "${label}" should be hidden for the current role`,
  ).not.toBeVisible({ timeout: 5_000 });
}

// ── Direct URL access control ──────────────────────────────────────────────

/**
 * Attempt to directly navigate to a protected URL and assert the outcome.
 *
 * @param page          - Playwright Page
 * @param url           - Protected URL to attempt
 * @param expectBlocked - If true, expect redirect or access-denied; if false, expect success
 */
export async function assertDirectUrlAccess(
  page: Page,
  url: string,
  expectBlocked: boolean,
): Promise<void> {
  await page.goto(url, { waitUntil: 'domcontentloaded' });

  if (expectBlocked) {
    // Should be redirected to dashboard or show access denied
    const isDashboard = page.url().includes('/Dashboard');
    const isAccessDenied = await page
      .locator('.access-denied, .unauthorized, [data-testid="access-denied"], h1:has-text("Access Denied")')
      .isVisible()
      .catch(() => false);

    expect(
      isDashboard || isAccessDenied,
      `URL ${url} should be blocked for current role`,
    ).toBeTruthy();
  } else {
    // Should load successfully – no redirect, no error
    await page.waitForLoadState('networkidle', { timeout: 15_000 });
    const hasError = await page
      .locator('.access-denied, .unauthorized, h1:has-text("Access Denied")')
      .isVisible()
      .catch(() => false);
    expect(hasError, `URL ${url} should be accessible`).toBeFalsy();
  }
}

// ── Breadcrumb helpers ─────────────────────────────────────────────────────

/**
 * Verify breadcrumb trail matches expected path segments.
 *
 * @example await assertBreadcrumbs(page, ['Home', 'Students', 'View']);
 */
export async function assertBreadcrumbs(page: Page, segments: string[]): Promise<void> {
  const breadcrumb = page.locator('.breadcrumb, nav[aria-label="breadcrumb"], ol.breadcrumb');
  await expect(breadcrumb).toBeVisible({ timeout: 5_000 });

  for (const segment of segments) {
    await expect(breadcrumb.locator(`li:has-text("${segment}")`)).toBeVisible();
  }
}

// ── Page heading assertion ─────────────────────────────────────────────────

/**
 * Assert that the main page heading contains the expected text.
 */
export async function assertPageHeading(page: Page, heading: string): Promise<void> {
  const h1 = page.locator('h1, .page-title, [data-testid="page-heading"]').first();
  await expect(h1).toBeVisible({ timeout: 10_000 });
  await expect(h1).toContainText(heading, { ignoreCase: true });
}

// ── Dropdown / select helpers ──────────────────────────────────────────────

/**
 * Select an option from a <select> or custom dropdown by its data-testid or label.
 */
export async function selectDropdown(
  page: Page,
  selector: string,
  optionText: string,
): Promise<void> {
  const dropdown = page.locator(selector);
  await dropdown.waitFor({ state: 'visible', timeout: 5_000 });
  await dropdown.selectOption({ label: optionText });
}
