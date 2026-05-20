# Site Acceptance Testing (SAT)

## Purpose
Site acceptance testing verifies the deployed application behaves correctly in its target environment after deployment.

## EduSphere SAT Steps

1. Deploy the API and web application to the target host.
2. Confirm the application starts cleanly and health checks are green.
3. Upload a valid license file from Tabsan.Lic.
4. Confirm the current license status is active.
5. Confirm the active host matches the allowed domain when one is configured.
6. Verify the institution policy stored in the application matches the license scope.
7. Confirm login and role-based navigation continue to work.
8. Confirm student lifecycle actions still function after deployment.
9. Validate user import templates in User Import Sheets:
	- faculty-admin-import-template.csv
	- students-import-template.csv
10. Test import/export (CSV, PDF, Excel) features for users and timetables.
11. Run Scripts/01-Schema-Current.sql through Scripts/05-PostDeployment-Checks.sql in order for site deployment verification.

## Finance SAT Steps

1. Sign in as a Finance user on the deployed environment.
2. Confirm Payments, Payment Reports, Analytics, and Theme Settings are available, and academic modules are hidden or blocked.
3. Open Payments and confirm the assigned-campus receipt list loads successfully.
4. Update a payment record and mark one receipt as paid.
5. Confirm the latest payment state and update trail are reflected after refresh.
6. Open Payment Reports and apply campus, department, course, and class/semester filters.
7. Export the filtered payment report to PDF and Excel.
8. Open Finance Analytics and confirm the Paid vs Unpaid chart matches the filtered payment scope.
9. Verify multi-campus Finance access remains restricted to assigned campuses only.

## Tabsan.Lic SAT Steps

1. Deploy the standalone Tabsan.Lic tool to the operator workstation.
2. Confirm the database file is created in `%APPDATA%/Tabsan/tabsan_lic.db`.
3. Generate a key and build a license file.
4. Confirm the file can be uploaded successfully to the deployed EduSphere environment.
5. Confirm the same license file is rejected if the domain binding does not match.

## Exit Criteria

- Deployed services respond successfully.
- License activation succeeds only in the intended environment.
- Institution scope and user limits remain enforced after deployment.
- Import/export and index maintenance features are validated.
- Role-specific user import templates are available and validated.
- Finance access boundaries, payment flows, and report filtering remain correct after deployment.
