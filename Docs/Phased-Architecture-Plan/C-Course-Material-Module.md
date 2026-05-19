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

#### Phase 4 Implementation Summary (2026-05-19)
- Added `PortalController` course-material actions for manage and student read-only workflows with filter-state preserving redirects.
- Added `Views/Portal/CourseMaterial.cshtml` for Admin/Faculty material management and `Views/Portal/CourseMaterialStudent.cshtml` for student read-only browsing.
- Added sidebar integration for `course_material` in web layout menu mapping and API sidebar module-key mapping to preserve entitlement filtering.

#### Phase 4 Validation Summary (2026-05-19)
- `dotnet build Tabsan.EduSphere.sln -c Debug` passed.
- unit tests passed (`151/151`).
- integration tests passed (`236/236`).
- contract tests passed (`1/1`).
- total automated validations passed (`388/388`).

### Phase 5: File & Link Handling
- **Stage 5.1:** Support file uploads and/or links
- **Stage 5.2:** Store files in persistent storage
- **Stage 5.3:** Ensure files/links are always accessible to students

#### Phase 5 Stage 5.1 Implementation Summary (2026-05-20)
- Added `POST /api/v1/course-materials/upload` with role guard (`Faculty,Admin,SuperAdmin`) to accept multipart uploads.
- Integrated deep file validation via `FileUploadValidator` before persistence, then saved files through the existing `IMediaStorageService` abstraction under `course-materials` category.
- Added portal wiring so Manage Course Materials create/update forms can upload files directly (`materialFile`), auto-populating `BlobPath`, `FileName`, and `FileSizeBytes` for material create/update requests.

#### Phase 5 Stage 5.2 Implementation Summary (2026-05-20)
- Updated Course Material uploads to persist under scope-aware storage categories (`course-materials/{tenantId}/{campusId}`) for tenant/campus isolation in persistent storage.
- Added `GET /api/v1/course-materials/{id}/file` to stream persisted files from storage with metadata-aware content type and download filename.
- Added portal download integration so both manage and student pages consume persisted files through a first-class "Download File" flow instead of displaying raw blob paths.

#### Phase 5 Stage 5.3 Implementation Summary (2026-05-20)
- Added authorization regression coverage for Course Material file endpoints to ensure student read access is preserved while upload remains restricted to elevated roles.
- Improved portal download UX fallback by preserving role-context redirect targets (`CourseMaterial` vs `CourseMaterialStudent`) when file retrieval fails.
- Completed student-access hardening for uploaded materials by validating end-to-end API and portal consumption paths without exposing raw storage keys in the UI.

### Phase 6: Performance & Optimization
- **Stage 6.1:** Optimize queries by TenantId, CampusId, DepartmentId, CourseId
- **Stage 6.2:** Add proper indexes

#### Phase 6 Stage 6.1 Implementation Summary (2026-05-20)
- Optimized `CourseMaterialRepository` read paths to use `AsNoTracking()` for filter and by-id lookups, reducing EF Core tracking overhead on read-mostly flows.
- Added early scope short-circuit for non-SuperAdmin calls without tenant scope to avoid unnecessary database round-trips.
- Preserved existing tenant/campus/department/program/semester/course filter behavior and ordering while improving query execution efficiency.

#### Phase 6 Stage 6.2 Implementation Summary (2026-05-20)
- Added composite index `IX_course_materials_scope_active_sort` on (`TenantId`, `CampusId`, `IsActive`, `Name`, `CreatedAt`) to support scoped active listing and default sort pattern efficiently.
- Added migration `PlanCPhase6Stage2CourseMaterialIndexTuning` to apply/revert the index cleanly in deployment pipelines.
- Kept existing scope lookup indexes intact and introduced only targeted index expansion to avoid unnecessary write overhead.

### Phase 7: Validation & Finalization
- **Stage 7.1:** Validate data safety, access control, and UI
- **Stage 7.2:** Final review for stability and scalability

#### Phase 7 Stage 7.1 Implementation Summary (2026-05-20)
- Executed full end-to-end validation sweep for Course Material data safety, role-based access behavior, and portal/API UI integration stability.
- Verified Stage 5/6 capabilities remain intact after upload/download hardening and scoped index tuning changes.
- Confirmed no additional code or schema mutation was required during this validation stage.

#### Phase 7 Stage 7.1 Validation Summary (2026-05-20)
- `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed (with one non-blocking existing warning).
- unit tests passed (`151/151`).
- integration tests passed (`241/241`).
- contract tests passed (`1/1`).
- total automated validations passed (`393/393`).

#### Phase 7 Stage 7.2 Implementation Summary (2026-05-20)
- Completed final stability/scalability closeout review for Course Material after file handling, access hardening, and index tuning delivery.
- Ran release-configuration build and targeted Course Material access regression checks to confirm production-path readiness.
- Reviewed load-testing assets under `tests/load` and confirmed scripts are available for environment-backed execution.

#### Phase 7 Stage 7.2 Validation Summary (2026-05-20)
- `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed (with one non-blocking existing warning).
- integration tests (Course Material authorization subset) passed (`5/5`) in Release mode.
- load test script dry run attempted (`k6-auth-current.js`) and failed due to unreachable local API target (`http://localhost:5181`), indicating environment readiness dependency rather than code failure.
- final phase validation status: stable for release with infrastructure-backed load test execution pending target API availability.

---

## Key Rules
- Do NOT break existing functionality
- Do NOT modify existing logic unnecessarily
- Do NOT alter current UI design system
- Strictly enforce Tenant + Campus isolation
