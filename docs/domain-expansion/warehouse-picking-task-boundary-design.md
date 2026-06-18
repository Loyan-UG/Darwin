# Warehouse Picking Task Boundary Design

## Summary

This document locked the warehouse picking boundary before implementation and now records the outcome of the core picking slice. Public WebApi contracts, mobile/member contracts, storefront flow, shipment flow, payment flow, invoice archive/download behavior, supplier invoice/payment flow, and finance export format remain unchanged.

The professional default is allocation-backed picking. Warehouse picking tasks must execute against existing order allocation and reservation facts, not create demand by themselves. A pick task can provide warehouse execution evidence, bin/source location evidence, picked quantity evidence, shortage evidence, and later PWA scan evidence. It does not own customer payment, invoice issuance, shipment label creation, order cancellation, or storefront availability promises.

## Current Darwin Picking And Fulfillment Findings

- `Order`, `OrderLine`, payment, shipment, invoice, refund, and member commerce contracts already exist and remain the operational fulfillment and customer-facing foundations.
- Inventory reservation and allocation handlers exist and have been hardened through the shared inventory movement reference policy.
- `StockLevel` is the availability summary and `InventoryTransaction` is the authoritative movement ledger.
- `Warehouse`, `WarehouseLocation`, `WarehouseLabelTemplate`, `WarehouseTask`, and `WarehouseTaskLine` exist in Inventory.
- Warehouse receiving and putaway task execution exists for goods receipt evidence. It does not replace `GoodsReceipt` or create a second stock ledger.
- Warehouse picking task core now exists on `WarehouseTask`: WebAdmin can create allocation-backed picking tasks from orders and completion validates order state, order-line linkage, allocation evidence, and active source locations.
- `Shipment` remains the shipment owner. Picking can feed fulfillment readiness, but it must not create shipment provider operations, labels, carrier events, payment state, invoice state, refunds, or customer notifications by itself.
- Public WebApi, mobile/member, storefront checkout, customer invoice archive/download, finance export, supplier invoice/payment, customer payment/refund, and credit-note flows are compatibility-sensitive and must remain unchanged in this design step.

## Locked Decisions

- `WarehouseTask` remains the task header and line foundation for picking; no separate `PickingTask` table is introduced for v1.
- Picking tasks are internal/WebAdmin and future operator-PWA records. They are not public storefront or member-mobile contracts.
- Picking must be allocation-backed. A pick task line must link to current order/order line allocation evidence or a future allocation reference. It must not create arbitrary demand from notes or free text.
- Picked quantity cannot exceed allocated quantity. Short-picked quantity is explicit evidence and does not automatically cancel an order, create a refund, issue a credit note, or edit an invoice.
- Picking completion does not create or update customer payment state, invoice state, refund state, supplier invoice/payment state, finance export packages, or invoice archive/download records.
- Packing and shipping remain separate fulfillment owners. Pick completion may mark pick evidence complete and expose readiness for packing/shipping, but shipment creation and carrier/provider actions stay with existing shipment handlers.
- Substitution is blocked in v1 picking implementation unless a dedicated substitution policy is designed. Operators must not silently pick another product variant for an allocated order line.
- Partial picking is allowed as evidence only when short quantity is captured. Partial fulfillment, backorder, cancellation, refund, or customer notification policy requires a separate fulfillment decision.
- Bin/source location selection must use active `WarehouseLocation` records in the same business and warehouse.
- Picking must not create bin-level stock storage. Any stock movement or reservation release must go through existing inventory handlers and idempotent reference policy.
- PWA scan UX is the likely execution surface after WebAdmin picking foundation, but offline mutation is blocked until an outbox, idempotency, conflict, retry, and support visibility design exists.
- Events and audit evidence come from `BusinessEvent/AuditTrail`; external pick/import ids come from `ExternalReference`; documents or damage photos come from `DocumentRecord` only after attachment flow design.

## Decision Matrix

| Picking surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Inventory impact | Order/shipment impact | Customer/finance impact | Mobile/PWA impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Pick trigger | Orders and allocation/reservation handlers exist. | Create pick tasks only from allocated order demand, not manual notes. | Order fulfillment and Inventory. | Pick task creation handler using allocation evidence. | Reads allocation and stock context. | Does not create shipment. | No payment/invoice/refund impact. | Future PWA can list ready picks. | Complete for core. | Keep shipment and packing separate. |
| Pick task model | `WarehouseTask` and `WarehouseTaskLine` exist and support allocation-backed picking and explicit shortage evidence. | Reuse `WarehouseTask` with `TaskType = Picking`; no separate picking table in v1. | Inventory task services. | Warehouse task handlers. | Task evidence only. | Links to order/order line source. | No finance effect. | Shared task model supports PWA. | Complete for core and shortage attention. | Design packing or PWA execution separately. |
| Source order line | `OrderLine` exists and may have catalog or non-catalog lines. | Pick lines must link to order lines and allocation evidence. Non-catalog lines can be evidence-only if they cannot drive stock. | Order and Inventory handlers. | Pick task handlers plus current allocation handlers. | Variant-backed lines can affect stock through inventory owners only. | Keeps order source intact. | No customer contract change. | PWA shows source line snapshot. | Complete for core. | Keep explicit shortage evidence. |
| Allocated quantity | Reservation/allocation handlers exist. | Pick requested quantity cannot exceed allocated quantity. | Inventory allocation owners. | Pick task creation/completion handlers. | Prevents over-pick. | Keeps fulfillment quantity coherent. | No payment/invoice mutation. | PWA validates scan quantities. | Complete for core. | Keep over-pick guard in completion handler. |
| Source bin/location | `WarehouseLocation` exists and picking validates active same-warehouse source location evidence. | Source location must be active and same warehouse. If allocation is not bin-specific yet, operator can choose source bin as execution evidence. | Warehouse task services. | Pick task handlers. | No bin stock store in v1. | Supports audit of where goods were picked. | No finance effect. | PWA scan can confirm bin barcode. | Complete for core. | Keep bin stock derivation separate from task creation. |
| Pick completion | Generic task lifecycle has pick-specific completion guards and shortage limits. | Completion requires order allocation evidence, requested quantity limits, valid source location evidence for variant-backed lines, and `CompletedQuantity + ShortQuantity <= RequestedQuantity`. | Warehouse task services. | Pick completion handler. | Delegates stock/reservation movement to existing owners only when implementation defines that mutation. | Does not create shipment. | No payment/invoice/refund mutation. | PWA completion uses handler. | Complete for core and shortage attention. | Keep automatic fulfillment outcomes blocked. |
| Short pick | `WarehouseTaskLine.ShortQuantity` and `ShortReason` exist. | Capture short quantity and safe reason only for `TaskType = Picking`. Do not automatically cancel, refund, invoice, substitute, ship, or notify. | Warehouse task services. | Pick task handler. | No automatic inventory adjustment. | Exposes fulfillment attention. | No customer-visible change without fulfillment policy. | PWA can capture reason later through the same handler. | Complete for current WebAdmin/internal phase. | Design packing, backorder, or customer notification only as separate fulfillment policies. |
| Substitution | No substitution policy exists. | Block silent substitution in v1. | Order fulfillment. | Dedicated future substitution handler only. | Prevents wrong stock movement. | Keeps order promise intact. | Avoids incorrect invoice/tax/customer expectation. | PWA cannot substitute without policy. | Not ready. | Design substitution before enabling. |
| Packing | Shipment/order fulfillment exists. | Packing is separate from picking. Pick completion can make items ready for packing but does not pack. | Fulfillment services. | Future packing task or shipment handler. | No direct stock effect from design. | Shipment remains owner. | No payment/invoice change. | Future PWA pack flow. | Needs boundary design if implemented. | Keep packing out of first picking implementation unless explicitly designed. |
| Shipment creation | `Shipment` and shipment provider operations exist. | Picking does not create shipments or carrier/provider operations. | Shipment handlers. | Existing shipment handlers. | No stock effect. | Existing shipment flow remains authoritative. | No customer notification change. | PWA can link to shipment readiness later. | Ready as guard. | Source guard no shipment mutation from pick UI. |
| Payment and invoice | Customer payment, invoice, refund, archive and credit-note flows exist. | Picking cannot mutate payment, invoice, refund, credit note, archive, or finance export. | Billing/Sales/Finance owners. | Existing owners only. | None. | None. | Compatibility preserved. | None. | Ready as guard. | Add tests/source guards in implementation. |
| Public/mobile/storefront | Existing public and member/mobile contracts are stable. | Picking remains internal/operator only. | Internal WebAdmin/PWA. | Warehouse task handlers. | No public stock promise change. | No customer route change. | No member DTO change. | Future PWA is internal, not member mobile. | Ready as guard. | Keep contracts/mobile smoke lanes. |
| Offline picking | No offline mutation policy exists. | Online-first only until outbox/conflict/retry/support design exists. | Future PWA/offline foundation. | Offline processor plus task handlers only after design. | Prevents duplicate pick completion. | Prevents hidden fulfillment conflicts. | No customer-facing inconsistency. | PWA can be online-first. | Not ready. | Design offline before enabling offline mutations. |
| Evidence and documents | `BusinessEvent`, `AuditTrail`, `DocumentRecord`, and `ExternalReference` exist. | Use shared primitives for events, audit, documents, and external ids. No raw scanner/provider payload in metadata. | Foundation services plus task handlers. | Pick handlers. | Audit without stock-ledger duplication. | Supports traceability. | Safe metadata only. | PWA can emit safe evidence. | Ready. | Reuse primitives in implementation. |

## Implementation Direction

1. `Warehouse Picking Task Core Slice`
   - Complete. Allocation-backed `WarehouseTask` records with `TaskType = Picking` can be created from eligible allocated orders.
   - Task lines link to `OrderLine` and current allocation evidence.
   - Active source location, allocated quantity, same business and warehouse, row-version, and sensitive metadata validation are in place.
   - WebAdmin internal pick queue creation is available without public/mobile/storefront contracts.
   - Shipment, payment, invoice, refund, supplier finance, and finance export owners remain unchanged.
2. `Warehouse Picking Shortage And Fulfillment Attention Slice`
   - Complete. `WarehouseTaskLine` stores explicit `ShortQuantity` and `ShortReason` for picking tasks, lifecycle guards prevent over-completion, queue filters expose shortage attention, and WebAdmin shows shortage summary and line-level evidence.
   - Keep cancellation, refund, backorder, substitution, and notification blocked until their own fulfillment policy is designed.
3. `Warehouse Packing Boundary Design`
   - Lock packing, shipment creation, label, carrier, and package evidence before implementation.
4. `Warehouse Mobile-First PWA Design/Slice`
   - Build internal online-first scan UX after picking and putaway handlers are stable.
   - Keep offline mutations blocked until outbox/conflict/retry/support design exists.

## Compatibility And Guardrails

- Picking core added internal Application DTO/handler and WebAdmin mutation only for warehouse task creation. It did not add schema, migration, public WebApi, mobile/member contract, storefront flow, shipment flow, payment/refund flow, supplier invoice/payment flow, finance export format, or invoice archive/download behavior changes.
- Focused `WarehouseTask|Picking|Order|InventoryMovement` unit tests and compatibility smoke for contracts and mobile member commerce guard the current implementation.
- Source guards prove no customer payment/refund, supplier payment/invoice, shipment provider, finance export, public/mobile, or invoice archive/download shortcuts are introduced in picking UI.
- Sensitive metadata must reject provider credentials, access tokens, scanner secrets, connection strings, raw device payloads, raw provider payloads, and customer payment payloads.
