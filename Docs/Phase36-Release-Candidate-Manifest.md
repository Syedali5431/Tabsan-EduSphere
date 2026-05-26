# Phase 36 Release Candidate Manifest

## Scope
- Phase: `36.1 - Release Candidate Baseline Freeze`
- Date: `2026-05-15`
- Environment target: `Staging/UAT -> Production`

## Release Candidate Identity
- RC tag: `rc-20260515-stage36-1`
- RC commit SHA: `b1670b8d0edadd55793ab5895f3b2c24a37741c2`
- Commit summary: `Harden Command.md completion and sync policy`

## Deployment Units
- API service:
  - Project: `src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj`
  - Runtime: `.NET 8`
  - Version source: `Tabsan.EduSphere.API.csproj` (`Version`/`AssemblyVersion` if set)
- Web service:
  - Project: `src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj`
  - Runtime: `.NET 8`
  - Version source: `Tabsan.EduSphere.Web.csproj` (`Version`/`AssemblyVersion` if set)
- Background jobs service:
  - Project: `src/Tabsan.EduSphere.BackgroundJobs/Tabsan.EduSphere.BackgroundJobs.csproj`
  - Runtime: `.NET 8`
  - Version source: `Tabsan.EduSphere.BackgroundJobs.csproj` (`Version`/`AssemblyVersion` if set)

All deployment units are pinned to the same RC commit SHA listed above.

## Runtime and Platform Prerequisites
- .NET SDK/Runtime: `8.x`
- SQL Server: target compatible with existing deployment scripts and EF migrations
- Reverse proxy/load balancer: configured for forwarded headers
- TLS termination and secure cookie policy enabled in production
- Optional infrastructure:
  - Redis (distributed cache)
  - RabbitMQ (queue provider)
  - Blob/local media storage provider

## Feature and Module Baseline
- Institution-aware mode behavior enabled (School/College/University via policy and license)
- Role and menu guard parity active (API + portal)
- Security baseline active:
  - privileged-role MFA policy support
  - audit and compliance logging endpoint
  - module-license route gating
- User import UX baseline active from Admin Users flow

## Required Environment Variables and Secrets
- Authentication and token security:
  - JWT signing key/secret (non-placeholder)
- Email integration:
  - SMTP credentials/host/port (non-placeholder for enabled email dispatch)
- SMS integration (optional when enabled):
  - `TWILIO_ACCOUNT_SID`
  - `TWILIO_AUTH_TOKEN`
  - `TWILIO_PHONE_NUMBER`
- Media security:
  - signed URL secret (non-placeholder when signed URL enforcement is enabled)
- Data/cache/queue connectivity:
  - SQL connection string
  - Redis connection string (if distributed cache enabled)
  - RabbitMQ connection string/credentials (if queue provider enabled)

## Configuration Consistency Checks (Pre-Deploy)
- Appsettings parity confirmed across:
  - `appsettings.json`
  - `appsettings.Development.json`
  - `appsettings.Production.json`
- Startup safety checks must reject unsafe placeholder secrets in non-development environments.
- Queue/cache/storage provider selections must match deployment environment capabilities.

## Script Name Compatibility Policy (Pre-Deploy)
- Accepted schema script names:
  - `Scripts/01-Schema-Current.sql` (maintained source)
  - `Scripts/01-Schema.sql` (canonical compatibility alias)
- Accepted core seed script names:
  - `Scripts/02-Seed-Core.sql` (maintained source)
  - `Scripts/02-CoreSeed.sql` (canonical compatibility alias)
- Release checklists and operator runbooks may use either accepted name; both must remain functionally equivalent.
- CI/deployment automation must fail fast if none of the accepted names exist for a required stage.

## Stage 36.1 Validation Evidence
- RC SHA captured from git and frozen in this manifest.
- RC tag creation command:
  - `git tag -a rc-20260515-stage36-1 b1670b8d0edadd55793ab5895f3b2c24a37741c2 -m "Phase 36.1 release candidate baseline"`
- Build baseline command:
  - `dotnet build Tabsan.EduSphere.sln -v minimal`

## Notes
- Scope is frozen to bug fixes and production blockers only after RC tag creation.
- Any post-RC functional enhancement requires explicit release decision and new RC tag.
