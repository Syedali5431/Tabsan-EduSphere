/**
 * full-student-lifecycle.spec.ts
 * School (spa6) → College (sch3s1) → University (bscs8s1)
 * Each student: quizzes, assignments, attendance, timetable, results, payments.
 * Then admin promotes, graduates, exports certificates + loads reports.
 */
import { test, expect } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';

const ADMIN = 'admin.uni';
const ADMIN_PW = process.env.ADMIN_PASSWORD || 'EduSphere147';
const STUDENT_PW = process.env.STUDENT_PASSWORD || 'EduSphere147';

const S = {
  school:     { u:'spa6',    pid:'39E39F33-C075-49D7-99D8-08F8C9E57CE2', r:'SPA-1-06',  did:'D0000005-0000-0000-0000-000000000005', sem:1  },
  college:    { u:'sch3s1',  pid:'2140EBB8-78AB-489B-B121-036C8110AA24', r:'SCH-3-01',  did:'D0000004-0000-0000-0000-000000000004', sem:3  },
  university: { u:'bscs8s1', pid:'BAC79F9C-8386-413A-9CF2-060B614B1940', r:'BSCS-8-01', did:'D0000001-0000-0000-0000-000000000001', sem:8  },
};

const CERT_DIR = 'C:\\Users\\alin\\Desktop\\3d\\Certificates';
const RPT_DIR   = 'C:\\Users\\alin\\Desktop\\3d\\Certificates\\Reports';

function dirs() { for(const d of[CERT_DIR,RPT_DIR]) if(!fs.existsSync(d)) fs.mkdirSync(d,{recursive:true}); }

async function tok(p:any){ return (await p.locator('input[name="__RequestVerificationToken"]').first().getAttribute('value'))||''; }

async function dl(p:any, url:string, fn:string, d:string){
  const r=await p.request.get(url.startsWith('http')?url:`http://localhost:5063${url}`);
  if(r.status()===200) fs.writeFileSync(path.join(d,fn), await r.body());
}

// ── Interactive student flow ─────────────────────────────────────────────────
async function studentFlow(page: any, user: string) {
  await page.goto('/Login',{waitUntil:'domcontentloaded'});
  await page.fill('#username',user);
  await page.fill('#password',STUDENT_PW);
  await page.click('button[type="submit"]');
  await expect(page.locator('.app-sidebar').first()).toBeVisible({timeout:15000});

  // QUIZZES
  await page.goto('/Portal/ViewQuizzes',{waitUntil:'domcontentloaded'});
  const sq=page.locator('a:has-text("Start"),button:has-text("Start"),a:has-text("Take")').first();
  if(await sq.isVisible({timeout:3000}).catch(()=>false)){await sq.click();await page.waitForLoadState('domcontentloaded');
    const r=page.locator('input[type="radio"]').first();
    if(await r.isVisible({timeout:2000}).catch(()=>false)) await r.check();
    const t=page.locator('input[type="text"],textarea').first();
    if(await t.isVisible({timeout:1000}).catch(()=>false)) await t.fill('Answer');
    const sb=page.locator('button:has-text("Submit"),button:has-text("Finish")').first();
    if(await sb.isVisible({timeout:2000}).catch(()=>false)){await sb.click();await page.waitForLoadState('domcontentloaded');}
  }

  // ASSIGNMENTS — create new
  await page.goto('/Portal/Assignments',{waitUntil:'domcontentloaded'});
  const cr=page.locator('button:has-text("Create"),a:has-text("Create"),a:has-text("New")').first();
  if(await cr.isVisible({timeout:3000}).catch(()=>false)){await cr.click();await page.waitForLoadState('domcontentloaded');
    const ti=page.locator('input[name="title"],input[name="Title"],input[placeholder*="title"]').first();
    if(await ti.isVisible({timeout:2000}).catch(()=>false)) await ti.fill('Test '+new Date().toISOString());
    const ta=page.locator('textarea').first();
    if(await ta.isVisible({timeout:2000}).catch(()=>false)) await ta.fill('Auto-submitted by Playwright.');
    const sv=page.locator('button:has-text("Save"),button:has-text("Submit"),button:has-text("Create")').first();
    if(await sv.isVisible({timeout:2000}).catch(()=>false)){await sv.click();await page.waitForLoadState('domcontentloaded');}
  }

  // ATTENDANCE
  await page.goto('/Portal/Attendance',{waitUntil:'domcontentloaded'});

  // TIMETABLE — apply filter
  await page.goto('/Portal/TimetableStudent',{waitUntil:'domcontentloaded'});
  const ap=page.locator('button:has-text("Apply")').first();
  if(await ap.isVisible({timeout:3000}).catch(()=>false)){await ap.click();await page.waitForLoadState('domcontentloaded');}

  // RESULTS — re-check
  await page.goto('/Portal/Results',{waitUntil:'domcontentloaded'});
  const rc=page.locator('button:has-text("Re-check"),a:has-text("Re-check"),button:has-text("Submit Re-check")').first();
  if(await rc.isVisible({timeout:3000}).catch(()=>false)){await rc.click();await page.waitForLoadState('domcontentloaded');}

  // PAYMENTS
  await page.goto('/Portal/Payments/My',{waitUntil:'domcontentloaded'});
  const al=page.locator('a:has-text("All")').first();
  if(await al.isVisible({timeout:2000}).catch(()=>false)){await al.click();await page.waitForLoadState('domcontentloaded');}
}

// ── Admin promote helper ─────────────────────────────────────────────────────
async function promote(page: any, s: any, max: number) {
  await page.goto('/Login',{waitUntil:'domcontentloaded'});
  await page.fill('#username',ADMIN); await page.fill('#password',ADMIN_PW);
  await page.click('button[type="submit"]');
  await expect(page.locator('.app-sidebar').first()).toBeVisible({timeout:15000});
  for(let sem=s.sem;sem<=max;sem++){
    const r=await page.request.post('/Portal/PromoteStudent',{form:{
      studentId:s.pid, departmentId:s.did, semester:sem.toString(), __RequestVerificationToken:await tok(page)
    }});
    expect(r.status()).toBeLessThan(500);
  }
}

// ── Certificate export helper ────────────────────────────────────────────────
async function certExport(page: any, s: any, prefix: string) {
  dirs();
  await page.goto('/Login',{waitUntil:'domcontentloaded'});
  await page.fill('#username',ADMIN); await page.fill('#password',ADMIN_PW);
  await page.click('button[type="submit"]');
  await expect(page.locator('.app-sidebar').first()).toBeVisible({timeout:15000});
  await page.goto('/Portal/GenerateCertificates',{waitUntil:'domcontentloaded'});
  const df=page.locator('select[name="departmentId"]');
  if(await df.isVisible({timeout:2000}).catch(()=>false)){
    const c=await df.locator('option').count(); if(c>1){await df.selectOption({index:1}); await page.waitForLoadState('domcontentloaded');}
  }
  const cl=page.locator('a[href*="GraduationCertificateDownload"],a[href*="DownloadCertificateTemplate"]');
  const cnt=await cl.count();
  for(let i=0;i<Math.min(cnt,2);i++){
    const h=await cl.nth(i).getAttribute('href');
    if(h) await dl(page,h,`${prefix}-${s.r}-${Date.now()}-${i}.pdf`,CERT_DIR);
  }
}

// ═══════════════════════════════ 1️⃣ SCHOOL — spa6 ═══════════════════════════
test('1-SCHOOL — spa6: quizzes, assignments, attendance, timetable, results, payments', async ({page})=>{await studentFlow(page,S.school.u);});
test('2-SCHOOL — Admin promotes spa6 class 1→10', async ({page})=>{await promote(page,S.school,10);});
test('3-SCHOOL — Export certificate', async ({page})=>{await certExport(page,S.school,'school');});

// ═══════════════════════════════ 2️⃣ COLLEGE — sch3s1 ═════════════════════════
test('4-COLLEGE — sch3s1: quizzes, assignments, attendance, timetable, results, payments', async ({page})=>{await studentFlow(page,S.college.u);});
test('5-COLLEGE — Admin promotes sch3s1 class 11→12', async ({page})=>{await promote(page,S.college,12);});
test('6-COLLEGE — Export certificate', async ({page})=>{await certExport(page,S.college,'college');});

// ═══════════════════════════ 3️⃣ UNIVERSITY — bscs8s1 ═════════════════════════
test('7-UNIVERSITY — bscs8s1: quizzes, assignments, attendance, timetable, results, payments', async ({page})=>{await studentFlow(page,S.university.u);});
test('8-UNIVERSITY — Admin promotes bscs8s1 sem 1→8', async ({page})=>{await promote(page,S.university,8);});
test('9-UNIVERSITY — Admin graduates bscs8s1', async ({page})=>{
  await page.goto('/Login',{waitUntil:'domcontentloaded'});
  await page.fill('#username',ADMIN); await page.fill('#password',ADMIN_PW);
  await page.click('button[type="submit"]');
  await expect(page.locator('.app-sidebar').first()).toBeVisible({timeout:15000});
  const r=await page.request.post('/Portal/GraduateStudent',{form:{
    studentId:S.university.pid, departmentId:S.university.did, __RequestVerificationToken:await tok(page)
  }});
  expect(r.status()).toBeLessThan(500);
});
test('10-UNIVERSITY — Export certificate', async ({page})=>{await certExport(page,S.university,'university');});

// ═══════════════════════════════ 📊 REPORTS ═══════════════════════════════════
test('11-REPORTS — Load attendance & results pages for all 3 institutes', async ({page})=>{
  dirs();
  await page.goto('/Login',{waitUntil:'domcontentloaded'});
  await page.fill('#username',ADMIN); await page.fill('#password',ADMIN_PW);
  await page.click('button[type="submit"]');
  await expect(page.locator('.app-sidebar').first()).toBeVisible({timeout:15000});
  const urls=[
    `/Portal/ReportAttendance?departmentId=${S.school.did}&institutionType=0`,
    `/Portal/ReportAttendance?departmentId=${S.college.did}&institutionType=1`,
    `/Portal/ReportAttendance?departmentId=${S.university.did}&institutionType=2`,
    `/Portal/ReportResults?departmentId=${S.school.did}&institutionType=0`,
    `/Portal/ReportResults?departmentId=${S.college.did}&institutionType=1`,
    `/Portal/ReportResults?departmentId=${S.university.did}&institutionType=2`,
  ];
  for(const u of urls) expect((await page.goto(u))?.status()).toBeLessThan(500);
});
