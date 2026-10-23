/**
 * module-progress.spec.ts — Plan M7 module items, requirements, prerequisites and progress. @modules
 *
 * Needs:
 *   MODULES_OFFERING_ID                       a course offering the admin can manage (skipped when unset)
 *   SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD (or ADMIN_USERNAME / ADMIN_PASSWORD)
 *   STUDENT_USERNAME / STUDENT_PASSWORD      a student enrolled in that offering (student steps skipped when unset)
 * Optional: SCREENSHOT_DIR. The spec creates two modules named "PW … <run>" and deletes them at the end.
 */
import { test, expect, Page } from '@playwright/test';
import { randomUUID } from 'crypto';

const OFFERING = process.env.MODULES_OFFERING_ID;
const SHOTS = process.env.SCREENSHOT_DIR;
const RUN = randomUUID().slice(0, 5);
const WEEK_A = `PW Basics ${RUN}`;
const WEEK_B = `PW Practice ${RUN}`;

async function login(page: Page, user?: string, pass?: string) {
  await page.goto('/Login');
  await page.fill('#username', user ?? '');
  await page.fill('#password', pass ?? '');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.toLowerCase().endsWith('/login'), { timeout: 15000 });
}

const loginAdmin = (page: Page) => login(page,
  process.env.SUPER_ADMIN_USERNAME ?? process.env.ADMIN_USERNAME, process.env.SUPER_ADMIN_PASSWORD ?? process.env.ADMIN_PASSWORD);

async function shot(page: Page, name: string) {
  if (!SHOTS) return;
  await page.locator('[data-page-loader]').waitFor({ state: 'hidden' }).catch(() => {});
  await page.screenshot({ path: `${SHOTS}/modules-${name}.png`, fullPage: true });
}

/** The card of a module on LMS Manage (the card header holds its title). */
const manageCard = (page: Page, title: string) => page.locator('.card', { has: page.locator('.card-header', { hasText: title }) });

test.describe('Module progress @modules', () => {
  test.describe.configure({ mode: 'serial' });
  test.skip(!OFFERING, 'MODULES_OFFERING_ID is not set');

  test('teacher builds two modules with requirements and a prerequisite', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await loginAdmin(page);
    const manage = `/Portal/LmsManage?offeringId=${OFFERING}`;

    for (const [title, week] of [[WEEK_A, '90'], [WEEK_B, '91']]) {
      await page.goto(manage);
      const create = page.locator('form[action*="CreateLmsModule"]');
      await create.locator('input[name="title"]').fill(title);
      await create.locator('input[name="weekNumber"]').fill(week);
      await create.locator('button[type="submit"]').click();
      await manageCard(page, title).locator('form[action*="PublishLmsModule"] button').click();
      await expect(manageCard(page, title).locator('.badge', { hasText: 'Published' })).toBeVisible();
    }

    // Week A: a page to view and a link to mark as done.
    let card = manageCard(page, WEEK_A);
    await card.locator('summary', { hasText: 'Add an item' }).click();
    let form = card.locator('[data-add-item-form]');
    await form.locator('[data-item-type]').selectOption('Quiz');
    await form.locator('[data-requirement]').selectOption('MustScoreAtLeast');
    await expect(form.locator('[data-min-score]')).toBeVisible(); // only for "score at least"
    await form.locator('[data-item-type]').selectOption('Page');
    await expect(form.locator('[data-for-target]')).toBeHidden();
    await expect(form.locator('[data-min-score]')).toBeHidden();
    await form.locator('input[name="title"]').fill(`Syllabus ${RUN}`);
    await form.locator('textarea[name="body"]').fill('Welcome.\nRead this first.');
    await form.locator('[data-requirement]').selectOption('MustView');
    await form.locator('button', { hasText: 'Add item' }).click();

    card = manageCard(page, WEEK_A);
    await card.locator('summary', { hasText: 'Add an item' }).click();
    form = card.locator('[data-add-item-form]');
    await form.locator('[data-item-type]').selectOption('ExternalUrl');
    await form.locator('input[name="title"]').fill(`Reading ${RUN}`);
    await form.locator('input[name="url"]').fill('https://example.org/reading');
    await form.locator('[data-requirement]').selectOption('MustMarkDone');
    await form.locator('button', { hasText: 'Add item' }).click();
    await expect(manageCard(page, WEEK_A).locator('[data-module-item-row]')).toHaveCount(2);
    await expect(manageCard(page, WEEK_A).locator('[data-module-item-row]').first()).toContainText(`Syllabus ${RUN}`);

    // Move the reading up and back down again.
    await manageCard(page, WEEK_A).locator('[data-module-item-row]').nth(1).locator('button[aria-label^="Move"][aria-label$="up"]').click();
    await expect(manageCard(page, WEEK_A).locator('[data-module-item-row]').first()).toContainText(`Reading ${RUN}`);
    await manageCard(page, WEEK_A).locator('[data-module-item-row]').first().locator('button[aria-label$="down"]').click();
    await expect(manageCard(page, WEEK_A).locator('[data-module-item-row]').first()).toContainText(`Syllabus ${RUN}`);

    // Week B: a page, opened only after Week A.
    card = manageCard(page, WEEK_B);
    await card.locator('summary', { hasText: 'Add an item' }).click();
    form = card.locator('[data-add-item-form]');
    await form.locator('[data-item-type]').selectOption('Page');
    await form.locator('input[name="title"]').fill(`Lab ${RUN}`);
    await form.locator('[data-requirement]').selectOption('MustView');
    await form.locator('button', { hasText: 'Add item' }).click();
    card = manageCard(page, WEEK_B);
    await card.locator('summary', { hasText: 'Release and completion' }).click();
    await card.locator('[data-release-form] .form-check', { hasText: WEEK_A }).locator('input').check();
    await card.locator('[data-release-form] button').click();
    await expect(manageCard(page, WEEK_B).locator('.badge', { hasText: `After ${WEEK_A}` })).toBeVisible();
    await shot(page, 'manage');
    expect(errors).toEqual([]);
  });

  test('student works through Week A and Week B opens', async ({ page }) => {
    test.skip(!process.env.STUDENT_USERNAME, 'STUDENT_USERNAME is not set');
    await login(page, process.env.STUDENT_USERNAME, process.env.STUDENT_PASSWORD);
    await page.goto(`/Portal/CourseLms?offeringId=${OFFERING}`);
    const weekB = page.locator('[data-student-module]', { hasText: WEEK_B });
    await expect(weekB).toHaveAttribute('data-locked', 'true');
    await expect(weekB.locator('[data-module-lock]')).toContainText(WEEK_A);
    await expect(weekB.locator('a', { hasText: `Lab ${RUN}` })).toHaveCount(0); // locked items are not links
    await shot(page, 'student-before');

    // Syllabus (view) → Next → Reading (mark as done).
    await page.locator('[data-student-module]', { hasText: WEEK_A }).locator('a', { hasText: `Syllabus ${RUN}` }).click();
    await expect(page.locator('[data-module-item]')).toContainText('Read this first.');
    await page.locator('[data-module-nav] a', { hasText: 'Next' }).click();
    await expect(page.locator('h2', { hasText: `Reading ${RUN}` })).toBeVisible();
    await page.locator('[data-mark-done]').click();
    await expect(page.locator('[data-mark-done]')).toContainText('Done');
    await shot(page, 'item');

    await page.goto(`/Portal/CourseLms?offeringId=${OFFERING}`);
    await expect(page.locator('[data-student-module]', { hasText: WEEK_A }).locator('.badge', { hasText: 'Complete' })).toBeVisible();
    await expect(page.locator('[data-student-module]', { hasText: WEEK_B })).toHaveAttribute('data-locked', 'false');
    await expect(page.locator('[data-student-module]', { hasText: WEEK_B }).locator('a', { hasText: `Lab ${RUN}` })).toBeVisible();
    await shot(page, 'student-after');
  });

  test('teacher sees progress, then removes the test modules', async ({ page }) => {
    await loginAdmin(page);
    await page.goto(`/Portal/ModuleProgress?offeringId=${OFFERING}`);
    await expect(page.locator('[data-progress-table]')).toBeVisible();
    await expect(page.locator('[data-progress-table] thead')).toContainText(WEEK_A);
    await shot(page, 'report');

    page.on('dialog', (d) => d.accept());
    for (const title of [WEEK_B, WEEK_A]) {
      await page.goto(`/Portal/LmsManage?offeringId=${OFFERING}`);
      await manageCard(page, title).locator('form[action*="DeleteLmsModule"] button').click();
      await expect(manageCard(page, title)).toHaveCount(0);
    }
  });
});
