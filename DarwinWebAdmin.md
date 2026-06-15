# Darwin WebAdmin Guide

Reviewed: 2026-05-26

`Darwin.WebAdmin` is the operational back-office for Darwin. It is the primary control surface for business onboarding, support, provider readiness, billing, communication, inventory, CRM, loyalty, mobile operations, and compliance review workflows.

Historical progress notes belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). Current readiness status belongs in [docs/go-live-status.md](docs/go-live-status.md).

## Architecture

- ASP.NET Core MVC/Razor with HTMX.
- Operator-only UI. Do not move diagnostics, readiness dashboards, review queues, or back-office wording into public/member UI.
- Controllers should keep request/response orchestration thin and delegate business rules to Application handlers.
- Use localized resources for visible operator text.
- Use row-version protected posts for mutations.
- Keep anti-forgery validation on form posts.
- Keep provider credentials masked or hidden.

## Core Workspaces

| Workspace | Purpose |
| --- | --- |
| Dashboard | Compact operational command center with high-value summaries and attention items. |
| Businesses | Business profile, lifecycle, approval, suspension, reactivation, members, invitations, locations, and setup readiness. |
| Merchant readiness / onboarding | Admin-assisted setup checklist that links to existing profile, plan, users, locations, loyalty, communications, visibility, and review surfaces. |
| Billing | Payments, refunds, disputes, subscriptions, webhook deliveries, tax/compliance review, and provider reconciliation. |
| Finance | Read-only receivables, journal posting, and credit-note reconciliation reporting over the existing Billing finance foundation. |
| BusinessCommunications | Email/channel dispatch, provider callbacks, failed sends, retry decisions, and communication readiness. |
| MobileOperations | Device registration, push token state, stale/disabled notification review, and mobile support handoffs. |
| CRM | Customers, leads, opportunities, invoices, invoice archive/source-model/e-invoice artifact actions, and support workflows. |
| Sales | Commercial sales workspace for read-only order/invoice overview plus internal quote creation, quote lifecycle, and conversion linkage into existing operational order workflows. |
| Inventory | Warehouses, suppliers, purchase orders, goods receipts, stock transfers, reservations, returns, and ledger review. |
| Shipping | Shipments, returns, provider operations, label handling, tracking, and carrier exception recovery. |
| Site Settings | Secure operational configuration and secret-free provider readiness. |

## Business Onboarding

WebAdmin owns the first-launch business onboarding path:

- New businesses start inactive and pending approval.
- Operators can create, review, approve, suspend, and reactivate businesses.
- Approval/reactivation requires minimum setup prerequisites such as owner, legal/contact data, and primary location.
- Invitation preview/acceptance supports pending onboarding without allowing suspended/unavailable businesses.
- Invitation resend/revoke actions are status-aware.
- The onboarding wizard summarizes existing workspaces and does not bypass their validation.

Future self-service onboarding in `Darwin.Web` must reuse the same backend rules instead of creating a parallel policy.

## Provider Operations

WebAdmin must expose provider state without exposing provider secrets:

- Stripe: payment, subscription, refund, dispute, webhook, and reconciliation queues.
- DHL: shipment, label, return-label, tracking, failed/stale provider-operation recovery.
- Brevo: dispatch audits, provider callbacks, failed sends, retry and support handoffs.
- Object storage: configured provider/profile status and safe smoke action.
- VIES/e-invoice: review queues, validation state, and operator-visible blockers.

Provider-specific live readiness is not complete until the matching provider smoke and operational monitoring pass.

## Billing And Subscription Policy

- Storefront and business payments use provider-hosted checkout.
- Browser return routes must not finalize payment or subscription state.
- Verified provider webhooks are authoritative.
- Business mobile subscription management is read-only for first launch; plan purchase, cancellation, SEPA mandate setup, and manual payment registration are web/back-office workflows.
- WebAdmin must show provider references, status, failures, and reconciliation state without exposing secret keys.

## Sales Workspace

- Sales is a commercial workspace over the current `Order` and `Invoice` foundation plus internal `SalesQuote` and `DeliveryNote` documents.
- Orders remains the operational fulfillment, payment, shipment, refund, and invoice-creation workspace.
- CRM invoice screens remain the operational invoice editing, archive, structured export, and artifact workspace.
- Sales must not introduce parallel order or invoice mutation paths; it links to existing operational flows.
- Sales quote mutations are allowed only for quote drafts and quote lifecycle: create draft, update draft, send, accept, reject, expire, create an order from an accepted quote, and link an accepted quote to an existing order.
- Sales quote order creation uses the current `Order` and `OrderLine` model with quote-time snapshots. It does not create a parallel Sales order model.
- Non-catalog quote lines can convert to order lines without fake products; these order lines have no product variant and are not treated as warehouse stock movement lines.
- Delivery notes are formal internal/WebAdmin documents created from existing shipments. Their line quantities come from `ShipmentLine.Quantity`; the UI must not accept manual delivery-note quantities.
- Delivery note lifecycle mutations are limited to create from shipment, prepare, issue, mark shipped, mark delivered, and cancel. They must not create parallel shipment, payment, refund, invoice, or inventory mutation paths.
- Delivery note workflow rules belong in Application policy/services so customer-specific variation can be introduced without forking controllers or views.
- Return orders are formal internal/WebAdmin Sales documents for request, approval, return-shipment linkage, receipt, inspection, refund readiness, refund linkage, and closure.
- Return order refund and restock eligibility is available only after receipt and inspection. Request, approval, or return-label creation alone must not trigger refund or stock movement.
- Return order restock uses the existing inventory return receipt owner for variant-backed accepted quantities. Non-catalog lines can be returned as commercial evidence but do not create stock movement.
- Return order refund settlement stays in the existing refund/payment flows; Sales only links completed refund records back to the return.
- Return order refund reconciliation shows linked and remaining amounts. Partial completed refund linkage keeps the return refund-ready; exact eligibility coverage marks it refunded; over-linking, wrong-currency, wrong-order, pending, failed, or deleted refunds are rejected.
- Return order restock retry is guarded by the return id reference so repeated inspection attempts do not create duplicate stock movement.
- Return orders must not create credit notes, negative invoices, parallel refund ledgers, or parallel shipment/payment/invoice mutations.
- Credit notes are formal internal/WebAdmin Sales/Finance documents. They are not negative invoices, refund actions, return-order printouts, or UI-only documents. Credit-note issue reserves legal numbering, validates cumulative credit, captures immutable source/archive evidence, posts receivable/revenue/tax reversal, and records lifecycle evidence.
- Credit-note reconciliation is hardened in WebAdmin: duplicate line input is aggregated, refund evidence must reconcile to the linked invoice through payment/order/currency boundaries, line prefill uses remaining creditable issued-invoice quantities, and source-model download is internal-only for issued/voided documents with stored source hashes.
- Finance posting evolves the existing Billing journal entry foundation. Manual journal entries remain Billing operations; automated posting has source linkage, idempotency, posting lifecycle/status, safe internal posting service support, business-scoped account-role mappings, read-only receivables projection, and invoice/payment/refund/cancellation/credit-note posting wiring. WebAdmin finance configuration/reporting UI is not exposed until it can include authorization, validation, and operator UX as a complete slice.
- Finance reporting is exposed as a separate workspace with overview, receivables, postings, account-mapping readiness pages, export workflow, and supplier invoice core operations. Reporting remains projection-oriented; account mapping and supplier invoice lifecycle are the only Finance-owned mutations in this workspace. Billing remains the owner for financial account creation/editing and manual journal entry mutations.
- Finance export and accounting integration packages posted journal entries from the existing Billing finance foundation. Finance Exports is an internal/operator-only workflow with batch identity, safe retry evidence, durable object-storage package files, and `DocumentRecord` metadata; it does not export mutable operational documents directly or create public/mobile/storefront contracts.
- Finance export generation stores the canonical package before marking a batch generated. Operators can download stored packages from Finance, but connector push, journal editing, invoice/payment/refund/credit-note mutations, and operational document export shortcuts do not belong in this workspace.
- Finance export connector push stays behind the provider-neutral adapter foundation. The internal push action reads the stored package, records target-side ids through `ExternalReference`, stores only safe retry evidence, and remains outbound-only until sync/conflict handling is designed. Production WebAdmin keeps push blocked unless a real accounting connector adapter is registered; no no-network adapter may mark a batch delivered at runtime.
- Finance export file-delivery is the first implemented live connector target. It copies only the stored canonical export package to the configured `FinanceExportOutbound` destination and reports success only after destination write and package-hash verification. Production WebAdmin enables push only when that outbound profile is configured with a non-database object-storage provider. It does not add connector credential forms, package regeneration, journal editing, invoice/payment/refund/credit-note mutations, public routes, or mobile/storefront contracts.
- Finance export deployment readiness requires both `FinanceExports` and `FinanceExportOutbound` object-storage profiles when export push is in scope. `FinanceExports` is the source package profile; `FinanceExportOutbound` is the delivery destination. Missing or database-backed outbound configuration keeps push blocked rather than creating fake delivered batches.
- Sales lifecycle evidence comes from `BusinessEvent` and `AuditTrail` recorded by the existing order, payment, shipment, refund, and invoice command handlers.
- Sales events are for traceability, reporting, and automation context; they are not a second source of operational state.
- Sales must not expose provider secrets, raw payment payloads, archive object credentials, or mobile-only contracts.

## Inventory And Purchasing Workspace

- Inventory remains the internal/WebAdmin owner for warehouses, stock levels, stock transfers, suppliers, purchase orders, goods receipts, and inventory ledger review.
- Supplier master data is business-scoped and uses the current `Supplier` model with code, status, preferred currency, payment terms, lead time, tax reference, website, and operational notes. WebAdmin must not introduce a parallel supplier model.
- Purchase orders use the current `PurchaseOrder` and `PurchaseOrderLine` models. Manual purchase order numbers are allowed, but empty purchase order numbers must reserve `NumberSequenceDocumentType.PurchaseOrder`; fake fallback numbers are not acceptable.
- Purchase order lifecycle mutations are limited to the current owner flow: create/update draft, issue, receive, and cancel. Row-version and anti-forgery protections must remain on mutations.
- Goods receipts are formal internal/WebAdmin documents for receiving, inspection, and inventory posting. Receipt numbers are reserved on receive, not while the receipt is only draft.
- Goods receipt line quantities are controlled by lifecycle stage: received quantity in receive, accepted/rejected/damaged quantity in inspection, and accepted quantity only in post-to-inventory.
- Goods receipt posting delegates stock changes to existing stock-level and `InventoryTransaction` owners with `GoodsReceiptPosted` and `ReferenceId = GoodsReceiptId`; it must not create a parallel stock ledger.
- Legacy purchase order receive behavior stays compatible by creating a formal posted goods receipt behind the existing purchase order action.
- Supplier invoice/payables boundary design is complete. WebAdmin must add supplier invoice only through the future formal `SupplierInvoice` core workflow and must not reuse customer-facing `Invoice`, create negative receivables, or add document-only payable shortcuts.
- Supplier invoice core/admin/posting is implemented in the Finance workspace. Operators can create, update, match, approve, post, and void eligible formal supplier invoices linked to suppliers, purchase orders, and goods receipts.
- Supplier payable liability comes only from the supplier invoice posting command and existing finance posting services, not from WebAdmin status text, supplier invoice attachment upload, approval state alone, or manual journal shortcuts.
- Posted supplier invoices link back to read-only Finance posting review. They must not create supplier payment, customer payment, refund, customer invoice, archive/download, or manual journal edit shortcuts.
- Supplier payment settlement remains a future flow; WebAdmin purchasing pages must not create supplier payment, refund, customer invoice, or archive/download mutations as shortcuts.
- Public WebApi, mobile/member, storefront checkout, issued invoice archive/download, finance export, payment, refund, and credit-note flows remain outside this purchasing hardening surface.

## Security And UX Rules

- Never display provider secrets, access keys, webhook secrets, connection strings, SAS tokens, or private credentials.
- Mask secret fields and preserve existing values when posts contain blank or placeholder values.
- Use anti-forgery tokens and row-version checks for mutations.
- Keep CSP, secure cookies, and self-hosted asset assumptions intact.
- Prefer compact operator dashboards over long explanatory pages.
- Keep diagnostics in module workspaces; the dashboard should summarize and link.

## Testing Expectations

Use hosted WebAdmin tests for:

- Authentication/authorization and anti-forgery.
- Render and HTMX fragment stability.
- Row-version protected mutation flows.
- Business onboarding and invitation lifecycle.
- Inventory/returns operator flows.
- Billing/payment/refund/dispute/provider callback surfaces.
- Mobile support surfaces.
- E-invoice artifact download safety.

See [DarwinTesting.md](DarwinTesting.md) for active commands and coverage priorities.
