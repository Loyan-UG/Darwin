using System;
using System.Collections.Generic;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.CartCheckout
{
    /// <summary>
    /// Shopping cart owned by a user or anonymous session. Stores a snapshot of pricing inputs (currency, tax mode).
    /// </summary>
    public sealed class Cart : BaseEntity
    {
        /// <summary>Optional registered user who owns the cart; otherwise anonymous.</summary>
        public Guid? UserId { get; set; }
        /// <summary>Anonymous identifier to merge carts upon sign-in.</summary>
        public string? AnonymousId { get; set; }
        /// <summary>ISO currency code; phase 1 fixed to EUR.</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Cart items.</summary>
        public List<CartItem> Items { get; set; } = new();
        /// <summary>Optional applied coupon code (validated against Promotion).</summary>
        public string? CouponCode { get; set; }
    }


    /// <summary>
    /// Cart line item with quantity and unit price (net) at the time of addition.
    /// </summary>
    public sealed class CartItem : BaseEntity
    {
        /// <summary>Owning cart id.</summary>
        public Guid CartId { get; set; }
        /// <summary>Product variant id being purchased.</summary>
        public Guid VariantId { get; set; }
        /// <summary>Ordered quantity (must respect min/max/step rules).</summary>
        public int Quantity { get; set; }
        /// <summary>Unit price net snapshot (minor units) when added to cart.</summary>
        public long UnitPriceNetMinor { get; set; }
        /// <summary>VAT rate (e.g., 0.19 for 19%). Stored to estimate gross line totals in cart stage.</summary>
        public decimal VatRate { get; set; }
    }
}