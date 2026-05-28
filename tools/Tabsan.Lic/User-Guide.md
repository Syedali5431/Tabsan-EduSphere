# Tabsan-Lic User Guide

## Purpose

This guide explains how to use Tabsan-Lic to generate a secure `.tablic` license file and apply it in Tabsan EduSphere.

Tabsan-Lic is a standalone vendor/operator tool. It is not part of the main EduSphere runtime application.

## Prerequisites

Before using the tool, ensure:

1. You are on a trusted machine (vendor/admin controlled).
2. `.NET 8` is installed (if running from source).
3. You have access to the Tabsan-Lic folder:
   - `tools/Tabsan.Lic/`
4. You understand the target deployment details:
   - license duration
   - institution scope (School/College/University)
   - max concurrent users
   - optional domain lock

## Run the Tool

### Option A: Run from source

From repository root:

```bash
dotnet run --project tools/Tabsan.Lic/Tabsan.Lic.csproj
```

### Option B: Run published executable

Run `tabsan-lic.exe` from the publish folder.

## Wizard Inputs (Step-by-Step)

When the wizard starts, provide the following:

1. **Expiry type**
   - `1` = 1 month
   - `2` = 1 year
   - `3` = 2 years
   - `4` = 3 years
   - `5` = permanent

2. **Customer/Tenant label (optional)**
   - Example: `Demo-University-May-2026`

3. **Max concurrent users**
   - `0` means unlimited
   - Any positive integer enforces a user limit

4. **Allowed domain (optional)**
   - Leave blank for unrestricted activation
   - Example domain lock: `portal.example.edu`

5. **Institution scope flags (required)**
   - Include School? `(y/n)`
   - Include College? `(y/n)`
   - Include University? `(y/n)`
   - At least one must be `y`

After input, the tool generates and signs a `.tablic` file automatically.

## Output Location

The file is saved automatically into the project-level `License` folder.

Canonical repository location:

- `tools/Tabsan.Lic/License/`

Fallback published location:

- `License/` beside `tabsan-lic.exe`

Filename format:

- `tabsan-license-{KeyId}.tablic`

## Security Model (What makes it tamper resistant)

Each generated `.tablic` is:

1. Encrypted (AES-256-CBC)
2. Signed (RSA signature over encrypted payload)
3. Bound with payload integrity data including:
   - verification key hash
   - unique `LicenseId`
   - nonce
   - verification fingerprint

If someone edits the file manually, verification fails during upload.

## Upload License in EduSphere

1. Log in as `SuperAdmin` in EduSphere.
2. Open portal license page:
   - `Portal -> Settings -> License Update`
3. Upload the generated `.tablic` file.
4. Confirm success message and updated license details.

## Validate After Upload

Check the following:

1. License status is active.
2. Expiry date matches selected plan.
3. Institution scope reflects selected School/College/University flags.
4. User concurrency behavior matches `MaxUsers` setting.
5. Domain-restricted licenses activate only on the intended domain.

## Common Scenarios

### Generate unrestricted license

- MaxUsers: `0`
- AllowedDomain: blank
- Scope: choose any required institution flags

### Generate domain-locked license

- AllowedDomain: set target host (for example `portal.example.edu`)
- Ensure activation occurs from same host/domain

### Generate school-only package

- School: `y`
- College: `n`
- University: `n`

## Troubleshooting

### Error: invalid/tampered license file

- Regenerate file and upload immediately.
- Do not edit file contents.
- Do not convert file through editors or text formatters.

### Error: verification key mismatch

- Ensure generator keys and EduSphere validator keys are matching pair.
- Check:
  - `tools/Tabsan.Lic/Crypto/EmbeddedKeys.cs`
  - `src/Tabsan.EduSphere.Infrastructure/Licensing/EmbeddedKeys.cs`

### Error: domain mismatch

- Verify uploaded domain equals generation `AllowedDomain`.
- If unsure, regenerate with blank `AllowedDomain`.

### Error: license reuse rejected

- Verification keys are one-time-use in activation history.
- Generate a new license file.

## Operational Best Practices

1. Keep private signing keys confidential.
2. Generate licenses only on trusted systems.
3. Store generated licenses in secure internal storage.
4. Keep an internal issuance log (customer, date, expiry, constraints).
5. Never commit private key material to public repositories.

## Quick Checklist

1. Run Tabsan-Lic.
2. Fill wizard values correctly.
3. Confirm output file in `license/` folder.
4. Upload in EduSphere as SuperAdmin.
5. Validate status, expiry, scope, and restrictions.
6. Run functional smoke checks for attendance/results governance (published visibility, controlled publish, correction reason flow).
7. For demo path validation, confirm `DemoDatasetVersion = FullDummyData-v9` after full seeding.
