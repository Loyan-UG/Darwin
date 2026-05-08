<img src="./src/Darwin.WebAdmin/wwwroot/images/DarwinJustLogo.png" width="64" style="vertical-align: middle;" />

# Darwin Platform

Current go-live implementation status is summarized in `docs/go-live-status.md`. Treat it as the concise source for provider readiness, dashboard scope, and remaining production blockers.

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-10.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![React](https://img.shields.io/badge/React-19.2.4-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Next.js](https://img.shields.io/badge/Next.js-16.2.1-black?logo=next.js)](https://nextjs.org/)
[![Node.js](https://img.shields.io/badge/Node.js-18.0.0-339933?logo=node.js&logoColor=white)](https://nodejs.org/)
[![TailwindCSS](https://img.shields.io/badge/TailwindCSS-4-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
[![HTMX](https://img.shields.io/badge/HTMX-2.0-3366CC?logo=htmx&logoColor=white)](https://htmx.org/)
[![Stripe](https://img.shields.io/badge/Stripe-Phase--1-635BFF?logo=stripe&logoColor=white)](https://stripe.com/)
[![DHL](https://img.shields.io/badge/DHL-Phase--1-FFCC00?logoColor=black)](https://www.dhl.de/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Visual Studio 2026](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/)

### Darwin is a multi-tenant-ready commerce and operations platform for small and medium businesses. It combines CMS, catalog, CRM, loyalty, inventory, billing, mobile, and API delivery under one solution.

The web layer is intentionally split into two separate applications:

- `Darwin.WebAdmin`: the ASP.NET Core MVC/Razor back-office for staff and operators, using HTMX for server-driven interactions
- `Darwin.Web`: the Next.js/React front-office for the clean public storefront and authenticated member portal

Both web applications consume the same core business model through `Darwin.Application` and expose or consume delivery-ready contracts through `Darwin.WebApi`.

## Documentation Map

Use this README as the product and delivery overview, then continue with the focused documents:

- [`BACKLOG.md`](BACKLOG.md): execution priorities, go-live critical work, near-term items, future-phase items, and decision log
- [`DarwinDomainDesign.md`](DarwinDomainDesign.md): current domain model, target refinements, lifecycle rules, and platform-level capabilities
- [`DarwinWebAdmin.md`](DarwinWebAdmin.md): back-office operating model, HTMX/MVC conventions, onboarding workflows, settings architecture, and admin UX priorities
- [`docs/webadmin-subscription-workspace-handoff.md`](docs/webadmin-subscription-workspace-handoff.md): durable continuation context for the current `Subscription.cshtml` cleanup lane in WebAdmin
- [`DarwinFrontEnd.md`](DarwinFrontEnd.md): Next.js storefront and member portal boundaries, delivery assumptions, and front-office constraints
- [`DarwinMobile.md`](DarwinMobile.md): MAUI app responsibilities, onboarding/account lifecycle dependencies, and mobile/backend coupling
- [`DarwinWebApi.md`](DarwinWebApi.md): HTTP surfaces, contract boundaries, audience split, and integration direction
- [`DarwinTesting.md`](DarwinTesting.md): testing strategy and validation expectations
- [`docs/persistence-providers.md`](docs/persistence-providers.md): PostgreSQL/SQL Server provider architecture, local Docker setup, migration commands, and provider verification rules
- [`howto-identity-access.md`](howto-identity-access.md): identity, roles, permissions, and admin identity operations
- [`CONTRIBUTING.md`](CONTRIBUTING.md): engineering rules and contribution standards

## Current Execution Focus

The current execution focus is not evenly distributed across the whole platform.

The near-term delivery order is:

1. complete `Darwin.WebAdmin` as the operational control center
2. make business onboarding, authentication support, setup, and operator workflows usable for real SMEs
3. ensure backend and admin workflows support `Darwin.Mobile.Business` as an early operational entry point
4. keep the mobile-used `Darwin.WebApi` flows stable
5. return to broader front-office and WebApi expansion once the back-office is operationally complete

Practical meaning:

- `Darwin.Mobile.Business` is already usable enough to matter operationally
- early go-live pressure sits on back-office setup and support flows, not on polishing the public storefront first
- onboarding a new business, linking owners/admins, account activation, password recovery, and operational visibility are high-priority platform concerns

## Phase-1 Provider Strategy

### Payments

- Phase 1 payment provider: `Stripe`
- Additional payment providers and market-specific payment methods remain later-phase work
- The architecture must stay extensible for additional providers, but the active implementation scope is intentionally Stripe-first

### Shipping

- Phase 1 shipping/logistics provider: `DHL`
- Additional shipping carriers and postal providers remain later-phase work
- The shipping model should stay generic, but implementation scope and operator workflow design are intentionally DHL-first

## Status Snapshot

| Pillar | Status | Notes |
| --- | --- | --- |
| Domain & Application | Completed foundation / In Progress refinement | Core CRM, loyalty, inventory, billing, and warehouse-aware fulfillment exist; payment, shipping, onboarding, communication, tax, and settings domains still need deeper refinement. |
| Infrastructure | Active | Shared EF Core model, provider-specific PostgreSQL/SQL Server registration, migrations, seed pipeline, security wiring, and module mappings are in place. PostgreSQL is the preferred local/default provider. |
| WebAdmin | In Progress and highest priority | MVC/Razor + HTMX back-office is the current delivery focus and is being completed as the first operational control center. |
| WebApi | Active | Mobile-used endpoints must remain stable; broader public/member/admin surface separation continues after critical admin/backend work. |
| Front-Office (`Darwin.Web`) | In Progress but not current priority | Next.js storefront/member portal direction is defined, but execution is intentionally secondary to WebAdmin completion. |
| Mobile Consumer | Active | Member profile, addresses, commerce, and loyalty flows are usable and must stay aligned with canonical contracts. |
| Mobile Business | Usable and strategically important | Early business-side operational usage is expected to start here, which raises the priority of onboarding, setup, auth, and admin support workflows. |
| Communication Core | Planned / Near-term | Email-first operational communication is required for signup, activation, invitation, password recovery, and support notifications. |
| Stripe Integration | Planned / Near-term | Generic payment handoff exists, but real provider-specific lifecycle, webhook verification, and reconciliation are still pending. |
| DHL Integration | Planned / Near-term | Generic shipping and shipment visibility exist, but provider-specific label/tracking/exception/return flows are still pending. |
| Localization | Partial | Mobile is already bilingual; WebAdmin must become localization-ready and then move into multilingual enablement immediately after core completion. |

## Architecture

```text
src/
|-- Darwin.Domain          - Entities, value objects, enums, and aggregate rules
|-- Darwin.Application     - Use cases, handlers, DTOs, validators, and orchestration
|-- Darwin.Infrastructure  - Shared EF Core model, DbContext, seed pipeline, and infrastructure services
|-- Darwin.Infrastructure.PostgreSql - PostgreSQL provider registration and PostgreSQL migrations
|-- Darwin.Infrastructure.SqlServer  - SQL Server provider registration and SQL Server migrations
|-- Darwin.Infrastructure.PersistenceProviders - Neutral runtime selector for PostgreSQL/SQL Server
|-- Darwin.WebAdmin        - MVC + Razor + HTMX back-office
|-- Darwin.WebApi          - REST API for public, member, business, admin, and future integrations
|-- Darwin.Web             - Next.js React storefront and member portal (SSR/SSG/ISR)
|-- Darwin.Worker          - Background jobs and schedulers
|-- Darwin.Shared          - Result wrappers, constants, helpers
|-- Darwin.Contracts       - Shared API contracts for WebApi and mobile
|-- Darwin.Mobile.Shared   - Shared mobile services, API clients, and route abstractions
|-- Darwin.Mobile.Consumer - Consumer-facing MAUI app
`-- Darwin.Mobile.Business - Business-facing MAUI app
```

Important delivery rules:

- `Darwin.WebAdmin` builds with the .NET solution and is the current operational priority
- `Darwin.Web` is managed by Node/npm and does not build through `dotnet build`
- `Darwin.Web` must stay customer-facing; diagnostics, review workflows, and operational readiness concepts belong in server logs, tests, or `Darwin.WebAdmin`, not in public UI
- `Darwin.WebApi` is the delivery boundary for storefront, member, mobile, and future external consumers
- `Darwin.Contracts` is the contract boundary; admin DTOs must not leak into public/member delivery

## Persistence Providers

PostgreSQL is the preferred local/default database provider. SQL Server remains supported for customer deployments that require it.

Provider selection is configuration-driven:

```json
{
  "Persistence": {
    "Provider": "PostgreSql"
  }
}
```

Use `PostgreSql`/`Postgres`/`Npgsql` for PostgreSQL and `SqlServer`/`MSSQL` for SQL Server. The shared EF model and seed pipeline live in `Darwin.Infrastructure`; provider-specific registration lives in `Darwin.Infrastructure.PostgreSql` and `Darwin.Infrastructure.SqlServer`; neutral runtime selection lives in `Darwin.Infrastructure.PersistenceProviders`.

PostgreSQL uses provider-native `pg_trgm`, `citext`, selected `jsonb`+GIN support, and normalized Npgsql connection defaults so search surfaces, stable identifiers, operational/configuration JSON documents, and runtime connections behave correctly without depending on SQL Server collation or text-storage behavior.

See [`docs/persistence-providers.md`](docs/persistence-providers.md) for Docker setup, migration commands, and provider verification rules.

## User Interface Segments

### Back-Office (`Darwin.WebAdmin`)

`Darwin.WebAdmin` is the operational control center for:

- business and tenant setup
- operator and staff support
- users, roles, permissions, and business ownership management
- CMS and catalog administration
- orders, payments, refunds, invoices, and fulfillment visibility
- CRM operations
- inventory and procurement operations
- settings, configuration, and future integrations

### Front-Office (`Darwin.Web`)

`Darwin.Web` is the customer-facing experience for:

- public CMS pages
- product and catalog discovery
- checkout and payment flows
- authenticated member profile, loyalty, orders, invoices, and support views

The member portal belongs to the front-office. It is not a second admin area.

## Cross-Cutting Platform Capabilities

Darwin now explicitly treats the following as platform-wide capabilities rather than isolated feature details:

- Communication Core
- Tax and VAT readiness
- Authentication and onboarding flows
- Site and system settings
- Localization and multilingual readiness
- Security and performance
- Auditability and observability

These concerns must be modeled consistently across Domain, Application, WebAdmin, WebApi, and client applications.

## CRM and Loyalty Boundary

CRM no longer duplicates loyalty state.

- `Customer.LoyaltyPointsTotal` has been removed
- `LoyaltyPointEntry` has been removed
- loyalty balances and ledgers belong only to `LoyaltyAccount` and `LoyaltyPointsTransaction`

Whenever a customer loyalty total is needed, it must be projected from loyalty transactions and accounts. Query handlers and view models must aggregate loyalty data instead of relying on CRM-owned balance fields.

## WebAdmin Technology Direction

`Darwin.WebAdmin` uses:

- ASP.NET Core MVC
- Razor views
- HTMX for partial loading and form submission
- Bootstrap for layout and interaction primitives

HTMX is the default way to reduce scattered `fetch` calls and full-page postbacks. Alpine.js is not part of the current stack.

## Operational Priorities

Near-term operational priorities are:

- complete business onboarding and setup flows in WebAdmin
- support business-user account lifecycle: signup, invitation, activation, forgot-password, reset-password, lock/unlock, and role assignment
- build minimum viable Communication Core for email-based operational flows
- replace generic payment placeholders with Stripe-specific integration
- replace generic shipping placeholders with DHL-specific integration
- improve admin visibility for payments, shipments, communications, settings, and support operations

## Security and Performance

Security and performance are cross-cutting platform concerns, not isolated implementation details.

Security concerns include:

- authentication hardening
- authorization and permission-aware UI
- tenant isolation
- secure token and activation flows
- PII protection
- audit logging
- secret/configuration handling
- secure admin operations
- secure webhook processing
- rate limiting and abuse protection

Performance concerns include:

- pagination, filtering, and search in admin
- async processing for notifications and provider callbacks
- scalable settings retrieval
- efficient projections for dashboards and operator lists
- retry-safe external integration patterns
- background jobs and operational diagnostics

## Localization and Multilingual Readiness

Current state:

- mobile apps already support bilingual operation
- adding a new mobile resource language is comparatively straightforward
- WebAdmin is not yet fully multilingual

Required direction:

- WebAdmin must remain localization-friendly during current development
- hard-coded labels should be reduced over time
- language/default-locale settings must exist in the future settings architecture
- template/content/settings translation readiness must be considered before the multilingual rollout starts

## Engineering Standards

- nullable reference types remain enabled
- non-nullable strings should be initialized, typically with `string.Empty`
- nullable values must be marked with `?`
- new public classes and members should include English XML documentation
- code comments must remain English-only
- TODO markers are reserved for later-phase work only
- avoid reintroducing duplicate ledgers, alias fields, or temporary parallel models

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 18.0+](https://nodejs.org/)
- Docker Desktop for local PostgreSQL development
- SQL Server only when validating SQL Server customer deployments or the SQL Server migration lane
- MAUI workloads for mobile work

### Local Setup

Clone the repository:

```bash
git clone https://github.com/shahramvafadar/Darwin.git
cd Darwin
```

Start the preferred local PostgreSQL services:

```bash
docker compose -f docker-compose.postgres.yml up -d
```

Configure the .NET provider and connection string in `src/Darwin.WebAdmin/appsettings.Development.json`, `src/Darwin.WebApi/appsettings.Development.json`, and/or `src/Darwin.Worker/appsettings.Development.json`.

Apply PostgreSQL migrations:

```bash
dotnet ef database update --project src/Darwin.Infrastructure.PostgreSql --startup-project src/Darwin.WebAdmin --context Darwin.Infrastructure.Persistence.Db.DarwinDbContext
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

Immediate direction:

1. complete `Darwin.WebAdmin` for real business operations
2. close onboarding, authentication-support, and settings gaps needed by SMEs
3. deliver minimum viable Communication Core for email-driven account lifecycle flows
4. implement real Stripe and DHL integrations
5. return to wider WebApi and front-office expansion with a stable operational backend
