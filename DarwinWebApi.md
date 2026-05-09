# Darwin WebApi Guide

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![REST](https://img.shields.io/badge/API-REST-0A66C2)](https://restfulapi.net/)

> Scope: `Darwin.WebApi`, its audience segmentation, contract rules, security model, BFF direction, and delivery expectations.

## Current Route and Provider Callback Status

Reviewed: 2026-05-09. See `docs/go-live-status.md` for implementation/gap detail.

Canonical route roots:

- `api/v1/public/*`: anonymous storefront, CMS, catalog, public provider callbacks.
- `api/v1/member/*`: authenticated customer/member operations.
- `api/v1/business/*`: business/mobile operator workflows.
- `api/v1/admin/*`: future HTTP admin/integration surface only where direct WebAdmin handler usage is not enough.

Provider callbacks:

- Stripe canonical webhook route: `api/v1/public/billing/stripe/webhooks`.
- Stripe legacy alias still present: `api/v1/billing/stripe/webhooks`.
- DHL canonical webhook route: `api/v1/public/shipping/dhl/webhooks`.
- DHL legacy alias still present: `api/v1/shipping/dhl/webhooks`.
- Brevo canonical webhook route: `api/v1/public/notifications/brevo/webhooks`.

Current production gaps:

- Stripe webhooks, reconciliation, and storefront Checkout Session creation are implemented; live-provider smoke and subscription/refund/dispute verification remain go-live tasks.
- DHL callbacks, provider-operation queueing, and Worker-side shipment/label client plumbing are implemented; live DHL account smoke and returns/RMA automation remain go-live blockers.
- Brevo callback and delivery plumbing are implemented; production readiness depends on provider account/domain/webhook setup and Worker deployment.

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
- shared persistence/security: `src/Darwin.Infrastructure`
- PostgreSQL provider: `src/Darwin.Infrastructure.PostgreSql`
- SQL Server provider: `src/Darwin.Infrastructure.SqlServer`
- persistence provider selector: `src/Darwin.Infrastructure.PersistenceProviders`
- API contracts: `src/Darwin.Contracts`

Persistence provider selection is configuration-driven through `Persistence:Provider`. PostgreSQL is the preferred local/default provider; SQL Server remains available for customer deployments that require it. See `docs/persistence-providers.md` for provider names, Docker setup, migration commands, and required verification.

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
- linked CRM customer context
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
- business access-state and approval-aware operational gating

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
- public business discovery now also includes a canonical map-discovery route under `api/v1/public/businesses/map`, while the legacy `/api/v1/businesses/map` alias remains available for existing mobile consumers
- public storefront cart and shipping estimation now have dedicated canonical route roots under `api/v1/public/cart` and `api/v1/public/shipping`, while legacy `/api/v1/cart*` and `/api/v1/shipping/rates` aliases remain available during migration
- public storefront checkout order placement now has a dedicated canonical route root under `api/v1/public/checkout/orders`, while the legacy `/api/v1/checkout/orders` alias remains available during migration
- public storefront checkout intent, payment intent, and confirmation now live under the dedicated canonical `api/v1/public/checkout/*` route group, while legacy `/api/v1/checkout/*` aliases remain available during migration
- business mobile now also has a dedicated account-access route under `api/v1/business/account/access-state` so approval/suspension state can be consumed without leaking admin DTOs into public business detail contracts

### Required public groups

These should be documented and expanded as implementation continues:

- `/api/v1/public/cms/pages`
- `/api/v1/public/cms/menus`
- `/api/v1/seo/*`
- `/api/v1/public/catalog/products`
- `/api/v1/public/catalog/categories`
- `/api/v1/public/cart/*`
- `/api/v1/public/shipping/*`
- `/api/v1/public/checkout/intent`
- `/api/v1/public/checkout/orders`
- `/api/v1/public/checkout/orders/{orderId}/payment-intent`
- `/api/v1/public/checkout/orders/{orderId}/payments/{paymentId}/complete`
- `/api/v1/public/checkout/orders/{orderId}/confirmation`

Current public delivery ownership:

- public CMS: published page listing, page-by-slug delivery, and menu delivery
- public catalog: published category listing, product listing, and product-by-slug delivery
- public business discovery: list, detail, category kinds, and viewport-based map discovery
- public storefront cart: cart summary, add/update/remove line, and coupon application for anonymous or authenticated storefront sessions
- public shipping: rate quotes for storefront checkout estimation
- public checkout intent: authoritative cart totals, derived shipment mass, and validated shipping options for the current address context
- public checkout: order placement from the authoritative cart summary with saved member addresses or inline address snapshots and automatic cart finalization
- public payment intent: pending storefront payment-session creation or reuse for already placed orders so front-office clients can hand off to a PSP without duplicating pending payment rows
- public PSP handoff: payment-intent responses now include `checkoutUrl`, `returnUrl`, and `cancelUrl`; when Stripe is enabled in Site Settings, WebApi creates a real Stripe Checkout Session and returns Stripe's hosted URL
- public payment completion: `POST /api/v1/public/checkout/orders/{orderId}/payments/{paymentId}/complete` validates the safe return context; for Stripe, it does not capture, void, or fail the payment because verified Stripe webhooks are the final payment-state authority
- public confirmation: safe post-order confirmation delivery for member-owned or anonymous orders without exposing member-owned orders to anonymous callers
- selected checkout shipping method is now persisted on the `Order` aggregate together with method-name/carrier/service snapshots so storefront confirmation, member order history, and admin views are not coupled to future shipping-method edits
- legacy `/api/v1/cms/*`, `/api/v1/catalog/*`, `/api/v1/cart*`, `/api/v1/shipping/rates`, `/api/v1/checkout/intent`, `/api/v1/checkout/orders*`, and `/api/v1/businesses/map` aliases remain only for compatibility and should not be used for new development

Current storefront checkout configuration:

- `StorefrontCheckout:FrontOfficeBaseUrl` defines where return/cancel flows land in `Darwin.Web`
- `StorefrontCheckout:StripeCheckoutBaseUrl` is retained as a development/fallback handoff URL when no provider-hosted URL is returned
- development defaults point to `http://localhost:3000` and `http://localhost:3000/mock-checkout`
- Stripe provider references are stored on `Payment.ProviderCheckoutSessionRef`, `Payment.ProviderPaymentIntentRef`, and `Payment.ProviderTransactionRef`; secrets stay in Site Settings/configuration and must not be returned to clients

### Required member groups

- `/api/v1/profile/*`
- `/api/v1/member/orders/*`
- `/api/v1/member/invoices/*`
- `/api/v1/member/orders/{id}/payment-intent`
- `/api/v1/member/orders/{id}/document`
- `/api/v1/member/invoices/{id}/payment-intent`
- `/api/v1/member/invoices/{id}/document`
- `/api/v1/member/loyalty/*`
- `/api/v1/member/profile/preferences`
- `/api/v1/member/*`

Current loyalty ownership:

- member loyalty: scan preparation, account summaries, reward browsing, timeline, promotions, and join flows
- member loyalty now also exposes a cross-business overview (`my/overview`) and a business-scoped dashboard (`business/{businessId}/dashboard`) so mobile and front-office clients can render loyalty home/detail screens without fanning out across multiple smaller endpoints
- member loyalty overview, account summaries, and the business dashboard now include additive next-reward progress fields (`nextRewardTitle`, `nextRewardRequiredPoints`, `pointsToNextReward`, `nextRewardProgressPercent`) so clients can render progress UI without re-deriving thresholds client-side
- the business dashboard also exposes `expiryTrackingEnabled`, `pointsExpiringSoon`, and `nextPointsExpiryAtUtc`; today the current loyalty domain model does not track point expiry, so the API explicitly reports expiry tracking as disabled instead of inventing synthetic expiry behavior
- business loyalty: reward configuration, scan processing, accrual/redemption confirmation, and campaign management
- legacy mixed loyalty routes remain only as compatibility aliases and should not be used for new development

Current member commerce ownership:

- member orders: paged order history and order detail under the member route root
- member invoices: paged invoice history and invoice detail under the member route root
- member order actions: additive retry-payment and plain-text document-download flows under the member route root, plus action metadata on order detail contracts so clients can discover canonical follow-up paths without hard-coding them
- member invoice actions: additive retry-payment and plain-text document-download flows for order-linked invoices, plus action metadata on invoice detail contracts and line-level projections suitable for self-service document rendering
- mobile shared now includes a dedicated member-commerce service abstraction over the canonical member order/invoice routes, including retry-payment and plain-text document downloads, so MAUI consumers can adopt the new audience-first paths without duplicating route composition logic in app code
- member profile addresses: reusable address-book CRUD and default billing/shipping selection under the member profile route root
- member CRM linkage: current identity-to-customer summary under the member profile route root
- member CRM customer context: a richer self-service projection under the member profile route root that includes current segments, consent history, and recent interaction history without exposing admin editing DTOs
- member preferences: privacy and communication preferences under the member profile route root, including aggregate marketing consent, per-channel delivery flags, and optional analytics tracking
- member loyalty overview/dashboard: additive projections intended to reduce chatty client behavior by combining account, reward, and recent-history context into fewer member-facing calls
- member retry-payment endpoints intentionally reuse the same storefront payment-intent handler and hosted-checkout URL builder as public checkout, so front-office and future mobile clients do not fork payment-session behavior
- storefront checkout is now intentionally allowed to reuse saved member addresses and then fall back to the same order aggregate for member confirmation/history rather than inventing a second order model
- member profile addresses are intentionally reusable by storefront checkout so signed-in users can place orders from saved addresses without exposing admin-facing address contracts
- mobile shared route/service catalogs now expose the canonical member-preferences, member CRM customer-context, and member loyalty overview/dashboard endpoints so future mobile UI work does not need to rediscover route ownership
- mobile shared profile services now also cover the canonical member address-book endpoints, so MAUI self-service address flows can move to the audience-first route surface without adding app-local route logic
- legacy `/api/v1/orders/*` and `/api/v1/invoices/*` aliases remain only for compatibility and should not be used for new development
- mobile shared route constants should prefer the canonical audience-first roots (`public`, `member`, `business`) even when the legacy aliases still exist server-side

Current business-mobile access-state ownership:

- `GET /api/v1/business/account/access-state` returns the authenticated business lifecycle/access snapshot used by `Darwin.Mobile.Business`
- the payload includes operational status, active/inactive state, approval/suspension timestamps, suspension reason, onboarding checklist booleans, and derived `isOperationsAllowed` / `isSetupComplete` flags
- this route is intentionally additive and business-scoped; it avoids polluting public business discovery/detail contracts with admin-only lifecycle context
- current phase-1 policy is a `Soft Gate`: business operators may sign in during `PendingApproval`, but live operational screens should block against this access-state until the business is approved and active

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
