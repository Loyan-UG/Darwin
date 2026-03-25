<img src="./src/Darwin.WebAdmin/wwwroot/images/DarwinJustLogo.png" width="64" style="vertical-align: middle;" />

# Darwin Platform - CMS + CRM + E-Commerce + Loyalty + Mobile

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-10.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js)](https://nextjs.org/)
[![Node.js](https://img.shields.io/badge/Node.js-20+-339933?logo=node.js&logoColor=white)](https://nodejs.org/)
[![Tailwind CSS](https://img.shields.io/badge/TailwindCSS-4-06B6D4?logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Visual Studio 2026](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/)
[![Build](https://img.shields.io/github/actions/workflow/status/shahramvafadar/Darwin/build.yml?branch=master&logo=githubactions&label=CI)](../../actions)

Darwin is a unified platform for CMS, commerce, loyalty, CRM, and mobile experiences. The web layer is now split into two distinct applications:

- `Darwin.WebAdmin` is the ASP.NET Core MVC/Razor back-office used by staff and operators.
- `Darwin.Web` is the Next.js/React front-office used by customers for the public storefront and member portal.

Both web experiences sit on top of the same business platform, share the same domain and application model, and are designed around `Darwin.WebApi` as the API-friendly integration surface for front-office, mobile, and future external clients.

## Documentation Map

Use this README as the entry point, then jump to the focused documents:

- **Backlog & roadmap (source of truth)**: [`BACKLOG.md`](BACKLOG.md)
- **WebApi guide (surfaces, policies, endpoint matrix, implementation rules)**: [`DarwinWebApi.md`](DarwinWebApi.md)
- **Front-end guide (Next.js app structure, SSR/SSG/ISR, auth flow)**: [`DarwinFrontEnd.md`](DarwinFrontEnd.md)
- **Mobile guide (Consumer/Business apps + Contracts)**: [`DarwinMobile.md`](DarwinMobile.md)
- **Mobile UI/UX guidelines**: [`DarwinMobile.Guidelines.md`](DarwinMobile.Guidelines.md)
- **Testing strategy & execution tracker**: [`DarwinTesting.md`](DarwinTesting.md)
- **Admin identity module how-to**: [`howto-identity-access.md`](howto-identity-access.md)
- **Contributing**: [`CONTRIBUTING.md`](CONTRIBUTING.md)

## Status Snapshot

This is a high-level summary only. For detailed task tracking and sequencing, use `BACKLOG.md`.

| Pillar | Status | Notes |
| --- | --- | --- |
| **Domain & Application** | Complete | Clean Architecture, handlers, DTOs, validators, and core business flows are in place. |
| **Infrastructure** | Complete | EF Core persistence, Identity/JWT, Data Protection, notifications, and shared hosting concerns are implemented. |
| **Web CMS & E-Commerce (Admin)** | In Progress | `Darwin.WebAdmin` delivers content, catalog, pricing, order, and settings workflows for back-office staff. |
| **Loyalty System** | Phase 3+ | Session-based accrual/redemption, rewards, campaigns, and mobile-oriented loyalty flows are implemented and evolving. |
| **REST WebApi** | Mature | `Darwin.WebApi` is the contracts-first HTTP surface for mobile, front-office, and future integrations. |
| **Mobile Apps** | Phase 3+ | Consumer and Business MAUI apps are active and continue to expand on loyalty, discovery, and operations flows. |
| **CRM Module** | Planned | Customer, consent, segmentation, interactions, and CRM-linked billing remain under active design/planning. |
| **Front-Office (`Darwin.Web`)** | In Progress | Next.js front-end for the public storefront and member portal, consuming `Darwin.WebApi`. |
| **Testing & QA** | In Progress | Unit, integration, and contract tests exist; coverage and release evidence continue to expand. |

## User Interface Segments

### Back-Office (`Darwin.WebAdmin`)

The back-office is the administrative portal built with ASP.NET Core MVC/Razor. It is used by staff to manage:

- CMS content and menus
- catalog and pricing
- orders and operational workflows
- loyalty programmes and business settings
- identity, permissions, and future CRM administration

### Front-Office (`Darwin.Web`)

The front-office is the public and authenticated customer-facing web application built with Next.js and React. It is where end-users:

- browse products and CMS pages
- search and discover businesses
- authenticate and manage their account
- view loyalty points, rewards, invoices, and support-related interactions

The member portal is part of the front-office. It is not a second admin area.

## Features

- **CMS**: pages, rich-text content, menus, SEO metadata, media-driven content workflows
- **Catalog**: brands, categories, products, variants, add-ons, attributes
- **Inventory**: stock tracking, reservations, allocation, warehouse-oriented expansion path
- **Pricing & tax**: VAT, promotions, coupons, shipping/payment rules
- **Orders**: order lifecycle, payment/shipment handling, back-office order operations
- **Identity & access**: roles, permissions, JWT, cookies, WebAuthn, TOTP
- **Loyalty**: session-based QR flows, accrual/redemption, rewards, campaigns
- **Front-office**: React/Next.js storefront and member portal with SSR/SSG/ISR-ready architecture
- **Mobile**: consumer and business MAUI applications sharing `Darwin.Contracts`
- **API & contracts**: contracts-first delivery through `Darwin.WebApi`
- **CRM direction**: customer 360, segmentation, consent, interactions, campaign enablement

## Architecture

Darwin follows a Clean Architecture with separate delivery applications over shared domain and application layers:

```text
src/
|-- Darwin.Domain           - Entities, ValueObjects, Enums, BaseEntity
|-- Darwin.Application      - Use cases, DTOs, Handlers, Validators
|-- Darwin.Infrastructure   - EF Core, DbContext, Migrations, DataSeeder
|-- Darwin.WebAdmin         - MVC + Razor for Back-Office (Admin portal)
|-- Darwin.WebApi           - Public REST API (mobile + frontend)
|-- Darwin.Web              - Next.js React app for Storefront & Member portal
|-- Darwin.Worker           - Background jobs / schedulers
|-- Darwin.Shared           - Result wrappers, constants, helpers
|-- Darwin.Contracts        - Shared DTOs for WebApi + Mobile (request/response contracts)
|-- Darwin.Mobile.Shared    - Mobile shared services (HTTP client, retry, auth, scanner/location)
|-- Darwin.Mobile.Consumer  - .NET MAUI consumer app (QR, discover, rewards, profile)
`-- Darwin.Mobile.Business  - .NET MAUI business app (scan, accrue, redeem, dashboard)
```

### Delivery Model

- `Darwin.WebAdmin` is a .NET web application and builds with the .NET solution.
- `Darwin.Web` is a Node/React application and is managed with `npm`, not `dotnet`.
- `Darwin.WebApi` is the public HTTP boundary for front-office, mobile, and integration scenarios.
- `Darwin.Contracts` remains the shared schema source for API request/response models.

### Composition Roots

- **Back-office composition root**: `src/Darwin.WebAdmin/Extensions/DependencyInjection.cs`
- **WebApi composition root**: `src/Darwin.WebApi/Extensions/DependencyInjection.cs`
- **Front-office app entry**: `src/Darwin.Web/src/app`

## API-Friendly Delivery Rules

These rules are non-negotiable and should guide all future work:

### CMS Must Be API-Friendly

Content pages, menus, SEO metadata, and structured components must be delivered through `Darwin.WebApi`. The public site must consume content via API contracts so the same content can be rendered by React and reused elsewhere.

### Separate DTOs

Do not reuse admin DTOs for the public site. Admin DTOs are operational. Public DTOs must be presentation-oriented, delivery-friendly, and optimized for front-office rendering.

### Isolated API Surfaces

`Darwin.WebApi` should expose distinct surfaces for:

- public storefront content
- authenticated member/customer operations
- business/mobile operations
- admin/integration workflows where appropriate

Avoid mixing public and admin concerns inside the same endpoint shape.

### Keep the Architecture BFF-Ready

A Backend-for-Frontend layer is not required immediately, but the architecture must not block it. Future BFF adoption may be used for:

- authentication/session handling
- API response composition
- caching and edge-friendly delivery
- reducing chatty client-to-API traffic

## Security Overview

- **Passwords**: Argon2id hashing
- **Passkeys/WebAuthn**: FIDO2 ceremonies for registration and login
- **TOTP 2FA**: RFC 6238 compatible implementation
- **Data Protection**: persistent key ring for shared hosting / multi-instance safety
- **API security**: JWT/cookie token flows, policy-based authorization, concurrency and audit concerns

Front-office authentication still depends on `Darwin.WebApi` and must follow the same token, policy, and security standards as the rest of the platform.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/)
- SQL Server (LocalDB is fine for development)
- MAUI workloads for mobile development

### Local Setup

```bash
git clone https://github.com/shahramvafadar/Darwin.git
cd Darwin
```

Configure your .NET connection string in `src/Darwin.WebAdmin/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Darwin;Trusted_Connection=True;"
  }
}
```

Run database migrations:

```bash
dotnet ef database update --project src/Darwin.Infrastructure
```

Start the back-office:

```bash
dotnet run --project src/Darwin.WebAdmin
```

Start the API:

```bash
dotnet run --project src/Darwin.WebApi
```

Initialize and run the front-office:

```bash
cd src/Darwin.Web
npm install
npm run dev
```

Open:

- Back-office: `https://localhost:7170/`
- WebApi Swagger (dev): `/swagger` on the WebApi host
- Front-office: `http://localhost:3000`

For a production front-office build:

```bash
cd src/Darwin.Web
npm run build
npm run start
```

`Darwin.Web` is managed by Node/npm and is not compiled by `dotnet build`.

## Front-Office and Back-Office Interaction

- `Darwin.WebAdmin` is optimized for operations, administration, and internal workflows.
- `Darwin.Web` is optimized for SEO, content delivery, commerce UX, and authenticated member journeys.
- `Darwin.WebApi` is the shared delivery boundary for public/member/mobile-facing data and should remain contracts-first.

## Roadmap

The roadmap source of truth is [`BACKLOG.md`](BACKLOG.md).

At a high level:

- back-office coverage continues to expand in `Darwin.WebAdmin`
- WebApi surfaces continue to mature and separate by audience
- mobile work remains active
- CRM remains a major future pillar
- front-office Next.js implementation is now underway; see `BACKLOG.md` for detailed tasks

## Documentation

- [`BACKLOG.md`](BACKLOG.md)
- [`DarwinWebApi.md`](DarwinWebApi.md)
- [`DarwinFrontEnd.md`](DarwinFrontEnd.md)
- [`DarwinMobile.md`](DarwinMobile.md)
- [`DarwinMobile.Guidelines.md`](DarwinMobile.Guidelines.md)
- [`DarwinTesting.md`](DarwinTesting.md)
- [`howto-identity-access.md`](howto-identity-access.md)
- [`CONTRIBUTING.md`](CONTRIBUTING.md)

## About

Darwin is designed to support small and medium businesses in Germany/EU with a platform that is:

- easy to host
- compliant with GDPR, VAT, and legal publishing requirements
- extensible across content, commerce, loyalty, CRM, and mobile

The long-term goal is one coherent platform with distinct delivery applications for staff, customers, and mobile users, all aligned on shared business rules and contracts.
