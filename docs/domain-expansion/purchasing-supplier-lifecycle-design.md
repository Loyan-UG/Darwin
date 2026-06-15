# Purchasing And Supplier Lifecycle Design

## Summary

This document locks the Purchasing expansion boundary and records the completed Purchasing implementation slices through formal goods receipt. The design step was documentation-only; subsequent implementation slices added schema, Application, and WebAdmin changes for the existing `Supplier`, `PurchaseOrder`, `GoodsReceipt`, and related line models only. They did not add public routes, mobile contracts, storefront changes, supplier invoice posting, invoice archive/download changes, or production finance flow changes.

The default decision is to evolve the current `Supplier` and `PurchaseOrder` foundations. Darwin must not create parallel supplier or purchase order models when the current models can be extended safely.

## Current Darwin Purchasing/Inventory Findings

- `Supplier` exists in the Inventory area with business scope, contact fields, status, code, common purchasing terms, tax reference, website, and notes.
- `PurchaseOrder` and purchase order lines exist and are surfaced in WebAdmin Inventory operations with currency, expected delivery, lifecycle timestamps, supplier SKU, line descriptions, and received/cancelled quantity tracking.
- Current purchase order lifecycle supports draft, issued, cancelled, and received behavior with row-version checks and deterministic event/audit evidence.
- `GoodsReceipt` and `GoodsReceiptLine` now exist as formal Inventory documents for receiving, inspection, stock posting, and audit evidence.
- Legacy purchase order receive behavior routes through a formal posted goods receipt so current operators keep behavior while inventory evidence becomes document-backed.
- `Warehouse`, `StockLevel`, `StockTransfer`, `InventoryTransaction`, and order/inventory reservation logic exist.
- `NumberSequence` exists. Purchase order numbering now allows manual input, but empty purchase order numbers reserve `NumberSequenceDocumentType.PurchaseOrder`; missing active sequence fails instead of creating a fake number.
- `ExternalReference`, `DocumentRecord`, `Activity`, `Note`, `BusinessEvent`, and `AuditTrail` exist and should be reused for purchasing evidence, documents, and external identity.
- `SupplierInvoice` and `SupplierInvoiceLine` now exist in Billing as the internal/WebAdmin supplier invoice core model with matching-ready state, payable posting, and journal-entry linkage.
- Supplier payment settlement boundary design, core/admin implementation, full-payment reversal, and reversal/bank-treasury boundary design are complete. `SupplierPayment` is a formal Billing/Finance flow and must not reuse customer `Payment`, customer `Refund`, supplier invoice status text, notes, attachment upload, or manual journal shortcuts.
- `FinancialAccount`, account mappings, journal entries, receivables, credit-note posting, and finance export foundations exist; supplier payable liability is created only by supplier invoice posting through finance posting services.
- Public WebApi, mobile/member, storefront checkout, invoice archive/download, and issued snapshot contracts do not depend on purchasing expansion and must remain unchanged in v1.

## Purchasing Decision Matrix

| Purchasing surface | Current Darwin model | Decision | Owning source | Foundation primitive to reuse | Schema/API impact | Inventory impact | Finance/payables impact | Mobile/member impact | Priority | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Supplier master | `Supplier` exists with business-scoped contact, status, code, and common purchasing fields. | Evolve `Supplier` with canonical purchasing fields instead of adding a parallel supplier model. | Inventory/Purchasing application services. | `ExternalReference`, custom fields, `Activity`, `Note`, `DocumentRecord`. | Additive supplier fields implemented. No public/mobile contract change. | Supplier drives purchasing and receiving workflows. | Supplier can later link to payables. | Unchanged. | P0 | Keep supplier master stable; add contacts/documents in a later cohesive slice. |
| Supplier contacts | No dedicated supplier contact entity is canonical. | Add structured supplier contacts only when implementation begins; do not overload supplier notes or JSON for frequent contacts. | Future purchasing service. | `DocumentRecord`, `Activity`, `Note`, custom fields for customer-specific contact metadata. | Future additive table likely. | None directly. | Contact can support invoice and payment communication later. | Unchanged. | P1 | Implement after supplier master lifecycle fields are stable. |
| Supplier terms | Supplier has common payment term, preferred currency, lead time, and external note fields. | Common/reportable terms are real columns; customer-specific terms use custom fields. | Supplier master. | `CustomFieldDefinition`, `CustomFieldValue`. | Additive common fields implemented. | Lead time can inform replenishment. | Payment terms feed payables later. | Unchanged. | P1 | Use custom fields for non-standard terms instead of adding JSON-only policy. |
| Purchase request | No separate request model exists. | Design as a future internal document before implementation; do not fake requests as draft purchase orders if approval history is required. | Future purchasing workflow. | `NumberSequence`, `BusinessEvent`, `AuditTrail`, `DocumentRecord`. | No change in this design step. | Can reserve expected demand later, but not in v1 design. | No posting. | Unchanged. | P2 | Implement only if approval/request workflow is needed before purchase order issue. |
| Purchase order | `PurchaseOrder` exists with currency, expected delivery, lifecycle timestamps, internal notes, sequence-backed numbering, and event/audit evidence. | Current `PurchaseOrder` is the canonical purchase order foundation and evolves in place. | Current inventory/purchasing handlers. | `NumberSequence`, `ExternalReference`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`. | Additive fields and lifecycle hardening implemented. | Purchase order issue/receive does not bypass inventory owner. | Supplier invoice matching later references purchase orders. | Unchanged. | P0 | Move next to formal goods receipt boundary. |
| Purchase order line | Purchase order lines exist with supplier SKU, description, ordered quantity, received quantity, cancelled quantity, and cost fields. | Current lines evolve in place; receipt tracking is real fields, not JSON. | Purchase order aggregate. | `ExternalReference` for supplier line ids, custom fields for special attributes. | Additive line fields implemented. | Goods receipt quantities derive from purchase order line quantities. | Supplier invoice matching depends on line identity and amounts. | Unchanged. | P0 | Add goods receipt line model before partial/discrepancy receiving. |
| Goods receipt | `GoodsReceipt` exists with draft, received, inspected, posted, and cancelled states. | Goods receipt is the formal purchasing/inventory boundary for stock increase. It records received and accepted quantities, then calls inventory movement owners idempotently. | Goods receipt service plus inventory handlers. | `NumberSequence`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`. | Additive document model implemented. No public/mobile contract change. | Receipt is the entry point to stock increase; inventory ledger remains authoritative for stock movement. | Supplier invoice matching can use posted receipt evidence later. | Unchanged. | P0 | Use posted receipts as the matching boundary for supplier invoice/payables design. |
| Receiving quality and discrepancies | `GoodsReceiptLine` stores received, accepted, rejected, and damaged quantities. | Inspection quantities are structured receipt facts; accepted quantity can post to stock, rejected/damaged quantity cannot. | Goods receipt workflow. | `Activity`, `Note`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`. | Additive receipt line fields implemented. | Accepted quantity can increase stock; rejected/damaged quantity does not. | Discrepancies can block supplier invoice matching. | Unchanged. | P1 | Extend discrepancy handling only when warehouse tasks/bin/lot/serial policy is designed. |
| Supplier invoice boundary | `SupplierInvoice` and `SupplierInvoiceLine` exist in Billing; customer `Invoice` remains customer-facing/shared issued invoice foundation. | Do not reuse customer `Invoice` or create a cosmetic supplier invoice. Supplier invoice core/admin and posting are implemented without supplier payment shortcuts. | Payables service. | Finance posting, account mappings, `ExternalReference`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`. | Additive supplier invoice core/posting implemented. No public/mobile API. | Matches purchase order and posted goods receipt evidence. | Payable liability is posted through finance posting services, not UI status or document upload. | Unchanged. | P1 | Implement supplier payment only from the locked payment boundary. |
| Pricing, discount, and tax snapshots | Purchase order amounts exist only at current operational depth. | Purchase documents must keep supplier-price and tax snapshots; do not recompute historical purchase documents from live catalog or supplier settings. | Purchase order and future supplier invoice. | `AuditTrail`, `DocumentRecord`. | Future additive fields. | Inventory valuation may use receipt cost later. | Payables and tax posting depend on snapshots. | Unchanged. | P0 | Classify required purchase amount/tax fields before schema expansion. |
| External references and import/export identity | `ExternalReference` exists. | Supplier, purchase order, purchase order line, goods receipt, and supplier invoice external ids use `ExternalReference`, not provider-specific columns. | Integration foundation. | `ExternalSystem`, `ExternalReference`, `SourceOfTruth`. | No change in this design step. | Imported supplier or receipt records can be labeled later. | Accounting export can link target ids later. | Unchanged. | P1 | Add references only in concrete import/export slices. |
| Purchasing documents and attachments | `DocumentRecord` exists. | Supplier contracts, purchase order documents, delivery notes from suppliers, receipt photos, and invoice attachments use `DocumentRecord` metadata. | Foundation document service. | `DocumentRecord`. | No upload/download route in this design step. | Receipt documents can support stock acceptance. | Supplier invoice evidence uses documents later. | Unchanged. | P1 | Define visibility before exposing documents outside WebAdmin. |
| Lifecycle event and audit evidence | `BusinessEvent` and `AuditTrail` exist and purchase order and goods receipt transitions record evidence. | Important purchasing transitions use deterministic event/audit evidence. | Purchasing handlers. | `BusinessEvent`, `AuditTrail`. | Purchase order and goods receipt instrumentation implemented. | Provides receiving traceability. | Supports payables audit. | Unchanged. | P0 | Reuse evidence in supplier invoice matching; do not duplicate audit records. |
| Number sequences | `NumberSequence` exists and purchase order and goods receipt numbering use it when official numbers are needed. | New official purchase documents use `NumberSequence`. Existing purchase order numbers are not rewritten. | Purchasing services. | `NumberSequence`. | Purchase order fallback-to-sequence and goods receipt numbering implemented. | Receipt numbers support stock audit. | Supplier invoice numbers may be external supplier numbers plus internal sequence. | Unchanged. | P0 | Define supplier invoice numbering only with payables design. |
| WebAdmin/internal visibility | Inventory WebAdmin exposes suppliers and purchase orders with purchasing fields and lifecycle actions. | Purchasing v1 remains internal/WebAdmin. Do not add mobile/member/storefront exposure. | WebAdmin Purchasing/Inventory workspace. | Feature visibility and permissions. | Existing Inventory WebAdmin routes evolved. | Warehouse receiving UI can link to inventory. | Payables UI waits for finance design. | Unchanged. | P0 | Keep public/mobile contracts stable. |

## Locked Purchasing Decisions

- `Supplier` is the current supplier master foundation and evolves in place.
- `PurchaseOrder` is the current purchase order foundation and evolves in place.
- Do not create parallel `Vendor`, `PurchasingSupplier`, `ProcurementOrder`, or second purchase order models.
- Purchase order numbers and future goods receipt numbers use `NumberSequence`; issued existing numbers are not rewritten.
- Supplier contacts should be structured when frequent/reportable; do not hide common contact records in notes or JSON.
- Goods receipt is the formal boundary before broad receiving expansion. It records receipt facts and delegates stock movement to inventory owners.
- Accepted received quantity can affect stock; rejected, damaged, or scrapped quantity must not increase stock.
- Supplier invoice/payables boundary design, supplier payment boundary design, supplier payment core/admin/posting, and full-payment reversal are complete. Bank/treasury remains the required design boundary before direct bank settlement or reconciliation.
- Supplier invoice must not reuse customer-facing issued invoice behavior as a shortcut.
- Purchasing external identity uses `ExternalReference`.
- Purchasing documents use `DocumentRecord` metadata. Specialized finance or compliance artifacts remain owned by their future specialized records.
- Public WebApi, mobile/member, storefront checkout, invoice archive/download, issued invoice snapshots, and member commerce contracts remain unchanged in v1 purchasing expansion.

## Implementation Sequence

1. `Purchasing Core Supplier And PurchaseOrder Hardening Slice`
   - Evolve current supplier and purchase order fields only where common/reportable.
   - Add lifecycle validation, row-version guards, number-sequence integration, event/audit evidence, and source guards.
   - Keep current WebAdmin Inventory/Purchasing routes stable unless a deliberate WebAdmin workspace rename is designed.
   - Outcome: completed. `Supplier`, `PurchaseOrder`, and `PurchaseOrderLine` evolved in place; purchase order numbering, supplier status, line receipt quantities, lifecycle timestamps, WebAdmin fields, and event/audit evidence are implemented without public/mobile contract changes.
2. `Goods Receipt Core Model And Inventory Reconciliation Slice`
   - Add formal goods receipt and receipt lines.
   - Use purchase order line snapshots and warehouse receipt quantities.
   - Call inventory movement owner idempotently; do not create a parallel stock ledger.
   - Outcome: completed. `GoodsReceipt` and `GoodsReceiptLine` are implemented in the Inventory schema with receive, inspect, post, cancel, receipt numbering, WebAdmin workflow, purchase order line reconciliation, and `InventoryTransaction`-backed stock movement.
3. `Purchasing Documents And Supplier Contacts Slice`
   - Add supplier contacts and document metadata exposure in WebAdmin.
   - Keep upload/download ownership aligned with existing object storage and document record rules.
4. `Supplier Invoice And Payables Design Slice`
   - Outcome: completed in [supplier-invoice-payables-boundary-design.md](supplier-invoice-payables-boundary-design.md). Future `SupplierInvoice` is a formal payables model, not the current customer `Invoice`, not a negative receivable, and not a cosmetic document.
5. `Supplier Invoice Core Model And Admin Slice`
   - Outcome: completed. `SupplierInvoice` and `SupplierInvoiceLine` are implemented in Billing with internal/WebAdmin create, update, match, approve, void, sequence-backed optional internal numbering, and deterministic event/audit evidence.
   - The slice creates no payable journal entries, supplier payments, public/mobile exposure, storefront changes, or customer invoice archive/download changes.
6. `Supplier Invoice Matching And Posting Slice`
   - Outcome: completed. Payable journal entries are created only through finance posting services after supplier invoice matching passes.
   - Supplier payment settlement remains outside this slice, and finance export format remains unchanged.
7. `Supplier Payment Boundary Design Slice`
   - Outcome: completed in [supplier-payment-boundary-design.md](supplier-payment-boundary-design.md). Future supplier payment is a formal Billing/Finance settlement flow from posted supplier invoices and must not reuse customer `Payment`, customer `Refund`, manual journal shortcuts, notes, or attachment uploads.
8. `Supplier Payment Core Model And Admin Slice`
   - Outcome: completed. `SupplierPayment` and `SupplierPaymentAllocation` are implemented in Billing with WebAdmin workflow, partial allocations, AP clearing posting, overpayment guards, and no customer payment/refund reuse.
9. `Supplier Payment Reversal And Bank/Treasury Boundary Design`
   - Outcome: completed in [supplier-payment-reversal-bank-treasury-boundary-design.md](supplier-payment-reversal-bank-treasury-boundary-design.md). Posted supplier payment correction, bank/treasury ownership, reconciliation, overpayment/advance, remittance evidence, and finance export impact are locked before reversal implementation.
10. `Supplier Payment Reversal Core Slice`
   - Outcome: completed. Posted supplier payments can be fully reversed through finance posting services, without direct bank settlement, bank reconciliation, customer payment/refund reuse, public/mobile exposure, or finance export format change.
11. `Bank/Treasury Foundation Design Slice`
   - Design bank account ownership, treasury clearing, bank statement import, reconciliation, returned transfer handling, and direct bank settlement before any bank-facing supplier payment workflow.
12. `Warehouse Receiving Task Design Slice`
   - Define receiving tasks, bin placement, lot/serial capture, discrepancy handling, and mobile-first PWA requirements.
   - Native app work starts only if hardware/offline requirements prove it necessary.

## Test Strategy For Future Implementation

- Unit tests for supplier and purchase order validation, lifecycle transitions, numbering, event/audit idempotency, and sensitive metadata rejection.
- Infrastructure tests for additive fields, schema placement, enum conversions, indexes, and migrations for both supported providers.
- Inventory tests for goods receipt idempotency, accepted/rejected quantity rules, and stock movement ownership.
- WebAdmin tests for supplier, purchase order, and goods receipt pages with authorization, anti-forgery, row-version, HTMX/full-page render, and no public/mobile route creation.
- Compatibility smoke for public contracts and mobile member commerce after every purchasing slice.
