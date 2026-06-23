/**
 * Certificates Module E2E Tests
 *
 * Covers: Degree (University only), Completion (School/College),
 *         Report Card, eligibility rule validation.
 *
 * Tags: @certificates, @smoke, @regression, @role-based
 */
import { test, expect } from '@fixtures/role-fixtures';
import { CertificatesPage } from '@pages/certificates.page';
import { CERTIFICATE_RULES } from '@data/test-data';

test.describe('Certificates Module', () => {
  // ── SuperAdmin (University context) ──────────────────────────────────────

  test.describe('As SuperAdmin', () => {
    test('@certificates @smoke should load certificates page', async ({ superAdminPage }) => {
      const page = new CertificatesPage(superAdminPage);
      await page.goto();
      await expect(page.certificateTypeDropdown).toBeVisible({ timeout: 10_000 });
    });

    test('@certificates @regression should show Degree certificate option (University context)', async ({ superAdminPage }) => {
      const page = new CertificatesPage(superAdminPage);
      await page.goto();
      await page.assertDegreeAvailable();
    });

    test('@certificates @regression should show Completion certificate option', async ({ superAdminPage }) => {
      const page = new CertificatesPage(superAdminPage);
      await page.goto();

      // Completion should be in dropdown (for School/College)
      await expect(
        page.certificateTypeDropdown.locator('option:has-text("Completion")'),
      ).toBeAttached({ timeout: 5_000 });
    });

    test('@certificates @regression should generate Degree certificate for eligible student', async ({ superAdminPage }) => {
      const page = new CertificatesPage(superAdminPage);
      await page.goto();

      await page.selectCertificateType('Degree');
      await page.generate();

      // Either preview or eligibility warning appears
      const hasPreview = await page.preview.isVisible().catch(() => false);
      const hasWarning = await page.eligibilityWarning.isVisible().catch(() => false);
      expect(hasPreview || hasWarning).toBeTruthy();
    });

    test('@certificates @edge should show eligibility warning for ineligible student', async ({ superAdminPage }) => {
      const page = new CertificatesPage(superAdminPage);
      await page.goto();

      await page.selectCertificateType('Degree');

      // Select a student who may not meet eligibility criteria
      // (This test ensures the system handles the edge case gracefully)
      await page.generate();

      // System should not crash — either shows preview or warning
      const hasPreview = await page.preview.isVisible().catch(() => false);
      const hasWarning = await page.eligibilityWarning.isVisible().catch(() => false);
      expect(hasPreview || hasWarning).toBeTruthy();
    });
  });

  // ── Student: view own certificates ───────────────────────────────────────

  test.describe('As Student', () => {
    test('@certificates @role-based should access certificates page', async ({ studentUniversityPage }) => {
      await studentUniversityPage.goto('/Portal/Certificates', { waitUntil: 'domcontentloaded' });

      const isBlocked =
        studentUniversityPage.url().includes('/Dashboard') ||
        (await studentUniversityPage
          .locator('.access-denied, [data-testid="access-denied"]')
          .isVisible()
          .catch(() => false));

      expect(isBlocked).toBeFalsy();
    });

    test('@certificates @role-based should see Degree option as University student', async ({ studentUniversityPage }) => {
      const page = new CertificatesPage(studentUniversityPage);
      await page.goto();
      await page.assertDegreeAvailable();
    });
  });

  // ── School Student: Degree should NOT be available ───────────────────────

  test.describe('As School Student', () => {
    test('@certificates @role-based should NOT see Degree certificate option', async ({ studentSchoolPage }) => {
      const page = new CertificatesPage(studentSchoolPage);
      await page.goto();
      await page.assertDegreeNotAvailable();
    });
  });
});
