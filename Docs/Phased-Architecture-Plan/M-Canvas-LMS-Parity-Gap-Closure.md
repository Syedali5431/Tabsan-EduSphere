# Plan M - Canvas LMS Parity: Gap Closure Roadmap

## Objective
Close the teaching and learning gaps between Tabsan EduSphere and Canvas LMS (Instructure) while keeping EduSphere's existing advantages (SIS, results/GPA, timetable, attendance, fees, FYP, licensing, multi-tenancy, compliance) untouched.

Positioning target after Plan M:
> **EduSphere = SIS + LMS + Campus Administration in one product** — matching Canvas on day-to-day teaching tools and beating it on institution management.

## How To Use This Document
- Each **Phase** (M1, M2, ...) is one gap area. Each **Stage** (M1.1, M1.2, ...) is one deliverable-sized unit of work (normally 1–5 working days).
- Tick `[ ]` → `[x]` as tasks finish. Update the **Status** column in the tracker below when a stage starts / finishes.
- Status legend: ⬜ Not started · 🟨 In progress · ✅ Done · ⏸ On hold · ❌ Dropped
- When a phase completes, append `### ✅ Implementation Summary` and `### ✅ Validation Summary` at the end of that phase section and run `Docs/Phase-Completion-Checklist.md`.
- Effort is in **developer-weeks (dw)** for one experienced .NET developer. Estimates — refine at stage kick-off.

## Baseline (verified in code, 2026-10-18)
| Area | Current state | Relevant code |
|---|---|---|
| Auth | Own JWT bearer + Argon2 passwords + TOTP 2FA. No external IdP / SSO. | `API/Program.cs`, `API/Controllers/AuthController.cs`, `API/Services/TwoFactor/` |
| Quizzes | `Quiz → QuizQuestion → QuizOption`; `QuestionType` = MultipleChoice, TrueFalse, ShortAnswer. `QuizAnswer` stores a **single** `SelectedOptionId` (multi-select MC cannot be stored correctly). No banks, no randomisation, no accommodations. | `Domain/Quizzes/Quiz.cs`, `Domain/Quizzes/QuizAttempt.cs` |
| Assignments | `AssignmentSubmission` has one `FileUrl` / `TextContent`, single `MarksAwarded` + `Feedback`. No attempt history, no late policy, no groups, no peer review, no inline annotation. | `Domain/Assignments/Assignment.cs`, `AssignmentSubmission.cs` |
| Rubrics | Rubric per assignment (`Rubric → RubricCriterion → RubricLevel`, `RubricStudentGrade`). Not linked to outcomes. | `Domain/Assignments/Rubric.cs` |
| Course content | `CourseContentModule` (Title, WeekNumber, Body, Published) + `ContentVideo`, `CourseMaterial`. No completion tracking, no release conditions. | `Domain/Lms/` |
| Integrations | SMTP, Twilio SMS, in-app providers, `ResilientOutboundIntegrationGateway`. No LTI, no plagiarism, no conferencing. | `Infrastructure/Integrations/` |
| AI | OpenAI-compatible `ILlmClient` (OpenAI / Azure OpenAI / Ollama). | `Infrastructure/AiChat/` |
| Localisation | None (`IStringLocalizer` not used). `LabelService` handles institution vocabulary only. | `Application/Services/LabelService.cs` |
| Mobile | Responsive Razor web only. No PWA manifest / service worker, no native app. | `Web/` |
| Stack | .NET 8, EF Core 8 (SQL Server), Redis, RabbitMQ, QuestPDF, ClosedXML, Serilog, OpenTelemetry, Swagger. | `*.csproj` |
| Tests | Unit, Integration, Contract, WebIntegration, Playwright, load. | `tests/` |

## Global Rules (apply to every stage)
1. **Additive first.** New entities/tables/services; extend existing entities only with nullable columns or new child tables. Do not break existing endpoints — version new behaviour where contracts change.
2. **Every schema change ships with an EF migration** in `Infrastructure/Persistence/Migrations`, applied and verified against LocalDB before the stage is marked done (known recurring issue: DB missing tables/columns the model expects).
3. **Tenant-scoped.** All new tables carry `TenantId` / resolve through `ITenantScopeResolver`; all per-institution settings are per tenant.
4. **License-gated.** Each new capability registers in the Module Registry / Portal Capability Matrix so it can be licensed and toggled per institution (feature flag default = off until released).
5. **Audit + permissions.** New admin actions write audit logs; new endpoints use the existing permission/policy model.
6. **Tests per stage:** unit tests for domain/services, integration tests for endpoints, Playwright for new portal pages.
7. **Verify through real API flows** (not SQL) — lifecycle-style walkthroughs catch real bugs.
8. **Docs sync** per `Phase-Completion-Checklist.md` (Function-List, Functionality, Database Schema, etc.).

---

## Master Tracker

| Phase | Name | Priority | Effort | Depends on | Status |
|---|---|---|---|---|---|
| M1 | Single Sign-On (OIDC + SAML) | P1 | 3 dw | — | ✅ M1.1–M1.6 (live IdP sign-in pending) |
| M2 | Quiz Engine Upgrade | P1 | 5–6 dw | — | ✅ M2.1–M2.7 + quiz → gradebook sync |
| M3 | SpeedGrader-style Grading Workspace | P1 | 5 dw | — | ✅ M3.1–M3.6 (live Gotenberg run open) |
| M4 | Progressive Web App (installable + push) | P1 | 1–2 dw | — | ✅ M4.1–M4.4 (live push verified on Chrome/Edge; phone install open) |
| M5 | LTI 1.3 / LTI Advantage Platform | P1 | 6–8 dw | M1 (key mgmt pattern) | ✅ M5.1–M5.7 incl. Dynamic Registration and saLTIre checks (vendor-account tools and 1EdTech certification open) |
| M6 | Learning Outcomes & Mastery | P2 | 4 dw | M2, M3 | ✅ M6.1–M6.4 |
| M7 | Module Progress & Release Conditions | P2 | 2 dw | — | ✅ M7.1–M7.4 |
| M8 | Assignment Enhancements (late policy, overrides, groups, peer review) | P2 | 3 dw | M3 | ✅ M8.1–M8.4 |
| M9 | Plagiarism Detection (direct API) | P2 (optional if M5 done) | 2 dw | M3 | ✅ M9.1–M9.3 (live Copyleaks run open) |
| M10 | Live Classes / Web Conferencing | P3 | 1 dw | — (or via M5) | ✅ M10.1–M10.3 (live service accounts open) |
| M11 | Localisation (multi-language + RTL) | P3 | 3–4 dw | — | ✅ M11.1–M11.4 (native-speaker review open) |
| M12 | Native Mobile Apps | P3 | 10–12 dw | M4, M1 | ✅ M12.1–M12.5 (iOS build on a Mac, real-phone and live push checks open) |
| M13 | SCORM / xAPI Packages | P4 (on demand) | 4 dw | M7 | ✅ M13.1–M13.2 incl. packages in parts and SCORM 2004 sequencing (commercial authoring-tool exports untested) |
| M14 | Accessibility (WCAG 2.1 AA) & Scale Hardening | Ongoing | 2–3 dw | all UI phases | ✅ M14.1–M14.3 (5,000-user staging run, live cloud storage, AT user testing open) |

**Totals:** P1 ≈ 20–24 dw · P2 ≈ 11 dw · P3/P4 ≈ 18–21 dw · Overall ≈ 9–12 months for 1 dev, ~5–6 months for 2.

Suggested order: **M1 → M2 → M3 → M4 → M5 → M6 → M7 → M8 → (M9) → M10 → M11 → M14 → M12 → M13**

---

## Phase M1 - Single Sign-On (OIDC + SAML 2.0)
Reason to do first:
- First question in every university RFP; quick win; unblocks native app login (M12) and establishes key/secret management pattern reused by LTI (M5).

Design: external IdP authenticates → EduSphere maps external identity to an existing user → issues the **existing** EduSphere JWT. Existing password + 2FA login stays unchanged.

### Stage M1.1 - Data model & settings ✅ (2026-10-18)
- [x] Entity `ExternalLogin` (TenantId, UserId, Provider, ExternalSubjectId, Email, LinkedAt, LastLoginAt) — unique on (TenantId, Provider, ExternalSubjectId) **and** (UserId, Provider).
- [x] Entity `SsoProviderSetting` (renamed from `TenantSsoConfiguration` to avoid an awkward `...ConfigurationConfiguration` EF class) — TenantId, Provider [Microsoft, Google, GenericOidc, Saml], DisplayName, ClientId, ClientSecretEncrypted, Authority, MetadataUrl, AllowedEmailDomains, AutoLinkByEmail, EnforceSsoOnly, TrustIdpMfa, IsEnabled. Soft-deletable; unique live row per (TenantId, Provider).
- [x] Client secret encryption: use the existing AES `IEncryptionService` (stable master key `Security:EncryptionMasterKey`) instead of ASP.NET Data Protection — Data Protection keys are not persisted in this app and would orphan secrets after key rotation (same reason the 2FA add-on stores its secret raw).
- [x] EF configuration + migration `20261018064107_PlanMPhase1SsoFoundation`; applied to LocalDB and verified (tables, indexes, `has-pending-model-changes` = none).
- [x] Module Registry entry `sso` (license-gated), `KnownModuleKeys.Sso`, seeded module "Single Sign-On (SSO)" (inactive by default), license middleware route `/api/v1/sso` → `sso`, capability matrix row `single_sign_on`.

#### ✅ Implementation Summary (M1.1)
- New: `Domain/Enums/SsoProvider.cs`, `Domain/Identity/ExternalLogin.cs`, `Domain/Identity/SsoProviderSetting.cs`, `Infrastructure/Persistence/Configurations/SsoConfigurations.cs`, `Infrastructure/Migrations/20261018064107_PlanMPhase1SsoFoundation.cs`, `tests/Tabsan.EduSphere.UnitTests/SsoFoundationTests.cs`.
- Changed: `ApplicationDbContext` (2 DbSets), `ModuleRegistry`, `KnownModuleKeys`, `DatabaseSeeder` (module seed), `ModuleLicenseEnforcementMiddleware`, `PortalCapabilityMatrixService`.
- Domain rules: provider rows start disabled and cannot be enabled until complete; clearing the secret auto-disables; Google authority is pinned to `https://accounts.google.com`; authorities/metadata must be absolute https URLs; email-domain allow-list is normalised and matched exactly (no suffix tricks).

#### ✅ Validation Summary (M1.1)
- Unit tests: 20 new (SsoFoundationTests) — full suite 239/239 passing (Release).
- LocalDB: `external_logins` (11 cols) and `sso_provider_settings` (18 cols) created; unique indexes confirmed; latest migration recorded in `__EFMigrationsHistory`.
- Pending: `sso` module row is seeded on next API restart (running live API was not restarted).

### Stage M1.2 - OIDC (Microsoft Entra ID / Google / generic) ✅ (2026-10-18)
Design change from the original bullets: instead of the ASP.NET OpenIdConnect handler with dynamic named schemes, the code flow is implemented directly (authorization code + PKCE S256) with `Microsoft.IdentityModel.Protocols.OpenIdConnect` 7.1.2 for discovery/JWKS and `JsonWebTokenHandler` for id_token validation. Reasons: per-tenant settings live in the DB (no restart / scheme juggling), and the API is not browser-facing (behind the portal / tunnel), so the **browser leg lives in the Web portal** and the API is called server-to-server.

- [x] Flow: Web `GET /Login/Sso/{provider}` → API `POST /api/v1/sso/start` (validates redirect URI, stores state + nonce + PKCE verifier in the distributed cache, 10 min, single-use) → browser to IdP → IdP → Web `GET /Login/SsoCallback` (state must match a Data-Protection-encrypted HttpOnly SameSite=Lax cookie bound to the browser — login-CSRF protection) → API `POST /api/v1/sso/complete` → standard `LoginResponse` → Web stores session exactly like password login.
- [x] Provider buttons on the login page from `GET /api/v1/sso/providers?tenant=CODE` (multi-tenant portals pass `?tenant=CODE`; it is carried through the forms). A 403 (module off) just hides the buttons.
- [x] id_token validation: signature against discovery JWKS (one forced refresh on unknown `kid` for key rollover), issuer, audience = client id, lifetime (2 min skew), nonce. Subject = Entra `oid` when present, else `sub`.
- [x] Account linking: existing `ExternalLogin` → else, only if `AutoLinkByEmail`, an existing **active** user with the same email **in the same tenant**, where the email is verified (`email_verified`, or Microsoft whose authority is pinned to one directory) and the user has no other link for that provider (never silently re-link) → else refused (`AccountNotLinked`). No user auto-creation (whitelist provisioning deferred).
- [x] Microsoft authority must be the institution's own directory — `common` / `organizations` / `consumers` are rejected at configuration time (prevents foreign-tenant / unverified-email takeover).
- [x] Email domain allow-list enforced on every SSO sign-in.
- [x] `AuthService` refactor: password login and SSO share one tail (`CompleteLoginAsync`) — active/lockout, TOTP, session risk, license concurrency, session + JWT issuance, audit + login activity. New `IAuthService.ExternalLoginAsync`. Password ageing / forced password change are skipped for SSO sign-ins.
- [x] Users with EduSphere TOTP: API returns `MfaCodeRequired` + one-time ticket (5 min); portal shows a code-only step (`POST /Login/SsoMfa` → `POST /api/v1/sso/complete-mfa`). Fresh ticket after each wrong code, locked after 5 failures. *(Pulled forward from M1.4)*: `TrustIdpMfa` + IdP `amr` = `mfa` skips the TOTP step.
- [x] Audit: `SsoLoginFailed` (reason), `SsoAccountLinked`, and successful `Login` entries carry `"method":"sso:<Provider>"`.
- [x] Config: API `AuthSecurity:Sso:AllowedRedirectUris` (exact list; when empty any `https://…/Login/SsoCallback`, or `http://localhost…` for dev, is accepted). Web `Sso:PublicBaseUrl` overrides the callback origin behind a tunnel/proxy without forwarded headers.

#### ✅ Implementation Summary (M1.2)
- New: `Application/Auth/SsoSignInService.cs`, `Application/DTOs/Auth/SsoDtos.cs`, `Application/Interfaces/ISsoInterfaces.cs`, `Infrastructure/Auth/OidcClient.cs`, `Infrastructure/Repositories/SsoRepository.cs`, `API/Controllers/SsoController.cs`, `Web/Models/Portal/SsoProviderButton.cs`.
- Changed: `AuthService` (+`IAuthService`), `AuthSecurityOptions` (`AllowedRedirectUris`), `SsoProviderSetting` (Microsoft single-tenant rule), API `Program.cs` (DI), Infrastructure csproj (+`Microsoft.IdentityModel.Protocols.OpenIdConnect` 7.1.2), Web `LoginController` + `Views/Login/Index.cshtml`.
- No schema change in this stage.

#### ✅ Validation Summary (M1.2)
- Unit: `SsoSignInTests` (30) — OidcClient against an in-memory IdP with real RSA-signed tokens (PKCE params, code redemption, bad nonce / audience / signature / expiry, token-endpoint error) and service rules (redirect allow-list, state single-use, linking, cross-tenant, re-link refusal, domain allow-list, TrustIdpMfa, MFA ticket rotation + lockout, gate rejection). Suite 269/269.
- Integration (real API pipeline + LocalDB, stubbed IdP): `SsoIntegrationTests` (4) — module gate 403, redirect rejection, full round trip (auto-link, tokens usable on an authenticated endpoint, session row, link row, state replay refused), unlinked identity refused. Suite 310/310 (+1 pre-existing skip).
- Web integration (portal against fake API): `SsoLoginWebIntegrationTests` (7) — buttons, no buttons when module off, IdP redirect + HttpOnly state cookie, callback sign-in + returnUrl, missing cookie / state mismatch never relayed, friendly not-linked error, TOTP step. Suite 13/13.
- Not yet verified against a real Microsoft/Google tenant (needs app registrations — do during M1.6), and providers can only be configured in the DB until the admin page (M1.5).

### Stage M1.3 - SAML 2.0 ✅ (2026-10-18)
- [x] Library: **ITfoxtec.Identity.Saml2 4.21.0** (BSD-3, no auth middleware — fits the API-side design; Sustainsys is a handler-based alternative and was not used). It requires Microsoft.IdentityModel ≥ 8.22, so **Microsoft.IdentityModel.\* and System.IdentityModel.Tokens.Jwt were aligned from 7.1.2 to 8.22.0** across the solution (all JWT/auth suites re-run green; every IdentityModel assembly resolves to 8.22.0).
- [x] Flow mirrors OIDC: Web `GET /Login/Sso/saml` → API `POST /api/v1/sso/start` (HTTP-Redirect AuthnRequest; state = RelayState, stores AuthnRequest ID + ACS + SP entity ID) → IdP → **cross-site POST** to Web `POST /Login/SamlAcs` (RelayState must match the browser cookie, which for SAML is `SameSite=None; Secure` because Lax cookies are not sent on cross-site POSTs) → API `POST /api/v1/sso/saml/complete` → same linking / MFA / login gates as OIDC (shared `FinishSignInAsync`).
- [x] SP metadata per portal origin + tenant: Web `GET /Login/SamlMetadata[?tenant=CODE]` (proxies API `GET /api/v1/sso/saml/metadata`). **SP entity ID = that metadata URL**, always derived from the validated ACS URL (never caller-supplied) — prevents audience confusion. Metadata advertises HTTP-POST ACS and `WantAssertionsSigned="true"`.
- [x] IdP trust from the admin-configured metadata URL (https only): signing certificates + entity ID, cached 1 h, refreshed once on signature failure (certificate rollover).
- [x] Response validation: signature required (unsigned/tampered/foreign-cert rejected), issuer = metadata entity ID, audience = our entity ID, validity window, success status, Response `Destination` = ACS, and a signed **bearer SubjectConfirmation with Recipient = ACS and InResponseTo = our request ID** (so IdP-initiated / unsolicited responses are rejected). State is single-use and cannot cross protocols (OIDC state ≠ SAML RelayState).
- [x] Attribute mapping: subject = Entra `objectidentifier` → else NameID (non-transient) → else eduPersonTargetedID / eduPersonPrincipalName; email from standard email attributes (Entra/ADFS claim URI, `urn:oid:0.9.2342.19200300.100.1.3`, `mail`, `email`) or an emailAddress NameID; display name from standard attributes; MFA from AuthnContextClassRef (REFEDS MFA, `multipleauthn`, two/multi-factor classes) or Entra `authnmethodsreferences`. SAML emails are treated as verified (institution-owned IdP) — the domain allow-list still applies.
- [x] Role-hint and student/staff-ID attribute mapping; signed AuthnRequests and encrypted assertions (2026-10-24). Per provider (OIDC and SAML): an **institution-ID claim/attribute** that links a first sign-in to the one active account of the tenant with that registration number (students) or username, and a **role claim/attribute with a value → role mapping** (`student=Student; faculty=Faculty|Admin`, scoped values match before "@"; Student, Faculty, Admin, Finance, Parent) with an option to refuse sign-ins whose asserted role does not map to the account's role (checked for existing links and before any automatic link; new failure `RoleNotAllowed`, 403, audited with the asserted values). Accounts are still never created. SAML: an SP certificate (RSA-2048, created on first use, `AuthSecurity:Sso:SpCertificateFile`) published for signing and encryption in the SP metadata; per provider **sign AuthnRequests** (redirect-binding signature, `AuthnRequestsSigned` in metadata) and **require encrypted assertions**; encrypted assertions are always decrypted. Migration `PlanMSsoAttributeMapping`; tests: unit (mapping rules, linking by ID, ambiguous IDs, role refusal without linking, scoped affiliations, signed request verified with the SP key, encrypted assertion decrypted / required / wrong key, metadata), integration (real SamlClient in the API: signed request, encrypted assertion, link by employeeNumber, role refusal), web (settings relayed).

#### ✅ Implementation Summary (M1.3)
- New: `Infrastructure/Auth/SamlClient.cs`; tests `UnitTests/SsoSamlClientTests.cs`, `IntegrationTests/Infrastructure/SwitchableSamlClient.cs`.
- Changed: `ISsoInterfaces.cs` (`ISamlClient`, `SamlServiceProvider`, `SamlAuthnRequest`; `OidcIdentity` → `ExternalIdentity`, `OidcException` → `SsoProtocolException`), `SsoDtos.cs` (`SsoSamlCompleteRequest`), `SsoSignInService` (SAML start/complete/metadata, shared finish, ACS-vs-callback redirect rules), `SsoController` (`saml/complete`, `saml/metadata`), API `Program.cs` (DI), Infrastructure csproj (ITfoxtec 4.21.0; IdentityModel 8.22.0), Web `LoginController` (`SamlAcs`, `SamlMetadata`, SAML cookie policy).
- No schema change (`has-pending-model-changes` = none).

#### ✅ Validation Summary (M1.3)
- Unit: `SsoSamlClientTests` (14) — real SamlClient against an in-memory IdP serving metadata and minting genuinely signed responses: AuthnRequest contents, valid response → identity (+ MFA, Entra object id preferred), and rejection of wrong InResponseTo, foreign audience, wrong Recipient, wrong Destination, tampered, unsigned, foreign-certificate, expired, transient-NameID, IdP error status, non-https metadata (each rejection confirmed to fail for its intended reason); SP metadata contents. `SsoSignInTests` +7 SAML service cases. Suite 290/290.
- Integration (API pipeline, stubbed IdP): SP metadata for allowed ACS only; SAML round trip (provider listed, entity ID derived, request ID bound, link + tokens, RelayState replay refused). Suite 312/312 (+1 pre-existing skip).
- Web integration: SAML start uses ACS + `SameSite=None; Secure` cookie, ACS POST without antiforgery signs in, foreign RelayState rejected, metadata route. Suite 16/16.
- Not yet tried against a real IdP (Entra ID SAML / ADFS / Shibboleth) — do in M1.6 together with OIDC.

### Stage M1.4 - Policy & 2FA interplay ✅ (2026-10-18)
- [x] `EnforceSsoOnly` blocks password login for non-SuperAdmin users of that tenant (break-glass SuperAdmin keeps password + 2FA). Rules (`SsoLoginPolicy`): applies when an **enabled, complete** provider with the flag covers the user's tenant **or the whole installation** (TenantId null); checked **after** the password is verified (policy not revealed to guessers; no failed-attempt increment); **not applied while the `sso` module is inactive** (a lapsed SSO licence can never lock an institution out). API returns 403 `SSO_REQUIRED`; portal shows "Your institution requires single sign-on…" above the SSO buttons. Audit `LoginBlockedSsoOnly`.
- [x] SSO logins skip EduSphere TOTP when IdP asserts MFA (`amr` claim) — configurable via `TrustIdpMfa` *(done in M1.2)*.
- [x] Login activity + audit log entries for SSO logins / link / unlink:
  - `login_activity_logs.AuthMethod` (new column, `password` / `sso:<Provider>`) on every attempt, exposed in the login-activity API DTO. Migration `20261018080924_PlanMPhase1SsoLoginActivityMethod` (applied to LocalDB).
  - SSO failures (bad state, token/assertion validation, domain not allowed, not linked, provider unavailable) now also land in login activity (`FailureReason` = `Sso<Reason>`, username = asserted email when known) — previously audit only.
  - Linked accounts API (`SsoLinksController`): `GET /api/v1/sso/links` and `DELETE /api/v1/sso/links/{provider}` (self-service), `GET /api/v1/sso/links/users/{userId}` and `DELETE /api/v1/sso/links/users/{userId}/{provider}` (Admin/SuperAdmin, tenant-scoped user lookup). Every unlink audited as `SsoAccountUnlinked` (actor, provider, email, self-service flag). Self-service unlink of the **only** link is refused (409 `WOULD_LOCK_OUT`) when the user's password login is blocked by SSO-only; admins may still unlink. Link already audited as `SsoAccountLinked` (M1.2).
- Note: refresh tokens issued by password logins **before** SSO-only was switched on keep working until they expire (≤ refresh lifetime); revoke sessions from Account Security if an immediate cut-over is needed.

#### ✅ Implementation Summary (M1.4)
- New: `Application/Auth/SsoAccountServices.cs` (`SsoLoginPolicy`, `SsoAccountService`), `API/Controllers/SsoLinksController.cs`, migration `PlanMPhase1SsoLoginActivityMethod`; tests `UnitTests/SsoPolicyTests.cs`, `IntegrationTests/SsoPolicyIntegrationTests.cs`.
- Changed: `AuthService` (optional `ISsoLoginPolicy`, `SsoRequired` gate, auth method on every activity row), `LoginFailureReason.SsoRequired`, `AuthController` (403 `SSO_REQUIRED`), `LoginActivityLog` (+`AuthMethod`) + configuration + DTO/service mapping, `ISsoRepository` (+enforcing settings, links, remove), `SsoSignInService` (failures → login activity), API `Program.cs` (DI), Web `LoginController` (SSO-required message).

#### ✅ Validation Summary (M1.4)
- Unit: `SsoPolicyTests` (11) — tenant vs installation-wide enforcement, SuperAdmin exemption, module-inactive bypass, disabled/unflagged providers, unlink + audit, lock-out guard (self vs admin), not-found cases, auth method on activity. Suite 301/301.
- Integration (real API + LocalDB): password login → 403 `SSO_REQUIRED` with activity (`SsoRequired`, `password`) and audit; SuperAdmin still allowed; allowed again when the module is deactivated and after the provider is removed; password success records `password`; SSO success records `sso:Google` and a not-linked failure records `SsoAccountNotLinked` with the asserted email; link list/unlink with audit; Student blocked from admin routes; admin unlink. Suite 317/317 (+1 pre-existing skip).
- Web integration: SSO-required message shown with provider buttons. Suite 17/17.

### Stage M1.5 - UI ✅ (2026-10-18)
- [x] Login page: "Sign in with Microsoft / Google / Institution" buttons per tenant config *(done in M1.2; SSO-required message added in M1.4)*.
- [x] Admin page `Views/Portal/SsoSettings.cshtml` (System Settings → **Single Sign-On**, SuperAdmin only): scope switch (installation-wide or `?tenant=CODE`), the values to register at the IdP (OIDC redirect URI, SAML entity ID / ACS URL, **Download SP metadata**) with copy buttons, one card per provider (Microsoft Entra ID, Google Workspace, other OIDC, SAML 2.0) with status badge, write-only client secret ("Stored — leave blank to keep"), allowed domains, switches (show on login page, auto-link by verified email, trust IdP MFA, require SSO with confirmation prompt), **Test connection**, **Remove**.
- [x] Linked accounts: shown on the personal security page (**Two-Factor Authentication**, reachable by every role — the existing *User Settings* page is an admin user-management screen, so it was not used) as "Linked sign-in accounts" with Unlink (confirmation; API refuses with a clear message when it would lock the user out).
- [x] Admin API `SsoSettingsController` (`/api/v1/sso/settings`, SuperAdmin only): list, `PUT {provider}?tenant=` (create/update; secret encrypted, never returned), `DELETE` (soft delete; links kept), `POST {provider}/test` (OIDC discovery / SAML metadata reachability + secret decryptable). Audit `SsoSettingsCreated` / `SsoSettingsUpdated` (no secret values, `secretChanged` flag) / `SsoSettingsDeleted`. Domain validation errors are returned as readable messages ("Authority: Use your institution's own Entra ID directory…").
- [x] Sidebar: seeded menu `sso_settings` under System Settings (SuperAdmin only; denied for Admin/Faculty/Student/Finance), layout route map, SuperAdmin fallback list, and the static SuperAdmin settings block.
- [x] `Sso:PublicBaseUrl` helper shared by login and admin pages (`Web/Helpers/SsoPublicUrls.cs`).

#### ✅ Implementation Summary (M1.5)
- New: `Application/Auth/SsoSettingsService.cs`, `API/Controllers/SsoSettingsController.cs`, `Web/Views/Portal/SsoSettings.cshtml`, `Web/Models/Portal/SsoSettingsPageModels.cs`, `Web/Helpers/SsoPublicUrls.cs`; tests `IntegrationTests/SsoSettingsIntegrationTests.cs`, `WebIntegrationTests/SsoAdminWebIntegrationTests.cs`.
- Changed: `SsoDtos.cs` (settings DTOs), `ISsoInterfaces.cs` (`ISsoSettingsService`, repo `GetAllSettingsAsync` / `AddSettingAsync`), `SsoRepository`, API `Program.cs`, `DatabaseSeeder` (menu), Web `PortalController` (5 actions + menu-key map + linked accounts on the security page), `IEduApiClient` (6 calls + models), `TwoFactorSettings.cshtml`, `_Layout.cshtml`, `ApiConnectionModel.cs`, `LoginController` (shared URL helper).
- No schema change.

#### ✅ Validation Summary (M1.5)
- Integration (API + LocalDB): non-SuperAdmins get 403; create (secret write-only, encrypted at rest, not in responses or audit) → update without secret keeps it → enable → list → test (stubbed IdP failure surfaces the message) → delete / delete again 404; Microsoft `common` authority and non-https SAML metadata rejected with field-named messages; unknown tenant rejected. Suite 320/320 (+1 pre-existing skip).
- Web integration (real portal, fake API, signed in as SuperAdmin / Student): page renders all four provider cards with IdP values and never a secret; Save posts the right settings (switches, domain list, secret); Student is redirected away; security page lists linked accounts and Unlink calls the API. Suite 21/21. Unit 301/301.
- Visual check in the browser against a local fake API: login buttons, admin page (cards, badges, switches, test toast), sidebar entry, linked-accounts card. Two fixes came out of it: status badges used Bootstrap 5.2 classes (the portal ships 5.1 → invisible badge) and the linked-accounts table broke words at narrow widths (now a stacked list).

### Stage M1.6 - Tests & docs ✅ (2026-10-19)
- [x] Unit: linking rules, enforce-SSO policy *(SsoSignInTests, SsoPolicyTests, SsoSamlClientTests, SsoFoundationTests — M1.1–M1.4)*. Integration: mocked OIDC handler round-trip — **`SsoProtocolIntegrationTests`** runs the **real** `OidcClient` and `SamlClient` inside the real API pipeline against in-memory IdPs: discovery + PKCE (the fake IdP rejects the code unless the S256 verifier matches) + signed id_token; SAML AuthnRequest (request ID and tenant-specific entity ID read back from the deflated request) + genuinely signed assertion; an assertion addressed to the installation-wide entity ID is refused for a tenant sign-in. Playwright: **`tests/playwright/tests/auth/sso-login.spec.ts`** (`@auth @sso`) — buttons match the API's enabled providers per scope (installation-wide and `SSO_TENANT_CODE`), each button's 302 goes to an https IdP with `state` + S256 (OIDC) or `SAMLRequest` + `RelayState` (SAML) and sets the HttpOnly browser-binding cookie, a forged callback is rejected, SP metadata is published; expectations adapt to whatever the deployment has configured.
- [x] Admin guide: **`User Guide/SSO-Setup-Guide.md`** — prerequisites (licence/module, stable HTTPS address, outbound HTTPS, NTP), deployment settings (`Sso:PublicBaseUrl`, `AuthSecurity:Sso:AllowedRedirectUris` incl. both callback and ACS), Entra ID app registration, Google Workspace OAuth client, other OIDC, SAML (Entra ID, ADFS, Shibboleth) with the attribute mapping table, policy switches, linking/unlinking, roll-out checklist, troubleshooting table, security notes. SuperAdmin guide §9.10 links to it.

#### ✅ Implementation Summary (M1.6)
- New: `IntegrationTests/SsoProtocolIntegrationTests.cs`, `tests/playwright/tests/auth/sso-login.spec.ts`, `User Guide/SSO-Setup-Guide.md`.
- Changed: `User Guide/SuperAdmin-Guide.md` (§9.10), `tests/playwright/.env.example` (`SSO_TENANT_CODE`), `tests/playwright/README.md`.

#### ✅ Validation Summary (M1.6)
- Integration 322/322 (+1 pre-existing skip) incl. the 2 real-protocol round trips; unit 301/301; web 21/21.
- Playwright spec: 6/6 passed in Chromium against the Release portal + a local fake API, both installation-wide and with `SSO_TENANT_CODE=UNI` (the spec was first corrected to read the portal's 302 `Location` — Playwright does not route server redirects, and the fake IdP host does not resolve). It has not yet been run against a real deployment with SSO configured.
- Note: `npx tsc` reports `Cannot find name 'process'` across the whole Playwright project (40 occurrences, pre-existing — `@types/node` is not a devDependency); Playwright itself runs the specs fine.

Deliverables:
- SSO login via Microsoft / Google / SAML per tenant, enforce-SSO option, admin config page. ✅

Acceptance:
- A user of Tenant A can log in with Entra ID and lands on their correct role dashboard; Tenant B's config is not visible/usable from Tenant A. — **Verified in automation** (tenant-scoped settings/links, tenant-specific SAML audience, cross-tenant auto-link refused, providers listed per tenant). **Still open: one live sign-in per IdP type with a real Entra ID / Google Workspace / SAML tenant** (needs the institution's app registrations; follow `SSO-Setup-Guide.md` §9 roll-out checklist). The running live-test API/Web must also be redeployed from the current build, and it needs a stable HTTPS address (a quick-tunnel URL that changes on restart cannot be registered at an IdP).
- **Real identity-provider check (2026-10-25):** a local **Keycloak 26.8** (https on localhost with the .NET development certificate) configured as both an OpenID Connect and a SAML 2.0 IdP for tenant `TABSAN-UNI`; 8 real browser sign-ins (headless Chrome through the portal's own pages): staff and student linked by institution ID (`employeeNumber` / registration number), a user without an ID linked by verified email, and a role mismatch (Keycloak `student` vs EduSphere Finance) refused — identical for OIDC and SAML. SAML with signed AuthnRequests (verified by Keycloak) and required encrypted assertions signs in; an unencrypted assertion when encryption is required is refused (audit `SsoLoginFailed` with the reason) and an unsigned request is refused by Keycloak. Procedure and results: `SSO-Setup-Guide.md` §9.1. Still open: a sign-in against an institution's own Entra ID / Google Workspace tenant.

---

## Phase M2 - Quiz Engine Upgrade
Reason to do second:
- Highest-frequency teacher-facing gap; current answer model cannot store multi-select answers.

### Stage M2.1 - Answer model fix (prerequisite) ✅ (2026-10-19)
- [x] New child table `quiz_answer_selections` (QuizAnswerId, QuizOptionId) for multi-select; `SelectedOptionId` kept for backward compatibility (set when exactly one option is chosen) and existing answers backfilled into the new table by the migration.
- [x] `ResponseJson` (nullable, nvarchar(max)) on `QuizAnswer` for structured answers (matching, ordering, blanks, numeric/formula, file metadata); `TextResponse` widened to nvarchar(max) for essays.
- [x] Migration `20261018132603_PlanMPhase2QuizEngine` + backfill; regression tests on the existing quiz flows.

#### ✅ Implementation Summary (M2.1)
- Domain `QuizAnswer` owns its selections through a backing field (`_selections`, EF field access) with `SetResponse(selectedIds, text, json, singleSelection)` and `Grade(marks, feedback, grader)`; `QuizAnswerSelection` entity.
- Fixed while here: students received `IsCorrect` through `GET /quiz/{id}`; submit / attempt-detail had no ownership checks; question and attempt endpoints had no course scope checks. Students now only get quiz information from `GET /quiz/{id}` and questions come with an attempt.

#### ✅ Validation Summary (M2.1)
- Migration applied to LocalDB; backfill verified (133/133 answers with a selected option have a matching selection row); `dotnet ef migrations has-pending-model-changes` → none.

### Stage M2.2 - Question banks ✅ (2026-10-19)
- [x] `QuestionBank` (course offering / department / tenant owner, Title, Description, IsShared). Bank questions are `quiz_questions` rows with `QuestionBankId` (QuizId null, enforced by a check constraint) plus `Tags`, `Difficulty`, `IsArchived` — one question model, one grader path.
- [x] `QuizQuestionGroup` (QuizId, BankId, PickCount, MarksPerQuestion, optional TagFilter) — "pick N at random from bank X"; copy-from-bank also available.
- [x] Import/export banks as Excel (ClosedXML; the export includes a Help sheet describing every column and type). QTI 2.1 import — **deferred** (stretch goal).
- [x] UI: **Question banks** page (create/delete, edit questions, import/export) and bank tools in the quiz builder (draw-random groups, copy selected questions).

#### ✅ Implementation Summary (M2.2)
- `Application/Quizzes/QuestionBankService.cs`, `Infrastructure/Reporting/QuestionBankSpreadsheet.cs`, `API/Controllers/QuestionBankController.cs` (`api/v1/quiz/banks…`). Bank questions with answers are archived rather than deleted; a bank used by a quiz group cannot be deleted.

#### ✅ Validation Summary (M2.2)
- Integration test (`QuestionBank_DrawGroup_Copy_ExcelRoundTrip`): a pick-N group is drawn into the paper with the group's marks and graded; a bank used by a quiz cannot be deleted; copy-from-bank works; Excel export → import into a new bank reproduces every question with no errors.

### Stage M2.3 - New question types + grader strategy ✅ (2026-10-19)
- [x] `QuestionType` 1–11: MultipleChoice, TrueFalse, ShortAnswer, Essay, MultipleAnswers, Matching, Ordering, FillInBlank (several blanks, alternative answers, optional case-sensitivity), Numeric (exact ± tolerance or range), Formula (variables with ranges/decimals, values generated per attempt), FileUpload (allowed types, size limit). Essay is plain text with an optional word limit (rich text **deferred**).
- [x] `IQuestionGrader` strategy, one grader per type; partial credit for MultipleAnswers (right minus wrong), Matching, Ordering (items in place) and FillInBlank. Short answer / essay / file upload are queued for manual marking. Config validated and normalised server-side (`QuestionAuthoring`).
- [x] Builder UI per type (`_QuizQuestionEditor` + `quiz-editor.js`) and student renderers per type (`TakeQuizAttempt` + `take-quiz.js`).

#### ✅ Implementation Summary (M2.3)
- `Application/Quizzes/Grading/` — `QuestionConfigs.cs` (JSON schemas), `QuestionGraders.cs`, `QuestionAuthoring.cs`, `FormulaEvaluator.cs` (safe recursive-descent evaluator, no `eval`).
- Portal: `QuizBuilder` page — questions (edit in place / delete / regrade), add question, settings, bank tools, accommodations and overrides. The list page now shows a real question count.

#### ✅ Validation Summary (M2.3)
- Unit tests per grader and per config validator (including formula parsing and tolerance edges); web-integration test checks the editor JSON is turned into the API request (blank options dropped).

### Stage M2.4 - Randomisation & attempt snapshot ✅ (2026-10-19)
- [x] Per-quiz `ShuffleQuestions`, `ShuffleOptions`.
- [x] `quiz_attempt_items` (AttemptId, QuestionId, Order, Marks, PresentationJson = presented option/prompt order, GeneratedValuesJson = formula values). Drawn bank questions are stored too, so the paper, review and regrade use exactly what the student saw.

#### ✅ Validation Summary (M2.4)
- Unit tests: options shuffle (True/False keeps its order), matching choices include distractors, ordering is scrambled, formula values stay in range and are substituted, the student view never carries correctness. Integration: the paper and the review are built from the stored items (drawn bank questions included). Paper order across reloads is stable by construction (read from the snapshot) but has no dedicated test.

### Stage M2.5 - Accommodations & availability overrides ✅ (2026-10-19)
- [x] `QuizAccommodation` (QuizId null = every quiz in the offering, StudentProfileId, TimeMultiplier then ExtraTimeMinutes, ExtraAttempts, Note).
- [x] `QuizAvailabilityOverride` per student (AvailableFrom/Until). Section-level overrides — **deferred** (no section entity yet; apply per student).
- [x] Deadline enforced server-side: `DeadlineAt` stored on the attempt (time limit × accommodation, capped by the close date), 30-second grace for in-flight saves; expired attempts are finalised on the next request (lazy timeout). The page's countdown is anchored to the server clock and auto-submits, but the server is the authority.

#### ✅ Validation Summary (M2.5)
- Unit: multiplier-then-minutes, open-until-deadline-plus-grace. Integration (`Accommodations_Overrides_AndServerSideTimeLimit`): a closed quiz is refused, an override re-opens it for one student, 1.5× + 5 min gives the expected deadline, starting again resumes the same attempt, a save after the deadline is refused (`TIME_EXPIRED`) and the attempt is closed as TimedOut and graded from its saved answers, and the extra attempt is honoured before `NO_ATTEMPTS_LEFT`.

### Stage M2.6 - Quiz security & review options ✅ (2026-10-19)
- [x] Access code (fixed-time compare), IP allow-list (single addresses and CIDR via `System.Net.IPNetwork`, validated on save), one question at a time, lock answers after moving on (enforced by the API, not only the page).
- [x] Review visibility: None / ScoreOnly / ResponsesAndScore / CorrectAnswers, plus "only after the quiz closes".
- [x] Manual grading queue for short answer / essay / file upload (marks + feedback, file download); regrade one question across every finished attempt (Recalculate with the current key, or FullCredit).

#### ✅ Implementation Summary (M2.6)
- Portal: `TakeQuiz` (info, access code, start/resume, my attempts), `TakeQuizAttempt` (all renderers, autosave every change via JSON + antiforgery header, 30-second safety save, server-anchored timer, paging/locking, file upload), `QuizReview` (respects visibility; staff can mark manual answers in place), `QuizGrading` (queue + all attempts). `ViewQuizzes` sends quizzes with questions to Start / Resume / Review; the old file-only submit stays for quizzes without questions.
- Fixed during the browser check: a closed attempt (409 from the API) now goes to the review instead of back to the quiz list.

#### ✅ Validation Summary (M2.6)
- Integration: access code required/invalid, IP allow-list refusal and invalid-range validation, locked answers skipped on re-save, review "None" hides score and items while faculty still see them, default review shows the score, another student gets 404 for the paper/submit/detail, grading queue → over-max marks refused → essay graded with feedback, regrade Recalculate and FullCredit. Not separately tested: ResponsesAndScore and the after-close rule. Web-integration: paper renders every type with saved answers; autosave rejected without the antiforgery token; closed attempt → review; the builder is not shown to students.

### Stage M2.7 - Statistics, tests & docs ✅ (2026-10-19)
- [x] Item analysis per quiz: mean/median/high/low/standard deviation, difficulty index, discrimination (top vs bottom 27%), option distribution — on the quiz's **Statistics** page (with regrade buttons). Not added to Report Center (the per-quiz page is where teachers act on it).
- [x] Unit tests per grader (`QuizEngineTests`, 54), integration tests for the attempt lifecycle (`QuizEngineIntegrationTests`), web-integration tests for builder / paper / review (`QuizEngineWebIntegrationTests`, 7). Playwright specs for builder + take-quiz — **not written**; the pages were checked in the browser against a local fake API instead.

#### ✅ Validation Summary (Phase M2)
- Unit 355/355; integration 326/326 (+1 pre-existing skip); web 28/28; contract 1/1 (all Release).
- Browser check (Release portal + local fake API): builder shows every type and each editor reloads and re-serialises its config unchanged; banks, grading and statistics pages render; the paper restores saved answers for every type, ordering/word count/timer/paging work, and autosave reaches the API.

### Quiz → gradebook sync ✅ (2026-10-19)
- [x] Finished quiz scores feed one aggregated **Quiz** result component per student per offering (`QuizGradebookSync` + `QuizGradebookCalculator`, through `IResultService.SyncSystemComponentAsync`, which also recalculates the Total / GPA standing).
- [x] Which component: the active result component named `Quiz` (else `Quizzes`, any case) for the offering's institution type. If neither is configured, nothing is synced — set it up under Result Calculation.
- [x] Score: only finished attempts with a score count; **best attempt per quiz** (highest %); quizzes combined by points (sum of best scores ÷ sum of their max marks); quizzes the student has not finished, or that are worth 0 marks, are left out (not zero); scaled to the component's weightage (MaxMarks = weightage, 2 dp).
- [x] Recalculated after submit, server time-out (start / paper / save / review paths), manual grading of an answer, and regrade (every affected student). An attempt with essays / file uploads still waiting for marks has no score, so it never posts a partial result; an earlier graded attempt keeps counting meanwhile.
- [x] **Ownership rule** (new `results.Source` column, migration `PlanMPhase2QuizGradebookSync`; existing rows are `Manual`):
  - The sync only writes rows it created itself (`Source = QuizSync`) while they are **drafts**.
  - A cell a teacher entered by hand (result entry, bulk entry, gradebook cell, CSV import) is `Manual` and is **never overwritten**.
  - A teacher who edits a synced cell (gradebook cell edit, or admin correction after publication) takes it over: it becomes `Manual` and later quiz activity leaves it alone.
  - Once a synced row is **published** it is locked; later quiz changes are not applied (logged) and need an admin correction.
  - The gradebook grid shows each cell's `Source`.
- [x] Fixed on the way: editing an existing **draft** cell in the gradebook threw ("only published results can be corrected"); drafts are now revised in place.
- [x] Tests: `QuizGradebookSyncTests` (15 unit: calculator, ownership rule, ResultService outcomes, gradebook takeover, sync wiring/failure handling) and `QuizGradebookIntegrationTests` (real API + DB: pending essay posts nothing → grade → 10/20 → regrade → 16.67 → second attempt → 20/20, Total follows; hand-entered cell untouched; teacher edit takes a synced cell over).
- [x] Full suites after the sync (Release): unit 370/370; integration 327/327 (+1 pre-existing skip); web 28/28; contract 1/1.
- Not done: a synced row is not removed if every attempt later loses its score (not reachable with today's grading paths); a quiz-based column per quiz (Canvas style) is not offered — one aggregated component only.

Deliverables:
- Banks, 11 question types, randomisation, accommodations, security options, item analysis, quiz scores in the gradebook. ✅

Acceptance:
- A quiz drawing 10 random questions from a 50-question bank, with shuffled options and a student with 1.5× time, is taken, auto-graded, manually graded for essays and appears in gradebook. — **Verified in automation** (taken, auto-graded, manually graded in `QuizEngineIntegrationTests`; a submitted and graded quiz shows in the gradebook for that student in `QuizGradebookIntegrationTests`). Needs a "Quiz" result component configured for the institution type. Not yet run on the live test site with real users.

---

## Phase M3 - SpeedGrader-style Grading Workspace
Reason to do third:
- Teachers grade weekly; inline annotation is a visible Canvas differentiator.

### Stage M3.1 - Submission history & comments ✅ (2026-10-19)
- [x] `SubmissionAttempt` (SubmissionId, AttemptNumber, TextContent, SubmittedAt, IsLate) — every resubmission kept; `AssignmentSubmission` keeps mirroring the latest attempt (`CurrentAttemptNumber`, `FileUrl`, `TextContent`, `SubmittedAt`, `IsLate`).
- [x] `SubmissionFile` (up to 10 per attempt, 100 MB per attempt) stored by the API in media storage (`assignment-submissions`), with type, size, SHA-256 and conversion status. Pre-M3 URLs are kept as legacy files.
- [x] `SubmissionComment` (SubmissionId, AttemptNumber, AuthorUserId, Body, MediaStorageKey for audio/video, IsPrivateToGraders, soft delete). Students can comment on their own submission; only staff can make grader-only comments. In-browser voice recording (MediaRecorder) or an uploaded audio/video file.
- [x] Assignment settings: AllowedAttempts (0 = unlimited), AllowedFileTypes (narrowing a safe default list — HTML/SVG/scripts/executables can never be allowed), MaxFileSizeMb (≤ 50).
- [x] Migration `20261018180947_PlanMPhase3GradingWorkspace` + backfill: each existing submission becomes attempt #1, its `FileUrl` a legacy file; grades given before posting policies count as posted.

#### ✅ Implementation Summary (M3.1)
- Domain `Assignments/SubmissionHistory.cs`, new members on `Assignment` and `AssignmentSubmission`; `ISubmissionWorkspaceRepository` (kept apart from `IAssignmentRepository`); keys of the new tables are not store-generated so new children tracked through navigations are inserted.
- API `SubmissionWorkspaceController` under `/api/v1/assignment/…` (licensed with the *assignments* module). Multipart submit, file/rendition/annotated/comment-media downloads (always attachments with `nosniff` and a sandbox CSP).
- Fixed while here: student submissions were written to the portal's public `wwwroot/uploads` folder — both portal submit paths now forward files to the API; students could submit to courses they are not enrolled in; the staff submissions list, grade and reject endpoints had no tenant/campus check; the old portal submissions table posted an empty student id (it is replaced by the workspace).

#### ✅ Validation Summary (M3.1)
- Migration applied to LocalDB: 14,417 submissions → 14,417 attempt rows; 11,056 graded submissions marked posted; defaults correct; no pending model changes.
- Integration: not-enrolled student refused, wrong type refused, two attempts allowed / third refused with every attempt kept, students cannot reach other students' submissions or files, media comment upload/playback/author-only delete, the legacy JSON submit becomes attempt 1.

### Stage M3.2 - Document conversion pipeline ✅ (2026-10-19)
- [x] Background conversion of DOC/DOCX/PPT/PPTX/XLS/XLSX/ODT/ODP/ODS/RTF/TXT → PDF through Gotenberg 8 (`POST /forms/libreoffice/convert`). The worker (`SubmissionConversionWorker`) runs in the API; the database is the queue (claim = status Converting guarded by the row version, stale claims retried after 10 minutes, 3 attempts then Failed, staff can re-queue). RabbitMQ was not needed — documented deviation from the original plan.
- [x] PDF rendition stored alongside the original (`assignment-submissions-pdf`); status on `SubmissionFile` (NotRequired / Pending / Converting / Ready / Failed).
- [x] Gotenberg service added to `docker-compose.yml` (internal only); settings section `DocumentConversion`.

#### ✅ Validation Summary (M3.2)
- Unit: claim / stale-claim / retry / fail / re-queue state machine. Integration: real repository + storage with a fake converter — DOCX becomes Ready and its rendition downloads as PDF; XLSX failing three times is Failed with the error; re-queue resets it.
- **Not verified against a real Gotenberg container** (no Docker on the development machine). With `GotenbergUrl` empty (default) office files stay download-only.

### Stage M3.3 - Inline annotation ✅ (2026-10-19)
- [x] PDF.js 3.11.174 viewer (vendored in `wwwroot/lib/pdfjs`, `isEvalSupported:false` against CVE-2024-4367) with an SVG layer: highlight, strike-out, box, text, comment pin, freehand; colour picker; zoom; numbered markers and an annotation list with comments.
- [x] `SubmissionAnnotation` (SubmissionFileId, PageNumber, Type, GeometryJson in page-relative 0–1 coordinates, Comment, Color, AuthorUserId). Geometry is validated per type and re-serialised (never echoed).
- [x] Students see annotations only once the grade is posted; **Download annotated PDF** draws them onto the PDF with PDFsharp 6.1.1 and appends a numbered comments page.
- [x] Image submissions (PNG/JPEG/GIF/WebP) are annotated on an image canvas (the annotated PDF supports PNG/JPEG).

#### ✅ Validation Summary (M3.3)
- Unit: geometry rules per type, re-serialisation, colour/page/text rules. Integration: invalid rectangle refused, highlight + freehand saved, hidden from the student until posted, annotated PDF produced (starts `%PDF`, larger than the original).
- Playwright (Release portal + fake API): a highlight is drawn by mouse on page 1 and appears in the layer and the list; image file opens with the layer. Found and fixed: switching file/student mid-render left an unhandled PDF.js rejection.

### Stage M3.4 - Grading workspace UI ✅ (2026-10-19)
- [x] `GradingWorkspace` page: submission list (All / Needs grading / Late / Graded not posted) · viewer · right panel (attempt switcher, files, text, marks + feedback, rubric, provisional grades, comments).
- [x] Next/previous student without a page load (buttons, Alt+→/Alt+←, J/K); marks and feedback autosave (Ctrl+Enter saves now).
- [x] Rubric scoring inline, reusing `RubricStudentGrade`; the total can set the marks, scaled to the assignment's marks.
- [x] Student `AssignmentSubmission` page: attempts, previews, posted grade with rubric breakdown, comments with recording, resubmission.

#### ✅ Implementation Summary (M3.4)
- `Views/Portal/GradingWorkspace.cshtml`, `AssignmentSubmission.cshtml`, `_AssignmentSettingsForm.cshtml`; `wwwroot/js/submission-viewer.js`, `grading-workspace.js`, `submission-page.js`, `comment-recorder.js`; `PortalController.Grading.cs` (pages, JSON relays with antiforgery header, file relays); `EduApiClient.Workspace.cs`.
- Fixed pre-existing rubric bugs: rubric creation never attached its criteria/levels (every rubric was saved empty), and rubric grading always stored 0 points.

#### ✅ Validation Summary (M3.4)
- Playwright `tests/playwright/tests/grading/grading-workspace.spec.ts` (`@grading`): 5 consecutive green runs against the fake API — render, annotate, autosave, keyboard navigation (URL changes, no reload), image view, posting, student preview without tools, no page errors. Not yet run against a real deployment (needs `GRADING_ASSIGNMENT_ID` and test accounts).
- Web-integration (5): workspace config and scripts, students refused, grade relay needs the antiforgery header and forwards JSON (bad JSON → 400), student page renders and submit forwards the files to the API, file relay is an attachment with `nosniff`.

### Stage M3.5 - Grading policies ✅ (2026-10-19)
- [x] Anonymous grading: graders get "Student N" (stable per assignment) and no student id; names reappear when every submission's grade is posted, or with *Reveal names*.
- [x] Posting policy Automatic (default, the old behaviour) / Manual: marks, feedback, rubric, annotations and (under manual posting) teacher comments reach the student only after **Post grades**; *Hide grades* reverses it. The old grade endpoint and "my submissions" follow the same rule.
- [x] Moderated grading: other graders' marks become provisional (one per grader, updatable); the moderator (who switched moderation on, or an admin) sees all of them, picks one or enters the final grade; only the moderator posts/reveals.
- [x] Posted grades feed the gradebook's "Assignment"/"Assignments" component (points-based, scaled to the weightage, `ResultSource.AssignmentSync`; hand-entered and published cells are never overwritten).

#### ✅ Validation Summary (M3.5)
- Integration: manual posting hides marks/feedback/teacher comments/annotations until posted; posting while one submission is ungraded keeps names hidden, posting all reveals them; hide works; moderated flow (provisional per grader, grader sees only their own, graders cannot post or moderate, moderator picks 9/10 → student sees it); gradebook row 24/30 after posting 16/20 with `AssignmentSync`.
- Unit: posting/hiding, resubmission after grading, anonymity reveal, moderation needs a moderator, gradebook calculation.

### Stage M3.6 - Tests & docs ✅ (2026-10-19)
- [x] Integration `GradingWorkspaceIntegrationTests` (4), unit `GradingWorkspaceTests` (25), web-integration `GradingWorkspaceWebIntegrationTests` (5), Playwright `grading-workspace.spec.ts` (2).
- [x] Faculty, Student and SuperAdmin guides updated; project documentation suite synced (Functionality, Function List, PRD, Modules, Development Plan, Database Schema) for M1–M3.

#### ✅ Validation Summary (Phase M3)
- Unit 395/395; integration 331/331 (+1 pre-existing skip); web 33/33; contract 1/1 (Release).

Deliverables:
- Full grading workspace with attempt history, annotations, media comments, anonymous grading and posting policy. ✅

Acceptance:
- Teacher grades 30 DOCX submissions in one screen without leaving the page; student sees annotated PDF only after grades are posted. — **Verified in automation**: next/previous without reload, annotations hidden until posting, annotated PDF after posting. **Still open**: DOCX preview depends on a running Gotenberg service, which has not been tested here; a 30-submission run against a real deployment has not been done.

---

## Phase M4 - Progressive Web App (installable + push)
Reason to do fourth:
- One-to-two-week effort that lets sales say "mobile app"; foundation for M12.

### Stage M4.1 - Installability ✅ (2026-10-19)
- [x] `/manifest.webmanifest` (name/short name/description from portal branding, institution logo first plus 192/512/maskable PNG icons, theme colour, `start_url`, `display: standalone`, shortcuts to Timetable / Assignments / Notifications). Linked from the portal layout and the sign-in page (`crossorigin="use-credentials"`), with Apple/Android meta tags; *Install the EduSphere app* appears in the profile menu when the browser offers installation.
- [x] Per-institution manifest: branding is per deployment (each customer has its own), so each install shows that institution's name and logo; without a session the session's cached branding or the defaults are used.

### Stage M4.2 - Service worker & offline ✅ (2026-10-19)
- [x] Hand-written service worker (`wwwroot/sw.js`, no Workbox/CDN): app shell (CSS/JS/icons/fonts) stale-while-revalidate; served with `Cache-Control: no-cache` so updates arrive.
- [x] Offline read-only copies of Dashboard, Timetable, Announcements, Course Material and Notifications pages (network-first, refreshed on every online visit, max 40) and downloaded course-material files (max 30, ≤ 25 MB each). Sign-in redirects are never stored. Copies are wiped on sign-out and on the sign-in page (shared devices). Forms, grades and every non-GET request always go to the network.
- [x] Offline fallback page (`/offline.html`) and an "you are offline" banner.

### Stage M4.3 - Web Push notifications ✅ (2026-10-19)
- [x] VAPID key pair generated on first use and kept outside the repository (`WebPush:KeyFile`, default `<app-data>/Tabsan/vapid-keys.json`, or `WebPush:PublicKey/PrivateKey`); `push_subscriptions` (UserId, Endpoint unique, P256dh, Auth, DeviceLabel, failure counters) and `notification_preferences` (UserId, Type, PushEnabled).
- [x] Delivery implemented in-house on .NET crypto: RFC 8291 `aes128gcm` encryption (ECDH P-256, HKDF, AES-GCM) and RFC 8292 VAPID (ES256 JWT) — `Infrastructure/Notifications/WebPush.cs`; every notification sent through `NotificationService` (API and BackgroundJobs) also goes to subscribed devices — queued in the API (bounded channel + worker), direct in the BackgroundJobs host. Per-type preferences, 404/410 removes the device, 5 consecutive failures switch it off.
- [x] Security: subscription endpoints must be https on an allowed push-service host (FCM, Mozilla, Windows, Apple; configurable) — no server-side request forgery; browser keys are validated; payloads carry only title, a short body and a portal-relative link; sign-out unsubscribes the device.
- [x] "Notifications on this device" card on the Notifications page: turn on/off, test, per-type switches, list of devices (iPhone/iPad: hint to add to Home Screen first).

#### ✅ Implementation Summary (M4)
- Web: `PwaController` (manifest), `PortalController.Push.cs` (relays), `wwwroot/sw.js`, `offline.html`, `js/pwa.js`, `images/pwa/*`, layout/login/notifications view changes, `Program.cs` (`sw.js` headers).
- API/Jobs: `PushController` (`/api/v1/push`), `PushDispatchQueue` + `PushDispatchWorker`, `PushNotificationService`, `DirectPushNotificationDispatcher`, `WebPushSender`/`VapidKeyStore`/`WebPushEncryption`, `PushSubscriptionRepository`; migration `PlanMPhase4WebPush`; `WebPush` settings in both hosts; Docker volume for the key file.

### Stage M4.4 - Tests ✅ (2026-10-19)
- [x] Lighthouse 12 removed its PWA category, so installability and offline behaviour are covered by Playwright (`tests/playwright/tests/pwa/pwa.spec.ts`, `@pwa`): manifest fields and icons, `sw.js` headers, service worker active and controlling, page saved and served offline with the banner, other pages fall back to the offline page, sign-out wipes saved pages.
- [x] Real push round trip (2026-10-24): desktop Chrome (Google FCM, `fcm.googleapis.com`) and Edge (Windows push, `*.notify.windows.com`), fresh profiles, against the live portal — subscription stored, EduSphere's test push (RFC 8291 aes128gcm + VAPID) accepted by the push service and shown by the service worker within ~2 s in both. Android Chrome uses the same FCM path.
- [x] Android phone check without a device (2026-10-24): real Google Chrome 155 emulating a Pixel 7 (Android 14 user agent, screen, touch) on the live portal — Chrome's own installability check reports **no errors**, the manifest parses without errors (standalone, 192/512 + maskable icons, 3 shortcuts), the service worker controls the page, push subscribes through `fcm.googleapis.com` (the service Android Chrome uses) and EduSphere's test push is shown in ~2 s, a saved page reloads offline with the offline notice, the device is listed as "Chrome on Android", and turning it off removes the subscription. The sign-in page now has the iPhone Home Screen tags too (app title "EduSphere", status bar, touch icon 180×180), so adding to the Home Screen from the sign-in page names the app correctly.
- [ ] Final check on physical phones (Home Screen icon, notification in the phone's tray, iPhone via Apple push) — the 2-minute procedure is in the SuperAdmin guide §9.12. No phone was available, and the Android emulator cannot run on this PC (no hardware acceleration: Windows Hypervisor Platform is off; the Android 14 image needs CPU features software emulation lacks).

#### ✅ Validation Summary (Phase M4)
- Unit (18 new): byte-exact RFC 8291 Appendix A test vector, encrypt/decrypt round trip, VAPID JWT claims and signature, endpoint allow-list (incl. suffix tricks, IP literals, credentials, ports), key validation, payload limits, failure counting.
- Integration (2): real VAPID key store; endpoint/key validation; endpoints never echoed; only the owner can unsubscribe; a browser moving between users; preferences; notification → queue → worker → device, respecting an opted-out type; a gone device is removed.
- Web-integration (8): branded manifest + icons, defaults without a session, short names, `sw.js` no-cache, offline page, manifest link and push card in the layout, push relays need the antiforgery token.
- Playwright: 3 consecutive green runs against the Release portal + local fake API. Totals: unit 413, integration 333 (+1 skip), web 41, contract 1.

Deliverables:
- Installable branded app with offline timetable/materials and push notifications. ✅ (device install and live push delivery still to be checked on real phones)

---

## Phase M5 - LTI 1.3 / LTI Advantage Platform
Reason to do fifth:
- Single integration that unlocks Turnitin, Zoom, H5P, publisher content and hundreds of tools; required by most universities.

Role: **EduSphere = LTI Platform**, external products = LTI Tools. Module key `lti` ("External Tools (LTI 1.3)"); the platform issuer is the portal address (`Lti:PlatformUrl`), so tools only ever talk to the portal, which relays service calls to the API.

### Stage M5.1 - Platform keys & registration ✅ (2026-10-19)
- [x] Platform RSA-2048 signing keys (RS256, `kid`) generated on first use and kept outside the repository (`Lti:KeyFile`, default `<app-data>/Tabsan/lti-platform-keys.json`; Docker: `/app/keys` volume). Rotation from the admin page: the new key signs at once, the old one stays in the JWKS for `Lti:RetiredKeyDays` (7). Public keyset at `/lti/jwks` (one platform for the installation).
- [x] Entities `LtiTool` (Name, generated ClientId, OIDC login URL, target link URI, deep-linking URL, redirect URIs, JWKS URL or PEM key ≥ 2048 bits, custom parameters, privacy level, grade/roster permissions, on/off) and `LtiToolDeployment` (own deployment ID; whole installation, one department or one course offering). Every new tool gets an installation-wide deployment.
- [x] Admin page `/Portal/LtiTools` (System Settings › External Tools; SuperAdmin by default, Admins once the menu is granted in Sidebar Settings; key rotation SuperAdmin only): the values to give the vendor, the tool list, register/edit, turn tools and deployments on/off, add department/course deployments. Editing keeps a stored key when the key box is left empty; a JWKS URL replaces it. All changes are audited.
- [x] Dynamic Registration (1EdTech LTI Dynamic Registration 1.0, 2026-10-24): administrators paste a tool's registration URL on External Tools; the portal opens it in a frame with `openid_configuration` (`/lti/openid-configuration`: issuer, endpoints, scopes, LTI platform configuration) and a single-use one-hour `registration_token`; the tool posts its client registration to `/lti/register`, validated (web, id_token, implicit + client_credentials, private_key_jwt, https redirect/login/JWKS URLs, domain, name) and mapped (claims → privacy level, scopes → grade/roster services, deep-linking message → URL, custom parameters); the tool is created **switched off** with an installation deployment and `RegisteredDynamicallyAt`, the answer carries `client_id` and `deployment_id`; `org.imsglobal.lti.close` returns to the list. Migration `PlanMLtiDynamicRegistration`. Verified with saLTIre (below).

### Stage M5.2 - Core launch (Resource Link) ✅ (2026-10-19)
- [x] Third-party initiated login: `/lti/launch/{item}` posts `iss`, `login_hint`, `lti_message_hint`, `target_link_uri`, `client_id`, `lti_deployment_id` to the tool; the tool's OIDC request to `/lti/authorize` (GET or POST) is answered with an auto-posted RS256 `id_token` (5-minute lifetime) and its `state`.
- [x] The `login_hint` is a single-use launch record bound to the user, tool and deployment (valid `Lti:LaunchMinutes`, 5). The portal session cookie is SameSite=Lax and is not sent on a tool's cross-site POST, so the launch record is the proof; when the cookie is present the API also checks it is the same user. `redirect_uri` must exactly match a registered one; the tool's nonce is recorded once (replay protection in table `lti_used_nonces`, not Redis — every node shares the database).
- [x] Claims: message_type, version 1.3.0, deployment_id, target_link_uri, roles (Student → Learner, Faculty → Instructor, Admin → Administrator, SuperAdmin → Administrator + system administrator, other staff → Instructor only when teaching the course), context (course offering), resource_link, launch_presentation (document target, locale, return URL), tool_platform, custom (tool + item parameters), AGS and NRPS endpoint claims when allowed. Name/email only as the privacy level allows.
- [x] Placement: "External tools" card on the course page (students) and the course content manager (staff): add an item (new tab or inside EduSphere, optional points), hide/show, grades link; items inside EduSphere open in a portal frame page.

### Stage M5.3 - Deep Linking 2.0 ✅ (2026-10-19)
- [x] "Choose content from <tool>" on the course content manager sends an `LtiDeepLinkingRequest` (accepts `ltiResourceLink`, multiple, line items when the tool may grade).
- [x] `/lti/deep-link-return` validates the tool's signed response (JWKS URL, cached 10 minutes, https only, or the registered PEM; RS256 only; issuer = client ID, audience = platform; lifetime), checks the message type, version, nonce (once), deployment and the `data` round-trip (the selection is accepted once, within an hour), then creates course items — and a graded assignment for each item with a line item. Unsupported item types are skipped and reported.

### Stage M5.4 - Assignment & Grade Services (AGS) ✅ (2026-10-19)
- [x] OAuth 2.0 client-credentials token endpoint `/lti/token` with a JWT client assertion (signature, `sub` = client ID, audience = token URL, single-use `jti`); scopes limited to what the tool is allowed; tokens are random, stored only as SHA-256 hashes, `Lti:AccessTokenMinutes` (60).
- [x] Line items API (list with resource_link_id/resource_id/tag filters, create, read, update, delete) backed by EduSphere assignments (`assignments.ExternalToolId`); deleting keeps the assignment if students already have grades.
- [x] Scores API: the newest score per student wins; "FullyGraded" scores become graded submissions scaled to the line item's points and follow the assignment's posting policy (and the gradebook sync from M3); Results API (read-only).

### Stage M5.5 - Names & Role Provisioning Services (NRPS) ✅ (2026-10-19)
- [x] `/lti/nrps/{course}/memberships` (NRPS 2.0): teachers and enrolled students with LIS roles and status; name/email per the tool's privacy level; `role` and `limit` filters.

### Stage M5.6 - Tool integrations verification
- [x] Verified with **saLTIre** (2026-10-24, saltire.lti.app, LTI-PHP 3.3.2) against the public test deployment: Dynamic Registration; resource-link launch and deep-linking request — "Verification (strict mode): Passed"; signed deep-linking response accepted; client-credentials token; NRPS memberships and AGS line items (list/create/update/delete), scores and results — no validation errors. **Found and fixed:** NRPS and AGS containers honoured `limit` without a `Link: rel="next"` header and applied the role / user_id filters after the limit — now filtered first, paged with `page` and a next link on the platform address (relayed by the portal). Details: `Docs/LTI-Certification-Readiness.md`.
- [ ] H5P, Zoom LTI and Turnitin/Copyleaks LTI with the institution's vendor accounts — checklist in `Docs/LTI-Certification-Readiness.md` §5.
- [x] Certification readiness (2026-10-24): `Docs/LTI-Certification-Readiness.md` — 1EdTech process (membership, certification suite, submission, annual recertification), capabilities, evidence, items to check before submitting (stable issuer, optional features not implemented).
- [ ] **1EdTech LTI Advantage certification** (Platform) — needs Tabsan's 1EdTech membership to run the certification suite and submit the results.

### Stage M5.7 - Tests & docs ✅ (2026-10-19)
- [x] Unit, integration, web-integration and Playwright tests (below). Security review: signatures (platform RS256 with `kid`; tool tokens through `JsonWebTokenHandler` with RS256 only), replay (single-use launches, nonces, client-assertion `jti`), exact redirect URIs, https-only tool URLs (loopback http for local testing), SSRF-safe JWKS fetching (https, no redirects, 10 s timeout), access tokens hashed, module gate on every `/api/v1/lti` route. The portal sends no CSP, so tools load in the frame page; the API's restrictive headers are not relayed.
- [x] Admin guide: "How to add an LTI tool" (`User Guide/LTI-Tool-Setup-Guide.md`), plus the SuperAdmin, Admin and Faculty guides.

#### ✅ Implementation Summary (M5)
- Domain: `Lti/LtiEntities.cs` (tools, deployments, resource links, line items, scores, launches, used nonces, access tokens), `Assignment.ExternalToolId` / `ForExternalTool` / `SyncExternal`, `ILtiRepository`.
- Application: `Lti/LtiContracts.cs` (options, constants, role mapping, DTOs), `LtiAdminService`, `LtiLaunchService` (launch, authorize, deep linking), `LtiServiceEndpoints.cs` (`LtiGradeBridge`, `LtiTokenService`, `LtiAgsNrpsService`).
- Infrastructure: `Lti/LtiCrypto.cs` (`LtiPlatformKeyStore`, `LtiToolJwtValidator`), `LtiRepository`, `LtiConfigurations`; migration `PlanMPhase5Lti` (8 tables + `assignments.ExternalToolId`).
- API: `LtiControllers.cs` (`LtiAdminController`, `LtiCourseController`, `LtiProtocolController`), `LtiMaintenanceWorker` (hourly purge of expired launches, nonces and tokens), `Lti` settings, module `lti` (registry, seeder, licence middleware, capability matrix), Docker key volume.
- Web: `LtiController` (`/lti/launch`, `/lti/deep-link`, `/lti/authorize`, `/lti/deep-link-return`, relays for `/lti/jwks`, `/lti/token`, `/lti/ags/…`, `/lti/nrps/…`), `ExternalToolsViewComponent` (course card), `PortalController.Lti.cs` + `Views/Portal/LtiTools.cshtml`, `Views/Lti/*` (auto-post, frame, done, error), sidebar entry `external_tools`.

#### ✅ Validation Summary (Phase M5)
- Unit (22 new, `LtiTests`): role mapping, custom parameters, https/loopback URL rules, tool validation (keys, redirect URIs, privacy), platform key signing verified against the JWKS, rotation grace period and expiry, key file shared by nodes, single-use launches, deployments, token hashing and scopes, unverified issuer parsing.
- Integration (3, `LtiIntegrationTests`, real API + LocalDB with a fake tool holding its own RSA key): full launch → id_token claims → client-credentials token → line items → scores into graded submissions → results → NRPS with privacy; Deep Linking creating items once; registry validation, key kept/replaced on edit, roles, key rotation, module gate.
- Web-integration (11, `LtiWebIntegrationTests`): new-tab and iframe launches, local-only return URLs, `/lti/authorize` without a session (GET and cross-site POST) and refusals, deep-link return, relays (token form body, bearer token, query, vendor media types, 401 + `WWW-Authenticate`, no-store), course card for students and staff, admin page, saving tools, students kept out, inactive module.
- Playwright (`tests/playwright/tests/lti/lti.spec.ts`, `@lti`): Playwright plays the tool — registers it with a generated key, adds course items, launches in a new tab and in the frame (decoding the id_token claims), signs a Deep Linking response that lands two items, and checks the student view. 3 consecutive green runs against the Release portal + local fake API.
- Found and fixed while testing: the `/lti/token` relay sent an empty body (MVC had already read the form), editing a tool would have wiped its stored key.
- Totals: unit 435, integration 336 (+1 skip), web 52, contract 1.

Deliverables:
- LTI 1.3 Core + Deep Linking + AGS + NRPS and the admin tool registry. ✅ Certification and checks with real vendor tools still open (M5.6).

Acceptance:
- Teacher adds an H5P activity via Deep Linking; student completes it; score appears in EduSphere gradebook automatically. — Covered end to end with a test tool (Deep Linking → line item → score → graded submission → gradebook); to be repeated with the real H5P tool on a public deployment.

---

## Phase M6 - Learning Outcomes & Mastery
Reason:
- Required for accreditation reporting; complements the existing Accreditation module.

Module key `outcomes` ("Learning Outcomes & Mastery", licence-gated). Menus `outcomes` (Admin, Faculty) and `my_outcomes` (Student).

### Stage M6.1 - Outcome model ✅ (2026-10-19)
- [x] `Outcome` (TenantId, Level Institution / Program / Course with ProgramId / CourseId, ParentId on the same level, Code unique per scope, Title, Description, MasteryPoints, CalculationMethod Highest / Latest / Average / DecayingAverage (weight %, default 65) / NMastery (times, default 5), IsActive). Admins manage every level; teachers manage course outcomes. Outcomes are per tenant; installation-wide ones (no tenant) are managed by Super Admins.
- [x] `OutcomeRatingLevel` — each outcome has its own scale (2–10 levels, distinct points, default Exceeds 4 / Meets 3 / Near 2 / Below 1 / No evidence 0; mastery must be on the scale).
- [x] `OutcomeMapping` (CLO → PLO). Excel import with a downloadable template (create or update by level + program/course + code, parent codes, "Maps to" with `PROGRAM:CODE` or plain codes resolved within the course's department first); every row error is reported, valid rows still import.

### Stage M6.2 - Alignment ✅ (2026-10-19)
- [x] `OutcomeAlignment` (OutcomeId, TargetType Assignment / RubricCriterion / QuizQuestion / BankQuestion, TargetId, CourseOfferingId; unique per outcome + target). A course can use institution outcomes, the outcomes of programs in its department or mapped from its CLOs, and its own CLOs.
- [x] UI: one **Outcomes** button (modal with checkboxes grouped by level) on rubric criteria (Rubric Management), quiz questions (Quiz Builder), bank questions (Question Banks) and assignment settings. Buttons stay hidden when the module is off. Changing an alignment recalculates the evidence of work already graded.

### Stage M6.3 - Mastery calculation ✅ (2026-10-19)
- [x] `StudentOutcomeResult` (student, outcome, offering, alignment, source submission/attempt, Percent of the work, Score on the outcome scale, AssessedAt; unique per student + alignment + source; removed with its alignment).
- [x] Evidence comes from the existing gradebook refreshes, so every grading path counts: the M3 assignment sync (workspace grade, posting/hiding, rubric, LTI tool scores) and the M2 quiz sync (finish, time-out, manual grade, regrade) are wrapped by outcome-aware decorators. Only grades students can see count (posted assignment grades; graded answers of finished attempts). The legacy grade and rubric endpoints now refresh the gradebook and outcomes too (they did not before).
- [x] Mastery per calculation method (`MasteryCalculator`, pure): scores always use the stored share × the outcome's current top points, so a changed scale applies to old evidence. Status: mastered / near (at or above the level just below mastery) / below / no evidence.

### Stage M6.4 - Reports ✅ (2026-10-19)
- [x] Learning Mastery Gradebook (`/Portal/OutcomeMastery`): students × outcomes, colour by status, mastered counts, Excel export (ClosedXML), recalculate.
- [x] Program attainment (`/Portal/OutcomeAttainment`): per PLO, evidence aligned directly plus mapped CLO evidence on the PLO's scale and method; students assessed / attaining, attainment % against a target (default 70%), average score, mapped CLOs, contributing courses; PDF (QuestPDF, institution name from branding) and Excel export.
- [x] Student view (`/Portal/MyOutcomes`): own mastery per outcome per course with the evidence (assignment/quiz title, score, date).

#### ✅ Implementation Summary (M6)
- Domain: `Outcomes/OutcomeEntities.cs`, `Interfaces/IOutcomeRepository.cs` (incl. evidence read models).
- Application: `Outcomes/OutcomeContracts.cs`, `MasteryCalculator`, `OutcomeService` (CRUD, scale, mappings, import, alignments), `OutcomeResultSync` + `OutcomeAwareAssignmentGradebookSync` / `OutcomeAwareQuizGradebookSync`, `OutcomeReportService`; `AssignmentService` and `RubricService` call the gradebook sync.
- Infrastructure: `OutcomeConfigurations`, `OutcomeRepository`, `Outcomes/OutcomeDocuments` (import template/reader, mastery workbook, attainment workbook + PDF); migration `PlanMPhase6Outcomes` (5 tables).
- API: `OutcomesController` (`/api/v1/outcomes`: CRUD, mappings, import/template, programs, available, alignments, mastery/offerings/{id}[/export|/recalculate], mastery/me, attainment/programs/{id}[/export]); module `outcomes` in registry, seeder, licence middleware and capability matrix; menus `outcomes` / `my_outcomes`.
- Web: `PortalController.Outcomes.cs`, `EduApiClient.Outcomes.cs`, views `Outcomes`, `OutcomeMastery`, `OutcomeAttainment`, `MyOutcomes`, `wwwroot/js/outcome-align.js`, buttons in `RubricManage`, `QuizBuilder`, `QuestionBanks`, `_AssignmentSettingsForm`; sidebar routes/groups.

#### ✅ Validation Summary (Phase M6)
- Unit (11, `OutcomeMasteryTests`): every calculation method incl. dates vs list order, rating/status rules, share clamping and scaling, outcome scope rules, default scale, scale validation.
- Integration (3, `OutcomesIntegrationTests`, real API + LocalDB): PLO/CLO + mapping, alignment to an assignment, a rubric criterion and a quiz question, evidence from real grading (grade → rubric re-score → quiz attempt), mastery gradebook cells, student view, program attainment, all exports, removing an alignment removes its evidence, recalculation, permissions; validation, custom scales, mapping/alignment rules, deactivation, module gate; Excel import create/update/map with row errors.
- Web-integration (7, `OutcomesWebIntegrationTests`): outcomes page per role, form → API JSON (empty scale rows dropped), alignment relay (staff + antiforgery), gradebook statuses and Excel, attainment + PDF, student page, inactive module.
- Playwright (`tests/playwright/tests/outcomes/outcomes.spec.ts`, `@outcomes`): create a CLO with an n-times rule and map it, align a quiz question through the modal, gradebook and attainment with downloads, the student page — 3 consecutive green runs against the Release portal + local fake API.
- Found and fixed while testing: EF could not filter after projecting into records (500 on program lookups), mastery cells lost their colours to the portal table CSS, the gradebook page showed nothing when loading failed, the sidebar role matrix lacked the new menus (and M1/M5's).
- Totals: unit 446, integration 339 (+1 skip), web 59, contract 1.

Deliverables:
- Outcome hierarchy, alignments, mastery gradebook and accreditation reports. ✅

---

## Phase M7 - Module Progress & Release Conditions
### Stage M7.1 - Module items ✅ (2026-10-19)
- [x] `ModuleItem` (ModuleId, OfferingId, ItemType Header / Page / Video / Material / Assignment / Quiz / Discussion / ExternalTool / ExternalUrl, TargetId, Title, Url, Body, Position, IndentLevel 0–3, IsPublished) — modules are ordered containers of items. Items can only point to content of the same course (assignments, quizzes, discussion threads, LTI items, the course's videos and course materials).
- [x] Existing videos became Video items by migration (in the order they were added), and the video endpoints keep items in step (add → last item, delete → item removed). The module body stays as the module's introduction rather than being copied into a Page item, so nothing is duplicated or deleted. Page items are plain text (shown with line breaks, HTML-encoded).

### Stage M7.2 - Completion requirements & progress ✅ (2026-10-19)
- [x] One requirement per item, stored on the item (as in Canvas): MustView, MustMarkDone, MustSubmit, MustScoreAtLeast (percentage of a posted grade or the best finished quiz attempt; LTI items use their graded assignment), MustContribute (start or reply in the discussion); only requirements that make sense for the type are allowed.
- [x] `ModuleItemProgress` (StudentProfileId, ModuleItemId, ViewedAt, MarkedDoneAt, CompletedAt). Instead of event handlers, progress is evaluated from current data whenever it is read (views and "mark as done" from progress rows; submissions, posted grades, finished attempts and discussion posts from their tables), so no event can be missed; the first completion is stored and stays (a later regrade does not lock students out), except when "mark as done" is undone or the teacher changes the requirement.

### Stage M7.3 - Release conditions ✅ (2026-10-19)
- [x] `ModulePrerequisite` (module B locked until module A is complete; loops are refused), unlock date per module (UTC), "items in order" (sequential) option, completion "all" or "any" required item; modules without requirements count as complete once open.
- [x] Locked modules and items stay visible with the reason ("Complete “Week 1” first", "Opens 21 Oct 2026, 09:00 (UTC)"); they cannot be opened, and the API refuses to start a quiz, accept an assignment submission or launch an external tool that sits only in locked places (`ModuleGate` in the quiz, assignment, workspace and LTI controllers). Content not placed in any module is unaffected.

### Stage M7.4 - UI ✅ (2026-10-19)
- [x] Student (Course Content): course progress bar, per-module progress and lock badges, item list with status (done / to do / locked / no requirement) and requirement hints; items open through `/Portal/ModuleItem/{id}`, which records the view, renders pages, videos, links and materials, and forwards assignments, quizzes, discussions and tools to their pages. A previous/next bar (with "Mark as done") appears on any page opened from a module (`?moduleItem=`).
- [x] Teacher (LMS Manage): per-module item editor (add with type-aware fields and requirements, edit, indent, hide, move up/down, remove), release and completion settings; **Student progress** report (`/Portal/ModuleProgress`): students × modules (done/required, locks), course %, one student's item-by-item detail.

#### ✅ Implementation Summary (M7)
- Domain: `Lms/ModuleItems.cs` (ModuleItem, ModuleItemProgress, ModulePrerequisite, enums), `CourseContentModule` release fields, `IModuleItemRepository`.
- Application: `Lms/ModuleProgressEvaluator` (pure rules), `ModuleItemService` (structure, items, release, student view, open/done/nav, progress report, lock reasons), `ModuleItemContracts`; `LmsService` keeps video items in step.
- Infrastructure: `ModuleItemConfigurations`, `ModuleItemRepository`; migration `PlanMPhase7ModuleProgress` (3 tables, 3 module columns, video backfill).
- API: `ModuleItemsController` (`/api/v1/lms/offerings/{id}/structure|linkable|my-modules|progress[/{student}]`, `modules/{id}/items|items/order|release`, `module-items/{id}[/open|/done|/nav]`), `Services/ModuleGate` used by Quiz, Assignment, SubmissionWorkspace and LTI controllers.
- Web: `PortalController.Modules.cs`, `EduApiClient.Modules.cs`, `ModuleNavViewComponent` (+ layout hook), views `_ModuleItemsEditor`, `ModuleItem`, `ModuleProgress`, Course Content and LMS Manage changes, `wwwroot/js/module-editor.js`.

#### ✅ Validation Summary (Phase M7)
- Unit (14, `ModuleProgressTests`): every requirement type, unlock date and prerequisite locks with reasons, sequential locking, sticky completion, any/all, modules without requirements, prerequisite loops, item validation (targets, URLs, allowed requirements, minimum score), progress mark-done/undo.
- Integration (2, `ModuleProgressIntegrationTests`, real API + LocalDB): prerequisites, unlock date and sequential order seen by a student; the quiz gate (409) until Week 1 is done; view → page body; assignment graded 75% completes "score ≥ 60%"; quiz submission unlocks the next item; mark done/undo; course 100%; teacher report and detail; changing a requirement re-evaluates; outsiders refused; item validation, ordering, hidden items, unpublished modules, video items created and removed with videos. The backfill SQL was also checked on real rows (rolled back).
- Web-integration (5, `ModuleProgressWebIntegrationTests`): editor and forms → API JSON (add, move order, release with UTC date), course page (progress, locked items not linked), item pages (encoded text, lock, redirect, link), previous/next bar and mark done with local-only return, progress report and student access.
- Playwright (`tests/playwright/tests/modules/module-progress.spec.ts`, `@modules`): teacher builds two modules (type-aware form, reorder, prerequisite), student works through Week A with Next and Mark as done and Week B opens, teacher report, clean-up — 3 consecutive green runs against the Release portal + local fake API.
- Found and fixed while testing: a hidden boolean field (`value="@(flag)"`) renders empty, so "Mark as done" never stuck; the same pattern broke **every "Activate"/"Turn on"/"Show" toggle in the portal** (23 forms, including older pages such as Departments, Programs, Buildings, Rooms, Tenants, Campuses, Module Composition, Report Settings, Announcements, Course Material and Analytics, plus the M5/M6 tool, deployment, link and outcome toggles) — they now post `true`/`false`.
- Not changed (noted for later): the older `/api/v1/lms` module endpoints still have no tenant/enrolment checks; the new M7 endpoints do.
- Totals: unit 460, integration 341 (+1 skip), web 64, contract 1.

Deliverables:
- Item-level module structure, completion tracking, prerequisites and sequential release. ✅

---

## Phase M8 - Assignment Enhancements
### Stage M8.1 - Late & missing policy ✅ (2026-10-19)
- [x] `LatePolicy` per course offering (unique): deduction on/off, DeductionPercent per started Interval (Hour / Day), LowestGradePercent floor (a grade already below the floor is not reduced), missing-work grading on/off with MissingGradePercent.
- [x] Applied whenever a grade is saved (grading workspace, moderation, legacy grade endpoint, LTI tool scores): the submission keeps `RawMarks` (as entered), `LatePenalty` and `MarksAwarded` (final); the teacher can waive or restore the deduction per submission (audited `LatePenaltyWaived`/`LatePenaltyRestored`); students see "Marked X, late deduction −Y". Existing grades were backfilled with `RawMarks = MarksAwarded`.
- [x] Missing work: an hourly worker (`AssignmentPolicyWorker`) gives the missing grade once a student's own due date has passed with nothing handed in (also "Grade missing work now"); handing in late work later replaces it.
- [x] Assignment options: `AvailableFrom` (opens) and `AvailableUntil` (late cut-off — submissions after the due date are late, after the cut-off refused).

### Stage M8.2 - Due-date overrides ("Assign To") ✅ (2026-10-19)
- [x] `AssignmentOverride` (AssignmentId, one Student **or** one Group, DueDate, AvailableFrom, AvailableUntil, Note); a new override for the same target replaces the old one. A student's own override wins over their group's; among groups the latest due date wins. (There are no sections below a course offering, so "Section" is the offering itself.)
- [x] Used by submission (open/closed/late), late deductions, missing grades, the student's assignment page ("Your own dates") and the student's assignment list (own due date). There are no assignment calendar entries or due-date reminder notifications in the system to update.

### Stage M8.3 - Student groups ✅ (2026-10-19)
- [x] `GroupSet` (OfferingId, Name, SelfSignup, MaxSize), `StudentGroup`, `StudentGroupMember` (a student is in at most one group per set — unique index).
- [x] Teacher: create sets and groups, add/remove members, random split of everyone not yet in a group (by number of groups or group size); students join/leave self sign-up groups (size limit; no changes once the group has handed in work).
- [x] Group assignments: an assignment can use a group set; one hand-in creates the same attempt (shared files) for every member; students without a group are refused ("Join a group…"); the grouping cannot change after work is handed in. Graded as a group (one grade copied to every member) or individually.
- [x] Group discussion threads: `DiscussionThread.GroupId`; students see and reply only in their own groups' threads, start threads only in their own groups; staff see all. Portal "Post to: Whole course / Group …" and group badges.

### Stage M8.4 - Peer review ✅ (2026-10-19)
- [x] Assignment options: PeerReviewCount (0–5), PeerReviewDueDate (after the due date), Anonymous (default on).
- [x] `PeerReview` (AssignmentId, SubmissionId, ReviewerStudentProfileId, Status, Score, Comment, RubricJson): allocated automatically after the due date (worker) or by the teacher, among students who handed in work — never their own or their group's work, least-reviewed work first, then rebalanced so every piece is reviewed within one review of every other.
- [x] Reviewers see the work (text and files — the file relay allows reviewers), choose rubric levels (score scaled to the assignment's points) or give a score, and comment; reviewees see the reviews (anonymous "Peer 1" unless turned off); teachers see an overview, each submission's peer average in the grading workspace and can **Use peer average** as the mark.

#### ✅ Implementation Summary (M8)
- Domain: `Assignments/AssignmentEnhancements.cs` (LatePolicy, AssignmentOverride, GroupSet, StudentGroup, StudentGroupMember, PeerReview), `Assignment` availability/group/peer fields, `AssignmentSubmission` RawMarks/LatePenalty/LatePenaltyWaived/IsMissing/GroupId (+ MarkMissing, SetLatePenaltyWaived, penalty-aware Grade), `DiscussionThread.GroupId`, `IAssignmentPolicyRepository`.
- Application: `AssignmentPolicyService` (dates, deductions, options, overrides, missing grades, waivers, student extras, own due dates), `GroupService`, `PeerReviewService` + `PeerReviewAllocator`; workspace, legacy grading and LTI score paths apply deductions; group hand-in and group grading in the submission workspace.
- Infrastructure: `AssignmentPolicyConfigurations`, `AssignmentPolicyRepository`; migration `PlanMPhase8AssignmentEnhancements` (6 tables, 12 columns, RawMarks backfill, anonymous peer review on by default).
- API: `AssignmentPolicyController` under `/api/v1/assignment` (late-policy, apply-missing, late-penalty waiver, options, overrides, roster, group sets/groups/members/random split, my-groups/join/leave, peer-reviews allocate/overview/task/submit, my-extras), `AssignmentPolicyWorker`, group checks in `DiscussionController`, own due dates in the assignment list.
- Web: `PortalController.Policies.cs`, `EduApiClient.Policies.cs`, pages **Late policy & groups**, **Assignment options** (dates, group work, peer review, Assign to, peer overview), **My groups**, **Peer review**; extras on the student assignment page; workspace badges, late deduction with waiver, group note and peer average; group posting on Discussion; links from Assignments, the workspace and the course page.

#### ✅ Validation Summary (Phase M8)
- Unit (16, `AssignmentEnhancementTests`): daily/hourly deductions, floor, disabled policy and validation, raw/penalty/final with waiver and regrading, missing grade only without work and replaced by real work, availability and peer-review validation, override targets, date resolution (student beats group, latest group wins), allocator never assigns own/group work and stays balanced (individual and group work), peer review content rules, group set validation.
- Integration (3, `AssignmentEnhancementsIntegrationTests`, real API + LocalDB): policy validation and scope (other tenant refused), late cut-off, override for one student (list shows their own due date), late hand-in → 4 marks deducted → waive/restore, missing grade once (idempotent) and blocked submission until a late window, override replacement and removal; random split, group hand-in (one attempt per group), group grade copied to members, grouping locked after hand-in, "join a group" refusal, roster, self sign-up (full group, teacher-assigned groups), group discussion visibility/creation/replies; peer allocation (none before work, 6 reviews for 3 pieces, never twice), anonymous task with rubric, others' reviews hidden, rubric score scaling, score/comment validation, reviewee sees the review, workspace peer average, automatic allocation after the due date.
- Web-integration (5, `AssignmentEnhancementsWebIntegrationTests`): policy page values and form JSON (booleans, interval, split by size, members), options page (UTC dates, selected group set, overrides, peer overview) and form JSON, student groups/peer review (encoded text, rubric choices posted)/submission extras, group discussion posting, workspace waiver relay.
- Playwright (`tests/playwright/tests/assignments/assignment-enhancements.spec.ts`, `@assignments`): teacher policy + group set (create, add group, random split, delete), Assign to dates with refusal message and removal, workspace deduction/waiver/group/peer average, student extras → peer review → my groups join/leave → group thread — 3 consecutive green runs against the Release portal + local fake API; the M3 grading spec still passes.
- Found and fixed while testing: the peer allocator could leave reviews uneven (now rebalanced); the workspace reopened a deducted grade with the final mark in the box (now the entered mark, so saving again no longer deducts twice); discussions need a tenant scope to be listed (tests run inside a tenant/campus); inline notes must not use `.alert` (site.js turns alerts into toasts).
- Totals: unit 476, integration 344 (+1 skip), web 69, contract 1.

Deliverables:
- Late/missing policies, per-student/group dates, groups, group assignments and discussions, peer review. ✅

---

## Phase M9 - Plagiarism Detection (Direct API)
Reason:
- Optional if M5 delivers Turnitin/Copyleaks via LTI; useful earlier for markets where institutions want no extra vendor setup.

### Stage M9.1 - Provider abstraction ✅ (2026-10-19)
- [x] Two providers behind one service: a **built-in comparison** (word fingerprints with winnowing; compares with other submissions of the same assignment, course or institution — no vendor or account needed) and **Copyleaks** (`ICopyleaksClient`: API v3 login with cached token, scan submission of the extracted text, status webhook). Turnitin Core API is not implemented (it needs a partner agreement; Turnitin is available through LTI, M5).
- [x] Per-tenant `SimilaritySetting` (provider, on/off, comparison scope, Copyleaks email, **API key encrypted at rest** with the existing AES service and never returned, sandbox) with an installation default; Admin/SuperAdmin page with "Test connection". Per-assignment toggle (`SimilarityCheckEnabled`) and "show students their own score".

### Stage M9.2 - Pipeline ✅ (2026-10-19)
- [x] On submission → queued `SimilarityCheck` (one per attempt; group work once per hand-in) → `SimilarityWorker` reads the text (typed text, .txt/.md/.csv, .html, .rtf, .docx, .pdf with selectable text), stores a `SimilarityDocument` with `SimilarityFingerprint`s, then compares (built-in) or submits to Copyleaks; the Copyleaks webhook (`/api/v1/similarity/webhooks/copyleaks/{status}`, one-time token per check, stored as SHA-256) completes or fails the check and stores score and sources.
- [x] The queue is the `similarity_checks` table processed by a hosted worker (like the M3 conversion queue) rather than RabbitMQ, so checks survive restarts and need no broker; Copyleaks calls go through the existing `ResilientOutboundIntegrationGateway` (retries, timeout, circuit breaker) and failed tries back off (2, 4, 8, 16 minutes) before failing. The worker runs only while the licensed "similarity" module is active.

### Stage M9.3 - UI ✅ (2026-10-19)
- [x] Similarity badge per submission in the grading workspace list and panel (green < 25%, amber < 50%, red), **Open report** and **Check again**; report page with each source's share and the matching passages side by side (built-in) or the web/publication sources (Copyleaks).
- [x] Optional student visibility: the student sees only their own score on the assignment page — never sources or other students' work.

#### ✅ Implementation Summary (M9)
- Domain: `Similarity/SimilarityEntities.cs` (SimilaritySetting, SimilarityCheck, SimilarityDocument, SimilarityFingerprint, enums), `Assignment.SimilarityCheckEnabled/SimilarityVisibleToStudents`, `ISimilarityRepository`.
- Application: `Similarity/Fingerprinter.cs` (words, shingles, winnowing, `SimilarityScorer` score and passages), `SimilarityService` (settings, test, assignment option, queue, built-in comparison, Copyleaks submission and webhook, scores, reports, student view), `SimilarityContracts` (`CopyleaksWebhook` parser, `SimilarityOptions`).
- Infrastructure: `SimilarityRepository`, `SimilarityConfigurations`, `Similarity/SubmissionTextExtractor` (OpenXML, PDFsharp), `Similarity/CopyleaksClient`; migration `PlanMPhase9Similarity`.
- API: `SimilarityController` (`/api/v1/similarity` settings, assignments/{id}/settings|scores, submissions/{id}/checks|recheck, checks/{id}, my/submissions/{id}, webhooks/copyleaks/{status}), `SimilarityWorker`, module key `similarity` (licence middleware, registry, seeder, capability matrix), `Similarity:*` settings.
- Web: `PortalController.Similarity.cs`, `EduApiClient.Similarity.cs`, pages **Similarity Checking** (settings menu `similarity_settings`) and **Similarity report**; assignment options card; workspace badges, report link and recheck; student score.

#### ✅ Validation Summary (Phase M9)
- Unit (9, `SimilarityTests`): words and positions, deterministic fingerprints independent of case/punctuation with no window left uncovered, 100% for copies / 0% for unrelated / partial for partial copies, any shared run of 8 words found, passages located in both texts, Copyleaks webhook parsing (completed/error/informational, only http(s) links), text extraction (txt, html without scripts, docx, simple PDF, unreadable files), Copyleaks client (cached login, base64 text, webhook, sandbox, refusal), retry back-off and failure, settings and assignment validation.
- Integration (2, `SimilarityIntegrationTests`, real API + LocalDB): built-in comparison of typed text and an uploaded .docx (first hand-in 0% until **Check again**, copy ≥ 70%, unrelated 0%, docx 100%), report sources and passages, student sees only their own score while allowed, report refused to students, other tenants refused, unknown webhooks 404; Copyleaks settings validation (key stored, never returned; Admins cannot edit the installation default), test connection, submission with sandbox and webhook URL, wrong token refused, completed webhook → score 37.5 and sources, error webhook → failed with the provider's message. A switchable stand-in replaces Copyleaks in the test host.
- Web-integration (3, `SimilarityWebIntegrationTests`): settings page (provider, key never echoed, webhook note, form JSON, test, module off), report (sources, encoded passages, safe external links, staff only), workspace config and recheck relay, assignment option form, student score.
- Playwright (`tests/playwright/tests/similarity/similarity.spec.ts`, `@similarity`): checker settings with Copyleaks and back to built-in, assignment option, workspace badge → report → check again, student's own score and refused report — 3 consecutive green runs against the Release portal + local fake API.
- Found and fixed while testing: winnowing alone scored an exact copy 96% and started passages a few words late (scores now fill between matched fingerprints and passages are widened word by word); an EF query ordered by a projected record member (now ordered before projection).
- Not done (needs a vendor account): a live Copyleaks scan and webhook over the internet (`Similarity:PublicApiBaseUrl` must be the API's public https address).
- Totals: unit 485, integration 346 (+1 skip), web 72, contract 1.

Deliverables:
- Similarity checking without a vendor (built-in) or with Copyleaks; scores and reports in the grading workspace; optional student visibility. ✅

---

## Phase M10 - Live Classes / Web Conferencing
### Stage M10.1 - Provider abstraction ✅ (2026-10-19)
- [x] `IConferenceProvider` with **BigBlueButton** (signed API — `checksum = SHA1(call + query + secret)`; meetings created on demand at the first join because empty BigBlueButton meetings expire; per-person signed join links, moderator for staff), **Zoom** (Server-to-Server OAuth: account credentials token, meeting create with host start link, participant report, cloud recording link, delete) and **Microsoft Teams** (Microsoft Graph application permissions: online meetings, attendance reports, delete), plus **meeting links** (teachers paste any https link — Google Meet, Jitsi, an existing Zoom/Teams link).
- [x] Per-tenant `ConferenceSetting` with installation default (provider, BigBlueButton address, account/tenant id, client id, organiser, **secret encrypted at rest**, never returned and dropped when the provider changes, "joining opens N minutes early"); Admin/SuperAdmin page with "Test connection".

### Stage M10.2 - Scheduling ✅ (2026-10-19)
- [x] `LiveSession` (OfferingId, Title, Description, Provider, StartsAt, DurationMinutes, Status, ExternalMeetingId, JoinUrl, host link encrypted, RecordingUrl, TimetableEntryId, AutoAttendance, MinimumMinutes): scheduled ad hoc or for the next 1–20 weeks of a **timetable slot** of the course's semester (the same date is never scheduled twice); edit, move and cancel (also at the provider).
- [x] Calendar + notifications: students are notified when a class is scheduled, moved or cancelled, and reminded 15 minutes before (new notification type `LiveClass`, also a Web Push category); every class has an iCalendar file (**Add to calendar**, with a 15-minute alarm), and the user's classes appear on the academic calendar page and the SuperAdmin dashboard.
- [x] Join button: the portal's Dashboard is SuperAdmin-only (other roles land on Helpdesk), so the join button for students and teachers is a **"Live now" banner on every page** while one of their classes is open (from 15 minutes before to 30 minutes after), plus Join on the course's Live Classes page and the course page link. Joining is a POST (it is recorded), opens the meeting in a new tab and only ever redirects to http(s) links.

### Stage M10.3 - Attendance link (EduSphere advantage) ✅ (2026-10-19)
- [x] Optionally auto-mark attendance after the class (worker, 10 minutes after the end) or with **Mark attendance now**: present for students who joined through the portal — and, where the provider reports minutes (Zoom participant report, Teams attendance report; matched by email), who stayed the class's minimum time — absent for the others. Students who already have a mark for that day (e.g. a teacher's "Excused") keep it. Recording links are collected from BigBlueButton/Zoom after the class.

#### ✅ Implementation Summary (M10)
- Domain: `LiveClasses/LiveClassEntities.cs` (ConferenceSetting, LiveSession, LiveSessionAttendance, enums), `NotificationType.LiveClass`, `ILiveClassRepository`.
- Application: `LiveClasses/LiveClassService` (settings/test, scheduling, timetable weeks, update/cancel, upcoming, join, calendar, attendance report and marking, worker run), `LiveClassContracts` (`IConferenceProvider`, DTOs, `LiveCalendar` iCalendar, `LiveClassOptions`).
- Infrastructure: `LiveClasses/ConferenceProviders.cs` (BigBlueButtonProvider, ZoomProvider, TeamsProvider, token cache), `LiveClassRepository`, `LiveClassConfigurations`; migration `PlanMPhase10LiveClasses`.
- API: `LiveClassesController` (`/api/v1/live` settings, upcoming, offerings/{id}/sessions|timetable|sessions/from-timetable, sessions/{id} update|cancel|join|calendar|attendance), `LiveClassWorker`, module `live_classes`, `LiveClasses:*` settings.
- Web: `PortalController.LiveClasses.cs`, `EduApiClient.Live.cs`, pages **Live Classes** (menu `live_classes`), **Live class attendance**, **Live Class Settings** (menu `live_class_settings`), `UpcomingLiveClassesViewComponent` (card + layout banner), course page link.

#### ✅ Validation Summary (Phase M10)
- Unit (8, `LiveClassTests`): BigBlueButton checksum and URL format, on-demand create + viewer/moderator links, checksum error message, recording link, no participant report; Zoom token (Basic, account_credentials, cached), meeting body (UTC start), participant minutes summed per email; Teams token form, organiser URL, attendance report minutes; session rules and join window, reminder reset on move; per-provider completeness; timetable occurrences; iCalendar times, escaping, alarm, cancellation.
- Integration (3, `LiveClassesIntegrationTests`, real API + LocalDB, stand-in BigBlueButton/Zoom): link validation, notifications (scheduled, starting soon, moved, cancelled), join window/enrolment/outsiders, upcoming for students and teachers, calendar file, report, worker reminders + automatic attendance (present/absent, teacher's "Excused" kept), no double marking, cancel blocks joining, scope; BigBlueButton settings (secret required, not echoed, test fails then passes, Admins cannot edit the installation default), viewer/moderator links with the student's name, timetable weeks on the slot's weekday without duplicates, cancel at the provider, secret dropped when switching provider; Zoom host link for teachers, participant minutes with a 20-minute minimum, recording link from the worker.
- Web-integration (3, `LiveClassesWebIntegrationTests`): teacher page (sessions, join, timetable slots), schedule/timetable/edit/cancel forms → UTC JSON, attendance page and marking; join redirects only to web links (no `javascript:`, no foreign return URL), refusal goes back, banner on every page only for open classes, calendar file; settings without echoing the secret; SuperAdmin dashboard card.
- Playwright (`tests/playwright/tests/live/live-classes.spec.ts`, `@live`): settings + test, schedule → edit → calendar download → cancel, timetable weeks, attendance page + marking, student joins from the banner into the meeting tab — 3 consecutive green runs against the Release portal + local fake API.
- Found and fixed while testing: two EF queries filtered or joined on projected members (timetable slots), and the dashboard turned out to be SuperAdmin-only, hence the layout banner.
- Not done (needs real services): live meetings on a BigBlueButton server, a Zoom account and a Microsoft 365 tenant.
- Totals: unit 493, integration 349 (+1 skip), web 75, contract 1.

Deliverables:
- Live classes with BigBlueButton, Zoom, Teams or links; timetable scheduling; notifications, reminders and calendar files; join banner; attendance from who joined. ✅

---

## Phase M11 - Localisation (Multi-language + RTL)
### Stage M11.1 - Infrastructure ✅ (2026-10-20)
- [x] Portal: `UseRequestLocalization` with one `PortalRequestCultureProvider` — `?lang=` → the signed-in user's saved language (read from the API once per session) → the language cookie → the institution default → the browser's Accept-Language (only when the default is "browser") → English. English keeps the server's culture, so English pages format exactly as before; Urdu (ur-PK) and Arabic (ar-SA) use the Gregorian calendar and "." / "," number separators so dates and numbers typed into forms mean the same in every language.
- [x] User setting → institution default → browser: `users.PreferredLanguage` (migration `PlanMPhase11Localisation`), `api/v1/localization` (languages and the default — readable before sign-in for the login page; SuperAdmin sets it, audited), `api/v1/localization/me` (each user's choice). The default lives in the portal settings (`default_language`: en, ur, ar or auto).
- [x] Resources: instead of `.resx`, translations are JSON keyed by the English text (decision: ~3,000 portal texts with values in them, Excel round trips for translators, overrides without a release). Portal: `Localization/translations/{ur,ar}.json` + `keys.json` (where each text is used); API: embedded `Localization/Resources/api-{ur,ar}.json` (validation messages, error messages incl. "{0}" patterns, email subjects, PDF labels). The portal sends its language to the API (Accept-Language); the API sets the UI culture (Content-Language), and `LocalizedResponseFilter` translates `{ message }` answers, problem details and validation errors keeping the JSON shape. Emails pick `{template}.{ur|ar}.html` (right-to-left) from the recipient's language (own choice, else the institution default) — account unlocked, password reset, notification alerts.

### Stage M11.2 - String extraction ✅ (2026-10-20)
- [x] All views (132 files): a Razor-aware extraction tool wrapped 3,638 text segments — static text `@T["…"]`, text with values `@T.Format("Due {0} · out of {1}", …)`, placeholder/title/aria-label/alt attributes, page titles — plus 222 display literals inside inline expressions (`@(x ? T.Text("Passed") : …)`) and 51 `confirm('…')` dialogs (`T.Js`, script-safe). English output is byte-for-byte the markup it replaced (all web tests unchanged).
- [x] Scripts: 164 messages in 11 portal scripts use `t('…')`; the page embeds that language's script texts (`portal-text`), spaces around fragments kept. Flash messages, model messages, login errors and page titles set by controllers are translated where pages show them (patterns translate messages with numbers/names, e.g. "Attendance marked for 12 student(s).").
- [x] Menus and vocabulary: sidebar names from the API, group headings and roles are translated; the institution vocabulary (Semester/Grade/Year, Course/Subject …) is chosen by `LabelService` first and then translated.
- [x] Every area (Admin, Faculty, Student, Parent, Finance pages) is covered: 3,002 portal texts and 335 API texts, all translated into Urdu and Arabic.

### Stage M11.3 - First languages + RTL ✅ (2026-10-20)
- [x] English (default), Urdu, Arabic. `<html lang dir>`, Bootstrap RTL stylesheet, site CSS converted to logical properties (padding-inline-start …, inset-inline-start …) plus RTL rules for transforms, the off-canvas sidebar and left-to-right fields (email, URL, numbers); Noto Nastaliq Urdu / Noto Sans Arabic fonts. Language switcher in the top bar and on the login page; Razor writes Urdu/Arabic as text (not entities).
- [x] Localised dates/numbers (month and day names from the culture, Gregorian). PDFs: QuestPDF pages run right to left with Arabic-script font fallbacks for Urdu/Arabic requests (reports, analytics, outcomes, timetable, audit); Report Center PDF titles and column headers are translated. The API Docker image installs Noto fonts. Official certificates keep their template language.

### Stage M11.4 - Translation workflow ✅ (2026-10-20)
- [x] **Language & Translations** page (SuperAdmin; menu `language_settings`): institution default language, coverage per language, **Export to Excel** (English key, Urdu, Arabic, where used + a Help sheet) and **Import** — only changed texts are stored as overrides (`Localization:OverridesPath`, default `App_Data/translations`), placeholders must be kept, unknown rows are skipped, the portal uses them at once.
- [x] Missing-key report in CI: unit tests fail when a key lacks an Urdu/Arabic translation, placeholders differ, or any `T[...]`/`T.Format`/`T.Text`/`T.Js` text in a view or `t('…')` in a script has no key.

#### ✅ Implementation Summary (M11)
- Application: `Localization/Localization.cs` (`PortalLanguages`, `ITextLocalizer`/`TextLocalizer` with "{0}" patterns, `IPortalLanguageService`), embedded `api-ur.json` / `api-ar.json` (named so MSBuild does not turn them into satellite assemblies); `AccountSecurityService` and `NotificationService` send emails in each recipient's language.
- Domain/Infrastructure: `User.PreferredLanguage`, migration `PlanMPhase11Localisation`, `INotificationRepository.GetActiveUserEmailLanguagesAsync`, `EmailTemplateRenderer` language variants + 6 Urdu/Arabic templates, `Reporting/PdfLanguage` applied to the QuestPDF documents.
- API: `LocalizationController`, `RequestLanguageMiddleware` (UI culture + Content-Language), `LocalizedResponseFilter`; security whitelist adds the read-only `LocalizationController.Get`.
- Web: `Localization/PortalText.cs` (`IPortalText` = `T` in every view, `TranslationStore` with overrides and patterns), `PortalCulture.cs` (cultures, resolver, provider), `TranslationWorkbook.cs`, `LanguageController` (`/Language/Set`), `PortalController.Language.cs`, `_LanguageSwitcher`, `LanguageSettings` page, `wwwroot/js/i18n.js`, RTL CSS, Accept-Language on API calls.

#### ✅ Validation Summary (Phase M11)
- Unit (27, `LocalizationTests`): language normalising and Accept-Language ranking; Gregorian/"." cultures parse form values; API catalogue complete with matching placeholders and embedded; exact/pattern translations; portal files complete (no missing, no orphan, placeholders kept); every view/script text has a key; HTML-safe output (translations cannot inject markup), formats with HTML/text values, script escaping; overrides; Excel export/import (changed only, placeholder check, unknown rows); API message filter keeps the JSON shape; localized email templates (path-safe); timetable PDF in Urdu/Arabic/English.
- Integration (5, `LocalizationIntegrationTests`, real API + LocalDB): public default and SuperAdmin-only change (Admin 403, anonymous 401, invalid 400), user choice save/clear, Arabic error message + Content-Language, Urdu validation errors, Arabic PDF export, recipients' languages for notification emails.
- Web-integration (4 new): login page by query/default/browser/cookie, switcher (safe return URL, rejects unknown languages), saved language wins after sign-in, translated menus, API told the language, flash message in the page language, Language & Translations SuperAdmin-only with Excel export → edit → import → override used; Arabic live-class page still posts UTC dates.
- Playwright (`tests/playwright/tests/i18n/localisation.spec.ts`, `@i18n`): login page Arabic/Urdu/English, portal Arabic (sidebar on the right, translated menus, `t()` script texts) and Urdu (Nastaliq font), choice kept after signing in again, default language for new visitors, Excel export — 3 consecutive green runs against the Release portal + local fake API.
- Found and fixed while testing: MSBuild treated `api.ur.json` as an Urdu satellite resource (renamed `api-ur.json`); Razor wrote Urdu/Arabic as `&#x…;` entities (WebEncoderOptions now allow all Unicode); null-conditional output `@st?.Level` was split by the extractor (restored); messages translated when stored showed in the old language after changing the default (now translated where shown).
- Not done: a native speaker's review of the Urdu and Arabic texts (the Excel workflow is ready for it); API messages outside the catalogue (335 texts) stay in English.

Deliverables:
- English, Urdu and Arabic portal with right-to-left layout, translated emails and PDFs, user/institution/browser language choice, and an Excel translation workflow with CI checks. ✅

---

## Phase M12 - Native Mobile Apps
Reason:
- After PWA (M4) proves demand; needs SSO (M1) for institutional login.

### Stage M12.1 - Technology decision ✅ (2026-10-23)
- [x] .NET MAUI chosen over Flutter/React Native (same language and team, shared tested core library, built-in system-browser sign-in and secure storage); one app with role tabs instead of three apps; quizzes stay in the portal. Recorded in `Docs/Decisions/ADR-M12-mobile-technology.md`.
- [x] API readiness review (same ADR): mobile read models in one call per screen (`api/v1/mobile/*`), bounded lists, names instead of ids, refresh rotation kept (app refreshes once behind a lock), one access rule for student/parent reads, app version gate (`X-App-Version` → 426), native push (FCM v1 with APNs relay), Accept-Language. Found: the API's rate-limit policies are defined but not attached (→ M14).

### Stage M12.2 - Student app MVP ✅ (2026-10-23)
- [x] Sign-in with password + authenticator code, or the institution's SSO in the system browser: the app opens `/Login/Sso/{provider}?appChallenge=` (PKCE S256), the portal finishes OIDC/SAML/2FA, gets a one-time code bound to the challenge (`POST api/v1/mobile/handoff`, 120 s, single use, stored hashed), ends its own session and redirects to `{scheme}://sso?code=`; the app redeems it with its verifier (`POST api/v1/mobile/handoff/redeem`). A plain portal sign-in clears an abandoned app hand-off.
- [x] Home (today's classes, due assignments/quizzes for 14 days, announcements, attendance %, CGPA, fees due), timetable (week), courses (materials, assignments with hand-in by camera photo or files, quizzes opened in the portal, announcements, discussions), results, attendance per course, report cards, notifications (paged, mark read), settings (language en/ur/ar with right-to-left, sign out). Session kept in secure storage.

### Stage M12.3 - Teacher app ✅ (2026-10-23)
- [x] Take attendance for a class and day (tap to cycle present/absent/late/excused), grade submissions (marks + feedback), post course announcements, course discussions as the messaging channel with students (start a discussion, reply). Home shows classes today and submissions to grade.

### Stage M12.4 - Parent app (EduSphere advantage) ✅ (2026-10-23)
- [x] My children: per child results, attendance, report cards (`api/v1/mobile/report-cards`, the stored report-card details shown as readable lines), fee status, CGPA and announcements; notifications. Only actively linked children (403 otherwise).

### Stage M12.5 - Release ✅ (2026-10-23)
- [x] Per-institution white-label build: `mobile/build-whitelabel.ps1` (name, application id, API address, colour, sign-in scheme, version/build, Firebase file, upload keystore from environment variables) → signed `.aab`/`.apk` (Android) or `.ipa` (iOS, on a Mac). The scheme is compiled into the Android intent filter and must match `Mobile:AppRedirectUri` on the API and portal.
- [x] Store listing texts (EN/UR/AR) and privacy notes, versioning and release policy (`Mobile:MinimumVersion` / `LatestVersion`, build numbers, two-minor-version API compatibility) — `mobile/README.md`.
- [x] Crash reporting: self-hosted (`api/v1/mobile/crash-reports`, `mobile_crash_reports`), kept on the device when offline and sent after the next sign-in.
- [x] Native push: device registry (`mobile_devices`), FCM HTTP v1 sender (service-account RS256 JWT, token cached, dead tokens removed, devices switched off after 5 failures) sent with Web Push from the dispatch worker and the same per-type preferences; Android Firebase Messaging compiled in when the institution's `google-services.json` is supplied; tapping a notification opens the matching tab.

#### ✅ Implementation Summary (M12)
- Domain/Infrastructure: `MobileDevice`, `MobileCrashReport` (migration `PlanMPhase12Mobile`), `MobileRepository`, `MobileQueries` (home/timetable/courses/course/results/attendance/report cards/children), `FcmPushSender`.
- Application: `Mobile/MobileContracts.cs` (options, records, interfaces), `MobileService` (config, hand-off, devices, crash reports, push), `ReportCardLines`; mobile messages added to the Urdu/Arabic API catalogue.
- API: `MobileController` (`config` and `handoff/redeem` anonymous, everything else signed in); push dispatch worker also sends native push. Portal: `LoginController` app hand-off; `Mobile:AppRedirectUri` setting.
- App: `src/Tabsan.EduSphere.Mobile.Core` (client, view models, texts — in the main solution) and `mobile/Tabsan.EduSphere.Mobile` (MAUI, net9.0; Android + iOS; Windows for development), `mobile/build-whitelabel.ps1`, `mobile/README.md`.

#### ✅ Validation Summary (Phase M12)
- Unit (26, `MobileAppTests`): sign-in with MFA, error statuses, SSO through the portal with PKCE, refresh once and sign-out on failure, uploads as multipart, discussions start/reply, report cards, report-card line formatting, version policy, crash reports kept and sent, app texts complete in Urdu/Arabic with the same placeholders, view models, timetable; server: config, single-use hand-off bound to the verifier, devices follow the signed-in user, native push respects preferences/removes dead tokens/stops after failures, FCM JWT and v1 messages.
- Integration (5, `MobileIntegrationTests`, real API + LocalDB): config before sign-in; hand-off (bad challenge 400, anonymous 401, redeem once, wrong verifier 400, tokens work); student home/timetable/courses/course/results/attendance/report cards; outsider 404/403; teacher and parent views, parent only for linked children; devices, crash reports, minimum version 426.
- Web-integration (8, `MobileHandoffWebIntegrationTests`): app sign-in ends in the app with a one-time code and no portal session, malformed challenges rejected before the identity provider, portal sign-in after an abandoned app sign-in stays in the portal, refused hand-off shows an error, the app address comes from configuration and is never a web address.
- Builds: Android (`.apk` 28 MB, with and without Firebase), white-label build "City University" (scheme `cityuni` in the manifest), Windows. Not done here: iOS build/App Store (needs a Mac), a run on physical phones, a live FCM send (needs the institution's Firebase project).

Deliverables:
- One EduSphere app for students, teachers and parents on Android and iOS with SSO, push, camera hand-in, three languages, white-label store builds and crash reports. ✅

---

## Phase M13 - SCORM / xAPI Packages (on demand)
### Stage M13.1 - SCORM 1.2 / 2004 player ✅ (2026-10-23)
- [x] Upload zip → validate `imsmanifest.xml` → extract to storage; module item type `ScormPackage`. Packages are checked before anything is written: zip-slip/absolute/drive paths, duplicate names, file count (`Scorm:MaxFiles`), size after extraction (`Scorm:MaxUncompressedMb`, entries cut off if larger than their header says), suspicious compression ratios, manifest read without DTDs/external entities, launch file present and inside the package, no launching from other websites. Version from `schemaversion`/namespaces, launch = default organization's first SCO (with `xml:base` and item parameters), mastery score from `adlcp:masteryscore` (1.2) or `minNormalizedMeasure` (2004). Files go to `Scorm:StoragePath` per package. Teachers manage packages on the course content page (upload, title, attempts, visibility, graded + marks, preview, report, remove); `ScormPackage` module items with View / Mark done / Complete / Score at least.
- [x] Player iframe using the scorm-again JS runtime (3.4.6, MIT, vendored): the portal frame loads a shell under `/scorm/play/{token}/` that creates `window.API` / `window.API_1484_11` with the attempt's CMI data, then loads the SCO below it (same origin, so SCO API discovery, multi-frame SCOs and XHR work). The play link is a sealed, time-limited token (attempt + learner, data protection); the shell, files (byte ranges for media) and commits are relayed to the API without a portal session, so the player can run on a separate cookie-less content origin (`Scorm:ContentOrigin`, frame-ancestors `Scorm:PortalOrigin`); responses send no referrer. CMI data persisted per `ScormAttempt` (completion, success, score raw/min/max/scaled → percent, location, suspend_data, exit, time on the final commit, the whole CMI for resume); resume sets `entry=resume`; finished attempts reopen in review mode (no credit, nothing recorded); new attempts on request within `MaxAttempts`; teachers preview without recording. 1.2 mastery decides passed/failed from the raw score; 2004 uses the scaled passing score.
- [x] Score → gradebook; completion → module progress (M7). Graded packages get an assignment (like LTI line items); the best score of finished attempts becomes the student's graded submission (late policy, posting policy and the gradebook sync apply) — students cannot upload to tool/package assignments. Module evidence: finished = submitted; best finished score for "score at least"; locked module items keep the package closed.

### Stage M13.2 - xAPI (optional) ✅ (2026-10-23)
- [x] Minimal LRS endpoint (statements store): `api/v1/xapi/statements` (POST single/batch, PUT by id, GET by id/voided id or filtered by agent, verb, activity, registration, since/until, with `more` paging) and `about`, xAPI 1.0.3 headers. Activity providers use HTTP Basic credentials created on the **Learning Record Store** page (secret shown once, stored hashed, switch off). Statements are validated (one inverse-functional identifier, verb/object IRIs, scaled score range), stored with id/stored/authority/version, conflicting ids refused, `voided` statements hide their target. Documents/state APIs and external LRS forwarding are not included.

#### ✅ Implementation Summary (M13)
- Domain/Infrastructure: `Lms/Scorm.cs` (`ScormPackage`, `ScormAttempt`, `XapiCredential`, `XapiStatement`), `Assignment.ScormPackageId`/`ForScormPackage`/`IsCompletedElsewhere`, `ModuleItemType.ScormPackage`; migration `PlanMPhase13Scorm` (`scorm_packages`, `scorm_attempts`, `xapi_credentials`, `xapi_statements`); `ScormRepository`, `LocalScormContentStore`, module linkables/evidence.
- Application: `Lms/Scorm/` — `ScormPackageReader`, `ScormCmi`, `ScormService`, `XapiService`, contracts; SCORM/xAPI messages in the Urdu/Arabic API catalogue.
- API: `ScormController` (staff, students, token-authorised play endpoints), `XapiController`, `ScormPlayTokens`; `Scorm` settings; security whitelist adds the play and LRS endpoints.
- Web: `ScormPlayController` (shell, content and commit relay), `PortalController.Scorm.cs` (package page, launch, player page, report, upload/edit/preview/remove, LRS page), `ScormPackages` view component on the course pages, module editor/redirect, `wwwroot/lib/scorm-again`, `scorm-player.js`, `scorm-page.js`; sidebar menu `xapi_settings`; 74 new portal texts in Urdu/Arabic.

#### ✅ Validation Summary (Phase M13)
- Unit (`ScormTests`): manifests 1.2 and 2004 (folder packages, xml:base, parameters, mastery), zip-slip/absolute/drive paths, missing/invalid manifests, XXE refused, file-count, size and compression-ratio limits, duplicate names, launch URL encoding; CMI status mapping, scores/percent, mastery, time formats, resume/review start data and clean-up; attempt finishing and time; package rules; xAPI credentials (hashed, Basic, switch off), statement validation, conflicts, batches, voiding, queries and paging.
- Integration (3, `ScormIntegrationTests`, real API + LocalDB): upload → launch → content → suspend/resume → passed 85% → graded submission 17/20 posted → module item done; review, second attempt, attempt limit 409, review commits ignored, report; bad packages explained; outsider 403; staff preview records nothing; hide, grade later, remove (assignment removed when ungraded); students cannot upload to the package assignment; LRS end to end (version header, Basic auth, PUT/POST, conflict, queries, admin read, voiding, switched-off credentials).
- Web-integration (4, `ScormWebIntegrationTests`): content-page card (upload streamed as multipart, edit, remove, preview, report), student package page and launch with a separate content origin, player relay without a session (shell headers, JSON-encoded data, content with byte ranges, 404s, commits incl. text/plain final commit), LRS page with the one-time secret.
- Playwright (`tests/playwright/tests/scorm/scorm-player.spec.ts`, `@scorm`): a SCORM 1.2 and a SCORM 2004 SCO in Chromium find the runtime, read the learner, commit scores/statuses and finish; commits reach the API through the relay; "progress saved" shows — 3 consecutive green runs against the Release portal + local fake API.
- Found and fixed while testing: the shell script ran before its frame existed; site.js turns page-load alerts into toasts (the "progress saved" notice is now a status box); a short package could finish before the page listened (the page now loads the player after registering its listener).
- Not done: a run with commercial packages (Articulate/iSpring/Captivate exports).
- **Packages in parts (2026-10-25):** every launchable item of the default organization is now a part (`ScormPackage.PartsJson`, at most 200), each part keeps its own CMI data and status (`ScormAttempt.PartsStateJson`), and the attempt rolls up from the parts — completed when every part is done, failed if a part failed, passed when every SCO passed (a package of pages only has no pass/fail), score = average of the parts' scores, time = their sum. The player page shows a contents list with each part's status and Previous/Next; switching asks the open part for a last commit, and each part commits to `commit/{part}`. Asset parts count as done once opened. Migration `PlanMScormMultiPart`. Sequencing rules and the upgrade of older packages followed the same day (below).
- **Real packages (2026-10-25):** the six Rustici *Golf Explained* samples (run-time basics 1.2 and 2004, advanced run-time 2004, one-file-per-SCO 1.2 and 2004 with 18 parts, simple remediation 2004 with 8 SCOs) import with the right version, parts, launch pages (query strings, item parameters) and mastery scores, and play in headless Chrome as a student: single-SCO 1.2 and 2004 paged through with their own buttons, test passed → passed 100% and 10/10 posted to the gradebook; 18-part course completed by opening each part with Next (every badge Completed); 2004 lesson and quiz parts recorded separately (Passed / Passed 100% / Not started) with the attempt In progress.
- Found and fixed: the contents list lagged one part behind (now refreshes when a part loads); a package of pages only rolled up to "passed" (now completed without pass/fail).
- **SCORM 2004 sequencing (2026-10-25):** the manifest's IMS Simple Sequencing is read into an activity tree (`ScormSequencingReader`, stored as `ScormPackage.SequencingJson`; `sequencingCollection` IDRefs, control modes, pre/exit/post-condition rules, limit conditions, rollup rules and considerations, objectives with global `mapInfo`, randomization, delivery controls, `hideLMSUI`, `isvisible`). `ScormSequencer` runs the learner's navigation requests (start/resume, continue, previous, choice, exit, exitAll, suspendAll, abandon) against the attempt's state (`ScormAttempt.SequencingStateJson`): skip/disabled/hiddenFromChoice/stopForwardTraversal, choice and flow control (choiceExit, forwardOnly), retry/retryAll/exitParent/exitAll/continue/previous post rules, attempt limits, randomized and selected children, measure/objective/completion rollup (rules or defaults, weights, satisfiedByMeasure), shared global objectives, and completion/objective set by the LMS when content does not. Continue, previous and choice are tried on a copy first, so a refused request changes nothing. The player page sends Next/Previous/contents clicks and the SCOs' `adl.nav.request` to `POST play/{token}/navigate`; parts that cannot be chosen show a padlock and the reason; the frame says "You have reached the end of this package" when nothing is left; each delivery of a part is an attempt and commits go to `commit/{part}/{attempt}`, so a late commit from a replaced frame is ignored. The package's own rollup gives the attempt's completion, success and score (root measure, else the parts' average). Preview and review open every part freely. SCORM 1.2 `adlcp:prerequisites` (AICC script: `&`, `|`, `~`, `=`/`<>` status, `n*{…}`) lock parts until met. Migration `PlanMScormSequencing` (`SequencingJson`, `StructureVersion`, `SequencingStateJson`).
- **Older packages (2026-10-25):** `ScormStructureUpgradeWorker` reads again, at start-up, the manifest of every package below the current structure version (2): parts, sequencing and prerequisites are applied without a new upload; attempts made when only the first part launched keep their data on that part, and progress made before sequencing is brought into the package's state (an Etiquette quiz passed earlier still counts).
- Real packages (Rustici *Golf Explained* sequencing samples, played as students in headless Chrome against the running portal): **Forced Order** — later parts locked until the previous one is passed, Next refused with the reason, then opens each part in turn, quiz passed → package passed and 10/10 to the gradebook; **Simple Remediation** — a failed Etiquette quiz sends the learner back to the Etiquette lesson only (passed quizzes skipped), then the quiz again, then the package ends passed; **Random Test** — a random hidden question per post-test attempt, two attempts, then the package ends failed (attempt limit); **Pre or Post Test** — a passed pre test passes the package and closes the post test while the lessons stay open; progress from before the upgrade kept.
- Found and fixed while playing them: the old part's last commit and the next part's opening reached the API together and one of them failed on the attempt's row version, showing "This link has expired" in the frame — the play, navigate and commit steps now read the attempt again and repeat (up to five times) on a concurrency conflict, and the frame shows "could not be opened just now — reload" instead of "expired" for anything but an unknown link; re-opening the part already open re-ran the checks for a new attempt (a post test on its last attempt is "disabled"), which restarted the package — the open part now simply carries on.
- Tests: `ScormSequencingTests` (forced order, remediation, random post test with attempt limit and re-opening, rollup rules/measure/hidden/choiceExit, delivery controls and suspend, no sequencing, SCORM 1.2 prerequisites), integration `SequencedPackage_DecidesWhatOpens_IgnoresStaleCommits_AndOlderPackagesAreReadAgain` and `SequencedPackage_NextPartOpening_AndThePreviousPartsLastCommit_ArriveTogether_AndBothSucceed` (fails with a 500 without the retry), web `SequencedPackage_ThePackageDecides_WhatOpens_AndCommitsNameThePartsAttempt` and `Teacher_SeesThatASequencedPackageSetsItsOwnOrder`.

Deliverables:
- SCORM 1.2/2004 packages in courses and modules with resume, attempts, review, gradebook scores and module completion, and a minimal xAPI LRS. ✅

---

## Phase M14 - Accessibility (WCAG 2.1 AA) & Scale Hardening
### Stage M14.1 - Accessibility audit ✅ (2026-10-24)
- [x] axe-core in Playwright tests for all portal pages; fix contrast, labels, focus order, keyboard traps, ARIA on custom widgets (quiz builder, grading workspace, annotation tools). `tests/playwright/tests/a11y/accessibility.spec.ts` (`@a11y`, @axe-core/playwright 4.13.0) visits every portal page action (`portal-pages.json`: 111 staff pages, 30 student pages) plus the sidebar links and the login page with the WCAG 2.0/2.1 A and AA rules and fails on serious/critical findings. First run: 105 findings in 9 rules; now **0**. Fixes: 470+ form controls named from their visible labels (`for`/`id`, or `aria-label` with the label's text in repeated rows), close buttons and icon-only links named, labelled regions for the grading-workspace and submission viewers, underlined links in running text, darker muted text and outline buttons on light panels, a 3 px keyboard focus ring. A keyboard walk (skip link first, no traps, every focus stop visible) covers the dashboard, quiz builder, grading workspace, question banks and course content pages.
- [x] Publish VPAT / Accessibility Conformance Report: `Docs/Accessibility-Conformance-Report.md` (VPAT 2.5 WCAG structure; every A/AA criterion with Supports/Partially supports and remarks; known limitations: pointer-only area annotation, no time-out warning, dark themes/branding colours not audited, no assistive-technology user testing yet).

### Stage M14.2 - Performance & scale ✅ (2026-10-24)
- [x] Load test (tests/load) for 5,000 concurrent users on exam-day scenario (quiz start/submit spike) and results publication: `tests/load/k6-exam-day.js` — students arrive over a start window, load the paper, autosave several times, submit near the deadline; then the teacher queues the results publication and students open their results; thresholds on p95 start/save/submit and errors. Seeding: the opt-in `ExamDayLoadSeed` integration test creates a course, a 10-question quiz and N enrolled students with tokens (`EDUSPHERE_LOAD_SEED_FILE`). Run here (one workstation, LocalDB, Release API): 500 students arriving over 60 s, 2 minutes of work, 4 autosaves each, then students reading results — 0.00 % errors, no 429s; p95 start 46 ms, paper 50 ms, autosave 65 ms, submit 74 ms (max 2.1 s), results 123 ms; 4,100 requests, peak 701 virtual users (details in `tests/load/README.md`). The teacher's publication step was checked in a 16-user smoke run. The 5,000-user run needs a staging environment (several API instances + SQL Server + Redis); the script and README describe it.
- [x] Redis caching for hot reads; DB index review for new tables; file storage abstraction (local / Azure Blob / S3) for submissions and conversions. Redis is the distributed cache whenever `ScaleOut:RedisConnectionString` is set (required outside Development/Testing), and the per-page reads (sidebar, live-class banner, branding, policies) were already cached; the exam-day run showed no hot path that needed more. Index review: SQL Server's missing-index statistics were empty after the load run and on the dev database; the schema check found two unindexed foreign keys (`rooms.BuildingId`, `incident_logs.AssignedTo`), now indexed. Storage: `IObjectStore` with a local folder, **Azure Blob Storage** (Azure.Storage.Blobs 12.30) and **Amazon S3 / S3-compatible** (AWSSDK.S3 4.0; MinIO, R2, Wasabi) backends; `MediaStorage:Provider = AzureBlob | S3` puts submissions, conversions, uploads and SCORM package files in the shared store (signed read links: SAS / pre-signed URLs). The Blob container is created on the first write (a storage outage cannot stop the API starting); `S3:DisablePayloadSigning` uploads with an unsigned payload over HTTPS for services without streaming signatures (Cloudflare R2).
- [x] Request limits (found in the M12 API review: policies existed but were never applied): sliding one-minute windows per API token, per signed-in user and per address without sign-in; `ExemptIps` (addresses or ranges such as 10.0.0.0/8, default the server itself) for the portal server, which signs everyone in; 429 with Retry-After (`RateLimiting` settings; off in the Testing environment).

### Stage M14.3 - Public API & developer docs ✅ (2026-10-24)
- [x] Versioned public REST API documentation (Swagger already present) + API tokens per integration user — matches Canvas's open API story. OpenAPI v1 with a description of authentication, limits, conventions and the v1 compatibility rule, XML comments, one Bearer scheme for a sign-in JWT or an `esk_` token. Found: the OpenAPI document had never generated (500) — six upload actions put `[FromForm]` on `IFormFile` parameters, which Swashbuckle rejects; fixed, and a test now generates the document; snapshot `Docs/api/openapi-v1.json`; developer guide `Docs/Public-API.md` (tokens, conventions, limits, versioning/deprecation, common integration tasks). Personal access tokens (`esk_…`, SHA-256 hash stored, shown once): created by allowed roles (`ApiTokens:AllowedRoles`, default Admin/SuperAdmin) on the **API Tokens** page or `api/v1/api-tokens`, act as their owner with the same claims as a sign-in, optional read-only, expiry up to 365 days, revocable (also by administrators), cannot manage tokens, limited per token, audited.

#### ✅ Implementation Summary (M14)
- Portal: label/name fixes across 106 views, CSS (focus ring, contrast, link underlines), `ApiTokens` page + menu `api_tokens`, 33 new texts in Urdu/Arabic.
- Application/Infrastructure: `IObjectStore` + `FileSystemObjectStore`, `AzureBlobObjectStore`, `S3ObjectStore`, `ObjectStoreScormContentStore`; `ApiToken` entity, `ApiTokenService`, `ApiTokenRepository`; migration `PlanMPhase14ApiTokensIndexes` (`api_tokens`, two foreign-key indexes).
- API: `ObjectStoreMediaStorageService`, provider selection; `ApiTokenAuthenticationHandler` with a JWT-or-token scheme selector, `ApiTokenGuardMiddleware`, `ApiTokensController`; `ApiRateLimiting` (global partitioned limiter after authentication); Swagger description and XML docs; `[FromForm]` removed from six `IFormFile` parameters.
- Tests/tools/docs: `tests/playwright/tests/a11y/*`, `tests/load/k6-exam-day.js` + README section, `ExamDayLoadSeed`; `Docs/Accessibility-Conformance-Report.md`, `Docs/Public-API.md`, `Docs/api/openapi-v1.json` (731 paths, 843 operations).

#### ✅ Validation Summary (Phase M14)
- Accessibility: axe 0 findings on 142 pages; keyboard walk green; portal web tests 91/91 and localisation tests 27/27 after the view changes.
- Load: exam-day run with 500 students on one workstation: 0 errors, p95 start 46 ms / autosave 65 ms / submit 74 ms / results 123 ms; 16-user smoke run incl. the results publication; missing-index statistics empty afterwards.
- Unit: `ObjectStoreTests` (the same behaviour from the local, Azure Blob and S3 stores — the cloud SDKs talk to in-memory fake services; media storage keeps hash/type/file name; SCORM files per package; SAS and pre-signed links; requests signed; streaming-signed and unsigned-payload S3 uploads), `ApiTokenTests` (roles, lifetime, hash-only storage, caching incl. misses, revocation, expiry, per-user cap), `ApiRateLimitingTests` (token / user / address allowances, exempt addresses and ranges).
- Integration: `ApiTokenIntegrationTests` (tokens act as the owner, read-only enforced, tokens cannot manage tokens, revoked/expired/unknown refused, role and lifetime rules; limits per user and per address with 429 + Retry-After; the OpenAPI document generates for every endpoint).
- Web-integration: `ApiTokensWebIntegrationTests` (list, create shown once, revoke, administrators' list, students refused).
- Final sweep (2026-10-24): unit 597/597, integration 367/368 (1 existing skip), web-integration 93/93, contract 1/1, Plan M Playwright specs 41/41 (SSO, assignments, grading, localisation, live classes, LTI, module progress, outcomes, PWA, SCORM, similarity, accessibility) against the Release portal + fake API.
- Found and fixed in the sweep: since M11 the script translation helper (`i18n.js`) was deferred, so it ran after the page scripts — scripts that call `t()` while loading failed, and the grading workspace stayed empty (the M3/M8/M9 browser specs had not run since M11). The helper now loads before page scripts (web test guards it). With the workspace rendering, the scan and keyboard walk found three more issues there, now fixed: red text on light panels (contrast), the scrollable document viewer not reachable by keyboard, and `scrollIntoView` moving the browser's Tab starting point into the student list.
- Not done here: the full 5,000-user run (needs staging), a live Azure/S3 account run, assistive-technology user testing.

Deliverables:
- WCAG 2.1 AA audit with zero automated findings and a conformance report; exam-day load test and results; shared cloud storage; request limits; public API with tokens and documentation. ✅

---

## Out of Scope (EduSphere already ahead — protect, don't rebuild)
SIS & academic structure, results/GPA calculation, degree audit & graduation, transcripts/certificates, timetable/rooms, attendance, fees/payments, FYP, parent portal, AI chat & study planner, helpdesk, incidents, compliance/ISO modules, licensing & multi-tenancy, theming/sidebar customisation.

## Change Log
| Date | Change | By |
|---|---|---|
| 2026-10-18 | Plan M created from Canvas LMS gap analysis | — |
| 2026-10-18 | Stage M1.1 completed (SSO data model, migration, module registration) | — |
| 2026-10-18 | Stage M1.2 completed (OIDC sign-in: Microsoft / Google / generic, portal flow, MFA step) | — |
| 2026-10-18 | Stage M1.3 completed (SAML 2.0 SP via ITfoxtec 4.21.0; IdentityModel aligned to 8.22.0) | — |
| 2026-10-18 | Stage M1.4 completed (EnforceSsoOnly, auth method in login activity, linked-account API + audit) | — |
| 2026-10-18 | Stage M1.5 completed (SSO admin page + API, linked accounts card, sidebar entry) | — |
| 2026-10-19 | Stage M1.6 completed (real-protocol pipeline tests, Playwright SSO spec, SSO Setup Guide) — Phase M1 done pending a live IdP sign-in | — |
| 2026-10-19 | Phase M2 completed (M2.1–M2.7): answer model, question banks + Excel, 11 question types with graders, attempt snapshot, accommodations/overrides with server deadline, security & review options, grading queue, regrade, item analysis, portal builder/take-quiz/review/grading/statistics pages. Open: quiz scores into gradebook; deferred: QTI import, section overrides, rich-text essays, Playwright specs | — |
| 2026-10-19 | Phase M3 completed (M3.1–M3.6): submission attempts/files/comments (voice), Gotenberg conversion queue, PDF.js annotation + annotated PDF, grading workspace, anonymous/manual-posting/moderated grading, assignment gradebook sync; fixed public submission uploads, missing enrolment and scope checks, empty rubrics and 0-point rubric grades. Open: live Gotenberg run | — |
| 2026-10-19 | Phase M4 completed (M4.1–M4.4): branded manifest, service worker with offline pages/files and sign-out wipe, Web Push (RFC 8291/8292 on .NET crypto, VAPID key file, per-type preferences, SSRF-safe endpoints), Playwright PWA spec. Open: install and push on real phones | — |
| 2026-10-19 | Phase M2 acceptance closed: quiz → gradebook sync (aggregated "Quiz" component, best attempt per quiz, scaled to weightage, pending essays post nothing; recalculated on submit / time-out / grade / regrade). Ownership rule via new `results.Source` (migration `PlanMPhase2QuizGradebookSync`): hand-entered cells never overwritten, teacher edits take a synced cell over, published rows locked. Gradebook draft-cell edit fix | — |
| 2026-10-19 | Phase M5 completed (M5.1–M5.5, M5.7): LTI 1.3 platform with RS256 key rotation, tool registry + deployments, OIDC launch (new tab / in-portal frame), Deep Linking 2.0, AGS (token, line items as assignments, scores → graded submissions → gradebook, results) and NRPS with privacy levels; course card, admin page, Playwright spec playing the tool. Fixed the token relay's empty body. Open: real vendor tools and 1EdTech certification (M5.6), Dynamic Registration | — |
| 2026-10-19 | Phase M6 completed (M6.1–M6.4): institution/program/course outcomes with own rating scales and mastery methods, CLO → PLO mapping, Excel import, alignment to assignments, rubric criteria and quiz/bank questions (Outcomes button), evidence from every grading path via the gradebook syncs, Learning Mastery Gradebook, program attainment (PDF/Excel), student mastery page. Legacy grade/rubric endpoints now refresh the gradebook | — |
| 2026-10-19 | Phase M7 completed (M7.1–M7.4): ordered module items (9 types, videos migrated), one completion requirement per item, progress evaluated from live data with sticky completion, prerequisites / unlock dates / sequential order with reasons, API gate on locked quizzes, submissions and tools, student course page + item pages + previous/next bar, teacher editor and progress report. Fixed every boolean toggle form in the portal (hidden bool rendered empty) | — |
| 2026-10-19 | Phase M8 completed (M8.1–M8.4): course late & missing policy (deduction per hour/day with floor, waiver, hourly missing grades), availability window and late cut-off, per-student/group dates, group sets (manual, random split, self sign-up), group hand-in and grading, group discussions, peer review (balanced allocation, rubric or score, anonymous, peer average in the workspace) | — |
| 2026-10-19 | Phase M9 completed (M9.1–M9.3): built-in similarity comparison (winnowed fingerprints, course/assignment/institution scope) and Copyleaks (encrypted key, webhook with one-time token, resilient gateway), DB-backed queue worker, text from typed text/txt/docx/pdf/html/rtf, workspace badges, passage reports, Check again, optional student score. Open: live Copyleaks scan (vendor account + public https API) | — |
| 2026-10-19 | Phase M10 completed (M10.1–M10.3): BigBlueButton (signed API, on-demand meetings, moderator/viewer links), Zoom (S2S OAuth, host link, participant minutes, recordings), Microsoft Teams (Graph meetings and attendance reports) and pasted links; timetable-week scheduling, notifications and reminders, iCalendar files, Live-now join banner on every page, attendance from joins/minutes without overwriting teachers' marks. Open: tries on real BBB/Zoom/Teams accounts | — |
| 2026-10-20 | Phase M11 completed (M11.1–M11.4): English/Urdu/Arabic portal (3,002 texts) with RTL layout, request language from query/user/cookie/institution default/browser, Gregorian and '.'-decimal cultures, API messages/validation/emails/PDFs in the caller's or recipient's language, Language & Translations page with Excel export/import overrides, CI missing-key tests. Open: native-speaker review | — |
| 2026-10-23 | Phase M12 completed (M12.1–M12.5): .NET MAUI app (ADR) for students, teachers and parents — PKCE SSO hand-off through the portal, one-call mobile read models, camera hand-in, attendance/grading/announcements/discussions, report cards, native push via FCM v1 with the Web Push preferences, version gate, self-hosted crash reports, white-label build script and store texts. Open: iOS build (Mac), real phones, live FCM | — |
| 2026-10-23 | Phase M13 completed (M13.1–M13.2): SCORM 1.2/2004 packages (safe zip import, manifest reading, scorm-again runtime in a session-less player relay on an optional separate content origin, resume/attempts/review/preview, best score to the gradebook via an assignment, ScormPackage module items, report) and a minimal xAPI 1.0.3 LRS (Basic credentials, validation, voiding, queries). Open: real authoring-tool packages, multi-SCO sequencing | — |
| 2026-10-24 | Phase M14 completed (M14.1–M14.3): axe-core scans of 142 portal pages (105 findings → 0: labels, names, contrast, link underlines, focus ring) + keyboard walk, Accessibility Conformance Report; exam-day k6 test with seed (500 students, 0 errors, p95 < 75 ms), shared object storage (local / Azure Blob / S3) for media and SCORM files, two missing foreign-key indexes, request limits applied (per token / user / address, 429 + Retry-After); personal API tokens (hashed, read-only option, expiry, revocation, API Tokens page), OpenAPI description and snapshot, developer guide. Fixed: OpenAPI document failed to generate; script translation helper loaded after page scripts (grading workspace empty since M11). Open: 5,000-user staging run, live Azure/S3 run, assistive-technology user testing | — |
| 2026-10-24 | Open items worked: SSO institution-ID and role attribute mapping with role check, SAML signed AuthnRequests and encrypted assertions (SP certificate); LTI Dynamic Registration 1.0; saLTIre checks against the public test site (launch, deep linking, token, NRPS, AGS — found and fixed NRPS/AGS paging); real Web Push round trip through FCM (Chrome) and Windows push (Edge); LTI certification readiness report. Still open: physical phones, vendor-account tools, 1EdTech certification (membership) | — |
| 2026-10-24 | M4.4 Android check without a device: Chrome emulating a Pixel 7 — installable (no Chrome installability errors), FCM push received, offline reload; iPhone Home Screen tags added to the sign-in page; phone procedure in the SuperAdmin guide. Physical-phone check still open | — |
| 2026-10-25 | SCORM 2004 sequencing (activity tree from the manifest, navigation requests, rules, attempt limits, randomization, rollup, global objectives) and SCORM 1.2 prerequisites; older packages read again at start-up; concurrency retry on the attempt; Rustici sequencing samples played as students; user manual v1.6. Tests: unit 658/658, integration 373/374 (1 long-standing skip), web-integration 100/100, contract 1/1, Plan M Playwright specs 41/41. | — |
| 2026-10-25 | Real IdP check with a local Keycloak (OIDC + SAML: ID and email linking, role refusal, signed requests, encrypted assertions); SCORM packages in parts (contents list, per-part data and roll-up) checked with Rustici's Golf Explained samples; user manual v1.5 (154 pages, 121 new screenshots, chapters on quizzes and assessment, learning outcomes, integrations). Found and fixed while preparing the manual: grading an essay in a quiz that draws bank questions failed (500); the notification inbox showed every date as 01 Jan 0001 and "Mark read" sent an empty id (portal DTO did not match the API); students had no way to reach Course Content (new **Course Content** menu and course chooser) | — |
