<img src="./src/Darwin.WebAdmin/wwwroot/images/DarwinJustLogo.png" width="64" style="vertical-align: middle;" />

# Darwin Platform

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-10.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Next.js](https://img.shields.io/badge/Next.js-14.0-black?logo=next.js)](https://nextjs.org/)
[![Node.js](https://img.shields.io/badge/Node.js-18.0.0-339933?logo=node.js&logoColor=white)](https://nodejs.org/)
[![TailwindCSS](https://img.shields.io/badge/TailwindCSS-3.0-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
[![HTMX](https://img.shields.io/badge/HTMX-2.0-3366CC?logo=htmx&logoColor=white)](https://htmx.org/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Visual Studio 2026](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/)

Darwin is a unified CMS, commerce, CRM, loyalty, inventory, billing, API, and mobile platform. The web layer is now split into two independent applications:

- `Darwin.WebAdmin`: the ASP.NET Core MVC/Razor back-office for staff and administrators, using HTMX for lightweight partial loading and form interactions.
- `Darwin.Web`: the Next.js/React front-office for the public storefront and authenticated member portal.

Both applications consume the same business model through `Darwin.Application` and expose delivery-friendly data through `Darwin.WebApi`.

## Documentation Map

Use this README as the entry point, then continue with the focused documents:

- [`BACKLOG.md`](BACKLOG.md): roadmap, epics, completed work, and current execution order
- [`DarwinDomainDesign.md`](DarwinDomainDesign.md): target domain model, module boundaries, and aggregate rules
- [`DarwinWebApi.md`](DarwinWebApi.md): WebApi surfaces, contracts, REST design, BFF direction, and integration rules
- [`DarwinWebAdmin.md`](DarwinWebAdmin.md): back-office architecture, HTMX conventions, and MVC/Razor workflow rules
- [`DarwinFrontEnd.md`](DarwinFrontEnd.md): Next.js front-office structure, data fetching, and member portal guidance
- [`DarwinMobile.md`](DarwinMobile.md): mobile integration and client responsibilities
- [`DarwinMobile.Guidelines.md`](DarwinMobile.Guidelines.md): mobile UI/UX rules
- [`DarwinTesting.md`](DarwinTesting.md): testing strategy and execution notes
- [`howto-identity-access.md`](howto-identity-access.md): admin identity, roles, permissions, and HTMX-backed address workflows
- [`CONTRIBUTING.md`](CONTRIBUTING.md): coding standards and contribution rules

## Status Snapshot

| Pillar | Status | Notes |
| --- | --- | --- |
| Domain & Application | Active | CRM, inventory, billing, and warehouse-aware fulfillment are now modeled and implemented in the core layers. |
| Infrastructure | Active | EF Core persistence, migrations, seeding, security, and new module mappings are in place. |
| Web CMS & E-Commerce (Admin) | In Progress | `Darwin.WebAdmin` is the operational portal built with MVC/Razor and HTMX. |
| Loyalty | Mature | Loyalty is already end-to-end and remains the only source of truth for loyalty balances and ledgers. |
| REST WebApi | Active | `Darwin.WebApi` is the shared HTTP delivery layer for front-office, mobile, and future external clients. |
| Mobile Apps | Active | MAUI consumer and business apps continue to build on the shared contracts and loyalty stack. |
| CRM Module | In Progress | Lead, Opportunity, Interaction, Consent, and CRM-linked billing are now in the domain and application stack. |
| Front-Office (`Darwin.Web`) | In Progress | Next.js front-end for the public storefront and member portal, consuming `Darwin.WebApi`. |
| Testing & QA | Active | Unit, infrastructure, WebApi, contract, and integration coverage exist and continue to expand. |

## Architecture

```text
src/
├── Darwin.Domain         – Entities, value objects, enums, base entity
├── Darwin.Application    – Use cases, handlers, DTOs, validators
├── Darwin.Infrastructure – EF Core, DbContext, migrations, seed pipeline
├── Darwin.WebAdmin       – MVC + Razor + HTMX back-office
├── Darwin.WebApi         – REST API for public, member, business, and admin delivery
├── Darwin.Web            – Next.js React front-office (SSR/SSG/ISR)
├── Darwin.Worker         – Background jobs and schedulers
├── Darwin.Shared         – Result wrappers, constants, helpers
├── Darwin.Contracts      – Shared request/response contracts for WebApi and mobile
├── Darwin.Mobile.Shared  – Shared mobile services
├── Darwin.Mobile.Consumer – Consumer MAUI app
└── Darwin.Mobile.Business – Business MAUI app
```

Important delivery rules:

- `Darwin.WebAdmin` builds with the .NET solution.
- `Darwin.Web` is managed by Node/npm and does not build via `dotnet build`.
- `Darwin.WebApi` is the API-friendly delivery boundary for front-office, mobile, and future external systems.

## User Interface Segments

### Back-Office (`Darwin.WebAdmin`)

The back-office is the internal administrative portal used for:

- CMS and menu management
- catalog and pricing management
- orders, payments, shipments, and warehouse-aware fulfillment
- CRM operations such as customers, leads, opportunities, interactions, and consent
- inventory, supplier, and purchasing workflows
- billing, subscription, and future accounting administration
- identity, permissions, and site settings

The implementation model is ASP.NET Core MVC/Razor with HTMX for partial rendering and server-driven interactions.

### Front-Office (`Darwin.Web`)

The front-office is the public and authenticated customer experience used for:

- public storefront browsing
- CMS page rendering
- product listing and product detail pages
- customer account, loyalty, orders, invoices, and support-related views

The member portal is part of the front-office. It is not an admin area and it must not depend on back-office DTOs or Razor models.

## CRM and Loyalty Boundary

The CRM model no longer stores loyalty balances directly.

- `Customer.LoyaltyPointsTotal` has been removed.
- `LoyaltyPointEntry` has been removed.
- Loyalty balances are managed only through `LoyaltyAccount` and `LoyaltyPointsTransaction`.

Whenever a total point balance is needed for a customer, it must be calculated from loyalty transactions or projected by a query/view model. Any code that previously depended on `Customer.LoyaltyPointsTotal` must now aggregate `LoyaltyPointsTransaction` records instead.

## HTMX in WebAdmin

HTMX is now a first-class part of `Darwin.WebAdmin`.

Current usage direction:

- use `hx-get` to load partial views without custom `fetch` code
- use `hx-post` to submit forms and replace only the affected section
- keep alerts and partial grids server-rendered
- use small Bootstrap-focused JavaScript only where modal orchestration or UI state wiring is still needed

Representative examples:

- Orders details uses `hx-get` to load the payments and shipments partials.
- Address create/edit in the Users screen uses `hx-post` to submit the modal form and refresh only the addresses section.
- Default billing/shipping address buttons now use `hx-post` to update the address section without a full page reload.

HTMX is the preferred replacement for scattered `fetch`-driven partial refreshes. Alpine.js is not part of the current stack; it may be introduced later only if richer client-side state becomes necessary.

## API-Friendly Delivery Rules

These rules are non-negotiable:

### CMS Must Be API-Friendly

CMS pages, menus, SEO metadata, and structured content blocks must be delivered through `Darwin.WebApi`. Razor views in the public site must not be the only rendering path for CMS content.

### Separate DTOs

Do not reuse back-office DTOs in the public site. Admin DTOs are operational; public DTOs must be presentation-oriented and stable for front-office delivery.

### Isolated API Surfaces

`Darwin.WebApi` must expose separate logical surfaces for:

- public storefront content
- member/account operations
- business/mobile operations
- admin/integration scenarios

### Keep the Architecture BFF-Ready

The current architecture must remain ready for a future Backend-for-Frontend layer responsible for:

- authentication/session orchestration
- response composition
- caching
- reduction of chatty client-to-API flows

## Security Overview

- passwords are hashed with Argon2id
- TOTP and WebAuthn are supported
- Data Protection is configured for shared hosting scenarios
- WebApi authentication uses JWT and/or cookie-based delivery patterns depending on the client
- optimistic concurrency is enforced with `RowVersion`

The same security practices apply to back-office, front-office, mobile, and future consumers.

## Engineering Standards

The repository expects these standards:

- nullable reference types must remain enabled
- non-nullable strings must be initialized, typically with `string.Empty`
- nullable values must be marked with `?`
- new classes and public members should include complete English XML documentation
- code comments must be English-only
- TODO markers are reserved only for genuinely future-phase work
- avoid technical debt by modeling the final architectural boundary early rather than layering aliases and temporary duplicates

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 18.0+](https://nodejs.org/)
- SQL Server
- MAUI workloads for mobile work

### Local Setup

Clone the repository:

```bash
git clone https://github.com/shahramvafadar/Darwin.git
cd Darwin
```

Configure the .NET connection string in `src/Darwin.WebAdmin/appsettings.Development.json` and/or `src/Darwin.WebApi/appsettings.Development.json`.

Apply migrations:

```bash
dotnet ef database update --project src/Darwin.Infrastructure --startup-project src/Darwin.WebAdmin
```

Run the back-office:

```bash
dotnet run --project src/Darwin.WebAdmin
```

Run the API:

```bash
dotnet run --project src/Darwin.WebApi
```

Run the front-office:

```bash
cd src/Darwin.Web
npm install
npm run dev
```

Production front-office build:

```bash
cd src/Darwin.Web
npm run build
npm run start
```

## Roadmap

The roadmap source of truth is [`BACKLOG.md`](BACKLOG.md).

Current direction:

- complete the HTMX-driven rewrite of existing WebAdmin modules
- continue surfacing the new CRM, inventory, and billing modules in WebAdmin
- expand `Darwin.WebApi` with clear public/member/admin surfaces
- continue the Next.js front-office implementation
- keep mobile aligned with contract and loyalty changes

Front-office Next.js implementation is now underway; see `BACKLOG.md` for detailed tasks.
