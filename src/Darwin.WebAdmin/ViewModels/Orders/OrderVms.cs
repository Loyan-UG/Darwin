using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.ViewModels.CRM;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Darwin.WebAdmin.ViewModels.Orders
{
    /// <summary>List page VM for Orders.</summary>
    public sealed class OrdersListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public OrderQueueFilter Filter { get; set; } = OrderQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<OrderListItemVm> Items { get; set; } = new();
    }

    public sealed class OrderListItemVm
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public long GrandTotalGrossMinor { get; set; }
        public OrderStatus Status { get; set; }
        public int PaymentCount { get; set; }
        public int FailedPaymentCount { get; set; }
        public int ShipmentCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Details page VM including lines (payments/shipments are loaded via partials).</summary>
    public sealed class OrderDetailVm
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool PricesIncludeTax { get; set; }
        public long SubtotalNetMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public long DiscountTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ShippingMethodId { get; set; }
        public string? ShippingMethodName { get; set; }
        public string? ShippingCarrier { get; set; }
        public string? ShippingService { get; set; }
        public long ShippingTotalMinor { get; set; }
        public TaxPolicySnapshotVm TaxPolicy { get; set; } = new();
        public ReturnSupportBaselineVm ReturnSupport { get; set; } = new();
        public OrderStatus Status { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid? SelectedWarehouseId { get; set; }
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<OrderLineVm> Lines { get; set; } = new();
    }

    public sealed class OrderLineVm
    {
        public Guid VariantId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long UnitPriceGrossMinor { get; set; }
        public long LineGrossMinor { get; set; }
    }

    /// <summary>Header info reused on AddPayment screen.</summary>
    public sealed class OrderHeaderVm
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public long GrandTotalGrossMinor { get; set; }
    }

    /// <summary>
    /// Form VM for creating a payment in Admin. Mirrors PaymentCreateDto.
    /// </summary>
    public sealed class PaymentCreateVm
    {
        /// <summary>Owning order identifier; provided via route/hidden field.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Payment provider (e.g., Stripe, PayPal).</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Provider-side reference (charge/intent id).</summary>
        public string ProviderReference { get; set; } = string.Empty;

        /// <summary>Amount in minor units (e.g., cents).</summary>
        public long AmountMinor { get; set; }

        /// <summary>Currency ISO-4217 (must match order currency, enforced in handler).</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Initial status. Admin may directly record captured payments.</summary>
        public PaymentStatus Status { get; set; }

        /// <summary>Human-friendly reason when Status=Failed.</summary>
        public string? FailureReason { get; set; }
    }

    /// <summary>Partial grid VM for payments.</summary>
    public sealed class OrderPaymentsPageVm
    {
        public Guid OrderId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public PaymentQueueFilter Filter { get; set; } = PaymentQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<PaymentListItemVm> Items { get; set; } = new();
    }

    public sealed class PaymentListItemVm
    {
        /// <summary>Primary key of the payment row.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning order identifier.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Logical provider name (e.g., "PayPal", "Stripe").</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Optional linked invoice identifier when the payment settles an invoice.</summary>
        public Guid? InvoiceId { get; set; }

        /// <summary>Optional linked invoice status.</summary>
        public InvoiceStatus? InvoiceStatus { get; set; }

        /// <summary>External reference or transaction id provided by the PSP.</summary>
        public string? ProviderReference { get; set; }

        /// <summary>Payment currency (ISO 4217), typically same as the order's currency.</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Amount in minor units (e.g., cents).</summary>
        public long AmountMinor { get; set; }

        /// <summary>Current processing state.</summary>
        public PaymentStatus Status { get; set; }

        /// <summary>Optional failure reason provided by the gateway.</summary>
        public string? FailureReason { get; set; }

        /// <summary>Creation timestamp (UTC) for sorting in UI.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>UTC timestamp when the payment was marked as paid/captured.</summary>
        public DateTime? PaidAtUtc { get; set; }

        public long RefundedAmountMinor { get; set; }
        public long NetCapturedAmountMinor { get; set; }

        /// <summary>RowVersion for optimistic concurrency in inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Partial grid VM for shipments.</summary>
    public sealed class OrderShipmentsPageVm
    {
        public Guid OrderId { get; set; }
        public Guid? DefaultRefundPaymentId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public ShipmentQueueFilter Filter { get; set; } = ShipmentQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<ShipmentListItemVm> Items { get; set; } = new();
    }

    public sealed class ShipmentListItemVm
    {
        /// <summary>Primary key of the shipment row.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning order identifier.</summary>
        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>Carrier code/name (e.g., "DHL").</summary>
        public string Carrier { get; set; } = string.Empty;

        /// <summary>Service level (e.g., "Standard", "Express").</summary>
        public string Service { get; set; } = string.Empty;

        public string? ProviderShipmentReference { get; set; }

        /// <summary>Optional tracking number provided by the carrier.</summary>
        public string? TrackingNumber { get; set; }

        /// <summary>Optional carrier tracking URL when the shipment is trackable in phase-1 flows.</summary>
        public string? TrackingUrl { get; set; }

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
        public List<ShipmentCarrierEventVm> RecentCarrierEvents { get; set; } = new();
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

    public sealed class ShipmentCarrierEventVm
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

    public sealed class ShipmentCreateVm
    {
        public Guid OrderId { get; set; }

        [Required]
        [StringLength(64)]
        public string Carrier { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string Service { get; set; } = string.Empty;

        [StringLength(128)]
        public string? ProviderShipmentReference { get; set; }

        [StringLength(64)]
        public string? TrackingNumber { get; set; }

        [StringLength(2048)]
        public string? LabelUrl { get; set; }

        [StringLength(128)]
        public string? LastCarrierEventKey { get; set; }

        public int? TotalWeight { get; set; }
        public List<ShipmentLineCreateVm> Lines { get; set; } = new();
    }

    public sealed class ShipmentLineCreateVm
    {
        public Guid OrderLineId { get; set; }
        public string Label { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    public sealed class RefundCreateVm
    {
        public Guid OrderId { get; set; }

        [Required]
        public Guid PaymentId { get; set; }

        [Range(1, long.MaxValue)]
        public long AmountMinor { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string Reason { get; set; } = string.Empty;

        public List<SelectListItem> PaymentOptions { get; set; } = new();
    }

    public sealed class OrderRefundsPageVm
    {
        public Guid OrderId { get; set; }
        public int ReturnedShipmentCount { get; set; }
        public Guid? DefaultRefundPaymentId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public RefundQueueFilter Filter { get; set; } = RefundQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<RefundListItemVm> Items { get; set; } = new();
    }

    public sealed class ReturnSupportBaselineVm
    {
        public int ReturnedShipmentCount { get; set; }
        public int ReturnedWithoutTrackingCount { get; set; }
        public int CarrierReviewShipmentCount { get; set; }
        public bool HasRefundablePayment { get; set; }
        public Guid? DefaultRefundPaymentId { get; set; }
    }

    public sealed class RefundListItemVm
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public string PaymentProvider { get; set; } = string.Empty;
        public string? PaymentProviderReference { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string RefundProvider { get; set; } = string.Empty;
        public string? RefundProviderReference { get; set; }
        public string? RefundProviderStatus { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? RequestedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class OrderInvoiceCreateVm
    {
        public Guid OrderId { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? PaymentId { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> PaymentOptions { get; set; } = new();
    }

    public sealed class OrderInvoicesPageVm
    {
        public Guid OrderId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public InvoiceQueueFilter Filter { get; set; } = InvoiceQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<OrderInvoiceListItemVm> Items { get; set; } = new();
    }

    public sealed class OrderInvoiceListItemVm
    {
        public Guid Id { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentProvider { get; set; } = string.Empty;
        public string? PaymentProviderReference { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public string Currency { get; set; } = string.Empty;
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

    /// <summary>Form VM to change order status (concurrency-aware).</summary>
    public sealed class OrderStatusChangeVm
    {
        public Guid OrderId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public OrderStatus NewStatus { get; set; }
        public Guid? WarehouseId { get; set; }
    }









    /// <summary>
    /// Paged list VM for payments of a single order in Admin.
    /// </summary>
    public sealed class OrderPaymentsListVm
    {
        /// <summary>Order being viewed (route parameter echo).</summary>
        public Guid OrderId { get; set; }

        /// <summary>Current page items projected in Application layer.</summary>
        public List<PaymentListItemVm> Items { get; set; } = new();

        /// <summary>1-based page number.</summary>
        public int Page { get; set; }

        /// <summary>Items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of items matching current filter.</summary>
        public int Total { get; set; }

        /// <summary>Prebuilt page-size options for a dropdown.</summary>
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// Paged list VM for shipments of a single order in Admin.
    /// </summary>
    public sealed class OrderShipmentsListVm
    {
        /// <summary>Order being viewed (route parameter echo).</summary>
        public Guid OrderId { get; set; }

        /// <summary>Current page items projected in Application layer.</summary>
        public List<ShipmentListItemVm> Items { get; set; } = new();

        /// <summary>1-based page number.</summary>
        public int Page { get; set; }

        /// <summary>Items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of items matching current filter.</summary>
        public int Total { get; set; }

        /// <summary>Prebuilt page-size options for a dropdown.</summary>
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
    }

    public sealed class ShipmentsQueueVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public ShipmentQueueFilter Filter { get; set; } = ShipmentQueueFilter.All;
        public DhlOperationsVm Dhl { get; set; } = new();
        public ShipmentOpsSummaryVm Summary { get; set; } = new();
        public List<ShipmentPlaybookVm> Playbooks { get; set; } = new();
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
        public List<ShipmentListItemVm> Items { get; set; } = new();
    }

    public enum ReturnQueueFilter
    {
        All = 0,
        FollowUp = 1,
        CarrierReview = 2
    }

    public sealed class ReturnsQueueVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public ReturnQueueFilter Filter { get; set; } = ReturnQueueFilter.All;
        public ShipmentOpsSummaryVm Summary { get; set; } = new();
        public List<ShipmentPlaybookVm> Playbooks { get; set; } = new();
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
        public List<ShipmentListItemVm> Items { get; set; } = new();
    }

    public sealed class ShipmentProviderOperationsListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool StalePendingOnly { get; set; }
        public bool FailedOnly { get; set; }
        public ShipmentProviderOperationSummaryVm Summary { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ProviderItems { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> OperationTypeItems { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusItems { get; set; } = new List<SelectListItem>();
        public List<ShipmentProviderOperationListItemVm> Items { get; set; } = new();
    }

    public sealed class ShipmentProviderOperationSummaryVm
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int StalePendingCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public sealed class ShipmentProviderOperationListItemVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid ShipmentId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public DateTime? LastAttemptAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int AgeMinutes { get; set; }
        public bool IsStalePending { get; set; }
        public string? FailureReason { get; set; }
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
    }

    public sealed class DhlOperationsVm
    {
        public bool Enabled { get; set; }
        public bool ApiBaseUrlConfigured { get; set; }
        public bool ApiCredentialsConfigured { get; set; }
        public bool AccountNumberConfigured { get; set; }
        public bool ShipperIdentityConfigured { get; set; }
        public string EnvironmentLabel { get; set; } = string.Empty;
        public int ShipmentAttentionDelayHours { get; set; }
        public int ShipmentTrackingGraceHours { get; set; }
    }

    public sealed class ShipmentOpsSummaryVm
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

    public sealed class ShipmentPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string SettingsDependency { get; set; } = string.Empty;
    }

    
}
