# Single Sign-On (SSO) Setup Guide

Version: 1.0
Date: 19 October 2026
Audience: SuperAdmins and the institution's identity / IT administrator
Applies to: Plan M1 (Stages M1.1–M1.5)

With SSO, students and staff sign in to Tabsan EduSphere with the account they already use at their institution: Microsoft 365 / Entra ID, Google Workspace, any other OpenID Connect provider (Okta, Keycloak, Auth0 …) or a SAML 2.0 identity provider (Entra ID SAML, ADFS, Shibboleth …).

SSO **signs people in to existing EduSphere accounts**. It never creates accounts. Each person is matched once (by an existing link or, if you allow it, by verified email) and the link is remembered.

---

## 1. Before you start

| Check | Why |
|---|---|
| The licence includes **Single Sign-On** and the module is **active** (System Settings → Module Composition) | Every SSO page and endpoint is switched off while the module is inactive. |
| The portal is published on a **stable HTTPS address** (e.g. `https://portal.uni.edu`) | Identity providers only send people back to the exact addresses you register. A temporary tunnel address that changes on every restart breaks SSO. |
| The API server can make **outbound HTTPS** calls to the identity provider | EduSphere downloads the provider's configuration / metadata and signing keys, and exchanges sign-in codes. |
| Server clocks are correct (NTP) | Tokens and assertions are rejected if the clock is more than 2 minutes off. |
| EduSphere accounts have the **same email** as the institution accounts (if you will use automatic linking) | Automatic linking matches on verified email. |

### 1.1 Configuration values (done once by whoever deploys EduSphere)

**Portal (`Tabsan.EduSphere.Web`), only needed behind a reverse proxy or tunnel that hides the public address:**

```json
"Sso": { "PublicBaseUrl": "https://portal.uni.edu" }
```

If the SSO Settings page shows an internal address (for example `http://localhost:5063`) in "Values to register", set this.

**API (`Tabsan.EduSphere.API`), recommended in production:** list the exact portal return addresses the API may hand to identity providers. Include **both** the OIDC callback and the SAML ACS for **every** public portal address:

```json
"AuthSecurity": {
  "Sso": {
    "AllowedRedirectUris": [
      "https://portal.uni.edu/Login/SsoCallback",
      "https://portal.uni.edu/Login/SamlAcs"
    ]
  }
}
```

If the list is empty, any `https://…/Login/SsoCallback` and `https://…/Login/SamlAcs` address is accepted (plain `http` only on `localhost`).

---

## 2. Open the SSO Settings page

Sign in as **SuperAdmin** → **System Settings → Single Sign-On**.

* **Configuring:** leave the tenant code blank for providers that apply to the whole installation (they appear on the normal login page). Enter a tenant code to configure providers for one tenant (they appear on `/Login?tenant=CODE`, which you can share with that institution's users).
* **Values to register at your identity provider:** copy these into the identity provider in the steps below.

| Value | Used for |
|---|---|
| OpenID Connect redirect URI | Microsoft, Google, other OIDC: `https://<portal>/Login/SsoCallback` |
| SAML entity ID (audience) | SAML: `https://<portal>/Login/SamlMetadata` (with `?tenant=CODE` for a tenant) |
| SAML ACS URL (HTTP-POST) | SAML: `https://<portal>/Login/SamlAcs` |
| SAML metadata | SAML: a downloadable file / URL the IdP can import |

Each provider card has: **Button label** (text on the login page), connection details, **Allowed email domains**, the policy switches (section 7), **Save**, **Test connection** and **Remove**. Client secrets are stored encrypted and are never shown again ("Stored — leave blank to keep").

---

## 3. Microsoft Entra ID (Microsoft 365), OpenID Connect

In the **Microsoft Entra admin center** (or Azure portal → Microsoft Entra ID):

1. **App registrations → New registration**
   * Name: `Tabsan EduSphere`
   * Supported account types: **Accounts in this organizational directory only (Single tenant)**
   * Redirect URI: platform **Web**, value = the *OpenID Connect redirect URI* from EduSphere.
2. Open the new app → **Overview**: copy the **Application (client) ID** and the **Directory (tenant) ID**.
3. **Certificates & secrets → Client secrets → New client secret**. Copy the secret **Value** immediately (it is shown once). Note the expiry date; you will need to renew it.
4. *(Recommended)* **Token configuration → Add optional claim → ID → `email`**. Without it EduSphere uses the sign-in name (UPN), which is usually the same address.
5. **API permissions**: the defaults (`openid`, `profile`, `email`, `User.Read`) are enough. Grant admin consent if your tenant requires it.

In EduSphere (Microsoft Entra ID card):

| Field | Value |
|---|---|
| Client ID | Application (client) ID |
| Client secret | the secret Value |
| Authority (your directory) | `https://login.microsoftonline.com/<Directory (tenant) ID>/v2.0` |
| Allowed email domains | e.g. `uni.edu, staff.uni.edu` |

> `common`, `organizations` and `consumers` authorities are refused on purpose: they would accept accounts from any Microsoft directory, whose email addresses your institution does not control.

**Save → Test connection** ("Connected: the OpenID configuration … was loaded"), then switch **Show this provider on the login page** on and **Save**.

---

## 4. Google Workspace, OpenID Connect

In **Google Cloud Console** (signed in with a Workspace admin account, in a project owned by your organization):

1. **APIs & Services → OAuth consent screen**: user type **Internal** (only your Workspace users can sign in). App name `Tabsan EduSphere`, support email, save.
2. **APIs & Services → Credentials → Create credentials → OAuth client ID**
   * Application type: **Web application**
   * Authorized redirect URIs: the *OpenID Connect redirect URI* from EduSphere.
3. Copy the **Client ID** and **Client secret**.

In EduSphere (Google Workspace card): enter Client ID and Client secret (the issuer is fixed to `https://accounts.google.com`). **Always set Allowed email domains** to your Workspace domains, because Google also has personal accounts. Save, Test connection, enable, Save.

---

## 5. Other OpenID Connect providers (Okta, Keycloak, Auth0, …)

Create a **confidential web application / client** using the **authorization code** flow:

* Redirect / callback URL: the *OpenID Connect redirect URI* from EduSphere.
* Scopes: `openid profile email`.
* PKCE: allowed or required. EduSphere always sends an S256 challenge.

In EduSphere (Other OpenID Connect IdP card): Client ID, Client secret and **Issuer / authority URL** (the issuer whose `/.well-known/openid-configuration` is published, e.g. `https://keycloak.uni.edu/realms/uni`). Automatic linking only trusts emails the provider marks `email_verified = true`.

---

## 6. SAML 2.0 identity providers

EduSphere is a SAML service provider using **HTTP-Redirect** for the request and **HTTP-POST** for the response. Responses **must be signed** (assertion and/or response).

### 6.1 Entra ID (SAML)

1. **Enterprise applications → New application → Create your own application** → "Integrate any other application you don't find in the gallery".
2. **Single sign-on → SAML → Basic SAML Configuration**: Identifier (Entity ID) = *SAML entity ID*; Reply URL (ACS) = *SAML ACS URL*. (Or **Upload metadata file** with the downloaded SP metadata.)
3. **Attributes & Claims**: the defaults are fine. EduSphere uses the object ID (`…/claims/objectidentifier`) as the stable identity and `…/claims/emailaddress` as email.
4. **Users and groups**: assign who may sign in.
5. Copy **App Federation Metadata Url** → EduSphere SAML card → **IdP metadata URL**.

### 6.2 ADFS

1. **Relying Party Trusts → Add Relying Party Trust** → import the EduSphere SP metadata (URL or file).
2. Claim issuance rules: send **E-Mail-Addresses → E-Mail Address**, and a **persistent Name ID** (e.g. from objectGUID or a transform rule).
3. IdP metadata URL for EduSphere: `https://<adfs-host>/FederationMetadata/2007-06/FederationMetadata.xml`.

### 6.3 Shibboleth / eduGAIN-style IdPs

Register the EduSphere SP metadata with the IdP and release at least one stable identifier (**eduPersonTargetedID**, **pairwise-id** via persistent NameID, or **eduPersonPrincipalName**) plus **mail** and **displayName**. Enter the IdP's metadata URL in EduSphere.

### 6.4 How SAML attributes are used

| EduSphere uses | Taken from (first found) |
|---|---|
| Stable identity | Entra `objectidentifier` → persistent NameID → eduPersonTargetedID → eduPersonPrincipalName (a **transient** NameID alone is rejected) |
| Email | `…/claims/emailaddress`, `urn:oid:0.9.2342.19200300.100.1.3` (mail), `mail`, `email`, or an emailAddress-format NameID |
| Display name | `…/claims/displayname`, `urn:oid:2.16.840.1.113730.3.1.241`, `displayName`, `name` |
| "Already did MFA" | AuthnContextClassRef REFEDS MFA / `multipleauthn` / two- or multi-factor classes, or Entra `authnmethodsreferences` |

### 6.5 Signed requests and encrypted assertions

Some identity providers require signed authentication requests or send encrypted assertions. EduSphere has its own
service-provider certificate for both, created on first use (`AuthSecurity:Sso:SpCertificateFile`, default
`<app-data>/Tabsan/saml-sp-certificate.json`; back it up and give every API server the same file). The SP metadata
(**Download SP metadata**) always contains this certificate for signing and encryption, so an IdP that imports the metadata
can encrypt assertions straight away — EduSphere decrypts them.

* **Sign authentication requests** — turn on when the IdP requires signed AuthnRequests (the metadata then says so).
* **Require encrypted assertions** — EduSphere refuses responses whose assertion is not encrypted.

Download the metadata again after changing these switches and update it at the IdP.

---

## 7. Sign-in policies (per provider card)

| Switch | Effect | Recommendation |
|---|---|---|
| **Show this provider on the login page** | Enables the provider. It cannot be enabled until the required fields are complete. | On once Test connection succeeds. |
| **Link … same verified email on first sign-in** | A person without a link is matched to the active EduSphere account (same tenant) with that verified email, and the link is saved. An account already linked to a *different* identity is never re-linked. | On for the initial roll-out; off afterwards if you prefer explicit links. |
| **Skip the EduSphere authenticator code when the IdP already did MFA** | Users with EduSphere TOTP are not asked for their code again when the provider reports MFA. | On if your IdP enforces MFA. |
| **Require single sign-on (turn off password login)** | Users in this scope can no longer sign in with a password; they see "Your institution requires single sign-on". **SuperAdmins always keep password sign-in** as a break-glass route. Ignored automatically while the SSO module is inactive. | Turn on last, after users have linked accounts. |
| **Allowed email domains** | Sign-ins whose email is outside these domains are refused. Blank allows any domain. | Always set for Google and multi-domain setups. |

**Turning on "Require single sign-on":** password sessions that started *before* the switch keep working until they expire. To cut over immediately, revoke sessions from Account Security.

### 7.1 Attribute mapping (institution ID and role)

Optional fields on each provider card. Use only claims or attributes the identity provider controls (people cannot edit them).

| Field / switch | Effect | Examples |
|---|---|---|
| **Institution ID claim or attribute** + **Link … same institution ID on first sign-in** | A person without a link is matched to the one active account of the tenant whose **registration number** (students) or **username** equals the asserted value, and the link is saved. No match or several matches → not linked. | OIDC `employee_id` (Entra optional claim), SAML `urn:oid:2.16.840.1.113730.3.1.3` (employeeNumber) or `schacPersonalUniqueCode` |
| **Role claim or attribute** + **Role mapping** | Translates the IdP's role values into EduSphere roles: `student=Student; faculty=Faculty; staff=Admin|Finance`. Values with a scope (`faculty@uni.edu`) also match on the part before "@". Roles: Student, Faculty, Admin, Finance, Parent. | OIDC `roles` (Entra app roles), SAML eduPersonAffiliation `urn:oid:1.3.6.1.4.1.5923.1.1.1.1` or eduPersonScopedAffiliation `…1.9` |
| **Refuse sign-in when the identity provider's role does not match** | The sign-in is refused (and no new link is made) unless an asserted value maps to the account's EduSphere role. The person sees "Your identity provider reports a role that does not match this EduSphere account". | Turn on once the mapping covers every group that should sign in. |

EduSphere never creates accounts from SSO; the mapping links and checks existing accounts only.

---

## 8. Linking and unlinking accounts

* **Users** see their linked accounts under **Two-Factor Authentication → Linked sign-in accounts** and can **Unlink** them. Unlinking the *only* link is refused while "Require single sign-on" applies to them, so nobody locks themselves out.
* **Admins / SuperAdmins** can list or remove a user's links through the API (`GET/DELETE /api/v1/sso/links/users/{userId}[/{provider}]`), for example when the wrong account was linked.
* **Removing a provider** keeps existing links; they work again if the provider is added back.

---

## 9. Roll-out checklist

1. Configure the provider, **Save**, **Test connection**.
2. Leave "Require single sign-on" **off**; turn on "Show on login page" and (optionally) automatic linking.
3. Sign in with a **test account** that exists in EduSphere; check you land on the right dashboard.
4. Check the login-activity records (`GET /api/v1/login-activity`, field `authMethod` = `sso:<Provider>`) and the **Audit log** (`Login`, `SsoAccountLinked`).
5. Announce the new button to users; monitor `SsoLoginFailed` / `SsoAccountNotLinked` entries.
6. When enough users have linked, turn on **Require single sign-on** (confirm the prompt).
7. Put a reminder in the calendar for the **client secret / SAML certificate expiry**.

### 9.1 Trying it with a local test identity provider (Keycloak)

Before connecting the institution's real directory you can rehearse everything with [Keycloak](https://www.keycloak.org) on the EduSphere server or a laptop (free; needs Java 21). This is how sign-on was checked on 25 October 2026.

1. Run Keycloak over https on localhost, reusing the trusted .NET development certificate: `dotnet dev-certs https -ep localhost.pem --format PEM --no-password`, then `kc.bat start-dev --https-port=8443 --https-certificate-file=localhost.pem --https-certificate-key-file=localhost.key --hostname=https://localhost:8443`. EduSphere requires https for the authority and the SAML metadata; `localhost` is accepted.
2. In a new realm, add realm roles (`student`, `faculty`, `staff`), allow unmanaged user attributes (Realm settings › User profile) and create test users with an email, an `employeeNumber` attribute (registration number or staff username) and a role.
3. **OpenID Connect:** a confidential client with redirect URI `<portal>/Login/SsoCallback` and PKCE S256, plus two mappers: user attribute `employeeNumber` → claim `institution_id`, and realm roles → multivalued claim `roles`. In EduSphere: *Other OpenID Connect IdP*, authority `https://localhost:8443/realms/<realm>`, client ID and secret, institution ID claim `institution_id`, role claim `roles`, role mappings `student=Student; faculty=Faculty; staff=Admin|Finance`.
4. **SAML:** Clients › Import client, and paste the SP metadata from **Download SP metadata** (it carries the entity ID, ACS URL and certificate). Set the NameID format to persistent and add mappers for `email`, `employeeNumber` and the role list (`Role`, not single). In EduSphere: *SAML 2.0 IdP*, metadata URL `https://localhost:8443/realms/<realm>/protocol/saml/descriptor`, institution ID attribute `employeeNumber`, role attribute `Role`.

Results of the 25 October 2026 check (EduSphere tenant `TABSAN-UNI`, Keycloak 26.8):

| Case | OIDC | SAML |
|---|---|---|
| User whose `employeeNumber` is a staff username (faculty.it1) | Signed in, linked by institution ID | Same |
| Student with a different email, linked by registration number (BSCS-8-01) | Signed in, linked by institution ID | Same |
| User with no institution ID, matched by verified email (testadmin) | Signed in, linked by email | Same |
| User whose Keycloak role (`student`) does not match the EduSphere role (Finance), with *Refuse … role does not match* on | Refused: "Your identity provider reports a role that does not match …" | Same |
| SAML with **Sign authentication requests** on and Keycloak requiring signed requests; **Require encrypted assertions** on and Keycloak encrypting | — | Signed in |
| Keycloak stops encrypting while EduSphere requires encryption | — | Refused; audit `SsoLoginFailed`: "SAML assertion is not encrypted, but this provider requires encrypted assertions." |
| EduSphere stops signing while Keycloak requires signed requests | — | Keycloak refuses the request |

---

## 10. Troubleshooting

| What the user sees / what is logged | Likely cause | Fix |
|---|---|---|
| No SSO buttons on the login page | Module inactive, provider disabled or incomplete, or a tenant provider viewed without `?tenant=CODE` | Check Module Composition and the provider card status badge. |
| "Single sign-on is not available for this provider right now." | Return address not allowed, or IdP configuration/metadata unreachable | Check `AllowedRedirectUris`, `Sso:PublicBaseUrl`, outbound HTTPS, then Test connection. |
| IdP error "redirect URI mismatch" / "Reply URL does not match" (AADSTS50011) | The address registered at the IdP differs from the one EduSphere sends | Copy the exact value from "Values to register" (scheme, host, path, no trailing slash). |
| "Your single sign-on session expired or is invalid." | Sign-in took longer than 10 minutes, the browser blocked the sign-in cookie, or the link was reused | Try again in the same browser. For SAML the portal must be served over HTTPS (the cookie must be `Secure`). |
| "This account is not linked to an EduSphere user." (`SsoAccountNotLinked`) | No link and automatic linking off, email not verified, email differs, account inactive or in another tenant | Fix the email, enable automatic linking, or have the user sign in once with a password and link. |
| "This account's email domain is not allowed…" | Email outside Allowed email domains | Add the domain, or the user uses the right account. |
| Login activity `SsoTokenValidationFailed` | Wrong client secret or expired secret, clock skew, IdP certificate changed, unsigned or wrongly addressed SAML response | Re-enter the secret, check NTP; for SAML confirm signing, Entity ID and ACS at the IdP. |
| "Your institution requires single sign-on." on the password form | Require single sign-on is on for this user's scope | Use the SSO button. A SuperAdmin can still use a password. |
| "Stored client secret cannot be decrypted" (Test connection) | The API encryption key (`Security:EncryptionMasterKey`) changed | Enter the client secret again. |

---

## 11. Security notes

* Sign-in uses the **authorization code flow with PKCE**. Tokens are validated (signature, issuer, audience, lifetime, nonce) and state is single-use and bound to the user's browser.
* SAML responses must be signed by a certificate from the configured metadata, addressed to EduSphere's entity ID and ACS, and answer a request EduSphere sent: unsolicited (IdP-initiated) responses are rejected.
* All standard EduSphere sign-in checks still apply to SSO: inactive or locked accounts, session-risk rules, licence concurrency limits and (unless trusted) the EduSphere authenticator code.
* Every settings change, link, unlink, success and failure is recorded in the audit log and/or login activity.
