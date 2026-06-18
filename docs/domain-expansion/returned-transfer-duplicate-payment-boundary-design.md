# Returned Transfer And Duplicate Payment Boundary Design

## Summary

This document locks the boundary for returned or failed supplier bank transfers and duplicate bank movements after supplier payment bank settlement. The boundary design is complete and the core internal implementation now exists. It adds no public/mobile/storefront contract, finance export format change, bank API integration, customer payment/refund flow change, journal editor shortcut, supplier advance, or supplier overpayment.

The core decision is that bank-settled supplier payment corrections must be evidence-backed and journal-owned. A returned or failed transfer cannot be represented by editing `SupplierPayment` status, deleting history, rewriting settlement journals, or changing finance export history. Duplicate bank movements are evidence and attention items first; any financial correction must go through a separate handler-owned correction flow.

## Current Darwin Returned Transfer And Duplicate Payment Findings

- `SupplierPayment` supports posted, reversed, and bank-settled states. Bank settlement is journal-backed by debiting `CashClearing` and crediting the mapped bank Asset account.
- `SupplierPayment` stores bank settlement timestamp, settlement journal link, reconciliation match link, and safe settlement notes.
- `BankAccount`, `BankStatementImport`, `BankStatementLine`, `BankReconciliationMatch`, and `BankReconciliationMatchLine` exist in Billing.
- Bank reconciliation is evidence. It does not rewrite `SupplierPayment`, customer `Payment`, `Refund`, `JournalEntry`, or finance export history.
- Bank-settled supplier payment is blocked from simple full-payment reversal because settlement correction requires bank evidence and the formal correction workflow.
- `SupplierPaymentBankCorrection` exists for internal returned-transfer correction and duplicate-payment attention after bank settlement.
- Bank API integration, returned-transfer automation, duplicate-payment automation, supplier advance, and supplier overpayment are not implemented.
- `BusinessEvent`, `AuditTrail`, `DocumentRecord`, and `ExternalReference` are available for lifecycle evidence, documents, and remote ids.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund flows, and finance export package format remain unchanged.

## Decision Matrix

| Correction surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Accounting impact | Evidence impact | WebAdmin impact | Public/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Returned or failed transfer trigger | Bank-settled supplier payment links to a matched reconciliation. | Trigger must come from bank statement or reconciliation evidence, not operator reason alone. | Future correction model plus bank evidence. | Future correction handler. | No status-only accounting change. | Requires bank statement line and reconciliation linkage. | Future attention/action only after implementation. | Unchanged. | Ready for core correction design. | Implement correction core. |
| Settlement reversal boundary | Settlement journal debits `CashClearing` and credits bank Asset. | v1 correction reverses or adjusts the settlement clearing entry only; original supplier payment history remains. | Finance posting service. | Future correction handler. | Debit/credit correction is journal-backed and idempotent. | Correction links original settlement journal. | UI shows correction evidence and journal links. | Unchanged. | Ready. | Add full-settlement correction handler. |
| Supplier payment state impact | Supplier payment is posted and bank-settled. | Payment is not deleted, rewritten, or changed by status-only edit. Bank-settled payment remains historical payment evidence. | Supplier payment aggregate plus correction model. | Future correction handler. | AP payment posting history remains intact. | Correction record explains bank failure or duplicate movement. | Read-only payment timeline plus correction action. | Unchanged. | Ready. | Add separate correction status/evidence. |
| Full vs partial correction | Settlement v1 is full-payment. | v1 returned-transfer correction is full-settlement only. Partial returned amount and allocation-level bank correction are blocked. | Future correction policy. | Future correction handler. | Avoids ambiguous AP aging and split clearing. | Partial evidence can be recorded as attention but not posted as partial correction. | UI must not offer partial correction. | Unchanged. | Locked for v1. | Design partial correction later only with allocation policy. |
| Bank statement evidence | `BankStatementLine` has normalized identity and safe statement facts. | Bank statement line is the primary source fact for returned, failed, or duplicate movement evidence. | Bank statement import model. | Bank statement import and correction handlers. | Statement alone does not post accounting. | Evidence references normalized statement identity. | UI links to statement line. | Unchanged. | Ready. | Use statement line in correction model. |
| Reconciliation linkage | `BankReconciliationMatch` links statement evidence to finance facts. | Correction must link to the reconciliation that proves the returned or duplicate movement. | Bank reconciliation model. | Future correction handler. | Reconciliation itself still does not mutate payment or journals. | Correction references reconciliation evidence. | UI links correction to reconciliation. | Unchanged. | Ready. | Require eligible reconciliation in core. |
| Duplicate bank movement detection | Statement line identity and reconciliation evidence exist. | Duplicate payment is evidence/attention first, not automatic reversal. | Bank statement and future correction query. | Future correction handler after operator decision. | No automatic journal reversal from detection alone. | Duplicate evidence uses statement identity and payment linkage. | Future attention queue can highlight suspected duplicates. | Unchanged. | Designed. | Add detection/readiness before automation. |
| Duplicate payment correction | No duplicate-payment correction model exists. | Financial correction must be explicit and handler-owned; auto-reversal is forbidden. | Future correction model. | Future correction handler. | Correction posts or reverses through finance posting only. | Operator decision and bank evidence are retained. | UI must require explicit correction action. | Unchanged. | Ready for core design. | Implement correction core after this design. |
| Correction model ownership | `SupplierPayment` has settlement fields only. | Use a separate future model such as `SupplierPaymentBankCorrection` or `BankSettlementCorrection`, not ad hoc fields on `SupplierPayment` and not event-only state. | Future Billing/Treasury correction model. | Future correction handlers. | Supports multiple corrections and audit without polluting payment history. | Each correction owns reason, evidence, status, and journal linkage. | UI can list corrections per payment. | Unchanged. | Locked. | Add additive model in core slice. |
| Journal posting and reversal impact | `JournalEntry` is authoritative. | Correction must create balanced, idempotent journal entries and never rewrite existing journals or exports. | Finance posting service. | Future correction handler. | Export continues to read posted journal entries. | Correction event links source journals. | Read-only journal links after correction. | Unchanged. | Ready. | Reuse posting service and deterministic keys. |
| Remittance and document evidence | `DocumentRecord` exists. | Bank letters, remittance proof, and return notices are documents only; they cannot trigger financial correction alone. | Document service plus correction model. | Document registration handlers. | Documents do not post accounting. | Documents support review and audit. | Read-only attachments after implementation. | Unchanged. | Foundation ready. | Reuse document foundation later. |
| External bank and accounting ids | `ExternalReference` exists. | Bank return ids, duplicate reference ids, remote transaction ids, and accounting ids use `ExternalReference`. | External reference foundation. | External reference service. | External ids do not decide posting state. | Store safe ids and display ids only. | Read-only references only. | Unchanged. | Ready. | Reuse existing foundation. |
| Supplier advance and overpayment boundary | Supplier payment blocks overpayment. | Advance and overpayment remain blocked and become a future dedicated slice with account roles, lifecycle, aging, and reconciliation policy. | Future payables/treasury policy. | Future advance handlers. | No hidden supplier credit or asset is created. | Advance evidence must be distinct from invoice settlement. | No advance UI in this correction slice. | Unchanged. | Not ready. | Document future dedicated slice. |
| Bank API target boundary | Finance export has file-delivery; bank API target is not selected. | No bank API adapter is implemented now. A future design should choose real targets, preferably widely used large German banks, with credential owner, payload mapping, error contract, and smoke strategy. | Future bank API adapter design. | Future adapter handlers. | API responses must not bypass posting/correction handlers. | Raw bank payload and credentials stay out of metadata. | No bank credential/config UI now. | Unchanged. | Conditional. | Design only after target selection. |
| WebAdmin/internal visibility | Finance workspace shows supplier payments, bank statements, and reconciliation. | v1 correction UI must be internal, evidence-backed, full-settlement, row-version protected, and handler-owned. | Finance WebAdmin plus Application handlers. | Future correction controller action. | UI cannot edit journal history. | UI shows evidence and correction status. | Internal only. | Unchanged. | Ready after core model. | Implement correction UI with core. |
| Public/mobile/storefront boundary | Public/member commerce is customer-facing. | No public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund, or finance export format change. | Internal Application and WebAdmin. | None outside internal/WebAdmin. | No customer-facing mutation. | No member-visible bank correction. | No public UI. | Unchanged. | Locked. | Keep compatibility smoke lanes. |

## Locked Decisions

- Returned or failed transfer is not represented by status-only edit on `SupplierPayment`.
- v1 correction for bank-settled supplier payment is full-settlement only.
- v1 correction reverses or adjusts the settlement clearing journal while original supplier payment history remains intact.
- Partial returned transfer, allocation-level bank settlement correction, and partial bank reversal remain blocked until a separate allocation policy is designed.
- Duplicate payment is not auto-reversed. The system records evidence and attention, then correction is performed only through an explicit handler-owned workflow.
- Correction needs a separate future model such as `SupplierPaymentBankCorrection` or `BankSettlementCorrection` so multiple correction events and evidence remain auditable.
- Bank statement and reconciliation evidence are required. Operator reason alone is not sufficient for financial correction.
- Supplier advance and overpayment remain blocked and are recorded as a future dedicated slice, not part of returned-transfer correction.
- Bank API adapter implementation is blocked until real targets, credential ownership, payload mapping, error contract, and smoke strategy are selected. Future target selection should consider widely used large German banks.
- Credentials, access tokens, refresh tokens, private keys, connection strings, raw bank payloads, and provider secrets must not be stored in metadata, documents, external references, events, logs, tests, or documentation.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund flows, and finance export package format remain unchanged.

## Implementation Sequence

1. `Returned Transfer And Duplicate Payment Boundary Design`
   - Outcome: this document. The correction boundary was locked before schema, WebAdmin action, or posting changes.
2. `Returned Transfer And Duplicate Payment Correction Core Slice`
   - Outcome: implemented. `SupplierPaymentBankCorrection` records evidence-backed corrections in Billing. Returned-transfer corrections are full-settlement v1 and post a balanced journal entry against the bank Asset account and `CashClearing`. Duplicate-payment corrections are evidence/attention only and cannot be auto-posted.
   - It adds no bank API integration, supplier advance/overpayment, public/mobile exposure, customer payment/refund reuse, or finance export format changes.
3. `Supplier Advance And Overpayment Boundary Design`
   - Outcome: completed in [supplier-advance-overpayment-boundary-design.md](supplier-advance-overpayment-boundary-design.md). Supplier advance is formal, Asset-backed, explicit, and separate from negative AP or silent overpayment conversion.
4. `Bank API Target Adapter Design`
   - Start only after real target banks, credential owner, payload mapping, error contract, and smoke strategy are selected.

## Implementation Outcome

The core correction implementation is complete for internal/WebAdmin use:

- Returned/failed transfer and duplicate bank movement decisions are locked and backed by a formal correction model.
- Returned-transfer correction is evidence-backed, full-settlement v1, and journal-owned.
- Duplicate payment is attention/evidence first and cannot be auto-reversed or auto-posted.
- Supplier payment history, original settlement journals, reconciliation history, customer payment/refund flows, and finance export package format are not rewritten.
- WebAdmin exposes only internal correction creation, returned-transfer correction posting, draft correction cancellation, and read-only evidence/journal links.
- Supplier advance/overpayment boundary design is complete; implementation remains a dedicated core slice. Bank API adapters remain future dedicated slices.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, customer payment/refund flows, and finance export package format remain unchanged.
