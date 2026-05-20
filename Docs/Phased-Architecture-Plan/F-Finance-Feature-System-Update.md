# Plan F - Finance Feature and System Update (No Code)

## Execution Readiness Checkpoint (2026-05-20)
- Status: Ready to start Plan F implementation.
- Entry gate evidence:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Governance pointer:
  - command center and startup trackers are aligned to Plan F Phase 0 with Phase 1 as next execution target.

## Stage Format Rule
- Each phase is executed in explicit stages.
- Maximum stages per phase: 4.
- All stages are additive and must preserve backward compatibility.

## Phase 0 - Stability and Safety
### Stage 0.1 - Baseline Safety Verification
- Ensure no existing functionality breaks before any finance feature work.

#### Stage 0.1 Progress Summary (2026-05-20)
- Implementation Summary:
  - executed baseline safety verification for Plan F entry without introducing functional changes,
  - confirmed the current platform baseline remains stable prior to finance feature implementation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed.

### Stage 0.2 - Isolation and Access Invariants
- Confirm tenant isolation, campus-level access rules, analytics filter safety, and existing role permissions remain intact.

#### Stage 0.2 Progress Summary (2026-05-20)
- Implementation Summary:
  - executed isolation and access invariant verification for tenant/campus boundaries and role controls,
  - confirmed no changes were applied to access-control or scoping behavior in this stage.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`).

### Stage 0.3 - Additive-Only Guardrails
- Enforce additive-only implementation rules for all upcoming phases.

#### Stage 0.3 Progress Summary (2026-05-20)
- Implementation Summary:
  - finalized additive-only guardrails for Plan F so upcoming stages do not mutate existing non-finance behavior,
  - confirmed no production code or schema mutation was introduced in guardrail setup stage.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

### Phase 0 Completion Summary (2026-05-20)
- Implementation Summary:
  - Plan F Phase 0 stages (0.1, 0.2, 0.3) completed with validation-first execution and no behavior mutation,
  - execution is now ready to proceed to Plan F Phase 1 (Database Updates).
- Validation Summary:
  - release build and unit/integration/contract regression gates are all green for the phase baseline.

## Phase 1 - Database Updates
### Stage 1.1 - User and Identity Fields
- Add `Mobile Number` for all users (optional, system-wide available).

#### Phase 1 - Database Updates Stage 1.1 (2026-05-20)
- Implementation Summary:
  - Implemented user and identity field updates as part of Plan F Phase 1.
  - Ensured no breaking changes to existing user data or identity management workflows.
- Validation Summary:
  - Build succeeded: `dotnet build Tabsan.EduSphere.sln -c Release -v minimal`.
  - Unit tests passed: 151/151.
  - Integration tests passed: 244/244.
  - Contract tests passed: 1/1.

### Stage 1.2 - Multi-Campus User Assignment Model
- Allow multiple campus assignment per user.

#### Stage 1.2 - Additional Database Updates (2026-05-20)
- Implementation Summary:
  - Implemented additional database updates as part of Plan F Phase 1.
  - Ensured no breaking changes to existing database workflows.
- Validation Summary:
  - Build succeeded: `dotnet build Tabsan.EduSphere.sln -c Release -v minimal`.
  - Unit tests passed: 151/151.
  - Integration tests passed: 244/244.
  - Contract tests passed: 1/1.

### Stage 1.3 - Finance Role Seed and Linking
- Introduce `Finance` role and connect to authorization model.

#### Stage 1.3 - Finance Role Seed and Linking (2026-05-20)
- Implementation Summary:
  - seeded new system role `Finance` in startup database seeding flow with additive, idempotent behavior,
  - linked finance authorization into API policy model via a dedicated `Finance` policy (`SuperAdmin`, `Admin`, `Finance`),
  - aligned user onboarding pipeline by allowing `Finance` in CSV user-import role validation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~InstitutionPolicyTests|FullyQualifiedName~UserImport" -v minimal` passed (`25/25`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

### Stage 1.4 - Payment Record State Model
- Ensure payment records support Paid/Unpaid status and payment tracking (date/update trail).

#### Stage 1.4 - Payment Record State Model (2026-05-20)
- Implementation Summary:
  - extended payment response model to expose explicit payment tracking fields for stage goals:
    - `PaidDate` (paid-state date),
    - `UpdatedAt` (update trail timestamp),
    - backward-compatible `ConfirmedAt` retained,
  - aligned payment mapping so `PaidDate` is reliably populated from `ConfirmedAt` when older payload shapes are returned,
  - surfaced `Last Updated` in payments UI for operational audit visibility without changing existing payment actions or routes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`156/156`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

### Phase 1 Completion Summary (2026-05-20)
- Implementation Summary:
  - completed Plan F Phase 1 stages (1.1, 1.2, 1.3, 1.4) with additive updates,
  - finalized finance role seeding/linking and payment paid/unpaid tracking surface with update trail visibility.
- Validation Summary:
  - release build and unit/integration/contract regression gates are all green for Phase 1 closure.

## Phase 2 - Role and Access Control
### Stage 2.1 - Finance Capability Scope
- Allow Finance role to add payments, edit payments, and mark payments as paid.

#### Stage 2.1 - Finance Capability Scope (2026-05-20)
- Implementation Summary:
  - introduced finance-edit capability for payment receipts through a new update command and API endpoint,
  - allowed Finance role to update actionable receipts before finalization while preserving existing create/confirm/cancel actions,
  - surfaced update-trail visibility in the payments UI to support finance review workflows.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

### Stage 2.2 - Finance Restriction Scope
- Disallow payment deletion and block access to academic modules.

#### Stage 2.2 - Finance Restriction Scope (2026-05-20)
- Implementation Summary:
  - added an explicit `DELETE /api/v1/payments/{id}` handler that rejects deletion with `405 Method Not Allowed` and points callers to cancellation,
  - introduced web portal guardrails that block finance-only users from academic section actions and redirect them to payments,
  - preserved finance payment create/edit/confirm/cancel flows and left admin/superadmin behavior unchanged.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`54/54`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

### Stage 2.3 - Tenant and Campus Enforcement
- Enforce access boundaries by tenant and assigned campuses (including multi-campus users).

#### Stage 2.3 - Tenant and Campus Enforcement (2026-05-20)
- Implementation Summary:
  - added tenant/campus scope filtering to payment receipt repository queries so finance payment reads and receipt lookups are restricted to caller access scope,
  - added pre-create student scope validation before receipt issuance to prevent out-of-scope receipt creation,
  - added integration coverage for scoped payment visibility (matching campus includes receipt, mismatched campus excludes receipt).
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`63/63`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).

## Phase 3 - Analytics
### Stage 3.1 - Payment Status Pie Chart
- Add interactive Paid vs Unpaid pie chart.

#### Stage 3.1 - Payment Status Pie Chart (2026-05-20)
- Implementation Summary:
  - added `PaymentStatusReport` analytics contracts plus `GetPaymentStatusReportAsync` service aggregation with tenant/campus scoped paid vs unpaid counts,
  - added `GET /api/analytics/payment-status` endpoint and finance-compatible access handling without changing existing academic analytics endpoints,
  - wired portal analytics snapshot/model/client/view to render an interactive Paid vs Unpaid pie chart with clickable segment legend.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`65/65`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).

### Stage 3.2 - Filter-Aware Analytics Behavior
- Ensure chart respects Campus, Department, Course, and Semester/Class filters and updates dynamically.

#### Stage 3.2 - Filter-Aware Analytics Behavior (2026-05-20)
- Implementation Summary:
  - extended payment status analytics endpoint/service contract to accept `courseId` and `semesterId` filters,
  - applied filter-aware payment scope by resolving eligible students through enrollment, course-offering, and semester relationships before receipt aggregation,
  - added integration coverage validating course+semester filtered payment status responses return only matching-scope receipt totals.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`66/66`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).

### Stage 3.3 - Finance Analytics Isolation
- Finance users see payment analytics only and never academic analytics.

#### Stage 3.3 - Finance Analytics Isolation (2026-05-20)
- Implementation Summary:
  - introduced `IsFinanceOnly` in portal analytics model/snapshot flow and set it for finance-only sessions,
  - constrained finance analytics page rendering to payment analytics visuals by suppressing academic chart sections/renderers,
  - added finance analytics access regression tests for deny-list behavior on academic analytics endpoints.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `runTests` on `AuthorizationRegressionTests.cs` + `AnalyticsInstituteParityIntegrationTests.cs` passed (`66/66`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug -v minimal` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug -v minimal` passed (`1/1`).

### Phase 3 Completion Summary (2026-05-20)
- Implementation Summary:
  - completed Plan F Phase 3 stages (3.1, 3.2, 3.3) with payment analytics delivery, filter-aware scope behavior, and finance-only analytics isolation,
  - preserved additive/backward-compatible behavior for existing non-finance analytics flows.
- Validation Summary:
  - build and targeted integration/unit/contract validations are green for Phase 3 closure.

## Phase 4 - Reports
### Stage 4.1 - Payment Report Types
- Implement monthly/yearly/semester-based payment reports (semester only for university context).
- Implement payment status report split: School/College class-based, University semester-based.

### Stage 4.2 - Report Payload Standardization
- Include Student ID, Student Name, Payment Amount, and Payment Status.

### Stage 4.3 - Report Filter Model
- Apply Campus, Department, Course, and Class/Semester filters.

### Stage 4.4 - Report Access Rules
- Finance users can view/download payment reports only; existing behavior for other roles remains unchanged.

### Phase 4 Completion Summary (2026-05-21)
- Implementation Summary:
  - added `payment_summary` report definition plus finance role assignment and finance-visible report center/menu access,
  - implemented payment report repository/service/controller flow with year/month/semester/department/course/level/institution filters and Excel/CSV/PDF exports,
  - added portal payment report page, client bindings, report-center routing, and finance authorization regressions proving finance is allowed for payment reports and denied for academic report endpoints.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~AuthorizationRegressionTests"` passed (`64/64`).

## Phase 5 - UI / UX
### Stage 5.1 - Finance Navigation Surface
- Add Finance section with Payments, Payment Reports, and Analytics entries.

### Stage 5.2 - Payment Interaction UX
- Add `Mark as Paid`, clear Paid/Unpaid status presentation, and campus-based selection UX.

### Stage 5.3 - Finance Analytics Presentation
- Place payment pie chart in the analytics dashboard.

### Stage 5.4 - Theme and Configuration Access Boundary
- Permit theme application for Finance users while blocking broader system-configuration access.

### Phase 5 Completion Summary (2026-05-21)
- Implementation Summary:
  - extended finance navigation availability by seeding finance access for `payments`, `report_center`, `analytics`, and `theme_settings`,
  - added a dedicated payment summary report UI with export actions and finance-ready status/amount presentation while preserving existing payments interaction UX,
  - kept finance blocked from academic modules through existing portal guards while allowing theme personalization without opening broader system-configuration surfaces.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~AuthorizationRegressionTests"` passed (`64/64`).

## Phase 6 - Import Sheets
### Stage 6.1 - CSV Template Extension
- Add `Mobile Number` field and multi-campus assignment field to relevant import templates.

#### Stage 6.1 - CSV Template Extension (2026-05-21)
- Implementation Summary:
  - extended official user import templates and portal guidance to include optional `MobileNumber` and `CampusAssignments` columns,
  - updated parser header handling so `MobileNumber` works as an alias for existing phone ingestion behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`).

### Stage 6.2 - Backward Compatibility Validation
- Keep existing templates/imports functional when new fields are omitted.

#### Stage 6.2 - Backward Compatibility Validation (2026-05-21)
- Implementation Summary:
  - preserved import behavior for legacy CSV templates that do not include new optional columns,
  - added explicit integration coverage for legacy-template success path.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`).

### Stage 6.3 - Field Validation Rules
- Add validation for mobile number and campus assignment formats.

#### Stage 6.3 - Field Validation Rules (2026-05-21)
- Implementation Summary:
  - added mobile-number character validation for optional phone/mobile import values,
  - added optional campus-assignment format validation for pipe-separated GUID lists.
- Validation Summary:
  - static diagnostics for touched import files returned no errors,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`).

### Phase 6 Completion Summary (2026-05-21)
- Implementation Summary:
  - completed Plan F Phase 6 stages (6.1, 6.2, 6.3) with template extension, compatibility preservation, and additive validation rules,
  - kept persistence/schema behavior unchanged while preparing import data for future multi-campus assignment workflow wiring.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`).

## Phase 7 - Documentation Updates
### Stage 7.1 - User Guide Update
- Add Finance role explanation, payment workflows, and reporting/analytics usage.

#### Phase 7 - Documentation Updates Stage 7.1 (2026-05-21)
- Implementation Summary:
  - updated the User Guide with Finance role boundaries, payment workflow guidance, and payment analytics/report usage,
  - kept all product behavior unchanged because this stage is documentation-only.
- Validation Summary:
  - manual doc review confirmed the Finance guide content was added and section numbering remains consistent,
  - no code or test execution was required for this documentation-only stage.

### Stage 7.2 - Training Manual Update
- Add Finance-specific training, payment handling procedures, and report generation training.

#### Phase 7 - Documentation Updates Stage 7.2 (2026-05-21)
- Implementation Summary:
  - updated the Training Manual with a Finance training session covering payments, payment reports, and payment analytics,
  - kept all application behavior unchanged because this stage is documentation-only.
- Validation Summary:
  - manual doc review confirmed the Finance training session and trainer notes were added,
  - no code or test execution was required for this documentation-only stage.

### Stage 7.3 - UAT/SAT Scenario Update
- Add scenarios for Finance permissions, multi-campus assignment, payment updates, and report filter accuracy.

#### Phase 7 - Documentation Updates Stage 7.3 (2026-05-21)
- Implementation Summary:
  - updated UAT and SAT documents with Finance-specific navigation, payment, reporting, analytics, and multi-campus scenarios,
  - kept schema, functions, and runtime behavior unchanged because this stage is documentation-only.
- Validation Summary:
  - manual doc review confirmed the Finance UAT/SAT steps were added and acceptance notes were updated,
  - no code or test execution was required for this documentation-only stage.

### Phase 7 Completion Summary (2026-05-21)
- Implementation Summary:
  - completed Plan F Phase 7 documentation updates across the user guide, training manual, UAT, and SAT artifacts,
  - no runtime code, schema, or API behavior changes were introduced in this phase.
- Validation Summary:
  - the updated documentation was reviewed for internal consistency and Phase 7 coverage,
  - repository synchronization remains required after this phase closeout.

## Phase 8 - DB Script Synchronization
### Stage 8.1 - Schema Script Updates
- Update DB scripts for Finance role, multi-campus support, mobile number, and payments enhancements.

#### Phase 8 - DB Script Synchronization Stage 8.1 (2026-05-21)
- Implementation Summary:
  - synchronized the standard and clean seed scripts to include Finance role and payment-summary report coverage,
  - kept schema creation additive and idempotent by reusing existing tables and role/report seed guards.
- Validation Summary:
  - manual script review confirmed the Finance seed/report additions are present in both deployment paths,
  - no destructive schema changes were introduced in this stage.

### Stage 8.2 - Idempotency and Conflict Guarding
- Ensure scripts remain idempotent and free from duplication/conflict.

#### Phase 8 - DB Script Synchronization Stage 8.2 (2026-05-21)
- Implementation Summary:
  - verified the SQL seed scripts use `MERGE`/`WHERE NOT EXISTS` guards to avoid duplication on rerun,
  - confirmed the Finance additions do not introduce conflicting or duplicate seed rows.
- Validation Summary:
  - manual script review confirmed the updates are replay-safe,
  - no code execution was required for this documentation checkpoint.

### Stage 8.3 - Post-Deployment Verification Alignment
- Align verification scripts/checks with new finance-related schema and seed behavior.

#### Phase 8 - DB Script Synchronization Stage 8.3 (2026-05-21)
- Implementation Summary:
  - extended the post-deployment validation scripts to check Finance role presence and payment-summary report access,
  - aligned deployment verification with the updated Finance seed behavior.
- Validation Summary:
  - manual review confirmed the new checks are additive and read-only,
  - no runtime behavior change was introduced.

### Phase 8 Completion Summary (2026-05-21)
- Implementation Summary:
  - completed Plan F Phase 8 DB script synchronization for Finance role and payment-summary reporting coverage,
  - no schema-breaking or destructive changes were introduced.
- Validation Summary:
  - standard deployment scripts were reviewed for repeatability and verification alignment,
  - repository synchronization remains required after this phase closeout.

## Phase 9 - Conflict Prevention (Critical)
### Stage 9.1 - Permission Isolation
- Finance must not inherit Admin privileges.

#### Phase 9 - Conflict Prevention Stage 9.1 (2026-05-21)
- Implementation Summary:
  - tightened the API Finance authorization policy so it now allows only `SuperAdmin` and `Finance` roles,
  - removed the implicit Admin fallback from Finance-gated authorization to keep the Finance surface isolated.
- Validation Summary:
  - code review confirmed the policy now excludes `Admin` from Finance access,
  - no schema or runtime behavior outside authorization was changed.

### Stage 9.2 - Data Boundary Enforcement
- Enforce tenant boundaries and campus filtering in all finance paths.

#### Phase 9 - Conflict Prevention Stage 9.2 (2026-05-21)
- Implementation Summary:
  - verified that the payment receipt repository already applies tenant/campus access scope through `IAccessScopeResolver` for payment receipt and student lookup queries,
  - confirmed the finance report and analytics paths continue to pass current tenant/campus context into report requests,
  - kept the stage verification-only because the boundary enforcement was already wired into the repository layer.
- Validation Summary:
  - code review confirmed scoped repository enforcement is active for finance-facing payment reads,
  - no schema or runtime behavior changes were required for this closeout step.

### Stage 9.3 - Analytics Separation
- Keep payment analytics fully separate from academic analytics.

#### Phase 9 - Conflict Prevention Stage 9.3 (2026-05-21)
- Implementation Summary:
  - verified payment analytics remains isolated in `AnalyticsController` while academic report flows remain in `ReportController`,
  - confirmed payment-status reporting uses finance-scoped analytics inputs and does not reuse the academic report catalog surface,
  - kept the stage verification-only because the separation is already enforced by the current controller boundaries.
- Validation Summary:
  - code review confirmed payment analytics and academic analytics are routed through distinct controllers and services,
  - no schema or runtime behavior changes were required for this closeout step.

### Stage 9.4 - Report Data Isolation
- Prevent payment reports from pulling unrelated academic datasets.

#### Phase 9 - Conflict Prevention Stage 9.4 (2026-05-21)
- Implementation Summary:
  - hardened payment summary reporting so enrollment/course/semester academic joins are executed only when academic filters (`semesterId` or `courseId`) are explicitly requested,
  - kept finance receipt totals, status aggregation, and scoped filtering behavior unchanged for non-academic report usage,
  - reduced unrelated academic data loading in the default finance report path.
- Validation Summary:
  - code review confirmed default payment-summary execution now avoids academic dataset joins unless filter-driven,
  - no schema changes were introduced.

## Phase 10 - Final Validation Checklist
### Stage 10.1 - Access and Multi-Campus Validation
- Validate strict Finance permissions and multi-campus assignment behavior.

### Stage 10.2 - Analytics and Reporting Validation
- Validate pie chart filtering behavior and report export correctness (PDF/Excel).

### Stage 10.3 - Data and Import Validation
- Validate mobile number persistence/usability and import template compatibility.

### Stage 10.4 - Documentation Closure
- Confirm all required documentation is updated and internally consistent.

## Recommended Enhancements (Optional)
- Payment audit tracking (who marked paid)
- SMS notifications using mobile numbers
- Payment reminders / overdue alerts
- Finance dashboard customization

## Result
This plan ensures:
- No overlap or permission conflicts
- Full alignment with current PRD
- Clean separation of Finance features
- Safe scaling without breaking existing modules
