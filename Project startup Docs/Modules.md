# University Portal – Module Definition, Activation & Packaging

**Document Version:** 1.3 (Implementation Baseline — Extended)  
**Aligned With PRD Version:** 1.8  
**Audience:** Super Admin, University Decision Makers  
**Purpose:** Define selectable system modules, activation rules, and pricing packages  

Placement rule: put Implementation Summary and Validation Summary at the end of each phase section (not at the start or end of the document).

## Execution Update - 2026-05-20 (Plan D Phase 1 Charting Framework & UI)

- Recent request issue:
  - complete Plan D Phase 1 and keep phase-end summary placement.
- Implementation Summary:
  - added interactive Analytics charting layout and clickable legends,
  - no module activation/deactivation, package pricing, or entitlement mapping changes were introduced.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 2 Stage 2.1 Global Filters)

- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.1.
- Implementation Summary:
  - introduced global analytics filters for institution/department/course/semester,
  - no module activation/deactivation, pricing, or entitlement mapping changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 2 Stage 2.2 Dependent Filtering)

- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.2.
- Implementation Summary:
  - added dependent filter cascade and downstream reset/auto-apply behavior in analytics UI flow,
  - no module activation/deactivation, pricing, or entitlement mapping changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 2 Stage 2.3 Instant Charts Update)

- Recent request issue:
  - proceed to Plan D Phase 2 Stage 2.3.
- Implementation Summary:
  - analytics charts now refresh through an async snapshot fetch flow,
  - module entitlements and role permissions are unchanged.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 3 Stage 3.1 Chart Types and Data)

- Recent request issue:
  - proceed to Plan D Phase 3 Stage 3.1.
- Implementation Summary:
  - enhanced analytics UI visual coverage with additional trend/distribution charts,
  - no module key, entitlement, or role-policy changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 4 Stage 4.1 Tenant/Campus Isolation)

- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.1.
- Implementation Summary:
  - hardened analytics data reads and cache scoping for tenant/campus isolation,
  - no module activation, entitlement, or role-policy changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`66/66`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 4 Stage 4.2 Leakage Prevention)

- Recent request issue:
  - proceed to Plan D Phase 4 Stage 4.2.
- Implementation Summary:
  - strengthened analytics export-job isolation controls with owner and tenant/campus scope checks,
  - no module catalog, entitlement, or role-policy matrix changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 5 Stage 5.1 Performance Optimization)

- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.1.
- Implementation Summary:
  - optimized analytics data retrieval and aggregation execution paths,
  - no changes to module keys, role-policy matrix, or entitlement behavior.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 5 Stage 5.2 Index and Data-Loading Refinement)

- Recent request issue:
  - proceed to Plan D Phase 5 Stage 5.2.
- Implementation Summary:
  - added analytics-focused index refinements and migration-backed schema updates,
  - no module entitlement or role-policy behavior changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 6 Stage 6.1 Validation and UI Consistency)

- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.1.
- Implementation Summary:
  - performed validation-only execution for analytics interactivity/filtering/UI consistency,
  - no module entitlement mapping or role-policy changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan D Phase 6 Stage 6.2 Final Performance and Security Review)

- Recent request issue:
  - proceed to Plan D Phase 6 Stage 6.2.
- Implementation Summary:
  - performed validation-only final review for analytics performance/security readiness,
  - no module mapping or entitlement changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests (`Analytics|AuthorizationRegressionTests`) passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 1 Stage 1.1 Functional Non-Regression Validation)

- Recent request issue:
  - there is no Phase 7 in this stream; move to Plan E and start Phase 1 Stage 1.1.
- Implementation Summary:
  - executed full automated non-regression checkpoint for existing functionality,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 1 Stage 1.2 End-to-End Module Validation)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.2.
- Implementation Summary:
  - executed full module end-to-end validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 1 Stage 1.3 UI Alignment and Form Stability)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.3.
- Implementation Summary:
  - executed UI/layout/binding/form stability validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 1 Stage 1.4 API Response and Runtime Stability)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.4.
- Implementation Summary:
  - executed API/runtime stability validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - contract tests passed (`1/1`),
  - unit tests passed (`151/151`).

## Execution Update - 2026-05-20 (Plan E Phase 1 Stage 1.5 Database Relationship Validation)

- Recent request issue:
  - proceed to Plan E Phase 1 Stage 1.5.
- Implementation Summary:
  - executed database relationship integrity validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 2 Stage 2.1 Tenant and Campus Isolation)

- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.1.
- Implementation Summary:
  - executed tenant/campus isolation validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 2 Stage 2.2 Cross-Tenant/Campus Leakage Validation)

- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.2.
- Implementation Summary:
  - executed cross-tenant/campus leakage validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 2 Stage 2.3 Tenant/Campus Query Scope Validation)

- Recent request issue:
  - proceed to Plan E Phase 2 Stage 2.3.
- Implementation Summary:
  - executed TenantId/CampusId query-scope validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - integration tests passed (`244/244`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 3 Stage 3.1 Course Material End-to-End Validation)

- Recent request issue:
  - proceed with next stage.
- Implementation Summary:
  - executed Course Material end-to-end validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted Course Material integration tests passed (`5/5`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-20 (Plan E Phase 3 Stage 3.2 Analytics Charts and Filters Validation)

- Recent request issue:
  - proceed with next stage.
- Implementation Summary:
  - executed analytics charts/filters validation checkpoint,
  - no module activation/deactivation, entitlement, pricing, or packaging changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - targeted analytics/authorization integration tests passed (`68/68`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

## Execution Update - 2026-05-19 (Plan C Phase 4 Implementation)

- Recent request issue:
  - proceed to Plan C Phase 4 UI and UX implementation.
- Implementation Summary:
  - integrated `course_material` portal navigation and page flows into the existing academic module menu path,
  - mapped `course_material` to the `courses` module key for entitlement-aware menu visibility,
  - did not alter module activation/deactivation rules or package pricing contracts.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged outside Course Material navigation visibility.

## Execution Update - 2026-05-20 (Plan C Phase 6 Implementation)

- Recent request issue:
  - complete Plan C Phase 6 Stage 6.1 and Stage 6.2.
- Implementation Summary:
  - optimized Course Material read-query execution and added targeted scoped/sorted index,
  - no module activation/deactivation, package pricing, or entitlement contract changes were introduced.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - full solution build passed.

## Execution Update - 2026-05-20 (Plan C Phase 7 Stage 7.1 Validation)

- Recent request issue:
  - start Plan C Phase 7 Stage 7.1 validation.
- Implementation Summary:
  - executed full validation for Course Material data safety/access/UI behavior,
  - no module activation/deactivation, entitlement, packaging, or pricing contract changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`393/393`).

## Execution Update - 2026-05-20 (Plan C Phase 7 Stage 7.2 Finalization)

- Recent request issue:
  - complete Plan C Phase 7 Stage 7.2 final review.
- Implementation Summary:
  - completed final stability/scalability closeout checks for Course Material,
  - no module activation/deactivation, entitlement, packaging, or pricing contract changes were introduced.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - Release-mode integration tests (Course Material authorization subset) passed (`5/5`),
  - load-test script requires running target API and failed due to unreachable local endpoint.

## Execution Update - 2026-05-20 (Plan C Phase 5 Implementation)

- Recent request issue:
  - complete Plan C Phase 5 Stage 5.1, Stage 5.2, and Stage 5.3.
- Implementation Summary:
  - added Course Material upload/download flows and role-context-aware portal fallback handling,
  - kept module contract unchanged (feature remained within existing `course_material` to `courses` entitlement mapping).
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - full solution build passed.

## Execution Update - 2026-05-19 (Plan C Phase 3 Implementation)

- Recent request issue:
  - proceed to Plan C Phase 3 access control and strict isolation.
- Implementation Summary:
  - added role-based API access and strict tenant/campus isolation for the Course Material slice,
  - no module entitlement or activation contract changed in this phase.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - existing module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan C Phase 2 Implementation)

- Recent request issue:
  - proceed after Plan C Phase 1.
- Implementation Summary:
  - added data-safety and migration hardening for Course Material records,
  - no module entitlement or activation contract changed in this phase.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - existing module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan C Phase 1 Implementation)

- Recent request issue:
  - start Plan C Phase 1.
- Implementation Summary:
  - added Course Material domain/schema foundation,
  - no module entitlement or activation contract changed in this phase.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - existing module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 10 Implementation)

- Recent request issue:
  - proceed to final validation and closeout.
- Implementation Summary:
  - completed the final readiness review of the already implemented configuration stack,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 9 Implementation)

- Recent request issue:
  - proceed to logging and visibility for startup metadata.
- Implementation Summary:
  - added safe startup visibility logging across hosts,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 8 Implementation)

- Recent request issue:
  - proceed to configuration performance and stability improvements.
- Implementation Summary:
  - optimized shared configuration bootstrap registration and reload behavior,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 7 Implementation)

- Recent request issue:
  - proceed to fail-safe configuration behavior.
- Implementation Summary:
  - added shared startup fail-safe validation for configuration and deployment settings,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 6 Implementation)

- Recent request issue:
  - proceed to tenant-aware configuration and isolation phase.
- Implementation Summary:
  - added tenant-isolation resolver and tenant JSON overlay support,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 5 Implementation)

- Recent request issue:
  - proceed to customer deployment support phase.
- Implementation Summary:
  - added deployment-pipeline config support and deployment metadata templates,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 4 Implementation)

- Recent request issue:
  - proceed to deployment flexibility phase.
- Implementation Summary:
  - added deployment-topology resolver and per-customer deployment metadata handling,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 3 Implementation)

- Recent request issue:
  - proceed to secure configuration handling phase.
- Implementation Summary:
  - added external deployment config support and secure secret validation helpers,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 2 Implementation)

- Recent request issue:
  - proceed to next Plan B phase.
- Implementation Summary:
  - added startup DB connection resolver for deployment/environment-aware connection management,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan B Phase 1 Implementation)

- Recent request issue:
  - proceed and begin Plan B configuration/deployment rollout.
- Implementation Summary:
  - standardized startup configuration hierarchy across runtime hosts,
  - no module activation/deactivation, entitlement, or module catalog behavior changed.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan A Phase 7 Implementation)

- Recent request issue:
  - proceed to Plan A Phase 7 and finalize validation/stability closeout.
- Implementation Summary:
  - completed full validation sweep and Plan A closeout documentation sync,
  - no module activation/deactivation, entitlement, or module catalog changes were introduced.
- Validation Summary:
  - build, full unit, full integration, and contract tests passed,
  - module behavior remained unchanged.

## Execution Update - 2026-05-19 (Plan A Phase 6 Implementation)

- Recent request issue:
  - proceed to Plan A Phase 6 and optimize tenant/campus scoped performance while preserving module behavior.
- Implementation Summary:
  - added query-path optimizations and scoped composite indexes,
  - no module activation/deactivation logic, module catalog behavior, or entitlement policy changed.
- Validation Summary:
  - build, focused unit tests, and focused integration tests passed,
  - module behavior remained unchanged by this phase.

## Execution Update - 2026-05-19 (Plan A Phase 5 Implementation)

- Recent request issue:
  - proceed to Plan A Phase 5 and add tenant/campus management interfaces while preserving existing module behavior.
- Implementation Summary:
  - added SuperAdmin-only tenant/campus management screens and corresponding API endpoints,
  - integrated navigation using established sidebar/menu patterns,
  - no module activation/deactivation rules, module catalog, or entitlement logic changed.
- Validation Summary:
  - build, focused unit tests, and focused integration tests passed,
  - module behavior remained unchanged by this phase.

## Execution Update - 2026-05-19 (Plan A Phase 4 Implementation)

- Recent request issue:
  - proceed to Plan A Phase 4 and enforce tenant/campus scoped access control with SuperAdmin bypass.
- Implementation Summary:
  - added repository-layer tenant/campus scoping for user and department data reads,
  - preserved SuperAdmin cross-tenant/campus access behavior,
  - no module activation/deactivation rules or module catalog behavior were changed.
- Validation Summary:
  - build, focused unit tests, and focused integration tests passed,
  - module behavior remained unchanged by this phase.

## Execution Update - 2026-05-19 (Plan A Phase 3 Implementation)

- Recent request issue:
  - proceed to Plan A Phase 3 and enforce compatibility/safety guards for tenant/campus data integrity.
- Implementation Summary:
  - added non-breaking tenant/campus integrity constraints and composite reference safety,
  - no module catalog, activation rules, or module-permission behavior changes were introduced.
- Validation Summary:
  - build, focused unit tests, and focused integration tests passed,
  - module behavior remained unchanged by this phase.

## Execution Update - 2026-05-19 (Plan A Phase 2 Implementation)

- Recent request issue:
  - proceed to Plan A Phase 2 and apply safe default tenant/campus integration for existing records.
- Implementation Summary:
  - introduced default-tenant/campus backfill migration and startup data safety enforcement,
  - module behavior and module activation rules remain unchanged.
- Validation Summary:
  - build, focused unit, and focused integration validations passed,
  - no module access-path regression introduced by this data-integration phase.

## Execution Update - 2026-05-19 (Plan A Phase 1 Implementation)

- Recent request issue:
  - proceed with Plan A Phase 1 implementation while preserving existing module and InstitutionType behavior.
- Implementation Summary:
  - introduced foundational tenant/campus domain model as additive architecture,
  - no module activation/deactivation behavior was changed in this phase,
  - existing module and role model remains fully compatible with School/College/University logic.
- Validation Summary:
  - solution build passed,
  - focused unit tests passed (`9/9`),
  - no module-level behavioral regression introduced by this phase foundation work.

## Execution Update - 2026-05-19 (Plan A Phase 1 Kickoff)

- Recent request issue:
  - start Plan A Phase 1 and synchronize mandatory governance/planning docs with phase-end implementation and validation summaries.
- Implementation Summary:
  - initiated Plan A Phase 1 execution baseline for introducing Tenant and Campus as additive architecture layers,
  - aligned module-governance narrative with non-breaking integration constraints so existing InstitutionType and module model remain intact,
  - synchronized module documentation with PRD, development plan, schema tracker, function registry, and command tracker.
- Validation Summary:
  - confirmed module behavior remains unchanged in this kickoff wave,
  - confirmed no runtime module activation/deactivation logic changed in this documentation-only update.

---

## 1. Overview

The University Portal follows a **modular architecture**, allowing universities to choose which functional modules they require.

⚠️ **Important:**  
👉 **License handling is NOT a module.**  
Licenses are created externally and uploaded by the **Super Admin**.  
Modules become available or unavailable **based on license entitlements**, but licensing itself cannot be disabled.

---

## 2. Core System Capabilities (Not Modules)

The following are **mandatory system capabilities** and are **ALWAYS present**:

- Licensing enforcement (key upload & validation)
- Role-based access control (RBAC)
- Super Admin controls
- Security & encryption
- Audit logging (minimum)

✅ These **cannot be turned off** and are not selectable modules.

---

## 3. Module Management Rules

### 3.1 Super Admin Responsibilities
Only the **Super Admin** can:
- Activate or deactivate modules
- Control module availability based on license
- Hide or expose functionality to users
- Enable paid add-on modules

---

### 3.2 Module Activation Behavior

- ✅ **Active Module**
  - Visible in UI
  - APIs enabled
  - Data accessible by role & department

- ❌ **Inactive Module**
  - Hidden from UI
  - APIs disabled
  - Existing data preserved but inaccessible

✅ Module toggling does **not delete any data**

---

## 4. Mandatory Modules (Always Enabled)

These modules are required for the portal to function and **cannot be disabled**:

### 4.1 Authentication & User Management
- Login & logout
- User roles (Student, Faculty, Admin, Super Admin)
- Student signup via **registration number**
- Password & session management

---

### 4.2 Department Management
- Department creation
- Assign faculty & programs
- Department-based access control

---

### 4.3 Student Information System (SIS)
- Student profiles
- Enrollment records
- Semester tracking
- **Full academic history across all semesters**

---

## 5. Academic Modules (Selectable)

### 5.1 Course & Program Management
- Courses and programs
- Semester mapping
- Faculty assignment

---

### 5.2 Assignment Management
- Faculty-created assignments
- Deadlines & submission rules
- Student submissions
- Feedback & grading
- Semester-based grouping

---

### 5.3 Quiz & Assessment Module
- Timed quizzes
- Attempt limits
- Auto/manual grading

---

### 5.4 Attendance Management
- Attendance tracking
- Low attendance thresholds
- Automatic student notifications

---

### 5.5 Examination & Results
- Marks entry
- GPA / CGPA calculation
- Result publishing
- Transcript generation

---

### 5.6 Timetable Management Module
- Admin creates and publishes timetables per department/semester
- Timetable download for all users in the department (PDF / Excel)
- Managed under the Departments admin menu

---

### 5.7 Finance & Payments Module
- Finance role creates and uploads fee receipts per student
- Students upload payment proof and mark as "Payment Submitted"
- Finance confirms → status = Paid
- Student account in read-only mode until fees are marked Paid
- Optional online payment gateway (card / bank) — Super Admin toggle in Module Settings

---

## 6. Communication & Coordination Modules

### 6.1 Notifications Module
- Dashboard notifications
- Assignment & quiz alerts
- Result announcements
- Low attendance warnings
- Read/unread tracking

---

### 6.2 Final Year Project (FYP) Module
- Project allocation
- Meeting scheduling
- Location entry:
  - Department
  - Room number
- Panel member selection
- Student & panel notifications

---

## 7. Intelligence & Insights Modules

### 7.1 AI Chatbot Module
- Role-aware responses
- Department-aware context
- Helps with:
  - Assignments
  - Results
  - Attendance
  - FYP schedules
- License-controlled access

---

### 7.2 Reporting & Analytics
- Student performance reports
- Department analytics
- Assignment & quiz statistics
- Export to PDF / Excel

---

## 8. UI & Experience Modules

### 8.1 Theme & Personalization Module
- Minimum **15 themes**
- Light & Dark modes
- High-contrast accessibility themes
- **Per-user** theme selection (not per-role)
- Persistent across sessions
- Admin system default theme (overridable by each user)
- AI chatbot inherits active theme

---

### 8.2 Dashboard & Navigation Module
- Role-based collapsible sidebar with menus and sub-menus
- Menus rendered based on active modules, user role, and Sidebar Settings configuration
- Super Admin always sees all menus regardless of Sidebar Settings
- "Departments" admin menu: create/edit departments, degrees, semesters, subjects, timetables
- "Graduated Students" menu: department sub-menus with checkbox list of final-semester students
- "Semester Management" menu: per-department checkbox list for semester completion/promotion

---

## 9. Compliance & Governance Modules

### 9.1 Advanced Audit & Logs
- Detailed activity logging
- Data access tracking
- Exportable audit trails
- Compliance reporting

---

### 9.2 System Settings Module

Accessible as a dedicated "Settings" menu in the navigation:

#### License Update (Super Admin upload / Admin view)
- Upload `.tablic` license file
- Status table: Status, Expiry Date, Date Updated, Remaining Days

#### Report Settings (Super Admin only)
- Table: SR#, Report Name, Purpose, Roles (multi-select checkbox)
- Activate or deactivate reports per role

#### Module Settings (Super Admin only)
- Table: SR#, Module Name, Purpose, Roles (multi-select checkbox), Status (Active/Inactive)
- Active: module visible and functional for selected roles
- Inactive: module hidden from all dashboards except Super Admin; no data deleted

#### Sidebar Settings (Super Admin only)
- Dedicated sub-menu in Dashboard Settings for controlling sidebar navigation visibility per role
- **Top-level menu table** columns: SR#, Name (menu label), Purpose (description), Roles (checkbox list of roles that can see this menu), Status (checkbox — Active = visible to permitted roles; Inactive = hidden from all roles except Super Admin)
- Clicking any top-level menu item reveals its **sub-menu table** in a panel below the top-level table; sub-menu table uses the same columns (SR#, Name, Purpose, Roles, Status)
- **Super Admin override**: Super Admin always sees every menu item and sub-menu regardless of Status or Roles settings — these settings cannot restrict Super Admin
- Changes take effect immediately on next page load for affected users; no restart required
- System menus (License, Theme, Module Settings, Sidebar Settings itself) remain visible to Super Admin and cannot be fully deactivated for that role

---

## 10. Module Dependency Summary

| Module | Can Be Disabled |
|------|---------------|
| Authentication & Users | ❌ No |
| Departments | ❌ No |
| SIS | ❌ No |
| System Settings | ❌ No |
| Dashboard & Navigation | ❌ No |
| Sidebar Settings | ❌ No |
| Courses & Programs | ✅ Yes |
| Assignments | ✅ Yes |
| Quizzes | ✅ Yes |
| Attendance | ✅ Yes |
| Exams & Results | ✅ Yes |
| Timetable Management | ✅ Yes |
| Finance & Payments | ✅ Yes |
| Notifications | ✅ Yes |
| FYP | ✅ Yes |
| AI Chatbot | ✅ Yes |
| Reports & Analytics | ✅ Yes |
| Themes & Personalization | ✅ Yes |
| Advanced Audit | ✅ Yes |

---

## 11. Basic Package (Included by Default)

The **Basic Package** includes:

- Authentication & User Management
- Department Management
- Student Information System (SIS)
- Course & Program Management
- Assignment Management
- Examination & Results
- Basic Notifications
- Timetable Management
- Dashboard & Navigation
- System Settings
- Core Themes (Light & Dark)
- Core Audit Logging

✅ Suitable for standard academic operations

---

## 12. Optional Paid Modules (Add-On)

Modules that can be enabled later (paid):

- Quiz & Assessment Module
- Attendance Management
- AI Chatbot
- Final Year Project (FYP)
- Finance & Payments (including online payment gateway)
- Timetable Management
- Advanced Reporting & Analytics
- Extended Theme Pack (15+ themes)
- Advanced Audit & Compliance
- Multi-campus support

---

## 13. Upgrade & Expansion Rules

- Modules can be activated anytime by Super Admin
- No system reinstall required
- Historical data becomes visible upon activation
- License upgrade required for paid modules

---

## 14. Final Notes

- Licensing is handled exclusively by Super Admin
- Universities never interact with license creation
- Modules ensure flexibility, scalability, and cost control
- Academic records are never deleted

---

## 15. ASP.NET Implementation Mapping

### 15.1 Module-to-Bounded-Context Mapping

| Functional Module | Bounded Context | Primary API Area |
|------|------|------|
| Authentication & Users | Identity and Access | /api/v1/auth, /api/v1/users |
| Departments | Academic Core | /api/v1/departments |
| SIS | Student Lifecycle | /api/v1/students, /api/v1/enrollments |
| Courses & Programs | Academic Core | /api/v1/programs, /api/v1/courses |
| Assignments | Learning Delivery | /api/v1/assignments |
| Quizzes | Learning Delivery | /api/v1/quizzes |
| Attendance | Learning Delivery | /api/v1/attendance |
| Exams & Results | Assessment and Results | /api/v1/results |
| Notifications | Notifications | /api/v1/notifications |
| FYP | FYP Management | /api/v1/fyp |
| AI Chatbot | AI Services | /api/v1/ai/chat |
| Reports & Analytics | Reporting | /api/v1/reports |
| Themes & Personalization | UX Personalization | /api/v1/themes |
| Advanced Audit | Audit and Compliance | /api/v1/audit |

---

### 15.2 Dependency Rules

- Courses & Programs depends on Departments
- Assignments depends on Courses & Programs and SIS
- Quizzes depends on Courses & Programs and SIS
- Attendance depends on Courses & Programs and SIS
- Exams & Results depends on Courses & Programs and SIS
- FYP depends on SIS and Notifications
- AI Chatbot depends on Licensing, RBAC, and at least one academic module
- Reporting depends on data-producing modules (Assignments, Quizzes, Attendance, Results)

If a dependency module is inactive, dependent module endpoints must return module-inactive responses.

---

### 15.3 Technical Activation Rules

- UI menu rendering checks module entitlements before route exposure
- API endpoints enforce module entitlement through policy filters
- Background jobs for a module run only when module is active
- Deactivation hides UI immediately and blocks writes; read access follows role and policy
- Reactivation restores feature access without data migration or data loss

---

### 15.4 Module State Contract

Module state should expose:

- module_key
- is_active
- source (mandatory, license, manual)
- last_changed_at
- changed_by

This contract enables auditability and deterministic behavior across UI, API, and jobs.

---

### 15.5 Packaging and Release Alignment

- v1 package: mandatory modules plus Courses, Assignments, Results, Notifications
- v1.1 package: Quizzes, Attendance, FYP, AI Chatbot baseline
- v1.2 package: Reporting, Advanced Audit, extended themes, multi-campus foundations

---