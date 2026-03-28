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
- `Planned / Near-term`: run a functional audit of all admin navigation, quick actions, and operator workflows from the perspective of daily SME usage
- `Planned / Near-term`: close high-friction support gaps in orders, CRM, media, settings, and business/user management
- `Completed foundation`: the admin media library now supports queue-style filters for missing alt text, editor-only assets, and library assets, so content cleanup can be worked as an operator queue instead of a flat gallery
- `Completed foundation`: the media queue now also exposes direct `Open File` and contextual `Set Alt` follow-up actions plus clearer asset-role badges, so routine content cleanup is less dependent on opening every asset blindly
- `Completed foundation`: the CRM customers list now supports queue-style filters for linked-user customers, customers needing segmentation, and customers with open opportunity context, making customer operations less list-heavy
- `Completed foundation`: the CRM customers queue now also exposes direct quick actions for linked user review, interactions, segment membership work, and prefilled opportunity creation, so common follow-up no longer always requires manually navigating through the full customer edit flow
- `Completed foundation`: the CRM leads list now also supports queue-style filters for qualified, unassigned, and unconverted leads, so pipeline follow-up can be worked as an operator queue instead of a flat list
- `Completed foundation`: the CRM opportunities list now supports queue-style filters for open, closing-soon, and high-value opportunities, so revenue follow-up is less dependent on a flat pipeline list
- `Completed foundation`: CRM lead and opportunity lists now expose richer follow-up context and direct quick actions, including assigned-owner visibility, quick lead conversion for qualified rows, customer deep-links, and prefilled opportunity creation from customer-linked leads
- `Completed foundation`: the CRM invoices queue now also exposes direct quick actions for customer/order/payment follow-up and common draft-to-open / open-to-paid transitions, so routine invoice operations no longer always require entering the full invoice editor first
- `Completed foundation`: the admin orders list now supports queue-style filters for open orders, payment-issue orders, and fulfillment-attention orders, so post-order operations are less dependent on a flat status list
- `Completed foundation`: order detail tabs for payments, shipments, refunds, and invoices now support queue-style filters too, so operators can work failed/refunded/pending/outstanding subsets without scanning full grids
- `Completed foundation`: the orders queue now also exposes direct quick actions for add payment, add shipment, and create invoice from list rows, so common support follow-up no longer always requires entering the full order detail first
- `Completed foundation`: inventory purchase orders and stock levels now support queue-style filters for draft/issued/received replenishment work and low-stock/reserved/in-transit stock review, so inventory ops are less dependent on flat lists
- `Completed foundation`: stock transfers now support queue-style filters for draft, in-transit, and completed transfer work, so replenishment follow-up is less dependent on manual scanning
- `Completed foundation`: warehouse and supplier lists now support queue-style filters for default/no-stock-level warehouses and missing-address/active suppliers, so inventory setup review is less dependent on flat lists
- `Completed foundation`: inventory ledger now supports queue-style filters for inbound, outbound, and reservation movements, and stock-level rows link directly into variant ledger review for faster stock troubleshooting
- `Completed foundation`: stock-level rows now expose direct `adjust`, `reserve`, and `release reservation` actions in WebAdmin, so inventory troubleshooting can move from queue detection into manual corrective action without leaving the operational context
- `Completed foundation`: stock-level rows now also expose a direct `return receipt` action, so phase-1 customer return intake can increase inventory from the same operational workspace without dropping into ad hoc scripts or database fixes
- `Completed foundation`: supplier rows now deep-link into purchase-order follow-up and warehouse rows deep-link into scoped stock-level review, reducing drill-in friction for procurement and stock support work
- `Completed foundation`: WebAdmin now has first-class loyalty workspaces for programs, reward tiers, accounts, campaigns, and recent scan-session diagnostics, so business-mobile loyalty operations are no longer managed only from the mobile app or raw API usage
- `Completed foundation`: WebAdmin now has a dedicated `Mobile Operations` workspace for JWT/mobile bootstrap settings, onboarding/support dependency counts, and transport-readiness visibility that affect the mobile apps directly
- `Completed`: WebAdmin loyalty operations now support admin-side account provisioning for member-support cases where a consumer has not self-enrolled yet
- `Completed`: WebAdmin loyalty operations now expose a dedicated redemption troubleshooting workspace with pending/completed/cancelled filters and direct confirm actions for pending redemptions
- `Completed`: Mobile Operations now includes real device-fleet diagnostics, app-version visibility, and device-level filters for stale installs, missing push tokens, notification-disabled devices, and business-member devices
- `Completed`: Mobile Operations now also supports lightweight device remediation through admin-side push-token clearing and device deactivation
- `Completed`: admin dashboard discoverability for loyalty/mobile is now in place through a compact snapshot with direct entry points into loyalty accounts, pending redemptions, scan sessions, and mobile device diagnostics
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
- `Completed foundation`: delegated business-support access now exists in WebAdmin through a dedicated permission/role path for member support and invitation operations without exposing approval, lifecycle, or owner-management actions
- `Completed foundation`: identity/admin screens now surface the delegated business-support role and permission more clearly, so assigning support access no longer depends on tribal knowledge
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
- `Planned / Near-term`: add provider abstraction, template support, delivery logging, retry handling, and admin visibility for email operations

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
- `Completed`: user edit screens now expose operational account state and support actions for confirm-email override, password reset email, and lock/unlock
- `Completed`: user edit screens now also support sending activation emails for unconfirmed accounts
- `Completed`: shared search/reset/pager behavior now covers legacy and newer operator lists across catalog, CMS, identity, billing, inventory, and orders
- `Completed foundation`: loyalty administration is now exposed directly in WebAdmin through programs, reward tiers, campaigns, accounts, and recent scan-session review screens
- `Completed`: loyalty account queues now also allow admin-side account creation, so support can provision accounts without waiting for self-enrollment
- `Completed foundation`: a dedicated mobile-operations admin page now centralizes mobile bootstrap settings and business-mobile readiness signals instead of leaving them spread across site settings and support queues
- `Planned / Near-term`: continue removing scattered `fetch`-based fragment refreshes from older WebAdmin pages
- `Planned / Near-term`: standardize remaining partial loading, modal submission, and alert refresh patterns
- `Planned / Near-term`: deepen operator workflows in Orders, CRM, and Media beyond structural refactor
- `Planned / Near-term`: deepen business onboarding from CRUD/invitation into activation, approval, and support workflows

### Communication management

- `Planned / Near-term`: add communication logs, resend/retry actions, and delivery-state visibility in WebAdmin
- `Planned / Near-term`: add per-business communication settings
- `Planned / Near-term`: introduce localization-aware notification/email template management
- `Planned / Near-term`: move password-reset, invitation, and future activation emails from direct SMTP composition into Communication Core templates and delivery logging
- `Completed foundation`: a dedicated `Business Communications` workspace now exists in WebAdmin for operator visibility into phase-1 transport readiness and business-level sender/support-email setup gaps
- `Completed foundation`: the `Business Communications` workspace now includes a pageable business queue for missing support-email and sender-identity setup, with direct links into business setup and global transport settings
- `Completed foundation`: the same workspace now documents the currently live hard-coded transactional email flows so operators can distinguish implemented email behavior from future Communication Core template/log capabilities
- `Completed foundation`: the `Business Communications` workspace now includes queue filters for missing support email, missing sender identity, and policy-enabled subsets, so communication debt can be worked as an operator queue rather than a static report
- `Completed foundation`: phase-1 SMTP email delivery now creates `EmailDispatchAudit` records, and the `Business Communications` workspace surfaces recent delivery attempts/failures for operational visibility
- `Completed foundation`: the `Business Communications` workspace now also has a full email audit-log screen with search/status filters, so SMTP delivery attempts and failures are no longer limited to a dashboard preview
- `Completed foundation`: each business in the communication queue now has a dedicated communication-profile screen that combines sender/support defaults, policy flags, global dependency readiness, and onboarding/support signals for troubleshooting
- `Completed foundation`: phase-1 email audits are now tagged with flow metadata (`BusinessInvitation`, `AccountActivation`, `PasswordReset`) and optional business correlation, so delivery failures are more diagnosable before full Communication Core logging exists
- `Completed foundation`: the `Business Communications` workspace now also exposes a capability-coverage matrix so operators can see which template, retry, and delivery-visibility capabilities are truly live today versus still planned Communication Core work
- `Completed foundation`: each business communication profile now includes recommended next actions and recent business-scoped email activity, so troubleshooting can move from visibility into operator action without leaving the workspace
- `Completed foundation`: the email audit-log now includes flow-specific operator playbooks, failed-flow quick filters, and business-linked shortcuts, so failed invitation/activation/password-reset emails are no longer only raw diagnostics
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

- `Planned / Near-term`: improve tax/VAT-aware order and invoice snapshots
- `Planned / Near-term`: add VAT ID support and B2B/B2C differentiation
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
- `Planned / Near-term`: deepen shipment/return domain model and delivery exception lifecycle
- `Planned / Near-term`: model Communication Core as a platform capability rather than feature-local helper code
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
- `Decision made`: settings should move via staged split from global to business-specific; start with branding/localization/communications, then move payment/shipping once provider integrations mature
- `Decision pending`: decide when to introduce a real multi-business switcher in business-facing clients instead of only preserving the preferred business context during refresh
- `Decision pending`: decide whether phase-1 activation should allow admin-side email-confirm override only, or require every activation/resend flow to consume a public confirm-email token before go-live
- `Decision made`: phase-1 password authentication now rejects unconfirmed accounts and locked accounts; follow-up UX should add self-service resend-activation where appropriate
- `Decision pending`: decide whether self-service activation resend should remain email-entry based in phase 1, or also support deep-link/magic-link recovery entry in later auth UX
- `Decision made`: phase-1 business approval policy is `Soft Gate`; `Darwin.Mobile.Business` sign-in remains allowed for `PendingApproval`, but live operational screens/actions stay blocked until `BusinessOperationalStatus.Approved`
- `Decision pending`: decide whether the current soft-gate should later evolve into a dedicated onboarding workspace/tab instead of screen-level blocking on operational pages
