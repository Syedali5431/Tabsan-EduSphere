# Tabsan EduSphere — Public REST API

Plan M, Phase M14.3. The same API the portal and the mobile app use is open to integrations: student information
systems, timetabling and reporting tools, scripts.

## Reference

- **OpenAPI 3 description:** `GET {api}/swagger/v1/swagger.json`; interactive reference at `{api}/swagger` (enabled in
  Development, or in other environments with `AppSettings:EnableSwagger = true`). A snapshot of this version is kept at
  [`Docs/api/openapi-v1.json`](api/openapi-v1.json).
- Every route starts with `/api/v1/`.

## Authentication

| Caller | How |
|---|---|
| Integrations | A **personal access token**: an Admin or SuperAdmin opens **Settings › API Tokens**, creates a token (name, validity up to 365 days, optional *read only*), copies it once and sends `Authorization: Bearer esk_…`. |
| Interactive apps | `POST /api/v1/auth/login` → `accessToken` (JWT, 15 minutes) and `refreshToken` (`POST /api/v1/auth/refresh`). |

A token acts as its owner: the same role, tenant/campus scope and permissions as when that user signs in. Recommended:
create a dedicated integration user per system, with only the role it needs, and give each integration its own token.

- **Read-only tokens** can only `GET`/`HEAD`; other methods answer `403`.
- Tokens cannot create or revoke tokens (`403`); that needs a sign-in.
- Revoked, expired or unknown tokens answer `401`. Revocation applies within a minute on every API instance.
- Only a SHA-256 hash is stored; a lost token cannot be shown again — revoke it and create a new one.
- Which roles may create tokens, the longest lifetime and the number per user: `ApiTokens:AllowedRoles`, `MaxLifetimeDays`,
  `MaxTokensPerUser`.

```bash
curl -H "Authorization: Bearer esk_XXXXXXXX" https://api.your-institution.edu/api/v1/course/offerings
```

## Conventions

| Topic | Rule |
|---|---|
| Format | JSON, camelCase; null fields are omitted. Dates are UTC ISO 8601. Ids are GUIDs. |
| Errors | `4xx`/`5xx` with `{ "message": "…" }` (validation problems: `{ "errors": { "field": ["…"] } }`). |
| Language | `Accept-Language: ur` or `ar` returns messages and validation errors in Urdu or Arabic. |
| Paging | List endpoints that can grow take `page` and `pageSize` (where offered) and return the page's items; mobile lists are bounded. |
| Files | Uploads are `multipart/form-data`; downloads stream with their content type. |
| Idempotency | `PUT` and `DELETE` are idempotent; repeat a `POST` only after checking the first one failed. |

## Limits

Requests are limited in sliding one-minute windows (`RateLimiting` settings): per API token (default 1,200/min), per
signed-in user (600/min), and per address without sign-in (300/min; addresses and ranges in `RateLimiting:ExemptIps` are exempt — by default the API server itself, so add the portal server when it runs elsewhere). Over the limit
the API answers `429 Too Many Requests` with `Retry-After` (seconds). Upload sizes are limited per endpoint (5 MB by
default; larger where documented, e.g. SCORM packages).

## Versioning and deprecation

- `v1` changes only by **adding**: new endpoints, new optional request fields, new response fields. Clients must ignore
  fields they do not know.
- Breaking changes get a new prefix (`/api/v2/`) and `v1` stays available for at least 12 months after `v2` is released.
- Deprecated endpoints are announced in the release notes and the OpenAPI description (`deprecated: true`) at least two
  minor releases before they are removed.

## Common integration tasks

| Task | Endpoints |
|---|---|
| Users and students | `api/v1/user-import` (bulk CSV), `api/v1/student`, `api/v1/admin-user` |
| Courses and enrolment | `api/v1/course`, `api/v1/course/offerings`, `api/v1/enrollment` |
| Results and transcripts | `api/v1/result` (by offering, publish, my-results), `api/v1/result/transcript/{studentProfileId}` |
| Attendance | `api/v1/attendance` (bulk, by offering) |
| Learning activity | `api/v1/assignment`, `api/v1/quiz`, `api/v1/lms` (modules), `api/v1/scorm`, `api/v1/xapi/statements` (LRS) |
| Mobile and notifications | `api/v1/mobile/*`, `api/v1/notification` |

The OpenAPI description lists every endpoint with its parameters, request body and roles.
