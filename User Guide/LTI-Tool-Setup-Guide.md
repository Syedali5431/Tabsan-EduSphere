# External Tools (LTI 1.3) Setup Guide

Version: 1.0
Date: 19 October 2026
Audience: SuperAdmins, Admins given the External Tools menu, and the institution's IT administrator
Applies to: Plan M5 (Stages M5.1–M5.5)

LTI 1.3 is the standard way learning platforms connect to outside tools — H5P, Zoom, Turnitin, Copyleaks, publisher content and many more. EduSphere acts as the **platform**: you register a tool once, teachers add it to their courses, students open it without signing in again, and tools you allow can send scores to the gradebook and read the class list.

---

## 1. Before you start

| Check | Why |
|---|---|
| The licence includes **External Tools (LTI 1.3)** and the module is **active** (System Settings → Module Composition) | Every LTI page and endpoint is switched off while the module is inactive. |
| The portal is published on a **stable HTTPS address** (e.g. `https://portal.uni.edu`) | Tools send people back to, and call, the exact addresses you give them. A tunnel address that changes on restart breaks every tool. |
| The API server can make **outbound HTTPS** calls to the tool | EduSphere downloads the tool's public keys (JWKS) to check what the tool signs. |
| Server clocks are correct (NTP) | Signed messages are rejected if clocks are more than a minute apart. |
| You have the tool's LTI 1.3 details from the vendor | Login (OIDC initiation) URL, launch (target link) URL, redirect URL(s), and its keyset URL or public key. |

### 1.1 Configuration values (done once by whoever deploys EduSphere)

**API (`Tabsan.EduSphere.API`):**

```json
"Lti": {
  "PlatformUrl": "https://portal.uni.edu",
  "KeyFile": "D:\\EduSphere\\keys\\lti-platform-keys.json"
}
```

* `PlatformUrl` is the **portal** address (not the API's). It is the platform's identity (issuer); all addresses given to tools are under it. Leave it empty and LTI stays off.
* `KeyFile` holds the platform's private signing keys. It is created on first use (default: the service account's app-data folder, `Tabsan/lti-platform-keys.json`). **Every API node must share the file — back it up.** In Docker it is `/app/keys/lti-platform-keys.json` on the `api_keys` volume.
* Optional: `RetiredKeyDays` (7) — how long a rotated-out key stays published; `LaunchMinutes` (5); `AccessTokenMinutes` (60); `PlatformName`.

Restart the API after changing these settings.

---

## 2. Register a tool

Sign in → **System Settings → External Tools**.

**Tools with a registration URL (LTI Dynamic Registration).** If the vendor's instructions give a *registration URL*,
paste it under **Register with the tool's registration URL** and press **Start registration**. The tool's page opens inside
EduSphere, reads EduSphere's details and registers itself; when it finishes you return to the tool list. The new tool is
**switched off** and marked "Registered by the tool": check what it shares (privacy) and which services it may use
(grades, class list), change them with **Edit** if needed, then **Turn on**. The registration link works once, for one
hour. Otherwise register the tool by hand as below.

1. **Values to give each tool** — send these to the tool vendor or enter them in the tool's admin screen:

   | EduSphere value | Usual name at the tool |
   |---|---|
   | Issuer (platform ID) | Platform ID / Issuer |
   | OIDC authorization URL | Authentication request URL / OIDC auth endpoint |
   | Access token URL | Token endpoint (OAuth2) |
   | Public keyset (JWKS) URL | Platform public keyset / Key set URL |
   | Deep linking return URL | (only for tools that ask for it) |

2. In **Register a tool**, enter what the vendor gave you:
   * **Login initiation URL** and **Target link URI** (launch URL).
   * **Redirect URIs** — one per line, exactly as the vendor lists them.
   * **Deep linking URL** — if the tool lets teachers pick content (H5P, publisher libraries, Zoom meetings …).
   * The tool's **public keyset (JWKS) URL** (preferred — key changes then need no action) **or** its **public key** (PEM, RSA ≥ 2048 bits).
   * **What the tool learns about people**: *Nothing* (an anonymous EduSphere ID and the role), *Name*, *Email address*, or *Name and email address*. Choose the least the tool needs.
   * **Custom parameters** — `name=value` lines, only if the vendor asks for them.
   * **Allow the tool to send scores to the gradebook** — needed for graded activities.
   * **Allow the tool to read the class list** — only if the tool needs the roster (e.g. Zoom attendance, Turnitin class sync).
3. Press **Register tool**. EduSphere shows the tool's **Client ID** and a **Deployment ID**. Enter both at the tool.

URLs must be `https` (plain `http` is accepted only for `localhost`, for testing).

### 2.1 Where a tool can be used (deployments)

A new tool has one deployment for the **whole installation**. To limit it, open the tool (**Edit**), add a deployment for **one department** or **one course**, and turn the installation-wide one **off**. Teachers only see the tool in courses covered by an active deployment. Give the tool the deployment IDs you use.

### 2.2 Turning things off

* **Turn off** a tool: every course item using it stops launching and its tokens stop working. Turning it on again restores everything.
* **Turn off** a deployment: the tool disappears from the courses that deployment covers.

### 2.3 Editing

The stored public key is never shown again. Leave the key box empty to keep it, paste a new key to replace it, or enter a JWKS URL to switch to the tool's keyset. All registry changes are recorded in the audit log.

---

## 3. What teachers do

On **LMS Manage** (course content) the **External tools** card lets teachers:

* **Add an external tool** — choose the tool, a title, where it opens (**New tab** or **Inside EduSphere**) and, for a graded activity, **Points**. A graded item creates an assignment that receives the tool's scores.
* **Choose content from <tool>** — opens the tool's content picker (Deep Linking). What the teacher picks is added to the course; activities the tool marks as graded get an assignment automatically.
* **Hide / Show** an item, and open **Grades** for a graded item (the grading workspace).

Students see the visible items on the course page and open them with **Open**.

## 4. Grades and the class list

* Scores the tool sends become **graded submissions**, scaled to the item's points. They follow the assignment's posting policy (automatic or manual) and reach the gradebook like any assignment grade. The newest score from the tool wins; teachers can still re-grade in the grading workspace.
* The class list a tool can read contains the course's teachers and enrolled students with their roles and, only as allowed, names and email addresses.
* Long lists come in pages: a tool asking for a limited number gets a `Link: <…>; rel="next"` header pointing to the next page (class lists, line items and results).

---

## 5. Security notes

* EduSphere signs launches with its own keys (RS256); rotate them from the page (**Signing keys → Rotate**, SuperAdmin). The old key stays published for `RetiredKeyDays` so tools that cached it keep working.
* Each launch can be used **once** within a few minutes, only by the person who started it, and only with a redirect URI registered for the tool.
* Everything a tool signs (content selections, token requests) is checked against the tool's own keys; replayed messages are refused.
* Tool access tokens last an hour, carry only the permissions you allowed, and are stored only as hashes.

## 6. Troubleshooting

| Symptom | Likely cause and fix |
|---|---|
| The page says the platform address is not set | Set `Lti:PlatformUrl` in the API settings and restart the API. |
| "The External Tools module is not active" | Activate it in Module Composition (licence must include it). |
| The tool says the issuer, client or deployment is unknown | The values entered at the tool do not match this page — copy Issuer, Client ID and Deployment ID again. |
| "redirect_uri is not registered for this tool" | Add the exact redirect URL the tool uses (scheme, host, path, trailing slash) to **Redirect URIs**. |
| "This launch has expired or was already used" | The person waited too long or refreshed the tool page — open the item again from the course. |
| Content selection fails with "The tool's token is not valid" | The tool's key changed — check its JWKS URL, or paste the new public key. Check server clocks. |
| A tool inside EduSphere shows a blank frame | The tool refuses to be framed — set the item to open in a **New tab**. |
| Scores do not appear | Check **Allow the tool to send scores**, that the item has points, and the assignment's posting policy (manual posting hides grades until posted). |

Not yet available: LTI Dynamic Registration (tools register themselves) — register tools manually as above.
