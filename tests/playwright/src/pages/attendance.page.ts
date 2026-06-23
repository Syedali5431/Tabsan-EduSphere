/**
 * Attendance Page Object Model.
 */
import { Page, Locator, expect } from '@playwright/test';

export class AttendancePage {
  readonly page: Page;
  readonly filterInstitution: Locator;
  readonly filterDepartment: Locator;
  readonly filterCourse: Locator;
  readonly filterSemester: Locator;
  readonly datePicker: Locator;
  readonly applyButton: Locator;
  readonly table: Locator;
  readonly rows: Locator;
  readonly presentRadio: Locator;
  readonly absentRadio: Locator;
  readonly saveButton: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.filterInstitution = page.locator('select[name="InstitutionId"], [data-testid="filter-institution"]');
    this.filterDepartment = page.locator('select[name="DepartmentId"], [data-testid="filter-department"]');
    this.filterCourse = page.locator('select[name="CourseId"], [data-testid="filter-course"]');
    this.filterSemester = page.locator('select[name="SemesterId"], select[name="ClassId"], [data-testid="filter-semester"]');
    this.datePicker = page.locator('input[type="date"], [data-testid="attendance-date"]');
    this.applyButton = page.locator('button:has-text("Apply"), button:has-text("Load"), [data-testid="apply-filters"]');
    this.table = page.locator('table, [data-testid="attendance-table"]');
    this.rows = page.locator('table tbody tr, [data-testid="attendance-row"]');
    this.presentRadio = page.locator('input[value="present"], input[value="Present"], [data-testid="present-radio"]');
    this.absentRadio = page.locator('input[value="absent"], input[value="Absent"], [data-testid="absent-radio"]');
    this.saveButton = page.locator('button:has-text("Save"), button:has-text("Submit"), [data-testid="save-attendance"]');
    this.successMessage = page.locator('.alert-success, .text-success, [data-testid="success-message"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/Portal/Attendance/Enter', { waitUntil: 'domcontentloaded' });
    await this.table.waitFor({ state: 'visible', timeout: 15_000 });
  }

  /**
   * Set attendance for the first N students as Present.
   */
  async markFirstNStudentsPresent(n: number): Promise<void> {
    const presentRadios = this.presentRadio;
    const count = await presentRadios.count();
    const toMark = Math.min(n, count);

    for (let i = 0; i < toMark; i++) {
      await presentRadios.nth(i).check();
    }
  }

  /**
   * Mark all visible students as Present.
   */
  async markAllPresent(): Promise<void> {
    const radios = this.presentRadio;
    const count = await radios.count();
    for (let i = 0; i < count; i++) {
      await radios.nth(i).check();
    }
  }

  async save(): Promise<void> {
    await this.saveButton.click();
    await expect(this.successMessage).toBeVisible({ timeout: 10_000 });
  }

  async getRowCount(): Promise<number> {
    return await this.rows.count();
  }

  async assertHasStudents(minCount: number = 1): Promise<void> {
    const count = await this.getRowCount();
    expect(count).toBeGreaterThanOrEqual(minCount);
  }
}
