/**
 * Role-based Playwright fixtures.
 *
 * Each fixture logs in as a specific role and provides a pre-authenticated page.
 * Tests using these fixtures start already logged in on the Dashboard.
 *
 * Usage in test files:
 *   import { test } from '@fixtures/role-fixtures';
 *   test('super admin can see settings', async ({ superAdminPage }) => { ... });
 */
import { test as base, Page } from '@playwright/test';
import { TEST_USERS, getPassword, TestUser } from '@data/test-users';
import { loginAs, logout } from '@helpers/auth';

// ── Type definition for our custom fixtures ────────────────────────────────

export type RoleFixtures = {
  /** Authenticated page for SuperAdmin */
  superAdminPage: Page;
  /** Authenticated page for University Admin */
  adminUniversityPage: Page;
  /** Authenticated page for School Admin */
  adminSchoolPage: Page;
  /** Authenticated page for University Faculty */
  facultyUniversityPage: Page;
  /** Authenticated page for School Faculty */
  facultySchoolPage: Page;
  /** Authenticated page for University Student */
  studentUniversityPage: Page;
  /** Authenticated page for School Student */
  studentSchoolPage: Page;
  /** Authenticated page for Finance */
  financePage: Page;
  /** Authenticated page for MFA-enabled user */
  mfaUserPage: Page;
};

// ── Create a role-authenticated page fixture ───────────────────────────────

/**
 * Factory: creates a fixture that logs in as the given test user.
 */
function createRoleFixture(userKey: keyof typeof TEST_USERS) {
  return async function ({ page }: { page: Page }, use: (page: Page) => Promise<void>) {
    const user: TestUser = TEST_USERS[userKey];
    const password = getPassword(user);

    await loginAs(page, {
      username: user.username,
      password,
      mfaExpected: user.hasMfa,
      totpSecret: user.hasMfa ? process.env.MFA_TOTP_SECRET : undefined,
    });

    await use(page);

    // Cleanup: log out after test
    await logout(page).catch(() => {
      // Ignore logout failures in cleanup
    });
  };
}

// ── Extend the base test with our fixtures ─────────────────────────────────

export const test = base.extend<RoleFixtures>({
  superAdminPage: createRoleFixture('superAdmin'),

  adminUniversityPage: createRoleFixture('adminUniversity'),

  adminSchoolPage: createRoleFixture('adminSchool'),

  facultyUniversityPage: createRoleFixture('facultyUniversity'),

  facultySchoolPage: createRoleFixture('facultySchool'),

  studentUniversityPage: createRoleFixture('studentUniversity'),

  studentSchoolPage: createRoleFixture('studentSchool'),

  financePage: createRoleFixture('finance'),

  mfaUserPage: createRoleFixture('mfaUser'),
});

export { expect } from '@playwright/test';
