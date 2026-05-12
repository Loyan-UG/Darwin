# Darwin Production Setup Runbook

This runbook lists the production configuration and smoke checks required before live traffic. Do not commit secrets to git; use environment variables, platform secrets, .NET user-secrets for local work, or a server-side secret store.

Provider smoke input names and execution order are summarized in `docs/external-smoke-inputs.md`.

## 1. Configuration Sources

Production `appsettings.json` must keep secrets empty or disabled. Supply provider credentials through secure configuration only.

Common production shape:

```powershell
$env:Persistence__Provider = "PostgreSql"
$env:ConnectionStrings__PostgreSql = "SET_BY_SECRET"
$env:DatabaseStartup__ApplyMigrations = "false"
$env:DatabaseStartup__Seed = "false"
$env:DataProtection__KeysPath = "D:\Darwin\_shared_keys"
$env:DataProtection__RequireKeyEncryption = "true"
$env:DataProtection__CertificateThumbprint = "..."
$env:Jwt__SigningKey = "SET_BY_SECRET"
```

Before committing configuration changes, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-go-live-readiness.ps1
```

## 2. Database

- Preferred provider: `PostgreSql`.
- SQL Server remains supported for deployments that require it.
- Apply migrations with the provider-specific migration project and an owner/migration role.
- Run WebAdmin, WebApi, and Worker with a restricted runtime role.
- Production/restricted runtime processes must set `DatabaseStartup:ApplyMigrations=false` and `DatabaseStartup:Seed=false`.
- PostgreSQL extensions required by migrations include `citext` and `pg_trgm`.

See `docs/postgresql-migration-runbook.md` and `docs/persistence-providers.md` for provider commands and grant scripts.

## 3. Data Protection

- WebAdmin, WebApi, and Worker must share `DataProtection:ApplicationName` and `DataProtection:KeysPath`.
- Enable `DataProtection:RequireKeyEncryption=true` in production when certificate-backed key encryption is configured.
- Configure `DataProtection:CertificateThumbprint`; startup should fail if the certificate is missing, lacks a private key, or is outside its validity window.
- Grant the service identity read access to the certificate private key and read/write access to the key path.

## 4. WebApi Authentication

- Set `Jwt__SigningKey` to a high-entropy value of at least 32 bytes.
- Keep issuer/audience aligned with front-office and mobile clients.
- Rotate JWT keys through Site Settings before production traffic.
- Never reuse development seeded JWT keys in shared, staging, or production environments.

## 5. Transactional Email / Brevo

Darwin supports `Email:Provider=Brevo` for production and `SMTP` for local or customer-specific fallback.

Brevo readiness checklist:

- Sender domain verified.
- DKIM and DMARC configured.
- HTTPS Brevo API base URL configured when overriding the default endpoint.
- API key stored only in secure configuration.
- `Email:Brevo:SandboxMode=false` for production traffic.
- Webhook Basic Auth username/password set.
- Brevo webhook URL configured at `/api/v1/public/notifications/brevo/webhooks`.
- Transactional events subscribed: request, delivered, deferred, soft_bounce, hard_bounce, spam, blocked, invalid, error, opened, click.
- `ProviderCallbackWorker:Enabled=true`.
- `EmailDispatchOperationWorker:Enabled=true`.
- Optional provider-managed template IDs mapped by Darwin template key under `Email:Brevo:TemplateIds`, for example `BusinessInvitationEmail`, `AccountActivationEmail`, and `PasswordResetEmail`.

Optional local harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -RequireDeliveryPipeline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute -Sandbox
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute
```

The first command verifies local prerequisites only. `-RequireDeliveryPipeline` also requires the public webhook endpoint, Brevo webhook subscription confirmation, transactional event subscription confirmation, and worker enablement confirmations. `-Execute -Sandbox` calls Brevo with the sandbox/drop header so account authentication and request shape can be checked before real delivery. `-Execute` sends one controlled-inbox message after sender DNS, webhook subscription, and operational approval are ready; non-sandbox execute mode requires the delivery pipeline confirmations. The script does not print secrets or raw Brevo response payloads.

Implementation note: Darwin renders transactional templates internally by default. When a Brevo template ID is configured for the current Darwin `TemplateKey` or `FlowKey`, Brevo receives `templateId` plus sanitized template parameters instead of inline HTML content. Provider-side template authoring and synchronization remains an optional operational workflow.

## 6. Stripe

Stripe webhook ingress is `/api/v1/public/billing/stripe/webhooks`. It validates `Stripe-Signature` before writing idempotent provider callback inbox records.

Stripe test-mode smoke checklist:

- Test publishable and server-side key entered in Settings or secure configuration.
- Stripe enabled in Site Settings.
- Stripe webhook signing secret entered securely.
- `ProviderCallbackWorker:Enabled=true` for the target Worker process.
- Checkout Session is created through WebApi.
- Business subscription checkout creates a Stripe-hosted subscription Checkout Session; `Billing:BusinessManagementBaseUrl`, `Billing:SubscriptionSuccessUrl`, and `Billing:SubscriptionCancelUrl` must point to customer-facing HTTPS pages in production.
- Storefront return URL is reached.
- Return route alone does not mark the payment successful.
- Verified webhook updates payment/order state.
- Verified subscription checkout webhook updates `BusinessSubscription` with provider checkout session, customer, and subscription references.
- WebAdmin refund creation for Stripe payments calls the provider refund API, stores provider refund references/status/failure details, and is reconciled by verified refund webhook events.
- Failed payment, refund, and dispute states are visible in WebAdmin.

Optional local harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-stripe-webhook-forwarding.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -CreateSmokeOrder
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckReturnRoute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckReturnRoute -OpenCheckout
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -CheckBusinessSubscriptionCheckout
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckBusinessSubscriptionCheckout
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -RequireRuntimePipeline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder -CheckReturnRoute -OpenCheckout -WaitForWebhookFinalization
```

The first command verifies local prerequisites only. `-RequireRuntimePipeline` additionally requires non-secret confirmation that ProviderCallbackWorker is enabled. The execute mode calls Darwin WebApi to create a Stripe Checkout Session for a pre-existing smoke order and confirms the returned checkout URL is Stripe-hosted while the payment remains `Pending`. `-CreateSmokeOrder` first creates a disposable storefront order through public cart/checkout so the smoke does not require pre-seeded order environment variables. `-CheckBusinessSubscriptionCheckout` uses `DARWIN_BUSINESS_API_BEARER_TOKEN` and `DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID` to verify the authenticated business subscription endpoint creates a Stripe-hosted subscription Checkout Session without printing bearer tokens, session URLs, or provider references. `-CheckReturnRoute` also verifies the storefront return route does not capture or complete Stripe payments without verified webhook events. `-OpenCheckout` opens the hosted Stripe checkout in the default browser without printing the session URL or provider references. `-WaitForWebhookFinalization` now requires confirmed Stripe Dashboard delivery or Stripe CLI forwarding plus ProviderCallbackWorker confirmation before it creates a checkout session, then polls the Darwin WebApi order confirmation endpoint after test payment and fails unless verified webhook processing moves the payment to `Captured` or `Completed`. Stripe keys and webhook signing secrets must be entered through Settings or secure configuration; this script does not accept, print, or persist secrets.

`scripts\check-stripe-webhook-forwarding.ps1` verifies that webhook delivery is operationally prepared without accepting or printing the webhook signing secret. Use either a public HTTPS endpoint ending in `/api/v1/public/billing/stripe/webhooks` or Stripe CLI local forwarding, then set `DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED=true` for the preflight.

Latest local test-mode handoff result: the handoff smoke passed on 2026-05-10 against a rebuilt WebApi instance from the current source on `http://localhost:5136`. The script created a disposable public checkout smoke order, received a Stripe-hosted Checkout Session from `checkout.stripe.com`, and confirmed the return route left the payment `Pending`. The focused WebApi Stripe webhook/provider-callback suite also passed on 2026-05-10 with `191` passed and `0` skipped using an isolated output path. Hosted checkout payment, refund, and verified webhook delivery still need to be completed before production traffic.

Live keys should be entered only when production smoke is scheduled. Storefront payment finalization must remain verified-webhook-only.

## 7. DHL

DHL webhook ingress is `/api/v1/public/shipping/dhl/webhooks`. It validates API key and HMAC signature before writing idempotent provider callback inbox records.

DHL live smoke checklist:

- Target account credentials entered in Settings or secure configuration.
- HTTPS base URL and product codes confirmed for the account.
- Shared media storage configured through `MediaStorage:RootPath` and `MediaStorage:PublicBaseUrl`.
- Create-shipment request succeeds.
- Label PDF is returned or retrievable.
- Label is stored in shared media storage.
- Tracking number/reference is stored.
- `ShipmentProviderOperationWorker:Enabled=true`.
- `ProviderCallbackWorker:Enabled=true`.
- Return-label operation creates a separate return shipment, persists provider reference/tracking, and stores the returned label without overwriting the outbound shipment.
- Callback is accepted and processed.
- Failed/stuck provider operations are recoverable in WebAdmin.

Optional local harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -RequireRuntimePipeline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute -IncludeReturn
```

The first command verifies required environment variables only. `-RequireRuntimePipeline` additionally requires non-secret confirmation that ShipmentProviderOperationWorker, ProviderCallbackWorker, and label storage are ready for the target environment. The `-Execute` command sends a real outbound DHL validation request and must only be used against the target account after credentials, product code, shipper, and receiver smoke-test data are approved. Add `-IncludeReturn` to validate the return-shipment sender/receiver mapping as well. The script does not print secrets or raw DHL response payloads.

Do not create fake DHL labels, fake references, or local fake tracking URLs.

## 8. VAT Validation / VIES

VIES configuration:

```json
{
  "Compliance": {
    "VatValidation": {
      "Vies": {
        "Enabled": true,
        "EndpointUrl": "https://ec.europa.eu/taxation_customs/vies/services/checkVatService",
        "TimeoutSeconds": 15
      }
    }
  }
}
```

VIES smoke checklist:

- VIES enabled in configuration.
- Valid VAT ID returns Valid.
- Invalid VAT ID returns Invalid.
- Disabled, unavailable, timeout, rate-limit, or malformed provider response returns Unknown/manual review.
- No false Valid/Invalid decision is recorded on provider failure.
- Operator-visible source/message fields are recorded.

Optional local harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute -CheckProviderFailure
```

The first command verifies that valid and invalid VAT ID smoke inputs are configured. Execute mode calls the VIES SOAP endpoint and expects the configured valid VAT ID to return `Valid` and the configured invalid VAT ID to return `Invalid`. The provider-failure check verifies the operational expectation that unavailable VIES calls stay `Unknown`/manual review instead of becoming false valid or invalid decisions.

The optional `DARWIN_VIES_ENDPOINT_URL` smoke override must be an absolute URL, and non-local overrides must use HTTPS. `DARWIN_VIES_TIMEOUT_SECONDS` must stay between 1 and 120 seconds.

The optional `VatValidationRetryWorker` is disabled by default. Enable it only after VIES live smoke and support ownership are approved; it retries provider-generated `Unknown` VAT validation decisions after the configured minimum age and does not overwrite operator/manual decisions.

## 9. Invoice Archive / Retention

Darwin uses a reusable object-storage architecture for archive/media/export-style artifacts. Invoice archive currently keeps the internal/database-backed provider as the development/internal fallback, a file-system provider for controlled non-compliance deployments, and an S3-compatible object-storage path for MinIO/AWS-style production storage. Production invoice archive should use provider-level immutable object storage.

`ObjectStorage:ActiveProfile` is the deployment-level default profile used only when a storage consumer does not request a named profile. Invoice archive, DHL labels, and CMS media should normally keep explicit named profiles so archive, label, and public media policies do not drift together accidentally.

Production decision:

- Recommended self-hosted target: MinIO through the generic S3-compatible provider.
- API compatibility target: S3-compatible object storage.
- Supported alternatives: AWS S3 and Azure Blob Storage.
- Development/internal fallback: database/internal archive provider.

Do not claim production archive immutability until the configured provider is validated with native versioning, object lock/retention/legal hold, or the provider-specific equivalent. Application-level overwrite protection is useful but is not the same as provider-level immutable retention.

Generic configuration shape:

```json
{
  "ObjectStorage": {
    "Provider": "S3Compatible",
    "S3Compatible": {
      "Endpoint": "https://minio.example.internal",
      "Region": "eu-central-1",
      "BucketName": "darwin-invoice-archive",
      "UseSsl": true,
      "UsePathStyle": true,
      "ForcePathStyle": true,
      "RequireObjectLock": true,
      "DefaultRetentionYears": 10,
      "DefaultRetentionMode": "Compliance",
      "LegalHoldEnabled": true,
      "ObjectLockValidationMode": "FailFast",
      "PublicBaseUrl": "https://media.example.com/darwin"
    },
    "Profiles": {
      "MediaAssets": {
        "Provider": "S3Compatible",
        "ContainerName": "darwin-media",
        "Prefix": "cms/uploads"
      },
      "ShipmentLabels": {
        "Provider": "S3Compatible",
        "ContainerName": "darwin-shipment-labels",
        "Prefix": "shipments"
      }
    }
  }
}
```

Supply `ObjectStorage:S3Compatible:AccessKey` and `ObjectStorage:S3Compatible:SecretKey` only through secure configuration. `ObjectStorage:Profiles:*:ContainerName` and `ObjectStorage:Profiles:*:Prefix` are validated at startup and applied centrally by the object-storage router, so consumers such as CMS media and DHL labels do not need provider-specific key logic.

Azure Blob configuration shape when a deployment selects Azure:

```json
{
  "ObjectStorage": {
    "Provider": "AzureBlob",
    "AzureBlob": {
      "AccountName": "darwinstorage",
      "ContainerName": "darwin-artifacts",
      "UseManagedIdentity": true,
      "RequireImmutabilityPolicy": true,
      "DefaultRetentionYears": 10,
      "LegalHoldEnabled": true,
      "ImmutabilityValidationMode": "FailFast",
      "PublicBaseUrl": "https://media.example.com/darwin"
    }
  }
}
```

Supply `ObjectStorage:AzureBlob:ConnectionString`, if used instead of managed identity, only through secure configuration. Do not expose the full connection string or account credentials in WebAdmin. `ObjectStorage:*:PublicBaseUrl` should point at the public bucket/container/profile root; Darwin appends only the normalized object key after that base URL.

MinIO production checklist:

- MinIO runs outside the Darwin application process.
- Dedicated storage volumes are configured; serious production deployments do not place MinIO and the database on the same disk.
- TLS is enabled.
- Darwin uses a dedicated least-privilege access key, not root credentials.
- Dedicated invoice archive bucket is created with Object Lock enabled from the start.
- Bucket versioning is enabled before go-live writes.
- When `ObjectStorage:S3Compatible:CreateBucketIfMissing=true`, Darwin can create a missing S3-compatible bucket on first write; if `RequireObjectLock=true`, the provider requests Object Lock at bucket creation and enables versioning.
- When `ObjectStorage:S3Compatible:ObjectLockValidationMode=FailFast`, Darwin validates bucket versioning and Object Lock configuration before writing immutable archive objects.
- Default retention is configured; use COMPLIANCE mode when legal retention is confirmed.
- Backup, replication, offsite copy, disk-usage monitoring, and failed-write alerts are configured.
- Restore is tested before go-live.

Local MinIO smoke:

- Developers can start an optional local MinIO instance with `docker compose -f docker-compose.minio.yml up -d`.
- The compose file uses development-only credentials from `.env.example`, creates a dedicated smoke bucket, requests Object Lock at bucket creation time, and enables versioning.
- Run `dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio"` only after setting `DARWIN_RUN_MINIO_SMOKE=true` and the `DARWIN_MINIO_*` environment variables documented in `docs/minio-storage-runbook.md`.
- Local MinIO smoke proves the S3-compatible provider can write/read/hash/metadata-check against a real endpoint. It does not prove production immutability; production still requires target-bucket Object Lock, default retention/legal hold policy, backup, restore, and least-privilege access validation.
- Latest local result on 2026-05-12: the smoke bucket existed, versioning was enabled, default Object Lock retention reported `COMPLIANCE` for `1DAYS`, and `dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio"` passed with `3` passed and `0` skipped.

Checklist:

- Internal/database archive enabled for development/internal fallback.
- Generic file-system object storage is available for development/internal/on-prem fallback profiles. It records hashes and retention metadata but does not provide storage-level immutability.
- File-system invoice archive provider selected with `InvoiceArchiveStorage:ProviderName=FileSystem` only for development, internal deployments, or controlled shared storage where deployment-level retention is understood.
- File-system archive provider root path configured under `InvoiceArchiveStorage:FileSystem:RootPath`.
- Generic `ObjectStorage` provider validation enabled in WebAdmin, WebApi, and Worker startup.
- WebAdmin Site Settings can run a safe object-storage smoke for the active provider plus `InvoiceArchive`, `ShipmentLabels`, and `MediaAssets` profiles. The smoke writes, reads, verifies, and attempts cleanup of a disposable object without displaying provider secrets.
- S3-compatible production profile configured for MinIO or AWS S3, or Azure Blob configured with a connection string/managed identity when a deployment selects Azure.
- Azure Blob immutable archive writes send native blob immutability and legal-hold options when retention/legal-hold metadata is requested. With `ObjectStorage:AzureBlob:RequireImmutabilityPolicy=true` and `ImmutabilityValidationMode=FailFast`, Darwin validates the target container has an immutability policy before writing archive objects.
- Named profile container and prefix defaults are configured and validated for `InvoiceArchive`, `ShipmentLabels`, `MediaAssets`, and any deployment-specific export/media profiles. Configure `ObjectStorage:ActiveProfile` only for a deliberate deployment default.
- Invoice archive can be routed to the generic object-storage path with `InvoiceArchiveStorage:ProviderName=S3Compatible`, `Minio`, `AwsS3`, or `AzureBlob` after the target bucket/container passes Object Lock/versioning or immutability-policy smoke. The archive path uses the `InvoiceArchive` object-storage profile when configured, including profile container and prefix defaults.
- Generated e-invoice artifacts use the same invoice archive object-storage profile when a compliant generator is configured. Keep the default generator disabled until ZUGFeRD/Factur-X generation and validation tooling is selected and tested.
- Optional e-invoice external generator adapter:

```json
{
  "Compliance": {
    "EInvoice": {
      "ExternalCommand": {
        "Enabled": false,
        "ExecutablePath": "",
        "TimeoutSeconds": 60,
        "MaxArtifactBytes": 20971520,
        "SupportsZugferdFacturX": true,
        "SupportsXRechnung": false,
        "ValidationProfile": "external-command"
      }
    }
  }
}
```

Only enable this after the generator/tooling decision is approved. The executable path must be absolute and supplied through deployment configuration. Darwin calls the command with `--input`, `--output`, and `--format`; the command must validate and create the artifact file. Darwin also rejects empty artifacts, artifacts larger than `MaxArtifactBytes`, non-PDF ZUGFeRD/Factur-X outputs, and malformed XRechnung XML outputs, but this is a safety guard, not full legal validation. Do not use this adapter to bypass ZUGFeRD/Factur-X or XRechnung validation.
- DHL labels can use the same generic object-storage backend by adding an `ObjectStorage:Profiles:ShipmentLabels` profile with `Provider` set to `S3Compatible`, `AzureBlob`, or `FileSystem`. If the profile is absent or uses the database fallback, labels continue to use the shared file-system media storage fallback.
- CMS/media uploads can use the same generic object-storage backend by adding an `ObjectStorage:Profiles:MediaAssets` profile with `Provider` set to `S3Compatible`, `AzureBlob`, or `FileSystem`, plus the selected provider's public base URL. If the profile is absent, has no public base URL, or uses the database fallback, WebAdmin keeps the current shared file-system media fallback.
- The WebAdmin and Darwin.Web public storefront must resolve the same media URLs. For object storage, publish the configured public base URL through the reverse proxy/CDN; for file-system fallback, keep the shared uploads path mounted consistently for both applications.
- Legal retention years confirmed before enabling purge.
- `InvoiceArchiveMaintenanceWorker:Enabled=true` only after retention settings are verified.
- Purge worker retains metadata/audit event while removing expired payloads.

## 10. Push Providers

- FCM/APNS remain disabled until real provider credentials are supplied.
- Enable only after server keys, sender/team/key identifiers, APNS key path, and bundle identifiers are configured.
- Keep `InactiveReminderWorker:Enabled=false` until push delivery is smoke-tested.

## 11. Worker Processes

Enable workers explicitly in production:

```json
{
  "WebhookDeliveryWorker": { "Enabled": true },
  "ProviderCallbackWorker": { "Enabled": true },
  "ShipmentProviderOperationWorker": { "Enabled": true },
  "EmailDispatchOperationWorker": { "Enabled": true },
  "ChannelDispatchOperationWorker": { "Enabled": true },
  "InvoiceArchiveMaintenanceWorker": { "Enabled": true, "PollIntervalMinutes": 1440, "BatchSize": 100 }
}
```

For development machines, keep outbound/side-effecting workers disabled unless validating that specific integration.

## 12. Production Smoke Checks

- Run the local readiness dry-run aggregator:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-go-live-readiness.ps1
```

It runs the secret scan and dry-run provider smoke prerequisite checks, including Stripe/DHL/Brevo worker-pipeline confirmations and object-storage profile readiness. Exit code `2` means one or more external smoke prerequisites are still intentionally blocked by missing deployment/account configuration.
The same dry-run also reports open deployment decisions for invoice archive object storage and e-invoice tooling.

- Start WebAdmin, WebApi, and Worker with the same provider, database, Data Protection, and media storage configuration.
- Confirm WebAdmin can read/write the configured database.
- Confirm WebApi health/public endpoints return success.
- Send one Brevo sandbox email, then one controlled production email after sandbox is disabled; use `scripts\smoke-brevo-readiness.ps1` for direct account/readiness validation.
- Replay or trigger one Brevo webhook and confirm provider callback processing.
- Run Stripe test-mode Checkout Session and webhook finalization smoke; use `scripts\smoke-stripe-testmode.ps1` for the local WebApi handoff and return-route guard before paying through Stripe test checkout.
- Run DHL live shipment/label/tracking callback smoke; use `scripts\smoke-dhl-live.ps1` only as a guarded validation harness before running the full WebAdmin/Worker operation smoke.
- Run VIES Valid, Invalid, and provider-failure smoke; use `scripts\smoke-vies-live.ps1` for direct VIES endpoint validation before WebAdmin operator-flow checks.
- Run selected-provider object-storage smoke against a disposable container/profile from WebAdmin Site Settings and with `scripts\smoke-object-storage.ps1 -Execute` before routing invoice archives, DHL labels, or CMS media to that provider. Use `-SmokeRetention` only for a disposable retained object that operators will inspect and clean up according to the provider's retention policy. The go-live readiness dry-run checks the generic storage smoke plus the `MediaAssets` and `ShipmentLabels` profiles; use `DARWIN_OBJECT_STORAGE_MEDIA_*` and `DARWIN_OBJECT_STORAGE_SHIPMENT_LABELS_*` overrides when those profiles use different disposable containers or prefixes.
- Confirm invoice archive download works and purge is disabled until retention policy is approved.
