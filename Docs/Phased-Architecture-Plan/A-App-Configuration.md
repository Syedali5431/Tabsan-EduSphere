# Phase Plan: A. App Configuration (Multi-Tenant + Campus)

## Overview
Refactor and extend the application to support a proper multi-tenant architecture using both Tenant and Campus concepts, without breaking any existing logic, database structure, or UI.

---

## Phases & Stages

### Phase 1: Domain Layer Extension
- **Stage 1.1:** Introduce `Tenant` as a new domain entity
- **Stage 1.2:** Introduce `Campus` as a new domain entity, linked to `Tenant`
- **Stage 1.3:** Update all relevant entities to reference both `Tenant` and `Campus`

#### Phase 1 Implementation Summary
- Implemented foundational multi-tenant domain entities: `Tenant` and `Campus`.
- Extended core root entities with non-breaking optional references:
	- `User` now supports `TenantId` and `CampusId`.
	- `Department` now supports `TenantId` and `CampusId`.
- Added EF Core configuration and indexes for tenant/campus ownership and relationships.
- Added Phase 1 migration for tenancy foundation:
	- `tenants` and `campuses` tables,
	- optional tenant/campus columns on `users` and `departments`,
	- foreign keys and lookup indexes.

#### Phase 1 Validation Summary
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Focused unit validation passed (`9/9`):
	- `EnrollmentServiceWaitlistTests`
	- `AuthSecurityUxTests`
- Verified InstitutionType behavior and existing architecture were preserved (no replacement/duplication of School/College/University logic).

### Phase 2: Data Integration & Migration
- **Stage 2.1:** Migrate existing data to assign a default Tenant and Campus
- **Stage 2.2:** Update database schema to add `TenantId` and `CampusId` where required
- **Stage 2.3:** Add foreign keys and indexes for Tenant/Campus

#### Phase 2 Implementation Summary
- Added migration `Phase42_DefaultTenantCampusBackfill` to ensure existing records are safely assigned to default tenant/campus.
- Implemented startup safeguards in `DatabaseSeeder` to:
	- guarantee default tenant (`DEFAULT`) and campus (`MAIN`) exist,
	- backfill null `TenantId`/`CampusId` for `users` and `departments`.
- Kept the integration non-breaking by using additive data assignment without replacing existing InstitutionType model.

#### Phase 2 Validation Summary
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Focused unit tests passed (`9/9`):
	- `EnrollmentServiceWaitlistTests`
	- `AuthSecurityUxTests`
- Focused integration tests passed (`52/52`):
	- `AdminUserManagementIntegrationTests`
	- `AuthorizationRegressionTests`

### Phase 3: Compatibility & Safety
- **Stage 3.1:** Ensure InstitutionType (School/College/University) logic is unchanged
- **Stage 3.2:** Integrate Tenant + Campus within existing structure
- **Stage 3.3:** Validate no data loss or invalid state

#### Phase 3 Implementation Summary
- Added domain safety guards so `TenantId` and `CampusId` must be provided together on key aggregates (`User`, `Department`).
- Added database-level compatibility/safety rules:
	- check constraints enforcing valid tenant/campus pair state,
	- composite FK integrity so campus selection must match the selected tenant,
	- alternate key support on campuses for tenant-bound campus references.
- Added migration `Phase43_TenantCampusCompatibilitySafety` to apply non-breaking integrity hardening.

#### Phase 3 Validation Summary
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Focused unit tests passed (`9/9`):
	- `EnrollmentServiceWaitlistTests`
	- `AuthSecurityUxTests`
- Focused integration tests passed (`52/52`):
	- `AdminUserManagementIntegrationTests`
	- `AuthorizationRegressionTests`
- Verified School/College/University InstitutionType behavior remains intact (no replacement or duplication).

### Phase 4: Access Control & Filtering
- **Stage 4.1:** Implement data access filtering by Tenant and Campus
- **Stage 4.2:** SuperAdmin cross-tenant/campus access
- **Stage 4.3:** User access limited to their Tenant/Campus

#### Phase 4 Implementation Summary
- Implemented access-scope resolution for current requests using authenticated claims (tenant/campus + role).
- Added JWT claim emission for `tenant_id` and `campus_id` during login token generation.
- Enforced repository-level data filtering for `User` and `Department` by tenant/campus scope.
- Added explicit SuperAdmin bypass for cross-tenant/campus visibility.

#### Phase 4 Validation Summary
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Focused unit tests passed (`9/9`):
	- `EnrollmentServiceWaitlistTests`
	- `AuthSecurityUxTests`
- Focused integration tests passed (`52/52`):
	- `AdminUserManagementIntegrationTests`
	- `AuthorizationRegressionTests`
- Verified scoped filtering is additive and does not alter InstitutionType (School/College/University) behavior.

### Phase 5: UI Management Interfaces
- **Stage 5.1:** Add Tenant Management UI (SuperAdmin only)
- **Stage 5.2:** Add Campus Management UI (linked to Tenant)
- **Stage 5.3:** Integrate with existing sidebar/menu patterns

#### Phase 5 Implementation Summary
- Added SuperAdmin-only tenant management API + portal UI for create, rename, activate/deactivate workflows.
- Added SuperAdmin-only campus management API + portal UI linked to tenant selection and campus lifecycle management.
- Integrated Tenant/Campus screens into existing sidebar/menu patterns and SuperAdmin fallback links.

#### Phase 5 Validation Summary
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Focused unit tests passed (`9/9`):
	- `EnrollmentServiceWaitlistTests`
	- `AuthSecurityUxTests`
- Focused integration tests passed (`52/52`):
	- `AdminUserManagementIntegrationTests`
	- `AuthorizationRegressionTests`
- Verified existing InstitutionType behavior remains unchanged and integration is additive.

### Phase 6: Performance & Optimization
- **Stage 6.1:** Optimize queries for Tenant + Campus scoping
- **Stage 6.2:** Add indexes and avoid unnecessary joins

#### Phase 6 Implementation Summary
- Optimized tenant/campus scoped repository predicates to avoid non-sargable string transforms on username/email/role lookups.
- Added composite query-path indexes for scoped user and department read patterns.
- Added migration `Phase46_TenantCampusQueryOptimization` for index rollout.

#### Phase 6 Validation Summary
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Focused unit tests passed (`9/9`):
	- `EnrollmentServiceWaitlistTests`
	- `AuthSecurityUxTests`
- Focused integration tests passed (`52/52`):
	- `AdminUserManagementIntegrationTests`
	- `AuthorizationRegressionTests`
- Verified existing InstitutionType behavior remains unchanged.

### Phase 7: Validation & Finalization
- **Stage 7.1:** Validate system behavior, data safety, and UI
- **Stage 7.2:** Final review for scalability and stability

#### Phase 7 Implementation Summary
- Executed final end-to-end validation sweep across build, unit, integration, and contract layers.
- Confirmed tenant/campus architecture integration remains non-breaking through all prior phases (foundation, safety, scope filtering, UI, and optimization).
- Completed final phase governance synchronization for implementation/validation evidence.

#### Phase 7 Validation Summary
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- Full unit suite passed (`151/151`).
- Full integration suite passed (`236/236`).
- Contract suite passed (`1/1`).
- Total automated validations passed: `388/388`.
- Verified InstitutionType behavior remains unchanged.

---

## Key Rules
- Do NOT break existing functionality
- Do NOT remove or rewrite existing modules
- Do NOT restructure current domain logic
- All changes must align with current architecture and naming conventions
- Data safety and isolation are critical
