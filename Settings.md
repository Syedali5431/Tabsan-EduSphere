# Settings Guide

## Overview

This project now supports a multi-environment configuration model using a single JSON matrix in src/environments.json.

Supported environments:

- Development
- LocalHost
- Production
- Cloud
- Staging
- Docker
- CI/CD
- VPS
- Testing

Each environment profile defines:

- AppConnectionString
- DatabaseConnectionString

## How Detection Works

Environment detection priority:

1. Environment variables (EDUSPHERE_ENVIRONMENT, ASPNETCORE_ENVIRONMENT, DOTNET_ENVIRONMENT, NODE_ENV)
2. Docker detection (DOTNET_RUNNING_IN_CONTAINER, CONTAINER, /.dockerenv)
3. CI/CD detection (GITHUB_ACTIONS, CI, TF_BUILD, GITLAB_CI, BUILD_BUILDID)
4. Hostname mapping (EnvironmentHostMap)
5. Fallback to DefaultEnvironment

Resolver behavior:

- Strong detections (env variable/docker/ci/hostname) prefer profile values.
- Existing app settings remain valid and are still used for backward compatibility.
- If profile values are missing, startup uses existing configuration keys and fallbacks.

## How to Change Connection Strings

Edit src/environments.json:

- `Environments:[Name]:AppConnectionString`
- `Environments:[Name]:DatabaseConnectionString`

You can also override via environment variables:

- EDUSPHERE_APP_CONNECTION
- EDUSPHERE_APP_BASE_URL
- EDUSPHERE_DB_CONNECTION
- EDUSPHERE_DEFAULT_CONNECTION

## How to Add a New Environment

1. Add a new object under Environments in src/environments.json.
2. Include AppConnectionString and DatabaseConnectionString.
3. Optionally map hosts in EnvironmentHostMap.
4. If needed, set DefaultEnvironment to the new profile.

## Setup Instructions

### Local Development

1. Set ASPNETCORE_ENVIRONMENT=Development (optional).
2. Ensure src/environments.json LocalHost profile is valid.
3. Run the app normally with dotnet run.

### Cloud Deployment

1. Set EDUSPHERE_ENVIRONMENT=Cloud or ASPNETCORE_ENVIRONMENT=Production.
2. Prefer secret-managed overrides for DB credentials.
3. Confirm TLS-enabled database connection settings.

### VPS Hosting

1. Set EDUSPHERE_ENVIRONMENT=VPS.
2. Update VPS profile in src/environments.json.
3. Optionally add hostname mapping under EnvironmentHostMap.

### CI/CD Pipelines

1. Set EDUSPHERE_ENVIRONMENT=CI/CD or rely on GITHUB_ACTIONS/CI auto-detection.
2. Inject EDUSPHERE_DB_CONNECTION from pipeline secrets.
3. Keep profile values non-secret when possible.

### Docker Setup (Step-by-step)

1. Build and run services:
   - docker compose up --build
2. API runs on <http://localhost:8080>
3. Database runs on localhost:14333
4. Docker environment is auto-detected and EDUSPHERE_DB_CONNECTION is injected by compose.

## Example Environment Variables

- EDUSPHERE_ENVIRONMENT=Docker
- ASPNETCORE_ENVIRONMENT=Production
- EDUSPHERE_DB_CONNECTION=Server=db;Database=TabsanEduSphereDb;User Id=sa;Password=***;
- EDUSPHERE_APP_CONNECTION=<https://api.example.com>

## Startup Configuration Validation Checklist

- Ensure `src/environments.json` includes canonical profiles: Development, Testing, Production.
- Ensure each environment profile contains both `AppConnectionString` and `DatabaseConnectionString`.
- Keep `DefaultEnvironment` aligned to a valid profile name (recommended: Development).
- Keep secrets out of `src/environments.json`; inject sensitive values through environment variables or secret stores.
- For integration tests, ensure the test host can load `src/environments.json` from the workspace root.
- Review startup logs to confirm environment detection source is expected and warnings are absent.

## Security Best Practices

- Do not commit real credentials or secrets into JSON files.
- Use environment variables or secret managers for passwords/tokens.
- Keep sample values in source control and inject actual secrets at deploy time.
- Rotate credentials regularly.

## Safety Notes

- Missing profile data does not crash startup by itself.
- Existing settings continue to work as fallback paths.
- Resolver emits startup warnings for missing profile data.
