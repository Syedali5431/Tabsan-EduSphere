import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5181';
const LOGIN_PATH = '/api/v1/auth/login';
const TARGET_USERS = 50000;
const LOCAL_VU_CAP = Number.parseInt(__ENV.LOCAL_VU_CAP || '16000', 10);
const EFFECTIVE_USERS = Math.min(TARGET_USERS, Number.isFinite(LOCAL_VU_CAP) ? LOCAL_VU_CAP : 16000);

// Final-Touches Phase 34 Stage 5.2 — allow sharding traffic across multiple generators.
const GENERATOR_TOTAL = Math.max(1, Number.parseInt(__ENV.GENERATOR_TOTAL || '1', 10));
const GENERATOR_INDEX = Math.min(
  GENERATOR_TOTAL,
  Math.max(1, Number.parseInt(__ENV.GENERATOR_INDEX || '1', 10))
);
const SHARD_MAX_VUS = GENERATOR_TOTAL > 1 ? Math.max(50, Math.floor(EFFECTIVE_USERS / GENERATOR_TOTAL)) : EFFECTIVE_USERS;

// Final-Touches Phase 34 Stage 5.1 — throughput-driven scenario with realistic ramp shape.
const TARGET_RPS = Math.max(1, Number.parseInt(__ENV.TARGET_RPS || '120', 10));
const SHARD_TARGET_RPS = Math.max(1, Math.ceil(TARGET_RPS / GENERATOR_TOTAL));
const PREALLOCATED_VUS = Math.max(20, Math.min(SHARD_MAX_VUS, Math.floor(SHARD_MAX_VUS * 0.35)));

const THINK_TIME_MIN = Number.parseFloat(__ENV.THINK_TIME_MIN || '0.15');
const THINK_TIME_MAX = Number.parseFloat(__ENV.THINK_TIME_MAX || '0.85');

const TEST_USERNAME = __ENV.TEST_USERNAME || '';
const TEST_PASSWORD = __ENV.TEST_PASSWORD || '';

export const apiDuration = new Trend('api_duration', true);
export const apiErrors = new Rate('api_errors');

export const options = {
  discardResponseBodies: true,
  scenarios: {
    scale_50k: {
      executor: 'ramping-arrival-rate',
      startRate: Math.max(1, Math.floor(SHARD_TARGET_RPS * 0.05)),
      timeUnit: '1s',
      preAllocatedVUs: PREALLOCATED_VUS,
      maxVUs: SHARD_MAX_VUS,
      stages: [
        { duration: '3m', target: Math.max(1, Math.floor(SHARD_TARGET_RPS * 0.2)) },
        { duration: '8m', target: Math.max(1, Math.floor(SHARD_TARGET_RPS * 0.6)) },
        { duration: '15m', target: SHARD_TARGET_RPS },
        { duration: '3m', target: 0 },
      ],
    },
  },
  thresholds: {
    api_errors: ['rate<0.10'],
    api_duration: ['p(95)<3000'],
  },
};

export function setup() {
  if (!TEST_USERNAME || !TEST_PASSWORD) {
    return { token: null };
  }

  const loginRes = http.post(
    `${BASE_URL}${LOGIN_PATH}`,
    JSON.stringify({ username: TEST_USERNAME, password: TEST_PASSWORD, deviceInfo: 'k6-scale-50k' }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'auth-login-setup' } }
  );

  if (loginRes.status !== 200) {
    return { token: null };
  }

  try {
    const body = loginRes.json();
    return { token: body && body.accessToken ? body.accessToken : null };
  } catch (_) {
    return { token: null };
  }
}

function callGet(path, token, accepted) {
  const headers = { Accept: 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = http.get(`${BASE_URL}${path}`, {
    headers,
    tags: { endpoint: path },
  });

  apiDuration.add(res.timings.duration);
  const ok = accepted.includes(res.status);
  apiErrors.add(ok ? 0 : 1);

  check(res, {
    [`${path} expected status`]: () => ok,
  });

  return res;
}

export default function (data) {
  const token = data && data.token ? data.token : null;

  group('security-profile', function () {
    callGet('/api/v1/auth/security-profile', token, [200]);
  });

  group('dashboard', function () {
    callGet('/api/v1/dashboard/composition', token, [200, 401, 403]);
  });

  group('sidebar', function () {
    callGet('/api/v1/sidebar-menu/my-visible', token, [200, 401, 403]);
  });

  group('notifications', function () {
    callGet('/api/v1/notification/inbox?page=0&pageSize=20', token, [200, 401, 403]);
  });

  // Final-Touches Phase 34 Stage 5.1 — randomized think-time to better emulate user pacing.
  const thinkRange = Math.max(0, THINK_TIME_MAX - THINK_TIME_MIN);
  sleep(THINK_TIME_MIN + Math.random() * thinkRange);
}

export function handleSummary(data) {
  // Final-Touches Phase 34 Stage 5.3 — keep output concise and summary-focused for regular runs.
  const txtPath = __ENV.SUMMARY_TXT_PATH;
  const lines = [
    'profile=50k',
    `baseUrl=${BASE_URL}`,
    `targetUsers=${TARGET_USERS}`,
    `effectiveUsers=${EFFECTIVE_USERS}`,
    `targetRps=${TARGET_RPS}`,
    `shardTargetRps=${SHARD_TARGET_RPS}`,
    `generatorTotal=${GENERATOR_TOTAL}`,
    `generatorIndex=${GENERATOR_INDEX}`,
    `shardMaxVus=${SHARD_MAX_VUS}`,
    `localVuCap=${LOCAL_VU_CAP}`,
    `iterations=${data.metrics.iterations ? data.metrics.iterations.values.count : 0}`,
    `api_errors=${data.metrics.api_errors ? data.metrics.api_errors.values.rate : 0}`,
    `api_duration_p95=${data.metrics.api_duration ? data.metrics.api_duration.values['p(95)'] : 0}`,
  ];
  const text = `${lines.join('\n')}\n`;
  const output = { stdout: text };
  if (txtPath) {
    output[txtPath] = text;
  }
  return output;
}
