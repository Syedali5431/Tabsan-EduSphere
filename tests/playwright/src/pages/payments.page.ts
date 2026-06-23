/**
 * Payments Page Object Model.
 */
import { Page, Locator, expect } from '@playwright/test';

export class PaymentsPage {
  readonly page: Page;
  readonly filterInstitution: Locator;
  readonly filterStudent: Locator;
  readonly applyButton: Locator;
  readonly table: Locator;
  readonly rows: Locator;
  readonly emptyState: Locator;
  readonly totalAmount: Locator;
  readonly receiptLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.filterInstitution = page.locator('select[name="InstitutionId"], [data-testid="filter-institution"]');
    this.filterStudent = page.locator('select[name="StudentId"], [data-testid="student-filter"]');
    this.applyButton = page.locator('button:has-text("Apply"), button:has-text("Filter"), [data-testid="apply-filters"]');
    this.table = page.locator('table, [data-testid="payments-table"]');
    this.rows = page.locator('table tbody tr, [data-testid="payment-row"]');
    this.emptyState = page.locator('.no-data, .empty-state, [data-testid="empty-state"]');
    this.totalAmount = page.locator('[data-testid="total-amount"], .total-amount');
    this.receiptLink = page.locator('a:has-text("Receipt"), a:has-text("View"), [data-testid="receipt-link"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/Portal/Payments', { waitUntil: 'domcontentloaded' });
    await this.table.waitFor({ state: 'visible', timeout: 15_000 });
  }

  async getRowCount(): Promise<number> {
    return await this.rows.count();
  }

  async assertHasRecords(minCount: number = 1): Promise<void> {
    const count = await this.getRowCount();
    expect(count).toBeGreaterThanOrEqual(minCount);
  }

  /**
   * Filter payments by institution.
   */
  async filterByInstitution(institutionName: string): Promise<void> {
    await this.filterInstitution.selectOption({ label: institutionName });
    if (await this.applyButton.isVisible({ timeout: 1_000 }).catch(() => false)) {
      await this.applyButton.click();
      await this.page.waitForLoadState('networkidle', { timeout: 10_000 });
    }
  }

  /**
   * Verify that all visible payments belong to the expected institution.
   */
  async assertAllPaymentsScoped(institutionName: string): Promise<void> {
    const institutionCells = this.page.locator('td[data-institution], [data-testid="payment-institution"]');
    const count = await institutionCells.count();
    for (let i = 0; i < count; i++) {
      await expect(institutionCells.nth(i)).toContainText(institutionName, { ignoreCase: true });
    }
  }

  /**
   * Verify that payments from other institutions are NOT shown.
   */
  async assertNoCrossInstitutionData(otherInstitution: string): Promise<void> {
    // All visible rows should not contain the other institution name
    const tableText = await this.table.textContent();
    expect(tableText).not.toContain(otherInstitution);
  }
}
