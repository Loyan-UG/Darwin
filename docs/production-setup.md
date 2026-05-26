# Darwin Production Setup

Reviewed: 2026-05-26

This runbook describes production setup at a deployment-neutral level. Do not commit deployment-specific domains, customer names, credentials, keys, webhook secrets, access keys, or signing material.

## Configuration Principles

- Store secrets in environment variables, .NET User Secrets for local development, a secret manager, or a deployment vault.
- Do not store real provider values in `appsettings*.json`, docs, logs, screenshots, or test output.
- Prefer PostgreSQL for new deployments. SQL Server remains supported.
- Keep WebAdmin, WebApi, Worker, and front-office settings aligned to the same database and provider configuration.
- Run all provider smoke scripts in dry-run mode first.

## Required Runtime Services

- PostgreSQL or SQL Server.
- WebAdmin for operator workflows.
- WebApi for public/member/business/admin/provider routes.
- Worker for provider callbacks, communication dispatch, shipment operations, archive maintenance, and retries.
- Front-office web application for storefront/member flows when enabled.
- Object storage provider for production archive/media/label scenarios.
- Transactional email provider.
- Payment, shipping, VAT, and e-invoice provider configuration as required by deployment scope.

## Persistence

Recommended:

- PostgreSQL for the default runtime provider.
- Dedicated runtime user with least privilege.
- Separate migration/admin role where possible.
- Backups, restore tests, monitoring, and alerting before production traffic.

See:

- [docs/persistence-providers.md](persistence-providers.md)
- [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md)

## Transactional Email

Site Settings owns the runtime transactional email configuration:

- `TransactionalEmailProvider`
- `NoReplyEmail`
- `BillingEmail`
- `SupportEmail`
- `SystemAdminEmail`
- `BrevoBaseUrl`
- `BrevoApiKey`
- Brevo webhook Basic Auth username/password
- Sandbox/test-recipient options

Recommended sender roles:

- `NoReplyEmail`: account activation, email confirmation, registration confirmations, password reset, security notices, and business invitations.
- `BillingEmail`: contracts, subscriptions, invoices, payment notices, failed-payment notices, refunds, and disputes.
- `SupportEmail`: human support sender and customer-facing `Reply-To`.
- `SystemAdminEmail`: internal critical admin alert recipient.

Brevo setup:

- Verify the sender domain in Brevo.
- Configure DKIM, SPF alignment where applicable, and DMARC.
- Configure an outbound/transactional webhook to the public WebApi endpoint ending with `/api/v1/public/notifications/brevo/webhooks`.
- Use Basic Auth or token authentication with dedicated provider-callback credentials stored in Site Settings.
- Subscribe transactional events needed for request, delivery, bounce, block, spam, error, defer, open/click if used, and unsubscribe tracking.
- Enable ProviderCallbackWorker and EmailDispatchOperationWorker.

Smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute -Sandbox
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -RequireDeliveryPipeline
```

Production readiness requires ongoing inbox-placement, webhook, failed-send, and callback-backlog monitoring.

## Payments

Stripe is the phase-one provider.

Rules:

- Browser return routes must not finalize payments or subscriptions.
- Verified Stripe webhooks are authoritative for final provider state.
- Store server-side keys and webhook signing secrets in secure configuration or Site Settings.
- Do not expose secret keys in WebAdmin.
- Run test-mode smoke before live-mode validation.

Smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-stripe-webhook-forwarding.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-stripe-live-readiness.ps1
```

Before live traffic:

- Live keys configured securely.
- Live webhook endpoint and events configured.
- ProviderCallbackWorker enabled.
- WebAdmin payment, subscription, refund, webhook, and dispute queues visible to operators.
- Monitoring and alerting configured.
- Refund/dispute/failed-payment playbook accepted.
- Approved live-mode smoke completed.

## Shipping

DHL is the phase-one shipping provider.

Rules:

- Do not create fake DHL labels, fake references, or local fake tracking URLs.
- Do not generate fake carrier labels, references, or tracking values.
- Store labels through configured shared storage.
- Use WebAdmin provider-operation queues for failed/stale recovery.
- Validate return-label payloads against the target account before production use.

Smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -RequireRuntimePipeline
```

Production readiness requires account-specific base URL, credentials, product code, account number, shipper/receiver data, label response validation, tracking validation, callback processing, storage, and WebAdmin recovery validation.

## VAT / VIES

Policy:

- Clear provider valid response may record `Valid`.
- Clear provider invalid response may record `Invalid`.
- Provider failures, unavailable service, timeouts, malformed responses, and exceptions must remain `Unknown` with manual review.

Smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute -CheckProviderFailure
```

Enable retry workers only after support ownership, cadence, and monitoring are approved.

## Object Storage

MinIO is the recommended self-hosted production target through the S3-compatible provider. AWS S3 and Azure Blob Storage remain supported alternatives.

Production controls:

- Provider runs outside the Darwin application process.
- TLS is enabled on API paths.
- Dedicated access keys use least privilege, never root credentials.
- Invoice archive bucket/container is dedicated.
- Versioning is enabled.
- Object Lock, retention, legal hold, or provider equivalent is validated before claiming immutability.
- Backup, replication/offsite copy, disk monitoring, failed-write monitoring, and restore tests are complete.

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-minio-production-readiness.ps1
```

See [docs/minio-storage-runbook.md](minio-storage-runbook.md).

## E-Invoice

Current direction:

- ZUGFeRD/Factur-X is the primary target.
- XRechnung is secondary unless a deployment requires it earlier.
- Mustangproject CLI behind the external-command adapter is the selected first implementation path.

Production readiness requires:

- Configure the external-command adapter with a bounded artifact size, for example `"MaxArtifactBytes": 20971520`.
- Reject non-PDF ZUGFeRD/Factur-X outputs and malformed XRechnung XML outputs before storage.
- Treat adapter smoke as not full legal validation.
- Pinned approved generator artifact.
- Runtime/JVM packaging where applicable.
- Deterministic fixtures.
- Legal validation evidence.
- Production artifact storage/download smoke.
- Operator/legal sign-off.

Current JSON/HTML/source-model exports are operational artifacts and must not be represented as compliant e-invoices.

## Mobile Release

Before store release:

- Android Release has approved signing key, Google Maps key restrictions, Firebase mobile config, notification permission UX, and device smoke.
- iOS/MacCatalyst have Apple Developer signing, provisioning profiles, production APNS, entitlements, and device smoke.
- No broad cleartext traffic or unsafe certificate trust is allowed in Release.
- Physical QR camera scanning is validated with real device/camera input.
- Push registration and logout/account-switch behavior are validated.

## Production Smoke Order

1. Restore database backup into the target environment and validate migrations.
2. Confirm WebAdmin, WebApi, Worker, and front-office use the intended configuration source.
3. Run `scripts/check-secrets.ps1`.
4. Run go-live dry-run checks.
5. Validate email provider in sandbox/drop mode, then controlled inbox.
6. Validate Stripe test-mode or live-mode according to deployment stage.
7. Validate DHL only after complete account data is available.
8. Validate VIES policy with controlled valid/invalid/provider-failure cases.
9. Validate object storage and archive/provider retention behavior.
10. Validate e-invoice generator only after legal fixtures are approved.
11. Validate signed mobile artifacts and device/provider flows.
12. Record final operator sign-off outside source control.
