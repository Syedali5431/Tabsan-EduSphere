/**
 * accessibility.spec.ts — Plan M14.1 WCAG 2.1 AA audit with axe-core. @a11y
 *
 * Signs in as each role, visits the pages in portal-pages.json (every page action of the portal, with ids the local fake
 * API knows) plus every page linked from that role's sidebar and the login page, and runs axe with the WCAG 2.0/2.1 A and AA rules. The test fails on "serious" or "critical" violations;
 * all findings are written to a JSON report (A11Y_REPORT, default test-results/a11y-report.json).
 *
 * Needs: SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD and STUDENT_USERNAME / STUDENT_PASSWORD.
 * Optional: A11Y_MAX_PAGES (default 120), A11Y_EXTRA_PAGES (comma-separated paths).
 */
import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import * as fs from 'fs';
import * as path from 'path';
import pagesFile from './portal-pages.json';

const ROLES = [
  { name: 'superadmin', user: process.env.SUPER_ADMIN_USERNAME, pass: process.env.SUPER_ADMIN_PASSWORD, pages: pagesFile.staff },
  { name: 'student', user: process.env.STUDENT_USERNAME, pass: process.env.STUDENT_PASSWORD, pages: pagesFile.student },
];
const MAX_PAGES = Number(process.env.A11Y_MAX_PAGES ?? 120);
const REPORT = process.env.A11Y_REPORT ?? path.join('test-results', 'a11y-report.json');
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'];
const EXTRA = (process.env.A11Y_EXTRA_PAGES ?? '').split(',').map((p) => p.trim()).filter(Boolean);

type Finding = { role: string; page: string; id: string; impact: string; help: string; nodes: string[] };
const findings: Finding[] = [];

async function login(page: Page, user: string, pass: string) {
  await page.goto('/Login?lang=en');
  await page.fill('#username', user);
  await page.fill('#password', pass);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.toLowerCase().endsWith('/login'), { timeout: 15000 });
}

async function sidebarLinks(page: Page): Promise<string[]> {
  const hrefs = await page.locator('aside a[href^="/"], nav a[href^="/"]').evaluateAll((links) =>
    links.map((a) => (a as HTMLAnchorElement).getAttribute('href') ?? ''));
  return [...new Set(hrefs.filter((h) => h.startsWith('/Portal') && !/logout|signout|export|download/i.test(h)))];
}

async function scan(page: Page, role: string, url: string) {
  if (!url.startsWith('/')) return;
  const response = await page.goto(url, { waitUntil: 'domcontentloaded' }).catch(() => null);
  if (!response || response.status() >= 400 || page.url().toLowerCase().includes('/login')) return;
  await page.locator('[data-page-loader]').waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {});
  const results = await new AxeBuilder({ page }).withTags(TAGS).analyze();
  for (const v of results.violations) {
    findings.push({ role, page: url, id: v.id, impact: v.impact ?? 'unknown', help: v.help, nodes: v.nodes.slice(0, 5).map((n) => n.target.join(' ')) });
  }
}

test.describe('Accessibility (WCAG 2.1 AA) @a11y', () => {
  test.describe.configure({ mode: 'serial' });
  test.setTimeout(15 * 60 * 1000);

  test('the login page', async ({ page }) => {
    await scan(page, 'anonymous', '/Login?lang=en');
  });

  for (const role of ROLES) {
    test(`every ${role.name} page in the sidebar`, async ({ page }) => {
      test.skip(!role.user, `${role.name} credentials are not set`);
      await login(page, role.user!, role.pass ?? '');
      const pages = [...new Set([...role.pages, ...(await sidebarLinks(page)), ...EXTRA])].slice(0, MAX_PAGES);
      for (const url of pages) await scan(page, role.name, url);
    });
  }

  test.afterAll(() => {
    fs.mkdirSync(path.dirname(REPORT), { recursive: true });
    const byRule = new Map<string, { impact: string; help: string; pages: Set<string> }>();
    for (const f of findings) {
      const entry = byRule.get(f.id) ?? { impact: f.impact, help: f.help, pages: new Set<string>() };
      entry.pages.add(`${f.role} ${f.page}`);
      byRule.set(f.id, entry);
    }
    const summary = [...byRule.entries()].map(([id, e]) => ({ id, impact: e.impact, help: e.help, pages: [...e.pages] }));
    fs.writeFileSync(REPORT, JSON.stringify({ summary, findings }, null, 2));
    console.log(`axe: ${findings.length} finding(s) in ${byRule.size} rule(s) — ${REPORT}`);
    for (const s of summary) console.log(`  ${s.impact.padEnd(9)} ${s.id} (${s.pages.length} page(s)): ${s.help}`);
  });

  // Keyboard: the skip link comes first and reaches the content; Tab moves through each custom widget's page without
  // getting stuck (no keyboard trap) and every focused element is visible.
  const KEYBOARD_PAGES = [
    '/Portal/Dashboard',
    '/Portal/QuizBuilder?quizId=11111111-0000-0000-0000-000000000001',
    '/Portal/GradingWorkspace?assignmentId=66666666-0000-0000-0000-000000000001',
    '/Portal/QuestionBanks?offeringId=44444444-0000-0000-0000-000000000001&bankId=33333333-0000-0000-0000-000000000001',
    '/Portal/LmsManage?offeringId=44444444-0000-0000-0000-000000000001',
  ];
  test('keyboard: skip link, no traps, visible focus', async ({ page }) => {
    const admin = ROLES[0];
    test.skip(!admin.user, 'superadmin credentials are not set');
    await login(page, admin.user!, admin.pass ?? '');
    for (const url of KEYBOARD_PAGES) {
      await page.goto(url);
      await page.locator('[data-page-loader]').waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {});
      await page.keyboard.press('Tab');
      const first = await page.evaluate(() => (document.activeElement as HTMLElement | null)?.textContent?.trim() ?? '');
      expect(first, `${url}: the first Tab stop`).toMatch(/skip to main content/i);

      const seen: string[] = [];
      let stuck = 0;
      for (let i = 0; i < 60; i++) {
        await page.keyboard.press('Tab');
        const info = await page.evaluate(() => {
          const el = document.activeElement as HTMLElement | null;
          if (!el || el === document.body) return null;
          const r = el.getBoundingClientRect();
          const path = `${el.tagName}#${el.id}.${el.className}|${el.getAttribute('name') ?? ''}|${el.textContent?.trim().slice(0, 30) ?? ''}|${Math.round(r.x)},${Math.round(r.y)}`;
          return { path, visible: r.width > 0 && r.height > 0 };
        });
        if (!info) continue;
        expect(info.visible, `${url}: focused element is visible — ${info.path}`).toBeTruthy();
        stuck = seen.length && seen[seen.length - 1] === info.path ? stuck + 1 : 0;
        expect(stuck, `${url}: focus stays on ${info.path} (keyboard trap)`).toBeLessThan(3);
        seen.push(info.path);
      }
      expect(new Set(seen).size, `${url}: Tab reaches the page's controls`).toBeGreaterThan(5);
    }
  });

  test('no serious or critical violations', async () => {
    const blocking = findings.filter((f) => f.impact === 'serious' || f.impact === 'critical');
    expect(blocking.map((f) => `${f.impact} ${f.id} — ${f.role} ${f.page} — ${f.nodes[0] ?? ''}`)).toEqual([]);
  });
});
