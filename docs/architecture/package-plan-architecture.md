# Package And Plan Architecture Over Capabilities

Reviewed: 2026-06-19

## Summary

This document designs Darwin's commercial package and plan layer over the existing capability foundation. It is documentation-only. It does not add entities, migrations, runtime gates, routes, DTOs, WebAdmin actions, mobile contracts, worker registrations, or provider flows.

Darwin should sell packages and plans, not raw modules. Some capabilities are foundational, and some capabilities are tightly connected in ways that make standalone enablement unsafe. `FeatureArea` remains the technical capability unit. Commercial package and tenant entitlement must be a separate layer above it.

## Current Code Context

| Current model | Current role | Design decision |
| --- | --- | --- |
| `FeatureArea` | Technical capability/visibility unit in Foundation. | Keep. It is the unit runtime gates will eventually evaluate. |
| `BusinessFeatureOverride` | Business-level feature exception. | Keep. Use only for scoped business exceptions after tenant entitlement is resolved. |
| `BillingPlan` | Current subscription plan with price and `FeaturesJson`. | Keep. Treat as current subscription billing model, not a complete ERP package contract. |
| `BusinessSubscription` | Current business subscription to a billing plan. | Keep. It remains current commercial subscription state until tenant-level entitlement is introduced. |
| `BusinessFeatureUsage` | Usage tracking for billable or limited business features. | Keep. It is usage evidence, not the source of package entitlement. |

## Domain Names

| Concept | Canonical name | Purpose |
| --- | --- | --- |
| Technical capability | `FeatureArea` | Stable capability code used by runtime gates and module visibility. |
| Commercial bundle | `PlatformPackage` | A marketable bundle of capabilities and add-ons. |
| Priced variant | `PlatformPlan` | A price, billing period, and limits variant for a package. |
| Package capability mapping | `PackageFeatureArea` | Included, required, optional, or blocked capability rows for a package. |
| Tenant entitlement | `TenantPackageAssignment` | What a tenant/customer is entitled to use. |
| Tenant direct override | `TenantFeatureOverride` | Audited tenant-level exception outside normal package mapping. |
| Business exception | `BusinessFeatureOverride` | Current per-business override, only after tenant entitlement allows the capability. |

## Layering

| Layer | Owns | Does not own |
| --- | --- | --- |
| Capability layer | Technical feature codes and runtime enablement state. | Pricing, subscription lifecycle, provider credentials, permission grants. |
| Package layer | Commercial bundles of capabilities and add-ons. | Runtime authorization or direct provider readiness. |
| Plan layer | Price, billing interval, plan limits, billing-provider mapping. | Capability implementation details. |
| Tenant assignment layer | Tenant/customer entitlement, effective dates, downgrade/upgrade history. | Business-specific exceptions or user permissions. |
| Business override layer | Business-level exception inside a tenant. | Tenant commercial entitlement. |
| Permission layer | User authorization. | Capability entitlement. |

Capability enablement must never grant authorization. Authorization still comes from roles, permissions, auth policies, and audience-specific access checks.

## Tenant Boundary

`BusinessId` is the current operational scope used by most records and subscriptions. Package architecture must not permanently bind entitlement to Business-only subscriptions. The future entitlement owner should be `Tenant` or `PlatformTenant`, with one or more businesses inside that tenant.

Until `Tenant`/`PlatformTenant` is implemented, designs can bridge to `BusinessId` through current `BusinessSubscription`, but the package model must keep the future tenant boundary explicit.

## Package Change Semantics

| Change | Expected behavior |
| --- | --- |
| Upgrade | Enable newly entitled capability gates after audit, dependency validation, and provider readiness checks. |
| Downgrade | Hide and block future behavior, but preserve historical records. |
| Add provider add-on | Enable only after secure configuration and readiness pass. |
| Remove provider add-on | Stop new provider operations, keep historical provider records and callbacks auditable. |
| Tenant override | Record audited exception and keep it separate from package definition. |
| Business override | Apply only inside a tenant already entitled to that capability unless an explicit tenant override allows it. |

Package changes must not delete tenant data automatically. Data retention and deletion require a separate retention policy.

## Implementation Sequence

1. `Tenant Catalog And Domain Resolution Foundation Slice`: introduce tenant/domain catalog safely before broad entitlement or `TenantId` migration.
2. `Package/Plan Foundation Core Slice`: add package, plan, mapping, assignment, and audit models after tenant boundary is locked.
3. `Capability Enforcement Foundation Core Slice`: implement shared runtime gates over `FeatureArea` and package assignments using [capability-enforcement-design.md](capability-enforcement-design.md).
4. `Module Disabled Behavior Guard Core Slice`: implement WebAdmin/WebApi/Worker/client disabled-mode behavior with source-contract tests.

## Non-Goals

- No project split or database split solely for commercial packages.
- No replacement of `FeatureArea` or `BusinessFeatureOverride`.
- No replacement of current `BillingPlan`/`BusinessSubscription` in this design step.
- No customer-specific package names.
- No provider credential or storage profile changes.
