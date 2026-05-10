# Darwin Backlog

Reviewed: 2026-05-10

This file is the active roadmap for go-live planning. Historical implementation notes were moved to `docs/implementation-ledger.md`. Code-backed status is tracked in `docs/go-live-status.md` and the module audit matrix in `docs/module-audit.md`.

## Current Go-Live Blockers

These are blocked on external credentials, deployment choices, provider account setup, or legal/compliance tooling decisions. Internal code-backed work that can be completed without those inputs is tracked in `docs/go-live-status.md`, `docs/module-audit.md`, and `docs/compliance-decisions.md`.

- `Stripe`: the WebApi source rejects local/mock Stripe handoff fallback and requires provider-hosted Checkout Session creation for storefront payments and business subscription checkout. WebAdmin refund creation now calls the Stripe refund API for Stripe payments with provider references and stores provider refund status/failure details for operator review. Test-mode WebApi handoff and return-route guard passed locally against a rebuilt WebApi instance. A webhook forwarding preflight now exists at `scripts/check-stripe-webhook-forwarding.ps1`. Remaining Stripe blocker: configure Stripe Dashboard delivery or Stripe CLI forwarding, pay through Stripe test checkout, and confirm verified webhook events finalize payment/order/subscription/refund state. Later, repeat the smoke with live keys before production traffic.
- `DHL`: run live smoke against the target DHL account. Confirm account-specific base URL, authentication, product codes, outbound shipment payload, return-label payload, label PDF retrieval/storage, tracking reference persistence, callback processing, and WebAdmin recovery for failed/stuck provider operations. A guarded local prerequisite/validation harness exists at `scripts/smoke-dhl-live.ps1`; real smoke remains blocked until approved account settings are configured.
- `Brevo`: complete production DNS and account readiness: sender domain verification, DKIM/DMARC, sandbox off, Basic Auth webhook configured, transactional webhook events subscribed, `ProviderCallbackWorker` and `EmailDispatchOperationWorker` enabled. A guarded sandbox/controlled-inbox harness exists at `scripts/smoke-brevo-readiness.ps1`; its `-RequireDeliveryPipeline` preflight now blocks controlled-inbox smoke until the public webhook endpoint, subscription, and worker confirmations are present. Real production readiness still requires account DNS, delivery, and webhook verification.
- `VAT/VIES`: live-smoke VIES. Phase 1 policy remains soft `Unknown` plus manual review when VIES is disabled, unavailable, rate-limited, malformed, or timed out. Do not record false Valid/Invalid decisions on provider failure. Internal provider policy coverage and a disabled-by-default retry worker now exist; a guarded direct VIES harness exists at `scripts/smoke-vies-live.ps1`, and external VIES behavior still needs deployment smoke.
- `Invoice archive/object storage`: keep the internal/database archive provider as the development fallback. The code now has a named invoice archive provider router, a direct file-system provider, a reusable `ObjectStorage` abstraction/options boundary, a provider/profile router with centralized container/prefix defaults, an SDK-backed S3-compatible adapter for MinIO/AWS-style storage, Azure Blob archive routing, and a generic file-system provider. Production direction is MinIO through the S3-compatible provider, with AWS S3 and Azure Blob as alternatives. Validate provider-level retention/versioning/legal-hold behavior with target credentials before claiming production archive immutability.
- `E-invoice`: structured invoice source-model JSON, minimum source-readiness validation, and a provider-neutral `IEInvoiceGenerationService` boundary now exist. Structured XML downloads, an optional disabled-by-default external command adapter, and an `IEInvoiceArtifactStorage` persistence boundary also exist, but no compliant artifact is generated until a deployment-approved generator is selected, configured, and validated; real ZUGFeRD/Factur-X generation and validation are still required before claiming full e-invoice compliance. XRechnung remains a secondary export backlog item; use `docs/e-invoice-tooling-decision.md` for the tooling decision checklist.

## Completed Internal Go-Live Baseline

- `Business onboarding`: hosted WebAdmin smoke covers creation into inactive `PendingApproval`, approval prerequisite failure, approve/suspend/reactivate lifecycle forms, invitation resend/revoke, and business-location row-version mutation. WebApi-hosted smoke covers email-confirm enforcement and public visibility against PostgreSQL.
- `Inventory/returns`: hosted operator-flow smoke coverage exercises stock reserve/release, order cancel stock release, return receipt idempotency, refund coordination without unintended stock movement, stock-transfer MarkInTransit/Complete, and purchase-order Issue/Receive through WebAdmin forms.
- `Source contracts`: WebAdmin source-contract (`257` passed, `0` skipped), Business source-contract (`87` passed, `0` skipped), all-source `SecurityAndPerformance` (`615` passed, `0` skipped), and broader Inventory/Business/Invitation/SignIn/Tax/Invoice/VAT (`981` passed, `0` skipped) are clean as of 2026-05-10.
- `Infrastructure tests`: `Darwin.Infrastructure.Tests` now passes locally with `45` passed and `0` skipped after provider-specific design-time factory and injected-clock alignment.
- `Provider smoke readiness`: guarded local smoke harnesses now exist for Stripe test-mode handoff/return-route checks, DHL live validation prerequisites, Brevo sandbox/controlled-inbox readiness, and VIES valid/invalid/provider-failure checks. Source-contract and behavior coverage verifies these scripts keep external calls opt-in, block safely when prerequisites are missing, report ready dry-run status without `-Execute`, and avoid committed secret patterns or raw provider response output.
- `VIES retry`: `RetryUnknownCustomerVatValidationBatchHandler` and the disabled-by-default `VatValidationRetryWorker` retry only provider-generated `Unknown` VAT validation decisions (`provider.unavailable`, `vies.disabled`, `vies.unavailable`) after a configured age. Operator/manual decisions are not overwritten by the retry batch.
- `Go-live readiness dry-run`: `scripts/check-go-live-readiness.ps1` aggregates the secret scan and provider smoke prerequisite checks without executing external provider calls.
- `External smoke inputs`: `docs/external-smoke-inputs.md` lists the required non-committed provider smoke inputs and execution commands for Stripe, DHL, Brevo, and VIES.

## Near-Term Tasks

- Execute the prioritized test queue in `DarwinTesting.md` section `5.5` before expanding lower-risk backlog items. Current P0 is Stripe business subscription checkout/reconciliation coverage: WebApi-hosted checkout endpoint tests, `CreateSubscriptionCheckoutIntentHandler` tests, subscription `checkout.session.completed` webhook idempotency tests, WebAdmin `Billing/Subscriptions` render/filter tests, and source-contract guards proving return routes remain webhook-only.
- Keep the source-contract lanes green after the 2026-05-10 cleanup completion: WebAdmin source-contract (`257` passed, `0` skipped), Business source-contract (`87` passed, `0` skipped), and all-source `SecurityAndPerformance` (`615` passed, `0` skipped). New source-contract changes should use stable security/localization/HTMX/route/mutation/public-boundary assertions instead of exact copy/layout assertions.
- Complete Stripe test-mode hosted checkout and webhook verification: run the webhook-forwarding preflight, pay the generated Stripe test Checkout Session, confirm `checkout.session.completed` and `payment_intent.succeeded` processing, run one business subscription checkout and verify local subscription reconciliation, then create a WebAdmin refund and verify `refund.created`/`refund.updated` reconciliation plus failed/refund/dispute visibility where applicable.
- Run DHL live smoke after target account credentials and product settings are entered; start with `scripts/smoke-dhl-live.ps1` prerequisite checks, validate the WebAdmin queued return-label flow, and only use `-Execute` for an approved real validation request.
- Keep the disabled-by-default VIES retry worker off in production until VIES live smoke, retry interval, and support ownership are approved.
- Run `scripts/smoke-vies-live.ps1` with configured valid and invalid VAT IDs before enabling VIES-dependent production review workflows.
- Run `scripts/smoke-brevo-readiness.ps1 -Execute -Sandbox`, then `scripts/smoke-brevo-readiness.ps1 -RequireDeliveryPipeline`, and finally one controlled-inbox `-Execute` pass after Brevo DNS, sender approval, webhook subscription, and worker enablement are complete.
- Run selected-provider external smoke for save/read/metadata/presigned URL/object lock or immutability-policy behavior, including profile container/prefix behavior for the invoice archive path, the `ShipmentLabels` profile for DHL labels, and the `MediaAssets` profile for CMS/media uploads. Keep the invoice archive policy wrapper selected only where retention/legal-hold requirements pass.
- Implement the selected e-invoice generator behind `IEInvoiceGenerationService` and wire it through the existing `GenerateInvoiceEInvoiceArtifactHandler`: extend the current structured invoice source-model JSON/XML mapping and readiness validation into the selected ZUGFeRD/Factur-X model, validate output, expose downloadable compliant artifacts, and add tests. Add XRechnung export later.
- Keep WebApi-hosted business onboarding smoke for email-confirm enforcement and public discovery/detail visibility green against PostgreSQL; keep the WebAdmin hosted lifecycle smoke green as onboarding evolves.
- Keep hosted inventory/returns operator-flow smoke coverage green as the return/RMA workflow evolves.
- Keep the WebAdmin admin-assisted onboarding wizard aligned with existing approval, invitation, location, loyalty, communication, and storefront-visibility handlers.
- Grow WebAdmin CI coverage thresholds only after stable green CI history.

## Later-Phase Tasks

- Carrier-integrated DHL RMA/returns automation beyond the current return-label queue path, returns queue, and shipment provider operations.
- Brevo provider-side template authoring/synchronization workflow, if operations decide to manage template bodies in Brevo instead of Darwin settings. Darwin can already map configured Brevo template IDs to Darwin template keys for send-time delivery.
- External immutable invoice archive provider hardening: MinIO/S3-compatible or Azure Blob external smoke, bucket/container Object Lock or immutability-policy validation, backup, restore, and least-privilege access review.
- Full ZUGFeRD/Factur-X plus secondary XRechnung export implementation.
- Self-service onboarding in `Darwin.Web` after WebAdmin/admin-assisted onboarding is stable.
- Additional public catalog/CMS facet/search projection improvements for scale.

## Open Decisions

- Object storage deployment profile: MinIO is the recommended self-hosted target through the S3-compatible provider; deployments may still choose AWS S3 or Azure Blob. Exact endpoint, bucket/container, Object Lock/retention/legal-hold settings, backup, and restore policy must be confirmed per deployment.
- E-invoice library/tooling for ZUGFeRD/Factur-X PDF/A-3 embedding and structured XML validation. Selection criteria are documented in `docs/e-invoice-tooling-decision.md`.
- Exact DHL account/product-code contract after target-account live smoke.
- When to rotate from Stripe test keys to live keys, and which restricted live key permissions are sufficient for Checkout Session creation and webhook reconciliation.

## Active Handoff Summary

- PostgreSQL remains the preferred/default runtime provider; SQL Server remains supported.
- WebAdmin remains MVC/Razor + HTMX.
- `Darwin.Web` remains a separate Next.js customer-facing application and must not receive operator diagnostics.
- Storefront payment completion remains verified-webhook-only.
- No provider secrets belong in git, docs, logs, or test output.
- Compliance planning decisions are summarized in `docs/compliance-decisions.md`.

