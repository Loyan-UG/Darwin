# Darwin Backlog (Refined Plan)

This document presents a refined backlog and roadmap for the Darwin platform. It condenses the large historical backlog into a concise set of completed work, current priorities and future phases. The goal is to give a clear, actionable plan for evolving the system into a complete CMS + commerce + CRM solution while preserving the existing Clean Architecture. No reference to AI is made; this is purely a planning document for developers.

## 1 - Completed (Stable)

The core foundations of Darwin are already in place and should not require major re-work. These items are summarised for context only:

- **Architecture & Infrastructure**: the solution uses a Clean Architecture structure (Domain -> Application -> Infrastructure -> Web/WebApi/Mobile). Major cross-cutting concerns (soft delete, audit fields, concurrency, translation pattern) and EF Core configurations are complete. The solution builds, migrates and seeds the database correctly.
- **Domain models**: catalog entities (Product, Variant, Category, Brand, Add-ons), CMS pages/menus, pricing (Promotions/Taxes), cart/checkout, partial order/payment/shipment, users & addresses, identity (Role/Permission), settings and SEO are all implemented.
- **Warehouse-aware order fulfillment**: order lines now persist an optional `WarehouseId`, admin status transitions can explicitly choose a warehouse, inventory allocation/reservation flows honor stored warehouse context, and the latest migration (`OrderLineWarehouseAssignment`) has been applied to the development database.
- **Security**: password hashing (Argon2id), two-factor (TOTP), WebAuthn, external logins, password reset flows and security stamp rotation are complete.
- **Application layer**: command/query handlers, validators and a `Result<T>` pattern exist for all current modules.
- **Admin panel (Back-office)**: pages for managing catalog (products, categories, brands), CMS (pages, menus), site settings (partial), robots/sitemap, canonical URL service and some shared UI components are implemented.
- **Mobile baseline**: the initial mobile apps (consumer and business) implement basic account flows, loyalty scanning, QR token handling and baseline discovery. These are considered stable and separate from web work.

> **Note**: Many additional mobile and marketing features exist in the historical backlog, but they remain stable and outside the scope of the web and domain work described here. See the original historical backlog details in repository history if needed.

## 2 - Phase 1: Project Refactor & Domain Completion

The first phase focuses on preparing the existing solution for growth and finishing the domain layer so future work does not require major refactoring. The high-level goal is to rename and reorganise the admin project, design and implement the missing domain modules (CRM, inventory, billing) and introduce simple extensibility patterns.

### Epic: Rename `Darwin.Web` to `Darwin.WebAdmin`

- **Task**: rename the project folder `src/Darwin.Web` to `src/Darwin.WebAdmin` and the project file to `Darwin.WebAdmin.csproj`. Update the solution file (`Darwin.sln`) so it references the new project name. Adjust `ProjectReference` entries in other projects accordingly.
- **Task**: change all namespaces starting with `Darwin.Web` to `Darwin.WebAdmin` throughout the code base. This includes controllers, views, tag helpers and any referenced classes.
- **Task**: move the existing `Areas/Admin` structure to the project root so admin controllers and views are no longer under an MVC area. Update routing and `_ViewImports.cshtml` accordingly. Ensure all links and URL helpers point to the new root paths.
- **Task**: update documentation (README, solution filter files) to reflect the new project name and folder layout.

### Epic: Domain completion and new modules

The current domain lacks CRM, multi-warehouse inventory and billing. Designing and implementing them now avoids conflict later.

- **Task**: design and implement a CRM bounded context (Customer, CustomerAddress, Segment, Interaction, Consent, Lead, Opportunity, Invoice). Each entity should be sealed with `Guid` keys and non-nullable strings. See the domain design document for field details.
- **Task**: design and implement multi-warehouse inventory. Introduce Warehouse, StockLevel and StockTransfer entities. Remove stock fields from ProductVariant and migrate data to StockLevel. Seed a default warehouse named "Main warehouse" for small companies. Handlers for reserving, releasing and transferring stock should be added.
- **Task**: design and implement a billing module. Extend Invoice and InvoiceLine for CRM and order scenarios, add Payment plus lightweight accounting entities (FinancialAccount, JournalEntry, Expense), and provide handlers to generate invoices, record payments, and prepare ERP-aligned bookkeeping.
- **Task**: expose appropriate commands/queries in the Application layer and REST endpoints in WebApi. Document DTOs and validation rules.

### Epic: Admin UI foundation upgrades

- **Task**: add support for HTMX or Alpine.js to the admin panel to enable lightweight dynamic interactions without a full SPA. Configure bundling (using LibMan or npm) and create sample partial views that update via HTMX (e.g., inline editing of site settings).
- **Task**: complete Site Settings UI (SMTP, analytics, WebAuthn origins, social links, etc.) with caching and concurrency handling.
- **Task**: build full role/permission and user management screens using existing handlers. Include WebAuthn registration management, 2FA enabling/disabling and concurrency conflict alerts.
- **Task**: add grid/pages for new domain modules: customers, segments, interactions, warehouses, stock levels, invoices and payments. Use shared table/list components and modals for create/edit.

## 3 - Phase 2: Back-office Completion

Once the domain is complete, the admin panel can be extended to cover all core business operations. This phase aims to provide a polished experience for staff.

### Epic: Inventory and order management

- **Task**: expand the inventory ledger UI from the current warehouse-aware variant ledger baseline. Warehouse filtering and warehouse surfacing in admin are complete; stock adjustments, transfer logs and cross-warehouse reporting remain.
- **Task**: continue order management screens from the current warehouse-aware status workflow baseline. Viewing orders, changing status with an explicit warehouse, and adding payments are complete; shipments, refunds/cancellations UX polish and invoice/billing integration remain.
- **Task**: add management pages for shipping methods, taxes, coupon/promotions and add-on groups.
- **Task**: create dashboards with simple charts showing sales totals, order counts, customer counts and loyalty points accrual using a lightweight charting library (e.g., Chart.js via HTMX).

### Epic: CRM management

- **Task**: build pages for viewing and editing customers, their addresses and segments. Implement search, filtering and pagination.
- **Task**: build lead and opportunity management flows, including assignment, qualification stages, and conversion from lead to customer.
- **Task**: create views to log and review interactions (email, call, meeting, order) and show customer timelines. Provide forms to add notes and set consents (GDPR compliant).
- **Task**: develop a segmentation editor: allow administrators to create customer segments by selecting criteria (lifetime spend, last purchase date, region, etc.). Each segment membership should be persisted in the `CustomerSegmentMembership` table.
- **Task**: implement invoice and payment management UI. Show open invoices, overdue invoices, paid invoices and allow recording manual payments or refunds.

### Epic: Back-office usability improvements

- **Task**: standardise table/list components across the admin panel with consistent sorting, filtering, search, pagination and actions. Abstract them into a reusable partial view.
- **Task**: ensure all pages are accessible (ARIA labels, keyboard navigation) and responsive. Document a style guide for admin UI to maintain consistency.
- **Task**: implement concurrency handling across admin forms (using row versions) and display user-friendly conflict messages with the ability to reload data.

## 4 - Phase 3: Front-office Implementation

This phase introduces a new, customer-facing web experience. The front-office now lives in `src/Darwin.Web` as a separate Next.js project. It consumes the public REST API and delivers the storefront plus authenticated member portal.

### Epic: Frontend project setup

- **Task**: continue evolving `src/Darwin.Web` as the dedicated Next.js front-office. Maintain TypeScript, Tailwind CSS, linting, and a production-ready app-router layout with clear storefront/member separation.
- **Task**: define environment variables (`NEXT_PUBLIC_API_URL`) and create an API client using axios. Set up SWR or React Query for data fetching and caching.
- **Task**: document and standardize the front-end developer workflow (`npm install`, `npm run dev`, `npm run build`, `npm run start`) and keep the project clearly outside the `dotnet build` pipeline.

### Epic: Public pages and storefront

- **Task**: implement the home page, product list pages and product detail pages. Use server-side rendering (SSR) for SEO and incremental static regeneration (ISR) for scalability. Fetch data from `/api/v1/catalog/products` and related endpoints.
- **Task**: build CMS pages (About, Contact, custom pages) by fetching content from `/api/v1/cms/pages` and menu definitions from `/api/v1/cms/menus`. Render them on the server.
- **Task**: implement cart functionality: display cart items, allow quantity changes, apply promotions and calculate totals. Connect to `/api/v1/cart` endpoints.
- **Task**: integrate a checkout flow: choose shipping method and payment method, confirm order and redirect to payment provider (e.g., Stripe). Handle success and failure callbacks.

### Epic: Customer portal

- **Task**: implement authentication using JWT tokens returned from `/api/v1/auth/login` and store tokens in HTTP-only cookies. Provide registration and password reset flows.
- **Task**: build a customer account area: view/edit profile, manage addresses, view loyalty balance, view order history (`/api/v1/orders`) and invoices (`/api/v1/invoices`).
- **Task**: implement a loyalty wallet: show points balance and transaction history by consuming `/api/v1/loyalty/points` and `/api/v1/loyalty/rewards`.
- **Task**: propagate warehouse context into public order/cart contracts and WebApi endpoints when storefront checkout and fulfillment flows are opened up beyond the current admin-managed workflow.

## 5 - Phase 4: Advanced CRM & Marketing

After the core system is operational, advanced CRM and marketing features can be added incrementally.

- **Task**: design a rules-based segmentation engine allowing administrators to define conditions (e.g., lifetime spend > X, last visit within Y days, specific tags) and assign customers to segments automatically.
- **Task**: implement a campaign management system: create campaigns, schedule them, define audience rules and deliver messages via email/SMS/push notifications. Integrate with third-party delivery providers where necessary.
- **Task**: add analytics dashboards summarising customer behaviour, purchase frequency, churn risk and campaign effectiveness. Use the analytics export jobs and files already present in the integration domain as a basis.

## 6 - Phase 5: Infrastructure & Operations

The final phase covers tooling, deployment and operational concerns.

- **Task**: set up CI/CD pipelines to build and test all projects (Admin, WebApi, Frontend, Mobile) and deploy them to staging/production environments. Each project should have its own build step to avoid coupling.
- **Task**: implement cloud-native data protection (Azure Blob/AWS S3) for key storage, with backup/restore across environments.
- **Task**: document multi-instance deployment guidelines: running multiple web instances behind a load balancer, caching strategies and sticky sessions if needed.
- **Task**: design a plugin mechanism (e.g., via NuGet packages) for future extensibility (e.g., POS integration, AI recommendations) and document the requirements for multi-tenant support.

## Status Legend

- **Completed** - work done and stable; no major changes expected.
- **Phase 1-5** - approved, scheduled phases (to be executed in order but tasks within a phase may run in parallel).
- **Future** - ideas not yet scheduled; may be pulled into later phases when resources allow.

This refined backlog replaces the verbose backlog in the repository for the web and domain portions of Darwin. It deliberately omits the detailed mobile and marketing tasks already delivered and focuses on the tasks required to finish the back-office, complete the domain and build the front-office.
