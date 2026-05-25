# Plan L - Safe Add-On 2FA (TOTP) for Existing ASP.NET Core Application

## Objective
Add secure TOTP-based Two-Factor Authentication (Google Authenticator / Authy compatible) as a plug-in extension to the existing ASP.NET Core application without breaking existing authentication or modules.

## Implementation Checkpoint
- The codebase now contains an isolated add-on 2FA surface under `src/Tabsan.EduSphere.API/Services/TwoFactor/` plus `src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs`.
- A dedicated EF-backed 2FA state store is wired through `ITwoFactorStateStore` so the legacy auth controllers remain untouched.
- The add-on exposes setup, verify, disable, and login-verify endpoints, with a commented plug-in insertion point for the existing password-first login flow.
- The current implementation is additive and builds successfully; the remaining work in this plan is now primarily rollout, UI, and test hardening.
- Focused unit coverage now exists for the new 2FA orchestration service, including setup, verify, disable, and login verification flows.
- Integration coverage now passes for the live `/api/v1/2fa` endpoints, and the MFA secret column has been widened to safely store protected secrets at rest.
- Portal-side unit coverage now exists for the new 2FA settings page and setup entrypoint, so the Web shell is covered as well.

## Mandatory Safety Rules
- Do not modify or remove existing authentication logic.
- Do not overwrite existing controllers/services.
- Only add new services, classes, and endpoints.
- Keep implementation modular, isolated, and plug-and-play.
- Follow existing project structure and coding style.
- Clearly comment where integration with existing login flow is required.

## Core Functionality
Implement RFC 6238 TOTP flow:
- Users can enable/disable 2FA.
- Use shared secret and time-based OTP.
- Compatible with Google Authenticator and Authy.

## Required Libraries
- Otp.NET for TOTP generation/validation.
- QRCoder for QR generation.

## Database Safety Rules
- Do not modify existing tables unless necessary.
- If needed, extend user table minimally or create separate extension table.
- Required stored fields:
  - TwoFactorEnabled (bool)
  - TwoFactorSecretKey (encrypted string)

## New Files Only (Output Contract)
Plan L implementation must primarily produce these new files:
- TwoFactorService.cs
- QRCodeService.cs
- TwoFactorSetupService.cs
- TwoFactorController.cs

Optional additional new files are allowed only for safe isolation:
- DTO/request/response models
- repository interface/implementation for 2FA extension state
- options/config classes
- test files

## Phase L1 - Extension Boundary and Non-Breaking Design
Reason to do first:
- Prevent regressions in existing authentication flow.

### Stage L1.1 - Define add-only architecture
- New 2FA services live in isolated extension namespace/folder.
- Existing auth controllers/services remain untouched.

### Stage L1.2 - Define safe integration seam
- Existing login remains Step 1 (username/password).
- Add a lightweight hand-off to 2FA challenge only when enabled.

### Stage L1.3 - Define DI contracts
- Register only new services:
  - TwoFactorService
  - QRCodeService
  - TwoFactorSetupService

Deliverables:
- Add-only architecture note
- Integration seam checklist

## Phase L2 - Data and Persistence Safety
Reason to do second:
- 2FA state storage must be secure and isolated before endpoint work.

### Stage L2.1 - Minimal schema strategy
- Preferred: add nullable fields to user record only if required.
- Alternative: add dedicated 2FA extension table keyed by user id.

### Stage L2.2 - Secure storage policy
- Store secret encrypted at rest.
- Never return plaintext secret in API response.

### Stage L2.3 - Additive migration policy
- Additive migrations only.
- No destructive changes to existing auth tables.

Deliverables:
- Safe schema update plan
- Encryption-at-rest policy

## Phase L3 - New Service Implementation (No Merges)
Reason to do third:
- Core functionality should be encapsulated in dedicated services.

### Stage L3.1 - TwoFactorService
- Generate secret key.
- Validate TOTP codes using Otp.NET.
- Allow time drift window (±30 seconds / ±1 step).

### Stage L3.2 - QRCodeService
- Generate QR image with QRCoder.
- No integration into existing utility classes.

### Stage L3.3 - TwoFactorSetupService
- Build setup payload:
  - QR code image/data
  - manual key
  - otpauth URI
- Keep setup orchestration separate from existing auth service.

Deliverables:
- TwoFactorService.cs
- QRCodeService.cs
- TwoFactorSetupService.cs

## Phase L4 - Enable 2FA Setup Flow
Reason to do fourth:
- User onboarding must be complete before login challenge can be enforced.

### Stage L4.1 - Start setup
- User chooses Enable 2FA.
- System generates secret and QR payload:
  - otpauth://totp/{AppName}:{Email}?secret={Key}&issuer={AppName}

### Stage L4.2 - Show QR + manual key
- Return minimal UI/API setup response.
- Include fallback manual key for authenticator app entry.

### Stage L4.3 - Confirm setup
- User submits first TOTP code.
- If valid:
  - persist encrypted secret
  - set TwoFactorEnabled = true

Deliverables:
- Setup and confirm flow via new controller endpoints

## Phase L5 - Login Flow Extension (Keep Existing Login Intact)
Reason to do fifth:
- Must add 2FA challenge without heavy login refactor.

### Stage L5.1 - Step 1 unchanged
- Keep current username/password validation intact.

### Stage L5.2 - Conditional 2FA challenge
- If TwoFactorEnabled true:
  - redirect to /2fa-verify flow/page

### Stage L5.3 - Complete login on valid TOTP
- User submits TOTP.
- Validate using TwoFactorService.
- If valid: complete existing login finalization path.
- If invalid: show error and increment retry count.

Deliverables:
- Minimal login extension hook (commented integration points only)

## Phase L6 - Security Controls and Abuse Protection
Reason to do sixth:
- 2FA must be secure against common misuse patterns.

### Stage L6.1 - Drift handling
- Accept valid tokens within ±1 step.

### Stage L6.2 - Retry limiting
- Basic failed-attempt protection for setup/login verification.
- Temporary block or throttling after threshold.

### Stage L6.3 - Secret hygiene
- Encrypt before save.
- Do not expose secrets in logs or API responses.

Deliverables:
- Retry limit policy
- Secret exposure prevention checklist

## Phase L7 - New Controller and Endpoints (Add-Only)
Reason to do seventh:
- Endpoint surface must be isolated from existing controllers.

### Stage L7.1 - Add TwoFactorController
- Create new TwoFactorController.cs only.

### Stage L7.2 - Add required endpoints
- GET /2fa/setup
- POST /2fa/verify
- POST /2fa/disable
- POST /2fa/login-verify

### Stage L7.3 - Non-interference guarantees
- Do not modify existing controller methods.
- Use separate route group and DTOs.

Deliverables:
- TwoFactorController.cs
- New request/response contracts

## Phase L8 - UI / Frontend Support (Minimal and Safe)
Reason to do eighth:
- Provide user operability without breaking current UX.

### Stage L8.1 - Setup display
- Minimal QR display page/response.
- Manual key shown as fallback.

### Stage L8.2 - Code input and error handling
- Minimal code entry form for setup verify and login verify.
- Friendly error messages without secret leakage.

### Stage L8.3 - Disable flow UX
- Minimal disable action with confirmation.

Deliverables:
- Basic non-breaking UI/API examples

## Phase L9 - Unit Tests (Additive)
Reason to do ninth:
- Validate correctness without touching existing test baselines.

### Stage L9.1 - Secret generation test
- Non-empty secret generated.
- Format suitable for authenticator setup.

### Stage L9.2 - Valid code test
- Known valid TOTP accepted by validation logic.

### Stage L9.3 - Invalid code test
- Incorrect/expired code rejected.

Deliverables:
- Unit tests for:
  - Generate secret
  - Validate valid code
  - Reject invalid code

## Phase L10 - Plug-In Integration Guide and Rollout
Reason to do last:
- Ensure safe adoption in existing system with low risk.

### Stage L10.1 - Integration snippet guidance
- Provide example snippet that extends login flow minimally:
  - after password check, if 2FA enabled then redirect to /2fa-verify
- Add comments indicating exact plug-in insertion point.

### Stage L10.2 - Startup/DI minimalism
- Register new services only.
- Avoid large startup/auth refactors.

### Stage L10.3 - Rollback safety
- Disable only new routes/services if rollback needed.
- Existing authentication continues unaffected.

Deliverables:
- Safe integration snippet
- Rollback checklist

## Endpoint Contract (New Only)
TwoFactorController endpoints:
- GET /2fa/setup -> generate QR + manual key payload
- POST /2fa/verify -> verify setup code and enable 2FA
- POST /2fa/disable -> disable 2FA and clear secret
- POST /2fa/login-verify -> verify login TOTP challenge

## Example Login Flow Integration Snippet (Plan Guidance)
Pseudo-flow for minimal extension:
1. Existing username/password validation runs unchanged.
2. If user.TwoFactorEnabled is false, complete login as normal.
3. If user.TwoFactorEnabled is true, store pending login context and redirect to /2fa-verify.
4. /2fa/login-verify validates OTP with TwoFactorService.
5. On valid OTP, continue existing login completion path.

Note:
- This snippet is guidance for safe plug-in integration points.
- It does not replace existing authentication implementation.

## Definition of Done (Updated Plan L)
- 2FA is added as modular extension, without breaking existing authentication.
- Existing controllers/services remain intact.
- New files are created for:
  - TwoFactorService.cs
  - QRCodeService.cs
  - TwoFactorSetupService.cs
  - TwoFactorController.cs
- Users can enable, verify, disable, and login-verify 2FA using new endpoints.
- Secrets are encrypted and never exposed in API output.
- Drift window and retry protection are implemented.
- Unit tests cover secret generation and valid/invalid TOTP validation.
- Integration guidance includes clearly commented plug-in insertion points.

## Completed Phase Summaries

### Phase L1 - Extension Boundary and Non-Breaking Design
- Implementation Summary:
  - Introduced an isolated 2FA extension boundary under dedicated API service and controller folders without altering the legacy auth controller path.
  - Registered only the new 2FA services in startup so the feature remains additive and plug-and-play.
- Validation Summary:
  - Full solution build succeeds with the 2FA boundary in place.
  - Existing authentication code paths remain untouched.

### Phase L2 - Data and Persistence Safety
- Implementation Summary:
  - Added protected-at-rest storage for the 2FA secret and widened the user MFA secret column to hold encrypted payloads safely.
  - Kept the persistence change additive and limited to the 2FA fields.
- Validation Summary:
  - Integration tests now persist and retrieve 2FA state successfully.
  - Build remains green after the schema change and migration.

### Phase L3 - New Service Implementation
- Implementation Summary:
  - Added the isolated TOTP, QR, and orchestration services for setup, verification, disable, and login verification flows.
  - Kept all 2FA logic separate from the legacy auth service.
- Validation Summary:
  - Focused unit tests for the 2FA orchestration service pass.
  - Full solution build succeeds.

### Phase L4 - Enable 2FA Setup Flow
- Implementation Summary:
  - Added the setup entrypoint and confirmation flow that returns QR/manual-key payloads and enables 2FA after code validation.
- Validation Summary:
  - API integration tests confirm setup payload generation and code verification.

### Phase L5 - Login Flow Extension
- Implementation Summary:
  - Added the isolated login-verify hand-off endpoint without refactoring the legacy password-first login flow.
- Validation Summary:
  - API integration tests confirm login-verify accepts valid TOTP codes and rejects invalid ones.

### Phase L6 - Security Controls and Abuse Protection
- Implementation Summary:
  - Stored secrets via Data Protection and kept the secret out of API responses after setup starts.
- Validation Summary:
  - Integration tests verify the secret persists encrypted and the application build remains stable.

### Phase L7 - New Controller and Endpoints
- Implementation Summary:
  - Added the dedicated `TwoFactorController` with `/api/v1/2fa/setup`, `/verify`, `/disable`, and `/login-verify` endpoints.
- Validation Summary:
  - Live API integration tests pass for the full 2FA controller surface.

### Phase L8 - UI / Frontend Support
- Implementation Summary:
  - Added the portal 2FA settings page, the profile/menu navigation links, and the web client methods to drive the new endpoints.
- Validation Summary:
  - Portal controller unit tests pass for the 2FA settings and setup entrypoint.

### Phase L9 - Unit Tests
- Implementation Summary:
  - Added focused unit tests for 2FA orchestration and portal-side 2FA setup behavior.
- Validation Summary:
  - `Tabsan.EduSphere.UnitTests` passes for the 2FA unit slices.

### Phase L10 - Plug-In Integration Guide and Rollout
- Implementation Summary:
  - Added the minimal login integration snippet guidance, startup/DI minimalism guidance, and rollback checklist for the add-on 2FA surface.
  - Preserved the non-breaking plug-in design by keeping the integration points commented and optional.
- Validation Summary:
  - The rollout guidance is aligned with the implemented `/api/v1/2fa` surface and the portal-side 2FA flow.
  - The solution build remains green after the final Plan L documentation pass.
