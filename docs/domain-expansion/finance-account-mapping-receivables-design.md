# Finance Account Mapping And Receivables Design

This document locks account mapping, receivables, tax reversal, period readiness, and posting ownership before credit-note or finance WebAdmin implementation. The account mapping foundation is implemented as an additive internal Billing foundation. Invoice, payment, refund, and invoice-cancellation reversal posting are now wired through internal Application services without adding public routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, invoice archive/download behavior, payment-intent behavior, or new mutation owners.

The decision is explicit: Darwin now has an internal finance posting service over the current Billing journal foundation. Invoice/payment/refund posting uses business-scoped account mappings and deterministic posting keys. Return-order posting remains unwired because ReturnOrder is evidence/linkage, and CreditNote remains blocked until source/archive/tax/legal policy is complete.

## Current Darwin Finance Mapping Findings

- `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` exist in the `Billing` schema and remain the lightweight double-entry accounting foundation.
- `FinancePostingService` can create idempotent, balanced, source-linked posted journal entries, but it intentionally does not choose accounts for invoices, payments, refunds, returns, or credit notes.
- `AccountType` classifies accounts as `Asset`, `Liability`, `Equity`, `Revenue`, or `Expense`. `FinancePostingAccountMapping` now maps business-specific posting roles such as receivables, revenue, tax payable, cash clearing, refund clearing, and rounding to concrete `FinancialAccount` records.
- `Invoice` remains the issued document source for invoice archive, structured source-model, tax evidence, and member invoice contracts.
- `Payment` and `Refund` remain authoritative settlement records. Posting may consume their facts later, but it must not replace their mutation owners or provider reconciliation data.
- `ReturnOrder` provides accepted return quantities, refund eligibility, refund linkage, and goods-return evidence. It does not post accounting entries and does not issue credit notes.
- `CreditNote` remains blocked until account mapping, receivable impact, tax reversal, legal numbering, immutable source/archive behavior, and member visibility are complete.

## Account Mapping Decision Matrix

| Mapping surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Posting impact | Receivable impact | Tax impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Account classification | `FinancialAccount.Type` exists with broad account classes. | Keep `AccountType` as financial statement classification; posting roles are mapped separately through `FinancePostingAccountMapping`. Do not overload `AccountType` with business roles. | `FinancialAccount` plus `FinancePostingAccountMapping`. | Billing/Finance account management service. | Posting service validates accounts but does not infer roles. | Receivable account is selected by mapping, not by account type alone. | Tax payable account is selected by mapping. | Unchanged. | Implemented foundation. | Use the mapping service from future posting flows. |
| Receivables account | `FinancePostingAccountRole.Receivables` exists. | A business must have one active default receivables account before invoice or credit-note posting. | `FinancePostingAccountMapping`. | Finance configuration handler/service. | Invoice issue debits receivables; payment credits receivables; refund debits receivables; cancellation reversal credits receivables; future credit note credits receivables. | Receivable balance derives from posted entries or a projection backed by posted entries. | No direct tax effect. | Unchanged. | Implemented and used by invoice/payment/refund/cancellation posting. | Continue using mapping resolution in future credit-note posting. |
| Revenue account | `FinancePostingAccountRole.SalesRevenue` exists. | V1 uses a business default sales revenue account unless a future product/category revenue mapping is explicitly added. | `FinancePostingAccountMapping`; current invoice line snapshot remains source data. | Finance configuration handler/service. | Invoice issue credits revenue. Invoice cancellation reversal and future credit notes debit the original revenue basis. | Revenue does not drive receivable balance directly. | Tax must remain separate from revenue. | Unchanged. | Implemented and used by invoice issue/cancellation posting. | Use default revenue role in future credit posting unless explicit revenue mapping is designed. |
| Tax payable account | `FinancePostingAccountRole.TaxPayable` exists. | V1 requires a default tax payable account when posted documents include tax. Jurisdiction-specific tax accounts can be added later only through explicit mapping, not hardcoded handler branches. | `FinancePostingAccountMapping`. | Finance configuration handler/service. | Invoice issue credits tax payable; invoice cancellation reversal and future credit notes debit tax payable based on issued evidence. | Tax posting affects receivable through total invoice value but remains separately posted. | Uses issued invoice tax evidence; never recomputes from current tax settings. | Unchanged. | Implemented and used when tax is present. | Use tax payable role in future credit posting. |
| Cash/clearing account | `FinancePostingAccountRole.CashClearing` exists. | Payment and refund posting use a mapped cash or clearing account. Provider settlement details stay on `Payment` and `Refund`. | Current `Payment`, `Refund`, and `FinancePostingAccountMapping`. | Existing payment/refund handlers remain settlement owners; finance posting service records accounting facts. | Payment debit goes to cash/clearing and credit goes to receivables. Refund credit goes to cash/clearing and debit goes to receivables. | Reduces or reverses receivable through posting. | No direct tax reversal. | Unchanged. | Implemented and used by payment/refund posting. | Future provider settlement reconciliation can build on the same role. |
| Refund/credit clearing account | `FinancePostingAccountRole.RefundClearing` exists. | V1 completed customer refund settlement posts against receivables and cash clearing. `RefundClearing` remains available for future provider-clearing or customer-credit policy, but is not silently used by current refund posting. | Current `Refund` plus future finance policy. | Existing refund handlers remain settlement owners. | No current automated posting uses this role. Future use requires an explicit policy. | Prevents hidden customer-credit balances. | Credit note, not refund payload, carries tax reversal. | Unchanged. | Implemented as a role, not consumed by current refund posting. | Use only after provider clearing or customer credit policy is designed. |
| Rounding account | `FinancePostingAccountRole.Rounding` exists. | Rounding differences are posted only when calculated from immutable issued source data and must use a mapped rounding account. Silent rounding into revenue or tax is forbidden. | `FinancePostingAccountMapping`. | Finance posting handler. | Posting remains balanced without hiding rounding in unrelated accounts. | Prevents unexplained receivable deltas. | Preserves tax evidence. | Unchanged. | Implemented as a role; no current posting scenario emits rounding lines. | Require rounding role only when a posting scenario can produce rounding deltas. |
| Business-level overrides | Structured posting role mapping exists and is business-scoped. | V1 mapping is business-scoped because `FinancialAccount.BusinessId` is required. Platform defaults require a separate global-account policy before implementation. Handler code must request mappings by role; it must not hardcode account ids. | `FinancePostingAccountMapping`. | Finance configuration handler/service. | Enables customer-specific accounting without branching operational handlers. | Receivable projection can group by mapped roles. | Tax accounts can differ by business policy later. | Unchanged. | Implemented foundation. | Do not add global defaults until global account ownership is designed. |
| Account mapping validation | `FinanceAccountMappingService` validates mapping completeness and role/account compatibility. | Posting automation must fail closed when required roles are missing, inactive, soft-deleted, cross-business, or incompatible with account type. It must not create fallback accounts silently. | `FinanceAccountMappingService` plus `FinancialAccount`. | Finance configuration and posting services. | Prevents wrong journal entries. | Prevents false receivable balances. | Prevents wrong tax liabilities. | Unchanged. | Implemented foundation. | Future posting flows must call `ResolveRequiredAccountsAsync`. |
| External accounting identity | `ExternalReference` exists. | External account ids and exported posting ids use `ExternalReference`; mapping rows must not store provider tokens or raw payloads. | `ExternalReference`. | Integration/export handlers. | Supports reconciliation with external accounting systems. | Exported receivable balances can be traced without duplicating source data. | Exported tax evidence can be traced. | Unchanged. | Foundation ready. | Use external references in future accounting export/import slice. |

## Receivables Decision Matrix

| Receivable surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Posting impact | Reporting impact | CreditNote impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Receivable source of truth | Invoice status and settlement records exist; journal posting foundation now exists. | Receivable source of truth is posted accounting entries, not invoice status text alone. A projection may be maintained later, but it must be backed by posted entries. | `JournalEntry`/`JournalEntryLine` plus source links. | Finance posting service. | Invoice/payment/refund/credit-note posting must use deterministic source keys. | Reporting can reconcile balances to journal entries. | Credit note reduces receivable only through posting. | Unchanged. | Design ready. | Implement receivable projection after account mapping exists. |
| Invoice issue | `Invoice` has issued state, lines, totals, archive/source-model paths. | Invoice posting consumes the issued invoice totals and never posts draft invoices. Issued invoice cancellation posts a reversal entry instead of destructively editing the original posting. | Current `Invoice`. | Existing invoice handlers remain document owners; finance posting service records accounting facts. | Debit receivable; credit revenue and tax payable. Cancellation reversal debits revenue/tax and credits receivable. | Creates and reverses receivable opening amount through posted entries. | Original invoice posting is the basis for future credit limits. | Member contracts unchanged. | Implemented for issue and cancellation reversal. | Design credit-note source/archive policy before credit-note schema. |
| Payment settlement | `Payment` records settlement/provider facts. | Captured/completed payment posting consumes current `Payment` facts and settles receivable. It does not mutate payment provider state. | Current `Payment`. | Existing payment handlers remain settlement owners; finance posting service records accounting facts. | Debit cash/clearing; credit receivable. | Reduces open receivable. | Credit note must reconcile with payment state but does not execute settlement. | Unchanged. | Implemented. | Future finance UI can show settlement entries after authorization/UX design. |
| Refund settlement | `Refund` records authoritative refund state. | Completed refund posting consumes current `Refund` facts. It does not create or complete refunds. Refund settlement debits receivable and credits cash clearing so it can reconcile with invoice cancellation or future credit-note reversal entries. | Current `Refund`. | Existing refund handlers remain settlement owners; finance posting service records accounting facts. | Debit receivable; credit cash/clearing. | Reflects settlement reversal without replacing refund records. | Credit note may justify/refine receivable/tax reversal separately. | Unchanged. | Implemented. | Keep credit note blocked until source/archive/tax/legal policy is complete. |
| Credit note receivable effect | Credit note is not implemented. | Credit note must post receivable and tax/revenue reversal from issued invoice snapshot/source model. It must not be a negative invoice or UI-only record. | Future `CreditNote` plus current `Invoice`. | Future credit-note issue handler. | Credit receivable; debit revenue/tax reversal accounts according to mapping and legal policy. | Reduces open receivable with document evidence. | Core requirement. | Member visibility deferred. | Blocked. | Implement only after account mapping, archive/source-model, numbering, and tax policy are complete. |
| Partial credits | ReturnOrder and refunds can be partial. | Remaining credit eligibility must be calculated from prior posted credits against the issued invoice basis. It must not depend only on unposted UI status. | Posted journal entries plus source links. | Future credit-note handler. | Prevents over-crediting. | Maintains accurate receivable and revenue/tax reversal. | Required for partial returns and commercial credits. | Unchanged. | Not ready. | Design cumulative credit calculation before credit-note schema. |
| Overpayment/credit balance | No customer credit balance model is canonical. | V1 does not create customer wallet/credit balance from accounting posting. Overpayment must remain explicit and separately designed. | Future finance/customer credit policy. | Future finance handler. | Avoids accidental loyalty/customer balance fields. | Prevents mixing receivables with customer credit. | Credit note may create open credit only after policy exists. | Unchanged. | Blocked. | Design customer credit balance separately if business requires it. |
| Aging | No receivable aging projection is canonical. | Aging is a reporting projection over posted receivables and invoice due dates. It is not a mutation owner. | Posted entries plus invoice due date/source data. | Finance reporting query/service. | No posting mutation. | Enables open balance by bucket. | Credit notes reduce aged balance through posting. | Unchanged. | Ready after receivable projection. | Build after invoice/payment/credit posting is wired. |
| Period locks | No accounting period lock model exists. | Period lock support is required before strict financial close. Until implemented, posting services must be designed so period checks can be added centrally. | Future finance period model. | Finance/accounting admin service. | Blocks late posting/reversal in closed periods later. | Stabilizes receivable reports. | Prevents late credit-note mutation in closed periods. | Unchanged. | Design ready; implementation later. | Keep period validation centralized in posting/mapping services. |
| Reversal and correction | Posting status supports reversal metadata and invoice-cancellation reversal is wired. | Corrections use reversal or adjustment entries. Posted entries are not destructively edited by operational handlers. | `JournalEntry` posting status/source linkage. | Finance posting service. | Preserves audit trail and financial history. | Receivable projection includes reversal entries. | Credit-note cancellation/void needs a credit-note-specific policy later. | Unchanged. | Invoice cancellation reversal implemented; credit-note reversal policy pending. | Design credit-note reversal commands before credit-note cancellation implementation. |

## Locked Decisions

- `AccountType` remains broad financial classification. Posting roles are separate mappings such as receivables, sales revenue, tax payable, cash/clearing, refund/credit clearing, and rounding.
- Automated posting must fail closed when required account mappings are missing. It must not create silent default accounts or guess from account names.
- Account mappings are business-scoped, with optional platform defaults only if explicitly implemented. Handler code must request accounts by role through a service.
- Receivable source of truth is posted accounting entries. Invoice status, payment status, refund status, and return status are operational facts, not the receivable ledger by themselves.
- Invoice posting consumes issued invoice snapshot/source-model evidence. It must not post from draft or mutable invoice state.
- Payment and refund posting consume completed settlement records; they do not create, complete, or replace payment/refund records.
- Credit-note posting must derive line/tax reversal from the issued invoice source basis and prior posted credit history.
- Rounding must be explicit. It must not be hidden in revenue, tax, or receivable lines.
- Period locks and reversal/correction rules must be central service policies, not scattered controller or handler checks.
- Public WebApi, mobile/member, storefront checkout, payment-intent routes, invoice archive/download, and issued invoice contracts remain unchanged by this design step.

## Required Implementation Slices

The complete path before `CreditNote Core Model And Admin Slice` is:

1. `Finance Account Mapping Foundation Slice`
   - Implemented: structured business-scoped posting-role mapping over existing `FinancialAccount`.
   - Implemented roles: receivables, sales revenue, tax payable, cash clearing, refund clearing, and rounding.
   - Implemented service validation/query foundation; WebAdmin configuration remains a separate slice because this implementation does not expose UI or public/mobile contracts.
2. `Receivables Projection Design/Implementation Slice`
   - Define and implement open receivable calculation from posted invoice, payment, refund, reversal, and future credit-note entries.
   - Keep projection read-only/reporting unless a separate maintained projection is explicitly justified and tested.
3. `Invoice/Payment/Refund Posting Wiring Slice`
   - Implemented: current invoice/payment/refund handlers post through `FinanceReceivablesPostingService` and `FinancePostingService`.
   - Implemented: deterministic posting keys and source links.
   - Implemented: current operational mutation ownership and public/member/mobile contracts are preserved.
4. `CreditNote Source Model And Archive Design Slice`
   - Define credit-note source JSON/XML/archive behavior, legal numbering, member visibility, cancellation/void, and source immutability.
5. `CreditNote Core Model And Admin Slice`
   - Implement only after the above slices are complete and verified.

## Next Implementation Gate

`Receivables Projection Design/Implementation Slice`

Implemented scope:

- Open receivable calculation from posted journal entries and source links now exists in [finance-receivables-projection.md](finance-receivables-projection.md), using account role mappings.
- The first projection is read-only/reporting. A maintained projection remains unintroduced because scale and close-period requirements do not justify another stored balance yet.
- Do not wire invoice, payment, refund, return order, or credit note posting in that slice unless the same slice also includes source-specific posting tests.
- Do not add public/mobile/member/storefront contracts.
- Keep invoice archive/download, issued snapshots, payment/refund mutation ownership, and mobile/member contracts unchanged.

## Finance Account Mapping Foundation Implementation Outcome

Darwin now has a structured internal mapping foundation for finance posting roles.

- `FinancePostingAccountRole` defines `Receivables`, `SalesRevenue`, `TaxPayable`, `CashClearing`, `RefundClearing`, and `Rounding`.
- `FinancePostingAccountMapping` is stored in `Billing.FinancePostingAccountMappings` and maps one business-specific role to one concrete `FinancialAccount`.
- One active mapping per business/role is enforced with a soft-delete-aware unique index.
- `FinanceAccountMappingService` creates, updates, lists, and resolves mappings. It rejects empty ids, cross-business accounts, incompatible account types, inactive/missing roles during resolution, deleted accounts, and sensitive metadata.
- Mapping v1 is business-scoped because `FinancialAccount.BusinessId` is required. Global defaults are intentionally not implemented until global account ownership is designed.
- PostgreSQL maps `MetadataJson` to `jsonb`; SQL Server keeps provider-neutral string storage.
- This slice does not wire invoice, payment, refund, return order, or credit-note posting. It does not add WebAdmin UI, public WebApi, mobile/member, storefront, archive/download, payment-intent, or issued snapshot changes.

## Receivables Projection Implementation Outcome

Darwin now has an internal read-only receivables projection over posted accounting entries.

- `ReceivablesProjectionService` resolves the active receivables mapping through `FinanceAccountMappingService`.
- Projection fails closed if the business id is empty, the date range is invalid, or the receivables mapping is missing/inactive/deleted.
- Open balance is calculated as debit minus credit on the mapped receivables account.
- Entries with `Posted` and `Reversed` status are included; `Draft`, `Voided`, deleted entries, and deleted lines are excluded.
- Balances are grouped by source entity type/id/document number so future invoice, payment, refund, and credit-note reconciliation can drill into one source.
- No invoice/payment/refund/return/credit-note posting is wired in this slice.

## Invoice Payment Refund Posting Wiring Outcome

Darwin now posts source-specific receivable facts through the current operational owners.

- `FinanceReceivablesPostingService` posts invoice issue, payment recorded, refund recorded, and invoice cancellation reversal.
- Posting uses mapped `Receivables`, `SalesRevenue`, `TaxPayable`, and `CashClearing` accounts.
- Missing required mappings fail closed; no fallback accounts are created.
- Refunds remain authoritative settlement records, and invoice cancellation remains an invoice status/reversal concern; no credit-note schema or negative invoice is introduced.
- Public WebApi, mobile/member, storefront checkout, payment-intent routes, invoice archive/download, and issued invoice contracts remain unchanged.

The next implementation gate is `CreditNote Source Model And Archive Design Slice`.
