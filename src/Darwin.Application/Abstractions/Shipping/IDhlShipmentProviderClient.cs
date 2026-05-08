using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;

namespace Darwin.Application.Abstractions.Shipping;

/// <summary>
/// Creates DHL shipments and retrieves provider labels.
/// </summary>
public interface IDhlShipmentProviderClient
{
    Task<DhlShipmentCreateResult> CreateShipmentAsync(
        SiteSetting settings,
        Order order,
        Shipment shipment,
        CheckoutAddressDto receiver,
        CancellationToken ct = default);

    Task<DhlShipmentLabelResult> GetLabelAsync(
        SiteSetting settings,
        Shipment shipment,
        CancellationToken ct = default);
}

public sealed class DhlShipmentCreateResult
{
    public string ProviderShipmentReference { get; init; } = string.Empty;

    public string? TrackingNumber { get; init; }

    public byte[]? LabelPdfBytes { get; init; }

    public string? ProviderLabelUrl { get; init; }
}

public sealed class DhlShipmentLabelResult
{
    public byte[]? LabelPdfBytes { get; init; }

    public string? ProviderLabelUrl { get; init; }
}
