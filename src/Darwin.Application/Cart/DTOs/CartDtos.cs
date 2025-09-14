using System;
using System.Collections.Generic;

namespace Darwin.Application.Cart.DTOs
{
    /// <summary>
    /// Represents a single item in the cart with quantity and pricing.
    /// </summary>
    public sealed class CartItemDto
    {
        public Guid Id { get; set; }
        public Guid VariantId { get; set; }              // product variant identifier
        public int Quantity { get; set; }                // number of units
        public long UnitPriceNetMinor { get; set; }      // price per unit in minor currency units
        public decimal VatRate { get; set; }             // VAT rate as a decimal (e.g. 0.19 for 19%)
    }

    /// <summary>
    /// DTO for creating a new cart. Items may be empty initially.
    /// </summary>
    public sealed class CartCreateDto
    {
        public Guid? UserId { get; set; }                // logged-in user ID (optional)
        public Guid? AnonymousId { get; set; }           // visitor ID if anonymous
        public string Currency { get; set; } = "EUR";    // currency code (e.g. "EUR")
        public List<CartItemDto> Items { get; set; } = new();
        public string? CouponCode { get; set; }          // promotional code, optional
    }

    /// <summary>
    /// DTO to add an item to an existing cart. If the variant exists, quantity will be incremented.
    /// </summary>
    public sealed class AddCartItemDto
    {
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; }
    }

    /// <summary>
    /// DTO to update quantity or price of an existing cart item.
    /// </summary>
    public sealed class UpdateCartItemDto
    {
        public Guid CartId { get; set; }
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public long UnitPriceNetMinor { get; set; }
        public decimal VatRate { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // concurrency token
    }

    /// <summary>
    /// DTO to remove a cart item by marking it as deleted.
    /// </summary>
    public sealed class RemoveCartItemDto
    {
        public Guid CartId { get; set; }
        public Guid ItemId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO representing a cart along with its items; used for display/edit.
    /// </summary>
    public sealed class CartDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? AnonymousId { get; set; }
        public string Currency { get; set; } = "EUR";
        public List<CartItemDto> Items { get; set; } = new();
        public string? CouponCode { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
