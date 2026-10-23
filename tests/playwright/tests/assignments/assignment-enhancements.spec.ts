/**
 * assignment-enhancements.spec.ts — Plan M8 late & missing policy, "Assign to" dates, student groups, group discussions
 * and peer review. @assignments
 *
 * Needs:
 *   ASSIGNMENTS_OFFERING_ID                    a course offering the admin can manage (skipped when unset)
 *   ASSIGNMENT_ID                              a published assignment in that offering
 *   SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD (or ADMIN_USERNAME / ADMIN_PASSWORD)
 *   STUDENT_USERNAME / STUDENT_PASSWORD        a student enrolled in that offering (student steps skipped when unset)
 * Optional: SCREENSHOT_DIR; M8_DEMO_DATA=1 adds checks that need a late, graded submission and a group submission with
 * completed peer reviews in the grading workspace (the preview data has both).
 * The spec creates a group set and a date override named "PW … <run>" and removes them again.
 */
import { test, expect, Page } from '@playwright/test';
import { randomUUID } from 'crypto';

const OFFERING = process.env.ASSIGNMENTS_OFFERING_ID;
const ASSIGNMENT = process.env.ASSIGNMENT_ID;
const SHOTS = process.env.SCREENSHOT_DIR;
const DEMO = process.env.M8_DEMO_DATA === '1';
const RUN = randomUUID().slice(0, 5);

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
  await page.screenshot({ path: `${SHOTS}/assignments-${name}.png`, fullPage: true });
}

const flash = (page: Page) => page.locator('.alert[role="status"]').first();

test.describe('Assignment enhancements @assignments', () => {
  test.describe.configure({ mode: 'serial' });
  test.skip(!OFFERING || !ASSIGNMENT, 'ASSIGNMENTS_OFFERING_ID / ASSIGNMENT_ID are not set');

  test.beforeEach(async ({ page }) => {
    page.on('dialog', (d) => d.accept());
  });

  test('teacher sets the late policy and builds a group set', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await loginAdmin(page);
    await page.goto(`/Portal/AssignmentPolicies?offeringId=${OFFERING}`);
    const policy = page.locator('[data-late-policy]');
    await expect(policy).toBeVisible();

    await policy.locator('#lp-late').check();
    await policy.locator('#lp-percent').fill('15');
    await policy.locator('#lp-interval').selectOption('Day');
    await policy.locator('#lp-floor').fill('40');
    await policy.locator('button', { hasText: 'Save policy' }).click();
    await expect(flash(page)).toContainText('Late and missing policy saved.');
    await expect(page.locator('[data-late-policy]')).toContainText('Late work loses 15% of the points per day late');

    const setName = `PW Teams ${RUN}`;
    const create = page.locator('form[action*="CreateGroupSet"]');
    await create.locator('input[name="name"]').fill(setName);
    await create.locator('input[name="maxSize"]').fill('3');
    await create.locator('button', { hasText: 'Create' }).click();
    await expect(flash(page)).toContainText(`Group set “${setName}” created.`);
    const set = page.locator(`[data-group-set="${setName}"]`);
    await expect(set).toContainText('Up to 3 per group');

    await set.locator('form[action*="CreateStudentGroup"] input[name="name"]').fill('PW Group');
    await set.locator('form[action*="CreateStudentGroup"] button').click();
    await expect(page.locator(`[data-group-set="${setName}"] [data-group="PW Group"]`)).toBeVisible();

    // The rest of the class goes into groups at random.
    const split = page.locator(`[data-group-set="${setName}"] form[action*="SplitGroupSet"]`);
    await split.locator('select[name="splitBy"]').selectOption('size');
    await split.locator('input[name="splitValue"]').fill('2');
    await split.locator('button').click();
    await expect(flash(page)).toContainText(/split into groups|already in a group/);
    await shot(page, 'policies');

    await page.locator(`[data-group-set="${setName}"] form[action*="DeleteGroupSet"] button`).click();
    await expect(flash(page)).toContainText('Group set deleted.');
    await expect(page.locator(`[data-group-set="${setName}"]`)).toHaveCount(0);
    expect(errors).toEqual([]);
  });

  test('teacher gives one student their own dates', async ({ page }) => {
    await loginAdmin(page);
    await page.goto(`/Portal/AssignmentOptions?assignmentId=${ASSIGNMENT}`);
    await expect(page.locator('[data-assignment-options]')).toBeVisible();

    const add = page.locator('form[action*="AddAssignmentOverride"]');
    const student = await add.locator('#ov-target optgroup[label="Students"] option').first().getAttribute('value');
    await add.locator('#ov-target').selectOption(student!);
    await add.locator('#ov-due').fill('2030-01-15T10:00');
    await add.locator('#ov-until').fill('2030-01-17T10:00');
    await add.locator('#ov-note').fill(`PW extension ${RUN}`);
    await add.locator('button', { hasText: 'Add dates' }).click();
    await expect(flash(page)).toContainText('Dates added.');
    const row = page.locator('[data-overrides] tr', { hasText: `PW extension ${RUN}` });
    await expect(row).toContainText('Due 15 Jan 2030 10:00 UTC');
    await expect(row).toContainText('Late work until 17 Jan 2030 10:00 UTC');
    await shot(page, 'options');

    // A late cut-off before the due date is refused with the API's message.
    await page.locator('#ao-until').fill('2000-01-01T00:00');
    await page.locator('[data-assignment-options] button', { hasText: 'Save options' }).click();
    await expect(flash(page)).toContainText('cannot be before the due date');

    await row.locator('button', { hasText: 'Remove' }).click();
    await expect(flash(page)).toContainText('Dates removed.');
    await expect(page.locator('[data-overrides] tr', { hasText: `PW extension ${RUN}` })).toHaveCount(0);
  });

  test('grading workspace shows late deductions, group work and peer averages', async ({ page }) => {
    test.skip(!DEMO, 'M8_DEMO_DATA is not set');
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await loginAdmin(page);
    await page.goto(`/Portal/GradingWorkspace?assignmentId=${ASSIGNMENT}`);
    await expect(page.locator('a', { hasText: 'Options & dates' })).toBeVisible();

    const late = page.locator('#ws-list [data-submission]', { has: page.locator('.badge', { hasText: 'late' }).filter({ hasText: '−' }) }).first();
    await late.click();
    await expect(page.locator('#ws-penalty')).toContainText('Late deduction −2');
    await page.locator('#ws-waive').check();
    await expect(page.locator('#ws-waive')).toBeChecked();
    await expect(page.locator('#ws-penalty')).toHaveText('');
    await page.locator('#ws-waive').uncheck();
    await expect(page.locator('#ws-penalty')).toContainText('Late deduction −2');

    const group = page.locator('#ws-list [data-submission]').first();
    await group.click();
    await expect(page.locator('#ws-panel')).toContainText('Group: Team A');
    await page.locator('#ws-peer button', { hasText: 'Use peer average' }).click();
    await expect(page.locator('#ws-marks')).toHaveValue('15.5');
    await shot(page, 'workspace');
    expect(errors).toEqual([]);
  });

  test('student sees their dates, reviews a classmate, joins a group and posts to it', async ({ page }) => {
    test.skip(!process.env.STUDENT_USERNAME, 'STUDENT_USERNAME is not set');
    await login(page, process.env.STUDENT_USERNAME, process.env.STUDENT_PASSWORD);

    await page.goto(`/Portal/AssignmentSubmission?assignmentId=${ASSIGNMENT}`);
    await expect(page.locator('[data-assignment-extras]')).toBeVisible();
    const reviews = page.locator('[data-peer-reviews]');
    if (await reviews.count()) {
      await shot(page, 'student-assignment');
      const link = reviews.locator('a', { hasText: /^(Review|Edit)$/ }).first();
      if (await link.count()) {
        await link.click();
        await expect(page.locator('[data-peer-work]')).toBeVisible();
        const form = page.locator('[data-peer-form]');
        for (const fieldset of await form.locator('fieldset').all()) await fieldset.locator('input[type="radio"]').last().check();
        if (await form.locator('#pr-score').count()) await form.locator('#pr-score').fill('15');
        await form.locator('#pr-comment').fill(`Clear argument — PW ${RUN}`);
        await shot(page, 'peer-review');
        await form.locator('button[type="submit"], button').last().click();
        await expect(flash(page)).toContainText('Your review has been saved.');
        await expect(page).toHaveURL(/AssignmentSubmission/);
      }
    }

    await page.goto(`/Portal/MyGroups?offeringId=${OFFERING}`);
    const join = page.locator('form[action*="JoinGroup"] button').first();
    if (await join.count()) {
      await join.click();
      await expect(flash(page)).toContainText('You joined the group.');
      await shot(page, 'my-groups');
      const leave = page.locator('form[action*="LeaveGroup"] button').first();
      await leave.click();
      await expect(flash(page)).toContainText('You left the group.');
    }

    await page.goto(`/Portal/Discussion?offeringId=${OFFERING}`);
    const target = page.locator('select[name="groupId"]');
    if (await target.count()) {
      const group = await target.locator('option').nth(1).getAttribute('value');
      await page.locator('input[name="title"]').fill(`PW group thread ${RUN}`);
      await target.selectOption(group!);
      await page.locator('form[action*="CreateDiscussionThread"] button[type="submit"]').click();
      const item = page.locator('.list-group-item', { hasText: `PW group thread ${RUN}` });
      await expect(item.locator('[data-group-thread]')).toBeVisible();
      await shot(page, 'group-discussion');
    }
  });
});
