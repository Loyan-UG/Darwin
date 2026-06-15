# Finance Posting Foundation Design

This document locks the finance posting foundation before credit-note, receivable, tax reversal, or broader accounting implementation changes. It started as a design artifact and now also records the implementation outcome for the foundation slice. The implementation adds additive journal-entry metadata, migrations, and an internal posting service; it does not add routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, invoice archive/download changes, or production posting flows.

The decision is explicit: Darwin should evolve the current lightweight accounting model instead of creating a parallel finance ledger. Automated posting is blocked until source linkage, idempotency, posting status, reversal policy, and account mapping are designed.

## Current Darwin Finance And Billing Findings

- `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` already exist in the `Billing` schema as a lightweight double-entry bookkeeping foundation.
- `CreateJournalEntryHandler` and `UpdateJournalEntryHandler` support manual balanced journal entries, row-version protection, and WebAdmin billing workflows.
- `Payment` and `Refund` remain authoritative settlement records. They are not journal entries and should not be duplicated as a second settlement ledger.
- `Invoice` remains the shared issued invoice foundation. Issued snapshots, archive metadata, structured JSON/XML, e-invoice artifacts, and member downloads remain authoritative compliance surfaces.
- `ReturnOrder` provides return eligibility, inspection evidence, and refund linkage, but it does not post receivables, tax reversals, or credit notes.
- Current accounting lacks canonical automated posting source linkage, posting status, source idempotency keys, receivable/payable projections, tax posting accounts, period locks, and reversal/correction policy.

## Posting Foundation Decision Matrix

| Posting surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Schema/API impact | CreditNote impact | Invoice/refund impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Ledger model | `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` exist in `Billing`. | Evolve existing accounting entities; do not create parallel `FinanceJournal`, `GeneralLedgerEntry`, or provider-specific ledgers. | Existing Billing accounting model. | Future finance posting service. | Future additive fields likely; no change in this design step. | Credit notes must post into this foundation when ready. | Invoices/refunds remain operational records until posting is implemented. | Unchanged. | Partially ready. | Design additive source linkage and posting policy over current model. |
| Manual journal entries | WebAdmin can create/edit balanced journal entries. | Preserve manual entries as operator-owned accounting adjustments. Automated postings must not overwrite them. | Current billing journal handlers. | Existing Billing WebAdmin handlers. | No change now. | Manual entries are not credit notes. | Manual entries do not change payment/refund status. | Unchanged. | Ready. | Keep manual and automated posting ownership separate. |
| Automated posting | No canonical automated posting service exists. | Add a future internal posting service with idempotent source keys and source document links. | Future finance posting service. | Application finance handlers only. | Future additive implementation. | Required before issuing credit notes. | Required before invoice/refund posting. | Unchanged. | Not ready. | Implement after this design as a dedicated foundation slice. |
| Source linkage | Journal entries do not have canonical source entity fields. | Future posting must record source entity type/id, source document number, source event key, and posting reason. | Future posting metadata on current journal entries. | Finance posting service. | Future additive fields. | Credit note postings must link to credit note and original invoice. | Invoice/refund postings must link to their source records. | Unchanged. | Not ready. | Add structured source linkage before any automated posting. |
| Idempotency | Manual journal entries can be edited; automated idempotency is not modeled. | Automated posting must be idempotent by source key and posting kind. Retry must not create duplicate debits/credits. | Finance posting service and journal entry metadata. | Finance posting service. | Future unique index/filter likely. | Prevents duplicate credit-note posting. | Prevents duplicate invoice/payment/refund posting. | Unchanged. | Not ready. | Design `PostingKey`/source uniqueness before implementation. |
| Posting status | No canonical status for draft/posted/reversed/voided automated entries. | Future posted entries need explicit lifecycle and immutable posted lines. Draft/manual editing must stay separate from posted automation. | Future journal entry status policy. | Finance posting service; WebAdmin for manual entries only. | Future additive enum/columns. | Credit note cannot be issued without posted state. | Invoice/refund posting must not depend only on operational status text. | Unchanged. | Not ready. | Define posting lifecycle before automation. |
| Account mapping | Financial accounts exist, but no default mapping policy is canonical. | Account mapping must be configured per business or defaulted safely before automation: receivables, revenue, tax payable, cash/clearing, refund/credit, rounding. | Future account mapping policy over `FinancialAccount`. | Finance configuration handler/service. | Future additive mapping model likely. | Credit-note lines need reversal accounts. | Invoice/payment/refund postings need deterministic accounts. | Unchanged. | Not ready. | Design account mapping foundation before posting service writes entries. |
| Receivables | Invoice status and payment/refund state exist; no receivable projection is canonical. | Receivable balance must derive from posted invoice, credit, payment, and refund entries or a maintained projection with posting evidence. | Future finance receivable projection. | Finance posting service. | Future projection/schema likely. | Credit notes reduce receivable through posting. | Invoice issue increases receivable; payments/refunds settle or adjust it. | Unchanged. | Not ready. | Define receivable projection after posting entries are structured. |
| Tax posting | Invoice tax totals exist; credit-note tax reversal is not modeled. | Tax posting must preserve original issued invoice tax context and reversal evidence. It must not recompute from current tax settings. | Issued invoice snapshot/source model plus posting service. | Finance posting service. | Future implementation only. | Credit-note tax reversal requires this. | Invoice tax posting consumes issued snapshot totals. | Unchanged. | Not ready. | Define tax account mapping and rounding rules. |
| Reversal/correction | No canonical reversal posting policy exists. | Corrections must use reversal or adjusting entries, not destructive edits to posted financial facts. | Future posting lifecycle. | Finance posting service. | Future additive lifecycle/status. | Credit-note cancellation/void depends on reversal policy. | Invoice/refund correction depends on reversal policy. | Unchanged. | Not ready. | Define reversal/void rules before credit-note implementation. |
| Period locks | No accounting period lock model is canonical. | Period locks are required before strict accounting close. V1 posting may document the gap, but posted entries must be designed so locks can be added without redesign. | Future finance period model. | Finance/accounting admin service. | Future additive model. | Prevents late credit-note mutation in closed periods. | Prevents late invoice/refund posting changes. | Unchanged. | Deferred by design. | Account for period lock fields in posting design; implement when finance close is in scope. |
| External accounting export | `ExternalReference` exists. | External accounting ids, export batch ids, and imported posting ids use `ExternalReference`, not provider-specific fields on journal entries. | `ExternalReference`. | Future integration/export handlers. | No change now. | Supports external credit-note coexistence. | Supports invoice/refund export reconciliation. | Unchanged. | Foundation ready. | Reuse external references in export/import slice. |
| Audit/evidence | `BusinessEvent` and `AuditTrail` exist. | Posting events/audits are evidence only; journal entries remain the financial source of truth. | `BusinessEvent`, `AuditTrail`, journal entries. | Finance posting service. | No change now. | Credit-note issue posts and emits evidence. | Invoice/refund postings emit evidence. | Unchanged. | Foundation ready. | Add evidence when automated posting is implemented. |
| Archive/source model | Invoice archive/source model exists. | Posting must reference immutable source documents and never replace legal invoice or future credit-note archives. | Invoice archive/source model; future credit-note archive/source model. | Archive handlers remain archive owners. | No change now. | Credit note requires archive/source model before issue. | Invoice archive remains authoritative. | Unchanged. | Not ready for credit note. | Keep archive/source model separate from journal entry metadata. |
| WebAdmin visibility | Billing has manual journal entry workflows. | Future finance posting UI should show source-linked postings and reconciliation without creating operational invoice/refund/credit-note mutations in the ledger UI. | Billing/Finance WebAdmin. | Existing operational handlers remain mutation owners. | Future UI only. | Credit-note actions wait for finance readiness. | Payment/refund/invoice actions stay in current workspaces. | Unchanged. | Not ready. | Design posting review UI after service/API shape is clear. |

## Locked Finance Posting Decisions

- Darwin will evolve current `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` rather than create a parallel general ledger model.
- Manual journal entries remain operator-owned and editable through current Billing workflows with row-version protection.
- Automated postings require structured source linkage, idempotency, posting lifecycle, account mapping, and reversal policy before implementation.
- `Payment` and `Refund` remain authoritative settlement records. Posting consumes their facts; it does not replace or mutate settlement state.
- `Invoice` remains the issued document source. Posting consumes issued invoice snapshot/source-model data and must not mutate issued invoice financial fields.
- `CreditNote` remains blocked until finance posting, receivable impact, tax reversal, legal numbering, and archive/source-model policy are implemented or explicitly proven ready.
- Journal entries are the financial source of truth for postings; `BusinessEvent` and `AuditTrail` are evidence and automation context only.
- `ExternalReference` is the foundation for external accounting/export identity.
- No public WebApi, mobile/member, storefront, checkout, invoice archive/download, payment-intent, or issued snapshot contract changes are introduced by this design.

## Next Implementation Slice

`Finance Posting Foundation Implementation Slice`

Implemented scope:

- Additive source-linkage and posting lifecycle fields were added to the existing `Billing.JournalEntries` foundation: `PostingStatus`, `PostingKind`, `PostingKey`, `SourceEntityType`, `SourceEntityId`, `SourceDocumentNumber`, posting timestamps, reversal reference, posting reason, and safe `MetadataJson`.
- PostgreSQL and SQL Server migrations add only columns and indexes on the existing `JournalEntries` table. They do not create a parallel ledger or new invoice/order tables.
- `FinancePostingService` now validates balanced debit/credit lines, account existence, same-business account ownership, deterministic posting keys, source identity, safe metadata, and idempotent retries.
- Existing manual journal-entry handlers remain the owner for manual accounting adjustments.
- Invoice, payment, refund, return order, and credit-note flows are not instrumented in this slice. They must use this foundation in future slices with explicit account mapping, receivable/tax rules, and compatibility tests.
- Public/mobile/member/storefront contracts remain unchanged.

## Finance Posting Foundation Implementation Outcome

Darwin now has the first automated posting foundation over the current lightweight Billing accounting model.

- `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` remain the accounting foundation.
- `JournalEntryPostingStatus` and `JournalEntryPostingKind` distinguish manual draft entries from posted automated entries without changing current WebAdmin manual workflows.
- `PostingKey` provides idempotency for future automation. Replaying the same source returns the existing journal entry; reusing the same key for a different source is rejected.
- `FinancePostingService` is internal Application-layer infrastructure. It is not exposed through public WebApi, mobile, storefront, or WebAdmin routes in this slice.
- The service rejects sensitive metadata, secrets, tokens, credentials, unbalanced lines, missing accounts, cross-business accounts, and invalid source identity.
- `MetadataJson` maps to PostgreSQL `jsonb`; SQL Server keeps provider-neutral string storage.
- `Payment` and `Refund` remain authoritative settlement records. `Invoice` remains the issued document source. Posting consumes their facts later; it does not replace their mutation owners.
- `CreditNote` remains finance-gated until account mapping, receivable/tax reversal policy, legal numbering, archive/source-model behavior, and member visibility are implemented together with the credit-note lifecycle.

## Next Implementation Gate

`Invoice/Payment/Refund Posting Wiring Slice`

The account mapping and receivables boundary is now decision-complete, account mapping is implemented in [finance-account-mapping-receivables-design.md](finance-account-mapping-receivables-design.md), and read-only receivables projection is implemented in [finance-receivables-projection.md](finance-receivables-projection.md). Before implementing `CreditNote`, Darwin still needs the remaining implementation slices defined there:

- invoice/payment/refund posting wiring through `FinancePostingService` and `FinanceAccountMappingService`;
- credit-note source-model/archive behavior;
- reversal/correction behavior for posted entries;
- period lock readiness.

Implemented account mapping foundation now provides:

- business-scoped roles for receivables, sales revenue, tax payable, cash clearing, refund clearing, and rounding;
- business-level overrides without hardcoding account ids in handlers;
- fail-closed validation for missing, inactive, soft-deleted, cross-business, or account-type-incompatible mappings.
- read-only receivables projection from effective journal entries, grouped by source document.

The next implementation gate is `Invoice/Payment/Refund Posting Wiring Slice`, not immediate credit-note schema/UI implementation.
