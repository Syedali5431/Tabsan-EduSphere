# Final Deep Scan

## Phase 1: Master Testing Prompt for Your Application

### Prompt Title
Perform End-to-End Validation of Education Portal (Results and Attendance Modules + Full System)

### Prompt
Act as a senior QA engineer and system auditor.
Your task is to perform a complete, structured, multi-layer testing and validation of an education management application based on the provided requirements documents.

## Phase 2: Testing Scope

Validate the system across the following modules and layers:

### Core Modules
- Enter Results Module
- Enter Attendance Module
- User Management
- Reporting and Analytics
- Sidebar/Menu Governance
- License and Role-Based Access

### Key Functional Areas
- Manual Data Entry
- CSV Import / Export
- Template Download
- Filtering and Scope Enforcement
- Publishing and Correction workflows
- Audit Trails and Reports

## Phase 3: Testing Objectives

You must verify:

### Functional Correctness
- All features behave exactly as described in requirements
- No missing flows or broken paths

### Access Control and Roles
- Only authorized roles (SuperAdmin, Admin, Faculty) access modules [new 5 | Txt]
- Students/unauthorized users cannot access via UI or direct URL
- Faculty is limited to assigned scope [new 5 | Txt]

### Filter and Scope Validation
- Required filters must be selected before any write action [new 5 | Txt]
- Dependent filters update correctly
- No data leakage across tenant/campus/department boundaries

### CSV Import/Export
- Template contains correct headers and sample rows [new 5 | Txt]
- Import validation includes:
  - Required fields
  - Correct formats
  - Scope validation
  - Duplicate prevention [new 5 | Txt]
- Strict vs non-strict mode behavior works correctly [new 5 | Txt]

### UI Behavior Logic
- Editable table appears only when filters are complete [new 5 | Txt]
- Disabled/guidance state appears otherwise
- All buttons correctly enabled/disabled

### Publishing and Governance
- Only Admin/SuperAdmin can publish results [newcastlew...epoint.com]
- Draft vs published behavior enforced
- Correction allowed only on published results
- All corrections must have audit reasons

### Audit and Reporting
- Audit trail generated for every import [new 5 | Txt]
- Import report includes row-level status
- Token-based report download:
  - One-time use
  - Expiry (2 hours)
  - Proper error messages [new 5 | Txt]

## Phase 4: Test Categories

### A. Unit-Level Validation
- Validation logic (marks range, duplicates)
- Filter enforcement
- Role access logic

### B. Integration Testing
- Sidebar to Module access
- Filter selection to data loading
- Import to database writes
- Report token flow

### C. End-to-End Scenarios
Test complete workflows:

#### Scenario 1: Results Entry (Manual)
- Select full filter scope
- Enter marks
- Save draft
- Publish as Admin
- Verify immutability

#### Scenario 2: Results CSV Import
- Download template
- Upload valid/invalid CSV
- Validate partial success handling
- Download report
- Verify audit log

#### Scenario 3: Attendance Entry
- Select filters
- Mark attendance manually
- Import CSV
- Validate duplicate prevention
- Verify database records

### D. Negative Testing
- Missing filters
- Wrong CSV format
- Invalid marks/attendance
- Unauthorized access attempts
- Expired report token usage

### E. Performance and Edge Cases
- Large CSV uploads
- Empty datasets
- Concurrent operations (publish, import)
- Rapid filter changes

### F. Security Testing
- Direct URL access bypass attempts
- Cross-tenant data access attempts
- Role escalation attempts

### G. Regression Testing
Ensure no breakage in:
- Existing academic modules
- Attendance workflows
- Reporting systems [new 5 | Txt]

## Phase 5: Expected Output Format

Return your testing results in the following format:

### Test Report

#### 1. Summary
- Total tests executed
- Passed / Failed
- Critical issues

#### 2. Functional Issues
- Issue description
- Steps to reproduce
- Severity
- Expected vs actual

#### 3. Security Findings
- Unauthorized access cases
- Data leakage risks

#### 4. Validation Gaps
- Missing validations
- Weak controls

#### 5. UI/UX Issues
- Inconsistent behavior
- Missing feedback messages

#### 6. Recommendations
- Fix priorities
- Improvements

## Phase 6: Special Instructions
- Do NOT assume anything not defined in requirements
- Treat tenant/campus boundaries as strict isolation rules
- Ensure full backward compatibility validation
- Highlight even minor inconsistencies
- Prefer realistic user flows over isolated checks

## Phase 7: Advanced Simulation Requirement
Also simulate attacker behavior, edge-case misuse, and real production load conditions. Identify hidden bugs that may not appear in normal workflows.
