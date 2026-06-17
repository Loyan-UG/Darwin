# Lot Serial And Handling Unit Boundary Design

## Summary

This document locks the exact-unit and grouped-stock boundary before any lot, serial, expiry, recall, handling unit, receipt capture, transfer capture, count capture, pick capture, WebAdmin mutation, PWA route, public WebApi contract, mobile/member contract, storefront behavior, supplier invoice/payment flow, finance export format, customer payment/refund flow, or invoice archive/download behavior changes.

The professional default is structured traceability. Lot, serial, expiry, and handling unit identity must be first-class inventory evidence when product policy requires it. They must not be hidden in metadata, custom fields, notes, SKU text, task lines, count lines, or receipt descriptions.

## Current Darwin Lot Serial And Handling Unit Findings

- `ProductVariant` is the current catalog identity used by stock levels, receipts, transfers, warehouse tasks, stock counts, order lines, delivery notes, supplier invoices, and inventory transactions.
- `StockLevel` is the current per-warehouse/product availability summary. It has no lot, serial, handling unit, expiry, recall, or bin-level quantity identity.
- `InventoryTransaction` is the authoritative stock movement ledger. Current movement rows are warehouse/product scoped with reason/reference idempotency and do not carry exact-unit identity.
- `WarehouseLocation` is the structured warehouse/bin/location master. It is not a handling unit, pallet, carton, lot, or serial container.
- `WarehouseLabelTemplate` provides location/bin label readiness. It does not provide product lot, serial, pallet, carton, or case label policy.
- `GoodsReceipt` and `GoodsReceiptLine` are the formal purchasing receipt boundary and post accepted product variant quantity.
- `StockTransfer` and `StockTransferLine` are the formal inter-warehouse movement boundary at product variant quantity level.
- `WarehouseTask` and `WarehouseTaskLine` are internal execution/evidence records for receiving, putaway, picking, and shortage attention.
- `StockCountSession` and `StockCountLine` are the formal count document and reviewed variance boundary at product variant quantity level.
- No canonical lot master, serial unit, handling unit, product tracking policy, expiry policy, recall workflow, exact-unit stock derivation, or handling unit label model exists yet.
- Public WebApi, mobile/member, storefront checkout, supplier invoice/payment, finance export, customer payment/refund, and invoice archive/download flows are compatibility-sensitive and remain unchanged by this design.

## Locked Decisions

- Future lot and serial tracking is Inventory-owned structured identity, not custom fields, notes, SKU suffixes, metadata JSON, warehouse task text, stock count notes, or supplier document text.
- Future handling units are Warehouse/Inventory execution records for grouped stock such as pallet, carton, tote, case, or container. They are not warehouse locations, lots, serial numbers, stock levels, or transfer lines.
- Product tracking policy must be explicit per product variant or product family before exact-unit capture is enforced. Default untracked products continue to operate at product variant quantity level.
- Supported tracking modes for design are: untracked, lot-tracked, serial-tracked, lot-and-expiry tracked, serial-and-expiry tracked, and handling-unit tracked for grouped movement. The implementation can model these as policy values, but every stock owner must respect the policy.
- Lot identity is reusable quantity identity. Multiple units can share one lot, optional expiry, optional manufacture date, optional supplier lot code, and recall status.
- Serial identity is unique unit identity. A serial number can be received, transferred, picked, counted, shipped, quarantined, scrapped, returned, or recalled, but it must not be represented only as quantity text.
- Handling unit identity groups stock for movement and scanning. A handling unit can contain tracked or untracked product quantities, but it does not replace lot/serial identity when those are required by product policy.
- Exact-unit stock impact must flow through `InventoryTransaction` or a formally linked inventory identity movement record. A parallel stock ledger is not allowed.
- Receipt capture is the first authoritative point for supplier lot, serial, expiry, and handling unit evidence. If a product requires tracking, goods receipt posting must not create stock without required identity evidence after implementation.
- Transfer and pick flows must preserve identity. A transfer cannot silently lose lot/serial/handling unit evidence, and picking cannot substitute tracked identity without explicit handler validation.
- Stock counts for tracked products require structured lot/serial/handling unit count lines or links after implementation. Exact-unit count data must not be stored in generic notes.
- Recall and quarantine require structured identity and status/evidence. They must not be implemented as negative stock adjustments or hidden warehouse task states.
- Expired stock handling is policy-driven and must be explicit. Expiry evidence alone does not automatically scrap, reserve, ship, refund, or finance-post without owning handlers.
- Documents, photos, certificates, and supplier evidence use `DocumentRecord`; external lot, serial, handling unit, or WMS/import ids use `ExternalReference`.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, customer payment/refund, supplier invoice/payment, and bank/treasury flows remain unchanged by this design.

## Decision Matrix

| Surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Inventory impact | Finance/payables impact | PWA/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Product tracking policy | Product and variant exist without tracking mode. | Add explicit tracking policy before enforcing lot/serial capture. | Catalog plus Inventory policy services. | Product/Inventory handlers. | Determines when identity is required for receipt, transfer, pick, count, and return. | None directly. | PWA can show required scan fields later. | Ready for core design. | Include policy in core model. |
| Lot identity | No lot model exists. | Future `InventoryLot` or equivalent is a structured quantity identity with supplier lot, expiry, manufacture date, status, and evidence. | Inventory identity services. | Lot handlers and receipt/import owners. | Enables expiry, quarantine, recall, and lot-level availability. | Supplier invoice matching remains quantity/cost based. | Scan/display later. | Ready after policy. | Implement with core identity model. |
| Serial identity | No serial model exists. | Future `InventorySerialUnit` or equivalent is a unique unit identity linked to product variant and optional lot. | Inventory identity services. | Serial handlers and receipt/import owners. | Enables exact-unit movement, warranty, recall, and shipment traceability. | None directly. | PWA scan required for serial-tracked products. | Ready after policy. | Implement only as structured identity. |
| Expiry | No inventory expiry model exists. | Expiry is part of lot/serial identity and product policy. It must not be embedded in SKU text or notes. | Inventory identity services. | Receipt and identity handlers. | Enables FEFO, quarantine, and expiry attention. | Valuation/write-off is future finance design. | PWA can warn later. | Needs core identity. | Add expiry fields to identity, not stock level. |
| Recall | No recall workflow exists. | Recall uses structured lot/serial identity and evidence. It does not rewrite shipment/order history. | Future recall services. | Recall handlers. | Flags impacted stock and shipped evidence. | Credit/refund impact requires sales/finance owners. | Later operator workflow. | Blocked until identity model. | Design recall workflow after identity core. |
| Handling unit identity | No handling unit model exists. | Future handling unit is grouped warehouse execution identity with barcode, status, parent/child relation, and contents. | Warehouse identity services. | Handling unit handlers. | Supports pallet/carton/tote movement without faking locations. | None directly. | PWA scan candidate. | Ready after boundary. | Include in core model or separate slice if scope grows. |
| Handling unit contents | No content model exists. | Contents must reference product quantity and optional lot/serial identity. | Warehouse identity services. | Handling unit content handlers. | Prevents grouped stock ambiguity. | None. | PWA can display contents. | Needs identity model. | Add content lines only with handling unit core. |
| Receipt capture | `GoodsReceiptLine` records accepted product variant quantity. | Tracked products require identity evidence before posting in future implementation. | Goods receipt and identity handlers. | Goods receipt lifecycle plus identity capture handlers. | Stock increase remains receipt-owned and ledger-backed. | Supplier invoice matching still uses posted receipt evidence. | PWA receiving scan later. | Needs identity core. | Extend receipt posting after model exists. |
| Transfer capture | `StockTransferLine` moves product variant quantity. | Transfer must preserve required identity evidence for tracked products. | Stock transfer handlers. | Transfer lifecycle handlers. | Prevents identity loss between warehouses/bins. | None. | PWA transfer scan later. | Needs identity core. | Extend transfer lifecycle after model exists. |
| Picking capture | `WarehouseTaskLine` captures picked/short product quantity. | Picking tracked products must validate and preserve lot/serial/handling unit evidence. | Warehouse task handlers plus allocation owners. | Picking handlers. | Prevents wrong-lot or wrong-serial fulfillment. | None. | PWA pick scan later. | Needs identity core and allocation policy. | Design pick identity after core model. |
| Stock count capture | `StockCountLine` captures expected/count/variance by variant. | Tracked-product counts require structured lot/serial/handling unit evidence after implementation. | Stock count handlers. | Count lifecycle handlers. | Variance can be identity-aware without a second ledger. | None directly. | PWA count scan later. | Needs identity core. | Extend count lines after identity model. |
| Bin-level stock | `WarehouseLocation` exists; no bin stock table exists. | Bin-level stock must be derived from ledger and identity movement evidence, not stored as a parallel authoritative quantity. | Inventory movement projections. | Projection/query handlers. | Preserves single movement authority. | None. | PWA can read derived stock. | Needs movement identity linkage. | Design projection after identity movement. |
| Quarantine and quality hold | `WarehouseLocationType.QualityHold` exists. | Quality hold location is physical/operational; identity status handles lot/serial quarantine. | Inventory identity services and locations. | Identity status handlers and task handlers. | Allows stock isolation without losing traceability. | Write-off/revaluation later. | PWA can route putaway later. | Needs identity status. | Add status fields in core identity. |
| Labeling | Location labels exist. | Product lot, serial, and handling unit labels need their own template/printing policy. | Inventory/Warehouse label services. | Future label handlers. | Supports scan identity. | None. | PWA can scan generated barcodes. | Later. | Do not overload location labels. |
| Documents and certificates | `DocumentRecord` exists. | Certificates, quality photos, recall docs, and supplier lot documents use `DocumentRecord`. | Foundation documents. | Document handlers plus identity owner. | Evidence without raw payloads in metadata. | None directly. | PWA attachments need separate upload design. | Ready for metadata links. | Add links only with document flow. |
| External identity | `ExternalReference` exists. | External WMS, supplier, manufacturer, or import ids use `ExternalReference`. | Integration foundation. | External reference service. | Provider-neutral identity. | None. | PWA unaffected. | Ready. | Use in import/adapter slices. |
| Finance/export impact | Finance posting/export exists. | Identity creation and movement evidence do not change finance export format. Valuation/write-off is a separate design. | Finance posting/export services. | Finance handlers only. | No automatic accounting from identity status. | No supplier payment/invoice mutation. | None. | Guarded. | Keep finance unchanged in core identity slice. |
| WebAdmin/internal visibility | Inventory WebAdmin exists. | First implementation is internal WebAdmin for identity setup and review. | Inventory WebAdmin. | Inventory handlers. | Operator control and audit. | No finance shortcuts. | PWA later for execution. | Ready. | Build after design. |
| Public/mobile/storefront boundary | Existing contracts are stable. | No public/mobile/member/storefront exposure in identity core. | Internal inventory services. | Internal handlers. | No customer-facing stock promise change. | None. | Member apps unchanged. | Ready as guard. | Run compatibility smoke on implementation. |

## Lifecycle Boundaries

### Lot

| State | Meaning | Allowed impact |
| --- | --- | --- |
| `Draft` | Lot identity is being prepared before official stock use. | No available stock unless linked receipt posts. |
| `Active` | Lot can be used by receipt, transfer, count, and pick flows subject to expiry/status policy. | Stock movement through existing inventory owners. |
| `Quarantined` | Lot is held for quality, recall, damage, or compliance review. | Blocks pick/ship unless policy permits override. |
| `Expired` | Lot passed expiry policy date. | Blocks normal pick/ship; scrap/write-off needs separate owner. |
| `Recalled` | Lot is subject to recall evidence. | Blocks outbound movement and supports traceability review. |
| `Closed` | Lot is no longer active for stock operations. | History remains immutable. |

### Serial Unit

| State | Meaning | Allowed impact |
| --- | --- | --- |
| `Received` | Serial was captured at receipt or import. | Available only if stock movement owner posted receipt. |
| `Available` | Serial can be allocated, transferred, counted, or picked. | Movement through existing owners. |
| `Reserved` | Serial is tied to operational demand. | Allocation owner controls release. |
| `Picked` | Serial is picked for fulfillment. | Shipment/order owner still controls shipping. |
| `Shipped` | Serial left inventory through fulfillment. | Return/recall evidence can link back. |
| `Quarantined` | Serial is blocked for quality/recall/compliance. | No normal pick/ship. |
| `Scrapped` | Serial is removed from usable stock by approved owner. | Requires movement/posting design. |

### Handling Unit

| State | Meaning | Allowed impact |
| --- | --- | --- |
| `Open` | Contents can be assembled or changed before closure. | No stock effect by itself. |
| `Closed` | Contents are fixed for movement/counting. | Can be moved or counted as a group. |
| `InTransit` | Handling unit is part of a transfer or shipment workflow. | Source owner controls movement. |
| `Received` | Handling unit arrived at destination. | Destination owner controls putaway. |
| `BrokenDown` | Handling unit has been unpacked and contents continue independently. | History remains traceable. |
| `Cancelled` | Handling unit record was stopped before use. | No stock effect. |

## Implementation Direction

1. `Lot Serial And Handling Unit Core Model And Admin Slice`
   - Add explicit product tracking policy for product variants or product families.
   - Add structured lot identity, serial unit identity, handling unit identity, and handling unit content records.
   - Add internal WebAdmin list/detail/create/update/archive or status actions.
   - Add event/audit evidence and safe metadata validation.
   - Do not change public/mobile/storefront contracts, finance export format, supplier invoice/payment flows, customer payment/refund flows, or invoice archive/download behavior.
2. `Receipt Identity Capture Slice`
   - Require lot/serial evidence for tracked goods receipt lines before posting accepted quantity.
   - Preserve supplier lot, expiry, serial, and handling unit evidence from receipt snapshots.
3. `Transfer Count Pick Identity Integration Slice`
   - Extend stock transfer, stock count, and warehouse picking flows to preserve required identity evidence.
   - Keep `InventoryTransaction` as the stock movement ledger.
4. `Recall And Expiry Operations Slice`
   - Add recall/quarantine/expiry attention and blocked movement policy after identity core and movement integration are stable.

## Compatibility And Guardrails

- This design step adds no schema, migration, route, DTO, WebAdmin mutation, public WebApi contract, mobile/member contract, storefront flow, finance export format, supplier invoice/payment flow, customer payment/refund flow, or invoice archive/download behavior.
- Future implementation must not create a parallel stock ledger, hide lot/serial/handling unit identity in metadata, overload `WarehouseLocation` as a pallet/carton, or overload SKU/name text as traceability.
- `InventoryTransaction` remains the stock movement authority. Future identity records provide traceability and movement context, not a second source of quantity truth.
- Product tracking policy must be explicit before receipt, transfer, count, or pick handlers require identity capture.
- Sensitive evidence must never store credentials, access tokens, refresh tokens, private keys, connection strings, raw scanner payloads, raw provider payloads, or raw device payloads in metadata, events, audit, external references, document metadata, logs, tests, or documentation.

## Test Strategy For Implementation

- Unit tests for product tracking policy enforcement and untracked product compatibility.
- Unit tests for lot and serial creation, duplicate identity rejection, expiry/quarantine/recall status guards, and sensitive metadata rejection.
- Unit tests for handling unit content validation, parent/child cycle rejection, breakdown behavior, and status transitions.
- Unit tests proving receipt, transfer, count, and pick flows cannot bypass required identity for tracked products after integration slices.
- Infrastructure tests for Inventory schema placement, enum conversions, JSON mapping, indexes, unique active keys, PostgreSQL JSON behavior, and SQL Server storage.
- WebAdmin tests for render, anti-forgery, row-version, HTMX/full-page behavior, and absence of finance, supplier payment, public/mobile, storefront, customer payment/refund, or invoice archive/download shortcuts.
- Compatibility smoke for public contracts and mobile member commerce after implementation.
