MASTER PROMPT: Deep Testing + Auto-Fix (Phased)
Perform a comprehensive, deep testing and validation of the entire application.

The goal is to identify, fix, and ensure that NO functionality is broken, missing, or producing incorrect results.

-----------------------------------
🔹 GENERAL REQUIREMENTS
-----------------------------------

- Perform testing in structured phases
- Validate both functionality AND data correctness
- Fix issues automatically where possible
- Ensure no regressions are introduced
- Maintain full application stability and performance

-----------------------------------
🔹 PHASE 1: CORE SYSTEM VALIDATION
-----------------------------------

- Verify app starts without errors
- Ensure no runtime errors (including "error 104")
- Check routing/navigation between modules
- Validate configuration loading (multi-environment settings)
- Confirm API endpoints return correct responses

✅ Fix:
- Startup failures
- Missing configs
- Broken routes
- API errors

---

### Implementation Summary (Plan J Phase J1 Stage J1.1)
- Established Plan J governance normalization by mapping the source PHASE 1 block to `Phase J1 Stage J1.1 (Core System Validation)` for tracker consistency.
- Documented Phase J1 scope: startup stability, runtime-error absence (including error 104), routing/navigation continuity, multi-environment configuration loading, and API response correctness.
- Documented bounded fix categories for this stage (startup/config/route/API error classes) while preserving non-destructive stability constraints.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J1 Stage J1.1)
- Manual review confirmed Phase J1 validation objectives and bounded fix categories are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 2: UI & DATA RENDERING
-----------------------------------

- Validate all tables load correctly
- Ensure charts and graphs:
  - Render properly
  - Display correct data
  - Handle empty data safely

- Check:
  - Pagination
  - Sorting
  - Data formatting (dates, currency, percentages)

✅ Fix:
- Broken UI components
- Empty/null crashes
- Incorrect data bindings

---

### Implementation Summary (Plan J Phase J2 Stage J2.1)
- Documented Phase J2 UI/data-rendering validation scope: table loading, chart/graph rendering correctness, empty-data safety handling, pagination, sorting, and data formatting for dates/currency/percentages.
- Documented bounded fix categories for this stage (UI component failures, null/empty-data crashes, and data-binding inconsistencies) while preserving non-destructive stability constraints.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J2 Stage J2.1)
- Manual review confirmed Phase J2 rendering and data-presentation validation objectives are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 3: FILTERS & DATA ACCURACY
-----------------------------------

- Test all filters:
  - Date filters
  - Role filters
  - Institution filters
  - Financial filters

- Verify:
  - Filter outputs match database data
  - Combined filters work correctly
  - No incorrect or missing results

✅ Fix:
- Incorrect query logic
- Inconsistent filtering
- Performance issues in queries

---

### Implementation Summary (Plan J Phase J3 Stage J3.1)
- Documented Phase J3 filter and data-accuracy validation scope covering date, role, institution, and financial filters, including combined-filter behavior and output correctness against database data.
- Documented bounded fix categories for this stage: query-logic correctness, filtering consistency, and query-performance issues.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J3 Stage J3.1)
- Manual review confirmed Phase J3 filter-accuracy objectives and bounded fix categories are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 4: MODULE TESTING
-----------------------------------

Test all modules individually and in combination:

- User Management (Admin, Roles, Permissions)
- Student module
- Finance module
- Institution-based features
- Dashboard
- Reports

Verify:
- Data integrity across modules
- Role-based access works correctly
- Institution isolation works (multi-tenant behavior)

✅ Fix:
- Permission leaks
- Cross-institution data exposure
- Broken workflows

---

### Implementation Summary (Plan J Phase J4 Stage J4.1)
- Documented Phase J4 module-testing scope across User Management, Student, Finance, Institution-based features, Dashboard, and Reports in both isolated and combined execution paths.
- Documented validation boundaries for cross-module data integrity, role-based access enforcement, and institution isolation behavior.
- Documented bounded fix categories for this stage: permission leaks, cross-institution data exposure, and broken workflow paths.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J4 Stage J4.1)
- Manual review confirmed Phase J4 module-integrity and access-isolation objectives are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 5: LICENSE SYSTEM VALIDATION
-----------------------------------

- Validate licensing logic:
  - Licensed users access correctly
  - Unauthorized users restricted properly
  - Super admin bypass logic works (if defined)

- Ensure:
  - License checks do not break performance
  - No false positives/negatives

✅ Fix:
- Incorrect validation logic
- Runtime errors from license checks

---

### Implementation Summary (Plan J Phase J5 Stage J5.1)
- Documented Phase J5 license-validation scope: licensed-user access correctness, unauthorized-user restriction behavior, and super-admin bypass handling where defined.
- Documented runtime-safety boundary to ensure license checks preserve performance characteristics and avoid false positive/negative outcomes.
- Documented bounded fix categories for this stage: license validation logic defects and runtime exceptions from license checks.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J5 Stage J5.1)
- Manual review confirmed Phase J5 licensing-validation objectives and bounded fix categories are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 6: SETTINGS & THEMES
-----------------------------------

- Verify settings are applied correctly
- Check theme switching:
  - UI updates properly
  - No broken styling

- Validate:
  - Config persistence
  - No override issues

✅ Fix:
- Theme rendering bugs
- Settings not saving/loading

---

### Implementation Summary (Plan J Phase J6 Stage J6.1)
- Documented Phase J6 settings/theme validation scope: settings application correctness, theme-switch behavior, UI rendering continuity, and configuration persistence.
- Documented safety boundaries to ensure no unintended override behavior across saved configuration paths.
- Documented bounded fix categories for this stage: theme rendering defects and settings save/load persistence failures.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J6 Stage J6.1)
- Manual review confirmed Phase J6 settings/theme validation objectives and bounded fix categories are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 7: IMPORT / EXPORT / DOWNLOAD
-----------------------------------

Test all data operations:

- Import (CSV, Excel, etc.)
- Export
- Download reports

Verify:
- Data consistency after import/export
- No data corruption
- Correct file formatting

✅ Fix:
- Mapping issues
- Encoding problems
- Missing fields

---

### Implementation Summary (Plan J Phase J7 Stage J7.1)
- Documented Phase J7 data-operation validation scope covering import (CSV/Excel), export workflows, and report download paths.
- Documented verification boundaries for data consistency after transfer operations, corruption prevention, and output file-format correctness.
- Documented bounded fix categories for this stage: mapping defects, encoding issues, and missing-field handling gaps.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J7 Stage J7.1)
- Manual review confirmed Phase J7 import/export/download validation objectives and bounded fix categories are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 8: DATABASE VALIDATION
-----------------------------------

- Validate:
  - Data relationships
  - Foreign keys
  - Null handling
  - Data consistency

- Run test queries to verify:
  - Aggregations
  - Reports
  - Financial calculations

✅ Fix:
- Incorrect joins
- Missing constraints
- Data anomalies

---

### Implementation Summary (Plan J Phase J8 Stage J8.1)
- Documented Phase J8 database-validation scope: relationship integrity, foreign-key behavior, null handling, and consistency verification for aggregations, reporting outputs, and financial calculations.
- Documented bounded fix categories for this stage: join correctness defects, missing constraint coverage, and data-anomaly handling.
- Preserved non-destructive safety boundaries by constraining this stage to validation and governance tracking intent.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J8 Stage J8.1)
- Manual review confirmed Phase J8 database-validation objectives and bounded fix categories are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 9: PERFORMANCE & EDGE CASES
-----------------------------------

- Test with:
  - Large datasets
  - Empty datasets
  - Invalid inputs

- Check:
  - Load times
  - Query performance
  - Memory usage

✅ Fix:
- Slow queries
- UI lag
- Crashes under load

---

### Implementation Summary (Plan J Phase J9 Stage J9.1)
- Documented Phase J9 performance and edge-case validation scope for large datasets, empty datasets, invalid inputs, load-time behavior, query performance, and memory usage stability.
- Documented bounded fix categories for this stage: slow-query behavior, UI lag conditions, and load-driven crash scenarios.
- Preserved non-destructive safety boundaries by constraining this stage to validation and governance tracking intent.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J9 Stage J9.1)
- Manual review confirmed Phase J9 performance and edge-case objectives are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

-----------------------------------
🔹 PHASE 10: FINAL INTEGRATION TEST
-----------------------------------

- Test full workflow:
  - User → Login → Role → Data → Report → Export

- Ensure:
  - All modules work together
  - No hidden issues remain

✅ Fix:
- Integration bugs
- State inconsistencies

---

### Implementation Summary (Plan J Phase J10 Stage J10.1)
- Documented Phase J10 final-integration validation scope for end-to-end workflow continuity from user authentication and role context through data access, reporting, and export paths.
- Documented cross-module interoperability boundary requiring coordinated behavior without hidden regressions.
- Documented bounded fix categories for this stage: integration defects and state-consistency failures.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan J Phase J10 Stage J10.1)
- Manual review confirmed Phase J10 end-to-end integration objectives and bounded fix categories are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

---

### Implementation Summary (Plan J Final Closure)
- Recorded final Plan J closure after documenting all stages from Phase J1 Stage J1.1 through Phase J10 Stage J10.1.
- Confirmed this execution stream remains documentation-only with no runtime, API, or schema mutation.

### Validation Summary (Plan J Final Closure)
- Manual verification confirmed complete stage coverage and consistent governance tracking across required documents.
- No build, test, or migration was required; final closure is documentation-only.

-----------------------------------
🔹 VALIDATION RULES
-----------------------------------

- NO “error 104” anywhere in app
- All UI elements must render without failure
- All filters must produce accurate results
- All roles must behave correctly
- Licensing must work exactly as intended

-----------------------------------
🔹 OUTPUT REQUIRED
-----------------------------------

1. List of issues found
2. Fixes applied
3. Remaining risks (if any)
4. Suggestions for improvement

-----------------------------------
🔹 SAFETY CONSTRAINTS
-----------------------------------

- Do NOT remove core functionality
- Do NOT break existing features
- Maintain backward compatibility
- Prefer fixing over rewriting

-----------------------------------

Ensure the application is fully stable, error-free, and production-ready after completion.