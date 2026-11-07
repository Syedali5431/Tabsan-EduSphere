// Portal, API and database screenshots for the Installation and Connection Guide (the "Northfield Model School" example).
// usage: node capture-portal.mjs <portal> <api> <apiAddressForPhones> <superadmin password> [sqlServer]
//   e.g. node capture-portal.mjs http://localhost:5163 http://localhost:5281 http://192.168.1.109:5281 <password>
// Saves one thing on the example installation: its branding (institution name and subtitle). Everything else only
// reads; the database setup page is tested ("Test Connection"), never saved. Images go to shots/portal/.
// The setup page (n04) needs SETUP_API: an API without a working database, because /setup is closed (404) otherwise.
import { chromium } from 'playwright-core';
import fs from 'fs';
import { execFileSync } from 'child_process';

const [, , PORTAL, API, PHONE_API, PASS, SQL = 'localhost\\SQLEXPRESS'] = process.argv;
if (!PASS) throw new Error('usage: node capture-portal.mjs <portal> <api> <apiAddressForPhones> <password> [sqlServer]');
const NAME = 'Northfield Model School', SUBTITLE = 'School Portal', DB = 'EduSphere-Northfield';
const SQLCMD = 'C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/180/Tools/Binn/SQLCMD.EXE';
const OUT = 'shots/portal';
fs.mkdirSync(OUT, { recursive: true });

const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new', '--hide-scrollbars'] });
// bypassCSP: the API's Content-Security-Policy forbids the evaluated scripts Playwright uses for waiting and highlighting.
const context = (width = 1366, height = 820) => browser.newContext({ viewport: { width, height }, deviceScaleFactor: 1.5, colorScheme: 'light', bypassCSP: true });

async function signIn(page) {
  await page.goto(`${PORTAL}/Login`);
  await page.fill('#username', 'superadmin');
  await page.fill('#password', PASS);
  await Promise.all([page.waitForNavigation().catch(() => {}), page.click('button[type=submit]')]);
  await page.waitForLoadState('networkidle').catch(() => {});
}

async function highlight(page, selector) {
  await page.evaluate(sel => {
    for (const el of document.querySelectorAll(sel)) { el.style.outline = '3px solid #DC2626'; el.style.outlineOffset = '3px'; el.style.borderRadius = '6px'; }
  }, selector);
}

async function shot(page, name, opts = {}) {
  await page.waitForLoadState('networkidle').catch(() => {});
  await page.waitForTimeout(500);
  await page.screenshot({ path: `${OUT}/${name}.png`, ...opts });
  console.log('saved', name);
}

const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');

/** Console output (real command output or real log lines) laid out readably under what produced it. */
async function consoleFigure(name, heading, lines) {
  // 780 px wide (like the configuration figure), so the text stays readable at the guide's figure width
  const ctx = await context(780, 700);
  const page = await ctx.newPage();
  await page.setContent(`<!doctype html><body style="margin:0;background:#fff">
    <div style="background:#F1F5F9;border-bottom:1px solid #CBD5E1;padding:10px 16px;font:14px Segoe UI,sans-serif;color:#334155">${heading}</div>
    <pre style="margin:0;padding:14px 18px;font:14px Consolas,monospace;line-height:1.5;color:#0F2A4A;white-space:pre-wrap">${esc(lines.join('\n'))}</pre></body>`);
  const height = await page.evaluate(() => document.body.getBoundingClientRect().height);
  await shot(page, name, { clip: { x: 0, y: 0, width: 780, height: Math.min(Math.ceil(height) + 4, 700) } });
  await ctx.close();
}

// 1. Two institutes' databases on one SQL Server (real query output).
{
  const query = "SET NOCOUNT ON; SELECT d.name AS [Database], CONVERT(varchar(16), d.create_date, 120) AS Created, CAST(SUM(f.size) * 8 / 1024 AS int) AS SizeMB FROM sys.databases d JOIN sys.master_files f ON f.database_id = d.database_id WHERE d.database_id > 4 GROUP BY d.name, d.create_date ORDER BY d.create_date";
  const out = execFileSync(SQLCMD, ['-S', SQL, '-E', '-C', '-W', '-s', '|', '-Q', query], { encoding: 'utf8' });
  // line the columns up (sqlcmd -W trims them); drop sqlcmd's dashed separator and draw one to fit
  const rows = out.trim().split(/\r?\n/).filter(r => !/^[-|]+$/.test(r)).map(r => r.split('|'));
  const widths = rows[0].map((_, i) => Math.max(...rows.map(r => (r[i] || '').length)));
  const fmt = r => r.map((c, i) => c.padEnd(widths[i])).join('   ');
  await consoleFigure('n01-databases', `sqlcmd -S <b>${esc(SQL)}</b> -E -C -Q "SELECT … FROM sys.databases"`,
    [fmt(rows[0]), widths.map(w => '-'.repeat(w)).join('   '), ...rows.slice(1).map(fmt)]);
}

// 2-3. The first lines of the example's API and portal (copied from their consoles).
await consoleFigure('n02-api-startup', 'Northfield API — console when it starts', fs.readFileSync('shots/console/api-startup.txt', 'utf8').trim().split(/\r?\n/));
await consoleFigure('n03-portal-startup', 'Northfield portal — console when it starts', fs.readFileSync('shots/console/portal-startup.txt', 'utf8').trim().split(/\r?\n/));

// 4. The API's database setup page with a connection test (not saved). The page is open only while an API has no
// working database (a working one answers 404), so SETUP_API names a second copy of the API started without one
// (README); without it this picture is kept as it is.
if (process.env.SETUP_API) {
  const ctx = await context(1200, 1000);
  const page = await ctx.newPage();
  const response = await page.goto(`${process.env.SETUP_API}/setup`);
  if (response.status() !== 200) throw new Error(`${process.env.SETUP_API}/setup answered ${response.status()}: start that API without a working database`);
  await page.selectOption('#dbType', 'Local').catch(() => {});
  await page.selectOption('#localProvider', 'SqlServerExpress').catch(() => {});
  await page.fill('#serverHost', SQL);
  await page.fill('#databaseName', DB);
  if (!(await page.isChecked('#trustedConnection'))) await page.check('#trustedConnection');
  await page.click('#testConnectionButton');
  for (let i = 0; i < 60 && !((await page.textContent('#testResult')) || '').trim(); i++) await page.waitForTimeout(500);
  await page.evaluate(() => document.querySelector('#testResult').scrollIntoView({ block: 'end' }));
  await highlight(page, '#testResult, #testConnectionButton');
  await shot(page, 'n04-setup-page', { fullPage: true });
  console.log('   test result:', (await page.textContent('#testResult')).trim());
  await ctx.close();
} else {
  console.log('kept n04-setup-page (set SETUP_API to take it again)');
}

// 5. The new institute's portal before anything is set.
{
  const ctx = await context();
  const page = await ctx.newPage();
  await page.goto(`${PORTAL}/Login`);
  await shot(page, 'n05-portal-login-new');
  await ctx.close();
}

// 6-9. First sign-in as superadmin, the empty dashboard, branding (saved), the licence page.
{
  const ctx = await context();
  const page = await ctx.newPage();
  await signIn(page);
  await shot(page, 'n06-dashboard-new');

  await page.goto(`${PORTAL}/Portal/DashboardSettings`);
  await page.fill('input[name="Branding.UniversityName"]', NAME);
  await page.fill('input[name="Branding.PortalSubtitle"]', SUBTITLE);
  await highlight(page, 'input[name="Branding.UniversityName"], input[name="Branding.PortalSubtitle"]');
  await page.locator('input[name="Branding.UniversityName"]').scrollIntoViewIfNeeded();
  await page.evaluate(() => window.scrollBy(0, -160));
  await shot(page, 'n07-branding');
  await Promise.all([page.waitForNavigation().catch(() => {}), page.locator('form:has(input[name="Branding.UniversityName"]) button[type="submit"]').first().click()]);
  await page.waitForLoadState('networkidle').catch(() => {});
  console.log('   branding saved:', await page.locator('input[name="Branding.UniversityName"]').inputValue().catch(() => '?'));

  await page.goto(`${PORTAL}/Portal/LicenseUpdate`);
  await shot(page, 'n09-license');
  await ctx.close();
}

// 8. After a new sign-in (the portal keeps the branding in the session) the pages carry the institute's name;
//    the sign-in page itself always says Tabsan EduSphere.
{
  const ctx = await context();
  const page = await ctx.newPage();
  await signIn(page);
  await page.goto(`${PORTAL}/Portal/Dashboard`);
  await highlight(page, '.brand-link');
  await shot(page, 'n08-dashboard-branded');
  await ctx.close();
}

// 10. What the app reads first: the configuration at the address phones use.
{
  const ctx = await context(1100, 760);
  const page = await ctx.newPage();
  const url = `${PHONE_API}/api/v1/mobile/config`;
  const json = await (await page.request.get(url)).json();
  await page.setContent(`<!doctype html><body style="margin:0;font:15px Consolas,monospace;background:#fff">
    <div style="background:#F1F5F9;border-bottom:1px solid #CBD5E1;padding:10px 16px;font:14px Segoe UI,sans-serif;color:#334155">GET <b>${url}</b> &nbsp;→&nbsp; 200 OK</div>
    <pre style="margin:0;padding:14px 18px;color:#0F2A4A">${esc(JSON.stringify({ ...json, languages: `[${json.languages?.length ?? 0} languages]` }, null, 2))}</pre></body>`);
  const height = await page.evaluate(() => document.body.getBoundingClientRect().height);
  await shot(page, 'n10-mobile-config', { clip: { x: 0, y: 0, width: 760, height: Math.min(Math.ceil(height) + 6, 760) } });
  await ctx.close();
}

await browser.close();
