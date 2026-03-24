# Darwin Domain Design

This document describes the domain model of the Darwin platform. It consolidates existing entities from the current code base and extends them with new modules for CRM, multi-warehouse inventory, and billing.

All classes reside in the `Darwin.Domain` project, are declared `sealed`, use `Guid` keys, and use non-nullable string properties unless explicitly noted. Value converters are used for JSON fields (such as addresses), and enums are strictly validated in the Application layer.

The goal is to provide a flexible yet clear model that can scale from small shops to more complex operations.

## Catalog

### Product

Products represent sellable items. A product may be simple (no variants), variant (multiple options), or bundled. The existing structure is retained with these clarifications:

- `Id: Guid` — unique identifier.
- `BrandId: Guid?` — optional reference to Brand.
- `PrimaryCategoryId: Guid?` — optional reference to Category.
- `Kind: ProductKind` — enum values: `Simple`, `Variant`, `Bundle`.
- `IsActive: bool` — available for sale.
- `IsVisible: bool` — appears in listings.
- `PublishStartUtc`, `PublishEndUtc: DateTime?` — optional publishing window.
- `Translations: List<ProductTranslation>` — per-culture fields (`Name`, `Slug`, `ShortDescription`, `FullDescriptionHtml`, SEO metadata).
- `Media: List<ProductMedia>` — attachments (images/videos/documents) via many-to-many join.
- `Options: List<ProductOption>` — option definitions (e.g., color/size) with values.
- `Variants: List<ProductVariant>` — SKU, price, VAT rate, dimensions, and weight. Inventory is not stored here in multi-warehouse design.
- `RelatedProductIds: List<Guid>` — recommendation links.

### Category & Brand

- Categories form a tree via `ParentId` and include translations.
- Brands include translated `Name`, `Description`, and `Slug` fields.
- Both are soft-deletable and publishable.

### Add-Ons

Add-on groups allow optional extras (e.g., toppings) to be attached to products, categories, brands, or variants.

- `AddOnGroup`, `AddOnOption`, and `AddOnOptionValue` remain unchanged.
- Assignments are handled via join tables.
- Each option value includes a price delta and currency.

## CMS

### Page

- `Id: Guid`
- `Title`, `Slug`, `ContentHtml`, `MetaTitle`, `MetaDescription: string`
- `IsPublished: bool`
- `PublishStartUtc`, `PublishEndUtc: DateTime?`
- `Translations: List<PageTranslation>`

### Menu & MenuItem

- `Menu`: `Id`, `Name`, `Culture`, `Items: List<MenuItem>`
- `MenuItem`: `Id`, `MenuId`, `ParentId?`, `Title`, `Url`, `Order`, `Children: List<MenuItem>`, `Translations: List<MenuItemTranslation>`

## Identity

- **User**: username/email/password hash/security stamp/2FA flags/consents/preferences, linked to roles via `UserRole`; includes navigations for devices, external logins, tokens, engagement snapshots, WebAuthn credentials.
- **Role**: `Id`, `Key`, `DisplayName`, linked to `Permission` via `RolePermission`.
- **Permission**: `Id`, `Key`, `DisplayName`, `Description`, `Area`.
- **Address**: `FullName`, `Company`, `Street`, `City`, `State`, `PostalCode`, `CountryCode`, `Phone`, plus default billing/shipping flags.

## Orders

- **Order**: numbering (`OrderNumber`), user relation (`UserId`), totals (`SubtotalNetMinor`, `TaxTotalMinor`, `ShippingTotalMinor`, `DiscountTotalMinor`, `GrandTotalGrossMinor`), `PricesIncludeTax`, `Status` (`Created`, `Paid`, `Shipped`, `Cancelled`, `Completed`), snapshots (`BillingAddressJson`, `ShippingAddressJson`), `Lines`, `Payments`, `Shipments`, and `InternalNotes`.
- **OrderLine**: `Id`, `OrderId`, `Name`, `Sku`, `Quantity`, `UnitPriceNetMinor`, `VatRate`, `LineGrossMinor`, `AddOnValueIdsJson`, `AddOnPriceDeltaMinor`.
- **Payment**: `Id`, `OrderId`, `AmountMinor`, `Currency`, `Status` (`Pending`, `Completed`, `Failed`, `Refunded`), provider metadata, timestamps.
- **Shipment**: `Id`, `OrderId`, `MethodId`, `Status`, `TrackingNumber`, `ShippedAtUtc?`, `DeliveredAtUtc?`, and `ShipmentLine` list.
- **Refund**: `Id`, `PaymentId`, `AmountMinor`, `Currency`, `Reason`, `Status` (`Pending`, `Completed`, `Failed`), timestamps.

## Inventory

Inventory moves from variant entity into dedicated tables:

- **Warehouse**: `Id`, `Name`, `Address?`, `IsDefault` (exactly one default warehouse).
- **StockLevel**: `WarehouseId`, `VariantId`, `OnHand`, `Reserved`, `ReorderPoint?` (composite key: `WarehouseId + VariantId`).
- **StockTransfer**: `Id`, `FromWarehouseId`, `ToWarehouseId`, `VariantId`, `Quantity`, `Reason`, `CreatedAtUtc`.

Operational behavior:

- Cart add reserves stock (`ReserveStockHandler`).
- Cart/order cancellation releases reserved stock (`ReleaseStockReservationHandler`).
- Order completion reduces on-hand + reserved.
- Transfers adjust on-hand between warehouses.

## CRM

CRM links orders and loyalty but does not require Identity user existence.

- **Customer**: `Id`, `FirstName`, `LastName`, `Email`, `Phone`, `CompanyName?`, `Notes?`, `LoyaltyPointsTotal`, timestamps, and collections (`CustomerSegments`, `Addresses`, `Interactions`, `Consents`, `Invoices`).
- **CustomerAddress**: `Id`, `CustomerId`, `Line1`, `Line2?`, `City`, `State?`, `PostalCode`, `Country`, `IsDefaultShipping`, `IsDefaultBilling`.
- **CustomerSegment**: `Id`, `Name`, `Description?`.
- **CustomerSegmentMembership**: `CustomerId`, `CustomerSegmentId`.
- **Interaction**: `Id`, `CustomerId`, `Type` (`Email`, `Call`, `Meeting`, `Order`, `Support`), `Subject?`, `Content?`, `Channel` (`Email`, `Phone`, `Chat`, `InPerson`), `CreatedAtUtc`, `UserId?`.
- **Consent**: `Id`, `CustomerId`, `Type` (`MarketingEmail`, `SMS`, `TermsOfService`), `Granted`, `GrantedAtUtc`, `RevokedAtUtc?`.
- **LoyaltyPointEntry**: `Id`, `CustomerId`, `Points`, `Reason`, `ReferenceId?`, `CreatedAtUtc`.
- **Invoice**: `Id`, `CustomerId`, `OrderId?`, `Status` (`Draft`, `Open`, `Paid`, `Cancelled`), `Currency`, `TotalNetMinor`, `TotalTaxMinor`, `TotalGrossMinor`, `DueDateUtc`, `PaidAtUtc?`, `CreatedAtUtc`, `Lines`.
- **InvoiceLine**: `Id`, `InvoiceId`, `Description`, `Quantity`, `UnitPriceNetMinor`, `TaxRate`, `TotalNetMinor`, `TotalGrossMinor`.

## Billing

Billing builds on CRM invoices and tracks payments:

- **InvoicePayment**: `Id`, `InvoiceId`, `AmountMinor`, `Currency`, `Provider`, `ProviderTransactionId?`, `Status` (`Pending`, `Completed`, `Failed`, `Refunded`), `CreatedAtUtc`, `CompletedAtUtc?`.
- Invoices can be manual or order-linked.
- Payment updates invoice status and can trigger order state updates via handlers.

## Pricing & Promotions

The pricing domain remains unchanged:

- **Promotion**: `Id`, `Name`, `Code`, `Type` (`Percent`, `Amount`), `Value`, `Currency?`, `MinimumSubtotalMinor?`, `ConditionsJson`, `StartAtUtc`, `EndAtUtc`, `RedemptionLimit?`, `Active`.
- **PromotionRedemption**: `Id`, `PromotionId`, `UserId?`, `OrderId?`, `RedeemedAtUtc`.

## Loyalty

- **LoyaltyProgram**: `Id`, `BusinessId`, `Name`, `AccrualMode` (`PerVisit`/`PerCurrencyUnit`), `PointsPerCurrencyUnit`, `RulesJson`, `Active`, reward tiers.
- **LoyaltyRewardRedemption**: `Id`, `LoyaltyAccountId`, `BusinessId`, `TierId`, `PointsSpent`, `Status` (`Pending`, `Completed`, `Cancelled`), `LocationId?`, `MetadataJson`, `RedeemedAtUtc?`.
- **LoyaltyPointsTransaction**: `Id`, `LoyaltyAccountId`, `BusinessId`, `Type` (`Accrual`, `Redemption`, `Adjustment`), `Points`, `ReferenceId?`, `UserId?`, `CreatedAtUtc`.

Loyalty remains outside front-office scope and is consumed through APIs.

## Integration & Marketing

Infrastructure entities stay largely unchanged:

- **WebhookSubscription**: `Id`, `Url`, `Secret`, `Events`, `IsActive`.
- **WebhookDelivery**: `Id`, `SubscriptionId`, `Attempt`, `Status`, `ResponseCode`, `SentAtUtc`, `DeliveredAtUtc?`, `ErrorMessage?`.
- **AnalyticsExportJob**: `Id`, `BusinessId`, `ReportType`, `Format`, `Status`, `ParametersJson`, `StartedAtUtc?`, `FinishedAtUtc?`, `ErrorMessage?`.
- **AnalyticsExportFile**: `Id`, `JobId`, `FileName`, `Url`, `Format`, `CreatedAtUtc`.

## Relationships

- `Order` links to `User` optionally and to `Customer` via `Invoice` in billing workflows.
- `Invoice` may be order-linked or standalone; `InvoicePayment` always links to `Invoice`.
- `Customer` may map to a `User` (1:1 common in B2C) or exist standalone (B2B/guest).
- `LoyaltyPointEntry` and `Consent` link to `Customer`.
- `StockLevel` links `Warehouse` and `ProductVariant`, decoupling inventory from catalog.

## Implementation Notes

1. Use `Guid` for all PKs and `DateTime`/`DateTime?` in UTC.
2. Keep all domain classes `sealed`.
3. Use record types only for DTOs; entities remain classes.
4. Apply `RowVersion` on aggregate roots (`Product`, `Order`, `Customer`, `Invoice`, `Warehouse`).
5. Seed a default warehouse (`Main warehouse`) and optionally a default loyalty program.
6. Prefer composition over inheritance.
7. Document entities with English XML comments for maintainability and generated docs.

This domain design is the reference for implementing missing modules and keeping the platform consistent.
