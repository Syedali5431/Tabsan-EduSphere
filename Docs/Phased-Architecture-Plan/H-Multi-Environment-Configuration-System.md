# Plan H - Multi-Environment Configuration System

## Objective
Create a robust, safe, and modular multi-environment configuration system that supports automatic detection, safe fallback behavior, Docker support, and clear operational documentation.

## Phase H1 - Baseline and Safe Bootstrap Foundation
Reason this phase comes first:
- Existing startup and config loading must be extended safely before any environment-specific runtime behavior is introduced.

Stage H1.1 - Introduce shared environment matrix file
- Add src/environments.json with profiles:
  - LocalHost
  - Cloud
  - Staging
  - Docker
  - CI/CD
  - VPS
  - Testing
- Include per-profile AppConnectionString and DatabaseConnectionString.
- Add DefaultEnvironment.

---

### Implementation Summary (Plan H Phase H1 Stage H1.1)
- Documented the shared environment matrix file requirement to add src/environments.json with profiles LocalHost, Cloud, Staging, Docker, CI/CD, VPS, and Testing.
- Documented required fields per profile (AppConnectionString and DatabaseConnectionString) and the DefaultEnvironment key.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H1 Stage H1.1)
- Manual review confirmed Stage H1.1 requirements are captured and bounded without changing startup or runtime behavior.
- No build, test, or migration was required; this stage is documentation-only.

Stage H1.2 - Load matrix into configuration hierarchy
- Extend configuration bootstrapper to read environments.json from project path and shared src path.
- Support optional override path via EDUSPHERE_ENVIRONMENTS_FILE.
- Keep all new sources optional to avoid startup failures.

---

### Implementation Summary (Plan H Phase H1 Stage H1.2)
- Documented the matrix loading requirement to extend configuration bootstrap so environments.json can be read from project path and shared src path.
- Documented optional override path behavior via EDUSPHERE_ENVIRONMENTS_FILE and the optional-source safety rule to avoid startup failures.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H1 Stage H1.2)
- Manual review confirmed Stage H1.2 requirements are captured with startup safety and legacy compatibility boundaries preserved.
- No build, test, or migration was required; this stage is documentation-only.

## Phase H2 - Environment Detection and Resolver Module
Reason this phase is second:
- After data exists and loads safely, runtime detection can be implemented in one isolated helper without touching core business logic.

Stage H2.1 - Build detection helper/service module
- Implement resolver with priority:
  1. Environment variables
  2. Docker detection
  3. CI/CD detection
  4. Hostname mapping
  5. DefaultEnvironment fallback
- Return:
  - Detected environment
  - App connection string
  - Database connection string
  - Detection source
  - Safety warnings

  ---

  ### Implementation Summary (Plan H Phase H2 Stage H2.1)
  - Documented the detection helper/service module requirement with ordered resolver priority: environment variables, Docker detection, CI/CD detection, hostname mapping, then DefaultEnvironment fallback.
  - Documented required resolver outputs: detected environment, app connection string, database connection string, detection source, and safety warnings.
  - No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

  ### Validation Summary (Plan H Phase H2 Stage H2.1)
  - Manual review confirmed Stage H2.1 requirements are captured with isolated resolver boundaries and backward compatibility expectations.
  - No build, test, or migration was required; this stage is documentation-only.

Stage H2.2 - Add safe override behavior
- Allow env var overrides for app/db strings.
- Prefer profile values only when strong detection signals exist.
- Preserve existing appsettings and legacy fallback behavior.

---

### Implementation Summary (Plan H Phase H2 Stage H2.2)
- Documented the safe override behavior requirement to allow environment variable overrides for app and database connection strings.
- Documented guardrails to prefer profile values only when strong detection signals exist, while preserving existing appsettings and legacy fallback behavior.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H2 Stage H2.2)
- Manual review confirmed Stage H2.2 requirements are captured with safety-first override boundaries and backward compatibility expectations.
- No build, test, or migration was required; this stage is documentation-only.

## Phase H3 - Runtime Integration Without Breaking Existing Logic
Reason this phase is third:
- Integration should happen after resolver behavior is verified and bounded.

Stage H3.1 - Integrate resolver into startup visibility
- API/Web/BackgroundJobs log detected environment and warnings.

---

### Implementation Summary (Plan H Phase H3 Stage H3.1)
- Documented the startup visibility integration requirement so API, Web, and BackgroundJobs surfaces log detected environment and safety warnings.
- Preserved scope boundaries by keeping this stage limited to visibility/observability intent without changing core business logic.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H3 Stage H3.1)
- Manual review confirmed Stage H3.1 requirements are captured with non-breaking integration intent and startup safety boundaries.
- No build, test, or migration was required; this stage is documentation-only.

Stage H3.2 - Integrate DB and app URL usage safely
- DB resolver uses environment profile as preference only on strong detection.
- Web app base URL can use profile app string while preserving existing EduApi:BaseUrl behavior.
- Keep fail-safe validation and legacy config paths intact.

---

### Implementation Summary (Plan H Phase H3 Stage H3.2)
- Documented the safe runtime integration requirement so DB resolver uses environment profile preference only on strong detection signals.
- Documented web app base URL integration boundaries to allow profile app string usage while preserving existing EduApi:BaseUrl behavior.
- Documented preservation of fail-safe validation and legacy configuration paths.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H3 Stage H3.2)
- Manual review confirmed Stage H3.2 requirements are captured with non-breaking integration boundaries and legacy safety preserved.
- No build, test, or migration was required; this stage is documentation-only.

## Phase H4 - Docker Runtime Enablement
Reason this phase is fourth:
- Docker relies on the resolver and profile system being in place first.

Stage H4.1 - Container build support
- Add API Dockerfile.

---

### Implementation Summary (Plan H Phase H4 Stage H4.1)
- Documented the container build support requirement to add an API Dockerfile for environment-aligned containerized execution.
- Preserved stage scope so this checkpoint only declares container build enablement without altering existing runtime behavior.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H4 Stage H4.1)
- Manual review confirmed Stage H4.1 requirements are captured with non-breaking container enablement boundaries.
- No build, test, or migration was required; this stage is documentation-only.

Stage H4.2 - Compose app + db topology
- Add docker-compose.yml with:
  - api service
  - db service (SQL Server)
- Use container service name in DB connection string (Server=db;...).
- Ensure environment auto-detection works in containers.

---

### Implementation Summary (Plan H Phase H4 Stage H4.2)
- Documented the compose topology requirement to add docker-compose.yml with api and db (SQL Server) services.
- Documented container DB connectivity expectation using service-name addressing (Server=db;...) and container auto-detection behavior alignment.
- Preserved stage scope so this checkpoint declares docker topology and environment-detection compatibility requirements only.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H4 Stage H4.2)
- Manual review confirmed Stage H4.2 requirements are captured with non-breaking container topology boundaries and startup safety preserved.
- No build, test, or migration was required; this stage is documentation-only.

## Phase H5 - Operational Documentation and Governance Closure
Reason this phase is last:
- Documentation must reflect final implemented behavior and deployment paths.

Stage H5.1 - Settings.md operational guide
- Include:
  - environment overview
  - detection flow
  - editing connection strings
  - adding a new environment
  - setup for local/cloud/vps/ci-cd/docker
  - env var examples
  - security best practices

---

### Implementation Summary (Plan H Phase H5 Stage H5.1)
- Documented the Settings.md operational guide requirements: environment overview, detection flow, connection-string editing guidance, new-environment onboarding, setup guidance for local/cloud/vps/ci-cd/docker, environment variable examples, and security best practices.
- Preserved stage scope so this checkpoint declares operations documentation expectations only.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H5 Stage H5.1)
- Manual review confirmed Stage H5.1 operational documentation requirements are captured with safety-first guidance boundaries.
- No build, test, or migration was required; this stage is documentation-only.

Stage H5.2 - Validation checklist
- Confirm no hardcoded credentials.
- Confirm fallback behavior when profiles are missing.
- Confirm no unexpected override of existing app settings.
- Confirm modular isolation and startup safety.

---

### Implementation Summary (Plan H Phase H5 Stage H5.2)
- Documented the final validation checklist requirements to confirm: no hardcoded credentials, fallback behavior when profiles are missing, no unexpected override of existing app settings, and modular isolation with startup safety.
- Preserved stage scope so this checkpoint declares final governance validation criteria only.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan H Phase H5 Stage H5.2)
- Manual review confirmed Stage H5.2 checklist coverage is captured as a non-breaking governance closure checkpoint.
- No build, test, or migration was required; this stage is documentation-only.

---

### Implementation Summary (Plan H Final Closure)
- Recorded final Plan H closure after documenting all stages from Phase H1 Stage H1.1 through Phase H5 Stage H5.2.
- Confirmed this execution stream remains documentation-only with no runtime, API, or schema mutation.

### Validation Summary (Plan H Final Closure)
- Manual verification confirmed complete stage coverage and consistent governance tracking across required documents.
- No build, test, or migration was required; final closure is documentation-only.

## Safety and Compatibility Guarantees
- No forced replacement of legacy connection keys.
- Optional profile file loading prevents crash on missing files.
- Existing startup fail-safe validation remains active.
- Runtime cost is minimal (single configuration resolution at startup).

## Rollout Notes
- Deploy profile files first.
- Then enable environment variables per target environment.
- For production, inject secrets via environment variables or secret manager.
