# User Acceptance Testing — Tabsan EduSphere

## MFA (Two-Factor Authentication) — UAT Test Cases (2026-06-18)

### Setup Flow
| # | Test | Expected Result |
|---|------|----------------|
| 1 | Navigate to TwoFactorSettings page | Page loads with current 2FA status |
| 2 | Click "Begin Setup" | QR code + manual key displayed |
| 3 | Scan QR code in Google Authenticator | Account added to authenticator app |
| 4 | Enter correct 6-digit TOTP code | 2FA enabled, success message |
| 5 | Enter wrong code (123456) | Error: invalid code rejected |
| 6 | Enter empty code | Validation error |

### Login Flow
| # | Test | Expected Result |
|---|------|----------------|
| 7 | Login with MFA-enabled user, no MFA code | Login fails with MFA required |
| 8 | Login with correct password + valid TOTP code | Login succeeds, JWT returned |
| 9 | Login with correct password + wrong TOTP code | Login fails with invalid MFA code |
| 10 | Login with correct password + recovery code | Login succeeds, recovery code consumed |

### Disable/Reset Flow
| # | Test | Expected Result |
|---|------|----------------|
| 11 | Disable 2FA with valid TOTP code | 2FA disabled, secret preserved |
| 12 | Re-enable 2FA with valid code | 2FA re-enabled |
| 13 | Reset setup | Pending setup cleared |
