# Phase 40 SMS Production Rollout Checklist

## Purpose
Enable SMS notification delivery safely in production after Phase 40 completed phone-number persistence and recipient resolution.

## Scope
This checklist covers:
- production configuration enablement,
- secret provisioning,
- user phone-number readiness,
- smoke validation,
- rollback.

It does not require code changes.

## Preconditions
- Phase 40 user phone-number migration is deployed.
- Latest application build is deployed successfully.
- Full solution regression is green.
- Notification email and in-app notification paths remain healthy.
- Twilio account is provisioned and approved for the target sending region(s).

## Required Secrets
Provision these environment variables in the production host/platform secret store:
- `TWILIO_ACCOUNT_SID`
- `TWILIO_AUTH_TOKEN`
- `TWILIO_PHONE_NUMBER`

Validation rules:
- use the real Twilio production values, not placeholders,
- confirm the sending phone number is SMS-capable,
- restrict access to secret-management roles only.

## Required App Configuration
In production configuration, confirm:
- `NotificationSms:Enabled` = `false` before smoke-test window opens,
- `NotificationSms:PortalUrl` points to the production notifications page,
- `NotificationEmail:Enabled` remains unchanged,
- no placeholder values exist in production app settings.

At rollout time:
- set `NotificationSms:Enabled` = `true`
- keep `NotificationSms:PortalUrl` set to the production notifications URL.

## Data Readiness
Before enabling SMS dispatch:
- populate valid phone numbers for target users through admin update flow or CSV import,
- prefer E.164 format (example: `+15551234567`),
- verify numbers are stored on active users only,
- verify a pilot cohort exists with confirmed opt-in/approved numbers.

Suggested readiness queries/checks:
- count active users with non-null phone numbers,
- sample-check phone formatting for the pilot cohort,
- confirm at least one Admin/SuperAdmin test account has a valid phone number.

## Rollout Steps
1. Confirm the production deployment already contains migration `20260518104000_Phase40_AddUserPhoneNumber`.
2. Provision Twilio secrets in the production environment.
3. Restart or recycle the API app only if required by the hosting platform to load new secrets.
4. Populate or verify pilot-user phone numbers.
5. Keep `NotificationSms:Enabled` = `false` while secrets and data are verified.
6. Execute a controlled smoke test using a pilot cohort.
7. Review logs for Twilio configuration errors, invalid-number errors, and outbound gateway failures.
8. If smoke passes, set `NotificationSms:Enabled` = `true`.
9. Monitor production logs and notification delivery metrics during the first release window.
10. Expand from pilot cohort to broader usage only after clean monitoring results.

## Smoke Test Plan
Use a small pilot cohort first:
- one SuperAdmin user,
- one Admin user,
- one standard recipient user with a valid phone number.

Smoke actions:
- send an in-app notification to the pilot users,
- verify the in-app notification appears,
- verify SMS delivery is attempted,
- verify the SMS content is concise and links users back to the portal context,
- verify duplicate recipients do not cause duplicate phone sends for the same number set.

Expected outcomes:
- no application exceptions,
- no Twilio credential/configuration errors,
- SMS arrives for valid recipients,
- users without phone numbers continue receiving in-app/email only.

## Monitoring During First Enablement Window
Watch for:
- `Twilio SMS provider is not configured` errors,
- invalid E.164 number exceptions,
- outbound gateway retry/circuit-breaker spikes,
- unexpected notification latency,
- user complaints about duplicate or malformed SMS content.

## Rollback
Immediate rollback steps:
1. Set `NotificationSms:Enabled` = `false`.
2. Recycle/reload configuration if the hosting model requires it.
3. Keep in-app and email notification channels active.
4. Capture failing logs and affected recipient samples.
5. Do not remove stored phone numbers unless data itself is incorrect.

Rollback triggers:
- repeated Twilio authentication/configuration failures,
- widespread invalid phone formatting in production data,
- materially increased notification request latency,
- unexpected high-volume or duplicate SMS behavior.

## Post-Rollout Evidence
Record:
- deployment date/time,
- operator name,
- pilot cohort used,
- sample successful notification IDs,
- any failed recipient examples,
- rollback decision or go-forward approval.

## Current Status
As of 2026-05-18:
- code support is complete,
- persisted user phone-number support is complete,
- recipient phone resolution is complete,
- production enablement remains an operational rollout task.
