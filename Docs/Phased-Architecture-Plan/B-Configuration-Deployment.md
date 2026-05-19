# Phase Plan: B. Configuration + Deployment

## Overview
Enhance the application to fully support environment-based configuration management, focusing on safe and flexible database connection handling for all deployment scenarios.

---

## Phases & Stages

### Phase 1: Configuration Structure
- **Stage 1.1:** Ensure base + environment-specific configuration hierarchy
- **Stage 1.2:** Implement clean separation and fallback logic

#### Phase 1 Implementation Summary (2026-05-19)
- Introduced shared startup configuration bootstrap helper `AddEduSphereConfigurationHierarchy` in application services.
- Standardized configuration source order across API, Web, and BackgroundJobs startup flows.
- Enforced consistent hierarchy: `appsettings.json` -> `appsettings.{Environment}.json` -> `appsettings.Local.json` -> prefixed environment variables (`EDUSPHERE_`) -> unprefixed environment variables.

#### Phase 1 Validation Summary (2026-05-19)
- `dotnet build Tabsan.EduSphere.sln -v minimal` passed.
- unit tests passed (`151/151`).
- integration tests passed (`236/236`).
- contract tests passed (`1/1`).
- total automated validations passed (`388/388`).

### Phase 2: Database Connection Management
- **Stage 2.1:** Remove hardcoded connection strings
- **Stage 2.2:** Support overrides via environment variables and deployment settings
- **Stage 2.3:** Auto-detect environment and load correct settings

### Phase 3: Secure Configuration Handling
- **Stage 3.1:** Use environment variables for secrets in production
- **Stage 3.2:** Support external configuration sources
- **Stage 3.3:** Prevent credentials in logs/errors

### Phase 4: Deployment Flexibility
- **Stage 4.1:** Support cloud, customer-hosted, and multi-instance deployments
- **Stage 4.2:** Allow per-customer DB, domain, and scaling

### Phase 5: Customer Deployment Support
- **Stage 5.1:** Enable customer-specific config without code changes
- **Stage 5.2:** Support config via app settings, env vars, deployment pipeline

### Phase 6: Tenant + Campus Aware Configuration
- **Stage 6.1:** Prepare for per-tenant settings and isolation

### Phase 7: Fail-Safe Behavior
- **Stage 7.1:** Provide clear error messages for missing config
- **Stage 7.2:** Fail early with meaningful logs

### Phase 8: Performance & Stability
- **Stage 8.1:** Avoid unnecessary config reloads
- **Stage 8.2:** Cache config where appropriate

### Phase 9: Logging & Visibility
- **Stage 9.1:** Log active environment, config source, and DB type (not credentials)

### Phase 10: Validation & Finalization
- **Stage 10.1:** Validate readiness for all environments
- **Stage 10.2:** Final review for security and scalability

---

## Key Rules
- Do NOT break existing functionality
- Do NOT hardcode configuration values
- Ensure backward compatibility and security
