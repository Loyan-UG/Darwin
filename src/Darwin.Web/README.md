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
- home composition now uses reusable web parts with live CMS/catalog spotlight data instead of a single blank-state block
- public account self-service now includes register, activation, password-recovery, and sign-in entry points against the canonical member auth contracts
- a provisional browser session layer now exists for the web portal, using web-owned cookies plus access-token refresh in front of the canonical member APIs
- account, orders, invoices, and loyalty routes now render authenticated member data instead of staying as placeholders
- profile, preferences, and reusable address-book editing now run against the member profile endpoints instead of remaining read-only account placeholders
- phone verification now runs through the canonical member profile verification endpoints and shared confirmation flag instead of a web-local flow
- loyalty business detail pages now expose business-scoped dashboard, rewards, and timeline data instead of keeping loyalty at an overview-only level
- runtime culture is now config-driven with `de-DE`/`en-US` support, cookie/query-string switching, and locale-aware money/date formatting across storefront and member views
- resource-backed localization now exists under `src/localization/resources`, and shared shell/catalog/storefront-commerce copy is moving onto that bundle-based path so future languages can be added without rewriting feature components
- loyalty overview now consumes the richer `my/businesses` contract and business detail pages now consume personalized promotions plus promotion-interaction tracking
- loyalty discovery now also consumes public business list/category metadata, and business detail routes now keep a public pre-join experience plus direct member join instead of only showing joined-business dashboards
- loyalty discovery now also supports query-driven proximity filters with a server-rendered coordinate preview while staying on the loyalty-filtered public discovery contract
- loyalty business detail now also supports canonical browser-side scan preparation for accrual/redemption, keeping the short-lived scan token in a web-owned cookie instead of the URL
- loyalty business detail now also renders a real QR image from the active prepared scan token so the browser-prepared scan flow is immediately usable
- catalog list/detail now pass the active request culture to `Darwin.WebApi` and expose contract-safe merchandising context such as selected-category panels, compare-at savings, and category-linked navigation
- member order/invoice detail pages now consume canonical document links plus richer payment, shipment, and linked-invoice presentation instead of only showing totals
- storefront cart now supports canonical coupon apply/clear plus richer line-level tax/pricing context, and checkout summary now surfaces shipment/country context from the live intent
- storefront confirmation now also reconciles hosted-checkout return/cancel flows through the canonical payment-completion endpoint instead of acting as a passive snapshot screen

## Architectural Rules

- Treat this project as the public storefront and member portal.
- Consume CMS, catalog, SEO, account, loyalty, order, and invoice data through `Darwin.WebApi`.
- Do not bind this app to back-office DTOs or MVC view models from `Darwin.WebAdmin`.
- Keep the app compatible with future BFF-style session and composition patterns.
- Keep the theme independent from feature logic so future themes can be added without rewriting storefront/member features.
- Build pages from reusable web parts / page components with explicit slots and composition boundaries.
- Keep user-facing copy in resource bundles so new languages remain additive work instead of component rewrites.
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
- `DARWIN_WEB_SUPPORTED_CULTURES`
  - default: `de-DE,en-US`
- `DARWIN_WEB_CULTURE_COOKIE_NAME`
  - default: `darwin-web-culture`

If the API or configured CMS menu is unavailable, the shell falls back to built-in navigation links so local development and builds still succeed.
That fallback is intentionally visible in the UI and should be treated as a degraded state, not as the normal operating path.
If `culture=<supported-culture>` is present in the query string, middleware persists it into the configured culture cookie and redirects back to the clean URL.

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

Localization resources now live under:

```text
src/Darwin.Web/src/localization/
|-- index.ts
`-- resources/
    |-- shared.de-DE.json
    |-- shared.en-US.json
    |-- shell.de-DE.json
    |-- shell.en-US.json
    |-- catalog.de-DE.json
    |-- catalog.en-US.json
    |-- commerce.de-DE.json
    `-- commerce.en-US.json
```

This is the current front-office equivalent of the resource-first direction already being established in `Darwin.WebAdmin`; new languages should be introduced by adding bundles here and then wiring them into the runtime config.

## Current Implemented Slice

The current web slice includes:

- theme-isolated storefront shell
- CMS-first `main-navigation` consumption with observable fallback
- Home built through reusable hero/card web parts
- public CMS listing and CMS slug routes against live `Darwin.WebApi` content endpoints
- CMS detail rendering with route metadata, related-page navigation, and visible degraded-state behavior
- public catalog browsing against live `Darwin.WebApi` category/product endpoints
- public product-detail route against the product-by-slug endpoint
- public cart page plus add/update/remove flows against public cart endpoints with stable anonymous cart identity
- public checkout page with server-rendered address capture, live checkout intent preview, shipping selection, and order placement
- public order-confirmation route with payment-handoff retry against the storefront confirmation and payment-intent endpoints
- hosted-checkout return/cancel reconciliation through the storefront payment-completion endpoint plus a short-lived web-owned handoff cookie
- public account self-service foundation for member registration, activation email request/confirm, and password reset request/complete flows
- provisional browser sign-in plus authenticated member dashboard, order history/detail, invoice history/detail, and loyalty overview
- editable profile, preferences, and member address-book flows against the canonical member profile endpoints
- protected member fetches now refresh provisional browser sessions near expiry and retry once before falling back to sign-in again
- phone verification request/confirm inside the profile surface via the canonical SMS/WhatsApp member verification endpoints
- business-scoped loyalty detail routes with rewards, recent transactions, and cursor-based timeline paging
- config-driven `de-DE`/`en-US` shell switching with locale-aware currency/date formatting across catalog, cart, checkout, orders, invoices, and loyalty pages
- resource-backed shared shell/catalog/storefront-commerce copy through JSON bundles under `src/localization/resources`
- loyalty overview cards backed by `my/businesses` plus business-scoped promotions feed/tracking through the canonical member loyalty contracts
- public loyalty discovery against the canonical business-discovery contracts plus pre-join business detail/join UX on `/loyalty` and `/loyalty/[businessId]`
- query-driven loyalty proximity browsing with a server-rendered coordinate preview derived from the same public discovery result set
- browser-side loyalty scan preparation for accrual/redemption with branch selection, reward selection, and short-lived token visibility on `/loyalty/[businessId]`
- QR rendering for the active prepared loyalty scan token on `/loyalty/[businessId]`
- culture-aware catalog delivery plus contract-safe merchandising polish across `/catalog` and `/catalog/[slug]`
- member commerce detail hardening for order/invoice documents, shipment/payment snapshots, and linked invoice/order follow-up
- public cart coupon apply/clear plus richer tax/shipping/billing presentation across cart and checkout summary surfaces

For broader platform documentation, see:

- [`../../README.md`](../../README.md)
- [`../../DarwinFrontEnd.md`](../../DarwinFrontEnd.md)
- [`../../DarwinWebApi.md`](../../DarwinWebApi.md)
