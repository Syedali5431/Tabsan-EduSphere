/**
 * lti.spec.ts — Plan M5 LTI 1.3 platform, end to end in the browser. @lti
 *
 * Playwright plays the external tool: requests to https://lti-tool.playwright.test are answered here, so no real tool
 * is needed, but every portal step is real (tool registry, course card, OIDC login → id_token, iframe launch,
 * Deep Linking response signed with the key registered in the first test).
 *
 * Needs:
 *   LTI_OFFERING_ID                         a course offering the admin can manage (skipped when unset)
 *   SUPER_ADMIN_USERNAME / SUPER_ADMIN_PASSWORD (or ADMIN_USERNAME / ADMIN_PASSWORD)
 *   the "lti" module active and Lti:PlatformUrl set to the portal address
 * Optional: STUDENT_USERNAME / STUDENT_PASSWORD (a student enrolled in that course), SCREENSHOT_DIR.
 */
import { test, expect, Page, BrowserContext, Route } from '@playwright/test';
import { generateKeyPairSync, createSign, randomUUID } from 'crypto';

const OFFERING = process.env.LTI_OFFERING_ID;
const SHOTS = process.env.SCREENSHOT_DIR;
const TOOL = 'https://lti-tool.playwright.test';
const RUN = randomUUID().slice(0, 6);
const TOOL_NAME = `Playwright Tool ${RUN}`;
const { publicKey, privateKey } = generateKeyPairSync('rsa', { modulusLength: 2048 });

let clientId = '';

async function login(page: Page, user?: string, pass?: string) {
  await page.goto('/Login');
  await page.fill('#username', user ?? '');
  await page.fill('#password', pass ?? '');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.toLowerCase().endsWith('/login'), { timeout: 15000 });
}

const loginAdmin = (page: Page) => login(page,
  process.env.SUPER_ADMIN_USERNAME ?? process.env.ADMIN_USERNAME, process.env.SUPER_ADMIN_PASSWORD ?? process.env.ADMIN_PASSWORD);

async function shot(page: Page, name: string) {
  if (!SHOTS) return;
  // The layout's loading overlay fades out after each navigation.
  await page.locator('[data-page-loader]').waitFor({ state: 'hidden' }).catch(() => {});
  await page.screenshot({ path: `${SHOTS}/lti-${name}.png`, fullPage: true });
}

const b64url = (data: Buffer | string) => Buffer.from(data).toString('base64url');
const decode = (jwt: string) => JSON.parse(Buffer.from(jwt.split('.')[1], 'base64url').toString('utf8'));

function signJwt(payload: object): string {
  const head = b64url(JSON.stringify({ alg: 'RS256', typ: 'JWT', kid: 'playwright' }));
  const body = b64url(JSON.stringify(payload));
  const signer = createSign('RSA-SHA256');
  signer.update(`${head}.${body}`);
  return `${head}.${body}.${b64url(signer.sign(privateKey))}`;
}

const html = (body: string) => ({ status: 200, contentType: 'text/html', body: `<!doctype html><html><body>${body}</body></html>` });

/**
 * The fake tool: /login answers the third-party initiated login with an OIDC authentication request;
 * /launch receives the id_token. A Deep Linking request is answered with a signed selection of two items.
 */
async function playTool(context: BrowserContext, launches: any[]) {
  await context.route(`${TOOL}/**`, async (route: Route) => {
    const request = route.request();
    const url = new URL(request.url());
    const form = new URLSearchParams(request.method() === 'POST' ? request.postData() ?? '' : url.search);

    if (url.pathname === '/login') {
      const auth = new URL(`${form.get('iss')}/lti/authorize`);
      const params: Record<string, string> = {
        scope: 'openid', response_type: 'id_token', response_mode: 'form_post', prompt: 'none',
        client_id: form.get('client_id') ?? '', redirect_uri: `${TOOL}/launch`, login_hint: form.get('login_hint') ?? '',
        state: `state-${RUN}`, nonce: randomUUID(),
      };
      if (form.get('lti_message_hint')) params.lti_message_hint = form.get('lti_message_hint')!;
      for (const [k, v] of Object.entries(params)) auth.searchParams.set(k, v);
      return route.fulfill({ status: 302, headers: { location: auth.toString() } });
    }

    if (url.pathname === '/launch') {
      const idToken = form.get('id_token') ?? '';
      const claims = decode(idToken);
      launches.push({ state: form.get('state'), claims });
      const type = claims['https://purl.imsglobal.org/spec/lti/claim/message_type'];
      if (type !== 'LtiDeepLinkingRequest') {
        const title = claims['https://purl.imsglobal.org/spec/lti/claim/resource_link']?.title ?? '';
        return route.fulfill(html(`<h1 id="tool-launched">Tool launched</h1><p id="tool-item">${title}</p>`));
      }
      const settings = claims['https://purl.imsglobal.org/spec/lti-dl/claim/deep_linking_settings'];
      const now = Math.floor(Date.now() / 1000);
      const jwt = signJwt({
        iss: claims.aud, aud: claims.iss, iat: now, exp: now + 300, nonce: randomUUID(),
        'https://purl.imsglobal.org/spec/lti/claim/message_type': 'LtiDeepLinkingResponse',
        'https://purl.imsglobal.org/spec/lti/claim/version': '1.3.0',
        'https://purl.imsglobal.org/spec/lti/claim/deployment_id': claims['https://purl.imsglobal.org/spec/lti/claim/deployment_id'],
        'https://purl.imsglobal.org/spec/lti-dl/claim/data': settings.data,
        'https://purl.imsglobal.org/spec/lti-dl/claim/msg': 'Two activities added.',
        'https://purl.imsglobal.org/spec/lti-dl/claim/content_items': [
          { type: 'ltiResourceLink', title: `Chapter 1 ${RUN}`, url: `${TOOL}/launch?c=1`, lineItem: { scoreMaximum: 20, label: `Chapter 1 ${RUN}` } },
          { type: 'ltiResourceLink', title: `Chapter 2 ${RUN}`, url: `${TOOL}/launch?c=2`, iframe: { height: 600 } },
        ],
      });
      return route.fulfill(html(
        `<h1>Pick content</h1><form method="post" action="${settings.deep_link_return_url}"><input type="hidden" name="JWT" value="${jwt}">` +
        `<button id="tool-add" type="submit">Add 2 items</button></form>`));
    }
    return route.fulfill({ status: 404, body: 'not found' });
  });
}

test.describe('External tools (LTI 1.3) @lti', () => {
  test.describe.configure({ mode: 'serial' });
  test.skip(!OFFERING, 'LTI_OFFERING_ID is not set');

  test('admin registers a tool and sees the values to give the vendor', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await loginAdmin(page);
    await page.goto('/Portal/LtiTools');
    const platform = page.locator('[data-lti-platform]');
    await expect(platform).toBeVisible();
    await expect(platform).toContainText('/lti/authorize');
    await expect(platform).toContainText('/lti/jwks');

    await page.fill('#lti-name', TOOL_NAME);
    await page.fill('#lti-oidc', `${TOOL}/login`);
    await page.fill('#lti-target-link', `${TOOL}/launch`);
    await page.fill('#lti-redirects', `${TOOL}/launch`);
    await page.fill('#lti-deep-link', `${TOOL}/deep`);
    await page.fill('#lti-pem', publicKey.export({ type: 'spki', format: 'pem' }).toString());
    await page.selectOption('#lti-privacy', 'Public');
    await page.check('#lti-nrps');
    await page.click('[data-lti-tool-form] button.btn-primary');

    await expect(page.locator('[data-lti-client-id]')).toBeVisible();
    clientId = (await page.locator('[data-lti-client-id]').innerText()).trim();
    expect(clientId).toMatch(/^edusphere-/);
    await expect(page.locator('[data-lti-deployments]')).toContainText('Whole installation');

    // A course-only deployment: the form shows just the course picker.
    await page.selectOption('#dep-scope', 'CourseOffering');
    await expect(page.locator('#dep-department')).toBeHidden();
    await page.selectOption('#dep-offering', OFFERING!);
    await page.locator('[data-lti-deployment-form] button').click();
    await expect(page.locator('[data-lti-deployments] tbody tr')).toHaveCount(2);
    await expect(page.locator('[data-lti-deployments]')).toContainText('Course:');
    await expect(page.locator('[data-lti-tools]')).toContainText(TOOL_NAME);
    await shot(page, 'admin');
    expect(errors).toEqual([]);
  });

  test('teacher adds the tool to a course and it launches in a new tab and inside EduSphere', async ({ page, context }) => {
    const launches: any[] = [];
    await playTool(context, launches);
    await loginAdmin(page);
    await page.goto(`/Portal/LmsManage?offeringId=${OFFERING}`);
    const card = page.locator('#external-tools');
    await expect(card).toBeVisible();

    for (const [title, target] of [[`Reading ${RUN}`, 'window'], [`Lab ${RUN}`, 'iframe']]) {
      await card.locator('summary', { hasText: 'Add an external tool' }).click();
      await card.locator('#lti-tool').selectOption({ label: TOOL_NAME });
      await card.locator('#lti-title').fill(title);
      await card.locator('#lti-target').selectOption(target);
      if (target === 'window') await card.locator('#lti-max').fill('10');
      await card.locator('form button', { hasText: 'Add' }).click();
      await expect(card.locator('.list-group-item', { hasText: title })).toBeVisible();
    }
    await expect(card.locator('.list-group-item', { hasText: `Reading ${RUN}` })).toContainText('Graded');
    await shot(page, 'course-card');

    // New tab: login initiation → OIDC request → id_token posted to the tool.
    const [popup] = await Promise.all([
      context.waitForEvent('page'),
      card.locator('.list-group-item', { hasText: `Reading ${RUN}` }).locator('a', { hasText: 'Open' }).click(),
    ]);
    await expect(popup.locator('#tool-launched')).toBeVisible();
    await expect(popup.locator('#tool-item')).toHaveText(`Reading ${RUN}`);
    const launch = launches.at(-1);
    expect(launch.state).toBe(`state-${RUN}`);
    expect(launch.claims.aud).toBe(clientId);
    expect(launch.claims['https://purl.imsglobal.org/spec/lti/claim/version']).toBe('1.3.0');
    expect(launch.claims['https://purl.imsglobal.org/spec/lti/claim/context'].id).toBe(OFFERING);
    expect(launch.claims['https://purl.imsglobal.org/spec/lti-ags/claim/endpoint'].lineitem).toContain('/lti/ags/');
    expect(launch.claims['https://purl.imsglobal.org/spec/lti-nrps/claim/namesroleservice'].context_memberships_url).toContain('/lti/nrps/');
    await popup.close();

    // Inside EduSphere: the portal page frames the launch.
    await card.locator('.list-group-item', { hasText: `Lab ${RUN}` }).locator('a', { hasText: 'Open' }).click();
    await expect(page.locator('iframe[title="External tool"]')).toBeVisible();
    await expect(page.frameLocator('iframe[title="External tool"]').locator('#tool-launched')).toBeVisible();
    await expect(page.frameLocator('iframe[title="External tool"]').locator('#tool-item')).toHaveText(`Lab ${RUN}`);
    await shot(page, 'iframe');
  });

  test('teacher chooses content in the tool (Deep Linking) and it lands in the course', async ({ page, context }) => {
    const launches: any[] = [];
    await playTool(context, launches);
    await loginAdmin(page);
    await page.goto(`/Portal/LmsManage?offeringId=${OFFERING}`);
    await page.locator('#external-tools button', { hasText: `Choose content from ${TOOL_NAME}` }).click();
    await page.locator('#tool-add').click();

    await expect(page.locator('h1')).toHaveText('2 items added to the course');
    await expect(page.locator('main')).toContainText(`Chapter 1 ${RUN}`);
    expect(launches.at(-1).claims['https://purl.imsglobal.org/spec/lti/claim/message_type']).toBe('LtiDeepLinkingRequest');
    await shot(page, 'deep-link-done');

    await page.locator('a', { hasText: 'Back to the course' }).click();
    const card = page.locator('#external-tools');
    await expect(card.locator('.list-group-item', { hasText: `Chapter 1 ${RUN}` })).toContainText('Graded');
    await expect(card.locator('.list-group-item', { hasText: `Chapter 2 ${RUN}` })).toBeVisible();
  });

  test('students see the course items but not the teacher controls', async ({ page }) => {
    test.skip(!process.env.STUDENT_USERNAME, 'STUDENT_USERNAME is not set');
    await login(page, process.env.STUDENT_USERNAME, process.env.STUDENT_PASSWORD);
    await page.goto(`/Portal/CourseLms?offeringId=${OFFERING}`);
    const card = page.locator('#external-tools');
    await expect(card.locator('.list-group-item', { hasText: `Reading ${RUN}` })).toBeVisible();
    await expect(card).not.toContainText('Add an external tool');
    await expect(card.locator('form')).toHaveCount(0);
    await shot(page, 'student');
  });
});
