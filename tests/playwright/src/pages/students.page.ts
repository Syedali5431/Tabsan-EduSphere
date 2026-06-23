/**
 * Students Page Object Model – list, filter, view.
 */
import { Page, Locator, expect } from '@playwright/test';

export class StudentsPage {
  readonly page: Page;
  readonly table: Locator;
  readonly rows: Locator;
  readonly filterInstitution: Locator;
  readonly filterDepartment: Locator;
  readonly filterCourse: Locator;
  readonly filterSemester: Locator;
  readonly applyButton: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    this.page = page;
    this.table = page.locator('table, .data-table, [data-testid="students-table"]');
    this.rows = page.locator('table tbody tr, [data-testid="student-row"]');
    this.filterInstitution = page.locator('select[name="InstitutionId"], [data-testid="filter-institution"]');
    this.filterDepartment = page.locator('select[name="DepartmentId"], [data-testid="filter-department"]');
    this.filterCourse = page.locator('select[name="CourseId"], [data-testid="filter-course"]');
    this.filterSemester = page.locator('select[name="SemesterId"], select[name="ClassId"], [data-testid="filter-semester"]');
    this.applyButton = page.locator('button:has-text("Apply"), button:has-text("Filter"), [data-testid="apply-filters"]');
    this.emptyState = page.locator('.no-data, .empty-state, [data-testid="empty-state"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/Portal/Students', { waitUntil: 'domcontentloaded' });
    await this.table.waitFor({ state: 'visible', timeout: 15_000 });
  }

  async getRowCount(): Promise<number> {
    return await this.rows.count();
  }

  /**
   * Assert that the student table has at least `minRows` rows.
   */
  async assertHasRows(minRows: number = 1): Promise<void> {
    const count = await this.getRowCount();
    expect(count).toBeGreaterThanOrEqual(minRows);
  }

  /**
   * Check if a specific student name appears in the table.
   */
  async assertStudentVisible(studentName: string): Promise<void> {
    await expect(this.table.locator(`td:has-text("${studentName}"), [data-student-name="${studentName}"]`)).toBeVisible();
  }

  /**
   * Select an institution from the filter and apply.
   */
  async selectInstitution(institutionName: string): Promise<void> {
    await this.filterInstitution.selectOption({ label: institutionName });
    if (await this.applyButton.isVisible({ timeout: 1_000 }).catch(() => false)) {
      await this.applyButton.click();
      await this.page.waitForLoadState('networkidle', { timeout: 10_000 });
    }
  }

  async assertEmptyState(): Promise<void> {
    await expect(this.emptyState).toBeVisible({ timeout: 10_000 });
  }
}
