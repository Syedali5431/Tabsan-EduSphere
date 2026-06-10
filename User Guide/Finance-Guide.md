# Finance User Guide

Version: 1.2  
Date: 10 June 2026  
Completion Status: Detailed finance operations baseline + ISO 27001 & ISO 9001 Compliance + 2026-06-10 Enhancements  

## 1. Purpose

This guide explains day-to-day Finance role workflows in Tabsan EduSphere, including payment management, payment reports, payment analytics, filter usage, and escalation standards.

## 2. Role Scope and Access Boundaries

Finance users are focused on payment operations and should not perform academic administration tasks.

Finance users can access:
- Payments
- Payment Reports
- Payment Analytics
- Theme and profile preferences

Finance users cannot access:
- Academic module management
- Attendance entry and governance
- Results entry and publication
- Course and assignment administration
- License and global system governance

## 3. What Is New for Finance Operations

Recent runtime synchronization includes:
- Institution-aware and student-aware filtering in payment workflows.
- Deterministic demo seed rows for payment filter testing.
- Improved CSV payment import date compatibility for spreadsheet formats.
- Stronger scope behavior in all-scope superadmin contexts when filtering by institution type.
- **ISO Compliance (June 2026)**: Enhanced audit logging now captures all payment operations with full context. Payment data classified as Restricted per data protection scheme. Data masking applied to sensitive financial information in UI exports. Encryption service available for sensitive payment data.

## 4. Login and Session Safety

1. Open the institutional EduSphere URL.
2. Sign in with your Finance account.
3. Confirm the Finance navigation is visible.
4. If required, complete password change flow using current and new secure password.
5. Log out when work is complete, especially on shared systems.

Security recommendations:
- Do not share credentials.
- Do not keep sessions open on unattended devices.
- Report suspicious payment or access activity immediately.
- **Enhanced Security**: Passwords expire every 90 days. Sessions timeout after 30 minutes of inactivity. All login attempts are monitored. Enable MFA in your profile settings for stronger account protection.

### 4.1 Data Protection and Compliance (ISO 27001)

Finance operations handle Restricted-classification data:
- Payment records and financial transactions are classified as Restricted — highest protection level.
- All payment operations are audit-logged with full context (who, what, when, from where).
- Data masking hides sensitive fields in UI exports and reports.
- AES-256 encryption is available for sensitive payment data storage and transmission.
- Report any suspicious payment activity or data anomalies through the incident reporting system (Settings > Incidents).

## 5. Finance Dashboard Routine

Use this short routine at the start of each day:

1. Check unpaid vs paid trend indicators.
2. Review pending payment verification tasks.
3. Confirm your current scope filters (institution, tenant, campus, department).
4. Open payment reports for same-day reconciliation needs.

## 6. Payments Workflow (Detailed)

Path: Sidebar -> Payments

### 6.1 Search and Filter

1. Select institution type if available.
2. Select tenant and campus where required.
3. Narrow by department and course/class as needed.
4. Optionally filter by student.
5. Verify result set count before making updates.

### 6.2 Create Payment Entry

1. Click create or add payment.
2. Select student from scope-correct list.
3. Enter amount, due date, and payment context.
4. Save as unpaid when awaiting settlement.
5. Reopen record and verify values before final confirmation.

### 6.3 Update and Confirm Payment

1. Open target payment record.
2. Verify student, amount, and period.
3. Update status to paid only after evidence is confirmed.
4. Save and confirm update timestamp changes.
5. Keep internal reference note when your process requires it.

## 7. Payment CSV Import Workflow

Path: Sidebar -> Payments -> Import

1. Download and review the payment import template.
2. Populate required columns exactly.
3. Use supported date formats:
- yyyy-MM-dd
- dd/MM/yyyy
- MM/dd/yyyy
- dash variants
4. Upload the file.
5. Review per-row success and failure results.
6. Correct failed rows and re-import.

Import quality rules:
- Keep scope columns consistent with selected filters.
- Avoid duplicate receipt numbers.
- Validate amount and date formats before upload.

## 8. Payment Reports

Path: Sidebar -> Payment Reports

1. Select the same scope used during payments processing.
2. Apply time filters (month, year, term, or custom range).
3. Review totals for paid, unpaid, and pending categories.
4. Export to PDF, Excel, or CSV as required.
5. Store reports according to institutional retention policy.

Best practice:
- Re-run reports after major update batches to validate consistency.

## 9. Payment Analytics

Path: Sidebar -> Analytics (Finance view)

1. Open Paid vs Unpaid chart.
2. Apply institution and campus filters.
3. Compare trend slices by department or course.
4. Identify areas with delayed settlement patterns.
5. Share findings with Admin leadership for action.

Suggested weekly analytics review:
- High unpaid clusters by campus
- Repeat delayed-payment groups
- Month-over-month payment improvement

## 10. Scope and Filter Accuracy Controls

Finance accuracy depends heavily on using correct filters.

Before updating any record:
1. Confirm institution type.
2. Confirm tenant and campus.
3. Confirm department and course/class context.
4. Confirm student identity.
5. Confirm receipt or transaction identifier.

If expected students are missing:
- Re-check institution filter first.
- Re-check tenant and campus scope.
- Escalate with full filter context if issue persists.

## 11. Governance and Audit Expectations

Finance actions are operationally sensitive and should be auditable.

Always ensure:
- Status updates are evidence-based.
- Report exports are authorized and tracked.
- Corrections are traceable and justified.
- Role boundaries are respected.

## 12. Common Issues and Resolution

1. Student not visible in payment form
- Verify institution and campus filters.
- Verify the student belongs to your scope.
- Retry with refreshed filter selection.

2. Import row fails date validation
- Convert to supported date format.
- Remove unexpected text or hidden characters.
- Re-upload corrected file.

3. Payment appears in wrong report slice
- Re-check report filters.
- Verify payment record scope fields.
- Re-run report after correction.

4. Access denied on non-finance pages
- Expected by role design.
- Request admin support only if you need temporary authorized access.

## 13. Escalation Matrix

Finance -> Admin
- Incorrect student scope mapping
- Department-level payment anomalies

Finance -> SuperAdmin
- Role/menu visibility mismatch
- Cross-scope governance inconsistency

Finance -> Platform/IT
- Import processing errors not resolved by data correction
- Repeated runtime errors with timestamp and reproducible steps

## 14. Support Ticket Template for Finance

Include:
- Reporter username and role
- Institution type
- Tenant and campus
- Department and course/class filters
- Student identifier if applicable
- Receipt or transaction identifier
- Step-by-step reproduction
- Expected vs actual result
- Timestamp and timezone

## 15. Daily and Weekly Checklist

### Daily Checklist
- Review unpaid queue.
- Validate and update new receipts.
- Run quick report reconciliation.
- Escalate unresolved high-priority issues.

### Weekly Checklist
- Run campus and department summary reports.
- Review analytics trends for delayed settlements.
- Validate import error trends and root causes.
- Share summary with Admin or Finance lead.

## 16. Finance Release Readiness Checklist

Before each release window:
- Confirm payment pages load successfully.
- Confirm student scope filters behave correctly.
- Confirm report exports complete for key filters.
- Confirm analytics chart updates with selected scope.
- Confirm no unauthorized module visibility for Finance role.

## 17. Finance Compliance and Data Handling

- Treat payment information as sensitive operational data.
- Export only what is needed for authorized tasks.
- Avoid sharing raw files through unsecured channels.
- Follow institution retention and deletion policy.

## Phase 40.6 ISO 27001 + ISO 9001 Compliance Update (2026-06-04)

- Payment operations now fully audit-logged with immutable records.
- Payment data classified as Restricted with enhanced protection controls.
- Data masking and AES-256 encryption for sensitive financial data.
- Enhanced password policy, session timeout, and login monitoring active.
- All changes are additive and backward-compatible.

## 18. Conclusion

The Finance role in EduSphere is designed for controlled, scope-aware, and auditable payment operations. Following this guide helps maintain data quality, reporting accuracy, and governance compliance while keeping day-to-day finance workflows efficient.
