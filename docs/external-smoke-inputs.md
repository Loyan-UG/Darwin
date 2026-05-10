# External Smoke Inputs

Reviewed: 2026-05-10

This file lists the inputs needed to run external provider smoke checks. Do not store real secret values in this file or any committed configuration.

Run the dry-run aggregator first:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-go-live-readiness.ps1
```

Exit code `2` means one or more external smoke checks are blocked by missing account or deployment inputs. That is expected until the provider settings are available.

## Stripe Test Mode

Secrets and account settings must be entered through Site Settings or secure configuration:

- Stripe test secret key.
- Stripe test publishable key for the front-office client.
- Stripe webhook signing secret.
- Stripe enabled flag.

Local smoke environment variables:

- `DARWIN_WEBAPI_BASE_URL`
- `DARWIN_STRIPE_SMOKE_ORDER_ID`, unless using `-CreateSmokeOrder`
- `DARWIN_STRIPE_SMOKE_ORDER_NUMBER`, unless using `-CreateSmokeOrder`
- `DARWIN_BUSINESS_API_BEARER_TOKEN`, when using `-CheckBusinessSubscriptionCheckout`
- `DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID`, when using `-CheckBusinessSubscriptionCheckout`

Webhook forwarding variables:

- `DARWIN_STRIPE_WEBHOOK_PUBLIC_URL`, when using a Stripe Dashboard endpoint or HTTPS tunnel. It must end with `/api/v1/public/billing/stripe/webhooks`.
- `DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED=true`, after Stripe Dashboard delivery or Stripe CLI forwarding is configured.
- `DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED=true`, after ProviderCallbackWorker is enabled for the target environment.

Commands:

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

The final provider result must still come from verified Stripe webhooks, not from the storefront return route.
Use `-CreateSmokeOrder` to create a disposable storefront order through public cart/checkout before the Stripe handoff. Use `-CheckBusinessSubscriptionCheckout` with a business API bearer token and an active billing-plan id to verify the authenticated business subscription endpoint creates a Stripe-hosted subscription Checkout Session. Use `-OpenCheckout` when you are ready to pay the Stripe test Checkout Session in a browser. The script opens the hosted checkout URL but does not print the URL or provider references. Use `-WaitForWebhookFinalization` after webhook delivery is configured; the script now blocks before creating the checkout session unless Stripe Dashboard delivery or Stripe CLI forwarding has been confirmed, then polls Darwin WebApi confirmation state until the verified webhook updates the payment to `Captured` or `Completed`.
If using Stripe CLI, forward events to `DARWIN_WEBAPI_BASE_URL/api/v1/public/billing/stripe/webhooks` and enter the CLI-provided webhook signing secret securely in Site Settings for the same database used by WebApi. The forwarding preflight never accepts or prints that secret.

The current WebApi source rejects local/mock Stripe handoff fallback. If execute mode does not return a Stripe-hosted URL, rebuild/restart WebApi and verify that `StripeEnabled` plus the test server-side key are configured for the same database/settings source used by that WebApi instance.

## DHL Live Smoke

Secrets and account settings must be entered through Site Settings or secure configuration:

- DHL API key.
- DHL API secret.
- DHL account number.
- DHL base URL. Non-local endpoints must use HTTPS.
- Product code confirmed for the target account.

Local smoke environment variables:

- `DARWIN_DHL_API_BASE_URL`
- `DARWIN_DHL_API_KEY`
- `DARWIN_DHL_API_SECRET`
- `DARWIN_DHL_ACCOUNT_NUMBER`
- `DARWIN_DHL_PRODUCT_CODE` optional; defaults to `V01PAK`.
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
- `DARWIN_DHL_TEST_RECEIVER_PHONE_E164` optional.
- `DARWIN_DHL_SHIPMENT_PROVIDER_OPERATION_WORKER_CONFIRMED=true`, after ShipmentProviderOperationWorker is enabled.
- `DARWIN_DHL_PROVIDER_CALLBACK_WORKER_CONFIRMED=true`, after ProviderCallbackWorker is enabled.
- `DARWIN_DHL_SHIPMENT_LABELS_STORAGE_CONFIRMED=true`, after the ShipmentLabels object-storage profile or shared media fallback is validated.

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -RequireRuntimePipeline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute -IncludeReturn
```

Use `-IncludeReturn` after outbound validation is approved to validate the return-shipment sender/receiver mapping against the same account and product code. Do not create fake DHL labels, references, or tracking URLs. Carrier-integrated RMA remains a separate implementation slice.

## Brevo

Secrets and account settings must be entered through secure configuration:

- Brevo API key.
- Webhook Basic Auth username/password.
- Sender domain verification.
- DKIM and DMARC.
- Sandbox mode off only for production delivery.

Local smoke environment variables:

- `DARWIN_BREVO_API_KEY`
- `DARWIN_BREVO_SENDER_EMAIL`
- `DARWIN_BREVO_TEST_RECIPIENT_EMAIL`
- `DARWIN_BREVO_BASE_URL` optional; non-local endpoints must use HTTPS.
- `DARWIN_BREVO_SENDER_NAME` optional.
- `DARWIN_BREVO_REPLY_TO_EMAIL` optional.
- `DARWIN_BREVO_WEBHOOK_PUBLIC_URL` for production pipeline readiness; must end with `/api/v1/public/notifications/brevo/webhooks`.
- `DARWIN_BREVO_WEBHOOK_CONFIGURED_CONFIRMED=true` after the webhook is configured in Brevo.
- `DARWIN_BREVO_TRANSACTIONAL_EVENTS_CONFIRMED=true` after transactional webhook events are subscribed.
- `DARWIN_BREVO_PROVIDER_CALLBACK_WORKER_CONFIRMED=true` after ProviderCallbackWorker is enabled.
- `DARWIN_BREVO_EMAIL_DISPATCH_WORKER_CONFIRMED=true` after EmailDispatchOperationWorker is enabled.

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -RequireDeliveryPipeline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute -Sandbox
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute
```

Run the controlled-inbox send only after DNS, sender approval, webhook subscription, and worker enablement are complete. Non-sandbox execute mode requires the delivery pipeline confirmations and does not accept or print webhook Basic Auth credentials.

## VIES

Configuration:

- `Compliance:VatValidation:Vies:Enabled=true`.
- Endpoint URL and timeout confirmed for the deployment.

Local smoke environment variables:

- `DARWIN_VIES_VALID_VAT_ID`
- `DARWIN_VIES_INVALID_VAT_ID`
- `DARWIN_VIES_ENDPOINT_URL` optional; if set, it must be an absolute URL and non-local endpoints must use HTTPS.
- `DARWIN_VIES_TIMEOUT_SECONDS` optional; if set, it must be between 1 and 120 seconds.

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-vies-live.ps1 -Execute -CheckProviderFailure
```

Provider failures must remain `Unknown` with manual review, not false `Valid` or `Invalid`.

## Object Storage

Use this smoke only after the selected storage provider and disposable smoke container/bucket are configured. The script creates, reads, inspects, optionally generates a temporary URL for, and deletes one disposable object unless retention smoke is enabled.

Common environment variables:

- `DARWIN_OBJECT_STORAGE_PROVIDER`: `S3Compatible`, `AzureBlob`, or `FileSystem`.
- `DARWIN_OBJECT_STORAGE_PROFILE` optional; defaults to `Smoke`.
- `DARWIN_OBJECT_STORAGE_CONTAINER`
- `DARWIN_OBJECT_STORAGE_PREFIX` optional; use it to verify profile-level object-key prefix behavior before routing CMS media, DHL labels, invoice archive, or export artifacts to that provider.
- `DARWIN_OBJECT_STORAGE_MEDIA_CONTAINER` optional; overrides the common container for the `MediaAssets` readiness check.
- `DARWIN_OBJECT_STORAGE_MEDIA_PREFIX` optional; overrides the common prefix for the `MediaAssets` readiness check.
- `DARWIN_OBJECT_STORAGE_SHIPMENT_LABELS_CONTAINER` optional; overrides the common container for the `ShipmentLabels` readiness check.
- `DARWIN_OBJECT_STORAGE_SHIPMENT_LABELS_PREFIX` optional; overrides the common prefix for the `ShipmentLabels` readiness check.
- `DARWIN_OBJECT_STORAGE_PUBLIC_BASE_URL` optional.
- `DARWIN_OBJECT_STORAGE_REQUIRE_OBJECT_LOCK` optional.
- `DARWIN_OBJECT_STORAGE_RETENTION_MODE` optional.
- `DARWIN_OBJECT_STORAGE_LEGAL_HOLD_ENABLED` optional.
- `DARWIN_OBJECT_STORAGE_VALIDATION_MODE` optional.
- `DARWIN_OBJECT_STORAGE_SMOKE_RETENTION` optional; when `true`, delete cleanup is skipped to let operators inspect retention behavior.

S3-compatible / MinIO / AWS S3 variables:

- `DARWIN_OBJECT_STORAGE_S3_BUCKET`
- `DARWIN_OBJECT_STORAGE_S3_ENDPOINT` or `DARWIN_OBJECT_STORAGE_S3_REGION`
- `DARWIN_OBJECT_STORAGE_S3_ACCESS_KEY`
- `DARWIN_OBJECT_STORAGE_S3_SECRET_KEY`
- `DARWIN_OBJECT_STORAGE_S3_USE_SSL` optional.
- `DARWIN_OBJECT_STORAGE_S3_USE_PATH_STYLE` optional.
- `DARWIN_OBJECT_STORAGE_S3_FORCE_PATH_STYLE` optional.

Azure Blob variables:

- `DARWIN_OBJECT_STORAGE_AZURE_CONTAINER`
- `DARWIN_OBJECT_STORAGE_AZURE_CONNECTION_STRING`, or managed identity with:
- `DARWIN_OBJECT_STORAGE_AZURE_ACCOUNT_NAME`
- `DARWIN_OBJECT_STORAGE_AZURE_USE_MANAGED_IDENTITY=true`
- `DARWIN_OBJECT_STORAGE_AZURE_CLIENT_ID` optional for user-assigned managed identity.

File-system fallback variable:

- `DARWIN_OBJECT_STORAGE_FILE_ROOT`

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute -SmokeRetention
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Provider FileSystem -ContainerName smoke -FileRoot C:\DarwinStorageSmoke -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -ProfileName MediaAssets
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -ProfileName ShipmentLabels
```

Do not run the execute mode against an invoice archive production container until retention/legal-hold policy and cleanup expectations are explicit. Use a disposable smoke container or object prefix first. Use `-SmokeRetention` only when the selected provider/container is expected to keep the disposable retained object for operator inspection; the script intentionally skips delete cleanup in that mode.
