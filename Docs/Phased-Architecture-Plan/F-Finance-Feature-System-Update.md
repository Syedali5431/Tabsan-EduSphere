# Plan F - Finance Feature and System Update (No Code)

## Phase 0 - Stability and Safety
- Ensure no existing functionality breaks.
- Maintain:
  - Tenant isolation
  - Campus-level access rules
  - Analytics filters
  - Existing role permissions
- Treat all changes as additive only.

## Phase 1 - Database Updates
- Add `Mobile Number` field for all users (optional but available across system).
- Allow multiple campus assignment per user.
- Introduce new role: `Finance`.
- Ensure payment records support:
  - Paid / Unpaid status
  - Payment tracking (date/updates)

## Phase 2 - Role and Access Control
### Finance Role Rules
- Can:
  - Add payments
  - Edit payments
  - Mark payments as paid
- Cannot:
  - Delete payments
  - Access academic modules
- Access is always restricted by:
  - Tenant
  - Assigned Campus (multi-campus support)

## Phase 3 - Analytics
- Add interactive pie chart:
  - Paid vs Unpaid payments
- Chart must:
  - Respect filters (Campus, Department, Course, Semester/Class)
  - Update dynamically
- Finance users:
  - See only payment-related analytics
  - Do NOT see academic analytics

## Phase 4 - Reports
### Report Types
- Payment reports:
  - Monthly
  - Yearly
  - Semester-based (only for university)
- Payment status report:
  - School/College -> Class-based
  - University -> Semester-based

### Report Data
- Student ID
- Student Name
- Payment Amount
- Payment Status

### Filters
- Campus
- Department
- Course
- Class / Semester

### Access Control
- Finance users:
  - Can view/download payment reports only
- Other roles remain unchanged.

## Phase 5 - UI / UX
### Add Finance Section
- Finance
  - Payments
  - Payment Reports
  - Analytics

### Enhancements
- Add `Mark as Paid` option in payments.
- Show Paid / Unpaid status clearly.
- Enable campus-based selection (multi-campus).
- Add pie chart in analytics dashboard.

### Theme Access
- Finance users can apply themes.
- No access to system configuration settings beyond themes.

## Phase 6 - Import Sheets
- Update all CSV templates:
  - Add `Mobile Number` field
  - Add campus assignment field (multiple allowed)
- Ensure:
  - Backward compatibility with existing imports
  - Validation for new fields

## Phase 7 - Documentation Updates
- Update all documents.

### User Guide
- Add Finance role explanation.
- Add payment workflows.
- Add reporting and analytics usage.

### Training Manual
- Finance-specific training section.
- Payment handling procedures.
- Report generation training.

### UAT / SAT Docs
- Add test scenarios:
  - Finance permissions
  - Multi-campus assignment
  - Payment updates
  - Report filtering accuracy

## Phase 8 - DB Script Synchronization
- Update all database scripts to reflect:
  - New role (Finance)
  - Multi-campus support
  - Mobile number field
  - Payments enhancements
- Ensure:
  - Scripts remain idempotent
  - No duplication or conflicts

## Phase 9 - Conflict Prevention (Critical)
### Role Separation
- Finance must NOT inherit Admin privileges.
- Keep permissions isolated.

### Data Security
- Always enforce:
  - Tenant boundary
  - Campus filtering

### Analytics Integrity
- Payment analytics must remain separate from academic analytics.

### Report Isolation
- Payment reports must not pull unrelated academic data.

## Phase 10 - Final Validation Checklist
- Ensure:
  - Multi-campus assignment works correctly
  - Finance permissions are enforced strictly
  - Pie chart functions with filters
  - Reports export correctly (PDF and Excel)
  - Mobile number is saved and usable
  - Import templates work with new fields
  - All documentation reflects updates

## Recommended Enhancements (Optional)
- Payment audit tracking (who marked paid)
- SMS notifications using mobile numbers
- Payment reminders / overdue alerts
- Finance dashboard customization

## Result
This plan ensures:
- No overlap or permission conflicts
- Full alignment with current PRD
- Clean separation of Finance features
- Safe scaling without breaking existing modules
