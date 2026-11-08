/**
 * outcomes.spec.ts — Plan M6 learning outcomes, alignment, mastery gradebook, attainment and the student's view. @outcomes
 *
 * Needs:
 *   OUTCOMES_OFFERING_ID                     a course offering with graded work (skipped when unset)
 *   SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD (or ADMIN_USERNAME / ADMIN_PASSWORD)
 *   the "outcomes" module active
 * Optional: OUTCOMES_QUIZ_ID (a quiz in that offering, to align a question), OUTCOMES_COURSE_ID (that offering's course: the
 * new outcome is made for it, so the quiz can be aligned to it; without it the outcome goes to the first course in the list,
 * which only works when there is one course), STUDENT_USERNAME / STUDENT_PASSWORD, SCREENSHOT_DIR.
 */
import { test, expect, Page } from '@playwright/test';
import { randomUUID } from 'crypto';

const OFFERING = process.env.OUTCOMES_OFFERING_ID;
const QUIZ = process.env.OUTCOMES_QUIZ_ID;
const COURSE = process.env.OUTCOMES_COURSE_ID;
const SHOTS = process.env.SCREENSHOT_DIR;
const CODE = `CLO-${randomUUID().slice(0, 5).toUpperCase()}`;

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
  await page.screenshot({ path: `${SHOTS}/outcomes-${name}.png`, fullPage: true });
}

test.describe('Learning outcomes @outcomes', () => {
  test.describe.configure({ mode: 'serial' });
  test.skip(!OFFERING, 'OUTCOMES_OFFERING_ID is not set');

  test('admin adds a course outcome with its own mastery rule and maps it to a program outcome', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await loginAdmin(page);
    await page.goto('/Portal/Outcomes');
    const form = page.locator('[data-outcome-form]');
    await expect(form).toBeVisible();

    await page.selectOption('#o-level', 'Course');
    await expect(page.locator('#o-program')).toBeHidden();
    await expect(page.locator('#o-course')).toBeVisible();
    if (COURSE) await page.selectOption('#o-course', COURSE.toLowerCase());
    await page.fill('#o-code', CODE);
    await page.fill('#o-title', 'Write correct programs using loops and functions');
    await page.selectOption('#o-method', 'NMastery');
    await expect(page.locator('[data-parameter-label]')).toHaveText('Times needed');
    await page.fill('#o-parameter', '2');
    await form.locator('button.btn-primary').click();

    await expect(page.locator('h3', { hasText: `Edit ${CODE}` })).toBeVisible();
    const row = page.locator('[data-outcomes-table] tr', { hasText: CODE });
    await expect(row).toContainText('Mastered 2 times');

    const mapping = page.locator('[data-outcome-mapping]');
    if (await mapping.count()) {
      await mapping.locator('input[type="checkbox"]').first().check();
      await mapping.locator('button').click();
      await expect(page.locator('[data-outcome-mapping] input[type="checkbox"]').first()).toBeChecked();
    }
    await shot(page, 'manage');
    expect(errors).toEqual([]);
  });

  test('teacher aligns a quiz question with the Outcomes button', async ({ page }) => {
    test.skip(!QUIZ, 'OUTCOMES_QUIZ_ID is not set');
    await loginAdmin(page);
    await page.goto(`/Portal/QuizBuilder?quizId=${QUIZ}`);
    await expect(page.locator('html')).toHaveAttribute('data-outcomes-ready', 'true');
    const button = page.locator('[data-outcome-align]:visible').first();
    await button.click();
    const modal = page.locator('#outcome-align-modal');
    await expect(modal).toBeVisible();
    const box = modal.locator('.form-check', { hasText: CODE }).locator('input');
    await box.check();
    await shot(page, 'align');
    await modal.locator('[data-outcome-save]').click();
    await expect(modal).toBeHidden();
    await expect(button).toContainText(/Outcomes \(\d+\)/);

    // Reopening shows the saved choice.
    await button.click();
    await expect(modal.locator('.form-check', { hasText: CODE }).locator('input')).toBeChecked();
  });

  test('mastery gradebook and program attainment open and export', async ({ page }) => {
    await loginAdmin(page);
    await page.goto(`/Portal/OutcomeMastery?offeringId=${OFFERING}`);
    const grid = page.locator('[data-mastery-grid]');
    await expect(grid.or(page.locator('[data-mastery-empty]'))).toBeVisible();
    if (await grid.count()) {
      await expect(grid.locator('thead th').nth(1)).toBeVisible();
      const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('link', { name: 'Export to Excel' }).click()]);
      expect(download.suggestedFilename()).toMatch(/\.xlsx$/);
    }
    await shot(page, 'mastery');

    await page.goto('/Portal/OutcomeAttainment');
    const program = page.locator('#a-program option').nth(1);
    test.skip(!(await program.count()), 'No academic programs');
    await page.selectOption('#a-program', { index: 1 });
    await page.fill('#a-target', '75');
    await page.getByRole('button', { name: 'Show report' }).click();
    const table = page.locator('[data-attainment-table]');
    await expect(table.or(page.getByText('This program has no program outcomes yet.'))).toBeVisible();
    if (await table.count()) {
      const [pdf] = await Promise.all([page.waitForEvent('download'), page.getByRole('link', { name: 'Download PDF' }).click()]);
      expect(pdf.suggestedFilename()).toMatch(/\.pdf$/);
    }
    await shot(page, 'attainment');
  });

  test('students see their own mastery', async ({ page }) => {
    test.skip(!process.env.STUDENT_USERNAME, 'STUDENT_USERNAME is not set');
    await login(page, process.env.STUDENT_USERNAME, process.env.STUDENT_PASSWORD);
    await page.goto('/Portal/MyOutcomes');
    await expect(page.locator('[data-outcome-item]').first().or(page.locator('[data-my-outcomes-empty]'))).toBeVisible();
    const evidence = page.locator('[data-outcome-item] details').first();
    if (await evidence.count()) {
      await evidence.locator('summary').click();
      await expect(evidence.locator('li').first()).toBeVisible();
    }
    await shot(page, 'student');
  });
});
