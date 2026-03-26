# Darwin WebApi Guide

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![REST](https://img.shields.io/badge/API-REST-0A66C2)](https://restfulapi.net/)

> Scope: `Darwin.WebApi`, its audience segmentation, contract rules, security model, BFF direction, and delivery expectations.

## Purpose

`Darwin.WebApi` is the shared HTTP delivery boundary for:

- `Darwin.Web` (public storefront and member portal)
- MAUI mobile applications
- future isolated consumers such as SharePoint web parts or other external systems
- selected admin/integration workflows where HTTP delivery is explicitly required

`Darwin.WebApi` is not a thin EF façade and it is not a transport layer for back-office DTO reuse. It must remain contracts-first and audience-aware.

## Composition

- composition root: `src/Darwin.WebApi/Extensions/DependencyInjection.cs`
- controllers: `src/Darwin.WebApi/Controllers`
- business logic: `src/Darwin.Application`
- persistence/security: `src/Darwin.Infrastructure`
- API contracts: `src/Darwin.Contracts`

## Audience Segmentation

The API must be designed as separate logical surfaces sharing a common host.

Canonical route ownership should now be audience-first:

- `api/v1/public/*` for anonymous storefront and CMS delivery
- `api/v1/member/*` for authenticated end-user operations
- `api/v1/business/*` for business/mobile operator workflows
- `api/v1/admin/*` for future back-office/integration delivery where HTTP is justified

Legacy non-audience-prefixed aliases may remain temporarily for backward compatibility, but new work should always attach to the audience-first canonical route.

### 1. Public API

For anonymous or low-friction browsing scenarios:

- CMS pages
- menus and navigation
- SEO metadata
- product, category, and storefront discovery
- anonymous cart bootstrap where required

This surface is consumed primarily by `Darwin.Web` and future public widgets.

### 2. Member API

For authenticated end users:

- profile and account settings
- addresses
- order history
- invoice history
- loyalty balances, rewards, and transaction history
- support- and CRM-adjacent customer views

This surface is consumed by the member portal in `Darwin.Web` and by consumer mobile apps.

### 3. Business / Mobile API

For authenticated business-side or operational mobile scenarios:

- loyalty scan flows
- loyalty accrual and redemption processing
- campaigns
- subscriptions
- business discovery and onboarding

This surface is consumed by `Darwin.Mobile.Business` and business-oriented workflows.

### 4. Admin API

This surface should exist only where the back-office genuinely benefits from HTTP delivery instead of direct application-handler usage.

Use it for:

- integration-oriented admin tasks
- future BFF/admin gateway scenarios
- cross-system automation

Do not mix Admin API contracts with public/member contracts.

## Contract Rules

- `Darwin.Contracts` is the source of truth for external request/response models
- public/member contracts must not reuse admin operational DTOs
- member DTOs must be consumer-friendly and presentation-oriented
- CRM admin DTOs must remain separate from public CRM/customer-facing projections
- inventory and billing DTOs must be designed per audience, not per table

Examples:

- public product cards are not admin product edit DTOs
- member invoice summaries are not admin invoice management DTOs
- loyalty totals must be projected from loyalty transactions, not copied from CRM entities

## Current and Planned Endpoint Groups

### Existing active groups

- auth
- profile
- loyalty
- notifications
- businesses/discovery
- billing subscriptions
- meta/health

Recent route organization changes:

- business discovery and public business detail now live under a dedicated public controller
- member business onboarding, engagement, and review actions now live under a dedicated member controller
- auth, profile, notification, and business billing controllers now use audience-first canonical route roots while preserving legacy aliases
- loyalty is now split between a dedicated member controller (`api/v1/member/loyalty`) and a dedicated business controller (`api/v1/business/loyalty`), while legacy `/api/v1/loyalty/*` aliases remain temporarily available
- member order history and member invoice history now have dedicated canonical route roots under `api/v1/member/orders` and `api/v1/member/invoices`, while legacy `/api/v1/orders/*` and `/api/v1/invoices/*` aliases remain temporarily available
- public CMS and public catalog delivery now have dedicated canonical route roots under `api/v1/public/cms` and `api/v1/public/catalog`, while legacy `/api/v1/cms/*` and `/api/v1/catalog/*` aliases remain temporarily available

### Required public groups

These should be documented and expanded as implementation continues:

- `/api/v1/public/cms/pages`
- `/api/v1/public/cms/menus`
- `/api/v1/seo/*`
- `/api/v1/public/catalog/products`
- `/api/v1/public/catalog/categories`
- `/api/v1/storefront/*`

Current public delivery ownership:

- public CMS: published page listing, page-by-slug delivery, and menu delivery
- public catalog: published category listing, product listing, and product-by-slug delivery
- legacy `/api/v1/cms/*` and `/api/v1/catalog/*` aliases remain only for compatibility and should not be used for new development

### Required member groups

- `/api/v1/profile/*`
- `/api/v1/member/orders/*`
- `/api/v1/member/invoices/*`
- `/api/v1/member/loyalty/*`
- `/api/v1/member/*`

Current loyalty ownership:

- member loyalty: scan preparation, account summaries, reward browsing, timeline, promotions, and join flows
- business loyalty: reward configuration, scan processing, accrual/redemption confirmation, and campaign management
- legacy mixed loyalty routes remain only as compatibility aliases and should not be used for new development

Current member commerce ownership:

- member orders: paged order history and order detail under the member route root
- member invoices: paged invoice history and invoice detail under the member route root
- legacy `/api/v1/orders/*` and `/api/v1/invoices/*` aliases remain only for compatibility and should not be used for new development

### Required CRM admin/integration groups

- `/api/v1/admin/crm/customers/*`
- `/api/v1/admin/crm/leads/*`
- `/api/v1/admin/crm/opportunities/*`
- `/api/v1/admin/crm/interactions/*`

### Required inventory and billing groups

- `/api/v1/admin/inventory/warehouses/*`
- `/api/v1/admin/inventory/stock-levels/*`
- `/api/v1/admin/inventory/transfers/*`
- `/api/v1/admin/billing/payments/*`
- `/api/v1/admin/billing/invoices/*`
- `/api/v1/admin/accounting/*`

## REST Design Rules

The current focus remains REST.

Requirements:

- use resource-oriented routes
- use standard HTTP methods correctly
- return standard HTTP status codes
- return problem details for validation and operational errors
- support filtering, sorting, and paging where list endpoints are expected to grow
- keep idempotent operations idempotent where possible

Typical conventions:

- `200 OK` for successful reads and updates with response payload
- `201 Created` for creation with location metadata where appropriate
- `204 No Content` for successful command-style operations without a payload
- `400 Bad Request` for validation and malformed request issues
- `401 Unauthorized` for missing/invalid auth
- `403 Forbidden` for policy failures
- `404 Not Found` when the resource does not exist
- `409 Conflict` for deterministic concurrency or state conflicts

## Security Model

`Darwin.WebApi` must support shared security patterns across multiple consumers.

Current direction:

- JWT bearer authentication for API-native clients
- cookie-backed token/session delivery where a BFF or web-oriented transport needs it
- policy-based authorization
- claims-based current-user resolution
- optimistic concurrency via `RowVersion` where update collisions matter

Front-office, mobile, and future consumers must all rely on the same core authorization model, even if the transport pattern differs.

## BFF Direction

The architecture must remain compatible with a future Backend-for-Frontend layer.

### Why BFF may be introduced

- centralize cookie/session handling for `Darwin.Web`
- reduce chatty API traffic from React pages
- compose multiple API calls into one web-optimized response
- enforce edge caching and response shaping
- isolate external clients such as SharePoint web parts from internal service complexity

### ASP.NET Core BFF options

Likely future approaches:

- a dedicated ASP.NET Core BFF host
- a reverse-proxy/gateway pattern using YARP
- route-specific composition endpoints for front-office delivery

The current WebApi design must not block this future split. Keep contracts clean, keep audience separation explicit, and avoid coupling all clients to the same operational DTOs.

## Extensibility Direction

The primary delivery style is REST today, but the architecture should remain extensible toward:

- gRPC for high-efficiency service-to-service integration
- GraphQL for selective front-end query composition where justified

These are future options only. Do not dilute the current REST design by prematurely mixing paradigms into the main delivery path.

## Front-Office and CMS Guidance

For `Darwin.Web`:

- CMS pages, menus, and SEO metadata must come from WebApi
- public storefront endpoints must be stable and cache-friendly
- member endpoints must remain distinct from public endpoints
- storefront contracts must be designed for SSR/SSG/ISR use cases, not for admin edit screens

## CRM, Billing, and Inventory Guidance

The new domain modules must also be reflected in API design:

- CRM surfaces should expose leads, opportunities, interactions, and customer summaries through explicit contracts
- billing surfaces should expose payments, invoices, and accounting-adjacent summaries per audience
- inventory surfaces should expose warehouses, stock levels, and transfers for admin/integration use
- warehouse-aware order fulfillment should propagate through contracts when order and fulfillment APIs are expanded

## Delivery Checklist for Any New Endpoint

When adding or changing an endpoint, update these together:

1. `Darwin.Contracts`
2. `Darwin.Application`
3. `Darwin.WebApi`
4. this document
5. any affected consumer docs (`DarwinFrontEnd.md`, `DarwinMobile.md`, `DarwinWebAdmin.md`)
