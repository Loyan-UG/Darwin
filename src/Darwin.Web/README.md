# Darwin.Web

`Darwin.Web` is the public front-office application for Darwin. It is built with Next.js 16, React 19, TypeScript, and Tailwind CSS 4.

This project is separate from:

- `Darwin.WebAdmin`, the ASP.NET Core MVC/Razor back-office
- `Darwin.WebApi`, the shared API surface used by front-office, mobile, and future integrations

## Development

Install dependencies:

```bash
npm install
```

Run the development server:

```bash
npm run dev
```

Build and run the production build:

```bash
npm run build
npm run start
```

Open [http://localhost:3000](http://localhost:3000).

## Current Project State

The repository has moved beyond the raw front-office starting line:

- the default starter page has been replaced by a real storefront shell
- the app now has theme isolation, navigation composition, and working storefront routes for CMS, catalog, cart, and checkout
- CMS-backed navigation is wired with a safe fallback so frontend work is not blocked when the local API or menu seed is unavailable
- CMS fallback is now explicitly observable in the shell so menu/API problems are visible during development and staging
- anonymous storefront cart and checkout now run against live public `Darwin.WebApi` contracts instead of placeholder-only pages

## Architectural Rules

- Treat this project as the public storefront and member portal.
- Consume CMS, catalog, SEO, account, loyalty, order, and invoice data through `Darwin.WebApi`.
- Do not bind this app to back-office DTOs or MVC view models from `Darwin.WebAdmin`.
- Keep the app compatible with future BFF-style session and composition patterns.
- Keep the theme independent from feature logic so future themes can be added without rewriting storefront/member features.
- Build pages from reusable web parts / page components with explicit slots and composition boundaries.
- It is acceptable for the initial Home page to remain minimal while the shell and routing foundation are established.

## Initial UX Direction

- Use the Cartzilla grocery storefront as a visual reference, not as an architectural dependency.
- Build a reusable storefront shell first: header, navigation, footer, content slots, and theme tokens.
- Pull navigation/content from `Darwin.WebApi` where contracts already exist, especially public CMS menus and pages.
- Keep public/member feature modules isolated from any specific theme implementation.
- Treat CMS `main-navigation` as the primary navigation source; built-in links are only an emergency fallback.

## Runtime Configuration

`Darwin.Web` currently uses these environment variables:

- `DARWIN_WEBAPI_BASE_URL`
  - default: `http://localhost:5134`
- `DARWIN_WEB_MAIN_MENU_NAME`
  - default: `main-navigation`
- `DARWIN_WEB_CULTURE`
  - default: `de-DE`

If the API or configured CMS menu is unavailable, the shell falls back to built-in navigation links so local development and builds still succeed.
That fallback is intentionally visible in the UI and should be treated as a degraded state, not as the normal operating path.

## Recommended Growth Structure

As the app moves beyond the starter template, prefer a structure similar to:

```text
src/Darwin.Web/src/
|-- app/
|-- components/
|-- features/
|-- themes/
|-- web-parts/
|-- lib/
|-- services/
`-- types/
```

The current implementation already follows that direction for `themes`, `web-parts`, `features`, and shared shell components.

## Current Implemented Slice

The current web slice includes:

- theme-isolated storefront shell
- CMS-first `main-navigation` consumption with observable fallback
- placeholder Home built through page-part composition
- public CMS listing and CMS slug routes against live `Darwin.WebApi` content endpoints
- public catalog browsing against live `Darwin.WebApi` category/product endpoints
- public product-detail route against the product-by-slug endpoint
- public cart page plus add/update/remove flows against public cart endpoints with stable anonymous cart identity
- public checkout page with server-rendered address capture, live checkout intent preview, shipping selection, and order placement
- public order-confirmation route with payment-handoff retry against the storefront confirmation and payment-intent endpoints

For broader platform documentation, see:

- [`../../README.md`](../../README.md)
- [`../../DarwinFrontEnd.md`](../../DarwinFrontEnd.md)
- [`../../DarwinWebApi.md`](../../DarwinWebApi.md)
