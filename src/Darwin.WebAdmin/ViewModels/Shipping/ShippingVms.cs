using System;
using System.Collections.Generic;
using Darwin.Application.Shipping.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Shipping;

public sealed class ShippingMethodListItemVm
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
    public DateTime? ModifiedAtUtc { get; set; }
}

public sealed class ShippingMethodsListVm
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public string Query { get; set; } = string.Empty;
    public ShippingMethodQueueFilter Filter { get; set; }
    public List<ShippingMethodListItemVm> Items { get; set; } = new();
    public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class ShippingRateEditVm
{
    public Guid? Id { get; set; }
    public int? MaxShipmentMass { get; set; }
    public long? MaxSubtotalNetMinor { get; set; }
    public long PriceMinor { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ShippingMethodEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string Name { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string? CountriesCsv { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Currency { get; set; }
    public List<ShippingRateEditVm> Rates { get; set; } = new();
}
