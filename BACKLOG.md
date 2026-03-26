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

### Business and tenant onboarding

- `In Progress`: business creation and onboarding workflow now has a real WebAdmin foundation, not only raw entity persistence
- `Planned / Near-term`: support tenant/customer provisioning for new business onboarding
- `In Progress`: support owner/admin assignment during onboarding
- `In Progress`: support invitation-based onboarding for owners/staff when no existing platform user is available
- `Completed`: `Darwin.Mobile.Business` now supports token-entry invitation preview and acceptance for phase-1 onboarding
- `Planned / Near-term`: model and expose onboarding state, activation state, approval state, and suspension/reactivation rules
- `In Progress`: seed and apply initial defaults during onboarding (locale, branding basics, payment/shipping defaults, communication defaults where applicable)

### Authentication and account lifecycle

- `In Progress`: complete signup, invitation, activation, forgot-password, and reset-password operational flows
- `Completed`: WebAdmin now supports lock/unlock, admin email confirmation override, and password-reset initiation support for operator troubleshooting
- `Completed`: platform now has a real confirm-email token flow plus resend-activation email issuance via public auth endpoints and WebAdmin support actions
- `Planned / Near-term`: decide when sign-in should actively enforce email confirmation for phase-1 go-live without breaking current mobile and consumer onboarding expectations
- `Completed`: business-app token refresh now preserves preferred business context during onboarding-safe refresh cycles
- `Planned / Near-term`: ensure business-user status directly affects access in mobile and admin-backed support workflows

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
- `Completed`: user edit screens now expose operational account state and support actions for confirm-email override, password reset email, and lock/unlock
- `Completed`: user edit screens now also support sending activation emails for unconfirmed accounts
- `Completed`: shared search/reset/pager behavior now covers legacy and newer operator lists across catalog, CMS, identity, billing, inventory, and orders
- `Planned / Near-term`: continue removing scattered `fetch`-based fragment refreshes from older WebAdmin pages
- `Planned / Near-term`: standardize remaining partial loading, modal submission, and alert refresh patterns
- `Planned / Near-term`: deepen operator workflows in Orders, CRM, and Media beyond structural refactor
- `Planned / Near-term`: deepen business onboarding from CRUD/invitation into activation, approval, and support workflows

### Communication management

- `Planned / Near-term`: add communication logs, resend/retry actions, and delivery-state visibility in WebAdmin
- `Planned / Near-term`: add per-business communication settings
- `Planned / Near-term`: introduce localization-aware notification/email template management
- `Planned / Near-term`: move password-reset, invitation, and future activation emails from direct SMTP composition into Communication Core templates and delivery logging

### Payments, refunds, reconciliation, disputes

- `Completed`: generic payment list/edit/refund visibility exists in WebAdmin and reconciliation projections exist
- `Planned / Near-term`: add Stripe-specific operational visibility, provider references, and status history
- `Planned / Near-term`: deepen refund, reconciliation, dispute, and support workflows
- `Planned / Near-term`: add webhook/callback audit trail visibility

### Shipping and returns

- `Completed`: order-bound shipment visibility exists in admin
- `Planned / Near-term`: add DHL-first shipment workspace, tracking timeline, label info, and exception handling
- `Planned / Near-term`: add return shipment / return request / RMA foundations

### Settings UI and architecture

- `In Progress`: basic site settings UI exists
- `Planned / Near-term`: restructure settings into categories such as General, Business Profile, Localization, Branding, Payments, Shipping, Communications, Users & Roles, Security, Integrations, Tax & Invoicing, and Advanced
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

- `Decision pending`: decide when it is safe to retire the legacy `/admin` and `/dashboard` redirects still served by `DashboardController`
- `Decision pending`: decide whether `MediaAsset` deletion should remain metadata-only soft delete or evolve into reference-aware physical file purge/orphan cleanup
- `Decision pending`: decide how far initial tenant/customer separation should go for SME onboarding in the first go-live wave versus a lighter business-first provisioning model
- `Decision pending`: decide whether phase-1 business onboarding should remain "assign existing user as owner" or include invitation-first owner creation before the Communication Core MVP is complete
- `Decision pending`: decide whether support admins should have an explicit emergency override for the "last active owner cannot be removed or disabled" policy
- `Decision pending`: decide whether invitation acceptance in phase 1 should be token-entry driven, magic-link driven, or both across `Darwin.Mobile.Business` and future front-end onboarding flows
- `Decision pending`: decide when to introduce a real multi-business switcher in business-facing clients instead of only preserving the preferred business context during refresh
- `Decision pending`: decide whether phase-1 activation should allow admin-side email-confirm override only, or require every activation/resend flow to consume a public confirm-email token before go-live
- `Decision pending`: decide when authentication should reject unconfirmed accounts by policy, since activation tokens now exist but login is still backward-compatible for current clients
