# Final Fixes Needed

System audit timestamp: 2026-05-26
Scope: full build/test/runtime and static validation only (no code changes applied)

## Execution Update (2026-05-27)

Pending-check execution started from this file and validated against current repo + local runs.

### Current Check Results

- Phase 1 - Critical checks:
  - Sidebar role/regression integration checks now pass locally (`SidebarMenuIntegrationTests`: 17 passed, 0 failed).
  - CSV-driven role matrix assertion path is now present in integration tests (test helper loads `Docs/Sidebar-Menu-Purpose.csv` and validates role visibility).
- Phase 2 - High checks:
  - Full solution build check passes locally.
  - Runtime startup still reports environment-profile warnings in API/Web logs (`No 'Environments' section` / profile fallback warnings) and API startup also fails on SQL connection pool timeout during seeding in current local environment.
  - Script governance compatibility check is implemented in docs (execution-order README contains compatibility alias policy).
- Phase 3 - Medium checks:
  - CI workflow includes SQL service + schema/seed + `05-PostDeployment-Checks.sql` execution.
  - CI workflow includes module health summary generation and artifact upload (`test-module-summary.md` + `test-module-summary.json`).
- Phase 4 - Low checks:
  - Docs lint cleanup has started with bounded fixes on operational docs, but full markdown backlog remains a larger ongoing task.

### Remaining Open from This Validation Pass

- Resolve runtime environment-profile warning path in API/Web startup (configuration source loading mismatch).
- Stabilize API startup DB connectivity in local smoke path (connection pool timeout during startup seed).
- Continue markdown cleanup as a full backlog burn-down (current pass only covered high-priority docs).

## Execution Update (2026-05-27, Continuation)

Follow-up execution focused on remaining high-priority startup checks.

### Fixes Applied During This Pass

- Startup configuration loader now correctly supports absolute parent-path JSON sources, ensuring `src/environments.json` is loaded for API/Web profile resolution.
- Development profile DB targets were normalized for local startup smoke consistency:
  - `src/environments.json` Development/LocalHost/Testing database connection values updated to LocalDB conventions.
  - API development appsettings connection string aligned to LocalDB baseline.

### Re-Validation Outcomes

- Build: passed (`Tabsan.EduSphere.sln`).
- API startup smoke: passed after starting LocalDB instance (`sqllocaldb start MSSQLLocalDB`).
- Web startup smoke: passed.
- Environment-profile warning path: resolved (no more `No 'Environments' section` / fallback warnings in startup logs).

### Remaining Open (Updated)

- Keep LocalDB (or equivalent SQL target) available before API smoke/startup in local validation runs.
- Continue markdown-lint backlog burn-down beyond the already cleaned operational docs.

## Execution Update (2026-05-27, Regression Refresh)

### Validation outcome

- Full regression suite rerun completed successfully.
- Current test snapshot: `443 passed / 0 failed`.
- The earlier sidebar drift failures are no longer present in the current run.

### Current state

- Build/runtime checks remain green from the prior pass.
- The audit now reflects a fully passing test run, so the remaining work is limited to documentation backlog burn-down and any future drift that reappears.

## Validation Result Summary (All Phases)

- Overall validation execution status: Completed
- Build status: Completed (Debug and Release passed)
- Runtime startup status: Completed (API and Web startup smoke passed)
- Automated tests status: Completed with findings (438 passed, 5 failed)

### Phase Completion Snapshot

- Phase 1 - Critical
  - Validation status: Completed
  - Findings: 2
  - Open issues by severity: Critical 2
  - Completion summary: Critical risk area identified and fully triaged; fixes pending implementation.

- Phase 2 - High
  - Validation status: Completed
  - Findings: 3
  - Open issues by severity: High 3
  - Completion summary: Runtime and deployment-governance concerns identified and scoped for next sprint.

- Phase 3 - Medium
  - Validation status: Completed
  - Findings: 2
  - Open issues by severity: Medium 2
  - Completion summary: Coverage/reporting maturity gaps identified; implementation tasks defined.

- Phase 4 - Low Priority / Improvements
  - Validation status: Completed
  - Findings: 2
  - Open issues by severity: Low 2
  - Completion summary: Documentation and reporting enhancements captured for continuous improvement.

### Totals by Phase

- Total findings: 9
- Critical: 2
- High: 3
- Medium: 2
- Low: 2
- Validation phases completed: 4 of 4

## Phase 1 - Critical

- [Role and Access] Sidebar role visibility baseline drift can hide real authorization regressions.
  - Evidence: integration failures in SidebarMenuIntegrationTests (expected role counts no longer match returned menu counts).
  - Severity: Critical
  - Suggested Fix: Replace hardcoded menu-count assertions with baseline-driven assertions generated from Docs/Sidebar-Menu-Purpose.csv and explicit per-key allow/deny checks for sensitive menus.
  - Actionable Task: Build a shared test helper that loads CSV role matrix and validates visible menu keys per role.
  - Estimate: 6-8 hours (5 story points)

- [Security Governance] Role/menu contract is not enforced by a single source-of-truth test gate.
  - Evidence: CSV defines permissions but tests currently rely on fixed counts (30/19/17/13) and are stale.
  - Severity: Critical
  - Suggested Fix: Add CI gate that compares seeded sidebar_menu_items + role access against Sidebar-Menu-Purpose.csv and fails on drift.
  - Actionable Task: Add integration test + pre-merge validation job to detect permission drift.
  - Estimate: 4-6 hours (3 story points)

## Phase 2 - High

- [Runtime Configuration] Environment profile fallback warnings during API/Web startup.
  - Evidence: startup logs report missing Environments section and unmatched Development/Testing profiles.
  - Severity: High
  - Suggested Fix: Define canonical Environments profile entries in environments.json for Development/Testing/Production and document required keys.
  - Actionable Task: Add startup configuration validation checklist and profile fixture for local/test pipelines.
  - Estimate: 3-4 hours (2 story points)

- [Transport Configuration] HTTPS redirection warning in integration runtime.
  - Evidence: log warning "Failed to determine the https port for redirect."
  - Severity: High
  - Suggested Fix: define HTTPS port in test launch settings or disable HTTPS redirection for integration host profile.
  - Actionable Task: stabilize test host startup configuration for deterministic HTTP/HTTPS behavior.
  - Estimate: 2-3 hours (2 story points)

- [Script Governance] Deployment script naming mismatch versus requested canonical names.
  - Evidence: repository uses Scripts/01-Schema-Current.sql and Scripts/02-Seed-Core.sql, not 01-Schema.sql and 02-CoreSeed.sql.
  - Severity: High
  - Suggested Fix: provide alias scripts/symlink-equivalent wrappers or update deployment runbooks to accepted canonical filenames.
  - Actionable Task: add script-name compatibility policy to README-SCRIPT-EXECUTION-ORDER.md and release checklist.
  - Estimate: 2 hours (1 story point)

## Phase 3 - Medium

- [Database Validation Coverage] No automated live SQL integrity run in this validation pass.
  - Evidence: FK/orphan/aggregation checks were statically reviewed from scripts, not executed against live DB in this run.
  - Severity: Medium
  - Suggested Fix: run Scripts/05-PostDeployment-Checks.sql and orphan-detection queries in CI against ephemeral SQL Server.
  - Actionable Task: add CI database-validation stage (schema apply + seed + post-deployment checks + integrity assertions).
  - Estimate: 8-12 hours (8 story points)

- [Regression Visibility] Functional coverage is broad but grouped pass/fail reporting is not module-tagged for triage.
  - Evidence: 438 tests passed, 5 failed; failure cluster visible but module-level dashboard not generated.
  - Severity: Medium
  - Suggested Fix: categorize tests by module tags and publish module health summary artifact in CI.
  - Actionable Task: introduce trait/category conventions and module summary report generation.
  - Estimate: 4-6 hours (3 story points)

## Phase 4 - Low Priority / Improvements

- [Documentation Quality] Large markdown lint backlog in docs reduces signal quality during audits.
  - Evidence: markdown diagnostics (heading/list/tab style) reported across Docs markdown files.
  - Severity: Low
  - Suggested Fix: run markdown lint autofix/manual cleanup on docs baseline and enforce lint in docs-only pipeline.
  - Actionable Task: create docs formatting cleanup sprint and add markdownlint config for consistent style.
  - Estimate: 6-10 hours (5 story points)

- [Operational Reporting] Validation output format is not yet standardized as a reusable artifact.
  - Evidence: validations are currently command-based and manually summarized.
  - Severity: Low
  - Suggested Fix: generate a machine-readable health report (JSON + markdown) from build/test/startup/script checks.
  - Actionable Task: add a validation runner script to emit standardized audit artifacts per sprint.
  - Estimate: 4-8 hours (3 story points)

## Sprint Grouping (Suggested)

- Sprint A (Security and Access Hardening):
  - Phase 1 tasks + HTTPS/runtime config task
  - Capacity: 10-13 hours, 8 story points

- Sprint B (Data and Deployment Reliability):
  - DB validation CI task + script governance task
  - Capacity: 10-14 hours, 9 story points

- Sprint C (Quality and Reporting):
  - Docs lint cleanup + module/reporting improvements
  - Capacity: 10-16 hours, 8 story points

### Sprint Execution Summary Table

| Sprint | Scope | Est. Hours | Story Points | Priority Outcome |
| --- | --- | --- | --- | --- |
| Sprint A | Phase 1 + runtime transport hardening | 10-13 | 8 | Close critical role/access regression risk |
| Sprint B | DB validation automation + script governance | 10-14 | 9 | Strengthen data integrity and deployment reliability |
| Sprint C | Docs quality + health reporting standardization | 10-16 | 8 | Improve maintainability and audit repeatability |

### Portfolio Totals

- Estimated delivery window: 30-43 hours
- Total story points: 25
- Recommended sequence: Sprint A -> Sprint B -> Sprint C
