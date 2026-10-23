/**
 * grading-workspace.spec.ts — Plan M3 grading workspace and student submission page. @grading
 *
 * Needs a published assignment with submissions:
 *   GRADING_ASSIGNMENT_ID   the assignment to grade (skipped when unset)
 *   FACULTY_USERNAME / FACULTY_PASSWORD, STUDENT_USERNAME / STUDENT_PASSWORD
 * Optional: SCREENSHOT_DIR to save screenshots of each step.
 */
import { test, expect, Page } from '@playwright/test';

const ASSIGNMENT = process.env.GRADING_ASSIGNMENT_ID;
const SHOTS = process.env.SCREENSHOT_DIR;

async function login(page: Page, user?: string, pass?: string) {
  await page.goto('/Login');
  await page.fill('#username', user ?? '');
  await page.fill('#password', pass ?? '');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.toLowerCase().endsWith('/login'), { timeout: 15000 });
}

async function shot(page: Page, name: string) {
  if (SHOTS) await page.screenshot({ path: `${SHOTS}/${name}.png`, fullPage: true });
}

test.describe('Grading workspace @grading', () => {
  test.skip(!ASSIGNMENT, 'GRADING_ASSIGNMENT_ID is not set');

  test('teacher moves between students, annotates, grades and posts without leaving the page', async ({ page }) => {
    await login(page, process.env.FACULTY_USERNAME, process.env.FACULTY_PASSWORD);
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto(`/Portal/GradingWorkspace?assignmentId=${ASSIGNMENT}`);
    const list = page.locator('#ws-list [data-submission]');
    await expect(list.first()).toBeVisible();
    const count = await list.count();
    expect(count).toBeGreaterThan(0);
    await expect(page.locator('#ws-list .active')).toHaveCount(1);

    // The first file renders (PDF canvas or image) with the annotation layer on top.
    const page1 = page.locator('#ws-viewer .sv-page').first();
    await expect(page1).toBeVisible({ timeout: 20000 });
    await expect(page1.locator('svg')).toHaveCount(1);
    await expect(page.locator('#ws-viewer[data-ready="true"]')).toHaveCount(1, { timeout: 20000 });
    await shot(page, '1-workspace');

    // Highlight a passage by dragging on the first page.
    const before = await page.locator('#ws-viewer [data-comment-for]').count();
    await page.locator('#ws-viewer [data-tool="Highlight"]').click();
    const box = (await page1.boundingBox())!;
    await page.mouse.move(box.x + box.width * 0.1, box.y + box.height * 0.08);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width * 0.6, box.y + box.height * 0.11, { steps: 5 });
    await page.mouse.up();
    await expect(page.locator('#ws-viewer [data-comment-for]')).toHaveCount(before + 1);
    await expect(page1.locator('svg [data-annotation]')).toHaveCount(before + 1);
    await page.locator('#ws-viewer [data-comment-for]').last().fill('Clear thesis statement');
    await page.locator('#ws-viewer [data-comment-for]').last().blur();
    await shot(page, '2-annotated');

    // Marks autosave.
    await page.fill('#ws-marks', '15');
    await page.fill('#ws-feedback', 'Well argued, cite one more source.');
    await expect(page.locator('#ws-panel')).toContainText(/Saved/, { timeout: 10000 });

    // Keyboard: next student without a page load.
    if (count > 1) {
      const firstUrl = page.url();
      const label = await page.locator('#ws-panel h2').textContent();
      await page.locator('body').click({ position: { x: 5, y: 5 } });
      await page.keyboard.press('Alt+ArrowRight');
      await expect(page.locator('#ws-panel h2')).not.toHaveText(label ?? '', { timeout: 10000 });
      expect(page.url()).not.toBe(firstUrl);
      await shot(page, '3-next-student');
      await page.keyboard.press('Alt+ArrowLeft');
      await expect(page.locator('#ws-panel h2')).toHaveText(label ?? '');
    }

    // Image submissions open on an annotatable image canvas.
    const imageButton = page.locator('#ws-panel button', { hasText: /\.(png|jpe?g|gif|webp)/i }).first();
    if (await imageButton.isVisible().catch(() => false)) {
      await imageButton.click();
      await expect(page.locator('#ws-viewer .sv-page img')).toBeVisible({ timeout: 15000 });
      await expect(page.locator('#ws-viewer .sv-page svg')).toHaveCount(1);
      await shot(page, '3b-image');
    }

    // Post grades (manual posting) when there is anything to post.
    const post = page.locator('button[name="operation"][value="post"]');
    if (await post.isVisible().catch(() => false)) {
      page.once('dialog', (d) => d.accept());
      await post.click();
      await expect(page.locator('.alert')).toContainText(/posted/);
    }
    await shot(page, '4-posted');
    expect(errors).toEqual([]);
  });

  test('student sees their attempts and previews a file', async ({ page }) => {
    await login(page, process.env.STUDENT_USERNAME, process.env.STUDENT_PASSWORD);
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await page.goto(`/Portal/AssignmentSubmission?assignmentId=${ASSIGNMENT}`);
    await expect(page.locator('h2')).toBeVisible();
    const preview = page.locator('[data-preview]').first();
    if (await preview.isVisible().catch(() => false)) {
      await preview.click();
      await expect(page.locator('#student-viewer .sv-page').first()).toBeVisible({ timeout: 20000 });
      // Students never get drawing tools.
      await expect(page.locator('#student-viewer [data-tool]')).toHaveCount(0);
    }
    await shot(page, '5-student');
    expect(errors).toEqual([]);
  });
});
