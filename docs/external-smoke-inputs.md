# External Smoke Inputs

Reviewed: 2026-06-18

This file lists non-committed inputs for external smoke checks. Do not store real secret values in this file or any committed configuration. Do not store real provider secrets, API keys, webhook secrets, access keys, or private signing material in this file.

When a smoke is used for deployment approval, record only non-secret run evidence in [production-readiness-evidence-package.md](production-readiness-evidence-package.md): command, run date, operator, target profile or provider label, high-level result, artifact hash or safe evidence reference when applicable, and owner sign-off. Do not copy provider responses, credentials, private payloads, customer data, bank details, payroll contents, or private document contents into source control.

Run the aggregate dry-run first:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-go-live-readiness.ps1
```

Exit code `2` means one or more checks are blocked by missing operator inputs. That is expected before a provider is fully configured.
The aggregate dry-run loads Brevo Site Settings from the local PostgreSQL Docker container when available, but still requires non-secret delivery-pipeline confirmations before marking Brevo production-readiness prerequisites complete.
The aggregate dry-run also runs the production readiness report-bundle validator, so the top-level go-live gate fails fast if the local non-secret report/helper set is missing, unparseable, failed, stale, or unsafe before the filled deployment evidence package is reviewed. Set `DARWIN_PRODUCTION_READINESS_REPORT_BUNDLE_DIRECTORY` only when the bundle lives outside the default ignored `artifacts\production-readiness\` directory.

To keep a non-secret readiness attachment for the evidence package, export the same dry-run to an ignored markdown report:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-report-bundle.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-report-bundle.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-report-bundle-clean-smoke.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-go-live-readiness-report.ps1 -Force
```

The bundle and aggregate reports are written under `artifacts\production-readiness\` by default. They summarize ready, blocked, and failed checks and include non-secret output only. The bundle exporter also refreshes the owner action plan, owner handoff, environment template, local execution summary, and local evidence-package draft. The bundle validator confirms the expected reports and helpers exist, are non-secret, match the current branch, commit, or release reference where applicable, carry report exit codes consistent with their readiness status, and match the report rows recorded in the bundle index. The clean smoke proves a new bundle can be generated and validated from an empty temporary directory without relying on a previous bundle artifact. A blocked report is valid evidence of current gating; it is not go-live approval.

When a filled production evidence package exists, validate its shape before go-live approval:

```powershell
$env:DARWIN_PRODUCTION_READINESS_EVIDENCE_PACKAGE_PATH = "artifacts\production-readiness\evidence-package.md"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-evidence-package.ps1
```

If a `Ready` row in that package references a local readiness bundle or generated helper, the validator checks that the referenced file exists, is secret-free, matches the current branch and commit, and reports `Ready` with exit code `0` when the artifact has readiness metadata. Deployment-specific private evidence can still live outside the repository; those references are checked by the deployment owner, not by the local script.

The validation check rejects placeholder text, unresolved open/blocked/failed result rows, missing required evidence markers, and sensitive value patterns. The required markers include production-like staging rehearsal, explicit MinIO production and Azure Blob readiness preflight rows, dual e-invoice evidence, Android launch evidence, provider smokes, and approval rows. It does not inspect the private evidence repository behind each non-secret reference.

Production-like staging rehearsal preflight:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-like-staging-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-like-staging-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-local-backup-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-local-backup-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-local-postgres-restore-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-local-postgres-restore-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-local-release-candidate-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-local-release-candidate-readiness-report.ps1 -Force
```

The local backup report inspects only non-secret backup structure: manifest presence, PostgreSQL dump integrity, MinIO mirror presence, private local configuration group presence, and Docker container inventory. It does not print private file names from the sensitive backup area, credentials, provider payloads, or backup file contents. Use `DARWIN_LOCAL_BACKUP_ROOT` or the `-BackupRoot` parameter when the backup root is not the default local path.

The local PostgreSQL restore report performs a temporary restore from the daily dump using `pg_restore --no-owner`, verifies restored application schema/table presence and the EF migration history table, then removes the temporary database. Use `DARWIN_POSTGRES_CONTAINER` or the `-PostgresContainerName` parameter when the running container name is not auto-detected. Docker restore commands run with a bounded timeout; use `-DockerCommandTimeoutSeconds` only for approved large local restore rehearsals.

The local release-candidate report runs focused .NET build/test lanes for WebAdmin, WebApi, Worker, Contracts compatibility, and Mobile Shared compatibility. Web storefront npm/toolchain evidence remains in the Web/Mobile readiness report so missing `npm` blocks the correct surface instead of hiding inside a backend build summary.

Required non-secret references:

- `DARWIN_STAGING_REHEARSAL_LABEL`
- `DARWIN_STAGING_RELEASE_REFERENCE`
- `DARWIN_STAGING_EVIDENCE_REFERENCE`

Required confirmations:

- `DARWIN_STAGING_BUILD_TESTS_CONFIRMED=true`
- `DARWIN_STAGING_MIGRATION_REHEARSAL_CONFIRMED=true`
- `DARWIN_STAGING_ROLLBACK_REHEARSAL_CONFIRMED=true`
- `DARWIN_STAGING_DATABASE_BACKUP_RESTORE_CONFIRMED=true`
- `DARWIN_STAGING_OBJECT_STORAGE_PREFLIGHTS_CONFIRMED=true`
- `DARWIN_STAGING_PROVIDER_PREFLIGHTS_CONFIRMED=true`
- `DARWIN_STAGING_EINVOICE_EVIDENCE_CONFIRMED=true`
- `DARWIN_STAGING_ANDROID_EVIDENCE_CONFIRMED=true`
- `DARWIN_STAGING_MONITORING_ALERTING_CONFIRMED=true`
- `DARWIN_STAGING_OWNER_SIGNOFF_CONFIRMED=true`

## Stripe Test Mode

Provider readiness summary:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-provider-readiness-report.ps1 -Force
```

The provider readiness report runs non-executing Stripe, DHL, Brevo, and VIES prerequisite checks and writes a non-secret summary under `artifacts\production-readiness\`. It does not execute live charges, create DHL validation requests, send Brevo messages, call VIES, or replace provider-specific smoke evidence.

Secrets must be entered through Site Settings or secure deployment configuration:

- Test publishable key.
- Test server-side key.
- Test webhook signing secret.
- Stripe enabled flag.

Smoke variables:

- `DARWIN_WEBAPI_BASE_URL`
- `DARWIN_STRIPE_SMOKE_ORDER_ID`, unless using `-CreateSmokeOrder`
- `DARWIN_STRIPE_SMOKE_ORDER_NUMBER`, unless using `-CreateSmokeOrder`
- `DARWIN_BUSINESS_API_BEARER_TOKEN`, when using `-CheckBusinessSubscriptionCheckout`
- `DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID`, when using `-CheckBusinessSubscriptionCheckout`
- `DARWIN_STRIPE_WEBHOOK_PUBLIC_URL`, when using a public HTTPS endpoint for webhook delivery
- `DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED=true`, after Dashboard delivery or Stripe CLI forwarding is confirmed
- `DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED=true`, after callback processing is enabled

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-stripe-webhook-forwarding.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder -CheckReturnRoute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckBusinessSubscriptionCheckout
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -RequireRuntimePipeline
```

Acceptance:

- The checkout URL comes from Stripe-hosted Checkout.
- Browser return routes do not mark payments or subscriptions successful.
- Verified webhook processing finalizes payment/subscription state.
- Provider references and secrets are not printed.

## Stripe Live Readiness

This is a checklist before approved live-mode execution. It does not call Stripe or create live charges.

Required confirmations:

- `DARWIN_STRIPE_LIVE_WEBHOOK_PUBLIC_URL`
- `DARWIN_STRIPE_LIVE_KEYS_CONFIGURED_CONFIRMED=true`
- `DARWIN_STRIPE_LIVE_WEBHOOK_ENDPOINT_CONFIRMED=true`
- `DARWIN_STRIPE_LIVE_WEBHOOK_EVENTS_CONFIRMED=true`
- `DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED=true`
- `DARWIN_STRIPE_WEBADMIN_VISIBILITY_CONFIRMED=true`
- `DARWIN_STRIPE_MONITORING_CONFIRMED=true`
- `DARWIN_STRIPE_ALERTING_CONFIRMED=true`
- `DARWIN_STRIPE_REFUND_DISPUTE_PLAYBOOK_CONFIRMED=true`

`DARWIN_STRIPE_LIVE_WEBHOOK_PUBLIC_URL` must be the public HTTPS webhook URL ending in `/api/v1/public/billing/stripe/webhooks`. Do not include embedded credentials, query strings, fragments, webhook signing secrets, tokens, provider payloads, or live payment details in the URL or readiness inputs.

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-stripe-live-readiness.ps1
```

Live-mode execution still requires explicit operator approval.

## Brevo

Runtime application settings are DB-backed through Site Settings. The script can either read non-secret inputs from environment variables or load Brevo Site Settings from a local PostgreSQL Docker container for local/staging validation. Secrets are not printed.

Required variables:

- `DARWIN_BREVO_API_KEY`
- `DARWIN_BREVO_SENDER_EMAIL`
- `DARWIN_BREVO_TEST_RECIPIENT_EMAIL`

Optional variables:

- `DARWIN_BREVO_BASE_URL`
- `DARWIN_BREVO_SENDER_NAME`
- `DARWIN_BREVO_REPLY_TO_EMAIL`

Delivery-pipeline confirmations:

- `DARWIN_BREVO_WEBHOOK_PUBLIC_URL`
- `DARWIN_BREVO_WEBHOOK_CONFIGURED_CONFIRMED=true`
- `DARWIN_BREVO_TRANSACTIONAL_EVENTS_CONFIRMED=true`
- `DARWIN_BREVO_PROVIDER_CALLBACK_WORKER_CONFIRMED=true`
- `DARWIN_BREVO_EMAIL_DISPATCH_WORKER_CONFIRMED=true`

`DARWIN_BREVO_WEBHOOK_PUBLIC_URL` must be the public HTTPS webhook URL ending in `/api/v1/public/notifications/brevo/webhooks`. Do not use a loopback URL, and do not include embedded credentials, query strings, fragments, tokens, provider payloads, or private delivery details in the URL or readiness inputs.

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -UseSiteSettings
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute -Sandbox
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -UseSiteSettings -Execute -Sandbox
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -RequireDeliveryPipeline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute
```

Acceptance:

- Sandbox mode verifies account authentication and request shape without delivering a message.
- Non-sandbox controlled-inbox smoke is run only after webhook and worker confirmations.
- Webhook events are visible in provider callback processing.
- Secrets and raw provider responses are not printed.

## DHL

Secrets and account settings must be entered through Site Settings or secure configuration.

Required variables:

- `DARWIN_DHL_API_BASE_URL`
- `DARWIN_DHL_API_KEY`
- `DARWIN_DHL_API_SECRET`
- `DARWIN_DHL_ACCOUNT_NUMBER`
- `DARWIN_DHL_PRODUCT_CODE` optional; defaults to the script value
- `DARWIN_DHL_SHIPPER_NAME`
- `DARWIN_DHL_SHIPPER_STREET`
- `DARWIN_DHL_SHIPPER_POSTAL_CODE`
- `DARWIN_DHL_SHIPPER_CITY`
- `DARWIN_DHL_SHIPPER_COUNTRY`
- `DARWIN_DHL_SHIPPER_EMAIL`
- `DARWIN_DHL_SHIPPER_PHONE_E164`
- `DARWIN_DHL_TEST_RECEIVER_NAME`
- `DARWIN_DHL_TEST_RECEIVER_STREET`
- `DARWIN_DHL_TEST_RECEIVER_POSTAL_CODE`
- `DARWIN_DHL_TEST_RECEIVER_CITY`
- `DARWIN_DHL_TEST_RECEIVER_COUNTRY`
- `DARWIN_DHL_TEST_RECEIVER_PHONE_E164` optional

Runtime confirmations:

- `DARWIN_DHL_SHIPMENT_PROVIDER_OPERATION_WORKER_CONFIRMED=true`
- `DARWIN_DHL_PROVIDER_CALLBACK_WORKER_CONFIRMED=true`
- `DARWIN_DHL_SHIPMENT_LABELS_STORAGE_CONFIRMED=true`

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -RequireRuntimePipeline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute -IncludeReturn
```

Acceptance:

- Provider references, tracking values, and labels come from DHL.
- No fake labels, references, or tracking URLs are generated.
- Labels are stored through configured storage.
- WebAdmin can inspect and recover failed/stale provider operations.

## VIES

Required variables:

- `DARWIN_VIES_VALID_VAT_ID`
- `DARWIN_VIES_INVALID_VAT_ID`

Optional variables:

- `DARWIN_VIES_ENDPOINT_URL`
- `DARWIN_VIES_TIMEOUT_SECONDS`

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute -CheckProviderFailure
```

Acceptance:

- Clear provider valid response maps to `Valid`.
- Clear provider invalid response maps to `Invalid`.
- Provider timeout, malformed response, unavailable service, or exception maps to `Unknown`/manual review.
- Provider failures must remain `Unknown` and require manual review.
- Scheduled retry may retry provider-generated `Unknown` outcomes, but operator decisions remain authoritative.
- WebAdmin may show format hints during manual review; format hints are not official VIES confirmation and must not mark a VAT ID valid.

## Object Storage And MinIO

Generic object-storage smoke variables:

- `DARWIN_OBJECT_STORAGE_PROVIDER`
- `DARWIN_OBJECT_STORAGE_CONTAINER`
- `DARWIN_OBJECT_STORAGE_S3_BUCKET`
- `DARWIN_OBJECT_STORAGE_S3_ACCESS_KEY`
- `DARWIN_OBJECT_STORAGE_S3_SECRET_KEY`
- `DARWIN_OBJECT_STORAGE_S3_ENDPOINT_OR_REGION`
- `DARWIN_OBJECT_STORAGE_AZURE_CONTAINER`
- Provider-specific endpoint, region, access key, secret key, bucket/container, and profile values required by the selected provider.
- `DARWIN_OBJECT_STORAGE_PRODUCTION_SMOKE_CONFIRMED=true`, required before executing a smoke against a non-local S3-compatible endpoint, AWS-style region-only configuration, or Azure Blob container. This confirmation allows a disposable smoke object to be written to the selected target; it does not claim production immutability.

Local MinIO smoke variables:

- `DARWIN_RUN_MINIO_SMOKE=true`
- `DARWIN_MINIO_ENDPOINT`
- `DARWIN_MINIO_REGION`
- `DARWIN_MINIO_ACCESS_KEY`
- `DARWIN_MINIO_SECRET_KEY`
- `DARWIN_MINIO_BUCKET`

Production MinIO readiness confirmations:

- `DARWIN_MINIO_PRODUCTION_ENDPOINT`
- `DARWIN_MINIO_PRODUCTION_BUCKET`
- `DARWIN_MINIO_PRODUCTION_PROVIDER_SELECTED_CONFIRMED=true`
- `DARWIN_MINIO_TLS_CONFIRMED=true`
- `DARWIN_MINIO_DEDICATED_KEYS_CONFIRMED=true`
- `DARWIN_MINIO_LEAST_PRIVILEGE_POLICY_CONFIRMED=true`
- `DARWIN_MINIO_BUCKET_OBJECT_LOCK_CONFIRMED=true`
- `DARWIN_MINIO_BUCKET_VERSIONING_CONFIRMED=true`
- `DARWIN_MINIO_DEFAULT_RETENTION_CONFIRMED=true`
- `DARWIN_MINIO_RETENTION_MODE_CONFIRMED=true`
- `DARWIN_MINIO_LEGAL_HOLD_POLICY_CONFIRMED=true`
- `DARWIN_MINIO_BACKUP_CONFIGURED_CONFIRMED=true`
- `DARWIN_MINIO_RESTORE_TEST_CONFIRMED=true`
- `DARWIN_MINIO_MONITORING_CONFIRMED=true`
- `DARWIN_MINIO_ALERTING_CONFIRMED=true`
- `DARWIN_MINIO_DARWIN_PROFILE_CONFIGURED_CONFIRMED=true`
- `DARWIN_MINIO_INVOICE_ARCHIVE_PROFILE_CONFIRMED=true`
- `DARWIN_MINIO_SHIPMENT_LABELS_PROFILE_CONFIRMED=true`
- `DARWIN_MINIO_MEDIA_ASSETS_PROFILE_DECIDED_CONFIRMED=true`
- `DARWIN_MINIO_FINANCE_EXPORTS_PROFILE_CONFIRMED=true`
- `DARWIN_MINIO_FINANCE_EXPORT_OUTBOUND_PROFILE_CONFIRMED=true`
- `DARWIN_MINIO_PERSONNEL_DOCUMENTS_PROFILE_CONFIRMED=true`
- `DARWIN_MINIO_PAYROLL_PAYSLIPS_PROFILE_CONFIRMED=true`
- `DARWIN_MINIO_DISPOSABLE_SMOKE_PREFIX_CONFIRMED=true`
- `DARWIN_MINIO_RETENTION_DELETE_BEHAVIOR_CONFIRMED=true`
- `DARWIN_MINIO_SELECTED_PROVIDER_SMOKE_CONFIRMED=true`
- `DARWIN_MINIO_SELECTED_PROVIDER_SMOKE_REFERENCE`
- `DARWIN_MINIO_OPERATOR_RUNBOOK_CONFIRMED=true`

Commands:

```powershell
dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-minio-production-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-minio-production-readiness-report.ps1 -Force
```

Azure Blob readiness confirmations:

- `DARWIN_AZURE_BLOB_PRODUCTION_ENDPOINT`
- `DARWIN_AZURE_BLOB_PRODUCTION_CONTAINER`
- `DARWIN_AZURE_BLOB_PROVIDER_SELECTED_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_TLS_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_DEDICATED_IDENTITY_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_LEAST_PRIVILEGE_POLICY_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_VERSIONING_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_IMMUTABILITY_POLICY_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_LEGAL_HOLD_POLICY_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_BACKUP_CONFIGURED_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_RESTORE_TEST_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_MONITORING_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_ALERTING_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_DARWIN_PROFILE_CONFIGURED_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_INVOICE_ARCHIVE_PROFILE_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_SHIPMENT_LABELS_PROFILE_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_MEDIA_ASSETS_PROFILE_DECIDED_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_FINANCE_EXPORTS_PROFILE_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_FINANCE_EXPORT_OUTBOUND_PROFILE_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_PERSONNEL_DOCUMENTS_PROFILE_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_PAYROLL_PAYSLIPS_PROFILE_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_DISPOSABLE_SMOKE_PREFIX_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_RETENTION_DELETE_BEHAVIOR_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_SELECTED_PROVIDER_SMOKE_CONFIRMED=true`
- `DARWIN_AZURE_BLOB_SELECTED_PROVIDER_SMOKE_REFERENCE`
- `DARWIN_AZURE_BLOB_OPERATOR_RUNBOOK_CONFIRMED=true`

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-azure-object-storage-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-azure-object-storage-readiness-report.ps1 -Force
```

Acceptance:

- Local smoke is optional and does not prove production immutability.
- Production immutability requires provider-level validation on the real bucket/container.
- Production-like smoke execution is guarded; set `DARWIN_OBJECT_STORAGE_PRODUCTION_SMOKE_CONFIRMED=true` or pass `-AllowProductionEndpoint` only after the disposable test prefix, cleanup behavior, retention mode, and operational approval are confirmed.

## E-Invoice External Command

Required variable:

- `DARWIN_EINVOICE_COMMAND_PATH`

Optional variables:

- `DARWIN_EINVOICE_FORMAT`
- `DARWIN_EINVOICE_VALIDATION_PROFILE`

Production configuration should set `Compliance:EInvoice:ExternalCommand:RequireValidationReport=true` so generated artifacts are rejected unless the selected tool writes a recognized positive validation report.

Production readiness variables:

- `DARWIN_EINVOICE_TOOLING_REFERENCE`
- `DARWIN_EINVOICE_EVIDENCE_PACKAGE_REFERENCE`
- `DARWIN_EINVOICE_ZUGFERD_FIXTURE_REFERENCE`
- `DARWIN_EINVOICE_ZUGFERD_ARTIFACT_REFERENCE`
- `DARWIN_EINVOICE_ZUGFERD_VALIDATION_REPORT_REFERENCE`
- `DARWIN_EINVOICE_ZUGFERD_STORAGE_DOWNLOAD_SMOKE_REFERENCE`
- `DARWIN_EINVOICE_ZUGFERD_ACCOUNTING_SIGNOFF_REFERENCE`
- `DARWIN_EINVOICE_XRECHNUNG_FIXTURE_REFERENCE`
- `DARWIN_EINVOICE_XRECHNUNG_ARTIFACT_REFERENCE`
- `DARWIN_EINVOICE_XRECHNUNG_VALIDATION_REPORT_REFERENCE`
- `DARWIN_EINVOICE_XRECHNUNG_STORAGE_DOWNLOAD_SMOKE_REFERENCE`
- `DARWIN_EINVOICE_XRECHNUNG_ACCOUNTING_SIGNOFF_REFERENCE`

Production readiness confirmations:

- `DARWIN_EINVOICE_TOOLING_PINNED_CONFIRMED=true`
- `DARWIN_EINVOICE_REQUIRE_VALIDATION_REPORT_CONFIRMED=true`
- `DARWIN_EINVOICE_ARCHIVE_RETENTION_CONFIRMED=true`
- `DARWIN_EINVOICE_ZUGFERD_FIXTURES_APPROVED_CONFIRMED=true`
- `DARWIN_EINVOICE_ZUGFERD_ARTIFACT_GENERATED_CONFIRMED=true`
- `DARWIN_EINVOICE_ZUGFERD_VALIDATION_REPORT_CONFIRMED=true`
- `DARWIN_EINVOICE_ZUGFERD_STORAGE_DOWNLOAD_SMOKE_CONFIRMED=true`
- `DARWIN_EINVOICE_ZUGFERD_ACCOUNTING_SIGNOFF_CONFIRMED=true`
- `DARWIN_EINVOICE_XRECHNUNG_FIXTURES_APPROVED_CONFIRMED=true`
- `DARWIN_EINVOICE_XRECHNUNG_ARTIFACT_GENERATED_CONFIRMED=true`
- `DARWIN_EINVOICE_XRECHNUNG_VALIDATION_REPORT_CONFIRMED=true`
- `DARWIN_EINVOICE_XRECHNUNG_STORAGE_DOWNLOAD_SMOKE_CONFIRMED=true`
- `DARWIN_EINVOICE_XRECHNUNG_ACCOUNTING_SIGNOFF_CONFIRMED=true`

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-einvoice-production-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-einvoice-production-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1 -Execute -RequireValidationReport
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1 -Format XRechnung -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1 -Format XRechnung -Execute -RequireValidationReport
```

For the repository-local Mustangproject wrapper, set `DARWIN_EINVOICE_COMMAND_PATH` to an absolute path for `scripts\mustang-einvoice-wrapper.cmd`. Run `scripts\install-mustang-cli.ps1` first if the pinned local jar is missing.

Acceptance:

- The production preflight confirms non-secret evidence references for both selected formats.
- Per-format references point to approved evidence records, not generated PDF/XML contents, validation-report dumps, customer invoices, provider payloads, or private approval documents.
- The adapter can call the approved wrapper.
- Output shape and validation-report handling pass.
- ZUGFeRD/Factur-X and XRechnung both have fixture, validation-report, storage/download, and reviewer evidence before compliant rollout is claimed.
- Smoke success does not by itself prove legal e-invoice compliance.

## Mobile Maps And Push

Android is the first launch target. iOS and MacCatalyst follow after Android signed artifact, maps, push, Google sign-in where enabled, and device/camera smoke evidence is complete.

Web storefront toolchain preflight:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-web-toolchain-readiness.ps1
```

This local check confirms `node` and `npm` are available before the solution-level build invokes the `Darwin.Web` Next.js build. It does not install packages, run `npm run build`, or accept registry credentials.

Web storefront local build preflight:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-web-storefront-local-build.ps1
```

This local build check runs `npm run build` only when `node_modules` is already present from the committed lockfile. It does not run `npm install`, accept npm tokens, read environment files, print registry credentials, or replace deployment runtime smoke.

Web storefront runtime/readiness preflight:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-web-storefront-routes.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-web-storefront-routes.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-web-storefront-readiness.ps1
```

The route smoke sends public GET requests only to configured storefront paths. `DARWIN_WEB_SITE_URL` must be a base deployment URL without embedded credentials, query strings, fragments, API keys, auth tokens, route state, or provider payloads. It prints route status codes only, never response bodies, cookies, customer data, or provider payloads. Non-local endpoints require `DARWIN_WEB_ROUTE_SMOKE_CONFIRMED=true` or `-AllowProductionEndpoint` after the deployment owner approves smoke traffic. Use `DARWIN_WEB_ROUTE_SMOKE_PATHS` to override the default public route list.

Web and mobile readiness summary:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-web-mobile-readiness-report.ps1 -Force
```

The Web/Mobile readiness report runs Web toolchain, local storefront build, Web storefront route-smoke configuration, Web storefront runtime, mobile resource-name, and Android project-metadata prerequisite checks and writes a non-secret summary under `artifacts\production-readiness\`. It does not run package installation, execute route smoke by default, run provider calls, build signed mobile packages, store npm tokens, store environment files, or replace route/device smoke evidence.

Required non-secret URLs:

- `DARWIN_WEBAPI_BASE_URL`
- `DARWIN_WEB_SITE_URL`
- `DARWIN_WEB_ROUTE_SMOKE_PATHS`, optional comma-separated public route override. Defaults to `/`, `/catalog`, `/help`, and `/cart`.

`DARWIN_WEBAPI_BASE_URL` and `DARWIN_WEB_SITE_URL` must be base deployment URLs. Do not include embedded credentials, query strings, fragments, API keys, auth tokens, route state, environment file values, customer data, or provider payloads in these readiness inputs.

Required confirmations:

- `DARWIN_WEB_STOREFRONT_BUILD_CONFIRMED=true`
- `DARWIN_WEB_RUNTIME_CONFIG_SMOKE_CONFIRMED=true`
- `DARWIN_WEB_PUBLIC_DISCOVERY_SMOKE_CONFIRMED=true`
- `DARWIN_WEB_MEMBER_PORTAL_ROUTE_SMOKE_CONFIRMED=true`
- `DARWIN_WEB_CHECKOUT_ROUTE_SMOKE_CONFIRMED=true`
- `DARWIN_WEB_DEGRADED_API_LOG_REVIEWED_CONFIRMED=true`
- `DARWIN_WEB_STAGING_OWNER_SIGNOFF_CONFIRMED=true`

If the Web storefront is intentionally pointed at `https://api.loyan.de`, also set `DARWIN_WEB_DEFAULT_PRODUCTION_API_CONFIRMED=true`; otherwise production-like staging should use its own WebApi URL.

Android readiness preflight:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-android-launch-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-android-launch-readiness-report.ps1 -Force
```

Mobile resource naming readiness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-mobile-resource-names.ps1
```

Android project readiness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-android-project-readiness.ps1
```

The Android project preflight validates checked-in Consumer and Business MAUI Android metadata, application identifiers, version fields, manifest transport/backup/camera/notification guards, release Firebase guards, the Consumer Maps guard, and that Firebase mobile configuration files are not tracked in git. It does not read Firebase file contents, signing keys, keystore paths, API keys, OAuth secrets, private package artifacts, provider payloads, customer data, or device logs.

The resource-name check is local and deterministic. It blocks MAUI image, splash, and app-icon assets whose filenames are not lowercase alphanumeric or underscore names, before Android-first launch evidence is accepted.

Required non-secret references:

- `DARWIN_ANDROID_RELEASE_ARTIFACT_REFERENCE`
- `DARWIN_ANDROID_VERSION_NAME`
- `DARWIN_ANDROID_VERSION_CODE`

Required confirmations:

- `DARWIN_ANDROID_RELEASE_CHANNEL_CONFIRMED`
- `DARWIN_ANDROID_SIGNING_PROFILE_CONFIRMED`
- `DARWIN_ANDROID_SIGNED_ARTIFACT_CONFIRMED`
- `DARWIN_ANDROID_MAPS_CONFIG_CONFIRMED`
- `DARWIN_ANDROID_MAPS_KEY_RESTRICTIONS_CONFIRMED`
- `DARWIN_ANDROID_FIREBASE_CONFIG_CONFIRMED`
- `DARWIN_ANDROID_PUSH_SMOKE_CONFIRMED`
- `DARWIN_ANDROID_CONSUMER_SMOKE_CONFIRMED`
- `DARWIN_ANDROID_BUSINESS_SMOKE_CONFIRMED`
- `DARWIN_ANDROID_CAMERA_QR_SMOKE_CONFIRMED`
- `DARWIN_ANDROID_CLEAR_TEXT_GUARD_CONFIRMED`
- `DARWIN_ANDROID_CERT_TRUST_GUARD_CONFIRMED`
- `DARWIN_ANDROID_ROUTE_COMPATIBILITY_CONFIRMED`
- `DARWIN_ANDROID_EVIDENCE_PACKAGE_CONFIRMED`

When native Google sign-in is enabled, also set `DARWIN_ANDROID_GOOGLE_SIGN_IN_ENABLED=true` and require `DARWIN_ANDROID_GOOGLE_SIGN_IN_SMOKE_CONFIRMED=true`.

Android maps:

- Provide `GoogleMapsApiKey`, `GOOGLE_MAPS_API_KEY`, or `ANDROID_GOOGLE_MAPS_API_KEY` at build time.
- Restrict the key to the Android package and signing certificate fingerprint.

Android push:

- Provide approved Firebase `google-services.json` at build time.
- Keep Firebase service-account credentials outside the repository.

iOS/MacCatalyst push:

- Provide Apple Developer App ID, provisioning profile, Push Notifications capability, and APNS provider credentials through secure configuration.

Acceptance:

- Signed Android release builds include production mobile configuration before Android launch.
- `scripts\check-android-project-readiness.ps1` passes against the checked-out release candidate.
- `scripts\check-android-launch-readiness.ps1` passes in the deployment shell.
- Android device smoke registers push tokens and verifies logout/account-switch behavior.
- Android physical camera QR scanning is validated with real device/camera input.
- iOS/MacCatalyst smoke uses the same acceptance rules after those launch targets enter scope.
