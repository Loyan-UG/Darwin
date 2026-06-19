# Strategic Sourcing, RFQ, And Supplier Scoring Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future strategic sourcing, RFQ, tender, and supplier scoring boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, supplier portal, purchase order mutation, finance posting, or provider flow.

Decision: Strategic sourcing is a procurement planning capability above current supplier and purchase order workflows. It must not be modeled as purchase order notes or supplier metadata only.

## Current Darwin Sourcing Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| `Supplier`, `SupplierContact`, supplier documents | Supplier master and evidence exist. | Sourcing can evaluate suppliers but supplier master remains procurement-owned. |
| `PurchaseOrder` and `PurchaseOrderLine` | Purchase execution exists. | RFQ award can create or recommend PO after approval; PO owner remains procurement. |
| Goods receipt and supplier invoice | Delivery and invoice evidence exist. | Supplier scoring can read delivery/quality/payment evidence. |
| External references/documents | External ids and document metadata exist. | Supplier bids, certifications, and external tender ids use these primitives. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Purchase request | Captures internal demand before PO. | Requires request header/lines, approval, sourcing status. |
| RFQ/tender | Collects supplier quotes competitively. | Requires RFQ header/lines, invited suppliers, bid responses, comparison. |
| Award decision | Selects supplier and terms. | Requires approval and PO creation boundary. |
| Supplier scoring | Measures performance across price, quality, delivery, compliance. | Requires scorecards and evidence aggregation. |
| Supplier qualification | Tracks certifications and risk. | Requires document evidence and expiry reminders later. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `PurchaseRequest` | procurement | business, requester, status, required date, priority, justification. |
| `PurchaseRequestLine` | procurement | product/service description, quantity, target warehouse/project/cost center. |
| `RequestForQuote` | procurement | RFQ number, status, deadline, currency, terms, source request. |
| `RfqLine` | procurement | item/service, quantity, specification evidence. |
| `SupplierBid` | procurement | supplier, RFQ, status, submitted date, totals, validity, document evidence. |
| `SupplierBidLine` | procurement | RFQ line, price, lead time, terms, compliance flag. |
| `SupplierScorecard` | procurement | supplier, period, score dimensions, outcome, evidence links. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Purchase request | `Draft -> Submitted -> Approved -> Sourcing -> Fulfilled` or `Rejected/Cancelled`. |
| RFQ | `Draft -> Issued -> ResponsesOpen -> Evaluation -> Awarded -> Closed` or `Cancelled`. |
| Supplier bid | `Invited -> Submitted -> Clarification -> Accepted` or `Rejected/Expired`. |
| Scorecard | `Generated -> Reviewed -> Approved -> Archived`. |

## Application Surface

Future handlers:

- Create/submit/approve purchase requests.
- Create/issue RFQ from request.
- Register supplier bids and document evidence.
- Compare bids and award supplier.
- Create draft purchase order through procurement owner after award approval.
- Generate supplier scorecards from delivery, quality, invoice, and payment evidence.

## WebAdmin Surface

Procurement WebAdmin should include purchase requests, RFQ/tender list/detail, bid comparison, award approval, supplier scorecards, qualification documents, and read-only links to suppliers, POs, goods receipts, quality records, supplier invoices, and payments.

Supplier portal is not added by default and needs a separate external access design.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `strategic-sourcing`. |
| Package role | Procurement add-on. |
| Required dependencies | `procurement`; optional `quality`, `finance`, `analytics`. |
| Disabled behavior | Hide sourcing/RFQ pages; supplier and PO execution remain if procurement is enabled. |
| Permissions | Manage requests, approve requests, issue RFQ, evaluate bids, award RFQ, manage scorecards. |
| SoD | Request approval and award decision are approval candidates. |

## Compatibility Boundaries

- No automatic PO creation without approved award and procurement handler.
- No supplier invoice/payment or finance posting mutation.
- No public/supplier portal route in this design.

## Implementation Slices

1. `Purchase Request Core Slice`.
2. `RFQ And Supplier Bid Core Slice`.
3. `Bid Evaluation And Award Slice`.
4. `Supplier Scorecard Design/Core Slice`.
5. `Supplier Portal Boundary Design` only if external supplier access is needed.

## Test Plan

Future tests must cover request/RFQ lifecycle, award approvals, PO owner boundary, supplier document evidence, scorecard source data, WebAdmin row-version/anti-forgery, no public route, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement strategic sourcing.
