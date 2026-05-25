# Tabsan EduSphere Dashboard Menus

## Purpose
This document lists dashboard menus and explains what each menu does for end users.

## How Menu Visibility Works
- Menu visibility is role-aware, module-aware, and status-aware.
- Sidebar configuration controls role access and active/inactive state per menu.
- SuperAdmin has override visibility for system governance surfaces.
- Direct route access is guarded; if a role cannot see a menu, access is denied and redirected.

## Core Menu Catalog (Seeded Sidebar Menus)

| Menu Key | Display Name | Default Role Access | What the Menu Does |
|---|---|---|---|
| dashboard | Dashboard | Admin, Faculty, Student | Main landing page with role-specific summaries, alerts, and widgets. |
| timetable_admin | Timetable Admin | Admin | Create and manage timetables for departments and offerings. |
| timetable_teacher | Teacher Timetable | Faculty | View faculty teaching timetable and schedule context. |
| timetable_student | Student Timetable | Student | View student class timetable for assigned department/offering. |
| lookups | Lookups | Admin | Parent menu for reference data management used by scheduling. |
| buildings | Buildings | Admin | Maintain campus building masters used in timetable/location flows. |
| rooms | Rooms | Admin | Maintain room inventory inside buildings for scheduling and capacity mapping. |
| system_settings | System Settings | SuperAdmin (governed) | Parent menu for platform-level settings and governance controls. |
| report_settings | Report Settings | SuperAdmin | Control report definitions and role visibility for reports. |
| sidebar_settings | Sidebar Settings | SuperAdmin | Configure which roles can see each menu/submenu and whether a menu is active. |
| admin_users | Admin Users | SuperAdmin | Manage platform administrative accounts and privileged-role user lifecycle actions. |
| tenant_management | Tenant Management | SuperAdmin | Manage tenant identities, tenant-level status, and multi-tenant operational governance. |
| campus_management | Campus Management | SuperAdmin | Manage campus identities and tenant-campus mapping context for scoped operations. |
| theme_settings | Theme Settings | Admin, Faculty, Student | Select and apply UI themes and personalization preferences. |
| license_update | License Update | SuperAdmin | Upload and validate license artifacts, view status and expiry context. |
| dashboard_settings | Dashboard Settings | SuperAdmin | Configure portal branding such as institution name/logo/subtitle/footer. |
| result_calculation | Result Calculation | Admin | Configure assessment components and GPA/grade calculation behavior. |
| notifications | Notifications | Admin, Faculty, Student | View inbox notifications and delivery/read status updates. |
| students | Students | Admin, Faculty | Manage and view student profile and lifecycle-related records. |
| departments | Departments | Admin | Manage departments and department-scoped academic setup. |
| courses | Courses | Admin, Faculty | Manage course masters and course offerings by academic level. |
| assignments | Assignments | Faculty, Student | Faculty creates/grades assignments; students view and submit assignments. |
| attendance | Attendance | Faculty, Student | Faculty records attendance; students view attendance and warnings. |
| results | Results | Admin, Faculty, Student | Enter/publish/view results and access transcript-related outputs. |
| quizzes | Quizzes | Faculty, Student | Faculty authors/publishes quizzes; students attempt and review outcomes. |
| fyp | FYP | Faculty, Student | Manage final year project workflows, meetings, and supervision updates. |
| analytics | Analytics | Admin, Faculty | View academic analytics, trends, and comparative performance metrics. |
| ai_chat | AI Chat | Faculty, Student | Open AI assistant for role-aware academic support and navigation help. |
| student_lifecycle | Student Lifecycle | Admin | Manage promotions, graduation, deactivation/reactivation, and request reviews. |
| payments | Payments | Admin, Student | Track payment receipt lifecycle and student payment state visibility. |
| enrollments | Enrollments | Admin, Faculty | Manage enrollment rosters and offering-level enrollment actions. |
| report_center | Report Center | Admin, Faculty, Student | Run and export reports according to role and report-assignment policy. |

## Additional Routed Menu Keys (Feature-Dependent)
These keys are supported by dashboard routing and may appear when enabled by module/policy/seed updates.

| Menu Key | Typical Surface | What It Does |
|---|---|---|
| helpdesk | Portal | Ticketing for academic/technical/administrative support workflows. |
| prerequisites | Portal | Manage prerequisite rules and dependency checks before enrollment. |
| gradebook | Portal | Faculty-gradebook management and per-assessment score tracking. |
| rubric_manage | Portal | Create and manage rubric templates and rubric-based grading. |
| degree_audit | Portal | Evaluate completion progress against degree requirements. |
| graduation_eligibility | Portal | Validate student graduation readiness against rules and thresholds. |
| degree_rules | Portal | Configure degree requirements and audit constraints. |
| graduation_apply | Portal | Student graduation application workflow entry point. |
| graduation_applications | Portal | Admin/faculty review queue for graduation applications. |
| grading_config | Portal | Configure grading policy and institution-level grading behavior. |
| lms_manage | Portal | Manage LMS integration and LMS configuration hooks. |
| discussion | Portal | Course discussion boards and collaboration threads. |
| announcements | Portal | Publish and consume institutional/course announcements. |
| study_plan | Portal | Student study planning with advisor review lifecycle. |
| library_config | Portal | Configure external/library integration behavior. |
| accreditation | Portal | Manage accreditation templates and export mappings. |
| institution_policy | Portal | Manage institution-type policy and vocabulary behavior. |
| module_composition | Portal | Manage module composition and module-role exposure alignment. |
| user_import | Portal | Admin-driven bulk user onboarding through CSV import flow. |
| admin_users | Portal | Privileged admin account governance and oversight actions. |
| tenant_management | Portal | Tenant-level governance and operational management controls. |
| campus_management | Portal | Campus-level governance and operational management controls. |
| programs / program | Portal | Program management and program-level academic structure controls. |

## Notes
- Final visible menus are the intersection of role permission, menu active status, module entitlement, and route authorization.
- If a user reports a missing menu, validate: role assignment, module status, sidebar status, and role-access mapping for the menu key.
