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

If `Darwin.WebApi` is running locally for storefront work, prefer its `http` launch profile while `Darwin.Web` points at `http://localhost:5134`. The web app now keeps local TLS handling explicit, but the clean development path is still to avoid forcing HTTP storefront calls through HTTPS redirection when the local certificate is not trusted.

Build and run the production build:

```bash
npm run build
npm run start
```

Run the focused unit tests:

```bash
npm run test
```

Open [http://localhost:3000](http://localhost:3000).

## Current Project State

The repository has moved beyond the raw front-office starting line:

- the default starter page has been replaced by a real storefront shell
- the app now has theme isolation, navigation composition, and working storefront routes for CMS, catalog, cart, and checkout
- CMS-backed navigation is wired with a safe fallback so frontend work is not blocked when the local API or menu seed is unavailable
- CMS fallback is now explicitly observable in the shell so menu/API problems are visible during development and staging
- shell navigation now also loads through a shared cached main-navigation context with timing diagnostics, so repeated layout composition does less duplicate menu work while still making slow menu/API behavior visible
- shell menu normalization, safe-link filtering, and fallback-message selection now also run through shared tested helpers, so header navigation feedback stays consistent when CMS menus are empty, missing, or partially unsafe
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
- storefront account/auth/commerce windows now also reuse one shared spotlight-selection model for CMS pages, category lanes, and strongest product opportunities, so content-driven follow-up narratives do not drift surface by surface
- storefront account/auth/commerce windows now also reuse shared campaign-card and offer-card projection helpers on top of that spotlight model, so CMS/category/product follow-up storytelling stays aligned across those surfaces instead of rebuilding the same narrative route by route
- storefront entry surfaces such as the public account hub, protected auth-required gates, and cart/checkout auth handoff now also consume those shared offer-card projections, so high-intent entry and recovery routes no longer drift away from the same campaign and next-buy storytelling model
- Home now also consumes shared offer-card and category-spotlight projections for its live offer board and category campaign board, so the storefront entry surface no longer keeps a separate route-local storytelling model for those content-and-commerce web parts
- account editor, protected history, and auth-entry routes now also consume those same canonical storefront projection helpers, so member/auth surfaces keep one stable content+browse+cart prop shape instead of repeating route-local storefront wiring
- shared observed loader helpers now also back the main storefront, CMS/catalog route-context, member-route, and Home-discovery assembly paths, so caching plus slow/failure diagnostics are wired through one canonical server pattern instead of being hand-built per route
- shared public-discovery, member-protected, and commerce page-loader cores now also accept canonical argument normalization directly, so cache-key cleanup and loader diagnostics can stay on the reusable family-loader abstraction instead of being re-implemented per route family
- those shared public-discovery, member-protected, and commerce page-loader cores now also emit standardized family diagnostics such as normalization mode plus continuation/auth fallback footprints, so production tracing can see the operational loader shape without reopening route-local context wiring
- shell model assembly now also runs through a shared cached observable loader with explicit shell-model health summaries, so the main storefront chrome no longer keeps its own page-local diagnostics and caching branch in `layout`
- route diagnostics now also carry health summaries for content, browse, cart, and member readiness on the main storefront/member loaders, so slow or degraded route assembly is easier to trace without reopening each route’s internal fetch graph
- shared observed loaders now also emit degraded-success telemetry whenever a route or core loader returns non-`ok` health statuses even without being slow, so production/staging diagnostics can catch partial storefront/member degradation before it turns into outright failures
- cart, checkout, and confirmation now also load through shared cached observed route-context helpers with explicit commerce health summaries, so the main conversion flow reuses one canonical server assembly pattern instead of keeping route-local diagnostics and caching behavior
- member summary snapshots and browser shopping continuity now also load through shared observed helpers with explicit health summaries, so account/Home/commerce routes reuse one traceable baseline for identity, billing, and live-cart continuity instead of rebuilding those snapshots piecemeal
- member portal GET endpoints now also reuse a shared request-tree cache layer for canonical profile/preferences/addresses/orders/invoices/loyalty reads, so dashboard, summary, history, and detail assembly does less duplicate member fetch work during one render
- member order and invoice history loaders now also emit canonical paged-collection health summaries, so diagnostics can distinguish item-count, total-count, and current page-window state on protected history routes instead of only seeing generic success/failure
- member order and invoice detail loaders now also emit explicit detail-availability health alongside the broader protected route summary, so diagnostics can separate storefront continuation health from missing or degraded commerce detail payloads
- public CMS/catalog/menu GET calls now also reuse canonical cached fetches and normalized cache tags, so equivalent public discovery requests avoid more duplicate work and keep cache invalidation/coherence cleaner across storefront routes
- public GET fetches now also normalize their query-string cache keys before reuse, so equivalent CMS/catalog/menu requests dedupe at the fetch layer itself instead of only sharing invalidation tags after the fact
- public CMS and catalog full-set loaders now also normalize their expansion inputs and reuse one cached request-tree result per canonical query tuple, so browse/detail/inventory routes do not re-expand the same matching set multiple times during one render
- shared route-context normalization for auth, member, commerce, and page-loader family inputs now also has direct unit coverage, so canonical cache-key behavior is less likely to drift silently during loader refactors
- standardized page-loader diagnostic helpers now also have direct unit coverage, so normalization-mode and footprint telemetry is less likely to drift silently across the shared loader families
- shared member-summary, storefront-continuation, public-storefront, and member-entry loaders now also emit standardized shared-context diagnostics such as normalization mode plus compact dependency footprints, so baseline identity/storefront tracing no longer depends on reopening each loader's inline success summary
- those shared member-summary, storefront-continuation, public-storefront, and member-entry loaders now also sit on one reusable shared-context loader abstraction, so caching plus baseline diagnostic wiring no longer has to be hand-assembled in each baseline loader
- shared SEO metadata loaders now also accept canonical argument normalization directly, and Home, public-auth, member-route, commerce, CMS, and catalog SEO paths now normalize equivalent args before cache-keying so metadata assembly no longer splits on whitespace or non-canonical browse inputs
- shared SEO metadata loaders now also emit explicit normalization mode plus alternate-footprint diagnostics, so production tracing can tell whether a metadata path was canonicalized and whether language alternates were present without reopening route-local SEO assembly
- catalog and CMS index routes now also support real public query search through the current WebApi contracts, so storefront discovery is no longer limited to page-local text filtering on already loaded items
- catalog and CMS search flows now also preserve readiness lenses and review context across list/detail movement, so discovery and review do not lose the active query window while drilling into pages or products
- catalog browse now also builds image-coverage and offer-strength facets from the full matching assortment before pagination, so counts, quick-review windows, and visible browse context stay aligned with the real assortment instead of only the current page
- CMS browse now also carries metadata-focus windows such as missing-title, missing-description, and missing-both across the full matching published set before pagination, so debt counts, quick-review actions, and detail follow-up stay aligned with the real content-review window instead of only the current page
- `/cms` now also exposes direct quick windows for missing title, missing description, and missing both metadata debt, so the main CMS route can jump straight into the right debt-review window without rebuilding metadata filters manually
- shared route observability now also emits explicit outcome kinds and timing bands such as `slow-success`, `degraded-success`, `slow-degraded-success`, and `failure`, so storefront/member diagnostics can distinguish healthy slowness from degraded slowness without reopening raw loader payloads
- shared route observability now also emits compact degraded-surface footprints such as `menu:fallback` or `products:fallback|cmsPages:fallback`, so production diagnostics can point straight at the affected storefront surfaces instead of only listing raw `...Status` keys
- product detail now also preserves catalog facet context such as image coverage and offer strength while drilling in and back out, so review/browse continuity between `/catalog` and `/catalog/[slug]` no longer loses the active assortment window
- product detail now also derives its review queue from the full active filtered browse set instead of only the related-products strip, so assortment drill-in stays aligned with the real review window and not just with cross-sell cards
- product detail now also surfaces previous/next navigation and in-window position from the active filtered browse set, so assortment review can step through the real catalog window directly from the drilled-in route
- CMS detail now also preserves metadata-focus review context while moving through previous/next pages, review queues, and back to `/cms`, so content drill-in no longer drops the active debt-review window
- CMS detail now also derives previous/next navigation, review queues, and content-navigation lists from the full active metadata-review set instead of the raw published-page set, so debt-review drill-in stays aligned with the real filtered window and not just the URL state
- the root HTML shell now suppresses hydration warnings for extension-mutated `<html>` attributes, so storefront startup stays stable when browser tooling injects classes before React hydrates
- shell navigation now also passes the active culture into CMS menu loading and can project the seeded `Footer` menu into the rendered footer, so seed-driven navigation labels/links stay aligned with the current storefront language instead of defaulting silently
- when a CMS `Footer` menu is available it is now treated as the authoritative footer navigation, and the fallback footer itself now carries storefront/legal links instead of platform/debug placeholders
- shared public-API fallback copy is now shopper-friendly instead of infrastructure-facing, so degraded CMS/catalog/cart surfaces say that part of the storefront is temporarily unavailable instead of exposing raw API wording
- the authoritative footer seed now also includes `Contact` alongside the German legal pages, so service navigation is not reduced to compliance-only links
- raw shell fallback/debug copy is no longer rendered in the header, utility actions are now icon-first, and the language switcher now uses compact two-letter codes, so the main storefront chrome stays cleaner and less noisy
- localized query-message resolution now also falls back across the shared resource bundle, so public API degradation shows real shopper-facing copy instead of leaking `i18n:*` keys into the UI
- Home spotlight empty states now also resolve localized degraded API messages before rendering, so CMS/product spotlight sections no longer leak raw `i18n:*` keys
- the seeded footer-linked legal CMS pages (`impressum`, `datenschutz`, `agb`, `widerruf`) now ship with fuller bilingual Germany-oriented starter content instead of one-line placeholders
- degraded-success diagnostics are now quiet by default during local development, so repeated partial-health warnings no longer spam the browser console while the app is still rendering successfully
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
- dashboard عضو، account editorها، orders/invoices list، و detail routeهای member now also load through shared route-context helpers, so protected self-service and history/detail surfaces reuse one observable member/storefront assembly model instead of repeating Promise.all composition per route
- cart، checkout، و confirmation now also load through shared commerce route-context helpers, and cart-vs-purchase follow-up filtering now runs through shared tested helpers, so conversion routes keep one reusable assembly model for member/storefront context and next-buy continuity
- anonymous storefront cart and checkout now run against live public `Darwin.WebApi` contracts instead of placeholder-only pages
- home composition now uses reusable web parts with live CMS/catalog spotlight data instead of a single blank-state block
- home composition now also includes data-backed metric web parts and a part-owned hero side panel, so the composer no longer depends on home-only hardcoded aside copy
- home composition now also includes a dedicated journey/link-list web part so CMS, catalog, and account entry flows stay visible as one front-office system instead of only a shortcut card grid
- home metrics and hero highlights now also surface CMS/catalog/category contract health explicitly, so public-content degradation is visible inside composition instead of hiding behind static counts
- home composition now also includes a reusable status-list web part so CMS, catalog, and account-entry surfaces can stay actionable as contract-backed lanes instead of only cards and metrics
- home composition now also includes a reusable stage-flow web part so CMS, catalog, and member follow-up read as one staged storefront journey instead of disconnected route clusters
- home composition now also includes a reusable pair-panel web part so CMS and catalog can stay visible as two coordinated storefront surfaces rather than isolated spotlight blocks
- home composition now also includes a reusable agenda-columns web part so content, commerce, and member follow-up can stay visible as parallel storefront streams instead of one-dimensional section stacks
- home composition now also includes a recovery/follow-up rail driven by live CMS/catalog health, so degraded public entry does not collapse into a passive shell and still points visitors toward CMS, catalog, and account paths
- home composition now also includes a reusable route-map web part that links Home into real CMS page detail, real product detail, and account/loyalty follow-up routes, so public entry shows the next concrete route instead of only section teasers
- home composition now also includes category-driven storefront lanes backed by public categories plus category-filtered product contracts, so top-level browse entry is data-backed instead of a generic catalog shortcut
- home composition now also includes a live priority lane that ranks checkout, billing, loyalty, order, CMS, and catalog next steps from current public/member signals, so the storefront entry route behaves like an actionable action surface instead of only a map of routes
- home entry now also picks the strongest visible product opportunity instead of the first catalog card, so the storefront surfaces a clearer best-offer moment from live browse data
- home composition now also surfaces a live offer board that ranks multiple visible catalog opportunities by savings strength, so the storefront entry can show several concrete next-buy options instead of one product spotlight alone
- home composition now also surfaces a live category-campaign board built from visible category lanes plus category-anchoring products, so the storefront entry can sell through stronger browse narratives instead of only isolated offer cards
- storefront runtime config now also supports app-configured theme selection between a grocer-style default and an atelier-style variant through `DARWIN_WEB_THEME`, so the same front-office system can ship more than one real visual direction without changing feature code
- storefront runtime config now also supports a third `harbor` visual direction through `DARWIN_WEB_THEME`, so the same front-office system can ship grocery, editorial, and cooler hospitality-style brand expressions without changing feature code
- storefront runtime config now also resolves the full registered multi-brand theme set through one canonical parser and registry-backed fallback, including `grocer`, `atelier`, `harbor`, `noir`, and `solstice`, so runtime theming no longer drifts between env parsing, theme registry, and shell composition
- Darwin.Web now also includes a lightweight unit-test runner plus focused coverage for query serialization, checkout draft/input normalization, HTML fragment sanitization, safe WebApi URL resolution, and locale-routing guardrails, so critical storefront handoff and trust-boundary logic is no longer validated only through manual browsing and full builds
- Darwin.Web now also covers the page-local catalog/CMS browse lenses with focused unit tests, so visible assortment/discovery-state behavior is regression-safe instead of staying embedded only inside route composition
- Darwin.Web now also emits shared API diagnostics for public, member, and auth fetch failures, including response-level request identifiers where available, so staging/production troubleshooting is less dependent on route-local guesswork
- shared API diagnostics now also classify public/member/auth failures into operational kinds such as `network-error`, `unauthorized`, `not-found`, `http-error`, and `invalid-payload`, together with retryability and status-family hints, so storefront and portal troubleshooting can distinguish transport, contract, and access issues much faster
- shared API diagnostics now also classify each failure into an API kind, surface family, surface area, attention level, and suggested action, so production logs can say whether the issue belongs to public discovery, commerce, member, auth, or shell follow-up instead of only showing transport-level metadata
- route-observability now also carries route/context metadata such as culture, page, slug, category, order id, and menu name across shared storefront/member loaders, so slow-operation and failure diagnostics are much easier to trace back to the exact front-office workflow in staging and production
- route-observation metadata is now also generated through shared tested builders, so storefront/member diagnostics keep one stable shape across shell, discovery, checkout, and protected routes instead of drifting via per-loader ad hoc objects
- catalog browse core, CMS browse core, Home core/storefront spotlight loaders, and confirmation-result loading now also emit dedicated success summaries, so diagnostics can trace the health of discovery core and post-purchase core loaders before route-level composition is inspected
- product detail core, product related-products loading, and CMS detail core now also load through the same shared observed-loader path with dedicated success summaries, so detail discovery no longer depends on the older manual cache-plus-observation branch
- Home route assembly plus public account/auth entry routes now also load through shared observable route-context helpers, so storefront entry and self-service entry no longer keep route-local diagnostics branches around shared storefront context assembly
- public sign-in, register, activation, and password routes now also reuse one shared public-auth page-context helper for storefront continuation assembly, so self-service entry pages no longer drift in how they build live cart/content follow-up
- `/account` now also assembles through a shared observable page-context loader that chooses the public hub or member dashboard path centrally, so the main account entry route no longer keeps its public/member split as page-local server wiring
- Home and `/account` now also project their final page props through shared entry-view helpers, so storefront entry and the main account entry no longer keep route-local public/member prop assembly or return-path sanitation branches inside the page files
- protected member, public discovery, and commerce page-loader cores now also live in pure tested helpers beneath their server-only wrappers, so the main page-assembly infrastructure has direct regression coverage without depending only on route-level integration tests
- protected member entry routes for account editor, orders/invoices, and order/invoice detail now also share one observable auth-gate context, so session check plus storefront fallback no longer drift route by route across the protected portal
- protected account editor, orders/invoices, and order/invoice detail pages now also assemble through shared observable page-context loaders, so the protected portal no longer repeats page-level auth gate plus route-context wiring in each route file
- protected member editor, history, and detail pages now also reuse one shared protected page-loader helper on top of the entry gate, so session-aware member fallback and page assembly no longer drift across orders, invoices, and self-service routes
- public CMS and catalog list/detail pages now also assemble through shared observable page-context loaders, so discovery routes no longer repeat page-level route-context plus continuation wiring in each page file
- public CMS and catalog list/detail pages now also reuse one shared public-discovery page-loader helper for continuation slices and route assembly, so content and commerce browse surfaces no longer drift in how they build live storefront follow-up
- cart, checkout, and confirmation now also assemble through shared observable page-context loaders, so the main conversion routes no longer repeat page-level commerce route-context plus follow-up wiring in each page file
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
- member order and invoice history now also expose an explicit visible-result review lens over the currently loaded page window, so members can quickly isolate attention or settled history without pretending backend-global search already exists
- member order and invoice detail routes now also surface explicit operational timelines, so payment, shipment, delivery, due-date, and settlement milestones stay visible on the detail surfaces themselves
- the member dashboard now also surfaces an explicit commerce-readiness layer, so order attention and open billing exposure are visible from `/account` before the member drills into history routes
- catalog index and product detail now also surface a shared browse-campaign window built from visible category lanes plus strongest product offers, so catalog routes can drive stronger browse and buying decisions instead of only listing products
- that home offer board now also shifts away from products already linked to the active cart when browser storefront-shopping state exists, so the storefront entry can suggest the next buying move beyond the current basket instead of echoing the same cart
- home composition now also includes a live commerce-opportunity window driven by cart, spotlight-product, and category-lane signals, so the storefront entry route can push the shopper toward the strongest immediate buying move instead of only showing generic browse links
- home composition now also includes an explicit browse-readiness window for CMS and catalog, so storefront entry can expose public discovery debt before the visitor has to infer it from deeper browse routes
- home composition now also includes a review cockpit for CMS and catalog, so storefront entry can reopen the strongest visible review windows and jump directly to the next concrete page/product targets without rebuilding browse state first
- home composition is now also session-aware for signed-in members and can surface direct portal re-entry routes for account, orders, and loyalty instead of treating Home like a purely anonymous landing page
- home composition is now also cart-aware and can surface direct cart/checkout recovery from browser-owned storefront snapshots, so Home can resume active shopping flows instead of only restarting browse
- home composition is now also live-cart-aware and can surface current cart totals plus checkout continuity from the canonical public cart contract when a storefront cart already exists
- home composition now also enriches signed-in member resume with recent orders and reward-focus data from the canonical member contracts, so storefront entry can resume real member context instead of only linking back to `/account`
- home composition now also surfaces invoice follow-up inside the signed-in member resume, so storefront entry can hand off directly into outstanding billing context instead of forcing a second hop through the account dashboard
- home composition now also surfaces member checkout readiness inside the signed-in member resume, so storefront entry can hand off directly into prepared checkout or address-book setup from the same protected context
- public account self-service now includes register, activation, password-recovery, and sign-in entry points against the canonical member auth contracts
- resend-activation recovery is now surfaced inline on account hub, sign-in, register, and password-recovery entry points instead of staying hidden behind the dedicated activation route
- public account self-service links are now locale-aware and the account hub now also exposes the shared continuation-rail pattern back into Home, Catalog, and CMS, so public account entry stays aligned with the rest of the storefront routing model
- sign-in, registration, activation, and password-recovery routes now also share the same continuation-rail pattern back into Home, Catalog, and CMS, and sign-in no longer uses raw anchor navigation for internal storefront routes
- the public account hub plus sign-in/register/activation/password routes now also consume live published CMS pages plus live public categories through the shared public-auth continuation wrapper, so public self-service entry can hand off into real content and browse surfaces instead of falling back to static continuation cards
- the public account hub plus sign-in/register/activation/password routes now also consume live storefront cart state through that same shared public-auth continuation wrapper, so public self-service entry can resume cart/checkout continuity instead of dropping active commerce context at the auth boundary
- sign-in/register/activation/password now also surface a shared post-auth destination summary fed by sanitized `returnPath` plus live cart state, so public auth routes keep checkout/cart/member intent explicit instead of acting like context-free forms
- that public auth post-auth destination summary now also exposes direct CTA handoff to the sanitized return route plus live cart continuation, so self-service routes can actively recover the current storefront/member journey instead of only describing it
- that shared public-auth continuation flow now also surfaces a browse-campaign board built from live category lanes plus strongest visible offers, so account hub, sign-in, registration, activation, password recovery, and protected auth walls can keep stronger browse-and-buy narratives alive instead of only listing continuation links
- the public account hub now also surfaces a dedicated storefront-readiness panel and carries the preferred post-auth destination into sign-in/register/activation/password recovery links, so public account entry preserves active cart/checkout intent instead of defaulting every auth jump back to `/account`
- the public account hub now also surfaces a live storefront action center fed by current cart state plus published CMS/category spotlights, so anonymous account entry can still move through real content and commerce next steps instead of collapsing to auth-only choices
- the public account hub action center now also surfaces a strongest visible product offer, so anonymous account entry can still create a real next-buy decision instead of only handing off into cart, CMS, or category browse
- the public account hub now also surfaces a live offer board with multiple strongest visible product opportunities, so anonymous account entry can pitch several next-buy options instead of stopping at one spotlight
- those public account, public auth, protected auth-wall, commerce handoff, and guest-confirmation offer boards now also classify visible offers into explicit merchandising tiers such as hero offer, value offer, price drop, and steady pick, so high-intent entry points communicate why an opportunity matters instead of only listing ranked products
- the public `/account` hub now also accepts and preserves a sanitized incoming `returnPath`, so auth walls can hand shoppers into the generic account entry route without losing the intended post-auth destination context
- public account self-service and sign-in now also normalize email input plus stronger required/autocomplete/password guardrails so avoidable auth-flow mismatches are reduced before the canonical API call
- CMS and catalog routes now also use feature-level continuation-rail wrappers on top of the shared public continuation component, so content/discovery continuity no longer depends on route-local item assembly
- a provisional browser session layer now exists for the web portal, using web-owned cookies plus access-token refresh in front of the canonical member APIs
- account, orders, invoices, and loyalty routes now render authenticated member data instead of staying as placeholders
- profile, preferences, and reusable address-book editing now run against the member profile endpoints instead of remaining read-only account placeholders
- the profile route now also surfaces explicit readiness for identity, phone verification, and locale/billing defaults, so member self-service can see commerce/communication completeness without inferring it only from the edit form
- authenticated member password change now runs on a dedicated security route against the canonical authenticated password-change endpoint instead of redirecting active-session users back into public recovery
- the authenticated security route now also surfaces current profile/session security context, including phone-verification state, session-expiry visibility, and direct handoff back into profile/dashboard follow-up instead of acting like a password-form-only leaf route
- dashboard, profile, preferences, and address-book screens now also share explicit member-portal navigation so the protected account area behaves like one subsystem instead of separate editor pages
- phone verification now runs through the canonical member profile verification endpoints and shared confirmation flag instead of a web-local flow
- orders, invoices, and loyalty overview now also follow the same shared member-portal navigation model, so the full authenticated portal is converging on one navigation/chrome contract
- dashboard, orders, invoices, and loyalty overview now also expose breadcrumb-style route orientation plus cross-surface handoff actions, so the member area stays connected to the wider front-office system instead of behaving like a sealed portal island
- dashboard, orders, invoices, and loyalty overview now also share a reusable member cross-surface rail, so protected overview routes no longer duplicate route-handoff blocks or drift into inconsistent portal-side follow-up UX
- profile, preferences, addresses, order detail, invoice detail, and loyalty business detail now also expose the same breadcrumb-style route orientation plus cross-surface handoff actions, so editor/detail routes no longer fall back to a narrower portal-only context
- profile, preferences, addresses, order detail, invoice detail, and loyalty business detail now also share the same reusable member cross-surface rail as the overview routes, so protected detail/editor continuity no longer drifts into route-specific CTA clusters
- dashboard and preferences now also expose explicit route-summary/follow-up panels, and address/order/invoice empty states now keep actionable storefront/member CTAs instead of falling back to passive placeholder blocks
- addresses, orders, and invoices empty states now also reuse the shared member cross-surface rail, so protected list routes keep the same continuation pattern as overview/detail/auth-required surfaces
- profile, preferences, and addresses now also keep explicit route-summary and unavailable follow-up guidance, so the account-edit subsystem stays navigable even when profile/preference/address data is partial or absent
- loyalty business detail pages now expose business-scoped dashboard, rewards, and timeline data instead of keeping loyalty at an overview-only level
- loyalty business detail now also follows the shared member-portal navigation model, and prepared scan-token state now clears stale or mismatched browser cookie state instead of drifting silently
- loyalty overview, discovery, public business detail, and signed-in business detail now also expose explicit route-summary panels plus actionable degraded/empty-state follow-up CTAs, so loyalty routes stay observable and navigable instead of collapsing into passive no-data blocks
- runtime culture is now config-driven with `de-DE`/`en-US` support, cookie/query-string switching, and locale-aware money/date formatting across storefront and member views
- resource-backed localization now exists under `src/localization/resources`, and shared shell/catalog/storefront-commerce plus account-edit/member-commerce and loyalty discovery/public-detail copy now runs on that bundle-based path so future languages can be added without rewriting feature components
- storefront typography now uses local/system font stacks instead of `next/font/google`, so builds stay deterministic in restricted/offline environments and theme typography remains a runtime/style concern rather than a build-time network dependency
- public CMS list/detail delivery now also follows the active request culture instead of relying on backend default-language behavior
- CMS-driven shell navigation now only accepts sanitized app-local paths or safe `http/https` URLs, so `main-navigation` content cannot push unsafe raw hrefs into the storefront chrome
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
- CMS unavailable/no-pages and catalog no-results fallbacks now also keep `account` follow-up visible, so public discovery/content dead ends still connect back into the wider front-office system
- CMS no-pages and catalog no-results now also use the shared continuation-rail pattern, so list-level public empty states do not drift back into one-off CTA clusters
- cart, checkout, and confirmation now also expose route-summary diagnostics plus stronger empty/unavailable follow-up CTAs, so the conversion path stays observable and actionable instead of degrading into passive panels
- loyalty overview now consumes the richer `my/businesses` contract and business detail pages now consume personalized promotions plus promotion-interaction tracking
- loyalty discovery now also consumes public business list/category metadata, and business detail routes now keep a public pre-join experience plus direct member join instead of only showing joined-business dashboards
- loyalty discovery now also supports query-driven proximity filters with a server-rendered coordinate preview while staying on the loyalty-filtered public discovery contract
- loyalty business detail currently also consumes the canonical scan-preparation contract and can render the active prepared token as a QR image, but this should be treated as a provisional contract-consumption slice rather than a commitment to browser camera/scanner or barcode workflows in `Darwin.Web`
- the intended web-product direction remains that loyalty on the web may evolve differently from mobile and can later support direct point accrual/redemption flows for store-enabled businesses without requiring browser camera/scanner support
- catalog list/detail now pass the active request culture to `Darwin.WebApi` and expose contract-safe merchandising context such as selected-category panels, compare-at savings, and category-linked navigation
- `/catalog` now also keeps its browse controls on the full matching assortment instead of only the currently loaded server page, so active review/sort/filter windows no longer drift around one page of products
- `/catalog` now also exposes a page-local visible assortment lens for `all`, `offers`, and `base assortment`, so shoppers can separate current-page offer windows from base assortment without pretending backend facets already exist
- `/catalog` now also applies `offers-first` and `base-first` review windows to the full matching assortment when a browse lens is active, so commercial review no longer drifts around the current loaded page while richer backend facet metadata still remains future work
- `/catalog` now also exposes real browse facets for image coverage and offer strength against the full matching assortment, so shoppers can open image-attention and hero-offer windows without falling back to page-local heuristics
- `/catalog` now also exposes direct quick windows for image-ready, image-attention, value-offer, and hero-offer browse sets, so the main catalog route can jump straight into the strongest facet-specific windows without rebuilding filters manually
- `/catalog` now also surfaces visible-vs-loaded-vs-total result summaries plus first/last page jumps, so catalog window navigation is more complete without pretending backend search/facets already exist
- `/catalog` now also surfaces an offer-focus window plus a buying-guide summary from the live visible product set, so merchandising signals stay explicit even before true backend search/facets arrive
- `/catalog` now also surfaces explicit assortment-readiness coverage for visible offers, base assortment, and support context, so browse review can inspect route debt from the catalog window itself instead of only from counts and lenses
- `/catalog` and `/catalog/[slug]` now also surface live cart/checkout continuity from the canonical public cart contract, so browse and product evaluation can hand off directly into an already active purchase flow instead of behaving like isolated discovery routes
- product detail now also surfaces category-aware related products by reusing the current public category/product contracts instead of inventing a separate recommendation endpoint
- product detail now also exposes breadcrumb, product-reference snapshot, and cross-surface storefront handoff actions so the conversion route does not behave like an isolated leaf page
- product detail now also surfaces offer-position and buying-context panels derived from the current product plus related-offer signals, so conversion/detail routes communicate active offer strength instead of behaving like static specification pages
- product detail cross-surface handoff now also includes a CMS return path inside a shared continuation-rail component, so content and commerce stay visibly connected in both directions instead of diverging into route-specific button clusters
- catalog index and product detail now also surface live published CMS follow-up windows, so public commerce browsing stays connected to storefront content without waiting for new backend contracts
- product detail unavailable and related-products-empty states now also keep account follow-up visible, so degraded conversion/detail states remain connected to the wider front-office system
- member order/invoice detail pages now consume canonical document links plus richer payment, shipment, and linked-invoice presentation instead of only showing totals
- member order/invoice detail pages now also surface explicit readiness panels for payment, shipment, balance, and document follow-up, so protected commerce detail pages no longer rely only on dense summary blocks to communicate the next actionable state
- member order/invoice detail pages now also surface storefront continuation windows fed by live public CMS pages plus public categories, so protected commerce detail routes stay connected to public content and catalog follow-up instead of ending inside portal-only branches
- member order/invoice detail storefront merchandising now also ranks visible product follow-up by strongest savings signal, so protected commerce detail routes can surface clearer next-buy opportunities instead of a raw public-product list
- member order/invoice detail storefront merchandising is now also cart-aware, so those protected commerce detail routes avoid echoing items already linked to the active storefront cart and keep the next-buy signal cleaner
- the authenticated orders route now also surfaces explicit fulfillment-readiness state for visible orders needing active follow-up, so order-history follow-up is visible from the history route instead of being inferred only from row-level statuses
- the authenticated orders route now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so order history stays connected to public content and catalog follow-up instead of behaving like a portal-only archive
- order-history storefront merchandising now also ranks visible product follow-up by strongest savings signal, so post-purchase history can surface clearer next-buy opportunities instead of a raw public-product list
- the authenticated invoices route now also surfaces explicit billing-readiness state for visible outstanding invoices and open balance, so finance follow-up is visible from the history route instead of being inferred only from row-level balances
- the authenticated invoices route now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so invoice history stays connected to public content and catalog follow-up instead of behaving like a portal-only finance archive
- invoice-history storefront merchandising now also ranks visible product follow-up by strongest savings signal, so billing history can surface clearer next-buy opportunities instead of a raw public-product list
- member order/invoice detail unavailable states now also keep explicit follow-up actions to orders/invoices, account, and catalog instead of collapsing into warning-only dead ends
- storefront cart now supports canonical coupon apply/clear plus richer line-level tax/pricing context, and checkout summary now surfaces shipment/country context from the live intent
- storefront cart now also keeps a small continue-shopping follow-up rail plus explicit cart next-step guidance visible by reusing the public catalog contract instead of depending only on totals and one checkout CTA
- storefront cart now also reuses saved member addresses when a browser member session exists, so shoppers can see address-book-backed checkout readiness before leaving the cart route
- storefront cart now also surfaces member profile/preferences/address readiness together, so signed-in shoppers can verify identity, phone/channel readiness, and address coverage before moving into checkout
- storefront cart/checkout/confirmation now also normalizes quantity, coupon, country-code, and controlled status-query inputs so public commerce flows do not trust raw browser values across redirects and form posts
- storefront checkout now also reuses saved member addresses when a browser member session exists, keeps the canonical member address book visible inside the checkout route, and falls back to manual address entry when member address delivery is unavailable
- storefront checkout now also reuses canonical member profile identity for name/phone prefill when a member session exists but no saved address is selected, so checkout can still start from member context before the address book is populated
- storefront checkout now also surfaces recent member invoice attention inside the route, so signed-in shoppers can keep open billing follow-up visible before placing a new order
- storefront checkout now also surfaces member profile, channel, and address-book readiness directly inside the route, so authenticated checkout keeps profile/preferences/address context visible instead of treating member prefill as hidden background state
- storefront checkout now also surfaces a dedicated payment-continuity window before order placement, so projected payable total, billing attention, and account handoff stay explicit instead of hiding behind the final submit action
- cart now also surfaces explicit opportunity and readiness panels, so the shopper can see the strongest adjacent offer plus basket readiness before leaving the route for checkout
- checkout now also surfaces explicit confidence and attention panels, so order-readiness, billing follow-up, phone verification, and address-book coverage stay visible at the final conversion step
- storefront confirmation now also reconciles hosted-checkout return/cancel flows through the canonical payment-completion endpoint instead of acting as a passive snapshot screen
- storefront confirmation now also keeps post-checkout guidance visible, including payment-next-step messaging, account/order-history follow-up, and stable order-reference handling
- storefront confirmation now also surfaces signed-in member continuation across orders, invoices, and loyalty, so post-checkout handoff into the protected portal stays explicit instead of stopping at generic CTA buttons
- storefront confirmation now also surfaces a dedicated payment-continuity window, so visible attempts, provider footprint, and commercial exposure are summarized before the shopper drops into lower-level payment detail rows
- storefront confirmation now also gives guest shoppers an explicit account-continuation panel with sign-in/register/activation/password recovery handoff bound to the protected order-follow-up return path instead of relying on two isolated CTA buttons
- storefront confirmation now also surfaces explicit post-purchase care and next-customer-window panels, so payment attention, order reference handling, and repeat-engagement follow-up stay visible after checkout instead of the route behaving like a passive receipt
- storefront confirmation and auth-required follow-up links now also sanitize app-local return targets centrally, and confirmation status messaging derives from the authoritative confirmation snapshot instead of trusting query-carried status text
- protected member entry points now also render route-summary plus cross-surface follow-up panels instead of collapsing to a minimal sign-in block, so auth-required routes stay aligned with the wider portal/storefront orientation model
- protected member entry points plus unavailable order/invoice/loyalty detail states now also reuse the shared member cross-surface rail, so protected failure/access walls keep the same continuation model as the healthy portal routes
- cart, checkout, and confirmation now also share breadcrumb-style route orientation plus explicit cross-surface handoff cards, so the conversion chain behaves like one storefront subsystem instead of detached routes
- cart, checkout, confirmation, and their empty/unavailable follow-up states now also reuse a shared commerce continuation rail, so public conversion continuity no longer drifts into route-local CTA clusters
- cart and checkout now also surface a shared anonymous account-handoff panel carrying a sanitized `returnPath` into sign-in/register/activation/password help, so commerce routes keep account recovery one step away without dropping the current conversion intent

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
- `DARWIN_WEB_THEME`
  - default: `grocer`
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
- Home now also exposes category-driven storefront lanes built from live public category plus category-filtered product contracts, so top-level browse paths are not just generic catalog entry cards
- public CMS listing and CMS slug routes against live `Darwin.WebApi` content endpoints
- CMS index now also applies discovery-state and review-priority windows to the full matching search set when a browse lens is active, so public content review no longer drifts around the current loaded page while richer CMS grouping/search metadata still remains future work
- CMS index now also surfaces current-window result summaries for visible vs loaded vs total published pages, so public content browsing stays set-aware instead of behaving like a flat card dump
- CMS index now also surfaces explicit visible discovery-readiness coverage for ready pages, attention pages, and review support, so public content review can inspect window-level discovery debt instead of relying only on lenses and result counts
- CMS index now also groups the visible page set by title initials with quick-jump anchors, so public content browsing reads like an oriented set instead of one undifferentiated card wall
- CMS index now also derives a spotlight-plus-follow-up reading rail from the current visible result window, so the public content surface offers a guided reading path without inventing a richer CMS contract
- CMS index now also exposes explicit cross-surface handoff cards into Home, Catalog, and Account, so public content consumption stays connected to the broader storefront system
- CMS index pagination now also exposes first/last page jumps while preserving the current page-local lens, so public CMS window navigation is more complete without claiming backend search support
- CMS detail rendering with route metadata, related-page navigation, storefront follow-up panels, and visible degraded-state behavior
- CMS detail now also exposes previous/next page adjacency plus account-aware follow-up CTAs, using the same published page list instead of requiring a dedicated navigation contract
- CMS detail now also exposes breadcrumb and published-set position context, using the current published page set instead of leaving long-form content detached from storefront orientation
- CMS detail now also uses the same reusable continuation-rail pattern as product detail, so public content and commerce routes share one continuity model instead of drifting into bespoke follow-up blocks
- CMS and catalog feature routes now also assemble their public follow-up rails through feature-level wrappers instead of route-local `PublicContinuationRail` item lists, so continuity rules stay reusable at the CMS/catalog module boundary
- CMS index empty-state handling now also keeps Home/Catalog follow-up actions visible instead of ending in a dead-end empty panel
- CMS detail now also derives section navigation anchors plus reading/structure metrics from the published HTML itself, so long-form content is no longer rendered as a single opaque block
- CMS detail anchor ids now also normalize diacritics before slugging section headings, so long-form German content keeps stable in-page navigation instead of collapsing into weak or duplicate fallback ids
- CMS detail now also surfaces an explicit published-content readiness panel for metadata, structure, and navigation coverage, so public content review can see discovery debt from the detail route itself instead of inferring it only from raw page HTML
- CMS detail unavailable state now also keeps cross-surface follow-up visible, and CMS/catalog detail/index routes now surface explicit route-summary diagnostics so degraded public content/discovery states do not collapse into passive leaves
- CMS index and CMS detail now also surface live catalog-category and live product follow-up windows, so public content routes can hand off directly into real commerce browse/detail paths instead of only pointing back to generic catalog entry
- CMS index and CMS detail now also surface a shared commerce-campaign window built from visible category lanes and strongest product opportunities, so public content routes can sell through stronger browse and buying stories instead of only exposing utility follow-up lists
- those CMS product follow-up windows now also rank by the strongest visible savings signal first, so content routes hand off into a clearer best-offer buying opportunity instead of arbitrary catalog order
- CMS index and CMS detail now also surface live cart/checkout continuity from the canonical public cart contract, so published content can hand off directly into an already active purchase flow instead of only into browse routes
- public catalog browsing against live `Darwin.WebApi` category/product endpoints
- `/catalog` now also applies category/query search plus offer/base browse lenses and review-oriented sort against the full matching assortment when those lenses are active, so pagination and result counts stay real even before richer backend facet metadata exists
- cart empty state, checkout unavailable state, follow-up-products unavailable state, and confirmation/cart/checkout route summaries now keep the commerce flow observable and actionable instead of collapsing into passive no-data states
- public product-detail route against the product-by-slug endpoint
- public cart page plus add/update/remove flows against public cart endpoints with stable anonymous cart identity
- cart follow-up products and next-step guidance using the canonical public catalog contract instead of a fake recommendation-only API
- public checkout page with server-rendered address capture, live checkout intent preview, shipping selection, and order placement
- public order-confirmation route with payment-handoff retry against the storefront confirmation and payment-intent endpoints
- hosted-checkout return/cancel reconciliation through the storefront payment-completion endpoint plus a short-lived web-owned handoff cookie
- post-checkout guidance on the confirmation route for payment attention, member-portal follow-up, and stable order-reference handling instead of a receipt-only end state
- public account self-service foundation for member registration, activation email request/confirm, and password reset request/complete flows
- inline resend-activation recovery on account hub, sign-in, register, and password-recovery surfaces, reusing the canonical activation request endpoint and preserving localized return-path context
- stronger public auth-form guardrails plus email canonicalization in registration/activation/password/sign-in actions
- account self-service return-path preservation across register, activation, password recovery, and sign-in so storefront/member entry points can carry the intended app-local destination through the public auth flows
- provisional browser sign-in plus authenticated member dashboard, order history/detail, invoice history/detail, and loyalty overview
- member dashboard and address-book routes now also hand off directly into checkout with saved-address prefills, so protected member data can feed the storefront conversion path instead of staying isolated inside the portal
- the authenticated address-book route now also surfaces explicit checkout-readiness state for reusable/default shipping/default billing coverage, so storefront handoff readiness is visible from the address subsystem instead of being inferred only from individual address cards
- member dashboard now also surfaces recent order and invoice snapshots from the canonical member commerce endpoints, so account overview acts as a real portal landing page instead of only a route map
- member dashboard now also surfaces loyalty overview totals plus joined-business snapshots from the canonical member loyalty endpoints, so account overview acts as a real cross-domain landing page for profile, commerce, and loyalty follow-up
- member dashboard now also surfaces next-reward focus cards derived from the canonical loyalty overview accounts, so members can jump from `/account` straight into the most relevant loyalty business follow-up instead of opening the full loyalty route blindly
- member dashboard now also derives an action-center from profile, address-book, invoice-balance, and loyalty snapshots, so `/account` can send members straight into the most urgent next step instead of acting as a passive summary screen
- member dashboard now also surfaces live storefront cart continuity from the canonical public cart contract, so signed-in members can resume cart/checkout directly from the member portal instead of treating commerce continuation as a separate public-only path
- member dashboard now also surfaces a dedicated security window with phone-verification state, session-lifetime visibility, and direct handoff into the authenticated security/profile routes, so `/account` can expose security readiness from the main member landing route instead of hiding it behind `/account/security`
- member dashboard now also surfaces a communication window derived from canonical profile plus preferences, so email/SMS/WhatsApp readiness is visible from `/account` instead of being hidden behind preferences/profile routes
- member dashboard now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so the protected member landing route stays connected to public browse/content follow-up instead of acting like a portal-only island
- member dashboard storefront merchandising now also ranks visible product follow-up by strongest savings signal, so signed-in account entry can surface clearer next-buy opportunities instead of a raw public-product list
- resource-backed public account auth, profile/preferences/addresses, signed-in dashboard shell, member orders/invoices, and loyalty surfaces including overview/discovery/public detail/business detail through JSON bundles under `src/localization/resources`
- shared member-portal navigation across dashboard/profile/preferences/addresses plus stronger profile/address form guardrails for the authenticated account area
- shared member-portal navigation now also covers orders, invoices, loyalty overview, and commerce detail sidebars instead of stopping at account editors
- profile/preferences/addresses plus order/invoice/loyalty-business detail routes now also carry the same breadcrumb-style route orientation and cross-surface handoff actions as the overview routes, so the authenticated portal stays coherent on edit/detail pages as well
- profile/preferences/addresses plus order/invoice/loyalty-business detail routes now also reuse the same member cross-surface rail as overview routes, so protected continuation and follow-up behavior stays on one shared component path instead of diverging per route
- dashboard/preferences sidebars now also surface route-summary/follow-up context, and address/order/invoice empty states now keep the member inside actionable front-office paths instead of passive dead ends
- editable profile, preferences, and member address-book flows against the canonical member profile endpoints
- the authenticated preferences route now also reads canonical profile channel readiness, so email/SMS/WhatsApp preference toggles are shown alongside the actual profile-channel prerequisites instead of behaving like detached booleans
- a dedicated authenticated security route for member password change against the canonical authenticated password-change endpoint
- profile, preferences, security, and addresses now also surface a shared storefront-continuation window fed by live public CMS pages plus public categories, so authenticated account editor routes stay connected to published content and catalog browse follow-up instead of behaving like portal-only leaves
- profile, preferences, security, and addresses now also surface live product highlights inside that shared storefront-continuation window, so authenticated account editor routes can create a next-buy moment instead of only offering content and category follow-up
- that shared account-editor storefront window now also ranks product opportunities by strongest visible savings and surfaces compare-at context, so self-service routes present sharper buying highlights instead of raw product order
- that shared account-editor storefront window now also classifies both product highlights and browse-campaign lanes into explicit merchandising tiers, so authenticated account routes can frame clearer buying stories instead of only showing ranked products and generic offer lanes
- account hub plus sign-in/register/activation/password routes now also surface live product highlights inside their shared public-auth continuation flow, so anonymous account recovery can preserve a next-buy opportunity instead of only keeping cart/content/category context alive
- those public-auth and account-entry product highlights now also rank by the strongest visible savings signal first, so self-service routes surface a clearer best-offer moment instead of echoing arbitrary catalog order
- protected member-route auth walls now also surface the same live storefront continuation context, so signed-out visits to orders, invoices, and account editor routes keep cart/content/product opportunity visible instead of collapsing into a minimal access block
- protected member-route auth walls now also surface a live offer board with several strongest visible buying opportunities, so protected entry does not reduce commercial momentum to a single spotlight
- protected member fetches now refresh provisional browser sessions near expiry and retry once before falling back to sign-in again
- phone verification request/confirm inside the profile surface via the canonical SMS/WhatsApp member verification endpoints
- business-scoped loyalty detail routes with rewards, recent transactions, and cursor-based timeline paging
- loyalty overview/discovery/public-detail/business-detail surfaces now also carry route-summary diagnostics and non-passive empty/degraded follow-up actions, keeping the member/visitor inside the broader storefront flow
- config-driven `de-DE`/`en-US` shell switching with locale-aware currency/date formatting across catalog, cart, checkout, orders, invoices, and loyalty pages
- resource-backed shared shell/catalog/storefront-commerce copy through JSON bundles under `src/localization/resources`
- feature-level continuity wrappers now cover public auth/account hub, CMS, catalog, member portal, and commerce flows so route follow-up rails no longer drift as raw route-local item lists
- auth-facing public/member entry links now also use a shared localized `returnPath` helper, so sign-in/register/activation/password and loyalty/confirmation follow-up links stop hand-building query strings per route
- auth, sign-in, and member-profile action inputs now also pass through shared trimmed/bounded FormData readers for email, returnPath, tokens, passwords, phone codes, and profile identity fields, so those server-action entry points are less dependent on route-local `String(...).trim()` coercion
- localized app-query links now also use shared helpers for auth and common public/member navigation cases, so category filters, loyalty timeline paging, and confirmation handoff links avoid route-local query-string assembly drift
- server-action redirect/query assembly in cart, checkout, and member-portal flows now also runs through shared app-query helpers, so flash/status/error redirects are less dependent on duplicated string concatenation across action files
- account and member-session server actions now also build auth-flow query redirects through the same shared app-query helper path, so public auth redirects no longer keep a separate local query-builder implementation
- member order/invoice payment-handoff failure redirects now also use the shared app-query param helper, so `paymentError` redirects in the protected commerce flow no longer keep a leftover local separator/encoding branch
- product-detail follow-up rails now also pass category-filtered catalog paths through the shared app-query helper instead of inline query assembly, closing the last route-level catalog query drift on that surface
- catalog route metadata, catalog pagination/filter links, CMS index metadata/pagination links, and loyalty discovery pagination/filter links now also share the central app-query path helper, so page/component-level query assembly follows the same routing path as action redirects
- checkout confirmation finalize redirects now also use the central app-query path helper, so hosted-checkout completion handoff no longer keeps a route-local query-string builder branch
- checkout confirmation finalize now also bounds callback-carried order/provider/failure text and clears stale mismatched handoff cookies, so PSP return handling is less trusting of raw callback query payloads
- shared query serialization now also backs catalog/CMS public API helpers, cart fetch, checkout draft search, checkout confirmation fetch, and member portal paging helpers, so repeated `URLSearchParams` serializers are no longer scattered across those infrastructure paths
- the shell culture switcher now also clones search params through the shared query utility instead of keeping its own local `URLSearchParams` copy branch, which closes the remaining component-layer query-manipulation drift
- member orders and invoices routes now also parse `page` through the shared positive-integer search-param helper, so history pagination no longer keeps a raw `Number(...)` branch separate from the rest of route input hardening
- browser-owned cart display, storefront payment handoff, and member session cookies now also validate parsed JSON shape before reuse, so corrupted cookie state is less likely to leak into storefront/member flows after a successful parse
- browser-owned prepared loyalty scan-session cookies now also validate parsed JSON shape before reuse, so loyalty scan preparation no longer accepts malformed cookie payloads just because they parse successfully
- member-session expiry checks and prepared loyalty scan-session expiry/validity checks now also use shared UTC timestamp helpers, so timestamp parsing no longer drifts across cookie/session hardening paths
- order confirmation and member order detail now also share a stricter address-JSON parser, and loyalty reward-progress rendering now clamps parsed percent input before display, so two more JSON/numeric edge-case paths are less optimistic about malformed payloads
- checkout integer parsing is now stricter for quantity, page, and shipping-minor-unit inputs, so values like malformed mixed strings no longer slip through permissive `parseInt`/`Number` branches in storefront routing and order placement
- loyalty business detail now normalizes next-reward progress through an explicit percent parser/clamp helper instead of repeated raw `Number(...)` casts in render, which closes the remaining percent-display edge case on that surface
- bounded numeric query parsing is now also strict about decimal shape before conversion, so latitude/longitude/radius-style inputs no longer rely on permissive `Number(...)` coercion in route parsing
- culture-aware route metadata plus key-based localized flash/error messaging for web-owned cart/checkout/member action flows
- loyalty overview cards backed by `my/businesses` plus business-scoped promotions feed/tracking through the canonical member loyalty contracts
- the authenticated loyalty overview route now also surfaces explicit engagement-readiness state for active joined places and reward-focus follow-up, so loyalty overview acts as a next-step surface instead of only a balances/list screen
- the authenticated loyalty overview route now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so loyalty follow-up can move back into public content and catalog discovery without detouring through the dashboard first
- public loyalty discovery against the canonical business-discovery contracts plus pre-join business detail/join UX on `/loyalty` and `/loyalty/[businessId]`
- public loyalty business detail now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so pre-join loyalty routes stay connected to public content and catalog browse follow-up instead of ending as standalone detail leaves
- public loyalty overview now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so signed-out loyalty discovery stays connected to published content and catalog browse follow-up instead of behaving like a discovery-only leaf route
- query-driven loyalty proximity browsing with a server-rendered coordinate preview derived from the same public discovery result set
- provisional loyalty scan-preparation contract consumption with branch selection, reward selection, and short-lived token visibility on `/loyalty/[businessId]`
- QR rendering for the active prepared loyalty scan token on `/loyalty/[businessId]`, without treating browser camera/scanner handling as committed web scope
- loyalty business detail now also stays on the shared member-portal navigation model and clears stale/mismatched prepared scan-token cookie state during readback
- loyalty business detail now also surfaces a storefront-continuation window fed by live public CMS pages plus public categories, so protected loyalty follow-up stays connected to public CMS and catalog browse routes instead of terminating inside a portal-only detail branch
- culture-aware catalog delivery plus contract-safe merchandising polish across `/catalog` and `/catalog/[slug]`
- category-aware related-product follow-up on `/catalog/[slug]` using the current public catalog contracts
- product detail now also surfaces degraded related-product follow-up state explicitly when the category-based follow-up fetch fails, instead of silently flattening adjacent catalog discovery
- product detail now also surfaces explicit buying-readiness coverage for metadata, merchandising, and adjacent follow-up, so public commerce review can inspect route debt from the detail page itself instead of only from route-summary status
- member commerce detail hardening for order/invoice documents, shipment/payment snapshots, and linked invoice/order follow-up
- public cart coupon apply/clear plus richer tax/shipping/billing presentation across cart and checkout summary surfaces
- checkout now also exposes readiness signals and a live order-review panel so the shopper can validate address/shipping/line items before order placement
- cart and checkout now also surface a live storefront-discovery window backed by published CMS pages, public categories, and visible product opportunities, so active purchase routes can hand shoppers back into content, browse, and upsell paths without dropping the current conversion flow
- the shared commerce storefront window now also picks the strongest visible product offer instead of the first catalog card, so cart, checkout, and confirmation show a clearer next-buy signal
- that shared commerce storefront window now also surfaces a live offer board with multiple strongest visible product opportunities, so cart, checkout, and confirmation can show several next-buy options instead of a single buying spotlight
- those commerce and protected-entry offer boards now also carry explicit merchandising-tier labels, so cart, checkout, confirmation, auth-wall, and account-handoff routes frame visible buying opportunities as concrete commercial stories instead of generic product lists
- cart and checkout now also surface live product highlights inside their guest account/auth handoff, so anonymous shoppers can keep a next-buy opportunity visible while deciding whether to sign in or create an account
- guest commerce auth handoff now also ranks product highlights by the strongest visible savings signal first, so cart and checkout keep a clearer best-offer pitch alongside account recovery
- the guest commerce auth handoff now also surfaces a live offer board with multiple strongest visible product opportunities, so cart and checkout can keep several next-buy options visible during the account decision
- order confirmation now also surfaces that same live storefront-discovery window, so after-purchase shoppers can move directly into content, browse, and product follow-up instead of ending on a receipt-only route
- order confirmation now also biases its product follow-up away from items already present in the just-finished order, so the after-purchase opportunity is closer to a real next-buy suggestion than a generic catalog echo
- guest order confirmation now also surfaces a next-buy highlight inside the account-tracking handoff, so anonymous after-purchase shoppers keep a real commercial next step visible while deciding whether to sign in or create an account
- guest order confirmation now also surfaces a live next-buy offer board inside the account-tracking handoff, so anonymous after-purchase shoppers can see several post-purchase opportunities instead of one spotlight
- that guest confirmation next-buy highlight now also selects the strongest visible offer outside the just-purchased items first, so after-purchase account tracking keeps a cleaner upsell signal instead of echoing the first catalog card
- checkout now also surfaces combined commercial exposure from the projected new order plus any visible open invoice balance, so payment and billing attention stay explicit before order placement
- order confirmation now also separates recorded payment from remaining payable exposure, so post-purchase payment follow-up can be read directly from confirmation instead of being inferred from raw attempt rows
- checkout and confirmation now also break payment exposure into order total, billing carry-over, combined exposure, and coverage state, so financial follow-up is easier to read before and after order placement
- the authenticated member dashboard now also surfaces live product highlights alongside CMS and category continuation, so signed-in shoppers can see the next buying opportunity directly from `/account`
- that member-dashboard storefront offer board now also shifts away from products already linked to the active cart when browser storefront-shopping state exists, so signed-in account entry can pitch the next buying move beyond the current basket instead of echoing it
- orders and invoices now also surface live product highlights alongside their storefront continuation windows, so member history routes can still create a next-buy moment instead of acting like passive archives
- those orders and invoices storefront offer boards now also shift away from products already linked to the active cart when browser storefront-shopping state exists, so member history routes can pitch the next buying move beyond the current basket instead of echoing it
- order and invoice detail routes now also surface live product highlights alongside their storefront continuation windows, so protected commerce detail pages can create a next-buy moment instead of only offering content and category follow-up
- public commerce hardening now also normalizes quantity/coupon/country-code input and constrains cart/confirmation status-query handling so storefront redirect/form glue stays explicit and controlled
- centralized SEO metadata shaping with canonical/Open Graph/Twitter support for public routes and explicit `noindex` policy for private/mixed portal routes
- public `robots.txt` and `sitemap.xml` generation backed by the live CMS/catalog contracts and limited to truly public/indexable storefront paths
- locale-prefixed public routing for Home/CMS/catalog plus localized language alternates on CMS/product detail by matching canonical public ids across the current `de-DE` / `en-US` public sets, so detail-page discovery no longer depends on slug-parity guesses
- sitemap generation now also includes localized CMS and product detail inventory by matching canonical public ids across the current `de-DE` / `en-US` public sets, so multilingual public discovery is no longer limited to index-level routes
- localized CMS and product detail inventory now also loads through shared cached server helpers, so detail-page alternates and sitemap discovery reuse one canonical multilingual inventory path instead of refetching public sets independently
- CMS and catalog browse cores now also load through the same shared observed-loader pattern as the broader storefront route-context stack, so public discovery assembly is more consistent, cache-aware, and traceable across list/detail/SEO surfaces
- storefront continuation snapshots for live CMS/category/product follow-up now also load through that same shared observed-loader path with explicit health summaries, so continuation windows no longer stay as the last route-local discovery assembly branch
- shell menu loading and cart view-model assembly now also run through the same shared observed-loader and health-summary path, so shell chrome and cart entry no longer keep manual diagnostics/caching branches outside the broader storefront infrastructure
- catalog and CMS campaign windows now also classify visible product opportunities into hero-offer, value-offer, price-drop, and steady-pick tiers, so merchandising framing can tell a clearer commercial story than a raw savings percentage alone
- member identity and member commerce summary snapshots now also flow through shared cached server contexts, so account, cart, checkout, confirmation, and Home can reuse the same protected summary data with less repeated fetch work
- account and commerce continuation windows now also surface browse-campaign boards built from live categories plus strongest visible offers, so recovery, portal, and conversion routes can propose stronger next-browse lanes instead of only raw follow-up links
- the web backlog is now also split between core flow delivery and a later dedicated quality pass, so UX/security/performance refinements can be completed deeply at the end without displacing the remaining main process work
- CMS detail now also exposes a direct review handoff back into the strongest CMS index window for discovery-ready versus attention-needed pages, so content review can continue from the right published set instead of restarting from the default list
- product detail now also exposes a direct review handoff back into the strongest catalog window for offer versus base-assortment review, so assortment review can continue from the right browse set instead of restarting from the default catalog
- CMS and catalog index now also surface explicit review action centers for the current window, so the strongest next review set can be reopened directly instead of manually rebuilding lens state
- CMS and catalog review windows now also break visible debt into concrete review reasons such as missing metadata, missing primary imagery, and base-assortment fallback, so teams can see what needs attention without inferring it from counts alone
- CMS and catalog review windows now also surface the next concrete review targets inside the current visible window, so teams can jump straight to the right page or product instead of hunting through the result set manually
- CMS page detail and product detail now also surface the next concrete review target from their current review sets, so teams can continue content or assortment review directly from the detail route instead of bouncing back to index lists first
- CMS and catalog cards now also show row-level review signals directly inside browse windows, so teams can spot metadata debt, image debt, and offer/base state without opening every detail route first
- CMS and catalog review now also run on shared priority helpers with focused tests, so Home, browse windows, and detail routes all point at the same next review targets instead of drifting into route-local heuristics
- CMS detail and product detail now also surface visible review queues, so item-by-item content and assortment review can continue directly from the drilled-in route instead of bouncing back to list windows after every target
- CMS detail and product detail now also preserve the active review window across drilled-in targets, so review can continue from item to item without losing the current CMS/catalog lens context
- CMS and catalog review queues plus preferred review-window reopening now also run through shared workflow helpers with focused tests, so Home, browse windows, and detail routes preserve the same review lens and next-target behavior instead of mixing route-local review logic
- Home review cockpit targets now also preserve the active CMS/catalog review window when opening the next page or product, so review can start from storefront entry without losing the intended lens context
- CMS/catalog review-window routing now also runs through shared tested helpers, so browse cards, Home review cockpit, and detail follow-up all preserve the same lens context without route-local link drift
- Home discovery now also runs through a shared cached server context with explicit timing diagnostics for core feeds and category spotlights, so storefront entry builds with less duplicate work and more observable performance behavior
- storefront runtime config now also supports `noir` and `solstice` visual directions through `DARWIN_WEB_THEME`, so the same front-office system can cover a broader real multi-brand theme set without feature rewrites
- CMS and catalog browse cards now also preserve the active review window when opening page/product detail, so review can move from list to detail without rebuilding the current lens context
- catalog browse and product detail now also load through shared cached public discovery contexts, so metadata, browse assembly, and detail follow-up reuse the same core catalog/category data instead of refetching it route-by-route
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
- `/catalog` and `/cms` now build their visible browse/review windows inside shared server page-context loaders, so lens-driven review no longer duplicates matching-set fetches in the route files and diagnostics reflect the real visible window instead of only the paged seed fetch
- shared public-discovery, protected-member, and commerce page-loader cores now emit stable loader-kind/auth-gate/continuation diagnostics and carry direct regression coverage, so production tracing can distinguish route families more quickly and core page assembly is less likely to drift silently
- multilingual CMS/product inventory for alternates and sitemap now loads through one shared public-discovery inventory snapshot, so sitemap and detail-page language discovery reuse a single cached source instead of spinning separate localized inventory loaders for pages and products
- shared route observability now also classifies diagnostics by signal type, attention level, degraded-status keys, and suggested action, so production/staging logs can distinguish slow-but-healthy work from degraded success and immediate failures without reading raw status maps by hand
- shared SEO metadata loaders now also have direct regression coverage, so canonical/no-index/language-alternate assembly for Home, discovery, commerce, and protected routes is less likely to drift silently behind route-level tests
- catalog and CMS index SEO loaders now also consume canonical route arguments instead of raw `searchParams` objects, so browse metadata caching aligns better with the shared page-context model and avoids object-shaped cache misses on repeated discovery requests
- shared multilingual discovery inventory now also carries precomputed CMS/product alternates plus sitemap-ready detail entries, so detail-page `hreflang` and public sitemap reuse one projection path instead of regrouping the same inventory in multiple loaders
- shared SEO metadata loaders now also emit canonical `seo-metadata` diagnostics with explicit indexability state, so production tracing can distinguish metadata assembly from page assembly and read indexable versus noindex outcomes directly from the log context
- shared SEO metadata diagnostics now also separate alternate detail from alternate summary and emit a compact visibility footprint, so production tracing can scan `localized`, `single-locale`, or `private` metadata state without mixing per-culture detail and roll-up summary in one field
- shared SEO diagnostics helpers now also have direct coverage for canonical alternate detail and visibility footprints, so summary/detail metadata state is less likely to drift behind the route-level SEO loader tests
- shared SEO diagnostics helpers now also cover canonical target and summary footprints directly, so metadata success diagnostics no longer depend on route-local string shaping for target identity or alternate roll-up state
- shared SEO diagnostics helpers now also classify canonical metadata state and alternate presence directly, so loader-level SEO diagnostics no longer depend on inline localized/private state branching
- shared SEO diagnostics helpers now also classify canonical indexability directly, so the full metadata visibility model no longer depends on inline `noindex` branching inside loader success diagnostics
- localized discovery diagnostics helpers now also have direct coverage for canonical state/detail/summary selection, so multilingual inventory and sitemap telemetry are less likely to drift behind loader-level tests
- route observability helpers now also have direct coverage for timing-band, signal-kind, attention-level, and suggested-action classification, so operational tracing is less likely to drift behind only end-to-end slow/degraded/failure tests
- shared multilingual discovery projections now also have direct regression coverage for alternates and sitemap assembly, so recent performance refactors around localized inventory reuse are less likely to drift silently behind route-level tests
- shared public paged-set expansion now also runs through one tested helper for CMS and catalog, so discovery-set loaders keep one consistent rule for when to widen a partial first page into the full matching set and when to fall back safely
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

## Feature Logging Rule

For `Darwin.Web`, every newly delivered capability, whether large or small, should also be summarized in this README so later operator/help-guide writing can reconstruct:

- what capability was added
- on which route or surface it appears
- when the shopper/member/operator should encounter it
- what kind of system feedback, readiness signal, warning, or follow-up action it shows

The preferred style for these additions is:

- one short behavior-oriented bullet
- business-facing wording instead of implementation-only wording
- explicit mention of the route/surface when that matters
- explicit mention of the visible feedback or follow-up when the slice changes customer/member guidance

This rule is additive:

- do not delete older implementation bullets only because newer slices exist
- update stale wording when it is no longer accurate
- keep the README usable as the living behavioral inventory for `Darwin.Web`

For broader platform documentation, see:
- [`../../README.md`](../../README.md)
- [`../../DarwinFrontEnd.md`](../../DarwinFrontEnd.md)
- [`../../DarwinWebApi.md`](../../DarwinWebApi.md)





