# Darwin Module Audit Matrix

Last reviewed: 2026-05-09

This matrix is intentionally concise. It records code-backed status across the surfaces needed before go-live, without treating phase-one visibility as production completion.

| Module | Domain entity | Application handler | WebAdmin surface | WebApi surface | Worker/background flow | Docs | Tests |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Stripe storefront payments | `Payment`, `Order`, `EventLog` | Checkout session creation, safe return validation, webhook reconciliation | Payments, refunds, disputes, webhook deliveries | Public checkout, Stripe webhook | Provider callback worker | `docs/go-live-status.md`, `docs/production-setup.md`, `DarwinWebApi.md` | Checkout flow and Stripe webhook unit tests |
| DHL shipping | `Shipment`, `ShipmentCarrierEvent`, provider operation entities | Shipment create/label operation handlers | Shipments, returns, provider operations | DHL webhook | Shipment provider operation worker and provider callback worker | `docs/go-live-status.md`, `docs/production-setup.md` | DHL webhook/provider-operation focused tests |
| Brevo communication | Email/channel audit and provider callback entities | Brevo sender, email dispatch, callback processing | BusinessCommunications and compact dashboard counts | Brevo webhook | Email dispatch and provider callback workers | `docs/go-live-status.md`, `docs/production-setup.md` | Brevo webhook/provider callback focused tests |
| Tax/VAT/e-invoice | Invoice, invoice line, order tax snapshots, site tax settings | Tax compliance overview queries | Billing/TaxCompliance | Member invoice surfaces | None yet for e-invoice/export | `docs/go-live-status.md`, `BACKLOG.md` | Billing/tax source-contract coverage only |
| Business onboarding | Business, members, invitations, owner override audits | Provisioning, approval, suspension, invitation lifecycle handlers | Businesses, Support Queue, Merchant Readiness | Business account/access-state endpoints | Invitation email dispatch | `BACKLOG.md`, `DarwinWebAdmin.md` | WebAdmin smoke/source-contract coverage |
| Settings/localization | `SiteSetting`, business effective settings | Site-setting update/query handlers and validators | Site Settings | App bootstrap/public settings projections | Cache invalidation through runtime services | `docs/go-live-status.md`, `DarwinWebAdmin.md` | Resource parity/source-contract checks |
| Inventory/returns | Warehouses, stock levels, transfers, purchase orders, inventory ledger | Adjustment, reserve/release, receiving/transfer lifecycle handlers | Inventory workspaces and returns queues | Member/order projections only | None dedicated | `BACKLOG.md` | Unit filters for inventory/order flows |
| Web/front-office compatibility | CMS, catalog, order, payment, shipment projections | Public query/checkout handlers | Operator-only diagnostics stay in WebAdmin | Public/member contracts consumed by Darwin.Web | None | `DarwinFrontEnd.md`, `DarwinWebApi.md` | Web frontend source-contract tests |

## Current Go-Live Blockers

- Stripe: complete live test-account smoke for Checkout Session creation and webhook-only finalization, then verify subscription checkout, refunds, and disputes against real provider events.
- DHL: replace phase-one shipment/reference/label generation with a real DHL API client and durable label storage.
- Tax/VAT/e-invoice: add operator actions for VAT validation, reverse-charge decisions, immutable issued invoice snapshots, invoice export/archive, and e-invoice readiness.
- Business onboarding: review the state machine and support actions end to end under tenant-scoped support queues.
- Inventory/returns: verify return receipt effects on stock, reservation release, and supplier receiving lifecycle against real operator flows.
