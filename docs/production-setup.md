# Darwin Production Setup

Reviewed: 2026-06-17

This runbook describes production setup at a deployment-neutral level. Do not commit deployment-specific domains, customer names, credentials, keys, webhook secrets, access keys, or signing material.

For repeatable customer rollout steps, approval ownership, and manual sign-off gates, use [docs/customer-deployment-onboarding-checklist.md](customer-deployment-onboarding-checklist.md). For the non-secret go-live evidence package, use [docs/production-readiness-evidence-package.md](production-readiness-evidence-package.md).

## Configuration Principles

- Store secrets in environment variables, .NET User Secrets for local development, a secret manager, or a deployment vault.
- Do not store real provider values in `appsettings*.json`, docs, logs, screenshots, or test output.
- Prefer PostgreSQL for new deployments. SQL Server remains supported.
- Keep WebAdmin, WebApi, Worker, and front-office settings aligned to the same database and provider configuration.
- Run all provider smoke scripts in dry-run mode first.

## Production Evidence Package

Before a customer deployment is treated as production-ready, create and validate a non-secret evidence package following [docs/production-readiness-evidence-package.md](production-readiness-evidence-package.md). Use `scripts\new-production-readiness-evidence-package.ps1` to create a consistent working copy from the repository template when needed, run `scripts\export-production-readiness-report-bundle.ps1 -Force` to generate the non-secret readiness report bundle plus the owner action plan, owner handoff, environment template, and local evidence-package draft, run `scripts\check-production-readiness-report-bundle.ps1` to validate report/helper shape and sensitive-pattern safety, then run `scripts\check-production-readiness-evidence-package.ps1` against the filled package. The package records proof and owner approvals; the report bundle records local current-state readiness outcomes; the action plan records owner follow-up; the owner handoff groups follow-up by owner; the environment template records missing evidence variable placeholders; the local draft records report/helper references and a local supporting-evidence snapshot while keeping deployment evidence blocked. None of these artifacts stores credentials, private endpoint tokens, webhook secrets, raw provider payloads, private documents, bank identifiers, payroll internals, or customer personal data.

The package must include:

- release build/test evidence, migration plan, rollback plan, and support ownership;
- database backup, restore, monitoring, and alert ownership;
- object-storage selected-provider preflight, disposable-prefix smoke, retention/legal-hold, backup, restore, and monitoring evidence;
- e-invoice fixture, validation, storage, download, and reviewer sign-off evidence when e-invoice generation is in scope;
- provider smoke outcomes for Stripe, DHL, Brevo, VIES, object storage, and mobile push/maps when each provider is in scope;
- final customer business, accounting/tax, operations, system-admin, legal/compliance where required, and Darwin technical approvals outside source control.

Do not mark production readiness from local smoke alone. Local MinIO, test-mode Stripe, local e-invoice adapter smoke, and parser fixtures prove code paths; they do not prove the selected production provider, retention policy, legal acceptance, or deployment monitoring.

MinIO remains the first production object-storage target for the current rollout path. Azure Blob readiness is prepared through [azure-object-storage-readiness-runbook.md](azure-object-storage-readiness-runbook.md) and `scripts\check-azure-object-storage-readiness.ps1` for Azure-first deployments or the next storage hardening lane after MinIO evidence.

Run the sequence in [production-go-live-evidence-execution-plan.md](production-go-live-evidence-execution-plan.md) before real production execution. The first complete evidence package, readiness report bundle, owner action plan, owner handoff, environment template, and local evidence-package draft should be produced against a production-like staging candidate so migration, rollback, storage, e-invoice, Android, provider, monitoring, and owner evidence are rehearsed before production traffic. The bundle validator proves the generated local reports and helpers are present, parseable where applicable, non-failed, and non-secret; it does not replace deployment owner approvals or the filled evidence-package validator.

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
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -UseSiteSettings
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
- Provider-generated `Unknown` outcomes are handled by the scheduled retry worker when enabled.
- WebAdmin may show country/VAT-format hints to help manual review, but these hints are never official validation and must not auto-mark a VAT ID valid.
- Critical retry-failure thresholds send admin email alerts; normal review items remain visible in WebAdmin Tax Compliance.

Smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute -CheckProviderFailure
```

Enable retry workers only when the deployment has an operator who owns the manual-review queue and alert response.

## Object Storage

MinIO is the selected first production target through the S3-compatible provider. Azure Blob Storage is the next provider-hardening target after MinIO evidence is complete. AWS S3 remains a supported alternative.

Production controls:

- Provider runs outside the Darwin application process.
- TLS is enabled on API paths.
- Dedicated access keys use least privilege, never root credentials.
- Invoice archive bucket/container is dedicated.
- Versioning is enabled.
- Object Lock, retention, legal hold, or provider equivalent is validated before claiming immutability.
- Backup, replication/offsite copy, disk monitoring, failed-write monitoring, and restore tests are complete.

Finance export profiles:

- `FinanceExports` stores generated canonical finance export packages. It is the package source used by Finance WebAdmin download and connector delivery.
- `FinanceExportOutbound` is the outbound delivery destination used by the file-delivery connector. WebAdmin push remains blocked when this profile is missing, invalid, or database-backed.
- Both profiles are configured through secure deployment configuration. Access keys, secret keys, connection strings, and provider credentials stay in environment variables, user-secrets, or a deployment vault.
- Profile names, container names, and prefixes are configuration. They must not be taken from batch metadata, attempt metadata, `DocumentRecord` metadata, `ExternalReference` metadata, or package content.
- The two profiles may share a provider and container with separate prefixes, or use separate containers when the deployment needs clearer operational ownership.

Personnel document profile:

- `PersonnelDocuments` stores internal HR personnel-file binaries linked through `DocumentRecord`.
- The profile is used only by WebAdmin HR upload, download, and archive flows. It must not be replaced by employee metadata, document metadata, event payloads, notes, or payroll-provider payloads.
- Use a storage provider with retention and legal-hold behavior appropriate for personnel files. File-system storage is acceptable for local smoke only; production should use a managed object-storage target with backup, monitoring, least-privilege credentials, and retention policy ownership.
- Access keys, secret keys, connection strings, and private credentials stay in secure configuration and must not appear in HR metadata, audit events, document metadata, docs, or logs.

Payroll payslip profile:

- `PayrollPayslips` stores internal HR payslip artifacts generated from approved payroll run snapshots and linked through `DocumentRecord`.
- The profile is used only by WebAdmin payroll artifact generation and download flows. It must not be replaced by payroll run metadata, employee metadata, document metadata, event payloads, notes, or payroll-provider payloads.
- Use a storage provider with retention and legal-hold behavior appropriate for payroll artifacts. File-system storage is acceptable for local smoke only; production should use a managed object-storage target with backup, monitoring, least-privilege credentials, and retention policy ownership.
- Access keys, secret keys, connection strings, private credentials, raw payroll-provider payloads, and private employee data must not appear in payroll metadata, audit events, document metadata, docs, or logs.

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-minio-production-readiness.ps1
```

The readiness preflight requires explicit confirmation for the archive, shipment-label, media, finance export source, finance export outbound, personnel document, and payroll payslip profile decisions, the disposable smoke prefix, retention/delete behavior, and the operator runbook. It does not accept or print access keys, secret keys, bucket policy JSON, object keys, or provider responses.

Run the selected-provider object-storage smoke against a production-like endpoint only after an operator approves the disposable smoke prefix and cleanup or retention behavior:

```powershell
$env:DARWIN_OBJECT_STORAGE_PRODUCTION_SMOKE_CONFIRMED = "true"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute
```

Do not set the production smoke confirmation for routine local validation. It exists to prevent accidental writes to a real bucket/container.

See [docs/minio-storage-runbook.md](minio-storage-runbook.md). For Azure-first deployments or later Azure hardening, see [docs/azure-object-storage-readiness-runbook.md](azure-object-storage-readiness-runbook.md).

## E-Invoice

Current direction:

- ZUGFeRD/Factur-X and XRechnung are both required for the selected German e-invoice readiness path.
- A deployment may stay receive-only or defer compliant generation only through an explicit scope decision recorded in the production evidence package.
- Mustangproject CLI behind the external-command adapter is the selected first implementation path.

Production readiness requires:

- Configure the external-command adapter with a bounded artifact size, for example `"MaxArtifactBytes": 20971520`.
- Configure `"RequireValidationReport": true` for production so the adapter rejects artifacts when the selected tool does not produce a recognized positive validation report.
- Run `scripts\check-einvoice-production-readiness.ps1` before compliant e-invoice rollout is claimed.
- Reject non-PDF ZUGFeRD/Factur-X outputs and malformed XRechnung XML outputs before storage.
- Treat adapter smoke as not full legal validation.
- Use [docs/e-invoice-acceptance-checklist.md](e-invoice-acceptance-checklist.md) for German B2B/B2G scope, reviewer approvals, deterministic fixture scenarios, and evidence requirements.
- Keep parser fixtures, smoke fixtures, and future legal-approved fixtures separated; see [docs/e-invoice-validation-fixtures.md](e-invoice-validation-fixtures.md).
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
- `scripts\check-android-launch-readiness.ps1` passes after signed Android artifact, push/maps, route compatibility, and physical device/camera evidence exist.
- Later iOS/MacCatalyst release scopes require Apple Developer signing, provisioning profiles, production APNS, entitlements, and device smoke after Android launch evidence is complete.
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
9. Validate object storage, archive/provider retention behavior, and finance export outbound delivery readiness when accounting export is in scope.
10. Validate e-invoice generator only after legal fixtures are approved.
11. Validate signed mobile artifacts and device/provider flows.
12. Record final operator sign-off outside source control.
