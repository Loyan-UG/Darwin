# Payroll Returned Transfer And Correction Boundary Design

## Summary

This document locks the boundary for returned, failed, or duplicate salary bank movement after payroll payment bank settlement. The boundary step was documentation-only, and the follow-up core implementation now adds only the internal payroll correction model and WebAdmin workflow. It adds no public/mobile/member/storefront contract, finance export package format change, bank API integration, payroll-provider submission, customer payment/refund flow, supplier payment flow, or journal editor shortcut.

The core decision is that bank-settled payroll payment correction must be evidence-backed and journal-owned. A returned salary transfer cannot be represented by editing `PayrollPayment` status, deleting payment history, rewriting bank settlement journals, changing payroll run status, changing payslip artifacts, or changing finance export history. Duplicate bank movement is evidence and attention first; any financial correction must go through a dedicated handler-owned correction workflow.

## Current Darwin Payroll Returned Transfer Findings

- `PayrollPayment` and `PayrollPaymentAllocation` exist in `HumanResources` as internal salary settlement records over posted payroll runs and payroll run lines.
- Posted payroll payment debits `PayrollPayable` and credits `CashClearing` through `JournalEntryPostingKind.PayrollPaymentPosted`.
- Full-payment payroll reversal exists before bank settlement. It debits `CashClearing`, credits `PayrollPayable`, stores reversal reason and journal linkage, and preserves original payment history.
- Payroll bank settlement exists for posted unreversed payroll payments. It requires matched zero-difference `BankReconciliationMatch` evidence linked to the payroll payment posting and a `BankAccount` mapped to an Asset `FinancialAccount`.
- Bank settlement posts `JournalEntryPostingKind.PayrollPaymentBankSettled`: debit `CashClearing`, credit mapped bank Asset account. It stores settlement timestamp, settlement journal link, reconciliation match link, and safe settlement notes.
- Bank-settled payroll payments are blocked from simple reversal. Correction after bank settlement uses `PayrollPaymentBankCorrection` with bank evidence and its own formal correction workflow.
- `BankAccount`, `BankStatementImport`, `BankStatementLine`, `BankReconciliationMatch`, and `BankReconciliationMatchLine` exist in Billing/Treasury.
- Bank reconciliation is evidence. It does not rewrite `PayrollPayment`, customer `Payment`, `Refund`, supplier finance records, `JournalEntry`, payslip artifacts, or finance export history.
- `BusinessEvent`, `AuditTrail`, `DocumentRecord`, and `ExternalReference` are available for lifecycle evidence, returned-transfer documents, and external bank/accounting ids.
- Public WebApi, mobile/member, storefront checkout, customer invoice archive/download, customer payment/refund flows, supplier finance flows, statutory filing, provider submission, and finance export package format remain unchanged.

## Decision Matrix

| Correction surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Accounting impact | Evidence impact | WebAdmin impact | Public/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Returned or failed salary transfer trigger | Bank-settled payroll payment links to matched reconciliation evidence. | Trigger must come from bank statement or reconciliation evidence, not operator reason alone. | `PayrollPaymentBankCorrection` plus treasury evidence. | Payroll correction handlers. | No status-only accounting change. | Requires bank statement line and reconciliation linkage. | Internal correction action is implemented. | Unchanged. | Implemented for v1. | Continue with employee payslip self-service boundary design. |
| Settlement reversal boundary | Settlement journal debits `CashClearing` and credits bank Asset. | v1 correction reverses or adjusts the settlement clearing entry only; original payroll payment history remains. | Finance posting service plus payroll correction model. | Payroll correction handler. | Debit/credit correction is journal-backed and idempotent. | Correction links original settlement journal. | UI shows correction evidence and journal links. | Unchanged. | Implemented. | Keep partial correction blocked until allocation policy. |
| Payroll payment state impact | Payroll payment can be posted, reversed, or bank-settled. | Payment is not deleted, rewritten, or changed by status-only edit. Bank-settled payment remains historical salary payment evidence. | Payroll payment aggregate plus `PayrollPaymentBankCorrection`. | Payroll correction handler. | Payroll payable and bank clearing history remain intact. | Correction record explains bank failure or duplicate movement. | Read-only payment timeline plus correction action. | Unchanged. | Implemented. | Continue with employee payslip self-service boundary design. |
| Full vs partial correction | Settlement v1 is full-payment. | v1 returned-transfer correction is full-settlement only. Partial returned amount and allocation-level bank correction are blocked. | Payroll correction policy. | Payroll correction handler. | Avoids ambiguous employee net-pay aging and split clearing. | Partial evidence can be recorded as attention but not posted as partial correction. | UI does not offer partial correction. | Unchanged. | Locked and implemented for v1. | Design partial correction later only with allocation policy. |
| Employee allocation impact | Payroll payments allocate to payroll run lines and employees. | Correction does not mutate allocation history. Open salary payable impact is produced only by journal-backed correction and future read projections. | Payroll correction model plus finance posting. | Payroll correction handler. | Original employee allocation remains evidence of intended payment. | Correction links payment-level evidence safely. | UI shows payment correction evidence. | Unchanged. | Implemented for payment-level correction. | Allocation-level correction remains separate. |
| Bank statement evidence | `BankStatementLine` has normalized identity and safe statement facts. | Bank statement line is the primary source fact for returned, failed, or duplicate salary movement evidence. | Bank statement import model. | Bank statement import and payroll correction handlers. | Statement alone does not post accounting. | Evidence references normalized statement identity. | UI links to statement line. | Unchanged. | Ready. | Use statement line in correction model. |
| Reconciliation linkage | `BankReconciliationMatch` links statement evidence to posted finance facts. | Correction must link to the reconciliation that proves the returned or duplicate movement. | Bank reconciliation model. | Payroll correction handler. | Reconciliation itself still does not mutate payroll payment or journals. | Correction references reconciliation evidence. | UI links correction to reconciliation. | Unchanged. | Implemented. | Keep reconciliation evidence read-only. |
| Duplicate bank movement detection | Statement line identity and reconciliation evidence exist. | Duplicate salary payment is evidence/attention first, not automatic reversal. | Bank statement and correction query. | Payroll correction handler for attention record only. | No automatic journal reversal from detection alone. | Duplicate evidence uses statement identity and payroll payment linkage. | UI can record duplicate attention. | Unchanged. | Implemented for evidence/attention. | Add automation only after a separate design. |
| Duplicate payment correction | `PayrollPaymentBankCorrection` exists. | Duplicate bank movement is attention/evidence only in v1; auto-reversal is forbidden. | Payroll bank correction model. | No posting owner for duplicate attention in v1. | No journal entry is created for duplicate attention. | Operator decision and bank evidence are retained. | UI records correction evidence but cannot post duplicate attention. | Unchanged. | Implemented for v1. | Design a dedicated duplicate financial correction only if needed later. |
| Correction model ownership | Payroll payment has settlement fields only. | Use `PayrollPaymentBankCorrection`, not ad hoc fields on `PayrollPayment` and not event-only state. | HumanResources/Treasury correction model. | Payroll correction handlers. | Supports multiple corrections and audit without polluting payment history. | Each correction owns reason, evidence, status, and journal linkage. | UI lists corrections per payroll payment. | Unchanged. | Implemented. | Continue with employee payslip self-service boundary design. |
| Journal posting and reversal impact | `JournalEntry` is authoritative. | Correction must create balanced, idempotent journal entries and never rewrite existing journals or exports. | Finance posting service. | Payroll correction handler. | Export continues to read posted journal entries. | Correction event links source journals. | Read-only journal links after correction. | Unchanged. | Implemented. | Keep export format unchanged. |
| Payslip relation | Payslip artifacts are immutable payroll evidence. | Returned-transfer correction does not edit payslip artifacts. Future read models may display payment/correction status without regenerating legal artifacts. | Payroll read model later. | No mutation from payslip service. | No payslip-driven accounting impact. | Payslip remains evidence of payroll calculation, not bank movement. | UI stays separate. | Employee self-service unchanged. | Locked. | Keep payslip template/PDF separate. |
| Remittance and document evidence | `DocumentRecord` exists. | Bank return notices, remittance proof, and employer communication documents are documents only; they cannot trigger financial correction alone. | Document service plus correction model. | Document registration handlers. | Documents do not post accounting. | Documents support review and audit. | Read-only attachments after implementation. | Unchanged. | Foundation ready. | Reuse document foundation later. |
| External bank and accounting ids | `ExternalReference` exists. | Bank return ids, duplicate reference ids, remote transaction ids, payroll-provider ids, and accounting ids use `ExternalReference`. | External reference foundation. | External reference service. | External ids do not decide posting state. | Store safe ids and display ids only. | Read-only references only. | Unchanged. | Ready. | Reuse existing foundation. |
| Payroll advance and employee receivable boundary | Payroll overpayment and employee advance are blocked. | Advance or overpayment remains a future dedicated slice with account roles, lifecycle, employee receivable or advance policy, privacy rules, and reconciliation policy. | Future payroll advance/employee receivable boundary. | No current mutation owner. | No hidden employee receivable or negative payroll payable is created. | Any advance evidence must be distinct from salary settlement. | No advance UI in this correction slice. | Unchanged. | Not ready. | Document future dedicated slice. |
| Bank API target boundary | Bank API target is not selected. | No bank API adapter is implemented now. Future design should choose real targets, preferably widely used large German banks, with credential owner, payload mapping, error contract, and smoke strategy. | Future bank API adapter design. | Future adapter handlers. | API responses must not bypass posting/correction handlers. | Raw bank payload and credentials stay out of metadata. | No bank credential/config UI now. | Unchanged. | Conditional. | Design only after target selection. |
| WebAdmin/internal visibility | HR and Finance workspaces exist. | v1 correction UI must be internal, evidence-backed, full-settlement, row-version protected, and handler-owned. | HR WebAdmin plus treasury links. | Future correction controller action. | UI cannot edit journal history. | UI shows evidence and correction status. | Internal only. | Unchanged. | Ready after core model. | Implement correction UI with core. |
| Public/mobile/storefront boundary | Payroll is internal HR/finance data. | No public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund, supplier finance mutation, or finance export format change. | Internal Application and WebAdmin. | None outside internal/WebAdmin. | No customer-facing mutation. | No member-visible bank correction. | No public UI. | Unchanged. | Locked. | Keep compatibility smoke lanes. |

## Locked Decisions

- Returned or failed salary transfer is not represented by status-only edit on `PayrollPayment`.
- v1 correction for bank-settled payroll payment is full-settlement only.
- v1 correction reverses or adjusts the bank settlement clearing journal while original payroll payment history remains intact.
- Partial returned transfer, allocation-level bank settlement correction, and partial bank reversal remain blocked until a separate payroll allocation policy is designed.
- Duplicate salary bank movement is not auto-reversed. The system records evidence and attention, then correction is performed only through an explicit handler-owned workflow.
- Correction needs a separate future model such as `PayrollPaymentBankCorrection` so multiple correction events and evidence remain auditable.
- Bank statement and reconciliation evidence are required. Operator reason alone is not sufficient for financial correction.
- Payslip artifacts are not regenerated or rewritten by returned-transfer correction.
- Payroll advance and employee receivable remain blocked and are recorded as future dedicated slices, not part of returned-transfer correction.
- Bank API adapter implementation is blocked until real targets, credential ownership, payload mapping, error contract, and smoke strategy are selected. Future target selection should consider widely used large German banks.
- Credentials, access tokens, refresh tokens, private keys, connection strings, raw bank payloads, provider secrets, medical details, private document contents, and raw HR import files must not be stored in metadata, documents, external references, events, logs, tests, or documentation.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund flows, supplier finance flows, statutory filing, provider submission, and finance export package format remain unchanged.

## Implementation Sequence

1. `Payroll Returned Transfer And Correction Boundary Slice`
   - Outcome: this document. The correction boundary is locked before schema, WebAdmin action, or posting changes.
2. `Payroll Returned Transfer And Correction Core Slice`
   - Outcome: complete. `PayrollPaymentBankCorrection` exists in HumanResources with full-settlement returned-transfer correction handler, duplicate bank movement evidence/attention, WebAdmin internal workflow, deterministic posting keys, row-version protection, migrations, and source guards.
   - It must add no bank API integration, payroll advance/employee receivable, public/mobile exposure, customer payment/refund reuse, supplier finance mutation, payslip rewrite, provider submission, or finance export format change.
3. `Payroll Payslip Template/PDF Completion Slice`
   - Outcome: complete. Formal versioned PDF rendering is implemented over immutable payslip artifact data and remains separate from salary payment and bank correction.
4. `Payroll Provider Adapter Design Slice`
   - Outcome: complete. Start implementation only after real provider targets, credential owner, payload mapping, safe error contract, and smoke strategy are selected.
5. `Bank API Target Adapter Design`
   - Start only after real target banks, credential owner, payload mapping, error contract, and smoke strategy are selected.

## Implementation Outcome

- Returned/failed salary transfer and duplicate salary bank movement decisions are implemented through `PayrollPaymentBankCorrection`.
- Returned-transfer correction is evidence-backed, full-settlement v1, and journal-owned.
- Duplicate payment is attention/evidence first and cannot be auto-reversed or auto-posted.
- Payroll payment history, original settlement journals, reconciliation history, payslip artifacts, customer payment/refund flows, supplier finance flows, and finance export package format are not rewritten.
- Payroll advance/employee receivable and bank API adapters remain future dedicated slices.
- Employee payslip self-service core and AI governance are complete for the current phase. The current implementation gate is production go-live evidence execution.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund flows, supplier finance flows, statutory filing, provider submission, and finance export package format remain unchanged.

## Compatibility Guards

- Do not model returned salary transfer by editing `PayrollPayment.Status`.
- Do not delete, rewrite, or mutate original payroll payment posting, bank settlement posting, bank reconciliation, payslip artifact, or finance export history.
- Do not create correction from manual reason alone; bank statement or reconciliation evidence is required.
- Do not add partial correction, payroll advance, employee receivable, bank API integration, bank credential UI, provider credential UI, statutory provider submission, employee self-service, public/mobile routes, finance export package changes, supplier finance mutations, customer payment/refund mutations, or invoice archive/download behavior in the boundary step.
- Do not store private employee bank account numbers, provider credentials, tokens, raw bank payloads, raw provider payloads, medical details, private document contents, or connection strings in metadata, events, audit trails, external references, tests, or documentation.

## Documentation Verification

- `docs/README.md` links this document from the ERP expansion map.
- `BACKLOG.md` and `erp-expansion-master-status.md` record employee payslip self-service core and AI governance as complete, with production go-live evidence execution as the current gate.
- This document contains no deferred ambiguous decisions for the payroll returned-transfer boundary or core outcome.
- Restricted vendor/source scans must return no output.
