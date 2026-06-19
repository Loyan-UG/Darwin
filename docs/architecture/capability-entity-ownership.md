# Capability Entity Ownership

Reviewed: 2026-06-19

This document maps capability ownership to current Domain entities and important cross-capability dependencies. It is intentionally conservative: when a capability writes another capability's entity, the future package-safe implementation must go through the owning application service or handler.

## Ownership Matrix

| Capability | Owned entities | Read-only dependencies | Write dependencies | Current source evidence |
| --- | --- | --- | --- | --- |
| `foundation-platform` | `FeatureArea`, `BusinessFeatureOverride`, `CustomFieldDefinition`, `CustomFieldValue`, `Activity`, `Note`, `DocumentRecord`, `NumberSequence`, `BusinessEvent`, `AuditTrail`, AI governance records. | `Business`, user identity for actor metadata. | Shared evidence writes from many modules. | `src/Darwin.Domain/Entities/Foundation`, `src/Darwin.Application/Foundation/FeatureAreaService.cs`. |
| `identity-access` | `User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `UserLogin`, `UserToken`, `UserDevice`, `Address`, security token records. | `BusinessMember`, `Customer` for account context. | Auth/profile handlers write identity-owned records. | `src/Darwin.Domain/Entities/Identity`, WebApi `AuthController`, profile controllers. |
| `business-master` | `Business`, `BusinessLocation`, `BusinessMember`, `BusinessInvitation`, `BusinessMedia`, engagement/favorite/like/review records. | Identity users and permissions. | Onboarding and support flows write business records. | `src/Darwin.Domain/Entities/Businesses`, WebApi Businesses controllers, WebAdmin Businesses. |
| `cms` | `Page`, `Menu`, `MediaAsset`. | Business/site settings for publication context. | CMS controllers write CMS records only. | `src/Darwin.Domain/Entities/CMS`, WebAdmin CMS controllers, `PublicCmsController`. |
| `catalog` | `Product`, `ProductVariant`, `Category`, `Brand`, add-on groups/options. | CMS/media, pricing/tax categories, inventory availability projections. | Catalog admin writes catalog records; checkout and orders should read snapshots. | `src/Darwin.Domain/Entities/Catalog`, WebAdmin Catalog controllers, `PublicCatalogController`. |
| `storefront` | Front-office route/components, no primary domain ledger. | CMS, catalog, cart-checkout, shipping, tax, inventory availability, member portal. | Should delegate checkout/order/payment writes to cart-checkout and billing/order handlers. | `src/Darwin.Web`, public WebApi controllers. |
| `member-portal` | Member-facing route/service surface, no primary domain ledger. | Identity, CRM customer bridge, orders, invoices, loyalty, payroll self-service. | Should not write operational ERP documents except through audience-safe handlers. | `src/Darwin.Mobile.Shared/Api/ApiRoutes.cs`, member WebApi controllers. |
| `cart-checkout` | `Cart`, `CartItem`. | Catalog, pricing, tax, shipping methods, inventory availability, billing providers. | Checkout writes `Order`, `OrderLine`, `Payment` through owning flows. | `src/Darwin.Domain/Entities/CartCheckout`, `PublicCartController`, `PublicCheckoutController`. |
| `crm` | `Customer`, `CustomerAddress`, `CustomerSegment`, `Interaction`, `Consent`, `Lead`, `Opportunity`, CRM invoice projection. | Identity users, businesses, orders, invoices, loyalty account summaries. | CRM owns customer/lead/opportunity writes; it must not write loyalty balances. | `src/Darwin.Domain/Entities/CRM`, `src/Darwin.WebAdmin/Controllers/Admin/CRM/CrmController.cs`. |
| `loyalty` | `LoyaltyProgram`, `LoyaltyAccount`, `LoyaltyPointsTransaction`, `LoyaltyRewardTier`, `LoyaltyRewardRedemption`, `ScanSession`, `QrCodeToken`. | Business, customer/member identity, billing plan features. | Loyalty writes loyalty ledger and scan state; it should not write CRM customer lifecycle. | `src/Darwin.Domain/Entities/Loyalty`, WebApi `LoyaltyController`, WebAdmin Loyalty. |
| `inventory` | `Warehouse`, `WarehouseLocation`, `WarehouseLabelTemplate`, `WarehouseTask`, stock count, transfer, lot/serial/HU, `StockLevel`, `InventoryTransaction`. | Catalog product variants, procurement receipts, sales returns. | Inventory owns stock ledger writes; other modules must call inventory handlers. | `src/Darwin.Domain/Entities/Inventory/InventoryTransaction.cs`, `DarwinDbContext.Inventory.cs`. |
| `procurement` | `Supplier`, `SupplierContact`, `PurchaseOrder`, `PurchaseOrderLine`, `GoodsReceipt`, `GoodsReceiptLine`. | Inventory warehouses/locations, catalog variants, finance supplier invoice matching. | Procurement writes PO/receipt state; stock writes must go through inventory transaction policy. | `src/Darwin.Infrastructure/Persistence/Db/DarwinDbContext.Inventory.cs`. |
| `sales` | `SalesQuote`, `DeliveryNote`, `ReturnOrder`, `CreditNote`. | `Order`, `Invoice`, shipment/payment/refund records, CRM customers, inventory returns. | Sales document workflows write Sales records and link to existing order/invoice/billing owners. | `src/Darwin.Domain/Entities/Sales`, WebAdmin Sales. |
| `billing` | `Payment`, `BillingPlan`, `BusinessSubscription`, `BusinessFeatureUsage`, `SubscriptionInvoice`, `Expense`. | Business, customer, order/invoice context, providers. | Billing owns customer payment/subscription settlement records. | `src/Darwin.Domain/Entities/Billing/BillingModels.cs`, WebAdmin Billing. |
| `finance` | `FinancialAccount`, `FinancePostingAccountMapping`, `JournalEntry`, `JournalEntryLine`, finance export batches/attempts, supplier invoice/payment/advance. | Billing payments, supplier/procurement receipts, bank/treasury records. | Finance posting service owns journal facts and export evidence. | `src/Darwin.Infrastructure/Persistence/Configurations/Billing/AccountingConfiguration.cs`, WebAdmin Finance. |
| `bank-treasury` | `BankAccount`, `BankStatementImport`, `BankStatementLine`, `BankReconciliationMatch`, `BankReconciliationMatchLine`. | Finance accounts, supplier/payroll payments, journal entries. | Treasury evidence links to payments and postings without rewriting them. | `src/Darwin.Infrastructure/Persistence/Db/DarwinDbContext.Billing.cs`. |
| `shipping` | `ShippingMethod`, shipping rates, shipment/payment-shipment records and provider operations. | Orders, addresses, DHL provider configuration. | Shipping owns shipment/provider operation state; order status changes should use order handlers. | `src/Darwin.Domain/Entities/Shipping`, `src/Darwin.Domain/Entities/Orders/PaymentShipment.cs`. |
| `communications` | `EmailDispatchOperation`, `ChannelDispatchOperation`, notification messages/recipients, dispatch audits. | Business/contact/user recipients, provider settings. | Communication workers write dispatch state and callback audit. | `src/Darwin.Domain/Entities/Integration`, `src/Darwin.Domain/Entities/Notifications`. |
| `tax-vat` | `TaxCategory` and VAT validation evidence in billing/invoice flows. | Business/customer tax profile, invoices. | VAT retry worker updates validation state through billing/tax handlers. | `src/Darwin.Domain/Entities/Pricing/TaxCategory.cs`, `VatValidationRetryBackgroundService`. |
| `e-invoice` | E-invoice artifacts and archive metadata through document/object-storage foundations. | Issued invoice source model, object storage. | Archive/generation flows write generated artifact metadata only. | e-invoice docs, invoice archive worker, object storage infrastructure. |
| `analytics` | `AnalyticsExportJob`, `AnalyticsExportFile`, reporting projections. | Cross-domain read models. | Analytics should not mutate source module ledgers. | `src/Darwin.Domain/Entities/Integration/AnalyticsExports.cs`. |
| `hr-time` | Employee, department, position, contract, schedule, attendance, time entry, timesheet, leave/absence records. | Identity/business membership and finance/payroll. | HR owns HR/time records; payroll owns payroll posting. | `src/Darwin.Domain/Entities/HumanResources/HrModels.cs`, HR WebAdmin. |
| `payroll` | Payroll periods/rules/runs/payslips/payments/corrections under HumanResources. | HR employee records, finance posting, bank reconciliation. | Payroll owns salary settlement records and delegates accounting to finance posting. | `HumanResourcesConfiguration`, member payroll WebApi. |
| `integrations-sync` | `ExternalSystem`, `ExternalReference`, `SyncState`, `SyncConflict`, webhook subscriptions/deliveries, provider callback inbox. | All sync-enabled domain records. | Integration services own sync metadata and callback inbox, not source domain facts. | `src/Darwin.Domain/Entities/Integration`. |

## Future Design-Only Ownership

These capability codes have boundary designs but no implemented domain entities yet. They are listed so package and enforcement work can use stable codes without implying runtime support.

| Capability | Future owned entities | Read-only dependencies | Prohibited ownership |
| --- | --- | --- | --- |
| `manufacturing-mrp` | BOM, routing, work center, production order, MRP run/recommendation. | Catalog variants, inventory stock, procurement supply, finance costing. | No parallel stock ledger or finance posting shortcut. |
| `quality` | Inspection plans, quality orders, results, nonconformance, corrective action. | Goods receipts, returns, lots/serials/HU, suppliers. | No refund, supplier invoice, or stock ledger ownership. |
| `project-operations` | Projects, phases, tasks, resource assignments, project cost entries, billing milestones. | CRM, sales, HR/time, procurement, finance. | No invoice, payroll, or journal ownership. |
| `service-management` | Service requests, service orders, tasks, parts/labor lines, service contracts/assets. | CRM, inventory, HR/time, sales/billing. | No direct stock, invoice, payment, or refund mutation. |
| `support-case-management` | Support cases, case messages, SLA policy, resolution records. | CRM, communications, member/order/invoice context. | No operational order/payment/refund/provider success mutation. |
| `advanced-pricing` | Price agreements, contract terms, rebate programs/accruals. | Catalog, CRM, sales, checkout snapshots, finance. | No historical order/invoice recomputation. |
| `strategic-sourcing` | Purchase requests, RFQs, supplier bids, bid lines, scorecards. | Suppliers, purchase orders, goods receipts, quality, payables. | No automatic PO/supplier invoice/payment creation without owner handlers. |
| `transportation-logistics` | Transport loads, routes, stops, freight estimates, exceptions. | Shipments, warehouse tasks, delivery notes, suppliers/carriers. | No carrier success, invoice, or finance posting ownership. |
| `finance-controlling` | Finance dimensions, budget versions/lines, allocation rules/runs. | Journal entries, project/manufacturing/HR dimensions. | No posted journal rewrite. |
| `fixed-assets` | Fixed assets, categories, depreciation books/schedules, asset transactions. | Supplier invoice lines, finance accounts, service assets. | No inventory stock ownership. |
| `pos-retail` | POS terminals, cash sessions, POS sales/lines/tenders/returns. | Catalog, inventory, billing/payments, loyalty, tax. | No checkout, payment, refund, or stock owner bypass. |
| `workforce-planning` | Workforce plans, demand lines, capacity snapshots, scenarios. | HR/time, project/service demand, analytics. | No payroll, time entry, or contract mutation. |
| `master-data-import` | Import batches, mapping profiles, staged rows, validation issues, apply results. | External systems, sync conflicts, all target module handlers. | No direct table writes that bypass owning handlers. |
| `provider-bank-api` | Future bank connection only if target design requires it. | Bank accounts, statement imports, sync state/conflict. | No direct settlement, credential storage, or payment mutation. |

## Cross-Capability Write Rules

| Source capability | Target capability | Current risk | Future package-safe rule |
| --- | --- | --- | --- |
| Storefront/cart-checkout | Orders/billing/shipping | Checkout can span order, payment, shipment, tax, inventory availability. | Checkout orchestration must remain handler-owned and capability-gated before it writes. |
| Sales | Orders/invoices/billing | Sales uses current order/invoice foundations rather than parallel models. | Sales may link/project; order/invoice/payment/refund mutations stay with owning handlers. |
| CRM | Loyalty | CRM can read customer loyalty context. | CRM must not own points balances or loyalty ledger writes. |
| Procurement | Inventory | Goods receipt affects stock. | Receipt posting must use `InventoryTransaction` policy, not a parallel stock ledger. |
| Supplier invoice/payment | Finance | Payables and settlement depend on journal entries. | Posting remains `FinancePostingService` owned and idempotent. |
| Bank reconciliation | Payments/journals | Reconciliation evidence can be linked to payments. | Reconciliation must not rewrite payment, refund, journal, or export history. |
| Providers | Domain modules | Provider callbacks can affect payment/shipping/communication state. | Provider workers must route through domain handlers and skip safely when disabled. |
