// Section 4.4 of the guide: the portal's Parent Links screen (a parent, their children, and a student search to link more).
// usage: node capture-parent-links.mjs <portalBaseUrl> <password> [adminUser] [parentSearch] [studentSearch]
//   e.g. node capture-parent-links.mjs http://localhost:5064 <demo password> admin.tmsgul parent.imran Sheikh
// Signs in as a campus admin; nothing is saved. The image goes to shots/portal/p08-parent-links.png.
import { chromium } from 'playwright-core';
const [, , PORTAL, PASS, USER = 'admin.tmsgul', PARENT = 'parent.imran', STUDENT = 'Sheikh'] = process.argv;
if (!PORTAL || !PASS) throw new Error('usage: node capture-parent-links.mjs <portalBaseUrl> <password> [adminUser] [parentSearch] [studentSearch]');
const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new', '--hide-scrollbars'] });
const page = await (await browser.newContext({ viewport: { width: 1366, height: 1250 }, deviceScaleFactor: 1.5, colorScheme: 'light' })).newPage();
await page.goto(`${PORTAL}/Login`);
await page.fill('#username', USER);
await page.fill('#password', PASS);
await Promise.all([page.waitForNavigation().catch(() => {}), page.click('button[type=submit]')]);

// Find the parent and open their row (as an administrator would), then search for students to link.
await page.goto(`${PORTAL}/Portal/ParentLinks?parentSearch=${encodeURIComponent(PARENT)}`);
const parent = page.locator('.list-group-item').first();
if (!(await parent.count()))
  throw new Error(`no parent account matches "${PARENT}" on ${page.url()}: ${(await page.locator('main').innerText().catch(() => '')).replace(/\s+/g, ' ').slice(0, 300)}`);
await Promise.all([page.waitForNavigation().catch(() => {}), parent.click()]);
await page.fill('#student-search', STUDENT);
await Promise.all([page.waitForNavigation().catch(() => {}), page.click('form[role=search]:has(#student-search) button')]);
await page.waitForLoadState('networkidle').catch(() => {});
for (const close of await page.locator('.btn-close:visible').all()) await close.click().catch(() => {});

// Frame the two steps (screenshot only) and photograph the page from its heading.
await page.evaluate(() => {
  for (const el of document.querySelectorAll('#parent-search, #student-search')) {
    el.style.outline = '3px solid #DC2626';
    el.style.outlineOffset = '3px';
    el.style.borderRadius = '6px';
  }
});
await page.locator('h1').first().scrollIntoViewIfNeeded();
await page.evaluate(() => window.scrollBy(0, -16));
await page.waitForTimeout(500);
await page.screenshot({ path: 'shots/portal/p08-parent-links.png' });
console.log('saved p08-parent-links from', page.url());
await browser.close();
