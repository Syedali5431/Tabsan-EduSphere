# User Acceptance Testing (UAT)

## Purpose
User acceptance testing confirms the product behaves correctly from an operator perspective before deployment handoff.

## EduSphere UAT Steps

1. Start the API and web application.
2. Sign in as SuperAdmin.
3. Open institution policy settings.
4. Enable any valid combination of School, College, and University.
5. Save the policy and confirm it persists after reload.
6. Upload a valid `.tablic` file from Tabsan.Lic.
7. Confirm the license becomes active and the correct institution scope is enforced.
8. Verify a domain-locked license is rejected from the wrong host.
9. Verify a license with `MaxUsers` greater than zero enforces the user limit.
10. Validate user import templates in User Import Sheets:
	- faculty-admin-import-template.csv
	- students-import-template.csv
11. Test import/export (CSV, PDF, Excel) features for users and timetables.
12. Run Scripts/01-Schema-Current.sql through Scripts/05-PostDeployment-Checks.sql in order for full deployment validation.

## Finance UAT Steps

1. Sign in as a Finance user.
2. Confirm Finance navigation shows Payments, Payment Reports, Analytics, and Theme Settings only.
3. Verify academic modules are blocked from the Finance account.
4. Open Payments and update an existing receipt in the correct campus scope.
5. Mark a payment as paid and confirm the paid/update trail fields change.
6. Open Payment Reports and apply campus, department, course, and class/semester filters.
7. Export the filtered payment report to PDF and Excel.
8. Open Finance Analytics and confirm the Paid vs Unpaid chart matches the filtered report totals.
9. Verify a Finance user with multi-campus assignment can only see receipts for assigned campuses.

## Tabsan.Lic UAT Steps

1. Start the console tool.
2. Generate a verification key.
3. Choose one of the available expiry choices (1 month, 1/2/3 years, Permanent).
4. Build a `.tablic` file.
5. Select the institution scope (School, College, University).
6. Set max users and allowed domain.
7. Confirm the tool writes the file and marks the key as license-generated.

## Acceptance Notes

- The license tool supports 1 month, 1 year, 2 years, 3 years, and Permanent expiry choices.
- The license payload carries institution-scope flags and the app applies them on activation.
- Import/export and index maintenance features are validated.
- User import templates are role-specific and aligned to current onboarding workflow.
- Finance payment workflows, report filters, and analytics scope are validated.
- The unit-test build for the main application passed during this session.
