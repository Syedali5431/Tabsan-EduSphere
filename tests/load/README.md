# Load Testing Suite — Tabsan-EduSphere

Current load scripts are aligned to live API routes under `api/v1/*` and local development URL `http://localhost:5181`.

Repository Sync Note (15 May 2026):
- Deployment automation now supports both Demo and Clean DB flows; load-test users should ensure target data state matches the chosen deployment mode.

**Current Focus**: auth and core authenticated flows (auth, dashboard, sidebar, notification inbox)

Phase 5 update:
- scale profiles (`k6-scale-50k.js`, `k6-scale-100k.js`, `k6-scale-1m.js`, `k6-scale-5m.js`) now use `ramping-arrival-rate` + randomized think-time.
- distributed sharding is supported through `GENERATOR_TOTAL` and `GENERATOR_INDEX`.
- summary-first output is the default; raw JSON output is diagnostics-only.

---

## 📁 Files in This Directory

| File | Purpose |
|------|---------|
| `k6-auth-current.js` | Auth-focused load test for `/api/v1/auth/login` + `/api/v1/auth/security-profile` |
| `k6-core-current.js` | Core authenticated flow test for dashboard, sidebar, and notifications |
| `k6-phase10-progressive.js` | Progressive gate scenario for incremental scale validation and bottleneck signal capture |
| `run-load-test.ps1` | PowerShell runner (recommended) |
| `run-load-test.bat` | Windows batch wrapper |
| `run-phase10-progressive.ps1` | Progressive gate orchestrator with bottleneck classification and retest loop |
| `README.md` | This file |

---

## 🚀 Quick Start

### 1. Install K6

```powershell
# Windows (Chocolatey)
choco install k6

# Or Windows (winget)
winget install k6

# macOS
brew install k6

# Linux
sudo apt-get install k6
```

### 2. Run Test (Recommended - PowerShell)

```powershell
# Auth suite (smoke)
.\run-load-test.ps1 -Suite auth -Profile smoke -Environment local

# Auth suite (load)
.\run-load-test.ps1 -Suite auth -Profile load -Environment local -OutputJson

# Core suite with credentials (dashboard/sidebar/notification)
.\run-load-test.ps1 -Suite core -Profile load -Environment local -TestUsername "admin" -TestPassword "Admin@123"
```

### 3. Direct K6 Execution

```bash
# Auth script direct
k6 run -e BASE_URL=http://localhost:5181 k6-auth-current.js

# Core script direct
k6 run -e BASE_URL=http://localhost:5181 -e TEST_USERNAME=admin -e TEST_PASSWORD=Admin@123 k6-core-current.js

# JSON output
k6 run --out json=results.json -e BASE_URL=http://localhost:5181 k6-auth-current.js
```

### 4. Scale Profile Execution (Phase 5)

```powershell
# 50k profile (single generator, summary-first)
.\run-50k.bat http://localhost:5181

# 100k profile, shard 1 of 4 generators, custom target RPS
.\run-100k.bat http://localhost:5181 summary 16000 4 1 320

# 1m profile diagnostics run with raw output enabled (focused troubleshooting)
.\run-1m.bat http://localhost:5181 raw 16000 1 1 900
```

### 5. Progressive Load Gates (Phase 10)

```powershell
# Default progressive plan: 10k -> 20k -> 50k -> 80k -> 100k
.\run-phase10-progressive.ps1 progressive http://localhost:5181

# Extended plan: adds 250k -> 500k -> 1m gates
.\run-phase10-progressive.ps1 extended http://localhost:5181

# Distributed run with 4 generators and one local re-test on failure
.\run-phase10-progressive.ps1 extended http://localhost:5181 -Distributed -GeneratorTotal 4 -GeneratorIndex 1 -RetestCount 2
```

Phase 10 uses the parameterized `k6-phase10-progressive.js` scenario to run each gate, capture `api_duration`/`api_errors`, and emit a bottleneck classification (`api`, `database/dependency`, `infra`, `infra/rate-limit`, or `contract/authz`) from the PowerShell wrapper.

---

## 📊 Test Scenarios

### Quick Reference

| Scenario | VUs | Duration | Purpose |
|----------|-----|----------|---------|
| **light** | 10 | 4m | Local development |
| **medium** | 1,000 | 14m | Staging validation |
| **high** | 10,000 | 27m | Production readiness |
| **extreme** | 1,000,000 | 60m+ | Scalability verification |
| **spike** | 100,000 | 6m | Sudden load recovery |
| **soak** | 1,000 | 120m | Memory leak detection |
| **stress** | 500,000 | 14m | Find breaking point |

### Example: Step-by-Step Load Testing

```powershell
# 1. Baseline (local)
.\run-load-test.ps1 light local
# Record metrics

# 2. Staging (medium)
.\run-load-test.ps1 medium staging -OutputJson

# 3. Staging (high)
.\run-load-test.ps1 high staging -OutputJson

# 4. Production (extreme + cloud)
.\run-load-test.ps1 extreme production
```

---

## ⚙️ Advanced Configuration

### Environment Variables

```powershell
# Set custom target URL
$env:TARGET_URL = "https://api.staging.example.com"

# Run test
.\run-load-test.ps1 high local
```

### Custom Scenarios

```powershell
# Run with custom parameters
.\run-load-test.ps1 custom -CustomArgs '--stage 2m:500 --stage 5m:500 --stage 2m:0'
```

### K6 Cloud Distributed Testing

```powershell
# First time: authenticate
k6 login cloud

# Run on cloud
.\run-load-test.ps1 extreme -CloudRun

# Or direct:
k6 cloud run login-load-test.js
```

### Distributed Generator Notes (Phase 5.2)

- `GENERATOR_TOTAL`: total number of generator machines.
- `GENERATOR_INDEX`: 1-based index of this generator.
- each generator runs its shard of `TARGET_RPS`; aggregate effective throughput is the sum of all generators.

### Output Discipline Notes (Phase 5.3)

- Batch scale runners default to `--quiet` + summary exports.
- `run-load-test.ps1` only emits raw JSON when both `-OutputJson` and `-AllowRawOutput` are provided.

---

## 📈 Understanding Results

### Key Metrics

```
Response Time:
├── Min: 45ms (fastest)
├── Avg: 187ms (average)
├── P95: 412ms (95% under this time)
├── P99: 956ms (99% under this time)
└── Max: 2341ms (slowest)

Errors: 127 / 50,000 = 0.25% error rate

Requests Per Second: 1,234,567 total / 3,600 sec = 343 RPS
```

### Health Assessment

**✅ Healthy System:**
- P95 < 500ms
- Error rate < 0.1%
- No sustained 500+ errors

**⚠️ Stressed System:**
- P95 > 1000ms
- Error rate 1-5%
- Connection pool warnings

**❌ Critical Issues:**
- P95 > 2000ms
- Error rate > 5%
- Server timeouts

---

## 🛠️ Optimization Recommendations

### Quick Wins (Apply First)

1. **Enable Connection Pooling** (database)
   ```csharp
   Max Pool Size=200; Min Pool Size=10;
   ```

2. **Use Async/Await** (code)
   ```csharp
   public async Task<IActionResult> Login(LoginRequest request)
   {
       var user = await _userRepository.GetByEmailAsync(request.Email);
   }
   ```

3. **Add Response Compression** (infrastructure)
   ```csharp
   services.AddResponseCompression();
   ```

### Comprehensive Optimization

See [LOAD_TESTING_GUIDE.md](LOAD_TESTING_GUIDE.md) for:
- Connection pool tuning
- Async implementation guide
- Redis caching setup
- Query optimization
- Rate limiting
- Horizontal scaling with Docker/Kubernetes
- Database indexing

---

## 🔄 Original Load Tests

**Legacy Test Scripts** (for Phase 31 certification):

```powershell
# Baseline test
k6 run tests/load/k6-baseline.js

# Certification bands
k6 run --env BAND=up-to-10k tests/load/k6-certification-bands.js
k6 run --env BAND=10k-100k tests/load/k6-certification-bands.js
k6 run --env BAND=100k-500k tests/load/k6-certification-bands.js
k6 run --env BAND=500k-1m tests/load/k6-certification-bands.js

# Recovery smoke test
powershell -ExecutionPolicy Bypass -File tests/load/recovery-smoke.ps1 -BaseUrl http://localhost:5181
```

---

## 🔧 Troubleshooting

### Connection Refused
```
Error: connection refused

Solution:
1. Verify API running: curl http://localhost:5000/health
2. Check TARGET_URL is correct
3. Verify firewall allows connections
```

### Rate Limited (429)
```
Error: Too many requests

Solution:
1. Reduce VU count or test duration
2. Use K6 Cloud for distributed load
3. Increase server rate limits
```

### Memory Exhausted
```
Error: Cannot allocate memory

Solution:
1. Reduce concurrent VUs
2. Use K6 Cloud instead
3. Run on more powerful machine
```

For more troubleshooting, see [LOAD_TESTING_GUIDE.md](LOAD_TESTING_GUIDE.md)

---

## 📋 Pre-Test Checklist

- [ ] K6 installed (`k6 version`)
- [ ] API running and accessible
- [ ] Database is connected
- [ ] Target URL verified
- [ ] Test user accounts exist
- [ ] Firewall allows connections
- [ ] Disk space available (for results)
- [ ] System resources available

---

## 📝 Legacy Test Information

The original load tests use thresholds to assert pass/fail:

- `http_req_duration['p(95)'] < 200` — p95 response time under 200 ms
- `http_req_failed < 0.01` — error rate below 1 %

A failed threshold exits with code 99 (non-zero), which will fail the CI step.

## Scenarios

| Scenario | VUs | Duration | Endpoints |
| --- | --- | --- | --- |
| Smoke | 1 | 30 s | All core endpoints |
| Baseline | 20 | 1 min | All core endpoints |
| Spike | 50 | 30 s | Dashboard + Auth |

## Stage 31.3 Certification Bands

The Stage 31.3 script (`k6-certification-bands.js`) maps required target bands to concrete VU and duration settings:

| Band | Script BAND value | VUs | Duration | p95 threshold |
| --- | --- | --- | --- | --- |
| Up to 10k users | `up-to-10k` | 20 | 2 min | < 250 ms |
| 10k to 100k users | `10k-100k` | 50 | 3 min | < 350 ms |
| 100k to 500k users | `100k-500k` | 100 | 4 min | < 500 ms |
| 500k to 1M+ users | `500k-1m` | 150 | 5 min | < 700 ms |

Common threshold for all bands:

- `error_rate < 2 %`

## Recovery Test

`recovery-smoke.ps1` validates node/service failure recovery by:

1. Starting the API and validating `/health` returns 200.
2. Running API startup with `--no-launch-profile` to avoid environment/profile drift.
3. Stopping the API process to simulate node failure.
4. Restarting the API process and polling `/health` until recovery or timeout.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
