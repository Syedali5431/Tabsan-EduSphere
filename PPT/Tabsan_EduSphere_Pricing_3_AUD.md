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
Summary: The platform enforces strict boundaries and operational safeguards for education data.
- Direct URL bypass protection through backend authorization checks
- Role escalation controls and scoped policy enforcement
- Tenant/campus boundary isolation and anti-leakage protections
- Auditable lifecycle for high-impact actions (publish, correction, imports)
- Controlled tokenized report retrieval patterns for sensitive downloadable artifacts

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
Summary: Balanced package for institutions scaling operations beyond basic administration.
- Plan: Growth
- AUD 3 per user (Yearly)
- Includes everything in Foundation Lite, plus LMS, discussion, helpdesk, and study planner
- Support: free priority-email support with faster response window
- Best for: institutions with active academic workflows and mixed admin/faculty adoption
- Optional add-ons: parent portal notifications, advanced analytics exports, custom integrations

---

## Conclusion / Call to Action
Summary: The Growth plan offers strong capability coverage with clear ROI for mid-scale institutions.
- Encourage adoption of the Growth plan for operational expansion
- Highlight feature depth at predictable cost
- Invite for demo with migration and enablement roadmap

## Mobile APP Roadmap Note (2026-05-21)

- Future implementation includes a dedicated Mobile APP for students, faculty, admins, and finance workflows.
- Mobile APP rollout is part of planned enhancements and is not billed as a separate pricing tier in the current cycle.
- Current pricing remains unchanged across this document version.
- New feature additions in this release wave are provided free under existing pricing plans.

## Functionality Synchronization Update (2026-05-28)

- Results governance strengthened: write-scope controls, controlled publish actions, and correction-reason workflow.
- Attendance and results data coverage expanded for realistic training/UAT with multi-day attendance and mixed result lifecycle states.
- Import report access hardening introduced with one-time, short-lived report token behavior.
- Institution-aware certificate workflows and scoped governance controls remain active across School/College/University modes.
- Pricing confirmation: Growth remains AUD 3 per user yearly (no price change).

## Detailed Plan Fit: Growth
Summary: Growth extends foundational operations with collaboration and learning workflows for broader institutional adoption.
- Best suited for institutions transitioning from basic administration to active digital teaching operations
- Adds stronger day-to-day workflow capacity for faculty and mixed-role execution
- Maintains cost predictability while preparing teams for advanced analytics and governance depth

## App Architecture Snapshot
Summary: Tabsan EduSphere uses modular architecture to combine academic operations, governance controls, and actionable analytics.
- API-first service model with policy-driven authorization
- License-aware module surfaces for controlled feature activation
- Web-first operational workflows designed for role-specific execution
- Structured script and validation path for safe rollout and maintenance

## Capability Depth in Growth Plan Context
Summary: Growth is positioned for institutions running active teaching operations across multiple role groups.
- Full academic workflow continuity from offering setup to result publication
- Collaboration features including LMS, discussion, and study planner support
- Stronger faculty-admin coordination through governed settings and report surfaces
- Extendable integration path without platform migration

## Operational Workflow Blueprint
Summary: A consistent operating model helps institutions reduce friction and improve delivery quality.

1. Academic Setup
- Program, offering, and enrollment alignment

2. Continuous Delivery
- Assignments, quizzes, attendance, and in-term result capture

3. Governance Checkpoints
- Role/menu/report scope validations and publication control

4. Analytics Review
- Trend and outcome monitoring for timely intervention

## Security and Compliance Details
Summary: Security posture combines strict authorization with auditable workflow transitions.
- Role and scope validation on protected endpoints
- Controlled publication and correction lifecycle
- Audit trail retention for high-impact actions
- Boundary enforcement across institution and scope contexts

## Adoption Roadmap (Recommended)
Summary: Growth plan adoption works best with phased enablement and measurable checkpoints.

Phase A: Operational Core
- Stabilize attendance, assessments, and results lifecycle

Phase B: Collaboration Layer
- Expand LMS/discussion/helpdesk usage for faculty and student engagement

Phase C: Governance and Insights
- Enable analytics and stronger report discipline for decision support

## Role Enablement Outcomes
Summary: Each role gains clear operational value under Growth.
- Students: better engagement and visibility through structured workflows
- Faculty: efficient delivery and controlled assessment operations
- Admins: stronger operational oversight and communication coverage
- SuperAdmins: policy-level governance and rollout control

## KPI Focus for Growth Institutions
Summary: Suggested metrics to track plan success.
- Assignment and quiz completion consistency
- Attendance capture timeliness
- Result publication cycle duration
- Collaboration feature adoption rates (LMS/discussion/helpdesk)
- Escalation reduction due to clearer governance paths

## FAQ (Growth Plan)

1. Is Growth suitable for mixed digital maturity institutions?
- Yes, it balances practical operational depth with manageable complexity.

2. Can we move to Advanced/Enterprise later?
- Yes, Growth is designed with upgrade continuity and minimal disruption.

3. Does Growth improve cross-role collaboration?
- Yes, it includes workflow features that reduce coordination delays.

## Pricing Integrity Note
Summary: This revision expands app detail content only.
- Growth pricing remains unchanged at AUD 3 per user yearly.

