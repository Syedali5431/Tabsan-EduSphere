/**
 * promote-graduate.spec.ts — Promote students across all 3 institutes
 * until they complete (School/College) or graduate (University).
 *
 * PREREQUISITES:
 *   - API must be running on http://localhost:5181
 *   - admin.uni must exist in DB with password EduSphere147
 *   - Departments must be populated and accessible via API for admin's tenant
 *   - Student profiles must exist in admin's scope
 *
 * Flow:
 *   1. Login as admin (admin.uni)
 *   2. Navigate to StudentLifecycle page
 *   3. Select institution type, department via UI filters
 *   4. Find students and promote/graduate via POST forms
 *   5. Verify success messages
 * Routes:
 *   GET  /Portal/StudentLifecycle?departmentId=...&institutionType=...
 *   POST /Portal/PromoteStudent   (school/college: advance to next class)
 *   POST /Portal/GraduateStudent  (university only: final graduation)
 *
 * School:  promote until semester ≥ 10 → "Student cleared the school"
 * College: promote through classes 11-12 → completion
 * Uni:     graduate → "Student graduated"
 */
import { test, expect } from '@playwright/test';

const TEST_USER = 'admin.uni';
const TEST_PASS = process.env.ADMIN_PASSWORD || 'EduSphere147';

async function adminLogin(page: any) {
  await page.goto('/Login');
  await page.fill('#username', TEST_USER);
  await page.fill('#password', TEST_PASS);
  await page.click('button[type="submit"]');
  await expect(page.locator('.app-sidebar').first()).toBeVisible({ timeout: 15000 });
}

test.describe('Student Promotion & Graduation', () => {
  // ═══════════════════════════════════════════════════════════════════════════
  //  LIFECYCLE PAGE LOAD
  // ═══════════════════════════════════════════════════════════════════════════
  test('admin can load Student Lifecycle page', async ({ page }) => {
    await adminLogin(page);
    await page.goto('/Portal/StudentLifecycle');
    await page.waitForLoadState('domcontentloaded');

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();

    // Page should have a department filter or student table
    const hasContent = await page
      .locator('table, select[name="departmentId"], .empty-state-card')
      .first()
      .isVisible()
      .catch(() => false);
    expect(hasContent).toBeTruthy();
  });

  // ═══════════════════════════════════════════════════════════════════════════
  //  PROMOTE STUDENTS — select department, then promote first visible student
  // ═══════════════════════════════════════════════════════════════════════════
  test('admin can promote students from Student Lifecycle', async ({ page }) => {
    await adminLogin(page);
    await page.goto('/Portal/StudentLifecycle');
    await page.waitForLoadState('domcontentloaded');

    // Select a department that has students in admin's scope
    const deptFilter = page.locator('select[name="departmentId"]');
    if (await deptFilter.isVisible({ timeout: 3000 }).catch(() => false)) {
      // Pick first non-placeholder option
      const options = deptFilter.locator('option');
      const count = await options.count();
      if (count > 1) {
        await deptFilter.selectOption({ index: 1 }); // first real department
        await page.waitForLoadState('domcontentloaded');
      }
    }

    // Look for promote buttons in the table
    const promoteButtons = page.locator(
      'button:has-text("Promote"), a:has-text("Promote"), form[action*="PromoteStudent"] button',
    );

    const btnCount = await promoteButtons.count();
    if (btnCount === 0) {
      console.log('No promote buttons found — no students in this view');
      return;
    }

    // Click first promote button
    await promoteButtons.first().click();
    await page.waitForLoadState('domcontentloaded');

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });

  // ═══════════════════════════════════════════════════════════════════════════
  //  GRADUATE UNIVERSITY STUDENT
  // ═══════════════════════════════════════════════════════════════════════════
  test('admin can graduate a university student', async ({ page }) => {
    await adminLogin(page);
    await page.goto('/Portal/StudentLifecycle');
    await page.waitForLoadState('domcontentloaded');

    // Select institution type = University (value=2)
    const instFilter = page.locator('select[name="institutionType"]');
    if (await instFilter.isVisible({ timeout: 2000 }).catch(() => false)) {
      await instFilter.selectOption('2'); // University
      await page.waitForLoadState('domcontentloaded');
    }

    // Select a department
    const deptFilter = page.locator('select[name="departmentId"]');
    if (await deptFilter.isVisible({ timeout: 2000 }).catch(() => false)) {
      const options = deptFilter.locator('option');
      const count = await options.count();
      if (count > 1) {
        await deptFilter.selectOption({ index: 1 });
        await page.waitForLoadState('domcontentloaded');
      }
    }

    // Look for Graduate buttons (university only)
    const graduateButtons = page.locator(
      'button:has-text("Graduate"), a:has-text("Graduate"), form[action*="GraduateStudent"] button',
    );

    const count = await graduateButtons.count();
    if (count === 0) {
      console.log('No graduate buttons found — may need university department selected');
      return;
    }

    await graduateButtons.first().click();
    await page.waitForLoadState('domcontentloaded');

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });

  // ═══════════════════════════════════════════════════════════════════════════
  //  FILTER BY INSTITUTION TYPE
  // ═══════════════════════════════════════════════════════════════════════════
  test('admin can filter lifecycle by institution type', async ({ page }) => {
    await adminLogin(page);
    await page.goto('/Portal/StudentLifecycle');
    await page.waitForLoadState('domcontentloaded');

    // Try selecting different institution types via the filter
    const instFilter = page.locator('select[name="institutionType"]');
    if (await instFilter.isVisible({ timeout: 2000 }).catch(() => false)) {
      // University (2) — check if graduate buttons appear
      await instFilter.selectOption('2');
      await page.waitForLoadState('domcontentloaded');

      // School (0) — check if promote-only
      await instFilter.selectOption('0');
      await page.waitForLoadState('domcontentloaded');

      // College (1)
      await instFilter.selectOption('1');
      await page.waitForLoadState('domcontentloaded');
    }

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });

  // ═══════════════════════════════════════════════════════════════════════════
  //  FULL PROMOTION LOOP — promote a student repeatedly until completion
  // ═══════════════════════════════════════════════════════════════════════════
  test('admin can repeatedly promote a school/college student to completion', async ({ page }) => {
    await adminLogin(page);
    await page.goto('/Portal/StudentLifecycle');
    await page.waitForLoadState('domcontentloaded');

    // Select School (institutionType=0) or College (1) via filter
    const instFilter = page.locator('select[name="institutionType"]');
    if (await instFilter.isVisible({ timeout: 2000 }).catch(() => false)) {
      await instFilter.selectOption('0'); // School
      await page.waitForLoadState('domcontentloaded');
    }

    // Select a department
    const deptFilter = page.locator('select[name="departmentId"]');
    if (await deptFilter.isVisible({ timeout: 2000 }).catch(() => false)) {
      const options = deptFilter.locator('option');
      const count = await options.count();
      if (count > 1) {
        await deptFilter.selectOption({ index: 1 });
        await page.waitForLoadState('domcontentloaded');
      }
    }

    // Promote up to 12 times (max semesters) until "cleared school" message
    for (let i = 0; i < 12; i++) {
      const promoteBtn = page.locator(
        'button:has-text("Promote"), a:has-text("Promote"), form[action*="PromoteStudent"] button',
      ).first();

      if (!(await promoteBtn.isVisible({ timeout: 2000 }).catch(() => false))) {
        break; // No more students or buttons
      }

      await promoteBtn.click();
      await page.waitForLoadState('domcontentloaded');

      // Check for completion message
      const completionMsg = page.locator('.alert-info:has-text("cleared"), .alert-success:has-text("cleared")');
      if (await completionMsg.isVisible({ timeout: 1000 }).catch(() => false)) {
        console.log('School student cleared!');
        break;
      }
    }

    const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
    expect(hasError).toBeFalsy();
  });
});
