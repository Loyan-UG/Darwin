# ERP Expansion Master Status

## Summary

This document is the compact decision status for the ERP redesign and expansion track. It is a planning and reconciliation document, not a replacement for the historical implementation ledger, module audit, or code-backed go-live status.

Darwin remains a single coherent ERP product. Module separation stays logical through UI navigation, permissions, feature visibility, ownership rules, and internal services. No project split or database split is introduced only to model modules.

## Completed Expansion Work

| Area | Status | Completed outcome | Compatibility notes | Next action |
| --- | --- | --- | --- | --- |
| Release-sensitive mobile foundation | Complete for current expansion baseline. | Identity/profile, business access, loyalty/scan, mobile API boundary, member commerce, order/invoice snapshot, and launch-sensitive surfaces were reviewed and guarded. | Mobile/member contracts stay stable unless a deliberate pre-release migration is designed and tested. | Keep contract and mobile smoke lanes green for every domain change. |
| Foundation primitives | Complete for current ERP expansion needs. | `ExternalSystem`, `ExternalReference`, source-of-truth markers, custom fields, activities, notes, document metadata, number sequences, business events, audit trails, and feature areas exist. | These primitives are shared foundations, not replacements for specialized ledgers, invoice archives, loyalty ledgers, or provider records. | Reuse primitives in purchasing, inventory, HR, and automation slices. |
| CRM expansion | Complete for current phase. | CRM core fields, customer bridge decisions, foundation primitive integration, and WebAdmin CRM exposure are implemented. | `Customer` does not own auth secrets, device tokens, provider tokens, phone verification tokens, push tokens, or loyalty balance. | Add CRM import/reference management only when an operational import flow is selected. |
| Sales expansion | Complete for current phase. | Sales projections, additive fields, Sales workspace, lifecycle evidence, quotes, quote-to-order conversion, delivery notes, return orders, credit notes, and reconciliation hardening exist. | Current `Order` and `Invoice` remain the Sales order and issued invoice foundations. No parallel `SalesInvoice` or `FinanceInvoice` model is introduced. | Continue with purchasing and inventory before adding more Sales document surfaces. |
| Finance foundation | Complete for current phase. | Posting foundation, account mapping, receivables projection, Finance reporting workspace, credit-note posting, export batch/package/storage, WebAdmin export, connector foundation, file-delivery adapter, and operational hardening exist. | Export reads posted journal entries and stored packages; it does not rebuild from mutable operational documents. | Accounting API adapter only after a real target and credential policy are selected. |
| Deployment docs for finance export storage | Complete for current phase. | `FinanceExports` and `FinanceExportOutbound` profiles are documented as package source and outbound destination. | Credentials remain in secure configuration, never batch metadata, document metadata, external-reference metadata, docs, or logs. | Validate selected-provider production smoke per deployment. |
| Purchasing supplier and purchase order core | Complete for current phase. | Current `Supplier`, `PurchaseOrder`, and `PurchaseOrderLine` evolved with common purchasing fields, supplier status, sequence-backed purchase order numbering, lifecycle timestamps, received/cancelled line quantities, WebAdmin exposure, and event/audit evidence. | No parallel supplier or purchase order model; no public WebApi, mobile/member, storefront, invoice archive/download, finance export, payment, refund, or supplier invoice flow change. | Continue with formal goods receipt and inventory reconciliation. |
| Goods receipt and inventory reconciliation | Complete for current phase. | `GoodsReceipt` and `GoodsReceiptLine` exist in the Inventory schema with receive, inspect, post, cancel, receipt numbering, WebAdmin workflow, idempotent inventory posting, purchase order line reconciliation, and event/audit evidence. | `InventoryTransaction` remains the authoritative stock ledger. Public WebApi, mobile/member, storefront, invoice archive/download, finance export, payment/refund, and supplier invoice flows are unchanged. | Move to supplier invoice and payables boundary design before any payable implementation. |
| Supplier invoice and payables boundary design | Complete for current phase. | Supplier invoice ownership, purchase order and goods receipt matching, payable posting boundary, payment terms, tax/export impact, document evidence, and WebAdmin/internal limits are locked. | Future `SupplierInvoice` is a formal payables model, not current customer `Invoice`, not a negative receivable, and not a cosmetic document. | Implement supplier invoice core model and WebAdmin workflow without posting/payment shortcuts. |
| Supplier invoice core model and WebAdmin | Complete for current phase. | `SupplierInvoice` and `SupplierInvoiceLine` exist in Billing with internal/WebAdmin create, update, match, approve, void, matching-ready line state, optional internal sequence numbering, and event/audit evidence. | No payable journal entry, supplier payment, public WebApi, mobile/member, storefront, customer invoice archive/download, finance export format, payment, or refund flow change. | Implement supplier invoice matching and posting through finance posting services. |
| Supplier invoice matching and posting | Complete for current phase. | Approved and matched supplier invoices post payable liability through `FinancePostingService` using hybrid account roles: `InventoryClearing`, `PurchaseExpense`, `TaxReceivable`, and `AccountsPayable`. | Finance export format is unchanged and continues to read posted journal entries. Supplier payment settlement, public/mobile exposure, storefront changes, and customer invoice archive/download changes are not added. | Design supplier payment boundary before implementation. |

## Active Next Gate

`Supplier Payment Boundary Design Slice`

The next ERP expansion step is to design supplier payment settlement before any payment implementation:

- keep supplier payments separate from customer payments and refunds;
- define partial payment, settlement evidence, payable aging, reversal, export, and account mapping boundaries;
- keep `SupplierInvoice` and posted journal entries as the payable source;
- avoid public WebApi, mobile/member, storefront, customer invoice archive/download, and finance export format changes unless deliberately designed;
- keep public WebApi, mobile/member, storefront, issued invoice archive/download, and finance export package compatibility unchanged;
- reuse `BusinessEvent`, `AuditTrail`, `FinancePostingAccountMapping`, `JournalEntry`, and `JournalEntryLine`.

## Remaining ERP Work

| Area | Status | Required work | Dependency | Default decision |
| --- | --- | --- | --- | --- |
| Goods receipt and purchasing receiving | Complete for current phase. | Formal goods receipt, receipt lines, accepted/rejected/damaged quantities, idempotent inventory movement ownership, and WebAdmin receiving workflow. | Hardened current `Supplier`, `PurchaseOrder`, and `PurchaseOrderLine`; inventory stock movement owners; foundation primitives. | Receipt is the boundary for stock increase; no parallel stock ledger. |
| Purchasing contacts and documents | Remaining. | Structured supplier contacts, supplier document metadata exposure, purchasing attachments, and visibility rules. | Hardened supplier master and `DocumentRecord`. | Add structured contacts/documents only as a cohesive slice, not as notes-only data. |
| Inventory ledger and warehouse operations | Remaining. | Warehouse locations/bins, stock ledger depth, reservations, transfers, counts, lots, serials, handling units, receiving tasks, picking tasks, and warehouse task workflow. | Purchasing goods receipt design and current inventory operations. | Mobile-first PWA by default; native app only if hardware/offline needs require it. |
| Supplier invoice and payables | Complete for current phase. | Supplier invoice core/admin and payable posting are implemented; supplier payment settlement remains to be designed. | Completed supplier invoice core/admin, posted goods receipts, finance posting, and account mappings. | Create liability only through finance posting policy; design payments separately. |
| Supplier payment settlement | Next design. | Supplier payment records, partial settlement, payable aging, payment posting, export impact, and reversal rules. | Posted supplier invoice payables and finance posting foundation. | Do not reuse customer payment/refund flows as supplier settlement. |
| HR and time tracking | Remaining. | Employee, department, position, contract, personnel file, schedule, attendance, time entry, absence, leave, timesheet, and payroll period. | Business member linkage decision and feature visibility. | `BusinessMember` remains business access source; employee/staff records link to it. |
| AI-readiness and automation governance | Remaining. | Sensitive-field classification, scoped data access, recommendation records, action drafts, approval workflow, and automation audit. | Business events, audit trails, permissions, feature areas. | AI drafts or proposes actions; normal commands remain mutation owners. |
| Sync state and conflict handling | Remaining. | Sync state, inbound reconciliation, conflict records, resolution workflow, and target-specific retry semantics. | External references and concrete inbound sync use case. | Do not introduce two-way sync until an inbound reconciliation target exists. |
| Accounting API target adapter | Conditional. | Target-specific credential policy, payload mapping, delivery contract, error handling, and smoke strategy. | Finance export file-delivery foundation and selected real target. | File-delivery remains production-safe until a real API target is chosen. |

## Operating Rules

- Update `BACKLOG.md` before starting a major ERP slice when the backlog no longer matches the current implementation state.
- Update the relevant domain-expansion design document before schema, migration, public contract, mobile contract, or WebAdmin mutation changes.
- Keep public WebApi, mobile/member, storefront, invoice archive/download, and issued snapshot compatibility guarded for every Sales, Finance, Purchasing, and Inventory change.
- Use real columns for common, reportable, compliance-relevant, accounting-relevant, inventory-relevant, integration-key, or cross-module fields.
- Use custom fields or JSON for customer-specific, uncertain, low-frequency, provider-specific payload, industry-specific, or unstructured metadata.
- Never store provider credentials, access tokens, refresh tokens, private keys, connection strings, or raw sensitive payloads in domain metadata, document metadata, external references, logs, tests, or documentation.
