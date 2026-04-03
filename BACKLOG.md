# Darwin Backlog

This document is the execution roadmap for Darwin. It is intentionally priority-based, not merely historical. The current guiding rule is:

- finish `Darwin.WebAdmin` and the admin/backend workflows needed for real SME operations first
- keep mobile-used API paths stable
- return to broader front-office and non-critical expansion after the operational core is ready

Status terms used below:

- `Completed`
- `In Progress`
- `Planned / Near-term`
- `Future / Later phase`

## 0. Cross-Chat Coordination Ledger

This section is the shared handoff ledger between the active implementation chats:

- `Web`
- `WebAdmin`
- `Mobile`

Rules:

- before starting a new slice, each chat should review this ledger
- each handoff should be recorded as one flat entry and updated in place when acknowledged or closed
- use these statuses:
  - `Open`
  - `Acked`
  - `Closed`
  - `Superseded`

Entry format:

- `[Status] YYYY-MM-DD | From: <Web|WebAdmin|Mobile> | To: <Web|WebAdmin|Mobile> | Topic: <short topic> | Message: <concrete handoff> | Expected action: <what the receiving chat should do>`

Active entries:

- `[Closed] 2026-03-31 | From: Web | To: WebAdmin | Topic: CMS main navigation seed | Message: Darwin.Web needed a published public menu named main-navigation plus stable CMS/catalog seed content so shell and storefront slices could stop depending on hidden fallback-only behavior. | Expected action: seed and publish the menu/content in WebAdmin/backend.`
- `[Closed] 2026-03-31 | From: WebAdmin | To: Web | Topic: CMS/storefront dependencies confirmed | Message: main-navigation is ready, CMS published page slugs such as ueber-uns and faq are available, representative catalog seed with real primary media exists, menu badge/icon/group metadata is not yet contract, publish rules are confirmed, and missing-image policy must be handled in Darwin.Web. | Expected action: treat main-navigation as the primary source, keep fallback nav emergency-only, consume CMS/catalog directly from WebApi, make fetch failures visible, and keep backlog/docs aligned with these confirmed contracts.`
- `[Closed] 2026-03-31 | From: WebAdmin | To: Web | Topic: Phone verification and multi-channel communication contracts | Message: backend now exposes canonical current-user phone verification over SMS or WhatsApp, persisted preferred-channel plus fallback policy in site settings, customizable phone-verification text templates, customizable provider-backed SMS/WhatsApp transport-test templates, and a new CustomerProfile flag for phone-number confirmation. | Expected action: when building account/profile UX, consume the new profile field and the request/confirm phone-verification endpoints instead of inventing a web-only mobile-verification flow.`
- `[Open] 2026-03-31 | From: WebAdmin | To: Mobile | Topic: Phone verification and multi-channel communication contracts | Message: backend now exposes canonical current-user phone verification over SMS or WhatsApp, persisted preferred-channel plus fallback policy in site settings, customizable phone-verification text templates, customizable provider-backed SMS/WhatsApp transport-test templates, and a new CustomerProfile flag for phone-number confirmation. | Expected action: during the mobile review/use-case pass, consume the new profile field and the request/confirm phone-verification endpoints instead of keeping any app-local verification workflow assumptions.`
- `[Acked] 2026-04-01 | From: WebAdmin | To: Web,Mobile | Topic: de/en localization baseline in admin | Message: WebAdmin now enforces a settings-driven `de-DE`/`en-US` request-localization baseline with cookie/query-string culture switching and bilingual shared/admin-auth chrome. Site Settings and Business Communications have started moving onto the same bilingual operator surface. | Expected action: assume German and English are the current platform languages, keep frontend/mobile wording compatible with that baseline, and treat future languages as additive rather than redefining the initial contract.`
- `[Acked] 2026-04-01 | From: Web | To: WebAdmin,Mobile | Topic: resource-backed front-office localization baseline | Message: Darwin.Web now has a resource-bundle localization slice under `src/Darwin.Web/src/localization/resources` for shared shell, catalog, storefront-commerce, account self-service, member commerce, and loyalty surfaces including discovery, pre-join detail, and signed-in business detail. Culture switching still uses the de/en platform baseline, but new frontend languages should be added as new resource bundles rather than in-component dictionaries. | Expected action: keep future shared wording and language additions compatible with the additive `de-DE` / `en-US` baseline and avoid introducing app-local hardcoded copy paths for common front-office/member wording.`
- `[Open] 2026-04-02 | From: Web | To: WebAdmin | Topic: localized slug mapping for SEO alternates | Message: Darwin.Web now supports locale-prefixed public URLs for Home/CMS/catalog, but true `hreflang` alternates on CMS/product detail routes still need canonical localized slug mapping or per-culture slug metadata from WebApi. Without that, web can only emit language alternates safely on index-level public routes. | Expected action: decide whether public CMS/product contracts should expose localized slug maps or another canonical culture-linking mechanism so detail-page alternate URLs can be emitted without guessing slug parity across languages.`
- `[Open] 2026-04-02 | From: Web | To: WebAdmin | Topic: catalog search and facet contract expansion | Message: Darwin.Web now exposes only page-local visible-result search/sort on `/catalog` because the current public catalog contracts do not yet provide global query search, facet counts, brand filters, or server-side sorting. The web slice is keeping that limitation explicit instead of faking a scalable storefront search experience. | Expected action: decide whether public catalog should expose query search, sort options, and facet/filter projections so Darwin.Web can replace the current page-local lens with true catalog discovery.`
- `[Open] 2026-04-02 | From: Web | To: WebAdmin | Topic: CMS search contract expansion | Message: Darwin.Web now exposes only a page-local visible-result search lens on `/cms`, plus grouping/reading/navigation rails derived from the currently loaded page window. The web slice is keeping that limitation explicit and non-canonical instead of pretending it already has true CMS search or topic curation. | Expected action: decide whether public CMS should expose query search, result metadata, or lightweight grouping signals so Darwin.Web can replace the current page-window lens with real public content discovery.`
- `[Open] 2026-04-02 | From: Web | To: WebAdmin | Topic: payment-attempt ordering metadata for confirmation/member commerce | Message: Darwin.Web can now render storefront confirmation and member commerce payment states, but the public/member payment snapshots only expose `status` and optional `paidAtUtc`. When multiple payment attempts exist, web cannot safely derive the latest attempt state without either attempt ordering guarantees or a creation timestamp on each payment snapshot. The current web fix now avoids showing a stale inferred "latest" status, but richer route summaries still need canonical payment-attempt ordering metadata from WebApi. | Expected action: decide whether public/member payment snapshot contracts should expose attempt ordering or `createdAtUtc` so web/mobile can display the current payment-attempt state without heuristic guessing.` 

## 1. Current Delivery Focus

### Primary focus

- `In Progress`: complete `Darwin.WebAdmin` as the first operational control center
- `In Progress`: support real onboarding and day-one operations for small and medium businesses
- `In Progress`: make backend + admin workflows sufficient for `Darwin.Mobile.Business` early operational usage

### Phase-1 provider decisions

- `Planned / Near-term`: payment implementation is `Stripe-first`
- `Planned / Near-term`: shipping implementation is `DHL-first`
- `Future / Later phase`: additional payment providers and market-specific payment methods
- `Future / Later phase`: additional shipping/logistics providers

## 2. Go-Live Critical

### WebAdmin operational completion

- `In Progress`: complete remaining operator workflows in `Darwin.WebAdmin` so every sidebar module has usable list/detail/create/edit/support actions
- `Completed foundation`: a broad HTMX-first completion audit has now covered the main high-traffic and lower-traffic WebAdmin workspaces, including CRM, orders, inventory, catalog, CMS, identity, business support, communication ops, and payment/shipping support
- `Planned / Near-term`: run one final ultra-low-traffic audit pass only if we want to exhaust the remaining microscopic detours before leaving WebAdmin completion work
- `Completed foundation`: the products workspace now exposes queue-style filters, live summary cards, and catalog-ops playbooks for inactive, hidden, single-variant, and scheduled products, so catalog follow-up is less dependent on scanning a flat list
- `Completed foundation`: the pages workspace now exposes queue-style filters, live summary cards, and content-ops playbooks for draft, published, windowed, and currently live windowed pages, so CMS follow-up is less dependent on a flat list
- `Completed foundation`: the categories workspace now exposes queue-style filters, live summary cards, and playbooks for inactive, unpublished, root, and child category review, so catalog-structure follow-up is less dependent on a flat list
- `Completed foundation`: the brands workspace now exposes queue-style filters, live summary cards, and playbooks for unpublished, missing-slug, and missing-logo brands, so brand-setup debt is easier to work as an operator queue
- `Completed foundation`: the add-on groups workspace now exposes queue-style filters, attachment visibility, live summary cards, and configuration playbooks for inactive, global, unattached, and linked groups, so menu-configuration support is less dependent on a flat list
- `Completed foundation`: the admin media library now supports queue-style filters for missing alt text, editor-only assets, and library assets, so content cleanup can be worked as an operator queue instead of a flat gallery
- `Completed foundation`: the media queue now also exposes direct `Open File` and contextual `Set Alt` follow-up actions plus clearer asset-role badges, so routine content cleanup is less dependent on opening every asset blindly
- `Completed foundation`: the media workspace now also exposes `Missing Title`, live summary cards, and media-ops playbooks, so accessibility and library-hygiene follow-up is easier to work from the main asset queue
- `Completed foundation`: the CRM customers list now supports queue-style filters for linked-user customers, customers needing segmentation, and customers with open opportunity context, making customer operations less list-heavy
- `Completed foundation`: the CRM customers queue now also exposes direct quick actions for linked user review, interactions, segment membership work, and prefilled opportunity creation, so common follow-up no longer always requires manually navigating through the full customer edit flow
- `Completed foundation`: the CRM leads list now also supports queue-style filters for qualified, unassigned, and unconverted leads, so pipeline follow-up can be worked as an operator queue instead of a flat list
- `Completed foundation`: the CRM opportunities list now supports queue-style filters for open, closing-soon, and high-value opportunities, so revenue follow-up is less dependent on a flat pipeline list
- `Completed foundation`: CRM lead and opportunity lists now expose richer follow-up context and direct quick actions, including assigned-owner visibility, quick lead conversion for qualified rows, customer deep-links, and prefilled opportunity creation from customer-linked leads
- `Completed foundation`: the CRM invoices queue now also exposes direct quick actions for customer/order/payment follow-up and common draft-to-open / open-to-paid transitions, so routine invoice operations no longer always require entering the full invoice editor first
- `Completed foundation`: the CRM invoice queue and invoice editor now also expose net/tax/gross splits together with the live VAT and invoice-issuer policy snapshot from site settings, so finance and support operators can review tax context without leaving the CRM workflow
- `Completed foundation`: CRM customers now carry an explicit B2C/B2B tax profile plus optional VAT ID, and both the customer queue and invoice screens surface that state so operators can triage business customers and missing VAT metadata directly from WebAdmin
- `Completed foundation`: the CRM customers queue now also exposes locale source and platform-fallback usage through HTMX-safe search/filter/action paths, so localization follow-up can be worked as an operator queue instead of only from settings guidance
- `Completed foundation`: CRM invoice review, invoice editing, and order tax snapshots now also hand off directly into tax settings, issuer-data cleanup, invoice review, and customer VAT-profile follow-up, so phase-1 compliance support can move from readiness indicators into operator remediation without pretending a full e-invoice engine exists
- `Completed foundation`: the CRM overview plus lead, opportunity, and invoice workspaces now also render through HTMX-aware workspace helpers with in-shell search, subset, pager, and editor handoff paths, so these lower-traffic CRM queues no longer fall back to older full-page navigation while the broader completion audit closes out the remaining legacy detours
- `Completed foundation`: the admin orders list now supports queue-style filters for open orders, payment-issue orders, and fulfillment-attention orders, so post-order operations are less dependent on a flat status list
- `Completed foundation`: the orders queue and order-details workspace now also render through HTMX-aware helpers, and add-payment/shipment/refund/invoice flows now return through the same shell-based workflow, so post-order operations no longer mix fragment-driven follow-up with older full-page queue/detail detours
- `Completed foundation`: the lower-traffic order detail grids for payments, refunds, invoices, and shipments now also keep pager, editor/config handoff, and tax-remediation links inside HTMX-safe shells, which closes another completion-audit layer in finance and fulfillment follow-up
- `Completed foundation`: order-detail status-change and allocation POST flows now also stay inside the HTMX shell, which closes another small completion-audit detour in lower-traffic operational follow-up
- `Completed foundation`: CRM customer creation now uses the same HTMX-aware editor entry path as the rest of the customer workflow, and the segments workspace plus customer/segment back-navigation now also stay inside shell-based search/filter/edit flows, which closes another set of lower-traffic CRM detours found during the completion audit
- `Completed foundation`: the remaining CRM invoice-editor order/customer handoff and tax-remediation links now also stay inside HTMX-safe shells, which closes another completion-audit layer in lower-traffic CRM remediation paths
- `Completed foundation`: the remaining shipping-method editor cancel path now also stays inside the HTMX workspace shell, which closes another small completion-audit detour in lower-traffic shipping configuration
- `Completed foundation`: add-on group attachment entry handoffs from the main queue now also stay inside the HTMX workspace shell, which closes another small completion-audit detour in lower-traffic catalog configuration
- `Completed foundation`: stale or missing add-on-group attachment routes now also redirect back through HTMX-safe workspace paths instead of dropping to plain not-found responses, which closes another small controller-level completion-audit detour in lower-traffic catalog configuration
- `Completed foundation`: missing-row edit routes in pages, categories, and products now also redirect back through HTMX-safe workspace paths instead of dropping to plain not-found responses, which closes another small controller-level completion-audit detour in lower-traffic catalog/CMS configuration
- `Completed foundation`: the remaining page/media editor back and cancel paths now also stay inside their HTMX shells, which closes another small completion-audit gap in lower-traffic CMS/media maintenance flows
- `Completed foundation`: order detail tabs for payments, shipments, refunds, and invoices now support queue-style filters too, so operators can work failed/refunded/pending/outstanding subsets without scanning full grids
- `Completed foundation`: the orders queue now also exposes direct quick actions for add payment, add shipment, and create invoice from list rows, so common support follow-up no longer always requires entering the full order detail first
- `Completed foundation`: inventory purchase orders and stock levels now support queue-style filters for draft/issued/received replenishment work and low-stock/reserved/in-transit stock review, so inventory ops are less dependent on flat lists
- `Completed foundation`: stock transfers now support queue-style filters for draft, in-transit, and completed transfer work, so replenishment follow-up is less dependent on manual scanning
- `Completed foundation`: warehouse and supplier lists now support queue-style filters for default/no-stock-level warehouses and missing-address/active suppliers, so inventory setup review is less dependent on flat lists
- `Completed foundation`: inventory ledger now supports queue-style filters for inbound, outbound, and reservation movements, and stock-level rows link directly into variant ledger review for faster stock troubleshooting
- `Completed foundation`: stock-level rows now expose direct `adjust`, `reserve`, and `release reservation` actions in WebAdmin, so inventory troubleshooting can move from queue detection into manual corrective action without leaving the operational context
- `Completed foundation`: stock-level rows now also expose a direct `return receipt` action, so phase-1 customer return intake can increase inventory from the same operational workspace without dropping into ad hoc scripts or database fixes
- `Completed foundation`: supplier rows now deep-link into purchase-order follow-up and warehouse rows deep-link into scoped stock-level review, reducing drill-in friction for procurement and stock support work
- `Completed foundation`: the remaining inventory workspaces for warehouses, suppliers, stock levels, stock transfers, purchase orders, and variant ledger now also render through HTMX-aware workspace helpers with in-shell search/filter/pager/editor/action flows, so inventory support no longer mixes modern editor shells with older full-page list detours
- `Completed foundation`: WebAdmin now has first-class loyalty workspaces for programs, reward tiers, accounts, campaigns, and recent scan-session diagnostics, so business-mobile loyalty operations are no longer managed only from the mobile app or raw API usage
- `Completed foundation`: WebAdmin now has a dedicated `Mobile Operations` workspace for JWT/mobile bootstrap settings, onboarding/support dependency counts, and transport-readiness visibility that affect the mobile apps directly
- `Completed`: WebAdmin loyalty operations now support admin-side account provisioning for member-support cases where a consumer has not self-enrolled yet
- `Completed`: WebAdmin loyalty operations now expose a dedicated redemption troubleshooting workspace with pending/completed/cancelled filters and direct confirm actions for pending redemptions
- `Completed`: Mobile Operations now includes real device-fleet diagnostics, app-version visibility, and device-level filters for stale installs, missing push tokens, notification-disabled devices, and business-member devices
- `Completed`: Mobile Operations now also supports lightweight device remediation through admin-side push-token clearing and device deactivation
- `Completed`: admin dashboard discoverability for loyalty/mobile is now in place through a compact snapshot with direct entry points into loyalty accounts, pending redemptions, scan sessions, and mobile device diagnostics
- `Completed`: business-member support now also exposes admin-side staff access badge preview and refresh, so operators can mirror the rotating business-app QR badge for checkpoint/support troubleshooting without leaving WebAdmin
- `Planned / Near-term`: extend mobile diagnostics beyond the current device/version snapshot only if support scope demands push-delivery telemetry, richer scanner/session failure analytics, or per-device remediation workflows

### Business and tenant onboarding

- `In Progress`: business creation and onboarding workflow now has a real WebAdmin foundation, not only raw entity persistence
- `Planned / Near-term`: support tenant/customer provisioning for new business onboarding
- `In Progress`: support owner/admin assignment during onboarding
- `In Progress`: support invitation-based onboarding for owners/staff when no existing platform user is available
- `Completed`: `Darwin.Mobile.Business` now supports token-entry invitation preview and acceptance for phase-1 onboarding
- `Completed`: WebAdmin now supports business approval, suspension, reactivation, and an onboarding-readiness checklist covering owner, primary location, legal name, and contact email
- `Completed`: business edit screens now expose actionable onboarding shortcuts, so missing owner/location/profile steps can be completed directly from the onboarding shell
- `Completed`: businesses index now supports operational-status and needs-attention filtering, so approval and onboarding queues are easier to work through operationally
- `Completed`: business-facing access-state API now exposes approval/suspension readiness to authenticated business clients
- `Completed`: phase-1 `Soft Gate` policy is now implemented in `Darwin.Mobile.Business`: pending-approval businesses can sign in and complete setup, but live operations stay blocked until approval
- `In Progress`: model and expose richer onboarding state, activation state, approval state, and suspension/reactivation rules beyond the current soft-gate snapshot
- `In Progress`: seed and apply initial defaults during onboarding (locale, time zone, branding basics, payment/shipping defaults, communication defaults where applicable)
- `Completed`: business onboarding now includes a dedicated setup workspace in WebAdmin for grouped profile/defaults editing, onboarding shortcuts, and visibility into phase-1 global settings dependencies
- `Completed`: the business setup workspace now also shows inline previews for members needing support action and open invitations, reducing page-hopping during onboarding troubleshooting
- `Completed foundation`: the business setup workspace now also shows a business subscription snapshot and the key business-app legal/billing handoff dependencies, so support/admin can audit mobile-facing account state without leaving WebAdmin
- `Completed foundation`: WebAdmin now also has a dedicated business subscription workspace with current plan snapshot, active-plan readiness, external billing-website handoff visibility, and FullAdmin cancel-at-period-end control, so subscription support is no longer trapped in the mobile app or external support playbooks
- `Completed foundation`: the same subscription workspace now also exposes provider invoice history with queue-style triage for open/paid/draft/uncollectible cases, hosted/PDF visibility, and direct payment follow-up links, so subscription billing support no longer depends only on the provider dashboard
- `Completed foundation`: that subscription invoice workspace now also exposes `Overdue` and `PDF Missing` triage plus direct refund-queue handoff, so subscription billing support can split collection problems from provider-document gaps inside WebAdmin
- `Completed foundation`: the payments workspace now also exposes webhook-lifecycle visibility with active subscription counts, pending/failed/retry delivery signals, and a dedicated webhook queue for callback history, so Stripe-first support can inspect callback drift without leaving WebAdmin
- `Completed foundation`: the billing webhook queue now also behaves as an HTMX-aware workspace, with filter/reset, queue subsets, and returns to payments/settings staying inside the same fragment workflow, so payment-support triage no longer mixes modern queue handoff with older full-page callback navigation
- `Completed foundation`: the business subscription workspace and its invoice queue now honor HTMX-driven load/filter/update paths too, so cancel-at-period-end changes and invoice triage no longer fall back to full-page refresh behavior inside subscription support workflows
- `Completed foundation`: the subscription invoice queue now also exposes HTMX-driven quick triage subsets for open, overdue, PDF-missing, Stripe, and uncollectible cases, so monthly-contract support can jump directly into the right billing queue without rebuilding filters by hand
- `Completed foundation`: the main business-subscription workspace now also exposes direct triage handoff actions for open, overdue, PDF-missing, and uncollectible invoice queues, so monthly-contract support can jump from subscription summary signals into the right follow-up queue immediately
- `Completed foundation`: navigation between the main business-subscription workspace and the subscription-invoice queue now also stays inside the HTMX workflow, so billing triage can move between summary and invoice queues without dropping back to older full-page navigation
- `Completed foundation`: business-subscription and subscription-invoice workspaces now also hand off internally to business edit/setup, payments, refunds, and payment/site-setting review through HTMX-aware routes, so monthly-contract support no longer mixes fragment-driven billing triage with older full-page admin detours
- `Completed foundation`: the payments workspace now also opens record/edit payment flows through HTMX-aware entry paths, and the payment editor now returns through the same fragment workflow, so finance support no longer drops back to older full-page payment-maintenance routes
- `Completed foundation`: subscription-plan handoff links are now business-aware and plan-aware, so admins can start or upgrade a business against a specific monthly plan from WebAdmin instead of only opening a generic external billing website
- `Completed foundation`: site settings now also expose phase-1 Stripe and DHL provider configuration from the admin UI, so payment/shipping credentials and shipper defaults are no longer config-file-only
- `Completed foundation`: site settings now also expose phase-1 VAT/invoicing defaults, including VAT mode, default VAT rate, reverse-charge allowance, and invoice-issuer identity, and the payments workspace now surfaces that readiness directly for billing support
- `Completed foundation`: delegated business-support access now exists in WebAdmin through a dedicated permission/role path for member support and invitation operations without exposing approval, lifecycle, or owner-management actions
- `Completed foundation`: identity/admin screens now surface the delegated business-support role and permission more clearly, so assigning support access no longer depends on tribal knowledge
- `Completed foundation`: the users workspace now also exposes queue-style lifecycle filters and quick support actions for unconfirmed, locked, inactive, and mobile-linked accounts, so admin-side identity support is no longer trapped inside individual user-edit pages
- `Completed foundation`: the roles and permissions workspaces now also expose queue-style filters, live summary cards, and lightweight ops playbooks for system/custom/delegated-support review, so access-governance cleanup is less dependent on scanning flat lists
- `Completed foundation`: user create/edit, change-email, change-password, role-assignment, role create/edit, and permission create/edit flows now all honor the standard HTMX shell pattern on initial load as well as validation rerender, so the access-management workspace no longer falls back to older full-page behavior when launched from HTMX-driven admin surfaces
- `Completed foundation`: the users queue now also behaves as an HTMX-aware workspace, so search, filter, page-size changes, and lifecycle subset navigation stay inside the newer server-rendered workflow instead of dropping back to older full-page list refreshes
- `Completed foundation`: the same users queue now also supports in-place activation, lock, and unlock actions from list rows, so common identity-support triage no longer has to leave the HTMX workspace to complete routine account-state changes
- `Completed foundation`: user-role assignment now also supports HTMX-aware entry and return paths from both the users queue and the user editor, so access-governance changes can stay inside the newer server-rendered workflow instead of detouring through separate full-page navigation
- `Completed foundation`: the user-role editor now also returns to the user editor through an HTMX-aware back path, so role-assignment follow-up no longer falls back to a legacy full-page route when launched from account maintenance
- `Completed foundation`: the remaining loyalty campaign queue entry/edit paths now also stay inside the HTMX workspace shell, reducing one of the last plain-link detours in the loyalty priority area
- `Completed foundation`: users queue and user-editor navigation now also stay inside HTMX-aware shells for create, edit, change-email, change-password, and back-navigation paths, so identity support no longer mixes fragment-driven triage with older full-page account-maintenance routes
- `Completed foundation`: the roles queue now also opens create, edit, and permission-assignment flows through HTMX-aware entry paths, and their back navigation stays inside the same server-rendered workflow instead of bouncing through separate full-page list/detail routes
- `Completed foundation`: the roles and permissions queues now also behave as HTMX-aware workspaces, so access-governance search, filter, and page-size changes stay inside the newer server-rendered workflow instead of dropping back to older full-page list refreshes
- `Completed foundation`: the roles and permissions queues now also open create/edit and permission-maintenance flows through HTMX-aware entry paths, and their back navigation stays inside the same server-rendered workflow instead of bouncing through older full-page routes
- `Completed foundation`: role-permission assignment now also has an HTMX-safe editor shell, so permission-binding updates can stay inside the newer server-rendered workflow without depending on the legacy encoded full-page editor
- `Completed foundation`: the WebAdmin dashboard now exposes a business-support queue with onboarding/support counts, and the business member/invitation lists now support actionable queue filters for pending activation, locked users, and open invitations
- `Completed foundation`: the businesses index now exposes quick queue shortcuts for needs-attention, pending-approval, and suspended businesses, reducing filter setup for support/admin operators
- `Completed foundation`: dashboard cards for communication operations and business-support queues are now HTMX-refreshable partials, so operators can refresh live snapshots without reloading the full admin dashboard
- `Completed foundation`: WebAdmin now also has a dedicated `Business Support Queue` workspace that combines attention businesses and recent failed communication events, reducing page-hopping during onboarding and support triage
- `Completed foundation`: the dedicated `Business Support Queue` workspace is now split into HTMX-refreshable summary, attention, and failed-email fragments, so operators can refresh triage data without reloading the full page

### Authentication and account lifecycle

- `In Progress`: complete signup, invitation, activation, forgot-password, and reset-password operational flows
- `Completed`: WebAdmin now supports lock/unlock, admin email confirmation override, and password-reset initiation support for operator troubleshooting
- `Completed`: platform now has a real confirm-email token flow plus resend-activation email issuance via public auth endpoints and WebAdmin support actions
- `Completed`: password sign-in now enforces account lockout and email-confirmation state for phase-1 activation policy
- `Completed`: consumer registration no longer auto-signs-in when confirmation is pending; it now waits for email confirmation explicitly
- `Completed`: consumer and business mobile login screens now provide self-service activation email requests against the canonical member-auth route
- `Completed`: business-app token refresh now preserves preferred business context during onboarding-safe refresh cycles
- `In Progress`: ensure broader business-user status directly affects access in mobile and admin-backed support workflows beyond lockout/confirmation alone
- `Completed`: Darwin.Web now surfaces equivalent self-service resend-activation UX inline on account hub, sign-in, register, and password-recovery routes, all reusing the canonical activation-request endpoint instead of hiding recovery behind the dedicated activation page

### Communication Core (email-first MVP)

- `Planned / Near-term`: implement minimum viable Communication Core with email as the first operational channel
- `Planned / Near-term`: support signup email, account activation email, invitation email, forgot-password email, reset-password email, and important account notifications
- `In Progress`: business invitation emails now exist via SMTP-backed operational email sending, but still use simple transactional composition rather than a full Communication Core template/logging model
- `In Progress`: phase-1 Communication Core admin support now includes template visibility, delivery history context, severity/backlog triage, controlled retry for failed or pending invitation, activation, and password-reset rows, policy-aware retry state (`ready`, `cooldown`, `rate-limited`, blocked reason, recent-chain volume), exact-chain exploration for one recipient/flow/business path, provider-backed SMS/WhatsApp transport test sends with customizable diagnostic templates, canonical SMS/WhatsApp phone-verification flows with persisted preferred-channel/fallback policy plus customizable text templates, and a persisted SMS/WhatsApp audit baseline for live verification and operator-diagnostic sends; deeper outbox/provider abstraction and richer delivery-log modeling remain near-term
- `Completed foundation`: the non-email communication workbench now also exposes live SMS/WhatsApp message-family context both in exact-chain incident mode and in the main workspace/profile views, including effective template text, rendered sample preview, supported tokens, target path, policy note, safe-usage guidance, rollout boundary, direct handoff back into the relevant site-settings fragment, and family-level operator actions for verification-audit drill-in and safe diagnostic test reruns, so operators can understand and act on live verification and diagnostic messaging without reconstructing template context from separate screens or guessing whether a family is safe for broader use
- `Completed foundation`: the non-email audit queue now also exposes `Heavy Chains` triage with per-row chain volume/profile context and a dedicated subset, so recurring SMS/WhatsApp delivery stress is easier to separate from isolated verification or diagnostic incidents

### Payment integration

- `Planned / Near-term`: replace generic hosted-checkout/payment completion flow with Stripe-specific integration
- `Planned / Near-term`: support Stripe payment intent/session references in the domain and application model
- `Planned / Near-term`: implement Stripe webhook verification and lifecycle handling
- `Planned / Near-term`: expose Stripe-related provider references, status history, and support visibility in WebAdmin

### Shipping integration

- `Planned / Near-term`: replace generic shipping assumptions with DHL-specific implementation
- `Planned / Near-term`: support DHL label creation, tracking visibility, and delivery lifecycle handling
- `Planned / Near-term`: surface DHL shipment visibility and operator support actions in WebAdmin
- `Completed foundation`: WebAdmin now exposes a dedicated shipping-method module with list/create/edit, queue filters, and editable rate tiers, so phase-1 shipping configuration no longer lives only in application code
- `Completed foundation`: WebAdmin now also exposes a cross-order shipment queue for pending/shipped operational follow-up, so shipment review is no longer trapped inside individual order-detail pages

### Settings foundation

- `Planned / Near-term`: formalize a settings architecture that separates global/system settings from tenant/business settings
- `Planned / Near-term`: redesign settings IA into discoverable categories instead of allowing settings to sprawl into one ambiguous page
- `Planned / Near-term`: model default locale/language, branding, payment, shipping, communication, security, and tax/invoicing settings explicitly
- `Completed foundation`: global settings now include phase-1 Stripe payment credentials/webhook identity and DHL carrier credentials/shipper defaults, aligned with the Stripe-first and DHL-first rollout strategy
- `In Progress`: global settings now also drive a real de/en localization baseline for WebAdmin itself, including supported-cultures governance, request-culture switching, and early bilingual operator surfaces that are designed to admit more languages later without reworking the contract
- `In Progress`: localization expansion is now active on high-traffic operator workspaces too, with `Site Settings`, `Business Communications`, `Orders`, and `Businesses` moving onto the same de/en baseline so operational admins do not switch between bilingual and monolingual surfaces
- `In Progress`: the same de/en expansion is now reaching `CRM` and `Billing` entry queues as well, so customer and finance operators can work inside the same bilingual admin contract instead of falling back to English-only queue shells
- `In Progress`: localization expansion now also covers key `Inventory` and `Business support/setup` workspaces, so stock triage and onboarding support no longer sit outside the bilingual operator baseline
- `In Progress`: communication-detail and billing-webhook/payment-detail surfaces are now also moving onto the same de/en baseline, so operator audit/reconciliation workbenches stop lagging behind the main bilingual admin shell
- `In Progress`: CRM lead/opportunity/invoice queues plus inventory supplier/purchase-order/stock-transfer queues are now also moving onto the same de/en baseline, so more high-traffic workbenches stay inside the required bilingual admin contract instead of falling back to English-only queue surfaces
- `In Progress`: orders detail/tab grids plus CRM segments and inventory ledger are now also moving onto the same de/en baseline, so deeper operator drill-in surfaces stop lagging behind the bilingual queue shells
- `In Progress`: billing payment-detail editing plus CRM/inventory editor shells are now also moving onto the same de/en baseline, so operators do not drop back to English-only forms after drilling into workflow actions
- `In Progress`: shared alert controls plus common form-help text and dynamic add/remove line/address controls are now also moving onto the de/en baseline, so workflow guidance and feedback stop lagging behind the bilingual queue/editor chrome
- `In Progress`: CRM and inventory controller feedback now also move onto key-based de/en TempData messaging for success/not-found/concurrency flows, and lower-traffic billing/purchase-order editor shells now use the same bilingual baseline, so operators do not drop back to English-only feedback after corrective actions
- `In Progress`: billing controller success/not-found/concurrency feedback now also moves onto the same key-based de/en path, and remaining low-traffic billing, shipping-method, and identity editor forms are being pulled onto the same bilingual baseline, so finance and support workflows stop diverging after drill-in
- `In Progress`: auth, identity, site-settings, and shipping-method hard-coded server-side messages are now also moving onto the same key-based de/en path, so bilingual coverage is starting to reach login, password, user-role, address, and settings/shipping correction feedback instead of stopping at workspace chrome
- `In Progress`: WebAdmin localization is now shifting toward a `resx-first` ASP.NET Core path (`IStringLocalizer` + `Resources`) with the existing dictionary kept as a temporary fallback, so future languages can be added through standard resource files without breaking the current bilingual baseline during migration
- `In Progress`: the same `resx-first` path now also backs shared DataAnnotations localization, so form labels and validation metadata can migrate toward the standard ASP.NET Core resource model instead of remaining tied to hard-coded attribute strings forever
- `In Progress`: more shared action/editor/form keys are now being migrated from the legacy in-code dictionary into `resx`, so the bilingual runtime increasingly uses standard resource files instead of treating `resx` as a thin shell around the old map
- `In Progress`: the shared localization marker/resource structure has been simplified to a root-level `SharedResource` plus `Resources/SharedResource*.resx`, so the standard ASP.NET Core `resx` path stays obvious now and can later split into feature-specific resource files only when a real bounded-context need appears
- `In Progress`: identity form metadata is now also starting to consume the shared `resx` path for labels and required/min-length/compare-style validation text, so the migration is beginning to touch DataAnnotations-backed form UX instead of only controller/view helper strings

### Access and support operations

- `In Progress`: continue improving users, roles, permissions, and business-aware admin visibility
- `Planned / Near-term`: add stronger auditability for sensitive admin actions
- `Planned / Near-term`: add support-oriented admin views for troubleshooting onboarding, auth, payment, shipping, and communication issues

## 3. High Priority / Near-Term

### WebAdmin module completion and refinement

- `Completed`: HTMX is established in shared WebAdmin layouts and representative module flows
- `Completed`: CRM, inventory, billing, media, CMS, catalog, identity, and order modules are exposed in WebAdmin
- `Completed`: business/tenant management foundation now exists in WebAdmin with business CRUD, owner assignment, member management, and location management
- `Completed`: business invitations can now be created, listed, resent, and revoked from WebAdmin
- `Completed`: business member lists in WebAdmin now expose activation/lock visibility for faster onboarding troubleshooting
- `Completed`: business member management now includes inline support actions for activation resend, email confirmation, password reset email, and lock/unlock without leaving the business workspace
- `Completed`: business setup and member workspaces now expose owner-override audit visibility so sensitive FullAdmin ownership exceptions can be reviewed operationally
- `Completed`: business list/detail views now surface operational lifecycle state instead of relying only on `IsActive`
- `Completed foundation`: business create/edit, location create/edit, member create/edit, and invitation-create flows now respect the standard HTMX shell pattern on both initial load and validation rerender, so the core onboarding workspace no longer falls back to older full-page editor behavior when launched from HTMX-driven admin surfaces
- `Completed foundation`: the business setup and business editor shells now also keep their main onboarding, subscription, payment, communication, and site-settings shortcuts inside HTMX-driven navigation paths, so business support no longer mixes fragment-driven setup with older full-page admin detours
- `Completed foundation`: the business members, invitations, and locations workspaces now also keep their main search, triage, edit, invitation-support, lock/reset/activation, and back-navigation flows inside HTMX-driven shells, so subscriber onboarding/support work can stay in one server-rendered workflow instead of bouncing through older full-page routes
- `Completed foundation`: the core business onboarding forms now also return through HTMX-aware cancel/back paths for business, member, location, and invitation editing, so the final maintenance steps inside subscriber onboarding no longer drop back to legacy full-page routes
- `Completed foundation`: the remaining owner-override audit, staff-badge preview, and setup-preview handoffs now also stay inside HTMX-driven shells, so business support can move between setup diagnostics and member/invitation remediation without dropping back to older full-page navigation
- `Completed foundation`: the businesses index and business support queue now also keep their main queue subsets, search/reset, and cross-workspace support handoffs inside HTMX-driven shells, so onboarding/support operators can triage subscriber issues without dropping back to older full-page navigation
- `Completed foundation`: the businesses index and support queue now also keep business-list triage, support-queue summaries, and failed-email remediation handoffs inside HTMX shells, reducing one of the last high-value onboarding/support detours in the subscriber workflow
- `Completed`: user edit screens now expose operational account state and support actions for confirm-email override, password reset email, and lock/unlock
- `Completed`: user edit screens now also support sending activation emails for unconfirmed accounts
- `Completed`: shared search/reset/pager behavior now covers legacy and newer operator lists across catalog, CMS, identity, billing, inventory, and orders
- `Completed foundation`: loyalty administration is now exposed directly in WebAdmin through programs, reward tiers, campaigns, accounts, and recent scan-session review screens
- `Completed`: loyalty account queues now also allow admin-side account creation, so support can provision accounts without waiting for self-enrollment
- `Completed foundation`: a dedicated mobile-operations admin page now centralizes mobile bootstrap settings and business-mobile readiness signals instead of leaving them spread across site settings and support queues
- `Completed foundation`: the remaining practical `fetch`/full-page detours across major WebAdmin workspaces have been removed or isolated to very low-traffic legacy surfaces
- `Planned / Near-term`: standardize the last low-traffic partial/modal/alert patterns only if we decide to exhaust the completion audit beyond the current operational bar
- `Completed foundation`: orders, CRM, media, catalog, inventory, identity, business support, and communications now all have HTMX-safe list/detail/editor paths at the current phase-1 operational bar
- `Completed foundation`: categories and brands now also keep list filters, pagers, editor entry, and delete/back flows inside HTMX workspace shells instead of mixing queue triage with older full-page detours
- `Completed foundation`: products, add-on groups, and shipping methods now also keep list filters, pagers, editor entry, and editor back paths inside HTMX workspace shells instead of mixing queue triage with older full-page detours
- `Completed foundation`: add-on-group attachment screens for variants, products, categories, and brands now also keep search, save, pager, and back paths inside HTMX shell workflows instead of dropping back to older full-page list cycles
- `Planned / Near-term`: deepen business onboarding from CRUD/invitation into activation, approval, and support workflows

### Communication management

- `In Progress`: Communication Management now includes delivery-state visibility, delivery-history context, repeated-failure triage, safe test reruns across email/SMS/WhatsApp, canonical phone verification over SMS/WhatsApp, and controlled resend/retry for supported live flows
- `Planned / Near-term`: add per-business communication settings
- `Planned / Near-term`: introduce localization-aware notification/email template management
- `Planned / Near-term`: move password-reset, invitation, and future activation emails from direct SMTP composition into Communication Core templates and delivery logging
- `Completed foundation`: a dedicated `Business Communications` workspace now exists in WebAdmin for operator visibility into phase-1 transport readiness and business-level sender/support-email setup gaps
- `Completed foundation`: the `Business Communications` workspace now includes a pageable business queue for missing support-email and sender-identity setup, with direct links into business setup and global transport settings
- `Completed foundation`: the same workspace now documents the currently live hard-coded transactional email flows so operators can distinguish implemented email behavior from future Communication Core template/log capabilities
- `Completed foundation`: the `Business Communications` workspace now includes queue filters for missing support email, missing sender identity, and policy-enabled subsets, so communication debt can be worked as an operator queue rather than a static report
- `Completed foundation`: site settings now also expose live transactional email policy controls for subject-prefixing and test-inbox rerouting, and the communication workspace surfaces those controls so phase-1 email behavior is operator-visible instead of config-only
- `Completed foundation`: site settings now also expose editable transactional email subject/body templates for invitation, activation, and password-reset flows, and those phase-1 flows now consume the configured templates with safe placeholder rendering instead of remaining fully hard-coded
- `Completed foundation`: the same communication workspace now also provides an operator-safe test-email action that only sends to the configured test inbox, so SMTP and reroute behavior can be validated from the UI without contacting real customers
- `Completed foundation`: phase-1 SMTP email delivery now creates `EmailDispatchAudit` records, and the `Business Communications` workspace surfaces recent delivery attempts/failures for operational visibility
- `Completed foundation`: the `Business Communications` workspace now also has a full email audit-log screen with search/status filters, so SMTP delivery attempts and failures are no longer limited to a dashboard preview
- `Completed foundation`: each business in the communication queue now has a dedicated communication-profile screen that combines sender/support defaults, policy flags, global dependency readiness, and onboarding/support signals for troubleshooting
- `Completed foundation`: phase-1 email audits are now tagged with flow metadata (`BusinessInvitation`, `AccountActivation`, `PasswordReset`) and optional business correlation, so delivery failures are more diagnosable before full Communication Core logging exists
- `Completed foundation`: the `Business Communications` workspace now also exposes a capability-coverage matrix so operators can see which template, retry, and delivery-visibility capabilities are truly live today versus still planned Communication Core work
- `Completed foundation`: the same workspace now also includes a template-inventory matrix and a resend/retry policy matrix, so operators can distinguish hard-coded transactional flows from true template management and can see the safe support action per flow without assuming a generic replay engine exists
- `Completed foundation`: each business communication profile now also includes local template-inventory and resend/retry policy snapshots, so per-business troubleshooting keeps the same Communication Core truth table without forcing operators back to the top-level workspace
- `Completed foundation`: each business communication profile now includes recommended next actions and recent business-scoped email activity, so troubleshooting can move from visibility into operator action without leaving the workspace
- `Completed foundation`: the email audit-log now includes flow-specific operator playbooks, failed-flow quick filters, and business-linked shortcuts, so failed invitation/activation/password-reset emails are no longer only raw diagnostics
- `Completed foundation`: the Business Communications workspace, business profile, and email audit-log now also surface delivery age, completion latency, severity, and follow-up backlog context, so operators can prioritize communication debt with more signal before a fuller Communication Core delivery log exists
- `Completed foundation`: the email-audit queue now also supports controlled generic retry for failed or pending invitation, activation, and password-reset flows after safe live-target resolution, so operators can recover common transactional email failures without pretending a blind replay engine exists
- `Completed foundation`: the communication workspace, business profile, and email-audit queue now also expose prior-attempt counts, repeated-failure chains, and last-success context for the same flow/recipient/business path, so delivery triage can distinguish isolated failures from recurring operational debt before a fuller outbox/delivery-history model exists
- `Completed foundation`: the email-audit queue now also exposes `repeated failures` and `prior success context` as first-class subset filters and summary cards, so operators can work recurring delivery debt as a queue instead of only inferring it from per-row context
- `Completed foundation`: the email-audit queue now also exposes first-class `Retry Ready` and `Retry Blocked` policy states, cooldown/rate-limit reasons, retry-available timing, and 24-hour chain attempt counts, so support staff can replay safe live flows without creating duplicate-email storms or guessing when a retry is appropriate
- `Completed foundation`: the same email-audit queue now also exposes `Heavy Chains` triage plus chain span and chain-status-mix context for the same flow/recipient/business path, so operators can distinguish a single failure from recurring instability before a fuller outbox and provider-event model exists
- `Completed foundation`: exact-chain exploration in the email-audit queue now also exposes chain-level totals, fail/sent/pending mix, first-attempt/last-attempt timing, last-success timing, and follow-up count for one recipient/flow/business path, so support can diagnose a delivery incident as one chain instead of inferring it from scattered rows
- `Completed foundation`: current live SMS and WhatsApp families now also write `ChannelDispatchAudit` records for phone verification and operator transport tests, and the Business Communications workspace/profile now surface recent multi-channel activity plus cross-channel failure counts, so non-email support is no longer a blind spot while broader Communication Core logging is still maturing
- `Completed foundation`: that same multi-channel audit baseline now also has a dedicated `SMS / WhatsApp Audits` workspace with channel/flow/status filters, failed-only and verification/test subsets, and business-scoped HTMX navigation, so operators can explore non-email incidents as a queue instead of only a dashboard preview
- `Completed foundation`: the `SMS / WhatsApp Audits` workspace now also supports exact-chain exploration for one recipient/flow/business path plus follow-up-vs-resolved slices, so verification and diagnostic incidents can be diagnosed as delivery chains instead of isolated rows
- `Completed foundation`: exact-chain exploration now also includes a recent delivery-history preview for the same recipient/flow/business path, so support can read the last few attempt outcomes, provider path, and failure messages without reconstructing the incident manually from the paged queue
- `Completed foundation`: exact-chain exploration now also supports chain-local `Needs Follow-up` and `Resolved History` navigation, so operators can separate unresolved incidents from already-settled attempts without dropping back to broad queue filters
- `Completed foundation`: non-email exact-chain workbenches now also deep-link into verification policy, SMS/WhatsApp settings, message-template surfaces, and rerun-safe admin transport tests while staying inside the same queue context, so SMS and WhatsApp incident handling is no longer just diagnostic but operational
- `Completed foundation`: the same `SMS / WhatsApp Audits` queue now also exposes `Repeated Failures` and `Prior Success Context` subsets plus per-row prior-attempt/prior-failure/last-success context, so support can distinguish recurring verification or transport instability from isolated incidents without manually reconstructing the chain
- `Completed foundation`: non-email audit rows now also expose operator action policy (`ready`, `retry ready`, `cooldown`, `canonical flow`, `unsupported`) with backend-enforced cooldown for admin transport-test reruns, so SMS/WhatsApp actions are governed by real policy instead of UI-only hints
- `Completed foundation`: non-email exact-chain summaries now also surface a recommended next step and escalation rule per flow, so operators can move from diagnosis into the correct verification/test/config action without inventing their own support playbook
- `Completed foundation`: the same non-email queue now also exposes explicit `Escalation Candidates` triage with row-level escalation reasons for repeated verification or transport failures that have not recovered successfully, so support can separate true platform/provider debt from routine operator follow-up
- `Completed foundation`: the same non-email queue now also exposes `Provider Review` triage with per-row 24-hour provider pressure context (`attempts`, `failures`, `pressure state`), so operators can distinguish recipient-specific incidents from provider-lane instability before a fuller provider-event/outbox model exists
- `Completed foundation`: the same non-email queue now also supports provider-lane drill-in with a provider-scoped 24-hour summary and direct provider-lane navigation from each row, so transport issues can be reviewed as one provider/channel/flow lane instead of only as scattered row pressure signals
- `Completed foundation`: provider-lane drill-in now also exposes a recommended next step and escalation rule per SMS/WhatsApp provider lane, so operators can move from provider pressure visibility into the correct transport/config/policy action without inventing their own playbook
- `Completed foundation`: provider-lane review now also includes direct action handoffs into verification policy, channel settings, test-target setup, and safe provider-scoped diagnostic reruns while preserving queue context, so provider pressure can be acted on from the same workbench instead of only being documented
- `Completed foundation`: the same provider-lane review now also exposes recovery state and last-success timing, so operators can distinguish stabilized provider lanes from still-unstable transport pressure without manually reconstructing provider recovery from row history
- `Completed foundation`: Business Communications workspace and business-profile troubleshooting now also expose a cross-channel truth matrix for Email, SMS, and WhatsApp, so operators can see which flows are truly live, which actions are safe today, and where the system intentionally stops short of becoming a generic multi-channel replay bus
- `Completed foundation`: the phase-1 communication flow catalog now also correctly treats SMS and WhatsApp test-send paths as live operator validation flows instead of future-only configuration placeholders, which reduces rollout drift between actual provider capability and operator assumptions
- `Completed foundation`: phone verification now has a persisted platform channel policy with preferred channel and optional SMS/WhatsApp fallback, so Web and Mobile can rely on one backend-owned verification rule instead of inventing client-side channel selection behavior
- `Completed foundation`: the dedicated `Business Support Queue` now links business attention signals with recent failed invitation/activation/password-reset emails, so support operators can triage cross-workflow issues from one place before full automation exists
- `Completed foundation`: the same support queue now refreshes summary, attention businesses, and failed-email signals independently via HTMX fragments, making operational triage faster under active support load

### Payments, refunds, reconciliation, disputes

- `Completed`: generic payment list/edit/refund visibility exists in WebAdmin and reconciliation projections exist
- `Completed foundation`: billing payment rows now expose direct invoice/order/customer follow-up actions, and expense rows expose supplier follow-up, reducing context switching for common finance support work
- `Completed foundation`: financial-account rows now deep-link into journal-entry follow-up using account-aware search, so finance operators can move from account review into ledger activity without manual re-filtering
- `Completed foundation`: financial accounts and journal entries now expose lightweight queue filters for account-type and recent-or-complex review, making the accounting screens less list-heavy without overclaiming deeper ERP workflows
- `Planned / Near-term`: add Stripe-specific operational visibility, provider references, and status history
- `Planned / Near-term`: deepen refund, reconciliation, dispute, and support workflows
- `Planned / Near-term`: add webhook/callback audit trail visibility

### Shipping and returns

- `Completed`: order-bound shipment visibility exists in admin
- `Completed foundation`: generic payment operations now support queue-style triage for pending, failed, refunded, unlinked, and provider-linked payments, so operators can work a support queue before Stripe-specific tooling lands
- `Completed foundation`: the payments workspace now also exposes live queue counts and operator playbooks for pending/failed/refunded/unlinked/provider-linked cases, so phase-1 finance support has an explicit action surface instead of only filters
- `Completed foundation`: the cross-order shipment queue now also supports missing-tracking and returned subsets, so carrier follow-up and return review are no longer trapped inside order detail tabs
- `Completed foundation`: the shipment queue now also exposes live operational counts and phase-1 support playbooks for pending, missing-tracking, and returned work, so shipping support can move from queue detection into guided operator follow-up
- `Completed foundation`: payment and shipment workspaces now also surface Stripe/DHL readiness cards with direct deep-links into system settings, so operators can see configuration gaps from the operational queues themselves
- `Completed foundation`: the payments workspace now also supports Stripe-specific queue subsets for Stripe rows, failed-Stripe cases, and missing provider references, with richer failure/timeline context so phase-1 provider triage is no longer buried inside generic payment rows
- `Completed foundation`: the payment editor now also acts as a payment-support workbench with lifecycle context, failure visibility, refund timeline, and Stripe-specific operator playbooks, so payment troubleshooting is no longer trapped in the queue row alone
- `Completed foundation`: payment queue rows, order-payment rows, and the payment editor now all deep-link directly into the existing refund-create workflow with the payment preselected, so finance support can move from payment troubleshooting into refund recording without re-navigating through the order manually
- `Completed foundation`: WebAdmin now also has a dedicated cross-order refund queue with pending/completed/failed/Stripe subsets, live counts, and direct links back into the linked payment and order workflows, so refund support is no longer trapped inside individual order tabs
- `Completed foundation`: the refund queue now also exposes a `Needs Support` subset and summary signal that combines pending, failed, and Stripe-reference-light refund rows, so finance triage no longer depends on manually combining multiple refund subsets
- `Completed foundation`: the shipment queue now also supports DHL-specific and missing-service subsets, with carrier-review flags and DHL environment visibility so phase-1 shipping follow-up is more provider-aware from the main operator queue
- `Planned / Near-term`: add DHL-first shipment workspace, tracking timeline, label info, and exception handling
- `Planned / Near-term`: add return shipment / return request / RMA foundations

### Settings UI and architecture

- `In Progress`: basic site settings UI exists
- `Completed foundation`: site settings UI now exposes grouped controls for security/JWT, mobile bootstrap, and soft-delete retention in addition to existing localization, SEO, and communications fields
- `Completed foundation`: site settings now also manage business-app legal/billing handoff URLs, and mobile operations surfaces whether those URLs are actually configured
- `Planned / Near-term`: restructure settings into categories such as General, Business Profile, Localization, Branding, Payments, Shipping, Communications, Users & Roles, Security, Integrations, Tax & Invoicing, and Advanced
- `In Progress`: business setup workspace now separates business-owned defaults from global phase-1 settings, but true tenant/business settings storage still needs domain and UI expansion
- `Completed foundation`: business-level branding, localization defaults, time zone, and phase-1 communication defaults now persist on the `Business` aggregate and are editable from the setup workspace
- `Completed foundation`: business setup now surfaces communication readiness against global transports, so operators can see whether business email/SMS/WhatsApp preferences are actually executable with current platform configuration
- `Completed foundation`: the admin dashboard now includes a communication-operations snapshot covering global transport readiness and business-level sender/support-email gaps, so communication setup debt is visible before full template/log management exists
- `Planned / Near-term`: split business communication defaults further into templates, channel policies, and delivery visibility once Communication Core moves out of direct SMTP composition
- `Planned / Near-term`: make settings UI tenant-aware, permission-aware, and future-safe

### Localization readiness

- `Planned / Near-term`: make WebAdmin localization-ready immediately after initial completion
- `Planned / Near-term`: introduce shared localization strategy for admin, templates, and settings labels
- `Planned / Near-term`: add language/default-locale settings at system, business, and user levels
- `Planned / Near-term`: reduce hard-coded WebAdmin text and prepare translation-friendly resource structure
- `Completed foundation`: WebAdmin localization now uses a `resx-first` shared-resource path with `IStringLocalizer`, request-localization culture switching, dictionary fallback for not-yet-migrated keys, and an MVC display-metadata provider so `Display(Name=...)` keys can be resolved through shared resources instead of staying hard-coded
- `Completed foundation`: localization naming/path cleanup is now in place too: the operational localization services/providers live under a dedicated `WebAdmin/Localization` slice, while `SharedResource*.resx` and `ValidationResource*.resx` intentionally stay at the root resource level so the standard ASP.NET Core resource path remains obvious until a real feature-based split is justified
- `Completed foundation`: the initial admin language contract is now centralized as well, so the platform-wide `de-DE` / `en-US` defaults are no longer scattered across controllers, settings cache, and view models
- `Completed foundation`: the legacy in-code admin text fallback map has now been removed because its active key inventory was fully covered by `SharedResource*.resx`, so WebAdmin shared UI text is now genuinely `resx`-backed instead of only `resx-first`
- `Completed foundation`: the main site-settings form and selected billing metadata have now moved onto named shared-resource keys, so localization is no longer limited to controller alerts and hand-authored view text
- `Completed foundation`: CRM customer metadata and identity password-label metadata have now also moved onto named shared-resource keys, reducing the remaining dependence on literal English display names in high-traffic editor flows
- `Completed foundation`: `Darwin.Application` now has its own shared `ValidationResource*.resx` family, and high-traffic validators such as site settings, journal-entry balancing, and core CRM interaction/consent rules now resolve localized validation text from application-owned resources instead of relying on hard-coded English strings
- `Completed foundation`: `WebApi` now also registers localization services so application-owned localized validators can resolve in both admin and API hosts without leaking `WebAdmin` resources across layers
- `Completed foundation`: catalog, CMS, SEO, and auth validator messages such as translation requirements, menu URL checks, redirect-path rules, and refresh-revoke guard rails now also use the shared application validation resource path, and the remaining manual validator call-sites have been aligned with DI/localizer-based construction
- `Planned / Near-term`: continue migrating application-layer validator and domain-error messages onto the shared validation resource path until hard-coded English feedback is no longer a meaningful operational gap

### Tax, VAT, and invoice improvement

- `Completed foundation`: site settings now expose global phase-1 VAT and invoice-issuer defaults, and billing surfaces now show VAT/invoice readiness to operators
- `Planned / Near-term`: improve tax/VAT-aware order and invoice snapshots
- `Completed foundation`: VAT ID support and B2B/B2C differentiation now exist on CRM customers and are surfaced in customer and invoice operations, while deeper country-aware tax automation still remains near-term
- `Planned / Near-term`: define reverse-charge readiness and country-aware taxation rules
- `Planned / Near-term`: improve invoice immutability, archive readiness, and structured export readiness

### Auditability and observability

- `Planned / Near-term`: add stronger audit logging around onboarding, auth support, settings, payments, and shipping actions
- `Planned / Near-term`: improve observability around provider callbacks, communication delivery, and admin-side operational failures

## 4. Medium Priority

### Communication expansion

- `Future / Later phase`: add channels beyond email, including SMS, WhatsApp, Push, and In-app, on top of the same Communication Core
- `Future / Later phase`: add richer targeting, queue/outbox strategies, and failure-routing workflows

### Returns and post-order operations

- `Planned / Near-term`: introduce return requests / RMA baseline
- `Future / Later phase`: add full end-to-end returns workflow with inventory, refund, and support coupling

### Tax and compliance expansion

- `Future / Later phase`: add deeper EU tax handling such as OSS/IOSS-specific behavior where required
- `Future / Later phase`: add compliance metadata foundations for products, packaging, and traceability-related needs
- `Future / Later phase`: add e-invoice and archive workflows beyond initial readiness

### Merchant operations depth

- `Future / Later phase`: add richer merchant operational settings and automation features after go-live-critical support is stable

## 5. Later Phase / Future Expansion

### Additional providers

- `Future / Later phase`: additional payment providers beyond Stripe
- `Future / Later phase`: additional market-specific payment methods beyond the phase-1 Stripe scope
- `Future / Later phase`: additional shipping carriers and postal companies beyond the phase-1 DHL scope

### Broader market expansion

- `Future / Later phase`: advanced EU expansion requirements beyond initial go-live
- `Future / Later phase`: deeper multi-provider abstractions once real provider variation justifies them

### Front-office growth

- `In Progress`: start `Darwin.Web` execution against `Darwin.WebApi` using narrow, contract-first slices
- `Decision made`: `Darwin.Web` must consume only `Darwin.WebApi`; no `Darwin.WebAdmin` DTO or MVC view model may leak into the public/member web app
- `Decision made`: the initial visual direction may be inspired by the Cartzilla grocery storefront, but implementation must stay theme-isolated through tokens, slots, and reusable page web parts
- `Decision made`: the initial home page may remain intentionally minimal while shell/navigation/composition foundations are established

### Darwin.Web workstreams

#### Experience shell and theme foundation

- `Unblocked now`: `Darwin.Web` can start with a shell, routing model, theme tokens, layout slots, and web-part composition without waiting on new backend work
- `Unblocked now`: the first pass can keep Home intentionally minimal and use the shell/navigation to expose major site areas while the deeper pages are built incrementally
- `Completed foundation`: `Darwin.Web` now has a theme-isolated storefront shell, Cartzilla-inspired token set, reusable page-part composer, placeholder Home, and route scaffolding for catalog/account/loyalty/orders/invoices
- `Completed foundation`: shell navigation now treats the public CMS `main-navigation` menu as the primary source, keeps fallback links only for emergency degradation, and surfaces a visible warning banner whenever fallback mode is active
- `Depends on WebApi/backend`: runtime theme selection, config-delivered branding, localized navigation metadata, and CMS-driven page-section composition still need explicit API/config contracts if we want them to be server-driven instead of app-configured
- `Future / Later phase`: multi-theme runtime switching, tenant-specific theming, and full CMS-driven page-layout orchestration

#### Public CMS / storefront

- `Unblocked now`: public CMS page listing, page-by-slug, and menu delivery already exist in `Darwin.WebApi`
- `Unblocked now`: WebAdmin now has real page, menu, media, and content-ops support, so storefront content does not depend on speculative operator tooling anymore
- `In Progress`: `Darwin.Web` shell navigation is now wired to the public CMS menu contract and CMS slug pages can render sanitized HTML when content exists
- `In Progress`: Home now also exposes category-driven storefront lanes backed by public categories plus category-filtered product contracts, so top-level browse entry is moving from generic shortcuts to real data-backed discovery
- `In Progress`: CMS-driven shell navigation now only accepts sanitized app-local paths or safe `http/https` URLs, so `main-navigation` content cannot push unsafe raw hrefs into the storefront chrome
- `In Progress`: CMS dependency is now assumed ready for `main-navigation`; future web work should treat fallback navigation as emergency behavior only
- `In Progress`: home composition now reuses explicit web parts and can surface live CMS and catalog spotlight data without pretending a richer server-driven section contract already exists
- `In Progress`: home composition now also uses a stat-grid web part plus part-owned hero aside copy fed by live CMS/catalog/runtime counts, so the page-composer contract is getting richer without coupling itself to one home-only layout
- `In Progress`: home composition now also includes a journey/link-list web part so CMS, catalog, and account entry flows stay visible as one front-office system instead of scattered route shortcuts
- `In Progress`: home composition now also surfaces CMS/catalog/category contract health directly in the hero/metric web parts, and CMS index empty states keep storefront follow-up actions visible instead of becoming dead ends
- `In Progress`: home composition now also includes a reusable status-list web part so contract-backed CMS/catalog/account lanes stay actionable and theme-independent instead of depending only on card grids or home-specific markup
- `In Progress`: home composition now also includes a reusable stage-flow web part so CMS, catalog, and member follow-up read like one staged storefront journey instead of disconnected route teasers
- `In Progress`: home composition now also includes a reusable pair-panel web part so CMS and catalog can stay visible as coordinated storefront surfaces instead of only independent spotlight blocks
- `In Progress`: home composition now also includes a reusable agenda-columns web part so content, commerce, and member follow-up can stay visible as parallel storefront streams instead of only linear or pairwise sections
- `In Progress`: CMS detail pages now preserve visible degraded-state behavior for network/http failures and reserve real 404 handling for confirmed `not-found` responses
- `In Progress`: CMS detail now also derives in-page section navigation anchors plus reading/structure metrics from published HTML, so long-form public content is more navigable without inventing a separate server-driven page-layout contract
- `In Progress`: CMS detail now also derives previous/next adjacency plus Home/Catalog/Account follow-up from the current published page set, so public content routes do not behave like isolated leaf pages
- `In Progress`: CMS detail now also exposes breadcrumb and published-set position context, and the route now prefers the broader published page set for adjacency/orientation instead of a tiny sidebar-only seed
- `In Progress`: public CMS list/detail delivery now follows the active request culture instead of relying on backend default-language behavior, which keeps Home and CMS composition aligned with the front-office localization baseline
- `In Progress`: public Home, CMS index/detail, and catalog routes now emit centralized canonical/Open Graph/Twitter metadata through `src/lib/seo.ts` and the configured `DARWIN_WEB_SITE_URL` instead of route-local ad hoc shaping
- `In Progress`: `robots.txt` and `sitemap.xml` now enumerate the public storefront surface from live CMS/catalog contracts instead of a hand-maintained URL list, while staying resilient if those feeds degrade
- `In Progress`: locale-prefixed public URLs now run through middleware rewrite plus request-culture headers for Home/CMS/catalog, so public multilingual URLs no longer depend only on cookies or `?culture=` query parameters
- `In Progress`: shell navigation, Home web parts, CMS browsing, and catalog browsing now emit locale-aware public links directly through a shared locale-routing helper instead of relying on redirect-only correction after navigation
- `In Progress`: catalog, CMS, and loyalty public search/discovery routes now normalize page/text/numeric search params through shared helpers and use locale-aware form actions, so server-rendered filters do not drift across public surfaces
- `In Progress`: sitemap now includes locale-prefixed inventory for the public index-level routes whose path shape is already culture-safe (`/`, `/cms`, `/catalog`), while detail-page inventory remains on default-culture canonicals until slug-linking contracts exist
- `In Progress`: member sign-in return targets, account self-service flow return paths, loyalty/cart/payment follow-up paths, and persisted storefront display links now sanitize app-local paths before redirecting or storing them, so front-office flow handoffs do not trust arbitrary user-supplied redirect targets
- `Completed foundation`: backend/CMS seed data now guarantees a public menu named exactly `main-navigation` plus published CMS pages such as `ueber-uns` and `faq`, so `Darwin.Web` can validate navigation and `/cms/[slug]` against true public content instead of fallback-only behavior
- `In Progress`: `Darwin.Web` now also renders a public CMS index route against the published page-list endpoint so storefront content consumption is not limited to hard-coded slug assumptions
- `In Progress`: the CMS index now also supports a visible-result search lens over the already loaded page set, while staying explicit and non-canonical until a real public CMS search contract exists
- `In Progress`: the CMS index now also surfaces visible-vs-loaded-vs-total result summaries, so the public content route stays aware of the current result window instead of behaving like a flat card collection
- `In Progress`: the CMS index now also groups the current visible page set by title initials with quick-jump anchors, so public content browsing gains orientation without pretending a richer CMS taxonomy/search contract already exists
- `In Progress`: the CMS index now also derives a spotlight-plus-follow-up reading rail from the current visible result window, so public content browsing can guide reading order without inventing a separate CMS curation contract
- `In Progress`: the CMS index now also exposes explicit cross-surface handoff cards into Home, Catalog, and Account, so public content routes stay connected to the broader storefront system instead of looping only within content
- `In Progress`: CMS detail anchor ids now normalize diacritics before slugging section headings, so long-form German content keeps stable in-page navigation instead of falling back to weak or duplicate section ids
- `In Progress`: CMS index and CMS detail now also surface live catalog-category and live product follow-up windows, so public content routes can hand off directly into real commerce browse/detail paths instead of only pointing back to the generic catalog route
- `In Progress`: those CMS product follow-up windows now also rank by the strongest visible savings signal first, so content routes hand off into a clearer best-offer buying opportunity instead of arbitrary catalog order
- `In Progress`: CMS index and CMS detail now also surface live cart/checkout continuity from the canonical public cart contract, so published content can hand off directly into an already active purchase flow instead of only into browse routes
- `Decision made`: future navigation metadata such as badge, icon, and featured grouping stays a design note only until `Darwin.Web` explicitly needs it; the current public menu contract remains label/url/sort-order only
- `Depends on WebApi/backend`: explicit SEO-focused endpoints, richer homepage merchandising payloads, and formal page-composition contracts for server-driven web parts are still API/backend work if we want them beyond static app composition
- `Depends on localization`: culture-aware CMS delivery exists through public endpoints, but broader multilingual content governance and fallback policy are still platform work and must stay explicit
- `Later-phase only`: editorial-rich home composition, campaign landing pages, recipe/blog-style content modules, public business discovery maps, and personalization-heavy storefront experiences

#### Catalog browsing

- `Unblocked now`: public category list, product list, product detail, and category-slug filtering already exist in `Darwin.WebApi`
- `Unblocked now`: catalog, media, product, category, and brand management now have real WebAdmin operator support, so catalog browsing can be built against maintained data instead of raw setup screens
- `In Progress`: `Darwin.Web` now renders a public catalog page with category filtering, product cards, paging, degraded-mode visibility, and product-detail routing against the public catalog contracts
- `In Progress`: catalog listing/detail now also pass the active request culture into the canonical public catalog endpoints instead of silently falling back to one backend default culture
- `In Progress`: catalog listing/detail now add merchandising context that is actually supported by the current contracts, including selected-category context, compare-at savings visibility, and category-linked navigation between list/detail without inventing unsupported search/facet APIs
- `In Progress`: `/catalog` now also exposes a visible-result search/sort lens for the products already loaded on the current page, while keeping that behavior explicit and `noindex` so the storefront does not pretend it already has global catalog search/facets/sort
- `In Progress`: `/catalog` now also surfaces visible-vs-loaded-vs-total result summaries plus first/last page jumps, so catalog window navigation is more complete while the search/facet contract gap remains explicit
- `In Progress`: `/catalog` now also surfaces an offer-focus window plus a buying-guide summary from the live visible product set, so merchandising signals stay explicit even before true backend search/facets land
- `In Progress`: `/catalog` and `/catalog/[slug]` now also surface live cart/checkout continuity from the canonical public cart contract, so browse and product evaluation can hand off directly into an already active purchase flow instead of behaving like isolated discovery routes
- `In Progress`: product detail now also shows category-aware related products by reusing the primary-category mapping plus the existing category listing contract, so storefront follow-up does not need a separate recommendation API for the current phase
- `In Progress`: product detail now also exposes breadcrumb, product-reference snapshot, and explicit storefront handoff actions, so the conversion route stays oriented inside the broader front-office system instead of behaving like an isolated leaf page
- `In Progress`: product detail now also surfaces offer-position and buying-context panels derived from the current product plus related-offer signals, so conversion/detail routes communicate active offer strength instead of behaving like static specification pages
- `In Progress`: product detail now also surfaces degraded related-product follow-up state explicitly when the category-based follow-up fetch fails, instead of silently flattening adjacent catalog discovery
- `In Progress`: catalog index and product detail now also surface live published CMS follow-up windows, so public commerce browsing stays connected to storefront content without waiting for new backend contracts
- `Completed foundation`: backend/catalog seed data now guarantees representative published products with valid localized slugs and attached primary media, while product-detail media keeps real `alt` and `title` values for storefront testing
- `Decision made`: storefront missing-image behavior must stay explicit; `PrimaryImageUrl` can still be null for some public products, so `Darwin.Web` must render a stable placeholder instead of hiding cards or assuming every public row has media
- `Decision made`: category browsing must not assume category-image support yet, because the current public category contract does not expose category media
- `Depends on WebApi/backend`: richer catalog filtering, sorting, search, brand/facet projections, availability signals, and merchandising-oriented product cards still need API expansion if the web UX wants more than the current baseline contracts
- `Depends on localization`: product/category localization must continue to honor culture/fallback behavior explicitly instead of assuming one platform default language
- `Later-phase only`: advanced discovery, personalized recommendations, cross-sell bundles, reviews-rich merchandising, and search-driven landing experiences

#### Auth / account self-service

- `Unblocked now`: member auth already exposes register, login, refresh, logout, change-password, email-confirmation request/confirm, password-reset request/reset, and member profile/preferences/address endpoints
- `Unblocked now`: Communication Core phase-1 support is operational enough for activation and password-reset follow-up, and WebAdmin now has recipient-aware resend/troubleshooting support for invitation, activation, and password-reset flows
- `In Progress`: `Darwin.Web` now exposes public self-service routes for registration, activation email request/confirm, and password reset request/complete without bypassing the canonical member auth contracts
- `Completed`: inline resend-activation recovery now also exists across account hub, sign-in, register, and password-recovery entry flows, preserving localized return-path context while reusing the canonical activation-request endpoint
- `Completed`: authenticated self-service password change now exists on a dedicated member-portal security route, using the canonical authenticated password-change endpoint instead of sending active-session users back through public recovery
- `In Progress`: public self-service and sign-in actions now canonicalize email input and the public auth forms now carry stronger required/autocomplete/password guardrails, so avoidable auth-flow mismatches are reduced before the canonical API call
- `In Progress`: register, activation, password-recovery, and sign-in now preserve a sanitized app-local `returnPath`, so storefront and loyalty entry points can carry the intended post-auth destination through the public self-service chain without trusting raw redirect input
- `In Progress`: the account hub now also reuses the same public-auth continuation wrapper as sign-in/register/activation/password, so public account continuity no longer depends on a route-local continuation item list that duplicates the auth entry routes
- `In Progress`: public auth screens plus loyalty/confirmation entry links now also build localized auth hrefs through one shared sanitized `returnPath` helper, so the public/member crossover no longer hand-builds encoded auth query strings differently per route
- `In Progress`: auth, sign-in, and member-profile action inputs now also pass through shared trimmed/bounded FormData readers for email, returnPath, tokens, passwords, phone codes, and profile identity fields, so those server-action entry points are less dependent on route-local `String(...).trim()` coercion
- `In Progress`: localized app-query hrefs for category follow-up, loyalty timeline paging, and order-confirmation handoff now also run through shared routing helpers, so public/member route transitions are less dependent on ad hoc query-string interpolation
- `In Progress`: cart, checkout, and member-portal server actions now also build flash/status/error redirect targets through shared app-query helpers, so action-level route transitions are less dependent on duplicated query-string concatenation across feature files
- `In Progress`: account and member-session server actions now also build auth-flow redirects through the same shared app-query helper path, so public auth redirects no longer keep a separate local query-builder implementation
- `In Progress`: member order/invoice payment-handoff failure redirects now also use the shared app-query param helper, so protected commerce handoff errors no longer keep a leftover local separator/encoding branch in the member action layer
- `In Progress`: product-detail category follow-up now also passes its catalog continuation path through the shared app-query helper, so the last inline catalog query string on that surface has been folded back into the common routing path
- `In Progress`: catalog route metadata plus catalog/CMS/discovery pagination and filter links now also use the central app-query path helper, so page- and component-level query construction follows the same routing abstraction as the action layer
- `In Progress`: checkout confirmation finalize now also builds its hosted-checkout handoff redirect through the central app-query helper, so that completion path no longer keeps a route-local query-string builder branch
- `In Progress`: checkout confirmation finalize now also bounds callback-carried order/provider/failure text and clears stale mismatched handoff cookies, so PSP return handling is less trusting of raw callback query payloads
- `In Progress`: shared query serialization now also covers public catalog/CMS fetch helpers, cart fetch, checkout-draft search persistence, checkout confirmation fetch, and member loyalty paging helpers, so infrastructure-side query construction is less dependent on repeated local `URLSearchParams` serializers
- `In Progress`: the shell culture switcher now also clones query state through the shared query utility path, so the remaining component-layer search-param copy branch is no longer local to that widget
- `In Progress`: member order-history and invoice-history routes now also parse `page` through the shared positive-integer search-param helper, so protected pagination input no longer keeps a raw number-cast branch outside the common route-hardening path
- `In Progress`: browser-owned cart display, storefront payment handoff, and member session cookies now also validate parsed JSON shape before reuse, so cookie corruption is less likely to leak into storefront/member flows after a successful parse
- `In Progress`: browser-owned prepared loyalty scan-session cookies now also validate parsed JSON shape before reuse, so loyalty scan preparation no longer accepts malformed cookie payloads just because they parse successfully
- `In Progress`: member-session expiry checks and prepared loyalty scan-session expiry/validity checks now also use shared UTC timestamp helpers, so timestamp parsing no longer drifts across cookie/session hardening paths
- `In Progress`: order confirmation and member order detail now also share a stricter address-JSON parser, and loyalty reward-progress rendering now clamps parsed percent input before display, so two more JSON/numeric edge-case paths are less optimistic about malformed payloads
- `In Progress`: checkout integer parsing is now stricter for quantity, page, and shipping-minor-unit inputs, so malformed mixed strings no longer pass through permissive integer/number casts in storefront routing and order placement
- `In Progress`: loyalty business detail now normalizes next-reward progress through an explicit percent parser/clamp helper instead of repeated raw `Number(...)` casts in render, which closes the remaining percent-display edge case on that surface
- `In Progress`: bounded numeric query parsing is now also strict about decimal shape before conversion, so latitude/longitude/radius-style route inputs no longer rely on permissive `Number(...)` coercion
- `In Progress`: `Darwin.Web` now also has a provisional browser-session layer with web-owned cookies plus sign-in/sign-out entry points for protected member routes
- `In Progress`: the provisional browser-session layer now refreshes access tokens near expiry and retries protected member fetches once before forcing a new sign-in
- `In Progress`: account dashboard, profile, preferences, and address-book screens now share explicit member-portal navigation plus stronger client-side form guardrails, so the authenticated account area behaves like one system instead of a set of isolated editor pages
- `In Progress`: editable profile, preferences, and reusable address-book flows now run through the canonical member profile endpoints instead of staying as account placeholders
- `In Progress`: the authenticated preferences route now also reads canonical profile channel readiness, so email/SMS/WhatsApp preference toggles are shown together with real profile-channel prerequisites instead of behaving like detached booleans
- `In Progress`: the authenticated security route now also surfaces current profile/session security context, including phone-verification state, session-expiry visibility, and direct handoff back into profile/dashboard follow-up instead of behaving like a password-form-only leaf route
- `In Progress`: member dashboard and address-book routes now also hand off directly into checkout with saved-address prefills, so protected member data can feed the storefront conversion path instead of staying isolated inside the portal
- `In Progress`: checkout now also surfaces member profile, channel, and address-book readiness directly inside the route, so authenticated storefront checkout keeps profile/preferences/address context visible instead of treating member prefill as hidden background state
- `In Progress`: storefront confirmation now also surfaces signed-in member continuation across orders, invoices, and loyalty, so post-checkout handoff into the protected portal stays explicit instead of stopping at generic CTA buttons
- `In Progress`: storefront confirmation now also gives guest shoppers an explicit account-continuation panel with sign-in/register/activation/password recovery handoff bound to the protected order-follow-up return path instead of relying on two isolated CTA buttons
- `In Progress`: storefront confirmation now also surfaces explicit post-purchase care and next-customer-window panels, so payment attention, order reference handling, and repeat-engagement follow-up stay visible after checkout instead of the route behaving like a passive receipt
- `In Progress`: the authenticated address-book route now also surfaces explicit checkout-readiness state for reusable/default shipping/default billing coverage, so storefront handoff readiness is visible from the address subsystem instead of being inferred only from individual address cards
- `In Progress`: member dashboard now also surfaces recent order and invoice snapshots from the canonical member commerce endpoints, so account overview acts as a real portal landing route instead of only a navigation shell
- `In Progress`: member dashboard now also surfaces loyalty overview totals plus joined-business snapshots from the canonical member loyalty endpoints, so `/account` acts as a real landing route across profile, commerce, and loyalty instead of stopping at account-only context
- `In Progress`: member dashboard now also derives next-reward focus cards from the canonical loyalty overview accounts, so `/account` can hand members directly into the most relevant loyalty business follow-up instead of forcing a blind jump into the broader loyalty route
- `In Progress`: member dashboard now also derives an action-center from profile, address-book, invoice-balance, and loyalty snapshots, so `/account` can hand members directly into the most urgent next-step route instead of remaining a passive summary screen
- `In Progress`: member dashboard now also surfaces live storefront cart continuity from the canonical public cart contract, so signed-in members can resume cart/checkout directly from the member portal instead of treating commerce continuation as a public-only path
- `In Progress`: member dashboard now also surfaces a dedicated security window with phone-verification state, session-lifetime visibility, and direct handoff into the authenticated security/profile routes, so `/account` exposes security readiness from the main member landing route instead of hiding it behind `/account/security`
- `In Progress`: member dashboard now also surfaces a communication window derived from canonical profile plus preferences, so email/SMS/WhatsApp readiness is visible from `/account` instead of being hidden behind preferences/profile routes
- `In Progress`: member dashboard now also surfaces a storefront continuation window fed by live public CMS pages plus public categories, so the protected member landing route stays connected to public browse/content follow-up instead of acting like a portal-only island
- `In Progress`: member dashboard storefront merchandising now also ranks visible product follow-up by strongest savings signal, so signed-in account entry can surface clearer next-buy opportunities instead of a raw public-product list
- `In Progress`: that member-dashboard storefront offer board now also shifts away from products already linked to the active cart when browser storefront-shopping state exists, so signed-in account entry can pitch the next buying move beyond the current basket instead of echoing it
- `In Progress`: profile, preferences, and addresses now also reuse the same member cross-surface rail as the protected overview routes, so detail/editor follow-up behavior stays on one shared continuity component instead of per-route CTA clusters
- `In Progress`: phone verification request/confirm now runs inside the profile surface and consumes the canonical SMS/WhatsApp verification endpoints plus the shared profile confirmation flag
- `In Progress`: dashboard, orders, invoices, and loyalty overview now also expose breadcrumb-style route orientation plus cross-surface handoff actions, so the authenticated member area stays connected to the wider front-office system instead of behaving like a sealed portal island
- `In Progress`: profile/preferences/addresses plus order/invoice/loyalty-business detail routes now also expose the same breadcrumb-style route orientation and cross-surface handoff actions, so editor/detail screens stay inside the shared front-office navigation model instead of collapsing back to isolated portal pages
- `In Progress`: profile, preferences, security, and addresses now also consume live public CMS pages plus public categories for a shared storefront-continuation window, so authenticated account editor routes stay connected to published content and catalog browse follow-up without needing new backend contracts
- `In Progress`: the authenticated invoices route now also surfaces explicit billing-readiness state for visible outstanding invoices and open balance, so finance follow-up is visible from the history route instead of being inferred only from row-level balances
- `In Progress`: member order/invoice detail routes now also surface explicit readiness panels for payment, shipment, balance, and document follow-up, so protected commerce detail routes communicate actionable next-step state instead of relying only on summary blocks
- `In Progress`: member order/invoice detail routes now also surface storefront continuation windows fed by live public CMS pages plus public categories, so protected commerce detail routes stay connected to public content and catalog follow-up instead of ending inside portal-only branches
- `In Progress`: the authenticated orders route now also surfaces explicit fulfillment-readiness state for visible orders needing active follow-up, so order-history follow-up is visible from the history route instead of being inferred only from row-level statuses
- `In Progress`: the authenticated orders and invoices routes now also surface storefront continuation windows fed by live public CMS pages plus public categories, so protected history routes stay connected to public content and catalog follow-up instead of behaving like portal-only archives
- `In Progress`: profile/preferences/addresses now also keep explicit route-summary plus unavailable follow-up guidance, so the account-edit subsystem remains navigable when canonical member data is partial or absent
- `In Progress`: the profile route now also surfaces explicit readiness for identity, phone verification, and locale/billing defaults, so member self-service can see commerce/communication completeness without inferring it only from the edit form
- `In Progress`: CMS unavailable/no-pages and catalog no-results fallbacks now also keep account follow-up visible, so public content/discovery dead ends still connect back into the wider front-office system
- `In Progress`: CMS index/detail and catalog index/detail now also assemble their public follow-up rails through feature-level wrappers on top of the shared public continuation component, so continuity rules stay reusable at the CMS/catalog module boundary instead of route-local item lists
- `In Progress`: Home now also exposes a recovery/follow-up rail derived from live CMS/catalog health, and product-detail degraded states now also keep account follow-up visible, so public entry and conversion routes stay connected to the broader front-office system when one contract family is weak
- `In Progress`: Home now also exposes a reusable route-map web part that hands off directly into live CMS detail, live product detail, and account/loyalty follow-up routes, so public entry shows concrete next routes instead of only surface-level section teasers
- `In Progress`: Home is now also session-aware for signed-in members and surfaces direct re-entry routes for account, orders, and loyalty, so storefront entry does not treat active members like anonymous visitors
- `In Progress`: Home is now also cart-aware and surfaces direct cart/checkout recovery from browser-owned storefront snapshots, so storefront entry can resume active shopping flows instead of only restarting browse
- `In Progress`: Home is now also live-cart-aware and surfaces current cart totals plus checkout continuity from the canonical public cart contract when a storefront cart already exists
- `In Progress`: Home now also enriches signed-in member resume with recent orders plus reward-focus data from the canonical member contracts, so storefront entry can resume real protected-context follow-up instead of only linking back to the generic account landing route
- `In Progress`: Home now also surfaces invoice follow-up inside the signed-in member resume, so storefront entry can hand off directly into outstanding billing context instead of forcing a second hop through the account dashboard
- `In Progress`: Home now also surfaces member checkout readiness inside the signed-in member resume, so storefront entry can hand off directly into prepared checkout or address-book setup from the same protected context
- `In Progress`: Home now also includes a live priority lane that ranks checkout, billing, loyalty, order, CMS, and catalog follow-up from current public/member signals, so storefront entry can surface the most actionable next move instead of only exposing broad route families
- `In Progress`: Home now also picks the strongest visible product opportunity instead of the first catalog card, so storefront entry surfaces a clearer best-offer moment from live browse data
- `In Progress`: Home now also surfaces a live offer board that ranks multiple visible catalog opportunities by savings strength, so storefront entry can show several concrete next-buy options instead of one product suggestion alone
- `In Progress`: that Home offer board now also shifts away from products already linked to the active cart when browser storefront-shopping state exists, so storefront entry can suggest the next buying move beyond the current basket instead of echoing the same cart
- `In Progress`: Home now also includes a live commerce-opportunity window driven by cart, spotlight-product, and category-lane signals, so storefront entry can emphasize the strongest immediate buying move instead of only generic browse entry points
- `In Progress`: CMS detail and product detail now also share a reusable public continuation-rail pattern, and product-detail continuity now includes a CMS return path, so public content and commerce routes do not drift into inconsistent follow-up UX
- `In Progress`: CMS no-pages, catalog no-results, and the public account hub now also follow the shared continuation-rail and locale-aware routing pattern, so entry and empty-state routes do not regress into isolated or culture-blind navigation
- `In Progress`: sign-in, registration, activation, and password-recovery routes now also follow the shared continuation-rail pattern and internal Link-based navigation model, so public auth/self-service entry does not drift away from the rest of the storefront routing behavior
- `In Progress`: the public account hub plus sign-in/register/activation/password routes now also consume live published CMS pages plus live public categories through the shared public-auth continuation wrapper, so public self-service entry can hand off into real content and browse surfaces instead of depending on static continuation cards
- `In Progress`: the public account hub plus sign-in/register/activation/password routes now also consume live storefront cart state through that same shared public-auth continuation wrapper, so public self-service entry can resume cart/checkout continuity instead of dropping active commerce context at the auth boundary
- `In Progress`: sign-in/register/activation/password now also surface a shared post-auth destination summary fed by sanitized `returnPath` plus live cart state, so public auth routes keep checkout/cart/member intent explicit instead of acting like context-free forms
- `In Progress`: that public auth post-auth destination summary now also exposes direct CTA handoff to the sanitized return route plus live cart continuation, so self-service routes can actively recover the current storefront/member journey instead of only describing it
- `In Progress`: the public account hub now also surfaces a dedicated storefront-readiness panel and carries the preferred post-auth destination into sign-in/register/activation/password recovery links, so public account entry preserves active cart/checkout intent instead of defaulting every auth jump back to `/account`
- `In Progress`: the public account hub now also surfaces a live storefront action center fed by current cart state plus published CMS/category spotlights, so anonymous account entry can still move through real content and commerce next steps instead of collapsing to auth-only choices
- `In Progress`: the public account hub action center now also surfaces a strongest visible product offer, so anonymous account entry can still create a real next-buy decision instead of only handing off into cart, CMS, or category browse
- `In Progress`: the public `/account` hub now also accepts and preserves a sanitized incoming `returnPath`, so auth walls can hand shoppers into the generic account entry route without losing the intended post-auth destination context
- `In Progress`: member dashboard, orders, invoices, and loyalty overview now also share a reusable member cross-surface rail, so protected overview routes do not regress into duplicated or inconsistent follow-up blocks
- `In Progress`: dashboard/preferences now also expose route-summary follow-up panels, and address/order/invoice empty states now keep actionable storefront/member handoff CTAs instead of passive dead-end placeholders
- `In Progress`: addresses, orders, and invoices empty states now also reuse the shared member cross-surface rail, so protected list routes keep the same continuation model as overview/detail/auth-required surfaces instead of route-local CTA clusters
- `In Progress`: loyalty overview/discovery/public-detail/business-detail now also expose route-summary diagnostics and actionable empty/degraded follow-up CTAs, so loyalty routes remain observable and non-passive when data is partial or absent
- `Decision made`: the current web-owned cookie wrapper is an implementation boundary, not a final architecture verdict; self-service and member portal work should still avoid hard-coding assumptions that block a future BFF/cookie-composition replacement
- `Depends on Communication Core`: web self-service UX must reuse the existing activation/reset backend lifecycle and should not invent a web-only mail pipeline
- `Decision made`: phone verification now follows the current backend SMS/WhatsApp verification contracts and shared profile confirmation flag rather than inventing a web-local verification workflow
- `Depends on WebApi/backend`: deeper token refresh hardening, logout-all/session-device visibility, and future BFF/cookie composition still need explicit platform decisions if we want something stronger than the current provisional web-owned cookie wrapper
- `Depends on business lifecycle/onboarding`: any future business-facing self-service onboarding on the web must honor the same activation, approval, suspension, and soft-gate rules already used by mobile/admin
- `Later-phase only`: social auth, deeper magic-link onboarding, multi-business account switching, richer browser-session/BFF hardening, and device/session-management UX beyond the current self-service slice

#### Loyalty / member portal

- `Unblocked now`: member loyalty overview, business dashboard, reward browsing, join, promotions, and history endpoints already exist in `Darwin.WebApi`
- `Unblocked now`: loyalty programs, tiers, campaigns, account support, redemption troubleshooting, and scan-session diagnostics now exist in WebAdmin, so member loyalty UX has a real operational fallback
- `Depends on loyalty/mobile contracts`: loyalty contracts are shared with mobile-facing usage, so `Darwin.Web` should consume the canonical member endpoints and avoid creating web-specific DTO forks
- `In Progress`: `Darwin.Web` now renders an authenticated loyalty overview through the member portal session layer
- `In Progress`: the authenticated loyalty overview route now also surfaces explicit engagement-readiness state for active joined places and reward-focus follow-up, so loyalty overview acts as a next-step surface instead of only a balances/list screen
- `In Progress`: business-scoped loyalty dashboard, rewards, recent transactions, and cursor-paged timeline views now render through `/loyalty/[businessId]` against the canonical member endpoints
- `In Progress`: loyalty business detail now also reuses the shared member cross-surface rail, so protected loyalty detail follow-up stays aligned with the rest of the member portal instead of a route-local CTA block
- `In Progress`: loyalty overview now also consumes `my/businesses`, so joined business cards render with image/category/city context instead of only flat account summaries
- `In Progress`: business-scoped promotions feed and promotion-interaction tracking now run through the canonical member contracts on `/loyalty/[businessId]`
- `In Progress`: `/loyalty` now also consumes public business discovery plus public category-kinds metadata so loyalty-ready businesses remain browseable even before the member joins
- `In Progress`: public loyalty overview now also consumes live public CMS pages plus public categories for a storefront-continuation window, so signed-out loyalty discovery stays connected to published content and catalog browse follow-up without needing new backend contracts
- `In Progress`: `/loyalty/[businessId]` now falls back to canonical public business detail for anonymous or not-yet-joined members and exposes a direct join action through the canonical member join contract when the member session is healthy
- `In Progress`: public loyalty business detail now also consumes live public CMS pages plus public categories for a storefront-continuation window, so pre-join loyalty routes stay connected to public content and catalog browse follow-up without needing new backend contracts
- `In Progress`: `/loyalty` now also supports query-driven proximity discovery and a server-rendered coordinate preview using the same public loyalty-filtered discovery result set
- `In Progress`: `/loyalty/[businessId]` now also prepares canonical browser-side scan sessions for accrual or redemption, including branch selection and reward selection, while keeping the short-lived scan token in a web-owned cookie instead of the URL
- `In Progress`: `/loyalty/[businessId]` now also renders a real QR image from the canonical short-lived scan token for contract visibility on the web route
- `In Progress`: loyalty business detail now also follows the shared member-portal navigation model, and scan-session cookie/action handling now clears stale or mismatched prepared-token state instead of letting browser-local scan state drift silently
- `Depends on auth/account`: join flows and any scan-preparation UX still depend on the same member session foundation and should stay aligned with mobile-facing contracts
- `Decision made`: `Darwin.Web` should not assume browser camera/scanner or barcode-handling ownership for loyalty. The current scan-preparation/QR slice is only provisional contract consumption, and later web loyalty behavior should be designed around explicit web requirements such as direct point accrual/redemption for store-enabled businesses rather than mobile-like scanner workflows
- `Completed`: loyalty overview and loyalty business detail in `Darwin.Web` now also surface storefront-continuation windows fed by live public CMS pages plus public categories, so protected loyalty routes stay connected to public content and catalog follow-up without requiring new backend contracts
- `Decision made`: public loyalty discovery is storefront-safe and should stay visible even when a member is signed out or has not joined a business yet; member-only balances, promotions, and timelines stay behind the authenticated portal
- `Depends on WebApi/backend`: full viewport map consumption through `public/businesses/map` still needs either a loyalty-active filter or another storefront-safe contract shape before `Darwin.Web` can rely on that endpoint without mixing non-loyalty businesses into the loyalty discovery surface
- `Later-phase only`: explicit web-specific loyalty accrual/redemption flows for store-enabled businesses, interactive map-pan/zoom flows if still needed, push-heavy engagement surfaces, and deeper loyalty gamification outside the current contract set

#### Orders / invoices / payments

- `Unblocked now`: public cart, shipping-rate estimation, checkout intent, order placement, payment-intent handoff, payment completion, and order confirmation already exist in `Darwin.WebApi`
- `Unblocked now`: member order history, order detail, invoice history, invoice detail, retry-payment, and plain-text document endpoints already exist in `Darwin.WebApi`
- `Unblocked now`: Stripe-first and DHL-first operator support now exists in WebAdmin across payments, refunds, webhooks, shipments, and carrier/support triage, so web checkout/payment flows now have real operational backing
- `In Progress`: `Darwin.Web` now has a public cart slice with stable anonymous storefront identity, add-to-cart from product detail, cart item quantity/remove actions, and visible degraded-mode handling around public cart API failures
- `In Progress`: `Darwin.Web` now also has a public checkout slice with server-rendered address capture, live checkout-intent preview, shipping-method selection, order placement, and storefront confirmation/payment-handoff entry points
- `In Progress`: public cart now also consumes coupon apply/clear and shows richer line-level net/VAT/add-on context from the canonical cart snapshot
- `In Progress`: public cart now also shows storefront follow-up products from the canonical public catalog contract plus explicit next-step guidance, so continue-shopping and conversion follow-up do not depend only on one summary card
- `In Progress`: public cart now also reuses saved member addresses when a browser member session exists, so checkout readiness can surface before the shopper leaves the cart route for checkout
- `In Progress`: public cart now also surfaces member profile/preferences/address readiness together, so signed-in shoppers can verify identity, phone/channel readiness, and address coverage before moving into checkout
- `In Progress`: checkout summary now also surfaces coupon state plus shipment-mass/shipping-country context from the live intent instead of keeping shipping/tax presentation minimal
- `In Progress`: checkout now also shows readiness signals plus a live order-review panel for current cart lines, so order placement is no longer gated only by the address form and total summary
- `In Progress`: checkout now also reuses saved member addresses when a browser member session is available, while preserving manual entry and degraded-mode visibility when the member address-book contract is unavailable
- `In Progress`: checkout now also reuses canonical member profile identity for name/phone prefill when a member session exists but no saved address is selected, so the route can still start from member context before the address book is populated
- `In Progress`: checkout now also surfaces recent member invoice attention inside the route, so signed-in shoppers can keep open billing follow-up visible before placing a new order
- `In Progress`: the cart route now also surfaces explicit opportunity and readiness panels, so shoppers can see the strongest adjacent offer plus basket readiness before leaving the route for checkout
- `In Progress`: checkout now also surfaces explicit confidence and attention panels, so order-readiness, billing follow-up, phone verification, and address-book coverage stay visible at the final conversion step
- `In Progress`: order detail and invoice detail now also reuse the shared member cross-surface rail, so protected commerce detail follow-up stays aligned with the overview routes instead of route-specific CTA groups
- `In Progress`: auth-required member entry points plus unavailable order/invoice/loyalty detail states now also reuse the shared member cross-surface rail, so protected degraded-state follow-up stays on the same continuity model as healthy portal routes
- `In Progress`: storefront confirmation now also reconciles hosted-checkout return/cancel flows through the canonical payment-completion endpoint, using a web-owned handoff cookie only as transport glue instead of treating return URLs as the source of truth
- `In Progress`: storefront confirmation now also keeps post-checkout guidance visible, including payment-attention vs paid next-step messaging, explicit account/order-history follow-up, and a stable order-reference panel instead of behaving like a passive receipt-only route
- `In Progress`: auth-required and post-checkout follow-up links now also sanitize app-local return targets centrally, and confirmation status messaging now derives from the authoritative confirmation snapshot instead of trusting query-carried order/payment status text
- `In Progress`: auth-required member entry points now also render route-summary plus cross-surface follow-up panels, so protected routes stay consistent with the wider portal orientation model instead of degrading to a minimal access wall
- `In Progress`: cart, checkout, and confirmation now also share breadcrumb-style route orientation plus explicit cross-surface handoff cards, so the conversion chain stays connected to the wider storefront system instead of behaving like detached transactional pages
- `In Progress`: cart, checkout, confirmation, and their empty/unavailable follow-up states now also reuse a shared commerce continuation rail, so public conversion continuity stays on one shared component instead of route-local CTA clusters
- `In Progress`: cart and checkout now also surface a shared anonymous account-handoff panel carrying a sanitized `returnPath` into sign-in/register/activation/password help, so commerce routes keep account recovery one step away without dropping the current conversion intent
- `In Progress`: public commerce hardening now also normalizes quantity, coupon, country-code, and allowed status-query inputs so cart/checkout/confirmation redirects and form posts stay explicit instead of trusting raw browser values
- `In Progress`: authenticated member order history/detail and invoice history/detail now render through the browser-session layer and can trigger member-scoped payment retry handoff
- `In Progress`: order-history storefront merchandising now also ranks visible product follow-up by strongest savings signal, so post-purchase history can surface clearer next-buy opportunities instead of a raw public-product list
- `In Progress`: invoice-history storefront merchandising now also ranks visible product follow-up by strongest savings signal, so billing history can surface clearer next-buy opportunities instead of a raw public-product list
  - `In Progress`: orders, invoices, and loyalty surfaces now also share the same member-portal navigation model as profile/preferences/addresses, so post-purchase and loyalty follow-up no longer feel like detached routes
- `In Progress`: member order detail now also renders shipment/payment snapshots, linked invoice follow-up, and direct document download from the canonical member order contract
- `In Progress`: member invoice detail now also renders canonical payment summary and direct document download from the member invoice contract
- `In Progress`: protected order/invoice detail storefront merchandising now also ranks visible product follow-up by strongest savings signal, so commerce detail routes can surface clearer next-buy opportunities instead of a raw public-product list
  - `In Progress`: member order/invoice history routes now also expose explicit history-window summaries, and their empty states retain account/catalog follow-up actions instead of collapsing into passive archive placeholders
- `Decision made`: anonymous storefront cart identity is currently owned by `Darwin.Web` through a stable web cookie until a broader browser-session/BFF transport is introduced
- `Decision made`: the current public cart contract does not expose product title/media snapshots for line display, so `Darwin.Web` may keep a lightweight local display snapshot for UX continuity without changing the API source of truth for cart totals and row identity
- `Decision made`: successful public order placement must clear the web-owned anonymous cart cookie and local display snapshot so the storefront does not fall back into stale cart `not-found` states after checkout
- `Depends on billing/compliance`: checkout, invoice, and payment UX must stay aware of B2B/B2C tax profile, VAT completeness, invoice readiness, and archive/e-invoice baseline signals without pretending the deeper compliance engine is already complete
- `Depends on WebApi/backend`: richer public cart display contracts, anonymous-to-member cart merge behavior, payment-state polling/completion visibility, and deeper checkout composition still need front-office integration work and may require additive API composition in practice
- `Later-phase only`: full provider callbacks/verification hardening, alternative PSPs, richer refund/return/RMA self-service, and dispute-heavy finance UX

#### Localization / config-driven behavior

- `Unblocked now`: the platform now has explicit localization ownership guidance, locale/fallback visibility in admin/CRM, and member profile locale/time-zone/currency fields that web flows can respect from day one
- `Unblocked now`: site settings and business setup already expose ownership splits around localization, communication, branding, payment, shipping, and tax defaults, so config-driven web planning can align to real platform governance
- `In Progress`: `Darwin.Web` now treats `de-DE` and `en-US` as the active front-office cultures, persists culture through cookie/query-string switching, and keeps shell fallback copy aligned with the current culture
- `In Progress`: `Darwin.Web` now has a resource-bundle localization foundation under `src/Darwin.Web/src/localization/resources`, and shared shell/catalog/storefront-commerce plus account-edit/member-commerce and loyalty discovery/public-detail copy now run on that path so future languages can be added without reworking component structure
- `In Progress`: `Darwin.Web` now validates member order/invoice document URLs before rendering download anchors, so malformed WebApi document paths degrade into observable unavailable states instead of broken or over-permissive links
- `In Progress`: `Darwin.Web` now routes loyalty discovery map links and linked-invoice handoffs through locale-aware paths, so those surfaces no longer drop users out of the active localized route tree
- `In Progress`: `Darwin.Web` now validates optional public loyalty-business website URLs before rendering external anchors, so malformed public metadata degrades into absence instead of unsafe or broken external links
- `In Progress`: `Darwin.Web` now resolves loyalty public/discovery/member-overview media URLs through the backend-safe helper, so malformed business image paths degrade into placeholders instead of raw `img` sources
- `In Progress`: `Darwin.Web` now resolves catalog, cart, checkout, and product-detail media URLs through the backend-safe helper as well, so public commerce image rendering degrades into placeholders instead of trusting raw payload URLs
- `In Progress`: safe public website links and member document downloads that open in new tabs now also use `noopener noreferrer`, so external/document handoff follows the same tab-isolation baseline as the rest of the hardened link surface
- `In Progress`: `Darwin.Web` now sanitizes CMS page HTML and product full-description HTML before injection, removing obvious high-risk tags and inline script hooks instead of trusting raw backend HTML wholesale
- `In Progress`: that lightweight fragment sanitizer now also strips unquoted inline event handlers and unquoted `javascript:` `href/src` attributes, so malformed dangerous fragments do not bypass the earlier quoted-attribute checks
- `In Progress`: CMS detail now also sanitizes its non-summary HTML fallback before injection, so heading-less pages do not bypass that same fragment-hardening path
- `In Progress`: `Darwin.Web` now sanitizes cart snapshot product links at both cookie-read and render time, so browser-local cart state cannot redirect storefront return links outside the intended app-local route tree
- In Progress: storefront typography now uses local/system font stacks instead of build-time Google font fetches, so web builds stay deterministic in restricted/offline environments and theme typography remains decoupled from external network availability
- `In Progress`: profile editing, preferences, address-book management, loyalty overview/discovery, loyalty map preview, public pre-join loyalty detail, and signed-in loyalty business detail now consume the member resource bundles instead of app-local text blocks
- `In Progress`: route-level metadata for shell, Home, CMS index/detail fallbacks, catalog, checkout, account, orders, invoices, and loyalty now resolves through the same culture-aware resource path instead of static page titles/descriptions
- `In Progress`: local web-owned validation and flash messages for register/activation/password/sign-in, cart/checkout, payment retry, profile/preferences/addresses, and loyalty join/scan/promotion flows now travel as localization keys and resolve inside the UI resource bundles instead of baking English-only fallback text into server actions
- `In Progress`: generic public/member API fallback messages and CMS degraded-mode UI copy now also resolve through resource bundles, so network/not-found/http fallback states no longer depend on English-only client literals
- `In Progress`: CMS detail unavailable state plus catalog index/detail now also expose route-summary diagnostics and actionable no-result/unavailable follow-up CTAs, so degraded public storefront routes stay observable and navigable instead of collapsing into passive warning blocks
- `In Progress`: cart, checkout, and confirmation now also expose route-summary diagnostics plus stronger empty/unavailable follow-up CTAs, so the conversion path stays observable and actionable instead of degrading into passive panels
- `In Progress`: locale-aware money/date formatting now follows the active request culture across catalog, cart, checkout, orders, invoices, and loyalty instead of assuming one hardcoded locale
- `In Progress`: private and mixed account/commerce/member routes now explicitly emit `noindex` metadata while public Home/CMS/catalog routes stay indexable through the centralized SEO helper, so storefront SEO policy is no longer implicit or route-by-route drift-prone
- `In Progress`: public Home/CMS/catalog routes now expose stable locale-prefixed canonical URLs, and index-level public routes emit language alternates where slug mapping is not ambiguous
- `Decision made`: profile locale editing now follows the supported-cultures config instead of accepting an unrestricted locale string inside the first self-service slice
- `Decision made`: until the web introduces crawlable path- or host-based locale URLs, sitemap/canonical discovery should stay on the default-culture public paths and not pretend that cookie/query-based culture switching creates stable alternate-language URLs
- `Decision made`: detail-level CMS/product language alternates must stay limited until WebApi exposes a real localized-slug linking contract; web must not guess slug parity across languages
- `Depends on localization`: the platform does not yet have complete multilingual CMS/template management, so web must be built localization-ready without assuming full translated content operations already exist
- `Depends on compliance/billing`: tax, invoice, and payment presentation should remain configuration-driven because issuer completeness, VAT handling, and archive/e-invoice readiness are still evolving
- `Depends on Communication Core`: user-facing communication preferences and message expectations must align with the current phase-1 email-first capability instead of assuming a finished multi-channel platform
- `Later-phase only`: translated CMS/page content operations, localized SEO metadata management, and additive languages beyond the current de/en baseline
- `Later-phase only`: full tenant-driven theming, advanced per-market payment/shipping matrices, complete multilingual content/template editing, and full compliance-driven presentation branching

## 6. Technical Foundations / Cross-Cutting

### Domain and application

- `Completed`: CRM no longer duplicates loyalty totals; `Customer.LoyaltyPointsTotal` and `LoyaltyPointEntry` are removed
- `Completed`: loyalty remains owned by `LoyaltyAccount` and `LoyaltyPointsTransaction`
- `Completed`: Lead and Opportunity are part of the domain and application layers
- `Completed`: inventory supports warehouses, stock levels, transfers, suppliers, and purchase orders
- `Completed`: billing/accounting supports payments, financial accounts, journal entries, and expenses
- `Completed`: warehouse-aware fulfillment persists fulfillment context on order lines
- `Planned / Near-term`: deepen payment domain lifecycle and provider audit trail
- `In Progress`: deepen shipment/return domain model and delivery exception lifecycle; phase-1 admin queues now include configurable handoff-delay and tracking-grace thresholds from Site Settings, and shipping-method admin screens now expose operational subsets for missing rates, global coverage, and multi-rate review
- `Planned / Near-term`: model Communication Core as a platform capability rather than feature-local helper code
- `In Progress`: expand Communication Core admin surfaces pragmatically; email test flow exists, and phase-1 SMS/WhatsApp test targets are now configurable for staged rollout visibility
- `Planned / Near-term`: formalize merchant/tenant onboarding domain rules
- `Planned / Near-term`: formalize settings domain architecture across global and business scopes

### WebApi

- `Completed`: mobile-used route surfaces have been reorganized with canonical audience-first routing while preserving compatibility aliases
- `Completed`: public/member loyalty, member commerce, profile, and storefront groundwork exists
- `Planned / Near-term`: keep mobile-used endpoints stable while WebAdmin and backend operational flows are prioritized
- `Planned / Near-term`: add admin/integration APIs only when WebAdmin and business/mobile scenarios demand them

### Mobile

- `Completed`: mobile apps remain operational and use shared canonical service abstractions
- `Completed`: `Darwin.Mobile.Business` is usable enough to influence delivery priorities
- `Completed`: `Darwin.Mobile.Business` invitation acceptance now works end-to-end against canonical business-auth endpoints
- `Planned / Near-term`: ensure onboarding/account lifecycle backend and admin support flows satisfy business mobile usage
- `Completed`: `Darwin.Mobile.Business` now keeps `Home` and `Settings` available during `PendingApproval`, while `Scanner`, `Session`, `Dashboard`, and `Rewards` are approval-gated
- `Planned / Near-term`: keep mobile route and contract compatibility intact whenever backend changes touch mobile-used flows

### Security and performance

- `Planned / Near-term`: harden authentication, tenant isolation, and sensitive-action protection
- `Planned / Near-term`: ensure pagination/filtering/search for large operational datasets everywhere in admin
- `Planned / Near-term`: use async/background handling for notifications, shipping callbacks, and payment callbacks
- `Planned / Near-term`: improve retry-safe integration and failure diagnostics across provider boundaries

## 7. Localization Program

### Current state

- `Completed`: mobile apps already support bilingual operation
- `In Progress`: WebAdmin is being built in a way that should not block later multilingual enablement

### Required next steps

- `Planned / Near-term`: add WebAdmin localization infrastructure
- `Planned / Near-term`: add shared localization strategy across admin, templates, and system messages
- `Planned / Near-term`: add language settings and fallback policy
- `Planned / Near-term`: translate core admin flows after initial operational completion
- `Planned / Near-term`: localize templates and communication content
- `Completed foundation`: order details and order invoice tabs in WebAdmin now surface persisted tax snapshots, price mode, and customer VAT profile context so finance support can review net/tax/gross from order-bound workflows instead of only CRM invoice screens
- `Completed foundation`: business locations now expose queue-style admin triage for primary, missing-address, and missing-coordinate states, with operational playbooks so location readiness can be reviewed before shipping, storefront, and mobile go-live
- `Completed foundation`: billing financial-accounts and journal-entry workspaces now expose operational summaries and review playbooks, which closes another action-light gap in WebAdmin finance support
- `Completed foundation`: billing expenses now expose summary signals and review playbooks, so cost follow-up is no longer limited to a plain list/editor workflow
- `Completed foundation`: business subscription handoff now exposes plan-level readiness, current-plan detection, and action-oriented external billing-website handoff signals so admins can distinguish ready versus blocked upgrade/support paths from the subscription workspace itself
- `Completed foundation`: billing-plan administration now exists in WebAdmin with queue filters, operational summaries, playbooks, and full create/edit forms, which closes a real control-panel gap in subscription catalog management
- `Completed foundation`: the billing-plan workspace now also behaves as an HTMX-aware queue, so subscription-catalog search, filter, and queue triage stay inside the newer server-rendered workflow instead of falling back to older full-page list refreshes
- `Completed foundation`: CRM customer segments now expose queue filters, operational summaries, and hygiene playbooks, so empty or under-documented segmentation no longer hides inside a plain list screen
- `Completed foundation`: a WebAdmin DI audit found and fixed a CRM composition-root drift where `CreateInvoiceRefundHandler` was injected by `CrmController` but not registered, so the currently wired admin invoice-refund path now has explicit controller-to-DI parity
- `Completed foundation`: inventory-ledger review now includes summary cards and troubleshooting playbooks, so inbound, outbound, and reservation-heavy stock history can be triaged without relying on a raw table alone
- `Completed foundation`: inventory purchase-order administration now includes queue summaries, playbooks, line-count visibility, and clearer status signals so replenishment review no longer depends on a plain list/editor flow
- `Completed foundation`: inventory stock-transfer administration now includes queue summaries, playbooks, line-count visibility, and clearer status badges so warehouse-move troubleshooting is possible before opening each transfer individually
- `Completed foundation`: warehouse administration now includes summary cards, playbooks, and readiness signals for default and empty locations, which makes fulfillment-setup review easier from the main list screen
- `Completed foundation`: supplier administration now includes summary cards, playbooks, missing-address visibility, and active procurement signals, so vendor follow-up is no longer limited to a plain list and editor flow
- `Completed foundation`: loyalty-program administration now includes queue filters, operational summaries, and playbooks, and its create/edit flow now supports the standard HTMX shell pattern instead of relying only on old full-page forms
- `Completed foundation`: loyalty reward-tier administration now includes queue filters, operational summaries, and playbooks, and its create/edit flow now supports the standard HTMX shell pattern so loyalty setup stays aligned with the newer Darwin WebAdmin interaction model
- `Completed foundation`: loyalty campaign administration now includes queue filters, operational summaries, playbooks, and HTMX shell-based create/edit flows, so campaign rollout support no longer depends on a plain list plus legacy full-page editor pattern
- `Completed foundation`: admin-side loyalty account provisioning now uses the standard HTMX shell pattern, so support-driven account creation no longer falls back to a legacy full-page editor flow
- `Completed foundation`: loyalty point-adjustment now uses the standard HTMX shell pattern, so support-driven balance remediation stays aligned with the newer Darwin WebAdmin interaction model
- `Completed foundation`: loyalty account details now behave as an HTMX-aware workspace, so suspend/activate and pending-redemption confirmation can rerender the same support surface instead of falling back to legacy full-page round-trips
- `Completed foundation`: loyalty account and redemption queues now behave as HTMX-aware workspaces, so loyalty triage filters and status subsets stay inside the newer server-rendered workflow instead of dropping back to older full-page list refreshes
- `Completed foundation`: loyalty account queues and account-details navigation now also stay inside HTMX-aware workspace shells for create, details, adjust-points, redemptions, and back-navigation paths, so support operators no longer bounce between fragment-driven loyalty triage and older full-page routes
- `Completed foundation`: loyalty campaign and scan-session queues now also behave as HTMX-aware workspaces, with queue subset navigation and in-place campaign activation staying inside the same shell, so mobile loyalty rollout/support no longer mixes newer fragment-driven flows with older full-page list refreshes
- `Completed foundation`: loyalty program and reward-tier queues now also behave as HTMX-aware workspaces, so program-setup and reward-catalog triage stay inside the newer server-rendered workflow instead of dropping back to older full-page list refreshes
- `Completed foundation`: loyalty program, reward-tier, campaign, account-create, and point-adjustment editors now also use HTMX-aware shell/form workflows with in-shell back paths, and the remaining program/reward/redemption/scan-session list handoffs now stay inside their workspace shells, so loyalty support/setup no longer mixes newer fragment-driven flows with older full-page editor/navigation routes
- `Completed foundation`: business list, support queue, members, invitations, locations, owner-override audits, and staff-access badge workspaces now all render through HTMX-aware workspace helpers, and their remaining queue-safe redirects no longer fall back to plain full-page responses when invoked from the newer server-rendered workflow
- `Completed foundation`: loyalty fallback redirects for deleted programs/tiers, missing campaigns, and missing account surfaces now also respect the HTMX workflow instead of bouncing fragment-driven support users back through legacy full-page redirects
- `Completed foundation`: billing financial-accounts, expenses, and journal-entry workspaces now render through HTMX-aware workspace helpers with in-shell filter/new/edit queue paths, so finance administration outside the payment/refund queue no longer depends on older full-page list refreshes
- `Completed foundation`: user, role, and permission delete flows now also stay inside their HTMX workspace/editor shells, with queue-safe redirects and modal post context wired for in-place refresh instead of falling back to older full-page delete redirects
- `Completed foundation`: loyalty workspace cleanup now includes HTMX-safe root redirects and in-shell delete flows for programs and reward tiers, so the remaining destructive/setup paths in loyalty no longer bounce support operators back through older full-page routes
- `Completed foundation`: CRM segment create/edit now uses the standard HTMX shell pattern, so segmentation hygiene no longer falls back to the older full-page editor model
- `Completed foundation`: inventory stock-level create/edit now uses the standard HTMX shell pattern, so baseline stock maintenance stays aligned with the newer Darwin WebAdmin interaction model
- `Completed foundation`: inventory stock action flows for adjust, reserve, release, and return receipt now use the standard HTMX shell pattern, so operational stock remediation no longer relies on legacy full-page forms

## 8. Decision Log

- `Decision made`: retire the legacy `/admin` and `/dashboard` redirects after `Darwin.WebAdmin` is functionally complete and post-completion testing confirms they are no longer needed
- `Planned / Near-term`: add a cleanup task to remove the legacy `/admin` and `/dashboard` redirects once the WebAdmin completion audit and test pass are finished
- `Decision made`: `MediaAsset` deletion follows a hybrid strategy: keep metadata-level soft delete now, then add reference-aware physical purge/orphan cleanup later
- `Decision made`: SME onboarding follows a staged rollout; phase 1 stays business-first, while deeper tenant/customer separation remains a later design/implementation decision
- `Decision made`: phase-1 business owner onboarding should support both assigning an existing platform user and invitation-first owner creation
- `Decision made`: the "last active owner cannot be removed or disabled" policy may be overridden only by `FullAdmin`, with required reason capture and explicit audit logging
- `Completed`: the `FullAdmin` override workflow for the "last active owner" policy now exists in business-member editing, with required reason entry, explicit UI warning, and persisted audit records
- `Future / Later phase`: add a site-setting option to require dual approval for the last-owner override path in enterprise-heavy deployments
- `Decision made`: invitation acceptance in phase 1 should support both token-entry and magic-link driven flows
- `Completed`: phase-1 invitation emails now include both the manual token path and a configurable magic-link path
- `Decision made`: business-scoped admins should receive delegated onboarding/support actions selectively; invitation issue/resend and reset-support are good candidates, while approval/suspension remain FullAdmin-only
- `Completed foundation`: WebAdmin now enforces that selective delegation model by allowing business-support operators into business list/member/invitation support flows while keeping business lifecycle, owner management, and archive actions FullAdmin-only
- `Planned / Near-term`: normalize legacy-encoded identity/admin views such as the role-permissions editor so future permission UX work does not keep tripping over file-encoding debt

## 23. Deferred Micro-Cleanup After WebAdmin Completion

- `Deferred intentionally`: ultra-low-traffic HTMX polish on remaining microscopic detours that do not block real operator workflows
- `Deferred intentionally`: legacy encoding cleanup on a few surviving admin files where touching them now adds more risk than value
- `Deferred intentionally`: final removal of transitional redirect helpers and stray fallback paths after we formally exit the WebAdmin completion phase
- `Completed foundation`: completion review closed two remaining HTMX consistency gaps in the priority chains: pager navigation now supports in-shell HTMX refresh with URL push, and business archive/remove modal actions now carry HTMX refresh context on the main queue surfaces
- `Completed foundation`: Business Communications now has HTMX-safe workspace/profile/audit flows plus direct operator handoffs from failed-email rows into invitation, user, settings, and business-support surfaces, which closes another non-later Communication Core gap without overclaiming a full template/retry engine
- `Completed foundation`: the billing payment queue now includes a reconciliation-focused subset, summary signal, and row badges for pending, failed, refunded, and Stripe-reference-light rows, which improves Stripe-first settlement review without claiming finished reconciliation automation
- `Completed foundation`: payment rows now also include provider-reference state, open-age hours, and last financial event context, which gives operators a phase-1 provider-history snapshot before deeper reconciliation/dispute tooling lands
- `Completed foundation`: refund rows now also include provider-reference state, open-age hours, last refund event context, and support-attention badges, which gives finance support a phase-1 provider-history view for refund follow-up before deeper dispute tooling exists
- `Completed foundation`: the shipment queue now includes a carrier-review subset, summary signal, shipment-age visibility, and HTMX-safe queue navigation for DHL exception handling, which improves tracking/handoff follow-up without claiming full carrier timeline or RMA completion
- `Completed foundation`: shipment rows now also include tracking-state, last-carrier-event, and exception-note context in both the global queue and order-level shipment grids, which gives operators a phase-1 tracking narrative before deeper DHL timeline or return tooling lands
- `Completed foundation`: order detail now also exposes a return-support baseline card and returned-shipment refund handoffs, so operators can start post-order return follow-up from existing shipment/refund surfaces before a dedicated RMA aggregate lands
- `Completed foundation`: the global shipment queue now also exposes direct `Refunds` and `Start Return Refund` handoffs for returned or carrier-review rows, and the order-level shipment grid now pushes returned/carrier-review rows toward the refund tab, so phase-1 return support is no longer trapped behind an order-detail detour
- `Completed foundation`: the shipment workspace now also exposes a dedicated `Return Follow-up` subset with queue counts, row badges, and direct refund/carrier-review handoffs, so returned parcels needing refund-path or carrier-completion review are no longer mixed into the general returned bucket
- `Completed foundation`: carrier-exception shipment rows now also hand off directly into shipping-method or shipping-setting remediation when missing service data or threshold/tracking configuration is the likely root cause, so DHL troubleshooting no longer stops at passive exception notes
- `Completed foundation`: the email-audit workspace now includes summary cards for failed-flow buckets and recent activity, which improves communication-failure triage before a fuller template/retry system exists
- `Completed foundation`: Communication Core template-inventory and resend-policy tables now also deep-link into failed-audit subsets and the relevant safe operator workspace, so operators can move from policy visibility into action without treating those tables as dead documentation
- `Completed foundation`: the email-audit workspace now also supports `stale pending` and `business-linked failure` triage, so operators can separate transport-latency issues from business-scoped follow-up without waiting for a full Communication Core queue/outbox model
- `Completed foundation`: the business communication profile and failed admin-test audit rows now both expose the safe `Send Test Email` rerun action, so operators can revalidate SMTP/test-inbox fixes without inventing a generic replay queue for live transactional mail
- `Completed foundation`: the Business Communications workspace and business communication profile now also show the live invitation, activation, and password-reset subject/body template previews plus supported-token hints, so operators can inspect current template truth without bouncing out to site settings
- `Completed foundation`: failed invitation, activation, and password-reset audit rows now deep-link into recipient-scoped invitation or member/user support surfaces, so controlled flow-specific resend follow-up is no longer just a generic recommendation string
- `Completed foundation`: the business communication profile now also exposes recipient-aware follow-up actions on recent failed audits, so business-scoped communication troubleshooting no longer has to detour into the full audit queue for every live issue
- `Completed foundation`: site settings and business setup now expose a visible ownership matrix and direct HTMX handoff between global policy and business-scoped defaults, so the staged split between platform settings and tenant-level settings is now explicit in the operator UX
- `Completed foundation`: the billing webhook queue now also exposes payment-exception and dispute-signal subsets from Stripe callback history, so finance support has a phase-1 callback anomaly surface before dedicated dispute tooling lands
- `Completed foundation`: webhook anomaly rows now also expose direct operator handoffs into the payment or refund queues plus payment settings, so Stripe-first callback review no longer stops at signal visibility and can move straight into the next safe support surface
- `Completed foundation`: the payments and refunds workspaces now also surface webhook-anomaly summary signals and direct handoffs into `Payment Exceptions` and `Dispute Signals`, so Stripe-first operators can move from settlement/refund triage into callback evidence without manually pivoting through billing navigation first
- `Completed foundation`: the payments workspace now also exposes a dedicated `Dispute Follow-up` subset with row-level dispute badges and direct webhook dispute handoffs, so Stripe callback anomalies are no longer trapped only in the webhook queue
- `Completed foundation`: the payment editor now includes a reconciliation/dispute snapshot with provider-reference state, last-event visibility, and direct handoffs into the main payment and webhook anomaly subsets, so Stripe troubleshooting no longer has to start only from list queues
- `Completed foundation`: payment and refund queue rows now also expose direct reconciliation/support/dispute queue jumps plus HTMX-safe links into adjacent order/customer/invoice/payment workspaces, so Stripe-first follow-up no longer depends on mixing queue triage with plain-link detours
- `Completed foundation`: the settings ownership split now also includes HTMX-safe handoffs between global site settings and business setup/support surfaces, so operators can move directly from policy ownership guidance into the right platform or tenant workspace
- `Completed foundation`: localization fallback governance is now explicit in both site settings and business setup, with direct handoffs into business and CRM review surfaces so tenant overrides are not confused with platform-wide defaults
- `Completed foundation`: the shared `ValidationResource*.resx` path now also covers orders, loyalty, inventory, and cart/checkout validator feedback plus selected business-rule messages, so bilingual validation coverage is no longer concentrated only in settings/catalog/CMS/auth
- `Completed foundation`: that same `ValidationResource*.resx` path now also covers pricing uniqueness, shipping uniqueness, SEO redirect uniqueness, add-on delete/attach validation, and selected catalog/CMS/settings business-rule feedback, so more front-office and operator-facing rule failures are now bilingual across both WebAdmin and WebApi
- `Completed foundation`: the `ValidationResource*.resx` path now also covers CRM invoice edit/status/refund financial-rule feedback, so invoice-payment mismatch, refund eligibility, and invoice-transition failures are no longer left as raw English strings in admin/API financial workflows
- `Completed foundation`: billing-plan administration now also routes duplicate-code, not-found, and concurrency feedback through `ValidationResource*.resx`, so subscription-catalog maintenance no longer mixes localized queue UX with raw English handler errors
- `Completed foundation`: billing payment/account/expense/journal update feedback now also routes not-found and concurrency messages through `ValidationResource*.resx`, so finance correction flows no longer fall back to raw English business-rule errors behind a bilingual admin shell
- `Completed foundation`: business lifecycle, invitation resend/revoke/create, and member-owner-protection feedback now also routes through `ValidationResource*.resx`, so approval/suspension, invitation support, and last-owner override incidents are bilingual across both WebAdmin and WebApi
- `Completed foundation`: storefront cart, checkout intent/place-order, payment confirmation, add-on pricing, and stock-helper/remediation feedback now also route through `ValidationResource*.resx`, so front-office purchase-flow failures and inventory allocation/reservation guardrails are no longer left as raw English strings behind the de/en platform baseline
- `Completed foundation`: cart-summary pricing/tax/currency guardrails plus business/location/media and baseline inventory-management not-found/concurrency feedback now also route through `ValidationResource*.resx`, so storefront quote computation and day-one admin correction flows are less dependent on raw English business-rule errors
- `Completed foundation`: CRM engagement, customer/lead/opportunity maintenance, customer-segment assignment, add-on-group attach/update, and media-asset edit guardrails now also route through `ValidationResource*.resx`, so common CRM cleanup and lower-traffic catalog/media remediation no longer fall back to raw English business-rule feedback
- `Completed foundation`: brand/menu/promotion/tax-category/redirect-rule/shipping-method maintenance plus product add-on applicability, shipment creation, and inventory release/allocation not-found or concurrency guardrails now also route through `ValidationResource*.resx`, so the remaining low-traffic admin maintenance paths are less dependent on raw English handler feedback
- `Completed foundation`: category/product/page concurrency feedback, loyalty current-user guardrails, and remaining order-billing not-found/linkability failures now also route through `ValidationResource*.resx`, so another slice of cross-host admin/storefront/API rule feedback is now bilingual instead of English-only
- `Completed foundation`: the WebAdmin shared UI-key inventory has now been bulk-migrated into `SharedResource*.resx`, so `AdminUiTextService` is effectively `resx-first` and its large in-code map is now only a transitional legacy fallback instead of the main localization source
- `Completed foundation`: the current application-side scan for raw `InvalidOperationException("...")` and `DbUpdateConcurrencyException("...")` strings is now closed for the migrated paths, so the main bilingual rule-feedback baseline is no longer obviously punctured by those direct English throw sites
- `Completed foundation`: WebAdmin localization now uses a cleaned-up `Localization` slice, root-level `SharedResource*.resx` / `ValidationResource*.resx` marker structure, and a platform-level `AdminTextOverridesJson` site-setting field so operators can override shared admin wording without editing source resource files; the override path now layers on top of `SharedResource*.resx` instead of the old in-code fallback map
- `Completed foundation`: business setup now also exposes a tenant-scoped `AdminTextOverridesJson` field stored on the `Business` aggregate, and admin text resolution now applies business override first, then platform site-setting override, then shared `resx` text, so one business can adjust operator wording without mutating the platform-wide baseline
- `Completed foundation`: invoice and order tax surfaces now also expose archive-readiness and e-invoice-baseline indicators based on issuer/VAT completeness, so phase-1 tax support can distinguish missing issuer data from the still-unimplemented deeper compliance workflows
- `Completed foundation`: Business Communications workspace/profile/audit navigation now also pushes URL state for its main HTMX transitions, so operators can move between workspace, profile, and audit triage without losing browser history fidelity
- `Completed foundation`: the Mobile Operations workspace now also supports HTMX-safe filtering, pagination, device remediation, and cross-workspace handoffs, so mobile diagnostics no longer drop back to older full-page admin navigation
- `Completed foundation`: inventory controller redirect debt is now closed across warehouses, suppliers, stock actions, stock levels, transfers, and purchase orders, so missing-row, success, and concurrency paths stay inside the HTMX workflow instead of falling back to plain redirects
- `Completed foundation`: site settings now render through the same HTMX-aware editor shell on both initial GET and save redirect paths, so platform configuration no longer keeps a legacy full-page entry path while the rest of WebAdmin uses workspace shells
- `Completed foundation`: site settings ownership and operator handoff links now also push browser URL state during HTMX transitions, so platform-to-business navigation from settings no longer loses history fidelity while staying inside the shell workflow
- `Completed foundation`: business communication profile handoffs now also push browser URL state for setup, member, invitation, and settings-remediation links, so operator navigation out of communication troubleshooting keeps history fidelity instead of behaving like a dead-end fragment hop
- `Completed foundation`: the main Business Communications workspace now also pushes browser URL state for settings remediation and audit/operator handoffs, so phase-1 communication triage behaves like a navigable workspace instead of a fragment-only dead end
- `Decision made`: settings should move via staged split from global to business-specific; start with branding/localization/communications, then move payment/shipping once provider integrations mature
- `Decision pending`: decide when to introduce a real multi-business switcher in business-facing clients instead of only preserving the preferred business context during refresh
- `Decision pending`: decide whether phase-1 activation should allow admin-side email-confirm override only, or require every activation/resend flow to consume a public confirm-email token before go-live
- `Decision made`: phase-1 password authentication now rejects unconfirmed accounts and locked accounts; follow-up UX should add self-service resend-activation where appropriate
- `Decision pending`: decide whether self-service activation resend should remain email-entry based in phase 1, or also support deep-link/magic-link recovery entry in later auth UX
- `Decision made`: phase-1 business approval policy is `Soft Gate`; `Darwin.Mobile.Business` sign-in remains allowed for `PendingApproval`, but live operational screens/actions stay blocked until `BusinessOperationalStatus.Approved`
- `Decision pending`: decide whether the current soft-gate should later evolve into a dedicated onboarding workspace/tab instead of screen-level blocking on operational pages
- `Completed foundation`: another low-traffic localization audit pass closed raw TempData feedback in permissions, roles, media, orders, and loyalty controllers by moving those user-visible strings onto `SharedResource*.resx`, which reduces the remaining de/en inconsistency outside the main high-traffic admin workspaces
- `Completed foundation`: the next localization audit pass closed raw TempData feedback in business lifecycle/setup/member support and product maintenance controllers, which reduces another chunk of low-traffic de/en inconsistency without introducing broader controller churn
- `Completed foundation`: `Darwin.Web` cart and checkout now also surface a live storefront-discovery window backed by published CMS pages, public categories, and visible product opportunities, so active purchase routes can hand shoppers back into content, browse, and upsell paths without dropping the current conversion flow
- `Completed foundation`: the shared commerce storefront window now also picks the strongest visible product offer instead of the first catalog card, so cart, checkout, and confirmation show a clearer next-buy signal
- `Completed foundation`: `Darwin.Web` order confirmation now also surfaces that same live storefront-discovery window, so after-purchase shoppers can move directly into content, browse, and product follow-up instead of ending on a receipt-only route
- `Completed foundation`: `Darwin.Web` order confirmation now also biases its product follow-up away from items already present in the just-finished order, so the after-purchase opportunity behaves more like a true next-buy suggestion than a generic catalog echo
- `Completed foundation`: `Darwin.Web` authenticated member dashboard now also surfaces live product highlights alongside CMS and category continuation, so signed-in shoppers can see the next buying opportunity directly from `/account`
- `Completed foundation`: `Darwin.Web` orders and invoices now also surface live product highlights alongside their storefront continuation windows, so member history routes can still create a next-buy moment instead of behaving like passive archives
- `Completed foundation`: those orders and invoices storefront offer boards now also shift away from products already linked to the active cart when browser storefront-shopping state exists, so member history routes can pitch the next buying move beyond the current basket instead of echoing it
- `Future / Later phase`: consider a cross-surface commercial ranking service for Darwin.Web that can choose the strongest next-buy/product/content follow-up per route from live cart, order, invoice, and member signals instead of today’s route-local spotlight heuristics
- `Completed foundation`: `Darwin.Web` order and invoice detail routes now also surface live product highlights alongside their storefront continuation windows, so protected commerce detail pages can create a next-buy moment instead of only offering content and category follow-up
- `Completed foundation`: protected order and invoice detail storefront merchandising is now also cart-aware, so those commerce detail routes avoid echoing items already linked to the active storefront cart and keep the next-buy signal cleaner
- `Completed foundation`: `Darwin.Web` profile, preferences, security, and addresses now also surface live product highlights inside their shared storefront continuation window, so authenticated account editor routes can create a next-buy moment instead of only offering content and category follow-up
- `Completed foundation`: that shared account-editor storefront window now also ranks product highlights by the strongest visible savings signal first and surfaces compare-at context, so self-service routes present a sharper best-offer moment instead of a raw product list
- `Completed foundation`: `Darwin.Web` account hub plus sign-in/register/activation/password routes now also surface live product highlights inside their shared public-auth continuation flow, so anonymous account recovery can preserve a next-buy opportunity instead of only keeping cart/content/category context alive
- `Completed foundation`: those public-auth and account-entry product highlights now also rank by the strongest visible savings signal first, so self-service routes surface a clearer best-offer moment instead of echoing arbitrary catalog order
- `Completed foundation`: the public account hub now also surfaces a live offer board with multiple strongest visible product opportunities, so anonymous account entry can pitch several next-buy options instead of stopping at one spotlight
- `Completed foundation`: `Darwin.Web` protected member-route auth walls now also surface the same live storefront continuation context, so signed-out visits to orders, invoices, and account editor routes keep cart/content/product opportunity visible instead of collapsing into a minimal access block
- `Completed foundation`: protected member-route auth walls now also surface a live offer board with several strongest visible buying opportunities, so protected entry does not reduce commercial momentum to a single spotlight
- `Completed foundation`: `Darwin.Web` cart and checkout now also surface live product highlights inside their guest account/auth handoff, so anonymous shoppers can keep a next-buy opportunity visible while deciding whether to sign in or create an account
- `Completed foundation`: guest commerce auth handoff now also ranks product highlights by the strongest visible savings signal first, so cart and checkout keep a clearer best-offer pitch alongside account recovery
- `Completed foundation`: the guest commerce auth handoff now also surfaces a live offer board with multiple strongest visible product opportunities, so cart and checkout can keep several next-buy options visible during the account decision
- `Completed foundation`: the shared commerce storefront-discovery window now also surfaces a live offer board with multiple strongest visible product opportunities, so cart, checkout, and confirmation can show several next-buy options instead of a single buying spotlight
- `Completed foundation`: `Darwin.Web` guest order confirmation now also surfaces a next-buy highlight inside the account-tracking handoff, so anonymous after-purchase shoppers keep a real commercial next step visible while deciding whether to sign in or create an account
- `Completed foundation`: guest order confirmation now also surfaces a live next-buy offer board inside that account-tracking handoff, so anonymous after-purchase shoppers can see several post-purchase opportunities instead of one spotlight
- `Completed foundation`: that guest confirmation next-buy highlight now also selects the strongest visible offer outside the just-purchased items first, so after-purchase account tracking keeps a cleaner upsell signal instead of echoing the first catalog card
- `Future / Later phase`: consider replacing the current savings-first merchandising heuristic on auth/account/commerce continuation surfaces with campaign- or margin-aware ranking once WebApi exposes stronger commercial ranking signals
