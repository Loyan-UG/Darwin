# Tenant Data Scope Matrix

Reviewed: 2026-06-19

## Summary

This matrix classifies major Darwin data by future tenant scope. It is documentation-only and does not add `TenantId`, indexes, migrations, filters, or query changes.

Current code mostly uses `BusinessId` as the operational scope. Future tenant migration must preserve existing business-scoped behavior while introducing `PlatformTenant` above `Business`.

## Scope Categories

| Scope | Definition |
| --- | --- |
| `global/platform` | Shared across the hosted platform or app installation. |
| `tenant` | Owned by a `PlatformTenant` and shared across its businesses. |
| `business-under-tenant` | Owned by one `Business` that belongs to a tenant. |
| `user` | Owned by a user identity and may cross tenants through memberships. |
| `provider-operation` | Operational integration evidence that may link to tenant/business after resolution. |
| `audit/log` | Append-only or operational evidence, with tenant/business links when available. |
| `lookup/reference` | Shared reference values or seed catalogs. |

## Major Entity Scope Matrix

| Entity group | Current scope evidence | Future scope | Tenant migration decision |
| --- | --- | --- | --- |
| `Business`, locations, members, invitations | `Business` root in `src/Darwin.Domain/Entities/Businesses/Business.cs`; many child records use `BusinessId`. | `business-under-tenant` | Add tenant ownership to business first; child records can inherit tenant through business until direct tenant columns are justified. |
| Users, roles, permissions, tokens | Identity entities and JWT service select active business membership. | `user` plus tenant memberships | Users remain platform identities; tenant access derives from business/tenant memberships. |
| `FeatureArea` | Foundation technical capability. | `global/platform` | Capability catalog remains global; enablement assignments become tenant/business scoped. |
| `BusinessFeatureOverride` | Business feature exception. | `business-under-tenant` | Keep as business exception below tenant entitlement. |
| Future package assignments | Designed in package architecture docs. | `tenant` | `TenantPackageAssignment` and `TenantFeatureOverride` belong to tenant. |
| CMS pages/media | CMS schema and media storage profiles. | Usually `business-under-tenant`, sometimes `tenant` | Storefront content should be tenant/business-aware before custom-domain routing claims. |
| Catalog products/categories/variants | Catalog entities used by storefront, checkout, inventory, procurement. | `business-under-tenant` | Unique SKU/slug constraints must become tenant/business-aware where currently global. |
| Cart/checkout | CartCheckout and order creation paths. | `business-under-tenant` | Checkout must resolve tenant domain and business before creating orders. |
| Orders, invoices, payments, refunds | Orders/Billing entities link to business/customer/order contexts. | `business-under-tenant` | Historical records keep business; future tenant backfill derives from business. |
| CRM customers/leads/opportunities | CRM entities often include optional or required business links. | `business-under-tenant` | Cross-business CRM inside one tenant requires explicit design before shared customer views. |
| Loyalty programs/accounts/ledger | Loyalty uses business/member context. | `business-under-tenant` | Tenant-wide loyalty is a future feature, not the default assumption. |
| Inventory warehouses/stock/transactions | Inventory handlers validate `BusinessId`; warehouses and stock records are business-scoped. | `business-under-tenant` | Stock ledger remains business-owned; tenant grouping is for isolation and reporting. |
| Procurement suppliers/PO/receipts | Procurement is implemented under Inventory schema and uses business context. | `business-under-tenant` | Supplier master can later become tenant-shared only after duplicate and accounting policies are designed. |
| Sales documents | Sales quote, delivery, return documents use business/order/customer context. | `business-under-tenant` | Tenant scope derives from business/order. |
| Finance/payables/treasury | Billing schema contains finance posting, supplier invoice/payment, bank evidence. | `business-under-tenant`, tenant reporting later | Journal facts remain business-scoped first; tenant consolidation requires reporting rules. |
| Shipping/provider operations | Shipping and provider operation records link to order/business/provider context. | `provider-operation` plus business link | Provider callbacks need tenant-safe correlation and parked-message behavior. |
| Communications | Dispatch/audit records can be business-scoped or platform-operational. | `provider-operation` plus business/tenant when available | Provider secrets stay outside tenant metadata. |
| Site settings | Settings schema stores platform-wide settings. | `global/platform` or deployment-local | Tenant-specific settings require a separate model instead of overloading global settings. |
| Object storage profiles | Configuration-driven profiles. | deployment/global plus tenant references later | Store secret references and profile names, not raw secrets in tenant rows. |
| Business events/audit trails | Foundation evidence records can include business/customer/entity context. | `audit/log` | Future records should include tenant where available; history must not be rewritten destructively. |
| External systems/references | Integration foundation links local and external identities. | tenant or business under tenant | External identity uniqueness must include tenant/business owner where relevant. |
| Lookup/reference seeds | Tax categories, role keys, feature codes, provider kinds. | `lookup/reference` | Remain global unless a tenant-customization policy exists. |

## Indexes That Must Become Tenant-Aware

| Current uniqueness family | Future tenant-aware rule |
| --- | --- |
| Business-facing slugs/codes/SKUs | Include `TenantId` or derive uniqueness through business under tenant. |
| External references | Include owner tenant/business plus external system identity. |
| Provider operation idempotency keys | Include provider, tenant/business correlation when the provider event can be tenant-specific. |
| Document numbers | Number sequences stay scoped by configured business/tenant policy, not globally by accident. |
| Domains | Hostname uniqueness is platform-global for hosted deployments; on-premise can be installation-local. |
| Package assignments | Active package assignment uniqueness is tenant-scoped. |
| Data-store routes | Active route uniqueness is tenant-scoped; physical connection references are not stored as raw secrets. |

## Migration Implication

The safe rollout starts with tenant metadata and `Business.PlatformTenantId`, then derives tenant scope for business-owned records through joins. Required `TenantId` columns on operational tables should be added only after source-contract tests prove query filters, unique indexes, API contracts, migration backfill, and dedicated database routing are ready.
