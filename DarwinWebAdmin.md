# Darwin WebAdmin Guide

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![HTMX](https://img.shields.io/badge/HTMX-2.0-3366CC?logo=htmx&logoColor=white)](https://htmx.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)

> Scope: `src/Darwin.WebAdmin`, the internal back-office used by staff and administrators.

## Purpose

`Darwin.WebAdmin` is the operational portal for managing the Darwin platform. It is responsible for internal workflows, not customer-facing delivery.

Primary module areas:

- CMS and content operations
- catalog and pricing management
- orders, payments, shipments, and warehouse-aware fulfillment
- CRM administration
- inventory, procurement, and supplier operations
- subscriptions, billing, and future accounting workflows
- identity, roles, permissions, and site settings

## Technology Stack

- ASP.NET Core MVC
- Razor views
- HTMX for server-driven partial updates
- Bootstrap for layout and components
- small JavaScript helpers only where modal orchestration or UI-only state is still required

Alpine.js is not part of the current stack. If richer client-side state becomes necessary later, it should be introduced intentionally, not as a default dependency.

## HTMX Conventions

HTMX is the preferred pattern for partial loading and form submissions.

### When to Use `hx-get`

Use `hx-get` for read-only partial rendering when only a subsection of the page needs to be refreshed.

Current examples:

- order payments partial on the order details page
- order shipments partial on the order details page
- future dashboards, list filters, and tabbed content

Example:

```html
<div id="payments-grid"
     hx-get="/Orders/Payments?orderId=..."
     hx-trigger="load"
     hx-swap="innerHTML">
</div>
```

### When to Use `hx-post`

Use `hx-post` for commands that update one section of the page and should return a refreshed partial.

Current examples:

- create/edit address modal in the Users screen
- set default billing/shipping address buttons

Example:

```html
<form hx-post="/Users/CreateAddress"
      hx-target="#addresses-section"
      hx-swap="innerHTML">
    ...
</form>
```

### Alerts and Partial Refreshes

Keep alerts server-rendered through `_Alerts.cshtml`. After a successful HTMX command, refresh the alerts container with another server-rendered partial rather than building alert HTML in JavaScript.

### Anti-Forgery

HTMX requests in WebAdmin must carry the ASP.NET Core anti-forgery token. The layout registers an `htmx:configRequest` handler that reads the current `__RequestVerificationToken` and sends it as the `RequestVerificationToken` header.

### Bootstrap Interop

After HTMX swaps content, Bootstrap tooltips/popovers must be re-initialized. The shared layout handles this by listening to `htmx:afterSwap` and re-running the Bootstrap initializer on the swapped fragment.

## Controller and View Responsibilities

### Controllers

Controllers should:

- call application handlers only
- map DTOs to view models
- return full views for page loads
- return partial views for HTMX fragment requests
- keep server responses authoritative for alerts, grid rows, and validation messages

Controllers should not:

- manipulate EF entities directly
- duplicate application-layer validation
- push complex UI state into JavaScript

### Views

Views should:

- remain thin and server-rendered
- prefer HTMX attributes over custom `fetch` calls
- use Bootstrap components and shared partials consistently
- preserve optimistic concurrency fields such as `RowVersion`

## Current HTMX-backed Areas

### Orders

The order details page now uses HTMX to load payments and shipments partials. Pagination inside those partials is also HTMX-boosted so the tab content can refresh without a full page navigation.

### Identity Addresses

The user edit page now uses HTMX for:

- create address
- edit address
- set default billing address
- set default shipping address

Delete still uses the existing modal flow and can be moved fully to HTMX in a later cleanup pass.

## Architectural Rules

- WebAdmin DTOs are operational and must never be reused as public storefront DTOs.
- Public content and front-office delivery must go through `Darwin.WebApi`, not direct reuse of MVC views.
- Loyalty balances must not be read from CRM. Any UI that needs a points total must consume a loyalty projection derived from `LoyaltyAccount` and `LoyaltyPointsTransaction`.
- New admin screens should be added on top of the existing Application handlers and validators, not by bypassing them.

## Coding Standards for WebAdmin

- keep nullable reference types enabled
- initialize non-nullable references
- write English XML documentation for public classes and members
- keep code comments in English
- reserve TODOs for genuinely later-phase work
- prefer small, intentional HTMX fragments over client-heavy page scripts

## Near-Term Rewrite Priorities

The current rewrite direction is:

1. continue replacing scattered `fetch` partial refreshes with HTMX
2. complete the rewrite of the existing admin modules against the new domain
3. surface CRM, inventory, billing, and accounting modules that already exist in the domain and application layers
4. standardize list/filter/modal patterns across all admin modules
