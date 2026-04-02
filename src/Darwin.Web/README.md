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
- home composition now also includes a recovery/follow-up rail driven by live CMS/catalog health, so degraded public entry does not collapse into a passive shell and still points visitors toward CMS, catalog, and account paths
- home composition now also includes a reusable route-map web part that links Home into real CMS page detail, real product detail, and account/loyalty follow-up routes, so public entry shows the next concrete route instead of only section teasers
- home composition now also includes category-driven storefront lanes backed by public categories plus category-filtered product contracts, so top-level browse entry is data-backed instead of a generic catalog shortcut
- home composition now also includes a live priority lane that ranks checkout, billing, loyalty, order, CMS, and catalog next steps from current public/member signals, so the storefront entry route behaves like an actionable action surface instead of only a map of routes
- home composition now also includes a live commerce-opportunity window driven by cart, spotlight-product, and category-lane signals, so the storefront entry route can push the shopper toward the strongest immediate buying move instead of only showing generic browse links
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
- the public account hub now also surfaces a dedicated storefront-readiness panel and carries the preferred post-auth destination into sign-in/register/activation/password recovery links, so public account entry preserves active cart/checkout intent instead of defaulting every auth jump back to `/account`
- the public account hub now also surfaces a live storefront action center fed by current cart state plus published CMS/category spotlights, so anonymous account entry can still move through real content and commerce next steps instead of collapsing to auth-only choices
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
- `/catalog` now also exposes a visible-result search/sort lens for the products already loaded on the current server page, while keeping that lens explicit and non-canonical until true public search/facet/sort contracts exist
- `/catalog` now also surfaces visible-vs-loaded-vs-total result summaries plus first/last page jumps, so catalog window navigation is more complete without pretending backend search/facets already exist
- `/catalog` now also surfaces an offer-focus window plus a buying-guide summary from the live visible product set, so merchandising signals stay explicit even before true backend search/facets arrive
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
- the authenticated orders route now also surfaces explicit fulfillment-readiness state for visible orders needing active follow-up, so order-history follow-up is visible from the history route instead of being inferred only from row-level statuses
- the authenticated orders route now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so order history stays connected to public content and catalog follow-up instead of behaving like a portal-only archive
- the authenticated invoices route now also surfaces explicit billing-readiness state for visible outstanding invoices and open balance, so finance follow-up is visible from the history route instead of being inferred only from row-level balances
- the authenticated invoices route now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so invoice history stays connected to public content and catalog follow-up instead of behaving like a portal-only finance archive
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
- cart now also surfaces explicit opportunity and readiness panels, so the shopper can see the strongest adjacent offer plus basket readiness before leaving the route for checkout
- checkout now also surfaces explicit confidence and attention panels, so order-readiness, billing follow-up, phone verification, and address-book coverage stay visible at the final conversion step
- storefront confirmation now also reconciles hosted-checkout return/cancel flows through the canonical payment-completion endpoint instead of acting as a passive snapshot screen
- storefront confirmation now also keeps post-checkout guidance visible, including payment-next-step messaging, account/order-history follow-up, and stable order-reference handling
- storefront confirmation now also surfaces signed-in member continuation across orders, invoices, and loyalty, so post-checkout handoff into the protected portal stays explicit instead of stopping at generic CTA buttons
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
- CMS index now also exposes a visible-result search lens over the pages already loaded on the current page, while staying explicit that true CMS search still needs a backend contract
- CMS index now also surfaces current-window result summaries for visible vs loaded vs total published pages, so public content browsing stays set-aware instead of behaving like a flat card dump
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
- CMS detail unavailable state now also keeps cross-surface follow-up visible, and CMS/catalog detail/index routes now surface explicit route-summary diagnostics so degraded public content/discovery states do not collapse into passive leaves
- CMS index and CMS detail now also surface live catalog-category and live product follow-up windows, so public content routes can hand off directly into real commerce browse/detail paths instead of only pointing back to generic catalog entry
- CMS index and CMS detail now also surface live cart/checkout continuity from the canonical public cart contract, so published content can hand off directly into an already active purchase flow instead of only into browse routes
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
- member commerce detail hardening for order/invoice documents, shipment/payment snapshots, and linked invoice/order follow-up
- public cart coupon apply/clear plus richer tax/shipping/billing presentation across cart and checkout summary surfaces
- checkout now also exposes readiness signals and a live order-review panel so the shopper can validate address/shipping/line items before order placement
- cart and checkout now also surface a live storefront-discovery window backed by published CMS pages, public categories, and visible product opportunities, so active purchase routes can hand shoppers back into content, browse, and upsell paths without dropping the current conversion flow
- order confirmation now also surfaces that same live storefront-discovery window, so after-purchase shoppers can move directly into content, browse, and product follow-up instead of ending on a receipt-only route
- order confirmation now also biases its product follow-up away from items already present in the just-finished order, so the after-purchase opportunity is closer to a real next-buy suggestion than a generic catalog echo
- the authenticated member dashboard now also surfaces live product highlights alongside CMS and category continuation, so signed-in shoppers can see the next buying opportunity directly from `/account`
- public commerce hardening now also normalizes quantity/coupon/country-code input and constrains cart/confirmation status-query handling so storefront redirect/form glue stays explicit and controlled
- centralized SEO metadata shaping with canonical/Open Graph/Twitter support for public routes and explicit `noindex` policy for private/mixed portal routes
- public `robots.txt` and `sitemap.xml` generation backed by the live CMS/catalog contracts and limited to truly public/indexable storefront paths
- locale-prefixed public routing for Home/CMS/catalog plus index-level language alternates where slug mapping is unambiguous

For broader platform documentation, see:
- [`../../README.md`](../../README.md)
- [`../../DarwinFrontEnd.md`](../../DarwinFrontEnd.md)
- [`../../DarwinWebApi.md`](../../DarwinWebApi.md)

