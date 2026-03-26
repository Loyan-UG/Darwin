# Darwin Front-End Guide

[![Next.js](https://img.shields.io/badge/Next.js-14.0-black?logo=next.js)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Node.js](https://img.shields.io/badge/Node.js-18.0.0-339933?logo=node.js&logoColor=white)](https://nodejs.org/)
[![TailwindCSS](https://img.shields.io/badge/TailwindCSS-3.0-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
[![Stripe](https://img.shields.io/badge/Stripe-Phase--1-635BFF?logo=stripe&logoColor=white)](https://stripe.com/)
[![DHL](https://img.shields.io/badge/DHL-Phase--1-FFCC00?logoColor=black)](https://www.dhl.de/)

> Scope: `src/Darwin.Web`, the public storefront and authenticated member portal.

## 1. Purpose

`Darwin.Web` is the customer-facing web application for:

- public CMS pages
- product and category browsing
- checkout and payment flow
- authenticated member profile, loyalty, orders, invoices, and account self-service

It is separate from `Darwin.WebAdmin`, which remains the staff-facing operational portal.

## 2. Current Status

- `In Progress`: the front-office direction is defined
- `Secondary priority`: it is not the current execution focus
- `Dependency-heavy`: it depends on `Darwin.WebApi`, core backend rules, and operational readiness from `Darwin.WebAdmin`

The current platform priority is to finish `WebAdmin` and the admin/backend capabilities needed for real operations and onboarding first.

## 3. Position in the Architecture

- `Darwin.Web` is the front-office delivery application
- `Darwin.WebApi` is the contracts-first HTTP boundary
- `Darwin.WebAdmin` is the operational back-office
- the member portal belongs to the front-office, not the admin system

Rules:

- do not consume admin Razor views
- do not consume admin DTOs directly
- do not bypass `Darwin.WebApi` for front-office delivery

## 4. Data Fetching and API Boundaries

The front-office should consume:

- public CMS/content endpoints
- public storefront/catalog endpoints
- public checkout endpoints
- member profile/order/invoice/loyalty endpoints

Delivery rules:

- storefront DTOs must remain presentation-oriented
- member DTOs must remain audience-specific
- admin operational models must not leak into public/member delivery

## 5. Phase-1 Integration Strategy

### Payments

- phase-1 checkout design is `Stripe-first`
- the front-office should assume Stripe is the first real provider integration
- other providers and market-specific payment methods remain later-phase work
- the UI should remain config-driven enough that additional providers can be added later without rewriting the entire checkout architecture

### Shipping

- phase-1 shipping flow is `DHL-first`
- other carriers are later-phase work
- shipping selection and visibility should remain generic enough for future carriers, but active assumptions and backlog should remain DHL-first

## 6. User-Facing Communication Touchpoints

Front-office journeys depend on platform communication capability.

Immediate touchpoints include:

- signup confirmation
- account activation
- forgot-password
- password reset
- important account notifications
- order and payment-related updates

Communication should come from reusable backend capability, not ad hoc front-end-only logic.

## 7. Account and Onboarding Dependencies

The front-office depends on backend/admin capabilities for:

- account creation
- account activation
- password recovery
- profile setup
- language/default-locale defaults
- business/member context where applicable

If future front-facing business onboarding is introduced, its dependency on WebAdmin approval and backend provisioning must remain explicit.

## 8. Rendering Strategy

Recommended direction:

- SSR for SEO-critical public pages
- SSG/ISR for cache-friendly public content
- authenticated fetches for member journeys

Typical split:

- CMS pages: SSR or ISR
- product/category pages: SSR or ISR
- member pages: authenticated server/client fetch depending on session approach

## 9. Performance and Security Expectations

### Performance

- use efficient projections from `Darwin.WebApi`
- avoid chatty client/API composition where BFF-style composition may later be needed
- keep product/category/search pages SEO- and cache-friendly
- keep checkout and account pages lean and state-aware

### Security

- front-office authentication still depends on shared platform auth practices
- secure token or future cookie/BFF session handling must be preserved
- protect PII, account actions, and order/invoice access carefully
- rate limiting and abuse protections are backend concerns that front-office flows must respect

## 10. Localization Readiness

Localization is broader than UI string translation.

The front-office must remain ready for:

- UI localization
- template/message localization
- content field translation
- language preference per user/business where relevant
- fallback language policy

This also means the front-office should consume settings and content in a configuration-driven way rather than relying on hard-coded assumptions.

## 11. Configuration-Driven Behavior

The front-office should expect future config-driven behavior around:

- branding
- default locale and language
- payment availability
- shipping availability
- tax/invoicing presentation
- communication preferences and channel visibility

These settings should come from backend/domain-driven configuration, not isolated front-end constants.

## 12. Build and Runtime Workflow

```bash
cd src/Darwin.Web
npm install
npm run dev
```

Production:

```bash
npm run build
npm run start
```

`Darwin.Web` is managed by Node/npm and does not participate in `dotnet build`.

## 13. Recommended Growth Structure

As the app grows, prefer a structure similar to:

```text
src/Darwin.Web/src/
|-- app/
|-- components/
|-- features/
|-- lib/
|-- services/
`-- types/
```

Keep API-facing types and service abstractions clearly separated from UI components.
