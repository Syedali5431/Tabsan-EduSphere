# Tabsan EduSphere

## "One Smart System for Schools, Colleges, and Universities"

Summary: A single academic platform that simplifies operations and supports long-term institutional growth.

---

## Problem Statement
Summary: Institutions lose efficiency and visibility when core academic processes are spread across disconnected tools.
- Institutions use multiple disconnected systems
- Manual processes waste time
- Poor data management
- Lack of centralized control

---

## Solution
Summary: Tabsan EduSphere unifies academic, administrative, and reporting workflows in one scalable platform.
- Tabsan EduSphere as a unified system
- One platform for all institutions
- Scalable and flexible system

---

## Key Features
Summary: Purpose-built capabilities help institutions run faster, smarter, and with better control.
- Multi-institution support (School, College, University)
- License-based feature control
- Dynamic dashboards
- Advanced reporting system
- Flexible course and grading system
- ISO 27001 & ISO 9001 compliant platform with full audit trail, security controls, and compliance dashboard

---

## System Flexibility
Summary: The platform adapts to each institution's academic model instead of forcing a fixed process.
- Supports different academic systems
- Semester-based and non-semester courses
- Custom grading for each institution

---

## User Roles
Summary: Clear role-based workflows improve accountability and daily efficiency.
- Admin (manages institution)
- Faculty (manages courses, results)
- Students (view results, dashboard)

---

## Reporting System
Summary: Decision-makers get targeted, accurate insights with minimal effort.
- Separate reports for School / College / University
- Smart filtering based on access and settings
- Performance insights

---

## License System
Summary: Institutions pay for what they need and scale functionality with confidence.
- Client can choose:
  - School (Yes/No)
  - College (Yes/No)
  - University (Yes/No)
- System automatically adjusts features

---

## Benefits
Summary: Tabsan EduSphere drives measurable efficiency and stronger institutional performance.
- Saves time and reduces manual work
- Improves accuracy
- Centralized data management
- Scalable and future-ready
- ISO 27001 + ISO 9001 compliance with enterprise-grade security and audit readiness

---

## Why Tabsan EduSphere is Different
Summary: Unlike fragmented alternatives, Tabsan EduSphere delivers one integrated and modern SaaS platform.
- One system instead of multiple tools
- Dynamic and flexible design
- Modern SaaS architecture

---

## Future Vision
Summary: Continuous innovation ensures institutions stay competitive and digitally advanced.
- AI-powered insights
- Advanced analytics
- Mobile applications
- Global expansion

---

## New Functionalities
Summary: Tabsan EduSphere continues to evolve with cutting-edge features.
- AI-powered chatbot for student and faculty support.
- Enhanced SMS notification system.
- Advanced analytics for institutional decision-making.

---

## Future Implementations
Summary: Upcoming features to further enhance the platform.
- Integration with third-party learning management systems.
- Blockchain-based credential verification.
- Predictive analytics for student performance.

---

## Detailed Functional Coverage
Summary: A complete functional map of what the platform delivers today across academic, governance, and operations workflows.

### Academic Operations
- Course lifecycle management (create, assign, open/close offering)
- Enrollment workflows with status-driven controls
- Assignment authoring, submission, grading, and feedback loops
- Quiz authoring with publish windows, attempt limits, and scoring
- Attendance recording with policy-aware status handling
- Results entry with draft/publish governance and correction rules

### Governance and Control
- Role-based access by SuperAdmin, Admin, Faculty, Student, Finance, Parent
- Sidebar/menu governance aligned to module entitlement and role policies
- Institution-aware behavior (School, College, University)
- Tenant/campus/department scoping controls for safer multi-entity operations
- Audit-sensitive operations hardened with traceability and controlled transitions

### Data and Reporting
- Institution-specific report compositions
- Advanced analytics and trend-ready metrics surfaces
- Scoped exports with standardized output patterns
- Post-deployment validation scripts and maintenance indexes for production readiness

---

## Enter Results Workflow (Detailed)
Summary: Results operations are structured for integrity, controlled publication, and accountability.

1. Scope Selection
- Required filters enforce context (institution/campus/department/offering and relevant period)
- Write operations remain unavailable until full valid scope is selected

2. Draft Entry
- Faculty/Admin enters marks in controlled table flow
- Validation checks run for data completeness and acceptable ranges
- Duplicate and invalid-entry prevention applied at workflow and data layers

3. CSV Import
- Download template with expected headers and sample format
- Upload validated file with row-wise processing feedback
- Import report generated with success/failure detail for each row

4. Publish Governance
- Publish flow is role-controlled (Admin/SuperAdmin governance path)
- Draft-to-published transition changes visibility behavior for student-facing surfaces

5. Correction Governance
- Correction allowed under governed conditions with reason capture
- Audit events retained for correction context and historical traceability

---

## Enter Attendance Workflow (Detailed)
Summary: Attendance operations support both daily execution and reliable historical analysis.

1. Context and Roster Loading
- Select offering/date/scope to load the correct roster
- Scope and assignment checks prevent unauthorized writes

2. Manual Entry
- Mark attendance statuses (Present/Absent/Late and policy variants)
- Save with immediate validation and conflict/duplicate checks

3. CSV Import Path
- Template-assisted import for large classes and bulk updates
- Validation for required columns, format quality, and scope consistency
- Duplicate-date and duplicate-roster safeguards

4. Analytics and Alerts
- Attendance records feed trend metrics and risk indicators
- Policy thresholds can drive warnings and interventions

---

## Security and Compliance Posture
Summary: Built on ISO 27001 and ISO 9001 standards with enterprise-grade security controls, full audit traceability, and compliance dashboard.
- ISO 27001 (Information Security) compliance: immutable audit logging with full context (actor role, IP, user agent, device info, correlation ID, severity, event category)
- ISO 27001 A.9 Access Control: 90-day password ageing, 5-password history, 30-minute idle session timeout, MFA support, session revocation
- Login activity monitoring: all authentication attempts logged with success/failure reason, risk level, and IP tracking
- ISO 27001 A.17 Business Continuity: backup operations logged with checksums and verification records (IntegrityCheck, RestoreTest)
- Data protection: AES-256 encryption service, PII data masking, data classification scheme (Public/Internal/Confidential/Restricted), GDPR consent tracking
- ISO 27001 A.16 Incident Management: full incident lifecycle tracking (Open → Investigating → Resolved → Closed) with severity and category classification
- ISO 9001 7.5 Document Management: version-controlled policy documents with Draft/Published/Archived lifecycle and access control levels
- Direct URL bypass protection through backend authorization checks
- Role escalation controls and scoped policy enforcement
- Tenant/campus boundary isolation and anti-leakage protections
- Auditable lifecycle for high-impact actions (publish, correction, imports)
- Controlled tokenized report retrieval patterns for sensitive downloadable artifacts
- Compliance dashboard with 7-section aggregated posture (Audit, Security, Backup, Incidents, Activity, Data Protection, Documents)

---

## Performance and Reliability Design
Summary: Built for sustained real-world load and predictable operational behavior.
- Bulk CSV processing support for operational scale
- High-volume seed and validation scripts for realistic performance rehearsal
- Index maintenance strategy for attendance/results and scope-heavy queries
- Safe handling of empty datasets, rapid filter changes, and concurrent actions
- Deterministic validation scripts for go-live confidence

---

## Implementation and Rollout Model
Summary: A phased approach minimizes risk and accelerates adoption.

Phase 1: Foundation
- Activate core modules, roles, and baseline data policies
- Configure institution mode and governance defaults

Phase 2: Academic Enablement
- Enable results, attendance, assignments, quizzes, and reporting baseline
- Run role-based UAT scenarios with real department stakeholders

Phase 3: Governance Hardening
- Apply scoped settings for tenant/campus/department boundaries
- Validate publish/correction controls and audit/report integrity

Phase 4: Optimization
- Introduce advanced analytics and export workflows
- Tune adoption, training, and KPI-based operational improvements

---

## Operational KPI Framework
Summary: Suggested measurable outcomes to track institutional ROI.
- Time-to-publish results (before vs after digitization)
- Attendance completion accuracy and on-time submission rates
- CSV import first-pass success ratio
- Audit exception rate and correction turnaround duration
- Report generation latency and stakeholder usage frequency
- Faculty/admin productivity gains by workflow type

---

## Data Migration and Integration Readiness
Summary: Migration and integration are designed for controlled adoption without service disruption.
- Structured import templates for user and academic data onboarding
- Validation-first migration path with staged dry-runs
- Backward compatibility awareness for legacy data patterns
- Integration roadmap for LMS, communication tools, and identity workflows

---

## Support and Success Model
Summary: Institutions receive predictable support and practical enablement during growth.
- Guided onboarding playbooks by role
- Admin and faculty operational training modules
- Troubleshooting runbooks for common workflow blockers
- Continuous enhancement path aligned to institutional maturity

---

## Pricing
Summary: Advanced package for institutions requiring broader automation, analytics, and parent transparency.
- Plan: Advanced
- AUD 4 per user (Yearly)
- Includes everything in Growth, plus advanced analytics, export jobs, parent portal read views, and notification fan-out
- Support: free accelerated support with operational guidance
- Best for: institutions running data-driven decisions and cross-role workflow orchestration
- Optional add-ons: enterprise SLA, custom reporting packs, integration acceleration

---

## Conclusion / Call to Action
Summary: The Advanced plan is ideal for institutions that need deeper visibility and higher workflow automation.
- Encourage adoption of the Advanced plan for analytics-led operations
- Highlight reduced manual load and improved governance
- Invite for demo with KPI-based adoption targets

## Mobile APP Roadmap Note (2026-05-21)

- Future implementation includes a dedicated Mobile APP for students, faculty, admins, and finance workflows.
- Mobile APP rollout is part of planned enhancements and is not billed as a separate pricing tier in the current cycle.
- Current pricing remains unchanged across this document version.
- New feature additions in this release wave are provided free under existing pricing plans.

## Functionality Synchronization Update (2026-06-04)

- Results governance strengthened: write-scope controls, controlled publish actions, and correction-reason workflow.
- Attendance and results data coverage expanded for realistic training/UAT with multi-day attendance and mixed result lifecycle states.
- Import report access hardening introduced with one-time, short-lived report token behavior.
- Institution-aware certificate workflows and scoped governance controls remain active across School/College/University modes.
- Pricing confirmation: Advanced remains AUD 4 per user yearly (no price change).
- ISO 27001 + ISO 9001 compliance instrumentation: 7 new compliance tables, 20+ indexes, compliance dashboard, incident management workflow, backup verification, policy document versioning, and data integrity checks operational across all plans.

## Detailed Plan Fit: Advanced
Summary: Advanced is ideal for institutions that need stronger visibility, governance, and analytics-backed decisions at scale.
- Best suited for organizations running cross-role operations with measurable performance targets
- Enables deeper monitoring and policy-backed execution confidence
- Bridges operational maturity toward enterprise-grade reliability and governance

## App Architecture Snapshot
Summary: Advanced-tier delivery is powered by a modular architecture that blends governance discipline with high-visibility analytics.
- Policy-enforced API execution for sensitive operations
- Role-aware portal surfaces with module-governed visibility
- Export-ready reporting pipeline for operational and strategic analysis
- Reliability-oriented workflow design for recurring academic cycles

## Advanced Plan Capability Emphasis
Summary: Advanced focuses on insight-driven execution, stronger controls, and parent-facing transparency.
- Advanced analytics for trend, comparison, and performance insight
- Export job workflows for high-volume report handling
- Parent portal read visibility and communication fan-out options
- Governance reinforcement across sidebar, report, and module settings

## Governance and Analytics Operating Model
Summary: Institutions can combine analytics depth with controlled policy execution.

1. Role and scope governance
- Validate access by institution, campus, department, and role assignment

2. Reporting discipline
- Define report ownership, access boundaries, and recurring review cadence

3. Analytics-led intervention
- Use trend indicators to prioritize academic and operational actions

4. Audit readiness
- Maintain evidence for publication/correction and report distribution events

## Reliability and Performance Considerations
Summary: Advanced deployments should emphasize predictable execution at medium-to-high operational volume.
- Support for bulk import and validation-heavy workflows
- Stable export patterns for recurring reporting cycles
- Improved operational resilience through staged rollout and post-deployment checks
- Structured handling for concurrent updates and large dataset navigation

## Adoption Roadmap (Advanced)
Summary: A focused rollout helps institutions realize analytics ROI quickly.

Phase 1: Core stability
- Confirm core academic workflows and governance baselines

Phase 2: Analytics expansion
- Activate advanced report and trend usage by governance-defined roles

Phase 3: Parent and communication pathways
- Enable parent read workflows and aligned communication patterns

Phase 4: Continuous optimization
- Tune KPI targets and governance controls based on measured outcomes

## KPI Targets for Advanced Institutions
Summary: Recommended performance indicators for Advanced rollout success.
- Reporting adoption by leadership and operations teams
- Export completion reliability and turnaround time
- Result publication governance compliance rate
- Attendance-risk intervention response time
- Parent communication reach and acknowledgement rate

## FAQ (Advanced Plan)

1. Is Advanced only for large institutions?
- No, it suits any institution needing richer analytics and tighter governance.

2. Can Advanced reduce manual decision cycles?
- Yes, analytics and standardized exports improve decision speed and consistency.

3. Does Advanced support phased rollout?
- Yes, institutions can activate capabilities by governance priority.

## ISO 27001 + ISO 9001 Compliance Update (2026-06-04)

- Platform now instrumented for ISO 27001 (Information Security) and ISO 9001 (Quality Management) compliance across 10 phases.
- Enhanced audit logging with immutable records, full context capture, severity/category classification, and exportable compliance evidence.
- Security hardening: password ageing (90-day), password history (5-password), idle session timeout (30 min), MFA, session management.
- Login activity monitoring, backup/DR logging, backup verification, incident management, policy document versioning, data classification, encryption/masking, data integrity checks, and compliance dashboard — all additive and backward-compatible.
- All ISO compliance features included in existing plans at no additional cost.

## Pricing Integrity Note
Summary: This revision expands app and execution details only.
- Advanced pricing remains unchanged at AUD 4 per user yearly.

