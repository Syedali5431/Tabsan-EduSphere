// Plan M14.2 — exam-day load test: thousands of students start the same quiz within a few minutes, autosave answers
// while they work, and submit near the deadline; afterwards the teacher publishes results and students open them.
//
//   k6 run tests/load/k6-exam-day.js -e BASE_URL=https://api.staging.example.edu -e QUIZ_ID=<guid> \
//          -e USERS_FILE=students.csv -e EXAM_USERS=5000 -e TEACHER_USERNAME=… -e TEACHER_PASSWORD=… -e OFFERING_ID=<guid>
//
// USERS_FILE: CSV "username,password" (or a "token" column of access tokens, e.g. written by the ExamDayLoadSeed
// integration test) with one enrolled student per line (a student can only start a quiz as often
// as MaxAttempts allows; use a quiz with unlimited attempts when users repeat). Without USERS_FILE every virtual user
// signs in as STUDENT_USERNAME/STUDENT_PASSWORD (smoke runs only).
// Scenarios: exam (one session per student, arriving over START_WINDOW: start, load the paper, AUTOSAVES autosaves,
// submit) and results (the teacher queues the results publication; students read their results). Thresholds follow the
// exam-day targets in tests/load/README.md.
import http from 'k6/http';
import { check, sleep, fail } from 'k6';
import { SharedArray } from 'k6/data';
import exec from 'k6/execution';
import { Trend, Rate, Counter } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5181').replace(/\/$/, '');
const QUIZ_ID = __ENV.QUIZ_ID || '';
const OFFERING_ID = __ENV.OFFERING_ID || '';
const EXAM_USERS = Math.max(1, Number.parseInt(__ENV.EXAM_USERS || '5000', 10));
const START_WINDOW = __ENV.START_WINDOW || '5m';          // students arrive over this window
const WORK_MINUTES = Number.parseFloat(__ENV.WORK_MINUTES || '20');
const AUTOSAVES = Math.max(1, Number.parseInt(__ENV.AUTOSAVES || '6', 10));
const P95_START_MS = Number.parseInt(__ENV.P95_START_MS || '2000', 10);
const P95_SUBMIT_MS = Number.parseInt(__ENV.P95_SUBMIT_MS || '3000', 10);
const P95_SAVE_MS = Number.parseInt(__ENV.P95_SAVE_MS || '1000', 10);
const MAX_ERROR_RATE = Number.parseFloat(__ENV.MAX_ERROR_RATE || '0.01');
const RUN_RESULTS = (__ENV.RUN_RESULTS || 'true') !== 'false';

const users = new SharedArray('students', () => {
  if (!__ENV.USERS_FILE) return [{ username: __ENV.STUDENT_USERNAME || '', password: __ENV.STUDENT_PASSWORD || '' }];
  const lines = open(__ENV.USERS_FILE).split(/\r?\n/);
  // A "token" column: pre-issued access tokens (sign-in is then not part of the run); otherwise "username,password".
  if (lines[0].trim().toLowerCase() === 'token') return lines.slice(1).filter((l) => l.trim()).map((l) => ({ token: l.trim() }));
  return lines.slice(1).map((l) => l.split(',')).filter((p) => p.length >= 2 && p[0])
    .map((p) => ({ username: p[0].trim(), password: p[1].trim() }));
});

export const startDuration = new Trend('exam_start_duration', true);
export const paperDuration = new Trend('exam_paper_duration', true);
export const saveDuration = new Trend('exam_save_duration', true);
export const submitDuration = new Trend('exam_submit_duration', true);
export const resultsDuration = new Trend('results_read_duration', true);
export const examErrors = new Rate('exam_errors');
export const throttled = new Counter('exam_429');

export const options = {
  scenarios: {
    exam: {
      executor: 'ramping-arrival-rate',
      startRate: 1,
      timeUnit: '1s',
      // EXAM_USERS sessions spread over START_WINDOW (peak at the start of the exam).
      stages: [{ duration: START_WINDOW, target: Math.max(1, Math.ceil(EXAM_USERS / durationSeconds(START_WINDOW)) * 2) }],
      preAllocatedVUs: Math.min(EXAM_USERS, 2000),
      maxVUs: Math.ceil(EXAM_USERS * 1.15),
      // Sessions outlast the arrival window: let started students work and submit.
      gracefulStop: `${Math.ceil(WORK_MINUTES * 60 + 90)}s`,
      exec: 'examSession',
    },
    ...(RUN_RESULTS && OFFERING_ID ? {
      results: {
        executor: 'per-vu-iterations',
        vus: Math.max(1, Math.min(EXAM_USERS, Number.parseInt(__ENV.RESULT_READERS || '1000', 10))),
        iterations: 1,
        startTime: __ENV.RESULTS_START || `${Math.ceil(durationSeconds(START_WINDOW) + WORK_MINUTES * 60)}s`,
        exec: 'readResults',
      },
    } : {}),
  },
  thresholds: {
    exam_start_duration: [`p(95)<${P95_START_MS}`],
    exam_save_duration: [`p(95)<${P95_SAVE_MS}`],
    exam_submit_duration: [`p(95)<${P95_SUBMIT_MS}`],
    exam_errors: [`rate<${MAX_ERROR_RATE}`],
    http_req_failed: [`rate<${MAX_ERROR_RATE}`],
  },
  summaryTrendStats: ['avg', 'p(50)', 'p(95)', 'p(99)', 'max'],
};

function durationSeconds(d) {
  const m = /^(\d+(?:\.\d+)?)(ms|s|m|h)$/.exec(d);
  if (!m) return 300;
  const n = Number.parseFloat(m[1]);
  return { ms: n / 1000, s: n, m: n * 60, h: n * 3600 }[m[2]];
}

function json(res) {
  try { return res.json(); } catch (e) { return null; }
}

function signIn(user) {
  if (user.token) return user.token;
  const res = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({ username: user.username, password: user.password }),
    { headers: { 'Content-Type': 'application/json' }, tags: { step: 'login' } });
  if (res.status === 429) throttled.add(1);
  const ok = check(res, { 'signed in': (r) => r.status === 200 });
  examErrors.add(!ok);
  return ok ? json(res)?.accessToken : null;
}

function auth(token) {
  return { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } };
}

/** An answer for each question type the paper offers (choice questions: the first option). */
function answerFor(q) {
  if (q.options && q.options.length) {
    return { quizQuestionId: q.questionId, selectedOptionId: q.options[0].id, textResponse: null };
  }
  return { quizQuestionId: q.questionId, selectedOptionId: null, textResponse: 'Load-test answer' };
}

export function setup() {
  if (!QUIZ_ID) fail('QUIZ_ID is required');
  if (!users.length || !(users[0].username || users[0].token)) fail('USERS_FILE or STUDENT_USERNAME/STUDENT_PASSWORD is required');
  return {};
}

export function examSession() {
  // One session per student (two sessions on one attempt would collide); a single smoke-test user is reused.
  const index = exec.scenario.iterationInTest;
  if (users.length > 1 && index >= users.length) return;
  const user = users[index % users.length];
  const token = signIn(user);
  if (!token) return;

  const start = http.post(`${BASE_URL}/api/v1/quiz/${QUIZ_ID}/start`, JSON.stringify({ accessCode: __ENV.ACCESS_CODE || null }), auth(token));
  startDuration.add(start.timings.duration);
  if (start.status === 429) throttled.add(1);
  const started = check(start, { 'quiz started': (r) => r.status === 200 });
  examErrors.add(!started);
  if (!started) return;
  const attemptId = json(start)?.attemptId ?? json(start)?.id;

  const paperRes = http.get(`${BASE_URL}/api/v1/quiz/attempt/${attemptId}/paper`, auth(token));
  paperDuration.add(paperRes.timings.duration);
  const paper = json(paperRes);
  examErrors.add(!check(paperRes, { 'paper loaded': (r) => r.status === 200 }));
  const questions = paper?.questions ?? [];
  const answers = questions.map(answerFor);

  // Autosaves spread over the working time (the take-quiz page saves as students answer).
  const pause = (WORK_MINUTES * 60) / (AUTOSAVES + 1);
  for (let i = 0; i < AUTOSAVES && answers.length; i++) {
    sleep(pause * (0.6 + Math.random() * 0.8));
    const chunk = answers.slice(0, Math.ceil(answers.length * (i + 1) / AUTOSAVES));
    const save = http.post(`${BASE_URL}/api/v1/quiz/attempt/${attemptId}/answers`, JSON.stringify({ answers: chunk }), auth(token));
    saveDuration.add(save.timings.duration);
    if (save.status === 429) throttled.add(1);
    examErrors.add(!check(save, { 'answers saved': (r) => r.status === 200 }));
  }

  sleep(Math.random() * 30); // the submit spike near the deadline
  const submit = http.post(`${BASE_URL}/api/v1/quiz/attempt/submit`, JSON.stringify({ attemptId, answers }), auth(token));
  submitDuration.add(submit.timings.duration);
  if (submit.status === 429) throttled.add(1);
  examErrors.add(!check(submit, { 'attempt submitted': (r) => r.status === 200 }));
}

export function readResults() {
  // The scenario's first iteration is the teacher: queue the publication of the course's results; the others open their results.
  if (exec.scenario.iterationInTest === 0 && (__ENV.TEACHER_TOKEN || __ENV.TEACHER_USERNAME)) {
    const token = signIn(__ENV.TEACHER_TOKEN ? { token: __ENV.TEACHER_TOKEN }
      : { username: __ENV.TEACHER_USERNAME, password: __ENV.TEACHER_PASSWORD || '' });
    if (token) {
      const job = http.post(`${BASE_URL}/api/v1/result/publish-all/jobs?courseOfferingId=${OFFERING_ID}`, null, auth(token));
      examErrors.add(!check(job, { 'publication queued': (r) => r.status === 200 || r.status === 202 }));
    }
    return;
  }
  sleep(Math.random() * 20);
  const token = signIn(users[(__VU * 104729) % users.length]);
  if (!token) return;
  for (let i = 0; i < 3; i++) {
    const res = http.get(`${BASE_URL}/api/v1/result/my-results`, auth(token));
    resultsDuration.add(res.timings.duration);
    examErrors.add(!check(res, { 'results read': (r) => r.status === 200 }));
    sleep(5 + Math.random() * 10);
  }
}

export function handleSummary(data) {
  const m = (name, stat) => data.metrics[name]?.values?.[stat];
  const line = (name) => `${name}: p95=${(m(name, 'p(95)') ?? 0).toFixed(0)}ms p99=${(m(name, 'p(99)') ?? 0).toFixed(0)}ms max=${(m(name, 'max') ?? 0).toFixed(0)}ms`;
  const text = [
    `exam-day  base=${BASE_URL} users=${EXAM_USERS} window=${START_WINDOW} work=${WORK_MINUTES}m`,
    line('exam_start_duration'), line('exam_paper_duration'), line('exam_save_duration'), line('exam_submit_duration'), line('results_read_duration'),
    `errors=${((m('exam_errors', 'rate') ?? 0) * 100).toFixed(2)}%  429s=${m('exam_429', 'count') ?? 0}  requests=${m('http_reqs', 'count') ?? 0}`,
  ].join('\n') + '\n';
  const out = { stdout: text };
  if (__ENV.SUMMARY_JSON_PATH) out[__ENV.SUMMARY_JSON_PATH] = JSON.stringify(data, null, 2);
  return out;
}
