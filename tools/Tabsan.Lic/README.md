# Tabsan-Lic — License Generation Tool

A **standalone .NET 8 console application** for generating encrypted `.tablic` license files for Tabsan EduSphere.

Repository Sync Note (15 May 2026):
- License generation workflow remains unchanged and compatible with both Demo and Clean deployment modes of EduSphere runtime environments.

## ⚠️ Important: Separate Application

**Tabsan-Lic is NOT part of the main EduSphere application.**

- **Location**: `tools/Tabsan.Lic/`
- **Solution**: `tools/Tabsan.Lic/Tabsan.Lic.sln` (independent)
- **Main EduSphere Solution**: `Tabsan.EduSphere.sln` (does NOT include Tabsan-Lic)
- **Deployment**: Build and publish separately from the main EduSphere app

## Isolation

✅ Tabsan-Lic is excluded from `dotnet publish Tabsan.EduSphere.sln`  
✅ Tabsan-Lic has its own dedicated solution file  
✅ No dependencies flow back to the main EduSphere codebase

## Building Tabsan-Lic

### From the root directory:
```bash
dotnet build tools/Tabsan.Lic/Tabsan.Lic.sln
```

### From the tool directory:
```bash
cd tools/Tabsan.Lic
dotnet build
```

## Publishing Tabsan-Lic

Publish as a self-contained executable:

```bash
dotnet publish tools/Tabsan.Lic/Tabsan.Lic.sln -c Release -r win-x64 --self-contained
```

Output: `tools/Tabsan.Lic/bin/Release/net8.0/win-x64/publish/tabsan-lic.exe`

## Running Tabsan-Lic

Single-step wizard:

```bash
./tabsan-lic.exe
```

Or from source:

```bash
cd tools/Tabsan.Lic
dotnet run
```

Wizard prompts include:

1. Expiry type (1 month, 1/2/3 years, permanent)
2. Customer/tenant label (optional)
3. Max users (0 = unlimited)
4. Allowed domain (optional)
5. Institution scope flags:
	- Include School? (y/n)
	- Include College? (y/n)
	- Include University? (y/n)

The tool then creates a signed `.tablic` automatically.

## Database

- **Location**: `%APPDATA%/Tabsan/tabsan_lic.db` (SQLite)
- **Auto-created** on first run
- Contains: issued keys, hashes, expiry types, institution scope, generation timestamps, labels

## Features

1. **Single Guided License Generation** — No menu branching for key-only operations
2. **Institution Scope Selection** — School/College/University Y/N prompts
3. **Auto Output Folder** — Saves generated files to `license/` under the app runtime directory
4. **Tamper-Resistant `.tablic`** — AES-256-CBC encryption + RSA signature validation
5. **Verification Key Binding** — Payload includes verification fingerprint derived from signing key

## Generated License File Location

On each run, generated licenses are saved automatically to:

- `tools/Tabsan.Lic/License/` (canonical project-level output folder when running from this repository)
- `License/` beside the executable when the tool cannot resolve the project folder path

Filename format:

- `tabsan-license-{KeyId}.tablic`

## Architecture

| Component | Purpose |
|-----------|---------|
| `Crypto/LicCrypto.cs` | AES-256-CBC + RSA-2048 PKCS#1 encryption/signing |
| `Services/KeyService.cs` | Key generation, storage, listing |
| `Services/LicenseBuilder.cs` | `.tablic` builder + verification fingerprint embedding |
| `Data/LicDb.cs` | SQLite EF Core context |

## Deployment Checklist

- [x] Build `tools/Tabsan.Lic/Tabsan.Lic.sln` independently
- [x] Test wizard-based `.tablic` generation and upload into EduSphere
- [x] Publish as standalone executable
- [x] Distribute to vendor/Super Admin only
- [x] Verify EduSphere build does NOT include Tabsan-Lic binaries
- [x] Document deployment path (vendor internal tool, not part of portal)

Evidence (2026-05-19 verification):

- `Tabsan.EduSphere.sln` contains only EduSphere app/test projects and excludes Tabsan.Lic.
- `Artifacts/Phase37/Publish-Separation-20260515.md` reports PASS for API, Web, BackgroundJobs, and LicenseApp as separate publish targets.
- `Docs/Phase37-Phase38-Publish-Separation.md` documents app/runtime and license publish roots as separate delivery sets.
- `UAT-SAT docs/UAT-Test.md` includes acceptance cases for `.tablic` generation and successful upload/activation in EduSphere.
- `UAT-SAT docs/Phase36-Stage36.5-Approval-Pack.md` records final UAT and SAT conclusions as PASS for release readiness.

## Interaction with EduSphere

**Tabsan-Lic generates `.tablic` files** → **Super Admin uploads to EduSphere** → **EduSphere validates & applies license**

- EduSphere imports only the RSA public key + AES key (embedded in `Infrastructure/Licensing/EmbeddedKeys.cs`)
- Tabsan-Lic keeps the RSA private key (never shared)
- One-way flow: Tabsan-Lic → `.tablic` file → EduSphere

## Troubleshooting

### License generated but upload fails in EduSphere

Possible causes:

1. **File was modified after generation**
	- `.tablic` files are RSA-signed. Any edit (even one byte) breaks signature validation.
	- Regenerate and upload the new file without modifying it.

2. **Key mismatch between tool and app**
	- Tabsan-Lic signs using the private key in `tools/Tabsan.Lic/Crypto/EmbeddedKeys.cs`.
	- EduSphere verifies with the public key in `src/Tabsan.EduSphere.Infrastructure/Licensing/EmbeddedKeys.cs`.
	- These key pairs must match.

3. **Domain restriction mismatch**
	- If `AllowedDomain` was set during generation, upload/activation must occur on that same domain.
	- Leave domain blank for unrestricted licenses.

4. **Verification key fingerprint mismatch**
	- Payload now includes a verification fingerprint bound to the signing key.
	- If runtime key material differs from generator key material, activation is rejected.

5. **Replay/one-time key consumption**
	- A consumed verification-key hash cannot be reactivated.
	- Generate a new license file for a new activation.

### License file not found after generation

- Default output folder is: `tools/Tabsan.Lic/License/`.
- Fallback for non-repo runtime is `License/` beside the running executable.

### App says license expired unexpectedly

- Recheck selected expiry option in generator.
- Generate and upload a fresh file with correct expiry.
- Confirm server UTC time is accurate on the target environment.

### Signature/key validation checklist

1. Generate a new `.tablic`.
2. Upload immediately without editing or transferring through text converters.
3. Confirm license app and EduSphere use matching embedded key pair files.
4. If domain-locked, activate from the exact same host/domain.
