# External Smoke Inputs

Reviewed: 2026-05-26

This file lists non-committed inputs for external smoke checks. Do not store real secret values in this file or any committed configuration. Do not store real provider secrets, API keys, webhook secrets, access keys, or private signing material in this file.

Run the aggregate dry-run first:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-go-live-readiness.ps1
```

Exit code `2` means one or more checks are blocked by missing operator inputs. That is expected before a provider is fully configured.

## Stripe Test Mode

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

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-stripe-live-readiness.ps1
```

Live-mode execution still requires explicit operator approval.

## Brevo

Runtime application settings are DB-backed through Site Settings. This script is a direct operator harness and expects environment variables in the current shell.

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

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute -Sandbox
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

Commands:

```powershell
dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-minio-production-readiness.ps1
```

Acceptance:

- Local smoke is optional and does not prove production immutability.
- Production immutability requires provider-level validation on the real bucket/container.

## E-Invoice External Command

Required variable:

- `DARWIN_EINVOICE_COMMAND_PATH`

Optional variables:

- `DARWIN_EINVOICE_FORMAT`
- `DARWIN_EINVOICE_VALIDATION_PROFILE`

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-einvoice-external-command.ps1 -Format XRechnung -Execute
```

Acceptance:

- The adapter can call the approved wrapper.
- Output shape and validation-report handling pass.
- Smoke success does not by itself prove legal e-invoice compliance.

## Mobile Maps And Push

Android maps:

- Provide `GoogleMapsApiKey`, `GOOGLE_MAPS_API_KEY`, or `ANDROID_GOOGLE_MAPS_API_KEY` at build time.
- Restrict the key to the Android package and signing certificate fingerprint.

Android push:

- Provide approved Firebase `google-services.json` at build time.
- Keep Firebase service-account credentials outside the repository.

iOS/MacCatalyst push:

- Provide Apple Developer App ID, provisioning profile, Push Notifications capability, and APNS provider credentials through secure configuration.

Acceptance:

- Signed release builds include production mobile configuration.
- Device smoke registers push tokens and verifies logout/account-switch behavior.
- Physical camera QR scanning is validated with real device/camera input.
