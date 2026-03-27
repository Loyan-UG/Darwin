# Darwin WebAdmin Guide

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![HTMX](https://img.shields.io/badge/HTMX-2.0-3366CC?logo=htmx&logoColor=white)](https://htmx.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)

> Scope: `src/Darwin.WebAdmin`, the internal back-office used by staff and administrators.

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

- `In Progress`: major module coverage exists and the HTMX-first rewrite is far along
- `Highest priority`: current team focus is to complete WebAdmin before wider storefront expansion
- `Operational goal`: make day-one SME operations workable from WebAdmin plus supporting backend/mobile flows

## 3. Technology Stack

- ASP.NET Core MVC
- Razor views
- HTMX for server-driven partial interactions
- Bootstrap for layout and components
- minimal JavaScript only where modal orchestration or editor integration still requires it

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
| Catalog/CMS | In Progress | Core CRUD and HTMX patterns exist; operator completeness still needs audit. |
| Business / Tenant Onboarding | In Progress | Business CRUD, owner assignment, member management, location management, invitation management, approval/suspension/reactivation, onboarding checklist visibility, actionable next-step shortcuts, delegated support access, and business mobile soft-gate policy now exist; tenant/customer provisioning and richer onboarding-state orchestration remain near-term. |
| Orders/Billing | In Progress | Order detail, payments, refunds, invoices, and reconciliation visibility exist, and both the orders list and the detail tabs now have queue-style operational filters for open, payment-issue, fulfillment, refund, and invoice follow-up work; the orders list now also exposes quick payment/shipment/invoice actions for common support cases, while Stripe-specific lifecycle support is still pending. |
| CRM | In Progress | Customers, leads, opportunities, interactions, segments, and invoice workflows exist; customer, lead, and opportunity lists now have queue-style operational filters, and lead/opportunity queues now expose quick follow-up actions like conversion, customer deep-links, and prefilled opportunity creation; deeper reporting and support depth still need improvement. |
| Inventory/Procurement | In Progress | Warehouses, suppliers, stock, transfers, purchase orders, and ledger views exist, and warehouse, supplier, purchase-order, stock-level, stock-transfer, and ledger screens now have queue-style operational filters or direct troubleshooting links for setup, replenishment, and stock-attention work; manual stock adjustment, reservation, reservation-release, and return-receipt flows are now exposed from stock levels, while richer exception and structured receiving workflows still need work. |
| Identity/Admin Support | In Progress | Users, roles, permissions, and core admin identity flows exist; invite/activation/support actions need completion. |
| Media | In Progress | Media library exists and now supports content-ops queue filters; deeper reference tracking and purge workflows are still later work. |
| Settings | In Progress | Global site settings exist and business onboarding now has a dedicated setup workspace, but full tenant-aware settings architecture still needs domain and UI expansion. |
| Communication Management | Planned / Near-term | Must become first-class for email templates, delivery logs, resend/retry, and admin visibility. |
| Shipping Operations | Partial | Generic order shipment visibility exists; DHL-first operational workspace is still near-term work. |

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
- `Completed`: invitation acceptance is now available in `Darwin.Mobile.Business` as the current phase-1 business-user onboarding path
- `Decision made`: phase-1 owner onboarding supports both assigning an existing platform user and invitation-first owner creation
- `Completed foundation`: invitation issue/resend emails can now carry both the manual token and a configurable magic-link path
- `Planned / Near-term`: harden the current config-driven magic-link path into fully verified app-link handling if production mobile onboarding needs it
- `Completed`: approval, suspension, and reactivation actions now exist in WebAdmin, together with a readiness checklist for owner, primary location, contact email, and legal-name completion
- `Completed`: approval decisions now have operational impact because `Darwin.Mobile.Business` uses a phase-1 soft gate against the business access-state API
- `Completed foundation`: delegated business-support operators can now access business listing, member-support, and invitation workflows through a dedicated permission path, while business lifecycle and ownership-sensitive actions remain FullAdmin-only
- `Completed foundation`: dashboard, user-role assignment, and permission listing screens now call out the delegated business-support role/permission explicitly, making support access assignment operationally discoverable
- `Completed foundation`: the admin dashboard now exposes a business-support queue with attention, approval, invitation, activation, and lockout counts, and the business member/invitation screens now support queue-oriented filters so support operators can jump directly into pending-activation or open-invitation work
- `Completed foundation`: the businesses index now includes quick queue shortcuts for needs-attention, pending-approval, and suspended businesses, which cuts down operator filter setup during onboarding/support review
- `Completed foundation`: the dashboard communication-operations card and business-support queue card are now HTMX-refreshable partials, so operators can refresh live summaries without reloading the full admin dashboard
- `Completed foundation`: WebAdmin now also includes a dedicated `Business Support Queue` workspace that combines attention businesses with recent failed invitation/activation/password-reset emails, reducing page-hopping during onboarding and support triage
- `Completed foundation`: that support queue is now broken into HTMX-refreshable summary, attention-business, and failed-email fragments, so operators can refresh live triage data without a full page reload
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
- `Completed foundation`: selective delegation is now enforced in both controller authorization and view affordances, so support operators can work with invitations and member support without inheriting approval, archive, location, or owner-management powers
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
- `Planned / Near-term`: settings IA must be restructured before settings sprawl becomes technical debt

## 10. Payment Operations UI

WebAdmin must provide operational payment support.

### Current state

- `Completed foundation`: generic payment list/edit and payment-linked order/invoice visibility exist
- `Completed foundation`: refund and reconciliation visibility exists
- `In Progress`: payment operations are still generic rather than Stripe-first operationally

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

- `Partial`: shipments are visible through order-related flows
- `Planned / Near-term`: dedicated shipping operations UI still needs to be strengthened

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

## 12. Communication Management UI

Communication is a platform capability and must be visible in WebAdmin.

### Required capabilities

- email template management
- notification template management
- communication logs
- resend/retry actions where applicable
- delivery status visibility
- per-business communication settings

### Current state

- `Planned / Near-term`: platform communication management is not yet complete enough for go-live-critical email flows
- `Completed foundation`: business setup now exposes a readiness summary that compares business-level communication preferences with global transport configuration, reducing onboarding ambiguity before full template/logging management exists
- `Completed foundation`: dashboard-level communication-operations metrics now expose transport readiness and business communication-default gaps, giving operators an earlier signal before delivery failures become support tickets
- `Completed foundation`: a dedicated `Business Communications` workspace now gives operators a read-only operational queue for businesses missing support-email or sender defaults, with direct links back into business setup and global transport settings
- `Completed foundation`: that workspace now also includes a pageable business queue, so operators can work communication setup debt as an explicit list instead of relying only on dashboard counts
- `Completed foundation`: the workspace also catalogs the currently live transactional email flows and labels them correctly as hard-coded phase-1 compositions, which reduces confusion before the real Communication Core template/log implementation lands
- `Completed foundation`: the workspace now also supports operator queue filters for missing support-email, missing sender identity, and policy-enabled subsets, making communication debt triage more actionable
- `Completed foundation`: current SMTP-based transactional emails now write `EmailDispatchAudit` records, and the workspace surfaces recent delivery attempts/failures as a stopgap audit trail before the full Communication Core logging pipeline exists
- `Completed foundation`: the same workspace now includes a full email audit-log page with search and status filters, which turns delivery visibility into an operator workflow rather than a dashboard-only preview
- `Completed foundation`: each queued business can now be opened into a communication-profile detail screen that combines business defaults, global transport dependency state, current phase-1 flow implications, and support/onboarding signals in one troubleshooting view
- `Completed foundation`: phase-1 email delivery audits now also store flow classification and optional business correlation, so operators can distinguish invitation, activation, and password-reset email failures without waiting for the full Communication Core delivery model
- `Completed foundation`: the workspace now also includes an explicit capability-coverage matrix for template management, retry/resend, and delivery visibility, so operators are not misled into assuming Communication Core features already exist when they do not
- `Completed foundation`: each business communication-profile screen now also surfaces recommended next actions and recent business-scoped email activity, so operators can move from diagnosis into support/setup action without leaving the communication workspace
- `Completed foundation`: the email audit-log now also includes flow-specific operator playbooks, failed-flow quick filters, and business-linked shortcuts, which makes invitation/activation/password-reset failures actionable without pretending a generic retry queue already exists
- `Completed foundation`: the new `Business Support Queue` links those failed-flow email signals back to business attention/setup/member/invitation actions, so communication failures can be triaged in the same operator workspace as onboarding/support issues
- `Completed foundation`: the same queue now refreshes its summary and triage panels independently via HTMX, improving support responsiveness before a fuller real-time ops surface exists

## 13. Localization Readiness in WebAdmin

WebAdmin is not fully multilingual yet, but it must be built in a localization-friendly way now.

### Important platform context

- mobile apps already support bilingual operation
- adding resource languages in mobile is comparatively straightforward
- WebAdmin should move into multilingual enablement immediately after core completion

### Required design rules

- avoid hard-coded labels where practical
- keep settings/categories/messages translation-friendly
- plan for language/default-locale settings now
- keep templates and system messages ready for later localization

## 14. Security and Performance Concerns

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

## 15. Controller and View Responsibilities

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

## 16. Near-Term WebAdmin Delivery Order

1. complete business onboarding and operator account lifecycle support
2. complete settings architecture and operational visibility
3. complete Stripe-first payment operations
4. complete DHL-first shipment operations
5. add Communication Core admin visibility and template support
6. run a full workflow audit across all implemented modules
