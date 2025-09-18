using System;
using System.Collections.Generic;

namespace Darwin.Application.CartCheckout.DTOs
{
    /// <summary>Identifies a cart by either user or anonymous id.</summary>
    public sealed class CartKeyDto
    {
        public Guid? UserId { get; set; }
        public string? AnonymousId { get; set; }
    }

    /// <summary>
    /// Add (or increase) an item in cart. The server remains the source of truth for prices,
    /// but we keep snapshots for traceability and debugging.
    /// </summary>
    public sealed class CartAddItemDto
    {
        public Guid? UserId { get; set; }
        public string? AnonymousId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; } = 1;

        /// <summary>Optional client snapshot: unit net minor (server may recompute/override).</summary>
        public long? UnitPriceNetMinor { get; set; }

        /// <summary>Optional client snapshot: VAT rate (server may recompute/override).</summary>
        public decimal? VatRate { get; set; }

        /// <summary>Currency (ISO 4217); if null, server defaults to SiteSetting.</summary>
        public string? Currency { get; set; }

        /// <summary>Selected add-on value IDs to apply for this cart line.</summary>
        public List<Guid> SelectedAddOnValueIds { get; set; } = new();
    }

    /// <summary>Change quantity of a specific cart item.</summary>
    public sealed class CartUpdateQtyDto
    {
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; }

        /// <summary>
        /// Optional JSON array that identifies the exact cart line when same variant exists
        /// with different add-on selections. If null, the handler may match the first line.
        /// </summary>
        public string? SelectedAddOnValueIdsJson { get; set; }

        /// <summary>
        /// New quantity. If zero, the line should be removed (soft-delete).
        /// </summary>
        public int Quantity { get; set; }
    }

    /// <summary>Remove a specific variant (and add-on configuration) from the cart.</summary>
    public sealed class CartRemoveItemDto
    {
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; }
        public string? SelectedAddOnValueIdsJson { get; set; }
    }

    /// <summary>Apply or clear a coupon code on a cart.</summary>
    public sealed class CartApplyCouponDto
    {
        public Guid CartId { get; set; }

        /// <summary>
        /// Coupon code to apply. Set to null or empty to clear an existing coupon.
        /// </summary>
        public string? CouponCode { get; set; }
    }

    public sealed class CartItemRowDto
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public long AddOnPriceDeltaMinor { get; set; }
        public decimal VatRate { get; set; }
        public long LineNetMinor { get; set; }
        public long LineVatMinor { get; set; }
        public long LineGrossMinor { get; set; }
        public string SelectedAddOnValueIdsJson { get; set; } = "[]";
    }

    public sealed class CartSummaryDto
    {
        public Guid CartId { get; set; }
        public string Currency { get; set; } = "EUR";
        public List<CartItemRowDto> Items { get; set; } = new();
        public long SubtotalNetMinor { get; set; }
        public long VatTotalMinor { get; set; }
        public long GrandTotalGrossMinor { get; set; }
        public string? CouponCode { get; set; }
    }
}
