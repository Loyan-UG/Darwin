using System;
using System.Collections.Generic;

namespace Darwin.Application.Shipping.DTOs
{
    /// <summary>
    /// Represents a single shipping rate tier. Each tier defines a maximum
    /// weight and/or subtotal threshold and the price for orders falling into
    /// that tier. SortOrder determines the order of evaluation.
    /// </summary>
    public sealed class ShippingRateDto
    {
        public Guid? ShippingMethodId { get; set; }
        public int? MaxWeight { get; set; }                  // grams; null means no weight limit
        public long? MaxSubtotalNetMinor { get; set; }       // subtotal in minor units (e.g. cents); null = no limit
        public long PriceMinor { get; set; }                 // price in minor units; must be >= 0
        public int SortOrder { get; set; }                   // ordering of rates (0-based)
    }

    /// <summary>
    /// DTO used when creating a shipping method. Includes basic info and a collection of rates.
    /// </summary>
    public sealed class ShippingMethodCreateDto
    {
        public string Name { get; set; } = string.Empty;       // friendly name (e.g. "Standard Shipping")
        public string? Carrier { get; set; }                   // e.g. "DHL", optional
        public string? Service { get; set; }                   // e.g. "Express", optional
        public string CountriesCsv { get; set; } = "";         // ISO 3166-1 alpha-2 codes (comma-separated)
        public bool IsActive { get; set; } = true;
        public List<ShippingRateDto> Rates { get; set; } = new(); // must have at least one rate
    }

    /// <summary>
    /// DTO used when editing a shipping method. Includes identity, concurrency token, and nested rates.
    /// </summary>
    public sealed class ShippingMethodEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // concurrency check

        public string Name { get; set; } = string.Empty;
        public string? Carrier { get; set; }
        public string? Service { get; set; }
        public string CountriesCsv { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public List<ShippingRateDto> Rates { get; set; } = new();
    }

    /// <summary>
    /// Lightweight DTO representing a shipping method in list views.
    /// </summary>
    public sealed class ShippingMethodListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Carrier { get; set; }
        public string? Service { get; set; }
        public bool IsActive { get; set; }
    }
}
