/**
 * Test user credentials and role definitions for all EduSphere roles.
 *
 * All passwords should be set via environment variables in .env (never commit real secrets).
 * The seeded test data in the DB should contain these users.
 */

export interface TestUser {
  username: string;
  passwordEnvVar: string;
  role: string;
  description: string;
  hasMfa: boolean;
  /** Institution type this user primarily belongs to */
  institutionType?: 'School' | 'College' | 'University';
}

// ── All test users ─────────────────────────────────────────────────────────

export const TEST_USERS: Record<string, TestUser> = {
  superAdmin: {
    username: process.env.SUPER_ADMIN_USERNAME || 'superadmin',
    passwordEnvVar: 'SUPER_ADMIN_PASSWORD',
    role: 'SuperAdmin',
    description: 'Full system access across all institutions',
    hasMfa: false,
  },

  adminUniversity: {
    username: process.env.ADMIN_USERNAME || 'admin.uni',
    passwordEnvVar: 'ADMIN_PASSWORD',
    role: 'Admin',
    description: 'Admin for University institution',
    hasMfa: false,
    institutionType: 'University',
  },

  adminSchool: {
    username: process.env.ADMIN_USERNAME || 'admin.sch',
    passwordEnvVar: 'ADMIN_PASSWORD',
    role: 'Admin',
    description: 'Admin for School institution',
    hasMfa: false,
    institutionType: 'School',
  },

  facultyUniversity: {
    username: process.env.FACULTY_USERNAME || 'faculty.uni',
    passwordEnvVar: 'FACULTY_PASSWORD',
    role: 'Faculty',
    description: 'Faculty member at University',
    hasMfa: false,
    institutionType: 'University',
  },

  facultySchool: {
    username: process.env.FACULTY_USERNAME || 'faculty.sch',
    passwordEnvVar: 'FACULTY_PASSWORD',
    role: 'Faculty',
    description: 'Faculty member at School',
    hasMfa: false,
    institutionType: 'School',
  },

  studentUniversity: {
    username: process.env.STUDENT_USERNAME || 'student.uni',
    passwordEnvVar: 'STUDENT_PASSWORD',
    role: 'Student',
    description: 'Student at University',
    hasMfa: false,
    institutionType: 'University',
  },

  studentSchool: {
    username: process.env.STUDENT_USERNAME || 'student.sch',
    passwordEnvVar: 'STUDENT_PASSWORD',
    role: 'Student',
    description: 'Student at School',
    hasMfa: false,
    institutionType: 'School',
  },

  finance: {
    username: process.env.FINANCE_USERNAME || 'finance.uni',
    passwordEnvVar: 'FINANCE_PASSWORD',
    role: 'Finance',
    description: 'Finance role with payment access',
    hasMfa: false,
  },

  mfaUser: {
    username: process.env.MFA_USERNAME || 'mfa-user@tabsan.edu',
    passwordEnvVar: 'MFA_PASSWORD',
    role: 'Faculty',
    description: 'User with MFA enabled',
    hasMfa: true,
  },
};

// ── Helper: resolve password from env ──────────────────────────────────────

/**
 * Get the password for a test user. Throws if env var is not set.
 */
export function getPassword(user: TestUser): string {
  const pw = process.env[user.passwordEnvVar];
  if (!pw) {
    throw new Error(
      `Password env var "${user.passwordEnvVar}" not set for user "${user.username}". ` +
      'Set it in tests/playwright/.env',
    );
  }
  return pw;
}

// ── Role groups for convenience ────────────────────────────────────────────

export const ALL_ROLES = Object.keys(TEST_USERS) as (keyof typeof TEST_USERS)[];

export const ADMIN_ROLES = ['superAdmin', 'adminUniversity', 'adminSchool'] as const;
export const FACULTY_ROLES = ['facultyUniversity', 'facultySchool'] as const;
export const STUDENT_ROLES = ['studentUniversity', 'studentSchool'] as const;
