// Demo data for the guide: creates the parent account parent.imran (Imran Sheikh) through the portal's
// User Import › Create Single User form, as section 4.3 describes. Run once against the test portal.
// usage: node create-parent.mjs <portalBaseUrl> <superadmin password>
import { chromium } from 'playwright-core';
const [, , PORTAL, PASS] = process.argv;
const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new'] });
const page = await (await browser.newContext()).newPage();
await page.goto(`${PORTAL}/Login`);
await page.fill('#username', 'superadmin');
await page.fill('#password', PASS);
await Promise.all([page.waitForNavigation().catch(() => {}), page.click('button[type=submit]')]);
await page.goto(`${PORTAL}/Portal/UserImport`);
await page.fill('input[name="Username"]', 'parent.imran');
await page.fill('input[name="Email"]', 'imran.sheikh@example.com');
await page.fill('input[name="FullName"]', 'Imran Sheikh');
await page.selectOption('#singleUserRole', 'Parent');
await Promise.all([page.waitForNavigation().catch(() => {}), page.locator('.card:has(#singleUserRole) button[type=submit]').first().click()]);
await page.waitForLoadState('networkidle').catch(() => {});
const messages = await page.locator('.alert, .toast-body, [role=alert]').allInnerTexts();
console.log(messages.map(m => m.replace(/\s+/g, ' ').trim()).filter(Boolean).join('\n') || '(no message shown)');
await browser.close();
