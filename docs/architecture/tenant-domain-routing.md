# Tenant Domain Routing

Reviewed: 2026-06-19

## Summary

This document defines future tenant-aware domain routing for Darwin. It is documentation-only and changes no routing, host resolution, callback, cookie, OAuth, checkout, email, mobile, or provider behavior.

Darwin must support hosted subdomains, customer-owned storefront domains, customer-owned admin domains, shared API hosts, dedicated app instances, and on-premise installations. Only verified active tenant domains route tenant traffic.

## Current Code Findings

| Surface | Current evidence | Routing gap |
| --- | --- | --- |
| WebAdmin | `src/Darwin.WebAdmin/Program.cs` delegates to `UseWebStartupAsync`; startup maps the default MVC route after auth. | No tenant host resolution layer is present before WebAdmin routing. |
| WebApi | `src/Darwin.WebApi/Program.cs` delegates to `UseWebApiStartupAsync`; controllers are mapped after auth and middleware. | API host resolution is not tenant-aware. |
| Checkout links | `StorefrontCheckoutUrlBuilder` builds links from global `StorefrontCheckout:FrontOfficeBaseUrl`. | Return/cancel URLs need tenant-domain-aware generation. |
| Provider callbacks | Provider callback inbox and workers process provider messages under shared Integration surfaces. | Callback host strategy must separate global provider ingress from tenant-specific callback links where providers require tenant routing. |
| Data protection | `AddSharedHostingDataProtection` uses a shared application name and key path from configuration. | Cookie/auth domain and key-ring strategy must be explicit per shared, dedicated, and on-premise deployments. |
| Mobile base URLs | Mobile appsettings carry API base configuration. | Mobile clients need tenant/capability metadata rather than hardcoded per-business assumptions. |

## Domain Kinds

| Domain kind | Example shape | Routing owner | Notes |
| --- | --- | --- | --- |
| Darwin-hosted storefront | `{tenant}.shop.darwin.example` | `TenantDomain` | For small customers and hosted tenants. |
| Darwin-hosted WebAdmin | `{tenant}.admin.darwin.example` | `TenantDomain` | Allows tenant-branded admin access without custom DNS. |
| Customer storefront domain | `shop.customer.example` | `TenantDomain` | Requires verification and TLS readiness before active routing. |
| Customer WebAdmin domain | `admin.customer.example` | `TenantDomain` | Requires stricter cookie, CSP, and certificate handling. |
| Shared API host | `api.darwin.example` | Platform host plus tenant context | Tenant can be resolved by token, request header for trusted clients, or business context. |
| Dedicated API host | `api.customer.example` | `TenantDomain` | Used for dedicated instance or high-isolation deployments. |
| Provider callback host | `callbacks.darwin.example` or dedicated callback host | Platform or tenant domain policy | Provider-specific callback strategy must not expose secrets in URL metadata. |

## Resolution Order

1. Normalize `Host` using forwarded-header-aware request data.
2. Resolve active `TenantDomain` by normalized host and domain kind.
3. Reject unverified, suspended, archived, or kind-mismatched domains.
4. Resolve `PlatformTenant` and confirm tenant status allows the requested surface.
5. Resolve selected `Business` only after tenant is resolved.
6. Apply capability/package gating and authorization.

## Fail-Safe Behavior

| Condition | Public storefront | WebAdmin | WebApi | Worker/provider callback |
| --- | --- | --- | --- | --- |
| Unmapped host | Neutral not-found response without tenant data. | Safe not-found or tenant resolution error before auth-sensitive data loads. | Structured problem without tenant details. | Reject or park message with safe provider-operation evidence. |
| Inactive tenant | Hide storefront or show neutral maintenance page. | `403 TenantDisabled` or maintenance response after authentication. | Structured `TenantDisabled` problem. | Skip tenant work without success claims. |
| Tenant maintenance | Tenant maintenance page if configured. | Maintenance response unless platform support role bypass is explicitly designed. | Structured maintenance problem. | Pause tenant-specific jobs. |
| Unverified domain | Never route tenant traffic. | Never route tenant admin traffic. | Never route tenant API traffic. | Provider callback must use verified callback host only. |
| Disabled capability | Hide navigation; direct URL returns `403 FeatureDisabled`. | Same. | Structured `FeatureDisabled` problem. | Skip safely and record no fake success. |

## URL Generation Rules

| URL type | Future rule |
| --- | --- |
| Storefront links | Resolve tenant's active storefront domain; fallback to Darwin-hosted storefront domain only if verified and active. |
| WebAdmin links | Resolve active admin domain for operator context; never build admin links from storefront domains. |
| Checkout return/cancel | Replace global-only base URL use with tenant-domain-aware URL builder. |
| OAuth/external login redirects | Use registered tenant/admin/storefront redirect hosts only; no wildcard redirect generation. |
| Provider callbacks | Prefer stable platform callback host unless provider requires tenant-specific host. Link callback records to tenant through safe metadata or resolved business context. |
| Email links | Use tenant domain appropriate to audience: storefront/member links for customers, admin links for operators. |
| Storage/media URLs | Public media URLs must not expose private storage paths and must honor tenant domain and storage-profile policy. |

## Cookie And Auth Domain Rules

| Surface | Decision |
| --- | --- |
| Shared Darwin admin host | Cookie scope should stay host-specific or parent-domain-scoped only when all child admin hosts are trusted Darwin domains. |
| Customer admin domain | Cookie scope must be exact host by default. |
| Storefront/member auth | Keep customer-facing cookies separate from WebAdmin cookies. |
| Dedicated instance | Data Protection application name and key ring can be deployment-specific. |
| On-premise | Key ring and cert ownership belong to customer infrastructure; no hosted control-plane key dependency. |

## HTTPS And Certificate Assumptions

- Every active custom domain requires DNS verification and TLS certificate readiness before routing.
- Darwin-hosted domains can use platform-managed wildcard or automated certificates.
- On-premise deployments can use customer-managed certificates.
- Failed certificate readiness blocks activation; it must not silently route tenant traffic over an insecure fallback.

## First Implementation Slice

`Tenant Catalog And Domain Resolution Foundation Slice` should add:

- Tenant/domain metadata and verification status.
- A host resolver service with tests for unmapped, inactive, unverified, hosted, custom, and on-premise modes.
- No behavior change to current routes until explicit gated middleware is introduced in a later slice.
