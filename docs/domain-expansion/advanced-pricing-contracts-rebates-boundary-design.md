# Advanced Pricing, Contracts, And Rebates Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks the future boundary for advanced pricing, customer/supplier contracts, and rebates. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, pricing runtime behavior, checkout change, invoice change, or finance posting.

Decision: Advanced pricing is a formal pricing capability, not ad hoc discounts in cart, product JSON, invoice notes, or manual journal adjustments. Checkout and issued documents must use immutable price snapshots; live pricing rules must not rewrite historical orders or invoices.

## Current Darwin Pricing Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| `Product`, variants, add-ons | Catalog holds base item data and add-on pricing support. | Keep base catalog pricing simple; advanced agreements live in pricing module. |
| `Promotion` and `PromotionRedemption` | Promotional/coupon foundation exists. | Promotions are not a full contract/rebate engine. |
| Cart/checkout/order snapshots | Checkout creates order snapshots and invoices rely on issued source models. | Advanced pricing must feed snapshots before order placement, not recompute after issuance. |
| Sales quotes | Quotes can represent negotiated offers. | Contract pricing can originate from accepted quotes but must own long-lived terms. |
| Finance posting/export | Finance reads posted facts. | Rebates/accruals need a finance boundary before posting. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Price agreements | Supports negotiated customer or customer-group prices. | Requires agreement header, lines, applicability, effective dates, and approval. |
| Contract terms | Supports recurring commercial terms, minimum quantities, and validity windows. | Requires contract lifecycle and document evidence. |
| Volume tiers | Enables tiered pricing based on quantity or period volume. | Requires deterministic tier evaluation and snapshot result. |
| Rebates | Supports retrospective discounts or supplier/customer incentive programs. | Requires accrual/payment/credit boundary before finance posting. |
| Price simulation | Operators can preview pricing before activation. | Requires pure evaluation service with no mutation. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `PriceAgreement` | advanced-pricing | business, customer/customer segment, currency, status, effective dates, priority, source quote/contract. |
| `PriceAgreementLine` | advanced-pricing | product/category/brand scope, price/discount type, value, min/max quantity, tax inclusion flag. |
| `CommercialContract` | advanced-pricing | counterparty, contract number, type, status, effective dates, renewal settings, document evidence. |
| `RebateProgram` | advanced-pricing | customer/supplier scope, calculation basis, status, period, currency, approval state. |
| `RebateAccrual` | advanced-pricing | program, period, source documents, calculated amount, status, finance linkage later. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Price agreement | `Draft -> Approved -> Active -> Suspended -> Expired -> Archived`; active rules are immutable for historical snapshots. |
| Contract | `Draft -> UnderReview -> Active -> Suspended -> Expired -> Terminated`. |
| Rebate program | `Draft -> Approved -> Active -> Calculated -> Settled`; settlement requires finance design. |

## Application Surface

Future handlers:

- Create/update/approve/activate/suspend/archive price agreements.
- Evaluate price for cart/quote/order using deterministic rule order.
- Create/update contract terms and link documents.
- Calculate rebate accruals from issued/posted source documents.
- Mark rebate ready for finance only after rebate posting boundary design.

## WebAdmin Surface

WebAdmin should include price agreements, contract terms, pricing simulation, active rule review, rebate programs, rebate calculation review, and read-only links to customers, quotes, orders, invoices, supplier invoices, and finance postings.

No public API exposes rule internals. Storefront/checkout only receives evaluated prices and final snapshots.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `advanced-pricing`. |
| Package role | Add-on to Commerce Standard, Sales, Procurement, or Enterprise Full Suite. |
| Required dependencies | `catalog`; optional `crm`, `sales`, `cart-checkout`, `finance`. |
| Disabled behavior | Hide pricing agreements and simulations; base catalog/promotions continue if enabled. |
| Permissions | Manage agreements, approve pricing, simulate pricing, manage rebates. |
| SoD | Approving high-value price agreements and rebate settlement are approval candidates. |

## Compatibility Boundaries

- No historical order, invoice, quote, refund, or finance export is recomputed after issuance.
- No rebate payment or credit note is created without finance boundary.
- No checkout route changes in this design.

## Implementation Slices

1. `Price Agreement Core Slice`.
2. `Pricing Evaluation Service Slice`.
3. `Commercial Contract Core Slice`.
4. `Rebate Boundary Design`.
5. `Rebate Calculation And Finance Readiness Slice`.

## Test Plan

Future tests must cover priority/effective-date evaluation, snapshot immutability, no invoice rewrite, approval gates, WebAdmin source guards, disabled-mode behavior, and compatibility for checkout/order/invoice contracts.

## No Runtime Behavior Changes

This design does not implement advanced pricing.
