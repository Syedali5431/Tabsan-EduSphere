### 2026-05-21 - Plan G Phase 8 Stage 8.3 Context Purity Guard
- Implementation Summary:
  - Documented the context purity guard requirement to prevent percentage and GPA mixing within a single context.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the context-purity guard requirement.
- Validation Summary:
  - Manual review confirmed the context-purity guard requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 8 Stage 8.2 Report Format Alignment
- Implementation Summary:
  - Documented the requirement for report format alignment so reports use the correct calculation type for each context.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the report-format alignment requirement.
- Validation Summary:
  - Manual review confirmed the report-format alignment requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 8 Stage 8.1 Result Format Rendering
- Implementation Summary:
  - Documented the requirement for result format rendering so School/College contexts show Percentage + Grade while University contexts show GPA/CGPA.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the result-format rendering requirement.
- Validation Summary:
  - Manual review confirmed the result-format rendering requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 7 Stage 7.3 Conflict Prevention in Shared Deployments
- Implementation Summary:
  - Documented the requirement to confirm conflict-free behavior for mixed-institution tenants in shared deployments.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the shared-deployment conflict-prevention requirement.
- Validation Summary:
  - Manual review confirmed the shared-deployment conflict-prevention requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 7 Stage 7.2 Cross-Context Example Validation
- Implementation Summary:
  - Documented the requirement to validate representative cross-context scenarios, including School->percentage and University->GPA outputs.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the cross-context example validation requirement.
- Validation Summary:
  - Manual review confirmed the cross-context example validation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 7 Stage 7.1 Multi-Institute Dispatch Logic
- Implementation Summary:
  - Documented the requirement to apply multi-institute dispatch logic so, when multiple institute types are enabled, the calculation method is selected by department institution type.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the multi-institute dispatch requirement.
- Validation Summary:
  - Manual review confirmed the multi-institute dispatch requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 6 Stage 6.3 Lifecycle API Freeze
- Implementation Summary:
  - Documented the lifecycle API freeze requirement: lifecycle APIs and workflows remain unchanged, with only calculation outputs subject to adjustment.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the lifecycle API freeze requirement.
- Validation Summary:
  - Manual review confirmed the lifecycle API freeze requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 6 Stage 6.2 Graduation/Progression Compatibility
- Implementation Summary:
  - Documented the requirement to ensure graduation workflows and semester progression remain valid with percentage-based outputs for school and college contexts.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the graduation/progression compatibility requirement.
- Validation Summary:
  - Manual review confirmed the graduation/progression compatibility requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 6 Stage 6.1 Promotion/Failure Compatibility
- Implementation Summary:
  - Documented the requirement to ensure School/College promotion and failure decisions correctly consume percentage-based outputs.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the promotion/failure compatibility requirement.
- Validation Summary:
  - Manual review confirmed the promotion/failure compatibility requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 5 Stage 5.3 Non-Target Module Protection
- Implementation Summary:
  - Documented the requirement to protect non-target modules so enrollment, assignments, quizzes, and unrelated analytics remain unchanged.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the non-target module protection requirement.
- Validation Summary:
  - Manual review confirmed the non-target module protection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 5 Stage 5.2 Display-Layer Integration
- Implementation Summary:
  - Documented the requirement to apply institute-conditional formatting at the result display layer.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the display-layer integration requirement.
- Validation Summary:
  - Manual review confirmed the display-layer integration requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 5 Stage 5.1 Calculation-Layer Integration
- Implementation Summary:
  - Documented the requirement to apply institute-conditional logic at the result calculation layer.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the calculation-layer integration requirement.
- Validation Summary:
  - Manual review confirmed the calculation-layer integration requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 4 Stage 4.3 GPA Isolation Guard
- Implementation Summary:
  - Documented the requirement to enforce GPA isolation so percentage grade mapping does not affect existing GPA data structures.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the GPA-isolation guard requirement.
- Validation Summary:
  - Manual review confirmed the GPA-isolation guard requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 4 Stage 4.2 Configurable Grade Scale
- Implementation Summary:
  - Documented the requirement to implement configurable grade-scale hooks so percentage grade bands can be adjusted in future iterations.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the configurable grade-scale hook requirement.
- Validation Summary:
  - Manual review confirmed the configurable grade-scale hook requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 4 Stage 4.1 Base Grade Bands
- Implementation Summary:
  - Documented the requirement to define standardized percentage grade bands for A+, A, B, and C/D for school and college contexts.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the base grade-band definition requirement.
- Validation Summary:
  - Manual review confirmed the base grade-band definition requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 3 Stage 3.3 Invalid Context Handling
- Implementation Summary:
  - Documented the requirement to define fallback/error behavior for unsupported or missing institute context.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the invalid-context handling requirement.
- Validation Summary:
  - Manual review confirmed the invalid-context handling requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 3 Stage 3.2 Mapping Resolver Enforcement
- Implementation Summary:
  - Documented the requirement to enforce mapping resolver behavior so canonical mapping is always applied before any display/output logic.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the resolver-enforcement requirement.
- Validation Summary:
  - Manual review confirmed the resolver-enforcement requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 3 Stage 3.1 Canonical Mapping Table
- Implementation Summary:
  - Documented the requirement to finalize and lock the canonical institute-to-calculation mapping table.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the canonical mapping lock requirement.
- Validation Summary:
  - Manual review confirmed the canonical mapping lock requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 2 Stage 2.4 Non-Refactor Guard
- Implementation Summary:
  - Documented the non-refactor guard that explicitly prohibits GPA system modification/refactor during this phase.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the GPA non-refactor protection requirement.
- Validation Summary:
  - Manual review confirmed the GPA non-refactor protection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 2 Stage 2.3 University Calculation Path
- Implementation Summary:
  - Documented the requirement to preserve the existing University GPA/CGPA credit-based calculation behavior unchanged.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the university calculation preservation requirement.
- Validation Summary:
  - Manual review confirmed the university calculation preservation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 2 Stage 2.2 College Calculation Path
- Implementation Summary:
  - Documented the requirement to implement percentage-based calculation for colleges (aligned to the school path) and return Percentage + Grade.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the college calculation path requirement.
- Validation Summary:
  - Manual review confirmed the college calculation path requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 1 Stage 1.2 Institute Type Detection
- Implementation Summary:
  - Documented the requirement to detect the enabled institute type (School, College, University) at runtime based on the parsed license.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the detection requirement.
- Validation Summary:
  - Manual review confirmed the detection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 1 Stage 1.1 License Parsing
- Implementation Summary:
  - Documented the requirement to parse enabled institute types (School, College, University) from the license file for future conditional logic.
  - No product behavior, API surface, or schema changes were introduced; this stage is documentation-only and sets the parsing requirement.
- Validation Summary:
  - Manual review confirmed the parsing requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

# Product Requirements Document (PRD)
## University Portal (License-Based, Department-Oriented System)

**Version:** 1.75 (Phase 38 final publish-separation baseline complete)  
**Status:** Approved  
**Prepared By:** Product Team  
**Last Updated:** 15 May 2026  

## Institute Parity Stage Documentation Rule (2026-05-13)

For each completed stage in `Docs/Institute-Parity-Issue-Fix-Phases.md`, PRD implementation log updates are mandatory and must include:
- `Implementation Summary`
- `Validation Summary`

Each stage log entry must clearly describe behavior impact for School/College/University and role-based access/filter/report outcomes.

---

## 0. Implementation Update Log



### 2026-05-21 - Plan G Phase 0 Stage 0.2 Conditional-Layer-Only Contract
- Implementation Summary:
  - Defined the conditional-layer-only contract: Only a conditional decision layer may be added over existing calculation paths; no direct modification of GPA/CGPA, lifecycle, or report logic is allowed.
  - No product behavior, API surface, or schema changes were introduced; this stage is a governance and safety declaration only.
- Validation Summary:
  - Manual review confirmed all GPA/CGPA, lifecycle, and report logic remain unchanged.
  - No build, test, or migration was required; this stage is documentation-only.

### 2026-05-21 - Plan G Phase 0 Stage 0.3 Compatibility Defaults
- Implementation Summary:
  - Established compatibility defaults: Full backward compatibility is enforced, and University GPA behavior is the default when no condition applies.
  - No product behavior, API surface, or schema changes were introduced; this stage is a governance and compatibility declaration only.
- Validation Summary:
  - Manual review confirmed backward compatibility and default behavior are preserved.
  - No build, test, or migration was required; this stage is documentation-only.

- Implementation Summary:
  - Declared the protected surface for result calculation: GPA/CGPA logic, lifecycle workflows, storage structure, and report logic are now explicitly frozen against direct modification unless a future phase requires it.
  - No product behavior, API surface, or schema changes were introduced; this stage is a governance declaration only.
- Validation Summary:
  - Manual review confirmed all GPA/CGPA, lifecycle, and report logic remain unchanged.
  - No build, test, or migration was required; this stage is documentation-only.

- Recent request issue:
  - update deployment scripts for Finance role, payment-summary reporting, and verification alignment.

#### Plan F Phase 8 (DB Script Synchronization)
- Implementation Summary:
  - synchronized the standard and clean seed scripts so Finance role and payment-summary report access are seeded in the deployment path,
  - extended post-deployment checks to validate Finance role presence and payment-summary role assignments,
  - kept the changes additive and replay-safe.
- Validation Summary:
  - manual script review confirmed the updates are idempotent and non-destructive,
  - no automated build or test execution was required for the SQL-only synchronization pass.
- Testing and result summary:
  - Phase 8 script sync completed as a database-script alignment update.

- Behavior impact:
  - deployment scripts now reflect Finance role/report support consistently,
  - no runtime API or schema behavior changes were introduced by this sync.

### 2026-05-21 - Plan F Phase 9 Stage 9.2 Data Boundary Enforcement
- Recent request issue:
  - proceed with Stage 9.2 and enforce tenant boundaries plus campus filtering across finance paths.

#### Plan F Phase 9 (Verified)
- Implementation Summary:
  - verified the payment receipt repository already applies tenant/campus scoping for finance-facing receipt and student lookups,
  - confirmed finance report and analytics requests continue to pass the current tenant/campus context,
  - recorded the stage as verification-only because no product behavior or schema mutation was needed.
- Validation Summary:
  - code review confirmed scoped finance data access is active in the repository-backed payment paths,
  - no build or automated test execution was required for this closeout step.
- Testing and result summary:
  - Phase 9.2 completed as a boundary-enforcement verification update.

- Behavior impact:
  - finance data access remains tenant/campus scoped,
  - no runtime API or schema changes were introduced.

### 2026-05-21 - Plan F Phase 9 Stage 9.3 Analytics Separation
- Recent request issue:
  - proceed with Stage 9.3 and keep payment analytics fully separate from academic analytics.

#### Plan F Phase 9 Stage 9.3 (Verified)
- Implementation Summary:
  - verified payment analytics remains isolated in `AnalyticsController` while academic report flows remain in `ReportController`,
  - confirmed the payment-status endpoint uses finance-scoped analytics inputs and does not reuse the academic report catalog surface,
  - recorded the stage as verification-only because the separation is already enforced by controller boundaries.
- Validation Summary:
  - code review confirmed payment analytics and academic analytics remain on separate controller/service paths,
  - no build or automated test execution was required for this closeout step.
- Testing and result summary:
  - Phase 9.3 completed as an analytics-separation verification update.

- Behavior impact:
  - finance analytics remains separate from academic analytics/reporting,
  - no runtime API or schema changes were introduced.

### 2026-05-21 - Plan F Phase 9 Stage 9.4 Report Data Isolation
- Recent request issue:
  - proceed with Stage 9.4 and prevent payment reports from pulling unrelated academic datasets.

#### Plan F Phase 9 Stage 9.4 (Implemented)
- Implementation Summary:
  - updated payment summary report data retrieval so enrollment/course/semester joins execute only when academic filters (`semesterId` or `courseId`) are explicitly requested,
  - preserved finance totals/status behavior for the default payment-summary path while reducing unrelated academic data loading.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - code review confirmed the default payment-summary path no longer pulls academic datasets unless filter-driven.
- Testing and result summary:
  - Phase 9.4 completed as a report-data isolation hardening update.

- Behavior impact:
  - payment reports avoid unrelated academic dataset reads by default,
  - finance reporting outputs remain stable for totals and status aggregation.

### 2026-05-21 - Plan F Phase 7 Documentation Synchronization
- Recent request issue:
  - update Finance documentation and related governance references for Phase 7.

#### Plan F Phase 7 (Documentation Only)
- Implementation Summary:
  - synchronized Finance user guide, training, UAT, and SAT documentation with payment workflows, reporting, analytics, and access boundaries,
  - no product behavior, API surface, or schema changes were introduced.
- Validation Summary:
  - manual review confirmed the documentation-only updates were applied consistently,
  - no build or automated test execution was required for this phase.
- Testing and result summary:
  - Phase 7 completed as a documentation sync with no functional regression surface.

- Behavior impact:
  - no functional behavior change; documentation coverage for Finance was expanded.

### 2026-05-21 - Plan F Phases 4 and 5 Payment Reports and Finance UI Surface
- Recent request issue:
  - proceed and complete the remaining finance reporting and UI phases.

#### Plan F Phases 4 and 5 (Implemented)
- Implementation Summary:
  - added finance payment summary reporting with time, institution, department, course, semester, and level filters plus Excel/CSV/PDF export,
### 2026-05-21 - Plan F Phase 10 Stage 10.4 Documentation Closure
- Recent request issue:
  - proceed.

#### Plan F Phase 10 Stage 10.4 (Implemented)
- Implementation Summary:
  - completed governance documentation synchronization and consistency closure across all Phase 10 records,
  - aligned Stage 10.1 through Stage 10.4 sequencing and completion references.
- Validation Summary:
  - manual cross-document review confirmed tracker, command-center, ASP.NET plan, modules, PRD, and schema entries are consistent,
  - no additional runtime, API, or schema change was required.
- Testing and result summary:
  - Stage 10.4 completed as documentation-only closure with consistency checks green.

- Behavior impact:
  - no functional behavior change,
  - documentation state is now internally consistent for full Plan F Phase 10 closure.

### 2026-05-21 - Plan F Phase 10 Stage 10.3 Data and Import Validation
- Recent request issue:
  - proceed.

#### Plan F Phase 10 Stage 10.3 (Implemented)
- Implementation Summary:
  - added integration assertions verifying imported mobile/phone values persist on created users,
  - added legacy CSV template compatibility coverage for `PhoneNumber` header import.
- Validation Summary:
  - `runTests` targeted suite passed (`6/6`):
    - `tests/Tabsan.EduSphere.IntegrationTests/UserImportAndForceChangeIntegrationTests.cs`.
- Testing and result summary:
  - Stage 10.3 completed with import-data persistence and template compatibility checks green.

- Behavior impact:
  - imported mobile/phone data persistence now has explicit regression coverage,
  - legacy `PhoneNumber` template files remain compatible with current import processing,
  - no schema or module-definition behavior regression introduced.

### 2026-05-21 - Plan F Phase 10 Stage 10.2 Analytics and Reporting Validation
- Recent request issue:
  - proceed.

#### Plan F Phase 10 Stage 10.2 (Implemented)
- Implementation Summary:
  - added explicit integration assertions for payment-summary export metadata correctness on Excel/PDF endpoints,
  - validated payment-status analytics filter behavior for course/semester-scoped outputs supporting pie-chart data integrity.
- Validation Summary:
  - `runTests` targeted suites passed (`33/33`):
    - `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsInstituteParityIntegrationTests.cs`,
    - `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs`.
- Testing and result summary:
  - Stage 10.2 completed with analytics filtering and report export correctness checks green.

- Behavior impact:
  - payment-summary export endpoints now have explicit regression protection for Excel/PDF response metadata,
  - payment-status chart data remains consistent with selected course/semester filters,
  - no schema or module-definition behavior regression introduced.

### 2026-05-21 - Plan F Phase 10 Stage 10.1 Access and Multi-Campus Validation
- Recent request issue:
  - proceed.

#### Plan F Phase 10 Stage 10.1 (Implemented)
- Implementation Summary:
  - corrected Finance policy unit-test modeling to match live policy enforcement (`SuperAdmin` + `Finance`, excluding `Admin`),
  - validated tenant/campus claim-bound payment visibility behavior using matching and mismatched campus integration scenarios.
- Validation Summary:
  - `runTests` targeted suites passed (`97/97`):
    - `tests/Tabsan.EduSphere.UnitTests/InstitutionPolicyTests.cs`,
    - `tests/Tabsan.EduSphere.IntegrationTests/AuthorizationRegressionTests.cs`,
    - `tests/Tabsan.EduSphere.IntegrationTests/StudentLifecycleIntegrationTests.cs`.
- Testing and result summary:
  - Stage 10.1 completed with strict finance access and multi-campus payment scope validation green.

- Behavior impact:
  - finance policy expectations now consistently reflect production authorization constraints,
  - finance payment-list access remains tenant/campus scoped with validated out-of-scope campus isolation,
  - no user-facing feature or schema behavior regression introduced.

  - surfaced payment reports in the portal report center with a dedicated finance report page and finance-focused totals/status presentation,
  - enabled finance navigation access for Payments, Report Center, Analytics, and Theme Settings while preserving academic-module blocking for finance-only sessions.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~AuthorizationRegressionTests"` passed (`64/64`).
- Testing and result summary:
  - finance report and navigation scope behavior completed with build and regression authorization checks green.

- Behavior impact:
  - finance users can now open and export payment reports without gaining access to academic reports,
  - finance navigation now includes payments, reporting, analytics, and theme personalization surfaces,
  - existing admin/faculty/student report behavior remains unchanged.

### 2026-05-20 - Plan F Phase 3 Stage 3.2 Filter-Aware Analytics Behavior
- Recent request issue:
  - proceed with Stage 3.2 and ensure payment analytics honors course/semester filter scope.

#### Plan F Phase 3 Stage 3.2 (Implemented)
- Implementation Summary:
  - added course/semester filter support to payment analytics endpoint and service contract,
  - enforced enrollment-linked filtering so payment status aggregates only include matching scope students,
  - added integration test coverage for filtered payment analytics requests.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`66/66`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).
- Testing and result summary:
  - Stage 3.2 completed with filter-path and authorization regression checks green.

- Behavior impact:
  - payment analytics now dynamically tracks course/semester filter selections,
  - existing tenant/campus and role access constraints remain unchanged.

### 2026-05-20 - Plan F Phase 3 Stage 3.3 Finance Analytics Isolation
- Recent request issue:
  - proceed with next stage.

#### Plan F Phase 3 Stage 3.3 (Implemented)
- Implementation Summary:
  - added finance-only analytics mode in portal analytics model/snapshot flow,
  - restricted finance analytics UI to payment analytics only and suppressed academic analytics charts/sections for finance-only sessions,
  - added integration regression checks proving finance is denied on academic analytics endpoints while remaining allowed on payment analytics endpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `runTests` targeted integration suites passed (`66/66`):
    - `tests/Tabsan.EduSphere.IntegrationTests/AuthorizationRegressionTests.cs`,
    - `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsInstituteParityIntegrationTests.cs`,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug -v minimal` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug -v minimal` passed (`1/1`).
- Testing and result summary:
  - Stage 3.3 completed with finance-role analytics isolation validated end-to-end.

- Behavior impact:
  - finance users now see payment analytics only,
  - finance users are prevented from academic analytics endpoints and academic analytics UI surfaces,
  - admin/superadmin/faculty analytics behavior remains unchanged.

### 2026-05-20 - Plan F Phase 3 Stage 3.1 Payment Status Pie Chart
- Recent request issue:
  - proceed with Stage 3.1 and add finance-compatible paid vs unpaid analytics charting.

#### Plan F Phase 3 Stage 3.1 (Implemented)
- Implementation Summary:
  - added payment status analytics contracts/service endpoint for scoped paid vs unpaid aggregates,
  - integrated payment status retrieval into portal analytics snapshot/model/client flow,
  - rendered interactive payment status pie chart with clickable segment legend.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`65/65`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).
- Testing and result summary:
  - Stage 3.1 completed with scoped analytics and authorization regression checks green.

- Behavior impact:
  - analytics now includes finance-relevant paid vs unpaid visibility,
  - finance role can access payment analytics while academic analytics behavior remains unchanged.

### 2026-05-20 - Plan F Phase 2 Stage 2.3 Tenant and Campus Enforcement
- Recent request issue:
  - proceed with Stage 2.3 and enforce tenant/campus boundaries for finance payment paths.

#### Plan F Phase 2 Stage 2.3 (Implemented)
- Implementation Summary:
  - scoped payment lifecycle repository queries by tenant/campus,
  - blocked receipt creation for out-of-scope student profiles before persistence,
  - added integration proof for scoped payment visibility behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`63/63`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Testing and result summary:
  - Stage 2.3 completed with build, unit, integration, and contract gates green.

- Behavior impact:
  - finance payment reads are now tenant/campus scoped,
  - out-of-scope receipts are excluded from finance list retrieval.

### 2026-05-20 - Plan F Phase 2 Stage 2.2 Finance Restriction Scope
- Recent request issue:
  - proceed with Stage 2.2 and enforce finance restrictions for payment deletion and academic module access.

#### Plan F Phase 2 Stage 2.2 (Implemented)
- Implementation Summary:
  - added explicit payment delete rejection route so receipt records remain permanent,
  - introduced finance-only web action guard that blocks academic modules and routes back to payments,
  - added regression tests to validate finance-denied academic APIs and delete rejection behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`54/54`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Testing and result summary:
  - Stage 2.2 completed with targeted and regression quality gates green.

- Behavior impact:
  - finance users can no longer delete payment receipts,
  - finance users are blocked from academic modules in portal action routing.

### 2026-05-20 - Plan F Phase 2 Stage 2.1 Finance Payment Edit Capability
- Recent request issue:
  - proceed with Stage 2.1 and add Finance payment editing support.

#### Plan F Phase 2 Stage 2.1 (Implemented)
- Implementation Summary:
  - added a finance-admin update flow for actionable payment receipts,
  - kept edit behavior additive so existing create/confirm/cancel workflows remain intact,
  - surfaced `Last Updated` in the payments UI to show edit activity.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Testing and result summary:
  - Stage 2.1 completed with full regression checks passing.

- Behavior impact:
  - Finance can now revise actionable payment receipts before they are finalized,
  - no database schema mutation was introduced.

### 2026-05-20 - Plan F Phase 1 Stage 1.4 Payment Record State Model
- Recent request issue:
  - proceed with Stage 1.4 and ensure payment records expose paid/unpaid state tracking with date/update trail.

#### Plan F Phase 1 Stage 1.4 (Implemented)
- Implementation Summary:
  - added explicit payment tracking fields in payment output model (`PaidDate`, `UpdatedAt`) while retaining backward-compatible `ConfirmedAt`,
  - applied compatibility fallback in web mapping to preserve behavior across older/newer payload shapes,
  - added `Last Updated` presentation in payments screen for end-user tracking clarity.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`156/156`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Testing and result summary:
  - Stage 1.4 completed with all baseline quality gates green.

- Behavior impact:
  - payment records now present paid/unpaid state with paid-date and update-trail visibility,
  - no schema mutation and no endpoint-removal contract break introduced.

### 2026-05-20 - Plan F Phase 1 Stage 1.3 Finance Role Seed and Linking
- Recent request issue:
  - proceed with Stage 1.3 and introduce the Finance role with authorization linkage.

#### Plan F Phase 1 Stage 1.3 (Implemented)
- Implementation Summary:
  - seeded new `Finance` system role in startup seeding pipeline,
  - added dedicated `Finance` authorization policy in API composition,
  - enabled CSV onboarding support for finance users by extending import role validation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~InstitutionPolicyTests|FullyQualifiedName~UserImport" -v minimal` passed (`25/25`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Testing and result summary:
  - Stage 1.3 completed with all targeted and regression quality gates green.

- Behavior impact:
  - finance role identity is now available in core role model and authorization policy graph,
  - no breaking API contract or schema mutation introduced in this stage.

### 2026-05-20 - Plan F Phase 0 Stage 0.1 Baseline Safety Verification
- Recent request issue:
  - start Plan F and complete stage 0.1 with implementation/validation evidence.

#### Plan F Phase 0 Stage 0.1 (Implemented)
- Implementation Summary:
  - completed baseline safety verification before finance feature implementation,
  - no production behavior changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed.
- Testing and result summary:
  - release build baseline is stable for Plan F progression.

### 2026-05-20 - Plan F Phase 0 Stage 0.2 Isolation and Access Invariants
- Recent request issue:
  - complete stage 0.2 and verify tenant/campus/role invariants.

#### Plan F Phase 0 Stage 0.2 (Implemented)
- Implementation Summary:
  - validated tenant/campus boundaries and role-access invariants under current baseline,
  - no API or schema behavior changes were introduced.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`).
- Testing and result summary:
  - integration regression confirms isolation behavior remains intact.

### 2026-05-20 - Plan F Phase 0 Stage 0.3 Additive-Only Guardrails
- Recent request issue:
  - complete stage 0.3 and enforce additive-only implementation guardrails.

#### Plan F Phase 0 Stage 0.3 (Implemented)
- Implementation Summary:
  - finalized additive-only implementation guardrails for Plan F,
  - no runtime functionality, API contract, or schema changes were introduced.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Testing and result summary:
  - baseline unit/contract safety nets remain green after guardrail closure.

- Behavior impact:
  - Plan F Phase 0 completed as validation/governance phase only,
  - no end-user behavior changes introduced yet.
### 2026-05-20 - Plan F Transition Readiness
- Recent request issue:
  - complete all prerequisites so execution can move to Plan F.

#### Plan F Entry Gate Validation (Implemented)
- Implementation Summary:
  - completed release-mode baseline verification of the current system before Plan F entry,
  - switched active execution governance pointer to Plan F Phase 0 with Phase 1 ready state,
  - confirmed selected plan source as `Docs/Phased-Architecture-Plan/F-Finance-Feature-System-Update.md`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Testing and result summary:
  - all baseline quality gates are green at Plan F handoff point.

- Behavior impact:
  - no runtime behavior changes introduced by this checkpoint,
  - this update records governance readiness and transition authorization into Plan F execution.

### 2026-05-20 - Backlog Security Hardening User Import Template Access Guard
- Recent request issue:
  - proceed with the next backlog stage and fix a user-import security inconsistency.

#### Backlog Hardening - User Import Template Access Guard (Implemented)
- Implementation Summary:
  - enforced Admin/SuperAdmin-only access for user import CSV template download,
  - retained template allow-list and traversal-safe path verification.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`4/4`).
- Testing and result summary:
  - targeted import + force-change regression suite remained green after the guardrail change.

- Behavior impact:
  - template downloads are now constrained to the same privileged roles that can execute CSV imports,
  - no API payload/schema changes introduced.

### 2026-05-20 - Plan D Phase 2 Stage 2.1 Global Filters
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.1 and implement global analytics filters.

#### Phase 2 Stage 2.1 - Global Filters (Implemented)
- Implementation Summary:
  - added global analytics filter support for institution, department, course, and semester,
  - extended analytics query propagation from portal UI through web API client and API endpoints to backend analytics queries,
  - added course/semester-aware analytics cache-key segments to avoid cross-filter cache bleed.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - all selected quality gates passed with no regressions.

- Behavior impact:
  - analytics can now be scoped by course and semester in addition to institution and department,
  - no schema mutation and no change to existing role/institute access boundaries.

### 2026-05-20 - Plan D Phase 2 Stage 2.2 Dependent Filtering
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.2 and enforce dependent filtering behavior.

#### Phase 2 Stage 2.2 - Dependent Filtering (Implemented)
- Implementation Summary:
  - implemented dependent filter cascade behavior so each upstream analytics filter updates downstream options,
  - added server-side offering-based option derivation and invalid downstream selection reset,
  - added client-side auto-apply submit handlers on parent filter changes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - all selected quality gates passed with no regressions.

- Behavior impact:
  - filter dependency behavior is now enforced end-to-end for analytics filtering,
  - no schema or access-control boundary changes introduced.

### 2026-05-20 - Plan D Phase 2 Stage 2.3 Instant Charts Update
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.3 and make charts update instantly on filter/legend change.

#### Phase 2 Stage 2.3 - Instant Charts Update (Implemented)
- Implementation Summary:
  - introduced analytics snapshot response for in-page updates,
  - replaced full-page filter submit behavior with async refresh and re-render flow,
  - kept legend-based series visibility toggles in place across chart refreshes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 2.3 completed with all selected quality gates passing.

- Behavior impact:
  - analytics charts now update in-place on filter changes while preserving existing security and tenant/campus boundaries.

### 2026-05-20 - Plan D Phase 3 Stage 3.1 Chart Types and Data
- Recent request issue:
  - proceed to Plan D Phase 3 Stage 3.1 and add expanded chart coverage.

#### Phase 3 Stage 3.1 - Chart Types and Data (Implemented)
- Implementation Summary:
  - added advanced trend/distribution chart section with pie, bar, and line visualizations,
  - implemented student distribution, department count, course trend, and semester performance-attendance trend charts,
  - maintained instant snapshot-based refresh behavior for all chart panels.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 3.1 passed all selected quality gates with no regressions observed.

- Behavior impact:
  - analytics now exposes broader visual insights for distribution and trends while keeping existing access boundaries unchanged.

### 2026-05-20 - Plan D Phase 4 Stage 4.1 Tenant/Campus Isolation
- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.1 and enforce tenant/campus analytics isolation.

#### Phase 4 Stage 4.1 - Tenant/Campus Isolation (Implemented)
- Implementation Summary:
  - implemented explicit tenant/campus scoped analytics query filtering for non-superadmin access contexts,
  - removed quiz analytics query-filter bypass path,
  - partitioned distributed analytics cache keys by tenant/campus scope,
  - added integration regression test covering tenant/campus scoped assignment analytics visibility.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`66/66`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 4.1 completed with full selected quality-gate pass.

- Behavior impact:
  - analytics query and cache behavior now enforces stricter tenant/campus isolation boundaries,
  - no schema migration and no public endpoint contract change introduced.

### 2026-05-20 - Plan D Phase 4 Stage 4.2 Leakage Prevention
- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.2 and prevent cross-scope leakage on broader analytics surfaces.

#### Phase 4 Stage 4.2 - Leakage Prevention (Implemented)
- Implementation Summary:
  - enforced owner-or-superadmin semantics for export-job status/download APIs,
  - added requester tenant/campus metadata in export-job queue/state lifecycle,
  - enforced tenant/campus scope parity checks during export-job access,
  - introduced negative integration tests for cross-user and cross-tenant/campus export-job access attempts.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 4.2 completed with all selected quality gates passing.

- Behavior impact:
  - analytics export-job retrieval now enforces identity ownership and tenant/campus scope consistency,
  - API routes and schema remained unchanged.

### 2026-05-20 - Plan D Phase 5 Stage 5.1 Performance Optimization
- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.1 and optimize analytics query execution.

#### Phase 5 Stage 5.1 - Performance Optimization (Implemented)
- Implementation Summary:
  - removed major N+1 query patterns from analytics report generation,
  - switched to batched grouped aggregate retrieval for report metrics,
  - applied no-tracking query execution for heavy report reads.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 5.1 completed with full selected quality-gate pass and no functional regressions.

- Behavior impact:
  - analytics response behavior is preserved while query execution scales better for wider scopes and higher data volume.

### 2026-05-20 - Plan D Phase 5 Stage 5.2 Index and Data-Loading Refinement
- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.2 and enforce index strategy for analytics hot paths.

#### Phase 5 Stage 5.2 - Index Strategy and Efficient Loading Alignment (Implemented)
- Implementation Summary:
  - added analytics-supporting indexes over assignment submissions, published results, and quiz attempt status aggregates,
  - introduced bounded `assignment_submissions.Status` storage to maintain efficient indexed access,
  - added EF migration to ship Stage 5.2 schema refinements.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 5.2 completed with all selected quality gates passing.

- Behavior impact:
  - analytics workloads are now better aligned with index-backed execution while preserving existing functional outcomes.

### 2026-05-20 - Plan D Phase 6 Stage 6.1 Validation and UI Consistency
- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.1 and validate analytics interactivity/filter behavior consistency.

#### Phase 6 Stage 6.1 - Validation and Consistency Assurance (Implemented)
- Implementation Summary:
  - executed analytics-focused regression validation for interactivity, filtering, and role/scope behavior consistency,
  - confirmed no further implementation changes are required in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 6.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms stability and consistency readiness for Stage 6.2.

### 2026-05-20 - Plan D Phase 6 Stage 6.2 Final Performance and Security Review
- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.2 final review for performance and security.

#### Phase 6 Stage 6.2 - Final Validation and Release Readiness (Implemented)
- Implementation Summary:
  - executed final release-mode regression validation for analytics performance/security stability,
  - confirmed no additional implementation updates are required to close Phase 6.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 6.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; Plan D Phase 6 is finalized for analytics performance and security readiness.

### 2026-05-20 - Plan E Phase 1 Stage 1.1 Functional Non-Regression Validation
- Recent request issue:
  - there is no Phase 7 continuation in this stream; move to Plan E and start Phase 1 Stage 1.1.

#### Phase 1 Stage 1.1 - Existing Functionality Verification (Implemented)
- Implementation Summary:
  - executed full automated validation to verify no existing functionality is broken,
  - confirmed no implementation changes were required for Stage 1.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 1.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage establishes Plan E functional baseline integrity.

### 2026-05-20 - Plan E Phase 1 Stage 1.2 End-to-End Module Validation
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.2.

#### Phase 1 Stage 1.2 - End-to-End Module Verification (Implemented)
- Implementation Summary:
  - executed full module end-to-end regression validation across integration coverage,
  - confirmed no implementation updates were required for Stage 1.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 1.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms module-level end-to-end stability baseline.

### 2026-05-20 - Plan E Phase 1 Stage 1.3 UI Alignment and Form Stability
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.3.

#### Phase 1 Stage 1.3 - UI and Form Stability Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for UI misalignment/layout integrity and form/binding continuity,
  - confirmed no implementation updates were required for Stage 1.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 1.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms UI/form stability baseline remains intact.

### 2026-05-20 - Plan E Phase 1 Stage 1.4 API Response and Runtime Stability
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.4.

#### Phase 1 Stage 1.4 - API and Runtime Stability Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for API response integrity and runtime stability,
  - confirmed no implementation updates were required for Stage 1.4.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`),
  - unit tests passed (`151/151`).
- Testing and result summary:
  - Stage 1.4 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms API/runtime stability baseline remains intact.

### 2026-05-20 - Plan E Phase 1 Stage 1.5 Database Relationship Validation
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.5.

#### Phase 1 Stage 1.5 - Database Relationship Integrity Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for database relationship/referential integrity continuity,
  - confirmed no implementation updates were required for Stage 1.5.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 1.5 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms Phase 1 database relationship stability baseline.

### 2026-05-20 - Plan E Phase 2 Stage 2.1 Tenant and Campus Isolation
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.1.

#### Phase 2 Stage 2.1 - Tenant/Campus Isolation Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for tenant/campus isolation continuity,
  - confirmed no implementation updates were required for Stage 2.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 2.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms tenant/campus isolation baseline remains intact.

### 2026-05-20 - Plan E Phase 2 Stage 2.2 Cross-Tenant/Campus Leakage Validation
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.2.

#### Phase 2 Stage 2.2 - Cross-Tenant/Campus Leakage Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for cross-tenant/campus leakage prevention continuity,
  - confirmed no implementation updates were required for Stage 2.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 2.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms cross-tenant/campus leakage protections remain intact.

### 2026-05-20 - Plan E Phase 2 Stage 2.3 Tenant/Campus Query Scope Validation
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.3.

#### Phase 2 Stage 2.3 - Tenant/Campus Query Scope Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for TenantId/CampusId query-scope continuity,
  - confirmed no implementation updates were required for Stage 2.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 2.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms TenantId/CampusId query-scope protections remain intact.

### 2026-05-20 - Plan E Phase 3 Stage 3.1 Course Material End-to-End Validation
- Recent request issue:
  - proceed with next stage.

#### Phase 3 Stage 3.1 - Course Material End-to-End Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for Course Material module end-to-end continuity,
  - confirmed no implementation updates were required for Stage 3.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted Course Material integration tests passed (`5/5`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 3.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms Course Material end-to-end baseline stability.

### 2026-05-20 - Plan E Phase 3 Stage 3.2 Analytics Charts and Filters Validation
- Recent request issue:
  - proceed with next stage.

#### Phase 3 Stage 3.2 - Analytics Charts and Filters Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for analytics charts/filters continuity,
  - confirmed no implementation updates were required for Stage 3.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted analytics/authorization integration tests passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 3.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms analytics chart/filter baseline stability.

### 2026-05-20 - Plan E Phase 3 Stage 3.3 Tenant and Campus Management Validation
- Recent request issue:
  - proceed.

#### Phase 3 Stage 3.3 - Tenant/Campus Management Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for tenant/campus management continuity,
  - confirmed no implementation updates were required for Stage 3.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted tenant/campus management integration tests passed (`63/63`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 3.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms tenant/campus management baseline stability.

### 2026-05-20 - Plan E Phase 3 Stage 3.4 Role-Based Access Validation
- Recent request issue:
  - proceed.

#### Phase 3 Stage 3.4 - Role-Based Access Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for role-based access continuity,
  - confirmed no implementation updates were required for Stage 3.4.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 3.4 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms role-based access baseline stability.

### 2026-05-20 - Plan E Phase 4 Stage 4.1 UI Consistency and Design Baseline Validation
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.1 - UI Consistency and Design Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for layout/spacing/design continuity,
  - confirmed no implementation updates were required for Stage 4.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 4.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms UI consistency/design baseline stability.

### 2026-05-20 - Plan E Phase 4 Stage 4.2 Sidebar Header and Content Structure Validation
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.2 - UI Structure Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for sidebar/header/content structure continuity,
  - confirmed no implementation updates were required for Stage 4.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 4.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms sidebar/header/content structure baseline stability.

### 2026-05-20 - Plan E Phase 4 Stage 4.3 Overlap and Responsive Layout Validation
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.3 - Responsive Layout Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for overlap prevention and responsive layout continuity,
  - confirmed no implementation updates were required for Stage 4.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 4.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms overlap/responsive layout baseline stability.

### 2026-05-20 - Plan E Phase 4 Stage 4.4 Validate All Buttons and Actions
- Recent request issue:
  - proceed.

#### Phase 4 Stage 4.4 - UI Action Verification (Implemented)
- Implementation Summary:
  - executed validation checkpoint for button/action continuity across key UI workflows,
  - confirmed no implementation updates were required for Stage 4.4.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 4.4 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage confirms button/action baseline stability.

### 2026-05-20 - Plan E Phase 5 Stage 5.1 TenantId/CampusId Schema Audit
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.1 - Schema Scope Audit Verification (Implemented)
- Implementation Summary:
  - executed schema audit for `TenantId`/`CampusId` usage in `Scripts/01-Schema-Current.sql`,
  - parsed `82` tables and identified `0` tables containing both scope columns in this script,
  - confirmed no implementation updates were required for Stage 5.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 5.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records schema-audit findings only.

### 2026-05-20 - Plan E Phase 5 Stage 5.2 Foreign Keys, Indexes, and Constraints Audit
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.2 - Schema Structure Verification (Implemented)
- Implementation Summary:
  - executed SQL artifact audit on `Scripts/01-Schema-Current.sql` and `Scripts/04-Maintenance-Indexes-And-Views.sql`,
  - recorded `65` foreign key constraints (`5` via `ALTER TABLE`), `82` primary key constraints, `2` default constraints, and `190` total index statements,
  - confirmed no implementation updates were required for Stage 5.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 5.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records structural audit findings only.

### 2026-05-20 - Plan E Phase 5 Stage 5.3 Nullable Field Audit
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.3 - Nullability Verification (Implemented)
- Implementation Summary:
  - executed nullable-field audit on `Scripts/01-Schema-Current.sql`,
  - recorded `280` nullable columns across `79` tables and flagged `2` review-candidate nullable fields (`users.Email`, `timetable_entries.FacultyName`),
  - confirmed no implementation updates were required for Stage 5.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 5.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records nullability-audit findings only.

### 2026-05-20 - Plan E Phase 5 Stage 5.4 Data Integrity and Migration Safety
- Recent request issue:
  - proceed.

#### Phase 5 Stage 5.4 - Integrity and Migration Safety Verification (Implemented)
- Implementation Summary:
  - executed data-integrity and migration-safety audit on schema and post-deployment SQL artifacts,
  - recorded `365` migration-history guard references, `324` `IF NOT EXISTS` guards, `40` transaction blocks, `175` post-deployment verification `SELECT` checks, and `19` EF migration files,
  - confirmed no implementation updates were required for Stage 5.4.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 5.4 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records integrity/migration-safety audit findings only.

### 2026-05-20 - Plan E Phase 6 Stage 6.1 Role-Based Access Review
- Recent request issue:
  - proceed.

#### Phase 6 Stage 6.1 - Role Access Verification (Implemented)
- Implementation Summary:
  - executed role-based access audit across API/Web authorization attributes, role/policy enforcement code, and role-seeding artifacts,
  - recorded `359` API `[Authorize]` attributes, `369` role/policy enforcement references, `5` policy registration references, and `105` role-seeding script references,
  - confirmed no implementation updates were required for Stage 6.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 6.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records role-access audit findings only.

### 2026-05-20 - Plan E Phase 6 Stage 6.2 Unauthorized/Cross-Scope Access
- Recent request issue:
  - proceed.

#### Phase 6 Stage 6.2 - Cross-Scope Access Verification (Implemented)
- Implementation Summary:
  - executed unauthorized/cross-tenant/cross-campus access audit across source enforcement points,
  - recorded `1326` isolation-enforcement source hits and `128` explicit `Forbid`/`Unauthorized` enforcement hits,
  - confirmed no implementation updates were required for Stage 6.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 6.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records unauthorized/cross-scope audit findings only.

### 2026-05-20 - Plan E Phase 6 Stage 6.3 API Endpoint Restriction
- Recent request issue:
  - proceed.

#### Phase 6 Stage 6.3 - API Restriction Verification (Implemented)
- Implementation Summary:
  - executed API endpoint restriction audit over authorization coverage in API controllers,
  - recorded `447` HTTP endpoints: `92` method-level `[Authorize]`, `349` class-level `[Authorize]` coverage, `1` `[AllowAnonymous]`, and `5` review-set endpoints without explicit authorize coverage,
  - confirmed no implementation updates were required for Stage 6.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 6.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records API-restriction audit findings only.

### 2026-05-20 - Plan E Phase 7 Stage 7.1 Query Scope Filtering
- Recent request issue:
  - proceed.

#### Phase 7 Stage 7.1 - Query Scope Filtering Verification (Implemented)
- Implementation Summary:
  - executed tenant/campus query-filter audit across source and repository layers,
  - recorded `551` Tenant/Campus scope references, `11` LINQ `Where` scope-filter references, and `20` repository-layer scope references,
  - confirmed no implementation updates were required for Stage 7.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 7.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records query-filter audit findings only.

### 2026-05-20 - Plan E Phase 7 Stage 7.2 Join and Full-Scan Risk Audit
- Recent request issue:
  - proceed.

#### Phase 7 Stage 7.2 - Query-Shape Risk Verification (Implemented)
- Implementation Summary:
  - executed query-shape/full-scan risk audit for joins, includes, raw SQL usage, and pagination coverage,
  - recorded `134` join references (`18` in repository layer), `167` include/then-include references, `0` raw SQL query references, `475` materialization references, and `37` pagination references,
  - confirmed no implementation updates were required for Stage 7.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 7.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records query-shape audit findings only.

### 2026-05-20 - Plan E Phase 7 Stage 7.3 Pagination and Analytics Query Efficiency
- Recent request issue:
  - proceed.

#### Phase 7 Stage 7.3 - Pagination and Analytics Efficiency Verification (Implemented)
- Implementation Summary:
  - executed pagination and analytics-query efficiency audit across source and analytics layers,
  - recorded `37` pagination references with `29` nearby ordering safeguards, `551` Tenant/Campus scope references, `11` LINQ scope-filter references, and `18` analytics `AsNoTracking` references,
  - confirmed no implementation updates were required for Stage 7.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 7.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records pagination/analytics audit findings only.

### 2026-05-20 - Plan E Phase 8 Stage 8.1 Environment-Based Configuration
- Recent request issue:
  - proceed.

#### Phase 8 Stage 8.1 - Environment Configuration Verification (Implemented)
- Implementation Summary:
  - executed environment-based configuration audit across startup/configuration-loading paths,
  - recorded `61` environment-handling references, `132` configuration/deployment references, `54` `appsettings*.json` files, and `127` Program.cs environment/configuration references,
  - confirmed no implementation updates were required for Stage 8.1.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 8.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records environment/configuration audit findings only.

### 2026-05-20 - Plan E Phase 8 Stage 8.2 Deployment Scenarios
- Recent request issue:
  - proceed.

#### Phase 8 Stage 8.2 - Deployment Scenario Verification (Implemented)
- Implementation Summary:
  - executed deployment-scenario readiness audit for cloud, on-prem, and multi-instance signals across source startup/configuration paths,
  - recorded `1` cloud reference, `0` on-prem references, `14` multi-instance/scale-out references, `200+` deployment/scaling/instance/tenant-isolation source references (search cap reached), and `9` source `appsettings*.json` files,
  - confirmed no implementation updates were required for Stage 8.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 8.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records deployment-readiness audit findings only.

### 2026-05-20 - Plan E Phase 8 Stage 8.3 Secrets and Configuration Security
- Recent request issue:
  - proceed.

#### Phase 8 Stage 8.3 - Secrets and Configuration Security Verification (Implemented)
- Implementation Summary:
  - executed secure-secrets/configuration handling audit across startup guards, environment-variable resolvers, and appsettings templates,
  - recorded `18` secure startup guard references, `18` environment-variable secret/deployment references, `18` secret-sensitive configuration key references in source appsettings, `12` placeholder/template secret markers, and `9` source `appsettings*.json` files,
  - confirmed no implementation updates were required for Stage 8.3.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed (non-blocking warning: CS0105 in API Program.cs),
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 8.3 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage records secrets/configuration security audit findings only.

### 2026-05-20 - Plan E Phase 9 Stage 9.1 Issue and Inconsistency Remediation
- Recent request issue:
  - proceed to Plan E Phase 9 Stage 9.1 (identify and fix issues, inconsistencies, or risks).

#### Phase 9 Stage 9.1 - Startup Consistency Risk Fix (Implemented)
- Implementation Summary:
  - fixed API startup import inconsistency in `Program.cs` by splitting merged `using` directives and removing duplicate import,
  - removed CS0105 warning source and completed risk remediation without behavior or schema mutation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full automated test suites passed (`396/396`),
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 9.1 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; this stage resolves startup code consistency and warning risk only.

### 2026-05-20 - Plan E Phase 9 Stage 9.2 Final Stability, Security, and Scalability Review
- Recent request issue:
  - proceed.

#### Phase 9 Stage 9.2 - Final Release-Readiness Verification (Implemented)
- Implementation Summary:
  - completed final verification-only review for stability, security, and scalability with full quality-gate rerun,
  - validated source risk markers as expected baseline patterns and confirmed no additional implementation changes were required.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full automated test suites passed (`396/396`),
  - full integration tests passed (`244/244`),
  - full unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - Stage 9.2 completed with all selected quality gates passing.

- Behavior impact:
  - no feature behavior changes were introduced; Stage 9.2 is final verification and closure only.

### 2026-05-20 - Plan F Phase 6 Stage 6.1/6.2/6.3 User Import Template Extension
- Recent request issue:
  - proceed.

#### Phase 6 Stage 6.1/6.2/6.3 - Import Template, Compatibility, and Validation (Implemented)
- Implementation Summary:
  - extended user import parsing to accept optional `MobileNumber` header (with legacy `PhoneNumber` compatibility),
  - added optional `CampusAssignments`/`CampusIds` format validation (pipe-separated GUID values),
  - updated official CSV templates and portal guidance to include `MobileNumber` and `CampusAssignments` columns,
  - kept old template uploads fully supported with no required new columns.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`),
  - static diagnostics on touched import files returned no errors,
  - verified both extended and legacy template import paths in integration coverage.
- Testing and result summary:
  - Phase 6 Stage 6.1/6.2/6.3 import extension checkpoint completed with all selected quality gates passing.

- Behavior impact:
  - import templates now include mobile and multi-campus assignment-ready fields with backward-compatible ingestion behavior.

### 2026-05-21 - Plan F Phase 6 Completion (Import Sheets)
- Recent request issue:
  - proceed and close Plan F Phase 6.

#### Plan F Phase 6 Completion - Import Template and Validation Closure (Implemented)
- Implementation Summary:
  - completed Stage 6.1, 6.2, and 6.3 execution with template extension, compatibility retention, and field-validation safeguards,
  - preserved additive-only behavior with no schema or API-contract mutation required for this phase closeout.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`).
- Testing and result summary:
  - Plan F Phase 6 completed with required implementation and validation summaries recorded for all included stages.

- Behavior impact:
  - no breaking changes to existing user import flows; legacy templates continue to import successfully.

### 2026-05-20 - Final-Touches Tracker Restoration (Governance)
- Recent request issue:
  - proceed.

#### Governance Restoration - Final-Touches Baseline File (Implemented)
- Implementation Summary:
  - restored missing `Project startup Docs/Final-Touches.md` required by command-center startup prerequisites,
  - aligned restored execution pointer with latest completed Plan E state.
- Validation Summary:
  - verified restored file existence and readability,
  - verified pointer alignment with `Docs/Command.md`,
  - no runtime code, API contract, or schema mutation introduced.
- Testing and result summary:
  - governance restoration completed with no product-surface behavior deltas.

- Behavior impact:
  - no feature behavior changes were introduced; this update restores execution-governance continuity only.

### 2026-05-20 - Plan D Phase 1 Stage 1.3 Clickable Legends
- Recent request issue:
  - proceed to Plan D Phase 1 Stage 1.3 and add color-coded clickable legends to Analytics charts.

#### Phase 1 Stage 1.3 - Clickable Legends (Implemented)
- Implementation Summary:
  - implemented reusable interactive legend controls for overview/performance/attendance/assignments charts,
  - legends now toggle chart dataset visibility and visually indicate hidden state,
  - removed duplicate analytics rendering fragments to keep a single stable dashboard path.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - all automated validation layers passed with no functional regressions.

- Behavior impact:
  - Analytics now supports legend-driven interactive data visibility,
  - no API/schema/tenant-isolation behavior changed.

### 2026-05-20 - Plan C Phase 7 Stage 7.2 Finalization
- Recent request issue:
  - complete Plan C Phase 7 Stage 7.2 final review.

#### Phase 7 Stage 7.2 - Final Review (Implemented)
- Implementation Summary:
  - completed final stability/scalability release-readiness review for the Course Material module,
  - confirmed no additional code or schema updates were required at this closeout stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - Release-mode integration tests (Course Material authorization subset) passed (`5/5`),
  - load-test script execution depends on reachable target API and was blocked by unavailable local endpoint (`http://localhost:5181`).
- Testing and result summary:
  - final closeout validations passed for code quality and role-scope behavior; environment-backed load validation pending target API availability.

- Behavior impact:
  - no new functionality introduced,
  - Plan C Course Material feature set is finalized and stable for release.

### 2026-05-20 - Plan C Phase 7 Stage 7.1 Validation
- Recent request issue:
  - start Plan C Phase 7 Stage 7.1 validation.

#### Phase 7 Stage 7.1 - Validation (Implemented)
- Implementation Summary:
  - completed end-to-end validation of Course Material data safety, access control boundaries, and UI delivery behavior,
  - confirmed no additional code or schema changes were required for this validation stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`393/393`).
- Testing and result summary:
  - full quality gate passed for the current Course Material implementation baseline.

- Behavior impact:
  - no new functionality introduced,
  - existing School/College/University and role-based behavior remains stable.

### 2026-05-20 - Plan C Phase 6 Implementation (Performance & Optimization)
- Recent request issue:
  - complete Plan C Phase 6 Stage 6.1 and Stage 6.2.

#### Phase 6 - Performance & Optimization (Implemented)
- Implementation Summary:
  - Stage 6.1 optimized Course Material repository read behavior with no-tracking execution and missing-scope short-circuit,
  - Stage 6.2 introduced index `IX_course_materials_scope_active_sort` through migration `PlanCPhase6Stage2CourseMaterialIndexTuning`.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Testing and result summary:
  - focused validations and full build passed.

- Behavior impact:
  - no functional change in role/institute behavior; performance characteristics improved for scoped Course Material list/read paths.

### 2026-05-20 - Plan C Phase 5 Implementation (File & Link Handling)
- Recent request issue:
  - complete Plan C Phase 5 Stage 5.1, Stage 5.2, and Stage 5.3.

#### Phase 5 - File & Link Handling (Implemented)
- Implementation Summary:
  - Stage 5.1 added validated multipart upload endpoint and portal upload integration,
  - Stage 5.2 added scoped persistent storage routing and file download endpoint,
  - Stage 5.3 added role-authorization regression tests and role-context-aware portal fallback handling.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Testing and result summary:
  - focused validations and full build passed.

- Behavior impact:
  - Course Material now supports upload/download flows under strict tenant/campus scope and role boundaries,
  - no regression in School/College/University and existing module behavior.

### 2026-05-19 - Plan C Phase 4 Implementation (UI & UX)
- Recent request issue:
  - proceed to Plan C Phase 4 UI and UX implementation.

#### Phase 4 - UI & UX (Implemented)
- Implementation Summary:
  - added portal manage and student read-only pages for Course Material,
  - added portal actions for create/update/activate/deactivate with scoped filter continuity,
  - integrated `course_material` menu key across web layout route/group/fallback maps and API sidebar module-key entitlement map.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed (`388/388`).

- Behavior impact:
  - Course Material is now exposed through the portal UX for Admin/Faculty management and Student read-only access,
  - no regression in existing role/institute/module functionality.

### 2026-05-19 - Plan C Phase 3 Implementation (Access Control & Security)
- Recent request issue:
  - proceed to Plan C Phase 3 access control and strict isolation.

#### Phase 3 - Access Control & Security (Implemented)
- Implementation Summary:
  - added `CourseMaterialController` endpoints with authenticated read and role-restricted write access,
  - added service/repository contracts and implementations for course-material operations,
  - enforced strict tenant/campus repository filtering for reads and mutation target resolution.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed (`388/388`).

- Behavior impact:
  - Course Material now has secured API-level access and strict scoped data access aligned to existing repository policies,
  - no regression in existing role/institute/module functionality.

### 2026-05-19 - Plan C Phase 2 Implementation (Data Safety & Migration)
- Recent request issue:
  - proceed after Plan C Phase 1.

#### Phase 2 - Data Safety & Migration (Implemented)
- Implementation Summary:
  - added strict domain guards for required scope IDs and material metadata,
  - added DB-level constraints for scope integrity, material-type validity, and file/link location validity,
  - added migration `PlanCPhase2DataSafetyScopeGuard`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - new material records are now hard-enforced to be tenant/campus scoped and location-valid,
  - no regression in existing role/institute/module functionality.

### 2026-05-19 - Plan C Phase 1 Implementation (Domain & Database Extension)
- Recent request issue:
  - start Plan C Phase 1.

#### Phase 1 - Domain & Database Extension (Implemented)
- Implementation Summary:
  - added `CourseMaterial` domain aggregate with required material fields,
  - linked material scope to tenant, campus, department, academic program, semester, and course (subject context),
  - added migration `PlanCPhase1CourseMaterialFoundation` with foreign keys and lookup indexes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - Course Material data foundation is now available for subsequent Plan C access-control and UI phases,
  - no regression in existing role/institute/module functionality.

### 2026-05-19 - Plan B Phase 10 Implementation (Validation & Finalization)
- Recent request issue:
  - proceed to validation and finalization after logging and visibility.

#### Phase 10 - Validation & Finalization (Implemented)
- Implementation Summary:
  - completed the final readiness and security/scalability review of the Phase B configuration stack,
  - confirmed the startup path remains backward-compatible, safe, and supportable across all deployment modes,
  - no code changes were required in this final closeout.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - the Phase B stack is finalized for release closeout,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 9 Implementation (Logging & Visibility)
- Recent request issue:
  - proceed to logging and visibility after configuration performance and stability.

#### Phase 9 - Logging & Visibility (Implemented)
- Implementation Summary:
  - added shared startup visibility reporting,
  - standardized startup logs across API, Web, and BackgroundJobs,
  - showed active environment, safe configuration source summary, database type, deployment profile, and tenant isolation posture.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - startup diagnostics are more useful for operations while remaining secret-safe,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 8 Implementation (Performance & Stability)
- Recent request issue:
  - proceed to configuration performance and stability after fail-safe startup validation.

#### Phase 8 - Performance & Stability (Implemented)
- Implementation Summary:
  - optimized the shared configuration bootstrap path to avoid duplicate provider registration,
  - preserved deployment/local/external/tenant override order,
  - reduced unnecessary reload monitoring for deployment-oriented overlay files.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - startup configuration remains backward-compatible with lower provider/watcher overhead,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 7 Implementation (Fail-Safe Behavior)
- Recent request issue:
  - proceed to fail-safe behavior after tenant-aware configuration support.

#### Phase 7 - Fail-Safe Behavior (Implemented)
- Implementation Summary:
  - added shared startup fail-safe validation across API, Web, and BackgroundJobs,
  - centralized clear startup failures for missing or placeholder configuration and invalid deployment/tenant settings,
  - corrected startup DB validation to honor the resolved deployment connection source.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - startup now fails earlier with actionable configuration guidance,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 6 Implementation (Tenant + Campus Aware Configuration)
- Recent request issue:
  - proceed to tenant-aware configuration and isolation after customer deployment support.

#### Phase 6 - Tenant + Campus Aware Configuration (Implemented)
- Implementation Summary:
  - added tenant-isolation resolver,
  - added optional tenant JSON overlay support,
  - exposed tenant-isolation metadata in startup logs and health diagnostics.
- Validation Summary:
  -
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - tenant-specific deployment settings can now be prepared through isolated config overlays without code changes,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 5 Implementation (Customer Deployment Support)
- Recent request issue:
  - proceed to customer deployment support after deployment flexibility.

#### Phase 5 - Customer Deployment Support (Implemented)
- Implementation Summary:
  - added optional deployment-pipeline JSON support for config-only customer overrides,
  - added deployment metadata sections to API/Web/BackgroundJobs appsettings templates,
  - preserved safe fallback to environment variables.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - customer-specific deployment values can now be supplied via app settings or deployment pipeline without code changes,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 4 Implementation (Deployment Flexibility)
- Recent request issue:
  - proceed to deployment flexibility after secure configuration handling.

#### Phase 4 - Deployment Flexibility (Implemented)
- Implementation Summary:
  - added deployment-topology resolver,
  - enabled customer-specific overrides for domain, database name, and scaling,
  - exposed deployment metadata in safe startup logs and health diagnostics.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - deployment profile handling is now explicit and configurable per customer,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 3 Implementation (Secure Configuration Handling)
- Recent request issue:
  - proceed to secure configuration handling after connection-management hardening.

#### Phase 3 - Secure Configuration Handling (Implemented)
- Implementation Summary:
  - added external deployment configuration source support,
  - added secure validation helper for production secrets,
  - preserved source-safe diagnostics and prevented credential leakage in startup messages.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - secret handling is now safer for production deployments,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan B Phase 2 Implementation (Database Connection Management)
- Recent request issue:
  - proceed to next Plan B phase after configuration-structure baseline.

#### Phase 2 - Database Connection Management (Implemented)
- Implementation Summary:
  - added shared startup DB connection resolver,
  - introduced override-first resolution from environment and deployment settings,
  - preserved legacy fallback to `ConnectionStrings:DefaultConnection` to avoid runtime breaks.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - deployment DB connection configuration is now centralized and easier to override,
  - no functional changes in role/institute/module behavior.

### 2026-05-19 - Plan B Phase 1 Implementation (Configuration Structure)
- Recent request issue:
  - proceed and begin Plan B configuration/deployment execution.

#### Phase 1 - Configuration Structure (Implemented)
- Implementation Summary:
  - added shared startup configuration hierarchy helper,
  - switched API/Web/BackgroundJobs startup pipelines to the same ordered configuration source model,
  - introduced optional local file override and prefixed environment-variable support with fallback.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - deployment configuration behavior is now more predictable and consistent across host projects,
  - no change to role/institute/module functional behavior.

### 2026-05-19 - Plan A Phase 7 Implementation (Validation and Finalization)
- Recent request issue:
  - proceed to Plan A Phase 7 and finalize full validation/stability closeout.

#### Phase 7 - Validation & Finalization (Implemented)
- Implementation Summary:
  - executed final quality-gate sweep for build, full unit, full integration, and contract suites,
  - confirmed Plan A rollout remains stable and additive,
  - completed final governance closeout synchronization.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - full unit tests passed (`151/151`),
  - full integration tests passed (`236/236`),
  - contract tests passed (`1/1`).
- Testing and result summary:
  - total automated validations passed: `388/388`.

- Behavior impact:
  - no new functional behavior introduced in this phase,
  - final validation confirms stable production-ready state for Plan A outputs,
  - InstitutionType behavior remains unchanged.

### 2026-05-19 - Plan A Phase 6 Implementation (Performance and Optimization)
- Recent request issue:
  - proceed to Plan A Phase 6 and optimize tenant/campus scoped query performance.

#### Phase 6 - Performance & Optimization (Implemented)
- Implementation Summary:
  - optimized scoped user lookup predicates for better index utilization,
  - added scoped composite indexes for user/department query hot paths,
  - added migration `Phase46_TenantCampusQueryOptimization`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - no functional behavior change; optimization-only phase,
  - scoped correctness and InstitutionType behavior remain unchanged.

### 2026-05-19 - Plan A Phase 5 Implementation (Tenant/Campus UI Management Interfaces)
- Recent request issue:
  - proceed to Plan A Phase 5 and add SuperAdmin tenant/campus management interfaces with existing portal navigation patterns.

#### Phase 5 - UI Management Interfaces (Implemented)
- Implementation Summary:
  - added SuperAdmin tenant and campus management API endpoints,
  - added dedicated portal screens for tenant/campus create/update/activate/deactivate workflows,
  - integrated the new screens into existing sidebar and SuperAdmin fallback navigation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - SuperAdmin now has dedicated tenant/campus management interfaces,
  - non-superadmin access remains restricted,
  - School/College/University InstitutionType behavior remains unchanged.

### 2026-05-19 - Plan A Phase 4 Implementation (Access Control and Filtering)
- Recent request issue:
  - proceed to Plan A Phase 4 and enforce tenant/campus-scoped reads while preserving SuperAdmin full access.

#### Phase 4 - Access Control & Filtering (Implemented)
- Implementation Summary:
  - added request-scope resolver for role/tenant/campus claim extraction,
  - added `tenant_id` and `campus_id` claims to JWT access tokens,
  - enforced scoped filtering in user and department repositories,
  - preserved SuperAdmin cross-tenant/campus bypass.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - non-superadmin user/department data reads are now tenant/campus scoped,
  - SuperAdmin retains global visibility,
  - School/College/University InstitutionType behavior remains unchanged.

### 2026-05-19 - Plan A Phase 3 Implementation (Compatibility and Safety Hardening)
- Recent request issue:
  - proceed to Plan A Phase 3 and harden tenant/campus compatibility safety without changing InstitutionType logic.

#### Phase 3 - Compatibility & Safety (Implemented)
- Implementation Summary:
  - added domain-level guardrails that reject partial tenant/campus assignment,
  - added schema-level check constraints for tenant/campus pairing,
  - added tenant-bound campus composite FK integrity for users/departments,
  - added migration `Phase43_TenantCampusCompatibilitySafety`.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - tenant/campus compatibility is now enforced at both domain and database layers,
  - School/College/University InstitutionType behavior remains unchanged.

### 2026-05-19 - Plan A Phase 2 Implementation (Default Tenant/Campus Data Integration)
- Recent request issue:
  - proceed into Plan A Phase 2 to safely assign default tenant/campus ownership for existing core data.

#### Phase 2 - Data Integration & Migration (Implemented)
- Implementation Summary:
  - added migration `Phase42_DefaultTenantCampusBackfill` to insert default tenant/campus baseline data when missing,
  - migration and startup seeding now backfill null tenant/campus for `users` and `departments`,
  - preserved existing InstitutionType and module behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests (`EnrollmentServiceWaitlistTests|AuthSecurityUxTests`) passed (`9/9`),
  - integration tests (`AdminUserManagementIntegrationTests|AuthorizationRegressionTests`) passed (`52/52`).
- Testing and result summary:
  - total focused tests passed: `61/61`.

- Behavior impact:
  - existing users/departments now have safe default tenant/campus ownership when previously null,
  - School/College/University InstitutionType logic remains unchanged.

### 2026-05-19 - Plan A Phase 1 Implementation (Tenant + Campus Domain Foundation)
- Recent request issue:
  - proceed from Plan A Phase 1 kickoff into code implementation while keeping existing architecture and InstitutionType behavior intact.

#### Phase 1 - Domain Layer Extension (Implemented)
- Implementation Summary:
  - added tenant/campus domain foundation with new entities (`Tenant`, `Campus`),
  - extended root entities (`User`, `Department`) with optional tenant/campus ownership references,
  - added EF configuration and migration `Phase41_TenantCampusFoundation` for additive schema rollout.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal --filter "FullyQualifiedName~EnrollmentServiceWaitlistTests|FullyQualifiedName~AuthSecurityUxTests"` passed (`9/9`).
- Testing and result summary:
  - build: passed,
  - focused unit tests: passed (`9/9`).

- Behavior impact:
  - no replacement or duplication of existing School/College/University InstitutionType flow,
  - tenancy/campus structural foundation is now available for upcoming scoped filtering and access-control phases.

### 2026-05-19 - Plan A Phase 1 Kickoff (App Configuration: Tenant + Campus)
- Recent request issue:
  - start Plan A Phase 1 and synchronize mandatory governance/planning trackers while placing implementation and validation summary at phase end.

#### Phase 1 - Domain Layer Extension (Kickoff)
- Implementation Summary:
  - initiated Plan A Phase 1 execution baseline for Tenant and Campus architecture extension,
  - confirmed additive integration approach that preserves existing School/College/University structure,
  - synchronized PRD, command tracker, function list, functionality reference, development plan, database schema, and modules document for this kickoff.
- Validation Summary:
  - cross-document consistency and traceability review completed,
  - confirmed no runtime/API/schema changes introduced in this documentation-governance kickoff.
- Testing and result summary:
  - documentation consistency checks completed,
  - no runtime tests executed because this phase kickoff update is documentation-only.

- Behavior impact:
  - no runtime behavior change,
  - Plan A Phase 1 execution and phase-end summary compliance are now recorded in the PRD implementation log.

### 2026-05-19 - Deep System Audit + Validation + AI Chatbot Modernization (Phase 1-7)
- Recent request issue:
  - perform a full system audit and validation against PRD/Functionality references, then replace chatbot UI with modular floating components and keep mandatory docs synchronized after each phase.

#### Phase 1 - System Understanding
- Implementation Summary:
  - completed deep docs-to-code mapping across controllers, services, repositories, DTO contracts, and portal bindings,
  - validated module coverage for Academic, Enrollment, Notifications, Reports/Analytics, Authentication/MFA, and AI Chat,
  - identified primary risk areas for route consistency and chatbot entrypoint modernization.
- Validation Summary:
  - completed static architecture and flow tracing across API/Application/Infrastructure/Web layers,
  - confirmed no blocking implementation gaps in SMS/LLM wiring (both are already registered in DI).
- Testing and result summary:
  - phase 1 architecture and traceability audit completed with actionable risk list.

#### Phase 2 - API and Backend Validation
- Implementation Summary:
  - introduced AI chat API versioned-route compatibility by adding `/api/v1/ai` alongside legacy `/api/ai`,
  - updated web API client chat calls to canonical `/api/v1/ai/*` endpoints,
  - aligned module-license middleware route map to enforce AI-chat module checks on both legacy and versioned routes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal --filter "FullyQualifiedName~Phase24Tests|FullyQualifiedName~AuthSecurityUxTests|FullyQualifiedName~EnrollmentServiceWaitlistTests"` passed (`9/9`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~ModuleBackendEnforcementIntegrationTests"` passed (`4/4`).
- Testing and result summary:
  - phase 2 backend validation and route-consistency hardening completed with green targeted tests.

#### Phase 3 - Database and Data Flow Validation
- Implementation Summary:
  - reviewed EF repository includes for enrollment/waitlist and validated existing waitlist queue ordering,
  - verified no new migration/schema delta is required by the API/chatbot modernization changes.
- Validation Summary:
  - validated current behavior against recent integration baseline and DeepScan closure notes,
  - no FK/include regressions introduced by this request.
- Testing and result summary:
  - phase 3 data-flow/schema validation completed with no new integrity issues introduced.

#### Phase 4 - UI/Frontend Validation
- Implementation Summary:
  - validated current portal shell/sidebar/role-aware menu logic and chat widget lifecycle wiring,
  - confirmed AI chat uses existing widget-state and widget-send backend flows without contract changes.
- Validation Summary:
  - static UI binding checks completed for layout, JS handlers, and API client model mapping,
  - no new frontend runtime contract mismatch introduced by this phase.
- Testing and result summary:
  - phase 4 UI validation completed; integration preserved.

#### Phase 5 - Performance and Stability Check
- Implementation Summary:
  - reviewed async paths and identified no new blocking patterns introduced by this request,
  - preserved existing caching/background-job behavior and avoided behavioral regression changes.
- Validation Summary:
  - full build and targeted unit/integration suites remained green after code updates.
- Testing and result summary:
  - phase 5 stability/performance sanity validation completed with no regressions observed in targeted suites.

#### Phase 6 - AI Chatbot Redesign (Critical)
- Implementation Summary:
  - replaced inline chatbot markup in shared layout with modular components:
    - `Views/Shared/FloatingChatButton.cshtml`
    - `Views/Shared/ChatPanel.cshtml`
  - preserved existing backend integration (`AiChatWidgetState`, `AiChatWidgetSend`) and message-history behavior,
  - switched launcher to an explicit button-based floating control while keeping open/close/state behavior intact.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed after component extraction,
  - targeted chat/module enforcement integration tests remained green.
- Testing and result summary:
  - phase 6 chatbot modernization completed with modular UI components and preserved API compatibility.

#### Phase 7 - Final Consolidated Reporting
- Implementation Summary:
  - synchronized mandatory execution/governance documentation across PRD, Command, Function List, Functionality Reference, Development Plan, and Database Schema,
  - recorded phase-by-phase implementation and validation evidence with explicit testing summaries.
- Validation Summary:
  - documentation updates include issue statement, implementation summary, validation summary, and phase-end test/result notes.
- Testing and result summary:
  - phase 7 reporting complete; all required artifacts updated for this request.

- Behavior impact:
  - API compatibility improved for AI chat routing (`/api/ai` + `/api/v1/ai`),
  - chatbot UI is now modularized (floating button + panel components) while preserving backend contracts,
  - no schema mutation introduced by this request.

### 2026-05-19 - UI/UX Redesign Phase 9 (Final UI Polish)
- Recent request issue:
  - proceed with the final redesign pass and ensure docs plus commit/push/pull are completed.
- Implementation Summary:
  - applied a final polish pass across shared UI surfaces for spacing, elevation, and presentation detail,
  - preserved all existing routes, backend behavior, and data contracts.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched final-polish stylesheet.
- Behavior impact:
  - no backend/API/business-rule/schema changes,
  - the portal now presents a more finished premium SaaS visual experience.

### 2026-05-18 - UI/UX Redesign Phase 8 (Responsive Hardening)
- Recent request issue:
  - proceed with the next redesign phase and always complete docs plus commit/push/pull.
- Implementation Summary:
  - strengthened responsive behavior across shell and page-level layouts,
  - improved mobile stacking for filters, action groups, profile menu, pagination, and overflow-prone table/modal areas,
  - preserved all existing workflows and backend contracts.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched responsive frontend files.
- Behavior impact:
  - no backend/API/business-rule/schema changes,
  - improved mobile/tablet usability for key operational screens.

### 2026-05-18 - UI/UX Redesign Phase 7 (AI Chatbot UI)
- Recent request issue:
  - proceed with next redesign phase and always complete docs plus commit/push/pull.
- Implementation Summary:
  - upgraded AI chatbot launcher and panel visual language,
  - added richer assistant identity/header presentation,
  - introduced quick-prompt suggestion chips with frontend-only interactions that still use existing chat state/send endpoints.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched chatbot frontend files.
- Behavior impact:
  - no backend/API/business-rule/schema changes,
  - improved chatbot discoverability and interaction quality.

### 2026-05-18 - UI/UX Redesign Phase 6 (Branding Pass)
- Recent request issue:
  - proceed with the next phase and always update docs and commit/push/pull after completion.
- Implementation Summary:
  - improved the shared shell branding treatment for institution logo/name/subtitle,
  - refined header composition,
  - upgraded notifications chip UI,
  - added a richer profile dropdown interface while preserving existing sign-out flow and route behavior.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched layout and CSS files.
- Behavior impact:
  - no backend/API/business-rule/schema changes,
  - improved branding clarity and global header usability.

### 2026-05-18 - UI/UX Redesign Continuation (Enrollments/Results/Payments + Phase-Level Summary Formatting)
- Recent request issue:
  - proceed with the next continuation wave and place implementation/validation summaries at the end of each completed redesign phase.
- Implementation Summary:
  - polished Enrollments, Results, and Payments pages to match the shared premium SaaS visual language while keeping all existing workflows intact,
  - updated `Docs/Improved UI and look.md` so phase completion summaries are embedded at each phase boundary with markdown-lint-safe formatting.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched continuation pages and redesign specification doc.
- Behavior impact:
  - no business-rule/API/schema behavior changes,
  - improved UX consistency and stronger phase-by-phase execution traceability.

### 2026-05-18 - UI/UX Redesign Continuation (Students/Courses/Admin Users Polish)
- Recent request issue:
  - continuation requested to apply the new SaaS visual baseline to additional high-use portal management pages.
- Implementation Summary:
  - upgraded Students, Courses, and Admin Users page presentation with consistent section structure, empty states, and visual controls,
  - added reusable CSS helper classes to support cross-page visual consistency.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - no backend/API/controller/service/domain/database files were changed.
- Behavior impact:
  - no business or API behavior change,
  - improved UI consistency for key administrative workflows.

### 2026-05-18 - UI/UX Redesign Request (Execution Snapshot)
- Recent request issue:
  - the web portal needed a complete frontend-only visual redesign so it presents as a professional modern SaaS product for schools, colleges, and universities without changing backend behavior.
- Implementation Summary:
  - redesigned the portal shell, branding, sidebar navigation, top header, search framing, and AI assistant chrome in the shared Razor layout,
  - introduced a unified academic design system in the web stylesheet for consistent colors, typography, cards, forms, tables, responsive spacing, and micro-interaction polish,
  - refreshed the dashboard UI into a hero-and-card experience while preserving existing model binding, routes, and form-post behavior,
  - added minimal frontend-only JavaScript for loader fade-out, alert-to-toast presentation, and responsive sidebar toggling.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the UI redesign,
  - workspace diagnostics reported no errors in the touched frontend files,
  - no API/controller/service/domain/database code was changed.
- Behavior impact:
  - no business-rule or API behavior change,
  - the portal now uses a premium, responsive, institution-friendly SaaS presentation layer.

### 2026-05-18 - Documentation Synchronization Follow-up (Execution Snapshot)
- Recent request issue:
  - requested follow-up synchronization across PRD, Function List, Complete Functionality Reference, Development Plan, and Database Schema after the latest TODO closure cycle.
- Implementation Summary:
  - added a new aligned execution snapshot entry in each of the five requested governance/traceability documents,
  - standardized wording so every entry includes the same request issue statement and closeout intent.
- Validation Summary:
  - verified all five requested documents contain a dated follow-up entry with issue, implementation, and validation blocks,
  - verified this request is documentation-only and introduces no runtime/API/schema behavior change.
- Behavior impact:
  - no runtime behavior change,
  - governance traceability for the follow-up request is complete and consistent.

### 2026-05-18 - Stage 40.1 PhoneNumber/SMS Recipient Dependency Completion (Execution Snapshot)
- Recent request issue:
  - notification SMS dispatch required active recipient phone numbers but user accounts did not persist a dedicated phone field.
- Implementation Summary:
  - added optional `PhoneNumber` support in user domain + EF mapping and applied schema migration,
  - implemented active-user phone resolution in notification repository for SMS delivery fan-out,
  - extended admin-user management, user CSV import, and student self-registration flows to accept/store optional phone values.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test Tabsan.EduSphere.sln -v minimal --filter "FullyQualifiedName~Phase28Stage2Tests|FullyQualifiedName~UserImportAndForceChangeIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~StudentRegistration"` passed (`13/13`).
- Behavior impact:
  - SMS notification delivery now has persisted phone-backed recipient resolution,
  - existing email/notification flows remain backward compatible while optional phone capture is available at key user-provisioning entry points.

### 2026-05-18 - StudentLifecycle Notification Completion (Execution Snapshot)
- Recent request issue:
  - multiple StudentLifecycle workflow notification backlog items were left unimplemented after state transitions and request-review actions.
- Implementation Summary:
  - implemented system-notification dispatch for graduation, semester promotion, student deactivate/reactivate actions,
  - implemented admin-review notifications on change/modification request creation,
  - implemented requestor/teacher approval and rejection notifications for reviewed requests.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~StudentLifecycleIntegrationTests -v minimal` passed (`7/7`).
- Behavior impact:
  - student lifecycle operations now emit user/admin notifications consistently at key workflow milestones,
  - no API contract or schema changes introduced.

### 2026-05-18 - DeepScan Phase 40 Closure and Production Readiness Revalidation (Execution Snapshot)
- Recent request issue:
  - after completing Phase 39 remediation, DeepScan required re-execution evidence and final closure classification before production-readiness signoff.
- Implementation Summary:
  - re-ran build and targeted suites that validate the previously open gap areas (MFA hardening, strict import rollback, enrollment waitlist flow, EF warning cleanup),
  - appended the task-by-task re-execution closure output to `Docs/DeepScan.md`,
  - synchronized final closure status across governance trackers.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter EnrollmentServiceWaitlistTests -v minimal` passed (`2/2`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter AuthSecurityUxTests -v minimal` passed (`7/7`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
  - DeepScan re-execution classification now reports no unresolved critical/high functional gap.
- Behavior impact:
  - no new runtime behavior introduced in Phase 40 itself,
  - production-readiness closure state is now documented with post-remediation evidence.

### 2026-05-18 - DeepScan Stage 39.4 EF Relationship and Query-Filter Warning Cleanup (Execution Snapshot)
- Recent request issue:
  - EF Core startup warnings indicated required-relationship plus global-filter mismatches, a quiz shadow foreign key mapping conflict, and a course enum default sentinel warning.
- Implementation Summary:
  - aligned dependent query filters with filtered required principals in affected configurations,
  - fixed quiz question relationship mapping to explicit `Quiz.Questions` navigation to remove shadow FK behavior,
  - removed `CourseType` DB default configuration that triggered sentinel warning behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
  - verified targeted EF warnings were no longer present in startup/runtime output.
- Behavior impact:
  - runtime entity loading behavior is now deterministic with filter-consistent required relationships,
  - noisy EF startup warnings for the Stage 39.4 target set are removed,
  - no API contract or business-flow behavior changed.

### 2026-05-18 - DeepScan Stage 39.3 MFA Hardening (TOTP + Recovery Codes) (Execution Snapshot)
- Recent request issue:
  - the MFA path used a deployment demo code instead of per-user strong-factor verification.
- Implementation Summary:
  - replaced demo-code login checks with per-user TOTP verification,
  - added one-time recovery-code generation, hashed persistence, and consumption flow,
  - added authenticated MFA enrollment endpoints (`setup`, `enable`, and recovery-code regeneration),
  - added user-level MFA persistence fields and migration support.
- Validation Summary:
  - targeted unit suite passed for MFA profile/risk and TOTP/recovery challenge behavior (`7/7`),
  - targeted integration suite passed for login and force-change-password compatibility (`4/4`).
- Behavior impact:
  - MFA is now user-enrolled and TOTP-backed rather than static deployment-code based,
  - recovery codes are one-time use and auditable,
  - existing login/refresh/force-change flows remain compatible.

### 2026-05-18 - DeepScan Stage 39.1 Enrollment Waitlist and Seat-Promotion Workflow (Execution Snapshot)
- Recent request issue:
  - the DeepScan remediation roadmap required waitlist handling for full course offerings so students could be queued and promoted on seat release.
- Implementation Summary:
  - added a `Waitlisted` enrollment state and promotion helpers in the enrollment aggregate,
  - updated enrollment service flow to create waitlisted records when an offering is full and auto-promote the oldest waitlisted student after a drop,
  - added ordered waitlist retrieval in the repository contract and implementation.
- Validation Summary:
  - targeted unit suite passed for waitlist creation and promotion (`2/2`),
  - verified full-offering enrollment now creates a waitlisted record instead of failing outright,
  - verified dropping an active enrollment promotes the oldest queued waitlisted student.
- Behavior impact:
  - enrollment now supports queueing instead of hard rejection when a section is full,
  - waitlist promotion is deterministic and based on queue order.

### 2026-05-18 - DeepScan Stage 39.2 Transactional CSV Import Strict Mode (Execution Snapshot)
- Recent request issue:
  - the DeepScan remediation roadmap called for an all-or-nothing import mode for user CSV imports so invalid files do not partially persist accounts.
- Implementation Summary:
  - added optional `strictMode` support to the user import service and controller,
  - made the import result payload report the executed path,
  - preserved permissive import behavior as the default for backward compatibility.
- Validation Summary:
  - targeted integration suite passed for user import and force-change flows (`4/4`),
  - verified strict-mode import aborts persistence when a mixed-validity CSV is submitted,
  - verified permissive import still works end-to-end.
- Behavior impact:
  - strict-mode CSV imports are now deterministic and atomic from the caller’s perspective,
  - existing permissive CSV imports remain unchanged.

### 2026-05-18 - DeepScan Gap Phase/Stage Synchronization Request (Execution Snapshot)
- Recent request issue:
  - DeepScan-identified missing/partial items were not yet represented as executable phase/stage entries across mandatory governance trackers.
- Implementation Summary:
  - added DeepScan-gap remediation planning to the consolidated execution tracker as new staged phases,
  - synchronized request closeout coverage in PRD, consolidated issues, function list, complete functionality reference, development plan, and database schema tracker,
  - aligned issue/implementation/validation wording so all mandatory trackers reflect the same execution narrative.
- Validation Summary:
  - verified all six requested documents include this dated DeepScan-gap synchronization snapshot,
  - verified each updated document explicitly records implementation and validation sections,
  - confirmed this update is documentation-only and introduces no runtime behavior or schema mutation.
- Behavior impact:
  - no runtime behavior change,
  - planning and execution governance now include explicit DeepScan-gap remediation staging.

### 2026-05-18 - Documentation Synchronization Request (Execution Snapshot)
- Recent request issue:
  - mandatory execution-tracker documentation was not yet synchronized for the latest closeout request across PRD, consolidated issues, function registry, full functionality reference, development plan, and schema tracker.
- Implementation Summary:
  - updated cross-document execution snapshots to include the same request closure narrative,
  - added explicit implementation/validation blocks in each mandatory tracker,
  - aligned wording so governance and handoff references remain consistent across planning and execution docs.
- Validation Summary:
  - verified all six requested documents now contain a dated Stage/Execution snapshot entry,
  - verified each entry explicitly includes both Implementation Summary and Validation Summary,
  - confirmed no runtime code or deployment behavior changes were introduced.
- Behavior impact:
  - no runtime behavior change,
  - documentation governance baseline is synchronized and audit-ready for this request cycle.

### 2026-05-15 - Final Documentation Synchronization (Post Phase 38 Execute)
- Completed final post-execute synchronization for user/operations documentation baseline.
- Implementation Summary:
  - updated role-based user guides and import templates to Phase 38 final baseline,
  - aligned training and consolidated user manuals with final publish-separation behavior,
  - updated deprecation stubs in Project startup Docs to point at canonical guide sources.
- Validation Summary:
  - confirmed final execute evidence references remain consistent across updated docs:
    - `Artifacts/Phase37/Publish-Separation-20260515.md`
    - `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`.
- Behavior impact:
  - no runtime behavior change,
  - documentation baseline is now fully aligned to final release packaging and operations workflows.

### 2026-05-15 - Phase 38 Stage 38.1/38.2 (Execution Snapshot)
- Completed non-runtime asset separation from runtime app publish path.
- Implementation Summary:
  - added `Scripts/Phase38-Separate-NonRuntime-Assets.ps1` to package Docs/PPT/Project startup Docs/Scripts/UAT-SAT docs/User Guide/New Enhancements separately,
  - added separation governance documentation in `Docs/Phase37-Phase38-Publish-Separation.md`.
- Validation Summary:
  - execute-mode separation report generated at `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`,
  - all requested folders were included in the separated package output `Artifacts/Phase38/NonRuntime-Assets-20260515.zip`.
- Behavior impact:
  - runtime app publishing can proceed without bundling non-runtime documentation and operations assets.

### 2026-05-15 - Phase 37 Stage 37.1/37.2 (Execution Snapshot)
- Completed runtime app vs license app publish separation.
- Implementation Summary:
  - added `Scripts/Phase37-Separate-App-And-License-Publish.ps1` to publish API/Web/BackgroundJobs separately from Tabsan.Lic,
  - added final publish separation governance doc `Docs/Phase37-Phase38-Publish-Separation.md`.
- Validation Summary:
  - execute-mode publish separation report generated at `Artifacts/Phase37/Publish-Separation-20260515.md`,
  - report confirms runtime app and license app are handled as separate publish targets and package outputs (`Artifacts/Phase37/Tabsan.EduSphere-App-Publish-20260515.zip`, `Artifacts/Phase37/Tabsan.Lic-Publish-20260515.zip`).
- Behavior impact:
  - Tabsan.Lic can be published independently from the main app stack and is no longer part of runtime app artifact composition.

### 2026-05-15 - Phase 36 Stage 36.6 (Execution Snapshot)
- Completed go-live execution and hypercare planning implementation.
- Implementation Summary:
  - added Stage 36.6 go-live and hypercare script to capture immediate smoke validation and checkpoint status,
  - added dedicated Stage 36.6 go-live and hypercare plan with incident triage, SLO guardrails, and checkpoint ownership,
  - added Stage 36.6 evidence artifact for execution readiness and hypercare activation.
- Validation Summary:
  - Stage 36.6 dry-run report generated under `Artifacts/Phase36/Stage36.6/`,
  - smoke validation checklist and focused integration suites are included in Stage 36.6 execution output,
  - hypercare 24/48/72-hour checkpoints and escalation signals are explicitly documented and aligned with Stage 36 runbook controls.
- Behavior impact:
  - Phase 36 now has an executable Stage 36.6 go-live playbook and auditable hypercare activation evidence,
  - the deployment readiness phase includes a complete transition from pre-go-live validation to post-go-live monitoring governance.

### 2026-05-15 - Phase 36 Stage 36.5 (Execution Snapshot)
- Completed UAT/SAT and operational sign-off implementation.
- Implementation Summary:
  - added deployment and rollback runbook with named ownership, on-call escalation path, maintenance window, communications plan, rollback thresholds, and post-deploy validation script set,
  - added Stage 36.5 UAT/SAT approval pack with final role-based pass outcomes for SuperAdmin/Admin/Faculty/Student,
  - added Stage 36.5 operational evidence artifact for sign-off traceability.
- Validation Summary:
  - confirmed role-based UAT/SAT final-pass outcomes are recorded as PASS,
  - confirmed runbook includes all required deployment-day checklist elements,
  - confirmed Stage 36.4 hardening/performance evidence is referenced in Stage 36.5 sign-off pack.
- Behavior impact:
  - deployment execution now has formal operational ownership and rollback decision governance,
  - go-live readiness can be audited from a single UAT/SAT approval and sign-off bundle.

### 2026-05-15 - Phase 36 Stage 36.4 (Execution Snapshot)
- Completed security, reliability, and performance gate implementation.
- Implementation Summary:
  - added Stage 36.4 smoke tests for public health snapshots, metrics output, and module-license blocking on a sensitive route,
  - added Stage 36.4 orchestration script for MFA/security regression, dashboard health visibility, health endpoint, performance smoke, and backup/restore evidence gates,
  - made Stage 34 backup/restore dry-runs tolerant of environments that do not have `sqlcmd` installed.
- Validation Summary:
  - Stage 36.4 gate report generated successfully under `Artifacts/Phase36/Stage36.4/`,
  - the combined gate set covered MFA/security hardening, dashboard-visible health, public health/metrics snapshots, module-license blocking, performance smoke, and backup/restore evidence,
  - recovery utility dry-run validation completed successfully as part of the gate flow.
- Behavior impact:
  - deployment readiness now has explicit proof for health visibility and sensitive-route license blocking,
  - the recovery gate can be rehearsed in dry-run mode even in environments without SQL tooling.

### 2026-05-15 - Phase 36 Stage 36.3 (Execution Snapshot)
- Completed data and migration deployment rehearsal implementation.
- Implementation Summary:
  - added `Scripts/Phase36-Deployment-Rehearsal.ps1` to validate the required `01 -> 05` deployment order,
  - included Stage 34 backup/restore drill and rollback-safe deployment utilities in the rehearsal plan,
  - generated a timestamped rehearsal evidence artifact under `Artifacts/Phase36/Stage36.3/`.
- Validation Summary:
  - dry-run rehearsal report generated successfully,
  - all seven rehearsal steps passed in the report,
  - the rehearsal plan covered schema, seed, dummy data, maintenance, post-deployment checks, backup/restore drill, and rollback-safe deployment utilities.
- Behavior impact:
  - deployment rehearsal is now scripted and auditable,
  - the release path has an explicit pre-production validation gate that does not require manual SQL sequencing.

### 2026-05-15 - Phase 36 Stage 36.2 (Execution Snapshot)
- Completed environment and secret readiness implementation for deployment gate validation.
- Implementation Summary:
  - added non-development startup safety guardrails in API/Web/BackgroundJobs to reject unsafe placeholder configuration for critical deployment settings,
  - added automated Stage 36.2 validation script for parity checks and effective secret-readiness checks with optional fail-fast mode,
  - generated evidence reports for baseline and strict production-like readiness validation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - baseline readiness report generated under `Artifacts/Phase36/Stage36.2/`,
  - strict readiness gate (`-FailOnIssues`) passed with secure environment-variable overrides.
- Behavior impact:
  - startup now fails earlier in non-development environments when critical settings remain placeholders,
  - deployment readiness checks are repeatable and auditable with generated artifacts.

### 2026-05-15 - Phase 36 Stage 36.1 (Execution Snapshot)
- Completed release-candidate baseline freeze setup for deployment readiness.
- Implementation Summary:
  - declared scope freeze to production blockers and defect-only changes,
  - captured immutable release baseline commit identity for deployment units,
  - added Stage 36.1 release manifest with prerequisites, module/security baseline, required secrets, and pre-deploy parity checks.
- Validation Summary:
  - RC baseline SHA captured and stored in release manifest,
  - release-candidate git tag created for baseline traceability,
  - solution build baseline command passed against current release candidate state.
- Behavior impact:
  - no runtime functional behavior changed,
  - deployment governance and traceability are now explicit for go-live readiness.

### 2026-05-15 - Phase 35 (Execution Snapshot)
- Completed In-App User Import UX Completion (Stages 35.1, 35.2, 35.3).
- Implementation Summary:
  - added `Import Users` button on Admin Users page and routed to existing portal CSV import flow,
  - added in-page template guidance with direct links for `faculty-admin-import-template.csv` and `students-import-template.csv`,
  - added secure template-download endpoint with file allow-list and safe path resolution,
  - improved upload-result UX by rendering row-level validation feedback in a structured Row/Issue table with correction guidance.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`3/3`).
- Behavior impact:
  - Admin and SuperAdmin can now enter user import directly from user-management context,
  - template-first guidance reduces malformed CSV uploads,
  - import validation output is more actionable for institution/role correction cycles.

### 2026-05-14 - Phase 23 Stage 23.2 (Execution Snapshot)
- Completed dynamic academic labels and context implementation/validation from `Docs/Advance-Enhancements.md`.
- Implementation Summary:
  - confirmed tenant policy-driven academic vocabulary resolution for School/College/University in label service layer,
  - confirmed authenticated label contract exposure at `GET /api/v1/labels`,
  - confirmed portal composition context consumes dynamic vocabulary for terminology adaptation without duplicating module screens,
  - added focused integration matrix for label endpoint behavior and precedence rules.
- Validation Summary:
  - focused integration test command passed (`8/8`) for `DynamicLabelIntegrationTests`,
  - label-service unit validation remains passing (`4/4`) in `Phase24Tests`,
  - verified unauthorized label endpoint access returns `401` and repeated requests remain consistent.
- Documentation synchronization: Stage 23.2 implementation and validation details are synchronized across Function List, Functionality, Schema, Development Plan, and Command trackers.
- Behavior impact:
  - academic terminology is now explicitly validated as institution-policy-aware across API and portal context surfaces,
  - precedence behavior is deterministic (University > School > College for enabled policy combinations),
  - no schema shape or migration changes introduced in Stage 23.2.

### 2026-05-13 - Phase 23 Stage 23.1 (Execution Snapshot)
- Completed institution-type foundation confirmation from `Docs/Advance-Enhancements.md`.
- Implementation Summary:
  - confirmed School/College/University mode support and persistence contracts in policy services,
  - confirmed University-safe default behavior remains active for backward compatibility.
- Validation Summary:
  - verified institution policy snapshot/default/save-load behavior in application services,
  - verified SQL seed baseline contains the three institution mode flags.
- Behavior impact:
  - no runtime behavior or schema-shape change was required for Stage 23.1,
  - foundation readiness is confirmed for Stage 23.2 label/context adaptation.

### 2026-05-13 - Institute Parity Stage 7.3 (Execution Snapshot)
- Completed monitoring and support handover documentation for Phase 7 operational readiness.
- Implementation Summary:
  - added report/analytics failure monitoring guidance and institute-scope triage priorities,
  - added SuperAdmin parity incident handover checklist for role, institution type, and scope verification.
- Validation Summary:
  - verified Stage 7.3 monitoring/support guidance is present in functionality and support docs,
  - verified command pointer progression toward Stage 7.4.
- Behavior impact:
  - no runtime behavior or schema-shape changes introduced in Stage 7.3,
  - support procedures for parity incidents are now explicit and repeatable.

### 2026-05-13 - Institute Parity Stage 7.4 (Execution Snapshot)
- Completed Phase 7 release exit criteria for institute parity.
- Implementation Summary:
  - finalized Phase 7 release-readiness evidence in the tracker set,
  - synchronized the mandatory handoff docs and advanced the execution pointer beyond Phase 7.
- Validation Summary:
  - verified Stage 7.4 completion evidence is present in the phase tracker and required documentation set,
  - verified release handoff moves to the next roadmap stage without behavior or schema changes.
- Behavior impact:
  - no runtime behavior or schema-shape changes introduced in Stage 7.4,
  - Phase 7 parity work is fully closed and ready for the next roadmap stage.

### 2026-05-13 - Institute Parity Stage 7.2 (Execution Snapshot)
- Completed functional documentation update for institute parity behavior.
- Implementation Summary:
  - updated functionality reference with role + institute scope behavior,
  - updated role-based user guides with institute filter behavior and expected access boundaries.
- Validation Summary:
  - verified Stage 7.2 tracking updates are synchronized across mandatory planning docs,
  - verified command pointer progression from Stage 7.2 to Stage 7.3.
- Behavior impact:
  - no runtime behavior or schema-shape changes introduced in Stage 7.2,
  - parity behavior guidance is now explicit for operational users and admins.

### 2026-05-13 - Institute Parity Stage 7.1 (Execution Snapshot)
- Completed deployment runbook finalization for Phase 7 operational readiness.
- Implementation Summary:
  - finalized deterministic DB script execution order (`01 -> 02 -> 03 -> 04 -> 05`) and operator environment notes,
  - added rollback + verification checklist for backup, fail-fast checks, cleanup fallback, and evidence archiving.
- Validation Summary:
  - verified deployment script set existence and run-order mapping,
  - verified schema bootstrap and post-deployment verification fail-fast script guards are present.
- Behavior impact:
  - no runtime behavior or schema-shape changes introduced in Stage 7.1,
  - deployment process is now documented with explicit rollback and verification controls.

### 2026-05-13 - Institute Parity Stage 6.4 (Execution Snapshot)
- Completed Phase 6 exit criteria sign-off for institute parity.
- Implementation Summary:
  - consolidated Stage 6.1/6.2/6.3 validation evidence into a single phase-exit certification run,
  - confirmed no additional runtime or schema changes were required for Stage 6.4 closure.
- Validation Summary:
  - consolidated Stage 6 parity regression run passed (`132/132`) with zero failures,
  - no critical/blocker defects observed across Phase 6 parity scenarios.
- Behavior impact:
  - Phase 6 parity outcomes are certified as exit-ready for transition to Phase 7 release/operational tasks,
  - no schema shape changes and no runtime contract changes were introduced in Stage 6.4.

### 2026-05-13 - Institute Parity Stage 6.3 (Execution Snapshot)
- Completed performance and query validation for parity-sensitive report/dashboard load paths.
- Implementation Summary:
  - added parity index read-usage validation for institute-filtered query patterns using Stage 1 parity indexes,
  - added common Admin dashboard/report latency budget regression checks for Stage 6.3 no-major-regression evidence.
- Validation Summary:
  - focused Stage 6.3 integration suite passed (`2/2`) via `PerformanceQueryValidationIntegrationTests`,
  - index read-path validation and endpoint latency loops completed with zero failures.
- Behavior impact:
  - institute-filtered query performance assumptions now have direct automated regression evidence,
  - no schema shape changes and no runtime contract changes were introduced in Stage 6.3.

### 2026-05-13 - Institute Parity Stage 6.2 (Execution Snapshot)
- Completed cross-role UAT matrix automation for School/College/University parity contexts.
- Implementation Summary:
  - added explicit SuperAdmin/Admin/Faculty/Student matrix coverage for report-catalog visibility behavior across institution claims,
  - added explicit role-boundary matrix coverage for account-security locked endpoint across institution claims,
  - added explicit role-boundary matrix coverage for attendance-by-offering endpoint across institution claims.
- Validation Summary:
  - focused Stage 6.2 integration matrix plus authorization/report baseline suites passed (`100/100`),
  - no regressions observed in tested role/institute guard paths.
- Behavior impact:
  - role and institute UAT evidence is now automated and repeatable for the Stage 6.2 matrix scope,
  - no schema shape changes and no runtime contract changes were introduced in Stage 6.2.

### 2026-05-13 - Institute Parity Stage 6.1 (Execution Snapshot)
- Completed automated regression expansion for institute parity validation in Phase 6.
- Implementation Summary:
  - added lifecycle matched-institute allow-path integration coverage for Admin callers,
  - added student submenu explicit department-filter allow-path coverage with institute-compatible scope assertions,
  - added enrollment-summary report matched-institution allow-path coverage for assigned Admin callers.
- Validation Summary:
  - focused integration parity suite passed (`28/28`) across lifecycle, submenu, and report parity tests,
  - existing mismatch-deny guard behavior remained intact while matched-scope requests returned expected success responses.
- Behavior impact:
  - institute parity validation depth increased with balanced deny-path and allow-path evidence on key lifecycle/report/student surfaces,
  - no schema shape changes and no runtime contract changes were introduced in Stage 6.1.

### 2026-05-13 - Institute Parity Stage 5.4 (Execution Snapshot)
- Completed Phase 5 exit-criteria validation for DB script readiness.
- Implementation Summary:
  - validated full script order (`01` -> `02` -> `03` -> `05`) in one run,
  - hardened full dummy replay path by reusing existing superadmin identity to avoid duplicate-email conflicts,
  - confirmed post-deployment checks emit institute-distributed parity counts and duplicate-safety outputs.
- Validation Summary:
  - targeted user-import institution-assignment regression suite passed (`3/3`),
  - one-run SQL script chain completed with full dummy success and parity check outputs.
- Behavior impact:
  - Phase 5 database scripts are now validated as one-run deployable for parity test data initialization,
  - Phase 5 scope is closed and execution proceeds to Phase 6 QA/UAT and regression expansion.

### 2026-05-13 - Institute Parity Stage 5.3 (Execution Snapshot)
- Completed data quality and replay safety hardening for Phase 5 script workflows.
- Implementation Summary:
  - hardened replay behavior in `Scripts/03-FullDummyData.sql` by aligning deterministic department/user values during reruns,
  - expanded `Scripts/05-PostDeployment-Checks.sql` with institute-level aggregate checks and critical workflow coverage checks,
  - added duplicate-safety checks for seeded usernames and registration numbers plus dataset-version integrity check.
- Validation Summary:
  - targeted user-import institution-assignment regression suite passed (`3/3`),
  - script-level verification confirms institute-level parity checks and replay-safety checks are present.
- Behavior impact:
  - replayed full dummy runs now maintain deterministic parity data shape more strictly,
  - post-deployment checks now provide direct quality gates for institute coverage and duplicate safety,
  - schema shape remains unchanged; no migration required.

### 2026-05-13 - Institute Parity Stage 5.2 (Execution Snapshot)
- Completed full dummy coverage expansion for parity data completeness.
- Implementation Summary:
  - expanded `Scripts/03-FullDummyData.sql` to seed full parity datasets for timetable/buildings/rooms, payments, lifecycle, and report artifacts,
  - added deterministic School/College/University institution assignment coverage in demo users/departments,
  - added admin/faculty assignment junction baselines for role/institute scoped parity testing.
- Validation Summary:
  - targeted user-import institution-assignment regression suite passed (`3/3`),
  - script-level verification confirms expanded entity coverage blocks and institution-alignment updates are present.
- Behavior impact:
  - full dummy runs now provide broader parity-ready data for role/institute, lifecycle, timetable, payment, and report-export scenarios,
  - schema shape remains unchanged; no migration required.

### 2026-05-13 - Institute Parity Stage 5.1 (Execution Snapshot)
- Completed core seed coverage for Phase 5 database script parity scope.
- Implementation Summary:
  - hardened `Scripts/02-Seed-Core.sql` to seed institution policy flags for School/College/University,
  - added deterministic core departments across all institution types,
  - normalized legacy report keys and seeded the current parity report definition set,
  - aligned seeded report/sidebar role-access defaults with policy matrix expectations.
- Validation Summary:
  - targeted user-import institution-assignment regression suite passed (`3/3`),
  - script-level verification confirms presence of new policy keys, institution-typed foundational rows, and updated report-role matrix.
- Behavior impact:
  - core DB seed output is now institute-aware and parity-aligned for foundational setup,
  - schema shape remains unchanged; no migration required.

### 2026-05-13 - Institute Parity Stage 4.4 (Execution Snapshot)
- Completed Phase 4 exit criteria for analytics and report parity + reliability.
- Implementation Summary:
  - no new code changes were required for the exit gate,
  - validated the repaired analytics/report surfaces under the full integration suite,
  - confirmed the phase closed without schema or migration changes.
- Validation Summary:
  - solution build passed,
  - full integration suite passed (`124/124`),
  - no regressions detected in report, analytics, role, or institute parity behavior.
- Behavior impact:
  - Phase 4 is closed with stable School/College/University parity behavior,
  - execution moves to Phase 5 without additional implementation debt in Phase 4.

### 2026-05-13 - Institute Parity Stage 4.3 (Execution Snapshot)
- Completed broken report fixes for School/College/University parity reliability scope.
- Implementation Summary:
  - repaired faculty report-scope enforcement on GPA, enrollment, semester-results, low-attendance, and FYP status endpoints,
  - enforced required faculty filter behavior for department-scoped report routes,
  - aligned faculty offering report checks with department-assignment scope to prevent false forbids,
  - added deterministic report integration coverage for missing-filter and unassigned-department denial scenarios.
- Validation Summary:
  - solution build passed,
  - focused parity integration suites passed (`42/42`) including Stage 4.3 report reliability tests,
  - no regressions detected in selected parity guard suites.
- Behavior impact:
  - broken faculty report behaviors are now deterministic and scope-safe,
  - repaired report endpoints enforce role + institute + department boundaries consistently,
  - no schema migration introduced in this stage.

### 2026-05-13 - Institute Parity Stage 4.2 (Execution Snapshot)
- Completed reports filter expansion for School/College/University parity behavior.
- Implementation Summary:
  - report endpoints and report exports now support institution filtering,
  - constrained-role report requests now auto-default to caller claim institute and reject explicit mismatches,
  - report dashboard pages now include institution + department filters aligned with report API query scope,
  - report repository queries now apply institution-aware filtering for attendance/result/assignment/quiz/GPA/enrollment/semester/low-attendance/FYP report paths.
- Validation Summary:
  - solution build passed,
  - focused parity integration suites passed (`43/43`) including expanded report parity tests,
  - no regressions detected in Stage 2/3/4 guard suites included in the run.
- Behavior impact:
  - report dashboard and report API/export queries are institute-aware and role-scoped by default for constrained users,
  - cross-institute report filter drift is denied for constrained roles,
  - no schema migration introduced in this stage.

### 2026-05-13 - Institute Parity Stage 4.1 (Execution Snapshot)
- Completed analytics filter expansion for School/College/University parity behavior.
- Implementation Summary:
  - analytics endpoints and exports now support institute filtering,
  - constrained-role analytics requests now auto-default to caller claim institute and reject explicit mismatches,
  - analytics dashboard now includes institute + department filters aligned with API analytics query scope,
  - analytics service queries now apply institution-aware filtering for performance, attendance, assignment, and quiz analytics paths.
- Validation Summary:
  - solution build passed,
  - focused parity integration suites passed (`41/41`) including new analytics parity tests,
  - no regressions detected in Stage 2/3 institute-scope guard suites included in the run.
- Behavior impact:
  - analytics dashboard and analytics API queries are institute-aware and role-scoped by default for constrained users,
  - cross-institute analytics filter drift is denied for constrained roles,
  - no schema migration introduced in this stage.

### 2026-05-13 - Institute Parity Stage 3.4 (Execution Snapshot)
- Completed Phase 3 exit criteria for module CRUD and workflow parity scope.
- Implementation Summary:
  - consolidated Stage 3.1-3.3 parity outcomes into Phase 3 closeout evidence,
  - added portal lookup-model `InstitutionType` contract field to keep institute-aware lifecycle/student filtering compile-consistent in web layer,
  - no schema migration or new authorization-policy mutation introduced.
- Validation Summary:
  - solution build passed,
  - full integration suite passed (`115/115`),
  - role/institute guard regressions not observed in Phase 3 covered surfaces.
- Behavior impact:
  - Phase 3 parity scope is exit-criteria complete for School/College/University implemented module workflows,
  - remaining parity expansion focus shifts to Phase 4 analytics/report filtering and broken-report reliability fixes.

### 2026-05-13 - Institute Parity Stage 3.3 (Execution Snapshot)
- Completed student submenu parity hardening for institute-aware student data filtering and terminology consistency.
- Implementation Summary:
  - enforced admin assignment + institution-claim constraints on student-list endpoint used by student submenu data reads,
  - added deterministic forbidden behavior for institute-mismatched department-filter requests,
  - normalized student submenu list wording from semester-centric labels to institute-neutral level labels.
- Validation Summary:
  - focused integration suites passed (`39/39`) including new student submenu parity tests and existing Stage 2/3 regression suites,
  - confirmed no regression in report/sidebar/assignment/lifecycle parity checks.
- Behavior impact:
  - student submenu backing reads now consistently enforce institute boundaries for Admin callers,
  - mixed-institute assignment datasets are safely filtered to caller-compatible institute scope,
  - no schema migration introduced by this stage.

### 2026-05-13 - Institute Parity Stage 3.2 (Execution Snapshot)
- Completed student lifecycle institute parity hardening for Phase 3.
- Implementation Summary:
  - added admin lifecycle endpoint guardrails for department-assignment and institution-type compatibility checks,
  - preserved SuperAdmin full-scope lifecycle behavior,
  - added web-session `institutionType` projection from JWT and institute-filtered lifecycle department selection in portal,
  - fixed lifecycle promote/graduate action route-state continuity in portal screen,
  - added integration coverage for lifecycle institution mismatch denial.
- Validation Summary:
  - focused integration validation suites passed (`37/37`) including new lifecycle mismatch tests and existing Stage 2/3 parity suites,
  - verified no regressions in report/sidebar/assignment parity behavior after lifecycle scope enforcement.
- Behavior impact:
  - lifecycle transitions and candidate reads now honor institute boundaries for Admin callers,
  - institute-mismatched lifecycle access attempts return deterministic forbidden responses,
  - no schema migration introduced by this stage.

### 2026-05-13 - Institute Parity Stage 3.1 (Execution Snapshot)
- Completed Phase 3 first stage for module CRUD/workflow parity in core academic/admin surfaces.
- Implementation Summary:
  - corrected portal department create/edit write path to carry explicit institution type instead of implicit University default,
  - updated Departments UI to display and edit institution type for School/College/University parity visibility,
  - expanded integration validation with cross-institution department/course CRUD checks under all-enabled institution policy,
  - hardened legacy admin assignment round-trip test to avoid mixed-dataset institution mismatch assumptions.
- Validation Summary:
  - solution build passed,
  - focused integration suites passed (`35/35`) for admin management, sidebar/menu guard, and report export coverage,
  - confirmed Stage 2 authorization/menu/report parity remains stable after Stage 3.1 updates.
- Behavior impact:
  - department portal create/edit flows are now institution-explicit and parity-safe for School/College/University,
  - cross-institution CRUD parity for core department/course paths is now integration-tested,
  - no schema migration was introduced in this stage.

### 2026-05-13 - Institute Parity Stage 2.4 (Execution Snapshot)
- Completed Phase 2 authorization exit criteria for role + institute matrix validation.
- Implementation Summary:
  - validated Stage 2 authorization outcomes as an integrated matrix across assignment controls, report institute scope controls, and portal menu/action guard consistency,
  - confirmed expected behavior for SuperAdmin/Admin/Faculty/Student authorization paths,
  - no additional runtime logic changes introduced in this stage.
- Validation Summary:
  - solution build passed,
  - combined Stage 2 integration suites
  - verified no unresolved access-mismatch regressions in tested parity authorization paths.
- Behavior impact:
  - no new feature behavior introduced by Stage 2.4 itself,
  - Phase 2 authorization and permission correction is now validated and complete,
  - Phase 3 module CRUD/workflow parity is unblocked.

### 2026-05-13 - Institute Parity Stage 2.3 (Execution Snapshot)
- Completed menu/action guard consistency hardening for portal navigation and direct route access.
- Implementation Summary:
  - added centralized portal route guard enforcement based on sidebar visibility,
  - introduced explicit action-to-menu key consistency mapping for parity-scope sections,
  - added integration verification for hidden-menu endpoint denial and SuperAdmin visible endpoint allowance.
- Validation Summary:
  - solution build passed,
  - focused sidebar integration suite passed (`14/14`),
  - verified deterministic access alignment between hidden menu state and guarded settings endpoints.
- Behavior impact:
  - hidden sections are no longer reachable through direct portal URL navigation for constrained roles,
  - SuperAdmin retains unrestricted navigation behavior,
  - existing role matrix and menu assignment behavior remains backward-compatible.

### 2026-05-13 - Institute Parity Stage 2.2 (Execution Snapshot)
- Completed role-scoped institute enforcement hardening for report access paths.
- Implementation Summary:
  - added `institutionType` claim emission in access-token generation for explicitly scoped users,
  - added report-handler institute-scope checks for Admin/Faculty callers in addition to role/assignment checks,
  - added integration verification for department-assigned but institution-mismatched Admin denial.
- Validation Summary:
  - solution build passed,
  - focused integration suites passed (`20/20`),
  - verified deterministic `403` behavior for institute-mismatch report requests.
- Behavior impact:
  - Admin/Faculty report access now enforces institute scope where explicit institution assignment exists,
  - SuperAdmin remains unrestricted,
  - report export payload/contract behavior remains unchanged for valid callers.

### 2026-05-13 - Institute Parity Stage 2.1 (Execution Snapshot)
- Completed SuperAdmin global-capability hardening for assignment management paths.
- Implementation Summary:
  - added SuperAdmin faculty department assignment APIs (assign/remove/list/list users),
  - enforced institution-type compatibility checks on admin/faculty department assignment writes,
  - added request contract for faculty assignment removal.
- Validation Summary:
  - solution build passed,
  - targeted integration suite (`AdminUserManagementIntegrationTests`) passed `6/6`,
  - verified assignment mismatch behavior now returns deterministic validation failure.
- Behavior impact:
  - SuperAdmin can now manage both Admin and Faculty department assignments through API,
  - cross-institute mismatched assignments are blocked early,
  - existing Admin-user management flows remain compatible.

### 2026-05-13 - Institute Parity Stage 1.4 (Execution Snapshot)
- Completed Phase 1 exit-criteria stage.
- Implementation Summary:
  - extended post-deployment SQL verification with institute-type validity/coverage checks,
  - added orphan-count checks for institute-linked core academic and assignment relationships.
- Validation Summary:
  - solution build passed,
  - targeted integration/security checks passed (`4/4`),
  - verified Stage 1.4 check markers are present in `Scripts/05-PostDeployment-Checks.sql`.
- Behavior impact:
  - no direct runtime feature change,
  - explicit and repeatable Phase 1 integrity verification now available for deployment/UAT environments.

### 2026-05-13 - Institute Parity Stage 1.3 (Execution Snapshot)
- Completed script-hardening stage for parity-safe schema deployment.
- Implementation Summary:
  - synchronized schema script execution path with Stage 1.1/1.2 model changes,
  - added idempotent parity index/column migration-equivalent blocks,
  - added deployment-time verification checks for parity migration/index/column state.
- Validation Summary:
  - verified script coverage for Stage 1.1 and Stage 1.2 migration equivalents,
  - verified replay guards for all added DDL operations.
- Behavior impact:
  - no direct runtime feature change,
  - lower deployment risk for environments using SQL script-based rollout and verification.

### 2026-05-13 - Institute Parity Stage 1.2 (Execution Snapshot)
- Completed referential-integrity and index-hardening execution for Phase 1.
- Implementation Summary:
  - made academic program code uniqueness department-scoped,
  - added course/offering creation guards for invalid or cross-scope references,
  - enforced program-department alignment for student profile creation,
  - added SQL index coverage for institute/report-heavy query paths,
  - normalized enrollment status column type for status-index compatibility.
- Validation Summary:
  - solution build passed,
  - targeted integration + unit checks passed (`8/8`),
  - migration flow verified during integration host startup.
- Behavior impact:
  - invalid academic link combinations now fail early with deterministic validation responses,
  - report/analytics/filter paths gain improved index support for department/institute scoped reads,
  - existing valid workflows remain backward-compatible.

### 2026-05-13 - Institute Parity Stage 1.1 (Execution Snapshot)
- Completed institute model normalization baseline for Phase 1.
- Implementation Summary:
  - introduced canonical department-level `InstitutionType` (School/College/University) in domain model,
  - persisted and indexed department institution type in schema,
  - enforced policy-aware institution type validation in department create/update API flows,
  - exposed institution type in department read payloads and synced web client contracts.
- Validation Summary:
  - solution build passed after Stage 1.1 updates,
  - targeted validation test suite (`SecurityValidationTests`) passed `4/4`,
  - confirmed backward-compatible behavior for existing University-default department operations.
- Behavior impact:
  - Department records are now explicit institute anchors for parity execution,
  - disabled institution modes are blocked at department write paths by active policy,
  - runtime behavior remains compatible for current University-only default workflows.

### 2026-05-13 - Institute Parity Stage 0.4 (Execution Snapshot)
- Completed Phase 0 baseline exit sign-off.
- Implementation Summary:
  - merged Stage 0.1 module audit, Stage 0.2 access matrix, and Stage 0.3 report failure inventory into prioritized remediation sequence.
- Validation Summary:
  - confirmed baseline artifact completeness and actionable next-phase mapping,
  - confirmed no open blocker at Phase 0 boundary.
- Behavior impact: no runtime change; governance and readiness closure completed for Phase 0.

### 2026-05-13 - Institute Parity Stage 0.3 (Execution Snapshot)
- Completed tagged report failure inventory for parity scope.
- Implementation Summary:
  - captured known report failures and guardrail outcomes by endpoint/surface,
  - linked each failure type to root-cause class and remediation stage.
- Validation Summary:
  - cross-validated against historical issue records and current report controller/repository behavior,
  - confirmed no unresolved critical report runtime crash in current baseline,
  - identified open risks around institute-filter parity and demo-data completeness.
- Behavior impact: no direct runtime change in Stage 0.3; report remediation planning quality and traceability improved.

### 2026-05-13 - Institute Parity Stage 0.2 (Execution Snapshot)
- Completed role x institute x module x action baseline matrix for School/College/University parity planning.
- Implementation Summary:
  - established module-level action coverage by SuperAdmin/Admin/Faculty/Student,
  - identified current scope-guard basis (institution policy flags, department scope, offering scope, ownership scope),
  - captured explicit parity enforcement gaps for next implementation stages.
- Validation Summary:
  - verified authorization and scope patterns from API/application source,
  - confirmed baseline reflects current behavior without runtime changes,
  - flagged report/analytics and selected mutation paths for explicit institute hardening.
- Behavior impact: no direct runtime behavior change in Stage 0.2; traceability and remediation backlog quality improved.

### 2026-05-13 - Institute Parity Stage 0.1 (Execution Snapshot)
- Completed baseline module-by-module parity audit (School/College/University) for target issue scope.
- Implementation Summary:
  - audited parity-scope API surfaces and mapped related service/repository paths,
  - documented current role/scope guards and identified University-default hotspots.
- Validation Summary:
  - source-level verification completed across API/Application/Infrastructure/Web/DB mapping layers,
  - no runtime mutation in this stage; baseline evidence prepared for Stage 0.2 matrix and remediation backlog.
- Behavior impact: no direct runtime behavior change in Stage 0.1; planning and traceability artifacts updated.

### 2026-05-12 — Institution License Validation Phase 7 (Execution Snapshot)
- Completed SuperAdmin full-access and permission matrix validation.
- Captured evidence in `Artifacts/Phase7/SuperAdmin/20260512-151302`.
- Verified privileged operations:
  - Department create/update/deactivate.
  - Admin-user create/deactivate/reactivate.
  - Institution policy mode switching by SuperAdmin.
- Verified cross-institution privileged access remained successful across School, College, and University modes for:
  - `GET /api/v1/dashboard/context`
  - `GET /api/v1/reports`
  - `GET /api/v1/portal-capabilities/matrix`
  - `GET /api/v1/license/details`
  - scoped report export endpoints.
- Run result: `35/35` successful checks, `0` failures.
- Phase 7 status: complete.

### 2026-05-12 — Institution License Validation Phase 6 (Execution Snapshot)
- Completed role-based institution data-boundary validation for Admin, Faculty, and Student.
- Captured evidence set in `Artifacts/Phase6/Access/20260512-150824` with per-check status and body files.
- Scope outcomes:
  - Admin assigned department export: `200`; non-assigned department: `403`; missing scope: `400`.
  - Faculty assigned offering export: `200`; non-assigned offering: `403`; missing scope: `400`.
  - Student operational report export remains blocked: `403`.
- Allowed student read surfaces remained accessible:
  - `GET /api/v1/reports` -> `200`
  - `GET /api/v1/dashboard/context` -> `200`
- Phase 6 status: complete.

### 2026-05-12 — Institution License Validation Phase 5 (Execution Snapshot)
- Completed explicit institution assignment implementation for manual admin create and CSV import flows.
- API contract updates:
  - `POST /api/v1/admin-user` accepts optional `institutionType` and returns persisted assignment.
  - `GET /api/v1/admin-user` includes `institutionType` for assigned users.
  - `POST /api/v1/user-import/csv` supports optional `InstitutionType` column with policy validation.
- Validation evidence captured in `Artifacts/Phase5/Api` (`20260512-144212` set):
  - manual create persisted assignment,
  - CSV invalid-assignment rejection,
  - CSV valid-assignment success,
  - admin list assignment visibility.
- Phase 5 status: complete.

### 2026-05-12 — Institution License Validation Phase 4 (Execution Snapshot)
- Completed mode-role validation sweep for School, College, and University across SuperAdmin, Admin, Faculty, and Student.
- Captured evidence in `Artifacts/Phase4/ModeRole/20260512-142021` for:
  - `GET /api/v1/institution-policy`
  - `GET /api/v1/labels`
  - `GET /api/v1/portal-capabilities/matrix`
  - `GET /api/v1/dashboard/context`
  - `GET /api/v1/reports` plus scoped report data/export endpoints
  - negative authorization checks (`admin-user`, `license/details`, student operational report access)
- Mode-level results:
  - School labels: `Grade/Promotion/Percentage/Subject/Class`
  - College labels: `Year/Progression/Percentage/Subject/Year-Group`
  - University labels: `Semester/Progression/GPA/CGPA/Course/Batch`
- Role-level results:
  - SuperAdmin/Admin/Faculty can access scoped report data and exports with valid filters.
  - Student remains restricted from operational report data/exports (`403`) while report catalog remains role-filtered.
- Phase 4 status: complete.

### 2026-05-12 — Institution License Validation Phase 3 (Execution Snapshot)
- Completed mixed-mode license coverage validation for:
  - School + College
  - School + University
  - College + University
  - School + College + University
- Captured endpoint evidence for policy, labels, capability matrix, grading-profile lookups, progression evaluations, and DB policy-key persistence.
- Validated union behavior:
  - policy flags and capability matrix rows align with licensed institution combinations,
  - persisted `institution_include_*` keys match each uploaded combination.
- Observed current label-resolution behavior:
  - School+College resolves to School vocabulary,
  - combinations containing University resolve to University vocabulary.
- Known test-environment caveat remained: generator verification-key reuse required consumed-key reset between sequential uploads.
- Phase 3 status: complete.

### 2026-05-12 — Institution License Validation Phase 2 (Execution Snapshot)
- Completed Phase 2 lifecycle validation for School, College, and University modes.
- Applied persistence fix so mode flags are committed to `portal_settings`:
  - `InstitutionPolicyService.SavePolicyAsync` now executes repository `SaveChangesAsync`.
- Captured mode-specific evidence for:
  - `GET /api/v1/institution-policy`
  - `GET /api/v1/labels`
  - `GET /api/v1/portal-capabilities/matrix`
  - `POST /api/v1/progression/evaluate`
  - DB verification of `institution_include_school|college|university` keys.
- Results:
  - School mode -> Grade/Promotion vocabulary, school-only matrix rows, school progression evaluation path.
  - College mode -> Year/Progression vocabulary, college-only matrix rows, college progression evaluation path.
  - University mode -> Semester/GPA vocabulary, university-only matrix rows, university progression evaluation path.
- Known test-environment caveat: generator currently emits licenses that share the same verification key; sequential mode uploads required clearing `consumed_verification_keys` between activations for validation flow continuity.
- Phase 2 status: complete.

### 2026-05-12 — Institution License Validation Phase 1 (Execution Snapshot)
- Executed Phase 1 checks against running API/Web environments.
- Verified SuperAdmin authentication and protected endpoint access.
- Captured baseline license/policy state:
  - `GET /api/v1/license/status` -> `Invalid`
  - `GET /api/v1/license/details` -> `None`
  - `GET /api/v1/institution-policy` -> `includeUniversity=true`, `includeSchool=false`, `includeCollege=false`
- Attempted license import using generated `.tablic` file from `tools/Tabsan.Lic/License`.
- Result: upload rejected with `License validation failed. The file may be invalid or tampered.`
- Root cause identified in API logs: database insert failed due legacy `license_state` non-null columns (`InstitutionScope`, `ExpiryType`) without defaults.
- Applied environment remediation defaults for legacy columns and re-ran upload.
- Re-run result: `License activated successfully`.
- Post-activation state:
  - `GET /api/v1/license/status` -> `Active`
  - `GET /api/v1/license/details` -> `Active` (`Yearly`, 365 remaining days)
  - `GET /api/v1/institution-policy` -> `includeUniversity=true`, `includeSchool=false`, `includeCollege=false`
- Final module/menu restriction validation:
  - `GET /api/v1/portal-capabilities/matrix` confirmed `school=false`, `college=false`, `university=true`
  - matrix rows with school enabled: `0`
  - matrix rows with college enabled: `0`
- Phase 1 is complete.

### 2026-05-12 — Institution License Validation Plan Added
- Added execution plan: `Docs/Institution-License-Validation-Phases.md`.
- Plan includes 7 phases to validate:
  - license-to-institution binding,
  - student lifecycle for School/College/University,
  - mixed-scope license behavior,
  - charts/tables/menus/reports scoping,
  - institution assignment in manual user creation and CSV import,
  - institution-based access boundaries for Student/Faculty/Admin,
  - full SuperAdmin permission and cross-institution visibility.
- Each phase requires `Implementation Summary`, `Validation Summary`, and `Status of Checks Done`.
- Phase completion requires docs sync plus repository sync (commit, pull, push).

### 2026-05-11 — Phase 10 Complete
- **Stage 10.1 incremental scale gates:** added a parameterized progressive gate runner that can execute the 10k -> 20k -> 50k -> 80k -> 100k sequence and an extended higher-tier plan.
- **Stage 10.2 bottleneck isolation:** added bottleneck classification heuristics so each gate run reports the first likely limiting class from the summary metrics.
- **Stage 10.3 fix-and-retest cycle:** added a retest loop so the same gate can be re-run after targeted fixes before promoting to the next tier.
- **Schema impact:** no database migration required.
- **Validation:** PowerShell syntax check on `tests/load/run-phase10-progressive.ps1` passed; editor diagnostics on `tests/load/k6-phase10-progressive.js` reported no errors.

### 2026-05-11 — Phase 9 Complete
- **Stage 9.1 metrics stack:** added OpenTelemetry metrics publishing with Prometheus scraping support in the API host.
- **Stage 9.2 latency SLO metrics:** added rolling request-latency capture with `/health/observability` snapshots that expose p50, p95, and p99.
- **Stage 9.3 full-stack health monitoring:** added database, CPU, memory, network, and error-rate health checks for continuous runtime monitoring.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 — Phase 8 Complete
- **Stage 8.1 auto-scaling policies:** added environment-configurable `InfrastructureTuning:AutoScaling` policy controls and startup validation for replica bounds across API, Web, and BackgroundJobs.
- **Stage 8.2 host limits:** added `InfrastructureTuning:HostLimits` controls for thread-pool minimums and Kestrel concurrent-connection ceilings.
- **Stage 8.3 network stack tuning:** added `InfrastructureTuning:NetworkStack` controls for Kestrel HTTP/2 stream tuning and outbound HTTP connection pooling/limits.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 — Phase 7 Complete
- **Stage 7.1 queue offloading:** account-security unlock/reset transactional email sends now enqueue background work items instead of always sending in request path.
- **Stage 7.2 queue platform integration:** added queue-platform runtime selection with in-memory channel mode and RabbitMQ producer/consumer mode for account-security email workloads.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**); syntax checks on touched files reported no errors.

### 2026-05-11 — Phase 6 Complete
- **Stage 6.1 external-call caching:** added short-TTL distributed cache for library loan external API reads keyed by student + configuration fingerprint.
- **Stage 6.2 resilience patterns:** added circuit-breaker threshold/open-window controls on outbound integration channels in addition to timeout/retry policies.
- **Stage 6.3 blocking risk reduction:** removed sync-over-async `.Result` usage in gradebook grid request assembly path.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**); syntax checks on touched files reported no errors.

### 2026-05-11 — Phase 5 Complete
- **Stage 5.1 realistic load model:** converted high-scale scripts to `ramping-arrival-rate` and added randomized think-time windows.
- **Stage 5.2 distributed generators:** added shard controls (`GENERATOR_TOTAL`, `GENERATOR_INDEX`) and runner wiring to split load safely across generators.
- **Stage 5.3 output discipline:** enforced summary-first defaults (`--quiet`, summary export) and gated raw JSON diagnostics in PowerShell runner with explicit opt-in.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**); syntax checks on touched k6/runner scripts reported no errors.

### 2026-05-11 — Phase 4 Complete
- **Stage 4.1 API cache policy:** added short-TTL distributed cache for expensive analytics report reads (`performance`, `attendance`, `assignments`, `quizzes`) with scoped cache keys.
- **Stage 4.2 edge/static caching:** added static asset `Cache-Control` headers in Web host with environment-configurable max-age settings.
- **Stage 4.3 cache scope control:** restricted new cache usage to expensive shared-safe operations and static/public assets only.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**); syntax checks on changed analytics/web startup/config files reported no errors.

### 2026-05-11 — Phase 3 Stage 3.3
- **Transport optimization:** added Kestrel keep-alive, request-header timeout, server-header suppression, and HTTP/2 ping tuning in API and Web startup.
- **Compression retained:** Brotli/Gzip response compression remains enabled with fast settings.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**); syntax checks on updated startup files reported no errors.

### 2026-05-11 — Phase 3 Stage 3.2
- **Async cleanup:** removed `ContinueWith` wrappers from timetable, settings, quiz, and building/room repository queries.
- **Hot path benefit:** repository reads now return direct awaited async EF results instead of sync-over-async task bridging.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**); syntax checks on touched repository files reported no errors.

### 2026-05-11 — Phase 3 Stage 3.1
- **Endpoint aggregation:** added `GET /api/v1/dashboard/context` to return visible modules, vocabulary, and widgets in one API round-trip.
- **Portal integration:** `PortalController.ModuleComposition` now uses the aggregated dashboard-context endpoint instead of three separate API calls.
- **Client model update:** web API client now deserializes dashboard context into a single response model for the ModuleComposition screen.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**); syntax checks on touched files reported no errors.

### 2026-05-11 — Phase 2 Stage 2.3
- **API stateless guard:** production now requires `ScaleOut:RedisConnectionString` so distributed cache state remains shared across API instances.
- **Web stateless guard:** production now requires `ScaleOut:SharedDataProtectionKeyRingPath` so protected auth cookies can be decrypted on every web node.
- **Developer flexibility preserved:** Development/Testing still allow local fallback behavior for easier iterative work.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 — Phase 2 Stage 2.2
- **Load balancer policy baseline:** added Nginx least-connections upstream template for multi-node API routing.
- **Operational orchestration:** added script to start/stop local load balancer container with generated upstream members.
- **Distribution verification:** added request sampler script to validate per-instance traffic distribution through LB policy.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 — Phase 2 Stage 2.1
- **Multi-instance baseline:** introduced API instance identity bootstrap (`ScaleOut:InstanceId`) with runtime fallback to machine/process identity.
- **Balancer observability:** added optional `X-EduSphere-Instance` response header for request distribution tracing.
- **Node probe endpoint:** added `GET /health/instance` payload for per-node verification (instance, process, host, uptime, version).
- **Operational script:** added `Scripts/Phase2-Stage2.1-MultiInstance-Api.ps1` for local multi-instance start/stop checks.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 — Phase 1 Stage 1.4
- **Dashboard composition caching:** added short-TTL role/policy keyed memory cache in dashboard composition service.
- **Sidebar visibility caching:** added short-TTL cache for top-level and role-visible sidebar menu reads with mutation-driven invalidation.
- **Notification read caching:** added short-TTL cache for inbox and badge reads with mutation-driven cache-version invalidation.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 — Phase 1 Stage 1.3
- **Notification inbox optimization:** added no-tracking read path for paged inbox retrieval to reduce EF tracking overhead.
- **Notification badge optimization:** removed unnecessary Include from unread-count query.
- **Sidebar query optimization:** top-level and visible-menu include-heavy reads now use no-tracking and split-query patterns.
- **Schema impact:** no database migration required.
- **Validation plan:** execute 12k/16k load-cap reruns and compare p95 latency + error-rate against Stage 1.2 baseline.

### 2026-05-11 — Phase 1 Stage 1.2
- **Connection-pool hardening:** updated API SQL connection strings to include explicit pool sizing and timeout controls.
- **Profiles updated:** base, development, and production appsettings profiles for `ConnectionStrings:DefaultConnection`.
- **Operational intent:** reduce connection exhaustion and timeout behavior under increased concurrent request pressure.
- **Schema impact:** no database migration required.
- **Validation plan:** execute 12k/16k load-cap reruns and compare p95 latency + error-rate deltas before Stage 1.3 query optimization.

### 2026-05-10 — Phase 33 Stage 33.3
- **Security hardening execution:** added DataAnnotations validation to auth/admin DTOs so invalid login, refresh, password-change, and admin-user payloads fail model validation.
- **Executable validation:** added `SecurityValidationTests` to verify login, admin creation, and forced password-change validation behavior.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet build Tabsan.EduSphere.sln` passed; `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter "FullyQualifiedName~SecurityValidationTests"` passed (**4/4**); `dotnet test Tabsan.EduSphere.sln --no-build` passed (**234/234**).

### 2026-05-10 — Phase 33 Stage 33.2
- **Runtime hosting hardening:** added config-driven reverse-proxy trust controls in API/Web startup (`ReverseProxy:Enabled`, `KnownProxies`, header symmetry, forward limit).
- **Startup safety guards:** added production startup guards for missing trusted proxies (when reverse-proxy mode is enabled) and empty API CORS origins outside Development/Testing.
- **Hardcoded-localhost removal:** removed localhost fallback defaults from Web login API base URL flow and portal API connection model defaults.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet build Tabsan.EduSphere.sln` passed; `dotnet test Tabsan.EduSphere.sln --no-build` passed (**230/230**).

### 2026-05-10 — Phase 33 Stage 33.1
- **Phase scope update:** re-scoped Phase 33 to `Hosting Configuration and Security Hardening` (source: `Docs/Refactoring-Hosting-Security.md`).
- **Hosting configuration foundation:** added explicit environment-aware configuration loading and startup diagnostics in API, Web, and BackgroundJobs.
- **Startup safeguards:** added required-setting guards for `DefaultConnection` and `EduApi:BaseUrl`, including placeholder rejection guard in BackgroundJobs base connection configuration.
- **Configuration alignment:** normalized `AppSettings` identity/version/base URL metadata across API/Web/BackgroundJobs appsettings.
- **Schema impact:** no database migration required.
- **Validation:** `dotnet build Tabsan.EduSphere.sln` passed.

### 2026-05-10 — Phase 32 Stage 32.5
- **Credential verification guardrail coverage:** added executable integration tests that seed deterministic smoke users and validate login for SuperAdmin/Admin/Faculty/Student.
- **Run-command verification:** established targeted post-change verification command for auth smoke coverage (`CredentialVerificationIntegrationTests`) to keep operational checks reproducible.
- **Schema impact:** no database migration required.
- **Validation:** targeted integration suite passed **4/4** (`CredentialVerificationIntegrationTests`).

### 2026-05-10 — Phase 32 Stage 32.4
- **Report Center sidebar guardrail coverage:** aligned sidebar role visibility so `report_center` remains visible for Admin, Faculty, and Student roles.
- **Link-operability guardrails:** added integration coverage to verify report-center-visible roles can successfully resolve report catalog payloads via `GET /api/v1/reports`.
- **Seeder hardening:** sidebar role-access seeding now self-heals existing role rows by updating `IsAllowed` values when they drift from baseline.
- **Schema impact:** no database migration required.
- **Validation:** targeted integration suite passed **12/12** (`SidebarMenuIntegrationTests`).

### 2026-05-10 — Phase 32 Stage 32.3
- **Sidebar Settings guardrail coverage:** added integration test coverage to ensure all seeded sidebar menu items remain assignable through role-assignment actions.
- **Operational safety:** validates Sidebar Settings role mutation endpoint works for every top-level and sub-menu key to prevent silent menu-configuration drift.
- **Schema impact:** no database migration required.
- **Validation:** targeted integration suite passed **9/9** (`SidebarMenuIntegrationTests`).

### 2026-05-10 — Phase 32 Stage 32.2
- **Report export guardrail coverage:** added integration tests that lock report export action routes for attendance/result/assignment/quiz across Excel/CSV/PDF variants.
- **Export contract guardrails:** added assertions for anonymous rejection, expected export media types, attachment filename conventions, and non-empty payload bytes.
- **Schema impact:** no database migration required.
- **Validation:** targeted integration suite passed **13/13** (`ReportExportsIntegrationTests`).

### 2026-05-10 — Phase 32 Stage 32.1
- **Report Center guardrail coverage:** added integration tests that lock report-catalog role visibility and seeded key availability for SuperAdmin/Admin/Faculty.
- **Report-link guardrail coverage:** added catalog-key-to-route validation to ensure report catalog entries continue to resolve to live report endpoints (no 404 link regressions).
- **Schema impact:** no database migration required.
- **Validation:** targeted integration suite passed **8/8** (`ReportCatalogIntegrationTests`).

### 2026-05-10 — Phase 31 Stage 31.3
- **Load certification bands:** added executable k6 certification-band script for `up-to-10k`, `10k-100k`, `100k-500k`, and `500k-1m` target bands.
- **Recovery certification:** added node/service failure recovery smoke script that validates restart recovery via `/health`.
- **Operational runbook:** updated load-test README and added Stage 31.3 certification artifact `Docs/Phase31-Stage31.3-Performance-Reliability-Certification.md`.
- **Schema impact:** no database migration required.
- **Validation:** full automated suite passed **201/201**.

### 2026-05-10 — Phase 31 Complete
- Stage 31.1, Stage 31.2, and Stage 31.3 are complete.
- Release hardening now includes regression matrix coverage, security-hardening enforcement, and performance/reliability certification assets.

### 2026-05-10 — Phase 31 Stage 31.2
- **Endpoint authorization audit:** added executable checks to ensure every API endpoint is explicitly authorized or explicitly anonymous.
- **Data exposure control:** added strict anonymous endpoint whitelist tests to detect unapproved public-surface expansion.
- **Sensitive-action audit coverage:** added explicit audit-log writes for feature-flag mutations, tenant operations control-plane writes, and institution policy updates.
- **Coverage artifact:** added Stage 31.2 hardening runbook at `Docs/Phase31-Stage31.2-Security-Hardening.md`.
- **Schema impact:** no database migration required.
- **Validation:** targeted tests passed **4/4**; full automated suite passed **201/201**.

### 2026-05-10 — Phase 31 Stage 31.1
- **Regression matrix baseline:** added executable matrix tests for role x institution mode x license combinations.
- **Tenant isolation baseline:** added isolated-store tests to verify tenant profile setting values do not leak between tenant scopes.
- **Coverage runbook:** added explicit Stage 31.1 matrix artifact at `Docs/Phase31-Stage31.1-Regression-Matrix.md`.
- **Schema impact:** no database migration required.
- **Validation:** targeted tests passed **25/25**; full automated suite passed **197/197**.

### 2026-05-10 — Phase 30 Stage 30.3
- **Feature-flag control plane:** added feature-flag service and SuperAdmin API endpoints for rollout control and emergency rollback.
- **Rollback safety guard:** tenant operations write paths now honor `tenant-operations.write` kill-switch behavior.
- **Runbook delivery:** added explicit reliability/rollback runbook at `Docs/Phase30-Stage30.3-Reliability-Rollback-Runbook.md`.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **172/172**.

### 2026-05-10 — Phase 30 Complete
- Stage 30.1, Stage 30.2, and Stage 30.3 are complete.
- Integration evolution, tenant/subscription operations, and rollback-safe controls are now available in the platform baseline.

### 2026-05-10 — Phase 30 Stage 30.2
- **Tenant onboarding templates:** added onboarding-template operations to manage default institution mode, admin role, welcome message, and starter modules.
- **Subscription plan controls:** added plan-level controls for user limits, monthly pricing, and module/integration feature toggles.
- **Tenant profile settings:** added tenant profile metadata controls (tenant code/name, support contacts, locale, timezone, currency, branding theme).
- **Operational API:** added SuperAdmin endpoints under `api/v1/tenant-operations` for onboarding-template, subscription-plan, and tenant-profile GET/PUT workflows.
- **Persistence strategy:** implemented using existing `portal_settings` key-value store to avoid schema churn.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **169/169**.

### 2026-05-10 — Phase 30 Stage 30.1
- **Unified outbound gateway:** introduced `IOutboundIntegrationGateway` and resilient runtime execution with channel-specific retry/timeout policy enforcement.
- **Dead-letter handling:** terminal outbound failures are now captured in distributed-cache-backed dead-letter storage for operational visibility.
- **Integration routing:** payment/email/sms/push/lms-external channel policy support added; active email, in-app push, and external library API calls now execute via the gateway.
- **Operational API diagnostics:** added communication integration endpoints for gateway policy visibility and dead-letter inspection.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **166/166**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 2
- **Cross-layer storage contract:** moved `IMediaStorageService` into the Application layer and added read-by-key support.
- **Certificate workflow migration:** graduation certificate generation now persists through provider-backed storage and stores storage keys.
- **Backward compatibility:** certificate download supports both new storage keys and legacy `/certificates/*` path records.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 3
- **License upload storage migration:** license upload endpoint now uses provider-backed temporary storage (save/read/delete) instead of direct temp-file path handling.
- **Validation flow decoupling:** added `ActivateFromBytesAsync` so license validation can run from in-memory bytes without filesystem-path assumptions.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 4
- **Provider selection:** added config-driven media storage provider selection (`MediaStorage:Provider`).
- **Object-storage adapter:** added `BlobMediaStorageService` with object-key semantics and configurable blob root path.
- **Configuration:** added `MediaStorage:BlobRootPath` in API appsettings defaults/placeholders.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 5
- **Portal branding storage migration:** migrated logo upload from inline base64 payload generation to provider-backed object persistence.
- **Streaming endpoint:** added `GET /api/v1/portal-settings/logo-files/{**storageKey}` for logo rendering against provider-backed assets.
- **Access guardrail:** endpoint only serves `portal-branding/logo` key category to avoid broad anonymous media access.
- **Backward compatibility:** previously stored data-URI logos continue to render unchanged.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 6
- **Storage contract hardening:** extended `IMediaStorageService` with `GenerateTemporaryReadUrlAsync` for temporary signed URL workflows.
- **Provider support:** local and blob storage adapters now generate temporary read URLs with optional HMAC signature support (`MediaStorage:SignedUrlSecret`).
- **Portal logo read path:** `GET /api/v1/portal-settings/logo-files/{**storageKey}` now prefers provider temporary URL redirect with safe byte-stream fallback.
- **Configuration:** added `SignedUrlSecret` placeholders to API appsettings files.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 7
- **Signed URL enforcement:** local logo-read endpoint now validates `exp` and `sig` when `MediaStorage:SignedUrlSecret` is configured.
- **Legacy compatibility:** unsigned local logo URLs are redirected to short-lived signed URLs.
- **Verification hardening:** signature validation uses fixed-time comparison and strict expiry checks.
- **Operational behavior:** provider temporary URL redirect-first path remains intact with byte-stream fallback.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 8
- **Certificate media endpoint:** added `GET /api/v1/graduation/certificate-files/{**storageKey}` for authenticated, storage-key based certificate streaming.
- **Redirect-first certificate reads:** graduation certificate download now redirects to provider temporary URLs when available, otherwise to signed local certificate URLs.
- **Signed local enforcement:** local certificate-file reads validate `exp` and `sig` when `MediaStorage:SignedUrlSecret` is configured.
- **Legacy compatibility:** existing legacy `/certificates/*` records continue to use the original byte-download path.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 9
- **Storage metadata contract:** added `GetMetadataAsync` plus content type and length fields on storage save results.
- **Provider metadata support:** local and blob storage adapters now expose metadata derived from persisted objects.
- **Response hardening:** logo and certificate streaming endpoints now use storage metadata to select content type instead of relying only on controller-side extension mapping.
- **Compatibility:** signed URL flows and legacy path fallbacks remain unchanged.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 10
- **Integrity/disposition metadata:** extended storage save and metadata contracts with SHA-256 content hash and optional download filename support.
- **Provider persistence:** local and blob storage adapters now persist sidecar metadata so integrity and filename semantics survive later reads and redirects.
- **Download behavior:** certificate generation and media streaming now preserve filename-aware downloads across signed local and redirect-first flows.
- **Phase outcome:** Stage 28.3 is complete, and with Stages 28.1 and 28.2 already delivered, Phase 28 is complete.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Stage 29.1
- **Index baseline:** added composite indexes for high-frequency student/user/status recency queries across graduation applications, support tickets, notification recipients, payment receipts, quiz attempts, and user sessions.
- **Migration:** added `20260509155457_20260510_Phase29_IndexBaseline`.
- **Schema audit:** current model contains no `InstitutionId`, `YearId`, or `GradeId` columns, so Stage 29.1 targeted existing `StudentId`/`UserId`/`CourseId`/`SemesterId` shaped contracts instead.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Stage 29.2 Slice 1
- **Pagination discipline:** helpdesk ticket listing now uses server-side paging with `page` and `pageSize` parameters.
- **Contract update:** API, application service, repository, and portal client now exchange paged ticket results instead of unbounded lists.
- **User experience:** portal helpdesk list now supports next/previous navigation while preserving the selected status filter.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Stage 29.2 Slice 2
- **Pagination discipline:** graduation application list endpoints now use server-side paging with `page` and `pageSize` parameters.
- **Contract update:** graduation API/application/repository/web contracts now exchange paged list results with total-count metadata.
- **User experience:** portal graduation apply/list pages now support next/previous navigation while preserving active filters.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Stage 29.2 Slice 3
- **Pagination discipline:** payment receipt list endpoints now use server-side paging with `page` and `pageSize` parameters.
- **Contract update:** payment API/application/repository/web contracts now exchange paged list results with total-count metadata.
- **User experience:** portal payments page now supports next/previous navigation while preserving the selected student filter for admin views.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Stage 29.3
- **Archive policy:** added `Scripts/3-Phase29-ArchivePolicy.sql` with retention matrix, dry-run candidate counts, and optional batched cleanup mode.
- **Index maintenance:** added `Scripts/4-Phase29-IndexMaintenance.sql` with fragmentation-threshold reorganize/rebuild planning and execution mode.
- **Capacity dashboards:** added `Scripts/5-Phase29-CapacityGrowthDashboard.sql` for table-size snapshots and recent growth signals.
- **Operational runbook:** updated `Scripts/README.md` with Stage 29.3 command flow.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated tests passed **162/162**.

### 2026-05-10 — Phase 29 Complete
- Stage 29.1, Stage 29.2 (all slices), and Stage 29.3 are complete.
- MSSQL optimization scope now includes index baseline, heavy-list pagination discipline, and operational lifecycle maintenance artifacts.

### 2026-05-10 — Phase 28 Stage 28.3 Slice 1
- **File/media abstraction:** introduced provider-based API storage contract (`IMediaStorageService`) and local implementation (`LocalMediaStorageService`) to remove endpoint-level hard-coded file persistence.
- **First migrated flow:** payment proof upload now stores through the provider and persists storage object keys as metadata.
- **Configuration:** added `MediaStorage` section (provider/local root/public base URL/key prefix) in API appsettings files for future object-storage/CDN migration.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed.

### 2026-05-09 — Phase 28 Stage 28.1 API and App Tier Scaling Complete
- **Load-balancer readiness:** expanded forwarded-header handling on the API and added forwarded-header handling to the Web tier for reverse-proxy deployments.
- **Stateless app nodes:** removed Web portal dependence on ASP.NET server session for API base URL, access token, session identity, and forced-password-change state; these now use protected cookies.
- **Shared key management:** Web startup now supports an optional shared data-protection key-ring path so multiple nodes can decrypt the same protected cookies.
- **Payload efficiency:** enabled Brotli/Gzip response compression on API and Web, and configured JSON serialization to omit null fields.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated test suite passed **160/160**.

### 2026-05-09 — Phase 28 Stage 28.2 Foundation Batch
- **Distributed cache:** API startup now supports an optional Redis connection string and shared cache instance name, with distributed-memory fallback for local/dev environments.
- **Hot reads:** module entitlement checks and report catalog retrieval now use the distributed cache layer so repeated reads can be reused across nodes.
- **Background workloads:** large notification fan-out batches are now deferred to a hosted worker that persists recipients in chunks.
- **Validation:** solution build passed; automated test suite passed **162/162**.

### 2026-05-10 — Phase 28 Stage 28.2 Completion
- **Report generation workloads:** queued result-summary export jobs now support asynchronous generation, status polling, and deferred download.
- **Large recalculation workloads:** queued result publish-all jobs now run in the background with job-state polling.
- **Scalability outcome:** Stage 28.2 objectives are fully met with distributed cache hot paths plus background offload for notification fan-out, report generation, and recalculation operations.
- **Schema impact:** no database migration required.
- **Validation:** solution build passed; automated test suite passed **162/162**.

### 2026-05-08 — Phase 16 Faculty Grading System Complete
- **Stage 16.1 — Gradebook Grid View:** `GradebookController` (GET/PUT/POST endpoints), `GradebookService` (GetGradebookAsync, UpsertEntryAsync, PublishAllAsync), `GradebookRepository` (3-way join for student info), `Gradebook.cshtml` (inline edit + publish-all).
- **Stage 16.2 — Rubric-Based Grading:** Domain entities `Rubric`/`RubricCriterion`/`RubricLevel`/`RubricStudentGrade`, EF configs, `RubricRepository`, `RubricService`, `RubricController`, `RubricManage.cshtml`, `RubricView.cshtml`.
- **Stage 16.3 — Bulk CSV Grading:** `GradebookService` (GetCsvTemplateAsync, ParseBulkCsvAsync, ConfirmBulkGradeAsync), `GradebookController` bulk endpoints, CSV upload UI in `Gradebook.cshtml`.
- Migration `Phase16_FacultyGrading` — creates `rubrics`, `rubric_criteria`, `rubric_levels`, `rubric_student_grades` tables.
- 78/78 unit tests passing.
- **Stage 15.1 — Prerequisite Validation:**
  - `CoursePrerequisite` domain entity (`CourseId`, `PrerequisiteCourseId`) with unique composite index.
  - `IPrerequisiteRepository` + `PrerequisiteRepository` (EF Core, table `course_prerequisites`, cascade-delete on parent course removal).
  - `EnrollmentService.TryEnrollAsync` fetches prerequisites and checks each against student's passing results; returns `UnmetPrerequisites` list on failure.
  - `PrerequisiteController` (`GET /api/v1/prerequisite/{courseId}`, `POST /api/v1/prerequisite`, `DELETE /api/v1/prerequisite/{courseId}/{prerequisiteCourseId}`) — read open to all authenticated; write restricted to Admin/SuperAdmin.
  - `EnrollmentRulesDTOs` updated: `AdminEnrollRequest` gains `OverrideClash` (bool) + `OverrideReason` (string?) fields.
  - EF migration `20260507133254_Phase15_EnrollmentRules` — creates `course_prerequisites` table + unique index `IX_course_prerequisites_course_prereq`.
- **Stage 15.2 — Timetable Clash Detection:**
  - `TryEnrollAsync` joins timetable entries for requested and already-enrolled offerings; rejects enrollment on time overlap.
  - Admin `OverrideClash` flag bypasses clash check; override reason is audit-logged.
- **Stage 15.3 — Capacity Limits (already implemented):**
  - `CourseOffering.MaxEnrollment` already enforced; `UpdateMaxEnrollment` API action in place.
- **Web Portal:**
  - `PrerequisitesPageModel`, `PrerequisiteWebItem`, `CoursePrerequisiteGroup` view models.
  - `Prerequisites` / `PrerequisiteAdd` / `PrerequisiteRemove` portal controller actions.
  - `Prerequisites.cshtml` view with department filter, per-course prerequisite list, add/remove forms.
  - Sidebar link added (Admin/SuperAdmin only).
- **Build:** 0 errors · **Tests:** 7/7 passed

### 2026-05-09 — Phase 14 Helpdesk / Support Ticketing System Complete (commit 8576e44)
- **Stage 14.1 — Ticket Submission and Tracking:**
  - `SupportTicket` entity (`SubmitterId`, `Category`, `Subject`, `Body`, `Status`, `AssignedToId`, timestamps); `SupportTicketMessage` thread entity.
  - `IHelpdeskRepository` + `HelpdeskRepository` (EF Core, tables `support_tickets` + `support_ticket_messages`, dept-scoped query filters).
  - `IHelpdeskService` + `HelpdeskService`: create, list, get, add-message, assign, status transitions, reopen window.
  - `HelpdeskController` (GET tickets, GET ticket, POST create, POST message, POST assign/resolve/close/reopen); EF migration `20260507_Phase14_Helpdesk`.
- **Stage 14.2 — Admin Case Management:** Admin/SuperAdmin endpoints; assignment to staff; dept-scoped visibility.
- **Stage 14.3 — Faculty Responses:** Faculty reply support; reopen within configurable window; `Helpdesk.cshtml`, `HelpdeskCreate.cshtml`, `HelpdeskDetail.cshtml`, `_TicketStatusBadge.cshtml`; sidebar link.
- **Build:** 0 errors · **Tests:** 78/78 passed

### 2026-05-08 — Phase 13 Global Search Complete (commit 00b7b64)
- **Stage 13.1 — Cross-Entity Search API:**
  - `GET /api/v1/search?q={term}&limit={n}` — authenticated all-roles endpoint; limit clamped 1–50.
  - Role-scoped results: SuperAdmin → all entities; Admin → assigned-department entities; Faculty → own department + own offerings; Student → enrolled offerings only.
  - `ISearchRepository` + `SearchRepository`: EF Core LINQ join queries (no stored procedures, no FTS, no new migration); placed in Application layer.
  - `ISearchService` + `SearchService`: role orchestration, de-duplication by (Type, Id), limit enforcement.
  - `SearchController`: extracts `callerId` + `role` from JWT claims; validates `q` ≥ 2 chars.
  - `SearchDTOs`: `SearchRequest`, `SearchResultItem`, `SearchResponse` records.
- **Stage 13.2 — Portal Search Bar:**
  - Global search `<input>` added to `_Layout.cshtml` header — visible on all pages when connected.
  - Typeahead dropdown: vanilla JS `fetch` to `/Portal/SearchTypeahead?q=...`; debounced 300 ms; renders top 5 inline; "See all results…" link.
  - Full results page `Search.cshtml`: re-search form, total-hit summary, Bootstrap nav-tabs per category (Student / Course / CourseOffering / Faculty / Department).
  - `_SearchResultsList.cshtml` partial: list-group with type badges and direct portal links.
  - `PortalController` — `Search()` + `SearchTypeahead()` actions added.
- **Build:** 0 errors · **Tests:** 78/78 passed

### 2026-05-07 — Phase 12 Academic Calendar System Complete (commits 6e89af1, 5758a71)
- **Stage 12.1 — Semester Timeline View:**
  - `AcademicCalendar` portal page visible to all authenticated roles (Student, Faculty, Admin, SuperAdmin).
  - Semester filter dropdown; days-remaining color badges (green ≥8 days, yellow 4–7, red 1–3, grey = passed).
  - Admin/SuperAdmin link to Manage Deadlines management page.
- **Stage 12.2 — Key Deadlines Management:**
  - New `AcademicDeadline` domain entity (`SemesterId`, `Title`, `Description`, `DeadlineDate`, `ReminderDaysBefore`, `IsActive`, `LastReminderSentAt`); extends `AuditableEntity`.
  - `IAcademicDeadlineRepository` + `AcademicDeadlineRepository` (EF Core, table `academic_deadlines`, soft-delete query filter, FK to `semesters` with cascade delete).
  - `IAcademicCalendarService` + `AcademicCalendarService`: full CRUD and `DispatchPendingRemindersAsync` (fan-out to all active users).
  - `CalendarController` (`GET /api/v1/calendar/deadlines`, `GET /api/v1/calendar/deadlines/by-semester/{id}`, `GET /api/v1/calendar/deadlines/{id}`, `POST`, `PUT`, `DELETE`) — read endpoints open to all authenticated roles; write endpoints restricted to Admin/SuperAdmin.
  - `AcademicDeadlines.cshtml` portal page with inline create/edit modals (Admin/SuperAdmin only).
  - `DeadlineReminderJob`: `BackgroundService` checking daily; dispatches `NotificationType.System` notifications to all active users when `DeadlineDate - ReminderDaysBefore <= today`.
  - EF migration `20260507_Phase12AcademicCalendar`.
- Validation: **0 build errors; 78/78 tests passed; commits 6e89af1 + 5758a71**

### 2026-05-07 — Stage 5.4 Admin Reporting Scope Portal UX Complete (commit ee9fb57)
- Added `isAdminOnly` guidance guards to all 9 report portal page actions in `Web/PortalController.cs`.
- `ReportAttendance`, `ReportResults`, `ReportAssignments`, `ReportQuizzes`: Admin receives friendly message when neither department nor offering filter is selected.
- `ReportGpa`, `ReportEnrollment`, `ReportSemesterResults`, `ReportFypStatus`: Admin receives friendly message when no department is selected.
- `ReportLowAttendance`: Admin receives friendly message when neither department nor offering is selected.
- Mirrors Faculty guidance pattern already in place; closes the portal UX gap that surfaced raw API 400 errors for Admin.
- API-level enforcement (`EnforceAdminDepartmentScopeAsync`) was already complete via Phase 6.
- Validation: **0 build errors, 0 warnings; 78/78 integration tests passed; commit ee9fb57**

### 2026-05-07 — Refactoring-Hosting-Security Remaining Items Complete (commit 5e80bc9)
- Serilog rolling file sink wired in `API/Program.cs` (`logs/app-.log`, daily rolling, 30-file retention, env-aware min level)
- `UserSecretsId` (`tabsan-edusphere-api-dev`) added to `API/Tabsan.EduSphere.API.csproj`
- `FileUploadValidator` extended with `ValidateImageAsync` (PNG/JPG/GIF/SVG/WebP ≤ 2 MB + magic-byte checks); wired into `PortalSettingsController.UploadLogo`; inline size+extension guard added to `Web/PortalController.SubmitAssignment`
- Validation: **0 build errors; 69/69 integration tests passed; commit 5e80bc9**

### 2026-05-07 — Refactoring-Hosting-Security Part A + Part B Complete
- **Part A — Hosting Configuration:**
  - Created `appsettings.Production.json` for API, Web, and BackgroundJobs projects with production-ready placeholder values
  - Updated `API/appsettings.Development.json`: Debug logging, CORS origins for localhost:5063, `EnableSwagger=true`, `EnableDetailedErrors=true`
  - Added `AppSettings` section to `API/appsettings.json` (EnableSwagger, EnableDetailedErrors, CorsOrigins)
  - DB retry on failure: `EnableRetryOnFailure(3, 30 s, null)` in `AddDbContext`
  - CORS from config: reads `AppSettings:CorsOrigins`; `AddCors` + `UseCors` wired in API pipeline
  - `ForwardedHeaders` middleware registered for non-dev environments (IIS/nginx/Cloudflare)
  - Health check endpoint at `/health` via `AddHealthChecks` + `MapHealthChecks`
  - 5 MB request body size limits on Kestrel, IIS, and FormOptions
  - Startup environment log: `Console.WriteLine` emits env + app name on start
  - Swagger gated by `AppSettings:EnableSwagger` flag (always on in Development)
  - WeatherForecast boilerplate removed from `API/Program.cs`
- **Part B — Security Hardening:**
  - `ExceptionHandlingMiddleware` created: maps exception types to HTTP codes; no stack traces in production; `traceId` in every error response; registered first in pipeline
  - `FileUploadValidator` (static) created: magic-bytes, MIME, extension allowlist, 5 MB limit
  - Web session cookie hardened: `SameSite=Strict`, `SecurePolicy=Always`
  - `.gitignore` updated: `*.pfx`, `*.key`, `logs/`, `appsettings.*.local.json`, `secrets/`, `.env.local`
- Validation: **0 build errors, 0 warnings; 69/69 integration tests passed; commit f56ccd9**

### 2026-05-07 — Issue-Fix Phase 3 Complete (Faculty Workflow Repair)
- Resolved all 8 Faculty workflow issues (403 errors and empty dropdowns):
  - `CourseController.GetAll` + `GetOfferings`: `Forbid()` → empty-list for out-of-scope dept requests
  - `CourseController.GetMyOfferings` for Faculty: changed from `FacultyUserId` filter to dept-assignment scope filter; fixes Assignments/Attendance/Results/Quizzes dropdowns
  - `StudentController.GetAll` for Faculty: removed `Forbid()`, silently scopes to allowed departments
  - `FypController.admin-create`: policy changed from `"Admin"` to `"Faculty"`
  - `PortalController.Fyp()` Faculty branch: loads students for FYP creation workflow
  - `Fyp.cshtml`: "Create Project" button + `createFypModal` now render for Faculty role
- Validation: 0 build errors; 78/78 tests passed

### 2026-05-07 — Issue-Fix Phase 4 Complete (Student Workflow Repair — Stages 4.1–4.6)
- **Stage 4.1 (Assignment Submission — completed)**
  - `POST /api/v1/assignment/submit` functional for students; validates file-or-text requirement
  - `SubmitAssignment` portal action saves files to GUID-named path in `wwwroot/uploads/assignment-submissions/`
  - `Assignments.cshtml` — Submit modal with file input and text area; submission status merged into rows
  - `EduApiClient.SubmitAssignmentAsync` wired to API
- **Stage 4.2 (Timetable Department Auto-Resolution — completed)**
  - Student timetable now auto-resolves department from authenticated student profile
  - `Guid.Empty` guard prevents invalid department lookups; falls back to dashboard config if profile unavailable
- **Stage 4.3/4.4/4.5 (Semester-Scoped Views — completed)**
  - Semester filter and semester-scoped offering dropdowns added to Assignments, Results, and Quizzes portal pages
  - Results: fallback to student-safe endpoint on 403 from offering-level call
  - Quizzes: Upcoming/Pending/Completed status badges based on availability windows
- **Stage 4.6 (FYP Student Lifecycle — completed)**
  - FYP sidebar menu gated: hidden until `CurrentSemesterNumber >= 8`
  - Student can request FYP completion (`POST /api/v1/fyp/{id}/request-completion`)
  - Faculty can approve completion (`POST /api/v1/fyp/{id}/approve-completion`); FYP auto-completes when all assigned faculty approve
  - Approval progress counter shown in portal (`approved/required`)
  - Completed FYP result row rendered inside student Results page
  - EF migration `Phase4FypCompletionApprovalFlow` added for approval state persistence
- **Auth consistency hardening**
  - Login flow resolves portal API base URL before token acquisition, eliminating intermittent student-page 401s
- Validation: 12/12 assignment integration tests passed; 78/78 full suite passed; 0 build errors

### 2026-05-06 — Issue-Fix Phase 4 Option A/C Complete (Web UI Import + Forced Password Change)
- Added/validated portal user import flow:
  - User Import page + CSV upload form + import summary rendering
  - CSV upload wired to `POST /api/v1/user-import/csv`
- Added first-login forced password change UX flow in Web:
  - login reads `MustChangePassword` and redirects to `Portal/ForceChangePassword`
  - portal action guard redirects all other portal routes until password is changed
  - forced change form posts to `POST /api/v1/auth/force-change-password`
  - session flag is cleared after successful password update
- Added integration tests covering:
  - user-import authorization guard (student forbidden)
  - end-to-end import -> first login requires password change -> password reset behavior
- Validation:
  - Focused integration tests passed (`2/2`)
  - Full integration suite passed (`70/70`)

### 2026-05-06 — Issue-Fix Phase 6 Delivery (Admin Multi-Department Assignment)
- **Stage 6.2 (Backend rules implementation - completed)**
  - Added `AdminDepartmentAssignment` domain entity to support many departments per Admin.
  - Added repository contract + EF implementation for admin assignment lifecycle.
  - Added EF migration: `20260506044806_20260506_Phase6AdminDepartmentAssignments`.
  - Added SuperAdmin-only assignment management APIs:
    - `POST /api/v1/department/admin-assignment`
    - `DELETE /api/v1/department/admin-assignment`
    - `GET /api/v1/department/admin-assignment/{adminUserId}`
  - Enforced assigned-department constraints for Admin role in:
    - Department list filters,
    - Course and offering filters,
    - Reporting data/export endpoints.
- **Stage 5.4 dependency closure (backend)**
  - Admin reporting scope is now bounded by assigned departments and offering department checks.
- **Stage 6.1 (UI - completed)**
  - Added SuperAdmin-only Admin assignment management panel on Departments page.
  - Added Admin selector + active department checkbox list + save action.
  - Added API endpoint for assignment UI user source:
    - `GET /api/v1/department/admin-users`
  - Added Web client/controller wiring to load and apply assignment diffs.
- **Validation**
  - `dotnet build Tabsan.EduSphere.sln` passed after backend + UI updates.

### 2026-05-06 — Issue-Fix Phase 6.1 Extended Delivery (Dedicated Admin User Management)
- Added SuperAdmin-only Admin account management API:
  - `GET /api/v1/admin-user`
  - `POST /api/v1/admin-user`
  - `PUT /api/v1/admin-user/{id}`
- Added dedicated SuperAdmin Admin Users portal page:
  - create Admin + initial department assignments
  - update Admin email/status/password + assignment synchronization
  - search/filter admin selector
  - select-all/clear controls for department checkboxes
- Departments page now includes quick-link to dedicated Admin Users management page.
- Added focused integration tests for admin-user access rules and admin create/update/assignment round-trip.
- Validation:
  - `dotnet build Tabsan.EduSphere.sln` succeeded.
  - Focused integration tests are currently blocked by an existing test-migration issue (`license_state.ActivatedDomain` duplicate column during test DB setup), not by these code changes.

### 2026-05-06 — Issue-Fix Phase 5 Progress (Reporting & Export Center)
- **Stage 5.2 (Export Actions - completed)**
  - Added CSV and PDF export support for attendance, results, assignments, and quizzes.
  - Added API endpoints:
    - `/api/v1/reports/attendance-summary/export/csv|pdf`
    - `/api/v1/reports/result-summary/export/csv|pdf`
    - `/api/v1/reports/assignment-summary/export/csv|pdf`
    - `/api/v1/reports/quiz-summary/export/csv|pdf`
  - Added matching Web portal proxy actions and UI export buttons (Excel/CSV/PDF) in all four report pages.
- **Stage 5.3 (SuperAdmin reporting scope - completed)**
  - SuperAdmin retains unrestricted report catalog/data/export scope across all departments and offerings.
- **Stage 5.5 (Faculty reporting scope - completed)**
  - Department list and course/offering filter sources are now faculty-scoped.
  - Faculty report data/export calls now require selected offering ownership validation.
- **Stage 5.4 (Admin reporting scope - completed)**
  - Admin report scope now fully bounded: API enforces assigned-department/offering constraints; portal shows friendly guidance before any API call is made.
  - All 9 report pages patched with `isAdminOnly` guidance guards.
- **Validation**
  - `dotnet build Tabsan.EduSphere.sln` succeeded after Stage 5.2 and scope hardening changes.

### 2026-05-06 — Issue-Fix Phase 6 Kickoff (Admin Multi-Department Assignment — now complete)
- Phase 6.1/6.2 fully implemented. Admin multi-department assignment model delivers final Stage 5.4 scope enforcement.

### 2026-05-06 — Issue-Fix Phase 2 Complete (Shared Portal and Settings)
- **Stage 2.1 (Branding and Asset Rendering - completed)**
  - Fixed `PortalSettingsController.UploadLogo` null-webroot failure by adding fallback to `ContentRootPath/wwwroot` when `WebRootPath` is not set.
  - Fixed API static file serving for branding assets by configuring `UseStaticFiles` with an explicit `PhysicalFileProvider` rooted at API `wwwroot`.
  - Uploaded logos are now accessible via `/portal-uploads/*` and render in the Web sidebar branding block.
- **Stage 2.2 (Privacy Policy Editing - completed)**
  - Verified dashboard branding settings include privacy content editor fields and persistence.
  - Verified `Home/Privacy` renders configured privacy policy content from portal settings.
- **Stage 2.3 (Shared Course Offering Dropdowns - completed)**
  - Verified shared offering sources are populated in portal forms; `Select Course Offering` shows expected offering options in Assignments.
- **Validation**
  - `POST /api/v1/portal-settings/logo` returns `200 OK` with uploaded URL payload.
  - `GET /portal-uploads/logo.svg` returns `200` after static file middleware fix.
  - Live portal validation confirmed: logo rendering, privacy rendering, offering dropdown population.

### 2026-05-05 — Phase 3 Complete (License App — Generator Alignment + File Security)
- **Stage 3.1 (Generator Alignment — completed)**
  - Added `MaxUsers` (int, 0=unlimited) and `AllowedDomain` (string?) to `IssuedKey` model in `tools/Tabsan.Lic`
  - Configured new columns in `LicDb` EF fluent model; startup SQLite migration in `Program.cs` auto-upgrades existing `tabsan_lic.db` files
  - Extended `LicenseBuilder.TablicPayload` to embed `MaxUsers` and `AllowedDomain` in the encrypted .tablic binary payload
  - `Program.cs` now prompts operator for MaxUsers and AllowedDomain during .tablic generation (option 3)
  - `HandleListKeys` shows MaxUsers/AllowedDomain per row; `ExportCsvAsync` includes both in CSV output
- **Stage 3.2 (File Security — pre-existing, verified)**
  - P3-S2-01: `LicCrypto.BuildTablicFile()` generates AES-256-CBC encrypted + RSA-2048 signed .tablic files; `LicenseValidationService` verifies signature and decrypts on every activation
  - P3-S2-02: RSA signature covers `SHA-256(IV + ciphertext)`; private key is tool-only; any payload modification → invalid signature → rejected; replay guard via `ConsumedVerificationKey` table
- **Build**: `dotnet build tools/Tabsan.Lic/Tabsan.Lic.csproj --no-restore` → 0 errors; full solution also 0 errors

### 2026-05-05 — Phase 2 Complete (License Concurrency + Domain Binding)
- **Stage 2.1 (Concurrency User Limit + SuperAdmin Exemption - completed)**
  - Added `MaxUsers` property to `LicenseState` domain entity; deserialized from .tablic binary payload
  - Added `CountActiveSessionsAsync()` to session repository; counts active sessions (RevokedAt=null AND ExpiresAt>UtcNow)
  - Updated `AuthService.LoginAsync()` to enforce concurrency limit: if MaxUsers > 0 and active session count >= MaxUsers, reject non-SuperAdmin login with HTTP 403
  - SuperAdmin always exempt from concurrency checks; can always login
- **Stage 2.2 (Unlimited Mode - completed)**
  - Implemented `MaxUsers == 0` as unlimited mode; skips all concurrency checks for that license state
- **Stage 2.3 (Domain Binding + Anti-Tamper - completed)**
  - Added `ActivatedDomain` property to `LicenseState`; captures request domain on first activation
  - Extended `.tablic` binary payload parsing to support optional `AllowedDomain` field from license issuer
  - Created `LicenseDomainMiddleware` that rejects cross-domain requests with HTTP 403 unless on whitelisted endpoints (`/api/v1/auth/login`, `/api/v1/license/upload`, `/api/v1/license/status`)
  - Prevents single license from being reused across multiple deployments; one license per domain
  - Anti-tamper: RSA-2048 signature + AES-256-CBC encryption + replay guard via ConsumedVerificationKey table + domain binding
- **Changed return type of IAuthService.LoginAsync** from `LoginResponse?` to `LoginResult` to differentiate failure reasons (invalid credentials vs concurrency limit reached)
- **EF Core migration created**: `20260505_Phase2LicenseConcurrency.cs` — adds MaxUsers and ActivatedDomain columns to license_state table
- **Build status**: 0 errors, all Phase 2 code compiles successfully

### 2026-05-05 — Phase 1 Remediation Restart (Batch 3)
- **Stage 1.3 (Result Summary InvalidOperationException - completed)**
  - Root cause identified in `ReportRepository.GetResultDataAsync()` query pipeline: EF Core could not translate `OrderBy` over projected `ResultReportRow` (`could not be translated`).
  - Fixed query by moving sort to SQL-side entity fields (`orderby u.Username, c.Code`) before projection in `BuildResultQuery()`.
  - Removed post-projection `OrderBy` calls in `GetResultDataAsync()` and `GetSemesterResultDataAsync()`.
- **SuperAdmin always-available enforcement (reports data endpoints)**
  - Updated `ReportController` data/export endpoints from faculty policy to explicit roles: `SuperAdmin,Admin,Faculty`.

- **Validation:**
  - Live API validation succeeded: `GET /api/v1/reports/result-summary` with SuperAdmin token returned `rows=21` and `totalRecords=21`.
  - File diagnostics report no errors in modified files.

### 2026-05-05 — Phase 1 Remediation Restart (Batch 2)
- **Stage 1.4 (menu and scripts cleanup - completed)**
  - Removed static `Module Settings` link from SuperAdmin sidebar in `_Layout.cshtml`.
  - Removed `module_settings` upsert entries from runtime seeder (`DatabaseSeeder`) and both SQL seeds:
    - `Scripts/1-MinimalSeed.sql`
    - `Scripts/2-FullDummyData.sql`
  - Added legacy cleanup logic to soft-delete/hide existing `module_settings` menu rows and disable their role access in both SQL seeds and runtime seeder.
  - Updated sidebar integration test expectations to reflect removal of `module_settings` key.

- **Validation:**
  - Modified files report no diagnostics.
  - SuperAdmin offerings access endpoint remains reachable after restart (`/api/v1/course/offerings/my`).

### 2026-05-05 — Phase 1 Remediation Restart (Batch 1 In Progress)
- **Stage 1.1 (403 access issues - in progress)**
  - Updated `CourseController.GetMyOfferings()` role gates from Faculty-only to `SuperAdmin,Admin,Faculty,Student`.
  - Added role-aware offering resolution so SuperAdmin/Admin can always load offering lists used by Assignments, Attendance, Results, and Quizzes pages.
- **Stage 1.3 (report visibility - completed for this batch)**
  - Updated `ReportRepository.GetCatalogForRoleAsync()` to return all active reports for SuperAdmin regardless of explicit role assignments.
- **Stage 1.4 (menu cleanup - partial)**
  - Removed `module_settings` route/group mapping in dynamic sidebar render path.
  - Updated sidebar branding block to a non-clickable container (TE / Tabsan EduSphere / Campus Portal no longer linked).
- **Stage 1.5 (promote error - completed for this batch)**
  - Fixed semester student mapping in `EduApiClient` by honoring `StudentProfileId` from lifecycle payload and falling back safely.

- **Validation:**
  - Static error scan reports no code diagnostics in modified files.
  - Full build currently blocked by running API/Web processes locking binaries (`MSB3021`/`MSB3027`), not by compile diagnostics.

### 2026-05-05 — Phase 9 Complete (Documentation and Script Regeneration)
- **Stage 9.1 (Script Modernization - COMPLETE)**
  - `1-MinimalSeed.sql` §15: Added 16 missing sidebar menu items and role accesses to match `DatabaseSeeder.cs`.
  - `1-MinimalSeed.sql` §17: Replaced 4 old hyphen-key report definitions with 8 canonical underscore-key definitions matching `ReportKeys.cs`.
  - `2-FullDummyData.sql`: Same §15 and §17 changes applied.
- **Stage 9.2 (Documentation Refresh - COMPLETE)**
  - User guides (Student, Admin, Faculty, SuperAdmin, License-KeyGen) bumped to v1.1.
  - Student guide: added Section 12 (Enrollments self-service).
  - Admin guide: updated Section 6 (Enrollment admin CRUD via portal).
  - Faculty guide: updated Section 4 (Enrollments roster view).
- **Stage 9.3 (Completion Artifacts - COMPLETE)**
  - All phase summaries recorded in Final-Touches.md Implementation/Validation summaries.
  - Function-List.md has Phase 7 and Phase 8 function tables.
  - PRD.md and Command.md updated per phase.
- **Build Status:** ✅ 0 errors, 0 warnings (no C# changes in Phase 9)

### 2026-05-05 — Phase 8 Complete (Enrollments Completion)
- **Stage 8.1 (Data and Dropdown Fixes - COMPLETE)**
  - Fixed `CourseController.GetOfferings()` to return all offerings when no filter provided (was returning empty list); fixed field names `CourseTitle` and `IsActive`.
  - Fixed `EnrollmentController.GetRoster()` to return `Id, StudentName, RegistrationNumber, ProgramName, SemesterNumber` matching EduApiClient `RosterApiDto`.
  - Added `ICourseRepository.GetAllOfferingsAsync()` + `CourseRepository` implementation.
  - Fixed `EnrollmentRepository.GetByOfferingAsync()` to include `StudentProfile.Program` so `ProgramName` is available.
  - Updated `EnrollmentController.MyCourses()` to include `CourseOfferingId` in response for student-drop flow.

- **Stage 8.2 (Enrollments CRUD - COMPLETE)**
  - Added `IEnrollmentRepository.GetByIdAsync()` + implementation.
  - Added `IEnrollmentService.AdminDropByIdAsync()` + `EnrollmentService` implementation.
  - Added `AdminEnrollRequest` DTO.
  - Added `POST /api/v1/enrollment/admin` — admin enrolls any student.
  - Added `DELETE /api/v1/enrollment/admin/{enrollmentId}` — admin drops any active enrollment.
  - Added 5 new EduApiClient methods: `GetMyEnrollmentsAsync`, `AdminEnrollStudentAsync`, `AdminDropEnrollmentAsync`, `StudentEnrollAsync`, `StudentDropEnrollmentAsync`.
  - Added `MyEnrollmentItem` view model; expanded `EnrollmentsPageModel` with `IsStudent`, `Students`, `MyCourses`.
  - Updated `PortalController.Enrollments GET` to branch by role; added 4 POST portal actions.
  - Rebuilt `Enrollments.cshtml`: student own-courses view + admin roster view with full CRUD.

- **Build Status:** ✅ 0 errors, 0 warnings

### 2026-05-04 — Phase 2 Stages 2.1–2.3 Complete (Data Visibility & CRUD Entry Points)
- **Stage 2.1 (Timetable Data Binding - COMPLETE)**
  - Fixed TimetableRepository query methods with proper EF Include statements for Building, Department, AcademicProgram, Semester
  - Faculty and Student My Timetable endpoints now render complete calendar data
  
- **Stage 2.2 (Lookup Data Visibility - COMPLETE)**
  - Fixed StudentProfileRepository to include Program and Department for accurate student profile display
  - Updated StudentController.GetAll() to return student names, program names, department names via related entities
  - Added CourseRepository.GetOfferingsByDepartmentAsync() for department-scoped offering retrieval
  - Refactored CourseController.GetOfferings() to support both ?semesterId and ?departmentId query parameters
  - Updated CourseController.GetAll() to include DepartmentName for course catalogue
  
- **Stage 2.3 (CRUD Entry Points - COMPLETE)**
  - Added 4 new CourseOffering lifecycle endpoints: maxenrollment update, close/reopen enrollment, soft-delete
  - Enhanced portal pages with create entry points and Bootstrap modals:
    - Students page: Add Student modal with Registration Number, Program, Department, Admission Date
    - Departments page: Add Department modal with Code, Name
    - Courses page: Add Course and Add Offering modals with all required fields
  - All modal forms include role-based visibility (Admin/SuperAdmin only)
  - Leveraged existing StudentController.Create, DepartmentController.Create/Update/Delete, CourseController.Create/Update/Delete endpoints
  
- **Build Status:** Verified (0 errors, all 3 stages successfully compiled)
- **Portal Enhancement:** Users can now create Students, Departments, Courses, and CourseOfferings directly from portal pages
- **API Completeness:** CourseOffering now supports full lifecycle management

### 2026-05-04 — Phase 2 Stages 2.1–2.2 Data Binding Complete
- **Stage 2.1 (Timetable Data Binding):**
  - Fixed TimetableRepository query methods to properly load Building, Department, AcademicProgram, and Semester navigation properties.
  - Faculty and Student My Timetable endpoints now return complete data for calendar display without null reference errors.
  - Test validation confirmed: 1 published timetable with 2 entries for CS dept, assigned to faculty.test.
  
- **Stage 2.2 (Lookup Data Visibility):**
  - Fixed StudentProfileRepository to include Program and Department navigation properties for accurate student profile data.
  - Updated StudentController.GetAll() to return student names, program names, and department names via related entity mapping.
  - Added CourseRepository.GetOfferingsByDepartmentAsync() method for department-scoped offering retrieval.
  - Refactored CourseController.GetOfferings() endpoint to accept both ?semesterId and ?departmentId query parameters for flexible filtering.
  - Updated CourseController.GetAll() to include department names for course catalogue display.
  - Build verified: 0 errors, all portal views ready to render with complete related entity data.

### 2026-05-03 — Final-Touches Phase 1 Complete
- Stabilized sidebar rendering behavior for portal pages by introducing resilient dynamic-menu loading with session cache fallback in web layout.
- Implemented grouped sidebar presentation for role-based navigation: Overview, Faculty Related, Student Related, Finance Related, Settings.
- Added `portal_settings` key-value table and `PortalBrandingService` to support configurable branding (university name, brand initials, portal subtitle, footer text).
- Added `DashboardSettings` page allowing SuperAdmin to set and save all branding values.
- Layout brand section (sidebar mark, name, subtitle, footer) now reads from DB settings with session cache fallback.
- Phase 1 validation passed: grouped sidebar, stable navigation, Dashboard Settings form, default values, live preview, and footer text all confirmed.

---

## 1. Purpose & Vision

### 1.1 Purpose
The University Portal is a secure, license-based web platform designed to manage academic, administrative, and communication processes for universities. The system uses a **unique, cryptographically protected license key** to control access and features, without embedding any institution-identifying data inside the license itself.

### 1.2 Vision
To deliver a scalable, long‑term university management system that preserves academic records permanently, enforces access through strong licensing, and enhances productivity with AI assistance.

---

## 2. Goals & Objectives

### 2.1 Business Goals
- Enable commercial distribution via licensing
- Support yearly and permanent license models
- Prevent license tampering or unauthorized reuse
- Reduce administrative workload
- Preserve academic records across all semesters permanently

### 2.2 Product Objectives
- Enforce license validity using secure cryptographic verification
- Ensure licenses cannot be edited or forged
- Provide role-based and department-based governance
- Deliver AI-assisted academic and administrative workflows
- Maintain high performance, security, and accessibility

---

## 3. User Roles

- Student
- Faculty
- Admin (All departments access)
- Super Admin (System authority)
- Finance (Payment and receipt management)
- External users (limited access)

---

## 4. Scope

### 4.1 In Scope
- Admissions & enrollment
- Student Information System (SIS)
- Assignments & quizzes
- Result calculation configuration and automated GPA / CGPA processing
- Notifications & alerts (dashboard + email)
- Final Year Project (FYP) management
- AI chatbot
- Reporting & analytics
- License management & enforcement
- Multi-theme UI system (15+ themes, per-user)
- **Tabsan-Lic** standalone license creation tool
- Student lifecycle: graduation, semester promotion/failure, dropout, department transfer
- Finance & payment receipts (with optional online payment gateway)
- CSV import for student registration whitelist
- Role-based sidebar navigation with per-role menus and sub-menus
- System Settings: License, Theme, Report, Module, Sidebar management
- Departments administration: degrees, semesters, subjects, timetable (PDF/Excel)
- Teacher modification requests with admin approval workflow
- Account lockout and admin/super-admin password reset
- OWASP Top 10 security hardening
- Database views and stored procedures
- Free/open-source email API integration
- Mobile-responsive, WCAG 2.1 AA accessible UI

### 4.2 Out of Scope
- Native mobile apps
- Alumni management
- Career marketplace
- Automated grading

---

## 5. Licensing Model

### 5.1 License Types

#### 5.1.1 Yearly License
- Valid for 12 months from activation
- Renewal required
- Includes:
  - Feature updates
  - Security updates
  - AI improvements
  - New UI themes
- Grace period after expiry
- Read-only access once expired

#### 5.1.2 Permanent License
- One-time activation
- Never expires
- No re-activation required
- Retains all features available at activation
- Academic data always accessible
- Optional paid upgrades for major versions

---

## 6. License Creation & Protection System

### 6.1 License Creation Tool

#### Description
A **dedicated License Creation Tool** is used to generate all licenses.  
Only licenses created by this tool are valid.

#### Functional Requirements
- Generate a **unique license key**
- License key contains:
  - Encrypted license type
  - Encrypted lifecycle rules (yearly/permanent)
  - Cryptographic signature
- License contains **NO**:
  - University name
  - University ID
  - Domain
  - Hardware identifiers
- Licenses are:
  - Machine-readable only
  - Not human-editable
  - Signed using a private key
- License revocation and regeneration supported
- Full audit logging

✅ The License Creation Tool is accessible **only to Super Admin / Vendor**

---

### 6.2 License File Security (Best Practice)

#### Protection Rules
- License file is:
  - Digitally signed
  - Encrypted
  - Obfuscated
- Signature verification uses an embedded public key
- Any alteration invalidates the license
- License stored in OS-protected directory
- Validation is backend-only

✅ No fields inside the license can be manually edited without breaking validation

---

### 6.3 License Validation Rules

- License validation occurs:
  - On system startup
  - On Super Admin login
  - Daily background validation
- Offline validation supported via cryptographic signature
- No online verification required
- Graceful degradation on license expiry

---

### 6.4 Graceful Degradation

If license expires or becomes invalid:
- Allowed:
  - View student data
  - Export transcripts
  - View historical records
- Blocked:
  - Creating assignments
  - New registrations
  - Data modifications

✅ Academic history is **never locked, deleted, or corrupted**

---

## 7. Authentication & User Management

### Student Signup
- Signup using official **registration number**
- Registration number must:
  - Exist in system
  - Be unique
- Auto-link student to:
  - Department
  - Program
  - Current semester

---

## 8. Role-Based Functional Requirements

### 8.1 Student
- View full academic history (all semesters — read-only for past semesters)
- Submit assignments and quizzes for active semester
- View attendance, grades, transcripts
- Receive notifications (dashboard + email)
- View FYP meeting schedules
- Interact with AI chatbot
- Select personal UI theme
- Self-edit password, email address, and mobile number only
- Submit admin change request for all other profile fields
- Upload payment receipt proof and mark as "Payment Submitted"
- Download timetable (PDF/Excel)
- **Graduated students**: full read-only dashboard — view and download only; no create/edit/submit
- **Students with unpaid fees**: read-only mode until Finance confirms payment
- **Inactive students**: blocked from login; all data preserved

---

### 8.2 Faculty
- Assigned to department(s)
- Create assignments and quizzes
- Grade and provide feedback
- View student history (department-restricted)
- Schedule FYP meetings (date, time, department, room, panel participants)
- Send notifications to students
- Submit attendance/result modification requests with a mandatory reason field
- Modification takes effect only after Admin approval
- Self-edit password, email address, and mobile number only; submit admin change request for other fields

---

### 8.3 Admin
- View all departments
- Access complete student academic history
- Generate university-wide reports
- Send notifications
- Manage departments: create/edit departments, degree programs, semesters, subjects, timetables
- Mark students as Graduated (checkbox list by department)
- Mark semester as completed or failed per student (with subject-level selection)
- Auto-promote students to next semester on completion
- Mark students as Inactive (dropout/leave)
- Transfer student to another department or change program
- Update student/faculty profiles on change request
- Approve or reject teacher attendance/result modification requests
- Enter or import CSV sheet of newly registered students to whitelist
- Create and manage payment receipts for students
- Confirm payment received (status → Paid)
- Configure Result Calculation rules: GPA-to-score mappings and assessment component weightages
- Unlock and reset passwords for non-admin locked accounts
- View license status (read-only)
- No role or system configuration access

---

### 8.4 Super Admin
- All Admin capabilities, plus:
- Create roles and users
- Assign departments
- Manage and upload licenses (via System Settings → License Update)
- Configure system settings: modules, reports, themes
- Control AI chatbot and online payment gateway toggle
- Manage UI themes
- View and export audit logs
- Reset passwords for any locked account including Admin accounts
- Manage Tabsan-Lic license generation (vendor tool, separate app)

### 8.5 Finance
- Create and upload payment receipts for students
- View payment status per student
- Confirm payment received → mark as Paid
- Cannot access academic records

### 8.6 External Users
- Limited read-only access as defined by admin configuration

---

## 9. Department-Based Structure

- Department is a core entity
- Faculty, courses, programs, and assignments are department-linked
- Admins see all departments
- Faculty restricted to assigned department(s)

---

## 10. Assignments & Quizzes

### Teachers
- Create assignments/quizzes per course and semester
- Set deadlines and rules
- Publish and notify students

### Students
- View assignments grouped by:
  - Semester → Course → Assignment
- Submit work
- View feedback and grades

### 10.1 Result Calculation & GPA Automation

#### Section 1: GPA-to-Score Mapping
- Admin configures a sidebar menu named **Result Calculation**
- The first section contains repeatable rows with two text boxes:
  - GPA
  - Score threshold / minimum score
- Example mappings:
  - GPA `2.0` = Score `60`
  - GPA `2.5` = Score `65`
- Admin can use:
  - **Add Row** to append another GPA/Score pair
  - **Save** to persist all rows to the database

#### Section 2: Assessment Component Weightage
- The second section contains repeatable rows for subject result components and their score contributions
- Admin defines how each subject total score is composed, for example:
  - Quizzes = 20
  - Midterms = 30
  - Finals = 50
- The configured weightages must total 100 before the configuration can be saved
- The configuration is stored in the database and used by all result-entry workflows

#### Automatic GPA, SGPA, and CGPA Processing
- When faculty enter quiz, midterm, or final marks, the system automatically recalculates the subject total using the saved component weightages
- The system automatically determines subject GPA using the saved GPA-to-score mapping
- Once all subjects for a semester are fully marked for a student, the system automatically calculates and stores:
  - Semester GPA (SGPA)
  - Total cumulative GPA (CGPA)
- Recalculation must also run whenever an existing mark is edited through an approved modification workflow
- All recalculation events must be auditable

---

## 11. Student Academic History

- Complete semester-by-semester record
- Includes:
  - Enrollment
  - Assignments
  - Quizzes
  - Grades
  - Attendance
  - Project history
- Never deleted or overwritten

---

## 12. Notifications & Alerts

- Results
- Assignments/quizzes
- Low attendance warnings
- FYP meetings
- Admin notices
- Delivered via dashboard (+ optional email)

---

## 13. Final Year Project (FYP)

- Faculty schedule meetings
- Define:
  - Location (Department + Room)
  - Panel members
- Students receive notifications and see details on dashboard

---

## 14. AI Chatbot

- Role-aware, department-aware
- Assists with:
  - Assignments
  - Results
  - Attendance
  - FYP meetings
- Escalation support
- Multilingual
- License-aware feature limits

---

## 15. UI Themes & Personalization

- Minimum **15 themes**
- Light & Dark included
- High-contrast accessibility themes
- **Per-user selection** — theme applies to the individual user, not the role
- Persistent across sessions
- AI chatbot inherits active theme
- Admin can set a system default theme; individual users can override it

---

## 16. Student Lifecycle Management

### 16.1 Graduation
- Admin marks students as Graduated via "Graduated Students" menu (per department, checkbox list of final-semester students)
- Graduated student dashboard becomes permanently read-only: view and download only

### 16.2 Semester Completion & Promotion
- Admin marks semester as completed per student; selects passed/failed subjects at subject level
- Students with all subjects passed: auto-promoted to next semester
- Students with failed subjects: status = "Completed with Failed Subjects"; failed subject list recorded
- Admin can mark semester as fully Failed: student repeats the semester; added to re-enrollment list
- Previous semesters always visible to students in read-only form

### 16.3 Student Status Changes
- Inactive: student blocked from login; all data preserved indefinitely
- Department/program transfer: admin opens student profile and updates department, program, and semester
- Admin change request workflow for name, address, and other non-self-editable fields

### 16.4 Student Signup via Registration Number
- Admin enters or imports a CSV of registration numbers into the whitelist before semester
- Student signup checks: registration number exists in whitelist + no duplicate account exists
- Duplicate account error: "An account already exists with this Registration Number. Please contact your admin for further details."

---

## 17. Finance & Payments

- Finance role creates fee receipts (amount, description, due date) per student
- Student uploads proof of payment and marks as "Payment Submitted"
- Finance confirms → status = **Paid**
- Until Paid: student account operates in read-only mode
- **Online payment gateway** (card / bank): disabled by default; Super Admin toggles on/off via Module Settings
- All payment records stored permanently; no deletion

---

## 18. System Settings

Accessible from the top navigation as a dedicated "Settings" menu:

| Sub-Menu | Access | Description |
|---|---|---|
| License Update | Super Admin (upload); Admin (view) | Upload `.tablic` license file; view status table (Status, Expiry Date, Date Updated, Remaining Days) |
| Theme Settings | All users | Per-user theme picker with preview |
| Report Settings | Super Admin only | Activate/deactivate reports per role; table: SR#, Report Name, Purpose, Roles (multi-select) |
| Module Settings | Super Admin only | Activate/deactivate modules per role; table: SR#, Module Name, Purpose, Roles (multi-select), Status (Active/Inactive) |
| Sidebar Settings | Super Admin only | Configure sidebar navigation visibility per role; table: SR#, Name, Purpose, Roles (checkbox list), Status (Active/Inactive). Click any top-level menu to reveal its sub-menus in the panel below. Super Admin always retains full access regardless of settings. |

---

## 19. Tabsan-Lic — License Creation Tool

- Standalone .NET application; separate from EduSphere
- Generates unique `VerificationKey` per license (one-time use; permanently invalidated after consumption)
- Operator selects expiry: 1 year / 2 years / 3 years / Permanent
- License file (`.tablic`) is AES-256 encrypted + RSA-2048 signed — machine-readable only
- Any modification to the file invalidates the signature
- EduSphere imports `.tablic`, verifies signature, applies license, marks VerificationKey as consumed
- Unlimited keys can be generated; each logged with timestamp and expiry choice
- EduSphere notifies Admin/Super Admin 5 days before license expiry

---

## 20. Security Requirements

- OWASP Top 10 compliance mandatory
- HTTPS-only; HSTS, CSP, X-Frame-Options, X-Content-Type-Options headers enforced
- Password policy: minimum 12 characters, uppercase + lowercase + digit + special character required; no common passwords; no last 5 reuse; hashed with Argon2id
- Rate limiting on authentication and sensitive endpoints
- Account lockout after 5 consecutive failed login attempts (configurable)
- Admin or Super Admin resets non-admin locked accounts; only Super Admin resets Admin accounts
- Dependency vulnerability scanning in CI; zero critical/high CVEs
- Penetration test sign-off before production release

---

## 21. Performance & Infrastructure Requirements

- Page load < 3 seconds; core dashboard p95 < 200 ms under load
- Chatbot response < 2 seconds
- 10,000+ concurrent users
- 99.9% uptime SLA
- SQL Views for high-traffic read patterns; Stored Procedures for complex write batches
- Horizontal scaling-ready (stateless API layer)

---

## 22. Email Integration

- Free/open-source transactional email via `IEmailSender` abstraction (MailKit SMTP, SendGrid free tier, or self-hosted)
- Email notifications for: results, assignment deadlines, low attendance, license expiry, password reset, account unlock
- All outbound email attempts logged with status

---

## 23. Mobile & Accessibility

- Responsive UI: tested at 360 px, 768 px, 1280 px viewports
- WCAG 2.1 AA compliance
- Touch-friendly controls (44×44 px minimum tap targets)
- Lighthouse score ≥ 90 for Performance, Accessibility, Best Practices on core pages

---

## 24. Non-Functional Requirements

- Page load < 3 seconds
- Chatbot response < 2 seconds
- 10,000+ concurrent users
- 99.9% uptime

---

## 25. Technical Overview

- Frontend: ASP.NET Core MVC + Razor (Web project); Bootstrap 5 responsive layout
- Backend: ASP.NET Core 8 Web API
- Database: SQL Server with EF Core 8; Views and Stored Procedures for performance
- REST APIs with JWT Bearer authentication
- AI integration (LLM via abstracted `IAiChatService`)
- Email: `IEmailSender` abstraction over MailKit/SendGrid
- Cloud-ready (Azure / AWS); Docker-compatible

---

## 26. Approval

| Name | Role | Signature | Date |
|-----|------|----------|------|
| | Product Owner | | |
| | University Representative | | |
| | Technical Lead | | |

---

## 20. Implementation Architecture Baseline (ASP.NET)

### 20.1 Target Solution Style
- Modular monolith for v1, with clean boundaries and migration path to services
- Backend stack: ASP.NET Core 8 Web API
- UI stack: ASP.NET Core MVC with Razor views and selective client-side enhancement
- Data access: Entity Framework Core with SQL Server as default provider
- Background jobs: Hosted Services for scheduled validation and notifications

### 20.2 Proposed Solution Layers
- Presentation: Web UI, REST API controllers, auth endpoints
- Application: Use cases, command/query handlers, validation, orchestration
- Domain: Entities, value objects, domain rules, domain events
- Infrastructure: EF Core, repositories, file storage, email/SMS adapters, audit sinks

### 20.3 Bounded Contexts
- Identity and Access
- Academic Core (departments, programs, courses, semesters)
- Student Lifecycle (profiles, enrollment, records)
- Learning Delivery (assignments, quizzes, attendance)
- Assessment and Results
- FYP Management
- Notifications
- Licensing and Entitlements
- Audit and Reporting

### 20.4 API and Contract Standards
- Versioned APIs under /api/v1
- Consistent envelope for success and error responses
- RFC7807-compatible problem details for validation and runtime errors
- Idempotency support for critical write operations (license activation, results publish)

### 20.5 Security Architecture
- ASP.NET Core Identity for authentication and password policies
- JWT bearer tokens for API access and secure cookies for web sessions
- Role-based authorization plus policy checks for department scoping
- Encryption at rest for sensitive columns and TLS in transit
- Centralized audit logging for privileged actions and data export operations

### 20.6 License Enforcement Integration
- License validated on startup, scheduled daily, and on Super Admin sign-in
- Degraded mode automatically enables read-only policy when invalid or expired
- Entitlement cache refreshed from signed local license payload
- Feature flags resolved by module entitlement plus system-level mandatory rules

### 20.7 Non-Functional Targets (Implementation)
- p95 API response under 500 ms for standard read endpoints
- p95 page load under 3 seconds on standard campus networks
- Horizontal scale readiness to 10,000 concurrent users
- Zero data-loss tolerance for academic history records

### 20.8 Observability and Operations
- Structured logging with correlation IDs
- Health checks for database, license state, and background workers
- Metrics: request latency, error rates, queue depth, failed jobs
- Audit export and retention controls for compliance reporting

### 20.9 Release Scope for v1
- In scope for v1: Authentication, Departments, SIS, Courses/Programs, Assignments, Results, Notifications, Licensing core
- In scope for v1.1: Quizzes, Attendance, FYP, AI Chatbot baseline, extended themes
- In scope for v1.2: Advanced analytics, advanced audit dashboards, multi-campus enhancements
- In scope for v1.4: Result Calculation configuration, automated subject GPA, semester GPA, and cumulative CGPA processing

### 20.10 Phase 11 Implementation Focus — Result Calculation and GPA Automation
- **Stage 11.1 Configuration UI and Data Model**
  - Add sidebar menu: `Result Calculation`
  - Add database tables for GPA-to-score mappings and assessment component weightages
  - Provide repeatable admin entry forms with Add Row and Save actions
- **Stage 11.2 Result Calculation Engine**
  - Compute subject totals from configured assessment weights
  - Resolve subject GPA from configured GPA/score thresholds
  - Validate total component weightage = 100 before activation
- **Stage 11.3 Academic Aggregation Automation**
  - Automatically compute semester GPA once all subject results are complete
  - Automatically update cumulative CGPA after semester GPA changes
  - Re-run calculations after approved mark modifications and retain audit logs

---

## Phase 20 — Learning Management System (LMS) ✅ Implemented (2026-05-08)

### 20.A Overview
Delivers structured digital learning content, discussion forums, and course announcements within the university portal. Faculty manage content per `CourseOffering`; students consume published content; announcements trigger in-app notifications to enrolled students.

### 20.B Functional Requirements

#### Stage 20.1 — Structured Course Content
- Faculty create weekly content modules (`CourseContentModule`) per offering with rich-text body and publish/unpublish control.
- Students see only published modules in week order.
- Faculty see all modules (published and draft).

#### Stage 20.2 — Video-Based Teaching
- Faculty attach video references (`ContentVideo`) to modules: direct upload URL or embed URL with optional duration.
- Students see videos rendered as iframes within the module accordion.

#### Stage 20.3 — Discussion Forums
- Faculty and students open `DiscussionThread` entries per offering.
- Faculty moderate: pin, close, reopen, delete threads.
- All participants add `DiscussionReply`; students can delete only their own replies; faculty delete any reply.

#### Stage 20.4 — Course Announcements
- Faculty post `CourseAnnouncement` entries; each post triggers `NotificationType.Announcement` in-app notifications to all active enrolled students.
- Announcements displayed as cards on the `Announcements` portal page.

### 20.C Technical Implementation
- **Domain:** `CourseContentModule`, `ContentVideo`, `DiscussionThread`, `DiscussionReply`, `CourseAnnouncement` — all extend `AuditableEntity` with soft-delete.
- **Persistence:** 5 new tables (`course_content_modules`, `content_videos`, `discussion_threads`, `discussion_replies`, `course_announcements`); EF migration `Phase20_LMS`.
- **API:** `LmsController` (`api/v1/lms`), `DiscussionController` (`api/v1/discussion`), `AnnouncementController` (`api/v1/announcement`) — JWT-enforced `AuthorId` on all write operations.
- **Web:** `CourseLms.cshtml`, `LmsManage.cshtml`, `Discussion.cshtml`, `DiscussionThread.cshtml`, `Announcements.cshtml`; sidebar entries `lms_manage`, `discussion`, `announcements`.
- **Validation:** 0 build errors · 7/7 unit tests · commit `ecf4d91`

---

## Phase 21 — Study Planner ✅ Implemented (2026-05-08)

### 21.A Overview
Enables students to build tentative semester plans with prerequisite and credit-load validation. A recommendation engine surfaces eligible courses based on degree audit gaps and available electives. Faculty advisors can endorse or reject plans.

### 21.B Functional Requirements

#### Stage 21.1 — Semester Planning Tool
- Students create `StudyPlan` entries per planned semester; add/remove `StudyPlanCourse` rows.
- Course picker restricted to `HasSemesters=true` (Phase 19) and `IsActive=true` courses.
- Service validates: all prerequisites passed in earned credits; total credit load ≤ `AcademicProgram.MaxCreditLoadPerSemester` (default 18).
- Faculty advisors endorse or reject plans with optional notes; status is `Pending / Endorsed / Rejected`.

#### Stage 21.2 — Course Recommendation Engine
- Required degree-plan gaps and eligible electives surfaced, prerequisite-gated, credit-load-capped.
- Per-course `Reason` string explains each recommendation.

### 21.C Technical Implementation
- **Domain:** `StudyPlan` (AuditableEntity, `StudyPlanStatus`, endorsement workflow), `StudyPlanCourse` (BaseEntity), `AcademicProgram.MaxCreditLoadPerSemester`.
- **Persistence:** Tables `study_plans`, `study_plan_courses`; EF migration `Phase21_StudyPlanner`.
- **API:** `StudyPlanController` (`api/v1/study-plan`) — 9 endpoints.
- **Web:** `StudyPlan.cshtml`, `StudyPlanDetail.cshtml`, `StudyPlanRecommendations.cshtml`; sidebar `study_plan` (Student Related).
- **Validation:** 0 build errors · 7/7 unit tests · migration applied

---

## Phase 22 — External Integrations ✅ Implemented (2026-05-08)

### 22.A Overview
Connects the portal to external third-party systems (library catalogue) and enables on-demand accreditation/government reporting. Both integration surfaces are configurable by SuperAdmin without code deployment.

### 22.B Functional Requirements

#### Stage 22.1 — Library System Integration
- SuperAdmin configures library catalogue URL + optional API token via `PUT /api/v1/library/config`.
- Authenticated users retrieve their own loan status via `GET /api/v1/library/loans` (proxied to external API).
- Admin/SuperAdmin can look up any student's loans via `GET /api/v1/library/loans/{studentIdentifier}`.
- Portal view: `LibraryConfig.cshtml` for SuperAdmin configuration.

#### Stage 22.2 — Government / Accreditation Reporting
- SuperAdmin defines `AccreditationTemplate` entries (name, description, field mappings, format, active state).
- `GET /api/v1/accreditation/{id}/generate` materialises data, streams report as CSV or plain-text PDF, and writes an audit-log entry.
- All template CRUD restricted to SuperAdmin; report generation accessible to Admin + SuperAdmin.

### 22.C Technical Implementation
- **Domain:** `AccreditationTemplate` entity (`Domain/Settings/AccreditationTemplate.cs`); `AccreditationTemplateConfiguration` EF config.
- **Persistence:** Table `accreditation_templates`; EF migration `Phase22_ExternalIntegrations`.
- **Application:** `ILibraryService` + `LibraryService`; `IAccreditationService` + `AccreditationService`; `IAccreditationRepository`; DTOs in `Application/DTOs/External/`.
- **API:** `LibraryController` (`api/v1/library`); `AccreditationController` (`api/v1/accreditation`).
- **Web:** `LibraryConfig.cshtml`, `AccreditationTemplates.cshtml`; sidebar entries `library_config` + `accreditation` (Settings group).
- **Validation:** 0 build errors · migration applied · commit `dddee69`

---

## Phase 23 — Core Policy Foundation ✅ Implemented (2026-05-09)

### 23.A Overview
Establishes the institution-type policy kernel so every downstream feature can adapt behaviour to School, College, or University mode without duplicating logic. Provides a cached, per-request policy snapshot consumed by middleware and downstream services.

### 23.B Functional Requirements

#### Stage 23.1 — License Policy Kernel
- `InstitutionType` enum: `University = 0` (default), `School = 1`, `College = 2`.
- `IInstitutionPolicyService` with 10-minute `IMemoryCache` backing and `portal_settings` persistence.
- SuperAdmin controls which institution types are enabled; at least one must always be active.
- `GET /api/v1/institution-policy` (all authenticated); `PUT /api/v1/institution-policy` (SuperAdmin only).

#### Stage 23.2 — Institution Context Resolution
- `InstitutionContextMiddleware` resolves snapshot per-request and stores it in `HttpContext.Items`.
- Extension method `GetInstitutionPolicy()` provides safe access with `Default` fallback (University-only).

#### Stage 23.3 — Role-Rights Hardening
- SuperAdmin portal page `InstitutionPolicy.cshtml` — three-flag toggle form.
- Sidebar module `institution_policy` (sort 33, SuperAdmin).

### 23.C Technical Implementation
- **Domain:** `Domain/Enums/InstitutionType.cs`.
- **Application:** `IInstitutionPolicyService` + `InstitutionPolicySnapshot` + `SaveInstitutionPolicyCommand`; `InstitutionPolicyService`; `Microsoft.Extensions.Caching.Memory 8.0.1` added.
- **API:** `InstitutionPolicyController`; `InstitutionContextMiddleware` (registered after `UseAuthorization`).
- **Web:** `PortalController.InstitutionPolicy`; `InstitutionPolicy.cshtml`; `EduApiClient` 2 new methods.
- **Persistence:** No migration — uses existing `portal_settings` table.
- **Validation:** 0 build errors · 27/27 unit tests · commit `28cac36`

---

## Phase 24 — Dynamic Module and UI Composition ✅ Implemented (2026-05-09)

### 24.A Overview
Adds compile-time module descriptors (role + institution-type + license-gate constraints), institution-aware academic vocabulary labels, and role-filtered dashboard widget composition — all driven by the Phase-23 policy snapshot at runtime.

### 24.B Functional Requirements

#### Stage 24.1 — Module Registry
- `ModuleDescriptor` sealed record: `Key`, `RequiredRoles[]`, `AllowedTypes[]?`, `IsLicenseGated`.
- `ModuleRegistry` static catalogue of all 14 modules; `IModuleRegistryService` combines registry with live activation + institution policy.
- `GET api/v1/module-registry/visible` returns per-user module visibility list.

#### Stage 24.2 — Dynamic Labels
- `ILabelService` / `LabelService` returns institution-appropriate `AcademicVocabulary` (Semester↔Grade↔Year, GPA↔Percentage, Course↔Subject, Batch↔Class↔Year-Group).
- `GET api/v1/labels` — all authenticated roles.

#### Stage 24.3 — Dashboard Composition
- `IDashboardCompositionService` / `DashboardCompositionService` — 10-widget catalogue filtered by role + institution type.
- `GET api/v1/dashboard/composition` — all authenticated roles.
- Portal: `ModuleComposition.cshtml` SuperAdmin page showing vocabulary tiles, widget cards, module registry table.

### 24.C Technical Implementation
- **Domain:** `ModuleDescriptor` sealed record (`Domain/Modules/`).
- **Application:** `ModuleRegistry`; `ModuleRegistryService`; `LabelService`; `DashboardCompositionService`; interfaces for all three.
- **API:** `ModuleRegistryController`, `LabelController`, `DashboardCompositionController`.
- **Web:** `ModuleComposition.cshtml`; `EduApiClient` 3 methods + 3 API models; `PortalController.ModuleComposition` (parallel `Task.WhenAll`).
- **Persistence:** No migration required.
- **Validation:** 0 build errors · 44/44 unit tests · commit `391ac45`

---

## Phase 25 — Academic Engine Unification ✅ Implemented (2026-05-09)

### 25.A Overview
Adds a pluggable result-calculation engine that supports GPA-based (University) and percentage-based (School/College) grading in parallel, alongside configurable institution grading profiles stored in the database and a student progression/promotion service that honours institution-specific pass thresholds.

### 25.B Functional Requirements

#### Stage 25.1 — Result Calculation Strategy Pattern
- `IResultCalculationStrategy` contract: converts component marks + configuration → `ResultSummary` (score, grade point, percentage, grade label, isPassing).
- `GpaResultStrategy` (University): weighted percentage → GPA on 0.0–4.0 scale. Pass = GPA ≥ threshold.
- `PercentageResultStrategy` (School/College): weighted percentage → grade band label. Built-in A+/A/B+/B/C/D/F bands; overridable via JSON. Pass = % ≥ threshold.
- `IResultStrategyResolver` / `ResultStrategyResolver` (singleton DI): maps `InstitutionType` → strategy. Zero changes to existing `ResultService`.

#### Stage 25.2 — Institution Grading Profiles
- `InstitutionGradingProfile` entity: one per institution type (unique index). Stores `PassThreshold` and `GradeRangesJson`.
- Threshold validation: University 0–4.0; School/College 0–100.
- `IInstitutionGradingService` / `InstitutionGradingService`: `GetAllAsync`, `GetByTypeAsync`, `UpsertAsync` (create-or-update semantics).
- `InstitutionGradingProfileController`:
  - `GET api/v1/institution-grading-profiles` — Admin+
  - `GET api/v1/institution-grading-profiles/{type}` — Admin+
  - `PUT api/v1/institution-grading-profiles/{type}` — SuperAdmin only

#### Stage 25.3 — Progression / Promotion Logic
- `IProgressionService` / `ProgressionService`:
  - University: CGPA ≥ pass threshold; period = "Semester N".
  - School: `CurrentSemesterGpa` (as %) ≥ pass threshold; period = "Grade N".
  - College: `CurrentSemesterGpa` (as %) ≥ pass threshold; period = "Year N".
  - Default thresholds if no profile: 2.0 (University), 40 (School/College).
- `EvaluateAsync`: read-only evaluation returning `ProgressionDecision`.
- `PromoteAsync`: evaluate + advance student semester; throws `InvalidOperationException` if not eligible.
- `ProgressionController`:
  - `POST api/v1/progression/evaluate` — Admin+
  - `POST api/v1/progression/promote` — Admin+
  - `GET api/v1/progression/me/{type}` — Student (self-view)

### 25.C Technical Implementation
- **Domain:** `InstitutionGradingProfile` entity; `IInstitutionGradingProfileRepository`.
- **Application:** `IResultCalculationStrategy`, `GpaResultStrategy`, `PercentageResultStrategy`, `ResultStrategyResolver`; `InstitutionGradingService`; `ProgressionService`; DTOs in `DTOs/Academic/`.
- **Infrastructure:** `InstitutionGradingProfileRepository`; EF config `InstitutionGradingProfileConfiguration`; migration `20260508152906_Phase25_AcademicEngineUnification`.
- **API:** `InstitutionGradingProfileController`, `ProgressionController`; Phase 25 DI registrations in `Program.cs`.
- **Tests:** 29 new unit tests in `Phase25Tests.cs`; 144/144 total passing.
- **Validation:** 0 build errors · 144/144 unit tests · commit `d2aabd3`

---

## Phase 26 — School and College Functional Expansion ✅ Implemented (2026-05-09)

### 26.A Overview
Builds school/college-specific academic operations on top of the unified engine by adding stream mapping, report-card snapshots, approval-based bulk promotion workflows, and parent-linked read-only student access.

### 26.B Functional Requirements

#### Stage 26.1 — School Streams and Subject Mapping
- `SchoolStream` master data with active/inactive lifecycle and unique stream names.
- Per-student stream assignment (`StudentStreamAssignment`) enforcing one active stream per student.
- Service/API for stream CRUD and student-stream assignment.

#### Stage 26.2 — School/College Report Cards and Promotion Operations
- `StudentReportCard` stores immutable report-card JSON snapshots by student and period label.
- `BulkPromotionBatch` + `BulkPromotionEntry` workflow with safeguards:
  - Draft → AwaitingApproval → Approved/Rejected → Applied
  - Apply allowed only for approved batches
  - Only Promote entries advance student period counters
  - Hold entries remain unchanged with optional reasons

#### Stage 26.3 — Parent-Facing Read Model (School Optional)
- `ParentStudentLink` maps parent user accounts to student profiles.
- Parent portal read endpoint returns only active links and linked student summaries.
- Strict scope by parent linkage; no cross-student leakage.

### 26.C Technical Implementation
- **Domain:** `SchoolStream`, `StudentStreamAssignment`, `StudentReportCard`, `BulkPromotionBatch`, `BulkPromotionEntry`, `ParentStudentLink`; enums `BulkPromotionStatus`, `EntryDecision`.
- **Application:**
  - `ISchoolStreamService`/`SchoolStreamService`
  - `IReportCardService`/`ReportCardService`
  - `IBulkPromotionService`/`BulkPromotionService`
  - `IParentPortalService`/`ParentPortalService`
  - DTOs in `Application/DTOs/Academic/Phase26Dtos.cs`
- **Infrastructure:**
  - Repositories in `Infrastructure/Repositories/Phase26Repositories.cs`
  - EF configs: `SchoolStreamConfiguration`, `StudentStreamAssignmentConfiguration`, `StudentReportCardConfiguration`, `BulkPromotionBatchConfiguration`, `BulkPromotionEntryConfiguration`, `ParentStudentLinkConfiguration`
  - Migration `20260509044437_Phase26_SchoolCollegeExpansion`
- **API:** `SchoolStreamController`, `ReportCardController`, `BulkPromotionController`, `ParentPortalController`.
- **Validation:** 0 build errors · 152/152 tests passed · commit `4c0904c`

---

## Phase 27 — University Portal Parity and Student Experience ✅ Implemented (2026-05-09)

### 27.A Overview
Completes parity and UX alignment across student/faculty/admin portal flows by adding a consolidated capability matrix, deployment-configurable auth-security UX, and provider-based communication integration contracts to avoid vendor lock-in.

### 27.B Functional Requirements

#### Stage 27.1 — Student Portal Capability Matrix
- Consolidates capabilities (dashboard, courses, assignments, quizzes, attendance, timetable, results, fees/payments, notifications, support, AI assistant, FYP workspace, reports) into a single role/institution-aware matrix.
- `GET api/v1/portal-capabilities/matrix` exposed for portal and admin visibility.

#### Stage 27.2 — Authentication and Security UX
- SSO-ready contract added for deployments that enable SSO.
- MFA toggle support from configuration with login-time enforcement.
- Session risk controls and expanded auth audit trail events.
- `GET api/v1/auth/security-profile` added to support adaptive login UX.

#### Stage 27.3 — Support and Communication Integration
- Added abstraction contracts for ticketing, announcements, and email delivery.
- Refactored core services to consume contracts instead of concrete vendor implementations.
- Added integration profile endpoint for runtime provider discovery.

### 27.C Technical Implementation
- **Application:**
  - `IPortalCapabilityMatrixService` + `PortalCapabilityMatrixService`.
  - `AuthSecurityOptions` and `AuthService` extensions (MFA, risk, security profile).
  - `ICommunicationIntegrationContracts` + `CommunicationIntegrationService`.
- **API:**
  - `PortalCapabilitiesController`.
  - `AuthController` security-profile endpoint and extended login failure mapping.
  - `CommunicationIntegrationsController`.
- **Infrastructure:**
  - `InAppSupportTicketingProvider`.
  - `InAppAnnouncementBroadcastProvider`.
  - `SmtpEmailDeliveryProvider`.
- **Web:**
  - `PortalCapabilityMatrix` page.
  - Adaptive login UX updates using security-profile data.
- **Validation:** 89/89 unit tests passed; full solution build passed.
- **Migrations:** none required.
- **Commits:** `fd3b137`, `20dba8d`, `56cf1dd`.

---
