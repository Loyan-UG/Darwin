# Darwin Capability Catalog

Reviewed: 2026-06-19

This catalog defines stable capability codes for modularity and package-readiness work. It is code-backed, but it is not an implementation claim that each capability is currently independently disable-safe.

## Classification Rules

| Field | Meaning |
| --- | --- |
| `base-required` | Required platform capability. Disabling it would make the tenant or product unusable. |
| `independently-sellable` | Candidate sellable module once feature gates exist across UI/API/mobile/worker surfaces. |
| `add-on` | Optional enhancement to a base or sellable module. |
| `provider-add-on` | External provider capability that should be disabled cleanly while the base module remains usable. |
| `platform-surface` | Application surface or delivery channel, not a business domain module. |
| `not-safely-separable-yet` | Capability exists but cannot honestly be sold as disable-safe today. |

## Commercial Packaging Role

Commercial packages sit above these capabilities. A capability can be included in a package without being safe to sell as a standalone module. `FeatureArea` remains the technical enablement unit; `PlatformPackage`, `PlatformPlan`, `PackageFeatureArea`, `TenantPackageAssignment`, and `TenantFeatureOverride` are the future commercial entitlement concepts described in [package-plan-architecture.md](package-plan-architecture.md).

| Capability role | Capabilities | Packaging note |
| --- | --- | --- |
| Package-only dependency | `foundation-platform`, `identity-access`, `business-master`, platform surfaces. | Required to make other packages work. These are not sold as raw standalone modules. |
| Commercial package candidate | `cms`, `storefront`, `catalog`, `crm`, `loyalty`, `inventory`, `procurement`, `sales`, `finance`, `hr-time`. | Candidate package anchors after capability gates and disabled-mode behavior exist. |
| Add-on capability | `member-portal`, `cart-checkout`, `billing`, `bank-treasury`, `shipping`, `communications`, `tax-vat`, `e-invoice`, `analytics`, `payroll`, `ai-governance`, `integrations-sync`. | Optional capability groups that depend on one or more package anchors. |
| Provider or storage add-on | `provider-stripe`, `provider-dhl`, `provider-brevo`, storage profiles used by archive/export/document features. | Must require secure runtime configuration and readiness checks. They are not enabled by package assignment alone. |
| Surface capability | `webadmin-operations`, `webapi-public`, `webapi-member`, `webapi-business`, `mobile-consumer`, `mobile-business`, `worker-operations`. | Delivery channels follow domain entitlement; they are not commercial packages by themselves. |

Most capabilities remain `not-safely-separable-yet` because current code does not consistently enforce package gates across WebAdmin, WebApi, mobile/Web navigation, workers, providers, and storage-backed features.

## Canonical Capabilities

| Code | Name | Category | Current code evidence | Current readiness |
| --- | --- | --- | --- | --- |
| `foundation-platform` | Foundation platform primitives | base-required | `src/Darwin.Domain/Entities/Foundation`, `FeatureAreaService`, `DocumentRecord`, `NumberSequence`, `BusinessEvent`, `AuditTrail`. | Base required. |
| `identity-access` | Identity and access | base-required | `src/Darwin.Domain/Entities/Identity`, `src/Darwin.WebAdmin/Controllers/Admin/Identity`, auth controllers. | Base required. |
| `business-master` | Business master and tenant context | base-required | `src/Darwin.Domain/Entities/Businesses`, WebApi business controllers, WebAdmin Businesses. | Base required. |
| `cms` | Content management | independently-sellable candidate | `Page`, `Menu`, `MediaAsset`, CMS WebAdmin controllers, public CMS controller. | Not safely separable yet. |
| `storefront` | Public storefront and front-office | independently-sellable candidate | `src/Darwin.Web`, public site/catalog/cart/checkout controllers. | Not safely separable yet. |
| `member-portal` | Member portal and self-service | add-on | member WebApi controllers, mobile profile/commerce/payroll services. | Not safely separable yet. |
| `catalog` | Product catalog | independently-sellable candidate | `Product`, `Category`, `Brand`, `AddOnGroups`, Catalog WebAdmin controllers. | Not safely separable yet. |
| `cart-checkout` | Cart and checkout | add-on | `Cart`, `CartItem`, public cart/checkout controllers, Stripe checkout client. | Not safely separable yet. |
| `crm` | CRM | independently-sellable candidate | `Customer`, `Lead`, `Opportunity`, `Interaction`, `Consent`, WebAdmin CRM. | Not safely separable yet. |
| `loyalty` | Loyalty | independently-sellable candidate | `LoyaltyProgram`, `LoyaltyAccount`, `ScanSession`, Loyalty WebApi and WebAdmin. | Partially package-aware through plan features, not broadly disable-safe. |
| `inventory` | Inventory and warehouse | independently-sellable candidate | `Warehouse`, `WarehouseLocation`, `InventoryTransaction`, `StockLevel`, warehouse tasks, stock counts. | Not safely separable yet. |
| `procurement` | Purchasing and suppliers | independently-sellable candidate | `Supplier`, `SupplierContact`, `PurchaseOrder`, `GoodsReceipt` under Inventory ownership. | Not safely separable yet. |
| `sales` | Sales documents and internal sales workspace | independently-sellable candidate | `SalesQuote`, `DeliveryNote`, `ReturnOrder`, `CreditNote`, Sales WebAdmin. | Not safely separable yet. |
| `billing` | Billing, payments, subscription plans | base-required for commerce, add-on otherwise | `Payment`, `BillingPlan`, `BusinessSubscription`, Billing WebAdmin/WebApi. | Not safely separable yet. |
| `finance` | Finance, posting, reporting, payables, treasury | independently-sellable candidate | `JournalEntry`, `FinancialAccount`, finance export, supplier invoice/payment, bank models. | Not safely separable yet. |
| `bank-treasury` | Bank and treasury | add-on to finance | `BankAccount`, `BankStatementImport`, `BankReconciliationMatch`. | Not safely separable yet. |
| `shipping` | Shipping methods and carrier operations | add-on | `ShippingMethod`, `Shipment`, DHL operation handlers, shipping WebAdmin. | Not safely separable yet. |
| `communications` | Email, channel dispatch, notifications | add-on | `EmailDispatchOperation`, `ChannelDispatchOperation`, notifications entities, Brevo sender. | Not safely separable yet. |
| `provider-stripe` | Stripe payment provider | provider-add-on | Stripe checkout/session/webhook handlers and public webhook controller. | Provider-config gated, package gating incomplete. |
| `provider-dhl` | DHL shipping provider | provider-add-on | DHL webhook/controller and shipment provider operation worker. | Provider-config gated, package gating incomplete. |
| `provider-brevo` | Brevo email provider | provider-add-on | Brevo webhook/controller and email sender. | Provider-config gated, package gating incomplete. |
| `tax-vat` | VAT/VIES compliance | add-on | VAT validation retry worker, billing tax compliance surfaces. | Not safely separable yet. |
| `e-invoice` | E-invoice artifact generation and archive | add-on | e-invoice generation boundary, invoice archive maintenance worker. | Not safely separable yet. |
| `analytics` | Analytics and exports | add-on | `AnalyticsExportJob`, `AnalyticsExportFile`, finance reporting/export. | Not safely separable yet. |
| `hr-time` | HR and time tracking | independently-sellable candidate | `HumanResources` entities, HR WebAdmin controller. | Not safely separable yet. |
| `payroll` | Payroll | add-on to HR/finance | payroll entities, member payslip WebApi, HR WebAdmin payroll pages. | Not safely separable yet. |
| `ai-governance` | AI governance and automation review | add-on | AI entities, `AiGovernanceController`, scoped projection services. | Not safely separable yet. |
| `integrations-sync` | External systems and sync/conflict | add-on/foundation | `ExternalSystem`, `ExternalReference`, `SyncState`, `SyncConflict`. | Foundation ready, target-specific disable safety depends on adapters. |
| `mobile-consumer` | Consumer mobile app | platform-surface | `src/Darwin.Mobile.Consumer`, `Darwin.Mobile.Shared`, member routes. | Surface package metadata missing. |
| `mobile-business` | Business mobile app | platform-surface | `src/Darwin.Mobile.Business`, business account/loyalty services. | Surface package metadata missing. |
| `webadmin-operations` | WebAdmin operations | platform-surface | `src/Darwin.WebAdmin`, `_Layout.cshtml`, admin controllers. | Required for operators; module nav gating incomplete. |
| `webapi-public` | Public WebApi | platform-surface | `src/Darwin.WebApi/Controllers/Public`. | Audience-scoped; capability gating incomplete. |
| `webapi-member` | Member WebApi | platform-surface | `src/Darwin.WebApi/Controllers/Member`, profile, loyalty, notifications. | Audience-scoped; capability gating incomplete. |
| `webapi-business` | Business WebApi | platform-surface | `src/Darwin.WebApi/Controllers/Business`, business billing/loyalty/media. | Audience-scoped; capability gating incomplete. |
| `worker-operations` | Worker operations | platform-surface | `src/Darwin.Worker/Program.cs` hosted services. | Worker capability skip policy incomplete. |

## Packaging Principle

Capabilities are product concepts. Schemas and folders are implementation clues, not final package boundaries. For example, procurement entities are implemented under Inventory, and finance/payables/treasury entities are implemented under Billing. Package ownership must follow the capability owner documented here.
