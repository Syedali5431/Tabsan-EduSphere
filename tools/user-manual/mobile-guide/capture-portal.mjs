// Portal screenshots for the mobile app connection guide.
// usage: node capture-portal.mjs <portalBaseUrl> <apiBaseUrl> <password>
//   e.g. node capture-portal.mjs http://localhost:5064 http://localhost:5182 <demo password>
// Signs in as superadmin; nothing is saved. Images go to shots/portal/.
import { chromium } from 'playwright-core';
import fs from 'fs';

const [, , PORTAL = 'http://localhost:5064', API = 'http://localhost:5182', PASS] = process.argv;
if (!PASS) throw new Error('usage: node capture-portal.mjs <portalBaseUrl> <apiBaseUrl> <password>');
const OUT = 'shots/portal';
fs.mkdirSync(OUT, { recursive: true });

const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new', '--hide-scrollbars'] });

async function session(user) {
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 820 }, deviceScaleFactor: 1.5, colorScheme: 'light' });
  const page = await ctx.newPage();
  await page.goto(`${PORTAL}/Login`);
  await page.fill('#username', user);
  await page.fill('#password', PASS);
  await Promise.all([page.waitForNavigation().catch(() => {}), page.click('button[type=submit]')]);
  await page.waitForLoadState('networkidle').catch(() => {});
  return { ctx, page };
}

// A red frame around what the step talks about (screenshots only; nothing is changed on the server).
async function highlight(page, selector) {
  await page.evaluate(sel => {
    for (const el of document.querySelectorAll(sel)) {
      el.style.outline = '3px solid #DC2626';
      el.style.outlineOffset = '3px';
      el.style.borderRadius = '6px';
    }
  }, selector);
}

async function shot(page, name, clip) {
  await page.waitForLoadState('networkidle').catch(() => {});
  await page.waitForTimeout(500);
  await page.screenshot({ path: `${OUT}/${name}.png`, ...(clip ? { clip } : {}) });
  console.log('saved', name);
}

// 1. The portal's sign-in page (same account as the app).
{
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 820 }, deviceScaleFactor: 1.5, colorScheme: 'light' });
  const page = await ctx.newPage();
  await page.goto(`${PORTAL}/Login`);
  await shot(page, 'p01-portal-login');
  await ctx.close();
}

// 2. Branding: the institution name the app shows on its sign-in screen.
{
  const { ctx, page } = await session('superadmin');
  await page.goto(`${PORTAL}/Portal/DashboardSettings`);
  await highlight(page, 'input[name="Branding.UniversityName"], input[name="Branding.PortalSubtitle"]');
  await page.locator('input[name="Branding.UniversityName"]').scrollIntoViewIfNeeded();
  await page.evaluate(() => window.scrollBy(0, -160));
  await shot(page, 'p02-branding');

  // 3. User import: the Parent role in the single-user form (nothing is submitted).
  await page.goto(`${PORTAL}/Portal/UserImport`);
  await shot(page, 'p03-user-import');
  // close the page's help tip, choose Parent and photograph just the "Create Single User" card
  for (const close of await page.locator('.btn-close:visible, [aria-label="Close"]:visible').all()) await close.click().catch(() => {});
  await page.selectOption('#singleUserRole', 'Parent');
  await page.fill('input[name="Username"]', 'parent.imran');
  await page.fill('input[name="Email"]', 'imran.sheikh@example.com');
  await page.fill('input[name="FullName"]', 'Imran Sheikh');
  await highlight(page, '#singleUserRole');
  const card = page.locator('.card:has(#singleUserRole)').first();
  await card.scrollIntoViewIfNeeded();
  await page.waitForTimeout(400);
  await card.screenshot({ path: `${OUT}/p04-single-user-parent.png` });
  console.log('saved p04-single-user-parent');
  await ctx.close();
}

// 4. The app's configuration as the server sends it (what IT checks after setting up).
{
  const ctx = await browser.newContext({ viewport: { width: 1100, height: 760 }, deviceScaleFactor: 1.5, colorScheme: 'light' });
  const page = await ctx.newPage();
  // the real response, laid out readably under the address it came from
  const url = `${API}/api/v1/mobile/config`;
  const json = await (await page.request.get(url)).json();
  await page.setContent(`<body style="margin:0;font:15px Consolas,monospace;background:#fff">
    <div style="background:#F1F5F9;border-bottom:1px solid #CBD5E1;padding:10px 16px;font:14px Segoe UI,sans-serif;color:#334155">GET <b>${url}</b> &nbsp;→&nbsp; 200 OK</div>
    <pre style="margin:0;padding:14px 18px;color:#0F2A4A">${JSON.stringify(json, null, 2).replace(/</g, '&lt;')}</pre></body>`);
  const height = await page.evaluate(() => document.body.scrollHeight);
  await shot(page, 'p06-mobile-config', { x: 0, y: 0, width: 760, height: Math.min(height + 10, 760) });
  await ctx.close();
}

await browser.close();
