# User Import Sheets

This folder contains CSV templates for bulk-importing user accounts via the admin portal.

Version: 1.6  
Date: 15 May 2026  
Completion Status: Phase 38 complete (final separation baseline)

Repository Sync Note (15 May 2026):
- Deployment workflows now support both Demo and Clean database modes.
- For clean startup environments, use `Scripts/Seed-Core-Clean.sql` and `Scripts/05-PostDeployment-Checks-Clean.sql`.

## Final Release Packaging Update (Phase 37/38)

- Runtime app and license app publish outputs are separated.
- User import templates are distributed through the non-runtime package flow.
- Evidence references:
	- Artifacts/Phase37/Publish-Separation-20260515.md
	- Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

## Templates

1. `faculty-admin-import-template.csv`
2. `students-import-template.csv`

Note: CSV format itself cannot store in-cell dropdown validation metadata. Dropdowns are applied in Excel after opening/importing the CSV files.

## Template: `faculty-admin-import-template.csv`

### Columns

| Column | Required | Description |
|---|---|---|
| Username | Yes | Unique login handle. Used as the initial password on import. |
| Email | No | Optional email address. Must be unique if provided. |
| FullName | No | Display name (reference only). |
| Role | Yes | Must be `Admin` or `Faculty` for this template. |
| DepartmentId | Yes | Department GUID. Required for faculty and recommended for admin scope assignment. |
| InstitutionType | Yes | `School`, `College`, or `University` (must be license-enabled). |
| MobileNumber | Optional | Mobile number for SMS/notification readiness. Accepts digits and common separators. |
| CampusAssignments | Optional | Pipe-separated campus GUID list (for assignment workflow prep), e.g. `guid1|guid2`. |

## Template: `students-import-template.csv`

### Columns

| Column | Required | Description |
|---|---|---|
| Username | Yes | Unique login handle. Used as the initial password on import. |
| Email | No | Optional email address. Must be unique if provided. |
| FullName | No | Display name (reference only). |
| Role | Yes | Must be `Student`. |
| DepartmentId | Yes | Student department GUID. |
| InstitutionType | Yes | `School`, `College`, or `University` (must be license-enabled). |
| MobileNumber | Optional | Mobile number for SMS/notification readiness. Accepts digits and common separators. |
| CampusAssignments | Optional | Pipe-separated campus GUID list (for assignment workflow prep), e.g. `guid1|guid2`. |
| ProgramId | Recommended | Program GUID for downstream student profile setup. |
| RegistrationNumber | Recommended | Student registration number for profile/whitelist workflows. |
| CurrentSemesterNumber | Recommended | Numeric level/semester value for initial academic state. |
| SemesterName | Yes | Semester/grade-year label used for allocation workflow. |
| CourseCode | Yes | Course code for enrollment mapping workflow. |
| CourseTitle | Recommended | Human-readable course title. |
| CourseOfferingId | Recommended | Course offering GUID when assigning directly to an offering. |

### Rules

- The **initial password** for every imported user is set to their **Username** (case-sensitive).
- All imported users are flagged as **MustChangePassword = true**. They will be prompted to set a new password on their first login.
- Rows with duplicate usernames (across the file or already in the database) are skipped and counted as duplicates.
- Rows with missing required fields or invalid values are skipped and reported as errors.
- If `InstitutionType` is provided, it must be enabled in the active institution license policy.
- `DepartmentId` must be a valid GUID and should match an active department in the current environment.
- `MobileNumber`/`PhoneNumber` (if provided) must contain only digits, spaces, `+`, `-`, `(`, `)` and be within the 32-character max length.
- `CampusAssignments` (if provided) must be a pipe-separated list of valid GUID values.
- Keep `InstitutionType` values consistent with active deployment policy to avoid avoidable import rejects.
- The current API import parser requires these minimum columns in uploaded files: `Username, Email, Role`.
- Older templates remain backward-compatible; new columns are optional and can be left blank.
- Extra student-oriented columns are included for operational consistency and downstream mapping workflows.

## Excel Dropdown Setup

1. Open `faculty-admin-import-template.csv` or `students-import-template.csv` in Excel.
2. Create a `Lists` sheet in the workbook and add values for:
	- `InstitutionType`: `School`, `College`, `University`
	- `CourseCode`: active course codes from your environment
	- `SemesterName`: active semester names from your environment
3. Select the target column (for example `InstitutionType`, `CourseCode`, or `SemesterName`).
4. Go to Data -> Data Validation -> List.
5. Set Source to the corresponding range in the `Lists` sheet.
6. Save as `.xlsx` if you want dropdowns to persist.

### How to import

1. Open the Admin Portal → User Management → Import Users.
2. Select your completed CSV file and click **Upload**.
3. Review the import summary (imported / duplicates / errors).
4. Share the generated usernames and initial-login instructions with the new users.

## Import Quality Checklist

- Validate duplicate usernames before upload.
- Confirm role names are exact (`Admin`, `Faculty`, `Student`) and template-specific.
- Confirm GUID format for all `DepartmentId` values.
- Confirm institution mode support before using `InstitutionType` assignments.
- For student imports, align `CourseCode` and `SemesterName` to active course offerings.

## Distribution Note

For release packaging, include this folder through the Phase 38 non-runtime asset workflow so import templates stay out of runtime app artifacts.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
