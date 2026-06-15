# Finance Receivables Projection

This document records the receivables projection implementation over Darwin's current finance posting foundation. This slice adds internal read-only Application behavior only. It does not add schema, migrations, public WebApi routes, mobile/member contracts, storefront checkout behavior, invoice archive/download behavior, payment/refund mutations, credit-note implementation, or WebAdmin finance UI.

## Current Darwin Receivables Findings

- `FinancePostingService` can create balanced, idempotent, source-linked journal entries.
- `FinancePostingAccountMapping` maps a business receivables role to a concrete `FinancialAccount`.
- `Invoice`, `Payment`, `Refund`, and `ReturnOrder` remain operational source records. They are not the receivables ledger by themselves.
- Invoice, payment, refund, and invoice-cancellation reversal posting are now wired through the current operational handlers when a business-scoped account mapping exists.
- Return order and credit-note posting remain unwired; ReturnOrder is return evidence/linkage, and CreditNote is still finance-gated until source/archive/tax/legal policy is complete.
- Member/mobile order and invoice contracts remain compatibility-sensitive and unchanged.

## Projection Decision Matrix

| Receivables surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Posting impact | Reporting impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Receivables account | `FinancePostingAccountMapping` maps `Receivables` to `FinancialAccount`. | Projection fails closed when the active receivables mapping is missing, inactive, deleted, cross-business, or account-type incompatible. | `FinanceAccountMappingService`. | Finance configuration service. | Prevents projection from guessing accounts. | Ensures balances are based on configured account roles. | Unchanged. | Implemented. | Future WebAdmin finance configuration can expose mappings with authorization and validation. |
| Open balance calculation | Journal entry lines have debit and credit amounts. | Open receivable balance is debit minus credit on the mapped receivables account. | `JournalEntry` and `JournalEntryLine`. | `FinancePostingService` and source-specific posting handlers. | Read-only projection; no posting mutation. | Enables source-level open balance reporting. | Unchanged. | Implemented. | Use as read model for posting verification and future WebAdmin finance reporting. |
| Financially effective entries | `JournalEntryPostingStatus` supports draft, posted, reversed, and voided. | Projection includes posted and reversed entries, and excludes draft, voided, deleted entries, and deleted lines. Reversal accounting must happen through reversal entries, not destructive recomputation. | Journal entry status and source linkage. | Finance posting/reversal service. | Preserves audit trail. | Avoids draft or voided amounts appearing as receivables. | Unchanged. | Implemented. | Define reversal commands before credit-note cancellation or finance close. |
| Source grouping | Journal entries have source entity type/id and document number. | Projection groups receivables by source entity type/id/document number and reports the last effective posting for each source. | Journal entry source linkage. | Posting handlers set source identity. | Supports invoice/payment/refund/credit reconciliation later. | Lets operators trace open balance to source documents. | Unchanged. | Implemented. | Future posting wiring must use deterministic source identity. |
| Date filtering | Journal entries have `EntryDateUtc`. | Projection supports `FromUtc` and `AsOfUtc`; invalid ranges fail closed. | Journal entry date. | Finance reporting query/service. | No mutation. | Enables period and aging foundations without period locks yet. | Unchanged. | Implemented. | Build aging buckets after invoice/payment/refund posting is wired. |
| Source filtering | Source entity identity is optional on old/manual entries. | Projection can filter by source entity type and id when provided. Entries without source identity remain visible in business-wide projection. | Journal entry source fields. | Posting handlers. | No mutation. | Supports focused reconciliation per invoice or future credit note. | Unchanged. | Implemented. | Future WebAdmin finance views can drill into one source document. |
| Invoice/payment/refund posting | Operational handlers own invoice issue/status, payment settlement, and refund settlement. | Automated posting is wired through `FinanceReceivablesPostingService` without changing operational mutation ownership. Issued invoices debit receivables and credit revenue/tax, payments debit cash clearing and credit receivables, completed refunds debit receivables and credit cash clearing, and invoice cancellation posts a reversal. | Current invoice/payment/refund handlers plus finance posting service. | Existing operational handlers remain owners; posting service records accounting facts. | Implemented with deterministic posting keys and mapped accounts. | Projection consumes posted entries immediately. | Unchanged. | Implemented for invoice, payment, refund, and cancellation reversal. | Build finance reporting/UI only after authorization and operator UX are designed as a complete slice. |
| Credit note impact | Credit note remains finance-gated. | Credit notes must reduce receivables through posted accounting entries, not a negative invoice or UI-only record. | Future credit-note handler plus issued invoice source model. | Future finance handler. | Blocked until source/archive/tax policy is complete. | Projection can include credit-note postings after implementation. | Unchanged. | Blocked. | Complete posting wiring and credit-note source/archive policy before credit-note core model. |

## Implementation Outcome

Darwin now has `ReceivablesProjectionService` as an internal Application service.

- It resolves the mapped `Receivables` account through `FinanceAccountMappingService`.
- It fails closed for empty business ids, invalid date ranges, or missing receivables mappings.
- It reads current `JournalEntry` and `JournalEntryLine` records only.
- It includes `Posted` and `Reversed` entries and excludes `Draft`, `Voided`, deleted entries, and deleted lines.
- It calculates debit, credit, and open balance in minor units for the mapped receivables account.
- It groups balances by source entity type, source entity id, and source document number.
- It does not mutate invoice, payment, refund, return order, credit-note, archive, storefront, WebApi, or mobile/member behavior.

## Invoice Payment Refund Posting Wiring Outcome

Darwin now has `FinanceReceivablesPostingService` as the internal wiring layer between operational sales/billing facts and the finance posting foundation.

- Issued invoices post balanced journal entries from the current `Invoice` totals: debit receivables, credit sales revenue, and credit tax payable when tax exists.
- Captured or completed payments post debit cash clearing and credit receivables.
- Completed refunds post debit receivables and credit cash clearing so refund settlement can reconcile with invoice cancellation or future credit-note reversal entries.
- Cancelled issued invoices post a reversal entry: debit revenue/tax reversal and credit receivables.
- Posting keys are deterministic per source record, so retry does not create duplicate journal entries.
- Required account mappings fail closed. The service does not create fallback accounts, guess account ids, or store provider secrets.
- Existing invoice, payment, refund, archive, storefront, WebApi, and mobile/member mutation owners remain unchanged.

## Next Implementation Gate

`CreditNote Source Model And Archive Design Slice`

Default scope:

- Define credit-note source JSON/XML/archive behavior, legal numbering, member visibility, cancellation/void policy, and source immutability before implementation.
- Keep existing invoice, payment, refund, archive, storefront, mobile/member, and WebApi mutation owners unchanged.
- Add source/archive tests before `CreditNote Core Model And Admin Slice`.
- Do not implement credit notes until credit-note source/archive, tax reversal, legal numbering, member visibility, and cancellation/void policy are complete.
