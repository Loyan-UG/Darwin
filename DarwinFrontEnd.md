# Darwin Front-End Guide

[![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Node.js](https://img.shields.io/badge/Node.js-20+-339933?logo=node.js&logoColor=white)](https://nodejs.org/)
[![Tailwind CSS](https://img.shields.io/badge/TailwindCSS-4-06B6D4?logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)

> Scope: `src/Darwin.Web`, the public storefront and member portal built with Next.js and React.

## Purpose

`Darwin.Web` is the front-office web application for:

- public storefront browsing
- CMS page rendering
- authenticated member/account journeys
- loyalty- and commerce-facing end-user experiences

It is separate from `Darwin.WebAdmin`, which remains the staff-facing back-office.

## Position in the Architecture

- `Darwin.Web` is the customer-facing web app.
- `Darwin.WebApi` is the contracts-first HTTP boundary for public/member/mobile use cases.
- `Darwin.WebAdmin` remains the operational/admin host.
- `Darwin.Web` should not consume back-office DTOs directly.

## Current Stack

From the current repository state:

- Next.js
- React
- TypeScript
- Tailwind CSS
- ESLint

## Project Layout

Current top-level structure:

```text
src/Darwin.Web/
|-- public/
|-- src/
|   `-- app/
|       |-- favicon.ico
|       |-- globals.css
|       |-- layout.tsx
|       `-- page.tsx
|-- package.json
|-- next.config.ts
|-- tsconfig.json
`-- eslint.config.mjs
```

### App Router

The project uses the Next.js app router (`src/app`).

- `layout.tsx` defines the global shell
- `page.tsx` is the root route
- `globals.css` contains global styles

As the app grows, keep route segments and shared UI organized under `src/app` and route-adjacent modules/components.

## Development Workflow

Install dependencies:

```bash
cd src/Darwin.Web
npm install
```

Run in development:

```bash
npm run dev
```

Build for production:

```bash
npm run build
npm run start
```

Open the app at:

- `http://localhost:3000`

## Data Fetching Guidance

The front-office should be designed for:

- SSR where SEO or first-paint content matters
- SSG/ISR where content is mostly public and can be cached
- client-side fetching only where interactivity requires it

Recommended direction:

- CMS pages: SSR or ISR
- product/category pages: SSR or ISR depending on freshness requirements
- member/account pages: authenticated server/client fetches depending on session model

## Authentication Guidance

Member authentication belongs to the front-office, but it is still part of the public application boundary.

Expected architectural behavior:

- authentication and account flows are backed by `Darwin.WebApi`
- token/session handling must follow the same security practices used elsewhere in the platform
- do not create a second "member admin" surface inside `Darwin.WebAdmin`

When a future BFF layer is introduced, it may own:

- cookie/session management
- token exchange
- server-side composition and caching

## API Consumption Rules

- Consume content, menus, SEO metadata, product listings, and member/account data through `Darwin.WebApi`.
- Do not bind the public site to back-office view models or admin DTOs.
- Keep public/member-facing contracts presentation-oriented.
- Document every new front-office API dependency in `DarwinWebApi.md`.

## Recommended Folder Growth

As the application expands, prefer a structure similar to:

```text
src/Darwin.Web/src/
|-- app/
|-- components/
|-- features/
|-- lib/
|-- services/
`-- types/
```

Suggested responsibilities:

- `components/`: reusable presentational UI
- `features/`: route- or domain-specific composition
- `lib/`: framework wiring, fetch helpers, config
- `services/`: API clients and request helpers
- `types/`: front-office-specific types and API response models

## Front-Office vs Back-Office

### Front-Office

- customer-facing
- SEO-aware
- public/member-oriented
- built for content delivery and commerce UX

### Back-Office

- operator-facing
- workflow-heavy
- CMS/catalog/order/settings management
- built with ASP.NET Core MVC/Razor

The member portal is part of the front-office, not the admin system.
