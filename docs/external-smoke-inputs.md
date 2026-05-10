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

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -CreateSmokeOrder
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckReturnRoute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CheckReturnRoute -OpenCheckout
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-stripe-testmode.ps1 -Execute -CreateSmokeOrder -CheckReturnRoute -OpenCheckout -WaitForWebhookFinalization
```

The final provider result must still come from verified Stripe webhooks, not from the storefront return route.
Use `-CreateSmokeOrder` to create a disposable storefront order through public cart/checkout before the Stripe handoff. Use `-OpenCheckout` when you are ready to pay the Stripe test Checkout Session in a browser. The script opens the hosted checkout URL but does not print the URL or provider references. Use `-WaitForWebhookFinalization` after webhook delivery is configured; it polls Darwin WebApi confirmation state until the verified webhook updates the payment to `Captured` or `Completed`.

If the execute command returns a local mock-checkout URL instead of a Stripe-hosted URL, verify that the WebApi process is running the current source and that `StripeEnabled` plus the test server-side key are configured for the same database/settings source used by that WebApi instance.

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

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-dhl-live.ps1 -Execute
```

Do not create fake DHL labels, references, or tracking URLs. Carrier-integrated RMA remains a separate implementation slice.

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

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute -Sandbox
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-brevo-readiness.ps1 -Execute
```

Run the controlled-inbox send only after DNS and sender approval are complete.

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
