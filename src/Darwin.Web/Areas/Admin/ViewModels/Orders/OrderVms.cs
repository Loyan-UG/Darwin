using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Orders
{
    /// <summary>List page VM for Orders.</summary>
    public sealed class OrdersListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<OrderListItemVm> Items { get; set; } = new();
    }

    public sealed class OrderListItemVm
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public long GrandTotalGrossMinor { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Details page VM including lines (payments/shipments are loaded via partials).</summary>
    public sealed class OrderDetailVm
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public long GrandTotalGrossMinor { get; set; }
        public OrderStatus Status { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<OrderLineVm> Lines { get; set; } = new();
    }

    public sealed class OrderLineVm
    {
        public Guid VariantId { get; set; }
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
        public string Currency { get; set; } = "EUR";
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
        public string Currency { get; set; } = "EUR";

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

        /// <summary>External reference or transaction id provided by the PSP.</summary>
        public string? ProviderReference { get; set; }

        /// <summary>Payment currency (ISO 4217), typically same as the order's currency.</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>Amount in minor units (e.g., cents).</summary>
        public long AmountMinor { get; set; }

        /// <summary>Current processing state.</summary>
        public PaymentStatus Status { get; set; }

        /// <summary>Optional failure reason provided by the gateway.</summary>
        public string? FailureReason { get; set; }

        /// <summary>Creation timestamp (UTC) for sorting in UI.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>RowVersion for optimistic concurrency in inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Partial grid VM for shipments.</summary>
    public sealed class OrderShipmentsPageVm
    {
        public Guid OrderId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<ShipmentListItemVm> Items { get; set; } = new();
    }

    public sealed class ShipmentListItemVm
    {
        /// <summary>Primary key of the shipment row.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning order identifier.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Carrier code/name (e.g., "DHL").</summary>
        public string Carrier { get; set; } = string.Empty;

        /// <summary>Service level (e.g., "Standard", "Express").</summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>Optional tracking number provided by the carrier.</summary>
        public string? TrackingNumber { get; set; }

        /// <summary>Total weight of the shipment, unit decided by SiteSetting/UI.</summary>
        public int TotalWeight { get; set; }

        /// <summary>Current shipment lifecycle state.</summary>
        public Darwin.Domain.Enums.ShipmentStatus Status { get; set; }

        /// <summary>Creation timestamp (UTC) for sorting in UI.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>RowVersion for optimistic concurrency in inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>Form VM to change order status (concurrency-aware).</summary>
    public sealed class OrderStatusChangeVm
    {
        public Guid OrderId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public OrderStatus NewStatus { get; set; }
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

    
}
