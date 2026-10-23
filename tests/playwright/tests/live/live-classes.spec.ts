/**
 * live-classes.spec.ts — Plan M10 live classes. @live
 *
 * Needs:
 *   LIVE_OFFERING_ID                           a course offering the admin can manage (skipped when unset)
 *   SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD (or ADMIN_USERNAME / ADMIN_PASSWORD)
 *   STUDENT_USERNAME / STUDENT_PASSWORD        a student enrolled in that offering (student steps skipped when unset)
 * Optional: SCREENSHOT_DIR; M10_DEMO_DATA=1 adds checks that need a class open for joining, an ended class with joins and
 * a timetable slot (the preview data has them). The spec schedules a class named "PW … <run>" and cancels it.
 */
import { test, expect, Page } from '@playwright/test';
import { randomUUID } from 'crypto';

const OFFERING = process.env.LIVE_OFFERING_ID;
const SHOTS = process.env.SCREENSHOT_DIR;
const DEMO = process.env.M10_DEMO_DATA === '1';
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
  await page.screenshot({ path: `${SHOTS}/live-${name}.png`, fullPage: true });
}

const flash = (page: Page) => page.locator('.alert[role="status"]').first();

/** "yyyy-MM-ddTHH:mm" in UTC, a few days ahead. */
function utcInput(daysAhead: number) {
  const d = new Date(Date.now() + daysAhead * 86400000);
  return d.toISOString().slice(0, 16);
}

test.describe('Live classes @live', () => {
  test.describe.configure({ mode: 'serial' });
  test.skip(!OFFERING, 'LIVE_OFFERING_ID is not set');

  test.beforeEach(async ({ page }) => {
    page.on('dialog', (d) => d.accept());
  });

  test('admin connects a conferencing service', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Portal/LiveClassSettings');
    const form = page.locator('[data-conference-settings]');
    await expect(form).toBeVisible();
    await form.locator('#cf-provider').selectOption('BigBlueButton');
    await form.locator('#cf-base').fill('https://bbb.example.edu/bigbluebutton/api');
    await form.locator('#cf-early').fill('15');
    await form.locator('button', { hasText: 'Save' }).click();
    await expect(flash(page)).toContainText('Live class settings saved.');
    await expect(page.locator('#cf-secret')).toHaveValue('');
    await page.locator('form[action*="TestLiveClassSettings"] button').click();
    await expect(flash(page)).toContainText('Connected');
    await shot(page, 'settings');
  });

  test('teacher schedules, edits and cancels a class, and schedules timetable weeks', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await loginAdmin(page);
    await page.goto(`/Portal/LiveClasses?offeringId=${OFFERING}`);
    const form = page.locator('[data-schedule-form]');
    await form.locator('#ls-title').fill(`PW class ${RUN}`);
    await form.locator('#ls-start').fill(utcInput(4));
    await form.locator('#ls-duration').fill('45');
    await form.locator('#ls-min').fill('10');
    await form.locator('button', { hasText: 'Schedule' }).click();
    await expect(flash(page)).toContainText('Live class scheduled');
    const item = page.locator(`[data-session="PW class ${RUN}"]`);
    await expect(item).toContainText('Upcoming');

    await item.locator('a', { hasText: 'Edit' }).click();
    await page.locator('#ls-title').fill(`PW class ${RUN} (moved)`);
    await page.locator('[data-schedule-form] button', { hasText: 'Save' }).click();
    await expect(flash(page)).toContainText('Live class saved.');
    const moved = page.locator(`[data-session="PW class ${RUN} (moved)"]`);
    await expect(moved).toBeVisible();
    await shot(page, 'teacher');

    // The download is an iCalendar file.
    const download = page.waitForEvent('download');
    await moved.locator('a', { hasText: 'Add to calendar' }).click();
    expect((await download).suggestedFilename()).toMatch(/\.ics$/);

    await moved.locator('form[action*="CancelLiveSession"] button').click();
    await expect(flash(page)).toContainText('cancelled');
    await expect(page.locator(`[data-session="PW class ${RUN} (moved)"]`)).toContainText('Cancelled');

    if (DEMO) {
      const weeks = page.locator('[data-timetable-form]');
      await weeks.locator('#lt-weeks').fill('2');
      await weeks.locator('button', { hasText: 'Schedule weeks' }).click();
      await expect(flash(page)).toContainText(/scheduled from the timetable|already scheduled/);
    }
    expect(errors).toEqual([]);
  });

  test('teacher reads the attendance of an ended class', async ({ page }) => {
    test.skip(!DEMO, 'M10_DEMO_DATA is not set');
    await loginAdmin(page);
    await page.goto(`/Portal/LiveClasses?offeringId=${OFFERING}`);
    await page.locator('[data-session="Week 5 recap"] a', { hasText: 'Attendance' }).click();
    const table = page.locator('[data-live-attendance]');
    await expect(table.locator('[data-student="Ayesha Malik"]')).toContainText('Present');
    await expect(table.locator('[data-student="Sara Ahmed"]')).toContainText('Absent');
    await page.locator('form[action*="TakeLiveAttendance"] button').click();
    await expect(flash(page)).toContainText('Attendance marked for 3 student(s)');
    await shot(page, 'attendance');
  });

  test('student joins a class that is on now from the banner', async ({ page, context }) => {
    test.skip(!process.env.STUDENT_USERNAME || !DEMO, 'STUDENT_USERNAME / M10_DEMO_DATA is not set');
    await login(page, process.env.STUDENT_USERNAME, process.env.STUDENT_PASSWORD);
    await page.goto(`/Portal/LiveClasses?offeringId=${OFFERING}`);
    await expect(page.locator('[data-schedule-form]')).toHaveCount(0);
    const banner = page.locator('[data-live-banner]');
    await expect(banner).toContainText('Week 6 lab');
    await shot(page, 'student');
    const popup = context.waitForEvent('page');
    await banner.locator('button', { hasText: 'Join' }).first().click();
    const room = await popup;
    await room.waitForLoadState();
    await expect(room.locator('#room')).toContainText('Meeting room');
  });
});
