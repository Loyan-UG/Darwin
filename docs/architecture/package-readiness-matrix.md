# Package Readiness Matrix

Reviewed: 2026-06-19

This matrix distinguishes product packaging from code existence. A capability can be implemented and still not be safe to disable per tenant until UI, API, worker, mobile, provider, and storage paths are guarded.

## Readiness Categories

| Category | Meaning | Darwin examples |
| --- | --- | --- |
| Required base capability | Required for a usable tenant/system. | `foundation-platform`, `identity-access`, `business-master`. |
| Independently sellable candidate | Business module that can become a package after enforcement work. | `crm`, `inventory`, `procurement`, `sales`, `finance`, `hr-time`. |
| Add-on capability | Enhances a base or sellable module. | `member-portal`, `cart-checkout`, `shipping`, `communications`, `tax-vat`, `e-invoice`, `analytics`, `payroll`, `bank-treasury`, `ai-governance`. |
| Provider add-on | External provider or target adapter. | `provider-stripe`, `provider-dhl`, `provider-brevo`. |
| Platform surface | Delivery channel, not a domain package. | `webadmin-operations`, `webapi-public`, `webapi-member`, `webapi-business`, `mobile-consumer`, `mobile-business`, `worker-operations`. |
| Not safely separable yet | Implemented or partially implemented, but disabled-mode enforcement is incomplete. | Most business modules until feature gates are wired. |

## Matrix

| Capability | Package class | Required with | Optional enhancements | Current disabled-mode confidence | Evidence |
| --- | --- | --- | --- | --- | --- |
| `foundation-platform` | Required base | All capabilities. | AI governance, sync, documents, custom fields. | High as always-on base. | Foundation entities and services exist. |
| `identity-access` | Required base | Business master, WebAdmin, WebApi, mobile. | WebAuthn, device management. | High as always-on base. | Identity entities and auth controllers exist. |
| `business-master` | Required base | CRM, loyalty, billing, procurement, finance, WebAdmin. | Onboarding, business communications. | High as always-on base. | Business entities and access state exist. |
| `cms` | Independently sellable candidate | Business master, WebAdmin, public WebApi. | Media, storefront. | Low. Navigation and routes are not feature-gated. | CMS controllers and entities exist. |
| `catalog` | Independently sellable candidate | Business master, WebAdmin. | Storefront, cart-checkout, inventory availability. | Low. Catalog is widely assumed by storefront and admin nav. | Catalog entities and controllers exist. |
| `storefront` | Independently sellable candidate | CMS, catalog, public WebApi. | Cart-checkout, member portal, Stripe, shipping. | Low. Front-office routes depend on multiple capabilities. | `src/Darwin.Web`, public controllers. |
| `member-portal` | Add-on | Identity, business master, WebApi member. | Loyalty, member commerce, payroll payslips. | Low. Mobile/client capability metadata is incomplete. | Mobile shared routes and member controllers. |
| `cart-checkout` | Add-on | Catalog, billing, orders, tax. | Stripe, shipping, inventory availability. | Low. Checkout crosses many module owners. | Cart entity and checkout controllers. |
| `crm` | Independently sellable candidate | Business master, identity, foundation. | Sales, loyalty context, communications. | Low. WebAdmin and APIs are not feature-gated. | CRM entities and WebAdmin. |
| `loyalty` | Independently sellable candidate | Business master, identity/member, billing plan limits. | Communications/push campaigns. | Medium for campaign entitlements, low for module-wide gating. | Loyalty services and `BusinessCampaignEntitlement`. |
| `inventory` | Independently sellable candidate | Catalog, business master, foundation. | Procurement, warehouse PWA, traceability. | Low. Inventory admin nav and worker dependencies are not capability-gated. | Inventory entities and controllers. |
| `procurement` | Independently sellable candidate | Inventory, finance, business master. | Supplier documents, payables. | Low. Procurement lives inside Inventory surfaces. | Supplier/PO/receipt models. |
| `sales` | Independently sellable candidate | Orders, billing, CRM/customer, foundation. | Delivery notes, returns, credit notes. | Low. Sales WebAdmin is hardcoded in layout. | Sales entities and controller. |
| `billing` | Required for commerce, optional for non-commerce tenants | Business master, identity. | Stripe, subscriptions, tax, e-invoice. | Low for package disablement. | Billing entities, WebAdmin Billing, WebApi Billing. |
| `finance` | Independently sellable candidate | Billing/foundation, business master. | Treasury, export, accounting adapters. | Low. Finance WebAdmin is hardcoded and finance entries are shared. | Billing accounting entities and Finance controller. |
| `bank-treasury` | Add-on to finance | Finance, business master. | Reconciliation, settlement, provider bank API later. | Low. Internal surfaces exist but package gating is incomplete. | Bank entities and finance WebAdmin. |
| `shipping` | Add-on | Orders, addresses, provider config. | DHL. | Low to medium. Provider readiness exists, package gating incomplete. | Shipping methods, DHL worker/controller. |
| `communications` | Add-on | Business/user/contact recipients. | Brevo, push. | Low to medium. Provider gating exists by config, module gating incomplete. | Email/channel operations and notification services. |
| `provider-stripe` | Provider add-on | Billing/cart-checkout. | Subscriptions, checkout. | Medium. Provider config gates much behavior; package entitlement still incomplete. | Stripe controllers/handlers. |
| `provider-dhl` | Provider add-on | Shipping/orders. | Return labels. | Medium. Provider operation queue exists; package entitlement incomplete. | DHL controllers/workers. |
| `provider-brevo` | Provider add-on | Communications. | Campaign emails. | Medium. Provider selection gates sending; package entitlement incomplete. | Brevo sender/webhook. |
| `tax-vat` | Add-on | Billing/invoice/customer tax context. | VIES retry worker. | Low. Worker and admin gating need feature checks. | VAT retry worker and tax compliance docs. |
| `e-invoice` | Add-on | Billing/invoice/archive/object storage. | German rollout artifacts. | Low. Artifact generation/download gating needs package rules. | E-invoice boundary docs and archive worker. |
| `analytics` | Add-on | Cross-domain read access. | Export jobs/files. | Low. Needs read-scope gates. | Analytics export entities. |
| `hr-time` | Independently sellable candidate | Business master, identity. | Payroll. | Low. WebAdmin and member payroll route gating incomplete. | HR entities and controller. |
| `payroll` | Add-on to HR and finance | HR, finance, bank-treasury for settlement. | Employee self-service. | Low. Member route exists; package metadata is incomplete. | Payroll entities and member payslip controller. |
| `ai-governance` | Add-on | Foundation, business events. | Provider adapter, executors. | Low. Internal UI exists but package gating needs explicit checks. | AI governance entities/controller. |
| `integrations-sync` | Add-on/foundation | ExternalSystem, ExternalReference. | Target adapters. | Medium for foundation, low for target-specific modules. | Integration entities and sync docs. |
| platform surfaces | Platform surface | Their domain capabilities. | Client navigation metadata. | Low for capability gating. | WebAdmin, WebApi, mobile, Worker code. |

## Readiness Rule

The package-ready label can only move from candidate to sellable after the capability has:

- WebAdmin navigation and direct URL checks.
- WebApi route checks and structured `FeatureDisabled` response.
- Worker skip behavior for tenant/provider-disabled jobs.
- Mobile/Web capability metadata for navigation gating.
- Source-contract tests proving disabled-mode behavior.
