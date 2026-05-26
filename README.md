<img src="./src/Darwin.WebAdmin/wwwroot/images/DarwinJustLogo.png" width="72" alt="Darwin logo" />

# Darwin Platform

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-default-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-supported-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![Docker](https://img.shields.io/badge/Docker-local%20services-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![React](https://img.shields.io/badge/React-19.2-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![Next.js](https://img.shields.io/badge/Next.js-16.2-black?logo=next.js)](https://nextjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![HTMX](https://img.shields.io/badge/HTMX-2.0-3366CC?logo=htmx&logoColor=white)](https://htmx.org/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Android](https://img.shields.io/badge/Android-mobile-3DDC84?logo=android&logoColor=black)](https://developer.android.com/)
[![iOS](https://img.shields.io/badge/iOS-mobile-000000?logo=apple&logoColor=white)](https://developer.apple.com/ios/)
[![MacCatalyst](https://img.shields.io/badge/MacCatalyst-mobile-000000?logo=apple&logoColor=white)](https://developer.apple.com/mac-catalyst/)
[![Stripe](https://img.shields.io/badge/Stripe-payments-635BFF?logo=stripe&logoColor=white)](https://stripe.com/)
[![DHL](https://img.shields.io/badge/DHL-shipping-FFCC00?logo=dhl&logoColor=black)](https://developer.dhl.com/)
[![Brevo](https://img.shields.io/badge/Brevo-email-0B996E)](https://www.brevo.com/)
[![MinIO/S3](https://img.shields.io/badge/MinIO%20%2F%20S3-object%20storage-C72E49?logo=minio&logoColor=white)](https://min.io/)
[![Azure Blob](https://img.shields.io/badge/Azure%20Blob-storage-0078D4?logo=microsoftazure&logoColor=white)](https://azure.microsoft.com/products/storage/blobs/)
[![xUnit](https://img.shields.io/badge/xUnit-tests-512BD4)](https://xunit.net/)
[![Serilog](https://img.shields.io/badge/Serilog-logging-2D3748)](https://serilog.net/)

Darwin is a multi-tenant-ready commerce and operations platform for small and medium businesses. It brings storefront commerce, CRM, loyalty, inventory, procurement, billing, subscriptions, provider integrations, back-office operations, public/member web experiences, REST APIs, background workers, and mobile apps into one modular product system.

The platform is built for operators, business owners, support teams, and members/customers who need a single operational backbone instead of disconnected tools. It is designed for secure configuration, provider-aware deployment, audit-friendly workflows, and web/mobile delivery from shared contracts.

Darwin is a product platform, not a one-off deployment. Deployment-specific domains, tenant names, credentials, smoke endpoints, provider keys, signing material, and operational secrets must stay in secure configuration or private runbooks outside source control.

Start with the [documentation map](docs/README.md) for the authoritative guide to every topic.

## What Darwin Provides

| Capability | What it covers |
| --- | --- |
| Storefront and member portal | Public catalog, customer-facing content, member account surfaces, loyalty entry points, and future commerce checkout expansion. |
| CRM and customer lifecycle | Customer records, business relationships, review/engagement surfaces, support handoffs, communication history, and account lifecycle operations. |
| Business onboarding | Business creation, owner/member invitation lifecycle, approval/suspension/reactivation, setup prerequisites, support queues, and readiness workspaces. |
| Loyalty operations | Loyalty accounts, rewards, campaigns, member history, QR/session preparation, business-side scanning, accrual, redemption, and operator review. |
| Inventory and procurement | Suppliers, purchase orders, receiving, stock ledgers, warehouses, stock transfers, reserved-stock release, returns coordination, and operator flows. |
| Billing and accounting workflows | Subscriptions, plans, invoices, payment/refund coordination, failed-payment handling, invoice archive, and e-invoice readiness groundwork. |
| Communication operations | Transactional email routing, channel dispatch, provider callback ingestion, failed-send audits, retries, and support remediation surfaces. |
| Provider integrations | Stripe for payments, DHL for shipping, Brevo for transactional email, VIES for VAT validation, and modular object storage. |
| Object storage and archive | Reusable object-storage abstraction with database/internal fallback, filesystem fallback, S3-compatible storage, MinIO, AWS S3, and Azure Blob support. |
| Delivery applications | WebAdmin, WebApi, Worker, Next.js Web front-office, Consumer mobile app, Business mobile app, and shared mobile services/contracts. |

## Business Value

- One operational control center for onboarding, support, CRM, billing, inventory, communication, and provider readiness.
- Fewer disconnected tools for SMEs: customer, business, subscription, loyalty, shipment, invoice, and support context stay connected.
- Shared WebApi and contract model for web, mobile, workers, and future integrations.
- Provider-ready architecture with smoke scripts, source-contract guards, and runbooks for deployment validation.
- Compliance-aware design for VAT, invoice archive, object retention, payment finalization, and e-invoice readiness without overstating legal completion.
- Modular deployment path: PostgreSQL-first persistence, SQL Server support, provider-specific infrastructure boundaries, and secure configuration discipline.

## Product Surface

Darwin is split into focused delivery surfaces:

| Surface | Purpose |
| --- | --- |
| `Darwin.WebAdmin` | Operational back-office for onboarding, support, CRM, billing, inventory, communication, providers, reporting, and readiness work. |
| `Darwin.WebApi` | Public, member, business, admin, and provider-callback API boundary. |
| `Darwin.Web` | Next.js front-office and member-facing web experience. |
| `Darwin.Worker` | Background processing for provider callbacks, email/channel dispatch, shipment operations, archive maintenance, and retry work. |
| `Darwin.Mobile.Consumer` | Consumer/member MAUI app for account, discovery, loyalty, orders, invoices, archive downloads, QR, legal, and notification flows. |
| `Darwin.Mobile.Business` | Business/operator MAUI app for access-state-aware operations, dashboard, scanning, session processing, loyalty work, settings, and subscription status. |
| `Darwin.Mobile.Shared` | Shared mobile API client, canonical routes, secure token storage, cache, resilience, service logic, and platform guards. |

## Architecture

```text
src/
|-- Darwin.Domain          - Entities, value objects, enums, aggregate rules, and domain contracts
|-- Darwin.Application     - Use cases, handlers, DTOs, validators, abstractions, and orchestration
|-- Darwin.Infrastructure  - Shared EF Core model, DbContext, seed pipeline, providers, storage, notifications, and integrations
|-- Darwin.Infrastructure.PostgreSql - PostgreSQL provider registration and migrations
|-- Darwin.Infrastructure.SqlServer  - SQL Server provider registration and migrations
|-- Darwin.Infrastructure.PersistenceProviders - Runtime persistence provider selection
|-- Darwin.WebAdmin        - ASP.NET Core MVC/Razor + HTMX back-office
|-- Darwin.WebApi          - REST API for public, member, business, admin, and provider callbacks
|-- Darwin.Web             - Next.js React storefront and member portal
|-- Darwin.Worker          - Background jobs, schedulers, retries, provider operations, and callback processing
|-- Darwin.Shared          - Shared results, constants, helpers, and cross-cutting primitives
|-- Darwin.Contracts       - Shared API contracts for WebApi, web, and mobile clients
|-- Darwin.Mobile.Shared   - Shared mobile services, route catalog, API client, cache, storage, and resilience
|-- Darwin.Mobile.Consumer - Consumer-facing MAUI app
`-- Darwin.Mobile.Business - Business-facing MAUI app
```

Core architecture rules:

- Domain and Application remain provider-agnostic.
- Provider SDK references belong in Infrastructure or provider-specific infrastructure.
- `Darwin.Contracts` owns shared API contracts; admin DTOs must not leak into public/member/mobile contracts.
- API audiences stay separated through canonical roots: `api/v1/public/*`, `api/v1/member/*`, `api/v1/business/*`, and `api/v1/admin/*`.
- Payment and subscription completion must remain verified-webhook-authoritative; browser return routes do not finalize payment.
- `Darwin.WebAdmin` is the operational control center and current delivery priority.
- `Darwin.Web` remains customer-facing and must not show internal diagnostics, readiness dashboards, review queues, or back-office wording.
- `Darwin.Worker` owns background provider work, retry loops, archive maintenance, dispatch operations, and callback processing.
- Provider secrets, connection strings, API keys, webhook secrets, access keys, signing profiles, and certificates must not be committed or logged.

## Platform Stack

| Layer | Technology |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, EF Core 10, C# 14, FluentValidation, AutoMapper, HtmlSanitizer, Serilog. |
| Persistence | PostgreSQL as preferred/default provider; SQL Server as supported alternative; runtime provider selection. |
| WebAdmin | ASP.NET Core MVC/Razor, HTMX, localized resources, source-contract guards, hosted smoke tests. |
| Web front-office | Next.js 16, React 19, TypeScript 6, Tailwind CSS, server-only boundaries, public/member route separation. |
| Mobile | .NET MAUI 10, CommunityToolkit, Syncfusion/UraniumUI, ZXing, QRCoder, Firebase/Maps integration points, Android/iOS/MacCatalyst targets. |
| Background processing | .NET Worker services for provider callbacks, email/channel dispatch, shipment operations, invoice archive maintenance, and retries. |
| Providers | Stripe, DHL, Brevo, VIES, MinIO/S3-compatible storage, AWS S3, Azure Blob Storage, database/internal archive fallback. |
| Testing and operations | xUnit, hosted tests, source-contract tests, security guards, smoke scripts, Docker Desktop local services, optional MinIO smoke. |

## Provider Strategy

- Persistence: PostgreSQL is the preferred/default provider. SQL Server remains supported. See [docs/persistence-providers.md](docs/persistence-providers.md).
- Payments: Stripe is the phase-one payment provider. Payment and subscription finalization are verified-webhook-authoritative. See [DarwinWebApi.md](DarwinWebApi.md).
- Shipping: DHL is the phase-one shipping provider. Live account validation remains a deployment blocker; fake labels, references, and tracking URLs are not acceptable.
- Email: Brevo is the transactional email provider path with SMTP fallback. Sender roles, secrets, and webhook settings belong in secure runtime configuration.
- VAT validation: VIES integration must preserve provider failure as `Unknown` or manual review, not false valid/invalid results.
- Object storage: MinIO is the recommended self-hosted production target through the S3-compatible provider. AWS S3 and Azure Blob Storage are supported alternatives. Database/internal storage is development/internal fallback only.
- E-invoice: ZUGFeRD/Factur-X is the primary target through the selected generator boundary; XRechnung remains a later export path. Current operational JSON/XML/source-model artifacts are not a legal compliance claim.

## Current Direction

The operational priority is WebAdmin plus the WebApi, Worker, provider, and mobile contracts needed to onboard and support real businesses.

Current strategic decisions:

- WebAdmin remains the primary operational workspace for onboarding, support, provider readiness, billing, inventory, communication, and mobile support.
- WebApi owns audience-first route boundaries and provider callback endpoints.
- Worker services own asynchronous dispatch, provider operations, archive maintenance, and retry processing.
- Mobile apps consume the same contracts and are guarded for implemented workflows, but store release still requires signed artifacts and real device/provider validation.
- PostgreSQL is the preferred/default persistence provider; SQL Server remains supported.
- MinIO is the recommended self-hosted object-storage provider; AWS S3 and Azure Blob remain supported alternatives.
- Consumer checkout is not in the first mobile-app launch scope. Business subscription purchase and payment operations stay in web/back-office workflows for the first launch.

## Readiness Snapshot

Code-backed status is tracked in [docs/go-live-status.md](docs/go-live-status.md). The active roadmap is [BACKLOG.md](BACKLOG.md).

| Area | Status |
| --- | --- |
| WebAdmin | Core operational workflows and support surfaces are implemented; provider live validation remains deployment-specific. |
| WebApi | Canonical audience-first route boundaries are active for public, member, business, admin, and provider callback flows. |
| Worker | Provider callback, communication, shipment, archive, and retry workers exist; production enablement depends on deployment configuration. |
| Web | Front-office/member shell exists and must stay customer-facing; checkout expansion remains separate from WebAdmin operations. |
| Mobile | Implemented Consumer and Business workflows are guarded and usable, but store launch still needs signed packages, production mobile configuration, and device/provider smoke. |
| Object storage | Modular architecture is implemented; local MinIO smoke exists; production immutability requires real provider validation. |
| E-invoice | Tooling boundary and local adapter smoke exist; legal/compliance evidence and production artifact smoke remain required. |
| External providers | Test/staging paths are guarded; production readiness requires provider-specific live smoke, monitoring, and operator sign-off. |

## Quick Start

Prerequisites:

- .NET 10 SDK
- Node.js 24.x for `src/Darwin.Web`
- Docker Desktop for local databases and optional object-storage smoke
- Visual Studio or equivalent MAUI tooling for mobile work
- Secure local configuration for secrets and provider credentials; never commit them

Common backend commands:

```powershell
dotnet restore Darwin.sln
dotnet build src/Darwin.WebAdmin/Darwin.WebAdmin.csproj
dotnet build src/Darwin.WebApi/Darwin.WebApi.csproj

dotnet run --project src/Darwin.WebAdmin -c Debug --launch-profile "https"
dotnet run --project src/Darwin.WebApi -c Debug --launch-profile "https"
```

Front-office:

```powershell
cd src/Darwin.Web
npm install
npm run dev
```

Before production or provider work, read:

- [docs/README.md](docs/README.md)
- [docs/production-setup.md](docs/production-setup.md)
- [docs/external-smoke-inputs.md](docs/external-smoke-inputs.md)
- [docs/go-live-status.md](docs/go-live-status.md)

## Documentation

- [docs/README.md](docs/README.md): documentation map and source-of-truth matrix.
- [BACKLOG.md](BACKLOG.md): active blockers, near-term work, later-phase work, and open decisions.
- [DarwinDomainDesign.md](DarwinDomainDesign.md): domain model and cross-module rules.
- [DarwinWebAdmin.md](DarwinWebAdmin.md): WebAdmin operating model and admin workflow guidance.
- [DarwinFrontEnd.md](DarwinFrontEnd.md): Next.js storefront/member portal boundaries.
- [DarwinWebApi.md](DarwinWebApi.md): route roots, API audiences, DTO boundaries, and webhook rules.
- [DarwinMobile.md](DarwinMobile.md): mobile responsibilities and backend dependencies.
- [DarwinTesting.md](DarwinTesting.md): testing lanes and verification policy.
- [docs/persistence-providers.md](docs/persistence-providers.md): persistence-provider architecture.
- [docs/archive-storage-provider-decision.md](docs/archive-storage-provider-decision.md): object-storage provider decision.
- [docs/minio-storage-runbook.md](docs/minio-storage-runbook.md): local MinIO smoke and production MinIO checklist.
- [docs/e-invoice-tooling-decision.md](docs/e-invoice-tooling-decision.md): e-invoice tooling decision and non-goals.
- [CONTRIBUTING.md](CONTRIBUTING.md): engineering rules and contribution standards.
