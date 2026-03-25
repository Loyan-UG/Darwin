# Darwin Domain Design

This document defines the current target domain model for Darwin. It replaces older CRM and billing notes that relied on duplicated loyalty state or narrower accounting concepts.

Core rules:

- All entities live in `Darwin.Domain`.
- Entities are `sealed`, inherit from `BaseEntity`, and use `Guid` keys.
- Non-nullable reference properties must be initialized.
- Loyalty points are managed only by the Loyalty module.
- CRM must not duplicate loyalty balances or loyalty ledgers.

## Identity and CRM Link

### Customer

`Customer` is the CRM profile for a person or company and may optionally link to a registered identity user through `UserId`.

Current shape:

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

- When `UserId` is present, application queries should prefer identity-owned profile and contact data from `User`.
- CRM fallback fields (`FirstName`, `LastName`, `Email`, `Phone`) remain for guest, imported, or pre-registration records.
- CRM does not contain `LoyaltyPointsTotal`.
- CRM does not contain `LoyaltyPointEntry`.

### CustomerAddress

`CustomerAddress` is used for CRM-managed addresses when the customer does not rely entirely on identity-owned addresses.

Current shape:

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

`Lead` represents a pre-customer CRM record.

Current shape:

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

`Opportunity` represents a sales opportunity linked to a customer.

Current shape:

- `CustomerId`
- `Title`
- `EstimatedValueMinor`
- `Stage`
- `ExpectedCloseDateUtc`
- `AssignedToUserId`
- `Items`
- `Interactions`

### OpportunityItem

`OpportunityItem` represents a quoted or discussed product line inside an opportunity.

Current shape:

- `OpportunityId`
- `ProductVariantId`
- `Quantity`
- `UnitPriceMinor`

### Interaction

`Interaction` is the shared CRM timeline record for calls, emails, meetings, support notes, and sales notes.

Current shape:

- `CustomerId`
- `LeadId`
- `OpportunityId`
- `Type`
- `Subject`
- `Content`
- `Channel`
- `UserId`

### Consent

`Consent` remains customer-scoped and stores privacy or marketing decisions.

Current shape:

- `CustomerId`
- `Type`
- `Granted`
- `GrantedAtUtc`
- `RevokedAtUtc`

### CustomerSegment and Membership

Segmentation remains explicit through `CustomerSegment` and `CustomerSegmentMembership`.

Current shape:

- `CustomerSegment.Name`
- `CustomerSegment.Description`
- `CustomerSegmentMembership.CustomerId`
- `CustomerSegmentMembership.CustomerSegmentId`

## Billing and Accounting

### CRM and Order Invoice

`Invoice` is shared across CRM and order-driven billing flows.

Current shape:

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

Current shape:

- `InvoiceId`
- `Description`
- `Quantity`
- `UnitPriceNetMinor`
- `TaxRate`
- `TotalNetMinor`
- `TotalGrossMinor`

### Payment

`Billing.Payment` is the generic accounting-facing payment record.

Current shape:

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

Current shape:

- `BusinessId`
- `Name`
- `Type`
- `Code`

### JournalEntry

Current shape:

- `BusinessId`
- `EntryDateUtc`
- `Description`
- `Lines`

### JournalEntryLine

Current shape:

- `JournalEntryId`
- `AccountId`
- `DebitMinor`
- `CreditMinor`
- `Memo`

### Expense

Current shape:

- `BusinessId`
- `SupplierId`
- `Category`
- `Description`
- `AmountMinor`
- `ExpenseDateUtc`

## Inventory and Procurement

### Warehouse

Current shape:

- `BusinessId`
- `Name`
- `Description`
- `Location`
- `IsDefault`
- `StockLevels`

### StockLevel

Current shape:

- `WarehouseId`
- `ProductVariantId`
- `AvailableQuantity`
- `ReservedQuantity`
- `ReorderPoint`
- `ReorderQuantity`
- `InTransitQuantity`

### StockTransfer

Current shape:

- `FromWarehouseId`
- `ToWarehouseId`
- `Status`
- `Lines`

### StockTransferLine

Current shape:

- `StockTransferId`
- `ProductVariantId`
- `Quantity`

### Supplier

Current shape:

- `BusinessId`
- `Name`
- `Email`
- `Phone`
- `Address`
- `Notes`
- `PurchaseOrders`

### PurchaseOrder

Current shape:

- `SupplierId`
- `BusinessId`
- `OrderNumber`
- `OrderedAtUtc`
- `Status`
- `Lines`

### PurchaseOrderLine

Current shape:

- `PurchaseOrderId`
- `ProductVariantId`
- `Quantity`
- `UnitCostMinor`
- `TotalCostMinor`

### InventoryTransaction

Current shape:

- `WarehouseId`
- `ProductVariantId`
- `QuantityDelta`
- `Reason`
- `ReferenceId`

## Loyalty Boundary

Loyalty remains its own bounded area and is the only place where point balances and point ledgers exist.

Relevant entities:

- `LoyaltyAccount`
- `LoyaltyPointsTransaction`
- `LoyaltyProgram`
- `LoyaltyRewardTier`
- `LoyaltyRewardRedemption`
- `QrCodeToken`
- `ScanSession`

Rules:

- Use `LoyaltyAccount.PointsBalance` and `LoyaltyPointsTransaction` for balances and history.
- Do not add customer-side loyalty totals inside CRM.
- Do not create a second points ledger in CRM or Billing.

## Enums

Important enum additions and updates:

- `LeadStatus`
- `OpportunityStage`
- `TransferStatus`
- `PurchaseOrderStatus`
- `AccountType`
- `PaymentStatus` now explicitly includes `Authorized`, `Captured`, and `Voided`

## Implementation Notes

- Prefer one canonical property name per relationship or timestamp.
- Avoid alias properties in domain entities.
- Use `long` minor units for money values.
- Use `decimal` for tax or discount rates.
- Use EF Core configuration to enforce indexes, uniqueness, precision, and delete behavior.
