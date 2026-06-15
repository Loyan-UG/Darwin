# CreditNote Finance And Accounting Boundary Design

This document locks the `CreditNote` finance and accounting boundary before any entity, migration, route, DTO, WebAdmin UI, mobile/member/storefront contract, refund/payment flow, invoice archive/download behavior, or production flow changes.

The decision is explicit: `CreditNote` remains finance-gated. Darwin must not create a negative invoice, a non-posting cosmetic document, or a UI-only credit-note record.

## Current Darwin Invoice/Refund/Archive Findings

- The current `Invoice` model is the shared issued invoice foundation for order, CRM, billing, member invoice history, archive, structured source-model, and compliance flows. Darwin must not create parallel `SalesInvoice` or `FinanceInvoice` models for the same issued document.
- `Refund` is the authoritative payment/order/invoice settlement record. It records settlement state and provider/payment evidence; it is not an accounting document and not a credit note.
- `ReturnOrder` provides return eligibility, received/inspected quantity evidence, refund readiness, and refund linkage. It does not issue invoices or credit notes.
- Issued invoice snapshots, archive metadata, structured JSON/XML, e-invoice artifact paths, retention, purge behavior, and member download contracts are compatibility-sensitive and authoritative.
- Invoice cancellation, refund logic, canonical invoice/payment/refund posting, and invoice-cancellation reversal posting exist. Legal credit-note numbering, tax reversal, credit-note source-model policy, credit-note archive policy, and member visibility are not yet defined.

## Decision Matrix

| Credit-note surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Accounting/posting impact | Tax/archive impact | Refund/return impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Credit-note trigger | Refunds, invoice cancellation, and ReturnOrder evidence exist separately. | Credit note can be triggered only by a finance-approved reason such as post-issue correction, accepted return, or commercial credit. It is not created automatically by every refund. | Future `CreditNote` policy over current invoice/refund/return evidence. | Future finance/accounting handler. | Requires posting reason and receivable effect. | Requires tax reversal reason and archive policy. | May reference completed refunds or accepted returns, but does not execute settlement. | No v1 exposure. | Ready for core implementation. | Implement guarded reason policy in core model. |
| Invoice linkage | `Invoice` is the issued document foundation with immutable issue snapshots. | Credit note must link to an issued invoice and must not mutate issued invoice financial fields or source snapshot. | Current `Invoice` plus future `CreditNote`. | Future credit-note issue handler. | Posts against the original invoice receivable context. | Uses issued invoice snapshot/source model as the evidence base. | Refund linkage remains separate. | Member visibility deferred. | Not ready. | Define immutable linkage and issued-invoice eligibility rules. |
| Refund linkage | `Refund` is authoritative settlement. | Credit note links to refund evidence when relevant, but does not create, complete, or replace refunds. | Current `Refund`. | Existing refund/payment handlers remain settlement owners. | Posting must reconcile with settlement without duplicating settlement state. | Tax evidence must reference credit-note source model, not refund payload. | Completed refunds can support credit-note settlement evidence. | Unchanged. | Not ready. | Define settlement-to-posting reconciliation rules. |
| Return-order linkage | `ReturnOrder` records return lifecycle, accepted quantities, and refund eligibility. | Credit note can reference ReturnOrder for goods-return reason and inspection evidence, but ReturnOrder does not issue the credit note. | Current `ReturnOrder`. | ReturnOrder handlers remain return owners; future finance handler owns credit note issue. | Return evidence may justify receivable/tax reversal. | Accepted quantity evidence can support line-level credit. | Return refund eligibility stays separate from credit-note posting. | Unchanged. | Not ready. | Use ReturnOrder only as evidence input after finance readiness. |
| Legal numbering | `NumberSequence` exists; no credit-note document type is implemented. | Credit-note numbers must use a future `NumberSequenceDocumentType.CreditNote` at issue time only. Draft/planned records must not consume legal numbers. | Future credit-note issue policy. | Future issue handler. | Number gaps, cancellation, and jurisdiction policy must be explicit. | Archive/source model must store issued number immutably. | No refund effect. | Member display deferred. | Not ready. | Define legal numbering and cancellation policy before schema. |
| Line/tax reversal | Invoice lines have issued financial snapshots and tax totals. | Credit-note lines must derive from issued invoice snapshot/source model, not live catalog, current tax settings, or mutable invoice lines. | Issued invoice snapshot/source model. | Future credit-note line builder. | Posts line-level reversal or credit according to finance policy. | Tax reversal must preserve original tax context and rounding evidence. | Can align with accepted return quantities where applicable. | Unchanged. | Not ready. | Define line source, rounding, and tax reversal rules. |
| Partial credit | Refund and ReturnOrder can be partial. | Partial credit is allowed only with explicit line/amount basis and must not exceed eligible issued invoice amounts after prior credits. | Future credit-note ledger or posting state. | Future credit-note handler. | Requires remaining-credit calculation and posted-credit reconciliation. | Requires cumulative tax reversal control. | May map to partial refund or accepted return quantities. | Unchanged. | Not ready. | Define cumulative credit limits before implementation. |
| Cancellation/void | Invoice cancellation exists with payment restrictions. | Issued credit note cancellation or void must be a finance policy, not a delete or silent status flip. | Future credit-note lifecycle. | Future finance handler. | Requires reversal posting or void policy. | Requires archive/source-model evidence and legal numbering handling. | Refund records remain unchanged. | Unchanged unless exposed later. | Not ready. | Define cancel/void lifecycle with audit evidence. |
| Accounting posting | Posting foundation, account mappings, receivables projection, invoice issue posting, payment posting, refund posting, and invoice-cancellation reversal posting exist. | Credit-note posting must be implemented with the core model and must use issued credit-note source evidence. | Current finance posting foundation plus future credit-note handler. | Future credit-note finance handler. | Foundation ready; credit-note posting implementation pending. | Tax/archive source policy is designed. | Settlement reconciliation can reuse posted refund/payment facts. | No v1 exposure. | Ready for core implementation. | Implement credit-note posting with core model. |
| Receivables impact | Receivable source of truth is posted journal entries; invoice/payment/refund/cancellation entries now exist. | Credit note must reduce receivable through a posted finance entry derived from issued invoice source evidence, not through invoice status text or a negative invoice. | Current finance posting foundation plus future `CreditNote`. | Future credit-note finance handler. | Requires credit-note receivable posting. | Tax and archive reference posting state. | Refund settlement remains separate but reconcilable. | Unchanged. | Foundation ready; credit-note effect not implemented. | Define cumulative credit limits and source evidence before schema. |
| E-invoice/source-model/archive | Invoice archive/source-model/e-invoice generation paths exist for issued invoices. | Credit-note archive/source-model must follow issued invoice discipline. `DocumentRecord` can hold generic metadata but cannot replace legal archive/source-model. | Future credit-note archive/source model. | Future archive/source-model handler. | Posting state must be reflected in source model. | Immutable source, hash, retention, structured JSON/XML, and artifact policy are designed; generated e-invoice artifacts remain blocked until generator readiness. | Can reference refund/return evidence. | Member download deferred. | Ready for internal source/archive implementation. | Implement issued source/archive in core model; keep generated e-invoice artifact separate. |
| Member download visibility | Member invoice downloads are compatibility-sensitive. | V1 stays internal/WebAdmin design. Member download can be added only as an additive compatibility-tested surface. | Future member credit-note projection. | Future public/member handler after policy approval. | No posting effect. | Must expose only issued archived documents. | May show related refund/return context. | Unchanged now. | Not ready. | Decide member visibility after legal archive policy. |
| External accounting/export identity | `ExternalReference` exists as foundation. | External accounting ids, export batch ids, and imported credit-note ids use `ExternalReference`; provider payloads and tokens must not be stored on the credit note. | `ExternalReference`. | Future integration/export handlers. | Supports posting/export reconciliation. | Archive references remain separate from integration ids. | No settlement mutation. | Unchanged. | Ready as a foundation pattern. | Use structured external references in future implementation. |
| Lifecycle event/audit | `BusinessEvent/AuditTrail` exists. | Credit-note lifecycle must write deterministic evidence, but events are not the accounting source of truth. | `BusinessEvent` and `AuditTrail`. | Future credit-note handlers. | Posting remains authoritative. | Archive/source-model remains authoritative. | Can include refund/return ids as safe evidence. | Unchanged. | Ready as a foundation pattern. | Add event/audit only with actual lifecycle implementation. |
| WebAdmin/internal visibility | Sales workspace exists and credit-note actions are intentionally absent. | WebAdmin may expose credit-note actions only in the complete internal core slice with source/archive, posting, tax reversal, numbering, rowversion, authorization, and audit evidence. | Future Finance/Sales WebAdmin workspace. | Future finance handler; Sales can link after readiness. | Prevents UI-only accounting documents. | Prevents non-compliant documents. | Prevents refund/return confusion. | No member exposure. | Ready for complete internal implementation. | Add WebAdmin only with core model, not as a standalone UI shell. |

## Locked CreditNote Decisions

- `CreditNote` is a future formal finance/sales document. It does not replace `Refund`, `ReturnOrder`, `Invoice`, or invoice cancellation.
- `CreditNote` can be implemented only after posting policy, tax reversal policy, legal numbering, immutable source-model/archive, and member visibility policy are defined.
- `Refund` remains the authoritative settlement record. Credit note does not execute settlement; it can link to settlement evidence.
- `ReturnOrder` can provide business reason and goods-return evidence. It does not issue credit notes.
- Credit-note lines must derive from issued invoice snapshot/source model, not live catalog, current tax settings, or mutable invoice lines.
- Credit-note numbering must use a future `NumberSequenceDocumentType.CreditNote`; no enum, schema, or sequence reservation is introduced in this design step.
- Credit-note archive/source-model behavior must follow issued invoice discipline. Generic `DocumentRecord` is not a replacement for legal archive.
- Public WebApi, mobile/member, storefront checkout, payment-intent routes, invoice archive/download, structured source-model, and issued invoice contracts remain unchanged in this design step.
- Member credit-note download is not part of v1. If added later, it must be additive and covered by contract/download compatibility tests.

## Finance Posting Foundation Design Outcome

The finance posting foundation is now designed in [finance-posting-foundation-design.md](finance-posting-foundation-design.md). That design confirms that Darwin already has lightweight `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` entities, and the correct path is to evolve them rather than create a parallel ledger.

- Manual journal entries remain operator-owned Billing records.
- Automated posting requires source linkage, idempotency, posting status, account mapping, reversal policy, and receivable/tax rules before implementation.
- `Invoice`, `Payment`, `Refund`, and `ReturnOrder` remain operational source records; posting consumes their facts and does not replace their mutation owners.
- `CreditNote` remains blocked until the posting foundation is implemented or a review proves equivalent readiness exists.

## Finance Posting Foundation Implementation Outcome

The first finance posting foundation has been implemented in [finance-posting-foundation-design.md](finance-posting-foundation-design.md):

- Existing `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` remain the accounting foundation.
- `JournalEntry` now has source linkage, posting lifecycle/status, deterministic posting key, source document reference, reversal reference, and safe metadata fields.
- `FinancePostingService` provides internal idempotent, balanced, account-validated posting creation.
- Invoice, payment, refund, return order, and credit-note flows are not posted automatically yet.

## Next Implementation Gate

The account mapping and receivables boundary is decision-complete, account mapping is implemented in [finance-account-mapping-receivables-design.md](finance-account-mapping-receivables-design.md), read-only receivables projection is implemented in [finance-receivables-projection.md](finance-receivables-projection.md), and invoice/payment/refund/cancellation posting wiring is implemented through current operational handlers.

## CreditNote Source Model And Archive Design Outcome

Credit-note source model and archive decisions are now locked in [credit-note-source-model-archive-design.md](credit-note-source-model-archive-design.md). This outcome is documentation-only and does not change entities, migrations, routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, refund/payment flows, invoice archive/download behavior, or production code.

- Credit-note source JSON is its own formal document model. It is not a negative invoice JSON.
- Issued credit-note source must include credit-note identity, number, issue date, reason, status, original invoice id/number/hash, credited lines, tax reversal summary, linked refund/return evidence, posting ids, and archive metadata.
- Credit-note lines derive from original issued invoice source evidence, not live catalog, current tax settings, mutable invoice lines, or return-order line text.
- Issued source is immutable. Corrections after issue require void/cancellation/reversal policy, not editing the source.
- `DocumentRecord` may hold supporting evidence, but legal credit-note archive/source remains owned by the credit-note archive model.
- V1 remains internal/WebAdmin. Member/mobile/storefront visibility is not part of core implementation.

## CreditNote Core Model And Admin Implementation Outcome

Darwin now implements the complete internal/WebAdmin `CreditNote` core slice:

- `CreditNote` and `CreditNoteLine` are additive Sales schema entities. No parallel `SalesInvoice`, `FinanceInvoice`, or negative-invoice model was introduced.
- Draft credit notes link to one issued invoice and optional `ReturnOrder`/completed `Refund` evidence.
- Issue reserves `NumberSequenceDocumentType.CreditNote` exactly once and captures immutable source JSON, source hash, archive metadata, issue timestamp, and finance posting id.
- Credit-note lines derive from existing issued invoice line evidence and cumulative quantity validation rejects over-credit against prior issued credit notes.
- Finance posting uses the existing Billing journal foundation: credit-note issue debits sales revenue/tax payable and credits receivables; void creates a reversal posting.
- WebAdmin Sales exposes Credit Notes list/detail/create/issue/cancel/void with anti-forgery and rowversion checks.
- Public WebApi, mobile/member, storefront checkout, payment/refund flows, invoice archive/download behavior, and issued invoice contracts remain unchanged.

## CreditNote Reconciliation And Source Export Hardening Outcome

The reconciliation/source-export gate is now implemented without schema, public WebApi, mobile/member, storefront, payment/refund, invoice archive/download, or issued invoice contract changes.

- Draft create input is normalized so duplicate invoice-line entries become one deterministic credit quantity.
- Completed refund evidence must match the invoice through payment/order/currency reconciliation before it can be linked to a credit note.
- Remaining creditable quantity lookup subtracts prior issued credit notes from the invoice-line quantity and does not treat drafts as official credit consumption.
- Internal WebAdmin source-model download is available only after issue when immutable source JSON and source hash exist. Draft and cancelled records do not export legal source evidence.
- Member visibility remains blocked until a separate additive compatibility-tested policy is accepted.

The next implementation gate is either `Finance Reporting Workspace Design` for internal review of posted receivable facts, or `CreditNote Member Visibility Decision` if customers require member-facing credit-note downloads.
