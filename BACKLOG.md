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
- `Planned / Near-term`: add equivalent self-service resend-activation UX in front-end web flows now that confirmation is enforced at sign-in

### Communication Core (email-first MVP)

- `Planned / Near-term`: implement minimum viable Communication Core with email as the first operational channel
- `Planned / Near-term`: support signup email, account activation email, invitation email, forgot-password email, reset-password email, and important account notifications
- `In Progress`: business invitation emails now exist via SMTP-backed operational email sending, but still use simple transactional composition rather than a full Communication Core template/logging model
- `In Progress`: phase-1 Communication Core admin support now includes template visibility, delivery history context, severity/backlog triage, controlled retry for failed or pending invitation, activation, and password-reset rows, and policy-aware retry state (`ready`, `cooldown`, `rate-limited`, blocked reason, recent-chain volume) so operators can distinguish safe replay from noisy resend behavior; deeper outbox/provider abstraction and richer delivery-log modeling remain near-term

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

- `In Progress`: Communication Management now includes delivery-state visibility, delivery-history context, repeated-failure triage, safe test reruns, and controlled resend/retry for supported live flows
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

- `In Progress but secondary`: continue `Darwin.Web` storefront and member portal implementation against `Darwin.WebApi`
- `Future / Later phase`: broaden public delivery capabilities after operational admin/backend work stabilizes

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
