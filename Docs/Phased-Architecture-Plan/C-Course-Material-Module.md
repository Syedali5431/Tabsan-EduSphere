# Phase Plan: C. Course Material Module

## Overview
Add a new "Course Material" feature, fully compatible with Tenant + Campus + InstitutionType (School/College/University) structure, supporting uploads and links with strict data isolation.

---

## Phases & Stages

### Phase 1: Domain & Database Extension
- **Stage 1.1:** Add CourseMaterial entity with required fields
- **Stage 1.2:** Link CourseMaterial to Tenant, Campus, Department, Course, Semester, Subject
- **Stage 1.3:** Update schema, add foreign keys and indexes

### Phase 2: Data Safety & Migration
- **Stage 2.1:** Migrate existing data safely (if any)
- **Stage 2.2:** Ensure all new records are scoped by Tenant and Campus

### Phase 3: Access Control & Security
- **Stage 3.1:** Implement role-based access (SuperAdmin, Admin, Faculty, Student)
- **Stage 3.2:** Enforce strict isolation (no cross-tenant/campus access)

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
