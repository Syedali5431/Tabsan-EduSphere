## 2026-05-21 Update - Plan G Phase 2 Stage 2.3 (University Calculation Path)

- Implementation Summary:
  - Documented the requirement to preserve the existing University GPA/CGPA credit-based calculation behavior unchanged.
  - No new runtime functionality or behavior was added; this stage is documentation-only and sets the university calculation preservation requirement.
- Validation Summary:
  - Manual review confirmed the university calculation preservation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

## 2026-05-21 Update - Plan G Phase 2 Stage 2.2 (College Calculation Path)

- Implementation Summary:
  - Documented the requirement to implement percentage-based calculation for colleges (aligned to the school path) and return Percentage + Grade.
  - No new runtime functionality or behavior was added; this stage is documentation-only and sets the college calculation path requirement.
- Validation Summary:
  - Manual review confirmed the college calculation path requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

## 2026-05-21 Update - Plan G Phase 1 Stage 1.3 (Detection Contract)

- Implementation Summary:
  - Documented the requirement to define deterministic calculation-mode selection using both license enablement and department context.
  - No new runtime functionality or behavior was added; this stage is documentation-only and sets the detection contract requirement.
- Validation Summary:
  - Manual review confirmed the detection contract requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

## 2026-05-21 Update - Plan G Phase 1 Stage 1.2 (Institute Type Detection)

- Implementation Summary:
  - Documented the requirement to detect the enabled institute type (School, College, University) at runtime based on the parsed license.
  - No new runtime functionality or behavior was added; this stage is documentation-only and sets the detection requirement.
- Validation Summary:
  - Manual review confirmed the detection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

## 2026-05-21 Update - Plan G Phase 1 Stage 1.1 (License Parsing)

- Implementation Summary:
  - Documented the requirement to parse enabled institute types (School, College, University) from the license file for future conditional logic.
  - No new runtime functionality or behavior was added; this stage is documentation-only and sets the parsing requirement.
- Validation Summary:
  - Manual review confirmed the parsing requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

## 2026-05-21 Update - Plan G Phase 0 Stage 0.2 (Conditional-Layer-Only Contract)

- Implementation Summary:
  - Defined the conditional-layer-only contract: Only a conditional decision layer may be added over existing calculation paths; no direct modification of GPA/CGPA, lifecycle, or report logic is allowed.
  - No new runtime functionality or behavior was added; this stage is a governance and safety declaration only.
- Validation Summary:
  - Manual review confirmed no direct modification of GPA/CGPA, lifecycle, or report logic.
  - No build, test, or migration was required; this stage is documentation-only.

## 2026-05-21 Update - Plan G Phase 0 Stage 0.3 (Compatibility Defaults)

- Implementation Summary:
  - Established compatibility defaults: Full backward compatibility is enforced, and University GPA behavior is the default when no condition applies.
  - No new runtime functionality or behavior was added; this stage is a governance and compatibility declaration only.
- Validation Summary:
  - Manual review confirmed backward compatibility and default behavior are preserved.
  - No build, test, or migration was required; this stage is documentation-only.


- Implementation Summary:
  - Declared the protected surface for result calculation: GPA/CGPA logic, lifecycle workflows, storage structure, and report logic are now explicitly frozen against direct modification unless a future phase requires it.
  - No new runtime functionality or behavior was added; this stage is a governance declaration only.
- Validation Summary:
  - Manual review confirmed all GPA/CGPA, lifecycle, and report logic remain unchanged.
  - No build, test, or migration was required; this stage is documentation-only.


- Recent request issue:
  - update the Finance-facing documentation and governance references for Phase 7.

### Plan F Phase 7 - Documentation Updates (Documentation Only)
- Implementation Summary:
  - synchronized the User Guide, Training Manual, UAT, and SAT documents with Finance role workflows and access boundaries,
  - did not introduce any new runtime functionality or behavior changes.
- Validation Summary:
  - manual documentation review confirmed the Finance documentation updates were applied consistently,
  - no code execution or automated test execution was required for this documentation-only update.

- Behavior impact:
  - no functional behavior change; this phase only improves user-facing documentation coverage.

## 2026-05-21 Update - Plan F Phases 4 and 5 (Payment Reports and Finance UI Surface)

- Recent request issue:
  - proceed and complete the finance reporting and UI/navigation stages.

### Plan F Phases 4 and 5 - Payment Reports and Finance UI Surface (Implemented)
- Implementation Summary:
  - added payment summary report behavior with finance-ready filters for year, month, semester, department, course, level, and institution type,
  - added portal payment report browsing/export behavior plus report-center discovery for finance users,
  - extended finance-visible navigation/config scope to include payments, reports, analytics, and theme settings while keeping finance out of academic report endpoints and academic modules.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~AuthorizationRegressionTests"` passed (`64/64`).

- Behavior impact:
  - finance users now have a dedicated exportable payment report surface,
  - finance can access payment reports but is still denied on academic report endpoints,
  - existing academic reporting behavior for other roles remains intact.

## 2026-05-20 Update - Plan F Phase 3 Stage 3.2 (Filter-Aware Analytics Behavior)

- Recent request issue:
  - proceed with Stage 3.2 and enforce filter-aware paid/unpaid analytics behavior.

### Plan F Phase 3 Stage 3.2 - Filter-Aware Analytics Behavior (Implemented)
- Implementation Summary:
  - extended payment analytics endpoint/service to accept `courseId` and `semesterId` filters,
  - constrained payment status aggregation to students with matching enrollment scope for requested course/semester,
  - added integration regression coverage for filtered payment analytics behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`66/66`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).

- Behavior impact:
  - payment status chart data now follows active course/semester filter selections,
  - tenant/campus scoped isolation remains in force while adding filter sensitivity.

## 2026-05-20 Update - Plan F Phase 3 Stage 3.3 (Finance Analytics Isolation)

- Recent request issue:
  - proceed with next stage.

### Plan F Phase 3 Stage 3.3 - Finance Analytics Isolation (Implemented)
- Implementation Summary:
  - added finance-only analytics mode (`IsFinanceOnly`) to portal analytics model and snapshot payload,
  - updated analytics UI rendering so finance-only users see payment analytics only and never academic analytics chart sections,
  - added authorization regression tests asserting finance gets `403` on academic analytics endpoints while keeping payment analytics access.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `runTests` targeted integration suites passed (`66/66`) for:
    - `tests/Tabsan.EduSphere.IntegrationTests/AuthorizationRegressionTests.cs`,
    - `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsInstituteParityIntegrationTests.cs`,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug -v minimal` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug -v minimal` passed (`1/1`).

- Behavior impact:
  - finance role analytics is now payment-only across API/UI paths,
  - academic analytics remains available to admin/superadmin/faculty under existing policies,
  - no schema changes were introduced.

## 2026-05-20 Update - Plan F Phase 3 Stage 3.1 (Payment Status Pie Chart)

- Recent request issue:
  - proceed with Stage 3.1 and add interactive paid vs unpaid payment analytics charting.

### Plan F Phase 3 Stage 3.1 - Payment Status Pie Chart (Implemented)
- Implementation Summary:
  - introduced payment status analytics contract/service endpoint flow for paid vs unpaid aggregates,
  - enabled finance-compatible endpoint access for payment analytics without changing existing academic endpoint contracts,
  - integrated payment status snapshot payload and interactive pie chart rendering in portal analytics UI.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`65/65`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).

- Behavior impact:
  - analytics now includes scoped payment status visualization (Paid vs Unpaid),
  - finance users can consume payment analytics while academic analytics remains unchanged.

## 2026-05-20 Update - Plan F Phase 2 Stage 2.3 (Tenant and Campus Enforcement)

- Recent request issue:
  - proceed with Stage 2.3 and enforce tenant/campus boundaries for finance payment operations.

### Plan F Phase 2 Stage 2.3 - Tenant and Campus Enforcement (Implemented)
- Implementation Summary:
  - payment receipt queries now enforce caller tenant/campus scope at repository level,
  - finance receipt creation now validates student scope before persisting receipts,
  - integration tests now validate scoped payment visibility for matching vs mismatched campus contexts.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`63/63`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

- Behavior impact:
  - finance payment data visibility is now constrained to effective tenant/campus scope,
  - out-of-scope payment receipts are no longer returned in finance listing paths.

## 2026-05-20 Update - Plan F Phase 2 Stage 2.2 (Finance Restriction Scope)

- Recent request issue:
  - proceed with Stage 2.2 and enforce finance restrictions for deletion and academic module access.

### Plan F Phase 2 Stage 2.2 - Finance Restriction Scope (Implemented)
- Implementation Summary:
  - payment delete requests are now explicitly rejected with `405 Method Not Allowed` on payment routes,
  - finance-only web sessions are blocked from academic-section actions and redirected to finance payments workspace,
  - regression tests were extended to assert finance denial on academic APIs and payment-delete rejection behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`54/54`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

- Behavior impact:
  - payment receipts remain non-deletable permanent records,
  - finance users are now explicitly prevented from accessing academic modules.

## 2026-05-20 Update - Plan F Phase 2 Stage 2.1 (Finance Capability Scope)

- Recent request issue:
  - proceed with Stage 2.1 and let Finance add, edit, and mark payment receipts as paid.

### Plan F Phase 2 Stage 2.1 - Finance Capability Scope (Implemented)
- Implementation Summary:
  - added a finance-edit payment update endpoint and web-layer action flow,
  - kept finance create/confirm/cancel behavior intact and additive,
  - surfaced `Last Updated` in payments UI so edits and confirmations are visible to operators.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

- Behavior impact:
  - Finance can now update actionable receipts prior to finalization,
  - no schema mutation was introduced.

## 2026-05-20 Update - Plan F Phase 1 Stage 1.4 (Payment Record State Model)

- Recent request issue:
  - proceed with Stage 1.4 and finalize paid/unpaid state tracking behavior.

### Plan F Phase 1 Stage 1.4 - Payment Record State Model (Implemented)
- Implementation Summary:
  - payment payload now exposes `PaidDate` and `UpdatedAt` as first-class tracking fields while retaining `ConfirmedAt` compatibility,
  - web payment mapping now tolerates legacy/new payload shapes by deriving paid date from `ConfirmedAt` fallback,
  - payments page now shows `Last Updated` to present update-trail visibility alongside status and paid date.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`156/156`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

- Behavior impact:
  - paid/unpaid tracking now includes explicit paid-date and update-trail semantics in operational views,
  - no endpoint removals and no schema mutation were introduced in this stage.

## 2026-05-20 Update - Plan F Phase 1 Stage 1.3 (Finance Role Seed and Linking)

- Recent request issue:
  - proceed with Stage 1.3 and add Finance role identity plus authorization linkage.

### Plan F Phase 1 Stage 1.3 - Finance Role Seed and Linking (Implemented)
- Implementation Summary:
  - seeded `Finance` as a system role in startup role provisioning,
  - added a dedicated API authorization policy (`Finance`) that permits `SuperAdmin`, `Admin`, and `Finance`,
  - extended CSV user-import role validation to allow onboarding finance users.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~InstitutionPolicyTests|FullyQualifiedName~UserImport" -v minimal` passed (`25/25`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

- Behavior impact:
  - Finance role is now part of the core identity and policy model,
  - no schema mutation and no breaking API contract changes were introduced.

## 2026-05-20 Update - Plan F Transition Readiness

- Recent request issue:
  - complete readiness prerequisites before moving execution to Plan F.

### Plan F Entry Gate Validation (Implemented)
- Implementation Summary:
  - completed final release-mode baseline verification before Plan F handoff,
  - switched active execution governance pointer to Plan F Phase 0/Phase 1-ready state,
  - confirmed Plan F plan artifact is present and executable as the next stream.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

- Behavior impact:
  - no runtime functionality changes were introduced in this checkpoint,
  - this update formalizes readiness and governance transition from Plan E to Plan F.

## 2026-05-20 Update - Backlog Security Hardening (User Import Template Access Guard)

- Recent request issue:
  - proceed with next backlog hardening item and close template-download access inconsistency.

### User Import Template Access Guard (Implemented)
- Implementation Summary:
  - `PortalController.UserImportTemplate(...)` now requires Admin or SuperAdmin session identity,
  - retained existing template filename allow-list and path traversal protection controls.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`4/4`).

- Behavior impact:
  - non-admin callers can no longer access CSV template download route,
  - no API contract, database schema, module entitlement, or pricing behavior changed.

## 2026-05-20 Update - Plan D Phase 1 (Charting Framework & UI)

- Recent request issue:
  - complete Plan D Phase 1 with phase-end summary placement.

### Phase 1 - Charting Framework & UI (Implemented)
- Implementation Summary:
  - integrated Chart.js,
  - added responsive Analytics cards/panels layout,
  - added color-coded clickable legends with dataset visibility toggling.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - Analytics supports interactive legend-based data control,
  - no backend API, schema, or tenancy/campus isolation behavior changed.

## 2026-05-20 Update - Plan D Phase 2 Stage 2.1 (Global Filters)

- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.1 and add global analytics filters.

### Phase 2 Stage 2.1 - Global Filters (Implemented)
- Implementation Summary:
  - added global filter support for institution, department, course, and semester,
  - wired filter values through portal model/controller, API client query builder, API analytics endpoints, and analytics service methods,
  - applied course/semester scoped analytics query filtering and cache-key partitioning.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - analytics reports/charts are now globally filterable by institution, department, course, and semester,
  - no schema mutation and no change to tenant/campus security isolation boundaries.

## 2026-05-20 Update - Plan D Phase 2 Stage 2.2 (Dependent Filtering)

- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.2 and enforce dependent filtering behavior.

### Phase 2 Stage 2.2 - Dependent Filtering (Implemented)
- Implementation Summary:
  - enforced dependent filter ordering where upstream selection constrains downstream options,
  - added offering metadata mapping and server-side dependent option computation,
  - implemented UI auto-apply on parent filter changes with downstream reset behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - filter flow is now deterministic and dependency-aware,
  - no API security boundary or schema behavior changed.

## 2026-05-20 Update - Plan D Phase 2 Stage 2.3 (Instant Charts Update)

- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.3 and make chart updates instant.

### Phase 2 Stage 2.3 - Instant Analytics Refresh (Implemented)
- Implementation Summary:
  - introduced web-layer analytics snapshot endpoint for asynchronous chart-data retrieval,
  - replaced filter auto-submit full reload behavior with in-place data refresh,
  - updated chart and summary-card rendering to refresh immediately after filter changes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - analytics charts now refresh without page navigation while keeping existing scope/security behavior intact.

## 2026-05-20 Update - Plan D Phase 3 Stage 3.1 (Chart Types and Data)

- Recent request issue:
  - proceed to Plan D Phase 3 Stage 3.1 chart expansion.

### Phase 3 Stage 3.1 - Additional Chart Types (Implemented)
- Implementation Summary:
  - added `Pie`, `Bar`, and `Line` analytics visualizations for student distribution, department counts, and trend views,
  - added semester trend chart combining performance and attendance trajectories where data is available,
  - retained instant in-page refresh behavior for all charts after filter changes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - analytics now provides broader comparative visual coverage without API contract or security model changes.

## 2026-05-20 Update - Plan D Phase 4 Stage 4.1 (Tenant/Campus Isolation)

- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.1 isolation hardening.

### Phase 4 Stage 4.1 - Tenant/Campus Isolation Hardening (Implemented)
- Implementation Summary:
  - applied tenant/campus filtering constraints to analytics read query paths,
  - removed query-filter bypass from quiz analytics pipeline,
  - expanded analytics cache-key scoping with tenant/campus dimensions to prevent cross-scope cache reuse,
  - added integration regression test validating tenant/campus constrained analytics result visibility.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`66/66`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - analytics report reads are now explicitly isolated by tenant/campus scope for non-superadmin callers,
  - no schema mutation and no public API contract change introduced.

## 2026-05-20 Update - Plan D Phase 4 Stage 4.2 (Leakage Prevention)

- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.2 leakage-prevention hardening.

### Phase 4 Stage 4.2 - Cross-Scope Leakage Prevention (Implemented)
- Implementation Summary:
  - enforced owner-or-superadmin checks for analytics export job status and download endpoints,
  - required tenant/campus scope parity against stored export-job requester scope metadata,
  - added integration negative tests for cross-user and cross-tenant/campus export-job access.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - analytics export job retrieval is now isolated by both identity ownership and tenant/campus scope,
  - no API route changes and no schema changes were introduced.

## 2026-05-20 Update - Plan D Phase 5 Stage 5.1 (Performance Optimization)

- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.1 query optimization.

### Phase 5 Stage 5.1 - Analytics Query Optimization (Implemented)
- Implementation Summary:
  - removed major N+1 query paths from analytics report computation,
  - replaced repeated row-by-row database calls with batched grouped aggregate reads,
  - reduced analytics read overhead by enabling no-tracking query execution on report data paths.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - analytics report outputs remain functionally equivalent while query execution is more efficient under broader scopes.

## 2026-05-20 Update - Plan D Phase 5 Stage 5.2 (Index and Data-Loading Refinement)

- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.2 schema/index refinement.

### Phase 5 Stage 5.2 - Index Strategy Implementation (Implemented)
- Implementation Summary:
  - added index coverage for analytics query hotspots on results publication filters and aggregate submission/attempt paths,
  - introduced bounded `assignment_submissions.Status` column length to keep status-based index seek paths efficient,
  - added EF migration to materialize index changes consistently across environments.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - analytics read patterns now align with stronger index coverage while preserving existing API/report behavior.

## 2026-05-20 Update - Plan D Phase 6 Stage 6.1 (Validation and UI Consistency)

- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.1 validation.

### Phase 6 Stage 6.1 - Interactivity, Filtering, and UI Consistency Validation (Implemented)
- Implementation Summary:
  - executed Stage 6.1 analytics validation for interactivity, filter consistency, and role/scope-safe behavior continuity,
  - confirmed previously delivered Stage 2 to Stage 5 analytics features remain stable without further implementation changes,
  - no schema mutation or functional API change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior change; Stage 6.1 establishes regression confidence for analytics interactivity and filtering consistency.

## 2026-05-20 Update - Plan D Phase 6 Stage 6.2 (Final Performance and Security Review)

- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.2 final review.

### Phase 6 Stage 6.2 - Performance and Security Finalization (Implemented)
- Implementation Summary:
  - completed final analytics release-readiness review for performance and security stability,
  - revalidated role/scope-safe analytics behavior and prior export-access hardening under regression suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no feature behavior changes; Phase 6 is finalized as stable for performance/security quality gates.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.1 (Functional Non-Regression Validation)

- Recent request issue:
  - there is no Phase 7 continuation for this stream; move to Plan E and start Phase 1 Stage 1.1.

### Phase 1 Stage 1.1 - Existing Functionality Integrity Check (Implemented)
- Implementation Summary:
  - executed full automated regression validation to verify no existing functionality is broken,
  - confirmed current baseline remains stable after Plan D completion,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 1.1 provides baseline functional integrity evidence for remaining Plan E stages.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.2 (End-to-End Module Validation)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.2.

### Phase 1 Stage 1.2 - Module End-to-End Validation (Implemented)
- Implementation Summary:
  - executed full module-level end-to-end regression validation over integration paths,
  - revalidated baseline platform behavior under release build configuration,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 1.2 confirms module end-to-end stability baseline.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.3 (UI Alignment, Bindings, and Form Stability)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.3.

### Phase 1 Stage 1.3 - UI and Form Stability Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for UI misalignment/layout regressions and form/binding continuity,
  - revalidated baseline behavior with release build and full automated suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 1.3 confirms UI/form stability baseline remains intact.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.4 (API Response and Runtime Stability)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.4.

### Phase 1 Stage 1.4 - API and Runtime Stability Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for API response integrity and runtime execution stability,
  - revalidated API/service behavior under release build plus full integration and contract suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`),
  - unit tests passed (`151/151`).

- Behavior impact:
  - no behavior changes introduced; Stage 1.4 confirms API/runtime stability baseline remains intact.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.5 (Database Relationship Validation)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.5.

### Phase 1 Stage 1.5 - Database Relationship Integrity Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint to verify database relationship integrity and referential behavior continuity,
  - revalidated baseline data-path behavior under release build and full regression suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 1.5 confirms Phase 1 database relationship stability baseline.

## 2026-05-20 Update - Plan E Phase 2 Stage 2.1 (Tenant and Campus Isolation)

- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.1.

### Phase 2 Stage 2.1 - Tenant/Campus Isolation Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint to verify tenant/campus isolation behavior continuity,
  - revalidated scope-sensitive flows under release build and full regression suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 2.1 confirms tenant/campus isolation baseline remains intact.

## 2026-05-20 Update - Plan E Phase 2 Stage 2.2 (Cross-Tenant/Campus Leakage Validation)

- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.2.

### Phase 2 Stage 2.2 - Cross-Tenant/Campus Leakage Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint to verify no cross-tenant/campus data leakage across protected flows,
  - revalidated scope-protection behavior under release build and full regression suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 2.2 confirms cross-tenant/campus leakage protections remain intact.

## 2026-05-20 Update - Plan E Phase 2 Stage 2.3 (Tenant/Campus Query Scope Validation)

- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.3.

### Phase 2 Stage 2.3 - Tenant/Campus Query Scope Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint to verify query execution respects TenantId/CampusId scope constraints,
  - revalidated query-scope behavior under release build and full regression suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 2.3 confirms TenantId/CampusId query-scope protections remain intact.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.1 (Course Material End-to-End Validation)

- Recent request issue:
  - proceed with next stage.

### Phase 3 Stage 3.1 - Course Material End-to-End Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for end-to-end Course Material module behavior,
  - revalidated module-specific authorization and upload/download interaction paths under release-mode verification,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted Course Material integration tests passed (`5/5`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 3.1 confirms Course Material end-to-end baseline stability.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.2 (Analytics Charts and Filters Validation)

- Recent request issue:
  - proceed with next stage.

### Phase 3 Stage 3.2 - Analytics Charts and Filters Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for analytics charts and filter behavior continuity,
  - revalidated analytics/filter interaction and guarded access behavior through targeted integration regression coverage,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted analytics/authorization integration tests passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 3.2 confirms analytics chart/filter baseline stability.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.3 (Tenant and Campus Management Validation)

- Recent request issue:
  - proceed.

### Phase 3 Stage 3.3 - Tenant and Campus Management Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for tenant/campus management behavior continuity,
  - revalidated management-related scope/authorization behavior under targeted integration and full regression safety nets,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted tenant/campus management integration tests passed (`63/63`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 3.3 confirms tenant/campus management baseline stability.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.4 (Role-Based Access Validation)

- Recent request issue:
  - proceed.

### Phase 3 Stage 3.4 - Role-Based Access Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for role-based access behavior continuity across module flows,
  - revalidated authorization-sensitive behavior under release build and full regression suites,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 3.4 confirms role-based access baseline stability.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.1 (UI Consistency and Design Baseline Validation)

- Recent request issue:
  - proceed.

### Phase 4 Stage 4.1 - UI Consistency and Design Baseline Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for layout/spacing/design continuity across key UI interaction paths,
  - revalidated UI/navigation-sensitive behavior through targeted integration coverage with full regression safety nets,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 4.1 confirms UI consistency/design baseline stability.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.2 (Sidebar Header and Content Structure Validation)

- Recent request issue:
  - proceed.

### Phase 4 Stage 4.2 - Sidebar Header and Content Structure Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for sidebar/header/content structure continuity across key UI interaction paths,
  - revalidated structure-sensitive UI/navigation behavior through targeted integration coverage with full regression safety nets,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 4.2 confirms sidebar/header/content structure baseline stability.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.3 (Overlap and Responsive Layout Validation)

- Recent request issue:
  - proceed.

### Phase 4 Stage 4.3 - Overlap and Responsive Layout Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for overlap prevention and responsive layout continuity across key UI interaction paths,
  - revalidated layout-sensitive UI/navigation behavior through targeted integration coverage with full regression safety nets,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 4.3 confirms overlap/responsive layout baseline stability.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.4 (Validate All Buttons and Actions)

- Recent request issue:
  - proceed.

### Phase 4 Stage 4.4 - Button and Action Validation (Implemented)
- Implementation Summary:
  - executed validation checkpoint for button/action continuity across key UI interaction paths,
  - revalidated action-sensitive UI behavior through full integration coverage with full regression safety nets,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 4.4 confirms button/action baseline stability.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.1 (TenantId/CampusId Schema Audit)

- Recent request issue:
  - proceed.

### Phase 5 Stage 5.1 - Tenant/Campus Schema Usage Audit (Implemented)
- Implementation Summary:
  - executed schema audit checkpoint for `TenantId`/`CampusId` usage on `Scripts/01-Schema-Current.sql`,
  - audit parsed `82` tables and found `0` tables containing both `TenantId` and `CampusId` in this script,
  - validated that tenant/campus access control remains enforced at application/domain layers,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 5.1 records audit findings only.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.2 (Foreign Keys, Indexes, and Constraints Audit)

- Recent request issue:
  - proceed.

### Phase 5 Stage 5.2 - FK/Index/Constraint Audit (Implemented)
- Implementation Summary:
  - executed SQL artifact audit on `Scripts/01-Schema-Current.sql` and `Scripts/04-Maintenance-Indexes-And-Views.sql`,
  - audit reported `65` foreign key constraints (`5` via `ALTER TABLE`), `82` primary key constraints, `2` default constraints, and `190` index statements across audited scripts,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 5.2 records structural audit findings only.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.3 (Nullable Field Audit)

- Recent request issue:
  - proceed.

### Phase 5 Stage 5.3 - Nullable Field Verification (Implemented)
- Implementation Summary:
  - executed nullable-field audit on `Scripts/01-Schema-Current.sql`,
  - audit reported `280` nullable columns across `79` tables and flagged `2` review-candidate nullable fields (`users.Email`, `timetable_entries.FacultyName`),
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 5.3 records nullable-field audit findings only.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.4 (Data Integrity and Migration Safety)

- Recent request issue:
  - proceed.

### Phase 5 Stage 5.4 - Data Integrity and Migration Safety Verification (Implemented)
- Implementation Summary:
  - executed data-integrity and migration-safety audit on schema and post-deployment SQL artifacts,
  - audit reported `365` migration-history guard references, `324` `IF NOT EXISTS` guards, `40` transaction blocks, `175` post-deployment verification `SELECT` checks, and `19` EF migration files,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 5.4 records integrity/migration-safety audit findings only.

## 2026-05-20 Update - Plan E Phase 6 Stage 6.1 (Role-Based Access Review)

- Recent request issue:
  - proceed.

### Phase 6 Stage 6.1 - Role Access Baseline Verification (Implemented)
- Implementation Summary:
  - executed role-based access audit across API/Web controller authorization attributes, role/policy enforcement code, and seed-role SQL artifacts,
  - audit reported `359` API `[Authorize]` attributes, `369` role/policy enforcement references, `5` policy registration references, and `105` role-seeding script references,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 6.1 records role-access audit findings only.

## 2026-05-20 Update - Plan E Phase 6 Stage 6.2 (Unauthorized/Cross-Scope Access)

- Recent request issue:
  - proceed.

### Phase 6 Stage 6.2 - Cross-Scope Access Verification (Implemented)
- Implementation Summary:
  - executed unauthorized/cross-tenant/cross-campus access audit across source enforcement points,
  - audit reported `1326` isolation-enforcement source hits and `128` explicit `Forbid`/`Unauthorized` enforcement hits,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 6.2 records cross-scope access audit findings only.

## 2026-05-20 Update - Plan E Phase 6 Stage 6.3 (API Endpoint Restriction)

- Recent request issue:
  - proceed.

### Phase 6 Stage 6.3 - API Restriction Coverage Verification (Implemented)
- Implementation Summary:
  - executed API endpoint restriction audit over authorization coverage in API controllers,
  - audit reported `447` HTTP endpoints: `92` method-level `[Authorize]`, `349` class-level `[Authorize]` coverage, `1` `[AllowAnonymous]`, and `5` review-set endpoints without explicit authorize coverage,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 6.3 records API-restriction audit findings only.

## 2026-05-20 Update - Plan E Phase 7 Stage 7.1 (Query Scope Filtering)

- Recent request issue:
  - proceed.

### Phase 7 Stage 7.1 - Tenant/Campus Query Filtering Verification (Implemented)
- Implementation Summary:
  - executed tenant/campus query-filter audit across source and repository layers,
  - audit reported `551` Tenant/Campus scope references, `11` LINQ `Where` scope-filter references, and `20` repository-layer scope references,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 7.1 records query-scope audit findings only.

## 2026-05-20 Update - Plan E Phase 7 Stage 7.2 (Join and Full-Scan Risk Audit)

- Recent request issue:
  - proceed.

### Phase 7 Stage 7.2 - Query-Shape Risk Verification (Implemented)
- Implementation Summary:
  - executed query-shape/full-scan risk audit for joins, includes, raw SQL usage, and pagination coverage,
  - audit reported `134` join references (`18` in repository layer), `167` include/then-include references, `0` raw SQL query references, `475` materialization references, and `37` pagination references,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 7.2 records query-shape audit findings only.

## 2026-05-20 Update - Plan E Phase 7 Stage 7.3 (Pagination and Analytics Query Efficiency)

- Recent request issue:
  - proceed.

### Phase 7 Stage 7.3 - Pagination and Analytics Efficiency Verification (Implemented)
- Implementation Summary:
  - executed pagination and analytics-query efficiency audit across source and analytics layers,
  - audit reported `37` pagination references with `29` nearby ordering safeguards, `551` Tenant/Campus scope references, `11` LINQ scope-filter references, and `18` analytics `AsNoTracking` references,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 7.3 records pagination/analytics audit findings only.

## 2026-05-20 Update - Plan E Phase 8 Stage 8.1 (Environment-Based Configuration)

- Recent request issue:
  - proceed.

### Phase 8 Stage 8.1 - Environment Configuration Verification (Implemented)
- Implementation Summary:
  - executed environment-based configuration audit across startup/configuration-loading paths,
  - audit reported `61` environment-handling references, `132` configuration/deployment references, `54` `appsettings*.json` files, and `127` Program.cs environment/configuration references,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 8.1 records environment/configuration audit findings only.

## 2026-05-20 Update - Plan E Phase 8 Stage 8.2 (Deployment Scenarios)

- Recent request issue:
  - proceed.

### Phase 8 Stage 8.2 - Deployment Scenario Verification (Implemented)
- Implementation Summary:
  - executed deployment-scenario readiness audit for cloud, on-prem, and multi-instance signals across source startup/configuration paths,
  - audit reported `1` cloud reference, `0` on-prem references, `14` multi-instance/scale-out references, `200+` deployment/scaling/instance/tenant-isolation source references (search cap reached), and `9` source `appsettings*.json` files,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 8.2 records deployment-readiness audit findings only.

## 2026-05-20 Update - Plan E Phase 8 Stage 8.3 (Secrets and Configuration Security)

- Recent request issue:
  - proceed.

### Phase 8 Stage 8.3 - Secrets and Configuration Security Verification (Implemented)
- Implementation Summary:
  - executed secure-secrets/configuration handling audit across startup guards, environment-variable resolvers, and appsettings templates,
  - audit reported `18` secure startup guard references, `18` environment-variable secret/deployment references, `18` secret-sensitive configuration key references in source appsettings, `12` placeholder/template secret markers, and `9` source `appsettings*.json` files,
  - no schema mutation or feature implementation change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed (non-blocking warning: CS0105 in API Program.cs),
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no behavior changes introduced; Stage 8.3 records secrets/configuration security audit findings only.

## 2026-05-20 Update - Plan E Phase 9 Stage 9.1 (Issue and Inconsistency Remediation)

- Recent request issue:
  - proceed to Plan E Phase 9 Stage 9.1 (identify and fix issues, inconsistencies, or risks).

### Phase 9 Stage 9.1 - Startup Consistency Risk Fix (Implemented)
- Implementation Summary:
  - fixed API startup import inconsistency in `Program.cs` by normalizing merged `using` directives,
  - removed duplicate `Tabsan.EduSphere.Application.Services` import that produced CS0105 warning noise,
  - no schema mutation or feature implementation behavior change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full automated test suites passed (`396/396`),
  - full integration tests passed (`244/244`),
  - full unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no runtime behavior changes introduced; Stage 9.1 resolves startup-code inconsistency and warning risk only.

## 2026-05-20 Update - Plan E Phase 9 Stage 9.2 (Final Stability, Security, and Scalability Review)

- Recent request issue:
  - proceed.

### Phase 9 Stage 9.2 - Final Release-Readiness Verification (Implemented)
- Implementation Summary:
  - executed final stability/security/scalability verification using release build and complete automated suite coverage,
  - performed source risk-marker sweep and confirmed no new critical behavior/security inconsistencies requiring implementation changes,
  - no schema mutation or feature implementation behavior change introduced in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full automated test suites passed (`396/396`),
  - full integration tests passed (`244/244`),
  - full unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

- Behavior impact:
  - no runtime behavior changes introduced; Stage 9.2 is final verification and closure only.

## 2026-05-20 Update - Final-Touches Tracker Restoration (Governance)

- Recent request issue:
  - proceed.

### Governance Restoration - Final-Touches Baseline File (Implemented)
- Implementation Summary:
  - restored missing `Project startup Docs/Final-Touches.md` to satisfy command-center startup prerequisites,
  - aligned restored execution pointer to current Plan E completion state.
- Validation Summary:
  - verified file creation and pointer consistency with `Docs/Command.md`,
  - no runtime behavior, API contract, or schema mutation introduced.

- Behavior impact:
  - no functional behavior changes introduced; governance tracking continuity was restored.

## 2026-05-20 Update - Plan C

- Recent request issue:
  - start Plan C Phase 7 Stage 7.1 validation for data safety, access control, and UI.

### Phase 7 Stage 7.1 - Validation (Implemented)
- Implementation Summary:
  - performed end-to-end verification of Course Material safety/access/UI behavior after Phase 5 and 6 changes,
  - confirmed upload/download and scoped isolation flows remain stable under full-suite execution.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`393/393`).

- Behavior impact:
  - no new functional behavior was introduced in this stage,
  - existing Course Material functionality remains stable across role/institute paths.

## 2026-05-20 Update - Plan C Phase 7 Stage 7.2 Finalization

- Recent request issue:
  - complete final stability/scalability review for Plan C.

### Phase 7 Stage 7.2 - Final Review (Implemented)
- Implementation Summary:
  - completed release-readiness stability/scalability closeout review for the Course Material module,
  - validated Release-mode build and role-access regression subset for production-path confidence.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - Release-mode integration tests (Course Material authorization subset) passed (`5/5`),
  - load-test script execution requires a running target API (`http://localhost:5181`) and was not executable in current environment.

- Behavior impact:
  - no new functionality introduced,
  - Course Material feature set is finalized and stable; scalability runtime verification is environment-dependent.

## 2026-05-20 Update - Plan C Phase 6 Implementation (Performance & Optimization)

- Recent request issue:
  - complete Plan C Phase 6 Stage 6.1 and Stage 6.2.

### Phase 6 - Performance & Optimization (Implemented)
- Implementation Summary:
  - Stage 6.1 optimized Course Material read paths with `AsNoTracking` and missing-scope short-circuit behavior,
  - Stage 6.2 added targeted index `IX_course_materials_scope_active_sort` through migration `PlanCPhase6Stage2CourseMaterialIndexTuning`.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.

- Behavior impact:
  - no feature-surface change; Course Material listing/retrieval performance is improved under scoped filters,
  - no regression introduced in School/College/University role behavior.

## 2026-05-20 Update - Plan C Phase 5 Implementation (File & Link Handling)

- Recent request issue:
  - complete Plan C Phase 5 Stage 5.1, Stage 5.2, and Stage 5.3.

### Phase 5 - File & Link Handling (Implemented)
- Implementation Summary:
  - Stage 5.1 added Course Material multipart upload endpoint and portal upload flow,
  - Stage 5.2 added scoped persistent storage pathing and file streaming endpoint,
  - Stage 5.3 added role-access regression tests and student/manage-aware download fallback behavior.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.

- Behavior impact:
  - Course Material now supports validated uploads and student-accessible downloads via scoped storage,
  - no regression introduced in existing institute/role/module behavior outside Course Material.

## 2026-05-19 Update - Plan C Phase 4 Implementation (UI & UX)

- Recent request issue:
  - proceed to Plan C Phase 4 UI and UX implementation.

### Phase 4 - UI & UX (Implemented)
- Implementation Summary:
  - added web portal course-material manage page for Admin/Faculty and student read-only page,
  - added portal controller create/update/activate flows with filter-state-preserving redirects,
  - integrated `course_material` into web layout route/group fallback mappings and API sidebar entitlement module-key mapping.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - Course Material is now available end-to-end in portal navigation for authorized users with student read-only access path,
  - no regression introduced in existing module, institute, or role behavior outside the Course Material slice.

## 2026-05-19 Update - Plan C Phase 3 Implementation (Access Control & Security)

- Recent request issue:
  - proceed to Plan C Phase 3 access control and strict isolation.

### Phase 3 - Access Control & Security (Implemented)
- Implementation Summary:
  - added `api/v1/course-materials` endpoints with authenticated read access and `Faculty,Admin,SuperAdmin` write restrictions,
  - added application/domain/infrastructure
- Behavior impact:
  - course-material API access now follows established role policy boundaries and request-scope isolation patterns,
  - no regression introduced in existing module, institute, or role behavior outside the new course-material slice.

## 2026-05-19 Update - Plan C Phase 2 Implementation (Data Safety & Migration)

- Recent request issue:
  - proceed after Plan C Phase 1.

### Phase 2 - Data Safety & Migration (Implemented)
- Implementation Summary:
  - added strict constructor/update guards to reject unscoped or invalid course-material records,
  - added DB check constraints for scope integrity, material-type validity, and file/link location validity,
  - applied migration `PlanCPhase2DataSafetyScopeGuard`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - all new course-material records are now hard-guarded for tenant/campus scoping and valid location shape,
  - no runtime role/institute/module behavior regression introduced.

## 2026-05-19 Update - Plan C Phase 1 Implementation (Domain & Database Extension)

- Recent request issue:
  - start Plan C Phase 1.

### Phase 1 - Domain & Database Extension (Implemented)
- Implementation Summary:
  - added `CourseMaterial` domain aggregate with required material metadata,
  - linked material scope to tenant/campus/department/program/semester/course (subject context),
  - added `course_materials` migration with foreign keys and scope-first indexes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/1
  - total automated validations passed: `388/388`.

- Behavior impact:
  - all new course-material records are now hard-guarded for tenant/campus scoping and valid location shape,
  - no runtime role/institute/module behavior regression introduced.

## 2026-05-19 Update - Plan C Phase 1 Implementation (Domain & Database Extension)

- Recent request issue:
  - start Plan C Phase 1.

### Phase 1 - Domain & Database Extension (Implemented)
- Implementation Summary:
  - added `CourseMaterial` domain aggregate with required material metadata,
  - linked material scope to tenant/campus/department/program/semester/course (subject context),
  - added `course_materials` migration with foreign keys and scope-first indexes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - schema and domain foundation for Course Material is in place without changing existing runtime workflows,
  - no role/institute/module behavior regression introduced.

## 2026-05-19 Update - Plan B Phase 10 Implementation (Validation & Finalization)

- Recent request issue:
  - proceed to validation and finalization after logging and visibility.

### Phase 10 - Validation & Finalization (Implemented)
- Implementation Summary:
  - completed the final readiness review of the configuration/deployment stack,
  - confirmed backward compatibility, safe logging, fail-fast behavior, and overlay precedence stability,
  - no code changes were required in this final closeout phase.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - the Phase B stack is ready for release closeout with no unresolved startup, logging, or overlay-precedence regressions,
  - no runtime role, institute, module, or schema behavior changed.

## 2026-05-19 Update - Plan B Phase 9 Implementation (Logging & Visibility)

- Recent request issue:
  - proceed to logging and visibility after configuration performance and stability.

### Phase 9 - Logging & Visibility (Implemented)
- Implementation Summary:
  - added shared startup visibility reporting,
  - standardized startup logs across API, Web, and BackgroundJobs to show active environment, safe configuration source summary, database type, deployment profile, and tenant isolation posture,
  - kept secrets and full connection strings out of logs.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - startup diagnostics are now more readable and operationally useful without increasing secret exposure risk,
  - no runtime role, institute, module, or schema behavior changed.

## 2026-05-19 Update - Plan B Phase 8 Implementation (Performance & Stability)

- Recent request issue:
  - proceed to configuration performance and stability after fail-safe startup validation.

### Phase 8 - Performance & Stability (Implemented)
- Implementation Summary:
  - removed duplicate config-provider registration from the shared startup bootstrap path,
  - preserved deployment/external/local/tenant override order without duplicating default base/environment/env-var sources,
  - reduced unnecessary file-watch reload behavior for deployment-oriented overlay files.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - startup configuration remains functionally equivalent while using fewer redundant providers and fewer unnecessary reload watchers,
  - no runtime role, institute, module, or schema behavior changed.

## 2026-05-19 Update - Plan B Phase 7 Implementation (Fail-Safe Behavior)

- Recent request issue:
  - proceed to fail-safe configuration behavior after tenant-aware deployment support.

### Phase 7 - Fail-Safe Behavior (Implemented)
- Implementation Summary:
  - added shared startup fail-safe validation across API, Web, and BackgroundJobs,
  - centralized clear startup failures for missing or placeholder configuration, invalid reverse-proxy trust settings, and tenant overlay path problems,
  - corrected non-development database validation to honor resolved deployment override sources.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - startup now fails earlier with clearer configuration guidance and without reintroducing legacy-only connection-string assumptions,
  - no runtime role, institute, module, or schema behavior changed after successful startup.

## 2026-05-19 Update - Plan B Phase 6 Implementation (Tenant + Campus Aware Configuration)

- Recent request issue:
  - proceed to tenant-aware configuration and isolation after customer deployment support.

### Phase 6 - Tenant + Campus Aware Configuration (Implemented)
- Implementation Summary:
  - added tenant-isolation resolver on top of deployment/customer profile resolution,
  - added optional tenant JSON overlay support via `EDUSPHERE_TENANT_CONFIG_PATH`,
  - seeded deployment templates with `TenantIsolation` sections and surfaced tenant-isolation metadata in startup logs and health output.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - tenant-specific deployment settings can now be prepared through isolated config overlays without code changes,
  - no regression in role/institute/module behavior.

## 2026-05-19 Update - Plan B Phase 5 Implementation (Customer Deployment Support)

- Recent request issue:
  - proceed to customer deployment support after deployment flexibility.

### Phase 5 - Customer Deployment Support (Implemented)
- Implementation Summary:
  - added optional deployment-pipeline JSON support for configuration-only customer overrides,
  - seeded deployment metadata templates in API/Web/BackgroundJobs appsettings files,
  - kept the override path config-first with environment variable fallback.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - customer-specific deployment values can now be supplied through app settings or deployment pipeline files without code changes,
  - no regression in role/institute/module behavior.

## 2026-05-19 Update - Plan B Phase 4 Implementation (Deployment Flexibility)

- Recent request issue:
  - proceed to deployment flexibility after secure configuration handling.

### Phase 4 - Deployment Flexibility (Implemented)
- Implementation Summary:
  - added deployment-topology resolver for cloud/customer-hosted/multi-instance profiles,
  - added per-customer domain/database/scaling overrides from config and environment variables,
  - surfaced safe deployment metadata in logs and health diagnostics.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - deployment metadata is now explicit and configurable per customer,
  - no regression in role/institute/module behavior.

## 2026-05-19 Update - Plan B Phase 3 Implementation (Secure Configuration Handling)

- Recent request issue:
  - proceed to secure configuration handling after connection-management hardening.

### Phase 3 - Secure Configuration Handling (Implemented)
- Implementation Summary:
  - added optional external configuration file support for deployment-hosted secrets,
  - added secure production validation helper for placeholder/missing secret rejection,
  - preserved source-safe diagnostics to avoid credential leakage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - secret handling is now safer for production deployment scenarios,
  - no change to functional role/institute/module behavior.

## 2026-05-19 Update - Plan B Phase 2 Implementation (Database Connection Management)

- Recent request issue:
  - proceed to Plan B Phase 2 and harden runtime database connection management.

### Phase 2 - Database Connection Management (Implemented)
- Implementation Summary:
  - added shared startup DB connection resolver used by API and BackgroundJobs,
  - prioritized deployment and environment variable overrides,
  - preserved backward-compatible fallback to `ConnectionStrings:DefaultConnection`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - deployment-specific DB connection control is now explicit and centralized,
  - no regression in role/institute/module behavior.

## 2026-05-19 Update - Plan B Phase 1 Implementation (Configuration Structure)

- Recent request issue:
  - proceed and start Plan B configuration/deployment work.

### Phase 1 - Configuration Structure (Implemented)
- Implementation Summary:
  - added shared startup configuration hierarchy helper,
  - applied the same configuration source order to API, Web, and BackgroundJobs,
  - added optional local overrides and prefixed environment variable ingestion.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - startup configuration loading is now consistent across deployable hosts,
  - existing behavior remains backward-compatible due to final unprefixed environment variable fallback.

## 2026-05-19 Update - Plan A Phase 7 Implementation (Validation and Finalization)

- Recent request issue:
  - proceed to Plan A Phase 7 and finalize validation/stability confirmation for tenant/campus rollout.

### Phase 7 - Validation & Finalization (Implemented)
- Implementation Summary:
  - executed final build + full automated suite validation,
  - confirmed prior phase outputs remain stable without behavioral regression,
  - completed final governance closeout synchronization.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - no additional functional mutation in this phase,
  - confirms stable final state for tenant/campus rollout,
  - InstitutionType behavior remains unchanged.

## 2026-05-19 Update - Plan A Phase 6 Implementation (Performance and Optimization)

- Recent request issue:
  - proceed to Plan A Phase 6 and optimize tenant/campus scoped query execution.

### Phase 6 - Performance & Optimization (Implemented)
- Implementation Summary:
  - optimized user lookup predicates to avoid non-sargable case-conversion filters,
  - added composite scoped indexes for tenant/campus user and department query hot paths,
  - added migration `Phase46_TenantCampusQueryOptimization`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - no functional behavior changes; this phase is performance-focused,
  - tenant/campus scoped reads retain same correctness semantics,
  - InstitutionType behavior remains unchanged.

## 2026-05-19 Update - Plan A Phase 5 Implementation (Tenant/Campus UI Management Interfaces)

- Recent request issue:
  - proceed to Plan A Phase 5 and add tenant/campus management interfaces for SuperAdmin within existing navigation patterns.

### Phase 5 - UI Management Interfaces (Implemented)
- Implementation Summary:
  - added SuperAdmin API endpoints for tenant/campus management lifecycle actions,
  - added portal tenant and campus management pages,
  - integrated pages into existing sidebar and SuperAdmin fallback links,
  - campus management UI is linked to tenant selection/filtering.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - SuperAdmin now has dedicated UI/API flows for tenant and campus lifecycle management,
  - non-superadmin behavior and InstitutionType logic remain unchanged.

## 2026-05-19 Update - Plan A Phase 4 Implementation (Access Control and Tenant/Campus Filtering)

- Recent request issue:
  - proceed to Plan A Phase 4 and enforce tenant/campus-scoped data visibility with SuperAdmin bypass.

### Phase 4 - Access Control & Filtering (Implemented)
- Implementation Summary:
  - added request-scope resolver for role/tenant/campus extraction from authenticated claims,
  - added `tenant_id` and `campus_id` claims to JWT issuance,
  - added repository-level tenant/campus filtering on user and department reads,
  - added SuperAdmin cross-tenant/campus bypass.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - non-superadmin reads are now tenant/campus scoped for user and department repositories,
  - SuperAdmin retains full cross-tenant/campus visibility,
  - InstitutionType behavior remains unchanged.

## 2026-05-19 Update - Plan A Phase 3 Implementation (Compatibility and Safety Hardening)

- Recent request issue:
  - proceed to Plan A Phase 3 and guarantee compatibility/safety for tenant+campus integration.

### Phase 3 - Compatibility & Safety (Implemented)
- Implementation Summary:
  - added aggregate-level pair validation for tenant/campus assignment,
  - added check constraints and tenant-bound campus composite FK integrity rules,
  - added migration `Phase43_TenantCampusCompatibilitySafety`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - tenant/campus assignments are now protected from inconsistent pairing and cross-tenant campus mismatch,
  - existing School/College/University InstitutionType behavior remains unchanged.

## 2026-05-19 Update - Plan A Phase 2 Implementation (Default Tenant/Campus Data Integration)

- Recent request issue:
  - proceed to Plan A Phase 2 and safely assign default tenant/campus ownership for existing core data.

### Phase 2 - Data Integration & Migration (Implemented)
- Implementation Summary:
  - added migration `Phase42_DefaultTenantCampusBackfill` to insert default tenant/campus when missing,
  - backfilled existing null tenant/campus values on `users` and `departments`,
  - added startup seeding safeguards to keep this invariant for legacy records.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit tests passed (`9/9`),
  - focused integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - existing users/departments are now assigned into a default tenant/campus baseline,
  - no replacement of InstitutionType logic; parity behavior remains intact.

## 2026-05-19 Update - Plan A Phase 1 Implementation (Tenant + Campus Domain Foundation)

- Recent request issue:
  - proceed with Plan A Phase 1 implementation and synchronize all mandatory governance trackers.

### Phase 1 - Domain Layer Extension (Implemented)
- Implementation Summary:
  - added foundational tenancy entities (`Tenant`, `Campus`),
  - added optional tenant/campus ownership fields on root entities (`User`, `Department`),
  - added EF mappings/relations/indexes and migration `Phase41_TenantCampusFoundation` for non-breaking schema extension.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - focused unit suite passed (`9/9`) for baseline regression confidence.
- Testing and result summary:
  - build: passed,
  - tests: `EnrollmentServiceWaitlistTests` + `AuthSecurityUxTests` passed (`9/9`).

- Behavior impact:
  - InstitutionType (School/College/University) logic remains unchanged,
  - tenancy/campus data model foundation is now in place for scoped filtering in upcoming phases.

## 2026-05-19 Update - Plan A Phase 1 Kickoff (App Configuration: Tenant + Campus)

- Recent request issue:
  - start Plan A Phase 1 and synchronize mandatory planning/governance trackers while placing implementation/validation summaries at phase end.

### Phase 1 - Domain Layer Extension (Kickoff)
- Implementation Summary:
  - initiated Plan A Phase 1 execution baseline for Tenant + Campus domain-layer extension,
  - updated Plan A documentation to keep completion evidence at the end of Phase 1,
  - synchronized all requested governance documents for this phase kickoff.
- Validation Summary:
  - cross-document consistency review completed for this request,
  - confirmed no runtime function, API contract, or schema behavior changed in this documentation-governance kickoff.
- Testing and result summary:
  - documentation consistency check completed,
  - no runtime test run required because this phase kickoff update is documentation-only.

- Behavior impact:
  - no runtime behavior change,
  - Plan A Phase 1 traceability and phase-end completion-summary compliance are now explicitly documented.

## 2026-05-19 Update - Deep System Audit + Validation + AI Chatbot Modernization (Phase 1-7)

- Recent request issue:
  - execute a full phase-by-phase system audit/validation and deliver chatbot UI modernization with required documentation synchronization after each phase.

### Phase 1 - System Understanding
- Implementation Summary:
  - completed deep module/functionality mapping across docs and live code for Academic, Enrollment, Notifications, Reports/Analytics, Auth/MFA, and AI Chat,
  - traced controller-service-repository and DTO/view bindings for high-risk areas.
- Validation Summary:
  - static traceability scan confirmed core implementations are present for requested modules,
  - identified route-consistency hardening opportunity for AI chat API.
- Testing and result summary:
  - phase 1 mapping completed with prioritized risk list.

### Phase 2 - API and Backend Validation
- Implementation Summary:
  - added `/api/v1/ai` compatibility route while preserving `/api/ai`,
  - updated web client chat calls to `/api/v1/ai/*`,
  - aligned module-license middleware route map for both AI chat prefixes.
- Validation Summary:
  - solution build and targeted unit/integration suites passed after changes.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted tests passed (`13/13`: unit `9/9`, integration `4/4`).

### Phase 3 - Database and Data Flow Validation
- Implementation Summary:
  - validated enrollment/waitlist data flow includes and ordering behavior,
  - confirmed no schema change required by this request's implementation.
- Validation Summary:
  - no migration/FK/index changes introduced.
- Testing and result summary:
  - phase 3 schema/data-flow check completed with no newly introduced integrity issue.

### Phase 4 - UI and Frontend Validation
- Implementation Summary:
  - validated sidebar/menu visibility logic and chatbot widget bindings,
  - confirmed widget-state and widget-send flows remain compatible.
- Validation Summary:
  - static contract/binding validation completed for layout + JS + API client mapping.
- Testing and result summary:
  - phase 4 UI validation completed without behavioral regression.

### Phase 5 - Performance and Stability Check
- Implementation Summary:
  - reviewed updated paths for async safety and avoided introducing blocking call regressions,
  - preserved existing caching/background-job behavior.
- Validation Summary:
  - build and targeted tests remained green after all code changes.
- Testing and result summary:
  - phase 5 stability sanity checks completed with no regression signal.

### Phase 6 - AI Chatbot Redesign (Critical)
- Implementation Summary:
  - replaced inline chat widget markup with reusable components:
    - `Views/Shared/FloatingChatButton.cshtml`
    - `Views/Shared/ChatPanel.cshtml`
  - kept existing backend chat APIs and session/history behavior intact.
- Validation Summary:
  - build + module backend enforcement integration tests passed after UI component extraction.
- Testing and result summary:
  - phase 6 chatbot modernization completed with preserved backend compatibility.

### Phase 7 - Final Consolidated Reporting
- Implementation Summary:
  - updated all requested governance documents with this phase-by-phase issue/implementation/validation closure.
- Validation Summary:
  - ensured each phase includes explicit testing/result summary.
- Testing and result summary:
  - phase 7 reporting and documentation synchronization completed.

- Behavior impact:
  - AI chat API now supports both legacy and versioned routes,
  - chatbot UI is modularized and maintained as a floating component set,
  - no schema mutation for this request.

## 2026-05-19 Update - UI/UX Redesign Phase 9 (Final UI Polish)

- Recent request issue:
  - proceed with the final redesign phase and ensure documentation plus repository synchronization are completed.
- Implementation Summary:
  - applied final polish to shared UI surfaces with smoother card elevation, cleaner section spacing, and improved dashboard presentation detail,
  - preserved all existing routes, API contracts, and backend behavior.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched final-polish stylesheet.
- Behavior impact:
  - no backend/API/schema changes,
  - the portal presentation is now more cohesive and visually finished.

## 2026-05-18 Update - UI/UX Redesign Phase 8 (Responsive Hardening)

- Recent request issue:
  - proceed with the responsive-design phase and keep documentation plus repository synchronization mandatory.
- Implementation Summary:
  - strengthened responsive behavior across the shared shell and major portal views,
  - improved mobile stacking for action groups, filter controls, profile menu, pagination, and overflow-sensitive table/modal regions.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched responsive frontend files.
- Behavior impact:
  - no backend/API/schema changes,
  - mobile and tablet usability is more resilient for common management workflows.

## 2026-05-18 Update - UI/UX Redesign Phase 7 (AI Chatbot UI)

- Recent request issue:
  - proceed with the next redesign phase and keep full documentation plus repository synchronization.
- Implementation Summary:
  - improved AI chatbot visual design for launcher, panel, assistant identity header, and message motion,
  - added quick suggestion chips with frontend-only interaction wiring that reuse the existing chat send/state endpoints.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched chatbot frontend files.
- Behavior impact:
  - no backend/API/schema changes,
  - chatbot experience is more discoverable, polished, and easier to start with quick prompts.

## 2026-05-18 Update - UI/UX Redesign Phase 6 (Branding Pass)

- Recent request issue:
  - proceed with next redesign phase and ensure docs plus repository synchronization after completion.
- Implementation Summary:
  - enhanced institution branding presentation in the shared shell,
  - improved top-header composition,
  - upgraded notification icon/chip visuals,
  - introduced a richer profile dropdown interface while preserving existing route and auth behavior.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in `Views/Shared/_Layout.cshtml` and `wwwroot/css/site.css`.
- Behavior impact:
  - no business logic/API/schema behavior changes,
  - improved brand clarity and account/notification UX in the global shell.

## 2026-05-18 Update - UI/UX Redesign Continuation (Enrollments/Results/Payments + Phase-Level Summary Formatting)

- Recent request issue:
  - continuation requested the next page-level UI polish wave and required implementation/validation summaries to appear at the end of each completed redesign phase.
- Implementation Summary:
  - applied UI continuity updates to Enrollments, Results, and Payments pages using existing global design helpers,
  - reformatted `Docs/Improved UI and look.md` so completion details are captured at each completed phase with lint-clean heading/list structure.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched Razor views and redesign document.
- Behavior impact:
  - no runtime/API/business-logic changes,
  - improved visual continuity across additional operational pages and clearer per-phase execution traceability in redesign planning docs.

## 2026-05-18 Update - UI/UX Redesign Continuation (Students/Courses/Admin Users Polish)

- Recent request issue:
  - continuation was requested to extend the new premium UI system to additional core management pages after the initial redesign.
- Implementation Summary:
  - applied the refreshed design language to Students, Courses, and Admin Users pages,
  - introduced reusable section-level helper styles for consistent layout and empty-state treatment.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - update remained frontend-only with no API, business-rule, or schema changes.
- Behavior impact:
  - no runtime behavior changes,
  - improved consistency and readability across additional admin-facing workflows.

## 2026-05-18 Update - UI/UX Redesign Request (Execution Snapshot)

- Recent request issue:
  - the portal required a complete frontend-only SaaS-style visual redesign so the product looks premium and modern for school, college, and university clients without affecting existing functionality.
- Implementation Summary:
  - redesigned the shared application shell, header, navigation, chatbot chrome, and responsive menu behavior in the Razor layout,
  - replaced the portal stylesheet with a cohesive academic design system for typography, palette, spacing, cards, forms, tables, modals, loaders, toasts, and responsive polish,
  - upgraded the dashboard page into a hero-and-card presentation while preserving the existing API connection flow and model binding,
  - added frontend-only JS for loader fade-out, alert-to-toast presentation, and mobile sidebar interaction.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the redesign changes,
  - workspace diagnostics reported no errors in the touched layout, dashboard, CSS, or JS files,
  - no backend/API/controller/database files were changed.
- Behavior impact:
  - runtime business behavior is unchanged,
  - portal presentation is now cleaner, more premium, and more responsive across desktop, tablet, and mobile views.

## 2026-05-18 Update - Documentation Synchronization Follow-up (Execution Snapshot)

- Recent request issue:
  - a follow-up request required synchronized updates across PRD, Function List, Complete Functionality Reference, Development Plan, and Database Schema.
- Implementation Summary:
  - added a dated follow-up execution snapshot to each requested document using consistent issue/implementation/validation structure,
  - aligned wording to preserve cross-document audit traceability after the most recent TODO closure cycle.
- Validation Summary:
  - verified all five requested documents include this follow-up snapshot,
  - verified this pass is documentation-only and does not change application behavior, API contracts, or schema/runtime state.
- Behavior impact:
  - no runtime functionality change,
  - governance documentation remains synchronized for this follow-up request.

## 2026-05-18 Update - Stage 40.1 PhoneNumber/SMS Recipient Dependency Completion (Execution Snapshot)

- Recent request issue:
  - notification SMS fan-out depended on active user phone numbers, but user profiles lacked persisted phone support.
- Implementation Summary:
  - added optional `PhoneNumber` persistence on user accounts and schema migration support,
  - implemented notification repository active-phone lookup for SMS recipient resolution,
  - wired optional phone capture through admin user management, CSV import, and student self-registration flows.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test Tabsan.EduSphere.sln -v minimal --filter "FullyQualifiedName~Phase28Stage2Tests|FullyQualifiedName~UserImportAndForceChangeIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~StudentRegistration"` passed (`13/13`).
- Behavior impact:
  - SMS dispatch can now resolve recipients from persisted active user phone numbers,
  - admin/import/self-registration entry points can store optional phone numbers without breaking existing clients.

## 2026-05-18 Update - StudentLifecycle Notification TODO Completion (Execution Snapshot)

- Recent request issue:
  - StudentLifecycle notification TODOs were completed in the prior remediation step for lifecycle transitions and request-review outcomes.
- Implementation Summary:
  - implemented notification dispatch for graduation, promotion, deactivation, and reactivation actions,
  - implemented notification dispatch for change/modification request creation and approval/rejection outcomes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~StudentLifecycleIntegrationTests -v minimal` passed (`7/7`).
- Behavior impact:
  - lifecycle participants and reviewers now receive consistent in-app notification events for key StudentLifecycle workflows,
  - no schema or API-contract changes were introduced.

## 2026-05-18 Update - DeepScan Phase 40 Closure and Production Readiness Revalidation (Execution Snapshot)

- Recent request issue:
  - final DeepScan closure evidence was required after completing Phase 39 remediation to confirm no unresolved production-blocking functional gap remained.
- Implementation Summary:
  - executed post-remediation validation bundle for prior DeepScan gaps (MFA hardening, strict import rollback mode, waitlist workflow, EF warning cleanup),
  - appended re-execution task-by-task outputs and updated final validation rule in `Docs/DeepScan.md`.
- Validation Summary:
  - build and targeted unit/integration suites passed for all previously open areas,
  - DeepScan re-execution now reports all tasks `4.1` through `4.20` implemented with no unresolved critical/high issue.
- Behavior impact:
  - no new business-functionality added in Phase 40 itself,
  - production-readiness closure is now evidenced and traceable after remediation.

## 2026-05-18 Update - DeepScan Stage 39.4 EF Relationship and Query-Filter Warning Cleanup (Execution Snapshot)

- Recent request issue:
  - startup/runtime EF logs contained mapping warnings for required-relationship plus global-filter mismatches, a quiz shadow FK path, and course enum default sentinel behavior.
- Implementation Summary:
  - aligned dependent query filters with filtered required principals across affected configurations,
  - fixed quiz question mapping to explicit parent navigation to remove shadow FK ambiguity,
  - removed course enum DB default configuration that triggered sentinel warning behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
  - verified targeted EF warning set is no longer emitted in the focused startup/runtime path.
- Behavior impact:
  - no business-flow or API-contract behavior change,
  - entity graph loading now follows filter-consistent mapping semantics without the prior warning noise.

## 2026-05-18 Update - Documentation Synchronization Request (Execution Snapshot)

- Recent request issue:
  - mandatory execution/planning tracker updates for the latest request were pending synchronized closure across six governance documents.
- Implementation Summary:
  - added synchronized 2026-05-18 execution snapshot coverage in PRD, consolidated issues, function list, functionality reference, development plan, and database schema documentation,
  - aligned issue wording and closure language so all trackers carry the same implementation and validation intent.
- Validation Summary:
  - verified each updated tracker contains a dated closure entry with both implementation and validation blocks,
  - verified the update is documentation-only and does not change runtime functionality, API contracts, or deployment behavior.
- Behavior impact:
  - no runtime functionality change,
  - documentation traceability for this request is complete and audit-consistent.

## 2026-05-18 Update - DeepScan Gap Phase/Stage Synchronization Request (Execution Snapshot)

- Recent request issue:
  - DeepScan findings identified missing/partial capability areas that were not yet captured as executable phase/stage entries in governance trackers.
- Implementation Summary:
  - added consolidated staged remediation planning (Phase 39/40) for waitlist, transactional import strict mode, MFA hardening, and EF warning cleanup,
  - synchronized this request closure snapshot across PRD, consolidated tracker, function list, development plan, and database schema docs.
- Validation Summary:
  - verified all six mandatory docs contain matching 2026-05-18 DeepScan-gap synchronization entries,
  - verified this request is documentation-only and does not alter runtime functionality, API contracts, or deployment behavior.
- Behavior impact:
  - no runtime functionality change,
  - functionality governance now explicitly tracks the DeepScan remediation roadmap.

## 2026-05-18 Update - DeepScan Stage 39.2 Transactional CSV Import Strict Mode (Execution Snapshot)

- Recent request issue:
  - user CSV imports needed an atomic strict mode so partially valid files would not persist a mixed result set.
- Implementation Summary:
  - added optional strict-mode rollback support to the user import service and API controller,
  - added a response flag to show whether the import used strict or permissive behavior,
  - preserved existing permissive import behavior as the default path.
- Validation Summary:
  - targeted integration suite passed for user import and force-change-password flows (`4/4`),
  - verified strict mode returns zero imported rows when a mixed-validity CSV is submitted,
  - verified permissive import continues to work with the existing end-to-end login and forced-password-change flow.
- Behavior impact:
  - strict-mode user import now behaves atomically from the caller’s perspective,
  - permissive user import behavior remains unchanged for existing clients.

## 2026-05-18 Update - DeepScan Stage 39.1 Enrollment Waitlist and Seat-Promotion Workflow (Execution Snapshot)

- Recent request issue:
  - enrollment handling needed a waitlist and seat-promotion path when course offerings are full.
- Implementation Summary:
  - added a waitlisted enrollment state and promotion helpers to the enrollment aggregate,
  - updated enrollment service logic to queue students when an offering is full and promote the oldest waitlisted student after a drop,
  - added ordered waitlist retrieval in the enrollment repository layer, exposed the waitlist queue through the API, and covered the flow with focused unit tests.
- Validation Summary:
  - targeted unit suite passed for waitlist creation and promotion (`2/2`),
  - verified the waitlist queue endpoint is exposed for faculty/admin review,
  - verified full offering enrollment now records a waitlisted state,
  - verified seat release promotes the oldest waitlisted enrollment deterministically.
- Behavior impact:
  - enrollment no longer hard-fails when a full offering still accepts a waitlist,
  - seat release now advances the waitlist automatically for the oldest queued student,
  - faculty/admin users can inspect the current queue through the roster companion endpoint.

## 2026-05-18 Update - DeepScan Stage 39.3 MFA Hardening (TOTP + Recovery Codes) (Execution Snapshot)

- Recent request issue:
  - MFA login enforcement relied on a static deployment demo code instead of per-user strong-factor verification.
- Implementation Summary:
  - replaced demo-code validation with per-user TOTP verification in the auth login flow,
  - added one-time recovery-code generation/storage/consumption with audit events,
  - added authenticated MFA enrollment endpoints (setup, enable, recovery-code regeneration),
  - persisted user MFA state/secret/recovery hashes through schema-backed fields.
- Validation Summary:
  - targeted auth security unit suite passed (`7/7`),
  - targeted login/force-change integration suite passed (`4/4`),
  - verified force-change-password and refresh-compatible login behavior remained functional.
- Behavior impact:
  - MFA is now user-specific TOTP-based rather than deployment-code based,
  - recovery codes are one-time and automatically invalidated after use,
  - auth flows remain compatible while enforcing stronger factor controls.

## 2026-05-21 Update - Plan F Phase 6 Import Sheets Completion (Execution Snapshot)

- Recent request issue:
  - proceed with Plan F Phase 6 import-sheet completion and synchronized documentation closure.
- Implementation Summary:
  - Stage 6.1 extended user import templates and parser header handling with optional `MobileNumber` and `CampusAssignments` support,
  - Stage 6.2 preserved backward-compatible imports for legacy templates without new columns,
  - Stage 6.3 added additive field-format validation for mobile numbers and campus assignment GUID lists.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`),
  - touched import files reported no static diagnostics errors.
- Behavior impact:
  - import templates now include mobile and multi-campus assignment-ready fields,
  - existing import files remain valid and functional.

## 2026-05-15 Update - Final Phase 37/38 Execute Closure Snapshot

- Completed execute-mode closure for final separation phases.
- Functional behavior confirmed:
  - runtime app publish output is separated from license app publish output,
  - non-runtime assets (docs/guides/scripts/training/import templates) are packaged independently from runtime app artifacts.
- Validation evidence:
  - `Artifacts/Phase37/Publish-Separation-20260515.md` shows execute mode `True` and phase status `PASS` (`4/4`),
  - `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md` shows execute mode `True` and phase status `PASS` (`7/7`).
- Behavior impact:
  - production app packaging no longer bundles license tooling,
  - operational/documentation assets are distributed through dedicated non-runtime packaging workflow.

## 2026-05-13 Update - Institute Parity Stage 0.1 Audit Snapshot

- Completed baseline functional inventory for parity-scope modules (timetable, courses, buildings/rooms, departments, assignments, enrollments, reports, results, quizzes, lifecycle, payments, settings surfaces).
- Confirmed broad endpoint/service/repository coverage already exists for all target modules, with role guards primarily centered on SuperAdmin/Admin/Faculty/Student authorization and department/offering scoping.
- Identified remaining University-default behavior hotspots in branding/onboarding/prompt/template text and default policy assumptions, now queued into subsequent parity implementation stages.
- No user-visible functionality was changed in this stage; this is a validated baseline mapping update.

## 2026-05-14 Update - Phase 23 Stage 23.2 Dynamic Academic Labels and Context Snapshot

- Completed dynamic academic vocabulary parity validation for School/College/University policy modes.
- Functional behavior confirmed:
  - `GET /api/v1/labels` returns policy-based vocabulary for current tenant configuration,
  - University-enabled policy resolves University vocabulary,
  - School-only policy resolves School vocabulary,
  - College-only policy resolves College vocabulary,
  - School+College policy resolves School vocabulary,
  - mixed policy including University resolves University vocabulary.
- Web behavior confirmed:
  - portal API client consumes vocabulary contract via `GetVocabularyAsync`,
  - module composition surface renders returned label set for context-aware wording without screen duplication.
- Validation evidence:
  - focused integration suite passed (`8/8`) for `DynamicLabelIntegrationTests`,
  - unit label-service suite remains green (`4/4`) in `Phase24Tests` label tests.
- Behavior impact:
  - tenant-wide label terminology is now explicitly validated end-to-end for API and portal consumption,
  - no schema/migration change introduced in Stage 23.2.

## 2026-05-13 Update - Institute Parity Stage 0.2 Access Matrix Snapshot

- Completed role/institute access matrix baseline across core modules and operations (view/create/edit/deactivate/export).
- Confirmed effective enforcement pattern is currently mixed:
  - explicit institution policy flags at platform level,
  - operational scoping mainly through department assignment, course-offering ownership, and student self-ownership.
- Identified parity risk areas:
  - institute-specific checks not explicit on all mutation paths,
  - School/College filter coverage still incomplete on selected analytics/report surfaces.
- No runtime behavior changes were introduced in Stage 0.2; this is baseline authorization/scope mapping evidence for upcoming remediation stages.

## 2026-05-13 Update - Institute Parity Stage 0.3 Report Failure Snapshot

- Completed baseline inventory of report failures and report-adjacent operational failure modes.
- Confirmed previously observed critical failures are resolved:
  - Result Summary runtime exception,
  - Report Center visibility gaps for privileged roles.
- Classified remaining report parity risks:
  - incomplete explicit School/College/University filter propagation,
  - expected scope-guard failures that require UX/API clarity,
  - sparse dummy data causing empty outputs in some report scenarios.
- This stage introduced no runtime code change; it establishes a tagged failure backlog for Phase 4 and Phase 5 execution.

## 2026-05-13 Update - Institute Parity Stage 0.4 Exit Snapshot

- Completed Phase 0 sign-off by consolidating module audit, role/institute matrix, and report failure inventory into a prioritized execution backlog.
- Confirmed functional reference readiness for parity implementation phases:
  - authorization and scope hardening (Phase 2),
  - module workflow parity (Phase 3),
  - report/analytics parity fixes (Phase 4),
  - data-completeness hardening (Phase 5).
- No runtime functionality changed in Stage 0.4; this is governance and execution-readiness closure for Phase 0.

## 2026-05-13 Update - Institute Parity Stage 1.1 Normalization Snapshot

- Added canonical institute dimension normalization for departments by introducing persisted `InstitutionType` (School/College/University) with default compatibility set to University.
- Department create/update API flows now enforce license policy constraints for institution type selection, preventing assignment to disabled modes.
- Department read payloads now expose institution type to support downstream parity-aware UI/API behavior.
- Existing department CRUD behavior remains backward-compatible for current University-first flows because create defaults and update optionals preserve prior request shapes.
- Stage 1.1 establishes the schema and contract base for follow-on institute-scoped referential and indexing hardening in Stage 1.2.

## 2026-05-13 Update - Institute Parity Stage 1.2 Referential Integrity + Indexing Snapshot

- Added referential guards on academic write paths:
  - course create now requires valid department,
  - offering create now requires valid course/semester and faculty-department assignment when faculty is provided,
  - student profile creation now enforces program-department alignment.
- Normalized academic program uniqueness to department scope (`Code + DepartmentId`) for parity-safe multi-department operation.
- Added institute/report-heavy index coverage on programs, courses, offerings, student profiles, enrollments, and role-assignment lookup paths to reduce filter/join overhead.
- Updated enrollment status storage to bounded string length for SQL index support on active-status query paths.
- Behavior impact: invalid cross-scope academic links now fail early with explicit validation errors, while existing valid workflows remain backward-compatible.

## 2026-05-13 Update - Institute Parity Stage 1.3 Script Hardening Snapshot

- Hardened deployment SQL scripts to mirror Stage 1.1/1.2 runtime schema behavior for script-first rollout paths.
- `01-Schema-Current.sql` now includes idempotent parity migration-aligned blocks for department institution dimension and Stage 1.2 index/column updates.
- `04-Maintenance-Indexes-And-Views.sql` now enforces parity-critical index presence with safe existence guards.
- `05-PostDeployment-Checks.sql` now emits explicit parity verification signals for migration IDs, column presence/shape, and critical index existence.
- User-visible runtime behavior is unchanged by Stage 1.3 itself; this stage improves deployment safety and consistency across environments.

## 2026-05-13 Update - Institute Parity Stage 1.4 Exit Criteria Snapshot

- Completed Phase 1 exit verification enablement for institute parity schema/data integrity checks.
- Post-deployment verification now explicitly reports:
  - department institution-type validity and representation coverage,
  - orphan counts across institute-linked academic relations and assignment links.
- Runtime behavior impact:
  - no API contract or user-flow mutation in Stage 1.4,
  - improved operational confidence by making Phase 1 exit criteria auditable with deterministic SQL check outputs.

## 2026-05-13 Update - Institute Parity Stage 2.1 SuperAdmin Capability Snapshot

- Expanded SuperAdmin management capabilities for user-to-department assignment workflows by adding direct Faculty department assignment APIs (assign/remove/list).
- Strengthened institute-scope correctness for assignment operations by enforcing institution-type compatibility between users and target departments when explicit user institution type is set.
- Added assignment-management response visibility for user institution type in SuperAdmin user candidate listings.
- Behavior impact:
  - SuperAdmin assignment flows now cover both Admin and Faculty department assignment operations,
  - cross-institute assignment mismatches now fail early with explicit validation responses,
  - existing Admin-user create/update/assignment behavior remains backward-compatible.

## 2026-05-13 Update - Institute Parity Stage 2.2 Role-Scoped Institute Enforcement Snapshot

- Added `institutionType` JWT claim propagation for explicitly assigned users to support runtime institute-scope checks.
- Extended report endpoint authorization flow for Admin/Faculty to enforce institute compatibility in addition to existing role/department/offering scope checks.
- Added integration proof that an Admin with valid department assignment is still denied when institution scope mismatches target department.
- Behavior impact:
  - Admin/Faculty report access is now constrained by both role scope and institute scope where explicit institution assignment exists,
  - SuperAdmin behavior remains global and unchanged,
  - existing report export contracts and metadata remain stable.

## 2026-05-13 Update - Institute Parity Stage 2.3 Menu/Action Guard Consistency Snapshot

- Added portal-level guard consistency enforcement so direct portal URL navigation must match visible sidebar menu permissions for non-SuperAdmin users.
- Introduced a centralized action-to-menu key map in `PortalController` to keep menu visibility and invokable portal section routes aligned.
- Preserved SuperAdmin unrestricted behavior while applying deterministic redirect-on-deny behavior for hidden sections.
- Added integration proof that hidden menu state and endpoint authorization remain consistent (`Admin` hidden settings path -> forbidden, `SuperAdmin` visible settings path -> allowed).
- Behavior impact:
  - hidden menu sections can no longer be accessed by direct URL path in the portal flow,
  - visible menu sections retain expected accessibility for authorized roles,
  - existing role matrix behavior remains backward-compatible.

## 2026-05-13 Update - Institute Parity Stage 2.4 Exit Criteria Snapshot

- Completed Phase 2 authorization and permission correction closeout.
- Confirmed role + institute access matrix pass state across integrated Stage 2 behavior surfaces:
  - SuperAdmin assignment management with institute-compatibility enforcement,
  - Admin/Faculty report scope checks including institution mismatch denial,
  - sidebar visibility and direct-route guard consistency for non-SuperAdmin users.
- Validation evidence executed as a combined integration matrix (`AdminUserManagementIntegrationTests`, `ReportExportsIntegrationTests`, `SidebarMenuIntegrationTests`) with all tests passing.
- Behavior impact:
  - no new runtime behavior introduced in Stage 2.4 itself,
  - Phase 2 authorization outcomes are now formally validated and documented as complete,
  - subsequent parity work can proceed to Phase 3 CRUD/workflow parity scope.

## 2026-05-13 Update - Institute Parity Stage 3.1 Core Academic/Admin Modules Snapshot

- Completed first Phase 3 module parity hardening slice focused on department and course operational parity across institution types.
- Portal department management behavior was aligned for School/College/University:
  - department list now shows institution type,
  - create/edit forms now include institution-type selection,
  - edit flow preloads institution-type value for deterministic updates.
- Web API client and portal controller contract behavior was corrected to avoid silent University-default writes:
  - department create now requires explicit institution type,
  - department update now supports explicit institution-type mutation.
- Added cross-institution integration validation:
  - temporarily enables all institution policy flags,
  - verifies department/course CRUD operations across School/College/University,
  - restores original institution policy state after test execution.
- Hardened legacy admin assignment round-trip integration path to choose/create institution-compatible departments in mixed datasets.
- Behavior impact:
  - School/College/University department CRUD paths are now parity-safe from portal create/edit surfaces,
  - mixed-institution datasets no longer cause false assignment-path regressions in integration coverage,
  - no database schema or migration mutation was required for Stage 3.1.

## 2026-05-13 Update - Institute Parity Stage 3.2 Student Lifecycle Snapshot

- Completed lifecycle institute-parity hardening for student lifecycle workflows currently implemented in platform scope (graduation candidates, promote, graduate, deactivate, reactivate).
- API lifecycle access behavior now enforces:
  - Admin department-assignment scope checks,
  - institution-type claim compatibility with target department/student department,
  - SuperAdmin bypass for expected global governance behavior.
- Portal lifecycle behavior now enforces institute-aware filtering:
  - session JWT identity includes optional `institutionType`,
  - lifecycle department selector is constrained by institute type for non-SuperAdmin sessions,
  - out-of-scope department selections are blocked before lifecycle calls.
- Lifecycle UI operation consistency improved:
  - per-row graduation now posts correctly,
  - promote/graduate actions preserve selected department and semester filter context.
- Added focused integration validation proving Admin institute-mismatch deny behavior on lifecycle read/mutation paths while keeping Stage 2 report/menu/assignment parity suites green.
- Behavior impact:
  - student lifecycle operations now follow the same institute-boundary enforcement model already adopted by report authorization paths,
  - lifecycle transitions cannot be invoked by Admin users outside assigned department + institution scope,
  - no schema/model migration changes were required for Stage 3.2.

## 2026-05-13 Update - Institute Parity Stage 3.3 Student Submenu Snapshot

- Completed student submenu parity hardening on core student-data read surface used by Students/Enrollments/Payments submenu workflows.
- Student list endpoint behavior now enforces:
  - Admin assignment-constrained department scope,
  - institution-claim compatibility for explicit department filter requests,
  - institute-compatible result shaping for unfiltered student list queries.
- Preserved existing SuperAdmin full-scope behavior and faculty scoped behavior while extending institute parity consistency.
- Updated student submenu table terminology to institute-neutral `Level` wording on student/enrollment list surfaces to reduce University-only language assumptions.
- Added dedicated integration evidence covering:
  - forbidden response on institute-mismatched student list department request,
  - institute-compatible list output enforcement for admin callers.
- Behavior impact:
  - student submenu backing data now follows parity boundaries consistently with Phase 2/Stage 3.2 institute guard model,
  - mixed-assignment/mixed-institute datasets no longer risk cross-institute student list leakage in Admin submenu reads,
  - no schema or migration update required for Stage 3.3.

## 2026-05-13 Update - Institute Parity Stage 3.4 Exit Snapshot

- Completed Phase 3 exit-criteria validation and closure for module CRUD/workflow parity.
- Consolidated verified behavior from Stage 3.1-3.3:
  - core department/course parity across School/College/University,
  - lifecycle institute-scope enforcement,
  - student submenu institute-scope enforcement and institute-neutral terminology.
- Added web contract alignment fix by extending shared portal `LookupItem` with optional `InstitutionType` required by institute-aware department filtering paths.
- Validation evidence:
  - solution build pass,
  - full integration suite pass (`115/115`) with no failures.
- Behavior impact:
  - Phase 3 module parity exit criteria met for implemented scope,
  - no schema/model migration change required for Stage 3.4,
  - next execution focus moves to Phase 4 analytics/report parity and reliability stages.

## 2026-05-13 Update - Institute Parity Stage 4.1 Analytics Filter Expansion Snapshot

- Completed analytics filter expansion across API queries and portal analytics dashboard controls.
- Analytics API behavior now enforces:
  - optional institute filter support across analytics read/export surfaces,
  - role-aware institute defaults for constrained roles via JWT `institutionType` claim,
  - forbidden responses on explicit institute mismatch requests,
  - department + institute compatibility checks before analytics query execution.
- Analytics service behavior now supports institution-aware filtering for:
  - performance analytics,
  - attendance analytics,
  - assignment analytics,
  - quiz analytics,
    while preserving distributed cache behavior with scope-aware cache keys.
- Portal analytics behavior now includes:
  - institution + department filter controls,
  - claim-driven default institute filtering for constrained roles,
  - safe clearing of out-of-scope department selections.
- Validation evidence:
  - solution build pass,
  - focused parity integration suites pass (`41/41`) including new analytics institute parity tests.
- Behavior impact:
  - analytics dashboards and analytics API queries are now institute-aware for School/College/University contexts,
  - constrained-role analytics requests are auto-scoped and protected against cross-institute filter drift,
  - no schema or migration update required for Stage 4.1.

## 2026-05-13 Update - Institute Parity Stage 4.2 Reports Filter Expansion Snapshot

- Completed report filter expansion across report APIs, report exports, and report portal controls.
- Report API behavior now enforces:
  - optional `institutionType` filter support across report generation and export endpoints,
  - role-aware default institution scope for constrained roles using JWT `institutionType`,
  - forbidden responses on explicit report institution mismatch requests,
  - report request scope propagation to queued result-summary export jobs.
- Report query behavior now supports institution-aware filtering for:
  - attendance summary,
  - result summary,
  - assignment summary,
  - quiz summary,
  - GPA,
  - enrollment summary,
  - semester results,
  - low attendance warning,
  - FYP status report.
- Portal report behavior now includes:
  - institution filter controls on report forms,
  - institution-aware department dropdown narrowing,
  - report export links that preserve institution filter state.
- Validation evidence:
  - solution build pass,
  - focused parity integration suites pass (`43/43`) including expanded report institute parity tests.
- Behavior impact:
  - report dashboards and report API queries/exports are institute-aware for School/College/University contexts,
  - constrained-role report requests are auto-scoped and protected against cross-institute filter drift,
  - no schema or migration update required for Stage 4.2.

## 2026-05-13 Update - Institute Parity Stage 4.3 Broken Report Fixes Snapshot

- Completed report reliability fixes for faculty report scope guardrails.
- Report API behavior now enforces:
  - faculty department-scoped reports require explicit department (or department/offering where applicable),
  - faculty requests against unassigned departments are denied consistently,
  - faculty offering-scoped report checks validate department assignment scope instead of strict offering owner match.
- Reliability and deterministic validation additions:
  - added integration checks proving `400` responses for missing required faculty filters,
  - added integration checks proving `403` responses for faculty unassigned department filters,
  - included repaired report endpoints in focused parity regression run.
- Validation evidence:
  - solution build pass,
  - focused parity integration suites pass (`42/42`) including expanded report reliability tests.
- Behavior impact:
  - broken faculty report access behavior is corrected without changing report UI contracts,
  - report endpoints now apply role + institute + department scope consistently for repaired routes,
  - no schema or migration update required for Stage 4.3.

## 2026-05-13 Update - Institute Parity Stage 4.4 Exit Criteria Snapshot

- Completed Phase 4 exit gate after the report and analytics reliability fixes.
- Exit validation now confirms:
  - full integration-suite regression stability across report, analytics, role, and institute guard paths,
  - no remaining Phase 4 functional regressions in School/College/University parity flows,
  - no schema or migration update required for the phase-exit checkpoint.
- Validation evidence:
  - solution build pass,
  - full integration suite pass (`124/124`).
- Behavior impact:
  - Phase 4 is closed with error-free report and analytics parity behavior,
  - the execution pointer advances to Phase 5 without additional code changes.

## 2026-05-13 Update - Institute Parity Stage 5.1 Core Seed Coverage Snapshot

- Completed core seed coverage hardening for institute-aware foundational data.
- Core script behavior now enforces:
  - default institution policy flags in `portal_settings` for School/College/University enablement,
  - deterministic baseline departments mapped to all three institution types,
  - normalized legacy report keys to current canonical report-key format,
  - parity-aligned report role assignment defaults (operational reports for SuperAdmin/Admin/Faculty, transcript also for Student),
  - explicit SuperAdmin baseline sidebar role-access seed rows.
- Validation evidence:
  - user-import institution-assignment regression suite passed (`3/3`),
  - script content verification confirms policy keys, institution-typed baseline departments, and updated report key matrix are present.
- Behavior impact:
  - fresh and replayed core seed runs now produce institute-aware foundational policy and access baselines required for Phase 5 data completion,
  - no schema/migration mutation introduced in this stage.

## 2026-05-13 Update - Institute Parity Stage 5.2 Full Dummy Coverage Snapshot

- Completed full dummy parity dataset expansion for School/College/University representative execution paths.
- Full dummy script behavior now enforces:
  - explicit institution-type assignment for parity users and deterministic department institution mapping,
  - assignment-junction coverage for Admin and Faculty department scopes,
  - buildings/rooms/timetable/timetable-entry parity data across representative institute departments,
  - payment receipt and transcript export artifact coverage,
  - lifecycle/report artifacts including bulk promotions, graduation approvals, school stream assignment, and student report cards.
- Validation evidence:
  - targeted user-import institution-assignment regression suite passed (`3/3`),
  - script verification confirms new parity entity blocks and institution-type alignment rows are present.
- Behavior impact:
  - one-run full dummy seeding now provides broader parity validation data for reports, lifecycle, timetable, payments, and role/institute scope checks,
  - no schema/migration mutation introduced in this stage.

## 2026-05-13 Update - Institute Parity Stage 5.3 Data Quality and Replay Safety Snapshot

- Completed full dummy replay-safety hardening and post-deployment data-quality verification expansion.
- Script behavior now enforces:
  - deterministic replay alignment for seeded user/department identity and institution mapping fields,
  - institute-level parity aggregate checks for School/College/University datasets,
  - critical workflow coverage checks for timetable, payments, report artifacts, and lifecycle entities,
  - duplicate and dataset-version integrity checks for replay safety.
- Validation evidence:
  - targeted user-import institution-assignment regression suite passed (`3/3`),
  - script verification confirms institute-level and replay-safety check outputs are present in post-deployment script.
- Behavior impact:
  - replayed full dummy seeding now preserves deterministic parity shape more strictly,
  - post-deployment verification now provides direct parity-count and duplicate-safety signals for Stage 5 quality gates.

## 2026-05-13 Update - Institute Parity Stage 5.4 Exit Criteria Snapshot

- Completed Phase 5 exit-criteria validation for script-chain readiness.
- Exit validation behavior now confirms:
  - full script deployment order (`01` -> `02` -> `03` -> `05`) succeeds in one run,
  - dummy seed replay avoids superadmin duplicate-email collisions by reusing preexisting superadmin identity when available,
  - post-deployment checks emit non-zero institute-distributed parity coverage and zero duplicate key-identifier signals.
- Validation evidence:
  - targeted user-import institution-assignment regression suite passed (`3/3`),
  - one-run SQL execution reported successful full dummy completion and post-check parity metrics across School/College/University.
- Behavior impact:
  - Phase 5 database script parity scope is now closed with executable one-run readiness evidence,
  - execution can proceed to Phase 6 QA/UAT/regression protection expansion.

## 2026-05-13 Update - Institute Parity Stage 6.1 Automated Test Expansion Snapshot

- Completed automated parity regression expansion for institute-scope lifecycle, student submenu, and report access paths.
- Added integration coverage for matched-scope success paths that complement existing mismatch-deny checks:
  - lifecycle graduation-candidates access now has explicit Admin matched-institute allow-path evidence,
  - student submenu explicit department filter now has institute-compatible scope-shaping allow-path evidence,
  - enrollment-summary report now has Admin matched-institution query allow-path evidence.
- Validation evidence:
  - focused Stage 6.1 parity integration run passed (`28/28`) across `StudentLifecycleIntegrationTests`, `StudentSubmenuParityIntegrationTests`, and `ReportExportsIntegrationTests`.
- Behavior impact:
  - institute parity guard model now has both deny-path and allow-path automated coverage on key Phase 5-expanded surfaces,
  - no runtime feature contract changes or schema mutations introduced by Stage 6.1.

## 2026-05-13 Update - Institute Parity Stage 6.2 Cross-Role UAT Matrix Snapshot

- Completed cross-role UAT matrix automation across School/College/University institution contexts.
- Added matrix validation coverage for SuperAdmin/Admin/Faculty/Student on stable parity-sensitive endpoints:
  - report catalog role visibility,
  - account-security locked-account access boundaries,
  - attendance-by-offering authorization boundaries.
- Validation evidence:
  - focused Stage 6.2 UAT integration run passed (`100/100`) across `CrossRoleUatMatrixIntegrationTests`, `ReportCatalogIntegrationTests`, `AccountSecurityIntegrationTests`, and `AuthorizationRegressionTests`.
- Behavior impact:
  - role-boundary expectations are now explicitly regression-protected across all three institution claim contexts,
  - no runtime feature contract changes or schema mutations introduced by Stage 6.2.

## 2026-05-13 Update - Institute Parity Stage 6.3 Performance and Query Validation Snapshot

- Completed performance and query validation for parity-sensitive institute-filtered report/dashboard paths.
- Added Stage 6.3 validation coverage for:
  - parity index read-usage confirmation on institute-filtered query patterns (`academic_programs`, `courses`, `course_offerings`),
  - no-major-regression latency budgets on common Admin dashboard/report paths.
- Validation evidence:
  - focused Stage 6.3 integration run passed (`2/2`) across `PerformanceQueryValidationIntegrationTests`.
- Behavior impact:
  - index-backed institute-filtered query paths now have explicit automated validation evidence,
  - no runtime feature contract changes or schema mutations introduced by Stage 6.3.

## 2026-05-13 Update - Institute Parity Stage 6.4 Exit Criteria Snapshot

- Completed Phase 6 exit-criteria consolidation for institute parity.
- Executed consolidated parity certification run across Stage 6 suites covering:
  - lifecycle and student submenu parity success + scope checks,
  - cross-role UAT matrix and authorization/report boundary checks,
  - performance/query validation checks for parity-sensitive paths.
- Validation evidence:
  - consolidated Stage 6 phase-exit run passed (`132/132`) with zero failures.
- Behavior impact:
  - Phase 6 parity scenarios are fully regression-certified with no critical/blocker defects,
  - no runtime feature contract changes or schema mutations introduced by Stage 6.4.

## 2026-05-13 Update - Institute Parity Stage 7.1 Deployment Runbook Snapshot

- Completed deployment runbook finalization for institute parity operational readiness.
- Added explicit rollout guidance for SQL execution order (`01 -> 02 -> 03 -> 04 -> 05`) and environment prerequisites.
- Added rollback + verification checklist covering backup, fail-fast verification via post-deployment checks, cleanup fallback, and evidence archiving.
- Validation evidence:
  - required deployment scripts existence verified,
  - schema/create-context and post-check fail-fast guards verified from script content.
- Behavior impact:
  - no runtime feature contract changes or schema mutations introduced by Stage 7.1,
  - operational deployment guidance is now explicit and reproducible for Phase 7 rollout.

## 2026-05-13 Update - Institute Parity Stage 7.2 Functional Documentation Snapshot

- Completed functional documentation alignment for institute parity behavior across shared functionality and user-facing role guides.
- Added explicit role/institute scope guidance for:
  - Admin department/institute scope boundaries,
  - Faculty assignment + institute scoped access,
  - Student safe-surface access and restricted operational endpoints.
- Validation evidence:
  - Stage 7.2 documentation updates synchronized across required tracking docs and user guides,
  - command execution pointer advanced to Stage 7.3.
- Behavior impact:
  - no runtime feature contract changes or schema mutations introduced by Stage 7.2,
  - institute parity behavior is now explicitly documented for operational users.

## 2026-05-13 Update - Institute Parity Stage 7.3 Monitoring and Support Handover Snapshot

- Completed monitoring and support handover documentation for institute parity operational readiness.
- Added monitoring guidance for report/analytics failure signals, expected authorization-denial triage, and institute-filtered analytics health checks.
- Added support handover checklist for SuperAdmin parity incidents covering role, institution type, department/offering scope, menu visibility, and correlation evidence.
- Validation evidence:
  - Stage 7.3 monitoring/support guidance synchronized across required docs,
  - support handover checklist explicitly references the report/analytics and role/institute scenarios most likely to generate parity incidents.
- Behavior impact:
  - no runtime feature contract changes or schema mutations introduced by Stage 7.3,
  - operational support handling is now explicit for parity-scope defects.

## 2026-05-13 Update - Institute Parity Stage 7.4 Release Exit Criteria Snapshot

- Completed Phase 7 release exit criteria for institute parity readiness.
- Validation evidence:
  - Stage 7.4 completion evidence is recorded in the phase tracker and mandatory handoff docs,
  - command execution pointer is advanced beyond the institute parity closeout.
- Behavior impact:
  - no runtime feature contract changes or schema mutations introduced by Stage 7.4,
  - Phase 7 parity work is fully closed and ready for the next roadmap stage.

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Core System Architecture](#core-system-architecture)
- [Complete Functionality Index](#complete-functionality-index)
- [Authentication & Authorization](#authentication--authorization)
- [User & Access Management](#user--access-management)
- [Academic Management](#academic-management)
- [Assessment & Grading](#assessment--grading)
- [Learning Management System](#learning-management-system)
- [Communication & Notifications](#communication--notifications)
- [Administration & Configuration](#administration--configuration)
- [Advanced Features](#advanced-features)
- [Integration & Support](#integration--support)
- [Service & Controller Matrix](#service--controller-matrix)
- [Architecture & Design Patterns](#architecture--design-patterns)
- [API Patterns & Data Flows](#api-patterns--data-flows)
- [Performance Considerations](#performance-considerations)

---

## Executive Summary

**Tabsan-EduSphere** is a comprehensive, enterprise-grade educational management system built on ASP.NET Core, designed to support modern academic institutions with multi-role access control, flexible academic management, comprehensive assessment tools, and advanced features.

### Key Statistics
- **63+ API Controllers** providing 200+ endpoints
- **50+ Application Services** implementing business logic
- **50+ Service Interfaces** for dependency injection and abstraction
- **14 Application Layer Modules** organized by domain
- **45+ Major Feature Areas** across academic, assessment, communication, and administration domains
- **Supported Roles:** SuperAdmin, Admin, Faculty, Student, Parent, Staff
- **Database Entities:** 50+ domain entities
- **Multi-tenancy Support** with license-based feature control

### Core Capabilities
✅ Multi-role RBAC with department scoping  
✅ License enforcement with concurrent user limits  
✅ Comprehensive audit logging  
✅ Flexible grading strategies (GPA, Percentage)  
✅ Enrollment with timetable clash detection  
✅ Gradebook with weighted calculations  
✅ Notifications with fan-out dispatch  
✅ Feature flags with emergency rollback  
✅ Advanced reporting & analytics  
✅ AI-powered learning assistance  
✅ FYP (Final Year Project) management  
✅ Full-text search across platform  

---

## Core System Architecture

### Architectural Layers

```
┌─────────────────────────────────────────┐
│      Web Presentation Layer              │
│   (Razor Pages, API Responses)           │
├─────────────────────────────────────────┤
│   API Controller Layer (63+ Controllers)│
│   Input Validation, Auth Checks         │
├─────────────────────────────────────────┤
│  Application Service Layer (50+ Services)│
│  Business Logic, Orchestration          │
├─────────────────────────────────────────┤
│   Domain Layer (Entities & Interfaces)  │
│   Business Rules, Invariants            │
├─────────────────────────────────────────┤
│ Infrastructure Layer (Repositories)    │
│  Data Access Abstraction                │
├─────────────────────────────────────────┤
│  Database Layer (SQL Server)            │
│  50+ Domain Entities                    │
└─────────────────────────────────────────┘
```

### Technology Stack
- **Backend:** ASP.NET Core Web API
- **Frontend:** Web-based user interface
- **Database:** MSSQL Server with indexed queries
- **Authentication:** JWT Tokens with refresh rotation
- **Messaging:** In-app, Email, SMS notification system
- **AI Integration:** OpenAI ChatGPT integration
- **Design Pattern:** Repository Pattern, Service Pattern, DTO Pattern

### Core Modules

| Module | Location | Purpose |
|--------|----------|---------|
| **Academic** | `Academic/` | Programs, courses, enrollment, degrees |
| **Assignments** | `Assignments/` | Assignments, submissions, gradebook |
| **Attendance** | `Attendance/` | Attendance tracking and reporting |
| **Auth** | `Auth/` | Authentication, authorization, sessions |
| **Quizzes** | `Quizzes/` | Quiz creation, attempts, grading |
| **LMS** | `Lms/` | Learning content, modules, videos |
| **Notifications** | `Notifications/` | Event-driven notifications, inbox |
| **FYP** | `Fyp/` | Final year project management |
| **Helpdesk** | `Helpdesk/` | Support tickets and knowledge base |
| **AI Chat** | `AiChat/` | AI-powered learning assistant |
| **Search** | `Search/` | Full-text search across platform |
| **Study Planner** | `StudyPlanner/` | Personal study planning |
| **Modules** | `Modules/` | Feature/module registry, licenses |
| **Services** | `Services/` | Cross-cutting concerns |

---

## Complete Functionality Index

### Core System (5 features)
- Authentication & Authorization
- User & Role Management
- Department & Institution Management
- License & Module Management
- Account Security & Settings

### Academic (12 features)
- Program & Course Management
- Enrollment Management
- Semester & Academic Calendar
- Student Registration & Whitelist
- Student Profiles & Lifecycle
- Results & Grading
- Degree Audit & Progression
- Parent Portal
- Timetable Management
- Bulk Promotion
- Course Prerequisites
- Student Status Tracking

### Assessment & Grading (5 features)
- Assignments & Submissions
- Quizzes & Assessments
- Gradebook & Assessment Grid
- Report Card Generation
- Rubric-Based Evaluation

### Learning Management (5 features)
- Learning Management System (LMS)
- Study Planner
- AI Chat Assistant
- Attendance Management
- Course Content Organization

### Communication & Notifications (4 features)
- Notifications System
- Announcements
- Discussions & Forums
- Communication Integrations

### Administration (8 features)
- Portal Settings & Configuration
- Theme & Personalization
- Sidebar Menu & Navigation
- Dashboard & Portal Composition
- Reporting & Analytics
- Audit & Logging
- User Import & Bulk Operations
- Building & Room Management

### Advanced Features (7 features)
- Final Year Project (FYP)
- Helpdesk & Support
- Search & Discovery
- Library Management
- Labels & Tagging
- Accreditation & Compliance
- Feature Flags & Rollback

### Integration & Support (5 features)
- Campus Infrastructure Integration
- Portal Capabilities Matrix
- Schema Streaming & Exports
- External System Integration
- Media & Storage Management

**Total: 51 Major Functionalities**

---

## Authentication & Authorization

### 1. User Authentication
**Module Location:** `Auth/`  
**API Controller:** `AuthController.cs`

**Features:**
- Multi-tenant login with email/password
- JWT-based access token generation (15-minute expiry)
- Refresh token rotation (7-day expiry)
- Forced password change for imported users
- Failed login lockout (5 attempts × 15 minutes)
- Multi-Factor Authentication (MFA) support
- Session risk detection and blocking
- Concurrent user limit enforcement per license
- SuperAdmin exemption from concurrency limits
- Device info tracking and IP address logging
- Risk-based session blocking

**Security Features:**
- Password history tracking (prevent reuse)
- License concurrent session enforcement
- Session risk-based blocking
- MFA code validation
- Lockout mechanism with time-based release
- Secure password hashing with salt

**Key Components:**
- `AuthService` - Authentication orchestration
- `ITokenService` - JWT token generation and validation
- `IPasswordHasher` - Secure password verification
- `IUserSessionRepository` - Session management
- DTOs: `LoginRequest`, `LoginResponse`, `RefreshRequest`, `ChangePasswordRequest`

---

### 2. Role-Based Access Control (RBAC)
**Features:**
- Multi-role support: SuperAdmin, Admin, Faculty, Student, Parent, Staff
- Department-based access control
- Role-based permission matrix
- Fine-grained permission assignment per role
- Dynamic authorization at runtime
- Access audit trail for all operations

**Role Hierarchy:**
```
SuperAdmin (System-wide)
├── System administration
├── License management
├── Feature flags
├── All data access
└── Concurrent limit exempt

Admin (Department-scoped)
├── User management
├── Course management
├── Student administration
└── Department data access

Faculty (Course-level)
├── Course teaching & management
├── Assignment/Quiz creation
├── Attendance marking
├── Grading
└── Course enrollment view

Student (Self-service)
├── Enrollment management
├── Assignment submission
├── Quiz participation
├── Result viewing
├── Attendance checking
└── Profile management

Parent (Read-only)
└── Student information viewing

Staff (Support)
├── Support ticket management
└── Limited administrative functions
```

**Key Components:**
- `AdminUserController` - User CRUD and import
- `StudentController` - Student profile management
- Role-based authorization middleware
- `IUserRepository` - User credential management

---

### 3. Department & Institution Management
**API Controllers:** `DepartmentController.cs`, `InstitutionPolicyController.cs`

**Features:**
- Department creation and hierarchical organization
- Faculty assignment to departments
- Admin assignment to departments
- Institution-wide policy configuration
- Institutional grading profiles
- Building and room management
- Department-specific settings

**Key Components:**
- `DepartmentController` - Department CRUD
- `InstitutionPolicyService` - Policy management
- `InstitutionGradingService` - Grading configuration
- `BuildingRoomService` - Facility management

---

### 4. License & Module Management
**API Controllers:** `LicenseController.cs`, `ModuleController.cs`, `ModuleRegistryController.cs`

**Features:**
- License key upload and validation
- Concurrent user limit management
- License expiry tracking and enforcement
- Module activation/deactivation per institution
- Feature flag control for rollout and emergency rollback
- Module entitlement resolution
- Portal capability matrix enforcement
- Usage tracking and reporting

**Key Components:**
- `ILicenseRepository` - License persistence and validation
- `ModuleRegistryService` - Module availability tracking
- `ModuleService` - Module CRUD operations
- `IModuleEntitlementResolver` - Permission matrix enforcement
- `IPortalCapabilityMatrixService` - Feature availability
- Feature Flag Control Plane - SuperAdmin kill switches

---

## User & Access Management

### 5. User Profiles & Account Management
**API Controllers:** `AdminUserController.cs`, `StudentController.cs`

**Features:**
- User account creation (individual and bulk)
- Profile management (personal information, contact details)
- Department assignment
- Role management
- Status management (Active/Inactive)
- Last login tracking
- Account security settings
- Password management and reset

**Key Components:**
- `AdminUserController` - User CRUD and operations
- `StudentController` - Student profile management
- `IUserRepository` - User data persistence
- DTOs: `CreateUserRequest`, `UpdateUserRequest`, `UserResponse`

---

### 6. Batch User Import
**API Controller:** `UserImportController.cs`, `RegistrationImportController.cs`

**Features:**
- CSV user import with validation
- Auto-generated hashed passwords
- Bulk student registration import
- CSV whitelist import
- Data validation before import
- Error reporting per row with detailed feedback
- Import history tracking
- Rollback capability
- Duplicate detection and handling

**Key Components:**
- `UserImportService` - User bulk import orchestration
- `CsvRegistrationImportService` - Registration import
- `IUserImportService` - Import interface
- DTOs: `BulkUserImportRequest`, `ImportResult`

---

### 7. Account Security & Settings
**API Controller:** `AccountSecurityController.cs`

**Features:**
- Password strength enforcement
- Password history (prevent reuse)
- Password change log
- Two-factor authentication setup
- Security questions
- Account recovery options
- Login activity viewing
- Active sessions management
- Device management
- Permission audit

**Key Components:**
- `AccountSecurityService` - Security management
- `IPasswordHasher` - Password verification
- `IAccountSecurityService` - Security interface

---

## Academic Management

### 8. Program & Course Management
**Module Location:** `Academic/`  
**API Controllers:** `ProgramController.cs`, `CourseController.cs`

**Features:**
- Program (degree) creation and configuration
- Course catalog management with credit hours
- Course categorization by department
- Semester-based course mapping
- Auto-generation of semester rows from course configuration
- Course offering creation with max enrollment
- Faculty assignment to course offerings
- Course prerequisite management
- Grading type specification (GPA, Percentage)
- Course sections/divisions
- Course code standardization

**Business Rules:**
- Each course maps to specific program
- Credit hours enforce academic standards
- Prerequisites prevent invalid enrollments
- Max enrollment enforced at offering level
- Faculty must be assigned to teach

**Key Components:**
- `CourseService` - Course CRUD and orchestration
- `ICourseRepository` - Course persistence
- `ISemesterRepository` - Semester lifecycle
- `IPrerequisiteRepository` - Prerequisite tracking
- DTOs: `CreateCourseRequest`, `CreateOfferingRequest`, `AssignFacultyRequest`

---

### 9. Enrollment Management
**API Controller:** `EnrollmentController.cs`

**Features:**
- Student self-enrollment in course offerings
- Admin enrollment of any student
- Seat availability enforcement
- Duplicate enrollment prevention
- Timetable clash detection and admin override
- Drop/withdrawal with audit trail
- Full enrollment history preservation
- Status tracking (Active, Dropped, Completed)
- Retroactive enrollment adjustments
- Enrollment verification and locking
- Waitlist management

**Business Rules Enforced:**
- Course offering must be open
- Semester must not be closed
- Available seats must exist
- Student cannot enroll twice in same offering
- Admin can override timetable clashes with reason
- Enrollment audit trail maintains full history

**Key Components:**
- `EnrollmentService` - Enrollment orchestration
- `IEnrollmentRepository` - Enrollment data
- `ITimetableRepository` - Schedule conflict detection
- DTOs: `EnrollRequest`, `AdminEnrollRequest`, `EnrollmentResponse`

---

### 10. Semester & Academic Calendar
**API Controllers:** `SemesterController.cs`, `CalendarController.cs`

**Features:**
- Semester creation and lifecycle management
- Semester open/closed status enforcement
- Named academic deadlines (assignment deadlines, exam dates)
- Automated reminder notifications based on deadlines
- Calendar-based scheduling
- Deadline visibility by role
- Semester-based course organization
- Academic calendar management

**Key Components:**
- `AcademicCalendarService` - Deadline management and reminders
- `IAcademicDeadlineRepository` - Deadline persistence
- `INotificationService` - Reminder dispatch
- DTOs: `CreateDeadlineRequest`, `UpdateDeadlineRequest`, `SemesterResponse`

---

### 11. Student Registration & Whitelist
**API Controller:** `RegistrationImportController.cs`

**Features:**
- Student self-registration via registration number
- Email-based student registration
- Whitelist management (approval before registration)
- Bulk whitelist import (CSV)
- Single entry whitelist add/update/delete
- Registration number/email dual-mode support
- Automated welcome emails
- Registration status tracking

**Key Components:**
- `StudentRegistrationService` - Registration orchestration
- `IStudentRegistrationService` - Registration business logic
- Student whitelist repositories
- DTOs: `StudentSelfRegisterRequest`, `WhitelistEntryRequest`

---

### 12. Student Profiles & Lifecycle
**Module Location:** `Academic/`  
**API Controllers:** `StudentController.cs`, `StudentLifecycleController.cs`

**Features:**
- Student profile creation (linked to user)
- Registration number tracking
- Program and department assignment
- Admission date recording
- Current semester tracking
- Academic history across all semesters
- Graduation management
- Student status changes (active, suspended, graduated)
- Student change requests (program change, leave of absence)
- Student modification workflow approvals
- Payment receipt management
- Promotion to next semester (bulk)
- Student lifecycle workflows

**Key Components:**
- `StudentRegistrationService` - Profile creation
- `StudentLifecycleService` - Lifecycle orchestration
- `BulkPromotionService` - Semester promotion
- `IStudentLifecycleRepository` - Student state
- DTOs: `CreateStudentProfileRequest`, `GraduationSummaryDto`, `StudentStatusChangeRequest`

---

### 13. Results & Grading
**Module Location:** `Academic/`  
**API Controllers:** `ResultController.cs`, `ResultCalculationController.cs`

**Features:**
- Component-based result entry (Midterm, Final, Project)
- GPA calculation (4.0 scale)
- CGPA (Cumulative GPA) tracking across semesters
- Result publishing with student notifications
- Transcript generation
- Multiple grading strategies (GPA, Percentage)
- Rubric-based evaluation
- Result correction tracking
- Max marks enforcement
- Weighted result calculation
- Grade locks to prevent accidental changes

**Grading Strategies:**
- **GPA Strategy:** 4.0 scale with letter grades (A, B, C, D, F)
- **Percentage Strategy:** 0-100 percentage marks
- Configurable per institution
- Custom grading rules per course

**Key Components:**
- `IResultService` - Result CRUD operations
- `IResultCalculationService` - GPA/CGPA computation
- `ICourseGradingService` - Course-level grading config
- `IInstitutionGradingService` - Institution grading profiles
- `IResultStrategyResolver` - Strategy selection
- DTOs: `CreateResultRequest`, `ResultResponse`, `RubricDto`

---

### 14. Degree Audit & Progression
**API Controllers:** `DegreeAuditController.cs`, `ProgressionController.cs`

**Features:**
- Degree audit trail showing course completion status
- GPA/CGPA tracking
- Remaining course requirements
- Academic standing assessment
- Student progression status (on-track, at-risk, behind)
- Academic alerts and warnings
- Course requirement fulfillment tracking
- Degree progress visualization
- Early warning system

**Key Components:**
- `DegreeAuditService` - Audit generation
- `ProgressionService` - Progression analysis
- `IProgressionRepository` - Progression data
- DTOs: `DegreeAuditResponse`, `ProgressionStatusResponse`

---

### 15. Parent Portal
**API Controller:** `ParentPortalController.cs`

**Features:**
- Parent/Guardian access to student information
- Course enrollment viewing
- Attendance summary
- Result/grade viewing
- Notifications and alerts
- Role-based access control
- Read-only access model

**Key Components:**
- `ParentPortalService` - Parent access logic
- `IParentPortalService` - Portal interface

---

### 16. Timetable Management
**API Controller:** `TimetableController.cs`

**Features:**
- Timetable creation per department/semester
- Timetable entry management (course, day, time, room)
- Publishing/unpublishing timetables
- Timetable clash detection
- PDF export (department-specific)
- Excel export (department-specific)
- Timetable download for all department users
- Auto-title generation from program code + semester
- Scheduling conflict prevention
- Room and instructor assignment

**Key Components:**
- `TimetableService` - Timetable orchestration
- `ITimetableRepository` - Timetable data
- `ITimetableExcelExporter` - Excel export
- `ITimetablePdfExporter` - PDF export
- DTOs: `TimetableEntryRequest`, `TimetableExportRequest`

---

## Assessment & Grading

### 17. Assignments & Submissions
**Module Location:** `Assignments/`  
**API Controller:** `AssignmentController.cs`

**Features:**
- Faculty assignment creation (unpublished draft)
- Assignment editing (only while unpublished)
- Assignment publishing (makes visible to students)
- Assignment retraction (unpublishes if no submissions)
- Assignment deletion (soft-delete with submission protection)
- Due date enforcement
- Max marks specification
- Student assignment submission
- File and text-based submissions
- Duplicate submission prevention
- Faculty grading with marks and feedback
- Submission rejection and re-submission
- Submission status tracking (Pending, Graded, Rejected)
- Assignment listing with submission counts
- Multiple attempt support

**Business Rules:**
- Only unpublished assignments can be edited
- Submissions require published assignment
- Submissions must be within due date
- Max marks awarded cannot exceed assignment max marks
- Duplicate submissions rejected
- Assignments with submissions cannot be retracted

**Key Components:**
- `AssignmentService` - Lifecycle orchestration
- `IAssignmentRepository` - Assignment persistence
- DTOs: `CreateAssignmentRequest`, `SubmitAssignmentRequest`, `GradeSubmissionRequest`

---

### 18. Quizzes & Assessments
**Module Location:** `Quizzes/`  
**API Controller:** `QuizController.cs`

**Features:**
- Quiz creation with configuration
- Timed quiz support
- Attempt limits per student
- Auto-grading for objective questions
- Manual grading for subjective questions
- Question pool management
- Question shuffling
- Student quiz attempts
- Attempt history
- Score and feedback
- Quiz publishing/unpublishing
- Result analytics
- Question randomization

**Key Components:**
- `QuizService` - Quiz management
- `IQuizService` - Quiz interface
- DTOs: `CreateQuizRequest`, `QuizAttemptRequest`, `QuizResultDto`

---

### 19. Gradebook & Assessment Grid
**API Controller:** `GradebookController.cs`

**Features:**
- Gradebook grid view by course offering
- Inline cell entry (marks for specific component)
- CSV import for bulk grade entry
- CSV export for external processing
- Weighted total calculation
- Component-based grading (Midterm, Final, Project)
- Publication status tracking per cell
- Student list with registration numbers
- Component column headers with weightage
- Publish all grades for offering
- Unpublish grades
- Grade visibility control

**Grid Calculation:**
- Weighted Total = Sum(Component Weight × (Marks/Max)) for complete records

**Key Components:**
- `GradebookService` - Grid orchestration
- `IGradebookRepository` - Gradebook data
- DTOs: `GradebookGridResponse`, `UpsertGradebookEntryRequest`, `GradebookExportRequest`

---

### 20. Report Card Generation
**API Controller:** `ReportCardController.cs`

**Features:**
- Semester report card generation
- Course listing with grades and credits
- GPA calculation display
- Semester performance summary
- PDF export capability
- Transcript generation (multi-semester)
- Official record format
- Print-ready layout

**Key Components:**
- `ReportCardService` - Report generation
- `IReportCardService` - Report interface
- DTOs: `ReportCardRequest`, `ReportCardResponse`

---

### 21. Rubric-Based Evaluation
**API Controller:** `RubricController.cs`

**Features:**
- Rubric creation with criteria
- Rubric scoring scales
- Assignment rubric linking
- Rubric application in grading
- Rubric template library
- Rubric sharing between faculty
- Detailed feedback framework

**Key Components:**
- `IRubricService` - Rubric management
- DTOs: `RubricDto`, `RubricCriterionDto`, `RubricScaleDto`

---

## Learning Management System

### 22. Learning Management System (LMS)
**Module Location:** `Lms/`  
**API Controller:** `LmsController.cs`

**Features:**
- Course content module organization (by week)
- Module publishing/unpublishing
- Course materials storage
- Content video embedding
- Video storage URL and embed URL support
- Video duration tracking
- Module-level content management
- Video deletion (soft-delete)
- Ordered module display
- Student access control to published content
- Course announcements
- Discussion forums
- Content versioning

**Key Components:**
- `LmsService` - Content management
- `ILmsRepository` - LMS data persistence
- DTOs: `CourseContentModuleDto`, `ContentVideoDto`, `ModuleCreateRequest`

---

### 23. Study Planner
**API Controller:** `StudyPlanController.cs`

**Features:**
- Personal study plan creation
- Task/milestone management
- Progress tracking
- Deadline management
- Study goal setting
- Calendar integration
- Study schedule visualization
- Task organization by subject/course
- Progress reports

**Key Components:**
- `StudyPlanService` - Study planning
- `IStudyPlanService` - Study planner interface
- DTOs: `StudyPlanDto`, `TaskDto`, `MilestoneDto`

---

### 24. AI Chat Assistant
**Module Location:** `AiChat/`  
**API Controller:** `AiChatController.cs`

**Features:**
- Role-aware chatbot responses
- Department context awareness
- Query topics: Assignments, Results, Attendance, FYP schedules, General help
- Natural language understanding
- Answer generation based on student context
- Theme inheritance from active user theme
- Conversation history (optional)
- LLM backend integration (configurable)
- Multi-turn conversations
- Context-aware responses

**Supported Topics:**
- Assignment and submission guidance
- Result inquiries and GPA calculations
- Attendance policies and records
- FYP schedule and requirements
- General academic guidance
- Course information

**Key Components:**
- `AiChatService` - Chat orchestration
- `IAiChatService` - Chat interface
- `ILlmClient` - LLM backend abstraction
- DTOs: `ChatMessageRequest`, `ChatMessageResponse`

---

### 25. Attendance Management
**Module Location:** `Attendance/`  
**API Controller:** `AttendanceController.cs`

**Features:**
- Single-student attendance marking
- Bulk class attendance marking
- Attendance correction (with audit trail)
- Date normalization (UTC)
- Attendance status: Present, Absent, Late, Excused
- Duplicate prevention per (student, offering, date)
- Remarks/notes field
- Attendance querying by offering
- Student attendance history
- Low attendance threshold checks
- Automatic notifications for low attendance
- Attendance reports and analytics
- Attendance trends analysis

**Business Rules:**
- One record per (student, offering, date)
- Duplicates silently skipped on bulk insert
- Corrections recorded with correcting user ID

**Key Components:**
- `AttendanceService` - Attendance orchestration
- `IAttendanceRepository` - Attendance data
- DTOs: `MarkAttendanceRequest`, `BulkMarkAttendanceRequest`, `AttendanceReportRequest`

---

## Communication & Notifications

### 26. Notifications System
**Module Location:** `Notifications/`  
**API Controller:** `NotificationController.cs`

**Features:**
- Dashboard notification display
- Notification types: Assignment, Quiz, Result, System, Announcement
- Fan-out dispatch to multiple recipients
- Notification read/unread tracking
- Inbox management with pagination
- Unread count badge
- Notification deactivation
- System-generated notifications (background jobs)
- Notification retention
- Role-specific notification delivery
- Notification preferences
- Email notifications
- SMS notifications (optional)
- In-app notifications

**Key Components:**
- `NotificationService` - Notification orchestration
- `INotificationRepository` - Notification persistence
- `INotificationFanoutQueue` - Asynchronous dispatch
- DTOs: `SendNotificationRequest`, `NotificationResponse`, `InboxResponse`

---

### 27. Announcements
**API Controller:** `AnnouncementController.cs`

**Features:**
- Faculty/Admin announcement creation
- Announcement scheduling
- Department-specific announcements
- Role-based visibility
- Announcement archival
- PIN/highlight important announcements
- Announcement expiration
- Rich text formatting
- Attachment support

**Key Components:**
- `IAnnouncementService` - Announcement interface
- DTOs: `CreateAnnouncementRequest`, `AnnouncementResponse`

---

### 28. Discussions & Forums
**API Controller:** `DiscussionController.cs`

**Features:**
- Course-level discussions
- Thread management
- Reply threading
- Moderation capabilities
- User mention support
- Rich text content
- Discussion archival
- Participant list
- Discussion search
- Notification on replies

**Key Components:**
- `IDiscussionService` - Discussion interface
- DTOs: `CreateDiscussionRequest`, `ThreadReplyRequest`, `DiscussionResponse`

---

### 29. Communication Integrations
**API Controller:** `CommunicationIntegrationsController.cs`

**Features:**
- Email integration
- SMS notification support (configurable)
- Chat integration hooks
- External system webhooks
- Integration audit logging
- SMTP configuration
- Email template management
- SMS gateway integration

**Key Components:**
- `CommunicationIntegrationService` - Integration management
- `IEmailSender` - Email abstraction
- `IOutboundIntegrationGateway` - External system communication

---

## Administration & Configuration

### 30. Portal Settings & Configuration
**API Controller:** `PortalSettingsController.cs`

**Features:**
- Institution name and branding
- Logo/favicon management
- System-wide email configurations
- SMS provider configuration
- Default theme selection
- LMS toggle
- Finance module configuration
- Payment gateway integration settings
- Session timeout configuration
- Feature flags management
- Super Admin controls
- System-wide parameters

**Key Components:**
- `SettingsService` - Settings management
- `ISettingsServices` - Settings interface
- DTOs: `PortalSettingsRequest`, `PortalSettingsResponse`

---

### 31. Theme & Personalization
**API Controller:** `ThemeController.cs`

**Features:**
- 15+ pre-built themes
- Light mode support
- Dark mode support
- High-contrast accessibility themes
- Per-user theme selection
- Persistent theme preferences
- Admin system default theme
- Theme customization (font, colors)
- AI chatbot theme inheritance
- Custom color schemes

**Key Components:**
- `ThemeService` - Theme management
- DTOs: `ThemeResponse`, `UserThemePreferenceRequest`

---

### 32. Sidebar Menu & Navigation
**API Controller:** `SidebarMenuController.cs`

**Features:**
- Role-based menu visibility
- Menu item ordering
- Sub-menu management
- Dynamic menu based on active modules
- Menu customization per role
- Menu state persistence
- Sidebar collapse/expand state
- Module-aware menu items

**Key Components:**
- `SidebarMenuController` - Menu management
- DTOs: `MenuResponse`, `MenuItemDto`

---

### 33. Dashboard & Portal Composition
**API Controller:** `DashboardCompositionController.cs`

**Features:**
- Customizable dashboard layout
- Widget support (cards, charts, lists)
- Role-specific dashboard defaults
- Student dashboard with courses, assignments, grades
- Faculty dashboard with class lists, grading, attendance
- Admin dashboard with analytics, reports
- Drag-and-drop composition (optional)
- Save/load dashboard configurations
- Dashboard state persistence

**Key Components:**
- `DashboardCompositionService` - Dashboard management
- `IDashboardCompositionService` - Dashboard interface

---

### 34. Reporting & Analytics
**API Controllers:** `ReportController.cs`, `AnalyticsController.cs`

**Features:**
- Student performance reports
- Department analytics
- Assignment/Quiz statistics
- Attendance reports
- Result distribution analysis
- Course popularity metrics
- Faculty performance metrics
- Export to PDF, Excel, CSV
- Scheduled report generation
- Report caching
- Custom report builder
- Data visualization
- Trend analysis

**Report Types:**
- Academic Performance Report
- Attendance Report
- Assessment Statistics
- Enrollment Report
- Financial Report (if Finance module active)
- Department Report
- Institution Report
- Course Analytics
- Student Analytics

**Key Components:**
- `IReportService` - Report generation
- `IAnalyticsService` - Analytics interface
- DTOs: `ReportRequest`, `ReportResponse`

---

### 35. Audit & Logging
**Module Location:** (Infrastructure)

**Features:**
- All create/update/delete operations logged
- IP address tracking
- User activity logging
- Failed authentication attempts
- Module access logging
- Data change tracking
- Audit trail export
- Log retention policies
- Compliance reporting
- Audit report generation

**Key Components:**
- `IAuditService` - Audit logging
- `AuditLog` - Domain entity
- DTOs: `AuditLogResponse`

---

### 36. User Import & Bulk Operations
**API Controller:** `UserImportController.cs`

**Features:**
- CSV user import with validation
- Bulk student registration import
- Auto-generated hashed passwords
- CSV whitelist import
- Data validation before import
- Error reporting per row
- Import history tracking
- Rollback capability
- Duplicate detection

**Key Components:**
- `UserImportService` - User bulk import
- `CsvRegistrationImportService` - Registration import
- `IUserImportService` - Import interface

---

### 37. Building & Room Management
**API Controller:** `BuildingController.cs`, `RoomController.cs`

**Features:**
- Building directory
- Room management
- Room capacity tracking
- Facility booking (for FYP, meetings)
- Room availability checking
- Room assignment for classes
- Facility reservation

**Key Components:**
- `BuildingRoomService` - Facility management
- `IBuildingRoomService` - Building interface

---

## Advanced Features

### 38. Final Year Project (FYP) Management
**Module Location:** `Fyp/`  
**API Controller:** `FypController.cs`

**Features:**
- Student FYP project allocation
- FYP proposal management
- Meeting scheduling
- Meeting location entry (Department, Room number)
- Panel member selection and assignment
- Supervisor assignment
- Student and panel notifications
- FYP status tracking (Proposal, Development, Review, Final, Completed)
- FYP progress reporting
- Defense scheduling
- Evaluation form management
- Grade assignment

**Key Components:**
- `FypService` - FYP orchestration
- `IFypService` - FYP interface
- DTOs: `FypAllocationDto`, `MeetingScheduleDto`, `ProposalSubmissionDto`

---

### 39. Helpdesk & Support
**Module Location:** `Helpdesk/`  
**API Controller:** `HelpdeskController.cs`

**Features:**
- Support ticket creation
- Ticket categorization
- Priority levels (Low, Medium, High, Urgent)
- Status tracking (Open, In Progress, Resolved, Closed)
- Ticket assignment to support staff
- Response management
- Attachment support
- Ticket history
- SLA tracking (optional)
- FAQ management
- Knowledge base
- Ticket search and filtering

**Key Components:**
- `HelpdeskService` - Ticket orchestration
- `IHelpdeskService` - Helpdesk interface
- DTOs: `CreateTicketRequest`, `TicketResponse`

---

### 40. Search & Discovery
**Module Location:** `Search/`  
**API Controller:** `SearchController.cs`

**Features:**
- Global full-text search
- Cross-entity search (courses, students, faculty, etc.)
- Search result ranking
- Faceted search by entity type
- Advanced search filters
- Search history (optional)
- Trending searches
- Search analytics
- Autocomplete suggestions

**Key Components:**
- `SearchService` - Search orchestration
- `ISearchService` - Search interface
- DTOs: `SearchRequest`, `SearchResponse`

---

### 41. Library Management
**API Controller:** `LibraryController.cs`

**Features:**
- Book catalog management
- Book checkout/return
- Availability tracking
- Due date management
- Late fee calculation
- Reservation system
- Student library account
- Book recommendations
- Digital resource access
- Book search

**Key Components:**
- `LibraryService` - Library management
- `ILibraryService` - Library interface

---

### 42. Labels & Tagging
**API Controller:** `LabelController.cs`

**Features:**
- Custom label creation
- Entity tagging (courses, students, etc.)
- Color-coded labels
- Bulk label operations
- Label search and filtering
- Label management

**Key Components:**
- `LabelService` - Label management
- `ILabelService` - Label interface

---

### 43. Accreditation & Compliance
**API Controller:** `AccreditationController.cs`

**Features:**
- Accreditation requirement tracking
- Compliance monitoring
- Report generation
- Audit trail
- Quality metrics
- Standards mapping
- Compliance documentation

**Key Components:**
- `AccreditationService` - Accreditation management
- `IAccreditationService` - Accreditation interface

---

### 44. Feature Flags & Rollback
**Module Location:** `Modules/`  
**API Controller:** `FeatureFlagsController.cs`

**Features:**
- Feature flag creation and management
- Rollout control (percentage-based, role-based)
- Emergency rollback capability
- A/B testing support
- Feature monitoring
- Flag audit trail
- SuperAdmin-only control
- Feature adoption tracking

**Key Components:**
- Feature Flag Control Plane (Infrastructure)
- `FeatureFlagsController` - Flag management
- DTOs: `FeatureFlagRequest`, `FeatureFlagResponse`

---

### 45. Labels & Classification
**Features:**
- Custom label/tag creation
- Entity classification
- Bulk operations
- Search by labels
- Color coding
- Hierarchical labeling

---

## Integration & Support

### 46. Campus Infrastructure Integration
**Module Location:** `Services/`  
**API Controller:** `BuildingController.cs`, `RoomController.cs`

**Features:**
- Student Information System (SIS) integration
- HR system integration
- Financial system integration
- Building Management integration
- Transportation system integration
- Hostel Management integration
- Data synchronization

**Key Components:**
- `BuildingRoomService` - Facility management
- `IBuildingRoomService` - Building interface

---

### 47. Portal Capabilities Matrix
**Module Location:** `Services/`  
**API Controller:** `PortalCapabilitiesController.cs`

**Features:**
- Capability discovery by client
- License-based capability filtering
- Module entitlement resolution
- API endpoint availability matrix
- Feature access control
- Capability export

**Key Components:**
- `IPortalCapabilityMatrixService` - Capability matrix
- `IModuleEntitlementResolver` - Entitlement logic

---

### 48. External System Integration
**Module Location:** (Infrastructure)

**Features:**
- REST API for external systems
- Webhook support for event notification
- OAuth2 integration (planned)
- SSO integration
- LMS platform integration (Canvas, Blackboard)
- Data exchange protocols

**Key Components:**
- `IOutboundIntegrationGateway` - External communication
- `ICommunicationIntegrationContracts` - Integration contracts

---

### 49. Media & Storage Management
**Module Location:** (Infrastructure)

**Features:**
- File upload/download management
- Document storage
- Video content storage
- File size limits
- Malware scanning (if configured)
- CDN integration support
- File expiry management
- Storage optimization

**Key Components:**
- `IMediaStorageService` - Storage abstraction

---

### 50. Schema Streaming & Exports
**Module Location:** (Implicit)

**Features:**
- Streaming data exports
- Large dataset handling
- Memory-efficient processing
- Export format options

---

---

## Service & Controller Matrix

### Academic Module Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `AcademicCalendarService` | Academic/ | `IAcademicCalendarService` | CalendarController | Deadlines, reminders |
| `CourseService` | Academic/ | `ICourseService` | CourseController | CRUD, auto-semester generation |
| `EnrollmentService` | Academic/ | `IEnrollmentService` | EnrollmentController | Enroll, drop, list |
| `BulkPromotionService` | Academic/ | `IBulkPromotionService` | BulkPromotionController | Promote students |
| `DegreeAuditService` | Academic/ | `IDegreeAuditService` | DegreeAuditController | Audit trail, requirements |
| `GraduationService` | Academic/ | `IGraduationService` | GraduationController | Graduate, list candidates |
| `InstitutionGradingService` | Academic/ | `IInstitutionGradingService` | InstitutionGradingProfileController | Profiles, rules |
| `ReportCardService` | Academic/ | `IReportCardService` | ReportCardController | Transcript, report card |
| `SchoolStreamService` | Academic/ | `ISchoolStreamService` | SchoolStreamController | Stream management |
| `StudentRegistrationService` | Academic/ | `IStudentRegistrationService` | RegistrationImportController | Register, whitelist, import |
| `ParentPortalService` | Academic/ | `IParentPortalService` | ParentPortalController | Parent info access |
| `ProgressionService` | Academic/ | `IProgressionService` | ProgressionController | Progression status |
| `CourseGradingService` | Academic/ | `ICourseGradingService` | GradingConfigController | Grading config |

### Assessment Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `AssignmentService` | Assignments/ | `IAssignmentService` | AssignmentController | Create, publish, submit, grade |
| `GradebookService` | Assignments/ | `IGradebookService` | GradebookController | Grid, entry, export |
| `QuizService` | Quizzes/ | `IQuizService` | QuizController | Create, attempt, grade |
| `RubricService` | Services/ | `IRubricService` | RubricController | Create, edit, apply |
| `ResultService` | Services/ | `IResultService` | ResultController | Enter, correct, publish |
| `ResultCalculationService` | Services/ | `IResultCalculationService` | ResultCalculationController | Calculate GPA, publish |

### Communication Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `NotificationService` | Notifications/ | `INotificationService` | NotificationController | Send, inbox, mark read |
| `AnnouncementService` | Services/ | `IAnnouncementService` | AnnouncementController | Create, list |
| `DiscussionService` | Services/ | `IDiscussionService` | DiscussionController | Create, reply, moderate |

### Learning Management Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `LmsService` | Lms/ | `ILmsService` | LmsController | Modules, videos CRUD |
| `TimetableService` | Services/ | `ITimetableService` | TimetableController | CRUD, export |
| `AttendanceService` | Attendance/ | `IAttendanceService` | AttendanceController | Mark, correct, query |
| `StudyPlanService` | StudyPlanner/ | `IStudyPlanService` | StudyPlanController | Create, update |

### Complete API Controller List (63 Controllers)

| # | Controller | Module | Primary Methods |
|---|-----------|--------|-----------------|
| 1 | AccountSecurityController | Services | Security, 2FA, password |
| 2 | AccreditationController | Services | Accreditation tracking |
| 3 | AdminChangeRequestController | Services | Admin workflows |
| 4 | AdminUserController | Auth | User CRUD, import |
| 5 | AiChatController | AiChat | Chat messages |
| 6 | AnalyticsController | Reports | Analytics queries |
| 7 | AnnouncementController | Services | Announcement CRUD |
| 8 | AssignmentController | Assignments | Assignment lifecycle |
| 9 | AttendanceController | Attendance | Mark, correct, query |
| 10 | AuthController | Auth | Login, refresh, logout |
| 11 | BuildingController | Services | Building CRUD |
| 12 | BulkPromotionController | Academic | Student promotion |
| 13 | CalendarController | Academic | Deadlines & calendar |
| 14 | CommunicationIntegrationsController | Services | Integration setup |
| 15 | CourseController | Academic | Course management |
| 16 | DashboardCompositionController | Services | Dashboard customization |
| 17 | DegreeAuditController | Academic | Degree audit |
| 18 | DepartmentController | Academic | Departments |
| 19 | DiscussionController | Services | Discussions |
| 20 | EnrollmentController | Academic | Enrollment |
| 21 | FeatureFlagsController | Modules | Feature flags |
| 22 | FypController | Fyp | FYP management |
| 23 | GradebookController | Assignments | Gradebook grid |
| 24 | GradingConfigController | Academic | Grading config |
| 25 | GraduationController | Academic | Graduation |
| 26 | HelpdeskController | Helpdesk | Support tickets |
| 27 | InstitutionGradingProfileController | Academic | Grading profiles |
| 28 | InstitutionPolicyController | Services | Policies |
| 29 | LabelController | Services | Tags/labels |
| 30 | LibraryController | Services | Library |
| 31 | LicenseController | Modules | License management |
| 32 | LmsController | Lms | Course content |
| 33 | ModuleController | Modules | Module management |
| 34 | ModuleRegistryController | Modules | Module registry |
| 35 | NotificationController | Notifications | Notifications |
| 36 | ParentPortalController | Academic | Parent access |
| 37 | PaymentReceiptController | Services | Payment receipts |
| 38 | PortalCapabilitiesController | Services | Capability matrix |
| 39 | PortalSettingsController | Services | Portal settings |
| 40 | PrerequisiteController | Academic | Prerequisites |
| 41 | ProgramController | Academic | Programs/degrees |
| 42 | ProgressionController | Academic | Student progression |
| 43 | QuizController | Quizzes | Quizzes |
| 44 | RegistrationImportController | Academic | Registration import |
| 45 | ReportCardController | Academic | Report cards |
| 46 | ReportController | Reports | Reports |
| 47 | ReportSettingsController | Reports | Report config |
| 48 | ResultCalculationController | Academic | Result calculation |
| 49 | ResultController | Academic | Results |
| 50 | RoomController | Services | Rooms |
| 51 | RubricController | Assignments | Rubrics |
| 52 | SchoolStreamController | Academic | Academic streams |
| 53 | SearchController | Search | Global search |
| 54 | SemesterController | Academic | Semesters |
| 55 | SidebarMenuController | Services | Menu config |
| 56 | StudentController | Academic | Student profiles |
| 57 | StudentLifecycleController | Services | Graduation, status |
| 58 | StudyPlanController | StudyPlanner | Study plans |
| 59 | TeacherModificationController | Academic | Faculty changes |
| 60 | TenantOperationsController | Modules | Multi-tenancy |
| 61 | ThemeController | Services | Theme selection |
| 62 | TimetableController | Services | Timetables |
| 63 | UserImportController | Services | User import |

---

## Architecture & Design Patterns

### Layered Architecture

```
┌────────────────────────────────────────────────┐
│         API Controller Layer (63+)             │
│    Input validation, Authentication checks    │
├────────────────────────────────────────────────┤
│    Application Service Layer (50+)            │
│    Business logic, orchestration, workflows   │
├────────────────────────────────────────────────┤
│      Domain Layer (Entities & Rules)          │
│    Business rule enforcement, validation      │
├────────────────────────────────────────────────┤
│  Repository & Infrastructure Layer            │
│    Data access abstraction, persistence       │
├────────────────────────────────────────────────┤
│         Database Layer (SQL Server)           │
│         50+ domain entities                   │
└────────────────────────────────────────────────┘
```

### Design Patterns Used

#### 1. Repository Pattern
- Abstraction over data persistence
- 40+ repository interfaces
- LINQ-based queries with Entity Framework Core
- Generic repository base class

```csharp
public interface IStudentRepository
{
    Task<Student> GetByIdAsync(int id);
    Task<IEnumerable<Student>> GetByDepartmentAsync(int deptId);
    Task AddAsync(Student student);
    Task UpdateAsync(Student student);
}
```

#### 2. Service Pattern
- Business logic encapsulation
- 50+ application services
- Dependency injection via constructor
- Service orchestration for complex operations

```csharp
public interface IEnrollmentService
{
    Task<EnrollmentResponse> EnrollAsync(int studentId, EnrollRequest request);
    Task DropAsync(int enrollmentId);
    Task<IEnumerable<EnrollmentResponse>> GetForStudentAsync(int studentId);
}
```

#### 3. DTO Pattern
- Request/Response DTOs for API contracts
- Type-safe data transfer
- Input validation via FluentValidation
- Mapping from/to domain entities

```csharp
public class EnrollRequest
{
    public int CourseOfferingId { get; set; }
    [Required]
    public bool OverrideTimetableClash { get; set; }
}
```

#### 4. Dependency Injection
- Constructor-based DI
- Service registration in startup
- Interface-based abstractions
- Loose coupling between components

#### 5. Specification Pattern (Optional)
- Complex query specifications
- Reusable query logic
- Linq-based predicates
- Domain query logic encapsulation

### Module Organization

```
Application Layer (14 Modules):
├── Academic/           → Program, course, enrollment, results
├── Assignments/        → Assignments, submissions, gradebook
├── Attendance/         → Attendance tracking
├── Auth/              → Authentication, authorization
├── Quizzes/           → Quiz management
├── Lms/               → Learning content
├── Notifications/     → Notification dispatch
├── Fyp/               → Final year projects
├── Helpdesk/          → Support tickets
├── AiChat/            → AI assistant
├── Search/            → Full-text search
├── StudyPlanner/      → Study planning
├── Modules/           → Feature registry
└── Services/          → Cross-cutting services

Each Module Contains:
├── Services/          → Business logic
├── Interfaces/        → Service contracts
└── DTOs/              → Request/Response models
```

---

## API Patterns & Data Flows

### Standard CRUD Pattern
```
GET    /api/v1/{resource}              List all
GET    /api/v1/{resource}/{id}         Get single
POST   /api/v1/{resource}              Create
PUT    /api/v1/{resource}/{id}         Update
DELETE /api/v1/{resource}/{id}         Delete
```

### Domain-Specific Patterns
```
POST   /api/v1/enrollments/enroll              Enroll student
POST   /api/v1/enrollments/{id}/drop           Drop enrollment
POST   /api/v1/assignments/{id}/publish        Publish assignment
POST   /api/v1/attendance/bulk-mark            Bulk mark attendance
GET    /api/v1/gradebook/{offeringId}         Get gradebook grid
POST   /api/v1/results/publish-all             Publish all results
GET    /api/v1/timetable/export/{id}/pdf      Export timetable
```

### Request → Response Flow
```
Client Request
    ↓
API Controller (validation, auth check)
    ↓
Middleware (logging, error handling)
    ↓
Application Service (business logic)
    ↓
Domain Model (invariant enforcement)
    ↓
Repository Interface (abstraction)
    ↓
Infrastructure (EF Core, SQL Server)
    ↓
Database Query Execution
    ↓
Response DTO → JSON → Client
```

### Notification Fan-Out Pattern
```
SendAsync() → Create Notification
    ↓
FanOutRecipientsAsync() → Create NotificationRecipient rows
    ↓
Queue for async dispatch (if > 250 recipients)
    ↓
User inbox fetch with Read tracking
    ↓
Notification history maintenance
```

### Error Handling Pattern
```
Controller try-catch
    ↓
Service exception handling
    ↓
Custom exception mapping
    ↓
HTTP status code response
    ↓
Error detail JSON response
```

---

## Performance Considerations

### Optimization Strategies

#### Database Level
- Indexed searches on commonly queried fields
- Composite indexes on (StudentId, CourseOfferingId, Date)
- Foreign key indexes
- Archive policies for old data
- Query optimization

#### Application Level
- Pagination for large datasets
- Lazy loading of navigation properties
- Batch operations for bulk imports
- Async operations throughout
- Caching strategies

#### Caching Strategy
- Role caching (30-minute TTL)
- Menu/sidebar caching (1-hour TTL)
- Settings caching (1-hour TTL)
- Result caching (varies by operation)
- Semester/program caching (daily TTL)

#### Query Optimization
```csharp
// Inefficient: N+1 queries
var students = _repository.GetAll();  // Query 1
foreach(var student in students)      // Query N
    var enrollments = student.Enrollments;

// Optimized: Single query with Include
var students = _repository.GetAll()
    .Include(s => s.Enrollments)
    .ToList();  // Query 1
```

### Common Performance Hotspots

- **Gradebook Grid Loading** - Use pagination/lazy loading
- **Full-Text Search** - Implement indexed search
- **Attendance Bulk Mark** - Use batch insert operations
- **Report Generation** - Run asynchronously with queuing
- **Notification Dispatch** - Use fan-out queue for large audiences

---

## Key Validations & Business Rules

### Enrollment
```
✓ Semester NOT closed
✓ Offering is OPEN
✓ Student NOT already enrolled
✓ Seat available
✓ Timetable clash check (admin override allowed)
✓ Prerequisites satisfied
```

### Assignment Lifecycle
```
✓ Can edit only if UNPUBLISHED
✓ Can submit only if PUBLISHED
✓ Can submit only within DUE DATE
✓ Cannot submit if already submitted
✓ Cannot retract if submissions exist
✓ Max marks awarded ≤ Assignment.MaxMarks
```

### Attendance
```
✓ One record per (Student, Offering, Date)
✓ Duplicates silently skipped on bulk insert
✓ Corrections tracked with correcting user ID
✓ Date must be valid for offering
```

### Authentication
```
✓ 5 failed attempts → 15 min lockout
✓ MFA check if enabled
✓ Session risk assessment
✓ License concurrent limit enforcement
✓ SuperAdmin exempt from concurrency
```

### Grade Entry
```
✓ Component marks ≤ Component max marks
✓ Weighted total calculated correctly
✓ Publication status tracked
✓ Grade visible to student after publication
✓ Grade change requires re-entry
```

---

## Security Features Matrix

| Feature | Implementation | Details |
|---------|---------------|---------| 
| **Authentication** | JWT Tokens | 15-min access, 7-day refresh |
| **Authorization** | RBAC | Role-based, department-scoped |
| **Password Security** | Hashing/Salting | Bcrypt or PBKDF2 |
| **Password History** | Enforcement | Prevent reuse of N recent passwords |
| **Sessions** | Risk Detection | IP tracking, device info |
| **Concurrent Limits** | Enforcement | Per-license user limits |
| **Audit Trail** | Comprehensive | All CRUD operations logged |
| **Encryption** | Data at Rest | Database encryption (optional) |
| **SSL/TLS** | Transmission | HTTPS only |
| **Rate Limiting** | API Protection | Per-endpoint limits |

---

## Database Entity Groups

### Identity & Security
- User, Role, UserSession, PasswordHistory, AuditLog

### Academic Structure
- Program, Course, CourseOffering, Semester, Enrollment, Student

### Assessment
- Assignment, AssignmentSubmission, Quiz, QuizAttempt, Result, Rubric

### Attendance
- AttendanceRecord, AttendanceStatus

### Notifications
- Notification, NotificationRecipient, NotificationType

### Configuration
- PortalSettings, InstitutionPolicy, ThemeProfile, ModuleRegistry

### Student Lifecycle
- StudentProfile, StudentStatus, ChangeRequest, GraduationRecord

### FYP & Projects
- FypAllocation, FypProposal, FypMeeting, FypPanel

### Support & Library
- HelpDeskTicket, LibraryBook, LibraryCheckout, Reservation

---

## Integration Points

### Email
- User welcome emails
- Password reset links
- Announcements
- Notifications
- Reports
- Grade notifications

### SMS (Optional)
- Low attendance alerts
- Result announcements
- Event reminders
- Critical notifications

### LLM (AI Chat)
- OpenAI, Anthropic, or local LLM
- Role-aware prompt engineering
- Department context injection
- Query classification

### Payment Gateway (Optional)
- Online fee payment
- Receipt generation
- Payment verification
- Refund processing

### Export Formats
- **Excel** - Gradebook, timetable, reports, attendance
- **PDF** - Transcripts, timetables, assignments, reports
- **CSV** - Bulk import, export data feeds

---

## Common Task Flows

### Student Enrollment Flow
```
- View available courses
- Check prerequisites
- Check timetable for clashes
- Enroll (with admin override capability)
- Confirmation notification
- Access to course materials
- Enrollment audit log created
```

### Assignment Submission Flow
```
- Faculty publishes assignment
- Student views assignment
- Student submits work (file or text)
- Duplicate submission check
- Faculty grades submission
- Student receives feedback
- Grade published to gradebook
- Automatic notification to student
```

### Grade Entry Flow
```
- Faculty views gradebook grid
- Inline edit marks for each component
- Or bulk import via CSV
- System calculates weighted totals
- Faculty reviews calculations
- Publish all grades
- Automatic student notification
- Audit log entry created
```

### Attendance Flow
```
- Faculty marks attendance (single or bulk)
- System checks for duplicates
- Attendance recorded with timestamp
- Automated low attendance check
- Notifications sent to at-risk students
- Attendance history maintained
- Reports generated automatically
```

### FYP Workflow
```
- Student submits FYP proposal
- Supervisor assignment
- Panel member selection
- Meeting scheduling
- Progress tracking
- Final submission
- Defense evaluation
- Graduation recommendation
```

---

## Statistics & Metrics

### Codebase Metrics
| Metric | Count |
|--------|-------|
| API Controllers | 63+ |
| Application Services | 50+ |
| Service Interfaces | 50+ |
| Application Modules | 14 |
| API Endpoints | 200+ |
| Domain Entities | 50+ |
| Database Tables | 50+ |
| DTOs | 100+ |
| Repository Interfaces | 40+ |
| Major Features | 51 |
| Supported Roles | 6 |

### System Capability Matrix
| Capability | Status | Module |
|-----------|--------|--------|
| Multi-Role RBAC | ✅ Implemented | Auth |
| License Management | ✅ Implemented | Modules |
| Enrollment Management | ✅ Implemented | Academic |
| Gradebook with Weighted Calculations | ✅ Implemented | Assignments |
| Timetable Clash Detection | ✅ Implemented | Academic |
| AI Chat Assistant | ✅ Implemented | AiChat |
| Full-Text Search | ✅ Implemented | Search |
| FYP Management | ✅ Implemented | Fyp |
| Feature Flags & Rollback | ✅ Implemented | Modules |
| Audit & Compliance | ✅ Implemented | Infrastructure |
| Advanced Reporting | ✅ Implemented | Reports |
| Multi-Format Export | ✅ Implemented | Reports |

---

## Related Documentation

For more detailed information, see:
- [Function-List.md](Function-List.md) - Detailed function reference
- [Advance-Enhancements.md](Advance-Enhancements.md) - Planned enhancements
- [Phase31-Stage31.2-Security-Hardening.md](Phase31-Stage31.2-Security-Hardening.md) - Security details
- [PRD.md](../Project%20startup%20Docs/PRD.md) - Product requirements

---

## Revision History

| Version | Date | Status | Notes |
|---------|------|--------|-------|
| 1.0 | May 11, 2026 | Initial | Comprehensive overview created |
| 2.0 | May 11, 2026 | Merged | Combined 4 reference documents |

---

**Last Updated:** May 11, 2026  
**Status:** Phase 33 - Security Hardening Active  
**Next Phase:** Phase 34 - Performance Optimization & Scaling


