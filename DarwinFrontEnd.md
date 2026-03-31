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

- `In Progress`: the front-office direction is defined and the main WebAdmin/operator prerequisites are now documented
- `Secondary priority`: it is not the current execution focus
- `Dependency-heavy`: it depends on `Darwin.WebApi`, core backend rules, and operational readiness from `Darwin.WebAdmin`

The current platform priority is to finish `WebAdmin` and the admin/backend capabilities needed for real operations and onboarding first.

## 2.1 What Is Now Ready Because of WebAdmin

The next `Darwin.Web` execution pass should assume the following operational foundations now exist or are materially clearer because of the WebAdmin work:

- business onboarding and support now have real operator workflows in `Darwin.WebAdmin`
- approval, suspension, reactivation, and support visibility now exist for business lifecycle
- identity/admin support now covers activation, reset, lock/unlock, role assignment, and delegated support paths
- loyalty operations now have real operator surfaces for programs, reward tiers, accounts, campaigns, redemptions, and scan-session diagnostics
- subscription/billing support now has plan, invoice, payment, refund, and webhook triage surfaces
- shipping and inventory support now have real operator queues and troubleshooting entry points
- communication management now has template visibility, audit history, repeated-failure triage, and controlled retry for supported live flows
- localization and compliance now have baseline readiness and remediation surfaces, even though full multilingual and compliance engines are still later work

Practical consequence for `Darwin.Web`:

- the front-office can now be planned against a much clearer operational platform
- public/member journeys should align with these operational realities instead of inventing parallel behavior
- if a web journey needs an operator fallback or a support outcome, that fallback can usually be assumed to exist now in `Darwin.WebAdmin`

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

## 4.1 Planning Rule for Web Work

When starting or extending `Darwin.Web`, treat the current platform like this:

- `Darwin.WebAdmin` is the operational source of truth for what staff can support
- `Darwin.WebApi` is the only delivery boundary that `Darwin.Web` should consume
- `Darwin.Web` should not replicate back-office workflows, but it must respect their lifecycle and state model

This means:

- if approval or suspension matters to business/member access, the web UX must reflect the same backend rules already used by mobile/admin
- if communication flows now support controlled retry and audit visibility in admin, web should integrate with the same lifecycle, not invent separate mail logic
- if loyalty, subscription, payment, shipping, and compliance states now surface in admin, web should use those same concepts consistently through API contracts

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

### Current platform implication

The platform is no longer at "no communication support exists". Phase-1 communication support now includes:

- live transactional template visibility
- email dispatch audits
- delivery severity, latency, and repeated-failure triage
- controlled retry for supported live flows:
  - business invitation
  - account activation
  - password reset

What is still not ready for the web to assume:

- a full multi-channel Communication Core
- broad generic replay for every message type
- a rich outbox or delivery-log platform equivalent to a dedicated messaging product

So the front-office should:

- reuse the existing backend flow keys and delivery behavior
- avoid inventing custom web-only mail pipelines
- keep communication UI expectations aligned with the current phase-1 backend reality

## 7. Account and Onboarding Dependencies

The front-office depends on backend/admin capabilities for:

- account creation
- account activation
- password recovery
- profile setup
- language/default-locale defaults
- business/member context where applicable

If future front-facing business onboarding is introduced, its dependency on WebAdmin approval and backend provisioning must remain explicit.

### Current readiness from WebAdmin/backend

The following are now real platform assumptions, not speculative backlog items:

- business invitation acceptance exists in `Darwin.Mobile.Business`
- pending-approval business accounts are soft-gated, not fully blocked from setup
- admin/operator support now exists for invitation, activation, approval, suspension, reset-password, and support follow-up
- subscription-plan, invoice, payment, and refund support are now operational enough that public/member UX can be designed with real escalation paths in mind

For `Darwin.Web`, this means:

- member/account journeys should model lifecycle states explicitly
- business-facing future web flows must not assume approval is binary-at-login only
- support escalation and "what happens when this fails" paths should be thought through against what admin actually supports now

## 7.1 Web Workstreams That Are Now Unblocked

Because of the WebAdmin work, these web workstreams are now materially less risky to start:

- public CMS and storefront build-out against stable operator-managed content/catalog surfaces
- member auth and account self-service UX against clarified activation/reset/support rules
- member loyalty UX against real admin-managed loyalty configuration
- member order/invoice/payment views against clearer payment/refund/compliance support surfaces
- localized storefront/account design against explicit platform fallback behavior

The biggest remaining cross-cutting constraints are no longer "admin cannot support this at all"; they are mostly:

- API-contract maturity
- front-office UX design
- deeper Communication Core evolution
- deeper localization/compliance implementation

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

### Current platform nuance

The platform now has:

- explicit site-level localization governance
- business-level localization ownership guidance
- CRM/customer visibility for locale-source and platform-fallback usage

The platform does not yet have:

- full admin localization infrastructure
- complete multilingual template/content management

So `Darwin.Web` should be built to:

- consume locale/fallback data explicitly where exposed
- keep strings and content rendering localization-ready from day one
- avoid assuming the current platform default culture is the same thing as a user-owned locale preference

## 11. Configuration-Driven Behavior

The front-office should expect future config-driven behavior around:

- branding
- default locale and language
- payment availability
- shipping availability
- tax/invoicing presentation
- communication preferences and channel visibility

These settings should come from backend/domain-driven configuration, not isolated front-end constants.

## 11.1 Compliance and Billing Readiness for Web

The current platform now exposes enough operator context that web planning should explicitly account for:

- B2B vs B2C customer tax profile
- VAT ID presence/absence
- invoice issuer completeness
- archive/e-invoice baseline readiness indicators
- subscription-plan and invoice/payment lifecycle support

This does not mean the public/member web should expose every operator-facing detail.
It does mean:

- member invoice/payment/account screens should be designed with these states in mind
- checkout/account/tax presentation should stay configuration-driven
- future web backlog items should call out where they depend on deeper compliance or billing support that is still near-term rather than already complete

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

## 14. How the Web Chat Should Start

When starting the dedicated `Darwin.Web` chat, the implementation pass should begin in this order:

1. read:
   - [E:\_Projects\Darwin\DarwinFrontEnd.md](E:\_Projects\Darwin\DarwinFrontEnd.md)
   - [E:\_Projects\Darwin\BACKLOG.md](E:\_Projects\Darwin\BACKLOG.md)
   - [E:\_Projects\Darwin\DarwinWebApi.md](E:\_Projects\Darwin\DarwinWebApi.md)
   - [E:\_Projects\Darwin\src\Darwin.Web\README.md](E:\_Projects\Darwin\src\Darwin.Web\README.md)
2. extract the current ready platform assumptions from WebAdmin/backend rather than planning as if the platform were still incomplete in the same ways
3. rewrite or refine the `Darwin.Web` backlog section so it reflects:
   - what is truly unblocked now
   - what still depends on API or backend work
   - what is later-phase and should not pollute phase-1 web delivery
4. pick a smallest coherent web slice and implement it end-to-end instead of scattering across many pages

## 15. Backlog Update Rules for the Web Chat

The dedicated web chat should update `BACKLOG.md` using these rules:

- move items from generic web ambitions into explicit workstreams such as:
  - storefront CMS
  - catalog browsing
  - auth/account self-service
  - member loyalty
  - orders/invoices/payments
- mark dependencies explicitly when a web story depends on:
  - `Darwin.WebApi` contract work
  - deeper Communication Core work
  - deeper localization/compliance work
  - later-phase provider/channel expansion
- do not mark a web area blocked by admin/support work if that support workflow is now actually present in `Darwin.WebAdmin`
- when in doubt, describe whether the gap is:
  - delivery UX gap
  - API gap
  - domain/backend gap
  - later-phase expansion

## 16. Mobile Review Impacts to Remember Later

When the separate mobile-review chat starts, it should explicitly revisit these platform changes because they may affect mobile assumptions:

- business approval soft-gate behavior and access-state expectations
- invitation acceptance and activation lifecycle behavior
- self-service resend-activation behavior
- loyalty admin configuration and support surfaces that may explain missing mobile states
- mobile operations diagnostics, push-token remediation, and device-state filtering
- communication audit/retry behavior for invitation, activation, and password-reset flows
- subscription/billing support surfaces that may change how business-account/payment issues should be triaged
- localization fallback visibility and customer locale-source handling
- compliance/tax readiness indicators that may affect how invoices or billing context should be interpreted in mobile
