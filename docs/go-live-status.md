# Darwin Go-Live Status

Last reviewed: 2026-05-09

This status is code-backed. It intentionally distinguishes implemented plumbing from production-complete provider behavior.

## WebAdmin Dashboard

Status: compact operational command center implemented.

- `HomeController` now avoids catalog, CMS, and identity list queries on the first dashboard paint.
- First paint keeps high-value summaries: CRM, orders, business support, communication readiness, mobile device health, and selected-business billing/inventory/loyalty summaries.
- `/Home/Index` renders KPI cards, a prioritized attention list capped at eight items, selected-business context, compact module summaries, and a short quick-link strip.
- Detailed diagnostics remain in their module workspaces: BusinessCommunications, Businesses support/readiness, Billing/TaxCompliance, MobileOperations, Orders, Inventory, and Loyalty.
- Existing fragment endpoints remain available: `Home/CommunicationOpsFragment` and `Home/BusinessSupportQueueFragment`.

## Stripe / Payments

Status: webhook boundary, internal reconciliation, and real Stripe Checkout Session creation path are implemented; test-mode validation is next and live-provider validation remains pending.

Verified implementation:

- `StripeWebhooksController` validates `Stripe-Signature`, enforces bounded payload reads, extracts Stripe event id/type, and writes idempotent inbox entries.
- `ProcessStripeWebhookHandler` handles checkout session, payment intent, refund, invoice, and subscription events, logs Stripe event ids in `EventLog`, and updates matched payments/orders/invoices when provider references match.
- `CreateStorefrontPaymentIntentHandler` can now create a real Stripe Checkout Session through the WebApi `StripeCheckoutSessionClient` when `SiteSetting.StripeEnabled=true`, `StripeSecretKey` is configured, and return/cancel URLs are provided.
- Storefront return handling no longer captures or voids Stripe payments; verified Stripe webhooks remain the source of truth for captured/failed/cancelled provider state.
- WebAdmin billing views expose payments, refunds, disputes, webhook deliveries, failed/pending/unlinked queues, and dispute review actions.

Gaps:

- The real Checkout Session path still needs Stripe test-mode smoke with the configured test keys, followed by a later live-mode smoke before production traffic.
- Subscription checkout creation and refund/dispute operator actions still need a separate live-provider verification pass.
- Keep Stripe secrets only in configuration/environment; no committed secrets were found by `scripts/check-secrets.ps1` in the latest pass.

See `docs/module-audit.md` for the cross-module audit matrix.

## DHL / Shipping

Status: provider operation queue, callbacks, labels/tracking presentation, shared label storage, and a real DHL HTTP client path are wired; live-provider validation remains pending.

Verified implementation:

- `DhlWebhooksController` validates API key and HMAC signature, bounds payload size, and writes idempotent provider callback inbox entries.
- `ApplyDhlShipmentCreateOperationHandler` calls `IDhlShipmentProviderClient`, stores the DHL shipment reference/tracking number returned by the provider, persists returned PDF labels through shared media storage, and queues label generation only when the create response does not include a label.
- `ApplyDhlShipmentLabelOperationHandler` now requires an existing provider shipment reference and retrieves/saves the carrier label instead of creating local fake references or label URLs.
- `GenerateDhlShipmentLabelHandler` safely queues or retries label operations.
- WebAdmin has shipment queues, returns queue, and shipment provider operations visibility.

Gaps:

- The DHL client still needs a live smoke against the target DHL account to confirm the exact base URL, authentication headers, shipment payload, label response shape, and callback lifecycle.
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

Status: operator review exists, VAT/collection/reverse-charge follow-up is data-backed, missing customer VAT IDs can be corrected from the TaxCompliance workspace, VAT IDs can be checked through a VIES-backed provider path, final invoice-level reverse-charge decisions can be recorded with row-version concurrency checks, invoice review data can be exported as CSV for archive/accounting review, draft invoices now have a minimum issue-readiness guard, and CRM invoices capture an immutable issue snapshot with retention metadata when first opened/paid; downloadable JSON and printable HTML archive artifacts are available from the CRM invoice editor for issued invoices. Expired invoice archive payloads can now be purged by the Worker with audit metadata retained on the invoice. Full e-invoicing compliance is not implemented. The primary planned downloadable e-invoice target is ZUGFeRD/Factur-X; XRechnung is a secondary export backlog item.

Verified implementation:

- WebAdmin `Billing/TaxCompliance` summarizes invoice/customer/business tax signals, links operators to relevant billing/CRM workspaces, and lets operators correct missing B2B customer company/VAT profile data with row-version concurrency checks.
- WebAdmin `Billing/TaxCompliance` now also surfaces B2B customers whose VAT ID exists but still needs validation review. Operators can mark the VAT ID as valid, invalid, or not applicable; the decision is stored on the customer with source, optional note, timestamp, and row-version concurrency protection.
- `ValidateCustomerVatIdHandler` can validate B2B customer VAT IDs through `IVatValidationProvider`. The infrastructure implementation calls the EU VIES SOAP endpoint when `Compliance:VatValidation:Vies:Enabled=true`; disabled or unavailable providers leave the customer in `Unknown` with an operator-visible source/message instead of creating a false valid/invalid decision.
- WebAdmin `Billing/TaxCompliance` now exposes a CSV invoice export with invoice/customer/order/payment/status/tax totals and VAT/reverse-charge follow-up flags. The export is UTF-8 with formula-injection protection for spreadsheet safety.
- Invoice review rows now receive explicit `RequiresVatId`, `IsReverseChargeCandidate`, `IsDueSoon`, and `IsOverdue` flags from `GetTaxComplianceOverviewHandler` instead of deriving collection or compliance follow-up from display status text.
- Reverse-charge review candidates are surfaced when VAT and reverse-charge settings are enabled, the customer is B2B, a VAT ID exists, and the CRM default billing country differs from the invoice issuer country.
- TaxCompliance operators can now mark an invoice-level reverse-charge decision as applies or not applicable. The decision is persisted on the invoice with `ReverseChargeApplied` and `ReverseChargeReviewedAtUtc`, guarded by the invoice row version, and the candidate count drops once reviewed.
- Draft invoices cannot be posted/opened or marked paid until issuer legal name, tax id, address, postal code, city, and country are configured. When VAT is enabled, business-customer invoices also require a customer VAT ID before issue.
- CRM invoices now persist `IssuedAtUtc` plus `IssuedSnapshotJson` the first time a draft invoice is opened or marked paid. The snapshot freezes issuer, customer, business, totals, due/payment timestamps, and line data for later archive/export work and is not rewritten by later status transitions.
- Issued invoice archive metadata now includes `IssuedSnapshotHashSha256`, `ArchiveGeneratedAtUtc`, `ArchiveRetainUntilUtc`, and `ArchiveRetentionPolicyVersion`. The retention horizon is controlled by the site-level `InvoiceArchiveRetentionYears` setting and is written once when the invoice is issued.
- WebAdmin CRM invoice editors expose downloadable JSON and printable HTML archive artifacts for invoices that already have an issued snapshot. Draft invoices and legacy issued invoices without a snapshot do not return a generated archive artifact silently.
- `PurgeExpiredInvoiceArchivesHandler` and `InvoiceArchiveMaintenanceBackgroundService` clear expired issued-snapshot payloads after `ArchiveRetainUntilUtc`, record `ArchivePurgedAtUtc` and `ArchivePurgeReason`, and write an `InvoiceArchivePurged` event log entry. The worker is explicit opt-in through `InvoiceArchiveMaintenanceWorker:Enabled`.
- TaxCompliance playbooks are default-collapsed so the workspace keeps operational queues first and long guidance available on demand.
- Invoice and order entities carry tax snapshots and VAT-related fields sufficient for current review workflows.

Gaps:

- TaxCompliance still lacks live VIES smoke in the target deployment, external immutable archive storage beyond the current `IInvoiceArchiveStorage` internal/database provider, final invoice document rendering beyond the current printable archive HTML, and e-invoice generation.
- VIES production smoke, async retry for Unknown/manual-review results, external object storage with immutable retention/legal hold, and ZUGFeRD/Factur-X generation/validation require dedicated implementation slices. Current JSON/HTML/CSV archive/export is useful operational output, not full e-invoice compliance.

## Business Onboarding / Public Visibility

Status: onboarding state, support queues, and lifecycle actions exist; front-office visibility is now guarded by both approval and active state.

Verified implementation:

- New businesses are created as inactive and `PendingApproval`.
- Admin lifecycle handlers approve, suspend, and reactivate businesses with row-version checks.
- Approval and reactivation now require the minimum go-live prerequisites: active owner, primary location, contact email, and legal name.
- Invitation preview and acceptance allow pending-approval onboarding businesses even while inactive, but continue to reject suspended or unavailable businesses.
- Invitation resend/revoke operations are scoped to actionable invitation states: accepted and revoked invitations cannot be reissued, accepted invitations cannot be revoked, and already revoked invitations remain closed.
- Both API password login and WebAdmin sign-in now require confirmed email addresses before issuing tokens or admin cookies.
- Public discovery, map discovery, public detail, and member engagement queries now require `OperationalStatus=Approved` and `IsActive=true`.

Gaps:

- Business onboarding still needs hosted smoke coverage for the full admin-assisted lifecycle: creation, invitation onboarding, resend/revoke, email-confirm enforcement, approval prerequisites, reactivation, and public visibility.

## Inventory / Returns

Status: inventory handlers cover manual adjustment, reservation/release, return receipt, and operator workspaces; core stock movement handlers are now safer for retried workflows.

Verified implementation:

- Manual adjustments validate variant/warehouse stock and reject negative adjustments that exceed available quantity.
- Reservation moves stock from available to reserved and release moves reserved stock back to available.
- Return receipt increases available stock and is idempotent when a return/reference id is supplied.
- Reservation and release are now also idempotent when a `ReferenceId` is supplied, preventing retried order/cart workflows from double-moving stock.

Gaps:

- Inventory/returns still need hosted operator-flow smoke coverage for supplier receiving, stock transfer, order cancel stock release, return receipt idempotency, and refund coordination.
- Carrier-integrated RMA automation remains under the DHL/shipping go-live slice.

## Testing / CI

Status: WebAdmin test project exists and is wired into split CI lanes; provider-focused WebApi tests and the split WebAdmin smoke subsets are green locally.

- `.github/workflows/tests-quality-gates.yml` now restores and runs `tests/Darwin.WebAdmin.Tests/Darwin.WebAdmin.Tests.csproj` as separate security, public/auth smoke, render smoke, tokenless CSRF, valid-token CSRF, and positive mutation coverage lanes.
- `scripts/ci/verify_coverage.py` now accepts `--webadmin-threshold`.
- Initial WebAdmin coverage threshold is intentionally low (`10`) so the lane can run continuously while coverage grows.
- The focused WebApi provider boundary run for Stripe, DHL, Brevo, and provider-callback worker tests passed locally on 2026-05-09 with an isolated output path to avoid a running `Darwin.WebApi.exe` file lock (`280` passed).
- `Darwin.WebAdmin.Tests` builds locally with an isolated output path. The non-hosted security subset, public/auth hosted smoke subset (`27` passed), render hosted smoke subset (`105` passed), tokenless CSRF matrix (`115` passed), valid-token CSRF matrix (`8` passed), and positive mutation smoke flow (`1` passed) passed locally on 2026-05-09 after aligning row-version protected smoke posts with the real Razor forms.
- The focused unit/source-contract run for Shipment/Stripe/Billing/Communication/Inventory/Tax/SignIn passed locally on 2026-05-09 (`602` passed). The broader Inventory/Business/Invitation/SignIn/Tax/Invoice/VAT filter passed on 2026-05-10 with `1007` active tests and `14` quarantined stale source-contract tests.
- WebAdmin CSP was tightened back to self-hosted script/style/font/connect sources, and admin/auth/anti-forgery cookies now keep secure defaults instead of relying on environment-specific relaxation.
- The broad WebAdmin source-contract suite now runs green locally with stale pre-simplification exact-layout contracts explicitly quarantined (`210` active passed, `47` WebAdmin-surface skipped on 2026-05-09). Source-contract cleanup is partial until those skipped contracts are rewritten into stable security/localization/HTMX/route assertions.


## Compliance Decisions

- VIES phase 1 policy is soft `Unknown` plus manual review for disabled, unavailable, rate-limited, timeout, malformed response, or provider-exception outcomes. Only clear valid/invalid provider responses may record `Valid` or `Invalid`.
- Invoice archive phase 1 uses the internal/database `IInvoiceArchiveStorage` provider. Production requires an external object-storage provider with immutable retention/legal hold before legal archive immutability is claimed.
- E-invoice phase 1 is planning only. The primary target is ZUGFeRD/Factur-X because SME users need a human-readable PDF plus structured data. XRechnung remains a secondary export backlog item. Current JSON/HTML/CSV exports are not full e-invoice compliance.
- WebAdmin onboarding wizard is planned as an admin-assisted Loyan business onboarding workflow; it is not implemented in this slice.
