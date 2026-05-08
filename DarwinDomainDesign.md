# Darwin Domain Design

This document describes both the current implemented domain shape and the near-term design refinements required for Darwin to support SME go-live, WebAdmin operations, Stripe-first payments, DHL-first shipping, onboarding, communication, tax, and localization.

This is a design document, not a claim that every target aggregate below is already implemented in code.

Current go-live provider/compliance gaps are tracked in `docs/go-live-status.md`. As of 2026-05-08, Stripe and DHL have strong callback/queue plumbing but still need live provider creation calls; Tax/VAT/E-Invoice has visibility support but still needs a dedicated compliance workflow.

Status terms used below:

- `Completed`
- `In Progress`
- `Planned / Near-term`
- `Future / Later phase`

## 1. Core Standards

- all domain entities live in `Darwin.Domain`
- all entities are `sealed` and inherit from `BaseEntity`
- nullable reference types remain enabled
- non-nullable strings should be initialized, typically with `string.Empty`
- public domain classes and members should remain documented with English XML documentation
- TODO markers are reserved for later-phase work only

## 2. Current Bounded Modules

Current bounded areas:

- Identity
- CRM
- Loyalty
- Catalog and CMS
- Orders and fulfillment
- Inventory and procurement
- Billing and accounting

Platform-level capabilities that now need clearer domain treatment:

- Communication Core
- Merchant / tenant onboarding
- Settings architecture
- Tax and VAT readiness
- Invoice and archive readiness
- Localization and language preference modeling

## 3. CRM

### Current implemented state

- `Completed`: `Customer` can optionally point to `User` through `UserId`
- `Completed`: `Customer.LoyaltyPointsTotal` is removed
- `Completed`: `LoyaltyPointEntry` is removed
- `Completed`: `Lead`, `Opportunity`, and `OpportunityItem` exist
- `Completed`: `Interaction`, `Consent`, segmentation, and CRM-linked invoice flows exist

### Core rule

CRM does not own loyalty balances.

Whenever a loyalty total is needed for a customer:

- aggregate from `LoyaltyPointsTransaction`
- and/or project from `LoyaltyAccount`

No CRM-owned loyalty ledger must be reintroduced.

### `Customer`

`Customer` is the CRM profile for a person or organization.

Current implemented fields include:

- `UserId`
- `FirstName`
- `LastName`
- `Email`
- `Phone`
- `CompanyName`
- `Notes`
- `CustomerSegments`
- `Addresses`
- `Interactions`
- `Consents`
- `Opportunities`
- `Invoices`

Rules:

- when `UserId` is present, queries should prefer identity-owned profile data where appropriate
- CRM fallback fields remain valid for imported, guest, lead-converted, or pre-registration records

### `Lead`

`Lead` is the pre-customer CRM record used before conversion.

Current implemented fields include:

- `FirstName`
- `LastName`
- `CompanyName`
- `Email`
- `Phone`
- `Source`
- `Notes`
- `Status`
- `AssignedToUserId`
- `CustomerId`
- `Interactions`

### `Opportunity`

`Opportunity` models a commercial opportunity linked to a customer.

Current implemented fields include:

- `CustomerId`
- `Title`
- `EstimatedValueMinor`
- `Stage`
- `ExpectedCloseDateUtc`
- `AssignedToUserId`
- `Items`
- `Interactions`

### Near-term CRM design refinements

- `Planned / Near-term`: add clearer owner assignment and approval policies around lead conversion and customer activation
- `Planned / Near-term`: strengthen business/tenant linkage rules for CRM records in multi-tenant operation
- `Planned / Near-term`: add richer operational policies for support interactions, consent provenance, and customer/account linkage

## 4. Loyalty Boundary

### Current state

- `Completed`: loyalty remains bounded within the loyalty module
- `Completed`: balances and history are represented through `LoyaltyAccount` and `LoyaltyPointsTransaction`

Relevant entities include:

- `LoyaltyProgram`
- `LoyaltyAccount`
- `LoyaltyPointsTransaction`
- `LoyaltyReward`
- `LoyaltyRewardTier`
- `LoyaltyRewardRedemption`

### Rules

- do not duplicate balance fields in CRM
- do not create a second loyalty ledger elsewhere
- keep mobile/web/member projections query-driven

## 5. Orders and Fulfillment

### Current implemented state

- `Completed`: `Order` and `OrderLine` persist commerce snapshots
- `Completed`: `OrderLine.WarehouseId` can persist warehouse context for downstream fulfillment
- `Completed`: shipments, refunds, and order-linked invoices are already operationally visible

### `Order`

Important current fields include:

- `OrderNumber`
- `UserId`
- `Currency`
- `PricesIncludeTax`
- `SubtotalNetMinor`
- `TaxTotalMinor`
- `ShippingTotalMinor`
- `DiscountTotalMinor`
- `GrandTotalGrossMinor`
- `Status`
- `BillingAddressJson`
- `ShippingAddressJson`
- `ShippingMethodId`
- `ShippingMethodName`
- `ShippingCarrier`
- `ShippingService`
- `Lines`
- `Payments`
- `Shipments`

### `OrderLine`

Important current fields include:

- `OrderId`
- `VariantId`
- `WarehouseId`
- `Name`
- `Sku`
- `Quantity`
- `UnitPriceNetMinor`
- `VatRate`
- `UnitPriceGrossMinor`
- `LineTaxMinor`
- `LineGrossMinor`
- `AddOnValueIdsJson`
- `AddOnPriceDeltaMinor`

### Near-term design refinements

- `Planned / Near-term`: store tax and compliance snapshots more explicitly per order/invoice
- `Planned / Near-term`: support stronger separation between operational shipment state and provider-specific delivery/tracking state
- `Planned / Near-term`: add returns and RMA concepts at the order and shipment level

## 6. Inventory and Procurement

### Current implemented state

- `Completed`: `Warehouse`, `StockLevel`, `StockTransfer`, `Supplier`, and `PurchaseOrder` exist
- `Completed`: procurement and internal inventory movement have basic operational coverage

### Current aggregates

`Warehouse`

- `BusinessId`
- `Name`
- `Description`
- `Location`
- `IsDefault`

`StockLevel`

- `WarehouseId`
- `ProductVariantId`
- `AvailableQuantity`
- `ReservedQuantity`
- `ReorderPoint`
- `ReorderQuantity`
- `InTransitQuantity`

`StockTransfer`

- `FromWarehouseId`
- `ToWarehouseId`
- `Status`
- `Lines`

`Supplier`

- `BusinessId`
- `Name`
- `Email`
- `Phone`
- `Address`
- `Notes`

`PurchaseOrder`

- `SupplierId`
- `BusinessId`
- `OrderNumber`
- `OrderedAtUtc`
- `Status`
- `Lines`

### Near-term design refinements

- `Planned / Near-term`: add manual stock adjustment and operational inventory exception flows
- `Planned / Near-term`: add better receipt and supplier-delivery lifecycle support
- `Planned / Near-term`: connect return flows and reverse logistics to inventory consequences

## 7. Billing and Accounting

### Current implemented state

- `Completed`: `Invoice`, `InvoiceLine`, `Payment`, `FinancialAccount`, `JournalEntry`, `JournalEntryLine`, and `Expense` exist
- `Completed`: generic reconciliation and refund-oriented visibility already exists at query/UI level
- `In Progress`: the payment and shipment domains still need more explicit provider-aware lifecycle modeling

### `Invoice`

Current implemented fields include:

- `BusinessId`
- `CustomerId`
- `OrderId`
- `PaymentId`
- `Status`
- `Currency`
- `TotalNetMinor`
- `TotalTaxMinor`
- `TotalGrossMinor`
- `DueDateUtc`
- `PaidAtUtc`
- `Lines`

### `Payment`

Current implemented fields include:

- `BusinessId`
- `OrderId`
- `InvoiceId`
- `CustomerId`
- `UserId`
- `AmountMinor`
- `Currency`
- `Status`
- `Provider`
- `ProviderTransactionRef`
- `PaidAtUtc`

## 8. Payment Domain Refinement

### Current state

- `Completed`: generic `Payment` aggregate exists
- `In Progress`: payment lifecycle is operational but not yet modeled with enough provider-specific depth for Stripe-first production use

### Near-term target design

The payment domain should explicitly model:

- `PaymentMethod`
- `PaymentProvider`
- `ProviderTransactionRef`
- `ProviderPaymentIntentRef`
- `ProviderSessionRef`
- `ReconciliationStatus`
- `RefundStatus`
- `DisputeStatus`
- `CaptureStatus`
- webhook/callback audit trail

### Stripe-first implementation rule

- `Planned / Near-term`: phase-1 provider implementation is Stripe
- `Future / Later phase`: multi-provider expansion comes later
- the domain must stay extensible, but backlog and operational design must remain Stripe-first rather than prematurely generic in every detail

### Payment lifecycle target

The target lifecycle should be explicit and safely transitionable:

- `initiated`
- `pending`
- `requires_action`
- `authorized`
- `paid`
- `partially_refunded`
- `refunded`
- `failed`
- `canceled`
- `disputed`
- `reconciliation_pending`
- `reconciled`

### Domain implications

- provider references should be immutable audit data once recorded
- callback/webhook events should be traceable and idempotent
- reconciliation state should not be inferred only from UI projections forever
- dispute and refund states need first-class operational meaning

## 9. Shipping Domain Refinement

### Current state

- `Completed`: order-bound shipment visibility exists
- `In Progress`: provider-specific shipping model is not yet deep enough for DHL-first operations

### Near-term target design

Shipping should explicitly model:

- `Shipment`
- `ShipmentProvider`
- `ShipmentLabel`
- `TrackingEvent`
- `DeliveryStatus`
- `ReturnShipment`
- `ReturnRequest` / `RMA`
- `ShipmentException` / `DeliveryFailure`

### DHL-first implementation rule

- `Planned / Near-term`: phase-1 provider implementation is DHL
- `Future / Later phase`: additional carriers are deferred
- the abstraction must stay generic, but active implementation and admin workflows should be DHL-first

### Shipment lifecycle target

The target lifecycle should be explicit:

- `pending`
- `label_created`
- `handed_over`
- `in_transit`
- `out_for_delivery`
- `delivered`
- `failed`
- `returned`
- `canceled`

## 10. Communication Core

### Current state

- `In Progress`: notification and email infrastructure exists in the platform
- `Completed foundation`: phase-1 SMTP email dispatch now writes append-only `EmailDispatchAudit` rows with provider, status, flow classification, and optional business correlation
- `Planned / Near-term`: Communication Core is not yet formalized as a proper domain/module-level capability

### Required capability

Darwin needs a platform-level communication abstraction covering:

- `Email`
- `SMS`
- `WhatsApp`
- `Push`
- `InApp`

### Near-term target design

Communication should model:

- message or notification aggregate
- channel enum
- template definition
- localization-aware template variants
- recipient targeting
- consent and preference implications
- delivery state
- retry policy
- queue/outbox responsibility
- message log and audit trail

### Immediate priority use cases

- signup email
- account activation email
- invitation email
- forgot-password email
- reset-password email
- important account notifications

## 11. Merchant / Tenant Onboarding Domain

### Current state

- `Completed`: businesses, users, roles, and identity relationships exist
- `Planned / Near-term`: onboarding still needs explicit domain language and state handling

### Near-term target design

Onboarding should explicitly address:

- business / merchant creation
- tenant/customer provisioning where required
- owner user creation
- invitation and activation token flows
- onboarding state
- activation state
- explicit exceptional-policy records for sensitive ownership overrides (for example, removing the final active owner under controlled admin action)
- approval state
- suspension / deactivation / reactivation state
- initial defaults and setup policies

This is especially important because early operational usage is expected to start from `Darwin.Mobile.Business` and admin-assisted onboarding.

## 12. Settings Architecture

### Current state

- `Completed`: a basic site settings capability already exists
- `Completed foundation`: the `Business` aggregate now stores initial business-scoped branding, localization defaults, business time zone, and phase-1 communication-default fields as the first step of a staged split away from purely global settings
- `Planned / Near-term`: settings still need explicit domain architecture to avoid uncontrolled key/value sprawl

### Required settings scopes

Darwin should explicitly distinguish:

- global/system settings
- tenant/business settings
- payment settings
- shipping settings
- communication settings
- branding settings
- localization settings
- security-related settings
- feature flags / operational toggles

### Required settings categories

At minimum:

- General
- Business Profile
- Localization
- Branding
- Payments
- Shipping
- Communications
- Users & Roles
- Tax & Invoicing
- Security
- Integrations
- Advanced / Operational

## 13. Tax / VAT Readiness

### Current state

- `In Progress`: tax amounts exist in orders and invoices
- `Planned / Near-term`: the domain is not yet explicit enough for Germany/EU-ready B2B/B2C tax handling

### Required design notes

Darwin should be ready for:

- tax profile modeling
- VAT-aware order/invoice snapshots
- VAT ID support
- B2B vs B2C handling
- reverse-charge readiness
- OSS/IOSS extensibility
- country-aware taxation rules

## 14. Invoice and E-Invoice Readiness

### Current state

- `Completed`: invoice aggregate exists and is operational
- `Planned / Near-term`: legal/compliance growth path still needs explicit rules

### Required design notes

Invoices should be able to grow toward:

- legal business identifiers
- tax identifiers
- invoice immutability rules
- structured invoice export readiness
- archive readiness

## 15. Localization and Language Preference Modeling

### Current state

- `Completed`: mobile is already bilingual
- `Planned / Near-term`: WebAdmin multilingual enablement has not started yet

### Domain and application implications

Localization is not only about `.resx` files.

Darwin should explicitly support:

- UI localization
- template localization
- system message localization
- settings label/category localization
- translatable content fields where required
- default language per business
- default language per user
- fallback language policy

## 16. API and Projection Guidance

The domain is not the delivery contract.

Rules:

- public storefront DTOs must remain separate from admin DTOs
- member DTOs must remain separate from admin DTOs
- CRM operational models must not leak directly into public/member delivery
- loyalty totals must be query-side projections from loyalty data
- onboarding, settings, communication, tax, and localization policies should be projected explicitly instead of reconstructed ad hoc in UI code
