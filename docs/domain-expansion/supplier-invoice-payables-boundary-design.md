# Supplier Invoice And Payables Boundary Design

## Summary

This document locks the supplier invoice and payables boundary and records the core and posting implementation outcomes. The original boundary step was documentation-only. The core implementation later added internal Billing/WebAdmin `SupplierInvoice` and `SupplierInvoiceLine` support. The posting implementation then added payable journal posting for approved and matched supplier invoices without public routes, mobile/member contracts, storefront behavior, finance export package changes, customer payment/refund flow changes, or customer invoice archive/download behavior changes. Supplier payment settlement is separately locked and implemented in [supplier-payment-boundary-design.md](supplier-payment-boundary-design.md), with reversal and bank/treasury boundaries locked in [supplier-payment-reversal-bank-treasury-boundary-design.md](supplier-payment-reversal-bank-treasury-boundary-design.md).

The core decision is that `SupplierInvoice` is a formal payables document. It is not the current customer-facing `Invoice`, not a negative receivable, not a note, not a JSON-only upload, and not a document-only UI shortcut. Payables must be created only through explicit matching and finance posting policy.

## Current Darwin Purchasing/Payables Findings

- `Supplier`, `PurchaseOrder`, `PurchaseOrderLine`, `GoodsReceipt`, and `GoodsReceiptLine` are the current purchasing and receiving foundations.
- `GoodsReceipt` is the formal stock-increase boundary; `InventoryTransaction` remains the authoritative stock ledger.
- Current `Invoice` is the customer-facing/shared issued invoice foundation. Supplier invoice must not reuse it or create a second issued-invoice shortcut.
- `SupplierInvoice` and `SupplierInvoiceLine` now exist in Billing as the internal/WebAdmin supplier invoice core model with matching-ready line state, supplier invoice number, optional internal number, dates, terms, due date, currency, totals, lifecycle timestamps, metadata, and event/audit evidence.
- Approved and matched supplier invoices can now post payable liability through `FinancePostingService` into `JournalEntry` and `JournalEntryLine`. `SupplierInvoice` stores `PostingJournalEntryId`, `PostedAtUtc`, and `Posted` status for idempotency and UI evidence.
- Supplier payment boundary design and core/admin implementation are complete. `SupplierPayment` is a formal Billing/Finance flow from posted supplier invoices, not customer `Payment`, customer `Refund`, manual journal shortcut, status text, note, or attachment upload.
- Supplier payment reversal implementation is complete for full posted payments. Posted supplier payments require formal reversal journal entries for correction; direct bank settlement, returned transfers, reconciliation, and overpayment/advance remain blocked until bank/treasury foundation is designed.
- `FinancialAccount`, `FinancePostingAccountMapping`, receivables projection, and finance export foundation exist. Finance export continues to read posted journal entries and does not export supplier invoice UI state directly.
- `DocumentRecord`, `ExternalReference`, `BusinessEvent`, `AuditTrail`, and `NumberSequence` are available and should be reused for evidence, identity, lifecycle history, and numbering.
- Payables must not be created by status text, attachment upload, manual journal shortcut, negative receivable, or cosmetic document state.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, payment/refund flows, and issued invoice source-model behavior remain unchanged.

## Decision Matrix

| Payables surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Matching/posting impact | Tax/finance export impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Supplier invoice trigger | Purchase orders and goods receipts exist; `SupplierInvoice` core now exists. | Supplier invoice can be created from supplier evidence and optionally linked to purchase orders and posted goods receipts. | Payables application service. | Supplier invoice command handlers only. | Trigger does not create payable until matching and posting complete. | No export entry until posted journal entries exist. | Unchanged. | Core/admin implemented. | Add posting only through finance posting slice. |
| Supplier invoice header ownership | `SupplierInvoice` owns supplier, supplier invoice number, dates, terms, currency, totals, and status. | Current customer `Invoice` remains separate. Supplier invoice owns payables header evidence. | Payables model. | Supplier invoice handlers. | Header provides payable source after validation. | Header fields feed posting and export later. | Unchanged. | Core/admin implemented. | Keep customer `Invoice` out of supplier payables. |
| Supplier invoice line ownership | Supplier invoice lines store supplier SKU/description, invoiced quantity, unit/total net/tax/gross, tax rate, and matching status. | Supplier invoice lines come from supplier invoice evidence and matching results, not live catalog or mutable tax settings. | Payables model. | Supplier invoice handlers. | Lines distinguish invoiced quantity/cost from ordered and received quantities. | Posted lines later drive tax and payables journals. | Unchanged. | Core/admin implemented. | Extend posting from line snapshots, not live catalog. |
| Purchase order matching | `PurchaseOrderLine` tracks ordered and received/cancelled quantities. | Matching compares invoice lines to ordered quantity, ordered cost, currency, and line identity. | Payables matching service. | Supplier invoice matching handlers. | Match exceptions block approval and future posting until resolved. | Price differences require explicit discrepancy treatment. | Unchanged. | Core matching state implemented. | Harden posting preconditions in next slice. |
| Goods receipt matching | `GoodsReceiptLine` tracks received, accepted, rejected, and damaged quantities. | Matching compares invoice quantity to posted accepted receipt quantity, not merely requested or received quantity. | Payables matching service. | Supplier invoice matching handlers. | Payable posting cannot rely on unposted or uninspected receipt quantity. | Receipt-backed evidence supports export audit. | Unchanged. | Core matching state implemented. | Use posted receipts as posting evidence. |
| Price/tax discrepancy | Purchase documents have operational costs; supplier invoice tax is not canonical. | Discrepancies are structured match results and must not be hidden in notes. | Payables matching service. | Supplier invoice discrepancy handlers. | Discrepancies block or route posting according to policy. | Tax reversal or adjustment requires finance posting policy. | Unchanged. | Needs implementation policy in core slice. | Add explicit discrepancy state before posting. |
| Payable posting | Finance posting foundation exists and supplier invoice posting is implemented. | Payable exists only after valid supplier invoice posting creates journal entries through finance posting services. | Finance posting service. | Supplier invoice posting handler. | Upload/status alone cannot create liability. | Finance export reads posted journal entries, not supplier invoice UI state. | Unchanged. | Implemented for approved/matched supplier invoices. | Design supplier payment settlement next. |
| Payment terms and due date | Supplier has common terms; purchase orders can carry purchasing fields. | Supplier invoice stores resolved payment terms and due date snapshots. | Supplier invoice header. | Supplier invoice create/update handlers. | Due date feeds payables aging after posting. | Export can include posted payable maturity later. | Unchanged. | Ready for additive fields. | Resolve from supplier terms unless explicitly overridden. |
| Supplier payment boundary | Payment/refund flows exist for customer/order settlement; supplier payment is now implemented separately. | Supplier payment settlement is a formal Billing/Finance flow and must not be faked as refund, order payment, note, attachment, status text, or manual journal-only state. | Supplier payment service. | Supplier payment handlers. | Payment reduces payable only through finance posting and settlement linkage; reversal reopens payable through inverse posting. | Export reads posted supplier payment and reversal entries through the existing journal package path. | Unchanged. | Core/admin/reversal implemented. | Design bank/treasury before direct bank settlement. |
| Numbering policy | `NumberSequence` exists; supplier external invoice numbers are modeled on `SupplierInvoice`. | Supplier invoice has supplier-provided invoice number as reportable evidence and may reserve an internal number through `NumberSequenceDocumentType.SupplierInvoice` on approval. | Supplier invoice service. | Supplier invoice handlers. | Numbering does not imply posting. | Export identity should include stable internal and supplier references. | Unchanged. | Core/admin implemented. | Use internal number in posting/export evidence when present. |
| Supplier document evidence | `DocumentRecord` exists for metadata and storage references. | Supplier invoice attachments use `DocumentRecord`, but attachments do not replace matching, posting, or audit evidence. | Document service plus supplier invoice service. | Document registration handlers. | Documents can support review and matching; they do not create payable. | Legal/accounting evidence depends on posted records and immutable references. | Unchanged. | Ready for internal/WebAdmin exposure. | Link documents to supplier invoice and source entities. |
| External supplier/accounting identity | `ExternalReference` exists. | Import ids, remote supplier invoice ids, and accounting target ids use `ExternalReference`; supplier external invoice number is a real supplier invoice field. | Integration foundation. | External reference service. | External ids do not decide posting state. | Export target ids link to posted package/delivery evidence. | Unchanged. | Ready for integration-aware core. | Avoid provider-specific columns. |
| Lifecycle event/audit | `BusinessEvent` and `AuditTrail` exist. | Supplier invoice create, match, approve, post, void, and payment linkage later record deterministic events/audits. | Supplier invoice and finance handlers. | Lifecycle command handlers. | Evidence must align with actual state changes. | Posting/export evidence references journal entries and batch exports. | Unchanged. | Ready for core lifecycle. | Add event keys in implementation. |
| Finance export impact | Finance export reads posted journal entries and stored export packages. | Supplier invoice posting creates normal posted journal entries; export format remains unchanged. | Finance export services. | Finance posting/export handlers. | Export remains downstream of posting. | No direct export from supplier invoice UI or uploaded documents. | Unchanged. | Implemented through journal entry source. | Keep export package source as journal entries. |
| WebAdmin/internal visibility | Inventory WebAdmin exposes suppliers, purchase orders, and goods receipts. | Supplier invoice v1 is internal/WebAdmin only after core model implementation. | WebAdmin purchasing/finance workspace. | WebAdmin controller calls supplier invoice handlers. | UI cannot create payables without posting command. | UI cannot bypass archive/export owners. | Unchanged. | Ready after core model. | Add compact operator screens only after schema/service implementation. |
| Mobile/member/storefront boundary | Current public/member commerce is customer order/invoice oriented. | Supplier invoice/payables have no public, mobile/member, or storefront exposure in v1. | Internal application and WebAdmin. | None outside internal/WebAdmin. | No payable mutation from mobile/storefront. | No customer invoice archive/download change. | Unchanged. | Locked. | Keep compatibility smoke lanes unchanged. |

## Locked Decisions

- `SupplierInvoice` is a formal payables model, not the current customer-facing `Invoice` and not a document-only or JSON-only record.
- Supplier invoice must link to `Supplier` and may link to `PurchaseOrder`, `GoodsReceipt`, and supplier documents.
- Supplier invoice lines come from supplier invoice evidence and matching results; they must not be recomputed from live catalog, current supplier settings, or current tax settings.
- Matching must distinguish purchase order ordered quantity/cost from posted goods receipt accepted quantity.
- Payable liability is created only through valid finance posting. UI status, attachment upload, and manual notes do not create liability.
- Supplier invoice posting uses hybrid account mapping: inventory-backed lines debit `InventoryClearing`, service/non-catalog lines debit `PurchaseExpense`, tax debits `TaxReceivable`, and gross total credits `AccountsPayable`.
- Posted supplier invoices cannot be voided without a future reversal/adjustment design.
- Supplier payment settlement is implemented as a separate Billing/Finance flow and is not implemented by customer payment, refund, negative receivable, note, attachment upload, status text, or journal shortcut behavior.
- Supplier payment core/admin implementation is complete. It allows payment only for posted supplier invoices, supports partial payment, blocks overpayment by default, and posts by debiting `AccountsPayable` and crediting `CashClearing`.
- Supplier payment reversal core implementation is complete. It allows only full-payment reversal of posted supplier payments, posts debit `CashClearing` and credit `AccountsPayable`, and does not claim external bank movement.
- Numbering uses `NumberSequenceDocumentType.SupplierInvoice`; approval reserves the optional internal number only once when an active sequence is configured.
- Supplier external invoice number is a reportable future supplier invoice field; provider/import/accounting target ids use `ExternalReference`.
- Attachments use `DocumentRecord`, but legal/accounting evidence remains owned by matching, posting, events, audits, and export records.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, payment/refund flows, and issued invoice source-model contracts remain unchanged.

## Implementation Sequence

1. `Supplier Invoice Core Model And Admin Slice`
   - Outcome: completed. Additive internal/WebAdmin `SupplierInvoice` and `SupplierInvoiceLine` model exists in Billing.
   - Supplier invoice links supplier, optional purchase order, optional goods receipt, lifecycle events, and audit evidence.
   - Supplier invoice stores supplier invoice number, optional internal number, dates, payment terms, due date, currency, totals, line snapshots, and matching state.
   - It creates no payable journal entries, supplier payments, public routes, mobile/member exposure, storefront changes, or customer invoice archive changes.
2. `Supplier Invoice Matching And Posting Slice`
   - Outcome: completed. Supplier invoice lines match to purchase order and posted goods receipt evidence, including cumulative over-invoicing guards.
   - Approved and matched invoices can create payable journal entries only through finance posting services.
   - Finance export remains downstream of posted journal entries and its package format is unchanged.
3. `Supplier Payment Boundary Design Slice`
   - Outcome: completed in [supplier-payment-boundary-design.md](supplier-payment-boundary-design.md). Supplier payment settlement, partial payment, payment export, payable aging, AP clearing, reversal limits, and evidence boundaries are locked before implementation.
4. `Supplier Payment Core Model And Admin Slice`
   - Outcome: completed in [supplier-payment-boundary-design.md](supplier-payment-boundary-design.md). `SupplierPayment` and `SupplierPaymentAllocation` are implemented with internal/WebAdmin workflow, finance posting, partial allocations, overpayment guards, and no customer payment/refund reuse.
   - Do not reuse customer payment/refund flows as supplier settlement.
5. `Supplier Payment Reversal And Bank/Treasury Boundary Design`
   - Outcome: completed in [supplier-payment-reversal-bank-treasury-boundary-design.md](supplier-payment-reversal-bank-treasury-boundary-design.md). Posted supplier payment correction must use formal reversal posting, while direct bank settlement and reconciliation wait for bank/treasury foundation.
6. `Supplier Payment Reversal Core Slice`
   - Outcome: completed in [supplier-payment-boundary-design.md](supplier-payment-boundary-design.md). Posted supplier payments can be fully reversed through finance posting with original and reversal journal linkage.
7. `Bank/Treasury Foundation Design Slice`
   - Design bank account ownership, treasury clearing, bank statement import, reconciliation, returned transfer handling, and direct bank settlement before extending supplier payment settlement.
8. `Supplier Documents And Contacts Slice`
   - Add structured supplier contacts and richer purchasing document exposure when invoice evidence and visibility rules are stable.

## Next Implementation Slice

`Bank/Treasury Foundation Design Slice`

The next step should design bank/treasury ownership before direct bank settlement or reconciliation. Supplier contacts/documents can proceed as a separate purchasing UX slice if bank settlement is not the immediate operational priority.
