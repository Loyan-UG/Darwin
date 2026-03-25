# Darwin Domain Design

This document defines the current target domain model for Darwin and replaces older designs that duplicated loyalty state in CRM or assumed a narrower commerce-only model.

## Core Standards

- all entities live in `Darwin.Domain`
- all entities are `sealed` and inherit from `BaseEntity`
- `Guid` is used for identifiers
- nullable reference types remain enabled
- non-nullable strings must be initialized
- XML documentation must remain complete and English-only
- TODO markers are reserved for genuinely later-phase work

## Bounded Modules

The current domain is organized around these major modules:

- Identity
- CRM
- Loyalty
- Catalog and CMS
- Orders and fulfillment
- Inventory and procurement
- Billing and accounting

## CRM

### Customer

`Customer` is the CRM profile for a person or company. It may optionally point to an identity user through `UserId`.

Current fields:

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

- if `UserId` is present, application queries should prefer profile/contact data from `User`
- CRM fallback contact fields remain valid for guest, imported, or pre-registration records
- CRM does not store loyalty balances
- CRM does not contain `LoyaltyPointsTotal`
- CRM does not contain `LoyaltyPointEntry`

Whenever a loyalty total is needed for a CRM customer, it must be derived from `LoyaltyPointsTransaction` and/or projected from `LoyaltyAccount`. Any previous logic that referenced `Customer.LoyaltyPointsTotal` must be rewritten as a query-side aggregation.

### CustomerAddress

`CustomerAddress` remains the CRM-owned address record for leads or customers that do not rely entirely on identity-managed addresses.

Current fields:

- `CustomerId`
- `AddressId`
- `Line1`
- `Line2`
- `City`
- `State`
- `PostalCode`
- `Country`
- `IsDefaultShipping`
- `IsDefaultBilling`

### Lead

`Lead` is the pre-customer CRM record.

Current fields:

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

### Opportunity

`Opportunity` represents a commercial opportunity linked to a customer.

Current fields:

- `CustomerId`
- `Title`
- `EstimatedValueMinor`
- `Stage`
- `ExpectedCloseDateUtc`
- `AssignedToUserId`
- `Items`
- `Interactions`

### OpportunityItem

`OpportunityItem` is a quoted or negotiated product line attached to an opportunity.

Current fields:

- `OpportunityId`
- `ProductVariantId`
- `Quantity`
- `UnitPriceMinor`

### Interaction

`Interaction` is the shared CRM timeline entity for calls, emails, meetings, notes, support touchpoints, and sales touchpoints.

Current fields:

- `CustomerId`
- `LeadId`
- `OpportunityId`
- `Type`
- `Subject`
- `Content`
- `Channel`
- `UserId`

### Consent

`Consent` records privacy, marketing, and similar customer-facing opt-in or opt-out choices.

Current fields:

- `CustomerId`
- `Type`
- `Granted`
- `GrantedAtUtc`
- `RevokedAtUtc`

### Segmentation

`CustomerSegment` and `CustomerSegmentMembership` remain the explicit segmentation model.

Current fields:

- `CustomerSegment.Name`
- `CustomerSegment.Description`
- `CustomerSegmentMembership.CustomerId`
- `CustomerSegmentMembership.CustomerSegmentId`

## Loyalty Boundary

Loyalty remains a distinct bounded area and is the only place where loyalty point balances and ledgers exist.

Relevant entities:

- `LoyaltyProgram`
- `LoyaltyAccount`
- `LoyaltyPointsTransaction`
- `LoyaltyReward`
- `LoyaltyRewardTier`
- `LoyaltyRewardRedemption`

Rules:

- do not reintroduce loyalty totals into CRM
- do not create a second loyalty ledger outside the loyalty module
- derive any “current points balance” from loyalty transactions or dedicated loyalty projections

## Orders and Fulfillment

### Order

`Order` is the commerce snapshot of a purchase.

Current fields include:

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
- `Lines`
- `Payments`
- `Shipments`
- `InternalNotes`

### OrderLine

`OrderLine` is the immutable purchase snapshot of a variant at order time.

Current fields include:

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

`WarehouseId` is optional, but when it is set it becomes the fulfillment context for downstream reservation/allocation work. This prevents later order transitions from resolving a different warehouse than the one chosen operationally.

## Inventory and Procurement

### Warehouse

Current fields:

- `BusinessId`
- `Name`
- `Description`
- `Location`
- `IsDefault`
- `StockLevels`

### StockLevel

Current fields:

- `WarehouseId`
- `ProductVariantId`
- `AvailableQuantity`
- `ReservedQuantity`
- `ReorderPoint`
- `ReorderQuantity`
- `InTransitQuantity`

### StockTransfer

Current fields:

- `FromWarehouseId`
- `ToWarehouseId`
- `Status`
- `Lines`

### StockTransferLine

Current fields:

- `StockTransferId`
- `ProductVariantId`
- `Quantity`

### Supplier

Current fields:

- `BusinessId`
- `Name`
- `Email`
- `Phone`
- `Address`
- `Notes`
- `PurchaseOrders`

### PurchaseOrder

Current fields:

- `SupplierId`
- `BusinessId`
- `OrderNumber`
- `OrderedAtUtc`
- `Status`
- `Lines`

### PurchaseOrderLine

Current fields:

- `PurchaseOrderId`
- `ProductVariantId`
- `Quantity`
- `UnitCostMinor`
- `TotalCostMinor`

### InventoryTransaction

Current fields:

- `WarehouseId`
- `ProductVariantId`
- `QuantityDelta`
- `Reason`
- `ReferenceId`

## Billing and Accounting

### Invoice

`Invoice` is shared between CRM billing scenarios and order-linked billing scenarios.

Current fields:

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

### InvoiceLine

Current fields:

- `InvoiceId`
- `Description`
- `Quantity`
- `UnitPriceNetMinor`
- `TaxRate`
- `TotalNetMinor`
- `TotalGrossMinor`

### Payment

Current fields:

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

### FinancialAccount

Current fields:

- `BusinessId`
- `Name`
- `Type`
- `Code`

### JournalEntry

Current fields:

- `BusinessId`
- `EntryDateUtc`
- `Description`
- `Lines`

### JournalEntryLine

Current fields:

- `JournalEntryId`
- `AccountId`
- `DebitMinor`
- `CreditMinor`
- `Memo`

### Expense

Current fields:

- `BusinessId`
- `SupplierId`
- `Category`
- `Description`
- `AmountMinor`
- `ExpenseDateUtc`

## API and Projection Guidance

The domain is not the delivery contract.

Rules:

- public storefront DTOs must be different from admin DTOs
- CRM operational DTOs must not leak directly into public/member surfaces
- loyalty totals must be projected from loyalty data, not read from CRM fields
- warehouse-aware fulfillment should be carried through application queries and API contracts where needed, not reconstructed from ad hoc UI rules
