<!-- markdownlint-disable MD060 -->

# Function List — Tabsan EduSphere

> Auto-maintained registry of all implemented functions, services, and API endpoints. Clean index — no summaries.

| Function Name | Purpose | Location |
|--------------|--------|----------|
| SamlServiceProviderCertificate | Plan M1: SAML SP certificate (sign AuthnRequests, decrypt assertions; created on first use) | src/Tabsan.EduSphere.Infrastructure/Auth/SamlServiceProviderCertificate.cs |
| SsoProviderSetting.SetAttributeMapping / MapRoles / SetSamlSecurity | Plan M1: institution-ID and role mapping, SAML signing/encryption options | src/Tabsan.EduSphere.Domain/Identity/SsoProviderSetting.cs |
| LtiDynamicRegistrationService | Plan M5.1: LTI Dynamic Registration — start (token + URL), OpenID configuration, tool registration | src/Tabsan.EduSphere.Application/Lti/LtiDynamicRegistration.cs |
| LtiAgsNrpsService.Page | Plan M5: AGS/NRPS paging with rel="next" links | src/Tabsan.EduSphere.Application/Lti/LtiServiceEndpoints.cs |
| PortalController.StartLtiRegistration | Plan M5.1: registration frame page | src/Tabsan.EduSphere.Web/Controllers/PortalController.Lti.cs |
| IObjectStore / FileSystemObjectStore / AzureBlobObjectStore / S3ObjectStore | Plan M14.2: object storage (local folder, Azure Blob, S3/S3-compatible) with metadata and signed read links | src/Tabsan.EduSphere.Infrastructure/Storage/ObjectStores.cs |
| ObjectStoreMediaStorageService | Plan M14.2: media storage (submissions, conversions, uploads) in the shared object store | src/Tabsan.EduSphere.API/Services/ObjectStoreMediaStorageService.cs |
| ObjectStoreScormContentStore | Plan M14.2: SCORM package files in the shared object store | src/Tabsan.EduSphere.Infrastructure/Lms/ObjectStoreScormContentStore.cs |
| ApiRateLimiting | Plan M14.2: request limits per API token / user / address, 429 with Retry-After | src/Tabsan.EduSphere.API/Services/ApiRateLimiting.cs |
| ApiTokenService | Plan M14.3: personal access tokens — create, list, revoke, authenticate (hashed, cached, expiry, per-user cap) | src/Tabsan.EduSphere.Application/Auth/ApiTokenService.cs |
| ApiTokenAuthenticationHandler / ApiTokenGuardMiddleware | Plan M14.3: `Bearer esk_…` sign-in as the token's owner; read-only tokens only read; tokens cannot manage tokens | src/Tabsan.EduSphere.API/Services/ApiTokenAuthentication.cs |
| ApiTokensController | Plan M14.3: api/v1/api-tokens — mine, create, revoke, all (Admin) | src/Tabsan.EduSphere.API/Controllers/ApiTokensController.cs |
| PortalController (ApiTokens, CreateApiToken, RevokeApiToken) | Plan M14.3: API Tokens page | src/Tabsan.EduSphere.Web/Controllers/PortalController.ApiTokens.cs |
| ScormService | Plan M13: package import, launch/resume/review/attempts, play data, commits, best score → gradebook, report, preview | src/Tabsan.EduSphere.Application/Lms/Scorm/ScormService.cs |
| ScormPackageReader | Plan M13: zip safety checks and imsmanifest.xml (SCORM 1.2/2004) reading | src/Tabsan.EduSphere.Application/Lms/Scorm/ScormPackageReader.cs |
| ScormCmi | Plan M13: SCORM data model on the server (commit values, mastery, time, start data for resume/review) | src/Tabsan.EduSphere.Application/Lms/Scorm/ScormCmi.cs |
| XapiService | Plan M13.2: LRS credentials, statement validation, storage, voiding and queries | src/Tabsan.EduSphere.Application/Lms/Scorm/XapiService.cs |
| ScormController | Plan M13: api/v1/scorm — packages (staff), my-packages/mine/launch (students), play/{token} shell data, content, commit | src/Tabsan.EduSphere.API/Controllers/ScormController.cs |
| XapiController | Plan M13.2: api/v1/xapi — about, statements (Basic credentials), credentials and recent (Admin) | src/Tabsan.EduSphere.API/Controllers/XapiController.cs |
| ScormPlayController | Plan M13: portal relay for the player shell, package files and commits under /scorm/play/{token}/ | src/Tabsan.EduSphere.Web/Controllers/ScormPlayController.cs |
| PortalController (Scorm, ScormPlay, ScormReport, XapiSettings …) | Plan M13: package page, launch, player page, report, teacher actions, LRS page | src/Tabsan.EduSphere.Web/Controllers/PortalController.Scorm.cs |
| MobileController | Plan M12: api/v1/mobile — config (public), handoff + redeem (public), home, timetable, courses, course, results, attendance, report-cards, children, devices, crash-reports | src/Tabsan.EduSphere.API/Controllers/MobileController.cs |
| MobileService | Plan M12: app config, PKCE single-use SSO hand-off, device registry, crash reports, native push with preferences | src/Tabsan.EduSphere.Application/Mobile/MobileService.cs |
| MobileQueries | Plan M12: one-call read models for phones by role (student, teacher, parent) with access checks | src/Tabsan.EduSphere.Infrastructure/Mobile/MobileQueries.cs |
| ReportCardLines | Plan M12: report-card details (free-form JSON) as readable label/value lines | src/Tabsan.EduSphere.Application/Mobile/ReportCardLines.cs |
| FcmPushSender | Plan M12: Firebase Cloud Messaging HTTP v1 (service-account JWT, cached token, dead-token detection) | src/Tabsan.EduSphere.Infrastructure/Mobile/FcmPushSender.cs |
| LoginController.Sso (appChallenge) / HandOffToAppAsync | Plan M12: portal side of app SSO — one-time code to {scheme}://sso, portal session ended | src/Tabsan.EduSphere.Web/Controllers/LoginController.cs |
| EduSphereClient / view models | Plan M12: the app's API client (sign-in, SSO, refresh, uploads) and screen logic | src/Tabsan.EduSphere.Mobile.Core/ |
| build-whitelabel.ps1 | Plan M12: institution store build (.aab/.apk/.ipa) | mobile/build-whitelabel.ps1 |
| PortalText / TranslationStore | Plan M11: view texts by English key (`T[...]`, `T.Format`, `T.Text`, `T.Js`), shipped translations + overrides, "{0}" patterns | src/Tabsan.EduSphere.Web/Localization/PortalText.cs |
| PortalCultures / PortalLanguageResolver / PortalRequestCultureProvider | Plan M11: request language (query → user → cookie → institution default → browser) and Gregorian, "."-decimal cultures | src/Tabsan.EduSphere.Web/Localization/PortalCulture.cs |
| TranslationWorkbook | Plan M11: coverage, Excel export and import of translations (changed texts kept as overrides) | src/Tabsan.EduSphere.Web/Localization/TranslationWorkbook.cs |
| LanguageController.Set / PortalController.LanguageSettings | Plan M11: language switcher; Language & Translations page (default, export, import) | src/Tabsan.EduSphere.Web/Controllers/ |
| PortalLanguages / TextLocalizer / PortalLanguageService | Plan M11: languages, API text catalogue with patterns, institution default and recipient language | src/Tabsan.EduSphere.Application/Localization/Localization.cs |
| LocalizationController | Plan M11: /api/v1/localization (public default + languages), /default (SuperAdmin), /me | src/Tabsan.EduSphere.API/Controllers/LocalizationController.cs |
| RequestLanguageMiddleware / LocalizedResponseFilter | Plan M11: Accept-Language → UI culture and Content-Language; translated messages, problem details and validation errors | src/Tabsan.EduSphere.API/Middleware/RequestLanguage.cs |
| PdfLanguage | Plan M11: right-to-left QuestPDF pages, Arabic-script fonts, translated report labels | src/Tabsan.EduSphere.Infrastructure/Reporting/PdfLanguage.cs |
| LiveClassService | Plan M10: settings/test, schedule (ad hoc, timetable weeks), update/cancel, upcoming, join, calendar, attendance report/marking, reminders, recordings | src/Tabsan.EduSphere.Application/LiveClasses/LiveClassService.cs |
| LiveClassService.NextOccurrences | Plan M10: next weekly starts of a timetable slot (UTC) | src/Tabsan.EduSphere.Application/LiveClasses/LiveClassService.cs |
| LiveCalendar.Event | Plan M10: iCalendar event with alarm for a live class | src/Tabsan.EduSphere.Application/LiveClasses/LiveClassContracts.cs |
| BigBlueButtonProvider / ZoomProvider / TeamsProvider | Plan M10: conferencing providers (signed BBB API, Zoom S2S OAuth, Microsoft Graph) | src/Tabsan.EduSphere.Infrastructure/LiveClasses/ConferenceProviders.cs |
| LiveClassRepository | Plan M10: settings, sessions, joins, timetable slots, attendance marks | src/Tabsan.EduSphere.Infrastructure/Repositories/LiveClassRepository.cs |
| LiveClassesController / LiveClassWorker | Plan M10: /api/v1/live endpoints and the minute worker | src/Tabsan.EduSphere.API/ |
| PortalController.LiveClasses / UpcomingLiveClassesViewComponent | Plan M10: live class pages, join, calendar, attendance, settings, banner and card | src/Tabsan.EduSphere.Web/ |
| SimilarityService | Plan M9: settings, assignment option, queue, built-in comparison, Copyleaks submission/webhook, scores, reports, student score | src/Tabsan.EduSphere.Application/Similarity/SimilarityService.cs |
| Fingerprinter / SimilarityScorer | Plan M9: words, winnowed fingerprints, coverage score and matching passages | src/Tabsan.EduSphere.Application/Similarity/Fingerprinter.cs |
| CopyleaksWebhook.Parse | Plan M9: Copyleaks v3 status webhook (completed / error) | src/Tabsan.EduSphere.Application/Similarity/SimilarityContracts.cs |
| SubmissionTextExtractor | Plan M9: text from txt/html/rtf/docx/pdf submissions | src/Tabsan.EduSphere.Infrastructure/Similarity/SubmissionTextExtractor.cs |
| CopyleaksClient | Plan M9: Copyleaks login (cached) and scan submission | src/Tabsan.EduSphere.Infrastructure/Similarity/CopyleaksClient.cs |
| SimilarityRepository | Plan M9: settings, checks queue, documents, fingerprint matches by scope | src/Tabsan.EduSphere.Infrastructure/Repositories/SimilarityRepository.cs |
| SimilarityController / SimilarityWorker | Plan M9: /api/v1/similarity endpoints and the background queue | src/Tabsan.EduSphere.API/ |
| PortalController.Similarity | Plan M9: settings page, report page, assignment option, workspace recheck, student score | src/Tabsan.EduSphere.Web/Controllers/PortalController.Similarity.cs |
| AssignmentPolicyService | Plan M8: late policy, deductions, availability/options, Assign-to overrides, missing grades, waivers, student extras, own due dates | src/Tabsan.EduSphere.Application/Assignments/AssignmentPolicyService.cs |
| GroupService | Plan M8: group sets, groups, members, random split, roster, student join/leave | src/Tabsan.EduSphere.Application/Assignments/GroupService.cs |
| PeerReviewService / PeerReviewAllocator.Allocate | Plan M8: balanced peer-review allocation, reviewer task, rubric-scaled scores, overview | src/Tabsan.EduSphere.Application/Assignments/PeerReviewService.cs |
| LatePolicy.PenaltyFor | Plan M8: deduction for a late hand-in (started intervals, floor) | src/Tabsan.EduSphere.Domain/Assignments/AssignmentEnhancements.cs |
| AssignmentPolicyRepository | Plan M8: late policies, overrides, groups, memberships, peer reviews | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentPolicyRepository.cs |
| AssignmentPolicyController | Plan M8: /api/v1/assignment late-policy, options, overrides, groups, peer reviews, my-extras | src/Tabsan.EduSphere.API/Controllers/AssignmentPolicyController.cs |
| AssignmentPolicyWorker | Plan M8: hourly missing grades and peer-review allocation after due dates | src/Tabsan.EduSphere.API/Services/AssignmentPolicyWorker.cs |
| PortalController.Policies | Plan M8: Late policy & groups, Assignment options, My groups, Peer review pages | src/Tabsan.EduSphere.Web/Controllers/PortalController.Policies.cs |
| ModuleItemService | Plan M7: module items, release settings, student view, open/done/nav, progress report, lock reasons | src/Tabsan.EduSphere.Application/Lms/ModuleItemService.cs |
| ModuleProgressEvaluator | Plan M7: requirements, prerequisites, unlock dates, sequential locks, completion | src/Tabsan.EduSphere.Application/Lms/ModuleProgressEvaluator.cs |
| ModuleItemRepository | Plan M7: items, progress, prerequisites and evidence from submissions, quizzes, discussions, LTI | src/Tabsan.EduSphere.Infrastructure/Repositories/ModuleItemRepository.cs |
| ModuleItemsController | Plan M7: /api/v1/lms structure, items, release, my-modules, open, done, nav, progress | src/Tabsan.EduSphere.API/Controllers/ModuleItemsController.cs |
| ModuleGate | Plan M7: refuses locked quizzes, submissions and tool launches | src/Tabsan.EduSphere.API/Services/ModuleGate.cs |
| PortalController.Modules / ModuleNavViewComponent | Plan M7: item editor, item pages, previous/next bar, progress report | src/Tabsan.EduSphere.Web/ |
| OutcomeService | Plan M6: outcomes CRUD, rating scales, CLO→PLO mappings, Excel import, alignments | src/Tabsan.EduSphere.Application/Outcomes/OutcomeService.cs |
| OutcomeResultSync / OutcomeAware*GradebookSync | Plan M6: evidence from graded work; decorators on the assignment and quiz gradebook syncs | src/Tabsan.EduSphere.Application/Outcomes/OutcomeResultSync.cs |
| MasteryCalculator | Plan M6: Highest / Latest / Average / DecayingAverage / NMastery, rating and status | src/Tabsan.EduSphere.Application/Outcomes/MasteryCalculator.cs |
| OutcomeReportService | Plan M6: mastery gradebook, student mastery, program attainment | src/Tabsan.EduSphere.Application/Outcomes/OutcomeReportService.cs |
| OutcomeDocuments | Plan M6: import template/reader, mastery workbook, attainment workbook and PDF | src/Tabsan.EduSphere.Infrastructure/Outcomes/OutcomeDocuments.cs |
| OutcomesController | Plan M6: /api/v1/outcomes | src/Tabsan.EduSphere.API/Controllers/OutcomesController.cs |
| PortalController.Outcomes / outcome-align.js | Plan M6: outcome pages and the alignment widget | src/Tabsan.EduSphere.Web/ |
| LtiAdminService | Plan M5: tool registry, validation, deployments, key rotation, audit | src/Tabsan.EduSphere.Application/Lti/LtiAdminService.cs |
| LtiLaunchService | Plan M5: course items, login initiation, OIDC authorize → id_token claims, Deep Linking response | src/Tabsan.EduSphere.Application/Lti/LtiLaunchService.cs |
| LtiTokenService / LtiAgsNrpsService / LtiGradeBridge | Plan M5: client-credentials tokens, AGS line items/scores/results, NRPS memberships, scores → graded submissions | src/Tabsan.EduSphere.Application/Lti/LtiServiceEndpoints.cs |
| LtiPlatformKeyStore / LtiToolJwtValidator | Plan M5: RS256 platform keys with rotation and JWKS; validation of tool-signed JWTs | src/Tabsan.EduSphere.Infrastructure/Lti/LtiCrypto.cs |
| LtiAdminController / LtiCourseController / LtiProtocolController | Plan M5: /api/v1/lti registry, course items and launches, protocol and services | src/Tabsan.EduSphere.API/Controllers/LtiControllers.cs |
| LtiController (Web) | Plan M5: /lti launch, authorize, deep-link return and service relays on the portal address | src/Tabsan.EduSphere.Web/Controllers/LtiController.cs |
| ExternalToolsViewComponent / PortalController.LtiTools | Plan M5: course "External tools" card and admin tool registry page | src/Tabsan.EduSphere.Web/ |
| PwaController.Manifest | Plan M4: branded web app manifest | src/Tabsan.EduSphere.Web/Controllers/PwaController.cs |
| sw.js / pwa.js | Plan M4: service worker (shell, offline pages/files, push), install button, offline banner, sign-out wipe, device push card | src/Tabsan.EduSphere.Web/wwwroot/ |
| PushNotificationService | Plan M4: push subscriptions, preferences, delivery with SSRF-safe endpoint rules | src/Tabsan.EduSphere.Application/Notifications/PushNotificationService.cs |
| WebPushEncryption / VapidKeys / VapidKeyStore / WebPushSender | Plan M4: RFC 8291 encryption, RFC 8292 VAPID signing, key file, push service requests | src/Tabsan.EduSphere.Infrastructure/Notifications/WebPush.cs |
| PushController | Plan M4: /api/v1/push summary, subscriptions, preferences, test | src/Tabsan.EduSphere.API/Controllers/PushController.cs |
| PushDispatchQueue / PushDispatchWorker | Plan M4: queued push delivery for notifications sent by the API | src/Tabsan.EduSphere.API/Services/PushDispatchQueue.cs |
| SubmissionWorkspaceService | Plan M3: attempts, multi-file submit, comments, annotations, grading workspace, posting/anonymity/moderation | src/Tabsan.EduSphere.Application/Assignments/SubmissionWorkspaceService*.cs |
| AnnotationGeometry.Validate | Plan M3: per-type validation and re-serialisation of page-relative annotation geometry | src/Tabsan.EduSphere.Application/Assignments/AnnotationGeometry.cs |
| SubmissionConversionProcessor.ProcessBatchAsync | Plan M3: claims and converts queued office files to PDF (retries, stale claims) | src/Tabsan.EduSphere.Application/Assignments/SubmissionConversionProcessor.cs |
| AssignmentGradebookSync / AssignmentGradebookCalculator | Plan M3: posted assignment grades → "Assignment(s)" gradebook component | src/Tabsan.EduSphere.Application/Assignments/AssignmentGradebookSync.cs |
| GotenbergDocumentConverter | Plan M3: DOCX/PPTX/XLSX/ODT → PDF through Gotenberg | src/Tabsan.EduSphere.Infrastructure/Documents/GotenbergDocumentConverter.cs |
| AnnotatedPdfRenderer | Plan M3: draws annotations onto a PDF/image with PDFsharp and appends a comments page | src/Tabsan.EduSphere.Infrastructure/Documents/AnnotatedPdfRenderer.cs |
| SubmissionWorkspaceRepository | Plan M3: EF data access for attempts, files, comments, annotations, provisional grades, conversion queue | src/Tabsan.EduSphere.Infrastructure/Repositories/SubmissionWorkspaceRepository.cs |
| SubmissionWorkspaceController | Plan M3: /api/v1/assignment settings, submissions, files, comments, annotations, workspace, grade, rubric, moderate, post/hide grades, reveal names | src/Tabsan.EduSphere.API/Controllers/SubmissionWorkspaceController.cs |
| SubmissionConversionWorker | Plan M3: background worker polling the conversion queue | src/Tabsan.EduSphere.API/Services/SubmissionConversionWorker.cs |
| Rubric.AddCriterion / RubricCriterion.AddLevel | Plan M3 fix: rubric criteria and levels are attached and saved | src/Tabsan.EduSphere.Domain/Assignments/Rubric.cs |
| PortalController.GradingWorkspace / AssignmentSubmission | Plan M3: grading workspace and student submission pages, JSON and file relays | src/Tabsan.EduSphere.Web/Controllers/PortalController.Grading.cs |
| submission-viewer.js / grading-workspace.js / comment-recorder.js | Plan M3: PDF.js viewer with annotation layer, workspace navigation and autosave, voice comments | src/Tabsan.EduSphere.Web/wwwroot/js/ |
| QuizService (Delivery, Grading partials) | Plan M2: paper, autosave, submit, deadline, review, grading queue, regrade, statistics | src/Tabsan.EduSphere.Application/Quizzes/QuizService*.cs |
| QuestionGraders / QuestionAuthoring / FormulaEvaluator | Plan M2: grader per question type, config validation, safe formula evaluation | src/Tabsan.EduSphere.Application/Quizzes/Grading/ |
| QuestionBankService / QuestionBankSpreadsheet | Plan M2: question banks and Excel import/export | src/Tabsan.EduSphere.Application/Quizzes/QuestionBankService.cs |
| QuizGradebookSync | Plan M2: finished quiz scores → "Quiz(zes)" gradebook component | src/Tabsan.EduSphere.Application/Quizzes/QuizGradebookSync.cs |
| QuestionBankController | Plan M2: /api/v1/quiz/banks CRUD, questions, import/export | src/Tabsan.EduSphere.API/Controllers/QuestionBankController.cs |
| PortalController.QuizBuilder / TakeQuiz / QuizGrading / QuizStatistics | Plan M2: quiz builder, banks, take-quiz, review, grading and statistics pages | src/Tabsan.EduSphere.Web/Controllers/PortalController.Quizzes.cs |
| SsoSignInService / SsoSettingsService / SsoAccountServices | Plan M1: SSO sign-in, provider settings, linked accounts, enforced SSO | src/Tabsan.EduSphere.Application/Auth/ |
| OidcClient / SamlClient | Plan M1: OpenID Connect (PKCE) and SAML 2.0 protocol clients | src/Tabsan.EduSphere.Infrastructure/Auth/ |
| SsoController / SsoSettingsController / SsoLinksController | Plan M1: /api/v1/sso start, callback, ACS, metadata, settings, links | src/Tabsan.EduSphere.API/Controllers/ |
| QA Phases H-M All Modules Verified | Payments, Certificates, Study Plan, Degree Audit, Graduation, Reports, UI/UX | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QA Phase G Results Attendance Assignments Verified | EnterResults, EnterAttendance, BulkMarkAttendance, Assignments all operational | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QA Phase F Student Lifecycle Verified | Semester dropdown constrained to TotalSemesters, student CRUD pages | src/Tabsan.EduSphere.Web/Helpers/AcademicLevelRangeHelper.cs |
| QA Phases D+E Verified | Profile upload, sidebar count, Degree Rules no-redirect, ISO/Backup/Doc modules | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QA Phase C User Import Verified | CSV templates, single-user creation form, import via api/v1/user-import/csv | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QA Phase B Login & MFA Verified | TOTP enrollment, verification, login-verify all passing; login credential validation | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| QA Phase B Idle Timeout Verified | RefreshAsync rejects sessions past IdleTimeoutMinutes | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| QA Phase B Change-Password Verified | Current/new/confirm password form with safe-password policy | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QA Phase A Building Creation Verified | Building creation form with Tenant/Campus dropdowns, blank-name rejection | src/Tabsan.EduSphere.API/Controllers/BuildingController.cs |
| QA Phase A Hierarchy Verified | Departments → Programs → Courses & Offerings pages load in correct order | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| Phase 6 Final Validation | Full regression test (186/215 passed), endpoint audit (32 POST controllers), Issues 1-10 re-verified | Docs/App-Issue-Resolution-Plan.md |
| Phase 5 Study Plan Demo Seed | 5 demo study plans with mixed advisor statuses (Draft/Submitted/Approved) and 3-5 courses each | Scripts/03-FullDummyData.sql |
| Phase 5 Prerequisites Demo Seed | CS101→CS201→CS301→CS401→CS501 prerequisite chain for demo | Scripts/03-FullDummyData.sql |
| Phase 5 Post-Deploy Checks Expanded | Added study plan, prerequisite, and payment receipt count checks | Scripts/05-PostDeployment-Checks.sql |
| Phase 4 Demo Payment Receipts Seed | 15 demo payment receipts with mixed statuses across graduated and regular students | Scripts/03-FullDummyData.sql |
| Phase 4 Sidebar Role Visibility Fix | Aligned Admin, Faculty, Student, Finance role visibility with Sidebar-Menu-Purpose.csv | Scripts/07-Fix-Sidebar-Role-Visibility.sql |
| Phase 3 ChangePassword Flow | Verified change-password feature under User Settings with current/new/confirm validation | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| Phase 3 DegreeRules Rendering Fix | DegreeRules GET returns View instead of redirect; handles non-SuperAdmin and capability-denied gracefully | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| Phase 3 ISO/Backup/Document Module Registry | Added iso_compliance, backup_dr, document_management as SuperAdmin-only modules | src/Tabsan.EduSphere.Application/Modules/ModuleRegistry.cs |
| Phase 3 Sidebar CSV ISO/Backup/Document | Added ISO Compliance, Backup & DR, Document Management as SuperAdmin Settings entries | Docs/Sidebar-Menu-Purpose.csv |
| Phase 3 PRD Terminology Cleanup | Replaced all "subjects" references with "courses"; removed "Create User" references | Project startup Docs/PRD.md |
| Phase 0 Guardrails Tracking | Documentation-only phase tracker for guardrail enforcement and scope protection | Docs/App-Issue-Resolution-Plan.md |
| Phase 2 Academic Level Range Helper | Resolves the valid university academic-level range from configured levels and a program's total-semester count | src/Tabsan.EduSphere.Web/Helpers/AcademicLevelRangeHelper.cs |
| BuildingRoomService.CreateBuildingAsync | Validates building input and persists scoped building records safely | src/Tabsan.EduSphere.Application/Services/BuildingRoomService.cs |
| BuildingController.Create | Accepts scoped building create requests and enforces tenant/campus requirements | src/Tabsan.EduSphere.API/Controllers/BuildingController.cs |
| PortalController.CreateBuilding | Propagates tenant/campus values from the portal form to the API create request | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| ITotpService.GenerateSecret | Generate cryptographically secure Base32 TOTP secret via Otp.NET | src/Tabsan.EduSphere.Application/Interfaces/ITotpService.cs |
| ITotpService.BuildProvisioningUri | Build otpauth://totp/ URI for authenticator enrollment | src/Tabsan.EduSphere.Application/Interfaces/ITotpService.cs |
| ITotpService.ValidateCode | Validate TOTP code using OtpNet.Totp.VerifyTotp with VerificationWindow | src/Tabsan.EduSphere.Application/Interfaces/ITotpService.cs |
| TotpService | Production TOTP implementation using Otp.NET 1.4.1 (RFC 6238) | src/Tabsan.EduSphere.Infrastructure/Auth/TotpService.cs |
| TwoFactorController.Setup | GET/POST /api/v1/2fa/setup — begin 2FA enrollment | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Status | GET /api/v1/2fa/status — current 2FA state | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Verify | POST /api/v1/2fa/verify — confirm initial TOTP code | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Disable | POST /api/v1/2fa/disable — soft-disable 2FA | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Enable | POST /api/v1/2fa/enable — re-enable with existing secret | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.ResetSetup | POST /api/v1/2fa/reset-setup — clear pending setup | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.LoginVerify | POST /api/v1/2fa/login-verify — login hand-off verification | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorSetupService | Orchestrates 2FA setup, verify, disable, enable, reset, login-verify | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorService | Wraps ITotpService with deployment-default TOTP parameters | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs |
| QRCodeService | Generates PNG/base64 QR data URLs for authenticator enrollment | src/Tabsan.EduSphere.API/Services/TwoFactor/QRCodeService.cs |
| ITwoFactorStateStore | 2FA persistence boundary (get, save, enable, disable, hard delete) | src/Tabsan.EduSphere.Application/Interfaces/ITwoFactorStateStore.cs |
| TwoFactorStateStore | EF-backed 2FA state with Data Protection encryption | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorSettingsPageModel | Web model for 2FA setup UI (QR, manual key, verify/disable) | src/Tabsan.EduSphere.Web/Models/Portal/ApiConnectionModel.cs |
| PortalController.TwoFactorSettings | GET /Portal/TwoFactorSettings — 2FA management page | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.BeginTwoFactorSetup | POST — start 2FA enrollment (generates QR + secret) | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.VerifyTwoFactorSetup | POST — verify initial TOTP code to complete enrollment | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DisableTwoFactor | POST — disable 2FA with valid TOTP code | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| TwoFactorSettings.cshtml | 2FA UI page: QR display, manual key, verify/disable buttons | src/Tabsan.EduSphere.Web/Views/Portal/TwoFactorSettings.cshtml |
| IEncryptionService.Encrypt | AES-256-CBC encrypt plaintext → Base64 ciphertext | src/Tabsan.EduSphere.Application/Interfaces/IEncryptionService.cs |
| IEncryptionService.Decrypt | Decrypt Base64 ciphertext → plaintext | src/Tabsan.EduSphere.Application/Interfaces/IEncryptionService.cs |
| EncryptionService | AES-256-CBC implementation with PBKDF2 key derivation | src/Tabsan.EduSphere.Infrastructure/Security/EncryptionService.cs |
| IDataMaskingService.MaskEmail | Masks email as j***@domain.com | src/Tabsan.EduSphere.Application/Interfaces/IDataMaskingService.cs |
| IDataMaskingService.MaskPhone | Masks phone as ***1234 | src/Tabsan.EduSphere.Application/Interfaces/IDataMaskingService.cs |
| IDataMaskingService.MaskName | Masks last name as John D*** | src/Tabsan.EduSphere.Application/Interfaces/IDataMaskingService.cs |
| DataMaskingService | PII masking implementation for UI display | src/Tabsan.EduSphere.Infrastructure/Security/DataMaskingService.cs |
| DataClassificationEntry (entity) | Data classification entry (Public/Internal/Confidential/Restricted) | src/Tabsan.EduSphere.Domain/DataProtection/DataClassificationEntry.cs |
| IDataClassificationRepository | Repository contract for classification entries | src/Tabsan.EduSphere.Domain/Interfaces/IDataClassificationRepository.cs |
| DataClassificationRepository | EF Core implementation of classification persistence | src/Tabsan.EduSphere.Infrastructure/Repositories/DataClassificationRepository.cs |
| IDataClassificationService | Service contract for classification CRUD | src/Tabsan.EduSphere.Application/Interfaces/IDataClassificationService.cs |
| DataClassificationService | Classification management implementation | src/Tabsan.EduSphere.Infrastructure/DataProtection/DataClassificationService.cs |
| DataProtectionController | API: encrypt, decrypt, mask, classify endpoints | src/Tabsan.EduSphere.API/Controllers/DataProtectionController.cs |
| User.ConsentToMonitoring | GDPR monitoring consent flag (Phase 5) | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.DataRetentionDate | Data lifecycle retention date (Phase 5) | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.ProfilePicturePath | Relative path to profile picture (nvarchar 500, nullable) | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.UpdateProfilePicturePath | Updates the profile picture path on the user entity | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| PortalController.UploadProfilePicture | POST /Portal/UploadProfilePicture — validates and saves profile picture to wwwroot | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| SessionIdentity.ProfilePicturePath | Cached profile picture path in session identity for navbar display | src/Tabsan.EduSphere.Web/Models/Portal/ApiConnectionModel.cs |
| UpdateUserSettingsRequest.ProfilePicturePath | DTO field for updating profile picture path via settings API | src/Tabsan.EduSphere.Application/DTOs/Auth/UserSettingsDtos.cs |
| IComplianceDashboardService.GetDashboardAsync | Aggregates compliance posture across all ISO phases (7 sections) | src/Tabsan.EduSphere.Application/Interfaces/IComplianceDashboardService.cs |
| ComplianceDashboardService | EF-based aggregation from 8 tables for compliance dashboard | src/Tabsan.EduSphere.Infrastructure/Compliance/ComplianceDashboardService.cs |
| ComplianceDashboardController.GetDashboard | GET /api/v1/compliance/dashboard — full compliance overview | src/Tabsan.EduSphere.API/Controllers/ComplianceDashboardController.cs |
| IDataIntegrityService.RunIntegrityCheckAsync | Runs full data integrity check across 7 areas | src/Tabsan.EduSphere.Application/Interfaces/IDataIntegrityService.cs |
| DataIntegrityService | EF-based integrity verification (audit coverage, orphans, stale records) | src/Tabsan.EduSphere.Infrastructure/Integrity/DataIntegrityService.cs |
| DataIntegrityController.RunCheck | GET /api/v1/data-integrity/check — runs integrity check | src/Tabsan.EduSphere.API/Controllers/DataIntegrityController.cs |
| BackupVerificationLog (entity) | Backup integrity check / restore test record | src/Tabsan.EduSphere.Domain/Backup/BackupVerificationLog.cs |
| IBackupVerificationRepository | Repository contract for backup verifications | src/Tabsan.EduSphere.Domain/Interfaces/IBackupVerificationRepository.cs |
| BackupVerificationRepository | EF Core implementation | src/Tabsan.EduSphere.Infrastructure/Repositories/BackupVerificationRepository.cs |
| IBackupVerificationService | Verification query and creation service | src/Tabsan.EduSphere.Application/Interfaces/IBackupVerificationService.cs |
| BackupVerificationService | Backup verification implementation | src/Tabsan.EduSphere.Infrastructure/Backup/BackupVerificationService.cs |
| BackupVerificationController | API: 3 verification endpoints | src/Tabsan.EduSphere.API/Controllers/BackupVerificationController.cs |
| PolicyDocument (entity) | Versioned policy doc with Draft→Published→Archived lifecycle | src/Tabsan.EduSphere.Domain/Documents/PolicyDocument.cs |
| PolicyDocumentVersion (entity) | Immutable version history for policy documents | src/Tabsan.EduSphere.Domain/Documents/PolicyDocumentVersion.cs |
| IPolicyDocumentRepository | Repository contract for policy docs + versions | src/Tabsan.EduSphere.Domain/Interfaces/IPolicyDocumentRepository.cs |
| PolicyDocumentRepository | EF Core implementation | src/Tabsan.EduSphere.Infrastructure/Repositories/PolicyDocumentRepository.cs |
| IPolicyDocumentService | Policy doc CRUD + publish/archive + version tracking | src/Tabsan.EduSphere.Application/Interfaces/IPolicyDocumentService.cs |
| PolicyDocumentService | Full policy doc management with version history | src/Tabsan.EduSphere.Infrastructure/Documents/PolicyDocumentService.cs |
| PolicyDocumentController | API: 7 endpoints for policy docs (read all, write admin) | src/Tabsan.EduSphere.API/Controllers/PolicyDocumentController.cs |
| IncidentLog (entity) | Security incident with lifecycle: Open→Investigating→Resolved→Closed | src/Tabsan.EduSphere.Domain/Incidents/IncidentLog.cs |
| IIncidentRepository | Repository contract for incident management | src/Tabsan.EduSphere.Domain/Interfaces/IIncidentRepository.cs |
| IncidentRepository | EF Core implementation of incident persistence | src/Tabsan.EduSphere.Infrastructure/Repositories/IncidentRepository.cs |
| IIncidentService | Incident CRUD + status flow + summary | src/Tabsan.EduSphere.Application/Interfaces/IIncidentService.cs |
| IncidentService | Full incident management implementation | src/Tabsan.EduSphere.Infrastructure/Incidents/IncidentService.cs |
| IncidentController | API: GET/POST/PUT incidents + summary endpoint | src/Tabsan.EduSphere.API/Controllers/IncidentController.cs |
| IncidentLogConfiguration | EF Core config for incident_logs table + 3 indexes | src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/IncidentLogConfiguration.cs |
| BackupLog (entity) | Immutable backup operation record with lifecycle methods (MarkCompleted/Failed/Verified) | src/Tabsan.EduSphere.Domain/Backup/BackupLog.cs |
| IBackupLogRepository | Repository contract for backup operation logging | src/Tabsan.EduSphere.Domain/Interfaces/IBackupLogRepository.cs |
| BackupLogRepository | EF Core implementation of backup log persistence | src/Tabsan.EduSphere.Infrastructure/Repositories/BackupLogRepository.cs |
| IBackupService.GetLogsAsync | Paged query of backup history with type/status/date filters | src/Tabsan.EduSphere.Application/Interfaces/IBackupService.cs |
| IBackupService.RecordBackupStartAsync | Records start of a new backup operation | src/Tabsan.EduSphere.Application/Interfaces/IBackupService.cs |
| IBackupService.UpdateBackupStatusAsync | Updates backup record to Completed/Failed/Verified | src/Tabsan.EduSphere.Application/Interfaces/IBackupService.cs |
| IBackupService.GetStatusSummaryAsync | Returns latest backup status per type for monitoring dashboard | src/Tabsan.EduSphere.Application/Interfaces/IBackupService.cs |
| BackupService | Full implementation of backup logging and querying | src/Tabsan.EduSphere.Infrastructure/Backup/BackupService.cs |
| BackupController.GetLogs | GET /api/v1/backup/logs — paged backup history | src/Tabsan.EduSphere.API/Controllers/BackupController.cs |
| BackupController.RecordBackup | POST /api/v1/backup/logs — record backup start | src/Tabsan.EduSphere.API/Controllers/BackupController.cs |
| BackupController.UpdateStatus | PUT /api/v1/backup/logs/{id} — update backup status | src/Tabsan.EduSphere.API/Controllers/BackupController.cs |
| BackupController.GetStatus | GET /api/v1/backup/status — latest backup summary | src/Tabsan.EduSphere.API/Controllers/BackupController.cs |
| BackupLogConfiguration | EF Core fluent config for backup_logs table + 2 indexes | src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/BackupLogConfiguration.cs |
| LoginActivityLog (entity) | Immutable record of every login attempt with structured fields for monitoring | src/Tabsan.EduSphere.Domain/Activity/LoginActivityLog.cs |
| ILoginActivityRepository.AddAsync | Appends a new login activity record | src/Tabsan.EduSphere.Domain/Interfaces/ILoginActivityRepository.cs |
| LoginActivityRepository.AddAsync | EF Core implementation — persists login activity entries | src/Tabsan.EduSphere.Infrastructure/Repositories/LoginActivityRepository.cs |
| ILoginActivityService.GetActivityAsync | Paged query of login activity with filters | src/Tabsan.EduSphere.Application/Interfaces/ILoginActivityService.cs |
| ILoginActivityService.GetSummaryAsync | Aggregated dashboard summary with daily breakdown, top failures, top IPs | src/Tabsan.EduSphere.Application/Interfaces/ILoginActivityService.cs |
| LoginActivityService.GetActivityAsync | EF query implementation with user/status/risk/reason/date filters | src/Tabsan.EduSphere.Infrastructure/Activity/LoginActivityService.cs |
| LoginActivityService.GetSummaryAsync | In-memory aggregation of login data for dashboard trends | src/Tabsan.EduSphere.Infrastructure/Activity/LoginActivityService.cs |
| LoginActivityFilterDto | Filter DTO (query, userId, isSuccess, riskLevel, failureReason, fromUtc, toUtc, page, pageSize) | src/Tabsan.EduSphere.Application/DTOs/LoginActivityDtos.cs |
| LoginActivityItemDto | Flat response DTO for login activity records | src/Tabsan.EduSphere.Application/DTOs/LoginActivityDtos.cs |
| LoginActivitySummaryDto | Dashboard summary DTO with DailyBreakdown, TopFailureReasons, TopIps | src/Tabsan.EduSphere.Application/DTOs/LoginActivityDtos.cs |
| AuthService.RecordLoginActivityAsync | Fire-and-forget helper that writes a LoginActivityLog entry after every login attempt | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| LoginActivityController.GetActivity | GET /api/v1/login-activity — paged query with Admin/SuperAdmin auth | src/Tabsan.EduSphere.API/Controllers/LoginActivityController.cs |
| LoginActivityController.GetSummary | GET /api/v1/login-activity/summary — dashboard trends with daily breakdown | src/Tabsan.EduSphere.API/Controllers/LoginActivityController.cs |
| LoginActivityLogConfiguration.Configure | EF Core fluent config for login_activity_logs table and 4 indexes | src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/LoginActivityLogConfiguration.cs |
| ApplicationDbContext.LoginActivityLogs | DbSet for login_activity_logs table | src/Tabsan.EduSphere.Infrastructure/Persistence/ApplicationDbContext.cs |
| User.LastPasswordChangedAt | Tracks UTC timestamp of most recent password change for ageing policy | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.IsPasswordExpired(maxAgeDays) | Returns true when password has exceeded the maximum allowed age | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| PasswordHistoryEntry.ExpiresAt | Optional UTC timestamp for history entry archival/pruning | src/Tabsan.EduSphere.Domain/Identity/PasswordHistoryEntry.cs |
| UserSession.LastActivityAt | Tracks last authenticated request timestamp for idle timeout | src/Tabsan.EduSphere.Domain/Identity/UserSession.cs |
| UserSession.TouchActivity() | Updates LastActivityAt to current UTC — called on each authenticated action | src/Tabsan.EduSphere.Domain/Identity/UserSession.cs |
| UserSession.IsActiveWithinIdleTimeout(minutes) | Returns true when active and idle timeout not exceeded; falls back to CreatedAt for legacy sessions | src/Tabsan.EduSphere.Domain/Identity/UserSession.cs |
| PasswordAgeingSettings | Configuration for password ageing: MaxPasswordAgeDays (default 90) | src/Tabsan.EduSphere.Application/Auth/AuthSecurityOptions.cs |
| SessionTimeoutSettings | Configuration for idle session timeout: Enabled (default true), IdleTimeoutMinutes (default 30) | src/Tabsan.EduSphere.Application/Auth/AuthSecurityOptions.cs |
| ActiveSessionDto | DTO for active session display (SessionId, UserId, Username, FullName, Role, DeviceInfo, IpAddress, CreatedAt, LastActivityAt, ExpiresAt) | src/Tabsan.EduSphere.Application/DTOs/AccountSecurityDtos.cs |
| RevokeSessionRequest | DTO for admin session revocation by session ID | src/Tabsan.EduSphere.Application/DTOs/AccountSecurityDtos.cs |
| IUserSessionRepository.GetByIdAsync | Finds session by primary key for manual revocation | src/Tabsan.EduSphere.Application/Interfaces/IUserSessionRepository.cs |
| IUserSessionRepository.GetActiveSessionsAsync | Returns all active sessions with user info for admin screen | src/Tabsan.EduSphere.Application/Interfaces/IUserSessionRepository.cs |
| IUserSessionRepository.GetActiveSessionsByUserIdAsync | Returns active sessions for a specific user | src/Tabsan.EduSphere.Application/Interfaces/IUserSessionRepository.cs |
| IUserSessionRepository.GetIdleSessionsAsync | Returns active sessions idle beyond timeout window | src/Tabsan.EduSphere.Application/Interfaces/IUserSessionRepository.cs |
| IAccountSecurityService.GetActiveSessionsAsync | Admin interface for listing all active sessions | src/Tabsan.EduSphere.Application/Interfaces/IAccountSecurityService.cs |
| IAccountSecurityService.RevokeSessionAsync | Admin interface for revoking a specific session | src/Tabsan.EduSphere.Application/Interfaces/IAccountSecurityService.cs |
| IAccountSecurityService.RevokeAllSessionsForUserAsync | Admin interface for revoking all sessions for a user | src/Tabsan.EduSphere.Application/Interfaces/IAccountSecurityService.cs |
| AccountSecurityService.GetActiveSessionsAsync | Returns all active sessions mapped to DTOs with user role info | src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs |
| AccountSecurityService.RevokeSessionAsync | Force-revokes a session by ID with audit logging | src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs |
| AccountSecurityService.RevokeAllSessionsForUserAsync | Force-revokes all active sessions for a user with audit logging | src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs |
| UserSessionRepository.GetByIdAsync | EF implementation — finds session by PK | src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs |
| UserSessionRepository.GetActiveSessionsAsync | EF implementation — active sessions with User+Role eager loading, ordered by activity | src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs |
| UserSessionRepository.GetActiveSessionsByUserIdAsync | EF implementation — user-scoped active session query | src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs |
| UserSessionRepository.GetIdleSessionsAsync | EF implementation — idle session detection using LastActivityAt/CreatedAt cutoff | src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs |
| AccountSecurityController.GetActiveSessions | GET /api/v1/account-security/sessions — lists all active sessions | src/Tabsan.EduSphere.API/Controllers/AccountSecurityController.cs |
| AccountSecurityController.RevokeSession | POST /api/v1/account-security/sessions/{id}/revoke — force-revokes a session | src/Tabsan.EduSphere.API/Controllers/AccountSecurityController.cs |
| AccountSecurityController.RevokeAllUserSessions | POST /api/v1/account-security/users/{id}/revoke-sessions — revokes all sessions for user | src/Tabsan.EduSphere.API/Controllers/AccountSecurityController.cs |
| TryResolveActorUserId | Resolves actor user ID from JWT claims | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| TryResolveActorRole | Resolves actor role from JWT claims | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| TryResolveIpAddress | Resolves client IP from X-Forwarded-For header or connection | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| TryResolveUserAgent | Resolves User-Agent from request headers | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| TryResolveDeviceInfo | Resolves device info from Sec-CH-UA headers or User-Agent | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| TryResolveCorrelationId | Resolves CorrelationId from X-Correlation-Id header or HttpContext.TraceIdentifier | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| AuditService.LogAsync | Enriches and persists audit entries with auto-resolved context (userId, role, IP, user-agent, correlationId) | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| AuditService.SearchAsync | Queryable audit log search with Phase 1 filters (actorRole, severity, eventCategory, correlationId) | src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs |
| IAuditService.LogAsync | Interface for writing audit log entries | src/Tabsan.EduSphere.Domain/Interfaces/IAuditService.cs |
| IAuditService.SearchAsync | Interface for searching audit logs with Phase 1 filter parameters | src/Tabsan.EduSphere.Domain/Interfaces/IAuditService.cs |
| AuditLog (entity) | Immutable audit record with bigint PK, CorrelationId, Severity, EventCategory | src/Tabsan.EduSphere.Domain/Auditing/AuditLog.cs |
| EnforceImmutableAuditLogs | Blocks UPDATE/DELETE on audit_logs at DbContext level | src/Tabsan.EduSphere.Infrastructure/Persistence/ApplicationDbContext.cs |
| AuditController.SearchLogs | GET /api/v1/audit/logs — filtered search with Phase 1 query params | src/Tabsan.EduSphere.API/Controllers/AuditController.cs |
| AuditController.ExportLogs | GET /api/v1/audit/logs/export/{format} — CSV/Excel/PDF export with Phase 1 filters | src/Tabsan.EduSphere.API/Controllers/AuditController.cs |
| AuditController.BuildCsv | Generates CSV byte array with CorrelationId, Severity, EventCategory columns | src/Tabsan.EduSphere.API/Controllers/AuditController.cs |
| AuditController.BuildExcel | Generates Excel byte array with CorrelationId, Severity, EventCategory columns | src/Tabsan.EduSphere.API/Controllers/AuditController.cs |
| AuditController.BuildPdf | Generates PDF byte array with Severity, Category columns | src/Tabsan.EduSphere.API/Controllers/AuditController.cs |
| IEduApiClient.SearchAuditLogsAsync | Web client contract for audit log search | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.ExportAuditLogsCsvAsync | Web client contract for audit log CSV export | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.ExportAuditLogsExcelAsync | Web client contract for audit log Excel export | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.ExportAuditLogsPdfAsync | Web client contract for audit log PDF export | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.SearchAuditLogsAsync | Calls audit log search API endpoint | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ExportAuditLogsCsvAsync | Calls audit log CSV export API endpoint | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ExportAuditLogsExcelAsync | Calls audit log Excel export API endpoint | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ExportAuditLogsPdfAsync | Calls audit log PDF export API endpoint | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| PortalController.AuditLogs | Renders the admin audit monitoring UI with filterable logs | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ExportAuditLogsCsv | Web action for audit log CSV export from portal | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ExportAuditLogsExcel | Web action for audit log Excel export from portal | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ExportAuditLogsPdf | Web action for audit log PDF export from portal | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| AuditLogConfiguration.Configure | EF Core fluent config for audit_logs table, columns, and indexes | src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs |
| AuthService.LoginAsync | Authenticates user, enforces lockout/MFA/session-risk/license concurrency, creates session | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.RefreshAsync | Rotates refresh token after validating session activity | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.LogoutAsync | Revokes the current user session | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.ChangePasswordAsync | Verifies old password, checks history (last 5), updates hash, logs audit | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.ForceChangePasswordAsync | Allows MustChangePassword user to set new password with policy enforcement | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.BeginMfaSetupAsync | Starts TOTP enrollment, returns QR/manual-key payload | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.EnableMfaAsync | Confirms MFA enrollment with TOTP code verification | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.RegenerateRecoveryCodesAsync | Regenerates MFA recovery codes | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| AuthService.GetSecurityProfileAsync | Returns authentication security features configuration (MFA, SSO, session risk) | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| IAuthService.LoginAsync | Interface for user authentication | src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs |
| IAuthService.ChangePasswordAsync | Interface for password change | src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs |
| IAuthService.ForceChangePasswordAsync | Interface for forced password change | src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs |
| IAuthService.LogoutAsync | Interface for session logout | src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs |
| IAuthService.RefreshAsync | Interface for token refresh | src/Tabsan.EduSphere.Application/Interfaces/IAuthService.cs |
| PasswordPolicyRules.BeSafePassword | Enforces password strength: 12-16 chars, upper, lower, digit, symbol, banned patterns | src/Tabsan.EduSphere.Application/Validators/AuthValidators.cs |
| AuthController.Login | POST /api/v1/auth/login — authenticates and returns JWT + refresh token | src/Tabsan.EduSphere.API/Controllers/AuthController.cs |
| AuthController.Refresh | POST /api/v1/auth/refresh — rotates refresh token | src/Tabsan.EduSphere.API/Controllers/AuthController.cs |
| AuthController.Logout | POST /api/v1/auth/logout — revokes current session | src/Tabsan.EduSphere.API/Controllers/AuthController.cs |
| AuthController.ChangePassword | PUT /api/v1/auth/change-password — changes authenticated user password | src/Tabsan.EduSphere.API/Controllers/AuthController.cs |
| AuthController.ForceChangePassword | POST /api/v1/auth/force-change-password — forced password change for MustChangePassword users | src/Tabsan.EduSphere.API/Controllers/AuthController.cs |
| AuthController.GetSecurityProfile | GET /api/v1/auth/security-profile — returns auth security feature flags | src/Tabsan.EduSphere.API/Controllers/AuthController.cs |
| AuthSecurityOptions | Configuration class for MFA, SSO, session risk, password ageing, session timeout settings | src/Tabsan.EduSphere.Application/Auth/AuthSecurityOptions.cs |
| AccountSecurityService.GetLockedAccountsAsync | Lists all currently locked non-admin accounts | src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs |
| AccountSecurityService.GetLockoutStatusAsync | Returns lockout status for a specific user | src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs |
| AccountSecurityService.UnlockAccountAsync | Unlocks a locked account and resets failed attempts | src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs |
| AccountSecurityService.ResetPasswordAsync | Admin resets password for non-admin accounts | src/Tabsan.EduSphere.Application/Services/AccountSecurityService.cs |
| AccountSecurityController.GetLockedAccounts | GET /api/v1/account-security/locked — lists locked accounts | src/Tabsan.EduSphere.API/Controllers/AccountSecurityController.cs |
| AccountSecurityController.GetLockoutStatus | GET /api/v1/account-security/{userId}/status — user lockout status | src/Tabsan.EduSphere.API/Controllers/AccountSecurityController.cs |
| AccountSecurityController.UnlockAccount | POST /api/v1/account-security/{userId}/unlock — unlocks account | src/Tabsan.EduSphere.API/Controllers/AccountSecurityController.cs |
| AccountSecurityController.ResetPassword | POST /api/v1/account-security/{userId}/reset-password — admin password reset | src/Tabsan.EduSphere.API/Controllers/AccountSecurityController.cs |
| TokenService.GenerateAccessToken | Creates JWT with user claims (sub, role, tenant, campus, studentProfileId) | src/Tabsan.EduSphere.Infrastructure/Auth/TokenService.cs |
| TokenService.GenerateRefreshToken | Generates cryptographically random 64-byte refresh token | src/Tabsan.EduSphere.Infrastructure/Auth/TokenService.cs |
| TokenService.HashRefreshToken | Produces SHA-256 hex hash of refresh token | src/Tabsan.EduSphere.Infrastructure/Auth/TokenService.cs |
| PasswordHasher.Hash | Hashes password using ASP.NET Core Identity v3 PBKDF2 with HMACSHA512 | src/Tabsan.EduSphere.Infrastructure/Auth/PasswordHasher.cs |
| PasswordHasher.Verify | Verifies password against stored hash | src/Tabsan.EduSphere.Infrastructure/Auth/PasswordHasher.cs |
| User.RecordLogin | Sets LastLoginAt, resets failed attempts, clears lockout | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.RecordFailedLoginAttempt | Increments failed counter, locks account at threshold (5 attempts, 15 min) | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.IsCurrentlyLockedOut | Returns true if account is locked and lockout period hasn't expired | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.UnlockAccount | Manually unlocks account (admin action) | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.UpdatePasswordHash | Replaces password hash and records LastPasswordChangedAt | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| UserSession.IsActive | Computed property — true when not revoked and not expired | src/Tabsan.EduSphere.Domain/Identity/UserSession.cs |
| UserSession.Revoke | Invalidates session by setting RevokedAt | src/Tabsan.EduSphere.Domain/Identity/UserSession.cs |
| UserSession.Rotate | Replaces refresh token hash and expiry for token rotation | src/Tabsan.EduSphere.Domain/Identity/UserSession.cs |
| PasswordHistoryEntry | Stores previous password hashes with optional ExpiresAt for archival | src/Tabsan.EduSphere.Domain/Identity/PasswordHistoryEntry.cs |
| UserSessionRepository.GetActiveByHashAsync | Finds active session by refresh token hash | src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs |
| UserSessionRepository.GetMostRecentByUserIdAsync | Returns user's most recent session for risk assessment | src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs |
| UserSessionRepository.CountActiveSessionsAsync | Counts all active sessions for license concurrency enforcement | src/Tabsan.EduSphere.Infrastructure/Repositories/UserSessionRepository.cs |
| PasswordHistoryRepository.GetRecentAsync | Returns last N password hashes for reuse prevention | src/Tabsan.EduSphere.Infrastructure/Repositories/PasswordHistoryRepository.cs |
| UserRepository.GetLockedAccountsAsync | Returns all locked non-admin accounts | src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs |
| UserRepository.GetByUsernameAsync | Finds user by username with role navigation | src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs |
| AdminUserController.Create | POST /api/v1/admin-user — creates admin user with institution type | src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs |
| AdminUserController.GetAll | GET /api/v1/admin-user — lists admin users | src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs |
| AdminUserController.Update | PUT /api/v1/admin-user/{id} — updates admin user | src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs |
| UserSettingsController.GetProfile | GET /api/v1/user-settings/me — current user profile | src/Tabsan.EduSphere.API/Controllers/UserSettingsController.cs |
| UserSettingsController.UpdateProfile | PUT /api/v1/user-settings/me — updates current user profile | src/Tabsan.EduSphere.API/Controllers/UserSettingsController.cs |
| UserSettingsController.ResetPassword | POST /api/v1/user-settings/{userId}/reset-password — admin password reset to default | src/Tabsan.EduSphere.API/Controllers/UserSettingsController.cs |
| ConfigurationBootstrapper.AddEduSphereConfigurationHierarchy | Loads layered configuration including parent/root environments.json, optional external profile file, and environment-variable overlays in deterministic order. | src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs |
| ConfigurationBootstrapper.InsertJsonSourceIfMissing | Adds JSON config sources idempotently and now supports absolute-path files through a physical file provider for reliable profile loading. | src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs |
| ResultCalculationController.Get | Returns institute-scoped result calculation settings using institutionType filter with safe university fallback. | src/Tabsan.EduSphere.API/Controllers/ResultCalculationController.cs |
| ResultCalculationController.Save | Validates institute type and persists institute-scoped GPA/component rules. | src/Tabsan.EduSphere.API/Controllers/ResultCalculationController.cs |
| ResultCalculationService.GetSettingsAsync | Retrieves institute-scoped GPA and component rules for result calculation settings. | src/Tabsan.EduSphere.Application/Assignments/ResultCalculationService.cs |
| ResultCalculationService.SaveSettingsAsync | Validates and saves institute-scoped result-calculation components and GPA rules with weight/threshold checks. | src/Tabsan.EduSphere.Application/Assignments/ResultCalculationService.cs |
| ResultRepository.GetAllComponentRulesAsync(InstitutionType, ...) | Returns all result component rules for the selected institution scope. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| ResultRepository.GetGpaScaleRulesAsync(InstitutionType, ...) | Returns GPA scale rules for the selected institution scope. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| ResultRepository.ReplaceCalculationRulesAsync(InstitutionType, ...) | Replaces existing result-calculation rules for only the selected institution scope. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| PortalController.GetCurrentUserId | Resolves current portal user id from claims for 2FA and secure user-context workflows. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| AnnouncementController.CreateAnnouncement | Normalizes invalid offering and runtime validation failures into consistent 400 responses instead of leaking unhandled API exceptions. | src/Tabsan.EduSphere.API/Controllers/AnnouncementController.cs |
| GraduationService.RejectInternalAsync | Converts optimistic-concurrency conflicts in graduation rejection flow into deterministic business error messaging for safe retries. | src/Tabsan.EduSphere.Application/Academic/GraduationService.cs |
| LmsService.CreateModuleAsync | Guards LMS module creation against invalid/offline offerings at both pre-check and save stages to avoid FK-driven 500 responses. | src/Tabsan.EduSphere.Application/Lms/LmsService.cs |
| PortalController.CreateAnnouncement | Blocks announcement posting when no valid offering is selected and returns a user-safe validation message. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| FypRepository.GetAllAsync(...) | Uses direct awaited EF execution (no ContinueWith) to avoid DbContext second-operation runtime faults in high-load FYP reads. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| FypPanelRole.Internal / FypPanelRole.External | Backward-compatible enum aliases that safely map legacy panel-role database string values. | src/Tabsan.EduSphere.Domain/Fyp/FypProject.cs |
| AddHealthChecks | Registers database, memory, CPU, network, and error-rate checks for continuous runtime health monitoring. | src/Tabsan.EduSphere.API/Program.cs |
| AddOpenTelemetry | Publishes ASP.NET Core, HttpClient, runtime, and process metrics and exposes Prometheus scraping support. | src/Tabsan.EduSphere.API/Program.cs |
| AddResponseCompression (API) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | src/Tabsan.EduSphere.API/Program.cs |
| AddResponseCompression (Web) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | src/Tabsan.EduSphere.Web/Program.cs |
| PortalController.BeginTwoFactorSetup | Starts the portal-side 2FA setup flow and reloads the settings page with QR/manual-key payloads. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DisableTwoFactor | Disables portal-side 2FA and returns the user to the 2FA settings page with status messaging. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.TestTwoFactorLogin | Tests the portal-side 2FA login hand-off and returns the user to the 2FA settings page with status messaging. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.TwoFactorSettings | Renders the portal-side 2FA settings page and binds the current user context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.VerifyTwoFactorSetup | Confirms the portal-side 2FA setup code and returns status messaging on the settings page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| TwoFactorController.Disable | Disables add-on 2FA after validating the current TOTP code. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.LoginVerify | Verifies a pending login TOTP hand-off for the add-on 2FA flow. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Setup | Starts add-on 2FA enrollment for the current signed-in user. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Verify | Confirms add-on 2FA setup with the initial TOTP code. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorService.BuildProvisioningUri | Builds an authenticator provisioning URI using the configured issuer and TOTP settings. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs |
| TwoFactorService.GenerateSecret | Generates a fresh Base32 TOTP secret for the add-on setup flow. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs |
| TwoFactorService.ValidateCode | Validates a TOTP code using the configured time-step and drift window. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs |
| TwoFactorSetupService.BeginSetupAsync | Starts 2FA enrollment, persists the protected secret, and returns QR/manual-key payloads. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorSetupService.DisableAsync | Disables 2FA after validating the current code against the stored secret. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorSetupService.VerifyLoginAsync | Verifies the login hand-off code for the add-on 2FA challenge. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorSetupService.VerifySetupAsync | Confirms the initial enrollment code before enabling 2FA. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorSetupService.ResetAsync | Resets 2FA for a user using a valid recovery code and restores a fresh secret. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorStateStore.TryUnprotect | Decrypts Data-Protection-encrypted TOTP secret with backward-compatible Base32 fallback for raw secrets. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.IsValidBase32Secret | Validates whether a string is a syntactically correct Base32 TOTP secret. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.HardDeleteAsync | Permanently removes MFA secret, recovery codes, and disables MFA for a user. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| ITwoFactorStateStore.HardDeleteAsync | Interface for permanent MFA secret and recovery-code removal. | src/Tabsan.EduSphere.Application/Interfaces/ITwoFactorStateStore.cs |
| TwoFactorController.Reset | POST /api/v1/two-factor/reset — resets 2FA using a recovery code. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.ResendRecoveryCodes | POST /api/v1/two-factor/recovery-codes — regenerates recovery codes for an authenticated user. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TotpService.Base32Decode | Decodes a Base32-encoded TOTP secret into raw key bytes for HMAC computation. | src/Tabsan.EduSphere.Infrastructure/Auth/TotpService.cs |
| TotpService.Base32Encode | Encodes raw key bytes into a Base32 string for TOTP secret provisioning. | src/Tabsan.EduSphere.Infrastructure/Auth/TotpService.cs |
| AuthService.LoginAsync | Single-step MFA login: validates TOTP only when user has MFA enabled; returns MfaCodeRequired or InvalidMfaCode. | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| LoginController.Index (POST) | Forwards MFA code to API login, surfaces MFA-required/MFA-invalid error messaging from API responses. | src/Tabsan.EduSphere.Web/Controllers/LoginController.cs |
| IEduApiClient.GetSecurityProfileAsync | Fetches auth security profile from API to determine whether MFA/SSO prompts should render. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GetTenantsAsync | Returns active-only tenants for dropdowns via client-side .Where(t => t.IsActive) filter. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| CampusController.GetAll | Returns campuses filtered by activeOnly=true (default) to hide deactivated campuses from dropdowns. | src/Tabsan.EduSphere.API/Controllers/CampusController.cs |
| ReportController | All report endpoints: relaxed scope enforcement; Faculty and Admin can run reports without mandatory department/course filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportService.GetAttendanceSummaryAsync | Populates ProgramName and DepartmentName from academic_programs left-join. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportRepository | All four report queries (attendance, result, assignment, quiz) now left-join academic_programs for ProgramName. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportRepository.cs |
| SemesterRepository.GetAllAsync | Returns semesters sorted by StartDate ascending (oldest first). | src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicProgramRepository.cs |
| SessionTimeoutSettings.IdleTimeoutMinutes | Session idle timeout reduced from 30 to 5 minutes. | src/Tabsan.EduSphere.Application/Auth/AuthSecurityOptions.cs |
| TwoFactorSetupService.ResetAsync | Resets 2FA for a user using a valid recovery code and restores a fresh secret. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorStateStore.TryUnprotect | Decrypts Data-Protection-encrypted TOTP secret with backward-compatible Base32 fallback for raw secrets. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.IsValidBase32Secret | Validates whether a string is a syntactically correct Base32 TOTP secret. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.HardDeleteAsync | Permanently removes MFA secret, recovery codes, and disables MFA for a user. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| ITwoFactorStateStore.HardDeleteAsync | Interface for permanent MFA secret and recovery-code removal. | src/Tabsan.EduSphere.Application/Interfaces/ITwoFactorStateStore.cs |
| TwoFactorController.Reset | POST /api/v1/two-factor/reset — resets 2FA using a recovery code. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.ResendRecoveryCodes | POST /api/v1/two-factor/recovery-codes — regenerates recovery codes for an authenticated user. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TotpService.Base32Decode | Decodes a Base32-encoded TOTP secret into raw key bytes for HMAC computation. | src/Tabsan.EduSphere.Infrastructure/Auth/TotpService.cs |
| TotpService.Base32Encode | Encodes raw key bytes into a Base32 string for TOTP secret provisioning. | src/Tabsan.EduSphere.Infrastructure/Auth/TotpService.cs |
| AuthService.LoginAsync | Updated: MFA enforcement temporarily disabled on login; password-only authentication restored. | src/Tabsan.EduSphere.Application/Auth/AuthService.cs |
| LoginController.Index (POST) | Updated: forwards MFA code to API login and surfaces MFA-required error messaging from the API response. | src/Tabsan.EduSphere.Web/Controllers/LoginController.cs |
| IEduApiClient.GetSecurityProfileAsync | Fetches auth security profile from API to determine whether MFA/SSO prompts should render. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DisableTwoFactorAsync | Calls API to disable 2FA after validating the current TOTP code. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.TestTwoFactorLoginAsync | Calls API to verify a pending 2FA login hand-off code. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.VerifyTwoFactorSetupAsync | Calls API to confirm initial 2FA enrollment with a TOTP code. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.BeginTwoFactorSetupAsync | Calls API to start 2FA enrollment and returns QR/manual-key payload. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| ApiConnectionModel | Updated with MfaEnabled and MfaCode fields for portal-side MFA state tracking. | src/Tabsan.EduSphere.Web/Models/Portal/ApiConnectionModel.cs |
| TwoFactorApiModels | DTOs for portal-side 2FA setup, verify, disable, and login-verify API calls. | src/Tabsan.EduSphere.Web/Services/TwoFactorApiModels.cs |
| AdminUserController.Create | Accepts optional institutionType, validates against active policy, persists to user, and returns assignment in response/list payloads. | src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs |
| AiChatService.GetConversationAsync | Returns a full conversation thread with message history for the requesting user. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AiChatService.GetConversationsAsync | Returns the requesting user's conversation list for the AI chat experience. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AiChatService.SendMessageAsync | Sends a user message to the AI provider and persists the response in conversation history. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AnalyticsController.GetPaymentStatus | Accepts and forwards course/semester filters for payment analytics requests. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| AnalyticsService.BuildAnalyticsCacheKey(...) | Enforces cache scope boundaries by keying shared analytics cache entries to report type and department scope. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAssignmentStatsAsync(...) | Adds short-TTL distributed cache policy for expensive assignment analytics reads and supports course/semester-scoped filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAttendanceReportAsync(...) | Adds short-TTL distributed cache policy for expensive attendance analytics reads and supports course/semester-scoped filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPaymentStatusReportAsync(...) | Aggregates scoped paid vs unpaid receipt counts/amounts and supports course/semester-scoped filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPerformanceReportAsync(...) | Adds short-TTL distributed cache policy for expensive department/all performance analytics reads and supports course/semester scope. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetQuizStatsAsync(...) | Adds short-TTL distributed cache policy for expensive quiz analytics reads. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| BuildingRoomRepository.GetAllBuildingsAsync(...) | Returns building lists with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| BuildingRoomRepository.GetAllRoomsAsync(...) | Returns room lists with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| BuildingRoomRepository.GetRoomsByBuildingAsync(...) | Returns building-scoped room lists with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| CanAccessExportJob | Enforces owner-or-superadmin and tenant/campus scope parity checks for export job access. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| CourseController.GetOfferings | Exposes department metadata for deterministic dependent filtering in web layer. | src/Tabsan.EduSphere.API/Controllers/CourseController.cs |
| CourseMaterial.EnsureMaterialLocation | Enforces material-type-specific file/link requirements before persistence. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.EnsureRequiredScope | Prevents creation of unscoped material records by rejecting empty scope identifiers. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.UpdateLocation | Updates material storage location for file/link and validates required location by material type. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.UpdateMetadata | Updates material title and description metadata. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterialController.BuildScopedStorageCategory | Builds tenant/campus-aware storage category path for Course Material upload isolation. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.Create | Creates scoped material records for authorized Faculty/Admin/SuperAdmin users using caller identity. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.DownloadFile | Streams stored material files with metadata-aware content type and file name for authorized scoped users. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.GetAll | Returns course materials with strict repository-enforced tenant/campus filtering. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.Upload | Uploads validated material files and persists them in scoped storage for Faculty/Admin/SuperAdmin users. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialRepository.ApplyTenantCampusScope | Enforces strict tenant/campus query isolation with SuperAdmin bypass behavior. | src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs |
| CourseMaterialRepository.IsScopeMissingForNonSuperAdmin | Short-circuits read operations early when tenant scope is missing for non-SuperAdmin callers. | src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs |
| DashboardCompositionController.GetContext(...) | Provides role- and policy-aware module/vocabulary/widget composition by aggregating visible modules, academic vocabulary, and widgets into one dashboard-context response. | src/Tabsan.EduSphere.API/Controllers/DashboardCompositionController.cs |
| DashboardCompositionService.GetWidgets(...) | Adds short-TTL in-memory cache for dashboard widget composition keyed by role and institution policy state to reduce repeated composition cost. | src/Tabsan.EduSphere.Application/Services/DashboardCompositionService.cs |
| DatabaseConnectionResolver.ResolveDefaultConnection | Resolves DB connection string from prioritized environment/deployment keys with backward-compatible fallback to legacy connection-string key. | src/Tabsan.EduSphere.Application/Services/DatabaseConnectionResolver.cs |
| DatabaseSeeder.SeedRolesAsync | Seeds Finance role additively alongside existing system roles during startup bootstrap. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| DatabaseSeeder.SeedSidebarMenusAsync(...) | Makes sidebar role seeding self-healing by updating existing role-access values, including the new enter_attendance menu, so corrected visibility rules are enforced on already-seeded databases. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| PortalController.EnterAttendance | Opens the new Enter Attendance menu route while reusing the current attendance screen and preserving guarded access. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadAttendanceCsvTemplate | Generates and downloads the Enter Attendance CSV template with required headers and sample rows. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ImportAttendanceCsv | Validates Enter Attendance CSV rows and imports attendance using existing bulk-mark API calls grouped by date. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.WriteAttendanceImportAudit (local function) | Emits per-upload attendance CSV audit trail details with actor/time/strict-mode/row counts/error reasons. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadAttendanceImportReport | Serves one-time downloadable CSV import result reports using report tokens. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.AttendanceImportReportTtl / UtcNowProvider | Controls report retention window and deterministic time evaluation for token expiry decisions. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| AttendancePageModel.MessageDetails | Carries row-level CSV import feedback details for attendance UI rendering. | src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs |
| AttendancePageModel.ImportReportToken | Carries report token used to render last import report download action in Enter Attendance UI. | src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs |
| Attendance.cshtml import hint text | Explains one-time and 2-hour expiry behavior for import report links in UI. | src/Tabsan.EduSphere.Web/Views/Portal/Attendance.cshtml |
| PortalController.BulkMarkAttendance | Enforces roster-scoped student validation and normalized attendance statuses before submitting manual attendance entries. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CorrectAttendance | Enforces roster-scoped student validation and normalized status values before submitting attendance corrections. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ValidateAttendanceWriteScopeAsync | Validates selected offering against selected Department, Course, and Class/Semester under effective tenant/campus scope before attendance write operations. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| Department.SetTenantCampus(tenantId, campusId) | Assigns or clears tenant/campus ownership for department-level scoping while preserving InstitutionType behavior. | src/Tabsan.EduSphere.Domain/Academic/Department.cs |
| DepartmentRepository.ApplyTenantCampusScope | Applies tenant/campus query filtering for department reads with SuperAdmin bypass. | src/Tabsan.EduSphere.Infrastructure/Repositories/DepartmentRepository.cs |
| DeploymentTopologyResolver.Resolve | Resolves effective deployment mode, customer identity, domain, database name, and scaling settings from config and environment variables. | src/Tabsan.EduSphere.Application/Services/DeploymentTopologyResolver.cs |
| EduApiClient.BuildAnalyticsQuery | Builds analytics query string including optional courseId and semesterId filters. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetDashboardCompositionContextAsync(...) | Deserializes the aggregated dashboard-context endpoint response into a single Web client model. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetPaymentStatusAnalyticsAsync | Retrieves payment status analytics data for portal snapshots. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetPaymentSummaryReportAsync | Retrieves payment summary report data for the portal. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.MapPayment | Consumes payment payload with compatibility fallback (PaidDate ?? ConfirmedAt) and update trail mapping. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.UpdatePaymentAsync | Web client method that calls the finance payment edit endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EnsureDefaultTenantCampusAsync | Guarantees default tenant (DEFAULT) and default campus (MAIN) are present and active at startup. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| EnsureTenantCampusBackfillAsync | Performs startup safety backfill for users/departments missing tenant/campus assignments. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| GetAnalyticsAccessScope | Resolves effective analytics access scope from caller tenant/campus claims with superadmin bypass handling. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetAssignmentStatsAsync | Computes assignment statistics from grouped submission/enrollment snapshots instead of per-assignment query loops. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetComparativeSummaryAsync | Computes comparative summary metrics via department-grouped batched queries. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetCurrentTenantId | Resolves caller tenant/campus claim scope used for export-job ownership checks. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| GetPerformanceReportAsync | Builds performance report using batched results/submissions aggregation instead of per-student round-trips. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetQuizStatsAsync | Aggregates quiz attempt statistics in a grouped pass rather than per-quiz queries. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GradebookService.GetGradebookAsync(...) | Removes sync-over-async .Result consumption from the gradebook request path by awaiting completed tasks. | src/Tabsan.EduSphere.Application/Assignments/GradebookService.cs |
| HttpTenantScopeResolver.GetTenantScopeKey() | Resolves tenant scope from JWT claims or X-Tenant-Code request header for API-request-scoped operations. | src/Tabsan.EduSphere.API/Services/HttpTenantScopeResolver.cs |
| IAnalyticsService.GetPaymentStatusReportAsync(..., courseId, semesterId) | Exposes filter-aware payment analytics contract including course/semester dimensions. | src/Tabsan.EduSphere.Application/Interfaces/IAnalyticsService.cs |
| IEduApiClient.DownloadCourseMaterialFileAsync | Downloads Course Material files from API for portal-proxied file delivery. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GetCourseMaterialsAsync | Fetches scoped course materials for portal pages with optional active-only filtering. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GetDashboardCompositionContextAsync(...) | Fetches the aggregated dashboard-context payload in a single API request for the web portal. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.UploadCourseMaterialFileAsync | Uploads Course Material files from portal to API multipart endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEnrollmentRepository.GetWaitlistedByOfferingAsync | Returns waitlisted enrollments in queue order so promotion can be deterministic. | src/Tabsan.EduSphere.Domain/Interfaces/IEnrollmentRepository.cs |
| INotificationRepository.GetForUserAsync(..., asNoTracking, ...) | Adds opt-in no-tracking control for read-heavy inbox retrieval while preserving tracked reads for mark-all-read operations. | src/Tabsan.EduSphere.Domain/Interfaces/INotificationRepository.cs |
| InstitutionPolicyController.Save | Used by SuperAdmin matrix run to switch School/College/University modes and validate privileged access continuity. | src/Tabsan.EduSphere.API/Controllers/InstitutionPolicyController.cs |
| InstitutionPolicyService.SavePolicyAsync | Persists institution policy flags to portal_settings by committing settings repository changes, enabling mode switches from uploaded licenses to be retained. | src/Tabsan.EduSphere.Application/Services/InstitutionPolicyService.cs |
| IReportRepository.GetPaymentSummaryDataAsync | Queries filtered payment receipt reporting rows with tenant/campus-safe scope and academic dimensions. | src/Tabsan.EduSphere.Domain/Interfaces/IReportRepository.cs |
| IStudentLifecycleService.UpdatePaymentReceiptAsync | Exposes finance receipt edit capability through the application contract. | src/Tabsan.EduSphere.Application/Interfaces/IStudentLifecycleService.cs |
| ITenantScopeResolver.GetTenantScopeKey() | Provides a tenant-scope abstraction for application-layer tenant-aware key resolution. | src/Tabsan.EduSphere.Application/Interfaces/ITenantScopeResolver.cs |
| LibraryService.BuildLoanLookupCacheKey(...) | Keys external-call cache entries by student identifier and integration configuration fingerprint to avoid stale cross-config reuse. | src/Tabsan.EduSphere.Application/Services/LibraryService.cs |
| LibraryService.GetLoansAsync(...) | Adds short-TTL distributed cache for safe external loan lookup reads to reduce repeated dependency calls. | src/Tabsan.EduSphere.Application/Services/LibraryService.cs |
| NotificationRepository.GetActiveUserPhoneNumbersAsync | Resolves distinct active recipient phone numbers by user IDs for SMS dispatch. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationRepository.GetForUserAsync(..., asNoTracking, ...) | Applies optional AsNoTracking to notification recipient + parent notification read path. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationRepository.GetUnreadCountAsync(...) | Removes unnecessary Include from unread count query to avoid extra query shaping overhead. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationService.BumpCacheVersion() | Invalidates notification read-cache windows by incrementing cache version after notification mutations. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| NotificationService.GetBadgeAsync(...) | Adds short-TTL in-memory cache for unread badge counts with cache-version keying. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| NotificationService.GetInboxAsync(...) | Adds short-TTL in-memory cache for paged inbox reads with cache-version keying for mutation-safe freshness. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| PaymentReceipt.UpdateDetails | Updates actionable receipt fields while preserving audit trail and blocking edits on Paid/Cancelled receipts. | src/Tabsan.EduSphere.Domain/StudentLifecycle/PaymentReceipt.cs |
| PaymentReceiptController.Delete | Explicitly rejects receipt deletion requests with 405 to preserve immutable payment history. | src/Tabsan.EduSphere.API/Controllers/PaymentReceiptController.cs |
| PaymentReceiptController.Update | API endpoint for finance/admin users to edit actionable payment receipts. | src/Tabsan.EduSphere.API/Controllers/PaymentReceiptController.cs |
| PortalController.BuildAnalyticsPageModelAsync | Loads payment status analytics into analytics page/snapshot model and summary cards. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CreateCourseMaterial | Creates a course material from the portal manage page while preserving current filter state. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadCourseMaterialFile | Proxies material file download from API and preserves student/manage redirect context on failure. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.SetCourseMaterialActive | Toggles material active state from the portal manage page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdateCourseMaterial | Updates course material metadata/location from the portal manage page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdatePayment | Web action that posts finance payment edits from the payments page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UserImportTemplate(fileName) | Serves approved CSV template files from User Import Sheets/ via filename allow-list with traversal-safe path resolution. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QuizRepository.GetAllAttemptsForStudentAsync(...) | Returns student quiz attempts with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| QuizRepository.GetAttemptsAsync(...) | Returns quiz attempts with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| QuizRepository.GetByOfferingAsync(...) | Returns quiz lists with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| refreshSnapshot | Fetches analytics snapshot and updates filters/cards/charts without full page reload. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderCourseTrend | Renders course trend line based on average assignment marks. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderDepartmentCounts | Renders department-wise student counts using a bar chart. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderPaymentStatus | Renders the Paid vs Unpaid interactive pie chart on the analytics page. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderSemesterTrend | Renders combined semester trend lines for marks and attendance where available. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderStudentDistribution | Renders semester-based student distribution using a pie chart. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| ReportController.ExportAssignmentSummary | Exports assignment summary report as Excel for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAssignmentSummaryCsv | Exports assignment summary report as CSV for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAssignmentSummaryPdf | Exports assignment summary report as PDF for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummary | Exports attendance summary report as Excel for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummaryCsv | Exports attendance summary report as CSV for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummaryPdf | Exports attendance summary report as PDF for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportGpaReport | Exports GPA report as Excel for selected scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummary | Exports payment summary report as Excel for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummaryCsv | Exports payment summary report as CSV for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummaryPdf | Exports payment summary report as PDF for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetAssignmentSummary | Returns assignment summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetAttendanceSummary | Returns attendance summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetCatalog | Returns report catalog entries available to the caller role. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetEnrollmentSummary | Returns enrollment summary report data with institution-aware filtering. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetFypStatusReport | Returns FYP status report data for scoped department and institution filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetGpaReport | Returns GPA report data for scoped program and department filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetInstitutionReportSections | Returns institution-aware report section visibility metadata. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetLowAttendanceWarning | Returns low-attendance warning report rows under scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetPaymentSummary | Exposes finance/admin/superadmin payment summary report endpoint with optional filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetQuizSummary | Returns quiz summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetResultSummary | Returns result summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetSemesterResults | Returns semester results report data for scoped semester filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetStudentTranscript | Returns transcript report data for a specific student profile. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportRepository.GetPaymentSummaryDataAsync | Materializes filtered payment summary rows (tenant/campus/course/semester/level aware) for finance reporting. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportRepository.cs |
| ReportService.ExportAssignmentSummaryCsvAsync | Builds CSV export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAssignmentSummaryExcelAsync | Builds Excel export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAssignmentSummaryPdfAsync | Builds PDF export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryCsvAsync | Builds CSV export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryExcelAsync | Builds Excel export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryPdfAsync | Builds PDF export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportGpaReportExcelAsync | Builds Excel export payload for GPA report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryCsvAsync | Builds CSV export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryExcelAsync | Builds Excel export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryPdfAsync | Builds PDF export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryCsvAsync | Builds CSV export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryExcelAsync | Builds Excel export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryPdfAsync | Builds PDF export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryCsvAsync | Builds CSV export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryExcelAsync | Builds Excel export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryPdfAsync | Builds PDF export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportTranscriptExcelAsync | Builds Excel export payload for student transcript report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetAssignmentSummaryAsync | Aggregates assignment report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetAttendanceSummaryAsync | Aggregates attendance report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetCatalogAsync | Builds role-filtered report catalog response for API consumers. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetEnrollmentSummaryAsync | Aggregates enrollment report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetFypStatusReportAsync | Aggregates FYP status report rows into response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetGpaReportAsync | Aggregates GPA report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetLowAttendanceWarningAsync | Aggregates low-attendance report rows into response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetPaymentSummaryAsync | Aggregates payment report rows into finance-ready totals and report response shape. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetQuizSummaryAsync | Aggregates quiz report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetResultSummaryAsync | Aggregates result report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetSemesterResultsAsync | Aggregates semester result rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetStudentTranscriptAsync | Builds transcript response payload for a target student profile. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| SettingsRepository.GetAllModuleRolesAsync(...) | Returns all module-role assignments with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetAllReportsAsync(...) | Returns report definitions with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetModuleRolesAsync(...) | Returns module-role assignments with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetSubMenusAsync(...) | Uses AsNoTracking for read-only submenu retrieval. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetTopLevelMenusAsync(...) | Uses AsNoTracking + AsSplitQuery for include-heavy top-level sidebar graph retrieval. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetVisibleMenusForRoleAsync(...) | Uses AsNoTracking + AsSplitQuery for include-heavy role-scoped sidebar visibility reads. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SidebarMenuService.GetTopLevelMenusAsync(...) | Adds short-TTL in-memory cache for top-level sidebar menu reads with versioned invalidation support. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| SidebarMenuService.GetVisibleForRoleAsync(...) | Adds short-TTL in-memory cache for role-scoped visible sidebar reads with versioned invalidation support. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| SidebarMenuService.InvalidateSidebarCache() | Bumps sidebar cache version after role/status mutations to force fresh reads on next request. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| StudyPlanService.AddCourseAsync | Adds a course to a study plan with validation for duplicates and plan state. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.AdvisePlanAsync | Applies advisor endorsement decision and notes for a study plan. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.CreatePlanAsync | Creates a new study plan for a student profile and planned semester. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.DeletePlanAsync | Deletes a study plan owned by the target student profile. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlanAsync | Returns a single study plan with associated planned courses. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlansAsync | Returns all study plans for a specific student profile. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlansByDepartmentAsync | Returns department-scoped study plans for advisor/admin workflows. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetRecommendationsAsync | Generates recommendation payload for missing requirements and sequencing guidance. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.RemoveCourseAsync | Removes a course from a study plan while preserving remaining entries. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StartupVisibilityReporter.DescribeDatabaseType | Classifies the resolved connection string into a safe database-type label for logging. | src/Tabsan.EduSphere.Application/Services/StartupVisibilityReporter.cs |
| StudentLifecycleRepository.ApplyPaymentAccessScope | Applies tenant/campus scope filtering to payment receipt lifecycle queries. | src/Tabsan.EduSphere.Infrastructure/Repositories/StudentLifecycleRepository.cs |
| StudentLifecycleRepository.ApplyStudentAccessScope | Applies tenant/campus scope filtering to student profile lifecycle queries. | src/Tabsan.EduSphere.Infrastructure/Repositories/StudentLifecycleRepository.cs |
| StudentLifecycleService.MapPaymentReceipt | Maps receipt state to output contract including PaidDate and UpdatedAt tracking fields. | src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs |
| StudentLifecycleService.UpdatePaymentReceiptAsync | Applies finance receipt edits, persists them, and notifies the student of the update. | src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs |
| TenantIsolationResolver.Resolve | Resolves tenant isolation mode, tenant code/name/domain/database, and tenant config path from config and environment variables. | src/Tabsan.EduSphere.Application/Services/TenantIsolationResolver.cs |
| TenantOperationsService.ReadSetting(all, rawKey, defaultValue) | Reads tenant-scoped values first and falls back to legacy unscoped keys for migration-safe compatibility. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TenantOperationsService.ScopedCacheKey(key) | Builds tenant-scoped distributed-cache keys to prevent cross-tenant cache collisions. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TenantOperationsService.ScopedSettingKey(key) | Builds tenant-scoped settings keys for onboarding/subscription/profile operations with default-tenant backward compatibility. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TimetableRepository.GetByDepartmentAsync(...) | Returns timetable lists with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetEntriesByCourseOfferingAsync(...) | Returns course-offering timetable entries with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetPublishedByDepartmentAsync(...) | Returns published timetable lists with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetTeacherEntriesAsync(...) | Returns teacher timetable entries with direct async EF execution instead of ContinueWith bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| UserImportController.ImportCsv | Accepts CSV uploads for bulk user import with optional strict mode and role-based access enforcement. | src/Tabsan.EduSphere.API/Controllers/UserImportController.cs |
| UserImportService.ImportFromCsvAsync | Parses CSV imports with role/identity validation, strict-mode rollback support, and additive mobile/campus column compatibility. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.IsValidMobileNumber | Validates optional mobile/phone values to accepted character set before user import persistence. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.ResolveCampusAssignmentsIndex | Resolves optional campus-assignment header aliases (CampusAssignments/CampusIds) for backward-compatible CSV parsing. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.ResolvePhoneNumberIndex | Resolves optional mobile/phone header aliases (MobileNumber/PhoneNumber) for backward-compatible CSV parsing. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.TryValidateCampusAssignments | Validates optional campus assignments as pipe-separated GUID values during CSV import. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| User.SetTenantCampus(tenantId, campusId) | Assigns or clears tenant/campus ownership for user-level scoping without breaking existing identity flows. | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.ValidateTenantCampusPair | Prevents invalid user state by requiring TenantId and CampusId to be set/cleared together. | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| UserRepository.ApplyTenantCampusScope | Applies tenant/campus query filtering for user reads with SuperAdmin bypass. | src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs |
| DegreeController.Download | Downloads generated degree document and applies .docx fallback when requested PDF is unavailable. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.DownloadDefaultTemplate | Streams default degree .docx template from the degree/transcript generation export service. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.Generate | Triggers degree document generation workflow for admin users. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.StudentDegree | Returns generated degree artifacts for the current student route /student/degree. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.ResolveCurrentUserId | Resolves current caller user-id from NameIdentifier/sub claims for student artifact filtering. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| TranscriptController.Download | Downloads generated transcript document and applies .docx fallback when requested PDF is unavailable. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.DownloadDefaultTemplate | Streams default transcript .docx template from the degree/transcript generation export service. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.Generate | Triggers transcript document generation workflow for admin users. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.StudentTranscript | Returns generated transcript artifacts for the current student route /student/transcript. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.ResolveCurrentUserId | Resolves current caller user-id from NameIdentifier/sub claims for student artifact filtering. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TemplateExportService.GetDegreeTemplateAsync | Generates default Degree Word template bytes with the degree/transcript placeholder contract. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService.GetTranscriptTemplateAsync | Generates default Transcript Word template bytes with the degree/transcript placeholder contract including {{COURSE_TABLE}}. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService.BuildTemplateDocument | Constructs in-memory .docx payload from template text lines using OpenXML. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| DegreeController.UploadTemplate | Accepts isolated degree template .docx uploads and persists K4 template metadata. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| AcademicDocumentTemplate.Create | Creates isolated storage metadata for degree/transcript templates. | src/Tabsan.EduSphere.Domain/Assignments/AcademicDocumentStorage.cs |
| DegreeDocumentRecord.Create | Creates a persisted record for generated degree artifacts. | src/Tabsan.EduSphere.Domain/Assignments/AcademicDocumentStorage.cs |
| TranscriptDocumentRecord.Create | Creates a persisted record for generated transcript artifacts. | src/Tabsan.EduSphere.Domain/Assignments/AcademicDocumentStorage.cs |
| TranscriptController.UploadTemplate | Accepts isolated transcript template .docx uploads and persists K4 template metadata. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| QRCodeService.GeneratePng | Produces QR PNG byte array from verification payload using QRCoder. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/QRCodeService.cs |
| QRCodeService.GenerateDataUrl | Produces Base64 QR data URL for lightweight UI rendering support. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/QRCodeService.cs |
| TwoFactorStateStore.DisableAsync | Clears the stored 2FA secret and disables 2FA for the current user. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.EnableAsync | Marks 2FA as enabled after the confirmation code has been validated. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.GetAsync | Returns the current protected 2FA snapshot for a user, or null if the user is missing. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.SaveSetupAsync | Stores an encrypted 2FA secret for an in-progress setup. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| IEduApiClient.BeginTwoFactorSetupAsync | Fetches the portal-side 2FA setup payload from the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DisableTwoFactorAsync | Sends a portal-side 2FA disable request to the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.VerifyTwoFactorLoginAsync | Sends a portal-side 2FA login challenge verification request to the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.VerifyTwoFactorSetupAsync | Sends a portal-side 2FA setup verification request to the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| DegreeController.EnsureDegreeTranscriptGenerationEnabledAsync | Checks the degree/transcript generation rollout flag before allowing degree controller actions. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| TranscriptController.EnsureDegreeTranscriptGenerationEnabledAsync | Checks the degree/transcript generation rollout flag before allowing transcript controller actions. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TemplateProcessorService.PopulateTemplate | Applies degree/transcript placeholder replacement and transcript table rendering to .docx template bytes. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceInMainBody | Replaces mapped placeholder tokens in main document body text nodes. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceInHeadersAndFooters | Replaces mapped placeholder tokens in header and footer parts. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceCourseTable | Replaces {{COURSE_TABLE}} marker with generated OpenXML table or empty-data text. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildCourseTable | Builds transcript course table with required degree/transcript columns. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildRow | Creates table row instances for course-table header and data rows. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildCell | Creates formatted OpenXML table cell content (with bold header support). | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| DocumentGenerationService.GenerateDegreeAsync | Orchestrates degree generation using export, processing, and QR services. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GenerateTranscriptAsync | Orchestrates transcript generation including course-table population. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GetAsync | Returns generated document metadata by document id, including database fallback. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.ListByStudentAsync | Lists generated artifacts for a specific student id with database-backed recovery. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GenerateInternalAsync | Performs shared generation pipeline, invokes optional PDF adapter, persists outputs, and registers metadata. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.PersistGeneratedDocumentAsync | Persists degree/transcript generation records into academic document storage tables. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GetFromDatabaseAsync | Retrieves a generated artifact from persisted academic document storage when it is not cached in memory. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.ListByStudentFromDatabaseAsync | Merges cached and persisted generated artifacts for a student. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DegreeGenerationRequest.ToPayload | Maps degree generation request data to the template payload shape with default serial/date assignment. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| TranscriptGenerationRequest.ToPayload | Maps transcript generation request data to the template payload shape with default serial/date assignment. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| IPdfConverterAdapter.TryConvertToPdfAsync | Defines optional PDF conversion adapter contract for generated degree/transcript documents. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| NoOpPdfConverterAdapter.TryConvertToPdfAsync | Default no-op adapter that returns null to preserve guaranteed .docx fallback behavior. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| TemplateUploadService.UploadDegreeTemplateAsync | Validates and stores isolated degree template uploads in the academic document storage layer. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.UploadTranscriptTemplateAsync | Validates and stores isolated transcript template uploads in the academic document storage layer. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.UploadAsync | Shared upload pipeline for .docx templates, storage persistence, and metadata creation. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.ValidateDocxAsync | Enforces .docx-only validation for isolated template uploads. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.BuildVersion | Builds additive template version metadata for uploaded templates. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| CertificateGenerationController.GetGraduatedStudents | Returns university-only graduated students filtered by tenant, campus, department, and course with role scope enforcement. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GenerateDegreeCertificate | Generates degree certificate document for a scoped graduated student (Admin/SuperAdmin only). | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GenerateTranscriptCertificate | Generates transcript document for a scoped graduated student (Admin/SuperAdmin only). | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetStudentDocuments | Returns generated document history for a scoped student with role-aware read access checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.Download | Downloads generated certificate/transcript artifact with pdf/docx selection and scope validation. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetStudentInScopeAsync | Enforces university/license/tenant/campus/department scope before management operations. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.HasStudentReadScopeAsync | Enforces scoped read permissions for Admin, Faculty, and Student document access. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.ResolveStudentNameAsync | Resolves student display name from user identity with registration fallback. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.BuildTranscriptRowsAsync(...) | Builds transcript course rows from enrollments/course offerings and supports class/semester filtered transcript generation. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| PortalController.GenerateCertificates | Renders Tenant/Campus/Department/Course-filtered Generate Certificates page with role-based manage mode. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.GenerateDegreeCertificate | Web action that triggers scoped degree generation and preserves current filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.GenerateTranscriptCertificate | Web action that triggers scoped transcript generation and preserves current filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadGeneratedCertificateDocument | Proxies generated document download/print for scoped portal users. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| IEduApiClient.GetGraduatedCertificateStudentsAsync | Client contract for graduated-student certificate listing with tenant/campus/department/course filters. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GenerateDegreeCertificateAsync | Client contract for scoped degree certificate generation. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GenerateTranscriptCertificateAsync | Client contract for scoped transcript generation. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DownloadGeneratedCertificateDocumentAsync | Client contract for generated document download/print retrieval. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetGraduatedCertificateStudentsAsync | Calls graduated-student listing endpoint with dynamic filter query composition. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GenerateDegreeCertificateAsync | Calls API endpoint to generate degree certificate for selected graduated student. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GenerateTranscriptCertificateAsync | Calls API endpoint to generate transcript for selected graduated student. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.DownloadGeneratedCertificateDocumentAsync | Downloads generated certificate/transcript files by document id and requested format. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| ProgramController.GetAll | Returns program list with tenant/campus scope resolution and role-aware scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.GetById | Returns a single program by id under validated tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.Create | Creates a program only when target department is inside resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.Update | Updates a program name under resolved tenant/campus scope and department ownership checks. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.Activate | Activates a program under resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.DeactivateAlias | Backward-compatible deactivate route alias for program status control under scope checks. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.ResolveEffectiveScope | Resolves requested tenant/campus scope against caller claims and superadmin rules. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.EnsureDepartmentIsInScopeAsync | Validates department belongs to current tenant/campus scope before program mutations. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ReportController.GetReportsStatus | Returns active/inactive report scope status for requested or claim-derived tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ActivateReports | Activates report center visibility for the resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.DeactivateReports | Deactivates report center visibility for the resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.IsReportsScopeActiveAsync | Evaluates effective report activation state from settings for tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| UserImportController.ResolveEffectiveScope | Resolves user-import tenant/campus scope from claims and requested query values. | src/Tabsan.EduSphere.API/Controllers/UserImportController.cs |
| IAcademicProgramRepository.SetActiveAsync | Contract for activating/deactivating program entities in scoped workflows. | src/Tabsan.EduSphere.Domain/Interfaces/IAcademicProgramRepository.cs |
| AcademicProgramRepository.SetActiveAsync | Persists active/inactive program state through repository layer. | src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicRepositories.cs |
| IEduApiClient.ActivateProgramAsync | Web client contract for activating scoped programs from portal workflows. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ActivateProgramAsync | Calls scoped activate program endpoint from web portal workflows. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetReportsScopeActiveAsync | Retrieves report scope activation status for current tenant/campus context. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ActivateReportsScopeAsync | Calls API to activate report scope for tenant/campus context. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.DeactivateReportsScopeAsync | Calls API to deactivate report scope for tenant/campus context. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| PortalController.Programs | Renders scoped programs page with tenant/campus filters and department context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CreateProgram | Creates scoped program from portal and preserves selected filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdateProgram | Updates scoped program name from portal and preserves selected filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.SetProgramActive | Toggles program active/inactive state from portal under scope-aware API calls. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.SetReportsActive | Toggles report scope active/inactive state from portal settings surface. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| CertificateGenerationController.GetAdditionalCertificates | Returns non-university additional certificate metadata for a scoped student with role-aware access checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.UploadAdditionalCertificate | Uploads additional student certificate files for school/college scope with admin/superadmin authorization and tenant/campus checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.DownloadAdditionalCertificate | Downloads previously uploaded additional student certificate files under scoped authorization checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetNonUniversityStudentForAdminManagementAsync | Validates school/college admin management scope for additional certificate upload operations. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.HasAdditionalCertificateReadScopeAsync | Enforces scoped read permissions for additional school/college certificates across Admin, Faculty, and Student users. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| PortalController.IsUniversityInstitutionType | Resolves institution type into university/non-university behavior for certificate workflow gating. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ResolvePeriodFilterLabel | Resolves dynamic period label (Class for university, Semester otherwise) for certificate filters. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ResolveCertificateInstitutionType | Resolves effective institution scope from selected department or identity for certificate page behavior. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UploadStudentAdditionalCertificate | Uploads school/college additional certificates from portal with preserved tenant/campus/department/course filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadStudentAdditionalCertificate | Downloads uploaded school/college additional certificates from portal. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| IEduApiClient.GetStudentAdditionalCertificatesAsync | Client contract for listing additional certificates uploaded for a student profile. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.UploadStudentAdditionalCertificateAsync | Client contract for uploading additional school/college certificate files per student profile. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DownloadStudentAdditionalCertificateAsync | Client contract for downloading uploaded additional certificate files. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetStudentAdditionalCertificatesAsync | Calls API endpoint to retrieve additional school/college certificate metadata per student profile. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.UploadStudentAdditionalCertificateAsync | Uploads additional school/college certificate files through multipart API endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.DownloadStudentAdditionalCertificateAsync | Downloads additional school/college certificate files by document id. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IGradebookRepository.GetInstitutionTypeForOfferingAsync | Resolves offering-specific institution mode so gradebook output switches between GPA/CGPA and percentage behavior. | src/Tabsan.EduSphere.Domain/Interfaces/IGradebookRepository.cs |
| GradebookRepository.GetInstitutionTypeForOfferingAsync | Reads institution type from the selected course offering in one scoped query. | src/Tabsan.EduSphere.Infrastructure/Repositories/GradebookRubricRepositories.cs |
| IResultRepository.GetActiveComponentRulesAsync(InstitutionType, ...) | Returns active result components for the requested institution scope. | src/Tabsan.EduSphere.Domain/Interfaces/IResultRepository.cs |
| ResultRepository.GetActiveComponentRulesAsync(InstitutionType, ...) | Applies institution filtering for active component rules and keeps legacy fallback behavior. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| GradebookService.GetGradebookAsync | Builds institution-aware aggregates: University weighted-total to GPA + CGPA, School/College percentage; includes fallback component derivation from existing result types when configured rules are missing. | src/Tabsan.EduSphere.Application/Assignments/GradebookService.cs |
| PortalController.IsApiConnectivityException | Classifies HTTP/socket/timeout failures so menu guard and Students view handle temporary API outage gracefully. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| EduApiClient.TryExtractApiErrorMessage | Extracts detail/message/title from JSON API error payloads so portal alerts show readable text. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.ForceChangePasswordAsync | Web client contract for forced password change | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ForceChangePasswordAsync | Calls forced password change API endpoint | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| PortalController.ForceChangePassword | Portal-side forced password change action with old+new password flow | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ValidateResultWriteScopeAsync | Enforces required-filter and scope enforcement before result write actions | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.RenderResultsAsync | Resolves and emits dynamic PeriodLabel for Results UI rendering | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadResultCsvTemplate | Generates and downloads Enter Results CSV template with example rows | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ImportResultCsv | Validates and imports CSV result rows, skips template example rows | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadResultImportReport | Serves one-time downloadable CSV import result reports using report tokens | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.PublishAllResults | Publishes all draft results for an offering with Admin/SuperAdmin gating | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CorrectResult | Corrects published result marks with correction reason audit trail | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ResultCalculationCourseFilterData | Loads course-type filter data for result calculation settings page | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.BuildLicensedInstitutionOptions | Builds institution filter options from license capability matrix | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ResolveLicensedInstitutionSelection | Auto-selects single licensed institute or exposes multi-select dropdown | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.BuildLicensedPaymentsInstitutionOptions | Builds payment-specific institution option mapping with scoped labels | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.NormalizeAdvisorStatusValue | Normalizes advisor status enum values for study plan display | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.NormalizeCourseTypeValue | Normalizes course type enum values for study plan display | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.LoadPaymentStudentsForScopeAsync | Applies institution-type constrained loading for payment student dropdowns | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ParsePaymentImportCsvAsync | Parses payment CSV with multiple date format support and validation guidance | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| StudentLifecycleController.GraduateStudent | API endpoint for single student graduation | src/Tabsan.EduSphere.API/Controllers/StudentLifecycleController.cs |
| StudentLifecycleController.GraduateStudentsBatch | API endpoint for batch student graduation | src/Tabsan.EduSphere.API/Controllers/StudentLifecycleController.cs |
| StudentLifecycleService.GraduateStudentAsync | Validates and executes single student graduation with eligibility checks | src/Tabsan.EduSphere.Application/Academic/StudentLifecycleService.cs |
| StudentLifecycleService.GraduateStudentsBatchAsync | Validates and executes batch student graduation with eligibility checks | src/Tabsan.EduSphere.Application/Academic/StudentLifecycleService.cs |
| DegreeAuditController.GetEligibilityList | Returns graduation eligibility list for scoped department/program | src/Tabsan.EduSphere.API/Controllers/DegreeAuditController.cs |
| DegreeAuditController.GetAllRules | Returns all degree audit rules | src/Tabsan.EduSphere.API/Controllers/DegreeAuditController.cs |
| DegreeAuditController.GetRuleByProgram | Returns degree rule for a specific academic program | src/Tabsan.EduSphere.API/Controllers/DegreeAuditController.cs |
| DegreeAuditService.GetEligibilityListAsync | Aggregates graduation eligibility data from enrollment/result records | src/Tabsan.EduSphere.Application/Academic/DegreeAuditService.cs |
| DegreeAuditService.GetAllRulesAsync | Returns all degree audit program rules | src/Tabsan.EduSphere.Application/Academic/DegreeAuditService.cs |
| DegreeAuditService.GetRuleByProgramAsync | Returns degree rule for a specific program | src/Tabsan.EduSphere.Application/Academic/DegreeAuditService.cs |
| DegreeAuditRepository.GetEarnedCreditsAsync | Aggregates earned credits with tenant/campus scope fallback chain | src/Tabsan.EduSphere.Infrastructure/Repositories/DegreeAuditRepository.cs |
| DepartmentController.GetAll | Returns departments with optional institutionType filter and scope enforcement | src/Tabsan.EduSphere.API/Controllers/DepartmentController.cs |
| PrerequisiteRepository.GetByCourseIdAsync | Returns prerequisites with eager-loaded Course navigation for full payload | src/Tabsan.EduSphere.Infrastructure/Repositories/PrerequisiteRepository.cs |
| verify-degree-rules-access.ps1 functions | PowerShell verification automation for degree-rule access validation | Scripts/verify-degree-rules-access.ps1 |
| verify-degree-rules-access-local-bootstrap.ps1 functions | Local bootstrap helpers for degree-rule verification scripts | Scripts/verify-degree-rules-access-local-bootstrap.ps1 |
| ReportService.ExportGpaReportCsvAsync | GPA report CSV export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportGpaReportPdfAsync | GPA report PDF export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportEnrollmentSummaryExcelAsync | Enrollment summary Excel export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportEnrollmentSummaryCsvAsync | Enrollment summary CSV export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportEnrollmentSummaryPdfAsync | Enrollment summary PDF export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportSemesterResultsExcelAsync | Semester results Excel export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportSemesterResultsCsvAsync | Semester results CSV export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportSemesterResultsPdfAsync | Semester results PDF export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportTranscriptCsvAsync | Student transcript CSV export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportTranscriptPdfAsync | Student transcript PDF export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportLowAttendanceExcelAsync | Low attendance warning Excel export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportLowAttendanceCsvAsync | Low attendance warning CSV export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportLowAttendancePdfAsync | Low attendance warning PDF export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportFypStatusExcelAsync | FYP status report Excel export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportFypStatusCsvAsync | FYP status report CSV export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportFypStatusPdfAsync | FYP status report PDF export | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportController.ExportGpaReportCsv/Pdf | API: GET /api/v1/reports/gpa-report/export/{csv,pdf} | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportEnrollmentSummary* | API: GET /api/v1/reports/enrollment-summary/export{/,/csv,/pdf} | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportSemesterResults* | API: GET /api/v1/reports/semester-results/export{/,/csv,/pdf} | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportStudentTranscriptCsv/Pdf | API: GET /api/v1/reports/student-transcript/export/{csv,pdf} | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportLowAttendance* | API: GET /api/v1/reports/low-attendance/export{/,/csv,/pdf} | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportFypStatus* | API: GET /api/v1/reports/fyp-status/export{/,/csv,/pdf} | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| PortalController.ResolveLicensedInstitutionTypesAsync | Resolves licensed institution types from portal capability matrix for catalog filtering | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| FypProject.FypGradePoint | Final grade point awarded upon FYP completion (decimal 5,2) | src/Tabsan.EduSphere.Domain/Fyp/FypProject.cs |
| FypProject.FypMarks | Final marks awarded upon FYP completion (decimal 7,2) | src/Tabsan.EduSphere.Domain/Fyp/FypProject.cs |
| FypProject.FypMaxMarks | Maximum possible marks for FYP (decimal 7,2) | src/Tabsan.EduSphere.Domain/Fyp/FypProject.cs |
| CertificateGenerationController.GenerateAdditionalCertificate | Generates Completion Certificate or Report Card for school/college students with institution-type-aware scoring | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.IsCompletionEligible | Validates completion eligibility: School≥Class10, College requires Class11+12 results | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.HasCompletedBothClassesAsync | Checks College student has published results in BOTH Class 11 and Class 12 | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.SanitizeFileName | Sanitizes registration number into safe file name characters | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.BuildAdditionalTemplateDocument | Builds professional DOCX templates for Completion Certificate and Report Card | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.BuildCompletionTemplate | Professional Completion Certificate layout: double border, gold badge, centered name, signatures | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.BuildReportCardTemplate | Professional Report Card layout: navy header, student info grid, summary cards, course table | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.DownloadDefaultAdditionalTemplate | Downloads default Completion/ReportCard .docx templates with professional formatting | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.UploadAdditionalTemplate | Uploads custom Completion/ReportCard .docx templates for school/college | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetAdditionalCertificates | Lists additional certificates (Completion/ReportCard) for a scoped student | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.UploadAdditionalCertificate | Uploads additional certificate files for school/college students | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| TemplateExportService.GetDegreeTemplateAsync | Builds professional Degree DOCX with double navy border, gold accents, signature block | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService.GetTranscriptTemplateAsync | Builds professional Transcript DOCX with dark navy header, summary cards, course table | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService (helpers) | Reusable OpenXML helpers: page borders, centered/left paragraphs, badges, info rows, signatures, summary cells | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateProcessorService.PopulateTemplate | Placeholder replacement with professional table formatting (navy headers, alternating rows, semester groups) | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildCourseTable | Professional transcript course table: Gold-accent borders, dark navy column headers, alternating row colors, semester headers, CGPA totals row | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| DocumentGenerationService.SanitizeFileName | Sanitizes registration number for safe file naming (RegNo-Type.docx pattern) | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| LibreOfficePdfConverterAdapter | Headless LibreOffice DOCX→PDF converter with auto-detection and graceful fallback | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| NoOpPdfConverterAdapter | Safe no-op PDF adapter fallback when LibreOffice is unavailable | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| FypProject.SetFinalResult | Sets result with optional grade point, marks, maxMarks for completed FYP | src/Tabsan.EduSphere.Domain/Fyp/FypProject.cs |
| ReportRepository.GetTranscriptDataAsync (updated) | Now includes completed FYP projects with grades in transcript output | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportRepository.cs |
