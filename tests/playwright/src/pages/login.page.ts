/**
 * Login Page Object Model.
 */
import { Page, Locator } from '@playwright/test';

export class LoginPage {
  readonly page: Page;
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;
  readonly mfaCodeInput: Locator;
  readonly mfaSubmitButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.usernameInput = page.locator('input[name="username"]');
    this.passwordInput = page.locator('input[name="password"]');
    this.submitButton = page.locator('button[type="submit"]');
    this.errorMessage = page.locator('.alert-danger');
    this.mfaCodeInput = page.locator('input[name="mfaCode"]');
    this.mfaSubmitButton = page.locator('button[type="submit"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/Login', { waitUntil: 'domcontentloaded' });
    await this.usernameInput.waitFor({ state: 'visible' });
  }

  async fillCredentials(username: string, password: string): Promise<void> {
    await this.usernameInput.fill(username);
    await this.passwordInput.fill(password);
  }

  async submit(): Promise<void> {
    await this.submitButton.click();
  }

  async login(username: string, password: string): Promise<void> {
    await this.fillCredentials(username, password);
    await this.submit();
  }

  async submitMfa(code: string): Promise<void> {
    await this.mfaCodeInput.waitFor({ state: 'visible', timeout: 10_000 });
    await this.mfaCodeInput.fill(code);
    await this.mfaSubmitButton.click();
  }

  async assertErrorVisible(): Promise<void> {
    const { expect } = await import('@playwright/test');
    await expect(this.errorMessage).toBeVisible({ timeout: 10_000 });
  }
}
