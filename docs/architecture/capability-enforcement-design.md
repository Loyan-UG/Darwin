# Capability Enforcement And Disabled-Mode Design

Reviewed: 2026-06-19

## Summary

This document designs how Darwin should enforce capability and package disabled-mode behavior across WebAdmin, WebApi, mobile/Web, workers, providers, and storage-backed features. It is documentation-only and adds no runtime behavior, entity, migration, route, DTO, WebAdmin action, mobile contract, worker registration, provider flow, or package assignment.

`FeatureArea` remains the technical capability unit. Package and plan assignments grant entitlement only. Authorization remains separate and is still enforced by roles, permissions, auth policies, audience rules, and business membership checks.

## Current Darwin Findings

| Current surface | Code-backed finding | Enforcement decision |
| --- | --- | --- |
| Feature registry | `src/Darwin.Domain/Entities/Foundation/FeatureArea.cs`, `BusinessFeatureOverride.cs`, and `src/Darwin.Application/Foundation/FeatureAreaService.cs` provide a capability foundation. | Reuse as the capability source, but add a shared enforcement abstraction before runtime gates are wired. |
| WebAdmin authorization | `src/Darwin.WebAdmin/Security/PermissionAuthorizeAttribute.cs`, `PermissionKeys.cs`, `AdminBaseController.cs`, and `_Layout.cshtml` enforce permissions and hardcoded navigation. | Capability gates must wrap navigation and actions, but must never replace permission checks. |
| WebApi audiences | Public/member/business routes are separated in `src/Darwin.WebApi/Controllers`, with auth policies on member/business surfaces. | Add capability filters per route family after the response contract is defined. |
| Mobile clients | `src/Darwin.Mobile.Shared/Api/ApiRoutes.cs` and shared services use static routes and no complete capability metadata contract. | Add read-only capability metadata before mobile navigation becomes package-driven. |
| Worker registration | `src/Darwin.Worker/Program.cs` registers workers globally for email/channel dispatch, provider callbacks, shipments, invoice archive, VAT retry, and webhooks. | Workers must evaluate tenant/provider capability before processing tenant-scoped work and must skip safely without fake success. |
| Provider readiness | Stripe, DHL, Brevo, object storage, invoice archive, e-invoice, and finance export flows have provider/config readiness rules, but package entitlement is not uniform. | Provider add-ons require both entitlement and readiness; missing readiness blocks success paths. |
| Subscription features | `src/Darwin.Application/Billing/BillingPlanFeatures.cs` and business subscription flows cover current subscription feature limits. | Keep them. They are not the broad ERP package entitlement model. |

## Enforcement Layers

| Layer | Owns | Required rule |
| --- | --- | --- |
| Tenant resolution | Which tenant/customer context is active. | Resolve before tenant-sensitive capability checks. Until `PlatformTenant` exists, bridge through current `BusinessId` where records are business-scoped. |
| Package entitlement | Which capability groups a tenant is entitled to use. | Future `TenantPackageAssignment` and `TenantFeatureOverride` decide tenant entitlement; package enablement does not grant permission. |
| Business exception | Scoped exception inside a tenant. | Existing `BusinessFeatureOverride` can narrow or expand a business only under tenant entitlement rules. |
| Capability gate | Runtime availability of a stable capability code. | A shared `ICapabilityGate` should return allowed/blocked with code, surface, tenant/business context, and safe reason. |
| Authorization | User permission for an allowed capability. | Existing permission services and auth policies remain authoritative. |
| Provider readiness | Whether a provider/storage profile can execute successfully. | Provider add-ons must fail blocked/readiness state instead of recording success. |

## Target Runtime Components

| Component | Purpose | First implementation slice |
| --- | --- | --- |
| `CapabilityGateContext` | Audience, host, tenant id when available, business id when available, user id, route/action id, and capability code. | Capability enforcement foundation. |
| `ICapabilityGate` | Shared Application-level service that evaluates entitlement, business override, provider readiness when requested, and disabled-mode result. | Capability enforcement foundation. |
| `CapabilityDisabledResult` | Stable result with `FeatureDisabled`, capability code, audience-safe message, optional business id, and correlation id. | Disabled behavior guard. |
| WebAdmin capability filter | Action filter or endpoint convention that runs after authentication and before handler mutation. | WebAdmin capability guard slice. |
| WebAdmin navigation provider | Capability-aware navigation model used by layout and sidebars. | WebAdmin capability navigation slice. |
| WebApi capability attribute/filter | Route-level capability declaration with structured problem response. | WebApi capability filter slice. |
| Client capability metadata query | Read-only metadata for Web, mobile consumer, and mobile business navigation gating. | Capability metadata contract slice. |
| Worker skip policy | Tenant/provider-aware skip decision for background work. | Worker capability skip policy slice. |
| Provider readiness contract | Provider/storage readiness without secrets or raw payloads. | Provider capability readiness guard slice. |

## Surface Decisions

| Surface | Disabled navigation | Direct access | Required evidence |
| --- | --- | --- | --- |
| WebAdmin | Hide module navigation, tabs, action buttons, and overview cards when disabled. | Authenticated direct URL returns `403 FeatureDisabled`; nonexistent resources remain not-found. | Source tests for nav hiding and action filter attributes. |
| Public WebApi | Omit disabled discovery surfaces when possible. | Capability-specific route returns structured problem with `FeatureDisabled` unless concealment is required. | Contract tests for public routes that can be disabled. |
| Member WebApi | Hide via metadata in clients. | Structured problem with `FeatureDisabled`. | Contract tests for member routes and stable error shape. |
| Business WebApi | Hide via metadata in clients. | Structured problem with `FeatureDisabled`. | Contract tests for business routes and business-access separation. |
| Darwin.Web storefront | Hide disabled CMS/catalog/checkout/member surfaces. | Server-side gate blocks disabled checkout/payment side effects. | Front-office route/source tests. |
| Mobile consumer | Use metadata to hide unavailable member, loyalty, commerce, payslip, and notification surfaces. | Server still gates direct API calls. | Mobile route contract and metadata parsing tests. |
| Mobile business | Use metadata to hide unavailable loyalty, billing, media, future warehouse, and operations surfaces. | Server still gates direct API calls. | Mobile business route and access-state tests. |
| Worker | Do not start tenant/provider work when disabled. | Record safe skip evidence only; never mark shipment, archive, export, payment, notification, or provider delivery as successful. | Worker source guards and focused skip tests. |
| Provider callback | Accept only configured callback routes and process only enabled provider capabilities. | Disabled provider callback can be recorded as ignored/rejected provider evidence, not success. | Callback inbox tests with no-secret payload handling. |

## Capability Declaration Rules

| Route/action type | Declaration rule |
| --- | --- |
| Base platform routes | No business package dependency; still require tenant/domain and authorization where applicable. |
| Module list/detail pages | Declare the module capability code, such as `finance` or `inventory`. |
| Lifecycle mutations | Declare both module capability and owning action permission. |
| Provider operation actions | Declare provider add-on capability plus owning base capability. |
| Storage-backed artifact actions | Declare owning module capability and storage readiness requirement. |
| Cross-module read panels | Declare owner capability for the page and optional read dependency capability for the panel. Disabled dependency hides the panel, not the owner page. |

## Dependency Rules

| Dependency shape | Decision |
| --- | --- |
| Required dependency disabled | Block the owner workflow and show disabled/readiness state. |
| Optional enhancement disabled | Hide enhancement UI and keep base workflow usable. |
| Provider add-on disabled | Keep base module usable; block provider-specific action. |
| Read-only cross-module context disabled | Hide the panel or return empty audience-safe context; do not mutate another module. |
| Historical data after downgrade | Preserve records and read-only audit/history where legally or operationally required; block new mutations. |

## Disabled-Mode Response Contract

| Field | Decision |
| --- | --- |
| `code` | `FeatureDisabled`. |
| `capability` | Stable capability code from `capability-catalog.md`. |
| `surface` | `WebAdmin`, `WebApiPublic`, `WebApiMember`, `WebApiBusiness`, `MobileConsumer`, `MobileBusiness`, `Worker`, `Provider`, or `Storefront`. |
| `message` | Localized/audience-safe text; no package internals, no provider credentials, no tenant commercial terms. |
| `businessId` | Include only when safe for the authenticated audience. |
| `correlationId` | Follow existing request correlation conventions when available. |

## Implementation Order

1. `Capability Enforcement Foundation Core Slice`: add shared result types and `ICapabilityGate` using `FeatureAreaService`, with no surface behavior change until filters opt in.
2. `WebAdmin Capability Navigation And Action Guard Slice`: capability-aware navigation and action filter source tests.
3. `WebApi Capability Filter And Problem Contract Slice`: route attributes, structured problem response, public/member/business contract tests.
4. `Capability Metadata Contract Slice`: Web/mobile read-only metadata for navigation gating.
5. `Worker Capability Skip Policy Slice`: tenant/provider skip behavior and no-fake-success tests.
6. `Provider And Storage Readiness Guard Slice`: provider add-ons and object-storage-dependent actions align with capability gates.

## Source Guards For Implementation

- No package entitlement used as a permission grant.
- No action route added without a capability declaration when it belongs to a module.
- No public/mobile route added for ERP modules unless module design explicitly allows it.
- No worker hosted service processes tenant/provider records without a skip policy.
- No provider/storage action records success when the provider profile is disabled or not ready.
- No disabled-mode response contains secrets, raw provider payload, package pricing details, or internal deployment configuration.

## Test Plan For Future Slices

| Lane | Required coverage |
| --- | --- |
| Unit | `ICapabilityGate` entitlement, business override, dependency, and provider-readiness decisions. |
| WebAdmin | Navigation hidden, direct URL blocked with `FeatureDisabled`, permissions still enforced. |
| WebApi | Structured problem response for disabled public/member/business routes. |
| Mobile/shared | Metadata parsing and navigation gating without route removal. |
| Worker | Disabled/provider-not-ready skip with no fake success. |
| Source guards | Route/action declarations, no provider secrets in metadata, no public/mobile exposure for internal modules. |

## No Runtime Behavior Changes

This design does not implement gates. Current code remains permission-based and route-based until the implementation slices above are executed.
