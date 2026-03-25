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

## 4. Phase 2 - WebAdmin Rewrite

### Epic: HTMX-first rewrite of existing modules

- Completed: add HTMX to the shared WebAdmin layouts.
- Completed: replace custom `fetch`-based loading in the Orders details screen with HTMX partial loading for payments and shipments.
- Completed: move user-address create/edit and default-address operations toward HTMX-driven partial updates.
- Completed: expose new CRM, inventory, and billing modules in WebAdmin with MVC/Razor screens aligned to the new Application handlers.
- Completed: add HTMX-backed CRM interaction timelines on customer, lead, and opportunity edit screens.
- Completed: add HTMX-backed customer consent history and segment membership management screens.
- Task: continue removing scattered `fetch`-based fragment refreshes from older WebAdmin pages.
- Task: standardize alert refresh, partial loading, list filtering, and modal submission patterns across all existing and newly added modules.
- Task: move the new CRM, inventory, and billing create/edit flows to richer HTMX partial submission patterns where it reduces full-page postbacks.

### Epic: Orders, fulfillment, and billing admin

- Completed: warehouse-aware order status changes and fulfillment context persistence.
- Completed: warehouse-aware inventory ledger filtering in WebAdmin.
- Completed: build billing management screens for payments, financial accounts, expenses, and journal entries.
- Completed: add shipment, refund, and invoice management screens to order administration.
- Completed: add order detail tabs for payments, shipments, refunds, and invoices.
- Task: expose CRM-linked invoices more deeply in the admin UI and connect them to payment workflows.
- Task: converge duplicated order-payment and billing-payment concepts into a single financial aggregate.

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

- Task: document and implement CMS page endpoints.
- Task: document and implement menus and storefront navigation endpoints.
- Task: document and implement product/category/storefront browsing endpoints.

### Epic: Member API

- Task: document and implement order history endpoints.
- Task: document and implement invoice endpoints.
- Task: document and implement member loyalty projections and account endpoints.
- Task: propagate warehouse-aware order context into future storefront checkout/order APIs where needed.

### Epic: Admin and integration API

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
- Task: update `Darwin.WebApi`, `Darwin.Contracts`, and MAUI apps wherever domain changes affect mobile consumers.
- Task: keep loyalty behavior backward compatible unless an explicit contract version change is introduced.
