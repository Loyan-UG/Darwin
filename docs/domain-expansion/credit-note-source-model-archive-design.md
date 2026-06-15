# CreditNote Source Model And Archive Design

This document locks the source-model, archive, tax, numbering, visibility, and compatibility policy for future `CreditNote` implementation. This step is documentation-only. It does not add entities, migrations, routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, refund/payment flows, invoice archive/download behavior, or production code.

The decision is explicit: a `CreditNote` is a formal finance/sales document with immutable issued source evidence. It must not be implemented as a negative invoice, a refund view, a return-order printout, a `DocumentRecord` attachment, or a UI-only document.

## Current Darwin Invoice Archive Findings

- `Invoice` is the current issued-document foundation for member invoice history, archive, structured source-model JSON/XML, e-invoice readiness, retention, purge, and download paths.
- `IssuedSnapshotJson` and `IssuedSnapshotHashSha256` are the authoritative issued invoice source evidence when present.
- Invoice archive storage is behind `IInvoiceArchiveStorage`; database, file-system, and object-storage providers preserve the same Application boundary.
- Structured invoice JSON/XML exports are generated from the issued invoice snapshot. They are not live recomputations from mutable catalog, tax, customer, or order state.
- `EInvoiceSourceReadinessValidator` and `GenerateInvoiceEInvoiceArtifactHandler` reject incomplete or invalid issued invoice source evidence instead of producing fake compliant artifacts.
- Member invoice archive, structured JSON, and structured XML routes are compatibility-sensitive and remain unchanged.
- `DocumentRecord` can track generic document metadata, but it is not the legal archive/source-model authority for issued invoice-like documents.
- Finance posting now exists for invoice issue, payment, refund, and invoice-cancellation reversal. Credit-note-specific posting remains unimplemented until source/archive policy is complete.

## Decision Matrix

| Credit-note source/archive surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Accounting/posting impact | Tax/source impact | Archive/download impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Source document type | `Invoice` has issued source evidence; `CreditNote` does not exist. | Add a future formal `CreditNote` document only after this policy is implemented. Do not reuse `Invoice` with negative totals and do not create parallel `SalesInvoice` or `FinanceInvoice`. | Future `CreditNote` aggregate linked to current `Invoice`. | Future credit-note issue handler. | Posts its own receivable/revenue/tax reversal. | Source derives from issued invoice evidence. | Has its own immutable archive/source model. | No v1 member exposure. | Ready for core design, not code in this step. | Implement `CreditNote Core Model And Admin Slice` only with archive/source fields included. |
| Trigger basis | Refunds, invoice cancellation, ReturnOrder, and commercial corrections exist separately. | Credit note can be started from a finance-approved reason: accepted return, post-issue correction, commercial credit, or cancellation correction. It is not created automatically by every refund. | Future credit-note policy over current evidence records. | Future finance handler. | Requires explicit posting reason and source entity links. | Reason controls line eligibility and tax reversal basis. | Reason is part of immutable source model. | Unchanged. | Decision locked. | Encode reason enum and validation in future core model. |
| Original invoice linkage | `Invoice` has `IssuedSnapshotJson`, archive metadata, and totals. | Every issued credit note must link to exactly one issued invoice. The original invoice financial fields and issued snapshot remain immutable. | Current `Invoice`. | Future credit-note issue handler. | Credit posting references original invoice context. | Line/tax data comes from original issued evidence. | Credit archive references original invoice id/number/hash. | Unchanged. | Decision locked. | Require original invoice readiness before issue. |
| Line source | Invoice lines and structured exports exist. | Credit-note lines must be derived from original issued invoice source data, with optional quantity/amount reductions. They must not be computed from live catalog, current tax settings, mutable invoice lines, or ReturnOrder line names alone. | Issued invoice source model. | Future credit-note line builder. | Prevents over-credit and wrong revenue/tax reversal. | Preserves original tax category, rate, rounding, currency, and line identity. | Lines are captured into credit-note source JSON. | Unchanged. | Decision locked. | Define line source DTOs in core slice. |
| Partial credit limit | Refunds and returns can be partial; prior credit tracking does not exist. | Future implementation must calculate remaining creditable quantity/amount per original invoice line from prior issued credit notes and posted reversal entries. It must reject over-credit. | Future `CreditNote` records plus finance postings. | Future credit-note issue handler. | Prevents duplicate receivable/revenue/tax reversal. | Prevents cumulative tax over-reversal. | Source model records prior-credit basis. | Unchanged. | Needs implementation with core model. | Add cumulative credit validation in core slice. |
| Tax reversal | Invoice source model contains tax-summary and line tax evidence. | Credit-note tax reversal must use original invoice source tax evidence and explicit credited quantities/amounts. It must not use current tax settings or provider payloads. | Issued invoice source model plus future credit-note source model. | Future credit-note issue handler. | Debits tax payable according to original tax basis. | Keeps original tax rate/category/rounding evidence. | Structured source includes tax reversal summary. | Unchanged. | Decision locked. | Include tax reversal builder and tests in core slice. |
| Legal numbering | `NumberSequence` exists; `CreditNote` document type is not implemented. | Draft/planned credit notes do not consume legal numbers. Issuing a credit note reserves `NumberSequenceDocumentType.CreditNote` exactly once. | Future `CreditNote` issue policy. | Future issue handler. | Posting uses issued credit-note number as source document number. | Number appears in immutable source model. | Archive/source model stores issued number. | Member display remains disabled in v1. | Needs enum/schema implementation. | Add document type with core model. |
| Draft vs issued source | Invoice draft/source behavior is separate from issued snapshot. | Draft credit notes may have editable planning data. Issued credit notes must capture immutable `IssuedSourceJson`, `IssuedSourceHashSha256`, `IssuedAtUtc`, archive metadata, and posting id. | Future `CreditNote`. | Future issue handler. | Posting happens only when issue preconditions pass. | Draft changes do not affect issued evidence. | Archive generated only from issued source. | Unchanged. | Decision locked. | Implement draft/update/issue boundaries together. |
| Archive storage | Invoice archive storage is provider-routed behind Application abstractions. | Credit-note archive should reuse the same storage-provider pattern but with credit-note-specific source keys and metadata. It must not store raw storage paths in UI. | Future credit-note archive service. | Future archive/source handler. | Archive metadata references posting/source id. | Source hash ties archive to issued evidence. | Supports database/internal fallback and object-storage providers through the same boundary discipline. | No v1 member download. | Ready for design; code pending. | Create credit-note archive abstraction or extend archive router intentionally in core/archive slice. |
| Structured JSON/XML | Invoice structured exports exist from issued invoice source. | Credit-note structured JSON/XML must be generated from issued credit-note source evidence and original invoice source references. It must identify itself as a credit note, not an invoice with negative values. | Future credit-note source export handler. | Future read/export handlers. | Includes posting/source reference but does not post. | Carries original and credited tax basis. | Internal/WebAdmin v1 only. | No route now. | Needs implementation. | Add internal exports with tests before member exposure. |
| Generated e-invoice artifact | Invoice e-invoice generator boundary exists but is not production-ready. | Credit-note e-invoice artifact generation is blocked until invoice e-invoice generation is production-ready and credit-note profiles are validated. It must not fake artifacts. | Future credit-note e-invoice handler. | Future compliant artifact handler. | No posting effect. | Requires credit-note-specific readiness validation. | No generated artifact button until real generator exists. | Unchanged. | Blocked. | Keep artifact generation outside core until validator/generator is ready. |
| Member visibility | Member invoice downloads exist and are compatibility-sensitive. | V1 credit notes remain internal/WebAdmin. Member visibility can be added later only as additive routes/DTOs with serialization, source-route, and mobile service tests. | Future member projection. | Future public/member handler. | No accounting mutation. | Exposes only issued archived documents. | Does not reuse invoice download paths. | Unchanged now. | Decision locked. | Do not add member routes in core slice. |
| DocumentRecord relationship | `DocumentRecord` exists for generic metadata. | `DocumentRecord` may reference credit-note attachments or supporting evidence, but legal credit-note source/archive is owned by credit-note archive fields/services. | Future credit-note plus `DocumentRecord`. | Future document handler. | No posting authority. | Supporting evidence only. | Not a substitute for issued source archive. | Unchanged. | Decision locked. | Keep attachments separate from source archive. |
| Refund linkage | `Refund` is settlement owner and posting now records refund settlement. | Credit note can link to completed refund evidence but must not create or complete refunds. Refund amount does not define tax reversal by itself. | Current `Refund`. | Existing refund handlers. | Settlement and credit posting stay separate but reconcilable. | Tax reversal comes from credit-note source. | Refund references can appear in source metadata. | Unchanged. | Decision locked. | Include optional refund links in core model. |
| ReturnOrder linkage | `ReturnOrder` provides inspection and accepted quantity evidence. | ReturnOrder can justify goods-return credit quantities, but credit-note issue still requires finance approval and issued invoice source validation. | Current `ReturnOrder`. | ReturnOrder handlers remain return owners. | Return evidence can support posting basis. | Accepted quantities can map to original invoice lines only through validated linkage. | Return evidence can be referenced, not copied as archive authority. | Unchanged. | Decision locked. | Add guarded return-order reference in future core model. |
| Event/audit | `BusinessEvent/AuditTrail` exists. | Credit-note lifecycle must record deterministic events/audit, but journal entries and source archive remain authoritative for accounting/legal evidence. | Future credit-note handler plus foundation event/audit. | Future credit-note handlers. | Events describe posting, issue, void/cancel, and archive creation. | Events never replace source hash/archive. | Safe metadata only. | Unchanged. | Ready pattern. | Add with lifecycle implementation. |
| External accounting/export identity | `ExternalReference` exists. | External credit-note ids, export batch ids, and imported references use `ExternalReference`. Provider payloads, tokens, and raw accounting exports are not stored on the credit note. | Future integration/export handlers. | Future integration handlers. | Supports accounting export reconciliation. | Does not alter legal source archive. | External ids are separate from archive keys. | Unchanged. | Ready pattern. | Use existing external reference service. |
| Cancellation/void | Invoice cancellation reversal exists; credit-note void policy does not. | Issued credit-note cancellation/void must be a finance reversal policy with audit and archive evidence, not delete or silent status change. | Future `CreditNote`. | Future finance handler. | Requires reversal or void posting. | Must preserve original issued source and cancellation evidence. | Archive remains retained; void evidence is additive. | Unchanged. | Needs design inside core/lifecycle slice. | Define statuses and reversal posting in core model. |

## Locked Source Model Decisions

- Credit-note source JSON must be its own document model, not a negative invoice JSON.
- Issued credit-note source must include credit-note id, number, issue date, currency, reason, status, original invoice id/number/hash, seller, buyer, credited lines, tax reversal summary, totals, linked refund ids, linked return-order id when applicable, posting ids, and archive metadata.
- Credited lines must preserve original invoice line identity, original description/SKU where available, original quantity/amount/tax basis, credited quantity/amount, tax rate/category, rounding evidence, and remaining-credit validation result.
- Source JSON is immutable after issue. Any correction after issue requires a credit-note void/cancellation/reversal policy, not editing the issued source.
- Structured JSON/XML exports must be generated from the issued credit-note source, not from mutable current entities.
- Archive hash must be calculated from the issued source payload or canonical archive payload and stored with the credit note.
- Archive retention and purge behavior must follow issued invoice discipline, with credit-note-specific metadata and tests.
- `DocumentRecord` can attach supporting evidence but cannot replace issued credit-note source/archive.
- Member/mobile/storefront exposure remains unchanged in this design step and is not part of v1 core implementation.

## Core Implementation Requirements

The future `CreditNote Core Model And Admin Slice` must include these pieces together:

- `CreditNote` and `CreditNoteLine` structured entities with draft and issued boundaries.
- `NumberSequenceDocumentType.CreditNote` and issue-time number reservation.
- Links to original issued invoice, optional refunds, optional ReturnOrder, and optional external references.
- Cumulative credit-limit validation against original issued invoice source and prior issued credit notes.
- Tax reversal builder based on original issued invoice source evidence.
- Finance posting through mapped receivables, sales revenue, tax payable, and rounding accounts when required.
- Immutable issued source JSON and hash capture at issue time.
- Credit-note archive/source export service for internal/WebAdmin v1.
- Deterministic `BusinessEvent/AuditTrail` evidence.
- WebAdmin internal list/detail/create/update/issue/cancel/void actions with authorization, anti-forgery, rowversion, and no member/public route changes.

## Next Implementation Gate

`CreditNote Reconciliation And Source Export Hardening`

Current implementation outcome:

- The formal internal/WebAdmin credit-note model is implemented with source/archive fields included from the start.
- V1 remains internal/WebAdmin only.
- Member/mobile/storefront routes and DTOs were not added.
- Negative invoices, cosmetic documents, and parallel invoice/order models were not introduced.
- Generated e-invoice artifacts remain blocked until the invoice e-invoice generator path is production-ready and credit-note validation is added.

Next scope:

- Harden cumulative amount/tax edge cases beyond quantity validation.
- Add internal structured source export/readiness tests for issued credit notes.
- Keep member visibility blocked until additive download contracts and compatibility tests are explicitly designed.

## CreditNote Reconciliation And Source Export Hardening Outcome

Darwin now hardens the implemented internal/WebAdmin credit-note surface without adding schema, public WebApi, mobile/member, storefront, payment/refund, invoice archive/download, or issued invoice contract changes.

- Credit-note create input aggregates duplicate invoice-line rows deterministically instead of throwing raw dictionary exceptions or creating duplicate line records.
- Refund evidence linked to a draft credit note must be a completed, active refund with payment/order/currency consistency against the linked invoice. A completed refund from another order or currency is rejected.
- WebAdmin auto-populates credit-note line quantities from remaining creditable issued-invoice quantities. Prior issued credit notes reduce the proposed quantity, while draft credit notes do not consume official credit capacity.
- Issued and voided credit notes expose an internal WebAdmin source-model JSON download generated from the stored immutable source model and hash. Draft and cancelled notes do not expose a source export.
- The source export is internal/WebAdmin only. Member/mobile/storefront visibility remains blocked until a separate additive compatibility-tested design is approved.
- No negative invoice, parallel invoice, refund mutation, payment mutation, or generic document-record replacement was introduced.

Next gate:

- `CreditNote Member Visibility Decision` only after legal/archive/export policy is accepted for customers who need member-facing credit-note downloads.
- Otherwise continue to `Finance Reporting Workspace Design` so posted invoice/payment/refund/credit-note facts can be reviewed without adding new mutation owners.
