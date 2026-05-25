# Plan L - Two-Factor Authentication (2FA) with TOTP for ASP.NET Core

## Objective
Implement production-ready Two-Factor Authentication (2FA) using TOTP (Google Authenticator / Authy compatible) in the existing ASP.NET Core web application, with clean architecture boundaries, secure key handling, and login flow integration.

## Scope Summary
This plan covers:
- User enable/disable 2FA from account settings
- Per-user secret generation and secure storage
- QR code generation plus manual key fallback
- RFC 6238 TOTP verification flow
- Login pipeline split into password step and TOTP step
- Time drift allowance and brute-force attempt controls
- Service/controller/UI/test deliverables
- Dependency injection and secure coding best practices

## Standards and Libraries
- TOTP library: Otp.NET
- QR generation: QRCoder
- Standard: RFC 6238 TOTP

## Data Contract Requirements
Per user store:
- TwoFactorEnabled (bool)
- TwoFactorSecretKey (encrypted string)

Recommended additional fields for security and operations:
- TwoFactorEnabledAtUtc (datetime, nullable)
- TwoFactorFailedAttempts (int)
- TwoFactorLockoutEndUtc (datetime, nullable)
- TwoFactorRecoveryUpdatedAtUtc (datetime, nullable)

## Phase L1 - Architecture Baseline and Security Design
Reason to do first:
- 2FA impacts identity, persistence, UI, and auth flow. Design alignment is required before implementation.

### Stage L1.1 - Define clean architecture boundaries
- Domain: user security state and invariants
- Application: 2FA services and use cases
- Infrastructure: encryption provider, OTP/QR adapters
- Web/API: controller endpoints and UI pages

### Stage L1.2 - Define threat model and controls
- Protect secret at rest (encryption)
- Prevent brute-force via failed attempt throttling
- Enforce session-bound pending 2FA state before full sign-in
- Avoid logging secrets, OTP codes, or otpauth URLs

### Stage L1.3 - Define DI and configuration contracts
- ITwoFactorService
- ISecretProtectionService
- IQRCodeService
- ILoginChallengeService
- Settings for issuer name, OTP digits, step window, lockout thresholds

Deliverables for Phase L1:
- Security architecture note
- Service interfaces and options model
- Threat-control checklist

## Phase L2 - Domain and Database Preparation
Reason to do second:
- User model and persistence must exist before services and flow integration.

### Stage L2.1 - Extend user domain model
- Add required fields:
  - TwoFactorEnabled
  - TwoFactorSecretKey (encrypted payload)
- Add guard methods:
  - EnableTwoFactor(secret)
  - DisableTwoFactor()
  - RegisterFailedTwoFactorAttempt()
  - ResetTwoFactorFailures()

### Stage L2.2 - Persistence mapping and migration
- Add EF model mappings and migration scripts
- Set secure defaults:
  - TwoFactorEnabled default false
  - Secret key nullable until setup complete
- Add index considerations for lockout queries if needed

### Stage L2.3 - Backward compatibility
- Existing users continue normal login until they opt in
- No forced 2FA rollout unless feature flag says otherwise

Deliverables for Phase L2:
- Updated user entity and mappings
- Database migration
- Rollback-safe migration notes

## Phase L3 - Secret Generation and Secure Storage
Reason to do third:
- Secure key lifecycle is foundational for all 2FA operations.

### Stage L3.1 - Secret generation service
- Generate random high-entropy secret bytes
- Encode manual key in Base32 for authenticator apps

### Stage L3.2 - Encryption at rest
- Encrypt secret before database write
- Decrypt only when validating code
- Use existing key management strategy (e.g., ASP.NET Core Data Protection or managed key vault abstraction)

### Stage L3.3 - Secret handling controls
- No plaintext storage in DB
- No plaintext exposure in logs
- Clear in-memory buffers where practical

Deliverables for Phase L3:
- Secret generation implementation
- Secret protection implementation
- Secure storage policy documentation

## Phase L4 - QR Code and Setup UX
Reason to do fourth:
- User onboarding needs scan + manual fallback prior to enabling 2FA.

### Stage L4.1 - otpauth URI generation
- Build URI: otpauth://totp/{Issuer}:{Username}?secret={Base32}&issuer={Issuer}&digits=6&period=30
- Ensure issuer and account labels are URL-safe

### Stage L4.2 - QR code rendering
- Generate QR image with QRCoder
- Return as PNG or data URL for page rendering

### Stage L4.3 - Manual key fallback
- Show Base32 manual key on setup page
- Add copy UI support and guidance for Google Authenticator/Authy

Deliverables for Phase L4:
- QRCode service adapter
- Setup view model with QR + manual key
- Setup page sample UI

## Phase L5 - TOTP Validation Engine (RFC 6238)
Reason to do fifth:
- Verification behavior must be deterministic and secure before login integration.

### Stage L5.1 - Build TwoFactor service
- Generate setup payload (secret + manual key + QR URI)
- Validate TOTP using Otp.NET
- Use time-step window allowing drift ±1 step

### Stage L5.2 - Confirmation workflow
- Enable setup starts in pending mode
- User must submit valid first code to confirm setup
- Only then set TwoFactorEnabled true

### Stage L5.3 - Disable workflow
- Require password re-check or current TOTP before disable
- Clear encrypted secret and reset attempt counters

Deliverables for Phase L5:
- TwoFactor service class
- Setup confirm/disable logic
- Validation policy configuration

## Phase L6 - Login Flow Integration
Reason to do sixth:
- Core auth behavior must include 2-step verification for enabled users.

### Stage L6.1 - Step 1 username/password
- Validate credentials as existing flow
- If 2FA disabled: complete login
- If 2FA enabled: create pending challenge state and redirect to code entry

### Stage L6.2 - Step 2 TOTP challenge
- Prompt for 6-digit code
- Validate against decrypted user secret
- On success: complete sign-in and clear pending challenge

### Stage L6.3 - Failed-attempt controls
- Increment failed 2FA attempts on invalid code
- Lock out challenge after threshold (basic brute-force protection)
- Return generic error messages to avoid information leakage

Deliverables for Phase L6:
- Login challenge state manager
- Updated login controller/page flow
- 2FA verification endpoint/page

## Phase L7 - Account Settings and Endpoints/UI
Reason to do seventh:
- Users/admin need complete operational controls after core flow is integrated.

### Stage L7.1 - Enable 2FA endpoint/page
- Start setup and return QR + manual key
- Accept initial verification code for confirmation

### Stage L7.2 - Disable 2FA endpoint/page
- Confirm user intent and authentication proof
- Disable and purge secret material

### Stage L7.3 - Login verification endpoint/page
- Handle code submission for pending login challenge
- Handle lockout timer feedback

Minimum UI artifacts:
- Account security page with 2FA status and actions
- Setup page showing QR image and manual key
- Login step-2 page for OTP entry

Deliverables for Phase L7:
- Endpoints or Razor Pages for enable/verify/disable/login-step
- Basic UI examples with validation messages

## Phase L8 - Middleware and Auth Pipeline Hardening
Reason to do eighth:
- Ensure partial authentication states cannot access protected resources.

### Stage L8.1 - Pending-2FA session policy
- Pending users are not treated as fully authenticated
- Block sensitive routes until challenge success

### Stage L8.2 - Cookie/token claims update
- Add claim flag when 2FA verified for the session
- Regenerate auth cookie on final sign-in

### Stage L8.3 - Security events and auditing
- Audit events for:
  - 2FA enable started
  - 2FA confirmed enabled
  - 2FA disabled
  - 2FA login success/failure
  - lockout triggered

Deliverables for Phase L8:
- Middleware/login-pipeline integration
- Session and claims hardening
- Audit integration points

## Phase L9 - Testing Strategy and Quality Gates
Reason to do ninth:
- 2FA is security-sensitive and must be verified with deterministic tests.

### Stage L9.1 - Unit tests for secret generation
- Generates non-empty secret
- Meets minimum entropy/length expectations
- Encodes manual key correctly

### Stage L9.2 - Unit tests for TOTP validation
- Valid code accepted in current time step
- Valid code accepted with ±1 drift window
- Invalid/expired code rejected

### Stage L9.3 - Integration tests for login flow
- 2FA-disabled user logs in directly
- 2FA-enabled user requires second step
- Lockout threshold enforced after repeated failures

Deliverables for Phase L9:
- Unit test suites
- Integration test scenarios
- Security test checklist

## Phase L10 - Deployment, Rollout, and Operations
Reason to do last:
- Controlled rollout and recovery are required for security features.

### Stage L10.1 - Feature flags and rollout
- Add configurable feature toggle for 2FA module
- Optional phased rollout by role or tenant

### Stage L10.2 - Operational dashboards and alerts
- Track 2FA adoption rate
- Track failed challenge rate and lockouts

### Stage L10.3 - Support and recovery flows
- Administrative unlock path for locked users
- Recovery guidance for device loss (future recovery codes optional stage)

Deliverables for Phase L10:
- Rollout plan
- Operational monitoring notes
- Support runbook

## Required Code Deliverables (Implementation Blueprint)
Services:
- TwoFactorService (TOTP generation/validation)
- SecretProtectionService (encrypt/decrypt)
- QRCodeService (QR generation)
- LoginChallengeService (pending challenge state)

Web/API endpoints or Razor Pages:
- Enable 2FA
- Verify and confirm setup
- Disable 2FA
- Login verification step

UI examples:
- Account settings 2FA panel
- Setup page (QR + manual key)
- Login challenge page

Integration:
- Login flow modification for 2-step process
- Middleware/auth pipeline support for pending 2FA state

Testing:
- Secret generation unit tests
- TOTP validation unit tests

## Reference Sequence Flow
1. User logs in with username/password.
2. System validates credentials.
3. If 2FA not enabled, sign in complete.
4. If 2FA enabled, store pending challenge and show code page.
5. User submits TOTP.
6. System validates code with drift window ±1.
7. On success, full login completes and pending state clears.
8. On failure, attempt count increments and lockout policy may trigger.

## Best-Practice Checklist
- No plaintext secret storage
- No OTP/secret in logs
- Strong random secret generation
- Time drift tolerance configured and tested
- Brute-force controls and lockout in place
- Clean architecture boundaries maintained
- DI registration for all services
- Unit and integration tests included

## Definition of Done (Plan L)
- Users can enable and disable 2FA from account settings.
- System generates and securely stores unique per-user secret.
- QR + manual key setup works with Google Authenticator/Authy.
- Login is split into credential step and TOTP step for enabled users.
- TOTP validation follows RFC 6238 with ±1 step drift support.
- Failed-attempt lockout protection is implemented.
- Endpoints/pages and basic UI are available for full 2FA lifecycle.
- Unit tests for secret generation and TOTP validation pass.
- Security and audit expectations are documented and implemented.
