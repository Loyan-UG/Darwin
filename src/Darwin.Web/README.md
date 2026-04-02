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
- home composition now also includes data-backed metric web parts and a part-owned hero side panel, so the composer no longer depends on home-only hardcoded aside copy
- home composition now also includes a dedicated journey/link-list web part so CMS, catalog, and account entry flows stay visible as one front-office system instead of only a shortcut card grid
- home metrics and hero highlights now also surface CMS/catalog/category contract health explicitly, so public-content degradation is visible inside composition instead of hiding behind static counts
- home composition now also includes a reusable status-list web part so CMS, catalog, and account-entry surfaces can stay actionable as contract-backed lanes instead of only cards and metrics
- home composition now also includes a reusable stage-flow web part so CMS, catalog, and member follow-up read as one staged storefront journey instead of disconnected route clusters
- home composition now also includes a reusable pair-panel web part so CMS and catalog can stay visible as two coordinated storefront surfaces rather than isolated spotlight blocks
- home composition now also includes a reusable agenda-columns web part so content, commerce, and member follow-up can stay visible as parallel storefront streams instead of one-dimensional section stacks
- public account self-service now includes register, activation, password-recovery, and sign-in entry points against the canonical member auth contracts
- public account self-service and sign-in now also normalize email input plus stronger required/autocomplete/password guardrails so avoidable auth-flow mismatches are reduced before the canonical API call
- a provisional browser session layer now exists for the web portal, using web-owned cookies plus access-token refresh in front of the canonical member APIs
- account, orders, invoices, and loyalty routes now render authenticated member data instead of staying as placeholders
- profile, preferences, and reusable address-book editing now run against the member profile endpoints instead of remaining read-only account placeholders
- dashboard, profile, preferences, and address-book screens now also share explicit member-portal navigation so the protected account area behaves like one subsystem instead of separate editor pages
- phone verification now runs through the canonical member profile verification endpoints and shared confirmation flag instead of a web-local flow
- orders, invoices, and loyalty overview now also follow the same shared member-portal navigation model, so the full authenticated portal is converging on one navigation/chrome contract
- dashboard, orders, invoices, and loyalty overview now also expose breadcrumb-style route orientation plus cross-surface handoff actions, so the member area stays connected to the wider front-office system instead of behaving like a sealed portal island
- profile, preferences, addresses, order detail, invoice detail, and loyalty business detail now also expose the same breadcrumb-style route orientation plus cross-surface handoff actions, so editor/detail routes no longer fall back to a narrower portal-only context
- dashboard and preferences now also expose explicit route-summary/follow-up panels, and address/order/invoice empty states now keep actionable storefront/member CTAs instead of falling back to passive placeholder blocks
- loyalty business detail pages now expose business-scoped dashboard, rewards, and timeline data instead of keeping loyalty at an overview-only level
- loyalty business detail now also follows the shared member-portal navigation model, and prepared scan-token state now clears stale or mismatched browser cookie state instead of drifting silently
- loyalty overview, discovery, public business detail, and signed-in business detail now also expose explicit route-summary panels plus actionable degraded/empty-state follow-up CTAs, so loyalty routes stay observable and navigable instead of collapsing into passive no-data blocks
- runtime culture is now config-driven with `de-DE`/`en-US` support, cookie/query-string switching, and locale-aware money/date formatting across storefront and member views
- resource-backed localization now exists under `src/localization/resources`, and shared shell/catalog/storefront-commerce plus account-edit/member-commerce and loyalty discovery/public-detail copy now runs on that bundle-based path so future languages can be added without rewriting feature components
- public CMS list/detail delivery now also follows the active request culture instead of relying on backend default-language behavior
- route metadata for shell, Home, CMS, catalog, checkout, account, orders, invoices, and loyalty now also resolves through the same culture-aware resource path instead of fixed title/description literals
- SEO metadata is now centralized through `src/lib/seo.ts`, using `DARWIN_WEB_SITE_URL` plus culture-aware resource bundles to shape canonical, Open Graph, and Twitter metadata for public routes
- private and mixed routes such as account, cart, checkout, orders, invoices, and loyalty now explicitly emit `noindex` metadata instead of relying on ad hoc route-level defaults
- `robots.txt` and `sitemap.xml` now publish only the public storefront surface, with sitemap entries derived from live CMS pages and catalog products instead of a hand-maintained route list
- public Home/CMS/catalog URLs now also support locale-prefixed routing through middleware rewrite and request-level culture headers, so indexable multilingual URLs no longer depend only on cookies or `?culture=...`
- shell navigation, home web parts, CMS browsing, catalog browsing, and related back-links now also generate locale-aware public URLs through a shared locale-routing helper instead of depending on redirect-only behavior
- member sign-in return targets, account self-service flow return paths, loyalty action return paths, payment failure paths, and storefront cart display links now pass through shared app-path sanitization so the web client does not trust user-supplied redirect targets
- catalog, CMS, and loyalty public search/discovery routes now also normalize page/text/numeric search params through shared helpers and use locale-aware form actions, so server-rendered filters behave consistently across public surfaces instead of mixing ad hoc parsing rules
- local web-owned validation and flash messages for register/activation/password/sign-in, cart/checkout, payment retry, profile/preferences/addresses, and loyalty join/scan/promotion flows now travel as localization keys and resolve in the UI bundles instead of staying as English-only server-action fallbacks
- generic public/member API fallback messages plus CMS degraded-mode UI copy now also resolve through resources, so network/not-found/http fallback states no longer depend on English-only client literals
- CMS detail unavailable state plus catalog index/detail now also expose route-summary diagnostics and actionable no-result/unavailable follow-up CTAs, so degraded public storefront routes stay observable and navigable instead of collapsing into passive warning blocks
- cart, checkout, and confirmation now also expose route-summary diagnostics plus stronger empty/unavailable follow-up CTAs, so the conversion path stays observable and actionable instead of degrading into passive panels
- loyalty overview now consumes the richer `my/businesses` contract and business detail pages now consume personalized promotions plus promotion-interaction tracking
- loyalty discovery now also consumes public business list/category metadata, and business detail routes now keep a public pre-join experience plus direct member join instead of only showing joined-business dashboards
- loyalty discovery now also supports query-driven proximity filters with a server-rendered coordinate preview while staying on the loyalty-filtered public discovery contract
- loyalty business detail now also supports canonical browser-side scan preparation for accrual/redemption, keeping the short-lived scan token in a web-owned cookie instead of the URL
- loyalty business detail now also renders a real QR image from the active prepared scan token so the browser-prepared scan flow is immediately usable
- catalog list/detail now pass the active request culture to `Darwin.WebApi` and expose contract-safe merchandising context such as selected-category panels, compare-at savings, and category-linked navigation
- `/catalog` now also exposes a visible-result search/sort lens for the products already loaded on the current server page, while keeping that lens explicit and non-canonical until true public search/facet/sort contracts exist
- `/catalog` now also surfaces visible-vs-loaded-vs-total result summaries plus first/last page jumps, so catalog window navigation is more complete without pretending backend search/facets already exist
- product detail now also surfaces category-aware related products by reusing the current public category/product contracts instead of inventing a separate recommendation endpoint
- product detail now also exposes breadcrumb, product-reference snapshot, and cross-surface storefront handoff actions so the conversion route does not behave like an isolated leaf page
- member order/invoice detail pages now consume canonical document links plus richer payment, shipment, and linked-invoice presentation instead of only showing totals
- member order/invoice detail unavailable states now also keep explicit follow-up actions to orders/invoices, account, and catalog instead of collapsing into warning-only dead ends
- storefront cart now supports canonical coupon apply/clear plus richer line-level tax/pricing context, and checkout summary now surfaces shipment/country context from the live intent
- storefront cart now also keeps a small continue-shopping follow-up rail plus explicit cart next-step guidance visible by reusing the public catalog contract instead of depending only on totals and one checkout CTA
- storefront cart/checkout/confirmation now also normalizes quantity, coupon, country-code, and controlled status-query inputs so public commerce flows do not trust raw browser values across redirects and form posts
- storefront confirmation now also reconciles hosted-checkout return/cancel flows through the canonical payment-completion endpoint instead of acting as a passive snapshot screen
- storefront confirmation now also keeps post-checkout guidance visible, including payment-next-step messaging, account/order-history follow-up, and stable order-reference handling
- storefront confirmation and auth-required follow-up links now also sanitize app-local return targets centrally, and confirmation status messaging derives from the authoritative confirmation snapshot instead of trusting query-carried status text
- protected member entry points now also render route-summary plus cross-surface follow-up panels instead of collapsing to a minimal sign-in block, so auth-required routes stay aligned with the wider portal/storefront orientation model
- cart, checkout, and confirmation now also share breadcrumb-style route orientation plus explicit cross-surface handoff cards, so the conversion chain behaves like one storefront subsystem instead of detached routes

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
- `DARWIN_WEB_SITE_URL`
  - default: `http://localhost:3000`
- `DARWIN_WEB_CULTURE`
  - default: `de-DE`
- `DARWIN_WEB_SUPPORTED_CULTURES`
  - default: `de-DE,en-US`
- `DARWIN_WEB_CULTURE_COOKIE_NAME`
  - default: `darwin-web-culture`

If the API or configured CMS menu is unavailable, the shell falls back to built-in navigation links so local development and builds still succeed.
That fallback is intentionally visible in the UI and should be treated as a degraded state, not as the normal operating path.
If `culture=<supported-culture>` is present in the query string, middleware persists it into the configured culture cookie and redirects back to the clean URL.
Public Home/CMS/catalog routes now also support locale-prefixed URLs such as `/en-US/catalog` through middleware rewrite. The sitemap still emits default-culture canonical URLs only, because true per-language detail-page inventory needs a backend slug-mapping contract before alternates can be generated safely across all CMS/product pages.
For the public index-level routes that already have unambiguous paths (`/`, `/cms`, `/catalog`), sitemap now emits the crawlable locale-prefixed variants as well. Detail-page sitemap inventory still stays on the default-culture canonical set until backend slug mapping exists.

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
    |-- commerce.en-US.json
    |-- home.de-DE.json
    |-- home.en-US.json
    |-- member.de-DE.json
    `-- member.en-US.json
```

This is the current front-office equivalent of the resource-first direction already being established in `Darwin.WebAdmin`; new languages should be introduced by adding bundles here and then wiring them into the runtime config.

## Current Implemented Slice

The current web slice includes:

- theme-isolated storefront shell
- CMS-first `main-navigation` consumption with observable fallback
- Home built through reusable hero/card web parts
- Home now also uses a dedicated stat-grid web part with live CMS/catalog/runtime metrics instead of relying only on hero and card grids
- Home now also uses a dedicated journey/link-list web part to keep CMS, catalog, and account entry flows visible as one system-level composition
- public CMS listing and CMS slug routes against live `Darwin.WebApi` content endpoints
- CMS index now also exposes a visible-result search lens over the pages already loaded on the current page, while staying explicit that true CMS search still needs a backend contract
- CMS index now also surfaces current-window result summaries for visible vs loaded vs total published pages, so public content browsing stays set-aware instead of behaving like a flat card dump
- CMS index now also groups the visible page set by title initials with quick-jump anchors, so public content browsing reads like an oriented set instead of one undifferentiated card wall
- CMS index now also derives a spotlight-plus-follow-up reading rail from the current visible result window, so the public content surface offers a guided reading path without inventing a richer CMS contract
- CMS index now also exposes explicit cross-surface handoff cards into Home, Catalog, and Account, so public content consumption stays connected to the broader storefront system
- CMS index pagination now also exposes first/last page jumps while preserving the current page-local lens, so public CMS window navigation is more complete without claiming backend search support
- CMS detail rendering with route metadata, related-page navigation, storefront follow-up panels, and visible degraded-state behavior
- CMS detail now also exposes previous/next page adjacency plus account-aware follow-up CTAs, using the same published page list instead of requiring a dedicated navigation contract
- CMS detail now also exposes breadcrumb and published-set position context, using the current published page set instead of leaving long-form content detached from storefront orientation
- CMS index empty-state handling now also keeps Home/Catalog follow-up actions visible instead of ending in a dead-end empty panel
- CMS detail now also derives section navigation anchors plus reading/structure metrics from the published HTML itself, so long-form content is no longer rendered as a single opaque block
- CMS detail anchor ids now also normalize diacritics before slugging section headings, so long-form German content keeps stable in-page navigation instead of collapsing into weak or duplicate fallback ids
- CMS detail unavailable state now also keeps cross-surface follow-up visible, and CMS/catalog detail/index routes now surface explicit route-summary diagnostics so degraded public content/discovery states do not collapse into passive leaves
- public catalog browsing against live `Darwin.WebApi` category/product endpoints
- page-local visible-result search/sort controls on `/catalog` that preserve category/page context without pretending the current API already supports true cross-catalog search or facets
- cart empty state, checkout unavailable state, follow-up-products unavailable state, and confirmation/cart/checkout route summaries now keep the commerce flow observable and actionable instead of collapsing into passive no-data states
- public product-detail route against the product-by-slug endpoint
- public cart page plus add/update/remove flows against public cart endpoints with stable anonymous cart identity
- cart follow-up products and next-step guidance using the canonical public catalog contract instead of a fake recommendation-only API
- public checkout page with server-rendered address capture, live checkout intent preview, shipping selection, and order placement
- public order-confirmation route with payment-handoff retry against the storefront confirmation and payment-intent endpoints
- hosted-checkout return/cancel reconciliation through the storefront payment-completion endpoint plus a short-lived web-owned handoff cookie
- post-checkout guidance on the confirmation route for payment attention, member-portal follow-up, and stable order-reference handling instead of a receipt-only end state
- public account self-service foundation for member registration, activation email request/confirm, and password reset request/complete flows
- stronger public auth-form guardrails plus email canonicalization in registration/activation/password/sign-in actions
- account self-service return-path preservation across register, activation, password recovery, and sign-in so storefront/member entry points can carry the intended app-local destination through the public auth flows
- provisional browser sign-in plus authenticated member dashboard, order history/detail, invoice history/detail, and loyalty overview
- resource-backed public account auth, profile/preferences/addresses, signed-in dashboard shell, member orders/invoices, and loyalty surfaces including overview/discovery/public detail/business detail through JSON bundles under `src/localization/resources`
- shared member-portal navigation across dashboard/profile/preferences/addresses plus stronger profile/address form guardrails for the authenticated account area
- shared member-portal navigation now also covers orders, invoices, loyalty overview, and commerce detail sidebars instead of stopping at account editors
- profile/preferences/addresses plus order/invoice/loyalty-business detail routes now also carry the same breadcrumb-style route orientation and cross-surface handoff actions as the overview routes, so the authenticated portal stays coherent on edit/detail pages as well
- dashboard/preferences sidebars now also surface route-summary/follow-up context, and address/order/invoice empty states now keep the member inside actionable front-office paths instead of passive dead ends
- editable profile, preferences, and member address-book flows against the canonical member profile endpoints
- protected member fetches now refresh provisional browser sessions near expiry and retry once before falling back to sign-in again
- phone verification request/confirm inside the profile surface via the canonical SMS/WhatsApp member verification endpoints
- business-scoped loyalty detail routes with rewards, recent transactions, and cursor-based timeline paging
- loyalty overview/discovery/public-detail/business-detail surfaces now also carry route-summary diagnostics and non-passive empty/degraded follow-up actions, keeping the member/visitor inside the broader storefront flow
- config-driven `de-DE`/`en-US` shell switching with locale-aware currency/date formatting across catalog, cart, checkout, orders, invoices, and loyalty pages
- resource-backed shared shell/catalog/storefront-commerce copy through JSON bundles under `src/localization/resources`
- culture-aware route metadata plus key-based localized flash/error messaging for web-owned cart/checkout/member action flows
- loyalty overview cards backed by `my/businesses` plus business-scoped promotions feed/tracking through the canonical member loyalty contracts
- public loyalty discovery against the canonical business-discovery contracts plus pre-join business detail/join UX on `/loyalty` and `/loyalty/[businessId]`
- query-driven loyalty proximity browsing with a server-rendered coordinate preview derived from the same public discovery result set
- browser-side loyalty scan preparation for accrual/redemption with branch selection, reward selection, and short-lived token visibility on `/loyalty/[businessId]`
- QR rendering for the active prepared loyalty scan token on `/loyalty/[businessId]`
- loyalty business detail now also stays on the shared member-portal navigation model and clears stale/mismatched prepared scan-token cookie state during readback
- culture-aware catalog delivery plus contract-safe merchandising polish across `/catalog` and `/catalog/[slug]`
- category-aware related-product follow-up on `/catalog/[slug]` using the current public catalog contracts
- product detail now also surfaces degraded related-product follow-up state explicitly when the category-based follow-up fetch fails, instead of silently flattening adjacent catalog discovery
- member commerce detail hardening for order/invoice documents, shipment/payment snapshots, and linked invoice/order follow-up
- public cart coupon apply/clear plus richer tax/shipping/billing presentation across cart and checkout summary surfaces
- checkout now also exposes readiness signals and a live order-review panel so the shopper can validate address/shipping/line items before order placement
- public commerce hardening now also normalizes quantity/coupon/country-code input and constrains cart/confirmation status-query handling so storefront redirect/form glue stays explicit and controlled
- centralized SEO metadata shaping with canonical/Open Graph/Twitter support for public routes and explicit `noindex` policy for private/mixed portal routes
- public `robots.txt` and `sitemap.xml` generation backed by the live CMS/catalog contracts and limited to truly public/indexable storefront paths
- locale-prefixed public routing for Home/CMS/catalog plus index-level language alternates where slug mapping is unambiguous

For broader platform documentation, see:

- [`../../README.md`](../../README.md)
- [`../../DarwinFrontEnd.md`](../../DarwinFrontEnd.md)
- [`../../DarwinWebApi.md`](../../DarwinWebApi.md)
