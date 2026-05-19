# Phase Plan: C. Course Material Module

## Overview
Add a new "Course Material" feature, fully compatible with Tenant + Campus + InstitutionType (School/College/University) structure, supporting uploads and links with strict data isolation.

---

## Phases & Stages

### Phase 1: Domain & Database Extension
- **Stage 1.1:** Add CourseMaterial entity with required fields
- **Stage 1.2:** Link CourseMaterial to Tenant, Campus, Department, Course, Semester, Subject
- **Stage 1.3:** Update schema, add foreign keys and indexes

#### Phase 1 Implementation Summary (2026-05-19)
- Added `CourseMaterial` domain entity with required material metadata, tenant/campus ownership, department/program/semester/course references, and file/link location support.
- Added EF Core configuration for `course_materials` with foreign keys to tenant, campus, department, academic program, semester, course (subject), and creator user.
- Extended `ApplicationDbContext` with `DbSet<CourseMaterial>` and created migration `PlanCPhase1CourseMaterialFoundation` to apply table, foreign keys, and scope-first indexes.

#### Phase 1 Validation Summary (2026-05-19)
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- unit tests passed (`151/151`).
- integration tests passed (`236/236`).
- contract tests passed (`1/1`).
- total automated validations passed (`388/388`).

### Phase 2: Data Safety & Migration
- **Stage 2.1:** Migrate existing data safely (if any)
- **Stage 2.2:** Ensure all new records are scoped by Tenant and Campus

#### Phase 2 Implementation Summary (2026-05-19)
- Added strict domain guards in `CourseMaterial` to reject empty tenant/campus/department/program/semester/course/user scope values and empty material names.
- Added database-level check constraints for `course_materials` to enforce valid material type values, required scope IDs, and location-by-type validity (file/link requirements).
- Added migration `PlanCPhase2DataSafetyScopeGuard` to apply data-safety constraints without changing existing runtime workflows.

#### Phase 2 Validation Summary (2026-05-19)
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- unit tests passed (`151/151`).
- integration tests passed (`236/236`).
- contract tests passed (`1/1`).
- total automated validations passed (`388/388`).

### Phase 3: Access Control & Security
- **Stage 3.1:** Implement role-based access (SuperAdmin, Admin, Faculty, Student)
- **Stage 3.2:** Enforce strict isolation (no cross-tenant/campus access)

#### Phase 3 Implementation Summary (2026-05-19)
- Added `CourseMaterialController` with authenticated read access and write access restricted to `Faculty,Admin,SuperAdmin`.
- Added `ICourseMaterialService`/`CourseMaterialService` and `ICourseMaterialRepository`/`CourseMaterialRepository` for end-to-end material CRUD flow.
- Enforced strict tenant/campus repository scope filtering using `IAccessScopeResolver` with SuperAdmin bypass behavior aligned to existing repository patterns.

#### Phase 3 Validation Summary (2026-05-19)
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- unit tests passed (`151/151`).
- integration tests passed (`236/236`).
- contract tests passed (`1/1`).
- total automated validations passed (`388/388`).

### Phase 4: UI & UX Implementation
- **Stage 4.1:** Add "Course Material" to sidebar/menu
- **Stage 4.2:** Create Manage Materials page (Admin/Faculty)
- **Stage 4.3:** Create View Materials page (Students)
- **Stage 4.4:** Implement dependent dropdowns for selection (Dept → Course → Semester → Subject)

### Phase 5: File & Link Handling
- **Stage 5.1:** Support file uploads and/or links
- **Stage 5.2:** Store files in persistent storage
- **Stage 5.3:** Ensure files/links are always accessible to students

### Phase 6: Performance & Optimization
- **Stage 6.1:** Optimize queries by TenantId, CampusId, DepartmentId, CourseId
- **Stage 6.2:** Add proper indexes

### Phase 7: Validation & Finalization
- **Stage 7.1:** Validate data safety, access control, and UI
- **Stage 7.2:** Final review for stability and scalability

---

## Key Rules
- Do NOT break existing functionality
- Do NOT modify existing logic unnecessarily
- Do NOT alter current UI design system
- Strictly enforce Tenant + Campus isolation
