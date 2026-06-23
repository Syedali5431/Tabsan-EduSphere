/**
 * Certificates Page Object Model.
 */
import { Page, Locator, expect } from '@playwright/test';

export class CertificatesPage {
  readonly page: Page;
  readonly certificateTypeDropdown: Locator;
  readonly studentFilter: Locator;
  readonly generateButton: Locator;
  readonly preview: Locator;
  readonly downloadButton: Locator;
  readonly eligibilityWarning: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.certificateTypeDropdown = page.locator(
      'select[name="CertificateType"], [data-testid="certificate-type"]',
    );
    this.studentFilter = page.locator(
      'select[name="StudentId"], [data-testid="student-filter"], input[data-testid="student-search"]',
    );
    this.generateButton = page.locator(
      'button:has-text("Generate"), button:has-text("Preview"), [data-testid="generate-certificate"]',
    );
    this.preview = page.locator('[data-testid="certificate-preview"], .certificate-preview, iframe, embed');
    this.downloadButton = page.locator(
      'button:has-text("Download"), a:has-text("Download"), [data-testid="download-certificate"]',
    );
    this.eligibilityWarning = page.locator(
      '.alert-warning, .eligibility-warning, [data-testid="eligibility-warning"]',
    );
    this.successMessage = page.locator('.alert-success, .text-success, [data-testid="success-message"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/Portal/Certificates', { waitUntil: 'domcontentloaded' });
  }

  /**
   * Select a certificate type from the dropdown.
   */
  async selectCertificateType(type: string): Promise<void> {
    await this.certificateTypeDropdown.selectOption({ label: type });
  }

  /**
   * Select a student from the filter.
   */
  async selectStudent(studentName: string): Promise<void> {
    await this.studentFilter.selectOption({ label: studentName });
  }

  async generate(): Promise<void> {
    await this.generateButton.click();
    // Wait for either preview or eligibility warning
    await Promise.race([
      expect(this.preview).toBeVisible({ timeout: 15_000 }),
      expect(this.eligibilityWarning).toBeVisible({ timeout: 15_000 }),
    ]).catch(() => {
      // Continue — either outcome is valid
    });
  }

  /**
   * Assert the certificate preview is shown.
   */
  async assertPreviewVisible(): Promise<void> {
    await expect(this.preview).toBeVisible({ timeout: 15_000 });
  }

  /**
   * Assert an eligibility warning is shown (student doesn't qualify).
   */
  async assertEligibilityWarning(): Promise<void> {
    await expect(this.eligibilityWarning).toBeVisible({ timeout: 10_000 });
  }

  /**
   * Assert the Degree certificate option is available.
   */
  async assertDegreeAvailable(): Promise<void> {
    await expect(
      this.certificateTypeDropdown.locator('option:has-text("Degree")'),
    ).toBeAttached({ timeout: 5_000 });
  }

  /**
   * Assert the Degree certificate is NOT available (School/College context).
   */
  async assertDegreeNotAvailable(): Promise<void> {
    await expect(
      this.certificateTypeDropdown.locator('option:has-text("Degree")'),
    ).not.toBeAttached({ timeout: 5_000 });
  }
}
