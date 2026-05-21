# Tabsan EduSphere User Guides

This folder contains role-based manuals for day-to-day use of the platform.

Version: 1.5 — updated 15 May 2026  
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
- [SuperAdmin-Guide.md](SuperAdmin-Guide.md)
- [License-KeyGen-Guide.md](License-KeyGen-Guide.md)
- [Training Manual.md](Training Manual.md)
- [User Guide.md](User Guide.md)

## Who Should Read What

- Students: start with Student Guide
- Faculty members: start with Faculty Guide
- Department admins: start with Admin Guide
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
