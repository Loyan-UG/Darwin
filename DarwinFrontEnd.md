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
- primary navigation now also loads through a shared cached shell context with timing diagnostics, so repeated layout composition does less duplicate menu work while still making slow CMS-menu behavior visible
- shell menu normalization, safe-link filtering, and fallback messaging now also run through shared tested helpers, so the header keeps one consistent reaction when CMS navigation is empty, missing, or partially unsafe
- public account/auth storefront context now also loads through a shared cached and observable wrapper, so account hub plus sign-in/register/recovery entry points reuse the same cart/content/browse snapshot with less duplicate server work
- member identity and commerce summary contexts now also emit shared timing diagnostics and back account/cart/checkout/history routes from one observable snapshot layer, so protected member entry and follow-up paths behave more consistently under load
- profile, preferences, security, addresses, orders, and invoices now also consume those shared member summary contexts instead of route-local duplicate fetches, so protected self-service and history surfaces stay aligned on the same underlying member state
- active storefront-shopping continuity now also flows through a shared shopping context, so Home, account, orders, invoices, and member detail routes all read the same live-cart and cart-linked-product snapshot instead of rebuilding that state route by route
- cart-linked product detection now also runs through a shared tested helper, so next-buy and follow-up surfaces stay more consistent about which catalog items are already part of the active shopping flow
- public CMS, catalog, and account/auth entry routes now also read live cart state through that same shared shopping context, so browse, content, and auth-continuation surfaces keep one consistent view of active shopping continuity instead of fetching cart state separately
- Home plus the main account/orders/invoices detail and history routes now also reuse one shared public storefront context for content, browse, and live-cart continuity, so public/member entry surfaces react to the same storefront snapshot with less duplicate server work
- that shared public storefront context now also has focused automated validation for healthy and degraded cart/discovery merges, so regression in the public content+browse+cart snapshot is less likely to hide until route-level failures
- cart, checkout, confirmation, and the main account editor routes now also reuse that same shared public storefront context, so conversion and self-service entry surfaces assemble their content/browse continuity with less duplicate server work
- CMS and catalog routes now also project that shared storefront snapshot through canonical tested route-projection helpers, so cart summary plus content/browse follow-up props keep one stable shape instead of being rebuilt ad hoc per route
- account editor, protected history, and auth-entry routes now also consume those same canonical storefront projection helpers, so member/auth surfaces keep one stable content+browse+cart prop shape instead of repeating route-local storefront wiring
- shared observed loader helpers now also back the main storefront, CMS/catalog route-context, member-route, and Home-discovery assembly paths, so caching plus slow/failure diagnostics are wired through one canonical server pattern instead of being hand-built per route
- shell model assembly now also runs through a shared cached observable loader with explicit shell-model health summaries, so the main storefront chrome no longer keeps its own page-local diagnostics and caching branch in `layout`
- route diagnostics now also carry health summaries for content, browse, cart, and member readiness on the main storefront/member loaders, so slow or degraded route assembly is easier to trace without reopening each route’s internal fetch graph
- shared observed loaders now also emit degraded-success telemetry whenever a route or core loader returns non-`ok` health statuses even without being slow, so production/staging diagnostics can catch partial storefront/member degradation before it turns into outright failures
- cart, checkout, and confirmation now also load through shared cached observed route-context helpers with explicit commerce health summaries, so the main conversion flow reuses one canonical server assembly pattern instead of keeping route-local diagnostics and caching behavior
- member summary snapshots and browser shopping continuity now also load through shared observed helpers with explicit health summaries, so account/Home/commerce routes reuse one traceable baseline for identity, billing, and live-cart continuity instead of rebuilding those snapshots piecemeal
- member order and invoice history loaders now also emit canonical paged-collection health summaries, so protected history diagnostics can distinguish item-count, total-count, and current page-window state instead of only generic route success/failure
- member order and invoice detail loaders now also emit explicit detail-availability health alongside the broader protected route summary, so diagnostics can separate storefront continuation health from missing or degraded commerce detail payloads
- public CMS/catalog/menu GET calls now also reuse canonical cached fetches and normalized cache tags, so equivalent public discovery requests avoid more duplicate work and keep cache invalidation/coherence cleaner across storefront routes
- public GET fetches now also normalize their query-string cache keys before reuse, so equivalent CMS/catalog/menu requests dedupe at the fetch layer itself instead of only sharing invalidation tags after the fact
- public CMS and catalog discovery now also support real query search through the current WebApi contracts, so storefront browse is no longer limited to page-local text filtering on already loaded rows
- CMS and catalog search now also preserve review/readiness context across list and detail routes, so discovery and review workflows do not lose the active search window while drilling in
- catalog browse now also evaluates image-coverage and offer-strength facets on the full matching assortment before pagination, so review counts and quick windows stay aligned with the real browse set instead of only the current page
- CMS browse now also evaluates missing-title, missing-description, and missing-both metadata windows on the full matching published set before pagination, so review counts and quick debt windows stay aligned with the real content-review set instead of only the current page
- `/cms` now also exposes direct quick windows for missing title, missing description, and missing both metadata debt, so the main CMS route can jump straight into the right debt-review window without rebuilding metadata filters manually
- shared route observability now also emits explicit outcome kinds and timing bands such as `slow-success`, `degraded-success`, `slow-degraded-success`, and `failure`, so storefront/member diagnostics can tell healthy slowness apart from degraded slowness without reopening raw loader payloads
- product detail now also carries the active catalog facet window for image coverage and offer strength through detail and related-product follow-up, so drill-in review does not drop the current browse context
- CMS detail now also carries the active metadata-focus review window through previous/next navigation, review queues, and back-to-index links, so content drill-in does not drop the current debt-review context
- CMS detail now also builds previous/next navigation, review queues, and content-navigation lists from the active filtered review set instead of the raw published-page set, so metadata-debt drill-in stays aligned with the actual review window and not only the route query
- product detail now also builds its review queue from the active filtered browse set instead of only the related-products strip, so assortment drill-in stays aligned with the actual review window and not just with cross-sell suggestions
- product detail now also surfaces previous/next navigation and current position from the active filtered browse set, so assortment review can move step-by-step through the actual catalog window from the drilled-in route
- multilingual CMS/product detail alternates plus public sitemap inventory now also load through shared cached observable loaders, so detail-page `hreflang` and sitemap discovery reuse one canonical localized snapshot instead of rebuilding alternate maps route by route
- CMS and product detail SEO metadata now also load through shared cached observable loaders, so title/description/canonical/`hreflang` assembly no longer stays embedded in the route files for those core discovery pages
- CMS and catalog index SEO metadata now also load through shared cached observable loaders, so browse-page canonical/no-index handling no longer stays embedded in the route files for those core discovery pages
- Home plus the public account/auth entry routes now also load SEO metadata through shared cached observable loaders, so the main storefront and self-service entry points no longer keep route-local title/canonical/no-index assembly branches
- protected member routes plus the main commerce routes now also load SEO metadata through shared cached observable loaders, so portal and conversion pages no longer keep route-local no-index metadata assembly branches
- all main SEO metadata loaders for Home, public account/auth, protected member, commerce, CMS, and catalog routes now also reuse one shared SEO-loader helper with the same observed health-summary pattern, so title/canonical/no-index assembly no longer drifts across near-duplicate route-specific implementations
- runtime theming now also resolves the full registered multi-brand theme set through one canonical parser and registry-backed fallback, including `grocer`, `atelier`, `harbor`, `noir`, and `solstice`, so env parsing, shell composition, and theme registry no longer drift apart as the theme inventory grows
- member dashboard, account editor routes, orders/invoices lists, and member detail routes now also load through shared route-context helpers, so protected self-service and history/detail surfaces reuse one observable member/storefront assembly model instead of repeating Promise.all composition per route
- cart, checkout, and confirmation now also load through shared commerce route-context helpers, and cart-vs-purchase follow-up filtering now runs through shared tested helpers, so conversion routes keep one reusable assembly model for member/storefront context and next-buy continuity
- the home page now uses reusable hero/card web parts and can surface live CMS/catalog spotlight data without coupling page structure to one theme
- the home page now also includes a stat-grid web part plus part-owned hero aside copy, so home composition is richer without pushing home-specific wording down into the generic composer
- the home page now also includes a dedicated journey/link-list web part, so CMS, catalog, and account entry flows remain visible as one composed front-office system instead of only a flat shortcut grid
- home metrics/highlights now also expose CMS/catalog/category contract health directly inside the composer so degraded public content does not hide behind static counts
- the home page now also includes a reusable status-list web part, so contract-backed CMS/catalog/account lanes remain actionable and theme-independent instead of collapsing into one-off home markup
- the home page now also includes a reusable stage-flow web part, so Home communicates a staged storefront journey across CMS, catalog, and member follow-up instead of a loose collection of route teasers
- the home page now also includes a reusable pair-panel web part, so CMS and catalog can be presented as coordinated storefront surfaces instead of only independent spotlight cards
- the home page now also includes a reusable agenda-columns web part, so content, commerce, and member follow-up can be rendered as parallel front-office streams instead of only linear or pairwise sections
- the home page now also includes a recovery/follow-up rail driven by live CMS/catalog health, so degraded public entry still guides the visitor toward CMS, catalog, and account paths instead of stopping at shell chrome
- the home page now also includes a reusable route-map web part, so Home can hand off directly into real CMS page detail, real product detail, and account/loyalty follow-up routes instead of only section-level teasers
- the home page now also exposes category-driven storefront lanes by reusing the public category list plus category-filtered product contracts, so top-level browse entry points are data-backed instead of generic catalog shortcuts
- the home page now also includes a live priority lane that ranks checkout, billing, loyalty, order, CMS, and catalog follow-up from current public/member signals, so the storefront entry route can surface the most actionable next move instead of only section teasers
- the home page now also picks the strongest visible product opportunity instead of the first catalog card, so storefront entry surfaces a clearer best-offer moment from live browse data
- the home page now also surfaces a live offer board that ranks multiple visible catalog opportunities by savings strength, so storefront entry can show several concrete next-buy options instead of one product suggestion alone
- the home page now also surfaces a live category-campaign board built from visible category lanes plus category-anchoring products, so storefront entry can sell through stronger browse narratives instead of only isolated offer cards
- storefront runtime config now also supports app-configured theme selection between a grocer-style default and an atelier-style variant through `DARWIN_WEB_THEME`, so the same front-office system can ship more than one real visual direction without changing feature code
- storefront runtime config now also supports a third `harbor` visual direction through `DARWIN_WEB_THEME`, so the same front-office system can ship grocery, editorial, and cooler hospitality-style brand expressions without changing feature code
- Darwin.Web now also includes a lightweight unit-test runner plus focused coverage for query serialization, checkout draft/input normalization, HTML fragment sanitization, safe WebApi URL resolution, and locale-routing guardrails, so critical storefront handoff and trust-boundary logic is no longer validated only through manual browsing and full builds
- Darwin.Web now also covers the page-local catalog/CMS browse lenses with focused unit tests, so visible assortment/discovery-state behavior is regression-safe instead of staying embedded only inside route composition
- Darwin.Web now also emits shared API diagnostics for public, member, and auth fetch failures, including response-level request identifiers where available, so staging/production troubleshooting is less dependent on route-local guesswork
- route-observability now also carries route/context metadata such as culture, page, slug, category, order id, and menu name across shared storefront/member loaders, so slow-operation and failure diagnostics are much easier to trace back to the exact front-office workflow in staging and production
- route-observation metadata is now also generated through shared tested builders, so storefront/member diagnostics keep one stable shape across shell, discovery, checkout, and protected routes instead of drifting via per-loader ad hoc objects
- shared API diagnostics now also classify public/member/auth failures into operational kinds such as `network-error`, `unauthorized`, `not-found`, `http-error`, and `invalid-payload`, together with retryability and status-family hints, so storefront and portal troubleshooting can distinguish transport, contract, and access issues much faster
- catalog browse core, CMS browse core, Home core/storefront spotlight loaders, and confirmation-result loading now also emit dedicated success summaries, so diagnostics can trace discovery-core and post-purchase-core health before route-level composition is inspected
- product detail core, product related-products loading, and CMS detail core now also load through the same shared observed-loader path with dedicated success summaries, so detail discovery no longer depends on the older manual cache-plus-observation branch
- Home route assembly plus public account/auth entry routes now also load through shared observable route-context helpers, so storefront entry and self-service entry no longer keep route-local diagnostics branches around shared storefront context assembly
- public sign-in, register, activation, and password routes now also reuse one shared public-auth page-context helper for storefront continuation assembly, so self-service entry pages no longer drift in how they build live cart/content follow-up
- `/account` now also assembles through a shared observable page-context loader that chooses the public hub or member dashboard path centrally, so the main account entry route no longer keeps its public/member split as page-local server wiring
- Home and `/account` now also project their final page props through shared entry-view helpers, so storefront entry and the main account entry no longer keep route-local public/member prop assembly or return-path sanitation branches inside the page files
- protected member entry routes for account editor, orders/invoices, and order/invoice detail now also share one observable auth-gate context, so session check plus storefront fallback no longer drift route by route across the protected portal
- protected account editor, orders/invoices, and order/invoice detail pages now also assemble through shared observable page-context loaders, so the protected portal no longer repeats page-level auth gate plus route-context wiring in each route file
- protected member editor, history, and detail pages now also reuse one shared protected page-loader helper on top of the entry gate, so session-aware member fallback and page assembly no longer drift across orders, invoices, and self-service routes
- public CMS and catalog list/detail pages now also assemble through shared observable page-context loaders, so discovery routes no longer repeat page-level route-context plus continuation wiring in each page file
- public CMS and catalog list/detail pages now also reuse one shared public-discovery page-loader helper for continuation slices and route assembly, so content and commerce browse surfaces no longer drift in how they build live storefront follow-up
- cart, checkout, and confirmation now also assemble through shared observable page-context loaders, so the main conversion routes no longer repeat page-level commerce route-context plus follow-up wiring in each route file
- cart, checkout, and confirmation now also reuse one shared commerce page-loader helper for follow-up assembly and page health wiring, so the conversion flow no longer drifts in how it builds live shopping and post-purchase context
- public CMS/navigation and catalog discovery now also run through differentiated cache windows plus cache tags instead of one generic revalidation setting, so storefront performance tuning can follow content volatility more closely
- account entry, public auth continuation, and member history routes now also reuse a shared cached storefront-continuation context for CMS/category/product follow-up data, so repeated cross-surface discovery windows assemble with less duplicate server work
- member commerce detail routes plus account editor routes now also reuse that shared cached storefront-continuation context, so protected continuation windows stay consistent while doing less duplicate server work across the portal
- cart, checkout, and confirmation now also reuse that shared cached storefront-continuation context for CMS/category/product continuity data, so the conversion path does less duplicate server work while keeping the same discovery windows alive
- CMS and catalog browse routes now also reuse that shared cached storefront-continuation context for their secondary content/commerce windows, so public discovery surfaces do less duplicate server work while keeping live follow-up windows intact
- Home, account, cart, checkout, confirmation, and shared storefront-continuation loading now also emit slow-operation timing diagnostics, so real route-composition bottlenecks are easier to spot in staging and production
- CMS, catalog, orders, and invoices now also emit slow-operation timing diagnostics, so browse and portal bottlenecks are visible across the wider front-office instead of only the main entry and conversion routes
- CMS detail, product detail, and the main browse/history routes now also emit route-level timing diagnostics for heavier follow-up fetches, so content, discovery, and portal slowdowns are easier to pinpoint before they become customer-facing regressions
- order/invoice detail plus account editor routes now also emit route-level timing diagnostics, so protected member follow-up and self-service slowdowns are visible across the portal instead of hiding behind server-side composition
- member order and invoice history now also support a page-local visible-result review lens, so users can narrow the currently loaded history window by query or state without pretending real backend search already exists
- member order and invoice detail routes now also expose explicit operational timelines, so the main payment, shipment, delivery, due-date, and settlement milestones stay visible on the detail routes themselves
- the member dashboard now also exposes an explicit commerce-readiness layer, so operational order attention and open billing exposure are visible before users drill into orders or invoices
- catalog index and product detail now also surface a shared browse-campaign window built from visible category lanes plus strongest product offers, so catalog routes can drive stronger browse and buying decisions instead of only listing products
- that home offer board now also shifts away from products already linked to the active cart when browser storefront-shopping state exists, so storefront entry can suggest the next buying move beyond the current basket instead of echoing the same cart
- the home page now also includes a live commerce-opportunity window driven by cart, spotlight-product, and category-lane signals, so storefront entry can emphasize the strongest immediate buying move instead of only generic browse entry points
- the home page now also includes an explicit browse-readiness window for CMS and catalog, so storefront entry can expose public discovery debt before the visitor has to infer it from deeper browse routes
- the home page now also includes a review cockpit for CMS and catalog, so storefront entry can reopen the strongest visible review windows and jump directly to the next concrete page/product targets without rebuilding browse state first
- the home page is now also session-aware for signed-in members and can surface direct re-entry routes for account, orders, and loyalty instead of behaving like a purely anonymous storefront entry
- the home page is now also cart-aware and can surface direct cart/checkout recovery from browser-owned storefront snapshots, so storefront entry can resume active shopping flows instead of only restarting browse
- the home page is now also live-cart-aware and can surface current cart totals plus checkout continuity from the canonical public cart contract when a storefront cart already exists
- the home page now also enriches signed-in member resume with recent orders plus reward-focus data from the canonical member contracts, so storefront entry can resume real protected-context follow-up instead of only linking back to the generic account landing route
- the home page now also surfaces invoice follow-up inside the signed-in member resume, so storefront entry can hand off directly into outstanding billing context instead of forcing a second hop through the account dashboard
- the home page now also surfaces member checkout readiness inside the signed-in member resume, so storefront entry can hand off directly into prepared checkout or address-book setup from the same protected context
- route scaffolding now exists for catalog, account, loyalty, orders, and invoices so later slices can bind feature data without reworking the shell

The next storefront slice is now also underway:

- the catalog listing page now consumes public category and product contracts from `Darwin.WebApi`
- product-detail routing now consumes the public product-by-slug contract
- a public CMS index route now consumes the published CMS page list contract
- public CMS list/detail delivery now also follows the active request culture instead of relying on backend default-language behavior
- CMS-driven shell navigation now only accepts sanitized app-local paths or safe `http/https` URLs, so `main-navigation` content cannot push unsafe raw hrefs into the storefront chrome
- the CMS index now also offers a visible-result search lens over the already loaded page set, but keeps that state explicit and non-canonical until a real public CMS search contract exists
- the CMS index now also applies discovery-state and review-priority windows to the full matching search set when a browse lens is active, so public content review no longer drifts around the current loaded page while richer CMS grouping/search metadata still remains future work
- the CMS index now also surfaces visible-vs-loaded-vs-total result summaries, so the public content route stays aware of the current published set instead of acting like an unbounded card grid
- the CMS index now also surfaces explicit visible discovery-readiness coverage for ready pages, attention pages, and review support, so public content review can inspect window-level discovery debt instead of relying only on lenses and result counts
- the CMS index now also groups the visible page set by title initials with quick-jump anchors, so public content browsing feels like an oriented set without inventing a separate CMS taxonomy contract
- the CMS index now also derives a spotlight-plus-follow-up rail from the current visible result window, so the route can guide storefront reading without pretending a richer CMS curation contract already exists
- the CMS index now also exposes explicit handoff cards into Home, Catalog, and Account, so public CMS browsing stays connected to the broader front-office system instead of ending in content-only loops
- the CMS index pagination now also exposes first/last jumps while preserving the page-local visible-result lens, so public content window navigation is more complete without implying real backend search
- CMS index empty states now also keep Home/Catalog follow-up actions visible instead of leaving the public content surface at a dead end
- CMS detail pages now expose route metadata, related-page navigation, and visible degraded-state handling instead of collapsing non-404 failures into a not-found route
- CMS detail now also derives previous/next adjacency from the published page list and keeps Home/Catalog/Account follow-up explicit, so long-form content does not end in a leaf route
- CMS detail now also exposes breadcrumb and published-set position context, so long-form content remains oriented inside the broader public CMS set rather than feeling detached from storefront navigation
- CMS detail now also uses the same reusable continuation-rail pattern as product detail, so public content and commerce routes share one continuity model instead of drifting into bespoke follow-up blocks
- CMS detail now also derives on-page section navigation and reading/structure metrics from the published HTML itself, so rich content remains navigable without needing a separate CMS-specific page-layout contract first
- CMS detail anchor ids now also normalize diacritics before slugging section headings, so German and other accented headings keep stable in-page navigation ids instead of collapsing into weak fallback slugs
- CMS detail now also surfaces explicit published-content readiness for metadata, structure, and navigation coverage, so public content review can inspect discovery debt from the detail route itself instead of only from list-level lenses
- CMS detail now also exposes a direct review handoff back into the strongest CMS index window for discovery-ready versus attention-needed pages, so content review can continue from the right published set instead of restarting from the default list
- CMS index now also surfaces an explicit review action center for the current visible window, so discovery-ready, attention-needed, and title-ordered review sets can be reopened directly instead of being rebuilt by hand
- CMS and catalog review windows now also break visible debt into concrete review reasons such as missing metadata, missing primary imagery, and base-assortment fallback, so browse/content review can see what needs attention without inferring it from counts alone
- CMS and catalog review windows now also surface the next concrete review targets inside the current visible window, so teams can jump straight to the right page or product instead of hunting through the result set manually
- CMS detail and product detail now also surface the next concrete review target from their current review sets, so content and assortment review can continue directly from the detail route instead of bouncing back to index lists first
- CMS and catalog cards now also surface row-level review signals inside the browse windows, so metadata debt, image debt, and offer/base state are visible without opening every detail route first
- CMS index and CMS detail now also surface live catalog-category and live product follow-up windows, so public content routes can hand off directly into real commerce browse/detail paths instead of only pointing back to the generic catalog route
- CMS index and CMS detail now also surface a shared commerce-campaign window built from visible category lanes and strongest product opportunities, so public content routes can sell through stronger browse and buying stories instead of only exposing utility follow-up lists
- those CMS product follow-up windows now also rank by the strongest visible savings signal first, so content routes hand off into a clearer best-offer buying opportunity instead of arbitrary catalog order
- CMS index and CMS detail now also surface live cart/checkout continuity from the canonical public cart contract, so published content can hand off directly into an already active purchase flow instead of only into browse routes
- a public cart route now consumes the public cart contract and supports server-side add/update/remove flows for anonymous storefront usage
- the cart route now also reuses the public catalog contract for a small continue-shopping follow-up rail plus explicit cart next-step guidance, instead of leaving the shopper only with totals and checkout CTA
- the cart route now also reuses saved member addresses when a browser member session exists, so checkout readiness is already visible before the shopper leaves cart for checkout
- the cart route now also surfaces member profile/preferences/address readiness together, so signed-in shoppers can verify identity, phone/channel readiness, and address coverage before moving into checkout
- a public checkout route now consumes checkout-intent and order-placement contracts with inline address capture and shipping selection
- the checkout UI now also keeps readiness signals and live line-item review visible, so storefront conversion no longer depends only on totals plus the address form
- checkout now also reuses saved member addresses when a browser member session is available, while preserving manual entry as the fallback when the member address-book contract is unavailable
- checkout now also reuses canonical member profile identity for name/phone prefill when a member session exists but no saved address is selected, so the route can still start from member context before the address book is populated
- checkout now also surfaces recent member invoice attention inside the route, so signed-in shoppers can keep open billing follow-up visible before placing a new order
- checkout now also surfaces member profile, channel, and address-book readiness directly inside the route, so authenticated storefront checkout keeps profile/preferences/address context visible instead of treating member prefill as hidden background state
- checkout now also surfaces a dedicated payment-continuity window before order placement, so projected payable total, billing attention, and account handoff stay explicit instead of hiding behind the final submit action
- the cart route now also surfaces explicit opportunity and readiness panels, so shoppers can see the strongest adjacent offer plus basket readiness before leaving the route for checkout
- checkout now also surfaces explicit confidence and attention panels, so order-readiness, billing follow-up, phone verification, and address-book coverage stay visible at the final conversion step
- a public confirmation route now consumes storefront confirmation data and exposes payment-handoff retry through the payment-intent contract
- the confirmation route now also keeps post-checkout guidance explicit, including payment-next-step messaging, account/order-history follow-up, and stable order-reference handling instead of a passive receipt-only view
- the confirmation route now also surfaces signed-in member continuation across orders, invoices, and loyalty, so post-checkout handoff into the protected portal stays explicit instead of stopping at generic CTA buttons
- the confirmation route now also surfaces a dedicated payment-continuity window, so visible attempts, provider footprint, and commercial exposure are summarized before the shopper drops into lower-level payment detail rows
- the confirmation route now also gives guest shoppers an explicit account-continuation panel with sign-in/register/activation/password recovery handoff bound to the protected order-follow-up return path instead of relying on two isolated CTA buttons
- the confirmation route now also surfaces explicit post-purchase care and next-customer-window panels, so payment attention, order reference handling, and repeat-engagement follow-up stay visible after checkout instead of the route behaving like a passive receipt
- confirmation and auth-required follow-up links now also sanitize app-local return targets centrally, and the confirmation UI derives displayed order/payment status from the authoritative confirmation snapshot instead of trusting query-carried status text
- auth-required member entry points now also expose route-summary plus cross-surface follow-up panels, so protected routes do not regress to a minimal access wall while the rest of the portal carries richer orientation and handoff UX
- cart, checkout, and confirmation now also share breadcrumb-style route orientation plus explicit cross-surface handoff cards, so the conversion chain stays connected to the wider storefront system instead of behaving like detached transactional pages
- cart, checkout, confirmation, and their empty/unavailable follow-up states now also reuse a shared commerce continuation rail, so public conversion continuity stays aligned across healthy and degraded commerce routes
- cart and checkout now also surface a shared anonymous account-handoff panel carrying a sanitized `returnPath` into sign-in/register/activation/password help, so commerce routes keep account recovery one step away without dropping the current conversion intent
- degraded-mode storefront data states are now visible in the UI instead of silently collapsing into placeholder-only behavior
- catalog list/detail now pass the active request culture into the canonical public catalog endpoints, so storefront localization no longer depends on the backend default culture leaking through
- catalog merchandising polish now stays within the real public contract set: selected-category context, compare-at savings badges, and category-linked navigation were added without pretending search/facets/sort already exist
- `/catalog` now also applies category/query search plus offer/base browse lenses and review-oriented sort against the full matching assortment when those lenses are active, so pagination and result counts stay real even before richer backend facet metadata exists
- `/catalog` now also includes a page-local visible assortment lens for `all`, `offers`, and `base` windows, so browse can separate current-page offer density from base assortment without pretending real backend facets already exist
- `/catalog` now also applies `offers-first` and `base-first` review windows to the full matching assortment when a browse lens is active, so commercial review no longer drifts around the current loaded page while richer backend facet metadata still remains future work
- `/catalog` now also exposes real browse facets for image coverage and offer strength against the full matching assortment, so shoppers can open image-attention and hero-offer windows without falling back to page-local heuristics
- `/catalog` now also exposes direct quick windows for image-ready, image-attention, value-offer, and hero-offer browse sets, so the main catalog route can jump straight into the strongest facet-specific windows without rebuilding the filter stack manually
- `/catalog` now also surfaces visible-vs-loaded-vs-total result summaries plus first/last page jumps, so catalog window navigation is more complete without implying true backend search/facets support
- `/catalog` now also surfaces an offer-focus window plus a buying-guide summary from the live visible product set, so merchandising signals stay explicit even before true backend search/facets land
- `/catalog` now also surfaces explicit assortment-readiness coverage for visible offers, base assortment, and support context, so browse review can inspect route debt from the catalog window itself instead of only from counts and lenses
- `/catalog` and `/catalog/[slug]` now also surface live cart/checkout continuity from the canonical public cart contract, so browse and product evaluation can hand off directly into an already active purchase flow instead of behaving like isolated discovery routes
- product detail now also reuses the product's primary category plus the existing category-product listing contract to show related products without inventing a recommendation-specific API
- product detail now also surfaces degraded related-product follow-up state explicitly when that category-based follow-up fetch fails, instead of silently flattening adjacent catalog discovery
- product detail now also exposes breadcrumb, product-reference snapshot, and explicit storefront handoff actions so the conversion route stays oriented inside the broader front-office system
- product detail now also surfaces offer-position and buying-context panels derived from the current product plus related-offer signals, so conversion/detail routes communicate active offer strength instead of behaving like static specification pages
- product detail now also surfaces explicit buying-readiness coverage for metadata, merchandising, and adjacent follow-up, so public commerce review can inspect route debt from the detail page itself instead of only from route-summary status
- product detail now also exposes a direct review handoff back into the strongest catalog window for offer versus base-assortment review, so assortment review can continue from the right browse set instead of restarting from the default catalog
- catalog index now also surfaces an explicit review action center for the current assortment window, so offer-first, base-first, and full-assortment review sets can be reopened directly instead of being rebuilt by hand
- product detail cross-surface handoff now also includes a CMS return path inside the shared continuation-rail pattern, so content and commerce remain visibly connected in both directions instead of diverging into route-local button clusters
- catalog index and product detail now also surface live published CMS follow-up windows, so public commerce browsing stays connected to storefront content without waiting for new backend contracts
- product detail unavailable and related-products-empty states now also keep account follow-up visible, so degraded conversion/detail states remain connected to the wider front-office system

The first account self-service foundation is now also in place:

- `/account` is now a real self-service hub instead of a pure placeholder
- public account self-service links are now locale-aware and the account hub now also exposes the shared continuation-rail pattern back into Home, Catalog, and CMS, so public account entry stays aligned with the rest of the storefront routing model
- sign-in, registration, activation, and password-recovery routes now also share the same continuation-rail pattern back into Home, Catalog, and CMS, and sign-in no longer uses raw anchor navigation for internal storefront routes
- the public account hub plus sign-in/register/activation/password routes now also consume live published CMS pages plus live public categories through the shared public-auth continuation wrapper, so self-service entry can hand off into real content and browse surfaces instead of falling back to static continuation cards
- the public account hub plus sign-in/register/activation/password routes now also consume live storefront cart state through that same shared public-auth continuation wrapper, so self-service entry can resume cart/checkout continuity instead of dropping active commerce context at the auth boundary
- sign-in/register/activation/password now also surface a shared post-auth destination summary fed by sanitized `returnPath` plus live cart state, so public auth routes keep checkout/cart/member intent explicit instead of acting like context-free forms
- that public auth post-auth destination summary now also exposes direct CTA handoff to the sanitized return route plus live cart continuation, so self-service routes can actively recover the current storefront/member journey instead of only describing it
- that shared public-auth continuation flow now also surfaces a browse-campaign board built from live category lanes plus strongest visible offers, so account hub, sign-in, registration, activation, password recovery, and protected auth walls can keep stronger browse-and-buy narratives alive instead of only listing continuation links
- the public account hub now also surfaces a dedicated storefront-readiness panel and carries the preferred post-auth destination into sign-in/register/activation/password recovery links, so public account entry preserves active cart/checkout intent instead of defaulting every auth jump back to `/account`
- the public account hub now also surfaces a live storefront action center fed by current cart state plus published CMS/category spotlights, so anonymous account entry can still move through real content and commerce next steps instead of collapsing to auth-only choices
- the public account hub action center now also surfaces a strongest visible product offer, so anonymous account entry can still create a real next-buy decision instead of only handing off into cart, CMS, or category browse
- the public account hub now also surfaces a live offer board with multiple strongest visible product opportunities, so anonymous account entry can pitch several next-buy options instead of stopping at one spotlight
- those public account, public auth, protected auth-wall, commerce handoff, and guest-confirmation offer boards now also classify visible offers into explicit merchandising tiers such as hero offer, value offer, price drop, and steady pick, so high-intent entry points communicate why an opportunity matters instead of only listing ranked products
- the public `/account` hub now also accepts and preserves a sanitized incoming `returnPath`, so auth walls can hand shoppers into the generic account entry route without losing the intended post-auth destination context
- the account hub now also consumes that same public-auth continuation wrapper instead of assembling a raw continuation item list locally, so public account entry stays on the same feature-level abstraction path as the other auth routes
- sign-in/register/activation/password plus loyalty/confirmation auth-facing links now also share one localized `returnPath` builder, so public/member entry routes no longer hand-build encoded auth query strings with slightly different per-route rules
- auth, sign-in, and member-profile action inputs now also pass through shared trimmed/bounded FormData readers for email, returnPath, tokens, passwords, phone codes, and profile identity fields, so those server-action entry points are less dependent on route-local `String(...).trim()` coercion
- product-category follow-up, loyalty timeline paging, and order-confirmation handoff links now also share a localized query-href builder, so route-level app-query assembly no longer drifts between public and protected surfaces
- cart, checkout, and member-portal server actions now also build flash/status/error redirects through shared app-query helpers, so route transitions no longer duplicate query-string concatenation logic across multiple action files
- account and member-session server actions now also use that same shared app-query helper path for sign-in/register/activation/password redirects, so auth-flow query construction no longer lives on a separate local utility path
- member order/invoice payment-intent failure redirects now also run through the shared app-query param helper, so protected commerce handoff errors no longer keep their own leftover separator/encoding branch inside the member action layer
- product detail now also feeds its category-filtered catalog continuation rail through the shared app-query helper path, so that catalog follow-up route no longer keeps a one-off inline query string on the detail surface
- catalog route metadata plus catalog/CMS/discovery pagination and filter links now also use the central app-query path helper, so page- and component-level query construction follows the same routing abstraction as the action layer
- checkout confirmation finalize now also builds its handoff redirect through the same central app-query helper, so hosted-checkout completion does not keep a route-local query builder branch apart from the rest of storefront routing glue
- checkout confirmation finalize now also bounds callback-carried order/provider/failure text and clears stale mismatched handoff cookies, so PSP return handling is less trusting of raw callback query payloads
- shared query serialization now also covers public catalog/CMS fetch helpers, cart fetch, checkout-draft search persistence, checkout confirmation fetch, and member loyalty paging helpers, so infrastructure-side query construction is no longer repeated across multiple feature modules
- the shell culture switcher now also clones and carries query state through the shared query utility path, so even component-level culture switching no longer keeps a separate local search-param copy branch
- member order-history and invoice-history routes now also read `page` through the shared positive-integer search-param helper, so protected pagination input no longer keeps a raw number-cast branch outside the broader route hardening path
- browser-owned cart display, storefront payment handoff, and member session cookies now also perform minimal shape validation after `JSON.parse`, so cookie corruption is less likely to masquerade as valid state inside storefront/member flows
- browser-owned prepared loyalty scan-session cookies now also validate parsed JSON shape before reuse, so loyalty scan preparation no longer accepts malformed cookie payloads just because they parse successfully
- member-session expiry checks and prepared loyalty scan-session expiry/validity checks now also use shared UTC timestamp helpers, so timestamp parsing no longer drifts across cookie/session hardening paths
- order confirmation and member order detail now also share a stricter address-JSON parser, and loyalty reward-progress rendering now clamps parsed percent input before display, so these JSON/numeric UI paths are less trusting of malformed payloads
- checkout integer parsing is now stricter for quantity, page, and shipping-minor-unit inputs, so malformed mixed strings no longer pass through permissive integer/number casts in storefront routing and order placement
- loyalty business detail now normalizes next-reward progress through an explicit percent parser/clamp helper instead of repeated raw `Number(...)` casts in render, which closes the remaining percent-display edge case on that surface
- bounded numeric query parsing is now also strict about decimal shape before conversion, so latitude/longitude/radius-style route inputs no longer rely on permissive `Number(...)` coercion
- CMS and catalog routes now also use feature-level continuation-rail wrappers on top of the shared public continuation component, so content/discovery continuity no longer depends on route-local item assembly
- public registration now runs through the member auth register endpoint
- activation email request and token confirmation now run through the member auth activation endpoints
- resend-activation recovery is now surfaced inline on account hub, sign-in, register, and password-recovery routes instead of relying only on the dedicated activation page
- password reset request and completion now run through the member auth reset endpoints
- public registration, activation, password reset, and sign-in now also normalize email input plus stronger required/autocomplete/password guardrails so avoidable auth-flow mismatches are reduced before the canonical API call
- browser sign-in persistence, member profile editing, addresses, and the authenticated portal remain a distinct follow-up slice because the final browser auth/session transport is still an explicit platform decision

The next member-portal slice is now also in place:

- `Darwin.Web` now has a provisional browser session layer that stores member session state in web-owned cookies rather than exposing raw tokens in the UI
- the provisional browser session layer now refreshes member access tokens near expiry and retries protected member fetches once before forcing a new sign-in
- the authenticated account route now renders profile, preference, and linked CRM context snapshots from the member API surface
- the authenticated account route now also surfaces loyalty overview totals plus joined-business snapshots, so `/account` can act as a real member landing route across profile, commerce, and loyalty
- the authenticated account route now also surfaces next-reward focus cards derived from the canonical loyalty overview accounts, so `/account` can hand members directly into the most relevant loyalty business follow-up
- the authenticated account route now also derives an action-center from profile, addresses, invoice balance, and loyalty snapshots, so `/account` acts as a real next-step surface instead of a passive summary route
- the authenticated account route now also surfaces live storefront cart continuity from the canonical public cart contract, so signed-in members can resume cart/checkout directly from the member portal
- the dashboard, profile, preferences, and address-book surfaces now also share explicit member-portal navigation, so the protected account area behaves like one front-office subsystem instead of separate editor pages
- editable member profile and communication/account preferences now run through the canonical member profile endpoints
- the authenticated preferences route now also reads canonical profile channel readiness, so email/SMS/WhatsApp toggles are shown together with the real profile-channel prerequisites instead of acting like detached booleans
- authenticated member password change now runs through a dedicated security route that uses the canonical authenticated password-change endpoint instead of the public recovery flow
- profile, preferences, security, and addresses now also surface a shared storefront-continuation window fed by live public CMS pages plus public categories, so authenticated account editor routes stay connected to published content and catalog browse follow-up instead of behaving like portal-only leaves
- profile, preferences, security, and addresses now also surface live product highlights inside that shared storefront-continuation window, so authenticated account editor routes can create a next-buy moment instead of only offering content and category follow-up
- the shared account-editor storefront window now also ranks product opportunities by strongest visible savings and compare-at context, so self-service routes present sharper buying highlights instead of a raw product list
- that shared account-editor storefront window now also classifies both product highlights and browse-campaign lanes into explicit merchandising tiers, so authenticated account routes can frame clearer buying stories instead of only showing ranked products and generic offer lanes
- account hub plus sign-in/register/activation/password routes now also surface live product highlights inside their shared public-auth continuation flow, so anonymous account recovery can preserve a next-buy opportunity instead of only keeping cart/content/category context alive
- those public-auth and account-entry product highlights now also rank by the strongest visible savings signal first, so self-service routes surface a clearer best-offer moment instead of echoing arbitrary catalog order
- protected member-route auth walls now also surface the same live storefront continuation context, so signed-out visits to orders, invoices, and account editor routes keep cart/content/product opportunity visible instead of collapsing into a minimal access block
- protected member-route auth walls now also surface a live offer board with several strongest visible buying opportunities, so protected entry does not reduce commercial momentum to a single spotlight
- the authenticated security route now also surfaces current profile/session security context, including phone-verification state, session-expiry visibility, and direct handoff back into profile/dashboard follow-up instead of behaving like a password-form-only leaf page
- reusable member address-book create/update/delete/default flows now run through the canonical member address endpoints
- member dashboard and address-book routes now also hand off directly into checkout with saved-address prefills, so protected member data can feed the storefront conversion path instead of staying isolated inside the portal
- the authenticated address-book route now also surfaces explicit checkout-readiness state for reusable/default shipping/default billing coverage, so storefront handoff readiness is visible from the address subsystem instead of being inferred only from individual cards
- member dashboard now also surfaces recent order and invoice snapshots from the canonical member commerce endpoints, so account overview acts as a real member landing page instead of only a route map
- member dashboard now also surfaces a dedicated security window with phone-verification state, session-lifetime visibility, and direct handoff into the authenticated security/profile routes, so account overview exposes security readiness from the main member landing page instead of hiding it behind a second route
- member dashboard now also surfaces a communication window derived from canonical profile plus preferences, so email/SMS/WhatsApp readiness is visible from `/account` instead of being hidden behind preferences/profile routes
- member dashboard now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so the protected member landing route stays connected to public browse/content follow-up instead of acting like a portal-only island
- member dashboard storefront merchandising now also ranks visible product follow-up by strongest savings signal, so signed-in account entry can surface clearer next-buy opportunities instead of a raw public-product list
- that member-dashboard storefront offer board now also shifts away from products already linked to the active cart when browser storefront-shopping state exists, so signed-in account entry can pitch the next buying move beyond the current basket instead of echoing it
- profile phone-verification and address forms now also carry stronger client-side input/autocomplete guardrails so avoidable member-data mistakes are caught before the canonical API call
- phone verification request/confirm now runs inside the profile surface through the canonical SMS/WhatsApp verification endpoints and shared profile confirmation flag
- orders and invoices now render authenticated history pages plus detail routes with payment-retry handoff
- the authenticated orders route now also surfaces explicit fulfillment-readiness state for visible orders needing active follow-up, so order-history follow-up is visible from the history route instead of being inferred only from row-level statuses
- the authenticated orders route now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so order history stays connected to public content and catalog follow-up instead of behaving like a portal-only archive
- order-history storefront merchandising now also ranks visible product follow-up by strongest savings signal, so post-purchase history can surface clearer next-buy opportunities instead of a raw public-product list
- the authenticated invoices route now also surfaces explicit billing-readiness state for visible outstanding invoices and open balance, so finance follow-up is visible from the history route instead of being inferred only from row-level balances
- the authenticated invoices route now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so invoice history stays connected to public content and catalog follow-up instead of behaving like a portal-only finance archive
- invoice-history storefront merchandising now also ranks visible product follow-up by strongest savings signal, so billing history can surface clearer next-buy opportunities instead of a raw public-product list
- protected order/invoice detail storefront merchandising now also ranks visible product follow-up by strongest savings signal, so commerce detail routes can surface clearer next-buy opportunities instead of a raw public-product list
- protected order/invoice detail storefront merchandising is now also cart-aware, so those commerce detail routes avoid echoing items already linked to the active storefront cart and keep the next-buy signal cleaner
- unavailable order/invoice detail states now also keep explicit follow-up actions to account/catalog and their parent history routes instead of collapsing into warning-only dead ends
- loyalty now renders the authenticated overview instead of remaining a placeholder page
- the authenticated loyalty overview route now also surfaces explicit engagement-readiness state for active joined places and reward-focus follow-up, so loyalty overview acts as a next-step surface instead of only a balances/list screen
- the authenticated loyalty overview route now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so loyalty follow-up can move back into public content and catalog discovery without detouring through the dashboard first
- member orders/invoices/loyalty now also follow the same shared member-portal navigation model as profile/preferences/addresses, so the authenticated portal behaves more like one subsystem than a set of detached routes
- dashboard, orders, invoices, and loyalty overview now also expose breadcrumb-style route orientation plus cross-surface handoff actions, so the authenticated member area stays connected to the wider front-office system instead of behaving like a sealed portal island
- dashboard, orders, invoices, and loyalty overview now also share a reusable member cross-surface rail, so protected overview routes no longer duplicate route-handoff blocks or drift into inconsistent portal-side follow-up UX
- profile/preferences/addresses plus order/invoice/loyalty-business detail routes now also expose the same breadcrumb-style route orientation and cross-surface handoff actions, so editor/detail screens stay inside the same front-office navigation model instead of collapsing back to isolated portal pages
- profile/preferences/addresses plus order/invoice/loyalty-business detail routes now also reuse the same member cross-surface rail as the overview routes, so protected continuation/follow-up behavior no longer drifts between overview and detail/editor surfaces
- protected member access walls plus unavailable order/invoice/loyalty detail states now also reuse the shared member cross-surface rail, so protected degraded-state follow-up stays aligned with the same portal continuity model used on healthy routes
- dashboard and preferences now also surface explicit route-summary/follow-up panels, and address/order/invoice empty states now keep actionable catalog/account follow-up instead of ending in passive placeholder blocks
- addresses, orders, and invoices empty states now also reuse the shared member cross-surface rail, so protected list routes keep the same continuation model as overview/detail/auth-required surfaces
- profile, preferences, and addresses now also keep explicit route-summary and unavailable follow-up guidance, so the account-edit subsystem stays navigable even when profile/preference/address data is partial or absent
- loyalty business detail routes now render business-scoped dashboard, rewards, and cursor-paged timeline data from the member contracts
- loyalty business detail now also follows the shared member-portal navigation model, and browser-prepared scan-token state now clears stale/mismatched cookie state instead of silently drifting
- loyalty business detail now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so protected loyalty follow-up stays connected to public CMS and catalog browse routes instead of terminating inside a portal-only detail branch
- loyalty overview, discovery, public business detail, and signed-in business detail now also expose explicit route-summary panels plus actionable empty/degraded follow-up CTAs, so loyalty routes stay observable and navigable instead of degrading into passive no-data states
- this remains an implementation boundary, not a permanent architecture verdict; deeper BFF/session hardening can still replace the current web-owned cookie wrapper later

The next localization/config-driven slice is now also in place:

- `Darwin.Web` now treats `de-DE` and `en-US` as the current supported front-office cultures and resolves the active culture through config plus a web-owned culture cookie
- query-string culture switching now lands through middleware so `?culture=de-DE|en-US` becomes a persisted preference instead of an ad hoc page-only toggle
- shell fallback navigation/footer copy now follows the active culture instead of staying hardcoded in one language
- shared shell/catalog/storefront-commerce wording plus account-edit/member-commerce and loyalty discovery/public-detail wording now runs through resource bundles under `src/Darwin.Web/src/localization/resources`, aligned with the same de/en-first additive strategy already being established across WebAdmin and mobile
- storefront typography now uses local/system font stacks instead of 
ext/font/google, so front-office builds stay deterministic in restricted/offline environments and theme typography does not depend on build-time external fetches
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
- CMS unavailable/no-pages and catalog no-results fallbacks now also keep account follow-up visible, so public discovery/content dead ends remain connected to the broader front-office system
- CMS no-pages and catalog no-results now also use the shared continuation-rail pattern, so list-level public empty states do not drift back into one-off CTA clusters
- CMS and catalog feature routes now also assemble their public follow-up rails through feature-level wrappers instead of route-local `PublicContinuationRail` item lists, so continuity rules stay reusable at the CMS/catalog module boundary
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
- public loyalty business detail now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so pre-join loyalty routes stay connected to public content and catalog browse follow-up instead of ending as standalone detail leaves
- public loyalty overview now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so signed-out loyalty discovery stays connected to published content and catalog browse follow-up instead of behaving like a discovery-only leaf route
- direct join from `/loyalty/[businessId]` now posts through the canonical member loyalty join contract and can optionally pass a preferred business location
- member-only balances, promotions, and timeline data remain behind the authenticated portal, so public discovery does not fork the loyalty contract model away from mobile/member usage
- `/loyalty` now also supports query-driven latitude/longitude/radius proximity filtering and a server-rendered coordinate preview driven by the same public discovery result set
- `Darwin.Web` deliberately does not consume `public/businesses/map` yet for the loyalty surface, because that contract currently lacks a loyalty-active filter and could mix non-loyalty businesses into a loyalty-only discovery page

The current loyalty scan-preparation slice is now also in place:

- `/loyalty/[businessId]` now prepares canonical browser-side scan sessions for accrual or redemption through `POST /api/v1/member/loyalty/scan/prepare`
- branch selection and redeemable reward selection now feed directly into that canonical member contract instead of relying on a web-local scan model
- the returned short-lived scan token is intentionally kept in a short-lived web-owned cookie and rendered back on the page, rather than being exposed in the URL
- the active prepared token now also renders as a real QR image on `/loyalty/[businessId]` for contract visibility
- this should currently be treated as a provisional contract-consumption slice, not as a final statement that `Darwin.Web` will own mobile-like browser camera/scanner or barcode handling
- current product direction for `Darwin.Web` is that loyalty on the web can evolve differently from mobile and may support direct point accrual/redemption flows for store-enabled businesses without requiring browser camera/scanner support; richer web-specific loyalty behavior remains a later product-definition slice

The next member-commerce hardening slice is now also in place:

- order detail now renders canonical member payment attempts, shipment snapshots, linked invoice summaries, and direct document download against the member order contract
- invoice detail now renders canonical payment summary plus direct document download against the member invoice contract
- member order/invoice detail routes now also surface explicit readiness panels for payment, shipment, balance, and document follow-up, so protected commerce detail routes communicate actionable next-step state instead of only relying on summary blocks
- member order/invoice detail routes now also surface storefront continuation windows fed by live public CMS pages plus public categories, so protected commerce detail routes stay connected to public content and catalog follow-up instead of ending inside portal-only branches
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
- `robots.txt` and `sitemap.xml` should enumerate only public/indexable routes; public Home/CMS/catalog routes can now resolve under locale-prefixed URLs, and CMS/product detail routes can now emit localized `hreflang` alternates by matching canonical public ids across the current `de-DE` / `en-US` public sets
- because `/`, `/cms`, `/catalog`, CMS detail, and product detail now have safe cross-language alternate URLs for the current baseline, sitemap/canonical work can treat the current two-language storefront as a real localized public surface instead of an index-only compromise
- sitemap generation now also builds localized CMS and product detail inventory from canonical public-id matching instead of default-culture-only paging, so multilingual public discovery stays coherent without route-local slug guessing
- that localized CMS/product inventory now also loads through shared cached server helpers, so detail-page metadata alternates and sitemap discovery reuse one canonical multilingual inventory path instead of rebuilding public sets separately
- CMS and catalog browse cores now also load through the same shared observed-loader pattern as the rest of the storefront route-context stack, so public discovery assembly is more consistent, cache-aware, and traceable across list/detail/SEO surfaces
- storefront continuation snapshots for live CMS/category/product follow-up now also load through that same shared observed-loader path with explicit health summaries, so continuation windows no longer stay as the last route-local discovery assembly branch
- shell menu loading and cart view-model assembly now also run through the same shared observed-loader and health-summary path, so shell chrome and cart entry no longer keep manual diagnostics/caching branches outside the broader storefront infrastructure

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

## 17. Recent Darwin.Web Hardening Notes

- CMS and catalog review now also run on shared priority helpers with focused tests, so Home, browse windows, and detail routes all point at the same next review targets instead of drifting into route-local heuristics
- CMS detail and product detail now also surface visible review queues, so item-by-item content and assortment review can continue directly from the drilled-in route instead of bouncing back to list windows after every target
- CMS detail and product detail now also preserve the active review window across drilled-in targets, so review can continue from item to item without losing the current CMS/catalog lens context
- CMS and catalog review queues plus preferred review-window reopening now also run through shared workflow helpers with focused tests, so Home, browse windows, and detail routes preserve the same review lens and next-target behavior instead of mixing route-local review logic
- Home review cockpit targets now also preserve the active CMS/catalog review window when opening the next page or product, so review can start from storefront entry without losing the intended lens context
- CMS/catalog review-window routing now also runs through shared tested helpers, so browse cards, Home review cockpit, and detail follow-up all preserve the same lens context without route-local link drift
- Home discovery now also runs through a shared cached server context with explicit timing diagnostics for core feeds and category spotlights, so storefront entry builds with less duplicate work and more observable performance behavior
- storefront runtime config now also supports `noir` and `solstice` visual directions through `DARWIN_WEB_THEME`, so the same front-office system can cover a broader real multi-brand theme set without feature rewrites
- CMS and catalog browse cards now also preserve the active review window when opening page/product detail, so review can move from list to detail without rebuilding the current lens context
- catalog browse and product detail now also load through shared cached public discovery contexts, so metadata, browse assembly, and detail follow-up reuse the same core catalog/category data instead of refetching it route by route
- CMS browse and CMS detail now also load through shared cached public content contexts, so metadata, published-page review, and detail follow-up reuse the same core page data instead of duplicating CMS fetch work per route
- CMS and catalog route composition now also loads through shared cached route-context helpers, so browse/detail routes reuse one observable public-discovery assembly model and CMS detail no longer duplicates its content-plus-commerce follow-up fetches route by route
- catalog review-sort parsing now also runs through a shared tested helper, so browse and detail routes keep one canonical interpretation of the active catalog review lens
- account editor, protected history, and auth-entry routes now also consume those same canonical storefront projection helpers, so member/auth surfaces keep one stable content+browse+cart prop shape instead of repeating route-local storefront wiring
- shared observed loader helpers now also back the main storefront, CMS/catalog route-context, member-route, and Home-discovery assembly paths, so caching plus slow/failure diagnostics are wired through one canonical server pattern instead of being hand-built per route
- route diagnostics now also carry health summaries for content, browse, cart, and member readiness on the main storefront/member loaders, so slow or degraded route assembly is easier to trace without reopening each route’s internal fetch graph
- cart, checkout, and confirmation now also load through shared cached observed route-context helpers with explicit commerce health summaries, so the main conversion flow reuses one canonical server assembly pattern instead of keeping route-local diagnostics and caching behavior
- member summary snapshots and browser shopping continuity now also load through shared observed helpers with explicit health summaries, so account/Home/commerce routes reuse one traceable baseline for identity, billing, and live-cart continuity instead of rebuilding those snapshots piecemeal
- public CMS/catalog/menu GET calls now also reuse canonical cached fetches and normalized cache tags, so equivalent public discovery requests avoid more duplicate work and keep cache invalidation/coherence cleaner across storefront routes
- public GET fetches now also normalize their query-string cache keys before reuse, so equivalent CMS/catalog/menu requests dedupe at the fetch layer itself instead of only sharing invalidation tags after the fact
- cart and checkout now also surface a live storefront-discovery window backed by published CMS pages, public categories, and visible product opportunities, so active purchase routes can hand shoppers back into content, browse, and upsell paths without dropping the current conversion flow
- the shared commerce storefront window now also picks the strongest visible product offer instead of the first catalog card, so cart, checkout, and confirmation show a clearer next-buy signal
- cart and checkout now also surface live product highlights inside their guest account/auth handoff, so anonymous shoppers can keep a next-buy opportunity visible while deciding whether to sign in or create an account
- guest commerce auth handoff now also ranks product highlights by the strongest visible savings signal first, so cart and checkout keep a clearer best-offer pitch alongside account recovery
- the guest commerce auth handoff now also surfaces a live offer board with multiple strongest visible product opportunities, so cart and checkout can keep several next-buy options visible during the account decision
- the shared commerce storefront-discovery window now also surfaces a live offer board with multiple strongest visible product opportunities, so cart, checkout, and confirmation can show several next-buy options instead of a single buying spotlight
- protected member, public discovery, and commerce page-loader cores now also live in pure tested helpers beneath their server-only wrappers, so the main page-assembly infrastructure has direct regression coverage without depending only on route-level integration tests
- those commerce and protected-entry offer boards now also carry explicit merchandising-tier labels, so cart, checkout, confirmation, auth-wall, and account-handoff routes frame visible buying opportunities as concrete commercial stories instead of generic product lists
- order confirmation now also surfaces that same live storefront-discovery window, so after-purchase shoppers can move directly into content, browse, and product follow-up instead of ending on a receipt-only route
- order confirmation now also biases its product follow-up away from items already present in the just-finished order, so the after-purchase opportunity behaves more like a true next-buy suggestion than a generic catalog echo
- guest order confirmation now also surfaces a next-buy highlight inside the account-tracking handoff, so anonymous after-purchase shoppers keep a real commercial next step visible while deciding whether to sign in or create an account
- guest order confirmation now also surfaces a live next-buy offer board inside the account-tracking handoff, so anonymous after-purchase shoppers can see several post-purchase opportunities instead of one spotlight
- that guest confirmation next-buy highlight now also selects the strongest visible offer outside the just-purchased items first, so after-purchase account tracking keeps a cleaner upsell signal instead of echoing the first catalog card
- checkout now also surfaces combined commercial exposure from the projected new order plus any visible open invoice balance, so payment and billing attention stay explicit before order placement
- order confirmation now also separates recorded payment from remaining payable exposure, so post-purchase payment follow-up can be read directly from confirmation instead of being inferred from raw attempt rows
- checkout and confirmation now also break payment exposure into order total, billing carry-over, combined exposure, and coverage state, so financial follow-up is easier to read before and after order placement
- the authenticated member dashboard now also surfaces live product highlights alongside CMS and category continuation, so signed-in shoppers can see the next buying opportunity directly from `/account`
- orders and invoices now also surface live product highlights alongside their storefront continuation windows, so member history routes can still create a next-buy moment instead of behaving like passive archives
- those orders and invoices storefront offer boards now also shift away from products already linked to the active cart when browser storefront-shopping state exists, so member history routes can pitch the next buying move beyond the current basket instead of echoing it
- order and invoice detail routes now also surface live product highlights alongside their storefront continuation windows, so protected commerce detail pages can create a next-buy moment instead of only offering content and category follow-up
- document download links in member order and invoice detail now validate absolute and proxied WebApi URL shape before rendering anchors
- if the backend contract returns a malformed document path, the web route now keeps the surface usable and shows an explicit unavailable state instead of rendering a broken or over-permissive download link
- remaining loyalty discovery map links and linked-invoice detail handoffs now also honor locale-prefixed routing, closing another localized-route drift inside public/member continuity flows
- public loyalty-business contact rendering now validates `http/https` website URL shape before exposing external anchors, so the storefront does not trust arbitrary schemes from optional public metadata
- loyalty public/discovery/member-overview media rendering now resolves optional business image URLs through the same backend-safe helper, so image placeholders remain the fallback when media paths are malformed
- catalog, cart, checkout, and product-detail media rendering now uses that same backend-safe helper, so public commerce image handling is aligned with the hardened loyalty-media path instead of trusting raw payload URLs
- safe public website links and member document downloads that open in new tabs now also use `noopener noreferrer`, so external/document handoff follows the same tab-isolation baseline as the rest of the hardened link surface
- public CMS and product-detail HTML rendering now runs through a lightweight fragment sanitizer before injection, so the web layer no longer blindly trusts raw backend HTML for those long-form surfaces
- that sanitizer now also strips unquoted inline event handlers and unquoted `javascript:` `href/src` attributes, so malformed dangerous fragments do not bypass the earlier quoted-attribute checks
- CMS detail now also sanitizes its non-summary HTML fallback before injection, so heading-less pages do not bypass that same fragment-hardening path
- browser-local cart snapshot links are now sanitized both when accepted from cookie state and when rendered back into cart/checkout UIs, so product return links stay inside the intended app-local route tree
- catalog and CMS campaign windows now also classify visible product opportunities into hero-offer, value-offer, price-drop, and steady-pick tiers, so merchandising framing can tell a clearer commercial story than a raw savings percentage alone
- member identity and member commerce summary snapshots now also flow through shared cached server contexts, so account, cart, checkout, confirmation, and Home can reuse the same protected summary data with less repeated fetch work
- account and commerce continuation windows now also surface browse-campaign boards built from live categories plus strongest visible offers, so recovery, portal, and conversion routes can propose stronger next-browse lanes instead of only raw follow-up links
- backlog management for `Darwin.Web` now explicitly separates core public/member flow delivery from a later quality pass for UX, security, performance, telemetry, and deeper regression coverage work

- the profile route now also surfaces explicit readiness for identity, phone verification, and locale/billing defaults, so member self-service can see commerce/communication completeness without inferring it only from the edit form
- the root HTML shell now also suppresses hydration warnings for browser-extension mutations on `<html>`, so storefront startup stays stable when client-side tooling injects classes before React hydrates
- shell navigation now also passes the active culture into CMS menu loading and can project the seeded `Footer` menu into the rendered footer, so seed-driven navigation labels and links stay aligned with the current storefront language instead of defaulting silently
- when a CMS `Footer` menu exists it now acts as the authoritative runtime footer, while the fallback footer keeps only storefront/legal links instead of platform/debug placeholders
- shared public-API fallback copy is now shopper-friendly instead of infrastructure-facing, so degraded CMS/catalog/cart surfaces no longer read like raw transport failures
- the authoritative footer seed now also includes `Contact` alongside the German legal pages, so service navigation is not reduced to compliance-only links
- the shell header no longer renders debug-style fallback/source copy, utility actions are now icon-first, and the language switcher now uses compact two-letter codes, so storefront chrome stays cleaner and less noisy
- localized query-message resolution now also falls back across the shared resource bundle, so degraded API states show real shopper-facing copy instead of leaking `i18n:*` keys into the UI
- Home spotlight empty states now also resolve localized degraded API messages before rendering, so CMS/product spotlight sections no longer leak raw `i18n:*` keys
- the seeded footer-linked legal CMS pages (`impressum`, `datenschutz`, `agb`, `widerruf`) now ship with fuller bilingual Germany-oriented starter content instead of one-line placeholders
- degraded-success diagnostics are now quiet by default during local development, so partial-health tracing no longer floods the browser console while the route still renders successfully
- `/catalog` and `/cms` now also build their visible browse/review windows inside shared server page-context loaders, so lens-driven review no longer duplicates matching-set fetches in the route files and diagnostics reflect the real visible window instead of only the paged seed fetch
- shared public-discovery, protected-member, and commerce page-loader cores now also emit stable loader-kind, auth-gate, and continuation diagnostics with direct core tests, so route-family tracing is more actionable and core assembly drift is easier to catch before UI-level regressions
- multilingual CMS/product inventory for alternates and sitemap now also flows through one shared public-discovery inventory snapshot, so `hreflang` and sitemap discovery reuse a single cached localized source instead of spinning separate page/product inventory loaders
- shared route observability now also classifies diagnostics by signal kind, attention level, degraded-status keys, and suggested action, so production/staging logs can separate slow-but-healthy work from degraded success and immediate failures much faster
- shared SEO metadata loaders now also have direct regression coverage, so canonical/no-index/language-alternate assembly for Home, discovery, commerce, and protected routes is less likely to drift silently behind route-level tests
- catalog and CMS index SEO loaders now also consume canonical route arguments instead of raw `searchParams` objects, so browse metadata caching aligns better with the shared page-context model and avoids object-shaped cache misses on repeated discovery requests
- shared multilingual discovery inventory now also carries precomputed CMS/product alternates plus sitemap-ready detail entries, so detail-page `hreflang` and public sitemap reuse one projection path instead of regrouping the same inventory in multiple loaders
- shared SEO metadata loaders now also emit canonical `seo-metadata` diagnostics with explicit indexability state, so production tracing can distinguish metadata assembly from page assembly and read indexable versus noindex outcomes directly from the log context
- shared multilingual discovery projections now also have direct regression coverage for alternates and sitemap assembly, so recent performance refactors around localized inventory reuse are less likely to drift silently behind route-level tests




