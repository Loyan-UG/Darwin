using System;
using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.CartCheckout
{
    /// <summary>
    /// Shopping cart owned by a user or anonymous session. It stores a minimal snapshot of
    /// pricing inputs (currency, tax) and – for add-ons – the selected value IDs and their total
    /// delta in minor units to make the cart self-contained and auditable.
    /// </summary>
    public sealed class Cart : BaseEntity
    {
        /// <summary>Optional registered user who owns the cart; otherwise anonymous.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Anonymous identifier used to merge carts upon sign-in.</summary>
        public string? AnonymousId { get; set; }

        /// <summary>ISO currency code. Phase 1: default "EUR".</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>Optional applied coupon code (validated against Promotion).</summary>
        public string? CouponCode { get; set; }

        /// <summary>Cart line items.</summary>
        public List<CartItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Cart line item holding unit net price snapshot and VAT rate, plus optional add-on selections.
    /// Add-on selections are stored as a compact JSON array of AddOnOptionValue IDs to keep the entity
    /// EF-friendly without complex navigation properties in the Application layer.
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

        /// <summary>VAT rate snapshot (e.g., 0.19 for 19%).</summary>
        public decimal VatRate { get; set; }

        /// <summary>
        /// Selected AddOnOptionValue IDs encoded as JSON array (e.g., "[\"guid1\",\"guid2\"]").
        /// This keeps the cart independent from eager loading and simplifies persistence.
        /// </summary>
        public string SelectedAddOnValueIdsJson { get; set; } = "[]";

        /// <summary>
        /// Snapshot of total add-on price delta (minor units, net) for this line.
        /// It is persisted to avoid re-computing on every view and for auditability.
        /// </summary>
        public long AddOnPriceDeltaMinor { get; set; } = 0;
    }
}
