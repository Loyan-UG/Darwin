# Stock Count Boundary Design

## Summary

This document locks the stock count boundary before any stock count schema, WebAdmin workflow, PWA route, stock adjustment mutation, lot/serial expansion, public contract, mobile/member contract, storefront behavior, finance export format, supplier invoice/payment flow, customer payment/refund flow, or invoice archive/download behavior changes.

The professional default is evidence-first counting. A stock count is a formal internal inventory document that captures counted quantity, variance, review, approval, and adjustment evidence. It is not a manual stock adjustment screen, not a note on `StockLevel`, not a warehouse task status, and not a replacement for the `InventoryTransaction` ledger.

## Current Darwin Stock Count Findings

- `Warehouse` exists as the business-scoped warehouse master.
- `WarehouseLocation` exists as the structured location and bin hierarchy under `Warehouse`.
- `WarehouseLabelTemplate` exists for provider-neutral location/bin label readiness.
- `StockLevel` exists as the current per-warehouse/product availability summary.
- `InventoryTransaction` is the authoritative stock movement ledger and current movement references are hardened through a shared reason/reference policy.
- Adjustment, reservation, release, order allocation, return receipt/restock, stock transfer lifecycle, goods receipt posting, and legacy purchase-order receive compatibility use idempotent movement references and reject sensitive reason text.
- `GoodsReceipt` is the formal stock increase boundary for purchasing receipt and inspection.
- `WarehouseTask` and `WarehouseTaskLine` exist as internal task evidence for receiving, putaway, picking, and picking shortage attention.
- `StockCountSession` and `StockCountLine` now exist as the internal Inventory stock count document, with WebAdmin list/detail/create/update/lifecycle workflow, expected quantity snapshots, variance review, approval, and idempotent adjustment posting through existing Inventory movement owners.
- No bin-level stock derivation, lot/serial identity, handling unit identity, or stock count PWA contract exists yet.
- Public WebApi, mobile/member, storefront checkout, supplier invoice/payment, finance export, customer payment/refund, and invoice archive/download flows are compatibility-sensitive and stay unchanged by this design.

## Locked Decisions

- Future `StockCountSession` is the formal stock count header. It is owned by Inventory and scoped by business, warehouse, optional location/bin, count type, lifecycle status, assigned operator, and count window.
- Future `StockCountLine` is the formal count line. It captures product variant, optional warehouse location, expected quantity snapshot, counted quantity, variance, reviewer decision, and adjustment eligibility.
- `StockLevel` remains a summary. It is not the stock count source of truth and it is not edited directly by the count UI.
- `InventoryTransaction` remains the only authoritative stock movement ledger. Approved variances post through the existing adjustment owner using a dedicated reason such as `StockCountAdjustment` and `ReferenceId = StockCountSessionId`.
- Count sessions do not post stock movement while draft, in progress, counted, or review pending. Stock changes are allowed only after review and approval.
- Expected quantity is a snapshot taken when the count session is prepared or started. It must not be recomputed live during approval in a way that hides variance.
- Counted quantity is operator-entered evidence. It must be non-negative and row-version protected.
- Variance is explicit and reviewable. Zero-variance lines can be approved without adjustment; non-zero variance lines require reviewer approval before any stock movement.
- Adjustment posting is full-line and idempotent in v1. Partial variance posting and repeated recount cycles require explicit session state and evidence.
- Bin/location count is supported by linking to `WarehouseLocation`; this design does not create bin-level stock storage.
- Non-catalog lines are not stock count lines. Stock count is for stock-tracked product variants only.
- Lot/serial and handling unit fields are not hidden in metadata. If exact-unit tracking is required, it must be designed in the later lot/serial and handling unit boundary.
- PWA can later capture count entries, but v1 design remains online-first. Offline count mutation requires outbox, idempotency, conflict, retry, and support visibility design.
- Count documents, photos, and evidence use `DocumentRecord` only after attachment flow design. Metadata must remain safe and must not store raw scanner payloads, device secrets, provider payloads, credentials, tokens, private keys, or connection strings.
- External count import ids use `ExternalReference`, not provider-specific stock count columns.
- WebAdmin is the first internal review surface. Public WebApi, member/mobile, storefront, customer invoice archive/download, finance export package format, customer payment/refund, supplier invoice/payment, and bank/treasury flows remain unchanged.

## Decision Matrix

| Stock count surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Inventory impact | Finance/payables impact | PWA/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Count trigger | `StockCountSession` exists. | Create formal sessions only through Inventory handlers. | Inventory count services. | Stock count create/update handlers. | Creates count evidence only. | None. | Future PWA can list assigned sessions. | Complete for WebAdmin/internal v1. | Keep public/mobile contracts unchanged. |
| Count scope | `Warehouse` and `WarehouseLocation` exist. | Scope by business, warehouse, optional location/bin, and optional product filter. | Inventory services. | Stock count handlers. | Enables full, cycle, bin, and product counts without new warehouse models. | None. | PWA can filter by warehouse/bin. | Ready. | Keep one session model. |
| Expected quantity | `StockLevel` is summary; `InventoryTransaction` is ledger. | Store expected quantity snapshot on count line when session starts or is prepared. | Inventory count services. | Stock count preparation handler. | Prevents live recompute from hiding variance. | None. | PWA displays expected quantity according to permissions. | Ready. | Define snapshot timing in core model. |
| Counted quantity | No canonical count evidence exists. | Operator enters non-negative counted quantity on line. | Inventory count services. | Count entry handler. | Evidence only until approved. | None. | PWA scan/count entry later. | Ready. | Add line-level row-version and validation. |
| Variance | Manual adjustment exists separately. | Variance is explicit: counted minus expected. It is reviewed before posting. | Inventory count services. | Review/approve handlers. | Prevents blind stock edits. | Valuation impact is future finance design. | PWA can display variance after count. | Ready. | Add review status and variance fields. |
| Approval | No stock count approval exists. | Non-zero variance requires reviewer approval. Zero variance can close without adjustment. | Inventory count services. | Approval handler. | Controls stock movement eligibility. | None directly. | Internal only. | Ready. | Add lifecycle statuses. |
| Adjustment posting | `AdjustInventoryHandler` and hardened movement policy exist. | Approved variance posts through existing inventory adjustment owner with `ReferenceId = StockCountSessionId`. | Inventory movement handlers. | Dedicated stock count post handler calling adjustment owner. | Creates idempotent `InventoryTransaction`; updates stock through existing path. | No journal entry or finance export change in this slice. | PWA must call handler, not write stock. | Ready after core model. | Implement in stock count core/admin slice. |
| Idempotency | Movement reference policy exists. | Retry must not create duplicate adjustment rows for the same session/warehouse/product/location scope. | Inventory movement policy. | Stock count post handler. | Prevents double stock movement. | None. | Supports retry-safe PWA later. | Ready. | Use shared reference policy. |
| Recount | No count sessions exist. | v1 allows line update before approval; after approval, correction requires a new count session or formal reversal design. | Inventory count services. | Count handlers. | Preserves audit history. | None. | PWA can submit corrections before approval. | Ready. | Keep post-approval edit blocked. |
| Bin/location | `WarehouseLocation` exists; no bin stock store exists. | Count can be scoped to a location/bin, but bin-level stock remains derived from ledger and future movement references. | Inventory count services. | Count handlers plus movement owner. | No parallel bin ledger. | None. | PWA can scan bin barcode. | Ready. | Store optional location id on count line. |
| Lot/serial | No canonical lot/serial model exists. | Do not add lot/serial fields to stock count metadata. Exact-unit count waits for lot/serial design. | Future inventory identity services. | Future lot/serial handlers. | Avoids unstructured traceability. | None. | Future PWA scan support. | Blocked until design. | Run lot/serial boundary after stock count core. |
| Handling units | No handling unit model exists. | Do not fake pallets/cartons as bins or count notes. | Future handling unit services. | Future handling unit handlers. | Avoids grouped-stock ambiguity. | None. | Future PWA scan support. | Blocked until design. | Run handling unit boundary after stock count core. |
| Documents and evidence | `DocumentRecord`, `BusinessEvent`, and `AuditTrail` exist. | Use shared primitives for evidence. Do not store raw payloads in metadata. | Foundation services plus count handlers. | Count handlers. | Auditable count lifecycle. | None. | PWA attachments require document flow design. | Ready for metadata; binary later. | Emit event/audit for lifecycle. |
| External identity | `ExternalReference` exists. | External count/import ids use `ExternalReference`. | Integration foundation. | External reference service. | Import-ready without provider columns. | None. | PWA unaffected. | Ready. | Add references only with import slice. |
| WebAdmin visibility | Inventory WebAdmin has Stock Counts pages. | WebAdmin owns first list/detail/create/count/review/post surface. | Inventory WebAdmin. | Stock count handlers. | Internal operator workflow. | No finance shortcuts. | PWA remains future execution surface. | Complete for WebAdmin/internal v1. | Design exact-unit tracking next. |
| Public/mobile/storefront boundary | Existing contracts are stable. | Stock count stays internal/operator only. | Inventory services. | Internal handlers. | No customer-facing stock promise change. | No payment/invoice effect. | No member mobile DTO change. | Ready as guard. | Run compatibility smoke for implementation. |

## Lifecycle

| Status | Meaning | Allowed next states | Stock impact |
| --- | --- | --- | --- |
| `Draft` | Session is created but not prepared for counting. | `Prepared`, `Cancelled` | None. |
| `Prepared` | Expected quantity snapshots exist and count can start. | `InProgress`, `Cancelled` | None. |
| `InProgress` | Operators enter counted quantities. | `Counted`, `Cancelled` | None. |
| `Counted` | Count entry is complete and ready for review. | `ReviewPending`, `Cancelled` | None. |
| `ReviewPending` | Variances are being reviewed. | `Approved`, `Rejected`, `Cancelled` | None. |
| `Approved` | Variance decisions are approved and eligible for posting. | `Posted` | None until post command. |
| `Posted` | Approved variance has been posted through inventory adjustment owner. | none in v1 | Inventory movement through `InventoryTransaction`. |
| `Rejected` | Count evidence is rejected and will not post. | none in v1 | None. |
| `Cancelled` | Session is stopped before posting. | none in v1 | None. |

## Implementation Direction

1. `Stock Count Core Model And Admin Slice` - complete for internal/WebAdmin v1.
   - `StockCountSession` and `StockCountLine` are in Inventory.
   - Lifecycle handlers cover create/update/prepare/start/count/review/approve/reject/post/cancel plus detail and paged list.
   - WebAdmin list/detail/create/update/lifecycle forms use row-version and anti-forgery.
   - Approved variances post only through current inventory adjustment owner and shared idempotent movement reference policy.
   - Public WebApi, member/mobile DTOs, storefront behavior, finance export changes, supplier invoice/payment changes, customer payment/refund changes, and invoice archive/download behavior remain unchanged.
2. `Lot Serial And Handling Unit Boundary Design`
   - Lock exact-unit tracking, expiry, recall, serial capture, lot capture, and grouped handling unit policy before adding those fields to count, receipt, transfer, or pick flows.
3. `Warehouse Mobile-First PWA Slice`
   - Add internal/operator online-first scan UX after count, picking, receiving, and putaway handlers are stable.
   - Keep offline mutations blocked until outbox/conflict/retry/support design exists.

## Compatibility And Guardrails

- This design step adds no schema, migration, route, DTO, WebAdmin mutation, public WebApi contract, mobile/member contract, storefront flow, finance export format, supplier payment flow, supplier invoice flow, customer payment/refund flow, or invoice archive/download behavior.
- Stock count implementation must not create a parallel stock ledger, bin stock table, lot/serial table, handling unit table, finance posting, supplier invoice/payment shortcut, customer payment/refund mutation, or public/mobile route.
- Count adjustment must be idempotent and reference-backed. A retry must not move stock twice.
- Sensitive evidence must never store credentials, access tokens, refresh tokens, private keys, connection strings, raw scanner payloads, raw provider payloads, or raw device payloads in metadata, events, audit, external references, document metadata, logs, tests, or documentation.

## Test Strategy For Implementation

- Unit tests for session create/prepare/start/count/review/approve/post/cancel lifecycle and invalid transitions.
- Unit tests for expected snapshot capture, counted quantity validation, variance calculation, zero-variance close, non-zero variance approval, and rejected count behavior.
- Unit tests for idempotent adjustment posting with `ReferenceId = StockCountSessionId` and no duplicate `InventoryTransaction` rows on retry.
- Unit tests for same-business warehouse/location/product validation, row-version checks, soft-deleted records, and sensitive metadata rejection.
- Infrastructure tests for Inventory schema placement, required fields, enum conversions, JSON mapping, indexes, unique filters, PostgreSQL JSON behavior, and SQL Server storage.
- WebAdmin tests for render, anti-forgery, row-version, HTMX/full-page behavior, and absence of supplier invoice/payment, finance, public/mobile, storefront, customer payment/refund, or invoice archive/download shortcuts.
- Compatibility smoke for contracts and mobile member commerce after implementation.
