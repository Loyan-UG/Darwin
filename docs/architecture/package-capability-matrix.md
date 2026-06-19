# Package Capability Matrix

Reviewed: 2026-06-19

This matrix describes example commercial packages on top of capability codes. The examples are generic product packages, not customer-specific names. They define product packaging intent; they do not claim current runtime disable-safety.

## Package Examples

| Package | Included capabilities | Required dependencies | Optional add-ons | Blocked or unsafe standalone capabilities | Main surfaces | Worker/provider/storage requirements | Data implications |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Storefront Starter | `cms`, `catalog`, `storefront`, `webapi-public` | `foundation-platform`, `identity-access`, `business-master` | `member-portal`, `analytics` | `cart-checkout` without `billing`; `provider-stripe` without checkout/billing. | Public Web, public CMS/catalog APIs, WebAdmin CMS/Catalog. | Object storage only if media uses stored assets. No payment provider required. | CMS/catalog data preserved on downgrade; storefront routes hidden or blocked. |
| Commerce Standard | Storefront Starter + `cart-checkout`, `billing`, `member-portal`, `shipping`, `tax-vat` | Storefront Starter, order/invoice foundations, billing. | `provider-stripe`, `provider-dhl`, `e-invoice`, `communications` | Checkout without billing; payment provider without secure config; shipping provider without shipping. | Public checkout, member order/invoice APIs, WebAdmin Orders/Billing/Shipping. | Stripe/DHL only as configured provider add-ons; VIES/e-invoice workers only when enabled. | Orders, payments, invoices, and shipment history are preserved after downgrade. |
| CRM/Loyalty | `crm`, `loyalty`, `member-portal`, `webapi-member`, `webapi-business` | `business-master`, `identity-access`, `foundation-platform` | `communications`, `mobile-consumer`, `mobile-business`, `analytics` | Loyalty without business/customer/member context; CRM-owned loyalty ledger. | WebAdmin CRM/Loyalty, member/business loyalty APIs, mobile loyalty surfaces. | Communications provider optional for campaigns. | Customer and loyalty ledger history remains immutable after downgrade. |
| Operations/Inventory | `catalog`, `inventory`, `procurement`, `webadmin-operations` | `business-master`, `foundation-platform` | `shipping`, `finance`, `bank-treasury`, `mobile-business` warehouse/PWA add-ons later. | Procurement without inventory; stock movement without inventory ledger. | WebAdmin Inventory, suppliers, purchase orders, goods receipts, warehouse tasks. | No provider required by default; DHL only if shipping add-on is enabled. | Stock ledger and purchase history are preserved; disabled package blocks new operations. |
| Finance/Accounting | `billing`, `finance`, `bank-treasury`, `tax-vat`, `e-invoice`, `analytics` | `foundation-platform`, `business-master` | Accounting API target adapter, finance export file delivery, storage profiles. | Finance export without stored package readiness; bank settlement without treasury/reconciliation. | WebAdmin Finance/Billing, member invoice artifacts where entitled. | Object storage for archive/export/e-invoice; provider targets require readiness. | Posted journal entries, invoice artifacts, and bank evidence remain preserved on downgrade. |
| Provider Add-ons | `provider-stripe`, `provider-dhl`, `provider-brevo`, storage profile add-ons such as MinIO. | Owning base capability: billing/checkout, shipping, communications, or storage-backed feature. | Environment-specific provider profiles. | Provider add-on without owning base module or secure readiness. | Provider callbacks, readiness UI, worker operations. | Secure runtime configuration, callback auth, no secrets in metadata/docs/logs. | Provider history stays auditable; new provider operations stop when disabled. |
| Enterprise Full Suite | All core business capabilities: storefront, commerce, CRM, loyalty, operations, finance, HR/time, payroll, AI governance, integration foundation. | All base capabilities. | Provider add-ons, target adapters, advanced storage. | Provider/API targets without readiness and credential owner. | All WebAdmin and selected public/member/business/mobile surfaces. | Workers and providers enabled only where configured and entitled. | Full historical preservation; package changes are auditable. |
| On-Premise Enterprise | Enterprise Full Suite with self-hosted deployment assumptions. | All base capabilities and deployment-ready storage/provider choices. | Local MinIO/S3-compatible storage, file-delivery exports, customer-owned provider profiles. | Cloud provider add-on without customer-approved credential policy. | Same as Enterprise Full Suite. | Self-hosted object storage and provider readiness evidence. | Same preservation rules; deployment evidence is required before enablement claims. |

## Future Module Positioning

These capability codes now have boundary designs but no runtime implementation claim.

| Future capability | Package positioning | Required package context | Business reason | Runtime claim |
| --- | --- | --- | --- | --- |
| `manufacturing-mrp` | Operations/Inventory or Enterprise add-on. | Catalog and Inventory. | Product-producing customers need BOM, routing, production orders, and MRP. | Design-only; no implementation or disable-safe claim. |
| `quality` | Operations/Inventory, Manufacturing, or regulated-goods add-on. | Inventory. | Regulated, serialized, expiring, or supplier-sensitive goods need inspection and nonconformance. | Design-only. |
| `project-operations` | CRM/Sales/Service or Enterprise add-on. | CRM and Sales. | Project-based businesses need tasks, resource planning, project cost, and billing readiness. | Design-only. |
| `service-management` | CRM/Sales/Operations add-on. | CRM. | Repair, maintenance, installation, and field-service businesses need service orders. | Design-only. |
| `support-case-management` | CRM add-on. | CRM. | Support-heavy customers need cases, queues, SLA, and resolution evidence. | Design-only. |
| `advanced-pricing` | Commerce/Sales/Enterprise add-on. | Catalog. | Contract prices, volume terms, and rebates need formal ownership. | Design-only. |
| `strategic-sourcing` | Procurement add-on. | Procurement. | Procurement-heavy customers need purchase requests, RFQ, bid comparison, and supplier scoring. | Design-only. |
| `transportation-logistics` | Operations/Shipping add-on. | Shipping and Inventory. | Route/load/freight planning goes beyond carrier label execution. | Design-only. |
| `finance-controlling` | Finance/Accounting or Enterprise add-on. | Finance. | Enterprise finance needs dimensions, budgets, allocations, and management accounting. | Design-only. |
| `fixed-assets` | Finance/Accounting add-on. | Finance. | Capital assets need register, depreciation, disposal, and audit evidence. | Design-only. |
| `pos-retail` | Commerce/Retail add-on. | Catalog, Billing, Inventory. | Physical retail needs counter sale, cash session, receipt, returns, and device policy. | Design-only. |
| `workforce-planning` | HR/Time add-on. | HR/time. | Managers need workforce demand/capacity planning beyond schedules and timesheets. | Design-only. |
| `master-data-import` | Enterprise/onboarding add-on. | Integrations/sync and target module. | Migrations and coexistence need validated import batches and conflict review. | Design-only. |
| `provider-bank-api` | Provider add-on. | Bank/treasury and integrations/sync. | Automated statement import requires a selected bank or aggregation target. | Blocked by target selection. |

## Safe Commercial Claims

| Claim type | Allowed now | Reason |
| --- | --- | --- |
| Base platform required | Yes for `foundation-platform`, `identity-access`, `business-master`. | These are needed for all tenant operation. |
| Provider add-on optional | Yes as product design, with readiness caveat. | Providers can be configured separately, but package-aware runtime gates remain future work. |
| Independently sellable modules | Design candidate only. | Most business modules still lack full WebAdmin/WebApi/mobile/worker capability gates. |
| Fully disable-safe capability | No broad claim yet. | The modularity audit shows enforcement is incomplete. |

## Dependency Rules

- Storefront Starter does not require checkout or payments.
- Commerce Standard requires checkout, billing, tax, and order/invoice foundations.
- Provider add-ons never stand alone; they attach to owning base capabilities.
- Finance/Accounting can exist without storefront, but not without business and foundation records.
- Operations/Inventory can exist without finance posting, but supplier invoice/payables need Finance/Accounting.
- Mobile surfaces are delivery channels and must follow the package entitlement of their domain capabilities.
