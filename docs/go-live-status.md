# Darwin Go-Live Status

Last reviewed: 2026-05-08

This status is code-backed. It intentionally distinguishes implemented plumbing from production-complete provider behavior.

## WebAdmin Dashboard

Status: compact operational command center implemented.

- `HomeController` now avoids catalog, CMS, and identity list queries on the first dashboard paint.
- First paint keeps high-value summaries: CRM, orders, business support, communication readiness, mobile device health, and selected-business billing/inventory/loyalty summaries.
- `/Home/Index` renders KPI cards, a prioritized attention list capped at eight items, selected-business context, compact module summaries, and a short quick-link strip.
- Detailed diagnostics remain in their module workspaces: BusinessCommunications, Businesses support/readiness, Billing/TaxCompliance, MobileOperations, Orders, Inventory, and Loyalty.
- Existing fragment endpoints remain available: `Home/CommunicationOpsFragment` and `Home/BusinessSupportQueueFragment`.

## Stripe / Payments

Status: webhook boundary and internal payment reconciliation are implemented; live Stripe Checkout/PaymentIntent creation is not production-complete.

Verified implementation:

- `StripeWebhooksController` validates `Stripe-Signature`, enforces bounded payload reads, extracts Stripe event id/type, and writes idempotent inbox entries.
- `ProcessStripeWebhookHandler` handles checkout session, payment intent, refund, invoice, and subscription events, logs Stripe event ids in `EventLog`, and updates matched payments/orders/invoices when provider references match.
- WebAdmin billing views expose payments, refunds, disputes, webhook deliveries, failed/pending/unlinked queues, and dispute review actions.

Gaps:

- `CreateStorefrontPaymentIntentHandler` currently creates local Stripe-like references (`pi_*`, `cs_*`) instead of calling Stripe to create a real PaymentIntent or Checkout Session.
- Storefront payment return handling can mark local payment state, but a production Stripe flow should rely on verified provider webhooks as the final source of truth.
- Keep Stripe secrets only in configuration/environment; no committed secrets were found by `scripts/check-secrets.ps1` in the latest pass.

## DHL / Shipping

Status: provider operation queue, callbacks, labels/tracking presentation, and WebAdmin operations exist; actual DHL API calls are still phase-one simulated/provider-reference generation.

Verified implementation:

- `DhlWebhooksController` validates API key and HMAC signature, bounds payload size, and writes idempotent provider callback inbox entries.
- `ApplyDhlShipmentCreateOperationHandler` creates provider shipment references, tracking numbers, and carrier events, then queues label generation.
- `GenerateDhlShipmentLabelHandler` safely queues or retries label operations.
- WebAdmin has shipment queues, returns queue, and shipment provider operations visibility.

Gaps:

- Shipment creation and label generation do not yet call the real DHL API.
- Label storage/download is represented through `LabelUrl`, but live carrier label retrieval and storage need production integration.
- Returns/RMA flows are visible, but full carrier-integrated RMA automation remains a go-live task.

## Brevo / Communication

Status: production-capable provider plumbing is present when configured correctly.

Verified implementation:

- `BrevoEmailOptionsValidator` fails startup when `Email:Provider=Brevo` is active without required API key/sender/timeout configuration.
- `BrevoEmailSender` sends transactional email through Brevo, records audit state, supports sandbox mode, correlation idempotency, and sanitized logging.
- `BrevoWebhooksController` requires Basic Auth, bounds payloads, normalizes provider events, and writes idempotent callback inbox entries.
- BusinessCommunications remains the detailed workspace for retry chains, failed sends, provider review, and callback state.

Gaps:

- Template lifecycle is still application-rendered rather than fully provider-managed in Brevo.
- Production setup must verify sender domain, DKIM/DMARC, webhook credentials, worker deployment, and sandbox disabled before live traffic.

## Tax / VAT / E-Invoice

Status: visibility and operator review exist; full e-invoicing/archive compliance is not implemented.

Verified implementation:

- WebAdmin `Billing/TaxCompliance` summarizes invoice/customer/business tax signals and links operators to relevant billing/CRM workspaces.
- Invoice and order entities carry tax snapshots and VAT-related fields sufficient for current review workflows.

Gaps:

- TaxCompliance is mainly a visibility page; it does not provide a complete action workflow for all tax corrections.
- VAT ID validation, reverse-charge decisioning, immutable issued-invoice snapshots, invoice document/export, archival policy, and e-invoice generation require a dedicated compliance implementation slice.

## Testing / CI

Status: WebAdmin test project exists and is now wired into CI.

- `.github/workflows/tests-quality-gates.yml` now restores and runs `tests/Darwin.WebAdmin.Tests/Darwin.WebAdmin.Tests.csproj` with coverage artifacts.
- `scripts/ci/verify_coverage.py` now accepts `--webadmin-threshold`.
- Initial WebAdmin coverage threshold is intentionally low (`10`) so the lane can run continuously while coverage grows.
