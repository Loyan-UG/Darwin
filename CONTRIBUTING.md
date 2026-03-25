# Contributing Guide

This guide gives new contributors enough context to work effectively in the Darwin repository without prior handover context.

## Project Overview

Darwin is a modular platform spanning:

- CMS
- commerce
- CRM
- loyalty
- inventory and procurement
- billing and accounting
- two distinct web applications
- mobile applications

Delivery applications:

- `Darwin.WebAdmin`: ASP.NET Core MVC/Razor back-office using HTMX
- `Darwin.Web`: Next.js/React front-office for storefront and member portal
- `Darwin.WebApi`: contracts-first HTTP boundary for front-office, mobile, and future external clients

## Repository Structure

```text
src/
├── Darwin.Domain
├── Darwin.Application
├── Darwin.Infrastructure
├── Darwin.WebAdmin
├── Darwin.WebApi
├── Darwin.Web
├── Darwin.Worker
├── Darwin.Shared
├── Darwin.Contracts
├── Darwin.Mobile.Shared
├── Darwin.Mobile.Consumer
└── Darwin.Mobile.Business

tests/
├── Darwin.Tests.Unit
├── Darwin.Tests.Integration
├── Darwin.Contracts.Tests
├── Darwin.Infrastructure.Tests
└── Darwin.WebApi.Tests
```

## Architecture Rules

- Domain must not depend on Application, Infrastructure, Web, or Mobile code.
- Application depends on Domain only.
- Infrastructure depends on Application.
- `Darwin.WebAdmin` depends on Application and Infrastructure.
- `Darwin.WebApi` depends on Application and Infrastructure.
- `Darwin.Web` is a Node/React project and does not build inside the .NET solution pipeline.

Delivery rules:

- CMS data for the public site must be delivered through `Darwin.WebApi`.
- do not reuse admin DTOs for public/member delivery.
- keep public, member, business, and admin API surfaces distinct.
- keep the architecture compatible with a future BFF layer.

## Coding Standards

- keep nullable reference types enabled
- initialize non-nullable strings with `string.Empty`
- mark optional references with `?`
- use English XML documentation for new public classes and members
- keep code comments English-only
- avoid alias properties and duplicate state
- reserve TODO markers for genuinely later-phase work
- do not write Persian inside source code

## Loyalty Rule

CRM does not own loyalty balances.

- `Customer.LoyaltyPointsTotal` is removed
- `LoyaltyPointEntry` is removed
- loyalty balances must come from `LoyaltyAccount` and `LoyaltyPointsTransaction`

## WebAdmin Rule

When adding new back-office interactions:

- prefer HTMX for partial rendering and form posts
- keep Bootstrap-focused JavaScript minimal
- do not introduce Alpine.js unless a specific UI state problem justifies it

## Front-End Rule

`Darwin.Web` is the front-office and is built with:

- Next.js
- React
- TypeScript
- Tailwind CSS
- Node/npm tooling

Use:

- `npm install`
- `npm run dev`
- `npm run build`
- `npm run start`

Do not document or expect `dotnet build` to compile the front-office.

## Change Discipline

When changing architecture, API shape, or developer workflow:

1. update code
2. update migrations if needed
3. update the relevant docs
4. update `BACKLOG.md`
5. run the relevant builds and tests

When contracts change, keep these synchronized:

1. `Darwin.Contracts`
2. `Darwin.Application`
3. `Darwin.WebApi`
4. consumer documentation
5. affected clients

## Pull Request Expectations

- no unrelated cleanup mixed into architectural work
- no breaking API changes without matching contract and doc updates
- no front-office assumptions in back-office DTOs or views
- no admin-only DTOs exposed to public/member consumers
