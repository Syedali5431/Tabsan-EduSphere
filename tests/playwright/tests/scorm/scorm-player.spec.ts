/**
 * scorm-player.spec.ts — Plan M13 SCORM player in a real browser. @scorm
 *
 * A student opens a SCORM 1.2 and a SCORM 2004 package; the package (in the player's frame) finds the runtime
 * (scorm-again) in its parent, reads the learner, reports a score and status, commits and finishes. The commits must
 * reach the API through the portal's /scorm/play/{token}/commit relay, and the portal page shows "progress saved".
 *
 * Needs: STUDENT_USERNAME / STUDENT_PASSWORD, and an API that serves the package and records commits:
 *   SCORM_PACKAGE_12, SCORM_PACKAGE_2004 — package ids; SCORM_COMMITS_URL — e.g. http://localhost:5198/fake/scorm/commits
 *   (the local fake API used for portal UI tests). Optional: SCREENSHOT_DIR.
 */
import { test, expect, Page, FrameLocator } from '@playwright/test';

const SHOTS = process.env.SCREENSHOT_DIR;
const USER = process.env.STUDENT_USERNAME;
const PASS = process.env.STUDENT_PASSWORD;
const COMMITS = process.env.SCORM_COMMITS_URL;
const PACKAGE_12 = process.env.SCORM_PACKAGE_12 ?? 'c1000000-0000-0000-0000-000000000012';
const PACKAGE_2004 = process.env.SCORM_PACKAGE_2004 ?? 'c1000000-0000-0000-0000-000000002004';

async function login(page: Page) {
  await page.goto('/Login?lang=en');
  await page.fill('#username', USER ?? '');
  await page.fill('#password', PASS ?? '');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.toLowerCase().endsWith('/login'), { timeout: 15000 });
}

function sco(page: Page): FrameLocator {
  // portal page → player shell → the package
  return page.frameLocator('iframe.scorm-frame').frameLocator('#scorm-content');
}

async function commitsFor(page: Page): Promise<Array<{ final: string; body: string }>> {
  const src = await page.locator('iframe.scorm-frame').getAttribute('src');
  const token = /\/scorm\/play\/([^/]+)\//.exec(src ?? '')?.[1];
  expect(token, 'play link in the frame').toBeTruthy();
  const response = await page.request.get(`${COMMITS}/${token}`);
  return response.json();
}

async function play(page: Page, packageId: string, name: string) {
  await page.goto(`/Portal/Scorm/${packageId}`);
  await page.getByRole('button', { name: 'Start' }).click();
  await page.waitForURL(/\/Portal\/ScormPlay\//);
  const out = sco(page).locator('#out');
  await expect(out).toContainText('done', { timeout: 20000 });
  await expect(page.locator('[data-scorm-finished]')).toBeVisible();
  if (SHOTS) await page.screenshot({ path: `${SHOTS}/scorm-${name}.png`, fullPage: true });
  return out.textContent();
}

test.describe('SCORM player @scorm', () => {
  test.skip(!USER || !COMMITS, 'STUDENT_USERNAME and SCORM_COMMITS_URL are needed');

  test('a SCORM 1.2 package reports its score and status through the runtime', async ({ page }) => {
    await login(page);
    const text = await play(page, PACKAGE_12, '12');
    expect(text).toContain('student=R-12');
    expect(text).toContain('entry=ab-initio');
    expect(text).toContain('commit=true');

    await expect.poll(async () => (await commitsFor(page)).length).toBeGreaterThan(0);
    const commits = await commitsFor(page);
    const last = JSON.parse(commits[commits.length - 1].body);
    const core = (last.cmi ?? last).core;
    expect(core.lesson_status).toBe('passed');
    expect(String(core.score.raw)).toBe('85');
    expect((last.cmi ?? last).suspend_data).toBe('page=1');
    expect(commits.some((c) => c.final === 'true')).toBeTruthy(); // the finish commit is marked final
  });

  test('a SCORM 2004 package reports its scaled score and statuses', async ({ page }) => {
    await login(page);
    const text = await play(page, PACKAGE_2004, '2004');
    expect(text).toContain('learner=R-2004');
    expect(text).toContain('mode=normal');
    expect(text).toContain('commit=true');

    await expect.poll(async () => (await commitsFor(page)).length).toBeGreaterThan(0);
    const commits = await commitsFor(page);
    const last = JSON.parse(commits[commits.length - 1].body);
    const cmi = last.cmi ?? last;
    expect(cmi.completion_status).toBe('completed');
    expect(cmi.success_status).toBe('passed');
    expect(String(cmi.score.scaled)).toBe('0.9');
    expect(cmi.location).toBe('q4');
  });

  test('the shell is not reachable with a made-up link', async ({ page }) => {
    const response = await page.goto('/scorm/play/not-a-real-token/');
    expect(response?.status()).toBe(200);
    await expect(page.locator('body')).toContainText('expired');
  });
});
