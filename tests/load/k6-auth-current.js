import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5181';
const LOGIN_PATH = '/api/v1/auth/login';
const GATE_NAME = __ENV.PHASE10_GATE || 'phase10';
const TARGET_USERS = Math.max(1, Number.parseInt(__ENV.PHASE10_TARGET_USERS || '10000', 10));
const LOCAL_VU_CAP = Number.parseInt(__ENV.LOCAL_VU_CAP || '16000', 10);
const EFFECTIVE_USERS = Math.min(TARGET_USERS, Number.isFinite(LOCAL_VU_CAP) ? LOCAL_VU_CAP : 16000);

// Final-Touches Phase 34 Stage 10.1 — progressive gate runner for incremental scale validation.
const GENERATOR_TOTAL = Math.max(1, Number.parseInt(__ENV.GENERATOR_TOTAL || '1', 10));
const GENERATOR_INDEX = Math.min(
  GENERATOR_TOTAL,
  Math.max(1, Number.parseInt(__ENV.GENERATOR_INDEX || '1', 10))
);
const SHARD_MAX_VUS = GENERATOR_TOTAL > 1 ? Math.max(50, Math.floor(EFFECTIVE_USERS / GENERATOR_TOTAL)) : EFFECTIVE_USERS;

const TARGET_RPS = Math.max(1, Number.parseInt(__ENV.PHASE10_TARGET_RPS || '120', 10));
const SHARD_TARGET_RPS = Math.max(1, Math.ceil(TARGET_RPS / GENERATOR_TOTAL));
const PREALLOCATED_VUS = Math.max(20, Math.min(SHARD_MAX_VUS, Math.floor(SHARD_MAX_VUS * 0.35)));

const THINK_TIME_MIN = Number.parseFloat(__ENV.THINK_TIME_MIN || '0.15');
const THINK_TIME_MAX = Number.parseFloat(__ENV.THINK_TIME_MAX || '0.90');

const RAMP_UP = __ENV.PHASE10_RAMP_UP || '2m';
const HOLD = __ENV.PHASE10_HOLD || '6m';
const RAMP_DOWN = __ENV.PHASE10_RAMP_DOWN || '2m';

const P95_THRESHOLD_MS = Number.parseInt(__ENV.PHASE10_P95_THRESHOLD_MS || '2500', 10);
const ERROR_RATE_THRESHOLD = Number.parseFloat(__ENV.PHASE10_ERROR_RATE_THRESHOLD || '0.05');

const TEST_USERNAME = __ENV.TEST_USERNAME || '';
const TEST_PASSWORD = __ENV.TEST_PASSWORD || '';

export const apiDuration = new Trend('api_duration', true);
export const apiErrors = new Rate('api_errors');
export const api5xx = new Rate('api_5xx');
export const api429 = new Rate('api_429');
export const api4xx = new Rate('api_4xx');

export const options = {
  discardResponseBodies: true,
  scenarios: {
    phase10_progressive: {
      executor: 'ramping-arrival-rate',
      startRate: Math.max(1, Math.floor(SHARD_TARGET_RPS * 0.05)),
      timeUnit: '1s',
      preAllocatedVUs: PREALLOCATED_VUS,
      maxVUs: SHARD_MAX_VUS,
      stages: [
        { duration: RAMP_UP, target: Math.max(1, Math.floor(SHARD_TARGET_RPS * 0.25)) },
        { duration: HOLD, target: SHARD_TARGET_RPS },
        { duration: RAMP_DOWN, target: 0 },
      ],
    },
  },
  thresholds: {
    api_errors: [`rate<${ERROR_RATE_THRESHOLD}`],
    api_duration: [`p(95)<${P95_THRESHOLD_MS}`],
    api_5xx: ['rate<0.01'],
    api_429: ['rate<0.01'],
  },
};

export function setup() {
  if (!TEST_USERNAME || !TEST_PASSWORD) {
    return { token: null };
  }

  const loginRes = http.post(
    `${BASE_URL}${LOGIN_PATH}`,
    JSON.stringify({ username: TEST_USERNAME, password: TEST_PASSWORD, deviceInfo: `k6-phase10-${GATE_NAME}` }),
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

function recordStatusMetrics(res, acceptedStatuses) {
  const accepted = acceptedStatuses.includes(res.status);
  apiErrors.add(accepted ? 0 : 1);
  api5xx.add(res.status >= 500 ? 1 : 0);
  api429.add(res.status === 429 ? 1 : 0);
  api4xx.add(res.status >= 400 && res.status < 500 ? 1 : 0);
  apiDuration.add(res.timings.duration);

  check(res, {
    expectedStatus: () => accepted,
  });
}

function callGet(path, token, acceptedStatuses) {
  const headers = { Accept: 'application/json' };
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const res = http.get(`${BASE_URL}${path}`, {
    headers,
    tags: { endpoint: path },
  });

  recordStatusMetrics(res, acceptedStatuses);
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

  // Final-Touches Phase 34 Stage 10.1 — randomized think-time to emulate user pacing during gate checks.
  const thinkRange = Math.max(0, THINK_TIME_MAX - THINK_TIME_MIN);
  sleep(THINK_TIME_MIN + Math.random() * thinkRange);
}

export function handleSummary(data) {
  // Final-Touches Phase 34 Stage 10.1 — keep gate summaries concise and machine-readable for orchestration.
  const txtPath = __ENV.SUMMARY_TXT_PATH;
  const p95 = data.metrics.api_duration ? data.metrics.api_duration.values['p(95)'] : 0;
  const errorRate = data.metrics.api_errors ? data.metrics.api_errors.values.rate : 0;
  const lines = [
    `phase10_gate=${GATE_NAME}`,
    `baseUrl=${BASE_URL}`,
    `targetUsers=${TARGET_USERS}`,
    `effectiveUsers=${EFFECTIVE_USERS}`,
    `targetRps=${TARGET_RPS}`,
    `shardTargetRps=${SHARD_TARGET_RPS}`,
    `generatorTotal=${GENERATOR_TOTAL}`,
    `generatorIndex=${GENERATOR_INDEX}`,
    `shardMaxVus=${SHARD_MAX_VUS}`,
    `localVuCap=${LOCAL_VU_CAP}`,
    `p95ThresholdMs=${P95_THRESHOLD_MS}`,
    `errorRateThreshold=${ERROR_RATE_THRESHOLD}`,
    `iterations=${data.metrics.iterations ? data.metrics.iterations.values.count : 0}`,
    `api_errors=${errorRate}`,
    `api_5xx=${data.metrics.api_5xx ? data.metrics.api_5xx.values.rate : 0}`,
    `api_429=${data.metrics.api_429 ? data.metrics.api_429.values.rate : 0}`,
    `api_4xx=${data.metrics.api_4xx ? data.metrics.api_4xx.values.rate : 0}`,
    `api_duration_p95=${p95}`,
  ];
  const text = `${lines.join('\n')}\n`;
  const output = { stdout: text };
  if (txtPath) {
    output[txtPath] = text;
  }
  return output;
}
