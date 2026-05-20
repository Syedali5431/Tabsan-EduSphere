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

Stage H1.2 - Load matrix into configuration hierarchy
- Extend configuration bootstrapper to read environments.json from project path and shared src path.
- Support optional override path via EDUSPHERE_ENVIRONMENTS_FILE.
- Keep all new sources optional to avoid startup failures.

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

Stage H2.2 - Add safe override behavior
- Allow env var overrides for app/db strings.
- Prefer profile values only when strong detection signals exist.
- Preserve existing appsettings and legacy fallback behavior.

## Phase H3 - Runtime Integration Without Breaking Existing Logic
Reason this phase is third:
- Integration should happen after resolver behavior is verified and bounded.

Stage H3.1 - Integrate resolver into startup visibility
- API/Web/BackgroundJobs log detected environment and warnings.

Stage H3.2 - Integrate DB and app URL usage safely
- DB resolver uses environment profile as preference only on strong detection.
- Web app base URL can use profile app string while preserving existing EduApi:BaseUrl behavior.
- Keep fail-safe validation and legacy config paths intact.

## Phase H4 - Docker Runtime Enablement
Reason this phase is fourth:
- Docker relies on the resolver and profile system being in place first.

Stage H4.1 - Container build support
- Add API Dockerfile.

Stage H4.2 - Compose app + db topology
- Add docker-compose.yml with:
  - api service
  - db service (SQL Server)
- Use container service name in DB connection string (Server=db;...).
- Ensure environment auto-detection works in containers.

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

Stage H5.2 - Validation checklist
- Confirm no hardcoded credentials.
- Confirm fallback behavior when profiles are missing.
- Confirm no unexpected override of existing app settings.
- Confirm modular isolation and startup safety.

## Safety and Compatibility Guarantees
- No forced replacement of legacy connection keys.
- Optional profile file loading prevents crash on missing files.
- Existing startup fail-safe validation remains active.
- Runtime cost is minimal (single configuration resolution at startup).

## Rollout Notes
- Deploy profile files first.
- Then enable environment variables per target environment.
- For production, inject secrets via environment variables or secret manager.
