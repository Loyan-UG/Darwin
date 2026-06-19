# Manufacturing And MRP Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future Manufacturing/MRP boundary before any production, BOM, routing, or planning implementation. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, mobile contract, worker, inventory posting, finance posting, or production flow.

Decision: Manufacturing is a distinct future capability, not a hidden extension of `Product`, `PurchaseOrder`, `WarehouseTask`, or `InventoryTransaction`. It must use the existing inventory ledger and finance posting foundations instead of creating parallel stock or cost ledgers.

## Current Darwin Manufacturing Findings

| Current Darwin model | Finding | Decision |
| --- | --- | --- |
| `Product`, variants, categories, add-ons | Catalog can describe sellable and stockable items, but it is not a bill-of-materials or engineering model. | Future `BillOfMaterial` derives manufacturing inputs; do not overload product add-ons. |
| `InventoryTransaction`, `StockLevel`, reservations, warehouse tasks | Inventory owns stock movement and task execution evidence. | Manufacturing consumption and production receipt must post through inventory handlers with deterministic references. |
| `WarehouseLocation`, `WarehouseTask`, lot/serial/HU docs | Warehouse execution foundation exists for bins, picking, receiving, identity capture, and PWA. | Shop-floor material issue and finished-goods receipt can integrate later, but manufacturing does not own warehouse task ledgers. |
| `PurchaseOrder`, `GoodsReceipt`, supplier invoice | Procurement supplies materials and cost evidence. | MRP can recommend purchase demand but purchase orders remain procurement-owned. |
| `JournalEntry`, finance posting | Finance owns accounting facts and export. | WIP, variance, and production costing require finance design before posting implementation. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Bill of materials | Product-producing customers can define required components for a finished product. | Requires versioned BOM header/lines, effective dates, approvals, and product variant links. |
| Routing and work centers | Operators can plan production steps and capacity. | Requires work centers, routing operations, standard time, sequence, and capacity calendars. |
| Production order | Converts demand into controlled production execution. | Requires planned, released, in-progress, completed, closed, and cancelled lifecycle. |
| Material planning | Calculates component shortages and suggested procurement/production. | Requires MRP snapshot, demand/supply inputs, lead times, safety stock, and no automatic purchase order creation in v1. |
| WIP and costing | Management can see production cost and variance. | Requires finance posting policy before any accounting entry. |

## Future Entity Ownership

| Entity | Owning capability | Key fields | Field class |
| --- | --- | --- | --- |
| `BillOfMaterial` | manufacturing | `BusinessId`, `ProductVariantId`, `BomNumber`, `Version`, `Status`, `EffectiveFromUtc`, `EffectiveToUtc`, `YieldQuantity`, `ScrapPercent`, notes, metadata. | Real columns for identifiers/status/effective dates; JSON for safe low-frequency metadata. |
| `BillOfMaterialLine` | manufacturing | component variant, quantity per, unit, scrap percent, issue warehouse/location preference, sort order. | Real columns. |
| `WorkCenter` | manufacturing | code, display name, status, calendar/capacity hints, cost-center link later. | Real columns; cost-center link after controlling design. |
| `Routing` | manufacturing | product variant, version, status, effective dates. | Real columns. |
| `RoutingOperation` | manufacturing | work center, sequence, standard setup/run time, queue time, instruction evidence. | Real columns plus `DocumentRecord` for instructions. |
| `ProductionOrder` | manufacturing | order number, product variant, planned quantity, due date, status, BOM/routing version snapshots, warehouse. | Real columns. |
| `ProductionOrderMaterialLine` | manufacturing | component, required/issued/scrapped quantity, reservation references. | Real columns. |
| `ProductionOrderOperation` | manufacturing | operation sequence, planned/actual time, status, worker/team evidence. | Real columns. |
| `MrpRun` | manufacturing | business, planning horizon, generated timestamp, status, demand/supply snapshot hash. | Real columns; snapshot JSON bounded and safe. |
| `MrpRecommendation` | manufacturing | source run, item, shortage, suggested production/purchase/transfer, due date. | Real columns. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| BOM/routing | `Draft -> Active -> Superseded` or `Archived`; only active versions can be used for released production orders. |
| Production order | `Draft -> Planned -> Released -> InProgress -> Completed -> Closed`; `Cancelled` allowed before material/final posting; post-release cancellation needs reversal design. |
| Material issue | Only after production order release; issues post negative inventory movement with `ReferenceId = ProductionOrderId`. |
| Finished goods receipt | Only after inspection/complete step if quality is enabled; posts positive inventory movement with `ReferenceId = ProductionOrderId`. |
| MRP run | `Generated -> Reviewed -> Applied` for recommendations; v1 application creates draft recommendations only, not automatic PO or production order mutation. |

## Application Surface

Future handlers:

- Create/update/archive BOM and routing.
- Activate/supersede BOM and routing with number/version validation.
- Create/release/start/complete/close/cancel production order.
- Issue component material through inventory owner.
- Receive finished goods through inventory owner.
- Generate MRP run and recommendations.
- Convert recommendation to draft purchase request or draft production order only after separate workflow design.

All stock mutation must go through inventory handlers. All finance mutation must go through finance posting services after costing design.

## WebAdmin Surface

Manufacturing WebAdmin should include:

- BOM list/detail/create/edit/activate/supersede/archive.
- Routing list/detail/create/edit/activate/supersede/archive.
- Work center list/detail/create/edit/archive.
- Production order list/detail/create/release/start/material issue/complete/close/cancel.
- MRP run list/detail/generate/review recommendations.
- Read-only links to products, stock, purchase orders, warehouse tasks, and journal entries where applicable.

No public/member route is added by default. Mobile-business or PWA shop-floor execution needs a separate scanner/offline design.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `manufacturing-mrp`. |
| Package role | Add-on to Operations/Inventory, package-only unless inventory and catalog are enabled. |
| Required dependencies | `catalog`, `inventory`, `procurement` for material supply, `finance` only when costing/posting is enabled. |
| Disabled behavior | Hide manufacturing nav; direct WebAdmin URL returns `FeatureDisabled`; MRP workers skip runs. |
| Permissions | Separate manage BOM, manage routing, manage production, run MRP, post material issue, post finished receipt. |
| SoD | Activating BOM/routing and releasing/closing production orders are approval candidates. |

## Compatibility Boundaries

- Do not change storefront, checkout, public catalog, payment/refund, invoice archive/download, finance export format, or mobile contracts in the design step.
- Do not create a stock ledger parallel to `InventoryTransaction`.
- Do not create WIP/costing journal entries until production costing policy is designed.
- Do not automatically create procurement documents from MRP recommendations in v1.

## Implementation Slices

1. `Manufacturing Core Master Data Slice`: BOM, routing, work centers, WebAdmin, no stock posting.
2. `Production Order Core Slice`: production order lifecycle, snapshots, WebAdmin, no inventory movement.
3. `Production Inventory Posting Slice`: material issue and finished receipt through inventory reference policy.
4. `MRP Recommendation Slice`: planning run and recommendations, no automatic procurement mutation.
5. `Production Costing Boundary Design`: WIP, variance, finance roles, and export impact before postings.

## Test Plan

Future tests must cover BOM/routing versioning, production lifecycle, inventory idempotency, no parallel ledger, no automatic purchase order creation, WebAdmin anti-forgery and row-version, capability disabled behavior, source guards for no public/mobile exposure, and compatibility smokes for commerce and finance export.

## No Runtime Behavior Changes

This design does not implement Manufacturing/MRP. It records the boundary and implementation order only.
