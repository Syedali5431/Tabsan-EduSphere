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

### Stage K1.3 - Define compatibility checklist
- Existing login/auth remains unchanged.
- Existing upload/storage paths remain unchanged.
- Existing database logic remains unchanged.

Deliverables:
- Extension boundary document
- Compatibility checklist

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

## Phase K3 - Template Export (Safe Addition)
Reason to do third:
- Export is the admin entry point and can be added independently.

### Stage K3.1 - Add TemplateExportService
- Implement TemplateExportService.cs as a new service.
- Download default .docx templates for degree and transcript.
- Keep existing endpoints untouched.

### Stage K3.2 - Default template assets
- Add sample .docx templates with required placeholders.
- Keep assets in a new dedicated extension folder.

### Stage K3.3 - New admin export endpoints
- Add new admin endpoints for template download.
- Do not modify existing upload/download endpoint logic.

Deliverables:
- TemplateExportService.cs
- New admin export endpoints

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

### Stage K5.2 - Placeholder replacement rules
- Replace:
  - {{StudentName}}
  - {{FatherName}}
  - {{DegreeTitle}}
  - {{CGPA}}
  - {{IssueDate}}
  - {{SerialNumber}}
  - {{QR_CODE}}

### Stage K5.3 - Extension model policy
- Do not modify existing data models.
- Create extension DTOs/view-models/entities if needed.

Deliverables:
- TemplateProcessorService.cs
- Additive extension DTO/model set

## Phase K6 - QR Code Support (Separate Utility)
Reason to do sixth:
- QR logic should be standalone and non-invasive.

### Stage K6.1 - Add QRCodeService
- Implement QRCodeService.cs using QRCoder.
- Generate QR images from verification payload.

### Stage K6.2 - Insert QR at token anchor
- Replace {{QR_CODE}} with generated image in Word document.
- Keep all behavior inside new processing pipeline.

Deliverables:
- QRCodeService.cs

## Phase K7 - Document Generation Orchestration
Reason to do seventh:
- Generation orchestration depends on export/upload/processing services.

### Stage K7.1 - Add DocumentGenerationService
- Implement DocumentGenerationService.cs.
- Generate degree and transcript documents from selected template.

### Stage K7.2 - Serial and issue date assignment
- Assign unique serial number.
- Stamp issue date.

### Stage K7.3 - Output path persistence
- Save generated output path in new extension storage scope.
- Do not change existing storage mechanisms.

Deliverables:
- DocumentGenerationService.cs

## Phase K8 - Transcript Dynamic Table Support
Reason to do eighth:
- Transcript table generation is a specialized extension behavior.

### Stage K8.1 - Build {{COURSE_TABLE}} renderer
- Insert dynamic rows for:
  - Course Name
  - Credit Hours
  - Grade
  - SGPA/Marks

### Stage K8.2 - Non-invasive data retrieval
- Read existing academic data through new query adapters.
- Do not modify existing DB/business logic paths.

Deliverables:
- COURSE_TABLE generation support in TemplateProcessorService

## Phase K9 - PDF Export with Fallback
Reason to do ninth:
- PDF is optional and should not block document delivery.

### Stage K9.1 - Optional PDF converter adapter
- Add new conversion adapter service (add-only).
- No changes to existing export modules.

### Stage K9.2 - Fallback behavior
- If converter unavailable/fails, return .docx.
- Preserve generation success even when PDF conversion is unavailable.

Deliverables:
- Optional PDF conversion component
- Guaranteed .docx fallback

## Phase K10 - New Student Endpoints (No Auth Changes)
Reason to do tenth:
- Student access must be additive and isolated.

### Stage K10.1 - Add student endpoints only
- /student/degree
- /student/transcript

### Stage K10.2 - Reuse current auth as-is
- Do not modify existing authentication logic.
- Apply existing authorization patterns to new endpoints only.

Deliverables:
- New student endpoints under isolated routes

## Phase K11 - New Admin Endpoints (Isolated Controls)
Reason to do eleventh:
- Admin actions should not interfere with current admin modules.

### Stage K11.1 - Add separate admin controllers/endpoints
- Template export/upload operations
- Degree generation operations
- Transcript generation operations

### Stage K11.2 - Keep admin modules isolated
- No modifications inside existing admin feature controllers.
- No behavior changes to existing admin workflows.

Deliverables:
- DegreeController.cs
- TranscriptController.cs
- Additive admin routes

## Phase K12 - Plug-and-Play Integration, Testing, and Release
Reason to do last:
- Validate extension safely before enabling in production.

### Stage K12.1 - Plug-in integration notes
- Document where to register new services in DI.
- Keep startup/config edits minimal and isolated.

### Stage K12.2 - Safety tests
- Verify existing endpoints continue unchanged.
- Verify new endpoints work independently.
- Verify docx generation, QR insertion, and course table rendering.

### Stage K12.3 - Rollout strategy
- Feature flag or route-level enablement (optional).
- Rollback by disabling new extension routes/services only.

Deliverables:
- Integration guide
- Safety validation checklist

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
