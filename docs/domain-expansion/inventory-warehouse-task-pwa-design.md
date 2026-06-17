# Inventory Ledger, Warehouse Tasks, And Mobile-First Warehouse PWA Design

## Summary

This document locks the next Inventory and Warehouse expansion boundary before schema, WebAdmin, PWA, mobile, or public contract work. It is a design-only step. No entity, migration, route, DTO, WebAdmin mutation, public WebApi contract, mobile/member contract, storefront flow, finance export format, supplier payment flow, supplier invoice flow, customer payment/refund flow, or invoice archive/download behavior changes in this step.

The professional default is warehouse-first and task-driven: stock changes remain owned by `InventoryTransaction` and specialized handlers, while warehouse execution gets formal structures for locations, bins, tasks, counts, lot/serial evidence, and handling units. The first operator surface for warehouse execution should be a mobile-first PWA because it can serve scanners, tablets, and desktop operators without creating a second native app contract. Native mobile is a later option only if device integration, offline guarantees, or managed hardware requirements prove that a PWA is not sufficient.

## Current Darwin Inventory And Warehouse Findings

- `Warehouse` exists and is business-scoped with name, description, location, default flag, WebAdmin list/edit screens, and lookup support.
- `StockLevel` exists as the current per-warehouse/product availability summary with available, reserved, reorder, and in-transit quantities.
- `InventoryTransaction` exists as the authoritative stock movement ledger for on-hand and reservation changes.
- `StockTransfer` exists with source/destination warehouses, line quantities, lifecycle states, WebAdmin workflow, and stock movement behavior.
- `GoodsReceipt` and `GoodsReceiptLine` exist as formal receiving documents with received, inspected, accepted, rejected, damaged, posted, and cancelled lifecycle boundaries.
- Goods receipt posting delegates accepted quantity stock increase to current inventory movement owners; it does not create a parallel ledger.
- Customer order inventory reservation/allocation logic exists and must remain compatible with current orders, checkout, shipping, payment, return, and mobile/member commerce contracts.
- `WarehouseLocation` now exists as the canonical structured warehouse location/bin hierarchy under the current `Warehouse` foundation, with WebAdmin internal list, tree, create, edit, and archive workflows.
- `WarehouseLabelTemplate` now exists as provider-neutral label readiness for warehouse locations and bins, with WebAdmin template management, preview, browser print, and download package support.
- Inventory movement reference hardening is implemented for current stock owners. Adjustment, reservation, release, order allocation, return receipt/restock, stock transfer lifecycle, goods receipt posting, and legacy purchase-order receive compatibility use a shared reason/reference policy, reject sensitive reason text, and skip already-ledgered reference/warehouse/product rows on retry.
- `WarehouseTask` and `WarehouseTaskLine` now exist as the canonical internal task foundation with lifecycle, assignment, source document linkage, warehouse/location context, quantities, WebAdmin review, and event/audit evidence.
- Goods receipt detail can create formal receiving tasks before posting and directed putaway tasks after posted accepted quantities exist. Putaway completion validates posted goods receipt ownership, receipt line linkage, accepted quantities, and active destination bins.
- Warehouse picking boundaries are locked in [warehouse-picking-task-boundary-design.md](warehouse-picking-task-boundary-design.md). Picking must be allocation-backed and cannot mutate shipment, payment, invoice, refund, finance export, public/mobile, or storefront contracts.
- Stock count boundaries are locked in [stock-count-boundary-design.md](stock-count-boundary-design.md), and the internal/WebAdmin stock count core is implemented. Lot/serial and handling unit boundaries are locked in [lot-serial-handling-unit-boundary-design.md](lot-serial-handling-unit-boundary-design.md), and the internal/WebAdmin lot, serial, tracking policy, handling-unit core model, receipt identity capture, receipt inline identity creation, transfer/count/pick identity integration, bin-level stock derivation, and internal/operator warehouse PWA are implemented. There is no printer-specific adapter, native warehouse mobile app, offline outbox, or public/mobile warehouse contract.
- Supplier contacts and document metadata now exist on the supplier master and must not be duplicated into warehouse task records.
- Supplier invoice, supplier payment, supplier advance, bank/treasury, and finance export foundations exist, but warehouse execution must not create finance postings or payment state by itself.
- `BusinessEvent`, `AuditTrail`, `DocumentRecord`, `ExternalReference`, `NumberSequence`, custom fields, and feature areas exist and should be reused for warehouse evidence and integration identity.

## Decision Matrix

| Warehouse surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Inventory impact | Finance/payables impact | PWA/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Warehouse master | `Warehouse` exists with business scope and WebAdmin CRUD. | Evolve current `Warehouse`; do not add a parallel site/depot model. | Inventory services. | Warehouse handlers. | Parent for bins, stock levels, tasks, counts, and transfers. | None directly. | PWA uses warehouse selection and operator context. | Ready. | Add only common operational fields when core warehouse structure implementation starts. |
| Warehouse zones and bins | `WarehouseLocation` exists as a single hierarchy under `Warehouse`. | Use `WarehouseLocation` as the location/bin master. Do not add separate zone/rack/bin entities or bin-level stock fields in this core model. | Inventory/Warehouse services. | Warehouse location handlers. | Enables bin placement, picking, counting, and task assignment. | None directly. | PWA needs scan/search friendly bin identity. | Complete for current phase. | Harden inventory movement references before task posting. |
| Warehouse labels | `WarehouseLabelTemplate` exists for provider-neutral location/bin labels. | Templates render from `WarehouseLocation` code, display, barcode, warehouse, type, status, and parent code. Browser print and download are allowed; printer-specific push is blocked until a target is selected. | Inventory/Warehouse services. | Warehouse label template handlers. | No stock effect. | None. | PWA can later reuse label identity fields but does not own printing in this slice. | Complete for current phase. | Add printer adapter only after a concrete target is selected. |
| Stock level summary | `StockLevel` exists per warehouse/product and `BinStock` derives bin/identity availability from evidence. | Keep `StockLevel` as summary; do not turn it into the detailed ledger or lot/bin owner. | Inventory services. | Existing stock level and stock movement handlers. | Summary remains fast read model for availability; bin availability is read-only derivation. | None. | PWA can display available/reserved/bin summaries. | Complete for current WebAdmin/internal phase. | Use derived bin stock in PWA execution screens. |
| Bin-level stock | `BinStock` derives location/bin, product, lot, serial-unit, and handling-unit availability from current stock levels plus warehouse task, stock count, and identity evidence. | Bin-level stock is read-only derivation, not a parallel authoritative quantity. Unassigned or inconsistent evidence is surfaced as attention. | Inventory movement projections. | Projection/query handlers. | Preserves single movement authority. | None. | PWA can read derived stock. | Complete for current WebAdmin/internal phase. | Use the projection in warehouse PWA design and execution. |
| Stock movement ledger | `InventoryTransaction` exists and current movement references are hardened. | Keep `InventoryTransaction` as authoritative stock ledger; extend with source context only when necessary. Do not create a second stock ledger. | Inventory movement handlers. | Receipt, transfer, reservation, return, adjustment, and future task posting handlers. | All stock movement remains idempotent and reference-based. | Finance uses postings separately, not inventory ledger rows. | PWA posts through Application handlers, not direct table edits. | Complete for current phase. | Use the hardened policy from task posting handlers. |
| Reservations and allocations | Order allocation/reservation handlers exist. | Existing reservation owners remain authoritative. Warehouse picking tasks use allocation evidence and must not bypass order allocation policy. | Order and Inventory handlers. | Current allocation/reservation handlers plus warehouse picking handlers. | Prevents double-reserve and over-pick. | None. | PWA pick tasks show reserved demand, not create arbitrary demand. | Complete for current WebAdmin/internal phase. | Keep shortage attention as evidence; design stock counts next. |
| Goods receipt integration | `GoodsReceipt` exists and posts accepted quantity. | Goods receipt remains receiving document. Warehouse receiving tasks can execute receipt work but must not replace `GoodsReceipt`. | Goods receipt handlers. | Goods receipt lifecycle handlers and future receiving task handlers. | Receiving task records support execution; goods receipt remains official receipt boundary. | Supplier invoice matching continues to use posted goods receipt evidence. | PWA receiving task can capture received/accepted quantities. | Needs task and bin design. | Implement receiving task after bin/location core. |
| Stock transfer integration | `StockTransfer` exists. | Stock transfer remains official inter-warehouse movement. Warehouse tasks can execute pick/ship/receive steps without creating a parallel transfer model. | Stock transfer handlers. | Stock transfer lifecycle handlers and future transfer task handlers. | Supports directed movement between warehouses/bins. | None. | PWA can guide source pick and destination putaway. | Needs task/bin model. | Add task linkage to stock transfer only after task foundation. |
| Stock counts | `StockCountSession` and `StockCountLine` exist with WebAdmin list/detail/create/update/lifecycle workflow and structured identity evidence. | Formal count documents own count evidence, tracked identity evidence, and reviewed variances. Manual adjustment alone is not a count workflow. | Inventory count services. | Stock count handlers. | Count variance posts through inventory adjustment owner after review and validates required tracked identity. | Finance valuation impact remains separate future accounting design. | PWA supports scan/count entry and review later. | Complete for internal/WebAdmin v1 with identity integration. | Derive bin/identity availability before PWA execution. |
| Lot and serial tracking | Boundary design, internal/WebAdmin core model, receipt capture, and transfer/count/pick integration are complete. | Lot/serial tracking is structured inventory identity, not custom fields, when product policy requires it. | Inventory identity services. | Lot/serial handlers plus receipt/transfer/pick/count posting handlers. | Enables regulated traceability, expiry, recall, and exact-unit stock. | Supplier invoice/payment remains unaffected. | PWA scan workflows must support lot/serial capture when required. | Enforcement complete for current receipt, transfer, count, and pick flows. | Derive bin/identity availability from movement evidence. |
| Handling units | Boundary design, internal/WebAdmin core model, receipt capture, and transfer/count/pick integration are complete. | Handling units are grouped warehouse execution identity; do not fake them as bins or lots. | Warehouse handling-unit services. | Handling unit handlers plus receipt/transfer/pick/count handlers. | Supports grouped movement and putaway/pick efficiency. | None directly. | PWA can scan handling unit labels. | Enforcement complete for current receipt, transfer, count, and pick flows. | Derive bin/handling-unit availability from movement evidence. |
| Warehouse tasks | `WarehouseTask` and `WarehouseTaskLine` exist as the internal task foundation. | Use formal task records with type, status, priority, warehouse, optional bin, source document, assigned operator, quantities, and evidence. Do not use notes or status-only UI. | Warehouse task services. | Task handlers. | Task execution delegates stock impact to stock owners and source document owners. | No finance posting. | Main future PWA execution surface. | Complete for foundation. | Add receiving and putaway execution against goods receipt accepted quantities. |
| Picking and packing | Allocation-backed picking task core exists on `WarehouseTask`; picking shortage attention exists on `WarehouseTaskLine`; packing remains separate. | Picking tasks link to existing order/order line allocation. Short-pick evidence stays on the task and does not auto-cancel, refund, substitute, ship, invoice, or notify. Packing/shipping remains shipment/order owner. | Order fulfillment and warehouse task services. | Pick task handlers plus existing shipment/order handlers. | Prevents over-pick and keeps fulfillment traceable. | None. | PWA pick workflow can scan product/bin and confirm picked and short quantity. | Complete for current picking core and shortage attention. | Design stock count before count adjustments; design packing separately before shipment workflow changes. |
| Putaway | Directed putaway task execution exists from posted goods receipts. | Putaway is a warehouse task evidence workflow over accepted receipt quantity and destination bin. It must not create supplier invoice or payment state or a second stock ledger. | Warehouse task services. | Putaway task handlers. | Records execution evidence for bin destination; stock increase remains owned by goods receipt posting and `InventoryTransaction`. | None. | PWA receiving/putaway flow should be scan-friendly. | Complete for current WebAdmin/internal phase. | Design picking before expanding task execution to fulfillment. |
| Mobile-first PWA | No warehouse PWA exists. | PWA is default for warehouse execution. It gets internal/operator routes and uses existing auth/permissions. Native app is only for proven device/offline constraints. | WebAdmin or dedicated internal warehouse web surface. | Application handlers only. | No direct database writes. | No finance mutations. | PWA supports scanner-friendly flows, responsive layout, offline-safe read/write policy when designed. | Needs UX and route design. | Design PWA route/auth/offline boundary before UI implementation. |
| Offline behavior | No offline warehouse mutation policy exists. | Do not enable offline mutations until outbox, idempotency, conflict resolution, retry visibility, and support tooling are designed. | Future PWA/offline foundation. | Offline outbox processor plus owning Application handlers. | Prevents duplicate stock movement and hidden conflicts. | None directly. | PWA can be online-first v1; offline read-only or queued mutations require separate design. | Not ready. | Keep online-first until offline foundation is explicit. |
| External scanner/hardware | No target hardware selected. | Avoid native/hardware-specific assumptions until scanners, label printers, or rugged devices are selected. | Deployment and warehouse operations design. | Adapter or browser integration only after target selection. | Hardware cannot bypass stock handlers. | None. | PWA can use keyboard-wedge scanners by default. | Conditional. | Design hardware adapter only with real target. |
| Documents and evidence | `DocumentRecord`, `BusinessEvent`, and `AuditTrail` exist. | Warehouse photos, count sheets, damage evidence, and task evidence use foundation primitives. Do not store raw provider/device payloads in metadata. | Foundation services plus warehouse handlers. | Task/count/receipt handlers. | Supports audit without replacing stock ledger. | None directly. | PWA can attach metadata only when document upload path is designed. | Partially ready. | Keep binary upload/download out of first task model unless document flow is designed. |
| External identity and imports | `ExternalReference` exists. | External warehouse, bin, shipment, device, or import ids use `ExternalReference`, not provider-specific columns. | Integration foundation. | External reference service. | Supports later WMS/import integration. | No finance effect. | PWA does not need external ids for v1. | Ready. | Add references only when an import/integration slice exists. |
| WebAdmin/internal visibility | Inventory WebAdmin exists. | WebAdmin remains planning/review surface; PWA becomes execution surface when implemented. Both use Application handlers. | Inventory WebAdmin and future Warehouse PWA. | Existing/future Application handlers. | Keeps operator workflows separated by use case. | No finance shortcuts. | PWA is internal/operator, not public storefront. | Needs route design. | Add WebAdmin review pages and PWA execution routes as separate surfaces. |
| Public/mobile/member boundary | Existing public and member/mobile contracts are stable. | Warehouse PWA is internal/operator-only and must not change member commerce, storefront checkout, public WebApi, or mobile member DTOs. | Internal web surface. | Warehouse handlers. | No customer-facing stock promises change without storefront design. | None. | Not the current Consumer/Business mobile apps. | Ready. | Guard with contracts/mobile smoke on every implementation slice. |

## Locked Decisions

- `InventoryTransaction` remains the authoritative stock movement ledger. Do not create a parallel inventory ledger for bins, tasks, lots, counts, or PWA actions.
- `StockLevel` remains an availability summary, not the detailed stock movement source of truth.
- `Warehouse` evolves in place. Do not add a parallel depot/site/location aggregate that duplicates `Warehouse`.
- Bin/location modeling must happen before bin-level stock, directed receiving, directed picking, stock count by bin, or handling unit workflows.
- Warehouse tasks must be formal records with lifecycle, assignment, quantities, source-document linkage, row-version, events, and audit evidence. Notes or status text are not acceptable substitutes.
- Goods receipt remains the official receiving document. Receiving tasks can execute work, but they do not replace `GoodsReceipt` or supplier invoice matching evidence.
- Stock transfer remains the official inter-warehouse movement document. Transfer tasks can execute pick/putaway work, but they do not replace `StockTransfer`.
- Stock count requires formal count session and line records before count adjustment. Manual adjustment without count evidence is not a count workflow.
- Lot/serial tracking must be structured when required by product policy. Do not hide lot, serial, expiry, recall, or exact-unit identity in custom fields or notes.
- Handling units are separate future warehouse execution records. Do not overload bins, lots, stock levels, or transfer lines to represent pallets or cartons.
- The first warehouse execution surface should be a mobile-first PWA. Native mobile starts only if hardware, managed-device, or offline constraints prove that PWA is insufficient.
- Online-first PWA is acceptable for v1. Offline mutation requires a separate outbox, idempotency, conflict, support, and retry design.
- Keyboard-wedge scanner support is the default hardware assumption. Target-specific scanner, printer, or rugged-device adapters require a concrete target and separate design.
- Supplier contacts and supplier document metadata stay on supplier master. Warehouse tasks should link to supplier, purchase order, goods receipt, or documents instead of copying supplier evidence.
- Warehouse execution must not create supplier invoices, supplier payments, customer payments, refunds, journal entries, bank settlements, finance export packages, customer invoices, archive downloads, or public/mobile member contracts.

## Implementation Sequence

1. `Warehouse Location And Bin Core Model Slice`
   - Complete. `WarehouseLocation` is implemented in Inventory.
   - Keep `Warehouse` as parent and `StockLevel` as summary.
   - Add WebAdmin internal list/detail/create/edit/archive for locations and bins.
   - Do not add task execution, lot/serial, PWA, or stock count in this slice.
2. `Warehouse Label Template And Printing Boundary/Slice`
   - Complete. `WarehouseLabelTemplate` is implemented in Inventory.
   - Add label template and print/download readiness for warehouse locations and bins using existing code/display/barcode fields.
   - Do not add stock mutation, task execution, mobile/PWA routes, or printer-specific integration without a target.
3. `Inventory Movement Reference Hardening Slice`
   - Complete. `InventoryTransaction` reason/reference conventions are hardened for goods receipt, return receipt/restock, stock transfer, order allocation/reservation, release, and adjustment.
   - System-owned movement reasons require aggregate references, sensitive reason text is rejected, and referenced retries do not mutate stock twice.
4. `Warehouse Task Foundation Slice`
   - Complete. `WarehouseTask` and `WarehouseTaskLine` are implemented in Inventory.
   - Application handlers and WebAdmin queue/detail/create/edit/lifecycle surfaces are available.
   - Task records remain execution/evidence records and do not post stock, mutate supplier finance, add PWA routes, or replace source document ownership.
5. `Warehouse Receiving And Putaway Task Slice`
   - Complete. Receiving task evidence and directed putaway task creation are implemented from `GoodsReceipt`.
   - Putaway tasks require posted goods receipt accepted quantities and active destination location before task completion.
   - This slice does not create bin-level stock storage, replace goods receipt ownership, or post finance/supplier/customer mutations.
6. `Warehouse Picking Task Boundary Design`
   - Complete. Allocation-backed picking, order line linkage, source bin validation, shortage evidence, substitution blocking, shipment/packing separation, and customer/finance compatibility are locked.
7. `Warehouse Picking Task Core Slice`
   - Complete. Allocation-backed pick task creation and completion guards are implemented over current order allocation and warehouse task foundations.
   - `WarehouseTask` and `WarehouseTaskLine` remain the picking task model; no parallel picking table was added.
   - Do not create shipment, payment, invoice, refund, supplier finance, finance export, public/mobile, or storefront mutations.
8. `Warehouse Picking Shortage And Fulfillment Attention Slice`
   - Complete. `WarehouseTaskLine` now stores explicit short quantity and reason, queue projections expose shortage attention, and WebAdmin shows shortage counts and line evidence.
   - This slice does not auto-cancel, auto-refund, auto-substitute, mutate shipment, mutate payment, mutate invoice, or change storefront/public/mobile contracts.
9. `Stock Count Boundary Design`
   - Complete. Count session, count line, expected quantity snapshot, counted quantity evidence, variance, approval, adjustment posting, bin/location scope, lot/serial limits, handling-unit limits, and evidence policy are locked.
   - No schema, mutation, public/mobile, storefront, finance, supplier invoice/payment, customer payment/refund, or archive/download behavior changed in this design step.
10. `Stock Count Core Model And Admin Slice`
   - Complete. `StockCountSession` and `StockCountLine` are implemented in Inventory with WebAdmin list/detail/create/update/lifecycle workflow.
   - Approved variances post only through existing Inventory adjustment handlers and idempotent movement references.
   - Public/mobile/storefront, finance export, supplier invoice/payment, customer payment/refund, and archive/download behavior remain unchanged.
11. `Lot Serial And Handling Unit Boundary Design`
   - Complete. Product tracking policy, lot identity, serial unit identity, expiry, recall, quarantine, handling unit identity, contents, receipt/transfer/count/pick boundaries, labels, evidence, and compatibility guards are locked.
12. `Lot Serial And Handling Unit Core Model And Admin Slice`
   - Complete. Product tracking policy, lot identity, serial unit identity, handling unit identity, and handling unit content records are implemented for internal WebAdmin use.
13. `Receipt Identity Capture Slice`
   - Complete for link-existing receipt evidence. Goods receipt inspection captures structured `GoodsReceiptLineIdentity` records and posting enforces active product tracking policy for lot, serial, expiry, supplier lot, and handling-unit evidence.
   - Public/mobile/storefront, finance, supplier, customer payment/refund, and invoice archive/download flows remain unchanged.
14. `Receipt Inline Identity Creation Slice`
   - Complete. Goods receipt inspection can create missing lot, serial unit, and handling-unit records in place and link them immediately to structured receipt identity evidence.
   - Inline creation keeps stock posting in the receipt post handler and does not add public/mobile/storefront or finance/supplier/customer mutations.
15. `Transfer Count Pick Identity Integration Slice`
   - Complete. `StockTransferLineIdentity`, `StockCountLineIdentity`, and `WarehouseTaskLineIdentity` preserve lot, serial-unit, and handling-unit evidence through transfer, count, and picking flows.
   - Transfer dispatch/receive, stock count approval/posting, and picking completion enforce active product tracking policies before tracked quantities move forward.
   - This slice does not create bin stock storage, PWA/mobile routes, public contracts, finance/supplier/customer mutations, or a second stock ledger.
16. `Bin-Level Stock Derivation Slice`
   - Complete. Internal/WebAdmin `BinStock` derives location/bin, product, lot, serial-unit, and handling-unit availability from current stock levels plus warehouse task, stock count, and identity evidence.
   - Unassigned quantity and negative evidence are surfaced as attention instead of rewriting movement history.
   - No schema, migration, authoritative bin stock table, public/mobile route, finance/supplier/customer mutation, or second stock ledger is added.
17. `Warehouse Mobile-First PWA Slice`
   - Complete. Internal/operator `WarehousePwa` is implemented as an online-first WebAdmin surface over existing warehouse task lifecycle handlers and read-only derived bin stock attention.
   - It supports scan/search-friendly filtering, task cards, start/complete/cancel lifecycle actions with anti-forgery and row-version protection, and links back to the full task detail for detailed identity capture.
   - It does not add an offline outbox, service worker mutation queue, native app contract, public/member mobile routes, direct database writes, stock ledger shortcuts, or finance/supplier/customer mutations.

## Compatibility And Guardrails

- Public WebApi, mobile/member, storefront checkout, customer invoice archive/download, finance export, supplier invoice, supplier payment, customer payment, refund, and credit-note flows stay unchanged in this design step.
- WebAdmin remains MVC/Razor plus HTMX for review and configuration surfaces.
- PWA routes are internal/operator WebAdmin routes, not public storefront routes and not Consumer/Business mobile DTO changes.
- Every implementation slice must run contracts and mobile smoke lanes for `Order|Invoice|Checkout|Commerce` and `ApiRoutes|MemberCommerceService`.
- Sensitive warehouse evidence must never store provider credentials, access tokens, scanner secrets, connection strings, raw device payloads, or raw provider payloads in metadata, events, audit, external references, document metadata, logs, tests, or docs.

## Test Strategy For Implementation Slices

- Unit tests for warehouse/bin validation, duplicate code guards, archive rules, same-business ownership, row-version checks, and sensitive metadata rejection.
- Unit tests for task lifecycle, assignment, quantity limits, source document linkage, idempotent stock posting, and invalid transition rejection.
- Unit tests for goods receipt task integration, stock transfer task integration, order allocation/pick integration, and prevention of duplicate inventory transactions.
- Infrastructure tests for Inventory schema placement, required fields, enum conversions, JSON mapping, indexes, unique filters, and PostgreSQL/SQL Server migrations.
- WebAdmin tests for render, anti-forgery, row-version, HTMX/full-page behavior, and absence of supplier invoice/payment, finance, public/mobile, or storefront shortcuts.
- PWA tests for responsive scan-first forms, keyboard scanner input behavior, online-first mutation flow, no direct database writes, and no member/mobile DTO expansion.
- Compatibility smoke for contracts and mobile member commerce after every warehouse implementation slice.
