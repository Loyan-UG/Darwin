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

    /// <summary>Add (or increase) an item in cart.</summary>
    public sealed class CartAddItemDto
    {
        public Guid? UserId { get; set; }
        public string? AnonymousId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; } = 1;
        /// <summary>Snapshot unit net price in minor units (e.g., cents).</summary>
        public long UnitPriceNetMinor { get; set; }
        /// <summary>Snapshot VAT rate like 0.19m.</summary>
        public decimal VatRate { get; set; }
        /// <summary>Currency (ISO 4217), e.g., EUR.</summary>
        public string Currency { get; set; } = "EUR";
    }

    /// <summary>Change quantity of a specific cart item.</summary>
    public sealed class CartUpdateQtyDto
    {
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>Remove a specific variant from the cart.</summary>
    public sealed class CartRemoveItemDto
    {
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; }
    }

    /// <summary>Apply or clear a coupon code on a cart.</summary>
    public sealed class CartApplyCouponDto
    {
        public Guid CartId { get; set; }
        public string? CouponCode { get; set; } // null or empty clears
    }

    /// <summary>Lightweight item row for returning cart state to UI.</summary>
    public sealed class CartItemRowDto
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; }
        public long LineNetMinor { get; set; }
        public long LineVatMinor { get; set; }
        public long LineGrossMinor { get; set; }
    }

    /// <summary>Computed cart summary.</summary>
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
