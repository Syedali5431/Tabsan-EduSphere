/**
 * pwa.spec.ts — Plan M4 installable app, offline reading and sign-out wipe. @pwa
 *
 * Needs STUDENT_USERNAME / STUDENT_PASSWORD. Runs against any deployment (http://localhost or https).
 * Lighthouse 12 no longer has PWA audits, so installability and offline behaviour are checked here.
 */
import { test, expect, Page } from '@playwright/test';

async function login(page: Page) {
  await page.goto('/Login');
  await page.fill('#username', process.env.STUDENT_USERNAME ?? '');
  await page.fill('#password', process.env.STUDENT_PASSWORD ?? '');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.toLowerCase().endsWith('/login'), { timeout: 15000 });
}

test.describe('Progressive web app @pwa', () => {
  test('manifest is installable and its icons load', async ({ request }) => {
    const res = await request.get('/manifest.webmanifest');
    expect(res.ok()).toBeTruthy();
    expect(res.headers()['content-type']).toContain('application/manifest+json');
    const m = await res.json();
    expect(m.display).toBe('standalone');
    expect(m.short_name.length).toBeLessThanOrEqual(12);
    expect(m.start_url).toMatch(/^\/Portal\//);
    const sizes = m.icons.map((i: any) => i.sizes);
    expect(sizes).toContain('192x192');
    expect(sizes).toContain('512x512');
    expect(m.icons.some((i: any) => i.purpose === 'maskable')).toBeTruthy();
    for (const icon of m.icons.filter((i: any) => i.type === 'image/png')) {
      const png = await request.get(icon.src);
      expect(png.ok()).toBeTruthy();
    }
    const sw = await request.get('/sw.js');
    expect(sw.headers()['cache-control']).toBe('no-cache');
  });

  test('saved pages open offline, others show the offline page, sign-out wipes them', async ({ page, context }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await login(page);

    await page.goto('/Portal/Notifications');
    await expect(page.locator('link[rel="manifest"]')).toHaveCount(1);
    expect(await page.evaluate(async () => !!(await navigator.serviceWorker.ready).active)).toBeTruthy();

    // Once the worker controls the page, opening it again keeps an offline copy.
    await page.reload();
    await expect.poll(() => page.evaluate(() => !!navigator.serviceWorker.controller)).toBeTruthy();
    await page.reload();
    await expect.poll(() => page.evaluate(async () => {
      const cache = await caches.open('edusphere-pages-v1');
      return (await cache.keys()).some((r) => new URL(r.url).pathname === '/Portal/Notifications');
    })).toBeTruthy();

    // The push card states this device's status.
    await expect(page.locator('#push-device-card [data-push-status]')).not.toHaveText('Checking…');

    await context.setOffline(true);
    await page.reload();
    await expect(page.locator('#push-device-card')).toBeVisible();
    await expect(page.getByRole('status').filter({ hasText: 'You are offline' })).toBeVisible();

    await page.goto('/Portal/Assignments').catch(() => { /* served by the worker */ });
    await expect(page.getByRole('heading', { name: 'You are offline' })).toBeVisible();
    await context.setOffline(false);

    // Signing out removes saved pages from this device.
    await page.goto('/Portal/Notifications');
    await page.evaluate(() => {
      const form = document.querySelector<HTMLFormElement>('form[action*="Logout" i]');
      form?.requestSubmit();
    });
    await page.waitForURL((url) => /login/i.test(url.pathname), { timeout: 15000 });
    expect(await page.evaluate(async () => (await caches.keys()).includes('edusphere-pages-v1'))).toBeFalsy();
    expect(errors).toEqual([]);
  });
});
