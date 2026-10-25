// Step-by-step workflow captures. usage: node flows.mjs <flowName|all>
import { chromium } from 'playwright-core';
import fs from 'fs';

const BASE = 'http://localhost:5063';
const only = process.argv[2] || 'all';
const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: false, args: ['--headless=new', '--hide-scrollbars'] });
const results = {};

// Ids of the demo items the Plan M flows open (made by demo-data.mjs), looked up through the API by their titles.
const API = 'http://localhost:5181/api/v1';
let ids = {};
async function demoIds() {
  const tokens = {};
  const get = async (user, url) => {
    tokens[user] ??= (await (await fetch(`${API}/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: user, password: 'EduSphere147' }) })).json()).accessToken;
    const r = await fetch(`${API}/${url}`, { headers: { Authorization: `Bearer ${tokens[user]}` } });
    return r.ok ? r.json() : null;
  };
  const out = {};
  const offering = (await get('faculty.it1', 'course/offerings')).find(o => o.courseCode === 'CS702');
  out.offering = offering.id;
  const quizzes = await get('faculty.it1', `quiz/by-offering/${out.offering}`);
  out.quiz = quizzes.find(q => q.title.startsWith('Quiz 1: Foundations'))?.quizId;
  out.bank = (await get('faculty.it1', 'quiz/banks'))?.find?.(b => b.title === 'Machine learning basics')?.bankId;
  const assignments = await get('faculty.it1', `assignment/by-offering/${out.offering}`);
  out.review = assignments.find(a => a.title.startsWith('Literature review'))?.id;
  out.attempt = (await get('bscs8s1', `quiz/${out.quiz}/my-attempts`))?.[0]?.attemptId;
  out.peerReview = (await get('bscs8s2', `assignment/${out.review}/my-extras`))?.reviewsToDo?.[0]?.id;
  const scores = await get('faculty.it1', `similarity/assignments/${out.review}/scores`) ?? [];
  out.similarityCheck = [...scores].sort((a, b) => (b.score ?? 0) - (a.score ?? 0))[0]?.checkId;
  const modules = await get('bscs8s1', `lms/offerings/${out.offering}/my-modules`);
  out.pageItem = modules?.modules?.[0]?.items?.find(i => i.itemType === 'Page')?.id;
  const packages = await get('faculty.it1', `scorm/offerings/${out.offering}/packages`) ?? [];
  out.scormParts = packages.find(p => p.title.includes('course in parts'))?.id;
  out.scormLessons = packages.find(p => p.title.includes('lessons and quizzes'))?.id;
  out.program = (await get('faculty.it1', 'outcomes/programs')).find(p => p.code === 'BSCS')?.id;
  return out;
}

async function session(user) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.5 });
  const page = await ctx.newPage();
  if (!user) return { ctx, page }; // a signed-out flow
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
  go: (path) => async p => { await p.goto(BASE + (typeof path === 'function' ? path(ids) : path)); },
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
  // Scrolls the card (or other block) holding the text, or a CSS selector, to the top of the window.
  top: (target, height = 900) => async p => {
    await p.setViewportSize({ width: 1440, height });
    const loc = target.startsWith('#') ? p.locator(target).first() : p.getByText(target, { exact: false }).first();
    await loc.evaluate(e => { const box = e.closest('.card, .accordion-item, section') || e; window.scrollTo(0, box.getBoundingClientRect().top + window.scrollY - 16); });
    await p.waitForTimeout(400);
  },
  fillCss: (css, value) => async p => { await p.locator(css).first().fill(value, { timeout: 8000 }); },
  fn: (f) => async p => { await f(p); await p.waitForTimeout(500); },
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
    { do: [s.scrollTo('Create Single User'), s.fill('Username *', 'jane.doe'), s.fill('Email *', 'jane.doe@tabsan.local'), s.fill('Full Name', 'Jane Doe'), s.fn(async p => { await p.selectOption('#singleUserRole', 'Student'); await p.selectOption('#studentFields [name="DepartmentId"]', { index: 1 }); await p.selectOption('#studentFields [name="ProgramId"]', { index: 1 }); await p.locator('#studentFields').scrollIntoViewIfNeeded(); })], shot: 'single', caption: 'Create Single User for a student: choose the Department and Program (the registration number defaults to the username), then click Create User.' },
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
  flow('institute-filter', 'superadmin', [
    { do: [s.go('/Portal/Programs'), s.fn(async p => { await Promise.all([p.waitForNavigation(), p.selectOption('select[data-institution-filter]', '2')]); })], shot: 'programs-college', caption: 'Institute set to College: only College programs are listed.' },
    { do: [s.go('/Portal/Courses'), s.fn(async p => { await Promise.all([p.waitForNavigation(), p.selectOption('select[data-institution-filter]', '1')]); })], shot: 'courses-school', caption: 'Institute set to School: the Courses page shows only School departments, classes and courses.' },
  ]),
  flow('theme-dark', 'testadmin', [
    { do: [s.go('/Portal/ThemeSettings'), s.click('Midnight Dark')], shot: 'preview', caption: 'Midnight Dark preview.' },
    { do: [s.go('/Portal/Departments'), s.fn(async p => { await p.addStyleTag({ content: '*{transition:none!important}' }); await p.evaluate(() => document.documentElement.setAttribute('data-theme', 'midnight_dark')); })], shot: 'departments', caption: 'Departments in the Midnight Dark theme.' },
  ]),

  // ── Plan M features (need the demo data from demo-data.mjs; SSO needs the test identity provider of SSO-Setup-Guide.md) ──
  flow('sso-login', null, [
    { do: [s.go('/Login?tenant=TABSAN-UNI')], shot: 'buttons', caption: 'With single sign-on set up, the sign-in page shows a button for each identity provider.' },
    { do: [s.click('Sign in with Keycloak (OIDC)', { role: 'link', nav: true }), s.wait(1500)], shot: 'idp', caption: 'The button opens the institution\'s own sign-in page (here a Keycloak test server); afterwards the portal opens.' },
  ]),
  flow('sso-settings', 'superadmin', [
    { do: [s.go('/Portal/SsoSettings?tenant=TABSAN-UNI')], shot: 'page', caption: 'Single Sign-On: the values to register at the identity provider, and one card per provider.' },
    { do: [s.top('#sso-saml', 1500)], shot: 'saml', caption: 'A SAML 2.0 provider: metadata URL, attribute mapping, role check, request signing and encrypted assertions.' },
  ]),
  flow('linked-accounts', 'faculty.it1', [
    { do: [s.go('/Portal/TwoFactorSettings'), s.top('Linked sign-in accounts')], shot: 'linked', caption: 'Two-Factor Authentication also lists the identity-provider accounts linked to you; you can unlink them there.' },
  ]),
  flow('language', 'bscs8s1', [
    { do: [s.go(i => `/Portal/CourseLms?offeringId=${i.offering}`), s.fn(async p => { await Promise.all([p.waitForNavigation(), p.selectOption('select[data-lang-select]', 'ar')]); })], shot: 'arabic', caption: 'The portal in Arabic: the whole layout switches to right-to-left.' },
    { do: [s.fn(async p => { await Promise.all([p.waitForNavigation(), p.selectOption('select[data-lang-select]', 'ur')]); })], shot: 'urdu', caption: 'The portal in Urdu.' },
    { do: [s.fn(async p => { await Promise.all([p.waitForNavigation(), p.selectOption('select[data-lang-select]', 'en')]); })], shot: 'english', caption: 'Back in English (the choice is saved to your profile).' },
  ]),
  flow('language-settings', 'superadmin', [
    { do: [s.go('/Portal/LanguageSettings')], shot: 'page', caption: 'Language & Translations: languages offered, the default, and translations you can change or export for a translator.' },
  ]),
  flow('quiz-teacher', 'faculty.it1', [
    { do: [s.go(i => `/Portal/Quizzes?offeringId=${i.offering}`)], shot: 'list', caption: 'Quizzes of a course with their status, attempts and the builder, grading and statistics links.' },
    { do: [s.go(i => `/Portal/QuizBuilder?quizId=${i.quiz}`)], shot: 'builder', caption: 'The quiz builder: settings, questions of every type, and a group that draws questions at random from a bank.', full: true },
    { do: [s.go(i => `/Portal/QuestionBanks?offeringId=${i.offering}&bankId=${i.bank}`)], shot: 'bank', caption: 'A question bank: reusable questions with tags and difficulty; export to and import from Excel.', full: true },
    { do: [s.go(i => `/Portal/QuizGrading?quizId=${i.quiz}`)], shot: 'grading', caption: 'Grading: answers that need a teacher (essays, file uploads) wait in a queue.' },
    { do: [s.go(i => `/Portal/QuizStatistics?quizId=${i.quiz}`)], shot: 'statistics', caption: 'Quiz statistics: score spread and, per question, difficulty and discrimination.', full: true },
  ]),
  flow('quiz-student', 'bscs8s5', [
    { do: [s.go(i => `/Portal/TakeQuiz?quizId=${i.quiz}`)], shot: 'start', caption: 'Before starting: time limit, attempts left and the instructions.' },
  ]),
  flow('quiz-review', 'bscs8s1', [
    { do: [s.go(i => `/Portal/QuizReview?attemptId=${i.attempt}&quizId=${i.quiz}`)], shot: 'review', caption: 'After submitting: the score, each answer, the correct answers (when the teacher allows) and feedback.', full: true },
  ]),
  flow('assignment-teacher', 'faculty.it1', [
    { do: [s.go(i => `/Portal/AssignmentPolicies?offeringId=${i.offering}`)], shot: 'policies', caption: 'Assignment Policies: the course\'s late and missing-work rules, and its student groups.', full: true },
    { do: [s.go(i => `/Portal/AssignmentOptions?assignmentId=${i.review}`)], shot: 'options', caption: 'Assignment options: availability, group work, peer review, personal due dates and similarity checking.', full: true },
    { do: [s.go(i => `/Portal/GradingWorkspace?assignmentId=${i.review}`)], shot: 'workspace', caption: 'The grading workspace: the submission, rubric, marks, comments and the similarity score in one place.' },
    { do: [s.go(i => `/Portal/SimilarityReport?id=${i.similarityCheck}`)], shot: 'similarity', caption: 'A similarity report: the matching passages and the work they match.' },
  ]),
  flow('assignment-student', 'bscs8s2', [
    { do: [s.go(i => `/Portal/AssignmentSubmission?assignmentId=${i.review}`)], shot: 'submission', caption: 'A student\'s assignment page: their own due date, the late rule, their submission, its similarity score and peer reviews to do.', full: true },
    { do: [s.go(i => `/Portal/PeerReview?id=${i.peerReview}`)], shot: 'peer-review', caption: 'Reviewing a classmate\'s work anonymously with the rubric.', full: true },
  ]),
  flow('groups-student', 'bscs8s1', [
    { do: [s.go(i => `/Portal/MyGroups?offeringId=${i.offering}`)], shot: 'groups', caption: 'My Groups: your project team and groups you can join yourself.' },
  ]),
  flow('modules-teacher', 'faculty.it1', [
    { do: [s.go(i => `/Portal/LmsManage?offeringId=${i.offering}`)], shot: 'manage', caption: 'LMS Manage: modules with their items, completion requirements and release rules.', full: true },
    { do: [s.go(i => `/Portal/LmsManage?offeringId=${i.offering}`), s.scrollTo('External tools')], shot: 'tools-scorm', caption: 'The same page lists the course\'s external tools (LTI) and SCORM packages.' },
    { do: [s.go(i => `/Portal/ModuleProgress?offeringId=${i.offering}`)], shot: 'progress', caption: 'Module Progress: how far each student has got.' },
  ]),
  flow('modules-student', 'bscs8s1', [
    { do: [s.go(i => `/Portal/CourseLms?offeringId=${i.offering}`)], shot: 'course', caption: 'A student\'s course page: modules, what each item needs, and progress.', full: true },
    { do: [s.go(i => `/Portal/ModuleItem/${i.pageItem}`)], shot: 'item', caption: 'An item opens with Previous and Next buttons to move through the module.' },
  ]),
  flow('outcomes', 'testadmin', [
    { do: [s.go('/Portal/Outcomes')], shot: 'list', caption: 'Learning Outcomes: program and course outcomes, their rating scales and mappings.' },
    { do: [s.go(i => `/Portal/OutcomeAttainment?programId=${i.program}`)], shot: 'attainment', caption: 'Outcome attainment for a program (for accreditation reports).' },
  ]),
  flow('outcomes-teacher', 'faculty.it1', [
    { do: [s.go(i => `/Portal/OutcomeMastery?offeringId=${i.offering}`)], shot: 'mastery', caption: 'Outcome mastery for a course: each student against each outcome.', full: true },
  ]),
  flow('outcomes-student', 'bscs8s1', [
    { do: [s.go('/Portal/MyOutcomes')], shot: 'mine', caption: 'My Outcomes: a student\'s mastery of each outcome.' },
  ]),
  flow('live-classes', 'faculty.it1', [
    { do: [s.go(i => `/Portal/LiveClasses?offeringId=${i.offering}`)], shot: 'teacher', caption: 'Live Classes: scheduled sessions with their link and automatic attendance.', full: true },
  ]),
  flow('live-classes-student', 'bscs8s1', [
    { do: [s.go(i => `/Portal/LiveClasses?offeringId=${i.offering}`)], shot: 'student', caption: 'Students see their upcoming live classes and join from here.' },
  ]),
  flow('admin-integrations', 'superadmin', [
    { do: [s.go('/Portal/LiveClassSettings')], shot: 'live-settings', caption: 'Live Class Settings: the meeting service (meeting links, BigBlueButton or Zoom).' },
    { do: [s.go('/Portal/SimilaritySettings')], shot: 'similarity-settings', caption: 'Similarity Checking: the built-in checker or a Copyleaks account.' },
    { do: [s.go('/Portal/XapiSettings')], shot: 'xapi', caption: 'Learning Record Store (xAPI): credentials for activity providers and recent statements.' },
    { do: [s.go('/Portal/ApiTokens')], shot: 'api-tokens', caption: 'API Tokens: personal tokens for scripts and integrations.' },
  ]),
  flow('lti-tools', 'superadmin', [
    { do: [s.go('/Portal/LtiTools')], shot: 'tools', caption: 'External Tools (LTI 1.3): registered tools, their deployments and the platform details to give a tool.', full: true },
  ]),
  flow('scorm-teacher', 'faculty.it1', [
    { do: [s.go(i => `/Portal/ScormReport?packageId=${i.scormLessons}`)], shot: 'report', caption: 'A SCORM package report: each learner\'s attempts, status, score and time.' },
  ]),
  flow('scorm-student', 'bscs8s1', [
    { do: [s.go(i => `/Portal/Scorm/${i.scormLessons}`)], shot: 'package', caption: 'A student\'s SCORM package page: status, attempts and Start / Continue / Review.' },
    { do: [s.click('Continue', { nav: true }), s.wait(4000)], shot: 'player', caption: 'A package in parts: the contents list shows each part\'s status; Previous and Next move between parts.' },
  ]),
  flow('push', 'bscs8s1', [
    { do: [s.go('/Portal/Notifications'), s.scrollTo('this device')], shot: 'device', caption: 'Notifications: turn on notifications for this phone or computer, and choose which ones you get.' },
  ]),
];
const planM = new Set(flows.slice(flows.findIndex(f => f.name === 'sso-login')).map(f => f.name));
if (only === 'all' || planM.has(only)) ids = await demoIds().catch(e => { console.log('demo ids:', e.message); return {}; });

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
      // A taller window (not a full-page capture) keeps the fixed sidebar and floating buttons where they belong.
      const h = await page.evaluate(() => document.documentElement.scrollHeight);
      await page.setViewportSize({ width: 1440, height: Math.max(900, Math.min(h, 1900)) });
      await page.waitForTimeout(400);
      await page.screenshot({ path: file });
    } else await page.screenshot({ path: file });
    await page.setViewportSize({ width: 1440, height: 900 });
    results[f.name].steps.push({ file, caption: st.caption, error, url: page.url().replace(BASE, '') });
    console.log(f.name, st.shot, error || 'ok');
  }
  await ctx.close();
}
fs.writeFileSync(`flows/flows${only === 'all' ? '' : '-' + only}.json`, JSON.stringify(results, null, 2));
await browser.close();
