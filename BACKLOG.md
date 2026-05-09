# Darwin Backlog

Reviewed: 2026-05-09

This file is the active roadmap for go-live planning. Historical implementation notes were moved to `docs/implementation-ledger.md`. Code-backed status is tracked in `docs/go-live-status.md` and the module audit matrix in `docs/module-audit.md`.

## Current Go-Live Blockers

- `Stripe`: run test-mode smoke with the keys entered through Settings, verify real Checkout Session creation, confirm storefront return pages do not mark payments successful, and confirm verified Stripe webhooks finalize payment/order state. Later, repeat the smoke with live keys before production traffic.
- `DHL`: run live smoke against the target DHL account. Confirm account-specific base URL, authentication, product codes, shipment payload, label PDF retrieval/storage, tracking reference persistence, callback processing, and WebAdmin recovery for failed/stuck provider operations.
- `Brevo`: complete production DNS and account readiness: sender domain verification, DKIM/DMARC, sandbox off, Basic Auth webhook configured, `ProviderCallbackWorker` and `EmailDispatchOperationWorker` enabled.
- `VAT/VIES`: live-smoke VIES. Phase 1 policy remains soft `Unknown` plus manual review when VIES is disabled, unavailable, rate-limited, malformed, or timed out. Do not record false Valid/Invalid decisions on provider failure.
- `Invoice archive`: keep the internal/database archive provider as the development fallback. Select production object storage with immutable retention/legal hold before claiming production archive immutability.
- `E-invoice`: implement real ZUGFeRD/Factur-X generation and validation before claiming full e-invoice compliance. XRechnung remains a secondary export backlog item.
- `Business onboarding`: add hosted smoke coverage for the admin-assisted onboarding lifecycle, approval prerequisites, invitation resend/revoke, email-confirm enforcement, reactivation, and public visibility.
- `Inventory/returns`: add hosted operator-flow smoke coverage for supplier receiving, stock transfer, order cancel stock release, return receipt idempotency, and refund coordination.

## Near-Term Tasks

- Rewrite the quarantined WebAdmin source-contract tests (`47` WebAdmin-surface skipped, `210` active passed locally on 2026-05-09) into stable security/localization/HTMX/route/mutation assertions.
- Run Stripe test-mode smoke after test keys and `StripeWebhookSecret` are entered in Settings/secure configuration.
- Run DHL live smoke after target account credentials and product settings are entered.
- Add async retry planning for VIES `Unknown` results; keep phase 1 operator-visible manual review.
- Design `IInvoiceArchiveStorage` production adapter after choosing Azure Blob, S3, MinIO, or another object-storage provider.
- Design the e-invoice generation service: map issued invoice snapshots to a structured invoice model, generate ZUGFeRD/Factur-X, validate output, expose downloadable artifacts, and add tests. Add XRechnung export later.
- Add hosted business onboarding smoke tests using the existing WebAdmin test host and no-op/fake email sender.
- Add hosted inventory/returns operator-flow smoke tests for the five deterministic scenarios listed above.
- Plan the WebAdmin admin-assisted onboarding wizard for businesses joining the Loyan loyalty system; do not implement it in the current provider/compliance slice.
- Grow WebAdmin CI coverage thresholds only after stable green CI history.

## Later-Phase Tasks

- Carrier-integrated DHL RMA/returns automation beyond the current returns queue and shipment provider operations.
- Brevo provider-managed template lifecycle mapped to Darwin template keys.
- External immutable invoice archive provider implementation after provider selection.
- Full ZUGFeRD/Factur-X plus secondary XRechnung export implementation.
- Self-service onboarding in `Darwin.Web` after WebAdmin/admin-assisted onboarding is stable.
- Additional public catalog/CMS facet/search projection improvements for scale.

## Open Decisions

- Object storage provider for invoice archives: Azure Blob, S3, MinIO, or another deployment-specific provider.
- E-invoice library/tooling for ZUGFeRD/Factur-X PDF/A-3 embedding and structured XML validation.
- Exact DHL account/product-code contract after target-account live smoke.
- When to rotate from Stripe test keys to live keys, and which restricted live key permissions are sufficient for Checkout Session creation and webhook reconciliation.

## Active Handoff Summary

- PostgreSQL remains the preferred/default runtime provider; SQL Server remains supported.
- WebAdmin remains MVC/Razor + HTMX.
- `Darwin.Web` remains a separate Next.js customer-facing application and must not receive operator diagnostics.
- Storefront payment completion remains verified-webhook-only.
- No provider secrets belong in git, docs, logs, or test output.

