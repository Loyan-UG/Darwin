# Darwin Production Setup Runbook

This runbook tracks operational setup that must exist outside the application code before a real server can send live traffic. Keep it updated whenever production-facing configuration changes.

## Configuration Sources

Use environment variables, platform secrets, or a server-side secret store for secrets. Do not place real credentials in `appsettings.json`.

Production `appsettings.json` intentionally leaves secrets and provider-owned identities empty or disabled. Missing required values should fail fast or keep the integration disabled until the server environment supplies real values.

Common environment variable shape:

```powershell
$env:Persistence__Provider = "PostgreSql"
$env:ConnectionStrings__PostgreSql = "Host=...;Port=5432;Database=darwin;Username=darwin_app;Password=..."
$env:Email__Provider = "Brevo"
$env:Email__Brevo__ApiKey = "xkeysib-..."
$env:Email__Brevo__SenderEmail = "no-reply@example.com"
$env:Email__Brevo__SenderName = "Darwin"
$env:Email__Brevo__ReplyToEmail = "support@example.com"
$env:Email__Brevo__ReplyToName = "Darwin Support"
$env:Email__Brevo__WebhookUsername = "brevo-darwin"
$env:Email__Brevo__WebhookPassword = "REPLACE_WITH_STRONG_SECRET"
$env:Email__Brevo__SandboxMode = "false"
$env:DataProtection__KeysPath = "D:\Darwin\_shared_keys"
$env:DataProtection__RequireKeyEncryption = "true"
$env:DataProtection__CertificateThumbprint = "..."
$env:Jwt__SigningKey = "REPLACE_WITH_32_BYTE_OR_LONGER_SECRET"
```

## Database

- Preferred provider: `PostgreSql`.
- Customer-specific SQL Server deployments remain supported by setting `Persistence:Provider=SqlServer`.
- Apply migrations with the provider-specific migration project that matches the server provider.
- PostgreSQL production users should not be superusers after initial provisioning. Grant only the rights needed for the app and migration role.
- For PostgreSQL provider details, extension requirements, and post-migration checks, see `docs/postgresql-migration-runbook.md` and `docs/persistence-providers.md`.

Minimum PostgreSQL preflight:

```sql
create database darwin;
create user darwin_app with encrypted password 'REPLACE_ME';
grant connect on database darwin to darwin_app;
```

Migration-time role must be able to create required extensions:

- `citext`
- `pg_trgm`

## Transactional Email: Brevo

Darwin now supports provider selection through `Email:Provider`:

- `Brevo`: primary production provider.
- `SMTP`: local/development fallback and customer-specific SMTP fallback.

Brevo API configuration lives under `Email:Brevo`:

```json
{
  "Email": {
    "Provider": "Brevo",
    "Brevo": {
      "BaseUrl": "https://api.brevo.com/v3/",
      "ApiKey": "SET_BY_SECRET",
      "SenderEmail": "no-reply@example.com",
      "SenderName": "Darwin",
      "ReplyToEmail": "support@example.com",
      "ReplyToName": "Darwin Support",
      "WebhookUsername": "SET_BY_SECRET",
      "WebhookPassword": "SET_BY_SECRET",
      "SandboxMode": false,
      "TimeoutSeconds": 30,
      "DefaultTags": [ "darwin", "transactional" ]
    }
  }
}
```

Brevo server setup checklist:

1. Create or upgrade the Brevo account.
2. Create an API key and store it as `Email__Brevo__ApiKey`.
3. Configure `Email__Brevo__SenderEmail` with a professional sender domain, not a free mailbox domain.
4. Authenticate the sender domain in Brevo.
5. Add/verify Brevo DNS records for domain ownership, DKIM, and DMARC.
6. Set a monitored reply-to mailbox with `Email__Brevo__ReplyToEmail`.
7. Set `Email__Brevo__WebhookUsername` and `Email__Brevo__WebhookPassword` to strong values.
8. In Brevo transactional webhooks, configure the notify URL with Basic Auth:
   `https://<username>:<password>@<host>/api/v1/public/notifications/brevo/webhooks`
9. Subscribe the Brevo webhook to transactional email events at least for `request`, `delivered`, `deferred`, `soft_bounce`, `hard_bounce`, `spam`, `blocked`, `invalid`, `error`, `opened`, and `click`.
10. Keep `Email__Brevo__SandboxMode=true` only for integration validation. Set it to `false` for real delivery.
11. Confirm `Darwin.Worker` is deployed and `EmailDispatchOperationWorker:Enabled=true` if queued admin test emails should be processed.
12. Confirm `ProviderCallbackWorker:Enabled=true` so Brevo webhook inbox messages are processed.

Current implementation details:

- Direct application sends resolve `IEmailSender` based on `Email:Provider`.
- Queued admin communication-test emails store the active provider name and `Darwin.Worker` dispatches either `Brevo` or `SMTP`.
- Brevo sends through `POST /v3/smtp/email`.
- Brevo request payload includes sender, reply-to when configured, HTML content, generated text content, tags, and correlation/idempotency headers when the current flow supplies a correlation key.
- `EmailDispatchAudit.ProviderMessageId` stores the Brevo `messageId` when Brevo returns it.
- Brevo transactional webhooks are received at `/api/v1/public/notifications/brevo/webhooks`, require configured Basic Auth, are stored idempotently in `ProviderCallbackInboxMessages`, and are processed by `ProviderCallbackWorker`.

Operational gaps intentionally kept in backlog:

- Brevo template IDs are not yet mapped to Darwin template keys; Darwin still renders transactional HTML internally and sends inline content.

## SMTP Fallback

SMTP remains available through:

```json
{
  "Email": {
    "Provider": "SMTP",
    "Smtp": {
      "Host": "smtp-relay.example.com",
      "Port": 587,
      "EnableSsl": true,
      "Username": "smtp-user",
      "Password": "SET_BY_SECRET",
      "FromAddress": "no-reply@example.com",
      "FromDisplayName": "Darwin"
    }
  }
}
```

Use SMTP only for local development, emergency fallback, or customer environments that require their own relay.

## Data Protection

- All entry points must share the same `DataProtection:ApplicationName`.
- Use a shared `DataProtection:KeysPath` across WebAdmin, WebApi, and Worker on the same deployment.
- Enable `DataProtection:RequireKeyEncryption=true` in production when certificate-backed key encryption is available.
- Configure `DataProtection:CertificateThumbprint` before enabling encryption enforcement.

## WebApi Authentication

- Set `Jwt__SigningKey` to a high-entropy secret of at least 32 bytes.
- Keep `Jwt__Issuer` and `Jwt__Audience` aligned with the mobile/client configuration.
- Rotate JWT signing keys through Site Settings after go-live; do not rely on seeded development keys for production.

## Push Providers

- FCM/APNS are disabled in production defaults until real provider credentials are supplied.
- Enable `PushProviders__Fcm__Enabled` or `PushProviders__Apns__Enabled` only after the server keys, sender/team/key identifiers, APNS auth key path, and bundle identifiers are set.
- Keep `InactiveReminderWorker:Enabled=false` until push delivery has been smoke-tested against the target gateway/provider.

## Worker Processes

Production worker switches should be explicit:

```json
{
  "WebhookDeliveryWorker": { "Enabled": true },
  "ProviderCallbackWorker": { "Enabled": true },
  "ShipmentProviderOperationWorker": { "Enabled": true },
  "EmailDispatchOperationWorker": { "Enabled": true },
  "ChannelDispatchOperationWorker": { "Enabled": true }
}
```

For development machines, keep outbound/side-effecting workers disabled unless validating that specific integration.

## Production Smoke Checks

After deployment:

1. Start WebAdmin, WebApi, and Worker with the same provider and Data Protection settings.
2. Confirm WebAdmin starts and can read/write the configured database.
3. Confirm WebApi health/public endpoint returns success.
4. In Brevo sandbox mode, send a transactional email request and confirm Darwin records a successful `EmailDispatchAudit` without real delivery.
5. Turn sandbox mode off and send one controlled email to the configured test inbox.
6. Confirm Brevo shows the message in transactional logs and Darwin stores the provider message id.
7. Trigger or replay one Brevo webhook event and confirm `ProviderCallbackInboxMessages` records then processes it.
