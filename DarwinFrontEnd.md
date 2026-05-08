# Darwin Front-End Guide

[![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![TailwindCSS](https://img.shields.io/badge/TailwindCSS-4-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
[![Stripe](https://img.shields.io/badge/Stripe-Phase--1-635BFF?logo=stripe&logoColor=white)](https://stripe.com/)
[![DHL](https://img.shields.io/badge/DHL-Phase--1-FFCC00?logoColor=black)](https://www.dhl.de/)

> Scope: `src/Darwin.Web`, the public storefront and authenticated member portal.

Current provider status matters for the storefront: Stripe webhook/reconciliation exists, but live Stripe PaymentIntent/Checkout Session creation is still a backend go-live blocker. Do not present the current checkout handoff as a production Stripe checkout until `docs/go-live-status.md` says that gap is closed.

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

## 2.3 Customer-Facing Page Architecture

`Darwin.Web` is the public storefront and member portal. It must not render internal diagnostics, route-health summaries, readiness dashboards, review queues, composition windows, route maps, API status labels, loaded-count/server-total summaries, or back-office review language to customers. Observability and loader diagnostics stay server-side, in tests, or in `Darwin.WebAdmin`.

Approved public page map:

- `/`: storefront landing page with a hero, catalog CTA, loyalty/account CTA, 4-6 featured categories, 4-8 featured products or offers, one loyalty teaser, up to 3 help/content cards, and an optional trust strip.
- `/catalog`: product listing with title, search, category filter, optional sort/basic filters, product grid, pagination, and empty state.
- `/catalog/[slug]`: product detail with breadcrumb, gallery, name, price, description, variants/add-ons when supported, quantity, add-to-cart, shipping note, details, and up to 4 related products.
- `/help`: customer help and information listing with search, article/page cards, pagination, and optional topics. `/cms` redirects to this public entry point. Do not label the experience as "CMS" for customers.
- `/page/[slug]`: customer-facing content detail pages with breadcrumb, title, published content, related pages, and back/help links. Legacy `/cms` remains an index redirect/compatibility entry, not a visible customer label.
- `/loyalty`: public loyalty explanation and partner discovery for anonymous users; points, joined businesses, reward progress, recent activity, and discovery/join flows for signed-in users.
- `/cart`: cart items, quantity update, remove item, coupon, order summary, continue shopping, checkout CTA, empty state, and up to 4 recommendations.
- `/checkout`: contact/customer information, address, shipping method, payment/hosted checkout handoff, order summary, place-order CTA, and concise error/success messages.
- `/checkout/orders/[orderId]/confirmation`: confirmation status, order number, payment status, shipping summary, and next actions to view order, continue shopping, account/orders, or retry payment.

Approved authenticated member page map:

- `/account`: anonymous sign-in/register/recovery entry or signed-in member dashboard with profile summary, recent orders, open invoices, loyalty summary, missing-address reminder, quick links, and sign out.
- `/account/profile`: profile edit form, phone verification state/action, save button, and back link.
- `/account/preferences`: marketing/privacy preferences for supported channels and save button.
- `/account/addresses`: address list, create/edit/delete, default billing/shipping actions, and checkout link when useful.
- `/account/security`: password, 2FA/passkey/phone verification where supported, and concise security notes.
- `/orders`, `/orders/[id]`, `/invoices`, `/invoices/[id]`: member order/invoice lists and details with status, date, totals, lines, payment/shipping state, invoice/document links when supported, retry-payment actions when allowed, and pagination where needed.

Navigation rules:

- Primary navigation is customer-facing only: Home, Shop, Loyalty, Help, Contact.
- Utility navigation is Account and Cart.
- Checkout is reachable from Cart or direct checkout continuation, not from primary navigation.
- Orders, invoices, profile, preferences, addresses, security, and loyalty account details live inside the member area.
- CMS content is presented as help, information, company, contact, legal, shipping, returns, terms, privacy, or cancellation content. Customers should not see "CMS" as a domain label.

Seed/menu alignment:

- Backend seed now aligns the `main-navigation` menu with Home, Shop, Loyalty, Help, and Contact, using `/help` for the help index and `/page/[slug]` for customer-facing content detail links.
- Footer fallback groups are Shop, Account, Support, and Legal. The current backend menu seed is a flat `MenuItem` structure, so grouped footer presentation remains a frontend responsibility unless WebAdmin publishes a grouped footer contract later.

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
- storefront account/auth/commerce windows now also reuse one shared spotlight-selection model for CMS pages, category lanes, and strongest product opportunities, so content-driven follow-up stays aligned across those surfaces instead of drifting route by route
- storefront account/auth/commerce windows now also reuse shared campaign-card and offer-card projection helpers on top of that spotlight model, so CMS/category/product storytelling stays aligned across those surfaces instead of rebuilding the same campaign narrative route by route
- public account hub, protected auth-required gates, and cart/checkout auth handoff now also consume those shared offer-card projections, so entry and recovery surfaces keep the same next-buy and campaign narrative instead of drifting back to route-local offer shaping
- Home now also consumes shared offer-card and category-spotlight projections for its live offer board and category campaign board, so the storefront entry route no longer keeps a separate route-local storytelling model for those core content-and-commerce web parts
- account editor, protected history, and auth-entry routes now also consume those same canonical storefront projection helpers, so member/auth surfaces keep one stable content+browse+cart prop shape instead of repeating route-local storefront wiring
- profile, preferences, addresses, and security are focused self-service forms with member-portal navigation and no storefront side rails or composition windows
- checkout is a focused conversion page with contact, address, shipping, payment handoff, order summary, and concise shopper-facing errors only
- sign-in, register, activation, and password recovery are focused account-action pages without merchandising boards or cross-surface rails
- cart renders basket review, coupon, order summary, continue-shopping, checkout CTA, empty state, and a small optional recommendation strip only
- `/account` renders either a focused anonymous account entry or a signed-in dashboard with profile, recent orders, invoices, loyalty, addresses, quick links, and sign out
- `/catalog` is a product listing with search, category/filter/sort controls, grid, pagination, and empty state only
- `/help` is the customer-facing help/content listing; `/cms` remains a compatibility redirect and is not a visible customer domain label
- Home is a concise storefront landing page with hero, category/product highlights, one loyalty teaser, help cards, and trust strip only
- public content detail lives at `/page/[slug]`; route maps, review queues, readiness sections, and sticky quick-jump rails must not be rendered to shoppers
- `/orders`, `/orders/[id]`, `/invoices`, and `/invoices/[id]` are member record pages with lists/details, payment/shipping state, documents/actions, and pagination where needed
- the protected member dashboard is a concise member portal surface; internal diagnostics stay server-side or in WebAdmin
- shared observed loader helpers now also back the main storefront, CMS/catalog route-context, member-route, and Home-discovery assembly paths, so caching plus slow/failure diagnostics are wired through one canonical server pattern instead of being hand-built per route
- shared public-discovery, member-protected, and commerce page-loader cores now also accept canonical argument normalization directly, so cache-key cleanup and loader diagnostics can stay on the reusable family-loader abstraction instead of being re-implemented per route family
- those shared public-discovery, member-protected, and commerce page-loader cores now also emit standardized family diagnostics such as normalization mode plus continuation/auth fallback footprints, so production tracing can see the operational loader shape without reopening route-local context wiring
- shell model assembly now also runs through a shared cached observable loader with explicit shell-model health summaries, so the main storefront chrome no longer keeps its own page-local diagnostics and caching branch in `layout`
- route diagnostics now also carry health summaries for content, browse, cart, and member readiness on the main storefront/member loaders, so slow or degraded route assembly is easier to trace without reopening each route’s internal fetch graph
- shared observed loaders now also emit degraded-success telemetry whenever a route or core loader returns non-`ok` health statuses even without being slow, so production/staging diagnostics can catch partial storefront/member degradation before it turns into outright failures
- cart, checkout, and confirmation now also load through shared cached observed route-context helpers with explicit commerce health summaries, so the main conversion flow reuses one canonical server assembly pattern instead of keeping route-local diagnostics and caching behavior
- member summary snapshots and browser shopping continuity now also load through shared observed helpers with explicit health summaries, so account/Home/commerce routes reuse one traceable baseline for identity, billing, and live-cart continuity instead of rebuilding those snapshots piecemeal
- canonical member GET reads now also reuse a shared request-tree cache layer, so dashboard, summary, history, and detail assembly does less duplicate profile/preferences/orders/invoices/loyalty fetch work during one render
- member order and invoice history loaders now also emit canonical paged-collection health summaries, so protected history diagnostics can distinguish item-count, total-count, and current page-window state instead of only generic route success/failure
- member order and invoice detail loaders now also emit explicit detail-availability health alongside the broader protected route summary, so diagnostics can separate storefront continuation health from missing or degraded commerce detail payloads
- public CMS/catalog/menu GET calls now also reuse canonical cached fetches and normalized cache tags, so equivalent public discovery requests avoid more duplicate work and keep cache invalidation/coherence cleaner across storefront routes
- public GET fetches now also normalize their query-string cache keys before reuse, so equivalent CMS/catalog/menu requests dedupe at the fetch layer itself instead of only sharing invalidation tags after the fact
- public CMS and catalog full-set loaders now also normalize and cache their expansion inputs per canonical query tuple, so browse/detail/inventory paths do not widen the same matching set multiple times inside one server render
- shared route-context normalization for auth, member, commerce, and page-loader family inputs now also has direct unit coverage, so canonical cache-key behavior is less likely to drift silently during loader refactors
- standardized page-loader diagnostic helpers now also have direct unit coverage, so normalization-mode and footprint telemetry is less likely to drift silently across the shared loader families
- shared member-summary, storefront-continuation, public-storefront, and member-entry loaders now also emit standardized shared-context diagnostics such as normalization mode plus compact dependency footprints, so baseline identity/storefront tracing no longer depends on reopening each loader's inline success summary
- those shared member-summary, storefront-continuation, public-storefront, and member-entry loaders now also sit on one reusable shared-context loader abstraction, so caching plus baseline diagnostic wiring no longer has to be hand-assembled in each baseline loader
- shared SEO metadata loaders now also accept canonical argument normalization directly, and Home, public-auth, member-route, commerce, CMS, and catalog SEO paths now normalize equivalent args before cache-keying so metadata assembly no longer splits on whitespace or non-canonical browse inputs
- shared SEO metadata loaders now also emit explicit normalization mode plus alternate-footprint diagnostics, so production tracing can tell whether a metadata path was canonicalized and whether language alternates were present without reopening route-local SEO assembly
- public CMS and catalog discovery now also support real query search through the current WebApi contracts, so storefront browse is no longer limited to page-local text filtering on already loaded rows
- CMS and catalog search now also preserve review/readiness context across list and detail routes, so discovery and review workflows do not lose the active search window while drilling in
- catalog browse now also evaluates image-coverage and offer-strength facets on the full matching assortment before pagination, so review counts and quick windows stay aligned with the real browse set instead of only the current page
- CMS browse now also evaluates missing-title, missing-description, and missing-both metadata windows on the full matching published set before pagination, so review counts and quick debt windows stay aligned with the real content-review set instead of only the current page
- `/cms` now also exposes direct quick windows for missing title, missing description, and missing both metadata debt, so the main CMS route can jump straight into the right debt-review window without rebuilding metadata filters manually
- shared route observability now also emits explicit outcome kinds and timing bands such as `slow-success`, `degraded-success`, `slow-degraded-success`, and `failure`, so storefront/member diagnostics can tell healthy slowness apart from degraded slowness without reopening raw loader payloads
- shared route observability now also emits compact degraded-surface footprints such as `menu:fallback` or `products:fallback|cmsPages:fallback`, so production diagnostics can point straight at the affected storefront surfaces instead of only listing raw `...Status` keys
- product detail now also carries the active catalog facet window for image coverage and offer strength through detail and related-product follow-up, so drill-in review does not drop the current browse context
- CMS detail now also carries the active metadata-focus review window through previous/next navigation, review queues, and back-to-index links, so content drill-in does not drop the current debt-review context
- CMS detail now also builds previous/next navigation, review queues, and content-navigation lists from the active filtered review set instead of the raw published-page set, so metadata-debt drill-in stays aligned with the actual review window and not only the route query
- product detail now also builds its review queue from the active filtered browse set instead of only the related-products strip, so assortment drill-in stays aligned with the actual review window and not just with cross-sell suggestions
- product detail now also surfaces previous/next navigation and current position from the active filtered browse set, so assortment review can move step-by-step through the actual catalog window from the drilled-in route
- multilingual CMS/product detail alternates plus public sitemap inventory now also load through shared cached observable loaders, so detail-page `hreflang` and sitemap discovery reuse one canonical localized snapshot instead of rebuilding alternate maps route by route
- localized public discovery inventory and public sitemap now also sit on one shared localized-discovery loader abstraction, so caching plus baseline diagnostics for multilingual inventory/sitemap paths no longer drift loader by loader
- localized discovery and sitemap observation contexts now also canonicalize multilingual culture lists before diagnostics are emitted, so culture-order drift no longer splits the same inventory/sitemap footprint into different telemetry shapes
- SEO metadata and localized sitemap/detail alternate maps now also canonicalize language-alternate ordering, so equivalent multilingual alternates no longer drift by input order across `hreflang` and sitemap assembly
- shared SEO diagnostics now also carry a culture-stable alternate footprint instead of only a raw alternate count, so production tracing can see exactly which multilingual alternates were present on a metadata path
- shared SEO diagnostic helpers now also have direct unit coverage for normalization mode, indexability, and alternate-footprint reporting, so metadata telemetry is less likely to drift silently during loader refactors
- shared localized sitemap projection now also canonicalizes alternate ordering before emitting sitemap languages, so `hreflang`-style alternates stay aligned between metadata assembly and sitemap output
- localized discovery inventory/sitemap loaders now also use standardized base/success diagnostics with direct unit coverage, so this multilingual branch no longer trails the shared SEO/page-loader abstractions in telemetry quality
- localized inventory and sitemap health summaries now also emit compact operational footprints, so production tracing can see empty-culture and sitemap-composition state without reopening raw count fields
- public sitemap static-entry assembly now also canonicalizes supported-culture input before emitting localized index routes, so duplicate or reordered culture lists no longer drift the sitemap shape
- localized public discovery projection now also has direct unit coverage for skipping invalid items while keeping alternate/sitemap output canonical, so multilingual inventory refactors are less likely to break detail alternates silently
- localized inventory health summaries now also emit a compact inventory footprint with direct unit coverage, so multilingual inventory diagnostics can show culture/item/empty-state shape without reopening raw counts
- localized inventory and alternates-map health summaries now also emit standardized summary footprints, so route-health can scan these multilingual baselines through one stable operational field per summary
- localized discovery inventory and sitemap health summaries now also emit compact summary footprints, so route-health can expose multilingual coverage and sitemap composition in one operational field instead of several separate counters
- localized discovery loader diagnostics now also prefer those standardized summary footprints directly, so multilingual success logs stay aligned with route-health instead of falling back to older raw footprint fields
- localized discovery loader diagnostics now also emit an explicit `present/empty` state for inventory and sitemap success paths, so multilingual logs say whether discovery content actually existed instead of only listing counters and footprints
- localized alternates-map health summaries now also emit a compact footprint with direct unit coverage, so multilingual detail-alternate diagnostics can show item/alternate/multi-culture shape without reopening raw counts
- culture-list-based observation contexts now also emit a compact culture footprint with direct unit coverage, so multilingual inventory/sitemap telemetry can show the canonical culture set without reopening raw arrays
- SEO metadata health summaries now also emit a compact alternate-culture footprint with direct unit coverage, so multilingual metadata diagnostics can show the canonical alternate set without reopening raw maps
- SEO metadata health summaries now also emit a compact indexability-plus-alternates summary footprint, so route-health summaries can be scanned by one operational SEO field instead of several separate metadata flags
- SEO metadata health summaries now also emit a compact target footprint for canonical path plus indexability, so route-health summaries can identify the affected metadata target without reopening route payloads
- localized discovery loader diagnostics now also emit a standardized success-summary footprint with direct unit coverage, so multilingual inventory/sitemap/alternate branches share one operational summary field in logs
- shared SEO loader diagnostics now also emit a standardized success-summary footprint with direct unit coverage, so metadata success logs can be scanned by one compact operational field instead of several separate flags
- shared SEO loader diagnostics now also emit a compact target footprint for canonical path plus indexability, so production logs can identify the affected metadata target without reopening route payloads
- shared SEO loader diagnostics now also reuse the standardized route-health SEO summaries directly, so success logs and health summaries stay aligned on one metadata truth instead of parallel formatting paths
- shared SEO diagnostics now also keep the `noindex / no-alternates` branch under direct test coverage, so private-route metadata telemetry is less likely to drift silently from the canonical SEO summaries
- localized discovery health summaries now also emit one canonical `present / empty` state for inventory, alternates, and sitemap branches, so multilingual loader diagnostics can reuse route-health state instead of re-deriving it from raw counters
- shared SEO health summaries now also emit the canonical `languageAlternateState`, so loader diagnostics can reuse route-health alternate presence directly instead of re-deriving it from counts
- shared SEO health summaries now also emit the canonical alternate summary footprint, so loader diagnostics can reuse route-health alternate formatting directly instead of rebuilding it locally
- shared SEO health summaries now also emit canonical `indexability`, so loader diagnostics can reuse the route-health visibility state directly instead of re-deriving it from `noIndex`
- shared SEO alternate helpers now also reuse the canonical metadata summary model, so alternate-footprint formatting no longer lives in a parallel helper path
- localized discovery health summaries now also emit a canonical detail footprint for inventory, alternates, and sitemap branches, so multilingual loader diagnostics can reuse one operational detail field instead of branch-specific footprint names
- direct unit coverage now also exercises private/single-locale SEO summaries and the empty sitemap summary branch, so the canonical shared health model is less likely to drift silently in non-public or empty-state scenarios
- localized discovery loader diagnostics now also have direct unit coverage for canonical detail-footprint preference and explicit empty-detail fallback, so multilingual telemetry is less likely to drift when branch-local footprint fields are refactored
- shared SEO health summaries now also emit a compact visibility footprint, so production diagnostics can scan `indexable/localized`, `indexable/single-locale`, or `noindex/private` state without combining multiple fields by hand
- shared SEO success diagnostics now also emit an explicit metadata state such as `private`, `localized`, or `single-locale`, so production logs can classify SEO paths faster than by combining indexability and alternate flags by hand
- SEO metadata health summaries now also emit that same explicit metadata state, so route-health and diagnostics classify SEO paths through one shared model instead of parallel route-local rules
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
- the home page now also surfaces explicit promotion lanes for hero offers, value offers, live offer momentum, and steady base assortment, so storefront entry can jump directly into the strongest merchandising windows instead of only generic offer/campaign teasers
- catalog and CMS campaign windows now also surface those same explicit promotion lanes, so browse/content routes can hand off directly into hero offers, value offers, live offer momentum, and base assortment windows instead of only generic campaign cards
- account and commerce storefront windows now also surface those same explicit promotion lanes, so self-service and conversion routes can jump directly into hero offers, value offers, live offer momentum, and base assortment windows instead of only generic campaign cards
- member dashboard, orders, invoices, and order/invoice detail storefront windows now also surface those same explicit promotion lanes, so protected history and detail routes can jump directly into hero offers, value offers, live offer momentum, and base assortment windows instead of only generic continuation cards
- CMS detail now also carries a content-composition window that turns the current page into an explicit reading/review/commerce journey plus route map, so public content routes no longer stop at HTML, sidebars, and campaign cards alone
- product detail now also carries a product-composition window that turns the current route into an explicit browse/review/conversion journey plus route map, so public commerce routes no longer stop at detail panels, related cards, and cross-surface rails alone
- order confirmation now also carries a confirmation-composition window that turns the after-purchase route into an explicit payment/account/storefront journey plus route map, so confirmation no longer stops at receipt panels, payment summaries, and storefront opportunity rails alone
- profile, preferences, and addresses now also carry a shared self-service composition window that turns those member routes into an explicit current-task/next-step/storefront journey plus route map, so self-service no longer stops at forms, readiness boxes, and storefront side windows alone
- security now also joins that shared self-service composition window, so the main authenticated account editor routes can all hand off through explicit current-task/next-step/storefront journeys instead of leaving password work as an isolated branch
- product detail now also carries a product-composition window that turns the current route into an explicit browse/review/conversion journey plus route map, so public commerce routes no longer stop at detail panels, related cards, and cross-surface rails alone
- storefront runtime config now also supports app-configured theme selection between a grocer-style default and an atelier-style variant through `DARWIN_WEB_THEME`, so the same front-office system can ship more than one real visual direction without changing feature code
- storefront runtime config now also supports a third `harbor` visual direction through `DARWIN_WEB_THEME`, so the same front-office system can ship grocery, editorial, and cooler hospitality-style brand expressions without changing feature code
- Darwin.Web now also includes a lightweight unit-test runner plus focused coverage for query serialization, checkout draft/input normalization, HTML fragment sanitization, safe WebApi URL resolution, and locale-routing guardrails, so critical storefront handoff and trust-boundary logic is no longer validated only through manual browsing and full builds
- Darwin.Web now also covers the page-local catalog/CMS browse lenses with focused unit tests, so visible assortment/discovery-state behavior is regression-safe instead of staying embedded only inside route composition
- Darwin.Web now also emits shared API diagnostics for public, member, and auth fetch failures, including response-level request identifiers where available, so staging/production troubleshooting is less dependent on route-local guesswork
- route-observability now also carries route/context metadata such as culture, page, slug, category, order id, and menu name across shared storefront/member loaders, so slow-operation and failure diagnostics are much easier to trace back to the exact front-office workflow in staging and production
- shared API diagnostics now also classify failures into API kind, surface family, surface area, attention level, and suggested action, so production logs point directly toward public discovery, commerce, member, auth, or shell remediation instead of only carrying raw request metadata
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
- public API cache identity now also runs through one canonical helper for normalized path, revalidate window, and key/path tags, so public fetch wiring no longer has to rebuild those cache-identity pieces in parallel
- public API fetch wiring now also reuses one canonical helper for GET cache options versus mutation no-store behavior, so cache policy and fetch-mode decisions no longer drift apart inside public JSON fetch paths
- public API fetch wiring now also reuses one canonical request-plan helper for normalized path, request URL, and fetch cache mode, so path/url/cache assembly no longer has to be rebuilt inline inside the public JSON fetch path
- public JSON fetch wiring now also uses canonical helpers for request headers and cached GET keys, with direct unit coverage, so request-shape and cache-key behavior are less likely to drift behind inline fetch code
- public JSON fetch wiring now also uses canonical helpers for failure message-key mapping and degraded result shaping, so shopper-facing fallback copy is less likely to drift behind inline status branches
- public JSON fetch wiring now also uses canonical helpers for HTTP-status classification and success-result shaping, so response branches are less likely to drift behind inline fetch handling
- public JSON fetch wiring now also uses a canonical failure-logging policy helper, so degraded public fetches keep one shared rule for which failure kinds stay quiet and which ones emit operational diagnostics
- public JSON fetch wiring now also uses canonical helpers for response and network diagnostics, so public fetch observability is less likely to drift behind inline response/error branches
- public JSON fetch wiring now also uses canonical outcome helpers for success, response-failure, invalid-payload, and network-failure branches, with direct unit coverage, so public fetch orchestration is less likely to drift behind inline result-shaping paths
- public JSON fetch wiring now also uses one canonical failure-logging helper around those shared outcomes, with direct unit coverage, so public fetch observability is less likely to drift behind inline log/no-log branches
- public JSON fetch wiring now also uses canonical helpers for request-init assembly and POST body shaping, with direct unit coverage, so public request execution is less likely to drift behind inline GET/POST wiring
- public JSON fetch wiring now also uses canonical helpers for execution context and cached GET args, with direct unit coverage, so request URL, normalized path, and cache-wrapper identity are less likely to drift behind inline fetch orchestration
- public JSON fetch wiring now also uses one canonical response-parser helper for success and invalid-payload branches, with direct unit coverage, so public response handling is less likely to drift behind inline JSON parsing paths
- public JSON fetch wiring now also routes fetch-success completion through one canonical execution-success helper, with direct unit coverage, so executor branches are less likely to drift behind inline success-finalization logic
- public JSON fetch wiring now also routes response-context dispatch through one canonical parser helper, with direct unit coverage, so response-failure and payload-parse branches are less likely to drift behind inline parse orchestration
- public JSON fetch wiring now also routes parsed-failure completion through one canonical failure-result helper, with direct unit coverage, so finalize branches are less likely to drift behind inline failure dispatch
- public JSON fetch wiring now also routes response-context parse-plus-finalize through one canonical result helper, with direct unit coverage, so execution success branches are less likely to drift behind inline response-result orchestration
- public JSON fetch wiring now also derives request-context, URL, and init through one canonical execution-plan helper, with direct unit coverage, so executor branches are less likely to drift behind inline execution planning
- public JSON fetch wiring now also routes fetch dispatch through one canonical execution-plan executor, with direct unit coverage, so request execution is less likely to drift behind inline try/catch orchestration
- public JSON fetch wiring now also routes WebApi fetch-wrapper shaping through one canonical fetcher helper, with direct unit coverage, so request-init wrapping is less likely to drift behind inline lambda dispatch
- public JSON fetch wiring now also routes status-based response degradation through one canonical response-status failure helper, with direct unit coverage, so response-failure shaping is less likely to drift behind inline status branches
- public JSON fetch wiring now also routes degraded outcome shaping through one canonical failure-outcome helper, with direct unit coverage, so response/network failure branches are less likely to drift behind duplicate result assembly
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
- orders, invoices, and their detail routes now also reuse shared storefront offer-card projections, so member commerce history/detail surfaces keep one stable product-storytelling model instead of rebuilding offer messaging route by route
- the authenticated member dashboard now also reuses that same shared storefront offer-card projection, so `/account` tells the same product-opportunity story as member history/detail surfaces instead of keeping a separate dashboard-only offer format
- the authenticated member dashboard plus the shared account-editor storefront window now also reuse shared CMS-page and category-spotlight card projections, so member/self-service surfaces no longer rebuild content-and-browse teaser cards route by route
- orders, invoices, and their detail routes now also reuse those same shared CMS-page and category-spotlight card projections, so member commerce routes no longer keep a separate local teaser-card shape for storefront content and browse follow-up
- the shared commerce storefront window plus the public auth continuation rail now also reuse shared CMS-page and category-spotlight card projections, so public conversion and auth-entry surfaces no longer keep their own local teaser-card shapes for storefront content and browse follow-up
- the public account hub action center and guest order-confirmation offer board now also reuse shared storefront spotlight/offer projections, so entry and after-purchase surfaces no longer keep their own local highlight shaping for content, browse, and next-buy follow-up
- catalog and CMS campaign windows now also reuse shared category-campaign and product-campaign card projections, so discovery storytelling no longer rebuilds those campaign cards route by route
- CMS index and CMS detail now also reuse one shared storefront-support window for category lanes, product follow-up, and cart continuity, so those support panels no longer drift route by route
- account hub, protected auth-required, and the shared commerce storefront window now also reuse one storefront offer-board web part, so next-buy panels no longer keep separate route-local rendering shapes
- account storefront windows, commerce storefront windows, and CMS storefront-support panes now also reuse one shared spotlight-board web part for CMS/category teaser cards, so content-and-browse follow-up no longer keeps separate route-local rendering shapes across those surfaces
- account, auth, commerce, catalog, and CMS campaign windows now also reuse one shared campaign-board web part for category/product campaign cards, so commercial storytelling no longer keeps separate route-local rendering shapes across those surfaces
- member order and invoice history routes now also reuse one shared member-storefront window for CMS/category/product follow-up, so protected commerce history no longer keeps separate route-local storefront window markup
- member order and invoice detail routes now also reuse that same shared member-storefront window, so protected commerce detail no longer keeps a separate storefront-window markup branch either
- the protected member dashboard now also reuses that same shared member-storefront window, so the remaining storefront-follow-up branch on `/account` no longer keeps route-local CMS/category/product window markup
- shared observed loaders now also normalize equivalent CMS/catalog browse and review arguments before cache-keying them, so route-context caching no longer drifts across whitespace, invalid lens values, or unsanitized review-window inputs
- shared route-context loaders now also normalize equivalent member, auth, and commerce route arguments before cache-keying them, so page/id/order-number drift no longer creates avoidable cache splits across protected and conversion flows
- route-context normalization for auth, member, and commerce is now backed by direct unit coverage, so canonical cache-key behavior on trimmed cultures, bounded paging, and cleaned order/id references is no longer only covered indirectly through page builds
- member summary loaders and public storefront continuation contexts now also consume that same canonical normalization layer, so paging- and culture-equivalent summary/continuation requests reuse one cache path instead of widening shared baseline context work
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
- `/page/[slug]` should be tested against real published slugs such as `ueber-uns` and `faq`; `/cms` is not a customer-facing detail route label
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
- shared SEO metadata diagnostics now also separate alternate detail from alternate summary and emit a compact visibility footprint, so production tracing can scan localized, single-locale, or private metadata state without mixing per-culture detail and roll-up summary in one field
- shared SEO diagnostics helpers now also have direct coverage for canonical alternate detail and visibility footprints, so summary/detail metadata state is less likely to drift behind the route-level SEO loader tests
- shared SEO diagnostics helpers now also cover canonical target and summary footprints directly, so metadata success diagnostics no longer depend on route-local string shaping for target identity or alternate roll-up state
- shared SEO diagnostics helpers now also classify canonical metadata state and alternate presence directly, so loader-level SEO diagnostics no longer depend on inline localized/private state branching
- shared SEO diagnostics helpers now also classify canonical indexability directly, so the full metadata visibility model no longer depends on inline `noindex` branching inside loader success diagnostics
- localized discovery diagnostics helpers now also have direct coverage for canonical state/detail/summary selection, so multilingual inventory and sitemap telemetry are less likely to drift behind loader-level tests
- route observability helpers now also have direct coverage for timing-band, signal-kind, attention-level, and suggested-action classification, so operational tracing is less likely to drift behind only end-to-end slow/degraded/failure tests
- shared API diagnostics helpers now also have direct coverage for API kind, operational surface, retryability, attention level, and suggested action classification, so failure telemetry is less likely to drift behind only response-level integration tests
- shared-context diagnostics now also expose canonical normalization-mode helpers and direct loader-wiring coverage, so baseline member/storefront context telemetry is less likely to drift behind cache-only tests
- page-loader diagnostics now also expose a canonical normalization-mode helper plus direct edge-case coverage for continuation and protected-route footprints, so shared `public/member/commerce` loader telemetry is less likely to drift behind only loader-level tests
- shared `public/member/commerce` page-loader families now also have direct wiring coverage for normalized context and success diagnostics, so canonical loader telemetry is less likely to drift behind helper-only tests
- shared SEO loaders now also expose a canonical normalization-mode helper plus direct wiring coverage for normalized observation context, so metadata telemetry is less likely to drift behind summary-only helper tests
- localized discovery loaders now also expose a canonical culture-normalization helper plus direct wiring coverage for context and success diagnostics, so multilingual telemetry is less likely to drift behind summary-only helper tests
- shared API diagnostics now also expose a canonical failure-dedupe key with direct log-deduplication coverage, so production error telemetry is less likely to drift into duplicate noise during refactors
- route observability now also exposes canonical helpers for operation identity and degraded-surface footprints, so slow/degraded telemetry is less likely to drift behind only end-to-end observe-operation tests
- route-health now also exposes canonical SEO visibility/alternate-state helpers with direct unit coverage, so shared metadata summaries are less likely to drift behind aggregate route-health tests
- public API cache policy now also exposes canonical revalidate and path-tag helpers with direct unit coverage, so cache identity is less likely to drift behind only end-to-end fetch tests
- public JSON fetch wiring now also uses one canonical response parser for success, response-failure, and invalid-payload branches, so storefront degradation handling is less likely to drift behind inline JSON parsing paths
- public JSON fetch execution now also uses one canonical request executor for fetch, parse, network-failure, and failure-logging branches, so storefront API wiring is less likely to drift behind inline request orchestration
- public JSON fetch planning now also uses one canonical WebApi execution-context helper, so request URL, normalized path, and cache-aware init stay aligned instead of being rebuilt inline in the storefront fetch path
- public JSON parsed-response shaping now also uses canonical success/failure helpers, so response handling is less likely to drift behind ad hoc parsed-result object construction
- public JSON parsed-response helpers now also have direct regression coverage, so canonical success/failure shaping is less likely to drift silently during fetch-layer refactors
- public JSON request execution now also finalizes parsed responses and network failures through canonical helpers, so fetch-layer orchestration is less likely to drift behind inline success/failure completion branches
- public JSON response-bound handling now also flows through one canonical response context for diagnostics, success, and degraded outcomes, so repeated `key/path/response` execution state is less likely to drift behind inline fetch branches
- public JSON request-bound handling now also flows through one canonical request context for parsing and network-failure outcomes, so repeated `key/path` execution state is less likely to drift behind shared fetch branches
- public JSON response-status degradation now also runs through one canonical response-failure helper, so HTTP failure classification is less likely to drift behind inline parse branches
- public JSON invalid-payload degradation now also runs through one canonical parse-failure helper, so malformed JSON handling is less likely to drift behind inline parse branches
- public JSON parsed-response completion now also runs through one canonical completion helper, so finalize-path result extraction is less likely to drift behind inline completion branches
- public JSON degraded-outcome finalization now also runs through one canonical failure finalizer, so logging and result completion are less likely to drift between parse-failure and network-failure branches
- public JSON degraded outcome shaping now also runs through one canonical failure-outcome helper, with direct unit coverage, so response and network failure branches are less likely to drift behind duplicate failure-result assembly
- public JSON parsed-response dispatch now also runs through one canonical failure-detector helper, with direct unit coverage, so success and failure finalization are less likely to drift behind inline branch checks
- public JSON fetch dispatch now also runs through one canonical execution helper, with direct unit coverage, so success and network-failure execution branches are less likely to drift behind inline request try/catch orchestration
- public JSON response-context parse dispatch now also runs through one canonical parsed-response helper, with direct unit coverage, so response-failure and payload-parse branches are less likely to drift behind inline parse selection
- public JSON response-context finalize flow now also runs through one canonical finalizer helper, with direct unit coverage, so parse-plus-finalize orchestration is less likely to drift behind inline response-context completion
- public JSON execution success wiring now also runs through one canonical response-result helper, with direct unit coverage, so response-to-result completion is less likely to drift behind inline success-path response-context assembly
- public JSON top-level request and parse wrappers now also run through canonical plan/context helpers, with direct unit coverage, so high-level execution and parse entry points are less likely to drift behind duplicate wrapper assembly
- public JSON execution success context now also runs through one canonical response-context helper, with direct unit coverage, so request-context-plus-response mapping is less likely to drift behind inline success-path assembly
- public JSON WebApi planning now also runs through one canonical request-plan helper, with direct unit coverage, so public request URL and cache-policy planning are less likely to drift behind inline WebApi wrapper assembly
- public JSON request-body shaping now also runs through one canonical body helper, with direct unit coverage, so mutation payload serialization is less likely to drift behind inline POST init assembly
- public JSON header shaping now also runs through one canonical content-type decision helper, with direct unit coverage, so JSON request headers are less likely to drift behind inline body checks
- public JSON success shaping now also runs through one canonical success helper, so valid JSON handling is less likely to drift behind inline parse branches
- public JSON payload parsing now also runs through one canonical payload parser, so valid and malformed JSON branches are less likely to drift behind inline parse orchestration
- public JSON execution success now also runs through one canonical execution-success helper, so fetch-success completion is less likely to drift behind inline executor branches
- shared multilingual discovery projections now also have direct regression coverage for alternates and sitemap assembly, so recent performance refactors around localized inventory reuse are less likely to drift silently behind route-level tests
- shared public paged-set expansion now also runs through one tested helper for CMS and catalog, so discovery-set loaders keep one consistent rule for when to widen a partial first page into the full matching set and when to fall back safely




 - Public JSON header shaping now also runs through one canonical content-type decision helper, with direct unit coverage, so JSON mutation headers are less likely to drift behind inline body checks.
 - Public JSON fetcher wiring now also runs through one canonical request-init helper, with direct unit coverage, so request-init shaping is less likely to drift behind inline builder branches.
 - Public JSON request-init merge now also runs through one canonical shape helper, with direct unit coverage, so cache options, init overrides, and final headers are less likely to drift behind inline object assembly.
 - Public JSON execution-context shaping now also runs through one canonical helper, with direct unit coverage, so request URL, normalized path, and final request-init are less likely to drift behind inline object assembly.
 - Public JSON execution-plan shaping now also runs through one canonical helper, with direct unit coverage, so request context, request URL, and final request-init are less likely to drift behind inline object assembly.
 - Public JSON mutation-init shaping now also runs through one canonical helper, with direct unit coverage, so POST method/body wiring is less likely to drift behind inline object assembly.
 - Public JSON request and response context shaping now also runs through canonical helpers, with direct unit coverage, so key/path/response wiring is less likely to drift behind inline object assembly.
- Public JSON response-failure detection, response-context parse dispatch, and parsed-response result dispatch now run through canonical helpers with direct unit coverage, so HTTP degradation branches are less likely to drift behind inline status checks or parse/finalize-time branching.
- storefront confirmation and member order detail now receive payment attempts with canonical creation timestamps and newest-first ordering from the backend contract, so latest payment-attempt state no longer depends on implicit array order or single-attempt heuristics.

- Done: cart, checkout, confirmation, account composition, product detail, and CMS detail now also expose explicit promotion-lane route maps into hero/value/live/base catalog windows, so the main commerce and drill-in surfaces can jump straight into real merchandising lanes instead of only generic content or browse follow-up

- Done: catalog index, CMS index, public auth composition, and the public account-hub composition window now also expose explicit promotion-lane route maps into hero/value/live/base catalog windows, so the main entry and index surfaces can jump straight into real merchandising lanes instead of only generic follow-up cards

- Done: route-health test fixtures now also validate merchandising lane diagnostics directly, so shared storefront observability for hero/value/live/base coverage is less likely to drift behind count-only expectations
- cart, checkout, and confirmation page-health summaries now also carry explicit promotion-lane counts and a compact merchandising footprint, so commerce-route diagnostics can see hero/value/live/base lane coverage directly instead of only generic follow-up counts.
- Home route success diagnostics now also carry explicit storefront merchandising lane counts and a compact promotion-lane footprint, so the main storefront entry is no longer behind commerce/account surfaces in operational visibility.
- Home entry composition now also exposes an explicit promotion-lane route-map step for hero/value/live/base catalog windows, so the main storefront entry can jump directly into the strongest merchandising lane instead of only generic browse or review follow-up.
- Catalog index page-health now also carries explicit live/base lane counts plus a compact promotion-lane footprint, and catalog/CMS index page-health summaries now have direct route-health coverage, so discovery-route diagnostics are less likely to drift behind loader-only expectations.
- The protected auth-required member entry now also exposes an explicit promotion-lane board for hero/value/live/base catalog windows, so protected entry fallback no longer stops at generic offer cards and can hand off directly into real merchandising lanes.
- The guest commerce auth handoff now also exposes an explicit promotion-lane board for hero/value/live/base catalog windows, so pre-auth cart and checkout interruptions no longer stop at generic offer cards and can jump directly into real merchandising lanes.
- Direct component coverage now also validates promotion-lane rendering on protected member-entry fallback and guest commerce auth handoff, so those merchandising handoff surfaces are less likely to drift behind build-only validation.
- Member dashboard, editor, collection, and detail route-health summaries now also carry compact workflow footprints, so protected portal diagnostics can read address/order/invoice/detail flow state directly instead of only isolated status and count fields.
- Done: public auth and protected member-entry route-health summaries now also carry compact workflow footprints, so entry-route diagnostics can read route/session/cart lane state directly instead of only isolated storefront counts.
- Done: account, cart, checkout, and confirmation route-health summaries now also carry compact workflow footprints, so entry and conversion diagnostics can read session/address/order/invoice lane state directly instead of only isolated counts.
- Done: catalog detail, CMS detail, product follow-up, and CMS route-health summaries now also carry compact workflow footprints, so drill-in diagnostics can read product/page and related-follow-up state directly instead of only isolated has/count fields.
- Done: catalog and CMS index route-health summaries now also carry compact browse workflow footprints, so discovery diagnostics can read mode, matching-set state, visible window, and review or promotion context directly instead of only isolated counters.
- Done: Home and shared commerce route-health summaries now also carry compact workflow footprints, so storefront entry and conversion diagnostics can read session, cart, order, invoice, confirmation, and merchandising-lane state directly instead of only isolated counters.
- Done: home discovery, confirmation result, and paged member-collection summaries now also carry compact workflow footprints, so storefront entry, after-purchase, and paged collection diagnostics can read state directly instead of only isolated counters.
- Done: localized discovery inventory, alternates, sitemap, and SEO metadata summaries now also carry compact workflow footprints, so multilingual and SEO diagnostics can read state directly instead of reconstructing it from several separate footprint fields.
- Done: public CMS search windows and catalog review/search windows now use path-aware cache revalidation instead of key-only timing, so storefront performance tuning can distinguish stable discovery feeds from more volatile search and merchandising slices.
- Done: public CMS expanded page-set windows and catalog category or full-set browse windows now also use path-aware cache revalidation, so discovery performance can keep volatile review and merchandising surfaces fresher than the baseline stable feeds.
- Done: member dashboard, editor, and collection route-health now carry merchandising-aware workflow footprints, and direct route-health coverage now validates those richer member workflow summaries so protected portal diagnostics do not drift behind count-only expectations.
- Done: Home now renders promotion lanes as a real merchandising card board instead of a status list, so hero, value, live, and base windows read like first-class storefront campaigns and align with the other main promotion surfaces.
- Done: direct coverage now validates that Home renders promotion lanes as a card-grid merchandising board while keeping route-map and readiness web-part shapes intact, so the main storefront entry is less likely to drift back into a status-only lane surface.
- Done: product detail now also includes a dedicated storefront support window for CMS follow-up, catalog browse lanes, live product follow-up, and cart or checkout continuity, so drilled-in catalog routes no longer rely only on composition and related-product blocks for storefront continuation.
- catalog browse now also exposes a dedicated storefront support window with published CMS follow-up, live product follow-up, and cart continuity, so `/catalog` no longer stops at composition plus campaign-only browse support
- catalog browse diagnostics now also carry an explicit support-window footprint for CMS follow-up, product follow-up, and cart continuity, so `/catalog` support context is readable as one workflow signal instead of scattered counts
- direct coverage now also checks the dedicated catalog and product storefront support windows, so those new browse/detail support surfaces do not rely only on lint and build for regression detection
- public API cache policy now also keeps CMS detail and product-detail routes on fresher path-aware windows, so storefront drill-in pages do not lag behind browse surfaces after the recent composition/support expansion
- product-detail and CMS-detail loaders now also emit explicit support-workflow footprints, so detail-route tracing can see related/review follow-up state directly instead of only core page/product status
- member dashboard/editor/collection diagnostics now also carry a compact storefront-support footprint, so CMS/category/product/cart support state is readable as one signal instead of being inferred from separate counts

- Added cart/checkout continuity sections to account and commerce storefront windows so authenticated self-service and commerce routes keep explicit purchase-continuation support alongside CMS, browse, and promotion lanes.
- Added storefront-support diagnostics to account and commerce route-health summaries so CMS/category/product/cart continuity is visible as a compact operational footprint, not just lane counters.
- Moved member dashboard cart/checkout continuity into the shared member storefront window and added direct coverage, giving dashboard storefront support the same integrated shape as account and commerce families.
- Extended the shared member storefront window on order and invoice detail routes with cart/checkout continuity backed by protected-route storefront cart context, so drill-in member commerce no longer depends only on content/browse/product follow-up.
- Added a reusable storefront-support footprint helper and wired order/invoice detail loaders to emit compact support diagnostics, keeping new member drill-in continuity observable as first-class route metadata.
- Extended shared member storefront support on order and invoice history routes with protected-route cart/checkout continuity, so member history surfaces stay aligned with dashboard and detail continuity.
- Tightened path-aware cache windows for paged CMS and catalog discovery so deeper browse pages refresh faster and stay closer to live storefront drift.
- Added promotion-lane boards to catalog and CMS storefront support windows, bringing explicit hero/value/live/base merchandising into browse/content support surfaces alongside product and cart continuity.

- Added catalog/CMS browse and support workflow footprints so route-health reflects support windows, promotion lanes, and cart continuity directly.

- Added explicit promotion-lane board to product detail storefront support so drill-in merchandising parity matches other major storefront support windows.

- Added a promotion-lane board to guest confirmation so after-purchase guest journeys expose hero/value/live/base windows directly.

- Added confirmation follow-up workflow footprint so after-purchase guest merchandising can be traced directly in route-health.

- Added direct component coverage for guest confirmation promotion lanes to protect the after-purchase merchandising surface from silent regressions.

- Expanded storefront window tests so member/account/commerce coverage protects explicit promotion-lane boards as well as cart continuity.

- Added storefront support footprints to auth entry, account, protected member entry, and home route-health summaries for more business-readable diagnostics.

- Added commerce storefront support footprints for cart, checkout, and confirmation so diagnostics separate support continuity from the broader workflow roll-up.
- Tightened path-aware cache windows for deeper paginated catalog browse so page-two-plus product discovery refreshes faster than the steadier first-page window.
- Expanded direct component coverage for catalog storefront support so explicit promotion-lane boards are protected alongside CMS/product/cart continuity.
- Added dedicated support-workflow helpers plus direct unit coverage for product and CMS detail loaders, keeping drill-in follow-up diagnostics readable and less dependent on route-health-only rollups.
- Added dedicated support-workflow helpers plus direct unit coverage for catalog and CMS index loaders, keeping browse-route support diagnostics readable beyond route-health-only rollups.
- Added a dedicated storefront-support helper plus direct unit coverage for member route loaders, keeping protected dashboard/editor/collection/detail diagnostics readable beyond route-health-only rollups.
- Added dedicated storefront-support helpers plus direct unit coverage for account and commerce route loaders, keeping entry and conversion diagnostics readable beyond route-health-only rollups.
- Added dedicated storefront-support helpers plus direct unit coverage for public auth route/page loaders and protected member-entry context, keeping auth-entry diagnostics readable beyond route-health-only rollups.
- Expanded route-health coverage so storefront-support footprints for home/auth/account/member-entry/commerce are validated directly, not only through helper-level tests.
- Closed another observability gap: the Home route loader now emits an explicit storefront-support footprint through a dedicated helper with direct unit coverage, so Home success diagnostics do not rely only on route-health rollups.
- Closed another test-depth gap: PublicAuthContinuation now has direct render coverage for promotion lanes plus cart/CMS/catalog continuity instead of relying only on page-level build safety.
- Closed another performance slice: large and paged catalog-category feeds now use shorter path-aware cache windows instead of the long-lived baseline.
- Closed another observability gap: Home discovery now emits an explicit storefront-support footprint through a dedicated helper with direct unit coverage, so the public storefront entry is observable beyond route-health rollups.
- Closed another test-depth gap: the public account hub page now has direct render coverage for action-center follow-up, cart continuity, and promotion lanes instead of relying only on inner-window tests.
- Closed another test-depth gap: the public CMS index page now has direct render coverage for composition, storefront support, promotion lanes, and cart continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the public catalog page now has direct render coverage for composition, storefront support, promotion-lane follow-up, and cart continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the product detail page now has direct render coverage for composition, storefront support, promotion-lane follow-up, and cart continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the CMS detail page now has direct render coverage for composition, storefront support, promotion-lane follow-up, and cart continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the member dashboard page now has direct render coverage for storefront support, promotion lanes, and cart/checkout continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the checkout page now has direct render coverage for conversion support, promotion lanes, and cart/account continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the orders history page now has direct render coverage for storefront support, promotion lanes, and cart/checkout continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the invoices history page now has direct render coverage for storefront support, promotion lanes, and cart/checkout continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: the order detail page now has direct render coverage for storefront support, promotion lanes, and cart/checkout continuity instead of relying only on inner-window tests.
- Closed another test-depth gap: sign-in, register, activation, and password-recovery pages now have direct render coverage, so the main public-auth family no longer relies only on continuation/composition window tests.
- Closed another test-depth gap: catalog and CMS campaign windows now have direct render coverage, so promotion-lane campaign surfaces no longer rely only on page-level or support-window coverage.
- Closed a storefront-readiness audit pass: local development seed now covers the core front-office graph for realistic testing, including demo users with default addresses, catalog products and categories, CMS pages, campaigns, shipping methods and rates, promotions such as `WELCOME10`, sample carts, and seeded orders with payments/shipments/refunds.
- Closed a storefront-flow audit pass: the non-loyalty front-office path is now explicitly confirmed end-to-end in code, covering product display, add-to-cart, cart mutation, coupon apply/clear, checkout-intent creation, order placement, hosted payment handoff creation, payment completion reconciliation, and confirmation rendering.
- Closed a storefront smoke-coverage gap: integration tests now protect discovery seed availability plus the anonymous commerce path from cart mutation and seeded coupon application through checkout, payment intent creation, payment completion, and confirmation.
- Closed the local hosted-checkout readiness gap: development payment handoff now returns into a real /mock-checkout route in Darwin.Web, which can drive explicit success, cancellation, and failure outcomes back into confirmation-finalize reconciliation.
- Closed a test-depth gap on local hosted checkout: `/mock-checkout` now has direct render coverage for explicit success, cancellation, and failure reconciliation handoff links.
- Closed another test-depth gap on local hosted checkout: `/mock-checkout` route helpers now have direct unit coverage for bounded search parsing plus success/cancellation/failure finalize URL building.

