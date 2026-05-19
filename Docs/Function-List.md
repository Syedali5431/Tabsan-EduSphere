# Function List — Tabsan EduSphere

> **Maintenance rule**: Every function added to the codebase must be registered here with Name, Purpose, and Location.
> Format: `Name | Purpose | Location`

## 2026-05-20 - Plan C Phase 7 Stage 7.1 Validation

- Recent request issue:
  - start Plan C Phase 7 Stage 7.1 validation.
- Implementation Summary:
  - executed full data safety/access control/UI validation for Course Material.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Debug -v minimal` passed,
  - unit tests passed (`151/151`),
  - integration tests passed (`241/241`),
  - contract tests passed (`1/1`),
  - total automated validations passed (`393/393`).

No new functions were added in this stage.

## 2026-05-20 - Plan C Phase 7 Stage 7.2 Finalization

- Recent request issue:
  - complete Plan C Phase 7 Stage 7.2 final review for stability and scalability.
- Implementation Summary:
  - executed release-readiness stability/scalability closeout checks for Course Material.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -c Release -v minimal` passed,
  - Release-mode integration tests (Course Material authorization subset) passed (`5/5`),
  - load-test script execution requires available API target and was blocked by unreachable `http://localhost:5181`.

No new functions were added in this stage.

## 2026-05-20 - Plan C Phase 6 Implementation (Performance & Optimization)

- Recent request issue:
  - complete Plan C Phase 6 Stage 6.1 and Stage 6.2.
- Implementation Summary:
  - optimized Course Material read-query execution path and added targeted scoped sort index.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - full build passed.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CourseMaterialRepository.IsScopeMissingForNonSuperAdmin` | Short-circuits read operations early when tenant scope is missing for non-SuperAdmin callers. | `src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs` |
| `IX_course_materials_scope_active_sort` | Optimizes scoped active Course Material listing and sort pattern (`TenantId`, `CampusId`, `IsActive`, `Name`, `CreatedAt`). | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/LmsConfigurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Migrations/20260519215715_PlanCPhase6Stage2CourseMaterialIndexTuning.cs` |

## 2026-05-20 - Plan C Phase 5 Implementation (File & Link Handling)

- Recent request issue:
  - complete Plan C Phase 5 Stage 5.1, Stage 5.2, and Stage 5.3.
- Implementation Summary:
  - added upload/download API + web flows and role-context-aware fallback handling.
- Validation Summary:
  - targeted Course Material authorization regression tests passed (`5/5`),
  - full build passed.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CourseMaterialController.Upload` | Uploads validated material files and persists them in scoped storage for Faculty/Admin/SuperAdmin users. | `src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs` |
| `CourseMaterialController.DownloadFile` | Streams stored material files with metadata-aware content type and file name for authorized scoped users. | `src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs` |
| `CourseMaterialController.BuildScopedStorageCategory` | Builds tenant/campus-aware storage category path for Course Material upload isolation. | `src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs` |
| `IEduApiClient.UploadCourseMaterialFileAsync` | Uploads Course Material files from portal to API multipart endpoint. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `IEduApiClient.DownloadCourseMaterialFileAsync` | Downloads Course Material files from API for portal-proxied file delivery. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `PortalController.DownloadCourseMaterialFile` | Proxies material file download from API and preserves student/manage redirect context on failure. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `JwtTestHelper.GenerateToken(...tenantId, campusId)` | Emits scoped tenant/campus JWT claims for integration authorization tests. | `tests/Tabsan.EduSphere.IntegrationTests/Infrastructure/JwtTestHelper.cs` |
| `AuthorizationRegressionTests.CourseMaterial_*` | Validates endpoint authorization matrix for Course Material upload/download access. | `tests/Tabsan.EduSphere.IntegrationTests/AuthorizationRegressionTests.cs` |

## 2026-05-19 - Plan C Phase 4 Implementation (UI & UX)

- Recent request issue:
  - proceed to Plan C Phase 4 UI and UX implementation.
- Implementation Summary:
  - added portal UI flows for course material management and student view mode with scoped filtering.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `PortalController.CourseMaterial` | Renders Admin/Faculty manage page with scoped filter options and material list retrieval. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `PortalController.CourseMaterialStudent` | Renders Student read-only page with active scoped materials. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `PortalController.CreateCourseMaterial` | Creates a course material from the portal manage page while preserving current filter state. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `PortalController.UpdateCourseMaterial` | Updates course material metadata/location from the portal manage page. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `PortalController.SetCourseMaterialActive` | Toggles material active state from the portal manage page. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `IEduApiClient.GetCourseMaterialsAsync` | Fetches scoped course materials for portal pages with optional active-only filtering. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `SidebarMenuController.MenuModuleKeyMap["course_material"]` | Maps the `course_material` menu key to the `courses` module for entitlement-based menu visibility. | `src/Tabsan.EduSphere.API/Controllers/SidebarMenuController.cs` |

## 2026-05-19 - Plan C Phase 3 Implementation (Access Control & Security)

- Recent request issue:
  - proceed to Plan C Phase 3 access control and strict isolation.
- Implementation Summary:
  - added scope-aware course material API/service/repository flow with role-based write guards.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CourseMaterialController.GetAll` | Returns course materials with strict repository-enforced tenant/campus filtering. | `src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs` |
| `CourseMaterialController.Create` | Creates scoped material records for authorized Faculty/Admin/SuperAdmin users using caller identity. | `src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs` |
| `ICourseMaterialService` | Defines the course material application contract for read/write and activation workflows. | `src/Tabsan.EduSphere.Application/Interfaces/ICourseMaterialService.cs` |
| `CourseMaterialService` | Implements material create/update/activation logic with scope-derived tenant/campus enforcement. | `src/Tabsan.EduSphere.Application/Lms/CourseMaterialService.cs` |
| `ICourseMaterialRepository` | Defines course material data-access contract with filter-based retrieval APIs. | `src/Tabsan.EduSphere.Domain/Interfaces/ICourseMaterialRepository.cs` |
| `CourseMaterialRepository.ApplyTenantCampusScope` | Enforces strict tenant/campus query isolation with SuperAdmin bypass behavior. | `src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs` |

## 2026-05-19 - Plan C Phase 2 Implementation (Data Safety & Migration)

- Recent request issue:
  - proceed after Plan C Phase 1.
- Implementation Summary:
  - added strict domain and database safety guards for course material scope and location validity.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CourseMaterial.EnsureRequiredScope` | Prevents creation of unscoped material records by rejecting empty scope identifiers. | `src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs` |
| `CourseMaterial.EnsureMaterialLocation` | Enforces material-type-specific file/link requirements before persistence. | `src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs` |
| `CK_course_materials_scope_required` | Database check constraint enforcing non-empty tenant/campus/department/program/semester/course/creator identifiers. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Migrations/20260519055118_PlanCPhase2DataSafetyScopeGuard.cs` |
| `CK_course_materials_material_type` | Database check constraint allowing only defined material type values. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Migrations/20260519055118_PlanCPhase2DataSafetyScopeGuard.cs` |
| `CK_course_materials_location_by_type` | Database check constraint enforcing valid file/link location per material type. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Migrations/20260519055118_PlanCPhase2DataSafetyScopeGuard.cs` |

## 2026-05-19 - Plan C Phase 1 Implementation (Domain & Database Extension)

- Recent request issue:
  - start Plan C Phase 1.
- Implementation Summary:
  - added course-material domain model and schema foundation.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CourseMaterial` | Represents tenant/campus scoped course materials linked to department/program/semester/course context. | `src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs` |
| `CourseMaterial.UpdateMetadata` | Updates material title and description metadata. | `src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs` |
| `CourseMaterial.UpdateLocation` | Updates material storage location for file/link and validates required location by material type. | `src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs` |
| `CourseMaterialConfiguration` | Configures `course_materials` table, foreign keys, and indexes for isolation and lookup efficiency. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/LmsConfigurations.cs` |
| `ApplicationDbContext.CourseMaterials` | Registers course-material aggregate set in EF DbContext. | `src/Tabsan.EduSphere.Infrastructure/Persistence/ApplicationDbContext.cs` |

## 2026-05-19 - Plan B Phase 10 Implementation (Validation & Finalization)

- Recent request issue:
  - proceed to final validation and closeout.
- Implementation Summary:
  - completed the final readiness and security/scalability review of the already implemented configuration stack.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Phase B closeout` | Finalizes the configuration and deployment roadmap after validating environment readiness, security posture, and scalability support. | `Docs/Phased-Architecture-Plan/B-Configuration-Deployment.md` |

## 2026-05-19 - Plan B Phase 9 Implementation (Logging & Visibility)

- Recent request issue:
  - proceed to logging and visibility for startup metadata.
- Implementation Summary:
  - added shared startup visibility reporting and standardized safe startup logs.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `StartupVisibilityReporter` | Produces safe startup visibility metadata such as database type and configuration source summaries without exposing secrets. | `src/Tabsan.EduSphere.Application/Services/StartupVisibilityReporter.cs` |
| `StartupVisibilityReporter.DescribeDatabaseType` | Classifies the resolved connection string into a safe database-type label for logging. | `src/Tabsan.EduSphere.Application/Services/StartupVisibilityReporter.cs` |
| `StartupVisibilityReporter.DescribeConfigurationSources` | Builds a safe, human-readable summary of the active configuration source metadata. | `src/Tabsan.EduSphere.Application/Services/StartupVisibilityReporter.cs` |

## 2026-05-19 - Plan B Phase 8 Implementation (Performance & Stability)

- Recent request issue:
  - proceed to configuration performance and stability improvements.
- Implementation Summary:
  - optimized shared configuration hierarchy registration to avoid duplicate providers and unnecessary reload watchers.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ConfigurationBootstrapper.AddEduSphereConfigurationHierarchy` | Builds the shared configuration hierarchy without duplicating default appsettings/environment providers and preserves correct source precedence. | `src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs` |
| `ConfigurationBootstrapper.InsertJsonSourceIfMissing` | Inserts optional JSON configuration sources only when they are not already registered. | `src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs` |
| `ConfigurationBootstrapper.InsertEnvironmentVariablesSourceIfMissing` | Avoids duplicate environment-variable providers while preserving intended override order. | `src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs` |
| `ConfigurationBootstrapper.GetInsertionIndex` | Places overlay config sources ahead of environment variables and command-line inputs to keep expected precedence stable. | `src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs` |

## 2026-05-19 - Plan B Phase 7 Implementation (Fail-Safe Behavior)

- Recent request issue:
  - proceed to fail-safe startup behavior for configuration and deployment settings.
- Implementation Summary:
  - added centralized startup fail-safe validation and removed duplicated host-specific placeholder checks.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `StartupConfigurationFailSafeValidator` | Centralizes startup fail-fast validation for deployment, database, reverse-proxy, and tenant-isolation configuration. | `src/Tabsan.EduSphere.Application/Services/StartupConfigurationFailSafeValidator.cs` |
| `StartupConfigurationFailSafeValidator.ValidateCommonStartupConfiguration` | Validates shared host startup settings and throws clear errors before the app starts serving traffic. | `src/Tabsan.EduSphere.Application/Services/StartupConfigurationFailSafeValidator.cs` |
| `StartupConfigurationFailSafeValidator.ValidateRequiredSetting` | Validates required non-development settings against missing or placeholder values. | `src/Tabsan.EduSphere.Application/Services/StartupConfigurationFailSafeValidator.cs` |
| `SecureConfigurationValidator.IsUnsafePlaceholderValue` | Exposes shared placeholder detection so startup validation uses one consistent unsafe-value rule set. | `src/Tabsan.EduSphere.Application/Services/SecureConfigurationValidator.cs` |

## 2026-05-19 - Plan B Phase 6 Implementation (Tenant + Campus Aware Configuration)

- Recent request issue:
  - proceed to tenant-aware configuration and isolation phase.
- Implementation Summary:
  - added tenant-isolation resolver and optional tenant JSON overlay support.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `TenantIsolationResolver` | Centralizes per-tenant settings and isolation metadata layered on top of deployment/customer profile resolution. | `src/Tabsan.EduSphere.Application/Services/TenantIsolationResolver.cs` |
| `TenantIsolationResolver.Resolve` | Resolves tenant isolation mode, tenant code/name/domain/database, and tenant config path from config and environment variables. | `src/Tabsan.EduSphere.Application/Services/TenantIsolationResolver.cs` |
| `TenantIsolationResolution` | Value contract carrying non-secret tenant-isolation metadata for startup logs and health diagnostics. | `src/Tabsan.EduSphere.Application/Services/TenantIsolationResolver.cs` |

## 2026-05-19 - Plan B Phase 5 Implementation (Customer Deployment Support)

- Recent request issue:
  - proceed to customer deployment support phase.
- Implementation Summary:
  - added deployment-pipeline config layer support and customer deployment templates.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `appsettings.Deployment.json support` | Allows deployment-pipeline mounted configuration to override customer-specific values without code changes. | `src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs` |
| `Deployment config templates` | Provide appsettings-based deployment metadata for customer mode, code, name, domain, database, and scaling overrides. | `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json`, `src/Tabsan.EduSphere.Web/appsettings.json`, `src/Tabsan.EduSphere.Web/appsettings.Production.json`, `src/Tabsan.EduSphere.BackgroundJobs/appsettings.json`, `src/Tabsan.EduSphere.BackgroundJobs/appsettings.Production.json` |

## 2026-05-19 - Plan B Phase 4 Implementation (Deployment Flexibility)

- Recent request issue:
  - proceed to deployment flexibility phase.
- Implementation Summary:
  - added shared deployment-topology resolver and deployment metadata exposure.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DeploymentTopologyResolver` | Centralizes deployment-profile resolution for cloud/customer-hosted/multi-instance runtime modes. | `src/Tabsan.EduSphere.Application/Services/DeploymentTopologyResolver.cs` |
| `DeploymentTopologyResolver.Resolve` | Resolves effective deployment mode, customer identity, domain, database name, and scaling settings from config and environment variables. | `src/Tabsan.EduSphere.Application/Services/DeploymentTopologyResolver.cs` |
| `DeploymentTopologyResolution` | Value contract carrying non-secret deployment metadata used by startup logs and health diagnostics. | `src/Tabsan.EduSphere.Application/Services/DeploymentTopologyResolver.cs` |

## 2026-05-19 - Plan B Phase 3 Implementation (Secure Configuration Handling)

- Recent request issue:
  - proceed to secure configuration handling phase.
- Implementation Summary:
  - added external config support and secure production validation helper.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SecureConfigurationValidator` | Centralizes secure startup validation for sensitive values while avoiding secret disclosure in errors/logs. | `src/Tabsan.EduSphere.Application/Services/SecureConfigurationValidator.cs` |
| `SecureConfigurationValidator.RequireSecureValue` | Validates production-only sensitive settings and rejects missing/placeholder secret values with sanitized error messaging. | `src/Tabsan.EduSphere.Application/Services/SecureConfigurationValidator.cs` |

## 2026-05-19 - Plan B Phase 2 Implementation (Database Connection Management)

- Recent request issue:
  - proceed to next Plan B phase for connection-management hardening.
- Implementation Summary:
  - added centralized startup DB connection resolver and integrated it into API/BackgroundJobs.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DatabaseConnectionResolver` | Centralized startup database connection selection strategy for deployment and environment aware resolution. | `src/Tabsan.EduSphere.Application/Services/DatabaseConnectionResolver.cs` |
| `DatabaseConnectionResolver.ResolveDefaultConnection` | Resolves DB connection string from prioritized environment/deployment keys with backward-compatible fallback to legacy connection-string key. | `src/Tabsan.EduSphere.Application/Services/DatabaseConnectionResolver.cs` |
| `DatabaseConnectionResolution` | Value contract carrying resolved connection string and sanitized source metadata for startup diagnostics. | `src/Tabsan.EduSphere.Application/Services/DatabaseConnectionResolver.cs` |

## 2026-05-19 - Plan B Phase 1 Implementation (Configuration Structure)

- Recent request issue:
  - proceed and begin Plan B with environment configuration hierarchy standardization.
- Implementation Summary:
  - introduced shared startup configuration hierarchy bootstrap and applied it across API/Web/BackgroundJobs.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ConfigurationBootstrapper` | Shared startup configuration loader that standardizes source order for base, environment-specific, local, and environment-variable layers. | `src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs` |
| `ConfigurationBootstrapper.AddEduSphereConfigurationHierarchy` | Applies consistent configuration hierarchy with prefixed-env override support and backward-compatible fallback to regular environment variables. | `src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs` |

## 2026-05-19 - Plan A Phase 7 Implementation (Validation and Finalization)

- Recent request issue:
  - proceed to Plan A Phase 7 and complete final validation/finalization of tenant/campus rollout.
- Implementation Summary:
  - executed full-system validation sweep and finalized Plan A governance synchronization.
- Validation Summary:
  - full automated validation passed (`388/388`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Plan A Phase 7 full validation sweep` | Final quality gate execution across build, full unit, full integration, and contract suites before closeout. | `Tabsan.EduSphere.sln`, `tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj`, `tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj`, `tests/Tabsan.EduSphere.ContractTests/Tabsan.EduSphere.ContractTests.csproj` |
| `Plan A Phase 7 governance closeout synchronization` | Final synchronization of implementation/validation summaries across required governance documents. | `Docs/Command.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md`, `Project startup Docs/Modules.md`, `Project startup Docs/PRD.md`, `Docs/Phased-Architecture-Plan/A-App-Configuration.md` |

## 2026-05-19 - Plan A Phase 6 Implementation (Performance and Scoped Query Optimization)

- Recent request issue:
  - proceed to Plan A Phase 6 and optimize tenant/campus scoped data access paths.
- Implementation Summary:
  - optimized user lookup predicates to preserve index usage,
  - added composite scoped indexes for users/departments,
  - added migration for performance index rollout.
- Validation Summary:
  - build and focused unit/integration suites passed (`61/61`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `UserRepository scoped predicate optimization` | Removes non-sargable lowercasing on username/email/role lookups to improve index utilization. | `src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs` |
| `IX_users_tenant_campus_active_role` | Speeds tenant/campus scoped role-based active user retrieval paths. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519040540_Phase46_TenantCampusQueryOptimization.cs` |
| `IX_users_tenant_campus_username` | Speeds tenant/campus scoped username lookup and ordered user retrieval paths. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519040540_Phase46_TenantCampusQueryOptimization.cs` |
| `IX_departments_tenant_campus_name` | Speeds tenant/campus scoped department listing ordered by name. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519040540_Phase46_TenantCampusQueryOptimization.cs` |
| `Phase46_TenantCampusQueryOptimization migration` | Applies composite index optimizations for tenant/campus scoped query workloads. | `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519040540_Phase46_TenantCampusQueryOptimization.cs` |

## 2026-05-19 - Plan A Phase 5 Implementation (Tenant/Campus UI Management Interfaces)

- Recent request issue:
  - proceed to Plan A Phase 5 and add SuperAdmin tenant/campus management interfaces integrated into existing menu patterns.
- Implementation Summary:
  - added tenant/campus management API endpoints,
  - added portal pages and service client methods for tenant/campus management operations,
  - integrated navigation links into existing sidebar and SuperAdmin fallback patterns.
- Validation Summary:
  - build and focused unit/integration suites passed (`61/61`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `TenantController` | SuperAdmin API for tenant list/create/update and activate/deactivate lifecycle actions. | `src/Tabsan.EduSphere.API/Controllers/TenantController.cs` |
| `CampusController` | SuperAdmin API for campus list/filter-by-tenant, create/update, and activate/deactivate lifecycle actions. | `src/Tabsan.EduSphere.API/Controllers/CampusController.cs` |
| `IEduApiClient tenant/campus methods` | Web client contract and implementation for tenant/campus management calls. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `PortalController.TenantManagement` | Tenant management page workflow handling create/update/toggle actions. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `PortalController.CampusManagement` | Campus management page workflow linked to selected tenant with create/update/toggle actions. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `TenantManagement view` | SuperAdmin UI for tenant lifecycle operations. | `src/Tabsan.EduSphere.Web/Views/Portal/TenantManagement.cshtml` |
| `CampusManagement view` | SuperAdmin UI for tenant-linked campus lifecycle operations. | `src/Tabsan.EduSphere.Web/Views/Portal/CampusManagement.cshtml` |

## 2026-05-19 - Plan A Phase 4 Implementation (Access Control and Tenant/Campus Filtering)

- Recent request issue:
  - proceed to Plan A Phase 4 and enforce tenant/campus-based data access while keeping SuperAdmin global visibility.
- Implementation Summary:
  - added request access-scope resolution,
  - added JWT tenant/campus scope claims,
  - enforced repository-level scoped filtering with SuperAdmin bypass.
- Validation Summary:
  - build and focused unit/integration suites passed (`61/61`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IAccessScopeResolver` | Contract for resolving caller scope (isSuperAdmin, tenantId, campusId) from request identity context. | `src/Tabsan.EduSphere.Application/Interfaces/IAccessScopeResolver.cs` |
| `HttpAccessScopeResolver` | Reads role and tenant/campus claims from `HttpContext` to provide runtime scope values. | `src/Tabsan.EduSphere.API/Services/HttpAccessScopeResolver.cs` |
| `TokenService tenant/campus claims emission` | Publishes `tenant_id` and `campus_id` claims in JWT tokens so downstream data filtering can be enforced. | `src/Tabsan.EduSphere.Infrastructure/Auth/TokenService.cs` |
| `UserRepository.ApplyTenantCampusScope` | Applies tenant/campus query filtering for user reads with SuperAdmin bypass. | `src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs` |
| `DepartmentRepository.ApplyTenantCampusScope` | Applies tenant/campus query filtering for department reads with SuperAdmin bypass. | `src/Tabsan.EduSphere.Infrastructure/Repositories/DepartmentRepository.cs` |

## 2026-05-19 - Plan A Phase 3 Implementation (Tenant/Campus Compatibility and Safety)

- Recent request issue:
  - proceed to Plan A Phase 3 and harden tenant/campus compatibility constraints while preserving current InstitutionType logic.
- Implementation Summary:
  - added domain and schema safeguards to prevent invalid tenant/campus combinations.
- Validation Summary:
  - focused unit and integration validations passed (`61/61`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `User.ValidateTenantCampusPair` | Prevents invalid user state by requiring TenantId and CampusId to be set/cleared together. | `src/Tabsan.EduSphere.Domain/Identity/User.cs` |
| `Department.SetTenantCampus pair guard` | Prevents invalid department state by rejecting partial tenant/campus assignment. | `src/Tabsan.EduSphere.Domain/Academic/Department.cs` |
| `Campus alternate key (Id,TenantId)` | Enables tenant-bound composite references so campus assignment is validated within tenant scope. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/CampusConfiguration.cs` |
| `CK_users_tenant_campus_pair` | Enforces database-level tenant/campus pair validity for users. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519034517_Phase43_TenantCampusCompatibilitySafety.cs` |
| `CK_departments_tenant_campus_pair` | Enforces database-level tenant/campus pair validity for departments. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519034517_Phase43_TenantCampusCompatibilitySafety.cs` |
| `Tenant-bound campus composite FK enforcement` | Ensures selected campus belongs to selected tenant for users/departments. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs` |

## 2026-05-19 - Plan A Phase 2 Implementation (Default Tenant/Campus Data Integration)

- Recent request issue:
  - proceed with Plan A Phase 2 and ensure all existing core records are safely assigned to default tenant/campus.
- Implementation Summary:
  - added migration-backed data integration and runtime seeding safeguards for tenant/campus backfill.
- Validation Summary:
  - build and focused unit/integration suites passed (`61/61`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Phase42_DefaultTenantCampusBackfill migration` | Ensures default tenant/campus rows exist and backfills null tenant/campus on existing users and departments. | `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519032844_Phase42_DefaultTenantCampusBackfill.cs` |
| `EnsureDefaultTenantCampusAsync` | Guarantees default tenant (`DEFAULT`) and default campus (`MAIN`) are present and active at startup. | `src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs` |
| `EnsureTenantCampusBackfillAsync` | Performs startup safety backfill for users/departments missing tenant/campus assignments. | `src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs` |

## 2026-05-19 - Plan A Phase 1 Implementation (Tenant + Campus Domain Foundation)

- Recent request issue:
  - proceed with Plan A Phase 1 actual implementation and keep documentation plus repository synchronization mandatory.
- Implementation Summary:
  - implemented additive tenancy domain foundation,
  - introduced optional tenant/campus references on root entities to preserve backward compatibility.
- Validation Summary:
  - solution build passed,
  - focused unit tests passed (`9/9`).

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Tenant aggregate` | Represents a top-level customer organization for multi-tenant SaaS ownership and activation lifecycle. | `src/Tabsan.EduSphere.Domain/Tenancy/Tenant.cs` |
| `Campus aggregate` | Represents a tenant-owned campus (physical/logical) with activation lifecycle and tenant linkage. | `src/Tabsan.EduSphere.Domain/Tenancy/Campus.cs` |
| `User.SetTenantCampus(tenantId, campusId)` | Assigns or clears tenant/campus ownership for user-level scoping without breaking existing identity flows. | `src/Tabsan.EduSphere.Domain/Identity/User.cs` |
| `Department.SetTenantCampus(tenantId, campusId)` | Assigns or clears tenant/campus ownership for department-level scoping while preserving InstitutionType behavior. | `src/Tabsan.EduSphere.Domain/Academic/Department.cs` |
| `TenantConfiguration` | Maps the `tenants` table with unique tenant code and soft-delete filter for non-destructive lifecycle management. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/TenantConfiguration.cs` |
| `CampusConfiguration` | Maps the `campuses` table with tenant-bound uniqueness and FK relationship to tenants. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/CampusConfiguration.cs` |
| `Phase41_TenantCampusFoundation migration` | Applies non-breaking schema expansion for tenancy foundation tables/columns/FKs/indexes. | `src/Tabsan.EduSphere.Infrastructure/Migrations/20260519031636_Phase41_TenantCampusFoundation.cs` |

## 2026-05-19 - Plan A Phase 1 Kickoff (Tenant + Campus Domain Layer Extension)

- Recent request issue:
  - start Plan A Phase 1 and update mandatory governance/planning documentation with phase-end implementation and validation summaries.
- Implementation Summary:
  - formalized Phase 1 completion-summary placement at the end of Phase 1 in Plan A,
  - synchronized execution status across requested governance trackers.
- Validation Summary:
  - confirmed no duplicate runtime-function registration is introduced by this request,
  - confirmed this wave introduces no runtime/API/schema code mutation.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Plan A Phase 1 phase-end summary placement` | Enforces phase-local completion reporting by placing implementation and validation summaries at the end of Phase 1 instead of at document end. | `Docs/Phased-Architecture-Plan/A-App-Configuration.md` |
| `Plan A Phase 1 governance synchronization checkpoint` | Records synchronized Phase 1 kickoff status across command, product, planning, module, schema, and functionality tracker documents. | `Docs/Command.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md`, `Project startup Docs/Modules.md`, `Project startup Docs/PRD.md` |

## 2026-05-19 - Deep System Audit + Validation + AI Chatbot Modernization (Phase 1-7)

- Recent request issue:
  - complete full-system phase audit/validation and deliver modular floating chatbot replacement while preserving backend contracts.
- Implementation Summary:
  - added AI chat API v1 compatibility route and aligned client route usage,
  - modularized chatbot widget into dedicated floating-button and panel components.
- Validation Summary:
  - `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
  - targeted unit/integration validations passed (`13/13`).
- Testing and result summary:
  - phase-by-phase checks completed and documented in governance artifacts.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AiChatController route dual-mapping` | Supports both legacy and versioned AI chat API route prefixes for compatibility and `/api/v1` consistency. | `src/Tabsan.EduSphere.API/Controllers/AiChatController.cs` |
| `AI module enforcement for /api/v1/ai` | Ensures license/module enforcement applies to versioned AI endpoints as well as legacy endpoints. | `src/Tabsan.EduSphere.API/Middleware/ModuleLicenseEnforcementMiddleware.cs` |
| `Versioned AI chat web client routes` | Uses canonical `/api/v1/ai` chat endpoints from the web API client. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `FloatingChatButton component` | Provides reusable floating chat launcher UI in the shared layout. | `src/Tabsan.EduSphere.Web/Views/Shared/FloatingChatButton.cshtml` |
| `ChatPanel component` | Provides reusable chat panel UI with header, history list, messages, and composer. | `src/Tabsan.EduSphere.Web/Views/Shared/ChatPanel.cshtml` |
| `Modular chatbot layout composition` | Replaces inline chatbot markup with component composition in shared layout. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml` |
| `Button-safe floating chat styling` | Ensures consistent cross-browser rendering for button-based floating launcher. | `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |

## 2026-05-19 - UI/UX Redesign Phase 9 (Final UI Polish)

- Recent request issue:
  - proceed with the final redesign phase and complete the mandated docs plus repository sync.
- Implementation Summary:
  - refined shared card, section, and dashboard presentation details to make the UI feel more finished,
  - preserved all existing backend interactions and route behavior.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched frontend CSS.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Final UI polish elevation tuning` | Improves card hover depth and general surface elevation to make the overall SaaS presentation feel more finished. | `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `Dashboard presentation refinement` | Improves dashboard visual rhythm and attention to detail without changing connection or navigation behavior. | `src/Tabsan.EduSphere.Web/Views/Portal/Dashboard.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `Shared spacing consistency pass` | Tightens spacing and readability across core content surfaces while preserving existing interactions. | `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |

## 2026-05-18 - UI/UX Redesign Phase 8 (Responsive Hardening)

- Recent request issue:
  - proceed with responsive-design phase and complete mandatory docs and repository sync.
- Implementation Summary:
  - added shared responsive shell/page utilities for stacked actions, filter controls, modal width, profile-menu behavior, and overflow handling,
  - applied targeted responsive hooks to results and payments views.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched responsive frontend files.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Shared mobile action and filter hardening` | Ensures action clusters, filter toolbars, card spacing, table containers, and modal widths adapt more cleanly on tablet/mobile screens. | `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `Responsive result action stacking` | Makes result management action controls wrap and stack cleanly on smaller screens while preserving existing publish/create flows. | `src/Tabsan.EduSphere.Web/Views/Portal/Results.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `Responsive payments pagination layout` | Improves small-screen pagination readability and tap targets for the payment receipts page. | `src/Tabsan.EduSphere.Web/Views/Portal/Payments.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |

## 2026-05-18 - UI/UX Redesign Phase 7 (AI Chatbot UI)

- Recent request issue:
  - proceed with AI chatbot UI phase and complete mandatory docs and repository sync.
- Implementation Summary:
  - enhanced chatbot launcher, header identity, panel aesthetics, message motion, and quick-prompt UX,
  - preserved existing backend route/contract usage for chat state and send actions.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched frontend files.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AI chatbot panel identity refresh` | Adds stronger assistant identity framing and cleaner visual hierarchy in the chatbot header while preserving current assistant behavior. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `AI quick-prompt suggestion chips` | Adds frontend-only prompt shortcuts that trigger existing send flow without introducing backend/API changes. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/js/site.js`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `AI launcher and thread motion polish` | Improves launcher presence, panel background depth, and message-entry animation for a more premium chat UX. | `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |

## 2026-05-18 - UI/UX Redesign Phase 6 (Branding Pass)

- Recent request issue:
  - proceed with next redesign step and ensure full docs plus repository synchronization.
- Implementation Summary:
  - refined shared brand block, header composition, notification chip, and profile dropdown visuals,
  - preserved all existing navigation/auth routes and backend behavior.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in touched frontend files.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Shared brand block refinement` | Improves institution logo/name/subtitle presentation and introduces a cleaner branded identity treatment in the sidebar shell. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `Header notification chip upgrade` | Enhances notification icon affordance with a stronger visual cue and direct route to notifications while keeping the existing endpoint flow unchanged. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `Profile dropdown UI enhancement` | Replaces basic profile/signout presentation with a richer dropdown panel for identity context and quick actions without changing authentication logic. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml`, `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |

## 2026-05-18 - UI/UX Redesign Continuation (Enrollments/Results/Payments + Phase-Level Summary Formatting)

- Recent request issue:
  - continuation requested the next page-polish wave plus phase-level implementation/validation summaries in the redesign specification.
- Implementation Summary:
  - upgraded `Enrollments`, `Results`, and `Payments` pages to match the shared premium visual system,
  - normalized `Docs/Improved UI and look.md` so each completed phase carries its own completion summary block with markdown-lint-safe formatting.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - workspace diagnostics reported no errors in the touched Razor views and redesign document.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Enrollments page sectioned visual harmonization` | Aligns enroll/drop and roster areas to the shared section-card, toolbar, status-pill, and empty-state conventions without changing enrollment workflows. | `src/Tabsan.EduSphere.Web/Views/Portal/Enrollments.cshtml` |
| `Results workflow visual continuity pass` | Harmonizes result filters, entry/publish/correct sections, and listing visuals with existing global UX tokens while preserving result operations. | `src/Tabsan.EduSphere.Web/Views/Portal/Results.cshtml` |
| `Payments page visual continuity pass` | Harmonizes receipt creation/filter/listing surfaces with the shared section-card and state-display patterns while preserving payment actions. | `src/Tabsan.EduSphere.Web/Views/Portal/Payments.cshtml` |
| `Phase-level completion summary formatting` | Enforces per-phase implementation/validation completion blocks and lint-clean markdown structure in the redesign specification document. | `Docs/Improved UI and look.md` |

## 2026-05-18 - UI/UX Redesign Continuation (Students/Courses/Admin Users Polish)

- Recent request issue:
  - requested continuation required applying the new SaaS visual language to more portal management pages for consistent UX.
- Implementation Summary:
  - upgraded page composition and visual hierarchy for Students, Courses, and Admin Users,
  - added shared helper styles to support consistent empty states, section headers, stat badges, and option cards.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
  - change scope remained frontend-only.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Students page visual harmonization` | Aligns Students page filters, list section, empty state, and action affordances to the premium design system. | `src/Tabsan.EduSphere.Web/Views/Portal/Students.cshtml` |
| `Courses page dual-panel polish` | Aligns Courses and Offerings panels with consistent section headers, badges, and empty-state treatment. | `src/Tabsan.EduSphere.Web/Views/Portal/Courses.cshtml` |
| `Admin users management page polish` | Improves Admin Users create/update layout consistency and department assignment card readability. | `src/Tabsan.EduSphere.Web/Views/Portal/AdminUsers.cshtml` |
| `Portal section helper style tokens` | Provides reusable section, toolbar, stat pill, and option-card classes used by continued page redesign work. | `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |

## 2026-05-18 - UI/UX Redesign Request (Execution Snapshot)

- Recent request issue:
  - the portal required a frontend-only premium SaaS redesign covering shell, dashboard, navigation, forms, tables, chatbot, responsiveness, and overall visual polish without any backend or business-logic changes.
- Implementation Summary:
  - delivered a global visual system refresh through shared layout, global CSS, dashboard markup, and frontend-only UI behavior enhancements,
  - preserved routes, form actions, controller behavior, API usage, and database interactions.
- Validation Summary:
  - `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the frontend redesign,
  - verified touched frontend files report no workspace diagnostics.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Portal shell visual redesign` | Provides upgraded branding, header, responsive sidebar, menu icons, active-state styling, and polished AI launcher framing without changing navigation logic. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml` |
| `Academic SaaS design system` | Defines the refreshed typography, palette, spacing, cards, forms, tables, modals, loaders, toasts, responsive layout, and chatbot visual treatment used across the portal. | `src/Tabsan.EduSphere.Web/wwwroot/css/site.css` |
| `Dashboard hero and stats presentation` | Reframes the dashboard into a premium hero/card layout while preserving the existing API connection model and form submission flow. | `src/Tabsan.EduSphere.Web/Views/Portal/Dashboard.cshtml` |
| `Frontend toast/loader/sidebar enhancements` | Adds non-invasive UI-only behavior for page-loader fade-out, toast rendering from existing alerts, and mobile sidebar interactions. | `src/Tabsan.EduSphere.Web/wwwroot/js/site.js` |

## 2026-05-18 - Documentation Synchronization Follow-up (Execution Snapshot)

- Recent request issue:
  - required follow-up synchronization was requested across five planning/traceability documents after the latest TODO closure.
- Implementation Summary:
  - added aligned execution snapshot entries in PRD, Function List, Complete Functionality Reference, Development Plan, and Database Schema,
  - normalized wording to keep issue/implementation/validation traceability consistent.
- Validation Summary:
  - verified all five requested documents now include this follow-up entry,
  - verified no runtime/API/schema mutation is associated with this update.

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Follow-up documentation issue capture` | Records the explicit follow-up request to synchronize the five required planning/traceability documents. | `Project startup Docs/PRD.md`, `Docs/Function-List.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md` |
| `Cross-document follow-up implementation alignment` | Applies a common implementation-summary narrative and closure wording across all requested documents. | `Project startup Docs/PRD.md`, `Docs/Function-List.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md` |
| `Follow-up validation evidence alignment` | Confirms every requested document includes the dated issue/implementation/validation block and that the update is documentation-only. | `Project startup Docs/PRD.md`, `Docs/Function-List.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md` |

## 2026-05-18 - Stage 40.1 PhoneNumber/SMS Recipient Dependency Completion (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `User.PhoneNumber persistence` | Stores optional per-user SMS recipient number for notification delivery channels. | `src/Tabsan.EduSphere.Domain/Identity/User.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Migrations/20260518104000_Phase40_AddUserPhoneNumber.cs` |
| `NotificationRepository.GetActiveUserPhoneNumbersAsync` | Resolves distinct active recipient phone numbers by user IDs for SMS dispatch. | `src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `Admin user phone management contract` | Enables super-admin create/update/list admin-user flows to capture and return optional phone numbers. | `src/Tabsan.EduSphere.Application/DTOs/Auth/AdminUserManagementDtos.cs`, `src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs` |
| `User import optional PhoneNumber ingestion` | Supports optional `PhoneNumber` CSV header/value ingestion with validation while preserving backward compatibility for existing CSV files. | `src/Tabsan.EduSphere.Application/Services/UserImportService.cs`, `src/Tabsan.EduSphere.Application/Interfaces/IUserImportService.cs` |
| `Student self-registration phone capture` | Accepts optional phone number during self-registration and persists it on the created user account. | `src/Tabsan.EduSphere.Application/DTOs/Academic/AcademicDtos.cs`, `src/Tabsan.EduSphere.Application/Academic/StudentRegistrationService.cs` |

## 2026-05-18 - StudentLifecycle Notification TODO Completion (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `StudentLifecycle milestone notifications` | Sends system notifications to students on graduation, semester promotion, profile deactivation, and profile reactivation actions. | `src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs` |
| `StudentLifecycle admin-review request alerts` | Sends pending-review alerts to Admin/SuperAdmin users when profile-change and teacher-modification requests are created. | `src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs` |
| `StudentLifecycle request outcome notifications` | Sends approval/rejection outcomes to requestor/teacher for admin change requests and teacher modification requests. | `src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs` |

## 2026-05-18 - DeepScan Phase 40 Closure and Production Readiness Revalidation (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DeepScan re-execution evidence bundle` | Re-runs the post-remediation validation command bundle for previously open DeepScan gap areas and records pass outcomes for closure evidence. | `Docs/DeepScan.md`, `Docs/Consolidated-Execution-Enhancements-Issues.md` |
| `DeepScan task-by-task closure update` | Updates task statuses for checklist `4.1` through `4.20` after remediation validation rerun. | `Docs/DeepScan.md` |
| `DeepScan final readiness classification update` | Reclassifies issue severity and publishes final production-readiness go/no-go statement after closure validation. | `Docs/DeepScan.md`, `Project startup Docs/PRD.md`, `Docs/Consolidated-Execution-Enhancements-Issues.md` |

## 2026-05-18 - DeepScan Stage 39.4 EF Relationship and Query-Filter Warning Cleanup (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `EF dependent/principal filter alignment` | Aligns dependent entity query filters with filtered required principals to eliminate required-relationship/global-filter warning patterns. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/AcademicConfigurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/StudentAcademicConfigurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/AssignmentConfigurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/Phase9Configurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/PaymentReceiptConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/StudyPlanConfigurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/QuizConfigurations.cs` |
| `QuizQuestion explicit parent-navigation mapping` | Uses explicit `Quiz.Questions` mapping to remove shadow foreign-key ambiguity (`QuizId1`) in quiz model configuration. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/QuizConfigurations.cs` |
| `CourseType sentinel-warning removal` | Removes DB default-value configuration for `CourseType` to prevent EF enum sentinel/default warning behavior. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/AcademicConfigurations.cs` |

## 2026-05-18 - Documentation Synchronization Request (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Recent request documentation-sync issue capture` | Records the issue that mandatory execution/planning trackers required synchronized update closure for the latest request cycle. | `Project startup Docs/PRD.md`, `Docs/Consolidated-Execution-Enhancements-Issues.md` |
| `Cross-document implementation summary alignment` | Applies a common implementation summary narrative across PRD, consolidated issues, function registry, full functionality reference, development plan, and schema tracker. | `Project startup Docs/PRD.md`, `Docs/Consolidated-Execution-Enhancements-Issues.md`, `Docs/Function-List.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md` |
| `Cross-document validation summary alignment` | Confirms each updated tracker now includes dated implementation/validation closure and documents no runtime/schema mutation for this request. | `Project startup Docs/PRD.md`, `Docs/Consolidated-Execution-Enhancements-Issues.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md` |

## 2026-05-18 - DeepScan Gap Phase/Stage Synchronization Request (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DeepScan gap issue capture in execution tracker` | Records that DeepScan-identified partial/missing items required explicit phase/stage planning in governance documents. | `Docs/Consolidated-Execution-Enhancements-Issues.md`, `Project startup Docs/PRD.md` |
| `DeepScan remediation phase/stage planning insertion` | Adds executable staged remediation plan entries for waitlist, transactional import strict mode, MFA hardening, and EF warning cleanup. | `Docs/Consolidated-Execution-Enhancements-Issues.md` |
| `DeepScan request snapshot cross-doc synchronization` | Synchronizes issue, implementation summary, and validation summary entries across six mandatory planning/execution documents. | `Project startup Docs/PRD.md`, `Docs/Consolidated-Execution-Enhancements-Issues.md`, `Docs/Function-List.md`, `Docs/Complete-Functionality-Reference.md`, `Project startup Docs/Development Plan - ASP.NET.md`, `Project startup Docs/Database Schema.md` |

## 2026-05-18 - DeepScan Stage 39.2 Transactional CSV Import Strict Mode (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `UserImportService strict-mode rollback` | Prevents partial persistence by rolling back CSV user imports when any validation issue or duplicate row is detected in strict mode. | `src/Tabsan.EduSphere.Application/Services/UserImportService.cs` |
| `UserImportController strictMode query option` | Exposes strict/permissive import behavior through a query parameter while preserving backward-compatible default behavior. | `src/Tabsan.EduSphere.API/Controllers/UserImportController.cs` |
| `UserImportResult strict-mode flag` | Signals whether the import response was generated from strict or permissive execution. | `src/Tabsan.EduSphere.Application/DTOs/CsvImportDtos.cs` |

## 2026-05-18 - DeepScan Stage 39.1 Enrollment Waitlist and Seat-Promotion Workflow (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `EnrollmentStatus.Waitlisted` | Represents an over-capacity enrollment that is queued for promotion when a seat becomes available. | `src/Tabsan.EduSphere.Domain/Academic/Enrollment.cs` |
| `EnrollmentService waitlist promotion flow` | Creates waitlisted enrollments when a course offering is full and promotes the oldest waitlisted enrollment after a drop. | `src/Tabsan.EduSphere.Application/Academic/EnrollmentService.cs` |
| `IEnrollmentRepository.GetWaitlistedByOfferingAsync` | Returns waitlisted enrollments in queue order so promotion can be deterministic. | `src/Tabsan.EduSphere.Domain/Interfaces/IEnrollmentRepository.cs`, `src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `EnrollmentController waitlist queue endpoint` | Exposes the ordered waitlist queue to faculty and admins for course-offering review. | `src/Tabsan.EduSphere.API/Controllers/EnrollmentController.cs` |

## 2026-05-18 - DeepScan Stage 39.3 MFA Hardening (TOTP + Recovery Codes) (Execution Snapshot)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AuthService TOTP login challenge` | Replaces static demo-code MFA with per-user TOTP verification and one-time recovery-code fallback during login. | `src/Tabsan.EduSphere.Application/Auth/AuthService.cs` |
| `AuthController MFA enrollment endpoints` | Provides authenticated endpoints for MFA setup, enablement verification, and recovery-code regeneration. | `src/Tabsan.EduSphere.API/Controllers/AuthController.cs` |
| `TotpService RFC 6238 implementation` | Generates Base32 secrets, provisioning URIs, and validates TOTP codes with configurable drift windows. | `src/Tabsan.EduSphere.Infrastructure/Auth/TotpService.cs`, `src/Tabsan.EduSphere.Application/Interfaces/ITotpService.cs` |
| `User MFA persistence fields` | Persists per-user MFA enablement, TOTP secret, and hashed recovery-code set. | `src/Tabsan.EduSphere.Domain/Identity/User.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `src/Tabsan.EduSphere.Infrastructure/Migrations/20260518091500_Phase39_MfaTotpRecoveryCodes.cs` |

## Phase 36 - Deployment Readiness (2026-05-15)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Phase36-Release-Candidate-Manifest` | Pins the release-candidate baseline commit and defines deployable units, prerequisites, security/config flags, required secrets, and validation evidence for production readiness. | `Docs/Phase36-Release-Candidate-Manifest.md` |
| `Stage 36.1 baseline freeze status` | Records completion state with implementation and validation summaries for release-candidate freeze execution. | `Docs/Consolidated-Execution-Enhancements-Issues.md` |
| `Phase36-Validate-Environment-Readiness.ps1` | Validates Stage 36.2 config parity and production secret readiness with optional fail-on-issues gate and markdown evidence output. | `Scripts/Phase36-Validate-Environment-Readiness.ps1` |
| `Production startup placeholder guardrails` | Fails non-development startup when critical deployment settings still contain unsafe placeholder values. | `src/Tabsan.EduSphere.API/Program.cs`, `src/Tabsan.EduSphere.Web/Program.cs`, `src/Tabsan.EduSphere.BackgroundJobs/Program.cs` |
| `Stage 36.2 readiness evidence reports` | Captures baseline and strict environment-readiness validation results for deployment gate traceability. | `Artifacts/Phase36/Stage36.2/Environment-Readiness-20260515-100414.md`, `Artifacts/Phase36/Stage36.2/Environment-Readiness-20260515-100417.md` |
| `Phase36-Deployment-Rehearsal.ps1` | Orchestrates Stage 36.3 deployment rehearsal with selectable `DeploymentMode` (`Demo` or `Clean`) and includes Stage 34 utility steps. | `Scripts/Phase36-Deployment-Rehearsal.ps1` |
| `Seed-Core-Clean.sql` | Seeds clean startup baseline only (roles, single SuperAdmin user, baseline departments/modules/settings/access) without dummy/demo records. | `Scripts/Seed-Core-Clean.sql` |
| `05-PostDeployment-Checks-Clean.sql` | Validates strict clean-baseline integrity and raises failure when dummy-domain data is present. | `Scripts/05-PostDeployment-Checks-Clean.sql` |
| `Phase34-Rollback-Safe-Deployment.ps1 deployment modes` | Executes rollback-safe SQL deployment sequence with selectable `DeploymentMode` for demo chain or clean chain using pre-backup and automatic restore-on-failure behavior. | `Scripts/Phase34-Rollback-Safe-Deployment.ps1` |
| `Stage 36.3 rehearsal report` | Captures the dry-run deployment rehearsal evidence for the required script order and rollback/drill utilities. | `Artifacts/Phase36/Stage36.3/Deployment-Rehearsal-20260515-101150.md` |
| `Phase36-Security-Reliability-Performance-Gates.ps1` | Orchestrates the Stage 36.4 hardening, dashboard-visibility, health, performance, and backup/restore evidence gates. | `Scripts/Phase36-Security-Reliability-Performance-Gates.ps1` |
| `Phase36Stage4HealthAndLicenseGateTests` | Verifies public health snapshots, metrics output, and module-license blocking on a sensitive route. | `tests/Tabsan.EduSphere.IntegrationTests/Phase36Stage4HealthAndLicenseGateTests.cs` |
| `Stage 36.4 gate report` | Captures the combined Stage 36.4 security, reliability, performance, and backup/restore evidence results. | `Artifacts/Phase36/Stage36.4/Security-Reliability-Performance-Gates-20260515.md` |
| `Phase36-Deployment-Rollback-Runbook` | Defines deployment-day ownership, escalation path, rollback thresholds, communications plan, and post-deploy validation script order for production go-live. | `Docs/Phase36-Deployment-Rollback-Runbook.md` |
| `Phase36 Stage36.5 UAT/SAT Approval Pack` | Records final role-based UAT/SAT outcomes and operational readiness approvals for Stage 36.5 sign-off. | `UAT-SAT docs/Phase36-Stage36.5-Approval-Pack.md` |
| `Stage 36.5 operational sign-off evidence` | Captures the final Stage 36.5 approval evidence and references to runbook and prior gate artifacts. | `Artifacts/Phase36/Stage36.5/UAT-SAT-Operational-SignOff-20260515.md` |
| `Phase36-GoLive-Hypercare.ps1` | Executes/plans Stage 36.6 go-live smoke checks and produces hypercare checkpoint evidence. | `Scripts/Phase36-GoLive-Hypercare.ps1` |
| `Phase36-GoLive-Hypercare-Plan` | Defines Stage 36.6 incident triage board, SLO/error-rate guardrails, and 24/48/72-hour hypercare checkpoints. | `Docs/Phase36-GoLive-Hypercare-Plan.md` |
| `Stage 36.6 go-live hypercare evidence` | Captures Stage 36.6 execution readiness and hypercare activation report. | `Artifacts/Phase36/Stage36.6/GoLive-Hypercare-20260515.md` |
| `Phase37-Separate-App-And-License-Publish.ps1` | Publishes runtime app targets separately from Tabsan.Lic to enforce independent publish outputs. | `Scripts/Phase37-Separate-App-And-License-Publish.ps1` |
| `Phase 37 publish separation evidence` | Captures execute-mode app-vs-license publish separation results and generated package outputs. | `Artifacts/Phase37/Publish-Separation-20260515.md` |
| `Phase38-Separate-NonRuntime-Assets.ps1` | Packages Docs/PPT/project-doc/user-guide/script assets separately from runtime app publish outputs. | `Scripts/Phase38-Separate-NonRuntime-Assets.ps1` |
| `Phase 38 non-runtime separation evidence` | Captures execute-mode separation status and generated non-runtime package for all requested folders. | `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md` |
| `Phase37-Phase38-Publish-Separation` | Defines final publish separation governance across runtime app, license app, and non-runtime assets. | `Docs/Phase37-Phase38-Publish-Separation.md` |

## Phase 35 - In-App User Import UX Completion (2026-05-15)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AdminUsers import entry action` | Adds direct `Import Users` entry from Admin user management page to keep create/manage/import flow contiguous. | `src/Tabsan.EduSphere.Web/Views/Portal/AdminUsers.cshtml` |
| `PortalController.UserImportTemplate(fileName)` | Serves approved CSV template files from `User Import Sheets/` via filename allow-list with traversal-safe path resolution. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `User Import template guidance block` | Displays template download links, required/optional columns, accepted file type, and role-value guidance before upload. | `src/Tabsan.EduSphere.Web/Views/Portal/UserImport.cshtml` |
| `User Import row-level validation table` | Renders post-upload row-level validation issues in a structured Row/Issue table for actionable correction and re-upload. | `src/Tabsan.EduSphere.Web/Views/Portal/UserImport.cshtml` |

## Institute Parity Stage Update Governance (2026-05-13)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Institute-Parity Stage Closeout Protocol` | Enforces that each completed stage records both `Implementation Summary` and `Validation Summary` before progressing. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Institute-Parity Stage Completion Template` | Standardizes per-stage completion entries for repeatable implementation/validation evidence capture. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Institute-Parity Mandatory Documentation Sync` | Requires synchronized updates to Function-List, Complete Functionality Reference, Database Schema, Development Plan, PRD, and Command after each stage completion. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Institute-Parity Git Sync Order` | Enforces commit -> pull --rebase -> push sequence after each stage closeout. | `Docs/Institute-Parity-Issue-Fix-Phases.md`, `Docs/Command.md` |
| `Stage 0.1 Module Parity Audit Baseline` | Captures module-by-module endpoint/service/repository/UI/DB dependency inventory for School/College/University parity planning. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `University-Default Hotspot Inventory` | Tracks University-centric defaults/labels/templates discovered during Stage 0.1 for Phase 1-4 remediation. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Stage 0.2 Role-Institute Access Matrix` | Defines baseline role x institute x module x action coverage and identifies scope-enforcement gaps for parity fixes. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Scope Guard Baseline (Dept/Offering/Ownership)` | Documents current enforcement basis used by major module endpoints before institute-specific hardening. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Stage 0.3 Report Failure Inventory` | Tracks report/analytics failures by root-cause tag and maps each to remediation stages. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Report Guardrail Outcome Classification` | Distinguishes expected scope-guard failures (400/403/404) from true report defects to reduce false-positive bug triage. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Stage 0.4 Phase-0 Exit Signoff` | Consolidates Phase 0 audit outputs into prioritized remediation backlog and confirms readiness for Phase 1 execution. | `Docs/Institute-Parity-Issue-Fix-Phases.md` |
| `Department.InstitutionType` canonical institute dimension | Anchors School/College/University parity at department level so downstream academic entities inherit institute scope consistently. | `src/Tabsan.EduSphere.Domain/Academic/Department.cs` |
| `DepartmentConfiguration` institute-type persistence/indexing | Persists department institution type with default University mode and adds `IX_departments_institution_type` query index. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs` |
| `Phase1Stage11DepartmentInstitutionType` migration | Adds `departments.InstitutionType` column and index to normalize institute dimension in schema. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Migrations/20260513121000_Phase1Stage11DepartmentInstitutionType.cs` |
| `DepartmentController` institution-policy enforcement for departments | Validates create/update institution type assignments against current license policy and returns institution type in department payloads. | `src/Tabsan.EduSphere.API/Controllers/DepartmentController.cs` |
| `EduApiClient` department institution-type round-trip | Carries institution type in portal department reads and preserves backward-compatible create/update payload behavior. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `Program code department-scope uniqueness` | Enforces academic program code uniqueness within department scope (`Code + DepartmentId`) instead of global code uniqueness. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/AcademicConfigurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicRepositories.cs`, `src/Tabsan.EduSphere.API/Controllers/ProgramController.cs` |
| `CourseController` offering scope integrity checks | Validates department existence, course/semester existence, and faculty active department assignment before creating course offerings. | `src/Tabsan.EduSphere.API/Controllers/CourseController.cs` |
| `StudentRegistrationService` program-department alignment guard | Prevents StudentProfile creation when selected program and department do not belong to the same academic scope. | `src/Tabsan.EduSphere.Application/Academic/StudentRegistrationService.cs` |
| `Phase1Stage12ReferentialIntegrityAndIndexes` migration | Applies Stage 1.2 index coverage and enrollment-status column normalization for institute/report scoped query performance. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Migrations/20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes.cs` |
| `Stage 1.2 academic/report index pack` | Adds composite indexes for programs, courses, offerings, student profiles, enrollments, and assignment lookup paths used in parity filtering and analytics/report joins. | `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/AcademicConfigurations.cs`, `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/StudentAcademicConfigurations.cs` |
| `Stage 1.3 schema replay hardening` | Adds idempotent Stage 1.1/1.2 parity DDL blocks and migration-history updates for script-first deployments. | `Scripts/01-Schema-Current.sql` |
| `Stage 1.3 maintenance parity index guardrails` | Ensures critical institute/report parity indexes exist and safely replaces legacy program-code index during maintenance runs. | `Scripts/04-Maintenance-Indexes-And-Views.sql` |
| `Stage 1.3 post-deployment parity verification` | Adds explicit checks for parity migration IDs, column shape, and critical index existence in deployment verification output. | `Scripts/05-PostDeployment-Checks.sql` |
| `Stage 1.4 parity exit criteria checks` | Adds institute-type coverage/validity checks and orphan-count checks for institute-linked entities to close Phase 1 with measurable integrity validation. | `Scripts/05-PostDeployment-Checks.sql` |
| `DepartmentController faculty assignment management endpoints` | Enables SuperAdmin to assign/remove/list faculty department assignments and retrieve faculty candidates for assignment workflows. | `src/Tabsan.EduSphere.API/Controllers/DepartmentController.cs` |
| `Department assignment institute-compatibility guards` | Blocks admin/faculty department assignments when user institution type conflicts with the target department institution type. | `src/Tabsan.EduSphere.API/Controllers/DepartmentController.cs` |
| `RemoveFacultyFromDepartmentRequest` | Adds request contract for revoking faculty department assignment through API. | `src/Tabsan.EduSphere.Application/DTOs/Academic/AcademicDtos.cs` |
| `Stage 2.1 SuperAdmin assignment integration checks` | Validates SuperAdmin faculty assignment round-trip and admin assignment institution-mismatch rejection. | `tests/Tabsan.EduSphere.IntegrationTests/AdminUserManagementIntegrationTests.cs` |
| `TokenService institution claim emission` | Emits `institutionType` claim for users with explicit institution assignment so handlers can enforce institute-scoped authorization without extra policy layers. | `src/Tabsan.EduSphere.Infrastructure/Auth/TokenService.cs` |
| `ReportController institute-scope enforcement` | Composes role scope (admin assignment/faculty ownership) with institution-type scope checks on report endpoints for Admin/Faculty callers. | `src/Tabsan.EduSphere.API/Controllers/ReportController.cs` |
| `ReportExportsIntegrationTests institute mismatch guard` | Verifies admin requests are forbidden when department access exists but institution claim mismatches target department institution. | `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs` |
| `JwtTestHelper institutionType claim support` | Supports emitting optional `institutionType` JWT claim for integration authorization-path validation. | `tests/Tabsan.EduSphere.IntegrationTests/Infrastructure/JwtTestHelper.cs` |
| `PortalController menu/action guard` | Enforces sidebar-driven menu visibility checks on portal actions so direct URL access cannot bypass hidden menu restrictions. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `Portal action-to-menu key map` | Defines canonical mapping between portal actions and sidebar menu keys for consistent navigation authorization behavior. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `Sidebar settings guard consistency integration tests` | Verifies hidden menu state and endpoint authorization outcomes remain aligned for Admin and SuperAdmin roles. | `tests/Tabsan.EduSphere.IntegrationTests/SidebarMenuIntegrationTests.cs` |
| `Stage 2.4 authorization matrix exit validation` | Confirms Phase 2 role + institute access matrix passes end-to-end by running assignment, report-scope, and menu/action guard integration suites together. | `Docs/Institute-Parity-Issue-Fix-Phases.md`, `tests/Tabsan.EduSphere.IntegrationTests/AdminUserManagementIntegrationTests.cs`, `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs`, `tests/Tabsan.EduSphere.IntegrationTests/SidebarMenuIntegrationTests.cs` |
| `Portal department institution-type create/edit parity` | Ensures department create/edit flows in the portal surface and submit explicit institution type values for School/College/University parity. | `src/Tabsan.EduSphere.Web/Views/Portal/Departments.cshtml`, `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `EduApiClient department institution-type write contract` | Removes silent University-only write behavior by requiring explicit institution type for department create/update operations. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `Stage 3.1 cross-institution department/course CRUD integration` | Validates department/course CRUD paths across School/College/University when policy enables all institution types, and restores policy after execution. | `tests/Tabsan.EduSphere.IntegrationTests/AdminUserManagementIntegrationTests.cs` |
| `Admin assignment round-trip institution-compatible department selector` | Hardens existing admin assignment round-trip integration test to select/create a department compatible with assigned institution type in mixed datasets. | `tests/Tabsan.EduSphere.IntegrationTests/AdminUserManagementIntegrationTests.cs` |
| `StudentLifecycleController institute-aware admin lifecycle scope guard` | Enforces admin department-assignment and institution-type compatibility checks on lifecycle read/mutation endpoints while preserving SuperAdmin global scope. | `src/Tabsan.EduSphere.API/Controllers/StudentLifecycleController.cs` |
| `SessionIdentity institution claim projection` | Projects optional `institutionType` from JWT payload into web session identity to enable institute-aware portal filtering behavior. | `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs`, `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `Portal Student Lifecycle institute-filtered department selector` | Filters lifecycle department options by caller institution type and prevents out-of-scope selection execution in portal lifecycle flows. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `Portal Student Lifecycle action route-state preservation` | Keeps selected lifecycle department/semester state during promote/graduate actions and fixes per-row graduate posting behavior. | `src/Tabsan.EduSphere.Web/Views/Portal/StudentLifecycle.cshtml` |
| `Stage 3.2 lifecycle institute mismatch integration checks` | Validates that Admin lifecycle graduation-candidate and promote paths return forbidden on institution mismatch despite department assignment. | `tests/Tabsan.EduSphere.IntegrationTests/StudentLifecycleIntegrationTests.cs` |
| `StudentController admin assignment + institution submenu scope` | Enforces department-assignment and institution-claim scope on student listing endpoint used by student-related submenu data sources. | `src/Tabsan.EduSphere.API/Controllers/StudentController.cs` |
| `Student submenu institute-neutral level labels` | Replaces University-specific semester-only wording with institute-neutral level wording in student submenu tables. | `src/Tabsan.EduSphere.Web/Views/Portal/Students.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/Enrollments.cshtml` |
| `Stage 3.3 student submenu institute-parity integration checks` | Validates student list endpoint denies mismatched institute department requests and returns institute-compatible students for admin scope. | `tests/Tabsan.EduSphere.IntegrationTests/StudentSubmenuParityIntegrationTests.cs` |
| `LookupItem institution-type parity contract` | Adds optional institution-type metadata to shared portal lookup model so institute-aware lifecycle/student filters compile and execute consistently. | `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs` |
| `Stage 3.4 phase-exit full integration validation` | Confirms consolidated Phase 3 parity behavior by running full integration suite and solution build as exit-criteria evidence. | `Docs/Institute-Parity-Issue-Fix-Phases.md`, `tests/Tabsan.EduSphere.IntegrationTests/*.cs` |
| `AnalyticsController role-aware institute scope resolver` | Applies institute claim defaults and mismatch-deny checks for analytics query/export requests while validating department/institute compatibility. | `src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs` |
| `AnalyticsService institution-type query filter support` | Extends performance/attendance/assignment/quiz analytics queries and cache keys with optional institution-type scope filtering. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs`, `src/Tabsan.EduSphere.Application/Interfaces/IAnalyticsService.cs` |
| `Portal analytics institute + department filters` | Adds analytics dashboard filter controls and role-aware filter defaults for institute-safe dashboard behavior. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs`, `src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml`, `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs` |
| `EduApiClient analytics scoped query support` | Sends analytics filter query parameters (`departmentId`, `institutionType`) from portal to API analytics endpoints. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `Stage 4.1 analytics institute parity integration checks` | Verifies analytics mismatch-deny and claim-default institute scoping behavior for admin analytics access. | `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsInstituteParityIntegrationTests.cs` |
| `ReportController effective report scope resolver` | Adds role-aware institution filter resolution for report requests, auto-scoping constrained roles to claim institute and denying explicit mismatches. | `src/Tabsan.EduSphere.API/Controllers/ReportController.cs` |
| `Report DTO and repository institution filter threading` | Extends report request/repository contracts so institutionType filters propagate through report service and report query paths. | `src/Tabsan.EduSphere.Application/DTOs/Reports/ReportDtos.cs`, `src/Tabsan.EduSphere.Domain/Interfaces/IReportRepository.cs`, `src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs`, `src/Tabsan.EduSphere.Infrastructure/Reporting/ReportRepository.cs` |
| `Queued report export institution scope preservation` | Extends queued result-summary export payload and worker request generation to keep institution scope in async exports. | `src/Tabsan.EduSphere.API/Services/ReportExportJobQueue.cs`, `src/Tabsan.EduSphere.API/Services/ReportExportJobWorker.cs` |
| `Portal report institution filter controls` | Adds institution filter controls and scoped department narrowing across report pages and report action handlers. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs`, `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportAttendance.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportResults.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportAssignments.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportQuizzes.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportGpa.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportEnrollment.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportSemesterResults.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportLowAttendance.cshtml`, `src/Tabsan.EduSphere.Web/Views/Portal/ReportFypStatus.cshtml` |
| `EduApiClient report institution query support` | Adds `institutionType` support to report read/export methods and query builders used by portal report pages. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `Stage 4.2 report institute parity integration checks` | Verifies report institute-filter scoping and explicit mismatch-deny behavior in report integration suite. | `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs` |
| `ReportController faculty department-scope guardrails` | Enforces faculty department/department-or-offering report scope on GPA, enrollment, semester-results, low-attendance, and FYP status report endpoints. | `src/Tabsan.EduSphere.API/Controllers/ReportController.cs` |
| `ReportController faculty offering assignment fallback` | Replaces strict offering owner check with faculty department-assignment scope validation to avoid false report forbids for assigned offerings. | `src/Tabsan.EduSphere.API/Controllers/ReportController.cs` |
| `Stage 4.3 report reliability integration checks` | Adds deterministic faculty report scope tests for required filters and unassigned-department denial behavior on repaired report routes. | `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs` |
| `Stage 4.4 phase-exit full integration validation` | Confirms Phase 4 report and analytics parity closure with a full integration-suite regression gate. | `tests/Tabsan.EduSphere.IntegrationTests/*.cs` |
| `Core seed institution policy flags` | Seeds default institution-mode policy keys (`institution_include_school|college|university`) in `portal_settings` with idempotent upsert behavior. | `Scripts/02-Seed-Core.sql` |
| `Core seed institute baseline departments` | Seeds deterministic School, College, and University baseline departments with explicit `InstitutionType` values for parity-safe foundational coverage. | `Scripts/02-Seed-Core.sql` |
| `Core seed report key normalization + parity report set` | Normalizes legacy hyphenated report keys to current underscore keys and seeds the full report definition matrix used by current API/report catalog behavior. | `Scripts/02-Seed-Core.sql` |
| `Core seed report role-access parity matrix` | Seeds report-role assignments aligned with policy matrix: SuperAdmin/Admin/Faculty for operational reports and Student access for transcript only. | `Scripts/02-Seed-Core.sql` |
| `Core seed sidebar SuperAdmin access alignment` | Adds explicit SuperAdmin allowed rows for baseline sidebar menu access to align seeded role matrix with policy expectations. | `Scripts/02-Seed-Core.sql` |
| `Full dummy institute assignment alignment` | Assigns deterministic `InstitutionType` values to parity users and enforces department institution mapping updates for replay-safe School/College/University representative coverage. | `Scripts/03-FullDummyData.sql` |
| `Full dummy assignment scope seeds` | Seeds admin and faculty department-assignment junction rows for deterministic role/institute scope test baselines. | `Scripts/03-FullDummyData.sql` |
| `Full dummy timetable infrastructure coverage` | Seeds buildings, rooms, timetables, and timetable entries across School/College/University representative departments. | `Scripts/03-FullDummyData.sql` |
| `Full dummy payment + report artifact coverage` | Seeds payment receipts and transcript export logs for parity testing of finance and report-export workflows. | `Scripts/03-FullDummyData.sql` |
| `Full dummy lifecycle parity artifacts` | Seeds bulk promotion batches/entries, graduation applications/approvals, student report cards, school streams, and stream assignments for lifecycle parity scenarios. | `Scripts/03-FullDummyData.sql` |
| `Full dummy deterministic replay alignment` | Reconciles seeded department and user key fields on reruns to keep deterministic parity data shape stable across replay executions. | `Scripts/03-FullDummyData.sql` |
| `Post-deployment institute parity aggregate checks` | Adds institute-level row-count outputs for users, student profiles, timetables, and payment receipts by School/College/University. | `Scripts/05-PostDeployment-Checks.sql` |
| `Post-deployment critical workflow coverage checks` | Adds aggregate presence checks for parity-critical entities (assignments, timetable entries, payments, transcript exports, promotion/graduation/report-card artifacts). | `Scripts/05-PostDeployment-Checks.sql` |
| `Post-deployment replay-safety duplicate checks` | Adds duplicate-detection outputs for usernames and registration numbers plus dataset-version single-row integrity check. | `Scripts/05-PostDeployment-Checks.sql` |
| `Full dummy superadmin identity reuse` | Reuses existing superadmin identity when present to keep replay runs deterministic and prevent duplicate-email failures across mixed baseline environments. | `Scripts/03-FullDummyData.sql` |
| `Phase 5 one-run deployment chain gate` | Validates end-to-end script readiness by executing schema, core seed, full dummy seed, and post-deployment checks in the required deployment order. | `Scripts/01-Schema-Current.sql`, `Scripts/02-Seed-Core.sql`, `Scripts/03-FullDummyData.sql`, `Scripts/05-PostDeployment-Checks.sql` |
| `Stage 6.1 lifecycle institute parity success-path check` | Verifies Admin lifecycle graduation-candidates access succeeds when department assignment and institution scope match. | `tests/Tabsan.EduSphere.IntegrationTests/StudentLifecycleIntegrationTests.cs` |
| `Stage 6.1 student submenu explicit department scope check` | Verifies Admin student-list requests with explicit department filter return only students from the requested in-scope department. | `tests/Tabsan.EduSphere.IntegrationTests/StudentSubmenuParityIntegrationTests.cs` |
| `Stage 6.1 report enrollment-summary matched-institution check` | Verifies Admin enrollment-summary report access succeeds when institution query matches caller institution claim and assigned department scope. | `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs` |
| `Stage 6.1 focused parity regression gate` | Executes targeted lifecycle, submenu, and report institute-parity integration suites as automated regression evidence. | `tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj` |
| `Stage 6.2 cross-role report catalog matrix` | Validates report-catalog role visibility behavior for SuperAdmin/Admin/Faculty/Student across School/College/University institution claims. | `tests/Tabsan.EduSphere.IntegrationTests/CrossRoleUatMatrixIntegrationTests.cs` |
| `Stage 6.2 cross-role account-security matrix` | Validates locked-account endpoint role boundaries across School/College/University institution claim contexts. | `tests/Tabsan.EduSphere.IntegrationTests/CrossRoleUatMatrixIntegrationTests.cs` |
| `Stage 6.2 cross-role attendance authorization matrix` | Validates attendance-by-offering authorization outcomes for privileged roles vs Student across School/College/University institution claims. | `tests/Tabsan.EduSphere.IntegrationTests/CrossRoleUatMatrixIntegrationTests.cs` |
| `Stage 6.2 focused cross-role UAT regression gate` | Executes cross-role UAT matrix plus authorization/report baseline suites as Stage 6.2 evidence. | `tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj` |
| `Stage 6.3 parity index read-usage validation` | Validates read-path index activity for institute-filtered queries using parity indexes on programs, courses, and offerings. | `tests/Tabsan.EduSphere.IntegrationTests/PerformanceQueryValidationIntegrationTests.cs` |
| `Stage 6.3 dashboard/report latency budget validation` | Validates no-major-regression latency budgets on common parity-sensitive dashboard/report endpoints for Admin callers. | `tests/Tabsan.EduSphere.IntegrationTests/PerformanceQueryValidationIntegrationTests.cs` |
| `Stage 6.3 focused performance/query regression gate` | Executes Stage 6.3 performance and query validation suite for parity evidence. | `tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj` |
| `Stage 6.4 consolidated parity exit gate` | Executes combined Stage 6.1/6.2/6.3 regression suites as Phase 6 exit certification evidence across role and institution matrices. | `tests/Tabsan.EduSphere.IntegrationTests` |
| `Stage 7.1 deployment run-order finalization` | Finalizes deterministic database deployment order, environment notes, and operator prerequisites for parity rollout. | `Scripts/README.md` |
| `Stage 7.1 rollback and verification checklist` | Defines backup, failure handling, cleanup fallback, and post-deployment evidence checklist for operational readiness. | `Scripts/README.md` |
| `Stage 7.2 institute parity functionality guidance` | Documents role/institute parity behavior and scoped access model in functionality reference content. | `Docs/Functionality.md` |
| `Stage 7.2 role/institute user-guide filter guidance` | Documents role-specific institute filtering behavior and expected access boundaries for Admin, Faculty, Student, and shared guide entry points. | `User Guide/README.md`, `User Guide/Admin-Guide.md`, `User Guide/Faculty-Guide.md`, `User Guide/Student-Guide.md` |
| `Stage 7.3 institute parity monitoring guidance` | Documents report/analytics failure monitoring points and institute-scope incident triage priorities. | `Docs/Functionality.md` |
| `Stage 7.3 support handover checklist` | Defines SuperAdmin parity-scope incident triage checklist for report/analytics and menu/route support handover. | `User Guide/SuperAdmin-Guide.md` |
| `Stage 7.4 release exit criteria snapshot` | Consolidates Phase 7 release-readiness evidence, confirms tracker synchronization, and advances the execution pointer to the next roadmap stage. | `Docs/Institute-Parity-Issue-Fix-Phases.md`, `Docs/Command.md` |
| `Phase 23 Stage 23.1 institution-mode foundation confirmation` | Confirms global School/College/University mode support, persistence, and backward-compatible University default behavior before Stage 23.2. | `Docs/Advance-Enhancements.md`, `src/Tabsan.EduSphere.Application/Interfaces/IInstitutionPolicyService.cs`, `src/Tabsan.EduSphere.Application/Services/InstitutionPolicyService.cs`, `Scripts/02-Seed-Core.sql` |
| `Phase 23 Stage 23.2 dynamic academic vocabulary resolution` | Resolves tenant policy-based academic labels (Period, Progression, Grading, Course, StudentGroup) for School/College/University modes through centralized label service. | `src/Tabsan.EduSphere.Application/Interfaces/ILabelService.cs`, `src/Tabsan.EduSphere.Application/Services/LabelService.cs` |
| `Phase 23 Stage 23.2 label vocabulary API endpoint` | Exposes authenticated vocabulary retrieval endpoint for current institution policy context via `GET /api/v1/labels`. | `src/Tabsan.EduSphere.API/Controllers/LabelController.cs` |
| `Phase 23 Stage 23.2 portal vocabulary consumption` | Provides web client contract and portal module composition rendering for dynamic policy-aware labels without duplicating workflow views. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs`, `src/Tabsan.EduSphere.Web/Views/Portal/ModuleComposition.cshtml`, `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` |
| `Phase 24 Stage 24.1 license-flag API integration validation` | Verifies institution policy GET role accessibility, PUT SuperAdmin-only restriction, all-false rejection, and valid persistence/read-back behavior for license mode flags. | `tests/Tabsan.EduSphere.IntegrationTests/InstitutionPolicyLicenseFlagsIntegrationTests.cs` |
| `Phase 24 Stage 24.2 module backend license-enforcement middleware` | Centralizes API module gating by route-prefix-to-module mapping and blocks disabled module requests with `403 Forbidden` before controller execution. | `src/Tabsan.EduSphere.API/Middleware/ModuleLicenseEnforcementMiddleware.cs`, `src/Tabsan.EduSphere.API/Program.cs` |
| `Phase 24 Stage 24.2 backend enforcement integration validation` | Validates disabled module APIs are blocked with `403` across representative modules (courses, reports, ai_chat, fyp). | `tests/Tabsan.EduSphere.IntegrationTests/ModuleBackendEnforcementIntegrationTests.cs` |
| `Phase 24 Stage 24.3 module-aware sidebar visibility filtering` | Filters current-user sidebar menus by module activation state (in addition to role/status) so disabled modules are hidden from navigation payloads. | `src/Tabsan.EduSphere.API/Controllers/SidebarMenuController.cs` |
| `Phase 24 Stage 24.3 sidebar navigation filtering integration validation` | Verifies disabled courses/reports/themes modules are removed from `my-visible` sidebar output and preserves deterministic module-state setup/restore for sidebar test reliability. | `tests/Tabsan.EduSphere.IntegrationTests/SidebarMenuIntegrationTests.cs` |
| `Phase 25 Stage 25.1 academic-level lifecycle API route` | Adds institution-agnostic academic-level student retrieval route with backward-compatible semester-route aliasing for lifecycle progression screens. | `src/Tabsan.EduSphere.API/Controllers/StudentLifecycleController.cs` |
| `Phase 25 Stage 25.1 lifecycle service academic-level contract` | Adds academic-level retrieval service contract and compatibility implementation over existing semester-backed storage. | `src/Tabsan.EduSphere.Application/Interfaces/IStudentLifecycleService.cs`, `src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs` |
| `Phase 25 Stage 25.1 portal period-label lifecycle rendering` | Uses policy-driven vocabulary (`Semester`/`Grade`/`Year`) in Student Lifecycle controls and headings, and consumes academic-level lifecycle endpoint in web client/controller flow. | `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs`, `src/Tabsan.EduSphere.Web/Views/Portal/StudentLifecycle.cshtml`, `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs`, `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `Phase 25 Stage 25.1 academic-level lifecycle integration validation` | Verifies scoped admin access to academic-level student lifecycle endpoint in integration tests. | `tests/Tabsan.EduSphere.IntegrationTests/StudentLifecycleIntegrationTests.cs` |
| `Phase 25 Stage 25.2 stream assignment eligibility enforcement` | Enforces School-only Grade 9-12 stream assignment and blocks inactive stream assignment requests in lifecycle stream service. | `src/Tabsan.EduSphere.Application/Academic/SchoolStreamService.cs` |
| `Phase 25 Stage 25.2 stream subject filtering for student offerings` | Applies stream-keyword subject filtering for School Grade 9-12 student offering endpoints with compatibility fallback for legacy course naming. | `src/Tabsan.EduSphere.Application/Academic/SchoolStreamSubjectFilter.cs`, `src/Tabsan.EduSphere.API/Controllers/CourseController.cs` |
| `Phase 25 Stage 25.2 stream rule unit validation` | Validates stream assignment rejection for non-school context and out-of-range grades, plus successful assignment behavior for eligible students. | `tests/Tabsan.EduSphere.UnitTests/Phase26Tests.cs` |
| `Phase 25 Stage 25.3 school promotion pass-rule enforcement` | Routes School lifecycle promotion through progression service so grade promotion is blocked when threshold criteria are not met. | `src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs` |
| `Phase 25 Stage 25.3 percentage normalization for progression` | Normalizes School/College progression scores from legacy GPA-scale values into percentage equivalents for pass-threshold evaluation. | `src/Tabsan.EduSphere.Application/Academic/ProgressionService.cs` |
| `Phase 25 Stage 25.3 student progression claim compatibility` | Accepts `studentProfileId` claim with fallback to `student_profile_id` for student self-progression endpoint compatibility. | `src/Tabsan.EduSphere.API/Controllers/ProgressionController.cs` |
| `Phase 25 Stage 25.3 school promotion integration validation` | Verifies School-mode lifecycle promotion returns `400 BadRequest` when pass criteria are not met. | `tests/Tabsan.EduSphere.IntegrationTests/StudentLifecycleIntegrationTests.cs` |
| `Phase 23 Stage 23.2 integration parity test suite` | Validates dynamic label API behavior across University/School/College and mixed-mode precedence, authentication boundary, consistency, and web-layer context readiness. | `tests/Tabsan.EduSphere.IntegrationTests/DynamicLabelIntegrationTests.cs`, `tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj` |
| `Phase 23 Stage 23.2 tracker synchronization` | Confirms Stage 23.2 completion details are synchronized in planning/hand-off docs including functionality reference updates. | `Docs/Advance-Enhancements.md`, `Docs/Functionality.md`, `Docs/Command.md`, `Project startup Docs/PRD.md`, `Project startup Docs/Database Schema.md`, `Project startup Docs/Development Plan - ASP.NET.md` |
| `Phase 31 Stage 31.1 institution report sections endpoint` | Exposes institution-aware report section partitions with claim-scoped institution resolution and optional SuperAdmin override via `GET /api/v1/reports/sections`. | `src/Tabsan.EduSphere.API/Controllers/ReportController.cs` |
| `Phase 31 Stage 31.1 report section response contracts` | Defines reporting DTO contracts for institution model metadata, section envelopes, and report items consumed by report-section clients. | `src/Tabsan.EduSphere.Application/DTOs/Reports/ReportDtos.cs` |
| `Phase 31 Stage 31.1 report sections integration validation` | Verifies School override behavior and claim-scoped College section selection for the report sections API. | `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs` |
| `Phase 31 Stage 31.2 advanced analytics response contracts` | Defines top-performer, performance-trend, and comparative-summary response models for analytics expansion endpoints. | `src/Tabsan.EduSphere.Application/DTOs/Analytics/AnalyticsDtos.cs` |
| `Phase 31 Stage 31.2 advanced analytics service contract` | Adds top-performer, trend, and comparative-summary operations to analytics service contract for API-layer composition. | `src/Tabsan.EduSphere.Application/Interfaces/IAnalyticsService.cs` |
| `Phase 31 Stage 31.2 advanced analytics computation engine` | Implements cached top-performer ranking, daily performance trends, and comparative department metrics in analytics infrastructure service. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs` |
| `Phase 31 Stage 31.2 analytics expansion API endpoints` | Exposes Stage 31.2 analytics routes for top performers, trends, and comparative summaries with existing role/institution scope enforcement. | `src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs` |
| `Phase 31 Stage 31.2 analytics parity integration validation` | Verifies institution-claim auto-scope behavior for Stage 31.2 analytics endpoints and stabilizes tests with reports-module activation guard. | `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsInstituteParityIntegrationTests.cs` |
| `Phase 31 Stage 31.3 analytics export conventions` | Standardizes analytics export file naming, content type, and extension mapping for sync and queued export paths. | `src/Tabsan.EduSphere.API/Services/AnalyticsExportJobQueue.cs` |
| `Phase 31 Stage 31.3 advanced analytics export endpoints` | Adds PDF/Excel export endpoints for top performers, performance trends, and comparative summary reports with standardized file metadata. | `src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs` |
| `Phase 31 Stage 31.3 advanced analytics export worker support` | Extends queued analytics export worker to process advanced analytics report families under standardized export conventions. | `src/Tabsan.EduSphere.API/Services/AnalyticsExportJobWorker.cs` |
| `Phase 31 Stage 31.3 advanced analytics export generators` | Adds PDF/Excel generators for top performers, performance trends, and comparative summary analytics outputs. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs` |
| `Phase 31 Stage 31.3 analytics export metadata integration validation` | Verifies standardized content-type and filename conventions across analytics export endpoints for all report families and formats. | `tests/Tabsan.EduSphere.IntegrationTests/AnalyticsExportsIntegrationTests.cs` |
| `Phase 32.1 AI chatbot floating entry planning` | Defines roadmap scope to replace menu-only chatbot entry with an always-visible floating launcher and responsive open behavior. | `Docs/Enhancements.md`, `Docs/Advance-Enhancements.md` |
| `Phase 32.1 chatbot launcher UX implementation checklist` | Captures staged implementation steps for floating button creation, fixed positioning, click-to-open interaction, overlap safety, and screen-size validation. | `Docs/Enhancements.md` |
| `Phase 32.1 chatbot launcher optional UX polish` | Records optional pulse/bounce animation and unread-badge enhancements for the floating chatbot icon pattern. | `Docs/Enhancements.md` |
| `Phase 32.1 AI chat sidebar-link removal mapping` | Removes `ai_chat` route/group mapping from sidebar rendering so chatbot access is no longer menu-driven in dynamic or fallback menu paths. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml` |
| `Phase 32.1 AI chat floating launcher visibility guard` | Computes launcher visibility from connection state and sidebar visibility contract so floating chat access remains role/module-safe. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml` |
| `Phase 32.1 AI chat floating launcher UI component` | Renders a persistent bottom-right floating launcher that opens the AI chat portal action from any app page. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml` |
| `Phase 32.1 AI chat floating launcher responsive styling` | Adds desktop/mobile fixed-position, overlap-safe, animated launcher styling with reduced-motion fallback. | `src/Tabsan.EduSphere.Web/Views/Shared/_Layout.cshtml.css` |
| `Phase 32.2 notification email delivery options` | Defines runtime controls for notification-email dispatch enablement, subject prefix, and portal deep-link URL. | `src/Tabsan.EduSphere.Application/Notifications/NotificationEmailOptions.cs` |
| `Phase 32.2 notification email fan-out dispatch` | Extends notification send flows to dispatch template-based notification emails to active recipients after in-app fan-out. | `src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs` |
| `Phase 32.2 active recipient email resolution` | Resolves active user email addresses for notification recipient sets in repository layer for email fan-out. | `src/Tabsan.EduSphere.Domain/Interfaces/INotificationRepository.cs`, `src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `Phase 32.2 notification alert email template` | Provides HTML notification email template rendering with title/body/type/timestamp and portal action link. | `src/Tabsan.EduSphere.Infrastructure/Email/Templates/notification-alert.html` |
| `Phase 32.2 notification email configuration binding` | Binds notification-email runtime options from API configuration into DI for application notification service consumption. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Phase 32.2 free SMTP relay baseline configuration` | Sets SMTP relay defaults and notification-email toggles for Brevo free-tier-compatible email dispatch posture across environments. | `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Development.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json` |
| `Phase 32.3 SMS delivery provider interface` | Defines `ISmsDeliveryProvider` contract for pluggable SMS delivery implementations in communication integration service. | `src/Tabsan.EduSphere.Application/Interfaces/ICommunicationIntegrationContracts.cs` |
| `Phase 32.3 Twilio SMS provider implementation` | Implements `ISmsDeliveryProvider` using Twilio REST API with E.164 phone validation and 160-character SMS segment truncation. | `src/Tabsan.EduSphere.Infrastructure/Integrations/TwilioSmsDeliveryProvider.cs` |
| `Phase 32.3 notification SMS delivery options` | Defines runtime controls for notification-SMS dispatch enablement and portal deep-link URL. | `src/Tabsan.EduSphere.Application/Notifications/NotificationSmsOptions.cs` |
| `Phase 32.3 notification SMS fan-out dispatch` | Extends notification send flows to dispatch template-based notification SMS to active recipients after in-app fan-out. | `src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs` |
| `Phase 32.3 active recipient phone-number resolution` | Resolves active user phone numbers for notification recipient sets in repository layer for SMS fan-out. | `src/Tabsan.EduSphere.Domain/Interfaces/INotificationRepository.cs`, `src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `Phase 32.3 notification SMS template rendering` | Renders concise plain-text SMS notification with title/body/type and optional type badge using 160-character segment truncation. | `src/Tabsan.EduSphere.Infrastructure/Integrations/TwilioSmsDeliveryProvider.cs` |
| `Phase 32.3 SMS configuration binding and DI registration` | Binds notification-SMS runtime options from API configuration and registers Twilio provider in DI container. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Phase 32.3 free Twilio tier baseline configuration` | Sets SMS notification toggles and Twilio credential environment-variable sourcing for free-tier-compatible SMS dispatch posture across environments. | `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Development.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json` |
| `Phase 32.3 Twilio NuGet package integration` | Adds Twilio 6.15.0 NuGet package to Infrastructure project for SMS REST API client support. | `src/Tabsan.EduSphere.Infrastructure/Tabsan.EduSphere.Infrastructure.csproj` |

## Institution License Validation Plan (2026-05-12)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Institution-License-Validation-Phases.md` | Defines 7 execution phases to validate license-driven institution behavior for School, College, University, and mixed-mode licenses, including required implementation summary, validation summary, and status per phase. | `Docs/Institution-License-Validation-Phases.md` |
| `Phase Completion Template` | Standardizes per-phase evidence capture for implementation, validation, and status checks. | `Docs/Institution-License-Validation-Phases.md` |
| `Mandatory Docs Update After Each Phase` | Enforces synchronized updates across Function-List, Functionality, PRD, Database Schema, Development Plan, and Command docs. | `Docs/Institution-License-Validation-Phases.md` |
| `Mandatory Git Workflow After Each Phase` | Defines commit, pull (rebase), push flow after each phase completion to keep repository state updated. | `Docs/Institution-License-Validation-Phases.md` |
| `Phase 1 Execution Snapshot` | Records endpoint-level evidence for auth, license activation, institution policy binding, and capability-matrix based mode-restriction validation. | `Docs/Institution-License-Validation-Phases.md` |
| `InstitutionPolicyService.SavePolicyAsync` persistence fix | Persists institution policy flags to `portal_settings` by committing settings repository changes, enabling mode switches from uploaded licenses to be retained. | `src/Tabsan.EduSphere.Application/Services/InstitutionPolicyService.cs` |
| `Phase 2 Execution Snapshot` | Records School/College/University lifecycle evidence for policy, labels, capability matrix, progression evaluation, and DB policy persistence checks. | `Docs/Institution-License-Validation-Phases.md` |
| `Phase 2 Replay-Key Validation Note` | Documents current license generator verification-key reuse behavior causing replay rejection without consumed-key reset during sequential validation runs. | `Docs/Institution-License-Validation-Phases.md` |
| `Phase 3 Execution Snapshot` | Records School+College, School+University, College+University, and School+College+University union validation for policy flags, labels, matrix rows, progression outputs, and DB state. | `Docs/Institution-License-Validation-Phases.md` |
| `Phase 3 Label-Resolution Observation` | Captures current mixed-mode label behavior where University-enabled combinations resolve to University vocabulary while pure School+College resolves to School vocabulary. | `Docs/Institution-License-Validation-Phases.md` |
| `Phase 4 Mode-Role Sweep` | Captures School/College/University x SuperAdmin/Admin/Faculty/Student validation for policy, labels, capability matrix, dashboard context, report catalog, and role-scoped report data/export behavior. | `Artifacts/Phase4/ModeRole/20260512-142021/RunSummary.json` |
| `User.InstitutionType` | Adds optional explicit per-user institution assignment field used by provisioning flows. | `src/Tabsan.EduSphere.Domain/Identity/User.cs` |
| `AdminUserController.Create` institution assignment path | Accepts optional `institutionType`, validates against active policy, persists to user, and returns assignment in response/list payloads. | `src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs` |
| `UserImportService` header-based `InstitutionType` parsing | Supports optional `InstitutionType` in CSV import with policy-aware validation and backward-compatible optional columns. | `src/Tabsan.EduSphere.Application/Services/UserImportService.cs` |
| `AddUserInstitutionTypeAssignment` migration | Adds nullable `InstitutionType` column to `users` table for explicit assignment persistence. | `src/Tabsan.EduSphere.Infrastructure/Migrations/20260512043929_AddUserInstitutionTypeAssignment.cs` |
| `Phase 5 API Evidence Set` | Captures post-implementation manual create + CSV import assignment validation artifacts (valid and invalid policy cases). | `Artifacts/Phase5/Api/*_20260512-144212.json` |
| `Phase 6 Access Boundary Evidence Set` | Captures Admin/Faculty/Student role-scope outcomes for assigned vs non-assigned report-export access and allowed read surfaces. | `Artifacts/Phase6/Access/20260512-150824/RunSummary.json` |
| `Phase 7 SuperAdmin Permission Matrix` | Captures SuperAdmin CRUD, activation/deactivation, and cross-institution privileged access checks across School/College/University mode switches. | `Artifacts/Phase7/SuperAdmin/20260512-151302/RunSummary.json` |
| `InstitutionPolicyController.Save` mode-switch validation path | Used by SuperAdmin matrix run to switch School/College/University modes and validate privileged access continuity. | `src/Tabsan.EduSphere.API/Controllers/InstitutionPolicyController.cs` |
| `ReportController` scope guards (`EnforceAdminDepartmentScopeAsync`, `EnforceFacultyOfferingScopeAsync`) | Enforces department/offering scope boundaries producing expected `200` for assigned scope and `403/400` for non-assigned/missing scope. | `src/Tabsan.EduSphere.API/Controllers/ReportController.cs` |
| `DashboardCompositionController.GetContext` | Provides role- and policy-aware module/vocabulary/widget composition used as evidence for menu/dashboard correctness by mode and role. | `src/Tabsan.EduSphere.API/Controllers/DashboardCompositionController.cs` |
| `InstitutionPolicyController.Save` | SuperAdmin mode-switch endpoint used to execute deterministic School/College/University validation runs in Phase 4. | `src/Tabsan.EduSphere.API/Controllers/InstitutionPolicyController.cs` |
| `ReportController` scope guards (`EnforceAdminDepartmentScopeAsync`, `EnforceFacultyOfferingScopeAsync`) | Enforces department/offering scoped report data/export access and generates expected 403/400 guardrail responses when scope inputs are missing or unauthorized. | `src/Tabsan.EduSphere.API/Controllers/ReportController.cs` |

## Final-Touches Phase 34 - High-Load Optimization (2026-05-11)

### Stage 10 - Progressive Load Test Strategy

| Function Name | Purpose | Location |
| --- | --- | --- |
| `k6-phase10-progressive.js` | Parameterized progressive-load scenario for stepwise scale gates, latency/error thresholds, and summary output. | `tests/load/k6-phase10-progressive.js` |
| `run-phase10-progressive.ps1::Get-Phase10GatePlan` | Defines the progressive and extended gate sequences used for Phase 10 stepwise validation. | `tests/load/run-phase10-progressive.ps1` |
| `run-phase10-progressive.ps1::Invoke-Phase10Gate` | Executes a single Phase 10 gate run, captures summaries, and emits machine-readable results for analysis. | `tests/load/run-phase10-progressive.ps1` |
| `run-phase10-progressive.ps1::Get-BottleneckClass` | Classifies the first likely bottleneck from gate metrics (api, database/dependency, infra, rate-limit, or contract/authz). | `tests/load/run-phase10-progressive.ps1` |
| `run-phase10-progressive.ps1` main loop | Runs the configured gate plan in sequence and supports repeated retest attempts for fix-and-retest validation. | `tests/load/run-phase10-progressive.ps1` |

### Stage 9 - Monitoring and Observability

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ObservabilityMetrics` | Stores rolling request durations, error counts, and process/runtime snapshots for latency SLO and observability endpoints. | `src/Tabsan.EduSphere.API/Services/ObservabilityMetrics.cs` |
| `DatabaseConnectivityHealthCheck` | Verifies the API can connect to the database for continuous runtime monitoring. | `src/Tabsan.EduSphere.API/Services/ObservabilityMetrics.cs` |
| `MemoryPressureHealthCheck` | Monitors memory pressure against configurable thresholds for continuous host health monitoring. | `src/Tabsan.EduSphere.API/Services/ObservabilityMetrics.cs` |
| `CpuPressureHealthCheck` | Monitors average process CPU usage against configurable thresholds for continuous host health monitoring. | `src/Tabsan.EduSphere.API/Services/ObservabilityMetrics.cs` |
| `NetworkStackHealthCheck` | Verifies network/DNS resolution for the configured probe endpoint to keep network monitoring active. | `src/Tabsan.EduSphere.API/Services/ObservabilityMetrics.cs` |
| `ErrorRateHealthCheck` | Compares rolling request error rate against configured SLO thresholds. | `src/Tabsan.EduSphere.API/Services/ObservabilityMetrics.cs` |
| `AddOpenTelemetry` metrics block | Publishes ASP.NET Core, HttpClient, runtime, and process metrics and exposes Prometheus scraping support. | `src/Tabsan.EduSphere.API/Program.cs` |
| `ObservabilityMetrics` middleware block | Captures per-request timings and status codes for rolling p50/p95/p99 latency snapshots. | `src/Tabsan.EduSphere.API/Program.cs` |
| `GET /metrics` | Prometheus scrape endpoint for Grafana/Prometheus collection. | `src/Tabsan.EduSphere.API/Program.cs` |
| `GET /health/observability` | Returns rolling latency and runtime observability snapshot data. | `src/Tabsan.EduSphere.API/Program.cs` |
| `AddHealthChecks` observability registrations | Registers database, memory, CPU, network, and error-rate checks for continuous runtime health monitoring. | `src/Tabsan.EduSphere.API/Program.cs` |

### Stage 8 - Infrastructure Tuning

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Program` auto-scaling policy bootstrap (`InfrastructureTuning:AutoScaling`) | Adds startup-validated auto-scaling policy metadata (`Enabled`, replica bounds, target utilization/cooldown controls) for API, Web, and BackgroundJobs runtime profiles. | `src/Tabsan.EduSphere.API/Program.cs`, `src/Tabsan.EduSphere.Web/Program.cs`, `src/Tabsan.EduSphere.BackgroundJobs/Program.cs` |
| `Program` host-limit tuning bootstrap (`InfrastructureTuning:HostLimits`) | Applies config-driven thread-pool minimums and Kestrel concurrent-connection limits for high-concurrency host behavior. | `src/Tabsan.EduSphere.API/Program.cs`, `src/Tabsan.EduSphere.Web/Program.cs`, `src/Tabsan.EduSphere.BackgroundJobs/Program.cs` |
| `Program` network-stack tuning bootstrap (`InfrastructureTuning:NetworkStack`) | Applies Kestrel keep-alive/header/HTTP2 stream tuning and outbound `SocketsHttpHandler` connection pooling limits for high-volume traffic patterns. | `src/Tabsan.EduSphere.API/Program.cs`, `src/Tabsan.EduSphere.Web/Program.cs`, `src/Tabsan.EduSphere.BackgroundJobs/Program.cs` |
| `GET /health/scaling` | Exposes active auto-scaling and infrastructure tuning baseline from API for deployment/runtime validation. | `src/Tabsan.EduSphere.API/Program.cs` |
| `InfrastructureTuning` configuration sections | Adds environment-scoped startup tuning keys for API, Web, and BackgroundJobs host/runtime/network scaling controls. | `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Development.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json`, `src/Tabsan.EduSphere.Web/appsettings.json`, `src/Tabsan.EduSphere.Web/appsettings.Development.json`, `src/Tabsan.EduSphere.Web/appsettings.Production.json`, `src/Tabsan.EduSphere.BackgroundJobs/appsettings.json`, `src/Tabsan.EduSphere.BackgroundJobs/appsettings.Development.json`, `src/Tabsan.EduSphere.BackgroundJobs/appsettings.Production.json` |

### Stage 7 - Background Processing

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IAccountSecurityEmailQueue` + `AccountSecurityEmailWorkItem` | Defines the queue contract and payload for offloading account-security transactional emails from request paths. | `src/Tabsan.EduSphere.Application/Interfaces/IAccountSecurityEmailQueue.cs` |
| `AccountSecurityService` queued email path (`UnlockAccountAsync`, `ResetPasswordAsync`) | Enqueues unlock/reset transactional emails when queue is available, keeping direct-send fallback for safety. | `src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs` |
| `InMemoryAccountSecurityEmailQueue` | Provides channel-based in-process queue implementation for account-security email work items. | `src/Tabsan.EduSphere.API/Services/InMemoryAccountSecurityEmailQueue.cs` |
| `InMemoryAccountSecurityEmailWorker` | Processes queued account-security email work items in background using existing email sender integration. | `src/Tabsan.EduSphere.API/Services/InMemoryAccountSecurityEmailWorker.cs` |
| `QueuePlatformOptions` | Adds deployment-model queue-platform configuration (`Provider`, `RabbitMq`) for background processing selection. | `src/Tabsan.EduSphere.API/Services/QueuePlatformOptions.cs` |
| `RabbitMqAccountSecurityEmailQueue` | Publishes account-security email work items to RabbitMQ when queue platform is configured for brokered mode. | `src/Tabsan.EduSphere.API/Services/RabbitMqAccountSecurityEmailQueue.cs` |
| `RabbitMqAccountSecurityEmailWorker` | Consumes account-security email work items from RabbitMQ and executes background email sends. | `src/Tabsan.EduSphere.API/Services/RabbitMqAccountSecurityEmailWorker.cs` |
| `Program` queue platform registration block (`QueuePlatform:Provider`) | Selects in-memory vs RabbitMQ queue platform at startup and registers matching queue + worker pipeline. | `src/Tabsan.EduSphere.API/Program.cs` |

### Stage 6 - Dependency Optimization

| Function Name | Purpose | Location |
| --- | --- | --- |
| `LibraryService.GetLoansAsync(...)` | Adds short-TTL distributed cache for safe external loan lookup reads to reduce repeated dependency calls. | `src/Tabsan.EduSphere.Application/Services/LibraryService.cs` |
| `LibraryService.BuildLoanLookupCacheKey(...)` | Keys external-call cache entries by student identifier and integration configuration fingerprint to avoid stale cross-config reuse. | `src/Tabsan.EduSphere.Application/Services/LibraryService.cs` |
| `ResilientOutboundIntegrationGateway` circuit state helpers (`EnsureCircuitClosedAsync`, `RegisterCircuitFailureAsync`, `ResetCircuitAsync`) | Adds channel-level circuit-breaker behavior over existing timeout/retry resilience policy. | `src/Tabsan.EduSphere.Infrastructure/Integrations/ResilientOutboundIntegrationGateway.cs` |
| `IntegrationChannelOptions.CircuitBreakerFailureThreshold` | Configures how many consecutive failures open a channel circuit breaker. | `src/Tabsan.EduSphere.Infrastructure/Integrations/IntegrationGatewayOptions.cs`, `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Development.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json` |
| `IntegrationChannelOptions.CircuitBreakerOpenSeconds` | Configures how long the integration circuit stays open before allowing retry attempts. | `src/Tabsan.EduSphere.Infrastructure/Integrations/IntegrationGatewayOptions.cs`, `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Development.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json` |
| `GradebookService.GetGradebookAsync(...)` | Removes sync-over-async `.Result` consumption from the gradebook request path by awaiting completed tasks. | `src/Tabsan.EduSphere.Application/Assignments/GradebookService.cs` |

### Stage 5 - k6 Load Testing Improvements

| Function Name | Purpose | Location |
| --- | --- | --- |
| `k6-scale-50k.js` scenario (`ramping-arrival-rate`) | Uses request-rate ramping with randomized think-time to model realistic 50k profile traffic. | `tests/load/k6-scale-50k.js` |
| `k6-scale-100k.js` scenario (`ramping-arrival-rate`) | Uses request-rate ramping with randomized think-time to model realistic 100k profile traffic. | `tests/load/k6-scale-100k.js` |
| `k6-scale-1m.js` scenario (`ramping-arrival-rate`) | Uses request-rate ramping with randomized think-time to model realistic 1m profile traffic. | `tests/load/k6-scale-1m.js` |
| `k6-scale-5m.js` scenario (`ramping-arrival-rate`) | Uses request-rate ramping with randomized think-time to model realistic 5m profile traffic. | `tests/load/k6-scale-5m.js` |
| `GENERATOR_TOTAL` / `GENERATOR_INDEX` shard controls | Splits target request-rate and VU ceilings across multiple generator machines for distributed runs. | `tests/load/k6-scale-50k.js`, `tests/load/k6-scale-100k.js`, `tests/load/k6-scale-1m.js`, `tests/load/k6-scale-5m.js` |
| `run-50k.bat` / `run-100k.bat` / `run-1m.bat` / `run-5m.bat` | Passes shard controls and target RPS while running in quiet summary-first mode. | `tests/load/run-50k.bat`, `tests/load/run-100k.bat`, `tests/load/run-1m.bat`, `tests/load/run-5m.bat` |
| `run-load-test.ps1` distributed/raw-output controls | Adds `-Distributed`, `-GeneratorTotal`, `-GeneratorIndex`, `-AllowRawOutput`, and quiet-by-default behavior for output discipline. | `tests/load/run-load-test.ps1` |

### Stage 4 - Caching Strategy

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AnalyticsService.GetPerformanceReportAsync(...)` | Adds short-TTL distributed cache policy for expensive department/all performance analytics reads. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs` |
| `AnalyticsService.GetAttendanceReportAsync(...)` | Adds short-TTL distributed cache policy for expensive attendance analytics reads. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs` |
| `AnalyticsService.GetAssignmentStatsAsync(...)` | Adds short-TTL distributed cache policy for expensive assignment analytics reads. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs` |
| `AnalyticsService.GetQuizStatsAsync(...)` | Adds short-TTL distributed cache policy for expensive quiz analytics reads. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs` |
| `AnalyticsService.BuildAnalyticsCacheKey(...)` | Enforces cache scope boundaries by keying shared analytics cache entries to report type and department scope. | `src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs` |
| `Program` static asset cache header policy (`UseStaticFiles` with `OnPrepareResponse`) | Adds edge/CDN-friendly cache headers for static web assets only, preserving non-cached dynamic/authenticated responses. | `src/Tabsan.EduSphere.Web/Program.cs` |
| `StaticAssetCaching` configuration keys (`Enabled`, `MaxAgeSeconds`) | Provides environment-controlled static asset caching policy for web host edge behavior. | `src/Tabsan.EduSphere.Web/appsettings.json`, `src/Tabsan.EduSphere.Web/appsettings.Development.json`, `src/Tabsan.EduSphere.Web/appsettings.Production.json` |

### Stage 3.3 - Transport Optimization

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Program` Kestrel transport tuning (API) | Sets keep-alive timeout, request-header timeout, server-header suppression, and HTTP/2 ping tuning for the API host. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Program` Kestrel transport tuning (Web) | Sets keep-alive timeout, request-header timeout, server-header suppression, and HTTP/2 ping tuning for the Web host. | `src/Tabsan.EduSphere.Web/Program.cs` |
| `AddResponseCompression` configuration (API) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | `src/Tabsan.EduSphere.API/Program.cs` |
| `AddResponseCompression` configuration (Web) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | `src/Tabsan.EduSphere.Web/Program.cs` |

### Stage 3.2 - Async and Non-Blocking IO

| Function Name | Purpose | Location |
| --- | --- | --- |
| `TimetableRepository.GetByDepartmentAsync(...)` | Returns timetable lists with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs` |
| `TimetableRepository.GetPublishedByDepartmentAsync(...)` | Returns published timetable lists with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs` |
| `TimetableRepository.GetTeacherEntriesAsync(...)` | Returns teacher timetable entries with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs` |
| `TimetableRepository.GetEntriesByCourseOfferingAsync(...)` | Returns course-offering timetable entries with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs` |
| `SettingsRepository.GetAllReportsAsync(...)` | Returns report definitions with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs` |
| `SettingsRepository.GetModuleRolesAsync(...)` | Returns module-role assignments with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs` |
| `SettingsRepository.GetAllModuleRolesAsync(...)` | Returns all module-role assignments with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs` |
| `QuizRepository.GetByOfferingAsync(...)` | Returns quiz lists with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs` |
| `QuizRepository.GetAttemptsAsync(...)` | Returns quiz attempts with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs` |
| `QuizRepository.GetAllAttemptsForStudentAsync(...)` | Returns student quiz attempts with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs` |
| `BuildingRoomRepository.GetAllBuildingsAsync(...)` | Returns building lists with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `BuildingRoomRepository.GetAllRoomsAsync(...)` | Returns room lists with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `BuildingRoomRepository.GetRoomsByBuildingAsync(...)` | Returns building-scoped room lists with direct async EF execution instead of `ContinueWith` bridging. | `src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs` |

### Stage 3.1 - Endpoint Aggregation

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DashboardCompositionController.GetContext(...)` | Aggregates visible modules, academic vocabulary, and widgets into one dashboard-context response for the portal ModuleComposition screen. | `src/Tabsan.EduSphere.API/Controllers/DashboardCompositionController.cs` |
| `IEduApiClient.GetDashboardCompositionContextAsync(...)` | Fetches the aggregated dashboard-context payload in a single API request for the web portal. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `EduApiClient.GetDashboardCompositionContextAsync(...)` | Deserializes the aggregated dashboard-context endpoint response into a single Web client model. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |
| `DashboardCompositionContextApiModel` | Holds the aggregated module/vocabulary/widget payload returned by the dashboard context endpoint. | `src/Tabsan.EduSphere.Web/Services/EduApiClient.cs` |

### Stage 2.3 - Stateless Runtime Hardening

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Program` distributed cache startup guard (`ScaleOut:RedisConnectionString`) | Requires Redis-backed distributed cache outside Development/Testing so API cache state stays shared across instances. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Program` shared data-protection startup guard (`ScaleOut:SharedDataProtectionKeyRingPath`) | Requires a shared key ring outside Development/Testing so web auth cookies remain valid across instances. | `src/Tabsan.EduSphere.Web/Program.cs` |
| `Program` local cache fallback allowance (Development/Testing only) | Preserves node-local cache/dev convenience only in non-production environments. | `src/Tabsan.EduSphere.API/Program.cs`, `src/Tabsan.EduSphere.Web/Program.cs` |

### Stage 2.2 - Load Balancer Policy Baseline (Least Connections)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Phase2-Stage2.2-nginx-leastconn.conf.template` | Defines least-connections upstream routing baseline for API node fan-out with forwarded-header propagation. | `Scripts/Phase2-Stage2.2-nginx-leastconn.conf.template` |
| `Phase2-Stage2.2-LoadBalancer.ps1` | Starts/stops local Nginx load balancer container with generated least-connections upstream node list. | `Scripts/Phase2-Stage2.2-LoadBalancer.ps1` |
| `Phase2-Stage2.2-Validate-LB.ps1` | Samples load-balanced requests and reports per-node traffic distribution using instance telemetry header/body. | `Scripts/Phase2-Stage2.2-Validate-LB.ps1` |

### Stage 2.1 - Multi-Instance API Deployment Baseline

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Program` scale-out instance identity bootstrap (`ScaleOut:InstanceId`) | Assigns deterministic runtime node identity (configured value or machine+process fallback) for horizontally scaled API observability. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Program` instance telemetry response header (`X-EduSphere-Instance`) | Emits node identity per response to verify load balancer traffic distribution across API nodes. | `src/Tabsan.EduSphere.API/Program.cs` |
| `GET /health/instance` | Exposes per-node health payload (instance id, process id, machine, uptime, version) for load balancer and scale validation probes. | `src/Tabsan.EduSphere.API/Program.cs` |
| `ScaleOut:InstanceId` configuration key | Provides deploy-time instance identity override for node-aware horizontal scaling rollout. | `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json` |
| `ScaleOut:ExposeInstanceHeader` configuration key | Controls whether instance identity response header is emitted from API nodes. | `src/Tabsan.EduSphere.API/appsettings.json`, `src/Tabsan.EduSphere.API/appsettings.Production.json` |
| `Phase2-Stage2.1-MultiInstance-Api.ps1` | Starts/stops multiple local API instances on sequential ports for Stage 2.1 horizontal-scale baseline checks. | `Scripts/Phase2-Stage2.1-MultiInstance-Api.ps1` |

### Stage 1.4 - Data Access Caching (Hot Read Paths)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DashboardCompositionService.GetWidgets(...)` | Adds short-TTL in-memory cache for dashboard widget composition keyed by role and institution policy state to reduce repeated composition cost. | `src/Tabsan.EduSphere.Application/Services/DashboardCompositionService.cs` |
| `SidebarMenuService.GetTopLevelMenusAsync(...)` | Adds short-TTL in-memory cache for top-level sidebar menu reads with versioned invalidation support. | `src/Tabsan.EduSphere.Application/Services/SettingsServices.cs` |
| `SidebarMenuService.GetVisibleForRoleAsync(...)` | Adds short-TTL in-memory cache for role-scoped visible sidebar reads with versioned invalidation support. | `src/Tabsan.EduSphere.Application/Services/SettingsServices.cs` |
| `SidebarMenuService.InvalidateSidebarCache()` | Bumps sidebar cache version after role/status mutations to force fresh reads on next request. | `src/Tabsan.EduSphere.Application/Services/SettingsServices.cs` |
| `NotificationService.GetInboxAsync(...)` | Adds short-TTL in-memory cache for paged inbox reads with cache-version keying for mutation-safe freshness. | `src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs` |
| `NotificationService.GetBadgeAsync(...)` | Adds short-TTL in-memory cache for unread badge counts with cache-version keying. | `src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs` |
| `NotificationService.BumpCacheVersion()` | Invalidates notification read-cache windows by incrementing cache version after notification mutations. | `src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs` |

### Stage 1.2 - Connection Pooling and Timeout Tuning

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` (pool tuning - default profile) | Adds explicit SQL client connection pooling controls (`Min Pool Size`, `Max Pool Size`, `Connect Timeout`) for baseline high-load stability. | `src/Tabsan.EduSphere.API/appsettings.json` |
| `ConnectionStrings:DefaultConnection` (pool tuning - development profile) | Applies explicit development pool sizing and connection timeout to reduce local exhaustion during higher VU runs. | `src/Tabsan.EduSphere.API/appsettings.Development.json` |
| `ConnectionStrings:DefaultConnection` (pool tuning - production profile guidance) | Defines production-target pool sizing guidance in profile placeholder connection string for deployment hardening. | `src/Tabsan.EduSphere.API/appsettings.Production.json` |

### Stage 1.3 - Hot-Path Query Optimization

| Function Name | Purpose | Location |
| --- | --- | --- |
| `INotificationRepository.GetForUserAsync(..., asNoTracking, ...)` | Adds opt-in no-tracking control for read-heavy inbox retrieval while preserving tracked reads for mark-all-read operations. | `src/Tabsan.EduSphere.Domain/Interfaces/INotificationRepository.cs` |
| `NotificationRepository.GetForUserAsync(..., asNoTracking, ...)` | Applies optional AsNoTracking to notification recipient + parent notification read path. | `src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `NotificationRepository.GetUnreadCountAsync(...)` | Removes unnecessary Include from unread count query to avoid extra query shaping overhead. | `src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `SettingsRepository.GetTopLevelMenusAsync(...)` | Uses AsNoTracking + AsSplitQuery for include-heavy top-level sidebar graph retrieval. | `src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs` |
| `SettingsRepository.GetSubMenusAsync(...)` | Uses AsNoTracking for read-only submenu retrieval. | `src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs` |
| `SettingsRepository.GetVisibleMenusForRoleAsync(...)` | Uses AsNoTracking + AsSplitQuery for include-heavy role-scoped sidebar visibility reads. | `src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs` |

## Final-Touches Phase 33 - Hosting Configuration and Security Hardening (2026-05-10)

### Hosting Configuration Foundation (Stage 33.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Program` startup configuration bootstrap (`SetBasePath` + `AddJsonFile` + `AddEnvironmentVariables`) | Forces explicit environment-aware configuration load order and startup diagnostics for API host. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Program` startup config guard (`DefaultConnection`) | Fails API startup early when required DB connection setting is missing. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Program` startup configuration bootstrap (`SetBasePath` + `AddJsonFile` + `AddEnvironmentVariables`) | Forces explicit environment-aware configuration load order for background workers. | `src/Tabsan.EduSphere.BackgroundJobs/Program.cs` |
| `Program` startup config guard (`DefaultConnection` placeholder rejection) | Prevents BackgroundJobs startup with non-overridden placeholder connection string in base config. | `src/Tabsan.EduSphere.BackgroundJobs/Program.cs` |
| `Program` startup configuration bootstrap (`SetBasePath` + `AddJsonFile` + `AddEnvironmentVariables`) | Forces explicit environment-aware configuration load order for Web host. | `src/Tabsan.EduSphere.Web/Program.cs` |
| `Program` startup config guard (`EduApi:BaseUrl`) | Fails Web startup early when API base URL configuration is missing. | `src/Tabsan.EduSphere.Web/Program.cs` |

### Runtime Hosting Hardening (Stage 33.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Program` reverse-proxy bootstrap (`ReverseProxy:Enabled`, `KnownProxies`, `ForwardLimit`, `RequireHeaderSymmetry`) | Restricts forwarded-header trust to configured proxy IPs and avoids implicit trust-all proxy behavior in API host. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Program` production CORS startup guard | Prevents API startup with empty `AppSettings:CorsOrigins` outside Development/Testing. | `src/Tabsan.EduSphere.API/Program.cs` |
| `Program` reverse-proxy bootstrap (`ReverseProxy:Enabled`, `KnownProxies`, `ForwardLimit`, `RequireHeaderSymmetry`) | Restricts forwarded-header trust to configured proxy IPs in Web host. | `src/Tabsan.EduSphere.Web/Program.cs` |
| `LoginController` API base URL resolution (`_configuredApiBaseUrl`) | Removes localhost fallback assumptions and requires configured `EduApi:BaseUrl` for login/security profile calls. | `src/Tabsan.EduSphere.Web/Controllers/LoginController.cs` |
| `ApiConnectionModel.ApiBaseUrl` default | Removes hardcoded localhost default from persisted portal API connection model. | `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs` |

### Security Hardening Execution (Stage 33.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `LoginRequest` validation attributes | Enforces username/password length and allowed-character constraints for auth login requests. | `src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs` |
| `RefreshRequest` validation attribute | Ensures refresh token input is required for auth refresh/logout flows. | `src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs` |
| `ChangePasswordRequest` validation attributes | Enforces required current/new password input with minimum strength length. | `src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs` |
| `ForceChangePasswordRequest` validation attribute | Enforces required password input for forced password change flow. | `src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs` |
| `CreateAdminUserRequest` validation attributes | Enforces username/email/password validation for admin user creation. | `src/Tabsan.EduSphere.Application/DTOs/Auth/AdminUserManagementDtos.cs` |
| `UpdateAdminUserRequest` validation attributes | Enforces safe email/password validation for admin user updates. | `src/Tabsan.EduSphere.Application/DTOs/Auth/AdminUserManagementDtos.cs` |
| `SecurityValidationTests.LoginRequest_InvalidUsernameAndShortPassword_FailsValidation()` | Verifies hardened login validation rejects invalid usernames and weak passwords. | `tests/Tabsan.EduSphere.UnitTests/SecurityValidationTests.cs` |
| `SecurityValidationTests.LoginRequest_ValidValues_PassesValidation()` | Verifies valid login inputs remain accepted by validation. | `tests/Tabsan.EduSphere.UnitTests/SecurityValidationTests.cs` |
| `SecurityValidationTests.CreateAdminUserRequest_InvalidEmail_FailsValidation()` | Verifies invalid admin email values are rejected. | `tests/Tabsan.EduSphere.UnitTests/SecurityValidationTests.cs` |
| `SecurityValidationTests.ForceChangePasswordRequest_EmptyPassword_FailsValidation()` | Verifies forced password change requires a non-empty password. | `tests/Tabsan.EduSphere.UnitTests/SecurityValidationTests.cs` |

## Final-Touches Phase 32 - Cross-Phase Operational Guardrails (2026-05-10)

### Report Center and Link Regression Guardrails (Stage 32.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ReportCatalogIntegrationTests.ReportCatalog_Unauthenticated_Returns401()` | Verifies report catalog endpoint remains protected from anonymous access. | `tests/Tabsan.EduSphere.IntegrationTests/ReportCatalogIntegrationTests.cs` |
| `ReportCatalogIntegrationTests.ReportCatalog_PrivilegedRoles_ReturnsExpectedSeededKeys(role)` | Verifies seeded report keys remain visible to SuperAdmin/Admin/Faculty in catalog responses. | `tests/Tabsan.EduSphere.IntegrationTests/ReportCatalogIntegrationTests.cs` |
| `ReportCatalogIntegrationTests.ReportCatalog_Student_ReturnsOnlyStudentAllowedReports()` | Verifies student report catalog remains scoped to student-allowed report definitions. | `tests/Tabsan.EduSphere.IntegrationTests/ReportCatalogIntegrationTests.cs` |
| `ReportCatalogIntegrationTests.ReportCatalog_PrivilegedRoles_AllCatalogKeysMapToLiveReportDataRoutes(role)` | Verifies every catalog key maps to a live report data route (no broken links/404 routes). | `tests/Tabsan.EduSphere.IntegrationTests/ReportCatalogIntegrationTests.cs` |

### Report Export Action Guardrails (Stage 32.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ReportExportsIntegrationTests.AttendanceSummary_Export_Unauthenticated_Returns401()` | Verifies report export endpoints remain protected from anonymous access. | `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs` |
| `ReportExportsIntegrationTests.ReportExports_WithSuperAdmin_ReturnExpectedFileMetadata(route, expectedContentType, expectedFileName)` | Verifies attendance/result/assignment/quiz export routes return expected media type, attachment filename contract, and non-empty payload bytes. | `tests/Tabsan.EduSphere.IntegrationTests/ReportExportsIntegrationTests.cs` |

### Sidebar Settings Assignability Guardrails (Stage 32.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SidebarMenuIntegrationTests.SetRoles_AllSeededMenus_AreAssignable()` | Verifies every seeded sidebar menu key accepts role-assignment updates through Sidebar Settings (no broken menu assignment actions). | `tests/Tabsan.EduSphere.IntegrationTests/SidebarMenuIntegrationTests.cs` |

### Report Center Sidebar Visibility and Link Guardrails (Stage 32.4)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DatabaseSeeder.SeedSidebarMenusAsync(...)` (`EnsureRoleAccess` helper) | Makes sidebar role seeding self-healing by updating existing role-access values, ensuring corrected visibility rules are enforced on already-seeded databases. | `src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs` |
| `SidebarMenuIntegrationTests.GetVisible_Student_ReturnsStudentMenusOnly()` | Verifies student sidebar includes `report_center` and updated menu-count contract after visibility correction. | `tests/Tabsan.EduSphere.IntegrationTests/SidebarMenuIntegrationTests.cs` |
| `SidebarMenuIntegrationTests.ReportCenter_VisibleRoles_HaveMenuAndReachableCatalog(role)` | Verifies Admin/Faculty/Student can see `report_center` and can successfully load report catalog data via API (working link behavior by role). | `tests/Tabsan.EduSphere.IntegrationTests/SidebarMenuIntegrationTests.cs` |

### Credential and Command Verification Guardrails (Stage 32.5)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CredentialVerificationIntegrationTests.EnsureUserAsync(username, password, role)` | Seeds deterministic role-based auth smoke users in integration DB so credential checks remain executable after backend changes. | `tests/Tabsan.EduSphere.IntegrationTests/CredentialVerificationIntegrationTests.cs` |
| `CredentialVerificationIntegrationTests.Login_WithVerifiedCredentials_ReturnsTokenAndExpectedRole(username, password, expectedRole)` | Verifies `api/v1/auth/login` succeeds for SuperAdmin/Admin/Faculty/Student smoke credentials and returns token + expected role. | `tests/Tabsan.EduSphere.IntegrationTests/CredentialVerificationIntegrationTests.cs` |

## Phase 33 - SaaS and Multi-Tenant Readiness (2026-05-14)

### Tenant Scope Isolation

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ITenantScopeResolver.GetTenantScopeKey()` | Provides a tenant-scope abstraction for application-layer tenant-aware key resolution. | `src/Tabsan.EduSphere.Application/Interfaces/ITenantScopeResolver.cs` |
| `HttpTenantScopeResolver.GetTenantScopeKey()` | Resolves tenant scope from JWT claims or `X-Tenant-Code` request header for API-request-scoped operations. | `src/Tabsan.EduSphere.API/Services/HttpTenantScopeResolver.cs` |
| `TenantOperationsService.ScopedSettingKey(key)` | Builds tenant-scoped settings keys for onboarding/subscription/profile operations with default-tenant backward compatibility. | `src/Tabsan.EduSphere.Application/Services/SettingsServices.cs` |
| `TenantOperationsService.ScopedCacheKey(key)` | Builds tenant-scoped distributed-cache keys to prevent cross-tenant cache collisions. | `src/Tabsan.EduSphere.Application/Services/SettingsServices.cs` |
| `TenantOperationsService.ReadSetting(all, rawKey, defaultValue)` | Reads tenant-scoped values first and falls back to legacy unscoped keys for migration-safe compatibility. | `src/Tabsan.EduSphere.Application/Services/SettingsServices.cs` |
| `Phase31Stage1RegressionMatrixTests.TenantIsolation_SameStore_DifferentTenantScopes_DoNotLeakData()` | Verifies no cross-tenant leakage when two tenant scopes share one settings repository instance. | `tests/Tabsan.EduSphere.UnitTests/Phase31Stage1RegressionMatrixTests.cs` |

## Final-Touches Phase 31 - Quality, Security, and Go-Live Gates (2026-05-10)

### Load and Reliability Certification (Stage 31.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `k6-certification-bands.js (certification_band scenario)` | Runs Stage 31.3 load certification per selected target band (`up-to-10k`, `10k-100k`, `100k-500k`, `500k-1m`) with latency/error thresholds. | `tests/load/k6-certification-bands.js` |
| `recovery-smoke.ps1::Test-Health(url)` | Probes API health endpoint and returns pass/fail used by recovery automation flow. | `tests/load/recovery-smoke.ps1` |
| `recovery-smoke.ps1` main flow | Simulates node/service failure by stopping/restarting API process and verifying recovery window SLA via `/health`. | `tests/load/recovery-smoke.ps1` |

### API and Tests - Security Hardening (Stage 31.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `FeatureFlagsController.Save(key, command, ct)` | Persists feature-flag changes and emits control-plane audit log entry (`FeatureFlagSave`). | `API/Controllers/FeatureFlagsController.cs` |
| `FeatureFlagsController.Rollback(command, ct)` | Executes feature-flag rollback and emits control-plane audit log entry (`FeatureFlagRollback`). | `API/Controllers/FeatureFlagsController.cs` |
| `TenantOperationsController.SaveOnboardingTemplate(command, ct)` | Persists onboarding-template settings and emits audit entries for successful or blocked writes. | `API/Controllers/TenantOperationsController.cs` |
| `TenantOperationsController.SaveSubscriptionPlan(command, ct)` | Persists subscription-plan settings and emits audit entries for successful or blocked writes. | `API/Controllers/TenantOperationsController.cs` |
| `TenantOperationsController.SaveTenantProfile(command, ct)` | Persists tenant-profile settings and emits audit entries for successful or blocked writes. | `API/Controllers/TenantOperationsController.cs` |
| `InstitutionPolicyController.Save(request, ct)` | Persists institution policy flags and emits audit log entry (`InstitutionPolicySave`). | `API/Controllers/InstitutionPolicyController.cs` |
| `Phase31Stage2SecurityHardeningTests.All_Api_Endpoints_Are_Explicitly_Authorized_Or_Anonymous()` | Prevents unguarded API endpoints by enforcing explicit auth or explicit anonymous declaration. | `tests/Tabsan.EduSphere.IntegrationTests/Phase31Stage2SecurityHardeningTests.cs` |
| `Phase31Stage2SecurityHardeningTests.AllowAnonymous_Surface_Is_Whitelisted()` | Enforces strict whitelist of anonymous endpoints to control data exposure surface. | `tests/Tabsan.EduSphere.IntegrationTests/Phase31Stage2SecurityHardeningTests.cs` |
| `Phase31Stage2SecurityHardeningTests.Sensitive_ControlPlane_Mutations_Write_Audit_Logs()` | Verifies sensitive control-plane writes emit expected audit events. | `tests/Tabsan.EduSphere.IntegrationTests/Phase31Stage2SecurityHardeningTests.cs` |
| `Phase31Stage2SecurityHardeningTests.Blocked_Tenant_Write_Is_Audited()` | Verifies rollback-safety blocked write paths are also audit logged. | `tests/Tabsan.EduSphere.IntegrationTests/Phase31Stage2SecurityHardeningTests.cs` |

### Unit Tests - Regression Matrix (Stage 31.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Phase31Stage1RegressionMatrixTests.Matrix_RoleModeLicense_Combinations_AreConsistent(role, institutionMode, aiChatLicensed)` | Executes 24-scenario regression matrix coverage for role x institution mode x license combination invariants. | `tests/Tabsan.EduSphere.UnitTests/Phase31Stage1RegressionMatrixTests.cs` |
| `Phase31Stage1RegressionMatrixTests.TenantIsolation_Baseline_SeparateStores_DoNotLeakData()` | Verifies baseline tenant isolation behavior by ensuring tenant profile settings do not leak across isolated stores. | `tests/Tabsan.EduSphere.UnitTests/Phase31Stage1RegressionMatrixTests.cs` |
| `MatrixEntitlementResolver.IsActiveAsync(moduleKey, ct)` | Provides deterministic license profile behavior in matrix tests (licensed vs restricted `ai_chat`). | `tests/Tabsan.EduSphere.UnitTests/Phase31Stage1RegressionMatrixTests.cs` |
| `MatrixModuleService.GetAllAsync(ct)` | Supplies registry-backed module inventory for matrix visibility assertions in tests. | `tests/Tabsan.EduSphere.UnitTests/Phase31Stage1RegressionMatrixTests.cs` |
| `MatrixSettingsRepository.GetPortalSettingAsync(key, ct)` | Supports test-time tenant settings reads used by tenant isolation baseline assertions. | `tests/Tabsan.EduSphere.UnitTests/Phase31Stage1RegressionMatrixTests.cs` |
| `MatrixSettingsRepository.UpsertPortalSettingAsync(key, value, ct)` | Supports test-time tenant settings writes used by tenant isolation baseline assertions. | `tests/Tabsan.EduSphere.UnitTests/Phase31Stage1RegressionMatrixTests.cs` |

## Final-Touches Phase 30 - Integrations and SaaS Operations (2026-05-10)

### Application - Reliability and Rollback Controls (Stage 30.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IFeatureFlagService.GetAllAsync(ct)` | Returns all rollout control flags including defaults and persisted overrides. | `Application/Interfaces/ISettingsServices.cs` |
| `IFeatureFlagService.GetAsync(key, ct)` | Resolves a specific feature flag with default fallback behavior. | `Application/Interfaces/ISettingsServices.cs` |
| `IFeatureFlagService.SaveAsync(command, ct)` | Persists a feature flag state update for controlled rollout. | `Application/Interfaces/ISettingsServices.cs` |
| `IFeatureFlagService.RollbackAsync(command, ct)` | Performs bulk rollback by disabling specified feature flags and recording rollback metadata. | `Application/Interfaces/ISettingsServices.cs` |
| `FeatureFlagService.GetAllAsync(ct)` | Enumerates feature flags from settings storage and merges runtime defaults. | `Application/Services/SettingsServices.cs` |
| `FeatureFlagService.GetAsync(key, ct)` | Returns effective state for a single feature flag key. | `Application/Services/SettingsServices.cs` |
| `FeatureFlagService.SaveAsync(command, ct)` | Writes feature flag state/description/update timestamp to settings storage. | `Application/Services/SettingsServices.cs` |
| `FeatureFlagService.RollbackAsync(command, ct)` | Disables a selected feature-flag set for emergency rollback and stores rollback context fields. | `Application/Services/SettingsServices.cs` |
| `FeatureFlagsController.GetAll(ct)` | SuperAdmin endpoint to list all feature flags. | `API/Controllers/FeatureFlagsController.cs` |
| `FeatureFlagsController.GetByKey(key, ct)` | SuperAdmin endpoint to inspect a single feature flag. | `API/Controllers/FeatureFlagsController.cs` |
| `TenantOperationsController.SaveOnboardingTemplate(command, ct)` (flag-guarded) | Enforces `tenant-operations.write` kill-switch before mutating onboarding template settings. | `API/Controllers/TenantOperationsController.cs` |
| `TenantOperationsController.SaveSubscriptionPlan(command, ct)` (flag-guarded) | Enforces `tenant-operations.write` kill-switch before mutating subscription plan settings. | `API/Controllers/TenantOperationsController.cs` |
| `TenantOperationsController.SaveTenantProfile(command, ct)` (flag-guarded) | Enforces `tenant-operations.write` kill-switch before mutating tenant profile settings. | `API/Controllers/TenantOperationsController.cs` |

### Application - Tenant and Subscription Operations (Stage 30.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ITenantOperationsService.GetOnboardingTemplateAsync(ct)` | Returns tenant onboarding template defaults used for new tenant operational setup. | `Application/Interfaces/ISettingsServices.cs` |
| `ITenantOperationsService.SaveOnboardingTemplateAsync(cmd, ct)` | Persists tenant onboarding template defaults to settings storage. | `Application/Interfaces/ISettingsServices.cs` |
| `ITenantOperationsService.GetSubscriptionPlanAsync(ct)` | Returns active tenant subscription plan controls (limits and feature toggles). | `Application/Interfaces/ISettingsServices.cs` |
| `ITenantOperationsService.SaveSubscriptionPlanAsync(cmd, ct)` | Persists subscription plan controls for tenant operations governance. | `Application/Interfaces/ISettingsServices.cs` |
| `ITenantOperationsService.GetTenantProfileAsync(ct)` | Returns tenant profile and branding metadata settings. | `Application/Interfaces/ISettingsServices.cs` |
| `ITenantOperationsService.SaveTenantProfileAsync(cmd, ct)` | Persists tenant profile and branding metadata settings. | `Application/Interfaces/ISettingsServices.cs` |
| `TenantOperationsService.GetOnboardingTemplateAsync(ct)` | Reads onboarding template defaults from portal settings key-value storage. | `Application/Services/SettingsServices.cs` |
| `TenantOperationsService.SaveOnboardingTemplateAsync(cmd, ct)` | Upserts onboarding template fields into settings storage and commits changes. | `Application/Services/SettingsServices.cs` |
| `TenantOperationsService.GetSubscriptionPlanAsync(ct)` | Resolves subscription plan values (price, user limits, feature flags) with safe defaults. | `Application/Services/SettingsServices.cs` |
| `TenantOperationsService.SaveSubscriptionPlanAsync(cmd, ct)` | Upserts subscription plan values to settings storage with guardrails for minimum limits. | `Application/Services/SettingsServices.cs` |
| `TenantOperationsService.GetTenantProfileAsync(ct)` | Reads tenant profile settings (code, display, locale, currency, support contacts). | `Application/Services/SettingsServices.cs` |
| `TenantOperationsService.SaveTenantProfileAsync(cmd, ct)` | Upserts tenant profile settings to support per-tenant branding/profile management. | `Application/Services/SettingsServices.cs` |
| `TenantOperationsController.GetOnboardingTemplate(ct)` | SuperAdmin endpoint to read onboarding template settings. | `API/Controllers/TenantOperationsController.cs` |
| `TenantOperationsController.GetSubscriptionPlan(ct)` | SuperAdmin endpoint to read subscription plan controls. | `API/Controllers/TenantOperationsController.cs` |
| `TenantOperationsController.GetTenantProfile(ct)` | SuperAdmin endpoint to read tenant profile settings. | `API/Controllers/TenantOperationsController.cs` |

### Application - Integration Gateway Contracts (Stage 30.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IOutboundIntegrationGateway.ExecuteAsync(channel, operation, action, ct)` | Executes outbound integration operations through centralized resilience policy handling (retry, timeout, dead-letter). | `Application/Interfaces/IOutboundIntegrationGateway.cs` |
| `IOutboundIntegrationGateway.ExecuteAsync<T>(channel, operation, action, ct)` | Generic execution path for operations that return payloads while preserving resilience behavior. | `Application/Interfaces/IOutboundIntegrationGateway.cs` |
| `IOutboundIntegrationGateway.GetRecentDeadLettersAsync(take, ct)` | Returns recent failed outbound integration operations from dead-letter storage. | `Application/Interfaces/IOutboundIntegrationGateway.cs` |
| `IOutboundIntegrationGateway.GetDeadLetterCountAsync(ct)` | Returns current dead-letter backlog count. | `Application/Interfaces/IOutboundIntegrationGateway.cs` |
| `IOutboundIntegrationGateway.GetPolicySnapshot(channel)` | Returns effective retry/timeout policy for a given integration channel. | `Application/Interfaces/IOutboundIntegrationGateway.cs` |

### Infrastructure - Gateway Runtime (Stage 30.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ResilientOutboundIntegrationGateway.ExecuteAsync(...)` | Applies bounded retry and timeout policy for non-returning integration operations and dead-letters terminal failures. | `Infrastructure/Integrations/ResilientOutboundIntegrationGateway.cs` |
| `ResilientOutboundIntegrationGateway.ExecuteAsync<T>(...)` | Core resilient execution loop with per-channel policy resolution and timeout conversion handling. | `Infrastructure/Integrations/ResilientOutboundIntegrationGateway.cs` |
| `ResilientOutboundIntegrationGateway.GetRecentDeadLettersAsync(...)` | Reads recent dead-letter records from distributed cache-backed storage. | `Infrastructure/Integrations/ResilientOutboundIntegrationGateway.cs` |
| `ResilientOutboundIntegrationGateway.GetDeadLetterCountAsync(...)` | Returns persisted dead-letter count for operational monitoring. | `Infrastructure/Integrations/ResilientOutboundIntegrationGateway.cs` |
| `ResilientOutboundIntegrationGateway.GetPolicySnapshot(channel)` | Exposes effective policy values used for a channel at runtime. | `Infrastructure/Integrations/ResilientOutboundIntegrationGateway.cs` |

### API and Provider Integrations (Stage 30.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CommunicationIntegrationsController.GetGatewayPolicies()` | Exposes configured effective outbound gateway policies for payment/email/sms/push/lms-external channels. | `API/Controllers/CommunicationIntegrationsController.cs` |
| `CommunicationIntegrationsController.GetGatewayDeadLetters(take, ct)` | Returns dead-letter backlog details for admin operational review. | `API/Controllers/CommunicationIntegrationsController.cs` |
| `SmtpEmailDeliveryProvider.SendHtmlAsync(...)` | Routes SMTP HTML email sending through centralized gateway policies. | `Infrastructure/Integrations/SmtpEmailDeliveryProvider.cs` |
| `SmtpEmailDeliveryProvider.SendTemplateAsync(...)` | Routes template-based SMTP email sending through centralized gateway policies. | `Infrastructure/Integrations/SmtpEmailDeliveryProvider.cs` |
| `InAppSupportTicketingProvider.NotifyTicketReplyAsync(...)` | Routes support reply push notifications through centralized gateway policies. | `Infrastructure/Integrations/InAppSupportTicketingProvider.cs` |
| `InAppSupportTicketingProvider.NotifyTicketAssignedAsync(...)` | Routes support assignment push notifications through centralized gateway policies. | `Infrastructure/Integrations/InAppSupportTicketingProvider.cs` |
| `InAppSupportTicketingProvider.NotifyTicketResolvedAsync(...)` | Routes support resolution push notifications through centralized gateway policies. | `Infrastructure/Integrations/InAppSupportTicketingProvider.cs` |
| `InAppAnnouncementBroadcastProvider.BroadcastAsync(...)` | Routes announcement fan-out notification dispatch through centralized gateway policies. | `Infrastructure/Integrations/InAppAnnouncementBroadcastProvider.cs` |

## Issue-Fix Phase 3 � Faculty Workflow Repair (2026-05-07)

### API � CourseController (Phase 3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAll(departmentId, ct)` Faculty branch | Stage 3.1/3.3 � Returns empty list instead of 403 when requested departmentId is outside faculty's assigned departments. | `API/Controllers/CourseController.cs` |
| `GetOfferings(departmentId, semesterId, ct)` Faculty branch | Stage 3.1 � Returns empty list instead of 403; shows ALL offerings in faculty's assigned departments (not filtered by FacultyUserId). | `API/Controllers/CourseController.cs` |
| `GetMyOfferings(ct)` Faculty branch | Stage 3.2/3.5/3.6/3.7 � Changed from `GetOfferingsByFacultyAsync(userId)` to all offerings filtered by faculty's assigned dept IDs, fixing empty Assignments/Attendance/Results/Quizzes dropdowns. | `API/Controllers/CourseController.cs` |

### API � StudentController (Phase 3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAll(departmentId, ct)` Faculty branch | Stage 3.4 � Removed `Forbid()` when departmentId is outside faculty's assigned list; silently scopes results to allowed departments. | `API/Controllers/StudentController.cs` |

### API � FypController (Phase 3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AdminCreate(request, ct)` | Stage 3.8 � Policy changed from `"Admin"` to `"Faculty"` so Faculty can create FYP records for their students. | `API/Controllers/FypController.cs` |

### Web � PortalController (Phase 3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Fyp(departmentId, ct)` Faculty branch | Stage 3.8 � Faculty branch now loads `model.Students` (dept-scoped) so faculty can create FYP records for students. | `Web/Controllers/PortalController.cs` |
| `Enrollments(offeringId, ct)` | Stage 3.3 � Removed dead duplicate Faculty branch; uses `GetCourseOfferingsAsync(null)` for all roles. | `Web/Controllers/PortalController.cs` |

### Web � Views (Phase 3)

| View | Change | Location |
| --- | --- | --- |
| `Fyp.cshtml` | Stage 3.8 � `createFypModal` condition and "Create Project" button extended to include `Faculty` role. | `Web/Views/Portal/Fyp.cshtml` |

## Issue-Fix Phase 4 � Student Workflow Repair (2026-05-07)

### API � AssignmentController (Stage 4.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Submit(SubmitAssignmentRequest, ct)` | Stage 4.1 � Student submits assignment with optional file URL and text content; validates at-least-one rule. | `API/Controllers/AssignmentController.cs` |
| `GetMySubmissions(ct)` | Stage 4.1 � Student retrieves their own submission list with status and marks. | `API/Controllers/AssignmentController.cs` |
| `Grade(GradeSubmissionRequest, ct)` | Stage 4.1 � Faculty grades a submitted assignment with marks and optional feedback. | `API/Controllers/AssignmentController.cs` |

### Web � PortalController (Stages 4.1�4.6)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SubmitAssignment(assignmentId, offeringId, semesterName, textContent, submissionFile, ct)` | Stage 4.1 � Saves uploaded file with GUID filename to `wwwroot/uploads/assignment-submissions/`; calls `SubmitAssignmentAsync`; validates file-or-text requirement. | `Web/Controllers/PortalController.cs` |
| `Timetable(departmentId, semesterId, ct)` (student branch) | Stage 4.2 � Auto-resolves student department from authenticated student profile; falls back to dashboard config; guards against `Guid.Empty`. | `Web/Controllers/PortalController.cs` |
| `Assignments(offeringId, semesterName, ct)` (student branch) | Stage 4.3 � Adds semester filter and semester-scoped offering dropdown for students; merges submission state into assignment rows. | `Web/Controllers/PortalController.cs` |
| `Results(offeringId, semesterName, ct)` (student branch) | Stage 4.4 � Adds semester filter; falls back to student-safe results endpoint when offering-level call returns 403. | `Web/Controllers/PortalController.cs` |
| `Quizzes(offeringId, semesterName, ct)` (student branch) | Stage 4.5 � Adds semester filter; assigns Upcoming/Pending/Completed status badges based on availability windows. | `Web/Controllers/PortalController.cs` |
| `Fyp(departmentId, ct)` (student branch, 4.6) | Stage 4.6 � Blocks students below 8th semester with redirect; shows `Request Completion` button for in-progress projects; shows approval progress. | `Web/Controllers/PortalController.cs` |
| `RequestFypCompletion(fypId, ct)` | Stage 4.6 � Student action to request FYP completion approval from assigned faculty. | `Web/Controllers/PortalController.cs` |
| `ApproveFypCompletion(fypId, ct)` | Stage 4.6 � Faculty action to approve FYP completion; FYP auto-completes when all approvers have approved. | `Web/Controllers/PortalController.cs` |

### API � FypController (Stage 4.6)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `RequestCompletion(id, ct)` | Stage 4.6 � Student endpoint to submit FYP completion request; persists request state. | `API/Controllers/FypController.cs` |
| `ApproveCompletion(id, ct)` | Stage 4.6 � Faculty endpoint to approve FYP completion; auto-transitions FYP to Completed when all assigned faculty have approved. | `API/Controllers/FypController.cs` |

### Web � EduApiClient (Stages 4.1�4.6)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SubmitAssignmentAsync(assignmentId, fileUrl, textContent, ct)` | Stage 4.1 � Posts submission payload to `POST /api/v1/assignment/submit`. | `Web/Services/EduApiClient.cs` |
| `GetMySubmissionsAsync(ct)` | Stage 4.1 � Fetches student's own submissions list. | `Web/Services/EduApiClient.cs` |
| `GetMyStudentProfileAsync(ct)` | Stage 4.2 � Fetches authenticated student profile to auto-resolve department for timetable. | `Web/Services/EduApiClient.cs` |
| `RequestFypCompletionAsync(fypId, ct)` | Stage 4.6 � Calls `POST /api/v1/fyp/{id}/request-completion`. | `Web/Services/EduApiClient.cs` |
| `ApproveFypCompletionAsync(fypId, ct)` | Stage 4.6 � Calls `POST /api/v1/fyp/{id}/approve-completion`. | `Web/Services/EduApiClient.cs` |

### Web � Views (Stage 4.1�4.6)

| View | Change | Location |
| --- | --- | --- |
| `Assignments.cshtml` | Stage 4.1/4.3 � Added `submitAssignmentModal` with file input + text area; semester filter dropdown; semester-scoped offering selector; submission status column. | `Web/Views/Portal/Assignments.cshtml` |
| `Results.cshtml` | Stage 4.4 � Added semester filter; handles student-safe results fallback rendering. | `Web/Views/Portal/Results.cshtml` |
| `Quizzes.cshtml` | Stage 4.5 � Added semester filter; status badges for Upcoming/Pending/Completed. | `Web/Views/Portal/Quizzes.cshtml` |

### Infrastructure / Domain (Stage 4.6)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `FypCompletionApproval` entity / migration | Stage 4.6 � Persists per-faculty approval records; EF migration `Phase4FypCompletionApprovalFlow`. | `Domain/Fyp/`, `Infrastructure/Migrations/` |

  ---

## Issue-Fix Phase 4 � Web User Import + Forced Password Change (2026-05-06)

### Web � Login / Portal Flow

| Function Name | Purpose | Location |
| --- | --- | --- |
| `LoginController.Index(username, password, returnUrl, ct)` | Reads `MustChangePassword` from login response, stores session flag, and redirects to forced password change page when required. | `Web/Controllers/LoginController.cs` |
| `PortalController.OnActionExecuting(context)` | Enforces forced-password-change redirect for all portal actions except the force-change page itself when session flag is set. | `Web/Controllers/PortalController.cs` |
| `PortalController.ForceChangePassword()` | Renders forced password change form for authenticated users flagged for first-login password reset. | `Web/Controllers/PortalController.cs` |
| `PortalController.ForceChangePassword(newPassword, confirmPassword, ct)` | Validates input, calls force-change API endpoint, clears session flag, and redirects to dashboard on success. | `Web/Controllers/PortalController.cs` |

### Web � API Client and Session Identity

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IEduApiClient.IsForcePasswordChangeRequired()` | Exposes whether forced password change is currently required in session state. | `Web/Services/EduApiClient.cs` |
| `IEduApiClient.SetForcePasswordChangeRequired(required)` | Persists/updates forced password change requirement in session and identity cache. | `Web/Services/EduApiClient.cs` |
| `IEduApiClient.ForceChangePasswordAsync(newPassword, ct)` | Calls `POST /api/v1/auth/force-change-password` from portal flow. | `Web/Services/EduApiClient.cs` |
| `EduApiClient.IsForcePasswordChangeRequired()` | Reads forced password change session flag. | `Web/Services/EduApiClient.cs` |
| `EduApiClient.SetForcePasswordChangeRequired(required)` | Writes forced password change session flag and mirrors value into session identity JSON. | `Web/Services/EduApiClient.cs` |
| `EduApiClient.ForceChangePasswordAsync(newPassword, ct)` | Sends password update payload to force-change API endpoint. | `Web/Services/EduApiClient.cs` |
| `SessionIdentity.MustChangePassword` | Carries first-login password reset requirement in session identity. | `Web/Models/Portal/PortalViewModels.cs` |
| `ForceChangePasswordPageModel` | View model backing forced password change page messaging and connection state. | `Web/Models/Portal/PortalViewModels.cs` |

### Integration Tests

| Function Name | Purpose | Location |
| --- | --- | --- |
| `UserImportCsv_StudentRole_ReturnsForbidden()` | Verifies student role cannot call CSV import endpoint. | `tests/Tabsan.EduSphere.IntegrationTests/UserImportAndForceChangeIntegrationTests.cs` |
| `UserImportCsv_Then_ForceChangePassword_WorksEndToEnd()` | Validates import-created account must change password on first login and that old password becomes invalid after change. | `tests/Tabsan.EduSphere.IntegrationTests/UserImportAndForceChangeIntegrationTests.cs` |

## Issue-Fix Phase 6 � Admin Multi-Department Assignment (Backend) (2026-05-06)

### Domain � AdminDepartmentAssignment

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AdminDepartmentAssignment(adminUserId, departmentId)` | Creates an active admin-to-department assignment row. | `Domain/Academic/AdminDepartmentAssignment.cs` |
| `Remove()` | Soft-revokes an assignment while preserving audit history. | `Domain/Academic/AdminDepartmentAssignment.cs` |

### Domain Interface / Infrastructure Repository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetByAdminAsync(adminUserId, ct)` | Returns active departments assigned to a specific admin. | `Domain/Interfaces/IAdminAssignmentRepository.cs`, `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `GetByDepartmentAsync(departmentId, ct)` | Returns active admin assignments for a department. | `Domain/Interfaces/IAdminAssignmentRepository.cs`, `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `GetAsync(adminUserId, departmentId, ct)` | Returns a single active assignment pair if present. | `Domain/Interfaces/IAdminAssignmentRepository.cs`, `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `GetDepartmentIdsForAdminAsync(adminUserId, ct)` | Returns only assigned department IDs for Admin role filtering logic. | `Domain/Interfaces/IAdminAssignmentRepository.cs`, `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `AddAsync(assignment, ct)` | Queues new admin assignment insert. | `Domain/Interfaces/IAdminAssignmentRepository.cs`, `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `Update(assignment)` | Marks assignment as modified (revocation). | `Domain/Interfaces/IAdminAssignmentRepository.cs`, `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `SaveChangesAsync(ct)` | Commits pending assignment mutations. | `Domain/Interfaces/IAdminAssignmentRepository.cs`, `Infrastructure/Repositories/AcademicSupportRepositories.cs` |

### API � DepartmentController (Phase 6 assignment management)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AssignAdminToDepartment(request, ct)` | SuperAdmin endpoint to assign an Admin user to a department. | `API/Controllers/DepartmentController.cs` |
| `RemoveAdminFromDepartment(request, ct)` | SuperAdmin endpoint to revoke an Admin user's department access. | `API/Controllers/DepartmentController.cs` |
| `GetAdminDepartmentAssignments(adminUserId, ct)` | SuperAdmin endpoint to list current assignments for an Admin user. | `API/Controllers/DepartmentController.cs` |
| `GetAdminUsers(ct)` | SuperAdmin endpoint to list active Admin users for assignment UI selection. | `API/Controllers/DepartmentController.cs` |
| `DepartmentController.GetAll(ct)` (admin scope) | Filters departments to only admin-assigned departments for non-SuperAdmin admins. | `API/Controllers/DepartmentController.cs` |

### API � AdminUserController (Phase 6 Stage 6.1 extension)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAll(ct)` | SuperAdmin endpoint to list Admin users (active and inactive) for dedicated management page. | `API/Controllers/AdminUserController.cs` |
| `Create(request, ct)` | SuperAdmin endpoint to create Admin users with hashed credentials. | `API/Controllers/AdminUserController.cs` |
| `Update(id, request, ct)` | SuperAdmin endpoint to update Admin email, active state, and optional password reset. | `API/Controllers/AdminUserController.cs` |

### Web � IEduApiClient / EduApiClient (Phase 6 Stage 6.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAdminUsersAsync(ct)` | Fetches active Admin users for SuperAdmin assignment UI. | `Web/Services/EduApiClient.cs` |
| `CreateAdminUserAsync(username, email, password, ct)` | Creates an Admin user from portal management UI. | `Web/Services/EduApiClient.cs` |
| `UpdateAdminUserAsync(userId, email, isActive, newPassword, ct)` | Updates Admin account state and optional password from portal management UI. | `Web/Services/EduApiClient.cs` |
| `GetAdminDepartmentIdsAsync(adminUserId, ct)` | Loads currently assigned department IDs for a selected Admin user. | `Web/Services/EduApiClient.cs` |
| `AssignAdminToDepartmentAsync(adminUserId, departmentId, ct)` | Assigns an Admin user to a department from portal UI workflows. | `Web/Services/EduApiClient.cs` |
| `RemoveAdminFromDepartmentAsync(adminUserId, departmentId, ct)` | Revokes an Admin user department assignment from portal UI workflows. | `Web/Services/EduApiClient.cs` |
| `DeleteWithBodyAsync(path, payload, ct)` | Sends DELETE requests with JSON payload for APIs that require request-body deletes. | `Web/Services/EduApiClient.cs` |

### Web � PortalController / Departments ViewModel (Phase 6 Stage 6.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Departments(selectedAdminUserId, ct)` | Loads departments plus SuperAdmin admin-assignment UI state (admins + selected assignments). | `Web/Controllers/PortalController.cs` |
| `UpdateAdminDepartmentAssignments(adminUserId, departmentIds, ct)` | Diffs selected vs current assignments and applies add/remove operations. | `Web/Controllers/PortalController.cs` |
| `AdminUsers(selectedAdminUserId, ct)` | Loads dedicated Admin user management page state (admins, departments, assignments). | `Web/Controllers/PortalController.cs` |
| `CreateAdminUser(username, email, password, departmentIds, ct)` | Creates Admin user and applies initial multi-department assignments. | `Web/Controllers/PortalController.cs` |
| `UpdateAdminUser(userId, email, isActive, newPassword, departmentIds, ct)` | Updates Admin account details and synchronizes multi-department assignments. | `Web/Controllers/PortalController.cs` |
| `SyncAdminDepartmentAssignmentsAsync(adminUserId, departmentIds, ct)` | Shared helper to reconcile selected and persisted admin-department assignments. | `Web/Controllers/PortalController.cs` |
| `AdminUserLookupItem` | Carries admin identity data for assignment selection controls. | `Web/Models/Portal/PortalViewModels.cs` |
| `DepartmentsPageModel.AdminUsers/SelectedAdminUserId/AssignedDepartmentIds` | Stores assignment-management UI state for departments page. | `Web/Models/Portal/PortalViewModels.cs` |
| `AdminUsersPageModel` | Stores dedicated Admin user management page state and assignment selections. | `Web/Models/Portal/PortalViewModels.cs` |

### API � CourseController / ReportController (Stage 5.4 closure)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CourseController.GetAll(departmentId, ct)` (admin scope) | Limits admin course list to assigned departments. | `API/Controllers/CourseController.cs` |
| `CourseController.GetOfferings(semesterId, departmentId, ct)` (admin scope) | Limits admin offering list to assigned departments. | `API/Controllers/CourseController.cs` |
| `CourseController.GetMyOfferings(ct)` (admin scope) | Limits admin "my offerings" list to assigned departments. | `API/Controllers/CourseController.cs` |

### Web � PortalController Report Pages (Stage 5.4 portal UX completion)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ReportAttendance(...)` (admin guard) | Stage 5.4 � Shows friendly guidance message for Admin when neither `departmentId` nor `offeringId` is selected; prevents raw API 400 error. | `Web/Controllers/PortalController.cs` |
| `ReportResults(...)` (admin guard) | Stage 5.4 � Same isAdminOnly guidance pattern for Results report page. | `Web/Controllers/PortalController.cs` |
| `ReportAssignments(...)` (admin guard) | Stage 5.4 � Same isAdminOnly guidance pattern for Assignments report page. | `Web/Controllers/PortalController.cs` |
| `ReportQuizzes(...)` (admin guard) | Stage 5.4 � Same isAdminOnly guidance pattern for Quizzes report page. | `Web/Controllers/PortalController.cs` |
| `ReportGpa(...)` (admin guard) | Stage 5.4 � Shows guidance message for Admin when no `departmentId` selected. | `Web/Controllers/PortalController.cs` |
| `ReportEnrollment(...)` (admin guard) | Stage 5.4 � Shows guidance message for Admin when no `departmentId` selected; previously always-fired and returned raw 400. | `Web/Controllers/PortalController.cs` |
| `ReportSemesterResults(...)` (admin guard) | Stage 5.4 � Shows guidance message for Admin when no `departmentId` selected (within semester gate). | `Web/Controllers/PortalController.cs` |
| `ReportLowAttendance(...)` (admin guard) | Stage 5.4 � Shows guidance message for Admin when neither `departmentId` nor `courseOfferingId` selected. | `Web/Controllers/PortalController.cs` |
| `ReportFypStatus(...)` (admin guard) | Stage 5.4 � Shows guidance message for Admin when no `departmentId` selected. | `Web/Controllers/PortalController.cs` |

## Refactoring-Hosting-Security � Part A + Part B (2026-05-07)

### API � ExceptionHandlingMiddleware (Part B)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ExceptionHandlingMiddleware.InvokeAsync(context)` | Global exception handler; catches all unhandled exceptions; logs full detail in all environments; maps exception types to HTTP status codes (400/401/404/405/503/500); returns sanitised JSON problem-detail response with no stack trace in production; includes `traceId` in every error response. | `API/Middleware/ExceptionHandlingMiddleware.cs` |
| `ExceptionHandlingMiddleware.HandleExceptionAsync(context, exception)` | Builds `application/problem+json` response payload; exposes `detail`/`exception` only in Development environment; determines HTTP status from exception type. | `API/Middleware/ExceptionHandlingMiddleware.cs` |

### API � FileUploadValidator (Part B)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `FileUploadValidator.ValidateAsync(file)` | Static async validator for uploaded academic documents; checks file is present, size = 5 MB, extension in allowlist (.pdf/.jpg/.jpeg/.png/.doc/.docx), MIME type matches extension, and magic bytes match expected binary header. Returns null on success or user-facing error string on failure. | `API/Services/FileUploadValidator.cs` |
| `FileUploadValidator.ValidateImageAsync(file)` | Static async validator for logo/image uploads; checks size = 2 MB, extension in allowlist (.png/.jpg/.jpeg/.gif/.svg/.webp), MIME type matches, and magic bytes for raster types (SVG uses ext+MIME only). Returns null on success or user-facing error string on failure. | `API/Services/FileUploadValidator.cs` |
| `FileUploadValidator.ValidateCoreAsync(file, allowedExts, allowedMimes, magicMap, maxBytes)` | Private shared core of both validate methods; applies size, extension, MIME, and magic-byte checks using caller-provided dictionaries and size limit. | `API/Services/FileUploadValidator.cs` |

## Phase 28 � Scalability Architecture (2026-05-09)

### API � Program.cs (Stage 28.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AddResponseCompression(...)` registration | Enables Brotli/Gzip compression on API responses for scale-out traffic efficiency. | `API/Program.cs` |
| `AddControllers().AddJsonOptions(...)` | Omits null JSON fields from API responses for lighter payloads. | `API/Program.cs` |

### Web � Program.cs / EduApiClient (Stage 28.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AddDataProtection().SetApplicationName(...)` + optional `PersistKeysToFileSystem(...)` | Configures protected cookie encryption with optional shared key ring for multi-node web deployments. | `Web/Program.cs` |
| `AddResponseCompression(...)` registration | Enables Brotli/Gzip compression on portal responses. | `Web/Program.cs` |
| `ReadCookie(key)` | Reads and unprotects stateless portal connection/auth state from HttpOnly cookies. | `Web/Services/EduApiClient.cs` |
| `WriteCookie(key, value)` | Protects and persists portal connection/auth state in cookies instead of server session. | `Web/Services/EduApiClient.cs` |
| `DeleteCookie(key)` | Clears protected state cookies during logout or connection reset. | `Web/Services/EduApiClient.cs` |
| `SaveConnection(model)` | Stores or clears API base URL, JWT token, department, and identity state using protected cookies. | `Web/Services/EduApiClient.cs` |

### API / Application � Phase 28 Stage 28.2 Foundation

| Function Name | Purpose | Location |
| --- | --- | --- |
| `NotificationFanoutQueue.Enqueue(workItem)` | Queues large notification recipient batches for background processing instead of blocking the request path. | `API/Services/NotificationFanoutQueue.cs` |
| `NotificationFanoutWorker.ExecuteAsync(...)` | Background worker that drains deferred fan-out batches and persists notification recipients in chunks. | `API/Services/NotificationFanoutWorker.cs` |
| `NotificationService.FanOutRecipientsAsync(...)` | Chooses between inline recipient persistence and deferred background fan-out based on recipient count. | `Application/Notifications/NotificationService.cs` |
| `ReportService.GetCatalogAsync(roleName, ct)` | Reads/writes report catalog results through distributed cache for shared hot-read reuse across nodes. | `Infrastructure/Reporting/ReportService.cs` |

### API / Application � Advanced Track Phase 28 Stage 28.2 (Parent Read-Only Views)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetLinkedStudentResults(studentProfileId, ct)` | Returns published results for a linked student under the authenticated parent/admin/superadmin context. | `API/Controllers/ParentPortalController.cs` |
| `GetLinkedStudentAttendance(studentProfileId, courseOfferingId, ct)` | Returns attendance rows for a linked student with optional offering filter. | `API/Controllers/ParentPortalController.cs` |
| `GetLinkedStudentAnnouncements(studentProfileId, courseOfferingId, ct)` | Returns announcements scoped to the linked student's active enrollments. | `API/Controllers/ParentPortalController.cs` |
| `GetLinkedStudentTimetable(studentProfileId, timetableId, ct)` | Returns the linked student's published timetable view, optionally by timetable id. | `API/Controllers/ParentPortalController.cs` |
| `GetLinkedStudentResultsAsync(parentUserId, studentProfileId, ct)` | Service-layer linked-student authorization + published result retrieval orchestration. | `Application/Academic/ParentPortalService.cs` |
| `GetLinkedStudentAttendanceAsync(parentUserId, studentProfileId, courseOfferingId, ct)` | Service-layer linked-student authorization + attendance retrieval orchestration. | `Application/Academic/ParentPortalService.cs` |
| `GetLinkedStudentAnnouncementsAsync(parentUserId, studentProfileId, courseOfferingId, ct)` | Service-layer linked-student authorization + active-enrollment announcement aggregation. | `Application/Academic/ParentPortalService.cs` |
| `GetLinkedStudentTimetableAsync(parentUserId, studentProfileId, timetableId, ct)` | Service-layer linked-student authorization + published department timetable resolution. | `Application/Academic/ParentPortalService.cs` |
| `GetLinkedStudentAsync(parentUserId, studentProfileId, ct)` | Internal active-link guard and student profile resolution used by parent read-only views. | `Application/Academic/ParentPortalService.cs` |

### API / Application / BackgroundJobs � Advanced Track Phase 28 Stage 28.3 (Parent Notifications)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `PublishAsync(studentProfileId, courseOfferingId, resultType, publishedByUserId, ct)` | Publishes an individual result and triggers linked-parent notification fan-out for the published result event. | `Application/Assignments/ResultService.cs` |
| `PublishAllForOfferingAsync(courseOfferingId, publishedByUserId, ct)` | Publishes all draft results for an offering and triggers linked-parent notification fan-out for affected students. | `Application/Assignments/ResultService.cs` |
| `NotifyParentsForPublishedResultsAsync(studentProfileIds, courseOfferingId, normalizedResultType, ct)` | Internal helper that resolves active parent links and dispatches result notifications to linked parent users. | `Application/Assignments/ResultService.cs` |
| `GetActiveParentUserIdsByStudentAsync(studentProfileId, ct)` | Resolves active linked parent user IDs for a single student profile for notification fan-out. | `Domain/Interfaces/IParentStudentLinkRepository.cs`, `Infrastructure/Repositories/Phase26Repositories.cs` |
| `GetActiveParentUserIdsByStudentsAsync(studentProfileIds, ct)` | Resolves active linked parent user IDs for multiple student profiles in one query for batch fan-out. | `Domain/Interfaces/IParentStudentLinkRepository.cs`, `Infrastructure/Repositories/Phase26Repositories.cs` |
| `RunCheckAsync(ct)` (parent alert path) | Sends linked-parent attendance warning notifications for student-offering pairs below the configured threshold. | `BackgroundJobs/AttendanceAlertJob.cs` |
| `BroadcastAsync(offeringId, title, body, ct)` | Includes linked parents in announcement recipient fan-out for key academic updates. | `Infrastructure/Integrations/InAppAnnouncementBroadcastProvider.cs` |

### API � Phase 28 Stage 28.3 Slice 5 (Portal Branding Logo Storage)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `UploadLogo(file, ct)` | Validates and stores portal logo assets through `IMediaStorageService` and returns a storage-backed logo URL instead of an inline base64 payload. | `API/Controllers/PortalSettingsController.cs` |
| `GetLogoFile(storageKey, ct)` | Streams provider-backed portal logo content by storage key for anonymous branding rendering on login/landing flows. | `API/Controllers/PortalSettingsController.cs` |
| `ResolveImageContentType(path)` | Maps stored logo key/file extension to response content type for logo streaming responses. | `API/Controllers/PortalSettingsController.cs` |

### Application/API � Phase 28 Stage 28.3 Slice 6 (Temporary Signed Read URLs)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GenerateTemporaryReadUrlAsync(storageKey, ttl, ct)` | Storage-contract method for provider-generated temporary read URLs used by redirect-first media serving paths. | `Application/Interfaces/IMediaStorageService.cs` |
| `CreateSignature(storageKey, expiresAt)` | Internal helper that signs temporary read URL payloads using `MediaStorage:SignedUrlSecret`. | `API/Services/LocalMediaStorageService.cs`, `API/Services/BlobMediaStorageService.cs` |
| `GetLogoFile(storageKey, ct)` (redirect-first path) | Updated to prefer provider-generated temporary URL redirects and fall back to in-process streaming when redirect URL is unavailable. | `API/Controllers/PortalSettingsController.cs` |

### API � Phase 28 Stage 28.3 Slice 7 (Local Signed URL Validation)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetLogoFile(storageKey, exp, sig, ct)` | Enforces signed local read parameters (`exp`, `sig`) when configured, redirects unsigned legacy requests to signed URLs, and maintains provider redirect-first streaming flow. | `API/Controllers/PortalSettingsController.cs` |
| `BuildLocalSignedLogoUrl(storageKey, ttl)` | Generates short-lived local signed logo endpoint URLs for legacy unsigned-link compatibility redirects. | `API/Controllers/PortalSettingsController.cs` |
| `IsValidLocalSignature(storageKey, expiresAt, providedSignature)` | Validates local signed read requests via fixed-time HMAC comparison. | `API/Controllers/PortalSettingsController.cs` |
| `TryDecodeHex(value, out bytes)` | Parses signature hex safely for local signed URL validation flow. | `API/Controllers/PortalSettingsController.cs` |

### API � Phase 28 Stage 28.3 Slice 8 (Certificate Signed Media Reads)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DownloadCertificate(id, ct)` (redirect-first path) | Validates caller access, preserves legacy path behavior, and redirects provider-backed certificates to temporary or signed tokenized read endpoints. | `API/Controllers/GraduationController.cs` |
| `GetCertificateFile(storageKey, exp, sig, ct)` | Streams provider-backed graduation certificates by storage key with signed URL validation when configured. | `API/Controllers/GraduationController.cs` |
| `BuildLocalSignedCertificateUrl(storageKey, ttl)` | Builds short-lived signed local certificate URLs for tokenized reads. | `API/Controllers/GraduationController.cs` |

### Application/API � Phase 28 Stage 28.3 Slice 9 (Storage Metadata Contract)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetMetadataAsync(storageKey, ct)` | Storage-contract method for retrieving object metadata such as content type and length. | `Application/Interfaces/IMediaStorageService.cs` |
| `MediaStorageObjectMetadata` | Metadata record carrying storage key, content type, and length for provider-backed media objects. | `Application/Interfaces/IMediaStorageService.cs` |
| `ResolveContentType(path)` | Provider helper that derives canonical content type for stored object metadata and save results. | `API/Services/LocalMediaStorageService.cs`, `API/Services/BlobMediaStorageService.cs` |

### Application/API � Phase 28 Stage 28.3 Slice 10 (Integrity and Disposition Metadata)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SaveAsync(content, category, fileExtension, contentType, downloadFileName, ct)` | Extended storage-contract save method that accepts content-type and download-filename metadata for persisted media objects. | `Application/Interfaces/IMediaStorageService.cs` |
| `MediaStorageSaveResult` | Storage save result now carries object hash and optional download filename alongside key/reference/content metadata. | `Application/Interfaces/IMediaStorageService.cs` |
| `CopyWithHashAsync(source, destination, ct)` | Writes object bytes while computing a SHA-256 hash for persisted integrity metadata. | `API/Services/LocalMediaStorageService.cs`, `API/Services/BlobMediaStorageService.cs` |
| `WriteMetadataAsync(storageKey, metadata, ct)` | Persists sidecar media metadata for later read/redirect operations. | `API/Services/LocalMediaStorageService.cs`, `API/Services/BlobMediaStorageService.cs` |
| `ReadMetadataAsync(fullPath, ct)` | Reads sidecar media metadata from persisted storage. | `API/Services/LocalMediaStorageService.cs`, `API/Services/BlobMediaStorageService.cs` |
| `GetCertificateFile(storageKey, exp, sig, download, ct)` | Streams certificate files with signed URL validation and filename-preserving download behavior. | `API/Controllers/GraduationController.cs` |
| `ModuleEntitlementResolver.IsActiveAsync(moduleKey, ct)` | Uses local memory + distributed cache to share module-activation decisions across API nodes. | `Infrastructure/Modules/ModuleEntitlementResolver.cs` |

### Infrastructure � Phase 29 Stage 29.1 (Index Baseline and Query Contracts)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GraduationApplicationConfiguration.Configure(builder)` | Adds recency-aware indexes for student and status filtered graduation application queues. | `Infrastructure/Persistence/Configurations/AcademicConfigurations.cs` |
| `SupportTicketConfiguration.Configure(builder)` | Adds composite recency indexes for submitter, assignee, and department/status ticket views. | `Infrastructure/Persistence/Configurations/SupportTicketConfiguration.cs` |
| `NotificationRecipientConfiguration.Configure(builder)` | Adds user inbox index for newest-first notification paging. | `Infrastructure/Persistence/Configurations/NotificationConfigurations.cs` |
| `PaymentReceiptConfiguration.Configure(builder)` | Adds receipt history and unpaid-queue composite indexes for student/status date filtering. | `Infrastructure/Persistence/Configurations/PaymentReceiptConfiguration.cs` |
| `QuizAttemptConfiguration.Configure(builder)` | Adds student/quiz recency indexes for attempt history queries. | `Infrastructure/Persistence/Configurations/QuizConfigurations.cs` |
| `UserSessionConfiguration.Configure(builder)` | Adds composite user-session recency index for most-recent refresh session lookup. | `Infrastructure/Persistence/Configurations/UserSessionConfiguration.cs` |
| `_20260510_Phase29_IndexBaseline.Up(migrationBuilder)` | Applies the Stage 29.1 index baseline migration to SQL Server. | `Infrastructure/Migrations/20260509155457_20260510_Phase29_IndexBaseline.cs` |
| `ParentStudentLinkConfiguration.Configure(builder)` | Adds composite index `(StudentProfileId, IsActive)` for parent-notification fan-out linked-student lookups. | `Infrastructure/Persistence/Configurations/ParentStudentLinkConfiguration.cs` |
| `_20260514_Phase29_ParentLinkStudentActiveIndexHotPath.Up(migrationBuilder)` | Applies the Stage 29.1 follow-up parent-link hot-path index migration to SQL Server. | `Infrastructure/Migrations/20260514134000_20260514_Phase29_ParentLinkStudentActiveIndexHotPath.cs` |

### Application/API/Web � Phase 29 Stage 29.2 Slice 1 (Helpdesk Pagination)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetTicketsAsync(callerId, callerRole, departmentIds, status, page, pageSize, ct)` | Returns role-scoped helpdesk tickets as a paged result instead of an unbounded list. | `Application/Interfaces/IHelpdeskService.cs`, `Application/Helpdesk/HelpdeskService.cs` |
| `TicketSummaryPageDto` | Carries paged ticket items plus page metadata across the API boundary. | `Application/DTOs/Helpdesk/HelpdeskDTOs.cs` |
| `GetTicketsBySubmitterAsync(submitterId, status, skip, take, ct)` | Fetches a paged student submitter ticket slice directly in SQL. | `Domain/Interfaces/IHelpdeskRepository.cs`, `Infrastructure/Repositories/HelpdeskRepository.cs` |
| `GetTicketsByDepartmentAsync(departmentIds, status, skip, take, ct)` | Fetches a paged admin/superadmin department ticket slice directly in SQL. | `Domain/Interfaces/IHelpdeskRepository.cs`, `Infrastructure/Repositories/HelpdeskRepository.cs` |
| `GetTicketsByAssigneeOrSubmitterAsync(userId, status, skip, take, ct)` | Fetches a paged faculty-visible ticket slice without combining unbounded in-memory lists. | `Domain/Interfaces/IHelpdeskRepository.cs`, `Infrastructure/Repositories/HelpdeskRepository.cs` |
| `GetTickets(status, page, pageSize, ct)` | API endpoint exposing paged helpdesk ticket listing. | `API/Controllers/HelpdeskController.cs` |
| `GetTicketsAsync(status, page, pageSize, ct)` | Web API client method that consumes the paged helpdesk response. | `Web/Services/EduApiClient.cs` |
| `Helpdesk(status, page, ct)` | Portal action that binds and renders paged helpdesk results. | `Web/Controllers/PortalController.cs` |

### Application/API/Web � Phase 29 Stage 29.2 Slice 2 (Graduation Pagination)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GraduationApplicationPageDto` | Carries paged graduation application items plus paging metadata at the application/API boundary. | `Application/DTOs/Academic/GraduationDTOs.cs` |
| `GetMyApplicationsAsync(studentProfileId, page, pageSize, ct)` | Returns paged student graduation applications instead of an unbounded list. | `Application/Interfaces/IGraduationService.cs`, `Application/Academic/GraduationService.cs` |
| `GetApplicationsAsync(departmentId, statusFilter, page, pageSize, ct)` | Returns paged admin/superadmin graduation applications with optional filters. | `Application/Interfaces/IGraduationService.cs`, `Application/Academic/GraduationService.cs` |
| `GetByStudentPagedAsync(studentProfileId, skip, take, ct)` | Executes SQL-side paging for student graduation application history. | `Domain/Interfaces/IGraduationRepository.cs`, `Infrastructure/Repositories/GraduationRepository.cs` |
| `GetByDepartmentPagedAsync(departmentId, status, skip, take, ct)` | Executes SQL-side paging for department-scoped graduation queues. | `Domain/Interfaces/IGraduationRepository.cs`, `Infrastructure/Repositories/GraduationRepository.cs` |
| `GetAllPagedAsync(status, skip, take, ct)` | Executes SQL-side paging for global graduation queues. | `Domain/Interfaces/IGraduationRepository.cs`, `Infrastructure/Repositories/GraduationRepository.cs` |
| `GetMyApplications(page, pageSize, ct)` | API endpoint exposing paged student graduation applications. | `API/Controllers/GraduationController.cs` |
| `GetAll(departmentId, status, page, pageSize, ct)` | API endpoint exposing paged staff graduation applications with filters. | `API/Controllers/GraduationController.cs` |
| `GetMyGraduationApplicationsAsync(page, pageSize, ct)` | Web client method consuming paged student graduation application responses. | `Web/Services/EduApiClient.cs` |
| `GetGraduationApplicationsAsync(departmentId, status, page, pageSize, ct)` | Web client method consuming paged staff graduation application responses. | `Web/Services/EduApiClient.cs` |
| `GraduationApply(page, ct)` | Portal action rendering paged student graduation application history. | `Web/Controllers/PortalController.cs` |
| `GraduationApplications(status, departmentId, page, ct)` | Portal action rendering paged staff graduation application lists with filters. | `Web/Controllers/PortalController.cs` |

### Application/API/Web � Phase 29 Stage 29.2 Slice 3 (Payment Receipts Pagination)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `PaymentReceiptPageDto` | Carries paged payment receipt items plus paging metadata at the application/API boundary. | `Application/DTOs/StudentLifecycleDtos.cs` |
| `GetAllReceiptsAsync(page, pageSize, ct)` | Returns paged admin payment receipts instead of an unbounded list. | `Application/Interfaces/IStudentLifecycleService.cs`, `Application/Services/StudentLifecycleService.cs` |
| `GetReceiptsByUserAsync(userId, page, pageSize, ct)` | Returns paged student payment receipts instead of an unbounded list. | `Application/Interfaces/IStudentLifecycleService.cs`, `Application/Services/StudentLifecycleService.cs` |
| `GetAllReceiptsPagedAsync(skip, take, ct)` | Executes SQL-side paging for admin payment receipt history. | `Domain/Interfaces/IStudentLifecycleRepository.cs`, `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetActiveReceiptsByStudentPagedAsync(studentProfileId, skip, take, ct)` | Executes SQL-side paging for student payment receipt history. | `Domain/Interfaces/IStudentLifecycleRepository.cs`, `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `CountAllReceiptsAsync(ct)` | Returns the total number of admin-visible payment receipts for paging metadata. | `Domain/Interfaces/IStudentLifecycleRepository.cs`, `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `CountActiveReceiptsByStudentAsync(studentProfileId, ct)` | Returns the total number of student-visible payment receipts for paging metadata. | `Domain/Interfaces/IStudentLifecycleRepository.cs`, `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetAll(page, pageSize, ct)` | API endpoint exposing paged admin payment receipts. | `API/Controllers/PaymentReceiptController.cs` |
| `GetMine(page, pageSize, ct)` | API endpoint exposing paged student payment receipts. | `API/Controllers/PaymentReceiptController.cs` |
| `GetAllPaymentsAsync(page, pageSize, ct)` | Web client method consuming paged admin payment receipt responses. | `Web/Services/EduApiClient.cs` |
| `GetMyPaymentsAsync(page, pageSize, ct)` | Web client method consuming paged student payment receipt responses. | `Web/Services/EduApiClient.cs` |
| `GetPaymentsByStudentAsync(studentId, page, pageSize, ct)` | Web client method consuming paged receipts for a selected student. | `Web/Services/EduApiClient.cs` |
| `Payments(studentId, page, ct)` | Portal action rendering paged payment receipt lists and student-filtered navigation. | `Web/Controllers/PortalController.cs` |

### Operations � Phase 29 Stage 29.3 (Data Lifecycle and Maintenance)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `3-Phase29-ArchivePolicy.sql` | Defines archive/retention policy windows, shows dry-run candidate counts, and can apply batched cleanup for old operational rows. | `Scripts/3-Phase29-ArchivePolicy.sql` |
| `4-Phase29-IndexMaintenance.sql` | Produces fragmentation-based index maintenance plans and optionally executes REORGANIZE/REBUILD operations. | `Scripts/4-Phase29-IndexMaintenance.sql` |
| `5-Phase29-CapacityGrowthDashboard.sql` | Provides table-size capacity snapshots and recent growth telemetry for high-volume tables. | `Scripts/5-Phase29-CapacityGrowthDashboard.sql` |
| `Scripts/README.md` Phase 29 section | Documents operational run commands for archive policy, index maintenance, and growth dashboards. | `Scripts/README.md` |

### API/Application/Infrastructure � Phase 30 Stage 30.1 (Redis Caching)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DashboardCompositionController.GetContext(ct)` | Adds distributed cache-backed dashboard context payload reuse for role + institution-policy scoped dashboard summaries. | `API/Controllers/DashboardCompositionController.cs` |
| `ReportService.GetAttendanceSummaryAsync(request, ct)` | Adds distributed cache for attendance summary report reads keyed by report filter parameters. | `Infrastructure/Reporting/ReportService.cs` |
| `ReportService.GetResultSummaryAsync(request, ct)` | Adds distributed cache for result summary report reads keyed by report filter parameters. | `Infrastructure/Reporting/ReportService.cs` |
| `ReportService.GetAssignmentSummaryAsync(request, ct)` | Adds distributed cache for assignment summary report reads keyed by report filter parameters. | `Infrastructure/Reporting/ReportService.cs` |
| `ReportService.GetQuizSummaryAsync(request, ct)` | Adds distributed cache for quiz summary report reads keyed by report filter parameters. | `Infrastructure/Reporting/ReportService.cs` |
| `ReportService.GetGpaReportAsync(request, ct)` | Adds distributed cache for GPA report summary reads keyed by report filter parameters. | `Infrastructure/Reporting/ReportService.cs` |
| `ReportService.GetEnrollmentSummaryAsync(request, ct)` | Adds distributed cache for enrollment summary report reads keyed by report filter parameters. | `Infrastructure/Reporting/ReportService.cs` |
| `ReportService.GetSemesterResultsAsync(request, ct)` | Adds distributed cache for semester-results summary reads keyed by report filter parameters. | `Infrastructure/Reporting/ReportService.cs` |

### API/Infrastructure � Phase 30 Stage 30.2 (Background Job Offloading)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AnalyticsController.QueueExportJob(...)` | Queues heavy analytics export generation for background processing (performance/attendance, PDF/Excel). | `API/Controllers/AnalyticsController.cs` |
| `AnalyticsController.GetExportJob(jobId, ct)` | Returns status for queued analytics export jobs. | `API/Controllers/AnalyticsController.cs` |
| `AnalyticsController.DownloadExportJob(jobId, ct)` | Downloads completed queued analytics export payloads. | `API/Controllers/AnalyticsController.cs` |
| `AnalyticsExportJobQueue.Enqueue(workItem)` | Enqueues analytics export work items for background execution. | `API/Services/AnalyticsExportJobQueue.cs` |
| `AnalyticsExportJobQueue.DequeueAllAsync(ct)` | Streams queued analytics export work items to hosted workers. | `API/Services/AnalyticsExportJobQueue.cs` |
| `AnalyticsExportJobStore.SetStateAsync(state, ct)` | Persists queued/running/completed/failed analytics export job state in distributed cache. | `API/Services/AnalyticsExportJobStore.cs` |
| `AnalyticsExportJobStore.GetStateAsync(jobId, ct)` | Reads analytics export job status for status endpoint responses. | `API/Services/AnalyticsExportJobStore.cs` |
| `AnalyticsExportJobStore.SetPayloadAsync(jobId, bytes, ct)` | Stores generated analytics export payload for deferred download. | `API/Services/AnalyticsExportJobStore.cs` |
| `AnalyticsExportJobStore.GetPayloadAsync(jobId, ct)` | Retrieves generated analytics export payload for download endpoint. | `API/Services/AnalyticsExportJobStore.cs` |
| `AnalyticsExportJobWorker.ExecuteAsync(stoppingToken)` | Executes queued analytics export jobs and writes final payload/state results. | `API/Services/AnalyticsExportJobWorker.cs` |

### API/Infrastructure � Phase 30 Stage 30.3 (Reliability Controls)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `BackgroundJobReliabilityOptions` | Defines configurable retry attempts, retry delay, and operational alert threshold for background job pipelines. | `API/Services/BackgroundJobReliabilityOptions.cs`, `API/appsettings.json`, `API/appsettings.Development.json`, `API/appsettings.Production.json` |
| `BackgroundJobHealthTracker.GetSnapshot()` | Provides live per-pipeline background job metrics (processed/succeeded/failed/retried/consecutive failures) for runtime health monitoring. | `API/Services/BackgroundJobReliabilityOptions.cs` |
| `ResultPublishJobWorker.ExecuteAsync(stoppingToken)` (retry-enhanced) | Adds bounded retry + backoff for transient failures and emits threshold-based consecutive-failure alerts for result publish workloads. | `API/Services/ResultPublishJobWorker.cs` |
| `ReportExportJobWorker.ExecuteAsync(stoppingToken)` (retry-enhanced) | Adds bounded retry + backoff for transient export failures and emits threshold-based consecutive-failure alerts for report export workloads. | `API/Services/ReportExportJobWorker.cs` |
| `AnalyticsExportJobWorker.ExecuteAsync(stoppingToken)` (retry-enhanced) | Adds bounded retry + backoff for transient analytics export failures and emits threshold-based consecutive-failure alerts for analytics workloads. | `API/Services/AnalyticsExportJobWorker.cs` |
| `GET /health/background-jobs` | Exposes background worker reliability configuration and live processing/retry/failure metrics for operational monitoring. | `API/Program.cs` |

### API � Phase 28 Stage 28.2 Completion

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ResultController.QueuePublishAll(courseOfferingId, ct)` | Queues result publish-all recalculation work for asynchronous background processing. | `API/Controllers/ResultController.cs` |
| `ResultController.GetPublishAllJob(jobId, ct)` | Returns status for queued result publish-all jobs. | `API/Controllers/ResultController.cs` |
| `ReportController.QueueResultSummaryExport(...)` | Queues result-summary export generation (excel/csv/pdf) for background processing. | `API/Controllers/ReportController.cs` |
| `ReportController.GetExportJob(jobId, ct)` | Returns status for queued report export jobs. | `API/Controllers/ReportController.cs` |
| `ReportController.DownloadExportJob(jobId, ct)` | Downloads completed queued report export payloads. | `API/Controllers/ReportController.cs` |
| `ResultPublishJobWorker.ExecuteAsync(...)` | Background worker that executes queued result publish-all jobs and persists job state. | `API/Services/ResultPublishJobWorker.cs` |
| `ReportExportJobWorker.ExecuteAsync(...)` | Background worker that executes queued result-summary export jobs and stores payloads for deferred download. | `API/Services/ReportExportJobWorker.cs` |

### API � Phase 28 Stage 28.3 Slice 1

| Function Name | Purpose | Location |
| --- | --- | --- |
| `PaymentReceiptController.SubmitProof(id, file, ct)` | Persists student payment-proof uploads through the storage provider abstraction and stores object-key references. | `API/Controllers/PaymentReceiptController.cs` |
| `IMediaStorageService.SaveAsync(content, category, fileExtension, ct)` | Defines provider-agnostic media persistence contract for local or future object storage implementations. | `API/Services/IMediaStorageService.cs` |
| `LocalMediaStorageService.SaveAsync(content, category, fileExtension, ct)` | Stores uploaded media in a configurable local root while issuing stable object keys for metadata-only DB references. | `API/Services/LocalMediaStorageService.cs` |
| `LocalMediaStorageService.BuildReference(objectKey)` | Builds an external reference using optional `PublicBaseUrl`, enabling CDN/object-storage style references without API contract changes. | `API/Services/LocalMediaStorageService.cs` |

### Application / API � Phase 28 Stage 28.3 Slice 2

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IMediaStorageService.ReadAsBytesAsync(storageKey, ct)` | Defines provider-agnostic read path for stored media by storage key. | `Application/Interfaces/IMediaStorageService.cs` |
| `LocalMediaStorageService.ReadAsBytesAsync(storageKey, ct)` | Resolves a storage key against configured local root and returns file bytes if present. | `API/Services/LocalMediaStorageService.cs` |
| `GraduationService.GenerateCertificateAsync(applicationId, ct)` | Persists generated certificate PDF through storage provider and stores resulting storage key in application record. | `Application/Academic/GraduationService.cs` |
| `GraduationService.DownloadCertificateAsync(applicationId, requestingStudentProfileId, ct)` | Downloads certificate via storage provider for new keys, while preserving legacy `/certificates/*` file-path fallback. | `Application/Academic/GraduationService.cs` |

### API / Infrastructure � Phase 28 Stage 28.3 Slice 3

| Function Name | Purpose | Location |
| --- | --- | --- |
| `LicenseController.Upload(file, ct)` | Uses media storage provider to persist/read/delete temporary license uploads before activation. | `API/Controllers/LicenseController.cs` |
| `LicenseValidationService.ActivateFromBytesAsync(fileBytes, requestDomain, ct)` | Validates and activates license directly from in-memory bytes, enabling provider-backed upload flows. | `Infrastructure/Licensing/LicenseValidationService.cs` |
| `IMediaStorageService.DeleteAsync(storageKey, ct)` | Defines provider-agnostic deletion for temporary media objects. | `Application/Interfaces/IMediaStorageService.cs` |
| `LocalMediaStorageService.DeleteAsync(storageKey, ct)` | Deletes stored local-file object by storage key. | `API/Services/LocalMediaStorageService.cs` |

### API � Phase 28 Stage 28.3 Slice 4

| Function Name | Purpose | Location |
| --- | --- | --- |
| `MediaStorageServiceCollectionExtensions.AddConfiguredMediaStorage(services, configuration)` | Registers storage provider implementation based on `MediaStorage:Provider` config, defaulting to local provider. | `API/Services/MediaStorageServiceCollectionExtensions.cs` |
| `BlobMediaStorageService.SaveAsync(content, category, fileExtension, ct)` | Persists objects using blob-style key semantics and configurable blob-root storage path. | `API/Services/BlobMediaStorageService.cs` |
| `BlobMediaStorageService.ReadAsBytesAsync(storageKey, ct)` | Reads stored object bytes by key from blob-style storage root. | `API/Services/BlobMediaStorageService.cs` |
| `BlobMediaStorageService.DeleteAsync(storageKey, ct)` | Deletes stored object by key from blob-style storage root. | `API/Services/BlobMediaStorageService.cs` |

### API � Program.cs Changes (Part A)

| Change | Purpose | Location |
| --- | --- | --- |
| DB retry on failure (`EnableRetryOnFailure(3, 30s, null)`) | Transient SQL Server failure recovery � retries up to 3 times with 30 s backoff. | `API/Program.cs` |
| CORS from config (`AppSettings:CorsOrigins`) | Reads allowed origins from configuration; registers `AllowConfiguredOrigins` policy with credentials support; skips registration when array is empty. | `API/Program.cs` |
| `ForwardedHeaders` middleware (non-dev) | Trusts `X-Forwarded-For` and `X-Forwarded-Proto` from reverse proxies (IIS/nginx/Cloudflare). | `API/Program.cs` |
| Health check at `/health` | `AddHealthChecks()` + `MapHealthChecks("/health")` for uptime monitoring. | `API/Program.cs` |
| Request body size limits (5 MB) | Configured on Kestrel, IIS, and FormOptions to reject over-limit requests. | `API/Program.cs` |
| Startup environment log | `Console.WriteLine` emits environment name + application name on startup. | `API/Program.cs` |
| Swagger gated by `AppSettings:EnableSwagger` | Swagger always on in Development; controlled by config flag in Production. | `API/Program.cs` |
| WeatherForecast boilerplate removed | Dead scaffold code removed from bottom of file. | `API/Program.cs` |

### Web � Program.cs Changes (Part B)

| Change | Purpose | Location |
| --- | --- | --- |
| Session cookie `SameSite=Strict` + `SecurePolicy=Always` | CSRF and HTTPS enforcement on the Web portal session cookie. | `Web/Program.cs` |

### Configuration Files Added/Updated (Part A)

| File | Purpose |
|---|---|
| `API/appsettings.Production.json` | Production placeholder config: Warning logging, production CORS origins, EnableSwagger=false, EnableDetailedErrors=false, Kestrel endpoint. |
| `Web/appsettings.Production.json` | Production placeholder config: Warning logging, AllowedHosts, production EduApi BaseUrl. |
| `BackgroundJobs/appsettings.Production.json` | Production placeholder config: Warning logging, production connection string placeholder. |
| `API/appsettings.Development.json` | Updated: Debug logging, CORS origins for localhost:5063, EnableSwagger=true, EnableDetailedErrors=true. |
| `API/appsettings.json` | Updated: Added `AppSettings` section (EnableSwagger, EnableDetailedErrors, CorsOrigins). |
| `BackgroundJobs/appsettings.Development.json` | Updated: Added dev connection string for localhost. |
| `EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct)` | Enforces admin assigned-department + offering ownership-by-department guards across reporting endpoints. | `API/Controllers/ReportController.cs` |

## Refactoring-Hosting-Security � Remaining Items (2026-05-07) | Commit 5e80bc9

### API � Program.cs (Serilog)

| Change | Purpose | Location |
| --- | --- | --- |
| `builder.Host.UseSerilog(...)` | Wires Serilog structured logging; env-aware min level (Debug dev / Warning prod); overrides Microsoft namespaces to Warning; console sink with timestamp; rolling daily file sink to `logs/app-.log` with 30-file retention. | `API/Program.cs` |

### API � PortalSettingsController (Part B wiring)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `PortalSettingsController.UploadLogo(file, ct)` | Replaced scattered manual size/ext/MIME checks with single `FileUploadValidator.ValidateImageAsync(file)` call; returns `BadRequest` with validator error message if invalid; continues to build data URI from valid image bytes. | `API/Controllers/PortalSettingsController.cs` |

### Web � PortalController (Part B wiring)

| Change | Purpose | Location |
| --- | --- | --- |
| `PortalController.SubmitAssignment` � inline file validation | Added guard before file is written to disk: size = 5 MB check + extension against allowlist (.pdf/.jpg/.jpeg/.png/.doc/.docx); redirects with `TempData["PortalMessage"]` on violation. | `Web/Controllers/PortalController.cs` |



### Infrastructure � ReportService (Stage 5.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ExportAttendanceSummaryCsvAsync(request, ct)` | Generates attendance summary CSV export bytes. | `Infrastructure/Reporting/ReportService.cs` |
| `ExportAttendanceSummaryPdfAsync(request, ct)` | Generates attendance summary PDF export bytes via QuestPDF. | `Infrastructure/Reporting/ReportService.cs` |
| `ExportResultSummaryCsvAsync(request, ct)` | Generates result summary CSV export bytes. | `Infrastructure/Reporting/ReportService.cs` |
| `ExportResultSummaryPdfAsync(request, ct)` | Generates result summary PDF export bytes via QuestPDF. | `Infrastructure/Reporting/ReportService.cs` |
| `ExportAssignmentSummaryCsvAsync(request, ct)` | Generates assignment summary CSV export bytes. | `Infrastructure/Reporting/ReportService.cs` |
| `ExportAssignmentSummaryPdfAsync(request, ct)` | Generates assignment summary PDF export bytes via QuestPDF. | `Infrastructure/Reporting/ReportService.cs` |
| `ExportQuizSummaryCsvAsync(request, ct)` | Generates quiz summary CSV export bytes. | `Infrastructure/Reporting/ReportService.cs` |
| `ExportQuizSummaryPdfAsync(request, ct)` | Generates quiz summary PDF export bytes via QuestPDF. | `Infrastructure/Reporting/ReportService.cs` |
| `BuildCsvBytes(headers, rows)` | Shared CSV builder for report exports with header + row serialization. | `Infrastructure/Reporting/ReportService.cs` |
| `EscapeCsvCell(value)` | Escapes CSV cells for commas, quotes, and line breaks. | `Infrastructure/Reporting/ReportService.cs` |
| `BuildPdfBytes(title, headers, rows)` | Shared PDF table builder for report exports. | `Infrastructure/Reporting/ReportService.cs` |

### API � ReportController (Stage 5.2 + 5.5)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ExportAttendanceSummaryCsv(...)` | Exposes attendance CSV export endpoint. | `API/Controllers/ReportController.cs` |
| `ExportAttendanceSummaryPdf(...)` | Exposes attendance PDF export endpoint. | `API/Controllers/ReportController.cs` |
| `ExportResultSummaryCsv(...)` | Exposes result CSV export endpoint. | `API/Controllers/ReportController.cs` |
| `ExportResultSummaryPdf(...)` | Exposes result PDF export endpoint. | `API/Controllers/ReportController.cs` |
| `ExportAssignmentSummaryCsv(...)` | Exposes assignment CSV export endpoint. | `API/Controllers/ReportController.cs` |
| `ExportAssignmentSummaryPdf(...)` | Exposes assignment PDF export endpoint. | `API/Controllers/ReportController.cs` |
| `ExportQuizSummaryCsv(...)` | Exposes quiz CSV export endpoint. | `API/Controllers/ReportController.cs` |
| `ExportQuizSummaryPdf(...)` | Exposes quiz PDF export endpoint. | `API/Controllers/ReportController.cs` |
| `EnforceFacultyOfferingScopeAsync(courseOfferingId, ct)` | Blocks faculty report access unless selected offering is owned by requesting faculty user. | `API/Controllers/ReportController.cs` |
| `GetCurrentUserId()` | Resolves current user id for offering ownership enforcement. | `API/Controllers/ReportController.cs` |

### API � DepartmentController / CourseController (Stage 5.5)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DepartmentController.GetAll(ct)` | Restricts faculty department list to assigned departments only. | `API/Controllers/DepartmentController.cs` |
| `CourseController.GetAll(departmentId, ct)` | Restricts faculty course list to assigned department scope. | `API/Controllers/CourseController.cs` |
| `CourseController.GetOfferings(semesterId, departmentId, ct)` | Restricts faculty offerings list to faculty-owned offerings within assigned departments. | `API/Controllers/CourseController.cs` |

### Web � PortalController / EduApiClient (Stage 5.2 + 5.5)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ReportAttendance(...)` (faculty guard) | Shows guidance when faculty runs report query without selecting offering. | `Web/Controllers/PortalController.cs` |
| `ReportResults(...)` (faculty guard) | Shows guidance when faculty runs report query without selecting offering. | `Web/Controllers/PortalController.cs` |
| `ReportAssignments(...)` (faculty guard) | Shows guidance when faculty runs report query without selecting offering. | `Web/Controllers/PortalController.cs` |
| `ReportQuizzes(...)` (faculty guard) | Shows guidance when faculty runs report query without selecting offering. | `Web/Controllers/PortalController.cs` |
| `ExportAttendanceSummaryCsvAsync(...)` | Calls attendance CSV API export endpoint. | `Web/Services/EduApiClient.cs` |
| `ExportAttendanceSummaryPdfAsync(...)` | Calls attendance PDF API export endpoint. | `Web/Services/EduApiClient.cs` |
| `ExportResultSummaryCsvAsync(...)` | Calls result CSV API export endpoint. | `Web/Services/EduApiClient.cs` |
| `ExportResultSummaryPdfAsync(...)` | Calls result PDF API export endpoint. | `Web/Services/EduApiClient.cs` |
| `ExportAssignmentSummaryCsvAsync(...)` | Calls assignment CSV API export endpoint. | `Web/Services/EduApiClient.cs` |
| `ExportAssignmentSummaryPdfAsync(...)` | Calls assignment PDF API export endpoint. | `Web/Services/EduApiClient.cs` |
| `ExportQuizSummaryCsvAsync(...)` | Calls quiz CSV API export endpoint. | `Web/Services/EduApiClient.cs` |
| `ExportQuizSummaryPdfAsync(...)` | Calls quiz PDF API export endpoint. | `Web/Services/EduApiClient.cs` |

## Issue-Fix Phase 2 � Shared Portal and Settings Issues (2026-05-06)

### API � PortalSettingsController

| Function Name | Purpose | Location |
| --- | --- | --- |

### API � Program Startup

| Function Name | Purpose | Location |
| --- | --- | --- |
| Static file middleware initialization | Added explicit static-file provider rooted at API `wwwroot` (with fallback path creation) so uploaded branding assets under `/portal-uploads/*` are publicly reachable. | `API/Program.cs` |

## Final-Touches Phase 1 Remediation � Batch 1 (2026-05-05)

### API � CourseController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetMyOfferings(ct)` | Expanded role support to SuperAdmin/Admin/Faculty/Student and added role-aware offering resolution so core academic pages can always load offering lists for SuperAdmin. | `API/Controllers/CourseController.cs` |
| `GetStudentProfileId()` | New helper to read `studentProfileId` claim when resolving student offerings. | `API/Controllers/CourseController.cs` |

### Infrastructure � ReportRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetCatalogForRoleAsync(roleName, ct)` | Added SuperAdmin bypass so all active reports are visible in Report Center even without explicit role assignment rows. | `Infrastructure/Reporting/ReportRepository.cs` |

### Web � EduApiClient

| Function Name | Purpose | Location |
| --- | --- | --- |
| `MapStudent(s)` | Fixed lifecycle student mapping to prefer `StudentProfileId` and semester fallback fields, preventing empty GUID promote requests. | `Web/Services/EduApiClient.cs` |

### Web � Shared Layout

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ResolveRoute(key)` | Removed `module_settings` route mapping from dynamic sidebar rendering. | `Web/Views/Shared/_Layout.cshtml` |
| `ResolveGroup(key)` | Removed `module_settings` group mapping from dynamic sidebar rendering. | `Web/Views/Shared/_Layout.cshtml` |
| Sidebar brand container | Converted header brand block from clickable link to non-clickable container (TE / Tabsan EduSphere / Campus Portal). | `Web/Views/Shared/_Layout.cshtml` |

## Final-Touches Phase 1 Remediation � Batch 2 (2026-05-05)

### Web � Shared Layout (Batch 2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| SuperAdmin system links block | Removed static `Module Settings` sidebar link from System Settings section. | `Web/Views/Shared/_Layout.cshtml` |

### Infrastructure � Database Seeder

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SeedAsync(services)` | Removed `module_settings` upsert path and added legacy cleanup that disables role access and soft-deletes historical `module_settings` sidebar row. | `Infrastructure/Persistence/DatabaseSeeder.cs` |

### SQL Seeds

| Function Name | Purpose | Location |
| --- | --- | --- |
| Sidebar seed block (`�15`) | Removed `module_settings` creation and role-assignment rows; added legacy cleanup SQL to hide existing `module_settings` records. | `Scripts/1-MinimalSeed.sql` |
| Sidebar seed block (`�15`) | Removed `module_settings` creation and role-assignment rows; added legacy cleanup SQL to hide existing `module_settings` records. | `Scripts/2-FullDummyData.sql` |

### Integration Tests

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetVisible_SuperAdmin_ReturnsAllMenus()` | Updated expected seeded menu count and removed `module_settings` key assertion after menu cleanup. | `tests/Tabsan.EduSphere.IntegrationTests/SidebarMenuIntegrationTests.cs` |

## Final-Touches Phase 1 Remediation � Batch 3 (2026-05-05)

### Infrastructure � Report Repository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetResultDataAsync(semesterId, courseOfferingId, studentProfileId, ct)` | Removed post-projection ordering to avoid EF translation failures on `ResultReportRow` projection. | `Infrastructure/Reporting/ReportRepository.cs` |
| `GetSemesterResultDataAsync(semesterId, departmentId, ct)` | Removed post-projection ordering to keep query fully translatable by EF Core. | `Infrastructure/Reporting/ReportRepository.cs` |
| `BuildResultQuery(semesterId, courseOfferingId, studentProfileId, departmentId)` | Added SQL-level `orderby u.Username, c.Code` before projection to resolve Result Summary InvalidOperationException. | `Infrastructure/Reporting/ReportRepository.cs` |

### API � Report Controller

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetResultSummary(...)` | Authorization expanded to `SuperAdmin,Admin,Faculty` so SuperAdmin can always access Result Summary data. | `API/Controllers/ReportController.cs` |
| `ExportResultSummary(...)` | Authorization expanded to `SuperAdmin,Admin,Faculty` for export parity with data endpoint. | `API/Controllers/ReportController.cs` |
| Report data/export endpoints | Attendance, GPA, Enrollment, Semester Results, Low Attendance, and FYP status endpoints now explicitly allow `SuperAdmin,Admin,Faculty`. | `API/Controllers/ReportController.cs` |

  ---

## Domain Layer

### `BaseEntity` — `src/Tabsan.EduSphere.Domain/Common/BaseEntity.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Touch()` | Updates the `UpdatedAt` timestamp to the current UTC time. Called by all domain mutation methods and by DbContext before SaveChanges. | `Domain/Common/BaseEntity.cs` |

  ---

### `AuditableEntity` — `src/Tabsan.EduSphere.Domain/Common/AuditableEntity.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `SoftDelete()` | Marks the entity as deleted (`IsDeleted = true`, sets `DeletedAt`) without physically removing the database row. | `Domain/Common/AuditableEntity.cs` |
| `Restore()` | Reverses a soft delete — clears `IsDeleted` and `DeletedAt`. | `Domain/Common/AuditableEntity.cs` |

  ---

### `User` — `src/Tabsan.EduSphere.Domain/Identity/User.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `RecordLogin()` | Sets `LastLoginAt` to the current UTC time after a successful authentication. | `Domain/Identity/User.cs` |
| `UpdatePasswordHash(newHash)` | Replaces the stored password hash when the user changes their password. | `Domain/Identity/User.cs` |
| `Deactivate()` | Sets `IsActive = false` to prevent the user from logging in without deleting the account. | `Domain/Identity/User.cs` |
| `Activate()` | Re-enables a previously deactivated user account. | `Domain/Identity/User.cs` |
| `UpdateEmail(email)` | Updates the user's email address with basic format validation. | `Domain/Identity/User.cs` |

  ---

### `UserSession` — `src/Tabsan.EduSphere.Domain/Identity/UserSession.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `IsActive` (computed property) | Returns true when the session has not been revoked and the expiry is in the future. | `Domain/Identity/UserSession.cs` |
| `Revoke()` | Stamps `RevokedAt` with the current UTC time, invalidating the session for all future refresh attempts. | `Domain/Identity/UserSession.cs` |
| `Rotate(newHash, newExpiry)` | Replaces the refresh token hash and extends the expiry — used during token rotation on refresh. | `Domain/Identity/UserSession.cs` |

  ---

### `Department` — `src/Tabsan.EduSphere.Domain/Academic/Department.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Rename(newName)` | Updates the display name of the department. | `Domain/Academic/Department.cs` |

  ---

### `LicenseState` — `src/Tabsan.EduSphere.Domain/Licensing/LicenseState.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `RefreshStatus()` | Re-evaluates license validity against current UTC time and updates `Status` to Active or Expired. | `Domain/Licensing/LicenseState.cs` |
| `MarkInvalid()` | Forces the status to `Invalid` when the signature check fails. | `Domain/Licensing/LicenseState.cs` |
| `Replace(newHash, newType, newExpiry)` | Replaces the current license record with data from a newly uploaded and validated license file. | `Domain/Licensing/LicenseState.cs` |

  ---

### `ModuleStatus` — `src/Tabsan.EduSphere.Domain/Modules/ModuleStatus.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Activate(changedBy, source)` | Activates the module and records who changed it and when. | `Domain/Modules/ModuleStatus.cs` |
| `Deactivate(changedBy)` | Deactivates the module, preserving data but blocking UI and API access. | `Domain/Modules/ModuleStatus.cs` |

  ---

### `AuditLog` — `src/Tabsan.EduSphere.Domain/Auditing/AuditLog.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `AuditLog(action, entityName, ...)` | Constructor — creates an immutable audit record for a privileged action. All audit writes use this constructor to ensure no field is omitted. | `Domain/Auditing/AuditLog.cs` |

  ---

## Infrastructure Layer

### `ApplicationDbContext` — `src/Tabsan.EduSphere.Infrastructure/Persistence/ApplicationDbContext.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `OnModelCreating(modelBuilder)` | Discovers and applies all `IEntityTypeConfiguration` implementations in the assembly automatically. | `Infrastructure/Persistence/ApplicationDbContext.cs` |
| `SaveChangesAsync(cancellationToken)` | Overrides EF Core save to call `Touch()` on all modified entities before writing to the database. | `Infrastructure/Persistence/ApplicationDbContext.cs` |
| `SetAuditTimestamps()` | Iterates all tracked `BaseEntity` entries in Modified state and calls `Touch()`. Called by `SaveChangesAsync`. | `Infrastructure/Persistence/ApplicationDbContext.cs` |

  ---

### `UserRepository` — `src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetByIdAsync(id, ct)` | Returns a user by GUID PK with Role loaded, or null. | `Infrastructure/Repositories/UserRepository.cs` |
| `GetByUsernameAsync(username, ct)` | Returns a user by username (case-insensitive) with Role loaded, or null. | `Infrastructure/Repositories/UserRepository.cs` |
| `GetByEmailAsync(email, ct)` | Returns a user by email address, or null. | `Infrastructure/Repositories/UserRepository.cs` |
| `UsernameExistsAsync(username, ct)` | Returns true when the username is already taken. | `Infrastructure/Repositories/UserRepository.cs` |
| `AddAsync(user, ct)` | Queues a new user entity for insertion. | `Infrastructure/Repositories/UserRepository.cs` |
| `Update(user)` | Marks the user entity as Modified for EF change tracking. | `Infrastructure/Repositories/UserRepository.cs` |

  ---

### `LicenseRepository` — `src/Tabsan.EduSphere.Infrastructure/Repositories/LicenseRepository.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetCurrentAsync(ct)` | Returns the most recently activated license row, or null. | `Infrastructure/Repositories/LicenseRepository.cs` |
| `AddAsync(state, ct)` | Queues a new LicenseState record for insertion. | `Infrastructure/Repositories/LicenseRepository.cs` |
| `Update(state)` | Marks the existing LicenseState as modified. | `Infrastructure/Repositories/LicenseRepository.cs` |

  ---

### `ModuleRepository` — `src/Tabsan.EduSphere.Infrastructure/Repositories/ModuleRepository.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetAllWithStatusAsync(ct)` | Returns all module definitions. | `Infrastructure/Repositories/ModuleRepository.cs` |
| `GetStatusByKeyAsync(moduleKey, ct)` | Returns the ModuleStatus row for the given module key, or null. | `Infrastructure/Repositories/ModuleRepository.cs` |
| `IsActiveAsync(moduleKey, ct)` | Returns true when the named module is active (lightweight query, no nav props). | `Infrastructure/Repositories/ModuleRepository.cs` |
| `UpdateStatus(status)` | Marks a ModuleStatus entity as Modified. | `Infrastructure/Repositories/ModuleRepository.cs` |

  ---

### `UserSessionRepository` — `src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetActiveByHashAsync(tokenHash, ct)` | Returns the non-revoked session matching the token hash, or null. | `Infrastructure/Repositories/UserSessionRepository.cs` |
| `AddAsync(session, ct)` | Queues a new UserSession for insertion. | `Infrastructure/Repositories/UserSessionRepository.cs` |
| `Update(session)` | Marks the session as Modified. | `Infrastructure/Repositories/UserSessionRepository.cs` |

  ---

### `TokenService` — `src/Tabsan.EduSphere.Infrastructure/Auth/TokenService.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GenerateAccessToken(user)` | Builds and signs a JWT access token with user ID, username, role, and department ID claims. | `Infrastructure/Auth/TokenService.cs` |
| `GenerateRefreshToken()` | Generates a cryptographically random 64-byte Base64 refresh token. | `Infrastructure/Auth/TokenService.cs` |
| `HashRefreshToken(rawToken)` | Computes the SHA-256 hex hash of a raw refresh token for safe storage. | `Infrastructure/Auth/TokenService.cs` |
| `GetRefreshTokenExpiry()` | Returns the UTC expiry DateTime for a new refresh token based on configured days. | `Infrastructure/Auth/TokenService.cs` |

  ---

### `PasswordHasher` — `src/Tabsan.EduSphere.Infrastructure/Auth/PasswordHasher.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Hash(password)` | Produces a PBKDF2-HMACSHA512 hash of the plain-text password for storage. | `Infrastructure/Auth/PasswordHasher.cs` |
| `Verify(storedHash, providedPassword)` | Returns true when the plain-text password matches the stored hash. | `Infrastructure/Auth/PasswordHasher.cs` |

  ---

### `LicenseValidationService` — `src/Tabsan.EduSphere.Infrastructure/Licensing/LicenseValidationService.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `ActivateFromFileAsync(filePath, ct)` | Reads, deserialises, and signature-verifies a license file; creates or replaces the LicenseState record. Returns true on success. | `Infrastructure/Licensing/LicenseValidationService.cs` |
| `ValidateCurrentAsync(ct)` | Refreshes the stored license status against the current time. Used at startup, on Super Admin login, and by the daily background job. | `Infrastructure/Licensing/LicenseValidationService.cs` |
| `VerifySignature(payload)` | Reconstructs the canonical signed string and verifies the RSA-SHA256 signature using the embedded public key. | `Infrastructure/Licensing/LicenseValidationService.cs` |
| `ComputeFileHash(bytes)` | Computes the SHA-256 hex hash of the raw license file bytes for change detection. | `Infrastructure/Licensing/LicenseValidationService.cs` |

  ---

### `ModuleEntitlementResolver` — `src/Tabsan.EduSphere.Infrastructure/Modules/ModuleEntitlementResolver.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `InvalidateCache(moduleKey)` | Removes the cache entry for a single module after a Super Admin toggle. | `Infrastructure/Modules/ModuleEntitlementResolver.cs` |
| `InvalidateAll()` | Clears all module entitlement cache entries after bulk changes or a license update. | `Infrastructure/Modules/ModuleEntitlementResolver.cs` |

  ---

### `AuditService` — `src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `LogAsync(entry, ct)` | Appends a new audit log entry to the database asynchronously. | `Infrastructure/Auditing/AuditService.cs` |

  ---

### `DatabaseSeeder` — `src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `SeedRolesAsync(db)` | Inserts the four system roles (SuperAdmin, Admin, Faculty, Student) if they do not already exist. | `Infrastructure/Persistence/DatabaseSeeder.cs` |
| `SeedModulesAsync(db)` | Inserts all known module definitions and creates a default ModuleStatus row for each. | `Infrastructure/Persistence/DatabaseSeeder.cs` |
| `SeedSuperAdminAsync(db, hasher)` | Creates the bootstrap Super Admin account from environment variables if no SuperAdmin user exists yet. | `Infrastructure/Persistence/DatabaseSeeder.cs` |

  ---

## Application Layer

### `AuthService` — `src/Tabsan.EduSphere.Application/Auth/AuthService.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `LoginAsync(request, ipAddress, ct)` | Validates credentials, creates a UserSession, and returns JWT + refresh token. | `Application/Auth/AuthService.cs` |
| `RefreshAsync(rawRefreshToken, ipAddress, ct)` | Rotates the refresh token and returns a new token pair. | `Application/Auth/AuthService.cs` |
| `LogoutAsync(rawRefreshToken, ct)` | Revokes the session associated with the refresh token. | `Application/Auth/AuthService.cs` |
| `ChangePasswordAsync(userId, request, ct)` | Verifies current password and replaces it with the new hash. | `Application/Auth/AuthService.cs` |

  ---

### `ModuleService` — `src/Tabsan.EduSphere.Application/Modules/ModuleService.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetAllAsync(ct)` | Returns all module definitions with their current activation status. | `Application/Modules/ModuleService.cs` |
| `ActivateAsync(moduleKey, changedByUserId, ct)` | Activates the named module, clears the entitlement cache, and writes an audit log. | `Application/Modules/ModuleService.cs` |
| `DeactivateAsync(moduleKey, changedByUserId, ct)` | Deactivates the named module (throws if mandatory), clears cache, writes audit log. | `Application/Modules/ModuleService.cs` |

  ---

## API Layer

### `AuthController` — `src/Tabsan.EduSphere.API/Controllers/AuthController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Login(request, ct)` | `POST /api/v1/auth/login` — authenticates the user and returns tokens. | `API/Controllers/AuthController.cs` |
| `Refresh(request, ct)` | `POST /api/v1/auth/refresh` — rotates the refresh token and returns a new pair. | `API/Controllers/AuthController.cs` |
| `Logout(request, ct)` | `POST /api/v1/auth/logout` — revokes the session. | `API/Controllers/AuthController.cs` |
| `ChangePassword(request, ct)` | `PUT /api/v1/auth/change-password` — changes the authenticated user's password. | `API/Controllers/AuthController.cs` |

  ---

### `ModuleController` — `src/Tabsan.EduSphere.API/Controllers/ModuleController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Activate(key, ct)` | `POST /api/v1/modules/{key}/activate` — activates the named module. Requires SuperAdmin. | `API/Controllers/ModuleController.cs` |
| `Deactivate(key, ct)` | `POST /api/v1/modules/{key}/deactivate` — deactivates the named module. Requires SuperAdmin. | `API/Controllers/ModuleController.cs` |
| `Status(key, ct)` | `GET /api/v1/modules/{key}/status` — returns the current active/inactive state from cache. | `API/Controllers/ModuleController.cs` |
| `GetUserId()` | Private helper — extracts the authenticated user's GUID from the JWT sub claim. | `API/Controllers/ModuleController.cs` |

  ---

### `LicenseController` — `src/Tabsan.EduSphere.API/Controllers/LicenseController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Upload(file, ct)` | `POST /api/v1/license/upload` — saves and validates a new license file. Requires SuperAdmin. | `API/Controllers/LicenseController.cs` |
| `Status(ct)` | `GET /api/v1/license/status` — runs an on-demand license check and returns current status. Requires SuperAdmin. | `API/Controllers/LicenseController.cs` |

  ---

## Background Jobs

### `LicenseCheckWorker` — `src/Tabsan.EduSphere.BackgroundJobs/LicenseCheckWorker.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `ExecuteAsync(stoppingToken)` | Main hosted-service loop — waits 30 s on startup, then calls `RunCheckAsync` every 24 hours. | `BackgroundJobs/LicenseCheckWorker.cs` |
| `RunCheckAsync(ct)` | Opens a fresh DI scope, resolves `LicenseValidationService`, and runs a validation check. Exceptions are caught and logged. | `BackgroundJobs/LicenseCheckWorker.cs` |

  ---

## Phase 2 — Academic Core

### `AcademicProgram` — `src/Tabsan.EduSphere.Domain/Academic/AcademicProgram.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `AcademicProgram(name, code, departmentId, totalSemesters)` | Constructor — creates a new degree programme; normalises code to uppercase. | `Domain/Academic/AcademicProgram.cs` |

  ---

### `Semester` — `src/Tabsan.EduSphere.Domain/Academic/Semester.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Semester(name, startDate, endDate)` | Constructor — creates a new open semester term. | `Domain/Academic/Semester.cs` |
| `Close()` | Permanently closes the semester. One-way: throws if already closed. | `Domain/Academic/Semester.cs` |

  ---

### `Course` — `src/Tabsan.EduSphere.Domain/Academic/Course.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Course(title, code, creditHours, departmentId)` | Constructor — creates a new course catalogue entry; normalises code to uppercase. | `Domain/Academic/Course.cs` |
| `UpdateTitle(newTitle)` | Updates the course display title. | `Domain/Academic/Course.cs` |

  ---

### `CourseOffering` — `src/Tabsan.EduSphere.Domain/Academic/CourseOffering.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `CourseOffering(courseId, semesterId, maxEnrollment, facultyUserId?)` | Constructor — schedules a course for a semester. | `Domain/Academic/CourseOffering.cs` |
| `AssignFaculty(facultyUserId)` | Assigns or re-assigns a faculty member to this offering. | `Domain/Academic/CourseOffering.cs` |
| `Reopen()` | Re-opens the offering to accept enrollments again. | `Domain/Academic/CourseOffering.cs` |
| `UpdateMaxEnrollment(max)` | Changes the maximum enrollment capacity. | `Domain/Academic/CourseOffering.cs` |

  ---

### `StudentProfile` — `src/Tabsan.EduSphere.Domain/Academic/StudentProfile.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `StudentProfile(userId, registrationNumber, programId, departmentId, admissionDate)` | Constructor — creates a student's academic profile. | `Domain/Academic/StudentProfile.cs` |
| `UpdateCgpa(newCgpa)` | Updates the cumulative GPA after result publication (0.0–4.0 range enforced). | `Domain/Academic/StudentProfile.cs` |
| `AdvanceSemester()` | Increments the student's current semester number. | `Domain/Academic/StudentProfile.cs` |

  ---

### `Enrollment` — `src/Tabsan.EduSphere.Domain/Academic/Enrollment.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Enrollment(studentProfileId, courseOfferingId)` | Constructor — records a new active enrollment. | `Domain/Academic/Enrollment.cs` |
| `Drop()` | Changes status to Dropped and sets DroppedAt. Throws if not Active. | `Domain/Academic/Enrollment.cs` |
| `Cancel()` | Changes status to Cancelled (used when the offering itself is cancelled). | `Domain/Academic/Enrollment.cs` |

  ---

### `RegistrationWhitelist` — `src/Tabsan.EduSphere.Domain/Academic/RegistrationWhitelist.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `RegistrationWhitelist(identifierType, identifierValue, departmentId, programId)` | Constructor — creates a pre-approved registration entry; normalises identifier to lowercase. | `Domain/Academic/RegistrationWhitelist.cs` |
| `MarkUsed(createdUserId)` | Marks the entry as consumed after a successful self-registration. Throws if already used. | `Domain/Academic/RegistrationWhitelist.cs` |

  ---

### `FacultyDepartmentAssignment` — `src/Tabsan.EduSphere.Domain/Academic/FacultyDepartmentAssignment.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `FacultyDepartmentAssignment(facultyUserId, departmentId)` | Constructor — creates an active assignment linking a faculty member to a department. | `Domain/Academic/FacultyDepartmentAssignment.cs` |

  ---

### `DepartmentRepository` — `src/Tabsan.EduSphere.Infrastructure/Repositories/DepartmentRepository.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `CodeExistsAsync(code, ct)` | Returns true when the uppercase code is already in use. | `Infrastructure/Repositories/DepartmentRepository.cs` |
| `AddAsync(department, ct)` | Queues a new department for insertion. | `Infrastructure/Repositories/DepartmentRepository.cs` |
| `Update(department)` | Marks the department as modified. | `Infrastructure/Repositories/DepartmentRepository.cs` |

  ---

### `AcademicProgramRepository` + `SemesterRepository` — `src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicRepositories.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `AcademicProgramRepository.GetAllAsync(departmentId?, ct)` | Returns all programmes, optionally scoped to a department, with Department loaded. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `AcademicProgramRepository.GetByIdAsync(id, ct)` | Returns the programme by ID with Department loaded, or null. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `AcademicProgramRepository.CodeExistsAsync(code, ct)` | Returns true when the uppercase code is already taken. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `AcademicProgramRepository.AddAsync(program, ct)` | Queues the programme for insertion. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `AcademicProgramRepository.Update(program)` | Marks the programme as modified. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `AcademicProgramRepository.SaveChangesAsync(ct)` | Commits pending changes. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `SemesterRepository.GetAllAsync(ct)` | Returns all semesters ordered by start date descending. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `SemesterRepository.GetByIdAsync(id, ct)` | Returns the semester by ID, or null. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `SemesterRepository.GetCurrentOpenAsync(ct)` | Returns the most recent open semester, or null. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `SemesterRepository.AddAsync(semester, ct)` | Queues the semester for insertion. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `SemesterRepository.Update(semester)` | Marks the semester as modified. | `Infrastructure/Repositories/AcademicRepositories.cs` |
| `SemesterRepository.SaveChangesAsync(ct)` | Commits pending changes. | `Infrastructure/Repositories/AcademicRepositories.cs` |

  ---

### `CourseRepository` — `src/Tabsan.EduSphere.Infrastructure/Repositories/CourseRepository.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetAllAsync(departmentId?, ct)` | Returns all courses filtered by department if provided, ordered by code. | `Infrastructure/Repositories/CourseRepository.cs` |
| `CodeExistsAsync(code, departmentId, ct)` | Returns true when the code+department combination already exists. | `Infrastructure/Repositories/CourseRepository.cs` |
| `AddAsync(course, ct)` | Queues the course for insertion. | `Infrastructure/Repositories/CourseRepository.cs` |
| `Update(course)` | Marks the course as modified. | `Infrastructure/Repositories/CourseRepository.cs` |
| `GetOfferingsBySemesterAsync(semesterId, ct)` | Returns all offerings for a semester with Course and Semester loaded. | `Infrastructure/Repositories/CourseRepository.cs` |
| `GetOfferingsByDepartmentAsync(departmentId, ct)` | Returns all offerings for a department (filtered by course.departmentId) with Course and Semester loaded. | `Infrastructure/Repositories/CourseRepository.cs` |
| `GetOfferingsByFacultyAsync(facultyUserId, ct)` | Returns all offerings assigned to the faculty user. | `Infrastructure/Repositories/CourseRepository.cs` |
| `GetOfferingByIdAsync(offeringId, ct)` | Returns an offering by ID with navigations loaded, or null. | `Infrastructure/Repositories/CourseRepository.cs` |
| `GetEnrollmentCountAsync(offeringId, ct)` | Returns the count of active enrollments for the offering. | `Infrastructure/Repositories/CourseRepository.cs` |
| `AddOfferingAsync(offering, ct)` | Queues the offering for insertion. | `Infrastructure/Repositories/CourseRepository.cs` |
| `UpdateOffering(offering)` | Marks the offering as modified. | `Infrastructure/Repositories/CourseRepository.cs` |

  ---

### Support Repositories — `src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicSupportRepositories.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `EnrollmentRepository.GetByStudentAsync(studentProfileId, ct)` | Returns all enrollment records for the student with course/semester details loaded. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `EnrollmentRepository.GetByOfferingAsync(courseOfferingId, ct)` | Returns active enrollments for an offering with student profile loaded. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `EnrollmentRepository.GetAsync(studentProfileId, courseOfferingId, ct)` | Returns the enrollment for the given pair, or null. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `EnrollmentRepository.IsEnrolledAsync(studentProfileId, courseOfferingId, ct)` | Returns true when an active enrollment already exists. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `EnrollmentRepository.AddAsync(enrollment, ct)` | Queues a new enrollment for insertion. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `EnrollmentRepository.Update(enrollment)` | Marks the enrollment as modified (status change). | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `EnrollmentRepository.SaveChangesAsync(ct)` | Commits pending changes. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.GetByUserIdAsync(userId, ct)` | Returns the profile linked to the User ID with Program/Department loaded. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.GetByIdAsync(id, ct)` | Returns the profile by ID, or null. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.GetByRegistrationNumberAsync(regNo, ct)` | Returns the profile matching the registration number. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.GetAllAsync(departmentId?, ct)` | Returns all student profiles, optionally scoped to a department. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.RegistrationNumberExistsAsync(regNo, ct)` | Returns true when the registration number is already in use. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.AddAsync(profile, ct)` | Queues the profile for insertion. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.Update(profile)` | Marks the profile as modified. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `StudentProfileRepository.SaveChangesAsync(ct)` | Commits pending changes. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `RegistrationWhitelistRepository.FindUnusedAsync(identifierValue, ct)` | Returns an unused whitelist entry by identifier value (case-insensitive), or null. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `RegistrationWhitelistRepository.AddAsync(entry, ct)` | Queues a new whitelist entry for insertion. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `RegistrationWhitelistRepository.AddRangeAsync(entries, ct)` | Bulk-queues multiple whitelist entries for insertion. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `RegistrationWhitelistRepository.Update(entry)` | Marks the entry as modified. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `RegistrationWhitelistRepository.SaveChangesAsync(ct)` | Commits pending changes. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `FacultyAssignmentRepository.GetByFacultyAsync(facultyUserId, ct)` | Returns active assignments for the faculty with Department loaded. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `FacultyAssignmentRepository.GetByDepartmentAsync(departmentId, ct)` | Returns active faculty assignments for the department. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `FacultyAssignmentRepository.GetAsync(facultyUserId, departmentId, ct)` | Returns the active assignment for the pair, or null. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `FacultyAssignmentRepository.GetDepartmentIdsForFacultyAsync(facultyUserId, ct)` | Returns the list of department IDs the faculty is actively assigned to. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `FacultyAssignmentRepository.AddAsync(assignment, ct)` | Queues the assignment for insertion. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `FacultyAssignmentRepository.Update(assignment)` | Marks the assignment as modified. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |
| `FacultyAssignmentRepository.SaveChangesAsync(ct)` | Commits pending changes. | `Infrastructure/Repositories/AcademicSupportRepositories.cs` |

  ---

### `EnrollmentService` — `src/Tabsan.EduSphere.Application/Academic/EnrollmentService.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `EnrollAsync(studentProfileId, request, ct)` | Validates offering state, checks duplicates and seat availability, creates an Enrollment row, and returns a response DTO. Returns null on rejection. | `Application/Academic/EnrollmentService.cs` |
| `DropAsync(studentProfileId, courseOfferingId, ct)` | Changes an active enrollment's status to Dropped. Returns false when no active enrollment exists. | `Application/Academic/EnrollmentService.cs` |
| `GetForStudentAsync(studentProfileId, ct)` | Returns all enrollment records for the student (full history). | `Application/Academic/EnrollmentService.cs` |
| `GetForOfferingAsync(courseOfferingId, ct)` | Returns active enrollments for the given offering (faculty roster). | `Application/Academic/EnrollmentService.cs` |

  ---

### `StudentRegistrationService` — `src/Tabsan.EduSphere.Application/Academic/StudentRegistrationService.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `SelfRegisterAsync(request, ct)` | Whitelist-gated self-registration: validates identifier, creates User + StudentProfile atomically, marks whitelist entry consumed. Returns new User ID or null. | `Application/Academic/StudentRegistrationService.cs` |
| `CreateProfileAsync(request, ct)` | Admin-managed profile creation for an existing User — bypasses the whitelist gate. Throws on duplicate registration number. | `Application/Academic/StudentRegistrationService.cs` |

  ---

### `DepartmentController` — `src/Tabsan.EduSphere.API/Controllers/DepartmentController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetById(id, ct)` | `GET /api/v1/department/{id}` — returns a single department. | `API/Controllers/DepartmentController.cs` |
| `Deactivate(id, ct)` | `DELETE /api/v1/department/{id}` — soft-deactivates the department. SuperAdmin only. | `API/Controllers/DepartmentController.cs` |

  ---

### `ProgramController` — `src/Tabsan.EduSphere.API/Controllers/ProgramController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetAll(departmentId?, ct)` | `GET /api/v1/program` — returns programmes, optionally filtered by department. | `API/Controllers/ProgramController.cs` |

  ---

### `SemesterController` — `src/Tabsan.EduSphere.API/Controllers/SemesterController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `GetCurrent(ct)` | `GET /api/v1/semester/current` — returns the current open semester. | `API/Controllers/SemesterController.cs` |
| `Close(id, ct)` | `POST /api/v1/semester/{id}/close` — permanently closes the semester. Admin+. One-way operation. | `API/Controllers/SemesterController.cs` |

  ---

### `CourseController` — `src/Tabsan.EduSphere.API/Controllers/CourseController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `UpdateTitle(id, request, ct)` | `PUT /api/v1/course/{id}/title` — updates the course title. Admin+. | `API/Controllers/CourseController.cs` |
| `GetOfferings(semesterId, ct)` | `GET /api/v1/course/offerings?semesterId=` — returns offerings for a semester. | `API/Controllers/CourseController.cs` |
| `CreateOffering(request, ct)` | `POST /api/v1/course/offerings` — creates a course offering. Admin+. | `API/Controllers/CourseController.cs` |
| `AssignFaculty(id, request, ct)` | `PUT /api/v1/course/offerings/{id}/faculty` — assigns faculty to an offering. Admin+. | `API/Controllers/CourseController.cs` |

  ---

### `EnrollmentController` — `src/Tabsan.EduSphere.API/Controllers/EnrollmentController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `Enroll(request, ct)` | `POST /api/v1/enrollment` — enrolls the calling student into a course offering. Student role. | `API/Controllers/EnrollmentController.cs` |
| `Drop(offeringId, ct)` | `DELETE /api/v1/enrollment/{offeringId}` — drops the student's active enrollment. Student role. | `API/Controllers/EnrollmentController.cs` |
| `MyCourses(ct)` | `GET /api/v1/enrollment/my-courses` — returns the student's full enrollment history. Student role. | `API/Controllers/EnrollmentController.cs` |
| `GetRoster(offeringId, ct)` | `GET /api/v1/enrollment/roster/{offeringId}` — returns active enrollments for an offering. Faculty/Admin+. | `API/Controllers/EnrollmentController.cs` |

  ---

### `StudentController` — `src/Tabsan.EduSphere.API/Controllers/StudentController.cs`

| Name | Purpose | Location |
|------|---------|----------|
| `SelfRegister(request, ct)` | `POST /api/v1/student/register` — public whitelist-gated self-registration. AllowAnonymous. | `API/Controllers/StudentController.cs` |
| `GetMyProfile(ct)` | `GET /api/v1/student/profile` — returns the calling student's academic profile. Student role. | `API/Controllers/StudentController.cs` |
| `AddWhitelistEntry(request, ct)` | `POST /api/v1/student/whitelist` — adds a single registration whitelist entry. Admin+. | `API/Controllers/StudentController.cs` |
| `BulkAddWhitelistEntries(requests, ct)` | `POST /api/v1/student/whitelist/bulk` — bulk-imports whitelist entries. Admin+. | `API/Controllers/StudentController.cs` |

  ---

## Phase 3 � Assignments and Results

### Domain � Assignment

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Assignment(courseOfferingId, title, description, dueDate, maxMarks)` | Constructor � creates an unpublished assignment. | `Domain/Assignments/Assignment.cs` |
| `Publish()` | Marks the assignment as published (visible to students). Throws if already published. | `Domain/Assignments/Assignment.cs` |
| `Retract()` | Withdraws a published assignment. Throws if not published. | `Domain/Assignments/Assignment.cs` |
| `Update(title, description, dueDate, maxMarks)` | Updates editable fields. Throws if already published. | `Domain/Assignments/Assignment.cs` |

### Domain � AssignmentSubmission

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AssignmentSubmission(assignmentId, studentProfileId, fileUrl, textContent)` | Constructor � requires at least one of fileUrl/textContent. | `Domain/Assignments/AssignmentSubmission.cs` |
| `Grade(marksAwarded, feedback, gradedByUserId)` | Records marks and feedback. Throws if submission was Rejected. | `Domain/Assignments/AssignmentSubmission.cs` |
| `Reject()` | Marks submission as Rejected and clears awarded marks. | `Domain/Assignments/AssignmentSubmission.cs` |

### Domain � Result

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Result(studentProfileId, courseOfferingId, resultType, marksObtained, maxMarks)` | Constructor � validates marks range. | `Domain/Assignments/Result.cs` |
| `Publish(publishedByUserId)` | One-way publication. Throws if already published. | `Domain/Assignments/Result.cs` |
| `CorrectMarks(newMarksObtained, newMaxMarks)` | Admin-only correction of a published result. Validates range. | `Domain/Assignments/Result.cs` |

### Domain � TranscriptExportLog

| Function Name | Purpose | Location |
| --- | --- | --- |
| `TranscriptExportLog(studentProfileId, requestedByUserId, format, documentUrl?, ipAddress?)` | Constructor � append-only, immutable after creation. | `Domain/Assignments/TranscriptExportLog.cs` |

### Infrastructure � AssignmentRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetByOfferingAsync(courseOfferingId, ct)` | Returns non-deleted assignments for the offering, ordered by due date. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `TitleExistsAsync(courseOfferingId, title, ct)` | Returns true when the offering already has an assignment with that title. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `GetSubmissionAsync(assignmentId, studentProfileId, ct)` | Returns the submission for a student+assignment pair or null. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `GetSubmissionsByAssignmentAsync(assignmentId, ct)` | Returns all submissions for an assignment, ordered by submission date. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `GetSubmissionsByStudentAsync(studentProfileId, ct)` | Returns all submissions by a student with assignment navigation loaded. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `HasSubmittedAsync(assignmentId, studentProfileId, ct)` | Returns true when the student has already submitted. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `GetSubmissionCountAsync(assignmentId, ct)` | Returns the total submission count for an assignment. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `AddSubmissionAsync(submission, ct)` | Queues submission for insertion. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `UpdateSubmission(submission)` | Marks submission as modified. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |

### Infrastructure � ResultRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAsync(studentProfileId, courseOfferingId, resultType, ct)` | Returns the specific result row for the combination or null. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `GetByStudentAsync(studentProfileId, ct)` | Returns all results for a student (draft + published). | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `GetPublishedByStudentAsync(studentProfileId, ct)` | Returns only published results for a student. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `ExistsAsync(studentProfileId, courseOfferingId, resultType, ct)` | Returns true when a result row already exists for the combination. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `AddAsync(result, ct)` | Queues a result for insertion. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `AddRangeAsync(results, ct)` | Queues multiple results for bulk insertion. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `Update(result)` | Marks a result as modified. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `GetExportLogsAsync(studentProfileId, ct)` | Returns all export logs for a student, newest first. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |
| `AddExportLogAsync(log, ct)` | Queues a transcript export log for insertion. | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |

### Application � AssignmentService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CreateAsync(request, createdByUserId, ct)` | Creates an unpublished assignment and logs the action. | `Application/Assignments/AssignmentService.cs` |
| `UpdateAsync(assignmentId, request, ct)` | Updates draft assignment fields. Returns false if published. | `Application/Assignments/AssignmentService.cs` |
| `PublishAsync(assignmentId, ct)` | Publishes an assignment so students can submit. | `Application/Assignments/AssignmentService.cs` |
| `RetractAsync(assignmentId, ct)` | Retracts a published assignment. Fails if submissions exist. | `Application/Assignments/AssignmentService.cs` |
| `DeleteAsync(assignmentId, ct)` | Soft-deletes an assignment. Fails if submissions exist. | `Application/Assignments/AssignmentService.cs` |
| `GetByIdAsync(assignmentId, ct)` | Returns a single assignment with submission count, or null. | `Application/Assignments/AssignmentService.cs` |
| `SubmitAsync(studentProfileId, request, ct)` | Submits student work; enforces published, not past due, no duplicate. | `Application/Assignments/AssignmentService.cs` |
| `GetMySubmissionsAsync(studentProfileId, ct)` | Returns all submissions by the student with assignment titles. | `Application/Assignments/AssignmentService.cs` |
| `GradeSubmissionAsync(request, gradedByUserId, ct)` | Grades a submission; validates marks <= MaxMarks. | `Application/Assignments/AssignmentService.cs` |
| `RejectSubmissionAsync(assignmentId, studentProfileId, ct)` | Rejects a submission. Returns false if not found. | `Application/Assignments/AssignmentService.cs` |
| `ToResponse(assignment, submissionCount)` | Private � maps Assignment to AssignmentResponse DTO. | `Application/Assignments/AssignmentService.cs` |
| `ToSubmissionResponse(submission, assignmentTitle)` | Private � maps AssignmentSubmission to SubmissionResponse DTO. | `Application/Assignments/AssignmentService.cs` |

### Application � ResultService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CreateAsync(request, ct)` | Creates a draft result entry. Throws on duplicate. | `Application/Assignments/ResultService.cs` |
| `BulkCreateAsync(request, ct)` | Bulk-inserts draft results; skips existing. Returns inserted count. | `Application/Assignments/ResultService.cs` |
| `CorrectAsync(studentProfileId, courseOfferingId, resultType, request, correctedByUserId, ct)` | Admin correction of a published result with audit logging. | `Application/Assignments/ResultService.cs` |
| `ExportTranscriptAsync(request, requestedByUserId, ipAddress, ct)` | Exports transcript, logs to TranscriptExportLog and AuditLog. | `Application/Assignments/ResultService.cs` |
| `GetExportHistoryAsync(studentProfileId, ct)` | Returns transcript export history for a student. | `Application/Assignments/ResultService.cs` |
| `ToResponse(result)` | Private � maps Result to ResultResponse DTO including percentage. | `Application/Assignments/ResultService.cs` |

### API � AssignmentController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Publish(id, ct)` | `POST /api/assignment/{id}/publish` � publishes an assignment. Faculty/Admin. | `API/Controllers/AssignmentController.cs` |
| `Retract(id, ct)` | `POST /api/assignment/{id}/retract` � retracts a published assignment. Faculty/Admin. | `API/Controllers/AssignmentController.cs` |
| `Delete(id, ct)` | `DELETE /api/assignment/{id}` � soft-deletes when no submissions exist. Admin. | `API/Controllers/AssignmentController.cs` |
| `GetByOffering(courseOfferingId, ct)` | `GET /api/assignment/by-offering/{id}` � lists assignments for an offering. | `API/Controllers/AssignmentController.cs` |
| `Submit(request, ct)` | `POST /api/assignment/submit` � student submission. Student. | `API/Controllers/AssignmentController.cs` |
| `GetSubmissions(id, ct)` | `GET /api/assignment/{id}/submissions` � all submissions for an assignment. Faculty/Admin. | `API/Controllers/AssignmentController.cs` |
| `Grade(request, ct)` | `PUT /api/assignment/submissions/grade` � grades a submission. Faculty/Admin. | `API/Controllers/AssignmentController.cs` |
| `Reject(assignmentId, studentProfileId, ct)` | `POST /api/assignment/{id}/submissions/{studentId}/reject` � rejects a submission. Faculty/Admin. | `API/Controllers/AssignmentController.cs` |
| `GetCurrentStudentProfileId()` | Private � extracts student profile ID from "studentProfileId" JWT claim. | `API/Controllers/AssignmentController.cs` |

### API � ResultController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `BulkCreate(request, ct)` | `POST /api/result/bulk` � bulk-creates draft results for a class. Faculty/Admin. | `API/Controllers/ResultController.cs` |
| `Publish(studentProfileId, courseOfferingId, resultType, ct)` | `POST /api/result/publish` � publishes a single result. Faculty/Admin. | `API/Controllers/ResultController.cs` |
| `PublishAll(courseOfferingId, ct)` | `POST /api/result/publish-all` � publishes all drafts for an offering. Faculty/Admin. | `API/Controllers/ResultController.cs` |
| `Correct(studentProfileId, courseOfferingId, resultType, request, ct)` | `PUT /api/result/correct` � Admin correction of a published result. Admin only. | `API/Controllers/ResultController.cs` |
| `GetMyResults(ct)` | `GET /api/result/my-results` � student's own published results. Student. | `API/Controllers/ResultController.cs` |
| `GetByStudent(studentProfileId, ct)` | `GET /api/result/by-student/{id}` � all results for a student. Faculty/Admin. | `API/Controllers/ResultController.cs` |
| `GetTranscript(studentProfileId, format, ct)` | `GET /api/result/transcript/{id}` � exports transcript, logs request. All roles. | `API/Controllers/ResultController.cs` |
| `GetTranscriptHistory(studentProfileId, ct)` | `GET /api/result/transcript/{id}/history` � export history for a student. Faculty/Admin. | `API/Controllers/ResultController.cs` |

  ---

## Phase 4 � Notifications and Attendance

### Domain � Notification

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Notification(title, body, type, senderUserId)` | Constructor � user-authored notification. | `Domain/Notifications/Notification.cs` |
| `Notification(title, body, type)` | Constructor � system-generated notification (no human sender). | `Domain/Notifications/Notification.cs` |

### Domain � NotificationRecipient

| Function Name | Purpose | Location |
| --- | --- | --- |
| `NotificationRecipient(notificationId, recipientUserId)` | Constructor � creates an unread delivery record for the user. | `Domain/Notifications/NotificationRecipient.cs` |
| `MarkRead()` | Marks the notification as read. Idempotent. | `Domain/Notifications/NotificationRecipient.cs` |

### Domain � AttendanceRecord

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AttendanceRecord(studentProfileId, courseOfferingId, date, status, markedByUserId, remarks?)` | Constructor � normalises date to UTC date only. | `Domain/Attendance/AttendanceRecord.cs` |
| `Correct(newStatus, correctedByUserId, remarks?)` | Corrects status and records the correcting user. | `Domain/Attendance/AttendanceRecord.cs` |

### Infrastructure � NotificationRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AddAsync(notification, ct)` | Queues a notification for insertion. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `Update(notification)` | Marks notification as modified. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `GetForUserAsync(userId, unreadOnly, skip, take, ct)` | Returns paged inbox for a user (active notifications only). | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `GetUnreadCountAsync(userId, ct)` | Returns unread active notification count for the user's badge. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `GetRecipientAsync(notificationId, userId, ct)` | Returns a specific delivery record or null. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `AddRecipientsAsync(recipients, ct)` | Bulk-inserts recipient rows for fan-out on dispatch. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `UpdateRecipient(recipient)` | Marks a recipient row as modified (read state). | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |

### Infrastructure � AttendanceRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAsync(studentProfileId, courseOfferingId, date, ct)` | Returns the attendance record for the combination or null. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `ExistsAsync(studentProfileId, courseOfferingId, date, ct)` | Returns true when a record already exists. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `GetByOfferingAsync(courseOfferingId, from?, to?, ct)` | Returns records for an offering, optionally filtered by date range. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `GetByStudentAsync(studentProfileId, courseOfferingId?, ct)` | Returns records for a student, optionally scoped to one offering. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `GetAttendanceSummaryAsync(studentProfileId, courseOfferingId, ct)` | Returns (TotalSessions, AttendedSessions) for the student. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `GetBelowThresholdAsync(thresholdPercent, ct)` | Returns all student-offering pairs with attendance below threshold. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `AddAsync(record, ct)` | Queues a single attendance record for insertion. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `AddRangeAsync(records, ct)` | Queues multiple records for bulk insertion. | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |
| `Update(record)` | Marks a record as modified (correction). | `Infrastructure/Repositories/NotificationAttendanceRepositories.cs` |

### Application � NotificationService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SendAsync(request, senderUserId, ct)` | Creates notification and fans out to recipient list. Returns notification ID. | `Application/Notifications/NotificationService.cs` |
| `SendSystemAsync(title, body, type, recipientUserIds, ct)` | System-generated fan-out (no human sender). Returns notification ID. | `Application/Notifications/NotificationService.cs` |
| `DeactivateAsync(notificationId, ct)` | Deactivates notification from all inboxes. Returns false if not found. | `Application/Notifications/NotificationService.cs` |
| `GetInboxAsync(userId, unreadOnly, page, pageSize, ct)` | Returns paged inbox for a user with optional unread filter. | `Application/Notifications/NotificationService.cs` |
| `GetBadgeAsync(userId, ct)` | Returns unread count for the notification bell badge. | `Application/Notifications/NotificationService.cs` |
| `MarkReadAsync(notificationId, userId, ct)` | Marks a specific notification as read. Idempotent. | `Application/Notifications/NotificationService.cs` |
| `MarkAllReadAsync(userId, ct)` | Marks all unread notifications as read for the user. | `Application/Notifications/NotificationService.cs` |
| `ToResponse(recipient)` | Private � maps NotificationRecipient (with navigation) to DTO. | `Application/Notifications/NotificationService.cs` |

### Application � AttendanceService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `MarkAsync(request, markedByUserId, ct)` | Records single attendance. Returns false on duplicate. | `Application/Attendance/AttendanceService.cs` |
| `BulkMarkAsync(request, markedByUserId, ct)` | Bulk-marks a class; skips duplicates. Returns inserted count. | `Application/Attendance/AttendanceService.cs` |
| `CorrectAsync(request, correctedByUserId, ct)` | Corrects an existing record. Returns false if not found. | `Application/Attendance/AttendanceService.cs` |
| `GetSummaryAsync(studentProfileId, courseOfferingId, ct)` | Returns attendance percentage summary for a student in an offering. | `Application/Attendance/AttendanceService.cs` |
| `ToResponse(record)` | Private � maps AttendanceRecord to AttendanceResponse DTO. | `Application/Attendance/AttendanceService.cs` |

### API � NotificationController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Send(request, ct)` | `POST /api/notification` � dispatches notification to a user list. Admin/Faculty. | `API/Controllers/NotificationController.cs` |
| `GetInbox(unreadOnly, page, pageSize, ct)` | `GET /api/notification/inbox` � paged inbox for current user. | `API/Controllers/NotificationController.cs` |
| `GetBadge(ct)` | `GET /api/notification/badge` � unread count for the bell icon. | `API/Controllers/NotificationController.cs` |
| `MarkRead(id, ct)` | `POST /api/notification/{id}/read` � marks one notification read. | `API/Controllers/NotificationController.cs` |
| `MarkAllRead(ct)` | `POST /api/notification/read-all` � marks all unread as read. | `API/Controllers/NotificationController.cs` |

### API � AttendanceController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Mark(request, ct)` | `POST /api/attendance` � marks attendance for one student. Faculty/Admin. | `API/Controllers/AttendanceController.cs` |
| `BulkMark(request, ct)` | `POST /api/attendance/bulk` � bulk-marks a full class. Faculty/Admin. | `API/Controllers/AttendanceController.cs` |
| `Correct(request, ct)` | `PUT /api/attendance/correct` � corrects an existing record. Faculty/Admin. | `API/Controllers/AttendanceController.cs` |
| `GetByOffering(courseOfferingId, from, to, ct)` | `GET /api/attendance/by-offering/{id}` � records for an offering. Faculty/Admin. | `API/Controllers/AttendanceController.cs` |
| `GetByStudent(studentProfileId, courseOfferingId, ct)` | `GET /api/attendance/by-student/{id}` � records for a student. Faculty/Admin. | `API/Controllers/AttendanceController.cs` |
| `GetMyAttendance(courseOfferingId, ct)` | `GET /api/attendance/my-attendance` � student's own records. Student. | `API/Controllers/AttendanceController.cs` |
| `GetSummary(studentProfileId, courseOfferingId, ct)` | `GET /api/attendance/summary/{studentId}/{offeringId}` � percentage summary. All roles. | `API/Controllers/AttendanceController.cs` |
| `GetBelowThreshold(threshold, ct)` | `GET /api/attendance/below-threshold` � students below threshold. Admin. | `API/Controllers/AttendanceController.cs` |

### Background Job � AttendanceAlertJob

| Function Name | Purpose | Location |
| --- | --- | --- |

  ---

## Phase 5 � Quizzes and FYP (Sprints 10�11)

### Domain � Quiz

| Function Name | Purpose | Location |
| --- | --- | --- |
| Quiz(courseOfferingId, title, createdByUserId, instructions, timeLimitMinutes, maxAttempts, availableFrom, availableUntil) | Creates a new quiz in unpublished state. | Domain/Quizzes/Quiz.cs |
| Publish() | Marks the quiz as published so students can view and attempt it. | Domain/Quizzes/Quiz.cs |
| Unpublish() | Reverts the quiz to draft/unpublished state. | Domain/Quizzes/Quiz.cs |
| Deactivate() | Soft-deletes the quiz by setting IsActive=false. | Domain/Quizzes/Quiz.cs |
| Update(title, instructions, timeLimitMinutes, maxAttempts, availableFrom, availableUntil) | Updates editable quiz metadata. | Domain/Quizzes/Quiz.cs |

### Domain � QuizQuestion

| Function Name | Purpose | Location |
| --- | --- | --- |
| QuizQuestion(quizId, text, type, marks, orderIndex) | Creates a new question within a quiz. | Domain/Quizzes/Quiz.cs |
| Update(text, marks, orderIndex) | Updates the question text, marks, and display order. | Domain/Quizzes/Quiz.cs |

### Domain � QuizOption

| Function Name | Purpose | Location |
| --- | --- | --- |
| QuizOption(quizQuestionId, text, isCorrect, orderIndex) | Creates an answer option for a MCQ or TrueFalse question. | Domain/Quizzes/Quiz.cs |

### Domain � QuizAttempt

| Function Name | Purpose | Location |
| --- | --- | --- |
| QuizAttempt(quizId, studentProfileId) | Starts a new attempt, setting status=InProgress and StartedAt=UtcNow. | Domain/Quizzes/QuizAttempt.cs |
| Submit() | Marks the attempt as Submitted and records FinishedAt. | Domain/Quizzes/QuizAttempt.cs |
| TimeOut() | Marks the attempt as TimedOut and records FinishedAt. | Domain/Quizzes/QuizAttempt.cs |
| Abandon() | Marks the attempt as Abandoned and records FinishedAt. | Domain/Quizzes/QuizAttempt.cs |
| RecordScore(score) | Sets the computed TotalScore on the attempt. | Domain/Quizzes/QuizAttempt.cs |

### Domain � QuizAnswer

| Function Name | Purpose | Location |
| --- | --- | --- |
| QuizAnswer(quizAttemptId, quizQuestionId, selectedOptionId) | Records an MCQ or TrueFalse answer by option ID. | Domain/Quizzes/QuizAttempt.cs |
| QuizAnswer(quizAttemptId, quizQuestionId, textResponse) | Records a ShortAnswer response as free text. | Domain/Quizzes/QuizAttempt.cs |
| AwardMarks(marks) | Sets the marks awarded for manually graded short answers. | Domain/Quizzes/QuizAttempt.cs |

### Domain � FypProject

| Function Name | Purpose | Location |
| --- | --- | --- |
| FypProject(studentProfileId, departmentId, title, description) | Creates a new FYP proposal in Proposed state. | Domain/Fyp/FypProject.cs |
| Approve(remarks) | Transitions the project to Approved with optional coordinator remarks. | Domain/Fyp/FypProject.cs |
| Reject(remarks) | Transitions the project to Rejected with mandatory remarks. | Domain/Fyp/FypProject.cs |
| AssignSupervisor(supervisorUserId) | Records the supervising faculty member and sets status to InProgress. | Domain/Fyp/FypProject.cs |
| Complete() | Marks the project as Completed. | Domain/Fyp/FypProject.cs |
| Update(title, description) | Updates the project title and description. | Domain/Fyp/FypProject.cs |

### Domain � FypPanelMember

| Function Name | Purpose | Location |
| --- | --- | --- |
| FypPanelMember(fypProjectId, userId, role) | Adds a faculty member to the project panel with a specified role. | Domain/Fyp/FypProject.cs |

### Domain � FypMeeting

| Function Name | Purpose | Location |
| --- | --- | --- |
| FypMeeting(fypProjectId, scheduledAt, venue, organiserUserId, agenda) | Schedules a new FYP meeting in Scheduled state. | Domain/Fyp/FypProject.cs |
| Complete(minutes) | Marks the meeting as Completed and records optional minutes. | Domain/Fyp/FypProject.cs |
| Cancel() | Cancels a scheduled meeting. | Domain/Fyp/FypProject.cs |
| Reschedule(scheduledAt, venue, agenda) | Updates the meeting time, venue, and agenda and resets status to Scheduled. | Domain/Fyp/FypProject.cs |

### Infrastructure � QuizRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| GetByIdAsync(id, ct) | Fetches a quiz by primary key. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetWithQuestionsAsync(id, ct) | Fetches a quiz with all questions and their options included. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetByOfferingAsync(courseOfferingId, ct) | Returns all published quizzes for a course offering. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddAsync(quiz, ct) | Persists a new quiz entity. | Infrastructure/Repositories/QuizFypRepositories.cs |
| Update(quiz) | Marks a quiz as modified in the EF change tracker. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetQuestionByIdAsync(questionId, ct) | Fetches a single quiz question by ID. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddQuestionAsync(question, ct) | Persists a new quiz question. | Infrastructure/Repositories/QuizFypRepositories.cs |
| UpdateQuestion(question) | Marks a question as modified. | Infrastructure/Repositories/QuizFypRepositories.cs |
| RemoveQuestion(question) | Removes a question from the context. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddOptionsAsync(options, ct) | Bulk-adds a collection of answer options. | Infrastructure/Repositories/QuizFypRepositories.cs |
| RemoveOptions(options) | Removes a collection of options from the context. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetAttemptByIdAsync(attemptId, ct) | Fetches an attempt with its answers included. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetAttemptsAsync(quizId, studentProfileId, ct) | Returns all attempts for a student on a quiz. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetAllAttemptsForStudentAsync(studentProfileId, ct) | Returns all attempts across all quizzes for a student (portal summary). | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetAttemptCountAsync(quizId, studentProfileId, ct) | Returns the count of completed or timed-out attempts for cap checking. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetInProgressAttemptAsync(quizId, studentProfileId, ct) | Returns any in-progress attempt for a student on a quiz. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddAttemptAsync(attempt, ct) | Persists a new quiz attempt. | Infrastructure/Repositories/QuizFypRepositories.cs |
| UpdateAttempt(attempt) | Marks an attempt as modified. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddAnswersAsync(answers, ct) | Bulk-adds a collection of quiz answers. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetAnswerByIdAsync(answerId, ct) | Fetches a single answer by ID for manual grading. | Infrastructure/Repositories/QuizFypRepositories.cs |
| UpdateAnswer(answer) | Marks an answer as modified. | Infrastructure/Repositories/QuizFypRepositories.cs |
| SaveChangesAsync(ct) | Commits all pending changes to the database. | Infrastructure/Repositories/QuizFypRepositories.cs |

### Infrastructure � FypRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| GetByIdAsync(id, ct) | Fetches an FYP project by primary key. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetWithDetailsAsync(id, ct) | Fetches a project with panel members and meetings included. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetByStudentAsync(studentProfileId, ct) | Returns all projects for a student. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetByDepartmentAsync(departmentId, status, ct) | Returns department projects optionally filtered by status. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetBySupervisorAsync(supervisorUserId, ct) | Returns all projects supervised by a given faculty member. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddAsync(project, ct) | Persists a new FYP project. | Infrastructure/Repositories/QuizFypRepositories.cs |
| Update(project) | Marks a project as modified. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetPanelMembersAsync(projectId, ct) | Returns all panel members for a project. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetPanelMemberAsync(projectId, userId, ct) | Returns a specific panel member by project and user ID. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddPanelMemberAsync(member, ct) | Persists a new panel member. | Infrastructure/Repositories/QuizFypRepositories.cs |
| RemovePanelMember(member) | Removes a panel member from the context. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetMeetingByIdAsync(meetingId, ct) | Fetches a meeting by primary key. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetMeetingsByProjectAsync(projectId, ct) | Returns all meetings for a project ordered by scheduled date. | Infrastructure/Repositories/QuizFypRepositories.cs |
| GetUpcomingMeetingsAsync(supervisorUserId, ct) | Returns upcoming scheduled meetings organised by a supervisor. | Infrastructure/Repositories/QuizFypRepositories.cs |
| AddMeetingAsync(meeting, ct) | Persists a new meeting. | Infrastructure/Repositories/QuizFypRepositories.cs |
| UpdateMeeting(meeting) | Marks a meeting as modified. | Infrastructure/Repositories/QuizFypRepositories.cs |
| SaveChangesAsync(ct) | Commits all pending changes to the database. | Infrastructure/Repositories/QuizFypRepositories.cs |

### Application � QuizService

| Function Name | Purpose | Location |
| --- | --- | --- |
| CreateAsync(request, facultyUserId, ct) | Creates a new quiz and persists it. | Application/Quizzes/QuizService.cs |
| UpdateAsync(quizId, request, ct) | Updates quiz metadata; returns false if not found. | Application/Quizzes/QuizService.cs |
| PublishAsync(quizId, ct) | Publishes a quiz so students can access it. | Application/Quizzes/QuizService.cs |
| UnpublishAsync(quizId, ct) | Reverts a quiz to draft state. | Application/Quizzes/QuizService.cs |
| DeactivateAsync(quizId, ct) | Soft-deletes a quiz. | Application/Quizzes/QuizService.cs |
| AddQuestionAsync(request, ct) | Adds a question with its options to an existing quiz. | Application/Quizzes/QuizService.cs |
| UpdateQuestionAsync(questionId, request, ct) | Updates question text, marks, and order; replaces options if provided. | Application/Quizzes/QuizService.cs |
| RemoveQuestionAsync(questionId, ct) | Removes a question and all its options. | Application/Quizzes/QuizService.cs |
| GetByOfferingAsync(courseOfferingId, ct) | Returns summary list of published quizzes for a course offering. | Application/Quizzes/QuizService.cs |
| GetDetailAsync(quizId, ct) | Returns full quiz detail including questions and options. | Application/Quizzes/QuizService.cs |
| StartAttemptAsync(quizId, studentProfileId, ct) | Validates availability, attempt cap, and in-progress check, then creates a new attempt. | Application/Quizzes/QuizService.cs |
| SubmitAttemptAsync(request, studentProfileId, ct) | Records answers, auto-grades MCQ/TrueFalse, computes score, submits attempt. | Application/Quizzes/QuizService.cs |
| GetStudentAttemptsAsync(quizId, studentProfileId, ct) | Returns all attempts for a student on a quiz. | Application/Quizzes/QuizService.cs |
| GetAllMyAttemptsAsync(studentProfileId, ct) | Returns all attempts across all quizzes for a student. | Application/Quizzes/QuizService.cs |
| GetAttemptDetailAsync(attemptId, ct) | Returns detailed attempt data including answer responses. | Application/Quizzes/QuizService.cs |
| GradeAnswerAsync(request, ct) | Awards marks to a short-answer response and updates attempt total score. | Application/Quizzes/QuizService.cs |
| ToSummary(quiz) | Private � maps Quiz to QuizSummaryResponse. | Application/Quizzes/QuizService.cs |
| ToDetail(quiz) | Private � maps Quiz with questions to QuizDetailResponse. | Application/Quizzes/QuizService.cs |
| ToQuestionResponse(question, hideAnswers) | Private � maps a QuizQuestion to QuestionResponse, optionally hiding correct answers. | Application/Quizzes/QuizService.cs |
| ToAttemptResponse(attempt) | Private � maps QuizAttempt to AttemptResponse. | Application/Quizzes/QuizService.cs |
| ToAttemptDetail(attempt) | Private � maps QuizAttempt with answers to AttemptDetailResponse. | Application/Quizzes/QuizService.cs |

### Application � FypService

| Function Name | Purpose | Location |
| --- | --- | --- |
| ProposeAsync(request, studentProfileId, ct) | Creates a new FYP project proposal and returns its ID. | Application/Fyp/FypService.cs |
| UpdateAsync(projectId, request, ct) | Updates project title and description. | Application/Fyp/FypService.cs |
| ApproveAsync(projectId, request, ct) | Approves a proposal with optional coordinator remarks. | Application/Fyp/FypService.cs |
| RejectAsync(projectId, request, ct) | Rejects a proposal with mandatory remarks. | Application/Fyp/FypService.cs |
| AssignSupervisorAsync(projectId, request, ct) | Assigns a supervisor to the project. | Application/Fyp/FypService.cs |
| CompleteAsync(projectId, ct) | Marks a project as completed. | Application/Fyp/FypService.cs |
| GetByStudentAsync(studentProfileId, ct) | Returns all projects for a student as summary DTOs. | Application/Fyp/FypService.cs |
| GetByDepartmentAsync(departmentId, statusString, ct) | Returns department projects filtered by optional status string. | Application/Fyp/FypService.cs |
| GetBySupervisorAsync(supervisorUserId, ct) | Returns all projects supervised by a faculty user. | Application/Fyp/FypService.cs |
| GetDetailAsync(projectId, ct) | Returns full project detail with panel and meetings. | Application/Fyp/FypService.cs |
| AddPanelMemberAsync(projectId, request, ct) | Adds a faculty member to the project panel. | Application/Fyp/FypService.cs |
| RemovePanelMemberAsync(projectId, userId, ct) | Removes a panel member by user ID. | Application/Fyp/FypService.cs |
| ScheduleMeetingAsync(request, organiserUserId, ct) | Creates a new scheduled meeting for a project. | Application/Fyp/FypService.cs |
| RescheduleMeetingAsync(meetingId, request, ct) | Reschedules a meeting to a new time, venue, and agenda. | Application/Fyp/FypService.cs |
| CompleteMeetingAsync(meetingId, request, ct) | Marks a meeting as completed with optional minutes. | Application/Fyp/FypService.cs |
| CancelMeetingAsync(meetingId, ct) | Cancels a scheduled meeting. | Application/Fyp/FypService.cs |
| GetMeetingsByProjectAsync(projectId, ct) | Returns all meetings for a project as response DTOs. | Application/Fyp/FypService.cs |
| GetUpcomingMeetingsAsync(supervisorUserId, ct) | Returns upcoming meetings organised by the supervisor. | Application/Fyp/FypService.cs |
| ToSummary(project) | Private � maps FypProject to FypProjectSummaryResponse. | Application/Fyp/FypService.cs |
| ToDetail(project) | Private � maps FypProject with panel/meetings to FypProjectDetailResponse. | Application/Fyp/FypService.cs |
| ToMeetingResponse(meeting) | Private � maps FypMeeting to MeetingResponse. | Application/Fyp/FypService.cs |

### API � QuizController

| Function Name | Purpose | Location |
| --- | --- | --- |
| Create(request, ct) | POST /api/quiz � Creates a quiz (Faculty). | API/Controllers/QuizController.cs |
| Update(id, request, ct) | PUT /api/quiz/{id} � Updates quiz metadata (Faculty). | API/Controllers/QuizController.cs |
| Publish(id, ct) | POST /api/quiz/{id}/publish � Publishes a quiz (Faculty). | API/Controllers/QuizController.cs |
| Unpublish(id, ct) | POST /api/quiz/{id}/unpublish � Unpublishes a quiz (Faculty). | API/Controllers/QuizController.cs |
| Deactivate(id, ct) | DELETE /api/quiz/{id} � Soft-deletes a quiz (Admin). | API/Controllers/QuizController.cs |
| AddQuestion(request, ct) | POST /api/quiz/question � Adds a question to a quiz (Faculty). | API/Controllers/QuizController.cs |
| UpdateQuestion(questionId, request, ct) | PUT /api/quiz/question/{questionId} � Updates a question (Faculty). | API/Controllers/QuizController.cs |
| RemoveQuestion(questionId, ct) | DELETE /api/quiz/question/{questionId} � Removes a question (Faculty). | API/Controllers/QuizController.cs |
| GetByOffering(courseOfferingId, ct) | GET /api/quiz/by-offering/{courseOfferingId} � Lists quizzes for an offering (All). | API/Controllers/QuizController.cs |
| GetDetail(id, ct) | GET /api/quiz/{id} � Returns full quiz detail (All). | API/Controllers/QuizController.cs |
| StartAttempt(id, ct) | POST /api/quiz/{id}/start � Starts a student attempt; 409 if cap reached (Student). | API/Controllers/QuizController.cs |
| SubmitAttempt(request, ct) | POST /api/quiz/attempt/submit � Submits answers and grades MCQ/TrueFalse (Student). | API/Controllers/QuizController.cs |
| GetMyAttempts(id, ct) | GET /api/quiz/{id}/my-attempts � Returns student's own attempts (Student). | API/Controllers/QuizController.cs |
| GetAttemptDetail(attemptId, ct) | GET /api/quiz/attempt/{attemptId} � Returns attempt detail with answers (All). | API/Controllers/QuizController.cs |
| GradeAnswer(request, ct) | POST /api/quiz/attempt/grade-answer � Manually grades a short-answer response (Faculty). | API/Controllers/QuizController.cs |
| GetCurrentUserId() | Private � Extracts authenticated user ID from JWT NameIdentifier claim. | API/Controllers/QuizController.cs |
| GetStudentProfileId() | Private � Extracts student profile ID from the studentProfileId JWT claim. | API/Controllers/QuizController.cs |

### API � FypController

| Function Name | Purpose | Location |
| --- | --- | --- |
| Propose(request, ct) | POST /api/fyp � Submits an FYP proposal (Student). | API/Controllers/FypController.cs |
| Update(id, request, ct) | PUT /api/fyp/{id} � Updates project title/description (Student). | API/Controllers/FypController.cs |
| Approve(id, request, ct) | POST /api/fyp/{id}/approve � Approves a proposal (Admin). | API/Controllers/FypController.cs |
| Reject(id, request, ct) | POST /api/fyp/{id}/reject � Rejects a proposal with remarks (Admin). | API/Controllers/FypController.cs |
| AssignSupervisor(id, request, ct) | POST /api/fyp/{id}/assign-supervisor � Assigns a supervisor (Admin). | API/Controllers/FypController.cs |
| Complete(id, ct) | POST /api/fyp/{id}/complete � Marks a project as completed (Admin). | API/Controllers/FypController.cs |
| GetMyProjects(ct) | GET /api/fyp/my-projects � Returns current student's projects (Student). | API/Controllers/FypController.cs |
| GetByDepartment(departmentId, status, ct) | GET /api/fyp/by-department/{departmentId} � Returns department projects (Faculty). | API/Controllers/FypController.cs |
| GetMySupervised(ct) | GET /api/fyp/my-supervised � Returns projects supervised by current user (Faculty). | API/Controllers/FypController.cs |
| GetDetail(id, ct) | GET /api/fyp/{id} � Returns full project detail (All). | API/Controllers/FypController.cs |
| AddPanelMember(id, request, ct) | POST /api/fyp/{id}/panel � Adds a panel member (Admin). | API/Controllers/FypController.cs |
| RemovePanelMember(id, userId, ct) | DELETE /api/fyp/{id}/panel/{userId} � Removes a panel member (Admin). | API/Controllers/FypController.cs |
| ScheduleMeeting(request, ct) | POST /api/fyp/meeting � Schedules a new FYP meeting (Faculty). | API/Controllers/FypController.cs |
| RescheduleMeeting(meetingId, request, ct) | PUT /api/fyp/meeting/{meetingId} � Reschedules a meeting (Faculty). | API/Controllers/FypController.cs |
| CompleteMeeting(meetingId, request, ct) | POST /api/fyp/meeting/{meetingId}/complete � Completes a meeting (Faculty). | API/Controllers/FypController.cs |
| CancelMeeting(meetingId, ct) | POST /api/fyp/meeting/{meetingId}/cancel � Cancels a meeting (Faculty). | API/Controllers/FypController.cs |
| GetMeetings(id, ct) | GET /api/fyp/{id}/meetings � Returns all meetings for a project (All). | API/Controllers/FypController.cs |
| GetUpcomingMeetings(ct) | GET /api/fyp/meeting/upcoming � Returns upcoming meetings for current supervisor (Faculty). | API/Controllers/FypController.cs |
| GetCurrentUserId() | Private � Extracts authenticated user ID from JWT NameIdentifier claim. | API/Controllers/FypController.cs |
| GetStudentProfileId() | Private � Extracts student profile ID from the studentProfileId JWT claim. | API/Controllers/FypController.cs |

  ---

## Phase 5 � Quizzes and FYP

### Domain � Quiz

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Quiz(courseOfferingId, title, createdByUserId, ...)` | Creates a new quiz in unpublished state. | `Domain/Quizzes/Quiz.cs` |
| `Unpublish()` | Reverts the quiz to draft state. | `Domain/Quizzes/Quiz.cs` |
| `Update(title, instructions, timeLimitMinutes, maxAttempts, availableFrom, availableUntil)` | Updates mutable quiz metadata. | `Domain/Quizzes/Quiz.cs` |
| `QuizQuestion(quizId, text, type, marks, orderIndex)` | Creates a new question attached to a quiz. | `Domain/Quizzes/Quiz.cs` |
| `QuizQuestion.Update(text, marks, orderIndex)` | Updates question text and grading details. | `Domain/Quizzes/Quiz.cs` |
| `QuizOption(quizQuestionId, text, isCorrect, orderIndex)` | Creates an answer option for a question. | `Domain/Quizzes/Quiz.cs` |

### Domain � QuizAttempt

| Function Name | Purpose | Location |
| --- | --- | --- |
| `QuizAttempt(quizId, studentProfileId)` | Starts a new in-progress attempt with StartedAt=UtcNow. | `Domain/Quizzes/QuizAttempt.cs` |
| `Submit()` | Finalises the attempt and records FinishedAt. | `Domain/Quizzes/QuizAttempt.cs` |
| `TimeOut()` | Marks the attempt as timed-out and records FinishedAt. | `Domain/Quizzes/QuizAttempt.cs` |
| `Abandon()` | Marks the attempt as abandoned and records FinishedAt. | `Domain/Quizzes/QuizAttempt.cs` |
| `RecordScore(score)` | Stores the computed total score after grading. | `Domain/Quizzes/QuizAttempt.cs` |
| `QuizAnswer(quizAttemptId, quizQuestionId, selectedOptionId)` | Records an MCQ/TrueFalse answer with chosen option. | `Domain/Quizzes/QuizAttempt.cs` |
| `QuizAnswer(quizAttemptId, quizQuestionId, textResponse)` | Records a short-answer textual response. | `Domain/Quizzes/QuizAttempt.cs` |
| `AwardMarks(marks)` | Stores instructor-awarded marks for a short-answer response. | `Domain/Quizzes/QuizAttempt.cs` |

### Domain � FypProject

| Function Name | Purpose | Location |
| --- | --- | --- |
| `FypProject(studentProfileId, departmentId, title, description)` | Proposes a new FYP project in Proposed state. | `Domain/Fyp/FypProject.cs` |
| `Approve(remarks)` | Moves project to Approved state and stores coordinator remarks. | `Domain/Fyp/FypProject.cs` |
| `Reject(remarks)` | Moves project to Rejected state with mandatory remarks. | `Domain/Fyp/FypProject.cs` |
| `AssignSupervisor(supervisorUserId)` | Links a supervisor and transitions project to InProgress. | `Domain/Fyp/FypProject.cs` |
| `Complete()` | Marks the project as Completed. | `Domain/Fyp/FypProject.cs` |
| `FypProject.Update(title, description)` | Updates project title and description. | `Domain/Fyp/FypProject.cs` |
| `FypPanelMember(fypProjectId, userId, role)` | Assigns a user to the evaluation panel with a given role. | `Domain/Fyp/FypProject.cs` |
| `FypMeeting(fypProjectId, scheduledAt, venue, organiserUserId, agenda)` | Schedules a new meeting in Scheduled state. | `Domain/Fyp/FypProject.cs` |
| `FypMeeting.Complete(minutes)` | Marks meeting as completed and records minutes. | `Domain/Fyp/FypProject.cs` |
| `FypMeeting.Cancel()` | Cancels a scheduled meeting. | `Domain/Fyp/FypProject.cs` |
| `FypMeeting.Reschedule(scheduledAt, venue, agenda)` | Updates meeting time, venue and agenda. | `Domain/Fyp/FypProject.cs` |

### Infrastructure � QuizRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetWithQuestionsAsync(id, ct)` | Fetches quiz with questions and options eager-loaded. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddAsync(quiz, ct)` | Inserts a new quiz. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `Update(quiz)` | Marks quiz entity as modified. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetQuestionByIdAsync(questionId, ct)` | Fetches a question by primary key. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddQuestionAsync(question, ct)` | Inserts a new question. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `UpdateQuestion(question)` | Marks question as modified. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `RemoveQuestion(question)` | Removes a question from the context. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddOptionsAsync(options, ct)` | Bulk inserts a collection of options. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `RemoveOptions(options)` | Bulk removes a collection of options. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetAttemptByIdAsync(attemptId, ct)` | Fetches an attempt with its answers. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetAttemptsAsync(quizId, studentProfileId, ct)` | Lists all attempts for a student on a quiz. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetAttemptCountAsync(quizId, studentProfileId, ct)` | Counts completed/timed-out attempts for cap validation. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetInProgressAttemptAsync(quizId, studentProfileId, ct)` | Returns an active (InProgress) attempt if one exists. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddAttemptAsync(attempt, ct)` | Inserts a new attempt. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `UpdateAttempt(attempt)` | Marks attempt as modified. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddAnswersAsync(answers, ct)` | Bulk inserts submitted answers. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetAnswerByIdAsync(answerId, ct)` | Fetches a single answer for manual grading. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `UpdateAnswer(answer)` | Marks answer as modified after manual grading. | `Infrastructure/Repositories/QuizFypRepositories.cs` |

### Infrastructure � FypRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetWithDetailsAsync(id, ct)` | Fetches project with panel members and meetings eager-loaded. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetByDepartmentAsync(departmentId, status, ct)` | Lists department projects, optionally filtered by status. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetBySupervisorAsync(supervisorUserId, ct)` | Lists projects supervised by a faculty user. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddAsync(project, ct)` | Inserts a new FYP project. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `Update(project)` | Marks project as modified. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetPanelMembersAsync(projectId, ct)` | Lists all panel members for a project. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetPanelMemberAsync(projectId, userId, ct)` | Fetches a specific panel member record. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddPanelMemberAsync(member, ct)` | Inserts a panel member assignment. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `RemovePanelMember(member)` | Removes a panel member from the context. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetMeetingByIdAsync(meetingId, ct)` | Fetches a meeting by primary key. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetMeetingsByProjectAsync(projectId, ct)` | Lists all meetings for a project ordered by scheduled date. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `GetUpcomingMeetingsAsync(supervisorUserId, ct)` | Returns future scheduled meetings for a supervisor. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `AddMeetingAsync(meeting, ct)` | Inserts a new meeting. | `Infrastructure/Repositories/QuizFypRepositories.cs` |
| `UpdateMeeting(meeting)` | Marks meeting as modified. | `Infrastructure/Repositories/QuizFypRepositories.cs` |

### Application � QuizService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `UpdateAsync(quizId, request, ct)` | Applies metadata updates to an existing quiz. | `Application/Quizzes/QuizService.cs` |
| `PublishAsync(quizId, ct)` | Publishes a quiz so students can see it. | `Application/Quizzes/QuizService.cs` |
| `UnpublishAsync(quizId, ct)` | Reverts a quiz to draft. | `Application/Quizzes/QuizService.cs` |
| `DeactivateAsync(quizId, ct)` | Soft-deletes a quiz. | `Application/Quizzes/QuizService.cs` |
| `AddQuestionAsync(request, ct)` | Adds a question with options to a quiz. | `Application/Quizzes/QuizService.cs` |
| `UpdateQuestionAsync(questionId, request, ct)` | Updates question text and grading. | `Application/Quizzes/QuizService.cs` |
| `RemoveQuestionAsync(questionId, ct)` | Removes a question and its options. | `Application/Quizzes/QuizService.cs` |
| `GetDetailAsync(quizId, ct)` | Returns full quiz detail with questions and options. | `Application/Quizzes/QuizService.cs` |
| `StartAttemptAsync(quizId, studentProfileId, ct)` | Validates and starts a new quiz attempt. | `Application/Quizzes/QuizService.cs` |
| `SubmitAttemptAsync(request, ct)` | Records answers, auto-grades MCQ/TrueFalse, computes score. | `Application/Quizzes/QuizService.cs` |
| `GetStudentAttemptsAsync(quizId, studentProfileId, ct)` | Lists a student's attempts on a quiz. | `Application/Quizzes/QuizService.cs` |
| `GetAttemptDetailAsync(attemptId, ct)` | Returns full attempt detail with answers. | `Application/Quizzes/QuizService.cs` |
| `GradeAnswerAsync(request, ct)` | Awards marks to a short-answer response. | `Application/Quizzes/QuizService.cs` |
| `ToSummary(quiz)` | Maps Quiz entity to QuizSummaryResponse DTO. | `Application/Quizzes/QuizService.cs` |
| `ToDetail(quiz)` | Maps Quiz with questions to QuizDetailResponse DTO. | `Application/Quizzes/QuizService.cs` |
| `ToQuestionResponse(q, revealAnswers)` | Maps QuizQuestion to QuestionResponse DTO, optionally revealing correct options. | `Application/Quizzes/QuizService.cs` |
| `ToAttemptResponse(attempt)` | Maps QuizAttempt to AttemptResponse DTO. | `Application/Quizzes/QuizService.cs` |
| `ToAttemptDetail(attempt)` | Maps QuizAttempt with answers to AttemptDetailResponse DTO. | `Application/Quizzes/QuizService.cs` |

### Application � FypService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ProposeAsync(request, studentProfileId, ct)` | Creates a new FYP project proposal. | `Application/Fyp/FypService.cs` |
| `UpdateAsync(projectId, request, ct)` | Updates project title and description. | `Application/Fyp/FypService.cs` |
| `ApproveAsync(projectId, request, ct)` | Approves a project proposal. | `Application/Fyp/FypService.cs` |
| `RejectAsync(projectId, request, ct)` | Rejects a project proposal with remarks. | `Application/Fyp/FypService.cs` |
| `AssignSupervisorAsync(projectId, request, ct)` | Assigns a faculty supervisor to a project. | `Application/Fyp/FypService.cs` |
| `CompleteAsync(projectId, ct)` | Marks a project as completed. | `Application/Fyp/FypService.cs` |
| `GetDetailAsync(projectId, ct)` | Returns full project detail including panel and meetings. | `Application/Fyp/FypService.cs` |
| `AddPanelMemberAsync(projectId, request, ct)` | Adds a user to the FYP evaluation panel. | `Application/Fyp/FypService.cs` |
| `RemovePanelMemberAsync(projectId, userId, ct)` | Removes a user from the evaluation panel. | `Application/Fyp/FypService.cs` |
| `ScheduleMeetingAsync(request, organiserUserId, ct)` | Schedules a new FYP meeting. | `Application/Fyp/FypService.cs` |
| `RescheduleMeetingAsync(meetingId, request, ct)` | Reschedules an existing meeting. | `Application/Fyp/FypService.cs` |
| `CompleteMeetingAsync(meetingId, request, ct)` | Marks a meeting as completed and stores minutes. | `Application/Fyp/FypService.cs` |
| `CancelMeetingAsync(meetingId, ct)` | Cancels a scheduled meeting. | `Application/Fyp/FypService.cs` |
| `ToSummary(project)` | Maps FypProject to FypProjectSummaryResponse DTO. | `Application/Fyp/FypService.cs` |
| `ToDetail(project)` | Maps FypProject to FypProjectDetailResponse DTO. | `Application/Fyp/FypService.cs` |
| `ToMeetingResponse(meeting)` | Maps FypMeeting to MeetingResponse DTO. | `Application/Fyp/FypService.cs` |

### API � QuizController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Unpublish(id, ct)` | POST /api/quiz/{id}/unpublish � Reverts quiz to draft. Faculty only. | `API/Controllers/QuizController.cs` |
| `AddQuestion(request, ct)` | POST /api/quiz/question � Adds a question to a quiz. Faculty only. | `API/Controllers/QuizController.cs` |
| `UpdateQuestion(questionId, request, ct)` | PUT /api/quiz/question/{questionId} � Updates a question. Faculty only. | `API/Controllers/QuizController.cs` |
| `RemoveQuestion(questionId, ct)` | DELETE /api/quiz/question/{questionId} � Removes a question. Faculty only. | `API/Controllers/QuizController.cs` |
| `GetDetail(id, ct)` | GET /api/quiz/{id} � Returns quiz with questions and options. | `API/Controllers/QuizController.cs` |
| `StartAttempt(id, ct)` | POST /api/quiz/{id}/start � Starts a new attempt. Student only. | `API/Controllers/QuizController.cs` |
| `SubmitAttempt(request, ct)` | POST /api/quiz/attempt/submit � Submits and auto-grades an attempt. Student only. | `API/Controllers/QuizController.cs` |
| `GetMyAttempts(id, ct)` | GET /api/quiz/{id}/my-attempts � Lists a student's own attempts. Student only. | `API/Controllers/QuizController.cs` |
| `GetAttemptDetail(attemptId, ct)` | GET /api/quiz/attempt/{attemptId} � Returns attempt with answers. | `API/Controllers/QuizController.cs` |
| `GradeAnswer(request, ct)` | POST /api/quiz/attempt/grade-answer � Awards marks to a short-answer. Faculty only. | `API/Controllers/QuizController.cs` |

### API � FypController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Propose(request, ct)` | POST /api/fyp � Submits a new FYP project proposal. Student only. | `API/Controllers/FypController.cs` |
| `Approve(id, request, ct)` | POST /api/fyp/{id}/approve � Approves a proposal. Admin only. | `API/Controllers/FypController.cs` |
| `Reject(id, request, ct)` | POST /api/fyp/{id}/reject � Rejects a proposal with remarks. Admin only. | `API/Controllers/FypController.cs` |
| `AssignSupervisor(id, request, ct)` | POST /api/fyp/{id}/assign-supervisor � Assigns a supervisor. Admin only. | `API/Controllers/FypController.cs` |
| `Complete(id, ct)` | POST /api/fyp/{id}/complete � Marks a project completed. Admin only. | `API/Controllers/FypController.cs` |
| `GetMyProjects(ct)` | GET /api/fyp/my-projects � Returns the student's own projects. | `API/Controllers/FypController.cs` |
| `GetByDepartment(departmentId, status, ct)` | GET /api/fyp/by-department/{departmentId} � Lists department projects. Faculty only. | `API/Controllers/FypController.cs` |
| `GetMySupervised(ct)` | GET /api/fyp/my-supervised � Returns supervised projects. Faculty only. | `API/Controllers/FypController.cs` |
| `AddPanelMember(id, request, ct)` | POST /api/fyp/{id}/panel � Adds a panel member. Admin only. | `API/Controllers/FypController.cs` |
| `RemovePanelMember(id, userId, ct)` | DELETE /api/fyp/{id}/panel/{userId} � Removes a panel member. Admin only. | `API/Controllers/FypController.cs` |
| `ScheduleMeeting(request, ct)` | POST /api/fyp/meeting � Schedules an FYP meeting. Faculty only. | `API/Controllers/FypController.cs` |
| `RescheduleMeeting(meetingId, request, ct)` | PUT /api/fyp/meeting/{meetingId} � Reschedules a meeting. Faculty only. | `API/Controllers/FypController.cs` |
| `CompleteMeeting(meetingId, request, ct)` | POST /api/fyp/meeting/{meetingId}/complete � Completes a meeting. Faculty only. | `API/Controllers/FypController.cs` |
| `CancelMeeting(meetingId, ct)` | POST /api/fyp/meeting/{meetingId}/cancel � Cancels a meeting. Faculty only. | `API/Controllers/FypController.cs` |
| `GetMeetings(id, ct)` | GET /api/fyp/{id}/meetings � Lists all meetings for a project. | `API/Controllers/FypController.cs` |
| `GetUpcomingMeetings(ct)` | GET /api/fyp/meeting/upcoming � Returns upcoming supervisor meetings. Faculty only. | `API/Controllers/FypController.cs` |

  ---

## Phase 6 � AI Chat Assistant & Analytics

### AiChatService (Application/AiChat/AiChatService.cs)
| Function | Description | File |
| --- | --- | --- |
| `SendMessageAsync(userId, userRole, departmentId, request, ct)` | Sends user message to LLM; guards module status; creates/fetches conversation; persists messages. | `Application/AiChat/AiChatService.cs` |
| `GetConversationsAsync(userId, ct)` | Returns summary list of past conversations for a user. | `Application/AiChat/AiChatService.cs` |
| `GetConversationAsync(conversationId, ct)` | Returns full conversation with message history. | `Application/AiChat/AiChatService.cs` |
| `BuildSystemPrompt(userRole, departmentId)` | Builds role-aware system prompt (Student/Faculty/Admin/SuperAdmin/Finance). | `Application/AiChat/AiChatService.cs` |
| `ToMessageResponse(message)` | Maps ChatMessage domain entity to DTO. | `Application/AiChat/AiChatService.cs` |

### AnalyticsService (Infrastructure/Analytics/AnalyticsService.cs)
| Function | Description | File |
| --- | --- | --- |
| `GetPerformanceReportAsync(departmentId, ct)` | Aggregates student results/submissions per department or all. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `GetAttendanceReportAsync(departmentId, ct)` | Attendance summary per student per course; supports dept filter. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `GetAssignmentStatsAsync(departmentId, ct)` | Assignment submission/grading stats per assignment. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `GetQuizStatsAsync(departmentId, ct)` | Quiz attempt/score stats per quiz. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `ExportPerformancePdfAsync(departmentId, ct)` | Exports performance report as QuestPDF A4 Landscape PDF. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `ExportAttendancePdfAsync(departmentId, ct)` | Exports attendance report as QuestPDF A4 Landscape PDF. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `ExportPerformanceExcelAsync(departmentId, ct)` | Exports performance report as ClosedXML Excel workbook. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `ExportAttendanceExcelAsync(departmentId, ct)` | Exports attendance report as ClosedXML Excel workbook. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `ResolveDeptNameAsync(departmentId, ct)` | Resolves department name from ID; returns "All Departments" for null. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `AddPdfHeader(table, headers)` | Adds styled blue header row to QuestPDF table. | `Infrastructure/Analytics/AnalyticsService.cs` |
| `AddPdfRow(table, values)` | Adds data row with bottom border to QuestPDF table. | `Infrastructure/Analytics/AnalyticsService.cs` |

### AiChatRepository (Infrastructure/Repositories/AiChatRepository.cs)
| Function | Description | File |
| --- | --- | --- |
| `GetByIdAsync(conversationId, ct)` | Fetches a conversation by ID. | `Infrastructure/Repositories/AiChatRepository.cs` |
| `GetByUserAsync(userId, ct)` | Fetches all conversations for a user with messages. | `Infrastructure/Repositories/AiChatRepository.cs` |
| `GetWithMessagesAsync(conversationId, ct)` | Fetches conversation with full message history. | `Infrastructure/Repositories/AiChatRepository.cs` |
| `AddConversationAsync(conversation, ct)` | Persists a new conversation. | `Infrastructure/Repositories/AiChatRepository.cs` |
| `AddMessageAsync(message, ct)` | Persists a new chat message. | `Infrastructure/Repositories/AiChatRepository.cs` |

### OpenAiLlmClient (Infrastructure/AiChat/OpenAiLlmClient.cs)
| Function | Description | File |
| --- | --- | --- |
| `SendAsync(systemPrompt, messages, ct)` | Calls OpenAI-compatible /v1/chat/completions; returns reply + token count. | `Infrastructure/AiChat/OpenAiLlmClient.cs` |

### AiChatController (API/Controllers/AiChatController.cs)
| Function | Description | File |
| --- | --- | --- |
| `SendMessage(request, ct)` | POST /api/ai/message � Send message to AI. All authenticated roles. | `API/Controllers/AiChatController.cs` |
| `GetConversations(ct)` | GET /api/ai/conversations � List user conversations. | `API/Controllers/AiChatController.cs` |
| `GetConversation(conversationId, ct)` | GET /api/ai/conversations/{id} � Get conversation history. | `API/Controllers/AiChatController.cs` |
| `GetDepartmentId()` | Extracts optional departmentId from JWT claim. | `API/Controllers/AiChatController.cs` |

### AnalyticsController (API/Controllers/AnalyticsController.cs)
| Function | Description | File |
| --- | --- | --- |
| `GetPerformance(departmentId, ct)` | GET /api/analytics/performance � Faculty+ scoped. | `API/Controllers/AnalyticsController.cs` |
| `GetAttendance(departmentId, ct)` | GET /api/analytics/attendance � Faculty+ scoped. | `API/Controllers/AnalyticsController.cs` |
| `GetAssignmentStats(departmentId, ct)` | GET /api/analytics/assignments � Faculty+ scoped. | `API/Controllers/AnalyticsController.cs` |
| `GetQuizStats(departmentId, ct)` | GET /api/analytics/quizzes � Faculty+ scoped. | `API/Controllers/AnalyticsController.cs` |
| `ExportPerformancePdf(departmentId, ct)` | GET /api/analytics/performance/export/pdf � Admin+ only. | `API/Controllers/AnalyticsController.cs` |
| `ExportPerformanceExcel(departmentId, ct)` | GET /api/analytics/performance/export/excel � Admin+ only. | `API/Controllers/AnalyticsController.cs` |
| `ExportAttendancePdf(departmentId, ct)` | GET /api/analytics/attendance/export/pdf � Admin+ only. | `API/Controllers/AnalyticsController.cs` |
| `ResolveEffectiveDepartment(requested)` | Scopes Faculty to own dept; Admin/SuperAdmin see all. | `API/Controllers/AnalyticsController.cs` |

### SecurityHeadersMiddleware (API/Middleware/SecurityHeadersMiddleware.cs)
| Function | Description | File |
| --- | --- | --- |
| `InvokeAsync(context)` | Adds HSTS, X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy, Permissions-Policy headers. | `API/Middleware/SecurityHeadersMiddleware.cs` |
| `UseSecurityHeaders(app)` | Extension method to register the middleware. | `API/Middleware/SecurityHeadersMiddleware.cs` |

## Phase 7: Tabsan-Lic + License Import

### Tabsan.Lic � KeyService (tools/Tabsan.Lic/Services/KeyService.cs)
| Function | Description | File |
| --- | --- | --- |
| `GenerateAsync(expiry, label)` | Generates a random VerificationKey, stores SHA-256 hash in SQLite, returns (record, rawToken). | `tools/Tabsan.Lic/Services/KeyService.cs` |
| `GenerateBulkAsync(count, expiry, labelPrefix)` | Generates N keys at once with the same expiry type. | `tools/Tabsan.Lic/Services/KeyService.cs` |
| `ListAllAsync()` | Returns all issued keys ordered by IssuedAt desc. | `tools/Tabsan.Lic/Services/KeyService.cs` |
| `GetByIdAsync(id)` | Returns a key record by auto-increment Id. | `tools/Tabsan.Lic/Services/KeyService.cs` |
| `MarkLicenseGeneratedAsync(key)` | Sets IsLicenseGenerated=true on a key record after .tablic file is built. | `tools/Tabsan.Lic/Services/KeyService.cs` |
| `ExportCsvAsync()` | Exports all issued keys to CSV string (Id, KeyId, ExpiryType, dates, flags, label). | `tools/Tabsan.Lic/Services/KeyService.cs` |

  ---

## Phase 8: Student Lifecycle, Account Security & Finance (Sprints 15–16)

### Domain — StudentProfile new methods
| Function | Description | File |
| --- | --- | --- |
| `Graduate(adminUserId)` | Sets status to `Graduated`; records graduation timestamp and acting admin. | `Domain/Entities/StudentProfile.cs` |
| `Deactivate(adminUserId, reason)` | Sets status to `Inactive`; blocks login for the student. | `Domain/Entities/StudentProfile.cs` |
| `Reactivate(adminUserId)` | Restores status to `Active` from `Inactive`. | `Domain/Entities/StudentProfile.cs` |

### Domain — User new methods
| Function | Description | File |
| --- | --- | --- |
| `RecordFailedLoginAttempt()` | Increments `FailedLoginCount`; locks account (15-min window) after 5 consecutive failures. | `Domain/Entities/User.cs` |
| `UnlockAccount()` | Resets `FailedLoginCount` to 0 and clears `LockoutEnd`. | `Domain/Entities/User.cs` |
| `IsCurrentlyLockedOut()` | Returns true if `LockoutEnd` is set and has not yet elapsed. | `Domain/Entities/User.cs` |

### Domain — AdminChangeRequest entity
| Function | Description | File |
| --- | --- | --- |
| `AdminChangeRequest(studentProfileId, requestedByUserId, field, oldValue, newValue)` | Constructor; creates a pending change request for a protected student field. | `Domain/Entities/AdminChangeRequest.cs` |
| `Approve(adminUserId)` | Sets status to `Approved`; applies the requested field value to the student profile. | `Domain/Entities/AdminChangeRequest.cs` |
| `Reject(adminUserId, remarks)` | Sets status to `Rejected` with rejection remarks. | `Domain/Entities/AdminChangeRequest.cs` |

### Domain — TeacherModificationRequest entity
| Function | Description | File |
| --- | --- | --- |
| `TeacherModificationRequest(teacherUserId, field, oldValue, newValue)` | Constructor; creates a pending modification request for a teacher-editable field. | `Domain/Entities/TeacherModificationRequest.cs` |

### Domain — PaymentReceipt entity
| Function | Description | File |
| --- | --- | --- |
| `PaymentReceipt(studentProfileId, createdByUserId, amount, description, dueDate)` | Constructor; creates a new fee receipt in `Pending` status. | `Domain/Entities/PaymentReceipt.cs` |
| `SubmitProof(proofFilePath)` | Student action — attaches proof of payment file path; sets status to `ProofSubmitted`. | `Domain/Entities/PaymentReceipt.cs` |
| `Confirm(financeUserId)` | Finance action — marks receipt as `Paid`; records confirmation timestamp. | `Domain/Entities/PaymentReceipt.cs` |
| `Cancel(cancelledByUserId, reason)` | Cancels the receipt; status set to `Cancelled`. | `Domain/Entities/PaymentReceipt.cs` |

### Infrastructure — StudentLifecycleRepository
| Function | Description | File |
| --- | --- | --- |
| `GetStudentByIdAsync(studentProfileId, ct)` | Returns StudentProfile with User navigation. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetStudentsByDepartmentAsync(departmentId, status, ct)` | Lists students by department with optional status filter. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetFinalSemesterStudentsAsync(departmentId, ct)` | Returns students in their final semester, eligible for graduation. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GraduateAsync(studentProfileId, adminUserId, ct)` | Persists graduation; calls `StudentProfile.Graduate`. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `DeactivateAsync(studentProfileId, adminUserId, reason, ct)` | Persists deactivation. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `ReactivateAsync(studentProfileId, adminUserId, ct)` | Persists reactivation. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `TransferDepartmentAsync(studentProfileId, newDeptId, newProgramId, newSemester, ct)` | Updates student department, program, and semester. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `CreateChangeRequestAsync(request, ct)` | Persists a new `AdminChangeRequest`. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetChangeRequestByIdAsync(id, ct)` | Returns a change request by ID. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetChangeRequestsForStudentAsync(studentProfileId, ct)` | Returns all change requests for a student. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetPendingChangeRequestsAsync(ct)` | Returns all pending change requests across all students. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `ApproveChangeRequestAsync(id, adminUserId, ct)` | Calls `AdminChangeRequest.Approve`; persists. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `RejectChangeRequestAsync(id, adminUserId, remarks, ct)` | Calls `AdminChangeRequest.Reject`; persists. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `CreateTeacherModificationAsync(request, ct)` | Persists a new `TeacherModificationRequest`. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetTeacherModificationByIdAsync(id, ct)` | Returns a teacher modification request by ID. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetTeacherModificationsByTeacherAsync(teacherUserId, ct)` | Returns all modification requests by a teacher. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetPendingTeacherModificationsAsync(ct)` | Returns all pending teacher modification requests. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `ApproveTeacherModificationAsync(id, adminUserId, ct)` | Approves a teacher modification request. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `RejectTeacherModificationAsync(id, adminUserId, remarks, ct)` | Rejects a teacher modification request. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `CreatePaymentReceiptAsync(receipt, ct)` | Persists a new `PaymentReceipt`. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetPaymentReceiptByIdAsync(id, ct)` | Returns a payment receipt by ID. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetPaymentReceiptsForStudentAsync(studentProfileId, ct)` | Returns all receipts for a student. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetPaymentReceiptsByStatusAsync(status, ct)` | Returns all receipts matching a given status. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `SubmitProofAsync(id, proofFilePath, ct)` | Student submits payment proof; calls `PaymentReceipt.SubmitProof`. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `ConfirmPaymentAsync(id, financeUserId, ct)` | Finance confirms payment; calls `PaymentReceipt.Confirm`. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `CancelReceiptAsync(id, cancelledByUserId, reason, ct)` | Cancels a receipt; calls `PaymentReceipt.Cancel`. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetAllReceiptsAsync(ct)` | Returns all payment receipts ordered by CreatedAt desc (admin view). Added Phase 7. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `GetStudentProfileByUserIdAsync(userId, ct)` | Looks up a student profile by the application user ID. Added Phase 7. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `AddRegistrationNumberAsync(registrationNumber, ct)` | Adds a single registration number to the whitelist. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `BulkAddRegistrationNumbersAsync(numbers, ct)` | Adds a list of registration numbers from CSV import. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `RegistrationNumberExistsAsync(registrationNumber, ct)` | Returns true if a registration number is in the whitelist. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |
| `AccountExistsForRegistrationAsync(registrationNumber, ct)` | Returns true if an account already exists for this registration number. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |

### Infrastructure — UserRepository (extended)
| Function | Description | File |
| --- | --- | --- |
| `GetLockedAccountsAsync(ct)` | Returns all users with active lockouts (`LockoutEnd > UtcNow`). | `Infrastructure/Repositories/UserRepository.cs` |

### Application — StudentLifecycleService
| Function | Description | File |
| --- | --- | --- |
| `GetStudentAsync(id, ct)` | Returns a student profile detail DTO. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetStudentsByDepartmentAsync(deptId, status, ct)` | Returns list of students by department. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetFinalSemesterStudentsAsync(deptId, ct)` | Returns eligible-for-graduation students. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GraduateStudentAsync(id, adminUserId, ct)` | Graduates the student. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `DeactivateStudentAsync(id, adminUserId, reason, ct)` | Deactivates a student account. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `ReactivateStudentAsync(id, adminUserId, ct)` | Reactivates a student account. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `TransferStudentAsync(id, newDeptId, newProgramId, newSemester, ct)` | Transfers student to new department/program/semester. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetChangeRequestsAsync(studentId, ct)` | Returns all change requests for a student. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetTeacherModificationsAsync(teacherId, ct)` | Returns all modification requests by a teacher. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `CreatePaymentReceiptAsync(request, ct)` | Finance creates a new fee receipt. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetReceiptsForStudentAsync(studentId, ct)` | Returns all receipts for a student. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetReceiptsByStatusAsync(status, ct)` | Returns receipts by status (Pending/ProofSubmitted/Paid/Cancelled). | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetReceiptAsync(id, ct)` | Returns a single receipt. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `SubmitPaymentProofAsync(id, proofFilePath, ct)` | Student submits proof of payment. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetFeeStatusAsync(studentId, ct)` | Returns outstanding fee status (any unpaid receipts). | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `GetReceiptsByUserAsync(userId, ct)` | Returns receipts for a student by user ID (JWT-based). Added Phase 7. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `ImportRegistrationCsvAsync(csv, ct)` | Parses and bulk-imports registration numbers from CSV string. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `AddRegistrationNumberAsync(number, ct)` | Adds single registration number to whitelist. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `ValidateSignupRegistrationAsync(number, ct)` | Validates number exists in whitelist and has no existing account. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `MapToStudentSummary(profile)` | Maps StudentProfile to StudentSummaryDto. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `MapToStudentDetail(profile)` | Maps StudentProfile + User to StudentDetailDto. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `MapToChangeRequestDto(cr)` | Maps AdminChangeRequest to ChangeRequestDto. | `Application/StudentLifecycle/StudentLifecycleService.cs` |
| `MapToPaymentReceiptDto(receipt)` | Maps PaymentReceipt to PaymentReceiptDto. | `Application/StudentLifecycle/StudentLifecycleService.cs` |

### Application — AccountSecurityService
| Function | Description | File |
| --- | --- | --- |
| `GetLockoutStatusAsync(userId, ct)` | Returns current lockout status and remaining time for a user. | `Application/AccountSecurity/AccountSecurityService.cs` |
| `UnlockAccountAsync(userId, adminUserId, ct)` | Unlocks a user account; validates admin privilege rules. | `Application/AccountSecurity/AccountSecurityService.cs` |
| `ResetPasswordAsync(userId, adminUserId, newPassword, ct)` | Resets a user's password; enforces password history policy. | `Application/AccountSecurity/AccountSecurityService.cs` |

### Application — CsvRegistrationImportService
| Function | Description | File |
| --- | --- | --- |
| `ParseAsync(csvContent, ct)` | Parses a CSV string; extracts and validates registration number rows; returns result with errors list. | `Application/Import/CsvRegistrationImportService.cs` |
| `ImportAsync(numbers, ct)` | Bulk-adds valid registration numbers; skips duplicates; returns import summary. | `Application/Import/CsvRegistrationImportService.cs` |

### API — StudentLifecycleController
| Function | Description | File |
| --- | --- | --- |
| `GetStudents(deptId, status, ct)` | GET /api/v1/students — Lists students by dept/status. Admin+. | `API/Controllers/StudentLifecycleController.cs` |
| `GetStudent(id, ct)` | GET /api/v1/students/{id} — Returns student detail. Admin+. | `API/Controllers/StudentLifecycleController.cs` |
| `GetFinalSemester(deptId, ct)` | GET /api/v1/students/final-semester — Graduates-eligible list. Admin. | `API/Controllers/StudentLifecycleController.cs` |
| `Graduate(id, ct)` | POST /api/v1/students/{id}/graduate — Marks student graduated. Admin. | `API/Controllers/StudentLifecycleController.cs` |
| `Deactivate(id, request, ct)` | POST /api/v1/students/{id}/deactivate — Deactivates student. Admin. | `API/Controllers/StudentLifecycleController.cs` |
| `Reactivate(id, ct)` | POST /api/v1/students/{id}/reactivate — Reactivates student. Admin. | `API/Controllers/StudentLifecycleController.cs` |
| `Transfer(id, request, ct)` | POST /api/v1/students/{id}/transfer — Transfers to new dept/program. Admin. | `API/Controllers/StudentLifecycleController.cs` |

### API — AdminChangeRequestController
| Function | Description | File |
| --- | --- | --- |
| `GetPending(ct)` | GET /api/v1/admin-change-requests/pending — All pending requests. Admin. | `API/Controllers/AdminChangeRequestController.cs` |
| `GetForStudent(studentId, ct)` | GET /api/v1/admin-change-requests/student/{id} — Requests for student. Admin. | `API/Controllers/AdminChangeRequestController.cs` |
| `Approve(id, ct)` | POST /api/v1/admin-change-requests/{id}/approve — Approves request. Admin. | `API/Controllers/AdminChangeRequestController.cs` |

### API — TeacherModificationController
| Function | Description | File |
| --- | --- | --- |
| `GetByTeacher(teacherId, ct)` | GET /api/v1/teacher-modifications/teacher/{id} — Requests by teacher. Admin+. | `API/Controllers/TeacherModificationController.cs` |

### API — PaymentReceiptController
| Function | Description | File |
| --- | --- | --- |
| `GetByStatus(status, ct)` | GET /api/v1/payment-receipts/by-status — Receipts by status. Finance. | `API/Controllers/PaymentReceiptController.cs` |
| `GetReceipt(id, ct)` | GET /api/v1/payment-receipts/{id} — Single receipt. | `API/Controllers/PaymentReceiptController.cs` |
| `SubmitProof(id, file, ct)` | POST /api/v1/payment-receipts/{id}/proof — Student uploads proof; validated file upload. | `API/Controllers/PaymentReceiptController.cs` |
| `Confirm(id, ct)` | POST /api/v1/payment-receipts/{id}/confirm — Finance confirms. Finance. | `API/Controllers/PaymentReceiptController.cs` |
| `Cancel(id, request, ct)` | POST /api/v1/payment-receipts/{id}/cancel — Cancels receipt. Finance/Admin. | `API/Controllers/PaymentReceiptController.cs` |
| `GetFeeStatus(studentId, ct)` | GET /api/v1/payment-receipts/fee-status/{id} — Outstanding fee flag. | `API/Controllers/PaymentReceiptController.cs` |

### API — RegistrationImportController
| Function | Description | File |
| --- | --- | --- |
| `ImportCsv(file, ct)` | POST /api/v1/registration-import/csv — Imports CSV of registration numbers. Admin. | `API/Controllers/RegistrationImportController.cs` |
| `AddSingle(request, ct)` | POST /api/v1/registration-import/single — Adds one registration number. Admin. | `API/Controllers/RegistrationImportController.cs` |

### API — AccountSecurityController
| Function | Description | File |
| --- | --- | --- |
| `GetLocked(ct)` | GET /api/v1/account-security/locked — Returns all locked accounts. Admin+. | `API/Controllers/AccountSecurityController.cs` |
| `GetLockoutStatus(userId, ct)` | GET /api/v1/account-security/{userId}/lockout-status — Status for user. Admin+. | `API/Controllers/AccountSecurityController.cs` |
| `Unlock(userId, ct)` | POST /api/v1/account-security/{userId}/unlock — Unlocks an account. Admin/SuperAdmin; role rules enforced. | `API/Controllers/AccountSecurityController.cs` |
| `ResetPassword(userId, request, ct)` | POST /api/v1/account-security/{userId}/reset-password — Admin resets password. Admin+. | `API/Controllers/AccountSecurityController.cs` |

  ---

## Phase 9: Dashboard, Navigation & System Settings (Sprints 17–18)

### Domain — Timetable aggregate
| Function | Description | File |
| --- | --- | --- |
| `Timetable(departmentId, semesterId, name)` | Constructor; creates a draft timetable for a dept/semester. | `Domain/Entities/Timetable.cs` |
| `TimetableEntry(timetableId, courseOfferingId, roomId, dayOfWeek, startTime, endTime)` | Constructor; adds a single slot to a timetable. | `Domain/Entities/TimetableEntry.cs` |

### Domain — Settings entities
| Function | Description | File |
| --- | --- | --- |
| `ReportDefinition(key, name, purpose)` | Constructor; creates a report definition record. | `Domain/Entities/ReportDefinition.cs` |
| `ReportRoleAssignment(reportKey, roleName, isAllowed)` | Constructor; grants or denies a role access to a report. | `Domain/Entities/ReportRoleAssignment.cs` |
| `ModuleRoleAssignment(moduleKey, roleName, isAllowed)` | Constructor; controls per-role module visibility. | `Domain/Entities/ModuleRoleAssignment.cs` |
| `SidebarMenuItem(key, name, purpose, displayOrder, parentId)` | Constructor; creates a navigable sidebar entry. | `Domain/Entities/SidebarMenuItem.cs` |
| `SidebarMenuRoleAccess(sidebarMenuItemId, roleName, isAllowed)` | Constructor; per-role access record for a menu item. | `Domain/Entities/SidebarMenuRoleAccess.cs` |
| `SetAllowed(isAllowed)` | Updates the `IsAllowed` flag for this role-access record. | `Domain/Entities/SidebarMenuRoleAccess.cs` |

### Infrastructure — TimetableRepository
| Function | Description | File |
| --- | --- | --- |
| `GetByDepartmentAsync(deptId, semesterId, ct)` | Lists timetables for a department and semester. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `CreateAsync(timetable, ct)` | Persists a new timetable. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `UpdateAsync(timetable, ct)` | Persists timetable metadata changes. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `DeleteAsync(id, ct)` | Deletes a timetable and its entries. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `PublishAsync(id, ct)` | Calls `Timetable.Publish`; persists. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `UnpublishAsync(id, ct)` | Calls `Timetable.Unpublish`; persists. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `AddEntryAsync(entry, ct)` | Persists a new `TimetableEntry`. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `UpdateEntryAsync(entry, ct)` | Updates a timetable entry (room, time, day). | `Infrastructure/Repositories/TimetableRepository.cs` |
| `DeleteEntryAsync(entryId, ct)` | Removes a single timetable slot. | `Infrastructure/Repositories/TimetableRepository.cs` |
| `ExportToExcelAsync(id, ct)` | Returns Excel byte array for a timetable (ClosedXML). | `Infrastructure/Repositories/TimetableRepository.cs` |
| `ExportToPdfAsync(id, ct)` | Returns PDF byte array for a timetable (QuestPDF). | `Infrastructure/Repositories/TimetableRepository.cs` |

### Infrastructure — BuildingRoomRepository
| Function | Description | File |
| --- | --- | --- |
| `GetBuildingAsync(id, ct)` | Returns a single building with rooms. | `Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `CreateBuildingAsync(building, ct)` | Creates a new building. | `Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `UpdateBuildingAsync(building, ct)` | Updates building name. | `Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `DeleteBuildingAsync(id, ct)` | Deletes a building (and its rooms if empty). | `Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `CreateRoomAsync(room, ct)` | Creates a room inside a building. | `Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `UpdateRoomAsync(room, ct)` | Updates room details (name, capacity). | `Infrastructure/Repositories/BuildingRoomRepository.cs` |
| `DeleteRoomAsync(id, ct)` | Deletes a room. | `Infrastructure/Repositories/BuildingRoomRepository.cs` |

### Infrastructure — SettingsRepository (sidebar & settings methods)
| Function | Description | File |
| --- | --- | --- |
| `GetSidebarMenusAsync(ct)` | Returns all top-level sidebar items with their `SidebarMenuRoleAccess` collection. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `GetSubMenusAsync(parentId, ct)` | Returns sub-menu items for a given parent ID. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `GetSidebarMenuByIdAsync(id, ct)` | Returns a single sidebar menu item with role access. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `GetVisibleSidebarMenusForRoleAsync(roleName, ct)` | Returns sidebar items where `IsActive=true` and the role's `IsAllowed=true`; SuperAdmin bypasses filter. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `SetSidebarMenuRolesAsync(menuId, roleAssignments, ct)` | Replaces all `SidebarMenuRoleAccess` rows for a menu item. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `SetSidebarMenuStatusAsync(menuId, isActive, ct)` | Sets `IsActive` on a menu item; throws `DomainException` if `IsSystemMenu=true`. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `GetReportDefinitionsAsync(ct)` | Returns all report definitions with role assignments. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `SetReportRolesAsync(reportKey, roleAssignments, ct)` | Replaces role assignments for a report. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `GetModuleRolesAsync(moduleKey, ct)` | Returns role assignments for a module. | `Infrastructure/Repositories/SettingsRepository.cs` |
| `SetModuleRolesAsync(moduleKey, roleAssignments, ct)` | Replaces role assignments for a module. | `Infrastructure/Repositories/SettingsRepository.cs` |

### Infrastructure — TimetableExcelExporter / TimetablePdfExporter
| Function | Description | File |
| --- | --- | --- |
| `ExportAsync(timetable, ct)` | Generates a ClosedXML Excel workbook for a timetable; returns byte array. | `Infrastructure/Exporters/TimetableExcelExporter.cs` |

### Infrastructure — DatabaseSeeder (extended)
| Function | Description | File |
| --- | --- | --- |
| `SeedSidebarMenusAsync(context, ct)` | Seeds default 11 sidebar menu items and their per-role `SidebarMenuRoleAccess` rows on first run. | `Infrastructure/Seeding/DatabaseSeeder.cs` |

### Application — TimetableService
| Function | Description | File |
| --- | --- | --- |
| `GetTimetablesAsync(deptId, semesterId, ct)` | Returns timetables for a dept/semester. | `Application/Timetable/TimetableService.cs` |
| `GetTimetableAsync(id, ct)` | Returns full timetable detail with entries. | `Application/Timetable/TimetableService.cs` |
| `CreateTimetableAsync(request, ct)` | Creates a draft timetable. Admin. | `Application/Timetable/TimetableService.cs` |
| `UpdateTimetableAsync(id, request, ct)` | Updates timetable metadata. Admin. | `Application/Timetable/TimetableService.cs` |
| `DeleteTimetableAsync(id, ct)` | Deletes a timetable. Admin. | `Application/Timetable/TimetableService.cs` |
| `AddEntryAsync(id, request, ct)` | Adds a slot to a timetable. Admin. | `Application/Timetable/TimetableService.cs` |
| `UpdateEntryAsync(entryId, request, ct)` | Updates a timetable slot. Admin. | `Application/Timetable/TimetableService.cs` |
| `ExportExcelAsync(id, ct)` | Returns Excel export for a timetable. | `Application/Timetable/TimetableService.cs` |
| `ExportPdfAsync(id, ct)` | Returns PDF export for a timetable. | `Application/Timetable/TimetableService.cs` |

### Application — BuildingRoomService
| Function | Description | File |
| --- | --- | --- |
| `CreateBuildingAsync(request, ct)` | Creates a new building. | `Application/BuildingRoom/BuildingRoomService.cs` |
| `UpdateBuildingAsync(id, request, ct)` | Updates building name. | `Application/BuildingRoom/BuildingRoomService.cs` |
| `CreateRoomAsync(buildingId, request, ct)` | Creates a room. | `Application/BuildingRoom/BuildingRoomService.cs` |
| `UpdateRoomAsync(roomId, request, ct)` | Updates room details. | `Application/BuildingRoom/BuildingRoomService.cs` |
| `DeleteRoomAsync(roomId, ct)` | Deletes a room. | `Application/BuildingRoom/BuildingRoomService.cs` |

### Application — ReportSettingsService
| Function | Description | File |
| --- | --- | --- |
| `GetAllReportsAsync(ct)` | Returns all report definitions with role assignments. | `Application/Settings/ReportSettingsService.cs` |
| `GetReportAsync(key, ct)` | Returns a single report definition. | `Application/Settings/ReportSettingsService.cs` |
| `SetReportRolesAsync(key, roleAssignments, ct)` | Updates role access for a report. SuperAdmin only. | `Application/Settings/ReportSettingsService.cs` |
| `ExportReportsExcelAsync(ct)` | Exports report definitions to Excel. | `Application/Settings/ReportSettingsService.cs` |
| `ExportReportsPdfAsync(ct)` | Exports report definitions to PDF. | `Application/Settings/ReportSettingsService.cs` |
| `GetActiveReportKeysForRoleAsync(roleName, ct)` | Returns report keys available for a given role. | `Application/Settings/ReportSettingsService.cs` |
| `MapToReportDto(report)` | Maps ReportDefinition to DTO. | `Application/Settings/ReportSettingsService.cs` |

### Application — ModuleRolesService
| Function | Description | File |
| --- | --- | --- |

### Application — ThemeService
| Function | Description | File |
| --- | --- | --- |
| `GetThemeAsync(userId, ct)` | Returns the user's current theme key. | `Application/Theme/ThemeService.cs` |
| `SetThemeAsync(userId, themeKey, ct)` | Persists the user's theme selection. | `Application/Theme/ThemeService.cs` |

### Application — SidebarMenuService
| Function | Description | File |
| --- | --- | --- |
| `GetTopLevelMenusAsync(ct)` | Returns all top-level sidebar menu items with role access data. | `Application/Sidebar/SidebarMenuService.cs` |
| `GetVisibleForRoleAsync(roleName, ct)` | Returns items visible to a role (active + allowed); SuperAdmin sees all. | `Application/Sidebar/SidebarMenuService.cs` |
| `SetRolesAsync(menuId, roleAssignments, ct)` | Replaces role assignments for a menu item; validates not locked system menu. | `Application/Sidebar/SidebarMenuService.cs` |
| `SetStatusAsync(menuId, isActive, ct)` | Toggles menu item status; returns 409 Conflict if `IsSystemMenu=true`. | `Application/Sidebar/SidebarMenuService.cs` |

### API — TimetableController
| Function | Description | File |
| --- | --- | --- |
| `GetAll(deptId, semesterId, ct)` | GET /api/v1/timetables — Lists timetables. | `API/Controllers/TimetableController.cs` |
| `AddEntry(id, request, ct)` | POST /api/v1/timetables/{id}/entries — Adds a slot. Admin. | `API/Controllers/TimetableController.cs` |
| `UpdateEntry(entryId, request, ct)` | PUT /api/v1/timetables/entries/{entryId} — Updates a slot. Admin. | `API/Controllers/TimetableController.cs` |
| `DeleteEntry(entryId, ct)` | DELETE /api/v1/timetables/entries/{entryId} — Removes a slot. Admin. | `API/Controllers/TimetableController.cs` |
| `ExportExcel(id, ct)` | GET /api/v1/timetables/{id}/export/excel — Excel export. | `API/Controllers/TimetableController.cs` |
| `ExportPdf(id, ct)` | GET /api/v1/timetables/{id}/export/pdf — PDF export. | `API/Controllers/TimetableController.cs` |

### API — BuildingRoomController
| Function | Description | File |
| --- | --- | --- |
| `GetBuilding(id, ct)` | GET /api/v1/buildings/{id} — Single building. | `API/Controllers/BuildingRoomController.cs` |
| `CreateBuilding(request, ct)` | POST /api/v1/buildings — Admin. | `API/Controllers/BuildingRoomController.cs` |
| `UpdateBuilding(id, request, ct)` | PUT /api/v1/buildings/{id} — Admin. | `API/Controllers/BuildingRoomController.cs` |
| `DeleteBuilding(id, ct)` | DELETE /api/v1/buildings/{id} — Admin. | `API/Controllers/BuildingRoomController.cs` |
| `CreateRoom(buildingId, request, ct)` | POST /api/v1/buildings/{id}/rooms — Admin. | `API/Controllers/BuildingRoomController.cs` |
| `UpdateRoom(roomId, request, ct)` | PUT /api/v1/buildings/rooms/{roomId} — Admin. | `API/Controllers/BuildingRoomController.cs` |
| `DeleteRoom(roomId, ct)` | DELETE /api/v1/buildings/rooms/{roomId} — Admin. | `API/Controllers/BuildingRoomController.cs` |

### API — ReportSettingsController
| Function | Description | File |
| --- | --- | --- |
| `GetReport(key, ct)` | GET /api/v1/settings/reports/{key} — Single report. SuperAdmin. | `API/Controllers/ReportSettingsController.cs` |
| `SetRoles(key, request, ct)` | PUT /api/v1/settings/reports/{key}/roles — Updates role access. SuperAdmin. | `API/Controllers/ReportSettingsController.cs` |
| `ExportExcel(ct)` | GET /api/v1/settings/reports/export/excel — Excel export. SuperAdmin. | `API/Controllers/ReportSettingsController.cs` |
| `ExportPdf(ct)` | GET /api/v1/settings/reports/export/pdf — PDF export. SuperAdmin. | `API/Controllers/ReportSettingsController.cs` |
| `GetForRole(role, ct)` | GET /api/v1/settings/reports/for-role/{role} — Active reports for a role. | `API/Controllers/ReportSettingsController.cs` |

### API — ModuleController (extended)
| Function | Description | File |
| --- | --- | --- |
| `GetModuleRoles(key, ct)` | GET /api/v1/modules/{key}/roles — Returns role assignments for a module. SuperAdmin. | `API/Controllers/ModuleController.cs` |
| `SetModuleRoles(key, request, ct)` | PUT /api/v1/modules/{key}/roles — Updates role access for a module. SuperAdmin. | `API/Controllers/ModuleController.cs` |

### API — ThemeController
| Function | Description | File |
| --- | --- | --- |
| `GetTheme(ct)` | GET /api/v1/theme — Returns current user's theme key. Authenticated. | `API/Controllers/ThemeController.cs` |
| `SetTheme(request, ct)` | PUT /api/v1/theme — Persists user's theme selection. Authenticated. | `API/Controllers/ThemeController.cs` |

### API — SidebarMenuController
| Function | Description | File |
| --- | --- | --- |
| `GetMyVisible(ct)` | GET /api/v1/sidebar-menu/my-visible — Returns visible menus for the calling user's role. Authenticated. | `API/Controllers/SidebarMenuController.cs` |
| `GetSubMenus(id, ct)` | GET /api/v1/sidebar-menu/{id}/sub-menus — Sub-menus for parent. SuperAdmin. | `API/Controllers/SidebarMenuController.cs` |
| `SetRoles(id, request, ct)` | PUT /api/v1/sidebar-menu/{id}/roles — Updates role visibility. SuperAdmin. | `API/Controllers/SidebarMenuController.cs` |
| `SetStatus(id, request, ct)` | PUT /api/v1/sidebar-menu/{id}/status — Toggles active/inactive; 409 if system menu. SuperAdmin. | `API/Controllers/SidebarMenuController.cs` |

### Web — EduApiClient (sidebar methods)
| Function | Description | File |
| --- | --- | --- |
| `GetVisibleSidebarMenusForCurrentUserAsync(ct)` | GET /api/v1/sidebar-menu/my-visible — Fetches visible menus for layout rendering. | `Web/Services/EduApiClient.cs` |
| `GetSidebarSubMenusAsync(parentId, ct)` | GET /api/v1/sidebar-menu/{id}/sub-menus — Fetches sub-menus for a parent. | `Web/Services/EduApiClient.cs` |
| `SetSidebarMenuRolesAsync(menuId, request, ct)` | PUT /api/v1/sidebar-menu/{id}/roles — Updates role visibility settings. | `Web/Services/EduApiClient.cs` |
| `SetSidebarMenuStatusAsync(menuId, request, ct)` | PUT /api/v1/sidebar-menu/{id}/status — Toggles menu item active/inactive. | `Web/Services/EduApiClient.cs` |

### Web — PortalController (sidebar settings actions)
| Function | Description | File |
| --- | --- | --- |
| `SidebarSettings(ct)` | GET /portal/settings/sidebar — Loads sidebar settings view with top-level menu table. SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `UpdateSidebarMenuRoles(id, request, ct)` | POST /portal/settings/sidebar/{id}/roles — Updates role access from form; CSRF-protected. SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `UpdateSidebarMenuStatus(id, request, ct)` | POST /portal/settings/sidebar/{id}/status — Toggles menu item status from form; CSRF-protected. SuperAdmin. | `Web/Controllers/PortalController.cs` |


  ---

## Integration Tests (tests/Tabsan.EduSphere.IntegrationTests)

### EduSphereWebFactory (Infrastructure/EduSphereWebFactory.cs)
| Function | Description | File |
| --- | --- | --- |
| `InitializeAsync()` | `IAsyncLifetime` � drops `TabsanEduSphere_IntegrationTests` DB via standalone context before factory builds; ensures clean state for every run. | `tests/.../Infrastructure/EduSphereWebFactory.cs` |
| `DisposeAsync()` | Drops test DB after all tests in the fixture complete; releases resources. | `tests/.../Infrastructure/EduSphereWebFactory.cs` |
| `BuildStandaloneContext()` | Creates a standalone `ApplicationDbContext` targeting the test connection string; used for pre-run DB drop outside the factory lifecycle. | `tests/.../Infrastructure/EduSphereWebFactory.cs` |
| `ConfigureWebHost(builder)` | Overrides connection string to test DB; removes all `IHostedService` registrations to prevent background job interference. | `tests/.../Infrastructure/EduSphereWebFactory.cs` |

### JwtTestHelper (Infrastructure/JwtTestHelper.cs)
| Function | Description | File |
| --- | --- | --- |
| `GenerateToken(role, userId, email)` | Generates a signed JWT for any system role using the same secret/issuer/audience as the API; returns Bearer token string for test HTTP client auth headers. | `tests/.../Infrastructure/JwtTestHelper.cs` |

### SidebarMenuIntegrationTests (SidebarMenuIntegrationTests.cs)
| Test | Assertion | File |
| --- | --- | --- |
| `GetVisible_SuperAdmin_ReturnsAllMenus` | SuperAdmin receives all 13 seeded menu keys via `GET my-visible`. | `tests/.../SidebarMenuIntegrationTests.cs` |
| `GetVisible_Admin_ReturnsAdminMenusOnly` | Admin receives exactly 7 keys (dashboard, timetable_admin, lookups, buildings, rooms, system_settings, theme_settings). | `tests/.../SidebarMenuIntegrationTests.cs` |
| `GetVisible_Faculty_ReturnsFacultyMenusOnly` | Faculty receives exactly 4 keys (dashboard, timetable_teacher, system_settings, theme_settings). | `tests/.../SidebarMenuIntegrationTests.cs` |
| `GetVisible_Student_ReturnsStudentMenusOnly` | Student receives exactly 4 keys (dashboard, timetable_student, system_settings, theme_settings). | `tests/.../SidebarMenuIntegrationTests.cs` |
| `SetStatus_DisableTimetableTeacher_RemovesFromFaculty_ThenRestore` | Deactivating a menu item removes it from Faculty visible; re-activating restores it. | `tests/.../SidebarMenuIntegrationTests.cs` |
| `SetRoles_DenyStudent_RemovesFromStudentVisible_ThenRestore` | Revoking Student role access removes menu from student visible; restore re-adds it. | `tests/.../SidebarMenuIntegrationTests.cs` |
| `SetStatus_SystemMenu_DeactivateAttempt_Returns409Conflict` | Attempting to deactivate a system menu returns `409 Conflict`. | `tests/.../SidebarMenuIntegrationTests.cs` |
| `GetVisible_NoToken_Returns401` | Unauthenticated request to `my-visible` returns `401 Unauthorized`. | `tests/.../SidebarMenuIntegrationTests.cs` |

  ---

## Phase 9 Web UI � Web Layer (Completed 30 April 2026)

### API � LicenseController (Phase 9 additions)
| Function | Description | File |
| --- | --- | --- |
| `Details(ct)` | GET /api/v1/license/details � Returns full license detail (status, licenseType, activatedAt, expiresAt, updatedAt, remainingDays). Roles: SuperAdmin, Admin. | `API/Controllers/LicenseController.cs` |

### API � ModuleController (Phase 9 all-settings endpoint)
| Function | Description | File |
| --- | --- | --- |
| `AllSettings(ct)` | GET /api/v1/modules/all-settings � Returns `List<ModuleSettingsDto>` with activation state + role assignments for all modules. Role: SuperAdmin. | `API/Controllers/ModuleController.cs` |

### Application � ModuleSettingsDto
| Type | Description | File |
| --- | --- | --- |
| `ModuleSettingsDto` | Record: `(Id, Key, Name, IsMandatory, IsActive, AssignedRoles)` � used by the all-settings endpoint. | `Application/DTOs/SettingsDtos.cs` |

### Infrastructure � LicenseValidationService (Phase 9 addition)
| Function | Description | File |
| --- | --- | --- |
| `GetCurrentStateAsync(ct)` | Returns the raw `LicenseState?` from the license repository; used by the details endpoint. | `Infrastructure/Licensing/LicenseValidationService.cs` |

### Infrastructure � DatabaseSeeder (Phase 9 idempotent update)
| Function | Description | File |
| --- | --- | --- |
| `SeedSidebarMenusAsync(db)` | Rewritten to upsert-by-key (fully idempotent). Adds `license_update` (SuperAdmin only) and `theme_settings` (all roles) entries. Now seeds 13 sidebar items total. | `Infrastructure/Persistence/DatabaseSeeder.cs` |

### Web � EduApiClient (Phase 9 UI methods)
| Function | Description | File |
| --- | --- | --- |
| `GetLicenseDetailsAsync(ct)` | GET /api/v1/license/details � Returns license status, type, dates, remaining days. | `Web/Services/EduApiClient.cs` |
| `UploadLicenseAsync(fileStream, fileName, ct)` | POST /api/v1/license/upload � Uploads a `.tablic` license file. | `Web/Services/EduApiClient.cs` |
| `GetCurrentThemeAsync(ct)` | GET /api/v1/theme � Returns the current user's theme key. | `Web/Services/EduApiClient.cs` |
| `SetThemeAsync(themeKey, ct)` | PUT /api/v1/theme � Persists the user's theme selection. | `Web/Services/EduApiClient.cs` |
| `CreateReportDefinitionAsync(form, ct)` | POST /api/v1/settings/reports � Creates a new report definition. | `Web/Services/EduApiClient.cs` |
| `SetReportActiveAsync(id, activate, ct)` | POST /api/v1/settings/reports/{id}/activate or /deactivate � Toggles report visibility. | `Web/Services/EduApiClient.cs` |
| `SetReportRolesAsync(id, roles, ct)` | PUT /api/v1/settings/reports/{id}/roles � Updates role access for a report. | `Web/Services/EduApiClient.cs` |
| `GetModuleSettingsAsync(ct)` | GET /api/v1/modules/all-settings � Returns all modules with activation + role data. | `Web/Services/EduApiClient.cs` |
| `SetModuleActiveAsync(key, activate, ct)` | POST /api/v1/modules/{key}/activate or /deactivate � Toggles module activation. | `Web/Services/EduApiClient.cs` |
| `SetModuleRolesAsync(key, roles, ct)` | PUT /api/v1/modules/{key}/roles � Updates role assignments for a module. | `Web/Services/EduApiClient.cs` |

### Web � PortalViewModels (Phase 9 additions)
| Type | Description | File |
| --- | --- | --- |
| `LicenseUpdatePageModel` | ViewModel for LicenseUpdate view: status, licenseType, activatedAt, expiresAt, updatedAt, remainingDays. | `Web/Models/Portal/PortalViewModels.cs` |
| `ThemeOption` | Value type: (Key, Label, PrimaryColor, SecondaryColor, AccentColor) for one theme swatch. | `Web/Models/Portal/PortalViewModels.cs` |
| `ThemeSettingsPageModel` | ViewModel for ThemeSettings view: CurrentThemeKey + list of 15 ThemeOption entries. | `Web/Models/Portal/PortalViewModels.cs` |
| `ReportDefinitionWebModel` | ViewModel for a single report row: Id, Key, Name, Purpose, IsActive, AdminAllowed, FacultyAllowed, StudentAllowed. | `Web/Models/Portal/PortalViewModels.cs` |
| `ReportSettingsPageModel` | ViewModel for ReportSettings view: list of ReportDefinitionWebModel. | `Web/Models/Portal/PortalViewModels.cs` |
| `CreateReportForm` | Form model: Name, Key, Purpose. | `Web/Models/Portal/PortalViewModels.cs` |
| `ModuleSettingsWebModel` | ViewModel for a single module row: Id, Key, Name, IsMandatory, IsActive, AdminAllowed, FacultyAllowed, StudentAllowed. | `Web/Models/Portal/PortalViewModels.cs` |
| `ModuleSettingsPageModel` | ViewModel for ModuleSettings view: list of ModuleSettingsWebModel. | `Web/Models/Portal/PortalViewModels.cs` |

### Web � PortalController (Phase 9 UI actions)
| Function | Description | File |
| --- | --- | --- |
| `LicenseUpdate(ct)` | GET /portal/settings/license-update � Loads license status + upload form. Roles: SuperAdmin, Admin. | `Web/Controllers/PortalController.cs` |
| `UploadLicense(licenseFile, ct)` | POST /portal/settings/license-upload � Uploads `.tablic` file; max 64 KB. Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `ThemeSettings(ct)` | GET /portal/settings/theme � Loads theme picker page with current selection. Authenticated. | `Web/Controllers/PortalController.cs` |
| `SetTheme(themeKey, ct)` | POST /portal/settings/set-theme � Persists theme selection via API. Authenticated. | `Web/Controllers/PortalController.cs` |
| `ReportSettings(ct)` | GET /portal/settings/reports � Loads report definitions list with role controls. Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `CreateReport(form, ct)` | POST /portal/settings/reports/create � Creates a new report definition. Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `ToggleReport(id, activate, ct)` | POST /portal/settings/reports/{id}/toggle � Activates or deactivates a report. Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `UpdateReportRoles(id, adminAllowed, facultyAllowed, studentAllowed, ct)` | POST /portal/settings/reports/{id}/roles � Updates role access for a report. Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `ModuleSettings(ct)` | GET /portal/settings/modules � Loads module settings list with activation + role controls. Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `ToggleModule(key, activate, ct)` | POST /portal/settings/modules/{key}/toggle � Activates or deactivates a module (non-mandatory only). Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `UpdateModuleRoles(key, adminAllowed, facultyAllowed, studentAllowed, ct)` | POST /portal/settings/modules/{key}/roles � Updates role assignments for a module. Role: SuperAdmin. | `Web/Controllers/PortalController.cs` |

### Web � _Layout.cshtml (Phase 9 sidebar routes)
| Addition | Description | File |
| --- | --- | --- |
| `"license_update"` route | `ResolveRoute` switch case maps to `("Portal", "LicenseUpdate")`. | `Web/Views/Shared/_Layout.cshtml` |
| `"theme_settings"` route | `ResolveRoute` switch case maps to `("Portal", "ThemeSettings")`. | `Web/Views/Shared/_Layout.cshtml` |

### Web � Views (Phase 9 new views)
| View | Description | File |
| --- | --- | --- |
| `LicenseUpdate.cshtml` | Displays license status table (Status badge, LicenseType, ActivatedAt, ExpiresAt + remaining days, UpdatedAt) and upload form (`.tablic` only, 64 KB max). SuperAdmin can upload; Admin sees read-only view. | `Web/Views/Portal/LicenseUpdate.cshtml` |
| `ThemeSettings.cshtml` | Color swatch grid (90�70 px buttons, one per theme). JS `previewTheme()` applies `data-theme` on click for live preview. Hidden form input saves selection on submit. | `Web/Views/Portal/ThemeSettings.cshtml` |
| `ReportSettings.cshtml` | Collapsible create-report form + accordion list of all report definitions. Each row has activate/deactivate toggle and role checkbox form (Admin / Faculty / Student). | `Web/Views/Portal/ReportSettings.cshtml` |
| `ModuleSettings.cshtml` | Accordion list of all modules. Each row shows Mandatory badge, activate/deactivate button (disabled for mandatory modules), and role checkbox form. | `Web/Views/Portal/ModuleSettings.cshtml` |

### CSS � site.css (Phase 9 themes)
| Addition | Description | File |
| --- | --- | --- |
| 15 CSS theme definitions | Themes: `ocean_blue`, `emerald_forest`, `sunset_orange`, `royal_purple`, `midnight_dark`, `rose_gold`, `arctic_teal`, `sand_dune`, `slate_grey`, `crimson`, `ivory_classic`, `cobalt_night`, `olive_grove`, `cosmic_violet`, plus default. Each uses `[data-theme="key"]` with 15 CSS custom property overrides. | `Web/wwwroot/css/site.css` |


  ---

## Phase 10: Security, Performance & Email Infrastructure

### Infrastructure � Argon2idPasswordHasher
| Function | Description | File |
| --- | --- | --- |

### Application � FluentValidation Validators
| Function | Description | File |
| --- | --- | --- |
| `LoginRequestValidator` | Validates Username (NotEmpty, MaxLength 100, no HTML chars) and Password (NotEmpty, MaxLength 128). | `Application/Validators/AuthValidators.cs` |
| `ChangePasswordRequestValidator` | Validates CurrentPassword (NotEmpty) and NewPassword (complexity: upper+lower+digit+special, 8-128 chars). | `Application/Validators/AuthValidators.cs` |
| `AdminResetPasswordRequestValidator` | Validates TargetUserId (NotEmpty) and NewPassword (same complexity as ChangePassword). | `Application/Validators/AuthValidators.cs` |

### Application � IEmailSender Interface
| Function | Description | File |
| --- | --- | --- |
| `SendAsync(to, subject, htmlBody, ct)` | Sends an HTML email to a single recipient. | `Application/Interfaces/IEmailSender.cs` |
| `SendAsync(to, subject, htmlBody, cc, ct)` | Sends an HTML email with optional CC recipients. | `Application/Interfaces/IEmailSender.cs` |

### Infrastructure � MailKitEmailSender
| Function | Description | File |
| --- | --- | --- |

### Infrastructure � Phase10PerformanceIndexes (EF Migration)
| Index | Description | Table |
| --- | --- | --- |
| `IX_assignments_offering_published` | Composite covering index on `(CourseOfferingId, IsPublished)` for student assignment list queries. | `assignments` |
| `IX_audit_logs_entity_occurred_at` | Composite index on `(EntityName, OccurredAt)` for entity-type audit trail pages. | `audit_logs` |

### Web � WCAG / Accessibility (_Layout.cshtml + site.css)
| Change | Description | File |
| --- | --- | --- |
| Skip-to-main link | `<a href="#main-content" class="skip-to-main visually-hidden-focusable">` at top of body for keyboard navigation. | `Web/Views/Shared/_Layout.cshtml` |
| Sidebar aria-label | `role="complementary" aria-label="Application sidebar"` on `<aside>`. | `Web/Views/Shared/_Layout.cshtml` |
| Navigation aria-label | `role="navigation" aria-label="Main navigation"` on sidebar `<nav>`. | `Web/Views/Shared/_Layout.cshtml` |
| Brand link aria-label | `aria-label="Tabsan EduSphere � go to home"` on brand anchor. | `Web/Views/Shared/_Layout.cshtml` |
| Main content landmark | `id="main-content" tabindex="-1"` on `<main>` for skip-link target. | `Web/Views/Shared/_Layout.cshtml` |
| Header role | `role="banner"` on `<header class="app-header">`. | `Web/Views/Shared/_Layout.cshtml` |
| Touch targets | `.btn, button, [type=submit/reset/button] { min-height: 44px; min-width: 44px; }` � WCAG 2.5.5. | `Web/wwwroot/css/site.css` |
| Table scroll | `.app-content table { overflow-x: auto; }` for responsive tables on small viewports. | `Web/wwwroot/css/site.css` |
| Focus ring | `:focus-visible { outline: 3px solid #0d6efd; }` for keyboard navigation visibility. | `Web/wwwroot/css/site.css` |

### Infrastructure � Phase10SecurityTables (EF Migration)
| Table | Description | File |
| --- | --- | --- |
| `password_history` | Stores per-user password hash history for last-N reuse enforcement. Columns: `Id`, `UserId`, `PasswordHash`, `CreatedAt`. Index: `IX_password_history_user_created`. | `Infrastructure/Persistence/Migrations/20260430141918_Phase10SecurityTables.cs` |
| `outbound_email_logs` | Audit log of all email send attempts (success + failure). Columns: `Id`, `ToAddress`, `Subject`, `Status`, `ErrorMessage`, `AttemptedAt`. Index: `IX_outbound_email_logs_status_attempted`. | `Infrastructure/Persistence/Migrations/20260430141918_Phase10SecurityTables.cs` |

### Infrastructure � Phase10SqlViews (EF Migration)
| View | Description | File |
| --- | --- | --- |
| `vw_student_attendance_summary` | Per-student per-offering aggregate: TotalSessions, AttendedSessions, AttendancePercentage. | `Infrastructure/Persistence/Migrations/20260430143000_Phase10SqlViews.cs` |
| `vw_student_results_summary` | Per-student published results with MarksObtained, MaxMarks, Percentage, CourseCode, CourseTitle, SemesterId. | `Infrastructure/Persistence/Migrations/20260430143000_Phase10SqlViews.cs` |
| `vw_course_enrollment_summary` | Per-offering enrollment counts: EnrolledCount, MaxEnrollment, AvailableSeats (open offerings only). | `Infrastructure/Persistence/Migrations/20260430143000_Phase10SqlViews.cs` |

### Infrastructure � Phase10StoredProcedures (EF Migration)
| Procedure | Description | File |
| --- | --- | --- |
| `sp_get_attendance_below_threshold` | Returns student-offering pairs with attendance below `@ThresholdPercent` (default 75%). Optionally scoped to `@CourseOfferingId`. | `Infrastructure/Persistence/Migrations/20260430142338_Phase10StoredProcedures.cs` |
| `sp_recalculate_student_cgpa` | Recomputes CGPA for `@StudentProfileId` from all published results (proportional 4.0 scale) and updates `student_profiles.Cgpa`. Returns new CGPA. | `Infrastructure/Persistence/Migrations/20260430142338_Phase10StoredProcedures.cs` |

### Infrastructure � PasswordHistoryRepository
| Function | Description | File |
| --- | --- | --- |
| `GetRecentAsync(userId, count, ct)` | Returns the most recent `count` password history entries for the given user, ordered by `CreatedAt` descending. | `Infrastructure/Repositories/PasswordHistoryRepository.cs` |
| `AddAsync(entry, ct)` | Queues a new `PasswordHistoryEntry` for insertion. | `Infrastructure/Repositories/PasswordHistoryRepository.cs` |

### Infrastructure � MailKitEmailSender (updated)
| Function | Description | File |
| --- | --- | --- |

### Unit Tests � PasswordHistoryTests
| Test | Assertion | File |
| --- | --- | --- |
| `NewPassword_NotInHistory_IsAllowed` | Brand-new password not found in history is allowed (no reuse flag). | `tests/UnitTests/PasswordHistoryTests.cs` |
| `NewPassword_MatchingRecentHistory_IsRejected` | Password matching any entry in the recent-5 window is flagged as reused. | `tests/UnitTests/PasswordHistoryTests.cs` |
| `NewPassword_MatchingMostRecent_IsRejected` | Reusing the immediately preceding password is rejected. | `tests/UnitTests/PasswordHistoryTests.cs` |
| `NewPassword_Beyond5History_IsAllowed` | Most-recent 5 entries are returned; password outside that window is not flagged. | `tests/UnitTests/PasswordHistoryTests.cs` |
| `NoHistory_AnyPasswordIsAllowed` | Empty history returns no entries; no false-positive reuse detection. | `tests/UnitTests/PasswordHistoryTests.cs` |
| `PasswordHistoryEntry_StoredHash_MatchesInput` | `PasswordHistoryEntry` constructor sets UserId, PasswordHash, and CreatedAt correctly. | `tests/UnitTests/PasswordHistoryTests.cs` |

### Integration Tests � AccountSecurityIntegrationTests
| Test | Assertion | File |
| --- | --- | --- |
| `GetLocked_Unauthenticated_Returns401` | `GET /api/v1/account-security/locked` without token returns 401. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `GetLocked_StudentRole_Returns403` | Student cannot access locked-accounts list � 403. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `GetLocked_AdminRole_ReturnsOk` | Admin receives 200 from locked-accounts list. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `GetLocked_SuperAdminRole_ReturnsOk` | SuperAdmin receives 200 from locked-accounts list. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `GetLocked_FreshDatabase_ReturnsEmptyList` | Fresh seeded DB has no locked accounts � returns `[]`. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `GetStatus_Unauthenticated_Returns401` | `GET /api/v1/account-security/{userId}/status` without token returns 401. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `GetStatus_NonExistentUser_Returns404` | Status for random non-existent user GUID returns 404. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `Unlock_Unauthenticated_Returns401` | `POST /api/v1/account-security/{userId}/unlock` without token returns 401. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `Unlock_StudentRole_Returns403` | Student cannot unlock accounts � 403. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `ResetPassword_Unauthenticated_Returns401` | `POST reset-password` without token returns 401. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |
| `ResetPassword_FacultyRole_Returns403` | Faculty cannot reset passwords via admin endpoint � 403. | `tests/IntegrationTests/AccountSecurityIntegrationTests.cs` |

### Application � IEmailTemplateRenderer
| Function | Description | File |
| --- | --- | --- |
| `Render(templateName, tokens)` | Renders a named HTML email template with `{{TOKEN}}` substitution. Token values are HTML-encoded before substitution to prevent XSS. Falls back to a minimal inline string if the template file is not found. | `Application/Interfaces/IEmailTemplateRenderer.cs` |

### Infrastructure � EmailTemplateRenderer
| Function | Description | File |
| --- | --- | --- |

### Infrastructure � Email Templates
| Template | Tokens | File |
| --- | --- | --- |
| `account-unlocked.html` | `{{USERNAME}}` | `Infrastructure/Email/Templates/account-unlocked.html` |
| `password-reset.html` | `{{USERNAME}}` | `Infrastructure/Email/Templates/password-reset.html` |
| `license-expiry-warning.html` | `{{EXPIRY_LABEL}}` | `Infrastructure/Email/Templates/license-expiry-warning.html` |

### CI/CD � GitHub Actions (.github/workflows/dotnet-ci.yml)
| Job / Step | Description |
|---|---|
| `build-test / Vulnerability scan` | Runs `dotnet list package --vulnerable --include-transitive` and emits a `::warning` annotation if any vulnerable packages are detected. Uploads `vuln-report.txt` as a CI artifact on every run. |
| `load-test` | Runs on push to `main` after `build-test` passes. Starts the API, installs k6, runs `tests/load/k6-baseline.js` (smoke scenario), and uploads `load-test-results.json`. |
| `lighthouse` | Runs on push to `main` after `build-test` passes. Starts the Web project, runs Lighthouse CI via `treosh/lighthouse-ci-action@v11` with `.lighthouserc.yml` thresholds (= 0.9 for Performance, Accessibility, Best Practices). |

### Load Tests � k6 (tests/load/)
| File | Description |
|---|---|
| `k6-baseline.js` | Three scenarios: **smoke** (1 VU, 30 s), **baseline** (20 VUs, 1 min), **spike** (0 ? 50 VUs ramp). Thresholds: `p(95) < 200 ms` on tagged API requests; error rate < 1 %. Endpoints exercised: `/health`, `/modules`, `/sidebar-menu/my-visible`, `/notifications/inbox`, `/departments`, `/attendance`. |

### Security � OWASP Penetration Test Checklist
| Document | Description |
|---|---|
| `Docs/Security-Pentest-Checklist.md` | Full OWASP Top 10 (2021) checklist mapped to code evidence for every control. All 10 categories verified. Covers A01 Broken Access Control, A02 Cryptographic Failures, A03 Injection, A04 Insecure Design, A05 Misconfiguration, A06 Vulnerable Components, A07 Auth Failures, A08 Integrity Failures, A09 Logging, A10 SSRF. 5 pre-production action items documented for DevOps/Security Lead sign-off. Sign-off table included. |

  ---

## Phase 11 � Result Calculation & GPA Automation

### Domain � GpaScaleRule

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GpaScaleRule(gpaValue, minScore, maxScore)` | Constructor � creates a GPA-to-score mapping rule. Validates `0 = minScore < maxScore = 100`. | `Domain/Assignments/ResultCalculation.cs` |

### Domain � ResultComponentRule

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ResultComponentRule(name, weightage)` | Constructor � creates a named assessment component with its percentage weight. Validates name non-empty and `0 < weightage = 100`. | `Domain/Assignments/ResultCalculation.cs` |

### Domain � Result (Phase 11 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SetGradePoint(decimal? gp)` | Sets the resolved GradePoint after GPA-scale lookup. Called by `ResultService` after computing subject total. | `Domain/Assignments/Result.cs` |

### Domain � StudentProfile (Phase 11 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `UpdateAcademicStanding(semesterGpa, cgpa)` | Updates `CurrentSemesterGpa` and `Cgpa` in a single call. Used after semester GPA/CGPA recalculation. | `Domain/Academic/StudentProfile.cs` |

### Domain � IResultRepository (Phase 11 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetActiveComponentRulesAsync(ct)` | Returns all active `ResultComponentRule` rows ordered by name. | `Domain/Interfaces/IResultRepository.cs` |
| `GetGpaScaleRulesAsync(ct)` | Returns all active `GpaScaleRule` rows ordered by minScore ascending. | `Domain/Interfaces/IResultRepository.cs` |
| `ReplaceCalculationRulesAsync(gpaRules, componentRules, ct)` | Deletes all existing GPA and component rules and inserts the new sets atomically. | `Domain/Interfaces/IResultRepository.cs` |
| `GetStudentProfileAsync(studentProfileId, ct)` | Returns a `StudentProfile` for GPA update operations. | `Domain/Interfaces/IResultRepository.cs` |
| `GetActiveEnrollmentsForSemesterAsync(studentProfileId, semesterId, ct)` | Returns all active enrollments for a student in a semester. | `Domain/Interfaces/IResultRepository.cs` |
| `GetActiveEnrollmentsForStudentAsync(studentProfileId, ct)` | Returns all active enrollments for a student across all semesters. | `Domain/Interfaces/IResultRepository.cs` |
| `GetSemesterIdForOfferingAsync(courseOfferingId, ct)` | Returns the `SemesterId` for a given course offering. | `Domain/Interfaces/IResultRepository.cs` |
| `GetByStudentAndOfferingAsync(studentProfileId, courseOfferingId, ct)` | Returns all result rows for one student in one offering. | `Domain/Interfaces/IResultRepository.cs` |
| `GetByStudentAndSemesterAsync(studentProfileId, semesterId, ct)` | Returns all published `Total` results for a student in a semester (for SGPA calculation). | `Domain/Interfaces/IResultRepository.cs` |
| `UpdateStudentProfile(profile)` | Marks a `StudentProfile` as modified in the EF change tracker. | `Domain/Interfaces/IResultRepository.cs` |

### Application � ResultCalculationDtos

| DTO | Purpose | Location |
| --- | --- | --- |
| `GpaScaleRuleDto` | Transfer object for a single GPA/Score mapping row (GpaValue, MinScore, MaxScore). | `Application/DTOs/Assignments/ResultCalculationDtos.cs` |
| `ResultComponentRuleDto` | Transfer object for a single component weightage row (Name, Weightage). | `Application/DTOs/Assignments/ResultCalculationDtos.cs` |
| `ResultCalculationSettingsResponse` | Wraps `IReadOnlyList<GpaScaleRuleDto>` and `IReadOnlyList<ResultComponentRuleDto>` for GET response. | `Application/DTOs/Assignments/ResultCalculationDtos.cs` |
| `SaveResultCalculationSettingsRequest` | Input for POST � carries lists of GPA rules and component rules to save. | `Application/DTOs/Assignments/ResultCalculationDtos.cs` |

### Application � IResultCalculationService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetSettingsAsync(ct)` | Returns the current active GPA scale and component weightage rules. | `Application/Interfaces/IResultCalculationService.cs` |
| `SaveSettingsAsync(request, ct)` | Validates and atomically replaces all GPA scale and component rules. | `Application/Interfaces/IResultCalculationService.cs` |

### Application � ResultCalculationService

| Function Name | Purpose | Location |
| --- | --- | --- |

### Application � ResultService (Phase 11 updates)

| Function Name | Purpose | Location |
| --- | --- | --- |

### Infrastructure � ResultCalculationConfigurations

| Entity Config | Purpose | Location |
| --- | --- | --- |
| `GpaScaleRuleConfiguration` | Maps `gpa_scale_rules` table with unique constraint on `(MinScore, MaxScore)` and check constraint `MinScore < MaxScore`. | `Infrastructure/Persistence/Configurations/ResultCalculationConfigurations.cs` |
| `ResultComponentRuleConfiguration` | Maps `result_component_rules` table with unique constraint on `Name` and check constraint `0 < Weightage <= 100`. | `Infrastructure/Persistence/Configurations/ResultCalculationConfigurations.cs` |

### API � ResultCalculationController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetSettings(ct)` | `GET /api/v1/result-calculation` � returns current GPA scale and component rules. SuperAdmin/Admin only. | `API/Controllers/ResultCalculationController.cs` |
| `SaveSettings(request, ct)` | `POST /api/v1/result-calculation` � replaces all calculation rules after validation. SuperAdmin/Admin only. | `API/Controllers/ResultCalculationController.cs` |

### Web � PortalController (Phase 11 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ResultCalculation(ct)` | `GET /Portal/ResultCalculation` � fetches and displays GPA and component rules. | `Web/Controllers/PortalController.cs` |
| `SaveResultCalculation(model, ct)` | `POST /Portal/ResultCalculation` � posts settings to API and redirects with success/error message. | `Web/Controllers/PortalController.cs` |

### Web � EduApiClient (Phase 11 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetResultCalculationSettingsAsync(ct)` | Calls `GET /api/v1/result-calculation` and deserialises settings response. | `Web/Services/EduApiClient.cs` |
| `SaveResultCalculationSettingsAsync(request, ct)` | Posts settings to `POST /api/v1/result-calculation`. | `Web/Services/EduApiClient.cs` |

### EF Migration � Phase11ResultCalculation

| Change | Details | Migration File |
| --- | --- | --- |
| `gpa_scale_rules` table | New table: Id, GpaValue (decimal 3,2), MinScore (decimal 5,2), MaxScore (decimal 5,2), IsActive. Unique IX on (MinScore, MaxScore). | `20260502134611_Phase11ResultCalculation.cs` |
| `result_component_rules` table | New table: Id, Name (nvarchar 100), Weightage (decimal 5,2), IsActive. Unique IX on Name. | `20260502134611_Phase11ResultCalculation.cs` |
| `results.GradePoint` | New nullable `decimal(3,2)` column on `results` table. | `20260502134611_Phase11ResultCalculation.cs` |
| `results.ResultType` | Altered from enum int to `nvarchar(100)` for flexible component name storage. | `20260502134611_Phase11ResultCalculation.cs` |
| `student_profiles.CurrentSemesterGpa` | New `decimal(3,2)` column. | `20260502134611_Phase11ResultCalculation.cs` |

  ---

## Phase 12 � Reporting & Document Generation

### Domain � ReportKeys

| Constant | Value | Location |
| --- | --- | --- |
| `AttendanceSummary` | `"attendance_summary"` | `Domain/Settings/ReportKeys.cs` |
| `ResultSummary` | `"result_summary"` | `Domain/Settings/ReportKeys.cs` |
| `GpaReport` | `"gpa_report"` | `Domain/Settings/ReportKeys.cs` |
| `EnrollmentSummary` | `"enrollment_summary"` | `Domain/Settings/ReportKeys.cs` |
| `SemesterResults` | `"semester_results"` | `Domain/Settings/ReportKeys.cs` |

### Application � ReportDtos

| DTO | Purpose | Location |
| --- | --- | --- |
| `ReportCatalogItemResponse` | Single report entry: Id, Key, Name, Purpose, IsActive, AllowedRoles list. | `Application/DTOs/Reports/ReportDtos.cs` |
| `ReportCatalogResponse` | Wraps `IReadOnlyList<ReportCatalogItemResponse>`. | `Application/DTOs/Reports/ReportDtos.cs` |
| `AttendanceSummaryRequest` | Filter: SemesterId?, DepartmentId?, CourseOfferingId?, StudentProfileId?. | `Application/DTOs/Reports/ReportDtos.cs` |
| `AttendanceSummaryRow` | Row: StudentProfileId, RegNo, StudentName, OfferingId, CourseCode, CourseTitle, TotalSessions, AttendedSessions, AttendancePercentage. | `Application/DTOs/Reports/ReportDtos.cs` |
| `AttendanceSummaryReportResponse` | Wraps rows list + TotalStudents + GeneratedAt. | `Application/DTOs/Reports/ReportDtos.cs` |
| `ResultSummaryRequest` | Filter: SemesterId?, DepartmentId?, CourseOfferingId?, StudentProfileId?. | `Application/DTOs/Reports/ReportDtos.cs` |
| `ResultSummaryRow` | Row: StudentProfileId, RegNo, StudentName, CourseCode, CourseTitle, ResultType, MarksObtained, MaxMarks, Percentage, PublishedAt?. | `Application/DTOs/Reports/ReportDtos.cs` |
| `ResultSummaryReportResponse` | Wraps rows + TotalRecords + GeneratedAt. | `Application/DTOs/Reports/ReportDtos.cs` |
| `GpaReportRequest` | Filter: DepartmentId?, ProgramId?. | `Application/DTOs/Reports/ReportDtos.cs` |
| `GpaReportRow` | Row: StudentProfileId, RegNo, StudentName, ProgramName, DepartmentName, CurrentSemester, Cgpa, CurrentSemesterGpa. | `Application/DTOs/Reports/ReportDtos.cs` |
| `GpaReportResponse` | Wraps rows + AverageCgpa + TotalStudents + GeneratedAt. | `Application/DTOs/Reports/ReportDtos.cs` |
| `EnrollmentSummaryRequest` | Filter: SemesterId?, DepartmentId?. | `Application/DTOs/Reports/ReportDtos.cs` |
| `EnrollmentSummaryRow` | Row: CourseOfferingId, CourseCode, CourseTitle, SemesterName, MaxEnrollment, EnrolledCount, AvailableSeats. | `Application/DTOs/Reports/ReportDtos.cs` |
| `EnrollmentSummaryReportResponse` | Wraps rows + TotalOfferings + GeneratedAt. | `Application/DTOs/Reports/ReportDtos.cs` |
| `SemesterResultsRequest` | Required SemesterId + optional DepartmentId. | `Application/DTOs/Reports/ReportDtos.cs` |
| `SemesterResultsRow` | Row: StudentProfileId, RegNo, StudentName, CourseCode, CourseTitle, ResultType, MarksObtained, MaxMarks, Percentage. | `Application/DTOs/Reports/ReportDtos.cs` |
| `SemesterResultsReportResponse` | Wraps rows + TotalStudents + GeneratedAt. | `Application/DTOs/Reports/ReportDtos.cs` |

### Domain � IReportRepository

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAttendanceDataAsync(semesterId?, courseOfferingId?, studentProfileId?, ct)` | Queries `attendance_records` with joins to student profile, user, course offering, course, and semester. | `Domain/Interfaces/IReportRepository.cs` |
| `GetResultDataAsync(semesterId?, courseOfferingId?, studentProfileId?, ct)` | Queries published `results` with joins. | `Domain/Interfaces/IReportRepository.cs` |
| `GetGpaDataAsync(departmentId?, programId?, ct)` | Queries `student_profiles` with user, academic program, and department joins. | `Domain/Interfaces/IReportRepository.cs` |
| `GetEnrollmentDataAsync(semesterId?, departmentId?, ct)` | Queries `course_offerings` with course, semester, department, and enrollment count. | `Domain/Interfaces/IReportRepository.cs` |
| `GetSemesterResultDataAsync(semesterId, departmentId?, ct)` | Queries published `results` for a specific semester with optional department filter. | `Domain/Interfaces/IReportRepository.cs` |

### Application � IReportService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetCatalogAsync(roleName, ct)` | Returns the report catalog for the calling role. | `Application/Interfaces/IReportService.cs` |
| `GetAttendanceSummaryAsync(request, ct)` | Returns `AttendanceSummaryReportResponse` for the given filters. | `Application/Interfaces/IReportService.cs` |
| `GetResultSummaryAsync(request, ct)` | Returns `ResultSummaryReportResponse`. | `Application/Interfaces/IReportService.cs` |
| `GetGpaReportAsync(request, ct)` | Returns `GpaReportResponse` with per-student GPA data and average CGPA. | `Application/Interfaces/IReportService.cs` |
| `GetEnrollmentSummaryAsync(request, ct)` | Returns `EnrollmentSummaryReportResponse`. | `Application/Interfaces/IReportService.cs` |
| `GetSemesterResultsAsync(request, ct)` | Returns `SemesterResultsReportResponse` for the specified semester. | `Application/Interfaces/IReportService.cs` |
| `ExportAttendanceSummaryExcelAsync(request, ct)` | Returns a `byte[]` Excel workbook of the attendance summary report. | `Application/Interfaces/IReportService.cs` |
| `ExportResultSummaryExcelAsync(request, ct)` | Returns a `byte[]` Excel workbook of the result summary report. | `Application/Interfaces/IReportService.cs` |
| `ExportGpaReportExcelAsync(request, ct)` | Returns a `byte[]` Excel workbook of the GPA report. | `Application/Interfaces/IReportService.cs` |

### Application � ReportService

| Function Name | Purpose | Location |
| --- | --- | --- |
| `BuildExcelBytes(ws, headers, rows)` | Private � writes header row and data rows to an `IXLWorksheet`; auto-fits columns; returns workbook as `byte[]`. | `Application/Services/ReportService.cs` |

### Infrastructure � ReportRepository

| Function Name | Purpose | Location |
| --- | --- | --- |

### API � ReportController

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetCatalog(ct)` | `GET /api/v1/reports` � returns active reports for the caller's role. All authenticated roles. | `API/Controllers/ReportController.cs` |
| `GetAttendanceSummary(request, ct)` | `GET /api/v1/reports/attendance-summary` � attendance data with filter params. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `GetResultSummary(request, ct)` | `GET /api/v1/reports/result-summary` � published results with filter params. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `GetGpaReport(request, ct)` | `GET /api/v1/reports/gpa-report` � student GPA data. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `GetEnrollmentSummary(request, ct)` | `GET /api/v1/reports/enrollment-summary` � offering seat utilisation. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `GetSemesterResults(request, ct)` | `GET /api/v1/reports/semester-results` � full published results for a semester. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `ExportAttendanceSummary(request, ct)` | `GET /api/v1/reports/attendance-summary/export` � returns `.xlsx` file download. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `ExportResultSummary(request, ct)` | `GET /api/v1/reports/result-summary/export` � Excel download. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `ExportGpaReport(request, ct)` | `GET /api/v1/reports/gpa-report/export` � Excel download. Admin/Faculty. | `API/Controllers/ReportController.cs` |
| `GetCurrentUserRole()` | Private � extracts role from JWT claims. | `API/Controllers/ReportController.cs` |

### Web � PortalController (Phase 12 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ReportCenter(ct)` | `GET /Portal/ReportCenter` � fetches available report catalog and renders landing page. | `Web/Controllers/PortalController.cs` |
| `ReportAttendance(semesterId?, departmentId?, offeringId?, studentId?, ct)` | `GET /Portal/ReportAttendance` � fetches attendance summary for filters. | `Web/Controllers/PortalController.cs` |
| `ReportResults(semesterId?, departmentId?, offeringId?, studentId?, ct)` | `GET /Portal/ReportResults` � fetches result summary for filters. | `Web/Controllers/PortalController.cs` |
| `ReportGpa(departmentId?, programId?, ct)` | `GET /Portal/ReportGpa` � fetches GPA report. | `Web/Controllers/PortalController.cs` |
| `ReportEnrollment(semesterId?, departmentId?, ct)` | `GET /Portal/ReportEnrollment` � fetches enrollment summary. | `Web/Controllers/PortalController.cs` |
| `ReportSemesterResults(semesterId?, departmentId?, ct)` | `GET /Portal/ReportSemesterResults` � fetches all published results for a selected semester. SemesterId required. Admin/Faculty. | `Web/Controllers/PortalController.cs` |
| `ExportAttendanceSummary(semesterId?, departmentId?, offeringId?, studentId?, ct)` | `GET /Portal/ExportAttendanceSummary` � proxies API export and streams `.xlsx` file. Admin/Faculty. | `Web/Controllers/PortalController.cs` |
| `ExportResultSummary(semesterId?, departmentId?, offeringId?, studentId?, ct)` | `GET /Portal/ExportResultSummary` � proxies API export and streams `.xlsx` file. Admin/Faculty. | `Web/Controllers/PortalController.cs` |
| `ExportGpaReport(departmentId?, programId?, ct)` | `GET /Portal/ExportGpaReport` � proxies API export and streams `.xlsx` file. Admin/Faculty. | `Web/Controllers/PortalController.cs` |

### Web � EduApiClient (Phase 12 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetReportCatalogAsync(ct)` | Calls `GET /api/v1/reports` and deserialises catalog. | `Web/Services/EduApiClient.cs` |
| `GetAttendanceSummaryReportAsync(query, ct)` | Calls `GET /api/v1/reports/attendance-summary` with query string params. | `Web/Services/EduApiClient.cs` |
| `GetResultSummaryReportAsync(query, ct)` | Calls `GET /api/v1/reports/result-summary`. | `Web/Services/EduApiClient.cs` |
| `GetGpaReportAsync(query, ct)` | Calls `GET /api/v1/reports/gpa-report`. | `Web/Services/EduApiClient.cs` |
| `GetEnrollmentSummaryReportAsync(query, ct)` | Calls `GET /api/v1/reports/enrollment-summary`. | `Web/Services/EduApiClient.cs` |
| `GetSemesterResultsReportAsync(semesterId, departmentId?, ct)` | Calls `GET /api/v1/reports/semester-results`; returns `SemesterResultsWebModel`. | `Web/Services/EduApiClient.cs` |
| `ExportAttendanceSummaryAsync(query, ct)` | Calls export endpoint; returns `byte[]`. | `Web/Services/EduApiClient.cs` |
| `ExportResultSummaryAsync(query, ct)` | Calls export endpoint; returns `byte[]`. | `Web/Services/EduApiClient.cs` |
| `ExportGpaReportAsync(query, ct)` | Calls export endpoint; returns `byte[]`. | `Web/Services/EduApiClient.cs` |
| `GetBytesAsync(path, ct)` | Private helper � sends GET, reads response as `byte[]`; used by all export methods. | `Web/Services/EduApiClient.cs` |

### Web - Shared Layout (Phase 1 Final-Touches)

| Function Name | Purpose | Location |
| --- | --- | --- |
| Dynamic menu cache load/save block | Persists last successful `my-visible` sidebar payload in session and reuses it when menu API fails/returns empty to avoid fallback regression. | `Web/Views/Shared/_Layout.cshtml` |

### API - Portal Settings (Phase 1 Final-Touches Stage 1.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| GET /api/v1/portal-settings | Returns current portal branding DTO (university name, initials, subtitle, footer). | API/Controllers/PortalSettingsController.cs |
| POST /api/v1/portal-settings | Saves (upserts) all portal branding fields. SuperAdmin only. | API/Controllers/PortalSettingsController.cs |
| PortalBrandingService.GetAsync() | Reads all portal_settings rows and maps to PortalBrandingDto with hardcoded defaults. | Application/Services/SettingsServices.cs |
| PortalBrandingService.SaveAsync() | Upserts university_name, brand_initials, portal_subtitle, footer_text keys via ISettingsRepository. | Application/Services/SettingsServices.cs |
| ISettingsRepository.GetAllPortalSettingsAsync() | Returns all portal_settings rows as a dictionary. | Domain/Interfaces/ISettingsRepository.cs |
| ISettingsRepository.UpsertPortalSettingAsync() | Creates or updates a single portal_settings row by key. | Domain/Interfaces/ISettingsRepository.cs |

### Web - Dashboard Settings (Phase 1 Final-Touches Stage 1.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| EduApiClient.GetPortalBrandingAsync() | Calls GET /api/v1/portal-settings and maps response to PortalBrandingWebModel. | Web/Services/EduApiClient.cs |
| EduApiClient.SavePortalBrandingAsync() | POSTs portal branding form values to /api/v1/portal-settings. | Web/Services/EduApiClient.cs |
| PortalController.DashboardSettings() [GET] | Loads DashboardSettingsPageModel with current branding for display. | Web/Controllers/PortalController.cs |

  ---

## Phase 3 Stage 3.2 � Data Entry Workflows (Assignments / Attendance / Results / Quizzes / FYP)

### Web � EduApiClient write methods (Stage 3.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CreateAssignmentAsync(courseOfferingId, title, description, dueDate, maxMarks, ct)` | POST api/v1/assignment � creates a draft assignment. Returns new assignment Guid. | Web/Services/EduApiClient.cs |
| `PublishAssignmentAsync(id, ct)` | POST api/v1/assignment/{id}/publish � publishes a draft assignment. | Web/Services/EduApiClient.cs |
| `DeleteAssignmentAsync(id, ct)` | DELETE api/v1/assignment/{id} � soft-deletes an assignment. | Web/Services/EduApiClient.cs |
| `GradeSubmissionAsync(assignmentId, studentProfileId, marksAwarded, feedback, ct)` | PUT api/v1/assignment/submissions/grade � grades a student submission. | Web/Services/EduApiClient.cs |
| `BulkMarkAttendanceAsync(offeringId, date, entries, ct)` | POST api/v1/attendance/bulk � marks attendance for all enrolled students in one request. | Web/Services/EduApiClient.cs |
| `CreateResultAsync(studentProfileId, courseOfferingId, resultType, marksObtained, maxMarks, ct)` | POST api/v1/result � enters a single result record. | Web/Services/EduApiClient.cs |
| `PublishAllResultsAsync(courseOfferingId, ct)` | POST api/v1/result/publish-all?courseOfferingId={id} � publishes all draft results for an offering. | Web/Services/EduApiClient.cs |
| `CreateQuizAsync(courseOfferingId, title, instructions, timeLimitMinutes, maxAttempts, ct)` | POST api/v1/quiz � creates a draft quiz. Returns new quiz Guid. | Web/Services/EduApiClient.cs |
| `PublishQuizAsync(id, ct)` | POST api/v1/quiz/{id}/publish � publishes a draft quiz. | Web/Services/EduApiClient.cs |
| `DeleteQuizAsync(id, ct)` | DELETE api/v1/quiz/{id} � soft-deletes a quiz. | Web/Services/EduApiClient.cs |
| `ProposeFypProjectAsync(departmentId, title, description, ct)` | POST api/v1/fyp � submits a new FYP project proposal. Returns new project Guid. | Web/Services/EduApiClient.cs |
| `ApproveFypProjectAsync(id, remarks, ct)` | POST api/v1/fyp/{id}/approve � approves a proposed FYP project. | Web/Services/EduApiClient.cs |
| `RejectFypProjectAsync(id, remarks, ct)` | POST api/v1/fyp/{id}/reject � rejects a proposed FYP project with mandatory remarks. | Web/Services/EduApiClient.cs |
| `DeleteAsync(path, ct)` [private] | HTTP DELETE helper used by DeleteAssignmentAsync and DeleteQuizAsync. | Web/Services/EduApiClient.cs |

### Web � PortalController write actions (Stage 3.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CreateAssignment(offeringId, title, description, dueDate, maxMarks, ct)` | POST � creates assignment, redirects to Assignments view. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `PublishAssignment(id, offeringId, ct)` | POST � publishes a draft assignment. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `DeleteAssignment(id, offeringId, ct)` | POST � deletes an assignment. Admin. | Web/Controllers/PortalController.cs |
| `GradeSubmission(assignmentId, studentProfileId, offeringId, marksAwarded, feedback, ct)` | POST � grades a submission, redirects with selectedAssignmentId. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `BulkMarkAttendance(offeringId, date, studentIds[], statuses[], ct)` | POST � zips arrays and submits bulk attendance. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `CreateResult(studentProfileId, offeringId, resultType, marksObtained, maxMarks, promote, ct)` | POST � enters one result record; if `promote=true` (Final type only), automatically promotes the student to the next semester. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `PromoteStudentFromResult(studentProfileId, offeringId, ct)` | POST � standalone promote action triggered from the Results table row; calls `PromoteStudentAsync` on the selected student. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `PublishAllResults(offeringId, ct)` | POST � publishes all draft results. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `CreateQuiz(offeringId, title, instructions, timeLimitMinutes, maxAttempts, ct)` | POST � creates a draft quiz. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `PublishQuiz(id, offeringId, ct)` | POST � publishes a draft quiz. Faculty/Admin. | Web/Controllers/PortalController.cs |
| `DeleteQuiz(id, offeringId, ct)` | POST � deletes a quiz. Admin. | Web/Controllers/PortalController.cs |
| `ProposeFypProject(departmentId, title, description, ct)` | POST � submits FYP proposal. Student. | Web/Controllers/PortalController.cs |
| `ApproveFypProject(id, remarks, departmentId, ct)` | POST � approves FYP project. Admin. | Web/Controllers/PortalController.cs |
| `RejectFypProject(id, remarks, departmentId, ct)` | POST � rejects FYP project with remarks. Admin. | Web/Controllers/PortalController.cs |
| PortalController.DashboardSettings() [POST] | Accepts branding form (incl. LogoFile), calls UploadLogoAsync if file present, then SavePortalBrandingAsync; redirects with TempData message. | Web/Controllers/PortalController.cs |
| Branding cache block in _Layout | Loads portal branding from API; caches in session (PortalBrandingCache); uses cache on API failure. | Web/Views/Shared/_Layout.cshtml |

  ---

## Phase 1 Remediation � Batch 5 Final Push (P1-S1-02, P1-S6-01/02/03/04)

### P1-S1-02 � Authorization Regression Tests

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AuthorizationRegressionTests` (class) | xUnit integration test class covering 401/403/pass cases for Attendance, Assignment, Quiz, Result endpoints across all roles. | tests/IntegrationTests/AuthorizationRegressionTests.cs |

### P1-S6-01 � 10 Additional Themes (Total: 29 incl. Default)

New themes added to site.css and ThemeSettingsPageModel: `neon_mint`, `sakura_pink`, `golden_hour`, `deep_navy`, `lavender_mist`, `rust_canyon`, `glacier_ice`, `graphite_pro`, `spring_blossom`, `dusk_fire`.

### P1-S6-02 � Logo Upload

| Function Name | Purpose | Location |
| --- | --- | --- |
| `POST /api/v1/portal-settings/logo` | Accepts multipart file upload (=2 MB, PNG/JPG/GIF/SVG/WEBP), saves to wwwroot/portal-uploads/, returns JSON {url}. SuperAdmin only. | API/Controllers/PortalSettingsController.cs |
| `EduApiClient.UploadLogoAsync(stream, fileName, ct)` | Sends multipart POST to logo endpoint; returns relative URL string on success. | Web/Services/EduApiClient.cs |
| Sidebar brand area in _Layout | Shows `<img>` with LogoUrl if set; falls back to brand initials circle. | Web/Views/Shared/_Layout.cshtml |

### P1-S6-03 � Privacy Policy URL

| Function Name | Purpose | Location |
| --- | --- | --- |
| `PrivacyPolicyUrl` field in PortalBrandingDto/Service | Persisted as `privacy_policy_url` key in portal_settings table. | Application/Services/SettingsServices.cs |
| Footer privacy link in _Layout | Renders `<a href>` Privacy Policy link in footer if PrivacyPolicyUrl is non-empty. | Web/Views/Shared/_Layout.cshtml |

### P1-S6-04 � Text Style Options

| Function Name | Purpose | Location |
| --- | --- | --- |
| `FontFamily` / `FontSize` in PortalBrandingDto/Service | Persisted as `font_family` / `font_size` keys in portal_settings. | Application/Services/SettingsServices.cs |
| Font CSS injection in _Layout `<head>` | Injects `<style>` block with `font-family` / `font-size` overrides on `body` when set. | Web/Views/Shared/_Layout.cshtml |

  ---

## Final-Touches Phase 4 � Report Center & New Reports (Stages 4.1 + 4.2)

### API � ReportController (Phase 4 Final-Touches additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GET /api/v1/reports/semester-results` | Returns semester results report data filtered by semesterId + optional departmentId. | API/Controllers/ReportController.cs |
| `GET /api/v1/reports/semester-results/export` | Streams an Excel (.xlsx) of semester results. | API/Controllers/ReportController.cs |
| `GET /api/v1/reports/student-transcript` | Returns full academic transcript for a given studentProfileId. | API/Controllers/ReportController.cs |
| `GET /api/v1/reports/student-transcript/export` | Streams Excel transcript file. | API/Controllers/ReportController.cs |
| `GET /api/v1/reports/low-attendance` | Returns students below an attendance threshold, with optional department/course filters. | API/Controllers/ReportController.cs |
| `GET /api/v1/reports/fyp-status` | Returns FYP project list filtered by department and/or status. | API/Controllers/ReportController.cs |

### Application � IReportService (Phase 4 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetSemesterResultsAsync(semesterId, departmentId?, ct)` | Aggregates semester result rows from repository. | Application/Interfaces/IReportService.cs |
| `ExportSemesterResultsExcelAsync(semesterId, departmentId?, ct)` | Builds ClosedXML workbook for semester results. | Application/Interfaces/IReportService.cs |
| `GetStudentTranscriptAsync(studentProfileId, ct)` | Returns full academic transcript rows per student. | Application/Interfaces/IReportService.cs |
| `ExportTranscriptExcelAsync(studentProfileId, ct)` | Builds ClosedXML workbook for student transcript. | Application/Interfaces/IReportService.cs |
| `GetLowAttendanceWarningAsync(threshold, departmentId?, courseOfferingId?, ct)` | Returns students whose attendance % is below the threshold. | Application/Interfaces/IReportService.cs |
| `GetFypStatusReportAsync(departmentId?, status?, ct)` | Returns FYP projects filtered by department and status. | Application/Interfaces/IReportService.cs |

### Domain � IReportRepository (Phase 4 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetSemesterResultsDataAsync(semesterId, departmentId?, ct)` | Queries EF Core for semester result raw rows. | Domain/Interfaces/IReportRepository.cs |
| `GetTranscriptDataAsync(studentProfileId, ct)` | Queries EF Core for all result/grade records for a student. | Domain/Interfaces/IReportRepository.cs |
| `GetLowAttendanceDataAsync(threshold, departmentId?, courseOfferingId?, ct)` | Queries EF Core for students below attendance threshold. | Domain/Interfaces/IReportRepository.cs |
| `GetFypStatusDataAsync(departmentId?, status?, ct)` | Queries EF Core for FYP projects filtered by department/status. | Domain/Interfaces/IReportRepository.cs |

### Web � PortalController (Phase 4 Final-Touches additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ExportSemesterResults(semesterId, departmentId?, ct)` | GET � proxies Excel export stream from API. | Web/Controllers/PortalController.cs |
| `ExportAttendanceSummary(...)` | GET � proxies attendance summary Excel export. | Web/Controllers/PortalController.cs |
| `ExportGpaReport(...)` | GET � proxies GPA report Excel export. | Web/Controllers/PortalController.cs |
| `ReportTranscript(studentProfileId?, ct)` | GET � loads student transcript report page with student lookup. | Web/Controllers/PortalController.cs |
| `ExportStudentTranscript(studentProfileId, ct)` | GET � proxies student transcript Excel export. | Web/Controllers/PortalController.cs |
| `ReportLowAttendance(threshold, departmentId?, courseOfferingId?, ct)` | GET � loads low attendance warning report with filters. | Web/Controllers/PortalController.cs |
| `ReportFypStatus(departmentId?, status?, ct)` | GET � loads FYP status report with department/status filters. | Web/Controllers/PortalController.cs |

### Web � EduApiClient (Phase 4 Final-Touches additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetStudentTranscriptReportAsync(studentProfileId, ct)` | Calls GET /api/v1/reports/student-transcript. | Web/Services/EduApiClient.cs |
| `ExportStudentTranscriptAsync(studentProfileId, ct)` | Calls GET /api/v1/reports/student-transcript/export; returns `byte[]`. | Web/Services/EduApiClient.cs |
| `GetLowAttendanceReportAsync(threshold, departmentId?, courseOfferingId?, ct)` | Calls GET /api/v1/reports/low-attendance. | Web/Services/EduApiClient.cs |

  ---

## Final-Touches Phase 5 � Settings Pages Functional Save Actions

### Web � _Layout.cshtml (Phase 5 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| Theme load + `data-theme` block | On every page load, fetches current user's theme key from `GET /api/v1/theme` (session-cached as `CurrentThemeCache`). Sets `data-theme` attribute on `<html>` element so CSS variables apply globally. | Web/Views/Shared/_Layout.cshtml |

### Web � PortalViewModels (Phase 5 additions)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ThemeOption` (5 new entries) | Added: Steel Blue, Forest Green, Amber Gold, Warm Copper, Indigo Dusk. Total themes: 20. | Web/Models/Portal/PortalViewModels.cs |

### Web � CSS (Phase 5 additions)

| Theme Key | Description | Location |
| --- | --- | --- |
| `steel_blue` | Blue-navy sidebar, light blue card/body. | wwwroot/css/site.css |
| `forest_green` | Deep green sidebar, soft green body. | wwwroot/css/site.css |
| `amber_gold` | Brown-amber sidebar, warm yellow-tinted body. | wwwroot/css/site.css |
| `warm_copper` | Dark copper-red sidebar, soft orange body. | wwwroot/css/site.css |
| `indigo_dusk` | Deep indigo sidebar, pale violet body. | wwwroot/css/site.css |

### Web � Settings Views (Phase 5 fixes)

| View | Change | Location |
| --- | --- | --- |

  ---

## Final-Touches Phase 6 � Notifications & Analytics

### API � NotificationController (Phase 6 Stage 6.1)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `[Route]` attribute | Controller attribute | Changed from `"api/[controller]"` to `"api/v1/[controller]"` � resolves route mismatch causing all notification endpoints to return 404 for EduApiClient requests. | API/Controllers/NotificationController.cs |

### Web � PortalController (Phase 6 Stage 6.1)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `MarkNotificationRead` | Action method (new) | POST action that calls `MarkNotificationReadAsync(id, ct)` and redirects to Notifications. Enables per-notification mark-as-read from the inbox view. | Web/Controllers/PortalController.cs |

### Web � Notifications.cshtml (Phase 6 Stage 6.1)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| Per-notification mark-read button | View addition | Unread notifications now show a `<form>` posting to `MarkNotificationRead` with an anti-forgery token and the notification ID. Renders a Bootstrap Icon `bi-check2` link button. | Web/Views/Portal/Notifications.cshtml |

### Web � IEduApiClient interface (Phase 6 Stage 6.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetPerformanceAnalyticsAsync` | Interface method | Replaced `GetPerformanceAnalyticsJsonAsync` (`Task<string?>`) with typed `Task<DepartmentPerformanceReport?>`. | Web/Services/EduApiClient.cs |
| `GetAttendanceAnalyticsAsync` | Interface method | Replaced `GetAttendanceAnalyticsJsonAsync` (`Task<string?>`) with typed `Task<DepartmentAttendanceReport?>`. | Web/Services/EduApiClient.cs |
| `GetAssignmentAnalyticsAsync` | Interface method | Replaced `GetAssignmentAnalyticsJsonAsync` (`Task<string?>`) with typed `Task<AssignmentStatsReport?>`. | Web/Services/EduApiClient.cs |

### Web � EduApiClient implementation (Phase 6 Stage 6.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|

### Web � PortalViewModels (Phase 6 Stage 6.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `AnalyticsPageModel.Performance` | Property (new) | Added `DepartmentPerformanceReport?` � replaces removed `PerformanceJson` string. | Web/Models/Portal/PortalViewModels.cs |
| `AnalyticsPageModel.Attendance` | Property (new) | Added `DepartmentAttendanceReport?` � replaces removed `AttendanceJson` string. | Web/Models/Portal/PortalViewModels.cs |
| `AnalyticsPageModel.Assignments` | Property (new) | Added `AssignmentStatsReport?` � replaces removed `AssignmentJson` string. | Web/Models/Portal/PortalViewModels.cs |

### Web � PortalController (Phase 6 Stage 6.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `Analytics` | Action method | Updated to call `GetPerformanceAnalyticsAsync`, `GetAttendanceAnalyticsAsync`, `GetAssignmentAnalyticsAsync` (typed). Populates `model.Cards` with real summary values (avg marks, attendance %, assignment count). | Web/Controllers/PortalController.cs |

### Web � Analytics.cshtml (Phase 6 Stage 6.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| Performance accordion | View section | Replaced `<pre><code>` JSON dump with responsive Bootstrap 5 table showing Reg No., Name, Semester, Avg Marks, Assignments, Submitted per student. | Web/Views/Portal/Analytics.cshtml |
| Attendance accordion | View section | Replaced `<pre><code>` JSON dump with responsive table showing Student, Course, Total Classes, Attended, Percentage (colour-coded green/yellow/red by threshold). | Web/Views/Portal/Analytics.cshtml |
| Assignments accordion | View section | Replaced `<pre><code>` JSON dump with responsive table showing Title, Course, Students, Submitted, Graded, Avg Marks per assignment. | Web/Views/Portal/Analytics.cshtml |

  ---

## Final-Touches Phase 7 � Finance and Payments Module Completion

### Infrastructure � StudentLifecycleRepository (Phase 7 Stage 7.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetAllReceiptsAsync` | Method (new) | Returns all `PaymentReceipt` rows with `StudentProfile` navigation loaded � used by admin receipts view. | Infrastructure/Repositories/StudentLifecycleRepositories.cs |
| `GetStudentProfileByUserIdAsync` | Method (new) | Looks up `StudentProfile` by `UserId` � required for student's own payment receipt flow. | Infrastructure/Repositories/StudentLifecycleRepositories.cs |

### Application � IStudentLifecycleService / StudentLifecycleService (Phase 7 Stage 7.2�7.3)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetReceiptsByUserAsync` | Method (new) | Returns receipts for the calling student by JWT `userId`. Uses `GetStudentProfileByUserIdAsync` then `GetReceiptsByStudentAsync`. | Application/Interfaces/IStudentLifecycleService.cs + Service |

### API � PaymentReceiptController (Phase 7 Stage 7.2�7.3)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetAll` | Endpoint (new) `GET /api/v1/payments` | Admin retrieves all payment receipts. | API/Controllers/PaymentReceiptController.cs |
| `GetMine` | Endpoint (new) `GET /api/v1/payments/mine` | Student retrieves their own receipts via JWT. | API/Controllers/PaymentReceiptController.cs |
| `MarkSubmitted` | Endpoint (new) `POST /api/v1/payments/{id}/mark-submitted` | Student submits proof of payment text. | API/Controllers/PaymentReceiptController.cs |

### Web � EduApiClient (Phase 7)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetAllPaymentsAsync` | Method (new) | Calls `GET api/v1/payments`. | Web/Services/EduApiClient.cs |
| `GetMyPaymentsAsync` | Method (new) | Calls `GET api/v1/payments/mine`. | Web/Services/EduApiClient.cs |
| `CreatePaymentAsync` | Method (new) | Posts to `POST api/v1/payments`. | Web/Services/EduApiClient.cs |
| `ConfirmPaymentAsync` | Method (new) | Posts to `POST api/v1/payments/{id}/confirm`. | Web/Services/EduApiClient.cs |
| `CancelPaymentAsync` | Method (new) | Posts to `POST api/v1/payments/{id}/cancel`. | Web/Services/EduApiClient.cs |
| `SubmitProofAsync` | Method (new) | Posts to `POST api/v1/payments/{id}/mark-submitted`. | Web/Services/EduApiClient.cs |

### Web � PortalController (Phase 7)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `Payments` GET | Action | Branches on `IsStudent` � student loads `GetMyPaymentsAsync`; admin loads `GetAllPaymentsAsync` + student list. | Web/Controllers/PortalController.cs |
| `CreatePayment` | Action (new) | Admin creates a receipt. | Web/Controllers/PortalController.cs |
| `ConfirmPayment` | Action (new) | Admin confirms (marks Paid). | Web/Controllers/PortalController.cs |
| `CancelPayment` | Action (new) | Admin cancels a receipt. | Web/Controllers/PortalController.cs |
| `SubmitProof` | Action (new) | Student submits proof note. | Web/Controllers/PortalController.cs |

  ---

## Final-Touches Phase 8 � Enrollments Completion

### Domain � ICourseRepository (Phase 8 Stage 8.1)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetAllOfferingsAsync` | Method (new) | Returns all `CourseOffering` rows with Course + Semester navigation loaded, ordered by course code. Used for enrollment dropdown when no filter applied. | Domain/Interfaces/ICourseRepository.cs |

### Domain � IEnrollmentRepository (Phase 8 Stage 8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetByIdAsync` | Method (new) | Returns an `Enrollment` by its own ID. Used by admin-drop endpoint. | Domain/Interfaces/IEnrollmentRepository.cs |

### Domain � IEnrollmentService (Phase 8 Stage 8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `AdminDropByIdAsync` | Method (new) | Drops any active enrollment identified by enrollment ID. Returns false when not found or already dropped. | Application/Interfaces/IEnrollmentService.cs |

### Infrastructure � CourseRepository (Phase 8 Stage 8.1)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|

### Infrastructure � EnrollmentRepository (Phase 8 Stage 8.1+8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetByOfferingAsync` | Fix | Added `.ThenInclude(sp => sp.Program)` so roster response can include `ProgramName`. | Infrastructure/Repositories/AcademicSupportRepositories.cs |

### Application � EnrollmentService (Phase 8 Stage 8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|

### Application � AcademicDtos (Phase 8 Stage 8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `AdminEnrollRequest` | Record (new) | `(Guid StudentProfileId, Guid CourseOfferingId)` � request body for admin-managed enrollment. | Application/DTOs/Academic/AcademicDtos.cs |

### API � CourseController (Phase 8 Stage 8.1)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetOfferings` | Fix | Now calls `GetAllOfferingsAsync` when no filter provided (was returning empty list). Fixed field names: `CourseTitle` (not `CourseName`), `IsActive` (not `IsOpen`). | API/Controllers/CourseController.cs |

### API � EnrollmentController (Phase 8 Stage 8.1+8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetRoster` | Fix | Returns `Id, StudentName, RegistrationNumber, ProgramName, SemesterNumber` matching `RosterApiDto` in EduApiClient. | API/Controllers/EnrollmentController.cs |
| `MyCourses` | Fix | Added `CourseOfferingId` to response so student can issue a drop using `offeringId`. | API/Controllers/EnrollmentController.cs |
| `AdminEnroll` | Endpoint (new) `POST /api/v1/enrollment/admin` | Admin enrolls any student using `AdminEnrollRequest` body. Reuses `EnrollmentService.EnrollAsync`. | API/Controllers/EnrollmentController.cs |
| `AdminDrop` | Endpoint (new) `DELETE /api/v1/enrollment/admin/{enrollmentId}` | Admin drops any active enrollment by its ID. | API/Controllers/EnrollmentController.cs |

### Web � EduApiClient (Phase 8 Stage 8.1+8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetMyEnrollmentsAsync` | Method (new) | Calls `GET api/v1/enrollment/my-courses`; returns `List<MyEnrollmentItem>`. | Web/Services/EduApiClient.cs |
| `AdminEnrollStudentAsync` | Method (new) | Posts `{StudentProfileId, CourseOfferingId}` to `POST api/v1/enrollment/admin`. | Web/Services/EduApiClient.cs |
| `AdminDropEnrollmentAsync` | Method (new) | Calls `DELETE api/v1/enrollment/admin/{enrollmentId}`. | Web/Services/EduApiClient.cs |

  ---

## Phase 2 � License Concurrency + Domain Binding (2026-05-05)

### Domain � LicenseState (Phase 2 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `MaxUsers` | Property (new) | `int` � max concurrent users; 0 = unlimited. Deserialized from `.tablic` binary payload. | Domain/Licensing/LicenseState.cs |
| `ActivatedDomain` | Property (new) | `string?` (max 253) � domain where license was first activated; persisted across renewals. Enforces one-license-per-domain binding. | Domain/Licensing/LicenseState.cs |
| `IsUnlimited` | Computed property | Returns `true` when `MaxUsers <= 0`; used in concurrency checks. | Domain/Licensing/LicenseState.cs |

### Application � AuthDtos (Phase 2 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `LoginFailureReason` | Enum (new) | `InvalidCredentials`, `ConcurrencyLimitReached` � distinguishes failure types in login response. | Application/DTOs/Auth/AuthDtos.cs |
| `LoginResult` | Class (new) | Wrapper around `LoginResponse?` with `IsSuccess` flag and optional `FailureReason`; static factory methods `Ok()` and `Fail()`. | Application/DTOs/Auth/AuthDtos.cs |

### Application � IAuthService (Phase 2 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `LoginAsync` | Method signature | Return type changed from `Task<LoginResponse?>` to `Task<LoginResult>` � allows distinguishing invalid credentials from concurrency limit. | Application/Interfaces/IAuthService.cs |

### Application � AuthService (Phase 2 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|

### Application � IUserSessionRepository (Phase 2 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `CountActiveSessionsAsync` | Method (new) | Returns `Task<int>` � count of sessions where `RevokedAt == null AND ExpiresAt > DateTime.UtcNow`. | Application/Interfaces/IUserSessionRepository.cs |

### Infrastructure � UserSessionRepository (Phase 2 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|

### Infrastructure � LicenseValidationService (Phase 2 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `TablicPayload` | Inner class | Added `MaxUsers { get; set; }` and `AllowedDomain { get; set; }` properties for deserialization from `.tablic` binary. | Infrastructure/Licensing/LicenseValidationService.cs |
| `ActivateFromFileAsync` | Method signature | Added optional `requestDomain` parameter (string?): `ActivateFromFileAsync(string filePath, string? requestDomain, CancellationToken ct)`. | Infrastructure/Licensing/LicenseValidationService.cs |

### Infrastructure � LicenseStateConfiguration (Phase 2 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `MaxUsers` property fluent config | Configuration | `HasDefaultValue(0)` � new column MaxUsers INT NOT NULL DEFAULT 0. | Infrastructure/Persistence/Configurations/LicenseStateConfiguration.cs |
| `ActivatedDomain` property fluent config | Configuration | `HasMaxLength(253).IsRequired(false)` � new column ActivatedDomain NVARCHAR(253) NULL. | Infrastructure/Persistence/Configurations/LicenseStateConfiguration.cs |

### Infrastructure � Database Migration (Phase 2 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `20260505_Phase2LicenseConcurrency` | Migration (new) | Manual migration file: Up() adds MaxUsers and ActivatedDomain columns; Down() drops both for rollback. Not yet applied to database. | Infrastructure/Migrations/20260505_Phase2LicenseConcurrency.cs |

### API � AuthController (Phase 2 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `Login POST` | Endpoint | Updated response handling: if `result.IsSuccess` is false and `FailureReason == ConcurrencyLimitReached`, return `StatusCode(403, ...)`. Otherwise return `Unauthorized(...)` for invalid credentials. | API/Controllers/AuthController.cs |

### API � LicenseController (Phase 2 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `Upload POST` | Endpoint | Extract `requestDomain = Request.Host.Host` and pass to `ActivateFromFileAsync(tempFile, requestDomain, ct)`. | API/Controllers/LicenseController.cs |

### API � LicenseDomainMiddleware (Phase 2 additions � NEW FILE)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `LicenseDomainMiddleware` | Middleware (new) | Enforces domain binding at request level. If `LicenseState.ActivatedDomain` is set and doesn't match `Request.Host.Host`, rejects with HTTP 403 unless on whitelisted endpoints: `/api/v1/auth/login`, `/api/v1/license/upload`, `/api/v1/license/status`. Prevents cross-domain license reuse. | API/Middleware/LicenseDomainMiddleware.cs |

### API � Program.cs (Phase 2 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| Middleware registration | Program.cs | Added `app.UseMiddleware<LicenseDomainMiddleware>();` in pipeline before `app.UseAuthentication()`. | API/Program.cs |

  ---

**Phase 2 Completion Summary:**
  - ? P2-S1-01: Concurrent user limit via MaxUsers field and CountActiveSessionsAsync()
  - ? P2-S1-02: SuperAdmin exemption from concurrency limits
  - ? P2-S2-01: Unlimited mode via MaxUsers == 0 convention
  - ? P2-S3-01: Domain binding on first activation (ActivatedDomain captured)
  - ? P2-S3-02: Domain enforcement via LicenseDomainMiddleware at request level
  - ? P2-S3-03: Anti-tamper hardening (RSA signature + replay guard + domain binding)
  - **Build**: 0 errors, all Phase 2 code compiles successfully
  - **Next**: Apply migration (`dotnet ef database update`), Begin Phase 3 (License App with UI for MaxUsers/AllowedDomain configuration)

  ---

## Phase 3 � License App Generator Alignment + File Security (2026-05-05)

### Tools � IssuedKey model (Phase 3 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `AllowedDomain` | Property (new) | `string?` � optional domain restriction (e.g. `portal.university.edu`). Null = unrestricted. Embedded in .tablic payload. | tools/Tabsan.Lic/Models/IssuedKey.cs |

### Tools � LicDb (Phase 3 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `OnModelCreating` | EF config | Added `HasDefaultValue(0)` for MaxUsers; added `HasMaxLength(253).IsRequired(false)` for AllowedDomain. | tools/Tabsan.Lic/Data/LicDb.cs |

### Tools � LicenseBuilder (Phase 3 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `BuildAsync` | Method | Updated to embed `key.MaxUsers` and `key.AllowedDomain` in the payload; these are now AES-256-CBC encrypted and RSA-2048 signed with the rest of the payload. | tools/Tabsan.Lic/Services/LicenseBuilder.cs |

### Tools � KeyService (Phase 3 additions)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `UpdateConstraintsAsync` | Method (new) | Persists MaxUsers and AllowedDomain changes on an `IssuedKey` to the SQLite DB before generating its .tablic file. | tools/Tabsan.Lic/Services/KeyService.cs |
| `ExportCsvAsync` | Method | Updated header and row format to include `MaxUsers` and `AllowedDomain` columns in exported CSV. | tools/Tabsan.Lic/Services/KeyService.cs |

### Tools � Program.cs (Phase 3 changes)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| Startup DB migration | Code block | On launch: reads `PRAGMA table_info(issued_keys)`, adds `MaxUsers INTEGER NOT NULL DEFAULT 0` and/or `AllowedDomain TEXT NULL` columns via `ALTER TABLE` if missing. Transparently upgrades pre-Phase-3 `tabsan_lic.db` files. | tools/Tabsan.Lic/Program.cs |
| `HandleBuildTablic` | Method | Added prompts for MaxUsers (validated non-negative int, 0=unlimited) and AllowedDomain (optional, stored lowercase). Saves via `UpdateConstraintsAsync` before calling `BuildAsync`. Displays constraint summary before generating file. | tools/Tabsan.Lic/Program.cs |
| `HandleListKeys` | Method | Updated display: shows `MaxUsers` ("Unlimited" when 0) and `AllowedDomain` ("(any)" when null) per row. | tools/Tabsan.Lic/Program.cs |

### Crypto � File Security (P3-S2-01 / P3-S2-02 � Pre-existing, Verified)

| Symbol | Type | Notes | Location |
| --- | --- | --- |---|
| `LicCrypto.BuildTablicFile` | Static method | Encrypts payload (AES-256-CBC, fresh IV per file) then signs `SHA-256(IV+ciphertext)` with RSA-2048 PKCS#1. All .tablic files generated by Tabsan.Lic are tamper-evident. **P3-S2-01 fulfilled.** | tools/Tabsan.Lic/Crypto/LicCrypto.cs |
| `LicenseValidationService.ActivateFromFileAsync` | Method | Verifies magic header, RSA signature, decrypts AES, prevents replay via `ConsumedVerificationKey` table. Any payload modification invalidates the RSA signature � rejected before parsing. **P3-S2-02 fulfilled.** | src/Infrastructure/Licensing/LicenseValidationService.cs |

  ---

**Phase 3 Completion Summary:**
  - ? P3-S1-01: Generator aligned � MaxUsers and AllowedDomain flow from IssuedKey ? TablicPayload ? encrypted .tablic binary
  - ? P3-S2-01: File encryption + load-time signature validation � fully implemented (pre-existing, verified)
  - ? P3-S2-02: Modified payload rejection � RSA signing makes tampering detectable (pre-existing, verified)
  - **Build**: `Tabsan.Lic` 0 errors; full solution 0 errors
  - **Next**: Phase 4 � CSV User Import (P4-S1-01 through P4-S3-01)

  ---

## Phase 4 � CSV User Import (2026-05-06)

### Domain � User (Phase 4 additions)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `MustChangePassword` | Property (bool) | Flags a user account as requiring a password change on next login. Set to `true` on CSV import; cleared by `ClearMustChangePassword()`. | Domain/Identity/User.cs |
| `User(�, mustChangePassword)` | Constructor | Extended to accept `mustChangePassword` parameter (default `false`). | Domain/Identity/User.cs |
| `ClearMustChangePassword()` | Method | Clears the `MustChangePassword` flag after a successful forced password change. | Domain/Identity/User.cs |

### Infrastructure � UserConfiguration (Phase 4)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `MustChangePassword` EF config | Configuration | Maps `bit NOT NULL DEFAULT 0` column on the `users` table. | Infrastructure/Persistence/Configurations/UserConfiguration.cs |

### Infrastructure � Migrations (Phase 4)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `Phase4UserImport` | Migration | Adds `MustChangePassword bit NOT NULL DEFAULT 0` column to the `users` table. | Infrastructure/Migrations/20260506_Phase4UserImport.cs |

### Domain � IUserRepository (Phase 4 additions)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `AddRangeAsync(IEnumerable<User>, ct)` | Method | Bulk-inserts multiple User entities in a single batch for CSV import. | Domain/Interfaces/IUserRepository.cs |
| `GetRoleByNameAsync(string, ct)` | Method | Resolves a Role entity by name (case-insensitive) � used by the import service to look up role IDs. | Domain/Interfaces/IUserRepository.cs |

### Infrastructure � UserRepository (Phase 4 additions)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `AddRangeAsync` | Method | Calls `_db.Users.AddRangeAsync`. | Infrastructure/Repositories/UserRepository.cs |
| `GetRoleByNameAsync` | Method | `_db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == ...)` | Infrastructure/Repositories/UserRepository.cs |

### Application � IUserImportService (Phase 4 new)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `IUserImportService` | Interface | Contract for CSV user import. Defines `ImportFromCsvAsync(Stream, ct)`. | Application/Interfaces/IUserImportService.cs |
| `UserImportResult` | Record | Import summary DTO: TotalRows, Imported, Duplicates, Errors, ErrorDetails. | Application/DTOs/CsvImportDtos.cs |

### Application � UserImportService (Phase 4 new)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `UserImportService` | Service | Parses CSV (Username,Email,FullName,Role[,DepartmentId]), validates each row, sets password=username (P4-S2-01), sets MustChangePassword=true (P4-S2-02), bulk-inserts valid rows. | Application/Services/UserImportService.cs |
| `ImportFromCsvAsync` | Method | Entry point � parses stream, resolves roles, detects duplicates (intra-batch + DB), returns `UserImportResult`. | Application/Services/UserImportService.cs |

### Application � AuthService / IAuthService (Phase 4 additions)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `ForceChangePasswordAsync(Guid, string, ct)` | Method | Sets a new password for a `MustChangePassword` user without requiring the old password; clears the flag on success. | Application/Auth/AuthService.cs |
| `ForceChangePasswordRequest` | DTO | Request body for the force-change-password endpoint. | Application/DTOs/Auth/AuthDtos.cs |
| `LoginResponse.MustChangePassword` | Property | `bool` (default `false`). Set to `true` for imported users on first login. | Application/DTOs/Auth/AuthDtos.cs |

### API � UserImportController (Phase 4 new)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `UserImportController` | Controller | `POST /api/v1/user-import/csv` � SuperAdmin/Admin only. Accepts IFormFile, streams to `IUserImportService`. | API/Controllers/UserImportController.cs |

### API � AuthController (Phase 4 addition)

| Symbol | Type | Purpose | Location |
| --- | --- | --- |---|
| `ForceChangePassword` | Action | `POST /api/v1/auth/force-change-password` � Authorized. Sets new password for flagged users; clears MustChangePassword. | API/Controllers/AuthController.cs |

### P4-S3-01 � User Import Sheets folder

| File | Purpose |
|---|---|
| `User Import Sheets/user-import-template.csv` | Template CSV with header row + 1 sample row. |
| `User Import Sheets/README.md` | Column descriptions, rules, and import instructions. |

**Phase 4 Completion Summary:**
  - ? P4-S1-01: CSV user import via `POST /api/v1/user-import/csv` (SuperAdmin/Admin)
  - ? P4-S2-01: Initial password = Username for all imported users
  - ? P4-S2-02: `MustChangePassword` flag + `POST /api/v1/auth/force-change-password` endpoint
  - ? P4-S3-01: `User Import Sheets/` folder with CSV template and README
  - **Build**: 0 errors
  - **Next**: Apply migration `dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure`, then proceed to next phase
| `StudentEnrollAsync` | Method (new) | Posts `{CourseOfferingId}` to `POST api/v1/enrollment`. | Web/Services/EduApiClient.cs |
| `StudentDropEnrollmentAsync` | Method (new) | Calls `DELETE api/v1/enrollment/{offeringId}`. | Web/Services/EduApiClient.cs |
| `MyCourseApiDto` | Private class (new) | DTO for deserializing `my-courses` response. | Web/Services/EduApiClient.cs |

### Web � PortalViewModels (Phase 8 Stage 8.1+8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `MyEnrollmentItem` | Class (new) | Student's enrolled-course view model: `EnrollmentId`, `CourseOfferingId`, `CourseCode`, `CourseTitle`, `SemesterName`, `Status`, `EnrolledAt`. | Web/Models/Portal/PortalViewModels.cs |
| `EnrollmentsPageModel` | Expanded | Added `IsStudent`, `Students` (for admin enroll modal), `MyCourses` (for student view). | Web/Models/Portal/PortalViewModels.cs |

### Web � PortalController (Phase 8 Stage 8.1+8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `Enrollments` GET | Fix/Expand | Branches on `IsStudent` � student loads `MyCourses`; admin loads `Students` + roster. | Web/Controllers/PortalController.cs |
| `EnrollStudent` | Action (new) POST | Admin enrolls a student into a selected offering. | Web/Controllers/PortalController.cs |
| `AdminDropEnrollment` | Action (new) POST | Admin drops any active enrollment by `enrollmentId`. | Web/Controllers/PortalController.cs |
| `StudentEnroll` | Action (new) POST | Student self-enrolls in a course offering. | Web/Controllers/PortalController.cs |
| `StudentDropEnrollment` | Action (new) POST | Student drops their own enrollment by `courseOfferingId`. | Web/Controllers/PortalController.cs |

### Web � Enrollments.cshtml (Phase 8 Stage 8.1+8.2)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| Admin roster view | Rebuilt | Shows offering filter + "Enroll Student" button (Admin/SuperAdmin) + roster table with per-row "Drop" button. | Web/Views/Portal/Enrollments.cshtml |
| Student own-courses view | New | Shows `MyCourses` list with per-row "Drop" button (Active only) + "Enroll in Course" modal. | Web/Views/Portal/Enrollments.cshtml |


  ---

## Final-Touches Phase 14 � Helpdesk / Support Ticketing System

### Domain � SupportTicket, SupportTicketMessage (Phase 14)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `SupportTicket` | Class (new) | Support ticket entity: `SubmitterId`, `Category`, `Subject`, `Body`, `Status`, `AssignedToId`, timestamps; extends `AuditableEntity`. | Domain/Helpdesk/SupportTicket.cs |
| `SupportTicketMessage` | Class (new) | Thread reply entity: `TicketId`, `SenderId`, `Body`; child of `SupportTicket`. | Domain/Helpdesk/SupportTicketMessage.cs |
| `TicketStatus` | Enum (new) | `Open`, `InProgress`, `Resolved`, `Closed`. | Domain/Helpdesk/TicketStatus.cs |
| `TicketCategory` | Enum (new) | `Academic`, `Technical`, `Administrative`. | Domain/Helpdesk/TicketCategory.cs |
| `IHelpdeskRepository` | Interface (new) | Contract for ticket CRUD and lifecycle operations. | Domain/Interfaces/IHelpdeskRepository.cs |

### Infrastructure � HelpdeskRepository (Phase 14)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `HelpdeskRepository` | Class (new) | EF Core implementation; tables `support_tickets` + `support_ticket_messages`; dept-scoped query filters; all lifecycle query methods. | Infrastructure/Repositories/HelpdeskRepository.cs |

### Application � HelpdeskService (Phase 14)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `IHelpdeskService` | Interface (new) | Contract: create ticket, list, get, add message, assign, resolve, close, reopen. | Application/Interfaces/IHelpdeskService.cs |
| `HelpdeskService` | Class (new) | Business logic; status validation; reopen window check; notification dispatch on status change. | Application/Services/HelpdeskService.cs |
| `HelpdeskDTOs` | Records (new) | `CreateTicketRequest`, `AddMessageRequest`, `TicketListItem`, `TicketDetail`, `TicketMessageItem`. | Application/DTOs/Helpdesk/HelpdeskDTOs.cs |

### API � HelpdeskController (Phase 14)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `HelpdeskController` | Class (new) | `GET /api/v1/helpdesk`, `GET /api/v1/helpdesk/{id}`, `POST /api/v1/helpdesk`, `POST /api/v1/helpdesk/{id}/message`, `POST /api/v1/helpdesk/{id}/assign`, `POST /api/v1/helpdesk/{id}/resolve`, `POST /api/v1/helpdesk/{id}/close`, `POST /api/v1/helpdesk/{id}/reopen`. | API/Controllers/HelpdeskController.cs |

### Web � EduApiClient + PortalController + Views (Phase 14)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetTicketsAsync`, `GetTicketAsync`, `CreateTicketAsync`, `AddTicketMessageAsync`, `AssignTicketAsync`, `ResolveTicketAsync`, `CloseTicketAsync`, `ReopenTicketAsync` | Methods (new) | `IEduApiClient` interface + `EduApiClient` impl for all helpdesk endpoints. | Web/Services/EduApiClient.cs |
| `HelpdeskPageModel`, `HelpdeskDetailModel`, `HelpdeskTicketItem`, `HelpdeskMessageItem` | Classes (new) | View models for helpdesk list and detail pages. | Web/Models/Portal/PortalViewModels.cs |
| `Helpdesk`, `HelpdeskCreate`, `HelpdeskDetail`, `HelpdeskReply`, `HelpdeskAssign`, `HelpdeskResolve`, `HelpdeskClose`, `HelpdeskReopen` | Actions (new) | Portal controller actions for full ticket lifecycle. | Web/Controllers/PortalController.cs |
| `Helpdesk.cshtml` | View (new) | Ticket list with status badges and filter. | Web/Views/Portal/Helpdesk.cshtml |
| `HelpdeskCreate.cshtml` | View (new) | Ticket creation form. | Web/Views/Portal/HelpdeskCreate.cshtml |
| `HelpdeskDetail.cshtml` | View (new) | Thread view with reply form and lifecycle action buttons. | Web/Views/Portal/HelpdeskDetail.cshtml |
| `_TicketStatusBadge.cshtml` | Partial (new) | Reusable Bootstrap badge for ticket status. | Web/Views/Shared/_TicketStatusBadge.cshtml |

  ---

## Final-Touches Phase 15 � Enrollment Rules Engine

### Domain � CoursePrerequisite (Phase 15)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `CoursePrerequisite` | Class (new) | Prerequisite link entity: `CourseId`, `PrerequisiteCourseId`; unique composite index `IX_course_prerequisites_course_prereq`. | Domain/Academic/CoursePrerequisite.cs |
| `IPrerequisiteRepository` | Interface (new) | Contract: `GetByCourseAsync`, `ExistsAsync`, `AddAsync`, `RemoveAsync`. | Domain/Interfaces/IPrerequisiteRepository.cs |

### Infrastructure � PrerequisiteRepository + Migration (Phase 15)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `PrerequisiteRepository` | Class (new) | EF Core impl; table `course_prerequisites`; cascade-delete on parent course. | Infrastructure/Repositories/PrerequisiteRepository.cs |
| `Phase15_EnrollmentRules` | Migration (new) | Creates `course_prerequisites` table with FKs to `courses` and unique index. | Infrastructure/Migrations/20260507133254_Phase15_EnrollmentRules.cs |

### Application � EnrollmentService + DTOs (Phase 15)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `TryEnrollAsync` | Method (modified) | Added prerequisite pass check (loops prereqs, calls `IAssignmentResultRepository.HasPassedCourseAsync`) + timetable clash detection; returns `UnmetPrerequisites` list. | Application/Services/EnrollmentService.cs |
| `HasPassedCourseAsync` | Method (new) | Returns true if student has a passing result for the given course. | Infrastructure/Repositories/AssignmentResultRepositories.cs |

### API � PrerequisiteController (Phase 15)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `PrerequisiteController` | Class (new) | `GET /api/v1/prerequisite/{courseId}` (all authenticated), `POST /api/v1/prerequisite` (Admin/SuperAdmin), `DELETE /api/v1/prerequisite/{courseId}/{prereqCourseId}` (Admin/SuperAdmin). | API/Controllers/PrerequisiteController.cs |
| `PrerequisiteDto` | Record (new) | Response DTO: `CourseId`, `PrerequisiteCourseId`, `PrerequisiteCourseCode`, `PrerequisiteCourseTitle`. | API/Controllers/PrerequisiteController.cs |

### Web � EduApiClient + PortalController + Views (Phase 15)

| Symbol | Type | Change | Location |
| --- | --- | --- |---|
| `GetPrerequisitesAsync`, `AddPrerequisiteAsync`, `RemovePrerequisiteAsync` | Methods (new) | `IEduApiClient` interface + `EduApiClient` impl for all prerequisite endpoints. | Web/Services/EduApiClient.cs |
| `PrerequisiteWebItem`, `CoursePrerequisiteGroup`, `PrerequisitesPageModel` | Classes (new) | View models for the Prerequisites portal page. | Web/Models/Portal/PortalViewModels.cs |
| `Prerequisites`, `PrerequisiteAdd`, `PrerequisiteRemove` | Actions (new) | Portal controller actions for prerequisite management. | Web/Controllers/PortalController.cs |
| `Prerequisites.cshtml` | View (new) | Prerequisite management page (Admin/SuperAdmin): department filter, per-course prerequisite list, add/remove forms. | Web/Views/Portal/Prerequisites.cshtml |

## Final-Touches Phase 16 � Faculty Grading System

### Domain � Rubric Entities (Phase 16)

| Symbol | Type | Change | Location |
| --- | --- | --- |-----------|
| `Rubric` | Class (new) | AuditableEntity: `AssignmentId`, `Title`, `IsActive`, `Criteria`; factory `Create`, `Update`, `Deactivate`. | Domain/Assignments/Rubric.cs |
| `RubricCriterion` | Class (new) | BaseEntity: `RubricId`, `Name`, `MaxPoints`, `DisplayOrder`, `Levels`. | Domain/Assignments/Rubric.cs |
| `RubricLevel` | Class (new) | BaseEntity: `CriterionId`, `Label`, `PointsAwarded`, `DisplayOrder`. | Domain/Assignments/Rubric.cs |
| `RubricStudentGrade` | Class (new) | BaseEntity: `AssignmentSubmissionId`, `RubricCriterionId`, `RubricLevelId`, `PointsAwarded`, `GradedByUserId`. | Domain/Assignments/Rubric.cs |

### Domain Interfaces (Phase 16)

| Symbol | Type | Change | Location |
| --- | --- | --- |-----------|
| `IGradebookRepository` | Interface (new) | `GetStudentsForOfferingAsync` ? `IReadOnlyList<GradebookStudentInfo>`. | Domain/Interfaces/IGradebookRepository.cs |
| `IRubricRepository` | Interface (new) | CRUD + student grade upsert for rubric entities. | Domain/Interfaces/IRubricRepository.cs |

### Infrastructure � Configurations + Repositories (Phase 16)

| Symbol | Type | Change | Location |
| --- | --- | --- |-----------|
| `RubricConfiguration`, `RubricCriterionConfiguration`, `RubricLevelConfiguration`, `RubricStudentGradeConfiguration` | Configs (new) | EF table configs for 4 rubric tables; soft-delete filter on Rubric; unique index on (SubmissionId, CriterionId). | Infrastructure/Persistence/Configurations/RubricConfigurations.cs |
| `GradebookRepository` | Class (new) | 3-way join Enrollments?StudentProfiles?Users for gradebook grid. | Infrastructure/Repositories/GradebookRubricRepositories.cs |
| `RubricRepository` | Class (new) | `GetByAssignmentAsync` with Include Criteria?Levels; upsert student grade. | Infrastructure/Repositories/GradebookRubricRepositories.cs |
| `ApplicationDbContext` | Modified | Added 4 new DbSets: Rubrics, RubricCriteria, RubricLevels, RubricStudentGrades. | Infrastructure/Persistence/ApplicationDbContext.cs |

### Application � Services + DTOs (Phase 16)

| Symbol | Type | Change | Location |
| --- | --- | --- |-----------|
| `GradebookGridResponse`, `GradebookStudentRow`, `GradebookCellDto`, `UpsertGradebookEntryRequest`, `BulkGradePreviewResponse`, `BulkGradeConfirmRequest` | DTOs (new) | Gradebook response/request DTOs. | Application/DTOs/Assignments/GradebookDTOs.cs |
| `RubricResponse`, `CreateRubricRequest`, `RubricGradeRequest`, `RubricGradeResponse` | DTOs (new) | Rubric CRUD + grading DTOs. | Application/DTOs/Assignments/RubricDTOs.cs |
| `IGradebookService` | Interface (new) | GetGradebookAsync, UpsertEntryAsync, PublishAllAsync, CSV methods. | Application/Interfaces/IGradebookService.cs |
| `IRubricService` | Interface (new) | Rubric lifecycle + GradeSubmissionAsync. | Application/Interfaces/IRubricService.cs |
| `GradebookService` | Class (new) | Full gradebook orchestration; CSV template generation + parsing. | Application/Assignments/GradebookService.cs |
| `RubricService` | Class (new) | Rubric lifecycle and per-criterion grading. | Application/Assignments/RubricService.cs |

### API � Controllers (Phase 16)

| Symbol | Type | Change | Location |
| --- | --- | --- |-----------|
| `GradebookController` | Class (new) | `GET /{offeringId}`, `PUT entry`, `POST publish-all`, `GET template`, `POST bulk-grade`, `POST bulk-grade/confirm`. | API/Controllers/GradebookController.cs |
| `RubricController` | Class (new) | `GET assignment/{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/grade`, `GET /{id}/grade/{subId}`. | API/Controllers/RubricController.cs |

### Web � EduApiClient + PortalController + Views (Phase 16)

| Symbol | Type | Change | Location |
| --- | --- | --- |-----------|
| Phase 16 interface + impl methods (12 methods) | Methods (new) | Gradebook grid, upsert, publish-all, CSV template/upload/confirm; Rubric CRUD + grade methods. | Web/Services/EduApiClient.cs |
| Gradebook + Rubric web models (16 classes) | Classes (new) | `GradebookGridWebModel`, `BulkGradePreviewWebModel`, `RubricWebModel`, `RubricGradeWebModel`, page models, etc. | Web/Models/Portal/PortalViewModels.cs |
| `Gradebook`, `GradebookUpsertEntry`, `GradebookPublishAll`, `GradebookCsvTemplate`, `GradebookBulkUpload`, `GradebookBulkConfirm`, `RubricManage`, `RubricCreate`, `RubricDelete`, `RubricView` | Actions (new) | Portal controller actions for Phase 16. | Web/Controllers/PortalController.cs |
| `Gradebook.cshtml` | View (new) | Gradebook grid with inline cell editing (JS fetch), CSV upload/preview/confirm, publish-all. | Web/Views/Portal/Gradebook.cshtml |
| `RubricManage.cshtml` | View (new) | Rubric definition UI with dynamic criterion/level builder. | Web/Views/Portal/RubricManage.cshtml |
| `RubricView.cshtml` | View (new) | Rubric grade scorecard view. | Web/Views/Portal/RubricView.cshtml |

## Final-Touches Phase 17 � Degree Audit System

### Domain � Entities (Phase 17)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DegreeRule.Create(...)` | Static factory � creates degree rule with min credits/GPA. | Domain/Academic/DegreeRule.cs |
| `DegreeRule.Update(...)` | Updates rule requirements. | Domain/Academic/DegreeRule.cs |
| `DegreeRule.AddRequiredCourse(courseId)` | Adds required course to rule. | Domain/Academic/DegreeRule.cs |
| `DegreeRule.RemoveRequiredCourse(courseId)` | Removes required course from rule. | Domain/Academic/DegreeRule.cs |
| `Course.SetCourseType(courseType)` | Tags course as Core or Elective. | Domain/Academic/Course.cs |

### Domain Interfaces (Phase 17)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IDegreeAuditRepository` | Repo interface for DegreeRule CRUD + earned-credit queries. | Domain/Interfaces/IDegreeAuditRepository.cs |
| `CreditRow` record | Sealed record returned by `GetEarnedCreditsAsync`. | Domain/Interfaces/IDegreeAuditRepository.cs |

### Infrastructure � Configurations + Repository (Phase 17)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DegreeRuleConfiguration` | EF config � `degree_rules` table, unique index on `AcademicProgramId`. | Infrastructure/Persistence/Configurations/AcademicConfigurations.cs |
| `DegreeRuleRequiredCourseConfiguration` | EF config � `degree_rule_required_courses` table, unique index on `(DegreeRuleId, CourseId)`. | Infrastructure/Persistence/Configurations/AcademicConfigurations.cs |
| `DegreeAuditRepository` (full) | Implements `IDegreeAuditRepository`; `GetEarnedCreditsAsync` 3-way join. | Infrastructure/Repositories/DegreeAuditRepository.cs |

### Application � Service + DTOs (Phase 17)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IDegreeAuditService` | Service interface for audit, eligibility, rule CRUD, course type. | Application/Interfaces/IDegreeAuditService.cs |
| `DegreeAuditService.GetAuditAsync` | Builds full degree audit for a student. | Application/Academic/DegreeAuditService.cs |
| `DegreeAuditService.GetEligibilityListAsync` | Evaluates all students in a program for graduation eligibility. | Application/Academic/DegreeAuditService.cs |
| `DegreeAuditService.CreateRuleAsync` / `UpdateRuleAsync` / `DeleteRuleAsync` | CRUD for degree rules. | Application/Academic/DegreeAuditService.cs |
| `DegreeAuditService.SetCourseTypeAsync` | Tags a course as Core or Elective. | Application/Academic/DegreeAuditService.cs |
| Phase 17 DTOs (8 types) | `DegreeAuditResponse`, `EarnedCourseRow`, `DegreeRuleResponse`, `RequiredCourseItem`, `CreateDegreeRuleRequest`, `UpdateDegreeRuleRequest`, `SetCourseTypeRequest`, `EligibilityListItem`. | Application/DTOs/Academic/DegreeAuditDTOs.cs |

### API � Controller (Phase 17)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `DegreeAuditController` (9 endpoints) | GET my audit, GET student audit, GET eligible list, GET/POST/PUT/DELETE rule, PUT course type. | API/Controllers/DegreeAuditController.cs |

### Web � EduApiClient + PortalController + Views (Phase 17)

| Function Name | Type | Purpose | Location |
| --- | --- | --- |---|
| Phase 17 interface + impl methods (9 methods) | Methods (new) | `GetMyDegreeAuditAsync`, `GetStudentDegreeAuditAsync`, `GetEligibilityListAsync`, `GetAllDegreeRulesAsync`, `GetDegreeRuleByProgramAsync`, `CreateDegreeRuleAsync`, `UpdateDegreeRuleAsync`, `DeleteDegreeRuleAsync`, `SetCourseTypeAsync`. | Web/Services/EduApiClient.cs |
| Phase 17 web models (9 classes) | Classes (new) | `DegreeAuditWebModel`, `EarnedCourseRowWebItem`, `DegreeRuleWebModel`, `RequiredCourseWebItem`, `EligibilityListWebItem`, `CreateDegreeRuleWebRequest`, `UpdateDegreeRuleWebRequest`, `DegreeAuditPageModel`, `DegreeRulesPageModel`, `EligibilityPageModel`. | Web/Models/Portal/PortalViewModels.cs |
| `DegreeAudit`, `GraduationEligibility`, `DegreeRules`, `DegreeRuleCreate`, `DegreeRuleDelete`, `CourseSetType` | Actions (new) | Portal controller actions for Phase 17. | Web/Controllers/PortalController.cs |
| `DegreeAudit.cshtml` | View (new) | Student audit view with credit breakdown, completed courses table, eligibility badge. | Web/Views/Portal/DegreeAudit.cshtml |
| `GraduationEligibility.cshtml` | View (new) | Eligibility list with View Audit links. | Web/Views/Portal/GraduationEligibility.cshtml |
| `DegreeRules.cshtml` | View (new) | SuperAdmin rule management with create form. | Web/Views/Portal/DegreeRules.cshtml |

## Final-Touches Phase 18 � Graduation Workflow

### Domain � Entities (Phase 18)
| Entity/Type | Purpose |
|---|---|
| `GraduationApplication` | Core entity � tracks student application with multi-stage status and certificate path |
| `GraduationApplicationApproval` | Approval record per stage (Faculty/Admin/SuperAdmin) |
| `GraduationApplicationStatus` enum | Draft, PendingFaculty, PendingAdmin, PendingFinalApproval, Approved, Rejected |

### Domain Interfaces (Phase 18)
| Interface | Method Count |
|---|---|
| `IGraduationRepository` | 13 methods (CRUD + student/faculty/admin lookup helpers) |

### Application (Phase 18)
| Function/Type | Purpose |
|---|---|
| `ICertificateGenerator.GeneratePdfAsync` | Abstraction keeping QuestPDF out of Application layer |
| `GraduationService.SubmitApplicationAsync` | Student submits; guards duplicate active applications |
| `GraduationService.FacultyApproveAsync` | Faculty approve/reject + notifies student |
| `GraduationService.AdminApproveAsync` | Admin approve/reject |
| `GraduationService.FinalApproveAsync` | SuperAdmin final approve ? auto-generates PDF cert + marks student Graduated |
| `GraduationService.RejectAsync` | Any approver rejects + notifies student |
| `GraduationService.GenerateCertificateAsync` | Reads portal setting headline, generates QuestPDF, writes to wwwroot/certificates/ |
| `GraduationService.DownloadCertificateAsync` | Returns certificate bytes with ownership validation |
| Phase 18 DTOs (5 types) | `SubmitGraduationApplicationRequest`, `GraduationApprovalRequest`, `GraduationApplicationSummary`, `GraduationApplicationDetail`, `ApprovalHistoryItem` |

### Infrastructure (Phase 18)
| Component | Purpose |
|---|---|
| `CertificateGenerator` | QuestPDF A4 Landscape PDF with student name, reg. number, program, date |
| `GraduationRepository` | EF Core implementation with Include chains for approvals |
| `GraduationApplicationConfiguration` | EF config � `graduation_applications` table + soft-delete global filter |
| `GraduationApplicationApprovalConfiguration` | EF config � `graduation_application_approvals` table |

### API � Controller (Phase 18)
| Endpoint | Role | Purpose |
| --- | --- | --- |
| `GET /api/v1/graduation/my` | Student | Student's own applications |
| `GET /api/v1/graduation` | Admin/SuperAdmin | All applications with optional filters |
| `GET /api/v1/graduation/{id}` | Any auth | Application detail |
| `POST /api/v1/graduation/submit` | Student | Submit application |
| `POST /api/v1/graduation/{id}/faculty-approve` | Faculty | Faculty approve/reject |
| `POST /api/v1/graduation/{id}/admin-approve` | Admin/SuperAdmin | Admin approve/reject |
| `POST /api/v1/graduation/{id}/final-approve` | SuperAdmin | Final approval + cert generation |
| `POST /api/v1/graduation/{id}/reject` | Faculty/Admin/SuperAdmin | Reject |
| `GET /api/v1/graduation/{id}/certificate` | Student/Admin/SuperAdmin | Download certificate PDF |
| `POST /api/v1/graduation/{id}/regenerate-certificate` | Admin/SuperAdmin | Regenerate certificate |

### Web � EduApiClient + PortalController + Views (Phase 18)
| Component | Type | Details |
| --- | --- | --- |
| Phase 18 interface + impl methods (10 methods) | Methods (new) | `GetMyGraduationApplicationsAsync`, `GetGraduationApplicationDetailAsync`, `GetGraduationApplicationsAsync`, `SubmitGraduationApplicationAsync`, `FacultyApproveApplicationAsync`, `AdminApproveApplicationAsync`, `FinalApproveApplicationAsync`, `RejectApplicationAsync`, `DownloadCertificateAsync`, `RegenerateCertificateAsync` |
| Phase 18 web models (6 classes) | Classes (new) | `GraduationApplicationWebModel`, `ApprovalHistoryWebItem`, `GraduationApplicationDetailWebModel`, `GraduationApplyPageModel`, `GraduationApplicationsPageModel`, `GraduationApplicationDetailPageModel` |
| Phase 18 portal actions (8) | Actions (new) | `GraduationApply`, `GraduationSubmit`, `GraduationApplications`, `GraduationApplicationDetail`, `GraduationApprove`, `GraduationReject`, `GraduationCertificateDownload`, `GraduationRegenerateCertificate` |
| `GraduationApply.cshtml` | View (new) | Student apply/status page with existing applications table and submit form |
| `GraduationApplications.cshtml` | View (new) | Staff application list with status/department filters |
| `GraduationApplicationDetail.cshtml` | View (new) | Full detail with approval history timeline, certificate download, approve/reject actions |

  ---

## Phase 19 � Advanced Course Creation & Result Configuration

### Domain (Phase 19)

| Symbol | Type | Notes |
|--------|------|-------|
| `Course.HasSemesters` | Property (new) | `bool`, default `true`; indicates semester-based course |
| `Course.TotalSemesters` | Property (new) | `int?`; number of semesters for semester-based courses |
| `Course.DurationValue` | Property (new) | `int?`; numeric duration for non-semester courses |
| `Course.DurationUnit` | Property (new) | `string?` (Weeks/Months/Years) |
| `Course.GradingType` | Property (new) | `string`, default `"GPA"` |
| `Course.SetSemesterBased(totalSemesters, gradingType)` | Method (new) | Sets HasSemesters=true |
| `Course.SetNonSemesterBased(durationValue, durationUnit, gradingType)` | Method (new) | Sets HasSemesters=false |
| `CourseGradingConfig` | Entity (new) | Per-course grading config; `PassThreshold`, `GradingType`, `GradeRangesJson` |
| `ICourseGradingRepository` | Interface (new) | `GetByCourseIdAsync`, `AddAsync`, `Update`, `SaveChangesAsync` |
| `ICourseRepository.GetAllAsync` | Method (updated) | Now accepts `bool? hasSemesters` filter |

### Application (Phase 19)

| Symbol | Type | Notes |
|--------|------|-------|
| `ICourseService` | Interface (new) | `AutoCreateSemestersAsync(courseId)` |
| `CourseService` | Service (new) | Idempotent auto-creates semester rows for semester-based courses |
| `ICourseGradingService` | Interface (new) | `GetConfigAsync`, `UpsertConfigAsync` |
| `CourseGradingService` | Service (new) | Validates and upserts course grading config |
| `CreateCourseRequest` | DTO (updated) | Extended with `HasSemesters`, `TotalSemesters`, `DurationValue`, `DurationUnit`, `GradingType` |
| `CourseGradingConfigDto` | DTO (new) | Response DTO for grading config |
| `SaveCourseGradingConfigRequest` | DTO (new) | Request DTO for upsert |

### API � Controller (Phase 19)

| Endpoint | Auth | Notes |
|----------|------|-------|
| `GET /api/v1/course?hasSemesters={bool}` | Authenticated | Filter by course type |
| `POST /api/v1/course` | Admin/SuperAdmin | Creates course with semester/duration/grading config; auto-creates semester rows |
| `GET /api/v1/grading-config/{courseId}` | Authenticated | Returns per-course grading config |
| `PUT /api/v1/grading-config/{courseId}` | SuperAdmin | Creates or updates grading config |

### Web � EduApiClient + PortalController + Views (Phase 19)

| Symbol | Type | Notes |
|--------|------|-------|
| `EduApiClient.CreateCourseAsync` | Method (updated) | Now includes hasSemesters, totalSemesters, durationValue, durationUnit, gradingType |
| `EduApiClient.GetCourseDetailsByTypeAsync` | Method (new) | Filter courses by HasSemesters |
| `EduApiClient.GetCourseGradingConfigAsync` | Method (new) | Fetch grading config for course |
| `EduApiClient.SaveCourseGradingConfigAsync` | Method (new) | PUT grading config then re-fetch |
| `GradingConfigApiModel` | Class (new) | API response model for grading config |
| `GradingConfigPageModel` | View model (new) | SuperAdmin grading config page model |
| `GradeRangeItem` | View model (new) | Grade range row: From, To, Label |
| `PortalController.GradingConfig` | Action (new) | GET � loads courses + existing config |
| `PortalController.SaveGradingConfig` | Action (new) | POST � saves config, redirects |
| `GradingConfig.cshtml` | View (new) | SuperAdmin page: course selector, pass threshold, grade-range builder |
| `Courses.cshtml` | View (updated) | Create modal now has HasSemesters toggle, duration/semesters fields, grading type; table shows Type badge |
| `ResultCalculation.cshtml` | View (updated) | Stage 19.3: Course Type + Course filter panel at top |

  ---

## Final-Touches Phase 20 � Learning Management System (LMS) (2026-05-08, commit `ecf4d91`)

### Domain � Entities (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `CourseContentModule` | Entity (new) | `OfferingId`, `Title`, `WeekNumber`, `Body` (50 000 char), `IsPublished`; `Publish()`, `Unpublish()`, `Update()` domain methods |
| `ContentVideo` | Entity (new) | `ModuleId`, `Title`, `StorageUrl`, `EmbedUrl`, `DurationSeconds`; child of `CourseContentModule` |
| `DiscussionThread` | Entity (new) | `OfferingId`, `Title`, `AuthorId`, `IsPinned`, `IsClosed`; `SetPinned()`, `Close()`, `Reopen()` methods |
| `DiscussionReply` | Entity (new) | `ThreadId`, `AuthorId`, `Body`; `UpdateBody()` method |
| `CourseAnnouncement` | Entity (new) | `OfferingId` (nullable), `AuthorId`, `Title`, `Body`, `PostedAt` |

### Domain � Repository Interfaces (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `ILmsRepository` | Interface (new) | `GetModulesByOfferingAsync`, `GetModuleByIdAsync`, `AddModuleAsync`, `AddVideoAsync`, `SaveChangesAsync` |
| `IDiscussionRepository` | Interface (new) | `GetThreadsByOfferingAsync`, `GetThreadByIdAsync`, `AddThreadAsync`, `AddReplyAsync`, `SaveChangesAsync` |
| `IAnnouncementRepository` | Interface (new) | `GetByOfferingAsync`, `AddAsync`, `GetByIdAsync`, `SaveChangesAsync` |

### Application � DTOs (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `CreateModuleRequest` | DTO (new) | `OfferingId`, `Title`, `WeekNumber`, `Body` |
| `UpdateModuleRequest` | DTO (new) | `ModuleId`, `Title`, `WeekNumber`, `Body` |
| `AddVideoRequest` | DTO (new) | `ModuleId`, `Title`, `StorageUrl`, `EmbedUrl`, `DurationSeconds` |
| `ContentVideoDto` | DTO (new) | Response DTO for a video attachment |
| `CourseContentModuleDto` | DTO (new) | Response DTO for a module including its Videos list |
| `CreateThreadRequest` | DTO (new) | `OfferingId`, `AuthorId`, `Title` |
| `AddReplyRequest` | DTO (new) | `ThreadId`, `AuthorId`, `Body` |
| `DiscussionReplyDto` | DTO (new) | `Id`, `ThreadId`, `AuthorId`, `AuthorName`, `Body`, `CreatedAt`, `UpdatedAt` |
| `DiscussionThreadDto` | DTO (new) | `Id`, `OfferingId`, `Title`, `AuthorName`, `IsPinned`, `IsClosed`, `CreatedAt`, `Replies` list |
| `CreateAnnouncementRequest` | DTO (new) | `OfferingId`, `AuthorId`, `Title`, `Body` |
| `CourseAnnouncementDto` | DTO (new) | Response DTO including `PostedAt` and `AuthorName` |

### Application � Service Interfaces (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `ILmsService` | Interface (new) | `GetModulesAsync`, `GetModuleAsync`, `CreateModuleAsync`, `UpdateModuleAsync`, `PublishModuleAsync`, `UnpublishModuleAsync`, `DeleteModuleAsync`, `AddVideoAsync`, `DeleteVideoAsync` |
| `IDiscussionService` | Interface (new) | `GetThreadsAsync`, `GetThreadAsync`, `CreateThreadAsync`, `SetPinnedAsync`, `CloseThreadAsync`, `ReopenThreadAsync`, `DeleteThreadAsync`, `AddReplyAsync`, `DeleteReplyAsync` |
| `IAnnouncementService` | Interface (new) | `GetByOfferingAsync`, `CreateAsync`, `DeleteAsync` |

### Application � Service Implementations (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `LmsService` | Service (new) | Implements `ILmsService`; inline `MapModule`/`MapVideo` helpers; `SoftDelete()` for deletes; filters `!v.IsDeleted` videos in map |
| `DiscussionService` | Service (new) | Implements `IDiscussionService`; resolves `AuthorName` via `IUserRepository.GetByIdAsync ? Username`; `DeleteReplyAsync` enforces ownership � throws `UnauthorizedAccessException` if not faculty and not reply author |
| `AnnouncementService` | Service (new) | Implements `IAnnouncementService`; on create, queries `IEnrollmentRepository.GetByOfferingAsync` ? filters Active + non-null `StudentProfile` ? collects `UserId` list ? calls `INotificationService.SendSystemAsync` with `NotificationType.Announcement` |

### Infrastructure � EF Configurations (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `CourseContentModuleConfiguration` | EF config (new) | Table `course_content_modules`; FK `OfferingId` ? `CourseOfferings` (Cascade); `Body` MaxLength 50 000; global query filter `!IsDeleted` |
| `ContentVideoConfiguration` | EF config (new) | Table `content_videos`; `StorageUrl`/`EmbedUrl` MaxLength 1 000; global query filter `!IsDeleted` |
| `DiscussionThreadConfiguration` | EF config (new) | Table `discussion_threads`; FK `OfferingId` ? `CourseOfferings` (Cascade); `Title` MaxLength 500; global query filter `!IsDeleted` |
| `DiscussionReplyConfiguration` | EF config (new) | Table `discussion_replies`; `Body` MaxLength 10 000; global query filter `!IsDeleted` |
| `CourseAnnouncementConfiguration` | EF config (new) | Table `course_announcements`; FK `OfferingId` optional (SetNull); `Title` MaxLength 300; `Body` MaxLength 10 000; global query filter `!IsDeleted` |

### Infrastructure � Repositories (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `LmsRepository` | Repository (new) | Implements `ILmsRepository`; `GetModulesByOfferingAsync` includes Videos; `GetModuleByIdAsync` includes Videos |
| `DiscussionRepository` | Repository (new) | Implements `IDiscussionRepository`; `GetThreadsByOfferingAsync` orders pinned first then `CreatedAt` desc; `GetThreadByIdAsync` includes Replies |
| `AnnouncementRepository` | Repository (new) | Implements `IAnnouncementRepository`; `GetByOfferingAsync` orders by `PostedAt` desc |

### API � Controllers (Phase 20)

| Endpoint | Auth | Notes |
|----------|------|-------|
| `GET api/v1/lms/modules/{offeringId}` | Authenticated | Students get `publishedOnly=true` automatically |
| `GET api/v1/lms/module/{moduleId}` | Authenticated | Returns module with videos |
| `POST api/v1/lms/module` | Faculty/Admin/SuperAdmin | Create module |
| `PUT api/v1/lms/module/{id}` | Faculty/Admin/SuperAdmin | Update module |
| `POST api/v1/lms/module/{id}/publish` | Faculty/Admin/SuperAdmin | Publish |
| `POST api/v1/lms/module/{id}/unpublish` | Faculty/Admin/SuperAdmin | Unpublish |
| `DELETE api/v1/lms/module/{id}` | Faculty/Admin/SuperAdmin | Soft-delete module |
| `POST api/v1/lms/video` | Faculty/Admin/SuperAdmin | Attach video to module |
| `DELETE api/v1/lms/video/{id}` | Faculty/Admin/SuperAdmin | Soft-delete video |
| `GET api/v1/discussion/threads/{offeringId}` | Authenticated | List threads, pinned first |
| `GET api/v1/discussion/thread/{threadId}` | Authenticated | Thread detail with replies |
| `POST api/v1/discussion/thread` | Authenticated | Create thread; `AuthorId` overridden from JWT |
| `POST api/v1/discussion/thread/{id}/pin` | Faculty/Admin/SuperAdmin | Pin/unpin |
| `POST api/v1/discussion/thread/{id}/close` | Faculty/Admin/SuperAdmin | Close |
| `POST api/v1/discussion/thread/{id}/reopen` | Faculty/Admin/SuperAdmin | Reopen |
| `DELETE api/v1/discussion/thread/{id}` | Faculty/Admin/SuperAdmin | Soft-delete thread |
| `POST api/v1/discussion/reply` | Authenticated | Add reply; `AuthorId` overridden from JWT |
| `DELETE api/v1/discussion/reply/{id}` | Authenticated | Delete reply � faculty always allowed; students only own replies |
| `GET api/v1/announcement/{offeringId}` | Authenticated | List announcements |
| `POST api/v1/announcement` | Faculty/Admin/SuperAdmin | Create + fan-out notification |
| `DELETE api/v1/announcement/{id}` | Faculty/Admin/SuperAdmin | Soft-delete |

### Web � EduApiClient (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `GetLmsModulesAsync` | Method (new) | `GET api/v1/lms/modules/{offeringId}` |
| `GetLmsModuleAsync` | Method (new) | `GET api/v1/lms/module/{moduleId}` |
| `CreateLmsModuleAsync` | Method (new) | `POST api/v1/lms/module` then re-fetch |
| `UpdateLmsModuleAsync` | Method (new) | `PUT api/v1/lms/module/{id}` |
| `PublishLmsModuleAsync` / `UnpublishLmsModuleAsync` | Methods (new) | Toggle publish state |
| `DeleteLmsModuleAsync` | Method (new) | `DELETE api/v1/lms/module/{id}` |
| `AddLmsVideoAsync` / `DeleteLmsVideoAsync` | Methods (new) | Video CRUD |
| `GetDiscussionThreadsAsync` / `GetDiscussionThreadAsync` | Methods (new) | Thread list + detail |
| `CreateDiscussionThreadAsync` / `AddDiscussionReplyAsync` | Methods (new) | Create thread / reply |
| `SetThreadPinnedAsync` / `CloseDiscussionThreadAsync` / `ReopenDiscussionThreadAsync` | Methods (new) | Thread moderation |
| `DeleteDiscussionThreadAsync` / `DeleteDiscussionReplyAsync` | Methods (new) | Delete thread / reply |
| `GetAnnouncementsAsync` / `CreateAnnouncementAsync` / `DeleteAnnouncementAsync` | Methods (new) | Announcement CRUD |
| `LmsVideoApiModel` / `LmsModuleApiModel` / `DiscussionReplyApiModel` / `DiscussionThreadApiModel` / `AnnouncementApiModel` | Classes (new) | API response models |

### Web � PortalController + Views (Phase 20)

| Symbol | Type | Notes |
|--------|------|-------|
| `CourseLms(GET)` | Action (new) | Student module listing |
| `LmsManage(GET)` | Action (new) | Faculty module management |
| `CreateLmsModule` / `UpdateLmsModule` / `PublishLmsModule` / `UnpublishLmsModule` / `DeleteLmsModule` | Actions (new) | Module CRUD POST actions |
| `AddLmsVideo` / `DeleteLmsVideo` | Actions (new) | Video CRUD POST actions |
| `Discussion(GET)` | Action (new) | Thread listing |
| `DiscussionThreadDetail(GET)` | Action (new) | Thread detail with replies |
| `CreateDiscussionThread` / `AddDiscussionReply` / `DeleteDiscussionThread` / `DeleteDiscussionReply` | Actions (new) | Discussion POST actions |
| `Announcements(GET)` | Action (new) | Announcement listing |
| `CreateAnnouncement` / `DeleteAnnouncement` | Actions (new) | Announcement POST actions |
| `CourseLms.cshtml` | View (new) | Student: accordion of published modules + embedded video iframes |
| `LmsManage.cshtml` | View (new) | Faculty: manage modules + videos, publish/unpublish controls |
| `Discussion.cshtml` | View (new) | Thread list; create thread form; faculty pin/close controls |
| `DiscussionThread.cshtml` | View (new) | Thread detail; reply list; add reply form; delete controls |
| `Announcements.cshtml` | View (new) | Announcement cards; create form; delete button |
| `LmsVideoItem` / `LmsModuleItem` / `CourseLmsPageModel` / `LmsManagePageModel` | View models (new) | LMS portal view models |
| `DiscussionReplyItem` / `DiscussionThreadItem` / `DiscussionPageModel` / `DiscussionDetailPageModel` | View models (new) | Discussion portal view models |
| `AnnouncementItem` / `AnnouncementsPageModel` | View models (new) | Announcement portal view models |

  ---

## Final-Touches Phase 21 � Study Planner (2026-05-08)

### Domain � Entities (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `StudyPlan` | Entity (`AuditableEntity`) | Student semester study plan; has `AdvisorStatus (Pending/Endorsed/Rejected)`, endorsement workflow methods |
| `StudyPlanCourse` | Entity (`BaseEntity`) | Course line item in a plan; FK to `StudyPlan` + `Course` |
| `StudyPlanStatus` | Enum | `Pending=0 / Endorsed=1 / Rejected=2` |
| `AcademicProgram.MaxCreditLoadPerSemester` | Property (added) | Max credit hours per semester plan; default 18 |
| `AcademicProgram.SetMaxCreditLoad()` | Method (added) | Updates max credit load |

### Domain � Repository Interfaces (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `IStudyPlanRepository` | Interface (new) | `GetByStudentAsync`, `GetByDepartmentAsync`, `GetByIdAsync`, `AddAsync`, `Update`, `GetPlannedCreditHoursAsync`, `SaveChangesAsync` |

### Application � DTOs (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `CreateStudyPlanRequest` / `AddPlanCourseRequest` / `AdvisePlanRequest` / `SetMaxCreditLoadRequest` | Records (new) | Request DTOs |
| `StudyPlanCourseDto` / `StudyPlanDto` | Records (new) | Response DTOs |
| `RecommendedCourseDto` / `StudyPlanRecommendationDto` | Records (new) | Recommendation response DTOs |

### Application � Service Interfaces (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `IStudyPlanService` | Interface (new) | Full CRUD + advisor workflow + recommendations |

### Application � Service Implementations (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `StudyPlanService` | Class (new) | Validates prerequisites, credit load; recommendation engine (degree gaps + electives) |

### Infrastructure � EF Configurations (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `StudyPlanConfiguration` | Config (new) | Table `study_plans`; HasQueryFilter `!IsDeleted`; cascade delete from student_profiles |
| `StudyPlanCourseConfiguration` | Config (new) | Table `study_plan_courses`; unique index per plan+course; restrict delete from courses |
| `AcademicProgramConfiguration` | Config (updated) | Added `MaxCreditLoadPerSemester` column (default 18) |

### Infrastructure � Repositories (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `StudyPlanRepository` | Class (new) | EF implementation of `IStudyPlanRepository`; includes Courses + Course nav properties |

### API � Controllers (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `StudyPlanController` | Controller (new) | Route `api/v1/study-plan`; 9 endpoints |
| `GET plans/{studentProfileId}` | Endpoint | Student/Faculty/Admin/SuperAdmin |
| `GET plans/department/{departmentId}` | Endpoint | Faculty/Admin/SuperAdmin |
| `GET plan/{planId}` | Endpoint | All authenticated |
| `POST plan` | Endpoint | Student/Admin/SuperAdmin |
| `POST plan/{planId}/course` | Endpoint | Student/Admin/SuperAdmin � validates prereqs + credit load |
| `DELETE plan/{planId}/course/{courseId}` | Endpoint | Student/Admin/SuperAdmin |
| `DELETE plan/{planId}` | Endpoint | Student/Admin/SuperAdmin |
| `POST plan/{planId}/advise` | Endpoint | Faculty/Admin/SuperAdmin |
| `GET recommendations/{studentProfileId}` | Endpoint | Student/Faculty/Admin/SuperAdmin |

### Web � EduApiClient (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `GetStudyPlansAsync` / `GetStudyPlansByDepartmentAsync` / `GetStudyPlanAsync` | Methods (new) | GET study plans |
| `CreateStudyPlanAsync` / `AddStudyPlanCourseAsync` / `RemoveStudyPlanCourseAsync` / `DeleteStudyPlanAsync` | Methods (new) | CRUD study plans |
| `AdvisePlanAsync` | Method (new) | Faculty advisor decision |
| `GetStudyPlanRecommendationsAsync` | Method (new) | Recommendation engine call |
| `StudyPlanApiModel` / `StudyPlanCourseApiModel` / `StudyPlanRecommendationApiModel` / `RecommendedCourseApiModel` | Classes (new) | API response models |

### Web � PortalController + Views (Phase 21)

| Symbol | Type | Notes |
|--------|------|-------|
| `StudyPlan(GET)` | Action (new) | Student's plan list |
| `StudyPlanDetail(GET)` | Action (new) | Plan detail + course management |
| `StudyPlanRecommendations(GET)` | Action (new) | Recommendation page |
| `CreateStudyPlan` / `AddStudyPlanCourse` / `RemoveStudyPlanCourse` / `DeleteStudyPlan` | Actions (new) | Plan CRUD POST actions |
| `AdvisePlan` | Action (new) | Faculty advisor POST action |
| `StudyPlan.cshtml` | View (new) | Plan list + create form + recommendations link |
| `StudyPlanDetail.cshtml` | View (new) | Plan detail + add/remove courses + advisor panel |
| `StudyPlanRecommendations.cshtml` | View (new) | Recommended courses table |
| `StudyPlanCourseItem` / `StudyPlanItem` / `StudyPlanPageModel` / `StudyPlanDetailPageModel` | View models (new) | Study plan portal view models |
| `RecommendationItem` / `RecommendationsPageModel` | View models (new) | Recommendation portal view models |

  ---

## Final-Touches Phase 22 � External Integrations (2026-05-08) | Commit `dddee69`

### API � LibraryController (Stage 22.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetConfig(ct)` | Returns current library connection config (catalogue URL + token). SuperAdmin only. | `API/Controllers/LibraryController.cs` |
| `SaveConfig(cmd, ct)` | Saves library connection settings to portal_settings. SuperAdmin only. | `API/Controllers/LibraryController.cs` |
| `GetLoans(ct)` | Proxies loan-status request to external library API using the calling user's username. All authenticated. | `API/Controllers/LibraryController.cs` |
| `GetLoansForStudent(studentIdentifier, ct)` | Proxies loan-status request for a specific student. Admin/SuperAdmin only. | `API/Controllers/LibraryController.cs` |

### API � AccreditationController (Stage 22.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Create(cmd, ct)` | Creates a new accreditation template. SuperAdmin only. | `API/Controllers/AccreditationController.cs` |
| `Update(id, cmd, ct)` | Updates an existing template. SuperAdmin only. | `API/Controllers/AccreditationController.cs` |
| `Generate(id, ct)` | Generates and streams the accreditation report for a template; writes to audit log. Admin/SuperAdmin. | `API/Controllers/AccreditationController.cs` |

### Application � ILibraryService / LibraryService (Stage 22.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetConfigAsync(ct)` | Returns current library connection configuration from portal_settings. | `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs` |
| `SaveConfigAsync(cmd, ct)` | Persists library URL + token to portal_settings. | `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs` |
| `GetLoansAsync(studentIdentifier, ct)` | Calls external library API with configured auth and returns loan list. | `Application/Interfaces/ILibraryService.cs`, `Application/Services/LibraryService.cs` |

### Application � IAccreditationService / AccreditationService (Stage 22.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetTemplatesAsync(ct)` | Returns all accreditation templates. | `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs` |
| `CreateAsync(cmd, ct)` | Creates and persists a new accreditation template. | `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs` |
| `UpdateAsync(id, cmd, ct)` | Updates name, description, field mappings, format, active state. | `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs` |
| `GenerateAsync(id, actorUserId, ct)` | Materialises data for the template field mappings; formats as CSV or PDF; writes audit-log entry; returns `GeneratedReport(Content, ContentType, FileName)`. | `Application/Interfaces/IAccreditationService.cs`, `Application/Services/AccreditationService.cs` |

### Infrastructure � AccreditationRepository (Stage 22.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `AddAsync(template, ct)` | Queues template insert. | `Infrastructure/Repositories/AccreditationRepository.cs` |
| `Update(template)` | Marks template as modified. | `Infrastructure/Repositories/AccreditationRepository.cs` |

### Web � EduApiClient (Phase 22)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetLibraryConfigAsync(ct)` | Fetches library connection config. | `Web/Services/EduApiClient.cs` |
| `SaveLibraryConfigAsync(url, token, ct)` | Saves library URL + token. | `Web/Services/EduApiClient.cs` |
| `GetLibraryLoansAsync(ct)` | Fetches current user's loan list from library proxy. | `Web/Services/EduApiClient.cs` |
| `GetAccreditationTemplatesAsync(ct)` | Fetches all templates. | `Web/Services/EduApiClient.cs` |
| `CreateAccreditationTemplateAsync(cmd, ct)` | Creates a template. | `Web/Services/EduApiClient.cs` |
| `UpdateAccreditationTemplateAsync(id, cmd, ct)` | Updates a template. | `Web/Services/EduApiClient.cs` |
| `DeleteAccreditationTemplateAsync(id, ct)` | Deletes a template. | `Web/Services/EduApiClient.cs` |
| `GenerateAccreditationReportAsync(id, ct)` | Calls generate endpoint and returns raw bytes + content type. | `Web/Services/EduApiClient.cs` |

### Web � PortalController / Views (Phase 22)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `LibraryConfig(ct)` | Loads library configuration portal page (SuperAdmin). | `Web/Controllers/PortalController.cs` |
| `SaveLibraryConfig(url, token, ct)` | Saves library config from portal form. | `Web/Controllers/PortalController.cs` |
| `AccreditationTemplates(ct)` | Loads accreditation templates page (SuperAdmin/Admin). | `Web/Controllers/PortalController.cs` |
| `CreateAccreditationTemplate(...)` | Creates template from portal form. | `Web/Controllers/PortalController.cs` |
| `UpdateAccreditationTemplate(...)` | Updates template from portal form. | `Web/Controllers/PortalController.cs` |
| `DeleteAccreditationTemplate(id, ct)` | Deletes template from portal. | `Web/Controllers/PortalController.cs` |
| `DownloadAccreditationReport(id, ct)` | Proxies generated report download from portal. | `Web/Controllers/PortalController.cs` |
| `LibraryConfig.cshtml` | View � library URL + token configuration form. | `Web/Views/Portal/LibraryConfig.cshtml` |
| `AccreditationTemplates.cshtml` | View � template list with create/edit/delete/generate actions. | `Web/Views/Portal/AccreditationTemplates.cshtml` |

  ---

## Final-Touches Phase 23 � Core Policy Foundation (2026-05-09) | Commit `28cac36`

### Domain � InstitutionType Enum (Stage 23.1)

| Symbol | Type | Notes |
|--------|------|-------|
| `InstitutionType` | Enum | `University = 0` (default, backward-compatible), `School = 1`, `College = 2` | `Domain/Enums/InstitutionType.cs` |

### Application � IInstitutionPolicyService / InstitutionPolicyService (Stages 23.1�23.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetPolicyAsync(ct)` | Returns current `InstitutionPolicySnapshot`; reads from 10-minute `IMemoryCache`; falls back to `portal_settings`; returns `Default` (University-only) when unset. | `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs` |
| `SavePolicyAsync(cmd, ct)` | Persists institution type flags to `portal_settings`; invalidates cache; throws `InvalidOperationException` when all three flags are false. | `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs` |
| `InvalidateCache()` | Evicts the `IMemoryCache` entry so the next call reloads from storage. | `Application/Interfaces/IInstitutionPolicyService.cs`, `Application/Services/InstitutionPolicyService.cs` |
| `InstitutionPolicySnapshot` | Sealed record � `IncludeSchool`, `IncludeCollege`, `IncludeUniversity`; `IsEnabled(InstitutionType)` method; static `Default`. | `Application/Interfaces/IInstitutionPolicyService.cs` |
| `SaveInstitutionPolicyCommand` | Command record carried by the PUT endpoint. | `Application/Interfaces/IInstitutionPolicyService.cs` |

### API � InstitutionPolicyController (Stages 23.1�23.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Get(ct)` | Returns current institution policy flags as `InstitutionPolicyResponse`. All authenticated roles. | `API/Controllers/InstitutionPolicyController.cs` |
| `Save(request, ct)` | Updates institution type flags. SuperAdmin only. Returns 400 when all flags are false. | `API/Controllers/InstitutionPolicyController.cs` |

### API � InstitutionContextMiddleware (Stage 23.2)

| Symbol | Purpose | Location |
|--------|---------|----------|
| `InstitutionContextMiddleware.InvokeAsync(context)` | Resolves `IInstitutionPolicyService` per-request; stores snapshot in `HttpContext.Items["InstitutionPolicy"]`; skips for anonymous requests. Registered after `UseAuthorization`. | `API/Middleware/InstitutionContextMiddleware.cs` |
| `HttpContextExtensions.GetInstitutionPolicy(context)` | Extension method to read the snapshot from `HttpContext.Items`; returns `InstitutionPolicySnapshot.Default` if not set. | `API/Middleware/InstitutionContextMiddleware.cs` |

### Web � EduApiClient (Phase 23)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetInstitutionPolicyAsync(ct)` | Fetches current institution policy flags from API. | `Web/Services/EduApiClient.cs` |
| `SaveInstitutionPolicyAsync(school, college, university, ct)` | Calls PUT endpoint to update institution type flags. | `Web/Services/EduApiClient.cs` |

### Web � PortalController / Views (Phase 23)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `InstitutionPolicy(ct)` | Loads institution policy config page (SuperAdmin). | `Web/Controllers/PortalController.cs` |
| `SaveInstitutionPolicy(school, college, university, ct)` | Saves institution flags from portal form; redirects with success/error message. | `Web/Controllers/PortalController.cs` |
| `InstitutionPolicy.cshtml` | SuperAdmin config view � three toggle checkboxes with save form and current-state display. | `Web/Views/Portal/InstitutionPolicy.cshtml` |

  ---

## Final-Touches Phase 24 � Dynamic Module and UI Composition (2026-05-09) | Commit `391ac45`

### Domain � ModuleDescriptor (Stage 24.1)

| Symbol | Type | Notes |
|--------|------|-------|
| `ModuleDescriptor` | Sealed record | `Key`, `RequiredRoles[]`, `AllowedTypes[]?`, `IsLicenseGated`; `RoleMatches(role)` + `TypeMatches(type)` methods. | `Domain/Modules/ModuleDescriptor.cs` |

### Application � ModuleRegistry (Stage 24.1)

| Symbol | Type | Notes |
|--------|------|-------|
| `ModuleRegistry` | Static class | Compile-time catalogue of all 14 module `ModuleDescriptor` entries keyed by module key. Notable: `fyp` = University-only; `ai_chat` = license-gated; `advanced_audit` = SuperAdmin-only. | `Application/Modules/ModuleRegistry.cs` |

### Application � IModuleRegistryService / ModuleRegistryService (Stage 24.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetVisibleModulesAsync(role, policy, ct)` | Returns `IReadOnlyList<ModuleVisibilityResult>` for a given role and institution policy � combines registry descriptor role/type check with live `IsActiveAsync`. | `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs` |
| `IsAccessibleAsync(key, role, policy, ct)` | Returns `bool` � checks whether a specific module key is accessible for the given role and policy. | `Application/Interfaces/IModuleRegistryService.cs`, `Application/Modules/ModuleRegistryService.cs` |
| `ModuleVisibilityResult` | Sealed record � `Key`, `Name`, `IsActive`, `IsAccessible`. | `Application/Interfaces/IModuleRegistryService.cs` |

### Application � ILabelService / LabelService (Stage 24.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetVocabulary(policy)` | Returns `AcademicVocabulary` appropriate for the active institution type (University ? Semester/GPA/Course/Batch; School ? Grade/Percentage/Subject/Class; College ? Year/Percentage/Subject/Year-Group). | `Application/Interfaces/ILabelService.cs`, `Application/Services/LabelService.cs` |
| `AcademicVocabulary` | Sealed record � `PeriodLabel`, `ProgressionLabel`, `GradingLabel`, `CourseLabel`, `StudentGroupLabel`; static `Default` = University vocab. | `Application/Interfaces/ILabelService.cs` |

### Application � IDashboardCompositionService / DashboardCompositionService (Stage 24.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetWidgets(role, policy)` | Returns ordered `IReadOnlyList<WidgetDescriptor>` filtered by role + institution type. `fyp_panel` = Faculty/Student + University only; `system_health` = SuperAdmin only; `ai_assistant` = all roles. | `Application/Interfaces/IDashboardCompositionService.cs`, `Application/Services/DashboardCompositionService.cs` |
| `WidgetDescriptor` | Sealed record � `Key`, `Title`, `Icon`, `Order`. | `Application/Interfaces/IDashboardCompositionService.cs` |

### API � ModuleRegistryController / LabelController / DashboardCompositionController (Stage 24.1�24.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ModuleRegistryController.GetVisible(ct)` | `GET api/v1/module-registry/visible` � returns visible module list for the authenticated user's role and institution policy. All authenticated. | `API/Controllers/ModuleRegistryController.cs` |
| `LabelController.Get(ct)` | `GET api/v1/labels` � returns institution-appropriate academic vocabulary. All authenticated. | `API/Controllers/LabelController.cs` |
| `DashboardCompositionController.Get(ct)` | `GET api/v1/dashboard/composition` � returns ordered widget list for the authenticated user's role and institution policy. All authenticated. | `API/Controllers/DashboardCompositionController.cs` |

### Web � EduApiClient (Phase 24)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetVisibleModulesAsync(ct)` | `GET api/v1/module-registry/visible` � fetches visible module list. | `Web/Services/EduApiClient.cs` |
| `GetVocabularyAsync(ct)` | `GET api/v1/labels` � fetches academic vocabulary for current institution mode. | `Web/Services/EduApiClient.cs` |
| `GetDashboardWidgetsAsync(ct)` | `GET api/v1/dashboard/composition` � fetches ordered widget list. | `Web/Services/EduApiClient.cs` |

### Web � PortalController / Views (Phase 24)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ModuleComposition(ct)` | Parallel-fetches visible modules, vocabulary, and widgets via `Task.WhenAll`; renders composition page. SuperAdmin. | `Web/Controllers/PortalController.cs` |
| `ModuleComposition.cshtml` | SuperAdmin view � vocabulary label tiles, widget cards, full module registry table (key, roles, institution types, license gate, accessible state). | `Web/Views/Portal/ModuleComposition.cshtml` |

  ---

## Phase 25 � Academic Engine Unification (2026-05-09)

### Application � IResultCalculationStrategy / GpaResultStrategy / PercentageResultStrategy (Stage 25.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IResultCalculationStrategy.Calculate(marks, gpaRules, threshold, gradeRangesJson)` | Strategy contract: converts component marks into a `ResultSummary` (score, GPA, percentage, grade label, pass/fail). | `Application/Interfaces/IResultCalculationStrategy.cs` |
| `GpaResultStrategy.Calculate(...)` | University mode: weighted percentage ? GPA lookup on configured scale; pass = GPA = threshold. | `Application/Academic/GpaResultStrategy.cs` |
| `PercentageResultStrategy.Calculate(...)` | School/College mode: weighted percentage ? grade band lookup (custom JSON or built-in defaults); pass = % = threshold. | `Application/Academic/PercentageResultStrategy.cs` |
| `ComponentMark` (record) | Input value type: component name, marks obtained/max, weightage. | `Application/Interfaces/IResultCalculationStrategy.cs` |
| `ResultSummary` (record) | Output value type: total score, grade point, percentage, grade label, isPassing. | `Application/Interfaces/IResultCalculationStrategy.cs` |
| `GpaScaleRuleEntry` (record) | Lightweight GPA scale entry for strategy calculations (no EF dependency). | `Application/Interfaces/IResultCalculationStrategy.cs` |
| `GradeBandEntry` (record) | Lightweight grade band for percentage strategies. | `Application/Interfaces/IResultCalculationStrategy.cs` |

### Application � IResultStrategyResolver / ResultStrategyResolver (Stage 25.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IResultStrategyResolver.Resolve(institutionType)` | Returns the appropriate strategy for the given institution type. | `Application/Interfaces/IResultStrategyResolver.cs` |
| `ResultStrategyResolver.Resolve(institutionType)` | Maps University ? GpaResultStrategy; School/College ? PercentageResultStrategy. Singleton. | `Application/Academic/ResultStrategyResolver.cs` |

### Domain � InstitutionGradingProfile (Stage 25.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `InstitutionGradingProfile(type, threshold, json)` | Creates a new profile; validates threshold (0�4.0 for University, 0�100 for School/College). | `Domain/Academic/InstitutionGradingProfile.cs` |
| `InstitutionGradingProfile.Update(threshold, json, isActive)` | Updates threshold, grade bands JSON, and active state. | `Domain/Academic/InstitutionGradingProfile.cs` |

### Domain � IInstitutionGradingProfileRepository (Stage 25.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetByTypeAsync(type, ct)` | Returns the active profile for the given institution type, or null. | `Domain/Interfaces/IInstitutionGradingProfileRepository.cs` |
| `AddAsync / Update / SaveChangesAsync` | Standard CRUD + persistence. | `Domain/Interfaces/IInstitutionGradingProfileRepository.cs` |

### Application � IInstitutionGradingService / InstitutionGradingService (Stage 25.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `UpsertAsync(type, request, ct)` | Creates or updates the grading profile for the given institution type. | `Application/Academic/InstitutionGradingService.cs` |

### Application � IProgressionService / ProgressionService (Stage 25.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `EvaluateAsync(request, ct)` | Evaluates whether a student can progress; returns `ProgressionDecision` without side effects. | `Application/Interfaces/IProgressionService.cs` |
| `PromoteAsync(request, ct)` | Evaluates then advances the student's `CurrentSemesterNumber` if eligible; throws on failure. | `Application/Academic/ProgressionService.cs` |
| `ProgressionDecision` (record) | Output: studentId, institutionType, canProgress, period labels, achieved/required scores, remarks. | `Application/DTOs/Academic/ProgressionDtos.cs` |
| `ProgressionEvaluationRequest` (record) | Input: studentProfileId + institutionType. | `Application/DTOs/Academic/ProgressionDtos.cs` |

### Application � Student Lifecycle Year Mapping (Stage 26.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IStudentLifecycleService.GetStudentsByAcademicLevelAsync(departmentId, levelNumber, ct)` | Returns students by academic level; for College, maps Year N to semesters `2N-1` and `2N`. | `Application/Interfaces/IStudentLifecycleService.cs` |
| `StudentLifecycleService.GetStudentsByAcademicLevelAsync(...)` | Implements institution-aware academic-level retrieval while preserving existing semester behavior for non-College institutions. | `Application/Services/StudentLifecycleService.cs` |
| `StudentLifecycleService.PromoteStudentAsync(studentProfileId, ct)` | Routes School and College promotions through progression service eligibility checks. | `Application/Services/StudentLifecycleService.cs` |

### Domain/Infrastructure � Lifecycle Repository Year Range Query (Stage 26.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IStudentLifecycleRepository.GetActiveStudentsBySemesterRangeAsync(departmentId, startSemesterNumber, endSemesterNumber, ct)` | Contract for active-student retrieval across an inclusive semester range (used for College year mapping). | `Domain/Interfaces/IStudentLifecycleRepository.cs` |
| `StudentLifecycleRepository.GetActiveStudentsBySemesterRangeAsync(...)` | EF implementation for inclusive semester range query with program projection ordering. | `Infrastructure/Repositories/StudentLifecycleRepository.cs` |

### Application � College Promotion Year Step (Stage 26.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ProgressionService.PromoteAsync(request, ct)` | For College promotion, advances two semesters to represent a full year progression after eligibility passes. | `Application/Academic/ProgressionService.cs` |

### Application � Bulk Promotion Supplementary Policy (Advanced Stage 26.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `BulkPromotionService.ApplyAsync(request, ct)` | Evaluates progression eligibility before applying promote entries; converts failed entries to `Hold` instead of force-promoting. | `Application/Academic/BulkPromotionService.cs` |
| `BulkPromotionService.ApplyAsync(request, ct)` (College path) | Adds supplementary-required reason when a College promote entry fails threshold checks. | `Application/Academic/BulkPromotionService.cs` |
| `BulkPromotionService.ApplyAsync(request, ct)` (College success path) | Uses progression orchestration so successful College promote entries preserve year-step advancement semantics. | `Application/Academic/BulkPromotionService.cs` |

### Web � SuperAdmin Grading Setup Sections (Advanced Stage 27.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `PortalController.GradingConfig(courseId, ct)` | Loads sectioned institution grading setup for SuperAdmin (School/College/University) alongside existing course grading configuration. | `Web/Controllers/PortalController.cs` |
| `PortalController.SaveInstitutionGradingProfile(...)` | Saves institution-specific grading section updates (threshold, JSON bands, active state) through upsert API calls. | `Web/Controllers/PortalController.cs` |
| `PortalController.BuildInstitutionGradingSections(...)` | Builds deterministic section view-model state per institution type with defaults and profile overlays. | `Web/Controllers/PortalController.cs` |
| `IEduApiClient.GetInstitutionGradingProfilesAsync(ct)` | Fetches institution grading profiles for section rendering. | `Web/Services/EduApiClient.cs` |
| `IEduApiClient.SaveInstitutionGradingProfileAsync(...)` | Persists a single institution grading section by institution type. | `Web/Services/EduApiClient.cs` |
| `InstitutionGradingSectionItem` | View model backing section cards (threshold bounds, JSON ranges, active state). | `Web/Models/Portal/PortalViewModels.cs` |

### Application/Infrastructure � Rule Application Engine (Advanced Stage 27.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `EnrollmentService.TryEnrollAsync(studentProfileId, courseOfferingId, overrideClash, overrideReason, ct)` | Applies institution-profile-driven prerequisite pass thresholds before enrollment approval. | `Application/Academic/EnrollmentService.cs` |
| `EnrollmentService.ResolvePrerequisitePassThresholdPercentageAsync(studentProfileId, ct)` | Resolves prerequisite pass threshold by student institution type and normalizes University GPA-scale thresholds to percentage. | `Application/Academic/EnrollmentService.cs` |
| `IResultRepository.HasPassedCourseAsync(studentProfileId, courseId, passThresholdPercentage, ct)` | Repository contract for threshold-parameterized prerequisite pass evaluation. | `Domain/Interfaces/IResultRepository.cs` |
| `ResultRepository.HasPassedCourseAsync(studentProfileId, courseId, passThresholdPercentage, ct)` | EF implementation of policy-driven prerequisite pass check (no fixed 50% constant). | `Infrastructure/Repositories/AssignmentResultRepositories.cs` |

### Application/API � Parent-Student Controlled Mapping (Advanced Stage 28.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IParentPortalService.GetLinksByParentAsync(parentUserId, ct)` | Returns all parent-student links for admin management view. | `Application/Interfaces/IParentPortalService.cs` |
| `IParentPortalService.UpsertLinkAsync(request, ct)` | Creates/updates parent-student links with role/scope validation. | `Application/Interfaces/IParentPortalService.cs` |
| `IParentPortalService.DeactivateLinkAsync(parentUserId, studentProfileId, ct)` | Soft-deactivates an existing parent-student link. | `Application/Interfaces/IParentPortalService.cs` |
| `ParentPortalService.UpsertLinkAsync(request, ct)` | Enforces Parent-role target user and School-student scope before persistence. | `Application/Academic/ParentPortalService.cs` |
| `ParentPortalController.GetLinksByParent(parentUserId, ct)` | `GET api/v1/parent-portal/links/{parentUserId}` admin-managed link list endpoint. | `API/Controllers/ParentPortalController.cs` |
| `ParentPortalController.UpsertLink(request, ct)` | `PUT api/v1/parent-portal/links` admin-managed controlled link upsert endpoint. | `API/Controllers/ParentPortalController.cs` |
| `ParentPortalController.DeactivateLink(parentUserId, studentProfileId, ct)` | `DELETE api/v1/parent-portal/links/{parentUserId}/{studentProfileId}` admin-managed link deactivation endpoint. | `API/Controllers/ParentPortalController.cs` |

### API � InstitutionGradingProfileController (Stage 25.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetByType(type, ct)` | `GET api/v1/institution-grading-profiles/{type}` � returns profile for type or 404. Admin+. | `API/Controllers/InstitutionGradingProfileController.cs` |
| `Upsert(type, request, ct)` | `PUT api/v1/institution-grading-profiles/{type}` � creates/updates profile. SuperAdmin only. | `API/Controllers/InstitutionGradingProfileController.cs` |

### API � ProgressionController (Stage 25.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `Evaluate(request, ct)` | `POST api/v1/progression/evaluate` � returns progression decision without changes. Admin+. | `API/Controllers/ProgressionController.cs` |
| `Promote(request, ct)` | `POST api/v1/progression/promote` � advances student if eligible; 400 on failure. Admin+. | `API/Controllers/ProgressionController.cs` |
| `GetMyProgression(type, ct)` | `GET api/v1/progression/me/{type}` � student self-view of progression eligibility. Student+. | `API/Controllers/ProgressionController.cs` |

  ---

## Phase 26 � School and College Functional Expansion (2026-05-09)

### Application � ISchoolStreamService / SchoolStreamService (Stage 26.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetAllStreamsAsync(ct)` | Returns all school stream definitions ordered by name. | `Application/Interfaces/ISchoolStreamService.cs` |
| `UpsertStreamAsync(id, request, ct)` | Creates or updates stream metadata and active state. | `Application/Academic/SchoolStreamService.cs` |
| `AssignStudentAsync(request, ct)` | Assigns/reassigns one stream per student profile. | `Application/Academic/SchoolStreamService.cs` |
| `GetStudentAssignmentAsync(studentId, ct)` | Returns stream assignment details for a student. | `Application/Academic/SchoolStreamService.cs` |

### Domain � SchoolStream / StudentStreamAssignment (Stage 26.1)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SchoolStream.Update(name, description, isActive)` | Updates stream master record with validation and audit touch. | `Domain/Academic/SchoolStream.cs` |
| `StudentStreamAssignment(studentId, streamId, byUser)` | Creates a stream assignment snapshot with assignment timestamp. | `Domain/Academic/StudentStreamAssignment.cs` |

### Application � IReportCardService / ReportCardService (Stage 26.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GenerateAsync(request, ct)` | Persists a report-card snapshot payload for a student period. | `Application/Academic/ReportCardService.cs` |
| `GetLatestAsync(studentId, ct)` | Returns the latest report card for a student. | `Application/Academic/ReportCardService.cs` |
| `GetHistoryAsync(studentId, ct)` | Returns report-card history for a student. | `Application/Academic/ReportCardService.cs` |

### Application � IBulkPromotionService / BulkPromotionService (Stage 26.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `CreateBatchAsync(request, ct)` | Creates a new draft bulk-promotion batch. | `Application/Academic/BulkPromotionService.cs` |
| `AddEntriesAsync(request, ct)` | Adds promote/hold entries to a draft batch with duplicate prevention. | `Application/Academic/BulkPromotionService.cs` |
| `SubmitAsync(batchId, ct)` | Moves batch from Draft to AwaitingApproval state. | `Application/Academic/BulkPromotionService.cs` |
| `ReviewAsync(request, ct)` | Approves or rejects a pending batch with reviewer metadata. | `Application/Academic/BulkPromotionService.cs` |
| `ApplyAsync(request, ct)` | Applies approved batch and advances only Promote entries. | `Application/Academic/BulkPromotionService.cs` |
| `GetByIdAsync(batchId, ct)` | Returns batch details including entry states. | `Application/Academic/BulkPromotionService.cs` |

### Domain � BulkPromotionBatch / BulkPromotionEntry (Stage 26.2)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `BulkPromotionBatch.AddEntry(studentId, decision)` | Adds a student decision row while in Draft status. | `Domain/Academic/BulkPromotionBatch.cs` |
| `BulkPromotionBatch.Submit()` | Transitions Draft batch to AwaitingApproval; rejects empty batches. | `Domain/Academic/BulkPromotionBatch.cs` |
| `BulkPromotionBatch.Approve(userId, note)` | Transitions AwaitingApproval to Approved. | `Domain/Academic/BulkPromotionBatch.cs` |
| `BulkPromotionBatch.Reject(userId, reason)` | Transitions AwaitingApproval to Rejected. | `Domain/Academic/BulkPromotionBatch.cs` |
| `BulkPromotionBatch.MarkApplied()` | Transitions Approved batch to Applied after execution. | `Domain/Academic/BulkPromotionBatch.cs` |
| `BulkPromotionEntry.UpdateDecision(decision, reason)` | Updates row-level decision/reason. | `Domain/Academic/BulkPromotionEntry.cs` |
| `BulkPromotionEntry.MarkApplied()` | Marks entry as applied with timestamp. | `Domain/Academic/BulkPromotionEntry.cs` |

### Application � IParentPortalService / ParentPortalService (Stage 26.3)

| Function Name | Purpose | Location |
| --- | --- | --- |
| `GetLinkedStudentsAsync(parentUserId, ct)` | Returns read-only student summaries for active parent links. | `Application/Academic/ParentPortalService.cs` |

### API � Phase 26 Controllers

| Function Name | Purpose | Location |
| --- | --- | --- |
| `SchoolStreamController.GetAll(ct)` | `GET api/v1/school-streams` � list streams. Faculty+. | `API/Controllers/SchoolStreamController.cs` |
| `SchoolStreamController.Upsert(id, request, ct)` | `PUT api/v1/school-streams/{id}` � create/update stream. Admin+. | `API/Controllers/SchoolStreamController.cs` |
| `SchoolStreamController.Assign(request, ct)` | `POST api/v1/school-streams/assign` � assign stream to student. Admin+. | `API/Controllers/SchoolStreamController.cs` |
| `ReportCardController.Generate(request, ct)` | `POST api/v1/report-cards/generate` � generate report card snapshot. Faculty+. | `API/Controllers/ReportCardController.cs` |
| `ReportCardController.GetLatest(studentId, ct)` | `GET api/v1/report-cards/latest/{studentId}` � latest card. Faculty+. | `API/Controllers/ReportCardController.cs` |
| `ReportCardController.GetHistory(studentId, ct)` | `GET api/v1/report-cards/history/{studentId}` � history. Faculty+. | `API/Controllers/ReportCardController.cs` |
| `BulkPromotionController.CreateBatch(request, ct)` | `POST api/v1/bulk-promotion/batch` � create draft batch. Admin+. | `API/Controllers/BulkPromotionController.cs` |
| `BulkPromotionController.AddEntries(request, ct)` | `POST api/v1/bulk-promotion/entries` � append student rows. Admin+. | `API/Controllers/BulkPromotionController.cs` |
| `BulkPromotionController.Submit(batchId, ct)` | `POST api/v1/bulk-promotion/submit/{batchId}` � submit for approval. Admin+. | `API/Controllers/BulkPromotionController.cs` |
| `BulkPromotionController.Review(request, ct)` | `POST api/v1/bulk-promotion/review` � approve/reject batch. Admin+. | `API/Controllers/BulkPromotionController.cs` |
| `BulkPromotionController.Apply(request, ct)` | `POST api/v1/bulk-promotion/apply` � execute approved promotion batch. Admin+. | `API/Controllers/BulkPromotionController.cs` |
| `ParentPortalController.GetMyLinkedStudents(ct)` | `GET api/v1/parent-portal/me/students` � read-only parent-linked students list. Authenticated. | `API/Controllers/ParentPortalController.cs` |

  ---

## Phase 27 � University Portal Parity and Student Experience (2026-05-09)

### Stage 27.1 � Portal Capability Matrix

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IPortalCapabilityMatrixService.GetMatrixAsync(policy, ct)` | Returns normalized portal capability matrix across roles, institution policy flags, and module activation state. | `Application/Interfaces/IPortalCapabilityMatrixService.cs` |
| `PortalCapabilityMatrixService.GetMatrixAsync(policy, ct)` | Builds matrix rows from module visibility and institution-type compatibility. | `Application/Services/PortalCapabilityMatrixService.cs` |
| `PortalCapabilitiesController.GetMatrix(ct)` | `GET api/v1/portal-capabilities/matrix` � API surface for capability matrix. | `API/Controllers/PortalCapabilitiesController.cs` |
| `PortalController.PortalCapabilityMatrix(ct)` | Loads API matrix and maps it to web model for portal rendering. | `Web/Controllers/PortalController.cs` |

### Stage 27.2 � Authentication and Security UX

| Function Name | Purpose | Location |
| --- | --- | --- |
| `IAuthService.GetSecurityProfileAsync(ct)` | Exposes runtime auth security capabilities (MFA/SSO/session-risk) to clients. | `Application/Interfaces/IAuthService.cs` |
| `AuthService.GetSecurityProfileAsync(ct)` | Returns deployment security profile from bound `AuthSecurity` options. | `Application/Auth/AuthService.cs` |
| `AuthService.LoginAsync(request, ipAddress, ct)` | Enforces MFA toggle, evaluates session risk, adds richer auth audit events, and returns risk-aware login response fields. | `Application/Auth/AuthService.cs` |
| `AuthController.SecurityProfile(ct)` | `GET api/v1/auth/security-profile` endpoint for adaptive login UX. | `API/Controllers/AuthController.cs` |
| `LoginController.PopulateSecurityProfileAsync(apiBase, ct)` | Reads API security profile and populates login view model flags. | `Web/Controllers/LoginController.cs` |

### Stage 27.3 � Support and Communication Integration Contracts

| Function Name | Purpose | Location |
| --- | --- | --- |
| `ISupportTicketingProvider` | Provider contract for ticketing-related outbound communication events. | `Application/Interfaces/ICommunicationIntegrationContracts.cs` |
| `IAnnouncementBroadcastProvider` | Provider contract for announcement fan-out delivery. | `Application/Interfaces/ICommunicationIntegrationContracts.cs` |
| `IEmailDeliveryProvider` | Provider contract for HTML/template email delivery abstraction. | `Application/Interfaces/ICommunicationIntegrationContracts.cs` |
| `CommunicationIntegrationService.GetProfileAsync(ct)` | Returns active provider keys for ticketing/announcement/email integrations. | `Application/Services/CommunicationIntegrationService.cs` |
| `CommunicationIntegrationsController.GetProfile(ct)` | `GET api/v1/communication-integrations/profile` endpoint for integration profile discovery. | `API/Controllers/CommunicationIntegrationsController.cs` |

