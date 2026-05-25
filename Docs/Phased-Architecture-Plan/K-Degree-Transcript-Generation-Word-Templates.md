# Plan K - Degree and Transcript Generation Using Word Templates (.docx)

## Objective
Build a production-ready Degree and Transcript Generation Module for the ASP.NET Core solution using Word template workflow:
- Admin downloads default templates, edits in Word, and uploads updated templates
- System populates templates with student academic data
- Documents can be generated as .docx and exported to PDF when conversion is available
- Student and Admin workflows remain auditable, secure, and version-controlled

## Architecture and Design Principles
- Follow existing clean architecture boundaries (Domain, Application, Infrastructure, API/Web)
- Use dependency injection for all document/template/QR services
- Keep template processing and generation logic in Application services
- Keep storage, OpenXML, QR, and PDF conversion adapters in Infrastructure
- Keep controllers thin and orchestration-focused
- Ensure all critical actions produce audit events

## Placeholder Contract (Template Token Standard)
Required placeholders for default templates:
- {{StudentName}}
- {{FatherName}}
- {{DegreeTitle}}
- {{CGPA}}
- {{IssueDate}}
- {{SerialNumber}}
- {{QR_CODE}}
- {{COURSE_TABLE}}

Token behavior contract:
- Text tokens replaced across body, tables, headers, and footers
- {{QR_CODE}} replaced with generated QR image
- {{COURSE_TABLE}} replaced by generated transcript row table (only for transcript template)
- Unknown placeholders preserved (non-breaking behavior)

## Phase K1 - Domain and Data Contract Baseline
Reason to do first:
- All services/controllers depend on stable entities and contracts.

### Stage K1.1 - Add domain models
- Add or extend domain entities:
  - Templates
  - TemplateVersions
  - Degrees
  - Transcripts
  - Students (reuse existing if available)
  - AuditLogs (reuse existing if available)
- Define relationships and invariants:
  - One template -> many versions
  - One generated document -> immutable serial number
  - Lock/approval state cannot be modified by student operations

### Stage K1.2 - Add application DTO contracts
- Add request/response DTOs for:
  - Template export/download
  - Template upload/version create
  - Degree generate
  - Transcript generate
  - Admin approve/lock/regenerate
  - Student view/download/print operations

### Stage K1.3 - Add repository interfaces
- Add repository abstractions for templates, template versions, generated docs, and serial tracking.
- Define query methods for latest active version lookup and historical version retrieval.

## Phase K2 - Database and Migration Setup
Reason to do second:
- Storage model must exist before controller and service flows are wired.

### Stage K2.1 - Persistence mappings
- Add EF mappings and indexes:
  - Template key and type index (Degree/Transcript)
  - Version index by template and created date
  - Unique serial number index for generated documents
  - Student-document index for portal retrieval

### Stage K2.2 - Migration scripts
- Add migrations for new tables/columns and constraints.
- Include safe defaults and backward-compatible nullable transitions where needed.

### Stage K2.3 - Seed defaults
- Seed default template metadata for:
  - Degree Certificate
  - Transcript
- Mark seeded version as active version 1.

## Phase K3 - Template Export Feature (Admin Download)
Reason to do third:
- Export is core admin bootstrap for editable Word templates.

### Stage K3.1 - Build TemplateExportService
- Add TemplateExportService in Application layer.
- Provide use-cases to return default .docx template bytes by template type.
- If admin has uploaded active template, export that active version; otherwise fallback to system default.

### Stage K3.2 - Provide default sample .docx templates
- Add default templates in Infrastructure assets:
  - Degree Certificate sample
  - Transcript sample
- Ensure placeholders are present in readable locations and formatting is print-friendly.

### Stage K3.3 - Add template download endpoints
- Add Template controller endpoints for admin:
  - DownloadDegreeTemplate
  - DownloadTranscriptTemplate
- Enforce role access: Admin/SuperAdmin only.

Deliverables for Stage K3:
- TemplateExportService
- TemplateController download actions
- Two default .docx sample templates

## Phase K4 - Template Upload and Versioning
Reason to do fourth:
- Upload/version governance is required before generation can rely on custom templates.

### Stage K4.1 - Add secure upload pipeline
- Allow .docx upload only
- Validate MIME and extension
- Size limits and antivirus hook point (if available)
- Store file in secure storage path or DB blob abstraction

### Stage K4.2 - Implement template versioning
- Every upload creates new TemplateVersion record
- Keep historical versions immutable
- Allow active version switch with audit trail

### Stage K4.3 - Add upload endpoints
- Add Template controller actions:
  - UploadDegreeTemplate
  - UploadTranscriptTemplate
  - ListTemplateVersions
  - ActivateTemplateVersion

Deliverables for Stage K4:
- Upload + storage adapter
- Versioning workflow
- Admin API/Web endpoints

## Phase K5 - Word Template Processing Engine (Open XML)
Reason to do fifth:
- Core document generation depends on robust token replacement across Word structures.

### Stage K5.1 - Build TemplateProcessorService
- Use DocumentFormat.OpenXml
- Replace placeholders in:
  - Paragraph runs
  - Table cells
  - Headers
  - Footers
- Support safe replacement when token text spans multiple runs.

### Stage K5.2 - Dynamic section handling
- Resolve {{COURSE_TABLE}} insertion anchor for transcript template.
- Resolve {{QR_CODE}} insertion anchor for both document types.

### Stage K5.3 - Error handling strategy
- If required token missing, return validation warning profile to admin.
- Keep generation fail-fast for hard-required fields (serial, student identity, issue date).

Deliverables for Stage K5:
- TemplateProcessorService
- OpenXML token replacement utility set
- Validation diagnostics for templates

## Phase K6 - Dynamic Transcript Table Generation
Reason to do sixth:
- Transcript output requires row-based academic data rendering.

### Stage K6.1 - Build course table model mapper
- Map student transcript rows to:
  - Course Name
  - Credit Hours
  - Grade
  - SGPA/Marks

### Stage K6.2 - Inject table into document
- Replace {{COURSE_TABLE}} token location with generated OpenXML table
- Apply consistent style, borders, alignment, and header row emphasis

### Stage K6.3 - Totals and summaries
- Add optional SGPA/CGPA summary section under table
- Ensure pagination remains readable for long transcripts

Deliverables for Stage K6:
- Transcript table builder
- Style presets for generated table

## Phase K7 - QR Code Integration
Reason to do seventh:
- Verification metadata must be embedded before final output is persisted.

### Stage K7.1 - Build QRCodeService
- Use QRCoder to generate PNG byte payload
- Encode verification payload (document id/serial/verification URL)

### Stage K7.2 - Insert QR into document
- Replace {{QR_CODE}} token with embedded image in Word document
- Support configurable image dimensions

### Stage K7.3 - Verification endpoint readiness
- Add verification endpoint contract used by QR payload
- Return minimal public verification metadata only

Deliverables for Stage K7:
- QRCodeService
- QR insertion utility in TemplateProcessorService
- Verification contract endpoint

## Phase K8 - Degree/Transcript Generation Workflow
Reason to do eighth:
- With templates, OpenXML, table, and QR ready, full generation can be orchestrated.

### Stage K8.1 - Build DocumentGenerationService
- Generate document from selected active template version
- Assign unique serial number
- Set issue date
- Replace placeholders and embed dynamic content

### Stage K8.2 - Persist generated artifact metadata
- Save generated file path/storage id
- Save generation metadata:
  - StudentId
  - TemplateVersionId
  - SerialNumber
  - IssueDate
  - Status (Draft/Approved/Locked)

### Stage K8.3 - Controller endpoints
- Add Degree controller endpoints:
  - GenerateDegree
  - GetDegree
  - DownloadDegreeDocx
  - DownloadDegreePdf
- Add Transcript controller endpoints:
  - GenerateTranscript
  - GetTranscript
  - DownloadTranscriptDocx
  - DownloadTranscriptPdf

Deliverables for Stage K8:
- DocumentGenerationService
- DegreeController
- TranscriptController

## Phase K9 - PDF Export and Fallback Policy
Reason to do ninth:
- Download format requirement includes PDF where available with safe fallback.

### Stage K9.1 - PDF conversion adapter
- Add IPdfConversionService abstraction and Infrastructure implementation.
- Supported path:
  - Convert generated .docx to PDF when converter is available

### Stage K9.2 - Fallback behavior
- If conversion unavailable/fails:
  - return .docx download
  - include explicit response message/metadata indicating fallback

### Stage K9.3 - Operational controls
- Feature flag for PDF conversion engine availability
- Telemetry for conversion success/failure rate

Deliverables for Stage K9:
- PDF conversion service
- Fallback policy implementation

## Phase K10 - Student Portal Experience
Reason to do tenth:
- Student consumption must be controlled and distinct from admin workflows.

### Stage K10.1 - Student document list and view
- Add portal views/endpoints to list student degree/transcript documents
- Enforce ownership checks (student can access own documents only)

### Stage K10.2 - Download and print controls
- Allow PDF download (or docx fallback)
- Print mode adds watermark:
  - Student Copy / Unofficial

### Stage K10.3 - UI and UX safeguards
- Show approval/lock status to student
- Hide admin-only actions from portal

Deliverables for Stage K10:
- Student portal document screens
- Watermark print support

## Phase K11 - Admin Governance and Audit Controls
Reason to do eleventh:
- Approval, locking, and regeneration require strict governance.

### Stage K11.1 - Admin actions
- Approve document
- Lock document
- Re-generate document (new version while preserving prior history)

### Stage K11.2 - Concurrency and integrity
- Prevent edits after lock
- Add optimistic concurrency token for admin updates

### Stage K11.3 - Audit logs
- Log upload/version activation/generation/approval/lock/regenerate/download events
- Capture actor, timestamp, operation, and target ids

Deliverables for Stage K11:
- Admin governance endpoints/actions
- Audit instrumentation

## Phase K12 - Testing, Hardening, and Release
Reason to do last:
- Module is document-critical and requires full validation before production rollout.

### Stage K12.1 - Automated testing
- Unit tests for token replacement, table generation, QR insertion, serial generation
- Integration tests for end-to-end generation and download
- Contract tests for controller responses and fallback behavior

### Stage K12.2 - Security and performance checks
- Upload validation and path traversal checks
- Role authorization checks
- Load/performance test for batch generation scenarios

### Stage K12.3 - Release and rollback
- Feature-flag rollout plan
- Operational runbook for template storage and PDF converter outage fallback
- Rollback strategy for template version activation

Deliverables for Stage K12:
- Test suite coverage
- Release checklist and rollback runbook

## Required Services and Controllers (Implementation Scope)
Application Services:
- TemplateExportService
- TemplateProcessorService
- QRCodeService
- DocumentGenerationService

Controllers:
- TemplateController
- DegreeController
- TranscriptController

Supporting abstractions:
- ITemplateRepository / ITemplateVersionRepository
- IDegreeRepository / ITranscriptRepository
- ISerialNumberService
- ITemplateStorageService
- IPdfConversionService
- IAuditService integration hooks

## Sample Template Text Structures (Authoring Guide)
Degree template structure (logical text layout):
- Header: Institution Name / Logo
- Title: Degree Certificate
- Body:
  - This certifies that {{StudentName}} son/daughter of {{FatherName}}
  - has successfully completed the requirements for {{DegreeTitle}}
  - with cumulative grade point average {{CGPA}}
- Meta line:
  - Issued on {{IssueDate}}
  - Serial No: {{SerialNumber}}
- Verification block:
  - QR: {{QR_CODE}}
- Footer: Registrar signature block

Transcript template structure (logical text layout):
- Header: Institution Name / Transcript Title
- Student block:
  - Name: {{StudentName}}
  - Father Name: {{FatherName}}
  - Degree Program: {{DegreeTitle}}
- Academic table anchor:
  - {{COURSE_TABLE}}
- Summary:
  - CGPA: {{CGPA}}
  - Issue Date: {{IssueDate}}
  - Serial Number: {{SerialNumber}}
- Verification:
  - {{QR_CODE}}

## Definition of Done (Plan K)
- Admin can download default .docx templates for degree and transcript.
- Admin can upload updated .docx templates with version history.
- Placeholders are replaced correctly in body, tables, headers, and footers.
- Transcript course table is generated dynamically at {{COURSE_TABLE}} anchor.
- QR is generated and inserted at {{QR_CODE}} anchor.
- Degree/transcript generation persists metadata and storage location.
- PDF export works when available; .docx fallback is automatic when not.
- Student portal supports view/download/print with Student Copy / Unofficial watermark.
- Admin approval, lock, and regenerate flows are implemented and audited.
- Automated tests and release runbook are completed.
