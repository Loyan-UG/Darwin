# Transportation And Logistics Planning Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future transportation/logistics planning boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, carrier adapter, worker, mobile contract, shipment flow, or finance flow.

Decision: Transportation planning is broader than current shipping provider operations. Existing DHL/shipping flows remain shipment execution/provider operations; a future TMS-style module would own shipment planning, consolidation, route/load planning, and freight cost evidence.

## Current Darwin Logistics Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| `ShippingMethod`, `Shipment`, DHL operation handlers | Carrier rating/label/provider operations exist for order shipment execution. | Keep as shipping execution; do not overload for route/load planning. |
| Warehouse tasks and locations | Warehouse picking/putaway/locations exist. | Logistics planning can read warehouse readiness but warehouse tasks own internal movement. |
| Orders and delivery notes | Customer fulfillment documents exist. | Transportation can consolidate shipments after sales/shipping ownership design. |
| Finance and supplier invoices | Freight cost can be supplier invoice evidence later. | Freight accrual/payment needs finance/payables boundary. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Load consolidation | Operators can group shipments by route/carrier/date. | Requires transport load header and shipment/load lines. |
| Route planning | Supports delivery sequence and vehicle/driver planning. | Requires route, stop, vehicle, driver/resource links. |
| Freight cost estimate | Captures planned carrier cost or internal fleet cost. | Requires cost snapshot and supplier invoice matching later. |
| Delivery exceptions | Tracks missed delivery, damage, delay, and proof-of-delivery evidence. | Requires exception records and document evidence. |
| Carrier handoff | Sends load or shipment package to carrier. | Requires provider target and callback design per carrier. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `TransportLoad` | transportation-logistics | business, load number, status, planned ship date, origin, destination region, carrier, vehicle, total weight/volume. |
| `TransportLoadShipment` | transportation-logistics | load, shipment, sequence, planned/actual pickup/delivery status. |
| `TransportRoute` | transportation-logistics | route code, status, origin, stops, recurrence, vehicle/driver hints. |
| `TransportStop` | transportation-logistics | route/load, sequence, address snapshot, planned/actual timestamps, proof evidence. |
| `FreightCostEstimate` | transportation-logistics | load/shipment, currency, amount, supplier/carrier, source, status. |
| `TransportException` | transportation-logistics | shipment/load/stop, type, severity, status, evidence. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Transport load | `Draft -> Planned -> Dispatched -> InTransit -> Completed -> Closed`; cancellation after dispatch needs carrier/provider reversal design. |
| Transport route | `Draft -> Active -> Archived`. |
| Transport exception | `Open -> Investigating -> Resolved -> Closed`. |

## Application Surface

Future handlers:

- Create/update/cancel loads.
- Add/remove shipments from load before dispatch.
- Plan route/stops.
- Record dispatch/in-transit/completion evidence.
- Record transport exceptions.
- Link freight cost estimates to supplier invoice/payables only after finance boundary.

## WebAdmin Surface

WebAdmin should include loads, route plans, stop sequence, shipment consolidation, dispatch readiness, transport exceptions, freight cost review, and read-only links to orders, shipments, delivery notes, warehouse tasks, supplier invoices, and carrier provider operations.

No public/member/mobile route is added by default. Driver mobile/PWA needs separate design.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `transportation-logistics`. |
| Package role | Add-on to Operations/Inventory/Shipping. |
| Required dependencies | `shipping`, `inventory`; optional `procurement`, `finance`. |
| Disabled behavior | Existing shipment execution can remain if `shipping` is enabled; transport planning nav/actions are hidden. |
| Permissions | Manage loads, dispatch load, close load, manage exceptions. |
| Provider readiness | Carrier API add-ons require target selection and no-secret credential policy. |

## Compatibility Boundaries

- Existing DHL/shipping provider operations remain authoritative for labels/tracking where already implemented.
- Transportation planning does not create orders, invoices, payments, refunds, or finance export changes.
- Freight cost posting waits for finance/payables design.

## Implementation Slices

1. `Transport Load Planning Core Slice`.
2. `Route And Stop Planning Slice`.
3. `Transport Exception Evidence Slice`.
4. `Freight Cost Boundary Design`.
5. `Carrier API Target Adapter Design`.

## Test Plan

Future tests must cover load lifecycle, shipment ownership, no duplicate shipment mutation, route sequencing, exception evidence, provider readiness blocking, WebAdmin source guards, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement Transportation/Logistics Planning.
