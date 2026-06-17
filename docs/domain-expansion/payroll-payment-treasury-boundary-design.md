# Payroll Payment And Treasury Boundary Design

## Summary

This document locks the payroll salary-payment and treasury boundary after payroll posting. It is documentation-only: it adds no entity, migration, route, DTO, WebAdmin mutation, public/mobile/member/storefront contract, finance export package format change, statutory filing, provider adapter, customer payment/refund flow, supplier payment flow, or journal editor shortcut.

The core decision is that future salary payment is a formal HR/Payroll and Treasury workflow over posted payroll liabilities. It must not reuse customer `Payment`, customer `Refund`, supplier `SupplierPayment`, supplier advance, manual journal shortcuts, payroll run status text, attachment uploads, notes, or payslip artifacts as salary settlement.

## Current Darwin Payroll Payment And Treasury Findings

- `PayrollRun`, `PayrollRunLine`, and `PayrollRunLineComponent` exist in `HumanResources` and snapshot approved payroll-period, employee, contract, time, absence, and payroll-rule facts.
- Internal `PayrollPayslip` artifacts exist and are generated from approved payroll run snapshots through object storage and `DocumentRecord`. Payslips are employee evidence, not payment settlement.
- Approved payroll runs can be posted through `FinancePostingService` with `JournalEntryPostingKind.PayrollRunPosted`, deterministic posting key `payroll-run-posted:{payrollRunId}`, and role-based account mappings.
- Payroll posting debits payroll expense and employer payroll tax expense as applicable, then credits payroll payable, payroll tax payable, and social insurance payable. Posted `JournalEntry` remains the authoritative accounting evidence.
- `Payment` and `Refund` remain customer/order settlement records and must not be reused for employee salary payments.
- `SupplierPayment`, supplier payment reversal, bank settlement, supplier advance, `BankAccount`, `BankStatementImport`, `BankStatementLine`, and `BankReconciliationMatch` exist for supplier/treasury workflows. They provide useful patterns but do not own payroll salary settlement.
- `CashClearing`, `BankAccount.FinancialAccountId`, finance posting, bank statement evidence, and reconciliation evidence are available foundations for salary payment clearing. Payroll bank settlement is implemented as a journal-backed action that requires matched reconciliation evidence and a mapped bank Asset account.
- `BusinessEvent`, `AuditTrail`, `DocumentRecord`, and `ExternalReference` are available for lifecycle evidence, remittance documents, and external bank/accounting references.
- Public WebApi, mobile/member, storefront checkout, customer invoice archive/download, customer payment/refund flows, supplier finance flows, statutory filing, provider submission, and finance export package format remain unchanged.

## Decision Matrix

| Payroll payment surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Accounting/treasury impact | Evidence/document impact | WebAdmin/internal impact | Public/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Payment trigger | Payroll liability exists only after approved payroll run posting. | Salary payment can start only from a posted payroll run with `PostingJournalEntryId`. Approved but unposted payroll runs cannot be paid. | Payroll payment aggregate. | Payroll payment handlers. | Prevents settlement before liability exists. | Links to payroll run, posting journal, and safe payroll run metadata. | Internal HR/Finance workflow only. | Unchanged. | Implemented. | Continue with employee payslip self-service boundary design. |
| Payroll run linkage | `PayrollRun.PostingJournalEntryId` links liability posting. | Salary payments link to one posted payroll run and employee-level run lines or allocation records. | Payroll payment aggregate. | Payroll payment handlers. | Enables open payroll payable projection by run and employee. | Events include payroll run id and safe employee ids only. | Detail pages can show run, lines, and journal links. | Unchanged. | Designed. | Use immutable run snapshots. |
| Employee allocation | Payroll run lines store employee net pay. | Payment allocations should settle employee net-pay amounts from payroll run lines. Bulk payment can cover many employees, but allocation evidence remains employee-line based. | Payroll payment aggregate. | Payroll payment handlers. | Prevents losing employee-level open payable visibility. | Remittance evidence can link to employee allocation safely. | UI shows employee payment rows and totals. | Employee self-service remains separate. | Designed. | Implement `PayrollPaymentAllocation` or equivalent in core slice. |
| Partial payment | No salary payment model exists. | Partial salary payment is allowed only when allocation sums do not exceed each employee line net pay. Open payroll payable remains visible for unpaid amounts. | Payroll payment service. | Payroll payment handlers. | Posts only the paid salary amount. | Partial status is evidence-backed, not payroll run status-only. | Operators can pay a run in batches. | Unchanged. | Designed. | Add cumulative guards in core slice. |
| Overpayment and advances | Supplier advance exists, payroll advance does not. | Employee salary overpayment or advance is blocked until a dedicated payroll advance/employee receivable design exists. | Future payroll advance boundary. | No current mutation owner. | Avoids silently creating employee receivables or negative payroll payable. | Any exception must be explicit evidence later. | UI must block overpayment. | Unchanged. | Blocked by policy. | Design payroll advance separately if required. |
| Payment method | Treasury foundations exist. | v1 supports internal payment method metadata such as bank transfer, cash, card, direct debit, and other, but method does not settle liability without finance posting. | Payroll payment aggregate. | Payroll payment handlers. | Payment method informs evidence, not accounting by itself. | External payment ids use `ExternalReference`. | No bank credential form in payroll UI. | Unchanged. | Ready. | Add enum in implementation. |
| Payroll payable clearing | Payroll posting credits payroll payable. | Salary payment posting debits `PayrollPayable` and credits `CashClearing`. Bank settlement later debits `CashClearing` and credits mapped bank Asset account. | Finance posting service plus payroll payment handler. | Payroll payment posting handler. | Keeps payroll clearing aligned with treasury foundation. | Deterministic posting keys and audit evidence. | UI shows read-only journal links. | Unchanged. | Ready. | Add account role validation if missing. |
| Tax and social insurance payment | Payroll posting credits payroll tax payable and social insurance payable. | Salary payment settles employee net pay only. Tax authority and social insurance remittance require separate statutory payment/provider boundaries. | Future statutory payment/provider boundary. | Dedicated statutory handlers later. | Prevents employee salary payment from clearing tax/social liabilities. | Filing/payment receipts use documents and external references later. | Not shown as salary payment action. | Unchanged. | Designed. | Keep provider adapter separate. |
| Bank settlement | Bank account, statement, and reconciliation foundations exist. | Salary payment first posts to `CashClearing`; bank settlement is now implemented only from matched reconciliation evidence with a mapped bank Asset account. | Payroll bank settlement handler. | Payroll/treasury settlement handler. | Debits `CashClearing` and credits the mapped bank Asset account without bank API or credential handling. | Bank statement and reconciliation evidence remain separate records; settlement links to the reconciliation and journal. | WebAdmin shows candidate reconciliation matches and settlement journal links. | Unchanged. | Complete for current phase. | Design returned-transfer correction. |
| Reversal and correction | Payroll payment full reversal exists. | Posted salary payment cannot be cancelled by status edit. Full-payment reversal v1 is journal-backed; allocation-level correction remains blocked until a separate design exists. | Payroll payment aggregate. | Payroll payment reversal handler. | Reversal posts inverse clearing entry and preserves original history. | Reason is required; bank evidence is required later for bank-settled corrections. | UI shows reversal on posted payroll payment detail. | Unchanged. | Complete for current phase. | Add bank-settled correction later. |
| Failed or returned bank movement | Bank correction patterns exist for supplier settlement. | Returned salary transfer is not modeled by editing payroll payment status. It needs bank statement evidence, reconciliation linkage, and journal-backed correction. | Future payroll bank correction boundary. | Payroll/treasury correction handlers. | Avoids rewriting salary payment history. | Evidence comes from bank statement/reconciliation, not manual reason alone. | Attention workflow later. | Unchanged. | Needs dedicated design. | Build after payment and bank settlement boundaries. |
| Payslip relation | Payslips are immutable artifacts. | Payslips can display payment status later only through additive read models; payslip artifact generation does not create or settle payment. | Payroll read model. | No mutation from payslip service. | No accounting impact. | Payslip remains generated document evidence. | Download remains internal in current phase. | Employee self-service later only by design. | Designed. | Keep template/PDF completion separate. |
| Remittance evidence | `DocumentRecord` exists. | Salary remittance files, bank confirmations, and employee payment documents use `DocumentRecord`; they do not replace posting or settlement. | Document service plus payroll payment service. | Document handlers. | Documents do not create journal entries. | Retention and privacy classification are required. | Internal visibility only in v1. | Unchanged. | Ready. | Reuse document foundation. |
| External bank/accounting identity | `ExternalReference` exists. | Bank batch ids, payment ids, payroll provider ids, accounting receipt ids, and import ids use `ExternalReference`, not provider-specific columns. | Integration foundation. | External reference service. | External ids do not decide settlement state. | Metadata must remain secret-free. | Read-only display later. | Unchanged. | Ready. | Add references when integration exists. |
| Privacy boundary | HR records and payroll are sensitive. | Payroll payment payloads must store only safe ids, dates, statuses, amounts, currency, jurisdiction, payment method, and business/employee references. | Payroll payment handlers. | Handler-owned only. | Finance export sees journal entries, not private HR payloads. | No bank account numbers, medical data, credentials, or raw provider payloads. | Operator-only visibility. | Unchanged. | Locked. | Enforce in validators and tests. |
| WebAdmin visibility | HR and Finance workspaces exist. | Payroll payment v1 is internal/WebAdmin-only. HR can initiate payroll payment review; Finance/Treasury owns bank evidence and clearing views. | HR/Payroll plus Finance/Treasury. | Payroll payment and treasury handlers. | No manual journal shortcut from payment UI. | Shows posting and bank evidence read-only. | Compact operator workflow. | Unchanged. | Designed. | Build complete UI in core slice. |
| Public/mobile/storefront boundary | Payroll is not a public/member commerce surface. | No public WebApi, mobile/member, or storefront salary payment exposure is added in v1. Employee payslip/payment self-service requires a separate additive design. | Internal application and WebAdmin. | None outside internal/WebAdmin. | No customer payment impact. | No member download impact. | Internal only. | Unchanged. | Locked. | Keep compatibility smoke lanes. |

## Locked Decisions

- Future `PayrollPayment` is a formal HR/Payroll and Treasury settlement workflow. It does not reuse customer `Payment`, customer `Refund`, supplier `SupplierPayment`, supplier advance, payroll run status text, manual journal shortcuts, document uploads, notes, or payslip artifacts.
- Salary payment is allowed only for posted payroll runs with `PostingJournalEntryId`. Approved but unposted payroll runs cannot be paid.
- Employee-level allocation is required so open payroll payable can be reconciled per payroll run line and employee. Bulk payment can exist, but it must not hide employee allocation evidence.
- Partial salary payment is allowed if cumulative paid amounts do not exceed each payroll run line net pay. Overpayment and employee advance remain blocked until a dedicated payroll advance or employee receivable boundary exists.
- v1 salary payment posting debits `PayrollPayable` and credits `CashClearing`. Bank settlement debits `CashClearing` and credits the mapped bank Asset account only after matched reconciliation evidence is selected.
- Salary payment clears employee net pay only. Payroll tax, social insurance, statutory remittance, and provider filing require separate statutory payment/provider boundaries.
- Posted salary payment cannot be cancelled or rewritten by status edit. Correction requires a journal-backed reversal/correction handler and preserved original history.
- Returned or failed bank movement is evidence-backed and must not be modeled by editing salary payment status. It requires bank statement or reconciliation evidence plus formal posting/reversal policy; this remains the next boundary.
- Payslip generation and salary payment remain separate. Payslip artifacts may later display payment state through read models, but they do not create payment settlement.
- Remittance documents use `DocumentRecord`. Bank, accounting, payroll-provider, and target-side ids use `ExternalReference`.
- Provider credentials, bank credentials, access tokens, private keys, connection strings, raw provider payloads, raw bank payloads, private document contents, medical details, and raw HR import files must not be stored in metadata, document metadata, events, audit trails, external references, logs, tests, or documentation.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, statutory filing, provider submission, and invoice archive/download contracts remain unchanged in this design step.

## Implementation Sequence

1. `Payroll Payment And Treasury Boundary Design`
   - Outcome: complete. Salary payment, employee allocation, payable clearing, treasury evidence, reversal, failed transfer, privacy, documents, external references, and public/mobile boundaries are locked before implementation.
2. `Payroll Payment Core Model And Admin Slice`
   - Outcome: complete. Formal internal `PayrollPayment` and `PayrollPaymentAllocation` records exist over posted payroll runs and run lines, with create/update/post/cancel-draft, cumulative payment guards, payroll payable clearing, WebAdmin list/detail/create/edit/post/cancel, and source guards.
3. `Payroll Payment Reversal Boundary/Core Slice`
   - Outcome: complete. Posted payroll payments can be fully reversed through journal-backed correction that debits `CashClearing`, credits `PayrollPayable`, stores reason and reversal journal linkage, and preserves original payment history.
4. `Payroll Payment Bank Settlement Boundary/Core Slice`
   - Outcome: complete. Posted payroll payments can be linked to matched bank reconciliation evidence and clear `CashClearing` to mapped bank Asset accounts through `JournalEntryPostingKind.PayrollPaymentBankSettled`. Bank-settled payments are blocked from simple reversal until returned-transfer correction is designed.
5. `Payroll Returned Transfer And Correction Boundary Slice`
   - Outcome: complete. Failed or returned salary transfer correction must be evidence-backed by bank statement/reconciliation facts, full-settlement v1, journal-owned, and represented by a separate correction model rather than payroll payment status edits.
6. `Payroll Returned Transfer And Correction Core Slice`
   - Outcome: complete. `PayrollPaymentBankCorrection` exists with internal WebAdmin workflow, full-settlement returned-transfer posting, duplicate salary bank movement attention, row-version checks, migrations, and source guards.
7. `Payroll Payslip Template/PDF Completion Slice`
   - Outcome: complete. Formal versioned PDF payslips are generated over immutable payslip artifact data with retained HTML source artifacts. This remains separate from salary payment.
8. `Payroll Provider Adapter Design Slice`
   - Outcome: complete. Statutory filing/provider submission remains blocked until real targets, credential owner, payload mapping, safe error contract, and smoke strategy are selected.
9. `Employee Payslip And Payroll Self-Service Boundary Design`
   - Outcome: complete. Employee-facing payslip and safe payroll payment visibility boundaries are locked for a dedicated additive public/mobile/member contract with privacy and download tests.

## Implementation Outcome

- Payroll payment and treasury boundaries are decision-complete, the core internal payment model/admin workflow has been implemented, full-payment reversal is implemented, bank settlement from matched reconciliation evidence is implemented, and returned-transfer correction core is implemented.
- Payroll posting remains the accounting source for payroll payable liability.
- Salary payment is explicitly separated from customer payment/refund, supplier payment, supplier advance, statutory filing, provider submission, payslip artifacts, and manual journal shortcuts.
- Formal payslip template/PDF completion is complete for the current phase.
- Payroll provider adapter boundary design is complete for the current phase.
- Employee payslip self-service core is complete for the current phase. The next gate is `AI-Readiness And Automation Governance`.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, statutory filing, provider submission, and invoice archive/download behavior remain unchanged.

## Compatibility Guards

- Do not create salary payment from `PayrollPeriod`, unposted `PayrollRun`, payslip artifact, personnel document, note, or generic metadata.
- Do not reuse customer `Payment`, customer `Refund`, supplier `SupplierPayment`, supplier advance, supplier bank settlement, or supplier correction models for salary payment.
- Do not create manual journal entry shortcuts from Payroll Payment UI. Payroll payment posting must use the owning payroll payment handler and finance posting service.
- Do not clear payroll tax payable or social insurance payable through employee salary payment. Statutory remittance needs its own boundary.
- Do not add bank credential UI, provider credential UI, bank API integration, statutory provider submission, employee self-service, public/mobile routes, finance export package changes, or invoice archive/download behavior in the salary payment or bank settlement slices.
- Do not store private employee bank account numbers, provider credentials, tokens, raw bank payloads, raw provider payloads, medical details, private document contents, or connection strings in metadata, events, audit trails, external references, tests, or documentation.

## Documentation Verification

- `docs/README.md` links this document from the ERP expansion map.
- `BACKLOG.md` and `erp-expansion-master-status.md` record employee payslip self-service core as complete and move the next gate to `AI-Readiness And Automation Governance`.
- This document contains no deferred ambiguous decisions for the payroll payment boundary.
- Restricted vendor/source scans must return no output.
