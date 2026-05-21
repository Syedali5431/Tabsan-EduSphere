# Plan I - Build and Publish Exclusion Optimization

## Objective
Reduce build and publish payload size by excluding non-runtime files from output packages while retaining all files in the repository.

## Phase I1 - Inventory and Runtime Safety Baseline
Reason to do first:
- Exclusions must be safe and avoid runtime regressions.

Stage I1.1 - Identify file categories
- Documentation and notes
- Tests and sample/demo assets
- Debug/temp/backup files
- Local-only config
- Non-runtime scripts

---

### Implementation Summary (Plan I Phase I1 Stage I1.1)
- Documented the file-category inventory requirement covering documentation/notes, tests and sample/demo assets, debug/temp/backup files, local-only configuration, and non-runtime scripts.
- Preserved stage scope so this checkpoint defines exclusion target categories only, without applying exclusions yet.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan I Phase I1 Stage I1.1)
- Manual review confirmed Stage I1.1 category boundaries are captured with runtime-safety intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

Stage I1.2 - Protect runtime-critical files
- Keep appsettings runtime files intact
- Keep licensing runtime inputs intact
- Do not touch core logic or code paths

---

### Implementation Summary (Plan I Phase I1 Stage I1.2)
- Documented runtime-protection guardrails to keep appsettings runtime files and licensing runtime inputs intact while preserving core logic and existing code paths.
- Preserved stage scope so this checkpoint sets non-destructive exclusion safety boundaries before project-level exclusion rules.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan I Phase I1 Stage I1.2)
- Manual review confirmed Stage I1.2 runtime-critical protection boundaries are captured with backward-compatibility intent preserved.
- No build, test, or migration was required; this stage is documentation-only.

## Phase I2 - Project-Level Publish Exclusions (.csproj)
Reason to do second:
- Publish exclusion should be explicit and deterministic at project level.

Stage I2.1 - Runtime projects
- Apply `CopyToOutputDirectory=Never` and `CopyToPublishDirectory=Never` patterns in:
  - API
  - Web
  - BackgroundJobs

---

### Implementation Summary (Plan I Phase I2 Stage I2.1)
- Documented project-level exclusion policy for runtime projects (API, Web, BackgroundJobs) using `CopyToOutputDirectory=Never` and `CopyToPublishDirectory=Never` patterns for non-runtime assets.
- Preserved stage scope so this checkpoint defines deterministic publish/output exclusion behavior at project configuration level only.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan I Phase I2 Stage I2.1)
- Manual review confirmed Stage I2.1 project-level exclusion policy is documented with runtime-safety boundaries preserved.
- No build, test, or migration was required; this stage is documentation-only.

Stage I2.2 - License projects
- Apply the same policy to:
  - tools/Tabsan.Lic
  - tools/KeyGen
- Ensure exclusion policy does not alter licensing logic.

---

### Implementation Summary (Plan I Phase I2 Stage I2.2)
- Documented extension of the project-level exclusion policy to license projects (`tools/Tabsan.Lic` and `tools/KeyGen`) using the same deterministic non-runtime asset exclusion approach.
- Documented explicit safety boundary that exclusion policy must not alter licensing logic or runtime license behavior.
- No runtime logic, API, or schema changes were implemented; this stage is documentation-only.

### Validation Summary (Plan I Phase I2 Stage I2.2)
- Manual review confirmed Stage I2.2 license-project exclusion boundaries are captured with licensing safety and backward compatibility preserved.
- No build, test, or migration was required; this stage is documentation-only.

## Phase I3 - Container Context Minimization (.dockerignore)
Reason to do third:
- Docker images should avoid docs/tests/scripts and transient files.

Stage I3.1 - Add root .dockerignore
- Exclude docs, test suites, temp files, logs, and local-only config.
- Preserve required source/runtime assets only.

## Phase I4 - Validation and Release Safety
Reason to do last:
- Confirm optimization is non-destructive and production-safe.

Stage I4.1 - Build and publish validation
- Run solution build
- Run targeted publish for core app and license module

Stage I4.2 - Output verification
- Verify excluded patterns are absent from publish output
- Confirm application startup paths remain functional

## Safety Guarantees
- No file deletion from repository
- Backward compatibility preserved
- Runtime references protected
- Licensing functionality unchanged

## Exclusion Pattern Summary
- *.md (documentation)
- tests/**
- Scripts/**
- Docs/** and planning doc folders
- *.log, *.tmp, *.old, *.bak
- appsettings.Local.json
- launchSettings.json
