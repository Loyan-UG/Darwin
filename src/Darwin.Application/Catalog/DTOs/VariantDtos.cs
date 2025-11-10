using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.DTOs
{
    /// <summary>
    /// Lightweight row for listing product variants in admin selection grids.
    /// Projects only the fields required by the view to keep queries efficient.
    /// </summary>
    public sealed class ProductVariantListItemDto
    {
        /// <summary>Variant identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Owning product identifier.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Parent product's localized display name (per culture with fallback).</summary>
        public string ProductName { get; init; } = string.Empty;

        /// <summary>SKU of the variant (display + search).</summary>
        public string Sku { get; init; } = string.Empty;

        /// <summary>Optional GTIN for display/search.</summary>
        public string? Gtin { get; init; }

        /// <summary>Currency code for price fields (ISO 4217).</summary>
        public string Currency { get; init; } = "EUR";

        /// <summary>Base price (net) in minor units; useful for display.</summary>
        public long BasePriceNetMinor { get; init; }

        /// <summary>On-hand stock (for physical variants).</summary>
        public int StockOnHand { get; set; }

        /// <summary>Indicates whether this variant is digital (no shipping).</summary>
        public bool IsDigital { get; set; }

        /// <summary>
        /// Display label composed server-side (e.g. "ProductName — SKU: 123").
        /// Keeps the Razor view simple and avoids recomposition logic there.
        /// </summary>
        public string? Display { get; set; }

        /// <summary>RowVersion for completeness (not used by the list form yet).</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
