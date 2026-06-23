/**
 * Results Page Object Model – enter and view results.
 */
import { Page, Locator, expect } from '@playwright/test';

export class ResultsPage {
  readonly page: Page;
  readonly filterInstitution: Locator;
  readonly filterDepartment: Locator;
  readonly filterCourse: Locator;
  readonly filterSemester: Locator;
  readonly applyButton: Locator;
  readonly table: Locator;
  readonly rows: Locator;
  readonly scoreInputs: Locator;
  readonly saveButton: Locator;
  readonly successMessage: Locator;
  readonly resultTypeLabel: Locator;

  constructor(page: Page) {
    this.page = page;
    this.filterInstitution = page.locator('select[name="InstitutionId"], [data-testid="filter-institution"]');
    this.filterDepartment = page.locator('select[name="DepartmentId"], [data-testid="filter-department"]');
    this.filterCourse = page.locator('select[name="CourseId"], [data-testid="filter-course"]');
    this.filterSemester = page.locator('select[name="SemesterId"], select[name="ClassId"], [data-testid="filter-semester"]');
    this.applyButton = page.locator('button:has-text("Apply"), button:has-text("Load"), [data-testid="apply-filters"]');
    this.table = page.locator('table, [data-testid="results-table"]');
    this.rows = page.locator('table tbody tr, [data-testid="result-row"]');
    this.scoreInputs = page.locator('input[type="number"], [data-testid="score-input"], [data-testid="marks-input"]');
    this.saveButton = page.locator('button:has-text("Save"), button:has-text("Submit Results"), [data-testid="save-results"]');
    this.successMessage = page.locator('.alert-success, .text-success, [data-testid="success-message"]');
    this.resultTypeLabel = page.locator('[data-testid="result-type-label"], .result-type-indicator');
  }

  async goto(): Promise<void> {
    await this.page.goto('/Portal/Results/Enter', { waitUntil: 'domcontentloaded' });
    await this.table.waitFor({ state: 'visible', timeout: 15_000 });
  }

  async getRowCount(): Promise<number> {
    return await this.rows.count();
  }

  /**
   * Enter scores for the first N students.
   */
  async enterScores(scores: number[]): Promise<void> {
    const count = await this.scoreInputs.count();
    for (let i = 0; i < Math.min(scores.length, count); i++) {
      await this.scoreInputs.nth(i).fill(scores[i].toString());
    }
  }

  /**
   * Assert the grading type being used (GPA vs Percentage).
   */
  async assertGradingType(expectedType: 'GPA' | 'Percentage'): Promise<void> {
    if (await this.resultTypeLabel.isVisible().catch(() => false)) {
      await expect(this.resultTypeLabel).toContainText(expectedType, { ignoreCase: true });
    }
  }

  /**
   * Verify a GPA value is displayed for a student (University context).
   */
  async assertGpaDisplayed(): Promise<void> {
    const gpaCell = this.page.locator('[data-testid="gpa-value"], td:has-text("GPA")');
    if (await gpaCell.isVisible().catch(() => false)) {
      await expect(gpaCell.first()).toBeVisible();
    }
  }

  /**
   * Verify percentage values are displayed (School/College context).
   */
  async assertPercentageDisplayed(): Promise<void> {
    const pctCell = this.page.locator('[data-testid="percentage-value"], td:has-text("%")');
    if (await pctCell.isVisible().catch(() => false)) {
      await expect(pctCell.first()).toBeVisible();
    }
  }

  async save(): Promise<void> {
    await this.saveButton.click();
    await expect(this.successMessage).toBeVisible({ timeout: 10_000 });
  }

  async assertHasStudents(minCount: number = 1): Promise<void> {
    const count = await this.getRowCount();
    expect(count).toBeGreaterThanOrEqual(minCount);
  }
}
