# Darwin Modularity And Package-Readiness Audit

Reviewed: 2026-06-19

## Summary

This audit is documentation-only. It makes no runtime behavior changes, no schema changes, no route changes, no DTO changes, no WebAdmin actions, no mobile contracts, no worker registrations, and no provider flow changes.

Darwin already has clear logical product areas and several modularity foundations. The current code also shows that package enforcement is not complete across all surfaces. `FeatureArea`, `BusinessFeatureOverride`, and `FeatureAreaService` exist in `src/Darwin.Domain/Entities/Foundation` and `src/Darwin.Application/Foundation/FeatureAreaService.cs`, but the current WebAdmin layout, WebApi controllers, mobile route catalog, and Worker registrations do not consistently consume those feature areas as runtime gates.

The safe conclusion is:

- Darwin is logically modular by domain ownership, schema placement, internal services, permissions, and documentation.
- Darwin is not yet fully package-disable-safe for tenant-specific capability bundles.
- No capability should be sold as independently disable-safe unless the relevant UI, API, worker, provider, and client navigation paths are gated in code and covered by source-contract tests.

## Code-Backed Findings

| Surface | Current evidence | Package-readiness finding |
| --- | --- | --- |
| Feature foundation | `FeatureArea`, `BusinessFeatureOverride`, `FeatureAreaService`, and tests in `tests/Darwin.Tests.Unit/Foundation/FoundationServicesTests.cs`. | Foundation exists for feature visibility and business overrides. It is not wired broadly enough to be treated as full package enforcement. |
| Billing plan features | `src/Darwin.Application/Billing/BillingPlanFeatures.cs` exposes staff, reward-tier, campaign, export, and SLA limits. | This is not a broad ERP package contract. It currently supports subscription/loyalty-oriented plan features. |
| WebAdmin navigation | `src/Darwin.WebAdmin/Views/Shared/_Layout.cshtml` contains hardcoded sections for Catalog, CMS, Orders, Sales, Finance, HR, CRM, Loyalty, Inventory, Billing, Business Setup, Identity, and System. | Navigation is mostly permission/hardcoded. It does not consistently hide entries by tenant capability. |
| WebAdmin authorization | `PermissionAuthorizeAttribute` and controller-level permissions are used in WebAdmin. | Authorization is not the same as package entitlement. A user can have permission while the tenant lacks a capability unless a feature gate is added. |
| WebApi routes | Controllers under `src/Darwin.WebApi/Controllers` use audience route roots such as `api/v1/public`, `api/v1/member`, and `api/v1/business`. | Audience boundaries exist. Capability gates are not consistently visible as route-level or handler-level requirements. |
| Mobile routes | `src/Darwin.Mobile.Shared/Api/ApiRoutes.cs` is a static shared route catalog for Consumer and Business apps. | Route drift is guarded, but mobile navigation cannot yet be driven by tenant capability metadata in a complete way. |
| Worker registrations | `src/Darwin.Worker/Program.cs` registers email, channel dispatch, inactive reminders, provider callbacks, shipment operations, webhook delivery, invoice archive maintenance, and VIES retry workers. | Workers are operationally modular by options/provider readiness, but not tenant capability-gated as a single package framework. |
| Provider infrastructure | Stripe, DHL, Brevo, VIES, e-invoice adapter, object storage, and finance export file delivery are registered behind infrastructure boundaries. | Provider add-ons can be disabled by configuration more cleanly than broad domain modules, but provider-worker and UI readiness still need explicit capability checks. |
| Domain ownership | Entities are grouped under folders and schemas such as CMS, Catalog, CartCheckout, CRM, Loyalty, Inventory, Sales, Billing, HumanResources, Foundation, Integration, Orders, Pricing, Shipping, Settings, and Businesses. | Ownership is mostly clear. Package boundaries must be defined by capability owner, not by schema alone, because procurement lives in Inventory and payables/treasury live in Billing. |

## Boundary Risks

| Risk | Current example | Required future guard |
| --- | --- | --- |
| UI assumes modules exist | WebAdmin `_Layout.cshtml` lists many module sections without `FeatureAreaService` checks. | Add a WebAdmin capability navigation model and controller/action feature filter. |
| API exposes audience routes without package checks | Public/member/business controllers are audience-scoped but not capability-scoped in a uniform way. | Add structured `FeatureDisabled` problem response and source-contract tests per capability route family. |
| Worker jobs can run for disabled modules | Worker services are registered globally in `src/Darwin.Worker/Program.cs`. | Workers must skip tenant/provider work safely when capability or provider add-on is disabled. |
| Billing plan features are too narrow | `BillingPlanFeaturesSnapshot` is campaign/subscription oriented. | Keep it as current subscription feature metadata and design a separate package entitlement contract before broad ERP packaging. |
| DTO reuse can cross audiences | Mobile services consume shared `ApiRoutes`; WebAdmin has separate MVC surfaces. | Keep admin DTOs out of public/member/business/mobile contracts and add source guards where package gating is introduced. |
| Provider names leak into module logic | Stripe/DHL/Brevo paths exist in Application/WebAdmin queries and provider workers. | Treat providers as add-on capabilities with provider-specific UI hidden and workers skipped when disabled. |
| Schema is not always module | Procurement models are in `Inventory`; payables, treasury, and export are in `Billing`. | Capability ownership documents must override raw schema naming when business ownership differs. |

## No Runtime Behavior Changes

This audit records current architecture and required future guard behavior only. It does not add feature filters, action filters, middleware, navigation code, API responses, tests, workers, or migrations.

## Related Documents

- [Capability catalog](capability-catalog.md)
- [Capability entity ownership](capability-entity-ownership.md)
- [Package readiness matrix](package-readiness-matrix.md)
- [Module disabled behavior](module-disabled-behavior.md)
- [Modularity gaps backlog](modularity-gaps-backlog.md)
- [Machine-readable capability map](capability-map.json)
- [Existing module audit](../module-audit.md)
- [Feature area module visibility foundation](../domain-expansion/feature-area-module-visibility-foundation.md)
