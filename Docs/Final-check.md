# Final Check — Tabsan EduSphere

> **Purpose**: Phased validation plan to ensure the application is production-ready across all modules, license enforcement, role-based access, MFA, filters, reports, sidebar menus, and data integrity.
>
> **Date**: 2026-06-12
>
> **Status**: ⬜ Not Started | 🔄 In Progress | ✅ Completed

---

## Documentation Sync Policy

After **every phase** is completed, the following documentation files MUST be updated to reflect changes:

| Document | Update Rule |
|----------|-------------|
| `Docs/Functionality.md` | Append implementation sync and validation sync entries (date-stamped, same format as existing entries). |
| `Docs/Function-List.md` | Add new functions/services/endpoints introduced. **Ensure no repetition** — check existing rows before adding. |
| `Project startup Docs/Development Plan - ASP.NET.md` | Mark completed items; add any new tasks discovered during the phase. |
| `Project startup Docs/Modules.md` | Update module status, dependencies, and configuration notes. |
| `Project startup Docs/PRD.md` | Reflect feature completion, scope changes, and acceptance criteria closure. |

> ⚠️ **Critical**: `Function-List.md` must never contain duplicate entries. Always search for the function name before appending.

---

## Phase 1: Database Script Validation

**Goal**: Verify all DB scripts (`Scripts/`) contain proper, consistent, and deployable data.

### 1.1 Schema Script (`01-Schema-Current.sql`)
- [ ] Run against a fresh database — confirm zero errors.
- [ ] Verify all tables, indexes, constraints, and foreign keys match the current EF Core model.
- [ ] Confirm `InstitutionType` column exists where needed (School=1, College=2, University=0).
- [ ] Confirm `IsActive` flags exist on tenants, campuses, courses, departments, users.

### 1.2 Core Seed Script (`02-Seed-Core.sql`)
- [ ] Verify SuperAdmin user is seeded with correct role.
- [ ] Verify required lookup data (roles, statuses, grading configs) is present.
- [ ] Confirm no duplicate IDs or constraint violations.

### 1.3 Full Dummy Data (`03-FullDummyData.sql`)
- [ ] Run against development DB — confirm zero errors.
- [ ] Verify School demo data: Class 1–Class 10 for all scoped students.
- [ ] Verify College demo data: Class 11–Class 12 for all scoped students.
- [ ] Verify University demo data: Semester 1–Semester 8, FYP entries, enrollments, attendance.
- [ ] Verify timetable admin demo entries (`25252525-...` GUIDs).
- [ ] Verify course material demo entries (`27272727-...` GUIDs).
- [ ] Verify Study Plan demo seed rows exist.
- [ ] Verify payment receipt demo rows exist for finance testing.

### 1.4 Maintenance Script (`04-Maintenance-Indexes-And-Views.sql`)
- [ ] Confirm all indexes are appropriate for current query patterns.
- [ ] Verify views return expected data.

### 1.5 Post-Deployment Checks (`05-PostDeployment-Checks.sql`)
- [ ] Run against seeded DB — all checks should pass (0 anomalies).
- [ ] Verify CourseMaterial unsupported file extension count = 0.
- [ ] Verify allowed extensions (.doc, .docx, .ppt, .pptx, .pdf, .txt, .xls, .xlsx, .jpg, .jpeg, .png).
- [ ] Verify attendance, result, enrollment counts are consistent.

### 1.6 Cleanup Script (`00-Cleanup-Master-Mistake.sql`)
- [ ] Review for any obsolete cleanup that may conflict with current schema.

### 1.7 SuperAdmin Script (`06-Create-SuperAdmin-User.sql`)
- [ ] Verify it creates a functional SuperAdmin account with all privileges.

### Phase 1 — Implementation Summary

> _Fill after phase completion: what was implemented, files changed, scripts modified._

### Phase 1 — Validation Summary

> _Fill after phase completion: test results, errors found & fixed, build status, manual verification notes._

---

## Phase 2: License-Based Institute Enforcement

**Goal**: Menus, filters, and options hide/show based on licensed institutes. University features (Graduation, FYP, Degree Audit, Degree Rules) must NOT appear for School/College.

### 2.1 License Structure Validation
- [ ] Verify license format includes `AllowedInstitutionTypes` array.
- [ ] Verify license parsing extracts School (1), College (2), University (0) correctly.
- [ ] Confirm `IEduApiClient.GetSecurityProfileAsync()` returns `LicensedInstitutionTypes`.
- [ ] Confirm `ApiConnectionModel` carries `LicensedInstitutionTypes` and `InstitutionType`.

### 2.2 University-Only Features Hidden for School/College
- [ ] `degree_audit` — hidden when license has no University.
- [ ] `degree_rules` — hidden when license has no University.
- [ ] `graduation_eligibility` — hidden when license has no University.
- [ ] `graduation_apply` — hidden when license has no University.
- [ ] `graduation_applications` — hidden when license has no University.
- [ ] `fyp` — hidden when license has no University.
- [ ] `study_plan` — semester-based logic hidden for School/College.

### 2.3 Institution-Type Filter Behavior
- [ ] Institute filter only shows licensed institution types.
- [ ] If license has only School, only School options appear.
- [ ] If license has School+College, both appear.
- [ ] If license has all three, all three appear.
- [ ] `ResolvePeriodFilterLabel` returns "Semester" for University, "Class" for School/College.

### 2.4 Certificate Document Types
- [ ] University: Transcript, Degree, Enrollment Verification, etc.
- [ ] School/College: only applicable certificate types (no Degree/Transcript).

### Phase 2 — Implementation Summary

> _Fill after phase completion: what was implemented, files changed, license parsing changes._

### Phase 2 — Validation Summary

> _Fill after phase completion: test results, institute-type filtering verified, hidden menus confirmed._

---

## Phase 3: Multi-Factor Authentication (MFA)

**Goal**: MFA works for users who enable it. Must accept valid TOTP codes from Gmail Authenticator, Microsoft Authenticator, and Authy. Must reject invalid/random codes.

### 3.1 MFA Enrollment Flow
- [ ] User navigates to `TwoFactorSettings` in portal.
- [ ] `BeginTwoFactorSetup` generates a valid Base32 TOTP secret.
- [ ] QR code / manual key displayed for scanning into authenticator app.
- [ ] `VerifyTwoFactorSetup` confirms enrollment with a valid TOTP code.
- [ ] Recovery codes generated and displayed (one-time view).

### 3.2 MFA Login Flow
- [ ] Login with correct password → returns `MFA_CODE_REQUIRED`.
- [ ] Submit valid TOTP code from Gmail Authenticator → login succeeds.
- [ ] Submit valid TOTP code from Microsoft Authenticator → login succeeds.
- [ ] Submit valid TOTP code from Authy → login succeeds.
- [ ] Submit invalid/random 6-digit code → returns `INVALID_MFA_CODE` (401).
- [ ] Submit expired TOTP (wait >30s) → returns `INVALID_MFA_CODE`.
- [ ] Submit empty code → returns `MFA_CODE_REQUIRED` (400).

### 3.3 MFA Disable Flow
- [ ] `DisableTwoFactor` removes TOTP secret from `TwoFactorStateStore`.
- [ ] After disable, login no longer prompts for MFA code.

### 3.4 MFA Reset Flow
- [ ] `Reset` endpoint with valid recovery code resets MFA.
- [ ] `ResendRecoveryCodes` generates new recovery codes.

### 3.5 MFA Secret Storage
- [ ] Base32 raw secret survives Data Protection key rotation.
- [ ] `HardDeleteAsync` permanently removes MFA data.
- [ ] `IsValidBase32Secret` validates secret format.

### Phase 3 — Implementation Summary

> _Fill after phase completion: what was implemented, MFA flow changes, authenticator compatibility tested._

### Phase 3 — Validation Summary

> _Fill after phase completion: Gmail/MS/Authy TOTP tests, invalid code rejection, disable/reset flow verified._

---

## Phase 4: Sidebar Menu Data & Functionality

**Goal**: All sidebar menus have proper data seeded. Every menu link navigates to a working page with real data.

### 4.1 Core Menus
| Menu Key | Verify Data Exists | Page Loads | CRUD Works |
|----------|-------------------|------------|------------|
| `dashboard` | KPIs, alerts, shortcuts | ⬜ | N/A |
| `timetable_admin` | Timetable entries | ⬜ | ⬜ |
| `timetable_teacher` | Teacher schedule | ⬜ | N/A |
| `timetable_student` | Student schedule | ⬜ | N/A |
| `lookups` | Reference data | ⬜ | ⬜ |
| `buildings` | Building records | ⬜ | ⬜ |
| `rooms` | Room records | ⬜ | ⬜ |
| `notifications` | Notification entries | ⬜ | ⬜ |
| `students` | Student profiles | ⬜ | ⬜ |
| `departments` | Department records | ⬜ | ⬜ |
| `courses` | Course catalog | ⬜ | ⬜ |
| `generate_certificates` | Certificate templates | ⬜ | ⬜ |
| `assignments` | Assignment entries | ⬜ | ⬜ |
| `attendance` | Attendance records | ⬜ | ⬜ |
| `enter_attendance` | Faculty entry page | ⬜ | ⬜ |
| `enter_results` | Faculty result entry | ⬜ | ⬜ |
| `results` | Published results | ⬜ | ⬜ |
| `quizzes` | Quiz entries | ⬜ | ⬜ |
| `fyp` | FYP projects | ⬜ | ⬜ |
| `analytics` | Charts & KPIs | ⬜ | N/A |
| `ai_chat` | AI assistant | ⬜ | N/A |
| `student_lifecycle` | Lifecycle states | ⬜ | ⬜ |
| `payments` | Payment receipts | ⬜ | ⬜ |
| `enrollments` | Enrollment records | ⬜ | ⬜ |
| `report_center` | Report definitions | ⬜ | ⬜ |

### 4.2 Settings Menus (SuperAdmin Only)
| Menu Key | Verify Data Exists | Page Loads | CRUD Works |
|----------|-------------------|------------|------------|
| `system_settings` | Config values | ⬜ | ⬜ |
| `report_settings` | Report configs | ⬜ | ⬜ |
| `sidebar_settings` | Menu visibility | ⬜ | ⬜ |
| `dashboard_settings` | Branding config | ⬜ | ⬜ |
| `license_update` | License upload | ⬜ | ⬜ |
| `institution_policy` | Policy config | ⬜ | ⬜ |
| `module_composition` | Module toggles | ⬜ | ⬜ |
| `result_calculation` | GPA rules | ⬜ | ⬜ |
| `theme_settings` | Theme config | ⬜ | ⬜ |
| `admin_users` | Admin accounts | ⬜ | ⬜ |
| `tenant_management` | Tenant records | ⬜ | ⬜ |
| `campus_management` | Campus records | ⬜ | ⬜ |

### 4.3 Feature Menus
| Menu Key | Verify Data Exists | Page Loads | CRUD Works |
|----------|-------------------|------------|------------|
| `helpdesk` | Ticket entries | ⬜ | ⬜ |
| `prerequisites` | Prereq rules | ⬜ | ⬜ |
| `gradebook` | Grade entries | ⬜ | ⬜ |
| `rubric_manage` | Rubric definitions | ⬜ | ⬜ |
| `degree_audit` | Audit results | ⬜ | ⬜ |
| `graduation_eligibility` | Eligibility checks | ⬜ | ⬜ |
| `degree_rules` | Rule configs | ⬜ | ⬜ |
| `graduation_apply` | Applications | ⬜ | ⬜ |
| `graduation_applications` | Application queue | ⬜ | ⬜ |
| `grading_config` | Grading profiles | ⬜ | ⬜ |
| `lms_manage` | LMS settings | ⬜ | ⬜ |
| `course_material` | Uploaded files | ⬜ | ⬜ |
| `discussion` | Discussion threads | ⬜ | ⬜ |
| `announcements` | Announcements | ⬜ | ⬜ |
| `study_plan` | Plan entries | ⬜ | ⬜ |
| `library_config` | Library settings | ⬜ | ⬜ |
| `accreditation` | Accreditation data | ⬜ | ⬜ |
| `user_import` | Import templates | ⬜ | ⬜ |
| `programs` | Program records | ⬜ | ⬜ |

### Phase 4 — Implementation Summary

> _Fill after phase completion: which menus had data seeded, pages created/fixed, CRUD verified._

### Phase 4 — Validation Summary

> _Fill after phase completion: menu-by-menu load test results, data integrity checks, broken links found & fixed._

---

## Phase 5: Reports Validation

**Goal**: All reports render correctly. Reports are filtered by licensed institute type. Unwanted reports are hidden per institute.

### 5.1 Report Center Access
- [ ] `report_center` menu visible per role and license.
- [ ] Report list filtered to licensed institution types only.
- [ ] University-only reports hidden for School/College license.

### 5.2 Report Generation
- [ ] Attendance Report — renders with ProgramName + DepartmentName columns.
- [ ] Result Report — renders with ProgramName + DepartmentName columns.
- [ ] Assignment Report — renders with ProgramName + DepartmentName columns.
- [ ] Quiz Report — renders with ProgramName + DepartmentName columns.
- [ ] Payment Report — renders correctly with receipt details.
- [ ] Enrollment Report — renders with student/offering data.
- [ ] All reports work without requiring department/course filter pre-selection.

### 5.3 Report Export
- [ ] PDF export produces valid PDF.
- [ ] CSV export produces valid CSV with correct headers.
- [ ] Excel export produces valid XLSX.

### 5.4 License-Based Report Filtering
- [ ] Reports containing University-specific data (FYP, Degree, Graduation) hidden for School/College.
- [ ] Reports containing School-specific data shown only for School licenses.
- [ ] Reports containing College-specific data shown only for College licenses.

### Phase 5 — Implementation Summary

> _Fill after phase completion: report endpoints updated, column additions, license filtering logic._

### Phase 5 — Validation Summary

> _Fill after phase completion: PDF/CSV/Excel export verified, license-filtered report visibility confirmed._

---

## Phase 6: Role-Based Sidebar Menu Restrictions

**Goal**: Each role sees ONLY the menus they are authorized for, as defined in the sidebar permission matrix.

### 6.1 SuperAdmin (All Access)
- [ ] Sees ALL sidebars: Core + Settings + Feature.
- [ ] Can View / Add / Edit / Enable / Disable everything.
- [ ] Only role that can DEACTIVATE anything in the system.
- [ ] Settings menus visible: `system_settings`, `report_settings`, `sidebar_settings`, `dashboard_settings`, `license_update`, `institution_policy`, `module_composition`, `result_calculation`, `admin_users`, `tenant_management`, `campus_management`.

### 6.2 Admin
- [ ] Sees all Core + Feature menus.
- [ ] **Hidden Settings menus**: `dashboard`, `module_composition`, `sidebar_settings`, `license_update`, `dashboard_settings`, `institution_policy`, `library_config`, `system_settings`, `report_settings`, `admin_users`, `tenant_management`, `campus_management`.
- [ ] Visible Settings menus: `result_calculation` (scoped), `theme_settings`.
- [ ] Can View / Add / Edit (no Deactivate).

### 6.3 Faculty
- [ ] **Visible menus**: `assignments`, `course_material`, `discussion`, `announcements`, `quizzes` (view only), `timetable_student` (view), `timetable_teacher` (view), `enter_attendance`, `attendance`, `enter_results`, `results` (scoped), `degree_audit` (scoped), `graduation_apply` (view), `study_plan` (advisor scope), `fyp`, `helpdesk` (assigned), `report_center` (scoped), `ai_chat`, `theme_settings` (own scope), `notifications`, `analytics` (scoped by tenant/campus).
- [ ] **Can deactivate**: Assignments (mark complete), Quizzes (mark complete).
- [ ] **No access to**: Settings menus (except `theme_settings` own scope, `result_calculation` view).
- [ ] **No access to**: `payments`, `students` (full list), `timetable_admin`, `lookups`, `buildings`, `rooms`, `departments`, `courses` (full CRUD), `generate_certificates`, `student_lifecycle`, `enrollments` (full CRUD), `user_import`, `programs`, `prerequisites`, `rubric_manage`, `gradebook`, `graduation_eligibility`, `degree_rules`, `lms_manage`, `library_config`, `accreditation`, `user_import`.
- [ ] Can update own mobile, address, email in user settings.
- [ ] Can apply themes on own user only.

### 6.4 Student
- [ ] **Visible menus**: `assignments` (view + submit), `course_material` (view + download), `announcements` (view), `quizzes` (view + attempt), `timetable_student` (view), `attendance` (view), `results` (view), `degree_audit` (view), `graduation_apply` (apply), `study_plan` (own plan), `fyp` (own project), `payments` (view own + mark complete → send to finance), `discussion` (create + view own), `ai_chat` (view), `theme_settings` (own scope), `notifications` (view), `helpdesk` (add + view own tickets), `generate_certificates` (view own).
- [ ] Can enable/disable Two-Factor Authentication on own account.
- [ ] Can update own mobile, address, email in user settings.
- [ ] **Hidden**: All admin/faculty CRUD menus, all Settings, `report_center`, `analytics`, `enter_attendance`, `enter_results`, `student_lifecycle`, `timetable_admin`, `timetable_teacher`.

### 6.5 Finance
- [ ] **Visible menus**: `payments` (add/edit/view/mark as paid), `theme_settings` (own scope), `report_center` (payment reports only), `analytics` (payment-related graphs only).
- [ ] **Hidden**: All other menus.
- [ ] Can update own mobile, address, email in user settings.
- [ ] Can apply themes on own user only.

### 6.6 Permission Deactivation Rule
- [ ] **Only SuperAdmin** can deactivate (set `IsActive = false`) any entity: users, tenants, campuses, courses, departments, menus, modules.
- [ ] Admin's "Deactivate" is scoped to non-system-critical entities (e.g., assignments, quizzes as "mark complete").
- [ ] No other role has deactivation capability.

### Phase 6 — Implementation Summary

> _Fill after phase completion: sidebar guard changes, role-based visibility logic, Finance scope enforcement._

### Phase 6 — Validation Summary

> _Fill after phase completion: login as each role, verify visible/hidden menus match matrix, no cross-role leaks._

---

## Phase 7: FYP Result Entry & Transcript Integration

**Goal**: When FYP is marked "Completed", the system prompts for FYP result entry. The FYP result is included in the transcript.

### 7.1 FYP Completion Flow
- [ ] Faculty/Admin marks FYP project status as "Completed".
- [ ] System triggers prompt to enter FYP result (grade/marks).
- [ ] FYP result entry form appears with proper validation.

### 7.2 FYP Result Validation
- [ ] Only completed FYPs can have results entered.
- [ ] Result cannot be entered twice without correction workflow.
- [ ] FYP result respects grading config (GPA scale, pass threshold).

### 7.3 Transcript Integration
- [ ] FYP result appears in the student's transcript.
- [ ] FYP result is included in GPA/CGPA calculation.
- [ ] `generate_certificates` includes FYP in degree audit summary.
- [ ] `degree_audit` counts FYP as a completed requirement.

### 7.4 University-Only Feature
- [ ] FYP result entry hidden for School and College.
- [ ] FYP transcript entry hidden for School and College.

### Phase 7 — Implementation Summary

> _Fill after phase completion: FYP completion trigger, result entry form, transcript/GPA integration._

### Phase 7 — Validation Summary

> _Fill after phase completion: FYP completion→result→transcript workflow tested end-to-end, School/College exclusion verified._

---

## Phase 8: Cascading Filter System

**Goal**: Filters cascade in order: Institute → Department → Course → Semester/Class. Labels and values change dynamically based on institution type and course selection.

### 8.1 Filter Order
```
Institute → Department → Course → Semester/Class
```
- [ ] All dropdowns are sorted alphabetically/numerically.
- [ ] Next filter loads only after current filter is selected.
- [ ] Previously selected values persist when changing higher-level filters (with intelligent reset).

### 8.2 Institute Type Behavior

#### University
- [ ] After Course selection, label shows **"Semester"**.
- [ ] Semester list shows **Semester 1 → Semester N** (ascending by StartDate).
- [ ] N depends on course duration (e.g., 8-semester course shows Semester 1–8, 4-semester course shows Semester 1–4).
- [ ] Semester list is filtered to the selected course's active semesters.

#### School
- [ ] After Course selection, label shows **"Class"**.
- [ ] Class list shows **Class 1 → Class 10**.
- [ ] Only classes applicable to the selected course appear.

#### College
- [ ] After Course selection, label shows **"Class"**.
- [ ] Class list shows **Class 11 → Class 12**.
- [ ] Only classes applicable to the selected course appear.

### 8.3 Filter Scope by Page
- [ ] `enter_attendance` — cascading filters + date picker.
- [ ] `enter_results` — cascading filters with write-guard (all filters required before entry).
- [ ] `attendance` — cascading filters for viewing.
- [ ] `results` — cascading filters for viewing.
- [ ] `assignments` — cascading filters.
- [ ] `quizzes` — cascading filters.
- [ ] `report_center` — cascading filters for report generation.
- [ ] `timetable_admin` — cascading filters.
- [ ] `enrollments` — cascading filters.

### 8.4 Filter Reset Behavior
- [ ] Changing Institute resets Department, Course, Semester/Class.
- [ ] Changing Department resets Course, Semester/Class.
- [ ] Changing Course resets Semester/Class.
- [ ] Semester/Class options update based on course's institution type and duration.

### Phase 8 — Implementation Summary

> _Fill after phase completion: cascading filter logic, dynamic label changes, institution-type-aware dropdowns._

### Phase 8 — Validation Summary

> _Fill after phase completion: filter cascade tested for Uni/School/College, label changes verified, reset behavior confirmed._

---

## Phase 9: Filter Visibility by Role

**Goal**: Users see only the filters relevant to their role and assignment.

### 9.1 Admin & Faculty
- [ ] **Institute filter HIDDEN** — institute is pre-assigned.
- [ ] **Tenant filter HIDDEN** — tenant is pre-assigned.
- [ ] Department filter shown (filtered to assigned departments if scoped).
- [ ] Course filter shown (filtered to assigned courses if scoped).
- [ ] Semester/Class filter shown (dynamic label).

### 9.2 Student
- [ ] **Institute filter HIDDEN** — institute is pre-assigned.
- [ ] **Tenant filter HIDDEN** — tenant is pre-assigned.
- [ ] **Department filter HIDDEN** — department is pre-assigned.
- [ ] **Course filter HIDDEN** — course is pre-assigned.
- [ ] **Only Class/Semester filter shown** — student selects their active class/semester.

### 9.3 SuperAdmin
- [ ] All filters visible (Institute, Tenant, Department, Course, Semester/Class).
- [ ] Institute filter shows all licensed institution types.

### 9.4 Finance
- [ ] Institute filter HIDDEN.
- [ ] Tenant filter HIDDEN.
- [ ] May see Department/Course filters scoped to payment-related views.

### Phase 9 — Implementation Summary

> _Fill after phase completion: role-based filter visibility, pre-assigned institute/tenant/department/course for restricted roles._

### Phase 9 — Validation Summary

> _Fill after phase completion: Admin/Faculty no tenant filter, Student only Class filter, SuperAdmin all filters visible._

---

## Phase 10: Deactivation Authority — SuperAdmin Only

**Goal**: Only SuperAdmin can deactivate (set `IsActive = false`) any entity. No other role has this power.

### 10.1 Entities Covered
- [ ] Users (Admin, Faculty, Student, Finance)
- [ ] Tenants
- [ ] Campuses
- [ ] Courses
- [ ] Departments
- [ ] Programs
- [ ] Sidebar menus (via `sidebar_settings`)
- [ ] Modules (via `module_composition`)
- [ ] Reports (via `report_settings`)
- [ ] Timetables
- [ ] Assignments (Admin can "mark complete" — not true deactivation)
- [ ] Quizzes (Admin can "mark complete" — not true deactivation)

### 10.2 API Enforcement
- [ ] Deactivation endpoints check `User.IsInRole("SuperAdmin")`.
- [ ] Non-SuperAdmin deactivation attempts return `403 Forbidden`.
- [ ] UI hides deactivation buttons/toggles from non-SuperAdmin users.

### 10.3 Reactivation
- [ ] Only SuperAdmin can reactivate deactivated entities.
- [ ] Reactivation restores entity to active state with audit trail.

### Phase 10 — Implementation Summary

> _Fill after phase completion: deactivation guards on all endpoints, UI button hiding, 403 enforcement._

### Phase 10 — Validation Summary

> _Fill after phase completion: non-SuperAdmin deactivation attempts return 403, UI buttons hidden, reactivation tested._

---

## Phase 11: Complete Role-Permission Matrix

**Goal**: Every role has exactly the permissions defined below. No leaks, no missing access.

### 11.1 SuperAdmin
| Category | Access |
|----------|--------|
| **All Sidebars** | View, Add, Edit, Enable, Disable |
| **Dashboard** | View with override |
| **Settings (all)** | Full CRUD + Deactivate |
| **Users** | Create, Edit, Deactivate, Reactivate |
| **License** | Upload, Validate, Activate |
| **Deactivation** | ONLY role that can deactivate anything |

### 11.2 Admin
| Category | Access |
|----------|--------|
| **Core Menus** | All except `dashboard` |
| **Feature Menus** | All |
| **Settings - Visible** | `result_calculation` (scoped), `theme_settings` |
| **Settings - Hidden** | `system_settings`, `report_settings`, `sidebar_settings`, `dashboard_settings`, `license_update`, `institution_policy`, `module_composition`, `admin_users`, `tenant_management`, `campus_management`, `library_config` |
| **Capabilities** | View, Add, Edit (NO Deactivate) |
| **User Settings** | Update own mobile, address, email |

### 11.3 Faculty
| Category | Access |
|----------|--------|
| **Teaching** | `assignments` (Add/Edit/View/Deactivate complete), `course_material` (Add/Edit/View/Deactivate), `discussion` (Add/Edit/View), `announcements` (Add/Edit/View assigned), `quizzes` (Add/Edit/View/Deactivate complete) |
| **Attendance** | `enter_attendance` (Add/Edit/View), `attendance` (View) |
| **Results** | `enter_results` (Add/Edit/View), `results` (View scoped), `gradebook` (Add/Edit/View) |
| **Academic** | `degree_audit` (View scoped), `graduation_apply` (View), `study_plan` (Add/Edit/View advisor scope), `fyp` (Add/Edit/View) |
| **Support** | `helpdesk` (Add/Edit/View assigned), `report_center` (View/Run/Export scoped) |
| **Timetable** | `timetable_student` (View), `timetable_teacher` (View) |
| **Personal** | `theme_settings` (Edit own), `notifications` (View), `ai_chat` (View), `analytics` (View scoped by tenant/campus) |
| **User Settings** | Update own mobile, address, email |
| **Hidden** | All Settings (except `theme_settings` own scope), `payments`, `students` full list, `timetable_admin`, `lookups`, `buildings`, `rooms`, `departments`, `courses` full CRUD, `generate_certificates`, `student_lifecycle`, `enrollments` full CRUD, `programs`, `prerequisites`, `rubric_manage`, `graduation_eligibility`, `degree_rules`, `lms_manage`, `library_config`, `accreditation`, `user_import` |

### 11.4 Student
| Category | Access |
|----------|--------|
| **Academic** | `assignments` (View + Submit), `course_material` (View + Download), `quizzes` (View + Attempt), `results` (View), `attendance` (View), `study_plan` (Add/Edit/View own), `fyp` (Add/Edit/View own project) |
| **University** | `degree_audit` (View), `graduation_apply` (Add + View own application), `generate_certificates` (View own) |
| **Communication** | `announcements` (View), `discussion` (Create + View own), `notifications` (View), `ai_chat` (View) |
| **Finance** | `payments` (View own + mark complete → send to finance for approval) |
| **Timetable** | `timetable_student` (View) |
| **Support** | `helpdesk` (Add + View own tickets) |
| **Security** | Two-Factor Authentication (Enable/Disable on own account) |
| **Personal** | `theme_settings` (Edit own), update own mobile/address/email |
| **Hidden** | All admin/faculty CRUD, all Settings, `report_center`, `analytics`, `enter_attendance`, `enter_results`, `student_lifecycle`, `timetable_admin`, `timetable_teacher`, `timetable_admin`, `buildings`, `rooms`, `lookups`, `departments`, `courses` CRUD, `enrollments` CRUD, `programs`, `prerequisites`, `rubric_manage`, `gradebook` CRUD, `graduation_eligibility`, `degree_rules`, `lms_manage`, `library_config`, `accreditation`, `user_import` |

### 11.5 Finance
| Category | Access |
|----------|--------|
| **Payments** | `payments` (Add/Edit/View/Mark as Paid) |
| **Reports** | `report_center` (Payment reports ONLY) |
| **Analytics** | `analytics` (Payment-related graphs ONLY) |
| **Personal** | `theme_settings` (Edit own), update own mobile/address/email |
| **Hidden** | EVERYTHING else |

### Phase 11 — Implementation Summary

> _Fill after phase completion: final permission matrix enforcement, cross-role audit, any remaining leaks closed._

### Phase 11 — Validation Summary

> _Fill after phase completion: all 5 roles tested, every menu verified, no permission leaks, documentation sync completed._

---

## References

- `Docs/Sidebar-Menu-Purpose.csv` — Complete menu-to-role permission matrix
- `Docs/Menus.md` — Dashboard menu documentation with sync notes
- `Docs/Functionality.md` — Implementation sync logs and feature status
- `Docs/Function-List.md` — Auto-maintained function registry
- `Scripts/` — All DB scripts (schema, seed, dummy data, maintenance, checks)
- `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` — Portal actions and menu-key mapping
- `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` — API client for Web→API communication
