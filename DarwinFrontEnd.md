# Darwin Front-End Guide

[![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![TailwindCSS](https://img.shields.io/badge/TailwindCSS-4-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
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
- `Execution can start`: the public/member `Darwin.WebApi` surface is now broad enough to support a real first slice instead of only speculative planning
- `Current codebase state`: `src/Darwin.Web` has already moved beyond the raw starter into a real storefront/member-portal shell with live public and member API consumption
- `Dependency-heavy`: it still depends on `Darwin.WebApi`, core backend rules, and operational readiness from `Darwin.WebAdmin`

The previous platform priority was to finish `WebAdmin` and the admin/backend capabilities needed for real operations and onboarding first.
That work has now produced enough real public/member support that `Darwin.Web` can start with a narrow, contract-first slice.

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

## 2.2 Actual Starting Point in `src/Darwin.Web`

The repository state matters as much as the platform state.

The repository started as:

- a bare Next.js app-router project
- on Next.js 16, React 19, TypeScript, and Tailwind CSS 4
- showing the default starter page and starter metadata
- not yet organized into storefront features, API clients, page composition, or theme isolation

Practical consequence:

- the first implementation pass needed to establish a reusable front-office shell, theme boundary, and API-consumption pattern before deeper slices could be added safely

That foundation is now in place, so the next slices should build on the existing shell and feature boundaries instead of reworking the app from scratch.

### Current implementation snapshot

The first web slice is now in place:

- a theme-isolated storefront shell now exists in `Darwin.Web`
- the shell uses a Cartzilla-inspired visual direction without coupling feature logic to one theme
- primary navigation now attempts to load from the public CMS menu contract and falls back to app-defined links when the API or seed data is unavailable
- the home page now uses reusable hero/card web parts and can surface live CMS/catalog spotlight data without coupling page structure to one theme
- the home page now also includes a stat-grid web part plus part-owned hero aside copy, so home composition is richer without pushing home-specific wording down into the generic composer
- the home page now also includes a dedicated journey/link-list web part, so CMS, catalog, and account entry flows remain visible as one composed front-office system instead of only a flat shortcut grid
- home metrics/highlights now also expose CMS/catalog/category contract health directly inside the composer so degraded public content does not hide behind static counts
- the home page now also includes a reusable status-list web part, so contract-backed CMS/catalog/account lanes remain actionable and theme-independent instead of collapsing into one-off home markup
- the home page now also includes a reusable stage-flow web part, so Home communicates a staged storefront journey across CMS, catalog, and member follow-up instead of a loose collection of route teasers
- the home page now also includes a reusable pair-panel web part, so CMS and catalog can be presented as coordinated storefront surfaces instead of only independent spotlight cards
- the home page now also includes a reusable agenda-columns web part, so content, commerce, and member follow-up can be rendered as parallel front-office streams instead of only linear or pairwise sections
- route scaffolding now exists for catalog, account, loyalty, orders, and invoices so later slices can bind feature data without reworking the shell

The next storefront slice is now also underway:

- the catalog listing page now consumes public category and product contracts from `Darwin.WebApi`
- product-detail routing now consumes the public product-by-slug contract
- a public CMS index route now consumes the published CMS page list contract
- public CMS list/detail delivery now also follows the active request culture instead of relying on backend default-language behavior
- the CMS index now also offers a visible-result search lens over the already loaded page set, but keeps that state explicit and non-canonical until a real public CMS search contract exists
- the CMS index now also surfaces visible-vs-loaded-vs-total result summaries, so the public content route stays aware of the current published set instead of acting like an unbounded card grid
- the CMS index now also groups the visible page set by title initials with quick-jump anchors, so public content browsing feels like an oriented set without inventing a separate CMS taxonomy contract
- the CMS index now also derives a spotlight-plus-follow-up rail from the current visible result window, so the route can guide storefront reading without pretending a richer CMS curation contract already exists
- the CMS index now also exposes explicit handoff cards into Home, Catalog, and Account, so public CMS browsing stays connected to the broader front-office system instead of ending in content-only loops
- the CMS index pagination now also exposes first/last jumps while preserving the page-local visible-result lens, so public content window navigation is more complete without implying real backend search
- CMS index empty states now also keep Home/Catalog follow-up actions visible instead of leaving the public content surface at a dead end
- CMS detail pages now expose route metadata, related-page navigation, and visible degraded-state handling instead of collapsing non-404 failures into a not-found route
- CMS detail now also derives previous/next adjacency from the published page list and keeps Home/Catalog/Account follow-up explicit, so long-form content does not end in a leaf route
- CMS detail now also exposes breadcrumb and published-set position context, so long-form content remains oriented inside the broader public CMS set rather than feeling detached from storefront navigation
- CMS detail now also derives on-page section navigation and reading/structure metrics from the published HTML itself, so rich content remains navigable without needing a separate CMS-specific page-layout contract first
- CMS detail anchor ids now also normalize diacritics before slugging section headings, so German and other accented headings keep stable in-page navigation ids instead of collapsing into weak fallback slugs
- a public cart route now consumes the public cart contract and supports server-side add/update/remove flows for anonymous storefront usage
- the cart route now also reuses the public catalog contract for a small continue-shopping follow-up rail plus explicit cart next-step guidance, instead of leaving the shopper only with totals and checkout CTA
- a public checkout route now consumes checkout-intent and order-placement contracts with inline address capture and shipping selection
- the checkout UI now also keeps readiness signals and live line-item review visible, so storefront conversion no longer depends only on totals plus the address form
- a public confirmation route now consumes storefront confirmation data and exposes payment-handoff retry through the payment-intent contract
- the confirmation route now also keeps post-checkout guidance explicit, including payment-next-step messaging, account/order-history follow-up, and stable order-reference handling instead of a passive receipt-only view
- confirmation and auth-required follow-up links now also sanitize app-local return targets centrally, and the confirmation UI derives displayed order/payment status from the authoritative confirmation snapshot instead of trusting query-carried status text
- auth-required member entry points now also expose route-summary plus cross-surface follow-up panels, so protected routes do not regress to a minimal access wall while the rest of the portal carries richer orientation and handoff UX
- cart, checkout, and confirmation now also share breadcrumb-style route orientation plus explicit cross-surface handoff cards, so the conversion chain stays connected to the wider storefront system instead of behaving like detached transactional pages
- degraded-mode storefront data states are now visible in the UI instead of silently collapsing into placeholder-only behavior
- catalog list/detail now pass the active request culture into the canonical public catalog endpoints, so storefront localization no longer depends on the backend default culture leaking through
- catalog merchandising polish now stays within the real public contract set: selected-category context, compare-at savings badges, and category-linked navigation were added without pretending search/facets/sort already exist
- `/catalog` now also includes a page-local visible-result search/sort lens, but it stays explicitly limited to the already loaded server page and intentionally does not pretend to be a scalable cross-catalog search experience
- `/catalog` now also surfaces visible-vs-loaded-vs-total result summaries plus first/last page jumps, so catalog window navigation is more complete without implying true backend search/facets support
- product detail now also reuses the product's primary category plus the existing category-product listing contract to show related products without inventing a recommendation-specific API
- product detail now also surfaces degraded related-product follow-up state explicitly when that category-based follow-up fetch fails, instead of silently flattening adjacent catalog discovery
- product detail now also exposes breadcrumb, product-reference snapshot, and explicit storefront handoff actions so the conversion route stays oriented inside the broader front-office system

The first account self-service foundation is now also in place:

- `/account` is now a real self-service hub instead of a pure placeholder
- public registration now runs through the member auth register endpoint
- activation email request and token confirmation now run through the member auth activation endpoints
- password reset request and completion now run through the member auth reset endpoints
- public registration, activation, password reset, and sign-in now also normalize email input plus stronger required/autocomplete/password guardrails so avoidable auth-flow mismatches are reduced before the canonical API call
- browser sign-in persistence, member profile editing, addresses, and the authenticated portal remain a distinct follow-up slice because the final browser auth/session transport is still an explicit platform decision

The next member-portal slice is now also in place:

- `Darwin.Web` now has a provisional browser session layer that stores member session state in web-owned cookies rather than exposing raw tokens in the UI
- the provisional browser session layer now refreshes member access tokens near expiry and retries protected member fetches once before forcing a new sign-in
- the authenticated account route now renders profile, preference, and linked CRM context snapshots from the member API surface
- the dashboard, profile, preferences, and address-book surfaces now also share explicit member-portal navigation, so the protected account area behaves like one front-office subsystem instead of separate editor pages
- editable member profile and communication/account preferences now run through the canonical member profile endpoints
- reusable member address-book create/update/delete/default flows now run through the canonical member address endpoints
- profile phone-verification and address forms now also carry stronger client-side input/autocomplete guardrails so avoidable member-data mistakes are caught before the canonical API call
- phone verification request/confirm now runs inside the profile surface through the canonical SMS/WhatsApp verification endpoints and shared profile confirmation flag
- orders and invoices now render authenticated history pages plus detail routes with payment-retry handoff
- unavailable order/invoice detail states now also keep explicit follow-up actions to account/catalog and their parent history routes instead of collapsing into warning-only dead ends
- loyalty now renders the authenticated overview instead of remaining a placeholder page
- member orders/invoices/loyalty now also follow the same shared member-portal navigation model as profile/preferences/addresses, so the authenticated portal behaves more like one subsystem than a set of detached routes
- dashboard, orders, invoices, and loyalty overview now also expose breadcrumb-style route orientation plus cross-surface handoff actions, so the authenticated member area stays connected to the wider front-office system instead of behaving like a sealed portal island
- profile/preferences/addresses plus order/invoice/loyalty-business detail routes now also expose the same breadcrumb-style route orientation and cross-surface handoff actions, so editor/detail screens stay inside the same front-office navigation model instead of collapsing back to isolated portal pages
- dashboard and preferences now also surface explicit route-summary/follow-up panels, and address/order/invoice empty states now keep actionable catalog/account follow-up instead of ending in passive placeholder blocks
- loyalty business detail routes now render business-scoped dashboard, rewards, and cursor-paged timeline data from the member contracts
- loyalty business detail now also follows the shared member-portal navigation model, and browser-prepared scan-token state now clears stale/mismatched cookie state instead of silently drifting
- loyalty overview, discovery, public business detail, and signed-in business detail now also expose explicit route-summary panels plus actionable empty/degraded follow-up CTAs, so loyalty routes stay observable and navigable instead of degrading into passive no-data states
- this remains an implementation boundary, not a permanent architecture verdict; deeper BFF/session hardening can still replace the current web-owned cookie wrapper later

The next localization/config-driven slice is now also in place:

- `Darwin.Web` now treats `de-DE` and `en-US` as the current supported front-office cultures and resolves the active culture through config plus a web-owned culture cookie
- query-string culture switching now lands through middleware so `?culture=de-DE|en-US` becomes a persisted preference instead of an ad hoc page-only toggle
- shell fallback navigation/footer copy now follows the active culture instead of staying hardcoded in one language
- shared shell/catalog/storefront-commerce wording plus account-edit/member-commerce and loyalty discovery/public-detail wording now runs through resource bundles under `src/Darwin.Web/src/localization/resources`, aligned with the same de/en-first additive strategy already being established across WebAdmin and mobile
- route-level metadata for shell, Home, CMS, catalog, checkout, account, orders, invoices, and loyalty now also resolves through the same culture-aware resource path instead of static page-level title/description literals
- `Darwin.Web` now also centralizes canonical, Open Graph, Twitter, and `noindex` policy in `src/lib/seo.ts`, driven by the configured `DARWIN_WEB_SITE_URL` instead of ad hoc route metadata shaping
- `Darwin.Web` now also exposes `robots.txt` and `sitemap.xml`, with sitemap entries derived from live public CMS/catalog contracts rather than a hand-maintained static URL list
- public Home/CMS/catalog routes now also support locale-prefixed URLs through middleware rewrite plus request-level culture headers, instead of relying only on cookie/query-based culture switching for indexable pages
- shell navigation, Home web parts, CMS browsing, and catalog browsing now also emit those locale-aware public links directly through a shared locale-routing helper, so non-default cultures do not depend on redirect-only navigation
- member sign-in, account self-service return paths, loyalty action return paths, payment failure redirects, and storefront cart display snapshots now also sanitize app-local paths before redirecting or persisting UI links, so the front-office does not trust arbitrary user-supplied redirect targets
- catalog, CMS, and loyalty public search/discovery routes now also normalize page/text/numeric search params through shared helpers and submit through locale-aware form actions, so public server-rendered filters do not drift across surfaces
- local web-owned validation and flash messages for register/activation/password/sign-in, cart/checkout, payment retry, profile/preferences/addresses, and loyalty join/scan/promotion flows now travel as localization keys and resolve inside the UI resource bundles instead of staying as server-action English fallbacks
- generic public/member API fallback messages plus CMS index/detail degraded-mode copy now also resolve through resources, so network/not-found/http fallback states no longer depend on English-only client literals
- CMS detail unavailable state plus catalog index/detail now also expose route-summary diagnostics and actionable no-result/unavailable follow-up CTAs, so degraded public storefront routes stay observable and navigable instead of collapsing into passive warning blocks
- cart, checkout, and confirmation now also expose route-summary diagnostics plus stronger empty/unavailable follow-up CTAs, so the conversion path stays observable and actionable instead of degrading into passive panels
- money and date formatting across catalog, cart, checkout, orders, invoices, and loyalty now follow the active request culture instead of assuming `de-DE`
- profile locale editing now follows the supported-cultures config instead of accepting an unrestricted free-text locale
- multilingual CMS/content operations still remain a separate platform dependency; the current slice only makes the web runtime localization-ready and aligned with the shared de/en baseline
- public account auth, the signed-in dashboard shell, profile/preferences/addresses, member orders/invoices, and loyalty surfaces including overview, discovery, public detail, and signed-in business detail now also consume resource-backed copy instead of app-local text blocks

The next loyalty engagement slice is now also in place:

- `/loyalty` now consumes both the aggregate overview and the richer `my/businesses` contract so joined loyalty places render with business image/category/city context instead of only flat account summaries
- `/loyalty/[businessId]` now consumes personalized promotions for the selected business in addition to dashboard, rewards, and timeline data
- promotion CTA interactions now post through the canonical member promotions tracking endpoint instead of inventing a web-local engagement event path

The next loyalty discovery slice is now also in place:

- `/loyalty` now also consumes public business discovery plus category-kinds metadata, so loyalty-ready businesses remain browseable before the member signs in or joins a business
- `/loyalty/[businessId]` now uses canonical public business detail as the pre-join experience for anonymous or not-yet-joined members instead of collapsing immediately into an auth-only route
- direct join from `/loyalty/[businessId]` now posts through the canonical member loyalty join contract and can optionally pass a preferred business location
- member-only balances, promotions, and timeline data remain behind the authenticated portal, so public discovery does not fork the loyalty contract model away from mobile/member usage
- `/loyalty` now also supports query-driven latitude/longitude/radius proximity filtering and a server-rendered coordinate preview driven by the same public discovery result set
- `Darwin.Web` deliberately does not consume `public/businesses/map` yet for the loyalty surface, because that contract currently lacks a loyalty-active filter and could mix non-loyalty businesses into a loyalty-only discovery page

The next loyalty scan-preparation slice is now also in place:

- `/loyalty/[businessId]` now prepares canonical browser-side scan sessions for accrual or redemption through `POST /api/v1/member/loyalty/scan/prepare`
- branch selection and redeemable reward selection now feed directly into that canonical member contract instead of relying on a web-local scan model
- the returned short-lived scan token is intentionally kept in a short-lived web-owned cookie and rendered back on the page, rather than being exposed in the URL
- the active prepared token now also renders as a real QR image on `/loyalty/[businessId]`, making the browser-prepared flow directly usable for staff-side scanning
- camera/scanner-specific browser flows still remain later-phase; the current slice focuses on making the shared scan-preparation contract usable from the web portal without inventing a separate web token model

The next member-commerce hardening slice is now also in place:

- order detail now renders canonical member payment attempts, shipment snapshots, linked invoice summaries, and direct document download against the member order contract
- invoice detail now renders canonical payment summary plus direct document download against the member invoice contract
- document download links now resolve against the configured `Darwin.WebApi` base URL instead of incorrectly treating API contract paths as internal Next.js routes

The next storefront-commerce hardening slice is now also in place:

- the public cart now consumes the canonical coupon apply/clear contract instead of ignoring available promotion/billing adjustments
- cart line presentation now shows unit net, add-on delta, VAT rate, line net, VAT, and line gross from the canonical cart snapshot instead of only a single gross total
- checkout summary now surfaces coupon state plus shipment-mass/shipping-country context from the live checkout intent so DHL-first and tax presentation are less opaque during storefront review
- storefront confirmation now finalizes hosted-checkout return and cancellation through the canonical payment-completion endpoint instead of only showing a post-redirect snapshot
- cart quantity/coupon handling plus checkout country-code and cart/confirmation status-query handling now also normalize and constrain browser-provided values instead of trusting raw redirect/form input
- a short-lived web-owned payment handoff cookie now exists purely to bridge the PSP return into canonical completion; the order/payment status still comes from `Darwin.WebApi`

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
- if the web needs true storefront search, facets, or server-side sorting, those capabilities must land as explicit public catalog contracts rather than being reconstructed from one page of product cards

## 4.2 Confirmed WebApi Surfaces That Are Already Real

The following routes are not just planned in docs; they are present in `Darwin.WebApi` and suitable for `Darwin.Web` planning:

- public CMS:
  - `GET /api/v1/public/cms/pages`
  - `GET /api/v1/public/cms/pages/{slug}`
  - `GET /api/v1/public/cms/menus/{name}`
- public catalog:
  - `GET /api/v1/public/catalog/categories`
  - `GET /api/v1/public/catalog/products`
  - `GET /api/v1/public/catalog/products/{slug}`
- public storefront commerce:
  - `GET /api/v1/public/cart`
  - `POST /api/v1/public/cart/items`
  - `PUT /api/v1/public/cart/items`
  - `DELETE /api/v1/public/cart/items`
  - `POST /api/v1/public/cart/coupon`
  - `POST /api/v1/public/shipping/rates`
  - `POST /api/v1/public/checkout/intent`
  - `POST /api/v1/public/checkout/orders`
  - `POST /api/v1/public/checkout/orders/{orderId}/payment-intent`
  - `POST /api/v1/public/checkout/orders/{orderId}/payments/{paymentId}/complete`
  - `GET /api/v1/public/checkout/orders/{orderId}/confirmation`
- member auth/account:
  - `POST /api/v1/member/auth/login`
  - `POST /api/v1/member/auth/register`
  - `POST /api/v1/member/auth/email/request-confirmation`
  - `POST /api/v1/member/auth/email/confirm`
  - `POST /api/v1/member/auth/password/request-reset`
  - `POST /api/v1/member/auth/password/reset`
  - `GET|PUT /api/v1/member/profile/me`
  - `GET|PUT /api/v1/member/profile/preferences`
  - `GET|POST|PUT /api/v1/member/profile/addresses*`
  - `GET /api/v1/member/profile/customer`
  - `GET /api/v1/member/profile/customer/context`
- member commerce and loyalty:
  - `GET /api/v1/member/orders`
  - `GET /api/v1/member/orders/{id}`
  - `POST /api/v1/member/orders/{id}/payment-intent`
  - `GET /api/v1/member/invoices`
  - `GET /api/v1/member/invoices/{id}`
  - `POST /api/v1/member/invoices/{id}/payment-intent`
  - `GET /api/v1/member/loyalty/my/overview`
  - `GET /api/v1/member/loyalty/business/{businessId}/dashboard`
  - `GET /api/v1/member/loyalty/business/{businessId}/rewards`
  - `POST /api/v1/member/loyalty/my/timeline`
  - `POST /api/v1/member/loyalty/account/{businessId}/join`

## 4.3 Public Content Readiness For Storefront Work

The storefront should now assume the following content/operator dependencies are provided by backend and `Darwin.WebAdmin`, not invented inside `Darwin.Web`:

- a public CMS menu named exactly `main-navigation` now exists and is intended to be the primary navigation source for `Darwin.Web`
- representative catalog seed data now exists with real published categories and products, including localized slugs and a small set of primary product media attachments for storefront testing
- representative CMS seed pages now exist in a truly public state, not only as draft records

Current public publish rules are:

- categories are public when `IsActive && IsPublished`
- products are public when `IsActive && IsVisible` and their publish window allows visibility
- CMS pages are public when `IsPublished`, `Status == Published`, and the publish window allows visibility

Current contract implications for `Darwin.Web`:

- `main-navigation` should be fetched from the public CMS API and treated as the primary source of storefront navigation
- `/cms/[slug]` should be tested against real published slugs such as `ueber-uns` and `faq`
- catalog browsing should expect real product slugs and at least a representative set of products with primary images, but it must still tolerate `PrimaryImageUrl == null`
- product-detail media carries `alt`, `title`, and `role` when media exists
- category cards should not assume category-image support yet; category media is not part of the current public category contract

Missing-image policy for storefront design:

- do not hide a public product card because `PrimaryImageUrl` is null
- show a stable placeholder image or placeholder panel instead
- keep card height/layout stable when an image is missing
- use real `alt` text when product media exists; use a neutral placeholder label when it does not

Future navigation metadata:

- `badge`, `icon`, `featured grouping`, and similar menu metadata are not part of the current contract
- `Darwin.Web` must not depend on those fields yet
- if those affordances become necessary later, add them as an explicit public CMS contract change instead of building silent client assumptions

## 4.4 Responsibility Split: WebAdmin/Backend vs Darwin.Web

`Darwin.WebAdmin` and backend are responsible for:

- publishing and seeding baseline public navigation/content/catalog data
- keeping public publish rules explicit and stable
- exposing real operational support paths when storefront/member flows fail
- documenting current contract limits such as missing-image behavior and unsupported future metadata

`Darwin.Web` is responsible for:

- consuming the published CMS/catalog contracts through `Darwin.WebApi`
- rendering resilient fallback states when optional fields such as `PrimaryImageUrl` are absent
- keeping storefront UX aligned with documented public contracts instead of inventing private assumptions
- keeping anonymous storefront cart and checkout state coherent, including clearing stale web-owned cart cookies after successful order placement
- updating its own backlog based on these confirmed dependencies instead of re-planning backend work inside the front-end chat

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
- public Home/CMS/catalog routes now also emit centralized canonical/Open Graph/Twitter metadata, while private or mixed member routes intentionally stay `noindex` until a cleaner public/member URL split exists
- `robots.txt` and `sitemap.xml` should enumerate only public/indexable routes; public Home/CMS/catalog routes can now resolve under locale-prefixed URLs, but sitemap still stays on default-culture canonicals until backend slug mapping exists for safe detail-page alternates
- because `/`, `/cms`, and `/catalog` already have stable cross-language path shapes, sitemap can enumerate their locale-prefixed variants now; CMS/product detail inventory still remains default-culture-only until backend exposes safe localized slug linking

## 8.1 Theme and Page-Composition Strategy

Initial visual direction may borrow from the feel of the Cartzilla grocery theme:

- bright retail-forward shell
- strong navigation and merchandising hierarchy
- card-based catalog surfaces
- soft promotional blocks and content bands

But implementation must stay theme-independent.

Rules for the first `Darwin.Web` architecture pass:

- keep theme assets, tokens, and shell styling isolated from feature logic
- treat the initial Cartzilla-inspired look as one theme, not as the application architecture
- build page sections as reusable web parts / page components with explicit slots and composition order
- allow the home page to remain intentionally minimal while the shell, navigation, and composition model are established
- keep CMS/page composition ready for later server-driven placement without coupling current pages to one-off layouts

Recommended separation:

- theme layer:
  - tokens
  - typography
  - spacing
  - shell chrome
  - visual variants
- composition layer:
  - page templates
  - slot contracts
  - web parts / section components
- feature layer:
  - CMS
  - catalog
  - account
  - loyalty
  - orders
  - invoices

This is important because the initial theme choice is temporary, while the web feature model should survive future theme replacement.

### Current implementation note

The first slice now concretely includes:

- `themes/*` and a theme registry for the active storefront skin
- `web-parts/*` for page composition primitives
- a shared shell with header/footer/navigation
- CMS-backed menu loading with observable fallback behavior
- a minimal home page plus placeholder feature routes
- a live catalog listing and product-detail route bound to public catalog endpoints
- a live public cart route with server-side mutations against public cart endpoints

The next CMS/storefront slice should build on this foundation rather than replacing it.

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
- keep UI copy in resource files instead of letting feature components grow app-local text dictionaries
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
|-- themes/
|-- web-parts/
|-- lib/
|-- services/
`-- types/
```

Keep API-facing types and service abstractions clearly separated from UI components.
Also keep theme assets and page-composition primitives separate from feature slices so future theme swaps do not force feature rewrites.

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
5. prefer starting with:
   - theme-isolated storefront shell
   - CMS-backed navigation
   - placeholder home
   - route scaffolding for the main storefront/member areas

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
