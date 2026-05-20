# Plan G - Result Calculation Based on License (Safe Implementation Prompt)

## Objective
Modify result calculation logic so it adapts dynamically based on institute types enabled in the license file, while preserving existing flows, lifecycle behavior, and the current GPA system.

## Phase 0 - Strict Safety Rules (Do Not Break Anything)
- Do NOT modify:
  - Existing GPA/CGPA calculation logic
  - Student lifecycle workflows (promotion, graduation, failure)
  - Existing result storage structure
  - Report generation logic unless required
- Implement only a conditional logic layer.
- Ensure full backward compatibility.
- Keep default behavior as University (GPA) if no condition applies.

## Phase 1 - License-Based Institute Detection
### Requirement
- Read institute types from license:
  - School
  - College
  - University

### Rule
- Result calculation must adapt based on:
  - Enabled institute type(s) in license
  - Department/institution type context already present in the system

## Phase 2 - Result Calculation Logic
### 2.1 School Result Logic
- For schools use percentage calculation (marks-based).
- Output:
  - Percentage (for example: 65%, 78%)
  - Grade (A+, A, B, etc)

### 2.2 College Result Logic
- For colleges use percentage-based system (same as school).
- Output:
  - Percentage
  - Grade

### 2.3 University Result Logic (Unchanged)
- Keep existing:
  - GPA
  - CGPA
  - Credit-based system

### Important
- No modification to existing GPA system.
- No refactoring of current GPA logic.

## Phase 3 - Mapping Rules (Critical)
Ensure consistent mapping:

| Institute Type | Calculation Method |
| --- | --- |
| School | Percentage + Grade |
| College | Percentage + Grade |
| University | GPA / CGPA |

## Phase 4 - Grade Mapping (For School and College)
Define standardized grade rules:
- A+ -> high percentage range
- A -> slightly lower range
- B -> average range
- C / D -> low range

Requirements:
- Must be configurable (future-safe).
- Must not affect GPA structure.

## Phase 5 - System Integration
Apply logic only at:
- Result calculation stage
- Result display layer

Do NOT affect:
- Enrollment
- Assignments
- Quizzes
- Analytics (except where result values are displayed)

## Phase 6 - Lifecycle Compatibility (Very Important)
Ensure result changes support:
- Student promotion (School/College based on percentage)
- Graduation workflows
- Failure criteria
- Semester progression

Constraints:
- No changes to lifecycle APIs or workflows.
- Only calculation output changes.

## Phase 7 - Mixed License Handling
If license includes multiple institute types:
- Use department institution type to decide calculation.

Example:
- School department -> percentage
- University department -> GPA

Outcome:
- Prevents conflicts
- Supports multi-institution operation already present in the system

## Phase 8 - Reports and UI
Display correct format:
- School/College -> Percentage + Grade
- University -> GPA/CGPA

Ensure:
- Reports reflect correct calculation type.
- No mixing of percentage and GPA in the same context.

## Phase 9 - Conflict Prevention (Critical)
Avoid:
- Overwriting GPA logic
- Mixing percentage and GPA calculations
- Breaking existing report queries
- Unnecessary DB schema changes

Enforce:
- Clear separation of calculation types
- Strict institute-based conditional handling

## Phase 10 - Validation Checklist
- License-based switching works.
- School results show percentage + grade.
- College results show percentage + grade.
- University results remain unchanged (GPA/CGPA).
- Lifecycle flows remain unaffected.
- Reports display correct formats.
- Multi-institute behavior works without conflict.

## Optional Enhancements (Safe to Add)
- Configurable grading scale (Admin settings)
- Percentage-based ranking (School/College)
- Result summary dashboard per institute type

## Final Result
This plan ensures:
- No breakage of current system behavior
- Clean separation of academic models
- Full compatibility with license-based architecture
- Safe extension of result calculation logic
- Support for future expansion
