# Tabsan EduSphere Development Plan (ASP.NET)

**Version:** 1.5  
**Date:** 15 May 2026  
**Based On:** PRD v1.75, Modules v1.3, Database Schema v1.2

## Institute Parity Stage Documentation Rule (2026-05-13)

For each completed stage in `Docs/Institute-Parity-Issue-Fix-Phases.md`, this plan must be updated with:
- `Implementation Summary`
- `Validation Summary`

Validation summaries must include at minimum:
- commands/test suites executed,
- role/institute behavior checks,
- regression confirmation status.

---

## Execution Updates

### 2026-05-20 - Plan D Phase 2 Stage 2.1 Global Filters
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.1 and add global analytics filters.

#### Phase 2 Stage 2.1 - Global Filters (Implemented)
- Implementation Summary:
  - added analytics filter inputs for institution, department, course, and semester in portal UI,
  - propagated course/semester filters through web client, API endpoints, and analytics service calls,
  - added course/semester filtering and cache-key partitioning in analytics queries.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 2 Stage 2.1 completed.
- Phase status: Plan D Phase 2 in progress (Stages 2.2 and 2.3 pending).

### 2026-05-20 - Plan D Phase 2 Stage 2.2 Dependent Filtering
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.2 and ensure each filter updates downstream filters.

#### Phase 2 Stage 2.2 - Dependent Filtering (Implemented)
- Implementation Summary:
  - implemented dependent filter cascade for Analytics and downstream auto-reset behavior,
  - added offering metadata mapping to support deterministic dependency calculation,
  - enabled auto-apply form submit on parent filter change in the analytics view.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 2 Stage 2.2 completed.
- Phase status: Plan D Phase 2 in progress (Stage 2.3 pending).

### 2026-05-20 - Plan D Phase 2 Stage 2.3 Instant Charts Update
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.3 and update charts instantly on filter/legend change.

#### Phase 2 Stage 2.3 - Instant Charts Update (Implemented)
- Implementation Summary:
  - introduced shared analytics model builder for view and JSON snapshot responses,
  - added `AnalyticsSnapshot` endpoint and converted filter submit workflow to async refresh,
  - updated analytics view rendering pipeline to refresh summary cards and charts in place.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 2 Stage 2.3 completed.
- Phase status: Plan D Phase 2 completed.

### 2026-05-20 - Plan D Phase 3 Stage 3.1 Chart Types and Data
- Recent request issue:
  - proceed to Plan D Phase 3 Stage 3.1 with additional chart types and trend views.

#### Phase 3 Stage 3.1 - Chart Expansion (Implemented)
- Implementation Summary:
  - added advanced trends analytics panel with pie/bar/line chart coverage,
  - integrated new chart renderers into snapshot-driven refresh flow,
  - implemented no-data chart cleanup for filter scopes with empty datasets.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 3 Stage 3.1 completed.
- Phase status: Plan D Phase 3 in progress.

### 2026-05-20 - Plan D Phase 4 Stage 4.1 Tenant/Campus Isolation
- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.1 and enforce strict tenant/campus filtering for analytics reads.

#### Phase 4 Stage 4.1 - Analytics Isolation Hardening (Implemented)
- Implementation Summary:
  - enforced tenant/campus scope filters in analytics service query paths,
  - removed analytics quiz query filter bypass,
  - partitioned analytics cache keys by tenant/campus and caller scope,
  - added integration test coverage for tenant/campus-constrained assignment analytics visibility.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`66/66`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 4 Stage 4.1 completed.
- Phase status: Plan D Phase 4 in progress (Stage 4.2 pending).

### 2026-05-20 - Plan D Phase 4 Stage 4.2 Leakage Prevention
- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.2 and enforce broader leakage-prevention checks.

#### Phase 4 Stage 4.2 - Leakage Prevention Hardening (Implemented)
- Implementation Summary:
  - enforced owner-or-superadmin access semantics for analytics export-job status/download,
  - added requester tenant/campus metadata propagation for analytics export jobs,
  - enforced tenant/campus scope parity checks for export-job retrieval endpoints,
  - added negative integration tests for cross-user and cross-scope access attempts.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 4 Stage 4.2 completed.
- Phase status: Plan D Phase 4 completed.

### 2026-05-20 - Plan D Phase 5 Stage 5.1 Performance Optimization
- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.1 and optimize analytics query paths.

#### Phase 5 Stage 5.1 - Analytics Query Optimization (Implemented)
- Implementation Summary:
  - replaced high-cost per-entity analytics queries with batched grouped aggregation patterns,
  - applied no-tracking read mode to report-heavy analytics queries,
  - reduced repeated per-department/per-assignment/per-student query round-trips.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 5 Stage 5.1 completed.
- Phase status: Plan D Phase 5 in progress (Stage 5.2 pending).

### 2026-05-20 - Plan D Phase 5 Stage 5.2 Index and Data-Loading Refinement
- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.2 and align analytics workloads with proper index support.

#### Phase 5 Stage 5.2 - Index Strategy Implementation (Implemented)
- Implementation Summary:
  - added analytics hotspot indexes on assignment submissions, published result filters, and quiz status aggregates,
  - introduced bounded assignment-submission status column length to improve index usage,
  - generated deployment-ready EF migration for schema/index updates.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 5 Stage 5.2 completed.
- Phase status: Plan D Phase 5 completed.

### 2026-05-20 - Plan D Phase 6 Stage 6.1 Validation and UI Consistency
- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.1 validation.

#### Phase 6 Stage 6.1 - Analytics Validation and Consistency Pass (Implemented)
- Implementation Summary:
  - executed Stage 6.1 validation for analytics interactivity, filter consistency, and UI stability,
  - confirmed no production code/schema change is required after Phase 5 completion.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 6 Stage 6.1 completed.
- Phase status: Plan D Phase 6 in progress (Stage 6.2 pending).

### 2026-05-20 - Plan D Phase 6 Stage 6.2 Final Performance and Security Review
- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.2 finalization.

#### Phase 6 Stage 6.2 - Final Analytics Readiness Review (Implemented)
- Implementation Summary:
  - executed release-mode performance and security regression validation for analytics,
  - confirmed no production implementation/schema change required to close Phase 6.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 6 Stage 6.2 completed.
- Phase status: Plan D Phase 6 completed.

### 2026-05-20 - Plan E Phase 1 Stage 1.1 Functional Non-Regression Validation
- Recent request issue:
  - there is no Phase 7 in this stream; start Plan E Phase 1 Stage 1.1.

#### Phase 1 Stage 1.1 - Existing Functionality Baseline Verification (Implemented)
- Implementation Summary:
  - executed full automated non-regression validation baseline,
  - confirmed no production code/schema updates are required for Stage 1.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 1 Stage 1.1 completed.
- Phase status: Plan E Phase 1 in progress (Stages 1.2-1.5 pending).

### 2026-05-20 - Plan E Phase 1 Stage 1.2 End-to-End Module Validation
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.2.

#### Phase 1 Stage 1.2 - Module End-to-End Validation Baseline (Implemented)
- Implementation Summary:
  - executed end-to-end module validation using full integration coverage,
  - confirmed no production code/schema updates are required for Stage 1.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 1 Stage 1.2 completed.
- Phase status: Plan E Phase 1 in progress (Stages 1.3-1.5 pending).

### 2026-05-20 - Plan E Phase 1 Stage 1.3 UI Alignment and Form Stability
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.3.

#### Phase 1 Stage 1.3 - UI and Form Stability Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for layout alignment, binding continuity, and form flow stability,
  - confirmed no production code/schema updates are required for Stage 1.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 1 Stage 1.3 completed.
- Phase status: Plan E Phase 1 in progress (Stages 1.4-1.5 pending).

### 2026-05-20 - Plan E Phase 1 Stage 1.4 API Response and Runtime Stability
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.4.

#### Phase 1 Stage 1.4 - API and Runtime Stability Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for API response and runtime stability continuity,
  - confirmed no production code/schema updates are required for Stage 1.4 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`),
  - unit tests passed (`151/151`).

- Stage status: Plan E Phase 1 Stage 1.4 completed.
- Phase status: Plan E Phase 1 in progress (Stage 1.5 pending).

### 2026-05-20 - Plan E Phase 1 Stage 1.5 Database Relationship Validation
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.5.

#### Phase 1 Stage 1.5 - Database Relationship Integrity Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for database relationship and referential integrity continuity,
  - confirmed no production code/schema updates are required for Stage 1.5 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 1 Stage 1.5 completed.
- Phase status: Plan E Phase 1 completed.

### 2026-05-20 - Plan E Phase 2 Stage 2.1 Tenant and Campus Isolation
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.1.

#### Phase 2 Stage 2.1 - Tenant/Campus Isolation Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for tenant/campus isolation continuity,
  - confirmed no production code/schema updates are required for Stage 2.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 2 Stage 2.1 completed.
- Phase status: Plan E Phase 2 in progress (Stages 2.2-2.3 pending).

### 2026-05-20 - Plan E Phase 2 Stage 2.2 Cross-Tenant/Campus Leakage Validation
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.2.

#### Phase 2 Stage 2.2 - Leakage Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for cross-tenant/campus leakage prevention continuity,
  - confirmed no production code/schema updates are required for Stage 2.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 2 Stage 2.2 completed.
- Phase status: Plan E Phase 2 in progress (Stage 2.3 pending).

### 2026-05-20 - Plan E Phase 2 Stage 2.3 Tenant/Campus Query Scope Validation
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.3.

#### Phase 2 Stage 2.3 - Tenant/Campus Query Scope Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for TenantId/CampusId query-scope continuity,
  - confirmed no production code/schema updates are required for Stage 2.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 2 Stage 2.3 completed.
- Phase status: Plan E Phase 2 completed.

### 2026-05-20 - Plan E Phase 3 Stage 3.1 Course Material End-to-End Validation
- Recent request issue:
  - proceed with next stage.

#### Phase 3 Stage 3.1 - Course Material Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for Course Material end-to-end continuity,
  - confirmed no production code/schema updates are required for Stage 3.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted Course Material integration tests passed (`5/5`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 3 Stage 3.1 completed.
- Phase status: Plan E Phase 3 in progress (Stages 3.2-3.4 pending).

### 2026-05-20 - Plan E Phase 3 Stage 3.2 Analytics Charts and Filters Validation
- Recent request issue:
  - proceed with next stage.

#### Phase 3 Stage 3.2 - Analytics and Filter Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for analytics chart/filter continuity,
  - confirmed no production code/schema updates are required for Stage 3.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted analytics/authorization integration tests passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 3 Stage 3.2 completed.
- Phase status: Plan E Phase 3 in progress (Stages 3.3-3.4 pending).

### 2026-05-20 - Plan E Phase 3 Stage 3.3 Tenant and Campus Management Validation
- Recent request issue:
  - proceed.

#### Phase 3 Stage 3.3 - Tenant/Campus Management Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for tenant/campus management continuity,
  - confirmed no production code/schema updates are required for Stage 3.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted tenant/campus management integration tests passed (`63/63`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 3 Stage 3.3 completed.
- Phase status: Plan E Phase 3 in progress (Stage 3.4 pending).

### 2026-05-20 - Plan E Phase 3 Stage 3.4 Role-Based Access Validation
- Recent request issue:
  - proceed.

#### Phase 3 Stage 3.4 - Role-Based Access Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for role-based access continuity,
  - confirmed no production code/schema updates are required for Stage 3.4 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 3 Stage 3.4 completed.
- Phase status: Plan E Phase 3 completed.

### 2026-05-20 - Plan E Phase 4 Stage 4.1 UI Consistency and Design Baseline Validation
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.1 - UI Consistency Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for layout/spacing/design continuity,
  - confirmed no production code/schema updates are required for Stage 4.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 4 Stage 4.1 completed.
- Phase status: Plan E Phase 4 in progress (Stages 4.2-4.4 pending).

### 2026-05-20 - Plan E Phase 4 Stage 4.2 Sidebar Header and Content Structure Validation
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.2 - UI Structure Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for sidebar/header/content structure continuity,
  - confirmed no production code/schema updates are required for Stage 4.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 4 Stage 4.2 completed.
- Phase status: Plan E Phase 4 in progress (Stages 4.3-4.4 pending).

### 2026-05-20 - Plan E Phase 4 Stage 4.3 Overlap and Responsive Layout Validation
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.3 - Responsive Layout Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for overlap prevention and responsive layout continuity,
  - confirmed no production code/schema updates are required for Stage 4.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 4 Stage 4.3 completed.
- Phase status: Plan E Phase 4 in progress (Stage 4.4 pending).

### 2026-05-20 - Plan E Phase 4 Stage 4.4 Validate All Buttons and Actions
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.4 - UI Action Validation Baseline (Implemented)
- Implementation Summary:
  - executed validation checkpoint for button/action continuity across key UI workflows,
  - confirmed no production code/schema updates are required for Stage 4.4 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 4 Stage 4.4 completed.
- Phase status: Plan E Phase 4 completed.

### 2026-05-20 - Plan E Phase 5 Stage 5.1 TenantId/CampusId Schema Audit
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.1 - Schema Scope Audit Baseline (Implemented)
- Implementation Summary:
  - executed schema audit against `Scripts/01-Schema-Current.sql` for `TenantId`/`CampusId` coverage,
  - audit parsed `82` tables and found `0` tables with both scope columns in the current schema script,
  - confirmed no production code/schema updates were required for Stage 5.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 5 Stage 5.1 completed.
- Phase status: Plan E Phase 5 in progress (Stages 5.2-5.4 pending).

### 2026-05-20 - Plan E Phase 5 Stage 5.2 Foreign Keys, Indexes, and Constraints Audit
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.2 - Schema Structure Audit Baseline (Implemented)
- Implementation Summary:
  - executed SQL artifact audit for foreign keys, indexes, and constraints on `Scripts/01-Schema-Current.sql` and `Scripts/04-Maintenance-Indexes-And-Views.sql`,
  - recorded `65` foreign key constraints (`5` via `ALTER TABLE`), `82` primary key constraints, `2` default constraints, and `190` total index statements,
  - confirmed no production code/schema updates were required for Stage 5.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 5 Stage 5.2 completed.
- Phase status: Plan E Phase 5 in progress (Stages 5.3-5.4 pending).

### 2026-05-20 - Plan E Phase 5 Stage 5.3 Nullable Field Audit
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.3 - Nullability Audit Baseline (Implemented)
- Implementation Summary:
  - executed nullable-field audit on `Scripts/01-Schema-Current.sql`,
  - recorded `280` nullable columns across `79` tables and identified `2` review-candidate nullable fields (`users.Email`, `timetable_entries.FacultyName`),
  - confirmed no production code/schema updates were required for Stage 5.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 5 Stage 5.3 completed.
- Phase status: Plan E Phase 5 in progress (Stage 5.4 pending).

### 2026-05-20 - Plan E Phase 5 Stage 5.4 Data Integrity and Migration Safety
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.4 - Integrity and Migration Safety Baseline (Implemented)
- Implementation Summary:
  - executed data-integrity and migration-safety audit on schema/post-deployment SQL artifacts,
  - recorded `365` migration-history guard references, `324` `IF NOT EXISTS` guards, `40` transaction blocks, `175` post-deployment verification `SELECT` checks, and `19` EF migration files,
  - confirmed no production code/schema updates were required for Stage 5.4 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 5 Stage 5.4 completed.
- Phase status: Plan E Phase 5 completed.

### 2026-05-20 - Plan E Phase 6 Stage 6.1 Role-Based Access Review
- Recent request issue:
  - proceed.

#### Phase 6 Stage 6.1 - Role Access Audit Baseline (Implemented)
- Implementation Summary:
  - executed role-based access audit across API/Web authorization attributes, role/policy enforcement code, and seed-role SQL artifacts,
  - recorded `359` API `[Authorize]` attributes, `369` role/policy enforcement references, `5` policy registration references, and `105` role-seeding script references,
  - confirmed no production code/schema updates were required for Stage 6.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 6 Stage 6.1 completed.
- Phase status: Plan E Phase 6 in progress (Stages 6.2-6.3 pending).

### 2026-05-20 - Plan E Phase 6 Stage 6.2 Unauthorized/Cross-Scope Access
- Recent request issue:
  - proceed.

#### Phase 6 Stage 6.2 - Cross-Scope Access Audit Baseline (Implemented)
- Implementation Summary:
  - executed unauthorized/cross-tenant/cross-campus access audit across source enforcement points,
  - recorded `1326` isolation-enforcement source hits and `128` explicit `Forbid`/`Unauthorized` enforcement hits,
  - confirmed no production code/schema updates were required for Stage 6.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 6 Stage 6.2 completed.
- Phase status: Plan E Phase 6 in progress (Stage 6.3 pending).

### 2026-05-20 - Plan E Phase 6 Stage 6.3 API Endpoint Restriction
- Recent request issue:
  - proceed.

#### Phase 6 Stage 6.3 - API Restriction Coverage Baseline (Implemented)
- Implementation Summary:
  - executed API endpoint restriction audit over authorization coverage in API controllers,
  - recorded `447` HTTP endpoints: `92` method-level `[Authorize]`, `349` class-level `[Authorize]` coverage, `1` `[AllowAnonymous]`, and `5` review-set endpoints without explicit authorize coverage,
  - confirmed no production code/schema updates were required for Stage 6.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 6 Stage 6.3 completed.
- Phase status: Plan E Phase 6 completed.

### 2026-05-20 - Plan E Phase 7 Stage 7.1 Query Scope Filtering
- Recent request issue:
  - proceed.

#### Phase 7 Stage 7.1 - Query Scope Filtering Baseline (Implemented)
- Implementation Summary:
  - executed tenant/campus query-filter audit across source and repository layers,
  - recorded `551` Tenant/Campus scope references, `11` LINQ `Where` scope-filter references, and `20` repository-layer scope references,
  - confirmed no production code/schema updates were required for Stage 7.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 7 Stage 7.1 completed.
- Phase status: Plan E Phase 7 in progress (Stages 7.2-7.3 pending).

### 2026-05-20 - Plan E Phase 7 Stage 7.2 Join and Full-Scan Risk Audit
- Recent request issue:
  - proceed.

#### Phase 7 Stage 7.2 - Query-Shape Risk Baseline (Implemented)
- Implementation Summary:
  - executed query-shape/full-scan risk audit for joins, includes, raw SQL usage, and pagination coverage,
  - recorded `134` join references (`18` in repository layer), `167` include/then-include references, `0` raw SQL query references, `475` materialization references, and `37` pagination references,
  - confirmed no production code/schema updates were required for Stage 7.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 7 Stage 7.2 completed.
- Phase status: Plan E Phase 7 in progress (Stage 7.3 pending).

### 2026-05-20 - Plan E Phase 7 Stage 7.3 Pagination and Analytics Query Efficiency
- Recent request issue:
  - proceed.

#### Phase 7 Stage 7.3 - Pagination and Analytics Efficiency Baseline (Implemented)
- Implementation Summary:
  - executed pagination and analytics-query efficiency audit across source and analytics layers,
  - recorded `37` pagination references with `29` nearby ordering safeguards, `551` Tenant/Campus scope references, `11` LINQ scope-filter references, and `18` analytics `AsNoTracking` references,
  - confirmed no production code/schema updates were required for Stage 7.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 7 Stage 7.3 completed.
- Phase status: Plan E Phase 7 completed.

### 2026-05-20 - Plan E Phase 8 Stage 8.1 Environment-Based Configuration
- Recent request issue:
  - proceed.

#### Phase 8 Stage 8.1 - Environment Configuration Baseline (Implemented)
- Implementation Summary:
  - executed environment-based configuration audit across startup/configuration-loading paths,
  - recorded `61` environment-handling references, `132` configuration/deployment references, `54` `appsettings*.json` files, and `127` Program.cs environment/configuration references,
  - confirmed no production code/schema updates were required for Stage 8.1 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 8 Stage 8.1 completed.
- Phase status: Plan E Phase 8 in progress (Stages 8.2-8.3 pending).

### 2026-05-20 - Plan E Phase 8 Stage 8.2 Deployment Scenarios
- Recent request issue:
  - proceed.

#### Phase 8 Stage 8.2 - Deployment Scenario Baseline (Implemented)
- Implementation Summary:
  - executed deployment-scenario readiness audit for cloud, on-prem, and multi-instance signals across source startup/configuration paths,
  - recorded `1` cloud reference, `0` on-prem references, `14` multi-instance/scale-out references, `200+` deployment/scaling/instance/tenant-isolation source references (search cap reached), and `9` source `appsettings*.json` files,
  - confirmed no production code/schema updates were required for Stage 8.2 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 8 Stage 8.2 completed.
- Phase status: Plan E Phase 8 in progress (Stage 8.3 pending).

### 2026-05-20 - Plan E Phase 8 Stage 8.3 Secrets and Configuration Security
- Recent request issue:
  - proceed.

#### Phase 8 Stage 8.3 - Secrets and Configuration Security Baseline (Implemented)
- Implementation Summary:
  - executed secure-secrets/configuration handling audit across startup guards, environment-variable resolvers, and appsettings templates,
  - recorded `18` secure startup guard references, `18` environment-variable secret/deployment references, `18` secret-sensitive configuration key references in source appsettings, `12` placeholder/template secret markers, and `9` source `appsettings*.json` files,
  - confirmed no production code/schema updates were required for Stage 8.3 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed (non-blocking warning: CS0105 in API Program.cs),
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 8 Stage 8.3 completed.
- Phase status: Plan E Phase 8 completed.

### 2026-05-20 - Plan E Phase 9 Stage 9.1 Issue and Inconsistency Remediation
- Recent request issue:
  - proceed to Plan E Phase 9 Stage 9.1 (identify and fix issues, inconsistencies, or risks).

#### Phase 9 Stage 9.1 - Startup Consistency Risk Fix (Implemented)
- Implementation Summary:
  - fixed API startup import inconsistency in `Program.cs` by splitting merged `using` directives and removing duplicate import,
  - removed CS0105 warning source and completed Stage 9.1 risk remediation with no behavior or schema mutation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full automated test suites passed (`396/396`),
  - full integration tests passed (`244/244`),
  - full unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Stage status: Plan E Phase 9 Stage 9.1 completed.
- Phase status: Plan E Phase 9 in progress (Stage 9.2 pending).

### 2026-05-20 - Plan D Phase 1 Stage 1.3 Clickable Legends
- Recent request issue:
  - proceed to Plan D Phase 1 Stage 1.3 and add color-coded clickable legends to Analytics charts.

#### Phase 1 Stage 1.3 - Clickable Legends (Implemented)
- Implementation Summary:
  - added reusable legend controls for all Analytics charts with dataset visibility toggling,
  - standardized legend styling and interaction behavior across overview/performance/attendance/assignments charts,
  - removed duplicate dashboard rendering fragments and stabilized the page structure.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`).

- Stage status: Plan D Phase 1 Stage 1.3 completed.
- Phase status: Plan D Phase 1 completed (Stages 1.1, 1.2, 1.3).

### 2026-05-20 - Plan C Phase 7 Stage 7.2 Finalization
- Recent request issue:
  - complete Plan C Phase 7 Stage 7.2.

#### Phase 7 Stage 7.2 - Final Review (Implemented)
- Implementation Summary:
  - completed final stability/scalability closeout review for Course Material release readiness,
  - validated release build and targeted release-mode Course Material authorization regression behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - Release-mode integration tests (Course Material authorization subset) passed (`5/5`),
  - load-test script execution depends on reachable API target and was blocked by unavailable local endpoint.

- Stage status: Plan C Phase 7 Stage 7.2 completed.
- Phase status: Plan C Phase 7 completed.

### 2026-05-20 - Plan C Phase 7 Stage 7.1 Validation
- Recent request issue:
  - start Plan C Phase 7 Stage 7.1.

#### Phase 7 Stage 7.1 - Validation (Implemented)
- Implementation Summary:
  - executed full validation pass for Course Material data safety, access control, and UI behavior,
  - confirmed existing Phase 5/6 changes remain stable and regression-free.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`393/393`).

- Stage status: Plan C Phase 7 Stage 7.1 completed.
- Phase status: validation track in progress (Stage 7.2 pending).

### 2026-05-20 - Plan C Phase 6 Implementation (Performance & Optimization)
- Recent request issue:
  - complete Plan C Phase 6 Stage 6.1 and Stage 6.2.

#### Phase 6 - Performance & Optimization (Implemented)
- Implementation Summary:
  - Stage 6.1 optimized `CourseMaterialRepository` read paths using no-tracking reads and scope-missing short-circuit behavior,
  - Stage 6.2 added targeted index tuning via `PlanCPhase6Stage2CourseMaterialIndexTuning` migration.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Testing and result summary:
  - focused validations passed and full solution build stayed green.

- Stage status: Plan C Phase 6 implementation completed.
- Phase status: performance optimization and index tuning completed.

### 2026-05-20 - Plan C Phase 5 Implementation (File & Link Handling)
- Recent request issue:
  - complete Plan C Phase 5 Stage 5.1, Stage 5.2, and Stage 5.3.

#### Phase 5 - File & Link Handling (Implemented)
- Implementation Summary:
  - Stage 5.1 added validated file upload endpoint and portal multipart upload integration,
  - Stage 5.2 added scoped persistent storage category routing and file streaming download endpoint,
  - Stage 5.3 added authorization regression coverage and portal role-context-aware fallback behavior.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Testing and result summary:
  - focused validations passed and full solution build stayed green.

- Stage status: Plan C Phase 5 implementation completed.
- Phase status: file/link handling feature completed end-to-end.

### 2026-05-19 - Plan C Phase 4 Implementation (UI & UX)
- Recent request issue:
  - proceed to Plan C Phase 4 UI and UX implementation.

#### Phase 4 - UI & UX (Implemented)
- Implementation Summary:
  - added portal course-material manage page for Admin/Faculty and student read-only page,
  - added portal actions for material list/create/update/activate/deactivate flows,
  - integrated `course_material` sidebar key mapping in both web layout and API sidebar entitlement mapping.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan C Phase 4 implementation completed.
- Phase status: UI and UX integration for Course Material completed.

### 2026-05-19 - Plan C Phase 3 Implementation (Access Control & Security)
- Recent request issue:
  - proceed to Plan C Phase 3 access control and strict isolation.

#### Phase 3 - Access Control & Security (Implemented)
- Implementation Summary:
  - added course-material API endpoints with role-based write restrictions,
  - added course-material service and repository abstractions/implementations,
  - enforced strict tenant/campus filtering with SuperAdmin bypass parity to existing scoped repositories.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan C Phase 3 implementation completed.
- Phase status: access-control and isolation service/API layer completed.

### 2026-05-19 - Plan C Phase 2 Implementation (Data Safety & Migration)
- Recent request issue:
  - proceed after Plan C Phase 1.

#### Phase 2 - Data Safety & Migration (Implemented)
- Implementation Summary:
  - hardened `CourseMaterial` creation/update validation for required scope IDs and material metadata,
  - added DB check constraints and migration `PlanCPhase2DataSafetyScopeGuard`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan C Phase 2 implementation completed.
- Phase status: data safety and migration hardening completed.

### 2026-05-19 - Plan C Phase 1 Implementation (Domain & Database Extension)
- Recent request issue:
  - start Plan C Phase 1.

#### Phase 1 - Domain & Database Extension (Implemented)
- Implementation Summary:
  - added `CourseMaterial` domain entity and lifecycle helpers,
  - added EF mapping and context registration,
  - generated migration `PlanCPhase1CourseMaterialFoundation` with required foreign keys and indexes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan C Phase 1 implementation completed.
- Phase status: domain and schema foundation completed.

### 2026-05-19 - Plan B Phase 10 Implementation (Validation & Finalization)
- Recent request issue:
  - proceed to validation and finalization after logging and visibility.

#### Phase 10 - Validation & Finalization (Implemented)
- Implementation Summary:
  - completed the final readiness and security/scalability review of the Phase B configuration stack,
  - confirmed the startup path remains backward-compatible and supportable across all deployment modes,
  - no code changes were required for this final closeout.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed (`388/388`).

- Stage status: Plan B Phase 10 implementation completed.
- Phase status: configuration and deployment roadmap fully finalized.

### 2026-05-19 - Plan B Phase 9 Implementation (Logging & Visibility)
- Recent request issue:
  - proceed to logging and visibility after configuration performance and stability.

#### Phase 9 - Logging & Visibility (Implemented)
- Implementation Summary:
  - added a shared startup visibility reporter,
  - standardized safe startup logs across API, Web, and BackgroundJobs,
  - surfaced active environment, configuration source summary, database type, deployment profile, and tenant isolation posture.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 9 implementation completed.
- Phase status: logging and visibility baseline completed.

### 2026-05-19 - Plan B Phase 8 Implementation (Performance & Stability)
- Recent request issue:
  - proceed to performance and stability improvements for the shared configuration stack.

#### Phase 8 - Performance & Stability (Implemented)
- Implementation Summary:
  - optimized the shared configuration bootstrapper to avoid duplicate provider registration,
  - preserved configuration precedence for deployment/external/local/tenant overlays,
  - reduced unnecessary reload-on-change monitoring for deployment-oriented configuration files.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 8 implementation completed.
- Phase status: configuration performance/stability baseline completed.

### 2026-05-19 - Plan B Phase 7 Implementation (Fail-Safe Behavior)
- Recent request issue:
  - proceed to fail-safe behavior after tenant-aware configuration support.

#### Phase 7 - Fail-Safe Behavior (Implemented)
- Implementation Summary:
  - added shared startup fail-safe validation,
  - unified error handling for resolved database configuration, reverse-proxy trust boundaries, tenant overlay files, and required non-development settings,
  - removed duplicated startup placeholder checks from API, Web, and BackgroundJobs.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 7 implementation completed.
- Phase status: fail-safe configuration baseline completed.

### 2026-05-19 - Plan B Phase 6 Implementation (Tenant + Campus Aware Configuration)
- Recent request issue:
  - proceed to tenant-aware configuration and isolation after customer deployment support.

#### Phase 6 - Tenant + Campus Aware Configuration (Implemented)
- Implementation Summary:
  - added tenant-isolation resolver and tenant-specific JSON overlay support,
  - extended deployment templates with tenant isolation metadata,
  - logged tenant-isolation posture in startup diagnostics and health output.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 6 implementation completed.
- Phase status: tenant-aware configuration baseline completed.

### 2026-05-19 - Plan B Phase 5 Implementation (Customer Deployment Support)
- Recent request issue:
  - proceed to customer deployment support after deployment flexibility.

#### Phase 5 - Customer Deployment Support (Implemented)
- Implementation Summary:
  - added optional deployment-pipeline JSON layer for configuration-only customer overrides,
  - added `Deployment` metadata sections to API/Web/BackgroundJobs appsettings templates,
  - preserved config-first override behavior with safe fallback to environment variables.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 5 implementation completed.
- Phase status: customer deployment support baseline completed.

### 2026-05-19 - Plan B Phase 4 Implementation (Deployment Flexibility)
- Recent request issue:
  - proceed to deployment flexibility after secure configuration handling.

#### Phase 4 - Deployment Flexibility (Implemented)
- Implementation Summary:
  - added shared deployment-topology resolver,
  - added customer-specific overrides for domain, database name, and scaling,
  - exposed safe deployment profile metadata in startup diagnostics and health output.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 4 implementation completed.
- Phase status: deployment flexibility baseline completed.

### 2026-05-19 - Plan B Phase 3 Implementation (Secure Configuration Handling)
- Recent request issue:
  - proceed to secure configuration handling after database connection management.

#### Phase 3 - Secure Configuration Handling (Implemented)
- Implementation Summary:
  - added optional external deployment configuration source for secrets,
  - added secure startup validation helper that rejects placeholder or missing sensitive values without exposing their contents,
  - preserved source-safe startup diagnostics.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 3 implementation completed.
- Phase status: secure configuration handling baseline completed.

### 2026-05-19 - Plan B Phase 2 Implementation (Database Connection Management)
- Recent request issue:
  - proceed to next Plan B phase after Phase 1 completion.

#### Phase 2 - Database Connection Management (Implemented)
- Implementation Summary:
  - implemented centralized startup DB connection resolver,
  - added deployment and environment override-first resolution strategy,
  - integrated resolver in API and BackgroundJobs startup paths.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 2 implementation completed.
- Phase status: database connection management baseline completed.

### 2026-05-19 - Plan B Phase 1 Implementation (Configuration Structure)
- Recent request issue:
  - proceed and begin Plan B before moving ahead.

#### Phase 1 - Configuration Structure (Implemented)
- Implementation Summary:
  - introduced shared configuration hierarchy helper in application services,
  - standardized startup configuration loading across API/Web/BackgroundJobs,
  - included optional local override layer and prefixed environment variable loading with fallback.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Plan B Phase 1 implementation completed.
- Phase status: configuration hierarchy baseline completed.

### 2026-05-19 - Plan A Phase 7 Implementation (Validation and Finalization)
- Recent request issue:
  - proceed to Plan A Phase 7 and complete final validation/stability closeout.

#### Phase 7 - Validation & Finalization (Implemented)
- Implementation Summary:
  - executed final quality-gate validation across build/unit/integration/contract layers,
  - verified end-to-end stability of all Plan A phase deliverables,
  - finalized governance closeout documentation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Stage status: Phase 7 implementation completed.
- Phase status: Plan A validation and finalization completed.

### 2026-05-19 - Plan A Phase 6 Implementation (Performance and Optimization)
- Recent request issue:
  - proceed to Plan A Phase 6 and optimize tenant/campus scoped query performance.

#### Phase 6 - Performance & Optimization (Implemented)
- Implementation Summary:
  - optimized scoped user lookups to remove non-sargable case transformations,
  - added composite indexes for tenant/campus user and department hot paths,
  - added migration `Phase46_TenantCampusQueryOptimization`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Stage status: Phase 6 implementation completed.
- Phase status: scoped query optimization baseline completed.

### 2026-05-19 - Plan A Phase 5 Implementation (Tenant/Campus UI Management Interfaces)
- Recent request issue:
  - proceed to Plan A Phase 5 and add SuperAdmin tenant/campus management interfaces integrated with existing portal menu patterns.

#### Phase 5 - UI Management Interfaces (Implemented)
- Implementation Summary:
  - added `TenantController` and `CampusController` SuperAdmin API endpoints,
  - added portal pages `TenantManagement` and `CampusManagement`,
  - added Web API client methods and view models for tenant/campus operations,
  - integrated new pages into sidebar and SuperAdmin fallback navigation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Stage status: Phase 5 implementation completed.
- Phase status: tenant/campus management UI baseline completed for SuperAdmin.

### 2026-05-19 - Plan A Phase 4 Implementation (Access Control and Filtering)
- Recent request issue:
  - proceed to Plan A Phase 4 and implement tenant/campus access filtering with SuperAdmin bypass.

#### Phase 4 - Access Control & Filtering (Implemented)
- Implementation Summary:
  - added `IAccessScopeResolver` and HTTP claims-backed implementation,
  - added `tenant_id` and `campus_id` JWT claim issuance,
  - enforced tenant/campus filtering in user and department repositories,
  - preserved SuperAdmin cross-tenant/campus access.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Stage status: Phase 4 implementation completed.
- Phase status: tenant/campus scoped access control baseline completed.

### 2026-05-19 - Plan A Phase 3 Implementation (Compatibility and Safety Hardening)
- Recent request issue:
  - proceed to Plan A Phase 3 and enforce compatibility/safety constraints for tenant+campus integration.

#### Phase 3 - Compatibility & Safety (Implemented)
- Implementation Summary:
  - added aggregate-level tenant/campus pair guards,
  - added database constraints for tenant/campus pairing and tenant-bound campus reference integrity,
  - added migration `Phase43_TenantCampusCompatibilitySafety`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Stage status: Phase 3 implementation completed.
- Phase status: compatibility/safety hardening completed for tenant/campus foundation.

### 2026-05-19 - Plan A Phase 2 Implementation (Default Tenant/Campus Data Integration)
- Recent request issue:
  - proceed to Plan A Phase 2 and implement safe default assignment for existing data.

#### Phase 2 - Data Integration & Migration (Implemented)
- Implementation Summary:
  - added migration `Phase42_DefaultTenantCampusBackfill` for default tenant/campus data assignment,
  - updated `DatabaseSeeder` to guarantee default tenant/campus existence and runtime backfill of null tenant/campus on core entities.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Stage status: Phase 2 implementation completed.
- Phase status: default tenant/campus baseline safely integrated for existing records.

### 2026-05-19 - Plan A Phase 1 Implementation (Tenant + Campus Domain Foundation)
- Recent request issue:
  - proceed with Plan A Phase 1 from documentation kickoff into real implementation and keep mandatory tracker synchronization.

#### Phase 1 - Domain Layer Extension (Implemented)
- Implementation Summary:
  - added `Tenant` and `Campus` domain entities,
  - added optional tenant/campus references to `User` and `Department` to keep backward compatibility,
  - added EF configurations and migration for tenancy foundation schema.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`).
- Testing and result summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal --filter "FullyQualifiedName~EnrollmentServiceWaitlistTests|FullyQualifiedName~AuthSecurityUxTests"` passed (`9/9`).

- Stage status: Phase 1 implementation completed.
- Phase status: Plan A foundation established for tenant/campus-aware phases.

### 2026-05-19 - Plan A Phase 1 Kickoff (App Configuration: Tenant + Campus)
- Recent request issue:
  - start Plan A Phase 1 and synchronize required planning/governance documents while adding phase-end implementation and validation summaries.

#### Phase 1 - Domain Layer Extension (Kickoff)
- Implementation Summary:
  - started Phase 1 planning baseline for introducing Tenant and Campus domain layers with non-breaking integration constraints,
  - updated Plan A phase document to include completion evidence at phase end,
  - synchronized Command, Function List, Functionality Reference, Development Plan, Database Schema, Modules, and PRD.
- Validation Summary:
  - cross-document consistency review completed,
  - verified this execution wave introduces no runtime, API, or schema mutation.
- Testing and result summary:
  - documentation consistency checks completed,
  - no runtime tests executed for this documentation-only kickoff update.

- Stage status: Phase 1 kickoff documentation completed.
- Phase status: Plan A execution started with phase-end summary compliance.

### 2026-05-19 - Deep System Audit + Validation + AI Chatbot Modernization (Phase 1-7)
- Recent request issue:
  - perform full phase-by-phase audit/validation and chatbot modernization, then synchronize mandatory governance docs with test-result summaries per phase.

#### Phase 1 - System Understanding
- Implementation Summary:
  - completed module and dependency mapping across API/Application/Infrastructure/Web for all requested domains,
  - documented risk hotspots for API route consistency and chatbot entrypoint modernization.
- Validation Summary:
  - static cross-layer traceability check completed.
- Testing and result summary:
  - phase 1 audit mapping completed with actionable findings.

#### Phase 2 - API and Backend Validation
- Implementation Summary:
  - added AI chat API dual route support (`/api/ai` + `/api/v1/ai`),
  - updated web chat client to use `/api/v1/ai` endpoints,
  - updated module-license route mapping for versioned AI path enforcement.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted unit/integration tests passed for module enforcement and auth/enrollment baselines.
- Testing and result summary:
  - unit `9/9` and integration `4/4` green after backend changes.

#### Phase 3 - Database and Data Flow Validation
- Implementation Summary:
  - validated current EF include and waitlist queue flow integrity against touched paths,
  - no schema changes required by this request.
- Validation Summary:
  - schema/migration impact reviewed and confirmed none.
- Testing and result summary:
  - phase 3 data-flow and schema check completed without new integrity issues.

#### Phase 4 - UI and Frontend Validation
- Implementation Summary:
  - validated role-aware shell/menu behavior and chatbot state/send bindings.
- Validation Summary:
  - static UI-binding verification completed across shared layout + site JS + API client.
- Testing and result summary:
  - phase 4 frontend validation completed with preserved backend contracts.

#### Phase 5 - Performance and Stability Check
- Implementation Summary:
  - reviewed updated async paths and ensured no new blocking-call patterns introduced.
- Validation Summary:
  - build and targeted regression suites remained green.
- Testing and result summary:
  - phase 5 stability check completed with no regression indicators in focused tests.

#### Phase 6 - AI Chatbot Redesign (Critical)
- Implementation Summary:
  - replaced inline chatbot markup with two reusable components in shared views:
    - `FloatingChatButton.cshtml`
    - `ChatPanel.cshtml`
  - retained existing API endpoints and conversation-state/message-history behavior.
- Validation Summary:
  - solution build and module-enforcement integration tests passed after component extraction.
- Testing and result summary:
  - phase 6 chatbot modernization completed and validated.

#### Phase 7 - Final Consolidated Reporting
- Implementation Summary:
  - synchronized PRD, Command, Function List, Functionality Reference, Development Plan, and Database Schema with phase outputs.
- Validation Summary:
  - all phase entries include testing/result summary blocks.
- Testing and result summary:
  - phase 7 reporting and documentation synchronization completed.

- Stage status: Deep system audit + chatbot modernization request completed.
- Phase status: all seven requested phases completed for this execution cycle.

### 2026-05-19 - UI/UX Redesign Phase 9 (Final UI Polish)
- Completed the final polish phase of the redesign sequence.
- Recent request issue:
  - proceed with the last redesign step and complete mandatory documentation and repository sync.
- Implementation Summary:
  - refined shared surface elevation, spacing, and dashboard presentation details,
  - preserved all existing page logic and backend contracts.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched final-polish stylesheet.
- Stage status: Phase 9 final polish completed.
- Phase status: redesign sequence completed.

### 2026-05-18 - UI/UX Redesign Phase 8 (Responsive Hardening)
- Completed the responsive-design phase of the redesign sequence.
- Recent request issue:
  - proceed with next phase and enforce documentation + repository sync after completion.
- Implementation Summary:
  - added shared responsive hardening rules for shell/page components and overflow-sensitive UI regions,
  - improved results/actions and payments pagination behavior on smaller screens,
  - preserved existing page logic and routing.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched responsive frontend files.
- Stage status: Phase 8 responsive pass completed.
- Phase status: redesign progression advanced with frontend-only changes.

### 2026-05-18 - UI/UX Redesign Phase 7 (AI Chatbot UI)
- Completed the AI chatbot phase of the redesign sequence.
- Recent request issue:
  - proceed with next phase and enforce documentation + repository sync after completion.
- Implementation Summary:
  - refined chatbot launcher and panel visual quality,
  - improved assistant header identity and message/thread polish,
  - added quick-prompt chips with frontend-only trigger logic bound to existing send flow.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched chatbot frontend files.
- Stage status: Phase 7 chatbot UI pass completed.
- Phase status: redesign progression advanced with frontend-only changes.

### 2026-05-18 - UI/UX Redesign Phase 6 (Branding Pass)
- Completed the branding phase of the frontend redesign sequence.
- Recent request issue:
  - proceed and enforce documentation + repository sync after completion.
- Implementation Summary:
  - refined institution branding visuals in the shared shell,
  - improved notification-chip styling and interaction affordance,
  - implemented a richer profile dropdown presentation in the header,
  - preserved existing routes and authentication behavior.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched frontend files.
- Stage status: Phase 6 branding pass completed.
- Phase status: redesign progression advanced with frontend-only changes.

### 2026-05-18 - UI/UX Redesign Continuation (Enrollments/Results/Payments + Phase-Level Summary Formatting)
- Completed continuation wave-B for remaining high-traffic operational pages.
- Recent request issue:
  - proceed with the next step and keep implementation/validation summaries at the end of each completed redesign phase.
- Implementation Summary:
  - polished Enrollments, Results, and Payments pages with section-card continuity, filter-toolbar consistency, status/empty-state visual alignment, and unchanged backend wiring,
  - normalized `Docs/Improved UI and look.md` so each completed phase carries its own completion summary block with markdown-lint-compliant structure.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched continuation views and redesign document.
- Stage status: continuation wave-B completed.
- Phase status: frontend redesign continuation advanced with no backend changes.

### 2026-05-18 - UI/UX Redesign Continuation (Students/Courses/Admin Users Polish)
- Continued the frontend-only redesign with additional page-level harmonization.
- Recent request issue:
  - proceed with extended UI consistency on additional admin/academic management pages.
- Implementation Summary:
  - polished Students, Courses, and Admin Users views using the new design system without changing workflow logic,
  - added reusable section helper styles in the global stylesheet.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed.
- Stage status: continuation page-polish pass completed.
- Phase status: frontend redesign rollout advanced with no backend changes.

### 2026-05-18 - UI/UX Redesign Request (Execution Snapshot)
- Completed the requested frontend-only portal redesign pass.
- Recent request issue:
  - the application required a complete UI/UX refresh to look like a premium modern SaaS product for educational institutions while preserving existing backend behavior.
- Implementation Summary:
  - refreshed the shared shell, sidebar, top bar, dashboard view, chatbot surface, and global visual system using only Razor/CSS/frontend JS,
  - preserved current controllers, endpoints, services, models, and data flow.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the redesign updates,
  - workspace diagnostics reported no errors in the touched frontend files.
- Stage status: Frontend SaaS visual refresh completed.
- Phase status: UX/design enhancement request completed with no backend changes.

### 2026-05-18 - Documentation Synchronization Follow-up (Execution Snapshot)
- Completed follow-up synchronization request across mandatory planning/traceability documents.
- Recent request issue:
  - requested update required PRD, Function List, Complete Functionality Reference, Development Plan, and Database Schema to include a unified follow-up closure entry.
- Implementation Summary:
  - inserted a dated follow-up snapshot in all five requested documents,
  - aligned issue/implementation/validation wording for consistent governance evidence.
- Validation Summary:
  - verified all five requested documents include this follow-up entry with issue, implementation, and validation sections,
  - confirmed this update does not introduce runtime code changes, API changes, or schema mutations.
- Stage status: Documentation synchronization follow-up completed.
- Phase status: Governance trackers remain aligned for the latest request cycle.

### 2026-05-18 - Stage 40.1 PhoneNumber/SMS Recipient Dependency Completion (Execution Snapshot)
- Completed phone persistence and SMS recipient dependency implementation.
- Implementation Summary:
  - added optional `PhoneNumber` on users with EF mapping and migration support,
  - implemented notification repository lookup for active user phone numbers,
  - wired optional phone support through admin user create/update/list, user CSV import, and student self-registration paths.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test Tabsan.EduSphere.sln -v minimal --filter "FullyQualifiedName~Phase28Stage2Tests|FullyQualifiedName~UserImportAndForceChangeIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~StudentRegistration"` passed (`13/13`).
- Stage status: Stage 40.1 completed.
- Phase status: SMS delivery dependency closure complete for user phone-backed recipient resolution.

### 2026-05-18 - StudentLifecycle Notification Completion (Execution Snapshot)
- Completed StudentLifecycle notification implementation.
- Implementation Summary:
  - added notification dispatch for graduation, promotion, deactivation, and reactivation actions,
  - added admin notification fan-out when profile-change and teacher-modification requests are created,
  - added request outcome notifications to requestor/teacher on approval and rejection.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~StudentLifecycleIntegrationTests -v minimal` passed (`7/7`).
- Stage status: lifecycle notification tasks completed.
- Phase status: Student lifecycle workflow completeness improved without schema change.

### 2026-05-18 - DeepScan Phase 40 Closure and Production Readiness Revalidation (Execution Snapshot)
- Completed DeepScan closure revalidation phase.
- Implementation Summary:
  - re-ran the targeted validation bundle for all previously open DeepScan gaps,
  - appended task-by-task re-execution closure outputs to `Docs/DeepScan.md`,
  - synchronized closure evidence across mandatory governance trackers.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter EnrollmentServiceWaitlistTests -v minimal` passed (`2/2`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter AuthSecurityUxTests -v minimal` passed (`7/7`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
  - final severity classification confirms no unresolved critical/high gap.
- Stage status: Stage 40.1 and Stage 40.2 completed.
- Phase status: Phase 40 completed.

### 2026-05-18 - DeepScan Stage 39.4 EF Relationship and Query-Filter Warning Cleanup (Execution Snapshot)
- Completed EF mapping/filter warning remediation stage.
- Implementation Summary:
  - aligned dependent query filters with filtered required principals across the affected EF mappings,
  - fixed quiz question relationship mapping to explicit parent navigation to remove shadow-FK behavior,
  - removed `CourseType` DB default configuration that triggered enum sentinel warning behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
  - verified targeted EF warning set is no longer emitted during startup in the focused integration path.
- Stage status: Stage 39.4 completed.
- Phase status: DeepScan Phase 39 remediation stage completed for EF warning cleanup.

### 2026-05-18 - DeepScan Stage 39.3 MFA Hardening (TOTP + Recovery Codes) (Execution Snapshot)
- Completed the MFA hardening remediation stage.
- Implementation Summary:
  - replaced demo-code MFA checks with per-user TOTP verification in auth service flow,
  - added recovery-code generation, hashed persistence, and one-time consumption,
  - added authenticated MFA enrollment endpoints and migration-backed user MFA persistence fields.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter AuthSecurityUxTests` passed (`7/7`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`4/4`),
  - verified login/refresh/force-change-password compatibility remained intact.
- Stage status: Stage 39.3 completed.
- Phase status: DeepScan remediation roadmap advanced by one completed stage.

### 2026-05-18 - DeepScan Stage 39.1 Enrollment Waitlist and Seat-Promotion Workflow (Execution Snapshot)
- Completed the enrollment waitlist remediation stage.
- Implementation Summary:
  - added a waitlisted enrollment state plus promotion helpers in the domain model,
  - updated enrollment service behavior to queue students when the offering is full and promote the oldest waitlisted student on drop,
  - added ordered waitlist retrieval in the repository layer.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter EnrollmentServiceWaitlistTests` passed (`2/2`),
  - verified queue-on-full behavior and deterministic promotion on seat release,
  - confirmed the change is isolated to enrollment workflow behavior and does not require schema migration.
- Stage status: Stage 39.1 completed.
- Phase status: DeepScan remediation roadmap advanced by one completed stage.

### 2026-05-18 - DeepScan Stage 39.2 Transactional CSV Import Strict Mode (Execution Snapshot)
- Completed the user CSV import strict-mode remediation stage.
- Implementation Summary:
  - added optional strict-mode rollback behavior to the user import service and API controller,
  - extended the import result payload with strict/permissive execution-path visibility,
  - preserved permissive import behavior for backward compatibility.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`4/4`),
  - verified strict-mode import rolls back all rows when a mixed-validity CSV is submitted,
  - verified the existing permissive import and forced-password-change flow still passes.
- Stage status: Stage 39.2 completed.
- Phase status: DeepScan remediation roadmap advanced by one completed stage.

### 2026-05-18 - DeepScan Gap Phase/Stage Synchronization Request (Execution Snapshot)
- Completed synchronized planning update for DeepScan-identified missing/partial areas.
- Implementation Summary:
  - added consolidated staged remediation plan coverage for waitlist workflow, transactional import strict mode, MFA hardening, and EF warning cleanup,
  - synchronized this request snapshot across PRD, consolidated issues, function list, complete functionality reference, development plan, and schema tracker.
- Validation Summary:
  - verified all six requested documents include a dated DeepScan-gap synchronization entry,
  - verified each entry includes implementation and validation sections,
  - confirmed no runtime code changes or schema migrations were introduced in this documentation pass.
- Stage status: DeepScan-gap planning synchronization completed.
- Phase status: Remediation roadmap captured for upcoming implementation phases.

### 2026-05-18 - Documentation Synchronization Request (Execution Snapshot)
- Completed requested documentation synchronization across mandatory execution/planning trackers.
- Implementation Summary:
  - added a dated request-closure snapshot to PRD, consolidated issues, function list, complete functionality reference, development plan, and database schema docs,
  - aligned all six entries to a consistent issue statement and closeout wording.
- Validation Summary:
  - verified all six requested documents include the 2026-05-18 issue + implementation + validation entry,
  - verified each entry explicitly records implementation and validation sections,
  - confirmed no runtime code changes or schema migrations are part of this closeout.
- Stage status: Documentation synchronization request completed.
- Phase status: Governance trackers synchronized for current request cycle.

### 2026-05-15 - Final Phase 37/38 Execute Closure (Execution Snapshot)
- Completed execute-mode finalization for publish-separation phases.
- Implementation Summary:
  - Phase 37 script finalized to separate runtime app and license app publish outputs with stable execute-mode behavior,
  - Phase 38 script finalized to package non-runtime repository assets separately from runtime app outputs,
  - documentation baseline synchronized for user guides, training, and import templates.
- Validation Summary:
  - Phase 37 execute evidence report passed (`4/4`) at `Artifacts/Phase37/Publish-Separation-20260515.md`,
  - Phase 38 execute evidence report passed (`7/7`) at `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`,
  - separated package outputs were generated for app/license/non-runtime bundles.
- Stage status: Phase 37 and Phase 38 execute closeout completed.
- Phase status: Final roadmap phases completed.

### 2026-05-14 - Phase 23 Stage 23.2 (Execution Snapshot)
- Completed dynamic academic labels and context stage from `Docs/Advance-Enhancements.md`.
- Implementation Summary:
  - validated centralized policy-based vocabulary mapping in `LabelService` for University, School, and College modes,
  - validated authenticated label API contract via `GET /api/v1/labels`,
  - validated web client/module-composition consumption path for dynamic terminology rendering,
  - added comprehensive integration coverage (`DynamicLabelIntegrationTests`) for mode and precedence behavior.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~DynamicLabelIntegrationTests" -v minimal` passed (`8/8`),
  - label-service unit validation in `Phase24Tests` remains green (`4/4`),
  - unauthenticated access guard and request consistency checks validated in integration suite.
- Documentation synchronization: Stage 23.2 closeout details are propagated to Function List, Functionality, Schema, PRD, and Command trackers.
- Stage status: Stage 23.2 completed.
- Phase status: Phase 23 in progress (next: Stage 23.3 Dashboard Context Switching).

### 2026-05-13 - Phase 23 Stage 23.1 (Execution Snapshot)
- Completed institution-type foundation confirmation from `Docs/Advance-Enhancements.md`.
- Implementation Summary:
  - confirmed global institution mode support and persistence path for School, College, and University,
  - confirmed backward-compatible University-default behavior in institution policy resolution.
- Validation Summary:
  - verified policy snapshot/default and save/load contracts in application service layer,
  - verified seed baseline includes the three institution mode flags in SQL core seed script.
- Stage status: Stage 23.1 completed.
- Phase status: Phase 23 in progress (next: Stage 23.2).

### 2026-05-13 - Institute Parity Stage 7.3 (Execution Snapshot)
- Completed Phase 7 Stage 7.3 monitoring and support handover.
- Implementation Summary:
  - added parity monitoring guidance for report/analytics failure signals and institute-scope incident triage,
  - added SuperAdmin support handover checklist for parity incidents covering role, institute, scope, and route/menu checks.
- Validation Summary:
  - verified Stage 7.3 monitoring/support guidance exists in the functionality and SuperAdmin guide docs,
  - verified command pointer progression to Stage 7.4.
- Stage status: Stage 7.3 completed.
- Phase status: Phase 7 in progress (next: Stage 7.4).

### 2026-05-13 - Institute Parity Stage 7.4 (Execution Snapshot)
- Completed Phase 7 release exit criteria.
- Implementation Summary:
  - finalized Phase 7 release-readiness evidence in the institute parity phase tracker,
  - synchronized the mandatory handoff trackers and advanced the command pointer beyond Phase 7.
- Validation Summary:
  - verified Stage 7.4 completion evidence is recorded across the required tracker set,
  - verified the release handoff transitions to the next roadmap stage without runtime or schema changes.
- Stage status: Stage 7.4 completed.
- Phase status: Phase 7 completed (next: Phase 8 infrastructure tuning).

### 2026-05-13 - Institute Parity Stage 7.2 (Execution Snapshot)
- Completed Phase 7 Stage 7.2 functional documentation update.
- Implementation Summary:
  - updated functionality reference with institute parity role/scope behavior guidance,
  - updated user-facing guides with explicit role/institute filter behavior instructions for Admin, Faculty, and Student flows.
- Validation Summary:
  - verified Stage 7.2 snapshots/checkpoints are synchronized across mandatory tracking docs,
  - verified command pointer progression to Stage 7.3.
- Stage status: Stage 7.2 completed.
- Phase status: Phase 7 in progress (next: Stage 7.3).

### 2026-05-13 - Institute Parity Stage 7.1 (Execution Snapshot)
- Completed Phase 7 Stage 7.1 deployment runbook finalization.
- Implementation Summary:
  - finalized script deployment run-order and environment notes in `Scripts/README.md`,
  - added rollback/verification checklist covering backup, failure handling, and sign-off evidence requirements.
- Validation Summary:
  - verified required deployment scripts (`01` to `05`) exist and are mapped in run-order guidance,
  - verified schema creation/context switch and post-deployment fail-fast checks are present in scripts.
- Stage status: Stage 7.1 completed.
- Phase status: Phase 7 in progress (next: Stage 7.2).

### 2026-05-13 - Institute Parity Stage 6.4 (Execution Snapshot)
- Completed Phase 6 Stage 6.4 exit criteria.
- Implementation Summary:
  - consolidated Stage 6 parity evidence using existing Stage 6.1/6.2/6.3 integration suites,
  - confirmed phase-exit certification scope without introducing runtime code or schema changes.
- Validation Summary:
  - consolidated parity integration run passed (`132/132`) across lifecycle/submenu/report parity, cross-role UAT matrix, authorization regression, analytics parity, and performance/query validation suites,
  - confirmed no critical/blocker defects in Phase 6 parity scope.
- Stage status: Stage 6.4 completed.
- Phase status: Phase 6 completed (next: Phase 7 Stage 7.1).

### 2026-05-13 - Institute Parity Stage 6.3 (Execution Snapshot)
- Completed Phase 6 Stage 6.3 performance and query validation.
- Implementation Summary:
  - added parity index read-usage integration checks for institute-filtered query paths on programs, courses, and offerings,
  - added no-major-regression latency budget integration checks on common Admin dashboard/report endpoints.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~PerformanceQueryValidationIntegrationTests" -v minimal` passed (`2/2`),
  - confirmed Stage 6.3 targets returned no `401/403` authorization regressions and no `5xx` responses in measured loops.
- Stage status: Stage 6.3 completed.
- Phase status: Phase 6 in progress (next: Stage 6.4).

### 2026-05-13 - Institute Parity Stage 6.2 (Execution Snapshot)
- Completed Phase 6 Stage 6.2 cross-role UAT matrix.
- Implementation Summary:
  - added cross-role UAT matrix integration tests for report catalog role visibility across institution claims `0/1/2`,
  - added matrix checks for account-security locked endpoint role boundaries across institution claims `0/1/2`,
  - added matrix checks for attendance-by-offering privileged-role and student-forbidden outcomes across institution claims `0/1/2`.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~CrossRoleUatMatrixIntegrationTests|FullyQualifiedName~ReportCatalogIntegrationTests|FullyQualifiedName~AccountSecurityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`100/100`),
  - verified role-boundary outcomes remain stable for SuperAdmin/Admin/Faculty/Student across School/College/University institution contexts.
- Stage status: Stage 6.2 completed.
- Phase status: Phase 6 in progress (next: Stage 6.3).

### 2026-05-13 - Institute Parity Stage 6.1 (Execution Snapshot)
- Completed Phase 6 Stage 6.1 automated test expansion.
- Implementation Summary:
  - added lifecycle matched-institute Admin allow-path integration coverage,
  - added student submenu explicit department-filter scope-shaping integration coverage,
  - added enrollment-summary report matched-institution Admin allow-path integration coverage.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~StudentSubmenuParityIntegrationTests|FullyQualifiedName~ReportExportsIntegrationTests" -v minimal` passed (`28/28`),
  - verified institute mismatch-deny checks remain intact while matched-scope parity requests succeed on targeted paths.
- Stage status: Stage 6.1 completed.
- Phase status: Phase 6 in progress (next: Stage 6.2).

### 2026-05-13 - Institute Parity Stage 5.4 (Execution Snapshot)
- Completed Phase 5 Stage 5.4 exit criteria.
- Implementation Summary:
  - executed the full script deployment chain (`01` -> `02` -> `03` -> `05`) as one-run readiness gate,
  - fixed replay collision behavior in full dummy script by reusing existing superadmin identity when present,
  - confirmed post-deployment checks report institute-level parity aggregates and duplicate-safety signals.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`3/3`),
  - one-run sqlcmd execution completed with successful full dummy seed completion and post-check parity metrics.
- Stage status: Stage 5.4 completed.
- Phase status: Phase 5 completed (next: Phase 6 Stage 6.1).

### 2026-05-13 - Institute Parity Stage 5.3 (Execution Snapshot)
- Completed Phase 5 Stage 5.3 data quality and replay safety.
- Implementation Summary:
  - hardened full dummy replay alignment by updating deterministic seeded department/user fields on rerun,
  - expanded post-deployment checks with institute-level aggregate counts for School/College/University parity datasets,
  - added critical workflow coverage count checks and duplicate-safety checks for seeded usernames/registration numbers.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`3/3`),
  - script verification confirms new institute-aggregate and replay-safety check outputs are present in post-deployment checks.
- Stage status: Stage 5.3 completed.
- Phase status: Phase 5 in progress (next: Stage 5.4).

### 2026-05-13 - Institute Parity Stage 5.2 (Execution Snapshot)
- Completed Phase 5 Stage 5.2 full dummy coverage.
- Implementation Summary:
  - expanded full dummy script with deterministic institute-assigned users and department institution-type alignment updates,
  - added assignment-junction baseline rows for admin/faculty scoped parity scenarios,
  - added buildings, rooms, timetables, and timetable-entry coverage across representative School/College/University departments,
  - added payment receipts, transcript export artifacts, and lifecycle/report artifacts (bulk promotions, graduation approvals, school streams, student report cards).
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`3/3`),
  - script verification confirms new parity-coverage entity blocks and deterministic institution-assignment rows are present.
- Stage status: Stage 5.2 completed.
- Phase status: Phase 5 in progress (next: Stage 5.3).

### 2026-05-13 - Institute Parity Stage 5.1 (Execution Snapshot)
- Completed Phase 5 Stage 5.1 core seed coverage.
- Implementation Summary:
  - updated core seed script to initialize institution policy flags for School/College/University,
  - seeded deterministic baseline departments spanning all institution types,
  - normalized legacy report keys and seeded current report definition/role-access matrix,
  - aligned baseline sidebar role-access seed with explicit SuperAdmin allow rows on core menus.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`3/3`),
  - script verification confirms institution policy keys and institution-typed foundational seed rows are present.
- Stage status: Stage 5.1 completed.
- Phase status: Phase 5 in progress (next: Stage 5.2).

### 2026-05-13 - Institute Parity Stage 4.4 (Execution Snapshot)
- Completed Phase 4 exit-criteria validation.
- Implementation Summary:
  - no new code changes required for the phase-exit gate,
  - validated the repaired analytics and report behaviors under the full integration suite,
  - confirmed no schema or migration work was needed to close Phase 4.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -v minimal` passed (`124/124`),
  - no regressions detected in the broader report, analytics, role, and institute guard paths.
- Stage status: Stage 4.4 completed.
- Phase status: Phase 4 completed (next: Phase 5 Stage 5.1).

### 2026-05-13 - Institute Parity Stage 4.3 (Execution Snapshot)
- Completed Phase 4 Stage 4.3 broken report fixes.
- Implementation Summary:
  - repaired faculty scope enforcement for department-scoped report endpoints,
  - added faculty department-assignment checks for GPA, enrollment, semester-results, low-attendance, and FYP status report routes,
  - replaced strict faculty offering ownership check with assignment-based department scope checks for offering-scoped reports,
  - added deterministic report reliability integration tests for missing-filter and unassigned-department deny scenarios.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~ReportExportsIntegrationTests|FullyQualifiedName~ReportCatalogIntegrationTests|FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~StudentSubmenuParityIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AdminUserManagementIntegrationTests" -v minimal` passed (`42/42`),
  - verified faculty missing required report filters return `400` and unassigned department report filters return `403` on repaired routes.
- Stage status: Stage 4.3 completed.
- Phase status: Phase 4 in progress (next: Stage 4.4).

### 2026-05-13 - Institute Parity Stage 4.2 (Execution Snapshot)
- Completed Phase 4 Stage 4.2 reports filter expansion.
- Implementation Summary:
  - added institution filter support to report APIs, export endpoints, and queued result-summary export requests,
  - added role-aware institute defaulting and mismatch-deny behavior in report controller for constrained roles,
  - added portal report institution filter controls and institution-aware department narrowing across report pages,
  - expanded report integration tests for institute-filter scoping and explicit mismatch denial.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~ReportExportsIntegrationTests|FullyQualifiedName~SidebarMenuIntegrationTests|FullyQualifiedName~StudentSubmenuParityIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AdminUserManagementIntegrationTests" -v minimal` passed (`43/43`),
  - verified report endpoints honor institute filter scoping and deny explicit claim mismatch filters for constrained roles.
- Stage status: Stage 4.2 completed.
- Phase status: Phase 4 in progress (next: Stage 4.3).

### 2026-05-13 - Institute Parity Stage 4.1 (Execution Snapshot)
- Completed Phase 4 Stage 4.1 analytics filter expansion.
- Implementation Summary:
  - added institution filter support to analytics API and analytics service query paths,
  - added role-aware institute defaulting and mismatch-deny behavior in analytics controller for constrained roles,
  - added portal analytics institute + department filter controls with claim-based default institute scope,
  - added analytics institute parity integration tests for mismatch-deny and default-scope behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~ReportExportsIntegrationTests|FullyQualifiedName~SidebarMenuIntegrationTests|FullyQualifiedName~StudentSubmenuParityIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AdminUserManagementIntegrationTests" -v minimal` passed (`41/41`),
  - verified constrained-role analytics requests auto-scope to claim institution and reject explicit mismatch filters.
- Stage status: Stage 4.1 completed.
- Phase status: Phase 4 in progress (next: Stage 4.2).

### 2026-05-13 - Institute Parity Stage 3.4 (Execution Snapshot)
- Completed Phase 3 Stage 3.4 exit criteria.
- Implementation Summary:
  - consolidated Phase 3 parity closure across Stage 3.1 (core module CRUD parity), Stage 3.2 (lifecycle institute scope), and Stage 3.3 (student submenu institute scope),
  - fixed web compile parity by extending shared portal `LookupItem` with optional `InstitutionType` used by institute-aware department filtering logic,
  - no additional DB or policy mutation introduced in this closeout stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -v minimal` passed (`115/115`),
  - verified no regressions in role/institute guards and Phase 3 module parity behaviors under full integration run.
- Stage status: Stage 3.4 completed.
- Phase status: Phase 3 completed (next: Phase 4 Stage 4.1).

### 2026-05-13 - Institute Parity Stage 3.3 (Execution Snapshot)
- Completed Phase 3 Stage 3.3 student submenu parity.
- Implementation Summary:
  - added admin assignment + institution-claim scope enforcement to student list endpoint used by student-related submenu pages,
  - added explicit forbidden response behavior for institute-mismatched department-filter requests,
  - ensured unfiltered student list results are institute-compatible for Admin callers,
  - updated student submenu table terminology to institute-neutral `Level` labels in Students and Enrollments views.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~StudentSubmenuParityIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AdminUserManagementIntegrationTests|FullyQualifiedName~SidebarMenuIntegrationTests|FullyQualifiedName~ReportExportsIntegrationTests" -v minimal` passed (`39/39`),
  - verified institute mismatch on student list department filter returns `403` for Admin caller,
  - verified institute-compatible student list shaping for Admin caller and no regressions in Stage 2/Stage 3.2 suites.
- Stage status: Stage 3.3 completed.
- Phase status: Phase 3 in progress (next: Stage 3.4).

### 2026-05-13 - Institute Parity Stage 3.2 (Execution Snapshot)
- Completed Phase 3 Stage 3.2 student lifecycle institute parity.
- Implementation Summary:
  - enforced lifecycle API scope checks for Admin based on department assignment and institute-type claim compatibility,
  - preserved SuperAdmin unrestricted lifecycle behavior,
  - projected JWT `institutionType` into web session identity and filtered portal lifecycle department options for non-SuperAdmin users,
  - fixed lifecycle portal action wiring to preserve selected department/semester context during promote/graduate operations,
  - added dedicated lifecycle integration tests for institution-mismatch deny cases.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AdminUserManagementIntegrationTests|FullyQualifiedName~SidebarMenuIntegrationTests|FullyQualifiedName~ReportExportsIntegrationTests" -v minimal` passed (`37/37`),
  - verified lifecycle Admin requests are denied on institution mismatch even when department assignment exists,
  - verified Stage 2 authorization/report/sidebar and Stage 3.1 admin-management parity suites remained green.
- Stage status: Stage 3.2 completed.
- Phase status: Phase 3 in progress (next: Stage 3.3).

### 2026-05-13 - Institute Parity Stage 3.1 (Execution Snapshot)
- Completed Phase 3 Stage 3.1 core academic/admin module parity slice for department/course institution-type correctness.
- Implementation Summary:
  - removed portal silent University-only department write behavior by passing explicit institution type through controller and API client,
  - updated Departments portal UX to show institution type and expose institution-type selectors in create/edit flows,
  - added cross-institution integration test covering School/College/University department+course CRUD when policy enables all,
  - hardened admin assignment round-trip test to select/create institution-compatible departments in mixed-institution datasets.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~AdminUserManagementIntegrationTests|FullyQualifiedName~SidebarMenuIntegrationTests|FullyQualifiedName~ReportExportsIntegrationTests" -v minimal` passed (`35/35`),
  - verified no regression in Stage 2 authorization/menu/report parity suites while adding Phase 3 CRUD parity checks.
- Stage status: Stage 3.1 completed.
- Phase status: Phase 3 in progress (next: Stage 3.2).

### 2026-05-13 - Institute Parity Stage 2.4 (Execution Snapshot)
- Completed Phase 2 Stage 2.4 exit criteria.
- Implementation Summary:
  - completed consolidated role + institute access-matrix validation across Stage 2 authorization surfaces,
  - verified Stage 2.1 assignment controls, Stage 2.2 report institute scope checks, and Stage 2.3 menu/action guard consistency as one integrated matrix,
  - no additional runtime code mutation introduced in this closeout stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~AdminUserManagementIntegrationTests|FullyQualifiedName~ReportExportsIntegrationTests|FullyQualifiedName~SidebarMenuIntegrationTests" -v minimal` passed (`34/34`),
  - verified cross-role authorization outcomes remain consistent for SuperAdmin/Admin/Faculty/Student.
- Stage status: Stage 2.4 completed.
- Phase status: Phase 2 completed (next: Phase 3 Stage 3.1).

### 2026-05-13 - Institute Parity Stage 2.3 (Execution Snapshot)
- Completed Phase 2 Stage 2.3 menu/action guard consistency.
- Implementation Summary:
  - added centralized portal action guard checks that enforce sidebar visibility alignment,
  - added canonical action-to-menu key mapping for parity-scope portal routes,
  - added integration tests validating hidden-menu/endpoint consistency behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~SidebarMenuIntegrationTests" -v minimal` passed (`14/14`),
  - verified Admin hidden settings path is denied while SuperAdmin visible path remains accessible.
- Stage status: Stage 2.3 completed.
- Phase status: Phase 2 in progress (next: Stage 2.4).

### 2026-05-13 - Institute Parity Stage 2.2 (Execution Snapshot)
- Completed Phase 2 Stage 2.2 role-scoped institute enforcement.
- Implementation Summary:
  - added `institutionType` JWT claim emission for explicitly assigned users,
  - added report-handler institute-scope checks for Admin/Faculty layered with existing role/department/offering enforcement,
  - added integration test coverage for assignment-valid but institute-mismatched admin report access denial.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused integration run passed (`20/20`) for report export and admin management suites,
  - confirmed institute mismatch on report endpoint returns `403` for Admin caller.
- Stage status: Stage 2.2 completed.
- Phase status: Phase 2 in progress (next: Stage 2.3).

### 2026-05-13 - Institute Parity Stage 2.1 (Execution Snapshot)
- Completed Phase 2 Stage 2.1 SuperAdmin global capability.
- Implementation Summary:
  - added SuperAdmin faculty department assignment APIs (assign/remove/list/list candidates),
  - added user-vs-department institution-type compatibility validation for admin/faculty assignment operations,
  - added faculty assignment revoke request contract DTO.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~AdminUserManagementIntegrationTests" -v minimal` passed (`6/6`),
  - validated new SuperAdmin faculty assignment round-trip and mismatch rejection checks in integration suite.
- Stage status: Stage 2.1 completed.
- Phase status: Phase 2 in progress (next: Stage 2.2).

### 2026-05-13 - Institute Parity Stage 1.4 (Execution Snapshot)
- Completed Phase 1 Stage 1.4 exit criteria.
- Implementation Summary:
  - expanded post-deployment checks with institute-type representation and validity assertions,
  - added orphan-count validation queries for institute-linked academic and assignment relationships.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted integration/security validation passed (`4/4`) using AdminUserManagement + SecurityValidation test filters,
  - verified Stage 1.4 SQL check markers are present in the post-deployment check script.
- Stage status: Stage 1.4 completed.
- Phase status: Phase 1 completed (next: Phase 2 Stage 2.1).

### 2026-05-13 - Institute Parity Stage 1.3 (Execution Snapshot)
- Completed Phase 1 Stage 1.3 script hardening.
- Implementation Summary:
  - updated master schema script with idempotent Stage 1.1/1.2 parity DDL and migration-history inserts,
  - updated maintenance script with parity index guardrails and safe legacy-index replacement,
  - updated post-deployment check script with explicit parity migration/column/index assertions.
- Validation Summary:
  - verified new migration IDs and parity index checks are present in all three scripts,
  - verified all new DDL operations are guarded for replay-safe/idempotent execution (`COL_LENGTH`, `sys.indexes`, migration-history checks).
- Stage status: Stage 1.3 completed.
- Phase status: Phase 1 in progress (next: Stage 1.4).

### 2026-05-13 - Institute Parity Stage 1.2 (Execution Snapshot)
- Completed Phase 1 Stage 1.2 referential integrity and indexing hardening.
- Implementation Summary:
  - enforced program code uniqueness per department,
  - added academic write-path integrity checks for course/offering creation,
  - added student profile program-department alignment validation,
  - added Stage 1.2 index pack for report/analytics and scope-guard query paths,
  - added migration `20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes` with enrollment status column normalization for index support.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln` passed,
  - targeted test set passed: `AdminUserManagementIntegrationTests` + `SecurityValidationTests` (`8/8`),
  - verified migration pipeline applies both Stage 1.1 and Stage 1.2 migrations in integration startup.
- Stage status: Stage 1.2 completed.
- Phase status: Phase 1 in progress (next: Stage 1.3).

### 2026-05-13 - Institute Parity Stage 1.1 (Execution Snapshot)
- Completed Phase 1 Stage 1.1 institute model normalization.
- Implementation Summary:
  - added canonical `InstitutionType` to `Department` domain entity,
  - added EF persistence + default + institute index on departments,
  - added migration `20260513121000_Phase1Stage11DepartmentInstitutionType`,
  - updated department API contracts and policy enforcement to validate institution type against active license policy.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln` passed,
  - targeted unit validation (`SecurityValidationTests`) passed `4/4`,
  - verified backward-compatible defaults/optionals for existing department create/update flows.
- Stage status: Stage 1.1 completed.
- Phase status: Phase 1 in progress (next: Stage 1.2).

### 2026-05-13 - Institute Parity Stage 0.4 (Execution Snapshot)
- Completed Phase 0 exit criteria and readiness sign-off.
- Implementation Summary:
  - consolidated Stage 0.1/0.2/0.3 outputs into prioritized remediation backlog,
  - confirmed traceability from reported issues to remediation phases/stages.
- Validation Summary:
  - verified all Phase 0 baseline artifacts are complete and internally consistent,
  - verified no blocker preventing transition to Phase 1 execution.
- Stage status: Stage 0.4 completed.
- Phase status: Phase 0 completed.

### 2026-05-13 - Institute Parity Stage 0.3 (Execution Snapshot)
- Completed report failure inventory baseline with required root-cause tagging.
- Implementation Summary:
  - inventoried historical and current report failures across result/report center/analytics surfaces,
  - classified outcomes into query logic, filter/join gaps, scoping mismatch, and dummy-data absence,
  - mapped each item to remediation stage ownership.
- Validation Summary:
  - validated evidence against issue docs, report controller guards, report repository query paths, and report integration tests,
  - confirmed historical critical failures are resolved,
  - confirmed remaining parity risks are filter-propagation and data completeness oriented.
- Stage status: Stage 0.3 completed.

### 2026-05-13 - Institute Parity Stage 0.2 (Execution Snapshot)
- Completed role and institute access matrix baseline for parity-scope modules.
- Implementation Summary:
  - mapped role-based view/create/edit/deactivate/export coverage by module,
  - mapped institute-scope enforcement basis (policy flags vs department/offering/ownership scope),
  - documented enforcement gaps for explicit institute checks.
- Validation Summary:
  - source-level verification completed on policy, admin-user, analytics/report, academic, assessment, lifecycle, and payment controller/service surfaces,
  - confirmed no runtime or schema mutation in this stage,
  - queued identified gaps for Phase 1-4 remediation stages.
- Stage status: Stage 0.2 completed.

### 2026-05-13 - Institute Parity Stage 0.1 (Execution Snapshot)
- Completed baseline module parity audit and dependency mapping for School/College/University execution planning.
- Implementation Summary:
  - audited API controllers/routes for all parity-scope modules,
  - mapped service/repository dependencies and scope-guard patterns,
  - captured University-default hotspots requiring normalization in next stages.
- Validation Summary:
  - static evidence collected from controller/service/infrastructure/web/db source scans,
  - confirmed broad module coverage exists; no runtime code changes in this stage,
  - flagged residual University-centric defaults for Stage 0.2 onward.
- Stage status: Stage 0.1 completed.

### 2026-05-12 - Institution License Validation Phase 7 (Execution Snapshot)
- Completed SuperAdmin full-access matrix execution.
- Captured evidence in `Artifacts/Phase7/SuperAdmin/20260512-151302`.
- Validated CRUD and activation/deactivation operations:
  - Department create/update/deactivate.
  - Admin-user create/deactivate/reactivate.
- Validated policy mode-switch and cross-institution privileged visibility across School, College, and University modes.
- Matrix summary: `35` checks executed, `35` passed, `0` failed.
- Phase 7 completed; Institution License Validation plan closed.

### 2026-05-12 - Institution License Validation Phase 6 (Execution Snapshot)
- Completed role-boundary validation for institution-scoped report access.
- Captured evidence in `Artifacts/Phase6/Access/20260512-150824`.
- Verified report export scope enforcement behavior:
  - Admin assigned department succeeds; non-assigned department denied; missing scope rejected.
  - Faculty assigned offering succeeds; non-assigned offering denied; missing scope rejected.
  - Student remains blocked from operational report exports.
- Verified allowed student read surfaces continue to work (`/reports`, `/dashboard/context`).
- Phase 6 completed.

### 2026-05-12 - Institution License Validation Phase 5 (Execution Snapshot)
- Implemented explicit per-user institution assignment for manual admin creation and CSV import.
- Added persistence and migration support:
  - nullable `users.InstitutionType` column via EF migration `AddUserInstitutionTypeAssignment`.
- Added policy-aware validation behavior:
  - assignment to disabled institution type is rejected,
  - assignment to enabled institution type is accepted and persisted.
- Updated portal create-admin flow and import templates/documentation to include optional institution assignment.
- Captured evidence in `Artifacts/Phase5/Api` (`20260512-144212` set).
- Phase 5 completed.

### 2026-05-12 - Institution License Validation Phase 4 (Execution Snapshot)
- Completed mode-role validation for School, College, and University across SuperAdmin, Admin, Faculty, and Student.
- Captured execution evidence in `Artifacts/Phase4/ModeRole/20260512-142021` including:
  - policy, labels, capability matrix,
  - dashboard composition context,
  - report catalog,
  - scoped report data and CSV export checks,
  - negative authorization probes.
- Scoped report/export behavior validated:
  - SuperAdmin/Admin/Faculty succeed with valid scope filters,
  - Student operational report endpoints remain denied (`403`).
- Role/mode dashboard composition and vocabulary results align with institution-policy flags and report scope rules.
- Phase 4 completed.

### 2026-05-12 - Institution License Validation Phase 3 (Execution Snapshot)
- Completed mixed-mode license validation for all planned combinations:
  - School + College
  - School + University
  - College + University
  - School + College + University
- Captured evidence per combination for:
  - license upload,
  - institution policy,
  - labels,
  - portal capability matrix row union,
  - progression evaluation by institution type,
  - persisted `portal_settings` policy keys.
- Validated union behavior:
  - matrix rows expand according to enabled institution flags,
  - persisted DB keys match runtime policy output.
- Observed current label-resolution behavior:
  - School+College uses School vocabulary,
  - any combination containing University uses University vocabulary.
- Known tooling caveat persisted:
  - sequential uploads require clearing `consumed_verification_keys` because generated licenses reuse verification-key material.
- Phase 3 completed.

### 2026-05-12 - Institution License Validation Phase 2 (Execution Snapshot)
- Completed end-to-end mode validation for School, College, and University after applying policy persistence fix.
- Applied service fix:
  - `InstitutionPolicyService.SavePolicyAsync` now commits settings repository changes so policy flags persist.
- Phase 2 evidence captured for each mode:
  - successful license activation,
  - policy flags,
  - labels vocabulary,
  - portal capability matrix rows,
  - progression evaluation result by institution type,
  - DB policy-key confirmation in `portal_settings`.
- Results summary:
  - School: Grade/Promotion flow active, school-only matrix rows.
  - College: Year/Progression flow active, college-only matrix rows.
  - University: Semester/GPA flow active, university-only matrix rows.
- Known tooling caveat noted in validation:
  - generated licenses currently reuse verification key; sequential mode validation required resetting `consumed_verification_keys` between activations.
- Phase 2 completed.

### 2026-05-12 - Institution License Validation Phase 1 (Execution Snapshot)
- Ran Phase 1 endpoint validation using SuperAdmin credentials and HTTPS API host.
- Baseline observed:
  - license status: `Invalid`
  - license details: `None`
  - institution policy: `University=true`, `School=false`, `College=false`
- Attempted `.tablic` upload from `tools/Tabsan.Lic/License`.
- Initial upload failure root cause: DB save failed due legacy non-null `license_state` columns (`InstitutionScope`, `ExpiryType`) without defaults.
- Applied environment-level SQL defaults for those legacy columns and retried upload.
- Retry succeeded with `License activated successfully`.
- Post-upload state: `license status=Active`, `license details=Active (Yearly)`, policy remained `University=true`, `School=false`, `College=false`.
- Final module restriction validation via `GET /api/v1/portal-capabilities/matrix` confirmed `school=false`, `college=false`, `university=true` with no School/College-enabled rows.
- Phase 1 completed.

### 2026-05-12 - Institution License Validation Plan Added
- Added phased validation baseline in `Docs/Institution-License-Validation-Phases.md`.
- Defined 7 validation phases covering:
  - license-to-institution binding,
  - student lifecycle routing by School/College/University,
  - mixed-mode feature/configuration union,
  - institution-scoped charts/tables/menus/reports,
  - institution assignment in create/import user flows,
  - role-based institution access boundaries,
  - SuperAdmin full-permission verification.
- Each phase now has mandatory deliverables: `Implementation Summary`, `Validation Summary`, and `Status of Checks Done`.
- Each phase completion requires docs synchronization and git synchronization (commit, pull, push).

### 2026-05-11 — Phase 10 Complete
- Stage 10.1: Added a parameterized progressive gate runner for 10k -> 20k -> 50k -> 80k -> 100k progression plus an extended high-tier plan.
- Stage 10.2: Added bottleneck classification heuristics that report the first likely limiter from each gate summary.
- Stage 10.3: Added a retest loop so targeted fixes can be revalidated against the same gate before moving forward.
- Validation: PowerShell syntax check on `tests/load/run-phase10-progressive.ps1` passed; editor diagnostics on `tests/load/k6-phase10-progressive.js` reported no errors.

### 2026-05-11 — Phase 9 Complete
- Stage 9.1: Added OpenTelemetry metrics publishing with Prometheus scraping support in the API host.
- Stage 9.2: Added rolling request-latency capture with `/health/observability` snapshots for p50/p95/p99 tracking.
- Stage 9.3: Added database, CPU, memory, network, and error-rate health checks for continuous runtime monitoring.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`.

### 2026-05-11 — Phase 8 Complete
- Stage 8.1: Added `InfrastructureTuning:AutoScaling` controls and startup validation for replica bounds in API, Web, and BackgroundJobs.
- Stage 8.2: Added `InfrastructureTuning:HostLimits` controls with thread-pool minimum tuning and API/Web Kestrel concurrent connection settings.
- Stage 8.3: Added `InfrastructureTuning:NetworkStack` controls with HTTP/2 stream tuning and outbound HTTP handler connection limits.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`.

### 2026-05-11 — Phase 7 Complete
- Stage 7.1: Added queue offloading for account-security unlock/reset transactional emails so request handlers enqueue background work.
- Stage 7.2: Added queue platform integration with startup-configurable `QueuePlatform:Provider` supporting in-memory and RabbitMQ account-security queue processing.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`; syntax checks on touched files reported no errors.

### 2026-05-11 — Phase 6 Complete
- Stage 6.1: Added short-TTL distributed caching in `LibraryService` for safe external loan API lookups.
- Stage 6.2: Added channel-level circuit-breaker controls in `ResilientOutboundIntegrationGateway` with configurable threshold/open durations.
- Stage 6.3: Replaced blocking `.Result` reads in `GradebookService.GetGradebookAsync(...)` with awaited async results.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`; syntax checks on touched files reported no errors.

### 2026-05-11 — Phase 5 Complete
- Stage 5.1: Reworked the 50k/100k/1m/5m k6 scripts to `ramping-arrival-rate` workloads with randomized think-time.
- Stage 5.2: Added distributed generator shard controls (`GENERATOR_TOTAL`, `GENERATOR_INDEX`) and runner support for multi-machine execution.
- Stage 5.3: Standardized summary-first output (`--quiet`, summary-export + compact summaries) and kept heavy raw output as explicit diagnostics-only mode.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`; syntax checks on touched load scripts reported no errors.

### 2026-05-11 — Phase 4 Complete
- Stage 4.1: Added short-TTL distributed cache in `AnalyticsService` for expensive report reads (`performance`, `attendance`, `assignments`, `quizzes`).
- Stage 4.2: Added configurable static-asset cache headers in Web startup via `UseStaticFiles(...OnPrepareResponse...)` and `StaticAssetCaching` appsettings keys.
- Stage 4.3: Kept cache scope constrained to expensive shared-safe analytics reads and static file responses only.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`; syntax checks on touched files reported no errors.

### 2026-05-11 — Phase 3 Stage 3.3
- Added Kestrel transport tuning in API and Web startup for keep-alive timeout, request-header timeout, server-header suppression, and HTTP/2 ping tuning.
- Preserved Brotli/Gzip response compression with Fastest settings in both hosts.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`; syntax checks on the updated startup files reported no errors.

### 2026-05-11 — Phase 3 Stage 3.2
- Replaced `ContinueWith` wrappers with direct async `await` returns in hot repository methods for timetable, settings, quiz, and building/room reads.
- Kept the data-access layer fully async on the high-traffic query paths that still fed portal screens and scheduling/reporting flows.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`; syntax checks on touched repository files reported no errors.

### 2026-05-11 — Phase 3 Stage 3.1
- Added aggregated dashboard-context endpoint in API (`GET /api/v1/dashboard/context`) to reduce ModuleComposition screen round-trips.
- Updated portal ModuleComposition flow to consume the single dashboard-context payload instead of three API calls.
- Added Web client support for the aggregated dashboard-context response model.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`; syntax checks on touched files reported no errors.

### 2026-05-11 — Phase 2 Stage 2.3
- Hardened API startup to require Redis-backed distributed cache outside Development/Testing (`ScaleOut:RedisConnectionString`) so cache state stays stateless across nodes.
- Hardened Web startup to require shared data-protection key ring outside Development/Testing (`ScaleOut:SharedDataProtectionKeyRingPath`) so auth cookies stay decryptable across nodes.
- Preserved Development/Testing fallback behavior for local iteration.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`.

### 2026-05-11 — Phase 2 Stage 2.2
- Added Nginx least-connections baseline template for API horizontal routing (`Scripts/Phase2-Stage2.2-nginx-leastconn.conf.template`).
- Added Stage 2.2 load balancer control script to start/stop local balancer container with generated upstream members (`Scripts/Phase2-Stage2.2-LoadBalancer.ps1`).
- Added Stage 2.2 distribution validator script to sample request spread per instance (`Scripts/Phase2-Stage2.2-Validate-LB.ps1`).
- Updated scripts catalog with Stage 2.2 entries (`Scripts/README.md`).
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`.

### 2026-05-11 — Phase 2 Stage 2.1
- Added API per-instance identity bootstrap in startup using `ScaleOut:InstanceId` with machine/process fallback for horizontal node uniqueness.
- Added optional node telemetry header emission (`X-EduSphere-Instance`) controlled by `ScaleOut:ExposeInstanceHeader`.
- Added node probe endpoint `GET /health/instance` for load balancer verification across scaled API instances.
- Added operational script `Scripts/Phase2-Stage2.1-MultiInstance-Api.ps1` to launch/stop multiple local API nodes for baseline scale testing.
- Validation:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`.
  - `dotnet build src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj -v minimal` encountered expected file-lock warnings/errors due running API process (PID 35564).

### 2026-05-11 — Phase 1 Stage 1.4
- Added short-TTL dashboard composition caching in `DashboardCompositionService.GetWidgets(...)` with role + institution policy cache keys.
- Added short-TTL sidebar read caching in `SidebarMenuService` for top-level and role-visible menus, plus version bump invalidation on sidebar mutations.
- Added short-TTL notification read caching in `NotificationService` for inbox and unread badge responses, plus version bump invalidation on send/deactivate/mark-read mutations.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed `130/130`.

### 2026-05-11 — Phase 1 Stage 1.3
- Optimized notification inbox read path by introducing repository-level no-tracking option and using it in `NotificationService.GetInboxAsync`.
- Optimized unread badge count query by removing unnecessary Include loading in `NotificationRepository.GetUnreadCountAsync`.
- Optimized sidebar read paths in `SettingsRepository`:
  - no-tracking for top-level/sub-menu/visible-menu queries,
  - split-query pattern on include-heavy sidebar graph reads.
- Validation plan: re-run 12k and 16k load caps and compare p95 latency + error-rate against Stage 1.2 baseline.

### 2026-05-11 — Phase 1 Stage 1.2
- Tuned SQL connection pooling settings for API runtime profiles:
  - `appsettings.json` and `appsettings.Development.json` now include `Min Pool Size=20;Max Pool Size=500;Connect Timeout=30`.
  - `appsettings.Production.json` connection string placeholder now includes guidance values `Min Pool Size=50;Max Pool Size=800;Connect Timeout=30`.
- Objective: reduce connection churn and timeout spikes during high-concurrency load-test stages.
- Validation plan: rerun 12k and 16k caps and compare p95 latency/error deltas before Stage 1.3 query tuning.

### 2026-05-10 — Phase 33 Stage 33.3
- Added DataAnnotations-based validation to auth/admin DTOs:
  - login, refresh, change-password, and forced-password-change requests now enforce required and length-constrained inputs,
  - admin user create/update requests now enforce required username/email/password constraints.
- Added `SecurityValidationTests` for executable coverage of the hardened validation paths.
- Validation:
  - `dotnet build Tabsan.EduSphere.sln` passed.
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter "FullyQualifiedName~SecurityValidationTests"` passed `4/4`.
  - `dotnet test Tabsan.EduSphere.sln --no-build` passed `234/234`.

### 2026-05-10 — Phase 33 Stage 33.2
- Hardened runtime hosting behavior in API/Web:
  - added config-driven reverse-proxy trust options (`ReverseProxy:Enabled`, `KnownProxies`, `ForwardLimit`, `RequireHeaderSymmetry`),
  - restricted forwarded-header middleware activation to configured reverse-proxy mode,
  - added startup guardrails for unsafe production startup conditions.
- API-specific hardening:
  - startup guard requires non-empty `AppSettings:CorsOrigins` outside Development/Testing.
- Web-specific hardening:
  - removed localhost fallback behavior in login API base URL resolution,
  - removed localhost default from portal API connection model.
- Validation:
  - `dotnet build Tabsan.EduSphere.sln` passed.
  - `dotnet test Tabsan.EduSphere.sln --no-build` passed `230/230`.

### 2026-05-10 — Phase 33 Stage 33.1
- Re-scoped Phase 33 to `Hosting Configuration and Security Hardening` using `Docs/Refactoring-Hosting-Security.md` as execution baseline.
- Added explicit config-load bootstrapping in API/Web/BackgroundJobs startup paths:
  - `appsettings.json`,
  - `appsettings.{Environment}.json`,
  - environment variables.
- Added startup setting guards for required values (`DefaultConnection`, `EduApi:BaseUrl`) and placeholder-rejection for BackgroundJobs base connection string.
- Aligned `AppSettings` metadata in environment appsettings files for API/Web/BackgroundJobs portability.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed.

### 2026-05-10 — Phase 32 Stage 32.5
- Added `CredentialVerificationIntegrationTests` to preserve credential/run-command guardrails:
  - deterministic smoke users are provisioned for SuperAdmin/Admin/Faculty/Student,
  - `POST /api/v1/auth/login` is verified for each role,
  - login response token + expected role contract is asserted.
- Validation command (targeted):
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~CredentialVerificationIntegrationTests"`
- Validation: targeted integration tests passed `4/4`.

### 2026-05-10 — Phase 32 Stage 32.4
- Added report-center visibility/link guardrails in sidebar integration tests:
  - `report_center` is visible for Admin, Faculty, and Student,
  - report-center-visible roles can successfully load report catalog data,
  - student sidebar visibility baseline now includes `report_center`.
- Hardened sidebar seed role-access logic to self-heal existing role rows by applying expected allow/deny values during seeding.
- Synced SQL minimal seed role-access matrix so student includes `report_center`.
- Validation: targeted integration tests passed `12/12`.

### 2026-05-10 — Phase 32 Stage 32.3
- Added `SidebarMenuIntegrationTests.SetRoles_AllSeededMenus_AreAssignable` to preserve cross-phase Sidebar Settings guardrails:
  - every seeded top-level and sub-menu key accepts role-assignment updates,
  - menu-configuration actions remain operational for full sidebar inventory,
  - existing role visibility and system-menu protections remain intact.
- Validation: targeted integration tests passed `9/9`.

### 2026-05-10 — Phase 32 Stage 32.2
- Added `ReportExportsIntegrationTests` to preserve cross-phase report-export guardrails:
  - anonymous requests to export endpoints remain blocked,
  - attendance/result/assignment/quiz export routes return expected media types,
  - export responses preserve attachment filename contracts,
  - export payloads remain non-empty for downloadable output integrity checks.
- Validation: targeted integration tests passed `13/13`.

### 2026-05-10 — Phase 32 Stage 32.1
- Added `ReportCatalogIntegrationTests` to preserve cross-phase report-center guardrails:
  - report catalog remains role-scoped,
  - seeded report keys remain present for privileged roles,
  - student catalog remains restricted to student-allowed definitions,
  - each catalog key maps to a live report endpoint route (no 404 regressions).
- Validation: targeted integration tests passed `8/8`.

### 2026-05-10 — Phase 31 Stage 31.3
- Added Stage 31.3 load certification script `tests/load/k6-certification-bands.js` with executable band profiles for:
  - up-to-10k
  - 10k-100k
  - 100k-500k
  - 500k-1m
- Added Stage 31.3 recovery-smoke script `tests/load/recovery-smoke.ps1` for node/service restart recovery validation.
- Updated `tests/load/README.md` with Stage 31.3 run commands and threshold tables.
- Added Stage 31.3 certification document: `Docs/Phase31-Stage31.3-Performance-Reliability-Certification.md`.
- Validation: full automated tests passed `201/201`.

### 2026-05-10 — Phase 31 Complete
- Stage 31.1, Stage 31.2, and Stage 31.3 are complete.

### 2026-05-10 — Phase 31 Stage 31.2
- Added explicit audit-log emission to sensitive control-plane mutation endpoints in `FeatureFlagsController`, `TenantOperationsController`, and `InstitutionPolicyController`.
- Added Stage 31.2 integration suite (`Phase31Stage2SecurityHardeningTests`) for:
  - endpoint authorization guard coverage,
  - anonymous endpoint whitelist enforcement,
  - audit log coverage validation for sensitive actions.
- Added Stage 31.2 artifact document: `Docs/Phase31-Stage31.2-Security-Hardening.md`.
- Validation: targeted tests passed `4/4`; full automated tests passed `201/201`.

### 2026-05-10 — Phase 31 Stage 31.1
- Added Stage 31.1 regression matrix unit suite (`Phase31Stage1RegressionMatrixTests`) with 24 scenario combinations across institution mode, role, and license profile states.
- Added tenant isolation baseline verification using isolated settings repositories.
- Added matrix artifact and traceability document: `Docs/Phase31-Stage31.1-Regression-Matrix.md`.
- Validation: targeted tests passed `25/25`; full automated tests passed `197/197`.

### 2026-05-10 — Phase 30 Stage 30.3
- Added feature-flag DTOs/contracts and `IFeatureFlagService` for rollout/rollback operations.
- Implemented `FeatureFlagService` using `portal_settings` persistence with default flags and batch rollback disable flow.
- Added `FeatureFlagsController` (`api/v1/feature-flags`) for SuperAdmin list/get/save/rollback workflows.
- Added `tenant-operations.write` guard to tenant write endpoints for rollback-safe shutdown behavior.
- Added rollout and rollback runbook doc: `Docs/Phase30-Stage30.3-Reliability-Rollback-Runbook.md`.
- Added focused Stage 30.3 unit tests (`Phase30Stage3Tests`).
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed `172/172`.

### 2026-05-10 — Phase 30 Complete
- Completed Stage 30.1 (integration gateway), Stage 30.2 (tenant/subscription operations), and Stage 30.3 (reliability/rollback controls).

### 2026-05-10 — Phase 30 Stage 30.2
- Added tenant/subscription operations DTOs and service contract (`ITenantOperationsService`) for onboarding template, subscription plan controls, and tenant profile settings.
- Implemented `TenantOperationsService` in Application settings services, backed by `portal_settings` key-value persistence.
- Added SuperAdmin API controller `TenantOperationsController` with `GET/PUT` endpoints for onboarding-template, subscription-plan, and tenant-profile operations.
- Registered tenant operations service in API DI.
- Added focused unit tests (`Phase30Stage2Tests`) for defaults and persistence round-trip behavior.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed `169/169`.

### 2026-05-10 — Phase 30 Stage 30.1
- Added centralized outbound integration gateway contracts and runtime (`IOutboundIntegrationGateway`, `ResilientOutboundIntegrationGateway`) with channel-based retry/timeout policies.
- Added distributed-cache-backed dead-letter capture for terminal outbound integration failures.
- Routed existing outbound integration flows through gateway policy execution:
  - SMTP email delivery
  - In-app support/announcement push notifications
  - External library loan API calls
- Added communication integration diagnostics endpoints for gateway policies and dead-letter inspection.
- Added focused unit tests covering retry success, timeout dead-letter behavior, and default policy fallback.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed `166/166`.

### 2026-05-10 — Phase 29 Stage 29.3
- Added `Scripts/3-Phase29-ArchivePolicy.sql` with retention policy windows, dry-run visibility, and optional batched cleanup execution.
- Added `Scripts/4-Phase29-IndexMaintenance.sql` with fragmentation threshold planning and optional reorganize/rebuild execution.
- Added `Scripts/5-Phase29-CapacityGrowthDashboard.sql` for capacity footprint and recent growth telemetry.
- Updated `Scripts/README.md` with Stage 29.3 operations run commands.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed `162/162`.

### 2026-05-10 — Phase 29 Complete
- Stage 29.1 (index baseline), Stage 29.2 (heavy-list pagination), and Stage 29.3 (lifecycle maintenance operations) are complete.

### 2026-05-10 — Phase 29 Stage 29.2 Slice 3
- Added server-side pagination for payment receipt lists across repository, application service, API controller, portal client, and portal page.
- Replaced previous unbounded payment list paths with `page` and `pageSize` aware queries plus total-count metadata.
- Preserved the admin student-filtered Payments workflow while adding previous/next navigation.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed `162/162`.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 3
- Added provider-based file/media storage abstraction in API (`IMediaStorageService`, `LocalMediaStorageService`, `MediaStorageOptions`).
- Added `MediaStorage` configuration to API appsettings (provider mode, local root path, key prefix, optional public base URL).
- Migrated payment-proof upload flow to storage abstraction so database records persist storage object keys instead of hard-coded file system paths.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 2
- Moved `IMediaStorageService` into the Application layer to support provider-backed media persistence from both controllers and application services.
- Extended local provider with read-by-key support.
- Migrated graduation certificate generation/download to provider-backed storage with legacy `/certificates/*` fallback for existing records.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 3
- Migrated `LicenseController.Upload` temporary file workflow to provider-backed media storage save/read/delete operations.
- Added `LicenseValidationService.ActivateFromBytesAsync` for path-independent license verification and activation.
- Extended storage contract with delete support to clean temporary upload objects.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 4
- Added `AddConfiguredMediaStorage` to register storage provider implementation from configuration.
- Added `BlobMediaStorageService` as an object-storage style adapter behind `IMediaStorageService`.
- Added `BlobRootPath` storage configuration key in API environment appsettings.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 5
- Migrated portal logo upload in `PortalSettingsController` to provider-backed save flow through `IMediaStorageService`.
- Added `GET /api/v1/portal-settings/logo-files/{**storageKey}` endpoint to stream persisted logo bytes for branding display paths.
- Added category guardrails so only `portal-branding/logo` keys are anonymously streamable.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 6
- Extended `IMediaStorageService` with temporary read URL generation (`GenerateTemporaryReadUrlAsync`) for signed URL ready media reads.
- Added temporary signed URL generation support in both local and blob provider implementations.
- Updated portal logo-file endpoint to use provider temporary URL redirect when available, then fall back to in-process byte streaming.
- Added `SignedUrlSecret` placeholders in API appsettings files.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 7
- Enforced local signed URL validation (`exp`/`sig`) in `PortalSettingsController.GetLogoFile` when `MediaStorage:SignedUrlSecret` is configured.
- Added unsigned legacy URL compatibility redirect to short-lived local signed logo URLs.
- Added fixed-time signature comparison and expiry validation for local signed reads.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 8
- Added authenticated certificate streaming endpoint `GET /api/v1/graduation/certificate-files/{**storageKey}`.
- Updated `GET /api/v1/graduation/{id}/certificate` to redirect to temporary provider URLs when available or signed local certificate URLs otherwise.
- Added local signed URL validation (`exp`/`sig`) for certificate-file reads when signing is configured.
- Preserved legacy `/certificates/*` compatibility for existing path-based records.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 9
- Extended `IMediaStorageService` with metadata lookup support (`GetMetadataAsync`) and added content type/length fields on save results.
- Implemented provider metadata resolution in `LocalMediaStorageService` and `BlobMediaStorageService`.
- Updated portal logo and certificate streaming endpoints to use provider metadata for response content type selection.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 10
- Extended the storage contract with SHA-256 hash and optional download filename metadata on save and metadata reads.
- Implemented sidecar metadata persistence in local/blob storage providers.
- Updated certificate generation and certificate-file streaming so filename-aware downloads survive signed local and redirect-first reads.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Complete
- Stage 28.1, Stage 28.2, and Stage 28.3 are complete.
- Phase 28 delivered multi-node readiness, distributed cache/background offload, and provider-backed media hardening without schema changes.

### 2026-05-10 — Phase 29 Stage 29.1
- Added composite indexes for hot student/user/status recency queries in the EF model.
- Generated migration `20260509155457_20260510_Phase29_IndexBaseline`.
- Documented that the current schema has no `InstitutionId`, `YearId`, or `GradeId` columns yet, so Stage 29.1 targeted the active query contracts instead.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Stage 29.2 Slice 1
- Added server-side pagination for helpdesk ticket listing across the repository, application service, API controller, portal client, and portal page.
- Replaced the previous unbounded helpdesk list path with `page` and `pageSize` aware queries.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Stage 29.2 Slice 2
- Added server-side pagination for graduation application list endpoints across repository, application service, API controller, portal client, and portal pages.
- Replaced previous unbounded graduation list paths with `page` and `pageSize` aware queries plus total-count metadata.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-09 — Phase 28 Stage 28.1 Complete
- Completed **API and App Tier Scaling** as the first stage of the scalability architecture roadmap.
- Replaced Web session-backed connection/auth state with protected-cookie storage to keep portal nodes stateless across a load-balanced deployment.
- Added optional shared data-protection key-ring configuration to support multi-node cookie decryption.
- Enabled Brotli/Gzip response compression in API and Web, and configured JSON payload shaping to omit null values.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **160/160**.

### 2026-05-09 — Phase 28 Stage 28.2 Foundation Batch
- Added optional Redis-backed distributed cache registration in API startup, with distributed-memory fallback for local or single-node environments.
- Shifted module entitlement resolution and report catalog retrieval onto the shared cache layer for scale-out reuse.
- Added a hosted notification fan-out worker so large recipient batches no longer block the API request path.
- Added focused unit tests for deferred fan-out behavior.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.2 Completion
- Added queue-backed result-summary export jobs in `ReportController` with dedicated status and download endpoints.
- Added queue-backed result publish-all jobs in `ResultController` for asynchronous recalculation-heavy publishing.
- Added `ResultPublishJobWorker` and `ReportExportJobWorker` hosted services with distributed-cache-backed job state/payload storage.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; automated tests passed **162/162**.

### 2026-05-05 — Phase 1 Remediation Restart (Batch 1)
- Re-opened Phase 1 workstream to address Observed-Issues Phase 1 items.
- Implemented role-access remediation for offerings used by Assignments, Attendance, Results, and Quizzes page data loads.
- Applied SuperAdmin visibility correction for Report Center catalog retrieval.
- Applied sidebar cleanup: removed module-settings route mapping in dynamic menu and removed brand-header hyperlink behavior.
- Applied student lifecycle mapping fix to prevent empty GUID promote requests from semester-student payload mapping.

### 2026-05-05 — Phase 1 Remediation Restart (Batch 2)
- Completed Stage 1.4 script/runtime cleanup for Module Settings removal.
- Removed `module_settings` seeding from runtime database seeder and both SQL seed scripts.
- Added legacy cleanup to disable role access and soft-delete existing `module_settings` menu rows.
- Removed remaining static SuperAdmin sidebar link to Module Settings.
- Updated sidebar integration test expected SuperAdmin menu count.

### 2026-05-05 — Phase 1 Remediation Restart (Batch 3)
- Completed Stage 1.3 Result Summary runtime exception fix.
- Corrected report query ordering in repository to SQL-level ordering prior to projection, eliminating EF translation failure.
- Expanded report data/export endpoint role gates to `SuperAdmin,Admin,Faculty`.
- Validated SuperAdmin result summary endpoint response with non-empty records.

### Next Execution Target
- Continue **next phase planning/execution per roadmap directive**.

### 2026-05-08 — Phase 13 Global Search Complete (commit 00b7b64)
- **Stage 13.1 — Cross-Entity Search API:**
  - `ISearchRepository` (Application/Interfaces) + `SearchRepository` (Infrastructure): EF LINQ join queries against `StudentProfiles`, `Users`, `Courses`, `CourseOfferings`, `Semesters`, `Departments`, `Enrollments`. No new migration.
  - `ISearchService` + `SearchService`: role-scoped orchestration (SuperAdmin → all; Admin → assigned depts; Faculty → own dept + offerings; Student → enrolled offerings).
  - `SearchController`: `GET /api/v1/search?q={term}&limit={n}` — all authenticated roles; extracts callerId + role from JWT claims; validates q ≥ 2 chars; clamps limit 1–50.
  - `SearchDTOs`: `SearchRequest`, `SearchResultItem`, `SearchResponse` records.
- **Stage 13.2 — Portal Search Bar:**
  - Global search `<input>` in `_Layout.cshtml` header (visible all pages when connected).
  - Vanilla JS typeahead: debounced 300 ms fetch to `/Portal/SearchTypeahead`; renders top 5 + "See all" link.
  - Full results page `Search.cshtml`: Bootstrap nav-tabs per category with `_SearchResultsList.cshtml` partial.
  - `PortalController` — `Search()` + `SearchTypeahead()` actions.
- Validation: **0 build errors; 78/78 tests passed**

### 2026-05-07 — Phase 12 Academic Calendar System Complete (commit 6e89af1)
- **Stage 12.1 — Semester Timeline View:**
  - `AcademicCalendar` portal page (all roles): semester dropdown filter, days-remaining badges, link to manage page for Admin/SuperAdmin.
- **Stage 12.2 — Key Deadlines Management:**
  - New domain entity `AcademicDeadline` (`AuditableEntity`): `SemesterId`, `Title`, `Description`, `DeadlineDate`, `ReminderDaysBefore`, `IsActive`, `LastReminderSentAt`.
  - `IAcademicDeadlineRepository` + `AcademicDeadlineRepository` (EF Core, table `academic_deadlines`, soft-delete filter).
  - `IAcademicCalendarService` + `AcademicCalendarService` (CRUD + reminder dispatch).
  - EF migration `20260507_Phase12AcademicCalendar`.
  - `CalendarController` at `api/v1/calendar/deadlines` — reads open to all auth roles; writes restricted to Admin/SuperAdmin.
  - `AcademicDeadlines.cshtml` portal CRUD page with create/edit modals; `AcademicCalendar.cshtml` read-only calendar view.
  - `DeadlineReminderJob` (`BackgroundService`): runs daily, calls `DispatchPendingRemindersAsync`, dispatches `NotificationType.System` notifications to all active users.
  - `IEduApiClient` + `EduApiClient` extended with 5 calendar methods; `PortalController` extended with 5 new actions.
- Validation: **0 build errors; 78/78 tests passed**

---

## 1. Delivery Strategy

- Delivery model: Phased, test-first, modular monolith
- Sprint length: 2 weeks
- Initial roadmap horizon: **42 weeks (21 sprints)**
- Release train:
  - v1.0 core operations (Sprints 1–5)
  - v1.1 academic expansion (Sprints 6–12)
  - v1.2 lifecycle, licensing tool, dashboard, finance (Sprints 13–18)
  - v1.3 security, performance, email, mobile (Sprint 19)
  - v1.4 result calculation and GPA automation (Sprints 20–21)

---

## 2. Engineering Baseline

### 2.1 Proposed Solution Structure

- src/Web: ASP.NET Core MVC and Razor UI
- src/API: ASP.NET Core Web API
- src/Application: CQRS handlers, validation, business use cases
- src/Domain: Entities, value objects, domain services
- src/Infrastructure: EF Core, auth adapters, storage, integrations
- src/BackgroundJobs: license checks, notifications, cleanups
- tests/UnitTests
- tests/IntegrationTests
- tests/ContractTests

### 2.2 Core Technical Standards

- .NET 8 LTS
- EF Core with SQL Server
- ASP.NET Core Identity + JWT
- FluentValidation for request validation
- Serilog for structured logging
- OpenTelemetry for tracing and metrics
- xUnit + FluentAssertions + Testcontainers for testing

---

## 3. Phase Plan

## Phase 0: Project Foundation (Sprint 1)

### Objectives
- Create solution skeleton and architecture boundaries
- Configure CI/CD pipeline and environments
- Establish coding standards and quality gates

### Deliverables
- Working solution with build and test pipeline
- Environment configuration (dev, staging, production)
- Baseline health check and logging endpoints

### Exit Criteria
- CI pipeline green on pull requests
- Code coverage baseline established
- Deployment to staging succeeds

---

## Phase 1: Identity, Licensing, and Access Control (Sprints 2-3)

### Objectives
- Implement authentication and RBAC
- Implement license upload, validation, and degraded mode
- Implement module entitlement resolver

### Deliverables
- Login/logout and role policies (Student, Faculty, Admin, Super Admin)
- License verification service with signed payload checks
- Module activation API and admin UI

### Exit Criteria
- Unauthorized access blocked by policy tests
- Expired/invalid license enforces read-only operations
- Mandatory modules cannot be deactivated

---

## Phase 2: Academic Core and SIS (Sprints 4-5)

### Objectives
- Implement departments, programs, courses, semesters
- Implement student profile and enrollment workflow
- Preserve permanent academic history

### Deliverables
- Department and program management screens
- Student signup using registration whitelist
- Semester records with immutable history behavior

### Exit Criteria
- Student record creation is fully auditable
- Academic history cannot be deleted via UI or API
- Faculty can only access assigned department data

---

## Phase 3: Assignments and Results (Sprints 6-7)

### Objectives
- Implement assignment lifecycle and submissions
- Implement grading workflows and result publication
- Implement transcript export

### Deliverables
- Assignment creation and submission APIs/UI
- Faculty grading and feedback workflows
- Result publication and transcript export logs

### Exit Criteria
- Assignment submissions enforce one-per-student rule
- Result publication is role-restricted and auditable
- Transcript export appears in audit/report logs

---

## Phase 4: Notifications and Attendance (Sprints 8-9)

### Objectives
- Implement notification engine and recipient tracking
- Implement attendance management and alerts

### Deliverables
- Notification compose, dispatch, read-state tracking
- Attendance daily capture and low-attendance checks
- Scheduled job for attendance alerts

### Exit Criteria
- Notifications tracked per user with read state
- Duplicate attendance entries prevented per day
- Alert job execution visible in operational logs

---

## Phase 5: Quizzes and FYP (Sprints 10-11)

### Objectives
- Implement quiz authoring and attempts
- Implement FYP project and meeting scheduling

### Deliverables
- Quiz question bank, attempts, and scoring
- FYP meetings with panel members and room scheduling
- Student dashboard views for quizzes and FYP events

### Exit Criteria
- Quiz attempts honor configured attempt limits
- FYP meetings generate notifications for stakeholders
- Faculty and department access boundaries verified

---

## Phase 6: AI, Analytics, and Hardening (Sprint 12)

### Objectives
- Integrate AI chatbot with role-aware context
- Implement initial reporting dashboards
- Complete performance and security hardening

### Deliverables
- AI chat endpoint with module/license guardrails
- Core analytics (performance, attendance, results)
- Security checklist completion and load test report

### Exit Criteria
- AI access obeys module and license policies
- p95 response targets met for core endpoints
- UAT sign-off for v1.x release candidate

---

## Phase 7: Tabsan-Lic — License Creation Tool (Sprints 13–14)

### Objectives
- Build a standalone .NET application for generating encrypted license files
- Implement one-time-use VerificationKey mechanism
- Wire EduSphere license import to consume Tabsan-Lic’s `.tablic` files

### Deliverables
- `Tabsan-Lic` standalone .NET app with VerificationKey generation UI
- AES-256 encrypted + RSA-2048 signed `.tablic` license file output
- VerificationKey expiry options: 1 year / 2 years / 3 years / Permanent
- EduSphere import endpoint: signature verify → decrypt → apply → mark key consumed
- License expiry background job: notification to Admin/Super Admin 5 days before expiry

### Exit Criteria
- Re-importing a used VerificationKey is rejected with error
- Tampered `.tablic` file fails signature check and is rejected
- License status table updates correctly on import
- Expiry notification fires on schedule in tests

---

## Phase 8: Student Lifecycle & Academic Operations (Sprints 15–16)

### Objectives
- Implement end-to-end student lifecycle: graduation, semester progression, dropout, transfer
- Implement finance and payment receipt workflow
- Implement CSV-based registration import
- Implement teacher modification request with admin approval

### Deliverables
- "Graduated Students" menu: per-department checkbox list; graduated → read-only dashboard
- "Semester Management" menu: per-student subject selection; promotion or failure logic
- Student inactive status (dropout/leave): blocks login, preserves data
- Department/program transfer admin action
- Admin change request workflow for non-self-editable fields
- Teacher attendance/result modification request with reason + admin approval + audit trail
- Finance role: payment receipts CRUD; student payment submission; Finance confirmation
- Read-only mode for students with unpaid fees
- Online payment gateway toggle (Super Admin, Module Settings)
- CSV import for registration whitelist with duplicate detection and error report
- Account lockout after 5 consecutive failures; Admin/Super Admin unlock + password reset

### Exit Criteria
- Graduated student cannot create/edit/submit anything on their dashboard
- Promoted students’ active semester number updates automatically
- Student with unpaid fees cannot write to any resource until Finance marks Paid
- CSV import rejects duplicates and produces downloadable error report
- Locked account cannot log in; unlocked account can log in immediately

---

## Phase 9: Dashboard, Navigation & System Settings (Sprints 17–18)

### Objectives
- Implement role-based sidebar navigation with dynamic menus
- Implement per-user theme persistence
- Implement Departments admin menu (timetable included)
- Implement full System Settings menu (License, Theme, Reports, Modules)

### Deliverables
- Collapsible sidebar: menus and sub-menus per role and active modules
- Per-user theme stored in user profile; theme picker with preview in Settings
- Departments admin CRUD: departments, degree programs, semesters, subjects, timetables
- Timetable PDF/Excel download for all department users
- Settings → License Update: upload `.tablic`; status table (Status, Expiry, Date Updated, Remaining Days)
- Settings → Report Settings: SR#, Report Name, Purpose, Roles (multi-select), active/inactive
- Settings → Module Settings: SR#, Module Name, Purpose, Roles (multi-select), Status dropdown
- License expiry notification (5 days prior) wired to background job

### Exit Criteria
- Sidebar shows only menus for active modules and user’s role
- Two users with different themes see their own theme independently
- Deactivated module hidden from all dashboards except Super Admin
- Deactivating a module does not delete any data
- Timetable PDF and Excel download verified in integration tests

---

## Phase 10: Security, Performance & Email Infrastructure (Sprint 19)

### Objectives
- Complete OWASP Top 10 hardening
- Add database views and stored procedures for performance
- Integrate free/open-source email API
- Deliver mobile-responsive, WCAG 2.1 AA accessible UI

### Deliverables
- OWASP Top 10 checklist: injection, broken auth, XSS, IDOR, misconfiguration remediation
- Security headers: HSTS, CSP, X-Frame-Options, X-Content-Type-Options
- Rate limiting on auth and sensitive endpoints
- Password policy: Argon2id hashing, complexity rules, lockout, no last-5 reuse
- SQL Views for student dashboard summary, department reports, attendance summary
- Stored Procedures for semester promotion batch, graduation batch, payment status update
- Covering indexes on all FK columns and frequently filtered columns
- `IEmailSender` abstraction wired to MailKit SMTP / SendGrid free tier (configurable)
- Email notifications: results, assignment deadlines, low attendance, license expiry, password reset, account unlock
- All outbound email attempts logged with status
- Bootstrap 5 responsive layout tested at 360 px, 768 px, 1280 px
- Lighthouse score ≥ 90 on core pages
- Penetration test report signed off; zero critical/high CVEs

### Exit Criteria
- OWASP Top 10 checklist fully signed off
- p95 < 200 ms for core dashboards under simulated load
- Email delivery verified in staging environment
- Lighthouse scores ≥ 90 on core pages
- No critical or high CVEs in dependency scan

---

## Phase 11: Result Calculation & GPA Automation (Sprints 20-21)

### Objectives
- Add a new sidebar menu named `Result Calculation` for admins
- Allow admins to configure GPA-to-score thresholds and subject component weightages
- Automatically calculate subject GPA, semester GPA, and cumulative CGPA from entered marks

### Deliverables
- Result Calculation screen with two sections:
  - Section 1: repeatable `GPA` and `Score` rows with `Add Row` and `Save`
  - Section 2: repeatable assessment component rows for items such as Quizzes, Midterms, Finals and their score weights
- Database tables and APIs for GPA mappings and result component configuration
- Validation rule that component score weights must total `100`
- Result-entry workflow updates so faculty mark entry triggers automatic subject total calculation
- Semester completion logic that automatically computes SGPA when all subject marks are present
- CGPA aggregation logic that automatically updates cumulative GPA after every semester completion or approved result modification
- Audit trail for recalculation operations and admin configuration changes

### Exit Criteria
- Admin can add, edit, and save multiple GPA/Score rows without data loss
- Admin can configure component weightages and cannot save an active rule set unless the total is `100`
- Entering quiz, midterm, or final marks automatically updates subject totals and subject GPA
- When all subjects for a semester are marked, the student’s SGPA and CGPA are recalculated and stored automatically
- Integration tests cover initial calculation, recalculation after mark edits, and incomplete-mark scenarios

---

## 4. Cross-Cutting Workstreams

## 4.1 Data and Migration Workstream
- Apply incremental EF Core migrations per phase
- Seed base roles/modules/themes in non-production and production-safe scripts
- Backup and restore runbook validated before each release

## 4.2 Security and Compliance Workstream
- Threat modeling at phase boundaries
- SAST/dependency scanning on each PR
- Audit log completeness checks for privileged operations

## 4.3 Quality Engineering Workstream
- Unit tests for domain and application layers
- Integration tests for API and data persistence
- Contract tests for client-server compatibility
- Regression suite before each milestone release

---

## 5. Definition of Done

A feature is complete only when:

- Functional requirements are implemented and demoed
- Unit and integration tests pass in CI
- Logging, metrics, and audit events are included
- Authorization and module-activation rules are enforced
- API documentation is updated
- No critical or high vulnerabilities remain open

---

## 6. Risk Register and Mitigations

- Licensing complexity risk:
  - Mitigation: implement and test license service early in Phase 1; Tabsan-Lic built as Phase 7
- Access-control regression risk:
  - Mitigation: centralized policy tests and authorization integration tests
- Performance risk under peak enrollment windows:
  - Mitigation: load testing from Phase 3 onward; SQL Views and Stored Procedures in Phase 10
- Scope creep risk:
  - Mitigation: v1.0 scope locked; Phases 7–10 routed to v1.2/v1.3 release train
- Finance/payment integration risk:
  - Mitigation: finance workflow built without gateway first; gateway added as a toggled add-on module
- Email delivery risk:
  - Mitigation: `IEmailSender` abstraction allows provider swap without code changes; SMTP fallback always available
- Student lifecycle complexity risk:
  - Mitigation: graduation, semester promotion, and dropout handled as discrete admin operations with individual audit trails
- Security hardening risk:
  - Mitigation: OWASP checklist tracked from Phase 1; dedicated Sprint 19 for final hardening and pen test

---

## 7. Immediate Next Actions (Week 1)

- Finalize solution structure and repository conventions
- Create initial ASP.NET solution and projects
- Configure CI pipeline with build, test, lint, and security scan
- Implement baseline auth model and role seed
- Draft initial EF Core migration for identity and department core

---

## 8. Execution Status Update (Kickoff)

### Completed

- .NET 8 modular solution scaffold created with `src/` and `tests/` layout
- Projects created: Web, API, Application, Domain, Infrastructure, BackgroundJobs
- Test projects created: UnitTests, IntegrationTests, ContractTests
- Solution files created: `Tabsan.EduSphere.sln` and `Tabsan.EduSphere.slnx`
- Project references wired according to planned architecture direction
- Baseline packages added (FluentValidation, EF Core SQL Server, Serilog, OpenTelemetry, testing stack)
- GitHub Actions CI workflow created at `.github/workflows/dotnet-ci.yml`
- Local validation completed: restore, build, and tests passed

### Next Immediate Implementation Tasks

- Add architecture decision records (ADRs) for auth, data, and module enforcement
- Implement `ApplicationDbContext` and first EF Core migration
- Implement role seed and Super Admin bootstrap flow
- Add first policy-based authorization matrix tests

---

## Phase 12: Reporting & Document Generation (Sprints 22-23)

### Objectives
- Build a role-gated Report Center portal backed by `ReportDefinition` / `ReportRoleAssignment` records from Phase 9
- Provide five standard reports accessible via web portal and REST API
- Support Excel (`.xlsx`) export on attendance, result, and GPA reports
- Enforce role-based access to each report using the existing report role assignment system

### Deliverables
- **Report Catalog endpoint** — `GET /api/v1/reports` returns the subset of active reports the caller's role is permitted to view
- **Attendance Summary Report** — per-student per-offering session counts and attendance percentage, filterable by semester / department / offering / student; Excel export
- **Result Summary Report** — all published results with marks and percentage, filterable by semester / department / offering / student; Excel export
- **GPA & CGPA Report** — per-student academic standing (Current Semester GPA + CGPA) filterable by department / program; Excel export; average CGPA summary row
- **Enrollment Summary Report** — per course-offering seat utilisation (enrolled vs max capacity); filterable by semester / department
- **Semester Results Report** — full published result set for a selected semester with optional department filter
- **Report Center web page** — landing page listing all reports available to the user's role as cards with filter and view buttons
- **Four report detail pages** — `ReportAttendance`, `ReportResults`, `ReportGpa`, `ReportEnrollment` — each with filter form, sortable data table, and Export button
- **Sidebar menu item** — `reports` entry visible to Admin, Faculty, and Student roles
- **Seed data** — five `ReportDefinition` rows seeded at startup with default role assignments

### Technical Highlights
- `ReportKeys` constants class prevents magic string duplication across backend and seeder
- `ReportRepository` uses EF Core 8 queries with explicit joins (no raw SQL) for portability and testability
- `ReportService` uses `ClosedXML` for in-memory Excel workbook generation — no temporary files on disk
- No new EF migration required — `report_definitions` and `report_role_assignments` tables were created in Phase 9

### Exit Criteria
- All five report endpoints return 200 for authenticated admin user
- All three export endpoints return `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Non-authorised role receives 403 on a report they are not assigned to
- `Build succeeded. 0 Error(s)` on `dotnet build`

---

### 2026-05-08 — Phase 20 Learning Management System Complete (commit `ecf4d91`)

**Stage 20.1 — Course Content Modules:**
- `CourseContentModule` + `ContentVideo` domain entities (`Domain/Lms/`).
- `ILmsRepository` + `LmsRepository`; `ILmsService` + `LmsService`.
- `LmsController` at `api/v1/lms` — full CRUD + publish/unpublish for modules; add/delete for videos.
- Students auto-scoped to `publishedOnly=true`; faculty see all modules.
- Portal views: `CourseLms.cshtml` (student accordion), `LmsManage.cshtml` (faculty management panel).

**Stage 20.2 — EF Configuration:**
- `LmsConfigurations.cs` — 5 EF entity configurations with table names, FK cascade rules, field lengths, and `HasQueryFilter(!IsDeleted)` for all LMS entities.
- EF migration `Phase20_LMS` — creates `course_content_modules`, `content_videos`, `discussion_threads`, `discussion_replies`, `course_announcements`.

**Stage 20.3 — Discussion Forums:**
- `DiscussionThread` + `DiscussionReply` domain entities.
- `IDiscussionRepository` + `DiscussionRepository`; `IDiscussionService` + `DiscussionService`.
- Author names resolved at service layer via `IUserRepository.GetByIdAsync`.
- `DiscussionController` at `api/v1/discussion`; JWT-enforced `AuthorId` on all write endpoints.
- Portal views: `Discussion.cshtml` + `DiscussionThread.cshtml`.

**Stage 20.4 — Course Announcements:**
- `CourseAnnouncement` domain entity; optional `OfferingId` FK with SET NULL on cascade.
- `IAnnouncementRepository` + `AnnouncementRepository`; `IAnnouncementService` + `AnnouncementService`.
- Fan-out on create: queries enrolled students → dispatches `NotificationType.Announcement` notification batch.
- `AnnouncementController` at `api/v1/announcement`; JWT-enforced `AuthorId`.
- Portal view: `Announcements.cshtml`.

**Cross-cutting:**
- `ApplicationDbContext` updated with 5 new `DbSet<T>` properties.
- `Program.cs` Phase 20 DI block: 6 scoped registrations.
- `EduApiClient` interface + models + 21 new method implementations.
- `PortalController` 19 new actions (LMS + Discussion + Announcements).
- `PortalViewModels.cs` 10 new view models.
- `_Layout.cshtml` sidebar entries: `lms_manage`, `discussion`, `announcements` (group: Academic Related).

**Validation:** 0 build errors · 7/7 unit tests · migration applied · commit `ecf4d91` pushed

---

### 2026-05-08 — Phase 21 Study Planner Complete

**Changes:**
- `Domain/StudyPlanner/StudyPlan.cs` — aggregate root; `StudyPlanStatus` enum; endorsement workflow methods.
- `Domain/StudyPlanner/StudyPlanCourse.cs` — child entity (physical delete).
- `AcademicProgram.MaxCreditLoadPerSemester` property + `SetMaxCreditLoad()` method.
- `Domain/Interfaces/IStudyPlanRepository.cs` — 7 methods.
- `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs` — 4 requests + 4 response records.
- `Application/Interfaces/IStudyPlanService.cs` + `Application/StudyPlanner/StudyPlanService.cs` — CRUD + prerequisite/credit-load validation + recommendation engine.
- `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs` — `StudyPlanConfiguration` + `StudyPlanCourseConfiguration`.
- `Infrastructure/Repositories/StudyPlanRepository.cs`.
- `Infrastructure/Persistence/ApplicationDbContext.cs` — `StudyPlans` + `StudyPlanCourses` DbSets.
- `API/Controllers/StudyPlanController.cs` — 9 endpoints.
- `API/Program.cs` Phase 21 DI block: 2 scoped registrations.
- `Web/Services/EduApiClient.cs` — 9 new methods + 4 API models.
- `Web/Controllers/PortalController.cs` — 9 new actions + `MapStudyPlanItem` helper.
- `Web/Models/Portal/PortalViewModels.cs` — 6 new view models.
- `_Layout.cshtml` sidebar: `study_plan` → `(Portal, StudyPlan)` (group: Student Related).
- Views: `StudyPlan.cshtml`, `StudyPlanDetail.cshtml`, `StudyPlanRecommendations.cshtml`.
- Migration `Phase21_StudyPlanner` applied.

**Validation:** 0 build errors · 7/7 unit tests · migration applied

---

### 2026-05-08 — Phase 22 External Integrations Complete

**Changes:**
- `Domain/Settings/AccreditationTemplate.cs` — `AccreditationTemplate` entity + `AccreditationTemplateConfiguration` EF config.
- `Application/DTOs/External/LibraryDTOs.cs` — `SaveLibraryConfigCommand`, `LibraryConfigDto`, `LibraryLoanDto`.
- `Application/DTOs/External/AccreditationDTOs.cs` — `CreateAccreditationTemplateCommand`, `UpdateAccreditationTemplateCommand`, `AccreditationTemplateDto`, `GeneratedReport`.
- `Application/Interfaces/ILibraryService.cs` + `Application/Services/LibraryService.cs` — `GetConfigAsync`, `SaveConfigAsync`, `GetLoansAsync`.
- `Application/Interfaces/IAccreditationService.cs` + `Application/Services/AccreditationService.cs` — full CRUD + `GenerateAsync` (field mapping materialisation + CSV/PDF export + audit log).
- `Domain/Interfaces/IAccreditationRepository.cs` + `Infrastructure/Repositories/AccreditationRepository.cs`.
- `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`.
- `Infrastructure/Persistence/ApplicationDbContext.cs` — `AccreditationTemplates` DbSet added.
- `API/Controllers/LibraryController.cs` — 4 endpoints (`GET/PUT config`, `GET loans`, `GET loans/{id}`).
- `API/Controllers/AccreditationController.cs` — 6 endpoints (CRUD + generate/stream).
- `API/Program.cs` Phase 22 DI block: `LibraryService` (scoped), `AccreditationService` (scoped), `AccreditationRepository` (scoped).
- `Web/Services/EduApiClient.cs` — 11 new methods + API models.
- `Web/Controllers/PortalController.cs` — 9 new actions.
- `Web/Views/Portal/LibraryConfig.cshtml` — library URL + token config view.
- `Web/Views/Portal/AccreditationTemplates.cshtml` — template list with CRUD modals + generate buttons.
- `_Layout.cshtml` sidebar entries: `library_config`, `accreditation` (Settings group).
- EF Migration `Phase22_ExternalIntegrations` — adds `accreditation_templates` table.
- `Scripts/1-MinimalSeed.sql` — adds sidebar seed entries for `library_config` (sort 31, SuperAdmin) and `accreditation` (sort 32, Admin+SuperAdmin).

**Validation:** 0 build errors · migration `Phase22_ExternalIntegrations` applied · commit `dddee69` pushed

---

### 2026-05-09 — Phase 23 Core Policy Foundation Complete

**Changes:**
- `Domain/Enums/InstitutionType.cs` — `InstitutionType` enum (`University=0`, `School=1`, `College=2`).
- `Application/Interfaces/IInstitutionPolicyService.cs` — `InstitutionPolicySnapshot` sealed record + `SaveInstitutionPolicyCommand` + `IInstitutionPolicyService` interface.
- `Application/Services/InstitutionPolicyService.cs` — implementation with `IMemoryCache` (10-min TTL) + `ISettingsRepository` persistence.
- `Tabsan.EduSphere.Application.csproj` — added `Microsoft.Extensions.Caching.Memory 8.0.1`.
- `API/Middleware/InstitutionContextMiddleware.cs` — resolves snapshot per-request; `GetInstitutionPolicy()` extension method with `Default` fallback.
- `API/Controllers/InstitutionPolicyController.cs` — `GET api/v1/institution-policy` (all authenticated) + `PUT api/v1/institution-policy` (SuperAdmin only).
- `API/Program.cs` — `IInstitutionPolicyService` scoped DI; `InstitutionContextMiddleware` after `UseAuthorization`.
- `Web/Services/EduApiClient.cs` — `GetInstitutionPolicyAsync`, `SaveInstitutionPolicyAsync`.
- `Web/Controllers/PortalController.cs` — `InstitutionPolicy` GET/POST actions.
- `Web/Views/Portal/InstitutionPolicy.cshtml` — three-flag toggle config form.
- `_Layout.cshtml` sidebar entry `institution_policy` (Settings group).
- `Scripts/1-MinimalSeed.sql` — sidebar seed `institution_policy` (sort 33, SuperAdmin).
- `tests/Tabsan.EduSphere.UnitTests/InstitutionPolicyTests.cs` — 13 new tests (27 total).

**Validation:** 0 build errors · 27/27 unit tests · no migration needed · commit `28cac36` pushed

---

### 2026-05-09 — Phase 24 Dynamic Module and UI Composition Complete

**Changes:**
- `Domain/Modules/ModuleDescriptor.cs` — `ModuleDescriptor` sealed record: `Key`, `RequiredRoles[]`, `AllowedTypes[]?`, `IsLicenseGated`; `RoleMatches()` + `TypeMatches()` methods.
- `Application/Modules/ModuleRegistry.cs` — static catalogue of all 14 module descriptors.
- `Application/Interfaces/IModuleRegistryService.cs` — `ModuleVisibilityResult` record + interface with `GetVisibleModulesAsync` + `IsAccessibleAsync`.
- `Application/Modules/ModuleRegistryService.cs` — combines registry + `IModuleEntitlementResolver` + institution policy.
- `Application/Interfaces/ILabelService.cs` — `AcademicVocabulary` sealed record + `ILabelService.GetVocabulary(policy)`.
- `Application/Services/LabelService.cs` — stateless singleton (University/School/College vocab branches).
- `Application/Interfaces/IDashboardCompositionService.cs` — `WidgetDescriptor` sealed record + `IDashboardCompositionService.GetWidgets(role, policy)`.
- `Application/Services/DashboardCompositionService.cs` — 10-widget catalogue, role + institution-type filtered.
- `API/Controllers/ModuleRegistryController.cs` — `GET api/v1/module-registry/visible`.
- `API/Controllers/LabelController.cs` — `GET api/v1/labels`.
- `API/Controllers/DashboardCompositionController.cs` — `GET api/v1/dashboard/composition`.
- `API/Program.cs` Phase 24 DI block: `ModuleRegistryService` (scoped), `LabelService` (singleton), `DashboardCompositionService` (singleton).
- `Web/Services/EduApiClient.cs` — `GetVisibleModulesAsync`, `GetVocabularyAsync`, `GetDashboardWidgetsAsync` + 3 API response models.
- `Web/Controllers/PortalController.cs` — `ModuleComposition` GET action (parallel `Task.WhenAll`).
- `Web/Models/Portal/PortalViewModels.cs` — `DashboardCompositionModel`, `ModuleVisibilityItem`, `WidgetItem`.
- `Web/Views/Portal/ModuleComposition.cshtml` — vocabulary tiles, widget cards, module registry table.
- `_Layout.cshtml` sidebar entry `module_composition` (Settings group).
- `Scripts/1-MinimalSeed.sql` — sidebar seed `module_composition` (sort 34, SuperAdmin).
- `tests/Tabsan.EduSphere.UnitTests/Phase24Tests.cs` — 17 new tests (44 total).

**Validation:** 0 build errors · 44/44 unit tests · no migration needed · commit `391ac45` pushed

---

### 2026-05-09 — Phase 25 Academic Engine Unification Complete

**Stage 25.1 — Result Calculation Strategy Pattern:**
- `Application/Interfaces/IResultCalculationStrategy.cs` — `IResultCalculationStrategy` interface + value types: `ComponentMark`, `ResultSummary`, `GpaScaleRuleEntry`, `GradeBandEntry`.
- `Application/Academic/GpaResultStrategy.cs` — University: weighted % → GPA 0.0–4.0 scale lookup. `AppliesTo = InstitutionType.University`.
- `Application/Academic/PercentageResultStrategy.cs` — School/College: weighted % → grade band label (custom JSON or built-in A+/A/B+/B/C/D/F). Throws if constructed for University.
- `Application/Interfaces/IResultStrategyResolver.cs` — `Resolve(InstitutionType)` contract.
- `Application/Academic/ResultStrategyResolver.cs` — singleton resolver: University→GpaResultStrategy, School/College→PercentageResultStrategy. Registered as Singleton.

**Stage 25.2 — Institution Grading Profiles:**
- `Domain/Academic/InstitutionGradingProfile.cs` — entity with `InstitutionType`, `PassThreshold` (validated), `GradeRangesJson`, `IsActive`. `Update()` method.
- `Domain/Interfaces/IInstitutionGradingProfileRepository.cs` — `GetAllAsync`, `GetByTypeAsync`, `GetByIdAsync`, `AddAsync`, `Update`, `SaveChangesAsync`.
- `Application/Interfaces/IInstitutionGradingService.cs` — `GetAllAsync`, `GetByTypeAsync`, `UpsertAsync`.
- `Application/Academic/InstitutionGradingService.cs` — upsert (create if missing, update if exists). Maps to `InstitutionGradingProfileDto`.
- `Application/DTOs/Academic/InstitutionGradingDtos.cs` — `InstitutionGradingProfileDto`, `SaveInstitutionGradingProfileRequest`.
- `Infrastructure/Repositories/InstitutionGradingProfileRepository.cs` — EF Core implementation.
- `Infrastructure/Persistence/Configurations/InstitutionGradingProfileConfiguration.cs` — table `institution_grading_profiles`, `decimal(5,2)` for threshold, unique index on `InstitutionType`.
- `Infrastructure/Persistence/ApplicationDbContext.cs` — `DbSet<InstitutionGradingProfile> InstitutionGradingProfiles`.
- `Infrastructure/Migrations/20260508152906_Phase25_AcademicEngineUnification.cs` — creates `institution_grading_profiles` table.
- `API/Controllers/InstitutionGradingProfileController.cs` — `GET /` (Admin+), `GET /{type}` (Admin+), `PUT /{type}` (SuperAdmin).

**Stage 25.3 — Progression / Promotion Logic:**
- `Application/Interfaces/IProgressionService.cs` — `EvaluateAsync`, `PromoteAsync`.
- `Application/Academic/ProgressionService.cs` — institution-aware evaluation; `PromoteAsync` calls `student.AdvanceSemester()`. Default thresholds: 2.0 (University), 40 (School/College).
- `Application/DTOs/Academic/ProgressionDtos.cs` — `ProgressionDecision`, `ProgressionEvaluationRequest`.
- `API/Controllers/ProgressionController.cs` — `POST /evaluate` (Admin+), `POST /promote` (Admin+), `GET /me/{type}` (Student+).
- `API/Program.cs` Phase 25 DI: `IResultStrategyResolver` (singleton), `IInstitutionGradingProfileRepository` (scoped), `IInstitutionGradingService` (scoped), `IProgressionService` (scoped).
- `tests/Tabsan.EduSphere.UnitTests/Phase25Tests.cs` — 29 new tests (144 total): `GpaResultStrategyTests`, `PercentageResultStrategyTests`, `ResultStrategyResolverTests`, `InstitutionGradingProfileTests`, `ProgressionServiceTests`.

**Validation:** 0 build errors · 144/144 unit tests · migration `20260508152906_Phase25_AcademicEngineUnification` · commit `d2aabd3` pushed

---

### 2026-05-09 — Phase 26 School and College Functional Expansion Complete

**Stage 26.1 — School Streams and Subject Mapping:**
- `Domain/Academic/SchoolStream.cs` and `Domain/Academic/StudentStreamAssignment.cs` created.
- `Domain/Interfaces/ISchoolStreamRepository.cs` + `Infrastructure/Repositories/Phase26Repositories.cs` (`SchoolStreamRepository`).
- `Application/Interfaces/ISchoolStreamService.cs` + `Application/Academic/SchoolStreamService.cs`.
- `API/Controllers/SchoolStreamController.cs` endpoints for stream listing/upsert and student assignment.
- EF configs: `SchoolStreamConfiguration.cs`, `StudentStreamAssignmentConfiguration.cs`.

**Stage 26.2 — Report Cards and Bulk Promotion:**
- `Domain/Academic/StudentReportCard.cs`, `BulkPromotionBatch.cs`, `BulkPromotionEntry.cs`.
- Enums: `BulkPromotionStatus.cs`, `EntryDecision.cs`.
- Repository interfaces: `IReportCardRepository.cs`, `IBulkPromotionRepository.cs`.
- Services: `IReportCardService`/`ReportCardService`, `IBulkPromotionService`/`BulkPromotionService`.
- API: `ReportCardController.cs`, `BulkPromotionController.cs`.
- EF configs: `StudentReportCardConfiguration.cs`, `BulkPromotionBatchConfiguration.cs`, `BulkPromotionEntryConfiguration.cs`.

**Stage 26.3 — Parent-Facing Read Model:**
- `Domain/Academic/ParentStudentLink.cs` + `Domain/Interfaces/IParentStudentLinkRepository.cs`.
- `Application/Interfaces/IParentPortalService.cs` + `Application/Academic/ParentPortalService.cs`.
- `API/Controllers/ParentPortalController.cs`.
- EF config: `ParentStudentLinkConfiguration.cs`.

**Cross-Cutting / Wiring:**
- `Application/DTOs/Academic/Phase26Dtos.cs` added for stream/report card/bulk promotion/parent read DTOs.
- `Infrastructure/Persistence/ApplicationDbContext.cs` adds 6 new DbSets.
- `API/Program.cs` Phase 26 DI registrations for repositories and services.
- Migration: `Infrastructure/Migrations/20260509044437_Phase26_SchoolCollegeExpansion.cs`.

**Validation:**
- `dotnet build Tabsan.EduSphere.sln` — 0 build errors.
- Unit + integration/contract suite: **152/152 tests passed**.
- Migration visible in list: `20260509044437_Phase26_SchoolCollegeExpansion`.
- Commit: `4c0904c` pushed.

---

### 2026-05-09 — Phase 27 Stage 27.1 Portal Capability Matrix Complete

**Changes:**
- Added `IPortalCapabilityMatrixService` + `PortalCapabilityMatrixService`.
- Added `PortalCapabilitiesController` endpoint: `GET api/v1/portal-capabilities/matrix`.
- Added web support: `PortalController.PortalCapabilityMatrix`, portal page model, and `PortalCapabilityMatrix.cshtml`.
- Added unit tests in `Phase27Tests.cs`.

**Validation:**
- Unit tests passed.
- Solution build successful.
- Commit: `fd3b137` pushed.

---

### 2026-05-09 — Phase 27 Stage 27.2 Authentication and Security UX Complete

**Changes:**
- Added `AuthSecurityOptions` config contract (`AuthSecurity` section in appsettings).
- Extended auth DTO/service/controller flows for MFA toggle, SSO-ready security profile, and session-risk controls.
- Added API endpoint `GET api/v1/auth/security-profile`.
- Updated web login flow/UI for MFA field, SSO/risk hints, and richer auth error handling.
- Added unit tests in `Phase27Stage2Tests.cs`.

**Validation:**
- Unit tests passed.
- Solution build successful.
- Commit: `20dba8d` pushed.

---

### 2026-05-09 — Phase 27 Stage 27.3 Support and Communication Integration Complete

**Changes:**
- Added provider abstraction contracts:
  - `ISupportTicketingProvider`
  - `IAnnouncementBroadcastProvider`
  - `IEmailDeliveryProvider`
- Added default adapters in Infrastructure:
  - `InAppSupportTicketingProvider`
  - `InAppAnnouncementBroadcastProvider`
  - `SmtpEmailDeliveryProvider`
- Refactored `HelpdeskService`, `AnnouncementService`, and `LicenseExpiryWarningJob` to use provider contracts.
- Added `ICommunicationIntegrationService` + `CommunicationIntegrationService` and API endpoint `GET api/v1/communication-integrations/profile`.
- Added unit test `Phase27Stage3Tests.cs`.

**Validation:**
- `dotnet test` (unit project): 89/89 passed.
- `dotnet build Tabsan.EduSphere.sln`: success.
- Commit: `56cf1dd` pushed.

---
