# Sales Document Lifecycle Design

This document locks the next Sales document lifecycle decisions before any schema, handler, route, DTO, WebAdmin, storefront, mobile, checkout, archive, or production-flow implementation. V1 visibility is internal/WebAdmin only. Member, mobile, and storefront contracts are not expanded by this design.

Darwin continues to evolve the current `Order`, `OrderLine`, `Invoice`, `InvoiceLine`, payment, shipment, refund, and invoice archive foundation. It must not introduce parallel `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` models for the same business surface.

## Lifecycle Decision Matrix

| Document | Lifecycle states | Owner model decision | Relations | Numbering | Documents and evidence | Inventory/refund/payment/tax/finance/archive impact | V1 visibility | Implementation decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `SalesQuote` | `Draft`, `Sent`, `Accepted`, `Rejected`, `Expired`, `Converted` | Additive Sales document implemented in the Sales schema. It is not `Opportunity` and does not replace CRM pipeline tracking. | May originate from `Opportunity`; accepted quotes can create the current `Order` from quote snapshots or link to an existing valid `Order`. It does not link directly to `Invoice`, `Shipment`, `Refund`, or `Payment` in v1. | Uses `NumberSequenceDocumentType.SalesQuote` when first sent. Drafts have no official quote number. Created orders use the existing order numbering policy through the shared order creation service. | Lifecycle and quote-to-order evidence use deterministic `BusinessEvent/AuditTrail`. Future quote PDFs and attachments use `DocumentRecord`; import/export identity uses `ExternalReference`. | Quote acceptance/conversion does not reserve inventory, capture payment, create invoices, create shipments, create refunds, alter invoice archive behavior, or post accounting. Tax/pricing are quote snapshots and become order snapshots during conversion. | Internal/WebAdmin only. | Implemented with quote core model and quote-to-current-order conversion. |
| `DeliveryNote` | `Draft`, `Prepared`, `Issued`, `Shipped`, `Delivered`, `Cancelled` | Additive Sales document implemented in the Sales schema over current `Shipment` and shipped quantities. It is not a PDF-only record detached from shipment work. | Links to current `Order`, `Shipment`, and shipped `OrderLine` quantities. It does not replace shipment provider operations, labels, or future inventory movement records. | Uses `NumberSequenceDocumentType.DeliveryNote` when issued. Draft/prepared notes have no official number. Existing shipment provider references remain provider records, not document numbers. | Lifecycle evidence uses deterministic `BusinessEvent/AuditTrail`. Future generated document metadata can use `DocumentRecord`; external delivery ids use `ExternalReference`. | Delivery note quantity comes from `ShipmentLine.Quantity`. It does not create stock changes independently from shipment/inventory handlers. No invoice archive behavior changes. | Internal/WebAdmin only. | Implemented with formal DeliveryNote core model and WebAdmin lifecycle. |
| `ReturnOrder` | `Requested`, `Approved`, `Rejected`, `ReturnShipmentQueued`, `Received`, `Inspected`, `RefundReady`, `Refunded`, `Closed`, `Cancelled` | Implemented as an additive Sales return document. It represents customer return intent, return shipment linkage, received quantity, inspection result, refund eligibility, refund linkage, and closure; it is not only a refund wrapper. | Links to current `Order`, original `OrderLine`, optional `Shipment`, optional `Invoice`, and resulting completed `Refund` records. | Uses `NumberSequenceDocumentType.ReturnOrder` at approval. Request drafts can remain internal without an official number. | Return documents and customer attachments use `DocumentRecord`; lifecycle and inspection evidence use `BusinessEvent/AuditTrail`; external RMA/import ids use `ExternalReference`. | Refund and restock are allowed only after received goods are inspected. Current `Refund` remains the authoritative payment settlement record; inventory handlers remain stock movement owners; credit note stays finance-gated. | Internal/WebAdmin only. | Core model, admin workspace, and refund/inventory reconciliation hardening are implemented. |
| `CreditNote` | `Draft`, `Issued`, `Voided` or `Cancelled` according to future core policy | Finance-gated formal document. It must not be modeled as a negative invoice and must not be created as a non-posting cosmetic document. | Must link to current issued `Invoice`, related `Refund`/settlement when relevant, optional `ReturnOrder`, and finance posting records. | Drafts do not consume official numbers. Issue reserves `NumberSequenceDocumentType.CreditNote` exactly once. | Issued credit-note source/archive metadata follows the invoice archive/source-model discipline; supporting evidence can use `DocumentRecord`; lifecycle evidence uses `BusinessEvent/AuditTrail`; external ids use `ExternalReference`. | Requires receivable/revenue/tax reversal posting, original invoice source evidence, refund settlement alignment, immutable archive/source-model behavior, and internal-only v1 visibility. | Internal/WebAdmin only in v1. | Source/archive policy is documented in `credit-note-source-model-archive-design.md`; next implementation is core model/admin with source/archive fields included from the start. |

## Locked Design Decisions

- `SalesQuote` is a formal sales document and remains separate from `Opportunity`. An opportunity can create a quote, and an accepted quote can create the current `Order` from snapshots or link to an existing `Order`.
- `DeliveryNote` is a fulfillment document tied to shipment and warehouse quantities. A standalone generated file without shipment/warehouse authority is not acceptable.
- `ReturnOrder` requires return receipt, inspection, stock consequence, refund/payment, and tax/finance boundaries before implementation. The boundary is now locked: refund and restock are not allowed before inspection.
- `CreditNote` is finance-gated. Darwin must not fake credit notes as negative invoices or issue non-posting credit-note documents.
- Existing `Order` and `Invoice` remain the Sales order and issued invoice foundation. No parallel `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` entity/table is introduced.
- `NumberSequence`, `DocumentRecord`, `BusinessEvent/AuditTrail`, and `ExternalReference` are the foundation primitives for all future Sales documents.
- V1 visibility is internal/WebAdmin only. Mobile, member, storefront, public WebApi, checkout, payment-intent, invoice archive/download, and structured JSON/XML contracts remain unchanged.
- Issued order, invoice, delivery, return, and future credit documents must preserve immutable snapshots. Historical records must not be recomputed from live catalog, address, pricing, tax, warehouse, payment, or shipment settings.

## Implementation Order Decision

| Candidate slice | Decision | Reason |
| --- | --- | --- |
| `SalesQuote Core Model Slice` | Implemented. | It adds commercial value without forcing inventory reversal, refund settlement, or finance posting rules. It reuses CRM opportunity context while staying separate from CRM pipeline entities. |
| `DeliveryNote Core Model Slice` | Implemented. | It uses `ShipmentLine.Quantity` as the v1 quantity authority and creates a formal internal/WebAdmin delivery document without duplicating shipment, payment, refund, invoice, or inventory mutations. |
| `ReturnOrder Boundary Design And Implementation Plan` | Implemented as design outcome. | It locks quantity ownership, refund eligibility, restock timing, shipment/label linkage, Finance-gated credit-note boundary, and compatibility rules before schema/UI. |
| `ReturnOrder Core Model And Admin Slice` | Implemented. | It enforces the locked after-inspection policy, links completed refunds instead of replacing refund settlement, and uses existing inventory return receipt handling for variant-backed restock. |
| `ReturnOrder Refund And Inventory Reconciliation Hardening` | Implemented. | It closes refund over-linking, refund status/currency/order validation, partial-refund state handling, duplicate refund idempotency, restock retry idempotency, and non-catalog no-restock guards. |
| `CreditNote Finance/Accounting Boundary Design Slice` | Implemented as design outcome. | It locks that credit notes cannot be negative invoices, UI-only records, or non-posting documents, and that posting, tax reversal, legal numbering, archive/source-model, and member visibility policy are required first. |
| `Finance Posting Foundation Design Slice` | Implemented as design outcome. | It confirms Darwin should evolve existing `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` instead of creating a parallel ledger, and locks source linkage, idempotency, posting lifecycle, account mapping, reversal, receivable, and tax prerequisites. |
| `Finance Posting Foundation Implementation Slice` | Implemented. | It adds source linkage, posting lifecycle/status, deterministic posting keys, safe metadata, migrations, and an internal idempotent `FinancePostingService` over the existing Billing journal foundation. |
| `CreditNote Source Model And Archive Design Slice` | Implemented as design outcome. | It locks immutable issued source JSON, archive ownership, original invoice source derivation, cumulative credit validation, tax reversal basis, member visibility, and v1 internal/WebAdmin scope. |
| `CreditNote Core Model And Admin Slice` | Implemented as internal/WebAdmin outcome. | Darwin now has a formal CreditNote model with source/archive fields, posting, tax reversal, legal numbering, cumulative validation, lifecycle evidence, and WebAdmin list/detail/lifecycle actions. |
| `CreditNote Reconciliation And Source Export Hardening` | Implemented. | It hardens duplicate-line input, refund/payment/order/currency reconciliation, remaining creditable quantity suggestions, and internal source-model export without changing public/mobile/member/storefront contracts. |

## Compatibility Rules For Future Slices

- Future Sales document implementation must be additive unless a separate compatibility migration is explicitly designed.
- Existing member/mobile commerce contracts are guarded compatibility surfaces and must not be expanded accidentally.
- Storefront checkout and payment finalization remain current Web/storefront flows until a deliberate storefront Sales document exposure slice is approved.
- Invoice archive, structured source-model, XML/JSON export, purge, and download behavior remain authoritative for issued invoices and must not be replaced by generic document records.
- Shipment provider operations and labels remain authoritative provider artifacts and must not be replaced by delivery note metadata.
- Payment and refund records remain authoritative settlement records; Sales lifecycle documents can reference them but must not duplicate their mutation logic.

## SalesQuote Core Model Outcome

Darwin now has an internal/WebAdmin-only `SalesQuote` implementation.

- `SalesQuote` and `SalesQuoteLine` are additive Sales schema entities; no `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` entity/table was introduced.
- Draft quotes are editable and do not receive an official number.
- Sending a draft quote reserves `NumberSequenceDocumentType.SalesQuote` exactly once and moves the quote to `Sent`.
- Sent quotes can be accepted, rejected, or expired. Accepted quotes can be marked `Converted` only by linking an existing valid `Order`.
- Quote lifecycle evidence is written through deterministic `BusinessEvent` and `AuditTrail` records with safe/reportable payloads only.
- WebAdmin Sales has Quotes navigation, list/filtering, create/edit/detail, and lifecycle actions. These actions are quote-only and do not create parallel order, invoice, payment, shipment, or refund mutations.
- Member, mobile, storefront, public WebApi, checkout, payment-intent, invoice archive/download, and structured source-model contracts remain unchanged.

## SalesQuote Order Conversion Outcome

Darwin now creates current `Order` records from accepted `SalesQuote` snapshots in WebAdmin/internal flows.

- `Create order from quote` is separate from `Link existing converted order`, so operators can choose between creating a new operational order and linking a pre-existing one.
- Conversion is allowed only for accepted quotes with a valid row version. Draft, sent, rejected, expired, and already converted quotes are rejected.
- Conversion creates the existing `Order` entity with `SalesChannel.Admin`, current `OrderedAtUtc`, quote business/customer/currency, quote address snapshots, quote totals, and quote line snapshots.
- `OrderLine.VariantId` is nullable so non-catalog/service/custom quote lines can become legal order snapshot lines without fake products.
- Catalog quote lines preserve `ProductVariantId`; non-catalog quote lines keep name/SKU/quantity/price/tax snapshots and do not trigger catalog recomputation.
- Inventory reservation/release/allocation logic ignores non-catalog order lines because no product variant exists to move in stock.
- Created orders use `NumberSequenceDocumentType.Order` when an active sequence exists. Existing fallback behavior remains compatible for direct admin order creation and storefront checkout.
- Conversion records deterministic `sales.quote.order_created` and `sales.quote.converted` event/audit evidence with safe/reportable payloads.
- No `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` entity/table was introduced.
- Public WebApi, mobile/member commerce, storefront checkout, payment/shipment/refund flows, and invoice archive/download behavior remain unchanged.

## DeliveryNote Core Model Outcome

Darwin now has an internal/WebAdmin-only formal `DeliveryNote` implementation.

- `DeliveryNote` and `DeliveryNoteLine` are additive Sales schema entities; no `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` entity/table was introduced.
- Delivery notes are created from existing shipments. V1 quantity authority is `ShipmentLine.Quantity`; operators cannot enter delivery-note quantities manually.
- Each non-deleted shipment can have one delivery note. A cancelled delivery note remains official evidence for that shipment rather than being silently duplicated.
- Delivery note lines preserve order/shipment snapshots for catalog and non-catalog order lines. Non-catalog lines can be documented without fake products.
- Issuing a delivery note reserves `NumberSequenceDocumentType.DeliveryNote` exactly once. Draft and prepared notes have no official number.
- Lifecycle transitions are centralized in `DeliveryNoteWorkflowPolicy`, so future customer-specific workflow variation belongs in a policy/service layer and foundation primitives, not forked controllers or views.
- Delivery note lifecycle evidence is written through deterministic `BusinessEvent` and `AuditTrail` records with safe/reportable payloads only.
- WebAdmin Sales now includes Delivery Notes navigation, list/filtering, create-from-shipment, detail, prepare, issue, mark shipped, mark delivered, and cancel actions.
- Shipment provider operations, labels, warehouse stock movement, payment, refund, invoice archive/source-model, storefront checkout, public WebApi, mobile/member commerce, and download behavior remain unchanged.

## ReturnOrder Core Model Outcome

Darwin now implements `ReturnOrder`, `ReturnOrderLine`, and `ReturnOrderRefundLink` as internal/WebAdmin Sales documents over current order, shipment, inventory, and refund foundations.

- Return requests, approvals, receipts, inspections, refund readiness, refund linkage, closure, and cancellation are modeled explicitly.
- Refund and restock are rejected before inspection. Refund eligibility is based on accepted quantities; restock is limited to variant-backed accepted/restock quantities.
- Actual refund settlement remains in current refund/payment handlers. ReturnOrder links completed refunds for evidence and reconciliation instead of creating a second refund ledger.
- Actual stock movement remains in inventory handlers through the existing return receipt flow with `ReferenceId = ReturnOrderId`.
- Return order numbering uses `NumberSequenceDocumentType.ReturnOrder` at approval and is not recomputed.
- WebAdmin Sales includes Return Orders list/detail/lifecycle actions and does not create shipment/payment/refund/invoice/credit-note mutation paths.
- Public WebApi, mobile/member, storefront checkout, invoice archive/download, payment-intent routes, and issued snapshots remain unchanged.

## ReturnOrder Refund And Inventory Reconciliation Hardening Outcome

Darwin now hardens ReturnOrder reconciliation without changing public WebApi, mobile/member commerce, storefront checkout, payment-intent routes, invoice archive/download behavior, issued snapshots, or credit-note policy.

- Linked refunds must be completed, active, same-order, same-currency settlement records. Partial linked totals stay `RefundReady`; exact coverage moves the return to `Refunded`; over-linking is rejected.
- Duplicate refund linkage is idempotent and does not create duplicate rows or double-count settlement.
- Inventory restock remains owned by the existing return receipt flow and is guarded by `ReferenceId = ReturnOrderId` so retry does not create duplicate stock movement.
- Non-catalog return lines remain valid for commercial return and refund evidence, but restock is rejected because no stock item exists.
- WebAdmin Return Orders show linked and remaining refund amounts and keep quantity inputs scoped to create, approve, receive, and inspect stages.
- Sales Return Orders still do not create refund, payment, shipment, invoice, or credit-note mutation paths.

## CreditNote Finance/Accounting Boundary Design Outcome

CreditNote boundaries are now documented in [credit-note-finance-accounting-boundary-design.md](credit-note-finance-accounting-boundary-design.md). This outcome is documentation-only and does not add schema, handlers, routes, DTOs, WebAdmin actions, mobile/member/storefront exposure, archive/download behavior, or production flows.

- `CreditNote` remains a future formal finance/sales document and does not replace `Refund`, `ReturnOrder`, `Invoice`, or invoice cancellation.
- Negative invoices, non-posting cosmetic documents, and UI-only credit-note records are explicitly forbidden.
- Credit-note lines must derive from issued invoice snapshot/source-model evidence, not live catalog, current tax settings, or mutable invoice lines.
- `Refund` remains the authoritative settlement record; credit note can link to settlement evidence but does not execute settlement.
- `ReturnOrder` can provide goods-return reason and inspection evidence, but it does not issue credit notes.
- Legal numbering, tax reversal, receivable posting, immutable archive/source-model, cancellation/void policy, and member visibility must be defined before implementation.
- Account mapping, receivables projection, and invoice/payment/refund/cancellation posting wiring are implemented in [finance-account-mapping-receivables-design.md](finance-account-mapping-receivables-design.md) and [finance-receivables-projection.md](finance-receivables-projection.md). Credit-note source/archive policy is documented in [credit-note-source-model-archive-design.md](credit-note-source-model-archive-design.md). `CreditNote Core Model And Admin Slice` is implemented as an internal/WebAdmin surface with source/archive fields, legal numbering, tax reversal, posting, cumulative credit validation, and lifecycle evidence from the start.

## CreditNote Source Model And Archive Design Outcome

Credit-note source model and archive policy is now documented in [credit-note-source-model-archive-design.md](credit-note-source-model-archive-design.md). This outcome is documentation-only and does not add schema, handlers, routes, DTOs, WebAdmin actions, mobile/member/storefront exposure, archive/download behavior, or production flows.

- `CreditNote` must be its own formal internal/WebAdmin document, not a negative invoice, refund view, return-order printout, generic document attachment, or UI-only record.
- Issued credit-note source JSON must be immutable and must include original invoice source references, credited lines, tax reversal summary, linked refund/return evidence, posting ids, and archive metadata.
- Credit-note lines derive from original issued invoice source evidence, not live catalog, current tax settings, mutable invoice lines, or return-order line text.
- Draft credit notes do not consume official numbers; issue reserves `NumberSequenceDocumentType.CreditNote` exactly once.
- `DocumentRecord` can hold supporting evidence, but legal credit-note source/archive remains owned by the credit-note archive model.
- V1 remains internal/WebAdmin and does not change public WebApi, mobile/member, storefront, invoice archive/download, or issued invoice contracts.

## CreditNote Reconciliation And Source Export Hardening Outcome

Darwin now hardens credit-note reconciliation before exposing any broader finance/reporting workflow.

- Duplicate credit-note create rows for the same invoice line are aggregated into one credited quantity and still respect cumulative over-credit validation.
- Optional refund evidence is accepted only when it is completed, active, and reconcilable to the linked invoice through payment/order/currency boundaries.
- WebAdmin line prefill now uses remaining creditable quantities after prior issued credit notes.
- Internal source-model download is available only for issued or voided credit notes with immutable source hash. It does not add member/mobile/storefront download contracts.
- CreditNote remains a formal internal/WebAdmin document and still does not create refunds, payments, negative invoices, parallel invoices, or generic document-record legal archives.

## Finance Posting Foundation Implementation Outcome

Finance posting boundaries and implementation outcome are now documented in [finance-posting-foundation-design.md](finance-posting-foundation-design.md). The implementation adds additive journal-entry metadata, migrations, and an internal posting service. It does not add routes, DTOs, WebAdmin actions, mobile/member/storefront exposure, archive/download behavior, or production posting flows.

- Darwin already has lightweight `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` entities in Billing. They should be evolved instead of replaced by a parallel finance ledger.
- Manual journal entries remain operator-owned and row-version protected.
- Automated posting now has structured source linkage, idempotency, posting lifecycle/status, safe metadata, and balanced account validation through `FinancePostingService`.
- `Invoice`, `Payment`, `Refund`, and `ReturnOrder` stay operational source records; posting consumes their facts and does not replace their mutation owners.
- `CreditNote` core implementation is now gated by implementing the already-designed source/archive, tax reversal, legal numbering, cumulative credit validation, posting, and lifecycle evidence policy as one complete internal/WebAdmin slice.
