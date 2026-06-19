# Quality And Nonconformance Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future Quality/Nonconformance boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, mobile contract, worker, stock movement, supplier invoice flow, or production flow.

Decision: Quality is a distinct capability that can extend inventory, procurement, manufacturing, returns, and service workflows. It must not be reduced to a free-text inspection note or a warehouse location status.

## Current Darwin Quality Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| `GoodsReceipt` and `GoodsReceiptLine` | Receiving and inspection quantities exist for supplier goods. | Quality can consume receipt inspection evidence but must own formal quality plans and nonconformance records. |
| `WarehouseLocationType.QualityHold` | Physical hold location exists. | Location is physical containment, not quality case ownership. |
| Lot/serial/HU foundation | Identity capture and traceability exist. | Quality holds and dispositions should link to lot/serial/HU when relevant. |
| `ReturnOrder` | Customer return inspection exists. | Quality can receive nonconformance evidence from returns but must not own refund decisions. |
| Supplier and purchase order foundation | Supplier identity and procurement evidence exist. | Supplier quality scoring can read quality outcomes but supplier master remains procurement-owned. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Inspection plans | Standardizes what must be checked for products, suppliers, receipts, production, or returns. | Requires versioned plan header, characteristics, sampling rules, and applicability. |
| Quality order | Creates an executable inspection instance. | Requires source entity links, status, inspector, result capture, and evidence. |
| Nonconformance | Formalizes defects and deviation handling. | Requires severity, source, disposition, corrective action, owner, due date, closure. |
| Quality hold/release | Blocks use of suspect stock without losing traceability. | Requires inventory identity/status integration; stock movement remains inventory-owned. |
| Supplier quality evidence | Helps rate supplier performance. | Requires read-only supplier links and reportable quality outcomes. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `QualityInspectionPlan` | quality | business, code, scope type, product/supplier applicability, status, version, effective dates. |
| `QualityInspectionCharacteristic` | quality | characteristic code, description, data type, target/tolerance, required flag, sort order. |
| `QualityOrder` | quality | source type/id, plan version, status, assigned inspector, due date, inspected date, result summary. |
| `QualityResultLine` | quality | characteristic, measured value, pass/fail, notes, evidence document links. |
| `Nonconformance` | quality | source type/id, severity, status, affected quantity, item/lot/serial/HU links, disposition, owner. |
| `CorrectiveAction` | quality | nonconformance, action type, owner, due date, status, completed evidence. |
| `QualityDisposition` | quality | accepted, rework, scrap, return-to-supplier, hold, release quantities. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Inspection plan | `Draft -> Active -> Superseded -> Archived`; only active plans create new quality orders. |
| Quality order | `Open -> InProgress -> Passed` or `Failed`; `Cancelled` only before final result. |
| Nonconformance | `Open -> UnderReview -> Dispositioned -> CorrectiveActionOpen -> Closed`; cancellation requires audit reason. |
| Quality hold | Hold/release must be evidence-backed and must not directly edit stock balances. |

## Application Surface

Future handlers:

- Create/update/activate/archive inspection plans.
- Create quality order from goods receipt, production order, stock count variance, return order, or manual quality request.
- Record inspection results and attachments.
- Create/review/disposition/close nonconformance.
- Request inventory hold/release through inventory owner.
- Read supplier quality and product quality summaries.

## WebAdmin Surface

Quality WebAdmin should include inspection plan list/detail/editor, quality order work queue, inspection execution screen, nonconformance list/detail/disposition/closure, corrective action queue, and read-only links to receipts, returns, production orders, lots, serial units, handling units, suppliers, and inventory ledger.

No public/member surface is added by default. Mobile-business scanning can be designed later for inspection execution.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `quality`. |
| Package role | Add-on to Operations/Inventory and Manufacturing; optional for trade-only customers. |
| Required dependencies | `inventory`; optional `procurement`, `manufacturing-mrp`, `sales`. |
| Disabled behavior | Hide quality queues and block creation of quality orders; existing quality evidence remains read-only when needed. |
| Permissions | Manage plans, execute inspections, disposition nonconformance, close corrective action. |
| SoD | Dispositioning scrap/release and closing high-severity nonconformance are approval candidates. |

## Compatibility Boundaries

- Quality does not own supplier invoice matching, refund eligibility, customer payment/refund, finance export, or stock ledger.
- Quality result can request inventory consequences; inventory handlers own actual stock movement.
- Document evidence uses `DocumentRecord`; external lab/customer/supplier ids use `ExternalReference`.
- No public/mobile/storefront behavior changes in this design.

## Implementation Slices

1. `Quality Inspection Plan Core Slice`.
2. `Quality Order Execution Slice`.
3. `Nonconformance And Corrective Action Slice`.
4. `Quality Inventory Hold Integration Slice`.
5. `Supplier Quality Reporting Slice`.

## Test Plan

Future tests must cover plan versioning, source links, inspection result validation, quality hold boundaries, nonconformance disposition, event/audit evidence, no direct stock mutation, no refund/payment mutation, WebAdmin row-version/anti-forgery, and capability disabled behavior.

## No Runtime Behavior Changes

This design does not implement quality workflows.
