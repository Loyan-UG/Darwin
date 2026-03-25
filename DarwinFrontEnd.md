# Darwin Front-End Guide

[![Next.js](https://img.shields.io/badge/Next.js-14.0-black?logo=next.js)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Node.js](https://img.shields.io/badge/Node.js-18.0.0-339933?logo=node.js&logoColor=white)](https://nodejs.org/)
[![TailwindCSS](https://img.shields.io/badge/TailwindCSS-3.0-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)

> Scope: `src/Darwin.Web`, the public storefront and member portal.

## Purpose

`Darwin.Web` is the customer-facing application for:

- public storefront browsing
- CMS page rendering
- member authentication and account journeys
- loyalty, orders, invoices, and support-related customer views

It is separate from `Darwin.WebAdmin`, which remains the internal staff-facing portal.

## Position in the Architecture

- `Darwin.Web` is the front-office delivery application
- `Darwin.WebApi` is the contracts-first HTTP boundary
- `Darwin.WebAdmin` is the back-office
- the member portal belongs to the front-office, not the admin system

## Data Fetching and API Boundaries

The front-office should consume:

- public CMS/content endpoints
- public storefront/catalog endpoints
- member-only profile/order/invoice/loyalty endpoints

Rules:

- do not consume admin Razor views or admin DTOs
- keep storefront DTOs presentation-oriented
- keep member DTOs separate from admin operational DTOs
- document every new front-office dependency in `DarwinWebApi.md`

## Build and Runtime Workflow

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

## Rendering Strategy

Recommended direction:

- SSR for SEO-critical pages
- SSG/ISR for mostly public, cache-friendly pages
- authenticated fetches for member journeys

Typical split:

- CMS pages: SSR or ISR
- product/category pages: SSR or ISR
- member pages: authenticated server/client fetch depending on session approach

## Authentication Direction

Front-office authentication is backed by `Darwin.WebApi`.

Current architecture must stay compatible with:

- direct token-based API access
- a future BFF that owns session/cookie management

## Folder Growth Guidance

As the application expands, prefer a structure such as:

```text
src/Darwin.Web/src/
├── app/
├── components/
├── features/
├── lib/
├── services/
└── types/
```
