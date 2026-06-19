# Modularity Gaps Backlog

Reviewed: 2026-06-19

This backlog is ordered to make Darwin package-safe without rewriting the product into separate deployments or databases.

## P0 Foundation Gates

| Gap | Why it matters | Proposed next slice |
| --- | --- | --- |
| FeatureArea is not wired into UI/API/mobile/worker surfaces. | Tenant package settings cannot reliably hide or block disabled modules. | `Capability Enforcement Foundation Core Slice`. |
| No shared `FeatureDisabled` response contract. | WebAdmin, WebApi, mobile, and Worker paths would fail differently. | `Module Disabled Behavior Guard Core Slice`. |
| WebAdmin navigation is hardcoded by module section. | Disabled modules remain visible even when a tenant package should hide them. | `WebAdmin Capability Navigation Slice`. |
| Direct WebAdmin URL access is permission-based, not capability-based. | A permitted operator could reach a disabled module. | `WebAdmin Capability Action Filter Slice`. |
| WebApi lacks uniform capability attributes or filters. | Public/member/business routes can expose disabled features. | `WebApi Capability Filter Slice`. |
| Worker tenant/provider skip behavior is not centralized. | Background jobs can process disabled tenant/provider work. | `Worker Capability Skip Policy Slice`. |
| Client capability metadata is incomplete. | Mobile/Web navigation cannot be reliably package-driven. | `Capability Metadata Contract Slice`. |

## P1 Capability Ownership Hardening

| Gap | Why it matters | Proposed next slice |
| --- | --- | --- |
| `BillingPlanFeatures` is not broad ERP package entitlement. | Subscription limits and ERP package enablement are different business concepts. | `Package/Plan Foundation Core Slice` after tenant catalog. |
| Procurement is implemented under Inventory schema. | Package ownership can be confused by schema-only analysis. | `Procurement Capability Boundary Source Guards`. |
| Finance/payables/treasury live in Billing schema. | Billing, finance, and treasury packages need clear ownership. | `Finance Capability Boundary Source Guards`. |
| Storefront crosses many owners. | Storefront disablement must stop checkout/order/payment/shipping side effects. | `Storefront Dependency Gate Design Slice`. |
| Sales uses order/invoice foundations. | Sales must not become a second order/invoice mutation owner. | `Sales Capability Boundary Source Guards`. |
| CRM reads loyalty/order/invoice context. | CRM must not own loyalty balance or financial settlement state. | `CRM Context Boundary Guard Slice`. |

## P2 Provider And Operations Guards

| Gap | Why it matters | Proposed next slice |
| --- | --- | --- |
| Provider add-ons are config-aware but not package-aware. | Stripe, DHL, and Brevo should be disabled per tenant/package without breaking base modules. | `Provider Capability Readiness Guard Slice`. |
| Object-storage dependent features need consistent readiness blocking. | Archive, e-invoice, finance export, and labels must not fake success. | `Storage Capability Readiness Guard Slice`. |
| Analytics/export reads cross-domain data. | Read scopes must match enabled capabilities and audience. | `Analytics Capability Scope Slice`. |
| AI scoped context can read many module aggregates. | AI must only see enabled, purpose-approved capability data. | `AI Capability Context Scope Slice`. |

## Acceptance Rules For Future Guard Slices

- Do not split projects or databases only to model packages.
- Keep permission authorization and capability entitlement separate.
- Treat `BusinessId` as the current tenant/customer scope unless a record is explicitly platform-global.
- Do not claim disable safety until WebAdmin, WebApi, Worker, Web/mobile navigation, and source-contract tests agree.
- Provider secrets, tokens, private keys, connection strings, raw provider payloads, and customer-specific package data must not appear in docs, logs, metadata, events, or external references.
