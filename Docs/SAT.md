<!-- markdownlint-disable MD022 MD058 MD060 -->

# Security Acceptance Testing — Tabsan EduSphere

## MFA (Two-Factor Authentication) — SAT Test Results (2026-06-18)

### Security Verification
| # | Test | Result |
|---|------|:---:|
| 1 | Otp.NET 1.4.1 package installed | ✅ |
| 2 | TOTP secrets generated via cryptographically secure RNG | ✅ |
| 3 | Wrong codes rejected (400 Bad Request) | ✅ |
| 4 | Empty codes rejected | ✅ |
| 5 | Codes with invalid length rejected | ✅ |
| 6 | Clock drift tolerance (±1 window) via VerificationWindow | ✅ |
| 7 | Secrets stored encrypted (Data Protection) | ✅ |
| 8 | AuthService.LoginAsync validates MFA before JWT issuance | ✅ |
| 9 | Recovery codes consumable once | ✅ |
| 10 | Build: 0 errors | ✅ |

### ISO 27001 Compliance
| Control | Status |
|---------|:---:|
| A.9.4.2 — Secure log-on procedures | ✅ MFA + lockout |
| A.9.4.3 — Password management system | ✅ Password history + ageing |
| A.12.4.1 — Event logging | ✅ Audit logs with CorrelationId, Severity, EventCategory |
| A.12.4.2 — Protection of log information | ✅ Append-only enforced |
