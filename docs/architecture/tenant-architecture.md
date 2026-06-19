# Tenant Architecture

Reviewed: 2026-06-19

## Summary

This document is a code-backed tenant architecture design. It is documentation-only and adds no `TenantId`, entity, migration, route, DTO, runtime gate, WebAdmin action, mobile contract, worker registration, provider flow, or production behavior.

Darwin currently uses `BusinessId` as the operational scope for most merchant data. That is not the final customer, account, deployment, or data-isolation boundary. The canonical future boundary is `PlatformTenant`. A tenant can own one or more businesses, domains, package assignments, and data-store routing records.

## Current Code Findings

| Surface | Current evidence | Finding |
| --- | --- | --- |
| Merchant root | `src/Darwin.Domain/Entities/Businesses/Business.cs` defines `Business` with legal, brand, communication, subscription, member, location, and lifecycle fields. | `Business` is the current merchant and operational scope. It should become a child of `PlatformTenant`, not the tenant itself. |
| Business scoping | Validators and handlers across Application require `BusinessId`; examples include `src/Darwin.Application/Businesses/Validators` and Inventory validators. | Existing data isolation is mostly business-scoped. Tenant rollout must bridge current `BusinessId` before broad required tenant columns are introduced. |
| Token scope | `src/Darwin.Infrastructure/Security/Jwt/JwtTokenService.cs` emits a `business_id` claim when a user has an active business membership. | Auth tokens do not carry tenant identity yet. Future tokens may include tenant context, but must preserve business selection compatibility. |
| Checkout URL generation | `src/Darwin.WebApi/Services/StorefrontCheckoutUrlBuilder.cs` uses `StorefrontCheckout:FrontOfficeBaseUrl`. | Checkout return/cancel links are currently global-config based, not tenant-domain-aware. |
| Persistence selection | `src/Darwin.Infrastructure.PersistenceProviders/Extensions/ServiceCollectionExtensions.ConfiguredPersistence.cs` selects PostgreSQL or SQL Server from configuration. | Provider selection is host-level. Tenant database routing does not exist yet. |
| Startup migrations | `MigrateAndSeedAsync` in `src/Darwin.Infrastructure/Extensions/ServiceCollectionExtensions.Persistence.cs` migrates one configured `DarwinDbContext`. | Multi-database tenant migration orchestration is a future requirement. |
| Web startup | WebAdmin and WebApi startup call forwarded headers, localization, static media, auth, and endpoint mapping in `Startup.cs` files. | Host resolution middleware must be introduced before tenant-sensitive URL, auth, media, and feature decisions. |
| Feature foundation | `FeatureAreaService`, `BusinessFeatureOverride`, and package design docs already distinguish capability and commercial entitlement. | Tenant package assignment should sit above `FeatureArea`; business overrides remain scoped exceptions inside tenant entitlement. |

## Canonical Tenant Model

| Concept | Future model name | Ownership |
| --- | --- | --- |
| Customer/account/deployment boundary | `PlatformTenant` | Commercial account, isolation mode, status, deployment mode, default locale, package entitlement owner. |
| Tenant domain identity | `TenantDomain` | Darwin-hosted and customer-owned storefront, admin, and API hostnames with verification state. |
| Data-store routing metadata | `TenantDataStore` | Shared or dedicated database routing, provider type, secret reference, migration state, and move status. |
| Tenant package assignment | `TenantPackageAssignment` | Package/plan entitlement, effective dates, downgrade state, and audit evidence. |
| Tenant feature exception | `TenantFeatureOverride` | Direct tenant-level capability exception outside normal package mapping. |
| Business exception | `BusinessFeatureOverride` | Existing business-level exception under an entitled tenant. |

## Tenant Relationship Rules

| Relationship | Decision |
| --- | --- |
| Tenant to business | One `PlatformTenant` can own multiple `Business` records. A business belongs to exactly one tenant after migration. |
| Tenant to user | Users can belong to multiple businesses and therefore multiple tenants through memberships. Future auth context must disambiguate tenant and selected business. |
| Tenant to package | Package assignment belongs to tenant, not directly to each business. Business-level overrides are exceptions. |
| Tenant to database | Shared-hosted tenants can share a physical database. Large tenants can route to a dedicated database. On-premise deployments can run single-tenant without hosted control-plane dependency. |
| Tenant to domain | A tenant can have multiple active or pending domains, but only verified active domains route tenant traffic. |

## Status And Mode Vocabulary

| Enum | Values | Meaning |
| --- | --- | --- |
| `TenantDeploymentMode` | `HostedShared`, `HostedDedicatedDatabase`, `HostedDedicatedInstance`, `OnPremise` | How the tenant is hosted and operated. |
| `TenantStatus` | `Provisioning`, `Active`, `Suspended`, `Maintenance`, `Archived` | Whether tenant traffic and operations are allowed. |
| `TenantIsolationMode` | `SharedDatabase`, `DedicatedDatabase`, `DedicatedInstance`, `OnPremiseSingleTenant` | Data and runtime isolation semantics. |
| `TenantDomainKind` | `Storefront`, `WebAdmin`, `WebApi`, `ProviderCallback` | Intended host purpose. |
| `TenantDomainStatus` | `PendingVerification`, `Active`, `Suspended`, `Archived` | Routing eligibility. |

## Integration With Packages And Features

Tenant entitlement is evaluated before business override:

1. Resolve `PlatformTenant` from host, token context, or explicit administrative context.
2. Resolve tenant package and tenant feature overrides.
3. Resolve selected `Business` inside that tenant.
4. Apply `BusinessFeatureOverride` only if tenant entitlement allows the capability or a tenant override explicitly grants it.
5. Apply user authorization separately; capability enablement is never a permission grant.

## First Implementation Slice

The first safe implementation slice is `Tenant Catalog And Domain Resolution Foundation Slice`:

- Add tenant catalog/domain/data-store metadata only, with no broad `TenantId` backfill.
- Add host resolution service and source-contract tests, but keep runtime behavior guarded behind explicit non-breaking paths.
- Backfill a single default platform tenant for existing businesses as metadata.
- Do not alter existing `BusinessId` contracts, mobile routes, checkout contracts, provider callbacks, or migrations for operational entities.

## Non-Goals

- No `TenantId` addition to operational entities in this design step.
- No database split, project split, or runtime route change.
- No replacement of `Business`, `BusinessSubscription`, `BillingPlan`, `FeatureArea`, or `BusinessFeatureOverride`.
- No provider credential, checkout, callback, email, mobile, worker, or archive behavior change.
