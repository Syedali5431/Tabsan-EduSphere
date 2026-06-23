/**
 * Test data constants – seeded IDs, filter values, and reference data
 * used across test specs.
 *
 * Update these to match your seeded database (Scripts/03-FullDummyData.sql).
 */

// ── Seeded Institution IDs ──────────────────────────────────────────────────

export const INSTITUTIONS = {
  demoUniversity: {
    name: 'Demo University',
    id: '11111111-1111-1111-1111-111111111101',
    type: 'University' as const,
  },
  demoSchool: {
    name: 'Demo School',
    id: '11111111-1111-1111-1111-111111111102',
    type: 'School' as const,
  },
  demoCollege: {
    name: 'Demo College',
    id: '11111111-1111-1111-1111-111111111103',
    type: 'College' as const,
  },
} as const;

// ── Seeded Department IDs ──────────────────────────────────────────────────

export const DEPARTMENTS = {
  computerScience: {
    name: 'Computer Science',
    id: '22222222-2222-2222-2222-222222222201',
    institutionId: INSTITUTIONS.demoUniversity.id,
  },
  mathematics: {
    name: 'Mathematics',
    id: '22222222-2222-2222-2222-222222222202',
    institutionId: INSTITUTIONS.demoUniversity.id,
  },
  schoolScience: {
    name: 'Science Department',
    id: '22222222-2222-2222-2222-222222222203',
    institutionId: INSTITUTIONS.demoSchool.id,
  },
} as const;

// ── Seeded Course IDs ──────────────────────────────────────────────────────

export const COURSES = {
  bscs: {
    name: 'BSCS',
    id: '33333333-3333-3333-3333-333333333301',
    departmentId: DEPARTMENTS.computerScience.id,
    type: 'University' as const,
  },
  bsMath: {
    name: 'BS Mathematics',
    id: '33333333-3333-3333-3333-333333333302',
    departmentId: DEPARTMENTS.mathematics.id,
    type: 'University' as const,
  },
  class9: {
    name: 'Class 9',
    id: '33333333-3333-3333-3333-333333333303',
    departmentId: DEPARTMENTS.schoolScience.id,
    type: 'School' as const,
  },
} as const;

// ── Seeded Semester / Class IDs ────────────────────────────────────────────

export const SEMESTERS = {
  semester1: { name: 'Semester 1', id: '44444444-4444-4444-4444-444444444401' },
  semester2: { name: 'Semester 2', id: '44444444-4444-4444-4444-444444444402' },
};

// ── Seeded Student IDs ─────────────────────────────────────────────────────

export const STUDENTS = {
  uniStudent1: { name: 'Demo University Student 1', id: '55555555-5555-5555-5555-555555555501' },
  uniStudent2: { name: 'Demo University Student 2', id: '55555555-5555-5555-5555-555555555502' },
  schoolStudent1: { name: 'Demo School Student 1', id: '55555555-5555-5555-5555-555555555503' },
} as const;

// ── Grading thresholds ─────────────────────────────────────────────────────

export const GRADING = {
  university: {
    gpaMax: 4.0,
    gradePoints: {
      A: { min: 85, gpa: 4.0 },
      B: { min: 70, gpa: 3.0 },
      C: { min: 60, gpa: 2.0 },
      D: { min: 50, gpa: 1.0 },
      F: { min: 0, gpa: 0.0 },
    },
  },
  school: {
    percentageMax: 100,
    grades: {
      'A+': { min: 90 },
      A: { min: 80 },
      B: { min: 70 },
      C: { min: 60 },
      D: { min: 50 },
      F: { min: 0 },
    },
  },
} as const;

// ── Certificate eligibility rules ──────────────────────────────────────────

export const CERTIFICATE_RULES = {
  degree: {
    institutionTypes: ['University'],
    minGpa: 2.0,
    minCredits: 120,
    description: 'Requires University enrolment, min 2.0 GPA, 120 credits',
  },
  completion: {
    institutionTypes: ['School', 'College'],
    minPercentage: 50,
    description: 'Requires School/College enrolment, min 50% aggregate',
  },
  reportCard: {
    institutionTypes: ['School', 'College', 'University'],
    description: 'Available for all institution types',
  },
  transcript: {
    institutionTypes: ['School', 'College', 'University'],
    description: 'Available for all institution types',
  },
} as const;

// ── Export format types ────────────────────────────────────────────────────

export const EXPORT_FORMATS = {
  excel: { label: 'Excel', contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', extension: '.xlsx' },
  csv: { label: 'CSV', contentType: 'text/csv', extension: '.csv' },
  pdf: { label: 'PDF', contentType: 'application/pdf', extension: '.pdf' },
} as const;
