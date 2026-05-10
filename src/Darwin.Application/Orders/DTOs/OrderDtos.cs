using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Orders.DTOs
{
    public enum OrderQueueFilter
    {
        All = 0,
        Open = 1,
        PaymentIssues = 2,
        FulfillmentAttention = 3
    }

    public enum PaymentQueueFilter
    {
        All = 0,
        Failed = 1,
        Refunded = 2
    }

    public enum ShipmentQueueFilter
    {
        All = 0,
        Pending = 1,
        Shipped = 2,
        MissingTracking = 3,
        Returned = 4,
        Dhl = 5,
        MissingService = 6,
        AwaitingHandoff = 7,
        TrackingOverdue = 8,
        CarrierReview = 9,
        ReturnFollowUp = 10
    }

    public enum RefundQueueFilter
    {
        All = 0,
        Pending = 1,
        Completed = 2
    }

    public enum InvoiceQueueFilter
    {
        All = 0,
        Outstanding = 1,
        Paid = 2
    }

    /// <summary>
    /// Input for creating an order. Totals are computed server-side from lines.
    /// </summary>
    public sealed class OrderCreateDto
    {
        public Guid? UserId { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public bool PricesIncludeTax { get; set; } = false;

        public string BillingAddressJson { get; set; } = "{}";
        public string ShippingAddressJson { get; set; } = "{}";

        public List<OrderLineCreateDto> Lines { get; set; } = new();

        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
    }

    /// <summary>
    /// A line to be included in a new order.
    /// </summary>
    public sealed class OrderLineCreateDto
    {
        public Guid VariantId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; } // 0.19m -> 19%
    }

    /// <summary>
    /// Lightweight list item used by Admin orders grid.
    /// Includes RowVersion for concurrency-sensitive actions (e.g., Delete/Cancel).
    /// </summary>
    public sealed class OrderListItemDto
    {
        /// <summary>Order id (primary key).</summary>
        public Guid Id { get; set; }

        /// <summary>Human-friendly order number (unique among non-deleted rows).</summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>3-letter ISO currency code.</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Grand total gross (minor units, e.g., cents).</summary>
        public long GrandTotalGrossMinor { get; set; }

        /// <summary>Current status in the order state machine.</summary>
        public OrderStatus Status { get; set; }

        public int PaymentCount { get; set; }
        public int FailedPaymentCount { get; set; }
        public int ShipmentCount { get; set; }

        /// <summary>Creation timestamp (UTC) for display/sorting.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Concurrency token to enable optimistic concurrency in Admin actions.
        /// Typical usage: pass into hidden field and verify on command execution.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Detailed order view for admin.
    /// </summary>
    public sealed class OrderDetailDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public bool PricesIncludeTax { get; set; }

        public long SubtotalNetMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public long ShippingTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }
        public Guid? ShippingMethodId { get; set; }
        public string? ShippingMethodName { get; set; }
        public string? ShippingCarrier { get; set; }
        public string? ShippingService { get; set; }

        public OrderStatus Status { get; set; }

        public string BillingAddressJson { get; set; } = "{}";
        public string ShippingAddressJson { get; set; } = "{}";

        public List<OrderLineDetailDto> Lines { get; set; } = new();
        public List<PaymentDetailDto> Payments { get; set; } = new();
        public List<ShipmentDetailDto> Shipments { get; set; } = new();
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class OrderLineDetailDto
    {
        public Guid Id { get; set; }
        public Guid VariantId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; }
        public long UnitPriceGrossMinor { get; set; }
        public long LineTaxMinor { get; set; }
        public long LineGrossMinor { get; set; }
    }

    /// <summary>
    /// Input DTO for recording a payment attempt or result against an order.
    /// The status is captured from the payment provider callback/flow.
    /// </summary>
    public sealed class PaymentCreateDto
    {
        /// <summary>
        /// Target order identifier the payment belongs to.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Payment provider key (e.g., "Stripe", "PayPal", "Adyen").
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Provider-side reference/identifier to correlate with webhooks/callbacks.
        /// </summary>
        public string ProviderReference { get; set; } = string.Empty;

        /// <summary>
        /// Amount in minor currency units (e.g., cents).
        /// </summary>
        public long AmountMinor { get; set; }

        /// <summary>
        /// ISO 4217 currency (e.g., "EUR").
        /// </summary>
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;

        /// <summary>
        /// Resulting status of the payment, as persisted in the domain.
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// Optional machine- or human-readable failure reason when Status == Failed.
        /// </summary>
        public string? FailureReason { get; set; }
    }

    public sealed class PaymentDetailDto
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderReference { get; set; } = string.Empty;
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public PaymentStatus Status { get; set; }
        public DateTime? CapturedAtUtc { get; set; }
        public string? FailureReason { get; set; }
    }

    public sealed class ShipmentCreateDto
    {
        public Guid OrderId { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? ProviderShipmentReference { get; set; }
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
        public string? LastCarrierEventKey { get; set; }
        public int? TotalWeight { get; set; }
        public List<ShipmentLineCreateDto> Lines { get; set; } = new();
    }

    public sealed class ShipmentLineCreateDto
    {
        public Guid OrderLineId { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class ApplyShipmentCarrierEventDto
    {
        public string Carrier { get; set; } = string.Empty;
        public string ProviderShipmentReference { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
        public string? Service { get; set; }
        public string CarrierEventKey { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public string? ProviderStatus { get; set; }
        public string? ExceptionCode { get; set; }
        public string? ExceptionMessage { get; set; }
    }

    public sealed class ResolveShipmentCarrierExceptionDto
    {
        public Guid ShipmentId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string ResolutionNote { get; set; } = string.Empty;
    }

    public sealed class ShipmentDetailDto
    {
        public Guid Id { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? ProviderShipmentReference { get; set; }
        public string? TrackingNumber { get; set; }
        public string? TrackingUrl { get; set; }
        public string? LabelUrl { get; set; }
        public int? TotalWeight { get; set; }
        public Darwin.Domain.Enums.ShipmentStatus Status { get; set; }
        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public string? LastCarrierEventKey { get; set; }
        public List<ShipmentCarrierEventListItemDto> RecentCarrierEvents { get; set; } = new();
    }

    public sealed class ShipmentCarrierEventListItemDto
    {
        public string CarrierEventKey { get; set; } = string.Empty;
        public string? ProviderStatus { get; set; }
        public string? ExceptionCode { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
        public string? Service { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }

    public sealed class UpdateOrderStatusDto
    {
        public Guid OrderId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public OrderStatus NewStatus { get; set; }
        public Guid? WarehouseId { get; set; }
    }

    /// <summary>
    /// Lightweight list item used by Admin queries to render a paged grid of payments for a single order.
    /// Includes RowVersion to support inline soft-delete or future concurrency-aware updates.
    /// </summary>
    public sealed class PaymentListItemDto
    {
        /// <summary>Primary key of the payment row.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning order identifier.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Logical provider name (e.g., "PayPal", "Stripe").</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Optional linked invoice identifier when this payment settles an invoice.</summary>
        public Guid? InvoiceId { get; set; }

        /// <summary>Optional invoice status of the linked invoice.</summary>
        public InvoiceStatus? InvoiceStatus { get; set; }

        /// <summary>External reference or transaction id provided by the PSP.</summary>
        public string? ProviderReference { get; set; }

        /// <summary>Payment currency (ISO 4217), typically same as the order's currency.</summary>
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;

        /// <summary>Amount in minor units (e.g., cents).</summary>
        public long AmountMinor { get; set; }

        /// <summary>Current processing state.</summary>
        public Darwin.Domain.Enums.PaymentStatus Status { get; set; }

        /// <summary>Optional failure reason provided by the gateway.</summary>
        public string? FailureReason { get; set; }

        /// <summary>Creation timestamp (UTC) for sorting in UI.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>UTC timestamp when the payment was captured or marked as paid.</summary>
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>Total refunded amount applied against this payment in minor units.</summary>
        public long RefundedAmountMinor { get; set; }

        /// <summary>Remaining net collected amount after completed refunds.</summary>
        public long NetCapturedAmountMinor { get; set; }

        /// <summary>RowVersion for optimistic concurrency in inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Lightweight list item used by Admin queries to render a paged grid of shipments for a single order.
    /// Includes RowVersion to support inline soft-delete or future concurrency-aware updates.
    /// </summary>
    public sealed class ShipmentListItemDto
    {
        /// <summary>Primary key of the shipment row.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning order identifier.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Human-friendly order number for queue views.</summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>Carrier code/name (e.g., "DHL").</summary>
        public string Carrier { get; set; } = string.Empty;

        /// <summary>Service level (e.g., "Standard", "Express").</summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>Optional carrier-side shipment or label reference.</summary>
        public string? ProviderShipmentReference { get; set; }

        /// <summary>Optional tracking number provided by the carrier.</summary>
        public string? TrackingNumber { get; set; }

        /// <summary>Optional carrier tracking URL when the shipment is trackable in phase-1 flows.</summary>
        public string? TrackingUrl { get; set; }

        /// <summary>Optional carrier-hosted label/document URL for operator follow-up.</summary>
        public string? LabelUrl { get; set; }

        /// <summary>Total weight of the shipment, unit decided by SiteSetting/UI.</summary>
        public int TotalWeight { get; set; }

        /// <summary>Current shipment lifecycle state.</summary>
        public Darwin.Domain.Enums.ShipmentStatus Status { get; set; }

        public DateTime? ShippedAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }

        /// <summary>Creation timestamp (UTC) for sorting in UI.</summary>
        public DateTime CreatedAtUtc { get; set; }

        public bool IsDhl { get; set; }
        public bool NeedsCarrierReview { get; set; }
        public bool NeedsReturnFollowUp { get; set; }
        public bool AwaitingHandoff { get; set; }
        public bool TrackingOverdue { get; set; }
        public int OpenAgeHours { get; set; }
        public int? InTransitAgeHours { get; set; }
        public DateTime? LastCarrierEventAtUtc { get; set; }
        public string? LastCarrierEventKey { get; set; }
        public List<ShipmentCarrierEventListItemDto> RecentCarrierEvents { get; set; } = new();
        public string TrackingState { get; set; } = string.Empty;
        public string ExceptionNote { get; set; } = string.Empty;
        public int AttentionDelayHours { get; set; }
        public int TrackingGraceHours { get; set; }
        public Guid? DefaultRefundPaymentId { get; set; }
        public bool HasRefundablePayment { get; set; }
        public bool ProviderOperationQueued { get; set; }
        public bool ProviderOperationFailed { get; set; }
        public string? ProviderOperationType { get; set; }
        public string? ProviderOperationFailureReason { get; set; }

        /// <summary>RowVersion for optimistic concurrency in inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class ShipmentOpsSummaryDto
    {
        public int PendingCount { get; set; }
        public int ShippedCount { get; set; }
        public int MissingTrackingCount { get; set; }
        public int ReturnedCount { get; set; }
        public int DhlCount { get; set; }
        public int MissingServiceCount { get; set; }
        public int AwaitingHandoffCount { get; set; }
        public int TrackingOverdueCount { get; set; }
        public int CarrierReviewCount { get; set; }
        public int ReturnFollowUpCount { get; set; }
    }

    /// <summary>
    /// Input DTO used to create a refund for an existing order payment.
    /// </summary>
    public sealed class RefundCreateDto
    {
        public Guid OrderId { get; set; }
        public Guid PaymentId { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lightweight refund list row used by admin order detail tabs.
    /// </summary>
    public sealed class RefundListItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid PaymentId { get; set; }
        public string PaymentProvider { get; set; } = string.Empty;
        public string? PaymentProviderReference { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string RefundProvider { get; set; } = string.Empty;
        public string? RefundProviderReference { get; set; }
        public string? RefundProviderStatus { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? RequestedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Input DTO used to generate an order invoice snapshot in the CRM invoice table.
    /// </summary>
    public sealed class OrderInvoiceCreateDto
    {
        public Guid OrderId { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? PaymentId { get; set; }
        public DateTime? DueAtUtc { get; set; }
    }

    /// <summary>
    /// Lightweight invoice row shown on order detail screens.
    /// </summary>
    public sealed class OrderInvoiceListItemDto
    {
        public Guid Id { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentProvider { get; set; } = string.Empty;
        public string? PaymentProviderReference { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long SettledAmountMinor { get; set; }
        public long BalanceMinor { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Address snapshot captured during storefront checkout.
    /// </summary>
    public sealed class CheckoutAddressDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string CountryCode { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault;
        public string? PhoneE164 { get; set; }
    }

    /// <summary>
    /// Input DTO for placing an order directly from a cart snapshot.
    /// </summary>
    public sealed class PlaceOrderFromCartDto
    {
        public Guid CartId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? BillingAddressId { get; set; }
        public Guid? ShippingAddressId { get; set; }
        public Guid? SelectedShippingMethodId { get; set; }
        public CheckoutAddressDto? BillingAddress { get; set; }
        public CheckoutAddressDto? ShippingAddress { get; set; }
        public long ShippingTotalMinor { get; set; }
        public string Culture { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCultureDefault;
    }

    /// <summary>
    /// Result DTO returned after a storefront cart is converted into an order.
    /// </summary>
    public sealed class PlaceOrderFromCartResultDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public long GrandTotalGrossMinor { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Created;
    }
}
