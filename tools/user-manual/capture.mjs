// usage: node capture.mjs <role-label> <username> <password> [pageFilter]
import { chromium } from 'playwright-core';
import fs from 'fs';

const BASE = 'http://localhost:5063';
const [, , label, user, pass, filter] = process.argv;
const outDir = `out/${label}`;
fs.mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new', '--hide-scrollbars'] });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.5, colorScheme: 'light' });
const page = await ctx.newPage();

async function shot(name, opts = {}) {
  await page.waitForLoadState('networkidle').catch(() => {});
  await page.waitForTimeout(400);
  const file = `${outDir}/${name}.png`;
  await page.screenshot({ path: file, fullPage: false, ...opts });
  return file;
}

async function describe() {
  return await page.evaluate(() => {
    const txt = el => (el.innerText || el.value || '').replace(/\s+/g, ' ').trim();
    const main = document.querySelector('main') || document.body;
    const vis = el => !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length);
    return {
      title: document.title,
      headings: [...main.querySelectorAll('h1,h2,h3,h4,h5,.card-title,.card-header')].filter(vis).map(txt).filter(Boolean).slice(0, 40),
      labels: [...main.querySelectorAll('label')].filter(vis).map(txt).filter(Boolean).slice(0, 60),
      buttons: [...main.querySelectorAll('button, input[type=submit], a.btn')].filter(vis).map(txt).filter(Boolean).slice(0, 60),
      tableHeaders: [...main.querySelectorAll('table')].filter(vis).map(t => [...t.querySelectorAll('th')].map(txt)).slice(0, 6),
      alerts: [...main.querySelectorAll('.alert')].filter(vis).map(txt).slice(0, 6),
      tabs: [...main.querySelectorAll('.nav-tabs a, .nav-pills a, [role=tab]')].filter(vis).map(txt).slice(0, 20),
      text: txt(main).slice(0, 1500),
    };
  });
}

const meta = { role: label, user, steps: [], pages: [] };

// ── Login steps ──
await page.goto(`${BASE}/Login`);
meta.steps.push({ name: '01-login-page', file: await shot('01-login-page'), info: await describe() });
await page.fill('#username', user);
await page.fill('#password', pass);
meta.steps.push({ name: '02-login-filled', file: await shot('02-login-filled') });
await Promise.all([page.waitForNavigation().catch(() => {}), page.click('button[type=submit]')]);
meta.steps.push({ name: '03-after-login', url: page.url(), file: await shot('03-after-login'), info: await describe() });

// ── Sidebar structure ──
const sidebar = await page.evaluate(() => {
  const nav = document.querySelector('nav, aside, .sidebar, #sidebar') || document.body;
  const items = [];
  let group = '';
  nav.querySelectorAll('*').forEach(el => {
    const cls = (el.className && el.className.toString()) || '';
    if (/(menu-label|sidebar-heading|nav-header|menu-header|group-title|sidebar-section|collapse-toggle|nav-group)/i.test(cls) && el.children.length <= 3) {
      const t = (el.innerText || '').trim().split('\n')[0];
      if (t && t.length < 40) group = t;
    }
    if (el.tagName === 'A') {
      const href = el.getAttribute('href') || '';
      const m = /^\/Portal\/([A-Za-z]+)/.exec(href);
      if (m) items.push({ group, action: m[1], text: (el.innerText || '').replace(/\s+/g, ' ').trim(), href });
    }
  });
  const seen = new Set();
  return items.filter(i => !seen.has(i.action) && seen.add(i.action));
});
meta.sidebar = sidebar;

// sidebar expanded screenshot
meta.steps.push({ name: '04-sidebar', file: await shot('04-sidebar', { clip: { x: 0, y: 0, width: 360, height: 900 } }) });

// profile menu
const toggle = page.locator('[aria-label="Open profile menu"], [data-bs-toggle=dropdown]').first();
if (await toggle.count()) {
  await toggle.click().catch(() => {});
  meta.steps.push({ name: '05-profile-menu', file: await shot('05-profile-menu') });
  await page.keyboard.press('Escape').catch(() => {});
}

// ── Every sidebar page ──
let i = 10;
for (const item of sidebar) {
  if (filter && !new RegExp(filter, 'i').test(item.action)) continue;
  try {
    const resp = await page.goto(BASE + item.href, { timeout: 60000 });
    const name = `${String(i++).padStart(3, '0')}-${item.action}`;
    const h = await page.evaluate(() => document.documentElement.scrollHeight);
    const file = await shot(name, { fullPage: true, clip: { x: 0, y: 0, width: 1440, height: Math.min(Math.max(h, 900), 1700) } });
    const info = await describe();
    meta.pages.push({ ...item, status: resp?.status(), finalUrl: page.url().replace(BASE, ''), file, info });
  } catch (e) {
    meta.pages.push({ ...item, error: String(e) });
  }
}

fs.writeFileSync(`${outDir}/meta.json`, JSON.stringify(meta, null, 2));
await browser.close();
console.log(label, 'sidebar', sidebar.length, 'pages', meta.pages.length, 'errors', meta.pages.filter(p => p.error || (p.status >= 400)).length);
