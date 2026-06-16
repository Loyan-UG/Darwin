# Supplier Payment Reversal And Bank/Treasury Boundary Design

## Summary

This document locks the boundary for supplier payment reversal, bank account ownership, treasury clearing, reconciliation, overpayment or advance handling, remittance evidence, bank settlement, and finance export impact. It began as documentation-only and now records that the focused supplier payment reversal core, bank/treasury foundation, bank reconciliation core, and supplier payment bank settlement boundary design have been completed. These steps add no bank API integration, direct bank settlement implementation, public WebApi route, mobile/member contract, storefront behavior, customer payment/refund flow change, finance export package format change, or journal editor shortcut.

The core decision is that a posted `SupplierPayment` cannot be corrected by status edit, delete, or cosmetic void. Any correction after posting must be a formal finance reversal that references the original payment and posts balanced accounting entries. Direct bank settlement must follow [supplier-payment-bank-settlement-boundary-design.md](supplier-payment-bank-settlement-boundary-design.md): it can only be future internal, evidence-backed, limited to posted supplier payments, and linked to bank reconciliation evidence.

## Current Darwin Supplier Payment And Treasury Findings

- `SupplierPayment` and `SupplierPaymentAllocation` exist in Billing. Draft payments can be cancelled; posted payments can be corrected only by full-payment accounting reversal.
- Posted supplier payments become authoritative through `JournalEntryPostingKind.SupplierPaymentPosted`, debiting `AccountsPayable` and crediting `CashClearing`.
- `CashClearing` is the v1 clearing account. Darwin now has canonical `BankAccount`, `BankStatementImport`, `BankStatementLine`, `BankReconciliationMatch`, and `BankReconciliationMatchLine` models for bank identity, statement evidence, and reconciliation evidence.
- Customer `Payment` and customer `Refund` remain customer/order settlement records and are not reused for supplier settlement, reversal, or bank reconciliation.
- `JournalEntry`, `JournalEntryLine`, `FinancePostingService`, `FinancePostingAccountMapping`, `AccountsPayable`, `CashClearing`, and `JournalEntryPostingKind.Reversal` exist.
- `DocumentRecord`, `ExternalReference`, `BusinessEvent`, and `AuditTrail` exist for remittance evidence, external bank/accounting ids, lifecycle history, and audit evidence.
- Finance export reads posted journal entries and stored export packages. It must not export supplier payment UI state, remittance documents, or bank metadata directly.
- Supplier payment bank settlement boundary design is complete. Reconciliation evidence can support future settlement, but it cannot rewrite supplier payments, customer payments, refunds, journal entries, or finance export history.
- Public WebApi, mobile/member, storefront checkout, customer invoice archive/download, issued invoice snapshots, customer payment/refund flows, and finance export package format remain unchanged.

## Decision Matrix

| Boundary surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Accounting impact | Treasury/bank impact | Evidence/export impact | Public/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Posted payment reversal | Posted `SupplierPayment` stores posting journal linkage and, after reversal, reversal journal linkage. | Posted payment correction creates a formal full-payment reversal entry linked to the original payment. Status edit, delete, and cosmetic void are forbidden. | Supplier payment reversal handler plus finance posting service. | Supplier payment reversal handler only. | Debit `CashClearing`, credit `AccountsPayable` for the reversed amount, using balanced journal posting. | Does not prove external bank return. | Reversal event/audit references original payment and original journal entry. Export reads the posted reversal entry. | Unchanged. | Implemented. | Design bank/treasury before direct bank settlement or returned-transfer handling. |
| Draft cancellation | Draft supplier payment can be cancelled today. | Draft cancellation remains status-only because no journal entry exists. | Supplier payment workflow. | Existing supplier payment handler. | No posting. | No bank impact. | Audit records cancellation. | Unchanged. | Implemented. | Keep current behavior. |
| Bank account ownership | No canonical bank account model exists. | Bank accounts need a treasury foundation before direct bank settlement or reconciliation. | Future treasury model. | Future treasury handlers. | Bank account does not replace `FinancialAccount`; it maps operational bank identity to accounting accounts. | Required before bank statement import or direct settlement. | External ids use `ExternalReference`. | Unchanged. | Not implemented. | Design bank/treasury foundation before bank settlement. |
| Cash clearing vs bank ledger | `CashClearing` is an account role. | Supplier payment v1 posts to `CashClearing`; real bank movement is outside the current settlement model. | Finance posting service. | Supplier payment posting handler. | Keeps AP settlement accounting consistent. | Bank ledger later clears `CashClearing` to bank account evidence. | Export includes clearing entries. | Unchanged. | Implemented for v1. | Preserve until treasury foundation exists. |
| Bank reconciliation | No bank statement or reconciliation model exists. | Reconciliation must compare bank statement facts to posted finance entries and external references; it must not mutate supplier payment history directly. | Future treasury/reconciliation service. | Future reconciliation handlers. | Reconciliation can create adjustment/reversal entries only through finance posting. | Requires bank statement source and matching policy. | Audit records match, mismatch, and operator decisions. | Unchanged. | Not implemented. | Design with bank/treasury foundation. |
| Overpayment and supplier advance | Current supplier payment blocks cumulative overpayment. | Overpayment and advances remain blocked until supplier credit/advance account policy is designed. | Future payables/treasury policy. | Future supplier advance handler. | Advance requires dedicated accounting role, not AP over-allocation. | Can later reconcile to bank movement. | Evidence must distinguish payment against invoice from supplier advance. | Unchanged. | Not implemented. | Design overpayment/advance before enabling. |
| Underpayment and open payable | Partial supplier payment and full-payment reversal exist. | Underpayment remains partial settlement; open payable is derived from posted invoice gross less posted supplier payments that have not been reversed. | Payables projection. | Query/read model handlers. | Reversal reopens payable by excluding the reversed payment from paid totals. | No bank-specific behavior. | Aging/export use posted journal facts. | Unchanged. | Implemented for payment and reversal. | Add richer aging projection only as a separate reporting slice. |
| Remittance evidence | `DocumentRecord` exists. | Remittance advice, bank confirmation, and payment files are evidence only; they do not create settlement, reversal, or reconciliation. | Document service plus supplier payment service. | Document registration handlers. | Documents cannot post accounting entries. | Documents can support review. | Stored metadata remains secret-free and internal. | Unchanged. | Ready. | Expose only when document workflow is designed. |
| External bank/accounting references | `ExternalReference` exists. | Bank transfer ids, accounting receipt ids, statement ids, and remote reversal ids use `ExternalReference`, not provider-specific columns. | Integration foundation. | External reference service. | External ids do not decide posting state. | Bank ids support reconciliation after treasury design. | Safe display ids only; no raw payload or credentials. | Unchanged. | Ready. | Add references only with concrete integration or import flow. |
| Failed bank transfer | No bank delivery model exists. | Failed transfer cannot be represented as supplier payment reversal until bank/treasury source and error contract exist. | Future treasury service. | Future bank integration handlers. | Accounting correction requires explicit reversal or adjustment. | Requires bank delivery status owner. | Failure evidence must be safe and provider-neutral. | Unchanged. | Not implemented. | Design bank delivery/import before modeling failures. |
| Duplicate or returned payment | Current supplier payment guards duplicate allocation/posting by payment id. | Duplicate or returned external payment must be handled by treasury/reconciliation rules, not by editing the original posted payment. | Future treasury/reconciliation service. | Future reconciliation handlers. | Correction uses reversal/adjustment postings. | Requires bank statement or bank event evidence. | Audit links original payment, bank evidence, and correction. | Unchanged. | Not implemented. | Include in bank/treasury foundation. |
| Finance export impact | Export reads posted journal entries. | Reversal entries export through the existing journal-entry package path. Export format remains unchanged. | Finance export services. | Existing export handlers. | No direct export from supplier payment UI or documents. | Bank metadata is not exported unless it is part of posted journal evidence. | Stored packages remain source of export. | Unchanged. | Ready. | Keep export source as journal entries. |
| WebAdmin/internal visibility | Finance workspace exposes supplier payments. | UI exposes `Reverse payment` only for posted payments and keeps bank operations unavailable until treasury foundation exists. | Finance WebAdmin. | Supplier payment reversal handler for reversal only. | UI cannot edit posted accounting history. | UI cannot fake bank settlement. | Shows original and reversal journal links read-only. | Unchanged. | Implemented for reversal. | Keep bank UI blocked until treasury foundation. |
| Public/mobile/storefront boundary | Public/member commerce is customer order/invoice oriented. | Supplier payment reversal, bank settlement, and treasury remain internal. | Internal application and WebAdmin. | None outside internal/WebAdmin. | No customer-facing accounting mutation. | No mobile bank operation. | No customer archive/download impact. | Unchanged. | Locked. | Keep compatibility smoke lanes. |

## Locked Decisions

- Posted `SupplierPayment` cannot be corrected by status edit, delete, or cosmetic void.
- A supplier payment reversal must be a formal finance action that creates a balanced reversal journal entry linked to the original payment and original posting journal entry.
- Reversal accounting uses the inverse of v1 supplier payment posting: debit `CashClearing` and credit `AccountsPayable` for the reversed amount.
- Draft cancellation remains allowed because no finance posting exists.
- Direct bank settlement, bank transfer failure, returned transfer, bank statement import, and reconciliation require a bank/treasury foundation before implementation.
- `CashClearing` remains the v1 settlement boundary until a canonical treasury model is designed.
- Overpayment and advance payment stay blocked until supplier credit/advance account policy is designed.
- Remittance evidence uses `DocumentRecord`; it does not create, reverse, or reconcile settlement.
- Bank, accounting, import, statement, and target-side ids use `ExternalReference`.
- Reversal lifecycle evidence uses deterministic `BusinessEvent` and `AuditTrail`.
- Finance export package format remains unchanged. Supplier payment reversals flow through posted `JournalEntry` records.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, issued invoice snapshots, customer payment/refund flows, and finance export package compatibility remain unchanged.

## Implementation Sequence

1. `Supplier Payment Reversal And Bank/Treasury Boundary Design`
   - Outcome: this document. Reversal, bank ownership, treasury clearing, reconciliation, overpayment/advance, remittance evidence, and export boundaries are locked before implementation.
2. `Supplier Payment Reversal Core Slice`
   - Outcome: completed. Reversal is full-payment only, uses balanced finance posting, stores original and reversal journal links, records deterministic event/audit evidence, and keeps open payable calculations from counting reversed payments as paid.
   - Bank transfer failure, returned payment, direct bank settlement, overpayment/advance, partial reversal, customer payment/refund reuse, public/mobile exposure, and finance export format changes remain outside this slice.
3. `Bank/Treasury Foundation Design Slice`
   - Outcome: completed in [bank-treasury-foundation-design.md](bank-treasury-foundation-design.md). Bank account ownership, cash clearing, bank statement import, bank transaction identity, reconciliation matching, returned transfer handling, duplicate payment handling, external identity, remittance evidence, supplier advance/overpayment, and compatibility boundaries are locked.
4. `Bank/Treasury Foundation Core Model And Admin Slice`
   - Outcome: completed. Internal bank account master, bank statement imports, and statement lines exist without direct bank settlement, returned-transfer automation, supplier advance/overpayment, public/mobile exposure, or finance export format changes.
5. `Bank Reconciliation Core Slice`
   - Outcome: completed. Reconciliation matches link bank evidence to posted or reversed finance facts and remain evidence-only.
6. `Supplier Payment Bank Settlement Boundary Design`
   - Outcome: completed in [supplier-payment-bank-settlement-boundary-design.md](supplier-payment-bank-settlement-boundary-design.md). Future settlement is limited to posted supplier payments, must link to reconciliation evidence, and cannot mutate payment, journal, refund, or export history by status-only edits.

## Next Implementation Slice

`Supplier Payment Bank Settlement Core Slice`

The next implementation gate is the internal evidence-backed supplier payment bank settlement core. Supplier payment reversal is complete at the accounting boundary, bank statement and reconciliation evidence exist, and direct settlement remains blocked unless it is implemented through the locked settlement boundary without bank API integration, returned-transfer automation, supplier advance/overpayment, public/mobile exposure, or finance export format changes.
