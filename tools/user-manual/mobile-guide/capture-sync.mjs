// Section 9.2 of the guide: the announcement a teacher posted from the phone, seen in the student's portal.
// usage: node capture-sync.mjs <portalBaseUrl> <password> "<announcement title>"
import { chromium } from 'playwright-core';
const [, , PORTAL, PASS, TITLE] = process.argv;
const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new', '--hide-scrollbars'] });
const page = await (await browser.newContext({ viewport: { width: 1366, height: 820 }, deviceScaleFactor: 1.5, colorScheme: 'light' })).newPage();
await page.goto(`${PORTAL}/Login`);
await page.fill('#username', 'tmsgul.c9s1');
await page.fill('#password', PASS);
await Promise.all([page.waitForNavigation().catch(() => {}), page.click('button[type=submit]')]);
for (const path of ['/Portal/Notifications', '/Portal/Announcements']) {
  await page.goto(PORTAL + path);
  await page.waitForLoadState('networkidle').catch(() => {});
  for (const close of await page.locator('.btn-close:visible').all()) await close.click().catch(() => {});
  const hit = page.locator(`text=${TITLE}`).first();
  if (await hit.count()) {
    await hit.scrollIntoViewIfNeeded();
    // frame the item (screenshot only)
    await hit.evaluate(el => { const box = el.closest('li, tr, .card, .list-group-item, .notification-item') || el; box.style.outline = '3px solid #DC2626'; box.style.outlineOffset = '4px'; box.style.borderRadius = '8px'; });
    await page.evaluate(() => window.scrollBy(0, -200));
    await page.waitForTimeout(500);
    await page.screenshot({ path: 'shots/portal/p07-announcement-in-portal.png' });
    console.log('found on', path);
    break;
  }
  console.log('not on', path, '| page text starts:', (await page.locator('main').innerText().catch(() => '')).replace(/\s+/g, ' ').slice(0, 300));
}
await browser.close();
