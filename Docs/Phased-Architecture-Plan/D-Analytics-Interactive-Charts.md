# Phase Plan: D. Analytics & Interactive Charts

## Overview
Enhance the Analytical section with advanced, interactive charts and global filters, supporting legend-based data control and strict Tenant + Campus isolation.

---

## Phases & Stages

### Phase 1: Charting Framework & UI
- **Stage 1.1:** Select and integrate interactive charting library
- **Stage 1.2:** Design dashboard layout with cards/panels
- **Stage 1.3:** Add color-coded, clickable legends to all charts

#### Phase 1 Implementation Summary (2026-05-20)
- Stage 1.1 selected and integrated Chart.js for interactive Analytics charting.
- Stage 1.2 refactored Analytics dashboard layout into responsive cards and chart panels.
- Stage 1.3 added color-coded clickable legends across overview, performance, attendance, and assignment charts.
- Removed duplicate Analytics rendering fragments and stabilized a single dashboard rendering path.

#### Phase 1 Validation Summary (2026-05-20)
- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed.
- Unit tests passed (`151/151`).
- Integration tests passed (`241/241`).
- Contract tests passed (`1/1`).
- Manual UI validation confirmed chart interactivity and responsive layout behavior.

### Phase 2: Data Integration & Filtering
- **Stage 2.1:** Implement global filters (InstitutionType, Department, Course, Semester)
- **Stage 2.2:** Ensure dependent filtering (each filter updates the next)
- **Stage 2.3:** Charts update instantly on filter/legend change

#### Phase 2 Progress Summary (through Stage 2.1) (2026-05-20)
- Implementation Summary:
  - added global Analytics filters for `InstitutionType`, `Department`, `Course`, and `Semester` in the portal UI,
  - extended analytics request/query plumbing across Web -> API -> AnalyticsService for `courseId` and `semesterId`,
  - applied course/semester filtering in performance, attendance, and assignment analytics queries,
  - updated analytics cache-key scoping to include course and semester filter dimensions.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

#### Phase 2 Progress Summary (through Stage 2.2) (2026-05-20)
- Implementation Summary:
  - implemented dependent filter cascade behavior (`Institution -> Department -> Course -> Semester`) for Analytics,
  - enriched course-offering payload metadata with `CourseId`, `SemesterId`, and `DepartmentId` for reliable downstream option computation,
  - added server-side dependent option generation and invalid-selection auto-reset logic,
  - added client-side auto-apply behavior so parent-filter changes immediately refresh dependent dropdown options.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

#### Phase 2 Progress Summary (through Stage 2.3) (2026-05-20)
- Implementation Summary:
  - added `PortalController.AnalyticsSnapshot` JSON endpoint to return scoped filter options and analytics report payloads,
  - refactored analytics page-model assembly into shared server-side logic used by both initial page render and snapshot fetch,
  - replaced filter form full-page submit flow with client-side snapshot fetch and in-place updates for dependent options, summary cards, and charts,
  - preserved clickable legend behavior while re-binding charts after instant data refresh.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

### Phase 3: Chart Types & Data
- **Stage 3.1:** Add Pie, Bar, and Line charts for:
  - Student distribution
  - Department-wise counts
  - Course trends
  - Semester/class performance
  - Attendance/result trends (if available)

#### Phase 3 Progress Summary (through Stage 3.1) (2026-05-20)
- Implementation Summary:
  - added advanced analytics chart panel with new `Pie`, `Bar`, and `Line` visualizations,
  - implemented student distribution by semester pie chart,
  - implemented department-wise student count bar chart,
  - implemented course-level assignment trend line chart,
  - implemented combined semester performance and attendance trend line chart,
  - kept Stage 2.3 async snapshot refresh compatibility so all new charts update instantly on filter changes.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted integration tests (`Analytics|AuthorizationRegressionTests`) passed (`65/65`),
  - unit tests passed (`151/151`),
  - contract tests passed (`1/1`).

### Phase 4: Multi-Tenant & Campus Isolation
- **Stage 4.1:** All queries/data strictly filtered by TenantId and CampusId
- **Stage 4.2:** Prevent cross-tenant/campus data leakage

### Phase 5: Performance & Optimization
- **Stage 5.1:** Optimize queries, avoid full dataset loads
- **Stage 5.2:** Use proper indexes and efficient data loading

### Phase 6: Validation & Finalization
- **Stage 6.1:** Validate interactivity, filtering, and UI consistency
- **Stage 6.2:** Final review for performance and security

---

## Key Rules
- Do NOT break existing functionality
- Do NOT redesign existing UI completely
- Maintain compatibility with Tenant + Campus architecture
- Focus on usability, interactivity, and clean integration
- Place Implementation Summary and Validation Summary at the end of each phase section.
