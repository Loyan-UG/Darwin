# POS And Retail Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future POS/Retail boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, POS UI, device flow, payment flow, stock mutation, invoice/archive change, or mobile contract.

Decision: POS is a dedicated retail surface with cash-session, receipt, payment, return, tax, stock, and offline considerations. It must not be treated as the existing storefront checkout with a different screen.

## Current Darwin POS Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| Storefront cart/checkout | Public checkout exists for web commerce. | POS needs a separate operator/device workflow and cannot reuse anonymous storefront assumptions. |
| Orders/payments/refunds/invoices | Order-to-cash and settlement foundations exist. | POS can create orders through order owners only after POS payment/tax/receipt design. |
| Inventory/stock | Inventory ledger and warehouse/bin foundations exist. | POS stock deduction must use inventory owner with idempotent references. |
| Loyalty | Loyalty account/scan/reward flows exist. | POS can integrate loyalty only after customer identity and cashier permission design. |
| Provider payments | Stripe checkout/webhook exists for online payment. | POS card terminal and cash handling need separate provider/device policy. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Counter sale | Physical store staff can sell immediately. | Requires POS session, cart, cashier, store/warehouse, receipt number, tax snapshot. |
| Cash drawer/session | Tracks opening float, cash sales, cash movements, and closing variance. | Requires cash session lifecycle and audit. |
| POS payments | Supports cash, card terminal, voucher, split tender. | Requires payment method policy and provider/device readiness. |
| POS returns | Handles in-store returns without bypassing ReturnOrder/refund policy. | Requires return boundary and evidence link. |
| Offline mode | Stores can continue limited sales when connectivity is down. | Requires explicit offline queue, idempotency, and conflict policy; not v1 unless designed. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `PosTerminal` | pos-retail | business, location/store, code, status, device identity reference, allowed payment methods. |
| `PosCashSession` | pos-retail | terminal, cashier, opened/closed timestamps, opening/closing cash, variance, status. |
| `PosSale` | pos-retail | business, terminal, session, receipt number, status, customer, totals, currency, order link. |
| `PosSaleLine` | pos-retail | product snapshot, quantity, unit price, tax, discount, warehouse/bin, stock reference. |
| `PosTenderLine` | pos-retail | method, amount, provider/device reference, status. |
| `PosReturn` | pos-retail | original sale/order/return order link, returned quantities, refund evidence. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Cash session | `Open -> Closing -> Closed`; reopen requires approval/correction design. |
| POS sale | `Draft -> Paid -> Posted -> Voided`; void after posting requires reversal/refund design. |
| Tender | `Pending -> Completed -> Failed -> Reversed`; provider evidence required for non-cash. |
| Offline sale | Blocked until offline queue design exists. |

## Application Surface

Future handlers:

- Open/close cash session.
- Build POS sale with product/customer/loyalty lookup.
- Complete tender and create order/payment through owning handlers.
- Post stock deduction through inventory owner.
- Print/download receipt artifact.
- Create POS return through ReturnOrder/refund owners.

## WebAdmin And POS Surface

WebAdmin should manage terminals, sessions, closing reports, receipt templates, exceptions, and read-only sales. The active selling surface may be a dedicated POS web app or device app after UX/offline/device design.

No generic public/member/mobile route is added by default.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `pos-retail`. |
| Package role | Retail add-on to Commerce/Inventory. |
| Required dependencies | `catalog`, `billing`, `inventory`; optional `loyalty`, `provider-stripe`, `tax-vat`. |
| Disabled behavior | Hide POS and block terminal/session/sale creation; storefront remains separate if enabled. |
| Permissions | Open session, sell, discount, return, close session, manage terminals. |
| SoD | Cash close variance, high discount, and post-sale void are approval candidates. |

## Compatibility Boundaries

- POS does not bypass order, payment, refund, tax, invoice, or inventory owners.
- No offline mutation exists until idempotency and conflict resolution are designed.
- Finance export remains journal-entry based and unchanged.

## Implementation Slices

1. `POS Terminal And Cash Session Design/Core Slice`.
2. `POS Sale Draft And Receipt Boundary Slice`.
3. `POS Payment/Tender Boundary Design`.
4. `POS Inventory Posting Slice`.
5. `POS Returns Boundary Design`.
6. `POS Offline Mode Design` only if required.

## Test Plan

Future tests must cover session lifecycle, payment method boundaries, stock owner integration, receipt snapshots, return/refund guard, no storefront checkout reuse, WebAdmin guards, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement POS/Retail.
