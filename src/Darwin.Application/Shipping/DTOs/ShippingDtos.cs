using System;
using System.Collections.Generic;

namespace Darwin.Application.Shipping.DTOs
{
    public sealed class ShippingMethodCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? CountriesCsv { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Currency { get; set; }
        public List<ShippingRateDto> Rates { get; set; } = new();
    }

    public sealed class ShippingMethodEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string? CountriesCsv { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Currency { get; set; }
        public List<ShippingRateDto> Rates { get; set; } = new();
    }

    public sealed class ShippingRateDto
    {
        public Guid? Id { get; set; }
        public int? MaxShipmentMass { get; set; }
        public long? MaxSubtotalNetMinor { get; set; }
        public long PriceMinor { get; set; }
        public int SortOrder { get; set; }
    }

    public sealed class ShippingMethodListItemDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int RatesCount { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }

    public sealed class RateShipmentInputDto
    {
        public string Country { get; set; } = "DE";
        public long SubtotalNetMinor { get; set; }
        public int ShipmentMass { get; set; }
        public string? Currency { get; set; }
    }

    public sealed class ShippingOptionDto
    {
        public Guid MethodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long PriceMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
    }
}
