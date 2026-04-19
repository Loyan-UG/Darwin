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
        public string Name { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? CountriesCsv { get; set; }
        public string? Currency { get; set; }
        public bool IsActive { get; set; }
        public int RatesCount { get; set; }
        public bool IsDhl { get; set; }
        public bool HasGlobalCoverage { get; set; }
        public bool HasMultipleRates { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }

    public enum ShippingMethodQueueFilter
    {
        All = 0,
        Active = 1,
        Inactive = 2,
        MissingRates = 3,
        Dhl = 4,
        GlobalCoverage = 5,
        MultiRate = 6
    }

    public sealed class ShippingMethodOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int MissingRatesCount { get; set; }
        public int DhlCount { get; set; }
        public int GlobalCoverageCount { get; set; }
        public int MultiRateCount { get; set; }
    }

    public sealed class RateShipmentInputDto
    {
        public string Country { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault;
        public long SubtotalNetMinor { get; set; }
        public int ShipmentMass { get; set; }
        public string? Currency { get; set; }
    }

    public sealed class ShippingOptionDto
    {
        public Guid MethodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long PriceMinor { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
    }
}
