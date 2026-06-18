using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.DTOs
{
    public enum SalesOrderDocumentFilter
    {
        All = 0,
        Open = 1,
        Paid = 2,
        FulfillmentAttention = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum SalesInvoiceDocumentFilter
    {
        All = 0,
        Draft = 1,
        Open = 2,
        DueSoon = 3,
        Overdue = 4,
        Paid = 5,
        Archived = 6
    }

    public sealed class SalesOverviewDto
    {
        public int OrderCount { get; set; }
        public int QuoteCount { get; set; }
        public int QuoteAttentionCount { get; set; }
        public int ReturnOrderCount { get; set; }
        public int ReturnOrderAttentionCount { get; set; }
        public long GrossTotalMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public int InvoiceCount { get; set; }
        public long OpenInvoiceBalanceMinor { get; set; }
        public int PaymentAttentionCount { get; set; }
        public int FulfillmentAttentionCount { get; set; }
        public int InvoiceAttentionCount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public IReadOnlyList<SalesChannelBreakdownDto> ChannelBreakdown { get; set; } = Array.Empty<SalesChannelBreakdownDto>();
    }

    public sealed class SalesChannelBreakdownDto
    {
        public SalesChannel SalesChannel { get; set; }
        public int OrderCount { get; set; }
        public long GrossTotalMinor { get; set; }
    }

    public sealed class SalesOrderListItemDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public SalesChannel SalesChannel { get; set; }
        public DateTime OrderedAtUtc { get; set; }
        public OrderStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long GrandTotalGrossMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public int LineCount { get; set; }
        public int PaymentCount { get; set; }
        public int FailedPaymentCount { get; set; }
        public int ShipmentCount { get; set; }
        public int InvoiceCount { get; set; }
    }

    public sealed class SalesInvoiceListItemDto
    {
        public Guid Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? PaymentId { get; set; }
        public string? OrderNumber { get; set; }
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public long BalanceMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool HasIssuedSnapshot { get; set; }
        public bool HasArchiveMetadata { get; set; }
    }

    /// <summary>
    /// Internal sales projection over the current order aggregate. This is not a public contract.
    /// </summary>
    public sealed class SalesOrderDocumentDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool PricesIncludeTax { get; set; }
        public SalesChannel SalesChannel { get; set; }
        public DateTime OrderedAtUtc { get; set; }
        public long SubtotalNetMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }
        public OrderStatus Status { get; set; }
        public string BillingAddressJson { get; set; } = "{}";
        public string ShippingAddressJson { get; set; } = "{}";
        public Guid? ShippingMethodId { get; set; }
        public string? ShippingMethodName { get; set; }
        public string? ShippingCarrier { get; set; }
        public string? ShippingService { get; set; }
        public string? InternalNotes { get; set; }
        public IReadOnlyList<SalesOrderLineDocumentDto> Lines { get; set; } = Array.Empty<SalesOrderLineDocumentDto>();
        public IReadOnlyList<SalesDocumentSettlementDto> Settlements { get; set; } = Array.Empty<SalesDocumentSettlementDto>();
        public IReadOnlyList<SalesDocumentFulfillmentDto> Fulfillments { get; set; } = Array.Empty<SalesDocumentFulfillmentDto>();
        public IReadOnlyList<SalesInvoiceDocumentSummaryDto> Invoices { get; set; } = Array.Empty<SalesInvoiceDocumentSummaryDto>();
    }

    public sealed class SalesOrderLineDocumentDto
    {
        public Guid Id { get; set; }
        public Guid? VariantId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; }
        public long UnitPriceGrossMinor { get; set; }
        public long LineTaxMinor { get; set; }
        public long LineGrossMinor { get; set; }
        public string AddOnValueIdsJson { get; set; } = "[]";
        public long AddOnPriceDeltaMinor { get; set; }
    }

    public sealed class SalesDocumentSettlementDto
    {
        public Guid Id { get; set; }
        public Guid? InvoiceId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? UserId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTransactionReference { get; set; }
        public string? ProviderPaymentIntentReference { get; set; }
        public string? ProviderCheckoutSessionReference { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public string? FailureReason { get; set; }
    }

    public sealed class SalesDocumentFulfillmentDto
    {
        public Guid Id { get; set; }
        public Guid? MethodId { get; set; }
        public ShipmentStatus Status { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? ProviderShipmentReference { get; set; }
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
        public int? TotalWeight { get; set; }
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
    }

    public sealed class SalesInvoiceDocumentSummaryDto
    {
        public Guid Id { get; set; }
        public Guid? PaymentId { get; set; }
        public string? InvoiceNumber { get; set; }
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool HasIssuedSnapshot { get; set; }
        public bool HasArchiveMetadata { get; set; }
    }

    /// <summary>
    /// Internal sales projection over the current shared invoice aggregate. This is not a public contract.
    /// </summary>
    public sealed class SalesInvoiceDocumentDto
    {
        public Guid Id { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public string? InvoiceNumber { get; set; }
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool? ReverseChargeApplied { get; set; }
        public DateTime? ReverseChargeReviewedAtUtc { get; set; }
        public string? ReverseChargeReviewNote { get; set; }
        public string? IssuedSnapshotJson { get; set; }
        public string? IssuedSnapshotHashSha256 { get; set; }
        public DateTime? ArchiveGeneratedAtUtc { get; set; }
        public DateTime? ArchiveRetainUntilUtc { get; set; }
        public string? ArchiveRetentionPolicyVersion { get; set; }
        public DateTime? ArchivePurgedAtUtc { get; set; }
        public string? ArchivePurgeReason { get; set; }
        public IReadOnlyList<SalesInvoiceLineDocumentDto> Lines { get; set; } = Array.Empty<SalesInvoiceLineDocumentDto>();
    }

    public sealed class SalesInvoiceLineDocumentDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal TaxRate { get; set; }
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
    }
}
