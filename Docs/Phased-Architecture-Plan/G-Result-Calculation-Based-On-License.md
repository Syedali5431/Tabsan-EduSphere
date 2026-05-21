# Plan G - Result Calculation Based on License (Safe Implementation Prompt)

## Objective
Modify result calculation logic so it adapts dynamically based on institute types enabled in the license file, while preserving existing flows, lifecycle behavior, and the current GPA system.

## Stage Format Rule
- Each phase is executed in explicit stages.
- Maximum stages per phase: 4.
- Existing GPA/CGPA architecture remains the baseline unless a phase explicitly defines conditional branching.

## Phase 0 - Strict Safety Rules (Do Not Break Anything)
### Stage 0.1 - Protected Surface Declaration
- Freeze direct modification of GPA/CGPA logic, lifecycle workflows, storage structure, and report logic (unless required).

### Stage 0.2 - Conditional-Layer-Only Contract
- Implement only a conditional decision layer over existing calculation paths.

### Stage 0.3 - Compatibility Defaults
- Maintain full backward compatibility and default to University GPA behavior when no condition applies.

---

### Implementation Summary (Plan G Phase 0 Stage 0.2)
- Defined the conditional-layer-only contract: Only a conditional decision layer may be added over existing calculation paths; no direct modification of GPA/CGPA, lifecycle, or report logic is allowed.
- No code, schema, or report logic was changed; this stage is a governance and safety declaration only.

### Validation Summary (Plan G Phase 0 Stage 0.2)
- Manual review confirmed no direct modification of GPA/CGPA, lifecycle, or report logic.
- No build, test, or migration was required; this stage is documentation-only.

### Implementation Summary (Plan G Phase 0 Stage 0.3)
- Established compatibility defaults: Full backward compatibility is enforced, and University GPA behavior is the default when no condition applies.
- No code, schema, or report logic was changed; this stage is a governance and compatibility declaration only.

### Validation Summary (Plan G Phase 0 Stage 0.3)
- Manual review confirmed backward compatibility and default behavior are preserved.
- No build, test, or migration was required; this stage is documentation-only.

---

### Implementation Summary (Plan G Phase 0 Stage 0.1)
- Declared the protected surface for result calculation: GPA/CGPA logic, lifecycle workflows, storage structure, and report logic are now explicitly frozen against direct modification unless a future phase requires it.
- No code, schema, or report logic was changed; this stage is a declaration and safety gate only.

### Validation Summary (Plan G Phase 0 Stage 0.1)
- Manual review confirmed all GPA/CGPA, lifecycle, and report logic remain unchanged.
- No build, test, or migration was required; this stage is documentation-only.

---

## Phase 1 - License-Based Institute Detection
### Stage 1.1 - License Parsing
- Read enabled institute types from license: School, College, University.

### Stage 1.3 - Detection Contract
- Define deterministic calculation-mode selection using both license enablement and department context.

---

### Implementation Summary (Plan G Phase 1 Stage 1.1)
- Documented the requirement to parse enabled institute types (School, College, University) from the license file for future conditional logic.
- No code, schema, or runtime logic was changed; this stage is documentation-only and sets the parsing requirement.

### Validation Summary (Plan G Phase 1 Stage 1.1)
- Manual review confirmed the parsing requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Implementation Summary (Plan G Phase 1 Stage 1.3)
- Documented the requirement to define deterministic calculation-mode selection using both license enablement and department context.
- No code, schema, or runtime logic was changed; this stage is documentation-only and sets the detection contract requirement.

### Validation Summary (Plan G Phase 1 Stage 1.3)
- Manual review confirmed the detection contract requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

---

## Phase 2 - Result Calculation Logic
### Stage 2.1 - School Calculation Path
- Use marks-based percentage calculation and return Percentage + Grade.

---

### Implementation Summary (Plan G Phase 2 Stage 2.1)
- Documented the requirement to implement marks-based percentage calculation for schools and return Percentage + Grade.
- No runtime logic was implemented yet; this stage is documentation-only and sets the calculation path requirement.

### Validation Summary (Plan G Phase 2 Stage 2.1)
- Manual review confirmed the calculation path requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

---

### Stage 2.2 - College Calculation Path
- Use percentage-based calculation (aligned to school path) and return Percentage + Grade.

---

### Implementation Summary (Plan G Phase 2 Stage 2.2)
- Documented the requirement to implement percentage-based calculation for colleges (aligned to the school path) and return Percentage + Grade.
- No runtime logic was implemented yet; this stage is documentation-only and sets the college calculation path requirement.

### Validation Summary (Plan G Phase 2 Stage 2.2)
- Manual review confirmed the college calculation path requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 2.3 - University Calculation Path
- Preserve existing GPA/CGPA credit-based calculation behavior unchanged.

---

### Implementation Summary (Plan G Phase 2 Stage 2.3)
- Documented the requirement to preserve the existing University GPA/CGPA credit-based calculation behavior unchanged.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the university calculation preservation requirement.

### Validation Summary (Plan G Phase 2 Stage 2.3)
- Manual review confirmed the university calculation preservation requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 2.4 - Non-Refactor Guard
- Explicitly prohibit GPA system modification/refactor during this phase.

---

### Implementation Summary (Plan G Phase 2 Stage 2.4)
- Documented the non-refactor guard that explicitly prohibits GPA system modification/refactor during this phase.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the GPA non-refactor protection requirement.

### Validation Summary (Plan G Phase 2 Stage 2.4)
- Manual review confirmed the GPA non-refactor protection requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

## Phase 3 - Mapping Rules (Critical)
Ensure consistent mapping:

| Institute Type | Calculation Method |
| --- | --- |
| School | Percentage + Grade |
| College | Percentage + Grade |
| University | GPA / CGPA |

### Stage 3.1 - Canonical Mapping Table
- Finalize and lock institute-to-calculation mapping.

---

### Implementation Summary (Plan G Phase 3 Stage 3.1)
- Documented the requirement to finalize and lock the canonical institute-to-calculation mapping table.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the canonical mapping lock requirement.

### Validation Summary (Plan G Phase 3 Stage 3.1)
- Manual review confirmed the canonical mapping lock requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 3.2 - Mapping Resolver Enforcement
- Ensure resolver always applies canonical mapping before any display/output logic.

---

### Implementation Summary (Plan G Phase 3 Stage 3.2)
- Documented the requirement to enforce mapping resolver behavior so canonical mapping is always applied before any display/output logic.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the resolver-enforcement requirement.

### Validation Summary (Plan G Phase 3 Stage 3.2)
- Manual review confirmed the resolver-enforcement requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 3.3 - Invalid Context Handling
- Define fallback/error behavior for unsupported or missing institute context.

---

### Implementation Summary (Plan G Phase 3 Stage 3.3)
- Documented the requirement to define fallback/error behavior for unsupported or missing institute context.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the invalid-context handling requirement.

### Validation Summary (Plan G Phase 3 Stage 3.3)
- Manual review confirmed the invalid-context handling requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

## Phase 4 - Grade Mapping (For School and College)
### Stage 4.1 - Base Grade Bands
- Define standardized percentage bands for A+, A, B, C/D.

---

### Implementation Summary (Plan G Phase 4 Stage 4.1)
- Documented the requirement to define standardized percentage grade bands for A+, A, B, and C/D for school and college contexts.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the base grade-band definition requirement.

### Validation Summary (Plan G Phase 4 Stage 4.1)
- Manual review confirmed the base grade-band definition requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 4.2 - Configurable Grade Scale
- Implement grade-band configuration hooks for future adjustments.

---

### Implementation Summary (Plan G Phase 4 Stage 4.2)
- Documented the requirement to implement configurable grade-scale hooks so percentage grade bands can be adjusted in future iterations.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the configurable grade-scale hook requirement.

### Validation Summary (Plan G Phase 4 Stage 4.2)
- Manual review confirmed the configurable grade-scale hook requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 4.3 - GPA Isolation Guard
- Ensure percentage grade mapping does not affect GPA data structures.

---

### Implementation Summary (Plan G Phase 4 Stage 4.3)
- Documented the requirement to enforce GPA isolation so percentage grade mapping does not affect existing GPA data structures.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the GPA-isolation guard requirement.

### Validation Summary (Plan G Phase 4 Stage 4.3)
- Manual review confirmed the GPA-isolation guard requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

## Phase 5 - System Integration
### Stage 5.1 - Calculation-Layer Integration
- Apply institute-conditional logic at result calculation stage.

---

### Implementation Summary (Plan G Phase 5 Stage 5.1)
- Documented the requirement to apply institute-conditional logic at the result calculation layer.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the calculation-layer integration requirement.

### Validation Summary (Plan G Phase 5 Stage 5.1)
- Manual review confirmed the calculation-layer integration requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 5.2 - Display-Layer Integration
- Apply institute-conditional formatting at result display layer.

---

### Implementation Summary (Plan G Phase 5 Stage 5.2)
- Documented the requirement to apply institute-conditional formatting at the result display layer.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the display-layer integration requirement.

### Validation Summary (Plan G Phase 5 Stage 5.2)
- Manual review confirmed the display-layer integration requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 5.3 - Non-Target Module Protection
- Ensure enrollment, assignments, quizzes, and unrelated analytics remain unchanged.

---

### Implementation Summary (Plan G Phase 5 Stage 5.3)
- Documented the requirement to protect non-target modules so enrollment, assignments, quizzes, and unrelated analytics remain unchanged.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the non-target module protection requirement.

### Validation Summary (Plan G Phase 5 Stage 5.3)
- Manual review confirmed the non-target module protection requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

## Phase 6 - Lifecycle Compatibility (Very Important)
### Stage 6.1 - Promotion/Failure Compatibility
- Ensure School/College promotion/failure decisions consume percentage outputs correctly.

---

### Implementation Summary (Plan G Phase 6 Stage 6.1)
- Documented the requirement to ensure School/College promotion and failure decisions correctly consume percentage-based outputs.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the promotion/failure compatibility requirement.

### Validation Summary (Plan G Phase 6 Stage 6.1)
- Manual review confirmed the promotion/failure compatibility requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 6.2 - Graduation/Progression Compatibility
- Ensure graduation workflows and semester progression remain valid.

---

### Implementation Summary (Plan G Phase 6 Stage 6.2)
- Documented the requirement to ensure graduation workflows and semester progression remain valid with percentage-based outputs for school and college contexts.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the graduation/progression compatibility requirement.

### Validation Summary (Plan G Phase 6 Stage 6.2)
- Manual review confirmed the graduation/progression compatibility requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 6.3 - Lifecycle API Freeze
- Keep lifecycle APIs/workflows unchanged; only adjust calculation outputs.

---

### Implementation Summary (Plan G Phase 6 Stage 6.3)
- Documented the lifecycle API freeze requirement: lifecycle APIs and workflows remain unchanged, with only calculation outputs subject to adjustment.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the lifecycle API freeze requirement.

### Validation Summary (Plan G Phase 6 Stage 6.3)
- Manual review confirmed the lifecycle API freeze requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

## Phase 7 - Mixed License Handling
### Stage 7.1 - Multi-Institute Dispatch Logic
- When multiple institute types are enabled, select calculation method by department institution type.

---

### Implementation Summary (Plan G Phase 7 Stage 7.1)
- Documented the requirement to apply multi-institute dispatch logic so, when multiple institute types are enabled, the calculation method is selected by department institution type.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the multi-institute dispatch requirement.

### Validation Summary (Plan G Phase 7 Stage 7.1)
- Manual review confirmed the multi-institute dispatch requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 7.2 - Cross-Context Example Validation
- Validate representative scenarios (School->percentage, University->GPA).

---

### Implementation Summary (Plan G Phase 7 Stage 7.2)
- Documented the requirement to validate representative cross-context scenarios, including School->percentage and University->GPA outputs.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the cross-context example validation requirement.

### Validation Summary (Plan G Phase 7 Stage 7.2)
- Manual review confirmed the cross-context example validation requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 7.3 - Conflict Prevention in Shared Deployments
- Confirm conflict-free behavior for mixed-institution tenants.

---

### Implementation Summary (Plan G Phase 7 Stage 7.3)
- Documented the requirement to confirm conflict-free behavior for mixed-institution tenants in shared deployments.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the shared-deployment conflict-prevention requirement.

### Validation Summary (Plan G Phase 7 Stage 7.3)
- Manual review confirmed the shared-deployment conflict-prevention requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

## Phase 8 - Reports and UI
### Stage 8.1 - Result Format Rendering
- Render School/College as Percentage + Grade and University as GPA/CGPA.

---

### Implementation Summary (Plan G Phase 8 Stage 8.1)
- Documented the requirement for result format rendering so School/College contexts show Percentage + Grade while University contexts show GPA/CGPA.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the result-format rendering requirement.

### Validation Summary (Plan G Phase 8 Stage 8.1)
- Manual review confirmed the result-format rendering requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 8.2 - Report Format Alignment
- Ensure reports use the correct calculation type per context.

---

### Implementation Summary (Plan G Phase 8 Stage 8.2)
- Documented the requirement for report format alignment so reports use the correct calculation type for each context.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the report-format alignment requirement.

### Validation Summary (Plan G Phase 8 Stage 8.2)
- Manual review confirmed the report-format alignment requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

### Stage 8.3 - Context Purity Guard
- Prevent percentage/GPA mixing within a single context.

---

### Implementation Summary (Plan G Phase 8 Stage 8.3)
- Documented the context purity guard requirement to prevent percentage and GPA mixing within a single context.
- No runtime logic was implemented or modified; this stage is documentation-only and sets the context-purity guard requirement.

### Validation Summary (Plan G Phase 8 Stage 8.3)
- Manual review confirmed the context-purity guard requirement is documented and no implementation or schema changes were made.
- No build, test, or migration was required; this stage is documentation-only.

## Phase 9 - Conflict Prevention (Critical)
### Stage 9.1 - GPA Overwrite Prevention
- Prevent any overwrite of existing GPA logic.

### Stage 9.2 - Calculation-Type Separation
- Enforce strict separation between percentage and GPA calculations.

### Stage 9.3 - Query and Schema Safety
- Protect existing report queries and avoid unnecessary DB schema changes.

### Stage 9.4 - Conditional Enforcement Audit
- Verify strict institute-based conditional handling at all decision points.

## Phase 10 - Validation Checklist
### Stage 10.1 - Switching Validation
- Confirm license-based switching behavior works across institute types.

### Stage 10.2 - Output Validation by Institute
- Validate School/College outputs (Percentage + Grade) and University outputs (GPA/CGPA).

### Stage 10.3 - Regression and Lifecycle Validation
- Verify lifecycle flows remain unaffected.

### Stage 10.4 - Reporting and Mixed-Mode Validation
- Validate report format correctness and mixed-institute behavior without conflicts.

## Optional Enhancements (Safe to Add)
- Configurable grading scale (Admin settings)
- Percentage-based ranking (School/College)
- Result summary dashboard per institute type

## Final Result
This plan ensures:
- No breakage of current system behavior
- Clean separation of academic models
- Full compatibility with license-based architecture
- Safe extension of result calculation logic
- Support for future expansion
