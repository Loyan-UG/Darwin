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
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute -Sandbox
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute
```

The first command verifies local prerequisites only. `-Execute -Sandbox` calls Brevo with the sandbox/drop header so account authentication and request shape can be checked before real delivery. `-Execute` sends one controlled-inbox message after sender DNS and operational approval are ready. The script does not print secrets or raw Brevo response payloads.

Implementation note: Darwin renders transactional templates internally by default. When a Brevo template ID is configured for the current Darwin `TemplateKey` or `FlowKey`, Brevo receives `templateId` plus sanitized template parameters instead of inline HTML content. Provider-side template authoring and synchronization remains an optional operational workflow.

## 6. Stripe

Stripe webhook ingress is `/api/v1/public/billing/stripe/webhooks`. It validates `Stripe-Signature` before writing idempotent provider callback inbox records.

Stripe test-mode smoke checklist:

- Test publishable and server-side key entered in Settings or secure configuration.
- Stripe enabled in Site Settings.
- Stripe webhook signing secret entered securely.
- Checkout Session is created through WebApi.
- Storefront return URL is reached.
- Return route alone does not mark the payment successful.
- Verified webhook updates payment/order state.
- Failed payment, refund, and dispute states are visible in WebAdmin.

Optional local harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -CreateSmokeOrder
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckReturnRoute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckReturnRoute -OpenCheckout
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder -CheckReturnRoute -OpenCheckout -WaitForWebhookFinalization
```

The first command verifies local prerequisites only. The execute mode calls Darwin WebApi to create a Stripe Checkout Session for a pre-existing smoke order and confirms the returned checkout URL is Stripe-hosted while the payment remains `Pending`. `-CreateSmokeOrder` first creates a disposable storefront order through public cart/checkout so the smoke does not require pre-seeded order environment variables. `-CheckReturnRoute` also verifies the storefront return route does not capture or complete Stripe payments without verified webhook events. `-OpenCheckout` opens the hosted Stripe checkout in the default browser without printing the session URL or provider references. `-WaitForWebhookFinalization` polls the Darwin WebApi order confirmation endpoint after test payment and fails unless verified webhook processing moves the payment to `Captured` or `Completed`. Stripe keys and webhook signing secrets must be entered through Settings or secure configuration; this script does not accept, print, or persist secrets.

Latest local test-mode handoff result: this script passed on 2026-05-10 against an isolated WebApi build from the current source with `-CreateSmokeOrder`, created a disposable public checkout smoke order, created a Stripe-hosted Checkout Session, and confirmed the return route left the payment `Pending`. The focused WebApi Stripe webhook/provider-callback suite also passed on 2026-05-10 with `191` passed and `0` skipped using an isolated output path. Hosted checkout payment and verified webhook delivery still need to be completed before production traffic. If an older local WebApi process is still running, stop or isolate it before relying on smoke results because stale binaries can still return the development mock-checkout URL.

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
- Callback is accepted and processed.
- Failed/stuck provider operations are recoverable in WebAdmin.

Optional local harness:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute
```

The first command verifies required environment variables only. The `-Execute` command sends a real DHL validation request and must only be used against the target account after credentials, product code, shipper, and receiver smoke-test data are approved. The script does not print secrets or raw DHL response payloads.

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
      "ObjectLockValidationMode": "FailFast"
    }
  }
}
```

Supply `ObjectStorage:S3Compatible:AccessKey` and `ObjectStorage:S3Compatible:SecretKey` only through secure configuration.

MinIO production checklist:

- MinIO runs outside the Darwin application process.
- Dedicated storage volumes are configured; serious production deployments do not place MinIO and the database on the same disk.
- TLS is enabled.
- Darwin uses a dedicated least-privilege access key, not root credentials.
- Dedicated invoice archive bucket is created with Object Lock enabled from the start.
- Bucket versioning is enabled before go-live writes.
- Default retention is configured; use COMPLIANCE mode when legal retention is confirmed.
- Backup, replication, offsite copy, disk-usage monitoring, and failed-write alerts are configured.
- Restore is tested before go-live.

Checklist:

- Internal/database archive enabled for development/internal fallback.
- File-system archive provider selected with `InvoiceArchiveStorage:ProviderName=FileSystem` only for development, internal deployments, or controlled shared storage where deployment-level retention is understood.
- File-system archive provider root path configured under `InvoiceArchiveStorage:FileSystem:RootPath`.
- Generic `ObjectStorage` provider validation enabled in WebAdmin, WebApi, and Worker startup.
- S3-compatible production profile configured for MinIO or AWS S3; Azure Blob remains an alternative provider boundary until its SDK adapter is implemented.
- Invoice archive can be routed to the generic S3-compatible path with `InvoiceArchiveStorage:ProviderName=S3Compatible` after the target bucket passes Object Lock/versioning smoke.
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

It runs the secret scan and dry-run provider smoke prerequisite checks. Exit code `2` means one or more external smoke prerequisites are still intentionally blocked by missing deployment/account configuration.
The same dry-run also reports open deployment decisions for invoice archive object storage and e-invoice tooling.

- Start WebAdmin, WebApi, and Worker with the same provider, database, Data Protection, and media storage configuration.
- Confirm WebAdmin can read/write the configured database.
- Confirm WebApi health/public endpoints return success.
- Send one Brevo sandbox email, then one controlled production email after sandbox is disabled; use `scripts\smoke-brevo-readiness.ps1` for direct account/readiness validation.
- Replay or trigger one Brevo webhook and confirm provider callback processing.
- Run Stripe test-mode Checkout Session and webhook finalization smoke; use `scripts\smoke-stripe-testmode.ps1` for the local WebApi handoff and return-route guard before paying through Stripe test checkout.
- Run DHL live shipment/label/tracking callback smoke; use `scripts\smoke-dhl-live.ps1` only as a guarded validation harness before running the full WebAdmin/Worker operation smoke.
- Run VIES Valid, Invalid, and provider-failure smoke; use `scripts\smoke-vies-live.ps1` for direct VIES endpoint validation before WebAdmin operator-flow checks.
- Confirm invoice archive download works and purge is disabled until retention policy is approved.
