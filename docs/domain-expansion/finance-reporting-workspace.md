# Finance Reporting Workspace

## Summary

The finance reporting workspace is the read-only operator surface for receivables, postings, and finance reconciliation evidence. It does not introduce new schema, public API routes, mobile contracts, payment/refund mutations, invoice mutations, credit-note mutations, or parallel finance documents.

Finance reporting is built on the current Darwin-owned foundations:

- `JournalEntry` and `JournalEntryLine` remain the posting source.
- `FinancePostingAccountMapping` remains the account-role resolution source.
- `ReceivablesProjectionService` remains the receivables projection source.
- `Invoice`, `Payment`, `Refund`, and `CreditNote` remain their existing authoritative operational records.
- Billing workspaces remain the owner for financial account, manual journal entry, payment, refund, and tax-compliance mutations.
- Accounting export design is locked separately in [finance-export-accounting-integration-design.md](finance-export-accounting-integration-design.md); export must package posted journal entries rather than mutable operational documents.

## Current Darwin Finance Findings

- Automated receivables posting already writes source-linked journal entries for issued invoices, payments, refunds, invoice cancellation, and credit notes.
- Receivables projection is business-scoped because account mappings are business-scoped.
- Credit notes now have legal numbering, immutable source model evidence, posting linkage, and internal source-model download.
- Billing has operational finance workspaces for financial accounts, expenses, journal entries, payments, refunds, webhooks, tax compliance, and subscriptions.
- A dedicated Finance workspace is still useful because operators need a business-facing reporting lens without leaving mutation authority ambiguous.

## Locked Decisions

| Surface | Decision | Owner | Mutation impact | Mobile/member impact | Notes |
| --- | --- | --- | --- | --- | --- |
| Finance overview | Read-only WebAdmin reporting workspace over current postings and receivables. | Finance WebAdmin reporting | None | None | Shows business-scoped balance, posting counts, credit-note attention, and top receivables. |
| Receivables | Uses `ReceivablesProjectionService`; no second receivables calculator is created. | Billing finance foundation | None | None | Missing account mapping is shown as readiness evidence, not silently ignored. |
| Postings | Reads existing `JournalEntry` records and links to Billing journal entry detail. | Billing journal foundation | None | None | Finance does not create or edit journal entries. |
| Credit-note reconciliation | Shows credit-note posting attention through existing source fields. | Sales/Finance credit-note foundation | None | None | Credit-note lifecycle remains in Sales internal WebAdmin. |
| Financial account setup | Linked to Billing financial account workspace. | Billing | Existing Billing mutations only | None | Finance reporting does not duplicate account-management forms. |
| Manual journal entries | Linked to Billing journal entry workspace. | Billing | Existing Billing mutations only | None | Finance reporting does not create a second journal editor. |
| Global reporting | Not implemented as cross-business aggregation in v1. | Future finance reporting | None | None | Business-scoped reporting avoids mixing account mappings and currencies. |

## Implementation Outcome

- Added internal Application reporting queries for overview, receivables, and postings.
- Added WebAdmin `Finance` navigation with `Overview`, `Receivables`, and `Postings`.
- Added focused source and render tests to keep the workspace read-only and tied to authoritative Billing workspaces.
- No schema, migration, public route, WebApi DTO, mobile DTO, storefront checkout, invoice archive/download, payment/refund flow, or credit-note lifecycle behavior changed.

## Next Step

Finance account mapping admin exposure is implemented in [finance-account-mapping-admin-exposure.md](finance-account-mapping-admin-exposure.md), and finance export boundaries are locked in [finance-export-accounting-integration-design.md](finance-export-accounting-integration-design.md).

The next suitable implementation gate is `Finance Export Batch Foundation Slice`: add structured export batch identity and safe retry evidence before package downloads or connector adapters are implemented.
