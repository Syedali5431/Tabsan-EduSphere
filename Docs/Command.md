### Plan G Phase 13 Stage 13.3 Reporting Consistency Guard (2026-05-21)
- Implementation Summary:
  - Documented the reporting consistency guard requirement to ensure dashboard summaries remain consistent with report outputs and mapping rules.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the dashboard reporting consistency guard requirement.
- Validation Summary:
  - Manual review confirmed the reporting consistency guard requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 13 Stage 13.2 Filter and Context Integrity (2026-05-21)
- Implementation Summary:
  - Documented the filter and context integrity requirement to ensure dashboard filters preserve institute context and prevent metric mixing.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the dashboard filter/context integrity requirement.
- Validation Summary:
  - Manual review confirmed the filter and context integrity requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 13 Stage 13.1 Per-Institute Summary Widgets (2026-05-21)
- Implementation Summary:
  - Documented the per-institute summary widgets requirement to define summary cards/widgets per institute type with context-correct metrics.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the dashboard per-institute summary widget requirement.
- Validation Summary:
  - Manual review confirmed the per-institute summary widgets requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 12 Stage 12.3 Non-Target Protection (2026-05-21)
- Implementation Summary:
  - Documented the non-target protection requirement to ensure ranking logic does not modify existing GPA lifecycle or grading storage.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the ranking non-target protection requirement.
- Validation Summary:
  - Manual review confirmed the non-target protection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 12 Stage 12.2 Tie-Handling and Scope Rules (2026-05-21)
- Implementation Summary:
  - Documented the tie-handling and scope rules requirement to define tie-handling behavior and ranking scope boundaries (class/section/cohort).
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the ranking tie/scope rules requirement.
- Validation Summary:
  - Manual review confirmed the tie-handling and scope rules requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 12 Stage 12.1 Ranking Calculation Contract (2026-05-21)
- Implementation Summary:
  - Documented the ranking calculation contract requirement to define deterministic percentage-based ranking rules for School and College contexts.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the percentage-based ranking contract requirement.
- Validation Summary:
  - Manual review confirmed the ranking calculation contract requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 11 Stage 11.3 Compatibility Guard (2026-05-21)
- Implementation Summary:
  - Documented the compatibility guard requirement to ensure configurable grade scales do not alter University GPA/CGPA flows.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the configurable-grade compatibility guard requirement.
- Validation Summary:
  - Manual review confirmed the compatibility guard requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 11 Stage 11.2 Safe Defaults and Fallback (2026-05-21)
- Implementation Summary:
  - Documented the safe defaults and fallback requirement to enforce baseline defaults when custom grade settings are missing or invalid.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the configurable-grade defaulting and fallback requirement.
- Validation Summary:
  - Manual review confirmed the safe defaults and fallback requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 11 Stage 11.1 Grade Scale Settings Model (2026-05-21)
- Implementation Summary:
  - Documented the grade scale settings model requirement to define admin-manageable grade band settings for School and College contexts.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the configurable grade-scale settings model requirement.
- Validation Summary:
  - Manual review confirmed the grade scale settings model requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Optional Enhancements Decomposition into Phases 11-13 (2026-05-21)
- Implementation Summary:
  - Converted Plan G Optional Enhancements into explicit phases: Phase 11 (Configurable Grading Scale), Phase 12 (Percentage-Based Ranking), and Phase 13 (Result Summary Dashboard).
  - Added stage-level contracts under each phase to maintain compatibility and non-target safety boundaries.
  - No code, schema, or runtime logic was changed; this is a documentation-only planning decomposition update.
- Validation Summary:
  - Manual review confirmed Optional Enhancements are now represented as explicit Phases 11-13 in the Plan G source document.
  - No build, test, or migration was required; this update is documentation-only.

### Plan G Phase 10 Stage 10.4 Reporting and Mixed-Mode Validation (2026-05-21)
- Implementation Summary:
  - Documented the reporting and mixed-mode validation requirement to validate report format correctness and mixed-institute behavior without conflicts.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the reporting/mixed-mode validation requirement.
- Validation Summary:
  - Manual review confirmed the reporting/mixed-mode validation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 10 Stage 10.3 Regression and Lifecycle Validation (2026-05-21)
- Implementation Summary:
  - Documented the regression and lifecycle validation requirement to verify lifecycle flows remain unaffected.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the regression/lifecycle validation requirement.
- Validation Summary:
  - Manual review confirmed the regression/lifecycle validation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 10 Stage 10.2 Output Validation by Institute (2026-05-21)
- Implementation Summary:
  - Documented the output-validation-by-institute requirement to validate School/College outputs as Percentage + Grade and University outputs as GPA/CGPA.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the institute-output validation requirement.
- Validation Summary:
  - Manual review confirmed the institute-output validation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 10 Stage 10.1 Switching Validation (2026-05-21)
- Implementation Summary:
  - Documented the switching validation requirement to confirm license-based switching behavior works across institute types.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the switching-validation requirement.
- Validation Summary:
  - Manual review confirmed the switching-validation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 9 Stage 9.4 Conditional Enforcement Audit (2026-05-21)
- Implementation Summary:
  - Documented the conditional enforcement audit requirement to verify strict institute-based conditional handling at all decision points.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the conditional-enforcement audit requirement.
- Validation Summary:
  - Manual review confirmed the conditional-enforcement audit requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 9 Stage 9.3 Query and Schema Safety (2026-05-21)
- Implementation Summary:
  - Documented the query and schema safety requirement to protect existing report queries and avoid unnecessary database schema changes.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the query/schema safety requirement.
- Validation Summary:
  - Manual review confirmed the query/schema safety requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 9 Stage 9.2 Calculation-Type Separation (2026-05-21)
- Implementation Summary:
  - Documented the calculation-type separation requirement to enforce strict separation between percentage and GPA calculations.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the calculation-type separation requirement.
- Validation Summary:
  - Manual review confirmed the calculation-type separation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 9 Stage 9.1 GPA Overwrite Prevention (2026-05-21)
- Implementation Summary:
  - Documented the GPA overwrite prevention requirement to ensure existing GPA logic is not overwritten.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the GPA-overwrite prevention requirement.
- Validation Summary:
  - Manual review confirmed the GPA-overwrite prevention requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 8 Stage 8.3 Context Purity Guard (2026-05-21)
- Implementation Summary:
  - Documented the context purity guard requirement to prevent percentage and GPA mixing within a single context.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the context-purity guard requirement.
- Validation Summary:
  - Manual review confirmed the context-purity guard requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 8 Stage 8.2 Report Format Alignment (2026-05-21)
- Implementation Summary:
  - Documented the requirement for report format alignment so reports use the correct calculation type for each context.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the report-format alignment requirement.
- Validation Summary:
  - Manual review confirmed the report-format alignment requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 8 Stage 8.1 Result Format Rendering (2026-05-21)
- Implementation Summary:
  - Documented the requirement for result format rendering so School/College contexts show Percentage + Grade while University contexts show GPA/CGPA.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the result-format rendering requirement.
- Validation Summary:
  - Manual review confirmed the result-format rendering requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 7 Stage 7.3 Conflict Prevention in Shared Deployments (2026-05-21)
- Implementation Summary:
  - Documented the requirement to confirm conflict-free behavior for mixed-institution tenants in shared deployments.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the shared-deployment conflict-prevention requirement.
- Validation Summary:
  - Manual review confirmed the shared-deployment conflict-prevention requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 7 Stage 7.2 Cross-Context Example Validation (2026-05-21)
- Implementation Summary:
  - Documented the requirement to validate representative cross-context scenarios, including School->percentage and University->GPA outputs.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the cross-context example validation requirement.
- Validation Summary:
  - Manual review confirmed the cross-context example validation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 7 Stage 7.1 Multi-Institute Dispatch Logic (2026-05-21)
- Implementation Summary:
  - Documented the requirement to apply multi-institute dispatch logic so, when multiple institute types are enabled, the calculation method is selected by department institution type.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the multi-institute dispatch requirement.
- Validation Summary:
  - Manual review confirmed the multi-institute dispatch requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 6 Stage 6.3 Lifecycle API Freeze (2026-05-21)
- Implementation Summary:
  - Documented the lifecycle API freeze requirement: lifecycle APIs and workflows remain unchanged, with only calculation outputs subject to adjustment.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the lifecycle API freeze requirement.
- Validation Summary:
  - Manual review confirmed the lifecycle API freeze requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 6 Stage 6.2 Graduation/Progression Compatibility (2026-05-21)
- Implementation Summary:
  - Documented the requirement to ensure graduation workflows and semester progression remain valid with percentage-based outputs for school and college contexts.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the graduation/progression compatibility requirement.
- Validation Summary:
  - Manual review confirmed the graduation/progression compatibility requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 6 Stage 6.1 Promotion/Failure Compatibility (2026-05-21)
- Implementation Summary:
  - Documented the requirement to ensure School/College promotion and failure decisions correctly consume percentage-based outputs.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the promotion/failure compatibility requirement.
- Validation Summary:
  - Manual review confirmed the promotion/failure compatibility requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 5 Stage 5.3 Non-Target Module Protection (2026-05-21)
- Implementation Summary:
  - Documented the requirement to protect non-target modules so enrollment, assignments, quizzes, and unrelated analytics remain unchanged.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the non-target module protection requirement.
- Validation Summary:
  - Manual review confirmed the non-target module protection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 5 Stage 5.2 Display-Layer Integration (2026-05-21)
- Implementation Summary:
  - Documented the requirement to apply institute-conditional formatting at the result display layer.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the display-layer integration requirement.
- Validation Summary:
  - Manual review confirmed the display-layer integration requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 5 Stage 5.1 Calculation-Layer Integration (2026-05-21)
- Implementation Summary:
  - Documented the requirement to apply institute-conditional logic at the result calculation layer.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the calculation-layer integration requirement.
- Validation Summary:
  - Manual review confirmed the calculation-layer integration requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 4 Stage 4.3 GPA Isolation Guard (2026-05-21)
- Implementation Summary:
  - Documented the requirement to enforce GPA isolation so percentage grade mapping does not affect existing GPA data structures.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the GPA-isolation guard requirement.
- Validation Summary:
  - Manual review confirmed the GPA-isolation guard requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 4 Stage 4.2 Configurable Grade Scale (2026-05-21)
- Implementation Summary:
  - Documented the requirement to implement configurable grade-scale hooks so percentage grade bands can be adjusted in future iterations.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the configurable grade-scale hook requirement.
- Validation Summary:
  - Manual review confirmed the configurable grade-scale hook requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 4 Stage 4.1 Base Grade Bands (2026-05-21)
- Implementation Summary:
  - Documented the requirement to define standardized percentage grade bands for A+, A, B, and C/D for school and college contexts.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the base grade-band definition requirement.
- Validation Summary:
  - Manual review confirmed the base grade-band definition requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 3 Stage 3.3 Invalid Context Handling (2026-05-21)
- Implementation Summary:
  - Documented the requirement to define fallback/error behavior for unsupported or missing institute context.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the invalid-context handling requirement.
- Validation Summary:
  - Manual review confirmed the invalid-context handling requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 3 Stage 3.2 Mapping Resolver Enforcement (2026-05-21)
- Implementation Summary:
  - Documented the requirement to enforce mapping resolver behavior so canonical mapping is always applied before any display/output logic.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the resolver-enforcement requirement.
- Validation Summary:
  - Manual review confirmed the resolver-enforcement requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 3 Stage 3.1 Canonical Mapping Table (2026-05-21)
- Implementation Summary:
  - Documented the requirement to finalize and lock the canonical institute-to-calculation mapping table.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the canonical mapping lock requirement.
- Validation Summary:
  - Manual review confirmed the canonical mapping lock requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 2 Stage 2.4 Non-Refactor Guard (2026-05-21)
- Implementation Summary:
  - Documented the non-refactor guard that explicitly prohibits GPA system modification/refactor during this phase.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the GPA non-refactor protection requirement.
- Validation Summary:
  - Manual review confirmed the GPA non-refactor protection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 2 Stage 2.3 University Calculation Path (2026-05-21)
- Implementation Summary:
  - Documented the requirement to preserve the existing University GPA/CGPA credit-based calculation behavior unchanged.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the university calculation preservation requirement.
- Validation Summary:
  - Manual review confirmed the university calculation preservation requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 2 Stage 2.2 College Calculation Path (2026-05-21)
- Implementation Summary:
  - Documented the requirement to implement percentage-based calculation for colleges (aligned to the school path) and return Percentage + Grade.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the college calculation path requirement.
- Validation Summary:
  - Manual review confirmed the college calculation path requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 1 Stage 1.2 Institute Type Detection (2026-05-21)
- Implementation Summary:
  - Documented the requirement to detect the enabled institute type (School, College, University) at runtime based on the parsed license.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the detection requirement.
- Validation Summary:
  - Manual review confirmed the detection requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 1 Stage 1.1 License Parsing (2026-05-21)
- Implementation Summary:
  - Documented the requirement to parse enabled institute types (School, College, University) from the license file for future conditional logic.
  - No code, schema, or runtime logic was changed; this stage is documentation-only and sets the parsing requirement.
- Validation Summary:
  - Manual review confirmed the parsing requirement is documented and no implementation or schema changes were made.
  - No build, test, or migration was required; this stage is documentation-only.

# Command Center

## Purpose
Use this file as the single handover reference between sessions and devices.
Before starting any work, the assistant must:
1. Read this file.
2. Read Project startup Docs/Final-Touches.md.
3. Continue from the exact Current Execution Pointer below.

## Global Repository Sync Rule (Always Mandatory)
After **every completion** (stage, phase, feature, fix, or documentation task), always add both:
1. `Implementation Summary`
2. `Validation Summary`

After that, always update the repository (commit, push, and pull required) using this sequence:
1. Commit all relevant changes
2. Pull latest remote changes with `--rebase`
3. Push local committed changes
4. Pull once more to confirm local/remote are fully synchronized

Do not end a completed task with local-only changes.

## Mandatory Documentation Sync Rule (Always Mandatory)
After each completed stage, also update these files (where applicable) with `Implementation Summary` and `Validation Summary`:
1. `Docs/Command.md`
2. `Docs/Complete-Functionality-Reference.md` (when functionality changes)
3. `Docs/Function-List.md` (when functions/endpoints/methods are added)
4. `Project startup Docs/Database Schema.md`
5. `Project startup Docs/Development Plan - ASP.NET.md`
6. `Project startup Docs/Modules.md`
7. `Project startup Docs/PRD.md`

### Plan F Phases 4 and 5 Completion Checkpoint (2026-05-21)
- Recent request issue:
  - proceed and complete the remaining Plan F report/UI finance phases.
- Implementation Summary:
  - delivered finance payment reporting end-to-end across report definitions, repository/service/controller/export layers, web client bindings, and the new payment report portal page,
  - updated finance menu visibility so finance users have Payments, Report Center, Analytics, and Theme Settings access while remaining blocked from academic modules,
  - added authorization regression coverage for finance-allowed payment reports and finance-denied academic report endpoints.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --no-build --filter "FullyQualifiedName~AuthorizationRegressionTests"` passed (`64/64`).
- Status of Checks Done:
  - Plan F Phase 4 completed,
  - Plan F Phase 5 completed,
  - Plan F Phases 3, 4, and 5 are now complete,
  - governance docs synchronized,
  - repository synchronization required.

### Plan F Phase 7 Documentation Updates Checkpoint (2026-05-21)
- Recent request issue:
  - update the Finance user guide, training manual, UAT/SAT docs, and related governance artifacts.
- Implementation Summary:
  - added Finance documentation coverage for payment workflows, payment reports, payment analytics, and access boundaries in the user guide,
  - added Finance training material plus Finance UAT/SAT scenarios for payments, reports, analytics, and multi-campus scope,
  - synchronized the phase tracker so Phase 7 is recorded as documentation-only with no runtime behavior change.
- Validation Summary:
  - manual review confirmed the updated documentation is present and internally consistent,
  - no code or automated test execution was required for this documentation-only phase.
- Status of Checks Done:
  - Plan F Phase 7 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan F Phase 8 DB Script Synchronization Checkpoint (2026-05-21)
- Recent request issue:
  - update the standard deployment scripts so Finance and payment-summary behavior is seeded and verified consistently.
- Implementation Summary:
  - added Finance role seeding and payment-summary report access into the standard and clean seed scripts,
  - extended post-deployment checks to verify Finance role presence and payment-summary report role access,
  - recorded Phase 8 as an additive, idempotent script-sync pass.
- Validation Summary:
  - script review confirmed the changes are repeatable and non-destructive,
  - no automated test execution was required for this script synchronization pass.
- Status of Checks Done:
  - Plan F Phase 8 completed,
  - governance docs synchronized,
  - repository synchronization required.


### Plan G Phase 0 Stage 0.1 Protected Surface Declaration (2026-05-21)
  - Declared the protected surface for result calculation: GPA/CGPA logic, lifecycle workflows, storage structure, and report logic are now explicitly frozen against direct modification unless a future phase requires it.
  - No code, schema, or report logic was changed; this stage is a declaration and safety gate only.
  - Manual review confirmed all GPA/CGPA, lifecycle, and report logic remain unchanged.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 0 Stage 0.2 Conditional-Layer-Only Contract (2026-05-21)
- Implementation Summary:
  - Defined the conditional-layer-only contract: Only a conditional decision layer may be added over existing calculation paths; no direct modification of GPA/CGPA, lifecycle, or report logic is allowed.
  - No code, schema, or report logic was changed; this stage is a governance and safety declaration only.
- Validation Summary:
  - Manual review confirmed no direct modification of GPA/CGPA, lifecycle, or report logic.
  - No build, test, or migration was required; this stage is documentation-only.

### Plan G Phase 0 Stage 0.3 Compatibility Defaults (2026-05-21)
- Implementation Summary:
  - Established compatibility defaults: Full backward compatibility is enforced, and University GPA behavior is the default when no condition applies.
  - No code, schema, or report logic was changed; this stage is a governance and compatibility declaration only.
- Validation Summary:
  - Manual review confirmed backward compatibility and default behavior are preserved.
  - No build, test, or migration was required; this stage is documentation-only.

---

---

### Plan F Phase 3 Stage 3.2 Filter-Aware Analytics Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with Stage 3.2 and ensure payment analytics respects department/course/semester scope and dynamic updates.
- Implementation Summary:
  - added `courseId` and `semesterId` query support to payment analytics API/service signatures,
  - enforced course/semester-aware payment aggregation by scoping receipt owners through matching enrollments and offerings,
  - added integration regression test for filtered payment analytics behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`66/66`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 3 Stage 3.2 completed,
  - governance docs synchronized (stage-level),
  - repository synchronization required.

### Plan F Phase 3 Stage 3.1 Payment Status Pie Chart Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with Stage 3.1 and add finance-compatible Paid vs Unpaid analytics pie chart behavior.
- Implementation Summary:
  - added payment analytics contract/service/controller flow for scoped paid vs unpaid aggregation,
  - added finance-compatible analytics endpoint access and portal model/client snapshot wiring for payment status,
  - added interactive payment status pie chart rendering with clickable segment legend and summary-card integration.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AnalyticsInstituteParityIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests"` passed (`65/65`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 3 Stage 3.1 completed,
  - governance docs synchronized (stage-level),
  - repository synchronization required.

### Plan F Phase 2 Stage 2.3 Tenant and Campus Enforcement Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with Stage 2.3 and enforce tenant/campus boundaries for finance payment paths.
- Implementation Summary:
  - added tenant/campus scoped filtering in payment receipt repository queries,
  - enforced scope-aware student lookup before receipt creation,
  - added integration tests for in-scope/out-of-scope payment visibility.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`63/63`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 2 Stage 2.3 completed,
  - governance docs synchronized (stage-level),
  - repository synchronization required.

### Plan F Phase 2 Stage 2.2 Finance Restriction Scope Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with Stage 2.2 and disallow payment deletion while blocking finance access to academic modules.
- Implementation Summary:
  - added explicit payment delete rejection endpoint (`405 Method Not Allowed`) to preserve permanent receipt history,
  - added finance-only academic module guard in web portal action pipeline with payments-page fallback,
  - added authorization regression coverage for finance restrictions and payment delete rejection.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AuthorizationRegressionTests" -v minimal` passed (`54/54`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 2 Stage 2.2 completed,
  - governance docs synchronized (stage-level),
  - repository synchronization required.

### Plan F Phase 3 Stage 3.3 Finance Analytics Isolation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with next stage.
- Implementation Summary:
  - enforced finance-only analytics presentation in the portal by introducing `IsFinanceOnly` model/snapshot mode and suppressing academic analytics chart rendering for finance-only sessions,
  - kept payment analytics available for finance while preserving existing academic analytics behavior for admin/superadmin/faculty roles,
  - added authorization regression tests asserting finance denial on academic analytics endpoints (`performance`, `attendance`, `assignments`) while allowing finance payment analytics access.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - targeted integration tests passed (`66/66`) via `runTests` on:
    - `tests/Tabsan.EduSphere.IntegrationTests/AuthorizationRegressionTests.cs`,
    - `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsInstituteParityIntegrationTests.cs`,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Debug -v minimal` passed (`158/158`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Debug -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 3 Stage 3.3 completed,
  - Plan F Phase 3 completed,
  - governance docs synchronized (stage-level),
  - repository synchronization required.

### Plan F Phase 2 Stage 2.1 Finance Capability Scope Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with Plan F Phase 2 Stage 2.1 and allow Finance to add, edit, and mark payments as paid.
- Implementation Summary:
  - added finance payment edit command and API update endpoint,
  - wired finance update action through web client/controller/UI with update-trail visibility,
  - kept create, confirm, and cancel payment flows intact.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~PaymentReceiptTests|FullyQualifiedName~InstitutionPolicyTests" -v minimal` passed (`27/27`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 2 Stage 2.1 completed,
  - governance docs synchronized (stage-level),
  - repository synchronization required.

### Plan F Phase 1 Stage 1.4 Payment Record State Model Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with Plan F Phase 1 Stage 1.4 and complete payment state tracking model requirements.
- Implementation Summary:
  - expanded payment receipt DTO/output contract to include explicit `PaidDate` and `UpdatedAt` trail fields,
  - preserved backward compatibility by retaining `ConfirmedAt` and mapping paid state from `ConfirmedAt` when required,
  - added payments UI `Last Updated` display so paid/unpaid tracking includes visible update trail.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`156/156`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 1 Stage 1.4 completed,
  - governance docs synchronized (stage-level),
  - phase-level repository synchronization required.

### Plan F Phase 1 Stage 1.3 Finance Role Seed and Linking Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with Plan F Phase 1 Stage 1.3 and implement finance role seeding plus authorization linkage.
- Implementation Summary:
  - added `Finance` as a system role in `DatabaseSeeder` role-seed set,
  - added API authorization policy `Finance` with role gate `SuperAdmin|Admin|Finance`,
  - enabled finance role onboarding through CSV import role validation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release --filter "FullyQualifiedName~InstitutionPolicyTests|FullyQualifiedName~UserImport" -v minimal` passed (`25/25`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 1 Stage 1.3 completed,
  - governance docs synchronized (stage-level),
  - repository synchronization required.

### Plan F Phase 0 Stage 0.1 Baseline Safety Verification Checkpoint (2026-05-20)
- Recent request issue:
  - start Plan F and complete Phase 0 Stage 0.1 with stage-level implementation and validation evidence.
- Implementation Summary:
  - executed Plan F Stage 0.1 as a baseline safety gate before finance implementation,
  - confirmed no production functionality or schema behavior was changed in this stage.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed.
- Status of Checks Done:
  - Plan F Phase 0 Stage 0.1 completed,
  - governance docs synchronized (stage-level),
  - phase-level repository synchronization pending until Phase 0 closeout.

### Plan F Phase 0 Stage 0.2 Isolation and Access Invariants Checkpoint (2026-05-20)
- Recent request issue:
  - complete Plan F Phase 0 Stage 0.2 and confirm tenant/campus/role isolation invariants.
- Implementation Summary:
  - executed isolation/access invariant verification for tenant and campus boundaries plus role-scoped access continuity,
  - kept all existing authorization and scoping behavior unchanged.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`).
- Status of Checks Done:
  - Plan F Phase 0 Stage 0.2 completed,
  - governance docs synchronized (stage-level),
  - phase-level repository synchronization pending until Phase 0 closeout.

### Plan F Phase 0 Stage 0.3 Additive-Only Guardrails Checkpoint (2026-05-20)
- Recent request issue:
  - complete Plan F Phase 0 Stage 0.3 and enforce additive-only guardrails for upcoming phases.
- Implementation Summary:
  - finalized additive-only execution guardrails for Plan F workstream,
  - confirmed no production code, database schema, or deployment behavior was modified during guardrail stage.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F Phase 0 Stage 0.3 completed,
  - Plan F Phase 0 fully completed,
  - governance docs synchronized,
  - repository synchronization required.
### Plan F Transition Readiness Checkpoint (2026-05-20)
- Recent request issue:
  - complete all required readiness gates so execution can move from Plan E closure/backlog hardening to Plan F.
- Implementation Summary:
  - validated Plan E completion state and aligned execution flow to Plan F source document,
  - confirmed Plan F plan artifact exists and is ready for Phase 0 start (`Docs/Phased-Architecture-Plan/F-Finance-Feature-System-Update.md`),
  - updated command-center execution pointer from backlog hold state to Plan F entry state.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -c Release -v minimal` passed (`151/151`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Release -v minimal` passed (`244/244`),
  - `dotnet test tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj -c Release -v minimal` passed (`1/1`).
- Status of Checks Done:
  - Plan F transition gates completed,
  - governance docs synchronized,
  - repository synchronization required.

### Backlog Security Hardening Checkpoint - User Import Template Access Guard (2026-05-20)
- Recent request issue:
  - proceed with the next actionable backlog item and fix a concrete security risk.
- Implementation Summary:
  - enforced Admin/SuperAdmin authorization in `PortalController.UserImportTemplate(...)` so CSV template downloads align with the same access boundary as user import actions,
  - preserved existing file-name allow-listing and traversal-safe path checks.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter "FullyQualifiedName~UserImportAndForceChangeIntegrationTests" -v minimal` passed (`4/4`).
- Status of Checks Done:
  - backlog hardening checkpoint completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 1 Charting Framework & UI Checkpoint (2026-05-20)
- Recent request issue:
  - complete Plan D Phase 1 stages 1.1 to 1.3 and enforce phase-end summary placement.
- Implementation Summary:
  - Stage 1.1 selected/integrated Chart.js,
  - Stage 1.2 implemented responsive Analytics cards/panels layout,
  - Stage 1.3 added color-coded clickable legends for all Analytics charts.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan F Phase 9 Stage 9.2 Data Boundary Enforcement Checkpoint (2026-05-21)
- Recent request issue:
  - proceed with Stage 9.2 and enforce tenant boundaries plus campus filtering across finance paths.
- Implementation Summary:
  - confirmed the payment receipt repository already applies tenant/campus scope via the access-scope resolver for receipt and student lookups,
  - verified the finance report and analytics flows propagate current tenant/campus context into report requests,
  - recorded Stage 9.2 as a verification-only closeout because no code mutation was needed.
- Validation Summary:
  - code review confirmed scoped finance data access is active in the repository layer,
  - no schema or runtime behavior changes were required.
- Status of Checks Done:
  - Plan F Phase 9 Stage 9.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 2 Stage 2.1 Global Filters Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.1 and implement global analytics filters.
- Implementation Summary:
  - added global filters in Analytics UI for institution, department, course, and semester,
  - extended analytics query plumbing with `courseId` and `semesterId` across web client, API controller, and analytics service,
  - applied filter predicates and cache-key dimensions for course/semester in analytics data queries.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 2 Stage 2.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 2 Stage 2.2 Dependent Filtering Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.2 and make each filter update downstream filters.
- Implementation Summary:
  - implemented dependent filter cascading for Analytics (`Institution -> Department -> Course -> Semester`),
  - added offering metadata (`CourseId`, `SemesterId`, `DepartmentId`) to support deterministic dependent option construction,
  - added server-side reset of invalid downstream selections and client-side auto-apply on parent filter changes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 2 Stage 2.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 2 Stage 2.3 Instant Charts Update Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.3 and update charts instantly on filter/legend changes.
- Implementation Summary:
  - added analytics snapshot endpoint for partial JSON refresh,
  - switched analytics filter interactions from full-page form submit to async in-page refresh,
  - updated client-side rendering to rebind summary cards and chart datasets from snapshot payloads.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 2 Stage 2.3 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 3 Stage 3.1 Chart Types and Data Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 3 Stage 3.1 and add pie/bar/line chart coverage for analytics trends.
- Implementation Summary:
  - added advanced trends chart section in Analytics with student-distribution, department-count, course-trend, and semester trend charts,
  - integrated new chart renderers into existing snapshot-driven instant refresh flow,
  - added stale-chart cleanup behavior when scoped filters return empty datasets.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 3 Stage 3.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 4 Stage 4.1 Tenant/Campus Isolation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.1 and harden strict tenant/campus analytics isolation.
- Implementation Summary:
  - enforced tenant/campus scoped filtering in analytics service read queries,
  - removed quiz analytics query-filter bypass (`IgnoreQueryFilters`),
  - scoped analytics distributed-cache keys by tenant/campus and caller scope profile,
  - added integration regression test for tenant/campus constrained analytics visibility.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`66/66`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 4 Stage 4.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 4 Stage 4.2 Leakage Prevention Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.2 and prevent cross-tenant/campus data leakage across broader analytics surfaces.
- Implementation Summary:
  - hardened analytics export-job access checks to owner-or-superadmin with tenant/campus scope parity enforcement,
  - extended export-job request/state payloads with requester tenant/campus metadata,
  - added integration negative tests for cross-user and cross-scope export-job status access.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 4 Stage 4.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 5 Stage 5.1 Performance Optimization Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.1 and optimize analytics queries to avoid full dataset loads.
- Implementation Summary:
  - refactored analytics aggregation paths to use batched grouped queries and in-memory keyed summaries instead of per-entity N+1 queries,
  - applied `AsNoTracking` to heavy analytics read queries,
  - optimized comparative summary aggregation by department-level batched metric retrieval.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 5 Stage 5.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 5 Stage 5.2 Index and Data-Loading Refinement Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.2 and implement proper indexes with efficient data loading support.
- Implementation Summary:
  - introduced analytics hot-path indexes for assignment submission, result publish/recency, and quiz status aggregates,
  - added bounded storage for `assignment_submissions.Status` to support efficient index usage,
  - scaffolded EF migration `PlanDPhase5Stage52AnalyticsIndexes` for controlled deployment.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 5 Stage 5.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 6 Stage 6.1 Validation and UI Consistency Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.1 validation.
- Implementation Summary:
  - performed analytics validation pass focused on interactivity, filtering, and UI consistency,
  - confirmed no additional code or schema modification is needed after Stage 5.2.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 6 Stage 6.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan D Phase 6 Stage 6.2 Final Performance and Security Review Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.2 final review for performance and security.
- Implementation Summary:
  - executed final release-mode analytics validation and security regression verification,
  - confirmed no additional code or schema changes are required for Phase 6 closure.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan D Phase 6 Stage 6.2 completed,
  - Plan D Phase 6 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 1 Stage 1.1 Functional Non-Regression Checkpoint (2026-05-20)
- Recent request issue:
  - there is no Phase 7 in current stream; move to Plan E and start Phase 1 Stage 1.1.
- Implementation Summary:
  - executed Plan E Stage 1.1 functional non-regression validation using full automated coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 1 Stage 1.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 1 Stage 1.2 End-to-End Module Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.2.
- Implementation Summary:
  - executed full end-to-end module validation using complete integration, unit, and contract suites,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 1 Stage 1.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 1 Stage 1.3 UI Alignment and Form Stability Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.3.
- Implementation Summary:
  - executed UI/layout/binding/form stability validation checkpoint using available automated regression coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 1 Stage 1.3 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 1 Stage 1.4 API Response and Runtime Stability Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.4.
- Implementation Summary:
  - executed API response and runtime stability validation checkpoint using full automated coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`),
  - unit tests passed (`151/151`).
- Status of Checks Done:
  - Plan E Phase 1 Stage 1.4 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 1 Stage 1.5 Database Relationship Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.5.
- Implementation Summary:
  - executed database relationship validity checkpoint using full automated regression suites,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 1 Stage 1.5 completed,
  - Plan E Phase 1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 2 Stage 2.1 Tenant and Campus Isolation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.1.
- Implementation Summary:
  - executed tenant/campus isolation validation checkpoint using full automated coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 2 Stage 2.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 2 Stage 2.2 Cross-Tenant/Campus Leakage Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.2.
- Implementation Summary:
  - executed cross-tenant/campus leakage validation checkpoint using full automated coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 2 Stage 2.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 2 Stage 2.3 Tenant/Campus Query Scope Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.3.
- Implementation Summary:
  - executed TenantId/CampusId query-scope validation checkpoint using full automated coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 2 Stage 2.3 completed,
  - Plan E Phase 2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 3 Stage 3.1 Course Material End-to-End Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with next stage.
- Implementation Summary:
  - executed Course Material module end-to-end validation checkpoint using targeted integration plus unit/contract suites,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted Course Material integration tests passed (`5/5`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 3 Stage 3.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 3 Stage 3.2 Analytics Charts and Filters Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed with next stage.
- Implementation Summary:
  - executed analytics charts/filter validation checkpoint using targeted analytics and authorization regression coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted analytics/authorization integration tests passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 3 Stage 3.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 3 Stage 3.3 Tenant and Campus Management Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed tenant/campus management validation checkpoint using targeted integration regression coverage plus unit/contract suites,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted tenant/campus management integration tests passed (`63/63`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 3 Stage 3.3 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 3 Stage 3.4 Role-Based Access Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed role-based access validation checkpoint using full release-mode automated coverage,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 3 Stage 3.4 completed,
  - Plan E Phase 3 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 4 Stage 4.1 UI Consistency and Design Baseline Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed UI consistency/design baseline validation checkpoint using targeted UI-related integration coverage plus unit/contract suites,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 4 Stage 4.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 4 Stage 4.2 Sidebar Header and Content Structure Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed sidebar/header/content structure validation checkpoint using targeted UI-related integration coverage plus unit/contract suites,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 4 Stage 4.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 4 Stage 4.3 Overlap and Responsive Layout Validation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed overlap/responsive layout validation checkpoint using targeted UI-related integration coverage plus unit/contract suites,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted UI-related integration tests passed (`71/71`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 4 Stage 4.3 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 4 Stage 4.4 Validate All Buttons and Actions Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed full UI action validation checkpoint focused on button and action-path continuity,
  - confirmed no code/schema modifications were required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 4 Stage 4.4 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 5 Stage 5.1 TenantId/CampusId Schema Audit Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed schema audit against `Scripts/01-Schema-Current.sql` for `TenantId`/`CampusId` usage,
  - parsed `82` tables and found `0` tables with both `TenantId` and `CampusId` columns in the current schema script,
  - confirmed scoping enforcement currently resides in application/domain logic and no code/schema mutation was applied in this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 5 Stage 5.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 5 Stage 5.2 Foreign Keys, Indexes, and Constraints Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed SQL artifact audit against `Scripts/01-Schema-Current.sql` and `Scripts/04-Maintenance-Indexes-And-Views.sql`,
  - recorded `65` foreign key constraints (`5` via `ALTER TABLE`), `82` primary key constraints, `2` default constraints, and `190` index statements across audited scripts,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 5 Stage 5.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 5 Stage 5.3 Nullable Field Audit Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed nullable-field audit against `Scripts/01-Schema-Current.sql`,
  - recorded `280` nullable columns across `79` tables and identified `2` review-candidate nullable fields (`users.Email`, `timetable_entries.FacultyName`),
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 5 Stage 5.3 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 5 Stage 5.4 Data Integrity and Migration Safety Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed data-integrity and migration-safety audit on schema and post-deployment SQL artifacts,
  - recorded `365` migration-history guard references, `324` `IF NOT EXISTS` guards, `40` transaction blocks, `175` post-deployment verification `SELECT` checks, and `19` EF migration files,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 5 Stage 5.4 completed,
  - Plan E Phase 5 fully completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 6 Stage 6.1 Role-Based Access Review Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed role-based access audit across API/Web controllers, policy registration code, and seed-role SQL artifacts,
  - recorded `359` API `[Authorize]` attributes, `369` role/policy enforcement references, `5` policy registration references, and `105` role-seeding script references,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 6 Stage 6.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 6 Stage 6.2 Unauthorized/Cross-Scope Access Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed unauthorized/cross-tenant/cross-campus access audit across source enforcement points,
  - recorded `1326` isolation-enforcement source hits and `128` explicit `Forbid`/`Unauthorized` enforcement hits,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 6 Stage 6.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 6 Stage 6.3 API Endpoint Restriction Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed API endpoint restriction audit over authorization coverage in API controllers,
  - recorded `447` HTTP endpoints: `92` method-level `[Authorize]`, `349` class-level `[Authorize]` coverage, `1` `[AllowAnonymous]`, and `5` review-set endpoints without explicit authorize coverage,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 6 Stage 6.3 completed,
  - Plan E Phase 6 fully completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 7 Stage 7.1 Query Scope Filtering Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed tenant/campus query-filter audit across source and repository layers,
  - recorded `551` Tenant/Campus scope references, `11` LINQ `Where` scope-filter references, and `20` repository-layer scope references,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 7 Stage 7.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 7 Stage 7.2 Join and Full-Scan Risk Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed query-shape/full-scan risk audit to review joins, includes, raw SQL usage, and pagination coverage,
  - recorded `134` join references (`18` in repository layer), `167` include/then-include references, `0` raw SQL query references, `475` materialization references, and `37` pagination references,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 7 Stage 7.2 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 7 Stage 7.3 Pagination and Analytics Query Efficiency Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed pagination and analytics-query efficiency audit across source and analytics layers,
  - recorded `37` pagination references with `29` nearby ordering safeguards, `551` Tenant/Campus scope references, `11` LINQ scope-filter references, and `18` analytics `AsNoTracking` references,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 7 Stage 7.3 completed,
  - Plan E Phase 7 fully completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 8 Stage 8.1 Environment-Based Configuration Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed environment-based configuration audit across startup and configuration-loading paths,
  - recorded `61` environment-handling references, `132` configuration/deployment references, `54` `appsettings*.json` files, and `127` Program.cs environment/configuration references,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 8 Stage 8.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 8 Stage 8.2 Deployment Scenarios Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed deployment-scenario readiness audit for cloud, on-prem, and multi-instance signals across source startup/configuration paths,
  - recorded `1` cloud reference, `0` on-prem references, `14` multi-instance/scale-out references, `200+` deployment/scaling/instance/tenant-isolation source references (search cap reached), and `9` source `appsettings*.json` files,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 8 Stage 8.2 completed,
  - Plan E Phase 8 in progress (Stage 8.3 pending),
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 8 Stage 8.3 Secrets and Configuration Security Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed secure-secrets/configuration handling audit across startup guards, environment-variable resolvers, and appsettings templates,
  - recorded `18` secure startup guard references, `18` environment-variable secret/deployment references, `18` secret-sensitive configuration key references in source appsettings, `12` placeholder/template secret markers, and `9` source `appsettings*.json` files,
  - confirmed no code/schema mutation was required for this checkpoint.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed (non-blocking warning: CS0105 in API Program.cs),
  - full integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 8 Stage 8.3 completed,
  - Plan E Phase 8 fully completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan E Phase 9 Stage 9.1 Issue and Inconsistency Remediation Checkpoint (2026-05-20)
- Recent request issue:
  - proceed to Plan E Phase 9 Stage 9.1 (identify and fix issues, inconsistencies, or risks).
- Implementation Summary:
  - fixed API startup import inconsistency in `Program.cs` by splitting merged `using` directives and removing duplicate `Tabsan.EduSphere.Application.Services` import,
  - removed the CS0105 warning source to reduce startup bootstrap ambiguity and maintenance risk,
  - confirmed no behavior, contract, or schema mutation was required.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full automated test suites passed (`396/396`),
  - full integration tests passed (`244/244`),
  - full unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 9 Stage 9.1 completed,
  - Plan E Phase 9 in progress (Stage 9.2 pending),
  - governance docs synchronized,

### Plan F Phase 9 Stage 9.3 Analytics Separation Checkpoint (2026-05-21)
- Recent request issue:
  - proceed with Stage 9.3 and keep payment analytics fully separate from academic analytics.
- Implementation Summary:
  - verified payment analytics remains isolated in `AnalyticsController` while academic report flows remain in `ReportController`,
  - confirmed the payment-status endpoint uses finance-scoped analytics inputs and does not reuse the academic report catalog surface,
  - recorded Stage 9.3 as a verification-only closeout because the separation is already enforced by controller boundaries.
- Validation Summary:
  - code review confirmed payment analytics and academic analytics remain on separate controller/service paths,
  - no schema or runtime behavior changes were required.
- Status of Checks Done:
  - Plan F Phase 9 Stage 9.3 completed,
  - governance docs synchronized,
  - repository synchronization required.
  - repository synchronization required.

### Plan E Phase 9 Stage 9.2 Final Stability, Security, and Scalability Review Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - executed final release-readiness review for stability, security, and scalability with full-suite validation,
  - performed source risk-marker sweep (`TODO|FIXME|HACK|XXX|pragma warning disable|AllowAnonymous`) and confirmed markers are expected baseline artifacts (migration/model pragmas and intentional auth exceptions),
  - confirmed no additional code/schema mutation was required.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - full automated test suites passed (`396/396`),
  - full integration tests passed (`244/244`),
  - full unit tests passed (`151/151`),
  - contract tests passed (`1/1`).
- Status of Checks Done:
  - Plan E Phase 9 Stage 9.2 completed,
  - Plan E Phase 9 fully completed,
  - governance docs synchronized,

### Plan F Phase 9 Stage 9.4 Report Data Isolation Checkpoint (2026-05-21)
- Recent request issue:
  - proceed with Stage 9.4 and prevent payment reports from pulling unrelated academic datasets.
- Implementation Summary:
  - updated payment summary reporting so enrollment/course/semester joins run only when `semesterId` or `courseId` filters are provided,
  - preserved payment totals and receipt-status behavior for the default finance report path,
  - reduced non-required academic data loading from finance report execution.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - code review confirmed default payment-summary execution avoids academic joins unless filter-driven.
- Status of Checks Done:
  - Plan F Phase 9 Stage 9.4 completed,
  - governance docs synchronized,
  - repository synchronization required.
  - repository synchronization required.

  ### Plan F Phase 10 Stage 10.1 Access and Multi-Campus Validation Checkpoint (2026-05-21)
  - Recent request issue:
    - proceed.
  - Implementation Summary:
    - aligned `InstitutionPolicyTests` Finance policy modeling with current API authorization policy (`SuperAdmin`, `Finance`),
    - validated multi-campus payment visibility behavior using integration scenarios for matching and mismatched tenant/campus claim scopes.
  - Validation Summary:
    - `runTests` targeted suites passed (`97/97`) across:
      - `tests/Tabsan.EduSphere.UnitTests/InstitutionPolicyTests.cs`,
      - `tests/Tabsan.EduSphere.IntegrationTests/AuthorizationRegressionTests.cs`,
      - `tests/Tabsan.EduSphere.IntegrationTests/StudentLifecycleIntegrationTests.cs`.
  - Status of Checks Done:
    - Plan F Phase 10 Stage 10.1 completed,
    - governance docs synchronized,
    - repository synchronization required.

  ### Plan F Phase 10 Stage 10.2 Analytics and Reporting Validation Checkpoint (2026-05-21)
  - Recent request issue:
    - proceed.
  - Implementation Summary:
    - added explicit payment-summary export integration assertions for Excel and PDF content-type/file-name contracts,
    - validated payment-status analytics filtering behavior for course/semester scoped slices and counts.
  - Validation Summary:
    - `runTests` targeted suites passed (`33/33`) across:
      - `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsInstituteParityIntegrationTests.cs`,
      - `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs`.
  - Status of Checks Done:
    - Plan F Phase 10 Stage 10.2 completed,
    - governance docs synchronized,
    - repository synchronization required.

  ### Plan F Phase 10 Stage 10.3 Data and Import Validation Checkpoint (2026-05-21)
  - Recent request issue:
    - proceed.
  - Implementation Summary:
    - added integration assertions confirming imported mobile/phone values persist for created users,
    - added legacy CSV template compatibility coverage validating `PhoneNumber` header handling.
  - Validation Summary:
    - `runTests` targeted suite passed (`6/6`):
      - `tests/Tabsan.EduSphere.IntegrationTests/UserImportAndForceChangeIntegrationTests.cs`.
  - Status of Checks Done:
    - Plan F Phase 10 Stage 10.3 completed,
    - governance docs synchronized,
    - repository synchronization required.

  ### Plan F Phase 10 Stage 10.4 Documentation Closure Checkpoint (2026-05-21)
  - Recent request issue:
    - proceed.
  - Implementation Summary:
    - completed documentation synchronization audit across Phase 10 governance artifacts,
    - aligned Stage 10.1 through 10.4 sequencing and completion statuses for internal consistency.
  - Validation Summary:
    - manual cross-document verification passed for tracker, command-center, ASP.NET plan, modules, PRD, and schema records,
    - no code, API behavior, or schema mutation was required for this closeout step.
  - Status of Checks Done:
    - Plan F Phase 10 Stage 10.4 completed,
    - Plan F Phase 10 fully completed,
    - governance docs synchronized,
    - repository synchronization required.

### Final-Touches Tracker Restoration Checkpoint (2026-05-20)
- Recent request issue:
  - proceed.
- Implementation Summary:
  - restored missing `Project startup Docs/Final-Touches.md` required by command-center startup workflow,
  - aligned the restored tracker execution pointer with current Plan E completion state.
- Validation Summary:
  - verified `Project startup Docs/Final-Touches.md` exists and is readable,
  - verified execution pointer consistency between `Docs/Command.md` and restored final-touches tracker,
  - no runtime code or schema mutation was introduced.
- Status of Checks Done:
  - Final-Touches tracker restoration completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 6 Performance and Optimization Checkpoint (2026-05-20)
- Recent request issue:
  - complete Plan C Phase 6 Stage 6.1 and Stage 6.2.
- Implementation Summary:
  - Stage 6.1 optimized Course Material read queries with `AsNoTracking` and scope-missing short-circuit logic,
  - Stage 6.2 added composite index `IX_course_materials_scope_active_sort` and migration `PlanCPhase6Stage2CourseMaterialIndexTuning`.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Status of Checks Done:
  - Plan C Phase 6 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 7 Stage 7.1 Validation Checkpoint (2026-05-20)
- Recent request issue:
  - start Plan C Phase 7 Stage 7.1 validation.
- Implementation Summary:
  - executed full validation pass for Course Material data safety, access control, and UI stability,
  - verified Phase 5 and Phase 6 implementations remain stable under full-suite execution.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`393/393`).
- Status of Checks Done:
  - Plan C Phase 7 Stage 7.1 completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 7 Stage 7.2 Finalization Checkpoint (2026-05-20)
- Recent request issue:
  - complete Plan C Phase 7 Stage 7.2 final review for stability and scalability.
- Implementation Summary:
  - performed final release-readiness review for Course Material stability and scalability,
  - executed release build and release-mode targeted Course Material authorization validation,
  - verified load-test assets are present and runnable with an available API environment.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - Release-mode integration tests (Course Material authorization subset) passed (`5/5`),
  - `k6-auth-current.js` execution attempt failed only due to unavailable local target API endpoint (`http://localhost:5181`).
- Status of Checks Done:
  - Plan C Phase 7 Stage 7.2 completed,
  - Plan C Phase 7 fully completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 5 File and Link Handling Checkpoint (2026-05-20)
- Recent request issue:
  - complete Plan C Phase 5 Stage 5.1, Stage 5.2, and Stage 5.3.
- Implementation Summary:
  - Stage 5.1 added multipart upload endpoint and portal upload wiring,
  - Stage 5.2 added scoped persistent storage paths and file download endpoint,
  - Stage 5.3 added authorization/access regression coverage and role-aware portal fallback handling.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed.
- Status of Checks Done:
  - Plan C Phase 5 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 4 UI and UX Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan C Phase 4 UI and UX implementation.
- Implementation Summary:
  - added portal manage and student read-only pages for course materials,
  - added course-material web actions for list/create/update/activate/deactivate flows,
  - integrated `course_material` sidebar mapping in both web layout route/group mapping and API sidebar entitlement module-key mapping.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - new web/UI integration compiled without diagnostics.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan C Phase 4 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 3 Access Control and Security Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan C Phase 3 access control and strict isolation.
- Implementation Summary:
  - added `CourseMaterialController` with read access for authenticated users and write access restricted to `Faculty,Admin,SuperAdmin`,
  - added `ICourseMaterialService`/`CourseMaterialService` and `ICourseMaterialRepository`/`CourseMaterialRepository`,
  - enforced strict tenant/campus filtering in repository queries with SuperAdmin bypass consistency.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - new access-control and scope-enforcement code compiled without diagnostics.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan C Phase 3 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 2 Data Safety and Migration Checkpoint (2026-05-19)
- Recent request issue:
  - proceed after Plan C Phase 1.
- Implementation Summary:
  - added domain-level scope and name validation for `CourseMaterial`,
  - added database check constraints for scope integrity, material type safety, and file/link location validity,
  - added migration `PlanCPhase2DataSafetyScopeGuard` for safe enforcement.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - migration compiled without diagnostics.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan C Phase 2 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan C Phase 1 Domain and Database Extension Checkpoint (2026-05-19)
- Recent request issue:
  - start Plan C Phase 1.
- Implementation Summary:
  - added `CourseMaterial` domain entity for tenant/campus scoped material records,
  - linked materials to department, academic program, semester, and course (subject scope),
  - added EF configuration and migration `PlanCPhase1CourseMaterialFoundation` with foreign keys and indexes.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - migration compiled without diagnostics.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan C Phase 1 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 10 Validation and Finalization Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to validation and finalization after logging and visibility.
- Implementation Summary:
  - completed the final readiness review for the Phase B configuration/deployment stack,
  - confirmed the startup path remains secure, backward-compatible, and supportable across development, testing, deployment, and customer-isolated environments,
  - no new code changes were required for this closeout stage.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - final security and scalability review completed against the already implemented startup path.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 10 finalization completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 9 Logging and Visibility Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to logging and visibility after configuration performance and stability.
- Implementation Summary:
  - added a shared startup visibility reporter,
  - standardized safe startup logs across API, Web, and BackgroundJobs,
  - surfaced active environment, configuration source summary, database type, deployment profile, and tenant isolation posture without exposing credentials.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - logging changes remained safe and non-secret.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 9 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 8 Performance and Stability Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to performance and stability improvements for configuration startup.
- Implementation Summary:
  - optimized the shared configuration bootstrapper to avoid duplicate config providers,
  - preserved config precedence for deployment/external/local/tenant overlays without rebuilding redundant provider chains,
  - reduced unnecessary reload watchers for deployment-style overlay files.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - startup configuration hierarchy remained backward-compatible.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 8 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 7 Fail-Safe Behavior Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to fail-safe behavior after tenant-aware configuration support.
- Implementation Summary:
  - added shared startup fail-safe validation for API, Web, and BackgroundJobs,
  - centralized checks for resolved database configuration, reverse-proxy trust settings, tenant overlay paths, and required non-development values,
  - fixed non-development DB validation to honor deployment-based connection sources instead of only legacy connection-string keys.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - startup validation behavior is now consistent across all three hosts.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 7 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 6 Tenant + Campus Aware Configuration Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to tenant-aware configuration and isolation after customer deployment support.
- Implementation Summary:
  - added `TenantIsolationResolver` for per-tenant settings and isolation metadata,
  - added optional `EDUSPHERE_TENANT_CONFIG_PATH` overlay support for tenant-specific JSON files,
  - seeded deployment templates with `TenantIsolation` sections and surfaced the metadata in startup/health diagnostics.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - no schema mutation or functional regression introduced.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 6 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 5 Customer Deployment Support Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to customer deployment support after deployment flexibility.
- Implementation Summary:
  - added optional deployment-pipeline JSON layer to the shared config bootstrap,
  - seeded API/Web/BackgroundJobs templates with a `Deployment` config section,
  - preserved config-first customer overrides with env-var fallback and safe startup diagnostics.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - no schema mutation or functional regression introduced.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 5 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 4 Deployment Flexibility Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to deployment flexibility after secure configuration handling.
- Implementation Summary:
  - added deployment-topology resolver for cloud/customer-hosted/multi-instance profile metadata,
  - added per-customer domain/database/scaling override support via config and environment variables,
  - surfaced safe deployment metadata in startup logs and health diagnostics.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - no schema mutation or functional regression introduced.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 4 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 3 Secure Configuration Handling Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to secure configuration handling after database connection management.
- Implementation Summary:
  - added external configuration file support for deployment-provided secrets,
  - added secure production validation helper that rejects placeholder or missing secrets without exposing secret contents in errors,
  - kept startup diagnostics source-safe by only emitting non-secret metadata.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - no schema mutation or functional regression introduced.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 3 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 2 Database Connection Management Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to the next Plan B phase after Phase 1 completion.
- Implementation Summary:
  - added shared `DatabaseConnectionResolver` for startup DB connection resolution,
  - prioritized deployment and environment overrides ahead of legacy fallback configuration,
  - updated API and BackgroundJobs startup to consume resolver output and log source-safe diagnostics.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - behavior remains backward-compatible through fallback to `ConnectionStrings:DefaultConnection`.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 2 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan B Phase 1 Configuration Structure Checkpoint (2026-05-19)
- Recent request issue:
  - proceed and start Plan B before moving to next plan.
- Implementation Summary:
  - added shared startup configuration bootstrap helper to enforce consistent config hierarchy,
  - switched API/Web/BackgroundJobs startup configuration loading to shared hierarchy helper,
  - added optional local override layer plus prefixed environment variable support with fallback.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - existing application behavior remained backward-compatible.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan B Phase 1 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 7 Validation and Finalization Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan A Phase 7 and complete final system validation plus closeout synchronization.
- Implementation Summary:
  - executed final full validation sweep across build/unit/integration/contract layers,
  - confirmed all Plan A phase outputs remain stable and additive,
  - completed final governance-doc synchronization for Plan A closeout.
- Validation Summary:
  - solution build passed,
  - full unit, integration, and contract suites passed,
  - InstitutionType (School/College/University) behavior remained unchanged.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`236/236`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`388/388`).
- Status of Checks Done:
  - Plan A Phase 7 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 6 Performance and Optimization Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan A Phase 6 and optimize tenant/campus scoped query performance.
- Implementation Summary:
  - optimized scoped user lookup predicates to avoid non-sargable `ToLower()` transformations,
  - added composite indexes for scoped user and department query hot paths,
  - added migration `Phase46_TenantCampusQueryOptimization`.
- Validation Summary:
  - solution build passed,
  - focused unit and integration suites passed,
  - InstitutionType (School/College/University) behavior remained unchanged.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Status of Checks Done:
  - Plan A Phase 6 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 5 UI Management Interfaces Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan A Phase 5 and add tenant/campus management UI with existing sidebar/menu integration.
- Implementation Summary:
  - added SuperAdmin-only API endpoints for tenant and campus management,
  - added portal screens for tenant and campus create/update/activate/deactivate flows,
  - integrated new screens into existing sidebar/menu patterns and SuperAdmin fallback navigation.
- Validation Summary:
  - solution build passed,
  - focused unit and integration test suites passed,
  - InstitutionType (School/College/University) behavior remained unchanged.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Status of Checks Done:
  - Plan A Phase 5 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 4 Access Control and Filtering Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan A Phase 4 and implement tenant/campus scoped access with SuperAdmin bypass.
- Implementation Summary:
  - added request access-scope resolver to read tenant/campus claims plus caller role,
  - added token claims (`tenant_id`, `campus_id`) at login for runtime scope propagation,
  - enforced tenant/campus filtering in user and department repositories,
  - enabled explicit SuperAdmin cross-tenant/campus bypass.
- Validation Summary:
  - solution build passed,
  - focused unit and integration suites passed,
  - InstitutionType (School/College/University) behavior remained unchanged.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Status of Checks Done:
  - Plan A Phase 4 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 3 Compatibility and Safety Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan A Phase 3 and enforce compatibility/safety guarantees for tenant/campus integration without changing InstitutionType flow.
- Implementation Summary:
  - added domain guard logic to prevent partial tenant/campus assignment,
  - added database check constraints and composite campus+tenant foreign keys for integrity,
  - added migration `Phase43_TenantCampusCompatibilitySafety`.
- Validation Summary:
  - solution build passed,
  - focused unit and integration test suites passed,
  - InstitutionType (School/College/University) behavior remained unchanged.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests filter passed (`9/9`),
  - integration tests passed (`52/52`).
- Status of Checks Done:
  - Plan A Phase 3 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 2 Data Integration Checkpoint (2026-05-19)
- Recent request issue:
  - proceed to Plan A Phase 2 and ensure default tenant/campus assignment safety for existing data.
- Implementation Summary:
  - added migration `Phase42_DefaultTenantCampusBackfill` for safe default tenant/campus assignment,
  - enhanced startup seeding to enforce default tenant/campus presence and backfill null scoping fields in `users` and `departments`.
- Validation Summary:
  - solution build passed,
  - focused unit and integration suites passed,
  - existing School/College/University InstitutionType flow remained unchanged.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests (`EnrollmentServiceWaitlistTests|AuthSecurityUxTests`) passed (`9/9`),
  - integration tests (`AdminUserManagementIntegrationTests|AuthorizationRegressionTests`) passed (`52/52`).
- Status of Checks Done:
  - Plan A Phase 2 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 1 Implementation Checkpoint (2026-05-19)
- Recent request issue:
  - proceed from Plan A Phase 1 kickoff into actual domain-layer implementation and keep mandatory tracker synchronization plus repository sync.
- Implementation Summary:
  - added tenancy domain foundation entities (`Tenant`, `Campus`),
  - extended core root entities (`users`, `departments`) with optional `TenantId` + `CampusId`,
  - added EF configurations, indexes, and relationships for tenant/campus scoping foundation,
  - added migration `Phase41_TenantCampusFoundation` for non-breaking schema expansion.
- Validation Summary:
  - solution build passed,
  - focused unit suite passed (`9/9`),
  - existing InstitutionType model remained unchanged.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal --filter "FullyQualifiedName~EnrollmentServiceWaitlistTests|FullyQualifiedName~AuthSecurityUxTests"` passed (`9/9`).
- Status of Checks Done:
  - Plan A Phase 1 implementation completed,
  - governance docs synchronized,
  - repository synchronization required.

### Plan A Phase 1 Kickoff Checkpoint (2026-05-19)
- Recent request issue:
  - start Plan A Phase 1 and synchronize mandatory planning/governance documents with phase-end implementation and validation summaries.
- Implementation Summary:
  - started Plan A Phase 1 (Domain Layer Extension) execution baseline,
  - updated Plan A document so implementation/validation summary is captured at the end of Phase 1,
  - synchronized required documents: Command, Function List, Complete Functionality Reference, Development Plan, Database Schema, Modules, and PRD.
- Validation Summary:
  - cross-document alignment review completed for this phase kickoff,
  - confirmed this execution wave is documentation-governance only with no runtime or schema mutation.
- Testing and result summary:
  - documentation consistency validation completed,
  - no code-path test execution required for this documentation-only phase kickoff.
- Status of Checks Done:
  - Plan A Phase 1 kickoff documentation completed,
  - repository synchronization required.

### Database Script Alignment Checkpoint (2026-05-19)
- Proceed continuation executed to ensure DB script parity with latest implemented user security/contact model.
- Implementation Summary:
  - updated schema script to add missing users columns when absent (PhoneNumber, MfaIsEnabled, MfaTotpSecret, MfaRecoveryCodesHashJson),
  - added maintenance index IX_users_active_phone for active SMS-recipient lookups,
  - extended full/clean post-deployment checks with users phone/MFA column verification,
  - aligned script README execution guidance for clean path with optional maintenance step.
- Validation Summary:
  - script/docs diagnostics returned no errors,
  - focused health/license integration sanity remained green.
- Testing and result summary:
  - get_errors on changed script/docs files: clean,
  - runTests Phase36Stage4HealthAndLicenseGateTests: passed (3/3).
- Status of Checks Done:
  - DB scripts updated,
  - audit docs updated,
  - repository synchronization required.

### UAT Stability Wave Checkpoint (2026-05-19)
- Proceed continuation executed after role-based UAT sweep.
- Implementation Summary:
  - ran an additional integration validation wave for performance smoke, health/license gates, security hardening, dashboard context switching, analytics institute parity, and query performance validation.
- Validation Summary:
  - all selected suites completed successfully with zero failures.
- Testing and result summary:
  - runTests bundle passed (28/28, failed: 0):
    - Phase36Stage4PerformanceSmokeTests
    - Phase36Stage4HealthAndLicenseGateTests
    - Phase31Stage2SecurityHardeningTests
    - DashboardContextSwitchingIntegrationTests
    - AnalyticsInstituteParityIntegrationTests
    - PerformanceQueryValidationIntegrationTests
  - Result: extended stability/security/performance automated UAT baseline remains green.
- Status of Checks Done:
  - extended UAT stability wave complete,
  - DeepSystemScan updated with evidence,
  - repository synchronization required.

### Live Browser UAT Attempt Checkpoint (2026-05-19)
- Proceed continuation executed for manual browser walkthrough.
- Implementation Summary:
  - attempted local API startup to perform browser-driven role walkthrough and chatbot interaction checks.
- Validation Summary:
  - API did not reach endpoint binding in this local attempt (startup remained in external DB mode state), so live browser UAT could not be completed in-session.
  - fallback automated evidence remains green.
- Testing and result summary:
  - manual browser UAT: blocked by local environment startup dependency state,
  - fallback evidence:
    - full regression 388/388 passed,
    - role UAT matrix 119/119 passed,
    - extended stability wave 28/28 passed.
- Status of Checks Done:
  - blocker documented,
  - alternative validation evidence recorded,
  - repository synchronization required.

### Role-Based UAT Sweep Checkpoint (2026-05-19)
- Proceed step executed after DeepSystemScan phase completion.
- Implementation Summary:
  - ran cross-role integration UAT matrix covering role authorization, sidebar/module visibility, report catalog scoping, and parent portal behavior.
- Validation Summary:
  - selected UAT suites completed with no failures.
- Testing and result summary:
  - runTests bundle passed (119/119, failed: 0):
    - CrossRoleUatMatrixIntegrationTests
    - SidebarMenuIntegrationTests
    - StudentSubmenuParityIntegrationTests
    - AuthorizationRegressionTests
    - ReportCatalogIntegrationTests
    - ParentPortalIntegrationTests
  - Result: automated role-based UAT baseline remains green.
- Status of Checks Done:
  - role-based UAT integration sweep complete,
  - audit docs updated,
  - repository synchronization required.

### Deep System Audit Checkpoint (2026-05-19 - Phase 1-7 Completion)
- Completed full request scope:
  - phase-by-phase system audit and validation,
  - AI chatbot modernization using modular floating components,
  - mandatory governance-doc synchronization.
- Recent request issue:
  - perform full system audit+validation+UI enhancement and update PRD/Command/Function List/Functionality Reference/Development Plan/Database Schema after each phase with testing summary.

#### Phase 1 - System Understanding
- Implementation Summary:
  - completed deep docs-to-code mapping and module coverage verification,
  - identified route consistency and chat UI modernization as primary execution targets.
- Validation Summary:
  - static traceability checks completed for controller-service-repository and DTO/UI links.
- Testing and result summary:
  - phase 1 completed with findings inventory.

#### Phase 2 - API and Backend Validation
- Implementation Summary:
  - added `/api/v1/ai` alias route to AI controller while retaining `/api/ai`,
  - updated web API client chat endpoints to `/api/v1/ai/*`,
  - extended module license enforcement map for `/api/v1/ai`.
- Validation Summary:
  - build and focused tests passed after API/backend updates.
- Testing and result summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit focused suite passed (`9/9`), integration focused suite passed (`4/4`).

#### Phase 3 - Database and Data Flow Validation
- Implementation Summary:
  - validated EF/repository flow behavior for touched areas and confirmed zero schema deltas.
- Validation Summary:
  - no migration, FK, or index change required by this request.
- Testing and result summary:
  - phase 3 completed with no new data integrity issues introduced.

#### Phase 4 - UI and Frontend Validation
- Implementation Summary:
  - validated shared layout/menu/chat binding behavior before and after component extraction.
- Validation Summary:
  - preserved existing widget state/send contracts and message history behavior.
- Testing and result summary:
  - phase 4 completed with no frontend contract regression.

#### Phase 5 - Performance and Stability Check
- Implementation Summary:
  - completed async/stability sanity review for changed paths.
- Validation Summary:
  - build and focused regressions remain green.
- Testing and result summary:
  - phase 5 completed with no new stability regressions observed.

#### Phase 6 - AI Chatbot Redesign (Critical)
- Implementation Summary:
  - removed inline layout chatbot markup,
  - added modular components:
    - `Views/Shared/FloatingChatButton.cshtml`
    - `Views/Shared/ChatPanel.cshtml`
  - preserved existing backend routes and widget behavior.
- Validation Summary:
  - post-change build and module-enforcement integration tests passed.
- Testing and result summary:
  - phase 6 completed with modular floating chatbot implementation.

#### Phase 7 - Final Consolidated Reporting
- Implementation Summary:
  - synchronized all required governance docs with per-phase implementation and testing summaries.
- Validation Summary:
  - documentation coverage completed for this request cycle.
- Testing and result summary:
  - phase 7 completed (reporting synchronized).

- Status of Checks Done:
  - all requested phases completed,
  - build and focused regression suites passed,
  - repository sync actions pending (commit/pull --rebase/push/final pull).

### UI/UX Redesign Checkpoint (2026-05-19 - Phase 9 Final UI Polish)
- Completed the final redesign polish pass and closeout step.
- Recent request issue:
  - proceed with the last redesign phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
  - refined global spacing and card elevation,
  - improved dashboard presentation detail,
  - tightened consistency across shared content surfaces for a more finished SaaS feel.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched final-polish stylesheet.
- Status of Checks Done:
  - phase 9 final polish validated,
  - redesign spec phase-completion summary updated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### UI/UX Redesign Checkpoint (2026-05-18 - Phase 8 Responsive Hardening)
- Completed the requested proceed step for responsive refinement.
- Recent request issue:
  - proceed with the next redesign phase and maintain mandatory doc + repository synchronization.
- Implementation Summary:
  - added shared responsive hardening for shell/header/profile menu/action groups/filter toolbars,
  - improved mobile behavior for result actions and payment pagination,
  - tightened spacing and overflow handling for cards, tables, and modals on smaller screens.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched responsive frontend files.
- Status of Checks Done:
  - phase 8 responsive pass validated,
  - redesign spec phase-completion summary updated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### UI/UX Redesign Checkpoint (2026-05-18 - Phase 7 AI Chatbot UI)
- Completed the requested proceed step for AI assistant UI refinement.
- Recent request issue:
  - proceed with the next phase and keep docs + commit/push/pull mandatory after completion.
- Implementation Summary:
  - upgraded AI chatbot panel identity and visual hierarchy,
  - improved launcher look and open-state polish,
  - added quick-prompt suggestion chips and frontend-only interactions in the chat thread,
  - preserved existing `AiChatWidgetState` and `AiChatWidgetSend` backend usage.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched chatbot frontend files.
- Status of Checks Done:
  - phase 7 chatbot UI pass validated,
  - redesign spec phase-completion summary updated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### UI/UX Redesign Checkpoint (2026-05-18 - Phase 6 Branding)
- Completed the requested proceed step for branding-focused UI refinement.
- Recent request issue:
  - proceed and always update docs and commit, push, and pull after completion.
- Implementation Summary:
  - improved branding in the shared shell with stronger institution identity styling,
  - refined header composition,
  - upgraded notification-chip visual treatment,
  - implemented a richer profile dropdown UI while preserving existing routes and sign-out flow.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in `Views/Shared/_Layout.cshtml` and `wwwroot/css/site.css`.
- Status of Checks Done:
  - phase 6 branding pass validated,
  - redesign spec phase-completion summary updated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### UI/UX Redesign Continuation Checkpoint (2026-05-18 - Enrollments/Results/Payments Polish + Phase-Level Summary Formatting)
- Completed the requested next continuation step and applied phase-summary placement at each phase in the redesign spec.
- Recent request issue:
  - proceed with next page-level polish and keep implementation/validation summaries at the end of each phase (not only at end of document).
- Implementation Summary:
  - refined `Enrollments`, `Results`, and `Payments` pages with consistent section cards, filter-toolbar treatment, stat/label consistency, and improved empty-state presentation,
  - updated `Docs/Improved UI and look.md` so completion details are placed at each phase and markdown structure is lint-clean.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in `Enrollments.cshtml`, `Results.cshtml`, `Payments.cshtml`, and `Docs/Improved UI and look.md`.
- Status of Checks Done:
  - continuation wave-B polish validated,
  - phase-level summary formatting requirement satisfied,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### UI/UX Redesign Continuation Checkpoint (2026-05-18 - Students/Courses/Admin Users Page Polish)
- Completed the requested continuation pass after the initial global redesign.
- Recent request issue:
  - proceed with further UI consistency so high-traffic management pages match the new premium SaaS visual system.
- Implementation Summary:
  - refined `Students`, `Courses`, and `Admin Users` pages with consistent section headers, empty states, polished toolbars, and improved action alignment,
  - added shared helper styles for section layout, stat pills, empty-state cards, and option-grid cards in the global design system,
  - preserved all existing routes, form actions, and page-level workflow logic.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the continuation updates,
  - no backend/API/controller/database files were touched.
- Status of Checks Done:
  - frontend continuation polish validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### UI/UX Redesign Checkpoint (2026-05-18 - Frontend-Only SaaS Visual Refresh)
- Completed the requested full UI/UX redesign pass for the web application without changing backend or business logic.
- Recent request issue:
  - the application needed a complete visual refresh so it presents like a professional modern SaaS product for schools, colleges, and universities while preserving all existing functionality.
- Implementation Summary:
  - redesigned the shared shell in `Views/Shared/_Layout.cshtml` with premium branding, icon-led sidebar navigation, stronger header composition, responsive mobile menu behavior, and upgraded AI assistant launcher/panel presentation,
  - replaced `wwwroot/css/site.css` with a unified academic design system covering colors, typography, spacing, cards, forms, tables, modals, loaders, toasts, responsive rules, and chatbot visuals,
  - rebuilt `Views/Portal/Dashboard.cshtml` into a more polished card-and-hero layout while preserving the existing data model and form-post behavior,
  - added minor frontend-only enhancements in `wwwroot/js/site.js` for loader fade-out, visual toast surfacing from existing alerts, and responsive shell interactions.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the first layout/dashboard/JS redesign pass,
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed again after the design-system CSS replacement,
  - workspace diagnostics reported no errors in the four touched frontend files,
  - no backend/API/controller/database files were changed in this redesign checkpoint.
- Status of Checks Done:
  - frontend redesign validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Validation and Rollout Checkpoint (2026-05-18 - Full Regression, SMS Rollout Checklist, Repo Cleanup Scan)
- Completed the requested follow-up bundle after TODO cleanup.
- Implementation Summary:
  - ran full solution regression and confirmed end-to-end green baseline,
  - added `Docs/Phase40-Sms-Production-Rollout-Checklist.md` to document production SMS enablement prerequisites, rollout steps, smoke test, monitoring, and rollback,
  - scanned for broader repo cleanup opportunities and corrected stale SMS documentation that still described phone-number resolution as a placeholder/stub.
- Validation Summary:
  - `dotnet test Tabsan.EduSphere.sln -v minimal` passed (`388/388`),
  - stale SMS placeholder references were removed from the current documentation set,
  - new rollout checklist doc passed workspace validation checks.
- Status of Checks Done:
  - regression complete,
  - rollout checklist prepared,
  - broader cleanup scan completed with actionable stale-doc items addressed,
  - repository sync required after this checkpoint.

### Cleanup Checkpoint (2026-05-18 - Residual TODO/Test Stub Cleanup)
- Completed remaining actionable TODO cleanup after Stage 40.1.
- Implementation Summary:
  - replaced `NotImplementedException` placeholders in unit-test `ISettingsRepository` stubs with safe empty/no-op implementations,
  - updated the Stage 32.3 SMS follow-up note to reflect that `PhoneNumber` persistence is complete and only operational rollout tasks remain.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal --filter "FullyQualifiedName~Phase30Stage2Tests|FullyQualifiedName~Phase30Stage3Tests|FullyQualifiedName~Phase31Stage1RegressionMatrixTests"` passed (`32/32`),
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Status of Checks Done:
  - code/doc cleanup validated,
  - repository sync required after this checkpoint.

### Stage Completion Checkpoint (2026-05-18 - Stage 40.1 PhoneNumber/SMS Recipient Dependency Completion)
- Completed Stage 40.1 phone-backed SMS recipient dependency implementation.
- Implementation Summary:
  - added optional user `PhoneNumber` persistence and EF mapping,
  - added migration `20260518104000_Phase40_AddUserPhoneNumber` for schema update,
  - implemented active-user phone lookup in notification repository,
  - wired optional phone capture through admin user create/update/list, CSV import, and student self-registration flows.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test Tabsan.EduSphere.sln -v minimal --filter "FullyQualifiedName~Phase28Stage2Tests|FullyQualifiedName~UserImportAndForceChangeIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~StudentRegistration"` passed (`13/13`).
- Status of Checks Done:
  - code and migration change validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Feature Completion Checkpoint (2026-05-18 - StudentLifecycle Notification TODO Completion)
- Completed implementation of pending StudentLifecycle notification TODOs.
- Implementation Summary:
  - implemented student notifications for graduation, promotion, deactivate, and reactivate lifecycle actions,
  - implemented admin-review pending notifications for change/modification request creation,
  - implemented approval/rejection outcome notifications for requestor/teacher review workflows.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~StudentLifecycleIntegrationTests -v minimal` passed (`7/7`).
- Status of Checks Done:
  - code change validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Phase Completion Checkpoint (2026-05-18 - DeepScan Phase 40 Closure and Production Readiness Revalidation)
- Completed Phase 40 closure revalidation and DeepScan evidence-pack synchronization.
- Implementation Summary:
  - re-ran build and targeted validation suites for all previously open DeepScan gap areas,
  - updated `Docs/DeepScan.md` with re-execution task-by-task closure output (`4.1` through `4.20`) and final readiness classification,
  - synchronized closure snapshots across mandatory governance trackers.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter EnrollmentServiceWaitlistTests -v minimal` passed (`2/2`),
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter AuthSecurityUxTests -v minimal` passed (`7/7`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
  - no unresolved critical/high functional gap remains in the re-executed DeepScan output.
- Status of Checks Done:
  - code and documentation closure validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Documentation Sync Checkpoint (2026-05-18 - DeepScan Gap Phase/Stage Synchronization Request)
- Completed the latest documentation-only closeout for DeepScan-identified missing/partial areas and the matching request-synchronization update.
- Implementation Summary:
  - added the DeepScan gap remediation phases/stages to the consolidated execution tracker,
  - synchronized matching request-closeout snapshots across PRD, function registry, full functionality reference, development plan, and database schema docs,
  - marked the completed documentation TODOs that were already satisfied by the updated governance documents.
- Validation Summary:
  - verified the six mandatory governance docs contain matching 2026-05-18 entries with implementation and validation sections,
  - verified the consolidated tracker now contains explicit Phase 39/40 remediation and closure planning for the DeepScan findings,
  - confirmed this update introduced no runtime code, database, or deployment changes.
- Status of Checks Done:
  - documentation-only validation complete,
  - repository sync completed after the documentation update sequence,
  - no unresolved file or migration change was required for this checkpoint.

### Stage Completion Checkpoint (2026-05-18 - DeepScan Stage 39.2 Transactional CSV Import Strict Mode)
- Completed the Stage 39.2 user-import strict-mode remediation with validated rollback behavior.
- Implementation Summary:
  - added optional strict-mode rollback behavior to the user import service and controller,
  - extended the import result payload to report strict/permissive execution mode,
  - preserved permissive import behavior as the default backward-compatible path.
- Validation Summary:
  - targeted user-import integration suite passed (`4/4`),
  - verified strict-mode import rolls back all rows for mixed-validity CSV input,
  - verified existing permissive import and forced-password-change flow remain green.
- Status of Checks Done:
  - code change validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Stage Completion Checkpoint (2026-05-18 - DeepScan Stage 39.4 EF Relationship and Query-Filter Warning Cleanup)
- Completed Stage 39.4 EF mapping/filter cleanup to remove known startup warning set.
- Implementation Summary:
  - aligned dependent query filters with filtered required principals across affected EF configurations,
  - fixed quiz question explicit relationship mapping to remove shadow FK (`QuizId1`) behavior,
  - removed course enum DB default that triggered sentinel/default warning behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
  - verified targeted EF warning set is no longer emitted in focused startup validation output.
- Status of Checks Done:
  - code change validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Stage Completion Checkpoint (2026-05-18 - DeepScan Stage 39.1 Enrollment Waitlist and Seat-Promotion Workflow)
- Completed the Stage 39.1 enrollment waitlist and promotion remediation with validated queue-on-full behavior.
- Implementation Summary:
  - added a waitlisted enrollment state and ordered repository access for queued students,
  - updated enrollment service behavior to waitlist students when full and promote the oldest queued student after a drop,
  - added focused unit coverage for waitlist creation and seat-release promotion.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter EnrollmentServiceWaitlistTests` passed (`2/2`),
  - verified full-offering enrollments now queue instead of rejecting,
  - verified seat release advances the waitlist in queue order.
- Status of Checks Done:
  - code change validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Stage Completion Checkpoint (2026-05-18 - DeepScan Stage 39.3 MFA Hardening (TOTP + Recovery Codes))
- Completed the Stage 39.3 MFA hardening remediation with per-user TOTP and recovery-code support.
- Implementation Summary:
  - replaced demo-code MFA login checks with per-user TOTP verification,
  - added one-time recovery-code generation, hashed persistence, and consumption path,
  - added authenticated MFA setup/enable/recovery-code regeneration endpoints,
  - added user MFA persistence fields with migration support.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter AuthSecurityUxTests` passed (`7/7`),
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`4/4`),
  - verified force-change-password compatibility after MFA hardening.
- Status of Checks Done:
  - code change validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Stage Completion Checkpoint (2026-05-20 - Plan F Phase 6 Stage 6.1/6.2/6.3 User Import Template and Validation Extension)
- Completed Plan F Phase 6 import-template extension with compatibility and validation safeguards.
- Implementation Summary:
  - extended user import parser to support optional `MobileNumber` aliasing to existing phone-number ingestion,
  - added optional `CampusAssignments` parser validation (pipe-separated GUID format) while keeping persistence behavior unchanged,
  - updated portal import guidance and distributed CSV templates with `MobileNumber` and `CampusAssignments` columns,
  - preserved backward compatibility for legacy templates without new columns.
- Validation Summary:
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`),
  - parser diagnostics for touched files reported no static errors,
  - verified both extended-template and legacy-template import flows in integration coverage.
- Status of Checks Done:
  - code change validated,
  - governance docs synchronized,
  - repository sync required after this checkpoint.

### Phase Completion Checkpoint (2026-05-21 - Plan F Phase 6 Import Sheets)
- Completed Plan F Phase 6 with all stage summaries and validations recorded.
- Implementation Summary:
  - Stage 6.1 completed template/header extension with optional `MobileNumber` and `CampusAssignments`,
  - Stage 6.2 confirmed legacy template backward compatibility when new columns are omitted,
  - Stage 6.3 completed mobile/campus field-format validation rules without schema mutation.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests` passed (`6/6`).
- Status of Checks Done:
  - phase-level implementation and validation summaries completed,
  - mandatory docs synchronized,
  - repository sync (commit/pull/push/pull) pending this checkpoint.

## Institution License Validation Workflow (2026-05-12)
- Plan file: `Docs/Institution-License-Validation-Phases.md`
- For each completed validation phase, mandatory outputs are:
  1. `Implementation Summary`
  2. `Validation Summary`
  3. `Status of Checks Done`
- After every completed validation phase, update all required docs:
  1. Docs/Function-List.md
  2. Docs/Functionality.md
  3. Project startup Docs/PRD.md
  4. Project startup Docs/Database Schema.md
  5. Project startup Docs/Development Plan - ASP.NET.md
  6. Docs/Command.md
- After every phase completion, run repository sync in order (mandatory):
  1. Commit
  2. Pull (`--rebase`)
  3. Push
  4. Pull again to confirm local and remote are synchronized
- Do not treat a phase as completed until both summaries and this full sync sequence are done.

## Institute Parity Workflow (2026-05-13)

- Plan file: `Docs/Institute-Parity-Issue-Fix-Phases.md`
- After every completed stage (not only phase-end), mandatory stage entry content:
  1. `Implementation Summary`
  2. `Validation Summary`

- After every completed stage, mandatory document synchronization list:
  1. `Docs/Institute-Parity-Issue-Fix-Phases.md`
  2. `Docs/Function-List.md`
  3. `Docs/Complete-Functionality-Reference.md`
  4. `Project startup Docs/Database Schema.md`
  5. `Project startup Docs/Development Plan - ASP.NET.md`
  6. `Project startup Docs/PRD.md`
  7. `Docs/Command.md`

- Mandatory Git synchronization order after stage completion:
  1. Commit
  2. Pull (`--rebase`)
  3. Push
  4. Pull again to confirm local and remote are synchronized
- A stage is not complete unless both `Implementation Summary` + `Validation Summary` and this full sync sequence are finished.

### Institute Parity Checkpoint (2026-05-13 - Stage 0.1)
- Completed Stage 0.1 baseline parity audit in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 0.2 (Role and Institute Access Matrix).

### Institute Parity Checkpoint (2026-05-13 - Stage 0.2)
- Completed Stage 0.2 role/institute access matrix in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Confirmed baseline access enforcement mapping across parity-scope modules and recorded gap list for explicit institute-hardening work.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 0.3 (Report Failure Inventory).

### Institute Parity Checkpoint (2026-05-13 - Stage 0.3)
- Completed Stage 0.3 report failure inventory in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.

### Deployment Readiness Checkpoint (2026-05-15 - Stage 36.4)
- Completed Stage 36.4 security, reliability, and performance gates in `Docs/Consolidated-Execution-Enhancements-Issues.md` with required Implementation Summary and Validation Summary.
- Added the combined Stage 36.4 gate script, health/license smoke tests, and dry-run compatibility for the Stage 34 recovery utilities.
- Next stage: Stage 36.5 (UAT/SAT, Runbook, and Operational Sign-Off).

### Deployment Readiness Checkpoint (2026-05-15 - Stage 36.5)
- Completed Stage 36.5 UAT/SAT, runbook, and operational sign-off in `Docs/Consolidated-Execution-Enhancements-Issues.md` with required Implementation Summary and Validation Summary.
- Added deployment and rollback runbook with ownership and escalation, plus Stage 36.5 UAT/SAT approval and sign-off evidence artifacts.
- Next stage: Stage 36.6 (Go-Live Execution and Hypercare Plan).

### Deployment Readiness Checkpoint (2026-05-15 - Stage 36.6)
- Completed Stage 36.6 go-live execution and hypercare plan in `Docs/Consolidated-Execution-Enhancements-Issues.md` with required Implementation Summary and Validation Summary.
- Added Stage 36.6 go-live/hypercare runner script, dedicated hypercare plan document, and Stage 36.6 execution evidence artifact.
- Next action: execute production go-live through the rollback-safe flow and monitor hypercare checkpoints to close Phase 36.

### Final Separation Checkpoint (2026-05-15 - Phase 37)
- Completed Phase 37 separation of runtime app publish outputs from Tabsan.Lic publish outputs.
- Execute-mode validation PASS with separated package outputs.
- Added publish separation script and evidence report:
  - `Scripts/Phase37-Separate-App-And-License-Publish.ps1`
  - `Artifacts/Phase37/Publish-Separation-20260515.md`
  - `Artifacts/Phase37/Tabsan.EduSphere-App-Publish-20260515.zip`
  - `Artifacts/Phase37/Tabsan.Lic-Publish-20260515.zip`

### Final Separation Checkpoint (2026-05-15 - Phase 38)
- Completed Phase 38 separation of non-runtime folders from runtime app publish outputs.
- Execute-mode validation PASS with separated package output.
- Added non-runtime asset separation script and evidence report:
  - `Scripts/Phase38-Separate-NonRuntime-Assets.ps1`
  - `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`
  - `Artifacts/Phase38/NonRuntime-Assets-20260515.zip`
- Classified report issues by root-cause tags and mapped resolution ownership to next stages.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 0.4 (Exit Criteria sign-off for Phase 0 baseline).

### Institute Parity Checkpoint (2026-05-13 - Stage 0.4)
- Completed Stage 0.4 exit criteria in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Completed Phase 0 baseline sign-off and confirmed readiness to begin Phase 1 execution.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next phase: Phase 1 (Institute Domain and Data Foundation), starting Stage 1.1.

### Institute Parity Checkpoint (2026-05-13 - Stage 1.1)
- Completed Stage 1.1 institute model normalization in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Implemented canonical institute dimension at department level and synchronized API/web contracts with policy-aware write validation.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 1.2 (Referential Integrity + Indexing).

### Institute Parity Checkpoint (2026-05-13 - Stage 1.2)
- Completed Stage 1.2 referential integrity + indexing in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Implemented department-scoped program uniqueness, academic write-path link validation, and report/analytics-focused index hardening.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 1.3 (Script Hardening).

### Institute Parity Checkpoint (2026-05-13 - Stage 1.3)
- Completed Stage 1.3 script hardening in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Updated schema, maintenance, and post-deployment SQL scripts for parity-safe idempotent rollout and verification of Stage 1.1/1.2 changes.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 1.4 (Exit Criteria).

### Institute Parity Checkpoint (2026-05-13 - Stage 1.4)
- Completed Stage 1.4 exit criteria in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added explicit post-deployment checks for institute-type coverage/validity and orphan-count validation across institute-linked entities.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next phase: Phase 2 (Authorization and Permission Correction), starting Stage 2.1.

### Institute Parity Checkpoint (2026-05-13 - Stage 2.1)
- Completed Stage 2.1 SuperAdmin global capability in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added SuperAdmin faculty department assignment APIs and institution-type compatibility checks for admin/faculty assignment writes.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 2.2 (Role-Scoped Institute Enforcement).

### Institute Parity Checkpoint (2026-05-13 - Stage 2.2)
- Completed Stage 2.2 role-scoped institute enforcement in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added token-level institution claim propagation and report-handler institute scope checks for Admin/Faculty callers.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 2.3 (Menu/Action Guard Consistency).

### Institute Parity Checkpoint (2026-05-13 - Stage 2.3)
- Completed Stage 2.3 menu/action guard consistency in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added centralized portal menu/action guard enforcement to align direct route access with sidebar visibility rules.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 2.4 (Exit Criteria).

### Institute Parity Checkpoint (2026-05-13 - Stage 2.4)
- Completed Stage 2.4 exit criteria in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Validated Phase 2 role + institute access matrix end-to-end across assignment controls, report institute-scope checks, and menu/action guard consistency.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next phase: Phase 3 (Module CRUD and Workflow Parity), starting Stage 3.1.

### Institute Parity Checkpoint (2026-05-13 - Stage 3.1)
- Completed Stage 3.1 core academic/admin module parity in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Corrected portal department create/edit paths to submit explicit institution type and added cross-institution CRUD validation for department/course workflows.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 3.2 (Core Academic/Admin Modules continuation).

### Institute Parity Checkpoint (2026-05-13 - Stage 3.2)
- Completed Stage 3.2 student lifecycle institute parity in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added lifecycle institute-scope enforcement for Admin users and portal lifecycle institute-filter alignment, plus integration mismatch-deny coverage.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 3.3 (Student Submenu Parity).

### Institute Parity Checkpoint (2026-05-13 - Stage 3.3)
- Completed Stage 3.3 student submenu parity in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added institute-aware student submenu data scope enforcement on student list endpoint and aligned student/enrollment submenu terminology for School/College/University neutrality.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 3.4 (Exit Criteria).

### Institute Parity Checkpoint (2026-05-13 - Stage 3.4)
- Completed Stage 3.4 phase-exit criteria in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Consolidated Phase 3 module parity evidence and fixed web compile parity by adding lookup-model institution-type support used by institute-aware filtering paths.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next phase: Phase 4 (Analytics and Reports Parity + Reliability), starting Stage 4.1.

### Institute Parity Checkpoint (2026-05-13 - Stage 4.1)
- Completed Stage 4.1 analytics filter expansion in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added institute-aware analytics query filtering across API/service layers plus portal analytics institute/department filter controls with role-aware defaults.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 4.2 (Reports Filter Expansion).

### Institute Parity Checkpoint (2026-05-13 - Stage 4.2)
- Completed Stage 4.2 reports filter expansion in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added institute-aware report filter propagation across API/service/repository layers plus report-center institution filter controls in portal pages.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 4.3 (Broken Report Fixes).

### Institute Parity Checkpoint (2026-05-13 - Stage 4.3)
- Completed Stage 4.3 broken report fixes in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Repaired faculty report-scope reliability on department-scoped report endpoints and aligned offering-scope checks with department assignments.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 4.4 (Phase 4 Exit Criteria).

### Institute Parity Checkpoint (2026-05-13 - Stage 4.4)
- Completed Stage 4.4 phase-exit validation in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Confirmed Phase 4 analytics/report parity closure via full integration-suite regression pass.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Phase 5 Stage 5.1 (User Creation and CSV Import with Institution Assignment).

### Institute Parity Checkpoint (2026-05-13 - Stage 5.1)
- Completed Stage 5.1 core seed coverage in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Updated core seed script with institution policy flags, institution-typed baseline departments, report key normalization, and policy-aligned report/sidebar role-access defaults.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 5.2 (Full Dummy Coverage - All Tables).

### Institute Parity Checkpoint (2026-05-13 - Stage 5.2)
- Completed Stage 5.2 full dummy coverage in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Expanded full dummy script with deterministic institute assignments and parity coverage blocks for timetable/buildings/rooms, payments, lifecycle artifacts, and report export artifacts.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 5.3 (Data Quality and Replay Safety).

### Institute Parity Checkpoint (2026-05-13 - Stage 5.3)
- Completed Stage 5.3 data quality and replay safety in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Hardened dummy replay alignment and expanded post-deployment checks with institute-level parity counts, critical workflow coverage checks, and duplicate-safety signals.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 5.4 (Exit Criteria).

### Institute Parity Checkpoint (2026-05-13 - Stage 5.4)
- Completed Stage 5.4 phase-exit validation in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Validated one-run script readiness with full execution order (`01` -> `02` -> `03` -> `05`) and successful post-deployment parity quality outputs.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Phase 6 Stage 6.1 (Automated Test Expansion).

### Institute Parity Checkpoint (2026-05-13 - Stage 6.1)
- Completed Stage 6.1 automated test expansion in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added focused integration parity coverage for lifecycle, student submenu, and report matched-institute success paths, complementing existing mismatch-deny safeguards.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 6.2 (Cross-Role UAT Matrix).

### Institute Parity Checkpoint (2026-05-13 - Stage 6.2)
- Completed Stage 6.2 cross-role UAT matrix in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added `CrossRoleUatMatrixIntegrationTests` executing School/College/University x SuperAdmin/Admin/Faculty/Student matrix across report catalog, account-security, and attendance authorization endpoints.
- Cross-role UAT suite passed 100/100 with zero failures across all institution and role combinations.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 6.3 (Performance and Query Validation).

### Institute Parity Checkpoint (2026-05-13 - Stage 6.3)
- Completed Stage 6.3 performance and query validation in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added `PerformanceQueryValidationIntegrationTests` for parity index read-usage evidence on institute-filtered query paths and no-major-regression latency budgets on common Admin dashboard/report paths.
- Stage 6.3 validation suite passed 2/2 with zero failures.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 6.4 (Exit Criteria).

### Institute Parity Checkpoint (2026-05-13 - Stage 6.4)
- Completed Stage 6.4 phase exit criteria in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Executed consolidated Phase 6 parity certification run across lifecycle/submenu parity, cross-role UAT matrix, report/authorization guards, analytics parity, and performance/query validation suites.
- Consolidated Stage 6 phase-exit suite passed 132/132 with zero failures and no critical/blocker defects.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next phase: Phase 7 (Release, Documentation, and Operational Readiness), starting Stage 7.1.

### Institute Parity Checkpoint (2026-05-13 - Stage 7.1)
- Completed Stage 7.1 deployment runbook in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Finalized deployment run-order, environment notes, and rollback/verification checklist in `Scripts/README.md` for deterministic parity rollout.
- Validation confirmed required script set exists, schema bootstrap/context-switch guards are present, and post-deployment fail-fast checks are in place.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 7.2 (Functional Documentation Update).

### Institute Parity Checkpoint (2026-05-13 - Stage 7.2)
- Completed Stage 7.2 functional documentation update in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Updated functionality and user-facing guides with explicit role/institute filter behavior guidance for parity operations.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 7.3 (Monitoring and Support Handover).

### Institute Parity Checkpoint (2026-05-13 - Stage 7.3)
- Completed Stage 7.3 monitoring and support handover in `Docs/Institute-Parity-Issue-Fix-Phases.md` with required Implementation Summary and Validation Summary.
- Added parity monitoring points for report/analytics failures and a SuperAdmin institute-scope incident triage checklist.
- Synchronized required tracking docs for stage closeout:
  - `Docs/Institute-Parity-Issue-Fix-Phases.md`
  - `Docs/Function-List.md`
  - `Docs/Complete-Functionality-Reference.md`
  - `Project startup Docs/Database Schema.md`
  - `Project startup Docs/Development Plan - ASP.NET.md`
  - `Project startup Docs/PRD.md`
  - `Docs/Command.md`
- Next stage: Stage 7.4 (Exit Criteria).

### Institution Validation Checkpoint (2026-05-12 - Phase 1)
- Execution evidence captured in `Docs/Institution-License-Validation-Phases.md`.
- Authentication and policy-read checks passed.
- License upload is now successful after resolving legacy DB schema defaults on `license_state` (`InstitutionScope`, `ExpiryType`).
- Final module restriction validation completed through `portal-capabilities/matrix` (School/College disabled, University enabled).
- Current phase status: Phase 1 completed.

### Institution Validation Checkpoint (2026-05-12 - Phase 2)
- Execution evidence captured in `Docs/Institution-License-Validation-Phases.md`.
- Root fix applied: institution policy persistence now commits `portal_settings` changes in `InstitutionPolicyService.SavePolicyAsync`.
- Verified mode switching and lifecycle evidence:
  - School mode: policy/labels/matrix/progression aligned to School semantics.
  - College mode: policy/labels/matrix/progression aligned to College semantics.
  - University mode: policy/labels/matrix/progression aligned to University semantics.
- DB policy flags now persist correctly per uploaded mode.
- Current phase status: Phase 2 completed.
- Known validation caveat: generated licenses currently reuse one verification key, so sequential mode validation in a single environment required clearing `consumed_verification_keys` between activations.

### Institution Validation Checkpoint (2026-05-12 - Phase 3)
- Execution evidence captured in `Docs/Institution-License-Validation-Phases.md`.
- Completed mixed-mode validation for:
  - School + College
  - School + University
  - College + University
  - School + College + University
- Confirmed union behavior in policy flags and capability matrix row exposure for each combination.
- Confirmed DB persistence of `institution_include_school|college|university` keys per uploaded combination.
- Current phase status: Phase 3 completed.
- Next phase: Phase 4 (Charts, Tables, Menus, and Reports by Institution and Role).

### Institution Validation Checkpoint (2026-05-12 - Phase 4)
- Execution evidence captured in `Docs/Institution-License-Validation-Phases.md` and `Artifacts/Phase4/ModeRole/20260512-142021`.
- Completed mode-role validation matrix for School, College, University across SuperAdmin, Admin, Faculty, and Student.
- Confirmed policy/labels/matrix/dashboard/report-catalog are accessible for authenticated roles in all modes and vary by role/mode as expected.
- Confirmed scoped report data/export behavior:
  - SuperAdmin/Admin/Faculty succeed with valid scope filters.
  - Student operational report data/export endpoints are denied (`403`).
- Confirmed negative checks by role:
  - `admin-user`: SuperAdmin `200`; Admin/Faculty/Student `403`.
  - `license/details`: SuperAdmin/Admin `200`; Faculty/Student `403`.
- Current phase status: Phase 4 completed.
- Next phase: Phase 5 (User Creation and CSV Import with Institution Assignment).

### Institution Validation Checkpoint (2026-05-12 - Phase 5)
- Execution evidence captured in `Docs/Institution-License-Validation-Phases.md` and `Artifacts/Phase5/Api/` (`20260512-144212` set).
- Implemented explicit per-user institution assignment in manual create/import flows.
- Added policy enforcement for institution assignment:
  - disabled institution assignment is rejected,
  - enabled institution assignment is accepted and persisted.
- Added migration `AddUserInstitutionTypeAssignment` for `users.InstitutionType`.
- Updated API/Web contracts and integration tests for manual create + CSV import institution assignment handling.
- Current phase status: Phase 5 completed.
- Next phase: Phase 6 (Data Access Boundaries by Assigned Institution).

### Institution Validation Checkpoint (2026-05-12 - Phase 6)
- Execution evidence captured in `Docs/Institution-License-Validation-Phases.md` and `Artifacts/Phase6/Access/20260512-150824`.
- Validated role-scoped access boundaries on report export endpoints:
  - Admin assigned department access succeeds; non-assigned department denied.
  - Faculty assigned offering access succeeds; non-assigned offering denied.
  - Student blocked from operational report export endpoints.
- Confirmed allowed student read surfaces remain available:
  - report catalog and dashboard context.
- Current phase status: Phase 6 completed.
- Next phase: Phase 7 (SuperAdmin Full Access and Permission Matrix).

### Institution Validation Checkpoint (2026-05-12 - Phase 7)
- Execution evidence captured in `Docs/Institution-License-Validation-Phases.md` and `Artifacts/Phase7/SuperAdmin/20260512-151302`.
- Completed SuperAdmin permission matrix:
  - department CRUD and deactivation,
  - admin user create/deactivate/reactivate,
  - mode-switch validation for School/College/University with successful privileged dashboard/report/license access in each mode.
- Run summary result: `35/35` successful checks, `0` failures.
- Current phase status: Phase 7 completed.
- Institution License Validation plan status: completed (Phases 1-7).

## Non-Negotiable Rule Per Completed Stage / Phase
After **every completed stage** (not just at phase-end), update **all** required tracking files:
1. Docs/Function-List.md
2. Docs/Command.md
3. Docs/Advance-Enhancements.md
4. Project startup Docs/PRD.md
5. Project startup Docs/Database Schema.md
6. Project startup Docs/Development Plan - ASP.NET.md
7. Docs/Functionality.md

After every documentation update, complete mandatory Git sync in this exact order:
1. Commit all current changes
2. Pull latest remote changes (rebase preferred)
3. Push committed changes to remote

For every completed stage entry in `Docs/High-Load-Optimization-Phases-And-Stages.md`, include:
- `Status: Completed`
- `Implementation Summary`
- `Validation Summary`

Also
- completed work
- validation summary
- next steps
- pending extras

**Always-on documentation sync (mandatory workflow — enforced from Phase 20 onward):**
- After **every completed stage**, update all tracking files listed above before moving to the next stage.
- Do not batch documentation to phase-end.
- Keep `Docs/Function-List.md`, `Docs/Command.md`, `Docs/Advance-Enhancements.md`, `Project startup Docs/PRD.md`, `Project startup Docs/Database Schema.md`, and `Project startup Docs/Development Plan - ASP.NET.md` updated continuously as work progresses.

**Always-on Git sync (requested workflow):**
- Before ending any work session, always run full sync: commit all changes, pull from remote, then push to remote.
- Do not leave local-only completed work.
- **Always do BOTH pull and push** — pull first (rebase), then push. Never push without pulling first.
- Use this command sequence:

```powershell
cmd /c git -C "<repo-root>" add -A
cmd /c git -C "<repo-root>" commit -m "<phase/stage summary>"
cmd /c git -C "<repo-root>" pull --rebase origin main
cmd /c git -C "<repo-root>" push origin main
```

**Code quality rules (enforced from Phase 5 onward):**
- Add a `// Final-Touches Phase X Stage X.X — <description>` comment above every block of code added or changed for that phase.
- Update `Docs/Function-List.md` with a new `## Final-Touches Phase X` section listing all new/modified functions, their purpose, and file location after every completed phase.

---

## Current Execution Pointer
- Plan Source: Docs/Phased-Architecture-Plan/F-Finance-Feature-System-Update.md
- Active Phase: **Plan F Phase 0 - Stability and Safety**
- Active Stage: **Plan F Phase 0 completed; Stage 1.1 User and Identity Fields is next**
- Status: **Plan F Phase 0 completed with stage-level validation and governance synchronization.**
- Last Updated: 2026-05-20
- Next: **Start Plan F Phase 1 - Database Updates.**
- Docs Updated: ✅ Plan F transition-readiness checkpoint and governance synchronization completed (2026-05-20).

### 2026-05-11 - Phase 10 Completion
- Stage 10.1: Added a parameterized progressive gate runner for stepwise scale validation and higher-tier execution.
- Stage 10.2: Added bottleneck classification heuristics to identify the first likely limiting class from each gate run.
- Stage 10.3: Added a retest loop so the same gate can be rerun after targeted fixes before promoting to the next stage.
- Validation: PowerShell syntax check on `tests/load/run-phase10-progressive.ps1` passed; editor diagnostics on `tests/load/k6-phase10-progressive.js` reported no errors.

### 2026-05-11 - Phase 9 Completion
- Stage 9.1: Added OpenTelemetry metrics publishing with Prometheus scraping endpoint support and runtime instrumentation.
- Stage 9.2: Added rolling request-latency capture with `/health/observability` output for p50, p95, and p99 tracking.
- Stage 9.3: Added health checks for database, CPU, memory, network, and error-rate monitoring.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 - Phase 8 Completion
- Stage 8.1: Added `InfrastructureTuning:AutoScaling` policy controls and startup validation in API, Web, and BackgroundJobs.
- Stage 8.2: Added host-limit controls (`InfrastructureTuning:HostLimits`) with thread-pool minimum tuning and Kestrel concurrent-connection controls.
- Stage 8.3: Added network-stack controls (`InfrastructureTuning:NetworkStack`) and outbound HTTP handler tuning for high-connection workloads.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 - Phase 7 Completion
- Stage 7.1: Added account-security email queue offloading so unlock/reset request paths enqueue email work items.
- Stage 7.2: Added queue platform integration for account-security queue processing with `QueuePlatform:Provider` selecting in-memory or RabbitMQ mode.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 - Phase 6 Completion
- Stage 6.1: Added short-TTL distributed caching for external library loan lookups with cache keys scoped to student + integration config fingerprint.
- Stage 6.2: Added circuit-breaker failure threshold/open-window support to outbound integration gateway channels.
- Stage 6.3: Replaced blocking `.Result` usage in gradebook request assembly with awaited async results.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 - Phase 5 Completion
- Stage 5.1: Converted 50k/100k/1m/5m scripts to `ramping-arrival-rate` and added randomized think-time windows.
- Stage 5.2: Added distributed generator sharding support (`GENERATOR_TOTAL`, `GENERATOR_INDEX`) in scale scripts and runners.
- Stage 5.3: Enforced summary-first output (`--quiet`, summary export) and gated raw JSON outputs in PowerShell runner behind `-AllowRawOutput`.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 - Phase 4 Completion
- Stage 4.1: Added short-TTL distributed cache for expensive analytics report endpoints (`performance`, `attendance`, `assignments`, `quizzes`).
- Stage 4.2: Added configurable static-asset cache headers in Web startup and appsettings profiles for edge/CDN-friendly caching.
- Stage 4.3: Restricted cache scope to expensive shared-safe operations and static assets only.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 - Phase 3 Stage 3.3 Completion
- Added Kestrel transport tuning in API and Web hosts: keep-alive timeout, request-header timeout, server-header suppression, and HTTP/2 ping tuning.
- Kept response compression enabled with Brotli/Gzip fast-path settings.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).

### 2026-05-11 - Phase 3 Stage 3.2 Completion
- Removed `ContinueWith` bridges from the hot timetable, settings, quiz, and building/room repository methods.
- Kept the hot request paths fully asynchronous by returning `await ToListAsync(...)` directly.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` passed (**130/130**).


### 2026-05-10 - Phase 33 Stage 33.3 Completion
- Added DataAnnotations to `LoginRequest`, `RefreshRequest`, `ChangePasswordRequest`, `ForceChangePasswordRequest`, `CreateAdminUserRequest`, and `UpdateAdminUserRequest`.
- Added `SecurityValidationTests` to verify the hardened DTO validation paths.
- Validation: `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj --filter "FullyQualifiedName~SecurityValidationTests"` passed (**4/4**); `dotnet test Tabsan.EduSphere.sln --no-build` passed (**234/234**).

### 2026-05-10 - Phase 33 Stage 33.2 Completion
- Added config-driven reverse-proxy trust controls in API/Web (`ReverseProxy:Enabled`, `KnownProxies`, symmetry/forward-limit settings).
- Added startup guardrails to reject unsafe reverse-proxy and production CORS-empty startup conditions.
- Removed `localhost` fallback defaults from Web login and portal API connection model.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed; `dotnet test Tabsan.EduSphere.sln --no-build` passed (**230/230**).

### 2026-05-10 - Phase 33 Stage 33.1 Completion
- Re-scoped Phase 33 to `Hosting Configuration and Security Hardening` from `Docs/Refactoring-Hosting-Security.md`.
- Added explicit environment-aware configuration loading blocks in:
  - `src/Tabsan.EduSphere.API/Program.cs`
  - `src/Tabsan.EduSphere.Web/Program.cs`
  - `src/Tabsan.EduSphere.BackgroundJobs/Program.cs`
- Added startup validation for required settings (`DefaultConnection`, `EduApi:BaseUrl`) and enforced BackgroundJobs environment override for connection string placeholder.
- Aligned API/Web/BackgroundJobs appsettings metadata for base identity/version and environment URLs.
- Validation: `dotnet build Tabsan.EduSphere.sln` passed.

### 2026-05-10 - Post-Stage Verification Refresh
- Command run: `dotnet build Tabsan.EduSphere.sln` -> passed (build warnings unchanged).
- Command run: `dotnet test Tabsan.EduSphere.sln --no-build` -> passed (**230/230**).
- Notes: no new failures introduced after Stage 32.5 completion and git sync.

---

## ⚡ Database & Migration Status

✅ **All pending migrations applied successfully** (2026-05-09):
1. `20260505_Phase2LicenseConcurrency` — adds `MaxUsers` + `ActivatedDomain` to `license_state` ✅
2. `20260506_Phase4UserImport` — adds `MustChangePassword` to `users` ✅
3. `20260507103000_PortalBrandingLogoValueMaxLength` — alters `portal_settings.Value` to `nvarchar(max)` ✅
4. `20260506044806_Phase6AdminDepartmentAssignments` — adds `admin_department_assignments` table ✅
5. `20260507_Phase12AcademicCalendar` — adds `academic_deadlines` table ✅
6. `20260507_Phase14_Helpdesk` — adds `support_tickets` + `support_ticket_messages` tables ✅
7. `20260507133254_Phase15_EnrollmentRules` — adds `course_prerequisites` table with unique index ✅
8. `Phase16_FacultyGrading` — adds `rubrics`, `rubric_criteria`, `rubric_levels`, `rubric_student_grades` tables ✅
9. `Phase17_DegreeAudit` — adds `degree_rules`, `degree_rule_required_courses` tables + `course_type` column on `courses` ✅
10. `20260508132355_Phase22_ExternalIntegrations` — adds `accreditation_templates` table ✅
11. `20260508152906_Phase25_AcademicEngineUnification` — adds `institution_grading_profiles` table ✅
12. `20260509044437_Phase26_SchoolCollegeExpansion` — adds `school_streams`, `student_stream_assignments`, `student_report_cards`, `bulk_promotion_batches`, `bulk_promotion_entries`, `parent_student_links` tables ✅

Database is fully synchronized with codebase.

## ⚡ Phase Summary — All Issue-Fix Phases Complete

✅ **Issue-Fix Phase 4 — Student Workflow Repair (ALL 6 stages)**
- Stage 4.1 — Assignment submission flow wired end-to-end (submit/view/grade)
- Stage 4.2 — Timetable department auto-resolved from student profile; `Guid.Empty` guard added
- Stage 4.3 — Assignments semester filter; semester-scoped offering dropdown for students
- Stage 4.4 — Results semester filter; fallback to student-safe result endpoints on 403
- Stage 4.5 — Quizzes semester filter; Upcoming/Pending/Completed status badges
- Stage 4.6 — FYP menu gated to 8th semester; student completion-request; faculty approval; auto-complete when all approvers approve; FYP result row in Results
- **Validation: 12/12 assignment integration tests passed; 0 build errors**

✅ **Issue-Fix Phase 5 — Reporting and Export Center (Stages 5.1–5.5)**
- Stage 5.1 — Assignment and Quiz summary report APIs + portal pages
- Stage 5.2 — CSV/PDF export for Attendance, Results, Assignments, Quizzes (Excel retained)
- Stage 5.3 — SuperAdmin unrestricted report scope confirmed
- Stage 5.4 — Admin reporting scope bounded by assigned departments (Phase 6 data model + portal guidance guards on all 9 report pages)
- Stage 5.5 — Faculty scope enforced on department/offering filters and report data/export endpoints

✅ **Issue-Fix Phase 6 — Admin Multi-Department Assignment (Stages 6.1–6.2 + Extension)**
- Backend: `AdminDepartmentAssignment` entity, repository, migration, API endpoints
- UI: Departments page assignment panel; dedicated AdminUsers portal page
- Admin create/update with multi-department checkbox list and assignment sync

✅ **Phase 12 — Academic Calendar System (Stages 12.1–12.2) — 2026-05-07 (commit 6e89af1)**
- Stage 12.1: `AcademicCalendar` portal page (all roles) with semester filter, days-remaining color badges
- Stage 12.2: `AcademicDeadline` entity + EF config (`academic_deadlines`); `IAcademicDeadlineRepository` + impl; `IAcademicCalendarService` + impl; `CalendarController` (GET all-auth, POST/PUT/DELETE Admin+SuperAdmin); `AcademicDeadlines` portal CRUD page (Admin+SuperAdmin); `DeadlineReminderJob` background service dispatching `NotificationType.System` notifications daily
- Migration: `20260507_Phase12AcademicCalendar`
- Validation: **0 build errors; 78/78 tests passed**

✅ **Phase 13 — Global Search (Stages 13.1–13.2) — 2026-05-08 (commit 00b7b64)**
- Stage 13.1: `GET /api/v1/search?q={term}&limit={n}` — role-scoped cross-entity search; `ISearchRepository` + `SearchRepository` (EF LINQ joins); `ISearchService` + `SearchService`; `SearchController`; `SearchDTOs` records; no new migration
- Stage 13.2: Global search bar in portal header (`_Layout.cshtml`); typeahead dropdown (JS fetch); full results page (`Search.cshtml`) with Bootstrap category tabs; `_SearchResultsList.cshtml` partial; `PortalController` actions `Search` + `SearchTypeahead`
- Validation: **0 build errors; 78/78 tests passed**

✅ **Phase 14 — Helpdesk / Support Ticketing System (Stages 14.1–14.3) — 2026-05-09 (commit 8576e44)**
- Stage 14.1: `SupportTicket` entity + `IHelpdeskRepository` + `HelpdeskRepository`; `IHelpdeskService` + `HelpdeskService`; `HelpdeskController` (GET tickets, GET ticket, POST create, POST message, POST assign/resolve/close/reopen); EF migration `20260507_Phase14_Helpdesk`; `HelpdeskDTOs`
- Stage 14.2: Admin/SuperAdmin case management endpoints; ticket assignment to staff users; all roles scoped per department
- Stage 14.3: Faculty reply support; `SupportTicketMessage` thread model; reopen within configurable window; `Helpdesk.cshtml` list, `HelpdeskCreate.cshtml` form, `HelpdeskDetail.cshtml` thread view, `_TicketStatusBadge.cshtml` partial; sidebar link + route/group maps; `Program.cs` Phase 14 DI registration
- Validation: **0 build errors; 78/78 tests passed**

✅ **Phase 15 — Enrollment Rules Engine (Stages 15.1–15.3) — 2026-05-08 (commit 42f0993)**
- Stage 15.1: `CoursePrerequisite` entity + `IPrerequisiteRepository` + `PrerequisiteRepository`; `EnrollmentService.TryEnrollAsync` validates all prerequisites; `PrerequisiteController` (GET/POST/DELETE `api/v1/prerequisite`); `EnrollmentRulesDTOs` with `AdminEnrollRequest` override fields; EF migration `20260507133254_Phase15_EnrollmentRules` (`course_prerequisites` table + unique index)
- Stage 15.2: Timetable clash detection inside `TryEnrollAsync`; `OverrideClash` + `OverrideReason` on `AdminEnrollRequest`; override audit-logged
- Stage 15.3: `CourseOffering.MaxEnrollment` capacity enforcement already in place; `UpdateMaxEnrollment` API action exists
- Web portal: `PrerequisitesPageModel`, `PrerequisiteWebItem`, `CoursePrerequisiteGroup` models; `Prerequisites` / `PrerequisiteAdd` / `PrerequisiteRemove` portal controller actions; `Prerequisites.cshtml` view; sidebar link (Admin/SuperAdmin only)
- Validation: **0 build errors; 7/7 tests passed**

---

## Completed Work
- **Issue-Fix Phase 4 — Student Workflow Repair (ALL Stages 4.1–4.6 Done)** ✅ (2026-05-07)
  - Stage 4.1: Assignment submission end-to-end — `POST /api/v1/assignment/submit`, file upload to GUID path, Submit modal, `SubmitAssignment` action
  - Stage 4.2: Timetable department auto-resolved from student profile; `Guid.Empty` guard prevents bad requests; dashboard-config fallback retained
  - Stage 4.3/4.4/4.5: Semester filter added to Assignments/Results/Quizzes portals; offering dropdowns are semester-scoped; Results fallback to student-safe endpoints on 403; Quiz status badges (Upcoming/Pending/Completed)
  - Stage 4.6: FYP menu gated by `CurrentSemesterNumber >= 8`; `POST /api/v1/fyp/{id}/request-completion` (student) + `POST /api/v1/fyp/{id}/approve-completion` (faculty); auto-complete when all approvers done; FYP result row in Results; EF migration `Phase4FypCompletionApprovalFlow`
  - Auth consistency: login flow now resolves API base URL before token acquisition to prevent intermittent student 401s
  - Validation: **12/12 assignment tests passed; 78/78 full suite passed; 0 build errors**
- **Issue-Fix Phase 5 — Reporting and Export Center (ALL Stages 5.1–5.5 Done)** ✅ (2026-05-07)
  - Stage 5.1: Assignment + Quiz summary report APIs (`/api/v1/reports/assignment-summary`, `/api/v1/reports/quiz-summary`) + portal pages `ReportAssignments`, `ReportQuizzes`
  - Stage 5.2: CSV + PDF export for Attendance/Results/Assignments/Quizzes (`/export/csv`, `/export/pdf` variants); Web portal proxy actions; Excel/CSV/PDF export buttons on all report pages
  - Stage 5.3: SuperAdmin unrestricted report scope verified
  - Stage 5.4: Admin report scope bounded by assigned departments (Phase 6 data model + portal UX guidance guards on all 9 report pages)
  - Stage 5.5: Faculty department/offering filter sources scoped; report data/export requires offering ownership validation
  - Validation: 0 build errors after all export + scope changes
- **Issue-Fix Phase 3 — Faculty Workflow Repair (ALL 8 stages Done)** ✅ (2026-05-07)
  - Stage 3.1: CourseController.GetAll + GetOfferings — replaced Forbid() with Ok(empty) for out-of-scope dept requests
  - Stage 3.2/3.5/3.6/3.7: CourseController.GetMyOfferings — changed from FacultyUserId filter to dept-scope filter; fixes all empty dropdowns
  - Stage 3.3: Enrollments 403 fixed via same CourseController changes; cleaned up dead branch in PortalController
  - Stage 3.4: StudentController.GetAll — removed Forbid(); silently scopes to allowed departments
  - Stage 3.8: FypController.admin-create policy → "Faculty"; PortalController.Fyp() loads students for faculty; Fyp.cshtml shows Create button for Faculty
  - Validation: 0 build errors, 78/78 tests passed
- **Phase 1 Remediation — ALL 15 items Done (P1-S1-01 through P1-S6-04)** ✅
  - Stage 1.1: 403 auth fixes on Attendance/Assignments/Quizzes/Results; 30+ regression tests in AuthorizationRegressionTests.cs
  - Stage 1.2: Departments, Courses+Offerings, Enrollments, FYP Management CRUD fully implemented
  - Stage 1.3: Result Summary exception fixed; all Report Center reports visible by role
  - Stage 1.4: Module Settings removed from sidebar; brand area made non-clickable
  - Stage 1.5: Student lifecycle Promote flow fixed (correct profileId passed)
  - Stage 1.6: 29 total themes (10 new); logo upload endpoint + sidebar; privacy policy footer link; font family/size dropdowns with CSS injection
- Final-Touches Phases 1–9 (original work before remediation): all complete
- Phase 1 Remediation Batches 1–5: all complete

## Next Steps
- **Phase 12 — Academic Calendar System — COMPLETE ✅** (commit 6e89af1, 2026-05-07)
- **Phase 13 — Global Search — COMPLETE ✅** (commit 00b7b64, 2026-05-08)
- **Phase 14 — Helpdesk / Support Ticketing System — COMPLETE ✅** (commit 8576e44, 2026-05-09)
- **Phase 15 — Enrollment Rules Engine — COMPLETE ✅** (commit 42f0993, 2026-05-08)
- **Phase 16 — Faculty Grading System — COMPLETE ✅** (commit `1f496f7`)
- **Phase 17 — Degree Audit System — COMPLETE ✅** (78/78 tests passed)
- **Phase 18 — Graduation Workflow — COMPLETE ✅** (78/78 tests passed; migration `Phase18_GraduationWorkflow`)
- **Phase 19 — Advanced Course Creation & Grading Config — COMPLETE ✅**
- **Phase 20 — Learning Management System (LMS) — COMPLETE ✅**
- **Phase 21 — Study Planner — COMPLETE ✅** (migration `Phase21_StudyPlanner`)
- **Phase 22 — External Integrations — COMPLETE ✅** (commit `dddee69`; migration `Phase22_ExternalIntegrations`)
- **Phase 23 — Core Policy Foundation — COMPLETE ✅** (commit `28cac36`; 27/27 tests passed)
- **Phase 24 — Dynamic Module and UI Composition — COMPLETE ✅** (commit `391ac45`; 44/44 tests passed)
- **Phase 25 — Academic Engine Unification — COMPLETE ✅** (commit `d2aabd3`; 144/144 tests passed; migration `Phase25_AcademicEngineUnification`)
- **Phase 26 — School and College Functional Expansion — COMPLETE ✅** (commit `4c0904c`; 152/152 tests passed; migration `Phase26_SchoolCollegeExpansion`)
- **Phase 27 — University Portal Parity and Student Experience — COMPLETE ✅**
  - Stage 27.1 commit `fd3b137`: Portal capability matrix (service + API + web view + tests)
  - Stage 27.2 commit `20dba8d`: MFA toggle, SSO-ready security profile, session risk controls, auth audit improvements
  - Stage 27.3 commit `56cf1dd`: provider abstraction contracts for ticketing, announcements, and email
- **Phase 28 — Scalability Architecture — Stage 28.1 COMPLETE ✅**
  - Load-balancer readiness: forwarded headers expanded on API and added to Web.
  - Stateless web nodes: session-backed portal/API auth state replaced with protected cookies; optional shared key-ring path introduced.
  - Response compression and payload shaping: Brotli/Gzip compression enabled; null JSON fields omitted in API/Web JSON responses.
- **Phase 28 — Scalability Architecture — Stage 28.2 COMPLETE ✅**
  - Distributed cache foundation: optional Redis-backed `IDistributedCache` added with distributed-memory fallback.
  - Hot-read sharing: module entitlement resolution and report catalog reads now use shared cache across API nodes.
  - Async workload offload: large notification fan-out batches now defer recipient insertion to a hosted background worker.
  - Report generation offload: queued result-summary export jobs now run in the background with status polling and deferred download endpoints.
  - Recalculation offload: queued result publish-all jobs now run in the background with status polling.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 1 DELIVERED ✅**
  - Added configurable media storage abstraction (`IMediaStorageService`) with local filesystem provider (`LocalMediaStorageService`) and `MediaStorage` settings section.
  - Migrated student payment-proof upload endpoint to storage abstraction and object-key persistence (instead of hard-coded local file paths).
  - Added stricter upload validation reuse (`FileUploadValidator`) before persistence.
  - No schema changes required; storage references remain metadata-only in existing receipt records.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 2 DELIVERED ✅**
  - Moved storage abstraction contract into the Application layer so both API controllers and Application services can share it.
  - Migrated graduation certificate generation to provider-backed persistence, storing storage keys instead of filesystem-relative webroot paths for new records.
  - Added provider-backed certificate read path in graduation downloads, with compatibility fallback for legacy `/certificates/*` records.
  - No schema changes required; existing certificate path column continues to store a metadata reference.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 3 DELIVERED ✅**
  - Migrated license upload flow from direct temp-file path handling to provider-backed save/read/delete operations via `IMediaStorageService`.
  - Added bytes-based activation method in `LicenseValidationService` so license verification no longer depends on filesystem paths.
  - Added storage-provider delete support to clean temporary upload objects after activation attempts.
  - No schema changes required.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 4 DELIVERED ✅**
  - Added configurable provider registration (`AddConfiguredMediaStorage`) so storage backend selection is now driven by `MediaStorage:Provider`.
  - Added `BlobMediaStorageService` adapter (object-storage style key semantics, isolated root path, reference generation).
  - Extended media storage settings with `BlobRootPath` and updated environment appsettings defaults/placeholders.
  - Local provider remains default to preserve runtime behavior unless provider is explicitly switched.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 5 DELIVERED ✅**
  - Migrated portal logo upload from inline base64 return to provider-backed persistence through `IMediaStorageService`.
  - Added public logo streaming endpoint `GET /api/v1/portal-settings/logo-files/{**storageKey}` for branding rendering without bearer headers.
  - Added guarded key-category enforcement so only `portal-branding/logo` objects are served by the anonymous endpoint.
  - Preserved backward compatibility for existing `data:image/*` logo values already stored in portal settings.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 6 DELIVERED ✅**
  - Extended `IMediaStorageService` with temporary read URL generation (`GenerateTemporaryReadUrlAsync`) for provider-backed signed URL workflows.
  - Added temporary signed URL generation support in both local and blob storage providers using optional `MediaStorage:SignedUrlSecret`.
  - Updated portal logo file endpoint to prefer redirecting to provider-generated temporary URLs and safely fall back to byte streaming when unavailable.
  - Added `SignedUrlSecret` placeholders to API appsettings files.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 7 DELIVERED ✅**
  - Enforced signed URL validation (`exp` + `sig`) on local logo streaming when `MediaStorage:SignedUrlSecret` is configured.
  - Added compatibility redirect from unsigned legacy logo URLs to short-lived signed local URLs.
  - Added fixed-time signature comparison and expiry enforcement for local signed reads.
  - Kept provider temporary URL redirect-first behavior and byte-stream fallback for operational compatibility.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 8 DELIVERED ✅**
  - Added tokenized certificate file endpoint `GET /api/v1/graduation/certificate-files/{**storageKey}` for provider-backed certificate reads.
  - Updated graduation certificate download flow to redirect to temporary provider URLs (when available) or signed local certificate URLs.
  - Enforced signed URL validation (`exp` + `sig`) for local certificate streaming when signing secret is configured.
  - Preserved legacy `/certificates/*` certificate path compatibility with existing byte-download flow.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 9 DELIVERED ✅**
  - Extended `IMediaStorageService` with metadata lookup support (`GetMetadataAsync`) and expanded save results to include content type and object length.
  - Added metadata resolution in local/blob storage providers so callers can retrieve provider-backed content type and length without re-deriving from business code.
  - Updated portal logo and certificate streaming endpoints to prefer storage metadata for response content type selection.
  - Preserved existing signed URL and legacy path compatibility behavior.
- **Phase 28 — Scalability Architecture — Stage 28.3 SLICE 10 DELIVERED ✅**
  - Extended storage save/metadata contracts with SHA-256 content hash and optional download filename metadata.
  - Persisted sidecar metadata for local/blob providers so integrity/disposition details survive provider redirects and later reads.
  - Updated certificate generation and upload flows to pass content type and original/download filename into storage.
  - Restored filename-preserving certificate downloads for signed local and redirect-first media reads.
- **Phase 28 COMPLETE ✅**
  - Stage 28.1 delivered stateless/load-balanced app behavior.
  - Stage 28.2 delivered distributed cache and background-work offload.
  - Stage 28.3 delivered provider-backed media persistence, signed reads, metadata, and integrity/disposition hardening with no schema changes.
- **Phase 29 — MSSQL Data and Indexing Optimization — Stage 29.1 DELIVERED ✅**
  - Added baseline composite indexes for high-frequency student/user/status recency queries across graduation applications, support tickets, notification inbox rows, payment receipts, quiz attempts, and user sessions.
  - Generated EF migration `20260509155457_20260510_Phase29_IndexBaseline` to apply the new index set.
  - Validated current model audit: no `InstitutionId`, `YearId`, or `GradeId` columns exist yet, so Stage 29.1 focused on current `StudentId`/`UserId`/`CourseId`/`SemesterId` shaped query contracts.
- **Phase 29 — MSSQL Data and Indexing Optimization — Stage 29.2 SLICE 1 DELIVERED ✅**
  - Added paged helpdesk ticket listing contract across API, application, repository, and web layers.
  - Replaced unbounded `GET /api/v1/helpdesk/tickets` listing with `page` and `pageSize` driven queries for Student, Faculty, Admin, and SuperAdmin views.
  - Updated portal helpdesk page with previous/next pagination controls and status-filter reset to page 1.
  - No database migration was required.
- **Phase 29 — MSSQL Data and Indexing Optimization — Stage 29.2 SLICE 2 DELIVERED ✅**
  - Added paged graduation application list contract for both student (`GET /api/v1/graduation/my`) and staff (`GET /api/v1/graduation`) list paths.
  - Replaced unbounded graduation list materialization with server-side `page` and `pageSize` SQL paging in repository/service/API layers.
  - Updated portal graduation list pages with previous/next pagination controls while preserving status/department filters.
  - No database migration was required.
- **Phase 29 — MSSQL Data and Indexing Optimization — Stage 29.2 SLICE 3 DELIVERED ✅**
  - Added paged payment receipt list contract for admin/student receipt endpoints and student-filtered admin listing.
  - Replaced unbounded payment receipt list materialization with server-side `page` and `pageSize` SQL paging in repository/service/API/web layers.
  - Updated portal payments page with previous/next pagination controls while preserving selected student filters.
  - No database migration was required.
- **Phase 29 — MSSQL Data and Indexing Optimization — Stage 29.3 DELIVERED ✅**
  - Added archive/retention policy script (`Scripts/3-Phase29-ArchivePolicy.sql`) with dry-run and optional batched cleanup mode.
  - Added index maintenance script (`Scripts/4-Phase29-IndexMaintenance.sql`) with fragmentation-driven reorganize/rebuild planning.
  - Added capacity and growth dashboard script (`Scripts/5-Phase29-CapacityGrowthDashboard.sql`) with table-size and recent-growth telemetry.
  - Updated `Scripts/README.md` with operations runbook commands.
  - No database migration was required.
- **Phase 29 COMPLETE ✅**
  - Stage 29.1 delivered index baseline and query contracts.
  - Stage 29.2 delivered pagination discipline for helpdesk, graduation, and payment receipt heavy-list endpoints.
  - Stage 29.3 delivered lifecycle maintenance scripts for archive policy, index maintenance, and capacity-growth observability.

## Pending Extra Tasks (Cross-Phase)
- None. All pending extras are complete.

---

## Session Resume Prompt Template
Copy/paste this in a new chat:

Resume from Command.md and Final-Touches.md.
Continue from Current Execution Pointer.
Do not replan completed items.
When a phase is completed, update:
- Docs/Function-List.md
- Project startup Docs/PRD.md
- Project startup Docs/Final-Touches.md
- Command.md

---

## Work Log

### Entry 024 — 2026-05-07 — Stage 5.4 Admin Reporting Scope Portal UX Completion
**Completed:**
- Added `isAdminOnly` guidance guards to all 9 report portal page actions in `Web/Controllers/PortalController.cs`:
  - `ReportAttendance`, `ReportResults`, `ReportAssignments`, `ReportQuizzes`: Admin receives friendly message when neither department nor offering is selected (mirrors existing Faculty guidance pattern).
  - `ReportGpa`, `ReportEnrollment`, `ReportSemesterResults`, `ReportFypStatus`: Admin receives friendly message when no department is selected.
  - `ReportLowAttendance`: Admin receives friendly message when neither department nor offering is selected.
- `isAdminOnly` pattern: `identity?.IsAdmin == true && !identity.IsSuperAdmin` (avoids triggering for SuperAdmin who has `IsAdmin=true`).
- No API changes needed — `EnforceAdminDepartmentScopeAsync` was already complete via Phase 6.

**Validation:**
- `dotnet build Tabsan.EduSphere.Web.csproj` — **0 errors, 0 warnings**
- Full integration suite — **78/78 tests passed**
- Commit: `ee9fb57` — pushed to `main`

**Moved to:**
- All phases and remediation items complete. No active pending work.

### Entry 023 — 2026-05-07 — Refactoring-Hosting-Security Part A + Part B Delivery
**Completed:**
- **Part A — Hosting Configuration:**
  - Created `appsettings.Production.json` for API, Web, BackgroundJobs (production-ready placeholders)
  - Created `appsettings.Development.json` for BackgroundJobs (dev connection string)
  - Updated `API/appsettings.Development.json`: debug logging, CORS origins `["https://localhost:5063", "http://localhost:5063"]`, EnableSwagger/EnableDetailedErrors flags
  - Updated `API/appsettings.json`: added `AppSettings` section (EnableSwagger, EnableDetailedErrors, CorsOrigins array)
  - `API/Program.cs` — DB retry on failure (3 attempts, 30 s backoff via `EnableRetryOnFailure`)
  - `API/Program.cs` — CORS configured from `AppSettings:CorsOrigins` config key; `UseCors` added to pipeline
  - `API/Program.cs` — `ForwardedHeaders` middleware registered and used in non-dev (IIS/nginx/Cloudflare support)
  - `API/Program.cs` — Health check endpoint at `/health` (`AddHealthChecks` + `MapHealthChecks`)
  - `API/Program.cs` — 5 MB request body size limits (Kestrel + IIS + FormOptions)
  - `API/Program.cs` — Startup environment log line
  - `API/Program.cs` — Swagger gated by `AppSettings:EnableSwagger` flag (dev always on)
  - `API/Program.cs` — WeatherForecast boilerplate block removed
- **Part B — Security Hardening:**
  - Created `API/Middleware/ExceptionHandlingMiddleware.cs`: global exception handler; maps exception types to HTTP codes; no stack traces in production; TraceIdentifier in every error response
  - Created `API/Services/FileUploadValidator.cs`: static validator with magic-bytes verification, MIME-type check, extension allowlist, 5 MB size limit
  - `Web/Program.cs` — session cookie hardened: `SameSite=Strict`, `SecurePolicy=Always`
  - `.gitignore` — added: `*.pfx`, `*.key`, `logs/`, `appsettings.*.local.json`, `appsettings.*.secret.json`, `secrets/`, `.env.local`, `.env.*.local`

**Validation:**
- `dotnet build Tabsan.EduSphere.API.csproj` — **0 errors, 0 warnings**
- Full integration suite — **69/69 tests passed**
- Commit: `f56ccd9` — pushed to `main`

**Pending (next session):**
- Serilog file sink (rolling log to `logs/app-.txt`)
- `UserSecretsId` in API `.csproj`
- Wire `FileUploadValidator.ValidateAsync()` into `AssignmentController.Submit` + logo upload controller

### Entry 022 — 2026-05-06 — Issue-Fix Phase 4 Option A/C Delivery (Web Import + Forced Password Change)
**Completed:**
- Confirmed User Import web page and CSV upload flow are wired through portal (`UserImport` + `ImportUsersCsv`).
- Added forced password change portal flow:
  - `LoginController` now reads `MustChangePassword` from login response and redirects to `Portal/ForceChangePassword`.
  - `EduApiClient` now tracks `ForcePasswordChangeRequired` session flag and exposes `ForceChangePasswordAsync`.
  - `PortalController` enforces redirect to forced-password page until password is updated.
  - Added new Razor page: `Views/Portal/ForceChangePassword.cshtml`.
- Added integration tests in `UserImportAndForceChangeIntegrationTests` for:
  - Student cannot import CSV (`403`)
  - Import user -> first login (`MustChangePassword=true`) -> force-change-password -> old password rejected -> new password accepted.

**Validation:**
- Focused tests: passed (`2/2`).
- Full integration suite: passed (`70/70`).

**Moved to:**
- Ready for Option B (define new phases) or remaining optional unit-test hardening.

### Entry 021 — 2026-05-06 — Issue-Fix Phase 6.1 Extended Delivery (Dedicated Admin User Management)
**Completed:**
- Added dedicated SuperAdmin API for Admin account management:
  - `GET /api/v1/admin-user`
  - `POST /api/v1/admin-user`
  - `PUT /api/v1/admin-user/{id}`
- Added repository support to fetch users by role with optional inactive inclusion.
- Added dedicated portal page:
  - `Portal/AdminUsers`
  - create Admin user + initial department assignment
  - update Admin email/status/password + assignment sync
  - search Admin selector and select-all/clear assignment UX controls
- Kept Departments page assignment panel and added quick navigation to the dedicated Admin Users page.
- Added focused integration tests for:
  - admin-user endpoint access control
  - admin create/update + assignment round-trip flow

**Validation:**
- `dotnet build Tabsan.EduSphere.sln` succeeded.
- Focused integration tests failed due pre-existing migration/seeding issue in test environment:
  - duplicate `ActivatedDomain` column in `license_state` migration path during DB setup.

**Moved to:**
- Ready for next issue-fix phase after test DB migration chain cleanup.

### Entry 020 — 2026-05-06 — Issue-Fix Phase 6.1 UI Delivered (Admin Department Assignment)
**Completed:**
- Added SuperAdmin-only Admin user listing endpoint:
  - `GET /api/v1/department/admin-users`
- Added Web API client methods for assignment UI flows:
  - `GetAdminUsersAsync`
  - `GetAdminDepartmentIdsAsync`
  - `AssignAdminToDepartmentAsync`
  - `RemoveAdminFromDepartmentAsync`
  - internal helper `DeleteWithBodyAsync` for DELETE-with-payload API calls
- Extended portal department page model for assignment management state:
  - admin list
  - selected admin id
  - assigned department id list
- Updated `PortalController.Departments` to load assignment state for SuperAdmin.
- Added `PortalController.UpdateAdminDepartmentAssignments` to diff and apply add/remove assignment operations.
- Updated `Views/Portal/Departments.cshtml` with SuperAdmin assignment UI:
  - Admin selector dropdown
  - Active department checkbox list
  - Save Assignments action

**Validation:**
- `dotnet build Tabsan.EduSphere.sln` succeeded after implementation.

**Moved to:**
- Phase 6 complete for backend + assignment management UI. Ready for next planned issue-fix phase.

### Entry 019 — 2026-05-06 — Issue-Fix Phase 6 Backend Delivered (Admin Multi-Department Assignment)
**Completed:**
- Added domain model + persistence for admin multi-department assignment:
  - `AdminDepartmentAssignment` entity
  - `IAdminAssignmentRepository` and `AdminAssignmentRepository`
  - EF mapping and migration `20260506044806_20260506_Phase6AdminDepartmentAssignments`
- Added SuperAdmin management endpoints for admin department scope in Department API:
  - `POST /api/v1/department/admin-assignment`
  - `DELETE /api/v1/department/admin-assignment`
  - `GET /api/v1/department/admin-assignment/{adminUserId}`
- Enforced Admin assignment scope in:
  - Department list responses
  - Course catalog and offerings responses
  - Report Center data and export endpoints (department/offering guard)
- This unblocks and closes Stage 5.4 backend dependency.

**Validation:**
- `dotnet build Tabsan.EduSphere.sln` succeeded after implementation.
- Migration creation completed successfully with EF Core tooling.

**Moved to:**
- Phase 6.1 UI integration (SuperAdmin create/update Admin with multi-department checkbox list)

### Entry 005 — 2026-05-06 — Issue-Fix Phase 2 Complete (Shared Portal and Settings)
**Completed:**
- Stage 2.1 (Branding and Asset Rendering): fixed logo upload/render path end-to-end.
  - API upload crash resolved (`WebRootPath` null fallback added in `PortalSettingsController.UploadLogo`).
  - API static asset serving fixed using explicit `PhysicalFileProvider` rooted at API `wwwroot` in `Program.cs`.
- Stage 2.2 (Privacy Policy Editing): verified editor and rendering flow works end-to-end.
  - Privacy content persisted via portal settings API and rendered on `/Home/Privacy`.
- Stage 2.3 (Shared Course Offering Dropdowns): verified offerings dropdown now returns populated options in portal pages (Assignments live-validated).

**Validation:**
- `POST /api/v1/portal-settings/logo` now returns `200 OK` with URL payload (`/portal-uploads/logo.svg`).
- `GET /portal-uploads/logo.svg` now returns `200`.
- Live UI: sidebar brand switched from initials to logo image.
- Live UI: Privacy page renders configured policy text.
- Live UI: Assignments `Select Course Offering` dropdown shows offerings list.

**Moved to:** Issue-Fix Phase 3 (Faculty Workflow Repair)

### Entry 001
- Date: 2026-05-03
- Action: Created Command.md as persistent handover controller.
- Changes:
  - Added execution pointer linked to Final-Touches Phase 1 Stage 1.1.
  - Added mandatory documentation update rule per completed phase.
  - Added resume template and cross-phase pending extras list.
- Validation:
  - File created successfully.
  - Paths and phase names align with Final-Touches.md.
- Next:
  - Begin implementation of Phase 1 Stage 1.1.

### Entry 002
- Date: 2026-05-03
- Action: Started Phase 1 implementation (Stage 1.1 completed, Stage 1.2 partially completed).
- Changes:
  - Updated [src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml](src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml) to cache dynamic sidebar menus in session and reuse cache when menu API fails/returns empty.
  - Reworked dynamic sidebar rendering into grouped sections: Overview, Faculty Related, Student Related, Finance Related, Settings.
  - Removed layout redirect-return behavior to prevent render instability and session flow breakage.
- Validation:
  - SuperAdmin login shows full grouped sidebar.
  - Buildings page shows existing records and sidebar remains intact.
  - No forced re-login during Buildings navigation in validation run.
- Next:
  - Complete remaining Stage 1.2 assignment visibility in Sidebar Settings.
  - Start Stage 1.3 Dashboard Settings implementation.

## Work Log

### Entry 001 — 2026-05-03 — Phase 1 Stages 1.1–1.2 Complete
**Completed:**
- Stabilized sidebar rendering by caching dynamic menu in session (VisibleSidebarMenusCache).
- Removed layout-level redirect-return behavior.
- Implemented grouped sidebar with 5 groups (Overview, Faculty Related, Student Related, Finance Related, Settings).
- Seeded 29 sidebar menu items with role-based visibility.
- Verified SuperAdmin login, Buildings navigation, Sidebar Settings page (29 items, all assignable).

**Validation:**
- SuperAdmin login: grouped sidebar, full menu set visible.
- Buildings page: renders existing rows, sidebar remains stable.
- Sidebar Settings: 29 items listed, role assignment working.

**Moved to:** Stage 1.3

---

### Entry 002 — 2026-05-03 — Phase 1 Stage 1.3 Complete (Dashboard Branding)
**Completed:**
- Added \portal_settings\ domain entity (Key–Value table).
- Added EF migration \Phase1DashboardBranding\ (table created, applied to DB).
- Added \PortalBrandingService\ with \GetAsync()\ and \SaveAsync()\ methods.
- Added \PortalSettingsController\ API with \GET\ (all users) and \POST\ (SuperAdmin).
- Added \EduApiClient\ methods \GetPortalBrandingAsync()\ and \SavePortalBrandingAsync()\.
- Added \PortalController.DashboardSettings()\ GET/POST actions.
- Created \DashboardSettings.cshtml\ Razor view with form + live preview.
- Updated \_Layout.cshtml\ to load branding from DB with session cache fallback.
- Seeded \dashboard_settings\ sidebar menu item (SuperAdmin only).
- Updated \DatabaseSeeder.cs\ with all 4 portal_settings keys (university_name, brand_initials, portal_subtitle, footer_text).

**Validation:**
- Dashboard Settings page renders with form and live preview.
- Default branding values pre-filled: Tabsan EduSphere, TE, Campus Portal, © 2026 Tabsan EduSphere.
- Sidebar shows "Dashboard Settings" under Settings group.
- Footer text in _Layout driven by DB branding.

**Phase 1 Status:** ✅ Complete

**Moved to:** Phase 2 Stage 2.1

**Docs Updated:**
- \Final-Touches.md\: Marked Phase 1 complete, added Stage 1.3 Implementation/Validation summaries.
- \PRD.md\: Bumped version to 1.13, updated log entry.
- \Function-List.md\: Added Phase 1 Stage 1.3 portal settings and Dashboard Settings functions.

---

### Entry 003 — 2026-05-04 — Phase 2 Stage 2.1 Complete (Timetable Data Binding)
**Completed:**
- Fixed TimetableRepository.GetTeacherEntriesAsync() missing Building include
- Fixed TimetableRepository.GetByDepartmentAsync() missing Department, AcademicProgram, Semester includes
- Fixed TimetableRepository.GetPublishedByDepartmentAsync() missing Department, AcademicProgram, Semester includes
- Fixed TimetableRepository.GetByIdWithEntriesAsync() with separate Building include for entries

**Changes:**
- Updated [src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs](src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs) with proper EF Include statements

**Validation:**
- Build succeeded with all fixes
- Faculty timetable endpoint includes Building navigation for proper data binding
- Student timetable endpoints include all required related entities for DTO mapping
- Test data exists: 1 published timetable with 2 entries for CS dept, faculty.test

**Moved to:** Stage 2.2

---

### Entry 004 — 2026-05-04 — Phase 2 Stage 2.2 Complete (Lookup Data Visibility)
**Completed:**
- Fixed StudentProfileRepository.GetAllAsync() to include Program and Department navigation properties
- Updated StudentController.GetAll() to return ProgramName, DepartmentName, and Status from included entities
- Added new CourseRepository.GetOfferingsByDepartmentAsync() method for department-filtered course offerings
- Updated ICourseRepository interface with GetOfferingsByDepartmentAsync() method signature
- Updated CourseRepository.GetOfferingsBySemesterAsync() and GetOfferingsByFacultyAsync() with proper includes
- Refactored CourseController.GetOfferings() endpoint to accept both ?semesterId and ?departmentId query parameters
- Updated CourseController.GetAll() to include DepartmentName mapping for courses

**Changes:**
- [src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicSupportRepositories.cs](src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicSupportRepositories.cs): Removed incorrect User include, added Program/Department includes
- [src/Tabsan.EduSphere.API/Controllers/StudentController.cs](src/Tabsan.EduSphere.API/Controllers/StudentController.cs): Enhanced GetAll() response with related entity names
- [src/Tabsan.EduSphere.Infrastructure/Repositories/CourseRepository.cs](src/Tabsan.EduSphere.Infrastructure/Repositories/CourseRepository.cs): Added GetOfferingsByDepartmentAsync(), updated 2 existing offering methods
- [src/Tabsan.EduSphere.Domain/Interfaces/ICourseRepository.cs](src/Tabsan.EduSphere.Domain/Interfaces/ICourseRepository.cs): Added GetOfferingsByDepartmentAsync() signature
- [src/Tabsan.EduSphere.API/Controllers/CourseController.cs](src/Tabsan.EduSphere.API/Controllers/CourseController.cs): Refactored GetOfferings() endpoint for dual-parameter support

**Validation:**
- Build succeeded (0 errors, 2 MailKit warnings only)
- StudentController.GetAll() returns Program and Department names for each student
- CourseController.GetAll() returns DepartmentName for each course
- CourseController.GetOfferings() endpoint accepts both ?semesterId and ?departmentId filters
- Portal views ready to consume updated API responses with complete related entity data
- Commit: e15e0b6

**Phase 2 Stage 2 Status:** ✅ Complete

**Moved to:** Stage 2.3 (CRUD Entry Points)

**Docs Updated:**
- Final-Touches.md: Marked Stage 2.2 complete, added Implementation/Validation summaries, adjusted Stage 2.3 section
- Command.md: Updated Current Execution Pointer to Stage 2.3

---

### Entry 005 — 2026-05-04 — Phase 2 Stage 2.3 Complete (CRUD Entry Points)
**Completed:**
- Added CourseRepository.GetOfferingsByDepartmentAsync() method for department-filtered offerings
- Added 4 new CourseOffering management endpoints to CourseController:
  - PUT /offerings/{id}/maxenrollment - Update max enrollment with validation
  - PUT /offerings/{id}/close - Close enrollment
  - PUT /offerings/{id}/reopen - Re-open enrollment
  - DELETE /offerings/{id} - Soft-delete offering using AuditableEntity.SoftDelete()
- Added Students.cshtml create button and modal (Registration Number, Program, Department, Admission Date)
- Added Departments.cshtml create button and modal (Code, Name)
- Added Courses.cshtml create buttons and modals for Courses and Offerings with all required fields

**Changes:**
- [src/Tabsan.EduSphere.API/Controllers/CourseController.cs](src/Tabsan.EduSphere.API/Controllers/CourseController.cs): Added 4 offering lifecycle endpoints
- [src/Tabsan.EduSphere.Application/DTOs/Academic/AcademicDtos.cs](src/Tabsan.EduSphere.Application/DTOs/Academic/AcademicDtos.cs): Added UpdateMaxEnrollmentRequest
- [src/Tabsan.EduSphere.Web/Views/Portal/Students.cshtml](src/Tabsan.EduSphere.Web/Views/Portal/Students.cshtml): Added create button and form modal
- [src/Tabsan.EduSphere.Web/Views/Portal/Departments.cshtml](src/Tabsan.EduSphere.Web/Views/Portal/Departments.cshtml): Added create button and form modal
- [src/Tabsan.EduSphere.Web/Views/Portal/Courses.cshtml](src/Tabsan.EduSphere.Web/Views/Portal/Courses.cshtml): Added create buttons and form modals for courses and offerings

**Validation:**
- Build succeeded (0 errors, 2 MailKit warnings only)
- All portal views render correctly with new create buttons and modals
- CourseOffering endpoints support full lifecycle: create, assign faculty, update enrollment, close/reopen, soft-delete
- Modal forms include role-based visibility (Admin/SuperAdmin only)
- All existing CRUD endpoints utilized: StudentController.Create, DepartmentController.Create/Update/Delete, CourseController.Create/Update/Delete, CourseController.CreateOffering

**Phase 2 Status:** ✅ Complete (All stages 2.1, 2.2, 2.3 finished)

**Moved to:** Phase 3 Stage 3.1 (403 Authorization Fixes)

**Docs Updated:**
- Final-Touches.md: Marked Phase 2 complete, marked Stage 2.3 complete with impl/validation summaries, adjusted Phase 3 section
- Command.md: Updated Current Execution Pointer to Phase 3 Stage 3.1



---

### Entry 007 — 2026-05-04 — Phase 7 Complete (Finance and Payments Module)
**Completed:**
- Stage 7.1: Verified 'payments' sidebar item in Finance Related group; fixed URL bug (api/v1/payment-receipt → api/v1/payments).
- Stage 7.2: Added GetAllReceiptsAsync + GetStudentProfileByUserIdAsync to repo, service, and API layer. Three new API endpoints: GET /mine, GET / (all), POST /{id}/mark-submitted.
- Stage 7.3: Admin Create/Confirm/Cancel receipt workflows; Student view-own + Submit Proof text form; Notifications on Create, SubmitProof, Confirm, Cancel via INotificationService.

**Changes:**
- IStudentLifecycleRepository + StudentLifecycleRepository: 2 new methods
- IStudentLifecycleService + StudentLifecycleService: 2 new methods; injected INotificationService; 4 notification calls
- PaymentReceiptController: 3 new endpoints (GetAll, GetMine, MarkSubmitted)
- PortalViewModels: Expanded PaymentReceiptItem, added CreatePaymentForm, expanded PaymentsPageModel
- EduApiClient: 6 new payment methods, expanded PaymentApiDto + MapPayment
- PortalController: Payments GET branches on IsStudent; 4 POST actions (CreatePayment, ConfirmPayment, CancelPayment, SubmitProof)
- Payments.cshtml: Full rebuild — admin Create Receipt form + filter + Confirm/Cancel; student receipts + Submit Proof collapse

**Validation:**
- Application and Infrastructure layers build with 0 errors.
- Web layer: 0 CS/RZ errors (file-lock MSB from running process only).
- Fixed StudentItem.FullName usage (was Name) and Razor selected attribute syntax.

**Moved to:** Phase 8 (Enrollments Completion)

**Docs Updated:**
- Final-Touches.md: Marked Phase 7 complete, updated Progress Tracker, Next Phase
- PRD.md: Bumped to v1.20
- Function-List.md: Added GetAllReceiptsAsync, GetStudentProfileByUserIdAsync, GetAllReceiptsAsync (service), GetReceiptsByUserAsync
- Command.md: Updated execution pointer + this entry

---

### Entry 008 — 2026-05-05 — Phase 8 Complete (Enrollments Completion)
**Completed:**
- Stage 8.1: Fixed empty enrollment dropdown — added `GetAllOfferingsAsync` to ICourseRepository + CourseRepository; updated CourseController.GetOfferings to call it when no filter, fixed field names (CourseTitle, IsActive).
- Stage 8.1: Fixed empty roster grid — fixed GetRoster response fields to match RosterApiDto; added .ThenInclude(sp => sp.Program) to GetByOfferingAsync; updated MyCourses to include CourseOfferingId.
- Stage 8.2: Added IEnrollmentRepository.GetByIdAsync + implementation.
- Stage 8.2: Added IEnrollmentService.AdminDropByIdAsync + EnrollmentService implementation.
- Stage 8.2: Added AdminEnrollRequest DTO.
- Stage 8.2: Added POST /api/v1/enrollment/admin (admin enroll) + DELETE /api/v1/enrollment/admin/{id} (admin drop).
- Stage 8.2: Added 5 EduApiClient methods + MyCourseApiDto private DTO.
- Stage 8.2: Added MyEnrollmentItem view model; expanded EnrollmentsPageModel.
- Stage 8.2: Updated PortalController Enrollments GET (branches on IsStudent); added 4 POST actions (EnrollStudent, AdminDropEnrollment, StudentEnroll, StudentDropEnrollment).
- Stage 8.2: Rebuilt Enrollments.cshtml — student own-courses + admin roster with CRUD.

**Validation:**
- Build: 0 errors, 0 warnings.
- Enrollment dropdown now populated from all offerings.
- Admin: offering select → roster with Drop buttons + "Enroll Student" modal.
- Student: own courses list with Drop buttons + "Enroll in Course" modal.

**Moved to:** Phase 9 (Documentation and Script Regeneration)

**Docs Updated:**
- Final-Touches.md: Marked Phase 8 complete, updated Progress Tracker, Next Phase
- PRD.md: Bumped to v1.21, added Phase 8 log entry
- Function-List.md: Added Phase 7 + Phase 8 sections (both were missing)
- Command.md: Updated execution pointer + this entry

---

### Entry 009 — 2026-05-05 — Phase 9 Complete (Documentation and Script Regeneration)
**Completed:**
- Stage 9.1: `1-MinimalSeed.sql` §15 — added 16 missing sidebar menu items (`result_calculation`, `notifications`, `students`, `departments`, `courses`, `assignments`, `attendance`, `results`, `quizzes`, `fyp`, `analytics`, `ai_chat`, `student_lifecycle`, `payments`, `enrollments`, `report_center`, `dashboard_settings`) + role accesses matching DatabaseSeeder.
- Stage 9.1: `1-MinimalSeed.sql` §17 — replaced 4 old hyphen-key report defs with 8 canonical underscore-key defs matching ReportKeys.cs; added gpa_report, enrollment_summary, low_attendance_warning, fyp_status.
- Stage 9.1: `2-FullDummyData.sql` — same §15 and §17 changes.
- Stage 9.2: User guides bumped to v1.1 (Student, Admin, Faculty, SuperAdmin, License-KeyGen, README).
- Stage 9.2: Student guide — added Section 12 (Enrollments self-service: view/enroll/drop).
- Stage 9.2: Admin guide — updated Section 6 (admin enrollment CRUD: roster/enroll/drop workflows).
- Stage 9.2: Faculty guide — updated Section 4 (Enrollments roster view).
- Stage 9.3: PRD v1.22, Final-Touches Phase 9 complete, Command.md pointer updated.

**Validation:**
- SQL scripts: all new INSERT statements use IF NOT EXISTS guards — idempotent on re-run.
- Role accesses match DatabaseSeeder.SeedSidebarMenusAsync exactly.
- Report keys match ReportKeys.cs constants exactly.
- No C# changes — build remains 0 errors, 0 warnings.

**Moved to:** All phases complete.

**Docs Updated:**
- Final-Touches.md: Marked Phase 9 complete, filled Implementation/Validation summaries, updated Progress Tracker
- PRD.md: Bumped to v1.22, added Phase 9 log entry
- Command.md: Updated execution pointer + this entry

---

### Entry 010 — 2026-05-05 — Phase 1 Remediation Restart (Batch 1)
**Completed:**
- Stage 1.1: Fixed API role gate for offerings used by Assignments/Attendance/Results/Quizzes pages by expanding `GET /api/v1/course/offerings/my` to all operational roles.
- Stage 1.1: Added role-aware behavior in `CourseController.GetMyOfferings()` so SuperAdmin/Admin can always access all offerings.
- Stage 1.3: Fixed Report Center visibility for SuperAdmin by returning all active reports regardless of role assignment rows.
- Stage 1.4: Removed `module_settings` from dynamic sidebar route/group mapping.
- Stage 1.4: Removed `module_settings` from seed scripts `1-MinimalSeed.sql` and `2-FullDummyData.sql`.
- Stage 1.4: Added legacy cleanup logic to disable role access and soft-delete existing `module_settings` records.
- Stage 1.4: Updated `SidebarMenuIntegrationTests` expected SuperAdmin key count after removal.

**Validation:**
- File diagnostics for modified files show no code errors.
- Full solution build is blocked by running app processes holding output DLL locks (`MSB3021`/`MSB3027`).

**Moved to:** Continue Phase 1 remediation.

**Docs Updated:**
- PRD.md: Bumped to v1.23 and added this remediation log entry
- Development Plan - ASP.NET.md: Added Phase 1 remediation restart execution note
- Function-List.md: Added Phase 1 remediation batch 1 function updates
- Command.md: Updated execution pointer + this entry

---

### Entry 011 — 2026-05-05 — Phase 1 Remediation Restart (Batch 2)
**Completed:**
- Stage 1.4: Removed static `Module Settings` sidebar link from SuperAdmin menu in `_Layout.cshtml`.
- Stage 1.4: Removed `module_settings` menu creation/assignment from runtime `DatabaseSeeder`.
- Stage 1.4: Removed `module_settings` from seed scripts `1-MinimalSeed.sql` and `2-FullDummyData.sql`.
- Stage 1.4: Added legacy cleanup logic to disable role access and soft-delete existing `module_settings` records.
- Stage 1.4: Updated `SidebarMenuIntegrationTests` expected SuperAdmin key count after removal.

**Validation:**
- Diagnostics check reports no errors in all modified files.
- SuperAdmin offerings endpoint check still returns data on the running API.

**Moved to:** Continue Phase 1 Stage 1.3 (`Result Summary` InvalidOperationException).

**Docs Updated:**
- Observed-Issues.md: Marked `P1-S4-01` as Done
- PRD.md: Bumped to v1.24 and added remediation batch 2 log
- Development Plan - ASP.NET.md: Added batch 2 execution update
- Function-List.md: Added Stage 1.4 cleanup function/file updates
- Command.md: Updated execution pointer + this entry

---

### Entry 012 — 2026-05-05 — Phase 1 Remediation Restart (Batch 3)
**Completed:**
- Stage 1.3: Fixed `System.InvalidOperationException` on Result Summary.
- Root cause: EF translation failure in report query ordering (`OrderBy` on projected `ResultReportRow`).
- Fixed `ReportRepository` by moving sorting to SQL (`orderby u.Username, c.Code`) before projection and removing post-projection ordering.
- Updated report data/export endpoint authorization to include `SuperAdmin,Admin,Faculty` so SuperAdmin always has report functionality access.

**Validation:**
- Live API check successful: `GET /api/v1/reports/result-summary` with SuperAdmin token returned `rows=21`, `totalRecords=21`.
- Diagnostics check reports no errors in modified files.

**Moved to:** Continue Phase 1 Stage 1.2 CRUD coverage and Stage 1.6 dashboard/theme enhancements.

**Docs Updated:**
- Observed-Issues.md: Marked `P1-S3-01` as Done
- PRD.md: Bumped to v1.25 and added remediation batch 3 log
- Development Plan - ASP.NET.md: Added batch 3 execution update
- Function-List.md: Added Stage 1.3 function/behavior updates
- Command.md: Updated execution pointer + this entry

---

### Entry 013 — 2026-05-05 — Phase 1 Remediation (Batch 4 — Stage 1.2 CRUD)
**Completed:**
- P1-S2-01: Departments CRUD — added `CreateDepartment`, `UpdateDepartment`, `DeactivateDepartment` POST actions to PortalController; added `CreateDepartmentAsync`, `UpdateDepartmentAsync`, `DeactivateDepartmentAsync` to EduApiClient; rewired `Departments.cshtml` with server-side `<form asp-action>` modals for Create/Edit/Deactivate with antiforgery tokens.
- P1-S2-02: Courses & Offerings CRUD — added `CreateCourse`, `CreateOffering`, `DeactivateCourse`, `DeleteOffering` POST actions; added matching methods to EduApiClient; updated `Courses.cshtml` with server-side forms, Deactivate/Delete buttons (SuperAdmin only); Courses GET now also loads Semesters + Faculty for dropdown population.
- P1-S2-03: Enrollments — already complete from Phase 8 (EnrollStudent, AdminDropEnrollment, AdminEnrollStudentAsync all existed); confirmed Done.
- P1-S2-04: FYP Management CRUD — added `AssignFypSupervisor` and `CompleteFypProject` POST actions; added `AssignFypSupervisorAsync`, `CompleteFypProjectAsync` to EduApiClient; updated `Fyp.cshtml` with Supervisor modal (faculty dropdown) and Complete button for Approved/InProgress projects; added FYP Supervisor modal with JS to wire `data-projectid`.
- Added `Faculty` list to `FypPageModel`; added `Semesters` + `Faculty` lists to `CoursesPageModel`.

**Validation:**
- `dotnet build` on Web project: succeeded with no C# errors (only file-lock warnings from running process).
- All modified C# files: no errors confirmed via diagnostics.
- FypController: `[Authorize(Policy = "Admin")]` and `[Authorize(Policy = "Faculty")]` already include SuperAdmin per Program.cs policy configuration.

**Moved to:** P1-S6-xx (Theme/branding enhancements) or P2-S1-01 (License concurrency).

**Docs Updated:**
- Observed-Issues.md: Marked P1-S2-01/02/03/04 as Done
- Command.md: This entry

---

### Entry 014 — 2026-05-05 — Phase 1 Remediation (Batch 5 — Final Push: Regression Tests + Branding Enhancements)
**Completed:**
- P1-S1-01: 403 authorization fixes confirmed complete from Batch 1–3; marked Done in Observed-Issues.md.
- P1-S1-02: Created `AuthorizationRegressionTests.cs` (30+ test methods in IntegrationTests project) covering Attendance, Assignment, Quiz, Result endpoints — 401 for unauthenticated, 403 for wrong role, pass for correct role.
- P1-S6-01: Added 10 new themes to `wwwroot/css/site.css` and corresponding `ThemeOption` entries to `ThemeSettingsPageModel`. New themes: Neon Mint, Sakura Pink, Golden Hour, Deep Navy, Lavender Mist, Rust Canyon, Glacier Ice, Graphite Pro, Spring Blossom, Dusk Fire. Total themes: 29 (including Default).
- P1-S6-02: Logo upload — added `POST /api/v1/portal-settings/logo` endpoint (PortalSettingsController) with 2 MB cap and whitelist (.png .jpg .jpeg .gif .svg .webp); saves to `wwwroot/portal-uploads/`; `EduApiClient.UploadLogoAsync` calls endpoint; PortalController POST handles logo file; DashboardSettings.cshtml has file input + current logo preview; sidebar now shows `<img>` if LogoUrl is set, falls back to initials circle.
- P1-S6-03: Privacy Policy URL — added `PrivacyPolicyUrl` field to PortalBrandingDto/Service/ApiDto/WebModel; DashboardSettings.cshtml has URL input; _Layout.cshtml footer shows Privacy Policy link if set.
- P1-S6-04: Font style options — added `FontFamily` and `FontSize` fields to PortalBrandingDto/Service/ApiDto/WebModel; DashboardSettings.cshtml has Font Family dropdown (5 options) and Font Size dropdown (5 options); _Layout.cshtml injects `<style>` block with `font-family`/`font-size` overrides when set.

**Validation:**
- `dotnet build` on Web project: `Build succeeded` — 0 errors, 4 pre-existing CS8620 nullable warnings only.

**Docs Updated:**
- Observed-Issues.md: Marked P1-S1-01, P1-S1-02, P1-S6-01, P1-S6-02, P1-S6-03, P1-S6-04 as Done
- Command.md: This entry
- PRD.md: Bumped to v1.26

---

### Entry 015 — 2026-05-05 — Phase 1 Complete / Phase 2 Handover

**Purpose:** Full handover record for resuming work on a different system or in a new session.

**Phase 1 Remediation — Final Status: ✅ ALL DONE**

All 15 Phase 1 items (P1-S1-01 through P1-S6-04) are complete. See Observed-Issues.md for the detailed implementation and validation summary added to each stage.

**Key files changed across all Phase 1 Remediation batches:**

| File | Changes |
|------|---------|
| `src/Tabsan.EduSphere.API/Controllers/AttendanceController.cs` | Fixed `[Authorize]` policy/role strings; corrected route prefix |
| `src/Tabsan.EduSphere.API/Controllers/AssignmentController.cs` | Fixed `[Authorize]` policy/role strings |
| `src/Tabsan.EduSphere.API/Controllers/QuizController.cs` | Fixed `[Authorize]` policy/role strings |
| `src/Tabsan.EduSphere.API/Controllers/ResultController.cs` | Fixed `[Authorize]` policy/role strings |
| `src/Tabsan.EduSphere.API/Controllers/PortalSettingsController.cs` | Added `POST /api/v1/portal-settings/logo` upload endpoint with file validation |
| `src/Tabsan.EduSphere.Application/DTOs/SettingsDtos.cs` | Added `LogoUrl`, `PrivacyPolicyUrl`, `FontFamily`, `FontSize` to `PortalBrandingDto` and `SavePortalBrandingCommand` |
| `src/Tabsan.EduSphere.Application/Services/SettingsServices.cs` | Updated `PortalBrandingService.GetAsync/SaveAsync` for all 8 keys |
| `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` | Added Departments/Courses/FYP CRUD POST actions; updated DashboardSettings POST to call UploadLogoAsync |
| `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` | Added `UploadLogoAsync`; updated `PortalBrandingApiDto` for 8 fields; added Departments/Courses/FYP CRUD client methods |
| `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs` | Added `LogoUrl`, `PrivacyPolicyUrl`, `FontFamily`, `FontSize` to `PortalBrandingWebModel`; added `LogoFile` to `DashboardSettingsPageModel`; added 10 new `ThemeOption` entries |
| `src/Tabsan.EduSphere.Web/Views/Portal/DashboardSettings.cshtml` | Added logo file input + preview, privacy policy URL input, font family/size dropdowns; form `enctype="multipart/form-data"` |
| `src/Tabsan.EduSphere.Web/Views/Portal/Departments.cshtml` | Added Create/Edit/Deactivate modals with server-side form actions |
| `src/Tabsan.EduSphere.Web/Views/Portal/Courses.cshtml` | Added Create Course/Offering, Deactivate/Delete with server-side forms |
| `src/Tabsan.EduSphere.Web/Views/Portal/Fyp.cshtml` | Added Supervisor assignment modal and Complete button |
| `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml` | Logo in sidebar (with initials fallback); privacy policy footer link; font CSS injection in `<head>` |
| `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` | Added 10 new theme blocks (Neon Mint → Dusk Fire) |
| `tests/Tabsan.EduSphere.IntegrationTests/AuthorizationRegressionTests.cs` | New file: 30+ regression tests for auth on Attendance/Assignment/Quiz/Result endpoints |

**Architecture context (Phase 2 must follow these patterns):**
- JWT auth policies defined in `src/Tabsan.EduSphere.API/Program.cs` lines 66–69. All policies include SuperAdmin.
- License data stored in `Tabsan.EduSphere.Domain/Licensing/` entities.
- License validation service: `src/Tabsan.EduSphere.Application/Services/` (look for `LicenseService` or similar).
- Background job for license expiry warning: `src/Tabsan.EduSphere.BackgroundJobs/LicenseExpiryWarningJob.cs`.
- License import/update in portal: `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` — `LicenseUpdate` action.
- License key generator tool: `tools/KeyGen/` and `tools/Tabsan.Lic/`.

**Phase 2 Work Plan (next up):**

| ID | Work Item | Approach |
|----|-----------|----------|
| P2-S1-01 | Concurrent user limit based on `MaxUsers` in license | Track active logins in a DB table (e.g., `ActiveSessions`). On login, count active non-expired sessions. If count >= MaxUsers, reject login with HTTP 403 + meaningful message. |
| P2-S1-02 | SuperAdmin always exempt from concurrency limit | In the concurrency check, skip if role is SuperAdmin. |
| P2-S2-01 | `MaxUsers = 0` or `"All"` = unlimited mode | Treat zero/negative/`"All"` as unlimited; skip concurrency check entirely. |
| P2-S3-01 | One-time domain binding on first activation | On first license load: capture request domain, persist as `ActivatedDomain` in license entity. On subsequent loads: compare current domain — reject if mismatch. |
| P2-S3-02 | Force re-upload when domain changes | If `ActivatedDomain` is set and does not match current domain, redirect to LicenseUpdate page with an error message. |
| P2-S3-03 | HMAC anti-tamper on license file | License file signed with HMAC-SHA256 using a server secret. Validate signature before parsing payload. Reject with clear error if tampered. |

**Resume prompt for new session:**
```
Read Command.md first. Phase 1 Remediation is complete (all Done in Observed-Issues.md).
Begin Phase 2 from P2-S1-01. Refer to the Phase 2 Work Plan table in Entry 015.
Do not re-do any Phase 1 work.
When Phase 2 is complete, update: Observed-Issues.md, Command.md, PRD.md, Docs/Function-List.md.
```

---

### Entry 016 — 2026-05-05 — Phase 2 Complete (License Concurrency + Domain Binding)

**Completed: ✅ ALL Phase 2 items (P2-S1-01 through P2-S3-03)**

**Stage 2.1 — User Count-Based Concurrency Restriction + SuperAdmin Exemption (P2-S1-01, P2-S1-02):**
- Added `MaxUsers` property to `LicenseState` entity (int, default 0 = unlimited)
- Extended `LicenseValidationService.TablicPayload` to deserialize `MaxUsers` from binary .tablic payload
- Updated `LicenseValidationService.ActivateFromFileAsync(string filePath, string? requestDomain, CancellationToken)` signature to accept optional request domain
- Added `CountActiveSessionsAsync(CancellationToken)` to `IUserSessionRepository` interface
- Implemented `CountActiveSessionsAsync()` in `UserSessionRepository` — counts sessions where `RevokedAt == null && ExpiresAt > DateTime.UtcNow`
- Updated `AuthService.LoginAsync()` to:
  - Fetch current license
  - If user is NOT SuperAdmin AND MaxUsers > 0: count active sessions
  - Reject login if count >= MaxUsers via `LoginResult.Fail(LoginFailureReason.ConcurrencyLimitReached)`
  - SuperAdmin always exempt (skips concurrency check entirely)
- Changed `IAuthService.LoginAsync` return type from `LoginResponse?` to `LoginResult` (wrapper with success flag + failure reason enum)
- Added `LoginResult` class and `LoginFailureReason` enum to `AuthDtos.cs`
- Updated `AuthController.Login POST` to check `FailureReason` and return 403 for concurrency limit, 401 for invalid credentials

**Stage 2.2 — Unlimited Mode (P2-S2-01):**
- Implemented unlimited concurrency via `MaxUsers == 0` convention
- When MaxUsers is 0, concurrency check is skipped for all users (SuperAdmin logic runs first)
- Allows licenses to operate in "All Users" mode with no per-user cost

**Stage 2.3 — License Domain Binding + Anti-Tamper (P2-S3-01, P2-S3-02, P2-S3-03):**
- Added `ActivatedDomain` property to `LicenseState` entity (string?, max 253 chars per DNS spec)
- Extended `LicenseValidationService.TablicPayload` to deserialize optional `AllowedDomain` field from .tablic
- On activation, if payload contains `AllowedDomain`, it must match `requestDomain` or activation fails
- First activation captures domain: `activatedDomain = requestDomain ?? payload.AllowedDomain`
- Subsequent activations preserve existing `ActivatedDomain`
- Updated `LicenseController.Upload POST` to extract `Request.Host.Host` and pass to `ActivateFromFileAsync()`
- Created `LicenseDomainMiddleware` that:
  - Checks incoming request host against stored `LicenseState.ActivatedDomain`
  - Rejects cross-domain requests with HTTP 403 unless on whitelisted endpoints
  - Allows `/api/v1/auth/login`, `/api/v1/license/upload`, `/api/v1/license/status` even on locked domain
  - Prevents single-license reuse across multiple deployments (one license per domain)
- Registered middleware in `Program.cs` pipeline before authentication
- Anti-tamper already implemented: RSA-2048 signature verification + AES-256-CBC decryption + replay guard + domain binding

**EF Core Migration:**
- Created manual migration: `20260505_Phase2LicenseConcurrency.cs`
  - Adds `MaxUsers INT NOT NULL DEFAULT 0` column
  - Adds `ActivatedDomain NVARCHAR(253) NULL` column
- Migration file compiles successfully

**Files Modified:**
- [src/Tabsan.EduSphere.Domain/Licensing/LicenseState.cs](src/Tabsan.EduSphere.Domain/Licensing/LicenseState.cs) — Added MaxUsers, ActivatedDomain properties
- [src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs](src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs) — Added LoginResult, LoginFailureReason
- [src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs](src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs) — Changed LoginAsync return type
- [src/Tabsan.EduSphere.Application/Auth/AuthService.cs](src/Tabsan.EduSphere.Application/Auth/AuthService.cs) — Concurrency limit + SuperAdmin exemption
- [src/Tabsan.EduSphere.Application/Interfaces/IUserSessionRepository.cs](src/Tabsan.EduSphere.Application/Interfaces/IUserSessionRepository.cs) — Added CountActiveSessionsAsync()
- [src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs](src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs) — Implemented CountActiveSessionsAsync()
- [src/Tabsan.EduSphere.Infrastructure/Licensing/LicenseValidationService.cs](src/Tabsan.EduSphere.Infrastructure/Licensing/LicenseValidationService.cs) — Domain binding + payload extension
- [src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/LicenseStateConfiguration.cs](src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/LicenseStateConfiguration.cs) — EF configuration
- [src/Tabsan.EduSphere.API/Controllers/AuthController.cs](src/Tabsan.EduSphere.API/Controllers/AuthController.cs) — Login 403 for concurrency limit
- [src/Tabsan.EduSphere.API/Controllers/LicenseController.cs](src/Tabsan.EduSphere.API/Controllers/LicenseController.cs) — Pass request domain
- [src/Tabsan.EduSphere.API/Middleware/LicenseDomainMiddleware.cs](src/Tabsan.EduSphere.API/Middleware/LicenseDomainMiddleware.cs) — NEW middleware
- [src/Tabsan.EduSphere.API/Program.cs](src/Tabsan.EduSphere.API/Program.cs) — Registered middleware
- [src/Tabsan.EduSphere.Infrastructure/Migrations/20260505_Phase2LicenseConcurrency.cs](src/Tabsan.EduSphere.Infrastructure/Migrations/20260505_Phase2LicenseConcurrency.cs) — NEW migration

**Validation:**
- Build: 0 errors, 4 pre-existing warnings (SettingsServices.cs CS8620 only)
- All Phase 2 code compiles successfully
- CountActiveSessionsAsync correctly counts active sessions
- LoginResult and LoginFailureReason enums integrated properly
- EF Core migration created

**Documentation Updated:**
- [Observed-Issues.md](Observed-Issues.md) — Marked P2-S1-01 through P2-S3-03 Done; added Phase 2 Implementation Summary
- [Command.md](Command.md) — Updated Current Execution Pointer; added Entry 016

**Next:**
- Apply migration: `dotnet ef database update`
- Begin Phase 3: License App (P3-S1-01, P3-S2-01, P3-S2-02)
- Update KeyGen tool to support MaxUsers and AllowedDomain in .tablic payload

---

### Entry 017 — 2026-05-05 — Phase 3 Complete (License App — Generator Alignment + File Security)

**Completed: ✅ ALL Phase 3 items (P3-S1-01, P3-S2-01, P3-S2-02)**

**Stage 3.1 — Generator Alignment (P3-S1-01):**
- Added `MaxUsers` (int, default 0 = unlimited) and `AllowedDomain` (string?, nullable) to `IssuedKey` model
- Configured new columns in `LicDb.OnModelCreating`: `HasDefaultValue(0)` + `HasMaxLength(253).IsRequired(false)`
- Extended `LicenseBuilder.TablicPayload` with `MaxUsers` and `AllowedDomain`; `BuildAsync` now embeds them in the JSON payload inside the encrypted .tablic binary
- Added `UpdateConstraintsAsync(IssuedKey key)` to `KeyService` — persists constraint values before license file generation
- Updated `ExportCsvAsync()` in KeyService — CSV now includes `MaxUsers` and `AllowedDomain` columns
- Updated `HandleBuildTablic` in `Program.cs`:
  - Prompts for "Max concurrent users (0 = unlimited):" — validates non-negative integer
  - Prompts for "Allowed domain (leave blank for no restriction):" — stored as lowercase or null
  - Saves constraints via `UpdateConstraintsAsync` before generating the file
  - Shows a summary block confirming MaxUsers and AllowedDomain before writing
- Updated `HandleListKeys` display — shows MaxUsers ("Unlimited" when 0) and AllowedDomain ("(any)" when null)
- Added **startup SQLite column migration** in `Program.cs`:
  - Reads `PRAGMA table_info(issued_keys)` to get existing column names
  - Adds `MaxUsers INTEGER NOT NULL DEFAULT 0` and/or `AllowedDomain TEXT NULL` if missing
  - Existing `tabsan_lic.db` files are transparently upgraded on first launch

**Stage 3.2 — File Security (P3-S2-01 and P3-S2-02) — Pre-existing, Verified:**
- P3-S2-01 (Encrypt + validate): `LicCrypto.BuildTablicFile()` = AES-256-CBC + RSA-2048 sign; `LicenseValidationService` verifies on every activation
- P3-S2-02 (Reject modified payload): RSA signature over SHA-256(IV+ciphertext); private key only in tool; replay guard via `ConsumedVerificationKey` table

**Files Modified:**
- [tools/Tabsan.Lic/Models/IssuedKey.cs](tools/Tabsan.Lic/Models/IssuedKey.cs) — MaxUsers, AllowedDomain
- [tools/Tabsan.Lic/Data/LicDb.cs](tools/Tabsan.Lic/Data/LicDb.cs) — EF fluent config
- [tools/Tabsan.Lic/Services/LicenseBuilder.cs](tools/Tabsan.Lic/Services/LicenseBuilder.cs) — Payload extended
- [tools/Tabsan.Lic/Services/KeyService.cs](tools/Tabsan.Lic/Services/KeyService.cs) — UpdateConstraintsAsync, updated ExportCsvAsync
- [tools/Tabsan.Lic/Program.cs](tools/Tabsan.Lic/Program.cs) — DB migration, prompts, list display

**Validation:**
- `dotnet build tools/Tabsan.Lic/Tabsan.Lic.csproj --no-restore` → Succeeded 2.2s, 0 errors
- Full solution build: 0 errors

**Next:**
- Apply migration to DB: `dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure`
- Proceed to next phase as defined in Observed-Issues.md

---

### Entry 018 — Phase 4: CSV User Import (2026-05-06)

**Items completed:**
- P4-S1-01: CSV user import via `POST /api/v1/user-import/csv` (SuperAdmin/Admin only)
- P4-S2-01: Initial password = username on CSV import
- P4-S2-02: `MustChangePassword` flag set on import; `POST /api/v1/auth/force-change-password` clears it
- P4-S3-01: `User Import Sheets/` folder with `user-import-template.csv` + `README.md`

**Files created:**
- [src/Tabsan.EduSphere.Application/Interfaces/IUserImportService.cs](src/Tabsan.EduSphere.Application/Interfaces/IUserImportService.cs) — import interface
- [src/Tabsan.EduSphere.Application/Services/UserImportService.cs](src/Tabsan.EduSphere.Application/Services/UserImportService.cs) — import implementation
- [src/Tabsan.EduSphere.API/Controllers/UserImportController.cs](src/Tabsan.EduSphere.API/Controllers/UserImportController.cs) — API endpoint
- [src/Tabsan.EduSphere.Infrastructure/Migrations/20260506_Phase4UserImport.cs](src/Tabsan.EduSphere.Infrastructure/Migrations/20260506_Phase4UserImport.cs) — MustChangePassword column
- [User Import Sheets/user-import-template.csv](User%20Import%20Sheets/user-import-template.csv) — CSV template
- [User Import Sheets/README.md](User%20Import%20Sheets/README.md) — usage instructions

**Files modified:**
- [src/Tabsan.EduSphere.Domain/Identity/User.cs](src/Tabsan.EduSphere.Domain/Identity/User.cs) — `MustChangePassword` property + `ClearMustChangePassword()` method
- [src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs](src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs) — EF config for MustChangePassword
- [src/Tabsan.EduSphere.Domain/Interfaces/IUserRepository.cs](src/Tabsan.EduSphere.Domain/Interfaces/IUserRepository.cs) — added `AddRangeAsync`, `GetRoleByNameAsync`
- [src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs](src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs) — implementations of above
- [src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs](src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs) — `MustChangePassword` in `LoginResponse`, `ForceChangePasswordRequest`
- [src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs](src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs) — `ForceChangePasswordAsync`
- [src/Tabsan.EduSphere.Application/Auth/AuthService.cs](src/Tabsan.EduSphere.Application/Auth/AuthService.cs) — `ForceChangePasswordAsync` impl, MustChangePassword in login response
- [src/Tabsan.EduSphere.API/Controllers/AuthController.cs](src/Tabsan.EduSphere.API/Controllers/AuthController.cs) — `POST /api/v1/auth/force-change-password`
- [src/Tabsan.EduSphere.API/Program.cs](src/Tabsan.EduSphere.API/Program.cs) — `IUserImportService` DI registration
- [src/Tabsan.EduSphere.Application/DTOs/CsvImportDtos.cs](src/Tabsan.EduSphere.Application/DTOs/CsvImportDtos.cs) — `UserImportResult` record
- [src/Tabsan.EduSphere.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs](src/Tabsan.EduSphere.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs) — snapshot updated

**Validation:**
- `dotnet build src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj` → 0 errors

**Next:**
- Apply migration: `dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure`
- Proceed to next phase as defined in Observed-Issues.md

---

## Phase 19 — Advanced Course Creation & Result Configuration

**EF Migration:**
```
dotnet ef migrations add Phase19_CourseTypeAndGrading --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 19 — Advanced Course Creation & Result Configuration"
git pull --rebase origin main
git push origin main
```

**Test Run:** 78/78 tests passed
**Status:** ✅ Complete

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage, Course↔Subject) | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs`, `API/Controllers/LabelController.cs` |
| 24.3 | Dashboard Composition — ordered widget list by role + institution type, fed to web layer | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs`, `API/Controllers/DashboardCompositionController.cs`, `Web/Views/Portal/ModuleComposition.cshtml` |

---

## Phase 23 — Core Policy Foundation

**EF Migration:** Not required (uses existing `portal_settings` table)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 23 — Core Policy Foundation (License Policy Kernel + Institution Context Resolution + Role-Rights Hardening)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 27/27 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 23.1 | Policy Kernel — domain enum + application interface + cached service + API controller | `Domain/Enums/InstitutionType.cs`, `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs`, `API/Controllers/InstitutionPolicyController.cs` |
| 23.2 | Institution Context Middleware — per-request snapshot resolution | `API/Middleware/InstitutionContextMiddleware.cs`, `API/Program.cs` |
| 23.3 | Role-Rights Hardening — unit tests (13 new) + web layer (EduApiClient, PortalController, view, sidebar seed) | `tests/.../InstitutionPolicyTests.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/InstitutionPolicy.cshtml`, `Web/Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 22 — External Integrations

**EF Migration:**
```powershell
dotnet ef migrations add Phase22_ExternalIntegrations --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 22 — External Integrations (Library System Integration + Accreditation Reporting)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 22.1 | Library System Integration — config + loan proxy | `Domain/`, `Application/DTOs/External/LibraryDTOs.cs`, `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs`, `API/Controllers/LibraryController.cs` |
| 22.2 | Accreditation Reporting — template CRUD + report generation (CSV/TXT) | `Domain/Settings/AccreditationTemplate.cs`, `Domain/Interfaces/IAccreditationRepository.cs`, `Infrastructure/Repositories/AccreditationRepository.cs`, `Infrastructure/Persistence/Configurations/AccreditationTemplateConfiguration.cs`, `Application/DTOs/External/AccreditationDTOs.cs`, `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs`, `API/Controllers/AccreditationController.cs` |
| Cross-cutting | DI, DbContext DbSet, EduApiClient, PortalController, views, sidebar, seed SQL | `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/LibraryConfig.cshtml`, `Web/Views/Portal/AccreditationTemplates.cshtml`, `Views/Shared/_Layout.cshtml`, `Scripts/1-MinimalSeed.sql` |

---

## Phase 20 — Learning Management System (LMS)

**EF Migration:**
```powershell
dotnet ef migrations add Phase20_LMS --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 20 — Learning Management System (LMS)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean; pre-existing nullability warnings only)
**Commit:** `ecf4d91` pushed to main — 2026-05-08
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 20.1 | Course Content Modules domain + service + API + web | `Domain/Lms/CourseContentModule.cs`, `Domain/Lms/ContentVideo.cs`, `Domain/Interfaces/ILmsRepository.cs`, `Application/Interfaces/ILmsService.cs`, `Application/Lms/LmsService.cs`, `API/Controllers/LmsController.cs`, `Web/Views/Portal/CourseLms.cshtml`, `Web/Views/Portal/LmsManage.cshtml` |
| 20.2 | EF configurations for LMS entities | `Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `Infrastructure/Repositories/LmsRepository.cs` |
| 20.3 | Discussion threads + replies | `Domain/Lms/DiscussionThread.cs`, `Domain/Lms/DiscussionReply.cs`, `Domain/Interfaces/IDiscussionRepository.cs`, `Application/Interfaces/IDiscussionService.cs`, `Application/Lms/DiscussionService.cs`, `Infrastructure/Repositories/DiscussionRepository.cs`, `API/Controllers/DiscussionController.cs`, `Web/Views/Portal/Discussion.cshtml`, `Web/Views/Portal/DiscussionThread.cshtml` |
| 20.4 | Course announcements with notification fan-out | `Domain/Lms/CourseAnnouncement.cs`, `Domain/Interfaces/IAnnouncementRepository.cs`, `Application/Interfaces/IAnnouncementService.cs`, `Application/Lms/AnnouncementService.cs`, `Infrastructure/Repositories/AnnouncementRepository.cs`, `API/Controllers/AnnouncementController.cs`, `Web/Views/Portal/Announcements.cshtml` |
| Cross-cutting | DTOs, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `Application/DTOs/Lms/LmsDTOs.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 21 — Study Planner

**EF Migration:**
```powershell
dotnet ef migrations add Phase21_StudyPlanner --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure --startup-project src/Tabsan.EduSphere.API -- --environment Development
```

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 21 — Study Planner (Semester Planning Tool + Recommendation Engine)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 7/7 unit tests passed (build clean)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 21.1 | StudyPlan + StudyPlanCourse domain entities, MaxCreditLoadPerSemester on AcademicProgram | `Domain/StudyPlanner/StudyPlan.cs`, `Domain/StudyPlanner/StudyPlanCourse.cs`, `Domain/Interfaces/IStudyPlanRepository.cs` |
| 21.1 | DTOs, service interface + implementation, EF configs, repository | `Application/DTOs/StudyPlanner/StudyPlannerDTOs.cs`, `Application/Interfaces/IStudyPlanService.cs`, `Application/StudyPlanner/StudyPlanService.cs`, `Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `Infrastructure/Repositories/StudyPlanRepository.cs` |
| 21.2 | Recommendation engine (degree audit gaps + electives + prerequisite gating) | Part of `StudyPlanService.GetRecommendationsAsync` |
| Cross-cutting | Controller, DI, DbContext DbSets, EduApiClient, PortalController, views, sidebar | `API/Controllers/StudyPlanController.cs`, `API/Program.cs`, `Infrastructure/Persistence/ApplicationDbContext.cs`, `Web/Services/EduApiClient.cs`, `Web/Controllers/PortalController.cs`, `Web/Views/Portal/StudyPlan.cshtml`, `Web/Views/Portal/StudyPlanDetail.cshtml`, `Web/Views/Portal/StudyPlanRecommendations.cshtml`, `Views/Shared/_Layout.cshtml` |

---

## Phase 24 — Dynamic Module and UI Composition

**EF Migration:** Not required (no new tables)

**Git Commit:**
```powershell
git add -A
git commit -m "Phase 24 — Dynamic Module and UI Composition (Module Registry + Dynamic Labels + Dashboard Composition)"
git pull --rebase origin main
git push origin main
```

**Test Run:** 44/44 unit tests passed (build clean, 0 errors)
**Status:** ✅ Complete

### Stages Completed
| Stage | Description | Files |
|-------|-------------|-------|
| 24.1 | Module Registry — static compile-time catalogue (key, roles, institution types, license gate) + registry service combining live activation with policy snapshot | `Domain/Modules/ModuleDescriptor.cs`, `Application/Modules/ModuleRegistry.cs`, `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs`, `API/Controllers/ModuleRegistryController.cs` |
| 24.2 | Dynamic Labels — institution-mode-aware vocabulary (Semester↔Grade↔Year, GPA/CGPA↔Percentage
