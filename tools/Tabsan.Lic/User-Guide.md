# Tabsan-Lic User Guide

Version: 1.2
Date: 02 June 2026
Applies to: tools/Tabsan.Lic (license generation utility)

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

## Detailed Issuance SOP

Use this end-to-end SOP for production-safe license operations.

### 1. Intake and Validation

Before opening the tool, confirm:

1. Customer or tenant identifier
2. Requested duration (time-bound or permanent)
3. Institution scope requirements (School/College/University)
4. Max concurrent user requirement
5. Domain lock requirement
6. Approval reference from authorized approver

### 2. License Generation

1. Launch Tabsan-Lic.
2. Enter approved values only.
3. Re-read inputs before final generation.
4. Generate `.tablic` output.
5. Save and checksum the resulting file for internal tracking.

### 3. Handover for Activation

1. Transfer generated file through approved secure channel.
2. Provide activation instructions to SuperAdmin.
3. Record handover time and receiver identity.

### 4. Activation Verification

After SuperAdmin upload, verify:

1. Status = active
2. Expiry = expected value
3. Institution mode behavior = expected scope
4. Module visibility = aligns with active license features
5. Domain lock behavior = enforced when configured

### 5. Audit Closure

Record issuance details in your internal license ledger:

1. Issuance ID
2. License file name
3. Customer/tenant label
4. Expiry option used
5. Scope flags used
6. Max users value
7. Domain lock value
8. Issuer and approver identities
9. Activation verification timestamp

## Input Governance Rules

Use these rules to avoid invalid issuance.

1. Scope rule
- At least one of School/College/University must be enabled.

2. Max users rule
- `0` means unlimited.
- Positive values should match approved subscription policy.

3. Domain lock rule
- Use exact host only.
- Do not include protocol.
- Example valid: `portal.example.edu`
- Example invalid: `https://portal.example.edu`

4. Label rule
- Use consistent pattern for traceability.
- Suggested format: `<Customer>-<Plan>-<YYYYMMDD>`

## Security and Key Handling Standards

Tabsan-Lic depends on embedded key material and signature verification consistency. Follow strict controls:

1. Keep key-bearing source files restricted to authorized maintainers.
2. Do not copy key material into tickets, chat, or email.
3. Do not run generation on untrusted/shared machines.
4. Rotate keys on incident or planned lifecycle schedule.
5. Revalidate verifier/generator key parity after any key update.

### Key Parity Validation

When troubleshooting verification mismatches, compare both files:

1. `tools/Tabsan.Lic/Crypto/EmbeddedKeys.cs`
2. `src/Tabsan.EduSphere.Infrastructure/Licensing/EmbeddedKeys.cs`

Ensure both represent matching trusted key material for signing/verification pair alignment.

## Operational Scenarios (Expanded)

### Scenario A: Trial License (Short Duration)

Recommended pattern:

1. Select 1 month
2. Set explicit tenant label with date
3. Apply domain lock for trial host when possible
4. Keep max users constrained as approved

### Scenario B: Enterprise Permanent License

Recommended pattern:

1. Select permanent
2. Apply exact approved institution scopes
3. Use `0` max users only if contractually approved
4. Apply domain lock where infrastructure is stable

### Scenario C: Multi-Institution Scope Rollout

Recommended pattern:

1. Enable multiple scope flags as contracted
2. Verify UI behavior in each institution context after activation
3. Validate report and module visibility in each mode

## Revocation and Re-Issuance Playbook

Use this when a license must be replaced due to policy or incident.

1. Mark existing issuance as revoked in ledger.
2. Create replacement issuance with new label and timestamp.
3. Activate replacement in target environment.
4. Verify old file is no longer in operational use.
5. Record incident and corrective actions.

## Post-Activation Validation Matrix

Validate these core behaviors after each activation:

1. Login and dashboard access for all primary roles
2. Module visibility parity in sidebar and API behavior
3. Attendance/result governance expectations
4. Report access boundaries by role and scope
5. Institution-aware labeling behavior (Semester/Grade/Year)

## Failure Response Guide

### 1. Upload accepted but behavior incorrect

1. Confirm uploaded file is latest generated artifact.
2. Confirm environment did not cache an old license state.
3. Re-check scope and restriction values used at generation.

### 2. Signature verification failure

1. Confirm file was not modified after generation.
2. Confirm key parity between generator and verifier.
3. Generate a fresh file from trusted environment and retry.

### 3. Unexpected access denials after activation

1. Validate role and module configuration (license is not the only gate).
2. Validate sidebar/report settings mappings.
3. Validate tenant/campus scope for affected user.

## Recommended Internal Templates

### License Issuance Record

- Issuance ID:
- Date/Time:
- Issuer:
- Approver:
- Customer/Tenant:
- Duration:
- Max Users:
- Domain Lock:
- Scope Flags:
- Output Filename:
- Activation Verified By:
- Notes:

### License Incident Record

- Incident ID:
- Detection Time:
- Reported By:
- Environment:
- Symptom:
- Root Cause:
- Corrective Action:
- Re-Issuance File:
- Closure Time:

## Release and Repository Notes

1. Tabsan-Lic is delivered separately from runtime web/API publish outputs.
2. Update this guide whenever license flow or security controls change.
3. Keep operational and training documents synchronized across release phases.
