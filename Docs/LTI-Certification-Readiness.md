# LTI Advantage Certification Readiness — Tabsan EduSphere (Platform)

Status: 24 October 2026. EduSphere is an LTI 1.3 / LTI Advantage **platform** (Plan M5). This document records what is
implemented, the evidence collected (automated tests and checks against the saLTIre test tool), what remains to be
checked before an official submission, and how 1EdTech certification is obtained.

## 1. What 1EdTech certification requires

- Certification is available to **1EdTech members** (Contributing, Affiliate or Alliance membership) and is included in
  the membership; active membership is required to keep it.
- The process has four steps: **Assess** readiness → **Test** with the 1EdTech certification suite (members only, as often
  as needed) → **Submit** the results for validation → **Receive** the certification (listed in the TrustEd Apps Directory).
- Products are **recertified every year**.
- Source: 1edtech.org/certification/get-certified (read 24 Oct 2026).

These steps need a 1EdTech membership and account owned by Tabsan; they cannot be completed from this repository.

## 2. Capabilities implemented

| Area | Specification | EduSphere implementation |
|---|---|---|
| Core launch | LTI 1.3 Core (OIDC third-party login, signed id_token) | `/lti/launch/{item}` → tool login initiation (`iss`, `login_hint`, `lti_message_hint`, `target_link_uri`, `client_id`, `lti_deployment_id`); `/lti/authorize` answers with an RS256 id_token (`kid`), single-use launch records, nonce replay table, exact redirect URIs |
| Claims | message type, version, deployment, target link, resource link, roles (LIS v2), context (+ type CourseOffering), tool_platform, launch_presentation (target, locale, return URL), custom, AGS/NRPS endpoints | `LtiLaunchService`; name/email by privacy level |
| Keys | Platform JWKS | `/lti/jwks`, rotation with retired keys kept 7 days |
| Deep Linking | Deep Linking 2.0 | `LtiDeepLinkingRequest` with `accept_types` [ltiResourceLink], `accept_presentation_document_targets` [iframe, window], `accept_multiple`, `accept_lineitem`, `auto_create`, `data`; signed response validated (tool JWKS/PEM, RS256, issuer = client ID, audience = platform, nonce once, `data` round trip) |
| AGS | Assignment and Grade Services 2.0 | client-credentials token with `private_key_jwt` assertion; line items CRUD (filters `resource_link_id`, `resource_id`, `tag`), scores, results; paging with `Link: rel="next"` |
| NRPS | Names and Role Provisioning Services 2.0 | memberships container with roles and status, `role` filter, paging with `Link: rel="next"`, privacy level |
| Registration | LTI Dynamic Registration 1.0 | `/lti/openid-configuration`, `/lti/register` (single-use registration token); tools start switched off for review |

## 3. Evidence

### 3.1 Automated tests
- Unit: `LtiTests`, `LtiDynamicRegistrationTests`, `LtiPagingTests` (crypto, claims, registration rules, paging).
- Integration (real API + SQL Server): `LtiIntegrationTests` — launch + AGS + NRPS end to end with an in-process tool,
  Deep Linking, registry validation and module gate, Dynamic Registration end to end, NRPS paging walk.
- Web-integration: `LtiWebIntegrationTests` — launches, relays (token, AGS, NRPS incl. the `Link` header), admin page,
  Dynamic Registration page.
- Playwright: `tests/playwright/tests/lti/lti.spec.ts`.

### 3.2 Checks with a real external tool — saLTIre (24 Oct 2026)
saLTIre (saltire.lti.app, ceLTIc Project, LTI-PHP 3.3.2) against the public test deployment:

| Check | Result |
|---|---|
| Dynamic Registration (saLTIre registration URL `https://saltire.lti.app/tool`) | Registered; tool created switched off with privacy Public, grade and roster services, deep linking; close message returned the portal to the tool list |
| Resource link launch (as administrator) | `LtiResourceLinkRequest`, LTI 1.3.0, RS256 — **Verification (strict mode): Passed** |
| Deep Linking request | `LtiDeepLinkingRequest` — **Verification (strict mode): Passed** |
| Deep Linking response (cancel with message) | Signed response accepted, tool message shown |
| OAuth 2 access token (client credentials, `private_key_jwt`) | Token issued for the AGS and NRPS scopes |
| NRPS memberships (limit 5) | 200, `application/vnd.ims.lti-nrps.v2.membershipcontainer+json`, **no validation errors**, `Link: rel="next"` |
| AGS line items: list / create / update / delete | 200 / 201 / 200 / 204, **no validation errors** |
| AGS score publish | 204; the score became the student's graded submission (8/10 with comment) |
| AGS results | 200, `resultcontainer+json`, **no validation errors** |

Found and fixed during these checks: NRPS and AGS containers honoured `limit` without a `rel="next"` link (tools could not
reach later pages), and the NRPS `role` filter / AGS results `user_id` filter were applied after the limit.

## 4. Before submitting for certification

1. **Stable platform address.** The issuer is `Lti:PlatformUrl`; tools store it. Use the institution's permanent https
   domain (a quick tunnel address changes when it restarts).
2. **Run the 1EdTech platform certification suite** for Core, Deep Linking, AGS and NRPS (and Dynamic Registration if
   offered) and fix any failures; keep the suite's report for the submission.
3. **Optional features not implemented** (state them if the suite asks): LTI platform storage (client-side postMessage
   storage — saLTIre reports "not offered by platform"); Deep Linking content types other than `ltiResourceLink`
   (`link`, `file`, `html`, `image` are reported as skipped); the `lis` person claim (sourcedId); Course Groups service.
4. **Learner launch** check with a student account (roles `Learner` + institution `Student`; privacy level applied).

## 5. Vendor tools (H5P, Zoom, Turnitin, Copyleaks)

These need the institution's own vendor accounts. For each tool:

1. In the vendor's admin console, find its LTI 1.3 set-up. If it offers a **registration URL** (Dynamic Registration),
   paste it on **External Tools › Register with the tool's registration URL**; otherwise create the tool manually with the
   values shown under "Values to give each tool" and enter the vendor's login, launch, redirect and JWKS URLs.
2. Review the new tool's privacy level and services, then turn it on and add it to a test course.
3. Launch as a teacher and as a student; for graded tools, check that a score reaches the gradebook; for roster-based
   tools, check the class list.
4. Record the results in the plan (Stage M5.6).

Similarity checking without LTI (Copyleaks direct API, built-in comparison) is covered by Plan M9.
