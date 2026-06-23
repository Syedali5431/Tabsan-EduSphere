/**
 * Filter helpers – cascading Institution → Department → Course → Semester/Class
 *
 * EduSphere uses a hierarchical filter chain. Selecting an institution populates
 * departments; selecting a department populates courses; etc. This module provides
 * reusable selectors and actions for interacting with these filters.
 */
import { Page, expect } from '@playwright/test';

// ── Types ──────────────────────────────────────────────────────────────────

export interface FilterChain {
  institution?: string;
  department?: string;
  course?: string;
  semesterOrClass?: string;
  /** 'semester' for University, 'class' for School/College */
  levelType?: 'semester' | 'class';
}

// ── Selectors (adjust to match your actual UI) ─────────────────────────────

const SELECTORS = {
  institutionFilter: 'select[name="InstitutionId"], select[data-testid="filter-institution"], #filter-institution',
  departmentFilter: 'select[name="DepartmentId"], select[data-testid="filter-department"], #filter-department',
  courseFilter: 'select[name="CourseId"], select[data-testid="filter-course"], #filter-course',
  semesterFilter: 'select[name="SemesterId"], select[data-testid="filter-semester"], #filter-semester',
  classFilter: 'select[name="ClassId"], select[data-testid="filter-class"], #filter-class',
  applyButton: 'button:has-text("Apply"), button:has-text("Filter"), button[type="submit"], [data-testid="apply-filters"]',
};

// ── Apply a full filter chain ──────────────────────────────────────────────

/**
 * Apply a complete cascade of filters.
 * Each filter is selected in order, waiting for the next dropdown to populate.
 *
 * @example
 *   await applyFilters(page, {
 *     institution: 'Demo University',
 *     department: 'Computer Science',
 *     course: 'BSCS',
 *     semesterOrClass: 'Semester 2',
 *     levelType: 'semester',
 *   });
 */
export async function applyFilters(page: Page, chain: FilterChain): Promise<void> {
  // 1) Institution
  if (chain.institution) {
    await selectFilterOption(page, SELECTORS.institutionFilter, chain.institution);
  }

  // 2) Department (populated after institution selection)
  if (chain.department) {
    await waitForFilterToLoad(page, SELECTORS.departmentFilter);
    await selectFilterOption(page, SELECTORS.departmentFilter, chain.department);
  }

  // 3) Course (populated after department selection)
  if (chain.course) {
    await waitForFilterToLoad(page, SELECTORS.courseFilter);
    await selectFilterOption(page, SELECTORS.courseFilter, chain.course);
  }

  // 4) Semester or Class
  if (chain.semesterOrClass) {
    const selector = chain.levelType === 'semester'
      ? SELECTORS.semesterFilter
      : SELECTORS.classFilter;
    await waitForFilterToLoad(page, selector);
    await selectFilterOption(page, selector, chain.semesterOrClass);
  }

  // 5) Click apply if there's a separate button
  const applyBtn = page.locator(SELECTORS.applyButton);
  if (await applyBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
    await applyBtn.click();
    await page.waitForLoadState('networkidle', { timeout: 10_000 });
  }
}

/**
 * Select an option by its visible text from a given dropdown.
 */
async function selectFilterOption(page: Page, selector: string, optionText: string): Promise<void> {
  const dropdown = page.locator(selector);
  await dropdown.waitFor({ state: 'visible', timeout: 8_000 });

  // Try selecting by label attribute
  try {
    await dropdown.selectOption({ label: optionText });
  } catch {
    // Fallback: select by value or visible text
    const option = dropdown.locator(`option:has-text("${optionText}")`);
    const value = await option.getAttribute('value');
    if (value) {
      await dropdown.selectOption(value);
    } else {
      await dropdown.selectOption({ label: optionText });
    }
  }
}

/**
 * Wait for a filter dropdown to be populated (has more than just a placeholder option).
 */
async function waitForFilterToLoad(page: Page, selector: string): Promise<void> {
  const dropdown = page.locator(selector);
  await dropdown.waitFor({ state: 'visible', timeout: 5_000 });
  // Wait until at least 2 options exist (placeholder + real data)
  await expect(async () => {
    const count = await dropdown.locator('option').count();
    expect(count).toBeGreaterThan(1);
  }).toPass({ timeout: 10_000, intervals: [500] });
}

// ── Clear all filters ──────────────────────────────────────────────────────

/**
 * Reset all filter dropdowns to their default (first option / placeholder).
 */
export async function clearAllFilters(page: Page): Promise<void> {
  const selectors = [
    SELECTORS.institutionFilter,
    SELECTORS.departmentFilter,
    SELECTORS.courseFilter,
    SELECTORS.semesterFilter,
    SELECTORS.classFilter,
  ];

  for (const selector of selectors) {
    const dropdown = page.locator(selector);
    if (await dropdown.isVisible({ timeout: 1_000 }).catch(() => false)) {
      await dropdown.selectOption({ index: 0 });
    }
  }

  // Click apply after resetting
  const applyBtn = page.locator(SELECTORS.applyButton);
  if (await applyBtn.isVisible({ timeout: 1_000 }).catch(() => false)) {
    await applyBtn.click();
    await page.waitForLoadState('networkidle', { timeout: 8_000 });
  }
}

// ── Assert filter state ────────────────────────────────────────────────────

/**
 * Assert that a filter dropdown has at least the expected number of options.
 */
export async function assertFilterHasOptions(page: Page, selector: string, minOptions: number): Promise<void> {
  const dropdown = page.locator(selector);
  await expect(dropdown).toBeVisible({ timeout: 5_000 });
  const count = await dropdown.locator('option').count();
  expect(count, `Filter should have at least ${minOptions} options`).toBeGreaterThanOrEqual(minOptions);
}

/**
 * Assert that a specific option exists in a filter dropdown.
 */
export async function assertFilterOptionExists(page: Page, selector: string, optionText: string): Promise<void> {
  const dropdown = page.locator(selector);
  await expect(dropdown.locator(`option:has-text("${optionText}")`)).toBeAttached({ timeout: 5_000 });
}

// ── Empty filters edge case ────────────────────────────────────────────────

/**
 * Verify that the page handles empty filter state gracefully (no crash, empty table message).
 */
export async function assertEmptyFilterState(page: Page): Promise<void> {
  // Select an institution with no data
  const dropdown = page.locator(SELECTORS.institutionFilter);
  if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
    // Pick the last option (often a "no data" institution in test seeds)
    const options = dropdown.locator('option');
    const count = await options.count();
    if (count > 1) {
      await dropdown.selectOption({ index: count - 1 });
      const applyBtn = page.locator(SELECTORS.applyButton);
      if (await applyBtn.isVisible().catch(() => false)) {
        await applyBtn.click();
        await page.waitForLoadState('networkidle', { timeout: 8_000 });
      }
    }
  }

  // Should show "no data" message or empty table, NOT crash
  const noDataMsg = page.locator('.no-data, .empty-state, td:has-text("No records"), [data-testid="empty-state"]');
  const table = page.locator('table');
  const hasNoData = await noDataMsg.isVisible().catch(() => false);
  const hasTable = await table.isVisible().catch(() => false);

  expect(
    hasNoData || hasTable,
    'Page should handle empty filter state without crashing',
  ).toBeTruthy();
}
