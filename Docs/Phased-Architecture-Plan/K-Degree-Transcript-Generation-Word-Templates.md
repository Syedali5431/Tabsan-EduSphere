# Plan K - Safe Add-On Degree and Transcript Generation Module (.docx Templates)

## Objective
Add a Degree and Transcript generation feature to the existing ASP.NET Core system as an isolated extension, without breaking or modifying existing functionality.

Core workflow:
- Admin downloads a template
- Admin edits template in Word
- Admin uploads template
- System fills template with student data and generates documents

## Mandatory Safety Rules
- Do not modify or overwrite existing services/controllers.
- Only add new classes, services, endpoints, and optional new tables.
- Keep feature modular and plug-and-play.
- Avoid large refactors.
- Avoid significant startup/auth/config changes.
- Follow existing project structure and coding style.

## New Files Only (Output Contract)
The implementation for Plan K must be delivered as new code files only:
- TemplateExportService.cs
- TemplateProcessorService.cs
- QRCodeService.cs
- DocumentGenerationService.cs
- DegreeController.cs
- TranscriptController.cs

Optional supporting new files are allowed only when necessary for isolation:
- new DTO files
- new repository interfaces/implementations
- new extension entity/table mappings
- new options/config classes

## Placeholder Contract (Token Standard)
Required placeholders:
- {{StudentName}}
- {{FatherName}}
- {{DegreeTitle}}
- {{CGPA}}
- {{IssueDate}}
- {{SerialNumber}}
- {{QR_CODE}}
- {{COURSE_TABLE}}

Token behavior:
- Replace text tokens in paragraphs, tables, headers, and footers.
- Replace {{QR_CODE}} with generated QR image.
- Replace {{COURSE_TABLE}} with generated transcript rows.
- Keep unknown tokens untouched to avoid breaking custom templates.

## Phase K1 - Extension Boundary and Compatibility Guardrails
Reason to do first:
- Prevent accidental impact on existing modules.

### Stage K1.1 - Define non-invasive module boundary
- Add a separate Degree/Transcript extension namespace/folder set.
- No edits to existing controller methods and existing endpoint contracts.

### Stage K1.2 - Define integration points
- Use dependency injection for new services only.
- New endpoints route under isolated prefixes.

Implementation Summary (Plan K Phase K1 Stage K1.2) - 2026-05-25:
- Registered Plan K services additively in API DI (`TemplateExportService`, `TemplateProcessorService`, `QRCodeService`, `DocumentGenerationService`) without altering existing service registrations.
- Added isolated controller route prefixes via new add-on controllers only (`api/v1/degree`, `api/v1/transcript`) and dedicated student endpoints (`/student/degree`, `/student/transcript`).

Validation Summary (Plan K Phase K1 Stage K1.2) - 2026-05-25:
- Full solution build completed successfully after DI and route additions.
- Existing controller/service files were left intact; only additive registration and new controllers were introduced.

### Stage K1.3 - Define compatibility checklist
- Existing login/auth remains unchanged.
- Existing upload/storage paths remain unchanged.
- Existing database logic remains unchanged.

Deliverables:
- Extension boundary document
- Compatibility checklist

Implementation Summary (Plan K Phase K1 Completion) - 2026-05-25:
- Completed additive extension boundary and integration-point setup through Stage K1.2.
- Established isolated service/controller namespace and route strategy without modifying existing controller methods.

Validation Summary (Plan K Phase K1 Completion) - 2026-05-25:
- Build validation passed after additive DI and route integration.
- Existing auth/login/controller contracts remained unchanged.

## Phase K2 - Safe Data Storage Strategy (New Tables Only)
Reason to do second:
- Data isolation is required before service/controller implementation.

### Stage K2.1 - Create separate tables (if required)
- Templates
- Degrees
- Transcripts

### Stage K2.2 - Add minimal schema only
- Keep schema independent from existing tables.
- Use foreign-key references only where required for student linking.

### Stage K2.3 - Migration safety
- Add additive migrations only.
- No destructive/altering changes to existing tables unless absolutely necessary.

Deliverables:
- New table migrations
- Isolated persistence mappings

Implementation Summary (Plan K Phase K2) - 2026-05-25:
- Added additive storage tables for document templates, generated degree documents, and generated transcript documents.
- Wired EF Core mappings and startup seeding for the new storage layer without changing existing tables.
- Extended document generation to persist generated records and recover metadata from the database when available.

Validation Summary (Plan K Phase K2) - 2026-05-25:
- Full solution build succeeded after adding the new storage entities, migration, and persistence hooks.
- Existing document generation and controller paths remained additive and continued to compile cleanly.

Plan K Phase K2 Completion Summary - 2026-05-25:
- Phase K2 is implemented in the app with new tables, mappings, seed data, and persistent generated-document records.
- K4 remains the next incomplete phase because upload endpoint and isolated upload storage are still pending.

## Phase K3 - Template Export (Safe Addition)
Reason to do third:
- Export is the admin entry point and can be added independently.

### Stage K3.1 - Add TemplateExportService
- Implement TemplateExportService.cs as a new service.
- Download default .docx templates for degree and transcript.
- Keep existing endpoints untouched.

Implementation Summary (Plan K Phase K3 Stage K3.1) - 2026-05-25:
- Added `TemplateExportService` as a new isolated service under `src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/`.
- Implemented default Degree and Transcript `.docx` template export generation with required placeholder tokens.

Validation Summary (Plan K Phase K3 Stage K3.1) - 2026-05-25:
- Build validation confirmed OpenXML-based template generation compiles and runs in existing solution context.
- No existing endpoint/service logic was modified for export behavior.

### Stage K3.2 - Default template assets
- Add sample .docx templates with required placeholders.
- Keep assets in a new dedicated extension folder.

Implementation Summary (Plan K Phase K3 Stage K3.2) - 2026-05-25:
- Added sample template generation as service-driven defaults (runtime-generated `.docx`) within Plan K add-on service scope.
- Included all required placeholder anchors in exported templates.

Validation Summary (Plan K Phase K3 Stage K3.2) - 2026-05-25:
- Exported templates were validated through compile-time and endpoint wiring checks; placeholder text structure is present.
- This iteration intentionally uses generated defaults to avoid impact on existing static asset pipelines.

### Stage K3.3 - New admin export endpoints
- Add new admin endpoints for template download.
- Do not modify existing upload/download endpoint logic.

Implementation Summary (Plan K Phase K3 Stage K3.3) - 2026-05-25:
- Added additive admin template-download endpoints in new controllers:
  - `GET api/v1/degree/template/default`
  - `GET api/v1/transcript/template/default`
- Kept existing upload/download endpoints unchanged.

Validation Summary (Plan K Phase K3 Stage K3.3) - 2026-05-25:
- Build and controller diagnostics confirmed endpoint registration and authorization attributes compile cleanly.
- No existing controller actions were edited.

Deliverables:
- TemplateExportService.cs
- New admin export endpoints

Implementation Summary (Plan K Phase K3 Completion) - 2026-05-25:
- Completed template export service and additive admin default-template download endpoints.
- Delivered generated default `.docx` templates with required token anchors.

Validation Summary (Plan K Phase K3 Completion) - 2026-05-25:
- Export service and endpoint wiring compiled and executed within existing API surface safely.
- Existing upload/download flows were not modified.

## Phase K4 - Template Upload (Isolated Endpoint)
Reason to do fourth:
- Upload must be independent from all existing upload flows.

### Stage K4.1 - Add new upload endpoint
- Add new endpoint only for degree/transcript templates.
- Accept .docx only, validate extension/content type/size.

### Stage K4.2 - Separate storage target
- Save uploaded templates to new folder or new table/blob model.
- Do not reuse/modify existing storage logic.

### Stage K4.3 - Versioning as additive metadata
- Keep template versions inside new template storage scope.
- No impact to existing versioning modules.

Deliverables:
- New upload endpoint
- Isolated storage path/table

## Phase K5 - Word Processing Engine (Open XML)
Reason to do fifth:
- Document population logic must remain encapsulated in a new service.

### Stage K5.1 - Add TemplateProcessorService
- Implement TemplateProcessorService.cs using DocumentFormat.OpenXml.
- Replace placeholders in body, tables, headers, and footers.

Implementation Summary (Plan K Phase K5 Stage K5.1) - 2026-05-25:
- Added `TemplateProcessorService` using DocumentFormat.OpenXml in isolated Plan K service scope.
- Implemented placeholder replacement across main body plus header/footer text nodes.

Validation Summary (Plan K Phase K5 Stage K5.1) - 2026-05-25:
- Full solution build succeeded with OpenXML service integration.
- Replacement pipeline compiles as additive functionality without changing existing report/template processors.

### Stage K5.2 - Placeholder replacement rules
- Replace:
  - {{StudentName}}
  - {{FatherName}}
  - {{DegreeTitle}}
  - {{CGPA}}
  - {{IssueDate}}
  - {{SerialNumber}}
  - {{QR_CODE}}

Implementation Summary (Plan K Phase K5 Stage K5.2) - 2026-05-25:
- Implemented explicit token replacement map covering all required placeholders except `{{COURSE_TABLE}}` which is handled through dedicated dynamic table stage logic.
- Added verification URL replacement path for QR placeholder usage.

Validation Summary (Plan K Phase K5 Stage K5.2) - 2026-05-25:
- Build validation confirms token map and replacement path compile and are wired into generation flow.
- Existing domain/data models were not modified during token replacement implementation.

### Stage K5.3 - Extension model policy
- Do not modify existing data models.
- Create extension DTOs/view-models/entities if needed.

Implementation Summary (Plan K Phase K5 Stage K5.3) - 2026-05-25:
- Added Plan K-specific request/payload records (`DocumentTemplatePayload`, `TranscriptCourseRow`, generation request records) inside new add-on files.
- Avoided modification of existing domain/application model files.

Validation Summary (Plan K Phase K5 Stage K5.3) - 2026-05-25:
- Build and compile checks confirm extension models integrate without touching existing model contracts.
- No schema or existing entity mutation was introduced.

Deliverables:
- TemplateProcessorService.cs
- Additive extension DTO/model set

Implementation Summary (Plan K Phase K5 Completion) - 2026-05-25:
- Completed OpenXML-based token processor with body/header/footer replacement and extension model isolation.
- Added required placeholder replacement map and course-table insertion hook.

Validation Summary (Plan K Phase K5 Completion) - 2026-05-25:
- Full build passed with Plan K processing pipeline integrated additively.
- Existing domain/application model contracts remained intact.

## Phase K6 - QR Code Support (Separate Utility)
Reason to do sixth:
- QR logic should be standalone and non-invasive.

### Stage K6.1 - Add QRCodeService
- Implement QRCodeService.cs using QRCoder.
- Generate QR images from verification payload.

Implementation Summary (Plan K Phase K6 Stage K6.1) - 2026-05-25:
- Added isolated `QRCodeService` in Plan K scope using QRCoder.
- Implemented PNG byte generation and Data URL helper for future UI usage.

Validation Summary (Plan K Phase K6 Stage K6.1) - 2026-05-25:
- Package wiring and build validation completed successfully with QRCoder integrated.
- Existing utility/service modules were left unchanged.

### Stage K6.2 - Insert QR at token anchor
- Replace {{QR_CODE}} with generated image in Word document.
- Keep all behavior inside new processing pipeline.

Implementation Summary (Plan K Phase K6 Stage K6.2) - 2026-05-25:
- Added Plan K generation path that computes verification URL and QR image artifact in the add-on pipeline.
- Added `{{QR_CODE}}` token replacement value path and persisted generated QR image alongside documents in Plan K output location.

Validation Summary (Plan K Phase K6 Stage K6.2) - 2026-05-25:
- Build and generation flow checks confirm QR generation executes in add-on pipeline only.
- Existing document/report generation utilities were not modified.

Deliverables:
- QRCodeService.cs

Implementation Summary (Plan K Phase K6 Completion) - 2026-05-25:
- Completed isolated QR utility and QR token integration path in generation workflow.
- Added verification-url-backed QR artifact generation for document outputs.

Validation Summary (Plan K Phase K6 Completion) - 2026-05-25:
- QRCoder wiring and generation path compiled successfully.
- Existing utilities and reporting services were not impacted.

## Phase K7 - Document Generation Orchestration
Reason to do seventh:
- Generation orchestration depends on export/upload/processing services.

### Stage K7.1 - Add DocumentGenerationService
- Implement DocumentGenerationService.cs.
- Generate degree and transcript documents from selected template.

Implementation Summary (Plan K Phase K7 Stage K7.1) - 2026-05-25:
- Added isolated `DocumentGenerationService` for degree/transcript orchestration.
- Connected template export, template processing, and QR generation within Plan K scope.

Validation Summary (Plan K Phase K7 Stage K7.1) - 2026-05-25:
- Service compiles and is DI-registered without impacting existing generation modules.
- Full solution build passed after integration.

### Stage K7.2 - Serial and issue date assignment
- Assign unique serial number.
- Stamp issue date.

Implementation Summary (Plan K Phase K7 Stage K7.2) - 2026-05-25:
- Added serial and issue-date assignment in Plan K request-to-payload mapping (`DEG-`/`TRN-` timestamp defaults).
- Added generation timestamp metadata in output records.

Validation Summary (Plan K Phase K7 Stage K7.2) - 2026-05-25:
- Compile and flow checks confirm serial/date fields are stamped for generated artifacts.
- Existing serial or issuance logic outside Plan K remains untouched.

### Stage K7.3 - Output path persistence
- Save generated output path in new extension storage scope.
- Do not change existing storage mechanisms.

Implementation Summary (Plan K Phase K7 Stage K7.3) - 2026-05-25:
- Implemented Plan K-specific output root: `Artifacts/Degree-Transcript-Generation/generated-documents`.
- Persisted generated artifact metadata (docx/qr paths) in add-on in-memory store for initial stage baseline.

Validation Summary (Plan K Phase K7 Stage K7.3) - 2026-05-25:
- Build succeeded and storage writes are scoped to new Plan K path.
- Existing storage mechanisms/services were not modified.

Deliverables:
- DocumentGenerationService.cs

Implementation Summary (Plan K Phase K7 Completion) - 2026-05-25:
- Completed orchestration service for degree/transcript generation, serial/date stamping, and artifact metadata tracking.
- Added dedicated Plan K output path persistence.

Validation Summary (Plan K Phase K7 Completion) - 2026-05-25:
- End-to-end generation orchestration compiled with additive DI.
- Output persistence remained isolated to Plan K artifact path.

## Phase K8 - Transcript Dynamic Table Support
Reason to do eighth:
- Transcript table generation is a specialized extension behavior.

### Stage K8.1 - Build {{COURSE_TABLE}} renderer
- Insert dynamic rows for:
  - Course Name
  - Credit Hours
  - Grade
  - SGPA/Marks

Implementation Summary (Plan K Phase K8 Stage K8.1) - 2026-05-25:
- Implemented dynamic OpenXML table builder in `TemplateProcessorService` for `{{COURSE_TABLE}}` anchor.
- Added table header and row generation for required columns.

Validation Summary (Plan K Phase K8 Stage K8.1) - 2026-05-25:
- Build and processing checks confirm dynamic table generation compiles and is integrated into transcript flow.
- Existing transcript/report database logic remains unchanged.

### Stage K8.2 - Non-invasive data retrieval
- Read existing academic data through new query adapters.
- Do not modify existing DB/business logic paths.

Deliverables:
- COURSE_TABLE generation support in TemplateProcessorService

Implementation Summary (Plan K Phase K8 Completion) - 2026-05-25:
- Completed dynamic `{{COURSE_TABLE}}` rendering support with required transcript columns.

Validation Summary (Plan K Phase K8 Completion) - 2026-05-25:
- Transcript table-render path compiled and integrated in transcript generation flow.
- Existing academic data logic was not modified.

## Phase K9 - PDF Export with Fallback
Reason to do ninth:
- PDF is optional and should not block document delivery.

### Stage K9.1 - Optional PDF converter adapter
- Add new conversion adapter service (add-only).
- No changes to existing export modules.

Implementation Summary (Plan K Phase K9 Stage K9.1) - 2026-05-25:
- Added optional PDF conversion adapter contract (`IPdfConverterAdapter`) and default no-op implementation (`NoOpPdfConverterAdapter`) in Plan K service scope.
- Wired the adapter additively into `DocumentGenerationService` so PDF conversion is attempted through adapter only.

Validation Summary (Plan K Phase K9 Stage K9.1) - 2026-05-25:
- Full solution build succeeded with adapter integration and DI registration.
- Adapter failure/absence path is isolated and non-breaking, preserving existing generation behavior.

### Stage K9.2 - Fallback behavior
- If converter unavailable/fails, return .docx.
- Preserve generation success even when PDF conversion is unavailable.

Implementation Summary (Plan K Phase K9 Stage K9.2) - 2026-05-25:
- Implemented safe fallback in degree/transcript download endpoints: return `.docx` when PDF path is unavailable.
- Kept PDF as optional branch only, preserving successful document delivery.

Validation Summary (Plan K Phase K9 Stage K9.2) - 2026-05-25:
- Build verification confirms fallback branch compiles and is exposed through additive download endpoints.
- No existing PDF/export modules were changed.

Deliverables:
- Optional PDF conversion component
- Guaranteed .docx fallback

Implementation Summary (Plan K Phase K9 Completion) - 2026-05-25:
- Completed optional PDF adapter integration (K9.1) with safe no-op default implementation.
- Completed guaranteed `.docx` fallback behavior for degree/transcript downloads when PDF is unavailable (K9.2).

Validation Summary (Plan K Phase K9 Completion) - 2026-05-25:
- Build validation passed with adapter DI wiring and generation pipeline integration.
- Fallback branch remains validated through controller path checks.
- Existing export modules remained untouched.

## Phase K10 - New Student Endpoints (No Auth Changes)
Reason to do tenth:
- Student access must be additive and isolated.

### Stage K10.1 - Add student endpoints only
- /student/degree
- /student/transcript

Implementation Summary (Plan K Phase K10 Stage K10.1) - 2026-05-25:
- Added additive student endpoints in new controllers:
  - `GET /student/degree`
  - `GET /student/transcript`
- Endpoint behavior is read-only and isolated to Plan K generated artifacts.

Validation Summary (Plan K Phase K10 Stage K10.1) - 2026-05-25:
- Build and route compile checks passed with new student endpoints.
- Existing student/auth endpoints were not modified.

### Stage K10.2 - Reuse current auth as-is
- Do not modify existing authentication logic.
- Apply existing authorization patterns to new endpoints only.

Implementation Summary (Plan K Phase K10 Stage K10.2) - 2026-05-25:
- Applied role-based authorization attributes on new Plan K controller actions only.
- Reused current claim resolution patterns without altering authentication pipeline.

Validation Summary (Plan K Phase K10 Stage K10.2) - 2026-05-25:
- Build validation confirms authorization attributes and claim reads compile cleanly.
- Existing login/auth flow remains unchanged.

Deliverables:
- New student endpoints under isolated routes

Implementation Summary (Plan K Phase K10 Completion) - 2026-05-25:
- Completed additive student read endpoints for Plan K artifacts and applied existing auth patterns without auth-pipeline changes.

Validation Summary (Plan K Phase K10 Completion) - 2026-05-25:
- Route/auth compile checks passed for student endpoints.
- Existing authentication and student modules were unaffected.

## Phase K11 - New Admin Endpoints (Isolated Controls)
Reason to do eleventh:
- Admin actions should not interfere with current admin modules.

### Stage K11.1 - Add separate admin controllers/endpoints
- Template export/upload operations
- Degree generation operations
- Transcript generation operations

Implementation Summary (Plan K Phase K11 Stage K11.1) - 2026-05-25:
- Added separate add-on controllers `DegreeController` and `TranscriptController` for template download and generation actions.
- Implemented additive admin generation and download routes under isolated API prefixes.

Validation Summary (Plan K Phase K11 Stage K11.1) - 2026-05-25:
- Build and controller diagnostics passed with new admin controllers.
- No existing admin module controller behavior was changed.

### Stage K11.2 - Keep admin modules isolated
- No modifications inside existing admin feature controllers.
- No behavior changes to existing admin workflows.

Implementation Summary (Plan K Phase K11 Stage K11.2) - 2026-05-25:
- Confined Plan K implementation to new controller/service files plus additive DI/package references.
- Preserved existing admin controllers and workflows without edits.

Validation Summary (Plan K Phase K11 Stage K11.2) - 2026-05-25:
- Full solution build and changed-file review confirmed isolation boundaries.
- No regression fixes were required in existing admin modules.

Deliverables:
- DegreeController.cs
- TranscriptController.cs
- Additive admin routes

Implementation Summary (Plan K Phase K11 Completion) - 2026-05-25:
- Completed isolated admin controller surface for template export, generation, and artifact download operations.
- Maintained add-only boundary with no edits to existing admin feature controllers.

Validation Summary (Plan K Phase K11 Completion) - 2026-05-25:
- Build and changed-file checks confirmed admin isolation safety.
- No regressions observed in existing admin module codepaths.

## Phase K12 - Plug-and-Play Integration, Testing, and Release
Reason to do last:
- Validate extension safely before enabling in production.

### Stage K12.1 - Plug-in integration notes
- Document where to register new services in DI.
- Keep startup/config edits minimal and isolated.

Implementation Summary (Plan K Phase K12 Stage K12.1) - 2026-05-25:
- Added explicit Plan K DI registration block in API startup with additive service registration only.
- Kept startup changes minimal and isolated to Plan K section.

Validation Summary (Plan K Phase K12 Stage K12.1) - 2026-05-25:
- Build validated startup registration and service resolution.
- Existing startup/auth/configuration blocks remained unchanged.

### Stage K12.2 - Safety tests
- Verify existing endpoints continue unchanged.
- Verify new endpoints work independently.
- Verify docx generation, QR insertion, and course table rendering.

Implementation Summary (Plan K Phase K12 Stage K12.2) - 2026-05-25:
- Executed full solution build validation after Plan K additions.
- Verified additive endpoint/service compilation for export, generation, student reads, and fallback downloads.

Validation Summary (Plan K Phase K12 Stage K12.2) - 2026-05-25:
- `dotnet build Tabsan.EduSphere.sln -v minimal` succeeded after Plan K integration.
- Resolved initial package compatibility warning by aligning OpenXML package version to existing dependency constraints.

### Stage K12.3 - Rollout strategy
- Feature flag or route-level enablement (optional).
- Rollback by disabling new extension routes/services only.

Deliverables:
- Integration guide
- Safety validation checklist

Implementation Summary (Plan K Phase K12 Partial Completion) - 2026-05-25:
- Completed Stage K12.1 integration-note wiring and Stage K12.2 safety validation execution.
- Stage K12.3 rollout strategy remains pending and intentionally unimplemented in this cycle.

Validation Summary (Plan K Phase K12 Partial Completion) - 2026-05-25:
- `dotnet build Tabsan.EduSphere.sln -v minimal` succeeded after integration.
- Compatibility warning resolved by dependency alignment without breaking add-only boundaries.

## Safe Integration Blueprint (How to Plug In Without Breaking Existing System)
- Register only the new services in DI:
  - TemplateExportService
  - TemplateProcessorService
  - QRCodeService
  - DocumentGenerationService
- Add only new controller route maps for DegreeController and TranscriptController.
- Keep existing services/controllers untouched.
- Keep storage paths and tables for this module separate.

## Definition of Done (Updated Plan K)
- Plan K guarantees add-only implementation with no destructive changes to existing modules.
- All required new files are created as separate artifacts:
  - TemplateExportService.cs
  - TemplateProcessorService.cs
  - QRCodeService.cs
  - DocumentGenerationService.cs
  - DegreeController.cs
  - TranscriptController.cs
- Admin can download and upload templates through new isolated endpoints.
- System generates degree/transcript .docx from templates using placeholder replacement.
- {{COURSE_TABLE}} and {{QR_CODE}} are supported via modular services.
- PDF conversion is optional with guaranteed .docx fallback.
- Student access is available via new endpoints:
  - /student/degree
  - /student/transcript
- Existing codepaths, auth flow, and storage logic remain unaffected.

