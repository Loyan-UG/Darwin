# Darwin WebAdmin Guide

Reviewed: 2026-05-26

`Darwin.WebAdmin` is the operational back-office for Darwin. It is the primary control surface for business onboarding, support, provider readiness, billing, communication, inventory, CRM, loyalty, mobile operations, and compliance review workflows.

Historical progress notes belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). Current readiness status belongs in [docs/go-live-status.md](docs/go-live-status.md).

## Architecture

- ASP.NET Core MVC/Razor with HTMX.
- Operator-only UI. Do not move diagnostics, readiness dashboards, review queues, or back-office wording into public/member UI.
- Controllers should keep request/response orchestration thin and delegate business rules to Application handlers.
- Use localized resources for visible operator text.
- Use row-version protected posts for mutations.
- Keep anti-forgery validation on form posts.
- Keep provider credentials masked or hidden.

## Core Workspaces

| Workspace | Purpose |
| --- | --- |
| Dashboard | Compact operational command center with high-value summaries and attention items. |
| Businesses | Business profile, lifecycle, approval, suspension, reactivation, members, invitations, locations, and setup readiness. |
| Merchant readiness / onboarding | Admin-assisted setup checklist that links to existing profile, plan, users, locations, loyalty, communications, visibility, and review surfaces. |
| Billing | Payments, refunds, disputes, subscriptions, webhook deliveries, tax/compliance review, and provider reconciliation. |
| BusinessCommunications | Email/channel dispatch, provider callbacks, failed sends, retry decisions, and communication readiness. |
| MobileOperations | Device registration, push token state, stale/disabled notification review, and mobile support handoffs. |
| CRM | Customers, leads, opportunities, invoices, invoice archive/source-model/e-invoice artifact actions, and support workflows. |
| Inventory | Warehouses, suppliers, purchase orders, stock transfers, reservations, returns, and ledger review. |
| Shipping | Shipments, returns, provider operations, label handling, tracking, and carrier exception recovery. |
| Site Settings | Secure operational configuration and secret-free provider readiness. |

## Business Onboarding

WebAdmin owns the first-launch business onboarding path:

- New businesses start inactive and pending approval.
- Operators can create, review, approve, suspend, and reactivate businesses.
- Approval/reactivation requires minimum setup prerequisites such as owner, legal/contact data, and primary location.
- Invitation preview/acceptance supports pending onboarding without allowing suspended/unavailable businesses.
- Invitation resend/revoke actions are status-aware.
- The onboarding wizard summarizes existing workspaces and does not bypass their validation.

Future self-service onboarding in `Darwin.Web` must reuse the same backend rules instead of creating a parallel policy.

## Provider Operations

WebAdmin must expose provider state without exposing provider secrets:

- Stripe: payment, subscription, refund, dispute, webhook, and reconciliation queues.
- DHL: shipment, label, return-label, tracking, failed/stale provider-operation recovery.
- Brevo: dispatch audits, provider callbacks, failed sends, retry and support handoffs.
- Object storage: configured provider/profile status and safe smoke action.
- VIES/e-invoice: review queues, validation state, and operator-visible blockers.

Provider-specific live readiness is not complete until the matching provider smoke and operational monitoring pass.

## Billing And Subscription Policy

- Storefront and business payments use provider-hosted checkout.
- Browser return routes must not finalize payment or subscription state.
- Verified provider webhooks are authoritative.
- Business mobile subscription management is read-only for first launch; plan purchase, cancellation, SEPA mandate setup, and manual payment registration are web/back-office workflows.
- WebAdmin must show provider references, status, failures, and reconciliation state without exposing secret keys.

## Security And UX Rules

- Never display provider secrets, access keys, webhook secrets, connection strings, SAS tokens, or private credentials.
- Mask secret fields and preserve existing values when posts contain blank or placeholder values.
- Use anti-forgery tokens and row-version checks for mutations.
- Keep CSP, secure cookies, and self-hosted asset assumptions intact.
- Prefer compact operator dashboards over long explanatory pages.
- Keep diagnostics in module workspaces; the dashboard should summarize and link.

## Testing Expectations

Use hosted WebAdmin tests for:

- Authentication/authorization and anti-forgery.
- Render and HTMX fragment stability.
- Row-version protected mutation flows.
- Business onboarding and invitation lifecycle.
- Inventory/returns operator flows.
- Billing/payment/refund/dispute/provider callback surfaces.
- Mobile support surfaces.
- E-invoice artifact download safety.

See [DarwinTesting.md](DarwinTesting.md) for active commands and coverage priorities.
