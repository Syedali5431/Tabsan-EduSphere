# MFA (Multi-Factor Authentication) — Implementation Details

> **Status**: Backend fully implemented. UI setup flow complete. MFA code verification at login is **functional** but the login hand-off flow (`login-verify` endpoint) is designed as a plug-in insertion point for future front-end work. MFA is enforced at login only when a user has individually enabled it.

---

## 1. Architecture Overview

```
┌──────────────┐     HTTP API     ┌────────────────┐     EF Core     ┌──────────┐
│  Portal Web  │ ──────────────── │  TwoFactor      │ ────────────── │  SQL     │
│  (Razor)     │                  │  Controller     │                │  Server  │
│              │                  │  + Services     │                │          │
└──────────────┘                  └────────────────┘                └──────────┘
       │                                  │
       │  IEduApiClient                   │  ITwoFactorStateStore
       │  (HTTP client)                   │  (Data Protection)
       ▼                                  ▼
  TwoFactorApiModels.cs           TwoFactorStateStore.cs
```

The MFA system is built as a **dedicated add-on boundary** (`api/v1/2fa`) separate from the legacy auth controller. The `AuthService.LoginAsync()` also has an inline MFA verification path (for password-first login flow).

---

## 2. Key Files & Their Roles

### 2.1 Domain Layer (`User` entity)

| File | Purpose |
|------|---------|
| `src/Tabsan.EduSphere.Domain/Identity/User.cs` | `MfaIsEnabled` (bool), `MfaTotpSecret` (Base32 TOTP key), `MfaRecoveryCodesHashJson` (SHA-256 hashed recovery codes), `SetMfaEnrollment()`, `TryConsumeRecoveryCodeHash()` |

**Key Properties on `User`:**
- `MfaIsEnabled` — When `true`, login requires a valid TOTP code or recovery code
- `MfaTotpSecret` — Base32-encoded TOTP secret (stored raw, not DP-encrypted, to survive key rotation)
- `MfaRecoveryCodesHashJson` — JSON array of SHA-256 hashes of one-time recovery codes

### 2.2 Application Layer (Business Logic)

| File | Purpose |
|------|---------|
| `src/Tabsan.EduSphere.Application/Auth/AuthService.cs` | `LoginAsync()` — password-first login with inline MFA TOTP validation + recovery code consumption. `RegenerateRecoveryCodesAsync()`. `BeginMfaSetupAsync()` (legacy path). |
| `src/Tabsan.EduSphere.Application/Auth/AuthSecurityOptions.cs` | `MfaSettings` class — `Enabled`, `RequireForPasswordLogin`, `RequireForPrivilegedRolesOnly`, `PrivilegedRoles[]`, `TotpIssuer`, `TotpDigits=6`, `TotpStepSeconds=30`, `TotpAllowedDriftWindows=1`, `RecoveryCodeCount=8` |
| `src/Tabsan.EduSphere.Application/Interfaces/ITwoFactorStateStore.cs` | `TwoFactorStateSnapshot` record + `ITwoFactorStateStore` interface — `GetAsync()`, `SaveSetupAsync()`, `EnableAsync()`, `DisableAsync()`, `HardDeleteAsync()` |
| `src/Tabsan.EduSphere.Application/Interfaces/ITotpService.cs` | `ITotpService` — `GenerateSecret()`, `BuildProvisioningUri()`, `ValidateCode()` |
| `src/Tabsan.EduSphere.Application/DTOs/TwoFactor/TwoFactorDtos.cs` | Request/Response DTOs: `TwoFactorSetupResponse`, `TwoFactorVerifyRequest`, `TwoFactorDisableRequest`, `TwoFactorLoginVerifyRequest`, `TwoFactorVerificationResponse` |
| `src/Tabsan.EduSphere.Application/DTOs/Auth/AuthDtos.cs` | `MfaRecoveryCodesResponse` record, `LoginRequest.MfaCode` field |

### 2.3 API Layer (Controllers + Services)

| File | Purpose |
|------|---------|
| `src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs` | **Dedicated 2FA API** — routes: `GET/POST /api/v1/2fa/setup`, `GET /api/v1/2fa/status`, `POST /api/v1/2fa/verify`, `POST /api/v1/2fa/disable`, `POST /api/v1/2fa/enable`, `POST /api/v1/2fa/reset-setup`, `POST /api/v1/2fa/login-verify` |
| `src/Tabsan.EduSphere.API/Controllers/AuthController.cs` | Legacy MFA endpoints: `POST /api/v1/auth/mfa/setup`, `POST /api/v1/auth/mfa/verify-mfa`, `POST /api/v1/auth/mfa/recovery-codes/regenerate` |
| `src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs` | Orchestrator — `BeginSetupAsync()`, `GetStatusAsync()`, `VerifySetupAsync()`, `DisableAsync()`, `EnableWithCodeAsync()`, `ResetSetupAsync()`, `VerifyLoginAsync()` |
| `src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs` | Wraps `ITotpService` with configured TOTP parameters (issuer, digits, step, drift) |
| `src/Tabsan.EduSphere.API/Services/TwoFactor/QRCodeService.cs` | Generates PNG/base64 QR codes for authenticator app enrollment using `QRCoder` |
| `src/Tabsan.EduSphere.API/Program.cs` | DI registration: `ITwoFactorStateStore → TwoFactorStateStore`, `TwoFactorService`, `TwoFactorSetupService` |

### 2.4 Infrastructure Layer (Persistence)

| File | Purpose |
|------|---------|
| `src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs` | EF-backed implementation of `ITwoFactorStateStore`. Uses `IDataProtector` to protect/unprotect secrets. Stores raw Base32 secret in `MfaTotpSecret` column. |

### 2.5 Web Layer (Portal UI)

| File | Purpose |
|------|---------|
| `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` | Actions: `TwoFactorSettings()` (GET), `BeginTwoFactorSetup()` (POST), `VerifyTwoFactorSetup()` (POST), `DisableTwoFactor()` (POST) |
| `src/Tabsan.EduSphere.Web/Views/Portal/TwoFactorSettings.cshtml` | UI page — QR code display, manual key input, verify/disable buttons |
| `src/Tabsan.EduSphere.Web/Models/Portal/ApiConnectionModel.cs` | `TwoFactorSettingsPageModel` — `TwoFactorEnabled`, `HasStoredSecret`, `Issuer`, `AccountName`, `ManualKey`, `ProvisioningUri`, `QrCodeDataUrl` |
| `src/Tabsan.EduSphere.Web/Services/TwoFactorApiModels.cs` | API model DTOs: `TwoFactorSetupApiModel`, `TwoFactorOperationResultApiModel`, `TwoFactorStatusApiModel` |
| `src/Tabsan.EduSphere.Web/Services/IEduApiClient.cs` | HTTP client — `BeginTwoFactorSetupAsync()`, `VerifyTwoFactorSetupAsync()`, `DisableTwoFactorAsync()`, `EnableTwoFactorAsync()`, `ResetTwoFactorSetupAsync()`, `GetTwoFactorStatusAsync()`, `VerifyTwoFactorLoginAsync()` |

### 2.6 Configuration

| File | Key Settings |
|------|-------------|
| `src/Tabsan.EduSphere.API/appsettings.json` | `AuthSecurity.Mfa.Enabled` = `false` (disabled by default). Full MFA settings block. |
| `src/Tabsan.EduSphere.API/appsettings.Development.json` | `TotpIssuer`: `"Tabsan EduSphere (Dev)"` |
| `src/Tabsan.EduSphere.API/appsettings.Production.json` | `TotpIssuer`: `"Tabsan EduSphere"` |

---

## 3. MFA Login Flow (Password-First)

The `AuthService.LoginAsync()` method in `AuthService.cs` implements the password-first MFA flow:

1. **Password verified** → `PasswordHasher.Verify(user.PasswordHash, request.Password)`
2. **MFA check**: `var userHasMfa = user.MfaIsEnabled && !string.IsNullOrWhiteSpace(user.MfaTotpSecret)`
3. If `userHasMfa == true`:
   - If `request.MfaCode` is blank → return `LoginFailureReason.MfaCodeRequired`
   - Get decrypted secret via `ITwoFactorStateStore.GetAsync()` (Data Protection unwrap)
   - Validate TOTP code via `ITotpService.ValidateCode()`
   - If TOTP invalid → try recovery code via `user.TryConsumeRecoveryCodeHash()`
   - If recovery code invalid → return `LoginFailureReason.InvalidMfaCode`
4. Continue with session creation, JWT generation

### Login Failure Reasons (MFA-specific)

| Enum Value | Meaning |
|-----------|---------|
| `MfaCodeRequired` | User has MFA enabled but didn't provide a code |
| `InvalidMfaCode` | Provided TOTP code is invalid and no valid recovery code was used |

---

## 4. Two-Factor Controller API Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| `GET/POST` | `/api/v1/2fa/setup` | `[Authorize]` | Start enrollment — returns QR code, manual key, provisioning URI |
| `GET` | `/api/v1/2fa/status` | `[Authorize]` | Get current 2FA status (enabled/disabled, has secret) |
| `POST` | `/api/v1/2fa/verify` | `[Authorize]` | Verify initial TOTP code to complete enrollment |
| `POST` | `/api/v1/2fa/disable` | `[Authorize]` | Soft-disable 2FA (keeps secret, can re-enable later) |
| `POST` | `/api/v1/2fa/enable` | `[Authorize]` | Re-enable 2FA with existing secret + valid TOTP code |
| `POST` | `/api/v1/2fa/reset-setup` | `[Authorize]` | Clear pending setup (allows fresh start) |
| `POST` | `/api/v1/2fa/login-verify` | `[AllowAnonymous]` | **Login hand-off** — verify TOTP challenge during login |

---

## 5. Setup Flow (User Enrollment)

1. **User visits** `TwoFactorSettings` page in portal
2. **Clicks "Begin Setup"** → `PortalController.BeginTwoFactorSetup()` → calls `IEduApiClient.BeginTwoFactorSetupAsync()` → `POST /api/v1/2fa/setup`
3. **API generates** a new Base32 TOTP secret via `ITotpService.GenerateSecret()`
4. **Secret stored** via `ITwoFactorStateStore.SaveSetupAsync()` → writes to `User.MfaTotpSecret` column, sets `MfaIsEnabled = false`
5. **QR code generated** via `QRCodeService.GenerateDataUrl()` from `otpauth://totp/...` URI
6. **User scans QR** in authenticator app (Google Authenticator, Authy, etc.)
7. **User enters verification code** → `PortalController.VerifyTwoFactorSetup()` → `POST /api/v1/2fa/verify`
8. **API validates** code via `ITotpService.ValidateCode()` → if valid, calls `ITwoFactorStateStore.EnableAsync()` → sets `MfaIsEnabled = true`
9. **MFA is now active** for the user

---

## 6. What Needs To Be Added To Make MFA Work Properly End-to-End

### 6.1 Front-End Login Hand-off (HIGH PRIORITY)

The backend `POST /api/v1/2fa/login-verify` endpoint and `IEduApiClient.VerifyTwoFactorLoginAsync()` exist but the **login page UI does not yet prompt for the MFA code after password entry**. When `AuthService.LoginAsync()` returns `LoginFailureReason.MfaCodeRequired`, the Web login page should:

1. **Intercept** the `MfaCodeRequired` failure
2. **Display a second form** asking for the 6-digit authenticator code
3. **Submit** the MFA code along with `userId` to the login
4. **Alternatively**: submit to `/api/v1/2fa/login-verify` as a separate hand-off step

**Files to modify:**
- `src/Tabsan.EduSphere.Web/Views/Login/Index.cshtml` (or equivalent login view)
- `src/Tabsan.EduSphere.Web/Controllers/LoginController.cs`

### 6.2 Recovery Code Display After Setup (MEDIUM PRIORITY)

After successful MFA setup verification, **recovery codes should be displayed to the user once** and prompted to save them. Currently the `TwoFactorSettings.cshtml` page shows "2FA setup verified and enabled" but does not display recovery codes.

**Files to modify:**
- `src/Tabsan.EduSphere.Web/Views/Portal/TwoFactorSettings.cshtml`
- `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` (update `VerifyTwoFactorSetup` to fetch recovery codes)
- `src/Tabsan.EduSphere.Web/Services/IEduApiClient.cs` (add recovery code fetch API)

### 6.3 Recovery Code Regeneration UI (LOW PRIORITY)

The backend `POST /api/v1/auth/mfa/recovery-codes/regenerate` endpoint exists in `AuthController.cs` but no UI button exists in `TwoFactorSettings.cshtml`.

**Files to modify:**
- `src/Tabsan.EduSphere.Web/Views/Portal/TwoFactorSettings.cshtml` — add "Regenerate Recovery Codes" button
- `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` — add `RegenerateRecoveryCodes()` action

### 6.4 Enable MFA by Default for Privileged Roles (LOW PRIORITY)

`MfaSettings.RequireForPrivilegedRolesOnly` is set to `true` with `PrivilegedRoles = ["SuperAdmin", "Admin"]` but this only **informs the UI** — it does not enforce MFA at login. Only individual MFA setup gates access. To enforce:

- Add a check in `AuthService.LoginAsync()` after password verification for privileged roles without MFA
- Force redirect to MFA setup page

### 6.5 Session-Level MFA Remember (OPTIONAL)

Add a "Remember this device for 30 days" cookie/claim to reduce MFA prompts on trusted devices.

---

## 7. Complete Function/Endpoint Reference

### TwoFactorController (`api/v1/2fa`)

```
POST/GET  /api/v1/2fa/setup           → TwoFactorSetupService.BeginSetupAsync()
GET       /api/v1/2fa/status           → TwoFactorSetupService.GetStatusAsync()
POST      /api/v1/2fa/verify           → TwoFactorSetupService.VerifySetupAsync()       (body: { code })
POST      /api/v1/2fa/disable          → TwoFactorSetupService.DisableAsync()           (body: { code })
POST      /api/v1/2fa/enable           → TwoFactorSetupService.EnableWithCodeAsync()    (body: { code })
POST      /api/v1/2fa/reset-setup      → TwoFactorSetupService.ResetSetupAsync()
POST      /api/v1/2fa/login-verify     → TwoFactorSetupService.VerifyLoginAsync()       (body: { userId, code })
```

### AuthController (legacy `/api/v1/auth/mfa`)

```
POST      /api/v1/auth/mfa/setup                    → AuthService.BeginMfaSetupAsync()
POST      /api/v1/auth/mfa/verify-mfa               → (verify MFA code inline)
POST      /api/v1/auth/mfa/recovery-codes/regenerate → AuthService.RegenerateRecoveryCodesAsync()
```

### ITwoFactorStateStore methods

```
GetAsync(userId)         → TwoFactorStateSnapshot?
SaveSetupAsync(userId, secretKey) → bool
EnableAsync(userId)      → bool
DisableAsync(userId)     → bool
HardDeleteAsync(userId)  → bool
```

### User entity MFA methods

```
SetMfaEnrollment(secret, recoveryCodesHashJson)
TryConsumeRecoveryCodeHash(hash) → bool
```

### ITotpService methods

```
GenerateSecret()                    → string (Base32)
BuildProvisioningUri(issuer, account, secret, digits, step) → string
ValidateCode(secret, code, utcNow, digits, step, drift) → bool
```

---

## 8. Database Columns (User table)

| Column | Type | Purpose |
|--------|------|---------|
| `MfaIsEnabled` | `bit` | Whether MFA is active for this user |
| `MfaTotpSecret` | `nvarchar(200)` | Base32-encoded TOTP secret (raw, not encrypted) |
| `MfaRecoveryCodesHashJson` | `nvarchar(max)` | JSON array of SHA-256 hashed recovery codes |

---

## 9. Configuration Reference (appsettings.json)

```json
{
  "AuthSecurity": {
    "Mfa": {
      "Enabled": false,
      "RequireForPasswordLogin": false,
      "RequireForPrivilegedRolesOnly": true,
      "PrivilegedRoles": ["SuperAdmin", "Admin"],
      "TotpIssuer": "Tabsan EduSphere",
      "TotpDigits": 6,
      "TotpStepSeconds": 30,
      "TotpAllowedDriftWindows": 1,
      "RecoveryCodeCount": 8
    }
  }
}
```

---

## 10. Current Limitations & Known Gaps

1. **Login hand-off UI missing** — The login page does not display an MFA code prompt when `MfaCodeRequired` is returned
2. **Recovery codes not shown after setup** — User must be shown recovery codes once after enabling MFA
3. **No "recovery code regenerate" button in UI** — Backend supports it but no front-end trigger
4. **MFA not globally enforced** — Individual opt-in only; even with `RequireForPrivilegedRolesOnly=true`
5. **No SMS/Email fallback** — Only TOTP authenticator app is supported (RFC 6238)
6. **No WebAuthn/FIDO2** — No hardware key or biometric MFA options
7. **MfaTotpSecret stored raw** — The Base32 secret is stored directly in the database (not Data Protection encrypted) to survive key rotation. The `TwoFactorStateStore` uses `IDataProtector` only for the in-memory snapshot.
