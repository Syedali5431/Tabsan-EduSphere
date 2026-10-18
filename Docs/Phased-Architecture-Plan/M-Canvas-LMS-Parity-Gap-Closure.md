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
| M1 | Single Sign-On (OIDC + SAML) | P1 | 3 dw | — | 🟨 M1.1–M1.5 ✅ (M1.6 left) |
| M2 | Quiz Engine Upgrade | P1 | 5–6 dw | — | ⬜ |
| M3 | SpeedGrader-style Grading Workspace | P1 | 5 dw | — | ⬜ |
| M4 | Progressive Web App (installable + push) | P1 | 1–2 dw | — | ⬜ |
| M5 | LTI 1.3 / LTI Advantage Platform | P1 | 6–8 dw | M1 (key mgmt pattern) | ⬜ |
| M6 | Learning Outcomes & Mastery | P2 | 4 dw | M2, M3 | ⬜ |
| M7 | Module Progress & Release Conditions | P2 | 2 dw | — | ⬜ |
| M8 | Assignment Enhancements (late policy, overrides, groups, peer review) | P2 | 3 dw | M3 | ⬜ |
| M9 | Plagiarism Detection (direct API) | P2 (optional if M5 done) | 2 dw | M3 | ⬜ |
| M10 | Live Classes / Web Conferencing | P3 | 1 dw | — (or via M5) | ⬜ |
| M11 | Localisation (multi-language + RTL) | P3 | 3–4 dw | — | ⬜ |
| M12 | Native Mobile Apps | P3 | 10–12 dw | M4, M1 | ⬜ |
| M13 | SCORM / xAPI Packages | P4 (on demand) | 4 dw | M7 | ⬜ |
| M14 | Accessibility (WCAG 2.1 AA) & Scale Hardening | Ongoing | 2–3 dw | all UI phases | ⬜ |

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
- [ ] *Deferred:* role-hint and student/staff-ID attribute mapping — only useful with user provisioning, which is out of scope until whitelist provisioning is designed. Signed AuthnRequests (needs an SP certificate) and encrypted assertions — add if an institution's IdP requires them.

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

### Stage M1.6 - Tests & docs
- [ ] Unit: linking rules, enforce-SSO policy. Integration: mocked OIDC handler round-trip. Playwright: button visibility per tenant.
- [ ] Admin guide: Entra ID app registration + Google OAuth client steps.

Deliverables:
- SSO login via Microsoft / Google / SAML per tenant, enforce-SSO option, admin config page.

Acceptance:
- A user of Tenant A can log in with Entra ID and lands on their correct role dashboard; Tenant B's config is not visible/usable from Tenant A.

---

## Phase M2 - Quiz Engine Upgrade
Reason to do second:
- Highest-frequency teacher-facing gap; current answer model cannot store multi-select answers.

### Stage M2.1 - Answer model fix (prerequisite)
- [ ] New child table `QuizAnswerSelection` (QuizAnswerId, QuizOptionId) to support multi-select; keep `SelectedOptionId` for backward compatibility and migrate existing data into the new table.
- [ ] Add `ResponseJson` (nullable) to `QuizAnswer` for structured answers (matching pairs, ordering, numeric, fill-blanks).
- [ ] Migration + data backfill script; regression tests on existing quiz flows.

### Stage M2.2 - Question banks
- [ ] Entities `QuestionBank` (Owner: course / department / tenant, Title, IsShared) and `BankQuestion` (same shape as QuizQuestion + options + `Tags`, `Difficulty`).
- [ ] `QuizQuestionGroup` (QuizId, BankId, PickCount, MarksPerQuestion) — "pick N random from bank X".
- [ ] Import/export banks (Excel via ClosedXML; QTI 2.1 import as stretch goal).
- [ ] UI: bank manager page, add-from-bank in quiz builder.

### Stage M2.3 - New question types + grader strategy
- [ ] Extend `QuestionType`: MultipleAnswers, Matching, Ordering, FillInBlank (multiple blanks), Numeric (exact / range / tolerance), Formula (variables + generated values), Essay (rich text), FileUpload.
- [ ] `IQuestionGrader` interface + one implementation per type, resolved by type (strategy pattern). Partial credit supported where sensible.
- [ ] Builder UI per question type; student renderers per type.

### Stage M2.4 - Randomisation & attempt snapshot
- [ ] Per-quiz flags: ShuffleQuestions, ShuffleOptions.
- [ ] Store the presented order and drawn bank questions on the attempt (`QuizAttemptItem`: AttemptId, QuestionId, Order, OptionOrderJson, GeneratedValuesJson) so review/regrade shows exactly what the student saw.

### Stage M2.5 - Accommodations & availability overrides
- [ ] `QuizAccommodation` (QuizId nullable = all quizzes in offering, StudentProfileId, ExtraTimeMinutes / TimeMultiplier, ExtraAttempts).
- [ ] `QuizAvailabilityOverride` (per student or section: AvailableFrom/Until).
- [ ] Timer honours accommodations server-side (auto-submit on expiry server-enforced, not only client JS).

### Stage M2.6 - Quiz security & review options
- [ ] Access code, IP range allow-list, one-question-at-a-time, lock questions after answering.
- [ ] Review options: show score only / show correct answers / show after due date.
- [ ] Manual grading queue for Essay / FileUpload; regrade a question for all attempts.

### Stage M2.7 - Statistics, tests & docs
- [ ] Item analysis: difficulty index, discrimination, option distribution (Report Center).
- [ ] Unit tests per grader; integration tests for attempt lifecycle; Playwright for builder + take-quiz.

Deliverables:
- Banks, 11 question types, randomisation, accommodations, security options, item analysis.

Acceptance:
- A quiz drawing 10 random questions from a 50-question bank, with shuffled options and a student with 1.5× time, is taken, auto-graded, manually graded for essays and appears in gradebook.

---

## Phase M3 - SpeedGrader-style Grading Workspace
Reason to do third:
- Teachers grade weekly; inline annotation is a visible Canvas differentiator.

### Stage M3.1 - Submission history & comments
- [ ] `SubmissionAttempt` (SubmissionId, AttemptNumber, FileUrl(s), TextContent, SubmittedAt, IsLate) — every resubmission kept; current `AssignmentSubmission` points to latest.
- [ ] `SubmissionFile` (multiple files per attempt).
- [ ] `SubmissionComment` (SubmissionId, AttemptNumber, AuthorUserId, Body, MediaUrl for audio/video, IsPrivateToGraders, CreatedAt).
- [ ] Assignment settings: AllowedAttempts, AllowedFileTypes, MaxFileSizeMb.
- [ ] Migration + backfill existing submissions as attempt #1.

### Stage M3.2 - Document conversion pipeline
- [ ] Background job (BackgroundJobs project, RabbitMQ) converting DOCX/PPTX/XLSX/ODT → PDF using Gotenberg container (or headless LibreOffice).
- [ ] Store converted PDF alongside original; status flag on `SubmissionFile` (Pending / Ready / Failed).
- [ ] Add Gotenberg service to `docker-compose.yml`.

### Stage M3.3 - Inline annotation
- [ ] PDF.js viewer in grading page; annotation layer (highlight, strike, free-text, pin comment, freehand).
- [ ] `SubmissionAnnotation` (SubmissionFileId, Page, Type, GeometryJson, Comment, AuthorUserId).
- [ ] Student view shows annotations once grade is released; "Download annotated PDF" via QuestPDF/PDF overlay.
- [ ] Image submissions: annotate on image canvas.

### Stage M3.4 - Grading workspace UI
- [ ] New page `GradingWorkspace.cshtml`: student list (filter: ungraded / late / all) · document viewer · right panel (rubric, mark, comments, attempt switcher).
- [ ] Keyboard navigation next/previous student; autosave of marks/comments.
- [ ] Rubric scoring inline (reuse `RubricStudentGrade`).

### Stage M3.5 - Grading policies
- [ ] Anonymous grading (hide names until release).
- [ ] Grade posting policy: Automatic vs Manual ("post grades" button) — students see marks only after posting.
- [ ] Moderated grading (stretch): multiple graders + moderator selects final grade.

### Stage M3.6 - Tests & docs
- [ ] Integration tests for attempts/comments/annotations; Playwright for workspace navigation and posting.

Deliverables:
- Full grading workspace with attempt history, annotations, media comments, anonymous grading and posting policy.

Acceptance:
- Teacher grades 30 DOCX submissions in one screen without leaving the page; student sees annotated PDF only after grades are posted.

---

## Phase M4 - Progressive Web App (installable + push)
Reason to do fourth:
- One-to-two-week effort that lets sales say "mobile app"; foundation for M12.

### Stage M4.1 - Installability
- [ ] `manifest.webmanifest` (name, icons from tenant branding, theme colour, start_url, display: standalone).
- [ ] Per-tenant manifest endpoint so each institution's install shows its own logo/name.

### Stage M4.2 - Service worker & offline
- [ ] Service worker (Workbox via CDN or hand-written): cache shell, CSS/JS, fonts.
- [ ] Offline read-only cache for timetable, announcements, downloaded course materials.
- [ ] Offline fallback page.

### Stage M4.3 - Web Push notifications
- [ ] VAPID keys (store like signing keys, outside repo); `PushSubscription` table (UserId, Endpoint, Keys, Device).
- [ ] New `WebPushDeliveryProvider` alongside SMTP/Twilio in `Infrastructure/Integrations`; user preference per notification type.

### Stage M4.4 - Tests
- [ ] Lighthouse PWA checks in `.lighthouserc.yml`; manual install test on Android Chrome + iOS Safari.

Deliverables:
- Installable branded app with offline timetable/materials and push notifications.

---

## Phase M5 - LTI 1.3 / LTI Advantage Platform
Reason to do fifth:
- Single integration that unlocks Turnitin, Zoom, H5P, publisher content and hundreds of tools; required by most universities.

Role: **EduSphere = LTI Platform**, external products = LTI Tools.

### Stage M5.1 - Platform keys & registration
- [ ] Platform RSA key pair with rotation (store like license signing keys, not in repo); public `GET /.well-known/jwks.json` (per tenant or global).
- [ ] Entities `LtiTool` (Name, ClientId, DeploymentId, OidcInitiationUrl, TargetLinkUri, ToolJwksUrl, RedirectUris, Scopes, CustomParams, PrivacyLevel), `LtiToolDeployment` (TenantId / Department / Course scope).
- [ ] Admin page `LtiTools.cshtml`: register tool manually; show platform details (issuer, auth endpoint, token endpoint, JWKS) for the tool vendor.
- [ ] Dynamic Registration (LTI DR spec) — stretch.

### Stage M5.2 - Core launch (Resource Link)
- [ ] OIDC third-party initiated login flow: `/lti/login-init` → tool → `/lti/authorize` → signed `id_token` POST to tool.
- [ ] Claims: message_type, version 1.3.0, deployment_id, target_link_uri, roles (map EduSphere roles to LIS roles), context (course offering), resource_link, launch_presentation, custom.
- [ ] Nonce/state store (Redis) to prevent replay.
- [ ] Placement: new content item type "External Tool" in course modules; launch in iframe or new window.

### Stage M5.3 - Deep Linking 2.0
- [ ] `LtiDeepLinkingRequest` launch from module builder / assignment editor.
- [ ] Receive `LtiDeepLinkingResponse`, validate tool signature (fetch tool JWKS), create module items / assignments from returned content items.

### Stage M5.4 - Assignment & Grade Services (AGS)
- [ ] OAuth2 client-credentials token endpoint (`/lti/token`, JWT client assertion).
- [ ] Line items API (create/read/update/delete) mapped to EduSphere assignments / gradebook columns.
- [ ] Scores API (tool posts scores) → writes into gradebook with source = tool; Results API (read).

### Stage M5.5 - Names & Role Provisioning Services (NRPS)
- [ ] Membership endpoint for a course offering (respecting tool privacy level: name/email shared or anonymised).

### Stage M5.6 - Tool integrations verification
- [ ] Verify with IMS reference tool (saLTIre / LTI RI) and at least: H5P, Zoom LTI, Turnitin LTI (or Copyleaks LTI).
- [ ] Pursue **1EdTech (IMS) LTI Advantage certification** (Platform).

### Stage M5.7 - Tests & docs
- [ ] Unit: JWT build/validate, role mapping. Integration: full launch with test tool. Security review (signature validation, replay, iframe CSP).
- [ ] Admin guide: "How to add an LTI tool".

Deliverables:
- LTI 1.3 Core + Deep Linking + AGS + NRPS, admin tool registry, certified platform.

Acceptance:
- Teacher adds an H5P activity via Deep Linking; student completes it; score appears in EduSphere gradebook automatically.

---

## Phase M6 - Learning Outcomes & Mastery
Reason:
- Required for accreditation reporting; complements the existing Accreditation module.

### Stage M6.1 - Outcome model
- [ ] `Outcome` (TenantId, ParentId, Code, Title, Description, Level: Institution / Program / Course, MasteryThreshold, CalculationMethod [Highest, Latest, DecayingAverage, NMastery]).
- [ ] `OutcomeRatingScale` (levels + points).
- [ ] Import outcomes from Excel; link Program outcomes ↔ Course outcomes (`OutcomeMapping`).

### Stage M6.2 - Alignment
- [ ] `OutcomeAlignment` (OutcomeId, TargetType [RubricCriterion, QuizQuestion, BankQuestion, Assignment], TargetId).
- [ ] UI: align in rubric editor, quiz builder and question bank.

### Stage M6.3 - Mastery calculation
- [ ] `StudentOutcomeResult` (StudentProfileId, OutcomeId, SourceType, SourceId, Score, AssessedAt).
- [ ] Event handlers on grading (assignment graded, quiz finished, LTI score received) write results; calculate mastery per calculation method.

### Stage M6.4 - Reports
- [ ] Learning Mastery Gradebook (student × outcome grid).
- [ ] Program-level outcome attainment report (for accreditation bodies: PEC/HEC/NCAAA/ABET-style), PDF/Excel export via QuestPDF/ClosedXML.
- [ ] Student view: own mastery per outcome.

Deliverables:
- Outcome hierarchy, alignments, mastery gradebook and accreditation reports.

---

## Phase M7 - Module Progress & Release Conditions
### Stage M7.1 - Module items
- [ ] `ModuleItem` (ModuleId, ItemType [Page, Material, Video, Assignment, Quiz, Discussion, ExternalTool, ExternalUrl, Header], ItemId, Order, IndentLevel) — modules become ordered containers of items (existing Body/Videos remain as Page/Video items via migration).

### Stage M7.2 - Completion requirements & progress
- [ ] `ModuleItemRequirement` (ItemId, Type [MustView, MustSubmit, MustScoreAtLeast, MustMarkDone, MustContribute], MinScore).
- [ ] `StudentModuleProgress` (StudentProfileId, ItemId, Status, CompletedAt) updated by events.

### Stage M7.3 - Release conditions
- [ ] Module prerequisites (`ModulePrerequisite`: module B locked until module A complete).
- [ ] Sequential progression option; unlock date per module.
- [ ] Locked items visible but not openable, with reason shown.

### Stage M7.4 - UI
- [ ] Student: progress bars per module and course; "Next" navigation.
- [ ] Teacher: progress report per student.

Deliverables:
- Item-level module structure, completion tracking, prerequisites and sequential release.

---

## Phase M8 - Assignment Enhancements
### Stage M8.1 - Late & missing policy
- [ ] `LatePolicy` per course offering (DeductionPercentPerInterval, Interval [Hour/Day], LowestGradePercent, MissingSubmissionGradePercent).
- [ ] Auto-apply at grade calculation; show original vs adjusted mark; teacher override with audit.
- [ ] Assignment option: AvailableUntil (hard cut-off).

### Stage M8.2 - Due-date overrides ("Assign To")
- [ ] `AssignmentOverride` (AssignmentId, Target [Student / Section / Group], DueDate, AvailableFrom, AvailableUntil).
- [ ] Apply to calendar, notifications and late calculation.

### Stage M8.3 - Student groups
- [ ] `GroupSet` (OfferingId, Name, SelfSignup, MaxSize) and `StudentGroup` / `StudentGroupMember`.
- [ ] Teacher: create manually / random split / allow self-signup.
- [ ] Group assignments: one submission per group; grade individually or as group.
- [ ] Group discussion threads (reuse `DiscussionThread` with GroupId).

### Stage M8.4 - Peer review
- [ ] Assignment option: PeerReviewsCount, ReviewDueDate, Anonymous.
- [ ] `PeerReviewAssignment` (ReviewerStudentId, SubmissionId, Status) — automatic random allocation.
- [ ] Reviewer uses rubric + comments; optional contribution to grade.

Deliverables:
- Late/missing policies, per-student/section dates, groups, group assignments, peer review.

---

## Phase M9 - Plagiarism Detection (Direct API)
Reason:
- Optional if M5 delivers Turnitin/Copyleaks via LTI; useful earlier for markets where institutions want no extra vendor setup.

### Stage M9.1 - Provider abstraction
- [ ] `ISimilarityCheckProvider` + implementation for Copyleaks API (and/or Turnitin Core API — requires partner agreement).
- [ ] Per-tenant API credentials (encrypted), per-assignment toggle.

### Stage M9.2 - Pipeline
- [ ] On submission → queue job (RabbitMQ) → submit to provider → webhook callback → store `SimilarityReport` (SubmissionFileId, Score, ReportUrl, Status).
- [ ] Use existing `ResilientOutboundIntegrationGateway` for retries.

### Stage M9.3 - UI
- [ ] Similarity badge in grading workspace (M3) with link to full report; optional student visibility.

---

## Phase M10 - Live Classes / Web Conferencing
### Stage M10.1 - Provider abstraction
- [ ] `IConferenceProvider` with BigBlueButton (self-hosted, free) first; Zoom (Server-to-Server OAuth) and Microsoft Teams (Graph) as next options.

### Stage M10.2 - Scheduling
- [ ] Create meeting from Timetable entry or ad-hoc from course; `LiveSession` (OfferingId, Provider, JoinUrl, StartsAt, RecordingUrl).
- [ ] Calendar + notification integration; join button on student dashboard.

### Stage M10.3 - Attendance link (EduSphere advantage)
- [ ] Optionally auto-mark attendance from conference participant report (feeds existing Attendance module).

---

## Phase M11 - Localisation (Multi-language + RTL)
### Stage M11.1 - Infrastructure
- [ ] `AddLocalization`, `RequestLocalizationMiddleware`, culture from user setting → tenant default → browser.
- [ ] Shared `.resx` resources in Web and API (validation + email templates).

### Stage M11.2 - String extraction
- [ ] Move hard-coded strings from all `Views/Portal/*.cshtml`, JS messages and email templates into resources (largest task — split per portal area: Admin, Faculty, Student, Parent).
- [ ] `LabelService` vocabulary layered on top of localisation.

### Stage M11.3 - First languages + RTL
- [ ] English (default), Urdu, Arabic. RTL stylesheet (`dir="rtl"`, logical CSS properties).
- [ ] Localised dates/numbers; PDF reports (QuestPDF) with RTL fonts.

### Stage M11.4 - Translation workflow
- [ ] Export/import resource files to Excel for translators; missing-key report in CI.

---

## Phase M12 - Native Mobile Apps
Reason:
- After PWA (M4) proves demand; needs SSO (M1) for institutional login.

### Stage M12.1 - Technology decision
- [ ] Choose .NET MAUI (same language/team) vs Flutter (better UI performance). Record decision.
- [ ] API readiness review: pagination, mobile-friendly DTOs, refresh tokens, push (FCM/APNs).

### Stage M12.2 - Student app MVP
- [ ] Login (password + 2FA + SSO via system browser / PKCE), dashboard, timetable, courses & materials, assignments submit (camera/file), quizzes, grades/results, attendance, announcements, notifications.

### Stage M12.3 - Teacher app
- [ ] Take attendance, grade submissions (simplified), announcements, messaging.

### Stage M12.4 - Parent app (EduSphere advantage)
- [ ] Child results, attendance, fee status, report cards, notifications.

### Stage M12.5 - Release
- [ ] Per-institution white-label build option (premium licence); store listings; crash reporting; app versioning policy.

---

## Phase M13 - SCORM / xAPI Packages (on demand)
### Stage M13.1 - SCORM 1.2 / 2004 player
- [ ] Upload zip → validate `imsmanifest.xml` → extract to storage; module item type `ScormPackage`.
- [ ] Player iframe using scorm-again JS runtime; persist CMI data (`ScormAttempt`: completion, score, suspend_data).
- [ ] Score → gradebook; completion → module progress (M7).

### Stage M13.2 - xAPI (optional)
- [ ] Minimal LRS endpoint (statements store) or integration with external LRS.

---

## Phase M14 - Accessibility (WCAG 2.1 AA) & Scale Hardening
### Stage M14.1 - Accessibility audit
- [ ] axe-core in Playwright tests for all portal pages; fix contrast, labels, focus order, keyboard traps, ARIA on custom widgets (quiz builder, grading workspace, annotation tools).
- [ ] Publish VPAT / Accessibility Conformance Report.

### Stage M14.2 - Performance & scale
- [ ] Load test (tests/load) for 5,000 concurrent users on exam-day scenario (quiz start/submit spike) and results publication.
- [ ] Redis caching for hot reads; DB index review for new tables; file storage abstraction (local / Azure Blob / S3) for submissions and conversions.

### Stage M14.3 - Public API & developer docs
- [ ] Versioned public REST API documentation (Swagger already present) + API tokens per integration user — matches Canvas's open API story.

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
