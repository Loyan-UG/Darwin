using System;

namespace Darwin.Mobile.Shared.Storage.Entities;

/// <summary>Lightweight cache entity for discovered businesses.</summary>
public sealed class BusinessCache
{
    public string BusinessId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Category { get; set; }
    public string? LogoUrl { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public DateTime CachedAtUtc { get; set; }
}
