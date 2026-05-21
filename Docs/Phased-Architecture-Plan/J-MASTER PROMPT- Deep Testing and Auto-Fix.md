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