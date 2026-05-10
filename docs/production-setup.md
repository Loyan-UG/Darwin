# Darwin Production Setup Runbook

This runbook lists the production configuration and smoke checks required before live traffic. Do not commit secrets to git; use environment variables, platform secrets, .NET user-secrets for local work, or a server-side secret store.

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
- API key stored only in secure configuration.
- `Email:Brevo:SandboxMode=false` for production traffic.
- Webhook Basic Auth username/password set.
- Brevo webhook URL configured at `/api/v1/public/notifications/brevo/webhooks`.
- Transactional events subscribed: request, delivered, deferred, soft_bounce, hard_bounce, spam, blocked, invalid, error, opened, click.
- `ProviderCallbackWorker:Enabled=true`.
- `EmailDispatchOperationWorker:Enabled=true`.

Implementation note: Darwin currently renders transactional templates internally and sends inline content through Brevo. Provider-managed template IDs remain a backlog item.

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

Live keys should be entered only when production smoke is scheduled. Storefront payment finalization must remain verified-webhook-only.

## 7. DHL

DHL webhook ingress is `/api/v1/public/shipping/dhl/webhooks`. It validates API key and HMAC signature before writing idempotent provider callback inbox records.

DHL live smoke checklist:

- Target account credentials entered in Settings or secure configuration.
- Base URL and product codes confirmed for the account.
- Shared media storage configured through `MediaStorage:RootPath` and `MediaStorage:PublicBaseUrl`.
- Create-shipment request succeeds.
- Label PDF is returned or retrievable.
- Label is stored in shared media storage.
- Tracking number/reference is stored.
- Callback is accepted and processed.
- Failed/stuck provider operations are recoverable in WebAdmin.

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

Future work: async retry workflow for Unknown provider results.

## 9. Invoice Archive / Retention

Current provider: internal/database-backed archive storage through `IInvoiceArchiveStorage`.

Production target: external object storage with immutable retention/legal hold. Do not claim production archive immutability until the storage provider and legal-hold behavior are implemented and smoke-tested.

Checklist:

- Internal/database archive enabled for development/internal fallback.
- Production object-storage provider selected and documented.
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

- Start WebAdmin, WebApi, and Worker with the same provider, database, Data Protection, and media storage configuration.
- Confirm WebAdmin can read/write the configured database.
- Confirm WebApi health/public endpoints return success.
- Send one Brevo sandbox email, then one controlled production email after sandbox is disabled.
- Replay or trigger one Brevo webhook and confirm provider callback processing.
- Run Stripe test-mode Checkout Session and webhook finalization smoke.
- Run DHL live shipment/label/tracking callback smoke.
- Run VIES Valid, Invalid, and provider-failure smoke.
- Confirm invoice archive download works and purge is disabled until retention policy is approved.
