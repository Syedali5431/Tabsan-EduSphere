// Step-by-step workflow captures. usage: node flows.mjs <flowName|all>
import { chromium } from 'playwright-core';
import fs from 'fs';

const BASE = 'http://localhost:5063';
const only = process.argv[2] || 'all';
const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new', '--hide-scrollbars'] });
const results = {};

async function session(user) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.5 });
  const page = await ctx.newPage();
  await page.goto(`${BASE}/Login`);
  await page.fill('#username', user);
  await page.fill('#password', 'EduSphere147');
  await Promise.all([page.waitForNavigation().catch(() => {}), page.click('button[type=submit]')]);
  return { ctx, page };
}

function flow(name, user, steps) { return { name, user, steps }; }

const ctl = (p, label, kind = "select or self::input or self::textarea") => {
  const lit = JSON.stringify(label);
  return p.locator(`xpath=(//label[normalize-space(.)=${lit}] | //label[starts-with(normalize-space(.), ${lit})])[1]/following::*[self::${kind}][not(@type="hidden")][1]`);
};
const s = {
  go: (path) => async p => { await p.goto(BASE + path); },
  sel: (label, pick = 1) => async p => {
    const loc = ctl(p, label, "select");
    await loc.waitFor({ timeout: 8000 });
    const opts = await loc.locator('option').evaluateAll(o => o.map(x => ({ v: x.value, t: x.textContent.trim() })));
    const real = opts.filter(o => o.v);
    const choice = typeof pick === 'number' ? real[Math.min(pick - 1, real.length - 1)] : real.find(o => o.t.toLowerCase().includes(pick.toLowerCase())) || real[0];
    if (!choice) throw new Error(`no options for ${label}`);
    await Promise.all([p.waitForLoadState('networkidle').catch(() => {}), loc.selectOption(choice.v)]);
    await p.waitForTimeout(700);
  },
  fill: (label, value) => async p => { await ctl(p, label, "input or self::textarea").fill(value, { timeout: 8000 }); },
  click: (text, opts = {}) => async p => {
    const loc = opts.css ? p.locator(text).first() : p.getByRole(opts.role || 'button', { name: text, exact: !!opts.exact }).first();
    await loc.scrollIntoViewIfNeeded().catch(() => {});
    await Promise.all([opts.nav ? p.waitForNavigation().catch(() => {}) : Promise.resolve(), loc.click({ timeout: 5000 }).catch(() => loc.dispatchEvent("click"))]);
    await p.waitForLoadState('networkidle').catch(() => {});
    await p.waitForTimeout(700);
  },
  scrollTo: (text) => async p => { await p.getByText(text, { exact: false }).first().scrollIntoViewIfNeeded(); await p.waitForTimeout(300); },
  fillCss: (css, value) => async p => { await p.locator(css).first().fill(value, { timeout: 8000 }); },
  wait: (ms) => async p => { await p.waitForTimeout(ms); },
};

// [action, ...] then {shot:'name', caption:'...'}
const flows = [
  flow('login', 'testadmin', [
    { do: [s.go('/Portal/Helpdesk')], shot: 'landing', caption: 'After signing in, non-SuperAdmin users land on the Helpdesk ticket queue.' },
    { do: [s.click('summary.auth-chip', { css: true })], shot: 'profile-menu', caption: 'Open the profile menu (top right) for notifications, user settings, two-factor authentication and Sign Out.' },
  ]),
  flow('department-add', 'testadmin', [
    { do: [s.go('/Portal/Departments')], shot: 'list', caption: 'The Departments list shows code, name, institution type and status.' },
    { do: [s.click('Add Department')], shot: 'form', caption: 'Click Add Department to open the form.' },
    { do: [s.fillCss('.modal.show input[name=code]', 'ENG'), s.fillCss('.modal.show input[name=name]', 'English Literature')], shot: 'filled', caption: 'Enter a unique code and the department name, choose the institution type, then click Save.' },
  ]),
  flow('students-filter', 'testadmin', [
    { do: [s.go('/Portal/Students'), s.sel('Department', 'Information Technology')], shot: 'filtered', caption: 'Choose a department to filter the student list.' },
  ]),
  flow('attendance-entry', 'testadmin', [
    { do: [s.go('/Portal/EnterAttendance')], shot: 'start', caption: 'Enter Attendance starts with the scope filters.' },
    { do: [s.sel('Department', 'Information Technology')], shot: 'dept', caption: 'Step 1: choose the department.' },
    { do: [s.sel('Course', 1)], shot: 'course', caption: 'Step 2: choose the course (program).' },
    { do: [s.sel('Select Course Offering', 1)], shot: 'grid', caption: 'Step 3: choose the subject offering; the roster appears with a Present checkbox per student. Tick or untick, then click Save Attendance.' },
  ]),
  flow('results-entry', 'testadmin', [
    { do: [s.go('/Portal/EnterResults'), s.sel('Department *', 'Information Technology')], shot: 'dept', caption: 'Step 1: choose the department.' },
    { do: [s.sel('Program/Course *', 1)], shot: 'program', caption: 'Step 2: choose the program.' },
    { do: [s.sel('Subject *', 1)], shot: 'subject', caption: 'Step 3: choose the subject.' },
    { do: [s.sel('Exam Type *', 1), s.click('Apply')], shot: 'grid', caption: 'Step 4: choose the exam type and click Apply. Enter marks in the Result Entry Grid, then publish.' },
  ]),
  flow('report-gpa', 'testadmin', [
    { do: [s.go('/Portal/ReportCenter')], shot: 'center', caption: 'Report Center lists every report your role may run.' },
    { do: [s.click('GPA & CGPA Report', { role: 'link' })], shot: 'filters', caption: 'Open a report and choose its filters.' },
    { do: [s.sel('Department', 1), s.click('Filter')], shot: 'output', caption: 'Generate the report; use the export buttons to download Excel, CSV or PDF.' },
  ]),
  flow('payment-create', 'testadmin', [
    { do: [s.go('/Portal/Payments')], shot: 'list', caption: 'Payments shows all fee receipts with their status.' },
    { do: [s.sel('Student', 1), s.fill('Amount', '25000'), s.fill('Receipt No', 'RCP-2026-0101'), s.fill('Description', 'Semester tuition fee')], shot: 'filled', caption: 'Fill in the Create Fee Receipt form and click Create.' },
  ]),
  flow('user-create', 'testadmin', [
    { do: [s.go('/Portal/UserImport')], shot: 'page', caption: 'User Import supports CSV bulk import and single-user creation.' },
    { do: [s.scrollTo('Create Single User'), s.fill('Username *', 'jane.doe'), s.fill('Email *', 'jane.doe@tabsan.local'), s.fill('Full Name', 'Jane Doe')], shot: 'single', caption: 'To create one account, complete the Create Single User form and click Create User.', full: true },
  ]),
  flow('lifecycle', 'testadmin', [
    { do: [s.go('/Portal/StudentLifecycle'), s.sel('Department', 'Information Technology')], shot: 'dept', caption: 'Choose a department.' },
    { do: [s.sel('Semester', 2)], shot: 'class', caption: 'Choose a semester/class to list its students with Promote and Graduate actions.' },
  ]),
  flow('announcement', 'testadmin', [
    { do: [s.go('/Portal/Announcements'), s.sel('Department', 'Information Technology')], shot: 'scope', caption: 'Pick a department (and optionally a course offering) to view announcements in that scope.' },
  ]),
  flow('license', 'superadmin', [
    { do: [s.go('/Portal/LicenseUpdate')], shot: 'page', caption: 'License Update shows the current license and lets the SuperAdmin upload a new .tablic file.' },
  ]),
  flow('faculty-gradebook', 'faculty.it1', [
    { do: [s.go('/Portal/Gradebook'), s.sel('Course Offering', 1)], shot: 'grid', caption: 'Choose a course offering to see the gradebook grid.' },
  ]),
  flow('faculty-quizzes', 'faculty.it1', [
    { do: [s.go('/Portal/Quizzes'), s.sel('Select Course Offering', 1)], shot: 'list', caption: 'Choose a course offering to list and manage its quizzes.' },
  ]),
  flow('faculty-assignments', 'faculty.it1', [
    { do: [s.go('/Portal/Assignments')], shot: 'list', caption: 'Assignments for your courses.' },
  ]),
  flow('student-results', 'bscs8s1', [
    { do: [s.go('/Portal/Results')], shot: 'list', caption: 'Students see their published results and can export them or request a re-check.' },
  ]),
  flow('student-helpdesk', 'bscs8s1', [
    { do: [s.go('/Portal/Helpdesk')], shot: 'queue', caption: 'Helpdesk lists your support tickets by status.' },
    { do: [s.click('New Ticket', { role: 'link' })], shot: 'new', caption: 'Click New Ticket, choose a category, enter a subject and description, and submit.' },
  ]),
  flow('user-settings', 'bscs8s1', [
    { do: [s.go('/Portal/UserSettings')], shot: 'page', caption: 'User Settings: update contact details, upload a photo, and change your password.', full: true },
  ]),
  flow('theme', 'bscs8s1', [
    { do: [s.go('/Portal/ThemeSettings'), s.click('Ocean Blue')], shot: 'preview', caption: 'Click a theme to preview it, then save.' },
  ]),
];

for (const f of flows) {
  if (only !== 'all' && only !== f.name) continue;
  const dir = `flows/${f.name}`; fs.mkdirSync(dir, { recursive: true });
  const { ctx, page } = await session(f.user);
  results[f.name] = { user: f.user, steps: [] };
  let n = 1;
  for (const st of f.steps) {
    let error = null;
    for (const a of st.do) { try { await a(page); } catch (e) { error = String(e).split('\n')[0]; break; } }
    await page.waitForLoadState('networkidle').catch(() => {});
    await page.waitForTimeout(500);
    const file = `${dir}/${String(n++).padStart(2, '0')}-${st.shot}.png`;
    if (st.full) {
      const h = await page.evaluate(() => document.documentElement.scrollHeight);
      await page.screenshot({ path: file, fullPage: true, clip: { x: 0, y: 0, width: 1440, height: Math.min(h, 1900) } });
    } else await page.screenshot({ path: file });
    results[f.name].steps.push({ file, caption: st.caption, error, url: page.url().replace(BASE, '') });
    console.log(f.name, st.shot, error || 'ok');
  }
  await ctx.close();
}
fs.writeFileSync(`flows/flows${only === 'all' ? '' : '-' + only}.json`, JSON.stringify(results, null, 2));
await browser.close();
