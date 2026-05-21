# License KeyGen User Guide

Version: 1.5  
Date: 15 May 2026  
Applies to: tools/KeyGen/KeyGen.cs  
Completion Status: Phase 38 complete (final separation baseline)

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
