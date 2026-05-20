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

### Stage 3.3 - Finance Analytics Isolation
- Finance users see payment analytics only and never academic analytics.

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

## Phase 5 - UI / UX
### Stage 5.1 - Finance Navigation Surface
- Add Finance section with Payments, Payment Reports, and Analytics entries.

### Stage 5.2 - Payment Interaction UX
- Add `Mark as Paid`, clear Paid/Unpaid status presentation, and campus-based selection UX.

### Stage 5.3 - Finance Analytics Presentation
- Place payment pie chart in the analytics dashboard.

### Stage 5.4 - Theme and Configuration Access Boundary
- Permit theme application for Finance users while blocking broader system-configuration access.

## Phase 6 - Import Sheets
### Stage 6.1 - CSV Template Extension
- Add `Mobile Number` field and multi-campus assignment field to relevant import templates.

### Stage 6.2 - Backward Compatibility Validation
- Keep existing templates/imports functional when new fields are omitted.

### Stage 6.3 - Field Validation Rules
- Add validation for mobile number and campus assignment formats.

## Phase 7 - Documentation Updates
### Stage 7.1 - User Guide Update
- Add Finance role explanation, payment workflows, and reporting/analytics usage.

### Stage 7.2 - Training Manual Update
- Add Finance-specific training, payment handling procedures, and report generation training.

### Stage 7.3 - UAT/SAT Scenario Update
- Add scenarios for Finance permissions, multi-campus assignment, payment updates, and report filter accuracy.

## Phase 8 - DB Script Synchronization
### Stage 8.1 - Schema Script Updates
- Update DB scripts for Finance role, multi-campus support, mobile number, and payments enhancements.

### Stage 8.2 - Idempotency and Conflict Guarding
- Ensure scripts remain idempotent and free from duplication/conflict.

### Stage 8.3 - Post-Deployment Verification Alignment
- Align verification scripts/checks with new finance-related schema and seed behavior.

## Phase 9 - Conflict Prevention (Critical)
### Stage 9.1 - Permission Isolation
- Finance must not inherit Admin privileges.

### Stage 9.2 - Data Boundary Enforcement
- Enforce tenant boundaries and campus filtering in all finance paths.

### Stage 9.3 - Analytics Separation
- Keep payment analytics fully separate from academic analytics.

### Stage 9.4 - Report Data Isolation
- Prevent payment reports from pulling unrelated academic datasets.

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
