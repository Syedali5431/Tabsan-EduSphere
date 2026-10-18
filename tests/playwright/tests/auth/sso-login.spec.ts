/**
 * Single Sign-On login page E2E tests (Plan M1 Stage M1.6).
 *
 * Expectations are driven by what the API reports for each scope, so the suite works on any
 * deployment: with no providers configured it checks that no SSO buttons appear; with providers
 * it checks every button, its target, and the hand-off to the identity provider.
 * The browser only loads the identity provider's sign-in page; no IdP credentials are used.
 *
 * Optional env: SSO_TENANT_CODE — also check /Login?tenant=CODE for a tenant-scoped setup.
 *
 * Tags: @auth, @sso
 */
import { test, expect, Page, APIRequestContext } from '@playwright/test';

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5181';
const TENANT = process.env.SSO_TENANT_CODE?.trim() || '';

type Provider = { provider: string; displayName: string };

async function providersFor(request: APIRequestContext, tenant: string): Promise<Provider[] | null> {
  const url = `${API_BASE_URL}/api/v1/sso/providers${tenant ? `?tenant=${encodeURIComponent(tenant)}` : ''}`;
  const response = await request.get(url);
  if (response.status() === 403) return null; // SSO module not active / not licensed
  expect(response.ok(), `GET ${url} returned ${response.status()}`).toBeTruthy();
  return (await response.json()) as Provider[];
}

async function openLogin(page: Page, tenant: string): Promise<void> {
  await page.goto(tenant ? `/Login?tenant=${encodeURIComponent(tenant)}` : '/Login', { waitUntil: 'domcontentloaded' });
  await expect(page.locator('input[name="username"]')).toBeVisible();
}

const scopes = TENANT ? ['', TENANT] : [''];

test.describe('Single sign-on login page @auth @sso', () => {
  test.use({ storageState: undefined });

  for (const tenant of scopes) {
    const label = tenant ? `tenant ${tenant}` : 'installation-wide';

    test(`shows exactly the enabled providers (${label})`, async ({ page, request }) => {
      const providers = await providersFor(request, tenant);
      await openLogin(page, tenant);

      const buttons = page.locator('[data-sso-provider]');
      if (!providers || providers.length === 0) {
        await expect(buttons).toHaveCount(0);
        return;
      }

      await expect(buttons).toHaveCount(providers.length);
      for (const p of providers) {
        const button = page.locator(`[data-sso-provider="${p.provider}"]`);
        await expect(button).toBeVisible();
        await expect(button).toHaveText(p.displayName);
        const href = await button.getAttribute('href');
        expect(href).toContain(`/Login/Sso/${p.provider.toLowerCase()}`);
        if (tenant) expect(href).toContain(`tenant=${encodeURIComponent(tenant)}`);
      }
      // Password sign-in stays available alongside SSO.
      await expect(page.locator('input[name="password"]')).toBeVisible();
    });

    test(`each provider button hands off to its identity provider (${label})`, async ({ page, request, baseURL }) => {
      const providers = await providersFor(request, tenant);
      test.skip(!providers || providers.length === 0, `no SSO providers configured (${label})`);

      const portalOrigin = new URL(baseURL!).origin;
      for (const p of providers!) {
        // The portal answers with a 302 to the IdP; assert on that redirect itself so the test
        // does not depend on the IdP being reachable from the test machine.
        await openLogin(page, tenant);
        const [redirect] = await Promise.all([
          page.waitForResponse(r => r.url().includes(`/Login/Sso/${p.provider.toLowerCase()}`) && r.status() === 302),
          page.locator(`[data-sso-provider="${p.provider}"]`).click(),
        ]);

        const idpUrl = new URL(redirect.headers()['location']);
        expect(idpUrl.origin, `${p.provider} should redirect away from the portal`).not.toBe(portalOrigin);
        expect(idpUrl.protocol, `${p.provider} should redirect to its IdP over https`).toBe('https:');
        const params = idpUrl.searchParams;
        if (p.provider === 'Saml') {
          expect(params.get('SAMLRequest')).toBeTruthy();
          expect(params.get('RelayState')).toBeTruthy();
        } else {
          expect(params.get('state')).toBeTruthy();
          expect(params.get('code_challenge_method')).toBe('S256');
          expect(params.get('redirect_uri')).toContain('/Login/SsoCallback');
        }

        // The in-flight sign-in is bound to this browser.
        const cookies = await page.context().cookies(portalOrigin);
        expect(cookies.some(c => c.name === '.Tabsan.EduSphere.Sso' && c.httpOnly)).toBeTruthy();

        await page.context().clearCookies();
      }
    });
  }

  test('a forged callback is rejected without contacting the API', async ({ page }) => {
    await page.goto('/Login/SsoCallback?code=forged&state=forged', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('.alert-danger')).toContainText('expired or is invalid');
    await expect(page.locator('input[name="username"]')).toBeVisible();
  });

  test('SAML service-provider metadata is published when SSO is active', async ({ request, page, baseURL }) => {
    const providers = await providersFor(request, '');
    test.skip(providers === null, 'SSO module not active');

    const response = await page.request.get(`${baseURL}/Login/SamlMetadata`);
    expect(response.ok()).toBeTruthy();
    expect(response.headers()['content-type']).toContain('samlmetadata+xml');
    expect(await response.text()).toContain('/Login/SamlAcs');
  });
});
