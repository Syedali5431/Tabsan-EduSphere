/**
 * localisation.spec.ts — Plan M11 portal languages (English, Urdu, Arabic) and right-to-left layout. @i18n
 *
 * Needs:
 *   SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD (or ADMIN_USERNAME / ADMIN_PASSWORD)
 * Optional: SCREENSHOT_DIR. The spec switches languages and resets the institution default to English at the end.
 */
import { test, expect, Page } from '@playwright/test';

const SHOTS = process.env.SCREENSHOT_DIR;
const USER = process.env.SUPER_ADMIN_USERNAME ?? process.env.ADMIN_USERNAME;
const PASS = process.env.SUPER_ADMIN_PASSWORD ?? process.env.ADMIN_PASSWORD;

async function shot(page: Page, name: string) {
  if (!SHOTS) return;
  await page.locator('[data-page-loader]').waitFor({ state: 'hidden' }).catch(() => {});
  await page.screenshot({ path: `${SHOTS}/i18n-${name}.png`, fullPage: true });
}

async function choose(page: Page, lang: 'en' | 'ur' | 'ar') {
  await Promise.all([page.waitForNavigation(), page.locator('[data-lang-select]').first().selectOption(lang)]);
  await expect(page.locator('html')).toHaveAttribute('lang', lang);
}

async function login(page: Page) {
  await page.goto('/Login');
  await page.fill('#username', USER ?? '');
  await page.fill('#password', PASS ?? '');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.toLowerCase().endsWith('/login'), { timeout: 15000 });
}

test.describe('Localisation @i18n', () => {
  test.describe.configure({ mode: 'serial' });
  test.skip(!USER, 'SUPER_ADMIN_USERNAME is not set');

  test('the login page switches to Arabic and Urdu, right to left', async ({ page }) => {
    await page.goto('/Login?lang=en');
    await expect(page.locator('html')).toHaveAttribute('dir', 'ltr');
    await choose(page, 'ar');
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    await expect(page.getByRole('heading', { name: 'سجّل الدخول إلى حسابك' })).toBeVisible();
    await expect(page.locator('link[href*="bootstrap.rtl.min.css"]')).toHaveCount(1);
    await shot(page, 'login-ar');
    await choose(page, 'ur');
    await expect(page.getByRole('heading', { name: 'اپنے اکاؤنٹ میں سائن اِن کریں' })).toBeVisible();
    await choose(page, 'en');
    await expect(page.getByRole('heading', { name: 'Sign in to your account' })).toBeVisible();
  });

  test('the portal follows the chosen language: layout, menus and script texts', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await login(page);
    await page.goto('/Portal/LanguageSettings');
    await choose(page, 'ar');
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    await expect(page.locator('h2', { hasText: 'اللغة والترجمات' })).toBeVisible();
    await expect(page.locator('.app-sidebar .menu-link-text', { hasText: /^الحصص المباشرة$/ })).toBeVisible();

    // Right to left: the sidebar sits on the right of the content.
    const sidebar = await page.locator('.app-sidebar').boundingBox();
    const main = await page.locator('.app-main').boundingBox();
    expect(sidebar && main && sidebar.x > main.x).toBeTruthy();

    // Script messages come from the same translations.
    expect(await page.evaluate(() => (window as any).t('Saving…'))).toBe('جارٍ الحفظ…');
    expect(await page.evaluate(() => (window as any).t('Marks out of '))).toBe('الدرجة من ');
    await shot(page, 'portal-ar');

    await choose(page, 'ur');
    await expect(page.locator('h2', { hasText: 'زبان اور تراجم' })).toBeVisible();
    const font = await page.evaluate(() => getComputedStyle(document.body).fontFamily);
    expect(font).toContain('Noto Nastaliq Urdu');
    await shot(page, 'portal-ur');

    // The choice is the user's: it is still Urdu after signing in again.
    await page.locator('details.profile-menu summary').click();
    await page.locator('.profile-menu-signout button').click();
    await page.waitForURL(/\/Login/i);
    await login(page);
    await page.goto('/Portal/LanguageSettings');
    await expect(page.locator('html')).toHaveAttribute('lang', 'ur');
    await choose(page, 'en');
    expect(errors).toEqual([]);
  });

  test('SuperAdmin sets the default language and exports translations', async ({ page }) => {
    await login(page);
    await page.goto('/Portal/LanguageSettings');
    await expect(page.locator('[data-translation-coverage] tr[data-language="ur"]')).toContainText('Urdu');
    const download = page.waitForEvent('download');
    await page.locator('a', { hasText: 'Export to Excel' }).click();
    expect((await download).suggestedFilename()).toMatch(/^edusphere-translations-\d{8}\.xlsx$/);

    await page.locator('#lang-default').selectOption('ar');
    await page.locator('[data-default-language] button').click();
    // This SuperAdmin has not chosen a language, so the new default applies to them too.
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    await expect(page.locator('.alert[role="status"]').first()).toContainText('تم حفظ اللغة الافتراضية.');
    await shot(page, 'settings');

    // New visitors (no cookie, no saved choice) see the default.
    const visitor = await page.context().browser()!.newContext({ baseURL: process.env.BASE_URL });
    const fresh = await visitor.newPage();
    await fresh.goto('/Login');
    await expect(fresh.locator('html')).toHaveAttribute('lang', 'ar');
    await visitor.close();

    await page.locator('#lang-default').selectOption('en');
    await page.locator('[data-default-language] button').click();
    await expect(page.locator('html')).toHaveAttribute('lang', 'en');
    await expect(page.locator('.alert[role="status"]').first()).toContainText('Default language saved.');
  });
});
