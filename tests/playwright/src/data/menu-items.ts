/**
 * Expected sidebar menu items per role and institution type.
 *
 * This is the "source of truth" for authorization tests.
 * Each entry maps a menu label to the role(s) that should see it,
 * and optionally the institution type(s) where it applies.
 *
 * Keep this in sync with your sidebar configuration (Sidebar-Menu-Purpose.csv).
 */

export interface MenuExpectation {
  label: string;
  roles: string[];
  institutionTypes?: ('School' | 'College' | 'University')[];
  urlPattern: string;
  licenseFeature?: string;
}

// ── Complete menu map ──────────────────────────────────────────────────────

export const MENU_ITEMS: MenuExpectation[] = [
  // ── Common (all roles) ──
  { label: 'Dashboard', roles: ['SuperAdmin', 'Admin', 'Faculty', 'Student', 'Finance'], urlPattern: '/Portal/Dashboard' },
  { label: 'Profile', roles: ['SuperAdmin', 'Admin', 'Faculty', 'Student', 'Finance'], urlPattern: '/Portal/Profile' },
  { label: 'Notifications', roles: ['SuperAdmin', 'Admin', 'Faculty', 'Student', 'Finance'], urlPattern: '/Portal/Notifications' },

  // ── SuperAdmin only ──
  { label: 'System Settings', roles: ['SuperAdmin'], urlPattern: '/Portal/Settings' },
  { label: 'User Management', roles: ['SuperAdmin'], urlPattern: '/Portal/Users' },
  { label: 'Institution Management', roles: ['SuperAdmin'], urlPattern: '/Portal/Institutions' },
  { label: 'License Management', roles: ['SuperAdmin'], urlPattern: '/Portal/License' },
  { label: 'Audit Logs', roles: ['SuperAdmin'], urlPattern: '/Portal/AuditLogs' },

  // ── Admin ──
  { label: 'Students', roles: ['SuperAdmin', 'Admin', 'Faculty'], urlPattern: '/Portal/Students' },
  { label: 'Student Registration', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Students/Register' },
  { label: 'Faculty Management', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Faculty' },
  { label: 'Courses', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Courses' },
  { label: 'Departments', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Departments' },
  { label: 'Timetable', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Timetable' },
  { label: 'Study Plans', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/StudyPlans' },

  // ── Attendance ──
  { label: 'Enter Attendance', roles: ['SuperAdmin', 'Admin', 'Faculty'], urlPattern: '/Portal/Attendance/Enter' },
  { label: 'View Attendance', roles: ['SuperAdmin', 'Admin', 'Faculty', 'Student'], urlPattern: '/Portal/Attendance' },
  { label: 'Attendance Reports', roles: ['SuperAdmin', 'Admin', 'Faculty'], urlPattern: '/Portal/Attendance/Reports' },

  // ── Results ──
  { label: 'Enter Results', roles: ['SuperAdmin', 'Admin', 'Faculty'], urlPattern: '/Portal/Results/Enter' },
  { label: 'View Results', roles: ['SuperAdmin', 'Admin', 'Faculty', 'Student'], urlPattern: '/Portal/Results' },
  { label: 'Result Reports', roles: ['SuperAdmin', 'Admin', 'Faculty'], urlPattern: '/Portal/Results/Reports' },
  { label: 'Transcript', roles: ['SuperAdmin', 'Admin', 'Student'], urlPattern: '/Portal/Transcript' },

  // ── Certificates ──
  {
    label: 'Degree Certificate',
    roles: ['SuperAdmin', 'Admin', 'Student'],
    institutionTypes: ['University'],
    urlPattern: '/Portal/Certificates/Degree',
  },
  {
    label: 'Completion Certificate',
    roles: ['SuperAdmin', 'Admin', 'Student'],
    institutionTypes: ['School', 'College'],
    urlPattern: '/Portal/Certificates/Completion',
  },
  { label: 'Report Card', roles: ['SuperAdmin', 'Admin', 'Student'], urlPattern: '/Portal/Certificates/ReportCard' },

  // ── FYP (University only) ──
  {
    label: 'FYP Management',
    roles: ['SuperAdmin', 'Admin', 'Faculty'],
    institutionTypes: ['University'],
    urlPattern: '/Portal/FYP',
  },
  {
    label: 'FYP Submission',
    roles: ['Student'],
    institutionTypes: ['University'],
    urlPattern: '/Portal/FYP/Submit',
  },

  // ── Payments ──
  { label: 'Payments', roles: ['SuperAdmin', 'Admin', 'Finance'], urlPattern: '/Portal/Payments' },
  { label: 'My Payments', roles: ['Student'], urlPattern: '/Portal/Payments/My' },
  {
    label: 'Generate Invoice',
    roles: ['SuperAdmin', 'Admin', 'Finance'],
    urlPattern: '/Portal/Payments/Invoice',
  },

  // ── Reports ──
  { label: 'Reports', roles: ['SuperAdmin', 'Admin', 'Faculty'], urlPattern: '/Portal/Reports' },
  { label: 'Export Reports', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Reports/Export' },

  // ── Course Materials ──
  { label: 'Course Materials', roles: ['SuperAdmin', 'Admin', 'Faculty', 'Student'], urlPattern: '/Portal/CourseMaterials' },
  { label: 'Upload Material', roles: ['SuperAdmin', 'Admin', 'Faculty'], urlPattern: '/Portal/CourseMaterials/Upload' },

  // ── Communication ──
  { label: 'Send SMS', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Sms', licenseFeature: 'Sms' },
  { label: 'Send Email', roles: ['SuperAdmin', 'Admin'], urlPattern: '/Portal/Email' },

  // ── AI Chatbot (if licensed) ──
  { label: 'AI Assistant', roles: ['SuperAdmin', 'Admin', 'Faculty', 'Student'], urlPattern: '/Portal/Chatbot', licenseFeature: 'AiChatbot' },
];

// ── Helpers ────────────────────────────────────────────────────────────────

/**
 * Get all menu items expected to be visible for a given role.
 */
export function getExpectedMenusForRole(role: string, institutionType?: string): MenuExpectation[] {
  return MENU_ITEMS.filter((item) => {
    const roleMatch = item.roles.includes(role);
    const institutionMatch = !item.institutionTypes || (institutionType ? item.institutionTypes.includes(institutionType as any) : true);
    return roleMatch && institutionMatch;
  });
}

/**
 * Get all menu items expected to be HIDDEN for a given role.
 */
export function getHiddenMenusForRole(role: string, institutionType?: string): MenuExpectation[] {
  return MENU_ITEMS.filter((item) => {
    const roleMatch = !item.roles.includes(role);
    const institutionMismatch = item.institutionTypes && institutionType && !item.institutionTypes.includes(institutionType as any);
    return roleMatch || institutionMismatch;
  });
}
