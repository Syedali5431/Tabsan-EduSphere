/**
 * Dashboard Page Object Model.
 */
import { Page, Locator, expect } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;
  readonly sidebar: Locator;
  readonly pageHeading: Locator;
  readonly widgets: Locator;
  readonly userMenu: Locator;

  constructor(page: Page) {
    this.page = page;
    this.sidebar = page.locator('.app-sidebar').first();
    this.pageHeading = page.locator('h1, .page-title, [data-testid="page-heading"]').first();
    this.widgets = page.locator('.widget, .dashboard-card, [data-testid^="widget-"], .card.dashboard');
    this.userMenu = page.locator('.user-menu, .profile-dropdown, [data-testid="user-menu"]');
  }

  async waitForLoad(): Promise<void> {
    await expect(this.sidebar).toBeVisible({ timeout: 15_000 });
    await expect(this.pageHeading).toBeVisible({ timeout: 10_000 });
  }

  async assertWidgetsVisible(minCount: number = 1): Promise<void> {
    const count = await this.widgets.count();
    expect(count).toBeGreaterThanOrEqual(minCount);
  }

  async getUserDisplayName(): Promise<string> {
    const name = await this.userMenu.textContent();
    return name?.trim() || '';
  }

  /**
   * Navigate to a module by its sidebar link.
   */
  async navigateTo(label: string): Promise<void> {
    const link = this.sidebar.locator(`a:has-text("${label}"), button:has-text("${label}")`).first();
    await link.click();
    await this.page.waitForLoadState('networkidle', { timeout: 10_000 });
  }
}
