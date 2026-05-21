## 2026-05-21 Update - Plan G Phase 2 Stage 2.2 (College Calculation Path)

- Implementation Summary:
	- Documented the requirement to implement percentage-based calculation for colleges (aligned to the school path) and return Percentage + Grade.
	- No schema/table/column/index/constraint or migration changes were introduced; this stage is documentation-only and sets the college calculation path requirement.
- Validation Summary:
	- Manual review confirmed the college calculation path requirement is documented and no implementation or schema changes were made.
	- No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan G Phase 1 Stage 1.3 (Detection Contract)

- Implementation Summary:
  - Documented the requirement to define deterministic calculation-mode selection using both license enablement and department context.
  - No schema/table/column/index/constraint or migration changes were introduced; this stage is documentation-only and sets the detection contract requirement.
- Validation Summary:
  - Manual review confirmed the detection contract requirement is documented and no implementation or schema changes were made.
  - No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan G Phase 1 Stage 1.2 (Institute Type Detection)

- Implementation Summary:
	- Documented the requirement to detect the enabled institute type (School, College, University) at runtime based on the parsed license.
	- No schema/table/column/index/constraint or migration changes were introduced; this stage is documentation-only and sets the detection requirement.
- Validation Summary:
	- Manual review confirmed the detection requirement is documented and no implementation or schema changes were made.
	- No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan G Phase 1 Stage 1.1 (License Parsing)

- Implementation Summary:
	- Documented the requirement to parse enabled institute types (School, College, University) from the license file for future conditional logic.
	- No schema/table/column/index/constraint or migration changes were introduced; this stage is documentation-only and sets the parsing requirement.
- Validation Summary:
	- Manual review confirmed the parsing requirement is documented and no implementation or schema changes were made.
	- No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan G Phase 2 Stage 2.1 (School Calculation Path)

- Implementation Summary:
  - Documented the requirement to implement marks-based percentage calculation for schools and return Percentage + Grade.
  - No schema/table/column/index/constraint or migration changes were introduced; this stage is documentation-only and sets the calculation path requirement.
- Validation Summary:
  - Manual review confirmed the calculation path requirement is documented and no implementation or schema changes were made.
  - No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

# Database Schema Documentation
## University Portal & License Creation Tool

**Version:** 1.2  
**Aligned With PRD:** v1.33  
**Purpose:** Define database schemas for the University Portal Application and the License Creation Tool  

## Institute Parity Stage Documentation Rule (2026-05-13)

After every completed stage in `Docs/Institute-Parity-Issue-Fix-Phases.md`:
- Add `Implementation Summary` and `Validation Summary` in the stage record.
- Update this schema document with explicit statement of schema impact:
	- `No schema mutation` or
	- `Schema updated` with table/column/index/migration details.

Placement rule: put Implementation Summary and Validation Summary at the end of each phase section (not at the start or end of the document).

## 2026-05-21 Update - Plan F Phase 10 Stage 10.1 Access and Multi-Campus Validation

### Plan F Phase 10 Stage 10.1 - Access and Multi-Campus Validation
- Implementation Summary:
	- aligned Finance policy test expectations with existing API authorization policy behavior,
	- validated tenant/campus payment scope behavior using existing schema relationships without data-model changes.
- Validation Summary:
	- `runTests` targeted validation passed (`97/97`) across authorization and payment-scope unit/integration suites,
	- no database scripts, migrations, or schema update actions were required.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan F Phase 10 Stage 10.2 Analytics and Reporting Validation

### Plan F Phase 10 Stage 10.2 - Analytics and Reporting Validation
- Implementation Summary:
	- added integration validation for payment-summary Excel/PDF export metadata contracts,
	- validated course/semester filter-aware payment analytics behavior using existing schema relationships and scoped query paths.
- Validation Summary:
	- `runTests` targeted validation passed (`33/33`) across analytics parity and report export integration suites,
	- no database scripts, migrations, or schema update actions were required.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan F Phase 10 Stage 10.3 Data and Import Validation

### Plan F Phase 10 Stage 10.3 - Data and Import Validation
- Implementation Summary:
	- added integration validation for imported mobile/phone persistence on created users,
	- added compatibility validation for legacy `PhoneNumber` CSV templates within existing import flow.
- Validation Summary:
	- `runTests` targeted validation passed (`6/6`) for user import integration coverage,
	- no database scripts, migrations, or schema update actions were required.
- Schema impact: `No schema mutation`.
- EF migration impact: none.



## 2026-05-21 Update - Plan G Phase 0 Stage 0.2 (Conditional-Layer-Only Contract)

- Implementation Summary:
	- Defined the conditional-layer-only contract: Only a conditional decision layer may be added over existing calculation paths; no direct modification of GPA/CGPA, lifecycle, or report logic is allowed.
	- No schema/table/column/index/constraint or migration changes were introduced; this stage is a governance and safety declaration only.
- Validation Summary:
	- Manual review confirmed no direct modification of GPA/CGPA, lifecycle, or report logic.
	- No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan G Phase 0 Stage 0.3 (Compatibility Defaults)

- Implementation Summary:
	- Established compatibility defaults: Full backward compatibility is enforced, and University GPA behavior is the default when no condition applies.
	- No schema/table/column/index/constraint or migration changes were introduced; this stage is a governance and compatibility declaration only.
- Validation Summary:
	- Manual review confirmed backward compatibility and default behavior are preserved.
	- No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.


- Implementation Summary:
	- Declared the protected surface for result calculation: GPA/CGPA logic, lifecycle workflows, storage structure, and report logic are now explicitly frozen against direct modification unless a future phase requires it.
	- No schema/table/column/index/constraint or migration changes were introduced; this stage is a governance declaration only.
- Validation Summary:
	- Manual review confirmed all GPA/CGPA, lifecycle, and report logic remain unchanged.
	- No database scripts or automated schema tests were required; this stage is documentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.


### Plan F Phase 10 Stage 10.4 - Documentation Closure
- Implementation Summary:
	- completed Phase 10 governance documentation synchronization and consistency closure,
	- verified Stage 10.1 through 10.4 references and completion markers are aligned.
- Validation Summary:
	- manual cross-document consistency review passed,
	- no database scripts, migrations, or schema update actions were required.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan F Phase 7 Documentation Synchronization

### Plan F Phase 7 - Documentation Updates
- Implementation Summary:
	- synchronized Finance documentation references across the user guide, training manual, UAT, and SAT artifacts,
	- no schema/table/column/index/constraint or migration changes were introduced.
- Validation Summary:
	- manual review confirmed this phase was documentation-only,
	- no database scripts or automated schema tests were required.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan F Phase 8 DB Script Synchronization

### Plan F Phase 8 - DB Script Synchronization
- Implementation Summary:
	- synchronized the standard and clean seed/check scripts to include Finance role seeding and payment-summary report validation,
	- no table/column/index/constraint or migration changes were introduced.
- Validation Summary:
	- manual review confirmed the database-related script updates are additive and idempotent,
	- no database migrations or automated schema tests were required.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan F Phases 4 and 5

### Plan F Phases 4 and 5 - Payment Reports and Finance UI Surface
- Implementation Summary:
	- implemented finance payment reporting and finance navigation/report-access behavior using existing payment receipt, student, enrollment, course, semester, department, and user scope relations,
	- no table/column/index/constraint or migration changes were required for this delivery.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~AuthorizationRegressionTests"` passed (`64/64`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 3 Stage 3.2

### Plan F Phase 3 Stage 3.2 - Filter-Aware Analytics Behavior
- Implementation Summary:
	- extended payment analytics filtering logic to include course/semester scope via enrollment-aware query behavior,
	- reused existing academic/payment relational schema without table/column/index/constraint changes.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`66/66`),
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 3 Stage 3.3

### Plan F Phase 3 Stage 3.3 - Finance Analytics Isolation
- Implementation Summary:
	- added finance-only analytics presentation flags in web model/controller snapshot flow,
	- restricted finance analytics page rendering to payment analytics visuals while keeping existing academic analytics for non-finance roles,
	- added integration authorization checks for finance-denied academic analytics endpoints.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
	- `runTests` targeted integration suites passed (`66/66`) for authorization + analytics parity,
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug -v minimal` passed (`158/158`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug -v minimal` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 3 Stage 3.1

### Plan F Phase 3 Stage 3.1 - Payment Status Pie Chart
- Implementation Summary:
	- implemented payment analytics aggregation and endpoint/web integration for paid vs unpaid reporting,
	- reused existing payment receipt/domain schema without table/column/index/constraint changes.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`65/65`),
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 2 Stage 2.3

### Plan F Phase 2 Stage 2.3 - Tenant and Campus Enforcement
- Implementation Summary:
	- enforced tenant/campus boundaries in payment repository queries and finance create-flow scope checks,
	- no table/column/index/constraint shape changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`63/63`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 2 Stage 2.2

### Plan F Phase 2 Stage 2.2 - Finance Restriction Scope
- Implementation Summary:
	- implemented payment deletion restriction and finance academic-access guardrails at API/web layers,
	- no table/column/index/constraint shape changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`54/54`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 2 Stage 2.1

### Plan F Phase 2 Stage 2.1 - Finance Payment Edit Capability
- Implementation Summary:
	- added finance payment edit behavior at the application/domain/API/UI layers,
	- no table/column/index/constraint shape changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 0 Stage Execution

### Stage 0.1 - Baseline Safety Verification
- Implementation Summary:
- executed Plan F baseline safety gate before finance feature implementation,
- no table/column/index/migration updates were required.
- Validation Summary:
- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Stage 0.2 - Isolation and Access Invariants
- Implementation Summary:
- verified tenant/campus isolation and role-access invariants under current baseline,
- no table/column/index/migration updates were required.
- Validation Summary:
- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Stage 0.3 - Additive-Only Guardrails
- Implementation Summary:
- finalized additive-only guardrails for Plan F implementation stream,
- no table/column/index/migration updates were required.
- Validation Summary:
- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.
## 2026-05-20 Update - Plan F Phase 1 Stage Execution

### Plan F Phase 1 Stage 1.4 - Payment Record State Model (2026-05-20)
- Implementation Summary:
	- completed payment state-model exposure for paid/unpaid tracking and update trail at DTO/view layer,
	- no table/column/index/constraint shape changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`156/156`),
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Plan F Phase 1 Stage 1.3 - Finance Role Seed and Linking (2026-05-20)
- Implementation Summary:
	- added `Finance` role to startup role-seed set in application seeding flow,
	- no table/column/index/constraint shape changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~InstitutionPolicyTests|FullyQualifiedName~UserImport" -v minimal` passed (`25/25`),
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
	- `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Plan F Phase 1 Stage 1.1 - Functional Non-Regression Validation
- Implementation Summary:
- executed validation-only non-regression checkpoint,
- no table/column/index/constraint changes were required.
- Validation Summary:
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
- unit tests passed (`151/151`),
- integration tests passed (`244/244`),
- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Plan F Phase 1 Stage 1.2 - Additional Database Updates (2026-05-20)
- Implementation Summary:
  - Implemented additional database updates as part of Plan F Phase 1.
  - No schema changes were required.
- Validation Summary:
  - Build succeeded: `dotnet build Tabsan.EduSphere.sln -c Release -v minimal`.
  - Unit tests passed: 151/151.
  - Integration tests passed: 244/244.
  - Contract tests passed: 1/1.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Plan F Phase 1 Stage 1.3 - End-to-End Module Validation
- Implementation Summary:
- executed validation-only module end-to-end checkpoint,
- no table/column/index/constraint changes were required.
- Validation Summary:
- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
- integration tests passed (`244/244`),
- unit tests passed (`151/151`),
- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Plan F Phase 1 Stage 1.4 - API Response and Runtime Stability
- Implementation Summary:
- executed validation-only API/runtime stability checkpoint,
- no table/column/index/constraint changes were required.
- Validation Summary:
- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
- integration tests passed (`244/244`),
- contract tests passed (`1/1`),
- unit tests passed (`151/151`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Plan F Phase 1 Stage 1.5 - Database Relationship Validation
- Implementation Summary:
- executed validation-only database relationship integrity checkpoint,
- no table/column/index/constraint changes were required.
- Validation Summary:
- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
- integration tests passed (`244/244`),
- unit tests passed (`151/151`),
- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 1 (Charting Framework & UI)

- Recent request issue:
	- complete Plan D Phase 1 and keep summaries at phase-end placement.
- Implementation Summary:
	- implemented front-end analytics charting layout and clickable legends,
	- no table/column/index/migration updates were required.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- unit tests passed (`151/151`),
	- integration tests passed (`241/241`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 2 Stage 2.1 (Global Filters)

- Recent request issue:
	- proceed to Stage 2.1 and implement global analytics filters.
- Implementation Summary:
	- added course/semester filter plumbing for analytics query scope,
	- no table/column/index/migration updates were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 2 Stage 2.2 (Dependent Filtering)

- Recent request issue:
	- proceed to Stage 2.2 and enforce dependent filter behavior.
- Implementation Summary:
	- added dependent filter cascade logic and offering metadata mapping for Analytics UI,
	- no table/column/index/migration updates were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 2 Stage 2.3 (Instant Charts Update)

- Recent request issue:
	- proceed to Stage 2.3 and make analytics charts update instantly.
- Implementation Summary:
	- implemented web-layer snapshot endpoint and client-side in-place rendering updates,
	- no relational schema changes and no migration changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 3 Stage 3.1 (Chart Types and Data)

- Recent request issue:
	- proceed to Stage 3.1 chart expansion.
- Implementation Summary:
	- added additional analytics chart renderers and layout containers in web UI,
	- no table, index, relation, or migration changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 4 Stage 4.1 (Tenant/Campus Isolation)

- Recent request issue:
	- proceed to Stage 4.1 tenant/campus analytics isolation hardening.
- Implementation Summary:
	- applied application-layer analytics query and cache partition scoping by tenant/campus claims,
	- no table/index/constraint/migration changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`66/66`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 4 Stage 4.2 (Leakage Prevention)

- Recent request issue:
	- proceed to Stage 4.2 and prevent broader cross-tenant/campus leakage.
- Implementation Summary:
	- hardened export-job access control and scope metadata checks in application/API layer,
	- no relational schema or migration changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 5 Stage 5.1 (Performance Optimization)

- Recent request issue:
	- proceed to Stage 5.1 analytics query optimization.
- Implementation Summary:
	- optimized analytics report query execution patterns in application/infrastructure layer,
	- no schema, table, index, or migration changes were required in this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 5 Stage 5.2 (Index and Data-Loading Refinement)

- Recent request issue:
	- proceed to Stage 5.2 and implement analytics-supporting index strategy.
- Implementation Summary:
	- added Stage 5.2 migration `PlanDPhase5Stage52AnalyticsIndexes` with analytics-oriented indexes,
	- refined `assignment_submissions.Status` column to bounded length (`nvarchar(32)`) for index efficiency.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `Index and column-length refinement only`.
- EF migration impact: `20260520002652_PlanDPhase5Stage52AnalyticsIndexes` added.

## 2026-05-20 Update - Plan D Phase 6 Stage 6.1 (Validation and UI Consistency)

- Recent request issue:
	- proceed to Stage 6.1 validation and consistency checks.
- Implementation Summary:
	- executed validation-only analytics checkpoint for interactivity/filtering/UI consistency,
	- no additional schema modifications were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan D Phase 6 Stage 6.2 (Final Performance and Security Review)

- Recent request issue:
	- proceed to Stage 6.2 final review for analytics performance and security.
- Implementation Summary:
	- executed final validation-only checkpoint for release readiness,
	- no additional schema changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.1 (Functional Non-Regression Validation)

- Recent request issue:
	- there is no Phase 7 continuation in this execution stream; move to Plan E and start Stage 1.1.
- Implementation Summary:
	- executed validation-only non-regression checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- unit tests passed (`151/151`),
	- integration tests passed (`244/244`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.2 (End-to-End Module Validation)

- Recent request issue:
	- proceed to Plan E Phase 1 Stage 1.2.
- Implementation Summary:
	- executed validation-only module end-to-end checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.3 (UI Alignment, Bindings, and Form Stability)

- Recent request issue:
	- proceed to Plan E Phase 1 Stage 1.3.
- Implementation Summary:
	- executed validation-only UI/layout/binding/form checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.4 (API Response and Runtime Stability)

- Recent request issue:
	- proceed to Plan E Phase 1 Stage 1.4.
- Implementation Summary:
	- executed validation-only API/runtime stability checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- contract tests passed (`1/1`),
	- unit tests passed (`151/151`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 1 Stage 1.5 (Database Relationship Validation)

- Recent request issue:
	- proceed to Plan E Phase 1 Stage 1.5.
- Implementation Summary:
	- executed validation-only database relationship integrity checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 2 Stage 2.1 (Tenant and Campus Isolation)

- Recent request issue:
	- proceed to Plan E Phase 2 Stage 2.1.
- Implementation Summary:
	- executed validation-only tenant/campus isolation checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 2 Stage 2.2 (Cross-Tenant/Campus Leakage Validation)

- Recent request issue:
	- proceed to Plan E Phase 2 Stage 2.2.
- Implementation Summary:
	- executed validation-only cross-tenant/campus leakage checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 2 Stage 2.3 (Tenant/Campus Query Scope Validation)

- Recent request issue:
	- proceed to Plan E Phase 2 Stage 2.3.
- Implementation Summary:
	- executed validation-only TenantId/CampusId query-scope checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.1 (Course Material End-to-End Validation)

- Recent request issue:
	- proceed with next stage.
- Implementation Summary:
	- executed validation-only Course Material end-to-end checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- targeted Course Material integration tests passed (`5/5`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.2 (Analytics Charts and Filters Validation)

- Recent request issue:
	- proceed with next stage.
- Implementation Summary:
	- executed validation-only analytics charts/filters checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- targeted analytics/authorization integration tests passed (`68/68`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.3 (Tenant and Campus Management Validation)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed validation-only tenant/campus management checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- targeted tenant/campus management integration tests passed (`63/63`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 3 Stage 3.4 (Role-Based Access Validation)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed validation-only role-based access checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.1 (UI Consistency and Design Baseline Validation)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed validation-only UI consistency/design baseline checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- targeted UI-related integration tests passed (`71/71`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.2 (Sidebar Header and Content Structure Validation)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed validation-only sidebar/header/content structure checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- targeted UI-related integration tests passed (`71/71`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.3 (Overlap and Responsive Layout Validation)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed validation-only overlap/responsive layout checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- targeted UI-related integration tests passed (`71/71`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 4 Stage 4.4 (Validate All Buttons and Actions)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed validation-only button/action continuity checkpoint,
	- no table/column/index/constraint changes were required.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.1 (TenantId/CampusId Schema Audit)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed schema audit on `Scripts/01-Schema-Current.sql` for `TenantId` and `CampusId` usage,
	- parsed `82` tables; `0` tables contain both `TenantId` and `CampusId` columns in the current script,
	- validated that tenant/campus scoping is currently represented in application/domain logic; no table/column/index/constraint changes were applied.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.2 (Foreign Keys, Indexes, and Constraints Audit)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed SQL artifact audit on `Scripts/01-Schema-Current.sql` and `Scripts/04-Maintenance-Indexes-And-Views.sql`,
	- recorded `65` foreign key constraints (`5` added via `ALTER TABLE`), `82` primary key constraints, `2` default constraints, and `190` index statements across audited scripts,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.3 (Nullable Field Audit)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed nullable-field audit on `Scripts/01-Schema-Current.sql`,
	- recorded `280` nullable columns across `79` tables and identified `2` review-candidate nullable fields (`users.Email`, `timetable_entries.FacultyName`),
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 5 Stage 5.4 (Data Integrity and Migration Safety)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed data-integrity and migration-safety audit on `Scripts/01-Schema-Current.sql`, `Scripts/05-PostDeployment-Checks.sql`, and `Scripts/05-PostDeployment-Checks-Clean.sql`,
	- recorded `365` migration-history guard references, `324` `IF NOT EXISTS` guards, `40` transaction blocks, `175` post-deployment verification `SELECT` checks, and `19` EF migration files,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 6 Stage 6.1 (Role-Based Access Review)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed role-based access audit across API/Web authorization attributes, role/policy enforcement code, and seed-role SQL artifacts,
	- recorded `359` API `[Authorize]` attributes, `369` role/policy enforcement references, `5` policy registration references, and `105` role-seeding script references,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 6 Stage 6.2 (Unauthorized/Cross-Scope Access)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed unauthorized/cross-tenant/cross-campus access audit across source enforcement points,
	- recorded `1326` isolation-enforcement source hits and `128` explicit `Forbid`/`Unauthorized` enforcement hits,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 6 Stage 6.3 (API Endpoint Restriction)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed API endpoint restriction audit over authorization coverage in `src/Tabsan.EduSphere.API/Controllers`,
	- recorded `447` HTTP endpoints: `92` method-level `[Authorize]`, `349` class-level `[Authorize]` coverage, `1` `[AllowAnonymous]`, and `5` review-set endpoints without explicit authorize coverage,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 7 Stage 7.1 (Query Scope Filtering)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed tenant/campus query-filter audit across source and repository layers,
	- recorded `551` Tenant/Campus scope references, `11` LINQ `Where` scope-filter references, and `20` repository-layer scope references,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 7 Stage 7.2 (Join and Full-Scan Risk Audit)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed query-shape/full-scan risk audit for joins, includes, raw SQL usage, and pagination coverage,
	- recorded `134` join references (`18` in repository layer), `167` include/then-include references, `0` raw SQL query references, `475` materialization references, and `37` pagination references,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 7 Stage 7.3 (Pagination and Analytics Query Efficiency)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed pagination and analytics-query efficiency audit across source and analytics layers,
	- recorded `37` pagination references with `29` nearby ordering safeguards, `551` Tenant/Campus scope references, `11` LINQ scope-filter references, and `18` analytics `AsNoTracking` references,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 8 Stage 8.1 (Environment-Based Configuration)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed environment-based configuration audit across startup/configuration-loading paths,
	- recorded `61` environment-handling references, `132` configuration/deployment references, `54` `appsettings*.json` files, and `127` Program.cs environment/configuration references,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 8 Stage 8.2 (Deployment Scenarios)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed deployment-scenario readiness audit for cloud, on-prem, and multi-instance signals across source startup/configuration paths,
	- recorded `1` cloud reference, `0` on-prem references, `14` multi-instance/scale-out references, `200+` deployment/scaling/instance/tenant-isolation source references (search cap reached), and `9` source `appsettings*.json` files,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 8 Stage 8.3 (Secrets and Configuration Security)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- executed secure-secrets/configuration handling audit across startup guards, environment-variable resolvers, and appsettings templates,
	- recorded `18` secure startup guard references, `18` environment-variable secret/deployment references, `18` secret-sensitive configuration key references in source appsettings, `12` placeholder/template secret markers, and `9` source `appsettings*.json` files,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed (non-blocking warning: CS0105 in API Program.cs),
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 9 Stage 9.1 (Issue and Inconsistency Remediation)

- Recent request issue:
	- proceed to Plan E Phase 9 Stage 9.1 (identify and fix issues, inconsistencies, or risks).
- Implementation Summary:
	- fixed API startup import inconsistency in `Program.cs` by splitting merged `using` directives and removing duplicate import,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full automated test suites passed (`396/396`),
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan E Phase 9 Stage 9.2 (Final Stability, Security, and Scalability Review)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- completed final stability/security/scalability verification with release build, full automated tests, and source risk-marker scan,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- full automated test suites passed (`396/396`),
	- full integration tests passed (`244/244`),
	- unit tests passed (`151/151`),
	- contract tests passed (`1/1`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan F Phase 6 Stage 6.1/6.2/6.3 (User Import Template Extension)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- extended CSV user-import parser to support optional `MobileNumber` alias handling and optional `CampusAssignments`/`CampusIds` format validation,
	- updated distributed import templates and portal import guidance with new optional columns,
	- no table/column/index/constraint changes were applied during this stage.
- Validation Summary:
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`),
	- touched import files reported no static diagnostics errors.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-21 Update - Plan F Phase 6 Completion (Import Sheets)

- Recent request issue:
	- proceed and close Plan F Phase 6 with synchronized stage summaries.
- Implementation Summary:
	- completed Stage 6.1/6.2/6.3 import-sheet extension, compatibility, and validation updates,
	- no table/column/index/constraint changes were applied for phase closure.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Final-Touches Tracker Restoration (Governance)

- Recent request issue:
	- proceed.
- Implementation Summary:
	- restored missing `Project startup Docs/Final-Touches.md` governance tracker required by startup workflow,
	- no table/column/index/constraint changes were applied.
- Validation Summary:
	- verified file presence and execution-pointer consistency with `Docs/Command.md`,
	- no runtime code or schema mutation introduced.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan C Phase 6 Implementation (Performance & Optimization)

- Recent request issue:
	- complete Plan C Phase 6 Stage 6.1 and Stage 6.2.
- Implementation Summary:
	- Stage 6.1 optimized Course Material repository read-query execution without table/column mutation,
	- Stage 6.2 added migration `20260519215715_PlanCPhase6Stage2CourseMaterialIndexTuning`.
- Validation Summary:
	- targeted Course Material authorization regression tests passed (`5/5`),
	- `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Schema impact: `Schema updated`.
- EF migration impact:
	- added index `IX_course_materials_scope_active_sort` on (`TenantId`, `CampusId`, `IsActive`, `Name`, `CreatedAt`).

## 2026-05-20 Update - Plan C Phase 7 Stage 7.1 Validation

- Recent request issue:
	- start Plan C Phase 7 Stage 7.1 validation.
- Implementation Summary:
	- executed full validation sweep for Course Material safety/access/UI behavior,
	- no schema changes were required during this validation stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
	- unit tests passed (`151/151`),
	- integration tests passed (`241/241`),
	- contract tests passed (`1/1`),
	- total automated validations passed (`393/393`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan C Phase 7 Stage 7.2 Finalization

- Recent request issue:
	- complete Plan C Phase 7 Stage 7.2 final review.
- Implementation Summary:
	- completed release-readiness stability/scalability closeout validation for Course Material,
	- no schema changes were required during this final review stage.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
	- Release-mode integration tests (Course Material authorization subset) passed (`5/5`),
	- load-test script run requires available target API and failed due to unreachable local endpoint (`http://localhost:5181`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-20 Update - Plan C Phase 5 Implementation (File & Link Handling)

- Recent request issue:
	- complete Plan C Phase 5 Stage 5.1, Stage 5.2, and Stage 5.3.
- Implementation Summary:
	- added upload/download API and portal integration for Course Material file handling,
	- persisted file keys through existing `course_materials.FilePath` model without new schema objects.
- Validation Summary:
	- targeted Course Material authorization regression tests passed (`5/5`),
	- `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan C Phase 4 Implementation (UI & UX)

- Recent request issue:
	- proceed to Plan C Phase 4 UI and UX implementation.
- Implementation Summary:
	- added web portal UI/controller integration and sidebar entitlement mapping for course materials,
	- no database schema or migration changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan C Phase 3 Implementation (Access Control & Security)

- Recent request issue:
	- proceed to Plan C Phase 3 access control and strict isolation.
- Implementation Summary:
	- added course-material API/service/repository access-control and query-isolation layer,
	- no new migration or table/index/constraint mutation introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan C Phase 2 Implementation (Data Safety & Migration)

- Recent request issue:
	- proceed after Plan C Phase 1.
- Implementation Summary:
	- added migration `20260519055118_PlanCPhase2DataSafetyScopeGuard` to harden `course_materials` data integrity.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `Schema updated`.
- EF migration impact:
	- added constraints: `CK_course_materials_scope_required`, `CK_course_materials_material_type`, `CK_course_materials_location_by_type`.

## 2026-05-19 Update - Plan C Phase 1 Implementation (Domain & Database Extension)

- Recent request issue:
	- start Plan C Phase 1.
- Implementation Summary:
	- added `course_materials` schema foundation with tenant/campus/department/program/semester/course scope columns,
	- added migration `20260519054518_PlanCPhase1CourseMaterialFoundation`.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `Schema updated`.
- EF migration impact:
	- new table: `course_materials`,
	- foreign keys: tenant, campus (composite with tenant), department, academic program, semester, course, created-by user,
	- indexes: tenant/campus indexes and scope lookup indexes for filtered material retrieval.

## 2026-05-19 Update - Plan B Phase 10 Implementation (Validation & Finalization)

- Recent request issue:
	- proceed to validation and finalization after logging and visibility.
- Implementation Summary:
	- completed the final readiness review only,
	- no database schema or migration changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed (`388/388`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 9 Implementation (Logging & Visibility)

- Recent request issue:
	- proceed to logging and visibility after configuration performance and stability.
- Implementation Summary:
	- added safe startup visibility logging across hosts,
	- no database schema or migration changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 8 Implementation (Performance & Stability)

- Recent request issue:
	- proceed to configuration performance and stability improvements.
- Implementation Summary:
	- optimized shared configuration bootstrap registration and reload behavior,
	- no database schema or migration changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 7 Implementation (Fail-Safe Behavior)

- Recent request issue:
	- proceed to fail-safe behavior after tenant-aware configuration support.
- Implementation Summary:
	- added centralized startup fail-safe validation for configuration and deployment settings,
	- no database schema or migration changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 5 Implementation (Customer Deployment Support)

- Recent request issue:
	- proceed to customer deployment support after deployment flexibility.
- Implementation Summary:
	- added deployment-pipeline config support and deployment metadata templates,
	- no database schema or migration changes were introduced.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 4 Implementation (Deployment Flexibility)

- Recent request issue:
	- proceed to deployment flexibility after secure configuration handling.
- Implementation Summary:
	- added deployment-topology resolver and per-customer database-name/domain/scaling metadata,
	- kept all deployment metadata non-secret and log-safe.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 3 Implementation (Secure Configuration Handling)

- Recent request issue:
	- proceed to secure configuration handling after connection-management hardening.
- Implementation Summary:
	- added external configuration source support and secure production validation helper,
	- kept diagnostics source-safe so credentials are never logged.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 2 Implementation (Database Connection Management)

- Recent request issue:
	- proceed to next Plan B phase to harden DB connection management.
- Implementation Summary:
	- added centralized startup DB connection resolver and integrated it into host startup flows,
	- prioritized env/deployment override keys with backward-compatible fallback.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan B Phase 1 Implementation (Configuration Structure)

- Recent request issue:
	- proceed and begin Plan B configuration/deployment phase execution.
- Implementation Summary:
	- implemented shared configuration hierarchy bootstrap for startup hosts,
	- standardized configuration source order in API/Web/BackgroundJobs startup code.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan A Phase 7 Implementation (Validation and Finalization)

- Recent request issue:
	- proceed to Plan A Phase 7 and finalize validation plus stabilization closeout.
- Implementation Summary:
	- performed final build and full automated test validation,
	- completed Plan A closeout governance synchronization.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- full unit tests passed (`151/151`),
	- full integration tests passed (`236/236`),
	- contract tests passed (`1/1`).
- Testing and result summary:
	- total automated validations passed: `388/388`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan A Phase 6 Implementation (Performance and Optimization)

- Recent request issue:
	- proceed to Plan A Phase 6 and optimize tenant/campus scoped query performance.
- Implementation Summary:
	- added scoped query optimization changes in repository predicates,
	- added composite indexes in EF configuration for user and department scoped query paths,
	- added migration `20260519040540_Phase46_TenantCampusQueryOptimization`.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260519040540_Phase46_TenantCampusQueryOptimization`.
	- added index `IX_users_tenant_campus_active_role`.
	- added index `IX_users_tenant_campus_username`.
	- added index `IX_departments_tenant_campus_name`.

## 2026-05-19 Update - Plan A Phase 5 Implementation (Tenant/Campus UI Management Interfaces)

- Recent request issue:
	- proceed to Plan A Phase 5 and add tenant/campus management screens linked to existing menu patterns.
- Implementation Summary:
	- added SuperAdmin API endpoints for tenant/campus lifecycle operations,
	- added web UI and API client wiring for tenant/campus management.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan A Phase 4 Implementation (Access Control and Filtering)

- Recent request issue:
	- proceed to Plan A Phase 4 and enforce tenant/campus read scoping with SuperAdmin bypass.
- Implementation Summary:
	- added request claims-based access-scope resolver,
	- added tenant/campus JWT claims used by repository filtering,
	- added tenant/campus scoped read filters in user/department repositories.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan A Phase 3 Implementation (Compatibility and Safety Hardening)

- Recent request issue:
	- proceed to Plan A Phase 3 and ensure tenant/campus integration cannot enter invalid state.
- Implementation Summary:
	- added migration `20260519034517_Phase43_TenantCampusCompatibilitySafety`,
	- migration adds tenant/campus pair check constraints on `users` and `departments`,
	- migration adds composite tenant-bound campus FK integrity for `users` and `departments`.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260519034517_Phase43_TenantCampusCompatibilitySafety`.
	- adds constraints/indexes/FK integrity hardening (no breaking table replacement).

## 2026-05-19 Update - Plan A Phase 2 Implementation (Default Tenant/Campus Data Integration)

- Recent request issue:
	- proceed to Plan A Phase 2 and ensure existing records are assigned to default tenant/campus safely.
- Implementation Summary:
	- added migration `20260519032844_Phase42_DefaultTenantCampusBackfill` to perform safe data backfill,
	- migration inserts default tenant/campus baseline records if missing,
	- migration updates null tenant/campus on `users` and `departments`.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260519032844_Phase42_DefaultTenantCampusBackfill`.
	- data backfill only (no new table/column/index structures in this phase migration).

## 2026-05-19 Update - Plan A Phase 1 Implementation (Tenant + Campus Domain Foundation)

- Recent request issue:
	- proceed with Plan A Phase 1 implementation and synchronize mandatory tracker documents.
- Implementation Summary:
	- added tenancy foundation entities and mappings,
	- applied migration `Phase41_TenantCampusFoundation` for additive schema expansion.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`) confirming no baseline regression in sampled security/enrollment paths.
- Testing and result summary:
	- build passed,
	- focused unit test filter passed (`9/9`).
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260519031636_Phase41_TenantCampusFoundation`.
	- added table `tenants`.
	- added table `campuses` with FK to `tenants`.
	- added nullable `TenantId` and `CampusId` to `users`.
	- added nullable `TenantId` and `CampusId` to `departments`.
	- added supporting indexes/FKs for tenant/campus lookup and integrity.

## 2026-05-19 Update - Plan A Phase 1 Kickoff (App Configuration: Tenant + Campus)

- Recent request issue:
	- start Plan A Phase 1 and synchronize mandatory governance/planning docs with phase-end implementation and validation summaries.
- Implementation Summary:
	- initiated Plan A Phase 1 (Domain Layer Extension) execution baseline,
	- updated phase document formatting so completion evidence is recorded at phase end,
	- synchronized schema tracker with required planning and product documentation updates.
- Validation Summary:
	- cross-document consistency review completed,
	- verified this kickoff wave is documentation-only and introduces no persistence or migration change.
- Testing and result summary:
	- documentation consistency checks completed,
	- no runtime/schema test execution required for this documentation-only kickoff update.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Deep System Audit + Validation + AI Chatbot Modernization (Phase 1-7)

- Recent request issue:
	- execute phase-by-phase full-system audit/validation and chatbot modernization with governance synchronization after each phase.

### Phase 1 - System Understanding
- Implementation Summary:
	- completed architecture and data-flow review for academic, enrollment, notifications, reports, auth/MFA, and AI chat modules.
- Validation Summary:
	- controller/service/repository and DTO binding checks completed.
- Testing and result summary:
	- phase 1 traceability audit completed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 2 - API and Backend Validation
- Implementation Summary:
	- added AI chat API versioned route compatibility and aligned client route usage,
	- updated middleware enforcement mapping for versioned AI
- Validation Summary:
	- build + targeted tests passed after backend updates.
- Testing and result summary:
	- validated by `dotnet build` plus focused unit/integration suites (`13/13` passed).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 3 - Database and Data Flow Validation
- Implementation Summary:
	- reviewed enrollment/waitlist EF data access and includes in affected flows,
	- confirmed no additional index/table/constraint changes are required.
- Validation Summary:
	- no migration delta introduced by this request.
- Testing and result summary:
	- phase 3 data-integrity check completed without new schema risk.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 4 - UI and Frontend Validation
- Implementation Summary:
	- validated frontend bindings and role-aware navigation behavior for chat-enabled shell.
- Validation Summary:
	- preserved API/data contracts in all touched UI paths.
- Testing and result summary:
	- phase 4 UI binding validation completed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 5 - Performance and Stability Check
- Implementation Summary:
	- completed async/stability sanity review for touched request paths.
- Validation Summary:
	- focused regression tests and build stayed green.
- Testing and result summary:
	- phase 5 stability check completed with no schema-related impact.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 6 - AI Chatbot Redesign (Critical)
- Implementation Summary:
	- modularized chatbot UI into shared components (floating button + chat panel) while preserving existing backend integration.
- Validation Summary:
	- build/tests passed after UI componentization.
- Testing and result summary:
	- phase 6 chatbot modernization validated with unchanged persistence behavior.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 7 - Final Consolidated Reporting
- Implementation Summary:
	- synchronized mandatory governance docs with per-phase implementation and validation/test summaries.
- Validation Summary:
	- all required trackers updated for this execution cycle.
- Testing and result summary:
	- phase 7 reporting completed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - UI/UX Redesign Phase 9 (Final UI Polish)

- Recent request issue:
	- proceed with the final redesign phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- refined shared presentation styles for final polish,
	- preserved all existing data contracts and backend behaviors.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched frontend CSS.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Phase 8 (Responsive Hardening)

- Recent request issue:
	- proceed with responsive-design phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- updated shared CSS and targeted view hooks for responsive behavior improvements,
	- preserved all existing data contracts, actions, and route behavior.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched responsive frontend files.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Phase 7 (AI Chatbot UI)

- Recent request issue:
	- proceed with AI chatbot phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- updated shared layout, CSS, and frontend JS for chatbot presentation and interaction polish,
	- preserved existing backend endpoint usage and request/response contracts.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched chatbot frontend files.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Phase 6 (Branding Pass)

- Recent request issue:
	- proceed with branding phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- updated shared layout and CSS for branding-focused shell/header improvements,
	- improved profile dropdown and notification icon UI treatment without changing data contracts.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched frontend files.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Continuation (Enrollments/Results/Payments + Phase-Level Summary Formatting)

- Recent request issue:
	- continuation requested the next portal-page visual pass and required implementation/validation summaries at each completed redesign phase.
- Implementation Summary:
	- updated `Enrollments`, `Results`, and `Payments` Razor views for visual continuity with the global design system,
	- reformatted `Docs/Improved UI and look.md` so phase completion summaries are phase-local and markdown-lint clean.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched continuation views and redesign document.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Continuation (Students/Courses/Admin Users Polish)

- Recent request issue:
	- continuation requested further frontend page-level visual consistency after the initial redesign.
- Implementation Summary:
	- updated Students, Courses, and Admin Users Razor views and supporting CSS helper classes,
	- retained all existing backend interactions and form endpoints.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- update scope remains presentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Request (Execution Snapshot)

- Recent request issue:
	- the portal required a complete frontend-only UI/UX redesign so the application looks like a premium SaaS product without changing backend behavior or persistence structures.
- Implementation Summary:
	- updated the web layout, dashboard Razor view, site stylesheet, and frontend-only JavaScript to deliver the redesign,
	- preserved all controller/API/model/database behavior and did not introduce schema-affecting code changes.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the redesign,
	- workspace diagnostics reported no errors in the touched frontend files,
	- verified the request is presentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - Documentation Synchronization Request (Execution Snapshot)

- Recent request issue:
	- a follow-up request required synchronized update coverage across PRD, Function List, Complete Functionality Reference, Development Plan, and Database Schema.
- Implementation Summary:
	- added aligned follow-up execution snapshot entries in all five requested documents,
	- normalized issue/implementation/validation wording for consistent governance evidence.
- Validation Summary:
	- verified all five requested documents now contain this follow-up dated entry,
	- verified no table/column/index/constraint/view/stored-procedure/migration changes were introduced by this update.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - Stage 40.1 PhoneNumber/SMS Recipient Dependency Completion (Execution Snapshot)

- Recent request issue:
	- notification SMS dispatch depended on persisted recipient phone numbers, but user profiles did not yet store phone values.
- Implementation Summary:
	- added optional `PhoneNumber` to user domain/EF mapping,
	- wired phone population paths through admin create/update, CSV import optional column handling, and student self-registration,
	- implemented active-user phone lookup in notification repository for SMS recipient resolution.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- `dotnet test Tabsan.EduSphere.sln -v minimal --filter "FullyQualifiedName~Phase28Stage2Tests|FullyQualifiedName~UserImportAndForceChangeIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~StudentRegistration"` passed (`13/13`).
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260518104000_Phase40_AddUserPhoneNumber`.
	- added `users.PhoneNumber` (`nvarchar(32)`, nullable).

- Recent request issue:
	- mandatory planning/execution documents required synchronized closeout updates for the

## 2026-05-18 Update - DeepScan Stage 39.4 EF Relationship and Query-Filter Warning Cleanup (Execution Snapshot)

- Recent request issue:
	- EF startup/runtime emitted warning set for required-relationship and query-filter mismatches, quiz shadow FK mapping conflict, and course enum default sentinel behavior.
- Implementation Summary:
	- updated EF configurations to align dependent filters with filtered required principals,
	- fixed quiz question relationship mapping to remove shadow foreign-key path,
	- removed course enum DB default configuration that caused sentinel warning behavior.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
	- verified targeted EF warning set no longer appears in focused startup/runtime validation output.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Gap Phase/Stage Synchronization Request (Execution Snapshot)

- Recent request issue:
	- DeepScan-identified missing/partial items needed explicit phase/stage planning entries in governance docs, with synchronized schema-impact reporting.
- Implementation Summary:
	- updated schema tracker in sync with PRD, consolidated execution tracker, function list, complete functionality reference, and development plan,
	- recorded that DeepScan remediation planning was added as phase/stage entries in the consolidated tracker.
- Validation Summary:
	- verified all six mandatory documents include this dated synchronization snapshot,
	- verified no table/column/index/constraint/view/stored-procedure/migration changes were introduced by this request.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Stage 39.2 Transactional CSV Import Strict Mode (Execution Snapshot)

- Recent request issue:
	- the user import flow needed an atomic strict mode to avoid partial persistence when a CSV contains mixed-validity rows.
- Implementation Summary:
	- updated the application/service/controller import path to support optional strict-mode rollback behavior,
	- added a strict-mode indicator to the import response payload.
- Validation Summary:
	- targeted integration suite passed for user import and force-change-password flows (`4/4`),
	- verified strict mode produces no persisted rows when mixed-validity input is supplied,
	- verified permissive import remains unchanged.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Stage 39.1 Enrollment Waitlist and Seat-Promotion Workflow (Execution Snapshot)

- Recent request issue:
	- waitlist handling for full course offerings needed to be added to the enrollment workflow.
- Implementation Summary:
	- updated the enrollment aggregate, repository contract, and service flow to support waitlisted records and deterministic promotion,
	- added repository retrieval for ordered waitlisted enrollments.
- Validation Summary:
	- targeted unit suite passed for waitlist creation and promotion (`2/2`),
	- verified no tables, columns, indexes, constraints, or migrations were required for this behavior update.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Stage 39.3 MFA Hardening (TOTP + Recovery Codes) (Execution Snapshot)

- Recent request issue:
	- MFA enforcement required replacement of deployment demo-code checks with per-user TOTP and recovery-code persistence.
- Implementation Summary:
	- updated auth flow and API to support per-user TOTP enrollment and recovery-code lifecycle,
	- added migration-backed user MFA persistence fields.
- Validation Summary:
	- targeted auth unit suite passed (`7/7`),
	- targeted login/force-change integration suite passed (`4/4`).
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260518091500_Phase39_MfaTotpRecoveryCodes`.
	- added `users.MfaIsEnabled` (`bit`, default `0`).
	- added `users.MfaTotpSecret` (`nvarchar(128)`, nullable).
	- added `users.MfaRecoveryCodesHashJson` (`nvarchar(4000)`, nullable).

## 2026-05-13 Update - Institute Parity Stage 0.1 (Execution Snapshot)

- Stage 0.1 completed as a schema/dependency audit baseline.
- Reviewed parity-related schema touchpoints through `ApplicationDbContext` and module repositories, including:
	- policy/config tables (`portal_settings`),
	- academic entities (departments/courses/offerings/enrollments),
	- assessments/results/quizzes,
	- lifecycle and payment entities,
	- timetable/building/room entities.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-14 Update - Phase 23 Stage 23.2 (Execution Snapshot)

- Stage 23.2 completed dynamic academic labels and context validation for policy-aware School/College/University terminology.
- API/application/web updates validated:
	- `ILabelService` and `LabelService` vocabulary resolution behavior,
	- authenticated `GET /api/v1/labels` endpoint in `LabelController`,
	- portal vocabulary consumption in web client and module composition surface.
- Validation evidence:
	- integration suite `DynamicLabelIntegrationTests` passed (`8/8`),
	- label-service unit tests remained passing.
- Documentation synchronization: Stage 23.2 schema-impact outcome is reflected in planning/command/functionality trackers.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 0.2 (Execution Snapshot)

- Stage 0.2 completed as a role/institute access matrix baseline on top of existing schema.
- Schema touchpoints reviewed for scope enforcement evidence:
	- policy flags in `portal_settings`,
	- role/user/assignment relations,
	- department and course-offering ownership relationships used by report/analytics guards.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 0.3 (Execution Snapshot)

- Stage 0.3 completed as report-failure inventory and root-cause classification.
- Schema/dependency touchpoints reviewed for report behavior:
	- report definition and role assignment entities,
	- course/department/offering joins used by report repository queries,
	- student-profile dependent transcript/report paths.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 0.4 (Execution Snapshot)

- Stage 0.4 completed as Phase 0 baseline exit sign-off.
- Schema perspective confirmed:
	- no additional unresolved schema blockers identified in Phase 0 inventories,
	- pending schema-affecting parity work remains planned in subsequent phases.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 1.1 (Execution Snapshot)

- Stage 1.1 implemented institute model normalization for department-level canonical institute scope.
- Schema update applied through EF migration:
	- `20260513121000_Phase1Stage11DepartmentInstitutionType`
	- Adds non-null `InstitutionType` (`int`) to `departments` with default `0` (University).
	- Adds index `IX_departments_institution_type` for institute-scoped lookup/query performance.
- Schema impact: `Schema updated`.
- EF migration impact: applied in source and ready for deployment migration pipeline.

## 2026-05-13 Update - Institute Parity Stage 1.2 (Execution Snapshot)

- Stage 1.2 implemented referential-integrity support and institute/report query indexing hardening.
- Schema update applied through EF migration:
	- `20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes`
	- Replaces program unique index with department-scoped unique index:
		- drop `IX_academic_programs_code`
		- add `IX_academic_programs_code_dept` (`Code`, `DepartmentId`, unique)
	- Adds operational indexes:
		- `IX_academic_programs_dept_active`
		- `IX_courses_dept_active`
		- `IX_course_offerings_semester_open`
		- `IX_course_offerings_faculty_open`
		- `IX_student_profiles_dept_status`
		- `IX_student_profiles_program_status`
		- `IX_enrollments_offering_status`
		- `IX_enrollments_student_status`
		- `IX_faculty_dept_assignments_active_lookup`
		- `IX_admin_dept_assignments_active_lookup`
	- Alters `enrollments.Status` to `nvarchar(32)` to support indexed status filters.
- Schema impact: `Schema updated`.
- EF migration impact: applied in source and validated via integration-test migration path.

## 2026-05-13 Update - Institute Parity Stage 1.3 (Execution Snapshot)

- Stage 1.3 completed schema-script hardening for parity-safe deployment replay.
- Script updates applied:
	- `Scripts/01-Schema-Current.sql`
		- appended idempotent Stage 1.1 + Stage 1.2 migration-equivalent DDL sections,
		- added migration-history inserts for:
			- `20260513121000_Phase1Stage11DepartmentInstitutionType`
			- `20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes`.
	- `Scripts/04-Maintenance-Indexes-And-Views.sql`
		- added guarded parity index ensure/replacement logic.
	- `Scripts/05-PostDeployment-Checks.sql`
		- added explicit parity migration/column/index verification checks.
- Schema impact: `No additional model mutation beyond Stage 1.1/1.2`; this stage hardens script execution consistency.
- EF migration impact: none (script-hardening stage only).

## 2026-05-13 Update - Institute Parity Stage 1.4 (Execution Snapshot)

- Stage 1.4 completed Phase 1 exit-criteria verification enablement.
- Script updates applied:
	- `Scripts/05-PostDeployment-Checks.sql`
		- added department institute-type validity and coverage checks,
		- added orphan-count checks for institute-linked entities and assignment mappings.
- Schema impact: `No schema mutation`;
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.1 (Execution Snapshot)

- Stage 2.1 completed SuperAdmin global capability hardening for assignment management.
- API updates applied:
	- `DepartmentController`:
		- added faculty department assignment endpoints (assign/remove/list/list users),
		- added institution-type compatibility checks for admin/faculty assignment operations.
	- `AcademicDtos`:
		- added `RemoveFacultyFromDepartmentRequest` contract.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.2 (Execution Snapshot)

- Stage 2.2 completed role-scoped institute enforcement hardening for report handlers.
- API/security updates applied:
	- `TokenService` now emits `institutionType` claim for explicitly assigned users.
	- `ReportController` now enforces institution compatibility for Admin/Faculty report requests alongside existing role scope checks.
	- Integration harness updated to support institution claim validation scenarios.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.3 (Execution Snapshot)

- Stage 2.3 completed menu/action guard consistency hardening.
- API/web updates applied:
	- `PortalController` now enforces sidebar visibility guard checks on mapped portal actions.
	- Direct access to hidden menu sections now redirects to allowed portal surfaces for constrained roles.
	- Added integration verification for hidden-menu endpoint denial and SuperAdmin-visible endpoint access.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.4 (Execution Snapshot)

- Stage 2.4 completed Phase 2 exit-criteria validation for role + institute authorization matrix behavior.
- Validation coverage executed against existing Stage 2 code paths:
	- SuperAdmin/Admin assignment authorization,
	- report institute-scope enforcement,
	- sidebar/menu action guard consistency.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.1 (Execution Snapshot)

- Stage 3.1 completed first Phase 3 module CRUD/workflow parity implementation slice.
- API/web contract updates applied:
	- portal department create/edit now submits explicit institution type,
	- web API client no longer silently forces University mode for department writes,
	- integration coverage validates School/College/University department+course CRUD execution paths under enabled policy.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.2 (Execution Snapshot)

- Stage 3.2 completed lifecycle institute-parity enforcement for student lifecycle endpoints and portal lifecycle filters.
- API/web contract updates applied:
	- lifecycle endpoints now enforce admin assignment + institution scope compatibility,
	- web session identity now projects JWT `institutionType` for institute-aware lifecycle filtering,
	- lifecycle portal actions now preserve selected department/semester route state.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.3 (Execution Snapshot)

- Stage 3.3 completed student submenu parity hardening for student-list data scope and institute-neutral submenu terminology.
- API/web contract updates applied:
	- student-list endpoint now enforces admin assignment + institution-claim scope compatibility,
	- student/enrollment submenu list labels use institute-neutral `Level` wording.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.4 (Execution Snapshot)

- Stage 3.4 completed Phase 3 exit-criteria validation for module CRUD/workflow parity.
- API/web contract updates applied:
	- shared portal lookup contract now includes optional `InstitutionType` metadata for institute-aware lifecycle/student filtering compile consistency.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.1 (Execution Snapshot)

- Stage 4.1 completed analytics filter expansion for institute-aware analytics queries and dashboard filters.
- API/web contract updates applied:
	- analytics API and web client now accept optional `institutionType` filter for analytics reads/exports,
	- portal analytics now carries institute + department filter state with role-aware defaults.
- Query/runtime update applied:
	- analytics query layer now filters by department and institution-type scope with scope-aware cache keys.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.2 (Execution Snapshot)

- Stage 4.2 completed reports filter expansion for institute-aware report reads/exports and report-center filters.
- API/web contract updates applied:
	- report API and web client now accept optional `institutionType` filter on report generation/export paths,
	- report portal page models/views now carry selected institution filter state.
- Query/runtime update applied:
	- report repository queries now apply institution-type scope filtering across report datasets.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.3 (Execution Snapshot)

- Stage 4.3 completed broken report reliability fixes for faculty report scope behavior.
- API/web contract updates applied:
	- report controller faculty scope checks now enforce department assignment boundaries for repaired report endpoints,
	- no DTO or schema contract expansion required.
- Query/runtime update applied:
	- report endpoint runtime guardrails now require explicit faculty filters where needed and deny unassigned department queries.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.4 (Execution Snapshot)

- Stage 4.4 completed Phase 4 exit-criteria validation.
- API/web contract updates applied:
	- no schema-facing contract changes required.
- Query/runtime update applied:
	- full integration regression validation confirmed no database-shape changes were needed for report/analytics parity closure.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.1 (Execution Snapshot)

- Stage 5.1 completed core seed coverage for institute-aware foundational data.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- core seed script now initializes institution policy flags, institution-typed baseline departments, and current report key/role-access defaults.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.2 (Execution Snapshot)

- Stage 5.2 completed full dummy data coverage expansion for parity testing surfaces.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- `Scripts/03-FullDummyData.sql` now seeds buildings/rooms/timetables/payments/lifecycle/report artifacts and deterministic institute-type alignment for demo users/departments.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.3 (Execution Snapshot)

- Stage 5.3 completed data quality and replay safety hardening for script-first parity rollout.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- `Scripts/03-FullDummyData.sql` now aligns seeded user/department deterministic values on replay,
	- `Scripts/05-PostDeployment-Checks.sql` now emits institute-level parity counts, critical workflow entity counts, and duplicate-safety checks.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.4 (Execution Snapshot)

- Stage 5.4 completed Phase 5 exit criteria for one-run script-chain readiness.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- executed full script order (`01` -> `02` -> `03` -> `05`) successfully,
	- added superadmin identity reuse in full dummy script to avoid replay collisions with preexisting baseline accounts.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.1 (Execution Snapshot)

- Stage 6.1 completed automated institute parity test expansion.
- API/web contract updates applied:
	- no runtime API/Web contract changes (test-only stage).
- Query/runtime update applied:
	- expanded integration-test coverage for lifecycle, student submenu, and enrollment-summary report matched-institute success-path validations.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.2 (Execution Snapshot)

- Stage 6.2 completed cross-role UAT matrix automation for institute parity.
- API/web contract updates applied:
	- no runtime API/Web contract changes (test-only stage).
- Query/runtime update applied:
	- expanded integration-test coverage for cross-role authorization and report-visibility behavior across institution claims `0/1/2`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.3 (Execution Snapshot)

- Stage 6.3 completed performance and query validation for institute parity.
- API/web contract updates applied:
	- no runtime API/Web contract changes (test-only stage).
- Query/runtime update applied:
	- added integration-test validation for parity index read-usage on institute-filtered query paths,
	- added integration-test latency budget checks for common Admin dashboard/report paths.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.4 (Execution Snapshot)

- Stage 6.4 completed Phase 6 exit criteria validation for institute parity.
- API/web contract updates applied:
	- no runtime API/Web contract changes (validation-only stage).
- Query/runtime update applied:
	- executed consolidated parity phase-exit regression suite over Stage 6 test scopes.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.1 (Execution Snapshot)

- Stage 7.1 completed deployment runbook finalization for parity release readiness.
- Deployment/script updates applied:
	- updated `Scripts/README.md` with deterministic execution order (`01 -> 02 -> 03 -> 04 -> 05`),
	- added environment notes for DB create/context switching, permissions, and cleanup fallback,
	- added rollback/verification checklist for backup, failure handling, and evidence capture.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Phase 23 Stage 23.1 (Execution Snapshot)

- Stage 23.1 completed institution-type foundation confirmation from `Docs/Advance-Enhancements.md`.
- Validation highlights:
	- confirmed policy persistence keys for School/College/University mode toggles,
	- confirmed University-default compatibility path remains active.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.2 (Execution Snapshot)

- Stage 7.2 completed functional documentation update for institute parity behavior.
- Documentation updates applied:
	- added role/institute parity behavior guidance in `Docs/Functionality.md`,
	- added role/institute filter behavior guidance across user guides (`User Guide/README.md`, `Admin-Guide.md`, `Faculty-Guide.md`, `Student-Guide.md`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.3 (Execution Snapshot)

- Stage 7.3 completed monitoring and support handover documentation for parity release readiness.
- Documentation updates applied:
	- added institute parity monitoring guidance in `Docs/Functionality.md`,
	- added institute parity support handover checklist in `User Guide/SuperAdmin-Guide.md`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.4 (Execution Snapshot)

- Stage 7.4 completed Phase 7 release exit criteria for institute parity.
- Documentation updates applied:
	- finalized Phase 7 exit criteria evidence in the institute parity phase tracker,
	- synchronized the mandatory release-readiness trackers used for phase handoff,
	- advanced the command pointer to the next roadmap stage after release closeout.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-12 Update — Institution License Validation Phase 7 (Execution Snapshot)

- Completed SuperAdmin permission matrix against existing management and policy tables.
- Evidence set: `Artifacts/Phase7/SuperAdmin/20260512-151302/RunSummary.json`.
- Validated operations touch existing entities and mappings:
	- Department lifecycle management (`departments`).
	- Admin-user lifecycle management (`users`, `roles`).
	- Institution policy mode switching (`portal_settings` policy flags).
	- Privileged cross-mode reporting and dashboard visibility over existing report/config tables.
- No schema mutation and no EF migration required for Phase 7.

## 2026-05-12 Update — Institution License Validation Phase 6 (Execution Snapshot)

- Completed role-scoped access-boundary validation using existing assignment and report-scope tables.
- Evidence set: `Artifacts/Phase6/Access/20260512-150824/RunSummary.json`.
- Validated scope enforcement against current schema relationships:
	- Admin department assignments (`admin_department_assignments`) gate report exports.
	- Faculty offering assignments (`course_offerings.faculty_user_id`) gate report exports.
	- Student role remains restricted from operational report export endpoints.
- No schema mutation and no EF migration required for Phase 6.

## 2026-05-12 Update — Institution License Validation Phase 5 (Execution Snapshot)

- Implemented explicit per-user institution assignment persistence.
- Schema update applied through EF migration:
	- `20260512043929_AddUserInstitutionTypeAssignment`
	- Adds nullable `InstitutionType` (`int`) to `users` table.
- Application contract impact:
	- manual admin create/list endpoints now persist and return per-user institution assignment,
	- CSV import supports optional `InstitutionType` column with policy validation.
- Validation artifacts: `Artifacts/Phase5/Api/*_20260512-144212.json`.

## 2026-05-12 Update — Institution License Validation Plan

- Added execution reference: `Docs/Institution-License-Validation-Phases.md`.
- This validation plan confirms schema-backed behavior for:
	- license state and institution mode enforcement,
	- institution-aware user assignment and CSV import mapping,
	- institution-scoped access for Student/Faculty/Admin,
	- mixed-scope (School/College/University) runtime behavior.
- Expected schema touchpoints for validation evidence include: `license_state`, `users`, role mapping tables, institutional policy/configuration settings, and enrollment/academic progression tables.
- Current update is documentation-only.
- No database schema change and no EF migration were required for this update.

## 2026-05-12 Update — Institution License Validation Phase 4 (Execution Snapshot)

- Completed School/College/University x role validation sweep for dashboard/menu/report behavior.
- Confirmed policy and vocabulary switching by mode through runtime policy state:
	- School labels: Grade/Promotion/Percentage/Subject/Class
	- College labels: Year/Progression/Percentage/Subject/Year-Group
	- University labels: Semester/Progression/GPA/CGPA/Course/Batch
- Confirmed scoped report access behavior with existing assignment tables and report guards:
	- Admin scope through admin-department assignment
	- Faculty scope through assigned course offering
	- Student restricted from operational report data/export endpoints
- Validation artifacts stored at `Artifacts/Phase4/ModeRole/20260512-142021`.
- No database schema change and no EF migration required.

## 2026-05-12 Update — Institution License Validation Phase 2 (Execution Snapshot)

- Completed School/College/University lifecycle-mode validation with persisted policy checks.
- Confirmed `portal_settings` now stores and updates:
	- `institution_include_school`
	- `institution_include_college`
	- `institution_include_university`
- Confirmed policy keys change per mode after license activation and are reflected in runtime endpoints.
- Code fix impact: application-layer persistence only (`SaveChangesAsync` call in institution policy service).
- No schema mutation and no EF migration required.
- Validation-only environment workaround used during sequential mode switching:
	- cleared `consumed_verification_keys` between uploads due verification-key reuse from current generator output.

## 2026-05-12 Update — Institution License Validation Phase 3 (Execution Snapshot)

- Completed multi-mode validation for School+College, School+University, College+University, and School+College+University.
- Confirmed persisted union state in `portal_settings` keys:
	- `institution_include_school`
	- `institution_include_college`
	- `institution_include_university`
- Confirmed capability-matrix union row exposure aligns with persisted policy keys for each mixed-mode license.
- No schema mutation and no EF migration required.
- Validation-only environment workaround continued for sequential uploads:
	- cleared `consumed_verification_keys` because current generator output reuses verification key material.

## 2026-05-12 Update — Institution License Validation Phase 1 (Execution Snapshot)

- Executed baseline runtime checks for license/policy endpoints with SuperAdmin authentication.
- Observed current state:
	- `license_status`: `Invalid` (pre-upload)
	- `license_details`: `None` (pre-upload)
	- policy flags: `includeUniversity=true`, `includeSchool=false`, `includeCollege=false`
- Initial upload failure was traced to legacy `license_state` non-null columns (`InstitutionScope`, `ExpiryType`) lacking defaults in the active database schema.
- Applied SQL defaults for those legacy columns in the validation environment.
- Re-ran upload successfully; post-upload state is `license_status=Active`.
- Final matrix validation confirmed policy-driven restriction (`school=false`, `college=false`, `university=true`).
- No EF migration was introduced in this phase; remediation was environment-level compatibility for legacy schema constraints.

## 2026-05-10 Update — Phase 32 Stage 32.1

- Stage 32.1 delivered report-catalog and report-route regression guardrail integration tests only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 33 Stage 33.1

- Phase 33 was re-scoped to hosting configuration and security hardening.
- Stage 33.1 delivered startup/configuration foundation updates in API/Web/BackgroundJobs only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 33 Stage 33.2

- Stage 33.2 delivered runtime hosting hardening in API/Web startup and Web runtime URL handling.
- Changes were limited to configuration and startup/runtime guards.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 33 Stage 33.3

- Stage 33.3 delivered DTO-level security hardening and executable validation coverage.
- Changes were limited to model validation attributes and unit tests.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 3 Stage 3.3

- Stage 3.3 delivered transport optimization in API/Web startup only.
- Changes were limited to Kestrel transport configuration and compression settings.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 4 Complete

- Phase 4 delivered API cache policy, static/edge caching behavior, and cache scope controls.
- Changes were limited to runtime caching and host configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 5 Complete

- Phase 5 delivered k6 load-model upgrades, distributed-generator controls, and output-discipline runner updates.
- Changes were limited to load-testing scripts and execution wrappers.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 6 Complete

- Phase 6 delivered external dependency caching, integration circuit-breaker controls, and request-path blocking-risk cleanup.
- Changes were limited to application/integration runtime and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 7 Complete

- Phase 7 delivered expanded queue offloading and queue-platform integration support for background email work.
- Changes were limited to background worker/queue runtime and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 8 Complete

- Phase 8 delivered auto-scaling policy baselines, host-limit tuning, and network stack tuning across API/Web/BackgroundJobs.
- Changes were limited to startup/runtime configuration and host/network behavior controls.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 9 Complete

- Phase 9 delivered observability metrics, latency SLO snapshots, and continuous runtime health monitoring.
- Changes were limited to API startup/runtime telemetry, health checks, and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 10 Complete

- Phase 10 delivered progressive load gate orchestration, bottleneck classification, and fix-and-retest support.
- Changes were limited to load-testing scripts and execution orchestration only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 32 Stage 32.2

- Stage 32.2 delivered report export action/endpoint regression guardrail integration tests only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 32 Stage 32.3

- Stage 32.3 delivered sidebar-settings menu-assignability regression guardrail integration tests only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Stage 31.1

- Stage 31.1 delivered regression-matrix test coverage and matrix documentation only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Stage 31.2

- Stage 31.2 delivered authorization/data-exposure/audit hardening in API controllers and integration tests.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Stage 31.3

- Stage 31.3 delivered performance-band load test and recovery-smoke certification scripts plus runbook updates.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 32 Stage 32.4


## 2026-05-10 Update — Phase 32 Stage 32.5

- Stage 32.5 delivered credential and run-command verification guardrail integration tests.
- Changes were limited to integration test coverage and operational verification documentation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Complete

- Phase 31 completed as release hardening (regression matrix, security hardening, and performance/reliability certification).
- No database schema change and no EF migration were required across Stage 31.1, 31.2, and 31.3.

## 2026-05-10 Update — Phase 30 Stage 30.1

- Stage 30.1 delivered a centralized outbound integration gateway layer (retry/timeout/dead-letter handling) and integration diagnostics endpoints.
- Changes were limited to application/service/integration runtime and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 30 Stage 30.2

- Stage 30.2 delivered tenant onboarding templates, subscription plan controls, and tenant profile settings operations.
- Persistence uses the existing `portal_settings` key-value table with new operational keys.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 30 Stage 30.3

- Stage 30.3 delivered feature-flag rollout controls and emergency rollback operations.
- Persistence uses existing `portal_settings` key-value entries for feature-flag state and rollback metadata.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 30 Complete

- Phase 30 completed with integration gateway controls, tenant/subscription operations, and rollback-safe feature-flag controls.
- No schema changes were required across Stage 30.1, 30.2, and 30.3.

## 2026-05-09 Update — Phase 28 Stage 28.1

- Stage 28.1 delivered runtime and hosting changes only.
- No database schema change and no EF migration were required.

## 2026-05-09 Update — Phase 28 Stage 28.2 Foundation

- Stage 28.2 foundation delivered cache and background-worker changes only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.2 Completion

- Stage 28.2 completion delivered additional API queue-processing endpoints and workers for report generation and publish-all recalculation workloads.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 1

- Stage 28.3 slice 1 delivered a storage-provider abstraction for file/media persistence and migrated payment-proof uploads to provider-backed object-key references.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 2

- Stage 28.3 slice 2 migrated graduation certificate persistence to the shared storage-provider abstraction and added provider-backed read support.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 3

- Stage 28.3 slice 3 migrated license-upload temporary file handling to provider-backed storage operations and added bytes-based license activation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 4

- Stage 28.3 slice 4 introduced configuration-driven storage provider selection and a blob-style storage adapter implementation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 5

- Stage 28.3 slice 5 migrated portal logo uploads to provider-backed storage and added key-based logo streaming from the API.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 6

- Stage 28.3 slice 6 added temporary signed read URL support in the storage abstraction and provider adapters.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 7

- Stage 28.3 slice 7 enforced local signed URL validation (`exp`/`sig`) for portal logo reads with legacy unsigned-link signed redirects.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 8

- Stage 28.3 slice 8 added authenticated tokenized certificate streaming endpoint support and signed local certificate read validation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 9

- Stage 28.3 slice 9 added provider-backed storage metadata lookup support (content type and length) for media objects.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 10

- Stage 28.3 slice 10 added provider-persisted integrity/disposition metadata support (SHA-256 content hash and optional download filename) for media objects.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Complete

- Phase 28 completed with runtime, cache/worker, and media-storage architecture changes only.
- No database schema change and no EF migration were required for Phase 28 completion.

## 2026-05-10 Update — Phase 29 Stage 29.1

- Stage 29.1 added composite indexes for graduation applications, support tickets, notification recipients, payment receipts, quiz attempts, and user sessions.
- EF migration added: `20260509155457_20260510_Phase29_IndexBaseline`.
- Current model audit found no `InstitutionId`, `YearId`, or `GradeId` columns, so no indexes were added for those keys in this stage.

## 2026-05-10 Update — Phase 29 Stage 29.2 Slice 1

- Stage 29.2 slice 1 added application/query contract pagination for helpdesk ticket lists only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Stage 29.2 Slice 2

- Stage 29.2 slice 2 added application/query contract pagination for graduation application lists.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Stage 29.2 Slice 3

- Stage 29.2 slice 3 added application/query contract pagination for payment receipt lists.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Stage 29.3

- Stage 29.3 delivered operational maintenance scripts for retention cleanup, index maintenance, and capacity-growth dashboards.
- Added scripts: `Scripts/3-Phase29-ArchivePolicy.sql`, `Scripts/4-Phase29-IndexMaintenance.sql`, `Scripts/5-Phase29-CapacityGrowthDashboard.sql`.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Complete

- Phase 29 is complete (Stages 29.1, 29.2 slices 1-3, and 29.3).
- Schema-impacting work concluded in Stage 29.1; Stage 29.2 and Stage 29.3 were application/operations-only updates.

---

# PART 1: UNIVERSITY PORTAL APPLICATION DATABASE

---

## 1. Identity & Access Control

### users
Stores all system users (students, faculty, admins, super admins).

- id (UUID, PK)
- username (unique)
- email (unique, nullable)
- password_hash
- role_id (FK → roles.id)
- department_id (FK → departments.id, nullable)
- is_active
- created_at
- updated_at
- last_login_at

---

### roles
Predefined system roles.

- id (PK)
- name (Student, Faculty, Admin, SuperAdmin)
- description
- is_system_role

---

## 2. Department & Academic Structure

### departments
Core academic departments.

- id (PK)
- name
- code (unique)
- is_active
- created_at

---

### programs
Academic programs offered by a department.

- id (PK)
- department_id (FK)
- name
- code
- duration_years
- is_active

---

### courses
Courses offered under programs.

- id (PK)
- department_id (FK)
- program_id (FK)
- code
- title
- credit_hours
- is_active

---

### semesters
Academic semesters.

- id (PK)
- name (e.g., Fall 2025)
- start_date
- end_date
- is_active

---

## 3. Student Information System (Permanent Records)

### students
Student core identity.

- id (UUID, PK)
- user_id (FK → users.id)
- registration_number (unique)
- program_id (FK)
- current_semester_id (FK)
- admission_date
- status

---

### student_semester_records
Complete academic history (never deleted).

- id (PK)
- student_id (FK)
- semester_id (FK)
- gpa
- cgpa
- academic_status
- created_at

---

### student_course_enrollments
Tracks course enrollment per semester.

- id (PK)
- student_id (FK)
- course_id (FK)
- semester_id (FK)
- status (enrolled, dropped, completed)

---

## 4. Assignments & Submissions

### assignments
Teacher-created assignments.

- id (PK)
- course_id (FK)
- faculty_id (FK → users.id)
- semester_id (FK)
- title
- description
- due_date
- is_published
- created_at

---

### assignment_submissions
Student submissions.

- id (PK)
- assignment_id (FK)
- student_id (FK)
- file_path
- submitted_at
- grade
- feedback

---

## 5. Quizzes & Assessments

### quizzes
Quiz definitions.

- id (PK)
- course_id (FK)
- semester_id (FK)
- title
- start_time
- end_time
- max_attempts

---

### quiz_attempts
Student quiz attempts.

- id (PK)
- quiz_id (FK)
- student_id (FK)
- score
- attempted_at

---

## 6. Attendance Management

### attendance_records
Daily attendance tracking.

- id (PK)
- student_id (FK)
- course_id (FK)
- semester_id (FK)
- attendance_date
- status (present, absent)

---

## 7. Results & Grades

### grades
Final course results.

- id (PK)
- student_id (FK)
- course_id (FK)
- semester_id (FK)
- grade

---

## 8. Notifications System

### notifications
Central notification messages.

- id (PK)
- title
- message
- type (assignment, quiz, result, attendance, fyp)
- created_by (FK → users.id)
- created_at

---

### notification_recipients
Per-user delivery tracking.

- id (PK)
- notification_id (FK)
- user_id (FK)
- is_read
- read_at

---

## 9. Final Year Project (FYP)

### fyp_projects
Student project records.

- id (PK)
- student_id (FK)
- title
- semester_id (FK)

---

### fyp_meetings
Scheduled project meetings.

- id (PK)
- fyp_project_id (FK)
- meeting_datetime
- department_id (FK)
- room_number

---

### fyp_panel_members
Faculty panel participants.

- id (PK)
- fyp_meeting_id (FK)
- faculty_id (FK)

---

## 10. UI Themes & Personalization

### themes
Available UI themes.

- id (PK)
- name
- is_dark
- is_accessible
- is_active

---

### user_theme_preferences
User-selected themes.

- id (PK)
- user_id (FK)
- theme_id (FK)

---

## 11. Module Control (Feature Toggles)

### modules
All selectable modules.

- id (PK)
- key (assignment, quiz, ai_chat, fyp, etc.)
- name
- is_mandatory

---

### module_status
Super Admin-controlled activation.

- id (PK)
- module_id (FK)
- is_active
- activated_at

---

## 12. License State (Application Side Only)

### license_state
Stores validated license status (no license creation).

- id (PK)
- license_hash
- license_type (yearly, permanent)
- status (active, expired)
- activated_at
- expires_at (nullable)

---

# PART 2: LICENSE CREATION TOOL DATABASE

*(Separate system, separate database)*

---

## 13. License Storage

### licenses
Only hashed keys stored.

- id (PK)
- license_key_hash (unique)
- license_type (yearly, permanent)
- issued_at
- expires_at (nullable)
- status (active, revoked)

---

## 14. License Issuance Logs

### license_issuance_logs
Tracks who generated licenses.

- id (PK)
- license_id (FK)
- issued_by_user
- issued_at
- notes

---

## 15. License Revocation

### license_revocations
Revoked licenses history.

- id (PK)
- license_id (FK)
- revoked_at
- reason

---

## 16. License Tool Users

### license_tool_users
Admins of license tool.

- id (PK)
- username
- password_hash
- role (SuperAdmin)
- created_at

---

## 17. License Audit Logs

### license_audit_logs
Full traceability.

- id (PK)
- action (create, revoke, view)
- license_id (FK)
- performed_by
- performed_at
- ip_address

---

## 18. Academic Calendar (Phase 12)

### academic_deadlines
Named academic deadlines and key dates attached to a semester. Used by the Academic Calendar portal page and the `DeadlineReminderJob` background service.

- id (UUID, PK)
- semester_id (FK → semesters.id, cascade delete)
- title (nvarchar 200, required)
- description (nvarchar 1000, nullable)
- deadline_date (datetime2)
- reminder_days_before (int, default 3 — 0 means day-of reminder only)
- is_active (bool, default true)
- last_reminder_sent_at (datetime2, nullable — set by DeadlineReminderJob when notification is dispatched)
- is_deleted (bool — soft delete via global query filter)
- deleted_at (datetime2, nullable)
- created_at (datetime2)
- updated_at (datetime2)
- row_version (rowversion / timestamp — optimistic concurrency)

**Indexes:**
- `IX_academic_deadlines_semester` on `semester_id`
- `IX_academic_deadlines_date_active` on `(deadline_date, is_active)`

**EF Migration:** `20260507_Phase12AcademicCalendar`

---

## 19. Global Search (Phase 13)

Phase 13 introduces no new database tables. All search queries execute against existing tables using EF Core LINQ joins:

| Entity searched | Table(s) queried |
|---|---|
| Students | `student_profiles` JOIN `users` |
| Courses | `courses` |
| Course Offerings | `course_offerings` JOIN `courses` JOIN `semesters` |
| Faculty | `users` JOIN `roles` (where `roles.name = 'Faculty'`) |
| Departments | `departments` |
| Student-enrolled offerings | `enrollments` JOIN `course_offerings` JOIN `courses` |

All queries respect global soft-delete query filters (`is_deleted = 0`) automatically.

Role-scoped filtering applied at the application service layer:
- **SuperAdmin** — all entities across all departments
- **Admin** — entities within their assigned departments
- **Faculty** — entities within their own department + their own course offerings
- **Student** — only their enrolled course offerings

**EF Migration:** None required (no schema changes)

---

## 18. Design Guarantees

- License contains **no university identity**
- License file is cryptographically protected
- License keys cannot be altered
- Academic data is never deleted
- Module-based feature control supported
- Fully offline-capable license validation

---

## 19. Implementation Conventions (ASP.NET + EF Core)

- Use GUID PKs for all user-facing and distributed entities (users, students, assignments, licenses)
- Use bigint PKs for high-volume append-only logs where sequential inserts are beneficial
- Add created_at, updated_at, and row_version (concurrency token) to mutable aggregates
- Use soft-delete columns (is_deleted, deleted_at) for operational entities; never soft-delete academic history tables
- Store all timestamps in UTC

---

## 20. Additional Core Tables Required for Build Readiness

### user_sessions
Tracks web/API sessions and refresh-token family state.

- id (UUID, PK)
- user_id (FK -> users.id)
- refresh_token_hash
- device_info
- ip_address
- expires_at
- revoked_at (nullable)

### faculty_department_assignments
Supports faculty mapped to one or more departments.

- id (PK)
- faculty_id (FK -> users.id)
- department_id (FK -> departments.id)
- is_primary

### course_offerings
Represents a course running in a specific semester and department context.

- id (PK)
- course_id (FK)
- semester_id (FK)
- department_id (FK)
- faculty_id (FK -> users.id)
- section
- capacity
- is_active

### registration_whitelist
Pre-approved registration numbers for controlled student signup.

- id (PK)
- registration_number (unique)
- program_id (FK)
- semester_id (FK)
- is_claimed
- claimed_by_student_id (FK, nullable)

### quiz_questions
Question bank entries tied to quizzes.

- id (PK)
- quiz_id (FK)
- question_text
- question_type (mcq, short, true_false)
- marks
- display_order

### quiz_question_options
Options for objective quiz questions.

- id (PK)
- quiz_question_id (FK)
- option_text
- is_correct

### quiz_attempt_answers
Submitted answers per attempt.

- id (PK)
- quiz_attempt_id (FK)
- quiz_question_id (FK)
- selected_option_id (FK, nullable)
- answer_text (nullable)
- awarded_marks

### transcript_exports
Tracks transcript generation history for compliance and auditability.

- id (PK)
- student_id (FK)
- exported_by (FK -> users.id)
- exported_at
- format (pdf, excel)

### audit_logs
Operational audit logs for privileged activities.

- id (bigint, PK)
- actor_user_id (FK -> users.id, nullable)
- action
- entity_name
- entity_id
- old_values_json
- new_values_json
- occurred_at
- ip_address

---

## 21. Constraint and Index Strategy

- users: unique indexes on username and email (filtered where email is not null)
- students: unique index on registration_number and unique index on user_id
- student_course_enrollments: unique composite index on (student_id, course_id, semester_id)
- attendance_records: unique composite index on (student_id, course_id, semester_id, attendance_date)
- assignment_submissions: unique composite index on (assignment_id, student_id)
- module_status: unique index on module_id to enforce single active status row
- notifications_recipients: index on (user_id, is_read)
- audit_logs: clustered index by occurred_at for time-range queries

---

## 22. Data Retention and Archival Rules

- Academic records: never deleted; archive to cold storage after policy threshold
- Audit logs: retain online for 24 months, archive for 7 years
- Notification delivery logs: retain online for 12 months, then archive
- Session and token records: purge expired and revoked entries after 90 days

---

## 23. Migration and Seeding Plan

- Baseline migration: identity, departments, SIS core, license_state
- Seed mandatory roles, modules, and default themes
- Seed Super Admin bootstrap user through secure deployment script
- Apply feature migrations per release train (v1, v1.1, v1.2)

---

## 24. Phase 20 LMS Tables (Migration `Phase20_LMS` — 2026-05-08)

### course_content_modules
Structured weekly learning modules created by faculty per `CourseOffering`.

- id (UUID, PK)
- offering_id (FK → course_offerings.id, ON DELETE CASCADE)
- title (varchar 300)
- week_number (int)
- body (nvarchar 50000)
- is_published (bool, default false)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### content_videos
Video attachments linked to a `CourseContentModule`.

- id (UUID, PK)
- module_id (FK → course_content_modules.id, ON DELETE CASCADE)
- title (varchar 300)
- storage_url (varchar 1000, nullable)
- embed_url (varchar 1000, nullable)
- duration_seconds (int, default 0)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### discussion_threads
Discussion threads opened within a `CourseOffering`.

- id (UUID, PK)
- offering_id (FK → course_offerings.id, ON DELETE CASCADE)
- title (varchar 500)
- author_id (FK → users.id, ON DELETE NO ACTION)
- is_pinned (bool, default false)
- is_closed (bool, default false)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### discussion_replies
Replies within a `DiscussionThread`.

- id (UUID, PK)
- thread_id (FK → discussion_threads.id, ON DELETE CASCADE)
- author_id (FK → users.id, ON DELETE NO ACTION)
- body (nvarchar 10000)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### course_announcements
Course-level announcements posted by faculty; triggers fan-out notification to enrolled students.

- id (UUID, PK)
- offering_id (FK → course_offerings.id, ON DELETE SET NULL, nullable)
- author_id (FK → users.id, ON DELETE NO ACTION)
- title (varchar 300)
- body (nvarchar 10000)
- posted_at (datetime)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

---

### study_plans
Student semester study plans. Phase 21 — Study Planner.

- id (UUID, PK)
- student_profile_id (FK → student_profiles.id, ON DELETE CASCADE)
- planned_semester_name (varchar 100)
- notes (nvarchar 2000, nullable)
- advisor_status (int, default 0 — 0=Pending, 1=Endorsed, 2=Rejected)
- advisor_notes (nvarchar 2000, nullable)
- reviewed_by_user_id (UUID, nullable)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### study_plan_courses
Course line items within a study plan.

- id (UUID, PK)
- study_plan_id (FK → study_plans.id, ON DELETE CASCADE)
- course_id (FK → courses.id, ON DELETE RESTRICT)
- created_at / updated_at / row_version
- UQ_study_plan_courses_plan_course (study_plan_id, course_id)

---

### accreditation_templates
Accreditation / government report templates. Phase 22 — External Integrations.
EF migration: `Phase22_ExternalIntegrations`.

- id (UUID, PK)
- name (nvarchar 200)
- description (nvarchar 1000, nullable)
- field_mappings_json (nvarchar max) — JSON array of field mapping objects
- format (nvarchar 20) — "CSV" or "PDF"
- is_active (bool, default true)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

---

### Phase 23 — Core Policy Foundation (no new tables)
Institution type flags stored as rows in `portal_settings` under the keys:
- `institution_policy_school` (value: "true"/"false")
- `institution_policy_college` (value: "true"/"false")
- `institution_policy_university` (value: "true"/"false")

No EF migration required — reuses the existing `portal_settings` key-value store.

---

### Phase 24 — Dynamic Module and UI Composition (no new tables)
All Phase 24 services (`ModuleRegistryService`, `LabelService`, `DashboardCompositionService`) are stateless / in-process. No new tables were added.

---

### institution_grading_profiles
Per-institution-type grading configuration (pass threshold and grade bands). Phase 25 — Academic Engine Unification.
EF migration: `20260508152906_Phase25_AcademicEngineUnification`.

- id (UUID, PK)
- institution_type (int) — `InstitutionType` enum: 0=University, 1=School, 2=College
- pass_threshold (decimal(5,2)) — 0–4.0 for University; 0–100 for School/College
- grade_ranges_json (nvarchar max, nullable) — JSON grade band array, e.g. `[{"From":90,"To":100,"Label":"A+"},...]`; null uses built-in defaults
- is_active (bool, default true)
- created_at / updated_at

**Indexes:**
- `IX_institution_grading_profiles_type` — unique on `institution_type` (one profile per type)

---

### school_streams
School stream master records (Science/Commerce/Arts etc.). Phase 26 — School and College Functional Expansion.
EF migration: `20260509044437_Phase26_SchoolCollegeExpansion`.

- id (UUID, PK)
- name (nvarchar 120, required, unique)
- description (nvarchar 500, nullable)
- is_active (bool, default true)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

**Indexes:**
- `IX_school_streams_name` — unique on `name`

---

### student_stream_assignments
One-to-one stream assignment per student profile.

- id (UUID, PK)
- student_profile_id (UUID, FK -> student_profiles.id)
- school_stream_id (UUID, FK -> school_streams.id)
- assigned_by_user_id (UUID)
- assigned_at (datetime)
- created_at / updated_at

**Indexes:**
- `IX_student_stream_assignments_student` — unique on `student_profile_id`

---

### student_report_cards
Report-card snapshot store for school/college/university period exports.

- id (UUID, PK)
- student_profile_id (UUID, FK -> student_profiles.id)
- institution_type (int)
- period_label (nvarchar 80)
- payload_json (nvarchar max)
- generated_by_user_id (UUID)
- generated_at (datetime)
- created_at / updated_at

**Indexes:**
- `IX_student_report_cards_student_generated` — (`student_profile_id`, `generated_at`)

---

### bulk_promotion_batches
Header table for approval-based bulk promotion operations.

- id (UUID, PK)
- title (nvarchar 180)
- status (int) — `BulkPromotionStatus` enum
- created_by_user_id (UUID)
- approved_by_user_id (UUID, nullable)
- reviewed_at (datetime, nullable)
- applied_at (datetime, nullable)
- review_note (nvarchar 1000, nullable)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

**Indexes:**
- `IX_bulk_promotion_batches_status_created` — (`status`, `created_at`)

---

### bulk_promotion_entries
Per-student promote/hold decisions attached to a bulk promotion batch.

- id (UUID, PK)
- batch_id (UUID, FK -> bulk_promotion_batches.id)
- student_profile_id (UUID, FK -> student_profiles.id)
- decision (int) — `EntryDecision` enum
- reason (nvarchar 500, nullable)
- is_applied (bool)
- applied_at (datetime, nullable)
- created_at / updated_at

**Indexes:**
- `IX_bulk_promotion_entries_batch` — (`batch_id`)
- `IX_bulk_promotion_entries_batch_student` — unique (`batch_id`, `student_profile_id`)

---

### parent_student_links
Parent/guardian to student mapping used for parent-read portal access.

- id (UUID, PK)
- parent_user_id (UUID, FK -> users.id)
- student_profile_id (UUID, FK -> student_profiles.id)
- relationship (nvarchar 60, nullable)
- is_active (bool, default true)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

**Indexes:**
- `IX_parent_student_links_parent_student` — unique (`parent_user_id`, `student_profile_id`)

---

### Phase 27 — University Portal Parity and Student Experience
Phase 27 (Stages 27.1, 27.2, 27.3) introduced service/API/web integration and security/abstraction contracts only.

- No new database tables.
- No altered columns.
- No new indexes.
- No EF migration required.
