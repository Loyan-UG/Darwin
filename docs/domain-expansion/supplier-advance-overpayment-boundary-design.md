# Supplier Advance And Overpayment Boundary Design

## Summary

This document locks the supplier advance and overpayment boundary before Darwin allows supplier payments beyond open payable. It is design-only. It adds no entity, migration, route, DTO, WebAdmin mutation, public/mobile/storefront contract, customer payment/refund flow change, finance export format change, bank API integration, journal editor shortcut, or production flow.

The core decision is that supplier advance and overpayment must be formal Billing/Finance concepts, not accidental over-allocation on `SupplierPayment`, not a negative supplier invoice, not a customer refund, not a note, and not a manual journal shortcut. Until a dedicated model and posting policy exist, current supplier payment overpayment guards remain active.

## Current Darwin Supplier Advance And Overpayment Findings

- `SupplierInvoice` creates payable liability only after matching, approval, and finance posting.
- `SupplierPayment` and `SupplierPaymentAllocation` settle posted supplier invoices and currently reject cumulative payment allocation beyond open payable.
- Supplier payment posting debits `AccountsPayable` and credits `CashClearing`.
- Bank settlement debits `CashClearing` and credits the mapped bank Asset account, backed by matched bank reconciliation evidence.
- Returned-transfer correction uses `SupplierPaymentBankCorrection`; returned-transfer correction is full-settlement v1 and duplicate-payment correction is evidence/attention only.
- `BankAccount`, `BankStatementImport`, `BankStatementLine`, `BankReconciliationMatch`, and `BankReconciliationMatchLine` exist as treasury evidence and reconciliation foundations.
- `JournalEntry` remains the authoritative accounting fact. Finance export reads posted journal entries and must not read UI-only advance state.
- `ExternalReference`, `DocumentRecord`, `BusinessEvent`, and `AuditTrail` are available for remote ids, documents, lifecycle evidence, and audit evidence.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund flows, and finance export package format remain unchanged.

## Decision Matrix

| Advance surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Accounting impact | Aging/reconciliation impact | Evidence impact | WebAdmin impact | Public/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Advance trigger | Supplier payments currently allocate to posted supplier invoices. | Supplier advance can exist only through a dedicated advance workflow, not through over-allocating invoice payments. | Future supplier advance model. | Future advance handlers. | No accounting without posted advance journal. | Advance is separate from invoice aging until allocated. | Trigger evidence references supplier and payment/bank evidence. | Future internal workflow. | Unchanged. | Ready for core model after this design. | Implement dedicated advance model. |
| Overpayment trigger | Cumulative allocation guard blocks overpayment. | Overpayment is not allowed as accidental allocation. If a supplier is intentionally paid above open payable, the excess becomes supplier advance. | Future advance service. | Future advance handlers. | Excess posts to an advance account, not `AccountsPayable`. | Open payable cannot go negative. | Event explains advance creation. | UI must show split settlement and advance. | Unchanged. | Ready. | Keep guard; add explicit split workflow later. |
| Account role | `AccountsPayable`, `CashClearing`, and bank Asset roles exist. | Add a future `SupplierAdvance` account role mapped to Asset. Supplier advance represents a recoverable/prepaid supplier balance. | Finance account mapping. | Finance account mapping and posting handlers. | Advance creation debits `SupplierAdvance` and credits `CashClearing`; bank settlement clears `CashClearing` to bank Asset. | Advance balance ages separately from AP. | Mapping evidence remains safe. | Account mapping readiness controls UI action. | Unchanged. | Ready. | Add account role in implementation slice. |
| Supplier credit ownership | No supplier credit model exists. | Supplier credit/advance is a dedicated Billing/Finance aggregate, not a field on `SupplierPayment` and not a negative supplier invoice. | Future supplier advance aggregate. | Future advance handlers. | Dedicated source entity owns posting keys and balance. | Allows allocation against future supplier invoices. | Supports documents, references, events, audit. | Dedicated Finance workspace section. | Unchanged. | Ready. | Add `SupplierAdvance` core model. |
| Allocation to future invoice | Supplier payment allocation links to supplier invoices. | Advance allocation to future supplier invoices must be explicit and handler-owned. | Future advance allocation model. | Future allocation handler. | Allocation debits `AccountsPayable` and credits `SupplierAdvance` only after target invoice is posted. | Invoice aging reduces when advance is applied. | Event links advance and invoice. | UI shows available advance and applied amount. | Unchanged. | Ready after core advance. | Add allocation workflow with posting. |
| Partial application | Partial supplier payment is implemented. | Partial advance application is allowed when it does not exceed available advance or target open payable. | Future advance allocation service. | Future allocation handler. | Posts only applied amount. | Remaining advance balance remains open. | Evidence stores safe amounts and ids. | UI shows open advance balance. | Unchanged. | Ready. | Implement balance calculations. |
| Overpayment during invoice payment | Current payment handler rejects overpayment. | v1 implementation may support explicit split payment: payable portion to invoices and excess to supplier advance. It must not silently convert overpayment. | Supplier payment plus advance service. | Future split payment handler. | Posts AP settlement and advance creation as explicit lines or linked postings. | Keeps AP and advance aging separate. | Evidence links payment, invoices, and advance. | UI requires operator confirmation. | Unchanged. | Ready if complexity remains contained. | Prefer explicit split in core implementation if tests stay clear. |
| Standalone advance payment | No advance payment model exists. | Standalone supplier advance is allowed only through future advance workflow with supplier, currency, amount, payment method, and evidence. | Future supplier advance aggregate. | Future advance handlers. | Debit `SupplierAdvance`, credit `CashClearing`; bank settlement later clears cash. | No supplier invoice aging impact until allocation. | Bank/remittance documents are evidence only. | Internal Finance workflow. | Unchanged. | Ready. | Include in core model. |
| Bank reconciliation | Reconciliation is evidence only. | Bank reconciliation can support advance bank settlement, but it does not create or allocate advance by itself. | Bank reconciliation evidence. | Future bank settlement handler. | Settlement posting still uses finance posting service. | Reconciliation links bank line to advance/payment facts. | Evidence uses bank statement and match ids. | Read-only evidence links. | Unchanged. | Ready. | Reuse reconciliation pattern. |
| Returned advance transfer | Returned-transfer correction exists for bank-settled supplier payments. | Returned advance bank movement needs a future advance correction flow, not reuse of supplier payment correction. | Future advance correction model. | Future correction handler. | Reverses advance settlement or creation through formal journal entries. | Advance balance remains auditable. | Bank evidence required. | No status-only edit. | Unchanged. | Designed. | Add after advance core if needed. |
| Duplicate advance payment | Duplicate supplier payment is evidence/attention only. | Duplicate advance bank movement is evidence/attention first and must not auto-reverse. | Future advance correction model. | Future correction handler. | Correction must be explicit and journal-backed. | Prevents hidden supplier credit changes. | Evidence links duplicate bank movement. | Attention queue only before correction. | Unchanged. | Designed. | Reuse duplicate-payment guard pattern. |
| Supplier statement and aging | Supplier invoice due dates exist. | Advance balance appears as separate supplier balance, not negative AP. Aging reports show AP due, advance balance, and net supplier exposure separately. | Finance/payables reporting queries. | Read-only query handlers. | Reports derive from posted facts. | Netting is display/reporting unless allocation posts. | Evidence remains linked to postings. | Finance overview can show advance balance. | Unchanged. | Ready after core. | Add reporting with implementation. |
| Documents and remittance | `DocumentRecord` exists. | Advance remittance, supplier confirmations, and agreements use `DocumentRecord`; documents do not create advance balance. | Document foundation. | Document handlers. | No posting from document alone. | Supports audit and review. | Read-only document links. | Internal only. | Unchanged. | Ready. | Reuse document foundation. |
| External references | `ExternalReference` exists. | Supplier advance ids, bank ids, remote accounting ids, and import references use `ExternalReference`. | Integration foundation. | External reference service. | External ids do not decide accounting state. | Supports reconciliation with external systems. | Safe ids only. | Read-only display. | Unchanged. | Ready. | Reuse foundation. |
| Reversal and void | Supplier payment reversal and bank correction are formal. | Advance reversal must be formal and journal-backed. Draft advance can be cancelled; posted advance cannot be deleted or status-edited. | Future advance workflow policy. | Future reversal/correction handlers. | Reversal posts inverse entries. | Balance history is never rewritten. | Event/audit links original and reversal. | Row-version protected actions. | Unchanged. | Ready. | Include reversal boundary in core slice or next hardening slice. |
| Finance export | Export reads posted journal entries. | Advance creation, allocation, and reversal flow through existing posted journal export. Export package format remains unchanged. | Finance export services. | Existing export handlers. | No direct export from advance UI. | Export includes journal facts only. | Export UI unchanged. | Unchanged. | Ready. | Keep package format stable. |
| WebAdmin visibility | Finance workspace owns supplier payments and bank settlement. | Supplier advance v1 is internal/WebAdmin-only under Finance. It must not add journal editor shortcuts, customer payment/refund mutation, or public routes. | Finance WebAdmin and Application handlers. | Future advance controller actions. | UI calls advance handlers only. | Shows advance balance and postings read-only. | Internal operator workflow. | Unchanged. | Ready after model. | Add compact list/detail/workflow. |
| Public/mobile/storefront boundary | Supplier settlement is internal. | No public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund, or issued invoice snapshot change. | Internal Application and WebAdmin. | None outside internal/WebAdmin. | No customer-facing mutation. | No member-visible supplier balance. | Internal only. | Unchanged. | Locked. | Keep compatibility smoke lanes. |

## Locked Decisions

- Supplier advance and overpayment are formal Billing/Finance concepts, not accidental over-allocation on `SupplierPayment`.
- Current supplier payment overpayment guards remain active until a dedicated advance implementation exists.
- Future advance balance must use a dedicated `SupplierAdvance` account role mapped to an Asset account.
- Open payable must not become negative. Any excess payment is tracked as supplier advance, not negative AP.
- Supplier advance is not a negative supplier invoice, not a customer refund, not a document-only state, and not a manual journal shortcut.
- Advance creation, allocation to future supplier invoices, reversal, and bank settlement must use `FinancePostingService` with deterministic posting keys.
- Advance allocation to a supplier invoice is explicit and handler-owned. It is not automatic merely because a future invoice appears.
- Bank reconciliation and bank statement lines are evidence; they do not create, allocate, reverse, or settle advance by themselves.
- Duplicate or returned advance bank movement is evidence/attention first and requires a future formal correction handler before financial mutation.
- Finance export format remains unchanged because export reads posted `JournalEntry` facts.
- v1 is internal/WebAdmin-only. Public WebApi, mobile/member, storefront, customer invoice archive/download, issued invoice snapshots, and customer payment/refund flows remain unchanged.
- Bank API adapters remain blocked until real target banks, credential ownership, payload mapping, error contract, and smoke strategy are selected.

## Implementation Sequence

1. `Supplier Advance And Overpayment Boundary Design Slice`
   - Outcome: this document. Supplier advance ownership, account role, posting, allocation, aging, reconciliation, reversal, evidence, WebAdmin, and compatibility decisions are locked.
2. `Supplier Advance Core Model And Admin Slice`
   - Add formal `SupplierAdvance` and `SupplierAdvanceApplication` or equivalent Billing models, `FinancePostingAccountRole.SupplierAdvance`, internal handlers, WebAdmin list/detail/create/post/apply/cancel-draft, reporting projection, events/audit, migrations, tests, and compatibility guards.
   - Support standalone advance and explicit split payment only if validation and UX stay clear: payable allocation remains separate from advance creation.
   - Must not add bank API integration, public/mobile exposure, customer payment/refund reuse, customer invoice archive/download changes, or finance export format changes.
3. `Supplier Advance Reversal And Bank Correction Hardening`
   - Add full journal-backed reversal for posted unapplied advances and full journal-backed reversal for advance applications before reversing the advance itself.
4. `Purchasing Documents And Supplier Contacts`
   - Continue purchasing master data and document surfaces after advance/overpayment no longer has an unresolved finance boundary.

## Implementation Outcome

- Supplier advance core/admin is implemented in Billing/Finance.
- `SupplierAdvance` and `SupplierAdvanceApplication` are formal Billing models with PostgreSQL and SQL Server migrations, indexed business/supplier/status/date lookups, unique active advance numbering, and JSON metadata mapping.
- `FinancePostingAccountRole.SupplierAdvance`, `JournalEntryPostingKind.SupplierAdvancePosted`, `JournalEntryPostingKind.SupplierAdvanceApplied`, and `NumberSequenceDocumentType.SupplierAdvance` are available.
- Standalone advance posting is journal-backed: debit `SupplierAdvance`, credit `CashClearing`.
- Explicit application to a posted supplier invoice is journal-backed: debit `AccountsPayable`, credit `SupplierAdvance`.
- Current supplier payment overpayment guards remain active. The system does not silently convert invoice overpayment into credit or advance.
- WebAdmin Finance now exposes Supplier Advances with list, detail, create/update draft, post, cancel draft, and apply-to-invoice actions. All mutations are internal, anti-forgery protected, and row-version protected where lifecycle state changes.
- Supplier advance reversal hardening is implemented. Posted, unapplied advances can be reversed only through a balanced finance posting that debits `CashClearing` and credits `SupplierAdvance`; status-only reversal is not allowed.
- Supplier advance application reversal is implemented. Applied advance allocations can be reversed only through a balanced finance posting that debits `SupplierAdvance` and credits `AccountsPayable`, which reopens payable balance without deleting application history.
- Applied advances cannot be reversed directly. Operators must reverse active applications first, then reverse the advance if the full balance is open.
- Bank statement and reconciliation evidence support review, but they do not create, allocate, reverse, or settle advance outside the owning advance handlers.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund flows, and finance export package format remain unchanged.
- Next gate: `Purchasing Documents And Supplier Contacts`.
