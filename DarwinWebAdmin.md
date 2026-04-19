# Darwin WebAdmin Guide

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![HTMX](https://img.shields.io/badge/HTMX-2.0-3366CC?logo=htmx&logoColor=white)](https://htmx.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)

> Scope: `src/Darwin.WebAdmin`, the internal back-office used by staff and administrators.

- `Invitation creation remediation`: `_BusinessInvitationForm` now exposes direct pivots for `Invitations`, `Setup`, `Business Support Queue`, and `Merchant Readiness`, so invitation help does not stop at passive copy.
- `Member assignment remediation`: `_BusinessMemberForm` now exposes direct pivots for `Members`, `Pending Activation`, `Business Support Queue`, and `Merchant Readiness`, so assignment help does not stop at passive copy.
- `Location form remediation`: `_BusinessLocationForm` now exposes direct pivots for `Setup` and `Merchant Readiness`, so location editing does not stop at a save/cancel-only footer.
- `Business creation remediation`: `_BusinessForm` now exposes direct pivots for `Users`, `Business Support Queue`, and `Merchant Readiness` from the initial-owner help block, so business creation does not stop at passive guidance.
- `Business lifecycle remediation`: `_BusinessForm` now exposes direct pivots for `Business Support Queue` and `Merchant Readiness` from the active-state help block, so lifecycle constraints do not stop at passive guidance.
- `Business edit remediation`: `_BusinessForm` now exposes direct pivots for `Business Support Queue` and `Merchant Readiness` from the edit footer, so business edit flows do not stop at structural workspace links alone.
- `Member editor remediation`: `_BusinessMemberEditorShell` now exposes direct pivots for `Members`, `Business Support Queue`, and `Merchant Readiness` from the base edit flow, so member editing does not rely only on header navigation or owner-override branches.
- `Editor footer remediation`: `_BusinessLocationEditorShell` and `_BusinessInvitationEditorShell` now expose footer pivots for their workspace follow-up lanes, so these editor flows do not rely only on header navigation.
- `Staff badge payload remediation`: `StaffAccessBadge` now exposes direct pivots for `Edit member`, `User edit`, `Members`, and `Merchant Readiness` from the payload card, so badge payload review does not stop at explanatory copy plus raw payload.
- `Owner override row remediation`: `OwnerOverrideAudits` now exposes direct pivots for `Business Support Queue` and `Merchant Readiness` from each audit row, so governance event review does not stop at user/member drill-in alone.
- `Subscription invoice row remediation`: `SubscriptionInvoices` now exposes direct pivots for `Business Support Queue` and `Merchant Readiness` from each invoice row, so invoice review does not stop at hosted/pdf/payments actions alone.
- `Subscription invoice empty-state remediation`: `SubscriptionInvoices` now exposes direct pivots for `Subscription`, `Payments`, `Business Support Queue`, and `Merchant Readiness` from its empty state, so zero-row invoice review does not stop at passive copy.
- `Location row remediation`: `Locations` now exposes direct pivots for `Setup` and `Merchant Readiness` from each location row, so location review does not stop at edit/archive actions alone.
- `Invitation row remediation`: `Invitations` now exposes direct pivots for `Business Support Queue` and `Merchant Readiness` from each invitation row, so invitation review does not stop at resend/revoke mutations alone.
- `Member row remediation`: `Members` now exposes direct pivots for `Business Support Queue` and `Merchant Readiness` from each member row, so member review does not stop at edit/user/mobile/loyalty actions and local mutations alone.

## 1. Role of WebAdmin

`Darwin.WebAdmin` is not a secondary side-panel. It is the first operational control center for Darwin.

Its responsibility is to support:

- business creation and setup
- tenant/customer provisioning where applicable
- user, owner, admin, and staff management
- onboarding and activation support
- catalog and CMS management
- payment and shipping visibility
- CRM and support operations
- inventory and procurement operations
- settings and operational configuration
- troubleshooting and admin intervention

This is the most important active delivery surface in the current phase.

## 2. Current Status

- `In Progress`: major module coverage exists and the HTMX-first rewrite is now near operational completion
- `Highest priority`: current team focus is to complete WebAdmin before wider storefront expansion
- `Operational goal`: make day-one SME operations workable from WebAdmin plus supporting backend/mobile flows

## 3. Technology Stack

- ASP.NET Core MVC
- Razor views
- HTMX for server-driven partial interactions
- Bootstrap for layout and components
- minimal JavaScript only where modal orchestration or editor integration still requires it
- ASP.NET Core localization via `IStringLocalizer` and `.resx` resources for shared UI and application validation text, with `SharedResource*.resx` for WebAdmin UI text and `ValidationResource*.resx` in `Darwin.Application` for cross-host validator/business-rule feedback; `SharedResource*.resx` is now the single source of truth for admin UI text, the current migrated application paths no longer rely on obvious raw English `InvalidOperationException`/`DbUpdateConcurrencyException` string literals for their main rule feedback, and site settings now expose a platform-level `AdminTextOverridesJson` layer so wording overrides can be applied without editing source resource files
- WebAdmin localization plumbing now lives under a dedicated `Localization` slice instead of the generic `Infrastructure` bucket; `SharedResource` and `ValidationResource` intentionally stay as root-level marker types because that keeps `Resources/SharedResource*.resx` and `Resources/ValidationResource*.resx` direct and predictable while feature-specific resource families can still be added later if a real bounded-context split emerges, and the current platform-level override contract intentionally uses the shorter `AdminText` wording rather than the older `AdminUiText` phrasing so naming stays aligned with `AdminTextLocalizer`
- the current admin language contract is now also centralized in a dedicated culture catalog instead of repeating `de-DE` / `en-US` defaults across controllers, settings cache, and view models, which makes the bilingual baseline easier to evolve without accidentally splitting platform-culture rules from one another
- admin text resolution now has a clear precedence model: business-scoped `AdminTextOverridesJson` on business surfaces, then platform-wide `AdminTextOverridesJson` from site settings, then `SharedResource*.resx`

HTMX is the preferred mechanism for partial loading and form submission. Alpine.js is not part of the current stack.

## 4. WebAdmin as Operational Control Center

WebAdmin must be treated as the initial command center for:

- creating and provisioning businesses
- assigning owner/admin users
- completing initial setup
- supporting activation and approval workflows
- exposing payment, shipment, communication, and account visibility
- supporting admin troubleshooting for mobile and backend-driven operations

This is especially important because early operational usage is expected to start from `Darwin.Mobile.Business`, which depends on backend/admin setup being correct.

## 5. Module Status Overview

| Area | Status | Notes |
| --- | --- | --- |
| Catalog/CMS | In Progress | Core CRUD and HTMX patterns exist; products, pages, categories, brands, add-on groups, and media/pages workspaces now have queue-style filters, live summary cards, attachment/context visibility where relevant, HTMX-safe list/search/pager/editor-back flows, and operator playbooks, while add-on attachment flows now also stay inside shell-based search/save/pager workflows; the shared application validation-resource path now also covers key catalog/CMS validation messages such as translation requirements, public-menu URL rules, add-on-group attach/update feedback, media-asset edit guardrails, brand/menu maintenance feedback, product add-on applicability not-found handling, and category/product/page concurrency feedback, while broader operational completeness still needs audit on the last lower-traffic config screens. |
| Business / Tenant Onboarding | In Progress | Business CRUD, owner assignment, member management, location management, invitation management, approval/suspension/reactivation, onboarding checklist visibility, actionable next-step shortcuts, delegated support access, dedicated business subscription support, staff-badge support preview, and business mobile soft-gate policy now exist; the main businesses queue plus support/setup surfaces are now also moving onto the de/en localization baseline, and business setup now also exposes a tenant-scoped `AdminTextOverridesJson` field for business-only operator wording overrides layered above the platform baseline, while tenant/customer provisioning and richer onboarding-state orchestration remain near-term. |
| Orders/Billing | In Progress | Order detail, payments, refunds, invoices, and reconciliation visibility exist, and both the orders list and the detail tabs now have queue-style operational filters for open, payment-issue, fulfillment, refund, and invoice follow-up work; the orders list now exposes quick payment/shipment/invoice actions and is now moving onto the de/en localization baseline, the orders detail shell plus payment/refund/invoice/shipment tab grids are now also moving onto that same bilingual path, the billing payments queue exposes direct invoice/order/customer follow-up actions, the billing payments/refunds entry queues are now also moving onto the same bilingual baseline, and webhook/payment-detail surfaces are now following that same bilingual path, expense rows expose supplier follow-up, financial-account rows deep-link into journal-entry follow-up, financial accounts / journal entries now expose lightweight account-type and recent-or-complex queue filters, lower-traffic order detail grids now also keep pager and editor/config handoff links inside HTMX-safe shells, and phase-1 Stripe/VAT readiness is now surfaced from both Site Settings and billing workspaces, while deeper provider reconciliation/dispute depth still needs work. |
| Orders/Billing | In Progress | Order detail, payments, refunds, invoices, and reconciliation visibility exist, and both the orders list and the detail tabs now have queue-style operational filters for open, payment-issue, fulfillment, refund, and invoice follow-up work; the orders list now exposes quick payment/shipment/invoice actions and is now moving onto the de/en localization baseline, the orders detail shell plus payment/refund/invoice/shipment tab grids are now also moving onto that same bilingual path, the billing payments queue exposes direct invoice/order/customer follow-up actions, the billing payments/refunds entry queues are now also moving onto the same bilingual baseline, webhook/payment-detail surfaces are now following that same bilingual path, the payment editor itself is now also moving onto the bilingual path so reconciliation/dispute work does not drop back to English-only editing, and remaining order-billing not-found/linkability failures plus shipment-creation not-found feedback now also route through `ValidationResource*.resx`, while expense rows expose supplier follow-up, financial-account rows deep-link into journal-entry follow-up, financial accounts / journal entries now expose lightweight account-type and recent-or-complex queue filters, lower-traffic order detail grids now also keep pager and editor/config handoff links inside HTMX-safe shells, and phase-1 Stripe/VAT readiness is now surfaced from both Site Settings and billing workspaces, while deeper provider reconciliation/dispute depth still needs work. |
| CRM | In Progress | Customers, leads, opportunities, interactions, segments, and invoice workflows exist; customer, lead, opportunity, and invoice queues now expose quick follow-up actions like linked-user review, interaction/segment deep-links, lead conversion, customer/order/payment deep-links, prefilled opportunity creation, common invoice status transitions, explicit B2C/B2B customer tax profiling with VAT ID visibility, locale-source and platform-fallback visibility for customer follow-up, and VAT-aware invoice context with net/tax/gross splits plus live tax-policy visibility, while the main CRM overview/lead/opportunity/invoice workspaces and the remaining invoice-editor order/customer remediation links now stay inside HTMX-safe shell navigation instead of falling back to older full-page routes; CRM overview, customer queue, lead/opportunity/invoice workbenches, the segment queue, and the main customer/lead/opportunity/invoice editor shells are now also moving onto the de/en localization baseline, while deeper reporting and support depth still need improvement. |
| CRM | In Progress | Customers, leads, opportunities, interactions, segments, and invoice workflows exist; customer, lead, opportunity, and invoice queues now expose quick follow-up actions like linked-user review, interaction/segment deep-links, lead conversion, customer/order/payment deep-links, prefilled opportunity creation, common invoice status transitions, explicit B2C/B2B customer tax profiling with VAT ID visibility, locale-source and platform-fallback visibility for customer follow-up, and VAT-aware invoice context with net/tax/gross splits plus live tax-policy visibility, while the main CRM overview/lead/opportunity/invoice workspaces and the remaining invoice-editor order/customer remediation links now stay inside HTMX-safe shell navigation instead of falling back to older full-page routes; CRM overview, customer queue, lead/opportunity/invoice workbenches, the segment queue, the main customer/lead/opportunity/invoice editor shells, several common help/dynamic-form texts, the main customer editor metadata, and CRM engagement/customer-lead/opportunity handler feedback now also move through the shared `resx` localization path, while deeper reporting and support depth still need improvement. |
| Loyalty | In Progress | WebAdmin now covers loyalty programs, reward tiers, member accounts, admin-side account provisioning, manual point adjustments, account suspend/activate, redemption troubleshooting/confirmation, business campaigns, and recent scan-session diagnostics; the admin dashboard now also surfaces loyalty counts and direct follow-up links, and the current-user loyalty-business query now also routes its missing-user guardrail through the shared validation-resource path, so the main near-term gap is deeper mobile diagnostics rather than basic loyalty operations. |
| Inventory/Procurement | In Progress | Warehouses, suppliers, stock, transfers, purchase orders, and ledger views exist, and warehouse, supplier, purchase-order, stock-level, stock-transfer, and ledger screens now have queue-style operational filters or direct troubleshooting links for setup, replenishment, and stock-attention work; supplier rows now deep-link into purchase-order follow-up, warehouse rows deep-link into scoped stock-level review, the same lower-traffic inventory workspaces now also render through HTMX-aware shell helpers with in-shell search/filter/pager/editor flows, manual stock adjustment, reservation, reservation-release, and return-receipt flows are now exposed from stock levels, and the warehouse/stock-level plus supplier/purchase-order/stock-transfer queues, inventory ledger, and the main warehouse/supplier/stock-transfer editor shells are now moving onto the de/en localization baseline, while richer exception and structured receiving workflows still need work. |
| Identity/Admin Support | In Progress | Users, roles, permissions, and core admin identity flows exist; the users workspace now also exposes queue-style lifecycle filters and quick support actions for unconfirmed, locked, inactive, and mobile-linked accounts, but broader invite/activation/support depth still needs audit. |
| Media | In Progress | Media library exists and now supports content-ops queue filters plus direct file-open, missing-alt, and missing-title follow-up actions, along with summary cards and media-ops playbooks; deeper reference tracking and purge workflows are still later work. |
| Settings | In Progress | Global site settings exist and business onboarding now has a dedicated setup workspace; business-app legal/billing handoff URLs plus phase-1 Stripe/DHL provider settings and VAT/invoice-issuer defaults are now manageable from site settings, and site settings now also drive a real `de-DE` / `en-US` localization baseline for WebAdmin with supported-cultures governance, culture switching, shared-resource-backed display metadata for the main settings form, a platform-level `AdminTextOverridesJson` override surface for wording changes without source edits, and shared application-validator localization for the highest-traffic settings rules; that validator path now also covers orders, loyalty, inventory, cart/checkout, cart-summary pricing/tax/currency guardrails, storefront checkout/payment confirmation, add-on pricing, pricing, shipping, SEO uniqueness, and selected catalog/CMS/business/onboarding business-rule feedback, but full tenant-aware settings architecture still needs domain and UI expansion. |
| Mobile Operations | In Progress | WebAdmin now includes a dedicated mobile-operations workspace for JWT/mobile bootstrap, onboarding/support dependency counts, communication readiness, device-fleet diagnostics, app-version visibility, device-level filtering for stale installs, missing push tokens, notification-disabled devices, and business-member devices, plus lightweight remediation actions like push-token clearing and device deactivation; the workspace now also stays inside HTMX-safe navigation and remediation flows, so future work is mainly deeper telemetry or richer remediation rather than basic visibility. |
| Communication Management | In Progress | Business communications now have HTMX-safe workspace/profile/audit flows, template and retry visibility matrices, flow-specific playbooks, safe test-email/SMS/WhatsApp reruns, recipient-aware handoffs into invitation/user/business support surfaces, delivery age/latency/severity context, controlled generic retry for failed or pending invitation/activation/password-reset rows after safe target resolution, policy-aware retry state (`ready`, `cooldown`, `rate-limited`, blocked reason, retry-available timing, recent-chain volume), first-class `repeated failures` / `prior success context` triage, exact-chain exploration for one recipient/flow/business path, canonical phone-verification support over SMS or WhatsApp with customizable text templates, a persisted SMS/WhatsApp audit baseline for live verification and operator diagnostics, live non-email message-family views in the workspace/profile and exact-chain surfaces that expose effective template text, rendered sample preview, supported tokens, target path, policy notes, safe-usage guidance, rollout boundaries, direct settings handoff, and safe family-level diagnostic reruns, plus `Heavy Chains`, provider-lane triage, provider recovery state, and provider-guided action depth for noisy SMS/WhatsApp lanes; email-audit and non-email-audit detail surfaces are now also moving onto the de/en localization baseline, while deeper delivery-log infrastructure, broader outbox/provider-event modeling, and richer template CRUD remain near-term. |
| Shipping Operations | In Progress | Generic order shipment visibility exists, and WebAdmin now exposes both shipping-method configuration with queue filters/editable rate tiers and a cross-order shipment queue for packing, tracking, carrier-review, and return-follow-up work; shipping-method maintenance, shipment creation, and part of the related not-found/concurrency feedback now also move through the shared validation-resource path, while deeper DHL carrier lifecycle and full RMA completion still remain near-term. |

## 6. HTMX Conventions

HTMX is the default pattern for server-driven interaction.

### Use `hx-get` for

- fragment loading
- tab/section refresh
- partial list updates
- detail sub-panels

### Use `hx-post` for

- form submissions
- modal commands
- partial section replacement
- operator workflows where the page should not fully reload

### Keep server responses authoritative

- validation messages stay server-rendered
- alerts stay server-rendered
- list rows and partial grids stay server-rendered
- concurrency and permission results come back from controller/application responses, not client guesses

### Keep anti-forgery and Bootstrap hooks centralized

- HTMX requests must carry ASP.NET Core anti-forgery
- swapped fragments must reinitialize Bootstrap affordances
- do not scatter one-off client-side refresh code when the layout can coordinate it centrally

### Standard interaction pattern

- queue/list screens should render as a `workspace shell`
- create/edit/assignment flows should render as an `editor shell`
- GET entry points should be HTMX-aware, not only POST validation rerenders
- successful POSTs should prefer `RedirectOrHtmx`-style return paths so operators stay inside the current workflow whenever possible

## 7. Business Onboarding Workflows

Business onboarding is a near-term operational requirement and should be documented as a real workflow, not only entity CRUD.

Target workflow:

1. create business
2. provision tenant/customer context as needed
3. assign owner/admin user
4. trigger invitation or activation
5. complete initial defaults and setup
6. move through activation/approval states
7. support suspension/deactivation/reactivation

### Current state

- `In Progress`: business creation, owner assignment, member management, location management, and invitation/resend/revoke now exist in WebAdmin
- `Completed`: business onboarding shells now surface direct next actions for missing owner, location, invitation, and profile-completion steps instead of only passive checklist warnings
- `Completed`: the business list now supports operational-status and needs-attention filtering, making approval and setup queues easier for operators to process
- `Completed`: a dedicated business setup workspace now groups profile, localization/defaults, onboarding shortcuts, and phase-1 settings dependencies instead of forcing operators to infer setup from scattered screens
- `Completed`: the setup workspace now also surfaces inline previews of members needing activation/lockout support and open invitations, so operators can troubleshoot onboarding from one place
- `Completed foundation`: business setup now persists business-level branding, localization defaults, time zone, and phase-1 communication toggles/defaults instead of treating all setup as a thin wrapper around global settings
- `Completed foundation`: the setup workspace now also shows the current business subscription snapshot so support/admin can confirm plan/provider/renewal context without leaving WebAdmin
- `Completed foundation`: WebAdmin now also has a dedicated business-subscription workspace with current plan state, active-plan readiness, external management-website handoff visibility, and FullAdmin cancel-at-period-end control
- `Completed foundation`: the same subscription workspace now also surfaces provider invoice history with queue-style triage for open/paid/draft/uncollectible cases, hosted/PDF visibility, and direct payment follow-up links so finance support is not trapped in Stripe or external billing pages
- `Completed foundation`: the same subscription invoice workspace now also supports `Overdue` and `PDF Missing` triage plus a direct refund-queue handoff, so subscription billing support can separate collection follow-up from provider-document gaps inside WebAdmin
- `Completed foundation`: the business subscription workspace and its invoice queue now also support HTMX-driven load, filtering, and cancel-at-period-end update paths, so subscription support no longer falls back to older full-page refresh behavior
- `Completed foundation`: the same subscription invoice queue now also supports HTMX-driven quick triage subsets for open, overdue, PDF-missing, Stripe, and uncollectible invoice cases, so monthly billing support can jump directly into the right subset without rebuilding filters manually
- `Completed foundation`: the main business-subscription workspace now also exposes direct triage handoff actions for open, overdue, PDF-missing, and uncollectible invoice queues, so monthly billing support can jump from summary signals into the right follow-up queue immediately
- `Completed foundation`: navigation between the main business-subscription workspace and the subscription-invoice queue now also stays inside the HTMX workflow, so billing triage can move between summary and invoice-detail queues without dropping back to older full-page navigation
- `Completed foundation`: business-subscription and subscription-invoice workspaces now also hand off internally to business edit/setup, payments, refunds, and payment/site-setting review through HTMX-aware routes, so monthly-contract support no longer mixes fragment-driven billing triage with older full-page admin detours
- `Completed foundation`: the payments workspace now also opens record/edit payment flows through HTMX-aware entry paths, and the payment editor now returns through the same fragment workflow, so finance support no longer drops back to older full-page payment-maintenance routes
- `Completed foundation`: subscription-plan handoff links are now business-aware and plan-aware, so admins can start or upgrade a business against a specific monthly plan from WebAdmin instead of only opening a generic external billing website
- `Completed`: invitation acceptance is now available in `Darwin.Mobile.Business` as the current phase-1 business-user onboarding path
- `Decision made`: phase-1 owner onboarding supports both assigning an existing platform user and invitation-first owner creation
- `Completed foundation`: invitation issue/resend emails can now carry both the manual token and a configurable magic-link path
- `Planned / Near-term`: harden the current config-driven magic-link path into fully verified app-link handling if production mobile onboarding needs it
- `Completed`: approval, suspension, and reactivation actions now exist in WebAdmin, together with a readiness checklist for owner, primary location, contact email, and legal-name completion
- `Completed`: approval decisions now have operational impact because `Darwin.Mobile.Business` uses a phase-1 soft gate against the business access-state API
- `Completed foundation`: delegated business-support operators can now access business listing, member-support, and invitation workflows through a dedicated permission path, while business lifecycle and ownership-sensitive actions remain FullAdmin-only
- `Completed foundation`: dashboard, user-role assignment, and permission listing screens now call out the delegated business-support role/permission explicitly, making support access assignment operationally discoverable
- `Completed foundation`: the roles and permissions workspaces now also expose queue-style filters, live summary cards, and small ops playbooks for system/custom/delegated-support review, reducing access-governance cleanup to less than a flat list scan
- `Completed foundation`: the admin dashboard now exposes a business-support queue with attention, approval, invitation, activation, and lockout counts, and the business member/invitation screens now support queue-oriented filters so support operators can jump directly into pending-activation or open-invitation work
- `Completed foundation`: the businesses index now includes quick queue shortcuts for needs-attention, pending-approval, and suspended businesses, which cuts down operator filter setup during onboarding/support review
- `Completed foundation`: the dashboard communication-operations card and business-support queue card are now HTMX-refreshable partials, so operators can refresh live summaries without reloading the full admin dashboard
- `Completed foundation`: WebAdmin now also includes a dedicated `Business Support Queue` workspace that combines attention businesses with recent failed invitation/activation/password-reset emails, reducing page-hopping during onboarding and support triage
- `Completed foundation`: that support queue is now broken into HTMX-refreshable summary, attention-business, and failed-email fragments, so operators can refresh live triage data without a full page reload
- `Completed foundation`: WebAdmin now also exposes a dedicated `Merchant Readiness` workspace that combines attention businesses, setup gaps, lifecycle state, and subscription/billing handoffs, so merchant operations can triage onboarding-to-go-live readiness from one queue instead of stitching together support, setup, and subscription screens manually
- `Completed foundation`: the `Merchant Readiness` workspace now also hands off directly into locations, business communication profile, and business-scoped payments, so merchant triage can move from setup/subscription readiness into fulfillment, communication, and billing follow-up without reopening the broader business workspace first
- `Completed foundation`: the same `Merchant Readiness` workspace now also hands off directly into subscription-invoice triage and global tax-compliance review, so merchant follow-up can move from readiness into billing-collection and compliance lanes without reopening separate admin entry points first
- `Completed foundation`: the same `Merchant Readiness` workspace now also hands off directly into business edit and invitation remediation, so merchant ops can move from readiness triage into profile cleanup and onboarding-follow-up without reopening the broader business queue first
- `Completed foundation`: the same `Merchant Readiness` workspace now also hands off directly into owner-override audits and support-queue escalation, so merchant governance follow-up and support routing can start from the readiness queue instead of a separate business workspace detour
- `Completed foundation`: the `Merchant Readiness` playbooks are now actionable handoffs instead of passive guidance rows, so approval, setup, and billing operators can jump from the playbook table directly into the exact queue or remediation lane they need
- `Completed foundation`: the `Merchant Readiness` summary cards are now actionable queue handoffs rather than read-only counts, so operators can jump from top-level merchant signals straight into business/support triage without first scanning the readiness grid
- `Completed foundation`: those `Merchant Readiness` summary handoffs now also open the correct merchant subsets for pending-approval and suspended businesses instead of only generic queue entry points, so top-level merchant triage is more precise
- `Completed foundation`: the business subscription workspace now also turns its billing playbooks into direct queue and remediation handoffs for site-settings setup, invoice review, payments, and support escalation, so subscription follow-up no longer depends on passive guidance rows.
- `Completed foundation`: the business-members workspace now also exposes member-ops summary cards plus direct playbook handoffs for pending activation, locked accounts, and owner-coverage gaps, so merchant support can start member remediation from top-level signals instead of a plain table and inline warnings.
- `Completed foundation`: the business-invitations workspace now also exposes invitation-ops summary cards plus direct playbook handoffs for open, pending, and expired invite lanes, so onboarding follow-up can start from top-level signals instead of a plain invitation table.
- `Completed foundation`: the owner-override audit workspace now also exposes governance playbooks plus direct drill-ins into affected users and the business members lane, so last-owner override review is no longer a passive audit log.
- `Completed foundation`: the `Merchant Readiness` row signals for pending invites, missing owner, and missing primary location now also hand off directly into invitation/member/location remediation, so merchant follow-up can start from the flagged signal itself instead of only from the row action cluster
- `Completed foundation`: the same `Merchant Readiness` row signals for missing contact email, missing legal name, and no active subscription now also hand off directly into profile/subscription remediation, so merchant profile and commercial gaps can be worked from the flagged signal itself
- `Completed foundation`: the `Merchant Readiness` subscription cell now also turns live plan/status, cancel-at-period-end, and current-period-end signals into direct subscription or invoice-lane handoffs, so commercial follow-up can start from the flagged subscription signal itself instead of only from the generic row action cluster
- `Completed foundation`: the `Merchant Readiness` status and setup cells now also turn pending-approval, suspended, active, and setup-missing signals into direct queue or setup-lane handoffs, so lifecycle and readiness remediation can start from the flagged signal itself instead of only from summary cards or the generic row action cluster
- `Completed foundation`: the `Merchant Readiness` row action cluster now also hands off directly into business-scoped refunds and financial accounts, so merchant billing follow-up can move from readiness into refund or accounting lanes without detouring through the broader billing workspace first
- `Completed foundation`: the same `Merchant Readiness` row action cluster now also hands off directly into business-scoped expenses and journal entries, so merchant finance follow-up can move from readiness into accounting-detail lanes without detouring through broader billing navigation first
- `Completed foundation`: the `Merchant Readiness` business identity block now also hands off directly into business profile editing, so merchant name and legal-name cleanup can start from the row header itself instead of only from the action cluster
- `Completed foundation`: the `Merchant Readiness` row action cluster now also hands off directly into business-scoped email audits and SMS/WhatsApp audits, so merchant communication follow-up can move from readiness into live audit lanes without detouring through the broader communications workspace first
- `Completed foundation`: the `Merchant Readiness` row action cluster now also hands off directly into `Tax Compliance`, so merchant commercial follow-up can jump from a flagged tenant straight into the compliance workspace instead of relying only on the page-level global header action
- `Completed foundation`: the business queue now supports a dedicated `MissingOwner` readiness subset, and the `Merchant Readiness` summary cards for missing-owner, pending-activation, and locked-member signals now jump into that precise business or user lane instead of the broad support queue
- `Completed foundation`: the business queue now also supports dedicated readiness subsets for missing primary location, missing contact email, missing legal name, and pending invites, and the queue's own attention labels now hand off into those precise subsets instead of staying read-only merchant warnings
- `Completed foundation`: `Merchant Readiness` now also surfaces summary cards for missing primary location, missing contact email, missing legal name, and pending invites, so those readiness gaps can be triaged from top-level merchant summaries instead of only from the grid or the broader business queue
- `Completed foundation`: the main `Businesses` workspace now also surfaces top-level readiness summary cards for attention, approval, suspension, missing owner, missing primary location, and pending invites, so merchant triage no longer depends only on filter chips plus table scanning
- `Completed foundation`: the main `Businesses` workspace now also surfaces top-level profile-gap summary cards for missing contact email and missing legal name, so merchant profile cleanup can start from workspace summaries instead of only from readiness chips and table labels
- `Completed foundation`: the main `Businesses` workspace now also surfaces top-level member-account summary cards for pending activation and locked members, so merchant-linked identity remediation can start from the business entry workspace instead of only from `Merchant Readiness` or separate user queues
- `Completed foundation`: those same `Businesses` summary cards now also expose direct remediation pivots into merchant readiness, failed invitation/activation/password-reset audits, payments, and mobile operations instead of staying queue-only, so top-level merchant triage can jump straight from summary signals into the next operational lane
- `Completed foundation`: the main `Businesses` workspace now also exposes the approval/setup/billing playbook table with direct queue and follow-up handoffs, so the primary merchant queue is no longer summary-and-grid only when operators need a structured remediation starting point
- `Completed foundation`: the `Business Support Queue` now also exposes the approval/setup/billing playbook table with direct queue and follow-up handoffs, so support operators have the same structured merchant-remediation entry points without bouncing out to other business workspaces first
- `Completed foundation`: the business-locations workspace now also turns its summary cards and location-readiness playbooks into direct filtered queue handoffs, so location triage no longer depends on scanning the full list or reading passive guidance text before acting
- `Completed foundation`: the business subscription-invoices workspace now also turns its summary cards and billing playbooks into direct filtered invoice-queue handoffs, so collections and document-hygiene follow-up can start from the signal itself instead of static guidance text
- `Completed foundation`: business rows in the main `Businesses` workspace now also hand off directly into business-scoped communications and payments, so merchant follow-up from the main queue no longer depends on detouring through `Merchant Readiness` just to reach those operational lanes
- `Completed foundation`: business rows in the main `Businesses` workspace now also hand off directly into subscription invoices, email audits, SMS/WhatsApp audits, refunds, and owner-override audits, so the main merchant queue is much closer to the deeper triage surface previously available only from `Merchant Readiness`
- `Completed foundation`: business rows in the main `Businesses` workspace now also hand off directly into financial accounts, expenses, journal entries, and tax compliance, so the main merchant queue now reaches the deeper finance/compliance lanes instead of leaving that depth exclusive to `Merchant Readiness`
- `Completed foundation`: the main `Businesses` workspace now also makes business identity, status, owner counts, member counts, and location counts directly actionable, so operators can start profile, lifecycle, membership, and location remediation from the core row cells instead of only from the action cluster
- `Completed foundation`: the main `Businesses` workspace and `Merchant Readiness` now also surface a dedicated `ApprovedInactive` subset with actionable summary cards and status badges, so approved-but-inactive merchants no longer collapse into generic `active` or broad attention states during merchant triage
- `Completed foundation`: the `Business Support Queue` now also surfaces dedicated merchant-readiness cards for approved-inactive, missing-location, missing-contact, and missing-legal-name gaps, and its attention rows now turn status and signal labels into direct remediation handoffs instead of leaving support triage broad and mostly read-only
- `Completed foundation`: the failed-email lane inside `Business Support Queue` now also exposes business-scoped edit, communication-profile, invitation/member, and unconfirmed-user handoffs directly from each failure row, so merchant communication remediation no longer depends on detouring through generic audit searches before reaching the operational lane
- `Completed foundation`: the same failed-email lane now also turns recipient addresses into direct user lookups and gives `PasswordReset` failures first-class `Users` and `Locked users` handoffs, so account-recovery triage no longer stops at the audit row when the next step is user-state investigation
- `Completed foundation`: the `Business Support Queue` header now also exposes direct shortcuts into failed invitation, activation, and password-reset email lanes, so support operators can jump from the queue entry workspace straight into the right transactional failure slice without first opening the broad failed-email log
- `Completed foundation`: the attention rows inside `Business Support Queue` now also hand off directly into business edit, locations, communication profile, and payments, so support triage no longer stalls at setup/member/invitation-only actions when the merchant issue actually lives in profile, fulfillment footprint, communications, or billing lanes
- `Completed foundation`: the same `Business Support Queue` attention rows now also hand off directly into subscription, subscription invoices, email audits, SMS/WhatsApp audits, and tax compliance, so support triage can move from an attention merchant straight into the deeper commercial, communications, and compliance lanes instead of detouring through other merchant workspaces first
- `Completed foundation`: the same `Business Support Queue` attention rows now also hand off directly into refunds, financial accounts, expenses, journal entries, and owner-override audits, so support triage now reaches the deeper finance and governance lanes that were previously easier to reach only from `Businesses` or `Merchant Readiness`
- `Completed foundation`: the main `Business Support Queue` links for queue slices, failed-email slices, attention rows, and audit/business drill-ins now consistently preserve HTMX history, so support operators can move across merchant remediation lanes without dropping into fragment dead-ends on back/forward navigation
- `Completed foundation`: the `Business Support Queue` summary now also promotes missing-owner, pending-invite, and locked-member signals into dedicated cards instead of leaving them only in the bottom inline summary, so top-level support triage can start from those operator-relevant counts without scanning mixed text lines
- `Completed foundation`: business-linked failed-email rows inside `Business Support Queue` now also hand off directly into business edit and payments, so support operators can move from a communication failure into profile or billing remediation without first detouring through a separate merchant queue
- `Completed foundation`: the same business-linked failed-email rows now also hand off directly into subscription, subscription invoices, and tax compliance, so merchant communication failures can move straight into commercial and compliance remediation without leaving the support workspace first
- `Completed foundation`: the same `Business Support Queue` summary now also promotes suspended businesses into a dedicated card instead of leaving suspension triage only in the bottom inline summary, so lifecycle-related merchant support can start from a top-level card like the other high-signal support lanes
- `Completed foundation`: the `Business Support Queue` summary cards for pending activation, pending invites, and locked members now also expose secondary remediation links into mobile operations and the matching failed-email slices, so support operators can move from a count card into the likely next operational lane without dropping to the inline summary
- `Completed foundation`: the same `Business Support Queue` summary cards for approved-inactive and suspended businesses now also expose secondary handoffs into payments and failed-email triage, so lifecycle-related merchant support can move from a top-level count straight into the likely next operational lane instead of stopping at a broad queue entry
- `Completed foundation`: the `Business Support Queue` summary cards for missing primary location, missing contact email, and missing legal name now also expose direct remediation-oriented secondary links, so readiness and profile cleanup can start from the count card instead of only from the broad business subset
- `Completed foundation`: the `Business Support Queue` header now also exposes direct shortcuts for approved-inactive, missing-owner, and pending-invite merchant subsets, so support operators can jump into those high-signal merchant slices from the queue entry workspace without first stepping through summary cards
- `Completed foundation`: the same `Business Support Queue` header now also exposes direct shortcuts for missing-primary-location, missing-contact-email, and missing-legal-name merchant subsets, so readiness and profile cleanup slices can be opened from the support entry workspace without first dropping into the summary grid
  - `Completed foundation`: business create/edit, location create/edit, member create/edit, and invitation-create flows now all render through HTMX-aware shell helpers on initial load and validation rerender, so the core onboarding workspace stays aligned with the newer server-rendered + HTMX admin interaction model
- `Completed foundation`: the business setup and business editor shells now also keep their main onboarding, subscription, payments, communications, and site-settings shortcuts inside HTMX-driven navigation paths, so operators no longer bounce between fragment workflows and older full-page admin detours during setup support
- `Completed foundation`: the business members, invitations, and locations workspaces now also keep their main search, triage, edit, invitation-support, lock/reset/activation, and back-navigation flows inside HTMX-driven shells, so subscriber onboarding/support work can stay in one server-rendered workflow instead of bouncing through older full-page routes
- `Completed foundation`: the core business onboarding forms now also return through HTMX-aware cancel/back paths for business, member, location, and invitation editing, so the final maintenance steps inside subscriber onboarding no longer drop back to legacy full-page routes
- `Completed foundation`: the remaining owner-override audit, staff-badge preview, and setup-preview handoffs now also stay inside HTMX-driven shells, so business support can move between setup diagnostics and member/invitation remediation without dropping back to older full-page navigation
- `Completed foundation`: the businesses index and business support queue now also keep their main queue subsets, search/reset, and cross-workspace support handoffs inside HTMX-driven shells, so onboarding/support operators can triage subscriber issues without dropping back to older full-page navigation
- `Completed foundation`: the businesses index and support queue now also keep business-list triage, support-queue summaries, and failed-email remediation handoffs inside HTMX shells, reducing one of the last high-value onboarding/support detours in the subscriber workflow
- `Planned / Near-term`: explicit onboarding state machine, richer setup workspace UX, and tenant/customer provisioning still need completion

## 8. Authentication-Related Admin Support

WebAdmin should support or coordinate:

- invite user
- resend activation email
- forgot-password assistance
- reset-password initiation/support
- lock/unlock account
- role assignment
- audit visibility

### Current state

- `In Progress`: user, role, permission, password, and email-change admin tooling exists
- `In Progress`: invite issuance/reissue/revoke now exists for business onboarding
- `Completed`: WebAdmin now supports admin-triggered password reset email, lock/unlock, email-confirm override, and activation-email resend from the user edit workflow
- `Completed`: the same activation/reset/lock/unlock support actions are now available directly inside the business-member workspace, which removes a major operator detour during onboarding troubleshooting
- `Completed foundation`: the users queue now also supports in-place activation, lock, and unlock actions from list rows, so common identity-support triage no longer has to leave the HTMX workspace to complete routine account-state changes
- `Completed foundation`: user-role assignment now also supports HTMX-aware entry and return paths from both the users queue and the user editor, so access-governance changes can stay inside the newer server-rendered workflow instead of detouring through separate full-page navigation
- `Completed foundation`: the user-role editor now also returns to the user editor through an HTMX-aware back path, so role-assignment follow-up no longer drops back to a legacy full-page route when launched from account maintenance
- `Completed foundation`: the remaining loyalty campaign queue entry/edit paths now also stay inside the HTMX workspace shell, reducing one of the last plain-link detours in the loyalty priority area
- `Completed foundation`: users queue and user-editor navigation now also stay inside HTMX-aware shells for create, edit, change-email, change-password, and back-navigation paths, so identity support no longer mixes fragment-driven triage with older full-page account-maintenance routes
- `Completed foundation`: the same business-member workspace now also exposes staff access badge preview/refresh, so support can mirror the rotating QR badge used in `Darwin.Mobile.Business`
- `Completed foundation`: selective delegation is now enforced in both controller authorization and view affordances, so support operators can work with invitations and member support without inheriting approval, archive, location, or owner-management powers
- `Completed foundation`: user create/edit, change-email, change-password, role-assignment, role create/edit, and permission create/edit flows now all render through HTMX-aware shell helpers on initial load as well as validation rerender, so the access-management workspace stays aligned with the newer server-rendered + HTMX admin interaction model
- `Completed foundation`: the users list workspace now also supports HTMX-aware search, filter, page-size changes, and lifecycle subset navigation, so identity triage no longer falls back to older full-page list refreshes
- `Completed foundation`: the roles and permissions list workspaces now also support HTMX-aware search, filter, and page-size changes, so access-governance triage stays aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: the roles and permissions queues now also open create/edit and permission-maintenance flows through HTMX-aware entry paths, and their back navigation stays inside the same server-rendered workflow instead of bouncing through older full-page routes
- `Completed foundation`: role-permission assignment now also has an HTMX-safe editor shell, so permission-binding updates no longer depend only on the older encoded full-page editor
- `Completed`: the business-member edit flow now supports a controlled `FullAdmin` override for the "last active owner" rule, with mandatory reason capture, explicit danger-state UI, and persisted audit records
- `Completed`: owner-override audit history is now visible inside the business workspace, so sensitive ownership exceptions are reviewable without querying the database directly
- `Completed foundation`: the platform now has public confirm-email token endpoints, so admin activation support is no longer only a placeholder
- `Planned / Near-term`: decide when activation/email-confirm must become an enforced sign-in prerequisite rather than a supported-but-nonblocking lifecycle step

## 9. Site and System Settings Architecture

Settings are a platform concern, not one crowded page.

### Required design direction

Settings should be grouped and scalable. At minimum, the information architecture should support:

- General
- Business Profile
- Localization
- Branding
- Payments
- Shipping
- Notifications / Communications
- Users & Roles
- Security
- Integrations
- Tax / Invoicing
- Advanced / Feature Flags

### UI/UX guidance

Settings UI must be:

- scalable
- discoverable
- tenant-aware
- permission-aware
- easy for operators to navigate
- future-safe as categories expand

### Current state

- `Partial`: a basic settings UI exists
- `Completed foundation`: the global site settings screen is now grouped into operational categories and exposes JWT/security, mobile bootstrap, and soft-delete retention controls instead of hiding those fields behind DTO-only configuration
- `Completed foundation`: business setup now has its own grouped workspace, persists business-level branding/localization/time-zone/communication defaults, and makes the remaining global phase-1 dependencies explicit
- `Completed foundation`: the business setup workspace now also shows communication readiness against global SMTP/SMS/WhatsApp/admin-alert routing, which gives operators immediate visibility into whether saved business preferences can actually be delivered
- `Completed foundation`: the admin dashboard now also exposes a communication-operations snapshot, making global transport readiness and business-level sender/support-email gaps visible before full Communication Core template/log UIs are built
- `Completed foundation`: site settings now also surface business-app legal and billing handoff URLs such as management website, impressum, privacy, business terms, and account deletion, and mobile operations shows whether those handoffs are configured
- `Completed foundation`: site settings now also expose phase-1 Stripe payment credentials/webhook identity and DHL carrier credentials/default shipper identity directly in the admin UI, instead of leaving those settings implicit or config-file-only
- `Completed foundation`: the site-settings ownership matrix and the business-setup ownership workspace now both include HTMX-safe operator handoffs into business support, communication ops, payment ops, shipment ops, and the relevant global fragments, so the platform-vs-tenant split is actionable instead of merely documented
- `Completed foundation`: the localization section in site settings and the localization defaults block in business setup now both explain fallback-vs-tenant ownership and link directly into business and CRM locale review surfaces, so operators can distinguish platform fallback changes from tenant-only overrides before true multilingual infrastructure lands
- `Completed foundation`: CRM invoice review, invoice editing, and order tax snapshots now also surface archive-readiness and e-invoice-baseline indicators derived from VAT and issuer identity completeness, so compliance-adjacent troubleshooting no longer stops at VAT/net-tax-gross visibility alone
- `Completed foundation`: those compliance snapshots now also hand off directly into tax settings, issuer-data cleanup, invoice review, and customer VAT-profile follow-up, so phase-1 tax/compliance support can move from readiness visibility into operational remediation without pretending a full e-invoice workflow exists
- `Completed foundation`: billing now also exposes a dedicated `Tax Compliance` workspace with VAT/issuer readiness, business-customer VAT-gap review, and invoice follow-up lanes, so phase-1 compliance triage has a real operator queue instead of only scattered settings and invoice snippets
- `Planned / Near-term`: settings IA must be restructured before settings sprawl becomes technical debt

## 10. Payment Operations UI

WebAdmin must provide operational payment support.

### Current state

- `Completed foundation`: generic payment list/edit and payment-linked order/invoice visibility exist
- `Completed foundation`: refund and reconciliation visibility exists
- `Completed foundation`: the payments workspace now also supports queue-style triage for pending, failed, refunded, unlinked, and provider-linked payments, which makes operator support less dependent on ad-hoc search before Stripe-specific lifecycle tooling exists
- `Completed foundation`: the payments workspace now also surfaces live queue counts and phase-1 support playbooks, so operators can move from payment triage into an explicit follow-up path without leaving the workspace
- `Completed foundation`: the global settings UI now also exposes Stripe enablement, publishable/secret/webhook credentials, and merchant display identity needed for phase-1 provider operations
- `Completed foundation`: the payments workspace now also shows a Stripe readiness panel with direct links into payment settings, so configuration gaps are visible from the same operational surface where support triages payments
- `Completed foundation`: the same workspace now also supports Stripe-specific queue subsets for Stripe rows, failed-Stripe cases, and missing provider references, with richer created/paid/failure context per row so phase-1 provider triage is not trapped inside generic payment lists
- `Completed foundation`: the payment editor now also includes lifecycle cards, failure visibility, refund timeline, and Stripe-aware support playbooks, so payment operators can troubleshoot one payment end-to-end without falling back to only the queue row or the linked order
- `Completed foundation`: payment queue rows, order-payment rows, and the payment editor now also deep-link directly into the refund-create workflow with the payment preselected, so support can move from payment troubleshooting into refund recording without manually re-navigating the full order flow
- `Completed foundation`: WebAdmin now also includes a dedicated cross-order refund queue with pending/completed/failed/Stripe subsets, live counts, and direct links back into the linked payment and order workflows, so finance support is no longer trapped inside individual order tabs
- `Completed foundation`: that refund queue now also exposes a `Needs Support` subset and summary signal that combines pending, failed, and Stripe-reference-light refund rows, so finance triage no longer depends on manually combining multiple refund subsets
- `Completed foundation`: the same payment workspace now also surfaces webhook lifecycle visibility and a dedicated webhook queue for subscription/delivery callback history, so Stripe-first finance support can inspect callback drift and retry-pending signals before treating queue rows as reconciled
- `Completed foundation`: the billing webhook queue now also behaves as an HTMX-aware workspace, with filter/reset, queue subsets, and returns to payments/settings staying inside the same fragment workflow, so payment-support triage no longer mixes modern queue handoff with older full-page callback navigation
- `In Progress`: payment operations are now more Stripe-aware for support triage, but webhook lifecycle history, reconciliation, and dispute-depth are still near-term work

### Required near-term capabilities

- payment list
- payment detail
- provider references
- status history
- refund action/support
- reconciliation-state visibility
- dispute visibility
- order/invoice linkage
- webhook/callback audit visibility

### Phase-1 provider direction

WebAdmin payment operations should optimize for Stripe first. Additional providers are later-phase work.

## 11. Shipping Operations UI

WebAdmin must provide shipping visibility and operator support.

### Current state

- `Completed foundation`: shipments are visible through order-related flows, shipping methods now have a dedicated WebAdmin module with list/create/edit, queue filters, and editable rate tiers, and operators now also have a cross-order shipment queue for packing and hand-off review
- `Completed foundation`: that shipment queue now also includes missing-tracking and returned subsets, which makes generic carrier follow-up and return-review workflows more actionable before dedicated DHL-first exception tooling exists
- `Completed foundation`: the shipment queue now also surfaces live operational counts and support playbooks for pending, missing-tracking, and returned work, so operators have a guided phase-1 follow-up path before deeper DHL exception tooling lands
- `Completed foundation`: the global settings UI now also exposes DHL enablement, environment/API credentials, account number, and default shipper identity, so phase-1 carrier configuration is centrally manageable
- `Completed foundation`: the shipment queue now also shows a DHL readiness panel with direct links into shipping settings, so carrier-setup gaps are visible from the same workspace used for shipment follow-up
- `Completed foundation`: the shipment queue now also supports DHL-only and missing-service subsets, carries forward the configured DHL environment, and flags rows needing carrier review, so phase-1 DHL triage is more explicit from the main queue
- `Planned / Near-term`: dedicated DHL-first shipment operations, tracking, and exception handling still need to be strengthened

### Required near-term capabilities

- shipment list
- shipment detail
- tracking timeline
- label info
- delivery exceptions
- return shipment / return request visibility
- manual intervention/support actions

### Phase-1 provider direction

Shipping operations should optimize for DHL first. Additional carriers are later-phase work.

## 12. Loyalty and Mobile Operations

Loyalty and mobile support are now part of the operational scope of WebAdmin, not only backend/application code.

### Loyalty coverage now in WebAdmin

- loyalty program list/create/edit/delete
- reward-tier list/create/edit/delete
- loyalty account queue with search and status filters
- admin-side loyalty account creation for support-led provisioning
- loyalty account detail with recent transactions and redemptions
- manual points adjustment
- account suspend / activate
- redemption workspace with pending/completed/cancelled filters and direct confirm actions for pending items
- business campaign list/create/edit/activate/deactivate
- recent scan-session diagnostics for business-mobile QR flows

### Why this matters

`Darwin.Mobile.Business` already uses reward configuration, campaigns, and scan-session flows. WebAdmin therefore needs enough surface to let operators:

- configure the loyalty program itself
- manage business-facing campaign content and activation
- inspect member account state and intervene with manual adjustments
- review recent scan/accrual/redemption session behavior when support issues happen

### Remaining near-term loyalty/mobile gaps

- deeper mobile remediation or telemetry only if support requirements justify it, such as push-delivery diagnostics, per-device remediation actions, or richer scanner/session failure correlation
- mobile diagnostics are now also on the shared de/en resource path, loyalty/mobile handoff links are first-class inside the workspace, and operator playbooks now explicitly connect push-token debt, scan-session follow-up, and transport-readiness review before support escalates app-only incidents

### Current mobile-operations coverage

The dedicated `Mobile Operations` workspace currently provides:

- JWT and device-binding bootstrap visibility
- QR refresh and mobile outbox bootstrap settings
- onboarding/support dependency counts that affect business-mobile usage
- transport-readiness visibility for email/SMS/WhatsApp/admin alerts
- device-fleet snapshot across active installs
- app-version visibility by platform
- pageable device list with filters for stale installs, missing push tokens, notifications-disabled devices, and business-member devices
- lightweight remediation actions for support: clear push token and deactivate device
- admin-side staff access badge preview/refresh for business members when support needs to mirror the mobile QR badge
- direct links into support queue, site settings, and scan-session review
- row-level handoffs from device diagnostics into loyalty accounts, loyalty scan sessions, and communication provider review when app, scan, and transport incidents overlap
- mobile-used bootstrap validation and misconfiguration feedback now also route through the shared application validation resources instead of raw English-only failure text

The admin dashboard now also exposes a compact loyalty/mobile snapshot so operators can jump directly into loyalty accounts, pending redemptions, scan sessions, and device diagnostics from the landing page instead of relying only on sidebar navigation.

This is intentionally an operational workspace, not yet a full mobile observability and remediation suite.

The identity support chain now also hands mobile-linked users directly into `Mobile Operations` and loyalty account review from the main user queue, so account remediation does not have to rediscover the same person through a separate device-only workspace first.

## 13. Communication Management UI

Communication is a platform capability and must be visible in WebAdmin.

### Required capabilities

- email template management
- notification template management
- communication logs
- resend/retry actions where applicable
- delivery status visibility
- per-business communication settings

### Current state

- `In Progress`: platform communication management now supports operationally safe phase-1 email troubleshooting plus provider-backed SMS/WhatsApp transport tests with customizable diagnostic templates and canonical SMS/WhatsApp phone verification with persisted preferred-channel/fallback policy and customizable text templates, but is not yet complete enough for a full multi-channel template/delivery-log platform
- `Completed foundation`: business setup now exposes a readiness summary that compares business-level communication preferences with global transport configuration, reducing onboarding ambiguity before full template/logging management exists
- `Completed foundation`: dashboard-level communication-operations metrics now expose transport readiness and business communication-default gaps, giving operators an earlier signal before delivery failures become support tickets
- `Completed foundation`: a dedicated `Business Communications` workspace now gives operators a read-only operational queue for businesses missing support-email or sender defaults, with direct links back into business setup and global transport settings
- `Completed foundation`: that workspace now also includes a pageable business queue, so operators can work communication setup debt as an explicit list instead of relying only on dashboard counts
- `Completed foundation`: the workspace also catalogs the currently live transactional email flows and labels them correctly as hard-coded phase-1 compositions, which reduces confusion before the real Communication Core template/log implementation lands
- `Completed foundation`: the workspace now also supports operator queue filters for missing support-email, missing sender identity, and policy-enabled subsets, making communication debt triage more actionable
- `Completed foundation`: current SMTP-based transactional emails now write `EmailDispatchAudit` records, and the workspace surfaces recent delivery attempts/failures as a stopgap audit trail before the full Communication Core logging pipeline exists
- `Completed foundation`: the same workspace now includes a full email audit-log page with search and status filters, which turns delivery visibility into an operator workflow rather than a dashboard-only preview
- `Completed foundation`: each queued business can now be opened into a communication-profile detail screen that combines business defaults, global transport dependency state, current phase-1 flow implications, and support/onboarding signals in one troubleshooting view
- `Completed foundation`: site settings now also expose transactional email subject-prefix and test-inbox reroute controls, and those controls are visible inside the communication workspace so operators can see when phase-1 invitation/activation/password-reset emails are being prefixed or rerouted
- `Completed foundation`: site settings now also expose editable transactional email subject/body templates for business invitation, account activation, and password-reset flows, and those phase-1 flows now consume the configured templates with placeholder rendering instead of remaining fully hard-coded
- `Completed foundation`: the same workspace now also exposes an operator-safe test-email action that only targets the configured communication test inbox, so SMTP readiness and reroute policy can be validated from the admin UI without emailing real customers
- `Completed foundation`: site settings now also expose phase-1 SMS and WhatsApp test recipients plus customizable phone-verification text templates, and the communication workspace now supports provider-backed SMS/WhatsApp transport test sends rather than visibility-only rollout placeholders
- `Completed foundation`: phase-1 email delivery audits now also store flow classification and optional business correlation, so operators can distinguish invitation, activation, and password-reset email failures without waiting for the full Communication Core delivery model
- `Completed foundation`: the workspace now also includes an explicit capability-coverage matrix for template management, retry/resend, and delivery visibility, so operators are not misled into assuming Communication Core features already exist when they do not
- `Completed foundation`: the same workspace now also exposes a template-inventory matrix and a resend/retry policy matrix, so operators can see which flows still use hard-coded composition, what support action is safe today, and where a generic replay engine still does not exist
- `Completed foundation`: each business communication-profile screen now also surfaces recommended next actions and recent business-scoped email activity, so operators can move from diagnosis into support/setup action without leaving the communication workspace
- `Completed foundation`: the business communication-profile screen now also carries a local template-inventory snapshot and resend/retry policy snapshot, so operators can keep flow-control context while troubleshooting one business instead of bouncing back to the top-level communication workspace
- `Completed foundation`: the email audit-log now also includes flow-specific operator playbooks, failed-flow quick filters, and business-linked shortcuts, which makes invitation/activation/password-reset failures actionable without pretending a generic retry queue already exists
- `Completed foundation`: the email-audit queue now also supports controlled generic retry for failed or pending invitation, activation, and password-reset flows after safe live-target resolution, so operators can recover common transactional email failures without pretending a blind replay engine exists
- `Completed foundation`: the communication workspace, business profile, and email-audit queue now also expose prior-attempt counts, repeated-failure chains, and last-success context for the same flow/recipient/business path, so delivery triage can distinguish isolated failures from recurring operational debt before a fuller outbox/delivery-history model exists
- `Completed foundation`: the email-audit queue now also exposes `repeated failures` and `prior success context` as first-class subset filters and summary cards, so operators can work recurring delivery debt as a queue instead of only inferring it from per-row context
- `Completed foundation`: the same email-audit queue now also exposes `Retry Ready` and `Retry Blocked` policy subsets, cooldown/rate-limit reasons, retry-available timing, and recent 24-hour chain-attempt volume, so support can tell when replay is safe, when it should wait, and when it should stop before duplicate-email storms degrade trust
- `Completed foundation`: the same queue now also exposes `Heavy Chains` triage together with chain span and status-mix context for the same flow/recipient/business path, so operators can separate one-off incidents from recurring delivery instability before a fuller provider-event and outbox model exists
- `Completed foundation`: exact-chain exploration in the email-audit queue now also exposes a chain-level summary for one recipient/flow/business path, including total attempts, fail/sent/pending mix, first/last attempt timing, last-success timing, and follow-up volume, so operators can review one delivery incident as a coherent chain instead of assembling that context manually
- `Completed foundation`: exact-chain exploration now also includes a recent delivery-history preview for the same recipient/flow/business path, so operators can read the last few attempt outcomes, provider path, and failure messages without reconstructing the incident manually from the paged queue
- `Completed foundation`: exact-chain exploration now also supports chain-local `Needs Follow-up` and `Resolved History` navigation, so operators can separate unresolved incidents from already-settled attempts without dropping back to broad queue filters
- `Completed foundation`: the Business Communications workspace and business-profile troubleshooting flow now also expose a cross-channel truth matrix for Email, SMS, and WhatsApp, so operators can see which flows are truly live, which actions are safe today, and where the platform intentionally still stops short of a generic multi-channel replay bus
- `Completed foundation`: the phase-1 communication flow catalog now also treats SMS and WhatsApp test-send paths as live operator validation flows rather than future-only placeholders, which keeps rollout expectations aligned with the actual provider-backed capability already in the system
- `Completed foundation`: phone verification now also has a persisted platform channel policy with preferred channel and optional SMS/WhatsApp fallback, so Web and Mobile can rely on one backend-owned verification rule instead of inventing client-side channel selection behavior
- `Completed foundation`: live SMS and WhatsApp sends for phone verification and operator transport tests now also write persisted `ChannelDispatchAudit` rows, and those rows are surfaced in the communication workspace and business profile as a multi-channel history baseline, so support can inspect non-email delivery behavior instead of treating SMS/WhatsApp as opaque transports
- `Completed foundation`: the multi-channel audit baseline now also includes a dedicated `SMS / WhatsApp Audits` workspace with queue filters and business-scoped drill-in, so non-email delivery incidents can be worked as an operator list rather than only as summary cards
- `Completed foundation`: that same `SMS / WhatsApp Audits` workspace now also supports exact-chain exploration for one recipient/flow/business path with `Needs Follow-up` and `Resolved History` slices, so phone-verification and transport-test incidents can be diagnosed as chains the same way email incidents now are
- `Completed foundation`: those same non-email exact-chain views now also deep-link into verification policy, SMS/WhatsApp settings, message-template surfaces, and safe admin-test reruns without dropping the operator out of the incident queue, so SMS and WhatsApp support is now operationally actionable instead of dashboard-only
- `Completed foundation`: the same non-email audit queue now also highlights `Repeated Failures` and `Prior Success Context` with per-row prior-attempt, prior-failure, and last-success signals, so operators can separate recurring transport or verification instability from one-off failures before escalating
- `Completed foundation`: the same non-email audit queue now also exposes `Provider Review` triage with 24-hour provider-lane attempt/failure pressure on each row, so operators can distinguish recipient-specific incidents from transport/provider instability before a fuller provider-event model exists
- `Completed foundation`: that same non-email queue now also supports provider-lane drill-in with a provider-scoped 24-hour summary and direct provider-lane navigation from each row, so SMS and WhatsApp provider issues can be worked as lane incidents instead of only inferred from row-level pressure signals
- `Completed foundation`: provider-lane drill-in now also includes a recommended next step and escalation rule per provider lane, so operators can move from transport pressure visibility into the right configuration or policy action without inventing their own provider playbook
- `Completed foundation`: provider-lane review now also includes direct action handoffs into verification policy, SMS/WhatsApp settings, test-target setup, and safe provider-scoped diagnostic reruns while preserving queue context, so transport pressure can be worked from the same incident surface instead of only read as guidance
- `Completed foundation`: that same provider-lane review now also exposes recovery state and last-success timing, so operators can tell whether a provider lane has actually stabilized after failures instead of inferring recovery manually from individual rows
- `Completed foundation`: the same non-email queue now also surfaces real action-policy state (`ready`, `retry ready`, `cooldown`, `canonical flow`, `unsupported`) and enforces cooldown on admin transport-test reruns in backend code, so SMS/WhatsApp operator actions follow policy instead of being UI-only affordances
- `Completed foundation`: those same non-email exact-chain views now also show a recommended next step and escalation rule per flow, so support can move from incident diagnosis into policy-compliant action without reconstructing the playbook manually
- `Completed foundation`: the same non-email queue now also surfaces explicit `Escalation Candidates` with row-level escalation reasons for repeated unresolved verification or transport failures, so operators can distinguish genuine provider/platform debt from incidents that still belong in normal follow-up lanes
- `Completed foundation`: the new `Business Support Queue` links those failed-flow email signals back to business attention/setup/member/invitation actions, so communication failures can be triaged in the same operator workspace as onboarding/support issues
- `Completed foundation`: the same queue now refreshes its summary and triage panels independently via HTMX, improving support responsiveness before a fuller real-time ops surface exists
- `Deferred intentionally`: the remaining communication gaps are now primarily deeper template CRUD, richer delivery-log/outbox modeling, and broader replay policy, rather than missing operator visibility or safe retry for the current live flows

## 32. Deferred Micro-Cleanup

These items are intentionally deferred rather than forgotten:

- ultra-low-traffic HTMX polish on the last microscopic detours that do not block real operator workflows
- cleanup of a few surviving legacy-encoded admin files where touching them now adds more risk than value
- final pruning of transitional redirect and fallback paths after the WebAdmin completion phase is formally closed

## 14. Localization Readiness in WebAdmin

WebAdmin now has a real bilingual baseline and must keep expanding from that base instead of postponing localization.

### Important platform context

- the current required baseline is `de-DE` and `en-US`
- WebAdmin now supports request-culture switching by cookie/query-string and reads supported cultures from site settings
- shared admin chrome, auth screens, and early operator surfaces such as Site Settings, Business Communications, email/channel audits, Orders, Businesses, CRM overview/customers, billing payments/refunds/webhooks/payment-detail, inventory warehouses/stock levels, and business support/setup are already moving onto bilingual rendering
- key operational feedback is also starting to move onto the same de/en baseline through shared alert text plus CRM/inventory success-not-found-concurrency messages, so bilingual coverage is no longer limited to headings and list shells
- billing success/not-found/concurrency feedback plus several lower-traffic billing, shipping-method, and identity editor forms are now also moving onto that same baseline, so bilingual coverage continues from queue/detail screens into finance/support corrective flows
- the same key-based bilingual path is now also being applied to auth, identity, settings, and shipping-method hard-coded feedback, so de/en support is beginning to cover operational validation and recovery messages instead of stopping at headings and TempData in only a few modules
- the localization implementation is now also starting to shift toward the standard ASP.NET Core `resx`/`IStringLocalizer` path, with the older in-code dictionary retained only as a migration fallback so additional languages can be introduced through resource files instead of expanding one giant hard-coded map forever
- that `resx-first` path now also begins to cover shared DataAnnotations localization, which is important because a bilingual admin is not complete if labels and validation metadata still depend on hard-coded English attribute strings
- the migration is also starting to move more shared editor/action/form keys into `resx`, so the current bilingual runtime is gradually becoming resource-file-driven in practice instead of only in architecture
- the resource structure has also been normalized around a root-level `SharedResource` and `Resources/SharedResource*.resx`, which is a better fit for the current shared-key model than hiding the first resource files under an unnecessary subfolder
- identity form metadata is now also beginning to use that same shared `resx` path for labels and validation strings, which matters because a multilingual admin is not really complete if password/email/profile forms still lean on hard-coded attribute text
- future languages must be additive on top of this baseline rather than forcing a redesign

### Required design rules

- avoid hard-coded labels where practical
- keep settings/categories/messages translation-friendly
- maintain German and English parity on high-traffic operator surfaces now
- keep translation plumbing extensible so future languages can be added without rewriting request-localization or screen contracts
- keep templates and system messages ready for later localization

## 15. Security and Performance Concerns

These are non-functional requirements for WebAdmin, not optional nice-to-haves.

### Security

- permission-aware UI
- safe admin action protection
- auditability
- tenant isolation awareness
- PII protection
- secure handling of activation/reset/support flows
- rate-limiting awareness for sensitive paths

### Performance

- pagination/filtering/search for large datasets
- efficient dashboard and list projections
- responsive partial updates
- avoid over-fetching
- background/async handling where provider callbacks or communication delivery are involved

## 16. Controller and View Responsibilities

### Controllers should

- call application handlers
- map DTOs to view models
- return full views for full-page loads
- return partials/shells for HTMX fragment requests
- keep responses authoritative for validation and operator feedback

### Controllers should not

- manipulate EF entities directly
- duplicate domain/application validation
- push complex business rules into page JavaScript

### Views should

- remain server-rendered and thin
- prefer HTMX attributes over custom fetch code
- preserve concurrency tokens such as `RowVersion`
- reuse shared modal, alert, pager, and partial patterns

## 17. Near-Term WebAdmin Delivery Order

1. complete business onboarding and operator account lifecycle support
2. complete settings architecture and operational visibility
3. complete Stripe-first payment operations
4. complete DHL-first shipment operations
5. add Communication Core admin visibility and template support
6. run a full workflow audit across all implemented modules

## 18. DHL-first Shipment Ops Progress

- `Completed foundation`: site settings now expose operator-editable shipment timing thresholds for phase-1 DHL support, specifically a handoff-attention delay and a tracking-grace window
- `Completed foundation`: shipment queues now include `Awaiting Handoff` and `Tracking Overdue` subsets derived from those admin-controlled thresholds instead of fixed code-only assumptions
- `Completed foundation`: DHL readiness cards now surface the configured timing thresholds alongside carrier credentials and shipper identity so operators can interpret queue counts correctly
- `Completed foundation`: order-level shipment review now mirrors those same support signals, which keeps queue triage and order troubleshooting aligned
- `Completed foundation`: shipping-method administration now includes operational summary cards, playbooks, and queue subsets for missing rates, DHL-specific review, global coverage, and multi-rate methods, so carrier setup can be triaged from configuration screens rather than only from shipment failures

## 19. Tax-Aware Order Review Progress

- `Completed foundation`: order details now expose price mode, subtotal net, tax total, discounts, gross total, and the current site tax-policy snapshot in WebAdmin
- `Completed foundation`: order invoice tabs now carry customer tax profile and VAT ID context alongside net/tax/gross breakdowns, which keeps order-bound finance review aligned with the richer CRM invoice workspace

## 20. Business Location Ops Progress

- `Completed foundation`: business-location administration now includes queue-style filters for primary, missing-address, and missing-coordinate states instead of only a flat location list
- `Completed foundation`: location rows now surface readiness badges for incomplete address or coordinate data, which makes storefront, shipping, and map-related follow-up easier from the same admin workspace
- `Completed foundation`: location operations now include summary cards and playbooks so operators can triage business-location readiness before go-live rather than only editing records one by one

## 21. Billing Workspace Completion Progress

- `Completed foundation`: financial-account administration now includes summary cards for asset, revenue, expense, and missing-code states so account hygiene can be reviewed before finance automation expands
- `Completed foundation`: financial-account rows now call out missing account codes directly and link more naturally into journal-entry review from the same workspace
- `Completed foundation`: journal-entry administration now includes operational summary cards and review playbooks for recent and multi-line entries, which makes finance-control triage easier from WebAdmin instead of only from the editor screen
- `Completed foundation`: expense administration now includes summary cards for supplier-linked, recent, and higher-value costs plus review playbooks, which brings it closer to the operational depth of the other billing workspaces

## 22. Business Subscription Handoff Progress

- `Completed foundation`: the business subscription workspace now includes handoff-readiness summary cards for total, ready, blocked, and current-plan counts
- `Completed foundation`: each available billing plan now clearly indicates whether it matches the current subscription, whether external billing handoff is possible, and why a plan is blocked when prerequisites are not satisfied
- `Completed foundation`: operators can now jump directly from a ready plan to the configured external billing-management website, while missing-configuration and missing-prerequisite states are surfaced explicitly instead of remaining implicit in checkout-validation text

## 23. Billing Plan Admin Progress

- `Completed foundation`: billing plans now have a dedicated WebAdmin workspace with queue filters for active, inactive, trial, missing-feature, and in-use plans
- `Completed foundation`: the billing-plan workspace now includes operational summary cards and playbooks so plan-catalog hygiene can be reviewed before subscription handoff or business support changes are made
- `Completed foundation`: operators can now create and edit billing plans directly from WebAdmin, including pricing cadence, trial duration, activation state, and phase-1 features metadata
- `Completed foundation`: the billing-plan workspace now also behaves as an HTMX-aware queue, so subscription-catalog search, filter, and queue triage stay inside the newer server-rendered workflow instead of falling back to older full-page list refreshes
- `Completed foundation`: billing-plan administration now also routes duplicate-code, not-found, and concurrency feedback through `ValidationResource*.resx`, so subscription-catalog support no longer mixes localized workspace UX with raw English handler errors
- `Completed foundation`: billing payment, financial-account, expense, and journal-entry update feedback now also routes not-found and concurrency messages through `ValidationResource*.resx`, so finance correction flows no longer hide raw English handler errors behind a bilingual admin shell

## 24. CRM Segment Ops Progress

- `Completed foundation`: CRM segment administration now includes queue filters for empty, in-use, and missing-description segments instead of relying on a plain searchable list
- `Completed foundation`: the segment workspace now exposes operational summary cards and playbooks so segmentation hygiene can be reviewed before campaigns, support filters, and reporting rely on stale taxonomy
- `Completed foundation`: segment rows now visibly call out empty segments and missing operator descriptions, which makes CRM cleanup work easier without leaving the list screen
- `Completed foundation`: CRM segment create/edit flows now support the standard HTMX shell pattern so segmentation hygiene stays aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: a WebAdmin DI audit found and fixed a CRM composition-root drift where `CreateInvoiceRefundHandler` was injected by `CrmController` but not registered, so the currently wired admin invoice-refund path now has explicit controller-to-DI parity
- `Completed foundation`: CRM invoice edit, status-transition, and refund guardrail feedback now also routes through `ValidationResource*.resx`, so linked-payment conflicts, refund eligibility failures, and unsupported invoice transitions are bilingual across WebAdmin and WebApi instead of remaining raw English strings
- `Completed foundation`: business lifecycle, invitation create/resend/revoke, and member-owner-protection feedback now also routes through `ValidationResource*.resx`, so approval/suspension support, invitation remediation, and last-owner override incidents are bilingual across both admin and API surfaces

## 25. Inventory Ledger Ops Progress

- `Completed foundation`: inventory-ledger review now includes summary cards for total, inbound, outbound, and reservation-heavy activity instead of only a paged transaction table
- `Completed foundation`: the ledger workspace now includes troubleshooting playbooks so stock investigators can interpret inbound corrections and outbound/reservation spikes without leaving the screen
- `Completed foundation`: inventory stock-level create/edit flows now support the standard HTMX shell pattern so baseline stock maintenance no longer depends on the older full-page editor model
- `Completed foundation`: inventory stock action flows for adjust, reserve, release, and return receipt now support the standard HTMX shell pattern so day-to-day stock remediation stays aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model

## 26. Inventory Replenishment and Transfer Ops Progress

- `Completed foundation`: purchase-order administration now includes operational summary cards for total, draft, issued, and received orders instead of relying on a plain searchable list
- `Completed foundation`: the purchase-order workspace now includes replenishment playbooks and clearer status badges plus line-count visibility, which makes supplier-order triage easier before opening each record
- `Completed foundation`: stock-transfer administration now includes operational summary cards for total, draft, in-transit, and completed transfers instead of only a flat list
- `Completed foundation`: the stock-transfer workspace now includes transfer playbooks, clearer status badges, and line-count visibility so warehouse-move troubleshooting can happen directly from the queue view

## 27. Inventory Warehouse and Supplier Ops Progress

- `Completed foundation`: warehouse administration now includes operational summary cards for total, default, and empty locations instead of only a searchable list
- `Completed foundation`: the warehouse workspace now includes readiness playbooks and visible empty-location/default badges, which makes fulfillment setup and cleanup easier before stock operations scale
- `Completed foundation`: supplier administration now includes operational summary cards for total, missing-address, and purchase-order-linked vendors instead of only a searchable list
- `Completed foundation`: the supplier workspace now includes procurement playbooks, explicit missing-address badges, and active-procurement signals so vendor hygiene can be reviewed before purchase-order and finance follow-up

- products, brands, categories, and shipping methods workspaces now also use shared de/en resource keys, localized operator playbooks, and HTMX-safe editor/workspace shells, so storefront-facing catalog and routing operations no longer lag behind the bilingual admin contract.
- CRM overview and invoice workspaces now also expose richer operator playbooks and triage guidance, so CRM follow-up no longer depends only on queue rows and editors.

## 28. Loyalty Workspace Modernization Progress

- `Completed foundation`: loyalty-program administration now includes queue filters for active, inactive, spend-based, and missing-rules programs plus operational summary cards and playbooks
- `Completed foundation`: loyalty reward-tier administration now includes queue filters for self-redemption, missing-description, discount, and free-item tiers plus operational summary cards and playbooks
- `Completed foundation`: loyalty program and reward-tier create/edit flows now support the standard HTMX shell pattern, which starts migrating older loyalty screens onto the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: loyalty campaign administration now includes queue filters for active, scheduled, draft, expired, and push-enabled campaigns plus operational summary cards and playbooks
- `Completed foundation`: loyalty campaign create/edit flows now support the standard HTMX shell pattern so older campaign screens align with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: admin-side loyalty-account provisioning now supports the standard HTMX shell pattern so support-led account creation stays aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: loyalty point-adjustment now supports the standard HTMX shell pattern so support-led balance remediation stays aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: loyalty account-details now support HTMX-aware suspend, activate, and pending-redemption confirmation actions so the main support workspace no longer drops back to older full-page post/redirect behavior
- `Completed foundation`: loyalty account and redemption list workspaces now support HTMX-aware filtering and subset navigation so queue triage stays aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: loyalty account queues and account-details navigation now also stay inside HTMX-aware workspace shells for create, details, adjust-points, redemptions, and back-navigation paths, so support operators no longer bounce between fragment-driven triage and older full-page loyalty routes
- `Completed foundation`: loyalty campaign and scan-session list workspaces now support HTMX-aware filtering, queue subset navigation, and in-place campaign activation, so mobile loyalty rollout/support stays aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: loyalty program and reward-tier list workspaces now support HTMX-aware filtering and queue navigation, so program-setup and reward-catalog triage stay aligned with the newer Darwin WebAdmin server-rendered + HTMX interaction model
- `Completed foundation`: loyalty program, reward-tier, campaign, account-create, and point-adjustment editors now also use HTMX-aware shell/form workflows with in-shell back paths, and the remaining program/reward/redemption/scan-session list handoffs now stay inside their workspace shells, so loyalty support/setup no longer mixes newer fragment-driven flows with older full-page editor/navigation routes
- `Completed foundation`: business list, support queue, members, invitations, locations, owner-override audits, and staff-access badge workspaces now all render through HTMX-aware workspace helpers, and their remaining queue-safe redirects no longer fall back to plain full-page responses when invoked from the newer server-rendered workflow
- `Completed foundation`: loyalty fallback redirects for deleted programs/tiers, missing campaigns, and missing account surfaces now also respect the HTMX workflow instead of bouncing fragment-driven support users back through legacy full-page redirects
- `Completed foundation`: billing financial-accounts, expenses, and journal-entry workspaces now render through HTMX-aware workspace helpers with in-shell filter/new/edit queue paths, so finance administration outside the payment/refund queue no longer depends on older full-page list refreshes
- `Completed foundation`: user, role, and permission delete flows now also stay inside their HTMX workspace/editor shells, with queue-safe redirects and modal post context wired for in-place refresh instead of falling back to older full-page delete redirects
- `Completed foundation`: loyalty workspace cleanup now includes HTMX-safe root redirects and in-shell delete flows for programs and reward tiers, so the remaining destructive/setup paths in loyalty no longer bounce support operators back through older full-page routes
- `Completed foundation`: loyalty accounts, scan sessions, and redemptions now also expose ops summaries and operator playbooks, so member-balance support, scanner diagnostics, and redemption triage are no longer table-only workspaces
- Completed foundation: loyalty filter labels and loyalty/mobile operator playbooks now also route through SharedResource*.resx, and the Mobile Operations workspace now exposes explicit loyalty follow-up handoffs plus localized playbooks/feedback, so the remaining loyalty/mobile priority area is no longer split between bilingual loyalty queues and an English-only mobile diagnostics surface
- `Completed foundation`: loyalty workspaces now render shared alert partials and controller fallbacks for account creation, campaign creation/update, point adjustments, activation, and redemption confirmation, so HTMX-driven loyalty support no longer loses feedback inside fragment refresh paths
- `Completed foundation`: a focused completion review on the priority chains found that pagination was still falling back to plain `<pager>` navigation; the shared pager tag helper now supports HTMX targets/push-url, and the priority loyalty/access/subscriber-support/payment queues now page in-place instead of breaking out to full-page responses
- `Completed foundation`: business archive/remove destructive actions on the main business, location, and member queues now carry HTMX modal context, so confirm-delete flows rerender the same workspace shell instead of degrading back to plain full-page post/redirect behavior
- `Completed foundation`: Business Communications and Email Audits now render through HTMX-aware workspace helpers, and the communication test/send, filter, pagination, and profile/audit navigation paths stay inside the newer server-rendered workflow instead of bouncing back to plain full-page responses
- `Completed foundation`: email-audit rows now expose direct operator handoffs into invitation support, user-account support, communication policy, and business setup/profile workspaces, which turns the phase-1 delivery log into an actionable support surface instead of a read-only audit table
- `Completed foundation`: the payment queue now exposes a reconciliation-focused subset and summary signal for pending, failed, refunded, and Stripe-reference-light rows, so finance support can separate raw support work from actual payment-settlement follow-up without overclaiming full provider reconciliation automation
- `Completed foundation`: payment rows now surface explicit reconciliation badges plus partial/full refund context, which makes Stripe-first provider-history review more defensible from the main queue instead of forcing every operator into the editor first
- `Completed foundation`: payment rows now also expose provider-reference state, open-age hours, and last financial event time, so phase-1 Stripe-first support has a more defensible provider-history snapshot even before deeper reconciliation and dispute tooling exists
- `Completed foundation`: refund rows now also expose provider-reference state, open-age hours, last refund event time, and support-attention badges, so phase-1 refund follow-up no longer depends on raw refund status alone
- `Completed foundation`: refund rows now also expose provider-reference state, open-age hours, last refund event time, and support-attention badges, so refund follow-up no longer depends on raw status alone when operators are working Stripe-first post-payment issues
- `Completed foundation`: the shipment queue now exposes a carrier-review subset and summary signal for DHL rows with missing service, missing tracking after shipment, or returned status, which gives operators a more defensible carrier-exception surface without overclaiming full carrier timeline automation
- `Completed foundation`: shipment rows now surface open-age and in-transit-age context plus HTMX-safe queue navigation, so DHL-first tracking and handoff review can stay inside the queue workspace instead of relying on a plain full-page list and order detours
- `Completed foundation`: shipment rows now also expose tracking-state, last-carrier-event, and exception-note context in both the global queue and order-level shipment grids, so operator follow-up has a phase-1 tracking narrative before deeper DHL timeline/RMA tooling exists
- `Completed foundation`: order detail now also exposes a `Return Support Baseline` card plus direct handoff from returned shipments into the refund flow, so post-delivery return follow-up no longer depends on manually correlating shipment and refund tabs before a dedicated RMA aggregate exists
- `Completed foundation`: the global shipment queue now also exposes direct `Refunds` and `Start Return Refund` handoffs for returned or carrier-review rows, and the order-level shipment grid now pushes returned/carrier-review rows toward the refund tab, so phase-1 return support is no longer trapped behind an order-detail detour
- `Completed foundation`: the shipment workspace now also exposes a dedicated `Return Follow-up` subset with queue counts, row badges, and direct refund/carrier-review handoffs, so returned parcels needing refund-path or carrier-completion review are no longer mixed into the general returned bucket
- `Completed foundation`: carrier-exception rows in the shipment queue and order shipment grid now also deep-link directly into `Shipping Methods` or `Shipping Settings` when missing-service or threshold/tracking issues are the likely next operator step, so DHL troubleshooting no longer stops at passive exception notes
- `Completed foundation`: the email-audit workspace now includes an operational summary panel for total, failed, sent, pending, last-24-hour activity, and failed-flow buckets, which makes Communication Core triage more actionable without pretending a generic resend engine already exists
- `Completed foundation`: Communication Core inventory and resend-policy matrices now also hand off directly into failed-audit subsets and the safe operator workspace for each flow, so these tables are no longer passive documentation surfaces
- `Completed foundation`: the email-audit workspace now also supports `stale pending` and `business-linked failure` triage, so operators can separate transport-latency issues from business-scoped follow-up without waiting for a full Communication Core queue/outbox model
- `Completed foundation`: the business communication profile and failed admin-test audit rows now both expose the safe `Send Test Email` rerun action, so operators can revalidate SMTP/test-inbox fixes without inventing a generic replay queue for live transactional mail
- `Completed foundation`: the Business Communications workspace and business communication profile now also show the live invitation, activation, and password-reset subject/body template previews plus supported-token hints, so operators can inspect current template truth without leaving the communication workflow for site settings
- `Completed foundation`: failed invitation, activation, and password-reset audit rows now deep-link into recipient-scoped invitation or member/user support surfaces, so controlled flow-specific resend follow-up is no longer just a generic recommendation string
- `Completed foundation`: the business communication profile's recent-audit table now mirrors those recipient-aware follow-up actions, so live communication troubleshooting can stay inside the business-level profile instead of detouring into the full audit workspace first
- `Completed foundation`: the Business Communications workspace, business profile, and full email-audit queue now also surface delivery age, completion latency, severity, and follow-up backlog context, so operators can triage communication debt with clearer operational signal before a fuller Communication Core delivery log exists
- `Completed foundation`: site settings and business setup now both expose an explicit ownership matrix with direct handoff between global policy and business-scoped defaults, so operators no longer have to guess whether localization, communication identity, VAT, shipping, or provider settings belong to the platform or the tenant surface
- `Completed foundation`: the webhook queue now also exposes `Payment Exceptions` and `Dispute Signals` subsets derived from Stripe callback history, so finance support can separate callback-level anomalies from general delivery failures before a dedicated dispute aggregate exists
- `Completed foundation`: webhook anomaly rows now also expose direct operator handoffs into the payment or refund queues plus payment settings, so Stripe-first callback review no longer stops at signal visibility and can move straight into the next safe support surface
- `Completed foundation`: the payments and refunds workspaces now also surface webhook-anomaly summary signals and direct handoffs into `Payment Exceptions` and `Dispute Signals`, so Stripe-first operators can move from settlement/refund triage into callback evidence without manually pivoting through the billing navigation first
- `Completed foundation`: the payments workspace now also exposes a dedicated `Dispute Follow-up` subset with row-level dispute badges and direct webhook dispute handoffs, so Stripe callback anomalies are no longer trapped only in the webhook queue
- `Completed foundation`: the payment editor now includes a reconciliation/dispute snapshot with provider-reference state, last-event visibility, and direct handoffs into `Needs Reconciliation`, `Dispute Follow-up`, `Payment Exceptions`, and `Dispute Signals`, so Stripe troubleshooting no longer has to start only from list queues
- `Completed foundation`: payment and refund queue rows now also expose direct reconciliation/support/dispute queue jumps plus HTMX-safe links into order, customer, invoice, and payment workspaces, so Stripe-first follow-up no longer depends on mixing queue triage with plain-link detours
- `Completed foundation`: the orders queue and order-details workspace now also render through HTMX-aware helpers, and add-payment/shipment/refund/invoice flows now return through the same shell-based workflow, so post-order operations no longer mix fragment-driven follow-up with older full-page queue/detail detours
- `Completed foundation`: CRM customer creation now uses the same HTMX-aware editor entry path as the rest of the customer workflow, and the segments workspace plus customer/segment back-navigation now also stay inside shell-based search/filter/edit flows, which closes another set of lower-traffic CRM detours found during the completion audit
- `Completed foundation`: low-traffic admin feedback debt is now reduced further across permissions, roles, media, orders, and loyalty controllers; the remaining user-visible TempData success/error/warning strings in those surfaces now route through `SharedResource*.resx`, so de/en consistency is no longer limited to the high-traffic queues and editors
- `Completed foundation`: another low-traffic admin feedback pass now covers business lifecycle/setup/member support and product maintenance controllers, so business onboarding/support and product CRUD no longer fall back to raw English TempData messaging while the surrounding WebAdmin shell is localized
- `Completed foundation`: CRM create/edit/convert/support actions now use resource-backed fallback feedback when exceptions do not surface a better localized message, and the interaction/consent/segment partial workspaces now render shared alerts inside their HTMX sections, so CRM operators no longer miss action feedback inside those embedded support surfaces
- `Completed foundation`: the CRM customers workspace now also exposes summary cards, customer-ops counts, and operator playbooks for locale fallback, segmentation gaps, and missing VAT follow-up, so the customer queue is less table-only and more usable as a day-to-day support workbench
- `Completed foundation`: the CRM customer editor now also embeds live interaction, consent, and segment sections plus follow-up actions for opportunity creation and invoice review, so customer maintenance no longer depends on bouncing between multiple CRM screens just to inspect the current support context
- `Completed foundation`: the CRM lead and opportunity editors now also embed their live interaction sections and carry direct follow-up handoffs to linked customer, opportunity creation, and invoice review lanes, so conversion and pipeline maintenance rely less on screen-to-screen hopping during support work
- `Completed foundation`: the CRM invoice editor now also exposes a dedicated follow-up action block for linked customer, order, payment trail, and related invoices, and its status/refund feedback now routes through shared localized messages instead of raw inline strings
- `Completed foundation`: the CRM leads and opportunities workspaces now also expose ops summaries and operator playbooks for qualification, ownership, conversion, close-date pressure, and high-interaction review, so queue triage is less table-only and closer to the richer editor workbenches

- Products workspace and product editors now use shared de/en resource keys, operator notes, and localized product playbooks for catalog readiness work.
- `Completed foundation`: the business support queue header now exposes direct lifecycle shortcuts for suspended businesses, pending activation, and locked members in addition to the newer merchant-readiness subsets, so support operators can jump into the right lifecycle lane without first traversing the summary cards or broader queue slices.
- `Completed foundation`: business-linked failed-email rows in the support queue now deep-link directly into scoped email-audit and SMS/WhatsApp-audit lanes in addition to the broader communication profile, so communication remediation can start from the failure card itself instead of routing back through broader workspace entry points.
- `Completed foundation`: the business support queue header now exposes a direct `PendingApproval` shortcut in the same HTMX-safe lifecycle lane set as approved-inactive and suspended business slices, so approval triage can start from the workspace entry without detouring through summary cards first.
- `Completed foundation`: the support-queue summary now gives `NeedsAttention` a direct `Merchant Readiness` handoff and lets the invitations card pivot either into failed invitation audits or the `PendingInvites` business subset, so summary-driven support triage is less single-entry and closer to the remediation lane the operator actually needs.
- `Completed foundation`: the support-queue summary now also gives `PendingApproval` and `SuspendedBusinesses` direct `Merchant Readiness` handoffs in addition to their queue-specific paths, so lifecycle triage can pivot from raw subset review into the richer merchant-remediation workspace without leaving the summary layer.
- `Completed foundation`: the support-queue summary now gives the `BusinessesMissingOwner` card a direct remediation handoff toward the merchant/member lane instead of only a raw subset jump, so owner-gap triage can move from detection to the next operational workspace without leaving the summary layer.
- `Completed foundation`: the support-queue summary now also gives `LockedMembers` a direct mobile-operations handoff in addition to user and password-reset lanes, so member lockout triage can pivot into device-state investigation without leaving the summary layer.
- `Completed foundation`: account-activation failed-email rows in the support queue now also hand off directly into mobile operations in addition to member and unconfirmed-user lanes, so activation-failure triage can pivot into device-state investigation from the failure card itself.
- `Completed foundation`: password-reset failed-email rows in the support queue now also hand off directly into mobile operations in addition to user and locked-user lanes, so recovery triage can pivot into device-state investigation from the failure card itself.
- `Completed foundation`: the support-queue summary now keeps the `BusinessesMissingOwner` remediation handoff labeled as `Merchant Readiness` instead of a misleading member-specific label, so the owner-gap card matches its actual target workspace and avoids sending operators toward the wrong mental model.
- `Completed foundation`: the support-queue summary now also gives `PendingInvites` a direct `Merchant Readiness` handoff in addition to the queue and failed-email paths, so invitation-gap triage can pivot into the richer merchant-remediation workspace without leaving the summary layer.
- `Completed foundation`: the support-queue summary now also gives `ApprovedInactive` a direct `Merchant Readiness` handoff in addition to the queue and payments paths, so approved-but-inactive triage can pivot into the richer merchant-remediation workspace without leaving the summary layer.
- `Completed foundation`: the support-queue summary now also gives `MissingPrimaryLocation`, `MissingContactEmail`, and `MissingLegalName` direct `Merchant Readiness` handoffs in addition to their queue-specific remediation paths, so profile/readiness triage can pivot into the richer merchant-remediation workspace without leaving the summary layer.
- `Completed foundation`: the support-queue summary now also gives `PendingActivation` a direct `Merchant Readiness` handoff in addition to failed-email, user, and mobile-operations paths, so activation triage can pivot into the richer merchant-remediation workspace without leaving the summary layer.
- `Completed foundation`: the support-queue summary now also gives `LockedMembers` a direct `Merchant Readiness` handoff in addition to locked-user, password-reset, and mobile-operations paths, so lockout triage can pivot into the richer merchant-remediation workspace without leaving the summary layer.
- `Completed foundation`: business-invitation failed-email rows in the support queue now also hand off directly into `Merchant Readiness` in addition to invitation/audit paths, so invitation-failure triage can pivot into the richer merchant-remediation workspace from the failure card itself.
- `Completed foundation`: account-activation failed-email rows in the support queue now also hand off directly into `Merchant Readiness` in addition to member, unconfirmed-user, and mobile-operations paths, so activation-failure triage can pivot into the richer merchant-remediation workspace from the failure card itself.
- `Completed foundation`: password-reset failed-email rows in the support queue now also hand off directly into `Merchant Readiness` in addition to user, locked-user, and mobile-operations paths, so recovery triage can pivot into the richer merchant-remediation workspace from the failure card itself.
- `Completed foundation`: business-linked failed-email rows in the support queue now also expose a direct `Merchant Readiness` handoff inside their generic action cluster, so communication-failure triage can pivot into the richer merchant-remediation workspace without depending only on flow-specific branches.
- `Completed foundation`: the support-queue header now labels its `attentionOnly` business-queue shortcut as `Needs Attention` instead of the broader `Businesses`, so the entry workspace matches the actual slice it opens and avoids sending operators toward the wrong queue expectation.
- `Completed foundation`: the support-queue summary no longer keeps a stale inline tail for locked-user/mobile/password-reset links after those signals were promoted into full remediation cards, so the summary layer is less mixed and less likely to send operators through redundant paths.
- `Completed foundation`: the attention-businesses card in the support queue now labels its `attentionOnly` business-queue jump as `Needs Attention` instead of `FullQueue`, so that drill-in matches the actual subset it opens and avoids sending operators toward the wrong queue expectation.
- `Completed foundation`: support-queue summary cards that open precise readiness subsets now use subset-specific labels such as `ApprovedInactive`, `MissingPrimaryLocation`, `MissingContactEmail`, `MissingLegalName`, `BusinessesMissingOwner`, and `PendingInvites` instead of the generic `OpenBusinessQueue`, so drill-ins match the actual slice they open and avoid sending operators toward a broader queue expectation.
- `Completed foundation`: the remaining support-queue summary drill-ins for `PendingInvites` and `SuspendedBusinesses` now also use slice-specific labels instead of a generic business-queue phrasing, so those lifecycle/support cards match the exact subset they open.
- `Completed foundation`: the `NeedsAttention` card in the support-queue summary now labels its drill-in with the exact attention slice instead of a generic business-queue phrasing, so that summary card matches the subset it actually opens.
- `Completed foundation`: the `PendingApproval` card in the support-queue summary now labels its drill-in with the exact approval slice instead of a separate open-queue phrasing, so that lifecycle card matches the subset it actually opens and stays consistent with the newer precision drill-ins.
- `Completed foundation`: the header drill-in on the support-queue failed-email card now uses the concrete `FailedEmails` label instead of `FullAuditLog`, so the top-level jump matches the exact failed-email slice it opens rather than implying a broader audit surface.
- `Completed foundation`: the `Invitations` card in the support-queue summary now also hands off directly into `Merchant Readiness` in addition to failed-invitation and pending-invite slices, so invitation triage can pivot into the richer merchant-remediation workspace from the summary layer itself.
- `Completed foundation`: attention-business rows in the support queue now also expose a direct `Merchant Readiness` action alongside setup and the deeper operational lanes, so row-level support triage can pivot into the richer merchant-remediation workspace without first bouncing through summary cards or header shortcuts.
- `Completed foundation`: business-linked failed-email rows in the support queue now also hand off directly into `Refunds` and `OwnerOverrideAudits`, so communication-failure triage can reach finance and governance follow-up from the same row instead of stopping at communications, subscription, or tax lanes.
- `Completed foundation`: business-linked failed-email rows in the support queue now also hand off directly into `FinancialAccounts`, `Expenses`, and `JournalEntries`, so communication-failure triage can reach accounting-detail follow-up from the same row instead of stopping at payments, refunds, subscription, or tax lanes.
- `Completed foundation`: business-linked failed-email rows in the support queue now also hand off directly into `Setup` and `Locations`, so communication-failure triage can reach onboarding/setup remediation from the same row instead of stopping at communication, billing, or governance lanes.
- `Completed foundation`: business-linked failed-email rows in the support queue now also hand off directly into `Members`, so communication-failure triage can reach member-support remediation from the same row instead of relying only on flow-specific activation branches.
- `Completed foundation`: the support-queue header now also exposes a direct `Payments` shortcut alongside communications and mobile operations, so the main support workspace can jump into the primary merchant billing lane without first drilling through rows or summary cards.
- `Completed foundation`: the support-queue header now also exposes a direct `TaxCompliance` shortcut alongside communications, payments, and mobile operations, so the main support workspace can jump into the primary merchant compliance lane without first drilling through rows or summary cards.
- `Completed foundation`: the support-queue header now also exposes a direct `Refunds` shortcut alongside communications, payments, tax-compliance, and mobile operations, so the main support workspace can jump into the primary merchant post-payment lane without first drilling through rows or summary cards.
- `Completed foundation`: the support-queue header now also exposes direct `FinancialAccounts`, `Expenses`, and `JournalEntries` shortcuts alongside the broader merchant support lanes, so the main support workspace can jump straight into accounting-detail follow-up without first drilling through rows or summary cards.
- `Completed foundation`: the support-queue header now also exposes a direct `OwnerOverrideAudits` shortcut alongside the broader merchant support lanes, so the main support workspace can jump straight into the primary governance trail without first drilling through rows or summary cards.
- `Completed foundation`: the business-linked header action in support-queue failed-email rows now uses the concrete `CommunicationOps` label instead of a generic business label, so that communication-profile drill-in matches the actual workspace it opens.
- `Completed foundation`: the failed-email row drill-in for same-flow same-business email failures now uses the concrete `FailedEmails` label instead of the vaguer `OpenSimilarFailures`, so the communication-audit jump matches the exact failed-email slice it opens.




- `Completed foundation`: the business staff-access-badge workspace now turns email-confirmation, inactive-membership, and lockout warnings into direct remediation handoffs to user edit, member edit, locked-user review, and mobile operations, so badge troubleshooting is no longer a passive preview surface.
- `Completed foundation`: the business setup member and invitation preview panels now deep-link from row identity and status badges into user edit, member edit, activation-failure audits, invitation-failure audits, and the exact pending/expired invitation slices, so setup triage can start from the preview surface instead of only from the broader workspaces.
- `Completed foundation`: the business setup summary cards now also deep-link into status review, member/location remediation, localization defaults, merchant readiness, and the support queue, so setup triage can start from the summary layer instead of only from the lower cards and previews.
- `Completed foundation`: the communication-readiness cards in the business setup workspace now also deep-link into the business communication profile, email audits, SMS/WhatsApp audits, and admin-alert routing settings, so communication setup triage can start from the readiness block instead of only from downstream communication workspaces.
- `Completed foundation`: the subscription snapshot cards in the business setup workspace now also deep-link into subscription review, subscription invoices, and payments, so billing triage can start from the live subscription signal instead of only from the action row beneath it.
- `Completed foundation`: the incomplete-setup warning in the business setup workspace now also deep-links into owner, location, contact-email, legal-name, and merchant-readiness remediation lanes, so the first setup gap alert is no longer passive text.
- `Completed foundation`: the ownership cards in the business setup workspace now also deep-link into business edit, business communication profile, setup-localization, and the global communication/payments/tax settings lanes, so ownership triage is no longer just explanatory copy.
- `Completed foundation`: the branding section in the business setup workspace now also deep-links into business-app settings, the business communication profile, and merchant readiness, so branding cleanup no longer stops at raw name/logo/color fields alone.
- `Completed foundation`: the operational-setup action block in the business setup workspace now also deep-links into pending-activation, locked-member, pending-invite, expired-invite, and merchant-readiness follow-up lanes, so setup operators no longer have to leave the action cluster to reach the most common remediation queues.
- `Completed foundation`: the no-active-subscription alert inside the business setup workspace now also deep-links into subscription review, subscription invoices, payments, and the business support queue, so the empty-state billing warning no longer stops at passive text.
- `Completed foundation`: the platform-dependencies intro and operator-rule blocks inside the business setup workspace now also deep-link into merchant readiness, the business support queue, global settings, business edit, and the business communication profile, so the tenant-vs-platform decision layer no longer stops at explanatory copy before the lower dependency cards.
- `Completed foundation`: the members-attention and open-invitations preview cards inside the business setup workspace now also deep-link from their headers into members, invitations, merchant readiness, and the business support queue, so operators can jump from the preview entry layer into the right remediation workspace before drilling into row-level details.
- `Completed foundation`: the footer action row in the business setup workspace now also deep-links into merchant readiness and the business support queue in addition to save/back, so exiting setup no longer forces operators to bounce only through the generic business edit surface.
- `Completed foundation`: the warning blocks at the top of the business members workspace now also deep-link into owner-override audits, merchant readiness, setup, the pending-activation member slice, and failed activation emails, so member-support triage no longer leaves those alerts as explanatory text plus generic user/mobile jumps only.
- `Completed foundation`: the business editor workspace now also deep-links from its summary cards and onboarding warning into members, locations, invitations, setup, merchant readiness, and the business support queue, so editor-driven merchant triage no longer stalls at counts and explanatory onboarding copy.
- `Completed foundation`: the business editor workspace now also deep-links from its operational-status and next-actions blocks into merchant readiness and the business support queue, so business lifecycle review inside the editor no longer depends only on inline approve/suspend forms or setup-specific actions.
- `Completed foundation`: the location and invitation editor shells now also deep-link into setup, merchant readiness, and the business support queue instead of only a single back button, so lower-traffic merchant maintenance flows do not dead-end when the operator needs to continue remediation from inside those editors.
- `Completed foundation`: the business-member editor now also deep-links its last-active-owner warning and controlled-owner-override block into owner-override audits, merchant readiness, and the business support queue, so governance-sensitive member remediation no longer depends only on the destructive override form itself.
- `Completed foundation`: the subscription workspace now also deep-links its no-active-subscription and no-recent-invoices empty states into setup, merchant readiness, the business support queue, invoices, and payments, so billing follow-up no longer dead-ends on empty-state copy inside the merchant subscription lane.
- `Completed foundation`: the subscription workspace now also deep-links its no-active-plans empty state into setup, merchant readiness, and the business support queue, so subscription provisioning follow-up no longer dead-ends when no selectable plan inventory is available for the merchant.
- `Completed foundation`: the subscription workspace now also deep-links its plan-row prerequisite branch into setup, merchant readiness, and the business support queue instead of only muted prerequisite copy, so blocked plan handoff no longer stalls inside the available-plans table.
- `Completed foundation`: the empty states in the business locations and invitations workspaces now also deep-link into create, setup, merchant-readiness, and support lanes instead of only rendering empty copy, so low-volume merchant maintenance queues do not dead-end when they currently have no rows.
- `Completed foundation`: the owner-override audits workspace now also deep-links its intro warning and zero-row empty state into members, setup, merchant readiness, and the business support queue, so governance review no longer dead-ends when the audit lane is informational or temporarily empty.
# Darwin WebAdmin

- `Staff access remediation`: `StaffAccessBadge` now exposes direct follow-up pivots for `Edit member`, `User edit`, `Members`, and `Merchant Readiness`, so badge review does not stop at a passive informational header.
- `Invitation empty-state remediation`: `Invitations` now exposes a direct pivot for `Merchant Readiness` from its empty state, so zero-row invitation review does not stop at create/setup/support actions alone.
- `Merchant readiness empty-state remediation`: `MerchantReadiness` now exposes direct pivots for `Businesses`, `Business Support Queue`, `Payments`, and `Communication Ops` from its empty state, so zero-row readiness review does not stop at passive copy.
- `Merchant readiness empty-state precision`: the full-queue pivot in the `MerchantReadiness` empty state now uses the concrete `BusinessesTitle` label instead of a generic queue phrasing, so that handoff matches the exact workspace it opens.
- `Merchant readiness precision labels`: `MerchantReadiness` summary cards now use slice-specific labels instead of a generic business-queue label, so each summary drill-in matches the exact subset it opens.
- `Businesses suspension precision`: the `SuspendedBusinesses` summary card in the main `Businesses` workspace now uses the exact suspension label instead of the generic `OpenBusinessQueue` phrasing, so that lifecycle drill-in matches the precise subset it opens.
- `Merchant readiness header precision`: the `attentionOnly` header shortcut in `MerchantReadiness` now uses the exact `NeedsAttention` label instead of a generic business-queue label, so the top workspace entry matches the slice it opens.
- `Merchant readiness playbook pivots`: the playbooks block in `MerchantReadiness` now also exposes direct header/footer pivots for `Business Support Queue`, `NeedsAttention`, and `Payments`, so the playbook table is no longer an isolated middle layer.
- `Merchant readiness playbook row remediation`: the title and scope cells in `MerchantReadiness` playbook rows now deep-link into the queue-action and follow-up lanes, so the table no longer keeps dead text in its main read columns.
- `Merchant readiness operator-action remediation`: the operator-action text in `MerchantReadiness` playbook rows now also deep-links into the queue-action lane, so the row’s main instruction no longer stays as passive copy above its button.
- `Merchant readiness playbook source guard`: the `MerchantReadiness` playbook block is now covered by a dedicated source test, so its approval/setup/billing handoffs, linked title/scope/operator-action cells, explicit plain-text fallback branches, and newer block-level pivots cannot silently drift.
- Merchant playbook button-label source guard: the Businesses, SupportQueue, and MerchantReadiness source guards now also pin the queue-action and follow-up button labels as linked HTMX handoffs, so those row-level pivots cannot quietly degrade back into passive text or broken buttons.
- Additional merchant playbook button-label source guard: the Members, Invitations, OwnerOverrideAudits, and Subscription source guards now also pin their queue-action and follow-up button labels as linked HTMX handoffs, so those row-level pivots cannot quietly degrade back into passive text or broken buttons.
- Locations playbook source guard depth: the Locations source guard now also pins the linked queue-label, context, operator-action, and queue-button cells together, so the row-level location triage path cannot quietly lose depth in one column while the others stay wired.
- `Merchant readiness source guard`: the deep row-action surface in `MerchantReadiness` is now covered by a dedicated source test, so business/setup/communications/billing/governance/support pivots cannot silently drift.
- `Merchant readiness summary source guard`: the summary cards and top queue-entry controls in `MerchantReadiness` are now also covered by a dedicated source test, so support/billing/tax entry pivots plus readiness/account-state subset drill-ins cannot silently drift.
- `Invitation onboarding defaults`: business-invitation onboarding now uses site-setting culture/time-zone/currency defaults for newly provisioned users instead of hardcoded `de-DE` / `Europe/Berlin` / `EUR`, and business create/edit DTO defaults now reuse the shared site-setting constants instead of repeating raw literals.
- `Identity DTO defaults`: the remaining user/profile/address DTO defaults now also reuse shared site-setting constants for locale, timezone, currency, and default country instead of repeating raw geographic/financial literals.
- `Settings fallback defaults`: `GetCultures`, `GetSiteSetting`, and `UpdateSiteSetting` now also reuse shared site-setting constants for culture/country/currency/time/date/home-slug fallbacks instead of repeating raw literals in the settings pipeline.
- `View-layer default options`: the `Register` and `SiteSettings` views now also use shared culture/settings constants for their default locale/currency/time-zone/country option values instead of repeating raw literal codes in Razor.
- `Catalog currency options`: the product-editor currency options now also reuse the shared default-currency constant instead of hardcoding `EUR` in the controller-level fallback list.
- `Settings read defaults`: `GetCultures` and `GetSiteSetting` now also apply the shared site-setting constants for null home-slug/country/time/date/culture fallbacks, so the read-path no longer keeps a parallel set of raw localization literals.
- `Admin default-country seeds`: the remaining admin-side default-country seeds in settings, identity address, CRM customer, and business-location models/controllers now also reuse the shared site-setting country constant instead of repeating raw `DE` literals.
- `CRM customer country seed`: the CRM customer fallback address row and the Razor address template now also reuse the shared site-setting country constant, so the create/edit customer form no longer keeps a raw `DE` literal in its UI seed layer.
- `Billing DTO currency defaults`: the remaining billing DTO currency defaults now also reuse the shared site-setting currency constant instead of repeating raw `EUR` literals across payment, refund, tax-compliance, billing-plan, and business-subscription models.
- `CRM and business API defaults`: the remaining CRM address DTO country defaults plus the member-business onboarding/public-business contract fallbacks now also reuse shared site-setting culture/currency/country constants instead of repeating raw `DE` / `de-DE` / `EUR` literals.
- `Public API culture/currency defaults`: the public catalog/CMS/checkout/shipping controllers and the underlying public catalog/CMS queries now also reuse shared site-setting culture/currency constants instead of repeating raw `de-DE` / `EUR` fallback literals in storefront API entry paths.
- `Catalog/CMS DTO defaults`: the remaining catalog/CMS DTO defaults and admin query culture defaults now also reuse shared site-setting culture/currency constants instead of repeating raw `de-DE` / `EUR` literals in application-layer catalog/content models.
- `Commerce DTO defaults`: the remaining orders/shipping/pricing DTO defaults now also reuse shared site-setting culture/currency/country constants instead of repeating raw `de-DE` / `EUR` / `DE` literals in application-layer commerce models.
- `Cart/invoice/add-on currency defaults`: the remaining cart checkout, CRM invoice, and applicable add-on currency fallbacks now also reuse the shared site-setting currency constant instead of repeating raw `EUR` literals.
- `Lookup/deletion defaults`: the remaining admin lookup culture fallback and account-deletion country placeholder now also reuse shared site-setting constants instead of repeating raw `de-DE` / `DE` literals in application-layer support flows.
- `Mobile profile/address defaults`: the remaining business/consumer MAUI profile and member-address defaults now also reuse shared contract-level locale/timezone/currency/country constants instead of repeating raw `de-DE` / `Europe/Berlin` / `EUR` / `DE` literals in mobile view models.
- `Client contract defaults`: the remaining business-detail, billing, cart, catalog, order, invoice, checkout, and public-shipping contracts now also reuse shared contract-level locale/currency/country constants instead of repeating raw `de-DE` / `EUR` / `DE` literals across public/member DTOs.
- `Business mobile subscription currency fallback`: the remaining runtime currency fallback in the business mobile subscription snapshot now also reuses the shared contract-level default currency constant instead of repeating a raw `EUR` literal in formatting code.
- `Admin override placeholder defaults`: the admin-text-override placeholder examples in business setup and site settings now also derive their culture keys from shared localization/settings defaults instead of repeating raw `de-DE` / `en-US` literals in the UI guidance layer.
- `Domain entity defaults`: the remaining default locale/timezone/currency/country values in core domain entities now also reuse a shared domain baseline instead of repeating raw `de-DE` / `Europe/Berlin` / `EUR` / `DE` literals across business, billing, cart, catalog, CMS, CRM, identity, order, pricing, and settings models.
- `Seed baseline defaults`: the remaining identity, site-settings, shipping, and business bootstrap seed paths now also reuse the shared domain baseline instead of repeating raw `de-DE` / `Europe/Berlin` / `EUR` / `DE` literals in infrastructure seeding.
- `Operational seed defaults`: the remaining billing, cart, catalog, CRM, order, pricing, and site-setting issuer/shipper seed defaults now also reuse the shared domain baseline instead of repeating raw `de-DE` / `EUR` / `DE` literals in infrastructure seeding.
- `CMS seed culture defaults`: the CMS menu/page seed culture defaults now also reuse the shared domain baseline instead of repeating raw `de-DE` literals in infrastructure seeding.
- `Admin culture baseline defaults`: the site-settings DTO baseline and the admin culture catalog now also reuse the shared domain baseline instead of repeating raw `de-DE` / `Europe/Berlin` / `EUR` / `DE` literals in parallel default catalogs.
- `Localization baseline guard`: the centralized default chain from `DomainDefaults` into `SiteSettingDto` and `AdminCultureCatalog` is now also source-guarded, so those baseline defaults cannot silently drift back into parallel raw literal catalogs.
- `Seed baseline guard`: the infrastructure seed baseline across identity, business, shipping, site settings, billing, cart, catalog, CMS, CRM, orders, and pricing is now also source-guarded, so those bootstrap defaults cannot silently drift back into raw literal catalogs.
- `Members/invitations/locations/owner-override/subscription playbook deep links`: the `Businesses`, `SupportQueue`, `Members`, `Invitations`, `Locations`, `OwnerOverrideAudits`, and `Subscription` playbook tables now deep-link from their queue-label and operator-action text into the queue action itself, so those merchant-support playbooks no longer keep their primary read columns passive above the action buttons.
- Members/invitations/owner-override/subscription playbook context remediation: the Members, Invitations, OwnerOverrideAudits, and Subscription playbook rows now also deep-link from their WhyItMatters text into the follow-up lane, so those workspaces no longer keep the context column passive while each row already exposes a dedicated escalation or remediation handoff.
- Locations playbook context remediation: the Locations playbook rows now also deep-link from their WhyItMatters text into the queue lane, so the location-context column no longer stays passive in the one workspace where the same row keeps all remediation inside the queue itself.
- Business-invitations playbook localization: the business-invitations playbook guidance is now resource-backed, so its WhyItMatters and OperatorAction copy no longer stays as raw English controller literals outside the shared localization system.
- Owner-override/subscription playbook localization: the owner-override and subscription playbook guidance is now resource-backed, so those merchant-ops lanes no longer keep governance and billing operator copy as raw English controller literals outside the shared localization system.
- Locations playbook localization: the locations playbook guidance is now resource-backed, so that queue no longer keeps location triage labels and operator copy as raw English controller literals outside the shared localization system.
- Locations filter/playbook label localization: the locations filter and playbook labels now also route through shared resource keys, so that workspace no longer mixes localized UI shells with raw English queue-label/filter literals in the controller path.
- Merchant filter localization: the business status/member/invitation/subscription-invoice filter labels now also route through shared resource keys, so those high-traffic merchant queue filters no longer mix localized shells with raw English controller literals.
- Support queue playbook title remediation: the SupportQueue playbook rows now also deep-link from their title text into the queue-action lane, so the first read column in that workspace is no longer passive while the rest of the row is already actionable.
- Businesses/support-queue playbook scope remediation: the Businesses and SupportQueue playbook rows now also deep-link from their scope text into the follow-up lane, so the second read column in those workspaces no longer stays passive while each row already exposes a dedicated follow-up action.



















- Merchant readiness/support localization: the remaining business communication-readiness summaries, subscription-plan handoff labels, and support-audit guidance now also route through shared resource keys, so those merchant setup/support controller paths no longer keep raw English readiness and remediation literals outside the localization system.

- Subscription status fallback localization: the subscription snapshot fallback status now also routes through shared resource keys, so that merchant billing surface no longer renders a raw English Unavailable literal when the status lookup is missing.

- Merchant mutation feedback localization: the remaining business create/archive, location-archive, invitation-revoke note, and subscription-cancellation flash messages now also route through shared resource keys, so those merchant mutation feedback paths no longer keep raw English literals in the controller.

- Communication resend-policy localization: the communication resend/retry policy matrix now also routes its flow names, safe-action guidance, retry-status notes, operator entry points, and escalation rules through shared resource keys, so that operator-facing communication policy surface no longer stays English-only in controller literals.
