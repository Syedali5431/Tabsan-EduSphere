/**
 * similarity.spec.ts — Plan M9 similarity (plagiarism) checking. @similarity
 *
 * Needs:
 *   SIMILARITY_ASSIGNMENT_ID                   a published assignment the admin can manage (skipped when unset)
 *   SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD (or ADMIN_USERNAME / ADMIN_PASSWORD)
 *   STUDENT_USERNAME / STUDENT_PASSWORD        a student who handed the assignment in (student step skipped when unset)
 * Optional: SCREENSHOT_DIR; M9_DEMO_DATA=1 adds checks that need completed checks in the grading workspace
 * (the preview data has them). The spec turns checking on for the assignment and restores the built-in checker.
 */
import { test, expect, Page } from '@playwright/test';

const ASSIGNMENT = process.env.SIMILARITY_ASSIGNMENT_ID;
const SHOTS = process.env.SCREENSHOT_DIR;
const DEMO = process.env.M9_DEMO_DATA === '1';

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
  await page.screenshot({ path: `${SHOTS}/similarity-${name}.png`, fullPage: true });
}

const flash = (page: Page) => page.locator('.alert[role="status"]').first();

test.describe('Similarity checking @similarity', () => {
  test.describe.configure({ mode: 'serial' });
  test.skip(!ASSIGNMENT, 'SIMILARITY_ASSIGNMENT_ID is not set');

  test('admin chooses the checker and tests it', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Portal/SimilaritySettings');
    const form = page.locator('[data-similarity-settings]');
    await expect(form).toBeVisible();

    // Copyleaks needs an email and key; the key is never shown again.
    await form.locator('#sim-copyleaks').check();
    await form.locator('#sim-email').fill('checker@uni.edu');
    await form.locator('#sim-key').fill('pw-test-key');
    await form.locator('#sim-sandbox').check();
    await form.locator('button', { hasText: 'Save' }).click();
    await expect(flash(page)).toContainText('Similarity settings saved.');
    await expect(page.locator('#sim-key')).toHaveAttribute('placeholder', /Saved/);
    await expect(page.locator('#sim-key')).toHaveValue('');
    await page.locator('form[action*="TestSimilaritySettings"] button').click();
    await expect(flash(page)).toContainText('Copyleaks');
    await shot(page, 'settings');

    // Back to the built-in comparison for the rest of the run.
    await page.locator('#sim-internal').check();
    await page.locator('#sim-scope').selectOption('Course');
    await page.locator('[data-similarity-settings] button', { hasText: 'Save' }).click();
    await expect(flash(page)).toContainText('Similarity settings saved.');
  });

  test('teacher turns checking on and reads a report from the workspace', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await loginAdmin(page);
    await page.goto(`/Portal/AssignmentOptions?assignmentId=${ASSIGNMENT}`);
    const card = page.locator('[data-assignment-similarity]');
    await card.locator('#sim-on').check();
    await card.locator('#sim-students').check();
    await card.locator('button', { hasText: 'Save' }).click();
    await expect(flash(page)).toContainText('Similarity checking is on');
    await expect(page.locator('#sim-on')).toBeChecked();

    test.skip(!DEMO, 'M9_DEMO_DATA is not set');
    await page.goto(`/Portal/GradingWorkspace?assignmentId=${ASSIGNMENT}`);
    const high = page.locator('#ws-list [data-submission]', { has: page.locator('[data-similarity]', { hasText: '62.5%' }) });
    await expect(high).toHaveCount(1);
    await high.click();
    await expect(page.locator('#ws-similarity')).toContainText('62.5%');
    await shot(page, 'workspace');
    await page.locator('#ws-similarity a', { hasText: 'Open report' }).click();
    await expect(page.locator('[data-similarity-score]')).toContainText('62.5%');
    await expect(page.locator('[data-source]').first()).toContainText('Student 1');
    await expect(page.locator('blockquote mark').first()).toContainText('Rayleigh scattering');
    await shot(page, 'report');

    await page.goBack();
    await page.locator('#ws-list [data-submission]').nth(0).click(); // Student 1: not the one above nor the student's own
    await page.locator('#ws-similarity button', { hasText: 'Check again' }).click();
    await expect(page.locator('#ws-similarity')).toContainText('checking…');
    expect(errors).toEqual([]);
  });

  test('student sees their own score only', async ({ page }) => {
    test.skip(!process.env.STUDENT_USERNAME || !DEMO, 'STUDENT_USERNAME / M9_DEMO_DATA is not set');
    await login(page, process.env.STUDENT_USERNAME, process.env.STUDENT_PASSWORD);
    await page.goto(`/Portal/AssignmentSubmission?assignmentId=${ASSIGNMENT}`);
    await expect(page.locator('[data-my-similarity]')).toContainText('%');
    await shot(page, 'student');
    // Reports are for staff: the portal sends students away instead of rendering one.
    const report = await page.request.get('/Portal/SimilarityReport/99999999-0000-0000-0000-000000000002', { maxRedirects: 0 });
    expect(report.status()).toBe(302);
    expect(await report.text()).not.toContain('data-similarity-score');
  });
});
