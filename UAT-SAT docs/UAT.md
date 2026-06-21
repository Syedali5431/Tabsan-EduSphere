<!-- markdownlint-disable MD022 MD058 MD060 -->

# User Acceptance Testing — Tabsan EduSphere

## Profile Picture Upload — UAT Test Cases (2026-06-22)

### Upload Flow
| # | Test | Expected Result |
|---|------|----------------|
| 1 | Navigate to User Settings page | Page loads with profile picture section |
| 2 | No file selected + click Upload | Error: No file selected |
| 3 | Select a .jpg file (under 2 MB) and upload | Profile picture updated, preview shown |
| 4 | Select a .png file and upload | Profile picture updated |
| 5 | Select a .gif or .bmp file | Error: Only JPG, JPEG, PNG allowed |
| 6 | Select a file larger than 2 MB | Error: File size must be 2 MB or less |
| 7 | Check navbar avatar after upload | Circular profile picture displayed before username |
| 8 | Log out and log back in, check avatar | Profile picture persists (session-cached) |
| 9 | Upload for another user (Admin) | Admin can update other user's profile picture |

### Display Flow
| # | Test | Expected Result |
|---|------|----------------|
| 10 | User with no profile picture | Initial letter displayed in colored circle |
| 11 | User with profile picture | Circular image (30-40px) shown before username |
| 12 | Broken image path | Falls back to initial letter (onerror handler) |
| 13 | Click profile menu dropdown | User Settings link appears in menu |

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
