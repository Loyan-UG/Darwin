# Darwin Backlog

Reviewed: 2026-05-10

This file is the active roadmap for go-live planning. Historical implementation notes were moved to `docs/implementation-ledger.md`. Code-backed status is tracked in `docs/go-live-status.md` and the module audit matrix in `docs/module-audit.md`.

## Current Go-Live Blockers

These are blocked on external credentials, deployment choices, provider account setup, or legal/compliance tooling decisions. Internal code-backed work that can be completed without those inputs is tracked in `docs/go-live-status.md`, `docs/module-audit.md`, and `docs/compliance-decisions.md`.

- `Stripe`: run test-mode smoke with the keys entered through Settings, verify real Checkout Session creation, confirm storefront return pages do not mark payments successful, and confirm verified Stripe webhooks finalize payment/order state. Later, repeat the smoke with live keys before production traffic.
- `DHL`: run live smoke against the target DHL account. Confirm account-specific base URL, authentication, product codes, shipment payload, label PDF retrieval/storage, tracking reference persistence, callback processing, and WebAdmin recovery for failed/stuck provider operations.
- `Brevo`: complete production DNS and account readiness: sender domain verification, DKIM/DMARC, sandbox off, Basic Auth webhook configured, `ProviderCallbackWorker` and `EmailDispatchOperationWorker` enabled.
- `VAT/VIES`: live-smoke VIES. Phase 1 policy remains soft `Unknown` plus manual review when VIES is disabled, unavailable, rate-limited, malformed, or timed out. Do not record false Valid/Invalid decisions on provider failure. Internal provider policy coverage now exists; external VIES behavior still needs deployment smoke.
- `Invoice archive`: keep the internal/database archive provider as the development fallback. Select production object storage with immutable retention/legal hold before claiming production archive immutability.
- `E-invoice`: implement real ZUGFeRD/Factur-X generation and validation before claiming full e-invoice compliance. XRechnung remains a secondary export backlog item.

## Completed Internal Go-Live Baseline

- `Business onboarding`: hosted WebAdmin smoke covers creation into inactive `PendingApproval`, approval prerequisite failure, approve/suspend/reactivate lifecycle forms, invitation resend/revoke, and business-location row-version mutation. WebApi-hosted smoke covers email-confirm enforcement and public visibility against PostgreSQL.
- `Inventory/returns`: hosted operator-flow smoke coverage exercises stock reserve/release, order cancel stock release, return receipt idempotency, refund coordination without unintended stock movement, stock-transfer MarkInTransit/Complete, and purchase-order Issue/Receive through WebAdmin forms.
- `Source contracts`: WebAdmin source-contract (`257` passed, `0` skipped), Business source-contract (`87` passed, `0` skipped), all-source `SecurityAndPerformance` (`599` passed, `0` skipped), and broader Inventory/Business/Invitation/SignIn/Tax/Invoice/VAT (`981` passed, `0` skipped) are clean as of 2026-05-10.
- `Infrastructure tests`: `Darwin.Infrastructure.Tests` now passes locally with `45` passed and `0` skipped after provider-specific design-time factory and injected-clock alignment.

## Near-Term Tasks

- Keep the source-contract lanes green after the 2026-05-10 cleanup completion: WebAdmin source-contract (`257` passed, `0` skipped), Business source-contract (`87` passed, `0` skipped), and all-source `SecurityAndPerformance` (`599` passed, `0` skipped). New source-contract changes should use stable security/localization/HTMX/route/mutation/public-boundary assertions instead of exact copy/layout assertions.
- Run Stripe test-mode smoke after test keys and `StripeWebhookSecret` are entered in Settings/secure configuration.
- Run DHL live smoke after target account credentials and product settings are entered.
- Implement VIES async retry after the planned workflow in `docs/compliance-decisions.md` is approved; keep phase 1 operator-visible manual review.
- Design `IInvoiceArchiveStorage` production adapter after choosing Azure Blob, S3, MinIO, or another object-storage provider.
- Implement the e-invoice generation service after library/tooling selection: map issued invoice snapshots to a structured invoice model, generate ZUGFeRD/Factur-X, validate output, expose downloadable artifacts, and add tests. Add XRechnung export later.
- Keep WebApi-hosted business onboarding smoke for email-confirm enforcement and public discovery/detail visibility green against PostgreSQL; keep the WebAdmin hosted lifecycle smoke green as onboarding evolves.
- Keep hosted inventory/returns operator-flow smoke coverage green as the return/RMA workflow evolves.
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
- Compliance planning decisions are summarized in `docs/compliance-decisions.md`.

