# Contributing Guide

This guide gives new contributors enough context to work effectively in the Darwin repository without prior chat or handover context.

## Project Overview

Darwin is a modular platform spanning:

- CMS
- commerce
- loyalty
- CRM planning
- mobile apps
- two separate web applications

The delivery layer is split as follows:

- `Darwin.WebAdmin`: ASP.NET Core MVC/Razor back-office for administrators and staff
- `Darwin.Web`: Next.js/React front-office for the public storefront and authenticated member portal
- `Darwin.WebApi`: contracts-first HTTP API for mobile, front-office, and integration use cases

## Repository Structure

```text
src/
|-- Darwin.Domain
|-- Darwin.Application
|-- Darwin.Infrastructure
|-- Darwin.WebAdmin
|-- Darwin.WebApi
|-- Darwin.Web
|-- Darwin.Worker
|-- Darwin.Shared
|-- Darwin.Contracts
|-- Darwin.Mobile.Shared
|-- Darwin.Mobile.Consumer
`-- Darwin.Mobile.Business

tests/
|-- Darwin.Tests.Unit
|-- Darwin.Tests.Integration
|-- Darwin.Contracts.Tests
|-- Darwin.Infrastructure.Tests
`-- Darwin.WebApi.Tests
```

## Core Platform

| Project | Description | Status |
| --- | --- | --- |
| `Darwin.Domain` | Entities, value objects, enums, business rules | Stable |
| `Darwin.Application` | Handlers, DTOs, validators, use cases | Active |
| `Darwin.Infrastructure` | EF Core, security, persistence, composition helpers | Stable |
| `Darwin.WebAdmin` | MVC/Razor back-office | Active |
| `Darwin.WebApi` | Public/member/business/mobile API surfaces | Active |
| `Darwin.Web` | Next.js front-office and member portal | In Progress |

## Shared Libraries

| Project | Description |
| --- | --- |
| `Darwin.Shared` | Result wrappers, helpers, constants |
| `Darwin.Contracts` | Shared API contracts for WebApi + mobile |
| `Darwin.Mobile.Shared` | Shared mobile services, auth, HTTP, integration abstractions |

## Architecture Rules

- Domain must not depend on application, infrastructure, web, or mobile code.
- Application depends on Domain only.
- Infrastructure depends on Application.
- `Darwin.WebAdmin` depends on Application + Infrastructure.
- `Darwin.WebApi` depends on Application + Infrastructure.
- `Darwin.Web` is a Node/React app and does not build as part of the .NET solution.
- Mobile apps depend on `Darwin.Contracts` and `Darwin.Mobile.Shared`.

Additional delivery rules:

- CMS data for the public site must be API-friendly.
- Do not reuse admin DTOs for public delivery.
- Keep public/member/business/admin API surfaces distinct.
- Keep the architecture compatible with a future BFF layer.

## Coding Standards

- C# 14 and nullable reference types are expected in .NET projects.
- Use async/await consistently where appropriate.
- Prefer strongly typed constants/enums over magic strings.
- Keep comments professional and English-only.
- Keep new code aligned with the repository's existing conventions.

For `Darwin.Web`:

- follow the current Next.js app-router structure
- keep front-office code separate from back-office assumptions
- do not document `dotnet build` as a way to build the front-end

## Solution Filters

### `Darwin.WebAdminOnly.slnf`

Use this for most .NET web/server work. It contains:

- Domain
- Application
- Infrastructure
- WebAdmin
- WebApi
- Shared
- Contracts

### `Darwin.MobileOnly.slnf`

Use this for mobile-focused work. It contains:

- Mobile.Shared
- Mobile.Business
- Mobile.Consumer
- Contracts

`Darwin.Web` is a Node project and is intentionally not part of the .NET solution filters.

## Local Development Expectations

Before opening a PR:

1. Build the relevant project(s).
2. Run the relevant tests.
3. Update documentation when architecture, API shape, or developer workflows change.
4. Keep contract changes synchronized across:
   - `Darwin.Contracts`
   - `Darwin.Application`
   - `Darwin.WebApi`
   - related docs

## Pull Request Requirements

- No unrelated cleanup mixed into architectural changes.
- No breaking API changes without corresponding contract/doc updates.
- No front-office assumptions embedded in back-office DTOs or views.
- No admin-only concerns exposed in public/member API contracts.

## Front-End Note

`Darwin.Web` currently lives under `src/Darwin.Web` and uses:

- Next.js
- React
- TypeScript
- Tailwind CSS
- Node/npm tooling

Use `npm install`, `npm run dev`, `npm run build`, and `npm run start` there. Do not expect it to participate in `dotnet build`.

## Ownership

All code in this repository is private to the Darwin project team unless stated otherwise.
