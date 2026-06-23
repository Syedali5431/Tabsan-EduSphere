# EduSphere Playwright E2E Test Suite

Comprehensive end-to-end test suite for Tabsan EduSphere using [Playwright](https://playwright.dev/).

## Quick Start

```bash
# 1. Navigate to the test directory
cd tests/playwright

# 2. Install dependencies
npm install

# 3. Install browsers (first time only)
npx playwright install

# 4. Copy and configure environment
cp .env.example .env
# Edit .env with your credentials and URLs

# 5. Run all tests
npm test

# 6. View report
npm run report
```

## Project Structure

```
tests/playwright/
├── playwright.config.ts          # Playwright configuration
├── package.json                  # Dependencies & scripts
├── tsconfig.json                 # TypeScript configuration
├── .env.example                  # Environment template
├── src/
│   ├── helpers/
│   │   ├── auth.ts               # Login, MFA, session helpers
│   │   ├── navigation.ts         # Sidebar nav, breadcrumbs, URL checks
│   │   ├── filters.ts            # Cascading filter helpers
│   │   └── api.ts                # API-level assertions & export checks
│   ├── fixtures/
│   │   └── role-fixtures.ts      # Pre-authenticated per-role fixtures
│   ├── pages/
│   │   ├── login.page.ts         # Login POM
│   │   ├── dashboard.page.ts     # Dashboard POM
│   │   ├── students.page.ts      # Students POM
│   │   ├── attendance.page.ts    # Attendance POM
│   │   ├── results.page.ts       # Results POM
│   │   ├── certificates.page.ts  # Certificates POM
│   │   └── payments.page.ts      # Payments POM
│   └── data/
│       ├── test-users.ts         # Role credentials & env helpers
│       ├── menu-items.ts         # Expected sidebar per role
│       └── test-data.ts          # Seeded IDs, grading rules
└── tests/
    ├── auth/
    │   ├── login.spec.ts         # Login, logout, session timeout
    │   └── mfa.spec.ts           # MFA challenge flow
    ├── authorization/
    │   └── role-sidebar.spec.ts  # Sidebar visibility & direct URL blocks
    ├── modules/
    │   ├── students.spec.ts      # Student listing & filtering
    │   ├── attendance.spec.ts    # Attendance entry & verification
    │   ├── results.spec.ts       # GPA vs Percentage results
    │   ├── certificates.spec.ts  # Degree/Completion certs
    │   ├── fyp.spec.ts           # University-only FYP module
    │   ├── reports.spec.ts       # PDF/Excel/CSV exports
    │   ├── profile.spec.ts       # Profile picture upload
    │   └── payments.spec.ts      # Payment filtering & scoping
    └── edge-cases/
        └── edge-cases.spec.ts    # SQL injection, XSS, long input, etc.
```

## Test Tags

Run subsets of tests using grep:

| Tag | Scope |
|-----|-------|
| `@smoke` | Critical path tests (fast, run on every commit) |
| `@regression` | Full regression suite |
| `@auth` | Authentication & MFA |
| `@role-based` | Role-specific authorization |
| `@student` | Student module |
| `@attendance` | Attendance module |
| `@results` | Results module |
| `@certificates` | Certificates module |
| `@fyp` | FYP module (University-only) |
| `@reports` | Reports & exports |
| `@profile` | Profile features |
| `@payments` | Payments module |
| `@edge` | Edge cases & security |

### Examples

```bash
# Run only smoke tests
npm run test:smoke

# Run all auth tests
npm run test:auth

# Run role-based sidebar tests
npm run test:role-based

# Run in headed mode for debugging
npm run test:headed

# Run with Playwright debugger
npm run test:debug
```

## Role Fixtures

Pre-authenticated pages for each role. Simply destructure in your test:

```typescript
import { test, expect } from '@fixtures/role-fixtures';

test('SuperAdmin can access settings', async ({ superAdminPage }) => {
  await superAdminPage.goto('/Portal/Settings');
  await expect(superAdminPage.locator('h1')).toBeVisible();
});

test('Student is blocked from settings', async ({ studentUniversityPage }) => {
  await studentUniversityPage.goto('/Portal/Settings');
  // Should be redirected to dashboard or see access denied
  const isBlocked = studentUniversityPage.url().includes('/Dashboard');
  expect(isBlocked).toBeTruthy();
});
```

Available fixtures: `superAdminPage`, `adminUniversityPage`, `adminSchoolPage`, `facultyUniversityPage`, `facultySchoolPage`, `studentUniversityPage`, `studentSchoolPage`, `financePage`, `mfaUserPage`.

## Environment Variables

All credentials are read from `.env`. Never commit `.env` to git.

| Variable | Description |
|----------|-------------|
| `BASE_URL` | Web app URL (default: `http://localhost:5063`) |
| `API_BASE_URL` | API URL (default: `http://localhost:5181`) |
| `SUPER_ADMIN_USERNAME` | SuperAdmin username |
| `SUPER_ADMIN_PASSWORD` | SuperAdmin password |
| `ADMIN_PASSWORD` | Admin password |
| `FACULTY_PASSWORD` | Faculty password |
| `STUDENT_PASSWORD` | Student password |
| `FINANCE_PASSWORD` | Finance password |
| `MFA_TOTP_SECRET` | TOTP secret for MFA user (Base32) |

## CI/CD Integration

```yaml
# GitHub Actions example
- name: Run Playwright tests
  run: |
    cd tests/playwright
    npm ci
    npx playwright install --with-deps chromium
    npm test
  env:
    SUPER_ADMIN_PASSWORD: ${{ secrets.SUPER_ADMIN_PASSWORD }}
    MFA_TOTP_SECRET: ${{ secrets.MFA_TOTP_SECRET }}
```

## Writing New Tests

1. **Use page objects** – Add selectors to `src/pages/`
2. **Use helpers** – Login via `auth.ts`, filters via `filters.ts`
3. **Use fixtures** – Extend `role-fixtures.ts` for new roles
4. **Tag your tests** – Add `@smoke`, `@regression`, or module tags
5. **Keep tests independent** – Each test should set up its own state
