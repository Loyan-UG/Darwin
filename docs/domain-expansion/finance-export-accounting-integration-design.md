# Finance Export And Accounting Integration Design

## Summary

This document locks the finance export and accounting integration boundary before any export table, file generator, API connector, WebAdmin action, or background job is implemented. This step is documentation-only. It does not add entities, migrations, routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, invoice archive/download behavior, payment/refund flows, credit-note behavior, or production code.

The decision is explicit: accounting export must package posted Darwin accounting facts from the current `JournalEntry` foundation. It must not export mutable operational documents directly, recompute finance from live order/invoice state, create provider-specific fields on finance documents, or store raw connector payloads and credentials in Darwin records.

## Current Darwin Finance Export Findings

- `JournalEntry` and `JournalEntryLine` are the current accounting posting source of truth.
- `FinancePostingService` writes idempotent, source-linked posted entries for automated finance facts.
- `FinanceAccountMappingService` resolves business-scoped posting roles and fails closed when required mappings are missing or incompatible.
- `ReceivablesProjectionService` calculates open receivables from posted accounting entries and mapped receivable accounts.
- Finance reporting exposes WebAdmin overview, receivables, postings, and account-mapping readiness without creating a second journal editor.
- `ExternalSystem`, `ExternalReference`, and `SourceOfTruth` exist as structured integration primitives.
- `BusinessEvent` and `AuditTrail` exist for lifecycle evidence, but journal entries remain the financial source of truth.
- Issued invoice and credit-note source/archive records remain authoritative compliance surfaces. Export must reference them, not replace them.
- No accounting export batch, export status, export file, connector sync state, or conflict-resolution UI is currently implemented.

## Export Boundary Decision Matrix

| Export surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Export identity | Accounting impact | Tax/archive impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Export source | Source-linked `JournalEntry` and `JournalEntryLine` records exist. | Export posted accounting entries only. Do not export draft entries or recompute from `Order`, `Invoice`, `Payment`, `Refund`, `ReturnOrder`, or `CreditNote` operational state. | Billing journal foundation. | Future finance export service. | Export rows reference journal entry ids and posting keys. | Keeps export aligned with the ledger. | References issued document evidence without replacing archives. | Unchanged. | Ready for export design; code pending. | Implement export package builder over posted entries. |
| Business scope | Accounts and mappings are business-scoped. | V1 export is one business per export batch. Cross-business export is blocked until global accounting ownership and currency consolidation are designed. | `Business`, `FinancialAccount`, account mappings. | Future finance export service. | Batch key includes business id. | Prevents mixing account charts and currencies. | Keeps tax/archive references scoped. | Unchanged. | Decision locked. | Implement business selector and business-scoped query. |
| Period boundary | No accounting period lock model is implemented. | Export requires an explicit UTC date range over posting entry dates. Period locks are not required for first export, but exported entries must be immutable enough to support future lock checks. | `JournalEntry.EntryDateUtc` and posting metadata. | Future export service; future period-lock service. | Batch key includes date range and filter hash. | Prevents open-ended exports. | Supports later closed-period policy. | Unchanged. | Ready with documented gap. | Design period-lock slice before strict close. |
| Posting status filter | Posting status exists. | Export only `Posted` and effective reversal entries according to a documented mode. Draft and voided entries are excluded. Reversed entries must be represented explicitly so the recipient can reconcile prior exports. | `JournalEntry.PostingStatus`. | Future export service. | Export line includes posting status and reversal reference. | Preserves audit trail and correction history. | Avoids destructive export edits. | Unchanged. | Decision locked. | Encode status filter and reversal representation. |
| Idempotency | `PostingKey` exists for automated postings. | Export batch generation must be idempotent by business, period, status mode, and target external system. Re-running the same batch returns the existing batch unless an explicit new revision is requested. | Future export batch model plus `PostingKey`. | Future export service. | Batch id and external reference are deterministic for the batch context. | Prevents duplicate export packages. | Archive references stay stable. | Unchanged. | Requires implementation. | Add `FinanceExportBatch` only when implementing export. |
| Exported entry identity | `ExternalReference` exists. | External posting ids, batch ids, and target-side document ids use `ExternalReference`. Do not add provider-specific id columns to `JournalEntry`, `Invoice`, `CreditNote`, `Payment`, or `Refund`. | `ExternalReference`. | Future export/integration handlers. | Reference kind distinguishes batch, entry, and target document references. | Keeps integration identity structured. | Does not alter legal document metadata. | Unchanged. | Foundation ready. | Use external reference service in export implementation. |
| File export format | No export file model exists. | V1 should support a neutral internal export package with deterministic CSV or JSON payload generated from posted entries. The file is evidence of export, not a second ledger. | Future export package service. | Future finance export service. | File metadata links to export batch id. | Preserves journal as source. | References source document ids and hashes where relevant. | Unchanged. | Design locked; format choice still implementation-level. | Implement one deterministic package format first, then add connector-specific adapters. |
| API connector export | Integration primitives exist, no connector sync engine is implemented. | API push is a later adapter over the same export batch model. It must not bypass batch idempotency, posted-only filters, or external references. | Future integration adapter. | Future export connector service. | Connector response ids stored as external references or sync records. | Prevents direct handler-to-provider coupling. | Provider payloads stay outside legal archive fields. | Unchanged. | Not ready for code. | Implement file/package export before API push unless a customer integration requires API first. |
| Error handling | No export failure model exists. | Failed export attempts must be visible and retryable without creating duplicate successful batches. Errors store safe summaries only; raw provider payloads, credentials, tokens, and private keys are rejected. | Future export attempt record. | Future export service. | Failed attempts link to batch context. | Preserves retry evidence. | Avoids leaking provider data into archive/compliance records. | Unchanged. | Requires implementation. | Add attempt/status model with safe metadata if export is implemented. |
| Conflict handling | `SyncState` and `SyncConflict` foundation exists. | V1 export is outbound-only. Conflicts from target systems are not modeled until a concrete inbound sync or two-way reconciliation target is designed. | Sync/conflict foundation. | Future target-specific integration service. | No conflict ids in v1 file-delivery export. | Avoids false bidirectional sync promises. | Unchanged. | Unchanged. | Foundation ready; target-specific sync blocked until a real target exists. | Keep export one-way unless two-way requirements are explicit. |
| Account mapping readiness | WebAdmin account mapping page exists. | Export readiness requires active compatible mappings for the postings being exported. Missing mappings block generated accounting packages that need account codes. | `FinanceAccountMappingService`. | Finance configuration and export service. | Batch records readiness result. | Prevents unmapped account exports. | Tax lines require tax payable mapping. | Unchanged. | Ready. | Reuse mapping readiness checks in export builder. |
| Source document references | Invoice, credit-note, payment, refund, and return evidence exist. | Export rows reference source entity type/id/document number and posting key. They do not embed invoice archive payloads, credit-note source JSON, provider payloads, or member-facing download paths. | Journal source linkage plus document records. | Future export service. | Source references remain internal ids plus safe display numbers. | Keeps export traceable. | Legal source/archive remains in its owning document. | Unchanged. | Ready. | Include safe source metadata in export row projection. |
| Corrections and reversals | Reversal posting support exists. | Corrections export as separate reversal or adjustment entries, not destructive replacements of prior exported rows. If a prior exported entry is reversed later, the later reversal appears in a later export batch. | `JournalEntry` reversal fields. | Finance posting and export services. | Reversal links reference original posting. | Preserves accounting audit trail. | Maintains document source history. | Unchanged. | Decision locked. | Build reversal tests before export implementation. |
| Tax evidence | Issued source evidence exists for invoices and credit notes. | Export consumes posted tax lines and references source document evidence. It must not recalculate tax from current settings, product catalog, or mutable invoice lines. | Posted entries plus issued source models. | Finance posting and export services. | Tax export rows reference source posting/document ids. | Prevents tax drift. | Legal source model remains authoritative. | Unchanged. | Decision locked. | Include source hash/reference fields when present. |
| WebAdmin visibility | Finance reporting workspace exists. | Export UI, when implemented, belongs in Finance and must be explicit: preview, readiness, generate batch, download package, retry failed attempt. It must not create invoices, refunds, payments, credit notes, or journal entries. | Finance WebAdmin. | Future finance export handlers. | UI displays batch ids and status only. | Keeps mutation ownership clean. | Archive/download behavior unchanged. | Unchanged. | UI pending. | Implement after service and tests exist. |
| Mobile/member exposure | Mobile/member finance export does not exist. | No mobile/member/storefront exposure for accounting export. Exports are operator/internal only. | WebAdmin/internal services. | Finance WebAdmin. | None. | No customer-facing accounting export contract. | Member invoice/credit-note downloads remain separate. | Unchanged. | Decision locked. | Keep public/mobile contracts unchanged. |

## Locked Export Decisions

- Export source of truth is posted `JournalEntry` and `JournalEntryLine`, not mutable operational documents.
- V1 export is business-scoped and date-range-scoped.
- Draft, voided, deleted, or unbalanced data must not be exported.
- Reversals and corrections export as explicit entries, not destructive edits to prior exported rows.
- Export identity and external target ids use `ExternalReference`; provider-specific columns are not added to accounting or sales documents.
- Export attempts store safe summaries only. Secrets, access tokens, refresh tokens, private keys, raw connector payloads, and provider credentials are forbidden.
- Issued invoice and credit-note archives remain legal/compliance authorities. Export packages reference them but do not replace them.
- API connector export is an adapter over the same batch model. It must not bypass batch idempotency or posted-only filters.
- V1 remains internal/WebAdmin only; public WebApi, mobile/member, storefront checkout, payment-intent, invoice archive/download, and issued document contracts remain unchanged.

## Future Implementation Shape

The complete implementation should be split into cohesive slices without leaving half-built behavior:

1. `Finance Export Batch Foundation Slice`
   - Add structured export batch and attempt records.
   - Record business id, external system id, date range, status mode, package hash, status, generated-at timestamp, and safe metadata.
   - Enforce idempotency for the same business, target, date range, and filter mode.
2. `Finance Export Package Builder Slice`
   - Build deterministic posted-entry packages from `JournalEntry` and `JournalEntryLine`.
   - Include source identity, posting key, account codes, debit/credit amounts, currency, entry date, reversal references, and safe source document numbers.
   - Exclude drafts, voided entries, deleted entries, raw archives, provider payloads, and secrets.
3. `Finance Export WebAdmin Slice`
   - Add Finance workspace pages for readiness, preview, generate, download, and retry.
   - Keep UI internal/operator-only with anti-forgery, authorization, and no operational document mutations.
4. `Finance Export Connector Adapter Slice`
   - Add provider-neutral delivery orchestration over stored packages.
   - Store target ids and batch ids through `ExternalReference` and safe attempt records.
5. `Finance Export Target Adapter Slice`
   - Implement the selected file-delivery adapter after destination profile, hash verification, idempotency, provider error policy, and smoke strategy are locked.

## Finance Export Batch Foundation Outcome

`Finance Export Batch Foundation Slice`

Implemented scope:

- Added `FinanceExportBatch` and `FinanceExportAttempt` in the existing `Billing` finance foundation.
- Added lifecycle enums for export batch status, posting-status filter mode, and attempt status.
- Added provider mappings and migrations for PostgreSQL and SQL Server.
- Batch identity is business-scoped, target-accounting-system-scoped, period-scoped, and posting-status-mode-scoped.
- Batch creation is idempotent by deterministic export key and active unique index.
- Export attempts store safe retry evidence, generated package metadata, success/failure timestamps, and safe error summaries.
- `FinanceExportBatchService` validates target `ExternalSystem` existence, active accounting-system kind, date range, metadata safety, attempt state transitions, and sensitive-data rejection.
- PostgreSQL maps export metadata to `jsonb`; SQL Server keeps provider-neutral string storage.
- No export package builder, connector push, WebAdmin export action, public WebApi route, mobile/member route, storefront contract, invoice archive/download change, payment/refund mutation, credit-note mutation, or journal-entry editor change was introduced.

## Finance Export Package Builder Outcome

`Finance Export Package Builder Slice`

Implemented scope:

- Added an internal deterministic package builder for existing finance export batches.
- The canonical v1 package format is provider-neutral JSON built from posted `JournalEntry` and `JournalEntryLine` records.
- Package generation records attempt evidence through the existing finance export batch service.
- Package output includes batch header data, posting metadata, source entity identity, account code/name/type, debit/credit amounts, entry and line counts, totals, and a SHA-256 package hash.
- `PostedOnly` exports posted entries only. `PostedAndReversed` exports posted and reversed entries.
- Draft, voided, deleted entries, and deleted lines are excluded.
- Empty periods generate a valid zero-entry package for auditable no-activity periods.
- Generated, delivered, and cancelled batches are rejected for package regeneration.
- Safe-text validation blocks sensitive posting/source metadata from entering the package.
- No file storage, download route, WebAdmin export action, connector push, public WebApi route, mobile/member route, storefront contract, invoice archive/download change, payment/refund mutation, credit-note mutation, or journal-entry editor change was introduced.

## Finance Export WebAdmin And Durable Package Storage Outcome

`Finance Export WebAdmin Slice`

Implemented scope:

- Added durable package storage for generated finance export packages. The canonical JSON package is written to object storage before a batch can be completed as generated.
- Added `DocumentRecord` metadata for stored packages with `EntityType = FinanceExportBatch`, internal visibility, evidence document kind, content hash, object container, and object key.
- Added a Finance `Exports` WebAdmin page for internal operators to select business, accounting target, date range, posting-status mode, create or resolve an export batch, generate a package, and download an already stored package.
- Package generation is all-or-nothing: if storage is unavailable, object write fails, or document metadata registration fails, the attempt is marked failed and the batch is not completed as generated.
- Download reads the stored package through `DocumentRecord` and object storage. It does not rebuild generated batches.
- The preferred object storage profile is `FinanceExports`; if that profile is not configured, the active object storage configuration is used, but database-backed storage blocks generation for finance export packages.
- WebAdmin export actions stay internal/operator-only with anti-forgery on posts. They do not add connector push, public WebApi routes, mobile/member routes, storefront changes, operational document mutations, invoice/payment/refund/credit-note flow changes, journal editing, or export regeneration for generated batches.

## Finance Export Target Adapter Smoke/Operations Hardening Outcome

`Finance Export Target Adapter Smoke/Operations Hardening`

Implemented scope:

- Verified the first real file-delivery connector target through filesystem object-storage smoke coverage.
- Confirmed `FinanceExports` is the stored package source and `FinanceExportOutbound` is the outbound delivery destination.
- Confirmed production push remains blocked when `FinanceExportOutbound` is missing, invalid, or database-backed.
- Confirmed same-key/same-hash destination delivery is idempotent and same-key/different-hash delivery fails without marking the batch delivered.
- Added deployment documentation for finance export source and outbound profiles without adding credentials, connector configuration UI, accounting API delivery, package regeneration, journal editing, public/mobile/storefront contracts, invoice/payment/refund mutations, or credit-note behavior.

Next gate:

- `Finance Export Accounting API Target Design Slice` only if a real accounting target, credential owner, payload mapping, and error contract are selected.
- If no accounting API target is selected, file-delivery remains the production-safe export delivery path.
- Future accounting API target selection should follow [finance-export-accounting-api-target-selection-design.md](finance-export-accounting-api-target-selection-design.md), prioritize several widely used German accounting products, and define credential owner, payload mapping, target-side ids, retry/error contract, sync/conflict behavior, and smoke strategy before any API adapter implementation.

## Finance Export WebAdmin Connector Push Outcome

`Finance Export WebAdmin Connector Push Slice`

Implemented scope:

- Added internal Finance WebAdmin push action for generated batches that already have stored package evidence.
- Push calls the provider-neutral connector delivery service and never rebuilds the package.
- Push readiness is read-only and target-specific; without a registered adapter the UI shows a blocked state and no fake delivery is possible.
- Delivered batch timestamp and target receipt/reference display come from batch state and `ExternalReference`.
- Production DI intentionally does not register a no-network adapter.
- No real connector, credential entry, connector configuration UI, public WebApi route, mobile/member route, storefront change, invoice/payment/refund/credit-note mutation, journal editor, schema/migration, or package regeneration path was added.

## Finance Export File-Delivery Target Adapter Design Outcome

`Finance Export File-Delivery Target Adapter Design Slice`

Decision scope:

- Selected file-delivery as the first real export connector target.
- The future `FinanceExportFileDeliveryAdapter` copies only the stored canonical JSON package to a configured outbound destination.
- `FinanceExportOutbound` is the preferred outbound storage profile.
- Delivery success requires object write completion and SHA-256 hash verification.
- Same-key same-hash retry is idempotent success; same-key different-hash retry is failure.
- Target-side delivery receipt uses `ExternalReferenceKind.Export`.
- No accounting API adapter, connector credential UI, target browsing UI, package regeneration, schema/migration, public/mobile/storefront contract, journal editor, invoice/payment/refund mutation, or credit-note mutation was added.

## Finance Export File-Delivery Adapter Outcome

`Finance Export File Delivery Adapter Slice`

Implemented scope:

- Added `FinanceExportFileDeliveryAdapter` as the first real outbound connector adapter.
- File-delivery copies only the stored canonical JSON package to the configured `FinanceExportOutbound` destination.
- Destination object naming is deterministic by business id, accounting target id, export period, and batch id.
- Delivery success requires completed object write and SHA-256 hash verification.
- Existing same-key/same-hash destination objects are idempotent success; same-key/different-hash objects fail delivery and keep the batch retryable.
- WebAdmin push remains blocked unless the outbound profile is configured with a non-database provider.
- Target-side delivery receipt still uses `ExternalReferenceKind.Export`.
- No schema/migration, accounting API adapter, connector credential UI, target browsing UI, package regeneration, public/mobile/storefront contract, journal editor, invoice/payment/refund mutation, or credit-note mutation was added.
