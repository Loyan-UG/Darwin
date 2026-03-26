# Darwin Backlog

This document is the execution roadmap for Darwin. It tracks what is stable, what was recently completed, and what still needs to be built in the recommended order.

## 1. Completed or Stable

- Clean Architecture foundation is in place across Domain, Application, Infrastructure, WebAdmin, WebApi, and mobile.
- `Darwin.Web` has been split from `Darwin.WebAdmin` as a dedicated front-office Next.js application.
- `Darwin.WebAdmin` has been renamed and moved to the root back-office structure.
- CRM no longer duplicates loyalty balances. `Customer.LoyaltyPointsTotal` and `LoyaltyPointEntry` are removed.
- Loyalty remains fully managed by `LoyaltyAccount` and `LoyaltyPointsTransaction`.
- Lead and Opportunity are now part of the domain and application layers.
- Inventory now supports warehouses, stock levels, transfers, suppliers, and purchase orders.
- Billing/accounting now includes payments, financial accounts, journal entries, and expenses.
- Order fulfillment is warehouse-aware and `OrderLine` can persist `WarehouseId`.
- HTMX has been added to `Darwin.WebAdmin` and is now used in representative existing flows.

## 2. Current Focus

The immediate direction is:

1. complete the HTMX-driven rewrite of existing WebAdmin modules
2. expose the new domain modules in WebAdmin
3. continue separating WebApi surfaces by audience
4. keep front-office and mobile aligned with the new contracts and domain rules

## 3. Phase 1 - Documentation and Delivery Alignment

### Epic: Architecture and documentation alignment

- Completed: rewrite README to reflect `Darwin.WebAdmin`, `Darwin.Web`, HTMX, CRM, inventory, billing, and loyalty boundaries.
- Completed: replace the old domain design notes with the new design removing `LoyaltyPointEntry` and documenting Lead, Opportunity, Warehouse, Supplier, Payment, FinancialAccount, JournalEntry, and related modules.
- Completed: expand WebApi documentation around public/member/business/admin surfaces and BFF direction.
- Completed: add a dedicated `DarwinWebAdmin.md` guide for MVC/Razor + HTMX patterns.
- Completed: update identity/how-to documentation to reflect HTMX-backed address flows.
- Completed: rebuild the EF Core migration history into a fresh initial migration aligned with the current domain and infrastructure model.

## 4. Phase 2 - WebAdmin Rewrite

### Epic: HTMX-first rewrite of existing modules

- Completed: add HTMX to the shared WebAdmin layouts.
- Completed: replace custom `fetch`-based loading in the Orders details screen with HTMX partial loading for payments and shipments.
- Completed: move user-address create/edit and default-address operations toward HTMX-driven partial updates.
- Completed: expose new CRM, inventory, and billing modules in WebAdmin with MVC/Razor screens aligned to the new Application handlers.
- Completed: add HTMX-backed CRM interaction timelines on customer, lead, and opportunity edit screens.
- Completed: add HTMX-backed customer consent history and segment membership management screens.
- Completed: move customer, lead, opportunity, invoice, and payment create/edit flows onto reusable HTMX editor-shell patterns with full-page fallback.
- Completed: extend the HTMX editor-shell pattern to financial account, expense, warehouse, and supplier screens so those forms no longer rely on full-page postbacks by default.
- Completed: harden HTMX editor shells so repeated in-place submissions keep a stable target container instead of losing the shell after the first swap.
- Completed: extend the HTMX editor-shell pattern to the remaining multi-line journal entry, stock transfer, and purchase order editors.
- Completed: move order operation screens (`AddPayment`, `AddShipment`, `AddRefund`, `CreateInvoice`) onto HTMX-backed shells with inline loading from order details and proper fallback pages.
- Task: continue removing scattered `fetch`-based fragment refreshes from older WebAdmin pages.
- Task: standardize alert refresh, partial loading, list filtering, and modal submission patterns across all existing and newly added modules.
- Task: extend the HTMX editor-shell pattern to the remaining legacy admin forms and any inline order/accounting operations where it reduces full-page postbacks.

### Epic: Orders, fulfillment, and billing admin

- Completed: warehouse-aware order status changes and fulfillment context persistence.
- Completed: warehouse-aware inventory ledger filtering in WebAdmin.
- Completed: build billing management screens for payments, financial accounts, expenses, and journal entries.
- Completed: add shipment, refund, and invoice management screens to order administration.
- Completed: add order detail tabs for payments, shipments, refunds, and invoices.
- Completed: converge duplicated order-payment and billing-payment concepts into a single `Billing.Payment` aggregate.
- Completed: remove legacy shadow foreign-key columns from business/identity joins and align `OrderLine.VatRate` precision in the database model.
- Completed: expose CRM-linked invoices, payment context, and customer/order cross-links more deeply in the admin UI so Billing and Orders screens no longer rely on raw ids.
- Completed: add dedicated CRM invoice list/edit screens and fix invoice lifecycle persistence so order links and payment reassignments do not leave stale associations.
- Completed: add explicit invoice status transition workflows in CRM so paid/open/cancelled operator actions enforce safer payment-aware guards.
- Completed: align order refunds and order-created invoices with the shared payment aggregate so refunds can mark payments as refunded and invoice creation links/captures eligible payments.
- Completed: add reconciliation visibility across CRM invoice screens, billing payment screens, and order tabs so operators can see refunded, net-collected, settled, and remaining-balance amounts without introducing new domain states.
- Completed: deepen CRM invoice lifecycle with explicit post/void language in the operator UI and a direct invoice refund workflow that records refunds against the linked payment and cancels fully refunded invoices.
- Task: extend invoice lifecycle beyond the current post/void/refund workflow with credit-note, write-off, and reconciliation-specific operator flows where the domain eventually needs them.

### Epic: CRM admin

- Completed: build customer list and editing screens.
- Completed: build lead management with assignment and qualification fields.
- Completed: build opportunity management with stages, projected value, and product lines.
- Completed: build interaction timeline and consent management screens.
- Completed: build segmentation definitions and membership management screens.
- Completed: add explicit lead-to-customer conversion workflows in WebAdmin and Application.
- Completed: add a lightweight CRM overview with pipeline and activity summary cards.
- Task: deepen CRM dashboard/reporting with owner-based, source-based, and conversion-rate breakdowns.

### Epic: Inventory and procurement admin

- Completed: build warehouse management pages.
- Completed: build stock-level management pages.
- Completed: build stock transfer management pages.
- Completed: build supplier management pages.
- Completed: build purchase order management pages.
- Task: add manual stock adjustment, reservation/release, and receipt-specific operator flows.
- Task: add inventory dashboards, low-stock monitoring, and transfer/purchase order workflow actions.

### Epic: Accounting admin

- Completed: build payment management pages.
- Completed: build expense management pages.
- Completed: build financial account management pages.
- Completed: build journal entry management pages.
- Task: add accounting summaries, balancing helpers, and safer posting workflows.

## 5. Phase 3 - WebApi Expansion

### Epic: Public API

- Completed: add non-breaking public/member/business route aliases so the future API surface split can evolve without breaking legacy callers.
- Completed: reorganize `Darwin.WebApi` route ownership so audience-first canonical routes are now explicit for member/business/public controllers, while legacy aliases remain in place for existing clients.
- Completed: split mixed business delivery endpoints into dedicated public and member controllers so storefront discovery and member engagement/onboarding no longer share one mixed controller surface.
- Completed: split loyalty delivery into dedicated member and business controllers so member account/reward/timeline flows and business scan/configuration/campaign flows no longer share one mixed controller surface.
- Completed: add initial public CMS delivery endpoints for published pages and menus under `api/v1/public/cms` with legacy `/api/v1/cms/*` aliases.
- Completed: add initial public catalog delivery endpoints for published categories and products under `api/v1/public/catalog` with legacy `/api/v1/catalog/*` aliases.
- Completed: restore a dedicated public business map-discovery endpoint under `api/v1/public/businesses/map` while preserving the legacy `/api/v1/businesses/map` alias used by existing mobile flows.
- Completed: add initial public storefront cart endpoints under `api/v1/public/cart` with legacy `/api/v1/cart*` aliases for anonymous and authenticated storefront cart mutations.
- Completed: add initial public storefront shipping-rate endpoints under `api/v1/public/shipping/rates` with the legacy `/api/v1/shipping/rates` alias.
- Completed: add initial public storefront checkout order-placement under `api/v1/public/checkout/orders` with the legacy `/api/v1/checkout/orders` alias, including authoritative cart totals, address snapshots, and cart finalization.
- Completed: add storefront checkout-intent preview under `api/v1/public/checkout/intent`, including authoritative cart totals, derived shipment mass, validated shipping options, and selected shipping-rate preview.
- Completed: add storefront payment-intent initiation under `api/v1/public/checkout/orders/{orderId}/payment-intent`, reusing active pending intents where possible instead of creating duplicate pending payments.
- Completed: add generic storefront PSP handoff and payment-completion flows under `api/v1/public/checkout/orders/{orderId}/payment-intent` and `api/v1/public/checkout/orders/{orderId}/payments/{paymentId}/complete`, including hosted-checkout URLs, safe member/anonymous access rules, and payment/order status finalization.
- Completed: add storefront post-order confirmation under `api/v1/public/checkout/orders/{orderId}/confirmation`, with safe access rules for member-owned versus anonymous orders.
- Completed: persist the selected checkout shipping method and its display snapshots on `Order` so confirmation, member history, and back-office screens do not depend on mutable shipping-method configuration.
- Task: deepen public CMS delivery with SEO, structured blocks, and culture fallback rules as storefront requirements expand.
- Task: deepen public catalog delivery with richer pricing, availability, attribute filtering, and search-oriented projections.
- Task: replace the current generic hosted-checkout handoff with provider-specific PSP integration, callback/webhook verification, and reconciliation-safe payment completion.

### Epic: Member API

- Completed: establish the canonical member loyalty route root under `api/v1/member/loyalty` while preserving the legacy `/api/v1/loyalty/*` aliases for existing consumers.
- Completed: add initial member order-history endpoints under `api/v1/member/orders` with legacy `/api/v1/orders/*` aliases for existing clients.
- Completed: add initial member invoice-history endpoints under `api/v1/member/invoices` with legacy `/api/v1/invoices/*` aliases for existing clients.
- Completed: add member profile address-book endpoints under `api/v1/member/profile/addresses` with legacy `/api/v1/profile/me/addresses*` aliases.
- Completed: add a member-facing linked CRM customer summary endpoint under `api/v1/member/profile/customer`.
- Completed: add member privacy and communication preference endpoints under `api/v1/member/profile/preferences` with legacy `/api/v1/profile/me/preferences` aliases, plus aligned route/service catalog updates in `Darwin.Mobile.Shared`.
- Completed: add a richer member-facing CRM customer-context endpoint under `api/v1/member/profile/customer/context`, including segments, consent history, and recent interactions, plus aligned route/service catalog updates in `Darwin.Mobile.Shared`.
- Completed: add member loyalty overview and business-dashboard projections under `api/v1/member/loyalty/my/overview` and `api/v1/member/loyalty/business/{businessId}/dashboard`, plus aligned route/service catalog updates in `Darwin.Mobile.Shared`.
- Completed: add member order and invoice action/document flows, including canonical retry-payment endpoints, plain-text document downloads, and additive action metadata on member order/invoice detail contracts.
- Completed: enrich member loyalty overview and business-dashboard projections with next-reward progress fields while explicitly reporting that point-expiry tracking is not yet enabled in the current loyalty domain model.
- Completed: add a shared mobile member-commerce service abstraction for canonical member order/invoice history, retry-payment, and plain-text document download flows so MAUI clients do not need to hard-code the new WebApi routes.
- Task: deepen member loyalty APIs with true point-expiry modeling, cross-business personalization, and any future reward-progress refinements beyond the current overview/dashboard projections.
- Task: deepen member order and invoice APIs beyond the current retry-payment/document flow with richer storefront-specific actions, document formats, and post-payment follow-up behavior as front-office requirements solidify.
- Task: extend member profile APIs with any additional self-service CRM views beyond the now-supported addresses, linked customer summary, CRM customer context, and privacy/communication preferences.
- Task: propagate warehouse-aware order context into future storefront checkout/order APIs where needed.

### Epic: Admin and integration API

- Completed: establish the canonical business loyalty route root under `api/v1/business/loyalty` while preserving the legacy `/api/v1/loyalty/*` aliases for business mobile flows.
- Task: design CRM admin/integration endpoints.
- Task: design inventory admin/integration endpoints.
- Task: design billing/accounting admin/integration endpoints.
- Task: keep each API surface on its own contract set; no admin DTO reuse in public/member delivery.

### Epic: BFF readiness

- Task: define the future ASP.NET Core BFF/gateway layout for `Darwin.Web`.
- Task: plan route composition, caching, token/session handling, and external consumer isolation.
- Task: ensure future consumers such as SharePoint web parts can use an isolated delivery surface.

## 6. Phase 4 - Front-Office

### Epic: Storefront and member portal

- Task: continue the Next.js storefront implementation in `src/Darwin.Web`.
- Task: implement public CMS and catalog pages against WebApi.
- Task: implement member profile, addresses, loyalty, orders, and invoices against WebApi.
- Task: keep front-office DTOs independent from admin DTOs.

## 7. Phase 5 - Mobile and Cross-Channel Alignment

### Epic: Mobile contract alignment

- Task: review downstream impact of CRM, billing, and fulfillment changes on mobile-facing contracts.
- Completed: migrate shared mobile route constants to the canonical audience-first WebApi route roots while preserving compatibility aliases server-side.
- Completed: add `Darwin.Mobile.Shared` member-commerce service coverage for canonical member order/invoice history, payment-intent, and document-download flows.
- Completed: extend `Darwin.Mobile.Shared` profile service coverage to the canonical member address-book endpoints so mobile self-service address flows use the same audience-first route ownership as WebApi.
- Completed: refactor the consumer rewards screen to consume the aggregated loyalty overview/business-dashboard endpoints and surface next-reward progress instead of relying only on smaller chatty account/history calls.
- Completed: surface canonical member address-book and linked CRM customer-context summaries directly in the consumer profile screen so profile UI uses the new profile service projections instead of ad-hoc follow-up work.
- Completed: add a consumer `Orders & Invoices` screen that consumes the canonical member-commerce routes for history, detail, payment retry, and document copy flows instead of leaving those APIs unused in UI.
- Task: continue updating `Darwin.WebApi`, `Darwin.Contracts`, and MAUI apps wherever future domain or route changes affect mobile consumers.
- Task: keep loyalty behavior backward compatible unless an explicit contract version change is introduced.
