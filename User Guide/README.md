# Tabsan EduSphere User Guides

This folder contains role-based manuals for day-to-day use of the platform.

Version: 1.6 — updated 26 May 2026  
Completion Status: Phase 38 complete (final separation baseline)

Repository Sync Note (15 May 2026):

- Operations now support dual deployment flows:
  - Demo flow (with full dummy data)
  - Clean flow (startup-only baseline without dummy data)
- Refer to `Scripts/README.md` and `Scripts/README-SCRIPT-EXECUTION-ORDER.md` for exact commands.

## Final Release Packaging Update (Phase 37/38)

- Runtime app publish output is separated from the license app publish output.
- Non-runtime documentation and training assets are distributed as a separate package.
- Canonical package outputs:
  - Artifacts/Phase37/Tabsan.EduSphere-App-Publish-20260515.zip
  - Artifacts/Phase37/Tabsan.Lic-Publish-20260515.zip
  - Artifacts/Phase38/NonRuntime-Assets-20260515.zip

## Documents

- [Student-Guide.md](Student-Guide.md)
- [Faculty-Guide.md](Faculty-Guide.md)
- [Admin-Guide.md](Admin-Guide.md)
- [Finance-Guide.md](Finance-Guide.md)
- [SuperAdmin-Guide.md](SuperAdmin-Guide.md)
- [License-KeyGen-Guide.md](License-KeyGen-Guide.md)
- [Training Manual.md](Training Manual.md)
- [User Guide.md](User Guide.md)

## Who Should Read What

- Students: start with Student Guide
- Faculty members: start with Faculty Guide
- Department admins: start with Admin Guide
- Finance officers: start with Finance Guide
- Platform owners / IT administrators: start with SuperAdmin Guide
- Teams responsible for issuing licenses: use License KeyGen Guide

## Notes

- Features shown in each guide depend on activated modules and current license state.
- Role and institute filters are enforced together: visible menus, data scope, and report behavior can differ between School, College, and University contexts.
- Some screens can look slightly different depending on theme and deployment settings.
- Audit-sensitive actions such as publishing results and system-level changes are logged.
- User import templates are now role-specific under User Import Sheets: faculty-admin-import-template.csv and students-import-template.csv.
- Standard database deployment run order is Scripts/01-Schema-Current.sql through Scripts/05-PostDeployment-Checks.sql.
- Consolidated planning and enhancement history is maintained in Docs/Consolidated-Execution-Enhancements-Issues.md.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Program management is now tenant and campus scoped with explicit activate/deactivate controls.
- Report Center availability can now be toggled per tenant and campus scope by authorized governance users.
- Sidebar governance synchronization now self-heals role/menu visibility mappings in existing environments.
- Settings and governance coverage now explicitly includes Sidebar Settings, Report Settings, Institution Policy, Module Composition, Admin Users, Tenant Management, and Campus Management surfaces.
- User import operations now follow claim-aware tenant and campus scope resolution for safer onboarding boundaries.

## Phase 40.4 Institution-Aware Certificates Update (2026-05-26)

- Generate Certificates behavior now adapts by institution mode.
- University mode supports degree generation and period-scoped transcript generation.
- School/College mode supports additional student certificate upload, listing, and download workflows.
- Degree/transcript actions are hidden when university mode is disabled by active policy/license.
- Certificate period label now adapts by context (`Class` in university mode, `Semester` otherwise).

## Phase 40.5 Results and Attendance Governance Update (2026-05-28)

- User guides now align with stricter Enter Results governance (write scope checks, publish role controls, correction reason requirement).
- Demo validation baseline for training/UAT now includes mixed result states (`Midterm`, published `Final`, draft `FinalReview`) and expanded attendance timeline rows.
- Script marker for full dataset path is now `DemoDatasetVersion = FullDummyData-v9`.

## Comprehensive Guide Index (Detailed)

Use this section to quickly identify the right guide for each operational need.

### Student Journey Docs

- Student onboarding, course participation, assignments, quizzes, attendance, and result tracking:
  - Student-Guide.md
- End-user consolidated guidance and quick troubleshooting:
  - User Guide.md

### Teaching and Academic Delivery Docs

- Day-to-day teaching operations, grading, publication readiness, and class analytics:
  - Faculty-Guide.md
- Instructor classroom readiness and escalation patterns:
  - Training Manual.md (Faculty sessions)

### Department and Governance Docs

- Department administration operations, user and semester controls, and compliance checks:
  - Admin-Guide.md
- Finance operations for payments, reports, analytics, and scope controls:
  - Finance-Guide.md
- Platform governance, licensing, module composition, sidebar/report governance, and audit handling:
  - SuperAdmin-Guide.md

### License and Crypto Operations Docs

- Key generation and secure license lifecycle operations:
  - License-KeyGen-Guide.md

### Training Delivery Docs

- Instructor-led enablement programs, exercises, acceptance criteria, and completion scoring:
  - Training Manual.md

## Recommended Reading Paths

Use these paths for faster onboarding by responsibility.

1. Student Support Team
- Start: User Guide.md
- Then: Student-Guide.md
- Then: Training Manual.md (Student sessions)

2. Faculty Support Coordinator
- Start: Faculty-Guide.md
- Then: Admin-Guide.md
- Then: Training Manual.md (Faculty and Admin sessions)

3. Department Administrator
- Start: Admin-Guide.md
- Then: SuperAdmin-Guide.md (governance dependencies)
- Then: User Guide.md (cross-role impact)

4. Platform Owner / IT Lead
- Start: SuperAdmin-Guide.md
- Then: License-KeyGen-Guide.md
- Then: Training Manual.md (IT and SuperAdmin sessions)

5. Finance Team
- Start: Finance-Guide.md
- Then: User Guide.md
- Then: Training Manual.md (Finance session)

## Operational Quality Bar

The following outcomes should be achievable after reading the role-specific guide:

- User can complete core tasks without escalation.
- User understands permission boundaries and when access denial is expected.
- User can identify whether an issue is data, scope, module, or license related.
- User can provide support-ready incident details (role, institution mode, tenant/campus, module, route, timestamp).

## Documentation Maintenance Protocol

When the product is updated, refresh these documents in this order:

1. Update User Guide.md for consolidated behavior changes.
2. Update role guides impacted by the release.
3. Update Training Manual.md with new session drills.
4. Verify all internal references and script/version markers.
5. Include a dated release-note block in each modified guide.
