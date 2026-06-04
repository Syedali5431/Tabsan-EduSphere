# License KeyGen User Guide

Version: 1.7  
Date: 04 June 2026  
Applies to: tools/KeyGen/KeyGen.cs  
Completion Status: Phase 38 complete + ISO 27001 & ISO 9001 Compliance (Phases 1-10)

## 1. Purpose

This guide explains how to use the license creation utility located in tools/KeyGen to generate cryptographic keys and issue licenses for Tabsan EduSphere.

The utility generates:
- RSA private key (PKCS#1 PEM)
- RSA public key (PKCS#1 PEM)
- AES-256 key (Base64)

These keys support secure license generation and verification workflows.

## 1.1 What's New (May 2026)

- Institution-scope-aware licensing remains a core control path for School/College/University enablement.
- Current operational baseline includes stronger module enforcement and institute-scope guardrails across API and portal surfaces.
- Continue using strict key-handling controls: private keys in vault only, public keys in verifier configuration only.

## 1.2 Documentation Baseline (15 May 2026)

- Institution scope remains tri-mode: School, College, University.
- SuperAdmin license workflows should be validated alongside standard deployment scripts Scripts/01 through Scripts/05.
- Consolidated planning and phase history now resides in Docs/Consolidated-Execution-Enhancements-Issues.md.

## 1.3 Final Release Packaging Update (Phase 37/38)

- The runtime app is now published separately from Tabsan.Lic outputs.
- License operations documentation is distributed via the non-runtime asset package.
- Separation evidence references:
	- Artifacts/Phase37/Publish-Separation-20260515.md
	- Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

## 2. Prerequisites

- .NET 8 SDK installed
- Access to repository root
- Permission to handle cryptographic material

Optional but recommended:
- Secret manager or secure vault for key storage
- Controlled workstation (no shared desktop)

## 3. File Location

- Utility source: tools/KeyGen/KeyGen.cs
- Project file: tools/KeyGen/KeyGen.csproj

## 4. Running the Utility

From repository root, run:

1. dotnet run --project tools/KeyGen

The tool prints three outputs to terminal:
- RSA Private Key PEM block
- RSA Public Key PEM block
- AES-256 key in Base64

## 5. Output Meaning

### 5.1 RSA Private Key

Use for signing license payloads.

Critical rule:
- Never commit private key to source control.
- Never place private key on public or shared servers.

### 5.2 RSA Public Key

Use in application verification side to verify signed license data.

This key can be distributed to application environments as needed.

### 5.3 AES-256 Key

Use for symmetric encryption in your license packaging process if your internal workflow requires encrypted payload transfer.

Store and rotate this key per institutional cryptographic policy.

## 6. Recommended Secure Workflow

1. Run KeyGen on a secure machine.
2. Copy private key to vault only.
3. Copy public key to deployment secrets/config used by the app verifier.
4. Store AES key in vault with strict access controls.
5. Record key creation date, owner, and purpose.

## 7. Integrating with License Issuance Process

Suggested process:
1. Build a license payload (organization, license type, issue date, expiry date, module scope).
2. Sign payload with RSA private key.
3. Optionally encrypt payload package if policy requires it.
4. Deliver final license artifact to SuperAdmin.
5. SuperAdmin uploads license in License Update screen.
6. Confirm app shows active license state.

## 8. Verification in EduSphere

After upload, validate:
- License status is Active
- License type matches expected plan
- Expiry date is correct (if non-permanent)
- Modules expected under license are available

If verification fails:
- Re-check key pair consistency
- Confirm payload signature process
- Confirm no corruption during transfer

## 9. Key Rotation Policy (Recommended)

Define a policy covering:
- Rotation interval
- Emergency rotation trigger
- Legacy key grace period
- Revocation and re-issuance process

Minimum operational standard:
- Rotate keys on administrator turnover
- Rotate immediately on suspected key exposure

## 10. Troubleshooting

1. dotnet run fails:
- Verify .NET SDK installation
- Run restore: dotnet restore tools/KeyGen/KeyGen.csproj

2. Output is malformed after copy/paste:
- Ensure PEM header and footer lines remain intact
- Preserve line breaks in key block

3. License rejected by app:
- Check public key configured in verifier
- Check payload signing algorithm compatibility
- Check issue/expiry timestamps and timezone handling

## 11. Security Warnings

- Do not store keys in plain text files inside repository.
- Do not send private keys over chat/email.
- Do not embed private keys in application binaries.
- Audit all key access requests.

## 12. Quick Operational Checklist

Before issuing a license:
- Keys generated and stored securely
- Payload reviewed and approved
- Signature verified
- Upload tested in non-production first

After issuing a license:
- Activation confirmed by SuperAdmin
- Audit record updated
- Renewal reminder scheduled

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Licensing operations should now be validated with tenant and campus scoped governance behaviors in mind for shared deployments.
- SuperAdmin post-license verification should include scoped Program and Report Center behavior checks by tenant/campus context.
- Continue strict key handling while expanding validation checklists to include scope-aware access and settings governance surfaces.

## Phase 40.4 Institution-Aware Certificates Update (2026-05-26)

- License verification should confirm university capability flags because they govern degree/transcript visibility.
- When university mode is disabled by policy/license, university-only certificate actions should not be visible.
- For School/College deployments, validate additional certificate workflows and student download visibility after license upload.

## Phase 40.5 Results and Attendance Governance Update (2026-05-28)

- Post-license validation should include results governance checks: controlled publication, published-only student visibility, and correction reason enforcement.
- Operational smoke tests should verify attendance and result workflows in at least one scoped department per institution mode.
- Demo-path verification now references full dummy marker `DemoDatasetVersion = FullDummyData-v9`.

## 13. Advanced Secure License Operations

This section extends the baseline workflow with enterprise-grade controls.

### 13.1 Key Custody Model

Use a minimum two-person control model for private-key access:

1. Key Custodian A: vault access owner.
2. Key Custodian B: issuance approver.
3. License Operator: prepares payload and executes signing.

No single operator should own all three responsibilities.

### 13.2 License Issuance Workflow (Detailed)

1. Intake request
- Capture customer/institution identity, requested modules, institution mode, tenant/campus constraints, and validity period.

2. Payload preparation
- Build deterministic payload fields in canonical order.
- Verify issue date, expiry date, and timezone normalization.

3. Approval
- Obtain approval for scope and duration.
- Log approver identity and timestamp.

4. Signing
- Sign payload with private key from controlled workstation.
- Verify signature locally using corresponding public key.

5. Packaging and transfer
- If policy requires, encrypt package with AES key.
- Transfer through approved secure channel only.

6. Activation verification
- Upload in environment.
- Validate module visibility and policy behavior.

7. Audit closure
- Record issuance ID, hash/fingerprint, approver, and activation result.

## 14. Validation Checklist by Environment

Run these checks in test and production.

1. License state
- Status is active.
- Scope fields match contract.

2. Module behavior
- Enabled modules visible and accessible.
- Disabled modules hidden and blocked by API.

3. Institution policy behavior
- School/College/University labels and features align with issued scope.

4. Governance behavior
- Sidebar/report settings remain consistent with active license and role policies.

## 15. Rotation and Revocation Procedure

### 15.1 Planned Rotation

1. Generate new key pair.
2. Distribute new public key to verifiers.
3. Issue new licenses signed with rotated private key.
4. Maintain grace period for old key where policy allows.
5. Disable old key after cutover window.

### 15.2 Emergency Revocation

1. Declare incident and freeze new issuance on affected key.
2. Generate emergency replacement key pair.
3. Reissue active licenses on priority order.
4. Publish revocation bulletin to operations owners.
5. Confirm all environments reject old-key signatures.

## 16. Security Baseline Controls (ISO 27001 Enhanced)

- Keep private keys only in HSM or managed vault where available.
- Enforce MFA for all key-access accounts.
- Log every private-key retrieval operation — all key operations are audit-logged with immutable records.
- Rotate AES keys separately from RSA keys.
- Never attach private keys in tickets, email, or chat.
- **ISO 27001 Alignment**: Key management practices comply with ISO 27001 A.10 (Cryptography) controls. All license generation and key rotation events are captured in audit_logs with EventCategory='Security' and appropriate severity level.

## 17. License Audit Record Template (ISO Enhanced)

Minimum audit fields:

- License issuance ID
- Institution/customer identifier
- Scope (modules, institution mode, tenant/campus)
- Issue and expiry dates
- Signing key fingerprint
- Approver and operator identities
- Activation result and verification timestamp
- Incident reference if reissued/revoked
- Audit correlation ID for traceability across systems

## Phase 40.6 ISO 27001 + ISO 9001 Compliance Update (2026-06-04)

- Key management security controls aligned with ISO 27001 A.10 (Cryptography).
- All license generation and key rotation events audit-logged with immutable records.
- Audit record template enhanced with correlation ID for distributed tracing.
- MFA enforcement and private-key vault storage requirements reaffirmed.
- All changes are additive and backward-compatible.

